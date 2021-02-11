using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;
using DevExpress.XtraTreeList.StyleFormatConditions;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;

using DW = System.Drawing;
using WF = System.Windows.Forms;

namespace Djs.Tools.CovidGraphs
{
    public class DxTestTreeForm : WF.Form
    {
        #region Konstrukce
        public static void Run()
        {
            using (DxTestTreeForm form = new DxTestTreeForm())
            {
                form.ShowDialog();
            }
        }
        public DxTestTreeForm()
        {
            this.SetProperties();
            this.CreateComponents();
        }
        private void SetProperties()
        {
            this.Text = "Test TreeForm";
            this.Size = new DW.Size(750, 450);
            this.StartPosition = WF.FormStartPosition.CenterScreen;
        }

        private void CreateComponents()
        {
            CreateImageList();
            CreateTreeView();
            // CreateTreeViewSimple();
            // this.Load += DxTestTreeForm_Load;

        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _TreeList.BestFitColumns();
        }
        private void DxTestTreeForm_Load(object sender, EventArgs e)
        {
            CreateTreeView();
        }
        DevExpress.XtraTreeList.TreeList _TreeList;

        private void CreateImageList()
        {
            _Images16 = new ImageList();
            _Images16.Images.Add("book_add_16", Properties.Resources.book_add_16);
            _Images16.Images.Add("book_edit_16", Properties.Resources.book_edit_16);
            _Images16.Images.Add("book_delete_16", Properties.Resources.book_delete_16);
            _Images16.Images.Add("book_go_16", Properties.Resources.book_go_16);
            _Images16.Images.Add("book_key_16", Properties.Resources.book_key_16);
            _Images16.Images.Add("book_link_16", Properties.Resources.book_link_16);
            _Images16.Images.Add("book_2_16", Properties.Resources.book_2_16);

            _Images16.Images.Add("bug_16", Properties.Resources.bug_16);
            _Images16.Images.Add("bug_add_16", Properties.Resources.bug_add_16);
            _Images16.Images.Add("bug_delete_16", Properties.Resources.bug_delete_16);
            _Images16.Images.Add("bug_edit_16", Properties.Resources.bug_edit_16);
            _Images16.Images.Add("bug_go_16", Properties.Resources.bug_go_16);
            _Images16.Images.Add("bug_link_16", Properties.Resources.bug_link_16);

            _Images16.Images.Add("computer_add_16", Properties.Resources.computer_add_16);
            _Images16.Images.Add("computer_delete_16", Properties.Resources.computer_delete_16);
            _Images16.Images.Add("computer_edit_16", Properties.Resources.computer_edit_16);
            _Images16.Images.Add("computer_go_16", Properties.Resources.computer_go_16);
            _Images16.Images.Add("computer_key_16", Properties.Resources.computer_key_16);
            _Images16.Images.Add("computer_link_16", Properties.Resources.computer_link_16);

            _Images16.Images.Add("db_16", Properties.Resources.db_16);
            _Images16.Images.Add("db_add_16", Properties.Resources.db_add_16);
            _Images16.Images.Add("db_add_2_16", Properties.Resources.db_add_2_16);
            _Images16.Images.Add("db_comit_16", Properties.Resources.db_comit_16);
            _Images16.Images.Add("db_comit_2_16", Properties.Resources.db_comit_2_16);
            _Images16.Images.Add("db_remove_16", Properties.Resources.db_remove_16);
            _Images16.Images.Add("db_remove_2_16", Properties.Resources.db_remove_2_16);
            _Images16.Images.Add("db_status_16", Properties.Resources.db_status_16);
            _Images16.Images.Add("db_status_2_16", Properties.Resources.db_status_2_16);
            _Images16.Images.Add("db_update_16", Properties.Resources.db_update_16);
            _Images16.Images.Add("db_update_2_16", Properties.Resources.db_update_2_16);

            _Images16.Images.Add("edit_add_4_16", Properties.Resources.edit_add_4_16);
            _Images16.Images.Add("list_add_3_16", Properties.Resources.list_add_3_16);
            _Images16.Images.Add("hourglass_16", Properties.Resources.hourglass_16);
        }
        private int GetImageIndex(string imageName)
        {
            return (_Images16.Images.ContainsKey(imageName) ? _Images16.Images.IndexOfKey(imageName) : -1);
        }
        ImageList _Images16;


        private void CreateTreeView()
        {
            var treeList = new DxTreeViewListSimple() { Dock = DockStyle.Fill };
            //  treeList.StateImageList = _Images16;
            treeList.SelectImageList = _Images16;
            treeList.ImageIndexSearcher = GetImageIndex;
            treeList.Parent = this;
            this.Controls.Add(treeList);               // Musí být dřív než se začne pracovat s daty!!!
            _TreeList = treeList;

            treeList.AddNodes(NodeItemInfo.CreateSampleList());


        }

