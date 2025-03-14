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

using DComponents = DjSoft.App.iCollect.Components;
using DRibbon = DjSoft.App.iCollect.Components.Ribbon;

namespace DjSoft.App.iCollect
{
    public class MainAppForm : DComponents.DjMainPanelRibbonForm
    {
        public MainAppForm()
        {
            Data.ApplicationState.DesktopForm = this;
            this.Text = "Sbíráme...";
            this.IconOptions.Icon = Properties.Resources.gpe_tetris_48ico;
            this.Size = new System.Drawing.Size(1200, 600);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Data.ApplicationState.DesktopFormState = Data.ApplicationFormStateType.Closed;
        }
        protected override void OnRibbonPrepare()
        {
            base.OnRibbonPrepare();

            DjRibbon.RibbonStyle = XRibbon.RibbonControlStyle.Office365;
            DjRibbon.CommandLayout = XRibbon.CommandLayout.Classic;

            var pageHome = DjRibbon.AddPage("Home", "Sbírka");
            var groupHomeLayout = pageHome.AddGroup("ViewType", "Zobrazení");
            groupHomeLayout.AddItem(DRibbon.DjRibbonItemType.Button, "Table", "Tabulka", "Zobrazení", "Tabulka se sloupci a řádky", null);
            groupHomeLayout.AddItem(DRibbon.DjRibbonItemType.Button, "Cards", "Kartotéka", "Zobrazení", "Kartotéka s jednotlivými záznamy", null);

            var collections = DjSoft.App.iCollect.Collect.CollectionSet.Collections;

            var groupHomeSchema = pageHome.AddGroup("Collect", "Správa sbírky");
            var createCollectionButton = groupHomeSchema.AddItem(DRibbon.DjRibbonItemType.Button, "Select", "Vytvoř sbírku", "Vytvoř sbírku", "Neexistuje žádná sbírka. Vytvořte první tímto tlačítkem", Properties.Resources.applications_office_2_32);
            createCollectionButton.Visible = true;
            var selectCollectionButton = groupHomeSchema.AddItem(DRibbon.DjRibbonItemType.Menu, "Select", "Vyber sbírku", "Vyber sbírku", "Zadat evidované prvky", Properties.Resources.applications_office_2_32, null, i => FillCollectionsToItem(i, collections));
            selectCollectionButton.Visible = true;
            groupHomeSchema.AddItem(DRibbon.DjRibbonItemType.Button, "Schema", "Nastavit schema", "Schema", "Zadat evidované prvky", Properties.Resources.applications_office_3_32);

            var pageAppl = DjRibbon.AddPage("Appl", "Program");
            var groupHomeSetting = pageAppl.AddGroup("HomeSetting", "Nastavení");
            groupHomeSetting.AddItem(DRibbon.DjRibbonItemType.SkinDropDownButton);
            groupHomeSetting.AddItem(DRibbon.DjRibbonItemType.SkinPaletteDropDownButton);


        }

        protected void FillCollectionsToItem(DRibbon.IDjRibbonItem item, Collect.Collection[] collections)
        {
            if (item is DRibbon.DjRibbonMenuButton barMenu)
                barMenu.AddItems(collections.Select(c => createBarItem(c)).ToArray());

            XBars.BarItem createBarItem(Collect.Collection collection)
            {
                return DRibbon.DjRibbonControl.CreateBarItem(DRibbon.DjRibbonItemType.Button, null, collection.Caption, collection.Caption, collection.Description, collection.Image, XRibbon.RibbonItemStyles.SmallWithText) as XBars.BarItem;
            }
        }

        protected override string PositionConfigName { get { return this.GetType().Name; } }
        protected override void OnContentPrepare()
        {
            base.OnContentPrepare();

            __SplitPanel = new XEditors.SplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 500, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True };
            MainPanel.Controls.Add(__SplitPanel);

            __Grid = new DevExpress.XtraGrid.GridControl() { Dock = DockStyle.Fill };
            __CardView = new DevExpress.XtraGrid.Views.Card.CardView(__Grid);
            __Grid.MainView = __CardView;

            __SplitPanel.Panel1.Controls.Add(__Grid);

            __GridData = new DataTable();
            __GridData.Columns.Add(new DataColumn() { ColumnName = "id", Caption = "ID prvku", DataType = typeof(int) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "group", Caption = "Skupina", DataType = typeof(string) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "name", Caption = "Celé jméno", DataType = typeof(string) });
            __GridData.Columns.Add(new DataColumn() { ColumnName = "serie", Caption = "Série", DataType = typeof(string) });
            __GridData.Rows.Add(1, "Vanessa Perrin", "Vanessa Perrin in Bloom", "2024 Stilettos");
            __GridData.Rows.Add(2, "Veronique Perrin", "Veronique Perrin On the rise", "2020 W Club exclusive");
            __GridData.Rows.Add(3, "Navia Phan", "Coming Out Navia Phan", "2021 Lotery");
            __GridData.Rows.Add(4, "Vanessa Perrin", "In Bloom Vanessa Perrin", "2007 Exclusive");
            __GridData.Rows.Add(5, "Violaine Perrin", "Sirene Violaine Perrin", "2023 NuFantasy Misc.");

            __Grid.DataSource = __GridData;

            __CardView.OptionsFilter.DefaultFilterEditorView = XEditors.FilterEditorViewMode.VisualAndText;
            __CardView.OptionsFilter.UseNewCustomFilterDialog = true;
            __CardView.OptionsView.ShowQuickCustomizeButton = false;
            __CardView.OptionsView.FilterCriteriaDisplayStyle = XEditors.FilterCriteriaDisplayStyle.Visual;
            __CardView.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.True;
            __CardView.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.True;
            __CardView.OptionsBehavior.AllowExpandCollapse = false;
            __CardView.OptionsBehavior.AutoFocusNewCard = true;
            __CardView.OptionsBehavior.UseTabKey = true;

            
            __CardView.CardWidth = 350;
            __CardView.ShowFindPanel();

        }
        private DevExpress.XtraEditors.SplitContainerControl __SplitPanel;
        private DevExpress.XtraGrid.GridControl __Grid;
        private DevExpress.XtraGrid.Views.Card.CardView __CardView;
        private System.Data.DataTable __GridData;
    }
}
