using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SystInfo = System.Windows.Forms.SystemInformation;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Knihovna stylů, výchozí hodnoty
    /// </summary>
    public class Styles
    {
        #region Singleton, konstruktor
        /// <summary>
        /// Singleton instannce
        /// </summary>
        protected static Styles Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Lock)
                    {
                        if (__Instance == null)
                            __Instance = new Styles();
                    }
                }
                return __Instance;
            }
        }
        private static Styles __Instance;
        private static object __Lock = new object();
        private Styles()
        {
            _Modifier = new ModifierStyle(); ((IStyleMember)_Modifier).IsStyleInstance = true;
            _ToolTip = new ToolTipStyle(); ((IStyleMember)_ToolTip).IsStyleInstance = true;
            _Label = new LabelStyle(); ((IStyleMember)_Label).IsStyleInstance = true;
            _TextBox = new TextBoxStyle(); ((IStyleMember)_TextBox).IsStyleInstance = true;
            _Button = new ButtonStyle(); ((IStyleMember)_Button).IsStyleInstance = true;

            _ScrollBar = new ScrollBarStyle(); ((IStyleMember)_ScrollBar).IsStyleInstance = true;
        }
        #endregion
        #region Styly konkrétních prvků
        /// <summary>
        /// Výchozí styl pro kreslení modifikátorů (interaktivita) a 3D efektů a stínů
        /// </summary>
        public static ModifierStyle Modifier { get { return Instance._Modifier; } } private ModifierStyle _Modifier;
        /// <summary>
        /// Výchozí styl pro kreslení ToolTipu
        /// </summary>
        public static ToolTipStyle ToolTip { get { return Instance._ToolTip; } } private ToolTipStyle _ToolTip;
        /// <summary>
        /// Výchozí styl pro Label
        /// </summary>
        public static LabelStyle Label { get { return Instance._Label; } } private LabelStyle _Label;
        /// <summary>
        /// Výchozí styl pro TextBox
        /// </summary>
        public static TextBoxStyle TextBox { get { return Instance._TextBox; } } private TextBoxStyle _TextBox;
        /// <summary>
        /// Výchozí styl pro Button
        /// </summary>
        public static ButtonStyle Button { get { return Instance._Button; } } private ButtonStyle _Button;

        /// <summary>
        /// Výchozí styl pro ScrollBar
        /// </summary>
        public static ScrollBarStyle ScrollBar { get { return Instance._ScrollBar; } } private ScrollBarStyle _ScrollBar;
        
        #endregion
    }
    #region ItemStyle : předek stylů
    /// <summary>
    /// <see cref="ItemStyle"/> : předek stylů
    /// </summary>
    public class ItemStyle : IStyleMember
    {
        /// <summary>
        /// Konstruktor pro běžnou instanci
        /// </summary>
        public ItemStyle()
        {
            IsStyleInstance = false;
        }
        /// <summary>
        /// Obsahuje true v té instanci, které reprezentuje základní Styl = je umístěna v třídě <see cref="Styles"/>.
        /// Tato instance při vyhodnocování Current hodnoty v metodách <see cref="GetValue{T}(Func{T?}, Func{T?}, Func{T?}, T)"/> 
        /// neřeší hodnoty z instance Parent ani z instance Style, řeší pouze dodanou explicitní hodnotu a defaultní hodnotu.
        /// </summary>
        protected bool IsStyleInstance { get; private set; }
        /// <summary>
        /// Postupně vyhodnotí dodané funkce, které vracejí hodnotu: moji explicitní, mého parenta, základního stylu a defaultní hodnotu. Vrátí první nalezenou.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="myFunc"></param>
        /// <param name="parentFunc"></param>
        /// <param name="styleFunc"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetValue<T>(Func<T?> myFunc, Func<T?> parentFunc, Func<T?> styleFunc, T defaultValue) where T : struct
        {
            var myValue = myFunc();
            if (myValue.HasValue) return myValue.Value;

            if (!IsStyleInstance)
            {
                var parentValue = parentFunc();
                if (parentValue.HasValue) return parentValue.Value;

                var styleValue = styleFunc();
                if (styleValue.HasValue) return styleValue.Value;
            }

            return defaultValue;
        }
        /// <summary>
        /// Vrátí reálný font sestavený z dodaných hodnot.
        /// </summary>
        /// <param name="myFontFunc"></param>
        /// <param name="myModifierFunc"></param>
        /// <param name="parentFontFunc"></param>
        /// <param name="parentModifierFunc"></param>
        /// <param name="styleFontFunc"></param>
        /// <param name="styleModifierFunc"></param>
        /// <param name="defaultFont"></param>
        /// <param name="defaultModifier"></param>
        /// <returns></returns>
        protected FontInfo GetFontInfo(
            Func<FontInfo> myFontFunc, Func<FontModifierInfo> myModifierFunc,
            Func<FontInfo> parentFontFunc, Func<FontModifierInfo> parentModifierFunc,
            Func<FontInfo> styleFontFunc, Func<FontModifierInfo> styleModifierFunc,
            FontInfo defaultFont = null, FontModifierInfo defaultModifier = null)
        {
            FontInfo font = myFontFunc();
            FontModifierInfo modifier = myModifierFunc();
            if (!IsStyleInstance)
            {
                if (font == null) font = parentFontFunc();
                if (font == null) font = styleFontFunc();

                if (modifier == null) modifier = parentModifierFunc();
                if (modifier == null) modifier = styleModifierFunc();
            }
            if (font == null) font = defaultFont ?? FontInfo.Default;
            if (modifier == null) modifier = defaultModifier;

            return ((modifier != null) ? font.GetModifiedFont(modifier) : font);
        }
        /// <summary>
        /// Metoda vrátí hodnotu z dané proměnné (pokud má hodnotu).
        /// Pokud hodnotu nemá, pak do proměnné vloží hodnotu získanou pomocí dodané funkce, a vrátí ji.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variable"></param>
        /// <param name="valueFunc"></param>
        /// <returns></returns>
        protected T GetDefault<T>(ref T? variable, Func<T> valueFunc) where T : struct
        {
            if (!variable.HasValue)
                variable = valueFunc();
            return variable.Value;
        }
        /// <summary>
        /// Metoda vrátí instanci z dané proměnné (pokud není null).
        /// Pokud dosud je null, pak do proměnné vloží new instanci získanou pomocí dodané funkce, a vrátí ji.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variable"></param>
        /// <param name="valueFunc"></param>
        /// <returns></returns>
        protected T GetDefault<T>(ref T variable, Func<T> valueFunc) where T : class
        {
            if (variable == null)
                variable = valueFunc();
            return variable;
        }
        bool IStyleMember.IsStyleInstance { get { return IsStyleInstance; } set { IsStyleInstance = value; } }
    }
    /// <summary>
    /// Deklarace rozhraní pro prvky, které jsou členem instance <see cref="Styles"/>
    /// </summary>
    public interface IStyleMember
    {
        /// <summary>
        /// Obsahuje true v té instanci, které reprezentuje základní Styl = je umístěna v třídě <see cref="Styles"/>.
        /// Tato instance při vyhodnocování Current hodnoty v metodách <see cref="ItemStyle.GetValue{T}(Func{T?}, Func{T?}, Func{T?}, T)"/> 
        /// neřeší hodnoty z instance Parent ani z instance Style, řeší pouze dodanou explicitní hodnotu a defaultní hodnotu.
        /// </summary>
        bool IsStyleInstance { get; set; }
    }
    #endregion
    #region ModifierStyle
    /// <summary>
    /// Styl pro kreslení modifikátorů (interaktivita) a 3D efektů a stínů
    /// </summary>
    public class ModifierStyle : ItemStyle, IModifierStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public ModifierStyle Parent { get; set; }

        /// <summary>
        /// Globální povolení pro používání 3D efektů. Lze je tak jednoduše potlačit. Pak všechny property "Effect3D" nebudou používány.
        /// </summary>
        public bool? Effect3DEnabled { get; set; }
        /// <summary>
        /// Barva reprezentující světlo ve 3D efektu. 
        /// Touto barvou bude kreslen levý / horní okraj prvku, pokud by Ratio bylo 1f.
        /// </summary>
        public Color? Effect3DColorLight { get; set; }
        /// <summary>
        /// Barva reprezentující tmu ve 3D efektu. 
        /// Touto barvou bude kreslen pravý / dolní okraj prvku, pokud by Ratio bylo 1f.
        /// </summary>
        public Color? Effect3DColorDark { get; set; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, neaktivní.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        public float? Effect3DRatioEnabled { get; set; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, s myší nad prvkem anebo s focusem.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        public float? Effect3DRatioMouseOn { get; set; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, se stisknutou myší (nebo stisknuté tlačítko).
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        public float? Effect3DRatioMouseDown { get; set; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, který je přetahován (Drag and Drop) myší.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        public float? Effect3DRatioMouseDrag { get; set; }

        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je Disabled.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? ModifierColorDisabled { get; set; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je MouseOn nebo má Focus.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? ModifierColorMouseOn { get; set; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je MouseDown.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? ModifierColorMouseDown { get; set; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je přetahován (Drag and Drop) myší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? ModifierColorMouseDrag { get; set; }

        /// <summary>
        /// Barva okrajů 3D světlejší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? Border3DColorLight { get; set; }
        /// <summary>
        /// Barva okrajů 3D tmavší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        public Color? Border3DColorDark { get; set; }

        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance <see cref="ModifierStyle"/> ve stylech (<see cref="Styles"/>), které obsahuje výchozí hodnoty pro tuto instanci pro hodnoty získávané třídou <see cref="ModifierStyle"/> ve třídách potomků.
        /// Typicky: výchozí barvu písma pro textbox nastavujeme do property <see cref="Styles.TextBox"/>.<see cref="LabelStyle.TextColor"/>, 
        /// ale protože barvu písma vyhodnocuje fyzicky třída <see cref="LabelStyle"/>, pak tato třída musí vědět, ze které instance v <see cref="Styles"/> má brát výchozí hodnotu.
        /// <para/>
        /// Proto třída <see cref="ModifierStyle"/> v property <see cref="StyleModifier"/> vrací instanci <see cref="Styles.Modifier"/>, 
        /// ale potomci (například <see cref="TextBoxStyle"/>) v této property vrací svoji instanci potomka (například <see cref="Styles.TextBox"/>), protože z této instance se bude brát patřičná hodnota.
        /// </summary>
        protected virtual ModifierStyle StyleModifier { get { return Styles.Modifier; } }
        #endregion
        #region Defaultní hodnoty
        /// <summary>Defaultní barva pro <see cref="Effect3DColorLight"/></summary>
        protected virtual Color DefaultEffect3DColorLight { get { return GetDefault<Color>(ref _DefaultEffect3DColorLight, () => Color.White); } } private static Color? _DefaultEffect3DColorLight = null;
        /// <summary>Defaultní barva pro <see cref="Effect3DColorDark"/></summary>
        protected virtual Color DefaultEffect3DColorDark { get { return GetDefault<Color>(ref _DefaultEffect3DColorDark, () => Color.DarkSlateGray); } } private static Color? _DefaultEffect3DColorDark = null;
        /// <summary>Defaultní barva pro <see cref="ModifierColorDisabled"/></summary>
        protected virtual Color DefaultModifierColorDisabled { get { return GetDefault<Color>(ref _DefaultModifierColorDisabled, () => Color.FromArgb(64, SystemColors.ControlDarkDark)); } } private static Color? _DefaultModifierColorDisabled = null;
        /// <summary>Defaultní barva pro <see cref="ModifierColorMouseOn"/></summary>
        protected virtual Color DefaultModifierColorMouseOn { get { return GetDefault<Color>(ref _DefaultModifierColorMouseOn, () => Color.FromArgb(32, Color.LightYellow)); } } private static Color? _DefaultModifierColorMouseOn = null;
        /// <summary>Defaultní barva pro <see cref="ModifierColorMouseDown"/></summary>
        protected virtual Color DefaultModifierColorMouseDown { get { return GetDefault<Color>(ref _DefaultModifierColorMouseDown, () => Color.FromArgb(32, Color.Black)); } } private static Color? _DefaultModifierColorMouseDown = null;
        /// <summary>Defaultní barva pro <see cref="ModifierColorMouseDrag"/></summary>
        protected virtual Color DefaultModifierColorMouseDrag { get { return GetDefault<Color>(ref _DefaultModifierColorMouseDrag, () => Color.FromArgb(64, Color.Violet));  } } private static Color? _DefaultModifierColorMouseDrag = null;
        /// <summary>Defaultní barva pro <see cref="Border3DColorLight"/></summary>
        protected virtual Color DefaultBorder3DColorLight { get { return GetDefault<Color>(ref _DefaultBorder3DColorLight, () => Color.FromArgb(192, SystemColors.ControlDarkDark)); } } private static Color? _DefaultBorder3DColorLight = null;
        /// <summary>Defaultní barva pro <see cref="Border3DColorDark"/></summary>
        protected virtual Color DefaultBorder3DColorDark { get { return GetDefault<Color>(ref _DefaultBorder3DColorDark, () => Color.FromArgb(192, SystemColors.ControlLightLight)); return _DefaultBorder3DColorDark.Value; } } private static Color? _DefaultBorder3DColorDark = null;
        #endregion
        #region Implementace interface
        bool IModifierStyle.Effect3DEnabled { get { return GetValue<bool>(() => Effect3DEnabled, () => Parent?.Effect3DEnabled, () => StyleModifier.Effect3DEnabled, true); } }
        Color IModifierStyle.Effect3DColorLight { get { return GetValue<Color>(() => Effect3DColorLight, () => Parent?.Effect3DColorLight, () => StyleModifier.Effect3DColorLight, DefaultEffect3DColorLight); } }
        Color IModifierStyle.Effect3DColorDark { get { return GetValue<Color>(() => Effect3DColorDark, () => Parent?.Effect3DColorDark, () => StyleModifier.Effect3DColorDark, DefaultEffect3DColorDark); } }
        float IModifierStyle.Effect3DRatioEnabled { get { return GetValue<float>(() => Effect3DRatioEnabled, () => Parent?.Effect3DRatioEnabled, () => StyleModifier.Effect3DRatioEnabled, 0.15f); } }
        float IModifierStyle.Effect3DRatioMouseOn { get { return GetValue<float>(() => Effect3DRatioEnabled, () => Parent?.Effect3DRatioEnabled, () => StyleModifier.Effect3DRatioEnabled, 0.35f); } }
        float IModifierStyle.Effect3DRatioMouseDown { get { return GetValue<float>(() => Effect3DRatioEnabled, () => Parent?.Effect3DRatioEnabled, () => StyleModifier.Effect3DRatioEnabled, -0.25f); } }
        float IModifierStyle.Effect3DRatioMouseDrag { get { return GetValue<float>(() => Effect3DRatioEnabled, () => Parent?.Effect3DRatioEnabled, () => StyleModifier.Effect3DRatioEnabled, -0.10f); } }
        Color IModifierStyle.ModifierColorDisabled { get { return GetValue<Color>(() => ModifierColorDisabled, () => Parent?.ModifierColorDisabled, () => StyleModifier.ModifierColorDisabled, DefaultModifierColorDisabled); } }
        Color IModifierStyle.ModifierColorMouseOn { get { return GetValue<Color>(() => ModifierColorMouseOn, () => Parent?.ModifierColorMouseOn, () => StyleModifier.ModifierColorMouseOn, DefaultModifierColorMouseOn); } }
        Color IModifierStyle.ModifierColorMouseDown { get { return GetValue<Color>(() => ModifierColorMouseDown, () => Parent?.ModifierColorMouseDown, () => StyleModifier.ModifierColorMouseDown, DefaultModifierColorMouseDown); } }
        Color IModifierStyle.ModifierColorMouseDrag { get { return GetValue<Color>(() => ModifierColorMouseDrag, () => Parent?.ModifierColorMouseDrag, () => StyleModifier.ModifierColorMouseDrag, DefaultModifierColorMouseDrag); } }
        Color IModifierStyle.Border3DColorLight { get { return GetValue<Color>(() => Border3DColorLight, () => Parent?.Border3DColorLight, () => StyleModifier.Border3DColorLight, DefaultBorder3DColorLight); } }
        Color IModifierStyle.Border3DColorDark { get { return GetValue<Color>(() => Border3DColorDark, () => Parent?.Border3DColorDark, () => StyleModifier.Border3DColorDark, DefaultBorder3DColorDark); } }
        #endregion
    }
    #endregion
    #region ToolTipStyle
    /// <summary>
    /// Styl pro kreslení ToolTipu
    /// </summary>
    public class ToolTipStyle : ItemStyle, IToolTipStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public ToolTipStyle Parent { get; set; }
        /// <summary>
        /// Explicitně zadaný font písma. Pokud není zadán, použije se <see cref="FontInfo.Default"/>.
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Explicitně zadaný modifikátor písma. Pokud není zadán, použije se <see cref="FontModifierInfo.Empty"/>
        /// </summary>
        public FontModifierInfo FontModifier { get; set; }
        /// <summary>
        /// Explicitně zadaná barva písma (nikoli pozadí)
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí
        /// </summary>
        public Color? TextColor { get; set; }
        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance <see cref="ToolTipStyle"/> ve stylech (<see cref="Styles"/>), které obsahuje výchozí hodnoty pro tuto instanci pro hodnoty získávané třídou <see cref="LabelStyle"/> ve třídách potomků.
        /// Typicky: výchozí barvu písma pro textbox nastavujeme do property <see cref="Styles.TextBox"/>.<see cref="LabelStyle.TextColor"/>, 
        /// ale protože barvu písma vyhodnocuje fyzicky třída <see cref="LabelStyle"/>, pak tato třída musí vědět, ze které instance v <see cref="Styles"/> má brát výchozí hodnotu.
        /// <para/>
        /// Proto třída <see cref="LabelStyle"/> v property <see cref="StyleToolTip"/> vrací instanci <see cref="Styles.Label"/>, 
        /// ale potomci (například <see cref="TextBoxStyle"/>) v této property vrací svoji instanci potomka (například <see cref="Styles.TextBox"/>), protože z této instance se bude brát patřičná hodnota.
        /// </summary>
        protected virtual ToolTipStyle StyleToolTip { get { return Styles.ToolTip; } }
        #endregion
        #region Defaultní hodnoty
        /// <summary>Defaultní barva pro <see cref="BackColor"/></summary>
        protected virtual Color DefaultBackColor { get { return SystemColors.Info; } }
        /// <summary>Defaultní barva pro <see cref="TextColor"/></summary>
        protected virtual Color DefaultTextColor { get { return SystemColors.InfoText; } }
        #endregion
        #region Implementace interface

        #endregion
    }
    #endregion
    #region LabelStyle
    /// <summary>
    /// Styl pro vykreslení labelu a dalších textů (barva textu, font, modifikátor fontu)
    /// </summary>
    public class LabelStyle : ItemStyle, ILabelStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public LabelStyle Parent { get; set; }
        /// <summary>
        /// Explicitně zadaný font písma. Pokud není zadán, použije se <see cref="FontInfo.Default"/>.
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Explicitně zadaný modifikátor písma. Pokud není zadán, použije se <see cref="FontModifierInfo.Empty"/>
        /// </summary>
        public FontModifierInfo FontModifier { get; set; }
        /// <summary>
        /// Explicitně zadaná barva písma (nikoli pozadí).
        /// Label není interaktivní, proto postačuje jedna barva pro statické písmo.
        /// </summary>
        public Color? TextColor { get; set; }
        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="LabelStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="LabelStyle"/> používá pro svoje vlastní property (například <see cref="LabelStyle.TextColor"/>) instanci uloženou v <see cref="Styles.Label"/>.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="TextBoxStyle"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="LabelStyle"/>),
        /// tedy například <see cref="LabelStyle.TextColor"/>, byly načtený z "jejich" instance = <see cref="Styles.TextBox"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// </summary>
        protected virtual LabelStyle StyleLabel { get { return Styles.Label; } }
        #endregion
        #region Defaultní hodnoty
        /// <summary>Defaultní hodnota pro <see cref="Font"/></summary>
        protected virtual FontInfo DefaultFont { get { return GetDefault<FontInfo>(ref _DefaultFont, () => FontInfo.Default); } } private static FontInfo _DefaultFont;
        /// <summary>Defaultní hodnota pro <see cref="TextColor"/></summary>
        protected virtual Color DefaultTextColor { get { return SystemColors.ControlText; } }
        #endregion
        #region Implementace interface
        Color ILabelStyle.TextColor { get { return GetValue<Color>(() => TextColor, () => Parent?.TextColor, () => StyleLabel.TextColor, DefaultTextColor); } }
        FontInfo ILabelStyle.Font { get { return GetFontInfo(() => Font, () => FontModifier, () => Parent?.Font, () => Parent?.FontModifier, () => StyleLabel.Font, () => StyleLabel.FontModifier, DefaultFont); } }
        #endregion
    }
    #endregion
    #region TextBorderStyle
    /// <summary>
    /// Bázová třída pro styly, které jsou interaktvní, mají text a pozadí a rámeček (textboxy, buttony, atd).
    /// Tato třída je abstraktní, 
    /// </summary>
    public abstract class TextBorderStyle : LabelStyle, ITextBorderStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public new TextBorderStyle Parent { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, základní.
        /// Barva je použita pro textbox ve stavu Enabled.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, pro textbox ve stavu Disabled.
        /// </summary>
        public Color? BackColorDisabled { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        public Color? BackColorMouseOn { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        public Color? BackColorMouseDown { get; set; }

        /// <summary>
        /// Explicitně zadaná barva písma, pro textbox ve stavu Disabled.
        /// </summary>
        public Color? TextColorDisabled { get; set; }
        /// <summary>
        /// Explicitně zadaná barva písma, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        public Color? TextColorMouseOn { get; set; }
        /// <summary>
        /// Explicitně zadaná barva písma, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        public Color? TextColorMouseDown { get; set; }

        /// <summary>
        /// Explicitně zadaná barva rámečku, pro textbox ve stavu Enabled
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Explicitně zadaná barva rámečku, pro textbox ve stavu Disabled.
        /// </summary>
        public Color? BorderColorDisabled { get; set; }
        /// <summary>
        /// Explicitně zadaná barva rámečku, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        public Color? BorderColorMouseOn { get; set; }
        /// <summary>
        /// Explicitně zadaná barva rámečku, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        public Color? BorderColorMouseDown { get; set; }

        /// <summary>
        /// Druh okraje
        /// </summary>
        public TextBoxBorderType? BorderType { get; set; }
        /// <summary>
        /// Prostor mezi vnitřkem Borderu a začátkem textu = počet prázdných pixelů
        /// </summary>
        public int? TextMargin { get; set; }

        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="TextBorderStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="TextBorderStyle"/> používá pro svoje vlastní property (například <see cref="TextBorderStyle.BackColor"/>) instanci uloženou v <see cref="Styles"/>.???.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="TextBoxStyle"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="TextBorderStyle"/>),
        /// tedy například <see cref="TextBorderStyle.BackColor"/>, byly načteny z "jejich" instance = <see cref="Styles.TextBox"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// <para/>
        /// Tato konkrétní třída <see cref="TextBorderStyle"/> se přímo nevyužívá, je abstraktní, proto předepisuje tuto property jako abstract, musí ji vyplnit potomek a vracet svoji základní instanci.
        /// </summary>
        protected abstract TextBorderStyle StyleTextBorder { get; }
        /// <summary>
        /// Zde sděluji svému předkovi <see cref="LabelStyle"/>, ze které instance <see cref="Styles"/> má čerpat svoje základní hodnoty.
        /// </summary>
        protected override LabelStyle StyleLabel { get { return StyleTextBorder; } }
        #endregion
        #region Defaultní hodnoty: vlastní i overrides
        /// <summary>Defaultní hodnota pro <see cref="BackColor"/></summary>
        protected abstract Color DefaultBackColor { get; }
        /// <summary>Defaultní hodnota pro <see cref="BackColorDisabled"/></summary>
        protected abstract Color DefaultBackColorDisabled { get; }
        /// <summary>Defaultní hodnota pro <see cref="BackColorMouseOn"/></summary>
        protected abstract Color DefaultBackColorMouseOn { get; }
        /// <summary>Defaultní hodnota pro <see cref="BackColorMouseDown"/></summary>
        protected abstract Color DefaultBackColorMouseDown { get; }
        /// <summary>Defaultní hodnota pro <see cref="TextColorDisabled"/></summary>
        protected abstract Color DefaultTextColorDisabled { get; }
        /// <summary>Defaultní hodnota pro <see cref="TextColorMouseOn"/></summary>
        protected abstract Color DefaultTextColorMouseOn { get; }
        /// <summary>Defaultní hodnota pro <see cref="TextColorMouseDown"/></summary>
        protected abstract Color DefaultTextColorMouseDown { get; }
        /// <summary>Defaultní hodnota pro <see cref="BorderColor"/></summary>
        protected abstract Color DefaultBorderColor { get; }
        /// <summary>Defaultní hodnota pro <see cref="BorderColorDisabled"/></summary>
        protected abstract Color DefaultBorderColorDisabled { get; }
        /// <summary>Defaultní hodnota pro <see cref="BorderColorMouseOn"/></summary>
        protected abstract Color DefaultBorderColorMouseOn { get; }
        /// <summary>Defaultní hodnota pro <see cref="BorderColorMouseDown"/></summary>
        protected abstract Color DefaultBorderColorMouseDown { get; }
        /// <summary>Defaultní hodnota pro <see cref="BorderType"/></summary>
        protected abstract TextBoxBorderType DefaultBorderType { get; }
        /// <summary>Defaultní hodnota pro <see cref="TextMargin"/></summary>
        protected abstract int DefaultTextMargin { get; }
        #endregion
        #region Implementace interface
        Color ITextBorderStyle.BackColor { get { return GetValue<Color>(() => BackColor, () => Parent?.BackColor, () => StyleTextBorder.BackColor, DefaultBackColor); } }
        Color ITextBorderStyle.BackColorDisabled { get { return GetValue<Color>(() => BackColorDisabled, () => Parent?.BackColorDisabled, () => StyleTextBorder.BackColorDisabled, DefaultBackColorDisabled); } }
        Color ITextBorderStyle.BackColorMouseOn { get { return GetValue<Color>(() => BackColorMouseOn, () => Parent?.BackColorMouseOn, () => StyleTextBorder.BackColorMouseOn, DefaultBackColorMouseOn); } }
        Color ITextBorderStyle.BackColorMouseDown { get { return GetValue<Color>(() => BackColorMouseDown, () => Parent?.BackColorMouseDown, () => StyleTextBorder.BackColorMouseDown, DefaultBackColorMouseDown); } }

        Color ITextBorderStyle.TextColorDisabled { get { return GetValue<Color>(() => TextColorDisabled, () => Parent?.TextColorDisabled, () => StyleTextBorder.TextColorDisabled, DefaultTextColorDisabled); } }
        Color ITextBorderStyle.TextColorMouseOn { get { return GetValue<Color>(() => TextColorMouseOn, () => Parent?.TextColorMouseOn, () => StyleTextBorder.TextColorMouseOn, DefaultTextColorMouseOn); } }
        Color ITextBorderStyle.TextColorMouseDown { get { return GetValue<Color>(() => TextColorMouseDown, () => Parent?.TextColorMouseDown, () => StyleTextBorder.TextColorMouseDown, DefaultTextColorMouseDown); } }

        Color ITextBorderStyle.BorderColor { get { return GetValue<Color>(() => BorderColor, () => Parent?.BorderColor, () => StyleTextBorder.BorderColor, DefaultBorderColor); } }
        Color ITextBorderStyle.BorderColorDisabled { get { return GetValue<Color>(() => BorderColorDisabled, () => Parent?.BorderColorDisabled, () => StyleTextBorder.BorderColorDisabled, DefaultBorderColorDisabled); } }
        Color ITextBorderStyle.BorderColorMouseOn { get { return GetValue<Color>(() => BorderColorMouseOn, () => Parent?.BorderColorMouseOn, () => StyleTextBorder.BorderColorMouseOn, DefaultBorderColorMouseOn); } }
        Color ITextBorderStyle.BorderColorMouseDown { get { return GetValue<Color>(() => BorderColorMouseDown, () => Parent?.BorderColorMouseDown, () => StyleTextBorder.BorderColorMouseDown, DefaultBorderColorMouseDown); } }

        TextBoxBorderType ITextBorderStyle.BorderType { get { return GetValue<TextBoxBorderType>(() => BorderType, () => Parent?.BorderType, () => StyleTextBorder.BorderType, DefaultBorderType); } }
        int ITextBorderStyle.TextMargin { get { return GetValue<int>(() => TextMargin, () => Parent?.TextMargin, () => StyleTextBorder.TextMargin, DefaultTextMargin); } }
        #endregion
    }
    #endregion
    #region TextBoxStyle
    /// <summary>
    /// Styly pro textbox
    /// </summary>
    public class TextBoxStyle : TextBorderStyle, ITextBoxStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public new TextBoxStyle Parent { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, pro textbox ve stavu Warning.
        /// Barva je použita pro textbox ve stavu Enabled, pokud je Required a prázdný a je nastaven příznak Zobrazit Warningy.
        /// </summary>
        public Color? BackColorWarning { get; set; }
        /// <summary>
        /// Explicitně zadaná barva pozadí, pro znaky, které jsou Selected.
        /// </summary>
        public Color? BackColorSelectedText { get; set; }
        /// <summary>
        /// Explicitně zadaná barva písma, pro znaky, které jsou Selected.
        /// </summary>
        public Color? TextColorSelectedText { get; set; }
        /// <summary>
        /// Explicitně zadaná barva rámečku, pro textbox ve stavu Warning
        /// </summary>
        public Color? BorderColorWarning { get; set; }
        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="TextBoxStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="TextBoxStyle"/> používá pro svoje vlastní property (například <see cref="TextBoxStyle.TextColorSelectedText"/>) instanci uloženou v <see cref="Styles.TextBox"/>.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="ComboBoxStyle"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="TextBoxStyle"/>),
        /// tedy například <see cref="TextBoxStyle.TextColorSelectedText"/>, byly načteny z "jejich" instance = <see cref="Styles.ComboBox"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// </summary>
        protected virtual TextBoxStyle StyleTextBox { get { return Styles.TextBox; } }
        /// <summary>
        /// Zde sděluji svému předkovi <see cref="TextBorderStyle"/>, ze které instance <see cref="Styles"/> má čerpat svoje základní hodnoty.
        /// </summary>
        protected override TextBorderStyle StyleTextBorder { get { return StyleTextBox; } }
        #endregion
        #region Defaultní hodnoty: vlastní i overrides
        /// <summary>Defaultní barva pozadí, pro textbox ve stavu Warning.</summary>
        protected virtual Color DefaultBackColorWarning { get { return Color.FromArgb(255, 192, 192); } }
        /// <summary>Defaultní barva pozadí, pro znaky, které jsou Selected.</summary>
        protected virtual Color DefaultBackColorSelectedText { get { return SystemColors.Highlight; } }
        /// <summary>Defaultní barva písma, pro znaky, které jsou Selected.</summary>
        protected virtual Color DefaultTextColorSelectedText { get { return SystemColors.HighlightText; } }
        /// <summary>Defaultní barva rámečku, pro textbox ve stavu Warning.</summary>
        protected virtual Color DefaultBorderColorWarning { get { return Color.FromArgb(128, 0, 0); } }

        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColor"/></summary>
        protected override Color DefaultBackColor { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorDisabled"/></summary>
        protected override Color DefaultBackColorDisabled { get { return SystemColors.ControlDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorMouseOn"/></summary>
        protected override Color DefaultBackColorMouseOn { get { return SystemColors.HotTrack; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorMouseDown"/></summary>
        protected override Color DefaultBackColorMouseDown { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorDisabled"/></summary>
        protected override Color DefaultTextColorDisabled { get { return SystemColors.GrayText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorMouseOn"/></summary>
        protected override Color DefaultTextColorMouseOn { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorMouseDown"/></summary>
        protected override Color DefaultTextColorMouseDown { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColor"/></summary>
        protected override Color DefaultBorderColor { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorDisabled"/></summary>
        protected override Color DefaultBorderColorDisabled { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorMouseOn"/></summary>
        protected override Color DefaultBorderColorMouseOn { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorMouseDown"/></summary>
        protected override Color DefaultBorderColorMouseDown { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderType"/></summary>
        protected override TextBoxBorderType DefaultBorderType { get { return TextBoxBorderType.Single; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextMargin"/></summary>
        protected override int DefaultTextMargin { get { return 1; } }

        /// <summary>Defaultní hodnota pro <see cref="LabelStyle.TextColor"/></summary>
        protected override Color DefaultTextColor { get { return SystemColors.ControlText; } }

        #endregion
        #region Implementace interface
        Color ITextBoxStyle.BackColorWarning { get { return GetValue<Color>(() => BackColorWarning, () => Parent?.BackColorWarning, () => StyleTextBox.BackColorWarning, DefaultBackColorWarning); } }
        Color ITextBoxStyle.BackColorSelectedText { get { return GetValue<Color>(() => BackColorSelectedText, () => Parent?.BackColorSelectedText, () => StyleTextBox.BackColorSelectedText, DefaultBackColorSelectedText); } }
        Color ITextBoxStyle.TextColorSelectedText { get { return GetValue<Color>(() => TextColorSelectedText, () => Parent?.TextColorSelectedText, () => StyleTextBox.TextColorSelectedText, DefaultTextColorSelectedText); } }
        Color ITextBoxStyle.BorderColorWarning { get { return GetValue<Color>(() => BorderColorWarning, () => Parent?.BorderColorWarning, () => StyleTextBox.BorderColorWarning, DefaultBorderColorWarning); } }
        #endregion
    }
    #endregion
    #region ButtonStyle
    /// <summary>
    /// Styly pro Button
    /// </summary>
    public class ButtonStyle : TextBorderStyle, IButtonStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public new ButtonStyle Parent { get; set; }

        /// <summary>
        /// Počet pixelů zaobleného rohu, default 0
        /// </summary>
        public int? RoundCorner { get; set; }

        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="ButtonStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="ButtonStyle"/> používá pro svoje vlastní property (například <see cref="ButtonStyle.RoundCorner"/>) instanci uloženou v <see cref="Styles.Button"/>.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="DropDownButtonStyle"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="ButtonStyle"/>),
        /// tedy například <see cref="ButtonStyle.RoundCorner"/>, byly načteny z "jejich" instance = <see cref="Styles.DropDownButton"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// </summary>
        protected virtual ButtonStyle StyleButton { get { return Styles.Button; } }
        /// <summary>
        /// Zde sděluji svému předkovi <see cref="TextBorderStyle"/>, ze které instance <see cref="Styles"/> má čerpat svoje základní hodnoty.
        /// </summary>
        protected override TextBorderStyle StyleTextBorder { get { return StyleButton; } }
        #endregion
        #region Defaultní hodnoty: vlastní i overrides
        /// <summary>Defaultní hodnota pro Počet pixelů zaobleného rohu.</summary>
        protected virtual int DefaultRoundCorner { get { return 0; } }

        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColor"/></summary>
        protected override Color DefaultBackColor { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorDisabled"/></summary>
        protected override Color DefaultBackColorDisabled { get { return SystemColors.ControlDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorMouseOn"/></summary>
        protected override Color DefaultBackColorMouseOn { get { return SystemColors.HotTrack; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BackColorMouseDown"/></summary>
        protected override Color DefaultBackColorMouseDown { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorDisabled"/></summary>
        protected override Color DefaultTextColorDisabled { get { return SystemColors.GrayText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorMouseOn"/></summary>
        protected override Color DefaultTextColorMouseOn { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextColorMouseDown"/></summary>
        protected override Color DefaultTextColorMouseDown { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColor"/></summary>
        protected override Color DefaultBorderColor { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorDisabled"/></summary>
        protected override Color DefaultBorderColorDisabled { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorMouseOn"/></summary>
        protected override Color DefaultBorderColorMouseOn { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderColorMouseDown"/></summary>
        protected override Color DefaultBorderColorMouseDown { get { return SystemColors.ControlDarkDark; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.BorderType"/></summary>
        protected override TextBoxBorderType DefaultBorderType { get { return TextBoxBorderType.Single; } }
        /// <summary>Defaultní hodnota pro <see cref="TextBorderStyle.TextMargin"/></summary>
        protected override int DefaultTextMargin { get { return 1; } }

        /// <summary>Defaultní hodnota pro <see cref="LabelStyle.TextColor"/></summary>
        protected override Color DefaultTextColor { get { return SystemColors.ControlText; } }
        #endregion
        #region Implementace interface
        int IButtonStyle.RoundCorner { get { return GetValue<int>(() => RoundCorner, () => Parent?.RoundCorner, () => StyleButton.RoundCorner, DefaultRoundCorner); } }
        #endregion
    }
    #endregion
    #region ColumnHeaderStyle
    #endregion
    #region ScrollBarStyle
    /// <summary>
    /// Styl pro vykreslení scrollbaru a dalších podobných objektů (velikosti, barvy, vlastnosti)
    /// </summary>
    public class ScrollBarStyle : ItemStyle, IScrollBarStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public ScrollBarStyle Parent { get; set; }
        /// <summary>
        /// Vnější velikost (šířka celého Scrollbaru u vodorovného, výška u svislého), neaktivní = bez myši
        /// </summary>
        public int? ScrollSizeInactive { get; set; }
        /// <summary>
        /// Vnitřní velikost (šířka vizuálního jezdce u vodorovného, výška u svislého), neaktivní = bez myši
        /// </summary>
        public int? ThumbSizeInactive { get; set; }
        /// <summary>
        /// Vnější velikost (šířka celého Scrollbaru u vodorovného, výška u svislého), aktivní = s myší
        /// </summary>
        public int? ScrollSizeActive { get; set; }
        /// <summary>
        /// Vnitřní velikost (šířka vizuálního jezdce u vodorovného, výška u svislého), aktivní = s myší
        /// </summary>
        public int? ThumbSizeActive { get; set; }
        /// <summary>
        /// Barva pozadí ScrollBaru, neaktivní
        /// </summary>
        public Color? ScrollColorInactive { get; set; }
        /// <summary>
        /// Barva jezdce ScrollBaru, neaktivní
        /// </summary>
        public Color? ThumbColorInactive { get; set; }
        /// <summary>
        /// Barva šipek a linek ScrollBaru, neaktivní
        /// </summary>
        public Color? ArrowColorInactive { get; set; }
        /// <summary>
        /// Barva pozadí ScrollBaru, aktivní
        /// </summary>
        public Color? ScrollColorActive { get; set; }
        /// <summary>
        /// Barva jezdce ScrollBaru, aktivní
        /// </summary>
        public Color? ThumbColorActive { get; set; }
        /// <summary>
        /// Barva šipek a linek ScrollBaru, aktivní
        /// </summary>
        public Color? ArrowColorActive { get; set; }
        /// <summary>
        /// Ikona kreslená uprostřed Thumbu, pokud je na ni místo a je povolena ve vlastnostech
        /// </summary>
        public Image ThumbImage { get; set; }
        /// <summary>
        /// Vlastnosti vzhledu ScrollBaru
        /// </summary>
        public ScrollBarProperties? Properties { get; set; }
        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="ScrollBarStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="ScrollBarStyle"/> používá pro svoje vlastní property (například <see cref="ScrollBarStyle.ScrollColorInactive"/>) instanci uloženou v <see cref="Styles.ScrollBar"/>.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="SmartScrollBarStyle"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="ScrollBarStyle"/>),
        /// tedy například <see cref="ScrollBarStyle.ScrollColorInactive"/>, byly načteny z "jejich" instance = <see cref="Styles.SmartScrollBar"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// </summary>
        protected virtual ScrollBarStyle StyleScrollBar { get { return Styles.ScrollBar; } }
        #endregion
        #region Defaultní hodnoty
        /// <summary>Defaultní hodnota pro <see cref="ScrollSizeInactive"/></summary>
        protected virtual int DefaultScrollSizeInactive { get { return SystInfo.HorizontalScrollBarHeight; } }
        /// <summary>Defaultní hodnota pro <see cref="ThumbSizeInactive"/></summary>
        protected virtual int DefaultThumbSizeInactive { get { return SystInfo.HorizontalScrollBarHeight - 4; } }
        /// <summary>Defaultní hodnota pro <see cref="ScrollSizeActive"/></summary>
        protected virtual int DefaultScrollSizeActive { get { return SystInfo.HorizontalScrollBarHeight; } }
        /// <summary>Defaultní hodnota pro <see cref="ThumbSizeActive"/></summary>
        protected virtual int DefaultThumbSizeActive { get { return SystInfo.HorizontalScrollBarHeight - 4; } }
        /// <summary>Defaultní hodnota pro <see cref="ScrollColorInactive"/></summary>
        protected virtual Color DefaultScrollColorInactive { get { return SystemColors.ScrollBar; } }
        /// <summary>Defaultní hodnota pro <see cref="ThumbColorInactive"/></summary>
        protected virtual Color DefaultThumbColorInactive { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="ArrowColorInactive"/></summary>
        protected virtual Color DefaultArrowColorInactive { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="ScrollColorActive"/></summary>
        protected virtual Color DefaultScrollColorActive { get { return SystemColors.ScrollBar; } }
        /// <summary>Defaultní hodnota pro <see cref="ThumbColorActive"/></summary>
        protected virtual Color DefaultThumbColorActive { get { return SystemColors.ButtonHighlight; } }
        /// <summary>Defaultní hodnota pro <see cref="ArrowColorActive"/></summary>
        protected virtual Color DefaultArrowColorActive { get { return SystemColors.HighlightText; } }
        /// <summary>Defaultní hodnota pro <see cref="Properties"/></summary>
        protected virtual ScrollBarProperties DefaultProperties { get { return ScrollBarProperties.None; } }
        #endregion
        #region Implementace interface
        int IScrollBarStyle.ScrollSizeInactive { get { return GetValue<int>(() => ScrollSizeInactive, () => Parent?.ScrollSizeInactive, () => StyleScrollBar.ScrollSizeInactive, DefaultScrollSizeInactive); } }
        int IScrollBarStyle.ThumbSizeInactive { get { return GetValue<int>(() => ThumbSizeInactive, () => Parent?.ThumbSizeInactive, () => StyleScrollBar.ThumbSizeInactive, DefaultThumbSizeInactive); } }
        int IScrollBarStyle.ScrollSizeActive { get { return GetValue<int>(() => ScrollSizeActive, () => Parent?.ScrollSizeActive, () => StyleScrollBar.ScrollSizeActive, DefaultScrollSizeActive); } }
        int IScrollBarStyle.ThumbSizeActive { get { return GetValue<int>(() => ThumbSizeActive, () => Parent?.ThumbSizeActive, () => StyleScrollBar.ThumbSizeActive, DefaultThumbSizeActive); } }
        Color IScrollBarStyle.ScrollColorInactive { get { return GetValue<Color>(() => ScrollColorInactive, () => Parent?.ScrollColorInactive, () => StyleScrollBar.ScrollColorInactive, DefaultScrollColorInactive); } }
        Color IScrollBarStyle.ThumbColorInactive { get { return GetValue<Color>(() => ThumbColorInactive, () => Parent?.ThumbColorInactive, () => StyleScrollBar.ThumbColorInactive, DefaultThumbColorInactive); } }
        Color IScrollBarStyle.ArrowColorInactive { get { return GetValue<Color>(() => ArrowColorInactive, () => Parent?.ArrowColorInactive, () => StyleScrollBar.ArrowColorInactive, DefaultArrowColorInactive); } }
        Color IScrollBarStyle.ScrollColorActive { get { return GetValue<Color>(() => ScrollColorActive, () => Parent?.ScrollColorActive, () => StyleScrollBar.ScrollColorActive, DefaultScrollColorActive); } }
        Color IScrollBarStyle.ThumbColorActive { get { return GetValue<Color>(() => ThumbColorActive, () => Parent?.ThumbColorActive, () => StyleScrollBar.ThumbColorActive, DefaultThumbColorActive); } }
        Color IScrollBarStyle.ArrowColorActive { get { return GetValue<Color>(() => ArrowColorActive, () => Parent?.ArrowColorActive, () => StyleScrollBar.ArrowColorActive, DefaultArrowColorActive); } }
        Image IScrollBarStyle.ThumbImage { get { return (ThumbImage ?? Parent?.ThumbImage ?? StyleScrollBar?.ThumbImage); } }
        ScrollBarProperties IScrollBarStyle.Properties { get { return GetValue<ScrollBarProperties>(() => Properties, () => Parent?.Properties, () => StyleScrollBar.Properties, DefaultProperties); } }
        #endregion
    }
    #endregion
    #region Interface, s nimiž pracuje Painter. Potřebné enumy.
    /// <summary>
    /// Deklarace stylu pro kreslení modifikátorů (interaktivita) a 3D efektů
    /// </summary>
    public interface IModifierStyle
    {
        /// <summary>
        /// Globální povolení pro používání 3D efektů. Lze je tak jednoduše potlačit. Pak všechny property "Effect3D" nebudou používány.
        /// </summary>
        bool Effect3DEnabled { get; }
        /// <summary>
        /// Barva reprezentující světlo ve 3D efektu. 
        /// Touto barvou bude kreslen levý / horní okraj prvku, pokud by Ratio bylo 1f.
        /// </summary>
        Color Effect3DColorLight { get; }
        /// <summary>
        /// Barva reprezentující tmu ve 3D efektu. 
        /// Touto barvou bude kreslen pravý / dolní okraj prvku, pokud by Ratio bylo 1f.
        /// </summary>
        Color Effect3DColorDark { get; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, neaktivní.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        float Effect3DRatioEnabled { get; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, s myší nad prvkem anebo s focusem.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        float Effect3DRatioMouseOn { get; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, se stisknutou myší (nebo stisknuté tlačítko).
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        float Effect3DRatioMouseDown { get; }
        /// <summary>
        /// Poměr úpravy barvy prvku pro 3D efekt pro prvek dostupný, který je přetahován (Drag and Drop) myší.
        /// Vlastní barva prvku bude morfována k barvám <see cref="Effect3DColorLight"/> a <see cref="Effect3DColorDark"/> v tomto poměru.
        /// </summary>
        float Effect3DRatioMouseDrag { get; }

        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je Disabled.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color ModifierColorDisabled { get; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je MouseOn nebo má Focus.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color ModifierColorMouseOn { get; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je MouseDown.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color ModifierColorMouseDown { get; }
        /// <summary>
        /// Modifikátor barvy objektu (typicky BackColor) pro objekt, který je přetahován (Drag and Drop) myší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color ModifierColorMouseDrag { get; }

        /// <summary>
        /// Barva okrajů 3D světlejší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color Border3DColorLight { get; }
        /// <summary>
        /// Barva okrajů 3D tmavší.
        /// <para/>
        /// Pozor, jde o barvu modifikátoru, její složka <see cref="Color.A"/> reprezentuje poměr morfování ze základní barvy objektu:
        /// Hodnota 0 = výchozí barva objektu se nezmění, modifikátor se neuplatní.
        /// Hodnota 255 = výchozí barva objektu se zcela ignoruje, použije se čistá barva modifikátoru.
        /// Pozor, hodnota <see cref="Color.A"/> z výchozí barvy objektu (tzn. jeho průhlednost) se zachovává.
        /// Viz metoda <see cref="DrawingExtensions.Morph(Color, Color)"/>
        /// </summary>
        Color Border3DColorDark { get; }

    }
    /// <summary>
    /// Deklarace stylu pro kreslení ToolTipu
    /// </summary>
    public interface IToolTipStyle
    {

    }
    /// <summary>
    /// Deklarace stylu pro kreslení Labelu
    /// </summary>
    public interface ILabelStyle
    {
        /// <summary>
        /// Reálná barva písma
        /// </summary>
        Color TextColor { get; }
        /// <summary>
        /// Reálný font (již má zapracované modifikátory)
        /// </summary>
        FontInfo Font { get; }
    }
    /// <summary>
    /// Deklarace stylu pro kreslení interaktivních prvků s písmem, pozadím a rámečkem
    /// </summary>
    public interface ITextBorderStyle : ILabelStyle
    {
        /// <summary>
        /// Reálná barva pozadí, pro textbox ve stavu Enabled
        /// </summary>
        Color BackColor { get; }
       /// <summary>
        /// Reálná barva pozadí, pro textbox ve stavu Disabled.
        /// </summary>
        Color BackColorDisabled { get; }
        /// <summary>
        /// Reálná barva pozadí, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        Color BackColorMouseOn { get; }
        /// <summary>
        /// Reálná barva pozadí, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        Color BackColorMouseDown { get; }

        /// <summary>
        /// Reálná barva písma, pro textbox ve stavu Disabled.
        /// </summary>
        Color TextColorDisabled { get; }
        /// <summary>
        /// Reálná barva písma, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        Color TextColorMouseOn { get; }
        /// <summary>
        /// Reálná barva písma, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        Color TextColorMouseDown { get; }
     
        /// <summary>
        /// Reálná barva rámečku, pro textbox ve stavu Enabled
        /// </summary>
        Color BorderColor { get; }
        /// <summary>
        /// Reálná barva rámečku, pro textbox ve stavu Disabled.
        /// </summary>
        Color BorderColorDisabled { get; }
        /// <summary>
        /// Reálná barva rámečku, pro textbox ve stavu MouseOn (tzv. HotTracking).
        /// </summary>
        Color BorderColorMouseOn { get; }
        /// <summary>
        /// Reálná barva rámečku, pro textbox ve stavu MouseDown a Focused.
        /// </summary>
        Color BorderColorMouseDown { get; }

        /// <summary>
        /// Reálný druh okraje
        /// </summary>
        TextBoxBorderType BorderType { get; }

        /// <summary>
        /// Prostor mezi vnitřkem Borderu a začátkem textu = počet prázdných pixelů
        /// </summary>
        int TextMargin { get; }
    }
    /// <summary>
    /// Deklarace stylu pro kreslení Textboxu
    /// </summary>
    public interface ITextBoxStyle : ILabelStyle
    {
        /// <summary>
        /// Reálná barva pozadí, pro textbox ve stavu Warning
        /// </summary>
        Color BackColorWarning { get; }
        /// <summary>
        /// Reálná barva pozadí, pro znaky, které jsou Selected.
        /// </summary>
        Color BackColorSelectedText { get; }
        /// <summary>
        /// Reálná barva písma, pro znaky, které jsou Selected.
        /// </summary>
        Color TextColorSelectedText { get; }
        /// <summary>
        /// Reálná barva rámečku, pro textbox ve stavu Warning
        /// </summary>
        Color BorderColorWarning { get; }

    }
    /// <summary>
    /// Deklarace stylu pro kreslení Buttonů
    /// </summary>
    public interface IButtonStyle : ITextBorderStyle
    {
        /// <summary>
        /// Počet pixelů zaobleného rohu, default 0
        /// </summary>
        int RoundCorner { get; }
    }
    /// <summary>
    /// Deklarace stylu pro kreslení ScrollBarů
    /// </summary>
    public interface IScrollBarStyle
    {
        /// <summary>
        /// Vnější velikost (šířka celého Scrollbaru u vodorovného, výška u svislého), neaktivní = bez myši
        /// </summary>
        int ScrollSizeInactive { get; }
        /// <summary>
        /// Vnitřní velikost (šířka vizuálního jezdce u vodorovného, výška u svislého), neaktivní = bez myši
        /// </summary>
        int ThumbSizeInactive { get; }
        /// <summary>
        /// Vnější velikost (šířka celého Scrollbaru u vodorovného, výška u svislého), aktivní = s myší
        /// </summary>
        int ScrollSizeActive { get; }
        /// <summary>
        /// Vnitřní velikost (šířka vizuálního jezdce u vodorovného, výška u svislého), aktivní = s myší
        /// </summary>
        int ThumbSizeActive { get; }
        /// <summary>
        /// Barva pozadí ScrollBaru, neaktivní
        /// </summary>
        Color ScrollColorInactive { get; }
        /// <summary>
        /// Barva jezdce ScrollBaru, neaktivní
        /// </summary>
        Color ThumbColorInactive { get; }
        /// <summary>
        /// Barva šipek a linek ScrollBaru, neaktivní
        /// </summary>
        Color ArrowColorInactive { get; }
        /// <summary>
        /// Barva pozadí ScrollBaru, aktivní
        /// </summary>
        Color ScrollColorActive { get; }
        /// <summary>
        /// Barva jezdce ScrollBaru, aktivní
        /// </summary>
        Color ThumbColorActive { get; }
        /// <summary>
        /// Barva šipek a linek ScrollBaru, aktivní
        /// </summary>
        Color ArrowColorActive { get; }
        /// <summary>
        /// Ikona kreslená uprostřed Thumbu, pokud je na ni místo a je povolena ve vlastnostech
        /// </summary>
        Image ThumbImage { get; }
        /// <summary>
        /// Vlastnosti vzhledu ScrollBaru
        /// </summary>
        ScrollBarProperties Properties { get; }
    }

    /// <summary>
    /// Druhy okraje (rámeček) okolo textboxu
    /// </summary>
    [Flags]
    public enum TextBoxBorderType
    {
        /// <summary>
        /// Nikdy není kreslen
        /// </summary>
        None = 0,
        /// <summary>
        /// Jednopixelový
        /// </summary>
        Single = 0x0001,
        /// <summary>
        /// Dvojpixelový
        /// </summary>
        Double = 0x0002,
        /// <summary>
        /// Měkký (poloprůhledný, nedotažené rohy)
        /// </summary>
        Soft = 0x0010,
        /// <summary>
        /// 3D efekt (levý a horní = tmavší, pravý a dolní = světlejší)
        /// </summary>
        Effect3D = 0x0020,
        /// <summary>
        /// Pouze interaktivní (je kreslen pouze při aktivaci myší, bez myši není žádný)
        /// </summary>
        InteractiveOnly = 0x0100,
        /// <summary>
        /// Zvýrazněný interaktivní (bez myši je vykreslen s 50% průhledností proti normálu, při aktivaci myší je vykreslen naplno)
        /// </summary>
        InteractiveHalf = 0x0200
    }
    /// <summary>
    /// Vlastnosti ScrollBaru
    /// </summary>
    [Flags]
    public enum ScrollBarProperties
    {
        /// <summary>
        /// Žádné vizuální ozdoby
        /// </summary>
        None = 0,
        /// <summary>
        /// Uprostřed Thumbu zobrazovat linky
        /// </summary>
        ThumbWithLines = 0x0001,
        /// <summary>
        /// Uprostřed Thumbu zobrazovat ikonu (pokud je, pak ignorovat <see cref="ThumbWithLines"/>)
        /// </summary>
        ThumbWithIcon = 0x0002,
        /// <summary>
        /// Vykreslit border kolem vnějších okrajů Scrollbaru
        /// </summary>
        DrawOuterBorder = 0x0010,
        /// <summary>
        /// Vykreslit border kolem vnitřních prvků Scrollbaru
        /// </summary>
        DrawInnerBorder = 0x0020,
        /// <summary>
        /// Vykreslit border ve stylu 3D
        /// </summary>
        Draw3DBorder = 0x0040,
        /// <summary>
        /// Pracovní prostor před a za Thumb jako aktivní
        /// </summary>
        AreaColorActive = 0x0100,
        /// <summary>
        /// Šipky (na koncích ScrollBaru) s aktivní barvou
        /// </summary>
        ArrowColorActive = 0x1000,
        /// <summary>
        /// Šipky (na koncích ScrollBaru) vykreslit podobně jako Thumb, podle volby 
        /// </summary>
        ArrowAsButton = 0x2000
    }
    #endregion
}
