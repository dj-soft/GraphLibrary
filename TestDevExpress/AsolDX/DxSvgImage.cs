// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DevExpress.Utils;
using DevExpress.Utils.Svg;

using Noris.WS.DataContracts.Desktop.Data;

namespace Noris.Clients.Win.Components.AsolDX
{
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
            return Create(null, DxSvgImagePaletteType.Explicit, Encoding.UTF8.GetBytes(xmlContent));
        }
        /// <summary>
        /// Static konstruktor z podkladového <see cref="SvgImage"/>
        /// </summary>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        public static DxSvgImage Create(SvgImage svgImage)
        {
            if (svgImage == null) return null;
            return Create(null, DxSvgImagePaletteType.Explicit, svgImage.ToXmlString());
        }
        /// <summary>
        /// Static konstruktor z dodaného pole byte
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DxSvgImage Create(byte[] data)
        {
            if (data == null) return null;
            return Create(null, DxSvgImagePaletteType.Explicit, data);
        }
        /// <summary>
        /// Static konstruktor pro dodaná data
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="palette"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DxSvgImage Create(string imageName, DxSvgImagePaletteType palette, byte[] data)
        {
            if (data is null) return null;
            DxSvgImage dxSvgImage = null;
            using (var stream = new System.IO.MemoryStream(data))
                dxSvgImage = new DxSvgImage(stream);
            dxSvgImage.ImageName = imageName;
            dxSvgImage.Palette = palette;
            return dxSvgImage;
        }
        /// <summary>
        /// Static konstruktor pro dodaná data
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="palette"></param>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        public static DxSvgImage Create(string imageName, DxSvgImagePaletteType palette, string xmlContent)
        {
            if (String.IsNullOrEmpty(xmlContent)) return null;
            return Create(imageName, palette, Encoding.UTF8.GetBytes(xmlContent));
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
            return Create(null, DxSvgImagePaletteType.Explicit, data);
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
        /// Paleta tohoto obrázku
        /// </summary>
        public DxSvgImagePaletteType Palette { get; private set; }
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
        /// Vyrenderuje SvgImage do Image
        /// </summary>
        /// <param name="svgImage"></param>
        /// <param name="size"></param>
        /// <param name="alignment"></param>
        /// <param name="svgPalette"></param>
        /// <returns></returns>
        public static Image RenderToImage(SvgImage svgImage, Size size, ContentAlignment alignment = ContentAlignment.MiddleCenter, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null)
        {
            Rectangle bounds = new Rectangle(Point.Empty, size);
            Bitmap image = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                _RenderTo(svgImage, graphics, bounds, alignment, svgPalette, out var imageBounds);
            }
            return image;
        }
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

            // Matrix(a,b,c,d,e,f):  Xr = (a * Xi  +  c * Yi  +  e);   Yr = (d * Yi  +  b * Xi  +  f);
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
                    case "arrowsmall": return _TryGetGenericSvgArrowSmall(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "arrow1": return _TryGetGenericSvgArrow1(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "arrow": return _TryGetGenericSvgArrow1(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "editsmall": return _TryGetGenericSvgEditAny(imageName, genericItems, sizeType, ref dxSvgImage, 2);
                    case "edit": return _TryGetGenericSvgEditAny(imageName, genericItems, sizeType, ref dxSvgImage, 0);
                    case "text": return _TryGetGenericSvgText(imageName, genericItems, sizeType, ref dxSvgImage);
                    case "textonly": return _TryGetGenericSvgTextOnly(imageName, genericItems, sizeType, ref dxSvgImage);
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
                return Create(this.ImageName, this.Palette, this.XmlContent);
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
        /// Očekávaná deklarace zní: "@circlegradient1|violet|75", 
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
            dxSvgImage = DxSvgImage.Create(imageName, DxSvgImagePaletteType.LightSkin, xmlContent);
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
        #region Arrow - šipky, zatím nepracují v systému Coordinates; viz Edit
        /// <summary>
        /// Tvoří malou šipku.
        /// Očekávaná deklarace zní: "@arrowsmall|U|blue", 
        /// kde "arrowsmall" je klíč pro malou šipku;
        /// kde "U" je směr a typ šipky (první písmeno z: Ceiling, Up, Down, Floor, Begin, Left, Right, End);
        /// kde "violet" je typ barvy;
        /// Argumenty:
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgArrowSmall(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage)
        {
            // Souřadný systém pro šipky (a jiné tvary):
            //  pro otáčení tvaru svisle/vodorovně platí: definujeme souřadnice 'a' a 'b', kde pro svislý tvar x = a, y = b; a pro vodorovný tvar x = b, y = a;
            //  pro zrcadlení tvaru nahoru/dolů použijeme záporné souřadnice, generátor je pak otočí vzhledem k velikosti tvaru size.
            //    Máme li např. základu na souřadnici 5, pak pro zrcadlený tvar předáme -5, a generátor tvarů pro size = 32 provede (32 + (-5)) = 27 = zrcadlená hodnota 5.
            // Velikost deklarujeme pro 32px, pro malé tvary následně dělíme 2.
            //      Komentáře souřadnic jsou pro šipku nahoru s horní linkou, tvar  ArrowType.Ceiling,  kde a=x,  b=y :
            //      Obrazec se skládá ze šipky, z čáry vedoucí do šipky, z linky nad šipkou
            int a0 = 8;                // levý okraj šipky
            int a2 = 14;               // levý okraj čáry
            int a4 = 16;               // střed šipky
            int a6 = 18;               // pravý okraj čáry
            int a8 = 24;               // pravý okraj šipky
            int b0 = 24;               // dolní hrana čáry
            int b2 = 18;               // dolní hrana šipky
            int b4 = 10;               // horní hrana šipky
            int b6 = 10;               // dolní hrana linky
            int b8 = 8;                // horní hrana linky

            return _TryGetGenericSvgArrowAny(imageName, genericItems, sizeType, ref dxSvgImage,
                a0, a2, a4, a6, a8, b0, b2, b4, b6, b8);
        }
        /// <summary>
        /// Tvoří šipku.
        /// Očekávaná deklarace zní: "@arrow|U|violet", 
        /// kde "arrow" je klíč pro šipku;
        /// kde "U" je směr a typ šipky (první písmeno z: Ceiling, Up, Down, Floor, Begin, Left, Right, End);
        /// kde "violet" je typ barvy;
        /// Argumenty:
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgArrow1(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage)
        {
            // Souřadný systém pro šipky (a jiné tvary):
            //  pro otáčení tvaru svisle/vodorovně platí: definujeme souřadnice 'a' a 'b', kde pro svislý tvar x = a, y = b; a pro vodorovný tvar x = b, y = a;
            //  pro zrcadlení tvaru nahoru/dolů použijeme záporné souřadnice, generátor je pak otočí vzhledem k velikosti tvaru size.
            //    Máme li např. základu na souřadnici 5, pak pro zrcadlený tvar předáme -5, a generátor tvarů pro size = 32 provede (32 + (-5)) = 27 = zrcadlená hodnota 5.
            // Velikost deklarujeme pro 32px, pro malé tvary následně dělíme 2.
            //      Komentáře souřadnic jsou pro šipku nahoru s horní linkou, tvar  ArrowType.Ceiling,  kde a=x,  b=y :
            //      Obrazec se skládá ze šipky, z čáry vedoucí do šipky, z linky nad šipkou
            int a0 = 4;                // levý okraj šipky
            int a2 = 12;               // levý okraj čáry
            int a4 = 16;               // střed šipky
            int a6 = 20;               // pravý okraj čáry
            int a8 = 28;               // pravý okraj šipky
            int b0 = 28;               // dolní hrana čáry
            int b2 = 18;               // dolní hrana šipky
            int b4 = 6;                // horní hrana šipky
            int b6 = 6;                // dolní hrana linky
            int b8 = 2;                // horní hrana linky

            return _TryGetGenericSvgArrowAny(imageName, genericItems, sizeType, ref dxSvgImage,
                a0, a2, a4, a6, a8, b0, b2, b4, b6, b8);
        }
        /// <summary>
        /// Tvoří šipku.
        /// Očekávaná deklarace zní: "@arrow|U|violet", 
        /// kde "arrow1" je klíč pro šipku;
        /// kde "T" je směr a typ šipky (první písmeno z: Ceiling, Up, Down, Floor, Begin, Left, Right, End);
        /// kde "violet" je typ barvy;
        /// <para/>
        /// Souřadný systém pro šipky (a jiné tvary):
        ///  pro otáčení tvaru svisle/vodorovně platí: definujeme souřadnice 'a' a 'b', kde pro svislý tvar x = a, y = b; a pro vodorovný tvar x = b, y = a;
        ///  pro zrcadlení tvaru nahoru/dolů použijeme záporné souřadnice, generátor je pak otočí vzhledem k velikosti tvaru size.
        ///    Máme li např. základu na souřadnici 5, pak pro zrcadlený tvar předáme -5, a generátor tvarů pro size = 32 provede (32 + (-5)) = 27 = zrcadlená hodnota 5.
        /// Velikost deklarujeme pro 32px, pro malé tvary následně dělíme 2.
        ///      Komentáře souřadnic jsou pro šipku nahoru s horní linkou, tvar  ArrowType.Ceiling,  kde a=x,  b=y :
        ///      Obrazec se skládá ze šipky, z čáry vedoucí do šipky, z linky nad šipkou
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <param name="a0">levý okraj šipky</param>
        /// <param name="a2">levý okraj čáry</param>
        /// <param name="a4">střed šipky</param>
        /// <param name="a6">pravý okraj čáry</param>
        /// <param name="a8">pravý okraj šipky</param>
        /// <param name="b0">dolní hrana čáry</param>
        /// <param name="b2">dolní hrana šipky</param>
        /// <param name="b4">horní hrana šipky</param>
        /// <param name="b6">dolní hrana linky</param>
        /// <param name="b8">horní hrana linky</param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgArrowAny(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage,
            int a0, int a2, int a4, int a6, int a8, int b0, int b2, int b4, int b6, int b8)
        {
            int size = (sizeType.HasValue && sizeType.Value == ResourceImageSizeType.Small ? 16 : 32);
            string xmlHeader = _GetXmlContentHeader(size);

            string xmlStyles = _GetXmlDevExpressStyles();

            ArrowType arrowType = _GetArrowType(_GetGenericParam(genericItems, 1, ""));
            string arrowColorName = _GetGenericParam(genericItems, 2, "");
            _ResolveSvgDesignParam(ref arrowColorName, "class='Blue'");

            int arrowLineDiff = _GetGenericParam(genericItems, 3, -1);
            if (arrowLineDiff >= 0)
            {   // Tento parametr může určovat délku čáry v šipce, tedy rozdíl b0 - b2.
                // Pokud je 0 a kladný, pak nastavuje b0 = b2 + Diff, přičemž výsledné b0 nemůže být větší než 32:
                b0 = b2 + arrowLineDiff;
                if (b0 > 32) b0 = 32;
            }

            string xmlGradient = "";

            string paths = "";
            switch (arrowType)
            {
                case ArrowType.Ceiling:
                    paths += _GetArrowPartArrow(size, arrowColorName, false, false, b0, b2, b4, a2, a0, a4, a8, a6);
                    paths += _GetArrowPartLine(size, arrowColorName, false, false, b6, b8, a0, a8);
                    break;
                case ArrowType.Up:
                    paths += _GetArrowPartArrow(size, arrowColorName, false, false, b0, b2, b4, a2, a0, a4, a8, a6);
                    break;
                case ArrowType.Down:
                    paths += _GetArrowPartArrow(size, arrowColorName, false, true, b0, b2, b4, a2, a0, a4, a8, a6);
                    break;
                case ArrowType.Floor:
                    paths += _GetArrowPartArrow(size, arrowColorName, false, true, b0, b2, b4, a2, a0, a4, a8, a6);
                    paths += _GetArrowPartLine(size, arrowColorName, false, true, b6, b8, a0, a8);
                    break;

                case ArrowType.Begin:
                    paths += _GetArrowPartArrow(size, arrowColorName, true, false, b0, b2, b4, a2, a0, a4, a8, a6);
                    paths += _GetArrowPartLine(size, arrowColorName, true, false, b6, b8, a0, a8);
                    break;
                case ArrowType.Left:
                    paths += _GetArrowPartArrow(size, arrowColorName, true, false, b0, b2, b4, a2, a0, a4, a8, a6);
                    break;
                case ArrowType.Right:
                    paths += _GetArrowPartArrow(size, arrowColorName, true, true, b0, b2, b4, a2, a0, a4, a8, a6);
                    break;
                case ArrowType.End:
                    paths += _GetArrowPartArrow(size, arrowColorName, true, true, b0, b2, b4, a2, a0, a4, a8, a6);
                    paths += _GetArrowPartLine(size, arrowColorName, true, true, b6, b8, a0, a8);
                    break;

            }

            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlGradient + paths + xmlFooter;
            dxSvgImage = DxSvgImage.Create(imageName, DxSvgImagePaletteType.LightSkin, xmlContent);
            dxSvgImage.SizeType = sizeType;
            dxSvgImage.GenericSource = imageName;
            return true;

            /*     pro zadání    @arrow|C|Blue   vygeneruje SVG:
﻿<?xml version='1.0' encoding='UTF-8'?>
<svg x="0px" y="0px" viewBox="0 0 32 32" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" id="Layer_1" style="enable-background:new 0 0 32 32">
  <style type="text/css"> .Blue{fill:#1177D7;} </style>
  <polygon points="20,28 12,28 12,18 4,18 16,6 28,18 20,18 " class="Blue" />
  <polygon points="4,6 4,2 28,2 28,6 " class="Blue" />
</svg>
            */
        }
        /// <summary>
        /// Vytvoří a vrátí tvar šipky
        /// </summary>
        /// <param name="size"></param>
        /// <param name="arrowColorName"></param>
        /// <param name="swapXY"></param>
        /// <param name="mirror"></param>
        /// <param name="b0"></param>
        /// <param name="b2"></param>
        /// <param name="b4"></param>
        /// <param name="a2"></param>
        /// <param name="a0"></param>
        /// <param name="a4"></param>
        /// <param name="a8"></param>
        /// <param name="a6"></param>
        /// <returns></returns>
        private static string _GetArrowPartArrow(int size, string arrowColorName, bool swapXY, bool mirror, int b0, int b2, int b4, int a2, int a0, int a4, int a8, int a6)
        {
            bool half = (size <= 16);
            if (half || mirror)
            {
                _GetArrowModifyDim(ref b0, half, size, mirror);
                _GetArrowModifyDim(ref b2, half, size, mirror);
                _GetArrowModifyDim(ref b4, half, size, mirror);
                _GetArrowModifyDim(ref a0, half, size, mirror);
                _GetArrowModifyDim(ref a2, half, size, mirror);
                _GetArrowModifyDim(ref a4, half, size, mirror);
                _GetArrowModifyDim(ref a6, half, size, mirror);
                _GetArrowModifyDim(ref a8, half, size, mirror);
            }

            return (!swapXY ?
                $"    <polygon points=\"{a6},{b0} {a2},{b0} {a2},{b2} {a0},{b2} {a4},{b4} {a8},{b2} {a6},{b2} \" {arrowColorName} />\r\n" :
                $"    <polygon points=\"{b0},{a6} {b0},{a2} {b2},{a2} {b2},{a0} {b4},{a4} {b2},{a8} {b2},{a6} \" {arrowColorName} />\r\n");
        }
        /// <summary>
        /// Vytvoří a vrátí tvar šipky
        /// </summary>
        /// <param name="size"></param>
        /// <param name="arrowColorName"></param>
        /// <param name="swapXY"></param>
        /// <param name="mirror"></param>
        /// <param name="b6"></param>
        /// <param name="b8"></param>
        /// <param name="a0"></param>
        /// <param name="a8"></param>
        /// <returns></returns>
        private static string _GetArrowPartLine(int size, string arrowColorName, bool swapXY, bool mirror, int b6, int b8, int a0, int a8)
        {
            bool half = (size <= 16);
            if (half || mirror)
            {
                _GetArrowModifyDim(ref b6, half, size, mirror);
                _GetArrowModifyDim(ref b8, half, size, mirror);
                _GetArrowModifyDim(ref a0, half, size, mirror);
                _GetArrowModifyDim(ref a8, half, size, mirror);
            }

            return (!swapXY ?
                $"    <polygon points=\"{a0},{b6} {a0},{b8} {a8},{b8} {a8},{b6} \" {arrowColorName} />\r\n" :
                $"    <polygon points=\"{b6},{a0} {b8},{a0} {b8},{a8} {b6},{a8} \" {arrowColorName} />\r\n");
        }
        /// <summary>
        /// Modifikuje dimenzi
        /// </summary>
        /// <param name="dim"></param>
        /// <param name="half"></param>
        /// <param name="size"></param>
        /// <param name="mirror"></param>
        private static void _GetArrowModifyDim(ref int dim, bool half, int size, bool mirror)
        {
            if (mirror) dim = size - dim;
            if (half) dim = dim / 2;
        }
        /// <summary>
        /// Konvertuje zadaný string na typ šipky
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static ArrowType _GetArrowType(string text)
        {
            string key = (text ?? "U").Trim().ToUpper();
            switch (key)
            {   // Ceiling, Up, Down, Floor, Begin, Left, Right, End, Stop
                case "CEILING":
                case "TOP":
                case "FIRST":
                case "C":
                case "T": return ArrowType.Ceiling;

                case "UP":
                case "PREV":
                case "P":
                case "U": return ArrowType.Up;

                case "DOWN":
                case "NEXT":
                case "N":
                case "D": return ArrowType.Down;

                case "FLOOR":
                case "BOTTOM":
                case "LAST":
                case "F": return ArrowType.Floor;

                case "BEGIN":
                case "B": return ArrowType.Begin;

                case "LEFT":
                case "L": return ArrowType.Left;

                case "RIGHT":
                case "R": return ArrowType.Right;

                case "END":
                case "E": return ArrowType.End;

                case "STOP":
                case "S": return ArrowType.Stop;
            }

            return ArrowType.None;
        }
        /// <summary>
        /// Typy ikon pro Šipky
        /// </summary>
        private enum ArrowType { None, Ceiling, Up, Down, Floor, Begin, Left, Right, End, Stop }
        #endregion
        #region Edit
        /// <summary>
        /// Vytvoří ikonu pro editaci, v dané velikosti
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="genericItems"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <param name="subSize">Počet jednotek od okraje, o kolik má být ikona zmenšená. Hodnota 0=kreslí se až k okraji, 3=vynechá 3px z 16px ikony nebo 6px z 32px ikony...; povolené hodnoty 0-4</param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgEditAny(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage, int subSize)
        {
            int[] coordinates = _GetCoordinates(sizeType);
            subSize = (subSize < 0 ? 0 : (subSize > 4 ? 4 : subSize));

            EditType editType = _GetEditType(_GetGenericParam(genericItems, 1, ""));
            string editStyle1, editStyle2, editStyle3;
            string xmlHeader = _GetXmlContentHeader(coordinates);
            string xmlStyles = _GetXmlDevExpressStyles();
            string xmlGradient = "";

            string xmlPaths = "";
            switch (editType)
            {
                case EditType.SelectAll1:
                    // Čtyři čtverce kousek od sebe:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Blue'");
                    editStyle2 = _GetGenericSvgDesignParam(genericItems, 3, "class='White'");
                    xmlPaths += _GetEditPartSelectAll1(coordinates, subSize, editStyle1, editStyle2);
                    break;
                case EditType.SelectAll2:
                    // Střední čtverec dané barvy a černý tečkovaný okraj:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Blue'");
                    xmlPaths += _GetEditPartSelectAllCenter2(coordinates, subSize, editStyle1);
                    editStyle2 = _GetGenericSvgDesignParam(genericItems, 3, "class='Black'");
                    xmlPaths += _GetEditPartBorderIntermitent(coordinates, subSize, editStyle2);
                    break;
                case EditType.SelectAll3:
                    // Střední čtverec dané barvy a černý tečkovaný okraj:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Blue'");
                    xmlPaths += _GetEditPartSelectAllCenter3(coordinates, subSize, editStyle1);
                    editStyle2 = _GetGenericSvgDesignParam(genericItems, 3, "class='Black'");
                    xmlPaths += _GetEditPartBorderIntermitent(coordinates, subSize, editStyle2);
                    break;
                case EditType.Delete1:
                    // tenká linka:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Red'");
                    xmlPaths += _GetEditPartXCross(coordinates, subSize, editStyle1, 1);
                    break;
                case EditType.Delete2:
                    // tlustší linka:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Red'");
                    xmlPaths += _GetEditPartXCross(coordinates, subSize, editStyle1, 2);
                    break;
                case EditType.Copy:
                    editStyle1 = _GetGenericSvgDesignParam(genericItems, 2, "class='Black'");
                    editStyle2 = _GetGenericSvgDesignParam(genericItems, 3, "class='White'");
                    editStyle3 = _GetGenericSvgDesignParam(genericItems, 4, "class='White'");
                    int edge = _GetGenericParam(genericItems, 5, 3);
                    xmlPaths += _GetEditPartDocument(coordinates, subSize, editStyle1, editStyle3, 5, 1, 1, 3, 1, edge);
                    xmlPaths += _GetEditPartDocument(coordinates, subSize, editStyle1, editStyle2, 2, 3, 4, 1, 1, edge);
                    break;
            }

            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlGradient + xmlPaths + xmlFooter;
            dxSvgImage = DxSvgImage.Create(imageName, DxSvgImagePaletteType.LightSkin, xmlContent);
            dxSvgImage.SizeType = sizeType;
            dxSvgImage.GenericSource = imageName;
            return true;
        }
        /// <summary>
        /// Vykreslí střed = rectangle pro SelectAll1
        /// </summary>
        /// <param name="coordinates">Fyzické souřadnice</param>
        /// <param name="subSize">Okraj ikony, číslo v rozsahu 0-4 včetně</param>
        /// <param name="borderStyle"></param>
        /// <param name="fillStyle"></param>
        /// <returns></returns>
        private static string _GetEditPartSelectAll1(int[] coordinates, int subSize, string borderStyle, string fillStyle)
        {
            int a = 1;
            switch (subSize)
            {
                case 0: a = 1; break;
                case 1: a = 2; break;
                case 2: a = 3; break;
                case 3: a = 4; break;
                case 4: a = 4; break;
            }
            int thick = coordinates[1];
            int d0 = coordinates[a];
            int d1 = coordinates[9];
            int w = coordinates[7] - d0;

            string xmlPaths =
                _GetXmlPathRectangleFill(d0, d0, w, w, thick, borderStyle, fillStyle) +
                _GetXmlPathRectangleFill(d1, d0, w, w, thick, borderStyle, fillStyle) +
                _GetXmlPathRectangleFill(d0, d1, w, w, thick, borderStyle, fillStyle) +
                _GetXmlPathRectangleFill(d1, d1, w, w, thick, borderStyle, fillStyle);

            return xmlPaths;
        }
        /// <summary>
        /// Vykreslí střed pro ikonu SelectAll2
        /// </summary>
        /// <param name="coordinates">Fyzické souřadnice</param>
        /// <param name="subSize">Okraj ikony, číslo v rozsahu 0-4 včetně</param>
        /// <param name="editColorName"></param>
        /// <returns></returns>
        private static string _GetEditPartSelectAllCenter2(int[] coordinates, int subSize, string editColorName)
        {
            int b = coordinates[3 + subSize];
            int e = coordinates[13 - subSize];
            int s = e - b;
            string xmlPaths = _GetXmlPathRectangle(b, b, s, s, editColorName);
            return xmlPaths;
        }
        /// <summary>
        /// Vykreslí střed = rectangle pro SelectAll3
        /// </summary>
        /// <param name="coordinates">Fyzické souřadnice</param>
        /// <param name="subSize">Okraj ikony, číslo v rozsahu 0-4 včetně</param>
        /// <param name="editColorName"></param>
        /// <returns></returns>
        private static string _GetEditPartSelectAllCenter3(int[] coordinates, int subSize, string editColorName)
        {
            if (subSize >= 3) return _GetEditPartSelectAllCenter2(coordinates, subSize, editColorName);    // Velké okraje = malý střed => vrátím plný střed jako pro SelectAll1.

            string xmlPaths = "";
            switch (subSize)
            {
                case 0:
                    {
                        int x0 = coordinates[3], x1 = coordinates[5], x8 = coordinates[11], x9 = coordinates[13];
                        int y0 = coordinates[3], y1 = coordinates[5], y2 = coordinates[6], y3 = coordinates[8],
                            y4 = coordinates[9], y5 = coordinates[10], y6 = coordinates[11], y9 = coordinates[13];
                        xmlPaths += _GetXmlPathRectangle(x0, y0, x9 - x0, y9 - y0, 2, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y1, x8 - x1, y2 - y1, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y3, x8 - x1, y4 - y1, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y5, x8 - x1, y6 - y1, editColorName);
                    }
                    break;
                case 1:
                    {
                        int x0 = coordinates[4], x1 = coordinates[6], x8 = coordinates[10], x9 = coordinates[12];
                        int y0 = coordinates[4], y1 = coordinates[6], y2 = coordinates[7], y3 = coordinates[9],
                            y4 = coordinates[10], y9 = coordinates[12];
                        xmlPaths += _GetXmlPathRectangle(x0, y0, x9 - x0, y9 - y0, 2, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y1, x8 - x1, y2 - y1, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y3, x8 - x1, y4 - y1, editColorName);
                    }
                    break;
                case 2:
                    {
                        int x0 = coordinates[5], x1 = coordinates[7], x8 = coordinates[9], x9 = coordinates[11];
                        int y0 = coordinates[5], y1 = coordinates[7], y2 = coordinates[9], y9 = coordinates[11];
                        xmlPaths += _GetXmlPathRectangle(x0, y0, x9 - x0, y9 - y0, 2, editColorName);
                        xmlPaths += _GetXmlPathRectangle(x1, y1, x8 - x1, y2 - y1, editColorName);
                    }
                    break;
            }

            return xmlPaths;
        }
        /// <summary>
        /// Vykreslí okraj = border pro SelectAll1 i SelectAll2
        /// </summary>
        /// <param name="coordinates">Fyzické souřadnice</param>
        /// <param name="subSize">Okraj ikony, číslo v rozsahu 0-4 včetně</param>
        /// <param name="editColorName"></param>
        /// <returns></returns>
        private static string _GetEditPartBorderIntermitent(int[] coordinates, int subSize, string editColorName)
        {
            //   Vzoreček pro typ = 0 = rozměr 2 ÷ 30:
            //paths += $"    <polygon points=\"2,2 6,2 6,4 4,4 4,6 2,6 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"8,2 12,2 12,4 8,4 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"14,2 18,2 18,4 14,4 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"20,2 24,2 24,4 20,4 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"26,2 30,2 30,6 28,6 28,4 26,4 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"28,8 30,8 30,12 28,12 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"28,14 30,14 30,18 28,18 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"28,20 30,20 30,24 28,24 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"28,26,30,26 30,30 26,30 26,28 28,28 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"20,28 24,28 24,30 20,30 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"14,28 18,28 18,30 14,30 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"8,28 12,28 12,30 8,30 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"2,26 4,26 4,28 6,28 6,30 2,30 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"2,20 4,20 4,24 2,24 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"2,14 4,14 4,18 2,18 \" {editColorName} />\r\n";
            //paths += $"    <polygon points=\"2,8 4,8 4,12 2,12 \" {editColorName} />\r\n";

            string xmlPaths = "";
            switch (subSize)
            {
                case 0:
                    {
                        int a0 = coordinates[1], a1 = coordinates[2], a2 = coordinates[3],
                            bl = coordinates[4], br = coordinates[6], cl = coordinates[7], cr = coordinates[9], dl = coordinates[10], dr = coordinates[12],
                            e2 = coordinates[13], e1 = coordinates[14], e0 = coordinates[15];
                        xmlPaths += $"    <polygon points=\"{a0},{a0} {a2},{a0} {a2},{a1} {a1},{a1} {a1},{a2} {a0},{a2} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{bl},{a0} {br},{a0} {br},{a1} {bl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{a0} {cr},{a0} {cr},{a1} {cl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{dl},{a0} {dr},{a0} {dr},{a1} {dl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e2},{a0} {e0},{a0} {e0},{a2} {e1},{a2} {e1},{a1} {e2},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{bl} {e0},{bl} {e0},{br} {e1},{br} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{cl} {e0},{cl} {e0},{cr} {e1},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{dl} {e0},{dl} {e0},{dr} {e1},{dr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{e2},{e0},{e2} {e2},{e0} {e2},{e0} {e2},{e1} {e1},{e1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{dl},{e1} {dr},{e1} {dr},{e0} {dl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{e1} {cr},{e1} {cr},{e0} {cl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{bl},{e1} {br},{e1} {br},{e0} {bl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{e2} {a1},{e2} {a1},{e1} {a2},{e1} {a2},{e0} {a0},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{dl} {a1},{dl} {a1},{dr} {a0},{dr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{cl} {a1},{cl} {a1},{cr} {a0},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{bl} {a1},{bl} {a1},{br} {a0},{br} \" {editColorName} />\r\n";
                    }
                    break;
                case 1:
                    {
                        int a0 = coordinates[2], a1 = coordinates[3], a2 = coordinates[4],
                            bl = coordinates[5], br = coordinates[6], cl = coordinates[7], cr = coordinates[9], dl = coordinates[10], dr = coordinates[11],
                            e2 = coordinates[12], e1 = coordinates[13], e0 = coordinates[14];
                        xmlPaths += $"    <polygon points=\"{a0},{a0} {a2},{a0} {a2},{a1} {a1},{a1} {a1},{a2} {a0},{a2} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{bl},{a0} {br},{a0} {br},{a1} {bl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{a0} {cr},{a0} {cr},{a1} {cl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{dl},{a0} {dr},{a0} {dr},{a1} {dl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e2},{a0} {e0},{a0} {e0},{a2} {e1},{a2} {e1},{a1} {e2},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{bl} {e0},{bl} {e0},{br} {e1},{br} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{cl} {e0},{cl} {e0},{cr} {e1},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{dl} {e0},{dl} {e0},{dr} {e1},{dr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{e2},{e0},{e2} {e0},{e0} {e2},{e0} {e2},{e1} {e1},{e1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{dl},{e1} {dr},{e1} {dr},{e0} {dl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{e1} {cr},{e1} {cr},{e0} {cl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{bl},{e1} {br},{e1} {br},{e0} {bl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{e2} {a1},{e2} {a1},{e1} {a2},{e1} {a2},{e0} {a0},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{dl} {a1},{dl} {a1},{dr} {a0},{dr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{cl} {a1},{cl} {a1},{cr} {a0},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{bl} {a1},{bl} {a1},{br} {a0},{br} \" {editColorName} />\r\n";
                    }
                    break;
                case 2:
                    {
                        int a0 = coordinates[3], a1 = coordinates[4], a2 = coordinates[6],
                            cl = coordinates[7], cr = coordinates[9],
                            e2 = coordinates[10], e1 = coordinates[12], e0 = coordinates[13];
                        xmlPaths += $"    <polygon points=\"{a0},{a0} {a2},{a0} {a2},{a1} {a1},{a1} {a1},{a2} {a0},{a2} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{a0} {cr},{a0} {cr},{a1} {cl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e2},{a0} {e0},{a0} {e0},{a2} {e1},{a2} {e1},{a1} {e2},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{cl} {e0},{cl} {e0},{cr} {e1},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{e2},{e0},{e2} {e0},{e0} {e2},{e0} {e2},{e1} {e1},{e1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{e1} {cr},{e1} {cr},{e0} {cl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{e2} {a1},{e2} {a1},{e1} {a2},{e1} {a2},{e0} {a0},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{cl} {a1},{cl} {a1},{cr} {a0},{cr} \" {editColorName} />\r\n";
                    }
                    break;
                case 3:
                    {
                        int a0 = coordinates[4], a1 = coordinates[5], a2 = coordinates[6],
                            cl = coordinates[7], cr = coordinates[9],
                            e2 = coordinates[10], e1 = coordinates[11], e0 = coordinates[12];
                        xmlPaths += $"    <polygon points=\"{a0},{a0} {a2},{a0} {a2},{a1} {a1},{a1} {a1},{a2} {a0},{a2} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{a0} {cr},{a0} {cr},{a1} {cl},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e2},{a0} {e0},{a0} {e0},{a2} {e1},{a2} {e1},{a1} {e2},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{cl} {e0},{cl} {e0},{cr} {e1},{cr} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{e2},{e0},{e2} {e0},{e0} {e2},{e0} {e2},{e1} {e1},{e1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{cl},{e1} {cr},{e1} {cr},{e0} {cl},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{e2} {a1},{e2} {a1},{e1} {a2},{e1} {a2},{e0} {a0},{e0} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{cl} {a1},{cl} {a1},{cr} {a0},{cr} \" {editColorName} />\r\n";
                    }
                    break;
                case 4:
                    {
                        int a0 = coordinates[4], a1 = coordinates[5], a2 = coordinates[6],
                            e2 = coordinates[10], e1 = coordinates[11], e0 = coordinates[12];
                        xmlPaths += $"    <polygon points=\"{a0},{a0} {a2},{a0} {a2},{a1} {a1},{a1} {a1},{a2} {a0},{a2} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e2},{a0} {e0},{a0} {e0},{a2} {e1},{a2} {e1},{a1} {e2},{a1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{e1},{e2},{e0},{e2} {e0},{e0} {e2},{e0} {e2},{e1} {e1},{e1} \" {editColorName} />\r\n";
                        xmlPaths += $"    <polygon points=\"{a0},{e2} {a1},{e2} {a1},{e1} {a2},{e1} {a2},{e0} {a0},{e0} \" {editColorName} />\r\n";
                    }
                    break;
            }

            return xmlPaths;
        }
        /// <summary>
        /// Vrátí paths ve tvaru X, s daným okrajem <paramref name="de"/>, s danou šířkou <paramref name="d1"/> a <paramref name="d2"/>.
        /// </summary>
        /// <param name="coordinates">Fyzické souřadnice</param>
        /// <param name="subSize">Okraj ikony, číslo v rozsahu 0-4 včetně</param>
        /// <param name="editColorName"></param>
        /// <param name="thick"></param>
        /// <returns></returns>
        private static string _GetEditPartXCross(int[] coordinates, int subSize, string editColorName, int thick)
        {
            int th = (thick < 1 ? 1 : (thick > 4 ? 4 : thick));      // Tloušťka půl-linky v ose X a Y
            int bp = subSize + 1;                                    // Index souřadnice bodu uprostřed okrajové linie (mezi 4 a 5) na začátku
            int ep = 16 - subSize - 1;                               // Index souřadnice bodu uprostřed okrajové linie (mezi 2 a 1) na konci

            // Reálné souřadnice    Designové souřadnice pro představu, na 32px, pro thick = 2:
            int b1 = coordinates[bp];           //  4
            int b2 = coordinates[bp + th];      //  6
            int c4 = coordinates[8 - th];       // 14 = před středem
            int c5 = coordinates[8];            // 16 = střed
            int c6 = coordinates[8 + th];       // 18 = za středem
            int e1 = coordinates[ep];           // 28
            int e2 = coordinates[ep - th];      // 26
            /*   ILUSTRACE JEDNOTLIVÝCH BODŮ
                         4       2
                       bp/\      /\ep
                      5 /  \    /  \ 1
                        \   \3 /   /
                         \   \/   /
                        6 \      / 12
                          /      \
                         /   /\   \
                      7 /   /9 \   \ 11
                        \  /    \  /
                         \/      \/
                         8       10
            */
            // Začneme malovat kříž počínaje zprava nahoře, doleva nahoru, střed nahoře, doleva nahoru, doleva, střed vlevo, doleva dolů, dolů, střed dole, atd...
            string paths = $"    <polygon points=\"{e1},{b2} {e2},{b1} {c5},{c4} {b2},{b1} {b1},{b2} {c4},{c5} {b1},{e2} {b2},{e1} {c5},{c6} {e2},{e1} {e1},{e2} {c6},{c5} \" {editColorName} />\r\n";
            return paths;
        }
        /// <summary>
        /// Metoda vrátí definici paths pro ikonu dokumentu
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="subSize"></param>
        /// <param name="borderStyle"></param>
        /// <param name="fillStyle"></param>
        /// <param name="pl"></param>
        /// <param name="pt"></param>
        /// <param name="pr"></param>
        /// <param name="pb"></param>
        /// <param name="thick"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private static string _GetEditPartDocument(int[] coordinates, int subSize, string borderStyle, string fillStyle,
            int pl, int pt, int pr, int pb, int thick, int edge)
        {
            int th = (thick < 0 ? 0 : (thick > 4 ? 4 : thick));      // Šířka okraje 0 až 4 (logické koordináty): 0=bez výplně !
            int iw = 16 - (subSize + pl + th + th + pr + subSize);   // Šířka vnitřního prostoru pro vyobrazení dokumentu po odečtení okrajů, padding a thick
            if (iw <= 2) throw new ArgumentException($"Too small width for _GetEditPartDocument(): InnerWidth={iw} (subSize={subSize}, pl={pl}, pr={pr}, thick={th}).");
            int ih = 16 - (subSize + pt + th + th + pt + subSize);   // Výška vnitřního prostoru pro vyobrazení dokumentu po odečtení okrajů, padding a thick
            if (iw <= 2) throw new ArgumentException($"Too small height for _GetEditPartDocument(): InnerHeight={ih} (subSize={subSize}, pt={pt}, pb={pb}, thick={th}).");

            int m = coordinates[1];                                  // Modul (počet pixelů na jeden bod) = Průměr obloučku: pro ikonu 32px je průměr 2px, poloměr 1px; pro ikonu 16px je průměr 0.5px
            int innSize = subSize + th;                              // Subsize zvětšená o sílu okraje, použije se pro inner koordináty
            int x0 = coordinates[subSize + pl];                      // X vlevo vnější
            int x2 = coordinates[innSize + pl];                      // X vlevo vnitřní
            int x7 = coordinates[16 - innSize - pr];                 // X vpravo vnitřní
            int x9 = coordinates[16 - subSize - pr];                 // X vpravo vnější
            int y0 = coordinates[subSize + pt];                      // Y nahoře vnější
            int y2 = coordinates[innSize + pt];                      // Y nahoře vnitřní
            int y7 = coordinates[16 - innSize - pb];                 // Y dole vnitřní
            int y9 = coordinates[16 - subSize - pb];                 // Y dole vnější

            // Přípravy na kulaté rohy:
            string x1 = _GetCoordCenter(x0, x0 + m);                 // X vlevo kde začíná levý oblouk
            string x8 = _GetCoordCenter(x9, x9 - m);                 // X vpravo kde začíná pravý oblouk
            string y1 = _GetCoordCenter(y0, y0 + m);                 // Y nahoře kde začíná horní oblouk
            string y8 = _GetCoordCenter(y9, y9 - m);                 // Y dole kde začíná dolní oblouk
            _GetXmlQuadCurveR1R2(m, out string d4, out string d2);             // 1/4 a 1/2 průměru kružnice = kulatý roh
            string clh = _GetXmlQuadCurve(CurveDirections.LeftDown, d4, d2);   // levý horní roh, směr doleva a dolů, relativní definice
            string cld = _GetXmlQuadCurve(CurveDirections.DownRight, d4, d2);  // levý dolní roh, směr dolů a doprava, relativní definice  (použitelný zvenku vpravo dole i uvnitř nahoře vlevo v rožku)
            string cpd = _GetXmlQuadCurve(CurveDirections.RightUp, d4, d2);    // pravý dolní roh, směr doprava a nahoru, relativní definice
            string cph = _GetXmlQuadCurve(CurveDirections.UpLeft, d4, d2);     // pravý horní roh, směr nahoru a doprava, relativní definice

            string xmlPath = "";
            if (edge > 0 && iw >= 4 && ih >= 4)
            {   // S ohnutým pravým horním rohem:
                int maxEdge = (iw < ih ? iw : ih) / 2;               // Maximální velikost rohu je 1/2 z menšího z vnitřních rozměrů (iw, ih), stále měřeno v logických koordinátech
                int ed = (edge < maxEdge ? edge : maxEdge);          // Reálně použijeme menší hodnotu
                int x3 = coordinates[16 - subSize - pr - ed];        // X vpravo zvenku : konec horní vodorovné linky, odkud jde šikmo doprava dolů (pravý horní uříznutý roh)
                int y3 = coordinates[subSize + pt + ed];             // Y nahoře zvenku : začátek pravé svislé linky, odkud jde rovně dolů (pravý horní uříznutý roh)
                int x5 = coordinates[16 - innSize - pr - ed];        // X vpravo uvnitř : svislá linka růžku
                int y5 = coordinates[innSize + pt + ed];             // Y nahoře uvnitř : vodorovná linka růžku
                string x4 = _GetCoordCenter(x5, x5 + m);             // X vpravo od x5, kde začíná kulatý roh uvnitř
                string y4 = _GetCoordCenter(y5, y5 - m);             // Y nahoře nad y5, kde začíná kulatý roh uvnitř
                //    M19,4H7C6.4,4,6,4.4,6,5v22c0,0.6,0.4,1,1,1h18c0.6,0,1-0.4,1-1V11L19,4z
                string pathOut = $"M{x4},{y0}H{x1}{clh}V{y8}{cld}H{x8}{cpd}V{y4}L{x4},{y0}z ";   // $"M{x3},{y0}H{x1}{clh}V{y8}{cld}H{x8}{cpd}V{y3}L{x3},{y0}z ";
                if (th > 0)
                {   // S výplní:
                    //    M24,26H8V6h10v5c0,0.6,0.4,1,1,1h5  V26z
                    string pathInt = $"M{x7},{y7}H{x2}V{y2}H{x5}V{y4}{cld}H{x7}V{y7}z ";
                    xmlPath = $@"      <path d='{pathOut}{pathInt}' {borderStyle} />
      <path d='{pathInt}' {fillStyle} />
";
                }
                else
                {   // Bez výplně:
                    xmlPath = $@"      <path d='{pathOut}' {borderStyle} />
";
                }
            }
            else
            {   // Pouze pravidelný rámeček bez rožku:
                string pathOut = $"M{x8},{y0}H{x1}{clh}V{y8}{cld}H{x8}{cpd}V{y1}{cph}z ";
                if (th > 0)
                {   // S výplní:
                    string pathInt = $"M{x7},{y7}H{x2}V{y2}H{x7}V{y7}z ";
                    xmlPath = $@"      <path d='{pathOut}{pathInt}' {borderStyle} />
      <path d='{pathInt}' {fillStyle} />
";
                }
                else
                {   // Bez výplně:
                    xmlPath = $@"      <path d='{pathOut}' {borderStyle} />
";
                }
            }
            return xmlPath.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací souřadnici uprostřed mezi d1 a d2 jako validní string.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        private static string _GetCoordCenter(double d1, double d2)
        {
            double dc = (d1 + d2) / 2d;
            string c = dc.ToString("####0.0").Trim().Replace(",", ".");
            return c;
        }
        /// <summary>
        /// Konvertuje zadaný string na typ editační ikony
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static EditType _GetEditType(string text)
        {
            string key = (text ?? "U").Trim().ToUpper();
            switch (key)
            {   // Ceiling, Up, Down, Floor, Begin, Left, Right, End, Stop
                case "SELECTALL":
                case "SELECTALL1":
                case "ALL":
                case "ALL1":
                case "A":
                case "A1": return EditType.SelectAll1;

                case "SELECTALL2":
                case "ALL2":
                case "A2": return EditType.SelectAll2;

                case "SELECTALL3":
                case "ALL3":
                case "A3": return EditType.SelectAll3;

                case "DELETE":
                case "DELETE1":
                case "DEL":
                case "DEL1":
                case "D":
                case "D1": return EditType.Delete1;

                case "DELETE2":
                case "DEL2":
                case "D2": return EditType.Delete2;

                case "CTRL+C":
                case "CTRLC":
                case "COPY":
                case "C": return EditType.Copy;

                case "CTRL+X":
                case "CTRLX":
                case "CUT":
                case "X": return EditType.Cut;

                case "CTRL+V":
                case "CTRLV":
                case "PASTE":
                case "V": return EditType.Paste;

                case "UNDO":
                case "U": return EditType.Undo;

                case "REDO":
                case "R": return EditType.Redo;

            }

            return EditType.None;
        }
        /// <summary>
        /// Typy ikon pro Editaci
        /// </summary>
        private enum EditType { None, SelectAll1, SelectAll2, SelectAll3, Delete1, Delete2, Copy, Cut, Paste, Undo, Redo }
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
            return DxSvgImage.Create(caption, DxSvgImagePaletteType.LightSkin, xmlContent);
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
        private static bool _TryGetGenericSvgTextOnly(string imageName, string[] genericItems, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage)
        {
            bool isDarkTheme = DxComponent.IsDarkTheme;
            int p = 1;
            string text = _GetGenericParam(genericItems, p++, "");                         // Text, bude pouze Trimován
            string textParam = _GetGenericParam(genericItems, p++, "");                    // Barva písma (class, fill, nic)
            if (String.IsNullOrEmpty(textParam)) textParam = $"fill='{(isDarkTheme ? _GenericTextColorDarkSkinText : _GenericTextColorLightSkinText)}'";
            string fontFamily = _GetGenericParam(genericItems, p++, "");                   // Font: Default = bezpatkové písmo
            if (String.IsNullOrEmpty(fontFamily)) fontFamily = "sans-serif";
            bool isBold = (_GetGenericParam(genericItems, p++, "N").StartsWith("B", StringComparison.InvariantCultureIgnoreCase));     // Bold
            return _TryGetGenericSvgTextParts(imageName, text, sizeType, ref dxSvgImage, GenericTextParts.Text, textParam, fontFamily, isBold, "", "");
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
            return _TryGetGenericSvgTextParts(imageName, text, sizeType, ref dxSvgImage, GenericTextParts.All, textParam, fontFamily, isBold, borderParam, fillParam);
        }
        /// <summary>
        /// Z dodané definice a pro danou velikost vygeneruje SvgImage obsahující text.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="text"></param>
        /// <param name="sizeType"></param>
        /// <param name="dxSvgImage"></param>
        /// <param name="parts"></param>
        /// <param name="textParam"></param>
        /// <param name="fontFamily"></param>
        /// <param name="isBold"></param>
        /// <param name="borderParam"></param>
        /// <param name="fillParam"></param>
        /// <returns></returns>
        private static bool _TryGetGenericSvgTextParts(string imageName, string text, ResourceImageSizeType? sizeType, ref DxSvgImage dxSvgImage, GenericTextParts parts,
            string textParam, string fontFamily, bool isBold, string borderParam, string fillParam)
        {
            int size = (sizeType.HasValue && sizeType.Value == ResourceImageSizeType.Small ? 16 : 32);
            TextInfo textInfo = new TextInfo(text, size, fontFamily, isBold);

            string xmlHeader = _GetXmlContentHeader(size);
            string xmlStyles = _GetXmlDevExpressStyles();
            string xmlTextBegin = parts.HasFlag(GenericTextParts.Text) ? textInfo.GetXmlGroupBegin() : "";
            string xmlPathBorder = parts.HasFlag(GenericTextParts.Border) ? _GetXmlPathBorderSquare(size, isBold, borderParam) : "";
            string xmlPathFill = parts.HasFlag(GenericTextParts.Fill) ? _GetXmlPathFillSquare(size, isBold, fillParam) : "";
            string xmlTextText = parts.HasFlag(GenericTextParts.Text) ? textInfo.GetXmlGroupText(textParam) : "";
            string xmlFooter = _GetXmlContentFooter();

            string xmlContent = xmlHeader + xmlStyles + xmlTextBegin + xmlPathBorder + xmlPathFill + xmlTextText + xmlFooter;
            dxSvgImage = DxSvgImage.Create(text, DxSvgImagePaletteType.LightSkin, xmlContent);
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
        /// Částice ikony generickho textu
        /// </summary>
        [Flags]
        private enum GenericTextParts
        {
            None = 0,
            Text = 1,
            Border = 2,
            Fill = 4,
            All = Text | Border | Fill
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
                _ResolveSvgDesignParam(ref textParam, "class='Black'");
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
        /// Vrací XML text zahajující SVG image dané velikosti. Generuje záhlaví SVG a otevírá element G.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private static string _GetXmlContentHeader(int[] coordinates)
        {
            return _GetXmlContentHeader(coordinates[16]);
        }
        /// <summary>
        /// Vrací XML text zahajující SVG image dané velikosti. Generuje záhlaví SVG a otevírá element G.
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
        /// Obsahuje pouze kompletní tag STYLE, nezačíná tag G.
        /// </summary>
        /// <returns></returns>
        private static string _GetXmlDevExpressStyles()
        {
            string xml = @"    <style type='text/css'> .White{fill:#FFFFFF;} .Red{fill:#D11C1C;} .Green{fill:#039C23;} .Blue{fill:#1177D7;} .Yellow{fill:#FFB115;} .Black{fill:#727272;} .st0{opacity:0.75;} .st1{opacity:0.5;} </style>
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Metoda vrací pole obsahující 17 prvků, určujících klíčové souřadnice pro danou celkovou velikost <paramref name="sizeType"/>.
        /// Pole se používá pro určení souřadnic v rastru dané velikosti. Pro vstupní hodnotu 16 pole obsahuje čísla: 0,1,2,3,...,14,15,16.
        /// Pro hodnotu 32 obsahuje čísla 0,2,4,6,...,28,30,32. 
        /// Pro větší hodnoty obdobně. Pro vstupní hodnoty nezarovnané na dělitele 16 obsahuje nejbližší nižší násobky. 
        /// Pro vstupní hodnotu menší než 16 výstup odpovídá velikosti 16.
        /// Modulo (=1 krok) se nachází samozřejmě v prvku [1], protože v prvku [0] je 0.
        /// <para/>
        /// Pole slouží jako základna pro určení pozic v ikonách, 
        /// s tím že použitím pole místo konstant je implicitně řešeno zoomování ikony 
        /// i určení souřadnic pro nejmenší ikonu 16x16px.
        /// </summary>
        /// <param name="sizeType"></param>
        /// <returns></returns>
        private static int[] _GetCoordinates(ResourceImageSizeType? sizeType)
        {
            int size = (sizeType.HasValue && sizeType.Value == ResourceImageSizeType.Small ? 16 : 32);
            return _GetCoordinates(size);
        }
        /// <summary>
        /// Metoda vrací pole obsahující 17 prvků, určujících klíčové souřadnice pro danou celkovou velikost <paramref name="size"/>.
        /// Pole se používá pro určení souřadnic v rastru dané velikosti. Pro vstupní hodnotu 16 pole obsahuje čísla: 0,1,2,3,...,14,15,16.
        /// Pro hodnotu 32 obsahuje čísla 0,2,4,6,...,28,30,32. 
        /// Pro větší hodnoty obdobně. Pro vstupní hodnoty nezarovnané na dělitele 16 obsahuje nejbližší nižší násobky. 
        /// Pro vstupní hodnotu menší než 16 výstup odpovídá velikosti 16.
        /// Modulo (=1 krok) se nachází samozřejmě v prvku [1], protože v prvku [0] je 0.
        /// <para/>
        /// Pole slouží jako základna pro určení pozic v ikonách, 
        /// s tím že použitím pole místo konstant je implicitně řešeno zoomování ikony 
        /// i určení souřadnic pro nejmenší ikonu 16x16px.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static int[] _GetCoordinates(int size)
        {
            int[] result = new int[17];
            int modulo = (size < 16 ? 1 : size / 16);
            int coordinate = 0;
            for (int i = 0; i < 17; i++)
            {
                result[i] = coordinate;
                coordinate += modulo;
            }
            return result;
        }
        /// <summary>
        /// Vrátí element path, ve tvaru obdélníku (s kulatými rohy) vepsaného do dané velikosti (size), s okraji (padding) o síle okraje (isBold ? 2 : 1).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="isBold"></param>
        /// <param name="styleParam"></param>
        /// <param name="counterClockWise"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathBorderSquare(int size, bool isBold, string styleParam, bool counterClockWise = false, Padding? padding = null)
        {
            _ResolveSvgDesignParam(ref styleParam);
            if (size <= 0 || String.IsNullOrEmpty(styleParam)) return "";

            string pathData = _GetXmlPathDataBorderSquare(size, isBold, counterClockWise, padding);
            string xml = $@"      <path d='{pathData}' {styleParam} />
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
            _GetXmlQuadCurveR1R2(d, out var d4, out var d2);        // Deklarace křivky, polovina a čtvrtina radiusu "d"

            string xml, bx, by, tr, br, bl, tl;
            if (!counterClockWise)
            {   // Ve směru hodinových ručiček
                bx = _GetXmlNumber(p.Left, d, 2);          // Počátek rovné části vlevo úplně nahoře, X
                by = _GetXmlNumber(p.Top, 0, 1);           // Počátek rovné části vlevo úplně nahoře, Y
                tr = _GetXmlQuadCurve(CurveDirections.RightDown, d4, d2);
                br = _GetXmlQuadCurve(CurveDirections.DownLeft, d4, d2);
                bl = _GetXmlQuadCurve(CurveDirections.LeftUp, d4, d2);
                tl = _GetXmlQuadCurve(CurveDirections.UpRight, d4, d2);

                xml = $"M{bx},{by}h{w}{tr}v{h}{br}h-{w}{bl}v-{h}{tl}z " + _GetXmlPathDataFillSquare(size, isBold, true, padding);
            }
            else
            {   // V protisměru
                bx = _GetXmlNumber(p.Left, 0, 1);          // Počátek rovné části úplně vlevo nahoře, X
                by = _GetXmlNumber(p.Top, d, 2);           // Počátek rovné části úplně vlevo nahoře, Y
                bl = _GetXmlQuadCurve(CurveDirections.DownRight, d4, d2);
                br = _GetXmlQuadCurve(CurveDirections.RightUp, d4, d2);
                tr = _GetXmlQuadCurve(CurveDirections.UpLeft, d4, d2);
                tl = _GetXmlQuadCurve(CurveDirections.LeftDown, d4, d2);

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
        /// <param name="styleParam"></param>
        /// <param name="counterClockWise">Směr: false = po směru ručiček, true = proti směru</param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private static string _GetXmlPathFillSquare(int size, bool isBold, string styleParam, bool counterClockWise = false, Padding? padding = null)
        {
            _ResolveSvgDesignParam(ref styleParam);
            if (size <= 0 || String.IsNullOrEmpty(styleParam)) return "";

            string pathData = _GetXmlPathDataFillSquare(size, isBold, counterClockWise, padding);
            string xml = $@"      <path d='{pathData}' {styleParam} />
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

            string xml = _GetXmlPathDataRectangle(l, t, w, h, counterClockWise);

            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací kompletní element "path" pro rectangle dané velikosti s danými vlastnostmi, plný (bez vnitřního otvoru)
        /// </summary>
        /// <param name="l"></param>
        /// <param name="t"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="thick"></param>
        /// <param name="borderStyle"></param>
        /// <param name="fillStyle"></param>
        /// <returns></returns>
        private static string _GetXmlPathRectangleFill(int l, int t, int w, int h, int thick, string borderStyle, string fillStyle)
        {
            string xml = "";
            xml += _GetXmlPathRectangle(l, t, w, h, thick, borderStyle);
            if (thick > 0)
            {
                int t1 = thick;
                int t2 = 2 * thick;
                xml += _GetXmlPathRectangle(l + t1, t + t1, w - t2, h - t2, 0, fillStyle);
            }
            return xml;
        }
        /// <summary>
        /// Vrací kompletní element "path" pro rectangle dané velikosti s danými vlastnostmi, plný (bez vnitřního otvoru)
        /// </summary>
        /// <param name="l"></param>
        /// <param name="t"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="styleParam"></param>
        /// <returns></returns>
        private static string _GetXmlPathRectangle(int l, int t, int w, int h, string styleParam)
        {
            return _GetXmlPathRectangle(l, t, w, h, 0, styleParam);
        }
        /// <summary>
        /// Vrací kompletní element "path" pro rectangle dané velikosti s danými vlastnostmi, uprostřed prázdný = má vnitřní rectangle tak, aby měl danou šířku linie <paramref name="thick"/>
        /// </summary>
        /// <param name="l"></param>
        /// <param name="t"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="thick"></param>
        /// <param name="styleParam"></param>
        /// <returns></returns>
        private static string _GetXmlPathRectangle(int l, int t, int w, int h, int thick, string styleParam)
        {
            string pathData1 = _GetXmlPathDataRectangle(l, t, w, h, true);
            string pathData2 = "";
            if (thick > 0 && thick < w && thick < h)
            {
                int th1 = thick;
                int th2 = 2 * th1;
                pathData2 = _GetXmlPathDataRectangle(l + th1, t + th1, w - th2, h - th2, false);
            }
            string xml = $@"      <path d='{pathData1}{pathData2}' {styleParam} />
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrátí čistá data pro XML path pro rectangle na dané souřadnici (l,t) a velikosti (w,h), v daném směru.
        /// </summary>
        /// <param name="l"></param>
        /// <param name="t"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="counterClockWise"></param>
        /// <returns></returns>
        private static string _GetXmlPathDataRectangle(int l, int t, int w, int h, bool counterClockWise)
        {
            string xml = !counterClockWise ?
                $"M{l},{t}h{w}v{h}h-{w}v-{h}z " :          // Ve směru hodinových ručiček
                $"M{l},{t}v{h}h{w}v-{h}h-{w}z ";           // V protisměru
            return xml;
        }

        private static string _GetXmlPathDocumentInt(int l, int t, int w, int h, int thick, int corner, string styleParam)
        {
            string pathData = _GetXmlPathDataRectangle(l, t, w, h, true);
            string xml = $@"      <path d='{pathData}' {styleParam} />
";
            return xml.Replace("'", "\"");
        }
        /// <summary>
        /// Vrací čistá data pro Path reprezentující vnitřek dokumentu
        /// </summary>
        /// <returns></returns>
        private static string _GetXmlPathDataDocumentInt(int l, int t, int w, int h, int thick, int corner)
        {
            if (thick < 0) thick = 0;
            int th1 = thick;
            int th2 = 2 * th1;
            int x0 = l + th1;
            int x1 = l + w - th2;
            int y0 = t + th1;
            int y1 = t + h - th2;
            return "";
        }

        /// <summary>
        /// Vrátí souřadice pro definici křivky pro dané modulo = průměr kružnice.
        /// </summary>
        /// <param name="diameter">Průměr kružnice v pixelech</param>
        /// <param name="d4">Out 1/4 průměru = 1/2 poloměru oblouku</param>
        /// <param name="d2">Out 1/2 průměru = celý poloměr oblouku</param>
        /// <returns></returns>
        private static void _GetXmlQuadCurveR1R2(int diameter, out string d4, out string d2)
        {
            d4 = _GetXmlNumber(0, diameter, 4);            // Deklarace křivky, polovina radiusu : pro large je zde "0.5", pro small je zde "0.25"
            d2 = _GetXmlNumber(0, diameter, 2);            // Deklarace křivky, celý radius      : pro large je zde "1", pro small je zde "0.50"
        }
        /// <summary>
        /// Vrátí křivku ve tvaru čtvrtkruhu, relativně umístěnou, v daném směru <paramref name="directions"/>;
        /// pro dané modulo = průměr kružnice.
        /// </summary>
        /// <param name="directions"></param>
        /// <param name="diameter"></param>
        /// <returns></returns>
        private static string _GetXmlQuadCurve(CurveDirections directions, int diameter)
        {
            _GetXmlQuadCurveR1R2(diameter, out string d4, out string d2);
            return _GetXmlQuadCurve(directions, d4, d2);
        }
        /// <summary>
        /// Vrátí křivku ve tvaru čtvrtkruhu, relativně umístěnou, v daném směru <paramref name="directions"/>;
        /// kde parametr <paramref name="d4"/> určuje půlrádius a <paramref name="d2"/> určuje rádius.
        /// <para/>
        /// Čtvrtkruh vychází z aktuálního bodu ve směru první části směrníku <see cref="CurveDirections"/>, a otáčí se směrem k druhé části směrníku.
        /// Například směrník <see cref="CurveDirections.RightDown"/> jde nejprve doprava, a ohýbá se směrem dolů (jde o pravý horní roh čtverce ve směru hodinových ručiček).
        /// Na rozdíl od toho směrník <see cref="CurveDirections.DownRight"/> jde nejprve dolů, a pak zahýbá doprava (jde o levý dolní roh čtverce proti směru hodinových ručiček).
        /// <para/>
        /// Jako radiusy je třeba zadat text odpovídající počtu pixelů rohu, i jako desetiné číslo, bez znaménka mínus.
        /// Pokud tedy chceme vykreslit oblouk přes jeden pixel, předáme <paramref name="d4"/> = "0.5" a <paramref name="d2"/> = "1".
        /// </summary>
        /// <param name="directions"></param>
        /// <param name="d4">1/4 průměru = 1/2 poloměru oblouku</param>
        /// <param name="d2">1/2 průměru = celý poloměr oblouku</param>
        /// <returns></returns>
        private static string _GetXmlQuadCurve(CurveDirections directions, string d4, string d2)
        {
            switch (directions)
            {
                case CurveDirections.RightDown: return $"c{d4},0,{d2},{d4},{d2},{d2}";
                case CurveDirections.RightUp: return $"c{d4},0,{d2},-{d4},{d2},-{d2}";
                case CurveDirections.LeftDown: return $"c-{d4},0,-{d2},{d4},-{d2},{d2}";
                case CurveDirections.LeftUp: return $"c-{d4},0,-{d2},-{d4},-{d2},-{d2}";
                case CurveDirections.UpRight: return $"c0,-{d4},{d4},-{d2},{d2},-{d2}";
                case CurveDirections.UpLeft: return $"c0,-{d4},-{d4},-{d2},-{d2},-{d2}";
                case CurveDirections.DownRight: return $"c0,{d4},{d4},{d2},{d2},{d2}";
                case CurveDirections.DownLeft: return $"c0,{d4},-{d4},{d2},-{d2},{d2}";
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
        /// Vrací XML text ukončující SVG image: ukončuje element G a poté SVG.
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
        /// Vrátí daný parametr z pole parametrů, ošetří jej jako SVG styl
        /// </summary>
        /// <param name="genericItems"></param>
        /// <param name="index"></param>
        /// <param name="defaultStyle"></param>
        /// <returns></returns>
        private static string _GetGenericSvgDesignParam(string[] genericItems, int index, string defaultStyle)
        {
            string style = _GetGenericParam(genericItems, index, "");
            _ResolveSvgDesignParam(ref style, defaultStyle);
            return style;
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
        /// Ošetří dodaný parametr, definující SVG vzhled. 
        /// Vstup smí být prázdný, nebo může obsahovat kompletní definici vzhledu ("fill='#e02080' stroke='Black'")
        /// Pokud je prázdný, na výstupu je "".
        /// Pokud je zadán a neobsahuje rovnítko, předsadí class, rovnítko a obalí apostrofy:    class='param'.
        /// Jinak jej ponechá beze změny.
        /// <para/>
        /// Tedy: 
        /// - pokud na vstupu je param = null nebo prázdný string, pak na výstupu je v param = defValue (nebo prázdný string);
        /// - pokud na vstupu je param obsahující mimo jiné rovnítko, pak na výstupu je param beze změny (ponecháme explicitní deklaraci)
        /// - pokud tedy na vstupu je neprázdný text bez rovnítka, považuje se text za jméno třídy CSS stylu a pak na výstupu je param:   class='vstupní text param'
        /// (například: vstup = "blue", výstup = "class='blue'")
        /// </summary>
        /// <param name="param"></param>
        /// <param name="defValue"></param>
        private static void _ResolveSvgDesignParam(ref string param, string defValue = null)
        {
            if (String.IsNullOrEmpty(param))
                param = defValue ?? "";
            else if (param.IndexOf("=") < 0)
                param = "class='" + param + "'";
        }
        #endregion
        #endregion
    }
    /// <summary>
    /// Typ palety obrázku
    /// </summary>
    public enum DxSvgImagePaletteType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Světlý skin = tmavé kontury
        /// </summary>
        LightSkin,
        /// <summary>
        /// Světlý skin a Šedé barvy pro Disabled prvky
        /// </summary>
        LightSkinDisabled,
        /// <summary>
        /// Tmavý skin = světlé kontrury
        /// </summary>
        DarkSkin,
        /// <summary>
        /// Tmavý skin a Disabled prvek
        /// </summary>
        DarkSkinDisabled,
        /// <summary>
        /// Explicitní barvy, platí zákaz je měnit
        /// </summary>
        Explicit
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
        /// Inicializace pro zadanou velikost
        /// </summary>
        private void Initialize(ResourceImageSizeType sizeType)
        {
            _NameIdDict = new Dictionary<string, int>();
            SizeType = sizeType;
            ImageSize = DxComponent.GetDefaultImageSize(sizeType);         // Hodnotu ani po změně Zoomu neměníme...
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
            // Velikost po změně Zoomu neměníme:
            //  this.ImageSize = DxComponent.GetImageSize(SizeType, true);
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
            bool isDark = DxComponent.IsDarkTheme;
            for (int i = 0; i < count; i++)
            {   // Procházím to takhle dřevěně proto, že
                //  a) potřebuji index [i] pro setování modifikovaného objektu,
                //  b) a protože v foreach cyklu není dobré kolekci měnit
                if (this[i] is DxSvgImage dxSvgImage && dxSvgImage.Palette != DxSvgImagePaletteType.Explicit)
                {   // Pokud na dané pozici je DxSvgImage, který je LightDarkCustomizable,
                    //  pak si pro jeho jméno získám instanci zdroje (resourceItem) a tento zdroj mi vytvoří aktuálně platný SvgImage (Světlý nebo Tmavý, podle aktuálního = nového skinu):
                    if (DxApplicationResourceLibrary.TryGetResource(dxSvgImage.ImageName, true, out var resourceItem, out var _) && resourceItem != null && resourceItem.ContentType == ResourceContentType.Vector)
                        this[i] = resourceItem.CreateSvgImage(GetPalette(dxSvgImage.Palette, isDark));
                    else
                        this[i] = dxSvgImage.CreateClone();
                }
            }
        }
        /// <summary>
        /// Vrátí typ palety pro obrázek, který byl původně renderován v původní paletě <paramref name="oldPalette"/>, pro nově platný odstín skinu <paramref name="isDark"/>.
        /// </summary>
        /// <param name="oldPalette"></param>
        /// <param name="isDark"></param>
        /// <returns></returns>
        private static DxSvgImagePaletteType GetPalette(DxSvgImagePaletteType oldPalette, bool isDark)
        {
            switch (oldPalette)
            {
                case DxSvgImagePaletteType.LightSkin:
                case DxSvgImagePaletteType.DarkSkin:
                    return (isDark ? DxSvgImagePaletteType.DarkSkin : DxSvgImagePaletteType.LightSkin);
                case DxSvgImagePaletteType.LightSkinDisabled:
                case DxSvgImagePaletteType.DarkSkinDisabled:
                    return (isDark ? DxSvgImagePaletteType.DarkSkinDisabled : DxSvgImagePaletteType.LightSkinDisabled);
            }
            return oldPalette;
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
        /// <summary>
        /// Vrátí typ velikosti ikon v dané kolekci
        /// </summary>
        /// <param name="svgImages"></param>
        /// <returns></returns>
        public static ResourceImageSizeType GetSizeType(DevExpress.Utils.SvgImageCollection svgImages)
        {
            // Default:
            if (svgImages is null) return ResourceImageSizeType.Large;

            // Pokud na vstupu je naše zdejší třída, pak druh velikosti máme uložen v SizeType:
            if (svgImages is DxSvgImageCollection dxSvgImages) return dxSvgImages.SizeType;

            // Vyjdeme z pixelové velikosti SvgImageCollection.ImageSize:
            return DxComponent.GetImageSizeType(svgImages.ImageSize, ResourceImageSizeType.Small);
        }
        #endregion
    }
    #endregion
    #region SvgImageModifier : Třída pro úpravu obsahu SVG podle aktivního Skinu (Světlý / Tmavý) + palety Enabled / Disabled
    /// <summary>
    /// Třída modifikující barevnost dodaného SVG image pro danou cílovou paletu
    /// </summary>
    internal class SvgImageModifier
    {
        #region Public rozhraní
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageModifier()
        {
            _PalettesDict = new Dictionary<DxSvgImagePaletteType, Palette>();
        }
        /// <summary>
        /// Vrátí obsah SVG image konvertovaný do daného cílového odstínu
        /// </summary>
        /// <param name="content"></param>
        /// <param name="paletteType"></param>
        /// <param name="imageName"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public byte[] Convert(byte[] content, DxSvgImagePaletteType paletteType, string imageName = null, Size? targetSize = null)
        {
            string xmlTextInp = Encoding.UTF8.GetString(content);
            string xmlTextOut = ConvertXml(xmlTextInp, paletteType, imageName, targetSize);
            return Encoding.UTF8.GetBytes(xmlTextOut);
        }
        /// <summary>
        /// Vrátí obsah SVG image konvertovaný do daného cílového odstínu
        /// </summary>
        /// <param name="xmlTextInp"></param>
        /// <param name="paletteType"></param>
        /// <param name="imageName"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public string Convert(string xmlTextInp, DxSvgImagePaletteType paletteType, string imageName = null, Size? targetSize = null)
        {
            return ConvertXml(xmlTextInp, paletteType, imageName, targetSize);
        }
        #endregion
        #region Vlastní konverze
        /// <summary>
        /// Konvertuje XML text
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="paletteType"></param>
        /// <param name="imageName"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        private string ConvertXml(string xmlText, DxSvgImagePaletteType paletteType, string imageName, Size? targetSize)
        {
            var palette = GetPalette(paletteType);
            if (palette != null) xmlText = ConvertXmlColor(xmlText, paletteType, palette, imageName, targetSize);
            if (targetSize.HasValue) xmlText = ConvertXmlSize(xmlText, paletteType, palette, imageName, targetSize);
            return xmlText;
        }
        /// <summary>
        /// Zpracuje barvy
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="paletteType"></param>
        /// <param name="palette"></param>
        /// <param name="imageName"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        private string ConvertXmlColor(string xmlText, DxSvgImagePaletteType paletteType, Palette palette, string imageName, Size? targetSize)
        {
            if (!palette.ContainsColorChanges) return xmlText;

            if (palette.ChangeSpecificColor)
                xmlText = ConvertXmlColorSpecific(xmlText, palette, imageName);
            if (TextContainsAny(xmlText, "fill", "stroke") && TextContainsAny(xmlText, "path", "polygon", "rect", "circle", "polyline", "ellipse", "line"))
                xmlText = ConvertXmlColorNodes(xmlText, palette, imageName);

            return xmlText;
        }
        /// <summary>
        /// Zpracuje barvy specificky podle jména ikony
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="palette"></param>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private string ConvertXmlColorSpecific(string xmlText, Palette palette, string imageName)
        {
            if (TextContainsAny(imageName, "class-colour-10", "form-colour-10", "tag-filled-grey", "button-grey-filled"))
            {   // Šedivé ikony: světle šedým zruším obrys, bílým ikonám dám obrys středně šedý:  ( swap light and dark colours of class-colour, form-colour, tag and button-grey )
                // JD 0065426 26.05.2020; JD 0066902 20.11.2020; JD 0065749 26.06.2020
                // DAJ zde se měnila barva stroke="none", ale to zmenšuje ikonu o chybějící 1px čáry, proto dávám stroke=stejná barva jako fill
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeC8C6C4}\" stroke=\"{Palette.ColorCode383838}\"", $"fill=\"{palette[Palette.ColorCodeC8C6C4, ColorType.Fill]}\" stroke=\"{palette[Palette.ColorCodeC8C6C4, ColorType.Fill]}\""); //circle/rect
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeFFFFFF}\" stroke=\"{Palette.ColorCode383838}\"", $"fill=\"{palette[Palette.ColorCodeFFFFFF, ColorType.Fill]}\" stroke=\"{palette[Palette.ColorCode787878 /* as JD */, ColorType.Stroke]}\"");    // path
            }
            else if (TextContainsAny(imageName, "Rel1ExtDoc", "RelNExtDoc"))
            {   // Dynamické vztahy jsou žluté:
                // JD 0067697 19.02.2021 Ve formuláři nejsou označ.blokované DV
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeF7DA8E}\" stroke=\"{Palette.ColorCode383838}\"", $"fill=\"{palette[Palette.ColorCodeD4D4D4 /* as JD */, ColorType.Fill]}\" stroke=\"{palette[Palette.ColorCodeE57428 /* as JD */, ColorType.Stroke]}\"");    // rect
                xmlText = xmlText.Replace($"fill=\"none\" stroke=\"{Palette.ColorCode383838}\"", $"fill=\"none\" stroke=\"{palette[Palette.ColorCodeE57428 /* as JD */, ColorType.Stroke]}\""); //path
                xmlText = xmlText.Replace($"fill=\"none\" stroke=\"{Palette.ColorCodeE57428}\"", $"fill=\"none\" stroke=\"{palette[Palette.ColorCodeE57428 /* as JD */, ColorType.Stroke]}\""); //path
            }
            else
            {   // Ikona třídy pro přehled a pro formulář, anebo button-(barva)-filled, = věci založené jen na barvě:
                // JD 0065426 26.05.2020; JD 0066902 20.11.2020; JD 0067697 19.02.2021 Ve formuláři nejsou označ.blokované DV
                bool isSpecific = palette.IsDark && (
                                      TextContainsAny(imageName, "class-colour", "form-colour", "tag-filled", "DynRel")
                                   || TextContainsAll(imageName, "button", "filled"));
                if (isSpecific)
                {
                    // DAJ zde se měnila barva stroke="none", ale to zmenšuje ikonu o chybějící 1px čáry, proto dávám stroke=stejná barva jako fill
                    xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeF7DA8E}\" stroke=\"{Palette.ColorCodeE57428}\"", $"fill=\"{palette[Palette.ColorCodeF7DA8E, ColorType.Fill]}\" stroke=\"{palette[Palette.ColorCodeF7DA8E, ColorType.Fill]}\""); //path - žlutá je specifická
                    foreach (var pair in palette.LightDarkPairs)
                        // DAJ zde se měnila barva stroke="none", ale to zmenšuje ikonu o chybějící 1px čáry, proto dávám stroke=stejná barva jako fill
                        xmlText = xmlText.Replace($"fill=\"{pair.Key}\" stroke=\"{pair.Value}\"", $"fill=\"{pair.Key}\" stroke=\"{pair.Key}\""); // circle/rect
                }

                /*
                foreach (var lightDarkColor in palette.Pairs)
                {   // JD 0065749 26.06.2020
                    if (isSpecific)
                    {
                        xmlText = xmlText.Replace($"fill=\"{lightDarkColor.Key}\" stroke=\"{lightDarkColor.Value}\"", $"fill=\"{lightDarkColor.Key}\" stroke=\"none\""); //circle/rect
                    }
                    else
                    {
                        xmlText = xmlText.Replace($"fill=\"{lightDarkColor.Key}\"", $"fill=\"{lightDarkColor.Value}\"");
                        xmlText = xmlText.Replace($"stroke=\"{lightDarkColor.Value}\"", $"stroke=\"{lightDarkColor.Key}\"");
                    }
                }
                */

                //světle modrá -> světlejší modrá
                xmlText = xmlText.Replace($"fill=\"none\" stroke=\"{Palette.ColorCode228BCB}\"", $"fill=\"none\" stroke=\"{palette[Palette.ColorCode228BCB, ColorType.Stroke]}\""); //JD 0065749 22.07.2020
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeFFFFFF}\" stroke=\"{Palette.ColorCode228BCB}\"", $"fill=\"none\" stroke=\"{palette[Palette.ColorCode228BCB, ColorType.Stroke]}\""); //JD 0065749 22.07.2020
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCode228BCB}\"", $"fill=\"{palette[Palette.ColorCode228BCB, ColorType.Fill]}\"");        // JD 0065749 22.07.2020
                xmlText = xmlText.Replace($"stroke=\"{Palette.ColorCode228BCB}\"", $"stroke=\"{palette[Palette.ColorCode228BCB, ColorType.Stroke]}\"");     // JD 0065749 03.08.2020

                //tmavě zelená -> zelená
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCode0BA04A}\"", $"fill=\"{palette[Palette.ColorCode0BA04A, ColorType.Fill]}\"");        // JD 0065749 22.07.2020
                xmlText = xmlText.Replace($"stroke=\"{Palette.ColorCode0BA04A}\"", $"stroke=\"{palette[Palette.ColorCode0BA04A, ColorType.Stroke]}\"");     // JD 0065749 03.08.2020

                //bílá -> tmavě šedá
                xmlText = xmlText.Replace($"fill=\"{Palette.ColorCodeFFFFFF}\"", $"fill=\"{palette[Palette.ColorCodeFFFFFF, ColorType.Fill]}\"");

                //černá a tmavě šedá -> světle šedá
                xmlText = xmlText.Replace($"stroke=\"{Palette.ColorCode000000}\"", $"stroke=\"{palette[Palette.ColorCode000000, ColorType.Stroke]}\"");
                xmlText = xmlText.Replace($"stroke=\"{Palette.ColorCode383838}\"", $"stroke=\"{palette[Palette.ColorCode383838, ColorType.Stroke]}\"");
            }
            return xmlText;
        }
        /// <summary>
        /// Konvertuje velikosti pro target 24px
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="paletteType"></param>
        /// <param name="palette"></param>
        /// <param name="imageName"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        private string ConvertXmlSize(string xmlText, DxSvgImagePaletteType paletteType, Palette palette, string imageName, Size? targetSize)
        {
            bool isSize24 = (targetSize.HasValue && targetSize.Value.Width == 24 && targetSize.Value.Height == 24);
            if (!isSize24) return xmlText;

            if (imageName.Contains("form-colour") //JD 0066902 17.12.2020 Rozlišit záložky přehledů a formulářů
                || imageName.Contains("Rel1"))    //JD 0067697 12.02.2021 Ve formuláři nejsou označ.blokované DV - ikona se liší pouze barvami
            {
                xmlText = xmlText.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                xmlText = xmlText.Replace($"d=\"M10,9.5h12M10,12.5h12M10,15.5h12M10,18.5h12M10,21.5h8\"", $"d=\"M10,10.25h12M10,12.75h12M10,15.5h12M10,18.25h12M10,21h8\"");
            }
            else if (imageName.Contains("RelN")) //JD 0067697 12.02.2021 Ve formuláři nejsou označ.blokované DV
            {
                xmlText = xmlText.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                xmlText = xmlText.Replace($"d=\"M10.5,6.5h13v17M13.5,3.5h13v17\"", $"d=\"M10.5,7.25h12.75v16M13.5,4.5h12.5v16\"");
                xmlText = xmlText.Replace($"d=\"M10,13.5h8M10,16.5h8M10,19.5h8M10,22.5h6\"", $"d=\"M10,14.25h8M10,17h8M10,19.5h8M10,22.25h6\"");
            }
            else if (imageName.Contains("RelArch")) //JD 0067697 17.02.2021 Ve formuláři nejsou označ.blokované DV
            {
                xmlText = xmlText.Replace($"opacity=\"1\"", $"opacity=\"0.8\"");
                xmlText = xmlText.Replace($"d=\"M7.5,15.5H24\"", $"d=\"M7.5,15.25H24\"");
                xmlText = xmlText.Replace($"d=\"M12.5,8v3.5h7v-3.5M12.5,19v3.5h7v-3.5\"", $"d=\"M12.5,8v3.5h7v-3.5M12.5,18.75v3.5h7v-3.5\"");
            }

            return xmlText;
        }
        /// <summary>
        /// Zpracuje rekurzivně strukturu XML nodů a nahradí barvy
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="palette"></param>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private string ConvertXmlColorNodes(string xmlText, Palette palette, string imageName)
        {
            var contentXmlDoc = new XmlDocument();
            contentXmlDoc.LoadXml(xmlText);

            // Rekurzivní mapování prvků a jejich zpracování:
            foreach (XmlNode childNode in contentXmlDoc.ChildNodes) 
                _ProcessSvgNode(childNode, palette);

            using (var sw = new System.IO.StringWriter())
            using (var xw = new XmlTextWriter(sw))
            {
                contentXmlDoc.WriteTo(xw);
                xmlText = sw.ToString();
            }
            return xmlText;
        }
        /// <summary>
        /// Zpracuje obecné nody v SVG
        /// </summary>
        /// <param name="node"></param>
        /// <param name="palette"></param>
        private void _ProcessSvgNode(XmlNode node, Palette palette)
        {
            if (node.Name == "svg")
            {
                foreach (XmlNode childNode in node.ChildNodes)
                    if (childNode.Name == "g")
                        _ProcessGNode(childNode, palette);
            }
        }
        /// <summary>
        /// Zpracuje nody G (grupa): buď rekurzivně do vnořené grupy, nebo do garfického nodu <see cref="_ProcessGraphicNode(XmlNode, Palette)"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="palette"></param>
        private void _ProcessGNode(XmlNode node, Palette palette)
        {
            if (node.Name == "g")
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Name == "g")
                        _ProcessGNode(childNode, palette); //recursion
                    else
                        _ProcessGraphicNode(childNode, palette);
                }
            }
        }
        /// <summary>
        /// Zpracuje nody, které by mohly obsahovat grafiku
        /// </summary>
        /// <param name="node"></param>
        /// <param name="palette"></param>
        private void _ProcessGraphicNode(XmlNode node, Palette palette)
        {
            if (node.Name == "path" || node.Name == "polygon" || node.Name == "rect" || node.Name == "circle" || node.Name == "polyline" || node.Name == "ellipse" || node.Name == "line")
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    switch (attr.Name)
                    {
                        case "fill":
                            if (palette.ModifyGenericFill)
                                attr.Value = palette[attr.Value, ColorType.Fill];
                            break;
                        case "stroke":
                            if (palette.ModifyGenericStroke)
                                attr.Value = palette[attr.Value, ColorType.Stroke];
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud v daném textu <paramref name="text"/> bude nalezen některý ze zadaných textů.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="contains"></param>
        /// <returns></returns>
        private bool TextContainsAny(string text, params string[] contains)
        {
            return contains.Any(c => text.Contains(c));
        }
        /// <summary>
        /// Vrátí true, pokud v daném textu <paramref name="text"/> budou nalezeny všechny zadané texty.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="contains"></param>
        /// <returns></returns>
        private bool TextContainsAll(string text, params string[] contains)
        {
            return contains.All(c => text.Contains(c));
        }
        #endregion
        #region Správa konverzních palet
        /// <summary>
        /// Najde / vytvoří a vrátí paletu pro danou konverzi 
        /// </summary>
        /// <param name="paletteType"></param>
        /// <returns></returns>
        private Palette GetPalette(DxSvgImagePaletteType paletteType)
        {
            if (!_PalettesDict.TryGetValue(paletteType, out var palette))
            {
                palette = new Palette(paletteType);
                _PalettesDict.Add(paletteType, palette);
            }
            return palette;
        }
        /// <summary>
        /// Palety pro jednotlivé cílové odstíny.
        /// Key = typ konverze;
        /// Value = paleta;
        /// <para/>
        /// Jedna každá paleta: 
        /// Key = string barvy v deklaraci SvgImage;
        /// Value = string výsledné barvy ve výstupním SvgImage.
        /// <para/>
        /// Pokud pro klíč TargetType neexistuje paleta, bude ondemand vytvořena. 
        /// Získání palety z této Dictionary provádí metoda <see cref="GetPalette(DxSvgImagePaletteType)"/>.
        /// Tvorbu konkrétné palety provádí její konstruktor, třída <see cref="Palette"/>.
        /// Pokud paleta pro určitý klíč TargetType je null, nebude se provádět konverze barev; proběhne pouze konverze velikostí (podle TargetSize).
        /// </summary>
        private Dictionary<DxSvgImagePaletteType, Palette> _PalettesDict;
        /// <summary>
        /// Třída popisující jednu paletu barev Vstup - Výstup pro určitý typ barevné konverze
        /// </summary>
        private class Palette
        {
            #region Konstruktor a public použití
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="paletteType"></param>
            public Palette(DxSvgImagePaletteType paletteType)
            {
                this._PaletteType = paletteType;
                this.ForceColorChange = true;
                this.CreateValues();
                this.CreatePairs();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "PaletteType: " + this._PaletteType;
            }
            /// <summary>
            /// Typ palety
            /// </summary>
            private DxSvgImagePaletteType _PaletteType;
            /// <summary>
            /// Všechny barvy (nejen ty základní <see cref="ColorCodes"/>) se mají konvertovat do šedé barvy (Disabled).
            /// Tedy vstupující barvy, které nejsou základní, se mají ondemand konvertovat do šedé!
            /// </summary>
            internal bool AllColorsToGray { get; private set; }
            /// <summary>
            /// Index barev
            /// </summary>
            private Dictionary<string, Colors> _ColorsDict;
            /// <summary>
            /// Páry barev: Světlá - Tmavá
            /// </summary>
            private KeyValuePair<string, string>[] _ColorPairs;
            /// <summary>
            /// Typ palety
            /// </summary>
            internal DxSvgImagePaletteType PaletteType { get { return _PaletteType; } }
            /// <summary>
            /// Jde o paletu pro Dark colors
            /// </summary>
            internal bool IsDark { get; private set; }
            /// <summary>
            /// Jde o paletu pro Disabled colors
            /// </summary>
            internal bool IsDisabled { get; private set; }
            /// <summary>
            /// Je povinné provádět změny barev, i když předdefinovaná paleta je prázdná?
            /// </summary>
            private bool ForceColorChange { get; set; }
            /// <summary>
            /// Obsahuje true u palety, která obsahuje změny barev = pro takovou paletu je třeba provádět konverzi barev
            /// </summary>
            internal bool ContainsColorChanges { get { return (ForceColorChange || AllColorsToGray || _ColorsDict.Count > 0); } }
            /// <summary>
            /// Obsahuje true u palety, která má provést změnu specifickou = podle jména ikony
            /// </summary>
            internal bool ChangeSpecificColor { get; private set; }
            /// <summary>
            /// Obsahuje true u palety, která má modifikovat <b>generické atributy</b> FILL (tj. ne-specifické)
            /// </summary>
            internal bool ModifyGenericFill { get; private set; }
            /// <summary>
            /// Obsahuje true u palety, která má modifikovat <b>generické atributy</b> STROKE (tj. ne-specifické)
            /// </summary>
            internal bool ModifyGenericStroke { get; private set; }
            /// <summary>
            /// Páry barev: Světlá - Tmavá
            /// </summary>
            internal KeyValuePair<string, string>[] LightDarkPairs { get { return _ColorPairs; } }
            /// <summary>
            /// Pole párů hodnot Původní / Nová hodnota, obě v notaci XML = "#RRGGBB".
            /// Item1 = původní hodnota; Item2 = nová hodnota Fill, Item3 = nová hodnota Stroke
            /// </summary>
            internal Tuple<string, string, string>[] Colors { get { return _ColorsDict.Select(kvp => new Tuple<string, string, string>(kvp.Key, kvp.Value.GetColor(ColorType.Fill), kvp.Value.GetColor(ColorType.Stroke))).ToArray(); } }
            /// <summary>
            /// Obsahuje cílovou barvu pro danou vstupní (definiční) barvu.
            /// </summary>
            /// <param name="current">Klíč aktuální barvy v notaci XML = "#RRGGBB"</param>
            /// <param name="type">Použít konverzi pro danou cílovou barvu</param>
            /// <returns></returns>
            internal string this[string current, ColorType type] { get { return GetTargetValue(current, type); } }
            #endregion
            #region Vytvoření jednotlivých převodních palet pro typ palety
            /// <summary>
            /// Vytvoří konverzní hodnoty pro konkrétní typ palety
            /// </summary>
            private void CreateValues()
            {
                this._ColorsDict = new Dictionary<string, Colors>(StringComparer.InvariantCultureIgnoreCase);
                switch (this._PaletteType)
                {
                    case DxSvgImagePaletteType.LightSkin:
                        this.ModifyGenericFill = false;
                        this.ModifyGenericStroke = false;
                        this.ChangeSpecificColor = false;
                        this.AllColorsToGray = false;
                        this.IsDark = false;
                        this.IsDisabled = false;
                        // AddColors(ColorCodeFFFFFF, "#FEFEFE");        // Konvertujeme výchozí barvu #FFFFFF na barvu lehce jinou: DevExpress kreslí barvu #FFFFFF jako transparentní, ale o číslo jinou kreslí jako bílou!
                        break;
                    case DxSvgImagePaletteType.DarkSkin:
                        this.ModifyGenericFill = true;
                        this.ModifyGenericStroke = true;
                        this.ChangeSpecificColor = true;
                        this.AllColorsToGray = false;
                        this.IsDark = true;
                        this.IsDisabled = false;
                        //        Vstupní barva - Nová pro Fill - Nová pro Stroke (obrys) (nepovinná, nezadaná = shodná jako Fill):
                        AddColors(ColorCodeFFFFFF, "#383837");
                        AddColors(ColorCodeF7DA8E, "#F7D52D");
                        AddColors(ColorCodeF7CDA7, "#E57427");
                        AddColors(ColorCodeF7D52C, "#F7DA8F", "#F7DA8F");
                        AddColors(ColorCodeF3B8B8, "#E42D2B");
                     // AddColors(ColorCodeE57428, "#F7DA8D", "#F7CDA8"); ???
                        AddColors(ColorCodeE57428, "#E57428", "#F7CDA8");      // Pokud je použita E57428 jako FILL, pak se nezmění. Pokud je STROKE, převede se na F7CDA8.
                        AddColors(ColorCodeE42D2C, "#F3B8B9", "#F3B8B8");
                        AddColors(ColorCodeDFBCD9, "#A0519E");
                        AddColors(ColorCodeDDAE85, "#9B5434");
                        AddColors(ColorCodeD4D4D4, "#383837");
                        AddColors(ColorCodeC8C6C4, "#787877");
                        AddColors(ColorCodeBEE2E5, "#21B4C8");
                        AddColors(ColorCodeACD8B1, "#0BA049");
                        AddColors(ColorCodeA0519F, "#DFBCD8", "#DFBCD8");
                        AddColors(ColorCode9B5435, "#DDAE85", "#DDAE85");
                        AddColors(ColorCode92CBEE, "#0964B1");
                        AddColors(ColorCode787878, "#C8C6C3");
                        AddColors(ColorCode383838, "#D4D4D3");
                        AddColors(ColorCode228BCB, "#92CBEE");
                        AddColors(ColorCode21B4C9, "#BEE2E6", "#BEE2E6");
                        AddColors(ColorCode17AB4F, "#0BA049");
                        AddColors(ColorCode0BA04A, "#17AB4E", "#ACD8B1");
                        AddColors(ColorCode0964B0, "#92CBEE", "#92CBEE");
                        AddColors(ColorCode000000, "#D4D4D3");
                        break;
                    case DxSvgImagePaletteType.LightSkinDisabled:
                        this.ModifyGenericFill = true;
                        this.ModifyGenericStroke = true;
                        this.ChangeSpecificColor = false;
                        this.AllColorsToGray = true;
                        this.IsDark = false;
                        this.IsDisabled = true;
                        foreach (var colorCode in ColorCodes)
                            AddColors(colorCode, GetGrayValue(colorCode));
                        break;
                    case DxSvgImagePaletteType.DarkSkinDisabled:
                        this.ModifyGenericFill = true;
                        this.ModifyGenericStroke = true;
                        this.ChangeSpecificColor = true;
                        this.AllColorsToGray = true;
                        this.IsDark = true;
                        this.IsDisabled = true;
                        foreach (var colorCode in ColorCodes)
                            AddColors(colorCode, GetGrayValue(colorCode));
                        break;
                }
            }
            /// <summary>
            /// Vytvoří pole párů barev
            /// </summary>
            private void CreatePairs()
            {
                var pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>(ColorCodeC8C6C4, ColorCode787878));    // class-colour-10 Šedá - #383838 změníme na #787878
                pairs.Add(new KeyValuePair<string, string>(ColorCodeF3B8B8, ColorCodeE42D2C));    // class-colour-20 Červená
                pairs.Add(new KeyValuePair<string, string>(ColorCodeF7CDA7, ColorCodeE57428));    // class-colour-30 Oranžová
                pairs.Add(new KeyValuePair<string, string>(ColorCodeF7DA8E, ColorCodeF7D52C));    // class-colour-40 Žlutá - dark je stejná jako u oranžové #E57428, změníme ji na #F7D52C
                pairs.Add(new KeyValuePair<string, string>(ColorCodeACD8B1, ColorCode0BA04A));    // class-colour-50 Zelená
                pairs.Add(new KeyValuePair<string, string>(ColorCodeBEE2E5, ColorCode21B4C9));    // class-colour-60 Tyrkysová
                pairs.Add(new KeyValuePair<string, string>(ColorCode92CBEE, ColorCode0964B0));    // class-colour-70 Modrá
                pairs.Add(new KeyValuePair<string, string>(ColorCodeDFBCD9, ColorCodeA0519F));    // class-colour-80 Fialová
                pairs.Add(new KeyValuePair<string, string>(ColorCodeDDAE85, ColorCode9B5435));    // class-colour-90 Hnědá

                _ColorPairs = pairs.ToArray();
            }
            /// <summary>
            /// Přidá barevný pár Vstup - (Výstup Fill + Výstup Stroke)
            /// </summary>
            /// <param name="current"></param>
            /// <param name="targetFill"></param>
            /// <param name="targetStroke"></param>
            private void AddColors(string current, string targetFill, string targetStroke = null)
            {
                if (!String.IsNullOrEmpty(current))
                {
                    if (_ColorsDict.ContainsKey(current))
                        throw new ArgumentException($"Palette initialization error, duplicite color key: {current} for new target {targetFill}, contains old target = {(_ColorsDict[current])}.");
                    // Do Dictionary nesmím vložit Value, která je rovna nějakému existujícímu klíči Key.
                    //  Proč: protože konverze barev probíhá někdy ve více vlnách, a pokud bych na místo barvy AAA dal barvu BBB, pak v další vlně se bude hledat barva BBB a najde se CCC,
                    //   pokud bych v Dictionary měl Key AAA => Value BBB; a Key BBB => Value CCC.
                    //  Proto nyní, když mám vložit Value = BBB, a už existuje jiný klíč BBB, tak vložím value BBC = o jedna jinou barvu.
                    //  Vizuálně se to nepozná, ale nespustí to cyklickou změnu.
                    // Mohu ale použít takovou Value, která je rovna svému vlastnímu klíči (jde o barvu, která se nemění),
                    //  taková kombinace nezpůsobí cyklické změny barvy AAA => BBB => CCC, protože zůstává beze změny EEE => EEE
                    if (_ColorsDict.ContainsKey(targetFill))
                        targetFill = SearchUniqueValue(targetFill);
                    if (targetStroke != null && _ColorsDict.ContainsKey(targetStroke))
                        targetStroke = SearchUniqueValue(targetStroke);
                    _ColorsDict.Add(current, new Colors(targetFill, targetStroke));
                }
            }
            /// <summary>
            /// Najde a vrátí těsně sousedící barvu (ve formě Xml) s barvou danou, která ale dosud není klíčem v <see cref="_ColorsDict"/>.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private string SearchUniqueValue(string value)
            {
                if (!TryParseColor(value, out var color)) return value;
                var modifiers = "112;121;122;212;221;222;110;101;100;102;120;010;011;211;210;202;222;000".Split(';');
                foreach (var modifier in modifiers)
                {
                    string test = Convertor.ColorToXmlString(GetModifiedColor(color, modifier));
                    if (!_ColorsDict.ContainsKey(test)) return test;
                }
                return value;
            }
            /// <summary>
            /// Vrací danou barvu modifikovanou daným číslem.
            /// Modifikátor musí být string délky tři znaky, kde každý znak smí být pouze "0", "1", "2".
            /// <paramref name="modifier"/> tedy může být typicky "001" nebo "121" nebo "201" atd.
            /// Hodnoty modifikátoru se přičtou/odečtou od složky barvy tak, aby modifikovaná složka byla v rozsahu 0-255, a vrací se upravená barva.
            /// Výsledná barva se tedy může od vstupní barvy lišit nanejvýše o 2 v každé složce.
            /// </summary>
            /// <param name="color"></param>
            /// <param name="modifier"></param>
            /// <returns></returns>
            private static Color GetModifiedColor(Color color, string modifier)
            {
                int r = GetModifiedColorPart(color.R, modifier[0]);
                int g = GetModifiedColorPart(color.G, modifier[1]);
                int b = GetModifiedColorPart(color.B, modifier[2]);
                return Color.FromArgb(r, g, b);
            }
            /// <summary>
            /// Vrací složku barvy modifikovanou daným číslem
            /// </summary>
            /// <param name="value"></param>
            /// <param name="diff"></param>
            /// <returns></returns>
            private static int GetModifiedColorPart(int value, char diff)
            {
                int d = (int)diff - 48;                     // Ze znaku char '0' udělá int 0; ze znaku '2' udělá 2...
                if (value < 5) return value + d;            // Pokud vstupní hodnota složky je malá, pak modifikátor přičteme (+0 až +2)
                if (value < 250) return value - 1 + d;      // Pokud vstupní hodnota složky je přiměřená, použijeme rozsah (-1 až +1)
                return value - 2 + d;                       // Pokud vstupní hodnota složky je malá, pak modifikátor odečteme (-2 až 0)
            }
            /// <summary>
            /// Vrátí barvu cílovou pro danou barvu vstupní v rámci aktuální palety.
            /// Tato metoda vrací cílovou barvu bez ohledu na hodnotu <see cref="ModifyGenericFill"/> nebo <see cref="ModifyGenericStroke"/>,
            /// protože tuto metodu volá i změna Specifická.
            /// Metody, které řeší Generické změny, si mají testovat uvedené přepínače a podle nich volat/nevolat změnu barvy.
            /// </summary>
            /// <param name="current">Klíč aktuální barvy</param>
            /// <param name="type">Vrátit barvu pro: false = fill / true = stroke</param>
            /// <returns></returns>
            private string GetTargetValue(string current, ColorType type)
            {
                if (String.IsNullOrEmpty(current)) return current;
                current = current.Trim();
                if (String.Equals(current, "none", StringComparison.OrdinalIgnoreCase)) return current;

                if (_ColorsDict.TryGetValue(current, out var target)) return target.GetColor(type);     // Známe cílovou barvu
                if (!this.AllColorsToGray) return current;                                              // Není režim OnlyGray: vrátíme vstupní barvu

                // Režim OnlyGray a dosud neznámá barva? Danou barvu odbarvíme, upravíme pro Dark skin a přidáme do Dictionary:
                string result = GetGrayValue(current);
                AddColors(current, result);
                return result;
            }
            /// <summary>
            /// Metoda vrátí kód šedé (Disabled) barvy k dané barvě.
            /// </summary>
            /// <param name="current"></param>
            /// <returns></returns>
            private string GetGrayValue(string current)
            {
                if (!TryParseColor(current, out var color)) return current;
                // Následující řádek provádí Morphování barvy zadané směrem k barvě cílové, v daném poměru.
                // Morphování = lineární aproximace dané hodnoty (color) k cílové hodnotě.
                // Cílová hodnota pro světlý skin je tmavší a pro tmavý skin je světlejší (kontrastní)
                Color other = (!this.IsDark ? Color.FromArgb(192, 192, 192) : Color.FromArgb(64, 64, 64));
                // Pokud tedy na vstupu je barva R = 40, cílová R = 192 (pro tmavý skin), a morph = 0.5d, pak výsledná barva R = 40 + 50% vzdálenosti k cíli,
                //   tedy:  40.Morph(192, 0.5) =  40 + 0.5 * (192 -  40) = 116;
                //   nebo: 220.Morph(192, 0.5) = 220 + 0.5 * (192 - 220) = 206;
                color = color.Morph(other, 0.5f);
                color = color.GrayScale();
                return Convertor.ColorToXmlString(color);
            }
            /// <summary>
            /// Z dodaného stringu "#D044AA" určí odpovídající barvu a vrátí true.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="color"></param>
            /// <returns></returns>
            private bool TryParseColor(string text, out Color color)
            {
                color = Convertor.XmlStringToColor(text);
                return !color.IsEmpty;
            }
            #endregion
            #region Konstanty - hodnoty barev používané v definici SVG
            /// <summary>
            /// Souhrn všech vstupních explicitních barev
            /// </summary>
            private static string[] ColorCodes 
            {
                get 
                {
                    return new string[]
                    {
                        ColorCodeFFFFFF,
                        ColorCodeF7DA8E,
                        ColorCodeF7CDA7,
                        ColorCodeF7D52C,
                        ColorCodeF3B8B8,
                        ColorCodeE57428,
                        ColorCodeE42D2C,
                        ColorCodeDFBCD9,
                        ColorCodeDDAE85,
                        ColorCodeD4D4D4,
                        ColorCodeC8C6C4,
                        ColorCodeBEE2E5,
                        ColorCodeACD8B1,
                        ColorCodeA0519F,
                        ColorCode9B5435,
                        ColorCode92CBEE,
                        ColorCode787878,
                        ColorCode383838,
                        ColorCode228BCB,
                        ColorCode21B4C9,
                        ColorCode17AB4F,
                        ColorCode0BA04A,
                        ColorCode0964B0,
                        ColorCode000000
                    };
                }
            }
            internal const string ColorCodeFFFFFF = "#FFFFFF";
            internal const string ColorCodeF7DA8E = "#F7DA8E";
            internal const string ColorCodeF7CDA7 = "#F7CDA7";
            internal const string ColorCodeF7D52C = "#F7D52C";
            internal const string ColorCodeF3B8B8 = "#F3B8B8";
            internal const string ColorCodeE57428 = "#E57428";
            internal const string ColorCodeE42D2C = "#E42D2C";
            internal const string ColorCodeDFBCD9 = "#DFBCD9";
            internal const string ColorCodeDDAE85 = "#DDAE85";
            internal const string ColorCodeD4D4D4 = "#D4D4D4";
            internal const string ColorCodeC8C6C4 = "#C8C6C4";
            internal const string ColorCodeBEE2E5 = "#BEE2E5";
            internal const string ColorCodeACD8B1 = "#ACD8B1";
            internal const string ColorCodeA0519F = "#A0519F";
            internal const string ColorCode9B5435 = "#9B5435";
            internal const string ColorCode92CBEE = "#92CBEE";
            internal const string ColorCode787878 = "#787878";
            internal const string ColorCode383838 = "#383838";
            internal const string ColorCode228BCB = "#228BCB";
            internal const string ColorCode21B4C9 = "#21B4C9";
            internal const string ColorCode17AB4F = "#17AB4F";
            internal const string ColorCode0BA04A = "#0BA04A";
            internal const string ColorCode0964B0 = "#0964B0";
            internal const string ColorCode000000 = "#000000";
            #endregion
        }
        #region class Colors
        /// <summary>
        /// Třída pro jednu zdrojovou barvu a jednu-dvě barvy cílové (fill + stroke)
        /// </summary>
        private class Colors
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="targetFill"></param>
            /// <param name="targetStroke"></param>
            internal Colors(string targetFill, string targetStroke)
            {
                TargetFill = targetFill;
                TargetStroke = targetStroke;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Fill:{TargetFill}; Stroke:{TargetStroke}";
            }
            /// <summary>
            /// Cílová barva FILL
            /// </summary>
            internal readonly string TargetFill;
            /// <summary>
            /// Cílová barva STROKE
            /// </summary>
            internal readonly string TargetStroke;
            /// <summary>
            /// Vrátí odpovídající barvu
            /// </summary>
            /// <param name="type">Vrátit barvu pro: false = fill / true = stroke</param>
            /// <returns></returns>
            internal string GetColor(ColorType type)
            {
                string color = (type == ColorType.Stroke && TargetStroke != null) ? TargetStroke : TargetFill;
                return color;
            }
        }
        private enum ColorType { Fill, Stroke }
        #endregion
        #endregion
    }
    /// <summary>
    /// SvgImageModify : Třída pro úpravu obsahu SVG podle aktivního Skinu
    /// </summary>
    internal class SvgImageCustomize
    {

        // POZOR, už se nepoužívá !!!

        // Používá se třída SvgImageModifier.


        #region Colours constans
        private const string DarkColorCodeTransparent = "#00FFFFFF";    //RMC 0070095 10.12.2021 Barvy ikon v QuickAccessToolbaru
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
            return DxSvgImage.Create(imageName, DxSvgImagePaletteType.DarkSkin, darkContent);
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
            return DxSvgImage.Create(name, DxSvgImagePaletteType.Explicit, xml); ;
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
 