using DevExpress.Utils.DirectXPaint.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XmlSerial = DjSoft.App.iCollect.Data.XmlSerializer;

namespace DjSoft.App.iCollect.Collect
{
    /// <summary>
    /// Data o jedné sbírce = položky podobného druhu, evicované v jedné tabulce
    /// </summary>
    public class Collection
    {
        #region Generátor ukázkových sbírkových souborů jako demo

        internal static Collection DemoCollectionDoll
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka panenek";
                demo.Description = "Úplná sbírka panenek Barbie, včetně jejich zdroje, stavu a umístění";

                return demo;
            }
        }
        internal static Collection DemoCollectionBook
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka knih";
                demo.Description = "Úplná sbírka knih, včetně jejich umístění i zapůjčení";
                return demo;
            }
        }
        internal static Collection DemoCollectionMovie
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka filmů";
                demo.Description = "Úplná sbírka filmů na DVD, BlR, VHS, NAS atd, včetně jejich umístění i zapůjčení";

                return demo;
            }
        }
        internal static Collection DemoCollectionAudioCd
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka Audio CDček";
                demo.Description = "Úplná sbírka nahrávek všech druhů na CD, včetně jejich umístění i zapůjčení";

                return demo;
            }
        }
        #endregion
        #region Load a Save
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Collection()
        {
            this.Name = null;
            this.FileName = null;
            this.Definition = new Definition();
            this.Content = new Content();
        }
        /// <summary>
        /// Z zadaného souboru načte data, volitelně kompletní (false = pouze záhlaví = rychlé)
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fullLoad"></param>
        /// <returns></returns>
        public static Collection LoadFromFile(string fileName, bool fullLoad)
        {
            if (!TryLoadParseDataFile(fileName, out var headerText, out var definitionText, out var contentText)) return null;
            var headerArgs = new XmlSerial.PersistArgs() { DataContent = headerText };
            var collection = XmlSerial.Persist.Deserialize(headerArgs) as Collection;    // Tento blok dat neobsahuje data Definition ani Content, pro úsporu paměti i času
            if (collection != null)
            {
                collection.FileName = fileName;
                if (fullLoad)
                    collection.LoadFull(definitionText, contentText);
            }
            
            return collection;
        }
        /// <summary>
        /// Načte data pro <see cref="Definition"/> a <see cref="Content"/> ze svého souboru
        /// </summary>
        private void LoadFull()
        {
            TryLoadParseDataFile(this.FileName, out var headerText, out var definitionText, out var contentText);
            this.LoadFull(definitionText, contentText);
        }
        /// <summary>
        /// Načte data pro <see cref="Definition"/> a <see cref="Content"/> z dodaných dat
        /// </summary>
        /// <param name="definitionText"></param>
        /// <param name="contentText"></param>
        private void LoadFull(string definitionText, string contentText)
        {
            Definition definition = null;
            if (!String.IsNullOrEmpty(definitionText))
            {
                var definitionArgs = new XmlSerial.PersistArgs() { DataContent = definitionText };
                definition = XmlSerial.Persist.Deserialize(definitionArgs) as Definition;
            }
            if (definition is null) definition = new Definition();
            this.Definition = definition;

            Content content = null;
            if (!String.IsNullOrEmpty(contentText))
            {
                var contentArgs = new XmlSerial.PersistArgs() { DataContent = contentText };
                content = XmlSerial.Persist.Deserialize(contentArgs) as Content;
            }
            if (content is null) content = new Content();
            this.Content = content;
        }
        /// <summary>
        /// Načte obsah souboru <paramref name="fileName"/>, a rozčlení jej na <paramref name="headerText"/>, <paramref name="definitionText"/> a <paramref name="contentText"/>.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="headerText"></param>
        /// <param name="definitionText"></param>
        /// <param name="contentText"></param>
        /// <returns></returns>
        private static bool TryLoadParseDataFile(string fileName, out string headerText, out string definitionText, out string contentText)
        {
            try
            {
                if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
                {
                    string fileText = System.IO.File.ReadAllText(fileName);

                    contentText = search(ref fileText, PartDelimiterContent);
                    definitionText = search(ref fileText, PartDelimiterDefiniton);
                    headerText = search(ref fileText, PartDelimiterHeader);

                    return true;
                }
            }
            catch { }

            headerText = null;
            definitionText = null;
            contentText = null;
            return false;

            // V textu vyhledá daný delimiter, vrátí vše co je za ním a ve vstupním textu ponechá to před delimiterem. Odkrajuje tedy obsah od konce.
            string search(ref string text, string delimiter)
            {
                string result = "";
                int length = text.Length;
                int delimiterLength = delimiter.Length;
                if (length >= delimiterLength)
                {   // Může tam být delimiter:
                    int index = text.LastIndexOf(delimiter);
                    if (index >= 0)
                    {   // Je tam delimiter:
                        int begin = index + delimiterLength;
                        if (begin < length)
                        {   // Za koncem delimiteru ještě něco je:
                            result = text.Substring(begin).Trim(' ', '\t', '\r', '\n');
                        }
                        // Vstupní text následně bude obsahovat jen to, co je před delimiterem:
                        text = text.Substring(0, begin);
                    }
                }
                return result;
            }
        }
        /// <summary>
        /// Uloží svůj obsah do svého / daného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string Save(string fileName = null)
        {
            var contentSb = new StringBuilder();
            contentSb.AppendLine(PartUserHeader1);
            contentSb.AppendLine(PartUserHeader2);

            var headerArgs = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this, headerArgs);
            contentSb.AppendLine(PartDelimiterHeader);
            contentSb.AppendLine(headerArgs.DataContent);
            contentSb.AppendLine();

            var definitionArgs = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this.Definition, definitionArgs);
            contentSb.AppendLine(PartDelimiterDefiniton);
            contentSb.AppendLine(definitionArgs.DataContent);
            contentSb.AppendLine();

            var contentArgs = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this.Content, contentArgs);
            contentSb.AppendLine(PartDelimiterContent);
            contentSb.AppendLine(contentArgs.DataContent);
            contentSb.AppendLine();

            string content = contentSb.ToString();

            // Do explicitního souboru? Anebo do našeho výchozího?
            if (!String.IsNullOrEmpty(fileName))
                this.FileName = fileName;
            else
                fileName = this.FileName;
        
            if (!String.IsNullOrEmpty(fileName))
            {
                if (Application.MainApp.TryPrepareAppPathForFile(fileName, true))
                    System.IO.File.WriteAllText(fileName, content, Encoding.UTF8);
            }

            return content;
        }
        private const string PartUserHeader1        = "<!--  Toto je soubor, obsahující kompletní data o jedné sbírce.   -->";
        private const string PartUserHeader2        = "<!--  Prosím: needituj jej ručně, jinam můžeš přijít o data !!!   -->";

        private const string PartDelimiterHeader    = "<!--      Header part :        -->";
        private const string PartDelimiterDefiniton = "<!--      Definiton part :     -->";
        private const string PartDelimiterContent   = "<!--      Content part :       -->";
        #endregion
        #region Data o sbírce = hlavička, definice, položky
        /// <summary>
        /// Jméno sbírky
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Ikona sbírky, v nabídkách atd
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Popis sbírky, delší
        /// </summary>
        public string Description { get; set; }



        /// <summary>
        /// Definice jednotlivých popisných kolonek (atributy, sloupečky) pro jednotlivé záznamy sbírky
        /// </summary>
        [XmlSerial.PersistingEnabled(false)]
        public Definition Definition 
        {
            get
            {   // OnDemand Full load:
                if (__Definition is null)
                    this.LoadFull();
                return __Definition;
            }
            set
            {   // Definition.Owner : dosavadní uvolnit, nový vložit:
                if (__Definition != null)
                    __Definition.Owner = null;

                if (value != null)
                    value.Owner = this;
                __Definition = value;
            }
        }
        private Definition __Definition;
        /// <summary>
        /// Jednotlivé položky sbírky = kusy (jednotlivé knihy, autíčka, mince, panenky atd)
        /// </summary>
        [XmlSerial.PersistingEnabled(false)]
        public Content Content
        {
            get
            {   // OnDemand Full load:
                if (__Content is null)
                    this.LoadFull();
                return __Content;
            }
            set
            {   // Content.Owner : dosavadní uvolnit, nový vložit:
                if (__Content != null)
                    __Content.Owner = null;

                if (value != null)
                    value.Owner = this;
                __Content = value;
            }
        }
        private Content __Content;
        [XmlSerial.PersistingEnabled(false)]
        public string FileName { get; set; }
        #endregion
    }
}
