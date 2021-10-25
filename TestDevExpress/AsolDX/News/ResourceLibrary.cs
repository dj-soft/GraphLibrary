using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Async;
using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Knihovna zdrojů
    /// </summary>
    public class ResourceLibrary
    {
        #region Instance
        /// <summary>
        /// Instance obsahující zdroje
        /// </summary>
        protected static ResourceLibrary Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                            __Current = new ResourceLibrary();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Úložiště singletonu
        /// </summary>
        private static ResourceLibrary __Current;
        /// <summary>
        /// Zámek singletonu
        /// </summary>
        private static object __Lock = new object();
        /// <summary>
        /// Konstruktor, načte adresáře se zdroji
        /// </summary>
        private ResourceLibrary()
        {
            __ItemDict = new Dictionary<string, ResourceItem>();
            LoadResources();
        }
        /// <summary>
        /// Dictionary zdrojů
        /// </summary>
        private Dictionary<string, ResourceItem> __ItemDict;
        #endregion
        #region Kolekce zdrojů, její načtení
        /// <summary>
        /// Zkusí najít adresáře se zdroji
        /// </summary>
        protected void LoadResources()
        {
            string resourcePath = DxComponent.ApplicationPath;
            LoadResources(resourcePath, 1, "Images");
            LoadResources(resourcePath, 1, "pic");
        }
        /// <summary>
        /// Zkusí najít zdroje v jednom adresáři
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="upDirs"></param>
        /// <param name="subDir"></param>
        private void LoadResources(string resourcePath, int upDirs, string subDir)
        {
            int pathLength = resourcePath.Length;
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            if (!String.IsNullOrEmpty(subDir)) path = System.IO.Path.Combine(path, subDir);
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            if (!dirInfo.Exists) return;
            
            foreach (var fileInfo in dirInfo.EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories))
            {
                if (!ResourceItem.IsResource(fileInfo)) continue;
                ResourceItem item = ResourceItem.CreateFromFile(fileInfo, pathLength);
                if (item == null) continue;
                if (__ItemDict.ContainsKey(item.Key))
                    __ItemDict[item.Key] = item;
                else
                    __ItemDict.Add(item.Key, item);
            }
        }
        /// <summary>
        /// Jedna položka zdrojů
        /// </summary>
        protected class ResourceItem
        {
            /// <summary>
            /// Vrátí true, pokud daný soubor může býti Resource
            /// </summary>
            /// <param name="fileInfo"></param>
            /// <returns></returns>
            internal static bool IsResource(System.IO.FileInfo fileInfo)
            {
                if (fileInfo is null) return false;
                if (fileInfo.Length >= 0x200000) return false;                 // 0x200000 = 2MB na jeden soubor Resource, to je vcelku dost, ne?
                string ext = fileInfo.Extension.ToLower();
                return (ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".tif" || ext == ".tiff" || ext == ".svg" || ext == ".ico" || ext == ".mp3" || ext == ".cur" || ext == ".xml");
            }
            /// <summary>
            /// Vytvoří a vrátí prvek pro daný soubor. Může vrátit NULL.
            /// </summary>
            /// <param name="fileInfo"></param>
            /// <param name="commonPathLength"></param>
            /// <returns></returns>
            internal static ResourceItem CreateFromFile(System.IO.FileInfo fileInfo, int commonPathLength = 0)
            {
                if (fileInfo == null || !fileInfo.Exists || fileInfo.FullName.Length <= commonPathLength) return null;
                string key = GetKey(commonPathLength <= 0 ? fileInfo.FullName : fileInfo.FullName.Substring(commonPathLength));
                return new ResourceItem(key, fileInfo.Name);
            }
            /// <summary>
            /// Vrátí klíč z daného jména souboru.
            /// Provede Trim()  ToLower() a záměnu zpětného lomítka za běžné lomítko.
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            internal static string GetKey(string text)
            {
                return (text ?? "").Trim().ToLower().Replace("\\", "/");
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="key"></param>
            /// <param name="fileName"></param>
            private ResourceItem(string key, string fileName)
            {
                this.Key = key;
                this.FileName = fileName;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.FileName;
            }
            /// <summary>
            /// Klíč
            /// </summary>
            public string Key { get; private set; }
            /// <summary>
            /// Plné jméno souboru
            /// </summary>
            public string FileName { get; private set; }
        }
        #endregion
        #region Public static
        public static int Count { get { return Current.__ItemDict.Count; } }
        #endregion



    }
}
