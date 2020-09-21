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
    /// FontInfo : Popisovač fontu. 
    /// Obsahuje všechna data pro fyzické vytvoření fontu, ale na rozdíl od <see cref="System.Drawing.Font"/> jsou tato data editovatelná.
    /// Dále obsahuje property <see cref="FontInfo.Font"/>, která vždy rycle vrátí reálný font, odpovídající aktuálním datům, který navíc pochází z cache (nesmí se Disposovat).
    /// </summary>
    public class FontInfo
    {
        #region Font properties
        /// <summary>
        /// Override GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = ((this.FontType.GetHashCode() << 24)
                  ^ (this.RelativeSize.GetHashCode() << 8)
                  ^ (this.Bold ? 4 : 0)
                  ^ (this.Italic ? 2 : 0)
                  ^ (this.Underline ? 1 : 0));
            if (this.FontFamilyName != null) hashCode = hashCode ^ this.FontFamilyName.GetHashCode();
            if (this.FontEmSize != null) hashCode = hashCode ^ this.FontEmSize.Value.GetHashCode();
            return hashCode;
        }
        /// <summary>
        /// Override Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            FontInfo other = obj as FontInfo;
            if (other == null) return false;
            return ((String.CompareOrdinal(this.FontFamilyName, other.FontFamilyName) == 0)
                 && ((this.FontEmSize ?? 0f) == (other.FontEmSize ?? 0f))
                 && (this.FontType == other.FontType)
                 && (this.RelativeSize == other.RelativeSize)
                 && (this.Bold == other.Bold)
                 && (this.Italic == other.Italic)
                 && (this.Underline == other.Underline));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
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
        /// Jméno fontu, default = null.
        /// Pokud je zadáno jméno fontu, je třeba zadat i jeho velikost <see cref="FontEmSize"/> (jinak se použije konstanta 9).
        /// Pokud se vloží null nebo empty, bude se používat <see cref="FontType"/>.
        /// </summary>
        public string FontFamilyName { get { return this._FontFamilyName; } set { this._FontFamilyName = (String.IsNullOrEmpty(value) ? null : value.Trim()); } }
        private string _FontFamilyName = null;
        /// <summary>
        /// Absolutní velikost fontu v EM size, default = null.
        /// Pokud je zadána, použije se namísto <see cref="RelativeSize"/>.
        /// Povolené hodnoty jsou 4 až 48.
        /// </summary>
        public float? FontEmSize { get { return this._FontEmSize; } set { this._FontEmSize = (value.HasValue ? (value.Value < 4f ? 4f : (value.Value > 48f ? 48f : value)) : value); } }
        private float? _FontEmSize = null;
        /// <summary>
        /// Typ fontu
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
        /// DefaultFont with small size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo DefaultSmall { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo Default { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with normal size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo DefaultBold { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// DefaultFont with big size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo DefaultBoldBig { get { return new FontInfo() { FontType = FontSetType.DefaultFont, RelativeSize = SizeBig, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// DialogFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo Dialog { get { return new FontInfo() { FontType = FontSetType.DialogFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with small size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo MenuSmall { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo Menu { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// MenuFont with normal size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo MenuBold { get { return new FontInfo() { FontType = FontSetType.MenuFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with small size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo CaptionSmall { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeSmall, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with small size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo CaptionSmallBold { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeSmall, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo Caption { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with normal size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo CaptionBold { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// CaptionFont with big size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo CaptionBoldBig { get { return new FontInfo() { FontType = FontSetType.CaptionFont, RelativeSize = SizeBig, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// IconTitleFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo IconTitle { get { return new FontInfo() { FontType = FontSetType.IconTitleFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// IconTitleFont with normal size, Bold. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo IconTitleBold { get { return new FontInfo() { FontType = FontSetType.IconTitleFont, RelativeSize = SizeStandard, Bold = true, Italic = false, Underline = false }; } }
        /// <summary>
        /// MessageBoxFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo MessageBox { get { return new FontInfo() { FontType = FontSetType.MessageBoxFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// SmallCaptionFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo SmallCaption { get { return new FontInfo() { FontType = FontSetType.SmallCaptionFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// StatusFont with normal size. Allways return new instance <see cref="FontInfo"/>.
        /// </summary>
        public static FontInfo Status { get { return new FontInfo() { FontType = FontSetType.StatusFont, RelativeSize = SizeStandard, Bold = false, Italic = false, Underline = false }; } }
        /// <summary>
        /// Konstanta pro Zoom pro menší písma
        /// </summary>
        protected const int SizeSmall = 85;
        /// <summary>
        /// Konstanta pro Zoom pro standardní písma
        /// </summary>
        protected const int SizeStandard = 100;
        /// <summary>
        /// Konstanta pro Zoom pro větší písma
        /// </summary>
        protected const int SizeBig = 115;
        #endregion
        #region Modifikovaný font = vytvořený z this, ale s úpravami z modifikátoru FontDeltaInfo
        /// <summary>
        /// Vygeneruje <see cref="FontInfo"/> na základě this údajů, modifikovaných danými daty.
        /// Pokud dodaný modifikátor <paramref name="modifier"/> je null nebo Empty, pak výstupem této metody je přímo this instance, ani ne Clone (kvůli rychlosti)!
        /// </summary>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public FontInfo GetModifiedFont(FontModifierInfo modifier)
        {
            if (modifier == null || modifier.IsEmpty) return this;

            FontInfo clone = this.Clone;
            if (modifier.FontFamilyName != null) clone.FontFamilyName = modifier.FontFamilyName;   // FontModifierInfo.FontFamilyName: null = beze změny, kdežto prázdný string se přenese do FontInfo.FontFamilyName a tím zruší explicitní jméno fontu.
            if (modifier.FontEmSize.HasValue) clone.FontEmSize = modifier.FontEmSize;
            if (modifier.FontType.HasValue) clone.FontType = modifier.FontType.Value;
            if (modifier.RelativeSize.HasValue) clone.RelativeSize = (clone.RelativeSize * modifier.RelativeSize.Value) / 100;
            if (modifier.Bold.HasValue) clone.Bold = modifier.Bold.Value;
            if (modifier.Italic.HasValue) clone.Italic = modifier.Italic.Value;
            if (modifier.Underline.HasValue) clone.Underline = modifier.Underline.Value;
            return clone;
        }
        #endregion
        #region Get font from cache; Create new font
        /// <summary>
        /// Fyzický font odpovídající aktuálním datům.
        /// Tento objekt se NESMÍ používat v using patternu, je to opakovaně použitelný objekt (získaný z interní cache).
        /// Pokud po získání hodnoty <see cref="Font"/> dojde ke změně hodnot v this objektu, 
        /// pak nové čtení hodnoty <see cref="Font"/> vrátí fyzicky jinou instanci, které bude mít patřičné hodnoty.
        /// </summary>
        public Font Font { get { return _GetFontFromCache(this); } }
        /// <summary>
        /// Metoda vygeneruje fyzický font odpovídající aktuálním datům.
        /// Tento objekt se MUSÍ používat v using patternu, je to new instance vytvořená výhradně pro účel této metody (nejde o objekt z cache).
        /// </summary>
        /// <returns></returns>
        public Font CreateNewFont()
        {
            FontStyle fontStyle =
                (this.Bold ? FontStyle.Bold : FontStyle.Regular) |
                (this.Italic ? FontStyle.Italic : FontStyle.Regular) |
                (this.Underline ? FontStyle.Underline : FontStyle.Regular);
            float emSize;

            if (this.FontFamilyName != null)
            {
                emSize = Application.App.Zoom.Value * (this.FontEmSize ?? 9f);
                return new Font(this.FontFamilyName, emSize, fontStyle);
            }

            Font prototype = GetFontPrototype(this.FontType);
            emSize = prototype.Size * _ToRange(Application.App.Zoom.Value * ((float)this.RelativeSize / 100f));
            return new Font(prototype.FontFamily, emSize, fontStyle);
        }
        /// <summary>
        /// Vrátí prototypový systémový font
        /// </summary>
        /// <param name="fontType"></param>
        /// <returns></returns>
        public static Font GetFontPrototype(FontSetType fontType)
        {
            switch (fontType)
            {
                case FontSetType.DefaultFont:
                    return SystemFonts.DefaultFont;
                case FontSetType.DialogFont:
                    return SystemFonts.DialogFont;
                case FontSetType.MenuFont:
                    return SystemFonts.MenuFont;
                case FontSetType.CaptionFont:
                    return SystemFonts.CaptionFont;
                case FontSetType.IconTitleFont:
                    return SystemFonts.IconTitleFont;
                case FontSetType.MessageBoxFont:
                    return SystemFonts.MessageBoxFont;
                case FontSetType.SmallCaptionFont:
                    return SystemFonts.SmallCaptionFont;
                case FontSetType.StatusFont:
                    return SystemFonts.StatusFont;
                default:
                    return SystemFonts.DefaultFont;
            }
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
    /// FontModifierInfo : modifikátor fontu
    /// </summary>
    public class FontModifierInfo
    {
        #region Konstrukce
        /// <summary>
        /// Obsahuje vždy new instanci, prázdnou
        /// </summary>
        public static FontModifierInfo Empty { get { return new FontModifierInfo(); } }
        /// <summary>
        /// Obsahuje true u instance, která neobsahuje žádnou změnu fontu
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (FontFamilyName != null) return false;            // Pouze NULL je Empty! Naproti tomu prázdný string vyjadřuje konkrétní hodnotu, která modifikuje FontInfo!
                if (FontEmSize.HasValue) return false;
                if (FontType.HasValue) return false;
                if (RelativeSize.HasValue) return false;
                if (Bold.HasValue) return false;
                if (Italic.HasValue) return false;
                if (Underline.HasValue) return false;
                return true;
            }
        }
        #endregion
        #region Font properties
        /// <summary>
        /// Jméno fontu, default = null.
        /// Pokud je zadáno jméno fontu, je třeba zadat i jeho velikost <see cref="FontEmSize"/> (jinak se použije konstanta 9).
        /// Zde (v modifikátoru) je možno zadat prázdný string, ten pak zajistí, že reálný <see cref="FontInfo"/> bude mít vynulovaný svůj <see cref="FontInfo.FontFamilyName"/>, 
        /// tedy pro konkrétní font se použije jeho <see cref="FontInfo.FontType"/>!
        /// Na rozdíl od toho pokud <see cref="FontModifierInfo.FontFamilyName"/> bude null, pak se <see cref="FontInfo.FontFamilyName"/> nebude měnit.
        /// </summary>
        public string FontFamilyName { get; set; }
        /// <summary>
        /// Absolutní velikost fontu v EM size, default = null.
        /// </summary>
        public float? FontEmSize { get; set; }
        /// <summary>
        /// Typ fontu
        /// </summary>
        public FontSetType? FontType { get; set; }
        /// <summary>
        /// Relative size to standard, in percent.
        /// </summary>
        public int? RelativeSize { get; set; }
        /// <summary>
        /// Is Bold?
        /// </summary>
        public bool? Bold { get; set; }
        /// <summary>
        /// Is Italic?
        /// </summary>
        public bool? Italic { get; set; }
        /// <summary>
        /// Is Underlined?
        /// </summary>
        public bool? Underline { get; set; }
        #endregion
    }
    #region enum FontSetType
    /// <summary>
    /// Type of font
    /// </summary>
    public enum FontSetType
    {
        /// <summary>Konkrétní typ písma</summary>
        DefaultFont = 0,
        /// <summary>Konkrétní typ písma</summary>
        DialogFont = 1,
        /// <summary>Konkrétní typ písma</summary>
        MenuFont = 2,
        /// <summary>Konkrétní typ písma</summary>
        CaptionFont = 3,
        /// <summary>Konkrétní typ písma</summary>
        IconTitleFont = 4,
        /// <summary>Konkrétní typ písma</summary>
        MessageBoxFont = 5,
        /// <summary>Konkrétní typ písma</summary>
        SmallCaptionFont = 6,
        /// <summary>Konkrétní typ písma</summary>
        StatusFont = 7,
        /// <summary>Konkrétní typ písma</summary>
        ExplicitFont = 8
    }
    #endregion
}
