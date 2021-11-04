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
    /// Knihovna zdrojů výhradně aplikace (Resources.bin, adresář Images), nikoli zdroje DevEpxress.
    /// Tyto zdroje jsou získány pomocí metod <see cref="SystemAdapter.GetResources()"/> atd.
    /// <para/>
    /// Toto je pouze knihovna = zdroj dat (a jejich vyhledávání), ale nikoli výkonný blok, tady se negenerují obrázky ani nic dalšího.
    /// <para/>
    /// Zastřešující algoritmy pro oba druhy zdrojů (aplikační i DevExpress) jsou v <see cref="DxComponent"/>, 
    /// metody typicky <see cref="DxComponent.ApplyImage(DevExpress.Utils.ImageOptions, string, Image, Size?, bool)"/>.
    /// Aplikační kódy by tedy neměly komunikovat napřímo s touto třídou <see cref="DxApplicationResourceLibrary"/>, ale s <see cref="DxComponent"/>,
    /// aby měly k dispozici zdroje obou druhů.
    /// </summary>
    public class DxApplicationResourceLibrary
    {
        #region Instance
        /// <summary>
        /// Instance obsahující zdroje
        /// </summary>
        protected static DxApplicationResourceLibrary Current
        {
            get
            {
                if (__Current is null)
                {
                    lock (__Lock)
                    {
                        if (__Current is null)
                            __Current = new DxApplicationResourceLibrary();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Úložiště singletonu
        /// </summary>
        private static DxApplicationResourceLibrary __Current;
        /// <summary>
        /// Zámek singletonu
        /// </summary>
        private static object __Lock = new object();
        /// <summary>
        /// Konstruktor, načte adresáře se zdroji
        /// </summary>
        private DxApplicationResourceLibrary()
        {
            __ItemDict = new Dictionary<string, ResourceItem>();
            __PackDict = new Dictionary<string, ResourcePack>();
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
        #endregion
        #region Kolekce zdrojů, inicializace - její načtení pomocí dat z SystemAdapter
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
        #region Public static rozhraní základní (Count, ContainsResource, TryGetResource, GetRandomName)
        /// <summary>
        /// Počet jednotlivých položek v evidenci = jednotlivé soubory
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
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/> anebo <paramref name="resourcePack"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// <para/>
        /// Tato varianta najde buď konkrétní zdroj (pokud dané jméno odkazuje na konkrétní prvek) anebo najde balíček zdrojů (obsahují stejný zdroj v různých velikostech a typech obsahu).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="resourcePack"></param>
        /// <returns></returns>
        public static bool TryGetResource(string resourceName, out ResourceItem resourceItem, out ResourcePack resourcePack)
        { return Current._TryGetResource(resourceName, out resourceItem, out resourcePack); }
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
        /// Metoda vrátí náhodný zdroj (dané přípony, pokud není prázdná).
        /// </summary>
        /// <param name="extension">Přípona, vybírat jen záznamy s touto příponou. Měla by začínat tečkou: ".svg"</param>
        /// <returns></returns>
        public static string GetRandomName(string extension = null)
        {
            var dict = Current.__ItemDict;
            var ext = (String.IsNullOrEmpty(extension) ? null : extension.Trim().ToLower());
            string[] keys = (ext == null) ? dict.Keys.ToArray() : dict.Keys.Where(k => k.EndsWith(ext)).ToArray();
            return DxComponent.GetRandomItem(keys);
        }
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
            return _TryGetResource(resourceName, out var _, null, null);
        }
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
        private bool _TryGetResource(string resourceName, out ResourceItem resourceItem, ResourceContentType? contentType = null, ResourceImageSizeType? sizeType = null)
        {
            if (_TryGetItem(resourceName, out resourceItem)) return true;
            if (_TryGetPackItem(resourceName, out resourceItem, contentType, sizeType)) return true;
            resourceItem = null;
            return false;
        }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/> anebo <paramref name="resourcePack"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="resourceName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// <para/>
        /// Tato varianta najde buď konkrétní zdroj (pokud dané jméno odkazuje na konkrétní prvek) anebo najde balíček zdrojů (obsahují stejný zdroj v různých velikostech a typech obsahu).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="resourcePack"></param>
        private bool _TryGetResource(string resourceName, out ResourceItem resourceItem, out ResourcePack resourcePack)
        {
            resourcePack = null;
            if (_TryGetItem(resourceName, out resourceItem)) return true;
            if (_TryGetPack(resourceName, out resourcePack)) return true;
            return false;
        }
        /// <summary>
        /// Zkusí najít <see cref="ResourceItem"/> podle explicitního jména (tj. včetně suffixu a přípony).
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        private bool _TryGetItem(string resourceName, out ResourceItem resourceItem)
        {
            string itemKey = SystemAdapter.GetResourceItemKey(resourceName);
            return __ItemDict.TryGetValue(itemKey, out resourceItem);
        }
        /// <summary>
        /// Zkusí najít <see cref="ResourcePack"/> podle dodaného jména.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourcePack"></param>
        /// <returns></returns>
        private bool _TryGetPack(string resourceName, out ResourcePack resourcePack)
        {
            string packKey = SystemAdapter.GetResourcePackKey(resourceName, out var _, out var _);
            return __PackDict.TryGetValue(packKey, out resourcePack);
        }
        /// <summary>
        /// Zkusí najít <see cref="ResourceItem"/> podle daného jména (typicky bez suffixu a přípony) v sadě zdrojů, 
        /// a upřesní výsledek podle požadované velikosti a typu.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private bool _TryGetPackItem(string resourceName, out ResourceItem resourceItem, ResourceContentType? contentType, ResourceImageSizeType? sizeType)
        {
            resourceItem = null;
            string packKey = SystemAdapter.GetResourcePackKey(resourceName, out var nameSizeType, out var nameContentType);
            if (!__PackDict.TryGetValue(packKey, out ResourcePack pack)) return false;

            // Pokud v parametrech není daný typ velikosti a/nebo obsahu, a bylo jej možno odvodit ze jména, pak akceptujeme typ určený dle jména.
            // Jinými slovy: parametr je "Image/Button-24x24.jpg", tedy nameSizeType ("-24x24") = Medium, a nameContentType (".jpg") = Bitmap:
            if (!sizeType.HasValue && nameSizeType != ResourceImageSizeType.None) sizeType = nameSizeType;
            if (!contentType.HasValue && nameContentType != ResourceContentType.None) contentType = nameContentType;

            // Vyhledáme odpovídající zdroj:
            if (!pack.TryGetItem(contentType, sizeType, out resourceItem)) return false;
            return true;
        }
        #endregion
        #region class ResourcePack
        /// <summary>
        /// Balíček několika variantních zdrojů jednoho typu (odlišují se velikostí, typem a vhodností pro Dark/Light skin)
        /// </summary>
        public class ResourcePack
        {
            /// <summary>
            /// Konstruktor pro daný klíč = jméno souboru s relativním adresářem, bez značky velikosti a bez přípony
            /// </summary>
            /// <param name="packKey"></param>
            public ResourcePack(string packKey)
            {
                _PackKey = packKey;
                _ResourceItems = new List<ResourceItem>();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"PackKey: {PackKey}; Items: {Count}";
            }
            /// <summary>Klíč balíčku</summary>
            private string _PackKey;
            /// <summary>Jednotlivé prvky</summary>
            private List<ResourceItem> _ResourceItems;
            /// <summary>
            /// Klíč: obsahuje jméno adresáře a souboru, ale bez označení velikosti a bez přípony
            /// </summary>
            public string PackKey { get { return _PackKey; } }
            /// <summary>
            /// Počet jednotlivých položek v evidenci = jednotlivé soubory
            /// </summary>
            public int Count { get { return this._ResourceItems.Count; } }
            /// <summary>
            /// Jednotlivé prvky, různých velikostí a typů
            /// </summary>
            public IEnumerable<ResourceItem> ResourceItems { get { return this._ResourceItems; } }
            /// <summary>
            /// Přidá dodaný prvek do zdejší kolekce zdrojů <see cref="ResourceItems"/>
            /// </summary>
            /// <param name="item"></param>
            public void AddItem(ResourceItem item)
            {
                if (item != null)
                    _ResourceItems.Add(item);
            }
            /// <summary>
            /// Vyhledá konkrétní zdroj odpovídající zadání
            /// </summary>
            /// <param name="contentType">Typ obsahu. Pokud je zadán, budou prohledány jen prvky daného typu. Pokud nebude zadán, prohledají se jakékoli prvky.</param>
            /// <param name="sizeType">Hledaný typ velikosti. Pro zadání Large může najít i Middle, pro zadání Small může najít Middle, pro zadání Middle i vrátí Small nebo Large.</param>
            /// <param name="resourceItem"></param>
            /// <returns></returns>
            public bool TryGetItem(ResourceContentType? contentType, ResourceImageSizeType? sizeType, out ResourceItem resourceItem)
            {
                resourceItem = null;
                if (this.Count == 0) return false;

                // Pracovní soupis odpovídající požadovanému typu obsahu / nebo všechny prvky:
                var items = (contentType.HasValue ? this._ResourceItems.Where(i => i.ContentType == contentType.Value).ToArray() : this._ResourceItems.ToArray());

                return TryGetOptimalSize(items, sizeType, out resourceItem);
            }
            /// <summary>
            /// Metoda zkusí najít nejvhodnější jeden zdroj pro zadanou velikost.
            /// </summary>
            /// <param name="resourceItems"></param>
            /// <param name="sizeType"></param>
            /// <param name="resourceItem"></param>
            /// <returns></returns>
            internal static bool TryGetOptimalSize(ResourceItem[] resourceItems, ResourceImageSizeType? sizeType, out ResourceItem resourceItem)
            {
                resourceItem = null;
                
                // Pokud na vstupu nejsou žádné zdroje, skončíme:
                int count = resourceItems?.Length ?? 0;
                if (count == 0) return false;

                // Pokud na vstupu je právě jediný zdroj, pak je to ten pravý:
                if (count == 1) { resourceItem = resourceItems[0]; return true; }

                // Máme na výběr z více zdrojů - zkusíme najít optimální velikost, podle zadání (bez zadání = Large):
                ResourceImageSizeType size = sizeType ?? ResourceImageSizeType.Large;
                switch (size)
                {   // Vyhledáme zdroj nejprve v požadované velikosti, a pak ve velikostech náhradních:
                    case ResourceImageSizeType.Small:
                        return _TryGetItemBySize(resourceItems, out resourceItem, size, ResourceImageSizeType.Medium, ResourceImageSizeType.Large, ResourceImageSizeType.None);
                    case ResourceImageSizeType.Medium:
                        return _TryGetItemBySize(resourceItems, out resourceItem, size, ResourceImageSizeType.Large, ResourceImageSizeType.Small, ResourceImageSizeType.None);
                    case ResourceImageSizeType.Large:
                    default:
                        return _TryGetItemBySize(resourceItems, out resourceItem, size, ResourceImageSizeType.Medium, ResourceImageSizeType.Small, ResourceImageSizeType.None);
                }
            }
            /// <summary>
            /// Najde první prvek ve velikosti dle pole <paramref name="sizes"/>, hledá jednotlivé velikosti v zadaném pořadí.
            /// Pokud vůbec nic nenajde, i když nějaký prvek má, tak vrátí první prvek v poli.
            /// Vrací true.
            /// </summary>
            /// <param name="items"></param>
            /// <param name="item"></param>
            /// <param name="sizes"></param>
            /// <returns></returns>
            private static bool _TryGetItemBySize(ResourceItem[] items, out ResourceItem item, params ResourceImageSizeType[] sizes)
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
            /// <summary>Podkladová data</summary>
            private IResourceItem _IResourceItem;
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"ItemKey: {ItemKey}; SizeType: {SizeType}; ContentType: {ContentType}";
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
