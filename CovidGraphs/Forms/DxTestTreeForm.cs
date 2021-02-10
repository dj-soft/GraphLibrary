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

        }
        ImageList _Images16;

        private void CreateTreeView()
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
