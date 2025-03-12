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

namespace DjSoft.App.iCollect
{
    public class MainAppForm : DComponents.DjTabbedRibbonForm
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
            groupHomeLayout.AddItem(DComponents.Ribbon.DjRibbonItemType.Button, "Table", "Tabulka", "Zobrazení", "Tabulka se sloupci a řádky", null);
            groupHomeLayout.AddItem(DComponents.Ribbon.DjRibbonItemType.Button, "Cards", "Kartotéka", "Zobrazení", "Kartotéka s jednotlivými záznamy", null);

            var groupHomeSchema = pageHome.AddGroup("Collect", "Správa sbírky");
            groupHomeSchema.AddItem(DComponents.Ribbon.DjRibbonItemType.Button, "Select", "Vyber sbírku", "Vyber sbírku", "Zadat evidované prvky", Properties.Resources.applications_office_2_32);
            groupHomeSchema.AddItem(DComponents.Ribbon.DjRibbonItemType.Button, "Schema", "Nastavit schema", "Schema", "Zadat evidované prvky", Properties.Resources.applications_office_3_32);

            var pageAppl = DjRibbon.AddPage("Appl", "Program");
            var groupHomeSetting = pageAppl.AddGroup("HomeSetting", "Nastavení");
            groupHomeSetting.AddItem(DComponents.Ribbon.DjRibbonItemType.SkinDropDownButton);
            groupHomeSetting.AddItem(DComponents.Ribbon.DjRibbonItemType.SkinPaletteDropDownButton);

        }

        protected override string PositionConfigName { get { return this.GetType().Name; } }
        protected override void OnContentPrepare()
        {
            base.OnContentPrepare();

            __Grid = new DevExpress.XtraGrid.GridControl() { Dock = DockStyle.Fill };
            __CardView = new DevExpress.XtraGrid.Views.Card.CardView(__Grid);
            __Grid.MainView = __CardView;
            MainPanel.Controls.Add(__Grid);

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
