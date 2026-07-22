using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Třída pro extrakci ikon z EXE, DLL a jiných souborů
    /// </summary>
    public class IconExtractor
    {
        // Win32 API importy
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phiconLarge,
            IntPtr[] phiconSmall,
            uint nIcons);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(
            IntPtr hInst,
            string lpszExeFileName,
            uint nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Získá ikonu ze souboru podle indexu
        /// </summary>
        /// <param name="filePath">Cesta k souboru (EXE, DLL, ICO, atd.)</param>
        /// <param name="iconIndex">Index ikony (0-based)</param>
        /// <param name="largeIcon">True pro velkou ikonu (32x32), False pro malou (16x16)</param>
        /// <returns>Icon objekt nebo null, pokud ikona není dostupná</returns>
        public static Icon GetIconFromFile(string filePath, int iconIndex = 0, bool largeIcon = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Cesta k souboru nesmí být prázdná.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Soubor '{filePath}' neexistuje.");

            try
            {
                return ExtractIconByIndex(filePath, iconIndex, largeIcon);
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba při extrakci ikony z '{filePath}' (index {iconIndex}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Převede Icon na Bitmap
        /// </summary>
        /// <param name="icon">Icon objekt</param>
        /// <returns>Bitmap reprezentující ikonu</returns>
        public static Bitmap IconToBitmap(Icon icon)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            return icon.ToBitmap();
        }

        /// <summary>
        /// Gets Icon/Bitmap z souboru s IconLocation a IconIndex z ShortcutInfo
        /// </summary>
        /// <param name="shortcutInfo">Objekt se zmformacemi o zástupci</param>
        /// <param name="returnBitmap">True pro Bitmap, False pro Icon</param>
        /// <returns>Icon nebo Bitmap, nebo null pokud se nepodařilo extrahovat</returns>
        public static object GetIconFromShortcut(ShortcutInfo shortcutInfo, bool returnBitmap = false)
        {
            if (shortcutInfo == null)
                throw new ArgumentNullException(nameof(shortcutInfo));

            try
            {
                // 1. Pokus: IconLocation z zástupce
                if (!string.IsNullOrWhiteSpace(shortcutInfo.IconLocation))
                {
                    object result = ExtractFromIconLocation(shortcutInfo.IconLocation, shortcutInfo.IconIndex, returnBitmap);
                    if (result != null)
                        return result;
                }

                // 2. Pokus: TargetPath
                if (!string.IsNullOrWhiteSpace(shortcutInfo.TargetPath) && File.Exists(shortcutInfo.TargetPath))
                {
                    Icon icon = ExtractIconByIndex(shortcutInfo.TargetPath, 0, true);
                    if (icon != null)
                        return returnBitmap ? (object)IconToBitmap(icon) : icon;
                }

                // 3. Fallback: Asociovaná ikona
                if (!string.IsNullOrWhiteSpace(shortcutInfo.TargetPath))
                {
                    Icon icon = Icon.ExtractAssociatedIcon(shortcutInfo.TargetPath);
                    if (icon != null)
                        return returnBitmap ? (object)IconToBitmap(icon) : icon;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba při extrakci ikony: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Detekuje počet ikon v souboru
        /// </summary>
        /// <param name="filePath">Cesta k souboru</param>
        /// <returns>Počet ikon v souboru</returns>
        public static int GetIconCount(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return 0;

            try
            {
                return (int)ExtractIconEx(filePath, 0, null, null, 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Exportuje všechny ikony ze souboru do disku
        /// </summary>
        /// <param name="filePath">Cesta ke zdrojovému souboru</param>
        /// <param name="outputDirectory">Výstupní adresář pro ICO soubory</param>
        /// <param name="baseName">Základní jméno pro exportované soubory (default: název souboru)</param>
        /// <returns>Počet úspěšně exportovaných ikon</returns>
        public static int ExportAllIcons(string filePath, string outputDirectory, string baseName = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Soubor '{filePath}' neexistuje.");

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            if (string.IsNullOrWhiteSpace(baseName))
                baseName = Path.GetFileNameWithoutExtension(filePath);

            int iconCount = GetIconCount(filePath);
            int exported = 0;

            for (int i = 0; i < iconCount; i++)
            {
                try
                {
                    Icon icon = ExtractIconByIndex(filePath, i, true);
                    if (icon != null)
                    {
                        string outputPath = Path.Combine(outputDirectory, $"{baseName}_{i}.ico");
                        using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                        {
                            icon.Save(fs);
                        }
                        icon.Dispose();
                        exported++;
                    }
                }
                catch
                {
                    // Pokračuj na další ikonu
                }
            }

            return exported;
        }

        // ==================== PRIVATE HELPER METODY ====================

        /// <summary>
        /// Interní metoda - extrahuje ikonu podle indexu
        /// </summary>
        private static Icon ExtractIconByIndex(string filePath, int iconIndex, bool largeIcon)
        {
            IntPtr[] largeIcons = new IntPtr[1];
            IntPtr[] smallIcons = new IntPtr[1];

            try
            {
                uint result = ExtractIconEx(
                    filePath,
                    iconIndex,
                    largeIcon ? largeIcons : null,
                    !largeIcon ? smallIcons : null,
                    1);

                if (result == 0)
                    return null;

                IntPtr iconHandle = largeIcon ? largeIcons[0] : smallIcons[0];
                if (iconHandle == IntPtr.Zero)
                    return null;

                return Icon.FromHandle(iconHandle);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Interní metoda - parsuje IconLocation a extrahuje ikonu
        /// </summary>
        private static object ExtractFromIconLocation(string iconLocation, int defaultIndex, bool returnBitmap)
        {
            if (string.IsNullOrWhiteSpace(iconLocation))
                return null;

            string filePath;
            int iconIndex = defaultIndex;

            // Formát: "C:\path\to\file.exe,0"
            int commaIndex = iconLocation.LastIndexOf(',');
            if (commaIndex > 0)
            {
                filePath = iconLocation.Substring(0, commaIndex).Trim('"');
                if (int.TryParse(iconLocation.Substring(commaIndex + 1).Trim(), out int parsedIndex))
                    iconIndex = parsedIndex;
            }
            else
            {
                filePath = iconLocation.Trim('"');
            }

            // Ujistit se, že cesta je absolutní
            if (!Path.IsPathRooted(filePath))
                return null;

            if (!File.Exists(filePath))
                return null;

            try
            {
                Icon icon = ExtractIconByIndex(filePath, iconIndex, true);
                if (icon != null)
                    return returnBitmap ? (object)IconToBitmap(icon) : icon;
            }
            catch
            {
                // Pokus o fallback na index 0
                try
                {
                    Icon icon = ExtractIconByIndex(filePath, 0, true);
                    if (icon != null)
                        return returnBitmap ? (object)IconToBitmap(icon) : icon;
                }
                catch
                {
                    // Ticho
                }
            }

            return null;
        }
    }
}