        private void CreateTreeViewSimple()
        {
            DevExpress.XtraTreeList.TreeList treeList1 = new DevExpress.XtraTreeList.TreeList() { Dock = WF.DockStyle.Fill };
            DevExpress.XtraTreeList.Columns.TreeListColumn treeListColumn1 = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            DevExpress.XtraTreeList.Columns.TreeListColumn treeListColumn2 = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            treeList1.Parent = this;
            this.Controls.Add(treeList1);               // Musí být dřív než se začne pracovat s daty!!!
            _TreeList = treeList1;


            ((System.ComponentModel.ISupportInitialize)(treeList1)).BeginInit();
            this.SuspendLayout();

            treeList1.Columns.AddRange(new DevExpress.XtraTreeList.Columns.TreeListColumn[] { treeListColumn1 , treeListColumn2 });

            treeListColumn1.OptionsColumn.AllowEdit = false;
            treeListColumn1.OptionsColumn.AllowSort = false;

            treeList1.OptionsView.FocusRectStyle = DrawFocusRectStyle.RowFocus;

            treeList1.BeginUnboundLoad();
            var node0 = treeList1.AppendNode(new object[] { "Node 1", "Popisek"}, -1);

            var node1 = treeList1.AppendNode(new object[] { "Node 11", "Popisek" }, 0);
            var node2 = treeList1.AppendNode(new object[] { "Node 2", "Popisek" }, -1);
            var node3 = treeList1.AppendNode(new object[] { "Node 21", "Popisek" }, 2);
            var node4 = treeList1.AppendNode(new object[] { "Node 22", "Popisek" }, 2);
            var node5 = treeList1.AppendNode(new object[] { "Node 3", "Popisek" }, -1);
            var node6 = treeList1.AppendNode(new object[] { "Node 31", "Popisek" }, 5);

//            treeList1.SelectImageList = _Images16;                    // Odkazuje se na node.ImageIndex nebo SelectImageIndex: reprezentuje první ikonu, když prvek je/není Selected
            treeList1.StateImageList = _Images16;                     // Odkazuje se na node.StateImageIndex: reprezentuje statickou ikonu u Node

            node0.CheckState = CheckState.Checked;

            node0.ImageIndex = -1;                   // Ikona když node NENÍ Selected, bere se z SelectImageList. Pokud není nastaveno, je zde 0 a bere se první ikona z Listu.
            node0.SelectImageIndex = 1;              // Ikona když node JE Selected, bere se z SelectImageList
            node0.StateImageIndex = 3;               // Trvalá ikona za první ikonou, bere se z StateImageList, nemění se při Selected
            node0.Expanded = true;

            node1.ImageIndex = -1;                   // Ikona když node NENÍ Selected, bere se z SelectImageList. Pokud není nastaveno, je zde 0 a bere se první ikona z Listu.
            node1.SelectImageIndex = -1;
            node1.StateImageIndex = 5;

            node2.StateImageIndex = 5;
            node2.SelectImageIndex = 13;

            node3.StateImageIndex = 11;
            node3.SelectImageIndex = 14;

            node4.StateImageIndex = 18;
            node4.SelectImageIndex = 16;


            treeList1.EndUnboundLoad();

            treeList1.OptionsView.AutoWidth = false;

            treeListColumn1.Caption = "treeListColumn1";
            treeListColumn1.FieldName = "treeListColumn1";
            treeListColumn1.Name = "treeListColumn1";
            treeListColumn1.Visible = true;
            treeListColumn1.VisibleIndex = 0;
            treeListColumn1.Width = 122;

            ((System.ComponentModel.ISupportInitialize)(treeList1)).EndInit();
            this.ResumeLayout(false);

















            treeList1.ViewStyle = TreeListViewStyle.TreeView;


        }

