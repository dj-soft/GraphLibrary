using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Překladový systém
    /// </summary>
    public class LanguageSet
    {
        #region Tvorba, načtení, příprava lokalizace
        /// <summary>
        /// Vytvoří a vrátí kompletní funkční sadu textových sad
        /// </summary>
        /// <returns></returns>
        public static LanguageSet CreateDefault()
        {
            return new LanguageSet();
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        private LanguageSet()
        {
            __Languages = Language.LoadMessageFiles();
            __Default = __Languages[DefaultCode];
        }
        /// <summary>
        /// Kolekce všech přítomných jazyků
        /// </summary>
        public static Language[] Collection { get { return App.Messages.Languages; } }
        /// <summary>
        /// Vrátí prvek daného jména
        /// </summary>
        /// <param name="languageCode"></param>
        /// <returns></returns>
        public static Language GetItem(string languageCode, bool useDefault = false)
        {
            return App.Messages.GetLanguage(languageCode, useDefault);
        }
        /// <summary>
        /// Vrátí jazyk pro daný kód
        /// </summary>
        /// <param name="languageCode"></param>
        /// <param name="useDefault"></param>
        /// <returns></returns>
        private Language GetLanguage(string languageCode, bool useDefault = false)
        {
            if (!String.IsNullOrEmpty(languageCode) && __Languages.TryGetValue(languageCode, out var language)) return language;
            if (String.IsNullOrEmpty(languageCode) || useDefault) return Default;
            return null;
        }
        /// <summary>
        /// Pole všech přítomných jazyků, kromě Default = ty načtené ze souborů
        /// </summary>
        private Language[] Languages { get { return __Languages.Values.Where(l => !l.IsDefault).ToArray(); } }
        /// <summary>
        /// Výchozí jazyk = defaultní. 
        /// Aplikace může jazyk změnit. Viz <see cref="App.CurrentLanguage"/>.
        /// </summary>
        public Language Default { get { return __Default; } } private Language __Default;
        /// <summary>
        /// Kód default jazyka
        /// </summary>
        public const string DefaultCode = "";
        /// <summary>
        /// Vrátí překlad daného textu do aktuálního jazyka, 
        /// </summary>
        /// <param name="defaultText"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private string _GetText(string defaultText, [CallerMemberName] string code = null)
        {
            var defaultLanguage = Default;
            string text;
            if (!String.IsNullOrEmpty(code))
            {
                var language = App.CurrentLanguage ?? defaultLanguage;
                if (language != null && !language.IsDefault)                   // Prohledáváme jen NonDefaultní jazyky!
                {
                    Hashtable parents = null;
                    while (true)
                    {   // Budeme prohlížet Parent jazyky...
                        if (language.IsDefault) break;                         // Default jazyk neobsahuje žádné konkrétní texty.
                        if (language.TryGetText(code, out text)) return getLines(text);  // Pokud aktuální jazyk má překlad pro daný kód, vrátíme jej.
                        string parentLanguage = language.ParentLanguage;
                        if (String.IsNullOrEmpty(parentLanguage)) break;       // Pokud daný jazyk nemá parenta, kde by mohly být obecnější překlady, pak Parentem je Default text.
                        if (parents is null) parents = new Hashtable();        // Hashtable vytvořím až při její první potřebě...
                        parents.Add(language.Code, null);                      // Soupis jazyků (v řadě Language => Parent), kam jsme se už dívali - a kde hledaný kód nebyl přítomen
                        if (parents.ContainsKey(parentLanguage)) break;        // Pokud jsme už Parent jazyk prohledali dříve, skončíme... (obrana před zacyklením parentů)
                        if (!__Languages.TryGetValue(parentLanguage, out language)) break;      // Najdeme Parent jazyk?
                        // Našli jsme Parent jazyk, jdeme znovu dokola a podíváme se po hledaném kódu v něm.
                        // Pokud nenajdeme, podíváme se na jeho parenta.
                    }
                }
            }
            // Defaultní text je daný aplikací:
            return getLines(defaultText);

            string getLines(string txt)
            {
                return (txt != null && txt.Contains("××") ? txt.Replace("××", "\r\n") : txt);
            }
        }
        private Dictionary<string, Language> __Languages;
        #endregion
        #region Konkrétní texty

        public string ToolStripButtonAppearanceToolTip { get { return _GetText("Změnit vzhled (barevná paleta, velikost, jazyk)"); } }
        public string ToolStripButtonEditToolTip { get { return _GetText("Upravit obsah"); } }
        public string StatusStripPageCountText { get { return _GetText("bez stránek×stránka×stránky×stránek"); } }
        public string StatusStripApplicationText { get { return _GetText("bez aplikací×aplikace×aplikace×aplikací"); } }
        public string AppearanceMenuHeaderColorPalette { get { return _GetText("BAREVNÁ PALETA"); } }
        public string AppearanceMenuHeaderLayoutStyle { get { return _GetText("VELIKOST"); } }
        public string AppearanceMenuHeaderLanguage { get { return _GetText("JAZYK"); } }
        public string TrayIconText { get { return _GetText("Program Launcher"); } }
        public string TrayIconBalloonToolTip { get { return _GetText("Ukončení aplikace"); } }
        public string TrayIconBalloonText { get { return _GetText("Aplikace je jen schovaná.××Pro reálné vypnutí ji zavřete křížkem spolu s klávesou CTRL!××Anebo použijte kontextové menu na této ikoně."); } }
        public string TrayIconShowApplicationText { get { return _GetText("Aktivuj aplikaci"); } }
        public string TrayIconShowApplicationToolTip { get { return _GetText("Aktivuje okno aplikace. Aplikace není vypnutá, je jen skrytá."); } }
        public string TrayIconAcceptTrayInfoText { get { return _GetText("Pochopil jsem informaci"); } }
        public string TrayIconAcceptTrayInfoToolTip { get { return _GetText("Aplikace se běžným zavřením neukončuje, pouze se skryje.××Při každém skrytí aplikace se rozsvítí informace.××Pokud již tuto informaci nechci zobrazovat, zaškrtnu tuto volbu."); } }
        public string TrayIconApplicationExitText { get { return _GetText("Ukončit aplikaci"); } }
        public string TrayIconApplicationExitToolTip { get { return _GetText("Ukončí tuto aplikaci.××Po jejím příštím otevření nemusí správně fungovat spouštění takových aplikací,××které jsou označené 'Otevřít jen jedno okno'."); } }
        #endregion
    }

    #region class MessageSet : Sada překladů jednoho jazyka z jednoho souboru.
    /// <summary>
    /// Sada překladů jednoho jazyka z jednoho souboru.
    /// </summary>
    public class Language : IMenuItem
    {
        #region Načítání dat ze souborů, tvorba defaultního
        /// <summary>
        /// Načte a vrátí sadu překladových objektů pro nalezené jazyky.
        /// První z nich s klíčem "" je Default.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Language> LoadMessageFiles()
        {
            var messageSets = new Dictionary<string, Language>();

            messageSets.Add(LanguageSet.DefaultCode, Data.Language.CreateDefault());

            var fileNames = System.IO.Directory.GetFiles(App.ApplicationPath, "Language.*");
            foreach (var fileName in fileNames)
            {
                Language messageSet = Data.Language.LoadFromFile(fileName);
                if (messageSet != null && !messageSets.ContainsKey(messageSet.Code))
                    messageSets.Add(messageSet.Code, messageSet);
            }

            return messageSets;
        }
        /// <summary>
        /// Vytvoří a vrátí defaultní set = neobsahuje žádné kódy, výstupem pak bude defaultní překlad daný kódem.
        /// </summary>
        /// <returns></returns>
        private static Language CreateDefault()
        {
            Language messageSet = new Language("");
            messageSet.__IsDefault = true;
            messageSet.__Code = LanguageSet.DefaultCode;
            messageSet.__Name = "Default";
            messageSet.__IconName = "";
            messageSet.__ParentLanguage = null;
            return messageSet;
        }
        /// <summary>
        /// Načte data z daného souboru. Vrátí instanci <see cref="Data.Language"/> pokud bude správně načtena a platná <see cref="IsValid"/>. Vrátí null, pokud ne.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static Language LoadFromFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName)) return null;
            if (!System.IO.File.Exists(fileName)) return null;
            try
            {
                Language messageSet = new Language(fileName);
                messageSet.__IsDefault = false;
                var lines = System.IO.File.ReadAllLines(fileName);
                foreach (var line in lines)
                    messageSet._ProcessLine(line);
                if (!messageSet.IsValid) return null;
                // Nějaká finalizace?
                return messageSet;
            }
            catch (Exception exc)
            {
                App.ShowMessage($"Nelze načíst obsah souboru {fileName}.\r\nDošlo k chybě: {exc.Message}");
            }
            return null;
        }
        /// <summary>
        /// Konstruktor pro daný soubor
        /// </summary>
        /// <param name="fileName"></param>
        private Language(string fileName)
        {
            __FileName = fileName;
            __Codes = new Dictionary<string, string>();
        }
        /// <summary>
        /// Zpracuje daný řádek textu ze vstupního souboru
        /// </summary>
        /// <param name="line"></param>
        private void _ProcessLine(string line)
        {
            if (String.IsNullOrEmpty(line)) return;
            line = line.TrimStart();
            if (line.StartsWith("//")) return;

            int index = line.IndexOf(' ');
            if (index < 0) return;

            string code = line.Substring(0, index);
            string value = line.Substring(++index).TrimStart();
            switch (code)
            {
                case "#Language":
                    __Code = value; 
                    break;
                case "#Name":
                    __Name = value; 
                    break;
                case "#IconName":
                    __IconName = value; 
                    break;
                case "#ParentLanguage":
                    __ParentLanguage = value; 
                    break;
                default:
                    __Codes.StoreValue(code, value);
                    break;
            }

            /*      Message file má jméno:  "Messages.cz" / "Messages.en"
                    Message file je umístěn přímo vedle aplikace, tedy v adresáři App.ApplicationPath
               Struktura souboru:

            //                                         ... jsou komentáře a ignorují se. I prázdné řádky se ignorují.
            #Language CZ                               ... Povinný. Kód tohoto jazyka, reference. Pokud není uveden, soubor nebude zpracován.
            #Name Čeština                              ... Povinný. Název tohoto jazyka v jazyce samotném
            #IconName FlagCzech.png                    ... Povinný. Jméno souboru s ikonou vlajky, PNG, 22x22 px
            #ParentLanguage CZ                         ... Nepovinný. Označuje jazyk, ze kterého se čerpají zde nepřítomné hlášky (tedy zdejší soubor obsahuje jen několik odlišností oproti Parent)
            ApplicationExitText Close application      ... překlad klíčového kódu do zdejšího jazyka
            MessageTip Konec××Hláška××s odřádkováním   ... znaky ×× reprezentují CrLf. Hláška musí být uvedena na jednom řádku i kdyby byl dlouhý 4000 znaků.

            Pokud se bude klíčový kód vyskytovat v jednom souboru opakovaně, pak platí poslední výskyt.

            */

        }
        private bool __IsDefault;
        private string __FileName;
        private string __Code;
        private string __Name;
        private string __IconName;
        private string __ParentLanguage;
        private Dictionary<string, string> __Codes;
        /// <summary>
        /// Vrátí Image pro zdejší <see cref="IconName"/>
        /// </summary>
        /// <returns></returns>
        private Image _GetImage()
        {
            string imageName = IconName;
            if (String.IsNullOrEmpty(imageName)) return null;
            string filePath = System.IO.Path.GetDirectoryName(FileName);
            string imageFile = System.IO.Path.Combine(filePath, imageName);
            return App.GetImage(imageFile);
        }
        #endregion
        #region Public data a vyhledání textu překladu z tohoto jazyka
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Code}: {Name}";
        }
        /// <summary>
        /// Jde o defaultní jazyk, překlady jsou dány aplikačním kódem
        /// </summary>
        public bool IsDefault { get { return __IsDefault; } }
        /// <summary>
        /// Jméno souboru z nějž je čerpáno
        /// </summary>
        public string FileName { get { return __FileName; } }
        /// <summary>
        /// Kód jazyka, má být unikátní
        /// </summary>
        public string Code { get { return __Code; } }
        /// <summary>
        /// Jméno jazyka v jazyce samém, např. "Čeština", "English", "Deutsch" atd
        /// </summary>
        public string Name { get { return __Name; } }
        /// <summary>
        /// Jméno souboru s ikonou, výchozí adresář je vedle souboru s jazykem <see cref="FileName"/>
        /// </summary>
        public string IconName { get { return __IconName; } }
        /// <summary>
        /// Parent jazyk, kde se hledají texty v tomto jazyce neuvedené
        /// </summary>
        public string ParentLanguage { get { return __ParentLanguage; } }
        /// <summary>
        /// Data v souoru jsou platná
        /// </summary>
        public bool IsValid { get { return (IsDefault || (!String.IsNullOrEmpty(Code) && __Codes.Count > 0)); } }
        /// <summary>
        /// Zkusí najít daný kód v this překladech
        /// </summary>
        /// <param name="code"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool TryGetText(string code, out string text)
        {
            if (!String.IsNullOrEmpty(code) && __Codes.TryGetValue(code, out text)) return true;
            text = null;
            return false;
        }
        #endregion
        #region IMenuItem
        string IMenuItem.Text { get { return Name; } }
        string IMenuItem.ToolTip { get { return null; } }
        MenuItemType IMenuItem.ItemType { get { return MenuItemType.Button; } }
        Image IMenuItem.Image { get { return _GetImage(); } }
        bool IMenuItem.Enabled { get { return true; } }
        FontStyle? IMenuItem.FontStyle { get { return (Object.ReferenceEquals(this, App.CurrentLanguage) ? (FontStyle?)FontStyle.Bold : (FontStyle?)null); } }
        object IMenuItem.ToolItem { get; set; }
        object IMenuItem.UserData { get; set; }
        #endregion
    }
    #endregion
}
