using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// Překladový systém
    /// </summary>
    public partial class LanguageSet
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

            var languages = __Languages.Values.Where(l => !l.IsDefault).ToList();
            languages.Sort(Language.CompareByOrder);
            __Collection = languages.ToArray();

            __Default = __Languages[DefaultCode];
        }
        /// <summary>
        /// Kolekce všech přítomných jazyků, správně setříděná pro použití v nabídce jazyků
        /// </summary>
        public static Language[] Collection { get { return App.Messages.__Collection; } }
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
        /// Výchozí jazyk = defaultní. 
        /// Aplikace může jazyk změnit. Viz <see cref="App.CurrentLanguage"/>.
        /// </summary>
        public Language Default { get { return __Default; } } private Language __Default;
        /// <summary>
        /// Kód default jazyka
        /// </summary>
        public const string DefaultCode = "";
        /// <summary>
        /// Vrátí překlad daného textu do aktuálního jazyka.
        /// První parametr obsahuje defaultní text hlášky.
        /// Druhý (neviditelný) parametr přináší jméno property, která má konkrétní text získat: toto jméno property slouží jako klíč pro text do překladového souboru.
        /// </summary>
        /// <param name="defaultText">Defaultní text hlášky. Bude vrácen, pokud není zvolen exaktní jazyk, anebo v tomto aktuálním jazyku není k dispozici překlad.</param>
        /// <param name="code">Název property = název klíče do překladového souboru</param>
        /// <returns></returns>
        private string _GetText(string defaultText, [CallerMemberName] string code = null)
        {
            if (this.__ForceDefaultValues) return defaultText;

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
        /// <summary>
        /// Specifický požadavek na vrácení defaultního textu v metodě <see cref="_GetText(string, string)"/>.
        /// Na true se nastavuje pouze v 
        /// </summary>
        private bool __ForceDefaultValues;
        /// <summary>
        /// Dictionary s jazyky. Klíč = kód jazyka. Obsahuje i Default prvek.
        /// </summary>
        private Dictionary<string, Language> __Languages;
        /// <summary>
        /// Setříděné pole jazyků, kromě Default = tedy jazyky načtené z konkrétních souborů
        /// </summary>
        private Language[] __Collection;
        #endregion
        #region Synchronizace hlášek
        /// <summary>
        /// Obsahuje true, pokud jsou všechny systémové hlášky přítomny ve všech Translate souborech.
        /// Neřeší se tedy, zda jsou hlášky přeloženy, ale že v souborech je odpovídající položka (řádek) zadána.
        /// </summary>
        public bool IsTranslateComplete
        {
            get
            {
                if (!__IsTranslateComplete.HasValue) __IsTranslateComplete = _GetTranslateComplete();
                return __IsTranslateComplete.Value;
            }
        }
        private bool? __IsTranslateComplete;
        /// <summary>
        /// Vrátí true, pokud jsou všechny systémové hlášky přítomny ve všech Translate souborech.
        /// Neřeší se tedy, zda jsou hlášky přeloženy, ale že v souborech je odpovídající položka (řádek) zadána.
        /// </summary>
        /// <returns></returns>
        private bool _GetTranslateComplete()
        {
            var standardCodes = _GetStandardCodes();
            foreach (var languageFile in __Collection)
            {
                if (!languageFile.IsTranslateComplete(standardCodes)) return false;
            }
            return true;
        }
        /// <summary>
        /// Metoda najde všechny aktuální kódy hlášek, jejich standardní texty a aktualizuje je do všech přítomných jazykových souborů.
        /// </summary>
        public void SynchronizeLanguageFiles()
        {
            var standardCodes = _GetStandardCodes();
            var modifiedFiles = "";
            foreach (var languageFile in __Collection)
            {
                bool isModified = languageFile.SynchronizeLanguageFile(standardCodes);
                if (isModified)
                    modifiedFiles += ", " + System.IO.Path.GetFileName(languageFile.FileName);
            }
            if (modifiedFiles.Length > 0)
            {
                App.LanguagesReload();
                App.ShowMessage(App.Messages.ModifiedFiles + Environment.NewLine + modifiedFiles.Substring(2), MessageBoxIcon.Asterisk, App.Messages.ResultInformationTitle);
            }
            else
                App.ShowMessage(App.Messages.NoModifiedFiles, MessageBoxIcon.Information, App.Messages.ResultInformationTitle);
        }
        /// <summary>
        /// Metoda vrátí kolekci všech programových kódu a jejich defaultních textů
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> _GetStandardCodes()
        {
            var defaultCodes = new Dictionary<string, string>();

            var properties = this.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(string) && p.SetMethod is null)
                .ToArray();
            this.__ForceDefaultValues = true;
            foreach (var property in properties)
            {
                var value = property.GetValue(this, null);
                defaultCodes.Add(property.Name, value as string);
            }
            this.__ForceDefaultValues = false;

            return defaultCodes;
        }
        #endregion
        #region Formátování parametrů
        public string Format(string message, params object[] parameters)
        {
            if (String.IsNullOrEmpty(message)) return "";
            int length = parameters?.Length ?? 0;
            if (length == 0) return message;

            string result = message;
            for (int i = 0; i < length; i++)
                replaceOne(i, parameters[i]);

            return result;

            void replaceOne(int number, object value)
            {
                string pattern1 = "%" + number;
                if (result.Contains(pattern1))
                    result = result.Replace(pattern1, ToUser(value));

                string pattern2 = "{" + number + "}";
                if (result.Contains(pattern2))
                    result = result.Replace(pattern2, ToUser(value));
            }
        }
        public static string ToUser(object value)
        {
            if (value is null) return "";
            if (value is string s) return s;

            return value.ToString();
        }
        #endregion
        #region Konkrétní texty - jednotlivé property a jejich defaultní text (umožňuje běh bez jazykových souborů)
        // Neměnit jméno property - vede to k nutnosti změnit toto jméno v překladových souborech!
        public string ErrorTitle { get { return _GetText("Došlo k chybě"); } }
        public string ResultInformationTitle { get { return _GetText("Výsledná informace"); } }
        public string ModifiedFiles { get { return _GetText("Byly modifikovány tyto soubory:"); } }
        public string NoModifiedFiles { get { return _GetText("Nebyly modifikovány žádné soubory."); } }
        public string ToolStripButtonAppearanceToolTip { get { return _GetText("Změnit vzhled (barevná paleta, velikost, jazyk)"); } }
        public string ToolStripButtonUndoToolTip { get { return _GetText("Vrátí zpět posledně provedenou změnu"); } }
        public string ToolStripButtonRedoToolTip { get { return _GetText("Obnoví vpřed posledně vrácenou změnu"); } }
        public string ToolStripButtonPreferenceToolTip { get { return _GetText("Nastavit chování aplikace"); } }
        public string ToolStripButtonEditToolTip { get { return _GetText("Upravit obsah"); } }
        public string ToolStripButtonMessageSyncToolTip { get { return _GetText("Naplnit jazykové soubory aktuálním seznamem hlášek"); } }
        public string StatusStripPageCountText { get { return _GetText("bez stránek×stránka×stránky×stránek"); } }
        public string StatusStripApplicationText { get { return _GetText("bez aplikací×aplikace×aplikace×aplikací"); } }

        public string AppearanceMenuHeaderColorPalette { get { return _GetText("BAREVNÁ PALETA"); } }
        public string AppearanceMenuHeaderLayoutStyle { get { return _GetText("VELIKOST"); } }
        public string AppearanceMenuHeaderLanguage { get { return _GetText("JAZYK"); } }

        public string AppContextMenuTitlePages { get { return _GetText("Stránky"); } }
        public string AppContextMenuTitlePage { get { return _GetText("Stránka '%0'"); } }
        public string AppContextMenuNewPageText { get { return _GetText("Nová stránka"); } }
        public string AppContextMenuNewPageToolTip { get { return _GetText("Přidá novou stránku: otevře okno a umožní změnit popis, barvu a chování"); } }
        public string AppContextMenuTitleApplications { get { return _GetText("Stránka aplikací"); } }
        public string AppContextMenuTitleGroup { get { return _GetText("Skupina '%0'"); } }
        public string AppContextMenuTitleApplication { get { return _GetText("Aplikace '%0'"); } }
        public string AppContextMenuRunText { get { return _GetText("Spustit"); } }
        public string AppContextMenuRunToolTip { get { return _GetText("Spustí tuto aplikaci"); } }
        public string AppContextMenuRunAsText { get { return _GetText("Spustit jako správce"); } }
        public string AppContextMenuRunAsToolTip { get { return _GetText("Spustí tuto aplikaci s oprávněním Správce"); } }
        public string AppContextMenuRemoveText { get { return _GetText("Odstranit"); } }
        public string AppContextMenuRemoveApplicationToolTip { get { return _GetText("Odstranit tuto aplikaci z nabídky"); } }
        public string AppContextMenuEditText { get { return _GetText("Upravit"); } }
        public string AppContextMenuCopyText { get { return _GetText("Zkopírovat"); } }
        public string AppContextMenuEditApplicationToolTip { get { return _GetText("Otevře okno a umožní změnit popis, cílovou aplikaci a chování této položky"); } }
        public string AppContextMenuNewApplicationText { get { return _GetText("Nová aplikace"); } }
        public string AppContextMenuCopyPageToolTip { get { return _GetText("Zkopírovat tuto stránku včetně celého obsahu"); } }
        public string AppContextMenuCopyGroupToolTip { get { return _GetText("Zkopírovat tuto skupinu aplikací včetně celého obsahu"); } }
        public string AppContextMenuCopyApplicationToolTip { get { return _GetText("Zkopírovat tuto aplikaci do nové ikony"); } }
        public string AppContextMenuNewApplicationToolTip { get { return _GetText("Přidá novou aplikaci: otevře okno a umožní změnit popis, cílovou aplikaci a chování této položky"); } }
        public string AppContextMenuNewGroupText { get { return _GetText("Nová skupina"); } }
        public string AppContextMenuNewGroupToolTip { get { return _GetText("Přidá novou skupinu aplikací, do ní pak bude možno přidávat aplikace"); } }

        public string EditFormTitleNewPage { get { return _GetText("Nová stránka"); } }
        public string EditFormTitleNewGroup { get { return _GetText("Nová skupina aplikací"); } }
        public string EditDataNewDefaultGroupTitle { get { return _GetText("Nová skupina"); } }
        public string EditFormTitleNewApplication { get { return _GetText("Nová aplikace"); } }
        public string EditFormTitleClone { get { return _GetText("Kopie: %0"); } }

        public string EditDataTitleText { get { return _GetText("Titulek"); } }
        public string EditDataDescriptionText { get { return _GetText("Popisek"); } }
        public string EditDataToolTipText { get { return _GetText("Nápověda"); } }
        public string EditDataImageFileNameText { get { return _GetText("Obrázek"); } }
        public string EditDataExecutableFileNameText { get { return _GetText("Aplikace"); } }
        public string EditDataExecutableArgumentsText { get { return _GetText("Argumenty"); } }
        public string EditDataExecuteInAdminModeText { get { return _GetText("Oprávnění Admin"); } }
        public string EditDataOnlyOneInstanceText { get { return _GetText("Jen jeden proces"); } }
        public string EditDataOpenMaximizedText { get { return _GetText("Maximalizované okno"); } }
        public string ExecutableFileIsNotSpecified { get { return _GetText("Aplikaci nelze spustit, není zadán její soubor."); } }
        public string ExecutableFileIsNotExists { get { return _GetText("Aplikaci nelze spustit, její soubor neexistuje."); } }

        public string DialogButtonOkText { get { return _GetText("OK"); } }
        public string DialogButtonCancelText { get { return _GetText("Storno"); } }
        public string DialogButtonYesText { get { return _GetText("Ano"); } }
        public string DialogButtonNoText { get { return _GetText("Ne"); } }
        public string DialogButtonAbortText { get { return _GetText("Přerušit"); } }
        public string DialogButtonRetryText { get { return _GetText("Opakovat"); } }
        public string DialogButtonIgnoreText { get { return _GetText("Ignorovat"); } }
        public string DialogButtonHelpText { get { return _GetText("Nápověda"); } }
        public string DialogButtonNextText { get { return _GetText("Další"); } }
        public string DialogButtonPrevText { get { return _GetText("Zpět"); } }
        public string DialogButtonApplyText { get { return _GetText("Aplikovat"); } }
        public string DialogButtonSaveText { get { return _GetText("Uložit"); } }

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

    #region class Language : Sada překladů jednoho jazyka z jednoho souboru.
    /// <summary>
    /// Sada překladů jednoho jazyka z jednoho souboru.
    /// </summary>
    public class Language : IMenuItem
    {
        #region Načítání dat ze souborů
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
        /// Porovná dvě instance <see cref="Language"/> podle jejich <see cref="Order"/> (plus <see cref="Code"/>).
        /// Prvek, jehož <see cref="Order"/> není zadané, bude na konci.
        /// Prvky se shodným <see cref="Order"/> budouv pořadí jejich <see cref="Code"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByOrder(Language a, Language b)
        {
            string aOrder = $"{(a?.Order ?? "§§§§")}#{a.Code}";
            string bOrder = $"{(b?.Order ?? "§§§§")}#{b.Code}";
            return String.CompareOrdinal(aOrder, bOrder);
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
            var lineType = _ProcessLineParse(line, out string code, out string value);
            if (lineType == LineType.Empty || lineType == LineType.Comment) return;
            switch (lineType)
            {
                case LineType.KeyLanguage:
                    __Code = value; 
                    break;
                case LineType.KeyName:
                    __Name = value; 
                    break;
                case LineType.KeyIconName:
                    __IconName = value; 
                    break;
                case LineType.KeyOrder:
                    __Order = value;
                    break;
                case LineType.KeyParentLanguage:
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
            #Order 20                                  ... Nepovinný. Pořadí tohoto jazyka v nabídce jazyků.
            #ParentLanguage CZ                         ... Nepovinný. Označuje jazyk, ze kterého se čerpají zde nepřítomné hlášky (tedy zdejší soubor obsahuje jen několik odlišností oproti Parent)
            ApplicationExitText Close application      ... překlad klíčového kódu do zdejšího jazyka
            MessageTip Konec××Hláška××s odřádkováním   ... znaky ×× reprezentují CrLf. Hláška musí být uvedena na jednom řádku i kdyby byl dlouhý 4000 znaků.

            Pokud se bude klíčový kód vyskytovat v jednom souboru opakovaně, pak platí poslední výskyt.

            */

        }
        /// <summary>
        /// Detekuje obsah daného řádku, a pokud jde o kód obsahující mezeru, pak jej oddělí do out parametrů
        /// </summary>
        /// <param name="line"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static LineType _ProcessLineParse(string line, out string code, out string value)
        {
            code = null;
            value = null;
            if (String.IsNullOrEmpty(line)) return LineType.Empty;

            line = line.TrimStart();
            if (line.StartsWith("//")) return LineType.Comment;

            int index = line.IndexOf(' ');
            if (index < 0)
            {
                code = line;
                return LineType.Code;
            }

            code = line.Substring(0, index);
            value = line.Substring(++index).TrimStart();

            switch (code)
            {
                case "#Language": return LineType.KeyLanguage;
                case "#Name": return LineType.KeyName;
                case "#IconName": return LineType.KeyIconName;
                case "#Order": return LineType.KeyOrder;
                case "#ParentLanguage": return LineType.KeyParentLanguage;
            }
            return LineType.CodeValue;
        }
        /// <summary>
        /// Druh obsahu řádku
        /// </summary>
        private enum LineType 
        {
            Empty,
            Comment,
            KeyLanguage,
            KeyName,
            KeyIconName,
            KeyOrder,
            KeyParentLanguage,
            Code,
            CodeValue
        }
        private bool __IsDefault;
        private string __FileName;
        private string __Code;
        private string __Name;
        private string __IconName;
        private string __Order;
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
        #region Synchronizace hlášek
        /// <summary>
        /// Vrátí true, pokud jsou všechny systémové hlášky přítomny ve všech Translate souborech.
        /// Neřeší se tedy, zda jsou hlášky přeloženy, ale že v souborech je odpovídající položka (řádek) zadána.
        /// </summary>
        /// <returns></returns>
        public bool IsTranslateComplete(Dictionary<string, string> standardCodes)
        {
            // Nevadí mi, když this soubor má něco navíc. Proto neřeším nadbytečné kódy v __Codes.
            // Ale musím v tomto souboru mít všechny kódy, které jsou uvedeny v dodaném slovníku standardCodes jako klíče:
            foreach (var code in standardCodes.Keys)
            {   // Jakmile v this.__Codes chybí byť i jen jediný kód z vnějšího seznamu standardCodes, pak vrátím false = nemám všechny hlášky.
                if (!__Codes.ContainsKey(code)) return false;
            }
            return true;
        }
        /// <summary>
        /// Metoda aktualizuje seznam hlášek v tomto souboru podle defaultních kódů
        /// </summary>
        public bool SynchronizeLanguageFile(Dictionary<string, string> standardCodes)
        {
            string fileName = __FileName;
            _ReadHeaderLines(fileName, this.Code, standardCodes, out var headerLines, out var contentLines);
            var resultLines = _CreateLanguageLines(headerLines, standardCodes, contentLines, out bool hasNewCodes);
            if (hasNewCodes)
                System.IO.File.WriteAllLines(fileName, resultLines, Encoding.UTF8);
            return hasNewCodes;
        }
        /// <summary>
        /// Načte zadaný soubor do out polí, případně vytvoří defaultní záhlaví pokud ousbor neexistuje
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="languageCode"></param>
        /// <param name="standardCodes"></param>
        /// <param name="headerLines"></param>
        /// <param name="contentLines"></param>
        private void _ReadHeaderLines(string fileName, string languageCode, Dictionary<string, string> standardCodes, out List<string> headerLines, out Dictionary<string, string> contentLines)
        {
            headerLines = new List<string>();
            contentLines = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
            {   // Máme soubor? Načteme jeho obsah:
                var lines = System.IO.File.ReadAllLines(fileName);
                foreach (var line in lines)
                {
                    var lineType = _ProcessLineParse(line, out string code, out string value);
                    if (lineType == LineType.CodeValue)
                    {   // Pokud na vstupu je řádek vypadající jako "code value" a má neprázdný code, a nalezený code je platný, a dosud nemáme jeho řádek uložen, tak si jej uložíme:
                        if (!String.IsNullOrEmpty(code) && standardCodes.ContainsKey(code) && !contentLines.ContainsKey(code))
                            contentLines.Add(code, line);
                    }
                    else if (contentLines.Count == 0)
                    {   // Není to CodeValue, a zatím nemáme žádný CodeValue (=jsme v Headers řádcích), přidáme vstupní řádek jako Header:
                        headerLines.Add(line);
                    }
                    // Tímto postupem vypustíme nevhodné řádky v pozicích uvnitř = mezi CodeValues:
                }
            }
            else
            {   // Nemáme soubor? Vytvoříme patřičné headerLines:
                headerLines.Add($"// Překlady aplikace ProgramLauncher do jazyka {languageCode}");
                headerLines.Add($"");
                headerLines.Add($"#Language {languageCode}");
                headerLines.Add($"#Name Název jazyka");
                headerLines.Add($"#IconName Flag-{languageCode}-24.png");
                headerLines.Add($"#Order 99");
                headerLines.Add($"");
            }
        }
        /// <summary>
        /// Vytvoří výstupní pole řádků: nejprve dá všechny <paramref name="headerLines"/>, a poté projde standardní kódy z <paramref name="standardCodes"/>
        /// a pokud najde odpovídající řádek v <paramref name="contentLines"/>, pak jej dá do výstupu (ve standardním pořadí), anebo do výstupu dá standardní kód i text.
        /// </summary>
        /// <param name="headerLines"></param>
        /// <param name="standardCodes"></param>
        /// <param name="contentLines"></param>
        /// <returns></returns>
        private string[] _CreateLanguageLines(List<string> headerLines, Dictionary<string, string> standardCodes, Dictionary<string, string> contentLines, out bool hasNewCodes)
        {
            hasNewCodes = false;
            var resultLines = new List<string>();
            resultLines.AddRange(headerLines);
            foreach (var standardCode in standardCodes)
            {
                if (!contentLines.TryGetValue(standardCode.Key, out var contentLine))
                {
                    contentLine = standardCode.Key + " *" + standardCode.Value;
                    hasNewCodes = true;
                }
                resultLines.Add(contentLine);
            }
            return resultLines.ToArray();
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
        /// Pořadí souboru v nabídce = v kolekci <see cref="LanguageSet.Collection"/>. 
        /// Nejde o číslo ale o string.
        /// </summary>
        public string Order { get { return __Order; } }
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
