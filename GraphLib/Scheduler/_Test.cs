using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noris.LCS.Base.WorkScheduler;

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
            GuiDataTable taskRows = new GuiDataTable() { Name = "taskRows" };
            taskRows.ClassId = 1363;
            taskRows.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId });
            taskRows.AddColumn(new GuiDataColumn() { Name = "reference", Title = "Reference", Width = 85 });
            taskRows.AddColumn(new GuiDataColumn() { Name = "nazev", Title = "Název", Width = 200 });
            taskRows.AddColumn(new GuiDataColumn() { Name = "constr_element", Title = "Dílec", Width = 200 });
            taskGrid.RowTable = taskRows;
            page.LeftPanel.Grids.Add(taskGrid);

            GuiGrid workGrid = new GuiGrid() { Name = "workGrid", Title = "Pracovní rozvrh", ToolTip = "Zobrazuje prostor dílny a její využití" };
            GuiDataTable workRows = new GuiDataTable() { Name = "workRows" };
            workRows.ClassId = 1817;
            workRows.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId });
            workRows.AddColumn(new GuiDataColumn() { Name = "reference", Title = "Reference", Width = 85 });
            workRows.AddColumn(new GuiDataColumn() { Name = "nazev", Title = "Název", Width = 200 });
            workRows.AddColumn(new GuiDataColumn() { Name = "constr_element", Title = "Dílec", Width = 200 });
            GuiDataRow wr1 = workRows.AddRow(new GuiId(1817, 1), "Refer 1", "Název 1", "Výrobek A");
            wr1.Graph = new GuiGraph();
            wr1.Graph.GraphItems.Add(new GuiGraphItem() { ItemId = new GuiId(1817, 1), RowId = new GuiId(1364, 1), Time = new GuiTimeRange(new DateTime(2018, 8, 1, 12, 0, 0), new DateTime(2018, 8, 1, 16, 0, 0)) });
            wr1.Graph.GraphItems.Add(new GuiGraphItem() { ItemId = new GuiId(1817, 2), RowId = new GuiId(1364, 1), Time = new GuiTimeRange(new DateTime(2018, 8, 1, 16, 0, 0), new DateTime(2018, 8, 1, 20, 0, 0)) });
            wr1.Graph.GraphItems.Add(new GuiGraphItem() { ItemId = new GuiId(1817, 3), RowId = new GuiId(1364, 1), Time = new GuiTimeRange(new DateTime(2018, 8, 1, 21, 0, 0), new DateTime(2018, 8, 1, 22, 0, 0)) });
            GuiDataRow wr2 = workRows.AddRow(new GuiId(1817, 2), "Referen 2", "Náz 2", "Výrobek B");
            GuiDataRow wr3 = workRows.AddRow(new GuiId(1817, 3), "Reference 3", "N 3", "Výrobek C");
            workGrid.RowTable = workRows;
            
            workGrid.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
            workGrid.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;

            GraphItemBehaviorMode graph4BehaviorMode = (GraphItemBehaviorMode.DefaultText | GraphItemBehaviorMode.ResizeTime);
            DateTime graph4Begin = new DateTime(2018, 8, 1, 14, 30, 45, 550);
            DateTime graph4End = new DateTime(2018, 8, 1, 18, 15, 30, 10);
            wr1.Graph.GraphItems.Add(new GuiGraphItem() { ItemId = new GuiId(1817, 4), RowId = new GuiId(1364, 2), Time = new GuiTimeRange(graph4Begin, graph4End), BehaviorMode = graph4BehaviorMode });

            page.MainPanel.Grids.Add(workGrid);

            data.Finalise();

            string guiMainItems = workRows.FullName;
            string guiItem4Path = wr1.Graph.GraphItems[0].FullName;

            IGuiItem item1 = data.FindByFullName(@"Data\toolBar\tlbSave");
            IGuiItem item2 = data.FindByFullName(@"Data\contextMenu\cnxAddWork");
            IGuiItem item3 = data.FindByFullName(@"Data\pageMain\mainPanel\workGrid\");


            string serial = Persist.Serialize(data);
            object deserial = Persist.Deserialize(serial);

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

            GuiGraphTable workItemsB = data.FindByFullName(guiMainItems) as GuiGraphTable;

            GuiGraphItem graphItem4B = data.FindByFullName(guiItem4Path) as GuiGraphItem;
            if (graphItem4B == null)
                throw new AssertFailedException("Deserializovaný objekt neobsahuje grafický prvek 4.");

            if (graphItem4B.BehaviorMode != graph4BehaviorMode)
                throw new AssertFailedException("Deserializovaný grafický prvek 4 nemá správnou hodnotu BehaviorMode, má být " + graph4BehaviorMode + "; je " + graphItem4B.BehaviorMode + ".");
            if (graphItem4B.Time.Begin != graph4Begin)
                throw new AssertFailedException("Deserializovaný grafický prvek 4 nemá správnou hodnotu Begin, má být " + graph4Begin + "; je " + graphItem4B.Time.Begin + ".");
            if (graphItem4B.Time.End != graph4End)
                throw new AssertFailedException("Deserializovaný grafický prvek 4 nemá správnou hodnotu End, má být " + graph4End + "; je " + graphItem4B.Time.End + ".");

        }
    }
}
