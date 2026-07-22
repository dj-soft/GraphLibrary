using System;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Příklad použití tříd pro práci se Windows zástupci
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Příklad 1: Načtení zástupce z disku
            Console.WriteLine("=== NAČÍTÁNÍ ZÁSTUPCE ===\n");
            
            try
            {
                // Nahraďte cestu na existující .lnk soubor
                string shortcutPath = @"C:\Users\YourUsername\Desktop\YourShortcut.lnk";

                Console.WriteLine($"Načítám zástupce: {shortcutPath}\n");
                ShortcutInfo shortcut = ShortcutLoader.LoadShortcut(shortcutPath);

                // Výstup všech vlastností
                Console.WriteLine(shortcut);

                // Přístup k jednotlivým vlastnostem
                Console.WriteLine("--- Jednotlivé vlastnosti ---");
                Console.WriteLine($"Cílový soubor: {shortcut.TargetPath}");
                Console.WriteLine($"Argumenty: {shortcut.Arguments}");
                Console.WriteLine($"Pracovní adresář: {shortcut.WorkingDirectory}");
                Console.WriteLine($"Popis: {shortcut.Description}");
                Console.WriteLine($"Ikona: {shortcut.IconLocation} (index: {shortcut.IconIndex})");
                Console.WriteLine($"Horká klávesa: {shortcut.HotKey}");
                Console.WriteLine($"Styl okna: {shortcut.WindowStyle}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }

            Console.WriteLine("\n\n=== VYTVOŘENÍ NOVÉHO ZÁSTUPCE ===\n");

            try
            {
                // Příklad 2: Vytvoření nového zástupce
                var newShortcut = new ShortcutInfo
                {
                    TargetPath = @"C:\Windows\System32\notepad.exe",
                    Arguments = "myfile.txt",
                    WorkingDirectory = @"C:\Users\YourUsername\Documents",
                    Description = "Otevřít Poznámkový blok",
                    IconLocation = @"C:\Windows\System32\shell32.dll",
                    IconIndex = 0,
                    WindowStyle = WindowStyle.Normal,
                    HotKey = "Ctrl+Alt+N"
                };

                string newShortcutPath = @"C:\Users\YourUsername\Desktop\MyNotepad.lnk";
                Console.WriteLine($"Vytvářím zástupce: {newShortcutPath}");
                ShortcutLoader.SaveShortcut(newShortcutPath, newShortcut);
                Console.WriteLine("Zástupce byl úspěšně vytvořen!");

                // Ověření - znovu načteme zástupce
                Console.WriteLine("\nOvěření - nově vytvořený zástupce:");
                ShortcutInfo loadedShortcut = ShortcutLoader.LoadShortcut(newShortcutPath);
                Console.WriteLine(loadedShortcut);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }

            Console.WriteLine("\n\n=== MODIFIKACE EXISTUJÍCÍHO ZÁSTUPCE ===\n");

            try
            {
                // Příklad 3: Načtení, modifikace a uložení zástupce
                string shortcutPath = @"C:\Users\YourUsername\Desktop\YourShortcut.lnk";
                
                Console.WriteLine($"Načítám zástupce: {shortcutPath}");
                ShortcutInfo shortcut = ShortcutLoader.LoadShortcut(shortcutPath);

                // Modifikace
                Console.WriteLine("Modifikuji zástupce...");
                shortcut.Arguments = "--modified";
                shortcut.Description = "Upravený zástupce";

                // Uložení změn
                ShortcutLoader.SaveShortcut(shortcutPath, shortcut);
                Console.WriteLine("Zástupce byl úspěšně aktualizován!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }

            Console.WriteLine("\n\nStiskněte libovolnou klávesu pro ukončení...");
            Console.ReadKey();
        }
    }
}
