using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using System.Xml;

using DevExpress.Data.Async;
using DevExpress.XtraEditors;
using DevExpress.Utils.Svg;
using DevExpress.Utils;

using Noris.WS.DataContracts.Desktop.Data;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Pokračování třídy <see cref="DxComponent"/>, zaměřené na obrázky
    /// </summary>
    partial class DxComponent
    {
        #region CreateBitmapImage - Získání new instance bitmapového obrázku;
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
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static Image CreateBitmapImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null, bool exactName = false)
        { return Instance._CreateBitmapImage(imageName, exactName, sizeType, optimalSvgSize, svgPalette, svgState, caption); }
        /// <summary>
        /// Vygeneruje a vrátí nový obrázek daného jména.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Image _CreateBitmapImage(string imageName, bool exactName,
            ResourceImageSizeType? sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            bool hasName = !String.IsNullOrEmpty(imageName);
            bool hasCaption = !String.IsNullOrEmpty(caption);

            if (hasName && _TrySearchApplicationResource(imageName, exactName, out var validItems, ResourceContentType.Bitmap, ResourceContentType.Vector))
                return _CreateImageApplication(validItems, sizeType, optimalSvgSize, svgPalette, svgState);
            if (hasName && _ExistsDevExpressResource(imageName))
                return _CreateBitmapImageDevExpress(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
            if (hasCaption)
                return _CreateBitmapImageForCaption(caption, sizeType, null);

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
        #region CreateVectorImage - Získání new instance vektorového obrázku, GetVectorImage - Rychlé získání instance vektorového obrázku
        /// <summary>
        /// Najde a vrátí new <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static SvgImage CreateVectorImage(string imageName, bool exactName = false, ResourceImageSizeType? sizeType = null, string caption = null)
        { return Instance._CreateVectorImage(imageName, exactName, sizeType, caption); }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static SvgImage GetVectorImage(string imageName, bool exactName = false, ResourceImageSizeType? sizeType = null, string caption = null)
        { return Instance._GetVectorImage(imageName, exactName, sizeType, caption); }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private SvgImage _CreateVectorImage(string imageName, bool exactName, ResourceImageSizeType? sizeType, string caption = null)
        {
            return _GetVectorImage(imageName, exactName, sizeType, caption);
        }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImage(string imageName, bool exactName, ResourceImageSizeType? sizeType, string caption)
        {
            if (String.IsNullOrEmpty(imageName)) return null;

            if (SvgImageArraySupport.TryGetSvgImageArray(imageName, out var svgImageArray))
                return _GetVectorImageArray(svgImageArray, sizeType);
            if (_TrySearchApplicationResource(imageName, exactName, out var validItems, ResourceContentType.Vector))
                return _GetVectorImageApplication(validItems, sizeType);
            if (_ExistsDevExpressResource(imageName) && _IsImageNameSvg(imageName))
                return _GetVectorImageDevExpress(imageName);
            if (!String.IsNullOrEmpty(caption))
                return _CreateVectorImageForCaption(caption, sizeType, null);
            return null;
        }
        #endregion
        #region TryGetResource - hledání aplikačního zdroje
        /// <summary>
        /// Metoda se pokusí najít zdroj v Aplikačních zdrojích, pro dané jméno.
        /// Prohledává obrázky vektorové a bitmapové, může preferovat bitmapy pokud <paramref name="preferBitmap"/> je true.
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="resourceItem">Výstup - nalezeného zdroje</param>
        /// <param name="sizeType">Vyhledat danou velikost, default = Large</param>
        /// <param name="preferBitmap">Preferovat bitmapy, pokdu je dáno true</param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static bool TryGetResource(string imageName, out DxApplicationResourceLibrary.ResourceItem resourceItem, ResourceImageSizeType? sizeType = null, bool preferBitmap = false, bool exactName = false)
        { return Instance._TryGetResource(imageName, exactName, sizeType, preferBitmap, out resourceItem); }
        private bool _TryGetResource(string imageName, bool exactName, ResourceImageSizeType? sizeType, bool preferBitmap, out DxApplicationResourceLibrary.ResourceItem resourceItem)
        {
            resourceItem = null;
            ResourceContentType[] validContentTypes = (preferBitmap ? new ResourceContentType[] { ResourceContentType.Bitmap, ResourceContentType.Vector } : new ResourceContentType[] { ResourceContentType.Vector, ResourceContentType.Bitmap });
            if (!_TrySearchApplicationResource(imageName, exactName, out var validItems, validContentTypes)) return false;
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(validItems, sizeType, out resourceItem)) return false;
            return true;
        }
        #endregion
        #region ApplyImage - do cílového objektu vepíše obrázek podle toho, jak je zadán a kam má být vepsán
        /// <summary>
        /// ApplyImage - do cílového objektu vepíše obrázek podle toho, jak je zadán a kam má být vepsán
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="image"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <param name="smallButton"></param>
        /// <param name="caption"></param>
        /// <param name="exactName"></param>
        public static void ApplyImage(ImageOptions imageOptions, string imageName = null, Image image = null, ResourceImageSizeType? sizeType = null, Size? imageSize = null, bool smallButton = false, string caption = null, bool exactName = false)
        { Instance._ApplyImage(imageOptions, imageName, exactName, image, sizeType, imageSize, smallButton, caption); }
        /// <summary>
        /// ApplyImage - do cílového objektu vepíše obrázek podle toho, jak je zadán a kam má být vepsán
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="image"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <param name="smallButton"></param>
        /// <param name="caption"></param>
        private void _ApplyImage(ImageOptions imageOptions, string imageName, bool exactName, Image image, ResourceImageSizeType? sizeType, Size? imageSize, bool smallButton, string caption = null)
        {
            bool hasImage = (image != null);
            bool hasName = !String.IsNullOrEmpty(imageName);
            bool hasCaption = !String.IsNullOrEmpty(caption);
            if (hasImage)
            {
                if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barItemImageOptions)
                {
                    barItemImageOptions.Image = image;
                    barItemImageOptions.LargeImage = image;
                }
                else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions imageCollectionImageOptions)
                {
                    imageCollectionImageOptions.Image = image;
                }
                else
                {
                    imageOptions.Image = image;
                }
            }
            else if (hasName || hasCaption)
            {
                try
                {
                    // Resource může být Combined (=více SVG obrázků v jedném textu!):
                    if (hasName && SvgImageArraySupport.TryGetSvgImageArray(imageName, out var svgImageArray))
                        _ApplyImageArray(imageOptions, svgImageArray, sizeType, imageSize);
                    else if (hasName && _ExistsApplicationResource(imageName, exactName))
                        _ApplyImageApplication(imageOptions, imageName, exactName, sizeType, imageSize);
                    else if (hasName && _ExistsDevExpressResource(imageName))
                        _ApplyImageDevExpress(imageOptions, imageName, sizeType, imageSize);
                    else if (hasCaption)
                        _ApplyImageForCaption(imageOptions, caption, sizeType, imageSize);
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
        /// <summary>
        /// Aplikuje Image typu Array (ve jménu obrázku je více zdrojů) do daného cíle v <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageArray(ImageOptions imageOptions, SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            if (svgImageArray != null)
                imageOptions.SvgImage = _GetVectorImageArray(svgImageArray, sizeType);
        }
        /// <summary>
        /// Vrátí SVG Image typu Array
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetVectorImageArray(SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType)
        {
            return SvgImageArraySupport.CreateSvgImage(svgImageArray, sizeType);
        }
        /// <summary>
        /// Aplikuje Image typu Vector nebo Bitmap (podle přípony) ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageApplication(ImageOptions imageOptions, string imageName, bool exactName, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            if (!_TrySearchApplicationResource(imageName, exactName, out var validItems, ResourceContentType.Vector, ResourceContentType.Bitmap)) return;

            var contentType = validItems[0].ContentType;
            if (contentType == ResourceContentType.Vector)
                _ApplyImageApplicationSvg(imageOptions, validItems, sizeType, imageSize);
            else if (contentType == ResourceContentType.Bitmap)
                _ApplyImageApplicationBmp(imageOptions, validItems, sizeType, imageSize);
        }
        /// <summary>
        /// Aplikuje Image typu Vector ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageApplicationSvg(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.Image = null;
            imageOptions.SvgImage = _GetVectorImageApplication(resourceItems, sizeType);
            if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
            else if (sizeType.HasValue) imageOptions.SvgImageSize = DxComponent.GetImageSize(sizeType.Value, true);
        }
        /// <summary>
        /// Aplikuje Image typu Bitmap ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageApplicationBmp(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _CreateImageApplication(resourceItems, sizeType, imageSize, null, null);
        }
        /// <summary>
        /// Aplikuje Image typu Vector nebo Bitmap (podle přípony) ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <param name="extension"></param>
        private void _ApplyImageDevExpress(ImageOptions imageOptions, string imageName, ResourceImageSizeType? sizeType, Size? imageSize, string extension = null)
        {
            if (extension == null) extension = _GetDevExpressResourceExtension(imageName);
            if (extension != null)
            {
                if (extension == "svg")
                    _ApplyImageDevExpressSvg(imageOptions, imageName, sizeType, imageSize);
                else
                    _ApplyImageDevExpressBmp(imageOptions, imageName, sizeType, imageSize);
            }
        }
        /// <summary>
        /// Aplikuje Image typu Vector ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageDevExpressSvg(ImageOptions imageOptions, string imageName, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.Image = null;
            imageOptions.SvgImage = _GetVectorImageDevExpress(imageName);             // Na vstupu je jméno Vektoru, dáme jej tedy do SvgImage
            if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
            else if (sizeType.HasValue) imageOptions.SvgImageSize = DxComponent.GetImageSize(sizeType.Value, true);
        }
        /// <summary>
        /// Aplikuje Image typu Bitmap ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageDevExpressBmp(ImageOptions imageOptions, string imageName, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _CreateBitmapImageDevExpressPng(imageName);          // Na vstupu je jméno bitmapy, tedy ji najdeme a dáme do Image. Tady nepřichází do úvahy renderování, velikost, paleta atd...
        }
        #endregion
        #region ImageList - Seznam obrázků typu Bitmapa, pro použití v controlech; GetImageList, GetImageListIndex, GetImage
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/> obsahující obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static ImageList GetImageList(ResourceImageSizeType sizeType)
        { return Instance._GetImageList(sizeType); }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/> obsahující obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private ImageList _GetImageList(ResourceImageSizeType sizeType)
        {
            if (__ImageListDict == null) __ImageListDict = new Dictionary<ResourceImageSizeType, ImageList>();   // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var imageListDict = __ImageListDict;
            if (!imageListDict.TryGetValue(sizeType, out ImageList imageList))
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
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static int GetImageListIndex(string imageName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null, bool exactName = false)
        { return Instance._GetImageListIndex(imageName, exactName, sizeType, optimalSvgSize, svgPalette, svgState, caption); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetImageListIndex(string imageName, bool exactName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            var imageInfo = _GetBitmapImageListItem(imageName, exactName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
            return imageInfo?.Item2 ?? -1;
        }
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
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static Image GetBitmapImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null,
            string caption = null, bool exactName = false)
        { return Instance._GetBitmapImage(imageName, exactName, sizeType ?? ResourceImageSizeType.Large, optimalSvgSize, svgPalette, svgState, caption); }
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
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Image _GetBitmapImage(string imageName, bool exactName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState,
            string caption)
        {
            var imageInfo = _GetBitmapImageListItem(imageName, exactName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
            return ((imageInfo == null || imageInfo.Item1 == null || imageInfo.Item2 < 0) ? null : imageInfo.Item1.Images[imageInfo.Item2]);
        }
        /// <summary>
        /// Metoda vyhledá, zda daný obrázek existuje
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Tuple<ImageList, int> _GetBitmapImageListItem(string imageName, bool exactName,
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
                Image image = _CreateBitmapImage(imageName, exactName, sizeType, optimalSvgSize, svgPalette, svgState, caption);
                if (image != null)
                {
                    imageList.Images.Add(key, image);
                    index = imageList.Images.IndexOfKey(key);
                }
            }
            return (index >= 0 ? new Tuple<ImageList, int>(imageList, index) : null);
        }
        /// <summary>
        /// Dictionary ImageListů - pro každou velikost <see cref="ResourceImageSizeType"/> je jedna instance
        /// </summary>
        private Dictionary<ResourceImageSizeType, System.Windows.Forms.ImageList> __ImageListDict;
        #endregion
        #region SvgImageCollection - Seznam obrázků typu Vector, pro použití v controlech; GetSvgIndex
        /// <summary>
        /// Vrátí kolekci SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje. Pro danou cílovou velikost.
        /// </summary>
        /// <param name="sizeType"></param>
        public static DevExpress.Utils.SvgImageCollection GetSvgImageCollection(ResourceImageSizeType sizeType) { return Instance._GetSvgImageCollection(sizeType); }
        /// <summary>
        /// Vrátí kolekci SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje. Pro danou cílovou velikost.
        /// </summary>
        /// <param name="sizeType"></param>
        private DxSvgImageCollection _GetSvgImageCollection(ResourceImageSizeType sizeType)
        {
            if (__SvgImageCollections == null) __SvgImageCollections = new Dictionary<ResourceImageSizeType, DxSvgImageCollection>();      // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var svgImageCollections = __SvgImageCollections;
            if (!svgImageCollections.TryGetValue(sizeType, out DxSvgImageCollection svgCollection))
            {
                lock (svgImageCollections)
                {
                    if (!svgImageCollections.TryGetValue(sizeType, out svgCollection))
                    {
                        svgCollection = new DxSvgImageCollection(sizeType);
                        svgCollection.ImageSize = GetImageSize(sizeType, true);          // Toto je třeba aktualizovat po změně Zoomu!!!  Viz metoda _RecalcSvgCollectionsSizeByZoom()
                        svgImageCollections.Add(sizeType, svgCollection);
                    }
                }
            }
            return svgCollection;
        }
        /// <summary>
        /// Volá se po změně Zoomu, projde existující kolekce SVG Images (instance <see cref="DxSvgImageCollection"/> v <see cref="__SvgImageCollections"/>),
        /// a aktualizuje jejich cílové pixelové velikosti podle aktuálního Zoomu.
        /// </summary>
        private void _RecalcSvgCollectionsSizeByZoom()
        {
            // Není nutno nic dělat explicitně. 
            // Kolekce v __SvgImageCollections jsou typu DxSvgImageCollection, tato třída sama implementuje IListenerZoomChange, a tím je volána z DxComponent po změně Zoomu a sama si přepočte velikost ikon...
        }
        /// <summary>
        /// Volá se po změně typu skinu (Světlý - Tmavý), nyní se mají přegenerovat SVG ikony aplikační, kterým se ručně mění barevnost...
        /// </summary>
        private void _ReloadSvgCollectionOnLightDarkChanged()
        {
            // Není nutno nic dělat explicitně. 
            // Kolekce v __SvgImageCollections jsou typu DxSvgImageCollection, tato třída sama implementuje IListenerLightDarkChanged, a tím je volána z DxComponent po změně LightDark a sama si regeneruje potřebné ikony...
        }
        /// <summary>
        /// Najde a vrátí index ID pro vektorový obrázek daného jména, obrázek je uložen v kolekci <see cref="SvgImageCollection"/>
        /// </summary>
        /// <param name="imageName">Jméno obrázku</param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetSvgImageCollection(ResourceImageSizeType)"/>)</param>
        /// <param name="exactName"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static int GetSvgIndex(string imageName, ResourceImageSizeType sizeType, bool exactName = false, string caption = null) { return Instance._GetSvgIndex(imageName, exactName, sizeType, caption); }
        /// <summary>
        /// Najde a vrátí index ID pro vektorový obrázek daného jména, obrázek je uložen v kolekci <see cref="SvgImageCollection"/>
        /// </summary>
        /// <param name="imageName">Jméno obrázku</param>
        /// <param name="exactName"></param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetSvgImageCollection(ResourceImageSizeType)"/>)</param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetSvgIndex(string imageName, bool exactName, ResourceImageSizeType sizeType, string caption)
        {
            var svgCollection = _GetSvgImageCollection(sizeType);
            return svgCollection.GetImageId(imageName, n => _GetVectorImage(n, exactName, sizeType, caption));
        }
        /// <summary>Kolekce SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje, instanční proměnná.</summary>
        private Dictionary<ResourceImageSizeType, DxSvgImageCollection> __SvgImageCollections;
        #endregion
        #region Přístup na Aplikační zdroje (přes AsolDX.DxResourceLibrary, s pomocí SystemAdapter)
        /// <summary>
        /// Existuje daný zdroj v aplikačních zdrojích?
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        private bool _ExistsApplicationResource(string imageName, bool exactName)
        {
            return DxApplicationResourceLibrary.TryGetResource(imageName, exactName, out var _, out var _);
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
        /// Pokud najde zdroj prvního typu, vrací jen ten. Pokud nenajde, hledá zdroje dalšího typu. Pokud nenajde, vrací false.
        /// Pokud není zadán žádný typ zdroje, akceptuje jakýkoli nalezený typ.
        /// <para/>
        /// Pokud vrátí true, pak v poli <paramref name="validItems"/> je nejméně jeden prvek.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="validItems"></param>
        /// <param name="validContentTypes"></param>
        /// <returns></returns>
        private bool _TrySearchApplicationResource(string imageName, bool exactName, out DxApplicationResourceLibrary.ResourceItem[] validItems,
            params ResourceContentType[] validContentTypes)
        {
            validItems = null;
            if (String.IsNullOrEmpty(imageName)) return false;
            if (!DxApplicationResourceLibrary.TryGetResource(imageName, exactName, out var resourceItem, out var resourcePack)) return false;

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
                {   // Chceme například najít ideálně typ obsahu Vektor, anebo když není tak náhradní Bitmapa:
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
        /// Na vstupu (<paramref name="imageName"/>) nemusí být uvedena přípona, může být uvedeno obecné jméno, např. "pic\address-book-undo-2";
        /// a až knihovna zdrojů sama najde reálné obrázky: "pic\address-book-undo-2-large.svg" anebo "pic\address-book-undo-2-small.svg".
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateImageApplication(string imageName, bool exactName,
            ResourceImageSizeType? sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
        {
            if (!_TrySearchApplicationResource(imageName, exactName, out var validItems, ResourceContentType.Bitmap, ResourceContentType.Vector)) return null;
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
            ResourceImageSizeType? sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
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
            ResourceImageSizeType? sizeType, Size? optimalSvgSize,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
        {
            if (svgImage is null) return null;
            var imageSize = optimalSvgSize ?? GetImageSize(sizeType);
            if (svgPalette == null)
                svgPalette = DxComponent.GetSvgPalette(null, svgState);
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(svgImage, imageSize, svgPalette);
            return svgImage.Render(imageSize, svgPalette);
        }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v dodaných Aplikačních zdrojích
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType? sizeType)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, sizeType ?? ResourceImageSizeType.Large, out var resourceItem))
                return null;
            return resourceItem.CreateSvgImage();
        }
        #endregion
        #region Přístup na DevExpress zdroje
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
        /// Vrátí bitmapu z DevExpress zdrojů, ať už tam je jako Png nebo Svg (Svg vyrenderuje)
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="svgPalette"></param>
        /// <param name="svgState"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpress(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (_IsImageNameSvg(imageName))
                return _CreateBitmapImageDevExpressSvg(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
            else
                return _CreateBitmapImageDevExpressPng(imageName, sizeType, optimalSvgSize, svgPalette, svgState);
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
        private Image _CreateBitmapImageDevExpressSvg(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            Size size = optimalSvgSize ?? GetImageSize(sizeType);

            if (svgPalette == null)
                svgPalette = GetSvgPalette(DevExpress.LookAndFeel.UserLookAndFeel.Default, svgState);
            _RewindDevExpressResourceStream(resourceName);
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(_DevExpressResourceCache.GetSvgImage(resourceName), size, svgPalette);       // Renderování se zajišťuje externě
            else
            {   // Ven budu vracet vždy kopii objektu, který mi DevExpress vrátí, protože interní zdroj (DevExpress Image) se nesmí Disposovat, a to okolní aplikaci nedonutím. Navíc jsme metoda "Create" a ne "Get":
                var dxImage = _DevExpressResourceCache.GetSvgImage(resourceName, svgPalette, size);                              // Renderování nám provede DevExpress
                return dxImage.Clone() as Image;
            }
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
        private Image _CreateBitmapImageDevExpressPng(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            return _DevExpressResourceCache.GetImage(resourceName).Clone() as Image;
        }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImageDevExpress(string imageName)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            if (String.IsNullOrEmpty(resourceName)) return null;
            var vectorDict = _DevExpressVectorDictionary;
            byte[] svgContent = null;
            if (!vectorDict.TryGetValue(resourceName, out svgContent))
            {
                svgContent = _CreateVectorContentDevExpress(resourceName);
                vectorDict.Add(resourceName, svgContent);
            }
            return svgContent;                   // třída SvgImage má implicitní konverzi z byte[]
        }
        /// <summary>
        /// Najde a vrátí byte[] obsah z <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private byte[] _CreateVectorContentDevExpress(string imageName)
        {
            SvgImage svgImage = _CreateVectorImageDevExpress(imageName);
            if (svgImage is null) return null;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                svgImage.Save(ms);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private SvgImage _CreateVectorImageDevExpress(string imageName)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            _RewindDevExpressResourceStream(resourceName);
            return _DevExpressResourceCache.GetSvgImage(resourceName);
        }
        /// <summary>
        /// Vrátí klíč pro daný DevExpress zdroj
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private static string _GetDevExpressResourceKey(string imageName) { return (imageName == null ? "" : imageName.Trim().ToLower()); }
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
        /// Vrátí příponu zdroje DevExpress nebo null, když není
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private string _GetDevExpressResourceExtension(string resourceName)
        {
            string extension = null;
            if (!String.IsNullOrEmpty(resourceName))
            {
                var key = _GetDevExpressResourceKey(resourceName);
                var dictionary = _DevExpressResourceDictionary;
                dictionary.TryGetValue(key, out extension);
            }
            return extension;
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
        /// Dictionary obsahující DevExpress SVG images ve formě byte[]
        /// </summary>
        private Dictionary<string, byte[]> _DevExpressVectorDictionary
        {
            get
            {
                if (__DevExpressVectorDictionary is null)
                    __DevExpressVectorDictionary = new Dictionary<string, byte[]>();
                return __DevExpressVectorDictionary;
            }
        }
        private Dictionary<string, byte[]> __DevExpressVectorDictionary;
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
        #region Vytvoření bitmapy / vektoru pro daný text (náhradní ikona)
        private Image _CreateBitmapImageForCaption(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            return SystemAdapter.CreateCaptionImage(caption, sizeType, imageSize);
        }
        private SvgImage _CreateVectorImageForCaption(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            return SystemAdapter.CreateCaptionVector(caption, sizeType, imageSize);
        }
        private void _ApplyImageForCaption(ImageOptions imageOptions, string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.Image = null;
            imageOptions.SvgImage = null;

            var svgImage = SystemAdapter.CreateCaptionVector(caption, sizeType, imageSize);
            if (svgImage != null)
            {
                imageOptions.SvgImage = svgImage;
            }
            else
            {
                var bmpImage = SystemAdapter.CreateCaptionImage(caption, sizeType, imageSize);
                if (bmpImage != null)
                    imageOptions.Image = bmpImage;
            }
        }
        #endregion
        #region Soupisy zdrojů: GetResourceNames, TryGetResourceContentType, GetContentTypeFromExtension
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
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="preferVector"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static bool TryGetResourceContentType(string imageName, ResourceImageSizeType sizeType, bool preferVector, out ResourceContentType contentType, bool exactName = false)
        { return Instance._TryGetResourceContentType(imageName, exactName, sizeType, preferVector, out contentType); }
        private bool _TryGetResourceContentType(string imageName, bool exactName, ResourceImageSizeType sizeType, bool preferVector, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(imageName)) return false;

            if (_TryGetContentTypeImageArray(imageName, out contentType))
                return true;
            if (_TrySearchApplicationResource(imageName, exactName, out var validItems))
                return _TryGetContentTypeApplication(validItems, sizeType, preferVector, out contentType);
            if (_ExistsDevExpressResource(imageName))
                return _TryGetContentTypeDevExpress(imageName, out contentType);

            return false;
        }
        /// <summary>
        /// Metoda zjistí, zda daný název Image odpovídá kombinované SVG ikoně
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private bool _TryGetContentTypeImageArray(string imageName, out ResourceContentType contentType)
        {
            if (SvgImageArraySupport.TryGetSvgImageArray(imageName, out var svgImageArray))
            {
                contentType = ResourceContentType.Vector;
                return true;
            }
            contentType = ResourceContentType.None;
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
        /// <summary>
        /// Vrátí prioritu obsahu v rámci stejného typu obsahu podle přípony
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static int GetContentPriorityFromExtension(string extension)
        {
            if (String.IsNullOrEmpty(extension)) return 9999;
            extension = extension.Trim().ToLower();
            if (!extension.StartsWith(".")) extension = "." + extension;
            switch (extension)
            {
                case ".bmp": return 4;
                case ".jpg": return 3;
                case ".jpeg": return 3;
                case ".png": return 1;
                case ".gif": return 2;
                case ".pcx": return 5;
                case ".tif": return 6;
                case ".tiff": return 6;
                    
                case ".svg": return 1;
                    
                case ".mp4": return 1;
                case ".mpg": return 2;
                case ".mpeg": return 2;
                case ".avi": return 3;

                case ".wav": return 4;
                case ".flac": return 3;
                case ".mp3": return 1;
                case ".mpc": return 2;

                case ".ico": return 1;

                case ".cur": return 1;

                case ".htm": return 2;
                case ".html": return 2;
                case ".xml": return 1;
            }
            return 9999;
        }
        #endregion
        #region Priority
        /// <summary>
        /// Obsahuje true, pokud jsou preferovány vektorové ikony.
        /// Hodnota je po dobu běhu aplikace konstantní.
        /// </summary>
        public static bool IsPreferredVectorImage { get { return Instance._IsPreferredVectorImage; } }
        /// <summary>
        /// Obsahuje true, pokud jsou preferovány vektorové ikony.
        /// Hodnota je po dobu běhu aplikace konstantní.
        /// </summary>
        private bool _IsPreferredVectorImage
        {
            get
            {
                if (!__IsPreferredVectorImage.HasValue)
                    __IsPreferredVectorImage = SystemAdapter.IsPreferredVectorImage;
                return __IsPreferredVectorImage.Value;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud jsou preferovány vektorové ikony.
        /// Hodnota je určena on demand při první její potřebě.
        /// </summary>
        private bool? __IsPreferredVectorImage = null;
        #endregion
        #region Modifikace SVG ikon ASOL - konverze do tmavého skinu
        /// <summary>
        /// Inicializace Svg convertoru
        /// </summary>
        private void _InitSvgConvertor()
        {
            __SvgImageCustomize = new SvgImageCustomize();
        }
        /// <summary>
        /// Instance Svg convertoru
        /// </summary>
        private SvgImageCustomize __SvgImageCustomize;
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> ve formě byte[] převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="content"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public static DxSvgImage ConvertToDarkSkin(string imageName, byte[] content, Size? targetSize = null)
        { return Instance.__SvgImageCustomize.ConvertToDarkSkin(imageName, content, targetSize); }
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="svgImage"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public static DxSvgImage ConvertToDarkSkin(string imageName, SvgImage svgImage, Size? targetSize = null)
        { return Instance.__SvgImageCustomize.ConvertToDarkSkin(imageName, svgImage, targetSize); }
        #endregion
    }
    /// <summary>
    /// Knihovna zdrojů výhradně aplikace (Resources.bin, adresář Images), nikoli zdroje DevEpxress.
    /// Tyto zdroje jsou získány pomocí metod <see cref="SystemAdapter.GetResources()"/> atd.
    /// <para/>
    /// Toto je pouze knihovna = zdroj dat (a jejich vyhledávání), ale nikoli výkonný blok, tady se negenerují obrázky ani nic dalšího.
    /// <para/>
    /// Zastřešující algoritmy pro oba druhy zdrojů (aplikační i DevExpress) jsou v <see cref="DxComponent"/>, 
    /// metody typicky <see cref="DxComponent.ApplyImage(ImageOptions, string, Image, ResourceImageSizeType?, Size?, bool, string, bool)"/>.
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
                if (!__Current.__IsResourceLoaded)
                    __Current.TryLoadResources();
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
            __IsResourceLoaded = false;
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
        /// <summary>Obsahuje true v situaci, kdy jsou zdroje načteny.</summary>
        private bool __IsResourceLoaded;
        #endregion
        #region Kolekce zdrojů, inicializace - její načtení pomocí dat z SystemAdapter
        /// <summary>
        /// Zkusí najít adresáře se zdroji a načíst jejich soubory.
        /// Pokud načte alespoň jeden zdroj, nastaví <see cref="__IsResourceLoaded"/> = true.
        /// </summary>
        protected void TryLoadResources()
        {
            try
            {
                var resources = SystemAdapter.GetResources();
                if (resources != null)
                {   // Zdroje mohou být jen v jedné sadě, a to v té nejnovější:
                    __ItemDict.Clear();
                    __PackDict.Clear();
                    foreach (var resource in resources)
                    {
                        ResourceItem item = ResourceItem.CreateFrom(resource);
                        if (item == null) continue;
                        __ItemDict.Store(item.ItemKey, item);
                        var pack = __PackDict.Get(item.PackKey, () => new ResourcePack(item.PackKey));
                        pack.AddItem(item);
                    }
                }
                __IsResourceLoaded = (__ItemDict.Count > 0);
            }
            catch (Exception exc)
            {

            }
        }
        #endregion
        #region Public static rozhraní základní (Count, ContainsResource, TryGetResource, GetRandomName)
        /// <summary>
        /// Obsahuje true v situaci, kdy jsou zdroje načteny.
        /// </summary>
        public static bool IsResourceLoaded { get { return Current.__IsResourceLoaded; } }
        /// <summary>
        /// Počet jednotlivých položek v evidenci = jednotlivé soubory
        /// </summary>
        public static int Count { get { return Current.__ItemDict.Count; } }
        /// <summary>
        /// Vrátí true, pokud knihovna obsahuje daný zdroj.
        /// <para/>
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static bool ContainsResource(string imageName, bool exactName)
        { return Current._ContainsResource(imageName, exactName); }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/> anebo <paramref name="resourcePack"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// <para/>
        /// Tato varianta najde buď konkrétní zdroj (pokud dané jméno odkazuje na konkrétní prvek) anebo najde balíček zdrojů (obsahují stejný zdroj v různých velikostech a typech obsahu).
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="resourcePack"></param>
        /// <returns></returns>
        public static bool TryGetResource(string imageName, bool exactName, out ResourceItem resourceItem, out ResourcePack resourcePack)
        { return Current._TryGetResource(imageName, exactName, out resourceItem, out resourcePack); }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static bool TryGetResource(string imageName, bool exactName, out ResourceItem resourceItem, ResourceContentType? contentType = null, ResourceImageSizeType? sizeType = null)
        { return Current._TryGetResourceItem(imageName, exactName, out resourceItem, contentType, sizeType); }
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
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        private bool _ContainsResource(string imageName, bool exactName)
        {
            return _TryGetResourceItem(imageName, exactName, out var _, null, null);
        }
        /// <summary>
        /// Vyhledá daný jeden zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="contentType"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private bool _TryGetResourceItem(string imageName, bool exactName, out ResourceItem resourceItem, ResourceContentType? contentType = null, ResourceImageSizeType? sizeType = null)
        {
            string resourceName = _GetResourceName(imageName, exactName);
            if (_TryGetItem(resourceName, out resourceItem)) return true;
            if (_TryGetPackItem(resourceName, out resourceItem, contentType, sizeType)) return true;
            resourceItem = null;
            return false;
        }
        /// <summary>
        /// Vyhledá daný zdroj, vrací true = nalezen, zdroj je umístěn do out <paramref name="resourceItem"/> anebo <paramref name="resourcePack"/>.
        /// <para/>
        /// Daný název zdroje <paramref name="imageName"/> může/nemusí začínat lomítkem, libovolno jakým. 
        /// Nemusí být kompletní, tj. může/nemusí obsahovat suffix s velikostí obrázku a příponu.
        /// <para/>
        /// Tato varianta najde buď konkrétní zdroj (pokud dané jméno odkazuje na konkrétní prvek) anebo najde balíček zdrojů (obsahují stejný zdroj v různých velikostech a typech obsahu).
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="resourceItem"></param>
        /// <param name="resourcePack"></param>
        private bool _TryGetResource(string imageName, bool exactName, out ResourceItem resourceItem, out ResourcePack resourcePack)
        {
            string resourceName = _GetResourceName(imageName, exactName);
            resourcePack = null;
            if (_TryGetItem(resourceName, out resourceItem)) return true;
            if (_TryGetPack(resourceName, out resourcePack)) return true;
            return false;
        }
        /// <summary>
        /// Vrátí klíčové jméno zdroje ze zadaného názvu image.
        /// Pokud je požadováno <paramref name="exactName"/> = true, pak ponechá suffix (velikost) i příponu (typ), pokud je false, pak obojí odstraní.
        /// Pokud by ve jménu byly velikost a typ obsaženy, pak jsou zahozeny.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        private static string _GetResourceName(string imageName, bool exactName)
        {
            return (exactName ? SystemAdapter.GetResourceItemKey(imageName) : SystemAdapter.GetResourcePackKey(imageName, out var _, out var _));
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
            /// <para/>
            /// Pozor, tato třída v této metodě řeší přebarvení aplikační SVG ikony ze světlého skinu (=nativní) 
            /// do tmavého skinu (pomocí metody <see cref="DxComponent.ConvertToDarkSkin(string, byte[], Size?)"/>
            /// </summary>
            /// <returns></returns>
            public DxSvgImage CreateSvgImage()
            {
                // V první řadě mohu použít cachovaný SvgImage pro aktuální skin (tmavý / světlý), pokud pro daný skin máme připraven SvgImage:
                bool isDarkSkin = DxComponent.IsDarkTheme;
                if (!isDarkSkin && _SvgImageNative != null) return _SvgImageNative;
                if (isDarkSkin && _SvgImageDark != null) return _SvgImageDark;

                // Nemáme připravený SvgImage pro aktuální skin, musíme si jej připravit:
                // Získám podkladová data pro SvgImage, v nativních barvách (světlý skin):
                if (!IsSvg) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} is not SVG type.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} can not load content.");

                // Uložíme si odpovídající SvgImage:
                if (!isDarkSkin)
                {
                    _SvgImageDark = null;
                    _SvgImageNative = DxSvgImage.Create(this.ItemKey, true, content);
                    return _SvgImageNative;
                }
                else
                {
                    _SvgImageNative = null;
                    _SvgImageDark = DxComponent.ConvertToDarkSkin(this.ItemKey, content);
                    return _SvgImageDark;
                }
            }
            private DxSvgImage _SvgImageNative;
            private DxSvgImage _SvgImageDark;
            #endregion
        }
        #endregion
    }
    #region class ImageName : Knihovna názvů standardních ikon klienta
    /// <summary>
    /// Knihovna názvů standardních ikon klienta.
    /// Pomocí této třídy je možno nasměrovat jakoukoli fixně definovanou ikonu na potřebný zdroj.
    /// </summary>
    public static class ImageName
    {
        public const string DxFormIcon = "svgimages/spreadsheet/conditionalformatting.svg";

        public const string DxLayoutCloseSvg = "svgimages/hybriddemoicons/bottompanel/hybriddemo_close.svg";
        public const string DxLayoutDockLeftSvg = "svgimages/align/alignverticalleft.svg";
        public const string DxLayoutDockTopSvg = "svgimages/align/alignhorizontaltop.svg";
        public const string DxLayoutDockBottomSvg = "svgimages/align/alignhorizontalbottom.svg";
        public const string DxLayoutDockRightSvg = "svgimages/align/alignverticalright.svg";
        public const string DxLayoutClosePng = "devav/actions/delete_16x16.png";
        public const string DxLayoutDockLeftPng = "images/alignment/alignverticalleft_16x16.png";
        public const string DxLayoutDockTopPng = "images/alignment/alignhorizontaltop_16x16.png";
        public const string DxLayoutDockBottomPng = "images/alignment/alignhorizontalbottom_16x16.png";
        public const string DxLayoutDockRightPng = "images/alignment/alignverticalright_16x16.png";

        public const string DxRibbonQatMenuAdd = "svgimages/icon%20builder/actions_add.svg";
        public const string DxRibbonQatMenuRemove = "svgimages/icon%20builder/actions_remove.svg";
        public const string DxRibbonQatMenuMoveUp = "svgimages/icon%20builder/actions_arrow2up.svg";
        public const string DxRibbonQatMenuMoveDown = "svgimages/icon%20builder/actions_arrow2down.svg";
        public const string DxRibbonQatMenuShowManager = "svgimages/scheduling/viewsettings.svg";

        public const string DxBarCheckToggleNull = "images/xaf/templatesv2images/bo_unknown_disabled.svg";
        public const string DxBarCheckToggleFalse = "svgimages/icon%20builder/actions_deletecircled.svg";    //  "svgimages/xaf/state_validation_invalid.svg";
        public const string DxBarCheckToggleTrue = "svgimages/icon%20builder/actions_checkcircled.svg";      //  "svgimages/xaf/state_validation_valid.svg";

        public const string DxImagePickerClearFilter = "pic_0/UI/FilterBox/CancelFilter";                    // "svgimages/spreadsheet/clearfilter.svg";
        public const string DxImagePickerClipboarCopy = "svgimages/xaf/action_copy.svg";
        public const string DxImagePickerClipboarCopyHot = "svgimages/xaf/action_modeldifferences_copy.svg";

        public const string DxFilterBoxMenu = "svgimages/dashboards/horizontallines.svg";
        public const string DxFilterClearFilter = "pic_0/UI/FilterBox/CancelFilter";
        public const string DxFilterOperatorContains = "pic_0/UI/FilterBox/Contains";
        public const string DxFilterOperatorDoesNotContain = "pic_0/UI/FilterBox/DoesNotContain";
        public const string DxFilterOperatorEndWith = "pic_0/UI/FilterBox/EndWith";
        public const string DxFilterOperatorDoesNotEndWith = "pic_0/UI/FilterBox/DoesNotEndWith";
        public const string DxFilterOperatorMatch = "pic_0/UI/FilterBox/Match";
        public const string DxFilterOperatorDoesNotMatch = "pic_0/UI/FilterBox/DoesNotMatch";
        public const string DxFilterOperatorStartWith = "pic_0/UI/FilterBox/DoesNotStartWith";
        public const string DxFilterOperatorDoesNotStartWith = "pic_0/UI/FilterBox/DoesNotStartWith";
        public const string DxFilterOperatorEquals = "pic_0/UI/FilterBox/Equals";
        public const string DxFilterOperatorGreaterThan = "pic_0/UI/FilterBox/GreaterThan";
        public const string DxFilterOperatorGreaterThanOrEqualTo = "pic_0/UI/FilterBox/GreaterThanOrEqualTo";
        public const string DxFilterOperatorLessThan = "pic_0/UI/FilterBox/LessThan";
        public const string DxFilterOperatorLessThanOrEqualTo = "pic_0/UI/FilterBox/LessThanOrEqualTo";
        public const string DxFilterOperatorLike = "pic_0/UI/FilterBox/Like";
        public const string DxFilterOperatorNotEquals = "pic_0/UI/FilterBox/NotEquals";
        public const string DxFilterOperatorNotLike = "pic_0/UI/FilterBox/NotLike";

        public const string DxDialogApply = "svgimages/outlook%20inspired/markcomplete.svg";
        public const string DxDialogCancel = "svgimages/outlook%20inspired/delete.svg";
        public const string DxDialogIconInfo = "pic_0/Win/MessageBox/info";
        public const string DxDialogIconWarning = "pic_0/Win/MessageBox/warning";
        public const string DxDialogIconError = "pic_0/Win/MessageBox/error";
    }
    #endregion
    #region class DxSvgImageCollection : Kolekce SvgImages rozšířená o numerický index
    /// <summary>
    /// class <see cref="DxSvgImageCollection"/> : Kolekce SvgImages rozšířená o numerický index
    /// </summary>
    public class DxSvgImageCollection : DevExpress.Utils.SvgImageCollection, IListenerZoomChange, IListenerLightDarkChanged, IDisposable
    {
        #region Konstruktor, Dispose, Public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImageCollection() : base() { this.Initialize(ResourceImageSizeType.Small); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImageCollection(ResourceImageSizeType sizeType) : base() { this.Initialize(sizeType); }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="container"></param>
        public DxSvgImageCollection(System.ComponentModel.IContainer container) : base(container) { this.Initialize(ResourceImageSizeType.Small); }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"DxSvgImageCollection Count: {this.Count}";
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void Initialize(ResourceImageSizeType sizeType)
        {
            _NameIdDict = new Dictionary<string, int>();
            SizeType = sizeType;
            RefreshCurrentSize();
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        void IDisposable.Dispose()
        {
            DxComponent.UnregisterListener(this);
        }
        /// <summary>
        /// Dictionary Key to Index
        /// </summary>
        private Dictionary<string, int> _NameIdDict;
        /// <summary>
        /// Druh velikosti ikon
        /// </summary>
        public ResourceImageSizeType SizeType { get; private set; }
        #endregion
        #region IListenerZoomChange, IListenerLightDarkChanged
        /// <summary>
        /// Po změně Zoomu
        /// </summary>
        void IListenerZoomChange.ZoomChanged() { RefreshCurrentSize(); }
        /// <summary>
        /// Aktualizuje velikost ikon po změně Zoomu
        /// </summary>
        private void RefreshCurrentSize()
        {
            this.ImageSize = DxComponent.GetImageSize(SizeType, true);
        }
        /// <summary>
        /// Po změně skinu LightDark
        /// </summary>
        void IListenerLightDarkChanged.LightDarkChanged() { OnLightDarkChanged(); }
        /// <summary>
        /// Přegeneruje svoje ikony, pokud jsou <see cref="DxSvgImage"/> a požadují změnu obsahu po změně skinu
        /// </summary>
        private void OnLightDarkChanged()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {   // Procházím to takhle dřevěně proto, že
                //  a) potřebuji index [i] pro setování modifikovaného objektu,
                //  b) a protože v foreach cyklu není dobré kolekci měnit
                if (this[i] is DxSvgImage dxSvgImage && dxSvgImage.IsLightDarkCustomizable)
                {   // Pokud na dané pozici je DxSvgImage, který je LightDarkCustomizable,
                    //  pak si pro jeho jméno získám instanci zdroje (resourceItem) a tento zdroj mi vytvoří aktuálně platný SvgImage (Světlý nebo Tmavý, podle aktuálního = nového skinu):
                    if (DxApplicationResourceLibrary.TryGetResource(dxSvgImage.ImageName, true, out var resourceItem, out var _) && resourceItem != null && resourceItem.ContentType == ResourceContentType.Vector)
                        this[i] = resourceItem.CreateSvgImage();
                }
            }
        }
        #endregion
        #region Správa kolekce
        /// <summary>
        /// Přidá nový SvgImage nebo přepíše stávající
        /// </summary>
        /// <param name="name"></param>
        /// <param name="svgImage"></param>
        public void AddSvgImage(string name, SvgImage svgImage)
        {
            if (String.IsNullOrEmpty(name) || svgImage is null) return;

            string key = _GetKey(name);
            if (_ContainsKey(key))
                this[key] = svgImage;
            else
                this._Add(key, svgImage);
        }
        /// <summary>
        /// Do this instance přidá new záznam pro daný klíč, přidá jej i do Dictionary <see cref="_NameIdDict"/>, vrátí jeho ID
        /// </summary>
        /// <param name="key"></param>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        private int _Add(string key, SvgImage svgImage)
        {
            int id = _CurrentId;
            _NameIdDict.Add(key, id);
            base.Add(key, svgImage);
            return id;
        }
        /// <summary>
        /// Najde index pro <see cref="SvgImage"/> daného jména, volitelně si jej nechá vytvořit a uloží jej.
        /// Pokud nenajde a ani nevytvoří, vrací -1.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public int GetImageId(string name, Func<string, SvgImage> creator)
        {
            string key = _GetKey(name);
            int id = -1;
            if (!_NameIdDict.TryGetValue(key, out id))
            {
                id = -1;
                if (creator != null)
                {
                    SvgImage svgImage = creator(name);
                    if (svgImage != null)
                        id = _Add(key, svgImage);
                }
            }
            return id;
        }
        /// <summary>
        /// Vrátí true pokud obsahuje záznam pro dané jméno
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new bool ContainsKey(string name)
        {
            string key = _GetKey(name);
            return _ContainsKey(key);
        }
        /// <summary>
        /// Vrátí true pokud obsahuje záznam pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool _ContainsKey(string key)
        {
            return _NameIdDict.ContainsKey(key);
        }
        /// <summary>
        /// Vrátí existenci záznamu a vyhledá <see cref="SvgImage"/> daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        public bool TryGetImage(string name, out SvgImage svgImage)
        {
            string key = _GetKey(name);
            return _TryGetImage(key, out svgImage);
        }
        /// <summary>
        /// Vrátí existenci záznamu a vyhledá <see cref="SvgImage"/> pro daný klíč
        /// </summary>
        /// <param name="key"></param>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        private bool _TryGetImage(string key, out SvgImage svgImage)
        {
            svgImage = null;
            if (_NameIdDict.TryGetValue(key, out int id))
                svgImage = this[key];
            return (svgImage != null);
        }
        /// <summary>
        /// Smaže vše
        /// </summary>
        public new void Clear()
        {
            _NameIdDict.Clear();
            base.Clear();
        }
        /// <summary>
        /// Aktuální ID = index záznamu, který se bude vkládat (před vložením)
        /// </summary>
        private int _CurrentId { get { return this.Count; } }
        /// <summary>
        /// Vrací klíč pro dané jméno
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string _GetKey(string name)
        {
            return (name == null ? "" : name.Trim().ToLower());
        }
        #endregion
    }
    #endregion
    #region class DxSvgImage : rozšíření třídy SvgImage o některé atributy (jméno a druh práce s barvou)
    /// <summary>
    /// DxSvgImage : rozšíření třídy SvgImage o některé atributy (jméno a druh práce s barvou)
    /// </summary>
    public class DxSvgImage : SvgImage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImage() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImage(System.IO.MemoryStream stream) : base(stream) { }
        /// <summary>
        /// Static konstruktor
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="isLightDarkCustomizable"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DxSvgImage Create(string imageName, bool isLightDarkCustomizable, byte[] data)
        {
            if (data is null) return null;
            DxSvgImage dxSvgImage = null;
            using (var stream = new System.IO.MemoryStream(data))
                dxSvgImage = new DxSvgImage(stream);
            dxSvgImage.ImageName = imageName;
            dxSvgImage.IsLightDarkCustomizable = isLightDarkCustomizable;
            return dxSvgImage;
        }
        /// <summary>
        /// Static konstruktor
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="isLightDarkCustomizable"></param>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        public static DxSvgImage Create(string imageName, bool isLightDarkCustomizable, string xmlContent)
        {
            if (String.IsNullOrEmpty(xmlContent)) return null;
            return Create(imageName, isLightDarkCustomizable, Encoding.UTF8.GetBytes(xmlContent));
        }
        /// <summary>
        /// Jméno zdroje
        /// </summary>
        public string ImageName { get; private set; }
        /// <summary>
        /// Po změně skinu (Světlý - Tmavý) je nutno obsah přegenerovat.
        /// Bohužel obsah SvgImage změnit nelze, je třeba vygenerovat new instanci.
        /// </summary>
        public bool IsLightDarkCustomizable { get; private set; }
        /// <summary>
        /// Vrací new instanci z daného bufferu
        /// </summary>
        /// <param name="data"></param>
        public static implicit operator DxSvgImage(byte[] data)
        {
            return Create(null, false, data);
        }
    }
    #endregion
    #region SvgImageCustomize : Třída pro úpravu obsahu SVG podle aktivního Skinu (Světlý / Tmavý)
    /// <summary>
    /// SvgImageModify : Třída pro úpravu obsahu SVG podle aktivního Skinu
    /// </summary>
    internal class SvgImageCustomize
    {
        #region Colours constans
        private const string DarkColorCode00 = "#000000";
        private const string DarkColorCode38 = "#383838";
        private const string LightColorCodeD4 = "#D4D4D4";
        private const string LightColorCodeFF = "#FFFFFF";

        private const byte DarkColorPartValue00 = 0x00;
        private const byte DarkColorPartValue38 = 0x38;
        private const byte LightColorPartValueD4 = 0xD4;
        private const byte LightColorPartValueFF = 0xFF;

        internal static readonly Color DarkColor00 = Color.FromArgb(255, DarkColorPartValue00, DarkColorPartValue00, DarkColorPartValue00);
        internal static readonly Color DarkColor38 = Color.FromArgb(255, DarkColorPartValue38, DarkColorPartValue38, DarkColorPartValue38);
        internal static readonly Color LightColorD4 = Color.FromArgb(255, LightColorPartValueD4, LightColorPartValueD4, LightColorPartValueD4);
        internal static readonly Color LightColorFF = Color.FromArgb(255, LightColorPartValueFF, LightColorPartValueFF, LightColorPartValueFF);

        private const string LightColorCodeC8C6C4 = "#C8C6C4";  //světle šedá
        private const string DarkColorCode78 = "#787878";       //tmavě šedá

        private const string LightColorCodeF3B8B8 = "#F3B8B8";  //světle červená
        private const string DarkColorCodeE42D2C = "#E42D2C";   //tmavě červená

        private const string LightColorCodeF7CDA7 = "#F7CDA7";  //světle oranžová
        private const string DarkColorCodeE57428 = "#E57428";   //tmavě oranžová

        private const string LightColorCodeF7DA8E = "#F7DA8E";  //světle žlutá
        private const string DarkColorCodeF7D52C = "#F7D52C";   //tmavě žlutá

        private const string LightColorCodeBEE2E5 = "#BEE2E5";  //světle tyrkysová
        private const string DarkColorCode21B4C9 = "#21B4C9";   //tmavě tyrkysová

        private const string LightColorCode92CBEE = "#92CBEE";  //světle modrá
        private const string LightColorCode228BCB = "#228BCB";  //světle modrá        
        private const string DarkColorCode0964B0 = "#0964B0";   //tmavě modrá

        private const string LightColorCodeDFBCD9 = "#DFBCD9";  //světle fialová
        private const string DarkColorCodeA0519F = "#A0519F";   //tmavě fialová

        private const string LightColorCodeDDAE85 = "#DDAE85";  //světle hnědá
        private const string DarkColorCode9B5435 = "#9B5435";   //tmavě hnědá

        private const string LightColorCodeACD8B1 = "#ACD8B1";  //světle zelená
        private const string DarkColorCode0BA04A = "#0BA04A";   //tmavě zelená
        private const string LightColorCode17AB4F = "#17AB4F";  //zelená
        #endregion
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageCustomize()
        {
            _SvgImageColorTable = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) //JD 0065749 26.06.2020
            {
                { LightColorCodeC8C6C4, DarkColorCode78 },     //class-colour-10 Šedá - #383838 změníme na #787878
                { LightColorCodeF3B8B8, DarkColorCodeE42D2C }, //class-colour-20 Červená
                { LightColorCodeF7CDA7, DarkColorCodeE57428 }, //class-colour-30 Oranžová
                { LightColorCodeF7DA8E, DarkColorCodeF7D52C }, //class-colour-40 Žlutá - dark je stejná jako u oranžové #E57428, změníme ji na #F7D52C
                { LightColorCodeACD8B1, DarkColorCode0BA04A }, //class-colour-50 Zelená
                { LightColorCodeBEE2E5, DarkColorCode21B4C9 }, //class-colour-60 Tyrkysová
                { LightColorCode92CBEE, DarkColorCode0964B0 }, //class-colour-70 Modrá
                { LightColorCodeDFBCD9, DarkColorCodeA0519F }, //class-colour-80 Fialová
                { LightColorCodeDDAE85, DarkColorCode9B5435 }  //class-colour-90 Hnědá
            };
        }
        /// <summary>
        /// Dictionary obsahující jako klíč barvu uloženou v SvgImage pro světlý skin, a jako Value odpovídající barvu v tmavém skinu
        /// </summary>
        private Dictionary<string, string> _SvgImageColorTable;
        private bool IsDarkTheme { get; set; }
        #endregion
        #region Konverze SvgImage Light to Dark skin
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> ve formě byte[] převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="content"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public DxSvgImage ConvertToDarkSkin(string imageName, byte[] content, Size? targetSize = null)
        {
            return _ConvertToDarkSkin(imageName, content, targetSize);
        }
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="svgImage"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public DxSvgImage ConvertToDarkSkin(string imageName, SvgImage svgImage, Size? targetSize = null)
        {
            byte[] content;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {   // Přesypu obsah SvgImage do byte[]:
                svgImage.Save(ms);
                content = ms.ToArray();
            }
            return _ConvertToDarkSkin(imageName, content, targetSize);
        }
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> ve formě byte[] převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="content"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        private DxSvgImage _ConvertToDarkSkin(string imageName, byte[] content, Size? targetSize = null)
        {
            string contentXml = Encoding.UTF8.GetString(content);

            // swap light and dark colour of path created by polygon or filled path
            if (contentXml.Contains("fill") && (contentXml.Contains("path") || contentXml.Contains("polygon"))) //JD 0064459 07.02.2020 //JD 0065749 26.06.2020
            {
                var contentXmlDoc = new XmlDocument();
                contentXmlDoc.LoadXml(contentXml);
                foreach (XmlNode childNode in contentXmlDoc.ChildNodes) _ProcessSvgNode(childNode);
                using (var sw = new System.IO.StringWriter())
                using (var xw = new XmlTextWriter(sw))
                {
                    contentXmlDoc.WriteTo(xw);
                    contentXml = sw.ToString();
                }
            }

            // swap light and dark colours of class-colour, form-colour, tag and button-grey
            if (imageName.Contains("class-colour-10")             //JD 0065426 26.05.2020
                || imageName.Contains("form-colour-10")           //JD 0066902 20.11.2020
                || imageName.Contains("tag-filled-grey")          //JD 0065749 26.06.2020
                || imageName.Contains("button-grey-filled"))
            {
                contentXml = contentXml.Replace($"fill=\"{LightColorCodeC8C6C4}\" stroke=\"{DarkColorCode38}\"", $"fill=\"{DarkColorCode78}\" stroke=\"none\""); //circle/rect
                contentXml = contentXml.Replace($"fill=\"{LightColorCodeFF}\" stroke=\"{DarkColorCode38}\"", $"fill=\"{LightColorCodeFF}\" stroke=\"{LightColorCodeC8C6C4}\""); //path
            }
            else if (imageName.Contains("Rel1ExtDoc") || imageName.Contains("RelNExtDoc")) //JD 0067697 19.02.2021 Ve formuláři nejsou označ.blokované DV
            {
                contentXml = contentXml.Replace($"fill=\"{LightColorCodeF7DA8E}\" stroke=\"{DarkColorCode38}\"", $"fill=\"{DarkColorCode38}\" stroke=\"{LightColorCodeF7DA8E}\""); //rect
                contentXml = contentXml.Replace($"fill=\"none\" stroke=\"{DarkColorCode38}\"", $"fill=\"none\" stroke=\"{LightColorCodeF7DA8E}\""); //path
                contentXml = contentXml.Replace($"fill=\"none\" stroke=\"{DarkColorCodeE57428}\"", $"fill=\"none\" stroke=\"{LightColorCodeF7DA8E}\""); //path
            }
            else
            {
                foreach (var lightDarkColor in _SvgImageColorTable)  //JD 0065749 26.06.2020
                {
                    if (imageName.Contains("class-colour")        //JD 0065426 26.05.2020
                        || imageName.Contains("form-colour")      //JD 0066902 20.11.2020
                        || imageName.Contains("tag-filled")
                        || (imageName.Contains("button") && imageName.Contains("filled"))
                        || imageName.Contains("DynRel"))          //JD 0067697 19.02.2021 Ve formuláři nejsou označ.blokované DV
                    {
                        contentXml = contentXml.Replace($"fill=\"{lightDarkColor.Key}\" stroke=\"{lightDarkColor.Value}\"", $"fill=\"{lightDarkColor.Key}\" stroke=\"none\""); //circle/rect
                        contentXml = contentXml.Replace($"fill=\"{LightColorCodeF7DA8E/*lightDarkColor.Key*/}\" stroke=\"{DarkColorCodeE57428}\"", $"fill=\"{LightColorCodeF7DA8E/*lightDarkColor.Key*/}\" stroke=\"none\""); //path - žlutá je specifická
                    }
                    else
                    {
                        contentXml = contentXml.Replace($"fill=\"{lightDarkColor.Key}\"", $"fill=\"{lightDarkColor.Value}\"");
                        contentXml = contentXml.Replace($"stroke=\"{lightDarkColor.Value}\"", $"stroke=\"{lightDarkColor.Key}\"");
                    }
                }

                //světle modrá -> světlejší modrá
                contentXml = contentXml.Replace($"fill=\"none\" stroke=\"{LightColorCode228BCB}\"", $"fill=\"none\" stroke=\"{LightColorCode92CBEE}\""); //JD 0065749 22.07.2020
                contentXml = contentXml.Replace($"fill=\"{LightColorCodeFF}\" stroke=\"{LightColorCode228BCB}\"", $"fill=\"none\" stroke=\"{LightColorCode92CBEE}\""); //JD 0065749 22.07.2020
                contentXml = contentXml.Replace($"fill=\"{LightColorCode228BCB}\"", $"fill=\"{LightColorCode92CBEE}\""); //JD 0065749 22.07.2020
                contentXml = contentXml.Replace($"stroke=\"{LightColorCode228BCB}\"", $"stroke=\"{LightColorCode92CBEE}\""); //JD 0065749 03.08.2020

                //tmavě zelená -> zelená
                contentXml = contentXml.Replace($"fill=\"{DarkColorCode0BA04A}\"", $"fill=\"{LightColorCode17AB4F}\""); //JD 0065749 22.07.2020
                contentXml = contentXml.Replace($"stroke=\"{DarkColorCode0BA04A}\"", $"stroke=\"{LightColorCode17AB4F}\""); //JD 0065749 03.08.2020

                //bílá -> tmavě šedá                    
                contentXml = contentXml.Replace($"fill=\"{LightColorCodeFF}\"", $"fill=\"{DarkColorCode38}\"");

                //černá a tmavě šedá -> světle šedá
                contentXml = contentXml.Replace($"stroke=\"{DarkColorCode00}\"", $"stroke=\"{LightColorCodeD4}\"");
                contentXml = contentXml.Replace($"stroke=\"{DarkColorCode38}\"", $"stroke=\"{LightColorCodeD4}\"");
            }
            

            if (targetSize.HasValue && targetSize.Value.Width == 24 && targetSize.Value.Height == 24)
            {
                if (imageName.Contains("form-colour") //JD 0066902 17.12.2020 Rozlišit záložky přehledů a formulářů
                    || imageName.Contains("Rel1"))    //JD 0067697 12.02.2021 Ve formuláři nejsou označ.blokované DV - ikona se liší pouze barvami
                {
                    contentXml = contentXml.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                    contentXml = contentXml.Replace($"d=\"M10,9.5h12M10,12.5h12M10,15.5h12M10,18.5h12M10,21.5h8\"", $"d=\"M10,10.25h12M10,12.75h12M10,15.5h12M10,18.25h12M10,21h8\"");
                }
                else if (imageName.Contains("RelN")) //JD 0067697 12.02.2021 Ve formuláři nejsou označ.blokované DV
                {
                    contentXml = contentXml.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                    contentXml = contentXml.Replace($"d=\"M10.5,6.5h13v17M13.5,3.5h13v17\"", $"d=\"M10.5,7.25h12.75v16M13.5,4.5h12.5v16\"");
                    contentXml = contentXml.Replace($"d=\"M10,13.5h8M10,16.5h8M10,19.5h8M10,22.5h6\"", $"d=\"M10,14.25h8M10,17h8M10,19.5h8M10,22.25h6\"");
                }
                else if (imageName.Contains("RelArch")) //JD 0067697 17.02.2021 Ve formuláři nejsou označ.blokované DV
                {
                    contentXml = contentXml.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                    contentXml = contentXml.Replace($"d=\"M7.5,15.5H24\"", $"d=\"M7.5,15.25H24\"");
                    contentXml = contentXml.Replace($"d=\"M12.5,8v3.5h7v-3.5M12.5,19v3.5h7v-3.5\"", $"d=\"M12.5,8v3.5h7v-3.5M12.5,18.75v3.5h7v-3.5\"");
                }
            }

            // Upravený string contentXml převedu do byte[], a z něj pak implicitní konverzí do SvgImage:
            byte[] darkContent = Encoding.UTF8.GetBytes(contentXml);
            return DxSvgImage.Create(imageName, true, darkContent);
        }
        private void _ProcessSvgNode(XmlNode node)
        {
            if (node.Name == "svg")
            {
                foreach (XmlNode childNodeOfSvg in node.ChildNodes)
                {
                    _ProcessGNode(childNodeOfSvg);
                }
            }
        }
        private void _ProcessGNode(XmlNode node)
        {
            if (node.Name == "g")
            {
                foreach (XmlNode childNodeOfG in node.ChildNodes)
                {
                    if (childNodeOfG.Name == "polygon") //path created by polygon
                    {
                        _ProcessPolygonNode(childNodeOfG);
                    }
                    else if (childNodeOfG.Name == "path") //filled path
                    {
                        _ProcessPathNode(childNodeOfG);
                    }
                    else if (childNodeOfG.Name == "g")
                    {
                        _ProcessGNode(childNodeOfG); //recursion
                    }
                }
            }
        }
        private void _ProcessPolygonNode(XmlNode childNodeOfG)
        {
            if (childNodeOfG.Name == "polygon")
            {
                foreach (XmlAttribute attr in childNodeOfG.Attributes)
                {
                    if (attr.Name == "fill")
                    {
                        switch (attr.Value)
                        {
                            case DarkColorCode00:
                            case DarkColorCode38:
                                attr.Value = LightColorCodeD4;
                                break;
                            default:
                                foreach (var lightDarkColor in _SvgImageColorTable) //JD 0065749 26.06.2020
                                {
                                    if (attr.Value == lightDarkColor.Key) attr.Value = lightDarkColor.Value;
                                }
                                break;
                        }
                    }
                }
            }
        }
        private void _ProcessPathNode(XmlNode childNodeOfPath)
        {
            if (childNodeOfPath.Name == "path")
            {
                foreach (XmlAttribute attr in childNodeOfPath.Attributes)
                {
                    if (attr.Name == "fill")
                    {
                        switch (attr.Value)
                        {
                            case DarkColorCode00:
                            case DarkColorCode38:
                                attr.Value = LightColorCodeD4;
                                break;
                            default:
                                foreach (var lightDarkColor in _SvgImageColorTable) //JD 0065749 26.06.2020
                                {
                                    if (attr.Value == lightDarkColor.Key) attr.Value = lightDarkColor.Value;
                                }
                                break;
                        }
                    }
                }
            }
        }
        #endregion
    }
    #endregion
    #region SvgImageArraySupport : podpora pro kombinace SVG images na straně klienta
    /// <summary>
    /// SvgImageArraySupport : podpora pro kombinace SVG images na straně klienta
    /// </summary>
    internal class SvgImageArraySupport
    {
        /// <summary>
        /// Vrátí true, pokud dodaný string představuje instanci <see cref="SvgImageArraySupport"/>, a pokud ano pak ji rovnou vytvoří.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="arrayInfo"></param>
        /// <returns></returns>
        internal static bool TryGetSvgImageArray(string resourceName, out SvgImageArrayInfo arrayInfo)
        {
            return SvgImageArrayInfo.TryDeserialize(resourceName, out arrayInfo);
        }
        /// <summary>
        /// Vygeneruje <see cref="SvgImage"/> z dodané sady vstupních obrázků
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        internal static SvgImage CreateSvgImage(SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType = null)
        {
            if (svgImageArray == null || svgImageArray.IsEmpty) return null;

            // Výsledný SvgImage:
            DevExpress.Utils.Svg.SvgImage svgImageOut;
            using (var memoryStream = new System.IO.MemoryStream(_BlankSvgBaseBuffer))
                svgImageOut = DevExpress.Utils.Svg.SvgImage.FromStream(memoryStream);

            // Kombinace:
            foreach (var image in svgImageArray.Items)
            {
                DevExpress.Utils.Svg.SvgImage svgImageInp = DxComponent.GetVectorImage(image.ImageName, false, sizeType);
                if (svgImageInp is null) continue;

                DevExpress.Utils.Svg.SvgGroup svgGroupSum = new DevExpress.Utils.Svg.SvgGroup();
                if (_SvgCombineSetTransform(svgImageInp, image, svgGroupSum))
                {
                    foreach (var svgItem in svgImageInp.Root.Elements)
                        svgGroupSum.Elements.Add(svgItem.DeepCopy());
                }
                svgImageOut.Root.Elements.Add(svgGroupSum);
            }

            return svgImageOut;
        }
        /// <summary>
        /// Metoda nastaví do <paramref name="svgGroupSum"/> sadu transformací tak, 
        /// aby vstupující SvgImage <paramref name="svgImageInp"/> 
        /// byl správně umístěn do relativního prostoru podle daného předpisu <paramref name="image"/>.
        /// </summary>
        /// <param name="svgImageInp"></param>
        /// <param name="image"></param>
        /// <param name="svgGroupSum"></param>
        /// <returns></returns>
        private static bool _SvgCombineSetTransform(DevExpress.Utils.Svg.SvgImage svgImageInp, SvgImageArrayItem image, DevExpress.Utils.Svg.SvgGroup svgGroupSum)
        {
            // Kontroly:
            if (svgImageInp is null || image is null || svgGroupSum is null) return false;

            // Nejprve zpracuji velikost (Width, Height) = přepočet z koordinátů vstupní ikony do koordinátů cílového prostoru:
            // Vstupní ikona a její umístění a velikost:
            double iw = svgImageInp.Width;
            double ih = svgImageInp.Height;
            if (iw <= 0d || ih <= 0d) return false;

            // Target požadovaný prostor = jde o souřadnice v celkovém prostoru { 0, 0, 120, 120 }:
            Rectangle? target = image.ImageRelativeBounds;
            int ts = SvgImageArrayInfo.BaseSize;
            double tw = target?.Width ?? ts;
            double th = target?.Height ?? ts;
            if (tw <= 0d || th <= 0d) return false;

            // Koeficient změny velikosti vstupní ikony do target prostoru (je zajištěno, že Width i Height jsou kladné):
            // Určím "rs" = poměr, jakým vynásobím vstupní velikosti (iw, ih) tak, abych zachoval poměr stran (obdélníky) a výsledek se vešel do daného prostoru (tw, th):
            double rw = tw / iw;
            double rh = th / ih;
            double rs = (rw < rh ? rw : rh);

            // Velikost vstupní ikony ve výstupním prostoru:
            double ow = iw * rs;
            double oh = ih * rs;

            // Umístění ikony ve výstupním prostoru podle požadavku:
            double tx = target?.X ?? 0;
            double ty = target?.Y ?? 0;
            double ox = _SvgCombineSetTransformDim(tx, tw, ow);
            double oy = _SvgCombineSetTransformDim(ty, th, oh);

            // První transformace = vstupní ikonu posunu tak, aby její obsah byl v souřadnici 0/0 (pokud to již není):
            double ix = svgImageInp.OffsetX;
            double iy = svgImageInp.OffsetY;
            if (ix != 0d || iy != 0d)
                svgGroupSum.Transformations.Add(new DevExpress.Utils.Svg.SvgTranslate(new double[] { -ix, -iy }));

            // Třetí transformace = posunutí zmenšené ikony do cílového prostoru:
            if (ox != 0d || oy != 0d)
                svgGroupSum.Transformations.Add(new DevExpress.Utils.Svg.SvgTranslate(new double[] { ox, oy }));

            // Druhá transformace = změna měřítka vstupní ikony:
            if (rs != 1d)
                svgGroupSum.Transformations.Add(new DevExpress.Utils.Svg.SvgScale(new double[] { rs, rs }));

            string log = $"Input: {{ X={ix}, Y={iy}, W={iw}, H={ih}, R={ix + iw}, B={iy + ih} }}; Target: {{ X={tx}, Y={ty}, W={tw}, H={th}, R={tx + tw}, B={ty + th} }}; RatioSize: {rs}; Output: {{ X={ox}, Y={oy}, W={ow}, H={oh}, R={ox + ow}, B={oy + oh} }}";

            return true;
        }
        /// <summary>
        /// Metoda vrátí souřadnici reálného výstupu tak, aby tento výstup byl v prostoru <see cref="SvgImageArrayInfo.BaseSize"/> umístěn stejně, jako je umístěn prostor target.
        /// Příklad: <paramref name="targetPoint"/> = 30, <paramref name="targetSize"/> = 60 (zabírá tedy prostor 30 + 60 + 30 = z celkem 120),
        /// a velikost reálné ikony <paramref name="realSize"/> je 30, pak výstupem bude 45 = tak, aby ikona byla reálně svým koncem uprostřed prostoru (prostor 45 + 30 + 45 = 120).
        /// <para/>
        /// Velikosti <paramref name="targetSize"/> i <paramref name="realSize"/> jsou kladné, tato metoda to už nekontroluje.
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <param name="targetSize"></param>
        /// <param name="realSize"></param>
        private static double _SvgCombineSetTransformDim(double targetPoint, double targetSize, double realSize)
        {
            // Poměr prostoru před targetPoint vzhledem k volnému prostoru (=120 - targetSize)
            //  => pokud targetPoint = 30 a targetSize = 60, pak target je přesně v polovině prostoru délky 120  (30 + 60 + 30 = 120) ... relative = 0.5d
            double relative = (targetPoint > 0d && targetSize < _SvgTargetSize ? (targetPoint / (_SvgTargetSize - targetSize)) : 0d);

            // Ve stejném poměru umístíme reálný prostor realSize (mělo by být zajištěno, že realSize <= targetSize, přepočtem velikosti pomocí koeficientu)
            //  => pokud realSize = 30, a relative = 0.5d, pak realPoint musí být 45 = tak, aby realSize bylo uprostřed targetSize (45 + 30 + 45 = 120):
            double realPoint = relative * (_SvgTargetSize - realSize);
            return realPoint;
        }
        /// <summary>
        /// Základní velikost cílového prostoru
        /// </summary>
        private static double _SvgTargetSize { get { return SvgImageArrayInfo.BaseSize; } }
        /// <summary>
        /// byte[] obsahující definici prázdného SVG Image o velikosti <see cref="SvgImageArrayInfo.BaseSize"/>
        /// </summary>
        private static byte[] _BlankSvgBaseBuffer { get { return Encoding.UTF8.GetBytes(_BlankSvgBaseXml); } }
        /// <summary>
        /// String  obsahující definici prázdného SVG Image o velikosti <see cref="SvgImageArrayInfo.BaseSize"/>
        /// </summary>
        private static string _BlankSvgBaseXml { get { string size = _SvgTargetSize.ToString(); return $"<svg viewBox='0 0 {size} {size}' xmlns='http://www.w3.org/2000/svg'></svg>"; } }
    }
    #endregion
}
