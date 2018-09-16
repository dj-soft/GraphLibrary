using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RES = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Správce dalších zdrojů
    /// </summary>
    public class Resources
    {
        #region Konstruktor a načítání Resources
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="resourceFile"></param>
        public Resources(string resourceFile)
        {
            this._ResourceDict = new Dictionary<string, byte[]>();
            if (!String.IsNullOrEmpty(resourceFile))
                this._ReadResources(resourceFile);
        }
        /// <summary>
        /// Úložiště zdrojů
        /// </summary>
        private Dictionary<string, byte[]> _ResourceDict;
        /// <summary>
        /// Vrátí klíč pro konkrétní jméno souboru.
        /// Klíč je Trim; ToLower; a namísto zpětného lomítka obsahuje obyčejné lomítko (znak dělení).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetKey(string name)
        {
            if (name == null) return "";
            return name
                .Trim()
                .ToLower()
                .Replace("\\", "/");
        }
        /// <summary>
        /// Načte obsah souboru resources
        /// </summary>
        /// <param name="resourceFile"></param>
        private void _ReadResources(string resourceFile)
        {
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(resourceFile);
            if (!fileInfo.Exists) return;

            Dictionary<string, byte[]> resourceDict = this._ReadResourceContent(fileInfo.FullName);
            this._ResourceDict = resourceDict;

            if (App.IsDebugMode && resourceDict.Count > 0)
                this._SaveResourceClass(fileInfo, resourceDict.Keys);
        }
        /// <summary>
        /// Metoda fyzicky načte obsah dodaného souboru (resourceFile)
        /// </summary>
        /// <param name="resourceFile"></param>
        /// <returns></returns>
        private Dictionary<string, byte[]> _ReadResourceContent(string resourceFile)
        {
            Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();

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
                            string key = GetKey(entry.FullName);
                            if (!result.ContainsKey(key))
                            {
                                using (var entryStream = entry.Open())
                                using (System.IO.MemoryStream entryContent = new System.IO.MemoryStream())
                                {
                                    entryStream.CopyTo(entryContent);
                                    result.Add(key, entryContent.ToArray());
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
        /// Metoda umožní uložit soubor "GraphLib\Application\ResourcesNames.cs", obsahující jména souborů načtených z Resources
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="fileKeys"></param>
        private void _SaveResourceClass(System.IO.FileInfo fileInfo, IEnumerable<string> fileKeys)
        {
            DateTime resourceDate = fileInfo.LastWriteTime;          // Datum souboru Resources
            string appPath = fileInfo.Directory.FullName;            // Adresář, kde běží aplikace
            string targetPath;
            string content = null;

            targetPath = System.IO.Path.Combine(UpPath(appPath), "GraphLib", "Shared");
            _SaveResourceClassToPath(targetPath, resourceDate, fileKeys, ref content);

            targetPath = System.IO.Path.Combine(UpPath(appPath), "App", "LCS", "Base");
            _SaveResourceClassToPath(targetPath, resourceDate, fileKeys, ref content);

        }
        private void _SaveResourceClassToPath(string targetPath, DateTime resourceDate, IEnumerable<string> fileKeys, ref string content)
        {
            if (!System.IO.Directory.Exists(targetPath)) return;

            string targetFile = System.IO.Path.Combine(targetPath, "WorkSchedulerResources.cs");
            DateTime? lastCodeDate = _ReadLastCodeDate(targetFile);
            if (lastCodeDate.HasValue && lastCodeDate.Value >= resourceDate) return;  // Soubor "ResourcesNames.cs" existuje a obsahuje správná (nebo novější) data

            if (content == null)
            {
                content = _SaveResourceClassCreateContent(resourceDate, fileKeys);
                if (content == null) return;
            }

            try
            {
                System.IO.File.WriteAllText(targetFile, content, Encoding.UTF8);
            }
            catch { }
        }
        private static string _SaveResourceClassCreateContent(DateTime resourceDate, IEnumerable<string> fileKeys)
        {
            List<_ResFileInfo> fileList = fileKeys.Select(k => new _ResFileInfo(k)).ToList();

            StringBuilder sb = new StringBuilder();

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
            sb.AppendLine("// Místo, kde je uloženo datum \"LastWriteTime\" souboru \"ASOL.GraphLib.res\" je na následujícím řádku:");
            sb.AppendLine("//     ResourceFile.LastWriteTime = " + Noris.LCS.Base.WorkScheduler.Convertor.DateTimeToString(resourceDate));

            var nsGroups = fileList.GroupBy(rfi => rfi.Namespace);
            foreach (var nsGroup in nsGroups)
            {
                sb.AppendLine("namespace Noris.LCS.Base.WorkScheduler.Resources." + nsGroup.Key);
                sb.AppendLine("{");
                var cnGroups = nsGroup.GroupBy(rfi => rfi.ClassName);
                foreach (var cnGroup in cnGroups)
                {
                    List<_ResFileInfo> cnFiles = cnGroup.ToList();
                    cnFiles.Sort((a, b) => String.Compare(a.FileName, b.FileName));
                    _ResFileInfo first = cnFiles[0];
                    sb.AppendLine("    /// <summary>");
                    sb.AppendLine("    /// Obsah adresáře " + first.ClassText);
                    sb.AppendLine("    /// </summary>");
                    sb.AppendLine("    public class " + first.ClassName);
                    sb.AppendLine("    {");

                    foreach (_ResFileInfo info in cnGroup)
                    {
                        sb.AppendLine("        public const string " + info.FileName + " = \"" + info.Key + "\";");
                    }

                    sb.AppendLine("    }");
                }
                sb.AppendLine("}");
            }

            return sb.ToString();

        }
        private class _ResFileInfo
        {
            public _ResFileInfo(string key)
            {
                this.Key = key;
                string file = key.Replace("/", "\\");

                string extension = System.IO.Path.GetExtension(file).ToLower();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp" || extension == ".gif")
                    this.Namespace = "Images";
                else
                    this.Namespace = "Other";

                string path = System.IO.Path.GetDirectoryName(file);
                string[] pathItems = path.Split('\\');
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
                this.ClassText = text;
                this.ClassName = name;

                this.FileName = _ToCSharpName(System.IO.Path.GetFileName(file));
            }
            public override string ToString()
            {
                return this.Namespace + " : " + this.ClassName + "." + this.FileName;
            }
            public string Key { get; private set; }
            public string Namespace { get; private set; }
            public string ClassText { get; private set; }
            public string ClassName { get; private set; }
            public string FileName { get; private set; }
        }
        private static string UpPath(string path)
        {
            return System.IO.Path.GetDirectoryName(path);
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
                _CSharpDisabled = " +-~<>,.;[]@{}$&#!%'\"\r\n\t".ToCharArray().ToDictionary(i => i);
            return !_CSharpDisabled.ContainsKey(c);
        }
        /// <summary>
        /// Index znaků, které nepatří do C Sharp názvu
        /// </summary>
        protected static Dictionary<char, char> _CSharpDisabled;
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
                        if (value.Length >= 19)
                        {
                            value = value.Substring(0, 19);
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
    }
}
