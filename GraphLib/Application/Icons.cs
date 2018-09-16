using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Přístup k ikonkám aplikace
    /// </summary>
    public class Icons
    {
        #region Konstrukce, výchozí zmapování adresářů a jejich obsahu (nenačítají se obrázky)
        /// <summary>
        /// Správce ikonek
        /// </summary>
        /// <param name="appPath"></param>
        public Icons(string appPath)
        {
            this._AppPath = appPath;
            this._FileDict = new Dictionary<string, string>();
            this._ImageDict = new Dictionary<string, Image>();
            Tuple<string, int, string[]> images = _SearchPicPath(appPath);
            foreach (string fileName in images.Item3)
            {
                string ext = System.IO.Path.GetExtension(fileName).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".gif")
                {
                    string key = GetKey(fileName);
                    if (!this._FileDict.ContainsKey(key))
                        this._FileDict.Add(key, fileName);
                }
            }
        }
        /// <summary>
        /// Adresář aplikace
        /// </summary>
        private string _AppPath;
        /// <summary>
        /// Index souborů, kde klíčem je jméno (získané pomocí <see cref="GetKey(string)"/>, 
        /// a hodnotu je fullname jména souboru (adresář, jméno, přípona).
        /// </summary>
        private Dictionary<string, string> _FileDict;
        /// <summary>
        /// Index ikon, kde klíčem je jméno (získané pomocí <see cref="GetKey(string)"/>, 
        /// a hodnotu je buď načtený obrázek, nebo null (pokud pro daný název neexistuje obrázek).
        /// </summary>
        private Dictionary<string, Image> _ImageDict;
        /// <summary>
        /// Metoda vyhledá a vrátí adresář, obsahující největší počet souborů.
        /// </summary>
        /// <param name="appPath"></param>
        /// <returns></returns>
        private static Tuple<string, int, string[]> _SearchPicPath(string appPath)
        {
            List<Tuple<string, int, string[]>> paths = new List<Tuple<string, int, string[]>>();
            string prgPath = System.IO.Path.GetDirectoryName(appPath);         // Z adresáře "C:\Csharp\GraphLibrary\bin" vrátí "C:\Csharp\GraphLibrary"
            string testPath;

            testPath = System.IO.Path.Combine(appPath, "img");                 // Podadresář "img" vedle aplikace
            paths.Add(_SearchOnePath(testPath));
            testPath = System.IO.Path.Combine(appPath, "pic");                 // Podadresář "pic" vedle aplikace
            paths.Add(_SearchOnePath(testPath));
            testPath = System.IO.Path.Combine(prgPath, "img");                 // Podadresář "img" nad adresářem aplikace
            paths.Add(_SearchOnePath(testPath));
            testPath = System.IO.Path.Combine(prgPath, "pic");                 // Podadresář "pic" nad adresářem aplikace
            paths.Add(_SearchOnePath(testPath));
            testPath = System.IO.Path.Combine(prgPath, "GraphLib", "img");     // Podadresář "img" v adresáři bin/../GraphLib/
            paths.Add(_SearchOnePath(testPath));
            testPath = System.IO.Path.Combine(prgPath, "GraphLib", "pic");     // Podadresář "pic" v adresáři bin/../GraphLib/
            paths.Add(_SearchOnePath(testPath));

            paths.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return paths[0];
        }
        /// <summary>
        /// Vrátí informace o obsahu daného adresáře
        /// </summary>
        /// <param name="testPath"></param>
        /// <returns></returns>
        private static Tuple<string, int, string[]> _SearchOnePath(string testPath)
        {
            if (!System.IO.Directory.Exists(testPath)) return new Tuple<string, int, string[]>(testPath, 0, new string[0]);
            string[] files = System.IO.Directory.GetFiles(testPath, "*.*", System.IO.SearchOption.AllDirectories);
            return new Tuple<string, int, string[]>(testPath, files.Length, files);
        }
        /// <summary>
        /// Metoda vrátí klíč pro daný název souboru.
        /// Klíč neobsahuje adresář ani příponu, ale jméno souboru se nijak jinak nezkrazuje. Je pouze Trim() a ToLower().
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string GetKey(string fileName)
        {
            if (String.IsNullOrEmpty(fileName)) return "";
            string key = System.IO.Path.GetFileNameWithoutExtension(fileName);
            return key.Trim().ToLower();
        }
        #endregion
        #region Získání obrázku pro dané jméno
        /// <summary>
        /// Indexer vrátí grafickou instanci <see cref="Image"/> pro obrázek daného jména.
        /// Může vrátit null.
        /// </summary>
        /// <param name="iconName"></param>
        /// <returns></returns>
        public Image this[string iconName] { get { return this.GetImageByName(iconName); } }
        /// <summary>
        /// Metoda vrátí grafickou instanci <see cref="Image"/> z dat předaných v instanci <see cref="GuiImage"/>.
        /// Může vrátit null.
        /// </summary>
        /// <param name="guiImage"></param>
        /// <returns></returns>
        public Image GetImage(GuiImage guiImage)
        {
            if (guiImage == null) return null;
            if (guiImage.Image != null) return guiImage.Image;
            if (guiImage.ImageContent != null)
            {
                _ReloadGuiImage(guiImage);
                if (guiImage.Image != null) return guiImage.Image;
            }
            if (!String.IsNullOrEmpty(guiImage.ImageFile)) return this.GetImageByName(guiImage.ImageFile);
            return null;
        }
        /// <summary>
        /// Metoda zajistí vytvoření obrázku <see cref="GuiImage.Image"/> z dat uložených v <see cref="GuiImage.ImageContent"/>.
        /// Pokud z dat nelze vytvořit obrázek, pak nuluje obsah <see cref="GuiImage.ImageContent"/>, aby se o to někdo nepokoušel opakovaně.
        /// </summary>
        /// <param name="guiImage"></param>
        private static void _ReloadGuiImage(GuiImage guiImage)
        {
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(guiImage.ImageContent))
                {
                    guiImage.Image = Bitmap.FromStream(ms);
                }
            }
            catch
            {
                guiImage.ImageContent = null;
                guiImage.Image = null;
            }
        }
        /// <summary>
        /// Metoda vrátí Image pro dané jméno.
        /// </summary>
        /// <param name="iconName"></param>
        /// <returns></returns>
        private Image GetImageByName(string iconName)
        {
            Image image;
            string key = GetKey(iconName);
            if (this._ImageDict.TryGetValue(key, out image)) return image;     // Vrátíme Image, nebo i null pokud pro daný klíč něco evidujeme
            string fileName;
            if (this._FileDict.TryGetValue(key, out fileName))                 // Pro daný klíč máme soubor?
                image = GetImage(fileName);
            this._ImageDict.Add(key, image);
            return image;
        }
        /// <summary>
        /// Metoda vrátí Image z daného souboru.
        /// Může vrátit null (po jakékoli chybě).
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static Image GetImage(string fileName)
        {
            Image image = null;
            try
            {
                image = Bitmap.FromFile(fileName);
            }
            catch
            {
                image = null;
            }
            return image;
        }
        #endregion
    }
}
