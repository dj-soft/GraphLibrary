using System;

namespace ShortcutParser
{
    /// <summary>
    /// Třída reprezentující všechny údaje z Windows zástupce (.lnk souboru)
    /// </summary>
    public class ShortcutInfo
    {
        /// <summary>
        /// Cesta k cílovému souboru/aplikaci
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Argumenty předávané cílové aplikaci
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Pracovní adresář aplikace
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Popis zástupce (zobrazen v tooltip)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Cesta k souboru s ikonou
        /// </summary>
        public string IconLocation { get; set; }

        /// <summary>
        /// Index ikony v souboru se ikonami
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// Typ okna (Normal, Minimized, Maximized)
        /// </summary>
        public WindowStyle WindowStyle { get; set; }

        /// <summary>
        /// Horká klávesa pro spuštění zástupce
        /// </summary>
        public string HotKey { get; set; }

        /// <summary>
        /// Relativní cesta k cíli (pokud existuje)
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Cesta ke zástupci (kam je soubor uložen)
        /// </summary>
        public string LinkPath { get; set; }

        /// <summary>
        /// Jméno/název zástupce
        /// </summary>
        public string LinkName { get; set; }

        /// <summary>
        /// Příznaky speciálních nastavení
        /// </summary>
        public ShortcutFlags Flags { get; set; }

        public override string ToString()
        {
            return $@"
Zástupce: {LinkName}
Cesta ke zástupci: {LinkPath}
Cílový soubor: {TargetPath}
Argumenty: {Arguments}
Pracovní adresář: {WorkingDirectory}
Popis: {Description}
Ikona: {IconLocation}
Index ikony: {IconIndex}
Styl okna: {WindowStyle}
Horká klávesa: {HotKey}
Relativní cesta: {RelativePath}
";
        }
    }

    /// <summary>
    /// Výčet stylů okna
    /// </summary>
    public enum WindowStyle
    {
        /// <summary>Normální okno</summary>
        Normal = 1,
        /// <summary>Minimalizované okno</summary>
        Minimized = 7,
        /// <summary>Maximalizované okno</summary>
        Maximized = 3
    }

    /// <summary>
    /// Příznaky speciálních vlastností zástupce
    /// </summary>
    [Flags]
    public enum ShortcutFlags
    {
        None = 0,
        /// <summary>Spustit se zvýšenými právy (admin)</summary>
        RunAsAdministrator = 1,
        /// <summary>Ikonifikovat</summary>
        Iconified = 2
    }
}
