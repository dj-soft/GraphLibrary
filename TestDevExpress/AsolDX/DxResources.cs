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
        /// <param name="caption"></param>
        /// <param name="exactName"></param>
        /// <param name="isPreferredVectorImage">Preference vektorů: true = vektory; false = bitmapy, null = podle konfigurace</param>
        /// <returns></returns>
        public static Image CreateBitmapImage(string imageName,
            ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null,
            string caption = null, bool exactName = false, bool? isPreferredVectorImage = null)
        { return Instance._CreateBitmapImage(imageName, exactName, sizeType, optimalSvgSize, caption, isPreferredVectorImage); }
        /// <summary>
        /// Vygeneruje a vrátí nový obrázek daného jména.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <param name="isPreferredVectorImage">Preference vektorů: true = vektory; false = bitmapy, null = podle konfigurace</param>
        /// <returns></returns>
        private Image _CreateBitmapImage(string imageName, bool exactName,
            ResourceImageSizeType? sizeType, Size? optimalSvgSize,
            string caption, bool? isPreferredVectorImage)
        {
            bool hasName = !String.IsNullOrEmpty(imageName);
            bool hasCaption = !String.IsNullOrEmpty(caption);

            if (hasName && DxSvgImage.TryGetXmlContent(imageName, sizeType, out var dxSvgImage))
                return _ConvertVectorToImage(dxSvgImage, sizeType, optimalSvgSize);
            if (hasName && _TryGetContentTypeImageArray(imageName, out var _, out var svgImageArray))
                return _CreateBitmapImageArray(svgImageArray, sizeType, optimalSvgSize);

            ResourceContentType[] validContentTypes = _GetValidImageContentTypes(isPreferredVectorImage);
            if (hasName && _TryGetApplicationResources(imageName, exactName, out var validItems, validContentTypes))
                return _CreateBitmapImageApplication(validItems, sizeType, optimalSvgSize);
            
            if (hasName && _ExistsDevExpressResource(imageName))
                return _CreateBitmapImageDevExpress(imageName, sizeType, optimalSvgSize);
            if (hasCaption)
                return CreateCaptionImage(caption, sizeType, null);

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
        /// <summary>
        /// Vrátí typ velikosti obrázku vhodný pro danou fyzickou velikost prostoru.
        /// Pro střední velikost (17 až 24 px včetně) dovoluje zadat explicitní hodnotu, protože pro vektorové obrázky je lepší generovat Small než Large.
        /// </summary>
        /// <param name="currentSize"></param>
        /// <param name="mediumSize"></param>
        /// <returns></returns>
        public static ResourceImageSizeType GetImageSizeType(Size currentSize, ResourceImageSizeType mediumSize = ResourceImageSizeType.Medium)
        {
            int s = (currentSize.Width < currentSize.Height ? currentSize.Width : currentSize.Height);
            if (s <= 3) return ResourceImageSizeType.None;
            if (s <= 16) return ResourceImageSizeType.Small;
            if (s <= 24) return mediumSize;
            return ResourceImageSizeType.Large;
        }
        /// <summary>
        /// Vrátí typ velikosti obrázku vhodný pro danou fyzickou velikost prostoru, optimálně pro Bitmap.
        /// Pro střední velikost (17 až 24 px včetně) dovoluje zadat explicitní hodnotu, protože pro vektorové obrázky je lepší generovat Small než Large.
        /// </summary>
        /// <param name="currentSize"></param>
        /// <returns></returns>
        public static ResourceImageSizeType GetImageSizeTypeBitmap(Size currentSize)
        {
            int s = (currentSize.Width < currentSize.Height ? currentSize.Width : currentSize.Height);
            if (s <= 3) return ResourceImageSizeType.None;
            if (s <= 16) return ResourceImageSizeType.Small;
            if (s <= 24) return ResourceImageSizeType.Medium;
            return ResourceImageSizeType.Large;
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

            bool hasName = !String.IsNullOrEmpty(imageName);
            bool hasCaption = !String.IsNullOrEmpty(caption);

            if (hasName && DxSvgImage.TryGetXmlContent(imageName, sizeType, out var dxSvgImage))
                return dxSvgImage;
            if (hasName && SvgImageSupport.TryGetSvgImageArray(imageName, out var svgImageArray))
                return _GetVectorImageArray(svgImageArray, sizeType);
            if (hasName && _TryGetApplicationResources(imageName, exactName, out var validItems, ResourceContentType.Vector))
                return _GetVectorImageApplication(validItems, sizeType);
            if (hasName && _ExistsDevExpressResource(imageName) && _IsImageNameSvg(imageName))
                return _GetVectorImageDevExpress(imageName);
            if (hasCaption)
                return CreateCaptionVector(caption, sizeType);
            return null;
        }
        /// <summary>
        /// Dodané pole vektorových images vyrenderuje do Image a ty zabalí a vrátí do jedné Icon
        /// </summary>
        /// <param name="vectorItems"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Icon _ConvertVectorsToIcon(DxApplicationResourceLibrary.ResourceItem[] vectorItems, ResourceImageSizeType? sizeType)
        {
            List<Tuple<Size, Image>> imageInfos = new List<Tuple<Size, Image>>();
            if (sizeType.HasValue)
            {
                if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(vectorItems, sizeType, out var vectorItem))
                    imageInfos.Add(new Tuple<Size, Image>(GetImageSize(vectorItem.SizeType), _ConvertApplicationVectorToImage(vectorItem, sizeType)));
            }
            if (imageInfos.Count == 0)
            {
                foreach (var vectorItem in vectorItems)
                    imageInfos.Add(new Tuple<Size, Image>(GetImageSize(vectorItem.SizeType), _ConvertApplicationVectorToImage(vectorItem, vectorItem.SizeType)));
            }
            var icon = _ConvertBitmapsToIcon(imageInfos);
            _DisposeImages(imageInfos);
            return icon;
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do bitmapy
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <returns></returns>
        private Image _ConvertVectorToImage(SvgImage svgImage, ResourceImageSizeType? sizeType, Size? optimalSvgSize)
        {
            if (svgImage is null) return null;
            var imageSize = optimalSvgSize ?? GetImageSize(sizeType);
            var svgPalette = DxComponent.GetSvgPalette();
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(svgImage, imageSize, svgPalette);
            return svgImage.Render(imageSize, svgPalette);
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do ikony
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <returns></returns>
        private Icon _ConvertVectorToIcon(SvgImage svgImage, ResourceImageSizeType? sizeType, Size? optimalSvgSize)
        {
            Icon icon = null;
            using (var image = _ConvertVectorToImage(svgImage, sizeType, optimalSvgSize))
            {
                List<Tuple<Size, Image>> imageInfos = new List<Tuple<Size, Image>>();
                imageInfos.Add(new Tuple<Size, Image>(image.Size, image));
                icon = _ConvertBitmapsToIcon(imageInfos);
                _DisposeImages(imageInfos);
            }
            return icon;
        }
        /// <summary>
        /// Vrátí typ velikosti obrázku vhodný pro danou fyzickou velikost prostoru, optimálně pro Vector.
        /// </summary>
        /// <param name="currentSize"></param>
        /// <returns></returns>
        public static ResourceImageSizeType GetImageSizeTypeVector(Size currentSize)
        {
            int s = (currentSize.Width < currentSize.Height ? currentSize.Width : currentSize.Height);
            if (s <= 3) return ResourceImageSizeType.None;
            if (s <= 24) return ResourceImageSizeType.Small;
            return ResourceImageSizeType.Large;
        }
        #endregion
        #region CreateIconImage - Tvorba ICO pro dodaný zdroj
        /// <summary>
        /// Metoda najde a vrátí ikonu pro dané jméno (a velikost) zdroje.
        /// Pokud najde zdroj typu <see cref="ResourceContentType.Icon"/>, vrací new instanci <see cref="Icon"/> vytvořenou z obsahu daného zdroje požadované velikosti.
        /// Pokud najde zdroje typu <see cref="ResourceContentType.Bitmap"/>, vrací new instanci <see cref="Icon"/> vytvořenou konverzí ze všech/z odpovídající bitmapy.
        /// Pro jiné zdroje vrací null. Třeba časem dokončíme i konverzi z SVG do ICO.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static Icon CreateIconImage(string imageName, bool exactName = false, ResourceImageSizeType? sizeType = null)
        { return Instance._CreateIconImage(imageName, exactName, sizeType); }
        /// <summary>
        /// Metoda najde a vrátí ikonu pro dané jméno (a velikost) zdroje.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Icon _CreateIconImage(string imageName, bool exactName, ResourceImageSizeType? sizeType)
        {
            if (String.IsNullOrEmpty(imageName)) return null;

            // Může to být explicitní SVG XML content:
            if (DxSvgImage.TryGetXmlContent(imageName, sizeType, out var dxSvgImage))
                return _ConvertVectorToIcon(dxSvgImage, sizeType, null);

            // Může to být pole SVG images:
            if (_TryGetContentTypeImageArray(imageName, out var _, out var svgImageArray))
                return _ConvertImageArrayToIcon(svgImageArray, sizeType);

            // Pro dané jméno zdroje máme k dispozici resource s typem Icon:
            DxApplicationResourceLibrary.ResourceItem[] validItems;
            if (_TryGetApplicationResources(imageName, exactName, out validItems, ResourceContentType.Icon))
            {   // Tady můžeme vrátit jen jednu ikonu, podle požadované velikosti:
                DxApplicationResourceLibrary.ResourceItem iconItem =
                    ((validItems.Length > 1 && DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(validItems, sizeType, out var item)) ?
                    item : validItems[0]);
                return iconItem?.CreateIconImage();
            }

            bool isPreferredVector = _IsPreferredVectorImage;

            // Preferujeme vektory, a je to být aplikační SVG image:
            if (isPreferredVector && _TryGetApplicationResources(imageName, exactName, out validItems, ResourceContentType.Vector))
                return _ConvertVectorsToIcon(validItems, sizeType);

            // Není to vektor (nebo je nepreferujeme), a je to aplikační Bitmapa:
            if (_TryGetApplicationResources(imageName, exactName, out validItems, ResourceContentType.Bitmap))
                return _ConvertBitmapsToIcon(validItems, sizeType);

            // Sice vektory nepreferujeme, ale Bitmapu jsme nenašli a našli jsme vektor:
            if (!isPreferredVector && _TryGetApplicationResources(imageName, exactName, out validItems, ResourceContentType.Vector))
                return _ConvertVectorsToIcon(validItems, sizeType);

            // Může to být Image z DevExpress?
            if (_ExistsDevExpressResource(imageName))
            {
                using (var bitmap = _CreateBitmapImageDevExpress(imageName, sizeType, null))
                    return _ConvertBitmapToIcon(bitmap);
            }

            return null;
        }
        /// <summary>
        /// Dané pole Resource převede na bitmapy a ty vloží do ikony, kterou vrátí.
        /// Pokud je daná velikost <paramref name="sizeType"/>, pak výsledná ikona bude obsahovat jen jednu ikonu (bitmapu) v požadované velikosti.
        /// Pokud je <paramref name="sizeType"/> = null, pak výsledná ikona bude obsahovat všechny dostupné bitmapy.
        /// </summary>
        /// <param name="bitmapItems"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapsToIcon(DxApplicationResourceLibrary.ResourceItem[] bitmapItems, ResourceImageSizeType? sizeType)
        {
            List<Tuple<Size, Image>> imageInfos = new List<Tuple<Size, Image>>();
            if (sizeType.HasValue)
            {
                if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(bitmapItems, sizeType, out var bitmapItem))
                    imageInfos.Add(new Tuple<Size, Image>(GetImageSize(bitmapItem.SizeType), bitmapItem.CreateBmpImage()));
            }
            if (imageInfos.Count == 0)
            {
                foreach (var bitmapItem in bitmapItems)
                    imageInfos.Add(new Tuple<Size, Image>(GetImageSize(bitmapItem.SizeType), bitmapItem.CreateBmpImage()));
            }
            var icon = _ConvertBitmapsToIcon(imageInfos);
            _DisposeImages(imageInfos);
            return icon;
        }
        /// <summary>
        /// Danou Bitmapu převede do ikony a vrátí ji.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="disposeImage"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapToIcon(Image bitmap, bool disposeImage = false)
        {
            if (bitmap == null) return null;

            // Konverzi zajistí metoda _ConvertBitmapsToIcon(), která požaduje na vstupu pole Image a jejich Size (imageInfos):
            List<Tuple<Size, Image>> imageInfos = new List<Tuple<Size, Image>>();
            imageInfos.Add(new Tuple<Size, Image>(bitmap.Size, bitmap));
            var icon = _ConvertBitmapsToIcon(imageInfos);
            if (disposeImage)    // Tady bude Dispose jen na požadavek, typicky ne: Image vzniká jinde, ať si jej disposuje ten kdo jej vytvořil!
                _DisposeImages(imageInfos);
            return icon;
        }
        /// <summary>
        /// Dané pole obrázků převede do formátu ICO a vrátí odpovídající MemoryStream
        /// </summary>
        /// <param name="imageInfos"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapsToIcon(List<Tuple<Size, Image>> imageInfos)
        {
            using (var msIco = new System.IO.MemoryStream())
            using (var bw = new System.IO.BinaryWriter(msIco))
            {   // https://en.wikipedia.org/wiki/ICO_(file_format)
                bw.Write((short)0);                                  // 0-1 reserved
                bw.Write((short)1);                                  // 2-3 image type, 1 = icon, 2 = cursor
                bw.Write((short)imageInfos.Count);                   // 4-5 number of images
                foreach (var imageInfo in imageInfos)
                {
                    using (var msImg = new System.IO.MemoryStream())
                    {
                        var size = imageInfo.Item1;
                        imageInfo.Item2.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);

                        bw.Write((byte)size.Width);                  // 6 image width
                        bw.Write((byte)size.Height);                 // 7 image height
                        bw.Write((byte)0);                           // 8 number of colors
                        bw.Write((byte)0);                           // 9 reserved
                        bw.Write((short)0);                          // 10-11 color planes
                        bw.Write((short)32);                         // 12-13 bits per pixel
                        bw.Write((int)msImg.Length);                 // 14-17 size of image data
                        bw.Write(22);                                // 18-21 offset of image data
                        bw.Write(msImg.ToArray());                   // write image data
                    }
                }
                bw.Flush();
                bw.Seek(0, System.IO.SeekOrigin.Begin);
                msIco.Seek(0, System.IO.SeekOrigin.Begin);
                return new Icon(msIco);
            }
        }
        /// <summary>
        /// Z dodaných zdrojů najde a vybere ikonu vhodné velikosti, konvertuje ji na Bitmapu a vrátí ji.
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Image _ConvertIconToBitmap(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType? sizeType)
        {
            Image image = null;
            if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, sizeType, out var resourceItem) && resourceItem.IsIcon)
                image = _ConvertIconToBitmap(resourceItem, sizeType);
            return image;
        }
        /// <summary>
        /// Z dodaného zdroje vezme ikonu, konvertuje ji na Bitmapu a vrátí ji.
        /// </summary>
        /// <param name="resourceItem"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Image _ConvertIconToBitmap(DxApplicationResourceLibrary.ResourceItem resourceItem, ResourceImageSizeType? sizeType)
        {
            Image image = null;
            if (resourceItem != null && resourceItem.IsIcon)
            {
                using (var icon = resourceItem.CreateIconImage())
                    image = icon.ToBitmap();
            }
            return image;
        }
        /// <summary>
        /// Disposuje Images v daném poli
        /// </summary>
        /// <param name="imageInfos"></param>
        private void _DisposeImages(List<Tuple<Size, Image>> imageInfos)
        {
            if (imageInfos == null || imageInfos.Count == 0) return;
            foreach (var imageInfo in imageInfos)
                imageInfo?.Item2?.Dispose();
            imageInfos.Clear();
        }
        #endregion
        #region TryGetResource - hledání aplikačního zdroje
        /// <summary>
        /// Metoda se pokusí najít zdroj v Aplikačních zdrojích, pro dané jméno.
        /// Prohledává obrázky vektorové a bitmapové, může preferovat vektory - pokud <paramref name="isPreferredVectorImage"/> je true.
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="resourceItem">Výstup - nalezeného zdroje</param>
        /// <param name="sizeType">Vyhledat danou velikost, default = Large</param>
        /// <param name="isPreferredVectorImage">Preferovat true = vektory / false = bitmapy / null = podle systému</param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static bool TryGetApplicationResource(string imageName, out DxApplicationResourceLibrary.ResourceItem resourceItem, bool exactName = false, ResourceImageSizeType? sizeType = null, bool? isPreferredVectorImage = null)
        { return Instance._TryGetApplicationResource(imageName, exactName, sizeType, isPreferredVectorImage, out resourceItem); }
        /// <summary>
        /// Zkusí najít jeden nejvhodnější zdroj
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="isPreferredVectorImage"></param>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        private bool _TryGetApplicationResource(string imageName, bool exactName, ResourceImageSizeType? sizeType, bool? isPreferredVectorImage, out DxApplicationResourceLibrary.ResourceItem resourceItem)
        {
            resourceItem = null;
            ResourceContentType[] validContentTypes = _GetValidImageContentTypes(isPreferredVectorImage);
            if (!_TryGetApplicationResources(imageName, exactName, out var validItems, validContentTypes)) return false;
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(validItems, sizeType, out resourceItem)) return false;
            return true;
        }

        /// <summary>
        /// Metoda se pokusí najít požadované zdroje v Aplikačních zdrojích, pro dané jméno.
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="exactName"></param>
        /// <param name="resourceItems">Výstup - nalezené zdroje</param>
        /// <param name="contentTypes"></param>
        /// <returns></returns>
        public static bool TryGetApplicationResources(string imageName, bool exactName, out DxApplicationResourceLibrary.ResourceItem[] resourceItems, params ResourceContentType[] contentTypes)
        { return Instance._TryGetApplicationResources(imageName, exactName, out resourceItems, contentTypes); }
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
            try
            {
                if (hasImage)
                {
                    _ApplyImageRaw(imageOptions, image);
                }
                else if (hasName || hasCaption)
                {
                    if (hasName && DxSvgImage.TryGetXmlContent(imageName, sizeType, out var dxSvgImage))
                        _ApplyDxSvgImage(imageOptions, dxSvgImage);
                    else if (hasName && SvgImageSupport.TryGetSvgImageArray(imageName, out var svgImageArray))
                        _ApplyImageArray(imageOptions, svgImageArray, sizeType, imageSize);
                    else if (hasName && _ExistsApplicationResource(imageName, exactName))
                        _ApplyImageApplication(imageOptions, imageName, exactName, sizeType, imageSize);
                    else if (hasName && _ExistsDevExpressResource(imageName))
                        _ApplyImageDevExpress(imageOptions, imageName, sizeType, imageSize);
                    else if (hasCaption)
                        _ApplyImageForCaption(imageOptions, caption, sizeType, imageSize);
                }
                else
                {
                    imageOptions.SvgImage = null;
                    imageOptions.Image = null;
                }
            }
            catch { /* Někdy může dojít k chybě uvnitř DevExpress. I jejich vývojáři jsou jen lidé... */ }

            // Malá služba nakonec:
            if (smallButton && imageOptions is SimpleButtonImageOptions buttonImageOptions)
                buttonImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
        }
        /// <summary>
        /// Aplikuje dodanou bitmapu do <see cref="ImageOptions"/>
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="image"></param>
        private void _ApplyImageRaw(ImageOptions imageOptions, Image image)
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
        /// <summary>
        /// Aplikuje explicitně dodaný <see cref="SvgImage"/> do daného objektu
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="dxSvgImage"></param>
        private void _ApplyDxSvgImage(ImageOptions imageOptions, DxSvgImage dxSvgImage)
        {
            if (imageOptions == null || dxSvgImage == null) return;
            imageOptions.Reset();
            imageOptions.SvgImage = dxSvgImage;
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
            if (svgImageArray == null) return;

            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Má prostor pro dvě velikosti obrázku najednou:
                barOptions.Image = null;
                barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    int smallIndex = _GetVectorImageIndex(svgImageArray, ResourceImageSizeType.Small);
                    int largeIndex = _GetVectorImageIndex(svgImageArray, ResourceImageSizeType.Large);
                    if (smallIndex >= 0 && largeIndex >= 0)
                    {   // Máme indexy pro obě velikosti?
                        barOptions.SvgImage = null;
                        barOptions.SvgImageSize = Size.Empty;
                        barOptions.ImageIndex = smallIndex;
                        barOptions.LargeImageIndex = largeIndex;
                        hasIndexes = true;
                    }
                }
                if (!hasIndexes)
                {
                    barOptions.SvgImage = _GetVectorImageArray(svgImageArray, sizeType);
                    barOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                iciOptions.Image = null;
                if (iciOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                    iciOptions.SvgImage = null;
                    iciOptions.SvgImageSize = Size.Empty;
                    iciOptions.ImageIndex = _GetVectorImageIndex(svgImageArray, sizeType ?? ResourceImageSizeType.Large);
                }
                else
                {   // Musíme tam dát přímo SvgImage:
                    iciOptions.SvgImage = _GetVectorImageArray(svgImageArray, sizeType);
                    iciOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                imageOptions.Image = null;
                imageOptions.SvgImage = _GetVectorImageArray(svgImageArray, sizeType);
                imageOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
            }
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
            if (!_TryGetApplicationResources(imageName, exactName, out var validItems, ResourceContentType.Vector, ResourceContentType.Bitmap, ResourceContentType.Icon)) return;

            var contentType = validItems[0].ContentType;
            switch (contentType)
            {
                case ResourceContentType.Vector:
                    _ApplyImageApplicationSvg(imageOptions, validItems, sizeType, imageSize);
                    break;
                case ResourceContentType.Bitmap:
                    _ApplyImageApplicationBmp(imageOptions, validItems, sizeType, imageSize);
                    break;
                case ResourceContentType.Icon:
                    _ApplyImageApplicationIco(imageOptions, validItems, sizeType, imageSize);
                    break;
            }
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
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Máme prostor pro dvě velikosti obrázku najednou:
                // barOptions.Reset();
                if (barOptions.Image != null) barOptions.Image = null;
                if (barOptions.LargeImage != null) barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    int smallIndex = _GetVectorImageIndex(resourceItems, ResourceImageSizeType.Small);
                    int largeIndex = _GetVectorImageIndex(resourceItems, ResourceImageSizeType.Large);
                    if (smallIndex >= 0 && largeIndex >= 0)
                    {   // Máme indexy pro obě velikosti?
                        if (barOptions.SvgImage != null) barOptions.SvgImage = null;
                        // Radši ne, jinde dochází k chybě, pokud SvgImage je null :    barOptions.SvgImageSize = Size.Empty;
                        if (barOptions.ImageIndex != smallIndex) barOptions.ImageIndex = smallIndex;
                        if (barOptions.LargeImageIndex != largeIndex) barOptions.LargeImageIndex = largeIndex;
                        hasIndexes = true;
                    }
                }
                if (!hasIndexes)
                {
                    barOptions.SvgImage = _GetVectorImageApplication(resourceItems, sizeType);
                    if (barOptions.SvgImage != null)
                        barOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                if (iciOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                    try
                    {   // Tady dochází k chybě v DevExpress v situaci, kdy provádím Refresh obrázku. Nevím proč...
                        if (iciOptions.SvgImage != null) iciOptions.SvgImage = null;
                        int imageIndex = _GetVectorImageIndex(resourceItems, sizeType ?? ResourceImageSizeType.Large);
                        if (iciOptions.ImageIndex != imageIndex)
                            iciOptions.ImageIndex = imageIndex;
                    }
                    catch { }
                }
                else
                {   // Musíme tam dát přímo SvgImage:
                    iciOptions.SvgImage = _GetVectorImageApplication(resourceItems, sizeType);
                    if (iciOptions.SvgImage != null)
                        iciOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                imageOptions.Image = null;
                imageOptions.SvgImage = _GetVectorImageApplication(resourceItems, sizeType);
                if (imageOptions.SvgImage != null)
                    imageOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
            }
        }
        /// <summary>
        /// Vrátí velikost obrázku SvgImage pro <see cref="ImageOptions.SvgImageSize"/>
        /// </summary>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        private Size _GetVectorSvgImageSize(ResourceImageSizeType? sizeType, Size? imageSize)
        {
            if (imageSize.HasValue) return imageSize.Value;
            if (sizeType.HasValue) return DxComponent.GetImageSize(sizeType.Value, true);
            return Size.Empty;
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
            imageOptions.Image = _CreateBitmapImageApplication(resourceItems, sizeType, imageSize);
        }
        /// <summary>
        /// Aplikuje Image typu Icon ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageApplicationIco(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _ConvertIconToBitmap(resourceItems, sizeType);
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
        #region ApplyIcon - do cílového formuláře vepíše obrázek podle toho, jaký je zadán a jaký je to formulář
        /// <summary>
        /// Do daného okna aplikuje danou ikonu.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="iconName"></param>
        /// <param name="sizeType"></param>
        /// <param name="forceToIcon">Povinně vkládat ikonu do <see cref="Form.Icon"/> i kdyby byl k dispozici objekt <see cref="XtraForm.IconOptions"/></param>
        public static void ApplyIcon(Form form, string iconName, ResourceImageSizeType? sizeType = null, bool forceToIcon = false) { Instance._ApplyIcon(form, iconName, sizeType, forceToIcon); }
        /// <summary>
        /// Do daného okna aplikuje danou ikonu.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="iconName"></param>
        /// <param name="sizeType"></param>
        /// <param name="forceToIcon">Povinně vkládat ikonu do <see cref="Form.Icon"/> i kdyby byl k dispozici objekt <see cref="XtraForm.IconOptions"/></param>
        private void _ApplyIcon(Form form, string iconName, ResourceImageSizeType? sizeType, bool forceToIcon = false)
        {
            if (form is null || String.IsNullOrEmpty(iconName)) return;
            if (!sizeType.HasValue) sizeType = ResourceImageSizeType.Large;

            if (!forceToIcon && form is DevExpress.XtraEditors.XtraForm xtraForm)
                ApplyImage(xtraForm.IconOptions, iconName, sizeType: sizeType);
            else
                _ApplyIconToForm(form, _CreateIconImage(iconName, false, sizeType));
        }
        /// <summary>
        /// Do daného formu vloží danou ikonu.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="icon"></param>
        private void _ApplyIconToForm(Form form, Icon icon)
        {
            if (form is DevExpress.XtraEditors.XtraForm xtraForm)
            {
                xtraForm.IconOptions.Reset();
                xtraForm.IconOptions.SvgImage = null;
            }
            form.Icon = icon;
        }
        #endregion
        #region PreferredImageList - Seznam obrázků typu Bitmapa/Vektor, pro použití v controlech; GetPreferredImageList, GetPreferredImageIndex
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/> anebo <see cref="SvgImageCollection"/>, obsahující bitmapové nebo vektorové obrázky dané velikosti.
        /// Volbu Bitmapa / Vektor provádí podle hodnoty <see cref="IsPreferredVectorImage"/>.
        /// Používá se pro ty DevExpress controly, které podporují ImageList typu Object, například Ribbon.
        /// Následně je možno do jednotlivých prvků objektu (typicky BarItem) vkládat index obrázku do ImageIndex / LargeImageIndex.
        /// <para/>
        /// Související metodou je <see cref="GetPreferredImageIndex(string, ResourceImageSizeType, Size?, string, bool)"/>.
        /// Ta metoda vrátí index z ImageList preferovaného typu (je to po celou dobu běhu aplikace shodný typ: Bitmap nebo Vector).
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static object GetPreferredImageList(ResourceImageSizeType sizeType)
        { return Instance._GetPreferredImageList(sizeType); }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private object _GetPreferredImageList(ResourceImageSizeType sizeType)
        {
            if (this._IsPreferredVectorImage)
                return this._GetVectorImageList(sizeType);
            else
                return this._GetBitmapImageList(sizeType);
        }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího <see cref="ImageList"/> anebo <see cref="SvgImageCollection"/>, obsahující bitmapové nebo vektorové obrázky dané velikosti.
        /// Volbu Bitmapa / Vektor provádí podle hodnoty <see cref="IsPreferredVectorImage"/>.
        /// Používá se pro ty DevExpress controly, které podporují ImageList typu Object, například Ribbon.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// Pokud není vygenerován náhradní obrázek (caption je prázdné), vrací se -1.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static int GetPreferredImageIndex(string imageName, ResourceImageSizeType sizeType, Size? optimalSvgSize = null, string caption = null, bool exactName = false)
        { return Instance._GetPreferredImageIndex(imageName, exactName, sizeType, optimalSvgSize, caption); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetPreferredImageIndex(string imageName, bool exactName, ResourceImageSizeType sizeType, Size? optimalSvgSize, string caption)
        {
            if (this._IsPreferredVectorImage)
                return this._GetVectorImageIndex(imageName, exactName, sizeType, caption);
            else
                return this._GetBitmapImageIndex(imageName, exactName, sizeType, optimalSvgSize, caption);
        }
        #endregion
        #region BitmapImageList - Seznam obrázků typu Bitmapa, pro použití v controlech; GetBitmapImageList, GetBitmapImageIndex, GetBitmapImage
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static ImageList GetBitmapImageList(ResourceImageSizeType sizeType)
        { return Instance._GetBitmapImageList(sizeType); }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private ImageList _GetBitmapImageList(ResourceImageSizeType sizeType)
        {
            return _GetDxBitmapImageList(sizeType).ImageList;
        }
        /// <summary>
        /// Vrací objekt <see cref="DxBmpImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private DxBmpImageList _GetDxBitmapImageList(ResourceImageSizeType sizeType)
        {
            if (__BitmapImageList == null) __BitmapImageList = new Dictionary<ResourceImageSizeType, DxBmpImageList>();   // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var imageListDict = __BitmapImageList;
            if (!imageListDict.TryGetValue(sizeType, out DxBmpImageList dxBmpImageList))
            {
                lock (imageListDict)
                {
                    if (!imageListDict.TryGetValue(sizeType, out dxBmpImageList))
                    {
                        dxBmpImageList = new DxBmpImageList(sizeType);
                        dxBmpImageList.ImageSize = _GetVectorSvgImageSize(sizeType, null);
                        dxBmpImageList.ColorDepth = ColorDepth.Depth32Bit;
                        imageListDict.Add(sizeType, dxBmpImageList);
                    }
                }
            }
            return dxBmpImageList;
        }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static int GetBitmapImageIndex(string imageName, ResourceImageSizeType sizeType, Size? optimalSvgSize = null, string caption = null, bool exactName = false)
        { return Instance._GetBitmapImageIndex(imageName, exactName, sizeType, optimalSvgSize, caption); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetBitmapImageIndex(string imageName, bool exactName, ResourceImageSizeType sizeType, Size? optimalSvgSize, string caption)
        {
            var imageInfo = _GetBitmapImageListItem(imageName, exactName, sizeType, optimalSvgSize, caption);
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
        /// <param name="caption"></param>
        /// <param name="exactName"></param>
        /// <returns></returns>
        public static Image GetBitmapImage(string imageName, ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null, string caption = null, bool exactName = false)
        { return Instance._GetBitmapImage(imageName, exactName, sizeType ?? ResourceImageSizeType.Large, optimalSvgSize, caption); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <paramref name="caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Image _GetBitmapImage(string imageName, bool exactName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            string caption)
        {
            var imageInfo = _GetBitmapImageListItem(imageName, exactName, sizeType, optimalSvgSize, caption);
            return ((imageInfo == null || imageInfo.Item1 == null || imageInfo.Item2 < 0) ? null : imageInfo.Item1.Images[imageInfo.Item2]);
        }
        /// <summary>
        /// Metoda vyhledá, zda daný obrázek existuje
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private Tuple<ImageList, int> _GetBitmapImageListItem(string imageName, bool exactName,
            ResourceImageSizeType sizeType, Size? optimalSvgSize,
            string caption)
        {
            bool hasName = !String.IsNullOrEmpty(imageName);
            string captionKey = DxComponent.GetCaptionForIcon(caption);
            bool hasCaption = (captionKey.Length > 0);
            string key = (hasName ? imageName.ToLower() : hasCaption ? $"«:{captionKey}:»" : "").Trim();
            if (key.Length == 0) return null;

            var dxImageList = _GetDxBitmapImageList(sizeType);
            int index = -1;
            if (dxImageList.ContainsKey(key))
            {
                index = dxImageList.IndexOfKey(key);
            }
            else if (hasName || hasCaption)
            {
                Image image = _CreateBitmapImage(imageName, exactName, sizeType, optimalSvgSize, caption, null);
                if (image != null)
                {
                    bool isColorized = !hasName && hasCaption;       // Image lze přebarvovat tehdy, když nepochází z obrázku, ale z Caption
                    dxImageList.Add(key, image, isColorized, imageName, exactName, optimalSvgSize, caption);
                    index = dxImageList.IndexOfKey(key);
                }
            }
            return (index >= 0 ? new Tuple<ImageList, int>(dxImageList.ImageList, index) : null);
        }
        /// <summary>
        /// Dictionary ImageListů - pro každou velikost <see cref="ResourceImageSizeType"/> je jedna instance
        /// </summary>
        private Dictionary<ResourceImageSizeType, DxBmpImageList> __BitmapImageList;
        #endregion
        #region VectorImageList - Seznam obrázků typu Vector, pro použití v controlech; GetVectorImageList, GetVectorImageIndex
        /// <summary>
        /// Vrátí kolekci SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje. Pro danou cílovou velikost.
        /// </summary>
        /// <param name="sizeType"></param>
        public static SvgImageCollection GetVectorImageList(ResourceImageSizeType sizeType) { return Instance._GetVectorImageList(sizeType); }
        /// <summary>
        /// Vrátí kolekci SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje. Pro danou cílovou velikost.
        /// </summary>
        /// <param name="sizeType"></param>
        private DxSvgImageCollection _GetVectorImageList(ResourceImageSizeType sizeType)
        {
            if (__VectorImageList == null) __VectorImageList = new Dictionary<ResourceImageSizeType, DxSvgImageCollection>();      // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var svgImageCollections = __VectorImageList;
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
        /// Najde a vrátí index ID pro vektorový obrázek daného jména, obrázek je uložen v kolekci <see cref="SvgImageCollection"/>
        /// </summary>
        /// <param name="imageName">Jméno obrázku</param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
        /// <param name="exactName"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static int GetVectorImageIndex(string imageName, ResourceImageSizeType sizeType, bool exactName = false, string caption = null) { return Instance._GetVectorImageIndex(imageName, exactName, sizeType, caption); }
        /// <summary>
        /// Najde a vrátí index ID pro vektorový obrázek daného jména, obrázek je uložen v kolekci <see cref="SvgImageCollection"/>
        /// </summary>
        /// <param name="imageName">Jméno obrázku</param>
        /// <param name="exactName"></param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private int _GetVectorImageIndex(string imageName, bool exactName, ResourceImageSizeType sizeType, string caption)
        {
            var svgCollection = _GetVectorImageList(sizeType);
            return svgCollection.GetImageId(imageName, n => _GetVectorImage(n, exactName, sizeType, caption));
        }
        /// <summary>
        /// Najde a vrátí index ID pro vhodný vektorový obrázek z dodaných zdrojů, pro danou velikost.
        /// </summary>
        /// <param name="resourceItems">Dodané zdroje různých velikostí</param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
        /// <returns></returns>
        private int _GetVectorImageIndex(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceImageSizeType sizeType)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, sizeType, out var resourceItem)) return -1;
            var svgCollection = _GetVectorImageList(sizeType);
            return svgCollection.GetImageId(resourceItem.ItemKey, n => resourceItem.CreateSvgImage());
        }
        /// <summary>
        /// Najde a vrátí index ID pro dodaný vektorový obrázek z ImageArray.
        /// </summary>
        /// <param name="svgImageArray">Kombinovaná ikona</param>
        /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
        /// <returns></returns>
        private int _GetVectorImageIndex(SvgImageArrayInfo svgImageArray, ResourceImageSizeType sizeType)
        {
            if (svgImageArray is null) return -1;
            var svgCollection = _GetVectorImageList(sizeType);
            string key = svgImageArray.Key;
            return svgCollection.GetImageId(key, n => _GetVectorImageArray(svgImageArray, sizeType));
        }
        /// <summary>Kolekce SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje, instanční proměnná.</summary>
        private Dictionary<ResourceImageSizeType, DxSvgImageCollection> __VectorImageList;
        #endregion
        #region VectorArray: více vektorových ikon v jednom názvu Image
        /// <summary>
        /// Vytvoří a vrátí bitmapu z pole vektorových image
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageArray(SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType, Size? optimalSvgSize)
        {
            var svgImage = SvgImageSupport.CreateSvgImage(svgImageArray, sizeType);
            return _ConvertVectorToImage(svgImage, sizeType, optimalSvgSize);
        }
        /// <summary>
        /// Vyrenderuje dané pole vektorových image do bitmapy a z ní vytvoří a vrátí Icon
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Icon _ConvertImageArrayToIcon(SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType)
        {
            var svgImage = SvgImageSupport.CreateSvgImage(svgImageArray, sizeType);
            using (var bitmap = _ConvertVectorToImage(svgImage, sizeType, null))
                return _ConvertBitmapToIcon(bitmap);
        }
        /// <summary>
        /// Vrátí SVG Image typu Array
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetVectorImageArray(SvgImageArrayInfo svgImageArray, ResourceImageSizeType? sizeType)
        {
            return SvgImageSupport.CreateSvgImage(svgImageArray, sizeType);
        }
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
        private bool _TryGetContentTypeApplication(DxApplicationResourceLibrary.ResourceItem[] validItems, ResourceImageSizeType sizeType, bool? preferVector, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            bool isPreferredVector = preferVector ?? this._IsPreferredVectorImage;
            if (validItems == null || validItems.Length == 0) return false;
            if (isPreferredVector && validItems.Any(i => i.ContentType == ResourceContentType.Vector))
                contentType = ResourceContentType.Vector;
            else if (validItems.Any(i => i.ContentType == ResourceContentType.Bitmap))
                contentType = ResourceContentType.Bitmap;
            else if (!isPreferredVector && validItems.Any(i => i.ContentType == ResourceContentType.Vector))
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
        /// Zkusí najít daný zdroj v aplikačních zdrojích, zafiltruje na daný typ obsahu 
        /// - pouze vektorové a/nebo bitmapové obrázky, podle <see cref="DxComponent.IsPreferredVectorImage"/>, typ obsahu je povinný. 
        /// Velikost se řeší následně.
        /// <para/>
        /// Pokud tedy systém preferuje vektorové obrázky, pak primárně hledá vektorové, a teprve když je nenajde, tak hledá bitmapové.
        /// A naopak, pokud systém preferuje bitmapy, pak se zde prioritně hledají bitmapy, a až v druhé řadě se akceptují vektory.
        /// <para/>
        /// Pokud vrátí true, pak v poli <paramref name="validItems"/> je nejméně jeden prvek.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="validItems"></param>
        /// <returns></returns>
        private bool _TrySearchApplicationPreferredImageResource(string imageName, bool exactName, out DxApplicationResourceLibrary.ResourceItem[] validItems)
        {
            ResourceContentType[] validContentTypes = this._IsPreferredVectorImage ?
                new ResourceContentType[] { ResourceContentType.Vector, ResourceContentType.Bitmap } :
                new ResourceContentType[] { ResourceContentType.Bitmap, ResourceContentType.Vector };
            return _TryGetApplicationResources(imageName, exactName, out validItems, validContentTypes);
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
        private bool _TryGetApplicationResources(string imageName, bool exactName, out DxApplicationResourceLibrary.ResourceItem[] validItems,
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
        /// Vrátí pole se dvěma prvky, popisující <see cref="ResourceContentType"/> typu Image, v pořadí preferujícím vektory (pokud na vstupu je true) nebo bitmapy (pro false).
        /// Pokud na vstupu je null nebo nic, pak se postupuje podle <see cref="_IsPreferredVectorImage"/>.
        /// </summary>
        /// <param name="isPreferredVectorImage">Preference vektorů: true = vektory; false = bitmapy, null = podle konfigurace</param>
        /// <returns></returns>
        private ResourceContentType[] _GetValidImageContentTypes(bool? isPreferredVectorImage = null)
        {
            bool isPreferVector = isPreferredVectorImage ?? _IsPreferredVectorImage;
            return (isPreferVector ?
                    new ResourceContentType[] { ResourceContentType.Vector, ResourceContentType.Bitmap, ResourceContentType.Icon } :
                    new ResourceContentType[] { ResourceContentType.Bitmap, ResourceContentType.Vector, ResourceContentType.Icon });
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
        /// <returns></returns>
        private Image _CreateImageApplication(string imageName, bool exactName,
            ResourceImageSizeType? sizeType, Size? optimalSvgSize)
        {
            if (!_TryGetApplicationResources(imageName, exactName, out var validItems, ResourceContentType.Bitmap, ResourceContentType.Vector)) return null;
            return _CreateBitmapImageApplication(validItems, sizeType, optimalSvgSize);
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů.
        /// Na vstupu (<paramref name="resourceItems"/>) je seznam zdrojů, z nich bude vybrán zdroj vhodné velikosti.
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems,
            ResourceImageSizeType? sizeType, Size? optimalSvgSize)
        {
            // Vezmu jediný zdroj anebo vyhledám optimální zdroj pro danou velikost:
            if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, sizeType, out var resourceItem))
            {
                switch (resourceItem.ContentType)
                {
                    case ResourceContentType.Bitmap:
                        return resourceItem.CreateBmpImage();
                    case ResourceContentType.Vector:
                        return _ConvertVectorToImage(resourceItem.CreateSvgImage(), sizeType, optimalSvgSize);
                    case ResourceContentType.Icon:
                        return _ConvertIconToBitmap(resourceItem, sizeType);
                }
            }
            return null;
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
        /// <summary>
        /// Konveruje (renderuje) dodaný aplikační vektorový obrázek na Bitmapu = Image
        /// </summary>
        /// <param name="vectorItem"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private Image _ConvertApplicationVectorToImage(DxApplicationResourceLibrary.ResourceItem vectorItem, ResourceImageSizeType? sizeType)
        {
            var svgImage = vectorItem.CreateSvgImage();
            return _ConvertVectorToImage(svgImage, sizeType, null);
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
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpress(string imageName, ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null)
        {
            if (_IsImageNameSvg(imageName))
                return _CreateBitmapImageDevExpressSvg(imageName, sizeType, optimalSvgSize);
            else
                return _CreateBitmapImageDevExpressPng(imageName, sizeType, optimalSvgSize);
        }
        /// <summary>
        /// Vrátí bitmapu z obrázku typu SVG uloženou v DevExpress zdrojích.
        /// Tato metoda již netestuje existenci zdroje, to má provést někdo před voláním této metody.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="optimalSvgSize"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpressSvg(string imageName, ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null)
        {
            string resourceName = _GetDevExpressResourceKey(imageName);
            Size size = optimalSvgSize ?? GetImageSize(sizeType);

            var svgPalette = GetSvgPalette();
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
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpressPng(string imageName, ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null)
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
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// Pokud vrátí null, zkusí se provést <see cref="CreateCaptionImage(string, ResourceImageSizeType?, Size?)"/>.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static SvgImage CreateCaptionVector(string caption, ResourceImageSizeType? sizeType = null)
        { return Instance._CreateCaptionVector(caption, sizeType ?? ResourceImageSizeType.Large); }
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// Pokud vrátí null, zkusí se provést <see cref="CreateCaptionImage(string, ResourceImageSizeType?, Size?)"/>.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private SvgImage _CreateCaptionVector(string caption, ResourceImageSizeType sizeType)
        {
            return DxSvgImage.CreateCaptionVector(caption, sizeType);
        }
        /// <summary>
        /// Najde a vrátí index pro SVG image pro daný text Caption. Nebo SVG image vytvoří, uloží a vrátí jeho index.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private int _GetCaptionVectorIndex(string caption, ResourceImageSizeType sizeType)
        {
            var svgCollection = _GetVectorImageList(sizeType);
            string text = DxComponent.GetCaptionForIcon(caption);
            string key = "*" + text;
            return svgCollection.GetImageId(key, n => _CreateCaptionVector(caption, sizeType));
        }
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        public static Image CreateCaptionImage(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            string text = DxComponent.GetCaptionForIcon(caption);                    // Odeberu mezery a nepísmenové znaky
            if (text.Length == 0) return null;

            if (!sizeType.HasValue) sizeType = ResourceImageSizeType.Large;
            bool isDark = DxComponent.IsDarkTheme;
            Color backColor = (isDark ? SvgImageCustomize.DarkColor38 : SvgImageCustomize.LightColorFF);
            Color textColor = (isDark ? SvgImageCustomize.LightColorD4 : SvgImageCustomize.DarkColor38);
            Color lineColor = textColor;

            var realSize = imageSize ?? DxComponent.GetImageSize(sizeType.Value, true);
            Bitmap bitmap = new Bitmap(realSize.Width, realSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                RectangleF bounds = new RectangleF(0, 0, realSize.Width, realSize.Height);
                graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColor), bounds);

                Rectangle borderBounds = Rectangle.Truncate(bounds);
                borderBounds.Width--;
                borderBounds.Height--;
                graphics.DrawRectangle(DxComponent.PaintGetPen(lineColor), borderBounds);

                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var sysFont = _ButtonFont;
                Rectangle textBounds = Rectangle.Ceiling(bounds);
                textBounds.X = textBounds.X - 1;
                textBounds.Width = textBounds.Width + 2;
                using (var stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    if (sizeType.Value != ResourceImageSizeType.Large)
                    {   // Malé a střední ikony
                        if (text.Length > 2) text = text.Substring(0, 2);                // Radši bych tu viděl jen jedno písmenko, ale JD...
                        using (Font font = new Font(sysFont.FontFamily, sysFont.Size * 1.0f))
                        {
                            //var textSize = graphics.MeasureString(text, font);
                            //var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                            graphics.DrawString(text, font, DxComponent.PaintGetSolidBrush(textColor), textBounds, stringFormat);
                        }
                    }
                    else
                    {   // Velké ikony
                        if (text.Length > 2) text = text.Substring(0, 2);
                        using (Font font = new Font(sysFont.FontFamily, sysFont.Size * 1.4f))
                        {
                            //var textSize = graphics.MeasureString(text, font);
                            //var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                            graphics.DrawString(text, font, DxComponent.PaintGetSolidBrush(textColor), textBounds, stringFormat);
                        }
                    }
                }
            }
            return bitmap;
        }
        /// <summary>
        /// Písmo použité v Ribbonu
        /// </summary>
        private static Font _RibbonFont
        {
            get
            {
                Font font = null;
                var skin = DxComponent.GetSkinInfo(SkinElementColor.RibbonSkins);
                if (skin != null)
                    font = skin[DevExpress.Skins.RibbonSkins.SkinButton]?.GetDefaultFont(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel);
                if (font == null)
                    font = SystemFonts.MenuFont;
                return font;
            }
        }
        /// <summary>
        /// Písmo použité v Buttonu
        /// </summary>
        private static Font _ButtonFont
        {
            get
            {
                Font font = null;
                var skin = DxComponent.GetSkinInfo(SkinElementColor.CommonSkins);
                if (skin != null)
                    font = skin[DevExpress.Skins.CommonSkins.SkinButton]?.GetDefaultFont(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel);
                if (font == null)
                    font = SystemFonts.DefaultFont;
                return font;
            }
        }
        /// <summary>
        /// Do daného objektu vloží náhradní ikonu pro daný text
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        private void _ApplyImageForCaption(ImageOptions imageOptions, string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        {
            bool useVectorImage = false;
            if (useVectorImage)
            {
                if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
                {   // Má prostor pro dvě velikosti obrázku najednou:
                    barOptions.Image = null;
                    barOptions.LargeImage = null;

                    bool hasIndexes = false;
                    if (barOptions.Images is SvgImageCollection)
                    {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                        int smallIndex = _GetCaptionVectorIndex(caption, ResourceImageSizeType.Small);
                        int largeIndex = _GetCaptionVectorIndex(caption, ResourceImageSizeType.Large);
                        if (smallIndex >= 0 && largeIndex >= 0)
                        {   // Máme indexy pro obě velikosti?
                            barOptions.SvgImage = null;
                            barOptions.SvgImageSize = Size.Empty;
                            barOptions.ImageIndex = smallIndex;
                            barOptions.LargeImageIndex = largeIndex;
                            hasIndexes = true;
                        }
                    }
                    if (!hasIndexes)
                    {
                        barOptions.SvgImage = _CreateCaptionVector(caption, sizeType ?? ResourceImageSizeType.Large);
                        barOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                    }
                }
                else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
                {   // Může využívat Index:
                    iciOptions.Image = null;
                    if (iciOptions.Images is SvgImageCollection)
                    {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                        iciOptions.SvgImage = null;
                        iciOptions.SvgImageSize = Size.Empty;
                        iciOptions.ImageIndex = _GetCaptionVectorIndex(caption, sizeType ?? ResourceImageSizeType.Large);
                    }
                    else
                    {   // Musíme tam dát přímo SvgImage:
                        iciOptions.SvgImage = _CreateCaptionVector(caption, sizeType ?? ResourceImageSizeType.Large);
                        iciOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                    }
                }
                else
                {   // Musíme vepsat přímo jeden obrázek:
                    imageOptions.Image = null;
                    imageOptions.SvgImage = _CreateCaptionVector(caption, sizeType ?? ResourceImageSizeType.Large);
                    imageOptions.SvgImageSize = _GetVectorSvgImageSize(sizeType, imageSize);
                }
            }
            else
            {
                imageOptions.Reset();
                if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
                {   // Má prostor pro dvě velikosti obrázku najednou:
                    barOptions.Image = _GetBitmapImage(null, false, ResourceImageSizeType.Small, null, caption);       // CreateCaptionImage(caption, ResourceImageSizeType.Small, imageSize);
                    barOptions.LargeImage = _GetBitmapImage(null, false, ResourceImageSizeType.Large, null, caption);  // CreateCaptionImage(caption, ResourceImageSizeType.Large, imageSize);
                }
                else
                {
                    imageOptions.Image = _GetBitmapImage(null, false, sizeType ?? ResourceImageSizeType.Small, null, caption); // CreateCaptionImage(caption, sizeType ?? ResourceImageSizeType.Large, imageSize);
                }
            }
        }
        /// <summary>
        /// Vrátí text pro danou caption pro renderování ikony.
        /// Z textu odstraní mezery a znaky - _ + / * #
        /// Pokud výsledek bude delší než 2 znaky, zkrátí jej na dva znaky.
        /// Pokud na vstupu je null, na výstupu je prázdný string (Length = 0). Stejně tak, pokud na vstupu bude string obsahující jen odstraněný balast.
        /// <para/>
        /// Tato metoda vracá UPPER-CASE.
        /// </summary>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static string GetCaptionForIcon(string caption)
        {
            if (String.IsNullOrEmpty(caption)) return "";

            // Pravidla podle JD:
            // 1. Pokud v textu najdu pomlčku (nebo čárku, středník, tečku, plus...) tak rozdělím text na část před a za;
            // 2. Pokud ne (bod 1), tak hledám mezeru, Tab, Eol a rozdělím text tam
            // 3. Pokud uspěju v bodech 1 nebo 2, pak vezmu první písmeno z první a z druhé části a udělám je velká
            // 4. Pokud neuspěju, tak vezmu první dva znaky a neměním velikost
            string delimiters0 = "-,;.+#*/_";
            string delimiters1 = " \t\r\n";
            caption = caption.Trim((delimiters0 + delimiters1).ToCharArray());     // Na krajích nechci oddělovače
            int length = caption.Length;

            if (length <= 2) return caption;      // Tudy odejde text složený jen z oddělovačů, a taky krátký text do dvou znaků, z něj nic lepšího neuděláme...

            int index = caption.IndexOfAny(delimiters0.ToCharArray());
            if (index < 0)
                index = caption.IndexOfAny(delimiters1.ToCharArray());

            if (index > 0 && index < (length - 1))
            {   // Dvě oddělené věty, anebo dvě (a více) slov => první písmena, velká:
                string text0 = caption.Substring(0, index).Trim();
                string text1 = caption.Substring(index + 1).Trim();
                if (text0.Length > 0 && text1.Length > 0)
                    return (text0.Substring(0, 1) + text1.Substring(0, 1)).ToUpper();
            }

            // V podstatě jedno slovo => první dvě písmena slova, beze změny velikosti:
            return caption.Substring(0, 2);
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
        public static bool TryGetResourceContentType(string imageName, ResourceImageSizeType sizeType, out ResourceContentType contentType, bool? preferVector = null, bool exactName = false)
        { return Instance._TryGetResourceContentType(imageName, exactName, sizeType, preferVector, out contentType); }
        private bool _TryGetResourceContentType(string imageName, bool exactName, ResourceImageSizeType sizeType, bool? preferVector, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (String.IsNullOrEmpty(imageName)) return false;

            if (_TryGetContentTypeImageArray(imageName, out contentType, out var _))
                return true;
            if (_TryGetApplicationResources(imageName, exactName, out var validItems))
                return _TryGetContentTypeApplication(validItems, sizeType, preferVector, out contentType);
            if (_ExistsDevExpressResource(imageName))
                return _TryGetContentTypeDevExpress(imageName, out contentType);

            return false;
        }
        /// <summary>
        /// Metoda zjistí, zda daný název Image odpovídá kombinované SVG ikoně.
        /// Pokud ano, pak sestavenou ikonu ukládá do out parametru <paramref name="svgImageArray"/>.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="contentType"></param>
        /// <param name="svgImageArray"></param>
        /// <returns></returns>
        private bool _TryGetContentTypeImageArray(string imageName, out ResourceContentType contentType, out SvgImageArrayInfo svgImageArray)
        {
            if (SvgImageSupport.TryGetSvgImageArray(imageName, out svgImageArray))
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
        #endregion
        #region Priority, velikosti ikon, atd
        /// <summary>
        /// Obsahuje true, pokud jsou preferovány vektorové ikony.
        /// Hodnota je po dobu běhu aplikace konstantní.
        /// </summary>
        public static bool IsPreferredVectorImage { get { return Instance._IsPreferredVectorImage; } }
        /// <summary>
        /// Standardní velikost ikony
        /// </summary>
        public static ResourceImageSizeType ImageSizeStandard { get { return Instance._ImageSizeStandard; } }
        /// <summary>
        /// Menší velikost ikony, o 1 stupeň menší než <see cref="ImageSizeStandard"/>
        /// </summary>
        public static ResourceImageSizeType ImageSizeSmaller { get { return Instance._ImageSizeSmaller; } }
        /// <summary>
        /// Větší velikost ikony, o 1 stupeň větší než <see cref="ImageSizeStandard"/>
        /// </summary>
        public static ResourceImageSizeType ImageSizeLarger { get { return Instance._ImageSizeLarger; } }
        /// <summary>
        /// Standardní velikost ikony
        /// </summary>
        private ResourceImageSizeType _ImageSizeStandard 
        {
            get
            {
                if (!__ImageSizeStandard.HasValue)
                {
                    ResourceImageSizeType sizeType = SystemAdapter.ImageSizeStandard;
                    if (sizeType == ResourceImageSizeType.Small || sizeType == ResourceImageSizeType.Medium || sizeType == ResourceImageSizeType.Large)
                        __ImageSizeStandard = sizeType;
                }
                return __ImageSizeStandard ?? ResourceImageSizeType.Medium;
            }
        }
        /// <summary>Standardní velikost ikony, nebo null dokud není určeno</summary>
        private ResourceImageSizeType? __ImageSizeStandard = null;
        /// <summary>
        /// Zmenšená velikost ikony
        /// </summary>
        private ResourceImageSizeType _ImageSizeSmaller
        {
            get
            {
                switch (_ImageSizeStandard)
                {
                    case ResourceImageSizeType.Small: return ResourceImageSizeType.Small;
                    case ResourceImageSizeType.Medium: return ResourceImageSizeType.Small;
                    case ResourceImageSizeType.Large: return ResourceImageSizeType.Medium;
                }
                return ResourceImageSizeType.Small;
            }
        }
        /// <summary>
        /// Zvětšená velikost ikony
        /// </summary>
        private ResourceImageSizeType _ImageSizeLarger
        {
            get
            {
                switch (_ImageSizeStandard)
                {
                    case ResourceImageSizeType.Small: return ResourceImageSizeType.Medium;
                    case ResourceImageSizeType.Medium: return ResourceImageSizeType.Large;
                    case ResourceImageSizeType.Large: return ResourceImageSizeType.Large;
                }
                return ResourceImageSizeType.Large;
            }
        }
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
                    __Current._TryLoadResources();
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
        private void _TryLoadResources()
        {
            try
            {
                var resources = SystemAdapter.GetResources();
                _AddResources(resources, true);
                __IsResourceLoaded = (__ItemDict.Count > 0);
            }
            catch (Exception) { }
        }
        /// <summary>
        /// Přidá další zdroje
        /// </summary>
        /// <param name="resources"></param>
        public static void AddResources(IEnumerable<IResourceItem> resources) { Current._AddResources(resources, false); }
        /// <summary>
        /// Přidá další zdroje
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="withReset"></param>
        private void _AddResources(IEnumerable<IResourceItem> resources, bool withReset)
        {
            if (resources != null)
            {
                if (withReset)
                {
                    __ItemDict.Clear();
                    __PackDict.Clear();
                }
                foreach (var resource in resources)
                {
                    ResourceItem item = ResourceItem.CreateFrom(resource);
                    if (item == null) continue;
                    __ItemDict.Store(item.ItemKey, item);
                    var pack = __PackDict.Get(item.PackKey, () => new ResourcePack(item.PackKey));
                    pack.AddItem(item);
                }
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
            /// Pokud je na vstupu pole obsahující přinejmenším jeden prvek, pak spolehlivě vrací true a vhodný prvek dává do out <paramref name="resourceItem"/>.
            /// Vrací false jen tehdy, když dodané pole je null nebo prázdné.
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
            /// Obsahuje true, pokud this zdroj je typu <see cref="ResourceContentType.Icon"/> (má příponu ".ico")
            /// </summary>
            public bool IsIcon { get { return (ContentType == ResourceContentType.Icon); } }
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
            #region Tvorba Image nebo SVG nebo Icon objektu
            /// <summary>
            /// Metoda vrátí new instanci <see cref="System.Drawing.Image"/> vytvořenou z <see cref="Content"/>.
            /// Pokud ale this instance není Bitmap (<see cref="IsBitmap"/> je false) anebo není platná, vyhodí chybu!
            /// <para/>
            /// Je vrácena new instance objektu, tato třída je <see cref="IDisposable"/>, používá se tedy v using { } patternu!
            /// </summary>
            /// <returns></returns>
            public System.Drawing.Image CreateBmpImage()
            {
                if (!IsBitmap) throw new InvalidOperationException($"ResourceItem.CreateBmpImage() error: Resource {ItemKey} is not BITMAP type, ContentType is {ContentType}.");
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
                if (!IsSvg) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} is not SVG type, ContentType is {ContentType}.");
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
            /// <summary>
            /// Metoda vrátí new instanci <see cref="System.Drawing.Icon"/> vytvořenou z <see cref="Content"/>.
            /// Pokud ale this instance není Icon (<see cref="IsIcon"/> je false) anebo není platná, vyhodí chybu!
            /// <para/>
            /// Je vrácena new instance objektu, tato třída je <see cref="IDisposable"/>, používá se tedy v using { } patternu!
            /// </summary>
            /// <returns></returns>
            public Icon CreateIconImage()
            {
                if (!IsIcon) throw new InvalidOperationException($"ResourceItem.CreateIconImage() error: Resource {ItemKey} is not ICON type, ContentType is {ContentType}.");
                var content = Content;
                if (content == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} can not load content.");

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(content))
                    return new System.Drawing.Icon(ms);
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
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFormIcon = "svgimages/spreadsheet/conditionalformatting.svg";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutCloseSvg = "svgimages/hybriddemoicons/bottompanel/hybriddemo_close.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockLeftSvg = "svgimages/align/alignverticalleft.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockTopSvg = "svgimages/align/alignhorizontaltop.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockBottomSvg = "svgimages/align/alignhorizontalbottom.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockRightSvg = "svgimages/align/alignverticalright.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutClosePng = "devav/actions/delete_16x16.png";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockLeftPng = "images/alignment/alignverticalleft_16x16.png";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockTopPng = "images/alignment/alignhorizontaltop_16x16.png";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockBottomPng = "images/alignment/alignhorizontalbottom_16x16.png";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxLayoutDockRightPng = "images/alignment/alignverticalright_16x16.png";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxRibbonQatMenuAdd = "svgimages/icon%20builder/actions_add.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxRibbonQatMenuRemove = "svgimages/icon%20builder/actions_remove.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxRibbonQatMenuMoveUp = "svgimages/icon%20builder/actions_arrow2up.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxRibbonQatMenuMoveDown = "svgimages/icon%20builder/actions_arrow2down.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxRibbonQatMenuShowManager = "svgimages/scheduling/viewsettings.svg";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxBarCheckToggleNull = "images/xaf/templatesv2images/bo_unknown_disabled.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxBarCheckToggleFalse = "svgimages/icon%20builder/actions_deletecircled.svg";    //  "svgimages/xaf/state_validation_invalid.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxBarCheckToggleTrue = "svgimages/icon%20builder/actions_checkcircled.svg";      //  "svgimages/xaf/state_validation_valid.svg";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxImagePickerClearFilter = "pic_0/UI/FilterBox/CancelFilter";                    // "svgimages/spreadsheet/clearfilter.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxImagePickerClipboarCopy = "svgimages/xaf/action_copy.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxImagePickerClipboarCopyHot = "svgimages/xaf/action_modeldifferences_copy.svg";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterBoxMenu = "svgimages/dashboards/horizontallines.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterClearFilter = "pic_0/UI/FilterBox/CancelFilter";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorContains = "pic_0/UI/FilterBox/Contains";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorDoesNotContain = "pic_0/UI/FilterBox/DoesNotContain";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorEndWith = "pic_0/UI/FilterBox/EndWith";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorDoesNotEndWith = "pic_0/UI/FilterBox/DoesNotEndWith";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorMatch = "pic_0/UI/FilterBox/Match";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorDoesNotMatch = "pic_0/UI/FilterBox/DoesNotMatch";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorStartWith = "pic_0/UI/FilterBox/DoesNotStartWith";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorDoesNotStartWith = "pic_0/UI/FilterBox/DoesNotStartWith";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorEquals = "pic_0/UI/FilterBox/Equals";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorGreaterThan = "pic_0/UI/FilterBox/GreaterThan";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorGreaterThanOrEqualTo = "pic_0/UI/FilterBox/GreaterThanOrEqualTo";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorLessThan = "pic_0/UI/FilterBox/LessThan";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorLessThanOrEqualTo = "pic_0/UI/FilterBox/LessThanOrEqualTo";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorLike = "pic_0/UI/FilterBox/Like";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorNotEquals = "pic_0/UI/FilterBox/NotEquals";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxFilterOperatorNotLike = "pic_0/UI/FilterBox/NotLike";

        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxDialogApply = "svgimages/outlook%20inspired/markcomplete.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxDialogCancel = "svgimages/outlook%20inspired/delete.svg";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxDialogIconInfo = "pic_0/Win/MessageBox/info";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxDialogIconWarning = "pic_0/Win/MessageBox/warning";
        /// <summary>Standardní ikona pro danou příležitost</summary>
        public const string DxDialogIconError = "pic_0/Win/MessageBox/error";
    }
    #endregion
    #region class DxBmpImageList : Kolekce Images rozšířená o možnost reloadu při změně barevnosti
    /// <summary>
    /// DxBmpImageList : Kolekce Images rozšířená o možnost reloadu při změně barevnosti
    /// </summary>
    public class DxBmpImageList : IListenerZoomChange, IListenerLightDarkChanged, IDisposable
    {
        #region Konstruktor, Dispose, Public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="sizeType"></param>
        public DxBmpImageList(ResourceImageSizeType sizeType)
        {
            this.SizeType = sizeType;
            this.ImageList = new ImageList();
            this._ExtendedDict = new Dictionary<string, ImageInfo>();
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Typ velikosti
        /// </summary>
        public ResourceImageSizeType SizeType { get; private set; }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            DxComponent.UnregisterListener(this);
            this._ExtendedDict?.Clear();
            this._ExtendedDict = null;
            this.ImageList?.Dispose();
            this.ImageList = null;
        }
        /// <summary>
        /// Byl objekt disposován?
        /// </summary>
        public bool IsDisposed { get { return (this.ImageList == null); } }
        #endregion
        #region Transparentnost do ImageListu
        /// <summary>
        /// Velikost obrázku
        /// </summary>
        public Size ImageSize { get { return ImageList.ImageSize; } set { ImageList.ImageSize = value; } }
        /// <summary>
        /// ColorDepth obrázků
        /// </summary>
        public ColorDepth ColorDepth { get { return ImageList.ColorDepth; } set { ImageList.ColorDepth = value; } }
        /// <summary>
        /// ImageList
        /// </summary>
        public ImageList ImageList { get; private set; }
        /// <summary>
        /// Obsahuje prvek?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) { return this.ImageList.Images.ContainsKey(key); }
        /// <summary>
        /// Vrátí index prvku
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfKey(string key) { return this.ImageList.Images.IndexOfKey(key); }
        /// <summary>
        /// Přidá prvek
        /// </summary>
        /// <param name="key"></param>
        /// <param name="image"></param>
        public void Add(string key, Image image)  { this.ImageList.Images.Add(key, image); }
        #endregion
        #region Extended info
        /// <summary>
        /// Přidá prvek, který bude možno po změně skinu znovu vytvořit pro novou barevnost
        /// </summary>
        /// <param name="key"></param>
        /// <param name="image"></param>
        /// <param name="isColorized"></param>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="optimalSvgSize"></param>
        /// <param name="caption"></param>
        public void Add(string key, Image image, bool isColorized, string imageName, bool exactName, Size? optimalSvgSize, string caption)
        {
            if (isColorized)
            {
                if (!_ExtendedDict.ContainsKey(key))
                    _ExtendedDict.Add(key, new ImageInfo(key, imageName, exactName, this.SizeType, optimalSvgSize, caption));
            }
            this.ImageList.Images.Add(key, image);
        }
        /// <summary>
        /// Dictionary prvků, pro které bude možno vytvořit new Image po změně skinu Light / Dark
        /// </summary>
        private Dictionary<string, ImageInfo> _ExtendedDict;
        /// <summary>
        /// Třída, která dokáže vytvořet new Image po změně skinu
        /// </summary>
        private class ImageInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="key"></param>
            /// <param name="imageName"></param>
            /// <param name="exactName"></param>
            /// <param name="sizeType"></param>
            /// <param name="optimalSvgSize"></param>
            /// <param name="caption"></param>
            public ImageInfo(string key, string imageName, bool exactName, ResourceImageSizeType sizeType, Size? optimalSvgSize, string caption)
            {
                this.Key = key;
                this.ImageName = imageName;
                this.ExactName = exactName;
                this.SizeType = sizeType;
                this.OptimalSvgSize = optimalSvgSize;
                this.Caption = caption;
            }
            /// <summary>
            /// Vytvoří a vrátí new Image pro this data, tedy pro aktuálně platný skin
            /// </summary>
            /// <returns></returns>
            public Image CreateImage()
            {
                return DxComponent.CreateBitmapImage(this.ImageName, this.SizeType, this.OptimalSvgSize, this.Caption, this.ExactName, false);
            }
            public readonly string Key;
            public readonly string ImageName;
            public readonly bool ExactName;
            public readonly ResourceImageSizeType SizeType;
            public readonly Size? OptimalSvgSize;
            public readonly string Caption;
        }
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
        /// Přegeneruje svoje ikony po změně skinu
        /// </summary>
        private void OnLightDarkChanged()
        {
            foreach (var info in _ExtendedDict.Values)
            {
                int index = IndexOfKey(info.Key);
                if (index >= 0)
                    this.ImageList.Images[index] = info.CreateImage();
            }
        }
        #endregion
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
                    else
                        this[i] = dxSvgImage.CreateClone();
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
        #region Tvorba - konstruktory, statické konstruktory, TryGet, implicit
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImage() : base() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSvgImage(System.IO.MemoryStream stream) : base(stream) { }
        /// <summary>
        /// Static konstruktor z dodaného XML stringu
        /// </summary>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        public static DxSvgImage Create(string xmlContent)
        {
            if (String.IsNullOrEmpty(xmlContent)) return null;
            return Create(null, false, Encoding.UTF8.GetBytes(xmlContent));
        }
        /// <summary>
        /// Static konstruktor z podkladového <see cref="SvgImage"/>
        /// </summary>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        public static DxSvgImage Create(SvgImage svgImage)
        {
            if (svgImage == null) return null;
            return Create(null, false, svgImage.ToXmlString());
        }
        /// <summary>
        /// Static konstruktor z dodaného pole byte
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DxSvgImage Create(byte[] data)
        {
            if (data == null) return null;
            return Create(null, false, data);
        }
        /// <summary>
        /// Static konstruktor pro dodaná data
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
        /// Static konstruktor pro dodaná data
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
        /// Metoda prověří, zda by dodaný string mohl být XML obsah, deklarující <see cref="DxSvgImage"/> a případně jej zkusí vytvořit.
        /// Pokud tedy vrátí true, pak v out <paramref name="dxSvgImage"/> bude vytvořený Image.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        public static bool TryGetXmlContent(string imageName, ResourceImageSizeType? sizeType, out DxSvgImage dxSvgImage)
        {
            dxSvgImage = null;
            if (String.IsNullOrEmpty(imageName)) return false;

            imageName = imageName.Trim();
            if (TryGetGenericSvg(imageName, sizeType, out dxSvgImage)) return true;
            if (TryGetRawSvg(imageName, out dxSvgImage)) return true;

            return false;
        }
        /// <summary>
        /// Může vygenerovat generické SVG podle dodaného XML obsahu
        /// </summary>
        /// <param name="imageName">Definice image, je zajištěno že není prázdné, a je Trim()</param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        protected static bool TryGetRawSvg(string imageName, out DxSvgImage dxSvgImage)
        {
            dxSvgImage = null;
            bool startWithXml = imageName.StartsWith("<?xml version", StringComparison.InvariantCultureIgnoreCase);
            bool startWithSvg = imageName.StartsWith("<svg ", StringComparison.InvariantCultureIgnoreCase);
            if (!startWithXml && !startWithSvg) return false;        // Pokud text NEzačíná <xml a NEzačíná <svg , tak to nemůže být SvgImage.
            if (!startWithSvg && imageName.IndexOf("<svg", StringComparison.InvariantCultureIgnoreCase) < 0) return false; // Pokud NEzačíná <svg  a ani neobsahuje <svg uvnitř, tak to nemůže být SvgImage.
            if (!imageName.EndsWith("</svg>", StringComparison.InvariantCultureIgnoreCase)) return false;                  // Musí končit tagem </svg>

            try { dxSvgImage = Create(imageName); }
            catch { /* Daný text není správný, ale to nám tady nevadí, my jsme "TryGet..." metoda... */ }
            return (dxSvgImage != null);
        }
        /// <summary>
        /// Vrací new instanci z daného bufferu
        /// </summary>
        /// <param name="data"></param>
        public static implicit operator DxSvgImage(byte[] data)
        {
            return Create(null, false, data);
        }
        #endregion
        #region Standardní public properties
        /// <summary>
        /// Jméno zdroje
        /// </summary>
        public string ImageName { get; private set; }
        /// <summary>
        /// Velikost, pokud byla zachycena
        /// </summary>
        public ResourceImageSizeType? SizeType { get; private set; }
        /// <summary>
        /// Po změně skinu (Světlý - Tmavý) je nutno obsah přegenerovat.
        /// Bohužel obsah SvgImage změnit nelze, je třeba vygenerovat new instanci.
        /// </summary>
        public bool IsLightDarkCustomizable { get; private set; }
        /// <summary>
        /// Zdrojová definice generického Image
        /// </summary>
        protected string GenericSource { get; private set; }
        /// <summary>
        /// XML obsah tohoto objektu. 
        /// Z principu nelze setovat - instance <see cref="DxSvgImage"/> stejně jako <see cref="SvgImage"/> je immutable.
        /// </summary>
        public string XmlContent { get { return this.ToXmlString(); } }
        /// <summary>
        /// Souřadnice image (Offset + Size)
        /// </summary>
        public RectangleF ViewBounds { get { return new RectangleF((float)this.OffsetX, (float)this.OffsetY, (float)this.Width, (float)this.Height); } }
        #endregion
        #region RenderTo Graphics
        /// <summary>
        /// Renderuje this image do dané grafiky na dané místo
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="svgPalette">SVG paleta pro korekce barev. Může být null.</param>
        public void RenderTo(Graphics graphics, Rectangle bounds, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            _RenderTo(this, graphics, bounds, ContentAlignment.MiddleCenter, svgPalette, out var _);
        }
        /// <summary>
        /// Renderuje this image do dané grafiky na dané místo
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="imageBounds"></param>
        /// <param name="alignment"></param>
        public void RenderTo(Graphics graphics, Rectangle bounds, ContentAlignment alignment, out RectangleF? imageBounds)
        {
            _RenderTo(this, graphics, bounds, alignment, null, out imageBounds);
        }
        /// <summary>
        /// Renderuje this image do dané grafiky na dané místo
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <param name="svgPalette">SVG paleta pro korekce barev. Může být null.</param>
        /// <param name="imageBounds"></param>
        public void RenderTo(Graphics graphics, Rectangle bounds, ContentAlignment alignment, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, out RectangleF? imageBounds)
        {
            _RenderTo(this, graphics, bounds, alignment, svgPalette, out imageBounds);
        }
        /// <summary>
        /// Renderuje daný image do dané grafiky na dané místo
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment "></param>
        /// <param name="svgPalette">SVG paleta pro korekce barev. Může být null.</param>
        public static void RenderTo(SvgImage svgImage, Graphics graphics, Rectangle bounds, ContentAlignment alignment = ContentAlignment.MiddleCenter, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            _RenderTo(svgImage, graphics, bounds, alignment, svgPalette, out var _);
        }
        /// <summary>
        /// Renderuje daný image do dané grafiky na dané místo
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="imageBounds"></param>
        /// <param name="alignment "></param>
        /// <param name="svgPalette">SVG paleta pro korekce barev. Může být null.</param>
        public static void RenderTo(SvgImage svgImage, Graphics graphics, Rectangle bounds, out RectangleF? imageBounds, ContentAlignment alignment = ContentAlignment.MiddleCenter, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            _RenderTo(svgImage, graphics, bounds, alignment, svgPalette, out imageBounds);
        }
        /// <summary>
        /// Renderuje daný image do dané grafiky na dané místo
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="graphics"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment "></param>
        /// <param name="svgPalette">SVG paleta pro korekce barev. Může být null.</param>
        /// <param name="imageBounds"></param>
        private static void _RenderTo(SvgImage svgImage, Graphics graphics, Rectangle bounds, ContentAlignment alignment, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, out RectangleF? imageBounds)
        {
            imageBounds = null;
            if (svgImage is null || graphics is null || bounds.Width <= 2 || bounds.Height <= 2) return;

            // Matrix(a,b,c,d,e,f):  Xr = Xi * a ..... Yr = Yi * d  +  Xi * b......
            //   a: zoom X
            //   b: X * b => Y
            //   c: Y * c => X
            //   d: zoom Y
            //   e: posun X
            //   f: posun Y
            var state = graphics.Save();
            var matrixOld = graphics.Transform;
            try
            {
                graphics.SetClip(bounds);
                SizeF imgSize = new SizeF((float)svgImage.Width, (float)svgImage.Height);
                RectangleF imgBounds = imgSize.ZoomTo((RectangleF)bounds, alignment);
                graphics.Transform = new System.Drawing.Drawing2D.Matrix(1f, 0f, 0f, 1f, imgBounds.X, imgBounds.Y);
                double scaleX = (double)imgBounds.Width / svgImage.Width;
                double scaleY = (double)imgBounds.Height / svgImage.Height;
                double scale = (scaleX <= scaleY ? scaleX : scaleY);

                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                svgImage.RenderToGraphics(graphics, svgPalette, scale);

                imageBounds = imgBounds;
            }
            finally
            {
                graphics.ResetClip();
                graphics.Transform = matrixOld;
                graphics.Restore(state);
            }
        }
        #endregion
        #region Generické SVG
        /// <summary>
        /// Může vygenerovat generické SVG podle názvu a parametrů
        /// </summary>
        /// <param name="imageName">Definice image, je zajištěno že není prázdné, a je Trim()</param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        protected static bool TryGetGenericSvg(string imageName, ResourceImageSizeType? sizeType, out DxSvgImage dxSvgImage)
        {
            dxSvgImage = null;
            if (_TryGetGenericParameters(imageName, out var genericItems))
            {
                switch (genericItems[0])
                {
                    case "circlegradient1": return _TryGetGenericSvgCircleGradient1(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "circle": return _TryGetGenericSvgCircleGradient1(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "text": return _TryGetGenericSvgText(imageName, genericItems, sizeType, ref dxSvgImage);
                }
            }
            return false;
        }
        /// <summary>
        /// Vytvoří klon aktuálního objektu pro aktuální barvu skinu
        /// </summary>
        /// <returns></returns>
        public DxSvgImage CreateClone()
        {
            if (!String.IsNullOrEmpty(this.GenericSource))
                return CreateGenericClone(this.GenericSource, this.SizeType);
            else
                return Create(this.ImageName, this.IsLightDarkCustomizable, this.XmlContent);
        }
        /// <summary>
        /// Vytvoří klon aktuálního objektu dle generické definice, pro aktuální barvu skinu
        /// </summary>
        /// <returns></returns>
        protected static DxSvgImage CreateGenericClone(string imageName, ResourceImageSizeType? sizeType)
        {
            if (TryGetGenericSvg(imageName, sizeType, out DxSvgImage dxSvgImage)) return dxSvgImage;
            return null;
        }
        /// <summary>
        /// Ze vstupního stringu detekuje parametry generického SVG.
        /// Pokud vrátí true, pak na indexu [0] pole out <paramref name="genericItems"/> bude klíčové slovo, na indexech 1++ budou jeho parametry.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <returns></returns>
        private static bool _TryGetGenericParameters(string imageName, out string[] genericItems)
        {
            genericItems = null;
            bool result = false;
            if (!String.IsNullOrEmpty(imageName) && imageName[0] == GenericHeader && imageName.Length > 1)
            {
                genericItems = imageName.Substring(1).Split(GenericParamSeparator);
                result = (genericItems.Length >= 1 && genericItems[0].Length > 0);
            }
            return result;
        }
        /// <summary>
        /// @  Úvodní znak generické deklarace SvgImage
        /// </summary>
        public static char GenericHeader { get { return '@'; } }
        /// <summary>
        /// |  Oddělovač parametrů v generické deklaraci SvgImage
        /// </summary>
        public static char GenericParamSeparator { get { return '|'; } }
        #region Circle
        /// <summary>
        /// Z dodané definice a pro danou velikost vygeneruje SvgImage obsahující Circle s Gradient výplní.
        /// Očekávaná deklarace zní: "?circlegradient1?violet?75", 
        /// kde "circlegradient" je klíčové slovo (může být "circle");
        /// kde "violet" je barva středu (default = Green);
        /// kde "65" je 65% velký kruh v rámci ikony (default = 80%);
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgCircleGradient1(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage)
        {
            int size = (sizeType.HasValue && sizeType.Value == ResourceImageSizeType.Small ? 16 : 32);
            string xmlHeader = _GetXmlContentHeader(size);

            string xmlStyles = _GetXmlDevExpressStyles();

            string circleColorName = _GetGenericParam(genericItems, 1, "Green");
            int radiusRel = _GetGenericParam(genericItems, 2, 80);

            string xmlGradient = _GetXmlContentGradientRadial(size, "CircleGradient", circleColorName, null, radiusRel);
            string xmlCircle = _GetXmlContentCircle(size, "url(#CircleGradient)", radiusRel);

            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlGradient + xmlCircle + xmlFooter;
            dxSvgImage = DxSvgImage.Create(imageName, true, xmlContent);
            dxSvgImage.SizeType = sizeType;
            dxSvgImage.GenericSource = imageName;
            return true;

            /*     pro zadání    @circlegradient1|violet|70    vygeneruje SVG:
﻿<?xml version="1.0" encoding="UTF-8"?>
<svg x="0" y="0" width="32" height="32" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" enable-background="new 0 0 32 32" xml:space="preserve" id="Layer_1">
  <g id="icon">
    <style type="text/css"> .White{fill:#FFFFFF;} .Red{fill:#D11C1C;} .Green{fill:#039C23;} .Blue{fill:#1177D7;} .Yellow{fill:#FFB115;} .Black{fill:#727272;} .st0{opacity:0.75;} .st1{opacity:0.5;} </style>
    <defs>
      <radialGradient id="CircleGradient" gradientUnits="userSpaceOnUse" cx="20" cy="20" r="54" fx="17" fy="17">
        <stop offset="0%" stop-color="violet" />
        <stop offset="100%" stop-color="white" />
      </radialGradient>
    </defs>
    <circle cx="16" cy="16" r="11" fill="url(#CircleGradient)" stroke="black" stroke-width="0"  />
  </g>
</svg>
    */
        }
        #endregion
        #region Text
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static SvgImage CreateCaptionVector(string caption, ResourceImageSizeType sizeType)
        {   // Využijeme metodu _TryGetGenericSvgText, předáme jí explicitní parametry:
            string text = DxComponent.GetCaptionForIcon(caption);
            string imageName = $"@text|{text}";     //   ... |fill='Black'|sans-serif|N|fill='DarkBlue'|fill='White'";
            if (!TryGetGenericSvg(imageName, sizeType, out DxSvgImage dxSvgImage)) return null;
            return dxSvgImage;
        }
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private static SvgImage CreateCaptionVectorOld(string caption, ResourceImageSizeType sizeType)
        {
            string text = DxComponent.GetCaptionForIcon(caption);
            bool isLarge = (sizeType != ResourceImageSizeType.Small);
            int size = (isLarge ? 32 : 16);

            if (text.Length == 0) return null;
            bool isWide = (text == "MM" || text == "OO" || text == "WW" || text == "QQ" || text == "AA");
            bool isBold = false;                           // true = písmo i rámeček (pro sizeType = Large) bude silnější, false = tenčí
            string fillClass = "White";                    // Použití tříd se jmény DevExpress zajistí automatické přebarvení v různobarevných skinech
            string borderClass = "Blue";
            string textClass = "Black";
            string fontFamily = "sans-serif";              // Vyžádáno bezpatkové písmo
            string fontSize = (isLarge ? (isWide ? "16px" : "18px") : (isWide ? "8px" : "9px"));   // Dává optimální využití prostoru ikony
            string textX = (isLarge ? "15" : "7");         // Posunutí mírně doleva dává správný grafický výsledek, na rozdíl od středu: (isLarge ? "16" : "8");
            string textY = (isLarge ? (isWide ? "20" : "22") : (isWide ? "10" : "11"));
            string weight = (isBold ? (isLarge ? "400" : "600") : (isLarge ? "300" : "500"));      // dříve: (isWide ? (isBold ? "600" : "300") : (isBold ? "600" : "300"));
            string path2 = isLarge ?
                (isBold ? "M30,30H2V2h28V30z" : "M31,31H1V1H31V31z") :
                "M15,15H1V1h14V15z";
            string path1 = isLarge ?
                "M31,0H1C0.5,0,0,0.5,0,1v30c0,0.5,0.5,1,1,1h30c0.5,0,1-0.5,1-1V1C32,0.5,31.5,0,31,0z " + path2 :
                "M15.5,0H0.5C0.25,0,0,0.25,0,0.5v15c0,0.25,0.25,0.5,0.5,0.5h15c0.25,0,0.5-0.25,0.5-0.5V0.5C16,0.25,15.75,0,15.5,0z " + path2;

            string xmlHeader = _GetXmlContentHeader(size);
            string xmlStyles = _GetXmlDevExpressStyles();
            string xmlText = $@"  <g id='icon{text}' style='font-size: {fontSize}; text-anchor: middle; font-family: {fontFamily}; font-weight: {weight}'>
    <path d='{path1}' class='{borderClass}' />
    <path d='{path2}' class='{fillClass}' />
    <text x='{textX}' y='{textY}' class='{textClass}'>{text}</text>
  </g>
";
            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlText + xmlFooter;
            xmlContent = xmlContent.Replace("'", "\"");
            return DxSvgImage.Create(caption, false, xmlContent);
        }
        /// <summary>
        ///  Z dodané definice a pro danou velikost vygeneruje SvgImage obsahující text.
        /// Očekávaná deklarace zní: "?text?ABCDEF?75", 
        /// kde "text" je klíčové slovo (může být "circle");
        /// kde "ABCDEF" je text (akceptují se nejvýše dva první znaky)
        /// kde ...
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgText(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage)
        {
            bool isDarkTheme = DxComponent.IsDarkTheme;
            int p = 1;
            string text = _GetGenericParam(genericItems, p++, "");                         // Text, bude pouze Trimován
            string textParam = _GetGenericParam(genericItems, p++, "");                    // Barva písma (class, fill, nic)
            if (String.IsNullOrEmpty(textParam)) textParam = $"fill='{(isDarkTheme ? _GenericTextColorDarkSkinText : _GenericTextColorLightSkinText)}'";
            string fontFamily = _GetGenericParam(genericItems, p++, "");                   // Font: Default = bezpatkové písmo
            if (String.IsNullOrEmpty(fontFamily)) fontFamily = "sans-serif";
            bool isBold = (_GetGenericParam(genericItems, p++, "N").StartsWith("B", StringComparison.InvariantCultureIgnoreCase));     // Bold
            string borderParam = _GetGenericParam(genericItems, p++, "");                  // Barva rámečku
            if (String.IsNullOrEmpty(borderParam)) borderParam = $"fill='{(isDarkTheme ? _GenericTextColorDarkSkinBorder : _GenericTextColorLightSkinBorder)}'";
            string fillParam = _GetGenericParam(genericItems, p++, "");                    // Barva podkladu: Default = průhledná
            if (String.IsNullOrEmpty(fillParam)) fillParam = $"fill='{(isDarkTheme ? _GenericTextColorDarkSkinFill : _GenericTextColorLightSkinFill)}'";
            return _TryGetGenericSvgText(imageName, text, sizeType, ref dxSvgImage, textParam, fontFamily, isBold, borderParam, fillParam);
        }
        /// <summary>
        /// Z dodané definice a pro danou velikost vygeneruje SvgImage obsahující text.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="text"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <param name="textParam"></param>
        /// <param name="fontFamily"></param>
        /// <param name="isBold"></param>
        /// <param name="borderParam"></param>
        /// <param name="fillParam"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgText(string imageName, string text, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage,
            string textParam, string fontFamily, bool isBold, string borderParam, string fillParam)
        {
            int size = (sizeType.HasValue && sizeType.Value == ResourceImageSizeType.Small ? 16 : 32);
            TextInfo textInfo = new TextInfo(text, size, fontFamily, isBold);

            string xmlHeader = _GetXmlContentHeader(size);
            string xmlStyles = _GetXmlDevExpressStyles();
            string xmlTextBegin = textInfo.GetXmlGroupBegin();
            string xmlPathBorder = _GetXmlPathBorderSquare(size, isBold, borderParam);
            string xmlPathFill = _GetXmlPathFillSquare(size, isBold, fillParam);
            string xmlTextText = textInfo.GetXmlGroupText(textParam);
            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlTextBegin + xmlPathBorder + xmlPathFill + xmlTextText + xmlFooter;
            dxSvgImage = DxSvgImage.Create(text, true, xmlContent);
            dxSvgImage.SizeType = sizeType;
            dxSvgImage.GenericSource = imageName;
            return true;

            /*     pro zadání    @text|OK    vygeneruje SVG:
﻿<?xml version="1.0" encoding="UTF-8"?>
<svg x="0" y="0" width="32" height="32" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" enable-background="new 0 0 32 32" xml:space="preserve" id="Layer_1">
  <g id="icon">
    <style type="text/css"> .White{fill:#FFFFFF;} .Red{fill:#D11C1C;} .Green{fill:#039C23;} .Blue{fill:#1177D7;} .Yellow{fill:#FFB115;} .Black{fill:#727272;} .st0{opacity:0.75;} .st1{opacity:0.5;} </style>
    <g id="iconOK" style="font-size: 20px; text-anchor: middle; font-family: sans-serif; font-weight: 500">
      <path d="M1,0h30c0.5,0,1,0.5,1,1v30c0,0.5,-0.5,1,-1,1h-30c-0.5,0,-1,-0.5,-1,-1v-30c0,-0.5,0.5,-1,1,-1z M1,1v30h30v-30h-30z" fill="#383838" />
      <path d="M1,1h30v30h-30v-30z" fill="#FFFFFF" />
      <text x="15" y="23" fill="#000000">OK</text>
    </g>
  </g>
</svg>

            */
            /*     pro zadání    @text|OK|Black|B|Blue|White    vygeneruje SVG:
﻿<?xml version="1.0" encoding="UTF-8"?>
<svg x="0" y="0" width="32" height="32" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" enable-background="new 0 0 32 32" xml:space="preserve" id="Layer_1">
  <g id="icon">
    <style type="text/css"> .White{fill:#FFFFFF;} .Red{fill:#D11C1C;} .Green{fill:#039C23;} .Blue{fill:#1177D7;} .Yellow{fill:#FFB115;} .Black{fill:#727272;} .st0{opacity:0.75;} .st1{opacity:0.5;} </style>
    <g id='iconOK' style='font-size: 18px; text-anchor: middle; font-family: ; font-weight: 600'>
      <path d="M1,0h30c0.5,0,1,0.5,1,1v30c0,0.5,-0.5,1,-1,1h-30c-0.5,0,-1,-0.5,-1,-1v-30c0,-0.5,0.5,-1,1,-1z M2,2v28h28v-28h-28z" class="Blue" />
      <path d="M2,2h28v28h-28v-28z" class="White" />
      <text x='15' y='22' class='Black'>OK</text>
    </g>
  </g>
</svg>
            */
        }
        /// <summary>
        /// Třída pro zpracování parametrů textu
        /// </summary>
        private class TextInfo
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="text"></param>
            /// <param name="size"></param>
            /// <param name="fontFamily"></param>
            /// <param name="isBold">true = písmo i rámeček (pro sizeType = Large) bude silnější, false = tenčí</param>
            public TextInfo(string text, int size, string fontFamily, bool isBold)
            {
                text = (text ?? "").Trim();
                this.Text = text;
                if (size >= 32)
                {
                    this.IsWide = (text == "WW");                    // není nutno:  || text == "MM" || text == "OO" || text == "QQ" || text == "AA");
                    this.FontFamily = fontFamily;
                    this.FontSize = (IsWide ? "16px" : "18px");
                    this.FontWeight = (isBold ? "600" : "200");
                    this.TextX = "15.5";
                    this.TextY = (IsWide ? "22" : "23");
                }
                else
                {
                    this.IsWide = (text == "WW" || text == "MM");    // není nutno: || text == "OO" || text == "QQ" || text == "AA");
                    this.FontFamily = fontFamily;
                    this.FontSize = (IsWide ? "8.2px" : "9.3px");
                    this.FontWeight = (isBold ? "600" : "400");
                    this.TextX = (IsWide ? "7.4" : "7.7");
                    this.TextY = (IsWide ? "11" : "11.5");
                }
            }
            /// <summary>
            /// Vrátí začátek grupy pro text, obsahuje popis fontu
            /// </summary>
            /// <returns></returns>
            public string GetXmlGroupBegin()
            {
                string xmlText = $@"    <g id='icon{this.Text}' style='font-size: {this.FontSize}; text-anchor: middle; font-family: {this.FontFamily}; font-weight: {this.FontWeight}'>
";
                return xmlText.Replace("'", "\"");
            }
            /// <summary>
            /// Vrátí konec grupy pro text, obsahuje text, jeho pozici a jeho třídu
            /// </summary>
            /// <param name="textParam"></param>
            /// <returns></returns>
            public string GetXmlGroupText(string textParam)
            {
                _ResolveParam(ref textParam, "class='Black'");
                string xmlText = $@"      <text x='{this.TextX}' y='{this.TextY}' {textParam} text-rendering='optimizeLegibility' >{this.Text}</text>
    </g>
";
                return xmlText.Replace("'", "\"");
            }
            public readonly string Text;
            public readonly bool IsWide;
            public readonly string FontFamily;
            public readonly string FontSize;
            public readonly string FontWeight;
            public readonly string TextX;
            public readonly string TextY;
        }
        /// <summary>Barva pro generický text, světlý skin: písmo</summary>
        private static string _GenericTextColorLightSkinText { get { return "#202020"; } }    // "#383838"
        /// <summary>Barva pro generický text, tmavý skin: písmo</summary>
        private static string _GenericTextColorDarkSkinText { get { return "#D4D4D4"; } }
        /// <summary>Barva pro generický text, světlý skin: okraj</summary>
        private static string _GenericTextColorLightSkinBorder { get { return "#383838"; } }
        /// <summary>Barva pro generický text, tmavý skin: okraj</summary>
        private static string _GenericTextColorDarkSkinBorder { get { return "#D4D4D4"; } }
        /// <summary>Barva pro generický text, světlý skin: výplň</summary>
        private static string _GenericTextColorLightSkinFill { get { return "#FCFCFC"; } }
        /// <summary>Barva pro generický text, tmavý skin: výplň</summary>
        private static string _GenericTextColorDarkSkinFill { get { return "#383838"; } }
        #endregion

        /* Dokument s podbarvením bez ohnutého rohu
﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x="0px" y="0px" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" id="Layer_1" style="enable-background:new 0 0 32 32">
  <style type="text/css">
	.Green{fill:#039C23;}
	.Black{fill:#727272;}
	.Red{fill:#D11C1C;}
	.Yellow{fill:#FFB115;}
	.Blue{fill:#1177D7;}
	.White{fill:#FFFFFF;}
	.st0{opacity:0.5;}
	.st1{opacity:0.75;}
</style>
  <g id="InsertListBox">
    <path d="M26,2H4v26h22V2z" fill="violet"  />
    <path d="M27,30H3c-0.5,0-1-0.5-1-1V1c0-0.6,0.5-1,1-1h24c0.5,0,1,0.4,1,1v28C28,29.5,27.5,30,27,30z M26,2H4v26h22V2   z M22,6H8v2h14V6z M22,10H8v2h14V10z M22,14H8v2h14V14z M22,18H8v2h14V18z M22,22H8v2h14V22z" class="Black" />
  </g>
</svg>        
        */
        /* Dokument bez linek s ohnutým rohem a podbarvením
﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x="0px" y="0px" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" id="New" style="enable-background:new 0 0 32 32">
  <style type="text/css"> .Black{fill:#727272;} </style>
  <g id="DocumentColor">
    <path d="M24,26H8V6h10v5c0,0.6,0.4,1,1,1h5  V26z" fill="violet"  />
    <path d="M19,4H7C6.4,4,6,4.4,6,5v22c0,0.6,0.4,1,1,1h18c0.6,0,1-0.4,1-1V11L19,4z M24,26H8V6h10v5c0,0.6,0.4,1,1,1h5  V26z" class="Black" />
  </g>
</svg>
        */
        /* Dokument s linkami, ohnutým rohem a podbarvením
﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x="0px" y="0px" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" id="New" style="enable-background:new 0 0 32 32">
  <style type="text/css">
	.Black{fill:#727272;}
</style>
  <path d="M24,26H8V6h10v5c0,0.6,0.4,1,1,1h5  V26z  " class="Yellow" />
  <path d="M19,4H7C6.4,4,6,4.4,6,5v22c0,0.6,0.4,1,1,1h18c0.6,0,1-0.4,1-1V11L19,4z M24,26H8V6h10v5c0,0.6,0.4,1,1,1h5  V26z 
M16,10H10v2H16v-2z 
M22,14H10v2H22v-2z 
M22,18H10v2H22v-2z 
M22,22H10v2H22v-2z " class="Black" />
</svg>
        */

        #region Generic Support: Header, Styles, Paths, Curve, Footer, Convertors, Parameters...
        /// <summary>
        /// Vrací XML text zahajující SVG image dané velikosti
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static string _GetXmlContentHeader(int size)
        {
            string xml = $@"﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x='0' y='0' width='{size}' height='{size}' viewBox='0 0 {size} {size}' enable-background='new 0 0 {size} {size}' 
      version='1.1' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' xml:space='preserve' id='Layer_1'>
  <g id='icon'>
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací XML text definující styly DevExpress.
        /// Dává se hned za Header = <see cref="_GetXmlContentHeader(int)"/>.
        /// </summary>
        /// <returns></returns>
        private static string _GetXmlDevExpressStyles()
        {
            string xml = @"    <style type='text/css'> .White{fill:#FFFFFF;} .Red{fill:#D11C1C;} .Green{fill:#039C23;} .Blue{fill:#1177D7;} .Yellow{fill:#FFB115;} .Black{fill:#727272;} .st0{opacity:0.75;} .st1{opacity:0.5;} </style>
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrátí element path, ve tvaru obdélníku (s kulatými rohy) vepsaného do dané velikosti (size), s okraji (padding) o síle okraje (isBold ? 2 : 1).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isBold"></param>
        /// <param name="borderParam"></param>
        /// <param name="counterClockWise"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathBorderSquare(int size, bool isBold, string borderParam, bool counterClockWise = false, Padding? padding = null)
        {
            _ResolveParam(ref borderParam);
            if (size <= 0 || String.IsNullOrEmpty(borderParam)) return "";

            string pathData = _GetXmlPathDataBorderSquare(size, isBold, counterClockWise, padding);
            string xml = $@"      <path d='{pathData}' {borderParam} />
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        ///  Vrátí čistá data pro element path, ve tvaru obdélníku (s kulatými rohy) vepsaného do dané velikosti (size), s okraji (padding) o síle okraje (isBold ? 2 : 1).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isBold"></param>
        /// <param name="counterClockWise"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathDataBorderSquare(int size, bool isBold, bool counterClockWise, Padding? padding = null)
        {
            if (size <= 0) return "";

            Padding p = padding ?? Padding.Empty;
            bool isLarge = (size >= 24);
            int d = (isLarge ? 2 : 1);                     // Průměr kruhu zaoblení hran (=dva radiusy)
            int w = size - (p.Horizontal + d);             // Šířka rovné části (tj. bez zaoblené části)
            int h = size - (p.Vertical + d);               // Výška rovné části (tj. bez zaoblené části)
            string r1 = _GetXmlNumber(0, d, 4);            // Deklarace křivky, polovina radiusu : pro large je zde "0.5", pro small je zde "0.25"
            string r2 = _GetXmlNumber(0, d, 2);            // Deklarace křivky, celý radius      : pro large je zde "1", pro small je zde "0.50"

            string xml, bx, by, tr, br, bl, tl;
            if (!counterClockWise)
            {   // Ve směru hodinových ručiček
                bx = _GetXmlNumber(p.Left, d, 2);          // Počátek rovné části vlevo úplně nahoře, X
                by = _GetXmlNumber(p.Top, 0, 1);           // Počátek rovné části vlevo úplně nahoře, Y
                tr = _GetXmlQuadCurve(r1, r2, CurveDirections.RightDown);
                br = _GetXmlQuadCurve(r1, r2, CurveDirections.DownLeft);
                bl = _GetXmlQuadCurve(r1, r2, CurveDirections.LeftUp);
                tl = _GetXmlQuadCurve(r1, r2, CurveDirections.UpRight);

                xml = $"M{bx},{by}h{w}{tr}v{h}{br}h-{w}{bl}v-{h}{tl}z " + _GetXmlPathDataFillSquare(size, isBold, true, padding);
            }
            else
            {   // V protisměru
                bx = _GetXmlNumber(p.Left, 0, 1);          // Počátek rovné části úplně vlevo nahoře, X
                by = _GetXmlNumber(p.Top, d, 2);           // Počátek rovné části úplně vlevo nahoře, Y
                bl = _GetXmlQuadCurve(r1, r2, CurveDirections.DownRight);
                br = _GetXmlQuadCurve(r1, r2, CurveDirections.RightUp);
                tr = _GetXmlQuadCurve(r1, r2, CurveDirections.UpLeft);
                tl = _GetXmlQuadCurve(r1, r2, CurveDirections.LeftDown);

                xml = $"M{bx},{by}v{h}{bl}h{w}{br}v-{h}{tr}h-{w}{tl}z " + _GetXmlPathDataFillSquare(size, isBold, false, padding);

            }
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrátí element path, ve tvaru obdélníku vepsaného do dané velikosti (size), s okraji (padding) a s odstupem od okrajů (isBold ? 2 : 1).
        /// Slouží mj. jako výplň do rámečku vráceného v metodě <see cref="_GetXmlPathBorderSquare(int, bool, string, bool, Padding?)"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isBold"></param>
        /// <param name="fillParam"></param>
        /// <param name="counterClockWise">Směr: false = po směru ručiček, true = proti směru</param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathFillSquare(int size, bool isBold, string fillParam, bool counterClockWise = false, Padding? padding = null)
        {
            _ResolveParam(ref fillParam);
            if (size <= 0 || String.IsNullOrEmpty(fillParam)) return "";

            string pathData = _GetXmlPathDataFillSquare(size, isBold, counterClockWise, padding);
            string xml = $@"      <path d='{pathData}' {fillParam} />
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrátí čistá data pro element path, ve tvaru obdélníku vepsaného do dané velikosti (size), s okraji (padding) a s odstupem od okrajů (isBold ? 2 : 1).
        /// Slouží jako výplň do rámečku vráceného v metodě <see cref="_GetXmlPathDataBorderSquare(int, bool, bool, Padding?)"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isBold"></param>
        /// <param name="counterClockWise">Směr: false = po směru ručiček, true = proti směru</param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathDataFillSquare(int size, bool isBold, bool counterClockWise, Padding? padding = null)
        {
            if (size <= 0) return "";

            Padding p = padding ?? Padding.Empty;
            bool isLarge = (size >= 24);
            int s = (isLarge && isBold ? 2 : 1);           // Šířka linky
            int l = p.Left + s;                            // Left, Top, Right, Bottom:
            int t = p.Top + s;
            int r = size - p.Right - s;
            int b = size - p.Bottom - s;
            int w = r - l;
            int h = b - t;

            string xml = !counterClockWise ?
                $"M{l},{t}h{w}v{h}h-{w}v-{h}z" :           // Ve směru hodinových ručiček
                $"M{l},{t}v{h}h{w}v-{h}h-{w}z";            // V protisměru

            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrátí křivku ve tvaru čtvrtkruhu, relativně umístěnou, v daném směru <paramref name="directions"/>;
        /// kde parametr <paramref name="r1"/> určuje půlrádius a <paramref name="r2"/> určuje rádius.
        /// <para/>
        /// Čtvrtkruh vychází z aktuálního bodu ve směru první části směrníku <see cref="CurveDirections"/>, a otáčí se směrem k druhé části směrníku.
        /// Například směrník <see cref="CurveDirections.RightDown"/> jde nejprve doprava, a ohýbá se směrem dolů (jde o pravý horní roh čtverce ve směru hodinových ručiček).
        /// Na rozdíl od toho směrník <see cref="CurveDirections.DownRight"/> jde nejprve dolů, a pak zahýbá doprava (jde o levý dolní roh čtverce proti směru hodinových ručiček).
        /// <para/>
        /// Jako radiusy je třeba zadat text odpovídající počtu pixelů rohu, i jako desetiné číslo, bez znaménka mínus.
        /// Pokud tedy chceme vykreslit oblouk přes jeden pixel, předáme <paramref name="r1"/> = "0.5" a <paramref name="r2"/> = "1".
        /// </summary>
        /// <param name="r1">Půl poloměru oblouku</param>
        /// <param name="r2">Celý poloměr oblouku</param>
        /// <param name="directions"></param>
        /// <returns></returns>
        private static string _GetXmlQuadCurve(string r1, string r2, CurveDirections directions)
        {
            switch (directions)
            {
                case CurveDirections.RightDown: return $"c{r1},0,{r2},{r1},{r2},{r2}";
                case CurveDirections.RightUp: return $"c{r1},0,{r2},-{r1},{r2},-{r2}";
                case CurveDirections.LeftDown: return $"c-{r1},0,-{r2},{r1},-{r2},{r2}";
                case CurveDirections.LeftUp: return $"c-{r1},0,-{r2},-{r1},-{r2},-{r2}";
                case CurveDirections.UpRight: return $"c0,-{r1},{r1},-{r2},{r2},-{r2}";
                case CurveDirections.UpLeft: return $"c0,-{r1},-{r1},-{r2},-{r2},-{r2}";
                case CurveDirections.DownRight: return $"c0,{r1},{r1},{r2},{r2},{r2}";
                case CurveDirections.DownLeft: return $"c0,{r1},-{r1},{r2},-{r2},{r2}";
            }
            return "";
        }
        /// <summary>
        /// Směr křivky
        /// </summary>
        private enum CurveDirections { None, RightDown, RightUp, LeftDown, LeftUp, UpRight, UpLeft, DownRight, DownLeft }
        /// <summary>
        /// Vrací XML text definující RadialGradient
        /// </summary>
        /// <param name="size"></param>
        /// <param name="name"></param>
        /// <param name="centerColorName"></param>
        /// <param name="outerColorName"></param>
        /// <param name="radiusRel"></param>
        /// <returns></returns>
        private static string _GetXmlContentGradientRadial(int size, string name, string centerColorName, string outerColorName = null, int radiusRel = 80)
        {
            int add = size * radiusRel / 1200;
            int c = size / 2;
            int cx = c + add;
            int cy = cx;
            int r = size + (size * radiusRel / 100);
            int fx = c + (size / 8);
            int fy = fx;

            if (String.IsNullOrEmpty(centerColorName)) centerColorName = "green";
            if (String.IsNullOrEmpty(outerColorName)) outerColorName = "white";

            string xml = $@"    <defs>
      <radialGradient id='{name}' gradientUnits='userSpaceOnUse' cx='{cx}' cy='{cy}' r='{r}' fx='{fx}' fy='{fy}'>
        <stop offset='0%' stop-color='{centerColorName}' />
        <stop offset='100%' stop-color='{outerColorName}' />
      </radialGradient>
    </defs>
";
            // alternativa
            xml = $@"    <defs>
      <radialGradient id='{name}' gradientUnits='userSpaceOnUse' cx='{fx}' cy='{fy}' r='{r}' fx='{cx}' fy='{cy}'>
        <stop offset='0%' stop-color='{centerColorName}' />
        <stop offset='100%' stop-color='{outerColorName}' />
      </radialGradient>
    </defs>
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací XML text definující Circle
        /// </summary>
        /// <param name="size"></param>
        /// <param name="fill"></param>
        /// <param name="radiusRel"></param>
        /// <returns></returns>
        private static string _GetXmlContentCircle(int size, string fill, int radiusRel)
        {
            int c = size / 2;
            int cx = c;
            int cy = c;
            int r = c * radiusRel / 100;
            if (r <= 1) return "";
            if (r > size) r = size;

            string xml = $@"    <circle cx='{cx}' cy='{cy}' r='{r}' fill='{fill}' stroke='black' stroke-width='0'  />
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací XML text ukončující SVG image
        /// </summary>
        /// <returns></returns>
        private static string _GetXmlContentFooter()
        {
            string xml = $@"  </g>
</svg>
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Z dodaného pole z prvku na daném indexu vrátí jeho Int hodnotu
        /// </summary>
        /// <param name="genericItems"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static int _GetGenericParam(string[] genericItems, int index, int defaultValue)
        {
            if (genericItems == null || index < 0 || index >= genericItems.Length) return defaultValue;
            string item = genericItems[index];
            if (String.IsNullOrEmpty(item)) return defaultValue;
            if (!Int32.TryParse(item, out int value)) return defaultValue;
            return value;
        }
        /// <summary>
        /// Z dodaného pole z prvku na daném indexu vrátí jeho string hodnotu
        /// </summary>
        /// <param name="genericItems"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static string _GetGenericParam(string[] genericItems, int index, string defaultValue)
        {
            if (genericItems == null || index < 0 || index >= genericItems.Length) return defaultValue;
            return genericItems[index];
        }
        /// <summary>
        /// Z dodaného pole z prvku na daném indexu vrátí jeho Color hodnotu.
        /// Barvy mají odpovídat deklaraci:
        /// https://www.w3.org/TR/SVG11/types.html#ColorKeywords
        /// </summary>
        /// <param name="genericItems"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static Color _GetGenericParam(string[] genericItems, int index, Color defaultValue)
        {
            if (genericItems == null || index < 0 || index >= genericItems.Length) return defaultValue;
            string item = genericItems[index];
            if (String.IsNullOrEmpty(item)) return defaultValue;
            object value = Convertor.StringToColor(item);
            if (!(value is Color)) return defaultValue;
            return (Color)value;
        }
        /// <summary>
        /// Konvertuje dodanou barvu do stringu vhodného pro SVG
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static string _GetXmlColor(Color color)
        {
            if (color.IsNamedColor) return color.Name.ToLower();
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
        /// <summary>
        /// Vrátí string obsahující výsledek (a + b / c) vyhovující XML zápisu (desetinná tečka, bez mezer, bez koncových nul)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        private static string _GetXmlNumber(int a, int b, int c, int decimals = 3)
        {
            decimal r = (decimal)a + ((decimal)b / (decimal)c);
            string t = r.ToString();
            if (t.Contains(",")) t = t.Replace(",", ".");
            if (t.Contains(" ")) t = t.Replace(" ", "");
            if (t.Contains("."))
            {
                while (t.Length > 1 && t.EndsWith("0"))
                    t = t.Substring(0, t.Length - 1);
            }
            return t;
        }
        /// <summary>
        /// Ošetří dodaný parametr. Vstup smí být prázdný, nebo může obsahovat kompletní definici vzhledu ("fill='#e02080' stroke='Black'")
        /// Pokud je prázdný, na výstupu je "".
        /// Pokud je zadán a neobsahuje rovnítko, předsadí:    class='param'.
        /// Jinak jej ponechá beze změny.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="defValue"></param>
        private static void _ResolveParam(ref string param, string defValue = null)
        {
            if (String.IsNullOrEmpty(param))
                param = defValue ?? "";
            else if (param.IndexOf("=") < 0)
                param = "class='" + param + "'";
        }
        #endregion
        #endregion
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
    #region SvgImageSupport : podpora práce se SVG images na straně klienta
    /// <summary>
    /// <see cref="SvgImageSupport"/> : podpora práce se SVG images na straně klienta
    /// </summary>
    internal class SvgImageSupport
    {
        #region Podpora pro kombinování více ikon (SvgImageArrayInfo)
        /// <summary>
        /// Vrátí true, pokud dodaný string představuje instanci <see cref="SvgImageSupport"/>, a pokud ano pak ji rovnou vytvoří.
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

            // Na vstupu jsou jména SVG obrázků, tady si získám obrázky reálné:
            List<SvgItemInfo> svgInfos = new List<SvgItemInfo>();
            foreach (var svgItem in svgImageArray.Items)
            {
                SvgImage svgImage = DxComponent.GetVectorImage(svgItem.ImageName, false, sizeType);          // Požadovaná velikost (sizeType) se uplatňuje pouze tady = při výběru ikon (small / large)
                SvgItemInfo svgInfo = new SvgItemInfo(svgItem, svgImage);
                svgInfos.Add(svgInfo);
            }

            // Projdu vstupní ikony, vyberu z nich ty "bez transformace" = ty by měly být zobrazeny "nativně", a z nich zkusím odvodit sumární velikost pro výslednou ikonu:
            RectangleF? bounds = svgInfos
                .Where(s => !s.HasTransform)
                .Select(s => s.SvgImageBounds)
                .SummaryVisibleRectangle();

            // Vytvořím základní SVG pro "podklad" = pro souřadnice sečtené z "nativních" (netransformovaných) ikon (cílový prostor má default 120p, pokud se nepodařilo najít nativní ikony):
            if (!bounds.HasValue) bounds = new RectangleF(0f, 0f, 120f, 120f);
            DxSvgImage svgImageOut = _CreateBaseSvg(svgImageArray.Key, bounds.Value);

            // Kombinace vstupních SvgImages pomocí SvgGroup [plus transformací] do cílového svgImageOut:
            foreach (var svgInfo in svgInfos)
            {
                SvgImage svgImageInp = svgInfo.SvgImage;
                if (svgImageInp is null) continue;

                SvgGroup svgGroupSum = new SvgGroup();
                bool hasTransform = svgInfo.HasTransform;
                bool isValid = (!hasTransform || (hasTransform && _SetTransformForImage(svgInfo, bounds.Value, svgGroupSum)));
                if (isValid)
                {
                    svgImageInp.Root.Elements.ForEachExec(e => svgGroupSum.Elements.Add(e.DeepCopy()));
                    svgImageOut.Root.Elements.Add(svgGroupSum);
                }
            }

            return svgImageOut;
        }
        /// <summary>
        /// Vrátí prázdný SVG image v dané cílové velikosti
        /// </summary>
        /// <param name="name"></param>
        /// <param name="boundsF"></param>
        /// <returns></returns>
        private static DxSvgImage _CreateBaseSvg(string name, RectangleF boundsF)
        {
            Rectangle bounds = Rectangle.Ceiling(boundsF);
            string xml = $"<svg x='{bounds.X}px' y='{bounds.Y}px' width='{bounds.Width}px' height='{bounds.Height}px' viewBox='{bounds.X} {bounds.Y} {bounds.Width} {bounds.Height}' xmlns='http://www.w3.org/2000/svg'></svg>";
            xml = xml.Replace("'", "\"");
            return DxSvgImage.Create(name, false, xml); ;
        }
        /// <summary>
        /// Metoda nastaví do <paramref name="svgGroupSum"/> sadu transformací tak, 
        /// aby vstupující SvgImage z <paramref name="svgInfo"/> 
        /// byl správně umístěn do relativního prostoru podle daného předpisu, v rámci reálného cílového prostoru <paramref name="bounds"/>.
        /// </summary>
        /// <param name="svgInfo">Vstupní image (definice plus SvgImage)</param>
        /// <param name="bounds">Reálný cílový prostor</param>
        /// <param name="svgGroupSum">Grupa pro vložení transformace</param>
        /// <returns></returns>
        private static bool _SetTransformForImage(SvgItemInfo svgInfo, RectangleF bounds, SvgGroup svgGroupSum)
        {
            // Kontroly - sem máme chodit jen když jsou definované podklady, a ty definují potřebu transformace:
            if (svgInfo is null || svgInfo.SvgImage is null || !svgInfo.ImageRelativeBounds.HasValue || svgInfo.SvgItem is null || !svgInfo.SvgImageBounds.HasValue || svgGroupSum is null) return false;

            // Pokud dodaná ikona je neviditelná, skončím:
            RectangleF currentBounds = svgInfo.SvgImageBounds.Value;
            if (currentBounds.Width <= 0f || currentBounds.Height <= 0f) return false;

            // Určím relativní pozici aktuální ikony v rámci celku:
            float baseSize = SvgImageArrayInfo.BaseSize;
            RectangleF targetData = svgInfo.ImageRelativeBounds.Value;
            RectangleF targetRatio = new RectangleF(targetData.X / baseSize, targetData.Y / baseSize, targetData.Width / baseSize, targetData.Height / baseSize);                            // Ratio = požadované umístění v rozsahu 0-1
            RectangleF targetBounds = new RectangleF(targetRatio.X * bounds.Width, targetRatio.Y * bounds.Height, targetRatio.Width * bounds.Width, targetRatio.Height * bounds.Height);     // Pixelové souřadnice cílového prostoru (nejde ještě o souřadnice konkrétní ikony, ale jen reálný prostor pro ni)

            // Vezmu rozměry aktuální reálné ikony a zmenším je do velikosti 'targetBounds' se zachováním poměru stran:
            //   (určím přepočtový Zoom z current do target Bounds, tak abych daný prostor targetBounds nepřelezl)
            float zoomW = targetBounds.Width / currentBounds.Width;
            float zoomH = targetBounds.Height / currentBounds.Height;
            float zoom = (zoomW < zoomH ? zoomW : zoomH);
            float imageW = zoom * currentBounds.Width;
            float imageH = zoom * currentBounds.Height;

            // Určím souřadnice reálné image tak, abych dodržel pozici reálné image v rámci bounds podle požadavku target:
            float imageX = _GetImageBegin(bounds.Width, targetBounds.X, targetBounds.Width, imageW);
            float imageY = _GetImageBegin(bounds.Height, targetBounds.Y, targetBounds.Height, imageH);
            RectangleF imageBounds = new RectangleF(imageX, imageY, imageW, imageH);

            string log = BoundsToLog("TotalBounds:", bounds) +
                         BoundsToLog("; ImageRelativeBounds:", targetData) +
                         BoundsToLog("; targetRatio:", targetRatio) +
                         BoundsToLog("; targetBounds:", targetBounds) +
                         BoundsToLog("; imageBounds:", imageBounds);

            // Určím a vygeneruji SVG transformace:
            if (imageX != 0f || imageY != 0f)
                svgGroupSum.Transformations.Add(new SvgTranslate(new double[] { imageX, imageY }));

            if (zoom != 1f)
                svgGroupSum.Transformations.Add(new SvgScale(new double[] { zoom, zoom }));

            return true;
        }
        /// <summary>
        /// Vrátí počátek reálného prostoru <paramref name="realSize"/> v celkovém prostoru <paramref name="totalSize"/> tak, aby vyhovoval zadání <paramref name="targetBegin"/> a <paramref name="targetSize"/>.
        /// </summary>
        /// <param name="totalSize"></param>
        /// <param name="targetBegin"></param>
        /// <param name="targetSize"></param>
        /// <param name="realSize"></param>
        /// <returns></returns>
        private static float _GetImageBegin(float totalSize, float targetBegin, float targetSize, float realSize)
        {
            if (targetBegin <= 0f) return 0f;                                                      // Real je úplně na začátku
            if ((targetBegin + targetSize) >= totalSize) return (totalSize - realSize);            // Real je úplně na konci
            if (realSize >= totalSize) return 0f;                                                  // Real je větší než Total, bude na začátku

            // Real je někde uprostřed Total, přihlédneme k pozici prostoru Target v rámci Total:
            float targetRatio = targetBegin / (totalSize - targetSize);
            return targetRatio * (totalSize - realSize);
        }
        /// <summary>
        /// Bounds to log
        /// </summary>
        /// <param name="title"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private static string BoundsToLog(string title, RectangleF bounds)
        {
            return $"{title}{{ X={bounds.X}, Y={bounds.Y}, W={bounds.Width}, H={bounds.Height}, R={bounds.Right}, B={bounds.Bottom} }}";
        }
        /// <summary>
        /// Data o jedné položce kombinovaného SVG: podklady plus reálný obrázek
        /// </summary>
        private class SvgItemInfo
        {
            public SvgItemInfo(SvgImageArrayItem svgItem, SvgImage svgImage)
            {
                this.SvgItem = svgItem;
                this.SvgImage = svgImage;
            }
            /// <summary>
            /// Vstupní definice ikony (jméno a relativní umístění)
            /// </summary>
            public SvgImageArrayItem SvgItem { get; private set; }
            /// <summary>
            /// Nalezený reálný SvgImage
            /// </summary>
            public SvgImage SvgImage { get; private set; }
            /// <summary>
            /// Souřadnice cílové, ve 120px virtuálním prostoru
            /// </summary>
            public Rectangle? ImageRelativeBounds { get { return SvgItem.ImageRelativeBounds; } }
            /// <summary>
            /// Obsahuje true, pokud this prvek bude transformován do jiné než výchozí pozice
            /// </summary>
            public bool HasTransform
            {
                get
                {
                    var bounds = this.ImageRelativeBounds;
                    if (!bounds.HasValue) return false;              // Pokud nejsou definované Target souřadnice, nemáme transformaci.
                    var b = bounds.Value;
                    bool isWhole = (b.X == 0 && b.Y == 0 && b.Width == 120 && b.Height == 120);   // true pokud souřadnice jsou "plný rozměr" = 100% = 120p
                    return !isWhole;                                 // Pokud NEJSME 100%, pak máme transformace!
                }
            }
            /// <summary>
            /// Souřadnice uvedené reálně v ikoně <see cref="SvgImage"/>
            /// </summary>
            public RectangleF? SvgImageBounds
            {
                get
                {
                    var svgImage = this.SvgImage;
                    if (svgImage is null) return null;
                    return new RectangleF((float)svgImage.OffsetX, (float)svgImage.OffsetY, (float)svgImage.Width, (float)svgImage.Height);
                }
            }
        }
        #endregion

        #region Podpora pro konverzi SVG ikon na paletové barvy - TODO

        #endregion
        

    }
    #endregion
}
