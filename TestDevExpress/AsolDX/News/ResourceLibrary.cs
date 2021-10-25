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
            __ImageListDict = new Dictionary<ResourceImageSizeType, System.Windows.Forms.ImageList>();
            LoadResources();
        }
        /// <summary>
        /// Dictionary zdrojů, kde klíčem je explicitní jméno zdroje
        /// </summary>
        private Dictionary<string, ResourceItem> __ItemDict;
        /// <summary>
        /// Dictionary ImageListů
        /// </summary>
        private Dictionary<ResourceImageSizeType, System.Windows.Forms.ImageList> __ImageListDict;
        #endregion
        #region Kolekce zdrojů, její načtení
        /// <summary>
        /// Zkusí najít adresáře se zdroji a načíst jejich soubory
        /// </summary>
        protected void LoadResources()
        {
            string resourcePath = DxComponent.ApplicationPath;
            LoadResources(resourcePath, 0, "Resources");
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
            string path = resourcePath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return;
            int pathLength = path.Length;
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
        #endregion
        #region Public static rozhraní
        /// <summary>
        /// Počet položek v evidenci = jednotlivé soubory
        /// </summary>
        public static int Count { get { return Current.__ItemDict.Count; } }
        /// <summary>
        /// Vrátí true, pokud knihovna obsahuje daný zdroj.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Ale musí být kompletní, tj. včetně velikosti obrázku a přípony.
        /// Nelze tedy očekávat, že po zadání jména "pic/ribbon/refresh" bude dohledán a identifikován zdroj se jménem "pic/ribbon/refresh-24x24.png".
        /// Je třeba zadat plné jméno s příponou. Hledání je case-insensitive, krajní mezery jsou odstraněny, úvodní nadbytečné lomítko je odstraněno.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static bool ContainsKey(string resourceName) { return Current.__ItemDict.ContainsKey(ResourceItem.GetKey(resourceName)); }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Ale musí být kompletní, tj. včetně velikosti obrázku a přípony.
        /// Nelze tedy očekávat, že po zadání jména "pic/ribbon/refresh" bude dohledán a identifikován zdroj se jménem "pic/ribbon/refresh-24x24.png".
        /// Je třeba zadat plné jméno s příponou. Hledání je case-insensitive, krajní mezery jsou odstraněny, úvodní nadbytečné lomítko je odstraněno.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        public static bool TryGetResource(string resourceName, out ResourceItem resourceItem) { return Current.__ItemDict.TryGetValue(ResourceItem.GetKey(resourceName), out resourceItem); }
        /// <summary>
        /// Metoda vrátí náhodný zdroj (dané přípony).
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetRandomName(string extension = null)
        {
            var dict = Current.__ItemDict;
            string[] keys;
            var ext = (String.IsNullOrEmpty(extension) ? null : extension.Trim().ToLower());
            if (ext == null)
                keys = dict.Keys.ToArray();
            else
                keys = dict.Keys.Where(k => k.EndsWith(ext)).ToArray();
            return DxComponent.GetRandomItem(keys);
        }
        /// <summary>
        /// Vrátí instanci knihovny obrázků dané velikosti
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static System.Windows.Forms.ImageList GetImageList(ResourceImageSizeType size) { return Current._GetImageList(size); }
        #endregion
        #region Private instanční sféra
        /// <summary>
        /// Najde / vytvoří a uloží / a vrátí ImageList pro danou velikost
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private System.Windows.Forms.ImageList _GetImageList(ResourceImageSizeType size)
        {
            System.Windows.Forms.ImageList imageList;
            if (!__ImageListDict.TryGetValue(size, out imageList))
            {
                lock (__ImageListDict)
                {
                    if (!__ImageListDict.TryGetValue(size, out imageList))
                    {
                        imageList = new System.Windows.Forms.ImageList();
                        __ImageListDict.Add(size, imageList);
                    }
                }
            }
            return imageList;
        }

        #endregion
        #region class ResourcePack
        /// <summary>
        /// Balíček několika variantních zdrojů jednoho typu (odlišují se velikostí, typem a vhodností pro Dark/Light skin)
        /// </summary>
        protected class ResourcePack
        {
            /// <summary>
            /// Klíč: obsahuje jméno adresáře a souboru, ale bez označení velikosti a bez přípony
            /// </summary>
            public string Key { get; private set; }
            /// <summary>
            /// Jednotlivé prvky, různých velikostí a typů
            /// </summary>
            public List<ResourceItem> ResourceItems { get; private set; }
        }
        #endregion
        #region class ResourceItem
        /// <summary>
        /// Jedna položka zdrojů = jeden soubor
        /// </summary>
        public class ResourceItem
        {
            #region Tvorba prvku
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
                return _IsExtension(ext, ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".svg", ".ico", ".mp3", ".cur", ".xml");
            }
            /// <summary>
            /// Vrátí true, pokud daná přípona <paramref name="extension"/> se najde v poli povolených přípon <paramref name="exts"/>.
            /// </summary>
            /// <param name="extension"></param>
            /// <param name="exts"></param>
            /// <returns></returns>
            private static bool _IsExtension(string extension, params string[] exts) { return exts.Any(e => e == extension); }
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
                return new ResourceItem(key, fileInfo.FullName);
            }
            /// <summary>
            /// Vrátí klíč z daného jména souboru.
            /// Provede Trim()  ToLower() a záměnu zpětného lomítka za běžné lomítko.
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            internal static string GetKey(string text)
            {
                string key = (text ?? "").Trim().ToLower().Replace("\\", "/");
                while (key.Length > 0 && key[0] == '/') key = key.Substring(1).Trim();
                return key;
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
                this.FileType = System.IO.Path.GetExtension(fileName).ToLower().Trim();
                this._Content = null;
                this._ContentLoaded = false;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.FileName;
            }
            #endregion
            #region Public data
            /// <summary>
            /// Klíč: obsahuje jméno adresáře a souboru, kompletní včetně označení velikosti a včetně přípony
            /// </summary>
            public string Key { get; private set; }
            /// <summary>
            /// Plné jméno souboru
            /// </summary>
            public string FileName { get; private set; }
            /// <summary>
            /// Typ souboru = přípona, ToLower(), včetně tečky; například ".jpg", ".png", ".svg", ".xml" atd
            /// </summary>
            public string FileType { get; private set; }
            /// <summary>
            /// Fyzická velikost bitmapy, určená podle obsahu, pro zdroje typu Bitmapa (<see cref="IsBitmap"/> = true).
            /// </summary>
            public System.Drawing.Size? BitmapSize { get { return _GetBitmapSize(); } }
            /// <summary>
            /// Typ velikosti bitmapy, určená podle obsahu, pro zdroje typu Bitmapa (<see cref="IsBitmap"/> = true).
            /// Bitmapy s menší stranou pod 24px jsou <see cref="ResourceImageSizeType.Small"/>;
            /// Bitmapy pod 32px jsou <see cref="ResourceImageSizeType.Medium"/>;
            /// Bitmapy 32px a více jsou <see cref="ResourceImageSizeType.Large"/>;
            /// </summary>
            public ResourceImageSizeType? BitmapSizeType { get { return _GetBitmapSizeType(); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu SVG (má příponu ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff")
            /// </summary>
            public bool IsBitmap { get { return _IsExtension(this.FileType, ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu SVG (má příponu ".svg")
            /// </summary>
            public bool IsSvg { get { return _IsExtension(this.FileType, ".svg"); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu XML (má příponu ".xml")
            /// </summary>
            public bool IsXml { get { return _IsExtension(this.FileType, ".xml"); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj má platná data v <see cref="Content"/>.
            /// Obsahuje false v případě, kdy soubor nebylo možno načíst (práva, soubor zmizel, atd).
            /// </summary>
            public bool IsValid { get { return (this.Content != null); } }
            /// <summary>
            /// Obsah souboru, byte[]. 
            /// Pokud soubor neexistuje (?) nebo nejde načíst, je zde null.
            /// </summary>
            public byte[] Content { get { return _GetContent(); } }
            /// <summary>
            /// Vrátí obsah souboru, v případě potřeby jej načte. Může vrátit null, pokud při načítání došlo k chybě.
            /// </summary>
            /// <returns></returns>
            private byte[] _GetContent()
            {
                if (!_ContentLoaded)
                {
                    _ContentLoaded = true;
                    DxComponent.TryRun(() => _Content = System.IO.File.ReadAllBytes(this.FileName));       // Chyba bude oznámena, ale načtení proběhne jen jedenkrát.
                }
                return _Content;
            }
            /// <summary>Data ze souboru nebo null</summary>
            private byte[] _Content;
            /// <summary>false před pokusem o načtení obsahu souboru, true po načtení (i po chybném načtení)</summary>
            private bool _ContentLoaded;
            #endregion
            #region Tvorba Image nebo SVG objektu
            /// <summary>
            /// Vrátí druh velikosti aktuálního obrázku, z cache <see cref="_BitmapSizeType"/> anebo ji nyní zjistí a uloží
            /// </summary>
            /// <returns></returns>
            private ResourceImageSizeType? _GetBitmapSizeType()
            {
                if (IsBitmap && !_BitmapSizeType.HasValue)
                {
                    var size = _GetBitmapSize();
                    _BitmapSizeType = (size.HasValue ? _GetBitmapSizeType(size.Value) : ResourceImageSizeType.None);
                }
                return _BitmapSizeType;
            }
            /// <summary>
            /// Vrátí velikost bitmapy v pixelech, z cache <see cref="_BitmapSize"/> anebo ji nyní zjistí a uloží
            /// </summary>
            /// <returns></returns>
            private System.Drawing.Size? _GetBitmapSize()
            {
                if (IsBitmap && !_BitmapSize.HasValue)
                {
                    try
                    {
                        using (var bitmap = CreateBmpImage())
                        {
                            _BitmapSize = bitmap.Size;
                        }
                    }
                    catch { _BitmapSize = System.Drawing.Size.Empty; }
                }
                return _BitmapSize;
            }
            /// <summary>
            /// Vrátí druh velikosti pro danou pixelovou velikost
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            private ResourceImageSizeType _GetBitmapSizeType(System.Drawing.Size size)
            {
                int s = (size.Width < size.Height ? size.Width : size.Height);
                if (s < 22) return ResourceImageSizeType.Small;
                if (s < 32) return ResourceImageSizeType.Medium;
                return ResourceImageSizeType.Large;
            }
            /// <summary>Fyzická velikost aktuální bitmapy</summary>
            private System.Drawing.Size? _BitmapSize;
            /// <summary>Druh velikosti aktuální bitmapy</summary>
            private ResourceImageSizeType? _BitmapSizeType;
            /// <summary>
            /// Metoda vrátí new instanci <see cref="System.Drawing.Image"/> vytvořenou z <see cref="Content"/>.
            /// Pokud ale this instance není Bitmap (<see cref="IsBitmap"/> je false) anebo není platná, vyhodí chybu!
            /// <para/>
            /// Je vrácena new instance objektu, tato třída je <see cref="IDisposable"/>, používá se tedy v using { } patternu!
            /// </summary>
            /// <returns></returns>
            public System.Drawing.Image CreateBmpImage()
            {
                if (!IsBitmap) throw new InvalidOperationException($"ResourceItem.CreateBmpImage() error: Resource {Key} is not BITMAP type.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {Key} can not load file {FileName}.");

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(content))
                    return System.Drawing.Image.FromStream(ms);
            }
            /// <summary>
            /// Metoda vrátí new instanci <see cref="DevExpress.Utils.Svg.SvgImage"/> vytvořenou z <see cref="Content"/>.
            /// Pokud ale this instance není SVG (<see cref="IsSvg"/> je false) anebo není platná, vyhodí chybu!
            /// Vrácený objekt není nutno rewindovat (jako u nativní knihovny DevExpress).
            /// <para/>
            /// Je vrácena new instance objektu, ale tato třída není <see cref="IDisposable"/>, tedy nepoužívá se v using { } patternu.
            /// </summary>
            /// <returns></returns>
            public DevExpress.Utils.Svg.SvgImage CreateSvgImage()
            {
                if (!IsSvg) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {Key} is not SVG type.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {Key} can not load file {FileName}.");

                // Třída DevExpress.Utils.Svg.SvgImage deklaruje implicitní operátor: public static implicit operator SvgImage(byte[] data);
                return content;
            }
            #endregion
        }
        #endregion
    }
}

/*

c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-large.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-locations-large.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-locations-small.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-small.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-undo-2-large.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-undo-2-small.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-update-bottom-left-large.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\address-book-update-bottom-left-small.svg
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressDelete-16x16.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressDelete-24x24.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressDelete-32x32.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressEdit-16x16.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressEdit-24x24.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressEdit-32x32.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressCheckedRuian-16x16.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressCheckedRuian-24x24.png
c:\inetpub\wwwroot\Noris99\Noris\pic\AddressCheckedRuian-32x32.png

*/