        private void CreateTreeViewFromDataSource()
        {   // https://docs.devexpress.com/WindowsForms/DevExpress.XtraTreeList.TreeList
            // https://docs.devexpress.com/WindowsForms/2434/controls-and-libraries/tree-list
            // https://docs.devexpress.com/WindowsForms/119635/controls-and-libraries/tree-list/feature-center/data-presentation/treeview-style

            DevExpress.XtraTreeList.TreeList treeList1 = new DevExpress.XtraTreeList.TreeList() { Dock = WF.DockStyle.Fill };
            treeList1.Parent = this;
            this.Controls.Add(treeList1);               // Musí být dřív než se začne pracovat s daty!!!
            _TreeList = treeList1;

            //Specify the fields that arrange underlying data as a hierarchy.
            treeList1.KeyFieldName = "ID";
            treeList1.ParentFieldName = "RegionID";
            //Allow the treelist to create columns bound to the fields the KeyFieldName and ParentFieldName properties specify.
            treeList1.OptionsBehavior.PopulateServiceColumns = true;
            //Specify the data source.
            treeList1.DataSource = SalesDataGenerator.CreateData();



            treeList1.ViewStyle = TreeListViewStyle.TreeView;



         //   treeList1.PopulateColumns();
         //   treeList1.Refresh();
         //   treeList1.ForceInitialize();

            //The treelist automatically creates columns for the public fields found in the data source. 
            //You do not need to call the TreeList.PopulateColumns method unless the treeList1.OptionsBehavior.AutoPopulateColumns option is disabled.

            //Change the row height.
            treeList1.RowHeight = 23;

            //Access the automatically created columns.
            TreeListColumn colRegion = treeList1.Columns["Region"];
            TreeListColumn colMarchSales = treeList1.Columns["MarchSales"];
            TreeListColumn colSeptemberSales = treeList1.Columns["SeptemberSales"];
            TreeListColumn colMarchSalesPrev = treeList1.Columns["MarchSalesPrev"];
            TreeListColumn colSeptemberSalesPrev = treeList1.Columns["SeptemberSalesPrev"];
            TreeListColumn colMarketShare = treeList1.Columns["MarketShare"];

            //Hide the key columns. An end-user can access them from the Customization Form.
            treeList1.Columns[treeList1.KeyFieldName].Visible = false;
            treeList1.Columns[treeList1.ParentFieldName].Visible = false;

            //Format column headers and cell values.
            colMarchSalesPrev.Caption = "<i>Previous <b>March</b> Sales</i>";
            colSeptemberSalesPrev.Caption = "<i>Previous <b>September</b> Sales</i>";
            treeList1.OptionsView.AllowHtmlDrawHeaders = true;
            colMarchSalesPrev.AppearanceCell.Font = new System.Drawing.Font(colMarchSalesPrev.AppearanceCell.Font, System.Drawing.FontStyle.Italic);
            colSeptemberSalesPrev.AppearanceCell.Font = new System.Drawing.Font(colSeptemberSalesPrev.AppearanceCell.Font, System.Drawing.FontStyle.Italic);

            //Create two hidden unbound columns that calculate their values from expressions.
            TreeListColumn colUnboundMarchChange = treeList1.Columns.AddField("FromPrevMarchChange");
            colUnboundMarchChange.Caption = "Change from prev March";
            colUnboundMarchChange.UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.Decimal;
            colUnboundMarchChange.UnboundExpression = "[MarchSales]-[MarchSalesPrev]";
            TreeListColumn colUnboundSeptemberChange = treeList1.Columns.AddField("FromPrevSepChange");
            colUnboundSeptemberChange.Caption = "Change from prev September";
            colUnboundSeptemberChange.UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.Decimal;
            colUnboundSeptemberChange.UnboundExpression = "[SeptemberSales]-[SeptemberSalesPrev]";
            colUnboundMarchChange.OptionsColumn.ShowInCustomizationForm = false;
            colUnboundSeptemberChange.OptionsColumn.ShowInCustomizationForm = false;

            //Make the Region column read-only.
            colRegion.OptionsColumn.ReadOnly = true;
            colRegion.OptionsColumn.AllowEdit = false;
            colRegion.OptionsColumn.AllowSort = false;
            colRegion.OptionsColumn.AllowSize = true;

            //Sort data against the Region column
            colRegion.SortIndex = 0;

            //Apply a filter.
            treeList1.ActiveFilterString = "[MarchSales] > 10000";
            treeList1.FocusedNodeChanged += TreeList1_FocusedNodeChanged;

            //Calculate two total summaries against root nodes.
            colMarchSales.SummaryFooter = DevExpress.XtraTreeList.SummaryItemType.Sum;
            colMarchSales.SummaryFooterStrFormat = "Total={0:c0}";
            colMarchSales.AllNodesSummary = false;
            colSeptemberSales.SummaryFooter = DevExpress.XtraTreeList.SummaryItemType.Sum;
            colSeptemberSales.SummaryFooterStrFormat = "Total={0:c0}";
            colSeptemberSales.AllNodesSummary = false;
            treeList1.OptionsView.ShowSummaryFooter = true;

            //Use a 'SpinEdit' in-place editor for the *Sales columns.
            RepositoryItemSpinEdit riSpinEdit = new RepositoryItemSpinEdit();
            riSpinEdit.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            riSpinEdit.DisplayFormat.FormatString = "c0";
            treeList1.RepositoryItems.Add(riSpinEdit);
            colMarchSales.ColumnEdit = riSpinEdit;
            colMarchSalesPrev.ColumnEdit = riSpinEdit;
            colSeptemberSales.ColumnEdit = riSpinEdit;
            colSeptemberSalesPrev.ColumnEdit = riSpinEdit;

            //Apply Excel-style formatting: display predefined 'Up Arrow' and 'Down Arrow' icons based on the unbound column values.
            TreeListFormatRule rule1 = new TreeListFormatRule();
            rule1.Rule = createThreeTrianglesIconSetRule();
            rule1.Column = colUnboundMarchChange;
            rule1.ColumnApplyTo = colMarchSales;
            TreeListFormatRule rule2 = new TreeListFormatRule();
            rule2.Rule = createThreeTrianglesIconSetRule();
            rule2.Column = colUnboundSeptemberChange;
            rule2.ColumnApplyTo = colSeptemberSales;
            treeList1.FormatRules.Add(rule1);
            treeList1.FormatRules.Add(rule2);

            //Do not stretch columns to the treelist width.
            treeList1.OptionsView.AutoWidth = false;


            // Pokud prvek vytvářím v konstruktoru okna, pak musím dát následující řádek, jinak jsou Nodes = prázdné:
            treeList1.ForceInitialize();
            // Když ale konstrukci TreeList provádím v Load eventu, pak to není nutné.

            //Locate a node by a value it contains.
            TreeListNode node1 = treeList1.FindNodeByFieldValue("Region", "North America");
            //Focus and expand this node.
            treeList1.FocusedNode = node1;
            node1.Expanded = true;

            //Locate a node by its key field value and expand it.
            TreeListNode node2 = treeList1.FindNodeByKeyID(32);//Node 'Asia'
            node2.Expand();

            // Pokud prvek vytvářím v konstruktoru okna, pak následující řádek nebude chodit = chyba "Není vytvořen popisovač okna":
            // Když ale konstrukci TreeList provádím v Load eventu, pak následující řádek funguje:
            // Calculate the optimal column widths after the treelist is shown.
            //     this.BeginInvoke(new WF.MethodInvoker(delegate { treeList1.BestFitColumns(); }));

        }

