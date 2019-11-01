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
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using RES = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    /// <summary>
    /// Třída která vytváří datový zdroj pro testování
    /// </summary>
    public class SchedulerDataSource : IAppHost, IDisposable
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SchedulerDataSource()
        {
            this.Rand = new Random();
            Eol = Environment.NewLine;
            this.IAppHostInit();
        }
        public static string Eol;
        void IDisposable.Dispose()
        {
            this.AppHostStop();
        }
        #endregion
        #region Tvorba výchozích výrobních dat, plánování operací do pracovišť
        /// <summary>
        /// Vytvoří a vrátí kompletní balík s GUI daty, podkladová data zůstávají přítomná v instanci
        /// </summary>
        /// <returns></returns>
        public GuiData CreateGuiData()
        {
            Application.App.TracePriority = Application.TracePriority.Priority3_BellowNormal;

            this.MainData = new Noris.LCS.Base.WorkScheduler.GuiData();

            this.InitData();
            this.CreateData();
            this.CreateProperties();
            this.CreateToolBar();
            this.CreateMainPage();
            this.CreateLeftPanel();
            this.CreateCenterPanelWorkplace();
            this.CreateCenterPanelPersons();
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
            this.DateTimeFirst = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            this.DateTimeLast = this.DateTimeFirst.AddMonths(3);
            this.TimeRangeCurrent = new GuiTimeRange(this.DateTimeNow, this.DateTimeNow.AddDays(7d));
            this.TimeRangeTotal = new GuiTimeRange(this.DateTimeFirst, this.DateTimeLast);
            this.DataChanged = false;
        }
        #region Vlastní vytvoření dat výrobních příkazů, operací, komponent, plánovacích jednotek atd
        /// <summary>
        /// Vygeneruje data { Výrobní příkazy, operace, jejich časy } a { Pracoviště, kalendáře }, 
        /// a provede jejich rozplánování (operace do pracovišť).
        /// </summary>
        protected void CreateData()
        {
            this.ProductOrderDict = new Dictionary<GuiId, ProductOrder>();
            this.ProductOperationDict = new Dictionary<GuiId, ProductOperation>();
            this.ProductStructureDict = new Dictionary<GuiId, ProductStructure>();

            // VÝROBA:
            this.CreateProductOrder("Židle lakovaná červená", Color.DarkGreen, 12, "židle", ProductTpv.Luxus);
            this.CreateProductOrder("Stůl konferenční", Color.DarkGreen, 3, "stoly", ProductTpv.Luxus);
            this.CreateProductOrder("Stolička třínohá", Color.DarkGreen, 8, "židle", ProductTpv.Standard);
            this.CreateProductOrder("Sedátko chodbové", Color.DarkGreen, 4, "židle", ProductTpv.Standard);
            this.CreateProductOrder("Židle lakovaná přírodní", Color.DarkGreen, 6, "židle", ProductTpv.Standard);
            this.CreateProductOrder("Stůl pracovní", Color.DarkGreen, 3, "stoly", ProductTpv.Standard);
            this.CreateProductOrder("Taburet ozdobný", Color.DarkBlue, 9, "židle", ProductTpv.Luxus);
            this.CreateProductOrder("Skříňka na klíče", Color.DarkGreen, 24, "skříně", ProductTpv.Standard);
            this.CreateProductOrder("Podstavec pod televizi", Color.DarkGreen, 12, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder("Botník krátký", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);
            this.CreateProductOrder("Skříň šatní široká", Color.DarkGreen, 3, "skříně", ProductTpv.Luxus);
            this.CreateProductOrder("Stolek HiFi věže", Color.DarkGreen, 4, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder("Polička na CD", Color.DarkBlue, 16, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder("Skříňka na šicí stroj", Color.DarkGreen, 2, "skříně", ProductTpv.Standard);
            this.CreateProductOrder("Parapet okenní 25cm", Color.DarkGreen, 18, "jiné", ProductTpv.Standard);
            this.CreateProductOrder("Dveře vnější ozdobné dub", Color.DarkGray, 3, "dveře", ProductTpv.Cooperation);
            this.CreateProductOrder("Stůl jídelní 6 osob buk", Color.DarkGreen, 2, "stoly", ProductTpv.Standard);
            this.CreateProductOrder("Židle jídelní buk", Color.DarkGreen, 12, "židle", ProductTpv.Standard);
            this.CreateProductOrder("Květinová stěna borovice 245cm", Color.DarkGreen, 1, "jiné", ProductTpv.Standard);
            this.CreateProductOrder("Knihovna volně stojící 90cm", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);
            this.CreateProductOrder("Regály sklepní smrk 3m", Color.DarkOrange, 8, "jiné", ProductTpv.Standard);
            this.CreateProductOrder("Stolek servírovací malý", Color.DarkGreen, 1, "stoly", ProductTpv.Standard);
            this.CreateProductOrder("Stůl pracovní (\"ponk\"), dub", Color.DarkGray, 2, "stoly", ProductTpv.Cooperation);
            this.CreateProductOrder("Skříňka zásuvková 85cm", Color.DarkGreen, 6, "skříně", ProductTpv.Standard);
            this.CreateProductOrder("Krabička dřevěná 35cm", Color.DarkCyan, 30, "jiné", ProductTpv.Simple);
            this.CreateProductOrder("Krabička dřevěná 45cm", Color.DarkCyan, 36, "jiné", ProductTpv.Simple);
            this.CreateProductOrder("Krabička dřevěná 60cm", Color.DarkBlue, 48, "jiné", ProductTpv.Standard);
            this.CreateProductOrder("Houpací křeslo tmavé", Color.DarkBlue, 6, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder("Psí bouda střední", Color.DarkOrange, 12, "outdoor", ProductTpv.Simple);
            this.CreateProductOrder("Zábradlí schodišťové standard", Color.DarkGreen, 30, "stavba", ProductTpv.Standard);
            this.CreateProductOrder("Stříška před vchodem", Color.DarkRed, 3, "stavba", ProductTpv.Simple);
            this.CreateProductOrder("Krmítko pro menší ptactvo", Color.DarkViolet, 42, "outdoor", ProductTpv.Simple);
            this.CreateProductOrder("Pergola zahradní 4x5 m", Color.DarkCyan, 3, "stavba", ProductTpv.Simple);

            // Pracvovní doby, Jednotky práce:
            this.WorkTimeDict = new Dictionary<GuiId, WorkTime>();
            this.WorkUnitDict = new Dictionary<GuiId, WorkUnit>();

            // DÍLNY:
            Color? colorLak = null;         // Color.FromArgb(224, 240, 255);
            this.WorkplaceDict = new Dictionary<GuiId, PlanUnitC>();
            this.CreatePlanUnitCWp("Pila pásmová", WP_PILA, "pila", 2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Pila okružní", WP_PILA, "pila", 2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Pilka vyřezávací malá", WP_PILA, "pila;drobné", 1, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Dílna truhlářská velká", WP_DILN, "truhláři",  4, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Dílna truhlářská malá", WP_DILN, "truhláři",  2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Lakovna aceton", WP_LAKO, "lakovna;chemie",  5, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp("Lakovna akryl", WP_LAKO, "lakovna",  5, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp("Moření", WP_LAKO, "lakovna;chemie",  3, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp("Dílna lakýrnická", WP_LAKO, "lakovna;chemie",  2, CalendarType.Work5d2x8h, colorLak);
            this.CreatePlanUnitCWp("Kontrola standardní", WP_KONT, "kontrola",  2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Kontrola mistr", WP_KONT, "kontrola",  1, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp("Kooperace DŘEVEX", WP_KOOP, "kooperace",  1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp("Kooperace TRUHLEX", WP_KOOP, "kooperace", 1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp("Kooperace JAREŠ", WP_KOOP, "kooperace;soukromník", 1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp("Kooperace TEIMER", WP_KOOP, "kooperace;soukromník",  1, CalendarType.Work5d1x24h, null);

            // OSOBY, RANNÍ SMĚNA:
            this.PersonDict = new Dictionary<GuiId, PlanUnitC>();
            this.CreatePlanUnitCZm("NOVÁK Jiří", CalendarType.Work5d1x8hR, "F1", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("DVOŘÁK Pavel", CalendarType.Work5d1x8hR, "A12", colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm("STARÝ Slavomír", CalendarType.Work5d1x8hR, "C2", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("PEŠEK Petr", CalendarType.Work5d1x8hR, null, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm("JENČÍK Jan", CalendarType.Work5d1x8hR, "H05", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("KRULIŠ Karel", CalendarType.Work5d1x8hR, "B12", colorLak, WP_LAKO);
            this.CreatePlanUnitCZm("BLÁHOVÁ Božena", CalendarType.Work5d1x8hR, "S123", null, WP_DILN);
            this.CreatePlanUnitCZm("NEKOKSA Jindřich", CalendarType.Work5d1x8hR, "X4", colorLak, WP_LAKO);
            this.CreatePlanUnitCZm("POKORNÝ Dan", CalendarType.Work5d1x8hR, "T15", null, WP_DILN, WP_KONT);
            this.CreatePlanUnitCZm("DRAHOKOUPIL Martin", CalendarType.Work5d1x8hR, null, null, WP_KONT);

            // OSOBY, ODPOLEDNÍ SMĚNA:
            this.CreatePlanUnitCZm("VETCHÝ Marek", CalendarType.Work5d1x8hO, "F07", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("SUP Václav", CalendarType.Work5d1x8hO, "J2", colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm("OSOLSOBĚ Viktor", CalendarType.Work5d1x8hO, "U02", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("ČERNÁ Marta", CalendarType.Work5d1x8hO, null, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm("VIDÍM Dan", CalendarType.Work5d1x8hO, "L50", null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm("NĚMEC Jaroslav", CalendarType.Work5d1x8hO, "N80", colorLak, WP_LAKO);
            this.CreatePlanUnitCZm("DLOUHÝ Bedřich", CalendarType.Work5d1x8hO, null, null, WP_DILN);
            this.CreatePlanUnitCZm("HANZAL Patrik", CalendarType.Work5d1x8hO, "R25", colorLak, WP_LAKO);
            this.CreatePlanUnitCZm("SPÍVALOVÁ Ilona", CalendarType.Work5d1x8hO, "D16", null, WP_DILN);
            this.CreatePlanUnitCZm("DIETRICH Zdenek", CalendarType.Work5d1x8hO, "B0", null, WP_KONT);

            this.PlanAllProductOrdersToWorkplaces();
        }
        /// <summary>
        /// Vytvoří a uloží jeden výrobní příkaz včetně jeho operací, pro dané zadání.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="backColor"></param>
        /// <param name="qty"></param>
        /// <param name="tagText"></param>
        /// <param name="tpv"></param>
        /// <param name="startTime">Přesný okamžik počátku VP <see cref="ProductOrder.DatePlanBegin"/>, null = vygeneruje jej náhodně</param>
        protected ProductOrder CreateProductOrder(string name, Color backColor, decimal qty, string tagText, ProductTpv tpv, DateTime? startTime = null)
        {
            DateTime begin = (startTime.HasValue ? startTime.Value : this.CreateRandomStartTime());

            ProductOrder productOrder = new ProductOrder(this)
            {
                Name = name,
                BackColor = backColor,
                Qty = qty,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null)
            };
            productOrder.Refer = "VP" + productOrder.RecordId.ToString();
            this.CreateProductOperations(productOrder, tpv, qty);
            productOrder.DatePlanBegin = begin;

            this.ProductOrderDict.Add(productOrder.RecordGid, productOrder);

            return productOrder;
        }
        /// <summary>
        /// Vrátí náhodně umístěný čas zahájení práce
        /// </summary>
        /// <returns></returns>
        protected DateTime CreateRandomStartTime()
        {
            DateTime start = this.DateTimeNow;
            if (start < this.DateTimeFirst.AddDays(4d))
                start = this.DateTimeFirst.AddDays(4d);
            DateTime begin = start.AddHours(this.Rand.Next(0, 240) - 72);
            return begin;
        }
        /// <summary>
        /// Vytvoří sadu operací pro daný VP a dané zadání.
        /// </summary>
        /// <param name="productOrder"></param>
        /// <param name="tpv"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        protected void CreateProductOperations(ProductOrder productOrder, ProductTpv tpv, decimal qty)
        {
            int line = 0;
            switch (tpv)
            {
                case ProductTpv.Simple:
                    CreateProductOperation(productOrder, ++line, Color.GreenYellow, "Řez tvaru", "Přeříznout", WP_PILA, qty, "D", false, 30, 20, 45, Pbb(60));
                    CreateProductOperation(productOrder, ++line, Color.DarkOrange, "Šroubovat", "Nasadit šrouby a sešroubovat", WP_DILN, qty, "Š", false, 0, 15, 0);
                    CreateProductOperation(productOrder, ++line, Color.ForestGreen, "Lakovat", "Lakování základní", WP_LAKO, qty, "L", true, 30, 30, 240);
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Scan kódu", "Scanování kódu před kontrolou", WP_KONT, qty, "", false, 0, 0, 0);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "OZ", false, 30, 15, 0);
                    break;

                case ProductTpv.Standard:
                    CreateProductOperation(productOrder, ++line, Color.GreenYellow, "Řez tvaru", "Přeříznout", WP_PILA, qty, "D", false, 30, 20, 45, Pbb(60));
                    CreateProductOperation(productOrder, ++line, Color.Blue, "Broušení hran", "Zabrousit", WP_DILN, qty, "", false, 0, 20, 30, Pbb(20));
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, "", false, 15, 15, 30, Pbb(5));
                    CreateProductOperation(productOrder, ++line, Color.DarkOrange, "Nasadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, "Č", false, 0, 45, 0);
                    CreateProductOperation(productOrder, ++line, Color.DarkRed, "Klížit", "Sklížit díly", WP_DILN, qty, "K", false, 30, 20, 360);
                    CreateProductOperation(productOrder, ++line, Color.ForestGreen, "Lakovat", "Lakování základní", WP_LAKO, qty, "L", true, 30, 45, 240);
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Scan kódu", "Scanování kódu před kontrolou", WP_KONT, qty, "", false, 0, 0, 0);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "O", false, 30, 20, 0);
                    break;

                case ProductTpv.Luxus:
                    CreateProductOperation(productOrder, ++line, Color.GreenYellow, "Řez délky", "Přeříznout", WP_PILA, qty, "D", false, 30, 25, 45, Pbb(70));
                    CreateProductOperation(productOrder, ++line, Color.Blue, "Brousit hrany", "Zabrousit", WP_DILN, qty, "", false, 0, 30, 45, Pbb(50));
                    CreateProductOperation(productOrder, ++line, Color.Blue, "Brousit povrch", "Zabrousit", WP_DILN, qty, "", false, 0, 20, 30, Pbb(40));
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, "", false, 30, 15, 45, Pbb(30));
                    CreateProductOperation(productOrder, ++line, Color.DarkOrange, "Vsadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, "Č", false, 0, 45, 0, Pbb(20));
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola čepů", "Kontrolovat čepy", WP_KONT, qty, "", false, 0, 30, 0, Pbb(10));
                    CreateProductOperation(productOrder, ++line, Color.DarkRed, "Klížit celek", "Sklížit díly", WP_DILN, qty, "K", false, 45, 60, 360);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola klížení", "Kontrolovat klížení", WP_KONT, qty, "", false, 0, 30, 0);
                    CreateProductOperation(productOrder, ++line, Color.ForestGreen, "Lakovat základ", "Lakování základní", WP_LAKO, qty, "L", true, 30, 45, 240);
                    CreateProductOperation(productOrder, ++line, Color.Blue, "Brousit lak", "Zabrousit", WP_DILN, qty, "", false, 0, 30, 5);
                    CreateProductOperation(productOrder, ++line, Color.DarkGreen, "Lakovat lesk", "Lakování lesklé", WP_LAKO, qty, "l", true, 60, 60, 240);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola celku", "Kontrolovat lakování", WP_KONT, qty, "", false, 0, 30, 0);
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Scan kódu", "Scanování kódu před kontrolou", WP_KONT, qty, "", false, 0, 0, 0);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "O", false, 30, 20, 0);
                    break;

                case ProductTpv.Cooperation:
                    CreateProductOperation(productOrder, ++line, Color.Gray, "Kooperace", "Udělá to někdo jiný", WP_KOOP, qty, "B", false, 360, 30, 1440);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola", "Kontrolovat kooperaci", WP_KONT, qty, "", false, 1440, 30, 60);
                    CreateProductOperation(productOrder, ++line, Color.BlueViolet, "Scan kódu", "Scanování kódu před kontrolou", WP_KONT, qty, "", false, 0, 0, 0);
                    CreateProductOperation(productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "OZ", false, 30, 20, 0);
                    break;

            }
        }
        /// <summary>
        /// Vytvoří a vrátí jednu operaci pro dané zadání.
        /// Operaci přidá do daného VP a do indexu <see cref="ProductOperationDict"/>.
        /// </summary>
        /// <param name="productOrder"></param>
        /// <param name="line"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        /// <param name="name"></param>
        /// <param name="toolTip"></param>
        /// <param name="qty"></param>
        /// <param name="isFragment"></param>
        /// <param name="tbcMin"></param>
        /// <param name="tacMin"></param>
        /// <param name="tecMin"></param>
        /// <param name="isFixed"></param>
        /// <returns></returns>
        protected ProductOperation CreateProductOperation(ProductOrder productOrder, int line, Color backColor, string name, string toolTip,
            string workPlace, decimal qty, string components, bool isFragment, int tbcMin, int tacMin, int tecMin, bool isFixed = false)
        {
            float height = CreateOperationHeight(isFragment);
            ProductOperation operation = new ProductOperation(this)
            {
                ProductOrder = productOrder,
                Line = line,
                Refer = (10 * line).ToString(),
                Name = name,
                IsFixed = isFixed,
                BackColor = backColor,
                Qty = qty,
                Height = height,
                WorkPlace = workPlace,
                TBc = TimeSpan.FromMinutes(tbcMin),
                TAc = TimeSpan.FromMinutes((double)(qty * (decimal)tacMin)),
                TEc = TimeSpan.FromMinutes(tecMin)
            };
            /*
            TimeSpan add = TimeSpan.FromHours(1d) - operation.TTc;
            if (add.Ticks > 0L) operation.TAc = operation.TAc + add;
            */

            if (IsExpectable(5))
                operation.Icon = RES.Images.Small16.BulletPinkPng;
            if (operation.TTc.Ticks == 0L && IsExpectable(50))
                operation.Icon = RES.Images.Actions24.SystemLogOut2Png;

            operation.ToolTip = operation.ReferName + Eol + productOrder.ReferName + Eol + toolTip;

            // Operaci do VP, a do indexu:
            productOrder.OperationList.Add(operation);
            this.ProductOperationDict.Add(operation.RecordGid, operation);

            // Komponenty:
            if (!String.IsNullOrEmpty(components))
            {
                foreach (char c in components)
                    CreateProductStructures(operation, c, qty);
            }

            return operation;
        }
        /// <summary>
        /// Vytvoří a do operace vepíše jednu komponentu
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="component"></param>
        /// <param name="qty"></param>
        protected void CreateProductStructures(ProductOperation operation, char component, decimal qty)
        {
            switch (component)
            {
                case 'D':
                    this.CreateProductStructure(operation, "DTD", "Dřevo", 0.25m);
                    break;
                case 'Š':
                    this.CreateProductStructure(operation, "M6š", "Šroub M6", 6m);
                    this.CreateProductStructure(operation, "M6p", "Podložka M6", 12m);
                    this.CreateProductStructure(operation, "M6m", "Matka M6", 6m);
                    break;
                case 'L':
                    this.CreateProductStructure(operation, "Cx1000", "Lak Celox 1000", 0.1m);
                    this.CreateProductStructure(operation, "C006", "Nitroředidlo", 0.1m);
                    break;
                case 'Č':
                    this.CreateProductStructure(operation, "Č6x20", "Čep dřevo 6 x 20", 6m);
                    break;
                case 'K':
                    this.CreateProductStructure(operation, "Kh12", "Klíh 12MPa", 0.1m);
                    break;
                case 'l':
                    this.CreateProductStructure(operation, "Sx1050", "Lak syntetic 1050", 0.1m);
                    this.CreateProductStructure(operation, "S006", "Syntetické ředidlo", 0.1m);
                    break;
                case 'B':
                    this.CreateProductStructure(operation, "BA95", "Benzin Natural95", 0.04m);
                    break;
                case 'O':
                    this.CreateProductStructure(operation, "Kt6", "Karton 6\"", 1.00m);
                    this.CreateProductStructure(operation, "Fb2", "Folie bublinková", 0.10m);
                    break;
                case 'Z':
                    this.CreateProductStructure(operation, "ZL", "Záruční list 2roky", 1.00m);
                    this.CreateProductStructure(operation, "Nobs", "Návod k použití", 0.10m);
                    break;
            }
        }
        /// <summary>
        /// Vytvoří a vrátí jednu komponentu k dané operaci, s daným názvem a jednotkovým počtem.
        /// Komponentu přidá do dodané operace i do zdejšího indexu <see cref="ProductStructureDict"/>.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="refer"></param>
        /// <param name="name"></param>
        /// <param name="qtyUnit"></param>
        /// <returns></returns>
        protected ProductStructure CreateProductStructure(ProductOperation operation, string refer, string name, decimal qtyUnit)
        {
            ProductStructure structure = new ProductStructure(this)
            {
                ProductOperation = operation,
                Refer = refer,
                Name = name,
                Qty = operation.Qty * qtyUnit
            };

            // Komponentu do Operace, a do indexu:
            operation.StructureList.Add(structure);
            this.ProductStructureDict.Add(structure.RecordGid, structure);

            return structure;
        }
        /// <summary>
        /// Odebere danou komponentu z její operace a ze zdejšího indexu <see cref="ProductStructureDict"/>.
        /// </summary>
        /// <param name="structure"></param>
        protected void RemoveStructure(ProductStructure structure)
        {
            if (structure == null) return;
            structure.ProductOperation.StructureList.RemoveAll(s => (s.RecordGid == structure.RecordGid));
            this.ProductStructureDict.RemoveIfExists(structure.RecordGid);
        }
        /// <summary>
        /// Vrátí výšku operace
        /// </summary>
        /// <param name="isFragment"></param>
        /// <returns></returns>
        protected float CreateOperationHeight(bool isFragment)
        {
            if (!isFragment) return 1f;
            return this.GetRandom(OperationHeights);
        }
        /// <summary>
        /// Pole vhodných výšek pro operace, které jsou fragmentované
        /// </summary>
        protected static float[] OperationHeights { get { return new float[] { 2.0f, 1.0f, 1.0f, 1.0f, 2.0f }; } }
        /// <summary>
        /// Vytvoří a uloží jeden záznam Dílna včetně jeho pracovních směn, pro dané zadání.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="workPlace"></param>
        /// <param name="tagText"></param>
        /// <param name="machinesCount"></param>
        /// <param name="calendar"></param>
        /// <param name="rowBackColor"></param>
        protected PlanUnitC CreatePlanUnitCWp(string name, string workPlace, string tagText, int machinesCount, CalendarType calendar, Color? rowBackColor)
        {
             PlanUnitC planUnitC = new PlanUnitC(this)
            {
                Name = name,
                WorkPlace = workPlace,
                RowBackColor = rowBackColor,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null),
                MachinesCount = machinesCount,
                PlanUnitType = PlanUnitType.Workplace
            };
            planUnitC.Refer = "D" + planUnitC.RecordId.ToString();
            planUnitC.WorkTimes = CreateWorkingItems(planUnitC, calendar, (float)machinesCount, this.TimeRangeTotal);
            this.WorkplaceDict.Add(planUnitC.RecordGid, planUnitC);

            return planUnitC;
        }
        /// <summary>
        /// Vytvoří a uloží jeden záznam Zaměstnanec včetně jeho pracovních směn, pro dané zadání.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="calendar"></param>
        /// <param name="note"></param>
        /// <param name="rowBackColor"></param>
        /// <param name="workPlaces"></param>
        protected GuiId CreatePlanUnitCZm(string name, CalendarType calendar, string note, Color? rowBackColor, params string[] workPlaces)
        {
            string workplace = workPlaces.ToString(";");
            PlanUnitC planUnitC = new PlanUnitC(this)
            {
                Name = name,
                Note = note,
                WorkPlace = workplace,
                RowBackColor = rowBackColor,
                TagTexts = null,
                MachinesCount = 1,
                PlanUnitType = PlanUnitType.Person
            };
            planUnitC.Refer = "Z" + planUnitC.RecordId.ToString();
            planUnitC.WorkTimes = CreateWorkingItems(planUnitC, calendar, 1f, this.TimeRangeTotal);
            this.PersonDict.Add(planUnitC.RecordGid, planUnitC);

            return planUnitC.RecordGid;
        }
        /// <summary>
        /// Vytvoří a vrátí záznamy pro pracovní směny.
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="height"></param>
        /// <param name="totalTimeRange"></param>
        /// <returns></returns>
        protected List<WorkTime> CreateWorkingItems(PlanUnitC planUnitC, CalendarType calendar, float height, GuiTimeRange totalTimeRange)
        {
            List<WorkTime> list = new List<WorkTime>();
            DateTime time = totalTimeRange.Begin.Date;
            GuiTimeRange workingTimeRange;
            Color backColor;
            float ratio = GetRandomRatio();
            while (this.CreateWorkingTime(ref time, calendar, totalTimeRange, out workingTimeRange, out backColor))
            {
                backColor = Color.FromArgb(64, backColor);
                WorkTime workTime = new WorkTime(this)
                {
                    PlanUnitC = planUnitC,
                    Time = workingTimeRange,
                    Height = height,
                    BackColor = backColor,
                    IsEditable = false,
                    Text = null,
                    ToolTip = workingTimeRange.ToString()
                };

                if (height >= 2f)
                {   // U "středních" pracovišť bude použito jednoduché Ratio:
                    workTime.RatioBegin = ratio;
                    workTime.RatioBeginBackColor = GetRatioColor(backColor, Color.Red, ratio);
                    workTime.RatioLineColor = Color.Black;
                }

                ratio = GetRandomRatio();

                if (height >= 7f)
                {   // U "vysokých" pracovišť dáme jiný graf Pracovních směn:
                    workTime.BackColor = null;
                    workTime.RatioEnd = ratio;
                    workTime.RatioEndBackColor = GetRatioColor(backColor, Color.Red, ratio);
                }

                list.Add(workTime);
            }

            foreach (WorkTime workTime in list)
                this.WorkTimeDict.Add(workTime.RecordGid, workTime);

            return list;
        }
        protected static Color GetRatioColor(Color backColor, Color targetColor, float ratio)
        {
            return backColor.Morph(targetColor, ratio / 2f);
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
                case CalendarType.Work5d1x8hR:
                    // Po ÷ Pá; { 6:00 ÷ 14:00 }
                    if (hour > 6d || !IsWorkingDay(time))
                        MoveToNextDay(ref time, true, 0);
                    time = time.Date;
                    workingTimeRange = new GuiTimeRange(time.AddHours(6), time.AddHours(14));
                    backColor = Color.FromArgb(160, Color.LightGreen);
                    time = time.AddDays(1d);
                    break;
                case CalendarType.Work5d1x8hO:
                    // Po ÷ Pá; { 14:00 ÷ 22:00 }
                    if (hour > 6d || !IsWorkingDay(time))
                        MoveToNextDay(ref time, true, 0);
                    time = time.Date;
                    workingTimeRange = new GuiTimeRange(time.AddHours(14), time.AddHours(22));
                    backColor = Color.FromArgb(160, Color.LightSalmon);
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

                case CalendarType.Work5d1x24h:
                    // Po ÷ Pá; { 0:00 ÷ 0:00 }
                    if (hour > 6d || !IsWorkingDay(time))
                        MoveToNextDay(ref time, true, 0);
                    time = time.Date;
                    workingTimeRange = new GuiTimeRange(time.AddHours(0), time.AddHours(24));
                    backColor = Color.FromArgb(160, Color.LightBlue);
                    time = time.AddDays(1d);
                    break;

                default:
                    var kalendar = calendar;
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
        protected enum ProductTpv { None, Simple, Standard, Luxus, Cooperation }
        /// <summary>
        /// Typ kalendáře
        /// </summary>
        protected enum CalendarType { None, Work5d1x8hR, Work5d1x8hO, Work5d2x8h, Work7d3x8h, Work5d1x24h }
        /// <summary>
        /// Dictionary s Výrobními příkazy
        /// </summary>
        protected Dictionary<GuiId, ProductOrder> ProductOrderDict;
        /// <summary>
        /// Dictionary s Operacemi
        /// </summary>
        protected Dictionary<GuiId, ProductOperation> ProductOperationDict;
        /// <summary>
        /// Dictionary s Komponentami
        /// </summary>
        protected Dictionary<GuiId, ProductStructure> ProductStructureDict;
        /// <summary>
        /// Dictionary s Pracovišti
        /// </summary>
        protected Dictionary<GuiId, PlanUnitC> WorkplaceDict;
        /// <summary>
        /// Dictionary s Dělníky
        /// </summary>
        protected Dictionary<GuiId, PlanUnitC> PersonDict;
        /// <summary>
        /// Dictionary s Pracovními směnami
        /// </summary>
        protected Dictionary<GuiId, WorkTime> WorkTimeDict;
        /// <summary>
        /// Dictionary s Pracovními časy
        /// </summary>
        protected Dictionary<GuiId, WorkUnit> WorkUnitDict;
        protected const string WP_PILA = "Pila";
        protected const string WP_DILN = "Dílna";
        protected const string WP_LAKO = "Lakovna";
        protected const string WP_KOOP = "Kooperace";
        protected const string WP_KONT = "Kontrola";
        #region NextRecordNumber
        /// <summary>
        /// Metoda vrátí další číslo záznamu pro danou třídu dat
        /// </summary>
        /// <param name="classNumber"></param>
        /// <returns></returns>
        public int GetNextRecordId(int classNumber)
        {
            if (this.LastKeyDict == null)
                this.LastKeyDict = new Dictionary<int, int>();
            int recordNumber = 0;
            if (this.LastKeyDict.TryGetValue(classNumber, out recordNumber))
            {
                recordNumber++;
                this.LastKeyDict[classNumber] = recordNumber;
            }
            else
            {
                recordNumber = 1001;
                this.LastKeyDict.Add(classNumber, recordNumber);
            }
            return recordNumber;
        }
        protected Dictionary<int, int> LastKeyDict;
        #endregion
        #endregion
        #region Rozplánování Výrobních příkazů (tj. operací) do pracovišť
        /// <summary>
        /// Umístí pracovní časy operací všech výrobních příkazů na vhodná pracoviště
        /// </summary>
        protected void PlanAllProductOrdersToWorkplaces()
        {
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values.Where(i => i.OperationList != null))
                this.PlanProductOrderToWorkplaces(productOrder, productOrder.DatePlanBegin);
        }
        /// <summary>
        /// Umístí pracovní časy operací daného výrobního příkazu na vhodná pracoviště
        /// </summary>
        /// <param name="productOrder"></param>
        /// <param name="startTime"></param>
        protected void PlanProductOrderToWorkplaces(ProductOrder productOrder, DateTime startTime)
        {
            DateTime flowTime = startTime;
            foreach (ProductOperation productOperation in productOrder.OperationList)
                this.PlanProductOperationToWorkplaces(productOperation, ref flowTime);
            productOrder.Time = new GuiTimeRange(startTime, flowTime);
        }
        /// <summary>
        /// Umístí pracovní časy operací dané operace na vhodné pracoviště
        /// </summary>
        /// <param name="productOperation"></param>
        /// <param name="flowTime"></param>
        protected void PlanProductOperationToWorkplaces(ProductOperation productOperation, ref DateTime flowTime)
        {
            string workPlace = productOperation.WorkPlace;
            if (String.IsNullOrEmpty(workPlace)) return;

            PlanUnitC[] workplaces = this.WorkplaceDict.Values.Where(p => p.WorkPlace == workPlace).ToArray();
            int count = workplaces.Length;
            if (count == 0) return;
            PlanUnitC workplace = this.GetRandom(workplaces);

            PlanUnitC[] persons = this.PersonDict.Values.Where(p => p.WorkPlace.Contains(workPlace)).ToArray();
            PlanUnitC person = this.GetRandom(persons);

            if (Pbb(25))
                flowTime = flowTime + TimeSpan.FromHours(Rand.Next(1, 9));        // Random pauza mezi operacemi 1 až 8 hodin, zařadím v 25% případů
            productOperation.PlanTimeOperation(ref flowTime, Direction.Positive, workplace, person);

            foreach (WorkUnit workUnit in productOperation.WorkUnitDict.Values)   // Sumarizuji WorkUnit z operace do globální Dictionary
                this.WorkUnitDict.Add(workUnit.RecordGid, workUnit);
        }
        #endregion
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
            this.MainData.Properties.TimeChangeSend = TimeChangeSendMode.OnNewTime;
            this.MainData.Properties.TimeChangeSendEnlargement = 2d;
            this.MainData.Properties.TimeChangeInitialValue = this.TimeRangeCurrent;
            this.MainData.Properties.DoubleClickOnGraph = GuiDoubleClickAction.OpenForm;
            this.MainData.Properties.DoubleClickOnGraphItem = GuiDoubleClickAction.TimeZoom;
            this.MainData.Properties.LineShapeEndBegin = GuiLineShape.ZigZagOptimal;
        }
        /// <summary>
        /// Vygeneruje nastavení toolbaru GUI
        /// </summary>
        protected void CreateToolBar()
        {
            this.MainData.ToolbarItems.ToolbarShowSystemItems = ToolbarSystemItem.Default;

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarSubDay,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
                GroupName = "FUNKCE",
                Title = "o den",
                ToolTip = "Posune časovou osu o 1 den doleva (do minulosti)",
                Image = RES.Images.Actions.ArrowLeft2Png,
                ImageHot = RES.Images.Actions.ArrowLeftDouble2Png
            });
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarAddDay,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextTable,
                GroupName = "FUNKCE",
                Title = "o den",
                ToolTip = "Posune časovou osu o 1 den doprava (do budoucnosti)",
                Image = RES.Images.Actions.ArrowRight2Png,
                ImageHot = RES.Images.Actions.ArrowRightDouble2Png
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarRePlan,
                Size = FunctionGlobalItemSize.Whole,
                GroupName = "FUNKCE",
                Title = "PLÁN",
                ToolTip = "Přepočítá plán pro vybrané položky",
                Image = RES.Images.Actions.GoNext4Png,
                ImageHot = RES.Images.Actions.GoNext5Png,
                BlockGuiTime = TimeSpan.FromSeconds(15d),
                BlockGuiMessage = "Přepočítávám plán\r\nPočkejte prosím..."
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarSaveData,
                Size = FunctionGlobalItemSize.Whole,
                Enable = false,
                GroupName = "FUNKCE",
                Title = "ULOŽ",
                ToolTip = "Uloží aktuální stav dat do databáze",
                Image = RES.Images.Actions.DocumentSave7Png,
                ImageHot = RES.Images.Actions.DocumentSaveAs6Png,
                BlockGuiTime = TimeSpan.FromSeconds(15d),
                BlockGuiMessage = "Probíhá uložení dat\r\nPočkejte prosím..."
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarCreateLink,
                Size = FunctionGlobalItemSize.Whole,
                LayoutHint = LayoutHint.NextItemSkipToNextTable,
                GroupName = "FUNKCE",
                Title = "Vztah",
                ToolTip = "Po aktivaci tohoto tlačítka je možno navázat vztah mezi dvěma operacemi",
                Image = RES.Images.Actions.InsertLink2Png,     //    InsertLinkPng,
                GuiActions = GuiActionType.EnableMousePaintLinkLine | GuiActionType.SuppressCallAppHost
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarFilterLeft,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
                GroupName = "NASTAVENÍ",
                Title = "Filtruj VP",
                ToolTip = "Pokud bude aktivní, budou v levé tabulce zobrazeny jen ty Výrobní příkazy, jejichž některá operace se provádí na aktuálním pracovišti.",
                IsCheckable = true,
                StoreValueToConfig = false,         /* Button sice jde aktivovat, ale tento stav nechceme ukládat pro příští start. */
                Image = RES.Images.Actions24.FormatIndentLess3Png,
                GuiActions = GuiActionType.ResetAllRowFilters | GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionFilterProductOrder,
                RunInteractionSource = SourceActionType.TableRowActivatedOnly | SourceActionType.TableRowChecked
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarResetFilters,
                Size = FunctionGlobalItemSize.Half,
                GroupName = "NASTAVENÍ",
                Title = "Zruš filtry",
                Image = RES.Images.Actions24.TabClose2Png,
                GuiActions = GuiActionType.ResetAllRowFilters | GuiActionType.SuppressCallAppHost
            });

            /*
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarTrackBar,
                ItemType = FunctionGlobalItemType.TrackBar,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.ThisItemSkipToNextTable | LayoutHint.ThisItemSkipToNextRow,
                ModuleWidth = 4,
                GroupName = "NASTAVENÍ",
                Image = RES.Images.Actions.DbComit2Png,
                Title = "TrackBar",
                ToolTip = "TrackBar",
                TrackBarSettings = new GuiTrackBarSettings() { TrackLines = 20 }
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarResetTrackBar,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextTable,
                GroupName = "NASTAVENÍ",
                Title = "Reset trackbaru",
                Image = RES.Images.Actions24.TabClose2Png,
                GuiActions = GuiActionType.ResetAllRowFilters | GuiActionType.SuppressCallAppHost
            });

            */
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowColorSet1,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.ThisItemSkipToNextTable | LayoutHint.NextItemOnSameRow,
                GroupName = "OZNAČIT OPERACE",
                Title = "Barva 1",
                ToolTip = "Aktivuje barvy skupiny 1.",
                IsCheckable = true,
                IsChecked = true,
                CheckedGroupName = "ShowColorGroup",
                Image = RES.Images.Actions24.FlagBluePng,
                GuiActions = GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionShowColorSet + ":0",        // :0 = parametr pro interakci (GuiNameInteractionShowColorSet)
                RunInteractionSource = SourceActionType.ToolbarClicked
            });
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowColorSet2,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
                GroupName = "OZNAČIT OPERACE",
                Title = "Barva 2",
                ToolTip = "Aktivuje barvy skupiny 2.",
                IsCheckable = true,
                CheckedGroupName = "ShowColorGroup",
                Image = RES.Images.Actions24.FlagRedPng,
                GuiActions = GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionShowColorSet + ":1",
                RunInteractionSource = SourceActionType.ToolbarClicked
            });
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowColorSet3,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemOnSameRow,
                GroupName = "OZNAČIT OPERACE",
                Title = "Barva 3",
                ToolTip = "Aktivuje barvy skupiny 3.",
                IsCheckable = true,
                CheckedGroupName = "ShowColorGroup",
                Image = RES.Images.Actions24.FlagGreenPng,
                GuiActions = GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionShowColorSet + ":2",
                RunInteractionSource = SourceActionType.ToolbarClicked
            });
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowColorSet4,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextTable,
                GroupName = "OZNAČIT OPERACE",
                Title = "Barva 4",
                ToolTip = "Aktivuje barvy skupiny 4.",
                IsCheckable = true,
                CheckedGroupName = "ShowColorGroup",
                Image = RES.Images.Actions24.FlagBlackPng,
                GuiActions = GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionShowColorSet + ":3",
                RunInteractionSource = SourceActionType.ToolbarClicked
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarAddRow1,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
                GroupName = "MODIFIKACE",
                Title = "Nový řádek",
                ToolTip = "Do grafu přidá další řádek s Výrobním příkazem",
                Image = RES.Images.Actions.ListAdd4Png
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarDelRow1,
                Size = FunctionGlobalItemSize.Half,
                GroupName = "MODIFIKACE",
                Title = "Smaž řádek",
                ToolTip = "Z grafu odebere náhodně řádek s Výrobním příkazem",
                Image = RES.Images.Actions.ListRemove4Png
            });

            /*
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowBottomTable,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.ThisItemSkipToNextTable | LayoutHint.NextItemSkipToNextRow,
                IsCheckable = true,
                IsChecked = true,
                StoreValueToConfig = true,
                GroupName = "NASTAVENÍ",
                Title = "Zaměstnanci",
                ToolTip = "Zobrazí / skryje dolní tabulku zaměstnanců",
                GuiActions = GuiActionType.SetVisibleForControl | GuiActionType.SuppressCallAppHost,
                ActionTargetNames = GuiFullNameGridCenterBottom + ";!" + GuiFullNameRightPanel,
                Image = RES.Images.Actions.EditFindUserPng
            });
            */

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarSwitchEmployeeA,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.ThisItemSkipToNextTable | LayoutHint.NextItemSkipToNextRow,
                IsCheckable = true,
                IsChecked = true,
                StoreValueToConfig = true,
                CheckedGroupName = "ViewSwitchEmployee",
                GroupName = "ZAMĚSTNANCI",
                Title = "Vpravo",
                ToolTip = "Zobrazí tabulku zaměstnanců VPRAVO",
                GuiActions = GuiActionType.SetVisibleForControl | GuiActionType.SuppressCallAppHost,
                ActionTargetNames = "!" + GuiFullNameGridCenterBottom + ";" + GuiFullNameRightPanel,
                Image = RES.Images.Actions.ViewSplitLeftRight2Png
            });
            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarSwitchEmployeeB,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.ThisItemSkipToNextRow | LayoutHint.NextItemSkipToNextTable,
                IsCheckable = true,
                IsChecked = false,
                StoreValueToConfig = true,
                CheckedGroupName = "ViewSwitchEmployee",
                GroupName = "ZAMĚSTNANCI",
                Title = "Dole",
                ToolTip = "Zobrazí tabulku zaměstnanců DOLE",
                GuiActions = GuiActionType.SetVisibleForControl | GuiActionType.SuppressCallAppHost,
                ActionTargetNames = GuiFullNameGridCenterBottom + ";!" + GuiFullNameRightPanel,
                Image = RES.Images.Actions.ViewSplitTopBottom2Png
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowMainLink,
                Size = FunctionGlobalItemSize.Half,
                IsCheckable = true,
                IsChecked = false,
                StoreValueToConfig = false,
                GroupName = "NASTAVENÍ",
                Title = "Vztahy",
                ToolTip = "Zobrazí / skryje vztahy mezi operacemi",
                GuiActions = GuiActionType.SetVisibleForControl | GuiActionType.SuppressCallAppHost,
                ActionTargetNames = GuiFullNameGridCenterTop + GuiData.NAME_DELIMITER + GuiData.TABLELINK_NAME,
                Image = RES.Images.Actions.OfficeChartLineStackedPng
            });
        }
        /// <summary>
        /// Vygeneruje kontextové funkce
        /// </summary>
        protected void CreateContextFunctions()
        {
            this.MainData.ContextMenuItems = new GuiContextMenuSet();
            this.MainData.ContextMenuItems.Title = "Nabídka funkcí";
            this.MainData.ContextMenuItems.BackColor = Color.FromArgb(220, 230, 255);
            this.MainData.ContextMenuItems.ImageScalingSize = new Size(24, 24);

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextFixItem,
                Title = "Nastav FIXOVÁNÍ",
                Image = RES.Images.Actions24.Lock4Png,
                ToolTip = "Tato funkce nastaví fixování u daného záznamu.\r\nTo pak znamená, že s tím nejde hnout.\r\nVŮBEC.",
                VisibleFor = GuiFullNameGridCenterTop + ":" + WorkUnit.ClassNumber.ToString()
            });

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextUnFixItem,
                Title = "Zrušit FIXOVÁNÍ",
                Image = RES.Images.Actions24.Lock2Png,
                ToolTip = "Tato funkce zruší fixování u daného záznamu.\r\nTo pak znamená, že s tím nejde hnout.\r\nVŮBEC.",
                VisibleFor = GuiFullNameGridCenterTop + ":" + WorkUnit.ClassNumber.ToString()
            });

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextShowTime,
                Title = "Zobrazit čas",
                Image = RES.Images.Actions24.ViewCalendarTimeSpentPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Pouze zobrazí čas.",
                VisibleFor = GuiFullNameGridCenterTop + ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW + ":" + PlanUnitC.ClassNumber.ToString()
            });

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextInsertStruct,
                Title = "Přidej komponentu",
                Image = RES.Images.Actions24.InsertTableRowPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Přidá novou komponentu do dané operace.",
                VisibleFor = GuiFullNameGridLeft +
                ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW +
                ":" + GuiContextMenuItem.CLASS_MASTER + ProductStructure.ClassNumber.ToString() + "," + ProductOperation.ClassNumber.ToString()
            });

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = "XxxYyy",        // Tato položka by se NEMĚLA objevit, protože povolené třídy ProductStructure a ProductOperation NEMOHOU být položkové...
                Title = "Tato funkce nesmí být vidět",
                Image = RES.Images.Actions24.InsertTableRowPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Pokud je vidět tato funkce, je chyba v detektorech ContextFunctionValidInfo a enum ClassValidityRange.",
                VisibleFor = GuiFullNameGridLeft +
                ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW +
                ":" + GuiContextMenuItem.CLASS_ENTRIES + ProductStructure.ClassNumber.ToString() + "," + GuiContextMenuItem.CLASS_ENTRIES + ProductOperation.ClassNumber.ToString()
            });

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextRemoveStruct1,
                Title = "Odeber komponentu",
                Image = RES.Images.Actions24.DeleteTableRowPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Odebere vybranou komponentu.",
                VisibleFor = GuiFullNameGridLeft +
                ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW +
                ":" + ProductStructure.ClassNumber.ToString()
            });

            /*
            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextAddOneStruct,
                Title = "Přidej komponentu do této operace",
                Image = RES.Images.Actions24.InsertTableRowPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Do této operace přidá jednu náhodnou komponentu.",
                VisibleFor = GuiFullNameGridLeft +
                ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW +
                ":" + ProductOperation.ClassNumber.ToString()
            });
            */

            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextRemoveStructs,
                Title = "Odeber všechny komponenty operace",
                Image = RES.Images.Actions24.DeleteTableRowPng,
                BackColor = Color.FromArgb(255, 235, 235),
                ToolTip = "Odebere všechny komponenty dané operace.",
                VisibleFor = GuiFullNameGridLeft +
                ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW +
                ":" + ProductOperation.ClassNumber.ToString()
            });

        }
        /// <summary>
        /// Vygeneruje hlavní (a jedinou) stránku pro data, zatím bez dat
        /// </summary>
        protected void CreateMainPage()
        {
            this.MainPage = new GuiPage() { Name = GuiNameMainPage, Title = "Plánování dílny POLOTOVARY", ToolTip = "Toto je pouze ukázková knihovna" };
            this.MainData.Pages.Add(this.MainPage);
        }
        /// <summary>
        /// Vygeneruje kompletní data do levého panelu = Výrobní příkazy
        /// </summary>
        protected void CreateLeftPanel()
        {
            GuiGrid gridLeft = new GuiGrid() { Name = GuiNameGridLeft, Title = "Výrobní příkazy" };

            gridLeft.GridProperties.TagFilterItemHeight = 26;
            gridLeft.GridProperties.TagFilterItemMaxCount = 60;
            gridLeft.GridProperties.TagFilterRoundItemPercent = 50;
            gridLeft.GridProperties.TagFilterEnabled = true;
            gridLeft.GridProperties.TagFilterBackColor = Color.FromArgb(64, 128, 64);

            gridLeft.GridProperties.AddInteraction(new GuiGridInteraction()
            {
                Name = GuiNameInteractionSelectOperations,
                SourceAction = (SourceActionType.TableRowActivatedOnly | SourceActionType.TableRowChecked),
                TargetGridFullName = GuiFullNameGridCenterTop,
                TargetAction = (TargetActionType.SearchSourceItemId | TargetActionType.SearchTargetGroupId | TargetActionType.SelectTargetItem)
            });

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

            GuiDataTable guiTable = new GuiDataTable() { ClassId = ProductOrder.ClassNumber };
            guiTable.ClassId = 1188;
            guiTable.RowCheckEnabled = false;
            guiTable.TreeViewNodeOffset = 14;
            guiTable.TreeViewLinkMode = GuiTreeViewLinkMode.Dot;
            guiTable.TreeViewLinkColor = Color.DarkViolet;
            guiTable.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId, TableClassId = ProductOrder.ClassNumber });
            guiTable.AddColumn(new GuiDataColumn() { Name = "reference_subjektu", Title = "Číslo", Width = 85 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "nazev_subjektu", Title = "Dílec", Width = 200 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "qty", Title = "Množství", Width = 45 });

            List<GuiDataRow> rowList = new List<GuiDataRow>();
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                productOrder.CreateGuiRows(rowList);
            guiTable.AddRows(rowList);

            this.AddProductOrderTagItems(guiTable);

            gridLeft.RowTable = guiTable;

            this.GridLeft = gridLeft;
            this.MainPage.LeftPanel.Grids.Add(gridLeft);
        }
        /// <summary>
        /// Do dodané tabulky přidá řádek za daný Výrobní příkaz, přidá jeho TagItems a graf z jeho operací.
        /// </summary>
        /// <param name="guiGrid"></param>
        /// <param name="productOrder"></param>
        protected void AddProductOrderToGrid(GuiDataTable guiTable, ProductOrder productOrder)
        {
            GuiId rowGid = productOrder.RecordGid;
            GuiIdText name = new GuiIdText() { GuiId = new GuiId(343, productOrder.RecordId), Text = productOrder.Name };
            GuiDataRow row = guiTable.AddRow(rowGid, productOrder.Refer, name, productOrder.Qty);         // productOrder.Name
            row.TagItems = new List<GuiTagItem>(productOrder.TagItems);
            row.Graph = productOrder.CreateGuiGraph();

            foreach (ProductOperation productOperation in productOrder.OperationList)
                this.AddProductOperationToGrid(guiTable, productOperation);
        }
        /// <summary>
        /// Přidá danou operaci do gridu, jako Child řádek ke svému parentu
        /// </summary>
        /// <param name="guiGrid"></param>
        /// <param name="productOperation"></param>
        protected void AddProductOperationToGrid(GuiDataTable guiTable, ProductOperation productOperation)
        {
            GuiId rowGid = productOperation.RecordGid;
            GuiDataRow row = guiTable.AddRow(rowGid, productOperation.Refer, productOperation.Name, productOperation.Qty);
            row.ParentRowGuiId = productOperation.ProductOrder.RecordGid;

            foreach (ProductStructure productStructure in productOperation.StructureList)
                this.AddProductStructureToGrid(guiTable, productStructure);
        }
        /// <summary>
        /// Přidá danou komponentu do gridu, jako Child řádek ke svému parentu
        /// </summary>
        /// <param name="guiGrid"></param>
        /// <param name="productStructure"></param>
        protected void AddProductStructureToGrid(GuiDataTable guiTable, ProductStructure productStructure)
        {
            GuiId rowGid = productStructure.RecordGid;
            GuiDataRow row = guiTable.AddRow(rowGid, productStructure.Refer, productStructure.Name, productStructure.Qty);
            row.ParentRowGuiId = productStructure.ProductOperation.RecordGid;
        }
        /// <summary>
        /// Do tabulky Výrobních příkazů přidá řádkové filtry do úrovně tabulky
        /// </summary>
        /// <param name="guiTable"></param>
        protected void AddProductOrderTagItems(GuiDataTable guiTable)
        {
            List<GuiTagItem> tagItems = new List<GuiTagItem>();

            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
            {
                string tagText = "Ref" + productOrder.Refer.Substring(productOrder.Refer.Length - 2, 1);
                tagItems.Add(new GuiTagItem() { RowId = productOrder.RecordGid, BackColor = Color.LightSeaGreen, TagText = tagText });
            }
            guiTable.TagItems = tagItems;
        }
        /// <summary>
        /// Vygeneruje kompletní data do středního panelu do horní tabulky = Pracoviště
        /// </summary>
        protected void CreateCenterPanelWorkplace()
        {
            GuiGrid gridCenterWorkplace = new GuiGrid() { Name = GuiNameGridCenterTop, Title = "Pracoviště" };

            this.SetCenterGridProperties(gridCenterWorkplace, true, true, true, true, GuiNameRowsCenterTop);
            gridCenterWorkplace.RowTable.RowCheckEnabled = false;
            // gridCenterWorkplace.RowTable.DefaultVisualStyle = new GuiVisualStyle() { FontBold = true, FontRelativeSize = 105 };
            // gridCenterWorkplace.RowTable.DefaultChildVisualStyle = new GuiVisualStyle() { FontBold = false, FontItalic = true, FontRelativeSize = 90, BackColor = Color.FromArgb(240, 240, 240) };
            gridCenterWorkplace.RowTable.DefaultVisualStyle = new GuiVisualStyle() { FontBold = true, FontRelativeSize = 100 };
            gridCenterWorkplace.RowTable.DefaultChildVisualStyle = new GuiVisualStyle() { FontBold = false, FontItalic = true, FontRelativeSize = 100 };

            gridCenterWorkplace.GridProperties.MousePaintLink = this.CreateCenterPanelMousePaint();

            gridCenterWorkplace.GridProperties.ChildRowsEvaluate =
                // Child řádky k Parent řádkům navážeme dynamicky, podle viditelného časového okna:
                GuiChildRowsEvaluateMode.VisibleTimeOnly |
                // K identifikátoru GroupId z Parent řádku najdeme shodný GroupId v Child řádku 
                //   (tzn. Child pracuje na stejné operaci, jako Parent):
                GuiChildRowsEvaluateMode.OnParentGroup | GuiChildRowsEvaluateMode.ToChildGroup |
                // Child věty budeme hledat v jiné tabulce (což znamená provést duplikát řádku!), a to pouze v jejích Root řádcích:
                GuiChildRowsEvaluateMode.InOtherRootRowsOnly |
                // A navíc ty dva prvky musí mít společný čas:
                GuiChildRowsEvaluateMode.ParentChildIntersectTimeOnly;

            gridCenterWorkplace.GridProperties.ChildRowsTableName = GuiFullNameGridCenterBottom;
            gridCenterWorkplace.GridProperties.ChildRowsCopyClassesMode =
                WorkTime.ClassNumber + ":A;" +        // Pracovní čas     : z OtherTable přenést vždy (=chceme vždy vykreslit směny daného child řádku = pracovníka)
                WorkUnit.ClassNumber + ":S;" +        // Pracovní jednotka: z OtherTable přenést jen tehdy, pokud na Parent řádku máme synchronní údaj GroupId
                "0:N";                                // 0 = jiné třídy   : nepřenášet

            gridCenterWorkplace.GridProperties.AddInteraction(new GuiGridInteraction()
            {
                Name = GuiNameInteractionFilterProductOrder,
                SourceAction = (SourceActionType.TimeAxisChanged | SourceActionType.TableRowActivatedOnly | SourceActionType.TableRowChecked),
                TargetGridFullName = GuiFullNameGridLeft,
                // Hledáme: SearchSourceDataId : ve Zdrojovém grafu načteme DataId (= GID Operace VP),
                //          SearchSourceVisibleTime : vezmeme pouze prvky ve viditelném čase,
                //          SearchTargetItemId : tento GID vyhledáme v Cílovém grafu jako ItemId = GID Operace VP,
                //          FilterTargetRows   : a pro cílové prvky zjistíme jejich Row a na ně dáme filtr:
                TargetAction = (TargetActionType.SearchSourceDataId | TargetActionType.SearchSourceVisibleTime | TargetActionType.SearchTargetItemId | TargetActionType.FilterTargetRows),
                // Interakce je podmíněna stavem IsChecked = true u tohoto buttonu v ToolBaru:
                Conditions = GuiNameToolbarFilterLeft
            });

            gridCenterWorkplace.GridProperties.AddInteraction(new GuiGridInteraction()
            {
                Name = GuiNameInteractionShowColorSet,
                // Interakce deklarovaná v GuiGridu CenterTop, která po kliknutí v toolbaru zajistí aktivaci daného skinu.
                SourceAction = SourceActionType.ToolbarClicked,
                TargetAction = TargetActionType.ActivateGraphSkin
            });

            // Data tabulky = Plánovací jednotky Pracoviště:
            foreach (PlanUnitC planUnitC in this.WorkplaceDict.Values)
                this.AddPlanUnitCToGridCenter(gridCenterWorkplace.RowTable, planUnitC, GridPositionType.Workplace);

            // Vztahy prvků (Link):
            gridCenterWorkplace.RowTable.GraphLinks = new List<GuiGraphLink>();
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                productOrder.AddGuiGraphLinksTo(gridCenterWorkplace.RowTable.GraphLinks);

            // Chci zavolat, když uživatel zmáčkne Delete:
            gridCenterWorkplace.ActiveKeys = new List<GuiKeyAction>();
            gridCenterWorkplace.ActiveKeys.Add(new GuiKeyAction() { KeyData = Keys.Delete, BlockGuiTime = TimeSpan.FromSeconds(15d), BlockGuiMessage = "Smazat..." });

            this.GridCenterWorkplace = gridCenterWorkplace;
            this.MainPage.MainPanel.Grids.Add(gridCenterWorkplace);
        }
        /// <summary>
        /// Vrátí definici pro ruční zakreslenování vztahů
        /// </summary>
        /// <returns></returns>
        private GuiMousePaintLink CreateCenterPanelMousePaint()
        {
            GuiMousePaintLink paint = new GuiMousePaintLink();
            paint.PaintLinkPairs = new List<string>();
            paint.PaintLinkPairs.Add("C1190:C1190");
            paint.PaintLineShape = GuiLineShape.ZigZagOptimal;
            paint.EnabledLineForeColor = Color.YellowGreen;
            paint.EnabledLineWidth = 6;
            paint.DisabledLineForeColor = Color.Red;
            paint.DisabledLineWidth = 6;
            return paint;
        }
        /// <summary>
        /// Vygeneruje kompletní data do středního panelu do dolní tabulky = Osoby
        /// </summary>
        protected void CreateCenterPanelPersons()
        {
            GuiGrid gridCenterPersons = new GuiGrid() { Name = GuiNameGridCenterBottom, Title = "Pracovníci" };

            this.SetCenterGridProperties(gridCenterPersons, true, true, true, true, GuiNameRowsCenterBottom);
            gridCenterPersons.RowTable.DefaultVisualStyle = new GuiVisualStyle() { FontBold = true, FontRelativeSize = 105 };
            gridCenterPersons.RowTable.DefaultChildVisualStyle = new GuiVisualStyle() { FontBold = false, FontItalic = true, FontRelativeSize = 90, BackColor = Color.FromArgb(240, 240, 240) };
            gridCenterPersons.RowTable.RowCheckEnabled = false;
            gridCenterPersons.GridProperties.RowDragMoveSource = GuiGridProperties.RowDragSource_DragActivePlusSelectedRows + " " + GuiGridProperties.RowDragSource_Root;
            gridCenterPersons.GridProperties.RowDragMoveToTarget = GuiFullNameGridCenterTop + " " + GuiGridProperties.RowDragTarget_RowRoot + ", " + GuiGridProperties.RowDragTarget_ToItem;

            gridCenterPersons.GridProperties.ChildRowsEvaluate =
                // Child řádky k Parent řádkům navážeme dynamicky, podle viditelného časového okna:
                GuiChildRowsEvaluateMode.VisibleTimeOnly |
                // K identifikátoru GroupId z Parent řádku najdeme shodný GroupId v Child řádku 
                //   (tzn. Child pracuje na stejné operaci, jako Parent):
                GuiChildRowsEvaluateMode.OnParentGroup | GuiChildRowsEvaluateMode.ToChildGroup |
                // Child věty budeme hledat v jiné tabulce (což znamená provést duplikát řádku!), a to pouze v jejích Root řádcích:
                GuiChildRowsEvaluateMode.InOtherRootRowsOnly |
                // A navíc ty dva prvky musí mít společný čas:
                GuiChildRowsEvaluateMode.ParentChildIntersectTimeOnly;

            gridCenterPersons.GridProperties.ChildRowsTableName = GuiFullNameGridCenterTop;
            gridCenterPersons.GridProperties.ChildRowsCopyClassesMode =
                WorkTime.ClassNumber + ":A;" +        // Pracovní čas     : z OtherTable přenést vždy (=chceme vždy vykreslit směny daného child řádku = pracovníka)
                WorkUnit.ClassNumber + ":S;" +        // Pracovní jednotka: z OtherTable přenést jen tehdy, pokud na Parent řádku máme synchronní údaj GroupId
                "0:N";                                // 0 = jiné třídy   : nepřenášet

            // Data tabulky = Plánovací jednotky Pracovníci:
            foreach (PlanUnitC planUnitC in this.PersonDict.Values)
                this.AddPlanUnitCToGridCenter(gridCenterPersons.RowTable, planUnitC, GridPositionType.Person);

            // Chci zavolat, když uživatel zmáčkne Delete:
            gridCenterPersons.ActiveKeys = new List<GuiKeyAction>();
            gridCenterPersons.ActiveKeys.Add(new GuiKeyAction() { KeyData = Keys.Delete, BlockGuiTime = TimeSpan.FromSeconds(15d), BlockGuiMessage = "Smazat..." });

            this.GridCenterPersons = gridCenterPersons;
            this.MainPage.MainPanel.Grids.Add(gridCenterPersons);
        }
        /// <summary>
        /// Vytvoří panel vpravo se zaměstnanci
        /// </summary>
        protected void CreateRightPanel()
        {
            GuiGrid gridRight = new GuiGrid() { Name = GuiNameGridRight, Title = "Pracovníci" };

            gridRight.GridProperties.TagFilterItemHeight = 26;
            gridRight.GridProperties.TagFilterItemMaxCount = 60;
            gridRight.GridProperties.TagFilterRoundItemPercent = 50;
            gridRight.GridProperties.TagFilterEnabled = true;
            gridRight.GridProperties.TagFilterBackColor = Color.FromArgb(64, 128, 64);

            gridRight.GridProperties.RowDragMoveSource = GuiGridProperties.RowDragSource_DragSelectedThenActiveRow;
            // gridRight.GridProperties.RowDragMoveToTarget = GuiFullNameGridCenterTop + " " + GuiGridProperties.RowDragTarget_RowRoot + "," + GuiGridProperties.RowDragTarget_ToItem;
            gridRight.GridProperties.RowDragMoveToTarget = 
                GuiFullNameGridCenterTop + " " + GuiGridProperties.RowDragTarget_RowRoot + "," + GuiGridProperties.RowDragTarget_ToItemClassPrefix + "1190";

            gridRight.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
            gridRight.GraphProperties.BottomMarginPixel = 2;
            gridRight.GraphProperties.GraphLineHeight = 18;
            gridRight.GraphProperties.GraphLinePartialHeight = 36;
            gridRight.GraphProperties.GraphPosition = DataGraphPositionType.OnBackgroundProportional;
            gridRight.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift;
            gridRight.GraphProperties.Opacity = 192;
            gridRight.GraphProperties.TableRowHeightMax = 40;
            gridRight.GraphProperties.TableRowHeightMin = 22;
            gridRight.GraphProperties.UpperSpaceLogical = 0.2f;

            GuiDataTable guiTable = new GuiDataTable() { ClassId = PlanUnitC.ClassNumber };
            guiTable.RowCheckEnabled = true;
            // guiTable.RowCheckedImage = RES.Images.Actions16.DialogOk3Png;
            // guiTable.RowNonCheckedImage = RES.Images.Actions16.DialogNonAccept2Png;
            // guiTable.RowNonCheckedImage = GuiImage.Empty;
            guiTable.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId, TableClassId = PlanUnitC.ClassNumber });
            guiTable.AddColumn(new GuiDataColumn() { Name = "reference_subjektu", Title = "Číslo", Width = 85 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "nazev_subjektu", Title = "Jméno", Width = 200 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "note", Title = "Poznámka", Width = 120 });
            gridRight.RowTable = guiTable;

            // Data tabulky = Plánovací jednotky Pracovníci:
            foreach (PlanUnitC planUnitC in this.PersonDict.Values)
                this.AddPlanUnitCToGridRight(guiTable, planUnitC);

            this.GridRight = gridRight;
            this.MainPage.RightPanel.Grids.Add(gridRight);
        }
        /// <summary>
        /// Připraví v Gridu podporu pro Center zobrazení
        /// </summary>
        /// <param name="gridCenterWorkplace"></param>
        /// <param name="tagFilter"></param>
        /// <param name="timeAxis"></param>
        /// <param name="graph"></param>
        /// <param name="table"></param>
        protected void SetCenterGridProperties(GuiGrid gridCenterWorkplace, bool tagFilter, bool timeAxis, bool graph, bool table, string tableName)
        {
            if (tagFilter)
            {
                gridCenterWorkplace.GridProperties.TagFilterItemHeight = 26;
                gridCenterWorkplace.GridProperties.TagFilterItemMaxCount = 60;
                gridCenterWorkplace.GridProperties.TagFilterRoundItemPercent = 50;
                gridCenterWorkplace.GridProperties.TagFilterEnabled = true;
                gridCenterWorkplace.GridProperties.TagFilterBackColor = Color.FromArgb(64, 128, 64);
            }

            if (timeAxis)
            {
                gridCenterWorkplace.GraphProperties.AxisResizeMode = AxisResizeContentMode.ChangeScale;
                gridCenterWorkplace.GraphProperties.TimeAxisBackColor = Color.FromArgb(192, 224, 255);
                gridCenterWorkplace.GraphProperties.TimeAxisSegmentList = new List<GuiTimeAxisSegment>();
                gridCenterWorkplace.GraphProperties.TimeAxisSegmentList.AddRange(CreateHistory(this.TimeRangeTotal, Color.FromArgb(255, 192, 224)));
                gridCenterWorkplace.GraphProperties.TimeAxisSegmentList.AddRange(CreateWeekends(this.TimeRangeTotal, Color.FromArgb(255, 96, 32)));
                gridCenterWorkplace.GraphProperties.TimeAxisSegmentList.AddRange(CreateHolidays(this.TimeRangeTotal, Color.FromArgb(255, 32, 255)));
            }

            if (graph)
            {
                gridCenterWorkplace.GraphProperties.BottomMarginPixel = 2;
                gridCenterWorkplace.GraphProperties.GraphLineHeight = 20;
                gridCenterWorkplace.GraphProperties.GraphLinePartialHeight = 40;
                gridCenterWorkplace.GraphProperties.GraphPosition = DataGraphPositionType.InLastColumn;
                gridCenterWorkplace.GraphProperties.BackEffectEditable = GuiGraphItemBackEffectStyle.Pipe;
                gridCenterWorkplace.GraphProperties.BackEffectNonEditable = GuiGraphItemBackEffectStyle.Flat;
                gridCenterWorkplace.GraphProperties.GraphItemMinPixelWidth = 3;
                gridCenterWorkplace.GraphProperties.InteractiveChangeMode = AxisInteractiveChangeMode.Shift | AxisInteractiveChangeMode.Zoom;
                gridCenterWorkplace.GraphProperties.LogarithmicGraphDrawOuterShadow = 0.15f;
                gridCenterWorkplace.GraphProperties.LogarithmicRatio = 0.60f;
                gridCenterWorkplace.GraphProperties.Opacity = 255;
                gridCenterWorkplace.GraphProperties.TableRowHeightMin = 22;
                gridCenterWorkplace.GraphProperties.TableRowHeightMax = 260;
                gridCenterWorkplace.GraphProperties.TimeAxisMode = TimeGraphTimeAxisMode.Standard;
                gridCenterWorkplace.GraphProperties.UpperSpaceLogical = 1f;
                gridCenterWorkplace.GraphProperties.LinkColorStandard = Color.LightGreen;
                gridCenterWorkplace.GraphProperties.LinkColorWarning = Color.Yellow;
                gridCenterWorkplace.GraphProperties.LinkColorError = Color.DarkRed;
            }

            if (table)
            {
                GuiDataTable guiTable = new GuiDataTable() { Name = tableName , ClassId = PlanUnitC.ClassNumber };
                guiTable.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId, TableClassId = PlanUnitC.ClassNumber });
                guiTable.AddColumn(new GuiDataColumn() { Name = "reference_subjektu", Title = "Číslo", Width = 85 });
                guiTable.AddColumn(new GuiDataColumn() { Name = "nazev_subjektu", Title = "Název", Width = 200 });
                guiTable.AddColumn(new GuiDataColumn() { Name = "machines_count", Title = "Počet", Width = 45 });
                gridCenterWorkplace.RowTable = guiTable;
            }
        }
        /// <summary>
        /// Vrátí pole, obsahující jeden prvek <see cref="GuiTimeAxisSegment"/>, představující minulý čas.
        /// </summary>
        /// <param name="totalTimeRange"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected static List<GuiTimeAxisSegment> CreateHistory(GuiTimeRange totalTimeRange, Color backColor)
        {
            List<GuiTimeAxisSegment> result = new List<GuiTimeAxisSegment>();
            GuiTimeRange history = new GuiTimeRange(totalTimeRange.Begin, DateTime.Now);
            string toolTip = "MINULOST";
            GuiTimeAxisSegment segment = new GuiTimeAxisSegment() { TimeRange = history, BackColor = backColor, ToolTip = toolTip };
            result.Add(segment);
            return result;
        }
        /// <summary>
        /// Vrátí pole, obsahující <see cref="GuiTimeAxisSegment"/> pro víkendy v daném časovém intervalu.
        /// </summary>
        /// <param name="totalTimeRange"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
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
                    GuiDoubleRange sizeRange = new GuiDoubleRange(0.0f, 0.125f);
                    GuiInt32Range heightRange = new GuiInt32Range(1, 4);
                    DateTime begin = weekend.Begin.Date;
                    DateTime end = weekend.End.AddDays(-1d).Date;
                    string beginFmt = "d.";
                    if (begin.Month != end.Month) beginFmt += "MMMM ";
                    if (begin.Year != end.Year) beginFmt += "yyyy";
                    string endFmt = "d.MMMM yyyy";
                    string toolTip = "Víkend: " + begin.ToString(beginFmt) + " až " + end.ToString(endFmt);
                    GuiTimeAxisSegment segment = new GuiTimeAxisSegment() { TimeRange = weekend, BackColor = backColor, SizeRange = sizeRange, HeightRange = heightRange, ToolTip = toolTip };
                    result.Add(segment);
                }
                monday = monday.AddDays(7d).Date;
            }

            return result;
        }
        /// <summary>
        /// Vrátí pole, obsahující <see cref="GuiTimeAxisSegment"/> pro víkendy v daném časovém intervalu.
        /// </summary>
        /// <param name="totalTimeRange"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected static List<GuiTimeAxisSegment> CreateHolidays(GuiTimeRange totalTimeRange, Color backColor)
        {
            List<GuiTimeAxisSegment> result = new List<GuiTimeAxisSegment>();

            DateTime day = totalTimeRange.Begin.Date;
            while (true)
            {
                string name;
                if (IsHoliday(day, out name))
                {
                    GuiDoubleRange sizeRange = new GuiDoubleRange(0.0f, 0.125f);
                    GuiInt32Range heightRange = new GuiInt32Range(4, 7);
                    DateTime begin = day;
                    DateTime end = day.AddDays(1d).Date;
                    GuiTimeRange holiday = new GuiTimeRange(begin, end);
                    string fmt = "d.MMMM yyyy";
                    string toolTip = day.ToString(fmt) + " : " + name;
                    GuiTimeAxisSegment segment = new GuiTimeAxisSegment() { TimeRange = holiday, BackColor = backColor, SizeRange = sizeRange, HeightRange = heightRange, ToolTip = toolTip };
                    result.Add(segment);
                }
                day = day.AddDays(1d).Date;
                if (day >= totalTimeRange.End) break;
            }

            return result;
        }
        /// <summary>
        /// Je daný den svátkem? A kterým?
        /// </summary>
        /// <param name="day"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static bool IsHoliday(DateTime day, out string name)
        {
            name = null;
            string code = day.ToString("ddMM");
            switch (code)
            {
                case "0101":
                    name = "Nový rok, do důchodu krok!";
                    break;
                case "0105":
                    name = "Svátek práce";
                    break;
                case "0805":
                    name = "Konec II.WW";
                    break;
                case "0507":
                    name = "Cyril a Metudek";
                    break;
                case "0607":
                    name = "Jan z Husi";
                    break;
                case "2809":
                    name = "Svatý Vácslave, oroduj za nás";
                    break;
                case "2810":
                    name = "Byli jsme před Rakouskem - a jsme i po něm";
                    break;
                case "1711":
                    name = "Plyšák";
                    break;
                case "2412":
                    name = "Jéžišek";
                    break;
                case "2512":
                    name = "Vánoční svátek";
                    break;
                case "2612":
                    name = "Vánoční svátek deja-vu";
                    break;
            }
            if (name != null) return true;

            string codefull = day.ToString("ddMMyyyy");
            switch (codefull)
            {
                case "14042017":
                case "30032018":
                case "19042019":
                case "10042020":
                    name = "Velikonoční Pátek";
                    break;
                case "17042017":
                case "02042018":
                case "22042019":
                case "13042020":
                    name = "Velikonoční Pondělí";
                    break;
            }
            if (name != null) return true;

            return false;
        }
        /// <summary>
        /// Do dodané tabulky některého z gridů Center (=Work nebo Employee) přidá řádek za danou Plánovací jednotku, přidá jeho TagItems a graf z jeho operací.
        /// </summary>
        /// <param name="guiTable"></param>
        /// <param name="planUnitC"></param>
        /// <param name="gridType"></param>
        protected void AddPlanUnitCToGridCenter(GuiDataTable guiTable, PlanUnitC planUnitC, GridPositionType gridType)
        {
            guiTable.AddRow(planUnitC.CreateGuiRow(gridType));
        }
        /// <summary>
        /// Do dodané tabulky gridu Right (=Employee) přidá řádek za danou Plánovací jednotku, přidá jeho TagItems a graf z jeho směn.
        /// </summary>
        /// <param name="guiTable"></param>
        /// <param name="planUnitC"></param>
        protected void AddPlanUnitCToGridRight(GuiDataTable guiTable, PlanUnitC planUnitC)
        {
            guiTable.AddRow(planUnitC.CreateGuiRow(GridPositionType.Employee));
        }
        protected GuiData MainData;
        protected GuiPage MainPage;
        protected GuiGrid GridLeft;
        protected GuiGrid GridCenterWorkplace;
        protected GuiGrid GridCenterPersons;
        protected GuiGrid GridRight;
        protected DateTime DateTimeNow;
        protected DateTime DateTimeFirst;
        protected DateTime DateTimeLast;
        protected GuiTimeRange TimeRangeTotal;
        protected GuiTimeRange TimeRangeCurrent;
        #endregion
        #region Přeplánování dat na základě požadavků z GUI
        protected void MoveGraphItem(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            CompoundDataInfo[] dataInfos = this.SearchForData(guiRequest.GraphItemMove.MoveItems);
            CompoundDataInfo sourceRow = this.SearchForData(guiRequest.GraphItemMove.SourceRowId);
            CompoundDataInfo targetRow = this.SearchForData(guiRequest.GraphItemMove.TargetRowId);

            this.DataChanged = true;
            int time = this.Rand.Next(100, 350);
            System.Threading.Thread.Sleep(time);

            this.ApplyCommonToResponse(guiResponse);
        }
        /// <summary>
        /// Vymazání dat plánu
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void DeleteGraphItems(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            // Najdeme prvky práce, odpovídající označeným prvkům grafů:
            CompoundDataInfo[] dataInfos = this.SearchForData(guiRequest.CurrentState?.SelectedGraphItems);
            List<GuiGridItemId> removedList = new List<GuiGridItemId>();
            foreach (CompoundDataInfo dataInfo in dataInfos)
            {
                if (this.DeleteGraphItem(dataInfo))
                    removedList.Add(dataInfo.GuiGridItemId);
            }
            if (removedList.Count > 0)
                this.DataChanged = true;

            guiResponse.RefreshGraphItems = removedList.Select(g => new GuiRefreshGraphItem() { GridItemId = g }).ToList();
            this.ApplyCommonToResponse(guiResponse);
        }
        /// <summary>
        /// Metoda zjistí, zda daný datový prvek obsahuje údaje, které lze 
        /// </summary>
        /// <param name="dataInfo"></param>
        /// <returns></returns>
        protected bool DeleteGraphItem(CompoundDataInfo dataInfo)
        {
            if (dataInfo == null || dataInfo.WorkUnit == null) return false;
            if (dataInfo.WorkUnit.PlanUnitC == null || dataInfo.WorkUnit.PlanUnitC.PlanUnitType != PlanUnitType.Person) return false;
            return this.DeleteData(dataInfo.WorkUnit.RecordGid);
        }
        /// <summary>
        /// Uživatel nakreslil vztah mezi prvek A a B
        /// </summary>
        /// <param name="request"></param>
        /// <param name="guiResponse"></param>
        protected void AddInteractiveLink(GuiRequest request, GuiResponse guiResponse)
        {
            if (request.InteractiveDraw != null)
            {
                var draw = request.InteractiveDraw;
                guiResponse.ChangeLinks = new List<GuiGraphLink>();
                guiResponse.ChangeLinks.Add(new GuiGraphLink()
                {
                    TableName = draw.SourceItem.TableName,
                    ItemIdPrev = draw.SourceItem.GroupId,
                    ItemIdNext = draw.TargetItem.GroupId,
                    LinkType = GuiGraphItemLinkType.PrevEndToNextBegin,
                    RelationType = GuiGraphItemLinkRelation.OneLevel,
                    LinkWidth = 5
                });
            }
        }

        /// <summary>
        /// Do dané <see cref="GuiResponse"/> vloží hodnoty ClearLinks, ClearSelected a ToolbarItems[SaveData].Enable
        /// </summary>
        /// <param name="response"></param>
        /// <param name="clearLinks"></param>
        /// <param name="clearSelected"></param>
        /// <param name="saveEnabled"></param>
        protected void ApplyCommonToResponse(GuiResponse response, bool clearLinks = true, bool clearSelected = true, bool saveEnabled = true)
        {
            response.Common = new GuiResponseCommon() { ClearLinks = clearLinks, ClearSelected = clearSelected };
            response.ToolbarItems = new List<GuiToolbarItem>()
            {
                new GuiToolbarItem() { Name = "SaveData", Enable = saveEnabled }
            };
        }
        #region Vyhledání typových dat (CompoundDataInfo) na základě dat z GUI (GuiGridItemId)
        /// <summary>
        /// Vyhledá a vrátí informace o zadaných prvcích
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        protected CompoundDataInfo[] SearchForData(IEnumerable<GuiGridItemId> itemIds, Func<GuiGridItemId, bool> filterId = null, Func<CompoundDataInfo, bool> filterData = null)
        {
            List<CompoundDataInfo> dataItemList = new List<CompoundDataInfo>();
            if (itemIds != null)
            {
                bool hasFilterId = (filterId != null);
                bool hasFilterData = (filterData != null);
                foreach (GuiGridItemId itemId in itemIds)
                {
                    if (itemId != null && (!hasFilterId || (hasFilterId && filterId(itemId))))
                    {
                        CompoundDataInfo dataItem = this.SearchForData(itemId);
                        if (dataItem != null && (!hasFilterData || (hasFilterData && filterData(dataItem))))
                            dataItemList.Add(dataItem);
                    }
                }
            }
            return dataItemList.ToArray();
        }
        /// <summary>
        /// Vyhledá a vrátí informace o zadaném prvku
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns></returns>
        protected CompoundDataInfo SearchForData(GuiGridRowId rowId)
        {
            if (rowId == null) return null;

            GridPositionType gridType = this.SearchGridType(rowId.TableName);
            RecordClass row = this.SearchForData(rowId.RowId);

            CompoundDataInfo dataInfo = new CompoundDataInfo(rowId, gridType, row);

            return dataInfo;
        }
        /// <summary>
        /// Vyhledá a vrátí informace o zadaném prvku
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        protected CompoundDataInfo SearchForData(GuiGridItemId itemId)
        {
            if (itemId == null) return null;

            GridPositionType gridType = this.SearchGridType(itemId.TableName);
            RecordClass row = this.SearchForData(itemId.RowId);
            RecordClass group = this.SearchForData(itemId.GroupId);
            RecordClass item = this.SearchForData(itemId.ItemId);
            RecordClass data = this.SearchForData(itemId.DataId);

            CompoundDataInfo dataInfo = new CompoundDataInfo(itemId, gridType, row, group, item, data);

            return dataInfo;
        }
        /// <summary>
        /// Metoda najde a vrátí typové označení gridu podle jeho jména
        /// </summary>
        /// <param name="gridName"></param>
        /// <returns></returns>
        internal GridPositionType SearchGridType(string gridName)
        {
            switch (gridName)
            {
                case GuiFullNameGridLeft: return GridPositionType.ProductOrder;
                case GuiFullNameGridCenterTop: return GridPositionType.Workplace;
                case GuiFullNameGridCenterBottom: return GridPositionType.Person;
                case GuiFullNameGridRight: return GridPositionType.Employee;
            }
            return GridPositionType.None;
        }
        /// <summary>
        /// Metoda najde a vrátí datový objekt pro daný klíč
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal RecordClass SearchForData(GuiId id)
        {
            if (id == null) return null;
            switch (id.ClassId)
            {
                case ProductOrder.ClassNumber:
                    ProductOrder productOrder;
                    if (this.ProductOrderDict.TryGetValue(id, out productOrder)) return productOrder;
                    break;
                case ProductOperation.ClassNumber:
                    ProductOperation productOperation;
                    if (this.ProductOperationDict.TryGetValue(id, out productOperation)) return productOperation;
                    break;
                case ProductStructure.ClassNumber:
                    ProductStructure productStructure;
                    if (this.ProductStructureDict.TryGetValue(id, out productStructure)) return productStructure;
                    break;
                case PlanUnitC.ClassNumber:
                    PlanUnitC planUnitC;
                    if (this.WorkplaceDict.TryGetValue(id, out planUnitC)) return planUnitC;
                    if (this.PersonDict.TryGetValue(id, out planUnitC)) return planUnitC;
                    break;
                case WorkTime.ClassNumber:
                    WorkTime workTime;
                    if (this.WorkTimeDict.TryGetValue(id, out workTime)) return workTime;
                    break;
                case WorkUnit.ClassNumber:
                    WorkUnit workUnit;
                    if (this.WorkUnitDict.TryGetValue(id, out workUnit)) return workUnit;
                    break;
            }
            return null;
        }
        /// <summary>
        /// Zajistí smazání jednoho záznamu z interních struktur.
        /// Tato metoda neřeší návaznosti = pokud smazaný záznam obsahoval podřízené záznamy, nejsou touto metodou smazány.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool DeleteData(GuiId id)
        {
            bool result = false;
            if (id == null) return result;
            switch (id.ClassId)
            {
                case ProductOrder.ClassNumber:
                    result = (this.ProductOrderDict.ContainsKey(id));
                    if (result) this.ProductOrderDict.Remove(id);
                    break;
                case ProductOperation.ClassNumber:
                    result = (this.ProductOperationDict.ContainsKey(id));
                    if (result) this.ProductOperationDict.Remove(id);
                    break;
                case ProductStructure.ClassNumber:
                    result = (this.ProductStructureDict.ContainsKey(id));
                    if (result) this.ProductStructureDict.Remove(id);
                    break;
                case PlanUnitC.ClassNumber:
                    bool resultW = (this.WorkplaceDict.ContainsKey(id));
                    if (resultW) this.WorkplaceDict.Remove(id);
                    bool resultP = (this.PersonDict.ContainsKey(id));
                    if (resultP) this.PersonDict.Remove(id);
                    result = (resultW || resultP);
                    break;
                case WorkTime.ClassNumber:
                    result = (this.WorkTimeDict.ContainsKey(id));
                    if (result) this.WorkTimeDict.Remove(id);
                    break;
                case WorkUnit.ClassNumber:
                    result = (this.WorkUnitDict.ContainsKey(id));
                    if (result) this.WorkUnitDict.Remove(id);
                    break;
            }
            return result;
        }
        #region class CompoundDataInfo : Třída obsahující maximum datových záznamů dohledaných k určitému prvku GUI
        /// <summary>
        /// CompoundDataInfo : Třída obsahující maximum datových záznamů dohledaných k určitému prvku GUI
        /// </summary>
        protected class CompoundDataInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="guiGridItemId"></param>
            /// <param name="gridType"></param>
            /// <param name="row"></param>
            internal CompoundDataInfo(GuiGridRowId rowId, GridPositionType gridType, RecordClass row)
            {
                this.GuiGridRowId = rowId;
                this.FillData(gridType, row, null, null, null);
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="guiGridItemId"></param>
            /// <param name="gridType"></param>
            /// <param name="row"></param>
            /// <param name="group"></param>
            /// <param name="item"></param>
            /// <param name="data"></param>
            internal CompoundDataInfo(GuiGridItemId guiGridItemId, GridPositionType gridType, RecordClass row, RecordClass group = null, RecordClass item = null, RecordClass data = null)
            {
                this.GuiGridItemId = guiGridItemId;
                this.FillData(gridType, row, group, item, data);

            }
            /// <summary>
            /// Dodaná data do sebe naplní a dopočítá další data
            /// </summary>
            /// <param name="gridType"></param>
            /// <param name="row"></param>
            /// <param name="group"></param>
            /// <param name="item"></param>
            /// <param name="data"></param>
            private void FillData(GridPositionType gridType, RecordClass row, RecordClass group, RecordClass item, RecordClass data)
            {
                this.GridType = gridType;
                this.Row = row;
                this.Group = group;
                this.Item = item;
                this.Data = data;

                this.ProductOrder = First<ProductOrder>(this.Row, this.Group, this.Data, this.Item);
                this.ProductOperation = First<ProductOperation>(this.Row, this.Group, this.Data, this.Item);
                this.ProductStructure = First<ProductStructure>(this.Row, this.Group, this.Data, this.Item);
                this.PlanUnitC = First<PlanUnitC>(this.Row, this.Group, this.Data, this.Item);
                this.WorkTime = First<WorkTime>(this.Item, this.Group, this.Data, this.Row);
                this.WorkUnit = First<WorkUnit>(this.Item, this.Group, this.Data, this.Row);

                if (this.ProductOperation == null && this.ProductStructure != null) this.ProductOperation = this.ProductStructure.ProductOperation;
                if (this.ProductOrder == null && this.ProductOperation != null) this.ProductOrder = this.ProductOperation.ProductOrder;
            }
            /// <summary>
            /// Vrátí první z dodaných objektů, který není null a je odpovídajícího typu T
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="items"></param>
            /// <returns></returns>
            protected T First<T>(params object[] items) where T : class
            {
                foreach (object item in items)
                {
                    if (item != null && item is T) return item as T;
                }
                return null;
            }
            /// <summary>
            /// Identifikátor řádku
            /// </summary>
            public GuiGridRowId GuiGridRowId { get; private set; }
            /// <summary>
            /// Identifikátor prvku grafu
            /// </summary>
            public GuiGridItemId GuiGridItemId { get; private set; }
            /// <summary>
            /// Typ tabulky
            /// </summary>
            internal GridPositionType GridType { get; private set; }
            /// <summary>
            /// Záznam primární - odpovídá řádku tabulky
            /// </summary>
            public RecordClass Row { get; private set; }
            /// <summary>
            /// Záznam primární - odpovídá skupině grafického prvku
            /// </summary>
            public RecordClass Group { get; private set; }
            /// <summary>
            /// Záznam primární - odpovídá jednotlivému prvku
            /// </summary>
            public RecordClass Item { get; private set; }
            /// <summary>
            /// Záznam primární - odpovídá datovému záznamu prvku
            /// </summary>
            public RecordClass Data { get; private set; }

            /// <summary>
            /// Záznam typový - nalezený Výrobní příkaz
            /// </summary>
            public ProductOrder ProductOrder { get; private set; }
            /// <summary>
            /// Záznam typový - nalezená Operace VP
            /// </summary>
            public ProductOperation ProductOperation { get; private set; }
            /// <summary>
            /// Záznam typový - nalezená Komponenta VP
            /// </summary>
            public ProductStructure ProductStructure { get; private set; }
            /// <summary>
            /// Záznam typový - nalezená Kapacitní jednotka
            /// </summary>
            public PlanUnitC PlanUnitC { get; private set; }
            /// <summary>
            /// Záznam typový - nalezený čas směny
            /// </summary>
            public WorkTime WorkTime { get; private set; }
            /// <summary>
            /// Záznam typový - nalezený pracovní úsek
            /// </summary>
            public WorkUnit WorkUnit { get; private set; }
        }
        #endregion
        #endregion
        #endregion
        #region Přidání / Odebrání řádku grafu
        /// <summary>
        /// Přidá řádek do grafu Výroba nebo Kapacity
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void AddRowToGraph(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            float rnd = GetRandomRatio();
            if (rnd > 0.333f)
                this.AddRowToGraphProductOrder(guiRequest, guiResponse);
            else
                this.AddRowToGraphWorkplace(guiRequest, guiResponse);
        }
        /// <summary>
        /// Přidá řádek do grafu Výroba
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void AddRowToGraphProductOrder(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            // Čas počátky výroby = na pozici 10% časové osy:
            TimeRange axisTime = guiRequest.CurrentState.TimeAxisValue;
            DateTime? startTime = axisTime.GetPoint(0.1m);

            // Vyrobíme náhodný Výrobní příkaz (přidá se do this dat!):
            string name = GetRandom("Krabička pro hrací skříňku", "Dárková kazeta na mýdlo", "Dřevěné obložení dveří", "Botník na suché boty", "Botník na špinavé boty", "Kuchyňské prkénko 30cm", "Sada špalků pro automobil",
                                    "Dřevěné hrací kostky 6ks", "Žebřík na půdu", "Dřevěný hlavolam «Kostka»", "Vyřezávaný model letadla 1:48", "Regál do malého sklepa 1.4m x 0.75m", "Ozdobná držadla příborů",
                                    "Dětská stavebnice \"Skládací hrad\"", "Taburet ozdobný Empír, bílý", "Dřevěný paravan 3-dílný", "Stolek barevný pod černobílou TV", "Skříňka pro retro-rozhlasový přijímač",
                                    "Kulatý poklop latriny, zdobený", "Sada nábytku pro panenky M 1:4", "Psí bouda pro jezevčíka", "Škrabadlo pro kočku domácí");
            Color backColor = GetRandom(Color.DarkBlue, Color.Violet, Color.Turquoise, Color.SlateGray, Color.SaddleBrown, Color.RoyalBlue, Color.PowderBlue, Color.PapayaWhip, Color.PaleGreen, Color.OrangeRed, Color.OliveDrab, Color.Navy);
            decimal qty = GetRandom(2m, 5m, 6m, 9m, 12m, 15m, 21m, 24m, 30m, 36m, 48m, 64m, 90m, 120m);
            string tagText = GetRandom("stoly", "skříně", "víka", "pro děti", "jiné");
            ProductTpv tpv = GetRandom(ProductTpv.Simple, ProductTpv.Standard, ProductTpv.Standard, ProductTpv.Luxus, ProductTpv.Cooperation, ProductTpv.Standard, ProductTpv.Simple, ProductTpv.Standard);
            ProductOrder productOrder = this.CreateProductOrder(name, backColor, qty, tagText, tpv, startTime);
            this.DataChanged = true;

            // Zaplánujeme VP do Kapacitních jednotek, tím dojde k termínování jeho operací:
            this.PlanProductOrderToWorkplaces(productOrder, productOrder.DatePlanBegin);

            // Tento Výrobní příkaz vygeneruje standardně řádky GUI:
            List<GuiDataRow> rowList = new List<GuiDataRow>();
            productOrder.CreateGuiRows(rowList);

            // Zajistíme, že první komponenta bude mít Expanded svůj Parent node:
            GuiDataRow rowStruct = rowList.First(r => r.RowGuiId.ClassId == ProductStructure.ClassNumber);
            if (rowStruct != null)
                guiResponse.ExpandRows = new List<GuiGridRowId>() { new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = rowStruct.ParentRowGuiId } };

            // Řádky přidáme do response tak, aby se zařadily do tabulky vlevo:
            guiResponse.RefreshRows = rowList
                .Select(row => new GuiRefreshRow() { GridRowId = new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = row.RowGuiId }, RowData = row })
                .ToList();
        }
        /// <summary>
        /// Přidá řádek do grafu Kapacity
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void AddRowToGraphWorkplace(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            // Vyrobíme náhodné Pracoviště (přidá se do this dat!):
            string name = GetRandom("Ruční pilník dřevo", "Ruční rašple", "Ruční pilník jehlový",
                                    "Ruční pilka na železo", "Ruční pila lupenková", "Ruční pila ocaska",
                                    "Kladivo", "Dláto", "Šroubovák křížový", "Šroubovák plochý",
                                    "Bateriový šroubovák", "Aku vrtačka", "Vrtačka Narex",
                                    "Štětec vlasový", "Štětec plochý", "Štětec kulatý");
            string wplc = GetRandom(WP_DILN, WP_KONT, WP_KOOP, WP_LAKO, WP_PILA);
            string tags = GetRandom("ruční", "aku", "štětce");
            int machines = GetRandom(1, 1, 1, 2, 3);
            CalendarType calendar = GetRandom(CalendarType.Work5d1x8hR, CalendarType.Work5d1x8hO, CalendarType.Work5d1x8hR, CalendarType.Work5d1x8hO, CalendarType.Work5d2x8h);

            PlanUnitC workplace = this.CreatePlanUnitCWp(name, wplc, tags, machines, calendar, null);
            this.DataChanged = true;

            // Pracoviště vygeneruje standardní řádek GUI:
            GuiDataRow row = workplace.CreateGuiRow(GridPositionType.Workplace);

            // Řádky přidáme do response tak, aby se zařadily do tabulky uprostřed nahoře:
            guiResponse.RefreshRows = new List<GuiRefreshRow>() {
                new GuiRefreshRow() { GridRowId = new GuiGridRowId() { TableName = GuiFullNameGridCenterTop, RowId = row.RowGuiId }, RowData = row }
                };
        }
        /// <summary>
        /// Odebere řádek z grafu Výroba nebo Kapacity
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void RemoveRowFromGraph(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            float rnd = GetRandomRatio();
            if (rnd > 0.2f)
                this.RemoveRowFromGraphProductOrder(guiRequest, guiResponse);
            else
                this.RemoveRowFromGraphWorkplace(guiRequest, guiResponse);
        }
        /// <summary>
        /// Odebere řádek z grafu Výroba
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void RemoveRowFromGraphProductOrder(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            // Vybereme jeden náhodný VP z našeho seznamu:
            ProductOrder productOrder = GetRandom(this.ProductOrderDict.Values.ToArray());
            if (productOrder != null)
            {
                GuiId[] rows = productOrder.AllRecordsGid;
                guiResponse.RefreshRows = rows
                    .Select(r => new GuiRefreshRow() { GridRowId = new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = r }, RowData = null })
                    .ToList();

                this.ProductStructureDict.RemoveKeys(rows);
                this.ProductOperationDict.RemoveKeys(rows);
                this.ProductOrderDict.Remove(productOrder.RecordGid);

                this.DataChanged = true;
            }
        }
        /// <summary>
        /// Odebere řádek z grafu Kapacity
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void RemoveRowFromGraphWorkplace(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            // Vybereme jedno náhodné pracoviště:
            PlanUnitC workplace = GetRandom(this.WorkplaceDict.Values.ToArray());
            if (workplace != null)
            {
                guiResponse.RefreshRows = new List<GuiRefreshRow>() {
                    new GuiRefreshRow() { GridRowId = new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = workplace.RecordGid }, RowData = null }
                    };

                this.WorkplaceDict.Remove(workplace.RecordGid);

                this.DataChanged = true;
            }
        }
        /// <summary>
        /// Přidá komponentu k vybrané operaci
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void InsertStruct(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            ProductOperation operation = this.SearchOperation(guiRequest.ContextMenu.ContextItemId);
            if (operation == null)
            {   // Pokud bylo kliknuto na řádek nějaké komponenty, tak najdu operaci z ní:
                ProductStructure itemStructure = this.SearchStructure(guiRequest.ContextMenu.ContextItemId);
                operation = itemStructure?.ProductOperation;
            }
            if (operation == null) return;

            // Vytvoříme komponentu VP:
            string referName = GetRandom(
                "M4:Matka M4", "M6:Matka M6", "M8:Matka M8",
                "M4x30:Šroub M4 x 30", "M6x45:Šroub M6 x 45", "M8x60:Šroub M8 x 60",
                "Cx1060:Lak Celox lesklý", "S2040:Lak Syntol matný",
                "H2/50:Hřebík 2 x 50", "H3/70:Hřebík 3 x 70",
                "P4:Překližka 4mm");
            decimal qty = GetRandom(1m, 2m, 4m, 6m, 9m, 10m, 16m, 20m, 24m);
            string[] rns = referName.Split(':');
            ProductStructure structure = this.CreateProductStructure(operation, rns[0], rns[1], qty);

            // Vytvořím GUI řádek za danou komponentu:
            List<GuiDataRow> rows = new List<GuiDataRow>();
            structure.CreateGuiRows(rows);
            GuiDataRow rowStruct = rows[0];

            // Zajistíme, že tato komponenta bude mít Expanded svůj Parent node = operace:
            guiResponse.ExpandRows = new List<GuiGridRowId>() { new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = operation.RecordGid } };

            // Řádek komponenty přidáme do response tak, aby se zařadil do tabulky vlevo:
            guiResponse.RefreshRows = new List<GuiRefreshRow>() { new GuiRefreshRow()
            {
                GridRowId = new GuiGridRowId() {TableName = GuiFullNameGridLeft, RowId = structure.RecordGid },
                RowData = rowStruct
            } };
        }
        /// <summary>
        /// Odebere komponentu z vybrané operace 
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        protected void RemoveStruct(GuiRequest guiRequest, GuiResponse guiResponse)
        {
            ProductStructure structure = this.SearchStructure(guiRequest.ContextMenu.ContextItemId);
            if (structure != null)
            {
                this.RemoveStruct(guiRequest, guiResponse, structure);
            }
            else
            {
                ProductOperation operation = this.SearchOperation(guiRequest.ContextMenu.ContextItemId);
                if (operation != null)
                {
                    ProductStructure[] structures = operation.StructureList.ToArray();
                    foreach (ProductStructure s in structures)
                        this.RemoveStruct(guiRequest, guiResponse, s);
                }
            }
        }
        /// <summary>
        /// Zajistí odebrání dané komponenty z dat i z GUI vrstvy, pomocí <see cref="GuiResponse.RefreshRows"/>
        /// </summary>
        /// <param name="guiRequest"></param>
        /// <param name="guiResponse"></param>
        /// <param name="structure"></param>
        protected void RemoveStruct(GuiRequest guiRequest, GuiResponse guiResponse, ProductStructure structure)
        {
            this.RemoveStructure(structure);

            if (guiResponse.RefreshRows == null) guiResponse.RefreshRows = new List<GuiRefreshRow>();
            guiResponse.RefreshRows.Add(new GuiRefreshRow()
            {
                GridRowId = new GuiGridRowId() { TableName = GuiFullNameGridLeft, RowId = structure.RecordGid },
                RowData = null
            });
        }
        /// <summary>
        /// Zkusí najít Operaci VP pro některý z ID v daném komplexním identifikátoru <see cref="GuiGridItemId"/>
        /// </summary>
        /// <param name="guiItemId"></param>
        /// <returns></returns>
        protected ProductOperation SearchOperation(GuiGridItemId guiItemId)
        {
            if (guiItemId == null) return null;
            return this.SearchOperation(guiItemId.ItemId, guiItemId.DataId, guiItemId.RowId, guiItemId.GroupId);
        }
        /// <summary>
        /// Zkusí najít Operaci VP pro některý z dodaných <see cref="GuiId"/>
        /// </summary>
        /// <param name="guiIds"></param>
        /// <returns></returns>
        protected ProductOperation SearchOperation(params GuiId[] guiIds)
        {
            ProductOperation operation = null;
            if (guiIds != null)
            {
                foreach (GuiId guiId in guiIds)
                {
                    if (guiId != null && this.ProductOperationDict.TryGetValue(guiId, out operation)) break;
                }
            }
            return operation;
        }
        /// <summary>
        /// Zkusí najít Komponentu VP pro některý z ID v daném komplexním identifikátoru <see cref="GuiGridItemId"/>
        /// </summary>
        /// <param name="guiItemId"></param>
        /// <returns></returns>
        protected ProductStructure SearchStructure(GuiGridItemId guiItemId)
        {
            if (guiItemId == null) return null;
            return this.SearchStructure(guiItemId.ItemId, guiItemId.DataId, guiItemId.RowId, guiItemId.GroupId);
        }
        /// <summary>
        /// Zkusí najít Komponentu VP pro některý z dodaných <see cref="GuiId"/>
        /// </summary>
        /// <param name="guiIds"></param>
        /// <returns></returns>
        protected ProductStructure SearchStructure(params GuiId[] guiIds)
        {
            ProductStructure structure = null;
            if (guiIds != null)
            {
                foreach (GuiId guiId in guiIds)
                {
                    if (guiId != null && this.ProductStructureDict.TryGetValue(guiId, out structure)) break;
                }
            }
            return structure;
        }
        #endregion
        #region Test Serializace + Deserializace
        /// <summary>
        /// Metoda provede serializaci daného objektu a jeho následnou deserializaci, výsledek vrací.
        /// Testuje se tak přenositelnost všech dat.
        /// </summary>
        /// <param name="guiData"></param>
        /// <returns></returns>
        public static GuiData SerialDeserialData(GuiData guiData)
        {
            // return guiData;
            if (!System.Diagnostics.Debugger.IsAttached) return guiData;

            // Asol.Tools.WorkScheduler.Application.App.TracePriority = Application.TracePriority.Priority2_Lowest;

            GuiData result = null;

            string debugFile = System.IO.Path.Combine(Application.App.AppCodePath, "_debug_data.xml");
            if (System.IO.File.Exists(debugFile))
            {
                result = DeserializeFile(debugFile);
                if (result != null) return result;
            }

            // result = SerialDeserialData(guiData, XmlCompressMode.None);
            result = SerialDeserialData(guiData, XmlCompressMode.Compress);
            return result;
        }
        private static GuiData DeserializeFile(string file)
        {
            string serial = System.IO.File.ReadAllText(file, Encoding.UTF8);

            PersistArgs desArgs = new PersistArgs() { CompressMode = XmlCompressMode.Auto, DataContent = serial };
            object result = Persist.Deserialize(desArgs);
            return result as GuiData;
        }
        /// <summary>
        /// Metoda provede test serializace v daném režimu komprese
        /// </summary>
        /// <param name="guiData"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private static GuiData SerialDeserialData(GuiData guiData, XmlCompressMode mode)
        {
            string serial = null;
            string primitive = null;
            PersistArgs desArgs = null;
            GuiData resData = null;
            bool isCompress = (mode != XmlCompressMode.None);
            string compress = (isCompress ? "Compress" : "Plain");
            string runMode = (System.Diagnostics.Debugger.IsAttached ? "Debug" : "Run");

            using (var scopeS = Application.App.Trace.Scope("SchedulerDataSource", "SerialDeserialData", "Serialize", compress, runMode))
            {
                PersistArgs serArgs = (isCompress ? PersistArgs.Compressed : PersistArgs.Default);
                serArgs.SavePrimitivesContent = new StringBuilder();
                serArgs.DataHeapEnabled = true;
                serial = Persist.Serialize(guiData, serArgs);
                scopeS.AddItem("SerialLength: " + serial.Length.ToString());
                if (serArgs.SavePrimitivesLength > 0)
                    scopeS.AddItem("PrimitivesLength: " + serArgs.SavePrimitivesLength.ToString());
                primitive = ((serArgs.SavePrimitivesRunning && serArgs.SavePrimitivesContent != null)? serArgs.SavePrimitivesContent.ToString() : null);
            }

            SerialSaveData(serial, mode);
            SerialSaveData(primitive, ".txt");

            using (var scopeD = Application.App.Trace.Scope("SchedulerDataSource", "SerialDeserialData", "Deserialize", compress, runMode))
            {
                desArgs = new PersistArgs() { CompressMode = mode, DataContent = serial };
                object result = Persist.Deserialize(desArgs);
                resData = result as GuiData;
            }

            // Prověříme alespoň základní shodu dat:
            CheckEqualData(guiData, resData);

            return resData;
        }
        /// <summary>
        /// Kontrola shody dat
        /// </summary>
        /// <param name="guiData"></param>
        /// <param name="resData"></param>
        private static void CheckEqualData(GuiData guiData, GuiData resData)
        {
            if (resData == null)
                throw new FormatException("Serialize and Deserialize of GuiData fail; Deserialize process returns null value.");

            if (resData.ContextMenuItems == null && guiData.ContextMenuItems != null)
                throw new FormatException("Serialize and Deserialize of GuiData.ContextMenuItems fail; Deserialize process of ContextMenuItems returns null value.");
            if (resData.ContextMenuItems != null && guiData.ContextMenuItems != null && resData.ContextMenuItems.Count != guiData.ContextMenuItems.Count)
                throw new FormatException("Serialize and Deserialize of GuiData.ContextMenuItems fail; Deserialize process of ContextMenuItems returns bad Count items.");

            if (resData.Pages == null && guiData.Pages != null)
                throw new FormatException("Serialize and Deserialize of GuiData.Pages fail; Deserialize process of Pages returns null value.");
            if (resData.Pages != null && guiData.Pages != null && resData.Pages.Count != guiData.Pages.Count)
                throw new FormatException("Serialize and Deserialize of GuiData.Pages fail; Deserialize process of Pages returns bad Count items.");

            if (guiData.Pages == null || guiData.Pages.Count == 0) return;

            GuiPage guiPage = guiData.Pages.Pages[0];
            GuiPage resPage = resData.Pages.Pages[0];
            if (guiPage.MainPanel.Grids == null || guiPage.MainPanel.Grids.Count == 0) return;
            if (resPage.MainPanel.Grids == null || resPage.MainPanel.Grids.Count != guiPage.MainPanel.Grids.Count)
                throw new FormatException("Serialize and Deserialize of GuiData.Pages fail; Deserialize process of Pages[0] returns bad MainPanel.Grids.Count items.");

            GuiGrid guiGrid = guiPage.MainPanel.Grids[0];
            GuiGrid resGrid = resPage.MainPanel.Grids[0];
            if (resGrid == null)
                throw new FormatException("Serialize and Deserialize of GuiData.Pages fail; Deserialize process of GuiGrid (in Page[0].MainPanel.Grids) returns null.");
            if (resGrid.RowTable == null || resGrid.RowTable.Columns == null || resGrid.RowTable.Rows == null)
                throw new FormatException("Serialize and Deserialize of GuiGrid fail; Deserialize process of GuiDataTable (in Page[0].MainPanel.Grids[0].RowTable) returns null (Table or Columns or Rows).");
            if (resGrid.RowTable.Columns.Length != guiGrid.RowTable.Columns.Length)
                throw new FormatException("Serialize and Deserialize of GuiGrid fail; Deserialize process of GuiDataTable (in Page[0].MainPanel.Grids[0].RowTable) returns bad Columns count.");
            if (resGrid.RowTable.Rows.Length != guiGrid.RowTable.Rows.Length)
                throw new FormatException("Serialize and Deserialize of GuiGrid fail; Deserialize process of GuiDataTable (in Page[0].MainPanel.Grids[0].RowTable) returns bad Rows count.");

            // Najdu první GraphItem pro třídu WorkUnit = kus práce
            var guiItems = guiGrid.RowTable.Rows[0].Graph.GraphItems;
            var resItems = resGrid.RowTable.Rows[0].Graph.GraphItems;
            int index = guiItems.FindIndex(i => i.ItemId.ClassId == WorkUnit.ClassNumber);
            GuiGraphItem guiItem = (index >= 0 ? guiItems[index] : null);
            GuiGraphItem resItem = (index >= 0 ? resItems[index] : null);

            if (guiItem != null)
            {
                if (resItem == null)
                    throw new FormatException("Serialize and Deserialize of GuiGraphItem fail; can not find WorkUnit item.");
                if (guiItem.SkinDict != null)
                {
                    if (resItem.SkinDict == null)
                        throw new FormatException("Serialize and Deserialize of GuiGraphItem fail; SkinDict is null.");
                    if (guiItem.SkinDict.Count != resItem.SkinDict.Count)
                        throw new FormatException("Serialize and Deserialize of GuiGraphItem fail; SkinDict has unequal Count.");
                }

                var guiBColor = guiItem.SkinDefault.BackColor;
                var resBColor = resItem.SkinDefault.BackColor;
                if (!Nullable.Equals(guiBColor, resBColor))
                    throw new FormatException("Serialize and Deserialize of GuiGraphItem fail; SkinDefault has unequal BackColor.");

                if (guiItem.SkinDict.Count > 0)
                {
                    int key = guiItem.SkinDict.First().Key;
                    guiItem.SkinCurrentIndex = key;
                    resItem.SkinCurrentIndex = key;

                    if (!Nullable.Equals(guiItem.BackColor, resItem.BackColor))
                        throw new FormatException("Serialize and Deserialize of GuiGraphItem fail; Skin[" + key + "] has unequal BackColor.");

                    guiItem.SkinCurrentIndex = 0;
                    resItem.SkinCurrentIndex = 0;
                }
            }
        }
        /// <summary>
        /// Uloží dodaná data do souboru v adresáři Trace
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="mode"></param>
        private static void SerialSaveData(string serial, XmlCompressMode mode)
        {
            string extension = (mode == XmlCompressMode.None ? ".xml" : ".bin");
            SerialSaveData(serial, extension);
        }
        /// <summary>
        /// Uloží dodaná data do souboru v adresáři Trace
        /// </summary>
        /// <param name="content"></param>
        /// <param name="extension"></param>
        private static void SerialSaveData(string content, string extension)
        {
            if (String.IsNullOrEmpty(content)) return;
            string traceFile = Application.App.Trace.File;
            if (String.IsNullOrEmpty(traceFile)) return;
            string saveFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(traceFile), "SerialData") + extension;
            System.IO.File.WriteAllText(saveFile, content, Encoding.UTF8);
        }
        #endregion
        #region Konstanty, jména GUI prvků
        protected const string GuiNameData = "Data";
        protected const string GuiNameToolbarSubDay = "TlbSubDay";
        protected const string GuiNameToolbarAddDay = "TlbAddDay";
        protected const string GuiNameToolbarRePlan = "RePlan";
        protected const string GuiNameToolbarSaveData = "SaveData";
        protected const string GuiNameToolbarFilterLeft = "TlbApplyFilterMainToLeft";
        protected const string GuiNameToolbarResetFilters = "TlbResetAllFilters";
        protected const string GuiNameToolbarTrackBar = "TlbTrackBar";
        protected const string GuiNameToolbarResetTrackBar = "TlbResetTrackBar";
        protected const string GuiNameToolbarCreateLink = "TlbCreateLink";
        protected const string GuiNameToolbarShowColorSet1 = "TlbShowColorSet1";
        protected const string GuiNameToolbarShowColorSet2 = "TlbShowColorSet2";
        protected const string GuiNameToolbarShowColorSet3 = "TlbShowColorSet3";
        protected const string GuiNameToolbarShowColorSet4 = "TlbShowColorSet4";
        protected const string GuiNameToolbarAddRow1 = "TlbAddRow1";
        protected const string GuiNameToolbarDelRow1 = "TlbDelRow1";
        protected const string GuiNameToolbarShowBottomTable = "ShowBottomTable";
        protected const string GuiNameToolbarShowMainLink = "ShowMainLink";
        protected const string GuiNameToolbarSwitchEmployeeA = "SwitchEmployeeA";
        protected const string GuiNameToolbarSwitchEmployeeB = "SwitchEmployeeB";

        protected const string GuiNameMainPage = "MainPage";
        protected const string GuiNameGridLeft = "GridLeft";
        protected const string GuiNameGridCenterTop = "GridCenterTop";
        protected const string GuiNameGridCenterBottom = "GridCenterBottom";
        protected const string GuiNameGridRight = "GridRight";

        protected const string GuiNameInteractionSelectOperations = "InteractionSelectOperations";
        protected const string GuiNameInteractionFilterProductOrder = "InteractionFilterProductOrder";
        protected const string GuiNameInteractionShowColorSet = "InteractionShowColorSet";

        protected const string GuiNameLeftRowTable = "RowsLeft";
        protected const string GuiNameRowsCenterTop = "RowsCenterTop";
        protected const string GuiNameRowsCenterBottom = "RowsCenterBottom";

        protected const string GuiFullNameMainPage = GuiNameData + GuiNameDelimiter + GuiNamePages + GuiNameDelimiter + GuiNameMainPage;
        protected const string GuiFullNameLeftPanel = GuiFullNameMainPage + GuiNameDelimiter + GuiNameLeftPanel;
        protected const string GuiFullNameGridLeft = GuiFullNameLeftPanel + GuiNameDelimiter + GuiNameGridLeft;
        protected const string GuiFullNameMainPanel = GuiNameData + GuiNameDelimiter + GuiNamePages + GuiNameDelimiter + GuiNameMainPage + GuiNameDelimiter + GuiNameMainPanel;
        protected const string GuiFullNameGridCenterTop = GuiFullNameMainPanel + GuiNameDelimiter + GuiNameGridCenterTop;
        protected const string GuiFullNameGridCenterBottom = GuiFullNameMainPanel + GuiNameDelimiter + GuiNameGridCenterBottom;
        protected const string GuiFullNameRightPanel = GuiFullNameMainPage + GuiNameDelimiter + GuiNameRightPanel;
        protected const string GuiFullNameGridRight = GuiFullNameRightPanel + GuiNameDelimiter + GuiNameGridRight;

        protected const string GuiNameContextFixItem = "CtxFixItem";
        protected const string GuiNameContextUnFixItem = "CtxUnFixItem";
        protected const string GuiNameContextShowTime = "CtxShowTimeItem";
        protected const string GuiNameContextRemoveStruct1 = "CtxRemoveStruct1";
        protected const string GuiNameContextAddOneStruct = "CtxAddOneStruct";
        protected const string GuiNameContextRemoveStructs = "CtxRemoveStructs";
        protected const string GuiNameContextInsertStruct = "CtxInsertStruct";

        protected const string GuiNameDelimiter = "\\";
        protected const string GuiNamePages = "pages";
        protected const string GuiNameLeftPanel = "leftPanel";
        protected const string GuiNameMainPanel = "mainPanel";
        protected const string GuiNameRightPanel = "rightPanel";
        protected const string GuiNameBottomPanel = "bottomPanel";
        #endregion
        #region Generátor náhodných dat
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
        /// metoda vrací náhodné ratio z rozsahu 0 - 1 včetně
        /// </summary>
        /// <returns></returns>
        internal float GetRandomRatio()
        {
            float ratio = ((float)this.Rand.Next(0, 101)) / 100f;
            return ratio;
        }
        /// <summary>
        /// Metoda vrátí true s pravděpodobností danou procentem.
        /// Tedy: pokud je <paramref name="percent"/> = 5, pak vrátí true v 5 případech z 100 volání této metody.
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        internal bool IsExpectable(int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            return (this.Rand.Next(0, 101) <= percent);
        }
        /// <summary>
        /// Vrátí jeden z prvků daného pole
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        internal T GetRandom<T>(params T[] items)
        {
            int count = (items != null ? items.Length : 0);
            if (count == 0) return default(T);
            return items[this.Rand.Next(count)];
        }
        /// <summary>
        /// Vrátí true, s pravděpodobností probability %.
        /// Pokud probability = 10, pak 10 případů ze 100 vrátí true, 90% bude false.
        /// </summary>
        /// <param name="probability"></param>
        /// <returns></returns>
        internal bool Pbb(int probability)
        {
            return (this.Rand.Next(0, 100) <= probability);
        }
        /// <summary>
        /// Generátor náhodných hodnot
        /// </summary>
        protected Random Rand;
        #endregion
        #region IAppHost : vyvolání funkce z Pluginu do AppHost
        /// <summary>
        /// Metoda je volána v threadu na pozadí, má za úkol zpracovat daný požadavek (parametr).
        /// </summary>
        /// <param name="requestArgs"></param>
        protected void AppHostExecCommand(AppHostRequestArgs requestArgs)
        {
            AppHostResponseArgs responseArgs = new AppHostResponseArgs(requestArgs);
            responseArgs.GuiResponse = new GuiResponse();
            int time;
            GuiTimeRange timeRange = requestArgs.Request?.CurrentState?.TimeAxisValue;
            if (requestArgs != null)
            {
                switch (requestArgs.Request.Command)
                {
                    case GuiRequest.COMMAND_KeyPress:
                        if (requestArgs.Request.KeyPress != null && requestArgs.Request.KeyPress.KeyData == Keys.Delete)
                            this.DeleteGraphItems(requestArgs.Request, responseArgs.GuiResponse);
                        break;

                    case GuiRequest.COMMAND_GraphItemMove:
                        this.MoveGraphItem(requestArgs.Request, responseArgs.GuiResponse);
                        break;

                    case GuiRequest.COMMAND_GraphItemResize:
                        this.DataChanged = true;
                        time = this.Rand.Next(100, 350);
                        System.Threading.Thread.Sleep(time);
                        this.ApplyCommonToResponse(responseArgs.GuiResponse);
                        break;

                    case GuiRequest.COMMAND_RowDragDrop:
                        var rdm = requestArgs.Request.RowDragMove;
                        var ti = (rdm.TargetItem != null ? rdm.TargetItem : (rdm.TargetGroup != null && rdm.TargetGroup.Length > 0 ? rdm.TargetGroup[0] : null));
                        this.DataChanged = true;
                        time = this.Rand.Next(100, 350);
                        System.Threading.Thread.Sleep(time);
                        this.ApplyCommonToResponse(responseArgs.GuiResponse);
                        break;

                    case GuiRequest.COMMAND_InteractiveDraw:
                        this.AddInteractiveLink(requestArgs.Request, responseArgs.GuiResponse);
                        break;

                    case GuiRequest.COMMAND_ToolbarClick:
                        switch (requestArgs.Request.ToolbarItem.Name)
                        {
                            case GuiNameToolbarSubDay:
                                timeRange = new GuiTimeRange(timeRange.Begin.AddDays(-1d), timeRange.End.AddDays(-1d));
                                responseArgs.GuiResponse.Common = new GuiResponseCommon() { TimeAxisValue = timeRange };
                                break;
                            case GuiNameToolbarAddDay:
                                timeRange = new GuiTimeRange(timeRange.Begin.AddDays(1d), timeRange.End.AddDays(1d));
                                responseArgs.GuiResponse.Common = new GuiResponseCommon() { TimeAxisValue = timeRange };
                                break;
                            case GuiNameToolbarRePlan:
                                time = this.Rand.Next(500, 5000);
                                System.Threading.Thread.Sleep(time);
                                responseArgs.GuiResponse.Dialog = GetDialog("Data jsou zaplánovaná.", GuiDialogButtons.Ok);
                                break;
                            case GuiNameToolbarSaveData:
                                time = this.Rand.Next(500, 5000);
                                System.Threading.Thread.Sleep(time);
                                this.DataChanged = false;
                                responseArgs.GuiResponse.Dialog = GetDialog("Data jsou uložena.", GuiDialogButtons.Ok);
                                responseArgs.GuiResponse.ToolbarItems = new List<GuiToolbarItem>()
                                {
                                    new GuiToolbarItem() { Name = "SaveData", Enable = false }
                                };

                                break;
                            case GuiNameToolbarAddRow1:
                                this.AddRowToGraph(requestArgs.Request, responseArgs.GuiResponse);
                                break;
                            case GuiNameToolbarDelRow1:
                                this.RemoveRowFromGraph(requestArgs.Request, responseArgs.GuiResponse);
                                break;
                        }
                        break;

                    case GuiRequest.COMMAND_ContextMenuClick:
                        switch (requestArgs.Request.ContextMenu.ContextMenuItem.Name)
                        {
                            case GuiNameContextRemoveStruct1:
                            case GuiNameContextRemoveStructs:
                                this.RemoveStruct(requestArgs.Request, responseArgs.GuiResponse);
                                break;
                            case GuiNameContextInsertStruct:
                                this.InsertStruct(requestArgs.Request, responseArgs.GuiResponse);
                                break;

                            default:
                                Application.App.ShowInfo(
                                    "Někdo chce provést funkci: " + requestArgs.Request.ContextMenu.ContextMenuItem.Title + Environment.NewLine +
                                    "Pro prvek grafu: " + requestArgs.Request.ContextMenu.ContextItemId.ToString() + Environment.NewLine +
                                    "V čase: " + requestArgs.Request.ContextMenu.ClickTime);
                                break;
                        }
                        break;
                    case GuiRequest.COMMAND_OpenRecords:
                        Application.App.ShowInfo("Bohužel neumím otevřít záznam, jehož ID=" + requestArgs.Request.RecordsToOpen.ToString(";"));
                        break;

                    case GuiRequest.COMMAND_QueryCloseWindow:
                        if (this.DataChanged)
                        {
                            // Chci si otestovat malou prodlevu před zobrazením dialogu:
                            time = this.Rand.Next(100, 750);
                            System.Threading.Thread.Sleep(time);
                            responseArgs.GuiResponse.Dialog = GetDialog(GetMessageSaveData(), GuiDialogButtons.YesNoCancel, GuiDialog.DialogIconQuestion);
                            responseArgs.GuiResponse.CloseSaveData = new GuiSaveData() { AutoSave = true, BlockGuiTime = TimeSpan.FromSeconds(20d), BlockGuiMessage = "Probíhá ukládání dat...\r\nData se právě ukládají do databáze.\r\nJakmile budou uložena, dostanete od nás spěšnou sovu." };
                        }
                        break;

                    case GuiRequest.COMMAND_SaveBeforeCloseWindow:
                        // Chci si otestovat malou prodlevu před skončením:
                        time = this.Rand.Next(1500, 12000);
                        System.Threading.Thread.Sleep(time);
                        if (this.Rand.Next(0, 100) <= 65)
                            responseArgs.Result = AppHostActionResult.Success;
                        else
                            responseArgs.Result = AppHostActionResult.Failure;

                        responseArgs.GuiResponse.Dialog = GetDialog("Došlo k chybě. Přejete si skončit i bez uložení dat?", GuiDialogButtons.YesNo);
                        break;

                }
            }
            if (requestArgs.CallBackAction != null)
                requestArgs.CallBackAction(responseArgs);
        }
        /// <summary>
        /// Vstupní metoda pro řešení requestů z GUI vrstvy
        /// </summary>
        /// <param name="requestArgs"></param>
        /// <returns></returns>
        AppHostResponseArgs IAppHost.CallAppHostFunction(AppHostRequestArgs requestArgs)
        {
            this.AppHostAddRequest(requestArgs);
            return null;              // Jsme asynchronní AppHost, vracíme null.
        }
        /// <summary>
        /// Prvotní inicializace výkonného threadu OnBackground, v němž se fyzicky provádí zpracování requestů z GUI vrstvy
        /// </summary>
        protected void IAppHostInit()
        {
            this.AppHostThread = new System.Threading.Thread(this.AppHostMainLoop);
            this.AppHostThread.Name = "AppHost_BackThread";
            this.AppHostThread.IsBackground = true;

            this.AppHostSemaphore = new System.Threading.AutoResetEvent(false);

            this.AppHostQueue = new Queue<AppHostRequestArgs>();
            this.AppHostRunning = true;
            this.AppHostThread.Start();
        }
        /// <summary>
        /// Main smyčka threadu AppHost, v této metodě běží celý thread na pozadí.
        /// Metoda je ukočena nastavením <see cref="AppHostRunning"/> na false (a budíčkem pomocí semaforu <see cref="AppHostSemaphore"/>.
        /// </summary>
        protected void AppHostMainLoop()
        {
            while (this.AppHostRunning)
            {
                AppHostRequestArgs requestArgs = null;
                lock (this.AppHostQueue)
                {
                    if (this.AppHostQueue.Count > 0)
                        requestArgs = this.AppHostQueue.Dequeue();
                }

                if (requestArgs != null)
                {   // Máme-li požadavek, pak jej zpracujeme:
                    this.AppHostExecCommand(requestArgs);
                    // A ani nechodíme spát, rovnou zjistíme, zda nemáme alší požadavek...
                }
                else
                {   // Nemáme žádný požadavek na práci => jdeme si na moment zdřímnout, a pak se zase podíváme.
                    this.AppHostSemaphore.WaitOne(500);
                    // Když by mezitím přišla nová práce, tak nás její přícho probudí (viz konec metody AppHostAddRequest())
                }
            }
        }
        /// <summary>
        /// Metoda přidá dodaný požadavek do fronty ke zpracování v threadu na pozadí
        /// </summary>
        /// <param name="requestArgs"></param>
        protected void AppHostAddRequest(AppHostRequestArgs requestArgs)
        {
            if (requestArgs == null) return;
            lock (this.AppHostQueue)
            {
                this.AppHostQueue.Enqueue(requestArgs);
            }
            this.AppHostSemaphore.Set();
        }
        /// <summary>
        /// true = data obsahují změnu
        /// </summary>
        protected bool DataChanged { get; private set; }
        /// <summary>
        /// Vrací RTF text pro zprávu "Ukončujete aplikaci, ale neuložili jste svoje změny v datech..."
        /// </summary>
        /// <returns></returns>
        protected static string GetMessageSaveData()
        {
            string rtf = @"{\rtf1\ansi\ansicpg1250\deff0\nouicompat\deflang1029{\fonttbl{\f0\fnil\fcharset238 Calibri;}{\f1\fnil\fcharset0 Calibri;}}
{\colortbl ;\red128\green0\blue64;}
{\*\generator Riched20 10.0.17763}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\b\f0\fs32 Ukon\'e8en\f1\lang1033\'ed\f0\lang1029  aplikace\par
\b0\fs24 Ukon\'e8ujete aplikaci, ale \b neulo\'9eili jste svoje zm\'ecny v datech\b0 .\par
\fs22 M\f1\lang1033\'e1\f0\lang1029  b\f1\lang1033\'fd\f0\lang1029 t p\'f8ed ukon\'e8en\f1\lang1033\'ed\f0\lang1029 m \cf1\b provedeno ulo\'9een\f1\lang1033\'ed\f0\lang1029  dat\cf0\b0 ?\line\i (Ano = ulo\'9eit a skon\'e8it, Ne = neukl\f1\lang1033\'e1\f0\lang1029 dat a skon\'e8it, Storno = neukl\f1\lang1033\'e1\f0\lang1029 dat a neskoon\'e8it)\i0\par
}
 ";
            return rtf;
        }
        /// <summary>
        /// Vrátí GuiDialog
        /// </summary>
        /// <param name="message"></param>
        /// <param name="guiButtons"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        protected static GuiDialog GetDialog(string message, GuiDialogButtons guiButtons, GuiImage icon = null)
        {
            return new GuiDialog()
            {
                Message = message,
                Buttons = guiButtons,
                Icon = icon
            };
        }
        /// <summary>
        /// Metoda zastaví běh threadu na pozadí
        /// </summary>
        protected void AppHostStop()
        {
            this.AppHostRunning = false;
            this.AppHostSemaphore.Set();
        }
        protected System.Threading.Thread AppHostThread;
        protected System.Threading.AutoResetEvent AppHostSemaphore;
        protected bool AppHostRunning;
        protected Queue<AppHostRequestArgs> AppHostQueue;
        #endregion
    }
    #region class ProductOrder : Výrobní příkaz
    /// <summary>
    /// ProductOrder : Výrobní příkaz
    /// </summary>
    public class ProductOrder : SubjectClass
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public ProductOrder(SchedulerDataSource dataSource)
            : base(dataSource)
        {
            this.OperationList = new List<ProductOperation>();
        }
        public const int ClassNumber = 1188;
        public override int ClassId { get { return ClassNumber; } }
        public List<ProductOperation> OperationList { get; set; }
        public IEnumerable<string> TagTexts { get; set; }
        public IEnumerable<GuiTagItem> TagItems { get { IEnumerable<string> tt = this.TagTexts; return (tt == null ? new GuiTagItem[0] : tt.Select(text => new GuiTagItem() { RowId = this.RecordGid, TagText = text }).ToArray()); } }
        public DateTime DatePlanBegin { get; set; }
        public GuiTimeRange Time { get; set; }
        public decimal Qty { get; set; }
        public Color BackColor { get; set; }
        /// <summary>
        /// Obsahuje pole všech <see cref="GuiId"/>, které spadají do tohoto VP: samotný VP, jeho operace a jejich komponenty
        /// </summary>
        public GuiId[] AllRecordsGid
        {
            get
            {
                List<GuiId> allRecordsGid = new List<GuiId>();
                allRecordsGid.Add(this.RecordGid);
                allRecordsGid.AddRange(this.OperationList.SelectMany(op => op.AllRecordsGid));
                return allRecordsGid.ToArray();
            }
        }
        /// <summary>
        /// Metoda vygeneruje řádky za this instanci a za svoje podřízené řádky, a přidá je do předané kolekce
        /// </summary>
        /// <returns></returns>
        public void CreateGuiRows(ICollection<GuiDataRow> list)
        {
            GuiId rowGid = this.RecordGid;
            GuiIdText name = new GuiIdText() { GuiId = new GuiId(343, this.RecordId), Text = this.Name };
            GuiDataRow row = new GuiDataRow(rowGid, this.Refer, name, this.Qty);
            row.RowGuiId = rowGid;
            row.TagItems = new List<GuiTagItem>(this.TagItems);
            row.Graph = this.CreateGuiGraph();
            list.Add(row);

            foreach (ProductOperation productOperation in this.OperationList)
                productOperation.CreateGuiRows(list);
        }
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
        /// Vytvoří prvky <see cref="GuiGraphLink"/> za své operace, a přidá je do daného Listu
        /// </summary>
        /// <param name="graphLinks"></param>
        public void AddGuiGraphLinksTo(List<GuiGraphLink> graphLinks)
        {
            if (this.OperationList == null || this.OperationList.Count <= 1) return;
            ProductOperation[] operations = this.OperationList.Where(op => op.HasLink).ToArray();
            int length = operations.Length;
            if (length < 2) return;
            for (int i = 1; i < length; i++)
                graphLinks.Add(ProductOperation.CreateGuiLink(operations[i - 1], operations[i]));
        }
    }
    #endregion
    #region class ProductOperation : Operace výrobního příkazu
    /// <summary>
    /// ProductOperation : Operace výrobního příkazu
    /// </summary>
    public class ProductOperation : SubjectClass
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public ProductOperation(SchedulerDataSource dataSource)
            : base(dataSource)
        {
            this.Height = 1f;
            this.BackColor = Color.FromArgb(64, 64, 160);
            this.StructureList = new List<ProductStructure>();
            this.WorkUnitDict = new Dictionary<GuiId, WorkUnit>();
        }
        public const int ClassNumber = 1190;
        public override int ClassId { get { return ClassNumber; } }
        /// <summary>
        /// Vztah na Výrobní příkaz (=hlavička pro více operací)
        /// </summary>
        public ProductOrder ProductOrder { get; set; }
        /// <summary>
        /// Soupis komponent
        /// </summary>
        public List<ProductStructure> StructureList { get; set; }
        public int Line { get; set; }
        public string ToolTip { get; set; }
        public decimal Qty { get; set; }
        public float Height { get; set; }
        public bool IsFixed { get; set; }
        public string Icon { get; set; }
        public string WorkPlace { get; set; }
        public TimeSpan TBc { get; set; }
        public TimeSpan TAc { get; set; }
        public TimeSpan TEc { get; set; }
        /// <summary>
        /// Součet <see cref="TBc"/> + <see cref="TAc"/> + <see cref="TEc"/>
        /// </summary>
        public TimeSpan TTc { get { return this.TBc + this.TAc + this.TEc; } }
        public GuiTimeRange Time { get; set; }
        public GuiTimeRange TimeTBc { get; set; }
        public GuiTimeRange TimeTAc { get; set; }
        public GuiTimeRange TimeTEc { get; set; }
        public Color BackColor { get; set; }
        /// <summary>
        /// Obsahuje pole všech <see cref="GuiId"/>, které spadají do této Operace VP: samotná Operace, a její komponenty
        /// </summary>
        public GuiId[] AllRecordsGid
        {
            get
            {
                List<GuiId> allRecordsGid = new List<GuiId>();
                allRecordsGid.Add(this.RecordGid);
                allRecordsGid.AddRange(this.StructureList.Select(st => st.RecordGid));
                return allRecordsGid.ToArray();
            }
        }
        /// <summary>
        /// Dictionary obsahující jednotky práce za tuto operaci
        /// </summary>
        public Dictionary<GuiId, WorkUnit> WorkUnitDict { get; private set; }
        /// <summary>
        /// Metoda vygeneruje řádky za this instanci a za svoje podřízené řádky, a přidá je do předané kolekce
        /// </summary>
        /// <returns></returns>
        public void CreateGuiRows(ICollection<GuiDataRow> list)
        {
            GuiId rowGid = this.RecordGid;
            GuiDataRow row = new GuiDataRow(rowGid, this.Refer, this.Name, this.Qty);
            row.RowGuiId = rowGid;
            row.ParentRowGuiId = this.ProductOrder.RecordGid;
            list.Add(row);

            foreach (ProductStructure productStructure in this.StructureList)
                productStructure.CreateGuiRows(list);
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
        /// <param name="flowTime"></param>
        /// <param name="direction"></param>
        /// <param name="workplace"></param>
        /// <param name="person"></param>
        public void PlanTimeOperation(ref DateTime flowTime, Data.Direction direction, PlanUnitC workplace, PlanUnitC person)
        {
            if (workplace == null) return;
            if (!(direction == Direction.Positive || direction == Direction.Negative))
                throw new Asol.Tools.WorkScheduler.Application.GraphLibCodeException("Směr plánu musí být pouze Positive nebo Negative.");

            this.TimeTBc = this.PlanTimePhase(ref flowTime, direction, workplace, person, this.TBc, this.BackColor.Morph(Color.Green, 0.25f));
            this.TimeTAc = this.PlanTimePhase(ref flowTime, direction, workplace, person, this.TAc, this.BackColor);
            this.TimeTEc = this.PlanTimePhase(ref flowTime, direction, workplace, person, this.TEc, this.BackColor.Morph(Color.Black, 0.25f));
            this.Time = new GuiTimeRange(this.TimeTBc.Begin, this.TimeTEc.End);
        }
        /// <summary>
        /// Uloží jednotku práce pro daný čas do pracoviště
        /// </summary>
        /// <param name="flowTime"></param>
        /// <param name="direction"></param>
        /// <param name="workplace"></param>
        /// <param name="person"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        protected GuiTimeRange PlanTimePhase(ref DateTime flowTime, Data.Direction direction, PlanUnitC workplace, PlanUnitC person, TimeSpan time, Color backColor)
        {
            if (workplace == null || time.Ticks <= 0L) return new GuiTimeRange(flowTime, flowTime);

            DateTime? phaseBegin = null;
            DateTime? phaseEnd = null;
            DateTime startTime = flowTime;
            for (int w = 0; w < 2; w++)
            {
                PlanUnitC[] planUnits = (person != null ? new PlanUnitC[] { workplace, person } : new PlanUnitC[] { workplace });
                flowTime = startTime;
                for (int t = 0; t < 25; t++)         // jenom Timeout
                {
                    if (time.Ticks <= 0L) break;     // Je hotovo.
                    bool isPlanned = this.PlanTimePart(ref flowTime, direction, ref phaseBegin, ref phaseEnd, planUnits, ref time, backColor);
                    if (!isPlanned) break;           // Nejde to.
                }
                if (time.Ticks <= 0L) break;         // Je hotovo.
                // Pokud tato smyčka byla plánována již bez pracovníka (= pouze s pracovištěm), skončíme:
                if (person == null) break;
                // Další smyčku pojedu bez pracovníka:
                person = null;
            }

            if (!phaseBegin.HasValue || !phaseEnd.HasValue) return new GuiTimeRange(startTime, startTime);
            return new GuiTimeRange(phaseBegin.Value, phaseEnd.Value);
        }
        /// <summary>
        /// Zaplánuje jednotku času
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="begin"></param>
        /// <param name="flowTime"></param>
        /// <param name="direction"></param>
        /// <param name="end"></param>
        /// <param name="planUnits"></param>
        /// <param name="needTime"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected bool PlanTimePart(ref DateTime flowTime, Data.Direction direction, ref DateTime? phaseBegin, ref DateTime? phaseEnd, PlanUnitC[] planUnits, ref TimeSpan needTime, Color backColor)
        {
            // Provedu přípravu pracovních časů pro každou PlanUnitC tak, aby její CurrentWorkTime začínal v flowTime nebo později:
            foreach (PlanUnitC planUnit in planUnits)
                planUnit.FindTime(flowTime, direction);

            // Určím ty PlanUnitC, které v dané době ještě mají pracovní směny:
            PlanUnitC[] workingUnits = planUnits
                .Where(p => p.CurrentWorkTime != null)
                .ToArray();
            if (workingUnits.Length == 0) return false;    // Pokud nikdo v dané době nemá už zaplánované pracovní směny, skončím.

            // Určím jednotlivé pracovní časy:
            TimeRange[] workingTimes = workingUnits
                .Select(p => (TimeRange)p.CurrentWorkTime)
                .ToArray();

            // Určím společný pracovní čas:
            TimeRange commonTime = workingTimes.TimeIntersect();
            if (commonTime == null) return false;          // V případě, že vstupní kolekce je prázdná nebo obsahuje pouze null hodnoty.

            // Údaj commonTime obsahuje Begin = Max(všech Begin), a End = Min(všech End), může tedy mít záporný interval (kdy Begin > End).
            DateTime begin = commonTime.Begin.Value;
            DateTime end = commonTime.End.Value;
            if (begin >= end)
            {   // Společný čas je nereálný nebo prázdný => zajistíme, že proběhne další kolo, ale od nového flowTime:
                flowTime = (direction == Direction.Positive ? begin : end);
                return true;
            }

            // Máme určen nějaký (společný) čas na práci => určíme, kolik ho reálně potřebujeme pro naši operaci (a fázi):
            TimeSpan dispTime = commonTime.Size.Value;
            TimeSpan usedTime = dispTime;
            if (needTime < dispTime)
            {
                usedTime = needTime;
                switch (direction)
                {
                    case Direction.Positive:
                        end = begin + usedTime;
                        break;
                    case Direction.Negative:
                        begin = end - usedTime;
                        break;
                }
            }
            needTime -= usedTime;

            // Vygenerujeme pracovní časy do odpovídajících Kapacitních jednotek:
            GuiTimeRange workTimeRange = new GuiTimeRange(begin, end);
            foreach (PlanUnitC workingUnit in workingUnits)
                workingUnit.AddUnitTime(this.CreateUnitTime(workingUnit, workTimeRange, backColor));

            // Řešíme čas celé fáze (phaseBegin - phaseEnd), a posun flowTime - a to podle směru plánu:
            switch (direction)
            {
                case Direction.Positive:
                    if (!phaseBegin.HasValue) phaseBegin = begin;
                    phaseEnd = end;
                    flowTime = end;
                    break;
                case Direction.Negative:
                    if (!phaseEnd.HasValue) phaseEnd = end;
                    phaseBegin = begin;
                    flowTime = begin;
                    break;
            }
            return true;
        }
        /// <summary>
        /// Vytvoří a vrátí jednotku práce
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="planUnitC"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        protected WorkUnit CreateUnitTime(PlanUnitC planUnitC, GuiTimeRange time, Color backColor)
        {
            WorkUnit workUnit = new WorkUnit(this.DataSource)
            {
                Operation = this,
                PlanUnitC = planUnitC,
                Time = time,
                Height = this.Height,
                BackColor = backColor,
                IsEditable = true,
                ToolTip = this.ToolTip
            };
            workUnit.Text = (workUnit.Height <= 1f ? this.ReferName : this.ReferName + "\r\n" + this.ProductOrder.ReferName);
            this.WorkUnitDict.Add(workUnit.RecordGid, workUnit);
            return workUnit;
        }
        /// <summary>
        /// Obsahuje true, pokud tato operace má mít Link na sousední operaci (tj. pokud má nějaký prvek, který se zobrazuje v grafu MainTop)
        /// </summary>
        public bool HasLink
        {
            get
            {
                return (this.WorkUnitDict != null && this.WorkUnitDict.Count > 0);
            }
        }
        /// <summary>
        /// Vygeneruje a vrátí vztah mezi dvěma operacemi (Link)
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static GuiGraphLink CreateGuiLink(ProductOperation prev, ProductOperation next)
        {
            int width = 1 + (int)(prev.ProductOrder.Qty / 6m);
            if (width < 1) width = 1;
            if (width > 4) width = 4;
            GuiGraphLink link = new GuiGraphLink()
            {
                ItemIdPrev = prev?.RecordGid,
                ItemIdNext = next?.RecordGid,
                LinkType = GuiGraphItemLinkType.PrevEndToNextBegin,
                RelationType = GuiGraphItemLinkRelation.OneLevel,
                LinkWidth = width
            };
            return link;
        }
    }
    #endregion
    #region class ProducStructure : Komponenta výrobního příkazu
    /// <summary>
    /// ProducStructure : Komponenta  výrobního příkazu
    /// </summary>
    public class ProductStructure : SubjectClass
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public ProductStructure(SchedulerDataSource dataSource)
            : base(dataSource)
        { }
        public const int ClassNumber = 1189;
        public override int ClassId { get { return ClassNumber; } }
        /// <summary>
        /// Vztah na Výrobní příkaz (=hlavička pro více operací)
        /// </summary>
        public ProductOrder ProductOrder { get { return this.ProductOperation?.ProductOrder; } }
        /// <summary>
        /// Vztah na Operaci výrobního příkazu (=hlavička pro více komponent)
        /// </summary>
        public ProductOperation ProductOperation { get; set; }
        public int Line { get; set; }
        public string ToolTip { get; set; }
        /// <summary>
        /// Množství komponenty celkem požadované
        /// </summary>
        public decimal Qty { get; set; }
        /// <summary>
        /// Metoda vygeneruje řádky za this instanci a za svoje podřízené řádky, a přidá je do předané kolekce
        /// </summary>
        /// <returns></returns>
        public void CreateGuiRows(ICollection<GuiDataRow> list)
        {
            GuiId rowGid = this.RecordGid;
            GuiDataRow row = new GuiDataRow(rowGid, this.Refer, this.Name, this.Qty);
            row.RowGuiId = rowGid;
            row.ParentRowGuiId = this.ProductOperation.RecordGid;
            list.Add(row);
        }
    }
    #endregion
    #region class PlanUnitC : Plánovací jednotka kapacit = Pracoviště + Pracovník
    /// <summary>
    /// PlanUnitC : Plánovací jednotka kapacit = Pracoviště + Pracovník
    /// </summary>
    public class PlanUnitC : SubjectClass
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public PlanUnitC(SchedulerDataSource dataSource)
            : base(dataSource)
        { }
        public const int ClassNumber = 1364;
        public override int ClassId { get { return ClassNumber; } }
        public string Note { get; set; }
        public string WorkPlace { get; set; }
        public IEnumerable<string> TagTexts { get; set; }
        public IEnumerable<GuiTagItem> TagItems { get { IEnumerable<string> tt = this.TagTexts; return (tt == null ? new GuiTagItem[0] : tt.Select(text => new GuiTagItem() { RowId = this.RecordGid, TagText = text }).ToArray()); } }
        public PlanUnitType PlanUnitType { get; set; }
        public Color? RowBackColor { get; set; }
        public int MachinesCount { get; set; }
        /// <summary>
        /// Soupis položek pracovní doby (obdoba Stavu kapacit)
        /// </summary>
        public List<WorkTime> WorkTimes { get; set; }
        public void AddUnitTime(WorkUnit unitTime)
        {
            if (unitTime == null) return;

            if (this.UnitTimes == null) this.UnitTimes = new List<WorkUnit>();
            this.UnitTimes.Add(unitTime);

            WorkTime workTime = this.WorkTimes.FirstOrDefault(w => (w.Time.Begin <= unitTime.Time.Begin && w.Time.End >= unitTime.Time.End));
            if (workTime != null)
                workTime.UsedTime += unitTime.UsedTime;
        }
        /// <summary>
        /// Soupis položek práce
        /// </summary>
        public List<WorkUnit> UnitTimes { get; set; }
        /// <summary>
        /// Metoda umístí do <see cref="CurrentWorkTime"/> nejbližší vyhovující pracovní čas, počínaje daným časem, v daném směru.
        /// Směr Positive: Jeho Begin bude buď rovný nebo vyšší než požadavek (nikdy nebude menší), jeho End bude vyšší.
        /// Směr Negative: Jeho End bude buď rovný nebo nižší než požadavek (nikdy nebude větší), jeho Begin bude nižší.
        /// </summary>
        /// <param name="flowTime"></param>
        public void FindTime(DateTime flowTime, Data.Direction direction)
        {
            this.CurrentWorkTime = null;
            WorkTime workTime;
            switch (direction)
            {
                case Direction.Positive:
                    workTime = this.WorkTimes.FirstOrDefault(wt => wt.Time.End > flowTime);
                    if (workTime != null)
                    {
                        DateTime begin = (workTime.Time.Begin < flowTime ? flowTime : workTime.Time.Begin);
                        this.CurrentWorkTime = new GuiTimeRange(begin, workTime.Time.End);
                    }
                    break;
                case Direction.Negative:
                    workTime = this.WorkTimes.LastOrDefault(wt => wt.Time.Begin < flowTime);
                    if (workTime != null)
                    {
                        DateTime end = (workTime.Time.End > flowTime ? flowTime : workTime.Time.End);
                        this.CurrentWorkTime = new GuiTimeRange(workTime.Time.Begin, end);
                    }
                    break;
            }
        }
        /// <summary>
        /// Pracovní čas nalezený v metodě <see cref="FindTime(DateTime)"/>
        /// </summary>
        public GuiTimeRange CurrentWorkTime { get; set; }
        /// <summary>
        /// Vytvoří a vrátí graf práce za toto Pracoviště / Pracovníka (obsahuje prvky = pracovní směny a prvky práce)
        /// </summary>
        /// <param name="gridType">Cílový graf, ovlivňuje detaily prvků grafu</param>
        /// <returns></returns>
        public GuiGraph CreateGuiGraphWork(GridPositionType gridType)
        {
            GuiGraph guiGraph = new GuiGraph();
            guiGraph.RowId = this.RecordGid;

            bool showUseRatio = (this.PlanUnitType == TestGUI.PlanUnitType.Person);
            bool setRealHeight = (this.PlanUnitType != TestGUI.PlanUnitType.Person);
            if (this.WorkTimes != null)
                guiGraph.GraphItems.AddRange(this.WorkTimes.Select(workTime => workTime.CreateGuiGraphItem(gridType)));

            if (this.UnitTimes != null)
                guiGraph.GraphItems.AddRange(this.UnitTimes.Select(unitTime => unitTime.CreateGuiGraphItem(gridType)));

            return guiGraph;
        }
        /// <summary>
        /// Vytvoří a vrátí graf času za toto Pracoviště / Pracovníka (obsahuje prvky = pracovní směny a jejich Ratio)
        /// </summary>
        /// <param name="gridType">Cílový graf, ovlivňuje detaily prvků grafu</param>
        /// <returns></returns>
        public GuiGraph CreateGuiGraphTime(GridPositionType gridType)
        {
            GuiGraph guiGraph = new GuiGraph();
            guiGraph.RowId = this.RecordGid;

            if (this.WorkTimes != null)
                guiGraph.GraphItems.AddRange(this.WorkTimes.Select(workTime => workTime.CreateGuiGraphItem(gridType)));

            return guiGraph;
        }
        /// <summary>
        /// Vytvoří a vrátí kompletní řádek
        /// </summary>
        /// <param name="gridType"></param>
        /// <returns></returns>
        internal GuiDataRow CreateGuiRow(GridPositionType gridType)
        {
            GuiDataRow guiRow = null;
            switch (gridType)
            {
                case GridPositionType.Workplace:
                case GridPositionType.Person:
                    GuiIdText mc = new GuiIdText() { GuiId = new GuiId(PlanUnitC.ClassNumber, this.RecordId), Text = this.MachinesCount.ToString() };
                    guiRow = new GuiDataRow(this.RecordGid, this.Refer, this.Name, mc);
                    guiRow.Graph = this.CreateGuiGraphWork(gridType);
                    break;
                case GridPositionType.Employee:
                    guiRow = new GuiDataRow(this.RecordGid, this.Refer, this.Name, this.Note);
                    guiRow.Graph = this.CreateGuiGraphTime(gridType);
                    break;
            }
            guiRow.RowGuiId = this.RecordGid;
            guiRow.Style = new GuiVisualStyle() { BackColor = this.RowBackColor };
            guiRow.TagItems = new List<GuiTagItem>(this.TagItems);
            return guiRow;
        }
    }
    /// <summary>
    /// Druh kapacity
    /// </summary>
    public enum PlanUnitType
    {
        None = 0,
        /// <summary>
        /// Pracoviště
        /// </summary>
        Workplace,
        /// <summary>
        /// Osoba
        /// </summary>
        Person
    }
    #endregion
    #region class WorkUnit : Pracovní jednotka = část práce na určité operaci na určitém pracovišti
    /// <summary>
    /// WorkUnit : Pracovní jednotka = část práce na určité operaci na určitém pracovišti
    /// </summary>
    public class WorkUnit : RecordClass
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public WorkUnit(SchedulerDataSource dataSource)
            : base(dataSource)
        { }
        public override string ToString()
        {
            return "Time: " + this.Time + "; Operation: " + this.Operation + "; PlanUnitC: " + this.PlanUnitC;
        }
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
        /// Využitý čas = (this.Time.End - this.Time.Begin)
        /// </summary>
        public TimeSpan UsedTime { get { return (this.Time != null ? (this.Time.End - this.Time.Begin) : TimeSpan.Zero); } }
        /// <summary>
        /// Vytvoří a vrátí prvek grafu za tuto jednotku práce.
        /// </summary>
        /// <param name="gridType">Cílový graf, ovlivňuje detaily prvků grafu</param>
        /// <returns></returns>
        public GuiGraphItem CreateGuiGraphItem(GridPositionType gridType)
        {
            GuiGraphItem guiGraphItem = new GuiGraphItem()
            {
                ItemId = this.RecordGid,
                GroupId = this.Operation?.RecordGid,
                DataId = this.Operation?.RecordGid,
                RowId = this.PlanUnitC?.RecordGid,
                Layer = 2,
                BackColor = this.BackColor,
                BehaviorMode = GraphItemBehaviorMode.ShowCaptionAllways | GraphItemBehaviorMode.ShowToolTipFadeIn,
                Height = this.Height,
                Text = this.Text,
                ToolTip = this.ToolTip,
                Time = this.Time
            };

            // V property this.PlanUnitC.PlanUnitType je uveden typ plánovací jednotky (stroj / osoba)
            // V parametru "target" je uveden typ cílového grafu;

            // Aktivita (režim chování BehaviorMode) se liší pro Pracoviště a pro Osobu:
            if (this.PlanUnitC != null)
            {
                switch (this.PlanUnitC.PlanUnitType)
                {
                    case PlanUnitType.Workplace:
                        guiGraphItem.BehaviorMode |=
                            GraphItemBehaviorMode.ShowLinks |
                            GraphItemBehaviorMode.MoveToAnotherTime | GraphItemBehaviorMode.ResizeTime;
                        if (this.Operation != null)
                        {
                            if (!this.Operation.IsFixed)
                                guiGraphItem.BehaviorMode |= GraphItemBehaviorMode.MoveToAnotherRow;
                        }
                        break;
                    case PlanUnitType.Person:
                        guiGraphItem.BehaviorMode |=
                            GraphItemBehaviorMode.CanSelect;
                        break;
                }
            }

            // Fixed:
            if (this.Operation != null && this.Operation.IsFixed)
            {
                guiGraphItem.BackStyle = System.Drawing.Drawing2D.HatchStyle.DarkHorizontal;
                Color backColor = this.BackColor;
                guiGraphItem.HatchColor = backColor.Morph(Color.Gray, 0.750f);
                guiGraphItem.ImageBegin = RES.Images.Actions24.Lock4Png;
            }

            // Icon:
            if (this.Operation != null && this.Operation.Icon != null)
            {
                guiGraphItem.ImageBegin = new GuiImage() { ImageFile = this.Operation.Icon };
            }

            // Testy Skinů:
            bool isHidden = ((this.Operation.RecordId % 5) == 0);

            guiGraphItem.SkinCurrentIndex = 1;
            guiGraphItem.BackColor = Color.LightYellow;
            if (isHidden)
            {
                guiGraphItem.ImageBegin = new GuiImage();
                guiGraphItem.ImageEnd = RES.Images.Actions24.DialogNo3Png;
                guiGraphItem.BackColor = Color.PaleTurquoise;
                guiGraphItem.BackStyle = System.Drawing.Drawing2D.HatchStyle.Percent25;
                guiGraphItem.HatchColor = Color.Black;
            }

            guiGraphItem.SkinCurrentIndex = 2;
            guiGraphItem.BackColor = (isHidden ? Color.DarkGray : Color.DarkBlue);

            guiGraphItem.SkinCurrentIndex = 3;
            guiGraphItem.BackColor = (isHidden ? Color.DarkGray : Color.DarkBlue);
            if (isHidden)
                guiGraphItem.IsVisible = false;

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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public WorkTime(SchedulerDataSource dataSource)
            : base(dataSource)
        {
            this.UsedTime = TimeSpan.Zero;
        }
        public override string ToString()
        {
            return this.Time.ToString();
        }
        public const int ClassNumber = 1365;
        public override int ClassId { get { return ClassNumber; } }
        public PlanUnitC PlanUnitC { get; set; }
        public GuiTimeRange Time { get; set; }
        public float Height { get; set; }
        public Color? BackColor { get; set; }
        public bool IsEditable { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public float RatioBegin { get; set; }
        public float? RatioEnd { get; set; }
        public Color? RatioBeginBackColor { get; set; }
        public Color? RatioEndBackColor { get; set; }
        public Color? RatioLineColor { get; set; }
        /// <summary>
        /// Součet použitého času
        /// </summary>
        public TimeSpan UsedTime { get; set; }
        /// <summary>
        /// Poměr použitého času vzhledem k celkovému času
        /// </summary>
        public float UsedRatio
        {
            get
            {
                double count = (this.PlanUnitC != null ? (double)this.PlanUnitC.MachinesCount : 1d);
                double total = 0d;
                if (this.Time != null) total = ((TimeSpan)(this.Time.End - this.Time.Begin)).TotalMinutes;
                double used = this.UsedTime.TotalMinutes;
                double ratio = (total > 0d ? (used / (count * total)) : 1d);
                return (float)ratio;
            }
        }
        /// <summary>
        /// Vytvoří a vrátí prvek grafu za tuto pracovní směnu.
        /// </summary>
        /// <param name="gridType">Cílový graf, ovlivňuje detaily prvků grafu</param>
        /// <returns></returns>
        public GuiGraphItem CreateGuiGraphItem(GridPositionType gridType)
        {
            GuiGraphItem guiGraphItem = new GuiGraphItem()
            {
                RowId = this.PlanUnitC?.RecordGid,
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

            // V property this.PlanUnitC.PlanUnitType je uveden typ plánovací jednotky (stroj / osoba)
            // V parametru "target" je uveden typ cílového grafu;

            if (this.PlanUnitC.PlanUnitType == PlanUnitType.Person)
            {   // Osoba:
                switch (gridType)
                {
                    case GridPositionType.Workplace:
                        break;
                    case GridPositionType.Person:          // Hlavní grid, dolná tabulka
                    case GridPositionType.Employee:        // Tabulka vpravo
                        // Bude mít šrafovanou výplň pozadí:
                        guiGraphItem.BackStyle = System.Drawing.Drawing2D.HatchStyle.Percent25;
                        guiGraphItem.HatchColor = (this.BackColor.HasValue ? this.BackColor.Value.Morph(Color.Black, 0.667f) : Color.DimGray);
                        // Zobrazíme poměr využití kapacity:
                        guiGraphItem.RatioBegin = this.UsedRatio;
                        guiGraphItem.RatioBeginBackColor = this.RatioBeginBackColor;
                        if (!guiGraphItem.RatioBeginBackColor.HasValue)
                            guiGraphItem.RatioBeginBackColor = (this.BackColor.HasValue ? this.BackColor.Value.Morph(Color.Red, 0.250f) : Color.LightPink);
                        // Dolní graf bude mít RatioStyle = VerticalFill, graf vpravo = HorizontalInner:
                        guiGraphItem.RatioStyle = (gridType == GridPositionType.Person ? GuiRatioStyle.VerticalFill : GuiRatioStyle.HorizontalInner);
                        // Výška prvku v grafu bude pro dolní graf == null, pro graf vpravo = 1:
                        guiGraphItem.Height = (gridType == GridPositionType.Person ? (float?)null : (float?)this.Height);
                        guiGraphItem.BackEffectNonEditable = GuiGraphItemBackEffectStyle.Flat;

                        break;
                }
            }
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
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public SubjectClass(SchedulerDataSource dataSource)
            : base (dataSource)
        { }
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
        /// Konstruktor
        /// </summary>
        /// <param name="dataSource"></param>
        public RecordClass(SchedulerDataSource dataSource)
        {
            this.DataSource = dataSource;
            this.RecordId = dataSource.GetNextRecordId(this.ClassId);
        }
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
        public int RecordId { get; private set; }
        /// <summary>
        /// Reference na datový základ
        /// </summary>
        protected SchedulerDataSource DataSource { get; private set; }
    }
    #endregion
    #region enumy
    /// <summary>
    /// Směr času
    /// </summary>
    public enum TimeDirectionx
    {
        /// <summary>
        /// ANi tam, ani jinam
        /// </summary>
        None = 0,
        /// <summary>
        /// Do minulosti, zpětně, na časové ose doleva
        /// </summary>
        ToHistory,
        /// <summary>
        /// Do budoucnosti, dopředně, na časové ose doprava
        /// </summary>
        ToFuture
    }
    /// <summary>
    /// Typové označení gridu
    /// </summary>
    public enum GridPositionType
    {
        None,
        /// <summary>
        /// Vlevo - Výrobní příkazy
        /// </summary>
        ProductOrder,
        /// <summary>
        /// Uprostřed nahoře - pracoviště a práce na nich
        /// </summary>
        Workplace,
        /// <summary>
        /// Uprostřed dole - osoby a jejich práce
        /// </summary>
        Person,
        /// <summary>
        /// Vpravo - zaměstnanci a jejich pracovní doba, nikoli práce
        /// </summary>
        Employee
    }
    #endregion
}
