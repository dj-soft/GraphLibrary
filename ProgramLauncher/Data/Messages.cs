using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class MessagesTexts
    {
        #region Tvorba, načtení, příprava lokalizace
        /// <summary>
        /// Vytvoří a vrátí kompletní funkční sadu textových sad
        /// </summary>
        /// <returns></returns>
        public static MessagesTexts CreateDefault()
        {
            return new MessagesTexts();
        }
        /// <summary>
        /// Privátní konstruktor
        /// </summary>
        private MessagesTexts()
        {
            __Messages = new Dictionary<string, MessageSet>();
            _FillMessages();
        }
        private void _FillMessages()
        {
            __Messages.Clear();
            _AddDefaultSet();
            _AddExternalSets();
        }
        private void _AddDefaultSet()
        {
            var properties = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        }
        private void _AddExternalSets()
        {
            var files = MessageSet.LoadMessageFiles();
        }
        private string _GetDefaultText(string propertyName)
        {
            var property = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.Name == propertyName).FirstOrDefault();
            if (property is null) return "";
            return _GetDefaultText(property);
        }
        private string _GetDefaultText(System.Reflection.PropertyInfo property)
        {
            var dtAttribute = property.GetCustomAttributes(true).OfType<DefaultTextAttribute>().FirstOrDefault();
            if (dtAttribute != null && dtAttribute is DefaultTextAttribute defaultTextAttribute) return defaultTextAttribute.Text;
            return null;
        }
        private string _GetText([CallerMemberName] string propertyName = null)
        {
            if (String.IsNullOrEmpty(propertyName)) return "";
            if (__Messages.TryGetValue(propertyName, out var text)) return text;
            return _GetDefaultText(propertyName);
        }
        private Dictionary<string, MessageSet> __Messages;
        #endregion

        [DefaultText("Ukončit aplikaci")]
        public string ApplicationExitText { get { return _GetText(); } }
        [DefaultText("Ukončí tuto aplikaci.××Po jejím příštím otevření nemusí správně fungovat spouštění takových aplikací,××které jsou označené 'Otevřít jen jedno okno'.")]
        public string ApplicationExitToolTip { get { return _GetText(); } }
    }
    public class MessageSet
    {
        public static Dictionary<string, MessageSet> LoadMessageFiles()
        {
            var files = System.IO.Directory.GetFiles(App.ApplicationPath, "Messages.*");

            foreach (var file in files)
            { }

            return null;
        }
        public static MessageSet CreateMessageFile(string language, string name, string iconName, string parentLanguage, IEnumerable<Tuple<string, string>> items)
        {
            MessageSet messagesFile = new MessageSet();
            messagesFile.__Language = language;
            messagesFile.__Name = name;
            messagesFile.__IconName = iconName;
            messagesFile.__ParentLanguage = parentLanguage;
            messagesFile.__Codes = items.CreateDictionary(t => t.Item1, t => t.Item2, true);
            return messagesFile;
        }
        private MessageSet()
        {
            
        }
        private string __Language;
        private string __Name;
        private string __IconName;
        private string __ParentLanguage;
        private Dictionary<string, string> __Codes;


        public string Language { get { return __Language; } }
        public string Name { get { return __Name; } }
        public string IconName { get { return __IconName; } }
        public string ParentLanguage { get { return __ParentLanguage; } }
        public bool TryGetText(string code, out string text)
        {
            if (!String.IsNullOrEmpty(code) && __Codes.TryGetValue(code, out text)) return true;
            text = null;
            return false;
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
    public class DefaultTextAttribute : Attribute
    {
        public DefaultTextAttribute(string text) 
        {
            Text = text;
        }
        public string Text { get; private set; }

    }
}
