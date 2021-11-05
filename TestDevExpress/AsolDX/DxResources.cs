using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using DevExpress.Data.Async;
using DevExpress.XtraEditors;
using DevExpress.Utils.Svg;
using DevExpress.Utils;
using System.Runtime.Remoting.Messaging;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Pokračování třídy <see cref="DxComponent"/>, zaměřené na obrázky
    /// </summary>
    partial class DxComponent
    {
        #region Standardní jména obrázků
        /// <summary>
        /// Jméno ikony formuláře
        /// </summary>
        public static string ImageNameFormIcon { get { return Instance._ImageNameFormIcon; } set { Instance._ImageNameFormIcon = value; } }
        /// <summary>
        /// Inicializace výchozích názvů obrázků
        /// </summary>
        private void _ImageInit()
        {
            _ImageNameFormIcon = "svgimages/business%20objects/bo_appearance.svg";
        }
        private string _ImageNameFormIcon;
        #endregion



        #region ImageResource, obecně aplikování obrázků do Controlů - obrázky Aplikační i DevExpress
        #region CreateImage - Získání new instance fyzického obrázku
        /// <summary>
        /// Vygeneruje a vrátí nový obrázek daného jména.
        /// Volitelné parametry upřesňují velikost a upřesňují proces renderování pro případ, kdy je nalezena vektorová ikona a má být vyrenderována bitmapa.
        /// <para/>
        /// Tato metoda generuje vždy new Image, je dobré jej použít v using patternu.
        /// Optimálnější je použít ImageList a získat index, nebo použít metodu GetImage(), která používá předpřipravené obrázky.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static Image CreateImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null)
        { return Instance._CreateImage(imageName, sizeType, optimalSvgSize, svgPalette, svgState, caption); }
        /// <summary>
        /// Vygeneruje a vrátí nový obrázek daného jména.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Image _CreateImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null)
        {
            bool hasName = !String.IsNullOrEmpty(imageName);
            bool hasCaption = !String.IsNullOrEmpty(caption);

            if (hasName && _ExistsDevExpressResource(imageName))
                return _CreateImageDevExpress(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
            else if (hasName && _TrySearchApplicationResource(imageName, out var validItems, ResourceContentType.Bitmap, ResourceContentType.Vector))
                return _CreateImageApplication(validItems, sizeType, optimalSvgSize, svgPalette, svgState);
            else if (hasCaption)
                return SystemAdapter.CreateCaptionImage(caption, sizeType ?? ResourceImageSizeType.Large);
            return null;
        }
        /// <summary>
        /// Vrátí pixelovou velikost odpovídající dané typové velikost, započítá aktuální Zoom a cílové DPI
        /// </summary>
        /// <param name="sizeType"></param>
        /// <param name="useZoom"></param>
        /// <param name="targetDpi"></param>
        /// <returns></returns>
        public static Size GetImageSize(ResourceImageSizeType? sizeType, bool useZoom = true, int? targetDpi = null)
        {
            if (!sizeType.HasValue) sizeType = ResourceImageSizeType.Medium;
            int s = 24;
            switch (sizeType.Value)
            {
                case ResourceImageSizeType.Small:
                    s = 16;
                    break;
                case ResourceImageSizeType.Large:
                    s = 32;
                    break;
                default:
                case ResourceImageSizeType.Medium:
                    s = 24;
                    break;
            }
            if (useZoom)
                s = (targetDpi.HasValue ? ZoomToGui(s, targetDpi.Value) : ZoomToGui(s));
            return new Size(s, s);
        }
        #endregion
        #region ImageList : GetImageList, GetImageListIndex, GetImage
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/> obsahující obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static ImageList GetImageList(ResourceImageSizeType sizeType)
        { return Instance._GetImageList(sizeType); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static int GetImageListIndex(string imageName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null)
        { return Instance._GetImageListIndex(imageName, sizeType, optimalSvgSize, svgPalette, svgState, caption); }
        /// <summary>
        /// Najde nebo vytvoří a uloží a vrátí nový obrázek daného jména.
        /// Volitelné parametry upřesňují velikost a upřesňují proces renderování pro případ, kdy je nalezena vektorová ikona a má být vyrenderována bitmapa.
        /// <para/>
        /// Tato metoda používá cache obrázků, vrácený obrázek se nikdy nesmí disposovat.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static Image GetImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null)
        { return Instance._GetImage(imageName, sizeType ?? ResourceImageSizeType.Large, optimalSvgSize, svgPalette, svgState, caption); }
        /// <summary>
        /// Vrátí text pro danou caption pro renderování ikony.
        /// Z textu odstraní mezery a znaky - _ + / * #
        /// Pokud výsledek bude delší než 2 znaky, zkrátí jej na dva znaky.
        /// Pokud na vstupu je null, na výstupu je prázdný string (Length = 0). Stejně tak, pokud na vstupu bude string obsahující jen odstraněný balast.
        /// </summary>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static string GetCaptionForIcon(string caption)
        {
            if (String.IsNullOrEmpty(caption)) return "";
            string text = caption.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace("+", "")
                .Replace("/", "")
                .Replace("*", "")
                .Replace("#", "");
            if (text.Length > 2) text = text.Substring(0, 2);
            return text;
        }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/> obsahující obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private ImageList _GetImageList(ResourceImageSizeType sizeType)
        {
            System.Windows.Forms.ImageList imageList;
            if (__ImageListDict == null) __ImageListDict = new Dictionary<ResourceImageSizeType, ImageList>();   // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var imageListDict = __ImageListDict;
            if (!imageListDict.TryGetValue(sizeType, out imageList))
            {
                lock (imageListDict)
                {
                    if (!imageListDict.TryGetValue(sizeType, out imageList))
                    {
                        imageList = new System.Windows.Forms.ImageList();
                        imageListDict.Add(sizeType, imageList);
                    }
                }
            }
            return imageList;
        }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetImageListIndex(string imageName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            var imageInfo = _GetImageListItem(imageName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
            return imageInfo?.Item2 ?? -1;
        }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Image _GetImage(string imageName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            var imageInfo = _GetImageListItem(imageName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
            return ((imageInfo == null || imageInfo.Item1 == null || imageInfo.Item2 < 0) ? null : imageInfo.Item1.Images[imageInfo.Item2]);
        }
        /// <summary>
        /// Metoda vyhledá, zda daný obrázek existuje
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Tuple<ImageList, int> _GetImageListItem(string imageName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            bool hasName = !String.IsNullOrEmpty(imageName);
            string captionKey = DxComponent.GetCaptionForIcon(caption);
            bool hasCaption = (captionKey.Length > 0);
            string key = (hasName ? imageName : hasCaption ? $"«:{captionKey}:»" : "").Trim().ToLower();
            if (key.Length == 0) return null;

            ImageList imageList = _GetImageList(sizeType);
            int index = -1;
            if (imageList.Images.ContainsKey(key))
            {
                index = imageList.Images.IndexOfKey(key);
            }
            else if (hasName || hasCaption)
            {
                Image image = _CreateImage(imageName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
                if (image != null)
                {
                    imageList.Images.Add(key, image);
                    index = imageList.Images.IndexOfKey(key);
                }
            }
            return (index >= 0 ? new Tuple<ImageList, int>(imageList, index) : null);
        }
        /// <summary>
        /// Dictionary ImageListů
        /// </summary>
        private Dictionary<ResourceImageSizeType, System.Windows.Forms.ImageList> __ImageListDict;
        #endregion
        #region Soupisy zdrojů
        /// <summary>
        /// Metoda vrátí pole obsahující jména všech zdrojů [volitelně s danou příponou].
        /// Pouze zdroje aplikační, nikoliv DevExpress.
        /// </summary>
        /// <param name="extension">Přípona, vybírat jen záznamy s touto příponou. Měla by začínat tečkou: ".svg"</param>
        /// <param name="withApplication">Zařadit zdroje aplikace</param>
        /// <param name="withDevExpress">Zařadit zdroje DevExpress</param>
        /// <returns></returns>
        public static string[] GetResourceNames(string extension = null, bool withApplication = true, bool withDevExpress = true) { return Instance._GetResourceNames(extension, withApplication, withDevExpress); }
        private string[] _GetResourceNames(string extension, bool withApplication, bool withDevExpress)
        {
            List<string> names = new List<string>();
            if (withApplication) names.AddRange(_GetApplicationResourceNames(extension));
            if (withDevExpress) names.AddRange(_GetDevExpressResourceNames(extension));
            names.Sort();
            return names.ToArray();
        }
        /// <summary>
        /// Metoda najde daný zdroj a zkusí určit jeho typ obsahu (Bitmap nebo Vector). Může preferovat Vector.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="preferVector"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static bool TryGetResourceContentType(string imageName, ResourceImageSizeType sizeType, bool preferVector, out ResourceContentType contentType)
        { return Instance._TryGetResourceContentType(imageName, sizeType, preferVector, out contentType); }
        private bool _TryGetResourceContentType(string imageName, ResourceImageSizeType sizeType, bool preferVector, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(imageName)) return false;

            if (_ExistsDevExpressResource(imageName))
                return _TryGetContentTypeDevExpress(imageName, out contentType);
            else if (_TrySearchApplicationResource(imageName, out var validItems))
                return _TryGetContentTypeApplication(validItems, sizeType, preferVector, out contentType);

            return false;
        }
        /// <summary>
        /// Vrátí typ obsahu podle přípony
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static ResourceContentType GetContentTypeFromExtension(string extension)
        {
            if (String.IsNullOrEmpty(extension)) return ResourceContentType.None;
            extension = extension.Trim().ToLower();
            if (!extension.StartsWith(".")) extension = "." + extension;
            switch (extension)
            {
                case ".bmp":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".pcx":
                case ".tif":
                case ".tiff":
                    return ResourceContentType.Bitmap;
                case ".svg":
                    return ResourceContentType.Vector;
                case ".mp4":
                case ".mpg":
                case ".mpeg":
                case ".avi":
                    return ResourceContentType.Video;
                case ".wav":
                case ".flac":
                case ".mp3":
                case ".mpc":
                    return ResourceContentType.Audio;
                case ".ico":
                    return ResourceContentType.Icon;
                case ".cur":
                    return ResourceContentType.Cursor;
                case ".htm":
                case ".html":
                case ".xml":
                    return ResourceContentType.Xml;
            }
            return ResourceContentType.None;
        }
        #endregion
        #region SvgImage
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public static DevExpress.Utils.Svg.SvgImage GetSvgImage(string imageName)
        { return Instance._GetSvgImage(imageName); }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetSvgImage(string imageName)
        {
            if (String.IsNullOrEmpty(imageName)) return null;

            if (_ExistsDevExpressResource(imageName) && _IsImageNameSvg(imageName))
                return _GetSvgImageDevExpress(imageName);
            else if (_TrySearchApplicationResource(imageName, out var validItems, ResourceContentType.Vector))
                return _GetSvgImageApplication(validItems);
            return null;
        }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetSvgImageDevExpress(string imageName)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            _RewindDevExpressResourceStream(resourceName);
            return _DevExpressResourceCache.GetSvgImage(resourceName);
        }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v Aplikačních zdrojích
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetSvgImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, ResourceImageSizeType.Large, out var resourceItem))
                return null;
            return resourceItem.CreateSvgImage();
        }
        #endregion
        #region Přístup na DevExpress zdroje
        /// <summary>
        /// Vrací seznam DevExpress resources - jména zdrojů
        /// </summary>
        /// <param name="extension">Přípona, vybírat jen záznamy s touto příponou. Měla by začínat tečkou: ".svg"</param>
        /// <returns></returns>
        private string[] _GetDevExpressResourceNames(string extension)
        {
            var ext = (String.IsNullOrEmpty(extension) ? null : extension.Trim().ToLower());
            var items = _DevExpressResourceCache.GetAllResourceKeys();
            if (ext != null) items = items.Where(k => k.EndsWith(ext)).ToArray();
            return items;
        }
        /// <summary>
        /// Vrátí SVG paletu [volitelně pro daný skin a pro daný stav objektu], defaultně pro aktuální skin
        /// </summary>
        /// <param name="skinProvider">Cílový skin, implicitně bude použit <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/></param>
        /// <param name="svgState">Stav objektu, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static DevExpress.Utils.Design.ISvgPaletteProvider GetSvgPalette(DevExpress.Skins.ISkinProvider skinProvider = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (skinProvider == null) skinProvider = DevExpress.LookAndFeel.UserLookAndFeel.Default;
            if (!svgState.HasValue) svgState = DevExpress.Utils.Drawing.ObjectState.Normal;
            return DevExpress.Utils.Svg.SvgPaletteHelper.GetSvgPalette(skinProvider, svgState.Value);
        }
        /// <summary>
        /// Vrátí true, pokud pro dané jméno existuje DevExpress zdroj (ikona png nebo svg)
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private bool _ExistsDevExpressResource(string resourceName)
        {
            if (String.IsNullOrEmpty(resourceName)) return false;
            var key = _GetDevExpressResourceKey(resourceName);
            var dictionary = _DevExpressResourceDictionary;
            return dictionary.TryGetValue(key, out var _);
        }
        /// <summary>
        /// Určí typ obsahu daného DevExpress zdroje
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private bool _TryGetContentTypeDevExpress(string resourceName, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(resourceName)) return false;
            var key = _GetDevExpressResourceKey(resourceName);
            var dictionary = _DevExpressResourceDictionary;
            bool exists = dictionary.TryGetValue(key, out var extension);
            if (exists)
                contentType = GetContentTypeFromExtension(extension);
            return (contentType != ResourceContentType.None);
        }
        /// <summary>
        /// Vrátí bitmapu z DevExpress zdrojů, ať už tam je jako Png nebo Svg (Svg vyrenderuje)
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageDevExpress(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (_IsImageNameSvg(imageName))
                return _CreateImageDevExpressSvg(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
            else
                return _CreateImageDevExpressPng(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí bitmapu uloženou v DevExpress zdrojích.
        /// Tato metoda již netestuje existenci zdroje, to má provést někdo před voláním této metody.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageDevExpressPng(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            return _DevExpressResourceCache.GetImage(resourceName);
        }
        /// <summary>
        /// Vrátí bitmapu z obrázku typu SVG uloženou v DevExpress zdrojích.
        /// Tato metoda již netestuje existenci zdroje, to má provést někdo před voláním této metody.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageDevExpressSvg(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            Size size = optimalSvgSize ?? GetImageSize(sizeType);

            if (svgPalette == null)
                svgPalette = GetSvgPalette(DevExpress.LookAndFeel.UserLookAndFeel.Default, svgState);
            _RewindDevExpressResourceStream(resourceName);
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(_DevExpressResourceCache.GetSvgImage(resourceName), size, svgPalette);
            else
                return _DevExpressResourceCache.GetSvgImage(resourceName, svgPalette, size);
        }
        /// <summary>
        /// Vrátí klíč pro daný DevExpress zdroj
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private static string _GetDevExpressResourceKey(string imageName) { return (imageName == null ? "" : imageName.Trim().ToLower()); }
        /// <summary>
        /// Vrátí true, pokud dané jméno zdroje končí příponou ".svg"
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private static bool _IsImageNameSvg(string resourceName)
        {
            return (!String.IsNullOrEmpty(resourceName) && resourceName.TrimEnd().EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Prefix pro ImageUri: "image://"
        /// </summary>
        private static string ImageUriPrefix { get { return "image://"; } }
        /// <summary>
        /// Zkusí najít daný zdroj v <see cref="_DevExpressResourceDictionary"/> (seznam DevExpress zdrojů = ikon) a určit jeho příponu. Vrací true = nalezeno.
        /// Přípona je trim(), lower() a bez tečky na začátku, například: "png", "svg" atd.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        private bool _TryGetDevExpressResourceExtension(string resourceName, out string extension)
        {
            extension = null;
            if (String.IsNullOrEmpty(resourceName)) return false;
            var key = _GetDevExpressResourceKey(resourceName);
            var dictionary = _DevExpressResourceDictionary;
            return dictionary.TryGetValue(key, out extension);
        }
        /// <summary>
        /// Napravuje chybu DevExpress, kdy v <see cref="DevExpress.Images.ImageResourceCache"/> pro SVG zdroje po jejich použití je jejich zdrojový stream na konci, a další použití je tak znemožněno.
        /// </summary>
        /// <param name="resourceName"></param>
        private void _RewindDevExpressResourceStream(string resourceName)
        {
            if (String.IsNullOrEmpty(resourceName)) return;

            var imageResourceCache = _DevExpressResourceCache;
            var dictonaryField = imageResourceCache.GetType().GetField("resources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dictonaryField == null) return;
            object dictonaryValue = dictonaryField.GetValue(imageResourceCache);

            if (!(dictonaryValue is Dictionary<string, System.IO.Stream> dictionary)) return;
            if (!dictionary.TryGetValue(resourceName, out System.IO.Stream stream)) return;

            var position = stream.Position;
            if (stream.Position > 0L && stream.CanSeek)
                stream.Seek(0L, System.IO.SeekOrigin.Begin);
        }
        /// <summary>
        /// Dictionary obsahující všechny DevExpress zdroje (jako Key) 
        /// a jejich normalizovanou příponu (jako Value) ve formě "png", "svg" atd (bez tečky, lower, trim)
        /// </summary>
        private Dictionary<string, string> _DevExpressResourceDictionary
        {
            get
            {
                if (__DevExpressResourceDictionary == null)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    var names = _DevExpressResourceCache.GetAllResourceKeys();
                    foreach (var name in names)
                    {
                        if (!String.IsNullOrEmpty(name))
                        {
                            string key = name.Trim().ToLower();
                            if (!dict.ContainsKey(key))
                            {
                                string ext = System.IO.Path.GetExtension(key).Trim();
                                if (ext.Length > 0 && ext[0] == '.') ext = ext.Substring(1);
                                dict.Add(key, ext);
                            }
                        }
                    }
                    __DevExpressResourceDictionary = dict;
                }
                return __DevExpressResourceDictionary;
            }
        }
        private Dictionary<string, string> __DevExpressResourceDictionary;
        /// <summary>
        /// Cache DevExpress image resources
        /// </summary>
        private DevExpress.Images.ImageResourceCache _DevExpressResourceCache
        {
            get
            {
                if (__DevExpressResourceCache == null)
                    __DevExpressResourceCache = DevExpress.Images.ImageResourceCache.Default;
                return __DevExpressResourceCache;
            }
        }
        private DevExpress.Images.ImageResourceCache __DevExpressResourceCache;
        #endregion
        #region Přístup na Aplikační zdroje (přes AsolDX.DxResourceLibrary, s pomocí SystemAdapter)
        /// <summary>
        /// Existuje daný zdroj v aplikačních zdrojích?
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private bool _ExistsApplicationResource(string resourceName)
        {
            return DxApplicationResourceLibrary.TryGetResource(resourceName, out var resourceItem, out var resourcePack);
        }
        /// <summary>
        /// Určí typ obsahu daného Aplikačního zdroje
        /// </summary>
        /// <param name="validItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="preferVector"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private bool _TryGetContentTypeApplication(DxApplicationResourceLibrary.ResourceItem[] validItems, ResourceImageSizeType sizeType, bool preferVector, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (validItems == null || validItems.Length == 0) return false;
            if (preferVector && validItems.Any(i => i.ContentType == ResourceContentType.Vector))
                contentType = ResourceContentType.Vector;
            else if (validItems.Any(i => i.ContentType == ResourceContentType.Bitmap))
                contentType = ResourceContentType.Bitmap;
            else if (!preferVector && validItems.Any(i => i.ContentType == ResourceContentType.Vector))
                contentType = ResourceContentType.Vector;
            return (contentType != ResourceContentType.None);
        }
        /// <summary>
        /// Vrací seznam Aplikačních resources - jména zdrojů
        /// </summary>
        /// <param name="extension">Přípona, vybírat jen záznamy s touto příponou. Měla by začínat tečkou: ".svg"</param>
        /// <returns></returns>
        private string[] _GetApplicationResourceNames(string extension)
        {
            return DxApplicationResourceLibrary.GetResourceNames(extension);
        }
        /// <summary>
        /// Zkusí najít daný zdroj v aplikačních zdrojích, zafiltruje na daný typ obsahu - typ obsahu je povinný. Velikost se řeší následně.
        /// <para/>
        /// Na vstupu je params pole vhodných typů obrázku, prochází se prioritně v daném pořadí a vrací se první existující sada zdrojů daného typu.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="validItems"></param>
        /// <param name="validContentTypes"></param>
        /// <returns></returns>
        private bool _TrySearchApplicationResource(string resourceName, out DxApplicationResourceLibrary.ResourceItem[] validItems,
            params ResourceContentType[] validContentTypes)
        {
            validItems = null;
            if (String.IsNullOrEmpty(resourceName)) return false;
            if (!DxApplicationResourceLibrary.TryGetResource(resourceName, out var resourceItem, out var resourcePack)) return false;

            bool hasValidTypes = (validContentTypes != null && validContentTypes.Length > 0);

            // Pokud jsem našel jeden konkrétní zdroj (je zadané explicitní jméno, a to bylo nalezeno):
            if (resourceItem != null)
            {   // Pokud není specifikován konkrétní typ obsahu, anebo nějaké typy specifikované jsou a nalezený zdroj je některého zadaného typu,
                //  pak nalezenou položku jako jedinou vložíme do out pole vyhovujících zdrojů:
                if (!hasValidTypes || validContentTypes.Any(t => resourceItem.ContentType == t))
                    validItems = new DxApplicationResourceLibrary.ResourceItem[] { resourceItem };
            }

            // Pokud jsem našel celý ResourcePack, pak z něj vyberu jen ty zdroje, které vyhovují prvnímu zadanému typu obsahu (pokud není požadavek na typoy, vezmu vše):
            else if (resourcePack != null)
            {
                if (!hasValidTypes)
                    // Není požadavek na konkrétní typ obsahu:
                    validItems = resourcePack.ResourceItems.ToArray();
                else
                {   // Chceme najít typicky typ obsahu Bitmapa, anebo když není tak Vector:
                    foreach (var validContentType in validContentTypes)
                    {
                        validItems = resourcePack.ResourceItems.Where(i => i.ContentType == validContentType).ToArray();
                        if (validItems.Length > 0) break;
                        validItems = null;
                    }
                }
            }
            return (validItems != null && validItems.Length > 0);
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů.
        /// Na vstupu (<paramref name="resourceName"/>) nemusí být uvedena přípona, může být uvedeno obecné jméno, např. "pic\address-book-undo-2";
        /// a až knihovna zdrojů sama najde reálné obrázky: "pic\address-book-undo-2-large.svg" anebo "pic\address-book-undo-2-small.svg".
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageApplication(string resourceName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (!_TrySearchApplicationResource(resourceName, out var validItems, ResourceContentType.Bitmap, ResourceContentType.Vector)) return null;
            return _CreateImageApplication(validItems, sizeType, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů.
        /// Na vstupu (<paramref name="resourceItems"/>) je seznam zdrojů, z nich bude vybrán zdroj vhodné velikosti.
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            // Vezmu jediný zdroj anebo vyhledám optimální zdroj pro danou velikost:
            if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, sizeType, out var resourceItem))
            {
                switch (resourceItem.ContentType)
                {
                    case ResourceContentType.Bitmap:
                        return resourceItem.CreateBmpImage();
                    case ResourceContentType.Vector:
                        return _RenderSvgImageToImage(resourceItem.CreateSvgImage(), sizeType, optimalSvgSize, svgPalette, svgState);
                }
            }
            return null;
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do bitmapy
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _RenderSvgImageToImage(SvgImage svgImage,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (svgImage is null) return null;
            var imageSize = optimalSvgSize ?? GetImageSize(sizeType);
            if (svgPalette == null)
                svgPalette = DxComponent.GetSvgPalette(null, svgState);
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(svgImage, imageSize, svgPalette);
            return svgImage.Render(imageSize, svgPalette);
        }
        #endregion



        // probrat :

        /// <summary>
        /// Zkusí najít daný zdroj v <see cref="_DevExpressResourceDictionary"/> (seznam systémových zdrojů = ikon) a určit jeho příponu. Vrací true = nalezeno.
        /// Přípona je trim(), lower() a bez tečky na začátku, například: "png", "svg" atd.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static bool TryGetResourceExtension(string resourceName, out string extension) { return Instance._TryGetDevExpressResourceExtension(resourceName, out extension); }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje</param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName,
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return GetImageFromResource(resourceName, out Size size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje</param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName, out Size size,
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return Instance._GetImageFromResource(resourceName, out size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje</param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        private Image _GetImageFromResource(string resourceName, out Size size,
            Size? maxSize, Size? optimalSvgSize, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
        {
            System.Drawing.Image image = null;
            size = new Size(32, 32);
            if (String.IsNullOrEmpty(resourceName)) return null;

            try
            {
                if (_IsImageNameSvg(resourceName))
                {
                    if (svgPalette == null)
                        svgPalette = GetSvgPalette(DevExpress.LookAndFeel.UserLookAndFeel.Default, svgState);
                    if (optimalSvgSize.HasValue)
                        size = optimalSvgSize.Value;
                    else if (maxSize.HasValue)
                        size = maxSize.Value;
                    _RewindDevExpressResourceStream(resourceName);
                    image = _DevExpressResourceCache.GetSvgImage(resourceName, svgPalette, size);
                }
                else
                {
                    image = _DevExpressResourceCache.GetImage(resourceName);
                    size = image?.Size ?? Size.Empty;
                    if (maxSize.HasValue)
                    {
                        if (maxSize.Value.Width > 0 && size.Width > maxSize.Value.Width) size.Width = maxSize.Value.Width;
                        if (maxSize.Value.Height > 0 && size.Height > maxSize.Value.Height) size.Height = maxSize.Value.Height;
                    }
                }
            }
            catch (Exception exc)
            {
                image = null;
            }

            return image;
        }
        public static DevExpress.Utils.SvgImageCollection SvgImageCollection { get { return Instance._SvgImageCollection; } }
        public static void ApplyImage(ImageOptions imageOptions, string resourceName = null, Image image = null, Size? imageSize = null, bool smallButton = false)
        { Instance._ApplyImage(imageOptions, resourceName, image, imageSize, smallButton); }
        private void _ApplyImage(ImageOptions imageOptions, string resourceName, Image image, Size? imageSize, bool smallButton)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }

            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_TryGetDevExpressResourceExtension(resourceName, out string extension))
                    {   // Interní DevExpress Resources:
                        if (extension == "svg")
                            _ApplyResourceImageSvg(imageOptions, resourceName, imageSize);
                        else
                            _ApplyResourceImageBmp(imageOptions, resourceName, imageSize);
                    }
                    else
                    {   // Externí zdroje:
                        imageOptions.Image = DxComponent.GetImage(resourceName, ResourceImageSizeType.Medium);

#warning POKRAČUJ !!!
                        // qqq;


                    }
                }
                catch { }
            }
            else
            {
                imageOptions.SvgImage = null;
                imageOptions.Image = null;
            }

            if (smallButton && imageOptions is SimpleButtonImageOptions buttonImageOptions)
            {
                buttonImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            }
        }
        private void _ApplyResourceImageSvg(ImageOptions imageOptions, string resourceName, Size? imageSize)
        {
            imageOptions.Image = null;
            _RewindDevExpressResourceStream(resourceName);
            imageOptions.SvgImage = _DevExpressResourceCache.GetSvgImage(resourceName);
            if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
        }
        private void _ApplyResourceImageBmp(ImageOptions imageOptions, string resourceName, Size? imageSize)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _DevExpressResourceCache.GetImage(resourceName);
        }



        public static void xxxApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false)
        { Instance._ApplyImage(imageOptions, image, resourceName, imageSize, smallButton); }
        private void _ApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }
            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        _RewindDevExpressResourceStream(resourceName);
                        imageOptions.SvgImage = _DevExpressResourceCache.GetSvgImage(resourceName);
                        if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
                    }
                    else
                    {
                        imageOptions.Image = _DevExpressResourceCache.GetImage(resourceName);
                    }
                }
                catch { }
            }
            if (smallButton)
            {
                imageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
                //                imageOptions.ImageToTextIndent = 0;
                //              imageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.BottomCenter;
            }
        }

        #region SvgCollection
        private SvgImage _GetSvgFromCollection(string imageName)
        {
            if (!imageName.StartsWith(ImageUriPrefix, StringComparison.OrdinalIgnoreCase)) imageName = ImageUriPrefix + imageName;
            var svgImageCollection = _SvgImageCollection;
            if (!svgImageCollection.ContainsKey(imageName))
                svgImageCollection.Add(imageName, imageName);
            return svgImageCollection[imageName];
        }
        private DevExpress.Utils.SvgImageCollection _SvgImageCollection
        {
            get
            {
                if (__SvgImageCollection == null)
                    __SvgImageCollection = new SvgImageCollection();
                return __SvgImageCollection;
            }
        }
        private DevExpress.Utils.SvgImageCollection __SvgImageCollection;
        #endregion
        #endregion


    }
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
        /// Metoda vrátí pole obsahující jména všech zdrojů [volitelně s danou příponou].
        /// Pouze zdroje aplikační, nikoliv DevExpress.
        /// </summary>
        /// <param name="extension">Přípona, vybírat jen záznamy s touto příponou. Měla by začínat tečkou: ".svg"</param>
        /// <returns></returns>
        public static string[] GetResourceNames(string extension = null)
        {
            var ext = (String.IsNullOrEmpty(extension) ? null : extension.Trim().ToLower());
            var items = Current.__ItemDict.Keys;
            return (ext == null) ? items.ToArray() : items.Where(k => k.EndsWith(ext)).ToArray();
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
