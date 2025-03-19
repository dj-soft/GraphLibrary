using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DevExpress.Drawing.Printing.Internal;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Třída, která přenáší požadavky na textovou ikonu mezi serverem a klientem ve formě jednoho snadného textu, který se dá snadno naplnit, serializovat, deserializovat i validovat.
    /// </summary>
    public class SvgImageTextIcon : SvgImageIcon
    {
        #region Static serializer a deserializer
        /// <summary>
        /// Vrátí string definující ikonu s textem a danými parametry
        /// </summary>
        /// <param name="text"></param>
        /// <param name="textBold"></param>
        /// <param name="textStyle"></param>
        /// <param name="textColor"></param>
        /// <param name="textColorBW"></param>
        /// <param name="backColor"></param>
        /// <param name="backgroundVisible"></param>
        /// <param name="borderColor"></param>
        /// <param name="borderColorBW"></param>
        /// <param name="borderVisible"></param>
        /// <param name="paddingPc"></param>
        /// <param name="borderWidthPc"></param>
        /// <param name="roundingPc"></param>
        /// <returns></returns>
        public static string CreateImageName(string text, bool? textBold = null, TextStyleType? textStyle = null, Color? textColor = null, bool? textColorBW = null, Color? backColor = null,
            bool? backgroundVisible = null, Color? borderColor = null, bool? borderColorBW = null, bool? borderVisible = null, int? paddingPc = null, int? borderWidthPc = null, int? roundingPc = null)
        {
            SvgImageTextIcon textIcon = new SvgImageTextIcon()
            {
                Text = text,
                TextBold = textBold,
                TextFont = textStyle,
                TextColor = textColor,
                TextColorBW = textColorBW,
                BackColor = backColor,
                BackgroundVisible = backgroundVisible,
                BorderColor = borderColor,
                BorderColorBW = borderColorBW,
                BorderVisible = borderVisible,
                PaddingPc = paddingPc,
                BorderWidthPc = borderWidthPc,
                RoundingPc = roundingPc
            };
            return textIcon.SvgImageName;
        }
        /// <summary>
        /// Deserializuje dodaný string do instance <see cref="SvgImageTextIcon"/> a vrátí ji.
        /// Pokud vstupní string není validní, vrátí false.
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="svgImageTextIcon"></param>
        /// <returns></returns>
        public static bool TryDeserializeFrom(string serial, out SvgImageTextIcon svgImageTextIcon)
        {
            SvgImageTextIcon result = new SvgImageTextIcon();
            if (result.Deserialize(serial))
            {
                svgImageTextIcon = result;
                return true;
            }
            svgImageTextIcon = null;
            return false;
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Vlastní text.<br/>
        /// Může být prázdný, pak se vykreslí prázdná ikona (čterec, kolečko) v zadané barvě.<br/>
        /// Nedoporučuje se zadávat více než 2 znaky. Ikona se pokusí vykreslit celý zadaný text, nemá vlastní omezení. Za výsledek ale neručíme.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Písmo bude zvýrazněné (Bold)?<br/>
        /// Default = ne.
        /// </summary>
        public bool? TextBold { get; set; }
        /// <summary>
        /// Styl (font) písma<br/>
        /// Default = <see cref="TextStyleType.Default"/>.
        /// </summary>
        public TextStyleType? TextFont { get; set; }
        /// <summary>
        /// Barva textu.<br/>
        /// Pokud nebude zadaná, určí se jako kontrastní barva z barvy pozadí <see cref="BackColor"/> a příznaku <see cref="TextColorBW"/>.
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Pokud nebude zadána barva textu <see cref="TextColor"/>, pak bude odvozena z barvy pozadí <see cref="BackColor"/> jako vhodná kontrastní.<br/>
        /// Zde se řídí, zda bude true = černá/bílá, anebo false = kontrastní plná barva.<br/>
        /// Defaultní hodnota (pokud bude null) je false = text bude barevný, kontrastní.
        /// </summary>
        public bool? TextColorBW { get; set; }
        /// <summary>
        /// Barva pozadí. Může být null, pokud bude určena barva textu. Pak se pozadí nebude kreslit.
        /// Může být zadáno a přitom hodnota <see cref="BackgroundVisible"/> bude false = pak tato barva poslouží jako zdroj barvy pro barvu textu, pokud nebude uvedena <see cref="TextColor"/>.
        /// <para/>
        /// Pokud nebude zadána, a přitom bude zapotřebí jako výchozí barva pro další prvky, použije se barva bílá.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Vykreslit plnou barvou pozadí ikony?<br/>
        /// Default = ano, lze potlačit hodnotou false.
        /// </summary>
        public bool? BackgroundVisible { get; set; }
        /// <summary>
        /// Barva okraje.<br/>
        /// Default = null, bude odvozena z barvy pozadí <see cref=""/>
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Pokud nebude zadána barva okraje <see cref="BorderColor"/>, pak bude odvozena z barvy pozadí <see cref="BackColor"/> jako vhodná kontrastní.<br/>
        /// Zde se řídí, zda bude true = černá/bílá, anebo false = kontrastní plná barva.<br/>
        /// Defaultní hodnota (pokud bude null) je false = rámeček bude barevný, kontrastní.
        /// </summary>
        public bool? BorderColorBW { get; set; }
        /// <summary>
        /// Vykreslit border ikony? Default = ano, lze potlačit hodnotou false.
        /// </summary>
        public bool? BorderVisible { get; set; }
        /// <summary>
        /// Volný prostor mezi fyzickým okrajem ikony a borderem (=Padding).<br/>
        /// Defaultní hodnota (pokud bude null) je 5%.
        /// <para/>
        /// Hodnoty jsou uváděny v procentech velikosti celého prostoru ikony.<br/>
        /// Validní rozsah je 0 až 100, 0 = ikona bude začínat hned na kraji prostoru, nebude to moc ladit s ostatními ikonami.<br/>
        /// Hodnoty větší než 15% jsou nehezké.
        /// </summary>
        public int? PaddingPc { get; set; }
        /// <summary>
        /// Šířka borderu.<br/>
        /// Defaultní hodnota (pokud bude null) je 3%.
        /// <para/>
        /// Hodnoty jsou uváděny v procentech velikosti celého prostoru ikony.<br/>
        /// Validní rozsah je 0 až 100, 0 = nebude se kreslit.<br/>
        /// Hodnoty větší než 10% jsou nehezké.
        /// </summary>
        public int? BorderWidthPc { get; set; }
        /// <summary>
        /// Zaoblení rohů ikony.<br/>
        /// Defaultní hodnota (pokud bude null) je 15%.
        /// <para/>
        /// Hodnoty jsou uváděny v procentech velikosti vlastní ikony (po odečtení <see cref="PaddingPc"/>).<br/>
        /// Validní rozsah je 0 až 100:<br/>
        /// 0 = ikona je čtvercová s ostrými rohy.<br/>
        /// 15 = optimální hodnota.<br/>
        /// 100 = kolečko
        /// </summary>
        public int? RoundingPc { get; set; }
        /// <summary>
        /// Typ písma
        /// </summary>
        public enum TextStyleType
        {
            /// <summary>
            /// Neurčeno, necháme na grafice
            /// </summary>
            Default,
            /// <summary>
            /// Serif = bezpatkové
            /// </summary>
            Serif,
            /// <summary>
            /// SansSerif = patkové
            /// </summary>
            SansSerif
        }
        #endregion
        #region Serializace a deserializace dat do/ze jména ikony; klonování
        /// <summary>
        /// Obsahuje "jméno ikony" = klíčové slovo a plný obsah všech zadaných dat
        /// </summary>
        public string SvgImageName { get { return Serialize(); } set { Deserialize(value); } }
        /// <summary>
        /// Vytvoří new instanci obsahující zdejší aktuální data
        /// </summary>
        /// <returns></returns>
        public SvgImageTextIcon CreateClone()
        {
            return (SvgImageTextIcon)this.MemberwiseClone();
        }
        /// <summary>
        /// Vrací text popisující všechna zadaná data v this instanci
        /// </summary>
        /// <returns></returns>
        protected virtual string Serialize()
        {
            // Povinný header formátu:
            SerializeStart(Header);
            // První je text, ten nemá klíč = je povinný:
            SerializeText(null, Text);
            // Další hodnoty mohou mít libovolné pořadí, jsou identifikovány klíčem :
            SerializeBool("S", TextBold);
            SerializeEnum("F", TextFont);
            SerializeColor("T", TextColor);
            SerializeBool("W", TextColorBW);
            SerializeColor("B", BackColor);
            SerializeBool("V", BackgroundVisible);
            SerializeColor("C", BorderColor);
            SerializeBool("O", BorderColorBW);
            SerializeBool("I", BorderVisible);
            SerializeInt("D", PaddingPc);
            SerializeInt("H", BorderWidthPc);
            SerializeInt("R", RoundingPc);
            return SerializeResult();
        }
        /// <summary>
        /// Do this instance vloží hodnoty z dodaného seializovaného textu
        /// </summary>
        /// <param name="serial"></param>
        protected virtual bool Deserialize(string serial)
        {
            _Clear();
            bool isValid = TryParseSerial(serial, Header, parse);
            return isValid;

            void parse(int index, string content)
            {
                if (index == 0) return;                                        // Na indexu 0 je Header
                if (index == 1) Text = DeserializeString(content);             // Na indexu 1 je Text
                else if (TryParseContent(content, out var key, out var value)) // Na dalších pozicích jsou nenulové jednotlivé hodnoty, na jejich pořadí nezáleží
                {   // Na dalších pozicích jsou jednotlivé hodnoty (Key Value)
                    switch (key)
                    {   // Klíče musí odpovídat klíčům při serializaci, podle nich se deserializuje hodnota a ukládá do odpovídající property:
                        case "S": { TextBold = DeserializeBool(value); break; }
                        case "F": { TextFont = DeserializeEnum<TextStyleType>(value); break; }
                        case "T": { TextColor = DeserializeColor(value); break; }
                        case "W": { TextColorBW = DeserializeBool(value); break; }
                        case "B": { BackColor = DeserializeColor(value); break; }
                        case "V": { BackgroundVisible = DeserializeBool(value); break; }
                        case "C": { BorderColor = DeserializeColor(value); break; }
                        case "O": { BorderColorBW = DeserializeBool(value); break; }
                        case "I": { BorderVisible = DeserializeBool(value); break; }
                        case "D": { PaddingPc = DeserializeInt(value); break; }
                        case "H": { BorderWidthPc = DeserializeInt(value); break; }
                        case "R": { RoundingPc = DeserializeInt(value); break; }
                    }
                }
            }
        }
        /// <summary>
        /// Vynuluje všechny property v tomto objektu
        /// </summary>
        private void _Clear()
        {
            Text = null;
            TextBold = null;
            TextFont = null;
            TextColor = null;
            TextColorBW = null;
            BackColor = null;
            BackgroundVisible = null;
            BorderColor = null;
            BorderColorBW = null;
            BorderVisible = null;
            PaddingPc = null;
            BorderWidthPc = null;
            RoundingPc = null;
        }
        /// <summary>
        /// Záhlaví názvu ikony
        /// </summary>
        private const string Header = "@textargs";
        #endregion
        #region Validace dat, doplnění odvozených hodnot
        /// <summary>
        /// Validuje data = namísto null dosadí defaulty, odvodí barvy, zajistí omezení číselných hodnot...<br/>
        /// Výsledkem je plně použitelná sada dat pro fyzickou tvorbu SVG ikony.
        /// <para/>
        /// Tuto metodu <b><u>není třeba volat</u></b> v procesu definice ikony ani před serializací, protože zbytečně prodlužuje název ikony !!!
        /// <para/>
        /// Meotdu volá klient před tvorbou SVG ikony, a může ji volat 
        /// </summary>
        public void Validate()
        {
            // Základní defaulty:
            if (Text == null) Text = "";
            TextBold = getDefault(TextBold, false);
            TextFont = getDefault(TextFont, TextStyleType.Default);
            TextColorBW = getDefault(TextColorBW, false);
            BackgroundVisible = getDefault(BackgroundVisible, true);
            BorderColorBW = getDefault(BorderColorBW, false);
            BorderVisible = getDefault(BorderVisible, true);
            PaddingPc = getDefaultInt(PaddingPc, 5, 0, 100);
            BorderWidthPc = getDefaultInt(BorderWidthPc, 3, 0, 100);
            RoundingPc = getDefaultInt(RoundingPc, 15, 0, 100);


            //  Barvy a jejich vzájemné doplnění:
            bool hasText = !String.IsNullOrEmpty(Text);
            bool hasBackground = BackgroundVisible.Value;
            bool hasBorder = BorderVisible.Value && BorderWidthPc.Value > 0;
            bool hasTextColor = TextColor.HasValue;
            bool hasBackColor = BackColor.HasValue;
            bool hasBorderColor = BorderColor.HasValue;

            // a) Mám text a nemám jeho barvu:
            if (hasText && !hasTextColor)
            {   // Nemám barvu textu => ta musí vycházet z barvy BackColor; a pokud ani ji nemám, pak její default je Bílá:
                if (!hasBackColor)
                {
                    BackColor = Color.White;
                    hasBackColor = true;
                }
                TextColor = GetContrastColor(BackColor.Value, TextColorBW.Value, 1f);
                hasTextColor = true;
            }

            // b) Mám pozadí a nemám jeho barvu:
            if (hasBackground && !hasBorderColor)
            {   // Nemám barvu pozadí => buď vychází z barvy textu, jako světlá kontrastní; anebo bude bílá:
                if (hasTextColor)
                {
                    BackColor = ;
                }
            }


            T getDefault<T>(T? valueN, T defValue) where T : struct
            {
                return valueN ?? defValue;
            }
            int getDefaultInt(int? valueN, int defValue, int minValue, int maxValue)
            {
                int value = valueN ?? defValue;
                value = (value < minValue ? minValue : (value > maxValue ? maxValue : value));
                return value;
            }
        }
        #endregion
    }
    #region abstract class SvgImageIcon
    /// <summary>
    /// Bázová třída, obsahuje support pro serializaci, deserializaci i validaci
    /// </summary>
    public abstract class SvgImageIcon
    {
        #region Support pro Serializaci
        /// <summary>
        /// Zahájí proces serializace
        /// </summary>
        /// <param name="header"></param>
        protected void SerializeStart(string header)
        {
            __Serializer = new StringBuilder();
            __Serializer.Append(header);

            __Keys = new Dictionary<string, object>();
        }
        /// <summary>
        /// Hlídám si jednoznačnost klíče
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void AddKeyValue(string key, object value)
        {
            if (key is null) key = "";
            if (__Keys.TryGetValue(key, out var oldValue)) throw new ArgumentException($"SvgImageIcon.Serialize exception: duplicite key '{key}'; OldValue: '{oldValue}'; NewValue: '{value}'");
            __Keys.Add(key, value);
        }
        /// <summary>
        /// Vrátí dosud serializovaný text, a serializer ukončí.
        /// </summary>
        /// <returns></returns>
        protected string SerializeResult()
        {
            string result = __Serializer.ToString();
            __Serializer = null;
            __Keys = null;
            return result;
        }
        /// <summary>
        /// Do serialu vloží [klíč] a [string hodnotu]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SerializeText(string key, string value)
        {
            // Hlídám si jednoznačnost klíče:
            AddKeyValue(key, value);

            // Null:
            if (key != null && value is null) return;                // Pokud je zadán klíč (=hodnota je optional), a přitom hodnota value je null, pak ji do serial nedáváme.

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);

            // Value:
            if (value is null) value = "";                           // Když už do serializeru musí jít data, tak tam nemůže jít null...
            string delimiter = Delimiter.ToString();
            if (value.Contains(delimiter))                           // Pokud ve vstupu je | (Delimiter), tak tam dáme ‼ (Selimiter):
                value = value.Replace(delimiter, Selimiter.ToString());
            __Serializer.Append(value);
        }
        /// <summary>
        /// Do serialu vloží [klíč] a [bool hodnotu]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SerializeBool(string key, bool? value)
        {
            // Hlídám si jednoznačnost klíče:
            AddKeyValue(key, value);

            // Null:
            if (!value.HasValue) return;                             // Null neserializujeme

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);

            // Value:
            __Serializer.Append(value.Value ? "1" : "0");
        }
        /// <summary>
        /// Do serialu vloží [klíč] a [int hodnotu]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SerializeInt(string key, int? value)
        {
            // Hlídám si jednoznačnost klíče:
            AddKeyValue(key, value);

            // Null:
            if (!value.HasValue) return;                             // Null neserializujeme

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);

            // Value:
            __Serializer.Append(value.Value.ToString());
        }
        /// <summary>
        /// Do serialu vloží [klíč] a [Enum hodnotu]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SerializeEnum<T>(string key, T? value) where T : struct
        {
            // Hlídám si jednoznačnost klíče:
            AddKeyValue(key, value);

            // Null:
            if (!value.HasValue) return;                             // Null neserializujeme

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);

            // Value:
            __Serializer.Append(value.Value.ToString());
        }
        /// <summary>
        /// Do serialu vloží [klíč] a [Color hodnotu]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SerializeColor(string key, Color? value)
        {
            // Hlídám si jednoznačnost klíče:
            AddKeyValue(key, value);

            // Null:
            if (!value.HasValue) return;                             // Null neserializujeme

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);

            // Value:
            Color color = value.Value;
            if (color.IsNamedColor)
                __Serializer.Append(color.Name);
            else
                __Serializer.Append("#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2"));
        }
        private StringBuilder __Serializer;
        private Dictionary<string, object> __Keys;
        #endregion
        #region Support pro Deserializaci (parsování)
        /// <summary>
        /// Parsuje vstupní serializovaný text na prvky, a ty pak jednotlivě posílá do akce ke zpracování
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="header"></param>
        /// <param name="itemAction"></param>
        protected bool TryParseSerial(string serial, string header, Action<int, string> itemAction)
        {
            if (String.IsNullOrEmpty(serial)) return false;
            serial = serial.Trim();
            if (!serial.StartsWith(header)) return false;

            var items = serial.Split(Delimiter);
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0)
                    itemAction(i, items[i]);
            }
            return true;
        }
        /// <summary>
        /// Ze vstupního textu <paramref name="content"/> oddělí první znak jako <paramref name="key"/>, a další text za tím jako <paramref name="value"/>. Vrací true, pokud vstupní text měl délku 1 a více, tedy <paramref name="key"/> obsahuje jeden znak.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool TryParseContent(string content, out string key, out string value)
        {
            key = null;
            value = null;
            int length = content?.Length ?? -1;
            if (length <= 0) return false;

            key = content.Substring(0, 1);
            value = (length == 1 ? "" : content.Substring(1));
            return true;
        }
        /// <summary>
        /// Deserializuje text
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected string DeserializeString(string content)
        {
            if (content is null || content.Length == 0) return "";

            string selimiter = Selimiter.ToString();
            if (content.Contains(selimiter))
                content = content.Replace(selimiter, Delimiter.ToString());

            return content;
        }
        /// <summary>
        /// Deserializuje bool
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected bool? DeserializeBool(string content)
        {
            if (content is null || content.Length == 0) return null;

            string value = content.Trim().ToLower();
            switch (value)
            {
                case "0":
                case "n":
                case "no":
                case "ne":
                    return false;
                case "1":
                case "y":
                case "yes":
                case "a":
                case "ano":
                    return true;
            }
            return null;
        }
        /// <summary>
        /// Deserializuje int
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected int? DeserializeInt(string content)
        {
            if (content is null || content.Length == 0) return null;

            if (Int32.TryParse(content.Trim(), out var number)) return number;
            return null;
        }
        /// <summary>
        /// Deserializuje enum
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected T? DeserializeEnum<T>(string content) where T : struct
        {
            if (content is null || content.Length == 0) return null;

            if (Enum.TryParse<T>(content.Trim(), true, out var value)) return value;
            return null;
        }
        /// <summary>
        /// Deserializuje Color
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected Color? DeserializeColor(string content)
        {
            if (content is null || content.Length == 0) return null;

            string value = content.Trim();
            if (value.Length == 7 && value[0] == '#')
            {
                if (isValidPart(value.Substring(1, 2), out int r) &&
                    isValidPart(value.Substring(3, 2), out int g) &&
                    isValidPart(value.Substring(5, 2), out int b))
                    return Color.FromArgb(r, g, b);
                return null;
            }
            else if (Enum.TryParse<KnownColor>(value, true, out var knownColor)) return Color.FromKnownColor(knownColor);

            return null;

            // parsuje HEX text čísla na hodnotu 0 - 255
            bool isValidPart(string text, out int part)
            {
                if (Int32.TryParse(text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out part))
                    return (part >= 0 && part <= 255);
                return false;
            }
        }
        /// <summary>
        /// Oddělovač jednotlivých prvků: <c>|</c><br/>
        /// Pokud by tento znak byl přítomen ve vstupním textu, pak je serializován jako ‼  (znak 19) = <see cref="Selimiter"/>
        /// </summary>
        protected const char Delimiter = '|';
        /// <summary>
        /// Znak, kterým je v serializované formě hodnoty <see cref="Text"/> nahrazen vstupní znak oddělovače <c>|</c><br/>
        /// </summary>
        protected const char Selimiter = '‼';
        #endregion
        #region Color transformace
        /// <summary>
        /// Vrátí kontrastní barvu k barvě zadané <paramref name="baseColor"/>.
        /// <para/>
        /// Pokud hodnota <paramref name="contrastBW"/> je true, pak vrátí černou nebo bílou.<br/>
        /// Pokud <paramref name="contrastBW"/> je false a hodnota <paramref name="contrastRatio"/> je zadaná, pak vrací barvu , která je kontrastní k zadané barvě, v dané míře 0 - 1.
        /// </summary>
        /// <param name="baseColor">Výchozí barva</param>
        /// <param name="contrastBW">Požadavek na Black / White kontrastní barvu</param>
        /// <param name="contrastBrightness"></param>
        /// <param name="contrastRatio">Míra sytosti kontrastní barvy, vrozsahu 0 = šedá až 1 = plná sytá barva, opačná než zadaná</param>
        /// <returns></returns>
        protected static Color GetContrastColor(Color baseColor, bool contrastBW, bool contrastBrightness, float? contrastRatio = null)
        {
            var hsbColor = Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.ColorToHsb(baseColor);
            if (contrastBW)
            {   // Hledám, zda kontrastní barva je Černá nebo Bílá, podle jasu bázové barvy:
                bool isLight = (hsbColor.Brightness >= 50);          // Pokud vstupní barva je světlá,
                return (isLight ? Color.Black : Color.White);        //  ... pak kontrastní je černá, a naopak.
            }
            else if (contrastBrightness)
            {   // Kontrastní jas, ale ponecháme stejný odstín (=ke světle modré vrátí sytou tmavomodrou, atd)


                var hsbColor = Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.HslToRgb ColorToHsb(baseColor);

            }
            else if (contrastRatio.HasValue)
            {   // Hledám částečnou kontrastní barvu:
                int contrastPart = (contrastRatio.Value <= 0f ? 0 : (contrastRatio.Value >= 1 ? 128 : (int)Math.Round(contrastRatio.Value * 128f, 0)));
                float a = baseColor.A;
                float r = getContrastPart(baseColor.R, contrastPart);
                float g = getContrastPart(baseColor.G, contrastPart);
                float b = getContrastPart(baseColor.B, contrastPart);
                return getColor(a, r, g, b);
            }
            else
            {   // Hledám plnou kontrastní barvu:
                int contrastPart = 128;
                float a = baseColor.A;
                float r = getContrastPart(baseColor.R, contrastPart);
                float g = getContrastPart(baseColor.G, contrastPart);
                float b = getContrastPart(baseColor.B, contrastPart);
                return getColor(a, r, g, b);
            }


            float getContrastPart(int basePart, int contrPart)
            {
                return (basePart <= 128 ? 128 + contrPart : 128 - contrPart);
            }
            Color getColor(float a, float r, float g, float b)
            {
                int ac = (a < 0f ? 0 : (a > 255f ? 255 : (int)a));
                int rc = (r < 0f ? 0 : (r > 255f ? 255 : (int)r));
                int gc = (g < 0f ? 0 : (g > 255f ? 255 : (int)g));
                int bc = (b < 0f ? 0 : (b > 255f ? 255 : (int)b));
                return Color.FromArgb(ac, rc, gc, bc);
            }
        }
        #endregion
    }
    #endregion
}

