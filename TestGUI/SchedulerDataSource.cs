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
        #region Tvorba výchozích dat, plánování operací do pracovišť
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
            this.CreateCenterPanelWorkplace();
            this.CreateCenterPanelPersons();
            this.CreateRightPanel();
            this.CreateContextFunctions();

            Application.App.TracePriority = Application.TracePriority.Priority5_Normal;

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

            // VÝROBA:
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
            this.CreateProductOrder(++recordId, "Krabička dřevěná 35cm", Color.DarkCyan, 30, "jiné", ProductTpv.Simple);
            this.CreateProductOrder(++recordId, "Krabička dřevěná 45cm", Color.DarkCyan, 36, "jiné", ProductTpv.Simple);
            this.CreateProductOrder(++recordId, "Krabička dřevěná 60cm", Color.DarkBlue, 48, "jiné", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Houpací křeslo tmavé", Color.DarkBlue, 6, "jiné", ProductTpv.Luxus);
            this.CreateProductOrder(++recordId, "Psí bouda střední", Color.DarkOrange, 12, "outdoor", ProductTpv.Simple);
            this.CreateProductOrder(++recordId, "Zábradlí schodišťové standard", Color.DarkGreen, 30, "stavba", ProductTpv.Standard);
            this.CreateProductOrder(++recordId, "Stříška před vchodem", Color.DarkRed, 3, "stavba", ProductTpv.Simple);
            this.CreateProductOrder(++recordId, "Krmítko pro menší ptactvo", Color.DarkViolet, 42, "outdoor", ProductTpv.Simple);
            this.CreateProductOrder(++recordId, "Pergola zahradní 4x5 m", Color.DarkCyan, 3, "stavba", ProductTpv.Simple);

            // DÍLNY:
            Color colorLak = Color.FromArgb(224, 240, 255);
            recordId = 10000;
            this.WorkplaceDict = new Dictionary<GuiId, PlanUnitC>();
            this.CreatePlanUnitCWp(++recordId, "Pila pásmová", WP_PILA, "pila", 2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Pila okružní", WP_PILA, "pila", 2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Pilka vyřezávací malá", WP_PILA, "pila;drobné", 1, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Dílna truhlářská velká", WP_DILN, "truhláři",  4, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Dílna truhlářská malá", WP_DILN, "truhláři",  2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Lakovna aceton", WP_LAKO, "lakovna;chemie",  5, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp(++recordId, "Lakovna akryl", WP_LAKO, "lakovna",  5, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp(++recordId, "Moření", WP_LAKO, "lakovna;chemie",  3, CalendarType.Work7d3x8h, colorLak);
            this.CreatePlanUnitCWp(++recordId, "Dílna lakýrnická", WP_LAKO, "lakovna;chemie",  2, CalendarType.Work5d2x8h, colorLak);
            this.CreatePlanUnitCWp(++recordId, "Kontrola standardní", WP_KONT, "kontrola",  2, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Kontrola mistr", WP_KONT, "kontrola",  1, CalendarType.Work5d2x8h, null);
            this.CreatePlanUnitCWp(++recordId, "Kooperace DŘEVEX", WP_KOOP, "kooperace",  1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp(++recordId, "Kooperace TRUHLEX", WP_KOOP, "kooperace", 1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp(++recordId, "Kooperace JAREŠ", WP_KOOP, "kooperace;soukromník", 1, CalendarType.Work5d1x24h, null);
            this.CreatePlanUnitCWp(++recordId, "Kooperace TEIMER", WP_KOOP, "kooperace;soukromník",  1, CalendarType.Work5d1x24h, null);

            // OSOBY, RANNÍ SMĚNA:
            recordId = 10100;
            this.PersonDict = new Dictionary<GuiId, PlanUnitC>();
            this.CreatePlanUnitCZm(++recordId, "NOVÁK Jiří", CalendarType.Work5d1x8hR, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "DVOŘÁK Pavel", CalendarType.Work5d1x8hR, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "STARÝ Slavomír", CalendarType.Work5d1x8hR, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "PEŠEK Petr", CalendarType.Work5d1x8hR, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "JENČÍK Jan", CalendarType.Work5d1x8hR, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "KRULIŠ Karel", CalendarType.Work5d1x8hR, colorLak, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "BLÁHOVÁ Božena", CalendarType.Work5d1x8hR, null, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "NEKOKSA Jindřich", CalendarType.Work5d1x8hR, colorLak, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "POKORNÝ Dan", CalendarType.Work5d1x8hR, null, WP_DILN, WP_KONT);
            this.CreatePlanUnitCZm(++recordId, "DRAHOKOUPIL Martin", CalendarType.Work5d1x8hR, null, WP_KONT);

            // OSOBY, ODPOLEDNÍ SMĚNA:
            this.CreatePlanUnitCZm(++recordId, "VETCHÝ Marek", CalendarType.Work5d1x8hO, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "SUP Václav", CalendarType.Work5d1x8hO, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "OSOLSOBĚ Viktor", CalendarType.Work5d1x8hO, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "ČERNÁ Marta", CalendarType.Work5d1x8hO, colorLak, WP_PILA, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "VIDÍM Dan", CalendarType.Work5d1x8hO, null, WP_PILA, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "NĚMEC Jaroslav", CalendarType.Work5d1x8hO, colorLak, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "DLOUHÝ Bedřich", CalendarType.Work5d1x8hO, null, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "HANZAL Patrik", CalendarType.Work5d1x8hO, colorLak, WP_LAKO);
            this.CreatePlanUnitCZm(++recordId, "SPÍVALOVÁ Ilona", CalendarType.Work5d1x8hO, null, WP_DILN);
            this.CreatePlanUnitCZm(++recordId, "DIETRICH Zdenek", CalendarType.Work5d1x8hO, null, WP_KONT);

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

            ProductOrder productOrder = new ProductOrder()
            {
                RecordId = recordId,
                Refer = refer,
                Name = name,
                BackColor = backColor,
                Qty = qty,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null)
            };
            productOrder.OperationList = this.CreateProductOperations(100 * recordId, productOrder, tpv, qty);
            productOrder.DatePlanBegin = begin;

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
        protected List<ProductOperation> CreateProductOperations(int recordId, ProductOrder productOrder, ProductTpv tpv, decimal qty)
        {
            List<ProductOperation> operations = new List<ProductOperation>();

            int line = 0;
            switch (tpv)
            {
                case ProductTpv.Simple:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.GreenYellow, "Řez tvaru", "Přeříznout", WP_PILA, qty, "D", false, 30, 20, 45, Pbb(60)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkOrange, "Šroubovat", "Nasadit šrouby a sešroubovat", WP_DILN, qty, "Š", false, 0, 15, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.ForestGreen, "Lakovat", "Lakování základní", WP_LAKO, qty, "L", true, 30, 30, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "OZ", false, 30, 15, 0));
                    break;

                case ProductTpv.Standard:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.GreenYellow, "Řez tvaru", "Přeříznout", WP_PILA, qty, "D", false, 30, 20, 45, Pbb(60)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.Blue, "Broušení hran", "Zabrousit", WP_DILN, qty, "", false, 0, 20, 30, Pbb(20)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, "", false, 15, 15, 30, Pbb(5)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkOrange, "Nasadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, "Č", false, 0, 30, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkRed, "Klížit", "Sklížit díly", WP_DILN, qty, "K", false, 30, 20, 360));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.ForestGreen, "Lakovat", "Lakování základní", WP_LAKO, qty, "L", true, 30, 45, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "O", false, 30, 20, 0));
                    break;

                case ProductTpv.Luxus:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.GreenYellow, "Řez délky", "Přeříznout", WP_PILA, qty, "D", false, 30, 25, 45, Pbb(70)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.Blue, "Brousit hrany", "Zabrousit", WP_DILN, qty, "", false, 0, 30, 45, Pbb(50)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.Blue, "Brousit povrch", "Zabrousit", WP_DILN, qty, "", false, 0, 20, 30, Pbb(40)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.BlueViolet, "Vrtat čepy", "Zavrtat pro čepy", WP_DILN, qty, "", false, 30, 15, 45, Pbb(30)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkOrange, "Vsadit čepy", "Nasadit a vlepit čepy", WP_DILN, qty, "Č", false, 0, 20, 0, Pbb(20)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola čepů", "Kontrolovat čepy", WP_KONT, qty, "", false, 0, 30, 0, Pbb(10)));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkRed, "Klížit celek", "Sklížit díly", WP_DILN, qty, "K", false, 45, 60, 360));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola klížení", "Kontrolovat klížení", WP_KONT, qty, "", false, 0, 30, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.ForestGreen, "Lakovat základ", "Lakování základní", WP_LAKO, qty, "L", true, 30, 45, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.Blue, "Brousit lak", "Zabrousit", WP_DILN, qty, "", false, 0, 30, 5));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DarkGreen, "Lakovat lesk", "Lakování lesklé", WP_LAKO, qty, "l", true, 60, 60, 240));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola celku", "Kontrolovat lakování", WP_KONT, qty, "", false, 0, 30, 0));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "O", false, 30, 20, 0));
                    break;

                case ProductTpv.Cooperation:
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.Gray, "Kooperace", "Udělá to někdo jiný", WP_KOOP, qty, "B", false, 360, 30, 1440));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola", "Kontrolovat kooperaci", WP_KONT, qty, "", false, 1440, 30, 60));
                    operations.Add(CreateProductOperation(++recordId, productOrder, ++line, Color.DimGray, "Kontrola", "Kontrola finální", WP_KONT, qty, "OZ", false, 30, 20, 0));
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
        /// <param name="isFragment"></param>
        /// <param name="tbcMin"></param>
        /// <param name="tacMin"></param>
        /// <param name="tecMin"></param>
        /// <param name="isFixed"></param>
        /// <returns></returns>
        protected ProductOperation CreateProductOperation(int recordId, ProductOrder productOrder, int line, Color backColor, string name, string toolTip,
            string workPlace, decimal qty, string components, bool isFragment, int tbcMin, int tacMin, int tecMin, bool isFixed = false)
        {
            float height = CreateOperationHeight(isFragment);
            ProductOperation operation = new ProductOperation()
            {
                RecordId = recordId,
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
                TAc = TimeSpan.FromMinutes((double)(qty = (decimal)tacMin)),
                TEc = TimeSpan.FromMinutes(tecMin)
            };
            TimeSpan add = TimeSpan.FromHours(1d) - operation.TTc;
            if (add.Ticks > 0L) operation.TAc = operation.TAc + add;

            operation.ToolTip = operation.ReferName + Eol + productOrder.ReferName + Eol + toolTip;

            // Komponenty:
            if (!String.IsNullOrEmpty(components))
            {
                foreach (char c in components)
                    CreateProductStructure(++recordId, operation, c, qty);
            }

            return operation;
        }
        /// <summary>
        /// Vytvoří a do operace vepíše jednu komponentu
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="operation"></param>
        /// <param name="component"></param>
        /// <param name="qty"></param>
        protected void CreateProductStructure(int recordId, ProductOperation operation, char component, decimal qty)
        {
            ProductStructure structure = null;
            switch (component)
            {
                case 'D':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "DTD", Name = "Dřevo", Qty = 0.25m * qty });
                    break;
                case 'Š':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "M6š", Name = "Šroub M6", Qty = 6m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "M6p", Name = "Podložka M6", Qty = 12m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "M6m", Name = "Matka M6", Qty = 6m * qty });
                    break;
                case 'L':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Cx1000", Name = "Lak Celox 1000", Qty = 0.1m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "C006", Name = "Nitroředidlo", Qty = 0.1m * qty });
                    break;
                case 'Č':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Č6x20", Name = "Čep dřevo 6 x 20", Qty = 6m * qty });
                    break;
                case 'K':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Kh12", Name = "Klíh 12MPa", Qty = 0.1m * qty });
                    break;
                case 'l':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Sx1050", Name = "Lak syntetic 1050", Qty = 0.1m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "S006", Name = "Syntetické ředidlo", Qty = 0.1m * qty });
                    break;
                case 'B':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "BA95", Name = "Benzin Natural95", Qty = 0.04m * qty });
                    break;
                case 'O':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Kt6", Name = "Karton 6\"", Qty = 1.00m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Fb2", Name = "Folie bublinková", Qty = 0.10m * qty });
                    break;
                case 'Z':
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "ZL", Name = "Záruční list 2roky", Qty = 1.00m * qty });
                    operation.StructureList.Add(new ProductStructure() { RecordId = recordId, ProductOperation = operation, Refer = "Nobs", Name = "Návod k použití", Qty = 0.10m * qty });
                    break;
            }
            if (structure != null)
                operation.StructureList.Add(structure);
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
        /// <param name="recordId"></param>
        /// <param name="name"></param>
        /// <param name="workPlace"></param>
        /// <param name="tagText"></param>
        /// <param name="machinesCount"></param>
        /// <param name="calendar"></param>
        /// <param name="rowBackColor"></param>
        protected GuiId CreatePlanUnitCWp(int recordId, string name, string workPlace, string tagText, int machinesCount, CalendarType calendar, Color? rowBackColor)
        {
            string refer = "D" + recordId.ToString();
            PlanUnitC planUnitC = new PlanUnitC()
            {
                RecordId = recordId,
                Refer = refer,
                Name = name,
                WorkPlace = workPlace,
                RowBackColor = rowBackColor,
                TagTexts = (tagText != null ? tagText.Split(',', ';') : null),
                MachinesCount = machinesCount,
                PlanUnitType = PlanUnitType.Workplace,
                WorkTimes = CreateWorkingItems(1000 * recordId, calendar, (float)machinesCount, this.TimeRangeTotal)
            };
            this.WorkplaceDict.Add(planUnitC.RecordGid, planUnitC);

            return planUnitC.RecordGid;
        }
        /// <summary>
        /// Vytvoří a uloží jeden záznam Zaměstnanec včetně jeho pracovních směn, pro dané zadání.
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="name"></param>
        /// <param name="calendar"></param>
        /// <param name="rowBackColor"></param>
        /// <param name="workPlaces"></param>
        protected GuiId CreatePlanUnitCZm(int recordId, string name, CalendarType calendar, Color? rowBackColor, params string[] workPlaces)
        {
            string refer = "Z" + recordId.ToString();
            string workplace = workPlaces.ToString(";");
            PlanUnitC planUnitC = new PlanUnitC()
            {
                RecordId = recordId,
                Refer = refer,
                Name = name,
                WorkPlace = workplace,
                RowBackColor = rowBackColor,
                TagTexts = null,
                MachinesCount = 1,
                PlanUnitType = PlanUnitType.Person,
                WorkTimes = CreateWorkingItems(1000 * recordId, calendar, 1f, this.TimeRangeTotal)
            };
            this.PersonDict.Add(planUnitC.RecordGid, planUnitC);

            return planUnitC.RecordGid;
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
            float ratio = GetRandomRatio();
            while (this.CreateWorkingTime(ref time, calendar, totalTimeRange, out workingTimeRange, out backColor))
            {
                backColor = Color.FromArgb(64, backColor);
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
        /// Dictionary s Pracovišti
        /// </summary>
        protected Dictionary<GuiId, PlanUnitC> WorkplaceDict;
        /// <summary>
        /// Dictionary s Dělníky
        /// </summary>
        protected Dictionary<GuiId, PlanUnitC> PersonDict;
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
            {
                DateTime startTime = productOrder.DatePlanBegin;
                DateTime flowTime = startTime;
                foreach (ProductOperation productOperation in productOrder.OperationList)
                    this.PlanOperationToWorkplaces(ref recordId, productOperation, ref flowTime);
                productOrder.Time = new GuiTimeRange(startTime, flowTime);
            }
        }
        /// <summary>
        /// Umístí pracovní časy operací dané operace na vhodné pracoviště
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="productOperation"></param>
        /// <param name="flowTime"></param>
        protected void PlanOperationToWorkplaces(ref int recordId, ProductOperation productOperation, ref DateTime flowTime)
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
            productOperation.PlanTimeOperation(ref recordId, ref flowTime, Direction.Positive, workplace, person);
        }
        #endregion
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
                Name = GuiNameToolbarFilterLeft,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
                GroupName = "NASTAVENÍ",
                Title = "Filtruj VP",
                ToolTip = "Pokud bude aktivní, budou v levé tabulce zobrazeny jen ty Výrobní příkazy, jejichž některá operace se provádí na aktuálním pracovišti.",
                IsCheckable = true,
                Image = RES.Images.Actions24.FormatIndentLess3Png,
                GuiActions = GuiActionType.ResetAllRowFilters | GuiActionType.RunInteractions | GuiActionType.SuppressCallAppHost,
                RunInteractionNames = GuiFullNameGridCenterTop + ":" + GuiNameInteractionFilterProductOrder,
                RunInteractionSource = SourceActionType.TableRowActivatedOnly | SourceActionType.TableRowChecked
            });

            this.MainData.ToolbarItems.Add(new GuiToolbarItem()
            {
                Name = GuiNameToolbarShowColorSet1,
                Size = FunctionGlobalItemSize.Half,
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
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
                LayoutHint = LayoutHint.NextItemSkipToNextTable,
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
                LayoutHint = LayoutHint.NextItemSkipToNextRow,
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
                Name = GuiNameToolbarResetFilters,
                Size = FunctionGlobalItemSize.Half,
                GroupName = "NASTAVENÍ",
                Title = "Zruš filtry",
                Image = RES.Images.Actions24.TabClose2Png,
                GuiActions = GuiActionType.ResetAllRowFilters | GuiActionType.SuppressCallAppHost
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
            guiTable.TreeViewNodeOffset = 14;
            guiTable.TreeViewLinkMode = GuiTreeViewLinkMode.Dot;
            guiTable.TreeViewLinkColor = Color.DarkViolet;
            guiTable.AddColumn(new GuiDataColumn() { Name = "record_gid", BrowseColumnType = BrowseColumnType.RecordId, TableClassId = ProductOrder.ClassNumber });
            guiTable.AddColumn(new GuiDataColumn() { Name = "reference_subjektu", Title = "Číslo", Width = 85 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "nazev_subjektu", Title = "Dílec", Width = 200 });
            guiTable.AddColumn(new GuiDataColumn() { Name = "qty", Title = "Množství", Width = 45 });

            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                this.AddProductOrderToGrid(guiTable, productOrder);

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
                UnitTime.ClassNumber + ":S;" +        // Pracovní jednotka: z OtherTable přenést jen tehdy, pokud na Parent řádku máme synchronní údaj GroupId
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
                this.AddPlanUnitCToGrid(gridCenterWorkplace.RowTable, planUnitC);

            // Vztahy prvků (Link):
            foreach (ProductOrder productOrder in this.ProductOrderDict.Values)
                this.AddGraphLinkToGrid(gridCenterWorkplace.RowTable, productOrder);

            this.GridCenterWorkplace = gridCenterWorkplace;
            this.MainPage.MainPanel.Grids.Add(gridCenterWorkplace);
        }
        /// <summary>
        /// Vygeneruje kompletní data do středního panelu do dolní tabulky = Osoby
        /// </summary>
        protected void CreateCenterPanelPersons()
        {
            GuiGrid gridCenterPersons = new GuiGrid() { Name = GuiNameGridCenterBottom, Title = "Pracovníci" };

            this.SetCenterGridProperties(gridCenterPersons, true, true, true, true, GuiNameRowsCenterBottom);

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
                UnitTime.ClassNumber + ":S;" +        // Pracovní jednotka: z OtherTable přenést jen tehdy, pokud na Parent řádku máme synchronní údaj GroupId
                "0:N";                                // 0 = jiné třídy   : nepřenášet


            // Data tabulky = Plánovací jednotky Pracovníci:
            foreach (PlanUnitC planUnitC in this.PersonDict.Values)
                this.AddPlanUnitCToGrid(gridCenterPersons.RowTable, planUnitC);

            this.GridCenterPersons = gridCenterPersons;
            this.MainPage.MainPanel.Grids.Add(gridCenterPersons);
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
        /// Do dodané tabulky přidá řádek za danou Plánovací jednotku, přidá jeho TagItems a graf z jeho operací.
        /// </summary>
        /// <param name="guiTable"></param>
        /// <param name="planUnitC"></param>
        protected void AddPlanUnitCToGrid(GuiDataTable guiTable, PlanUnitC planUnitC)
        {
            GuiId rowGid = planUnitC.RecordGid;
            GuiIdText mc = new GuiIdText() { GuiId = new GuiId(1365, planUnitC.RecordId), Text = planUnitC.MachinesCount.ToString() };
            GuiDataRow row = guiTable.AddRow(rowGid, planUnitC.Refer, planUnitC.Name, mc);      // planUnitC.MachinesCount
            row.BackColor = planUnitC.RowBackColor;
            row.TagItems = new List<GuiTagItem>(planUnitC.TagItems);
            row.Graph = planUnitC.CreateGuiGraph();
        }
        /// <summary>
        /// Do dodané tabulky přidá linky mezi operacemi daného Výrobního příkazu
        /// </summary>
        /// <param name="guiTable"></param>
        /// <param name="productOrder"></param>
        protected void AddGraphLinkToGrid(GuiDataTable guiTable, ProductOrder productOrder)
        {
            var links = productOrder.CreateGuiLinks();
            if (links == null || links.Length == 0) return;
            if (guiTable.GraphLinks == null) guiTable.GraphLinks = new List<GuiGraphLink>();
            guiTable.GraphLinks.AddRange(links);
        }
        protected void CreateRightPanel()
        { }
        /// <summary>
        /// Vygeneruje kontextové funkce
        /// </summary>
        protected void CreateContextFunctions()
        {
            this.MainData.ContextMenuItems = new GuiContextMenuSet();
            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = GuiNameContextFixItem,
                Title = "Nastav FIXOVÁNÍ",
                Image = RES.Images.Actions24.Lock4Png,
                ToolTip = "Tato funkce nastaví fixování u daného záznamu.\r\nTo pak znamená, že s tím nejde hnout.\r\nVŮBEC.",
                VisibleFor = GuiFullNameGridCenterTop + ":" + UnitTime.ClassNumber.ToString()
            });
            this.MainData.ContextMenuItems.Add(new GuiContextMenuItem()
            {
                Name = "test",
                Title = "Zobrazit čas",
                Image = RES.Images.Actions24.ViewCalendarTimeSpentPng,
                ToolTip = "Tato funkce nastaví fixování u daného záznamu.\r\nTo pak znamená, že s tím nejde hnout.\r\nVŮBEC.",
                VisibleFor = GuiFullNameGridCenterTop + ":" + GuiContextMenuItem.AREA_GRAF + "," + GuiContextMenuItem.AREA_ROW + ":" + PlanUnitC.ClassNumber.ToString()
            });
        }
        protected GuiData MainData;
        protected GuiPage MainPage;
        protected GuiGrid GridLeft;
        protected GuiGrid GridCenterWorkplace;
        protected GuiGrid GridCenterPersons;
        protected DateTime DateTimeNow;
        protected DateTime DateTimeFirst;
        protected DateTime DateTimeLast;
        protected GuiTimeRange TimeRangeTotal;
        protected GuiTimeRange TimeRangeCurrent;
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
            return guiData;
            // if (!System.Diagnostics.Debugger.IsAttached) return guiData;

            GuiData guiDataP = SerialDeserialData(guiData, XmlCompressMode.None);
            GuiData guiDataC = SerialDeserialData(guiData, XmlCompressMode.Compress);
            return guiDataP;
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
                serial = XmlPersist.Serialize(guiData, serArgs);
                scopeS.AddItem("SerialLength: " + serial.Length.ToString());
                scopeS.AddItem("PrimitivesLength: " + serArgs.SavePrimitivesLength.ToString());
                primitive = serArgs.SavePrimitivesContent.ToString();
            }

            SerialSaveData(serial, mode);
            SerialSaveData(primitive, ".txt");

            // Test formátu verze 1.00 (načítání starších souborů):
            // serial = System.IO.File.ReadAllText(@"c:\Users\David\AppData\Local\Asseco Solutions\WorkScheduler\Data\WorkScheduler.setting", Encoding.UTF8);

            using (var scopeD = Application.App.Trace.Scope("SchedulerDataSource", "SerialDeserialData", "Deserialize", compress, runMode))
            {
                desArgs = new PersistArgs() { CompressMode = mode, DataContent = serial };
                object result = XmlPersist.Deserialize(desArgs);
                resData = result as GuiData;
            }

            // Prověříme alespoň základní shodu dat:
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

            if (guiData.Pages == null || guiData.Pages.Count == 0) return resData;

            GuiPage guiPage = guiData.Pages.Pages[0];
            GuiPage resPage = resData.Pages.Pages[0];
            if (guiPage.MainPanel.Grids == null || guiPage.MainPanel.Grids.Count == 0) return resData;
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

            return resData;
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
        protected const string GuiNameToolbarShowColorSet1 = "TlbShowColorSet1";
        protected const string GuiNameToolbarShowColorSet2 = "TlbShowColorSet2";
        protected const string GuiNameToolbarShowColorSet3 = "TlbShowColorSet3";
        protected const string GuiNameToolbarShowColorSet4 = "TlbShowColorSet4";

        protected const string GuiNameMainPage = "MainPage";
        protected const string GuiNameGridLeft = "GridLeft";
        protected const string GuiNameGridCenterTop = "GridCenterTop";
        protected const string GuiNameGridCenterBottom = "GridCenterBottom";

        protected const string GuiNameInteractionSelectOperations = "InteractionSelectOperations";
        protected const string GuiNameInteractionFilterProductOrder = "InteractionFilterProductOrder";
        protected const string GuiNameInteractionShowColorSet = "InteractionShowColorSet";

        protected const string GuiNameLeftRowTable = "RowsLeft";
        protected const string GuiNameRowsCenterTop = "RowsCenterTop";
        protected const string GuiNameRowsCenterBottom = "RowsCenterBottom";

        protected const string GuiFullNameLeftPanel = GuiNameData + GuiNameDelimiter + GuiNamePages + GuiNameDelimiter + GuiNameMainPage + GuiNameDelimiter + GuiNameLeftPanel + GuiNameDelimiter;
        protected const string GuiFullNameGridLeft = GuiFullNameLeftPanel + GuiNameGridLeft;
        protected const string GuiFullNameMainPanel = GuiNameData + GuiNameDelimiter + GuiNamePages + GuiNameDelimiter + GuiNameMainPage + GuiNameDelimiter + GuiNameMainPanel + GuiNameDelimiter;
        protected const string GuiFullNameGridCenterTop = GuiFullNameMainPanel + GuiNameGridCenterTop;
        protected const string GuiFullNameGridCenterBottom = GuiFullNameMainPanel + GuiNameGridCenterBottom;

        protected const string GuiNameContextFixItem = "CtxFixItem";

        protected const string GuiNameDelimiter = "\\";
        protected const string GuiNamePages = "pages";
        protected const string GuiNameLeftPanel = "leftPanel";
        protected const string GuiNameMainPanel = "mainPanel";
        protected const string GuiNameRightPanel = "rightPanel";
        protected const string GuiNameBottomPanel = "bottomPanel";
        #endregion
        #region IAppHost : vyvolání funkce z Pluginu do AppHost
        AppHostResponseArgs IAppHost.CallAppHostFunction(AppHostRequestArgs requestArgs)
        {
            this.AppHostAddRequest(requestArgs);
            return null;              // Jsme asynchronní AppHost, vracíme null.
        }
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
        /// Metoda je volána v threadu na pozadí, má za úkol zpracovat daný požadavek (parametr).
        /// </summary>
        /// <param name="requestArgs"></param>
        protected void AppHostExecCommand(AppHostRequestArgs requestArgs)
        {
            AppHostResponseArgs responseArgs = new AppHostResponseArgs(requestArgs);
            int time;
            GuiTimeRange timeRange = requestArgs.Request?.CurrentState?.TimeAxisValue;
            if (requestArgs != null)
            {
                switch (requestArgs.Request.Command)
                {
                    case GuiRequest.COMMAND_GraphItemMove:
                        this.DataChanged = true;
                        time = this.Rand.Next(100, 350);
                        System.Threading.Thread.Sleep(time);
                        responseArgs.GuiResponse = new GuiResponse();
                        responseArgs.GuiResponse.Common = new GuiResponseCommon() { ClearLinks = true, ClearSelected = true };
                        responseArgs.GuiResponse.ToolbarItems = new GuiToolbarItem[]
                        {
                            new GuiToolbarItem() { Name = "SaveData", Enable = true }
                        };
                        break;

                    case GuiRequest.COMMAND_GraphItemResize:
                        this.DataChanged = true;
                        time = this.Rand.Next(100, 350);
                        System.Threading.Thread.Sleep(time);
                        responseArgs.GuiResponse = new GuiResponse();
                        responseArgs.GuiResponse.Common = new GuiResponseCommon() { ClearLinks = true, ClearSelected = true };
                        responseArgs.GuiResponse.ToolbarItems = new GuiToolbarItem[]
                        {
                            new GuiToolbarItem() { Name = "SaveData", Enable = true }
                        };
                        break;

                    case GuiRequest.COMMAND_ToolbarClick:
                        responseArgs.GuiResponse = new GuiResponse();
                        switch (requestArgs.Request.ToolbarItem.Name)
                        {
                            case "TlbSubDay":
                                timeRange = new GuiTimeRange(timeRange.Begin.AddDays(-1d), timeRange.End.AddDays(-1d));
                                responseArgs.GuiResponse.Common = new GuiResponseCommon() { TimeAxisValue = timeRange };
                                break;
                            case "TlbAddDay":
                                timeRange = new GuiTimeRange(timeRange.Begin.AddDays(1d), timeRange.End.AddDays(1d));
                                responseArgs.GuiResponse.Common = new GuiResponseCommon() { TimeAxisValue = timeRange };
                                break;
                            case "RePlan":
                                time = this.Rand.Next(500, 5000);
                                System.Threading.Thread.Sleep(time);
                                responseArgs.GuiResponse.Dialog = GetDialog("Data jsou zaplánovaná.", GuiDialogButtons.Ok);
                                break;
                            case "SaveData":
                                time = this.Rand.Next(500, 5000);
                                System.Threading.Thread.Sleep(time);
                                this.DataChanged = false;
                                responseArgs.GuiResponse.Dialog = GetDialog("Data jsou uložena.", GuiDialogButtons.Ok);
                                responseArgs.GuiResponse.ToolbarItems = new GuiToolbarItem[]
                                {
                                    new GuiToolbarItem() { Name = "SaveData", Enable = false }
                                };

                                break;
                        }
                        break;

                    case GuiRequest.COMMAND_ContextMenuClick:
                        Application.App.ShowInfo(
                            "Někdo chce provést funkci: " + requestArgs.Request.ContextMenu.ContextMenuItem.Title + Environment.NewLine +
                            "Pro prvek grafu: " + requestArgs.Request.ContextMenu.ContextItemId.ToString() + Environment.NewLine +
                            "V čase: " + requestArgs.Request.ContextMenu.ClickTime);
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
                            responseArgs.GuiResponse = new GuiResponse()
                            {
                                Dialog = GetDialog(GetMessageSaveData(), GuiDialogButtons.YesNoCancel, GuiDialog.DialogIconQuestion),
                                CloseSaveData = new GuiSaveData() { AutoSave = true, BlockGuiTime = TimeSpan.FromSeconds(20d), BlockGuiMessage = "Probíhá ukládání dat...\r\nData se právě ukládají do databáze.\r\nJakmile budou uložena, dostanete od nás spěšnou sovu." }
                            };
                        }
                        break;

                    case GuiRequest.COMMAND_SaveBeforeCloseWindow:
                        // Chci si otestovat malou prodlevu před skončením:
                        time = this.Rand.Next(1500, 12000);
                        System.Threading.Thread.Sleep(time);
                        if (this.Rand.Next(0,100) <= 65)
                            responseArgs.Result = AppHostActionResult.Success;
                        else
                            responseArgs.Result = AppHostActionResult.Failure;

                        responseArgs.GuiResponse = new GuiResponse()
                        {
                            Dialog = GetDialog("Došlo k chybě. Přejete si skončit i bez uložení dat?", GuiDialogButtons.YesNo)
                        };
                        break;

                }
            }
            if (requestArgs.CallBackAction != null)
                requestArgs.CallBackAction(responseArgs);
        }
        protected bool DataChanged = false;
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
        public DateTime DatePlanBegin { get; set; }
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
            this.StructureList = new List<ProductStructure>();
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
        public string WorkPlace { get; set; }
        public TimeSpan TBc { get; set; }
        public TimeSpan TAc { get; set; }
        public TimeSpan TEc { get; set; }
        public TimeSpan TTc { get { return this.TBc + this.TAc + this.TEc; } }
        public GuiTimeRange Time { get; set; }
        public GuiTimeRange TimeTBc { get; set; }
        public GuiTimeRange TimeTAc { get; set; }
        public GuiTimeRange TimeTEc { get; set; }
        public Color BackColor { get; set; }
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
        /// <param name="flowTime"></param>
        /// <param name="direction"></param>
        /// <param name="workplace"></param>
        /// <param name="person"></param>
        public void PlanTimeOperation(ref int recordId, ref DateTime flowTime, Data.Direction direction, PlanUnitC workplace, PlanUnitC person)
        {
            if (workplace == null) return;
            if (!(direction == Direction.Positive || direction == Direction.Negative))
                throw new Asol.Tools.WorkScheduler.Application.GraphLibCodeException("Směr plánu musí být pouze Positive nebo Negative.");

            this.TimeTBc = this.PlanTimePhase(ref recordId, ref flowTime, direction, workplace, person, this.TBc, this.BackColor.Morph(Color.Green, 0.25f));
            this.TimeTAc = this.PlanTimePhase(ref recordId, ref flowTime, direction, workplace, person, this.TAc, this.BackColor);
            this.TimeTEc = this.PlanTimePhase(ref recordId, ref flowTime, direction, workplace, person, this.TEc, this.BackColor.Morph(Color.Black, 0.25f));
            this.Time = new GuiTimeRange(this.TimeTBc.Begin, this.TimeTEc.End);
        }
        /// <summary>
        /// Uloží jednotku práce pro daný čas do pracoviště
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="flowTime"></param>
        /// <param name="direction"></param>
        /// <param name="workplace"></param>
        /// <param name="person"></param>
        /// <param name="time"></param>
        /// <param name="backColor"></param>
        protected GuiTimeRange PlanTimePhase(ref int recordId, ref DateTime flowTime, Data.Direction direction, PlanUnitC workplace, PlanUnitC person, TimeSpan time, Color backColor)
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
                    bool isPlanned = this.PlanTimePart(ref recordId, ref flowTime, direction, ref phaseBegin, ref phaseEnd, planUnits, ref time, backColor);
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
        protected bool PlanTimePart(ref int recordId, ref DateTime flowTime, Data.Direction direction, ref DateTime? phaseBegin, ref DateTime? phaseEnd, PlanUnitC[] planUnits, ref TimeSpan needTime, Color backColor)
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
                workingUnit.AddUnitTime(this.CreateUnitTime(ref recordId, workingUnit, workTimeRange, backColor));

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
                ToolTip = this.ToolTip
            };
            unitTime.Text = (unitTime.Height <= 1f ? this.ReferName : this.ReferName + "\r\n" + this.ProductOrder.ReferName);
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
            int width = 1 + (int)(prev.ProductOrder.Qty / 6m);
            if (width < 1) width = 1;
            if (width > 4) width = 4;
            GuiGraphLink link = new GuiGraphLink()
            {
                ItemIdPrev = prev?.RecordGid,
                ItemIdNext = next?.RecordGid,
                LinkType = GuiGraphItemLinkType.PrevEndToNextBeginSCurve,
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
        public PlanUnitType PlanUnitType { get; set; }
        public Color? RowBackColor { get; set; }
        public int MachinesCount { get; set; }
        public List<WorkTime> WorkTimes { get; set; }
        public void AddUnitTime(UnitTime unitTime)
        {
            if (unitTime != null)
            {
                if (this.UnitTimes == null)
                    this.UnitTimes = new List<UnitTime>();
                this.UnitTimes.Add(unitTime);
            }
        }
        public List<UnitTime> UnitTimes { get; set; }
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
    #region class UnitTime : Pracovní jednotka = kus práce na pracovišti
    /// <summary>
    /// UnitTime : Pracovní jednotka = kus práce na pracovišti
    /// </summary>
    public class UnitTime : RecordClass
    {
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
                BehaviorMode = GraphItemBehaviorMode.ShowCaptionAllways | GraphItemBehaviorMode.ShowToolTipFadeIn,
                Height = this.Height,
                Text = this.Text,
                ToolTip = this.ToolTip,
                Time = this.Time
            };

            // Aktivita se liší pro Pracoviště a pro Osobu:
            if (this.PlanUnitC != null && this.PlanUnitC.PlanUnitType == PlanUnitType.Workplace)
                guiGraphItem.BehaviorMode |=
                    GraphItemBehaviorMode.ShowLinkInMouseOver | GraphItemBehaviorMode.ShowLinkInSelected |
                    GraphItemBehaviorMode.MoveToAnotherRow | GraphItemBehaviorMode.MoveToAnotherTime | GraphItemBehaviorMode.ResizeTime;

            // Fixed:
            if (this.Operation != null && this.Operation.IsFixed)
            {
                guiGraphItem.BackStyle = System.Drawing.Drawing2D.HatchStyle.DarkHorizontal;
                Color backColor = this.BackColor;
                guiGraphItem.HatchColor = backColor.Morph(Color.Gray, 0.750f);
                guiGraphItem.ImageBegin = RES.Images.Actions24.Lock4Png;
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
        public override string ToString()
        {
            return this.Time.ToString();
        }
        public const int ClassNumber = 1365;
        public override int ClassId { get { return ClassNumber; } }
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
                //RatioBegin = this.RatioBegin,
                //RatioBeginBackColor = this.RatioBeginBackColor,
                //RatioEnd = this.RatioEnd,
                //RatioEndBackColor = this.RatioEndBackColor,
                //RatioLineColor = this.RatioLineColor
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
    #endregion
    #endregion
}
