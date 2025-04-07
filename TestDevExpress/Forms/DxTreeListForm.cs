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
using DevExpress.PivotGrid.OLAP.Mdx;
using DevExpress.Utils.DirectXPaint;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            __MainSplitContainer.Panel1.MinSize = 200;

            __ParamSplitContainer = new DxSplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 300, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterOrientation = Orientation.Vertical, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True, Name = "ParamSplitContainer" };
            __MainSplitContainer.Panel2.Controls.Add(__ParamSplitContainer);

            __ParamsPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "ParamsPanel" };
            _ParamsInit();
            __ParamSplitContainer.Panel1.Controls.Add(__ParamsPanel);

            __LogPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "LogPanel" };
            _LogInit();
            __ParamSplitContainer.Panel2.Controls.Add(__LogPanel);

            _SettingLoad();
            _SampleLoad();
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
            __DxTreeList.FilterBoxMode = RowFilterBoxMode.Server;
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
            if (SetingsLogToolTipChanges)
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
            string column = (args.ColumnIndex.HasValue ? "; Column:" + args.ColumnIndex.Value.ToString() : "");
            string parentNodeId = nodeInfo.ParentNodeFullId;

            string textInfo = "";
            string newValue = "";
            if (args.ColumnIndex.HasValue && nodeInfo.Cells != null && args.ColumnIndex.Value >= 0 && args.ColumnIndex.Value < nodeInfo.Cells.Length)
            {
                newValue = nodeInfo.Cells[args.ColumnIndex.Value] as string;
                textInfo = $"Nová hodnota: '{newValue}'";
            }
            else
            {
                newValue = nodeInfo.TextEdited;
                textInfo = $"Výchozí hodnota: '{nodeInfo.Text}' => Nová hodnota: '{newValue}'";
            }

            _AddToLog($"Změna textu pro node '{nodeId}'{column}: {textInfo}");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            /*
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

            */
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
        #region Vytváření nodů, smazání a plnění dat do TreeListu
        /// <summary>
        /// Naplní nějaká výchozí data po otevření okna
        /// </summary>
        private void _SampleLoad()
        {
            _PrepareTreeList(25, 1);
        }
        /// <summary>
        /// Vymaže obsah TreeListu
        /// </summary>
        private void _ClearTreeList()
        {
            _PrepareTreeList(0, 0);
        }
        /// <summary>
        /// Naplní data do TreeListu pro daný požadavek na cca počet nodů a počet sub-úrovní
        /// </summary>
        /// <param name="sampleCountBase"></param>
        /// <param name="sampleLevelsCount"></param>
        private void _PrepareTreeList(int sampleCountBase, int sampleLevelsCount)
        {
            _LogClear();

            _CheckColumns();

            DxComponent.LogActive = true;

            __TotalNodesCount = 0;
            __SampleCountBase = sampleCountBase;
            __SampleLevelsCount = sampleLevelsCount;
            if (sampleCountBase == 0)
            {
                var time0 = DxComponent.LogTimeCurrent;
                __DxTreeList.ClearNodes();
                var time1 = DxComponent.LogTimeCurrent;
                this._AddToLog($"Smazání nodů z TreeListu; čas: {DxComponent.LogGetTimeElapsed(time0, time1, DxComponent.LogTokenTimeMilisec)} ms");
            }
            else
            {
                var time0 = DxComponent.LogTimeCurrent;
                var nodes = _CreateNodes(null);
                var time1 = DxComponent.LogTimeCurrent;
                __DxTreeList.AddNodes(nodes, true, PreservePropertiesMode.None);
                var time2 = DxComponent.LogTimeCurrent;

                this._AddToLog($"Tvorba nodů; počet: {nodes.Length}; čas: {DxComponent.LogGetTimeElapsed(time0, time1, DxComponent.LogTokenTimeMilisec)} ms");
                this._AddToLog($"Plnění nodů do TreeList; počet: {nodes.Length}; čas: {DxComponent.LogGetTimeElapsed(time1, time2, DxComponent.LogTokenTimeMilisec)} ms");
            }

            __DxTreeList.TreeListNative.Refresh();

            string text = this.GetControlStructure();
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné sloupce podle předvolby <see cref="SettingsUseMultiColumns"/>
        /// </summary>
        private void _CheckColumns()
        {
            bool useMultiColumns = SettingsUseMultiColumns;
            var dxColumns = __DxTreeList.TreeListNative.DxColumns;
            if (useMultiColumns && (dxColumns is null || dxColumns.Length < 3))
                _CreateMultiColumns();
            else if (!useMultiColumns && (dxColumns != null && dxColumns.Length >= 3))
                _CreateSingleColumns();
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné Multi sloupce
        /// </summary>
        private void _CreateMultiColumns()
        {
            List<DataTreeListColumn> dxColumns = new List<DataTreeListColumn>();
            dxColumns.Add(new DataTreeListColumn() { Caption = "Text", Width = 220, MinWidth = 150, CanEdit = true });
            dxColumns.Add(new DataTreeListColumn() { Caption = "Informace", Width = 120, MinWidth = 80, HeaderContentAlignment = DevExpress.Utils.HorzAlignment.Center, CellContentAlignment = DevExpress.Utils.HorzAlignment.Far, CanEdit = false });
            dxColumns.Add(new DataTreeListColumn() { Caption = "Popisek", Width = 160, MinWidth = 100, CanEdit = true });
            __DxTreeList.DxColumns = dxColumns.ToArray();
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné Single sloupce
        /// </summary>
        private void _CreateSingleColumns()
        {
            __DxTreeList.DxColumns = null;
        }
        /// <summary>
        /// Vytvoří nody
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="canAddEditable"></param>
        /// <param name="canAddShowNext"></param>
        /// <returns></returns>
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
                bool hasChild = false;
                if (canAddChilds(currentLevel))
                {
                    var childCount = _AddNodesToList(child, canAddEditable, canAddShowNext, nodes);
                    hasChild = (childCount > 0);
                    if (hasChild && Randomizer.IsTrue(25))
                    {
                        child.Expanded = true;
                    }
                }
                // Tento konkrétní node mohu editovat tehdy, když node nemá SubNodes:
                //   Pokud by nebyla povolena editace celého TreeListu, tak nelze editovat ani takový Node!
                child.CanEdit = !hasChild;
                child.CanCheck = !hasChild && this.SettingsUseCheckBoxes && Randomizer.IsTrue(40);
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
            // Určí, zda je vhodné přidat subnody do dané úrovně
            bool canAddChilds(int level)
            {
                if (level >= __SampleLevelsCount) return false;
                int probability = (level == 0 ? 50 : (level == 1 ? 25 : (level == 2 ? 10 : 0)));
                return Randomizer.IsTrue(probability);
            }
            // Určí počet prvků do daného levelu
            int getCount(int level)
            {
                if (level > __SampleLevelsCount) return 0;

                int baseCount = __SampleCountBase;
                if (level > 0)
                    baseCount = baseCount / (level + 1);

                return Randomizer.GetValueInRange(baseCount * 60 / 100, baseCount * 175 / 100);
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
            if (__TotalNodesCount >= _MaxNodesCount) return null;

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
                    childNode.FontStyle = FontStyle.Italic;
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

                    // Více sloupců?
                    if (SettingsUseMultiColumns)
                    {
                        childNode.Cells = new string[]
                        {
                            text,
                            Randomizer.GetSentence(1, 3),
                            Randomizer.GetSentence(1, 3)
                        };
                    }

                    _FillNode(childNode);
                    __TotalNodesCount++;
                    break;
            }
            return childNode;
        }
        /// <summary>
        /// Naplní data do nodu, vyjma textu. Plní ToolTip, ikony, styl, kalíšek - podle Settings.
        /// </summary>
        /// <param name="node"></param>
        private void _FillNode(DataTreeListNode node)
        {
            if (Randomizer.IsTrue(7))
                node.ImageDynamicDefault = _GetSuffixImageName();

            node.ImageName = _GetMainImageName(SettingsNodeImageSet);
            node.ToolTipTitle = null;
            node.ToolTipText = Randomizer.GetSentence(10, 50);

            if (SettingsUseExactStyle && Randomizer.IsTrue(33))
            {
                node.FontStyle = getRandomFontStyle();
                node.FontSizeRatio = getRandomSizeRatio();
                node.BackColor = getRandomBackColor();
                node.ForeColor = getRandomForeColor();
            }
            if (SettingsUseStyleName && Randomizer.IsTrue(33))
            {
                node.StyleName = getRandomStyleName();
            }

            FontStyle? getRandomFontStyle()
            {
                FontStyle? result = null;
                if (Randomizer.IsTrue(60))
                {
                    bool isBold = Randomizer.IsTrue(50);
                    bool isItalic = Randomizer.IsTrue(20);
                    result = (isBold ?  FontStyle.Bold : FontStyle.Regular) 
                           | (isItalic ? FontStyle.Italic : FontStyle.Regular);
                }
                return result;
            }
            float? getRandomSizeRatio()
            {
                float? result = null;
                if (Randomizer.IsTrue(60))
                {
                    int delta = Randomizer.GetValueInRange(6, 14);
                    result = ((float)delta) / 10f;
                }
                return result;
            }
            Color? getRandomBackColor()
            {
                Color? result = null;
                if (Randomizer.IsTrue(20))
                {
                    result = _GetRandomBackColor();
                }
                return result;
            }
            Color? getRandomForeColor()
            {
                Color? result = null;
                if (Randomizer.IsTrue(35))
                {
                    result = _GetRandomForeColor();
                }
                return result;
            }
            string getRandomStyleName()
            {
                string result = null;
                if (Randomizer.IsTrue(35))
                {
                    result = _GetRandomStyleName();
                }
                return result;
            }
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
        /// Maximální počet nodů
        /// </summary>
        private static int _MaxNodesCount { get { return 10000; } }
        /// <summary>
        /// Základní typický počet nodů v Root úrovni; pro subnody je poloviční.
        /// </summary>
        private int __SampleCountBase;
        /// <summary>
        /// Maximální počet úrovní
        /// </summary>
        private int __SampleLevelsCount;
        #endregion
        #region Ikony: druhy ikon, seznam názvů podle druhů, generátor ikony, barvy, stylu
        /// <summary>
        /// Vrátí náhodný Main obrázek z dané sady <paramref name="imageSet"/>.
        /// </summary>
        /// <returns></returns>
        private string _GetMainImageName(NodeImageSetType imageSet)
        {
            var images = _GetMainImageNames(imageSet);
            if (images != null && images.Length > 0)
                return Randomizer.GetItem(images);
            return null;
        }
        /// <summary>
        /// Vrať náhodný Suffix image name
        /// </summary>
        /// <returns></returns>
        private string _GetSuffixImageName()
        {
            if (__ImagesSuffix is null)
            {
                __ImagesSuffix = new string[]
                {
                    "svgimages/xaf/action_navigation_history_back.svg",
                    "svgimages/xaf/action_navigation_history_forward.svg",
                    "svgimages/xaf/action_navigation_next_object.svg",
                    "svgimages/xaf/action_navigation_previous_object.svg"
                };
            }
            return Randomizer.GetItem(__ImagesSuffix);
        }
        /// <summary>
        /// Vrať náhodnou světlou barvu pro BackColor
        /// </summary>
        /// <returns></returns>
        private Color _GetRandomBackColor()
        {
            if (__BackColors is null)
            {
                int h = 240;
                int l = 210;
                __BackColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__BackColors);
        }
        /// <summary>
        /// Vrať náhodnou tmavou barvu pro ForeColor
        /// </summary>
        /// <returns></returns>
        private Color _GetRandomForeColor()
        {
            if (__ForeColors is null)
            {
                int h = 80;
                int l = 24;
                __ForeColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__ForeColors);
        }
        private string _GetRandomStyleName()
        {
            if (__StyleNames is null)
                __StyleNames = new string[]
                {   // Nebudu dávat všechny styly, jen vybrané:
                    AdapterSupport.StyleDefault,
                    AdapterSupport.StyleOK,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleImportant,
                    AdapterSupport.StyleNote,
                    AdapterSupport.StyleHeader1

                };
            return Randomizer.GetItem(__StyleNames);
        }
        /// <summary>
        /// Vrátí set požadovaných obrázků. Autoinicializační.
        /// </summary>
        /// <param name="imageSet"></param>
        /// <returns></returns>
        private string[] _GetMainImageNames(NodeImageSetType imageSet)
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
        private string[] __ImagesSuffix;
        private Color[] __BackColors;
        private Color[] __ForeColors;
        private string[] __StyleNames;
        internal enum NodeImageSetType
        {
            None,
            Documents,
            Actions,
            Formats,
            Charts,
            Spreadsheet
        }
        private NewNodePositionType __NewNodePosition = NewNodePositionType.None;
        private enum NewNodePositionType { None, First, Last }
        #endregion
        #region Parametry v okně
        /// <summary>
        /// Vytvoří obsah panelu s parametry
        /// </summary>
        private void _ParamsInit()
        {
            int maxY = 0;
            int topY = 8;
            int columnSpace = 20;

            int x = 25;
            int y = topY;
            int labelOffset = 22;
            int labelWidth = 110;
            int comboWidth = 220;
            int comboOffset = 5;
            createTitle("Základní vlastnosti TreeListu");
            __CheckMultiSelect = createToggle(false, "MultiSelect", "MultiSelectEnabled", "MultiSelectEnabled = výběr více nodů", "Zaškrtnuto: lze vybrat více nodů (Ctrl, Shift). Sledujme pak události.");
            __TextNodeIndent = createSpinner(false, "NodeIndent", "Node indent:", 0, 100, "Node indent = odstup jednotlivých úrovní stromu", "Počet pixelů mezi nody jedné úrovně a jejich podřízenými nody, doprava.");
            __CheckShowTreeLines = createToggle(false, "ShowTreeLines", "Guide Lines Visible", "Guide Lines Visible = vodicí linky jsou viditelné", "Zaškrtnuto: Strom obsahuje GuideLines mezi úrovněmi nodů.", false);
            __CheckShowFirstLines = createToggle(false, "ShowFirstLines", "Show First Lines", "Show First Lines = zobrazit vodicí linky v první úrovni", "Zaškrtnuto: Strom obsahuje GuideLines v levé úrovni.");
            __CheckShowHorzLines = createToggle(false, "ShowHorzLines", "Show Horizontal Lines", "", "");
            __CheckShowVertLines = createToggle(false, "ShowVertLines", "Show Vertical Lines", "", "");
            __ComboTreeLineStyle = createCombo(false, "TreeLineStyle", "TreeLine Style:", typeof(DevExpress.XtraTreeList.LineStyle));
            __CheckShowRoot = createToggle(false, "ShowRoot", "Show Root", "", "");
            __CheckShowHierarchyIndentationLines = createToggle(false, "ShowHierarchyLines", "Show Hierarchy Indentation Lines", "", "", true);
            __CheckShowIndentAsRowStyle = createToggle(false, "ShowIndentAsRow", "Show Indent As RowStyle", "", "");
            __ComboRowFilterBoxMode = createCombo(false, "RowFilterMode", "Row Filter Mode:", typeof(RowFilterBoxMode));
            __ComboCheckBoxStyle = createCombo(false, "CheckBxStyle", "CheckBox Style:", typeof(DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle));
            __ComboFocusRectStyle = createCombo(false, "FocusRectangleStyle", "Focus Style:", typeof(DevExpress.XtraTreeList.DrawFocusRectStyle));
            __CheckEditable = createToggle(false, "Editable", "Editable", "", "");
            __ComboEditingMode = createCombo(false, "EditingMode", "Editing mode:", typeof(DevExpress.XtraTreeList.TreeListEditingMode));
            __ComboEditorShowMode = createCombo(false, "EditorShowMode", "Editor Show Mode:", typeof(DevExpress.XtraTreeList.TreeListEditorShowMode));
            maxY = (y > maxY ? y : maxY);
            x += labelOffset + labelWidth + comboOffset + comboWidth + columnSpace;
            y = topY;

            labelWidth = 100;
            comboWidth = 200;
            createTitle("Vlastnosti jednotlivých prvků");
            __ComboNodeImageSet = createCombo(true, "NodeImageType", "Node images:", typeof(NodeImageSetType));
            __CheckUseExactStyle = createToggle(true, "UseExactStyle", "Use explicit styles", "Použít exaktně dané nastavení stylu", "Budou vepsány hodnoty jako FontStyle, FontSizeDelta, BackColor, ForeColor");
            __CheckUseStyleName = createToggle(true, "UseStyleName", "Use Style Cup", "Použít styl daný kalíškem", "Bude vepsán StyleName, ten bude dohledán a aplikován");
            __CheckUseCheckBoxes = createToggle(true, "UseCheckBoxes", "Use Check Boxes", "Použít pro některé koncové nody CheckBoxy", "Některé nody, které nemají podřízenou úroveň, budou zobrazeny jako CheckBox");
            __CheckUseMultiColumns = createToggle(true, "UseMultiColumns", "Use Multi Columns", "Zobrazit více sloupců v TreeListu", "TreeList pak může připomínat BrowseGrid se stromem");

            y += 25;
            createTitle("Vytvoření prvků stromu");
            y += 10;
            DxComponent.CreateDxSimpleButton(x + labelOffset, y, 120, 30, __ParamsPanel, "Vytvoř 15:1", _NodeCreateClick, tag: new Tuple<int, int>(15, 1));
            DxComponent.CreateDxSimpleButton(x + labelOffset + 130, y, 120, 30, __ParamsPanel, "Vytvoř 25:2", _NodeCreateClick, tag: new Tuple<int, int>(25, 2)); 
            y += 38;
            DxComponent.CreateDxSimpleButton(x + labelOffset, y, 120, 30, __ParamsPanel, "Vytvoř 40:3", _NodeCreateClick, tag: new Tuple<int, int>(40, 3)); 
            DxComponent.CreateDxSimpleButton(x + labelOffset + 130, y, 120, 30, __ParamsPanel, "Vytvoř 60:4", _NodeCreateClick, tag: new Tuple<int, int>(60, 4));
            y += 38;
            DxComponent.CreateDxSimpleButton(x + labelOffset, y, 250, 30, __ParamsPanel, "Smaž všechny prvky", _NodeCreateClick, tag: new Tuple<int, int>(0, 0));
            maxY = (y > maxY ? y : maxY);
            x += labelOffset + labelWidth + comboOffset + comboWidth + columnSpace;
            y = topY;

            labelWidth = 100;
            comboWidth = 150;
            createTitle("Logování");
            __CheckLogToolTipChanges = createToggle(false, "", "Log: ToolTipChange", "Logovat události ToolTipChange", "Zaškrtnuto: při pohybu myši se plní Log událostí.");
            DxComponent.CreateDxSimpleButton(x + labelOffset, ref y, 160, 30, __ParamsPanel, "Smazat Log", _LogClearBtnClick, shiftY: true);
            maxY = (y > maxY ? y : maxY);

            __ParamSplitContainer.Panel1.MinSize = (maxY + 8);


            // Tvorba prvků:
            DxTitleLabelControl createTitle(string text)
            {
                return DxComponent.CreateDxTitleLabel(x, ref y, (labelWidth + comboOffset + comboWidth), __ParamsPanel, text, shiftY: true);
            }
            DxCheckEdit createToggle(bool isClearNode, string controlInfo, string text, string toolTipTitle, string toolTipText, bool? allowGrayed = null)
            {
                var toggle = DxComponent.CreateDxCheckEdit(x, ref y, (labelWidth + comboOffset + comboWidth), __ParamsPanel, text, _ParamsChanged, 
                    DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                    toolTipTitle, toolTipText, allowGrayed: allowGrayed, shiftY: true);
                toggle.Tag = _PackTag(isClearNode, controlInfo);
                return toggle;
            }
            DxSpinEdit createSpinner(bool isClearNode, string controlInfo, string label, int minValue, int maxValue, string toolTipTitle, string toolTipText)
            {
                DxComponent.CreateDxLabel(x + labelOffset, y + 3, labelWidth, __ParamsPanel, label, LabelStyleType.Default, hAlignment: DevExpress.Utils.HorzAlignment.Far);

                var spinner = DxComponent.CreateDxSpinEdit(x + labelOffset + labelWidth + comboOffset, ref y, 50, __ParamsPanel, _ParamsChanged,
                    minValue, maxValue, mask: "####", spinStyles: DevExpress.XtraEditors.Controls.SpinStyles.Vertical, 
                    toolTipTitle: toolTipTitle, toolTipText: toolTipText, shiftY: true);
                spinner.Tag = _PackTag(isClearNode, controlInfo);
                return spinner;
            }
            DxImageComboBoxEdit createCombo(bool isClearNode, string controlInfo, string label, Type enumType)
            {
                DxComponent.CreateDxLabel(x + labelOffset, y + 3, labelWidth, __ParamsPanel, label, LabelStyleType.Default, hAlignment: DevExpress.Utils.HorzAlignment.Far);

                var combo = DxComponent.CreateDxImageComboBox(x + labelOffset + labelWidth + comboOffset, ref y, comboWidth, __ParamsPanel, _ParamsChanged, shiftY: true);
                combo.Tag = _PackTag(isClearNode, controlInfo);

                var enumName = enumType.Name + ".";
                var names = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType);
                for (int n = 0; n < names.Length; n++)
                {
                    var item = new DevExpress.XtraEditors.Controls.ImageComboBoxItem() { Description = enumName + names[n], Value = values.GetValue(n) };
                    combo.Properties.Items.Add(item);
                }
                return combo;
            }
        }
        /// <summary>
        /// Zabalí data do Tagu
        /// </summary>
        /// <param name="isClearNode"></param>
        /// <param name="controlInfo"></param>
        /// <returns></returns>
        private object _PackTag(bool isClearNode, string controlInfo)
        {
            return new Tuple<bool, string>(isClearNode, controlInfo);
        }
        /// <summary>
        /// Rozbalí data vložená do Tagu
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="isClearNode"></param>
        /// <param name="controlInfo"></param>
        private void _UnPackTag(object tag, out bool isClearNode, out string controlInfo)
        {
            isClearNode = false;
            controlInfo = null;
            if (tag is Tuple<bool, string> tuple)
            {
                isClearNode = tuple.Item1;
                controlInfo = tuple.Item2;
            }
        }
        /// <summary>
        /// Událost po změně parametru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ParamsChanged(object sender, EventArgs e)
        {
            if (!__SettingsLoaded) return;
            var control = sender as Control;

            // Hodnoty z parametrů přenesu do properties Settings* :
            //  Protože na ně reaguje např. _ClearTreeList() => _CheckColumns() :
            _SettingCollect();

            // Návaznosti z konkrétního controlu = logovat hodnotu parametru; nulovat obsah => nastavit sloupce:
            _UnPackTag(control.Tag, out bool isClearNode, out string controlInfo);
            if (!String.IsNullOrEmpty(controlInfo))
                _AddToLogParamChange(control, controlInfo);
            if (isClearNode)
                _ClearTreeList();

            // Uložit Settings do TreeListu a do configu:
            _SettingOnInteractiveChanged();
        }
        /// <summary>
        /// Do logu vepíše informaci o změně parametru
        /// </summary>
        /// <param name="control"></param>
        /// <param name="controlInfo"></param>
        private void _AddToLogParamChange(Control control, string controlInfo)
        {
            string text = "";
            if (control is DxCheckEdit checkEdit)
            {
                if (checkEdit.Properties.AllowGrayed)
                    text = "; CheckState: " + checkEdit.CheckState.ToString();
                else
                    text = "; Checked: " + checkEdit.Checked.ToString();
            }
            else if (control is DxSpinEdit spinEdit)
            {
                text = "; Value: " + spinEdit.Value.ToString("###0");
            }
            else if (control is DxImageComboBoxEdit comboBox)
            {
                text = "; Selected: " + (comboBox.SelectedItem != null ? comboBox.SelectedItem?.ToString() : "NULL");
            }
            _AddToLog($"Change Setting: {controlInfo}{text}");
        }
        /// <summary>
        /// Po kliknutí na tlačítko Vytvoř / Smaž data TreeListu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NodeCreateClick(object sender, EventArgs e)
        {
            if (sender is Control control && control.Tag is Tuple<int, int> tuple)
                _PrepareTreeList(tuple.Item1, tuple.Item2);
        }
        /// <summary>
        /// Po kliknutí na tlačítko Clear Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LogClearBtnClick(object sender, EventArgs e)
        {
            _LogClear();
        }
        private DxCheckEdit __CheckMultiSelect;
        private DxSpinEdit __TextNodeIndent;
        private DxCheckEdit __CheckShowTreeLines;
        private DxCheckEdit __CheckShowFirstLines;
        private DxCheckEdit __CheckShowHorzLines;
        private DxCheckEdit __CheckShowVertLines;
        private DxImageComboBoxEdit __ComboTreeLineStyle;
        private DxCheckEdit __CheckShowRoot;
        private DxCheckEdit __CheckShowHierarchyIndentationLines;
        private DxCheckEdit __CheckShowIndentAsRowStyle;
        private DxImageComboBoxEdit __ComboRowFilterBoxMode;
        private DxImageComboBoxEdit __ComboCheckBoxStyle;
        private DxImageComboBoxEdit __ComboFocusRectStyle;
        private DxCheckEdit __CheckEditable;
        private DxImageComboBoxEdit __ComboEditingMode;
        private DxImageComboBoxEdit __ComboEditorShowMode;

        private DxImageComboBoxEdit __ComboNodeImageSet;
        private DxCheckEdit __CheckUseExactStyle;
        private DxCheckEdit __CheckUseStyleName;
        private DxCheckEdit __CheckUseCheckBoxes;
        private DxCheckEdit __CheckUseMultiColumns;

        private DxCheckEdit __CheckLogToolTipChanges;
        #endregion
        #region Settings: načtení/uložení do konfigurace; zobrazení/sesbírání z Checkboxů Params; aplikování do TreeListu
        /// <summary>
        /// Načte konfiguraci z Settings do properties, do TreeListu i do Parametrů
        /// </summary>
        private void _SettingLoad()
        {
            // Do Properties   z DxComponent.Settings
            SetingsMultiSelect = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsMultiSelect", ""));
            SettingsNodeIndent = ConvertToInt32(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsNodeIndent", ""), 25);
            SetingsShowTreeLines = ConvertToDefaultBoolean(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowTreeLines", null));
            SetingsShowFirstLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowFirstLines", ""));
            SetingsShowHorzLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowHorzLines", ""));
            SetingsShowVertLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowVertLines", ""));
            SettingsTreeLineStyle = ConvertToLineStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsTreeLineStyle", ""));
            SetingsShowRoot = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowRoot", ""));
            SetingsShowHierarchyIndentationLines = ConvertToDefaultBoolean(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowHierarchyIndentationLines", ""));
            SettingsShowIndentAsRowStyle = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsShowIndentAsRowStyle", ""));
            SetingsCheckBoxStyle = ConvertToNodeCheckBoxStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsCheckBoxStyle", ""));
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ""));
            SetingsFocusRectStyle = ConvertToDrawFocusRectStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsFocusRectStyle", ""));
            SettingsEditable = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditable", ""));
            SettingsEditingMode = ConvertToTreeListEditingMode(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditingMode", ""));
            SettingsEditorShowMode = ConvertToTreeListEditorShowMode(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditorShowMode", ""));
            
            SettingsNodeImageSet = ConvertToNodeImageSetType(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsNodeImageSet ", ""), NodeImageSetType.Documents);
            SettingsUseExactStyle = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseExactStyle", ""));
            SettingsUseStyleName = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseStyleName", ""));
            SettingsUseCheckBoxes = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseCheckBoxes", ""));
            SettingsUseMultiColumns = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseMultiColumns", ""));

            SetingsLogToolTipChanges = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsLogToolTipChanges", "N"));

            _SettingShow();
            _SettingApply();

            // Teprve od tohoto místa se změny ukládají do Settings:
            __SettingsLoaded = true;
        }
        /// <summary>
        /// Uloží konfiguraci z Properties do Settings
        /// </summary>
        private void _SettingSave()
        {
            if (!__SettingsLoaded) return;

            // Do DxComponent.Settings   z Properties
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsMultiSelect", ConvertToString(SetingsMultiSelect));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsNodeIndent", ConvertToString(SettingsNodeIndent));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowTreeLines", ConvertToString(SetingsShowTreeLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowFirstLines", ConvertToString(SetingsShowFirstLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowHorzLines", ConvertToString(SetingsShowHorzLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowVertLines", ConvertToString(SetingsShowVertLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsTreeLineStyle", ConvertToString(SettingsTreeLineStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowRoot", ConvertToString(SetingsShowRoot));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowHierarchyIndentationLines", ConvertToString(SetingsShowHierarchyIndentationLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsShowIndentAsRowStyle", ConvertToString(SettingsShowIndentAsRowStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsCheckBoxStyle", ConvertToString(SetingsCheckBoxStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ConvertToString(SetingsRowFilterBoxMode));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsFocusRectStyle", ConvertToString(SetingsFocusRectStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditable", ConvertToString(SettingsEditable));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditingMode", ConvertToString(SettingsEditingMode));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditorShowMode", ConvertToString(SettingsEditorShowMode));

            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsNodeImageSet", ConvertToString(SettingsNodeImageSet));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseExactStyle", ConvertToString(SettingsUseExactStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseStyleName", ConvertToString(SettingsUseStyleName));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseCheckBoxes", ConvertToString(SettingsUseCheckBoxes));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseMultiColumns", ConvertToString(SettingsUseMultiColumns));
            

            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsLogToolTipChanges", ConvertToString(SetingsLogToolTipChanges));
        }
        /// <summary>
        /// Main klíč v Settings pro zdejší proměnné
        /// </summary>
        private const string SettingsKey = "DxTreeListValues";
        /// <summary>
        /// Hodnoty z properties vepíše do vizuálních parametrů (checkboxy), přitom potlačí eventy o změnách
        /// </summary>
        private void _SettingShow()
        {
            // Do vizuálních checkboxů   z Properties
            __CheckMultiSelect.Checked = SetingsMultiSelect;
            __TextNodeIndent.Value = SettingsNodeIndent;
            __CheckShowTreeLines.CheckState = ConvertToCheckState(SetingsShowTreeLines);
            __CheckShowFirstLines.Checked = SetingsShowFirstLines;
            __CheckShowHorzLines.Checked = SetingsShowHorzLines;
            __CheckShowVertLines.Checked = SetingsShowVertLines;
            SelectComboItem(__ComboTreeLineStyle, SettingsTreeLineStyle);
            __CheckShowRoot.Checked = SetingsShowRoot;
            __CheckShowHierarchyIndentationLines.CheckState = ConvertToCheckState(SetingsShowHierarchyIndentationLines);
            __CheckShowIndentAsRowStyle.Checked = SettingsShowIndentAsRowStyle;
            SelectComboItem(__ComboCheckBoxStyle, SetingsCheckBoxStyle);
            SelectComboItem(__ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            SelectComboItem(__ComboFocusRectStyle, SetingsFocusRectStyle);
            __CheckEditable.Checked = SettingsEditable;
            SelectComboItem(__ComboEditingMode, SettingsEditingMode);
            SelectComboItem(__ComboEditorShowMode, SettingsEditorShowMode);

            SelectComboItem(__ComboNodeImageSet, SettingsNodeImageSet);
            __CheckUseExactStyle.Checked = SettingsUseExactStyle;
            __CheckUseStyleName.Checked = SettingsUseStyleName;
            __CheckUseCheckBoxes.Checked = SettingsUseCheckBoxes;
            __CheckUseMultiColumns.Checked = SettingsUseMultiColumns;

            __CheckLogToolTipChanges.Checked = SetingsLogToolTipChanges;
        }
        /// <summary>
        /// Hodnoty z vizuálních parametrů (checkboxy) opíše do properties, nic dalšího nedělá
        /// </summary>
        private void _SettingCollect()
        {
            SetingsMultiSelect = __CheckMultiSelect.Checked;
            SetingsShowTreeLines = ConvertToDefaultBoolean(__CheckShowTreeLines.CheckState);
            SettingsNodeIndent = (int)__TextNodeIndent.Value;
            SetingsShowFirstLines = __CheckShowFirstLines.Checked;
            SetingsShowHorzLines = __CheckShowHorzLines.Checked;
            SetingsShowVertLines = __CheckShowVertLines.Checked;
            SettingsTreeLineStyle = ConvertToLineStyle(__ComboTreeLineStyle, SettingsTreeLineStyle);
            SetingsShowRoot = __CheckShowRoot.Checked;
            SetingsShowHierarchyIndentationLines = ConvertToDefaultBoolean(__CheckShowHierarchyIndentationLines.CheckState);
            SettingsShowIndentAsRowStyle = __CheckShowIndentAsRowStyle.Checked;
            SetingsCheckBoxStyle = ConvertToNodeCheckBoxStyle(__ComboCheckBoxStyle, SetingsCheckBoxStyle);
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(__ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            SetingsFocusRectStyle = ConvertToDrawFocusRectStyle(__ComboFocusRectStyle, SetingsFocusRectStyle);
            SettingsEditable = __CheckEditable.Checked;
            SettingsEditingMode = ConvertToTreeListEditingMode(__ComboEditingMode, SettingsEditingMode);
            SettingsEditorShowMode = ConvertToTreeListEditingMode(__ComboEditorShowMode, SettingsEditorShowMode);

            SettingsNodeImageSet = ConvertToNodeImageSetType(__ComboNodeImageSet, SettingsNodeImageSet);
            SettingsUseExactStyle = __CheckUseExactStyle.Checked;
            SettingsUseStyleName = __CheckUseStyleName.Checked;
            SettingsUseCheckBoxes = __CheckUseCheckBoxes.Checked;
            SettingsUseMultiColumns = __CheckUseMultiColumns.Checked;

            SetingsLogToolTipChanges = __CheckLogToolTipChanges.Checked;
        }
        /// <summary>
        /// Hodnoty z konfigurace vepíše do TreeListu
        /// </summary>
        private void _SettingApply()
        {
            // Do TreeListu   z Properties
            __DxTreeList.MultiSelectEnabled = SetingsMultiSelect;
            __DxTreeList.TreeListNative.TreeLevelWidth = SettingsNodeIndent;
            __DxTreeList.TreeListNative.OptionsView.ShowTreeLines = SetingsShowTreeLines;
            __DxTreeList.TreeListNative.OptionsView.ShowFirstLines = SetingsShowFirstLines;
            __DxTreeList.TreeListNative.OptionsView.ShowHorzLines = SetingsShowHorzLines;
            __DxTreeList.TreeListNative.OptionsView.ShowVertLines = SetingsShowVertLines;
            __DxTreeList.TreeListNative.OptionsView.TreeLineStyle = SettingsTreeLineStyle;
            __DxTreeList.TreeListNative.OptionsView.ShowRoot = SetingsShowRoot;
            __DxTreeList.TreeListNative.OptionsView.ShowHierarchyIndentationLines = SetingsShowHierarchyIndentationLines;
            __DxTreeList.TreeListNative.OptionsView.ShowIndentAsRowStyle = SettingsShowIndentAsRowStyle;
            __DxTreeList.TreeListNative.OptionsView.CheckBoxStyle = SetingsCheckBoxStyle;
            __DxTreeList.FilterBoxMode = SetingsRowFilterBoxMode;
            __DxTreeList.TreeListNative.OptionsView.RootCheckBoxStyle = DevExpress.XtraTreeList.NodeCheckBoxStyle.Default;
            __DxTreeList.TreeListNative.OptionsView.FocusRectStyle = SetingsFocusRectStyle;
            __DxTreeList.TreeListNative.OptionsBehavior.Editable = SettingsEditable;
            __DxTreeList.TreeListNative.OptionsBehavior.EditingMode = SettingsEditingMode;
            __DxTreeList.TreeListNative.OptionsBehavior.EditorShowMode = SettingsEditorShowMode;
        }
        /// <summary>
        /// Hodnoty z vizuálních parametrů (checkboxy) opíše do properties, do TreeListu a uloží do konfigurace
        /// </summary>
        private void _SettingOnInteractiveChanged()
        {
            if (!__SettingsLoaded) return;

            _SettingApply();
            _SettingSave();
        }

        // Properties jsou uváděny v typech odpovídajících TreeListu.
        //  Konvertují se z/na string do Settings;
        //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;
        internal bool SetingsMultiSelect { get; set; }
        internal int SettingsNodeIndent { get; set; }
        internal DevExpress.Utils.DefaultBoolean SetingsShowTreeLines { get; set; }
        internal bool SetingsShowFirstLines { get; set; }
        internal bool SetingsShowHorzLines { get; set; }
        internal bool SetingsShowVertLines { get; set; }
        internal DevExpress.XtraTreeList.LineStyle SettingsTreeLineStyle { get; set; }
        internal bool SetingsShowRoot { get; set; }
        internal DevExpress.Utils.DefaultBoolean SetingsShowHierarchyIndentationLines { get; set; }
        internal bool SettingsShowIndentAsRowStyle { get; set; }
        internal DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle SetingsCheckBoxStyle { get; set; }
        internal RowFilterBoxMode SetingsRowFilterBoxMode { get; set; }
        internal DevExpress.XtraTreeList.DrawFocusRectStyle SetingsFocusRectStyle { get; set; }
        internal bool SettingsEditable { get; set; }
        internal DevExpress.XtraTreeList.TreeListEditingMode SettingsEditingMode { get; set; }
        internal DevExpress.XtraTreeList.TreeListEditorShowMode SettingsEditorShowMode { get; set; }

        internal NodeImageSetType SettingsNodeImageSet { get; set; }
        internal bool SettingsUseExactStyle { get; set; }
        internal bool SettingsUseStyleName { get; set; }
        internal bool SettingsUseCheckBoxes { get; set; }
        internal bool SettingsUseMultiColumns { get; set; }

        internal bool SetingsLogToolTipChanges { get; set; }

        private bool __SettingsLoaded;
        #endregion
        #region Konverze typů
        internal static bool ConvertToBool(string value)
        {
            return (String.Equals(value, "Y", StringComparison.InvariantCultureIgnoreCase));
        }
        internal static string ConvertToString(bool value)
        {
            return value ? "Y" : "N";
        }
        internal static bool ConvertToBool(bool? value)
        {
            return value ?? false;
        }
        internal static string ConvertToString(bool? value)
        {
            return (value.HasValue ? (value.Value ? "Y" : "N") : "");
        }
        internal static bool? ConvertToBoolN(CheckState checkState)
        {
            switch (checkState)
            {
                case CheckState.Checked: return true;
                case CheckState.Unchecked: return false;
                case CheckState.Indeterminate: return null;
            }
            return null;
        }
        internal static bool? ConvertToBoolN(string value)
        {
            if (String.Equals(value, "Y", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (String.Equals(value, "N", StringComparison.InvariantCultureIgnoreCase)) return false;
            return null;
        }

        internal static int ConvertToInt32(string value, int defValue = 0)
        {
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out var number)) return number;
            return defValue;
        }
        internal static string ConvertToString(int value)
        {
            return value.ToString();
        }

        internal static CheckState ConvertToCheckState(bool? value)
        {
            if (value.HasValue) return (value.Value ? CheckState.Checked : CheckState.Unchecked);
            return CheckState.Indeterminate;
        }
        internal static CheckState ConvertToCheckState(DevExpress.Utils.DefaultBoolean value)
        {
            switch (value)
            {
                case DevExpress.Utils.DefaultBoolean.Default: return CheckState.Indeterminate;
                case DevExpress.Utils.DefaultBoolean.False: return CheckState.Unchecked;
                case DevExpress.Utils.DefaultBoolean.True: return CheckState.Checked;
            }
            return CheckState.Indeterminate;
        }

        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(string value, DevExpress.Utils.DefaultBoolean defValue = DevExpress.Utils.DefaultBoolean.Default)
        {
            if (!String.IsNullOrEmpty(value)) 
            {
                switch (value)
                {
                    case "D": return DevExpress.Utils.DefaultBoolean.Default;
                    case "N": return DevExpress.Utils.DefaultBoolean.False;
                    case "Y": return DevExpress.Utils.DefaultBoolean.True;
                }
            }
            return defValue;
        }
        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(CheckState value)
        {
            switch (value)
            {
                case CheckState.Indeterminate: return DevExpress.Utils.DefaultBoolean.Default;
                case CheckState.Unchecked: return DevExpress.Utils.DefaultBoolean.False;
                case CheckState.Checked: return DevExpress.Utils.DefaultBoolean.True;
            }
            return DevExpress.Utils.DefaultBoolean.Default;
        }
        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(bool? value)
        {
            if (value.HasValue) return (value.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False);
            return DevExpress.Utils.DefaultBoolean.Default;
        }
        internal static string ConvertToString(DevExpress.Utils.DefaultBoolean value)
        {
            switch (value)
            {
                case DevExpress.Utils.DefaultBoolean.Default: return "D";
                case DevExpress.Utils.DefaultBoolean.False: return "N";
                case DevExpress.Utils.DefaultBoolean.True: return "Y";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.LineStyle ConvertToLineStyle(string value, DevExpress.XtraTreeList.LineStyle defValue = DevExpress.XtraTreeList.LineStyle.Percent50)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.LineStyle.None;
                    case "P": return DevExpress.XtraTreeList.LineStyle.Percent50;
                    case "D": return DevExpress.XtraTreeList.LineStyle.Dark;
                    case "L": return DevExpress.XtraTreeList.LineStyle.Light;
                    case "W": return DevExpress.XtraTreeList.LineStyle.Wide;
                    case "G": return DevExpress.XtraTreeList.LineStyle.Large;
                    case "S": return DevExpress.XtraTreeList.LineStyle.Solid;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.LineStyle ConvertToLineStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.LineStyle defValue = DevExpress.XtraTreeList.LineStyle.Percent50)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.LineStyle)
                        return (DevExpress.XtraTreeList.LineStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.LineStyle)
                {
                    return (DevExpress.XtraTreeList.LineStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.LineStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.LineStyle.None: return "N";
                case DevExpress.XtraTreeList.LineStyle.Percent50: return "P";
                case DevExpress.XtraTreeList.LineStyle.Dark: return "D";
                case DevExpress.XtraTreeList.LineStyle.Light: return "L";
                case DevExpress.XtraTreeList.LineStyle.Wide: return "W";
                case DevExpress.XtraTreeList.LineStyle.Large: return "G";
                case DevExpress.XtraTreeList.LineStyle.Solid: return "S";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle ConvertToNodeCheckBoxStyle(string value, DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle defValue = DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "D": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default;
                    case "C": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Check;
                    case "R": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Radio;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle ConvertToNodeCheckBoxStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle defValue = DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)
                        return (DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)
                {
                    return (DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default: return "D";
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Check: return "C";
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Radio: return "R";
            }
            return "";
        }

        internal static RowFilterBoxMode ConvertToRowFilterBoxMode(string value, RowFilterBoxMode defValue = RowFilterBoxMode.None)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return RowFilterBoxMode.None;
                    case "C": return RowFilterBoxMode.Client;
                    case "S": return RowFilterBoxMode.Server;
                }
            }
            return defValue;
        }
        internal static RowFilterBoxMode ConvertToRowFilterBoxMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, RowFilterBoxMode defValue = RowFilterBoxMode.None)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is RowFilterBoxMode)
                        return (RowFilterBoxMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is RowFilterBoxMode)
                {
                    return (RowFilterBoxMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(RowFilterBoxMode value)
        {
            switch (value)
            {
                case RowFilterBoxMode.None: return "N";
                case RowFilterBoxMode.Client: return "C";
                case RowFilterBoxMode.Server: return "S";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.DrawFocusRectStyle ConvertToDrawFocusRectStyle(string value, DevExpress.XtraTreeList.DrawFocusRectStyle defValue = DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.DrawFocusRectStyle.None;
                    case "R": return DevExpress.XtraTreeList.DrawFocusRectStyle.RowFocus;
                    case "C": return DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus;
                    case "F": return DevExpress.XtraTreeList.DrawFocusRectStyle.RowFullFocus;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.DrawFocusRectStyle ConvertToDrawFocusRectStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.DrawFocusRectStyle defValue = DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.DrawFocusRectStyle)
                        return (DevExpress.XtraTreeList.DrawFocusRectStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.DrawFocusRectStyle)
                {
                    return (DevExpress.XtraTreeList.DrawFocusRectStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.DrawFocusRectStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.DrawFocusRectStyle.None: return "N";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.RowFocus: return "R";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus: return "C";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.RowFullFocus: return "F";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.TreeListEditingMode ConvertToTreeListEditingMode(string value, DevExpress.XtraTreeList.TreeListEditingMode defValue = DevExpress.XtraTreeList.TreeListEditingMode.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "D": return DevExpress.XtraTreeList.TreeListEditingMode.Default;
                    case "I": return DevExpress.XtraTreeList.TreeListEditingMode.Inplace;
                    case "F": return DevExpress.XtraTreeList.TreeListEditingMode.EditForm;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.TreeListEditingMode ConvertToTreeListEditingMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.TreeListEditingMode defValue = DevExpress.XtraTreeList.TreeListEditingMode.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.TreeListEditingMode)
                        return (DevExpress.XtraTreeList.TreeListEditingMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.TreeListEditingMode)
                {
                    return (DevExpress.XtraTreeList.TreeListEditingMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.TreeListEditingMode value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.TreeListEditingMode.Default: return "D";
                case DevExpress.XtraTreeList.TreeListEditingMode.Inplace: return "I";
                case DevExpress.XtraTreeList.TreeListEditingMode.EditForm: return "F";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.TreeListEditorShowMode ConvertToTreeListEditorShowMode(string value, DevExpress.XtraTreeList.TreeListEditorShowMode defValue = DevExpress.XtraTreeList.TreeListEditorShowMode.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.TreeListEditorShowMode.Default;
                    case "D": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDown;
                    case "U": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;
                    case "C": return DevExpress.XtraTreeList.TreeListEditorShowMode.Click;
                    case "F": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDownFocused;
                    case "2": return DevExpress.XtraTreeList.TreeListEditorShowMode.DoubleClick;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.TreeListEditorShowMode ConvertToTreeListEditingMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.TreeListEditorShowMode defValue = DevExpress.XtraTreeList.TreeListEditorShowMode.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.TreeListEditorShowMode)
                        return (DevExpress.XtraTreeList.TreeListEditorShowMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.TreeListEditorShowMode)
                {
                    return (DevExpress.XtraTreeList.TreeListEditorShowMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.TreeListEditorShowMode value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.TreeListEditorShowMode.Default: return "N";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDown: return "D";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp: return "U";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.Click: return "C";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDownFocused: return "F";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.DoubleClick: return "2";
            }
            return "";
        }


        internal static NodeImageSetType ConvertToNodeImageSetType(string value, NodeImageSetType defValue = NodeImageSetType.Documents)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return NodeImageSetType.None;
                    case "D": return NodeImageSetType.Documents;
                    case "A": return NodeImageSetType.Actions;
                    case "F": return NodeImageSetType.Formats;
                    case "C": return NodeImageSetType.Charts;
                    case "S": return NodeImageSetType.Spreadsheet;
                }
            }
            return defValue;
        }
        internal static NodeImageSetType ConvertToNodeImageSetType(DevExpress.XtraEditors.ComboBoxEdit comboBox, NodeImageSetType defValue = NodeImageSetType.Documents)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is NodeImageSetType)
                        return (NodeImageSetType)comboItem.Value;
                }
                if (comboBox.SelectedItem is NodeImageSetType)
                {
                    return (NodeImageSetType)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(NodeImageSetType value)
        {
            switch (value)
            {
                case NodeImageSetType.None: return "N";
                case NodeImageSetType.Documents: return "D";
                case NodeImageSetType.Actions: return "A";
                case NodeImageSetType.Formats: return "F";
                case NodeImageSetType.Charts: return "C";
                case NodeImageSetType.Spreadsheet: return "S";
            }
            return "";
        }

        internal static void SelectComboItem(DevExpress.XtraEditors.ImageComboBoxEdit comboBox, object value)
        {
            comboBox.SelectedItem = comboBox.Properties.Items.FirstOrDefault(i => isValidItem(i));

            bool isValidItem(object item)
            {
                if (item is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem && Object.Equals(comboItem.Value, value)) return true;
                if (Object.Equals(item, value)) return true;
                return false;
            }
        }

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
            string column = (args.ColumnIndex.HasValue ? "; Column:" + args.ColumnIndex.Value.ToString() : "");
            _AddToLog($"{actionName}: Node: {args.Node}{column}{value}");
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
