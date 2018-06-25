using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    public class Constants
    {
    }
    #region class GreenClasses
    /// <summary>
    /// 
    /// </summary>
    public class GreenClasses
    {
        #region Konstanty - čísla tříd
        public const int SourceCharacteristic = 1141;    // Klasifikace
        public const int Processing = 1142;    // Zpracování
        public const int Workplace = 1143;    // Pracoviště
        public const int StandardMfrOperations = 1144;    // Standardní operace
        public const int ConstrElement = 1151;    // Dílec
        public const int StructureMod = 1153;    // K modifikace
        public const int ProtocolChng = 1154;    // Protokol změny
        public const int OperationMod = 1155;    // T modifikace
        public const int Structure = 1156;    // Komponenta kusovníku
        public const int Operation = 1157;    // Operace postupu
        public const int PlanUnitS = 1160;    // Plánovací jednotka S zdrojů
        public const int AStructureMod = 1161;    // K modifikace STPV
        public const int AStructure = 1162;    // Komponenta kusovníku STPV
        public const int AOperationMod = 1163;    // T modifikace STPV
        public const int AOperation = 1164;    // Operace postupu STPV
        public const int AStructOperRel = 1165;    // Komponenta operace STPV
        public const int ExpHdr = 1167;    // Uložené rozpady - Hlavička
        public const int StructOperRel = 1176;    // Komponenta operace
        public const int ResourceAllocation = 1177;    // Zdroj operace
        public const int MfrModulConfig = 1178;    // Konfigurace modulu Výroba
        public const int PlanUnitSSl = 1179;    // Plánovací jednotka S stav skladu
        public const int PlanUnitSAxis = 1180;    // Plánovací jednotka S osa
        public const int PlanUnitSAxisRel = 1183;    // Plánovací jednotka S osa vztahy
        public const int StockOfDep = 1184;    // Sklady útvaru
        public const int AlternativeSource = 1185;    // Alternativní komponenta
        public const int ConstrElementConfig = 1187;    // Konfigurace katalogu dílců
        public const int ProductOrder = 1188;    // Výrobní příkaz
        public const int ProductOrderStructure = 1189;    // Komponenta výrobního příkazu
        public const int ProductOrderOperation = 1190;    // Operace výrobního příkazu
        public const int ProductOrderConfig = 1191;    // Konfigurace výrobního příkazu
        public const int OtherCosts = 1193;    // Ostatní náklady standardní
        public const int OtherCostsPO = 1194;    // Ostatní náklady VP
        public const int AConstrElement = 1195;    // Dílec plánovací jednotky
        public const int MfrTariffSet = 1209;    // Sady výrobních sazeb
        public const int OverheadCosts = 1210;    // Režie
        public const int PricingComponent = 1211;    // Složka kalkulace
        public const int OverheadCostsSet = 1212;    // Sady režií
        public const int MfrTariff = 1213;    // Výrobní sazby
        public const int PricingPar = 1214;    // Parametry kalkulací
        public const int Pricing = 1215;    // Kalkulace
        public const int ExpStructure = 1218;    // Uložené rozpady - Kusovník
        public const int PricingDetail = 1219;    // Detail kalkulace
        public const int OperationReport = 1232;    // Odvedení operace
        public const int BusinessPlan = 1255;    // Obchodní plán
        public const int BusinessPlanConfig = 1260;    // Konfigurace obchodního plánu
        public const int ProductOrderCosts = 1268;    // Výrobní pohyb
        public const int CumulDocWIP = 1269;    // Kumulovaný doklad NV
        public const int AccountingStandardWIP = 1275;    // Předpis kontací NV
        public const int MRPProtocol = 1276;    // MRP protokol
        public const int OperativeWIP = 1307;    // Operativní rozpracovanost výroby
        public const int Reject = 1309;    // Vyřazení
        public const int PricingFlow = 1318;    // Postupová kalkulace
        public const int DefectCode = 1324;    // Kód závady
        public const int PriceArraySl = 1325;    // Cenový vektor stavu skladu
        public const int WIP = 1332;    // Nedokončená výroba
        public const int WIPLevel = 1335;    // Stav nedokončené výroby
        public const int WIPByOper = 1336;    // Nedokončená výroba po operacích
        public const int WIPDetail = 1337;    // Nedokončená výroba detail
        public const int PlanCVersion = 1362;    // Kapacitní plán verze
        public const int PlanUnitCTask = 1363;    // Kapacitní úkol
        public const int PlanUnitC = 1364;    // Kapacitní plánovací jednotka
        public const int PlanUnitCCl = 1365;    // Stav kapacit
        public const int PlanUnitSCRel = 1366;    // Kapacitní úkoly a S osa vztahy
        public const int PlanUnitCClRel = 1367;    // Kap. úkoly a stav kap. vztahy
        public const int InventuraNV = 1370;    // Inventura NV
        public const int KonfiguraceInventuryNV = 1374;    // Konfigurace inventury NV
        public const int PUSAxisLink = 1386;    // Zajištění požadavku
        public const int AAlternativeSource = 1390;    // Alternativní komponenta STPV
        public const int TestDataConsistence = 1393;    // Test konzistence dat
        public const int PlanCTaskLink = 1395;    // Vztahy kapacitních úkolů
        public const int OtherCostsDevelop = 1411;    // Ostatní náklady VTPV
        public const int OtherCostsRatif = 1412;    // Ostatní náklady STPV
        public const int CumulDocWIPConfig = 1427;    // Konfigurace kumul.dokl.NV
        public const int MoveTimeMatrix = 1438;    // Matice mezioperačních časů
        public const int AResourceAllocation = 1439;    // Zdroj operace STPV
        public const int ProductOrderResourceAlloc = 1441;    // Zdroj operace VP
        public const int RequirementCategories = 1455;    // Kategorie požadavků
        public const int PlanRealLink = 1482;    // Propojení plánu a reality obj.
        public const int ImpConstrElement = 1490;    // Dílec importního TPV
        public const int ImpStructure = 1491;    // Komponenta importního TPV
        public const int ImpOperation = 1492;    // Operace importního TPV
        public const int ImpOperationMod = 1493;    // T modifikace importního TPV
        public const int ImpKmenovaKarta = 1500;    // Kmenová karta importního TPV
        public const int ImpResourceAllocation = 1501;    // Zdroj operace ITPV
        public const int WorkEvidenceConfig = 1518;    // Konfigurace evidence činností
        public const int WorkEvidence = 1519;    // Evidence činností
        public const int ActionCode = 1520;    // Kód akce
        public const int TerminalData = 1521;    // Data terminálů
        public const int TerminalDevice = 1527;    // Terminál
        public const int Tools = 1535;    // Nářadí
        public const int ToolsType = 1542;    // Typ nářadí
        public const int PlanCSourceLink = 1592;    // Vztahy kap. jednotek a zdrojů
        public const int AlternativeOperation = 1595;    // Alternativní operace
        public const int AlternativeAOperation = 1596;    // Alternativní operace STPV
        public const int AlternativePoOperation = 1597;    // Alternativní operace VP
        public const int CallOffIn = 1628;    // Odvolávka přijatá
        public const int CallOffInConfig = 1630;    // Konfigurace odvolávek přijat.
        public const int CallOffInItem = 1631;    // Odvolávka přijatá položka
        public const int PlanUnitCPass = 1815;    // Kapacitní úkol, paralelní průchod
        public const int PlanUnitCTime = 1816;    // Kapacitní úkol, pracovní čas
        public const int PlanUnitCUnit = 1817;    // Kapacitní úkol, pracovní jednotka
        public const int PlanUnitCDesk = 1818;    // Stav kapacit, kapacitní linka
        public const int CheckPlan = 1892;    // Kontrolní plán
        public const int ACheckPlan = 1893;    // Kontrolní plán STPV
        public const int BatchNumberStatus = 1901;    // Stav šarže
        public const int WorkTeam = 1902;    // Pracovní tým
        public const int OperationReportWorkers = 1903;    // Zaměstnanci odvedení operace
        public const int InventoryFindWip = 1949;    // Inventurní nález NV
        public const int CallOffInPack = 1960;    // Odvolávka přijatá balení
        public const int CallOffInDeliv = 1961;    // Odvolávka přijatá dodávky
        public const int SchedulerConfig = 1963;    // Konfigurace modulu Scheduler
        public const int EDIUdajeProExpozituru = 2028;    // EDI údaje pro expozituru cizí
        public const int EDIUdajeDodavatele = 2029;    // EDI údaje dodavatele
        public const int TextFMEA = 2062;    // Text FMEA
        public const int FMEASTPV = 2064;    // FMEA STPV
        public const int FMEA = 2065;    // FMEA
        public const int MrpBussinesCaseInfo = 2178;    // MRP informace obch. případu
        public const int TextCheckPlan = 2186;    // Text kontrolního plánu
        public const int NonconformanceStats = 2198;    // Statistika neshod
        public const int WorkReportStats = 2207;    // Statistika odvedení práce
        public const int SumarVyrobnichPohybu = 2210;    // Sumář výrobních pohybů
        public const int PricingItem = 2234;    // Kalkulace položka
        public const int OfferBudget = 2266;    // Rozpočet nabídky
        public const int CallOffOut = 2296;    // Odvolávka vydaná
        public const int CallOffOutConfig = 2297;    // Konfigurace odvolávek vydaných
        public const int PricingElement = 2298;    // Položka kalkulace
        public const int CallOffOutItem = 2299;    // Odvolávka vydaná položka
        public const int PlanUnitCritical = 2391;    // Kritická položka plánu výroby
        public const int PricingDetailDecomp = 2409;    // Rozpad detailní kalkulace
        public const int BatchNumberDecomp = 2444;    // Rozpad šarže
        public const int AuditlogMessagesConfig = 2496;    // Konfigurace zpráv auditlogu
        public const int SimulCustOrderHead = 2499;    // Simulace zákaznické objednávky
        public const int SimulCustOrderEntry = 2500;    // Simulace zákaz. obj., položka
        public const int WorkshopPlan = 2540;    // Dílenský plán

        public const int ResourcesCalendar = 1196;    // Kalendář zdrojů
        public const int ResourcesCalendarData = 1207;    // Položky kalendáře zdrojů
        #endregion
        #region Čísla a názvy tříd Helios Green
        /// <summary>
        /// Metoda vrátí název dané třídy Helios Green.
        /// Nepracuje s databází, má v paměti čísla tříd Manufacturing ke dni 22.6.2018.
        /// </summary>
        /// <param name="classNumber"></param>
        /// <returns></returns>
        public static string GetClassName(int classNumber)
        {
            switch (classNumber)
            {
                // Třídy z Manufacturing:
                case 1141: return "Klasifikace";
                case 1142: return "Zpracování";
                case 1143: return "Pracoviště";
                case 1144: return "Standardní operace";
                case 1151: return "Dílec";
                case 1153: return "K modifikace";
                case 1154: return "Protokol změny";
                case 1155: return "T modifikace";
                case 1156: return "Komponenta kusovníku";
                case 1157: return "Operace postupu";
                case 1160: return "Plánovací jednotka S zdrojů";
                case 1161: return "K modifikace STPV";
                case 1162: return "Komponenta kusovníku STPV";
                case 1163: return "T modifikace STPV";
                case 1164: return "Operace postupu STPV";
                case 1165: return "Komponenta operace STPV";
                case 1167: return "Uložené rozpady - Hlavička";
                case 1176: return "Komponenta operace";
                case 1177: return "Zdroj operace";
                case 1178: return "Konfigurace modulu Výroba";
                case 1179: return "Plánovací jednotka S stav skladu";
                case 1180: return "Plánovací jednotka S osa";
                case 1183: return "Plánovací jednotka S osa vztahy";
                case 1184: return "Sklady útvaru";
                case 1185: return "Alternativní komponenta";
                case 1187: return "Konfigurace katalogu dílců";
                case 1188: return "Výrobní příkaz";
                case 1189: return "Komponenta výrobního příkazu";
                case 1190: return "Operace výrobního příkazu";
                case 1191: return "Konfigurace výrobního příkazu";
                case 1193: return "Ostatní náklady standardní";
                case 1194: return "Ostatní náklady VP";
                case 1195: return "Dílec plánovací jednotky";
                case 1209: return "Sady výrobních sazeb";
                case 1210: return "Režie";
                case 1211: return "Složka kalkulace";
                case 1212: return "Sady režií";
                case 1213: return "Výrobní sazby";
                case 1214: return "Parametry kalkulací";
                case 1215: return "Kalkulace";
                case 1218: return "Uložené rozpady - Kusovník";
                case 1219: return "Detail kalkulace";
                case 1232: return "Odvedení operace";
                case 1255: return "Obchodní plán";
                case 1260: return "Konfigurace obchodního plánu";
                case 1268: return "Výrobní pohyb";
                case 1269: return "Kumulovaný doklad NV";
                case 1275: return "Předpis kontací NV";
                case 1276: return "MRP protokol";
                case 1307: return "Operativní rozpracovanost výroby";
                case 1309: return "Vyřazení";
                case 1318: return "Postupová kalkulace";
                case 1324: return "Kód závady";
                case 1325: return "Cenový vektor stavu skladu";
                case 1332: return "Nedokončená výroba";
                case 1335: return "Stav nedokončené výroby";
                case 1336: return "Nedokončená výroba po operacích";
                case 1337: return "Nedokončená výroba detail";
                case 1362: return "Kapacitní plán verze";
                case 1363: return "Kapacitní úkol";
                case 1364: return "Kapacitní plánovací jednotka";
                case 1365: return "Stav kapacit";
                case 1366: return "Kapacitní úkoly a S osa vztahy";
                case 1367: return "Kap. úkoly a stav kap. vztahy";
                case 1370: return "Inventura NV";
                case 1374: return "Konfigurace inventury NV";
                case 1386: return "Zajištění požadavku";
                case 1390: return "Alternativní komponenta STPV";
                case 1393: return "Test konzistence dat";
                case 1395: return "Vztahy kapacitních úkolů";
                case 1411: return "Ostatní náklady VTPV";
                case 1412: return "Ostatní náklady STPV";
                case 1427: return "Konfigurace kumul.dokl.NV";
                case 1438: return "Matice mezioperačních časů";
                case 1439: return "Zdroj operace STPV";
                case 1441: return "Zdroj operace VP";
                case 1455: return "Kategorie požadavků";
                case 1482: return "Propojení plánu a reality obj.";
                case 1490: return "Dílec importního TPV";
                case 1491: return "Komponenta importního TPV";
                case 1492: return "Operace importního TPV";
                case 1493: return "T modifikace importního TPV";
                case 1500: return "Kmenová karta importního TPV";
                case 1501: return "Zdroj operace ITPV";
                case 1518: return "Konfigurace evidence činností";
                case 1519: return "Evidence činností";
                case 1520: return "Kód akce";
                case 1521: return "Data terminálů";
                case 1527: return "Terminál";
                case 1535: return "Nářadí";
                case 1542: return "Typ nářadí";
                case 1592: return "Vztahy kap. jednotek a zdrojů";
                case 1595: return "Alternativní operace";
                case 1596: return "Alternativní operace STPV";
                case 1597: return "Alternativní operace VP";
                case 1628: return "Odvolávka přijatá";
                case 1630: return "Konfigurace odvolávek přijat.";
                case 1631: return "Odvolávka přijatá položka";
                case 1815: return "Kapacitní úkol, paralelní průchod";
                case 1816: return "Kapacitní úkol, pracovní čas";
                case 1817: return "Kapacitní úkol, pracovní jednotka";
                case 1818: return "Stav kapacit, kapacitní linka";
                case 1892: return "Kontrolní plán";
                case 1893: return "Kontrolní plán STPV";
                case 1901: return "Stav šarže";
                case 1902: return "Pracovní tým";
                case 1903: return "Zaměstnanci odvedení operace";
                case 1949: return "Inventurní nález NV";
                case 1960: return "Odvolávka přijatá balení";
                case 1961: return "Odvolávka přijatá dodávky";
                case 1963: return "Konfigurace modulu Scheduler";
                case 2028: return "EDI údaje pro expozituru cizí";
                case 2029: return "EDI údaje dodavatele";
                case 2062: return "Text FMEA";
                case 2064: return "FMEA STPV";
                case 2065: return "FMEA";
                case 2178: return "MRP informace obch. případu";
                case 2186: return "Text kontrolního plánu";
                case 2198: return "Statistika neshod";
                case 2207: return "Statistika odvedení práce";
                case 2210: return "Sumář výrobních pohybů";
                case 2234: return "Kalkulace položka";
                case 2265: return "Konfigurace rozpočtu nabídky";
                case 2266: return "Rozpočet nabídky";
                case 2296: return "Odvolávka vydaná";
                case 2297: return "Konfigurace odvolávek vydaných";
                case 2298: return "Položka kalkulace";
                case 2299: return "Odvolávka vydaná položka";
                case 2391: return "Kritická položka plánu výroby";
                case 2409: return "Rozpad detailní kalkulace";
                case 2444: return "Rozpad šarže";
                case 2496: return "Konfigurace zpráv auditlogu";
                case 2499: return "Simulace zákaznické objednávky";
                case 2500: return "Simulace zákaz. obj., položka";
                case 2540: return "Dílenský plán";
                // Třídy z Base:
                case 1196: return "Kalendář zdrojů";
                case 1207: return "Položky kalendáře zdrojů";
            }
            return null;
        }
        #endregion
    }
    #endregion
}
