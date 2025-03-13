using DevExpress.Utils.DirectXPaint.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XmlSerial = DjSoft.App.iCollect.Data.XmlSerializer;

namespace DjSoft.App.iCollect.Collect
{
    public class Collection
    {
        public Collection()
        {
            this.Name = null;
            this.FileName = null;
            this.Definition = new Definition();
            this.Content = new Content();
        }

        #region Generátor ukázkových sbírkových souborů jako demo

        internal static Collection DemoCollectionBook
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka knih";

                return demo;
            }
        }
        internal static Collection DemoCollectionMovie
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka filmů";


                return demo;
            }
        }
        internal static Collection DemoCollectionAudioCd
        {
            get
            {
                var demo = new Collection();
                demo.Name = "Sbírka Audio CDček";


                return demo;
            }
        }
        #endregion
        #region Load a Save

        public static Collection LoadFromFile(string fileName, bool fullLoad)
        {
            if (!TryLoadParseDataFile(fileName, out var header, out var schema, out var items)) return null;
            var args = new XmlSerial.PersistArgs() { DataContent = header };
            var collection = XmlSerial.Persist.Deserialize(args) as Collection;
            return collection;
        }
        private static bool TryLoadParseDataFile(string fileName, out string header, out string schema, out string items)
        {
            try
            {
                if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
                {
                    string fileText = System.IO.File.ReadAllText(fileName);
                    string delimiter = PartsDelimiter;
                    int delimLength = delimiter.Length;
                    int length = fileText.Length;
                    int index1 = fileText.IndexOf(delimiter);
                    if (index1 > 0)
                    {
                        int index2 = fileText.IndexOf(delimiter, index1 + delimLength);
                        if (index2 > 0)
                        {
                            header = fileText.Substring(0, index1);
                            schema = fileText.Substring (index1 + delimLength, index2 - index1 - delimLength);
                            items = fileText.Substring(index2 + delimLength);
                            return true;
                        }
                    }
                }
            }
            catch { }

            header = null;
            schema = null;
            items = null;
            return false;

        }
        public string Save()
        {
            var contentSb = new StringBuilder();
            var delimiter = PartsDelimiter;

            var argsCollection = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this, argsCollection);
            contentSb.AppendLine(argsCollection.DataContent);
            contentSb.AppendLine(delimiter);

            var argsDefinition = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this.Definition, argsDefinition);
            contentSb.AppendLine(argsDefinition.DataContent);
            contentSb.AppendLine(delimiter);

            var argsContent = new XmlSerial.PersistArgs();
            XmlSerial.Persist.Serialize(this.Content, argsContent);
            contentSb.AppendLine(argsContent.DataContent);

            string content = contentSb.ToString();
            string fileName = this.FileName;
            if (!String.IsNullOrEmpty(fileName))
            {
                if (Application.MainApp.TryPrepareAppPathForFile(fileName, true))
                    System.IO.File.WriteAllText(fileName, content, Encoding.UTF8);
            }

            return content;
        }
        private const string PartsDelimiter = "<!--      Next part start here ...     -->\r\n";
        #endregion

        /// <summary>
        /// Jméno sbírky
        /// </summary>
        public string Name { get; set; }




        [XmlSerial.PersistingEnabled(false)]
        public Definition Definition { get; set; }
        [XmlSerial.PersistingEnabled(false)]
        public Content Content { get; set; }
        [XmlSerial.PersistingEnabled(false)]
        public string FileName { get; set; }
    }
    

}
