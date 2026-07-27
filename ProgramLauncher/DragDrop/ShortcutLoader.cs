using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Třída pro načítání a parsování Windows zástupců (.lnk soubory)
    /// </summary>
    public class ShortcutLoader
    {
        /// <summary>
        /// Získej seznam .lnk souborů z Clipboardu.
        /// Pokud tam nic není, vrátí prázdné pole, ale ne null.
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
        /// <para/>
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
        /// Zkusí načíst obsah Windows zástupce (<see cref="ShortcutInfo"/>) z daného souboru.
        /// <para/>
        /// Pokud se nezdaří, vrací false. Nevyhodí chybu.
        /// </summary>
        /// <param name="shortcutFile">Cesta k souboru *.lnk</param>
        /// <returns>Objekt <see cref="ShortcutInfo"/> s načtenými údaji</returns>
        public static bool TryLoadShortcutFromFile(string shortcutFile, out ShortcutInfo shortcut)
        {
            return _TryLoadShortcutFromFile(shortcutFile, out shortcut, out var _);
        }
        /// <summary>
        /// Zkusí načíst obsah Windows zástupců (<see cref="ShortcutInfo"/>) z dodaných souborů. 
        /// <para/>
        /// Pokud na vstupu nic není, anebo se nezdaří, vrací prázdné pole. Nevrací null, nevyhodí chybu.
        /// </summary>
        /// <param name="shortcutFiles">Cesta k souboru *.lnk</param>
        /// <returns>Pole objektů <see cref="ShortcutInfo"/> s načtenými údaji</returns>
        public static ShortcutInfo[] LoadShortcutsFromFiles(string[] shortcutFiles)
        {
            var shortcuts = new List<ShortcutInfo>();
            if (shortcutFiles != null && shortcutFiles.Length > 0)
            {
                foreach (var shortcutFile in shortcutFiles)
                {
                    if (_TryLoadShortcutFromFile(shortcutFile, out var shortcut, out var _))
                        shortcuts.Add(shortcut);
                }
            }
            return shortcuts.ToArray();
        }
        /// <summary>
        /// Zkusí načíst obsah Windows zástupce z daného souboru. 
        /// <para/>
        /// Pokud se nezdaří, vrací false. Nevyhodí chybu.
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
            bool result = false;
            shortcut = null;
            errorText = null;

            // Validace vstupů
            if (string.IsNullOrWhiteSpace(lnkFilePath))
            {
                errorText = $"Není zadán soubor obsahující Shortcut.";
                return result;
            }

            lnkFilePath = lnkFilePath.Trim();
            if (!lnkFilePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                errorText = $"Soubor '{lnkFilePath}' není typu Shortcut.";
                return result;
            }
            if (!File.Exists(lnkFilePath))
            {
                errorText = $"Soubor '{lnkFilePath}' typu Shortcut neexistuje.";
                return result;
            }


            IWshRuntimeLibrary.WshShell wShell = null;
            IWshRuntimeLibrary.IWshShortcut wShortcut = null;
            try
            {
                // 1. Načtu COM objekt pro Shortcut:
                wShell = new IWshRuntimeLibrary.WshShell();
                wShortcut = (IWshRuntimeLibrary.IWshShortcut)wShell.CreateShortcut(lnkFilePath);

                // 2. Vytvořím náš objekt ShortcutInfo a naplnění jeho vlastností:
                var shortcutInfo = new ShortcutInfo();
                shortcutInfo.LinkPath = lnkFilePath;
                shortcutInfo.LinkName = Path.GetFileNameWithoutExtension(lnkFilePath);
                shortcutInfo.TargetPath = tryGetValue(() => wShortcut.TargetPath, "") ?? "";
                shortcutInfo.Arguments = tryGetValue(() => wShortcut.Arguments, "") ?? "";
                shortcutInfo.WorkingDirectory = tryGetValue(() => wShortcut.WorkingDirectory, "") ?? "";
                shortcutInfo.Description = tryGetValue(() => wShortcut.Description, "") ?? "";
                shortcutInfo.IconInfo = tryGetValue(() => wShortcut.IconLocation, "") ?? "";
                shortcutInfo.WindowStyle = (WindowStyle)(tryGetValue(() => wShortcut.WindowStyle, 0));
                shortcutInfo.HotKey = tryGetValue(() => wShortcut.Hotkey, "") ?? "";
                shortcutInfo.Flags = parseFlags(wShortcut);

                _FillIconIndex(shortcutInfo);

                // 3. Výstup:
                shortcut = shortcutInfo;
                result = true;
                return true;
            }
            catch (Exception ex)
            {
                errorText = $"Chyba při čtení zástupce '{lnkFilePath}': {ex.Message}";
            }
            finally
            {
                try { if (wShortcut != null) Marshal.FinalReleaseComObject(wShortcut); } catch { }
                wShortcut = null;

                try { if (wShell != null) Marshal.FinalReleaseComObject(wShell); } catch { }
                wShell = null;
            }

            return result;

            // Pokusí se načíst hodnotu pomocí dané funkce
            T tryGetValue<T>(Func<T> func, T defaultValue)
            {
                T value = defaultValue;
                try
                {
                    value = func();
                }
                catch
                {
                    value = defaultValue;
                }
                return value;
            }
            // Parsuje speciální příznaky zástupce
            ShortcutFlags parseFlags(IWshRuntimeLibrary.IWshShortcut shortcutLink)
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
        /// Parsuje index ikony z řetězce IconInfo
        /// </summary>
        /// <param name="iconInfo"></param>
        private static void _FillIconIndex(ShortcutInfo shortcutInfo)
        {
            string iconInfo = shortcutInfo.IconInfo;
            shortcutInfo.IconLocation = iconInfo;
            shortcutInfo.IconIndex = 0;

            if (String.IsNullOrEmpty(iconInfo)) return;

            // Formát je obvykle: "cesta\k\souboru,indexIkony"
            int commaIndex = iconInfo.LastIndexOf(',');
            if (commaIndex > 0 && commaIndex < (iconInfo.Length - 1) && Int32.TryParse(iconInfo.Substring(commaIndex + 1).Trim(), out int index))
            {
                shortcutInfo.IconLocation = iconInfo.Substring(0, commaIndex);
                shortcutInfo.IconIndex = index;
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