        private void TreeList1_FocusedNodeChanged(object sender, FocusedNodeChangedEventArgs e)
        {
            var node = e.Node;

            TreeListColumn colRegion = _TreeList.Columns["Region"];
            colRegion.OptionsColumn.AllowEdit = (node.Level > 0);

            node.ImageIndex = 2;
        }

        FormatConditionRuleIconSet createThreeTrianglesIconSetRule()
        {
            FormatConditionRuleIconSet ruleIconSet = new FormatConditionRuleIconSet();
            FormatConditionIconSet iconSet = ruleIconSet.IconSet = new FormatConditionIconSet();
            FormatConditionIconSetIcon icon1 = new FormatConditionIconSetIcon();
            FormatConditionIconSetIcon icon2 = new FormatConditionIconSetIcon();
            FormatConditionIconSetIcon icon3 = new FormatConditionIconSetIcon();
            //Choose predefined icons. 
            icon1.PredefinedName = "Triangles3_3.png";
            icon2.PredefinedName = "Triangles3_2.png";
            icon3.PredefinedName = "Triangles3_1.png";
            //Specify the type of threshold values. 
            iconSet.ValueType = FormatConditionValueType.Number;
            //Define ranges to which icons are applied by setting threshold values. 
            icon1.Value = Decimal.MinValue;
            icon1.ValueComparison = FormatConditionComparisonType.GreaterOrEqual;
            icon2.Value = 0;
            icon2.ValueComparison = FormatConditionComparisonType.GreaterOrEqual;
            icon3.Value = 0;
            icon3.ValueComparison = FormatConditionComparisonType.Greater;
            //Add icons to the icon set. 
            iconSet.Icons.Add(icon1);
            iconSet.Icons.Add(icon2);
            iconSet.Icons.Add(icon3);
            return ruleIconSet;
        }

        /*
        private void CreateTreeCombo()
        {
            DevExpress.XtraEditors.TreeComboBoxEdit treeEdit = new DevExpress.XtraEditors.TreeComboBoxEdit() { Bounds = new System.Drawing.Rectangle(20, 15, 350, 25) };

            List<ITreeSelectableItem> rootItems = new List<ITreeSelectableItem>();
            TreeNode item00 = new TreeNode("Kraj Chrudim");
            item00.AddChild("Obec Chrudim");
            item00.AddChild("Obec Hlinsko");
            item00.AddChild("Obec Medlešice");
            item00.AddChild("Obec Slatiňany");
            rootItems.Add(item00);
            TreeNode item01 = new TreeNode("Kraj Chotěboř");
            item01.AddChild("Obec Chotěboř");
            item01.AddChild("Obec Ždírec");
            item01.AddChild("Obec Bílek");
            item01.AddChild("Obec Trhovka");
            item01.AddChild("Obec Ransko");
            rootItems.Add(item01);

            treeEdit.RootItems = new List<ITreeSelectableItem>();
            treeEdit.RootItems.AddRange(rootItems);
           
            this.Controls.Add(treeEdit);
        }
        private class TreeNode : DevExpress.Data.ITreeSelectableItem
        {
            public TreeNode(string text)
            {
                Text = text;
                Children = new List<ITreeSelectableItem>();
            }
            public TreeNode AddChild(string text)
            {
                TreeNode child = new TreeNode(text);
                child.Parent = this;
                this.Children.Add(child);
                return child;
            }
            public string Text { get; set; }
            public TreeNode Parent { get; set; }
            public List<ITreeSelectableItem> Children { get; private set; }
            #region ITreeSelectableItem
            ITreeSelectableItem ITreeSelectableItem.Parent { get { return Parent; } }
            List<ITreeSelectableItem> ITreeSelectableItem.Children { get { return Children; } }
            bool ITreeSelectableItem.AllowSelect { get { return true; } }
            string ITreeSelectableItem.Text { get { return Text; } }
            #endregion
        }
        */
        #endregion
    }

