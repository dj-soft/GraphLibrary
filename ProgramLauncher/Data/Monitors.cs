using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DjSoft.Tools.ProgramLauncher
{
    /// <summary>
    /// Třída která usnadňuje práci s okny na více monitorech
    /// </summary>
    public class Monitors
    {
        /// <summary>
        /// Aktuální klíč konfigurace všech monitorů.
        /// String obsahuje jednotlivé monitory, jejich příznak Primární, a jejich souřadnice.
        /// Je vhodné ukládat souřadnice pracovních oken pro jejich restorování s klíčem aktuální konfigurace monitorů: 
        /// uživatel používající různé konfigurace monitorů očekává, že konkrétní okno se mu po otevření zobrazí na konkrátním místě v závislosti na tom, které monitory právě používá.
        /// </summary>
        public static string CurrentMonitorsKey { get { return _GetCurrentMonitorsKey(); } }
        /// <summary>
        /// Určí a vrátí Aktuální klíč konfigurace všech monitorů.
        /// </summary>
        /// <returns></returns>
        private static string _GetCurrentMonitorsKey()
        {
            string key = "";
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                key += $".[{(screen.Primary ? "P" : "S")}:{Convertor.RectangleToString(screen.Bounds)}]";
            return (key.Length > 0 ? key.Substring(1) : "");
        }
        /// <summary>
        /// Metoda určí souřadnice toho monitoru, který je nejblíže dané souřadnici. 
        /// Výstupem je kompletní souřadnice monitoru, nikoli zadaná vstupní souřadnice umístěná do toho monitoru.
        /// <para/>
        /// Je vyhledán monitor, s nímž má vstupní souřadnice největší společnou plochu (průnik), 
        /// anebo pokud nemá průnik se žádným monitorem, pak má k němu nejbližší vzdálenost.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="acceptWholeBounds">Brát do úvahy: true = celé souřadnice monitoru (včetně systémvé oblasti) / false = default = jen pracovní oblast, kam se běžně zobrazují aplikace </param>
        /// <returns></returns>
        public static Rectangle GetNearestMonitorBounds(Rectangle bounds, bool acceptWholeBounds = false)
        {
            // Zkratka pro záporný bounds:
            if (bounds.Width < 0 || bounds.Height < 0)
                return getBounds(System.Windows.Forms.Screen.PrimaryScreen);

            // Zkratka pro jediný monitor:
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens.Length == 1) return getBounds(System.Windows.Forms.Screen.PrimaryScreen);

            // Máme tedy více monitorů; najděme mezi nimi ten nejvhodnější:
            int? maximalCommonPixels = null;               // Dosud největší společná plocha (pixely čtverečné)
            Rectangle? maximalCommonMonitorBounds = null;  // Souřadnice monitoru, který má dosud největší společnou plochu
            int? nearestDistance = null;                   // Dosud nejnižší vzdálenost sousedního monitoru
            Rectangle? nearestMonitorBounds = null;        // Souřadnice monitoru, který nemá společnou plochu ale je nejbližší

            foreach (var screen in screens)
            {
                var monitorBounds = getBounds(screen);     // Prostor monitoru
                monitorBounds.DetectRelation(bounds, out var distance, out var commonBounds);

                if (commonBounds.HasValue)
                {   // Máme v tomto monitoru společný prostor - pokud prostor bude větší než v dřívějším, akceptujeme jej:
                    int commonPixels = commonBounds.Value.GetPixelsCount();
                    if (!maximalCommonPixels.HasValue || (maximalCommonPixels.HasValue && maximalCommonPixels.Value < commonPixels))
                    {
                        maximalCommonPixels = commonPixels;
                        maximalCommonMonitorBounds = monitorBounds;
                    }
                }
                else if (distance.HasValue)
                {   // Nemáme sice v tomto monitoru společný prostor, ale známe vzdálenost k němu - pokud distance bude menší než k dřívějším, akceptujeme jej:
                    if (!nearestDistance.HasValue || (nearestDistance.HasValue && nearestDistance.Value > distance.Value))
                    {
                        nearestDistance = distance;
                        nearestMonitorBounds = monitorBounds;
                    }
                }
            }

            if (maximalCommonMonitorBounds.HasValue) return maximalCommonMonitorBounds.Value;
            if (nearestMonitorBounds.HasValue) return nearestMonitorBounds.Value;
            return getBounds(System.Windows.Forms.Screen.PrimaryScreen);


            // Vrátí prostor Bounds / WorkingArea z daného monitoru, podle acceptWholeBounds:
            Rectangle getBounds(System.Windows.Forms.Screen screen)
            {
                return (acceptWholeBounds ? screen.Bounds : screen.WorkingArea);
            }
        }
    }
}