namespace Noris.Clients.Win.Components.AsolDX.Colors
{
    #region class HsbColor
    /// <summary>
    /// Represents a HSV (=HSB) color space.
    /// http://en.wikipedia.org/wiki/HSV_color_space
    /// </summary>
    internal sealed class HsbColor : AnyColor
    {
        public HsbColor(double hue, double saturation, double brightness, int alpha)
        {
            PreciseHue = hue;
            PreciseSaturation = saturation;
            PreciseBrightness = brightness;
            Alpha = alpha;
        }
        /// <summary>
        /// Gets or sets the hue. Values from 0 to 360.
        /// </summary>
        public double PreciseHue { get { return __PreciseHue; } set { __PreciseHue = Cycle(value, 360d); } } private double __PreciseHue;
        /// <summary>
        /// Gets or sets the saturation. Values from 0 to 100.
        /// </summary>
        public double PreciseSaturation { get { return __PreciseSaturation; } set { __PreciseSaturation = Align(value, 0d, 100d); } } private double __PreciseSaturation;
        /// <summary>
        /// Gets or sets the brightness. Values from 0 to 100.
        /// </summary>
        public double PreciseBrightness { get { return __PreciseBrightness; } set { __PreciseSaturation = Align(value, 0d, 100d); } } private double __PreciseBrightness;
        /// <summary>
        /// Gets or sets the alpha. Values from 0 to 255.
        /// </summary>
        public int Alpha { get { return __Alpha; } set { __Alpha = Align(value, 0, 255); } } private int __Alpha;
        /// <summary>
        /// Gets or sets the hue. Values from 0 to 360.
        /// </summary>
        public int Hue => Convert.ToInt32(PreciseHue);
        /// <summary>
        /// Gets or sets the saturation. Values from 0 to 100.
        /// </summary>
        public int Saturation => Convert.ToInt32(PreciseSaturation);
        /// <summary>
        /// Gets or sets the brightness. Values from 0 to 100.
        /// </summary>
        public int Brightness => Convert.ToInt32(PreciseBrightness);

