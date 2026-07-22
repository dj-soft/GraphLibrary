using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShortcutParser
{
    /// <summary>
    /// Příklady použití IconExtractor třídy
    /// </summary>
    class IconExtractionExamples
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== PŘÍKLADY EXTRAKCE IKON ===\n");

            // PŘÍKLAD 1: Základní extrakce ikony ze souboru
            Console.WriteLine("--- PŘÍKLAD 1: Extrakce ikony ze EXE souboru ---");
            BasicIconExtraction();

            // PŘÍKLAD 2: Extrakce ikony z konkrétního indexu
            Console.WriteLine("\n--- PŘÍKLAD 2: Extrakce ikony z DLL souboru s indexem ---");
            ExtractFromDLL();

            // PŘÍKLAD 3: Direktní extrakce z IconLocation ze zástupce
            Console.WriteLine("\n--- PŘÍKLAD 3: Extrakce z ShortcutInfo ---");
            ExtractFromShortcut();

            // PŘÍKLAD 4: Zjištění počtu ikon a export
            Console.WriteLine("\n--- PŘÍKLAD 4: Zjištění počtu ikon ---");
            CountAndExport();

            // PŘÍKLAD 5: Zobrazení ikony ve Windows formě
            Console.WriteLine("\n--- PŘÍKLAD 5: Zobrazení ikony v aplikaci ---");
            DisplayIconInForm();

            Console.WriteLine("\n\nStiskněte libovolnou klávesu pro ukončení...");
            Console.ReadKey();
        }

        /// <summary>
        /// PŘÍKLAD 1: Základní extrakce
        /// </summary>
        static void BasicIconExtraction()
        {
            try
            {
                string exePath = @"C:\Windows\System32\notepad.exe";

                // Extrahuj ikonu
                Icon icon = IconExtractor.GetIconFromFile(exePath, 0, true);

                if (icon != null)
                {
                    Console.WriteLine($"✓ Ikona úspěšně získána z: {exePath}");
                    Console.WriteLine($"  Velikost: {icon.Width}x{icon.Height}");

                    // Ulož ikonu na disk
                    string outputPath = @"C:\temp\notepad_icon.ico";
                    using (System.IO.FileStream fs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
                    {
                        icon.Save(fs);
                    }
                    Console.WriteLine($"  Uloženo do: {outputPath}");

                    icon.Dispose();
                }
                else
                {
                    Console.WriteLine("✗ Nepodařilo se získat ikonu");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }
        }

        /// <summary>
        /// PŘÍKLAD 2: Extrakce z DLL s konkrétním indexem
        /// </summary>
        static void ExtractFromDLL()
        {
            try
            {
                string dllPath = @"C:\Windows\System32\shell32.dll";

                // shell32.dll obsahuje spoustu ikon - zkus index 0 až 10
                for (int i = 0; i < 5; i++)
                {
                    Icon icon = IconExtractor.GetIconFromFile(dllPath, i, true);
                    if (icon != null)
                    {
                        Console.WriteLine($"✓ Ikona {i} získána - velikost: {icon.Width}x{icon.Height}");
                        icon.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }
        }

        /// <summary>
        /// PŘÍKLAD 3: Extrakce přímo ze ShortcutInfo
        /// </summary>
        static void ExtractFromShortcut()
        {
            try
            {
                // Nejdřív načti zástupce
                string shortcutPath = @"C:\Users\YourUsername\Desktop\Notepad.lnk";

                ShortcutInfo shortcut = ShortcutLoader.LoadShortcut(shortcutPath);
                Console.WriteLine($"Zástupce: {shortcut.LinkName}");
                Console.WriteLine($"IconLocation: {shortcut.IconLocation}");
                Console.WriteLine($"IconIndex: {shortcut.IconIndex}");

                // Extrahuj ikonu přímo
                object result = IconExtractor.GetIconFromShortcut(shortcut, returnBitmap: false);

                if (result is Icon icon)
                {
                    Console.WriteLine($"✓ Ikona úspěšně získána - velikost: {icon.Width}x{icon.Height}");
                    icon.Dispose();
                }
                else if (result is Bitmap bitmap)
                {
                    Console.WriteLine($"✓ Bitmap úspěšně získán - velikost: {bitmap.Width}x{bitmap.Height}");
                    bitmap.Dispose();
                }
                else
                {
                    Console.WriteLine("✗ Nepodařilo se získat ikonu");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }
        }

        /// <summary>
        /// PŘÍKLAD 4: Zjištění počtu ikon a export všech
        /// </summary>
        static void CountAndExport()
        {
            try
            {
                string filePath = @"C:\Windows\System32\shell32.dll";

                // Zjisti, kolik ikon soubor obsahuje
                int iconCount = IconExtractor.GetIconCount(filePath);
                Console.WriteLine($"Počet ikon v {System.IO.Path.GetFileName(filePath)}: {iconCount}");

                if (iconCount > 0)
                {
                    // Exportuj prvních 5 ikon
                    string outputDir = @"C:\temp\exported_icons";
                    int exported = IconExtractor.ExportAllIcons(filePath, outputDir, "shell32");

                    Console.WriteLine($"✓ Exportováno {exported} ikon do: {outputDir}");

                    // Vypiš soubory
                    if (System.IO.Directory.Exists(outputDir))
                    {
                        var files = System.IO.Directory.GetFiles(outputDir, "*.ico");
                        foreach (var file in files)
                        {
                            Console.WriteLine($"  - {System.IO.Path.GetFileName(file)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }
        }

        /// <summary>
        /// PŘÍKLAD 5: Zobrazení ikony v Windows formě
        /// </summary>
        static void DisplayIconInForm()
        {
            try
            {
                // Vytvoř jednoduchou formu
                Form form = new Form
                {
                    Text = "Náhled ikony",
                    Width = 300,
                    Height = 300,
                    StartPosition = FormStartPosition.CenterScreen
                };

                // Načti ikonu
                Icon icon = IconExtractor.GetIconFromFile(
                    @"C:\Windows\System32\notepad.exe", 
                    0, 
                    true
                );

                if (icon != null)
                {
                    // Nastav ikonu formy
                    form.Icon = icon;

                    // Přidej PictureBox pro zobrazení ikony
                    PictureBox pictureBox = new PictureBox
                    {
                        Image = IconExtractor.IconToBitmap(icon),
                        SizeMode = PictureBoxSizeMode.CenterImage,
                        Dock = DockStyle.Fill
                    };

                    form.Controls.Add(pictureBox);

                    Console.WriteLine("✓ Forma se zobrazí s ikonou...");
                    Application.Run(form);
                }
                else
                {
                    Console.WriteLine("✗ Nepodařilo se načíst ikonu");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CHYBA: {ex.Message}");
            }
        }
    }
}
