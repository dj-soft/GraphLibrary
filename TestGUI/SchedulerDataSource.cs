using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    /// <summary>
    /// Třída která vytváří datový zdroj pro testování
    /// </summary>
    public class SchedulerDataSource : IAppHost
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SchedulerDataSource()
        {
            this.Rand = new Random();
        }
        #endregion
        #region Tvorba výchozích dat
        /// <summary>
        /// Vytvoří a vrátí kompletní balík s GUI daty, podkladová data zůstávají přítomná v instanci
        /// </summary>
        /// <returns></returns>
        public GuiData CreateGuiData()
        {
            this.MainData = new Noris.LCS.Base.WorkScheduler.GuiData();

            this.InitData();

            this.CreateData();

            this.CreateProperties();
            this.CreateToolBar();
            this.CreateMainPage();
            this.CreateLeftPanel();
            this.CreateCenterPanel();
            this.CreateRightPanel();
            this.CreateContextFunctions();

            return this.MainData;
        }
        /// <summary>
        /// Inicializace datových úseků
        /// </summary>
        protected void InitData()
        {
            DateTime now = DateTime.Now;
            this.DateTimeNow = now.Date;
            this.DateTimeFirst = new DateTime(now.Year, now.Month, 1);
            this.TimeRangeCurrent = new GuiTimeRange(this.DateTimeNow, this.DateTimeNow.AddDays(7d));
            this.TimeRangeTotal = new GuiTimeRange(this.DateTimeFirst, this.DateTimeFirst.AddMonths(2));
        }
        #region Vlastní vytvoření dat k zobrazení
        /// <summary>
        /// Vygeneruje data { Výrobní příkazy, operace, jejich časy } a { Pracoviště, kalendáře }, 
        /// a provede jejich rozplánování (operace do pracovišť).
        /// </summary>
        protected void CreateData()
        {
            int recordId;

            this.ProductOrderDict = new Dictionary<GuiId, ProductOrder>();
            recordId = 10000;
            this.CreateProductOrder(++recordId, "Židle lakovaná červená", Color.DarkGreen, 12, "židle", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Stůl konferenční", Color.DarkGreen, 3, "stoly", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Stolička třínohá", Color.DarkGreen, 8, "židle", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Sedátko chodbové", Color.DarkGreen, 4, "židle", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Židle lakovaná přírodní", Color.DarkGreen, 6, "židle", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Stůl pracovní", Color.DarkGreen, 3, "stoly", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Taburet ozdobný", Color.DarkBlue, 9, "židle", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Skříňka na klíče", Color.DarkGreen, 24, "skříně", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Podstavec pod televizi", Color.DarkGreen, 12, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Botník krátký", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Skříň šatní široká", Color.DarkGreen, 3, "skříně", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Stolek HiFi věže", Color.DarkGreen, 4, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Polička na CD", Color.DarkBlue, 16, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Skříňka na šicí stroj", Color.DarkGreen, 2, "skříně", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Parapet okenní 25cm", Color.DarkGreen, 18, "jiné", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Dveře vnější ozdobné dub", Color.DarkGray, 3, "dveře", ProductTpv.Cooperation);
            this.CreateProductOrder(++recordId, "Stůl jídelní 6 osob buk", Color.DarkGreen, 2, "stoly", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Židle jídelní buk", Color.DarkGreen, 12, "židle", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Květinová stěna borovice 245cm", Color.DarkGreen, 1, "jiné", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Knihovna volně stojící 90cm", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Regály sklepní smrk 3m", Color.DarkOrange, 8, "jiné", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Stolek servírovací malý", Color.DarkGreen, 1, "stoly", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Stůl pracovní (\"ponk\"), dub", Color.DarkGray, 2, "stoly", ProductTpv.Cooperation);
            this.CreateProductOrder(++recordId, "Skříňka zásuvková 85cm", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);

            this.PlanUnitCDict = new Dictionary<GuiId, PlanUnitC>();
            recordId = 10000;
            this.CreatePlanUnitC(++recordId, "Pila pásmová", WP_PILA, "pila", 2, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Pila okružní", WP_PILA, "pila", 2, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Pilka vyřezávací malá", WP_PILA, "pila;drobné", 1, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Dílna truhlářská velká", WP_DILN, "truhláři",  4, CalendarType.Work5d2x8h);
            this.CreatePlanUnitC(++recordId, "Dílna truhlářská malá", WP_DILN, "truhláři",  1, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Lakovna aceton", WP_LAKO, "lakovna;chemie",  5, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Lakovna akryl", WP_LAKO, "lakovna",  5, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Moření", WP_LAKO, "lakovna;chemie",  2, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Dílna lakýrnická", WP_LAKO, "lakovna;chemie",  1, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Kontrola standardní", WP_KONT, "kontrola",  2, CalendarType.Work5d2x8h);
            this.CreatePlanUnitC(++recordId, "Kontrola mistr", WP_KONT, "kontrola",  1, CalendarType.Work5d1x8h);
            this.CreatePlanUnitC(++recordId, "Kooperace DŘEVEX", WP_KOOP, "kooperace",  1, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Kooperace TRUHLEX", WP_KOOP, "kooperace", 1, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Kooperace JAREŠ", WP_KOOP, "kooperace;soukromník", 1, CalendarType.Work7d3x8h);
            this.CreatePlanUnitC(++recordId, "Kooperace TEIMER", WP_KOOP, "kooperace;soukromník",  1, CalendarType.Work7d3x8h);

            this.PlanOperationsToWorkplaces();
        }
        /// <summary>
        /// Vytvoří a uloží jeden výrobní příkaz včetně jeho operací, pro dané zadání.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="name"></param>
        /// <param name="backColor"></param>
        /// <param name="qty"></param>
        /// <param name="tagText"></param>
        /// <param name="tpv"></param>
        protected void CreateProductOrder(int recordId, string name, Color backColor, decimal qty, string tagText, ProductTpv tpv)
        {
            string refer = "VP" + recordId.ToString();
            DateTime start = this.DateTimeNow;
            if (start < this.DateTimeFirst.AddDays(4d))
                start = this.DateTimeFirst.AddDays(4d);
            DateTime begin = start.AddHours(this.Rand.Next(0, 240) - 72);
            DateTime time = begin;

            ProductOrder productOrder = new ProductOrder()
            {
                RecordId = recordId,
                Refer = refer,
                Name = name,
                BackColor = backColor,
                Qty = qty,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null)
            };
            productOrder.OperationList = this.CreateProductOperations(100 * recordId, productOrder, ref time, tpv, qty);
            productOrder.Time = new GuiTimeRange(begin, time);

            this.ProductOrderDict.Add(productOrder.RecordGid, productOrder);
        }
        /// <summary>
        /// Vytvoří a vrátí sadu operací pro dané zadání.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="time"></param>
        /// <param name="tpv"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        protected List<ProductOperation> CreateProductOperations(int recordId, ProductOrder productOrder, ref DateTime time, ProductTpv tpv, decimal qty)
        {
            List<ProductOperation> operations = new List<ProductOperation>();

            int line = 0;
            switch (tpv)
            {
                case ProductTpv.Standard:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.GreenYellow, "Řez tvaru", "Přeříznout", WP_PILA, qty, 30, 10, 45));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.Blue, "Broušení hran", "Zabrousit", WP_DILN, qty, 0, 10, 30));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, 15, 5, 30));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DarkOrange, "Nasadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, 0, 10, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DarkRed, "Klížit", "Sklížit díly", WP_DILN, qty, 30, 5, 360));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.ForestGreen, "Lakovat", "Lakování základní", WP_LAKO, qty, 30, 30, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, 30, 10, 0));
                    break;

                case ProductTpv.Luxus:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.GreenYellow, "Řez délky", "Přeříznout", WP_PILA, qty, 30, 15, 45));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.Blue, "Brousit hrany", "Zabrousit", WP_DILN, qty, 0, 20, 45));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.Blue, "Brousit povrch", "Zabrousit", WP_DILN, qty, 0, 20, 30));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, 30, 5, 45));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DarkOrange, "Vsadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, 0, 5, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DimGray, "Kontrola čepů", "Kontrolovat čepy", WP_KONT, qty, 0, 15, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DarkRed, "Klížit celek", "Sklížit díly", WP_DILN, qty, 45, 30, 360));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DimGray, "Kontrola klížení", "Kontrolovat klížení", WP_KONT, qty, 0, 15, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.ForestGreen, "Lakovat základ", "Lakování základní", WP_LAKO, qty, 30, 45, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.Blue, "Brousit lak", "Zabrousit", WP_DILN, qty, 0, 20, 5));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DarkGreen, "Lakovat lesk", "Lakování lesklé", WP_LAKO, qty, 60, 45, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DimGray, "Kontrola celku", "Kontrolovat lakování", WP_KONT, qty, 0, 10, 0));
                    break;

                case ProductTpv.Cooperation:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.Gray, "Kooperace", "Udělá to někdo jiný", WP_KOOP, qty, 360, 0, 1440));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, ref time, Color.DimGray, "Kontrola", "Kontrolovat kooperaci", WP_KONT, qty, 30, 5, 0));
                    break;

            }
            return operations;
        }
        /// <summary>
        /// Vytvoří a vrátí jednu operaci pro dané zadání.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="line"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        /// <param name="name"></param>
        /// <param name="toolTip"></param>
        /// <param name="qty"></param>
        /// <param name="tbcMin"></param>
        /// <param name="tacMin"></param>
        /// <param name="tecMin"></param>
        /// <returns></returns>
        protected ProductOperation CreateProductOperation(int recordId, ProductOrder productOrder, int line, ref DateTime time, Color backColor, string name, string toolTip,
            string workPlace, decimal qty, int tbcMin, int tacMin, int tecMin)
        {
            ProductOperation operation = new ProductOperation()
            {
                RecordId = recordId,
                ProductOrder = productOrder,
                Line = line,
                Refer = (10 * line).ToString(),
                Name = name,
                BackColor = backColor,
                Qty = qty,
                WorkPlace = workPlace,
                TBc = TimeSpan.FromMinutes(tbcMin),
                TAc = TimeSpan.FromMinutes(tacMin),
                TEc = TimeSpan.FromMinutes(tecMin)
            };
            operation.ToolTip = operation.ReferName + Environment.NewLine + toolTip;
            operation.FillTimes(this, ref time);

            return operation;
        }
        /// <summary>
        /// Vytvoří a uloží jeden záznam Dílna včetně jeho pracovních směn, pro dané zadání.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="name"></param>
        /// <param name="workPlace"></param>
        /// <param name="tagText"></param>
        /// <param name="machinesCount"></param>
        /// <param name="calendar"></param>
        protected void CreatePlanUnitC(int recordId, string name, string workPlace, string tagText, int machinesCount, CalendarType calendar)
        {
            string refer = "VP" + recordId.ToString();
            PlanUnitC planUnitC = new PlanUnitC()
            {
                RecordId = recordId,
                Refer = refer,
                Name = name,
                WorkPlace = workPlace,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null),
                MachinesCount = machinesCount,
                WorkTimes = CreateWorkingItems(1000 * recordId, calendar, (float)machinesCount, this.TimeRangeTotal)
            };
            this.PlanUnitCDict.Add(planUnitC.RecordGid, planUnitC);
        }
        /// <summary>
        /// Vytvoří a vrátí záznamy pro pracovní směny.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="calendar"></param>
        /// <param name="height"></param>
        /// <param name="totalTimeRange"></param>
        /// <returns></returns>
        protected List<WorkTime> CreateWorkingItems(int recordId, CalendarType calendar, float height, GuiTimeRange totalTimeRange)
        {
            List<WorkTime> list = new List<WorkTime>();
            DateTime time = totalTimeRange.Begin.Date;
            GuiTimeRange workingTimeRange;
            Color backColor;
            while (this.CreateWorkingTime(ref time, calendar, totalTimeRange, out workingTimeRange, out backColor))
            {
                WorkTime workTime = new WorkTime()
                {
                    RecordId = ++recordId,
                    Time = workingTimeRange,
                    Height = height,
                    BackColor = backColor,
                    IsEditable = false,
                    Text = null,
                    ToolTip = workingTimeRange.ToString()
                };
                list.Add(workTime);
            }
            return list;
        }
        /// <summary>
        /// Metoda vytvoří a vrátí další pracovní čas, jehož začátek je roven nebo větší než daný výchozí čas.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="calendar"></param>
        /// <param name="totalTimeRange"></param>
        /// <param name="workingTimeRange"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected bool CreateWorkingTime(ref DateTime time, CalendarType calendar, GuiTimeRange totalTimeRange, out GuiTimeRange workingTimeRange, out Color backColor)
        {
            workingTimeRange = null;
            backColor = Color.Empty;
            if (time >= totalTimeRange.End) return false;  // Konec úseku.

            int shift = 0;
            double hour = time.TimeOfDay.TotalHours;       // Hodina počátku (včetně minut a sekund), například hodnota 6.75 = čas 06:45
            switch (calendar)
            {
                case CalendarType.Work5d1x8h:
                    // Po ÷ Pá; { 7:00 ÷ 15:30 }
                    if (hour > 7d || !IsWorkingDay(time))
                        MoveToNextDay(ref time, true, 0);
                    time = time.Date;
                    workingTimeRange = new GuiTimeRange(time.AddHours(7), time.AddHours(15.5d));
                    backColor = Color.FromArgb(160, Color.LightGreen);
                    time = time.AddDays(1d);
                    break;

                case CalendarType.Work5d2x8h:
                    // Po ÷ Pá; { 6:00 ÷ 14:00 }  +  { 14:00 ÷ 22:00 }
                    shift = 0;
                    if (hour > 14d || !IsWorkingDay(time))
                    {   // Ranní část:
                        MoveToNextDay(ref time, true, 0);
                    }
                    else if (hour > 6d)
                    {   // Odpolední část:
                        shift = 1;
                    }
                    time = time.Date;
                    switch (shift)
                    {
                        case 0:        // Ranní směna:
                            workingTimeRange = new GuiTimeRange(time.AddHours(6d), time.AddHours(14d));
                            backColor = Color.FromArgb(160, Color.LightGreen);
                            time = time.AddHours(14d);
                            break;
                        case 1:        // Odpolední směna:
                            workingTimeRange = new GuiTimeRange(time.AddHours(14d), time.AddHours(22d));
                            backColor = Color.FromArgb(160, Color.LightSalmon);
                            time = time.AddDays(1d);
                            break;
                    }
                    break;
                case CalendarType.Work7d3x8h:
                    // Po ÷ Ne; { 6:00 ÷ 14:00 }  +  { 14:00 ÷ 22:00 }  +  { 22:00 ÷ 6:00 }
                    shift = (hour <= 6d ? 0 : (hour <= 14d ? 1 : 2));
                    time = time.Date;
                    switch (shift)
                    {
                        case 0:        // Ranní směna:
                            workingTimeRange = new GuiTimeRange(time.AddHours(6d), time.AddHours(14d));
                            backColor = Color.FromArgb(160, Color.LightGreen);
                            time = workingTimeRange.End;
                            break;
                        case 1:        // Odpolední směna:
                            workingTimeRange = new GuiTimeRange(time.AddHours(14d), time.AddHours(22d));
                            backColor = Color.FromArgb(160, Color.LightSalmon);
                            time = workingTimeRange.End;
                            break;
                        case 2:        // Noční směna:
                            workingTimeRange = new GuiTimeRange(time.AddHours(22d), time.AddHours(30d));
                            backColor = Color.FromArgb(160, Color.LightBlue);
                            time = workingTimeRange.End;
                            break;
                    }
                    break;
            }
            return (workingTimeRange != null);
        }
        /// <summary>
        /// Posune dané datum na následující den na danou hodinu.
        /// Pokud je dán požadavek workingDay = true, pak se datum posune na nejbližší pracovní den.
        /// Datum bude posunuto vždy nejméně o jeden den dopředu.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="workingDay"></param>
        /// <param name="hour"></param>
        protected static void MoveToNextDay(ref DateTime time, bool workingDay, int hour)
        {
            for (int t = 0; t < 7; t++)
            {
                time = time.AddDays(1d).Date;
                if (!workingDay || IsWorkingDay(time)) break;
            }
            if (hour > 0 && hour <= 23)
                time = time.AddHours(hour);
        }
        /// <summary>
        /// Vrátí true, pokud daný den je všeobecně pracovní (Po - Pá)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        protected static bool IsWorkingDay(DateTime time)
        {
            DayOfWeek dow = time.DayOfWeek;
            return (dow == DayOfWeek.Monday || dow == DayOfWeek.Tuesday || dow == DayOfWeek.Wednesday || dow == DayOfWeek.Thursday || dow == DayOfWeek.Friday);
        }
        /// <summary>
        /// Typ výroby
        /// </summary>
        protected enum ProductTpv { None, Standard, Luxus, Cooperation }
        /// <summary>
        /// Typ kalendáře
        /// </summary>
        protected enum CalendarType { None, Work5d1x8h, Work5d2x8h, Work7d3x8h }
        /// <summary>
        /// Dictionary s Výrobními příkazy
        /// </summary>
        protected Dictionary<GuiId, ProductOrder> ProductOrderDict;
        /// <summary>
        /// Dictionary s Pracovišti
        /// </summary>
        protected Dictionary<GuiId, PlanUnitC> PlanUnitCDict;
        protected const string WP_PILA = "Pila";
        protected const string WP_DILN = "Dílna";
        protected const string WP_LAKO = "Lakovna";
        protected const string WP_KOOP = "Kooperace";
        protected const string WP_KONT = "Kontrola";
        #endregion
        #region Rozplánování operací do pracovišť
        /// <summary>
        /// Umístí pracovní časy operací výrobních příkazů na vhodná pracoviště
        /// </summary>
        protected void PlanOperationsToWorkplaces()
        {
            int recordId = 0;
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values.Where(i => i.OperationList != null))
                foreach (ProductOperation productOperation in productOrder.OperationList)
                    this.PlanOperationToWorkplaces(ref recordId, productOperation);
        }
        /// <summary>
        /// Umístí pracovní časy operací dané operace na vhodné pracoviště
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="productOperation"></param>
        protected void PlanOperationToWorkplaces(ref int recordId, ProductOperation productOperation)
        {
            string workPlace = productOperation.WorkPlace;
            if (String.IsNullOrEmpty(workPlace)) return;
            PlanUnitC[] units = this.PlanUnitCDict.Values.Where(p => p.WorkPlace == workPlace).ToArray();
            int count = units.Length;
            if (count == 0) return;
            PlanUnitC planUnitC = this.GetRandom(units);
            productOperation.PlanOperationToWorkplace(ref recordId, planUnitC);
        }
        #endregion
        #region Tvorba GUI
        /// <summary>
        /// Vygeneruje základní nastavení GUI prostředí
        /// </summary>
        protected void CreateProperties()
        {
            this.MainData.Properties.InitialTimeRange = this.TimeRangeCurrent;
            this.MainData.Properties.TotalTimeRange = this.TimeRangeTotal;
            this.MainData.Properties.PluginFormBorder = PluginFormBorderStyle.Sizable;
            this.MainData.Properties.PluginFormIsMaximized = true;
            this.MainData.Properties.PluginFormTitle = "Plánovací nářadí";
            this.MainData.Properties.GraphItemMoveSameGraph = GraphItemMoveAlignY.OnOriginalItemPosition;
            this.MainData.Properties.GraphItemMoveOtherGraph = GraphItemMoveAlignY.OnMousePosition;
        }
        /// <summary>
        /// Vygeneruje nastavení toolbaru GUI
        /// </summary>
        protected void CreateToolBar()
        {
            this.MainData.ToolbarItems.ToolbarShowSystemItems = ToolbarSystemItem.Default;
        }
        /// <summary>
        /// Vygeneruje hlavní (a jedinou) stránku pro data, zatím bez dat
        /// </summary>
        protected void CreateMainPage()
        {
            this.MainPage = new GuiPage() { Name = "MainPage", Title = "Plánování dílny POLOTOVARY", ToolTip = "Toto je pouze ukázková knihovna" };
            this.MainData.Pages.Add(this.MainPage);
        }
        /// <summary>
        /// Vygeneruje kompletní data do levého panelu = Výrobní příkazy
        /// </summary>
        protected void CreateLeftPanel()
        {
            GuiGrid gridLeft = new GuiGrid() { Name = "GridLeft", Title = "Výrobní příkazy" };

            gridLeft.GridProperties.TagFilterItemHeight = 26;
            gridLeft.GridProperties.TagFilterItemMaxCount = 60;
            gridLeft.GridProperties.TagFilterRoundItemPercent = 50;
            gridLeft.GridProperties.TagFilterEnabled = true;
            gridLeft.GridProperties.TagFilterBackColor = Color.FromArgb(64, 128, 64);

            gridLeft.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
            gridLeft.GraphProperties.BottomMarginPixel = 2;
            gridLeft.GraphProperties.GraphLineHeight = 6;
            gridLeft.GraphProperties.GraphLinePartialHeight = 24;
            gridLeft.GraphProperties.GraphPosition = DataGraphPositionType.OnBackgroundLogarithmic;
            gridLeft.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;
            gridLeft.GraphProperties.LogarithmicGraphDrawOuterShadow = 0.15f;
            gridLeft.GraphProperties.LogarithmicRatio = 0.60f;
            gridLeft.GraphProperties.Opacity = 192;
            gridLeft.GraphProperties.TableRowHeightMax = 28;
            gridLeft.GraphProperties.TableRowHeightMin = 22;
            gridLeft.GraphProperties.TimeAxisMode = TimeGraphTimeAxisMode.LogarithmicScale;
            gridLeft.GraphProperties.UpperSpaceLogical = 1f;
            
            DataTable rowTable = WorkSchedulerSupport.CreateTable("RowsLeft", "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, qty decimal");
            gridLeft.Rows = new GuiTable() { Name = "GridLeft", DataTable = rowTable };
            gridLeft.Rows.ColumnsExtendedInfo[0].ClassNumber = ProductOrder.ClassNumber;
            gridLeft.Rows.ColumnsExtendedInfo[0].BrowseColumnType = BrowseColumnType.SubjectNumber;
            gridLeft.Rows.ColumnsExtendedInfo[1].PrepareDataColumn("Číslo", 85, true, null, true);
            gridLeft.Rows.ColumnsExtendedInfo[2].PrepareDataColumn("Dílec", 200, true, null, true);
            gridLeft.Rows.ColumnsExtendedInfo[3].PrepareDataColumn("Množství", 45, true, null, true);
            gridLeft.Rows.RowTags = new GuiTagItems();

            // Data tabulky = Výrobní příkazy:
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                this.AddProductOrderToGrid(gridLeft, productOrder);

            this.GridLeft = gridLeft;
            this.MainPage.LeftPanel.Grids.Add(gridLeft);
        }
        /// <summary>
        /// Do dodaného GuiGridu přidá řádek za daný Výrobní příkaz, přidá jeho TagItems a graf z jeho operací.
        /// </summary>
        /// <param name="guiGrid"></param>
        /// <param name="productOrder"></param>
        protected void AddProductOrderToGrid(GuiGrid guiGrid, ProductOrder productOrder)
        {
            GuiId rowId = productOrder.RecordGid;
            guiGrid.Rows.DataTable.Rows.Add(productOrder.RecordId, productOrder.Refer, productOrder.Name, productOrder.Qty);
            guiGrid.Rows.RowTags.TagItemList.AddRange(productOrder.TagItems);
            guiGrid.Graphs.Add(productOrder.CreateGuiGraph());
        }
        /// <summary>
        /// Vygeneruje kompletní data do středního panelu = Pracoviště
        /// </summary>
        protected void CreateCenterPanel()
        {
            GuiGrid gridCenter = new GuiGrid() { Name = "GridCenter", Title = "Pracoviště" };

            gridCenter.GridProperties.TagFilterItemHeight = 26;
            gridCenter.GridProperties.TagFilterItemMaxCount = 60;
            gridCenter.GridProperties.TagFilterRoundItemPercent = 50;
            gridCenter.GridProperties.TagFilterEnabled = true;
            gridCenter.GridProperties.TagFilterBackColor = Color.FromArgb(64, 128, 64);

            gridCenter.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
            gridCenter.GraphProperties.TimeAxisBackColor = Color.FromArgb(192, 224, 255);
            gridCenter.GraphProperties.BottomMarginPixel = 2;
            gridCenter.GraphProperties.GraphLineHeight = 20;
            gridCenter.GraphProperties.GraphLinePartialHeight = 40;
            gridCenter.GraphProperties.GraphPosition = DataGraphPositionType.InLastColumn;
            gridCenter.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;
            gridCenter.GraphProperties.LogarithmicGraphDrawOuterShadow = 0.15f;
            gridCenter.GraphProperties.LogarithmicRatio = 0.60f;
            gridCenter.GraphProperties.Opacity = 255;
            gridCenter.GraphProperties.TableRowHeightMax = 260;
            gridCenter.GraphProperties.TableRowHeightMin = 22;
            gridCenter.GraphProperties.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
            gridCenter.GraphProperties.UpperSpaceLogical = 1f;
            gridCenter.GraphProperties.LinkColorStandard = Color.LightGreen;
            gridCenter.GraphProperties.LinkColorWarning = Color.Yellow;
            gridCenter.GraphProperties.LinkColorError = Color.DarkRed;
            gridCenter.GraphProperties.TimeAxisSegmentList = CreateWeekends(this.TimeRangeTotal, Color.FromArgb(212, 255, 255));

            DataTable rowTable = WorkSchedulerSupport.CreateTable("RowsCenter", "cislo_subjektu int, reference_subjektu string, nazev_subjektu string, machines_count decimal");
            gridCenter.Rows = new GuiTable() { Name = "GridCenter", DataTable = rowTable };
            gridCenter.Rows.ColumnsExtendedInfo[0].ClassNumber = PlanUnitC.ClassNumber;
            gridCenter.Rows.ColumnsExtendedInfo[0].BrowseColumnType = BrowseColumnType.SubjectNumber;
            gridCenter.Rows.ColumnsExtendedInfo[1].PrepareDataColumn("Číslo", 85, true, null, true);
            gridCenter.Rows.ColumnsExtendedInfo[2].PrepareDataColumn("Název", 200, true, null, true);
            gridCenter.Rows.ColumnsExtendedInfo[3].PrepareDataColumn("Počet", 45, true, null, true);
            gridCenter.Rows.RowTags = new GuiTagItems();

            // Data tabulky = Plánovací jednotky:
            foreach (PlanUnitC planUnitC in this.PlanUnitCDict.Values)
                this.AddPlanUnitCToGrid(gridCenter, planUnitC);

            // Vztahy prvků (Link):
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                this.AddGraphLinkToGrid(gridCenter, productOrder);

            this.GridCenter = gridCenter;
            this.MainPage.MainPanel.Grids.Add(gridCenter);
        }
        protected static List<GuiTimeAxisSegment> CreateWeekends(GuiTimeRange totalTimeRange, Color backColor)
        {
            List<GuiTimeAxisSegment> result = new List<GuiTimeAxisSegment>();

            DateTime monday = totalTimeRange.Begin;
            if (monday.TimeOfDay.Ticks > 0L)
                monday = monday.AddDays(1d).Date;
            DayOfWeek dow = monday.DayOfWeek;              // Neděle=0; Pondělí=1; Úterý=2; ... Sobota=6
            int add = (dow == DayOfWeek.Sunday ? 1 : (dow == DayOfWeek.Monday ? 0 : 8 - (int)dow));
            if (add > 0) monday = monday.AddDays((double)add);

            while (true)
            {
                DateTime saturday = monday.AddDays(-2d);
                GuiTimeRange weekend = new GuiTimeRange(saturday, monday);
                if (weekend.Begin > totalTimeRange.End) break;
                if (weekend.End > totalTimeRange.Begin)
                {
                    GuiDoubleRange sizeRange = new GuiDoubleRange(0.0f, 0.7f);
                    string toolTip = "Víkend " + weekend.Begin.ToShortDateString() + " - " + weekend.End.ToShortDateString();
                    GuiTimeAxisSegment segment = new GuiTimeAxisSegment() { TimeRange = weekend, BackColor = backColor, SizeRange = sizeRange, ToolTip = toolTip };
                    result.Add(segment);
                }
                monday = monday.AddDays(7d).Date;
            }

            return result;
        }
        /// <summary>
        /// Do dodaného GuiGridu přidá řádek za danou Plánovací jednotkupříkaz, přidá jeho TagItems a graf z jeho operací.
        /// </summary>
        /// <param name="guiGrid"></param>
        /// <param name="planUnitC"></param>
        protected void AddPlanUnitCToGrid(GuiGrid guiGrid, PlanUnitC planUnitC)
        {
            GuiId rowId = planUnitC.RecordGid;
            guiGrid.Rows.DataTable.Rows.Add(planUnitC.RecordId, planUnitC.Refer, planUnitC.Name, planUnitC.MachinesCount);
            guiGrid.Rows.RowTags.TagItemList.AddRange(planUnitC.TagItems);
            guiGrid.Graphs.Add(planUnitC.CreateGuiGraph());
        }
        protected void AddGraphLinkToGrid(GuiGrid guiGrid, ProductOrder productOrder)
        {
            guiGrid.GraphLinks.AddRange(productOrder.CreateGuiLinks());
        }
        protected void CreateRightPanel()
        { }
        protected void CreateContextFunctions()
        { }
        protected GuiData MainData;
        protected GuiPage MainPage;
        protected GuiGrid GridLeft;
        protected GuiGrid GridCenter;
        protected DateTime DateTimeNow;
        protected DateTime DateTimeFirst;
        protected GuiTimeRange TimeRangeTotal;
        protected GuiTimeRange TimeRangeCurrent;
        #endregion
        #endregion
        #region Náhodná data
        /// <summary>
        /// Metoda vygeneruje a vrátí časový úsek.
        /// Jeho počátek = begin;
        /// Jeho end = počátek + (time * timeQty, pokud timeQty má hodnotu která je 0 nebo kladná).
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="time"></param>
        /// <param name="timeQty"></param>
        /// <returns></returns>
        internal GuiTimeRange GetTimeRange(ref DateTime begin, TimeSpan time, double? timeQty = null)
        {
            return this.GetTimeRange(ref begin, 0d, 0, time, timeQty);
        }
        /// <summary>
        /// Metoda vygeneruje a vrátí časový úsek.
        /// Jeho počátek = begin [ + pauza vložená s pravděpodobností pauseRatio v délce 0 až pauseMaxHour];
        /// Jeho end = počátek + (time * timeQty, pokud timeQty má hodnotu která je 0 nebo kladná).
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="pauseRatio"></param>
        /// <param name="pauseMaxHour"></param>
        /// <param name="time"></param>
        /// <param name="timeQty"></param>
        /// <returns></returns>
        internal GuiTimeRange GetTimeRange(ref DateTime begin, double pauseRatio, int pauseMaxHour, TimeSpan time, double? timeQty = null)
        {
            if (pauseRatio > 0d && this.Rand.NextDouble() > pauseRatio)
                begin = begin + TimeSpan.FromHours(this.Rand.Next(pauseMaxHour + 1));
            DateTime end = begin + ((timeQty.HasValue && timeQty.Value >= 0d) ? TimeSpan.FromHours(timeQty.Value * time.TotalHours) : time);
            GuiTimeRange timeRange = new GuiTimeRange(begin, end);
            begin = end;
            return timeRange;
        }
        /// <summary>
        /// Vrátí jeden z prvků daného pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        internal T GetRandom<T>(T[] items)
        {
            int count = (items != null ? items.Length : 0);
            if (count == 0) return default(T);
            return items[this.Rand.Next(count)];
        }
        /// <summary>
        /// Generátor náhodných hodnot
        /// </summary>
        protected Random Rand;
        #endregion
        #region IAppHost
        void IAppHost.CallAppHostFunction(AppHostRequestArgs args)
        {
            
        }
        #endregion
    }
    #region Třídy pro datové prvky
    #region class ProductOrder : Výrobní příkaz
    /// <summary>
    /// ProductOrder : Výrobní příkaz
    /// </summary>
    public class ProductOrder : SubjectClass
    {
        public ProductOrder()
        { }
        public const int ClassNumber = 1188;
        public override int ClassId { get { return ClassNumber; } }
        public List<ProductOperation> OperationList { get; set; }
        public IEnumerable<string> TagTexts { get; set; }
        public IEnumerable<GuiTagItem> TagItems { get { IEnumerable<string> tt = this.TagTexts; return (tt == null ? new GuiTagItem[0] : tt.Select(text => new GuiTagItem() { RowId = this.RecordGid, TagText = text }).ToArray()); } }
        public GuiTimeRange Time { get; set; }
        public decimal Qty { get; set; }
        public Color BackColor { get; set; }
        /// <summary>
        /// Vytvoří a vrátí graf za tento Výrobní příkaz (obsahuje prvky = operace)
        /// </summary>
        /// <returns></returns>
        public GuiGraph CreateGuiGraph()
        {
            GuiGraph guiGraph = new GuiGraph();
            guiGraph.RowId = this.RecordGid;

            if (this.OperationList != null)
                guiGraph.GraphItems.AddRange(this.OperationList.Select(operation => operation.CreateGuiGraphItem()));

            return guiGraph;
        }
        /// <summary>
        /// Vytvoří a vrátí sadu vztahů mezi operacemi tohoto Výrobního příkazu
        /// </summary>
        /// <returns></returns>
        public GuiGraphLink[] CreateGuiLinks()
        {
            if (this.OperationList == null || this.OperationList.Count <= 1) return null;
            List<GuiGraphLink> linkList = new List<GuiGraphLink>();
            for (int i = 1; i < this.OperationList.Count; i++)
                linkList.Add(ProductOperation.CreateGuiLink(this.OperationList[i - 1], this.OperationList[i]));
            return linkList.ToArray();
        }
    }
    #endregion
    #region class ProductOperation : Operace výrobního příkazu
    /// <summary>
    /// ProductOperation : Operace výrobního příkazu
    /// </summary>
    public class ProductOperation : SubjectClass
    {
        public ProductOperation()
        {
            this.Height = 1f;
            this.BackColor = Color.FromArgb(64, 64, 160);
        }
        public const int ClassNumber = 1190;
        public override int ClassId { get { return ClassNumber; } }
        public ProductOrder ProductOrder { get; set; }
        public int Line { get; set; }
        public string ToolTip { get; set; }
        public decimal Qty { get; set; }
        public float Height { get; set; }
        public string WorkPlace { get; set; }
        public TimeSpan TBc { get; set; }
        public TimeSpan TAc { get; set; }
        public TimeSpan TEc { get; set; }
        public GuiTimeRange Time { get; set; }
        public GuiTimeRange TimeTBc { get; set; }
        public GuiTimeRange TimeTAc { get; set; }
        public GuiTimeRange TimeTEc { get; set; }
        public Color BackColor { get; set; }
        /// <summary>
        /// Vypočte a uloží časy jednotlivých fází operace
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="time"></param>
        public void FillTimes(SchedulerDataSource dataSource, ref DateTime time)
        {
            this.TimeTBc = dataSource.GetTimeRange(ref time, 0.25d, 8, this.TBc);
            this.TimeTAc = dataSource.GetTimeRange(ref time, 0.25d, 0, this.TAc, (double?)this.Qty);
            this.TimeTEc = dataSource.GetTimeRange(ref time, 0.10d, 2, this.TEc);
            this.Time = new GuiTimeRange(this.TimeTBc.Begin, this.TimeTEc.End);
        }
        /// <summary>
        /// Vytvoří a vrátí prvek grafu za tuto operaci.
        /// </summary>
        /// <returns></returns>
        public GuiGraphItem CreateGuiGraphItem()
        {
            GuiGraphItem guiGraphItem = new GuiGraphItem()
            {
                ItemId = this.RecordGid,
                GroupId = this.ProductOrder?.RecordGid,
                BackColor = this.BackColor,
                BehaviorMode = GraphItemBehaviorMode.ShowToolTipFadeIn | GraphItemBehaviorMode.ShowCaptionNone,
                DataId = this.RecordGid,
                Text = "",           // Grafický prvek za operaci se zobrazuje v grafu OnBackground, a tam nechceme mít texty!!!
                Time = this.Time
            };
            return guiGraphItem;
        }
        /// <summary>
        /// Zaplánuje tuto operaci (její jednotlivé časy) do pracoviště
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="planUnitC"></param>
        public void PlanOperationToWorkplace(ref int recordId, PlanUnitC planUnitC)
        {
            if (planUnitC == null) return;
            this.PlanUnitTimeToWorkplace(ref recordId, planUnitC, this.TimeTBc, this.BackColor.Morph(Color.Green, 0.25f));
            this.PlanUnitTimeToWorkplace(ref recordId, planUnitC, this.TimeTAc, this.BackColor);
            this.PlanUnitTimeToWorkplace(ref recordId, planUnitC, this.TimeTEc, this.BackColor.Morph(Color.Black, 0.25f));
        }
        /// <summary>
        /// Uloží jednotku práce pro daný čas do pracoviště
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="planUnitC"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        protected void PlanUnitTimeToWorkplace(ref int recordId, PlanUnitC planUnitC, GuiTimeRange time, Color backColor)
        {
            if (planUnitC == null || time == null || time.End <= time.Begin) return;
            if (planUnitC.UnitTimes == null)
                planUnitC.UnitTimes = new List<UnitTime>();
            planUnitC.UnitTimes.Add(this.CreateUnitTime(ref recordId, planUnitC, time, backColor));
        }
        /// <summary>
        /// Vytvoří a vrátí jednotku práce
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="planUnitC"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected UnitTime CreateUnitTime(ref int recordId, PlanUnitC planUnitC, GuiTimeRange time, Color backColor)
        {
            UnitTime unitTime = new UnitTime()
            {
                RecordId = ++recordId,
                Operation = this,
                PlanUnitC = planUnitC,
                Time = time,
                Height = this.Height,
                BackColor = backColor,
                IsEditable = true,
                Text = this.ReferName,
                ToolTip = this.ToolTip
            };
            return unitTime;
        }
        /// <summary>
        /// Vygeneruje a vrátí vztah mezi dvěma operacemi (Link)
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static GuiGraphLink CreateGuiLink(ProductOperation prev, ProductOperation next)
        {
            GuiGraphLink link = new GuiGraphLink()
            {
                ItemIdPrev = prev?.RecordGid,
                ItemIdNext = next?.RecordGid,
                LinkType = GuiGraphItemLinkType.PrevEndToNextBeginSCurve,
                RelationType = GuiGraphItemLinkRelation.OneLevel,
                LinkWidth = 1
            };
            return link;
        }
    }
    #endregion
    #region class PlanUnitC : Pracoviště
    /// <summary>
    /// PlanUnitC : Pracoviště
    /// </summary>
    public class PlanUnitC : SubjectClass
    {
        public const int ClassNumber = 1364;
        public override int ClassId { get { return ClassNumber; } }
        public string WorkPlace { get; set; }
        public IEnumerable<string> TagTexts { get; set; }
        public IEnumerable<GuiTagItem> TagItems { get { IEnumerable<string> tt = this.TagTexts; return (tt == null ? new GuiTagItem[0] : tt.Select(text => new GuiTagItem() { RowId = this.RecordGid, TagText = text }).ToArray()); } }
        public int MachinesCount { get; set; }
        public List<WorkTime> WorkTimes { get; set; }
        public List<UnitTime> UnitTimes { get; set; }
        /// <summary>
        /// Vytvoří a vrátí graf za toto Pracoviště (obsahuje prvky = pracovní směny a prvky práce)
        /// </summary>
        /// <returns></returns>
        public GuiGraph CreateGuiGraph()
        {
            GuiGraph guiGraph = new GuiGraph();
            guiGraph.RowId = this.RecordGid;

            if (this.WorkTimes != null)
                guiGraph.GraphItems.AddRange(this.WorkTimes.Select(workTime => workTime.CreateGuiGraphItem()));

            if (this.UnitTimes != null)
                guiGraph.GraphItems.AddRange(this.UnitTimes.Select(unitTime => unitTime.CreateGuiGraphItem()));

            return guiGraph;
        }
    }
    #endregion
    #region class UnitTime : Pracovní jednotka = kus práce na pracovišti
    /// <summary>
    /// UnitTime : Pracovní jednotka = kus práce na pracovišti
    /// </summary>
    public class UnitTime : RecordClass
    {
        public const int ClassNumber = 1817;
        public override int ClassId { get { return ClassNumber; } }
        public ProductOrder ProductOrder { get { return this.Operation?.ProductOrder; } }
        public ProductOperation Operation { get; set; }
        public PlanUnitC PlanUnitC { get; set; }
        public GuiTimeRange Time { get; set; }
        public float Height { get; set; }
        public Color BackColor { get; set; }
        public bool IsEditable { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        /// <summary>
        /// Vytvoří a vrátí prvek grafu za tuto jednotku práce.
        /// </summary>
        /// <returns></returns>
        public GuiGraphItem CreateGuiGraphItem()
        {
            GuiGraphItem guiGraphItem = new GuiGraphItem()
            {
                ItemId = this.RecordGid,
                GroupId = this.Operation?.RecordGid,
                DataId = this.Operation?.RecordGid,
                RowId = this.PlanUnitC?.RecordGid,
                Layer = 1,
                BackColor = this.BackColor,
                BehaviorMode = GraphItemBehaviorMode.ShowCaptionAllways | GraphItemBehaviorMode.ShowToolTipFadeIn | 
                        GraphItemBehaviorMode.ShowLinkInMouseOver | GraphItemBehaviorMode.ShowLinkInSelected |
                        GraphItemBehaviorMode.MoveToAnotherRow | GraphItemBehaviorMode.MoveToAnotherTime,
                Height = this.Height,
                Text = this.Text,
                ToolTip = this.ToolTip,
                Time = this.Time
            };
            return guiGraphItem;
        }

    }
    #endregion
    #region class WorkTime : Pracovní směna
    /// <summary>
    /// WorkTime : Pracovní směna
    /// </summary>
    public class WorkTime : RecordClass
    {
        public const int ClassNumber = 1365;
        public override int ClassId { get { return ClassNumber; } }
        public GuiTimeRange Time { get; set; }
        public float Height { get; set; }
        public Color BackColor { get; set; }
        public bool IsEditable { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        /// <summary>
        /// Vytvoří a vrátí prvek grafu za tuto pracovní směnu.
        /// </summary>
        /// <returns></returns>
        public GuiGraphItem CreateGuiGraphItem()
        {
            GuiGraphItem guiGraphItem = new GuiGraphItem()
            {
                ItemId = this.RecordGid,
                Layer = 0,
                BackColor = this.BackColor,
                BehaviorMode = GraphItemBehaviorMode.DefaultText,
                Height = this.Height,
                DataId = this.RecordGid,
                Text = this.Text,
                ToolTip = this.ToolTip,
                Time = this.Time
            };
            return guiGraphItem;
        }
    }
    #endregion
    #region class SubjectClass : Subjektový záznam
    /// <summary>
    /// SubjectClass : Subjektový záznam
    /// </summary>
    public abstract class SubjectClass : RecordClass
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + "; \"" + this.ReferName + "\"";
        }
        /// <summary>
        /// Reference
        /// </summary>
        public virtual string Refer { get; set; }
        /// <summary>
        /// Název
        /// </summary>
        public virtual string Name { get; set; }
        /// <summary>
        /// Reference: Název
        /// </summary>
        public string ReferName
        {
            get
            {
                bool hr = !String.IsNullOrEmpty(this.Refer);
                bool hn = !String.IsNullOrEmpty(this.Name);
                if (hr && hn) return this.Refer + ": " + this.Name;
                if (hr) return this.Refer;
                if (hn) return this.Name;
                return "{Empty}";
            }
        }
    }
    #endregion
    #region class RecordClass : Obecný záznam
    /// <summary>
    /// RecordClass : Obecný záznam
    /// </summary>
    public abstract class RecordClass
    {
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Record: " + this.RecordGid;
        }
        /// <summary>
        /// GuiId záznamu (třída + záznam)
        /// </summary>
        public GuiId RecordGid { get { return new GuiId(this.ClassId, this.RecordId); } }
        /// <summary>
        /// Číslo třídy
        /// </summary>
        public abstract int ClassId { get; }
        /// <summary>
        /// Číslo záznamu
        /// </summary>
        public int RecordId { get; set; }
    }
    #endregion
    #endregion
}
