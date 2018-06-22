using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    public class Constants
    {
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
}
