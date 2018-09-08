using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    /// <summary>
    /// Třída obsahující testy prvků v Scheduleru
    /// </summary>
    [TestClass]
    public class TestScheduler
    {
        /// <summary>
        /// Test sestavení, serializace a deserializace balíku dat <see cref="GuiData"/>
        /// </summary>
        [TestMethod]
        public void TestGuiData()
        {
            GuiData data = new GuiData() { Name = "Data" };

            data.ToolbarItems.Add(new GuiToolbarItem() { Name = "tlbSave", Title = "Uložit", ToolTip = "Uložit všechna data" });
            data.ToolbarItems.Add(new GuiToolbarItem() { Name = "tlbReload", Title = "Přenačíst", ToolTip = "Znovu načíst všechna data" });

            data.ContextMenuItems.Add(new GuiContextMenuItem() { Name = "cnxAddWork", Title = "Přidá další práci", ToolTip = "Přidá další práci v označeném místě" });

            GuiPage page = new GuiPage() { Name = "pageMain", Title = "Dílna 1", ToolTip = "Zobrazuje veškerá data první dílny" };
            data.Pages.Add(page);

            GuiGrid taskGrid = new GuiGrid() { Name = "taskGrid", Title = "Pracovní úkoly", ToolTip = "Zobrazuje úkoly, které se mají na této dílně provádět" };
            GuiTable taskRows = new GuiTable() { Name = "taskRows" };
            taskRows.DataTable = WorkSchedulerSupport.CreateTable("taskRows", "record_id int, reference string, nazev string, constr_element string");
            taskGrid.Rows = taskRows;
            page.LeftPanel.Grids.Add(taskGrid);

            GuiGrid workGrid = new GuiGrid() { Name = "workGrid", Title = "Pracovní rozvrh", ToolTip = "Zobrazuje prostor dílny a její využití" };
            GuiTable workRows = new GuiTable() { Name = "workRows" };
            workRows.DataTable = WorkSchedulerSupport.CreateTable("workRows", "record_id int, reference string, nazev string, constr_element string");
            workRows.DataTable.Rows.Add(1, "Refer 1", "Název 1", "Výrobek A");
            workRows.DataTable.Rows.Add(2, "Referen 2", "Náz 2", "Výrobek B");
            workRows.DataTable.Rows.Add(3, "Reference 3", "N 3", "Výrobek C");
            workGrid.Rows = workRows;
            workGrid.GraphProperties.AxisResizeMode = Components.AxisResizeContentMode.ChangeScale;
            workGrid.GraphProperties.InteractiveChangeMode = Components.AxisInteractiveChangeMode.Shift;
            page.MainPanel.Grids.Add(workGrid);

            data.Finalise();

            IGuiBase item1 = data.FindByFullName(@"Data\toolBar\tlbSave");
            IGuiBase item2 = data.FindByFullName(@"Data\contextMenu\cnxAddWork");
            IGuiBase item3 = data.FindByFullName(@"Data\pageMain\mainPanel\workGrid\");


            string serial = Data.Persist.Serialize(data);
            object deserial = Data.Persist.Deserialize(serial);
            if (deserial == null)
                throw new AssertFailedException("Deserializovaný objekt je null.");

            GuiData clone = deserial as GuiData;
            if (clone == null)
                throw new AssertFailedException("Deserializovaný objekt není odpovídající třídy GuiData.");

            if (clone.ToolbarItems.Count != data.ToolbarItems.Count)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající počet prvků v ToolbarItems, má být "+ data.ToolbarItems.Count + "; je " + clone.ToolbarItems.Count + ".");

            if (clone.ToolbarItems.Items[0].Name != data.ToolbarItems.Items[0].Name)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající Name prvku ToolbarItems[0], má být " + data.ToolbarItems.Items[0].Name + "; je " + clone.ToolbarItems.Items[0].Name + ".");

            if (clone.Pages.Count != data.Pages.Count)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající počet prvků v Pages, má být " + data.Pages.Count + "; je " + clone.Pages.Count + ".");

            if (clone.Pages[0].Name != data.Pages[0].Name)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající Title prvku Pages[0], má být " + data.Pages[0].Title + "; je " + clone.Pages[0].Title + ".");

            if (clone.Pages[0].LeftPanel.Grids.Count != data.Pages[0].LeftPanel.Grids.Count)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající počet prvků v Pages[0].LeftPanel.Grids, má být " + data.Pages[0].LeftPanel.Grids.Count + "; je " + clone.Pages[0].LeftPanel.Grids.Count + ".");

            string taskGridText = taskGrid.ToString();
            string taskClonText = clone.Pages[0].LeftPanel.Grids[0].ToString();
            if (taskClonText != taskGridText)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající obsah v TaskGridu, má být " + taskGridText + "; je " + taskClonText + ".");

            if (clone.Pages[0].MainPanel.Grids.Count != data.Pages[0].MainPanel.Grids.Count)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající počet prvků v Pages[0].MainPanel.Grids, má být " + data.Pages[0].MainPanel.Grids.Count + "; je " + clone.Pages[0].MainPanel.Grids.Count + ".");

            string workGridText = workGrid.ToString();
            string workClonText = clone.Pages[0].MainPanel.Grids[0].ToString();
            if (workClonText != workGridText)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje odpovídající obsah v WorkGridu, má být " + workGridText + "; je " + workClonText + ".");





        }
    }
}
