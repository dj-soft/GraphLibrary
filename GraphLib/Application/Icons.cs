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
        public Icons(string appPath)
        {
            this._AppPath = appPath;
            Tuple < string, int, string[]> images = _SearchPicPath(appPath);
            this._PicPath = images.Item1;
            this._PicFiles = images.Item3;
        }
        private string _AppPath;
        private string _PicPath;
        private string[] _PicFiles;
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
        private static Tuple<string, int, string[]> _SearchOnePath(string testPath)
        {
            if (!System.IO.Directory.Exists(testPath)) return new Tuple<string, int, string[]>(testPath, 0, new string[0]);
            string[] files = System.IO.Directory.GetFiles(testPath, "*.*", System.IO.SearchOption.AllDirectories);
            return new Tuple<string, int, string[]>(testPath, files.Length, files);
        }
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
        private Image GetImageByName(string iconName)
        {
            return null;
        }
    }
}