    public class DxTreeViewListSimple : DevExpress.XtraTreeList.TreeList
    {
        public DxTreeViewListSimple()
        {
            this._NodesId = new Dictionary<int, NodeItemInfo>();
            this._NodesKey = new Dictionary<string, NodeItemInfo>();
            this._TreeNodesId = new Dictionary<int, TreeListNode>();
            this.InitTreeView();
        }
        protected void InitTreeView()
        { 
            this.OptionsBehavior.PopulateServiceColumns = false;
            this._MainColumn = new TreeListColumn() { Name = "MainColumn", Visible = true, Width = 150, UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.String, Caption = "Sloupec1", AllowIncrementalSearch=true, FieldName = "Text", ShowButtonMode = ShowButtonModeEnum.ShowForFocusedRow, ToolTip = "Tooltip pro sloupec" };
            this.Columns.Add(this._MainColumn);

            this._MainColumn.OptionsColumn.AllowEdit = false;
            this._MainColumn.OptionsColumn.AllowSort = false;

            this.OptionsBehavior.AllowExpandOnDblClick = true;
            this.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsBehavior.Editable = true;
            this.OptionsBehavior.EditingMode = TreeListEditingMode.Inplace;
            this.OptionsBehavior.EditorShowMode = TreeListEditorShowMode.MouseUp;             // Kdy se zahájí editace (kurzor)? MouseUp: docela hezké; MouseDownFocused: po MouseDown ve stavu Focused (až na druhý klik)
            this.OptionsBehavior.ShowToolTips = true;
            this.OptionsBehavior.SmartMouseHover = true;

            this.OptionsBehavior.AllowExpandAnimation = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsBehavior.AutoNodeHeight = true;
            this.OptionsBehavior.AutoSelectAllInEditor = true;
            this.OptionsBehavior.CloseEditorOnLostFocus = true;

            this.OptionsNavigation.AutoMoveRowFocus = true;
            this.OptionsNavigation.EnterMovesNextColumn = false;
            this.OptionsNavigation.MoveOnEdit = false;
            this.OptionsNavigation.UseTabKey = false;

            this.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.OptionsSelection.EnableAppearanceHotTrackedRow = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsSelection.InvertSelection = true;




            // this.Appearance.Row.FontSizeDelta = 1;
            // this.Appearance.HotTrackedRow.FontSizeDelta = -1;

            // this.CreateAppearances();
            // this.Appearance.Row.FontSizeDelta = 1;
            // this.Appearance.SelectedRow.GradientMode = DW.Drawing2D.LinearGradientMode.Horizontal;

            this.ViewStyle = TreeListViewStyle.TreeView;

            this.NodeCellStyle += _OnNodeCellStyle;
            this.DoubleClick += DxTreeViewListSimple_DoubleClick;
            this.KeyDown += DxTreeViewListSimple_KeyDown;
            this.KeyUp += DxTreeViewListSimple_KeyUp;
            this.ValidatingEditor += DxTreeViewListSimple_ValidatingEditor;
            this.FocusedNodeChanged += _OnFocusedNodeChanged;
            this.BeforeExpand += _OnBeforeExpand;
        }

