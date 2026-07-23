using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Třída pro načítání a parsování Windows zástupců (.lnk soubory)
    /// </summary>
    public class ShortcutLoader
    {
        /// <summary>
        /// Získej seznam .lnk souborů z Clipboardu
        /// </summary>
        public static string[] GetShortcutsFilesFromClipboard()
        {
            var shortcuts = new List<string>();

            try
            {
                if (!Clipboard.ContainsFileDropList())
                    return shortcuts.ToArray();

                var files = Clipboard.GetFileDropList();
                foreach (string file in files)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(file))
                        {
                            var shortcutFile = file.Trim();
                            if (shortcutFile.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(shortcutFile))
                                shortcuts.Add(shortcutFile);
                        }
                    }
                    catch (Exception ex) { /* Chybné jméno, práva, atd... */ }
                }
            }
            catch (Exception ex) { /* Chyba při čtení Clipboardu */ }

            return shortcuts.ToArray();
        }
        /// <summary>
        /// Načte obsah Windows zástupce z daného souboru.
        /// Pokud se nepodaří, dojde k chybě.
        /// </summary>
        /// <param name="shortcutFile">Cesta k souboru *.lnk</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Soubor neexistuje nebo není .lnk soubor</exception>
        public static ShortcutInfo LoadShortcutFromFile(string shortcutFile)
        {
            var success = _TryLoadShortcutFromFile(shortcutFile, out ShortcutInfo shortcut, out string errorText);
            if (success) return shortcut;
            throw new ArgumentException(errorText);
        }
        /// <summary>
        /// Zkusí načíst obsah Windows zástupce z daného souboru. Pokud se nezdaří, vrací false. Nevyhodí chybu.
        /// </summary>
        /// <param name="shortcutFile">Cesta k souboru *.lnk</param>
        /// <returns>Objekt ShortcutInfo s načtenými údaji</returns>
        public static bool TryLoadShortcutFromFile(string shortcutFile, out ShortcutInfo shortcut)
        {
            return _TryLoadShortcutFromFile(shortcutFile, out shortcut, out var _);
        }
        /// <summary>
        /// Zkusí načíst obsah Windows zástupce z daného souboru. Pokud se nezdaří, vrací false. Nevyhodí chybu.
        /// </summary>
        /// <param name="shortcutFile">Cesta k souboru *.lnk</param>
        /// <returns>Objekt ShortcutInfo s načtenými údaji</returns>
        public static bool TryLoadShortcutFromFile(string shortcutFile, out ShortcutInfo shortcut, out string errorText) 
        {
            return _TryLoadShortcutFromFile(shortcutFile, out shortcut, out errorText);
        }
        /// <summary>
        /// Načte obsah Windows zástupce z daného souboru
        /// </summary>
        /// <param name="lnkFilePath">Cesta k souboru *.lnk</param>
        /// <returns>Objekt ShortcutInfo s načtenými údaji</returns>
        private static bool _TryLoadShortcutFromFile(string lnkFilePath, out ShortcutInfo shortcut, out string errorText)
        {
            shortcut = null;
            errorText = null;

            // Validace vstupů
            if (string.IsNullOrWhiteSpace(lnkFilePath))
            {
                errorText = $"Není zadán soubor obsahující Shortcut.";
                return false;
            }

            lnkFilePath = lnkFilePath.Trim();
            if (!lnkFilePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                errorText = $"Soubor '{lnkFilePath}' není typu Shortcut.";
                return false;
            }
            if (!File.Exists(lnkFilePath))
            {
                errorText = $"Soubor '{lnkFilePath}' typu Shortcut neexistuje.";
                return false;
            }

            try
            {
                // Vytvoření Shell objektu pro práci se zástupci
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                
                // Načtení objektu zástupce
                dynamic shortcutLink = shell.CreateShortCut(lnkFilePath);

                // Vytvoření objektu ShortcutInfo a naplnění jeho vlastností
                var shortcutInfo = new ShortcutInfo
                {
                    LinkPath = lnkFilePath,
                    LinkName = Path.GetFileNameWithoutExtension(lnkFilePath),
                    TargetPath = shortcutLink.TargetPath ?? "",
                    Arguments = shortcutLink.Arguments ?? "",
                    WorkingDirectory = shortcutLink.WorkingDirectory ?? "",
                    Description = shortcutLink.Description ?? "",
                    IconLocation = shortcutLink.IconLocation ?? "",
                    IconIndex = parseIconIndex(shortcutLink.IconLocation),
                    WindowStyle = (WindowStyle)(shortcutLink.WindowStyle ?? 1),
                    HotKey = shortcutLink.Hotkey ?? "",
                    RelativePath = shortcutLink.RelativePath ?? "",
                    Flags = parseFlags(shortcutLink)
                };

                // Uvolnění COM objektů
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcutLink);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

                shortcut = shortcutInfo;
                return true;
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                errorText = $"Chyba při čtení zástupce '{lnkFilePath}': {ex.Message}";
                return false;
            }

            // Parsuje index ikony z řetězce IconLocation
            int parseIconIndex(string iconLocation)
            {
                if (string.IsNullOrEmpty(iconLocation))
                    return 0;

                // Formát je obvykle: "cesta\k\souboru,indexIkony"
                int commaIndex = iconLocation.LastIndexOf(',');
                if (commaIndex > 0 && int.TryParse(iconLocation.Substring(commaIndex + 1).Trim(), out int index))
                    return index;

                return 0;
            }
            // Parsuje speciální příznaky zástupce
            ShortcutFlags parseFlags(dynamic shortcutLink)
            {
                var flags = ShortcutFlags.None;

                try
                {
                    // Kontrola příznaku pro spuštění jako administrátor
                    // Poznámka: Toto není dostupné přes standardní COM API WshShell
                    // Pro plnou detekci by bylo potřeba parsovat binární formát .lnk souboru
                    // nebo použít OS API na nižší úrovni
                }
                catch
                {
                    // Ignorovat chyby při čtení příznaků
                }

                return flags;
            }
        }

        /// <summary>
        /// Uloží (vytvoří nový) zástupce s danými vlastnostmi
        /// </summary>
        /// <param name="lnkFilePath">Cesta, kam uložit zástupce</param>
        /// <param name="shortcutInfo">Údaje zástupce</param>
        /// <exception cref="ArgumentException">Neplatné vstupy</exception>
        /// <exception cref="Exception">Chyba při vytváření zástupce</exception>
        public static void SaveShortcut(string lnkFilePath, ShortcutInfo shortcutInfo)
        {
            if (string.IsNullOrWhiteSpace(lnkFilePath))
                throw new ArgumentException("Cesta k souboru nesmí být prázdná.", nameof(lnkFilePath));

            if (shortcutInfo == null)
                throw new ArgumentNullException(nameof(shortcutInfo));

            if (string.IsNullOrWhiteSpace(shortcutInfo.TargetPath))
                throw new ArgumentException("Cílový soubor musí být zadán.", nameof(shortcutInfo));

            try
            {
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic shortcutLink = shell.CreateShortCut(lnkFilePath);

                shortcutLink.TargetPath = shortcutInfo.TargetPath;
                shortcutLink.Arguments = shortcutInfo.Arguments ?? "";
                shortcutLink.WorkingDirectory = shortcutInfo.WorkingDirectory ?? "";
                shortcutLink.Description = shortcutInfo.Description ?? "";
                shortcutLink.IconLocation = shortcutInfo.IconLocation ?? "";
                shortcutLink.WindowStyle = (int)shortcutInfo.WindowStyle;
                shortcutLink.Hotkey = shortcutInfo.HotKey ?? "";

                shortcutLink.Save();

                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcutLink);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba při vytváření zástupce '{lnkFilePath}': {ex.Message}", ex);
            }
        }
    }
}
