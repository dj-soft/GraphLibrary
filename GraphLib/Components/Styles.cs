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
            _Styles = new List<IStyleMember>();

            _Modifier = new ModifierStyle(); _Styles.Add(_Modifier);
            _ToolTip = new ToolTipStyle(); _Styles.Add(_Modifier);
            _Label = new LabelStyle(); _Styles.Add(_Modifier);
            _TextBox = new TextBoxStyle(); _Styles.Add(_Modifier);
            _Button = new ButtonStyle(); _Styles.Add(_Modifier);
            _Panel = new PanelStyle(); _Styles.Add(_Modifier);
            _ScrollBar = new ScrollBarStyle(); _Styles.Add(_Modifier);

            _Styles.ForEach(s => s.IsStyleInstance = true);
            _CurrentType = StyleType.System;
        }
        private List<IStyleMember> _Styles;
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
        /// Výchozí styl pro Panel
        /// </summary>
        public static PanelStyle Panel { get { return Instance._Panel; } } private PanelStyle _Panel;

        /// <summary>
        /// Výchozí styl pro ScrollBar
        /// </summary>
        public static ScrollBarStyle ScrollBar { get { return Instance._ScrollBar; } } private ScrollBarStyle _ScrollBar;

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Aktuálně platný výchozí styl.
        /// </summary>
        public static StyleType CurrentType { get { return Instance._CurrentType; } set { Instance._CurrentType = value; } }
        /// <summary>
        /// Aktuálně platný výchozí styl.
        /// Setování se promítá do základních stylů.
        /// </summary>
        protected StyleType _CurrentType
        {
            get { return __CurrentType; }
            set
            {
                __CurrentType = value;
                _Styles.ForEach(s => s.IsStyleInstance = true);
            }
        }
        private StyleType __CurrentType;
        #endregion
    }
    /// <summary>
    /// 
    /// </summary>
    public enum StyleType
    {
        /// <summary>
        /// Systémový typ = opírá se o hodnoty ze <see cref="System.Drawing.SystemColors"/> a <see cref="System.Windows.Forms.SystemInformation"/>
        /// </summary>
        System,
        /// <summary>
        /// Světlý neutrální
        /// </summary>
        Light3D,
        /// <summary>
        /// Světlý neutrální bez 3D efektů
        /// </summary>
        LightFlat,
        /// <summary>
        /// Tmavý neutrální
        /// </summary>
        Dark3D,
        /// <summary>
        /// Tmavý neutrální bez 3D efektů
        /// </summary>
        DarkFlat
    }
    #region ItemStyle : předek stylů
    /// <summary>
    /// <see cref="ItemStyle"/> : předek stylů
    /// </summary>
    public abstract class ItemStyle : IStyleMember
    {
        /// <summary>
        /// Konstruktor pro běžnou instanci
        /// </summary>
        public ItemStyle()
        {
            IsStyleInstance = false;
        }
        /// <summary>
        /// Obsahuje true tehdy, když všechny hodnoty v this instanci jsou prázdné. 
        /// Pak taková instance vůbec nemusí existovat, protože ji zastoupí odpovídající instance parent nebo ve <see cref="Styles"/>.
        /// </summary>
        public bool IsEmpty { get { return !HasValue; } }
        /// <summary>
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected abstract bool HasValue { get; }
        /// <summary>
        /// Obsahuje true v té instanci, které reprezentuje základní Styl = je umístěna v třídě <see cref="Styles"/>.
        /// Tato instance při vyhodnocování Current hodnoty v metodách <see cref="GetValue{T}(Func{T?}, Func{T?}, Func{T?}, Func{T})"/> 
        /// neřeší hodnoty z instance Parent ani z instance Style, řeší pouze dodanou explicitní hodnotu a defaultní hodnotu.
        /// </summary>
        protected bool IsStyleInstance { get; private set; }
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected virtual void ActivateStyle(StyleType styleType) { }
        /// <summary>
        /// Postupně vyhodnotí dodané funkce, které vracejí hodnotu: moji explicitní, mého parenta, základního stylu a defaultní hodnotu. Vrátí první nalezenou.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="myFunc"></param>
        /// <param name="parentFunc"></param>
        /// <param name="styleFunc"></param>
        /// <param name="defaultFunc"></param>
        /// <returns></returns>
        protected T GetValue<T>(Func<T?> myFunc, Func<T?> parentFunc, Func<T?> styleFunc, Func<T> defaultFunc) where T : struct
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

            return defaultFunc();
        }
        /// <summary>
        /// Postupně vyhodnotí dodané funkce, které vracejí hodnotu: moji explicitní, mého parenta, základního stylu a defaultní hodnotu. Vrátí první nalezenou.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="myFunc"></param>
        /// <param name="parentFunc"></param>
        /// <param name="styleFunc"></param>
        /// <param name="defaultFunc"></param>
        /// <returns></returns>
        protected T GetInstance<T>(Func<T> myFunc, Func<T> parentFunc, Func<T> styleFunc, Func<T> defaultFunc) where T : class
        {
            var myValue = myFunc();
            if (myValue != null) return myValue;

            if (!IsStyleInstance)
            {
                var parentValue = parentFunc();
                if (parentValue != null) return parentValue;

                var styleValue = styleFunc();
                if (styleValue != null) return styleValue;
            }

            return defaultFunc();
        }
        /// <summary>
        /// Vrátí výsledek z odpovídající funkce podle interaktivního stavu.
        /// <para/>
        /// Je vhodnější pro získání jedné z několika hodnot použít předání funkce (funkce vracející potřebnou hodnotu), která bude vyhodnocena až podle potřeby, 
        /// než nejprve vyhodnotit všechny hodnoty (což může chvilku trvat) a pak z nich vybrat jednu a ostatní zahodit.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interactiveState">Interaktivní stav</param>
        /// <param name="enabledFunc">Funkce, vracející hodnotu Enabled, bez myši</param>
        /// <param name="disabledFunc">Funkce, vracející hodnotu Disabled</param>
        /// <param name="mouseOnFunc">Funkce, vracející hodnotu MouseOn</param>
        /// <param name="mouseDownFunc">Funkce, vracející hodnotu MouseDown (nebo Focused)</param>
        /// <param name="focusedFunc">Funkce, vracející hodnotu Focused, pokud bude null použije se <paramref name="mouseDownFunc"/></param>
        /// <param name="mouseDragFunc">Funkce, vracející hodnotu MouseDrag, pokud bude null použije se <paramref name="mouseDownFunc"/></param>
        /// <returns></returns>
        protected T GetByState<T>(GInteractiveState interactiveState, Func<T> enabledFunc, Func<T> disabledFunc, Func<T> mouseOnFunc, Func<T> mouseDownFunc, Func<T> focusedFunc = null, Func<T> mouseDragFunc = null)
        {   // Pořadí řádků odpovídá logice očekávání uživatele, a prioritě stavů mezi sebou:
            if (interactiveState.HasFlag(GInteractiveState.Disabled)) return disabledFunc();
            if (interactiveState.HasFlag(GInteractiveState.Focused)) return (focusedFunc != null ? focusedFunc() : mouseDownFunc());
            if (interactiveState.HasFlag(GInteractiveState.FlagDrag)) return (mouseDragFunc != null ? mouseDragFunc() : mouseDownFunc());
            if (interactiveState.HasFlag(GInteractiveState.FlagDown)) return mouseDownFunc();
            if (interactiveState.HasFlag(GInteractiveState.MouseOver)) return mouseOnFunc();
            return enabledFunc();
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
        /// <summary>
        /// Vrátí danou hodnotu <paramref name="value"/> zarovnanou do mezí <paramref name="min"/> až <paramref name="max"/>, obě meze včetně
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        protected int Align(int value, int min, int max)
        {
            return (min > max ? min : (value < min ? min : (value > max ? max : value)));
        }
        bool IStyleMember.IsStyleInstance { get { return IsStyleInstance; } set { IsStyleInstance = value; } }
        StyleType IStyleMember.CurrentType { set { if (IsStyleInstance) ActivateStyle(value); } }
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
        /// <summary>
        /// Vložení hodnoty typu stylu provede nastavení vhodné hodnoty (barvy atd) do všech vlastností stylu.
        /// </summary>
        StyleType CurrentType { set; }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (Effect3DEnabled.HasValue || Effect3DColorLight.HasValue || Effect3DColorDark.HasValue || 
                    Effect3DRatioEnabled.HasValue || Effect3DRatioMouseOn.HasValue || Effect3DRatioMouseDown.HasValue || Effect3DRatioMouseDrag.HasValue ||
                    ModifierColorDisabled.HasValue || ModifierColorMouseOn.HasValue || ModifierColorMouseDown.HasValue || ModifierColorMouseDrag.HasValue ||
                    Border3DColorLight.HasValue || Border3DColorDark.HasValue); } }

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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            switch (styleType)
            {
                case StyleType.System:
                    Effect3DEnabled = DefaultEffect3DEnabled;
                    Effect3DColorLight = DefaultEffect3DColorLight;
                    Effect3DColorDark = DefaultEffect3DColorDark;
                    Effect3DRatioEnabled = DefaultEffect3DRatioEnabled;
                    Effect3DRatioMouseOn = DefaultEffect3DRatioMouseOn;
                    Effect3DRatioMouseDown = DefaultEffect3DRatioMouseDown;
                    Effect3DRatioMouseDrag = DefaultEffect3DRatioMouseDrag;
                    ModifierColorDisabled = DefaultModifierColorDisabled;
                    ModifierColorMouseOn = DefaultModifierColorMouseOn;
                    ModifierColorMouseDown = DefaultModifierColorMouseDown;
                    ModifierColorMouseDrag = DefaultModifierColorMouseDrag;
                    Border3DColorLight = DefaultBorder3DColorLight;
                    Border3DColorDark = DefaultBorder3DColorDark;
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    ModifierColorDisabled = Color.FromArgb(64, 112, 112, 112);
                    ModifierColorMouseOn = Color.FromArgb(64, 255, 255, 224);
                    ModifierColorMouseDown = Color.FromArgb(32, 0, 0, 0);
                    ModifierColorMouseDrag = Color.FromArgb(64, 238, 130, 238);
                    if (styleType == StyleType.Light3D)
                    {
                        Effect3DEnabled = true;
                        Effect3DColorLight = Color.White;
                        Effect3DColorDark = Color.FromArgb(16, 24, 24);
                        Effect3DRatioEnabled = 0.15f;
                        Effect3DRatioMouseOn = 0.35f;
                        Effect3DRatioMouseDown = -0.25f;
                        Effect3DRatioMouseDrag = -0.10f;
                        Border3DColorLight = Color.FromArgb(160, 216, 216, 216);
                        Border3DColorDark = Color.FromArgb(160, 40, 40, 40);
                    }
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    ModifierColorDisabled = Color.FromArgb(64, 140, 140, 140);
                    ModifierColorMouseOn = Color.FromArgb(64, 0, 0, 32);
                    ModifierColorMouseDown = Color.FromArgb(32, 96, 96, 96);
                    ModifierColorMouseDrag = Color.FromArgb(64, 32, 32, 32);
                    if (styleType == StyleType.Dark3D)
                    {
                        Effect3DEnabled = true;
                        Effect3DColorLight = Color.White;
                        Effect3DColorDark = Color.FromArgb(16, 24, 24);
                        Effect3DRatioEnabled = 0.15f;
                        Effect3DRatioMouseOn = 0.35f;
                        Effect3DRatioMouseDown = -0.25f;
                        Effect3DRatioMouseDrag = -0.10f;
                        Border3DColorLight = Color.FromArgb(160, 128, 128, 128);
                        Border3DColorDark = Color.FromArgb(160, 4, 4, 4);
                    }
                    break;
            }

            if (styleType == StyleType.LightFlat || styleType == StyleType.DarkFlat)
            {
                Effect3DEnabled = false;
                Effect3DColorLight = Color.Transparent;
                Effect3DColorDark = Color.Transparent;
                Effect3DRatioEnabled = 0f;
                Effect3DRatioMouseOn = 0f;
                Effect3DRatioMouseDown = 0f;
                Effect3DRatioMouseDrag = 0f;
                Border3DColorLight = Color.Transparent;
                Border3DColorDark = Color.Transparent;
            }
        }
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
        /// <summary>Defaultní barva pro <see cref="Effect3DEnabled"/></summary>
        protected virtual bool DefaultEffect3DEnabled { get { return true; } }
        /// <summary>Defaultní barva pro <see cref="Effect3DColorLight"/></summary>
        protected virtual Color DefaultEffect3DColorLight { get { return GetDefault<Color>(ref _DefaultEffect3DColorLight, () => Color.White); } } private static Color? _DefaultEffect3DColorLight = null;
        /// <summary>Defaultní barva pro <see cref="Effect3DColorDark"/></summary>
        protected virtual Color DefaultEffect3DColorDark { get { return GetDefault<Color>(ref _DefaultEffect3DColorDark, () => Color.DarkSlateGray); } } private static Color? _DefaultEffect3DColorDark = null;
        /// <summary>Defaultní barva pro <see cref="Effect3DRatioEnabled"/></summary>
        protected virtual float DefaultEffect3DRatioEnabled { get { return 0.15f; } }
        /// <summary>Defaultní barva pro <see cref="Effect3DRatioMouseOn"/></summary>
        protected virtual float DefaultEffect3DRatioMouseOn { get { return 0.35f; } }
        /// <summary>Defaultní barva pro <see cref="Effect3DRatioMouseDown"/></summary>
        protected virtual float DefaultEffect3DRatioMouseDown { get { return -0.25f; } }
        /// <summary>Defaultní barva pro <see cref="Effect3DRatioMouseDrag"/></summary>
        protected virtual float DefaultEffect3DRatioMouseDrag { get { return -0.10f; } }
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
        protected virtual Color DefaultBorder3DColorDark { get { return GetDefault<Color>(ref _DefaultBorder3DColorDark, () => Color.FromArgb(192, SystemColors.ControlLightLight)); } } private static Color? _DefaultBorder3DColorDark = null;
        #endregion
        #region Implementace interface
        bool IModifierStyle.Effect3DEnabled { get { return GetValue<bool>(() => Effect3DEnabled, () => Parent?.Effect3DEnabled, () => StyleModifier.Effect3DEnabled, () => DefaultEffect3DEnabled); } }
        Color IModifierStyle.Effect3DColorLight { get { return GetValue<Color>(() => Effect3DColorLight, () => Parent?.Effect3DColorLight, () => StyleModifier.Effect3DColorLight, () => DefaultEffect3DColorLight); } }
        Color IModifierStyle.Effect3DColorDark { get { return GetValue<Color>(() => Effect3DColorDark, () => Parent?.Effect3DColorDark, () => StyleModifier.Effect3DColorDark, () => DefaultEffect3DColorDark); } }
        float IModifierStyle.Effect3DRatioEnabled { get { return GetValue<float>(() => Effect3DRatioEnabled, () => Parent?.Effect3DRatioEnabled, () => StyleModifier.Effect3DRatioEnabled, () => DefaultEffect3DRatioEnabled); } }
        float IModifierStyle.Effect3DRatioMouseOn { get { return GetValue<float>(() => Effect3DRatioMouseOn, () => Parent?.Effect3DRatioMouseOn, () => StyleModifier.Effect3DRatioMouseOn, () => DefaultEffect3DRatioMouseOn); } }
        float IModifierStyle.Effect3DRatioMouseDown { get { return GetValue<float>(() => Effect3DRatioMouseDown, () => Parent?.Effect3DRatioMouseDown, () => StyleModifier.Effect3DRatioMouseDown, () => DefaultEffect3DRatioMouseDown); } }
        float IModifierStyle.Effect3DRatioMouseDrag { get { return GetValue<float>(() => Effect3DRatioMouseDrag, () => Parent?.Effect3DRatioMouseDrag, () => StyleModifier.Effect3DRatioMouseDrag, () => DefaultEffect3DRatioMouseDrag); } }
        Color IModifierStyle.ModifierColorDisabled { get { return GetValue<Color>(() => ModifierColorDisabled, () => Parent?.ModifierColorDisabled, () => StyleModifier.ModifierColorDisabled, () => DefaultModifierColorDisabled); } }
        Color IModifierStyle.ModifierColorMouseOn { get { return GetValue<Color>(() => ModifierColorMouseOn, () => Parent?.ModifierColorMouseOn, () => StyleModifier.ModifierColorMouseOn, () => DefaultModifierColorMouseOn); } }
        Color IModifierStyle.ModifierColorMouseDown { get { return GetValue<Color>(() => ModifierColorMouseDown, () => Parent?.ModifierColorMouseDown, () => StyleModifier.ModifierColorMouseDown, () => DefaultModifierColorMouseDown); } }
        Color IModifierStyle.ModifierColorMouseDrag { get { return GetValue<Color>(() => ModifierColorMouseDrag, () => Parent?.ModifierColorMouseDrag, () => StyleModifier.ModifierColorMouseDrag, () => DefaultModifierColorMouseDrag); } }
        Color IModifierStyle.Border3DColorLight { get { return GetValue<Color>(() => Border3DColorLight, () => Parent?.Border3DColorLight, () => StyleModifier.Border3DColorLight, () => DefaultBorder3DColorLight); } }
        Color IModifierStyle.Border3DColorDark { get { return GetValue<Color>(() => Border3DColorDark, () => Parent?.Border3DColorDark, () => StyleModifier.Border3DColorDark, () => DefaultBorder3DColorDark); } }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (Font != null || (FontModifier != null && !FontModifier.IsEmpty) || 
                    BackColor.HasValue || TextColor.HasValue); } }
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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            switch (styleType)
            {
                case StyleType.System:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    BackColor = DefaultBackColor;
                    TextColor = DefaultTextColor;
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    BackColor = Color.FromArgb(240, 242, 160);
                    TextColor = Color.Black;
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    BackColor = Color.FromArgb(6, 10, 86);
                    TextColor = Color.Black;
                    break;
            }
        }
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
        /// <summary>Defaultní hodnota pro <see cref="Font"/></summary>
        protected virtual FontInfo DefaultFont { get { return GetDefault<FontInfo>(ref _DefaultFont, () => FontInfo.Default); } } private static FontInfo _DefaultFont;
        /// <summary>Defaultní hodnota pro <see cref="FontModifier"/></summary>
        protected virtual FontModifierInfo DefaultFontModifier { get { return null; } }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (Font != null || (FontModifier != null && !FontModifier.IsEmpty) || TextColor.HasValue); } }
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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            switch (styleType)
            {
                case StyleType.System:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    TextColor = DefaultTextColor;
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    TextColor = Color.Black;
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    Font = DefaultFont;
                    FontModifier = DefaultFontModifier;
                    TextColor = Color.White;
                    break;
            }
        }
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
        /// <summary>Defaultní barva pro <see cref="FontModifier"/></summary>
        protected virtual FontModifierInfo DefaultFontModifier { get { return null; } }
        /// <summary>Defaultní hodnota pro <see cref="TextColor"/></summary>
        protected virtual Color DefaultTextColor { get { return SystemColors.ControlText; } }
        #endregion
        #region Implementace interface
        Color ILabelStyle.TextColor { get { return GetValue<Color>(() => TextColor, () => Parent?.TextColor, () => StyleLabel.TextColor, () => DefaultTextColor); } }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (base.HasValue ||
                    BackColor.HasValue || BackColorDisabled.HasValue || BackColorMouseOn.HasValue || BackColorMouseDown.HasValue ||
                    TextColorDisabled.HasValue || TextColorMouseOn.HasValue || TextColorMouseDown.HasValue ||
                    BorderColor.HasValue || BorderColorDisabled.HasValue || BorderColorMouseOn.HasValue || BorderColorMouseDown.HasValue ||
                    BorderType.HasValue || TextMargin.HasValue); } }
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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            base.ActivateStyle(styleType);

            switch (styleType)
            {
                case StyleType.System:
                    BackColor = DefaultBackColor;
                    BackColorDisabled = DefaultBackColorDisabled;
                    BackColorMouseOn = DefaultBackColorMouseOn;
                    BackColorMouseDown = DefaultBackColorMouseDown;
                    TextColorDisabled = DefaultTextColorDisabled;
                    TextColorMouseOn = DefaultTextColorMouseOn;
                    TextColorMouseDown = DefaultTextColorMouseDown;
                    BorderColor = DefaultBorderColor;
                    BorderColorDisabled = DefaultBorderColorDisabled;
                    BorderColorMouseOn = DefaultBorderColorMouseOn;
                    BorderColorMouseDown = DefaultBorderColorMouseDown;
                    BorderType = DefaultBorderType;
                    TextMargin = DefaultTextMargin;
                    break;
                case StyleType.Light3D:
                    break;
                case StyleType.LightFlat:
                    break;
                case StyleType.Dark3D:
                    break;
                case StyleType.DarkFlat:
                    break;
            }
        }
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
        /// <summary>Maximální přípustná hodnota pro <see cref="TextMargin"/></summary>
        protected virtual int MaxTextMargin { get { return 6; } }
        #endregion
        #region Implementace interface
        Color ITextBorderStyle.BackColor { get { return GetValue<Color>(() => BackColor, () => Parent?.BackColor, () => StyleTextBorder.BackColor, () => DefaultBackColor); } }
        Color ITextBorderStyle.BackColorDisabled { get { return GetValue<Color>(() => BackColorDisabled, () => Parent?.BackColorDisabled, () => StyleTextBorder.BackColorDisabled, () => DefaultBackColorDisabled); } }
        Color ITextBorderStyle.BackColorMouseOn { get { return GetValue<Color>(() => BackColorMouseOn, () => Parent?.BackColorMouseOn, () => StyleTextBorder.BackColorMouseOn, () => DefaultBackColorMouseOn); } }
        Color ITextBorderStyle.BackColorMouseDown { get { return GetValue<Color>(() => BackColorMouseDown, () => Parent?.BackColorMouseDown, () => StyleTextBorder.BackColorMouseDown, () => DefaultBackColorMouseDown); } }

        Color ITextBorderStyle.TextColorDisabled { get { return GetValue<Color>(() => TextColorDisabled, () => Parent?.TextColorDisabled, () => StyleTextBorder.TextColorDisabled, () => DefaultTextColorDisabled); } }
        Color ITextBorderStyle.TextColorMouseOn { get { return GetValue<Color>(() => TextColorMouseOn, () => Parent?.TextColorMouseOn, () => StyleTextBorder.TextColorMouseOn, () => DefaultTextColorMouseOn); } }
        Color ITextBorderStyle.TextColorMouseDown { get { return GetValue<Color>(() => TextColorMouseDown, () => Parent?.TextColorMouseDown, () => StyleTextBorder.TextColorMouseDown, () => DefaultTextColorMouseDown); } }

        Color ITextBorderStyle.BorderColor { get { return GetValue<Color>(() => BorderColor, () => Parent?.BorderColor, () => StyleTextBorder.BorderColor, () => DefaultBorderColor); } }
        Color ITextBorderStyle.BorderColorDisabled { get { return GetValue<Color>(() => BorderColorDisabled, () => Parent?.BorderColorDisabled, () => StyleTextBorder.BorderColorDisabled, () => DefaultBorderColorDisabled); } }
        Color ITextBorderStyle.BorderColorMouseOn { get { return GetValue<Color>(() => BorderColorMouseOn, () => Parent?.BorderColorMouseOn, () => StyleTextBorder.BorderColorMouseOn, () => DefaultBorderColorMouseOn); } }
        Color ITextBorderStyle.BorderColorMouseDown { get { return GetValue<Color>(() => BorderColorMouseDown, () => Parent?.BorderColorMouseDown, () => StyleTextBorder.BorderColorMouseDown, () => DefaultBorderColorMouseDown); } }

        Color ITextBorderStyle.GetBackColor(GInteractiveState interactiveState) { return GetByState<Color>(interactiveState, () => ((ITextBorderStyle)this).BackColor, () => ((ITextBorderStyle)this).BackColorDisabled, () => ((ITextBorderStyle)this).BackColorMouseOn, () => ((ITextBorderStyle)this).BackColorMouseDown); }
        Color ITextBorderStyle.GetTextColor(GInteractiveState interactiveState) { return GetByState<Color>(interactiveState, () => ((ILabelStyle)this).TextColor, () => ((ITextBorderStyle)this).TextColorDisabled, () => ((ITextBorderStyle)this).TextColorMouseOn, () => ((ITextBorderStyle)this).TextColorMouseDown); }
        Color ITextBorderStyle.GetBorderColor(GInteractiveState interactiveState) { return GetByState<Color>(interactiveState, () => ((ITextBorderStyle)this).BorderColor, () => ((ITextBorderStyle)this).BorderColorDisabled, () => ((ITextBorderStyle)this).BorderColorMouseOn, () => ((ITextBorderStyle)this).BorderColorMouseDown); }

        TextBoxBorderType ITextBorderStyle.BorderType { get { return GetValue<TextBoxBorderType>(() => BorderType, () => Parent?.BorderType, () => StyleTextBorder.BorderType, () => DefaultBorderType); } }
        int ITextBorderStyle.TextMargin { get { return Align(GetValue<int>(() => TextMargin, () => Parent?.TextMargin, () => StyleTextBorder.TextMargin, () => DefaultTextMargin), 0, MaxTextMargin); } }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (base.HasValue || 
                    BackColorWarning.HasValue || BackColorSelectedText.HasValue || TextColorSelectedText.HasValue || BorderColorWarning.HasValue); } }
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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            base.ActivateStyle(styleType);

            switch (styleType)
            {
                case StyleType.System:
                    BackColorWarning = DefaultBackColorWarning;
                    BackColorSelectedText = DefaultBackColorSelectedText;
                    TextColorSelectedText = DefaultTextColorSelectedText;
                    BorderColorWarning = DefaultBorderColorWarning;
                    // Další property již nastavila base metoda ze zdejších override defaultů...
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    BackColor = Color.FromArgb(248, 248, 248);
                    BackColorDisabled = Color.FromArgb(192, 192, 192);
                    BackColorMouseOn = Color.FromArgb(255, 248, 248);
                    BackColorMouseDown = Color.FromArgb(255, 255, 255);
                    TextColor = Color.Black;
                    TextColorDisabled = Color.FromArgb(64, 64, 64);
                    TextColorMouseOn = Color.Black;
                    TextColorMouseDown = Color.Black;
                    BorderColor = Color.FromArgb(122, 122, 122);
                    BorderColorDisabled = Color.FromArgb(96, 96, 96);
                    BorderColorMouseOn = Color.FromArgb(24, 24, 24);
                    BorderColorMouseDown = Color.FromArgb(0, 120, 215);
                    BorderType = (styleType == StyleType.Light3D ? TextBoxBorderType.Single : TextBoxBorderType.Soft);
                    TextMargin = 1;
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    BackColor = Color.FromArgb(24, 24, 24);
                    BackColorDisabled = Color.FromArgb(48, 48, 48);
                    BackColorMouseOn = Color.FromArgb(32, 32, 32);
                    BackColorMouseDown = Color.FromArgb(16, 16, 16);
                    TextColor = Color.White;
                    TextColorDisabled = Color.FromArgb(224, 224, 224);
                    TextColorMouseOn = Color.White;
                    TextColorMouseDown = Color.White;
                    BorderColor = Color.FromArgb(64, 64, 64);
                    BorderColorDisabled = Color.FromArgb(96, 96, 96);
                    BorderColorMouseOn = Color.FromArgb(64, 64, 64);
                    BorderColorMouseDown = Color.FromArgb(64, 64, 64);
                    BorderType = (styleType == StyleType.Dark3D ? TextBoxBorderType.Single : TextBoxBorderType.Soft);
                    TextMargin = 1;
                    break;
            }
        }
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
        Color ITextBoxStyle.BackColorWarning { get { return GetValue<Color>(() => BackColorWarning, () => Parent?.BackColorWarning, () => StyleTextBox.BackColorWarning, () => DefaultBackColorWarning); } }
        Color ITextBoxStyle.BackColorSelectedText { get { return GetValue<Color>(() => BackColorSelectedText, () => Parent?.BackColorSelectedText, () => StyleTextBox.BackColorSelectedText, () => DefaultBackColorSelectedText); } }
        Color ITextBoxStyle.TextColorSelectedText { get { return GetValue<Color>(() => TextColorSelectedText, () => Parent?.TextColorSelectedText, () => StyleTextBox.TextColorSelectedText, () => DefaultTextColorSelectedText); } }
        Color ITextBoxStyle.BorderColorWarning { get { return GetValue<Color>(() => BorderColorWarning, () => Parent?.BorderColorWarning, () => StyleTextBox.BorderColorWarning, () => DefaultBorderColorWarning); } }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (base.HasValue ||
                    RoundCorner.HasValue); } }
        /// <summary>
        /// Počet pixelů zaobleného rohu, default 0
        /// </summary>
        public int? RoundCorner { get; set; }

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            base.ActivateStyle(styleType);

            switch (styleType)
            {
                case StyleType.System:
                    RoundCorner = DefaultRoundCorner;
                    // Další property již nastavila base metoda ze zdejších override defaultů...
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    RoundCorner = 0;
                    BackColor = Color.FromArgb(192, 192, 192);
                    BackColorDisabled = Color.FromArgb(160, 160, 160);
                    BackColorMouseOn = Color.FromArgb(214, 214, 214);
                    BackColorMouseDown = Color.FromArgb(128, 128, 128);
                    TextColor = Color.Black;
                    TextColorDisabled = Color.FromArgb(64, 64, 64);
                    TextColorMouseOn = Color.Black;
                    TextColorMouseDown = Color.Black;
                    BorderColor = Color.FromArgb(64, 64, 64);
                    BorderColorDisabled = Color.FromArgb(96, 96, 96);
                    BorderColorMouseOn = Color.FromArgb(64, 64, 64);
                    BorderColorMouseDown = Color.FromArgb(64, 64, 64);
                    BorderType = (styleType == StyleType.Light3D ? TextBoxBorderType.Single : TextBoxBorderType.Soft);
                    TextMargin = 1;
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    RoundCorner = 0;
                    BackColor = Color.FromArgb(24, 24, 24);
                    BackColorDisabled = Color.FromArgb(48, 48, 48);
                    BackColorMouseOn = Color.FromArgb(32, 32, 32);
                    BackColorMouseDown = Color.FromArgb(16, 16, 16);
                    TextColor = Color.White;
                    TextColorDisabled = Color.FromArgb(224, 224, 224);
                    TextColorMouseOn = Color.White;
                    TextColorMouseDown = Color.White;
                    BorderColor = Color.FromArgb(64, 64, 64);
                    BorderColorDisabled = Color.FromArgb(96, 96, 96);
                    BorderColorMouseOn = Color.FromArgb(64, 64, 64);
                    BorderColorMouseDown = Color.FromArgb(64, 64, 64);
                    BorderType = (styleType == StyleType.Dark3D ? TextBoxBorderType.Single : TextBoxBorderType.Soft);
                    TextMargin = 1;
                    break;
            }
        }
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
        int IButtonStyle.RoundCorner { get { return GetValue<int>(() => RoundCorner, () => Parent?.RoundCorner, () => StyleButton.RoundCorner, () => DefaultRoundCorner); } }
        #endregion
    }
    #endregion
    #region ColumnHeaderStyle
    #endregion
    #region PanelStyle
    /// <summary>
    /// Styly pro Panel (pozadí, titulek, linka)
    /// </summary>
    public class PanelStyle : ItemStyle, IPanelStyle
    {
        #region Základní public property
        /// <summary>
        /// Parent styl. Pokud není zadán, používá se odpovídající styl z knihovny <see cref="Styles"/>.
        /// </summary>
        public PanelStyle Parent { get; set; }
        /// <summary>
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (BackColor.HasValue || TitleFontModifier != null || TitleTextColor.HasValue ||
                    TitleLocation.HasValue || TitleLineSize.HasValue || 
                    TitleLineColorBegin.HasValue || TitleLineColorEnd.HasValue ||
                    BackColorFocused.HasValue || TitleFontModifierFocused != null || TitleTextColorFocused.HasValue || 
                    TitleLineColorBeginFocused.HasValue || TitleLineColorEndFocused.HasValue); } }
        /// <summary>
        /// Barva pozadí běžná
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Modifikátor textu titulku běžný
        /// </summary>
        public FontModifierInfo TitleFontModifier { get; set; }
        /// <summary>
        /// Barva textu titulku s focusem
        /// </summary>
        public Color? TitleTextColor { get; set; }
        /// <summary>
        /// Umístění počátku titulku (definuje tedy okraje zleva a shora)
        /// </summary>
        public Point? TitleLocation { get; set; }
        /// <summary>
        /// Šířka linky podtržení (reálně jde o výšku), počet pixelů. 0 (a záporné) = bez linky.
        /// </summary>
        public int? TitleLineSize { get; set; }
        /// <summary>
        /// Barva linky podtržení, počáteční (vlevo), běžná
        /// </summary>
        public Color? TitleLineColorBegin { get; set; }
        /// <summary>
        /// Barva linky podtržení, koncová (vpravo), běžná
        /// </summary>
        public Color? TitleLineColorEnd { get; set; }
        /// <summary>
        /// Barva pozadí s focusem
        /// </summary>
        public Color? BackColorFocused { get; set; }
        /// <summary>
        /// Modifikátor textu titulku s focusem
        /// </summary>
        public FontModifierInfo TitleFontModifierFocused { get; set; }
        /// <summary>
        /// Barva textu titulku s focusem
        /// </summary>
        public Color? TitleTextColorFocused { get; set; }
        /// <summary>
        /// Barva linky podtržení, počáteční (vlevo), s focusem
        /// </summary>
        public Color? TitleLineColorBeginFocused { get; set; }
        /// <summary>
        /// Barva linky podtržení, koncová (vpravo), s focusem
        /// </summary>
        public Color? TitleLineColorEndFocused { get; set; }

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            switch (styleType)
            {
                case StyleType.System:
                    BackColor = DefaultBackColor;
                    TitleFontModifier = DefaultTitleFontModifier;
                    TitleTextColor = DefaultTitleTextColor;
                    TitleLocation = DefaultTitleLocation;
                    TitleLineSize = DefaultTitleLineSize;
                    TitleLineColorBegin = DefaultTitleLineColorBegin;
                    TitleLineColorEnd = DefaultTitleLineColorEnd;
                    BackColorFocused = DefaultBackColorFocused;
                    TitleFontModifierFocused = DefaultTitleFontModifierFocused;
                    TitleTextColorFocused = DefaultTitleTextColorFocused;
                    TitleLineColorBeginFocused = DefaultTitleLineColorBeginFocused;
                    TitleLineColorEndFocused = DefaultTitleLineColorEndFocused;
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    BackColor = Color.FromArgb(250, 250, 250);
                    TitleFontModifier = new FontModifierInfo() { SizeRatio = 1.08f };
                    TitleTextColor = Color.Black;
                    TitleLocation = DefaultTitleLocation;
                    TitleLineSize = 2;
                    TitleLineColorBegin = Color.FromArgb(180, 208, 224);
                    TitleLineColorEnd = Color.Transparent;
                    BackColorFocused = Color.FromArgb(250, 250, 250);
                    TitleFontModifierFocused = new FontModifierInfo() { SizeRatio = 1.08f, Bold = true };
                    TitleTextColorFocused = Color.Black;
                    TitleLineColorBeginFocused = Color.FromArgb(128, 189, 221);
                    TitleLineColorEndFocused = Color.Transparent;
                    break;
                case StyleType.Dark3D:
                case StyleType.DarkFlat:
                    BackColor = Color.FromArgb(12, 12, 12);
                    TitleFontModifier = new FontModifierInfo() { SizeRatio = 1.08f };
                    TitleTextColor = Color.White;
                    TitleLocation = DefaultTitleLocation;
                    TitleLineSize = 2;
                    TitleLineColorBegin = Color.FromArgb(221, 194, 179);
                    TitleLineColorEnd = Color.Transparent;
                    BackColorFocused = Color.FromArgb(12, 12, 12);
                    TitleFontModifierFocused = new FontModifierInfo() { SizeRatio = 1.08f, Bold = true };
                    TitleTextColorFocused = Color.Black;
                    TitleLineColorBeginFocused = Color.FromArgb(221, 165, 135);
                    TitleLineColorEndFocused = Color.Transparent;
                    break;
            }
        }
        #endregion
        #region Protected: Přechody stylů mezi předkem a potomkem
        /// <summary>
        /// Instance konkrétní třídy (zde <see cref="PanelStyle"/>) pro určení výchozích hodnot pro svoje property používá základní společnou instanci, uloženou ve ve stylech (<see cref="Styles"/>).
        /// Tedy typicky třída <see cref="PanelStyle"/> používá pro svoje vlastní property (například <see cref="PanelStyle.BackColor"/>) instanci uloženou v <see cref="Styles.Panel"/>.
        /// <para/>
        /// Nicméně potomci zdejší třídy (například <see cref="JinyPanel"/>) budou chtít, aby tyto hodnoty, které načítá třída předka (<see cref="PanelStyle"/>),
        /// tedy například <see cref="PanelStyle.BackColor"/>, byly načteny z "jejich" instance = <see cref="Styles.JinyPanel"/>, protože tam je logicky ukládá uživatel, a mohou se lišit.
        /// Typicky se liší barva pozadí TextBoxu od barvy pozadí Buttonu, atd.
        /// <para/>
        /// Proto každá třída deklaruje protected virtual property, která má vracet konkrétní instanci ze <see cref="Styles"/>, která obsahuje odpovídající hodnoty.
        /// Potomek tuto property přepisuje, a vrací svoji vlastní základní instanci. Tím předek čte "moje" hodnoty namísto "svých vlastních".
        /// </summary>
        protected virtual PanelStyle StylePanel { get { return Styles.Panel; } }
        #endregion
        #region Defaultní hodnoty: vlastní i overrides
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.BackColor"/></summary>
        protected virtual Color DefaultBackColor { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleFontModifier"/></summary>
        protected virtual FontModifierInfo DefaultTitleFontModifier { get { return GetDefault<FontModifierInfo>(ref _DefaultTitleFontModifier, () => new FontModifierInfo() { SizeRatio = 1.07f }); } } private static FontModifierInfo _DefaultTitleFontModifier;
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleTextColor"/></summary>
        protected virtual Color DefaultTitleTextColor { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLocation"/></summary>
        protected virtual Point DefaultTitleLocation { get { return GetDefault<Point>(ref _DefaultTitleLocation, () => new Point(9, 6)); } } private static Point? _DefaultTitleLocation;
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLineSize"/></summary>
        protected virtual int DefaultTitleLineSize { get { return 2; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLineColorBegin"/></summary>
        protected virtual Color DefaultTitleLineColorBegin { get { return SystemColors.ButtonShadow; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLineColorEnd"/></summary>
        protected virtual Color DefaultTitleLineColorEnd { get { return Color.Transparent; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.BackColorFocused"/></summary>
        protected virtual Color DefaultBackColorFocused { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleFontModifierFocused"/></summary>
        protected virtual FontModifierInfo DefaultTitleFontModifierFocused { get { return GetDefault<FontModifierInfo>(ref _DefaultTitleFontModifierFocused, () => new FontModifierInfo() { SizeRatio = 1.07f, Bold = true }); } } private static FontModifierInfo _DefaultTitleFontModifierFocused;
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleTextColorFocused"/></summary>
        protected virtual Color DefaultTitleTextColorFocused { get { return SystemColors.ControlText; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLineColorBeginFocused"/></summary>
        protected virtual Color DefaultTitleLineColorBeginFocused { get { return SystemColors.ControlLight; } }
        /// <summary>Defaultní hodnota pro <see cref="PanelStyle.TitleLineColorEndFocused"/></summary>
        protected virtual Color DefaultTitleLineColorEndFocused { get { return Color.Transparent; } }
        #endregion
        #region Implementace interface
        Color IPanelStyle.BackColor { get { return GetValue<Color>(() => BackColor, () => Parent?.BackColor, () => StylePanel.BackColor, () => DefaultBackColor); } }
        FontModifierInfo IPanelStyle.TitleFontModifier { get { return GetInstance<FontModifierInfo>(() => TitleFontModifier, () => Parent?.TitleFontModifier, () => StylePanel?.TitleFontModifier, () => DefaultTitleFontModifier); } }
        Color IPanelStyle.TitleTextColor { get { return GetValue<Color>(() => TitleTextColor, () => Parent?.TitleTextColor, () => StylePanel.TitleTextColor, () => DefaultTitleTextColor); } }
        Point IPanelStyle.TitleLocation { get { return GetValue<Point>(() => TitleLocation, () => Parent?.TitleLocation, () => StylePanel.TitleLocation, () => DefaultTitleLocation); } }
        int IPanelStyle.TitleLineSize { get { return GetValue<int>(() => TitleLineSize, () => Parent?.TitleLineSize, () => StylePanel.TitleLineSize, () => DefaultTitleLineSize); } }
        Color IPanelStyle.TitleLineColorBegin { get { return GetValue<Color>(() => TitleLineColorBegin, () => Parent?.TitleLineColorBegin, () => StylePanel.TitleLineColorBegin, () => DefaultTitleLineColorBegin); } }
        Color IPanelStyle.TitleLineColorEnd { get { return GetValue<Color>(() => TitleLineColorEnd, () => Parent?.TitleLineColorEnd, () => StylePanel.TitleLineColorEnd, () => DefaultTitleLineColorEnd); } }
        Color IPanelStyle.BackColorFocused { get { return GetValue<Color>(() => BackColorFocused, () => Parent?.BackColorFocused, () => StylePanel.BackColorFocused, () => DefaultBackColorFocused); } }
        FontModifierInfo IPanelStyle.TitleFontModifierFocused { get { return GetInstance<FontModifierInfo>(() => TitleFontModifierFocused, () => Parent?.TitleFontModifierFocused, () => StylePanel?.TitleFontModifierFocused, () => DefaultTitleFontModifierFocused); } }
        Color IPanelStyle.TitleTextColorFocused { get { return GetValue<Color>(() => TitleTextColorFocused, () => Parent?.TitleTextColorFocused, () => StylePanel.TitleTextColorFocused, () => DefaultTitleTextColorFocused); } }
        Color IPanelStyle.TitleLineColorBeginFocused { get { return GetValue<Color>(() => TitleLineColorBeginFocused, () => Parent?.TitleLineColorBeginFocused, () => StylePanel.TitleLineColorBeginFocused, () => DefaultTitleLineColorBeginFocused); } }
        Color IPanelStyle.TitleLineColorEndFocused { get { return GetValue<Color>(() => TitleLineColorEndFocused, () => Parent?.TitleLineColorEndFocused, () => StylePanel.TitleLineColorEndFocused, () => DefaultTitleLineColorEndFocused); } }

        Color IPanelStyle.GetBackColor(GInteractiveState interactiveState) { return GetByState<Color>(interactiveState, () => ((ITextBorderStyle)this).BackColor, () => ((ITextBorderStyle)this).BackColorDisabled, () => ((ITextBorderStyle)this).BackColorMouseOn, () => ((ITextBorderStyle)this).BackColorMouseDown); }

        #endregion
    }
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
        /// Obsahuje true tehdy, když this instance obsahuje alespoň jednu nenulovou hodnotu. Tedy typicky: return (this.Color.HasValue || ...);
        /// </summary>
        protected override bool HasValue { get { return (ScrollSizeInactive.HasValue || ThumbSizeInactive.HasValue || ScrollSizeActive.HasValue || 
                    ThumbSizeActive.HasValue || ScrollColorInactive.HasValue || ThumbColorInactive.HasValue || ArrowColorInactive.HasValue ||
                    ScrollColorActive.HasValue || ThumbColorActive.HasValue || ArrowColorActive.HasValue ||
                    ThumbImage != null || Properties.HasValue); } }
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

        // Další property: přidat do interface, přidat do this.HasValue + do ActivateStyle() + do defaultních hodnot + do implementace interface

        #endregion
        #region Předdefinované styly
        /// <summary>
        /// Potomek v této metodě nastaví svoje explicitní hodnoty pro daný vizuální styl.
        /// Metoda je volána pouze v základní instanci (kde <see cref="ItemStyle.IsStyleInstance"/> je true).
        /// </summary>
        /// <param name="styleType"></param>
        protected override void ActivateStyle(StyleType styleType)
        {
            switch (styleType)
            {
                case StyleType.System:
                    ScrollSizeInactive = DefaultScrollSizeInactive;
                    ThumbSizeInactive = DefaultThumbSizeInactive;
                    ScrollSizeActive = DefaultScrollSizeActive;
                    ThumbSizeActive = DefaultThumbSizeActive;
                    ScrollColorInactive = DefaultScrollColorInactive;
                    ThumbColorInactive = DefaultThumbColorInactive;
                    ArrowColorInactive = DefaultArrowColorInactive;
                    ScrollColorActive = DefaultScrollColorActive;
                    ThumbColorActive = DefaultThumbColorActive;
                    ArrowColorActive = DefaultArrowColorActive;
                    ThumbImage = DefaultThumbImage;
                    Properties = DefaultProperties;
                    break;
                case StyleType.Light3D:
                case StyleType.LightFlat:
                    ScrollSizeInactive = 15;
                    ThumbSizeInactive = 13;
                    ScrollSizeActive = 15;
                    ThumbSizeActive = 13;
                    ScrollColorInactive = Color.FromArgb(233, 233, 233);
                    ThumbColorInactive = Color.FromArgb(200, 200, 200);
                    ArrowColorInactive = Color.FromArgb(92, 92, 92);
                    ScrollColorActive = Color.FromArgb(240, 240, 240);
                    ThumbColorActive = Color.FromArgb(166, 166, 166);
                    ArrowColorActive = Color.FromArgb(0, 0, 0);
                    ThumbImage = DefaultThumbImage;
                    Properties = (styleType == StyleType.Dark3D ? ScrollBarProperties.Default3D : ScrollBarProperties.DefaultFlat);
                    break;
                case StyleType.Dark3D:
                    break;
                case StyleType.DarkFlat:
                    break;
            }
        }
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
        /// <summary>Defaultní hodnota pro <see cref="ThumbImage"/></summary>
        protected virtual Image DefaultThumbImage { get { return null; } }
        /// <summary>Defaultní hodnota pro <see cref="Properties"/></summary>
        protected virtual ScrollBarProperties DefaultProperties { get { return ScrollBarProperties.Default3D; } }
        #endregion
        #region Implementace interface
        int IScrollBarStyle.ScrollSizeInactive { get { return GetValue<int>(() => ScrollSizeInactive, () => Parent?.ScrollSizeInactive, () => StyleScrollBar.ScrollSizeInactive, () => DefaultScrollSizeInactive); } }
        int IScrollBarStyle.ThumbSizeInactive { get { return GetValue<int>(() => ThumbSizeInactive, () => Parent?.ThumbSizeInactive, () => StyleScrollBar.ThumbSizeInactive, () => DefaultThumbSizeInactive); } }
        int IScrollBarStyle.ScrollSizeActive { get { return GetValue<int>(() => ScrollSizeActive, () => Parent?.ScrollSizeActive, () => StyleScrollBar.ScrollSizeActive, () => DefaultScrollSizeActive); } }
        int IScrollBarStyle.ThumbSizeActive { get { return GetValue<int>(() => ThumbSizeActive, () => Parent?.ThumbSizeActive, () => StyleScrollBar.ThumbSizeActive, () => DefaultThumbSizeActive); } }
        Color IScrollBarStyle.ScrollColorInactive { get { return GetValue<Color>(() => ScrollColorInactive, () => Parent?.ScrollColorInactive, () => StyleScrollBar.ScrollColorInactive, () => DefaultScrollColorInactive); } }
        Color IScrollBarStyle.ThumbColorInactive { get { return GetValue<Color>(() => ThumbColorInactive, () => Parent?.ThumbColorInactive, () => StyleScrollBar.ThumbColorInactive, () => DefaultThumbColorInactive); } }
        Color IScrollBarStyle.ArrowColorInactive { get { return GetValue<Color>(() => ArrowColorInactive, () => Parent?.ArrowColorInactive, () => StyleScrollBar.ArrowColorInactive, () => DefaultArrowColorInactive); } }
        Color IScrollBarStyle.ScrollColorActive { get { return GetValue<Color>(() => ScrollColorActive, () => Parent?.ScrollColorActive, () => StyleScrollBar.ScrollColorActive, () => DefaultScrollColorActive); } }
        Color IScrollBarStyle.ThumbColorActive { get { return GetValue<Color>(() => ThumbColorActive, () => Parent?.ThumbColorActive, () => StyleScrollBar.ThumbColorActive, () => DefaultThumbColorActive); } }
        Color IScrollBarStyle.ArrowColorActive { get { return GetValue<Color>(() => ArrowColorActive, () => Parent?.ArrowColorActive, () => StyleScrollBar.ArrowColorActive, () => DefaultArrowColorActive); } }
        Image IScrollBarStyle.ThumbImage { get { return (ThumbImage ?? Parent?.ThumbImage ?? StyleScrollBar?.ThumbImage); } }
        ScrollBarProperties IScrollBarStyle.Properties { get { return GetValue<ScrollBarProperties>(() => Properties, () => Parent?.Properties, () => StyleScrollBar.Properties, () => DefaultProperties); } }
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
        /// Prostor mezi vnitřkem Borderu a začátkem textu = počet prázdných pixelů.
        /// Rozumná hodnota je 0 až 6.
        /// </summary>
        int TextMargin { get; }

        /// <summary>
        /// Vrátí barvu BackColor pro daný stav
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        Color GetBackColor(GInteractiveState interactiveState);
        /// <summary>
        /// Vrátí barvu BackColor pro daný stav
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        Color GetTextColor(GInteractiveState interactiveState);
        /// <summary>
        /// Vrátí barvu BackColor pro daný stav
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        Color GetBorderColor(GInteractiveState interactiveState);
    }
    /// <summary>
    /// Deklarace stylu pro kreslení Textboxu
    /// </summary>
    public interface ITextBoxStyle : ITextBorderStyle
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
    /// Deklarace stylu pro kreslení Panelů
    /// </summary>
    public interface IPanelStyle
    {
        /// <summary>
        /// Barva pozadí běžná
        /// </summary>
        Color BackColor { get; }
        /// <summary>
        /// Modifikátor textu titulku běžný
        /// </summary>
        FontModifierInfo TitleFontModifier { get; }
        /// <summary>
        /// Barva textu titulku s focusem
        /// </summary>
        Color TitleTextColor { get; }
        /// <summary>
        /// Umístění počátku titulku (definuje tedy okraje zleva a shora)
        /// </summary>
        Point TitleLocation { get; }
        /// <summary>
        /// Šířka linky podtržení (reálně jde o výšku), počet pixelů. 0 (a záporné) = bez linky.
        /// </summary>
        int TitleLineSize { get; }
        /// <summary>
        /// Barva linky podtržení, počáteční (vlevo), běžná
        /// </summary>
        Color TitleLineColorBegin { get; }
        /// <summary>
        /// Barva linky podtržení, koncová (vpravo), běžná
        /// </summary>
        Color TitleLineColorEnd { get; }
        /// <summary>
        /// Barva pozadí s focusem
        /// </summary>
        Color BackColorFocused { get; }
        /// <summary>
        /// Modifikátor textu titulku s focusem
        /// </summary>
        FontModifierInfo TitleFontModifierFocused { get; }
        /// <summary>
        /// Barva textu titulku s focusem
        /// </summary>
        Color TitleTextColorFocused { get; }
        /// <summary>
        /// Barva linky podtržení, počáteční (vlevo), s focusem
        /// </summary>
        Color TitleLineColorBeginFocused { get; }
        /// <summary>
        /// Barva linky podtržení, koncová (vpravo), s focusem
        /// </summary>
        Color TitleLineColorEndFocused { get; }
        /// <summary>
        /// Vrátí barvu BackColor pro daný stav
        /// </summary>
        /// <param name="interactiveState"></param>
        /// <returns></returns>
        Color GetBackColor(GInteractiveState interactiveState);
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
        /// 3D efekt "dolů": levý a horní = tmavší, pravý a dolní = světlejší.
        /// Typické pro TextBox.
        /// </summary>
        Effect3DDown = 0x0020,
        /// <summary>
        /// 3D efekt "nahoru": levý a horní = světlejší, pravý a dolní = tmavší
        /// </summary>
        Effect3DUp = 0x0040,
        /// <summary>
        /// 3D efekt interaktivní: ve výchozím stavu bude podle nastaveného bitu <see cref="Effect3DDown"/> nebo <see cref="Effect3DUp"/>, v interaktivním bude v opačné poloze.
        /// <para/>
        /// Příklad: (<see cref="Effect3DInteractive"/> | <see cref="Effect3DUp"/>) = logické chování: v klidu (včetně MouseOn) je button nahoru, zmáčknutý (MouseDown) je dolů.
        /// <para/>
        /// Pokud bude specifikováno pouze <see cref="Effect3DInteractive"/>, a nebude zadáno <see cref="Effect3DDown"/> ani <see cref="Effect3DUp"/>, bude se chovat jako <see cref="Effect3DDown"/>.
        /// </summary>
        Effect3DInteractive = 0x0080,
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
        ArrowAsButton = 0x2000,
        /// <summary>
        /// Default pro 3D
        /// </summary>
        Default3D = ThumbWithLines | DrawInnerBorder | Draw3DBorder | AreaColorActive | ArrowColorActive,
        /// <summary>
        /// Default pro Flat
        /// </summary>
        DefaultFlat = AreaColorActive | ArrowColorActive
    }
    #endregion
}
