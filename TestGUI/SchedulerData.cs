using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    public class SchedulerData : IAppHost
    {
        public SchedulerData()
        {
            this.Rand = new Random();
        }
        protected Random Rand;
        #region Tvorba výchozích dat
        public GuiData CreateGuiData()
        {
            this.MainData = new Noris.LCS.Base.WorkScheduler.GuiData();

            this.CreateProperties();
            this.CreateToolBar();
            this.CreateMainPage();
            this.CreateLeftPanel();
            this.CreateCenterPanel();
            this.CreateRightPanel();
            this.CreateContextFunctions();

            return this.MainData;
        }
        protected void CreateProperties()
        {
            DateTime now = DateTime.Now;
            this.DateTimeNow = now.Date;
            this.DateTimeFirst = new DateTime(now.Year, now.Month, 1);
            this.TimeRangeCurrent = new GuiTimeRange(this.DateTimeNow, this.DateTimeNow.AddDays(7d));
            this.TimeRangeTotal = new GuiTimeRange(this.DateTimeFirst, this.DateTimeFirst.AddMonths(2));

            this.MainData.Properties.InitialTimeRange = this.TimeRangeCurrent;
            this.MainData.Properties.TotalTimeRange = this.TimeRangeTotal;
            this.MainData.Properties.PluginFormBorder = PluginFormBorderStyle.Sizable;
            this.MainData.Properties.PluginFormIsMaximized = true;
            this.MainData.Properties.PluginFormTitle = "Plánovací nářadí";
            this.MainData.Properties.GraphItemMoveSameGraph = GraphItemMoveAlignY.OnOriginalItemPosition;
            this.MainData.Properties.GraphItemMoveOtherGraph = GraphItemMoveAlignY.OnMousePosition;
        }
        protected void CreateToolBar()
        {
            this.MainData.ToolbarItems.ToolbarShowSystemItems = ToolbarSystemItem.Default;
        }
        protected void CreateMainPage()
        {
            this.MainPage = new GuiPage() { Name = "MainPage", Title = "Plánování dílny POLOTOVARY", ToolTip = "Toto je pouze ukázková knihovna" };
            this.MainData.Pages.Add(this.MainPage);
        }
        protected void CreateLeftPanel()
        {

            GuiGrid leftGrid = new GuiGrid() { Name = "GridLeft", Title = "Výrobní příkazy" };

            leftGrid.GridProperties.TagFilterItemHeight = 26;
            leftGrid.GridProperties.TagFilterItemMaxCount = 60;
            leftGrid.GridProperties.TagFilterVisible = true;

            leftGrid.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
            leftGrid.GraphProperties.BottomMarginPixel = 2;
            leftGrid.GraphProperties.GraphLineHeight = 6;
            leftGrid.GraphProperties.GraphLinePartialHeight = 24;
            leftGrid.GraphProperties.GraphPosition = DataGraphPositionType.OnBackgroundLogarithmic;
            leftGrid.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;
            leftGrid.GraphProperties.LogarithmicGraphDrawOuterShadow = 0.15f;
            leftGrid.GraphProperties.LogarithmicRatio = 0.60f;
            leftGrid.GraphProperties.Opacity = 192;
            leftGrid.GraphProperties.TableRowHeightMax = 28;
            leftGrid.GraphProperties.TableRowHeightMin = 22;
            leftGrid.GraphProperties.TimeAxisMode = TimeGraphTimeAxisMode.LogarithmicScale;
            leftGrid.GraphProperties.UpperSpaceLogical = 1f;
            
            DataTable rowTable = WorkSchedulerSupport.CreateTable("LeftRows", "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, qty decimal");
            leftGrid.Rows = new GuiTable() { Name = "GridLeft", DataTable = rowTable };
            leftGrid.Rows.ColumnsExtendedInfo[0].ClassNumber = 1188;
            leftGrid.Rows.ColumnsExtendedInfo[0].BrowseColumnType = BrowseColumnType.SubjectNumber;
            leftGrid.Rows.ColumnsExtendedInfo[1].PrepareDataColumn("Číslo", 120, true, null, true);
            leftGrid.Rows.ColumnsExtendedInfo[2].PrepareDataColumn("Dílec", 240, true, null, true);
            leftGrid.Rows.ColumnsExtendedInfo[3].PrepareDataColumn("Množství", 60, true, null, true);

            // Data tabulky:
            this.CreateLeftPanelRow(leftGrid, 10001, "VP10001", "Židle lakovaná červená", 12, "židle", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10002, "VP10002", "Stůl konferenční", 3, "stoly", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10003, "VP10003", "Stolička třínohá", 8, "židle", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10004, "VP10004", "Sedátko chodbové", 4, "židle", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10005, "VP10005", "Židle lakovaná přírodní", 6, "židle", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10006, "VP10006", "Stůl pracovní", 3, "stoly", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10007, "VP10007", "Taburet ozdobný", 9, "židle", ProductTpv.Luxus);
            this.CreateLeftPanelRow(leftGrid, 10008, "VP10008", "Skříňka na klíče", 24, "skříně", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10009, "VP10009", "Podstavec pod televizi", 12, "jiné", ProductTpv.Luxus);
            this.CreateLeftPanelRow(leftGrid, 10010, "VP10010", "Botník krátký", 6, "skříně", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10011, "VP10011", "Skříň šatní široká", 3, "skříně", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10012, "VP10012", "Stolek HiFi věže", 4, "jiné", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10013, "VP10013", "Polička na CD", 16, "jiné", ProductTpv.Luxus);
            this.CreateLeftPanelRow(leftGrid, 10014, "VP10014", "Skříňka na šicí stroj", 2, "skříně", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10015, "VP10015", "Parapet okenní 25cm", 18, "jiné", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10016, "VP10016", "Dveře vnější ozdobné dub", 3, "dveře", ProductTpv.Cooperation);
            this.CreateLeftPanelRow(leftGrid, 10017, "VP10017", "Stůl jídelní 6 osob buk", 2, "stoly", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10018, "VP10018", "Židle jídelní buk", 12, "židle", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10019, "VP10019", "Květinová stěna borovice 245cm", 1, "jiné", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10020, "VP10020", "Knihovna volně stojící 90cm", 6, "skříně", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10021, "VP10021", "Regály sklepní smrk 3m", 8, "jiné", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10022, "VP10022", "Stolek servírovací malý", 1, "stoly", ProductTpv.Standard);
            this.CreateLeftPanelRow(leftGrid, 10023, "VP10023", "Stůl pracovní (\"ponk\"), dub", 2, "stoly", ProductTpv.Cooperation);
            this.CreateLeftPanelRow(leftGrid, 10024, "VP10024", "Skříňka zásuvková 85cm", 6, "skříně", ProductTpv.Standard);

            


            this.LeftGrid = leftGrid;
            this.MainPage.LeftPanel.Grids.Add(leftGrid);
        }
        protected void CreateLeftPanelRow(GuiGrid guiGrid, int recordNumber, string refer, string name, decimal qty, string tagText, ProductTpv tpv)
        {
            guiGrid.Rows.DataTable.Rows.Add(recordNumber, refer, name, qty);
            GuiId rowId = new GuiId(1188, recordNumber);

            if (guiGrid.Rows.RowTags == null)
                guiGrid.Rows.RowTags = new GuiTagItems();
            guiGrid.Rows.RowTags.TagItemList.Add(new GuiTagItem() { RowId = rowId, TagText = tagText });

            GuiGraph guiGraph = new GuiGraph();
            guiGraph.RowId = rowId;
            DateTime begin = this.DateTimeFirst.AddHours(this.Rand.Next(0, 240));
            switch (tpv)
            {
                case ProductTpv.Standard:
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 1, Color.GreenYellow, "10:Pásová pila", "Přeříznout", qty, 10, 5, 15);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 2, Color.Blue, "20:Bruska", "Zabrousit", qty, 0, 10, 5);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 3, Color.BlueViolet, "30:Vrtačka", "Zavrtat pro čepy", qty, 2, 1, 5);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 4, Color.DarkOrange, "40:Čepy", "Nasadit a vlepit čepy", qty, 0, 1, 0);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 5, Color.DarkRed, "50:Klížit", "Sklížit díly", qty, 5, 5, 360);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 6, Color.ForestGreen, "60:Lakovat", "Lakování základní", qty, 5, 30, 240);
                    break;

                case ProductTpv.Luxus:
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 1, Color.GreenYellow, "10:Pásová pila", "Přeříznout", qty, 10, 5, 15);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 2, Color.Blue, "20:Bruska", "Zabrousit", qty, 0, 20, 5);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 3, Color.BlueViolet, "30:Vrtačka", "Zavrtat pro čepy", qty, 2, 1, 5);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 4, Color.DarkOrange, "40:Čepy", "Nasadit a vlepit čepy", qty, 0, 1, 0);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 5, Color.DarkRed, "50:Klížit", "Sklížit díly", qty, 5, 5, 360);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 6, Color.ForestGreen, "60:Lakovat", "Lakování základní", qty, 5, 45, 240);
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 7, Color.DarkGreen, "70:Lakovat", "Lakování lesklé", qty, 5, 45, 240);
                    break;

                case ProductTpv.Cooperation:
                    this.CreateLeftPanelOperation(guiGraph, ref begin, recordNumber, 1, Color.Gray, "10:Kooperace", "Udělá to někdo jiný", qty, 360, 0, 1440);
                    break;

            }
            guiGrid.Graphs.Add(guiGraph);
        }
        protected void CreateLeftPanelOperation(GuiGraph guiGraph, ref DateTime begin, int groupNumber, int itemNumber, Color backColor, string text, string toolTip, decimal qty, decimal tbc, decimal tac, decimal tec)
        {
            GuiGraphItem item = new GuiGraphItem();
            item.ItemId = new GuiId(1189, 10 * groupNumber + itemNumber);
            item.GroupId = new GuiId(1188, groupNumber);
            item.BackColor = backColor;
            item.BehaviorMode = GraphItemBehaviorMode.DefaultText;
            item.DataId = item.ItemId;
            item.Text = text;

            TimeSpan time = TimeSpan.FromMinutes((double)(tbc + qty * tac + tec));
            DateTime end = begin + time;
            item.Time = new GuiTimeRange(begin, end);

            guiGraph.GraphItems.Add(item);

            TimeSpan pause = ((this.Rand.Next(0, 100) < 25) ? TimeSpan.Zero : TimeSpan.FromHours(this.Rand.Next(1, 12)));
            begin = end + pause;
        }
        protected enum ProductTpv { None, Standard, Luxus, Cooperation }
        protected void CreateCenterPanel()
        { }
        protected void CreateRightPanel()
        { }
        protected void CreateContextFunctions()
        { }
        protected GuiData MainData;
        protected GuiPage MainPage;
        protected GuiGrid LeftGrid;
        protected DateTime DateTimeNow;
        protected DateTime DateTimeFirst;
        protected GuiTimeRange TimeRangeTotal;
        protected GuiTimeRange TimeRangeCurrent;
        #endregion
        #region Generátory náhodných textů

        #endregion

        void IAppHost.CallAppHostFunction(AppHostRequestArgs args)
        {
            
        }
    }
}
