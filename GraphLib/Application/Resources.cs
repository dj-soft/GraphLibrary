using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Data;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Správce zdrojů
    /// </summary>
    public class Resources
    {
        #region Konstruktor a načítání Resources
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="resourceFile">Název souboru zdrojů</param>
        /// <param name="isMainResources">true pro hlavní resources knihovnu, ta se má promítat do kódu WorkSchedulerResources.cs</param>
        public Resources(string resourceFile, bool isMainResources)
        {
            this._ResourceDict = new Dictionary<string, ResourceItem>();
            this._IsMainResources = isMainResources;
            if (!String.IsNullOrEmpty(resourceFile))
                this._ReadResources(resourceFile);
        }
        /// <summary>
        /// Úložiště zdrojů
        /// </summary>
        private Dictionary<string, ResourceItem> _ResourceDict;
        /// <summary>
        /// Obsahuje true pro hlavní resources knihovnu, ta se má promítat do kódu WorkSchedulerResources.cs
        /// </summary>
        private bool _IsMainResources;
        /// <summary>
        /// Načte obsah souboru resources
        /// </summary>
        /// <param name="resourceFile"></param>
        private void _ReadResources(string resourceFile)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(resourceFile);
            if (!fileInfo.Exists) return;

            Dictionary<string, ResourceItem> resourceDict = this._ReadResourceContent(fileInfo.FullName);
            this._ResourceDict = resourceDict;

            if (this._IsMainResources && App.IsDebugMode && resourceDict.Count > 0)
                this._SaveResourceClass(fileInfo, resourceDict.Values);
        }
        /// <summary>
        /// Metoda fyzicky načte obsah dodaného souboru (resourceFile)
        /// </summary>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        private Dictionary<string, ResourceItem> _ReadResourceContent(string resourceFile)
        {
            Dictionary<string, ResourceItem> result = new Dictionary<string, ResourceItem>();

            // byte[] resourceContent = System.IO.File.ReadAllBytes(resourceFile);
            //using (System.IO.Stream resourceStream = new System.IO.MemoryStream(resourceContent))
            try
            {
                using (System.IO.Stream resourceStream = new System.IO.FileStream(resourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(resourceStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.Length > 0)
                        {
                            string key = ResourceItem.GetKey(entry.FullName);
                            if (!result.ContainsKey(key))
                            {
                                using (var entryStream = entry.Open())
                                using (System.IO.MemoryStream entryContent = new System.IO.MemoryStream())
                                {
                                    entryStream.CopyTo(entryContent);
                                    result.Add(key, new ResourceItem(key, entryContent.ToArray()));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                App.Trace.Exception(exc);
            }

            return result;
        }
        #endregion
        #region Ukládání soupisu Resources do zdrojového kódu
        /// <summary>
        /// Metoda umožní uložit soubor "GraphLib\Shared\WorkSchedulerResources.cs", obsahující jména souborů načtených z Resources
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="resources"></param>
        private void _SaveResourceClass(System.IO.FileInfo fileInfo, IEnumerable<ResourceItem> resources)
        {
            DateTime resourceDate = fileInfo.LastWriteTime;          // Datum souboru Resources
            string appPath = fileInfo.Directory.FullName;            // Adresář, kde běží aplikace
            string targetPath;
            string content = null;

            targetPath = System.IO.Path.Combine(ResourceItem.UpPath(appPath, 1), "GraphLib", "Shared");
            _SaveResourceClassToPath(targetPath, resourceDate, resources, ref content);

            targetPath = System.IO.Path.Combine(ResourceItem.UpPath(appPath, 1), "App", "LCS", "Base");
            _SaveResourceClassToPath(targetPath, resourceDate, resources, ref content);
        }
        /// <summary>
        /// Zajistí uložení dat do souboru "WorkSchedulerResources.cs" do daného adresáře, pokud je to zapotřebí.
        /// Metoda si vytvoří obsah souboru (ref content), pokud je potřeba do souboru uložit aktuální stav, a obsah dosud nebyl vygenerován (je null).
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="resourceDate"></param>
        /// <param name="resources"></param>
        /// <param name="content"></param>
        private void _SaveResourceClassToPath(string targetPath, DateTime resourceDate, IEnumerable<ResourceItem> resources, ref string content)
        {
            if (!System.IO.Directory.Exists(targetPath)) return;

            resourceDate = resourceDate.TrimPart(DateTimePart.Seconds);
            string targetFile = System.IO.Path.Combine(targetPath, "WorkSchedulerResources.cs");
            DateTime? lastCodeDate = _ReadLastCodeDate(targetFile);
            if (lastCodeDate.HasValue)
            {
                DateTime codeDate = lastCodeDate.Value.TrimPart(DateTimePart.Seconds);
                if (codeDate >= resourceDate) return;      // Soubor "WorkSchedulerResources.cs" existuje a obsahuje správná (nebo novější) data
            }

            if (content == null)
            {
                content = _SaveResourceCreateContent(resourceDate, resources);
                if (content == null) return;
            }

            try
            {
                System.IO.File.WriteAllText(targetFile, content, Encoding.UTF8);
            }
            catch (Exception exc)
            {
                App.ShowWarning("Error " + exc.Message + " on update resource file " + targetFile);
                App.Trace.Exception(exc, "Update resources file " + targetFile);
            }
        }
        /// <summary>
        /// Metoda vrátí obsah souboru "WorkSchedulerResources.cs", pro dané datum souboru .res a pro jeho obsah (soupis jeho položek = souborů v ZIPu).
        /// </summary>
        /// <param name="resourceDate"></param>
        /// <param name="resources"></param>
        /// <returns></returns>
        private static string _SaveResourceCreateContent(DateTime resourceDate, IEnumerable<ResourceItem> resources)
        {
            StringBuilder sb = new StringBuilder();

            _SaveResourceAddFileHeader(sb, resourceDate);

            var nameSpaces = resources.GroupBy(r => r.Namespace);
            foreach (var nameSpace in nameSpaces)
            {   // Grupa nameSpace obsahuje všechny položky _ResFileInfo, které mají stejný Namespace:
                sb.AppendLine("namespace Noris.LCS.Base.WorkScheduler.Resources." + nameSpace.Key);
                sb.AppendLine("{");
                var classNames = nameSpace.GroupBy(r => r.ClassName);
                foreach (var className in classNames)
                {   // Grupa className obsahuje všechny položky _ResFileInfo, které mají stejný ClassName:
                    var classFiles = className.ToList();
                    classFiles.Sort((a, b) => String.Compare(a.FileName, b.FileName));
                    var first = classFiles[0];

                    sb.AppendLine("    #region " + first.ClassName);
                    _SaveResourceAddClassHeader(sb, first);
                    foreach (var info in classFiles)
                        sb.AppendLine("        public const string " + info.FileName + " = \"" + info.Key + "\";");
                    
                    sb.AppendLine("    }");
                    sb.AppendLine("    #endregion");
                }
                sb.AppendLine("}");
            }
            sb.AppendLine("#pragma warning restore 1591");

            return sb.ToString();
        }
        /// <summary>
        /// Metoda do daného <see cref="StringBuilder"/> zapíše záhlaví celého souboru 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="resourceDate"></param>
        private static void _SaveResourceAddFileHeader(StringBuilder sb, DateTime resourceDate)
        {
            resourceDate = resourceDate.TrimPart(DateTimePart.Seconds);

            sb.AppendLine("// Supervisor: DAJ");
            sb.AppendLine("// Part of Helios Green, proprietary software, (c) LCS International, a. s.");
            sb.AppendLine("// Redistribution and use in source and binary forms, with or without modification, ");
            sb.AppendLine("// is not permitted without valid contract with LCS International, a. s. ");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("");
            sb.AppendLine("// Tento soubor obsahuje třídy a konstanty, které popisují názvy resources na straně pluginu. Resources jsou Images a další zdroje ve formě bytového pole.");
            sb.AppendLine("// Tento soubor se nachází jednak v Greenu: Noris\\App\\Lcs\\Base\\WorkSchedulerResources.cs, a zcela identický i v GraphLibrary: \\GraphLib\\Shared\\WorkSchedulerResources.cs");
            sb.AppendLine("// UPOZORNĚNÍ: tento soubor nemá být editován uživatelem, protože jeho obsah si udržuje plugin sám.");
            sb.AppendLine("//   Plugin si načte resources, což je obsah ZIP souboru umístěného v tomtéž adresáři, kde je DLL soubor pluginu. Soubor má název \"ASOL.GraphLib.res\".");
            sb.AppendLine("//   A poté, když plugin běží v rámci VisualStudia (má připojen debugger), a přitom existuje ve vhodném umístění soubor \"WorkSchedulerResources.cs\",");
            sb.AppendLine("//      pak plugin prověří, že soubor \"WorkSchedulerResources.cs\" obsahuje uložené datum \"LastWriteTime\" souboru \"ASOL.GraphLib.res\".");
            sb.AppendLine("//   Pokud fyzické zdroje (\"ASOL.GraphLib.res\") jsou novější, pak znovu vygeneruje kompletní obsah souboru \"WorkSchedulerResources.cs\".");
            sb.AppendLine("// Generátor tohoto souboru je v aplikaci GraphLib, v kódu \"GraphLib\\Application\\Resources.cs\".");
            sb.AppendLine("// Místo, kde je uloženo datum \"LastWriteTime\" souboru \"ASOL.GraphLib.res\" je na následujícím řádku:");
            sb.AppendLine("//     ResourceFile.LastWriteTime = " + Noris.LCS.Base.WorkScheduler.Convertor.DateTimeToString(resourceDate));
            sb.AppendLine("#pragma warning disable 1591");
        }
        /// <summary>
        /// Metoda do daného <see cref="StringBuilder"/> zapíše záhlaví jedné třídy
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="first"></param>
        private static void _SaveResourceAddClassHeader(StringBuilder sb, ResourceItem first)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Obsah adresáře " + first.ClassText);
            sb.AppendLine("    /// <para/>");
            sb.AppendLine("    /// Programátor, který chce vidět jednotlivé ikonky, si najde soubor \"ASOL.GraphLib.res\" v adresáři pluginu WorkScheduler,");
            sb.AppendLine("    /// zkopíruje si jej do pracovního adresáře, přejmenuje příponu na .zip a rozzipuje.");
            sb.AppendLine("    /// <para/>");
            sb.AppendLine("    /// Programátor, který chce doplnit další resource, si do výše uvedeného rozbaleného adresáře přidá nové ikony nebno adresář s ikonami nebo jiná data,");
            sb.AppendLine("    /// poté celý adresář zazipuje, přejmenuje celý zip na \"ASOL.GraphLib.res\" a vloží soubor do balíčku WorkScheduleru.");
            sb.AppendLine("    /// <para/>");
            sb.AppendLine("    /// Poté programátor spustí WorkScheduler z Visual studia v režimu Debug, a plugin při startu nově vygeneruje soubor WorkSchedulerResources.cs, obsahující nově dodané položky jako konstanty.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class " + first.ClassName);
            sb.AppendLine("    {");
        }
        /// <summary>
        /// Vrací C Sharp název
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected static string _ToCSharpName(string text)
        {
            text = text.ToLower();
            string result = "";
            bool nextUpper = true;
            foreach (char c in text)
            {
                if (_IsCSharpEnabled(c))
                {
                    if (nextUpper)
                    {
                        result = result + c.ToString().ToUpper();
                        nextUpper = false;
                    }
                    else
                    {
                        result = result + c.ToString();
                    }
                }
                else
                {
                    nextUpper = true;
                }
            }
            return result;
        }
        /// <summary>
        /// Vrátí true, pokud daný znak může být součástí C Sharp názvu
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        protected static bool _IsCSharpEnabled(char c)
        {
            if (_CSharpDisabled == null)
                _CSharpDisabled = " _+-~<>,.;[]@{}$&#!%'\"\r\n\t".ToCharArray().ToDictionary(i => i);
            return !_CSharpDisabled.ContainsKey(c);
        }
        /// <summary>
        /// Index znaků, které nepatří do C Sharp názvu
        /// </summary>
        protected static Dictionary<char, char> _CSharpDisabled;
        /// <summary>
        /// Vrátí text Namespace pro daný typ resource
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static string _ToCSharpNamespace(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Image: return "Images";
                case ResourceType.Icon: return "Icons";
                case ResourceType.Video: return "Videos";
                case ResourceType.Audio: return "Audios";
            }
            return "Others";
        }
        /// <summary>
        /// Metoda otevře daný soubor (typicky je to soubor "GraphLib/Application/ResourcesNames.cs"), 
        /// najde v něm řádek, obsahující: "// ResourceFile.LastWriteTime = ......", načte vytečkovanou oblast a vrátí hodnotu DateTime.
        /// </summary>
        /// <param name="targetFile"></param>
        /// <returns></returns>
        private static DateTime? _ReadLastCodeDate(string targetFile)
        {
            if (!System.IO.File.Exists(targetFile)) return null;
            string searchFor1 = "//";
            string searchFor2 = "ResourceFile.LastWriteTime = ";
            using (System.IO.StreamReader textReader = new System.IO.StreamReader(targetFile, Encoding.UTF8, true))
            {
                while (!textReader.EndOfStream)
                {
                    string line = textReader.ReadLine();
                    if (String.IsNullOrEmpty(line)) continue;

                    int index1 = line.IndexOf(searchFor1);
                    int index2 = line.IndexOf(searchFor2);
                    if (index1 >= 0 && index2 >= 0 && index2 > index1 && (index2 + searchFor2.Length) < (line.Length - 12))
                    {
                        string value = line.Substring(index2 + searchFor2.Length).Trim();
                        if (value.Length >= 16)                      // Vyžadujeme přinejmenším čas HH:mm
                        {
                            if (value.Length >= 19)
                                value = value.Substring(0, 19);      // Akceptujeme čas HH:mm:ss, ale ne více
                            string sample = Noris.LCS.Base.WorkScheduler.Convertor.DateTimeToString(DateTime.Now);

                            DateTime dateTime = (DateTime)Noris.LCS.Base.WorkScheduler.Convertor.StringToDateTime(value);
                            if (dateTime.Year > 2000)
                                return dateTime;
                        }
                    }
                }
                textReader.Close();
            }
            return null;
        }
        #endregion
        #region Čtení a vracení dat z Resource
        /// <summary>
        /// Metoda vrátí <see cref="System.Drawing.Image"/> na základě dat v objektu <see cref="Noris.LCS.Base.WorkScheduler.GuiImage"/>.
        /// </summary>
        /// <param name="guiImage"></param>
        /// <returns></returns>
        public Image GetImage(Noris.LCS.Base.WorkScheduler.GuiImage guiImage)
        {
            if (guiImage == null) return null;
            if (guiImage.Image != null) return guiImage.Image;
            if (guiImage.ImageContent != null)
            {
                guiImage.Image = ResourceItem.GetImage(guiImage.ImageContent);
                return guiImage.Image;
            }
            if (guiImage.ImageFile != null) return this.GetImage(guiImage.ImageFile);
            return null;
        }
        /// <summary>
        /// Metoda vrátí image daného jména
        /// </summary>
        /// <param name="resourceKey"></param>
        /// <returns></returns>
        public Image GetImage(string resourceKey)
        {
            ResourceItem resourceItem = this.SearchForResource(resourceKey);
            return (resourceItem != null ? resourceItem.Image : null);
        }
        /// <summary>
        /// Vrátí zdroj daného jména, nebo null.
        /// </summary>
        /// <param name="resourceKey"></param>
        /// <returns></returns>
        private ResourceItem SearchForResource(string resourceKey)
        {
            string key = ResourceItem.GetKey(resourceKey);
            ResourceItem resourceItem;
            if (this._ResourceDict.TryGetValue(key, out resourceItem)) return resourceItem;
            return null;
        }
        #endregion
        #region class ResourceItem
        /// <summary>
        /// Třída pro data jednoho zdroje
        /// </summary>
        private class ResourceItem
        {
            #region Konstrukce a základní data
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="key"></param>
            /// <param name="buffer"></param>
            public ResourceItem(string key, byte[] buffer)
            {
                this._Key = key;
                this._Buffer = buffer;
                this._Type = GetResourceType(key);
                this._KeyIsParsed = false;
            }
            /// <summary>
            /// Klíč = jméno souboru, Trim(), ToLower(), 
            /// </summary>
            public string Key { get { return this._Key; } } private string _Key;
            /// <summary>
            /// RAW data objektu
            /// </summary>
            public byte[] Buffer { get { return this._Buffer; } } private byte[] _Buffer;
            /// <summary>
            /// Typ objektu
            /// </summary>
            public ResourceType Type { get { return this._Type; } } private ResourceType _Type;
            /// <summary>
            /// Obrázek Image, pokud <see cref="Type"/> je <see cref="ResourceType.Image"/>.
            /// Objekt <see cref="System.Drawing.Image"/> bude z uložených dat <see cref="Buffer"/> vytvořen až v případě potřeby.
            /// Může být ull, pokud <see cref="Type"/> není <see cref="ResourceType.Image"/>, nebo <see cref="Buffer"/> je prázdný nebo vadný.
            /// </summary>
            public Image Image { get { this.CheckImage(); return this._Image; } } private Image _Image;
            /// <summary>
            /// true po vytvoření <see cref="Image"/>
            /// </summary>
            private bool _ImageCreated;
            /// <summary>
            /// Metoda v případě potřeby vytvoří <see cref="Image"/> z dat v <see cref="Buffer"/>.
            /// </summary>
            protected void CheckImage()
            {
                if (this._ImageCreated) return;
                if (this.Type != ResourceType.Image || this.Buffer == null) return;
                this._Image = GetImage(this.Buffer);
                this._ImageCreated = true;
            }
            #endregion
            #region Servis
            /// <summary>
            /// Vrátí klíč pro konkrétní jméno souboru.
            /// Klíč je Trim; ToLower; a namísto zpětného lomítka obsahuje obyčejné lomítko (znak dělení).
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public static string GetKey(string name)
            {
                if (name == null) return "";
                return name
                    .Trim()
                    .ToLower()
                    .Replace("\\", "/");
            }
            /// <summary>
            /// Vrátí typ zdroje podle jeho přípony
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public static ResourceType GetResourceType(string key)
            {
                string file = key.Replace("/", "\\");
                string extension = System.IO.Path.GetExtension(file).ToLower();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif") return ResourceType.Image;
                if (extension == ".ico") return ResourceType.Icon;
                if (extension == ".avi" || extension == ".mp4" || extension == ".flv" || extension == ".mpg" || extension == ".mpeg") return ResourceType.Video;
                if (extension == ".mp3" || extension == ".wav" || extension == ".flac" || extension == ".mpc") return ResourceType.Video;
                return ResourceType.Other;
            }
            /// <summary>
            /// Metoda vrátí nad-adresář n-té úrovně
            /// </summary>
            /// <param name="path"></param>
            /// <param name="upCount"></param>
            /// <returns></returns>
            public static string UpPath(string path, int upCount)
            {
                string result = path;
                for (int i = 0; (result != null && result.Length > 0 && i < upCount); i++)
                    result = System.IO.Path.GetDirectoryName(result);
                return result;
            }
            /// <summary>
            /// Metoda vrátí <see cref="System.Drawing.Image"/> z dat v bufferu.
            /// Pokud něco selže, vrátí null.
            /// </summary>
            /// <param name="buffer"></param>
            /// <returns></returns>
            public static Image GetImage(byte[] buffer)
            {
                Image result = null;
                try
                {
                    if (buffer != null && buffer.Length > 10)
                    {
                        using (var stream = new System.IO.MemoryStream(buffer))
                        {
                            result = Bitmap.FromStream(stream);
                        }
                    }
                }
                catch
                {
                    result = null;
                }
                return result;
            }
            #endregion
            #region Data pro generování zdrojového C# kódu
            /// <summary>
            /// Namespace, obsahuje "Images", "Other", atd
            /// </summary>
            public string Namespace { get { this.CheckParsed(); return this._Namespace; } } private string _Namespace;
            /// <summary>
            /// Text do popisku třídy, uživatelský název vstupního adresáře
            /// Příklad: pro vstupní soubor "status\tango-a16\anonymous_simple_weather_symbols_1.png" je zde "Status\Tango-a16".
            /// </summary>
            public string ClassText { get { this.CheckParsed(); return this._ClassText; } } private string _ClassText;
            /// <summary>
            /// Název třídy, C# korektní
            /// Příklad: pro vstupní soubor "status\tango-a16\anonymous_simple_weather_symbols_1.png" je zde "Status_TangoA16".
            /// </summary>
            public string ClassName { get { this.CheckParsed(); return this._ClassName; } } private string _ClassName;
            /// <summary>
            /// Název souboru, C# korektní.
            /// Příklad: pro vstupní soubor "status\tango-a16\anonymous_simple_weather_symbols_1.png" je zde "AnonymousSimpleWeatherSymbols1Png".
            /// </summary>
            public string FileName { get { this.CheckParsed(); return this._FileName; } } private string _FileName;
            /// <summary>
            /// true po parsování dat Key
            /// </summary>
            private bool _KeyIsParsed;
            #endregion
            #region Rozborka klíče (=název souboru včetně adresáře) na prvky C# kódu
            /// <summary>
            /// Z klíče (název souboru) <see cref="Key"/> odvodí hodnoty do <see cref="Namespace"/>, <see cref="ClassText"/>, <see cref="ClassName"/>, <see cref="FileName"/>
            /// </summary>
            protected void CheckParsed()
            {
                if (this._KeyIsParsed) return;

                this._Namespace = _ToCSharpNamespace(this.Type);

                string file = this.Key.Replace("/", "\\");                          // status\tango-a16\anonymous_simple_weather_symbols_1.png
                string path = System.IO.Path.GetDirectoryName(file);                // status\tango-a16
                string[] pathItems = path.Split('\\');                              // { status, tango-a16 }
                string text = "";
                string name = "";
                foreach (string pathItem in pathItems)
                {
                    int length = pathItem.Length;
                    if (length == 0) continue;
                    string item = pathItem.Substring(0, 1).ToUpper() + (length > 1 ? pathItem.Substring(1).ToLower() : "");
                    text = text + (text.Length == 0 ? "" : "\\") + item;
                    name = name + (name.Length == 0 ? "" : "_") + _ToCSharpName(item);
                }
                this._ClassText = text;                                             // Status\Tango-a16
                this._ClassName = name;                                             // Status_TangoA16
                this._FileName = _ToCSharpName(System.IO.Path.GetFileName(file));   // AnonymousSimpleWeatherSymbols1Png
            }
            #endregion
        }
        /// <summary>
        /// Druh zdroje
        /// </summary>
        public enum ResourceType
        {
            /// <summary>
            /// Dosud neurčeno
            /// </summary>
            None,
            /// <summary>
            /// Obrázek (Image)
            /// </summary>
            Image,
            /// <summary>
            /// Ikona
            /// </summary>
            Icon,
            /// <summary>
            /// Video
            /// </summary>
            Video,
            /// <summary>
            /// Audio
            /// </summary>
            Audio,
            /// <summary>
            /// Jiná data
            /// </summary>
            Other
        }
        #endregion
    }
}
