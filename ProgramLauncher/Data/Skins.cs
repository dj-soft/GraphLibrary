using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DjSoft.Tools.ProgramLauncher.App;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region class AppearanceSet
    /// <summary>
    /// Sada definující jeden druh vzhledu ("skin")
    /// </summary>
    public class AppearanceSet
    {
        #region Public properties
        /// <summary>
        /// Barva statického pozadí pod všemi prvky = celé okno
        /// </summary>
        public Color WorkspaceColor { get { return __WorkspaceColor; } set { if (!__IsReadOnly) __WorkspaceColor = value; } } private Color __WorkspaceColor;
        /// <summary>
        /// Barvy aktivního prostoru. Nepoužívá se pro stav Enabled a Disabled, pouze MouseOn a MouseDown.
        /// </summary>
        public ColorSet ActiveContentColor { get { return __ActiveContentColor; } set { if (!__IsReadOnly) __ActiveContentColor = value; } } private ColorSet __ActiveContentColor;
        /// <summary>
        /// Sada barev pro čáru Border, kreslí se když <see cref="BorderWidth"/> je kladné
        /// </summary>
        public ColorSet BorderLineColors { get { return __BorderLineColors; } set { if (!__IsReadOnly) __BorderLineColors = value; } } private ColorSet __BorderLineColors;
        /// <summary>
        /// Sada barev pro pozadí pod buttonem, ohraničený prostorem Border
        /// </summary>
        public ColorSet ButtonBackColors { get { return __ButtonBackColors; } set { if (!__IsReadOnly) __ButtonBackColors = value; } } private ColorSet __ButtonBackColors;
        /// <summary>
        /// Sada barev pro MainTitle
        /// </summary>
        public ColorSet MainTitleColors { get { return __MainTitleColors; } set { if (!__IsReadOnly) __MainTitleColors = value; } } private ColorSet __MainTitleColors;
        /// <summary>
        /// Sada barev pro SubTitle
        /// </summary>
        public ColorSet SubTitleColors { get { return __SubTitleColors; } set { if (!__IsReadOnly) __SubTitleColors = value; } } private ColorSet __SubTitleColors;
        /// <summary>
        /// Sada barev pro běžné texty
        /// </summary>
        public ColorSet StandardTextColors { get { return __TextStandardColors; } set { if (!__IsReadOnly) __TextStandardColors = value; } } private ColorSet __TextStandardColors;

        /// <summary>
        /// Vzhled velkého titulku
        /// </summary>
        public TextAppearance MainTitleAppearance { get { return __MainTitleAppearance; } set { if (!__IsReadOnly) __MainTitleAppearance = value; } } private TextAppearance __MainTitleAppearance;
        /// <summary>
        /// Vzhled pod-titulku
        /// </summary>
        public TextAppearance SubTitleAppearance { get { return __SubTitleAppearance; } set { if (!__IsReadOnly) __SubTitleAppearance = value; } } private TextAppearance __SubTitleAppearance;
        /// <summary>
        /// Vzhled standardního textu
        /// </summary>
        public TextAppearance StandardTextAppearance { get { return __StandardTextAppearance; } set { if (!__IsReadOnly) __StandardTextAppearance = value; } } private TextAppearance __StandardTextAppearance;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
        #endregion
        #region Dynamické získání ColorSet a TextAppearance podle PaletteColorPartType a AppearanceTextPartType
        /// <summary>
        /// Vrátí barevnou sadu daného typu
        /// </summary>
        public ColorSet GetColorSet(AppearanceColorPartType part)
        {
            switch (part)
            {
                case AppearanceColorPartType.ContentColor: return ActiveContentColor;
                case AppearanceColorPartType.BorderLineColors: return BorderLineColors;
                case AppearanceColorPartType.ButtonBackColors: return ButtonBackColors;
                case AppearanceColorPartType.MainTitleColors: return MainTitleColors;
                case AppearanceColorPartType.SubTitleColors: return SubTitleColors;
                case AppearanceColorPartType.StandardTextColors: return StandardTextColors;
            }
            return ActiveContentColor;
        }
        /// <summary>
        /// Vrátí definici vzhledu textu daného typu
        /// </summary>
        public TextAppearance GetTextAppearance(AppearanceTextPartType part)
        {
            switch (part)
            {
                case AppearanceTextPartType.MainTitle: return MainTitleAppearance;
                case AppearanceTextPartType.SubTitle: return SubTitleAppearance;
                case AppearanceTextPartType.StandardText: return StandardTextAppearance;
            }
            return StandardTextAppearance; ;
        }
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Defaultní barevné schema
        /// </summary>
        public static AppearanceSet Default
        {
            get
            {
                if (__Default is null)
                {
                    var paletteSet = new AppearanceSet();
                    paletteSet.__WorkspaceColor = Color.FromArgb(64, 68, 72);

                    int a0 = 40;
                    int a1 = 80;
                    int a2 = 120;
                    int a3 = 160;

                    int b1 = 180;
                    int b2 = 200;

                    paletteSet.__ActiveContentColor = new ColorSet(true, null,
                        activeColor: Color.FromArgb(255, 240, 240, 190),
                        mouseOnColor: Color.FromArgb(a0, 200, 200, 230),
                        mouseDownColor: Color.FromArgb(a0, 180, 180, 210));
                    paletteSet.__BorderLineColors = new ColorSet(true,
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2));
                    paletteSet.__ButtonBackColors = new ColorSet(true,
                        Color.FromArgb(a1, 120, 120, 120),
                        Color.FromArgb(a1, 216, 216, 216),
                        Color.FromArgb(a1, 216, 216, 216),
                        Color.FromArgb(a2, 200, 200, 230),
                        Color.FromArgb(a2, 180, 180, 210),
                        Color.FromArgb(a3, 180, 180, 240));
                    paletteSet.__MainTitleColors = new ColorSet(true, Color.Black);
                    paletteSet.__SubTitleColors = new ColorSet(true, Color.Black);
                    paletteSet.__TextStandardColors = new ColorSet(true, Color.Black);

                    paletteSet.__MainTitleAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.2f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.3f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                        );

                    paletteSet.__SubTitleAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.1f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.2f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                        );

                    paletteSet.__StandardTextAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.0f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.1f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                        );

                    paletteSet.__IsReadOnly = true;
                    __Default = paletteSet;
                }
                return __Default;
            }
        }
        private static AppearanceSet __Default;
        /// <summary>
        /// Tmavomodrý svět
        /// </summary>
        public static AppearanceSet DarkBlue
        {
            get
            {
                if (__DarkBlue is null)
                {
                    var paletteSet = new AppearanceSet();
                    paletteSet.__WorkspaceColor = Color.FromArgb(64, 68, 72);

                    int a0 = 40;
                    int a1 = 80;
                    int a2 = 120;
                    int a3 = 160;

                    int b1 = 16;
                    int b2 = 32;

                    paletteSet.__ActiveContentColor = new ColorSet(true, null,
                        activeColor: Color.FromArgb(255, 48, 48, 96),
                        mouseOnColor: Color.FromArgb(a0, 32, 32, 48),
                        mouseDownColor: Color.FromArgb(a0, 40, 40, 64));
                    paletteSet.__BorderLineColors = new ColorSet(true,
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2));
                    paletteSet.__ButtonBackColors = new ColorSet(true,
                        Color.FromArgb(a1, 24, 24, 24),
                        Color.FromArgb(a1, 0, 0, 32),
                        Color.FromArgb(a1, 48, 48, 96),
                        Color.FromArgb(a2, 32, 32, 64),
                        Color.FromArgb(a2, 40, 40, 72),
                        Color.FromArgb(a3, 40, 40, 96));
                    paletteSet.__MainTitleColors = new ColorSet(true, Color.White);
                    paletteSet.__SubTitleColors = new ColorSet(true, Color.White);
                    paletteSet.__TextStandardColors = new ColorSet(true, Color.White);

                    paletteSet.__MainTitleAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.2f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.3f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.3f, FontStyle.Bold)
                        );

                    paletteSet.__SubTitleAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.1f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.2f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.2f, FontStyle.Bold)
                        );

                    paletteSet.__StandardTextAppearance = new TextAppearance(true,
                        FontType.CaptionFont,
                        ContentAlignment.MiddleLeft,
                        AppearanceColorPartType.MainTitleColors,
                        null,
                        new TextInteractiveStyle(true, InteractiveState.Default, null, 1.0f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseOn, null, 1.1f, null),
                        new TextInteractiveStyle(true, InteractiveState.MouseDown, null, 1.1f, FontStyle.Bold)
                        );

                    paletteSet.__IsReadOnly = true;
                    __DarkBlue = paletteSet;
                }
                return __DarkBlue;
            }
        }
        private static AppearanceSet __DarkBlue;
        #endregion
    }
    /// <summary>
    /// Část barevné definice v paletě
    /// </summary>
    public enum AppearanceColorPartType
    {
        None,
        ContentColor,
        BorderLineColors,
        ButtonBackColors,
        MainTitleColors,
        SubTitleColors,
        StandardTextColors
    }
    /// <summary>
    /// Část definice vzhledu textu v paletě
    /// </summary>
    public enum AppearanceTextPartType
    {
        None,
        MainTitle,
        SubTitle,
        StandardText
    }
    #endregion
    #region class ColorSet
    /// <summary>
    /// Definice barev pro jednu oblast, liší se interaktivitou
    /// </summary>
    public class ColorSet
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ColorSet() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="allColors"></param>
        /// <param name="disabledColor"></param>
        /// <param name="enabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(Color? allColors, Color? disabledColor = null, Color? enabledColor = null, Color? activeColor = null, Color? mouseOnColor = null, Color? mouseDownColor = null, Color? mouseHighlightColor = null)
        {
            this.__DisabledColor = disabledColor ?? allColors;
            this.__EnabledColor = enabledColor ?? allColors;
            this.__ActiveColor = activeColor ?? allColors;
            this.__MouseOnColor = mouseOnColor ?? allColors;
            this.__MouseDownColor = mouseDownColor ?? allColors;
            this.__MouseHighlightColor = mouseHighlightColor ?? allColors;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="disabledColor"></param>
        /// <param name="enabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(Color? disabledColor, Color? enabledColor, Color? activeColor, Color? mouseOnColor, Color? mouseDownColor, Color? mouseHighlightColor)
        {
            this.__DisabledColor = disabledColor;
            this.__EnabledColor = enabledColor;
            this.__ActiveColor = activeColor;
            this.__MouseOnColor = mouseOnColor;
            this.__MouseDownColor = mouseDownColor;
            this.__MouseHighlightColor = mouseHighlightColor;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="allColors"></param>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(bool isReadOnly, Color? allColors, Color? disabledColor = null, Color? enabledColor = null, Color? activeColor = null, Color? mouseOnColor = null, Color? mouseDownColor = null, Color? mouseHighlightColor = null)
        {
            this.__DisabledColor = disabledColor ?? allColors;
            this.__EnabledColor = enabledColor ?? allColors;
            this.__ActiveColor = activeColor ?? allColors;
            this.__MouseOnColor = mouseOnColor ?? allColors;
            this.__MouseDownColor = mouseDownColor ?? allColors;
            this.__MouseHighlightColor = mouseHighlightColor ?? allColors;
            this.__IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(bool isReadOnly, Color? disabledColor, Color? enabledColor, Color? activeColor, Color? mouseOnColor, Color? mouseDownColor, Color? mouseHighlightColor)
        {
            this.__DisabledColor = disabledColor;
            this.__EnabledColor = enabledColor;
            this.__ActiveColor = activeColor;
            this.__MouseOnColor = mouseOnColor;
            this.__MouseDownColor = mouseDownColor;
            this.__MouseHighlightColor = mouseHighlightColor;
            this.__IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Barva ve stavu Disabled = nedostupné
        /// </summary>
        public Color? DisabledColor { get { return __DisabledColor; } set { if (!__IsReadOnly) __DisabledColor = value; } } private Color? __DisabledColor;
        /// <summary>
        /// Barva ve stavu Enabled = bez myši, ale dostupné
        /// </summary>
        public Color? EnabledColor { get { return __EnabledColor; } set { if (!__IsReadOnly) __EnabledColor = value; } } private Color? __EnabledColor;
        /// <summary>
        /// Barva ve stavu Active = aktivní, je dáno stavem konkrétního prvku
        /// </summary>
        public Color? ActiveColor { get { return __ActiveColor; } set { if (!__IsReadOnly) __ActiveColor = value; } } private Color? __ActiveColor;
        /// <summary>
        /// Barva ve stavu MouseOn = myš je na prvku
        /// </summary>
        public Color? MouseOnColor { get { return __MouseOnColor; } set { if (!__IsReadOnly) __MouseOnColor = value; } } private Color? __MouseOnColor;
        /// <summary>
        /// Barva ve stavu MouseDown
        /// </summary>
        public Color? MouseDownColor { get { return __MouseDownColor; } set { if (!__IsReadOnly) __MouseDownColor = value; } } private Color? __MouseDownColor;
        /// <summary>
        /// Barva zvýraznění prostoru myši
        /// </summary>
        public Color? MouseHighlightColor { get { return __MouseHighlightColor; } set { if (!__IsReadOnly) __MouseHighlightColor = value; } } private Color? __MouseHighlightColor;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
        /// <summary>
        /// Vrátí barvu pro daný stav
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Color? GetColor(InteractiveState state)
        {
            switch (state)
            {
                case InteractiveState.Enabled: return this.EnabledColor;
                case InteractiveState.Disabled: return this.DisabledColor;
                case InteractiveState.MouseOn: return this.MouseOnColor;
                case InteractiveState.MouseDown: return this.MouseDownColor;
            }
            return this.EnabledColor;
        }
    }
    #endregion
    #region class TextAppearance
    /// <summary>
    /// Vzhled textu - font, styl, velikost
    /// </summary>
    public class TextAppearance
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextAppearance() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextAppearance(bool isReadOnly, FontType? fontType, ContentAlignment textAlignment, AppearanceColorPartType textColorType, ColorSet textColors, params TextInteractiveStyle[] styles)
        {
            __FontType = fontType;
            __TextAlignment = textAlignment;
            __TextColorType = textColorType;
            __TextColors = textColors;
            __TextStyles = new TextInteractiveStyles(isReadOnly, styles);
            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Typ systémového fontu
        /// </summary>
        public FontType? FontType { get { return __FontType; } set { if (!__IsReadOnly) __FontType = value; } } private FontType? __FontType;
        /// <summary>
        /// Explicitně daná velikost, není ale nijak optimální definovat ji takto explicitně. 
        /// Lepší je definovat <see cref="SizeRatio"/>.
        /// </summary>
        public float? EmSize { get { return TextStyles[InteractiveState.Default].EmSize; } set { if (!__IsReadOnly) TextStyles[InteractiveState.Default].EmSize = value; } }
        /// <summary>
        /// Poměr velikosti aktuálního fontu ku fontu defaultnímu daného typu
        /// </summary>
        public float? SizeRatio { get { return TextStyles[InteractiveState.Default].SizeRatio; } set { if (!__IsReadOnly) TextStyles[InteractiveState.Default].SizeRatio = value; } }
        /// <summary>
        /// Styl fontu; default = dle systémového fontu
        /// </summary>
        public FontStyle? FontStyle { get { return TextStyles[InteractiveState.Default].FontStyle; } set { if (!__IsReadOnly) TextStyles[InteractiveState.Default].FontStyle = value; } }
        /// <summary>
        /// Umístění textu v jeho prostoru
        /// </summary>
        public ContentAlignment TextAlignment { get { return __TextAlignment; } set { if (!__IsReadOnly) __TextAlignment = value; } } private ContentAlignment __TextAlignment;
        /// <summary>
        /// Barvy písma - zdrojové místo v paletě
        /// </summary>
        public AppearanceColorPartType TextColorType { get { return __TextColorType; } set { if (!__IsReadOnly) __TextColorType = value; } } private AppearanceColorPartType __TextColorType;
        /// <summary>
        /// Barvy písma, lze zadat i explicitně
        /// </summary>
        public ColorSet TextColors
        {
            get 
            {
                var textColors = __TextColors;
                if (textColors is null)
                    textColors = App.CurrentPalette.GetColorSet(this.TextColorType);
                return textColors;
            }
            set
            {
                if (!__IsReadOnly)
                    __TextColors = value;
            }
        }
        private ColorSet __TextColors;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
        /// <summary>
        /// Styl textu pro daný interaktivní stav (použij indexer s indexem type <see cref="InteractiveState"/>).
        /// Na výstupu není nikdy null - každý stav má svůj styl.
        /// </summary>
        public TextInteractiveStyles TextStyles 
        {
            get 
            { 
                if (__TextStyles is null) 
                    __TextStyles = new TextInteractiveStyles(); 
                return __TextStyles;
            }
        }
        private TextInteractiveStyles __TextStyles;
    }
    /// <summary>
    /// Sada interaktivních stylů
    /// </summary>
    public class TextInteractiveStyles
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextInteractiveStyles() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextInteractiveStyles(bool isReadOnly, params TextInteractiveStyle[] styles)
        {
            var dict = _Styles;

            foreach ( var style in styles ) 
            {
                if (style != null && !dict.ContainsKey(style.InteractiveState))
                    dict.Add(style.InteractiveState, style);
            }

            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Data odpovídající danému stavu
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public TextInteractiveStyle this[InteractiveState state]
        {
            get
            {
                var styles = _Styles;
                if (!styles.TryGetValue(state, out var style))
                {
                    if (__IsReadOnly)
                    {   // Jsme ReadOnly: pro neexistující stav nebudu nic přidávat, ale použiju prázdný readonly styl:
                        style = TextInteractiveStyle.Empty;
                    }
                    else
                    {   // Nejsme ReadOnly: mohu přidat novou editovatelnou položku pro daný stav:
                        style = new TextInteractiveStyle();
                        __Styles.Add(state, style);
                    }
                }
                return style;
            }
        }
        /// <summary>
        /// Dictionary obsahující styly
        /// </summary>
        private Dictionary<InteractiveState, TextInteractiveStyle> _Styles
        {
            get
            {
                if (__Styles is null) __Styles = new Dictionary<InteractiveState, TextInteractiveStyle>();
                return __Styles;
            }
        }
        private Dictionary<InteractiveState, TextInteractiveStyle> __Styles;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
    }
    /// <summary>
    /// Modifikátor stylu písma pro kokrétní interaktivní stav
    /// </summary>
    public class TextInteractiveStyle
    {
        /// <summary>
        /// Konstruktor pro editovatelnou instanci
        /// </summary>
        public TextInteractiveStyle() { }
        /// <summary>
        /// Konstruktor pro naplněnou instanci
        /// </summary>
        public TextInteractiveStyle(bool isReadOnly, InteractiveState interactiveState, float? emSize, float? sizeRatio, FontStyle? fontStyle)
        {
            __InteractiveState = interactiveState;
            __EmSize = emSize;
            __SizeRatio = sizeRatio;
            __FontStyle = fontStyle;
            __IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// Pro tento stav je instance vytvořena
        /// </summary>
        public InteractiveState InteractiveState { get { return __InteractiveState; } set { if (!__IsReadOnly) __InteractiveState = value; } } private InteractiveState __InteractiveState;
        /// <summary>
        /// Explicitně daná velikost, není ale nijak optimální definovat ji takto explicitně. 
        /// Lepší je definovat <see cref="SizeRatio"/>.
        public float? EmSize { get { return __EmSize; } set { if (!__IsReadOnly) __EmSize = value; } } private float? __EmSize;
        /// <summary>
        /// Poměr velikosti aktuálního fontu ku fontu defaultnímu daného typu
        /// </summary>
        public float? SizeRatio { get { return __SizeRatio; } set { if (!__IsReadOnly) __SizeRatio = value; } } private float? __SizeRatio;
        /// <summary>
        /// Styl fontu; default = dle systémového fontu
        /// </summary>
        public FontStyle? FontStyle { get { return __FontStyle; } set { if (!__IsReadOnly) __FontStyle = value; } } private FontStyle? __FontStyle;
        /// <summary>
        /// Data jsou ReadOnly?
        /// </summary>
        public bool IsReadOnly { get { return __IsReadOnly; } } private bool __IsReadOnly;
        /// <summary>
        /// Obsahuje prázdnou ReadOnly instanci
        /// </summary>
        public static TextInteractiveStyle Empty
        {
            get
            {
                if (__Empty is null)
                    __Empty = new TextInteractiveStyle(true, InteractiveState.Default, null, null, null);
                return __Empty;
            }
        }
        public static TextInteractiveStyle __Empty;
    }
    #endregion
}
