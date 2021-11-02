using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Async;
using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Knihovna zdrojů
    /// </summary>
    public class DxResourceLibrary
    {
        #region Instance
        /// <summary>
        /// Instance obsahující zdroje
        /// </summary>
        protected static DxResourceLibrary Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                            __Current = new DxResourceLibrary();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Úložiště singletonu
        /// </summary>
        private static DxResourceLibrary __Current;
        /// <summary>
        /// Zámek singletonu
        /// </summary>
        private static object __Lock = new object();
        /// <summary>
        /// Konstruktor, načte adresáře se zdroji
        /// </summary>
        private DxResourceLibrary()
        {
            __ItemDict = new Dictionary<string, ResourceItem>();
            __PackDict = new Dictionary<string, ResourcePack>();
            __ImageListDict = new Dictionary<ResourceImageSizeType, System.Windows.Forms.ImageList>();
            LoadResources();
        }
        /// <summary>
        /// Dictionary explicitních zdrojů, kde klíčem je explicitní jméno zdroje = včetně suffixu (velikost) a přípony,
        /// a obsahem je jeden zdroj (obrázek)
        /// </summary>
        private Dictionary<string, ResourceItem> __ItemDict;
        /// <summary>
        /// Dictionary balíčkových zdrojů, kde klíčem je jméno balíčku = bez suffixu a bez přípony,
        /// a obsahem je balíček několika příbuzných zdrojů (stejný obrázek v různých velikostech)
        /// </summary>
        private Dictionary<string, ResourcePack> __PackDict;
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
            var resources = SystemAdapter.GetResources();
            foreach (var resource in resources)
            {
                ResourceItem item = ResourceItem.CreateFrom(resource);
                if (item == null) continue;
                __ItemDict.Store(item.Key, item);
                var pack = __PackDict.Get(item.PackKey, () => new ResourcePack(item.PackKey));
                pack.AddItem(item);
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
        public static bool ContainsResource(string resourceName) 
        { return Current._ContainsResource(resourceName); }
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
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static bool TryGetResource(string resourceName, out ResourceItem resourceItem, ResourceContentType? contentType = null, ResourceImageSizeType? sizeType = null)
        { return Current._TryGetResource(resourceName, out resourceItem, contentType, sizeType); }
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
        #endregion
        #region Získání Bitmapy
        public static Image CreateBitmap(string imageName, ResourceImageSizeType? sizeType = null) 
        { return Current._CreateBitmap(imageName, sizeType); }
        /// <summary>
        /// Vrátí instanci knihovny obrázků dané velikosti
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static System.Windows.Forms.ImageList GetImageList(ResourceImageSizeType size) 
        { return Current._GetImageList(size); }
        #endregion
        #region Private instanční sféra
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
        private bool _ContainsResource(string resourceName)
        {
            string key;
            key = ResourceItem.GetKey(resourceName);
            if (__ItemDict.ContainsKey(key)) return true;
            key = ResourcePack.GetKey(resourceName);
            if (__PackDict.ContainsKey(key)) return true;
            return false;
        }
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
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private bool _TryGetResource(string resourceName, out ResourceItem resourceItem, ResourceContentType? contentType = null, ResourceImageSizeType? sizeType = null)
        {
            if (_TryGetDirectItem(resourceName, out resourceItem)) return true;
            if (_TryGetPackItem(resourceName, out resourceItem, contentType, sizeType)) return true;
            resourceItem = null;
            return false;
        }
        /// <summary>
        /// Zkusí najít <see cref="ResourceItem"/> podle explicitního jména (tj. včetně suffixu a přípony).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _TryGetDirectItem(string resourceName, out ResourceItem item)
        {
            string itemKey = ResourceItem.GetKey(resourceName);
            return __ItemDict.TryGetValue(itemKey, out item);
        }
        /// <summary>
        /// Zkusí najít <see cref="ResourceItem"/> podle daného jména (typicky bez suffixu a přípony) v sadě zdrojů, a upřesní výsledek podle požadované velikosti a typu.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private bool _TryGetPackItem(string resourceName, out ResourceItem resourceItem, ResourceContentType? contentType, ResourceImageSizeType? sizeType)
        {
            resourceItem = null;
            string packKey = ResourcePack.GetKey(resourceName);
            if (!__PackDict.TryGetValue(packKey, out ResourcePack pack)) return false;
            if (!pack.TryGetItem(contentType, sizeType, out resourceItem)) return false;
            return true;
        }


        private Image _CreateBitmap(string imageName, ResourceImageSizeType? sizeType = null)
        {
            ResourceItem resourceItem;
            if (_TryGetResource(imageName, out resourceItem, ResourceContentType.Bitmap, sizeType))
                return resourceItem.CreateBmpImage();

            if (_TryGetResource(imageName, out resourceItem, ResourceContentType.Vector, sizeType))
                return resourceItem.CreateSvgImage();

            return null;
        }
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
            /// Konstruktor pro daný klíč = jméno souboru s relativním adresářem, bez značky velikosti a bez přípony
            /// </summary>
            /// <param name="packKey"></param>
            public ResourcePack(string packKey)
            {
                PackKey = packKey;
                ResourceItems = new List<ResourceItem>();
            }
            /// <summary>
            /// Klíč: obsahuje jméno adresáře a souboru, ale bez označení velikosti a bez přípony
            /// </summary>
            public string PackKey { get; private set; }
            /// <summary>
            /// Jednotlivé prvky, různých velikostí a typů
            /// </summary>
            public List<ResourceItem> ResourceItems { get; private set; }
            /// <summary>
            /// Přidá dodaný prvek do zdejší kolekce zdrojů <see cref="ResourceItems"/>
            /// </summary>
            /// <param name="item"></param>
            public void AddItem(ResourceItem item)
            {
                if (item != null)
                    ResourceItems.Add(item);
            }
            /// <summary>
            /// Určí patřičné vlastnosti zdroje na základě jeho jména souboru
            /// </summary>
            /// <param name="fullName"></param>
            /// <param name="packKey"></param>
            /// <param name="contentType"></param>
            /// <param name="sizeType"></param>
            internal static void DetectInfo(string fullName, out string packKey, out ResourceContentType contentType, out ResourceImageSizeType? sizeType)
            {
                string name = ResourceItem.GetKey(fullName);
                sizeType = null;
                if (RemoveContentTypeByExtension(ref name, out contentType))
                    RemoveSizeTypeBySuffix(ref name, out sizeType);
                packKey = name;
            }
            /// <summary>
            /// Vrátí klíč z daného jména souboru.
            /// Provede Trim() a ToLower() a záměnu zpětného lomítka za běžné lomítko.
            /// Odebere suffix označující velikost a odebere i příponu.
            /// Namísto NULL vrátí prázdný string, takový klíč lze použít do Dictionary (namísto NULL).
            /// </summary>
            /// <param name="fullName"></param>
            /// <returns></returns>
            internal static string GetKey(string fullName)
            {
                string key = ResourceItem.GetKey(fullName);
                if (RemoveContentTypeByExtension(ref key, out ResourceContentType contentType))
                    RemoveSizeTypeBySuffix(ref key, out ResourceImageSizeType? sizeType);
                return key;
            }
            /// <summary>
            /// Vyhledá konkrétní zdroj odpovídající zadání
            /// </summary>
            /// <param name="contentType"></param>
            /// <param name="sizeType">Hledaný typ velikosti. Pro zadání Large může najít i Middle, pro zadání Small může najít Middle, pro zadání Middle i vrátí Small nebo Large.</param>
            /// <param name="item"></param>
            /// <returns></returns>
            internal bool TryGetItem(ResourceContentType? contentType, ResourceImageSizeType? sizeType, out ResourceItem item)
            {
                item = null;
                if (this.ResourceItems.Count == 0) return false;
                if (sizeType.HasValue && contentType.HasValue)
                {   // Podle velikosti a typu obsahu:
                    item = this.ResourceItems.FirstOrDefault(i => i.IsSuitable(contentType, sizeType, 0));       // Hledáme zcela přesný obrázek
                    if (item == null)
                        item = this.ResourceItems.FirstOrDefault(i => i.IsSuitable(contentType, sizeType, 1));   // Akceptujeme i velikost o 1 stupeň jinou
                }
                else if (sizeType.HasValue && !contentType.HasValue)
                {   // Jen podle velikosti:
                    item = this.ResourceItems.FirstOrDefault(i => i.IsSuitable(null, sizeType, 0));
                    if (item == null)
                        item = this.ResourceItems.FirstOrDefault(i => i.IsSuitable(null, sizeType, 1));
                }
                if (item is null)
                {   // Nenalezeno: najdu prvek podle daného obsahu (nebo najdu null když požadovaný obsah není přítomen),
                    // nebo najdu jakýkoli první prvek (když typ obsahu není zadán):
                    item = this.ResourceItems.FirstOrDefault(i => i.IsSuitable(contentType, null));
                }
                return (item != null);
            }
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
            /// Vytvoří pracovní prvek <see cref="ResourceItem"/> z dodaných dat
            /// </summary>
            /// <param name="resource"></param>
            /// <returns></returns>
            internal static ResourceItem CreateFrom(IResourceItem resource)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Vrátí klíč z daného jména souboru.
            /// Provede Trim() a ToLower() a záměnu zpětného lomítka za běžné lomítko.
            /// Ponechává suffix označující velikost a ponechá i příponu.
            /// Namísto NULL vrátí prázdný string, takový klíč lze použít do Dictionary (namísto NULL).
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
            /// <param name="packKey"></param>
            /// <param name="fileName"></param>
            /// <param name="contentType"></param>
            /// <param name="sizeType"></param>
            private ResourceItem(string key, string packKey, string fileName, ResourceContentType contentType, ResourceImageSizeType? sizeType)
            {
                this.Key = key;
                this.PackKey = packKey;
                this.ContentType = contentType;
                this.SizeType = sizeType;
                this.FileName = fileName;
                this.FileType = System.IO.Path.GetExtension(fileName).ToLower().Trim();
                this._Content = null;
                this._ContentLoaded = false;
            }
            /// <summary>
            /// Podkladová data
            /// </summary>
            private IResourceItem _ResourceItem;
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
            /// Klíč: obsahuje relativní jméno adresáře a souboru, kompletní včetně označení velikosti a včetně přípony
            /// </summary>
            public string Key { get; private set; }
            /// <summary>
            /// Klíč skupinový: obsahuje relativní jméno adresáře a souboru, bez označení velikosti a bez přípony
            /// </summary>
            public string PackKey { get; private set; }
            /// <summary>
            /// Typ obsahu určený podle přípony
            /// </summary>
            public ResourceContentType ContentType { get; private set; }
            /// <summary>
            /// Velikost obrázku určená podle konvence názvu souboru
            /// </summary>
            public ResourceImageSizeType? SizeType { get; private set; }
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
            public System.Drawing.Size? RealBitmapSize { get { return _GetRealBitmapSize(); } }
            /// <summary>
            /// Typ velikosti bitmapy, určená podle obsahu, pro zdroje typu Bitmapa (<see cref="IsBitmap"/> = true).
            /// Bitmapy s menší stranou pod 24px jsou <see cref="ResourceImageSizeType.Small"/>;
            /// Bitmapy pod 32px jsou <see cref="ResourceImageSizeType.Medium"/>;
            /// Bitmapy 32px a více jsou <see cref="ResourceImageSizeType.Large"/>;
            /// </summary>
            public ResourceImageSizeType? RealBitmapSizeType { get { return _GetRealBitmapSizeType(); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu SVG (má příponu ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff")
            /// </summary>
            public bool IsBitmap { get { return (ContentType == ResourceContentType.Bitmap); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu SVG (má příponu ".svg")
            /// </summary>
            public bool IsSvg { get { return (ContentType == ResourceContentType.Vector); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj je typu XML (má příponu ".xml")
            /// </summary>
            public bool IsXml { get { return (ContentType == ResourceContentType.Xml); } }
            /// <summary>
            /// Obsahuje true, pokud this zdroj má platná data v <see cref="Content"/>.
            /// Obsahuje false v případě, kdy soubor nebylo možno načíst (práva, soubor zmizel, atd).
            /// <para/>
            /// Získání této hodnoty při prvotním volání vede k načtení obsahu do <see cref="Content"/>.
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
            private ResourceImageSizeType? _GetRealBitmapSizeType()
            {
                if (IsBitmap && !_BitmapSizeType.HasValue)
                {
                    var size = _GetRealBitmapSize();
                    _BitmapSizeType = (size.HasValue ? _GetBitmapSizeType(size.Value) : ResourceImageSizeType.None);
                }
                return _BitmapSizeType;
            }
            /// <summary>
            /// Vrátí velikost bitmapy v pixelech, z cache <see cref="_BitmapSize"/> anebo ji nyní zjistí a uloží
            /// </summary>
            /// <returns></returns>
            private System.Drawing.Size? _GetRealBitmapSize()
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
            /// <summary>
            /// Vrátí true, pokud this zdroj vyhovuje požadavku na typ obsahu a na velikost (pokud je zadáno).
            /// U velikosti obrázku lze zadat toleranci mezi velikostí reálnou a požadovanou:
            /// Pokud je <paramref name="sizeDiff"/> = 1 a zdejší velikost <see cref="SizeType"/> = <see cref="ResourceImageSizeType.Large"/>,
            /// a parametr <paramref name="sizeType"/> = <see cref="ResourceImageSizeType.Medium"/>, pak velikost (s tolerancí 1 stupeň) vyhovuje.
            /// Poku by <paramref name="sizeDiff"/> bylo 0 (=bez tolerance), pak by velikost nevyhovovala.
            /// </summary>
            /// <param name="contentType"></param>
            /// <param name="sizeType"></param>
            /// <param name="sizeDiff"></param>
            /// <returns></returns>
            internal bool IsSuitable(ResourceContentType? contentType, ResourceImageSizeType? sizeType, int sizeDiff = 0)
            {
                if (contentType.HasValue && this.ContentType != contentType.Value) return false;   // Pokud chci konkrétní typ, a zdejší je jiný, pak to nelze akceptovat
                if (!sizeType.HasValue || !this.SizeType.HasValue) return true;                    // Pokud není zadaná anebo není požadovaná velikost, pak zdejší záznam lze akceptovat
                // Máme zadanou velikost požadovanou i reálnou:
                int currDiff = ((int)sizeType.Value) - ((int)SizeType.Value);                      // O kolik stupňů se liší velikost požadovaná od velikosti reálné
                if (currDiff < 0) currDiff = -currDiff;
                return (currDiff <= sizeDiff);             // Pokud aktuální rozdíl velikostí (požadované - reálné) je menší nebo rovný danému požadavku, pak zdejší zdroj lze akceptovat
            }
            #endregion
        }
        #endregion
    }
}