        public static HsbColor FromColor(Color color)
        {
            return ColorConverting.ColorToRgb(color).ToHsbColor();
        }

        public static HsbColor FromRgbColor(RgbColor color)
        {
            return color.ToHsbColor();
        }

        public static HsbColor FromHsbColor(HsbColor color)
        {
            return new(color.PreciseHue, color.PreciseSaturation, color.PreciseBrightness, color.Alpha);
        }

        public static HsbColor FromHslColor(HslColor color)
        {
            return FromRgbColor(color.ToRgbColor());
        }

        public override string? ToString()
        {
            return $@"Hue: {Hue}; saturation: {Saturation}; brightness: {Brightness}.";
        }

        public Color ToColor()
        {
            return ColorConverting.HsbToRgb(this).ToColor();
        }

        public RgbColor ToRgbColor()
        {
            return ColorConverting.HsbToRgb(this);
        }

        public HsbColor ToHsbColor()
        {
            return new(PreciseHue, PreciseSaturation, PreciseBrightness, Alpha);
        }

        public HslColor ToHslColor()
        {
            return ColorConverting.RgbToHsl(ToRgbColor());
        }

        public override bool Equals(object obj)
        {
            var equal = false;

            if (obj is HsbColor color)
            {
                if (Math.Abs(PreciseHue - color.PreciseHue) < 0.00001 &&
                    Math.Abs(PreciseSaturation - color.PreciseSaturation) < 0.00001 &&
                    Math.Abs(PreciseBrightness - color.PreciseBrightness) < 0.00001 &&
                    Alpha == color.Alpha)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"H:{Hue}-S:{Saturation}-B:{Brightness}-A:{Alpha}".GetHashCode();
        }
    }
    #endregion
    #region class HslColor
    /// <summary>
    /// Represents a HSL color space.
    /// http://en.wikipedia.org/wiki/HSV_color_space
    /// </summary>
    internal sealed class HslColor : AnyColor
    {
        public HslColor(double hue, double saturation, double light, int alpha)
        {
            PreciseHue = hue;
            PreciseSaturation = saturation;
            PreciseLight = light;
            Alpha = alpha;
        }
        /// <summary>
        /// Gets the precise hue. Values from 0 to 360.
        /// </summary>
        public double PreciseHue { get { return __PreciseHue; } set { __PreciseHue = Cycle(value, 360d); } } private double __PreciseHue;
        /// <summary>
        /// Gets the precise saturation. Values from 0 to 100.
        /// </summary>
        public double PreciseSaturation { get { return __PreciseSaturation; } set { __PreciseSaturation = Align(value, 0d, 100d); } } private double __PreciseSaturation;
        /// <summary>
        /// Gets the precise light. Values from 0 to 100.
        /// </summary>
        public double PreciseLight { get { return __PreciseLight; } set { __PreciseLight = Align(value, 0d, 100d); } } private double __PreciseLight;
        /// <summary>
        /// Gets the alpha. Values from 0 to 255
        /// </summary>
        public int Alpha { get { return __Alpha; } set { __Alpha = Align(value, 0, 255); } } private int __Alpha;
        /// <summary>
        /// Gets the hue. Values from 0 to 360.
        /// </summary>
        public int Hue => Convert.ToInt32(PreciseHue);
        /// <summary>
        /// Gets the saturation. Values from 0 to 100.
        /// </summary>
        public int Saturation => Convert.ToInt32(PreciseSaturation);
        /// <summary>
        /// Gets the light. Values from 0 to 100.
        /// </summary>
        public int Light => Convert.ToInt32(PreciseLight);
        
