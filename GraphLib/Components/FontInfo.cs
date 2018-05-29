using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// FontInfo : Font descriptor
    /// </summary>
    public class FontInfo
    {
        #region Font properties
        public override int GetHashCode()
        {
            return ((this.FontType.GetHashCode() << 24)
                  ^ (this.RelativeSize.GetHashCode() << 8)
                  ^ (this.Bold ? 4 : 0)
                  ^ (this.Italic ? 2 : 0)
                  ^ (this.Underline ? 1 : 0));
        }
        public override bool Equals(object obj)
        {
            FontInfo other = obj as FontInfo;
            if (other == null) return false;
            return ((this.FontType == other.FontType)
                 && (this.RelativeSize == other.RelativeSize)
                 && (this.Bold == other.Bold)
                 && (this.Italic == other.Italic)
                 && (this.Underline == other.Underline));
        }
        public override string ToString()
        {
            return this.FontType.ToString()
                + "; Size: " + this.RelativeSize.ToString()
                + (this.Bold ? "; Bold" : "")
                + (this.Italic ? "; Italic" : "")
                + (this.Underline ? "; Underline" : "")
                + ((!this.Bold && !this.Italic && !this.Underline) ? "; Normal" : "");
        }
        /// <summary>
        /// Type of basic font
        /// </summary>
        public FontSetType FontType { get; set; }
        /// <summary>
        /// Relative size to standard, in percent.
        /// Default = 100 = 100%.
        /// Accept values 20 to 500 (this is: 1/5 ÷ 5x size).
        /// </summary>
        public int RelativeSize { get { return this._RelativeSize; } set { this._RelativeSize = (value < 20 ? 20 : (value > 500 ? 500 : value)); } } private int _RelativeSize = SizeStandard;
        /// <summary>
        /// Is Bold?
        /// </summary>
        public bool Bold { get; set; }
        /// <summary>
        /// Is Italic?
        /// </summary>
        public bool Italic { get; set; }
        /// <summary>
        /// Is Underlined?
        /// </summary>
        public bool Underline { get; set; }
        /// <summary>
        /// Clone of this FontInfo
        /// </summary>
        public FontInfo Clone { get { return (FontInfo)this.MemberwiseClone(); } }
        /// <summary>
        /// Returns new instance of this font, with applied zoom.
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public FontInfo GetZoom(float zoom)
        {
            FontInfo clone = this.Clone;
            clone.ApplyZoom(zoom);
            return clone;
        }
        /// <summary>
        /// Zoom this RelativeSize with zoom value.
        /// Values of zero and negative are ignored.
        /// </summary>
        /// <param name="zoom"></param>
        public void ApplyZoom(float zoom)
        {
            if (zoom > 0f)
                this.RelativeSize = (int)Math.Round((decimal)this.RelativeSize * (decimal)zoom, 0);
        }
        #endregion
        #region Predefined fonts
        /// <summary>
        /// DefaultFont with small size. Allways return new instance.
        /// </summary>
        public static FontInfo DefaultSmall { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo Default { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with normal size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo DefaultBold { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with big size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo DefaultBoldBig { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeBig, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// DialogFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo Dialog { get { return new FontInfo() { FontType = FontSetType.DialogFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with small size. Allways return new instance.
        /// </summary>
        public static FontInfo MenuSmall { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo Menu { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with normal size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo MenuBold { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with small size. Allways return new instance.
        /// </summary>
        public static FontInfo CaptionSmall { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with small size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo CaptionSmallBold { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeSmall, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo Caption { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with normal size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo CaptionBold { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with big size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo CaptionBoldBig { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeBig, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// IconTitleFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo IconTitle { get { return new FontInfo() { FontType = FontSetType.IconTitleFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// IconTitleFont with normal size, Bold. Allways return new instance.
        /// </summary>
        public static FontInfo IconTitleBold { get { return new FontInfo() { FontType = FontSetType.IconTitleFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// MessageBoxFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo MessageBox { get { return new FontInfo() { FontType = FontSetType.MessageBoxFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// SmallCaptionFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo SmallCaption { get { return new FontInfo() { FontType = FontSetType.SmallCaptionFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// StatusFont with normal size. Allways return new instance.
        /// </summary>
        public static FontInfo Status { get { return new FontInfo() { FontType = FontSetType.StatusFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        protected const int SizeSmall = 85;
        protected const int SizeStandard = 100;
        protected const int SizeBig = 115;
        #endregion
        #region Get font from cache; Create new font
        /// <summary>
        /// Real font. 
        /// Return re-usable font from cache.
        /// Do not use as Dispose object (in using { ... } pattern), font is re-usable!!!
        /// </summary>
        public Font Font { get { return _GetFontFromCache(this); } }
        /// <summary>
        /// Create new font by this descriptor.
        /// Result must be used in using { } pattern. This is not font from cache!!!
        /// </summary>
        /// <returns></returns>
        public Font CreateNewFont()
        {
            Font prototype = null;
            switch (this.FontType)
            {
                case FontSetType.DefaultFont:
                    prototype = SystemFonts.DefaultFont;
                    break;
                case FontSetType.DialogFont:
                    prototype = SystemFonts.DialogFont;
                    break;
                case FontSetType.MenuFont:
                    prototype = SystemFonts.MenuFont;
                    break;
                case FontSetType.CaptionFont:
                    prototype = SystemFonts.CaptionFont;
                    break;
                case FontSetType.IconTitleFont:
                    prototype = SystemFonts.IconTitleFont;
                    break;
                case FontSetType.MessageBoxFont:
                    prototype = SystemFonts.MessageBoxFont;
                    break;
                case FontSetType.SmallCaptionFont:
                    prototype = SystemFonts.SmallCaptionFont;
                    break;
                case FontSetType.StatusFont:
                    prototype = SystemFonts.StatusFont;
                    break;
                default:
                    prototype = SystemFonts.DefaultFont;
                    break;
            }

            float ratio = _ToRange(Application.App.Zoom.Value * ((float)this.RelativeSize / 100f));   // Relative size (in percent) * Zoom (ratio) = Font Ratio to prototype size
            float emSize = ratio * prototype.Size;
            FontStyle fontStyle =
                (this.Bold ? FontStyle.Bold : FontStyle.Regular) |
                (this.Italic ? FontStyle.Italic : FontStyle.Regular) |
                (this.Underline ? FontStyle.Underline : FontStyle.Regular);

            return new Font(prototype.FontFamily, emSize, fontStyle);          // This must be only one row in whole aplication, where new Font() is called !!!
        }
        private static float _ToRange(float value)
        {
            return _ToRange(value, 0.2f, 5.0f);
        }
        private static float _ToRange(float value, float minimum, float maximum)
        {
            return (value < minimum ? minimum : (value > maximum ? maximum : value));
        }
        #endregion
        #region Singleton cache
        /// <summary>
        /// Return Font for specified descriptor, from font-cache.
        /// </summary>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        private static Font _GetFontFromCache(FontInfo fontInfo)
        {
            if (fontInfo == null) return null;

            Font font;
            Dictionary<FontInfo, Font> fontDict = FontDict;
            if (!fontDict.TryGetValue(fontInfo, out font))
            {
                lock (__FontLock)
                {
                    if (!fontDict.TryGetValue(fontInfo, out font))
                    {
                        font = fontInfo.CreateNewFont();
                        fontDict.Add(fontInfo, font);
                    }
                }
            }
            return font;
        }
        /// <summary>
        /// Reset fonts cache
        /// </summary>
        public static void ResetFonts()
        {
            Dictionary<FontInfo, Font> fontDict = __FontDict;
            if (fontDict == null) return;

            lock (__FontLock)
            {
                foreach (Font font in fontDict.Values)
                {
                    if (font != null)
                        font.Dispose();
                }

                __FontDict.Clear();
            }
        }
        /// <summary>
        /// Singleton instance
        /// </summary>
        protected static Dictionary<FontInfo, Font> FontDict
        {
            get
            {
                if (__FontDict == null)
                {
                    lock (__FontLock)
                    {
                        if (__FontDict == null)
                            __FontDict = new Dictionary<FontInfo, Font>();
                    }
                }
                return __FontDict;
            }
        }
        /// <summary>
        /// Instance
        /// </summary>
        private static Dictionary<FontInfo, Font> __FontDict = new Dictionary<FontInfo, Font>();
        /// <summary>
        /// Lock for instance (create, insert new item, reset)
        /// </summary>
        private static object __FontLock = new object();
        #endregion
    }
    /// <summary>
    /// Type of font
    /// </summary>
    public enum FontSetType
    {
        DefaultFont,
        DialogFont,
        MenuFont,
        CaptionFont,
        IconTitleFont,
        MessageBoxFont,
        SmallCaptionFont,
        StatusFont,
        ExplicitFont
    }
}
