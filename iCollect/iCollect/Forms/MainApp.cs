using DevExpress.XtraBars.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using XBars = DevExpress.XtraBars;
using XRibbon = DevExpress.XtraBars.Ribbon;
using XEditors = DevExpress.XtraEditors;

namespace DjSoft.App.iCollect
{
    public partial class MainApp : XRibbon.RibbonForm
    {
        #region Windows Form Designer generated code
        public MainApp()
        {
            InitializeComponent();
            
        }
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this._MainPanelPrepare();
            this._RibbonPrepare();
            this._StatusPrepare();
            this._ContentPrepare();
            this.Text = "Sbíráme...";
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        #endregion
        #region Ribbon
        private void _RibbonPrepare()
        {
            __Ribbon = new XRibbon.RibbonControl() { Visible = true, Dock = DockStyle.Top, CommandLayout = XRibbon.CommandLayout.Simplified,  ButtonGroupsLayout = DevExpress.XtraBars.ButtonGroupsLayout.TwoRows };
            __Ribbon.AllowMdiChildButtons = false;
            __Ribbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            __Ribbon.ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;
            __Ribbon.OptionsExpandCollapseMenu.EnableExpandCollapseMenu = DevExpress.Utils.DefaultBoolean.False;

            __PageHome = new XRibbon.RibbonPage() { Name = "Home", Text = "Sbírka" };
            __GroupLayout = new XRibbon.RibbonPageGroup() { Text = "Vzhled" };
            __GroupLayout.ItemLinks.Add(new XBars.SkinDropDownButtonItem());
            __GroupLayout.ItemLinks.Add(new XBars.SkinPaletteDropDownButtonItem());
            __PageHome.Groups.Add(__GroupLayout);
            __Ribbon.Pages.Add(__PageHome);
            this.Controls.Add(__Ribbon);
        }
        private XRibbon.RibbonControl __Ribbon;
        private XRibbon.RibbonPage __PageHome;
        private XRibbon.RibbonPageGroup __GroupLayout;
        #endregion
        #region MainPanel
        private void _MainPanelPrepare()
        {
            __MainPanel = new XEditors.PanelControl() { Dock = DockStyle.Fill, BackColor = Color.LightBlue, BorderStyle = XEditors.Controls.BorderStyles.Office2003 };
            this.Controls.Add(__MainPanel);
        }
        private XEditors.PanelControl __MainPanel;
        #endregion
        #region Status
        private void _StatusPrepare()
        {
            __Status = new XRibbon.RibbonStatusBar() { Visible = true, Ribbon = __Ribbon, Dock = DockStyle.Bottom };
            this.Controls.Add(__Status);
        }
        private XRibbon.RibbonStatusBar __Status;
    	#endregion


        private void _ContentPrepare()
        {
            __Grid = new DevExpress.XtraGrid.GridControl() { Dock = DockStyle.Fill };
            __CardView = new DevExpress.XtraGrid.Views.Card.CardView(__Grid);
            __Grid.MainView = __CardView;
            __MainPanel.Controls.Add(__Grid);

            __GridData = new DataTable();
            __GridData.Columns.Add(new DataColumn() { ColumnName = "id", Caption = "ID prvku", DataType = typeof(int) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "group", Caption = "Skupina", DataType = typeof(string) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "name", Caption = "Celé jméno", DataType = typeof(string) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "serie", Caption = "Série", DataType = typeof(string) });
            __GridData.Rows.Add(1, "Vanessa Perrin", "Vanessa Perrin in Bloom", "2024 Stilettos");
            __GridData.Rows.Add(2, "Veronique Perrin", "Veronique Perrin On the rise", "2020 W Club exclusive");
            __GridData.Rows.Add(3, "Navia Phan", "Coming Out Navia Phan", "2021 Lotery");

            __Grid.DataSource = __GridData;
        }
        private DevExpress.XtraGrid.GridControl __Grid;
        private DevExpress.XtraGrid.Views.Card.CardView __CardView;
        private System.Data.DataTable __GridData;
    }
}