        public static HslColor FromColor(Color color)
        {
            return ColorConverting.RgbToHsl(ColorConverting.ColorToRgb(color));
        }

        public static HslColor FromRgbColor(RgbColor color)
        {
            return color.ToHslColor();
        }

        public static HslColor FromHslColor(HslColor color)
        {
            return new(
                color.PreciseHue,
                color.PreciseSaturation,
                color.PreciseLight,
                color.Alpha);
        }

        public static HslColor FromHsbColor(HsbColor color)
        {
            return FromRgbColor(color.ToRgbColor());
        }

        public override string? ToString()
        {
            return Alpha < 255
                ? $@"hsla({Hue}, {Saturation}%, {Light}%, {Alpha / 255f})"
                : $@"hsl({Hue}, {Saturation}%, {Light}%)";
        }

        public Color ToColor()
        {
            return ColorConverting.HslToRgb(this).ToColor();
        }

        public RgbColor ToRgbColor()
        {
            return ColorConverting.HslToRgb(this);
        }

        public HslColor ToHslColor()
        {
            return this;
        }

        public HsbColor ToHsbColor()
        {
            return ColorConverting.RgbToHsb(ToRgbColor());
        }

        public override bool Equals(object? obj)
        {
            var equal = false;

            if (obj is HslColor color)
            {
                if (Math.Abs(Hue - color.PreciseHue) < 0.00001 &&
                    Math.Abs(Saturation - color.PreciseSaturation) < 0.00001 &&
                    Math.Abs(Light - color.PreciseLight) < 0.00001 &&
                    Alpha == color.Alpha)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"H:{PreciseHue}-S:{PreciseSaturation}-L:{PreciseLight}-A:{Alpha}".GetHashCode();
        }
    }
    #endregion
    #region class RgbColor
    /// <summary>
    /// Represents a RGB color space.
    /// http://en.wikipedia.org/wiki/HSV_color_space
    /// </summary>
    internal sealed class RgbColor : AnyColor
    {
        public RgbColor(int red, int green, int blue, int alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
        /// <summary>
        /// Gets or sets the red component. Values from 0 to 255.
        /// </summary>
        public int Red { get { return __Red; } set { __Red = Align(value, 0, 255); } } private int __Red;
        /// <summary>
        /// Gets or sets the green component. Values from 0 to 255.
        /// </summary>
        public int Green { get { return __Green; } set { __Green = Align(value, 0, 255); } } private int __Green;
        /// <summary>
        /// Gets or sets the blue component. Values from 0 to 255.
        /// </summary>
        public int Blue { get { return __Blue; } set { __Blue = Align(value, 0, 255); } } private int __Blue;
        /// <summary>
        /// Gets or sets the alpha component. Values from 0 to 255.
        /// </summary>
        public int Alpha { get { return __Alpha; } set { __Alpha = Align(value, 0, 255); } } private int __Alpha;

        public static RgbColor FromColor(Color color)
        {
            return ColorConverting.ColorToRgb(color);
        }

        public static RgbColor FromRgbColor(RgbColor color)
        {
            return new(color.Red, color.Green, color.Blue, color.Alpha);
        }

        public static RgbColor FromHsbColor(HsbColor color)
        {
            return color.ToRgbColor();
        }

        public static RgbColor FromHslColor(HslColor color)
        {
            return color.ToRgbColor();
        }

        public override string ToString()
        {
            return Alpha < 255 ? $@"rgba({Red}, {Green}, {Blue}, {Alpha / 255d})" : $@"rgb({Red}, {Green}, {Blue})";
        }

        public Color ToColor()
        {
            return ColorConverting.RgbToColor(this);
        }

        public RgbColor ToRgbColor()
        {
            return this;
        }

        public HsbColor ToHsbColor()
        {
            return ColorConverting.RgbToHsb(this);
        }

        public HslColor ToHslColor()
        {
            return ColorConverting.RgbToHsl(this);
        }

        public override bool Equals(object? obj)
        {
            var equal = false;

            if (obj is RgbColor color)
            {
                if (Red == color.Red && Blue == color.Blue && Green == color.Green && Alpha == color.Alpha)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"R:{Red}-G:{Green}-B:{Blue}-A:{Alpha}".GetHashCode();
        }
    }
    #endregion
    #region class ColorConverting
    /// <summary>
    /// Provides color conversion functionality.
    /// </summary>
    /// <remarks>
    /// http://en.wikipedia.org/wiki/HSV_color_space
    /// http://www.easyrgb.com/math.php?MATH=M19#text19
    /// </remarks>
    internal static class ColorConverting
    {
        #region Public konverze finálních datových typů
        public static RgbColor HsbToRgb(HsbColor hsb)
        {
            _HsbToRgb(hsb.Hue, hsb.Saturation, hsb.Brightness, out var red, out var green, out var blue);
            return new RgbColor(red, green, blue, hsb.Alpha);
        }
        public static Color HsbToColor(HsbColor hsb)
        {
            _HsbToRgb(hsb.Hue, hsb.Saturation, hsb.Brightness, out var red, out var green, out var blue);
            return Color.FromArgb(hsb.Alpha, red, green, blue);
        }

        public static RgbColor ColorToRgb(Color color)
        {
            return new RgbColor(color.R, color.G, color.B, color.A);
        }
        public static HsbColor ColorToHsb(Color color)
        {
            _RgbToHsb(color.R, color.G, color.B, out double hue, out double saturation, out double brighness);
            return new HsbColor(hue, saturation, brighness, color.A);
        }

        public static Color RgbToColor(RgbColor rgb)
        {
            return Color.FromArgb(rgb.Alpha, rgb.Red, rgb.Green, rgb.Blue);
        }

        public static HsbColor RgbToHsb(RgbColor rgb)
        {
            _RgbToHsb(rgb.Red, rgb.Green, rgb.Blue, out double hue, out double saturation, out double brighness);
            return new HsbColor(hue, saturation, brighness, rgb.Alpha);
        }

        public static HslColor ColorToHsl(Color color)
        {
            _RgbToHsl(color.R, color.G, color.B, out double hue, out double saturation, out double light);
            return new HslColor(hue, saturation, light, color.A);
        }
        public static HslColor RgbToHsl(RgbColor rgb)
        {
            _RgbToHsl(rgb.Red, rgb.Green, rgb.Blue, out double hue, out double saturation, out double light);
            return new HslColor(hue, saturation, light, rgb.Alpha);
        }
      
        public static RgbColor HslToRgb(HslColor hsl)
        {
            _HslToRgb(hsl, out int nRed, out int nGreen, out int nBlue);
            return new RgbColor(nRed, nGreen, nBlue, hsl.Alpha);
        }
        public static Color HslToColor(HslColor hsl)
        {
            _HslToRgb(hsl, out int nRed, out int nGreen, out int nBlue);
            return Color.FromArgb(hsl.Alpha, nRed, nGreen, nBlue);
        }

        #endregion
        #region Primitivní privátní konverze barevných prostorů
        /// <summary>
        /// Konverze HSB to RGB
        /// </summary>
        /// <param name="hue">Inp HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Inp SATURATION v rozsahu 0 - 100d</param>
        /// <param name="brightness">Inp BRIGHTNESS v rozsahu 0 - 100d</param>
        /// <param name="red">Out RED v rozsahu 0 - 255</param>
        /// <param name="green">Out GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Out BLUE v rozsahu 0 - 255</param>
        /// <returns></returns>
        private static void _HsbToRgb(double hue, double saturation, double brightness, out int red, out int green, out int blue)
        {
            double redRatio = 0d;
            double greenRatio = 0d;
            double blueRatio = 0d;

            hue = hue % 360d;
            while (hue < 0d) hue += 360d;
            var satRatio = _GetRatio(saturation, 100d, 0d, 1d);
            var brightRatio = _GetRatio(brightness, 100d, 0d, 1d);

            if (Math.Abs(satRatio - 0) < 0.00001)
            {
                redRatio = brightRatio;
                greenRatio = brightRatio;
                blueRatio = brightRatio;
            }
            else
            {
                // the color wheel has six sectors.

                var sectorPosition = hue / 60d;
                var sectorNumber = (int)Math.Floor(sectorPosition);
                var fractionalSector = sectorPosition - sectorNumber;

                var p = brightRatio * (1 - satRatio);
                var q = brightRatio * (1 - satRatio * fractionalSector);
                var t = brightRatio * (1 - satRatio * (1 - fractionalSector));

                // Assign the fractional colors to r, g, and b
                // based on the sector the angle is in.
                switch (sectorNumber)
                {
                    case 0:
                        redRatio = brightRatio;
                        greenRatio = t;
                        blueRatio = p;
                        break;

                    case 1:
                        redRatio = q;
                        greenRatio = brightRatio;
                        blueRatio = p;
                        break;

                    case 2:
                        redRatio = p;
                        greenRatio = brightRatio;
                        blueRatio = t;
                        break;

                    case 3:
                        redRatio = p;
                        greenRatio = q;
                        blueRatio = brightRatio;
                        break;

                    case 4:
                        redRatio = t;
                        greenRatio = p;
                        blueRatio = brightRatio;
                        break;

                    case 5:
                        redRatio = brightRatio;
                        greenRatio = p;
                        blueRatio = q;
                        break;
                }
            }

            red = Convert.ToInt32(redRatio * 255d);
            green = Convert.ToInt32(greenRatio * 255d);
            blue = Convert.ToInt32(blueRatio * 255d);
        }
        /// <summary>
        /// Konverze RGB to HSB
        /// </summary>
        /// <param name="red">Inp RED v rozsahu 0 - 255</param>
        /// <param name="green">Inp GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Inp BLUE v rozsahu 0 - 255</param>
        /// <param name="hue">Out HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Out SATURATION v rozsahu 0 - 100d</param>
        /// <param name="brightness">Out BRIGHTNESS v rozsahu 0 - 100d</param>
        private static void _RgbToHsb(double red, double green, double blue, out double hue, out double saturation, out double brightness)
        {
            double redRatio = _GetRatio(red, 255d, 0d, 1d);
            double greenRatio = _GetRatio(green, 255d, 0d, 1d);
            double blueRatio = _GetRatio(blue, 255d, 0d, 1d);

            // _NOTE #1: Even though we're dealing with a very small range of
            // numbers, the accuracy of all calculations is fairly important.
            // For this reason, I've opted to use double data types instead
            // of float, which gives us a little bit extra precision (recall
            // that precision is the number of significant digits with which
            // the result is expressed).
            var minValue = _GetMinValue(redRatio, greenRatio, blueRatio);
            var maxValue = _GetMaxValue(redRatio, greenRatio, blueRatio);
            var delta = maxValue - minValue;

            double hueRatio = 0;
            double saturRatio;
            var brighRatio = maxValue * 100;

            if (Math.Abs(maxValue - 0) < 0.00001 || Math.Abs(delta - 0) < 0.00001)
            {
                hueRatio = 0d;
                saturRatio = 0d;
            }
            else
            {
                // _NOTE #2: FXCop insists that we avoid testing for floating 
                // point equality (CA1902). Instead, we'll perform a series of
                // tests with the help of 0.00001 that will provide 
                // a more accurate equality evaluation.

                if (Math.Abs(minValue - 0) < 0.00001)
                {
                    saturRatio = 100;
                }
                else
                {
                    saturRatio = delta / maxValue * 100;
                }

                if (Math.Abs(redRatio - maxValue) < 0.00001)
                {
                    hueRatio = (greenRatio - blueRatio) / delta;
                }
                else if (Math.Abs(greenRatio - maxValue) < 0.00001)
                {
                    hueRatio = 2 + (blueRatio - redRatio) / delta;
                }
                else if (Math.Abs(blueRatio - maxValue) < 0.00001)
                {
                    hueRatio = 4 + (redRatio - greenRatio) / delta;
                }
            }

            hue = 60d * hueRatio;
            if (hueRatio < 0d)
                hueRatio += 360d;

            saturation = saturRatio;
            brightness = brighRatio;
        }
        /// <summary>
        /// Konverze RGB to HSL
        /// </summary>
        /// <param name="red">Inp RED v rozsahu 0 - 255</param>
        /// <param name="green">Inp GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Inp BLUE v rozsahu 0 - 255</param>
        /// <param name="hue">Out HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Out SATURATION v rozsahu 0 - 100d</param>
        /// <param name="light">Out LIGHT v rozsahu 0 - 100d</param>
        private static void _RgbToHsl(double red, double green, double blue, out double hue, out double saturation, out double light)
        {
            var redRatio = _GetRatio(red, 255d, 0d, 1d);
            var greenRatio = _GetRatio(green, 255d, 0d, 1d);
            var blueRatio = _GetRatio(blue, 255d, 0d, 1d);

            var varMin = _GetMinValue(redRatio, greenRatio, blueRatio); //Min. value of RGB
            var varMax = _GetMaxValue(redRatio, greenRatio, blueRatio); //Max. value of RGB
            var delMax = varMax - varMin; //Delta RGB value

            double hueRatio;
            double satRatio;
            var lightRatio = (varMax + varMin) / 2;

            if (Math.Abs(delMax - 0) < 0.00001) //This is a gray, no chroma...
            {
                hueRatio = 0; //HSL results = 0 ÷ 1
                satRatio = 0;
                // UK:
                //				s = 1.0;
            }
            else //Chromatic data...
            {
                if (lightRatio < 0.5)
                {
                    satRatio = delMax / (varMax + varMin);
                }
                else
                {
                    satRatio = delMax / (2.0 - varMax - varMin);
                }

                var delR = ((varMax - redRatio) / 6.0 + delMax / 2.0) / delMax;
                var delG = ((varMax - greenRatio) / 6.0 + delMax / 2.0) / delMax;
                var delB = ((varMax - blueRatio) / 6.0 + delMax / 2.0) / delMax;

                if (Math.Abs(redRatio - varMax) < 0.00001)
                {
                    hueRatio = delB - delG;
                }
                else if (Math.Abs(greenRatio - varMax) < 0.00001)
                {
                    hueRatio = 1.0 / 3.0 + delR - delB;
                }
                else if (Math.Abs(blueRatio - varMax) < 0.00001)
                {
                    hueRatio = 2.0 / 3.0 + delG - delR;
                }
                else
                {
                    // Uwe Keim.
                    hueRatio = 0.0;
                }

                if (hueRatio < 0.0)
                {
                    hueRatio += 1.0;
                }
                if (hueRatio > 1.0)
                {
                    hueRatio -= 1.0;
                }
            }

            hue = hueRatio * 360.0d;
            saturation = satRatio * 100.0d;
            light = lightRatio * 100.0d;
        }
        /// <summary>
        /// Konverze HSL to RGB
        /// </summary>
        /// <param name="hue">Inp HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Inp SATURATION v rozsahu 0 - 100d</param>
        /// <param name="light">Inp LIGHT v rozsahu 0 - 100d</param>
        /// <param name="red">Out RED v rozsahu 0 - 255</param>
        /// <param name="green">Out GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Out BLUE v rozsahu 0 - 255</param>
        private static void _HslToRgb(double hue, double saturation, double light, out int red, out int green, out int blue)
        {
            double redRatio = 0d;
            double greenRatio = 0d;
            double blueRatio = 0d;

            hue = hue / 360.0d;
            while (hue < 0d) hue += 360d;
            var saturRatio = _GetRatio(saturation, 100d, 0d, 1d);
            var lightRatio = _GetRatio(light, 100d, 0d, 1d);

            if (Math.Abs(saturRatio - 0.0) < 0.00001)
            {
                redRatio = lightRatio;
                greenRatio = lightRatio;
                blueRatio = lightRatio;
            }
            else
            {
                double var2;

                if (lightRatio < 0.5)
                {
                    var2 = lightRatio * (1.0 + saturRatio);
                }
                else
                {
                    var2 = lightRatio + saturRatio - saturRatio * lightRatio;
                }

                var var1 = 2.0 * lightRatio - var2;

                redRatio = hue2Rgb(var1, var2, hue + 1.0 / 3.0);
                greenRatio = hue2Rgb(var1, var2, hue);
                blueRatio = hue2Rgb(var1, var2, hue - 1.0 / 3.0);
            }

            red = Convert.ToInt32(redRatio * 255.0);
            green = Convert.ToInt32(greenRatio * 255.0);
            blue = Convert.ToInt32(blueRatio * 255.0);


            double hue2Rgb(double v1, double v2, double vH)
            {
                if (vH < 0.0)
                {
                    vH += 1.0;
                }
                if (vH > 1.0)
                {
                    vH -= 1.0;
                }
                if (6.0 * vH < 1.0)
                {
                    return v1 + (v2 - v1) * 6.0 * vH;
                }
                if (2.0 * vH < 1.0)
                {
                    return v2;
                }
                if (3.0 * vH < 2.0)
                {
                    return v1 + (v2 - v1) * (2.0 / 3.0 - vH) * 6.0;
                }

                return v1;
            }
        }
        /// <summary>
        /// Hodnotu <paramref name="value"/> vydělí dělitelem <paramref name="divider"/> a zarovná do mezí <paramref name="minRatio"/> až <paramref name="maxRatio"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="divider"></param>
        /// <param name="minRatio"></param>
        /// <param name="maxRatio"></param>
        /// <returns></returns>
        private static double _GetRatio(double value, double divider, double minRatio, double maxRatio)
        {
            double ratio = value / divider;
            ratio = (ratio < minRatio ? minRatio : (ratio > maxRatio ? maxRatio : ratio));
            return ratio;
        }
        /// <summary>
        /// Determines the maximum value of all of the numbers provided in the
        /// variable argument list.
        /// </summary>
        private static double _GetMaxValue(params double[] values)
        {
            var maxValue = values[0];

            if (values.Length >= 2)
            {
                for (var i = 1; i < values.Length; i++)
                {
                    var num = values[i];
                    maxValue = Math.Max(maxValue, num);
                }
            }

            return maxValue;
        }
        /// <summary>
        /// Determines the minimum value of all of the numbers provided in the
        /// variable argument list.
        /// </summary>
        private static double _GetMinValue(params double[] values)
        {
            var minValue = values[0];

            if (values.Length >= 2)
            {
                for (var i = 1; i < values.Length; i++)
                {
                    var num = values[i];
                    minValue = Math.Min(minValue, num);
                }
            }

            return minValue;
        }
        #endregion
    }
    internal abstract class AnyColor
    {
        protected int Align(int value, int minValue, int maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        protected double Align(double value, double minValue, double maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        protected double Cycle(double value, double cycle)
        {
            value = value % cycle;
            while (value < 0d)
                value += cycle;
            return value;
        }
    }
    #endregion
}