        private void DxTreeViewListSimple_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                var node = this.FocusedNode;
                if (node != null && node.HasChildren && !node.Expanded)
                {
                    node.Expanded = true;
                    e.Handled = true;
                }
            }
            if (e.KeyCode == Keys.Left)
            {
                var node = this.FocusedNode;
                if (node != null)
                {
                    if (node.HasChildren && node.Expanded)
                    {
                        node.Expanded = false;
                        e.Handled = true;
                    }
                    else if (node.ParentNode != null)
                    {
                        this.FocusedNode = node.ParentNode;
                        e.Handled = true;
                    }
                }
            }
        }

        private void DxTreeViewListSimple_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            { }
        }

        private void DxTreeViewListSimple_DoubleClick(object sender, EventArgs e)
        {
            
        }

        private void DxTreeViewListSimple_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {

            var x = this.EditingValue;
            var y = this.FocusedNode;
            var z = this.FocusedNodeInfo;


        }

        public NodeItemInfo FocusedNodeInfo
        {
            get
            {
                int? nodeId = this.FocusedNode?.Id;
                if (nodeId.HasValue && _NodesId.TryGetValue(nodeId.Value, out NodeItemInfo nodeInfo)) return nodeInfo;
                return null;
            }
        }

        DevExpress.XtraTreeList.Columns.TreeListColumn _MainColumn;
        private Dictionary<int, NodeItemInfo> _NodesId;
        private Dictionary<string, NodeItemInfo> _NodesKey;
        private Dictionary<int, TreeListNode> _TreeNodesId;

        private void _OnBeforeExpand(object sender, BeforeExpandEventArgs args)
        {
            NodeItemInfo nodeInfo = args.Node.Tag as NodeItemInfo;
            if (nodeInfo == null) return;

            if (!nodeInfo.HasChildsOnServer) return;

        }
        private void _OnNodeCellStyle(object sender, GetCustomNodeCellStyleEventArgs args)
        {
            NodeItemInfo nodeInfo = args.Node.Tag as NodeItemInfo;
            if (nodeInfo == null) return;

            if (nodeInfo.FontSizeDelta.HasValue)
                args.Appearance.FontSizeDelta = nodeInfo.FontSizeDelta.Value;
            if (nodeInfo.FontStyleDelta.HasValue)
                args.Appearance.FontStyleDelta = nodeInfo.FontStyleDelta.Value;
            if (nodeInfo.BackColor.HasValue)
            {
                args.Appearance.BackColor = nodeInfo.BackColor.Value;
                args.Appearance.Options.UseBackColor = true;
            }
            if (nodeInfo.ForeColor.HasValue)
            {
                args.Appearance.ForeColor = nodeInfo.ForeColor.Value;
                args.Appearance.Options.UseForeColor = true;
            }
        }

        private void _OnFocusedNodeChanged(object sender, FocusedNodeChangedEventArgs args)
        {
            bool editable = false;
            if (args.Node.Tag is NodeItemInfo node) editable = node.Editable;
            _MainColumn.OptionsColumn.AllowEdit = editable;
        }

        /// <summary>
        /// Funkce, která pro název ikony vrátí její index v ImageListu
        /// </summary>
        public Func<string, int> ImageIndexSearcher { get; set; }

        public void AddNodes(IEnumerable<NodeItemInfo> nodes)
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();

            foreach (var node in nodes)
                this._AddNode(node);

            foreach (var node in nodes.Where(n => n.Expanded))
                this._TreeNodesId[node.NodeId].Expanded = true;

            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        public void AddNode(NodeItemInfo node)
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            this._AddNode(node);
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        private void _AddNode(NodeItemInfo node)
        {
            int parentId = _GetNodeId(node.ParentNodeKey);
            var treeNode = this.AppendNode(new object[] { node.Text }, parentId);
            int nodeId = treeNode.Id;
            ((IItemId)node).Id = nodeId;

            treeNode.Tag = node;
            // treeNode.Expanded = node.Expanded;                  // Nyní nemá význam

            int imageIndex = GetImageIndex(node.ImageName);
            int selectImageIndex = (!String.IsNullOrEmpty(node.ImageNameSelected) ? GetImageIndex(node.ImageNameSelected) : -1);
            if (selectImageIndex < 0) selectImageIndex = imageIndex;
            treeNode.ImageIndex = imageIndex;
            treeNode.SelectImageIndex = selectImageIndex;

            // treeNode.StateImageIndex = -1;  // nodeId + 2;      // Zatím nepoužívám druhou ikonu, používám ImageIndex, ta se nechá změnit podle Selected stavu

            // treeNode.TreeList.ToolTipController = new DevExpress.Utils.ToolTipController();
            this._AddNodeId(node, treeNode);
            
        }
        /// <summary>
        /// Vrací index image pro dané jméno
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private int GetImageIndex(string imageName)
        {
            return (ImageIndexSearcher != null ? ImageIndexSearcher(imageName) : -1);
        }

        public new void ClearNodes()
        {
            foreach (var treeNode in this._TreeNodesId.Values)
                treeNode.Tag = null;
            this._TreeNodesId.Clear();

            base.ClearNodes();

            foreach (var nodeItemInfo in this._NodesId.Values)
                ((IItemId)nodeItemInfo).Id = -1;
            this._NodesId.Clear();

            this._NodesKey.Clear();
        }
        private void _AddNodeId(NodeItemInfo node, TreeListNode treeNode)
        {
            int nodeId = node.NodeId;
            if (!this._NodesId.ContainsKey(nodeId)) this._NodesId.Add(nodeId, node);

            string nodeKey = node.NodeKey;
            if (nodeKey != null)
            {
                if (this._NodesKey.ContainsKey(nodeKey))
                    throw new ArgumentException($"Node with key '{nodeKey}' already exists in {this.GetType().FullName}");

                this._NodesKey.Add(nodeKey, node);
            }

            _TreeNodesId.Add(nodeId, treeNode);
        }
        private int _GetNodeId(string key)
        {
            if (key != null && this._NodesKey.TryGetValue(key, out var node)) return node.NodeId;
            return -1;
        }
    }

    public class NodeItemInfo : IItemId
    {
        public NodeItemInfo(string nodeKey, string parentNodeKey, string text,
            bool editable = false, bool expanded = false, bool hasChildsOnServer = false,
            string imageName = null, string imageNameSelected = null, string toolTipTitle = null, string toolTipText = null,
            int? fontSizeDelta = null, DW.FontStyle? fontStyleDelta = null, DW.Color? backColor = null, DW.Color? foreColor = null)
        {
            this.NodeId = -1;
            this.NodeKey = nodeKey;
            this.ParentNodeKey = parentNodeKey;
            this.Text = text;
            this.Editable = editable;
            this.Expanded = expanded;
            this.HasChildsOnServer = hasChildsOnServer;
            this.ImageName = imageName;
            this.ImageNameSelected = imageNameSelected;
            this.ToolTipTitle = toolTipTitle;
            this.ToolTipText = toolTipText;
            this.FontSizeDelta = fontSizeDelta;
            this.FontStyleDelta = fontStyleDelta;
            this.BackColor = backColor;
            this.ForeColor = foreColor;
        }
        public override string ToString()
        {
            return this.Text;
        }
        public int NodeId { get; private set; }
        public string NodeKey { get; private set; }
        public string ParentNodeKey { get; private set; }
        public string Text { get; private set; }
        public string ImageName { get; set; }
        public string ImageNameSelected { get; set; }
        public bool Editable { get; set; }
        public bool Expanded { get; set; }
        public bool HasChildsOnServer { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipText { get; set; }
        public int? FontSizeDelta { get; set; }
        public DW.FontStyle? FontStyleDelta { get; set; }
        public DW.Color? BackColor { get; set; }
        public DW.Color? ForeColor { get; set; }
        int IItemId.Id { get { return NodeId; } set { NodeId = value; } }
        public static List<NodeItemInfo> CreateSampleList()
        {
            List<NodeItemInfo> list = new List<NodeItemInfo>();
            list.Add(new NodeItemInfo("Rel:10112:L", null, "Objednávky", false, true, imageName: "book_2_16", toolTipTitle: "Objednávky", toolTipText: "Vztah 10112 zleva: Objednávky (N:M)", fontStyleDelta: DW.FontStyle.Bold));
            list.Add(new NodeItemInfo("Rec:210.658001", "Rel:10112:L", "DO-2014-0025", true, imageName: "db_16", toolTipTitle: "Objednávka od dodavatele", toolTipText: "DO-2014-0025, Novák Jan, Chotěboř"));
            list.Add(new NodeItemInfo("Rec:210.658002", "Rel:10112:L", "DO-2018-0198", true, imageName: "db_16", toolTipTitle: "Objednávka od dodavatele", toolTipText: "DO-2018-0198, Solnička Vladimír, Žleby"));
            list.Add(new NodeItemInfo("New:10112:L", "Rel:10112:L", "...nový vztah", true, imageName: "edit_add_4_16", toolTipTitle: "Objednávka od dodavatele", toolTipText: "Zadejte referenci nového záznamu", fontStyleDelta: DW.FontStyle.Italic));
            list.Add(new NodeItemInfo("Rel:10260:L", null, "Poptávky", false, true, imageName: "book_2_16", fontStyleDelta: DW.FontStyle.Bold));
            list.Add(new NodeItemInfo("Rec:211.658012", "Rel:10260:L", "OP-2019-VCK0115", true, toolTipTitle: "Poptávka od zákazníka", toolTipText: "OP-2019-VCK0115, Jeřábková Jindřiška, Jistebnice"));
            list.Add(new NodeItemInfo("New:10260:L", "Rel:10260:L", "...nový vztah", true, imageName: "edit_add_4_16", toolTipTitle: "Objednávka od dodavatele", toolTipText: "Zadejte referenci nového záznamu", fontStyleDelta: DW.FontStyle.Italic));
            list.Add(new NodeItemInfo("Rel:11520:L", null, "Příjemky", false, false, imageName: "book_2_16"));
            list.Add(new NodeItemInfo("New:11520:L", "Rel:11520:L", "...nový vztah", true, imageName: "edit_add_4_16", fontStyleDelta: DW.FontStyle.Italic));
            list.Add(new NodeItemInfo("Rel:11560:L", null, "Výdejky", false, false, imageName: "book_2_16"));
            list.Add(new NodeItemInfo("New:11560:L", "Rel:11560:L", "...nový vztah", true, imageName: "edit_add_4_16", fontStyleDelta: DW.FontStyle.Italic));
            list.Add(new NodeItemInfo("Rel:12560:L", null, "OnDemandLoad", false, false, imageName: "book_2_16"));
            list.Add(new NodeItemInfo("Load:12560:L", "Rel:12560:L", "Načítám záznamy...", false, imageName: "hourglass_16", fontStyleDelta: DW.FontStyle.Italic));

            /*
            list.Add(new NodeItemInfo(5,0,));
            list.Add(new NodeItemInfo(6,5,));
            list.Add(new NodeItemInfo(7,5,));
            list.Add(new NodeItemInfo(8,5,));
            list.Add(new NodeItemInfo(9,5,));
            list.Add(new NodeItemInfo(10,0,));
            list.Add(new NodeItemInfo(11,10,));
            list.Add(new NodeItemInfo(12,10,));
            list.Add(new NodeItemInfo(13,10,));
            list.Add(new NodeItemInfo(14,10,));
            list.Add(new NodeItemInfo(15,10,));
            list.Add(new NodeItemInfo(16,0,));
            list.Add(new NodeItemInfo(17,0,));
            list.Add(new NodeItemInfo(18,17,));
            list.Add(new NodeItemInfo(19,0,));
            list.Add(new NodeItemInfo(20,19,));
            list.Add(new NodeItemInfo(21,0,));
            list.Add(new NodeItemInfo(22,21,));
            */
            return list;
        }
    }
    public interface IItemId
    {
        int Id { get; set; }
    }







    public class SalesData
    {
        static int UniqueID = 37;
        public SalesData()
        {
            ID = UniqueID++;
        }
        public SalesData(int id, int regionId, string region, decimal marchSales, decimal septemberSales, decimal marchSalesPrev, decimal septermberSalesPrev, double marketShare)
        {
            ID = id;
            RegionID = regionId;
            Region = region;
            MarchSales = marchSales;
            SeptemberSales = septemberSales;
            MarchSalesPrev = marchSalesPrev;
            SeptemberSalesPrev = septermberSalesPrev;
            MarketShare = marketShare;
        }
        public int ID { get; set; }
        public int RegionID { get; set; }
        public string Region { get; set; }
        public decimal MarchSales { get; set; }
        public decimal SeptemberSales { get; set; }
        public decimal MarchSalesPrev { get; set; }
        public decimal SeptemberSalesPrev { get; set; }

        // [DisplayFormat(DataFormatString = "p0")]
        public double MarketShare { get; set; }
    }

    public class SalesDataGenerator
    {
        public static List<SalesData> CreateData()
        {
            List<SalesData> sales = new List<SalesData>();
            sales.Add(new SalesData(0, -1, "Western Europe", 30540, 33000, 32220, 35500, .70));
            sales.Add(new SalesData(1, 0, "Austria", 22000, 28000, 26120, 28500, .92));
            sales.Add(new SalesData(2, 0, "France", 23020, 27000, 20120, 29200, .51));
            sales.Add(new SalesData(3, 0, "Germany", 30540, 33000, 32220, 35500, .93));
            sales.Add(new SalesData(4, 0, "Spain", 12900, 10300, 14300, 9900, .82));
            sales.Add(new SalesData(5, 0, "Switzerland", 9323, 10730, 7244, 9400, .14));
            sales.Add(new SalesData(6, 0, "United Kingdom", 14580, 13967, 15200, 16900, .91));

            sales.Add(new SalesData(17, -1, "Eastern Europe", 22500, 24580, 21225, 22698, .62));
            sales.Add(new SalesData(18, 17, "Belarus", 7315, 18800, 8240, 17480, .34));
            sales.Add(new SalesData(19, 17, "Bulgaria", 6300, 2821, 5200, 10880, .8));
            sales.Add(new SalesData(20, 17, "Croatia", 4200, 3890, 3880, 4430, .29));
            sales.Add(new SalesData(21, 17, "Russia", 22500, 24580, 21225, 22698, .85));

            sales.Add(new SalesData(26, -1, "North America", 31400, 32800, 30300, 31940, .84));
            sales.Add(new SalesData(27, 26, "USA", 31400, 32800, 30300, 31940, .87));
            sales.Add(new SalesData(28, 26, "Canada", 25390, 27000, 5200, 29880, .64));

            sales.Add(new SalesData(29, -1, "South America", 16380, 17590, 15400, 16680, .32));
            sales.Add(new SalesData(30, 29, "Argentina", 16380, 17590, 15400, 16680, .88));
            sales.Add(new SalesData(31, 29, "Brazil", 4560, 9480, 3900, 6100, .10));

            sales.Add(new SalesData(32, -1, "Asia", 20388, 22547, 22500, 25756, .52));
            sales.Add(new SalesData(34, 32, "India", 4642, 5320, 4200, 6470, .44));
            sales.Add(new SalesData(35, 32, "Japan", 9457, 12859, 8300, 8733, .70));
            sales.Add(new SalesData(36, 32, "China", 20388, 22547, 22500, 25756, .82));
            return sales;
        }
    }
}
