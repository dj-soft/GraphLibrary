using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Async;
using DevExpress.XtraEditors;
using DevExpress.Utils.Svg;

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
                __ItemDict.Store(item.ItemKey, item);
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
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static bool ContainsResource(string resourceName)
        { return Current._ContainsResource(resourceName); }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
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
        #region Získání Bitmapy, ImageListu a indexu do ImageListu
        public static Image GetImage(string imageName, ResourceImageSizeType sizeType = ResourceImageSizeType.Large)
        { return Current._CreateBitmap(imageName, sizeType); }
        /// <summary>
        /// Vrátí instanci knihovny obrázků dané velikosti
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static System.Windows.Forms.ImageList GetImageList(ResourceImageSizeType size)
        { return Current._GetImageList(size); }
        public static int GetImageListIndex(string imageName, ResourceImageSizeType sizeType, string caption = null)
        { return Current._GetImageListIndex(imageName, sizeType, caption); }
        #endregion
        #region Private instanční sféra
        /// <summary>
        /// Vrátí true, pokud knihovna obsahuje daný zdroj.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private bool _ContainsResource(string resourceName)
        {
            string itemKey = SystemAdapter.GetResourceItemKey(resourceName);
            if (__ItemDict.ContainsKey(itemKey)) return true;
            string packKey = SystemAdapter.GetResourcePackKey(resourceName, out var _, out var _);
            if (__PackDict.ContainsKey(packKey)) return true;
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
            if (_TryGetItem(resourceName, out resourceItem)) return true;
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
        private bool _TryGetItem(string resourceName, out ResourceItem item)
        {
            string itemKey = SystemAdapter.GetResourceItemKey(resourceName);
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
            string packKey = SystemAdapter.GetResourcePackKey(resourceName, out var _, out var _);
            if (!__PackDict.TryGetValue(packKey, out ResourcePack pack)) return false;
            if (!pack.TryGetItem(contentType, sizeType, out resourceItem)) return false;
            return true;
        }


        private Image _CreateBitmap(string imageName, ResourceImageSizeType sizeType)
        {
            ResourceItem resourceItem;
            if (_TryGetResource(imageName, out resourceItem, ResourceContentType.Bitmap, sizeType))
                return resourceItem.CreateBmpImage();

            if (_TryGetResource(imageName, out resourceItem, ResourceContentType.Vector, sizeType))
                return _RenderSvgAsImage(resourceItem.CreateSvgImage(), sizeType);

            return null;
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do bitmapy
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Image _RenderSvgAsImage(DevExpress.Utils.Svg.SvgImage svgImage, ResourceImageSizeType? sizeType)
        {
            Size imageSize = _GetImageSize(sizeType, true);
            var paletteProvider = DxComponent.GetSvgPalette();
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(svgImage, paletteProvider, imageSize);
            return svgImage.Render(imageSize, paletteProvider);
        }
        /// <summary>
        /// Vrací Size pro ikonu v dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <param name="applyCurrentZoom"></param>
        /// <returns></returns>
        private Size _GetImageSize(ResourceImageSizeType? sizeType, bool applyCurrentZoom)
        {
            int s = 0;
            switch (sizeType ?? ResourceImageSizeType.Large)
            {
                case ResourceImageSizeType.Small: 
                    s = 16; 
                    break;
                case ResourceImageSizeType.Medium: 
                    s = 24; 
                    break;
                case ResourceImageSizeType.Large:
                default:
                    s = 32;
                    break;
            }
            if (applyCurrentZoom)
                s = DxComponent.ZoomToGui(s);
            return new Size(s, s);
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


        private int _GetImageListIndex(string imageName, ResourceImageSizeType sizeType, string caption)
        {
            
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
            /// Vyhledá konkrétní zdroj odpovídající zadání
            /// </summary>
            /// <param name="contentType">Typ obsahu. Pokud je zadán, budou prohledány jen prvky daného typu. Pokud nebude zadán, prohledají se jakékoli prvky.</param>
            /// <param name="sizeType">Hledaný typ velikosti. Pro zadání Large může najít i Middle, pro zadání Small může najít Middle, pro zadání Middle i vrátí Small nebo Large.</param>
            /// <param name="item"></param>
            /// <returns></returns>
            internal bool TryGetItem(ResourceContentType? contentType, ResourceImageSizeType? sizeType, out ResourceItem item)
            {
                item = null;
                if (this.ResourceItems.Count == 0) return false;

                // Pracovní soupis odpovídající požadovanému typu obsahu, řešení pokud je počet vyhovujících prvků 0 nebo 1:
                var items = (contentType.HasValue ? this.ResourceItems.Where(i => i.ContentType == contentType.Value).ToArray() : this.ResourceItems.ToArray());
                if (items.Length == 0) return false;
                if (items.Length == 1) { item = items[0]; return true; }

                // Požadovanému typu obsahu vyhovuje více než 1 prvek - zkusíme najít optimální velikost, podle zadání (bez zadání = největší):
                ResourceImageSizeType size = sizeType ?? ResourceImageSizeType.Large;
                if (items.TryGetFirst(i => i.SizeType == size, out item)) return true;

                // Nemáme přesně odpovídající prvek podle požadované velikosti, najdeme tedy nějaký prvek v nejvhodnější velikosti:
                switch (size)
                {
                    case ResourceImageSizeType.Small:
                        return _TryGetItemBySize(items, out item, ResourceImageSizeType.Medium, ResourceImageSizeType.Large);
                    case ResourceImageSizeType.Medium:
                        return _TryGetItemBySize(items, out item, ResourceImageSizeType.Large, ResourceImageSizeType.Small);
                    case ResourceImageSizeType.Large:
                    default:
                        return _TryGetItemBySize(items, out item, ResourceImageSizeType.Medium, ResourceImageSizeType.Small);
                }
            }
            /// <summary>
            /// Najde první prvek ve velikosti dle pole <paramref name="sizes"/>, hledá jednotlivé velikosti v zadaném pořadí.
            /// Pokud nikdy nic nenajde, tak vrátí první prvek v poli.
            /// Vrací true.
            /// </summary>
            /// <param name="items"></param>
            /// <param name="item"></param>
            /// <param name="sizes"></param>
            /// <returns></returns>
            private bool _TryGetItemBySize(ResourceItem[] items, out ResourceItem item, params ResourceImageSizeType[] sizes)
            {
                item = null;
                if (items == null || items.Length == 0) return false;
                if (sizes != null && sizes.Length > 0)
                {
                    foreach (var size in sizes)
                    {
                        if (items.TryGetFirst(i => i.SizeType == size, out item))
                            return true;
                    }
                }
                item = items[0];
                return true;
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
            /// <param name="iResourceItem"></param>
            /// <returns></returns>
            internal static ResourceItem CreateFrom(IResourceItem iResourceItem)
            {
                if (iResourceItem is null || iResourceItem.ContentType == ResourceContentType.None) return null;
                return new ResourceItem(iResourceItem);
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="iResourceItem"></param>
            private ResourceItem(IResourceItem iResourceItem)
            {
                this._IResourceItem = iResourceItem;
            }
            /// <summary>
            /// Podkladová data
            /// </summary>
            private IResourceItem _IResourceItem;
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.ItemKey;
            }
            #endregion
            #region Public data o Resource, základní
            /// <summary>
            /// Klíč: obsahuje relativní jméno adresáře a souboru, kompletní včetně označení velikosti a včetně přípony
            /// </summary>
            public string ItemKey { get { return _IResourceItem.ItemKey; } }
            /// <summary>
            /// Klíč skupinový: obsahuje relativní jméno adresáře a souboru, bez označení velikosti a bez přípony
            /// </summary>
            public string PackKey { get { return _IResourceItem.PackKey; } }
            /// <summary>
            /// Typ obsahu určený podle přípony
            /// </summary>
            public ResourceContentType ContentType { get { return _IResourceItem.ContentType; } }
            /// <summary>
            /// Velikost obrázku určená podle konvence názvu souboru
            /// </summary>
            public ResourceImageSizeType SizeType { get { return _IResourceItem.SizeType; } }
            /// <summary>
            /// Obsah zdroje jako byte[].
            /// Pokud zdroj neexistuje (?) nebo nejde načíst, je zde null.
            /// </summary>
            public byte[] Content { get { return SystemAdapter.GetResourceContent(this._IResourceItem); } }
            #endregion
            #region Podpůrné public property: IsBitmap, IsSvg, IsValid...
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
            #endregion
            #region Tvorba Image nebo SVG objektu


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
                if (!IsBitmap) throw new InvalidOperationException($"ResourceItem.CreateBmpImage() error: Resource {ItemKey} is not BITMAP type.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} can not load content.");

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
                if (!IsSvg) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} is not SVG type.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} can not load content.");

                // Třída DevExpress.Utils.Svg.SvgImage deklaruje implicitní operátor: public static implicit operator SvgImage(byte[] data);
                return content;
            }
            #endregion
        }
        #endregion
    }
}

