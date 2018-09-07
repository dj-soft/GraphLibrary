using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class TestScheduler
    {
        [TestMethod]
        public void TestData()
        {
            GuiData data = new GuiData();

            data.ToolbarItems.Add(new GuiToolbarItem() { Name = "tlbSave", Title = "Uložit", ToolTip = "Uložit všechna data" });
            data.ToolbarItems.Add(new GuiToolbarItem() { Name = "tlbReload", Title = "Přenačíst", ToolTip = "Znovu načíst všechna data" });

            GuiPage page = new GuiPage() { Name = "pageMain", Title = "Dílna 1", ToolTip = "Zobrazuje veškerá data první dílny" };
            data.Pages.Add(page);

            GuiGrid taskGrid = new GuiGrid() { Name = "taskGrid", Title = "Pracovní úkoly", ToolTip = "Zobrazuje úkoly, které se mají na této dílně provádět" };
            GuiTable taskRows = new GuiTable();
            taskRows.DataTable = WorkSchedulerSupport.CreateTable("taskRows", "record_id int, reference string, nazev string, constr_element string");
            taskGrid.Rows = taskRows;
            page.LeftPanel.Grids.Add(taskGrid);

            GuiGrid workGrid = new GuiGrid() { Name = "workGrid", Title = "Pracovní rozvrh", ToolTip = "Zobrazuje prostor dílny a její využití" };
            GuiTable workRows = new GuiTable();
            workRows.DataTable = WorkSchedulerSupport.CreateTable("workRows", "record_id int, reference string, nazev string, constr_element string");
            workRows.DataTable.Rows.Add(1, "Refer 1", "Název 1", "Výrobek A");
            workRows.DataTable.Rows.Add(2, "Referen 2", "Náz 2", "Výrobek B");
            workRows.DataTable.Rows.Add(3, "Reference 3", "N 3", "Výrobek C");
            workGrid.Rows = workRows;
            workGrid.GraphProperties.AxisResizeMode = Components.AxisResizeContentMode.ChangeScale;
            workGrid.GraphProperties.InteractiveChangeMode = Components.AxisInteractiveChangeMode.Shift;
            page.MainPanel.Grids.Add(workGrid);

            string serial = Data.Persist.Serialize(data);

            object deserial = Data.Persist.Deserialize(serial);
        }
    }
}
