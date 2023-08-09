// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

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
using System.Runtime.CompilerServices;

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
        { return Instance._CreateBitmapImage(new ResourceArgs(imageName, exactName, sizeType, optimalSvgSize, null, caption, isPreferredVectorImage)); }
        /// <summary>
        /// Vygeneruje a vrátí nový obrázek daného jména.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImage(ResourceArgs args)
        {
            bool hasName = args.HasImageName;
            bool hasCaption = args.HasCaption;

            if (hasName && DxSvgImage.TryGetXmlContent(args.ImageName, args.SizeType, out var dxSvgImage))
                return _ConvertVectorToImage(dxSvgImage, args);
            if (hasName && _TryGetContentTypeImageArray(args.ImageName, out var _, out var svgImageArray))
                return _CreateBitmapImageArray(svgImageArray, args);

            if (hasName && _TryGetApplicationResources(args, ResourceContentType.StandardImage, out var validItems))
                return _CreateBitmapImageApplication(validItems, args);

            if (hasName && _ExistsDevExpressResource(args.ImageName))
                return _CreateBitmapImageDevExpress(args);
            if (hasCaption)
                return _CreateCaptionImage(args);

            return null;
        }
        /// <summary>
        /// Vrátí defaultní nezoomovanou velikost, tedy 16x16 / 24x24 / 32x32
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static Size GetDefaultImageSize(ResourceImageSizeType? sizeType)
        {
            return GetImageSize(sizeType, false, null);
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
            int s;
            switch (sizeType.Value)
            {
                case ResourceImageSizeType.None:
                    s = 0;
                    break;
                case ResourceImageSizeType.Small:
                    s = 16;
                    break;
                case ResourceImageSizeType.Large:
                case ResourceImageSizeType.Original:
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
        /// Metoda vrátí optimální velikost pro Image tak, aby se vešel do dané velikosti, 
        /// s případně danými okraji, zarovnanou dolů na násobky 8, nejméně 16 x 16 (vždy vrací čtverec)
        /// </summary>
        /// <param name="size"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static Size GetOptimalImageSize(Size size, int padding = 0)
        {
            int s = (size.Width < size.Height ? size.Width : size.Height);
            if (padding > 0) s = s - (2 * padding);
            if (s < 24) s = 16;
            else if (s < 32) s = 24;
            else if (s < 40) s = 32;
            else if (s < 48) s = 40;
            else if (s < 64) s = 48;
            else s = 64;
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
        /// <summary>
        /// Vyrenderuje daný <see cref="SvgImage"/> do Image
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="sizeType"></param>
        /// <param name="targetDpi"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        public static Image RenderSvgImage(SvgImage svgImage, ResourceImageSizeType sizeType, int? targetDpi = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            if (svgImage == null) return null;
            Size imageSize = GetImageSize(sizeType, true, targetDpi);
            return Instance._RenderSvgImage(svgImage, imageSize, svgPalette);
        }
        /// <summary>
        /// Vyrenderuje daný <see cref="SvgImage"/> do Image
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="imageSize"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        public static Image RenderSvgImage(SvgImage svgImage, Size imageSize, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            if (svgImage == null) return null;
            return Instance._RenderSvgImage(svgImage, imageSize, svgPalette);
        }
        /// <summary>
        /// Vyrenderuje daný <see cref="SvgImage"/> do Image
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="imageSize"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        private Image _RenderSvgImage(SvgImage svgImage, Size imageSize, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette)
        {
            if (svgImage == null) return null;
            Image image = DxSvgImage.RenderToImage(svgImage, imageSize, ContentAlignment.MiddleCenter, svgPalette);
            return image;
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
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <returns></returns>
        public static SvgImage CreateVectorImage(string imageName, bool exactName = false, ResourceImageSizeType? sizeType = null, string caption = null, bool? disabled = null)
        { return Instance._CreateVectorImage(new ResourceArgs(imageName, exactName, sizeType, caption: caption, disabled: disabled)); }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="exactName"></param>
        /// <param name="sizeType"></param>
        /// <param name="caption"></param>
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <returns></returns>
        public static SvgImage GetVectorImage(string imageName, bool exactName = false, ResourceImageSizeType? sizeType = null, string caption = null, bool? disabled = null)
        { return Instance._GetVectorImage(new ResourceArgs(imageName, exactName, sizeType, caption: caption, disabled: disabled)); }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private SvgImage _CreateVectorImage(ResourceArgs args)
        {
            return _GetVectorImage(args);
        }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress i Aplikačních zdrojích
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImage(ResourceArgs args)
        {
            if (!args.HasImageName) return null;

            bool hasName = args.HasImageName;
            bool hasCaption = args.HasCaption;

            if (hasName && DxSvgImage.TryGetXmlContent(args.ImageName, args.SizeType, out var dxSvgImage))
                return dxSvgImage;
            if (hasName && SvgImageSupport.TryGetSvgImageArray(args.ImageName, out var svgImageArray))
                return _GetVectorImageArray(svgImageArray, args);
            if (hasName && _TryGetApplicationResources(args, ResourceContentType.Vector, out var validItems))
                return _GetVectorImageApplication(validItems, args);
            if (hasName && _ExistsDevExpressResource(args.ImageName) && _IsImageNameSvg(args.ImageName))
                return _GetVectorImageDevExpress(args);
            if (hasCaption)
                return _CreateCaptionVector(args);
            return null;
        }
        /// <summary>
        /// Dodané pole vektorových images vyrenderuje do Image a ty zabalí a vrátí do jedné Icon
        /// </summary>
        /// <param name="vectorItems"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _ConvertApplicationVectorsToIcon(DxApplicationResourceLibrary.ResourceItem[] vectorItems, ResourceArgs args)
        {
            List<Tuple<Size, Image, string>> imageInfos = new List<Tuple<Size, Image, string>>();
            if (args.SizeType.HasValue)
            {
                if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(vectorItems, args.SizeType, out var vectorItem))
                    imageInfos.Add(new Tuple<Size, Image, string>(GetDefaultImageSize(vectorItem.SizeType), _ConvertApplicationVectorToImage(vectorItem, args), vectorItem.ItemKey));
            }
            if (imageInfos.Count == 0)
            {
                foreach (var vectorItem in vectorItems)
                    imageInfos.Add(new Tuple<Size, Image, string>(GetDefaultImageSize(vectorItem.SizeType), _ConvertApplicationVectorToImage(vectorItem, args), vectorItem.ItemKey));
            }
            var icon = _ConvertBitmapsToIcon(imageInfos);
            _DisposeImages(imageInfos.Select(ii => ii.Item2));
            return icon;
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do bitmapy
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _ConvertVectorToImage(SvgImage svgImage, ResourceArgs args)
        {
            if (svgImage is null) return null;
            var imageSize = args.OptimalSvgSize ?? GetDefaultImageSize(args.SizeType);
            var svgPalette = DxComponent.GetSvgPalette();
            if (SystemAdapter.CanRenderSvgImages)
                return SystemAdapter.RenderSvgImage(svgImage, imageSize, svgPalette);
            return svgImage.Render(imageSize, svgPalette);
        }
        /// <summary>
        /// Vyrenderuje SVG obrázek do ikony
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _ConvertVectorToIcon(SvgImage svgImage, ResourceArgs args)
        {
            Icon icon = null;
            using (var image = _ConvertVectorToImage(svgImage, args))
            {
                List<Tuple<Size, Image, string>> imageInfos = new List<Tuple<Size, Image, string>>();
                imageInfos.Add(new Tuple<Size, Image, string>(image.Size, image, args.ImageName));
                icon = _ConvertBitmapsToIcon(imageInfos);
                _DisposeImages(imageInfos.Select(ii => ii.Item2));
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
        { return Instance._CreateIconImage(new ResourceArgs(imageName, exactName, sizeType)); }
        /// <summary>
        /// Metoda najde a vrátí ikonu pro dané jméno (a velikost) zdroje.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _CreateIconImage(ResourceArgs args)
        {
            if (!args.HasImageName) return null;

            // Může to být explicitní SVG XML content:
            if (DxSvgImage.TryGetXmlContent(args.ImageName, args.SizeType, out var dxSvgImage))
                return _ConvertVectorToIcon(dxSvgImage, args);

            // Může to být pole SVG images:
            if (_TryGetContentTypeImageArray(args.ImageName, out var _, out var svgImageArray))
                return _ConvertImageArrayToIcon(svgImageArray, args);

            // Pro dané jméno zdroje máme k dispozici resource s typem Icon:
            DxApplicationResourceLibrary.ResourceItem[] validItems;
            if (_TryGetApplicationResources(args, ResourceContentType.Icon, out validItems))
            {   // Tady můžeme vrátit jen jednu ikonu, podle požadované velikosti:
                DxApplicationResourceLibrary.ResourceItem iconItem =
                    ((validItems.Length > 1 && DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(validItems, args.SizeType, out var item)) ?
                    item : validItems[0]);
                return iconItem?.CreateIconImage();
            }

            // Najdeme v aplikačních zdrojích Vektor nebo Bitmapu (podle preferencí)? Pokud ano, vyřešíme ji:
            if (_TryGetApplicationResources(args, ResourceContentType.BasicImage, out validItems, out var validType))
                return _ConvertApplicationResourceToIcon(validItems, validType, args);

            // Může to být Image z DevExpress?
            if (_ExistsDevExpressResource(args.ImageName))
            {
                using (var bitmap = _CreateBitmapImageDevExpress(args))
                    return _ConvertBitmapToIcon(bitmap);
            }

            return null;
        }
        /// <summary>
        /// Konvertuje aplikační obrázek (bitmapu / image) na ikonu
        /// </summary>
        /// <param name="validItems"></param>
        /// <param name="contentType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _ConvertApplicationResourceToIcon(DxApplicationResourceLibrary.ResourceItem[] validItems, ResourceContentType contentType, ResourceArgs args)
        {
            switch (contentType)
            {
                case ResourceContentType.Bitmap:
                    return _ConvertApplicationBitmapsToIcon(validItems, args);
                case ResourceContentType.Vector:
                    return _ConvertApplicationVectorsToIcon(validItems, args);
            }
            return null;
        }
        /// <summary>
        /// Dané pole Resource převede na bitmapy a ty vloží do ikony, kterou vrátí.
        /// Pokud je daná velikost <paramref name="args"/>, pak výsledná ikona bude obsahovat jen jednu ikonu (bitmapu) v požadované velikosti.
        /// Pokud je <paramref name="args"/> = null, pak výsledná ikona bude obsahovat všechny dostupné bitmapy.
        /// </summary>
        /// <param name="bitmapItems"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _ConvertApplicationBitmapsToIcon(DxApplicationResourceLibrary.ResourceItem[] bitmapItems, ResourceArgs args)
        {
            if (bitmapItems.Length == 0) return null;

            List<Tuple<Size, Image, string>> imageInfos = new List<Tuple<Size, Image, string>>();
            if (args.SizeType.HasValue)
            {
                if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(bitmapItems, args.SizeType, out var bitmapItem))
                    imageInfos.Add(new Tuple<Size, Image, string>(GetDefaultImageSize(bitmapItem.SizeType), bitmapItem.CreateBmpImage(), bitmapItem.ItemKey));
            }
            if (imageInfos.Count == 0)
            {
                foreach (var bitmapItem in bitmapItems)
                    imageInfos.Add(new Tuple<Size, Image, string>(GetDefaultImageSize(bitmapItem.SizeType), bitmapItem.CreateBmpImage(), bitmapItem.ItemKey));
            }
            var icon = _ConvertBitmapsToIcon(imageInfos);
            _DisposeImages(imageInfos.Select(ii => ii.Item2));
            return icon;
        }
        /// <summary>
        /// Danou Bitmapu převede do ikony a vrátí ji.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="disposeImage"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapToIcon(Image bitmap, bool disposeImage = false, string name = null)
        {
            if (bitmap == null) return null;

            // Konverzi zajistí metoda _ConvertBitmapsToIcon(), která požaduje na vstupu pole Image a jejich Size (imageInfos):
            List<Tuple<Size, Image, string>> imageInfos = new List<Tuple<Size, Image, string>>();
            imageInfos.Add(new Tuple<Size, Image, string>(bitmap.Size, bitmap, name));
            var icon = _ConvertBitmapsToIcon(imageInfos);
            if (disposeImage)    // Tady bude Dispose jen na požadavek, typicky ne: Image vzniká jinde, ať si jej disposuje ten kdo jej vytvořil!
                _DisposeImages(imageInfos.Select(ii => ii.Item2));
            return icon;
        }
        /// <summary>
        /// Dané pole obrázků převede do formátu ICO a vrátí ji, bezpečná obálka
        /// </summary>
        /// <param name="imageInfos"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapsToIcon(List<Tuple<Size, Image, string>> imageInfos)
        {
            // DAJ 0070675 28.2.2022: odfiltrujeme nesmyslné požadavky:
            if (imageInfos is null || imageInfos.Count == 0) return null;
            imageInfos = imageInfos.Where(i => (i.Item1.Width >= 8 && i.Item1.Width <= 64 && i.Item1.Height >= 8 && i.Item1.Height <= 64)).ToList();
            if (imageInfos.Count == 0) return null;

            // Simulace chyby:
            var imageInfo = imageInfos[0];
            imageInfos = new List<Tuple<Size, Image, string>>();
            imageInfos.Add(new Tuple<Size, Image, string>(new Size(0, 0), imageInfo.Item2, imageInfo.Item3));

            // DAJ 0070675 28.2.2022: odstíníme chyby:
            try
            {
                return _ConvertBitmapsToIconExec(imageInfos);
            }
            catch (Exception exc)
            {
                string names = imageInfos.Select(ii => ii.Item3).Where(n => !String.IsNullOrEmpty(n)).ToOneString(", ");
                SystemAdapter.TraceText(TraceLevel.Error, typeof(DxComponent), nameof(_ConvertBitmapsToIcon), "INVALID_ICON", $"_ConvertBitmapsToIcon() error: {exc.Message}; ImageNames: {names}");
            }
            return null;
        }
        /// <summary>
        /// Dané pole obrázků převede do formátu ICO a vrátí ji, výkonná metoda
        /// </summary>
        /// <param name="imageInfos"></param>
        /// <returns></returns>
        private Icon _ConvertBitmapsToIconExec(List<Tuple<Size, Image, string>> imageInfos)
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
                        var image = imageInfo.Item2;
                        if (image is null) continue;
                        // DAJ 0070143 2021-12-20: do ICO formátu mohu nativně převést jen vstupní formát 32bpp PNG;
                        //   ostatní formáty musím do odpovídajícího formátu nejprve vykreslit (vyrenderovat):
                        if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) // Není nutno mít na vstupu explicitně PNG:  && image.RawFormat == System.Drawing.Imaging.ImageFormat.Png)
                            // Nativní PNG můžu do ICO uložit rovnou:
                            image.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);
                        else
                        {   // Nemám-li na vstupu 32bpp PNG obrázek, musím si jej vytvořit:
                            using (var bitmap = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                            using(var graphics = Graphics.FromImage(bitmap))
                            {   // Dodaný image (typicky GIF) vykreslím do Grafiky (která je 32bpp) a uložím do streamu jako PNG:
                                graphics.DrawImage(image, Point.Empty);
                                bitmap.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }

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
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _ConvertIconToBitmap(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            Image image = null;
            if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, args.SizeType, out var resourceItem) && resourceItem.IsIcon)
                image = _ConvertIconToBitmap(resourceItem, args);
            return image;
        }
        /// <summary>
        /// Z dodaného zdroje vezme ikonu, konvertuje ji na Bitmapu a vrátí ji.
        /// </summary>
        /// <param name="resourceItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _ConvertIconToBitmap(DxApplicationResourceLibrary.ResourceItem resourceItem, ResourceArgs args)
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
        /// <param name="images"></param>
        private void _DisposeImages(IEnumerable<Image> images)
        {
            if (images == null) return;
            foreach (var image in images)
                image?.Dispose();
        }
        #endregion
        #region TryGetResource - hledání aplikačního zdroje
        /// <summary>
        /// Metoda vrací true, pokud aplikační zdroje obsahují daný zdroj
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="exactName">Hledat přesně daný název?</param>
        /// <param name="contentTypes">Druhy obsahu, bez zadání = Bitmapy a vektory, dle preference</param>
        /// <param name="sizeType">Vyhledat danou velikost, default = Large</param>
        /// <param name="isPreferredVectorImage">Preferovat true = vektory / false = bitmapy / null = podle systému</param>
        /// <returns></returns>
        public static bool ContainsApplicationResource(string imageName, bool exactName = false, ResourceContentType? contentTypes = null, ResourceImageSizeType? sizeType = null, bool? isPreferredVectorImage = null)
        { return Instance._TryGetApplicationResource(new ResourceArgs(imageName, exactName, sizeType, isPreferredVectorImage: isPreferredVectorImage), contentTypes, out var _); }
        /// <summary>
        /// Metoda se pokusí najít zdroj v Aplikačních zdrojích, pro dané jméno.
        /// Prohledává obrázky vektorové a bitmapové, může preferovat vektory - pokud <paramref name="isPreferredVectorImage"/> je true.
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="resourceItem">Výstup - nalezeného zdroje</param>
        /// <param name="contentTypes">Druhy obsahu, bez zadání = Bitmapy a vektory, dle preference</param>
        /// <param name="sizeType">Vyhledat danou velikost, default = Large</param>
        /// <param name="isPreferredVectorImage">Preferovat true = vektory / false = bitmapy / null = podle systému</param>
        /// <param name="exactName">Hledat přesně daný název?</param>
        /// <returns></returns>
        public static bool TryGetApplicationResource(string imageName, out DxApplicationResourceLibrary.ResourceItem resourceItem, bool exactName = false, ResourceContentType? contentTypes = null, ResourceImageSizeType? sizeType = null, bool? isPreferredVectorImage = null)
        { return Instance._TryGetApplicationResource(new ResourceArgs(imageName, exactName, sizeType, isPreferredVectorImage: isPreferredVectorImage), contentTypes, out resourceItem); }
        /// <summary>
        /// Zkusí najít jeden nejvhodnější zdroj
        /// </summary>
        /// <param name="args"></param>
        /// <param name="contentTypes">Druhy obsahu, bez zadání = Bitmapy a vektory, dle preference</param>
        /// <param name="resourceItem"></param>
        /// <returns></returns>
        private bool _TryGetApplicationResource(ResourceArgs args, ResourceContentType? contentTypes, out DxApplicationResourceLibrary.ResourceItem resourceItem)
        {
            resourceItem = null;
            if (!_TryGetApplicationResources(args, contentTypes ?? ResourceContentType.BasicImage, out var validItems)) return false;
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(validItems, args.SizeType, out resourceItem)) return false;
            return true;
        }
        /// <summary>
        /// Metoda se pokusí najít požadované zdroje v Aplikačních zdrojích, pro dané jméno.
        /// </summary>
        /// <param name="imageName">Jméno zdroje</param>
        /// <param name="exactName"></param>
        /// <param name="validTypes"></param>
        /// <param name="resourceItems">Výstup - nalezené zdroje</param>
        /// <returns></returns>
        public static bool TryGetApplicationResources(string imageName, bool exactName, ResourceContentType validTypes, out DxApplicationResourceLibrary.ResourceItem[] resourceItems)
        { return Instance._TryGetApplicationResources(new ResourceArgs(imageName, exactName), validTypes, out resourceItems); }
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
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <param name="prepareDisabledImage">Požadavek na přípravu ikony typu 'Disabled' i pro ten prvek Ribbonu, který má aktuálně hodnotu Enabled = true;</param>
        public static void ApplyImage(ImageOptions imageOptions, string imageName = null, Image image = null, ResourceImageSizeType? sizeType = null, Size? imageSize = null, bool smallButton = false, string caption = null, bool exactName = false, bool? disabled = null, bool prepareDisabledImage = false)
        { Instance._ApplyImage(imageOptions, new ResourceArgs(imageName, exactName, sizeType, imageSize, null, caption, null, disabled, image, smallButton, false, prepareDisabledImage)); }
        /// <summary>
        /// ApplyImage - do cílového objektu vepíše obrázek podle toho, jak je zadán a kam má být vepsán
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImage(ImageOptions imageOptions, ResourceArgs args)
        {
            bool hasImage = args.HasImage;
            bool hasName = args.HasImageName;
            bool hasCaption = args.HasCaption;
            try
            {
                if (hasImage)
                {
                    _ApplyImageRaw(imageOptions, args);
                }
                else if (hasName || hasCaption)
                {
                    if (hasName && DxSvgImage.TryGetXmlContent(args.ImageName, args.SizeType, out var dxSvgImage))
                        _ApplyDxSvgImage(imageOptions, dxSvgImage, args);
                    else if (hasName && SvgImageSupport.TryGetSvgImageArray(args.ImageName, out var svgImageArray))
                        _ApplyImageArray(imageOptions, svgImageArray, args);
                    else if (hasName && _TryGetApplicationResources(args, _ValidImageTypes, out var validItems, out ResourceContentType contentType))
                        _ApplyImageApplication(imageOptions, validItems, contentType, args);
                    else if (hasName && _ExistsDevExpressResource(args.ImageName))
                        _ApplyImageDevExpress(imageOptions, args);
                    else if (hasCaption)
                        _ApplyImageForCaption(imageOptions, args);
                }
                else
                {
                    try
                    {
                        imageOptions.SvgImage = null;
                        imageOptions.Image = null;
                        imageOptions.ImageUri = null;
                        imageOptions.Reset();
                    }
                    catch { }
                }
            }
            catch (Exception) { /* Někdy může dojít k chybě uvnitř DevExpress. I jejich vývojáři jsou jen lidé... */ }

            // Malá služba nakonec:
            if (args.SmallButton && imageOptions is SimpleButtonImageOptions buttonImageOptions)
                buttonImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
        }
        /// <summary>
        /// Aplikuje dodanou bitmapu do <see cref="ImageOptions"/>
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageRaw(ImageOptions imageOptions, ResourceArgs args)
        {
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barItemImageOptions)
            {
                barItemImageOptions.Image = args.Image;
                barItemImageOptions.LargeImage = args.Image;
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions imageCollectionImageOptions)
            {
                imageCollectionImageOptions.Image = args.Image;
            }
            else
            {
                imageOptions.Image = args.Image;
            }
        }
        /// <summary>
        /// Aplikuje explicitně dodaný <see cref="SvgImage"/> do daného objektu
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="dxSvgImage"></param>
        /// <param name="args"></param>
        private void _ApplyDxSvgImage(ImageOptions imageOptions, DxSvgImage dxSvgImage, ResourceArgs args)
        {
            if (imageOptions == null || dxSvgImage == null) return;
            imageOptions.Reset();
            imageOptions.SvgImage = dxSvgImage;
            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Aplikuje Image typu Array (ve jménu obrázku je více zdrojů) do daného cíle v <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="svgImageArray"></param>
        /// <param name="args"></param>
        private void _ApplyImageArray(ImageOptions imageOptions, SvgImageArrayInfo svgImageArray, ResourceArgs args)
        {
            if (svgImageArray == null) return;

            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Má prostor pro dvě velikosti obrázku najednou:
                barOptions.Image = null;
                barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    int smallIndex = _GetVectorImageIndex(svgImageArray, args.CloneForSize(ResourceImageSizeType.Small));
                    int largeIndex = _GetVectorImageIndex(svgImageArray, args.CloneForSize(ResourceImageSizeType.Large));
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
                    barOptions.SvgImage = _GetVectorImageArray(svgImageArray, args);
                    _ApplySvgImageSize(barOptions, args);
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                iciOptions.Image = null;
                if (iciOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                    iciOptions.SvgImage = null;
                    iciOptions.SvgImageSize = Size.Empty;
                    iciOptions.ImageIndex = _GetVectorImageIndex(svgImageArray, args);
                }
                else
                {   // Musíme tam dát přímo SvgImage:
                    iciOptions.SvgImage = _GetVectorImageArray(svgImageArray, args);
                    _ApplySvgImageSize(iciOptions, args);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                imageOptions.Image = null;
                imageOptions.SvgImage = _GetVectorImageArray(svgImageArray, args);
                _ApplySvgImageSize(imageOptions, args);
            }
            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Aplikuje Image typu Vector nebo Bitmap (podle přípony) ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageApplication(ImageOptions imageOptions, ResourceArgs args)
        {
            if (_TryGetApplicationResources(args, ResourceContentType.StandardImage, out var validItems, out ResourceContentType contentType))
                _ApplyImageApplication(imageOptions, validItems, contentType, args);
        }
        /// <summary>
        /// Aplikuje Image typu Vector nebo Bitmap (podle přípony) ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="validItems"></param>
        /// <param name="contentType"></param>
        /// <param name="args"></param>
        private void _ApplyImageApplication(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] validItems, ResourceContentType contentType, ResourceArgs args)
        {
            switch (contentType)
            {
                case ResourceContentType.Vector:
                    _ApplyImageApplicationSvg(imageOptions, validItems, args);
                    break;
                case ResourceContentType.Bitmap:
                    _ApplyImageApplicationBmp(imageOptions, validItems, args);
                    break;
                case ResourceContentType.Icon:
                    _ApplyImageApplicationIco(imageOptions, validItems, args);
                    break;
            }
        }
        /// <summary>
        /// Aplikuje Image typu Vector ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="args"></param>
        private void _ApplyImageApplicationSvg(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Máme prostor pro dvě velikosti obrázku najednou:
                // barOptions.Reset();
                if (barOptions.Image != null) barOptions.Image = null;
                if (barOptions.LargeImage != null) barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    //  (Máme pochopitelně za to, že barOptions.Images obsahuje kolekci velikostí Small, a barOptions.LargeImages velikost Large)
                    int smallIndex = _GetVectorImageIndex(resourceItems, args.CloneForSize(ResourceImageSizeType.Small));
                    int largeIndex = _GetVectorImageIndex(resourceItems, args.CloneForSize(ResourceImageSizeType.Large));
                    if (smallIndex >= 0 && largeIndex >= 0)
                    {   // Máme indexy pro obě velikosti?
                        if (barOptions.SvgImage != null) barOptions.SvgImage = null;
                        // Radši ne, jinde dochází k chybě, pokud SvgImage je null :    barOptions.SvgImageSize = Size.Empty;
                        if (barOptions.ImageIndex != smallIndex) barOptions.ImageIndex = smallIndex;
                        if (barOptions.LargeImageIndex != largeIndex) barOptions.LargeImageIndex = largeIndex;
                        hasIndexes = true;

                        if (args.PrepareDisabledImage || _NeedDisabledImage(args.Disabled, barOptions.Item))
                        {
                            var argsDis = args.CloneForDisabled(true);
                            barOptions.DisabledImageIndex = _GetVectorImageIndex(resourceItems, argsDis.CloneForSize(ResourceImageSizeType.Small)); ;
                            barOptions.DisabledLargeImageIndex = _GetVectorImageIndex(resourceItems, argsDis.CloneForSize(ResourceImageSizeType.Large));
                        }
                    }
                }
                if (!hasIndexes)
                {   // Nemáme-li indexy, vložíme SvgImage přímo (typicky: kolekce obrázků je Bitmapová a my vkládáme Vektor)
                    barOptions.SvgImage = _GetVectorImageApplication(resourceItems, args);
                    if (barOptions.SvgImage != null)
                        _ApplySvgImageSize(barOptions, args);
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {
                if (iciOptions.Image != null) iciOptions.Image = null;
                // Můžeme využívat Index?
                if (iciOptions.Images is SvgImageCollection svgCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku - ale právě v té velikosti, která je použita v svgCollection,
                    //  protože právě v té kolekci se bude pro zadaný index hledat reálná ikona!
                    var sizeType = DxSvgImageCollection.GetSizeType(svgCollection);
                    var argsSize = args.CloneForSize(sizeType);
                    try
                    {   // Tady dochází k chybě v DevExpress v situaci, kdy provádím Refresh obrázku. Nevím proč...
                        if (iciOptions.SvgImage != null) iciOptions.SvgImage = null;
                        int imageIndex = _GetVectorImageIndex(resourceItems, argsSize);            // Obrázek musím vygenerovat pro tu velikost, kterou obsahuje Collection!
                        if (iciOptions.ImageIndex != imageIndex) iciOptions.ImageIndex = imageIndex;
                    }
                    catch { }
                }
                else
                {   // Musíme tam dát přímo SvgImage:
                    iciOptions.SvgImage = _GetVectorImageApplication(resourceItems, args);
                    if (iciOptions.SvgImage != null)
                        _ApplySvgImageSize(iciOptions, args);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                if (imageOptions.Image != null) imageOptions.Image = null;
                imageOptions.SvgImage = _GetVectorImageApplication(resourceItems, args);
                if (imageOptions.SvgImage != null)
                    _ApplySvgImageSize(imageOptions, args);
            }
            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Vrátí velikost obrázku SvgImage pro <see cref="ImageOptions.SvgImageSize"/>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Size _GetVectorSvgImageSize(ResourceArgs args)
        {
            if (args.ImageSize.HasValue) return args.ImageSize.Value;
            if (args.SizeType.HasValue) return DxComponent.GetDefaultImageSize(args.SizeType.Value);
            return Size.Empty;
        }
        /// <summary>
        /// Aplikuje Image typu Bitmap ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="args"></param>
        private void _ApplyImageApplicationBmp(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            // Poznámka k parametru disabled: Zde řeším ikony typu Bitmap, jejich konverzi do Disabled stavu není třeba provádět zde,
            //   to si pro Bitmapy zajistí grafika DevExpress!
            imageOptions.Reset();
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Máme prostor pro dvě velikosti obrázku najednou:
                // barOptions.Reset();
                if (barOptions.Image != null) barOptions.Image = null;
                if (barOptions.LargeImage != null) barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is ImageList)
                {   // Máme připravenou podporu pro bitmapový index, můžeme tam dát dvě velikosti:
                    int smallIndex = _GetBitmapImageIndex(resourceItems, args.CloneForSize(ResourceImageSizeType.Small));
                    int largeIndex = _GetBitmapImageIndex(resourceItems, args.CloneForSize(ResourceImageSizeType.Large));
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
                {   // Nemám ImageList pro bitmapy (tedy ImageList je zjevně null nebo vektorový), pak do prvku můžu dát jen fyzický obrázek:
                    barOptions.Image = _GetBitmapImage(resourceItems, args.CloneForSize(ResourceImageSizeType.Small));
                    barOptions.LargeImage = _GetBitmapImage(resourceItems, args.CloneForSize(ResourceImageSizeType.Large));
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                if (iciOptions.Images is ImageList)
                {   // Máme připravenou podporu pro bitmapový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                    try
                    {   // Tady dochází k chybě v DevExpress v situaci, kdy provádím Refresh obrázku. Nevím proč...
                        if (iciOptions.SvgImage != null) iciOptions.SvgImage = null;
                        int imageIndex = _GetBitmapImageIndex(resourceItems, args);
                        if (iciOptions.ImageIndex != imageIndex)
                            iciOptions.ImageIndex = imageIndex;
                    }
                    catch { }
                }
                else
                {   // Nemám ImageList pro bitmapy (tedy ImageList je zjevně null nebo vektorový), pak do prvku můžu dát jen fyzický obrázek:
                    iciOptions.Image = _GetBitmapImage(resourceItems, args);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                imageOptions.Image = _GetBitmapImage(resourceItems, args);
                imageOptions.SvgImage = null;
            }
            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Aplikuje Image typu Icon ze zdroje Aplikační do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="resourceItems"></param>
        /// <param name="args"></param>
        private void _ApplyImageApplicationIco(ImageOptions imageOptions, DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _ConvertIconToBitmap(resourceItems, args);
        }
        /// <summary>
        /// Aplikuje Image typu Vector nebo Bitmap (podle přípony) ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageDevExpress(ImageOptions imageOptions, ResourceArgs args)
        {
            string extension = _GetDevExpressResourceExtension(args.ImageName);
            if (extension != null)
            {
                if (extension == "svg")
                    _ApplyImageDevExpressSvg(imageOptions, args);
                else
                    _ApplyImageDevExpressBmp(imageOptions, args);
            }
        }
        /// <summary>
        /// Aplikuje Image typu Vector ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageDevExpressSvg(ImageOptions imageOptions, ResourceArgs args)
        {
            imageOptions.Image = null;
            imageOptions.SvgImage = _GetVectorImageDevExpress(args);             // Na vstupu je jméno Vektoru, dáme jej tedy do SvgImage
            _ApplySvgImageSize(imageOptions, args);
        }
        /// <summary>
        /// Aplikuje Image typu Bitmap ze zdroje DevExpress do daného cíle <paramref name="imageOptions"/>.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageDevExpressBmp(ImageOptions imageOptions, ResourceArgs args)
        {
            imageOptions.SvgImage = null;
            imageOptions.Image = _CreateBitmapImageDevExpressPng(args);             // Na vstupu je jméno bitmapy, tedy ji najdeme a dáme do Image. Tady nepřichází do úvahy renderování, velikost, paleta atd...
        }
        /// <summary>
        /// Do daného objektu vloží náhradní ikonu pro daný text
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageForCaption(ImageOptions imageOptions, ResourceArgs args)
        {
            args = args.CloneForOnlyCaption();
            // _ApplyImageForCaptionSvg(imageOptions, caption, sizeType, imageSize);
            // _ApplyImageForCaptionBmp(imageOptions, caption, sizeType, imageSize);
            _ApplyImageForCaptionMix(imageOptions, args);
        }
        /// <summary>
        /// Do daného objektu vloží náhradní ikonu pro daný text - jako SVG.
        /// Podle JD je malá ikona příliš rozmazaná.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageForCaptionSvg(ImageOptions imageOptions, ResourceArgs args)
        {
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Má prostor pro dvě velikosti obrázku najednou:
                barOptions.Image = null;
                barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    int smallIndex = _GetCaptionVectorIndex(args.CloneForSize(ResourceImageSizeType.Small));
                    int largeIndex = _GetCaptionVectorIndex(args.CloneForSize(ResourceImageSizeType.Large));
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
                    barOptions.SvgImage = _CreateCaptionVector(args);
                    barOptions.SvgImageSize = _GetVectorSvgImageSize(args);
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                iciOptions.Image = null;
                if (iciOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                    iciOptions.SvgImage = null;
                    iciOptions.SvgImageSize = Size.Empty;
                    iciOptions.ImageIndex = _GetCaptionVectorIndex(args);
                }
                else
                {   // Musíme tam dát přímo SvgImage:
                    iciOptions.SvgImage = _CreateCaptionVector(args);
                    iciOptions.SvgImageSize = _GetVectorSvgImageSize(args);
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                imageOptions.Image = null;
                imageOptions.SvgImage = _CreateCaptionVector(args);
                imageOptions.SvgImageSize = _GetVectorSvgImageSize(args);
            }

            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Do daného objektu vloží náhradní ikonu pro daný text - jako BMP.
        /// Podle JD je velká ikona příliš tlustá.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageForCaptionBmp(ImageOptions imageOptions, ResourceArgs args)
        {
            imageOptions.Reset();
            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Má prostor pro dvě velikosti obrázku najednou:
                barOptions.Image = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Small));
                barOptions.LargeImage = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Large));
            }
            else
            {   // Jen jeden obrázek:
                imageOptions.Image = _GetBitmapImage(args);
            }
        }
        /// <summary>
        /// Do daného objektu vloží náhradní ikonu pro daný text - velkou jako SVG a malou jako BMP.
        /// JD tvrdí, že to je nejlepší...
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplyImageForCaptionMix(ImageOptions imageOptions, ResourceArgs args)
        {
            ResourceImageSizeType sizeT = args.CurrentSizeType;

            if (imageOptions is DevExpress.XtraBars.BarItemImageOptions barOptions)
            {   // Má prostor pro dvě velikosti obrázku najednou:
                barOptions.Image = null;
                barOptions.LargeImage = null;

                bool hasIndexes = false;
                if (barOptions.Images is SvgImageCollection)
                {   // Máme připravenou podporu pro vektorový index, můžeme tam dát dvě velikosti:
                    int largeIndex = _GetCaptionVectorIndex(args.CloneForSize(ResourceImageSizeType.Large));
                    if (largeIndex >= 0)
                    {   // Máme index pro Large = SVG:
                        barOptions.SvgImage = null;
                        barOptions.SvgImageSize = Size.Empty;
                        barOptions.LargeImageIndex = largeIndex;
                        // Malý = BMP:
                        barOptions.Image = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Small));
                        hasIndexes = true;
                    }
                }
                if (!hasIndexes)
                {
                    if (sizeT == ResourceImageSizeType.Large)
                    {   // Velký = SVG:
                        barOptions.SvgImage = _CreateCaptionVector(args.CloneForSize(ResourceImageSizeType.Large));
                        _ApplySvgImageSize(barOptions, args);
                    }
                    else
                    {   // Malý = BMP:
                        barOptions.Image = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Small));
                    }
                }
            }
            else if (imageOptions is DevExpress.Utils.ImageCollectionImageOptions iciOptions)
            {   // Může využívat Index:
                iciOptions.Image = null;
                if (sizeT == ResourceImageSizeType.Large)
                {   // Velký = SVG:
                    if (iciOptions.Images is SvgImageCollection)
                    {   // Máme připravenou podporu pro vektorový index, můžeme tam dát index prvku v požadované velikosti (defalt = velká):
                        iciOptions.SvgImage = null;
                        iciOptions.SvgImageSize = Size.Empty;
                        iciOptions.ImageIndex = _GetCaptionVectorIndex(args.CloneForSize(ResourceImageSizeType.Large));
                    }
                    else
                    {   // Musíme tam dát přímo SvgImage:
                        iciOptions.SvgImage = _CreateCaptionVector(args.CloneForSize(ResourceImageSizeType.Large));
                        _ApplySvgImageSize(iciOptions, args);
                    }
                }
                else
                {   // Malý = BMP:
                    if (iciOptions.Images is ImageList)
                    {   // Máme připravenou podporu pro BMP index = dáme index:
                        iciOptions.ImageIndex = _GetBitmapImageIndex(args.CloneForSize(ResourceImageSizeType.Small));
                    }
                    else
                    {   // Dáme Image, ale z interního ImageListu (GetImage versus CreateImage):
                        iciOptions.Image = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Small));
                    }
                }
            }
            else
            {   // Musíme vepsat přímo jeden obrázek:
                if (sizeT == ResourceImageSizeType.Large)
                {   // Velký = SVG:
                    imageOptions.Image = null;
                    imageOptions.SvgImage = _CreateCaptionVector(args.CloneForSize(ResourceImageSizeType.Large));
                    _ApplySvgImageSize(imageOptions, args);
                }
                else
                {   // Malý = BMP:
                    imageOptions.SvgImage = null;
                    imageOptions.Image = _GetBitmapImage(args.CloneForSize(ResourceImageSizeType.Small));
                }
            }

            imageOptions.SvgImageColorizationMode = SvgImageColorizationMode.None;
        }
        /// <summary>
        /// Do daného <see cref="ImageOptions"/> vloží odpovídající velikost <see cref="ImageOptions.SvgImageSize"/>, 
        /// pokud v <paramref name="args"/> jsou nějaké podklady.
        /// </summary>
        /// <param name="imageOptions"></param>
        /// <param name="args"></param>
        private void _ApplySvgImageSize(ImageOptions imageOptions, ResourceArgs args)
        {
            if (args.ImageSize.HasValue) imageOptions.SvgImageSize = args.ImageSize.Value;
            else if (args.SizeType.HasValue) imageOptions.SvgImageSize = DxComponent.GetDefaultImageSize(args.SizeType.Value);
        }
        /// <summary>
        /// Vrátí true, pokud prvek má mít ikonku typu Disabled.
        /// </summary>
        /// <param name="disabled"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static bool _NeedDisabledImage(bool? disabled, DevExpress.XtraBars.BarItem item = null)
        {
            if (disabled.HasValue) return disabled.Value;            // Je dána hodnota disabled? Vrátíme její hodnotu (vracíme = Disabled)
            if (item != null) return !item.Enabled;                  // Je dán prvek BarItem? Vrátíme true, když prvek má Enabled = false 
            return false;                                            // Nepotřebujeme ikonku Disabled
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
        public static void ApplyIcon(Form form, string iconName, ResourceImageSizeType? sizeType = null, bool forceToIcon = false) 
        { Instance._ApplyIcon(form, new ResourceArgs(iconName, false, sizeType, forceToIcon: forceToIcon)); }
        /// <summary>
        /// Do daného okna aplikuje danou ikonu.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="args"></param>
        private void _ApplyIcon(Form form, ResourceArgs args)
        {
            if (form is null || !args.HasImageName) return;

            if (!args.ForceToIcon && form is DevExpress.XtraEditors.XtraForm xtraForm)
                _ApplyImage(xtraForm.IconOptions, args);
            else
                _ApplyIconToForm(form, _CreateIconImage(args));
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
        /// Související metodou je <see cref="GetPreferredImageIndex(string, ResourceImageSizeType, Size?, string, bool, bool?)"/>.
        /// Ta metoda vrátí index z ImageList preferovaného typu (je to po celou dobu běhu aplikace shodný typ: Bitmap nebo Vector).
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static object GetPreferredImageList(ResourceImageSizeType sizeType) { return Instance._GetPreferredImageList(new ResourceArgs(sizeType: sizeType)); }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private object _GetPreferredImageList(ResourceArgs args)
        {
            if (this._IsPreferredVectorImage)
                return this._GetVectorImageList(args);
            else
                return this._GetBitmapImageList(args);
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
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <returns></returns>
        public static int GetPreferredImageIndex(string imageName, ResourceImageSizeType sizeType, Size? optimalSvgSize = null, string caption = null, bool exactName = false, bool? disabled = null)
        { return Instance._GetPreferredImageIndex(new ResourceArgs(imageName, exactName, sizeType, optimalSvgSize, caption: caption, disabled: disabled)); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán v <paramref name="args"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetPreferredImageIndex(ResourceArgs args)
        {
            if (this._IsPreferredVectorImage)
                return this._GetVectorImageIndex(args);
            else
                return this._GetBitmapImageIndex(args);
        }
        /// <summary>
        /// Připraví ImageListy bitmapové i vektorové.
        /// Je vhodné volat co nejdříve, aby se odpovídající objekty zaregistrovaly mezi prvními mezi ostatní Listenery.
        /// </summary>
        private void _PrepareImageLists()
        {
            _GetDxBitmapImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Small));
            _GetDxBitmapImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Medium));
            _GetDxBitmapImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Large));

            _GetVectorImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Small));
            _GetVectorImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Medium));
            _GetVectorImageList(new ResourceArgs(sizeType: ResourceImageSizeType.Large));
        }
        #endregion
        #region BitmapImageList - Seznam obrázků typu Bitmapa, pro použití v controlech; GetBitmapImageList, GetBitmapImageIndex, GetBitmapImage
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        public static ImageList GetBitmapImageList(ResourceImageSizeType sizeType) { return Instance._GetBitmapImageList(new ResourceArgs(sizeType: sizeType)); }
        /// <summary>
        /// Vrací objekt <see cref="ImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private ImageList _GetBitmapImageList(ResourceArgs args)
        {
            return _GetDxBitmapImageList(args).ImageList;
        }
        /// <summary>
        /// Vrací objekt <see cref="DxBmpImageList"/>, obsahující bitmapové obrázky dané velikosti
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private DxBmpImageList _GetDxBitmapImageList(ResourceArgs args)
        {
            if (__BitmapImageList == null) __BitmapImageList = new Dictionary<ResourceImageSizeType, DxBmpImageList>();   // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var imageListDict = __BitmapImageList;
            var sizeType = args.CurrentSizeType;
            if (!imageListDict.TryGetValue(sizeType, out DxBmpImageList dxBmpImageList))
            {
                lock (imageListDict)
                {
                    if (!imageListDict.TryGetValue(sizeType, out dxBmpImageList))
                    {
                        dxBmpImageList = new DxBmpImageList(sizeType);
                        dxBmpImageList.ImageSize = _GetVectorSvgImageSize(args);
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
        { return Instance._GetBitmapImageIndex(new ResourceArgs(imageName, exactName, sizeType, optimalSvgSize, caption: caption)); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <see cref="ResourceArgs.Caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetBitmapImageIndex(ResourceArgs args)
        {
            var imageInfo = _GetBitmapImageListItem(args);
            return imageInfo?.Item2 ?? -1;
        }
        /// <summary>
        /// Najde a vrátí index ID pro vhodný bitmapový obrázek z dodaných zdrojů, pro danou velikost.
        /// Akceptuje typy zdrojů <see cref="ResourceContentType.Bitmap"/>, <see cref="ResourceContentType.Vector"/> i <see cref="ResourceContentType.Icon"/>.
        /// Pokud daný Resource ještě není uložen v odpovídajícím ImageListu (pro danou velikost), pak je Image vytvořen a do ImageListu přidán.
        /// </summary>
        /// <param name="resourceItems">Dodané zdroje různých velikostí</param>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetBitmapImageIndex(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, args.SizeType, out var resourceItem)) return -1;
            var dxImageList = _GetDxBitmapImageList(args);
            return dxImageList.GetImageId(resourceItem.ItemKey, n => _CreateBitmapImageApplication(resourceItem, args));
        }
        /// <summary>
        /// Najde a vrátí Image z dodaných zdrojů, pro danou velikost.
        /// Akceptuje typy zdrojů <see cref="ResourceContentType.Bitmap"/>, <see cref="ResourceContentType.Vector"/> i <see cref="ResourceContentType.Icon"/>.
        /// Pokud daný Resource ještě není uložen v odpovídajícím ImageListu (pro danou velikost), pak je Image vytvořen a do ImageListu přidán.
        /// </summary>
        /// <param name="resourceItems">Dodané zdroje různých velikostí</param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _GetBitmapImage(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, args.SizeType, out var resourceItem)) return null;
            var dxImageList = _GetDxBitmapImageList(args);
            return dxImageList.GetImage(resourceItem.ItemKey, n => _CreateBitmapImageApplication(resourceItem, args));
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
        { return Instance._GetBitmapImage(new ResourceArgs(imageName, exactName, sizeType, optimalSvgSize, caption: caption)); }
        /// <summary>
        /// Vrátí index pro daný obrázek do odpovídajícího ImageListu.
        /// Pokud není k dispozici požadovaný obrázek, a je dodán <see cref="ResourceArgs.Caption"/>, je vygenerován náhradní obrázek v požadované velikosti.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _GetBitmapImage(ResourceArgs args)
        {
            var imageInfo = _GetBitmapImageListItem(args);
            if (imageInfo == null || imageInfo.Item1 == null || imageInfo.Item2 < 0) return null;
            if (args.CurrentSizeType == ResourceImageSizeType.Original && imageInfo.Item3.ContainsOriginalImages && imageInfo.Item3.OriginalImageDict.TryGetValue(imageInfo.Item4, out var foundImage))
                return foundImage;
            return imageInfo.Item1.Images[imageInfo.Item2];
        }
        /// <summary>
        /// Metoda vyhledá, zda daný obrázek existuje
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Tuple<ImageList, int, DxBmpImageList, string> _GetBitmapImageListItem(ResourceArgs args)
        {
            bool hasName = args.HasImageName;
            string captionKey = DxComponent.GetCaptionForIcon(args.Caption);
            bool hasCaption = (captionKey.Length > 0);
            string key = (hasName ? args.ImageName.ToLower() : hasCaption ? $"«:{captionKey}:»" : "").Trim();
            if (key.Length == 0) return null;

            var dxImageList = _GetDxBitmapImageList(args);
            int index = -1;
            if (dxImageList.ContainsKey(key))
            {
                index = dxImageList.IndexOfKey(key);
            }
            else if (hasName || hasCaption)
            {
                Image image = _CreateBitmapImage(args);
                if (image != null)
                {
                    bool isColorized = !hasName && hasCaption;       // Image lze přebarvovat tehdy, když nepochází z obrázku, ale z Caption
                    dxImageList.Add(key, image, isColorized, args.ImageName, args.ExactName, args.OptimalSvgSize, args.Caption);
                    index = dxImageList.IndexOfKey(key);
                }
            }
            return (index >= 0 ? new Tuple<ImageList, int, DxBmpImageList, string>(dxImageList.ImageList, index, dxImageList, key) : null);
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
        public static SvgImageCollection GetVectorImageList(ResourceImageSizeType sizeType) { return Instance._GetVectorImageList(new ResourceArgs(sizeType: sizeType)); }
        /// <summary>
        /// Vrátí kolekci SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje. Pro danou cílovou velikost.
        /// </summary>
        /// <param name="args"></param>
        private DxSvgImageCollection _GetVectorImageList(ResourceArgs args)
        {
            if (__VectorImageList == null) __VectorImageList = new Dictionary<ResourceImageSizeType, DxSvgImageCollection>();      // OnDemand tvorba, grafika se používá výhradně z GUI threadu takže tady zámek neřeším
            var svgImageCollections = __VectorImageList;
            var sizeType = args.CurrentSizeType;
            if (!svgImageCollections.TryGetValue(sizeType, out DxSvgImageCollection svgCollection))
            {
                lock (svgImageCollections)
                {
                    if (!svgImageCollections.TryGetValue(sizeType, out svgCollection))
                    {
                        svgCollection = new DxSvgImageCollection(sizeType);
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
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <returns></returns>
        public static int GetVectorImageIndex(string imageName, ResourceImageSizeType sizeType, bool exactName = false, string caption = null, bool? disabled = null) 
        { return Instance._GetVectorImageIndex(new ResourceArgs(imageName, exactName, sizeType, caption: caption, disabled: disabled)); }
        /// <summary>
        /// Najde a vrátí index ID pro vektorový obrázek daného jména, obrázek je uložen v kolekci <see cref="SvgImageCollection"/>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetVectorImageIndex(ResourceArgs args)
        {
            var svgCollection = _GetVectorImageList(args);
            return svgCollection.GetImageId(args.ImageName, n => _GetVectorImage(args));
        }
        /// <summary>
        /// Najde a vrátí index ID pro vhodný vektorový obrázek z dodaných zdrojů, pro danou velikost.
        /// </summary>
        /// <param name="resourceItems">Dodané zdroje různých velikostí</param>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetVectorImageIndex(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, args.SizeType, out var resourceItem)) return -1;
            var svgCollection = _GetVectorImageList(args);
            DxSvgImagePaletteType palette = GetImagePalette(DxComponent.IsDarkTheme, args.Disabled);
            string key = CreateVectorImageKey(resourceItem.ItemKey, palette);
            return svgCollection.GetImageId(key, n => resourceItem.CreateSvgImage(palette));
        }
        /// <summary>
        /// Najde a vrátí index ID pro dodaný vektorový obrázek z ImageArray.
        /// </summary>
        /// <param name="svgImageArray">Kombinovaná ikona</param>
        /// <param name="args">Informace o obrázku, obsahuje cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
        /// <returns></returns>
        private int _GetVectorImageIndex(SvgImageArrayInfo svgImageArray, ResourceArgs args)
        {
            if (svgImageArray is null) return -1;
            var svgCollection = _GetVectorImageList(args);
            string key = svgImageArray.Key;
            return svgCollection.GetImageId(key, n => _GetVectorImageArray(svgImageArray, args));
        }

        /// <summary>
        /// Metoda vrátí typ palety pro daný odstín a Disabled.
        /// </summary>
        /// <param name="darkSkin">Převést barvy na tmavý skin</param>
        /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
        /// <returns></returns>
        internal static DxSvgImagePaletteType GetImagePalette(bool darkSkin, bool? disabled)
        {
            bool isDisabled = (disabled ?? false);
            DxSvgImagePaletteType palette =
                (!darkSkin ? (!isDisabled ? DxSvgImagePaletteType.LightSkin : DxSvgImagePaletteType.LightSkinDisabled)
                           : (!isDisabled ? DxSvgImagePaletteType.DarkSkin : DxSvgImagePaletteType.DarkSkinDisabled));
            return palette;
        }
        /// <summary>
        /// Metoda vrátí klíč vektorového obrázku, do něhož je zakomponován tmavý skin a příznak Disabled.
        /// </summary>
        /// <param name="itemKey"></param>
        /// <param name="darkSkin"></param>
        /// <param name="isDisabled"></param>
        /// <returns></returns>
        internal static string CreateVectorImageKey(string itemKey, bool darkSkin, bool isDisabled)
        {
            string prefix = (!darkSkin ? (!isDisabled ? _KeyPrefixLightSkin : _KeyPrefixLightSkinDisabled)
                                       : (!isDisabled ? _KeyPrefixDarkSkin : _KeyPrefixDarkSkinDisabled));
            return prefix + itemKey;
        }
        /// <summary>
        /// Metoda vrátí klíč vektorového obrázku, do něhož je zakomponován typ palety.
        /// </summary>
        /// <param name="itemKey"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        internal static string CreateVectorImageKey(string itemKey, DxSvgImagePaletteType palette)
        {
            string prefix = (palette == DxSvgImagePaletteType.LightSkin ? _KeyPrefixLightSkin :
                            (palette == DxSvgImagePaletteType.LightSkinDisabled ? _KeyPrefixLightSkinDisabled :
                            (palette == DxSvgImagePaletteType.DarkSkin ? _KeyPrefixDarkSkin :
                            (palette == DxSvgImagePaletteType.DarkSkinDisabled ? _KeyPrefixDarkSkinDisabled : _KeyPrefixExplicit))));
            return prefix + itemKey;
        }
        private const string _KeyPrefixLightSkin = "G";
        private const string _KeyPrefixLightSkinDisabled = "W";
        private const string _KeyPrefixDarkSkin = "E";
        private const string _KeyPrefixDarkSkinDisabled = "D";
        private const string _KeyPrefixExplicit = "X";

        /// <summary>Kolekce SvgImages pro použití v controlech, obsahuje DevExpress i Aplikační zdroje, instanční proměnná.</summary>
        private Dictionary<ResourceImageSizeType, DxSvgImageCollection> __VectorImageList;
        #endregion
        #region ImageArray : více vektorových ikon v jednom názvu Image
        /// <summary>
        /// Vytvoří a vrátí bitmapu z pole vektorových image
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageArray(SvgImageArrayInfo svgImageArray, ResourceArgs args)
        {
            var svgImage = SvgImageSupport.CreateSvgImage(svgImageArray, args.SizeType);
            return _ConvertVectorToImage(svgImage, args);
        }
        /// <summary>
        /// Vyrenderuje dané pole vektorových image do bitmapy a z ní vytvoří a vrátí Icon
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Icon _ConvertImageArrayToIcon(SvgImageArrayInfo svgImageArray, ResourceArgs args)
        {
            var svgImage = SvgImageSupport.CreateSvgImage(svgImageArray, args.SizeType);
            using (var bitmap = _ConvertVectorToImage(svgImage, args))
                return _ConvertBitmapToIcon(bitmap);
        }
        /// <summary>
        /// Vrátí SVG Image typu Array
        /// </summary>
        /// <param name="svgImageArray"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private DevExpress.Utils.Svg.SvgImage _GetVectorImageArray(SvgImageArrayInfo svgImageArray, ResourceArgs args)
        {
            return SvgImageSupport.CreateSvgImage(svgImageArray, args.SizeType);
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
        /// <param name="args"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private bool _TryGetContentTypeApplication(DxApplicationResourceLibrary.ResourceItem[] validItems, ResourceArgs args, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            bool isPreferredVector = args.IsPreferredVectorImage ?? this._IsPreferredVectorImage;
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
        /// - pouze vektorové a/nebo bitmapové obrázky (<see cref="ResourceContentType.BasicImage"/>),
        /// s prioritou podle <see cref="DxComponent.IsPreferredVectorImage"/>.
        /// Velikost se řeší následně.
        /// <para/>
        /// Pokud tedy systém preferuje vektorové obrázky, pak primárně hledá vektorové, a teprve když je nenajde, tak hledá bitmapové.
        /// A naopak, pokud systém preferuje bitmapy, pak se zde prioritně hledají bitmapy, a až v druhé řadě se akceptují vektory.
        /// <para/>
        /// Pokud vrátí true, pak v poli <paramref name="validItems"/> je nejméně jeden prvek.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="validItems"></param>
        /// <returns></returns>
        private bool _TryGetApplicationResources(ResourceArgs args, out DxApplicationResourceLibrary.ResourceItem[] validItems)
        {
            return __TryGetApplicationResources(args, ResourceContentType.BasicImage, out validItems, out var _);
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
        /// <param name="args"></param>
        /// <param name="contentTypes"></param>
        /// <param name="validItems"></param>
        /// <returns></returns>
        private bool _TryGetApplicationResources(ResourceArgs args, ResourceContentType contentTypes, out DxApplicationResourceLibrary.ResourceItem[] validItems)
        {
            return __TryGetApplicationResources(args, contentTypes, out validItems, out var _);
        }
        /// <summary>
        /// Na vstupu dostává 
        /// Zkusí najít daný zdroj v aplikačních zdrojích, zafiltruje na daný typ obsahu - typ obsahu je povinný. Velikost se řeší následně.
        /// <para/>
        /// Na vstupu je dán požadovaný typ obrázků. Pokud jsou zadány hodnoty <see cref="ResourceContentType.Bitmap"/> a <see cref="ResourceContentType.Vector"/>,
        /// pak preferenci mezi typy řeší tato metoda podle hodnoty <see cref="DxComponent.IsPreferredVectorImage"/>.
        /// <para/>
        /// Pokud není zadán žádný typ zdroje, akceptuje jakýkoli nalezený typ.
        /// Pokud na vstupu v poli <paramref name="validItems"/> je jen jeden prvek, pak jej vrátí jen tehdy, pokud jeho konkrétní typ obsahu vyhovuje danému <paramref name="contentTypes"/>.
        /// <para/>
        /// Pokud vrátí true, pak v poli <paramref name="validItems"/> je nejméně jeden prvek.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="contentTypes"></param>
        /// <param name="validItems"></param>
        /// <param name="validType"></param>
        /// <returns></returns>
        private bool _TryGetApplicationResources(ResourceArgs args, ResourceContentType contentTypes, out DxApplicationResourceLibrary.ResourceItem[] validItems, out ResourceContentType validType)
        {
            return __TryGetApplicationResources(args, contentTypes, out validItems, out validType);
        }
        /// <summary>
        /// Obsahuje platné typy obsahu pro obrázky, podle aktuální preference vektorů <see cref="IsPreferredVectorImage"/>.
        /// </summary>
        private ResourceContentType _ValidImageTypes { get { return (_IsPreferredVectorImage ? ResourceContentType.Vector | ResourceContentType.Bitmap : ResourceContentType.Bitmap); } }
        /// <summary>
        /// Na vstupu dostává 
        /// Zkusí najít daný zdroj v aplikačních zdrojích, zafiltruje na daný typ obsahu - typ obsahu je povinný. Velikost se řeší následně.
        /// <para/>
        /// Na vstupu je dán požadovaný typ obrázků. Pokud jsou zadány hodnoty <see cref="ResourceContentType.Bitmap"/> a <see cref="ResourceContentType.Vector"/>,
        /// pak preferenci mezi typy řeší tato metoda podle hodnoty <see cref="DxComponent.IsPreferredVectorImage"/>.
        /// <para/>
        /// Pokud není zadán žádný typ zdroje, akceptuje jakýkoli nalezený typ.
        /// Pokud na vstupu v poli <paramref name="validItems"/> je jen jeden prvek, pak jej vrátí jen tehdy, pokud jeho konkrétní typ obsahu vyhovuje danému <paramref name="contentTypes"/>.
        /// <para/>
        /// Pokud vrátí true, pak v poli <paramref name="validItems"/> je nejméně jeden prvek.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="contentTypes"></param>
        /// <param name="validItems"></param>
        /// <param name="validType"></param>
        /// <returns></returns>
        private bool __TryGetApplicationResources(ResourceArgs args, ResourceContentType contentTypes, out DxApplicationResourceLibrary.ResourceItem[] validItems, out ResourceContentType validType)
        {
            validItems = null;
            validType = ResourceContentType.None;
            if (!args.HasImageName || contentTypes == ResourceContentType.None) return false;
            if (!DxApplicationResourceLibrary.TryGetResource(args.ImageName, args.ExactName, out var resourceItem, out var resourcePack)) return false;

            // Pokud jsem našel jeden konkrétní zdroj (je zadané explicitní jméno, a to bylo nalezeno):
            if (resourceItem != null)
            {   // Pak zjistím, zda nalezený zdroj vyhovuje požadavku na typ obsahu:
                if ((resourceItem.ContentType & contentTypes) != 0)
                {
                    validItems = new DxApplicationResourceLibrary.ResourceItem[] { resourceItem };
                    validType = resourceItem.ContentType;
                    return true;
                }
                // Pro dané jméno jsem našel právě jen jeden prvek, ale není to ten typ, který byl požadován:
                return false;
            }

            // Pokud jsem nenašel právě jen jeden prvek, ale našel jsem celý ResourcePack,
            //   pak z něj vyberu jen ty zdroje, které odpovídají požadovanému typu obsahu:
            if (resourcePack != null)
            {
                bool isPreferredVector = args.IsPreferredVectorImage ?? _IsPreferredVectorImage;

                // Postupně hledáme zdroje určitého typu:
                if ((contentTypes & ResourceContentType.StandardImage) != 0)
                {
                    // a) pokud preferujeme vektory:
                    if (isPreferredVector && _TryGetResourcesOfContentType(contentTypes, ResourceContentType.Vector, resourcePack, ref validItems, ref validType)) return true;

                    // b) bitmapy v pořadí čitelnosti:
                    if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.Bitmap, resourcePack, ref validItems, ref validType)) return true;
                    if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.Icon, resourcePack, ref validItems, ref validType)) return true;

                    // c) pokud nepreferujeme vektory, ale přitom jsou přípustné, tak až po bitmapách:
                    if (!isPreferredVector && _TryGetResourcesOfContentType(contentTypes, ResourceContentType.Vector, resourcePack, ref validItems, ref validType)) return true;

                    return false;
                }

                // d) následují typy, které nejsou řazeny preferencemi:
                if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.Cursor, resourcePack, ref validItems, ref validType)) return true;
                if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.AnyMultimedia, resourcePack, ref validItems, ref validType)) return true;
                if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.AnyText, resourcePack, ref validItems, ref validType)) return true;

                if (_TryGetResourcesOfContentType(contentTypes, ResourceContentType.All, resourcePack, ref validItems, ref validType)) return true;
            }
            return false;
        }
        /// <summary>
        /// Metoda zkusí najít prvky daného typu <paramref name="testTypes"/>, pokud jsou požadovány v <paramref name="contentTypes"/> a pokud jsou přítomny v <paramref name="resourcePack"/>.
        /// </summary>
        /// <param name="contentTypes"></param>
        /// <param name="testTypes">Jeden nebo více typů obsahu. Pokud bude zadáno více typů, akceptuje se kterýkoli z nich.</param>
        /// <param name="resourcePack"></param>
        /// <param name="validItems"></param>
        /// <param name="validType"></param>
        /// <returns></returns>
        private static bool _TryGetResourcesOfContentType(ResourceContentType contentTypes, ResourceContentType testTypes, 
            DxApplicationResourceLibrary.ResourcePack resourcePack,
            ref DxApplicationResourceLibrary.ResourceItem[] validItems, ref ResourceContentType validType)
        {
            // if (((contentTypes & testTypes) != 0) && ((resourcePack.ResourceTypes & testTypes) != 0))
            if ((contentTypes & resourcePack.ResourceTypes & testTypes) != 0)
            {   // Pokud požadujeme testovaný typ obsahu, a pokud je v ResourcePack přítomen:
                // Získáme prvky daného typu (měly by tam být, když to tvrdí resourcePack.ResourceTypes):
                validItems = resourcePack.ResourceItems.Where(i => ((i.ContentType & testTypes) != 0)).ToArray();
                if (validItems.Length > 0)
                {   // A vrátíme true:
                    validType = validItems[0].ContentType;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů.
        /// Na vstupu (<paramref name="args"/> : <see cref="ResourceArgs.ImageName"/>) nemusí být uvedena přípona, může být uvedeno obecné jméno, např. "pic\address-book-undo-2";
        /// a až knihovna zdrojů sama najde reálné obrázky: "pic\address-book-undo-2-large.svg" anebo "pic\address-book-undo-2-small.svg".
        /// </summary>
        /// <returns></returns>
        private Image _CreateImageApplication(ResourceArgs args)
        {
            if (!_TryGetApplicationResources(args, ResourceContentType.StandardImage, out var validItems)) return null;
            return _CreateBitmapImageApplication(validItems, args);
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů.
        /// Na vstupu (<paramref name="resourceItems"/>) je seznam zdrojů, z nich bude vybrán zdroj vhodné velikosti.
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, args.SizeType, out var resourceItem))
                return _CreateBitmapImageApplication(resourceItem, args);
            return null;
        }
        /// <summary>
        /// Vrátí Image z knihovny zdrojů pro daný konkrétní zdroj. Akceptuje typy zdrojů <see cref="ResourceContentType.Bitmap"/>,
        /// <see cref="ResourceContentType.Vector"/> i <see cref="ResourceContentType.Icon"/>.
        /// </summary>
        /// <param name="resourceItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageApplication(DxApplicationResourceLibrary.ResourceItem resourceItem, ResourceArgs args)
        {
            if (resourceItem != null)
            {
                switch (resourceItem.ContentType)
                {
                    case ResourceContentType.Vector:
                        return _ConvertVectorToImage(resourceItem.CreateSvgImage(GetImagePalette(DxComponent.IsDarkTheme, args.Disabled)), args);
                    case ResourceContentType.Bitmap:
                        return resourceItem.CreateBmpImage();
                    case ResourceContentType.Icon:
                        return _ConvertIconToBitmap(resourceItem, args);
                }
            }
            return null;
        }
        /// <summary>
        /// Najde a rychle vrátí <see cref="SvgImage"/> pro dané jméno, hledá v dodaných Aplikačních zdrojích
        /// </summary>
        /// <param name="resourceItems"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImageApplication(DxApplicationResourceLibrary.ResourceItem[] resourceItems, ResourceArgs args)
        {
            if (!DxApplicationResourceLibrary.ResourcePack.TryGetOptimalSize(resourceItems, (args.SizeType ?? ResourceImageSizeType.Large), out var resourceItem))
                return null;
            return resourceItem.CreateSvgImage(GetImagePalette(DxComponent.IsDarkTheme, args.Disabled));
        }
        /// <summary>
        /// Konveruje (renderuje) dodaný aplikační vektorový obrázek na Bitmapu = Image
        /// </summary>
        /// <param name="vectorItem"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _ConvertApplicationVectorToImage(DxApplicationResourceLibrary.ResourceItem vectorItem, ResourceArgs args)
        {
            var svgImage = vectorItem.CreateSvgImage(GetImagePalette(DxComponent.IsDarkTheme, args.Disabled));
            return _ConvertVectorToImage(svgImage, args);
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
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpress(ResourceArgs args)
        {
            if (_IsImageNameSvg(args.ImageName))
                return _CreateBitmapImageDevExpressSvg(args);
            else
                return _CreateBitmapImageDevExpressPng(args);
        }
        /// <summary>
        /// Vrátí bitmapu z obrázku typu SVG uloženou v DevExpress zdrojích.
        /// Tato metoda již netestuje existenci zdroje, to má provést někdo před voláním této metody.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpressSvg(ResourceArgs args)
        {
            string resourceName = _GetDevExpressResourceKey(args.ImageName);
            Size size = args.OptimalSvgSize ?? GetDefaultImageSize(args.SizeType);

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
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateBitmapImageDevExpressPng(ResourceArgs args)
        {
            string resourceName = _GetDevExpressResourceKey(args.ImageName);
            return _DevExpressResourceCache.GetImage(resourceName).Clone() as Image;
        }
        /// <summary>
        /// Najde a vrátí <see cref="SvgImage"/> pro dané jméno, hledá v DevExpress zdrojích
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private SvgImage _GetVectorImageDevExpress(ResourceArgs args)
        {
            string resourceName = _GetDevExpressResourceKey(args.ImageName);
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
        { return Instance._CreateCaptionVector(new ResourceArgs(sizeType: (sizeType ?? ResourceImageSizeType.Large), caption: caption)); }
        /// <summary>
        /// Vytvoří <see cref="SvgImage"/> pro daný text, namísto chybějící ikony.
        /// Pokud vrátí null, zkusí se provést <see cref="CreateCaptionImage(string, ResourceImageSizeType?, Size?)"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private SvgImage _CreateCaptionVector(ResourceArgs args)
        {
            return DxSvgImage.CreateCaptionVector(args.Caption, (args.SizeType ?? ResourceImageSizeType.Large));
        }
        /// <summary>
        /// Najde a vrátí index pro SVG image pro daný text Caption. Nebo SVG image vytvoří, uloží a vrátí jeho index.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private int _GetCaptionVectorIndex(ResourceArgs args)
        {
            var svgCollection = _GetVectorImageList(args);
            string text = DxComponent.GetCaptionForIcon(args.Caption);
            string key = "*" + text;
            return svgCollection.GetImageId(key, n => _CreateCaptionVector(args));
        }
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="sizeType"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        public static Image CreateCaptionImage(string caption, ResourceImageSizeType? sizeType, Size? imageSize)
        { return Instance._CreateCaptionImage(new ResourceArgs(sizeType: sizeType, imageSize: imageSize, caption: caption)); }
        /// <summary>
        /// Vyrenderuje dodaný text jako náhradní ikonu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Image _CreateCaptionImage(ResourceArgs args)
        {
            string text = DxComponent.GetCaptionForIcon(args.Caption);                    // Odeberu mezery a nepísmenové znaky
            if (text.Length == 0) return null;

            if (!args.SizeType.HasValue) args.SizeType = ResourceImageSizeType.Large;
            bool isDark = DxComponent.IsDarkTheme;
            Color backColor = (isDark ? SvgImageCustomize.DarkColor38 : SvgImageCustomize.LightColorFF);
            Color textColor = (isDark ? SvgImageCustomize.LightColorD4 : SvgImageCustomize.DarkColor38);
            Color lineColor = textColor;

            var realSize = args.ImageSize ?? DxComponent.GetDefaultImageSize(args.SizeType.Value);
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
                textBounds.X = textBounds.X - 2;
                textBounds.Width = textBounds.Width + 4;   // Radši ať se znaky vejdou a ořízne se jejich okraj, než aby vypadl celý znak...
                using (var stringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.None, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit | StringFormatFlags.FitBlackBox })
                {
                    if (args.SizeType.Value != ResourceImageSizeType.Large)
                    {   // Malé a střední ikony
                        if (text.Length > 2) text = text.Substring(0, 2);                // Radši bych tu viděl jen jedno písmenko, ale JD...
                        float fontSize = _GetCaptionFontSize(graphics, sysFont, text, bounds.Width);
                        using (Font font = new Font(sysFont.FontFamily, fontSize))
                        {
                            //var textSize = graphics.MeasureString(text, font);
                            //var textBounds = textSize.AlignTo(bounds, ContentAlignment.MiddleCenter);
                            graphics.DrawString(text, font, DxComponent.PaintGetSolidBrush(textColor), textBounds, stringFormat);
                        }
                    }
                    else
                    {   // Velké ikony
                        if (text.Length > 2) text = text.Substring(0, 2);
                        float fontSize = _GetCaptionFontSize(graphics, sysFont, text, bounds.Width - 2);
                        using (Font font = new Font(sysFont.FontFamily, fontSize))
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
        /// Vrátí vhodnou velikost fontu
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="sysFont"></param>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private static float _GetCaptionFontSize(Graphics graphics, Font sysFont, string text, float width)
        {
            var textSize = graphics.MeasureString("XX", sysFont);
            float tw = textSize.Width;
            float fs = sysFont.Size;
            if (tw <= 1f) return sysFont.Size;
            float iw = width - 2f;
            float ratio = iw / tw;
            return ratio * fs;
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
        { return Instance._TryGetResourceContentType(new ResourceArgs(imageName, exactName, sizeType, null, null, null, preferVector), out contentType); }
        private bool _TryGetResourceContentType(ResourceArgs args, out ResourceContentType contentType)
        {
            contentType = ResourceContentType.None;
            if (!args.HasImageName) return false;

            if (_TryGetContentTypeImageArray(args.ImageName, out contentType, out var _))
                return true;
            if (_TryGetApplicationResources(args, ResourceContentType.All, out var validItems))
                return _TryGetContentTypeApplication(validItems, args, out contentType);
            if (_ExistsDevExpressResource(args.ImageName))
                return _TryGetContentTypeDevExpress(args.ImageName, out contentType);

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
        /// Hodnota se za běhu aplikace může změnit.
        /// </summary>
        private bool _IsPreferredVectorImage { get { return SystemAdapter.IsPreferredVectorImage; } }
        /// <summary>
        /// Vrátí prioritu obsahu v rámci stejného typu obsahu podle přípony.
        /// Určuje prioritu jen v rámci jednoho typu obsahu.
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
            __SvgImageModifier = new SvgImageModifier();
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
        [Obsolete("Používá se metoda ConvertSvgImageToPalette!", true)]
        private static DxSvgImage ConvertToDarkSkin(string imageName, byte[] content, Size? targetSize = null)
        { return Instance.__SvgImageCustomize.ConvertToDarkSkin(imageName, content, targetSize); }
        /// <summary>
        /// Dodaný <see cref="SvgImage"/> převede do dark skin barev
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="svgImage"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        [Obsolete("Používá se metoda ConvertSvgImageToPalette!", true)]
        private static DxSvgImage ConvertToDarkSkin(string imageName, SvgImage svgImage, Size? targetSize = null)
        { return Instance.__SvgImageCustomize.ConvertToDarkSkin(imageName, svgImage, targetSize); }
        /// <summary>
        /// Instance Svg modifikátoru
        /// </summary>
        private SvgImageModifier __SvgImageModifier;
        /// <summary>
        /// Dodaný vektorový obrázek (<see cref="SvgImage"/>) převede do dané cílové palety
        /// </summary>
        /// <param name="content">Obsah definice obrázku</param>
        /// <param name="paletteType">Cílová paleta</param>
        /// <param name="imageName">Jméno obrázku, může vyvolat další konverze</param>
        /// <param name="targetSize">Cílová velikost, může vyvolat další konverze</param>
        /// <returns></returns>
        internal static byte[] ConvertSvgImageToPalette(byte[] content, DxSvgImagePaletteType paletteType, string imageName = null, Size? targetSize = null)
        { return Instance.__SvgImageModifier.Convert(content, paletteType, imageName, targetSize); }
        #endregion
        #region class ResourceArgs
        /// <summary>
        /// Třída, obsahující argumenty předávané mezi metodami pro práci se zdroji v <see cref="DxComponent"/>.
        /// </summary>
        private class ResourceArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="imageName">Jméno obrázku</param>
            /// <param name="exactName"></param>
            /// <param name="sizeType">Cílový typ velikosti; každá velikost má svoji kolekci (viz <see cref="GetVectorImageList(ResourceImageSizeType)"/>)</param>
            /// <param name="optimalSvgSize"></param>
            /// <param name="imageSize"></param>
            /// <param name="caption"></param>
            /// <param name="isPreferredVectorImage"></param>
            /// <param name="disabled">Převést barvy na Disabled variantu (pokud má hodnotu true)</param>
            /// <param name="image"></param>
            /// <param name="smallButton"></param>
            /// <param name="forceToIcon">Povinně vkládat ikonu do <see cref="Form.Icon"/> i kdyby byl k dispozici objekt <see cref="XtraForm.IconOptions"/></param>
            /// <param name="prepareDisabledImage">Požadavek na přípravu ikony typu 'Disabled' i pro ten prvek Ribbonu, který má aktuálně hodnotu Enabled = true;</param>
            public ResourceArgs(string imageName = null, bool exactName = false,
                    ResourceImageSizeType? sizeType = null, Size? optimalSvgSize = null, Size? imageSize = null,
                    string caption = null, bool? isPreferredVectorImage = null, bool? disabled = null,
                    Image image = null, bool smallButton = false, bool forceToIcon = false, bool prepareDisabledImage = false)
            {
                this.ImageName = imageName;
                this.ExactName = exactName;
                this.SizeType = sizeType;
                this.OptimalSvgSize = optimalSvgSize;
                this.ImageSize = imageSize;
                this.Caption = caption;
                this.IsPreferredVectorImage = isPreferredVectorImage;
                this.Disabled = disabled;
                this.Image = image;
                this.SmallButton = smallButton;
                this.ForceToIcon = forceToIcon;
                this.PrepareDisabledImage = prepareDisabledImage;
            }
            /// <summary>
            /// Vrátí klon this, kde nuluje <see cref="Image"/> a <see cref="ImageName"/>, ponechá tedy jen <see cref="Caption"/>.
            /// </summary>
            /// <returns></returns>
            public ResourceArgs CloneForOnlyCaption()
            {
                ResourceArgs clone = this.MemberwiseClone() as ResourceArgs;
                clone.ImageName = null;
                clone.Image = null;
                return clone;
            }
            /// <summary>
            /// Vrátí klon this, kde nastaví na danou hodnotu <see cref="SizeType"/>.
            /// </summary>
            /// <param name="sizeType"></param>
            /// <returns></returns>
            public ResourceArgs CloneForSize(ResourceImageSizeType? sizeType)
            {
                ResourceArgs clone = this.MemberwiseClone() as ResourceArgs;
                clone.SizeType = sizeType;
                return clone;
            }
            /// <summary>
            /// Vrátí klon this, kde nastaví na danou hodnotu <see cref="Disabled"/>.
            /// </summary>
            /// <param name="disabled"></param>
            /// <returns></returns>
            public ResourceArgs CloneForDisabled(bool? disabled)
            {
                ResourceArgs clone = this.MemberwiseClone() as ResourceArgs;
                clone.Disabled = disabled;
                return clone;
            }
            /// <summary>
            /// Jméno obrázku
            /// </summary>
            public string ImageName { get; set; }
            /// <summary>
            /// Je zadané jméno <see cref="ImageName"/> ?
            /// </summary>
            public bool HasImageName { get { return !String.IsNullOrEmpty(ImageName); } }
            /// <summary>
            /// Jméno má být chápáno exaktně = včetně suffixu s velikostí a včetně přípony
            /// </summary>
            public bool ExactName { get; set; }
            /// <summary>
            /// Typ velikosti
            /// </summary>
            public ResourceImageSizeType? SizeType { get; set; }
            /// <summary>
            /// Platný typ velikosti <see cref="SizeType"/>, default = <see cref="ResourceImageSizeType.Large"/>
            /// </summary>
            public ResourceImageSizeType CurrentSizeType { get { return (this.SizeType ?? ResourceImageSizeType.Large); } }
            /// <summary>
            /// Cílová velikost SVG
            /// </summary>
            public Size? OptimalSvgSize { get; set; }
            /// <summary>
            /// Požadovaná velikost IMG
            /// </summary>
            public Size? ImageSize { get; set; }
            /// <summary>
            /// Náhradní text tlačítka
            /// </summary>
            public string Caption { get; set; }
            /// <summary>
            /// Je zadaný text <see cref="Caption"/> ?
            /// </summary>
            public bool HasCaption { get { return !String.IsNullOrEmpty(Caption); } }
            /// <summary>
            /// Explicitně dodaný obrázek
            /// </summary>
            public Image Image { get; set; }
            /// <summary>
            /// Je zadaný explicitní obrázek <see cref="Image"/> ?
            /// </summary>
            public bool HasImage { get { return (this.Image != null); } }
            /// <summary>
            /// Cílem má být malý button
            /// </summary>
            public bool SmallButton { get; set; }
            /// <summary>
            /// Povinně vkládat ikonu do <see cref="Form.Icon"/> i kdyby byl k dispozici objekt <see cref="XtraForm.IconOptions"/>
            /// </summary>
            public bool ForceToIcon { get; set; }
            /// <summary>
            /// Požadavek na přípravu ikony typu 'Disabled' i pro ten prvek Ribbonu, který má aktuálně hodnotu Enabled = true
            /// </summary>
            public bool PrepareDisabledImage { get; set; }
            /// <summary>
            /// Preference vektorů: true = vektory; false = bitmapy, null = podle konfigurace
            /// </summary>
            public bool? IsPreferredVectorImage { get; set; }
            /// <summary>
            /// Má být generován obrázek Disabled (týká se pouze aplikačních vektorových ikon)
            /// </summary>
            public bool? Disabled { get; set; }
        }
        #endregion
    }
    /// <summary>
    /// Knihovna zdrojů výhradně aplikace (Resources.bin, adresář Images), nikoli zdroje DevEpxress.
    /// Tyto zdroje jsou získány pomocí metod <see cref="SystemAdapter.GetResources()"/> atd.
    /// <para/>
    /// Toto je pouze knihovna = zdroj dat (a jejich vyhledávání), ale nikoli výkonný blok, tady se negenerují obrázky ani nic dalšího.
    /// <para/>
    /// Zastřešující algoritmy pro oba druhy zdrojů (aplikační i DevExpress) jsou v <see cref="DxComponent"/>, 
    /// metody typicky <see cref="DxComponent.ApplyImage(ImageOptions, string, Image, ResourceImageSizeType?, Size?, bool, string, bool, bool?, bool)"/>.
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
                _ResourceTypes = null;
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
                {
                    _ResourceItems.Add(item);
                    _ResourceTypes = null;
                }
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
            /// Suma typů obsahu v tomto packu.
            /// Protože typ <see cref="ResourceContentType "/> je Flags, pak v <see cref="ResourceTypes"/> 
            /// je souhrn bitů všech konkrétních prvků obsažených v this.<see cref="ResourceItems"/>.
            /// </summary>
            public ResourceContentType ResourceTypes
            {
                get
                {
                    if (!_ResourceTypes.HasValue)
                    {   // Nemáme platný údaj, určíme jej právě nyní OnDemand:
                        ResourceContentType types = ResourceContentType.None;
                        _ResourceItems.ForEachExec(i => types |= i.ContentType);
                        _ResourceTypes = types;
                    }
                    return _ResourceTypes.Value;
                }
            }
            private ResourceContentType? _ResourceTypes;
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
                this._SvgImagesContent = new Dictionary<DxSvgImagePaletteType, byte[]>();
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
            /// do tmavého skinu (pomocí metody <see cref="DxComponent.ConvertSvgImageToPalette(byte[], DxSvgImagePaletteType, string, Size?)"/>
            /// </summary>
            /// <returns></returns>
            public DxSvgImage CreateSvgImage()
            {
                return CreateSvgImage(DxSvgImagePaletteType.LightSkin);
            }
            /// <summary>
            /// Metoda vrátí new instanci <see cref="DevExpress.Utils.Svg.SvgImage"/> vytvořenou z <see cref="Content"/>.
            /// Pokud ale this instance není SVG (<see cref="IsSvg"/> je false) anebo není platná, vyhodí chybu!
            /// Vrácený objekt není nutno rewindovat (jako u nativní knihovny DevExpress).
            /// <para/>
            /// Je vrácena new instance objektu, ale tato třída není <see cref="IDisposable"/>, tedy nepoužívá se v using { } patternu.
            /// <para/>
            /// Pozor, tato třída v této metodě řeší přebarvení aplikační SVG ikony ze světlého skinu (=nativní) 
            /// do cílové palety.
            /// </summary>
            /// <param name="palette">Cílová paleta barev</param>
            /// <returns></returns>
            public DxSvgImage CreateSvgImage(DxSvgImagePaletteType palette)
            {
                // Klíč obrázku obsahuje jako prefix informaci o (darkSkin, isDisabled):
                string key = DxComponent.CreateVectorImageKey(this.ItemKey, palette);

                // V Dictionary _SvgImagesContent si držím pole byte pro jednotlivé palety (světlý/tmavý skin, enabled/disabled):
                bool exists = _SvgImagesContent.TryGetValue(palette, out var imageContent);
                if (imageContent is null)
                {   // Pokud pro danou kombinaci (darkSkin, isDisabled) dosud nemáme konvertovaná data, pak je musíme vygenerovat:
                    if (!IsSvg) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} is not SVG type, ContentType is {ContentType}.");
                    var resourceContent = Content;
                    if (resourceContent == null) throw new InvalidOperationException($"ResourceItem.CreateSvgImage() error: Resource {ItemKey} can not load content.");

                    imageContent = DxComponent.ConvertSvgImageToPalette(resourceContent, palette, this.ItemKey);
                    if (!exists)
                        _SvgImagesContent.Add(palette, imageContent);
                    else
                        _SvgImagesContent[palette] = imageContent;
                }

                return DxSvgImage.Create(this.ItemKey, palette, imageContent);
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
            /// <summary>
            /// Obsah SvgImage pro jednotlivé palety barev
            /// </summary>
            private Dictionary<DxSvgImagePaletteType, byte[]> _SvgImagesContent;
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
            if (ContainsOriginalImages)
                this.OriginalImageDict = new Dictionary<string, Image>();
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
            if (ContainsOriginalImages)
            {
                this.OriginalImageDict?.Values.ForEachExec(i => i?.Dispose());
                this.OriginalImageDict?.Clear();
                this.OriginalImageDict = null;
            }
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
        /// Obsahuje true, když pro aktuální velikost používáme i <see cref="OriginalImageDict"/>
        /// </summary>
        public bool ContainsOriginalImages { get { return (this.SizeType == ResourceImageSizeType.Original); } }
        /// <summary>
        /// Dictionary originálních obrázků, pouze pro velikost <see cref="SizeType"/> == <see cref="ResourceImageSizeType.Original"/>
        /// </summary>
        public Dictionary<string, Image> OriginalImageDict { get; private set; }
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
        /// Najde index pro <see cref="Image"/> daného jména, volitelně si nechá Image vytvořit pomocí <paramref name="creator"/> a uloží si získaný Image.
        /// Pokud nenajde a ani nevytvoří, vrací -1. Běžně vrací index Image.
        /// <para/>
        /// Tato varianta vytváří běžný Image, který se při přebarvení skinu nezmění.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public int GetImageId(string key, Func<string, Image> creator)
        {
            if (key is null) return -1;
            if (!this.ContainsKey(key))
            {
                if (creator == null) return -1;
                Image image = creator(key);
                if (image is null) return -1;

                this.ImageList.Images.Add(key, image);
                if (ContainsOriginalImages)
                    this.OriginalImageDict.Store(key, image);
            }
            return this.IndexOfKey(key);
        }
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> daného jména, volitelně si nechá Image vytvořit pomocí <paramref name="creator"/> a uloží si získaný Image.
        /// Pokud nenajde a ani nevytvoří, vrací null. Běžně vrací nalezený Image.
        /// <para/>
        /// Tato varianta vytváří běžný Image, který se při přebarvení skinu nezmění.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public Image GetImage(string key, Func<string, Image> creator)
        {
            if (key is null) return null;
            if (!this.ContainsKey(key))
            {
                if (creator == null) return null;
                Image image = creator(key);
                if (image is null) return null;
                this.ImageList.Images.Add(key, image);
                if (ContainsOriginalImages)
                    this.OriginalImageDict.Store(key, image);
            }
            if (this.SizeType == ResourceImageSizeType.Original && this.OriginalImageDict.TryGetValue(key, out var foundImage)) 
                return foundImage;
            return this.ImageList.Images[key];
        }
        /// <summary>
        /// Přidá prvek, který bude možno po změně skinu znovu vytvořit pro novou barevnost (podpora pro tvorbu Image pro světlý / tmavý skin, tvorba Image podle Caption)
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
            if (ContainsOriginalImages)
                this.OriginalImageDict.Store(key, image);
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
            /// Vytvoří a vrátí new Image pro this data, tedy pro aktuálně platný skin a zoom
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
            // Velikost po změně Zoomu neměníme:
            //  this.ImageSize = DxComponent.GetImageSize(SizeType, true);
            //  this.ReCreateImages();
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
            this.ReCreateImages();
        }
        /// <summary>
        /// Vytvoří nově všechny potřebné Images
        /// </summary>
        private void ReCreateImages()
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
}
