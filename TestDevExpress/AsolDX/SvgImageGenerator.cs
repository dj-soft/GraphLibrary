﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using DevExpress.Drawing.Printing.Internal;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class SvgImageTextIcon : Třída, která přenáší požadavky na textovou ikonu mezi serverem a klientem
    /// <summary>
    /// Třída, která přenáší požadavky na textovou ikonu mezi serverem a klientem ve formě jednoho snadného textu, který se dá snadno naplnit, serializovat, deserializovat i validovat.
    /// <para/>
    /// <u>Příklad jednoduchého použití = v jednom řádku zadat základní parametry a získat definici ikony:</u>
    /// <code>
    /// string iconName = SvgImageTextIcon.CreateImageName("As", textFont: SvgImageTextIcon.TextFontType.Tahoma, textBold: true, textColorName: "#0F0F44", backColorName: "#FFFFE0", rounding: 6, padding: 2, borderWidth: 1);
    /// </code>
    /// <para/>
    /// <u>Příklad komplexního použití = vytvořit ikonu a nasetovat do ní parametry, a nakonec získat definici ikony:</u>
    /// <code>
    /// string iconNameW = SvgImageTextIcon.CreateImageName(iconText, textFont: SvgImageTextIcon.TextFontType.Tahoma, textBold: true, textColorName: "#0F0F44", backColorName: "#FFFFE0", rounding: 6, padding: 2, borderWidth: 1);
    /// var icon = new SvgImageTextIcon()
    /// {
    ///     Text = "As",
    ///     TextFont = SvgImageTextIcon.TextFontType.SansSerif,
    ///     TextColor = Color.DarkGray,
    ///     TextBold = false,
    ///     BackColor = Color.LightBlue,
    ///     BorderColor = Color.Black,
    ///     BorderWidth = 1,
    ///     Padding = 2,
    ///     Rounding = 16
    /// };
    /// return icon.SvgImageName;
    /// </code>
    /// </summary>
    internal class SvgImageTextIcon : SvgImageIcon
    {
        #region Static serializer a deserializer; klonování
        /// <summary>
        /// Vrátí string definující ikonu s textem a danými parametry
        /// </summary>
        /// <param name="text">Písmeno (nebo dvě) zobrazené v ikoně</param>
        /// <param name="textBold">Písmo Bold?</param>
        /// <param name="textFont">Typ fontu</param>
        /// <param name="textColor">Definice barvy písma</param>
        /// <param name="textColorBW">Pokud není daná barva písma <paramref name="textColor"/>, pak se použije kontrastní k barvě pozadí <paramref name="backColor"/>: černobílá nebo barevná?</param>
        /// <param name="backColor">Definice barvy pozadí ikony</param>
        /// <param name="backgroundVisible">Pozadí ikony bude vykreslené?</param>
        /// <param name="borderColor">Definice barvy rámečku ikony</param>
        /// <param name="borderColorBW">Pokud není daná barva rámečku <paramref name="borderColor"/>, a rámeček je viditelný, pak se použije kontrastní k barvě pozadí <paramref name="backColor"/>: černobílá nebo barevná?</param>
        /// <param name="borderVisible">Rámeček bude viditelný? Bez ohledu na barvu a jeho definovanou šířku</param>
        /// <param name="padding">Okraje kolem ikony (=zmenšení ikony proti jejímu prostoru): zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <param name="borderWidth">Šířka borderu: zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <param name="rounding">Zaoblení ikony, zadává se průměr kružnice v pixelech vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <returns></returns>
        public static string CreateImageName(string text, bool? textBold = null, TextFontType? textFont = null, Color? textColor = null, bool? textColorBW = null, Color? backColor = null,
            bool? backgroundVisible = null, Color? borderColor = null, bool? borderColorBW = null, bool? borderVisible = null, int? padding = null, int? borderWidth = null, int? rounding = null)
        {
            SvgImageTextIcon textIcon = new SvgImageTextIcon()
            {
                Text = text,
                TextBold = textBold,
                TextFont = textFont,
                TextColor = textColor,
                TextColorBW = textColorBW,
                BackColor = backColor,
                BackgroundVisible = backgroundVisible,
                BorderColor = borderColor,
                BorderColorBW = borderColorBW,
                BorderVisible = borderVisible,
                Padding = padding,
                BorderWidth = borderWidth,
                Rounding = rounding
            };
            string imageName = textIcon.SvgImageName;
            return imageName;
        }
        /// <summary>
        /// Vrátí string definující ikonu s textem a danými parametry
        /// </summary>
        /// <param name="text">Písmeno (nebo dvě) zobrazené v ikoně</param>
        /// <param name="textBold">Písmo Bold?</param>
        /// <param name="textFont">Typ fontu</param>
        /// <param name="textColorName">Definice barvy písma</param>
        /// <param name="textColorBW">Pokud není daná barva písma <paramref name="textColorName"/>, pak se použije kontrastní k barvě pozadí <paramref name="backColorName"/>: černobílá nebo barevná?</param>
        /// <param name="backColorName">Definice barvy pozadí ikony</param>
        /// <param name="backgroundVisible">Pozadí ikony bude vykreslené?</param>
        /// <param name="borderColorName">Definice barvy rámečku ikony</param>
        /// <param name="borderColorBW">Pokud není daná barva rámečku <paramref name="borderColorName"/>, a rámeček je viditelný, pak se použije kontrastní k barvě pozadí <paramref name="backColorName"/>: černobílá nebo barevná?</param>
        /// <param name="borderVisible">Rámeček bude viditelný? Bez ohledu na barvu a jeho definovanou šířku</param>
        /// <param name="padding">Okraje kolem ikony (=zmenšení ikony proti jejímu prostoru): zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <param name="borderWidth">Šířka borderu: zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <param name="rounding">Zaoblení ikony, zadává se průměr kružnice v pixelech vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru)</param>
        /// <returns></returns>
        public static string CreateImageName(string text, bool? textBold = null, TextFontType? textFont = null, string textColorName = null, bool? textColorBW = null, string backColorName = null,
            bool? backgroundVisible = null, string borderColorName = null, bool? borderColorBW = null, bool? borderVisible = null, int? padding = null, int? borderWidth = null, int? rounding = null)
        {
            SvgImageTextIcon textIcon = new SvgImageTextIcon()
            {
                Text = text,
                TextBold = textBold,
                TextFont = textFont,
                TextColorName = textColorName,
                TextColorBW = textColorBW,
                BackColorName = backColorName,
                BackgroundVisible = backgroundVisible,
                BorderColorName = borderColorName,
                BorderColorBW = borderColorBW,
                BorderVisible = borderVisible,
                Padding = padding,
                BorderWidth = borderWidth,
                Rounding = rounding
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
        public static bool TryParse(string serial, out SvgImageTextIcon svgImageTextIcon)
        {
            if (!String.IsNullOrEmpty(serial) && serial.Length > 3 && serial.StartsWith(FullHeader + Delimiter.ToString()))
            {
                SvgImageTextIcon result = new SvgImageTextIcon();
                if (result.Deserialize(serial))
                {
                    svgImageTextIcon = result;
                    return true;
                }
            }
            svgImageTextIcon = null;
            return false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageTextIcon()
        {
            __ImageName = null;
            __IsValidated = false;
        }
        private string __ImageName;
        private bool __IsValidated;
        /// <summary>
        /// Vytvoří new instanci obsahující zdejší aktuální data
        /// </summary>
        /// <returns></returns>
        public SvgImageTextIcon CreateClone()
        {
            return (SvgImageTextIcon)this.MemberwiseClone();
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Jméno ikony, odpovídá zadání dat ikony před provedením první validace
        /// </summary>
        public string ImageName { get { return __IsValidated ? __ImageName : SvgImageName; } }
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
        /// Default = <see cref="TextFontType.Default"/>.
        /// </summary>
        public TextFontType? TextFont { get; set; }
        /// <summary>
        /// Styl (font) písma, jméno fontfamily<br/>
        /// Default = <see cref="TextFontType.Default"/>.
        /// </summary>
        public string TextFontName
        {
            get
            {
                if (!this.TextFont.HasValue) return null;
                var font = this.TextFont.Value;
                switch (font)
                {
                    case TextFontType.Default: return "";
                    case TextFontType.Serif: return "serif";
                    case TextFontType.SansSerif: return "sans_serif";
                    case TextFontType.Tahoma: return "tahoma";
                }
                return "";
            }
            set
            {
                if (value is null)
                {
                    this.TextFont = null;
                }
                else
                {
                    string key = value.Trim().ToLower();
                    switch (key)
                    {
                        case "serif": this.TextFont = TextFontType.Serif; break;
                        case "sansserif":
                        case "sans serif":
                        case "sans-serif":
                        case "sans_serif": this.TextFont = TextFontType.SansSerif; break;
                        case "tahoma": this.TextFont = TextFontType.Tahoma; break;
                        case "default": this.TextFont = TextFontType.Default; break;
                        default: this.TextFont = null; break;
                    }
                }
            }
        }
        /// <summary>
        /// Barva textu.<br/>
        /// Pokud nebude zadaná, určí se jako kontrastní barva z barvy pozadí <see cref="BackColor"/> a příznaku <see cref="TextColorBW"/>.
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Barva textu.<br/>
        /// Pokud nebude zadaná, určí se jako kontrastní barva z barvy pozadí <see cref="BackColor"/> a příznaku <see cref="TextColorBW"/>.
        /// </summary>
        public string TextColorName { get { return SerializeColor(TextColor); } set { TextColor = DeserializeColor(value); } }
        /// <summary>
        /// Barva textu, pro tmavé skiny.<br/>
        /// Pokud nebude zadaná, určí se jako kontrastní barva z barvy pozadí <see cref="BackColor"/> a příznaku <see cref="TextColorBW"/>.
        /// </summary>
        public Color? TextColorDark { get; set; }
        /// <summary>
        /// Barva textu, pro tmavé skiny.<br/>
        /// Pokud nebude zadaná, určí se jako kontrastní barva z barvy pozadí <see cref="BackColor"/> a příznaku <see cref="TextColorBW"/>.
        /// </summary>
        public string TextColorDarkName { get { return SerializeColor(TextColorDark); } set { TextColorDark = DeserializeColor(value); } }
        /// <summary>
        /// Pokud nebude zadána barva textu <see cref="TextColor"/>, pak bude odvozena z barvy pozadí <see cref="BackColor"/> jako vhodná kontrastní.<br/>
        /// Zde se řídí, zda bude true = černá/bílá, anebo false = kontrastní plná barva.<br/>
        /// Defaultní hodnota (pokud bude null) je true = text bude černý nebo bílý, kontrastní.
        /// </summary>
        public bool? TextColorBW { get; set; }
        /// <summary>
        /// Barva pozadí.<br/>
        /// Může být null, pokud bude určena barva textu. Pak se pozadí nebude kreslit.
        /// Může být zadáno a přitom hodnota <see cref="BackgroundVisible"/> bude false = pak tato barva poslouží jako zdroj barvy pro barvu textu, pokud nebude uvedena <see cref="TextColor"/>.
        /// <para/>
        /// Pokud nebude zadána, a přitom bude zapotřebí jako výchozí barva pro další prvky, použije se barva bílá.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva pozadí.<br/>
        /// Může být null, pokud bude určena barva textu. Pak se pozadí nebude kreslit.
        /// Může být zadáno a přitom hodnota <see cref="BackgroundVisible"/> bude false = pak tato barva poslouží jako zdroj barvy pro barvu textu, pokud nebude uvedena <see cref="TextColor"/>.
        /// <para/>
        /// Pokud nebude zadána, a přitom bude zapotřebí jako výchozí barva pro další prvky, použije se barva bílá.
        /// </summary>
        public string BackColorName { get { return SerializeColor(BackColor); } set { BackColor = DeserializeColor(value); } }
        /// <summary>
        /// Barva pozadí, pro tmavé skiny.<br/>
        /// Může být null, pokud bude určena barva textu. Pak se pozadí nebude kreslit.
        /// Může být zadáno a přitom hodnota <see cref="BackgroundVisible"/> bude false = pak tato barva poslouží jako zdroj barvy pro barvu textu, pokud nebude uvedena <see cref="TextColor"/>.
        /// <para/>
        /// Pokud nebude zadána, a přitom bude zapotřebí jako výchozí barva pro další prvky, použije se barva bílá.
        /// </summary>
        public Color? BackColorDark { get; set; }
        /// <summary>
        /// Barva pozadí, pro tmavé skiny.<br/>
        /// Může být null, pokud bude určena barva textu. Pak se pozadí nebude kreslit.
        /// Může být zadáno a přitom hodnota <see cref="BackgroundVisible"/> bude false = pak tato barva poslouží jako zdroj barvy pro barvu textu, pokud nebude uvedena <see cref="TextColor"/>.
        /// <para/>
        /// Pokud nebude zadána, a přitom bude zapotřebí jako výchozí barva pro další prvky, použije se barva bílá.
        /// </summary>
        public string BackColorDarkName { get { return SerializeColor(BackColorDark); } set { BackColorDark = DeserializeColor(value); } }
        /// <summary>
        /// Vykreslit plnou barvou pozadí ikony?<br/>
        /// Default = ano, lze potlačit hodnotou false.
        /// </summary>
        public bool? BackgroundVisible { get; set; }
        /// <summary>
        /// Barva okraje.<br/>
        /// Default = null, bude odvozena z barvy pozadí <see cref="BackColor"/>
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Barva okraje.<br/>
        /// Default = null, bude odvozena z barvy pozadí <see cref="BackColorName"/>
        /// </summary>
        public string BorderColorName { get { return SerializeColor(BorderColor); } set { BorderColor = DeserializeColor(value); } }
        /// <summary>
        /// Barva okraje, pro tmavé skiny.<br/>
        /// Default = null, bude odvozena z barvy pozadí <see cref=""/>
        /// </summary>
        public Color? BorderColorDark { get; set; }
        /// <summary>
        /// Barva okraje, pro tmavé skiny.<br/>
        /// Default = null, bude odvozena z barvy pozadí <see cref=""/>
        /// </summary>
        public string BorderColorDarkName { get { return SerializeColor(BorderColorDark); } set { BorderColorDark = DeserializeColor(value); } }
        /// <summary>
        /// Pokud nebude zadána barva okraje <see cref="BorderColor"/>, pak bude odvozena z barvy pozadí <see cref="BackColor"/> jako vhodná kontrastní.<br/>
        /// Zde se řídí, zda bude true = černá/bílá, anebo false = kontrastní plná barva.<br/>
        /// Defaultní hodnota = null: rámeček bude mít barvu jako je barva textu.
        /// </summary>
        public bool? BorderColorBW { get; set; }
        /// <summary>
        /// Vykreslit border ikony? Default = ano, lze potlačit hodnotou false.
        /// </summary>
        public bool? BorderVisible { get; set; }
        /// <summary>
        /// Volný prostor mezi fyzickým okrajem ikony a borderem (=Padding).<br/>
        /// Zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely dolů).
        /// <para/>
        /// Defaultní hodnota (pokud bude null) je 1px (32px ikona). Validní rozsah je 0 až 8.
        /// </summary>
        public int? Padding { get; set; }
        /// <summary>
        /// Šířka borderu.<br/>
        /// Zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru).
        /// <para/>
        /// Defaultní hodnota (pokud bude null) je 1px (32px ikona). Validní rozsah je 0 až 8.
        /// </summary>
        public int? BorderWidth { get; set; }
        /// <summary>
        /// Zaoblení rohů ikony.<br/>
        /// Zadávají se pixely vzhledem k 32px ikoně (pro menší ikony se proporionálně zmenší a zarovná na celé pixely nahoru).
        /// <para/>
        /// Defaultní hodnota (pokud bude null) je 1px (32px ikona). Validní rozsah je 0 (hranatý čtverec) až 32 (kolečko).
        /// </summary>
        public int? Rounding { get; set; }
        /// <summary>
        /// Typ písma
        /// </summary>
        public enum TextFontType
        {
            /// <summary>
            /// Neurčeno, necháme na grafice. Nebude se vepisovat. Zobrazuje se typicky ve stylu patkového písma = Serif
            /// </summary>
            Default,
            /// <summary>
            /// Serif = patkové, knižní, pro ERP se nepoužívá.
            /// </summary>
            Serif,
            /// <summary>
            /// SansSerif = bezpatkové, vhodné pro ERP, použije se pokud nebude zadáno jinak
            /// </summary>
            SansSerif,
            /// <summary>
            /// Tahoma
            /// </summary>
            Tahoma
        }
        #endregion
        #region Serializace a deserializace dat do/ze jména ikony
        /// <summary>
        /// Vrací text popisující všechna zadaná data v this instanci
        /// </summary>
        /// <returns></returns>
        protected override string Serialize()
        {
            // Povinný header formátu:
            SerializeStart(FullHeader);
            // První je text, ten nemá klíč = je povinný:
            SerializeText(null, Text);
            // Další hodnoty mohou mít libovolné pořadí, jsou identifikovány klíčem :
            SerializeBool("S", TextBold);
            SerializeText("F", TextFontName);
            SerializeColor("T", TextColor);
            SerializeColor("t", TextColorDark);
            SerializeBool("W", TextColorBW);
            SerializeColor("B", BackColor);
            SerializeColor("b", BackColorDark);
            SerializeBool("V", BackgroundVisible);
            SerializeColor("C", BorderColor);
            SerializeColor("c", BorderColorDark);
            SerializeBool("O", BorderColorBW);
            SerializeBool("I", BorderVisible);
            SerializeInt("D", Padding);
            SerializeInt("H", BorderWidth);
            SerializeInt("R", Rounding);
            return SerializeResult();
        }
        /// <summary>
        /// Do this instance vloží hodnoty z dodaného seializovaného textu
        /// </summary>
        /// <param name="serial"></param>
        protected override bool Deserialize(string serial)
        {
            Clear();
            bool isValid = TryParseSerial(serial, FullHeader, parse);
            return isValid;

            void parse(int index, string content)
            {
                if (index == 0) return;                                        // Na indexu 0 je FullHeader
                if (index == 1) Text = DeserializeString(content);             // Na indexu 1 je Text
                else if (TryParseContent(content, out var key, out var value)) // Na dalších pozicích jsou nenulové jednotlivé hodnoty, na jejich pořadí nezáleží
                {   // Na dalších pozicích jsou jednotlivé hodnoty (Key Value)
                    switch (key)
                    {   // Klíče musí odpovídat klíčům při serializaci, podle nich se deserializuje hodnota a ukládá do odpovídající property:
                        case "S": { TextBold = DeserializeBool(value); break; }
                        case "F": { TextFontName = DeserializeString(value); break; }
                        case "T": { TextColor = DeserializeColor(value); break; }
                        case "t": { TextColorDark = DeserializeColor(value); break; }
                        case "W": { TextColorBW = DeserializeBool(value); break; }
                        case "B": { BackColor = DeserializeColor(value); break; }
                        case "b": { BackColorDark = DeserializeColor(value); break; }
                        case "V": { BackgroundVisible = DeserializeBool(value); break; }
                        case "C": { BorderColor = DeserializeColor(value); break; }
                        case "c": { BorderColorDark = DeserializeColor(value); break; }
                        case "O": { BorderColorBW = DeserializeBool(value); break; }
                        case "I": { BorderVisible = DeserializeBool(value); break; }
                        case "D": { Padding = DeserializeInt(value); break; }
                        case "H": { BorderWidth = DeserializeInt(value); break; }
                        case "R": { Rounding = DeserializeInt(value); break; }
                    }
                }
            }
        }
        /// <summary>
        /// Vynuluje všechny property v tomto objektu
        /// </summary>
        protected override void Clear()
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
            Padding = null;
            BorderWidth = null;
            Rounding = null;
        }
        /// <summary>
        /// Kompletní záhlaví názvu ikony = <c>"@textargs"</c>
        /// </summary>
        private const string FullHeader = BaseHeader + Header;
        /// <summary>
        /// Čisté záhlaví názvu ikony bez úvodního @ = <c>"textargs"</c>
        /// </summary>
        internal const string Header = "textargs";
        #endregion
        #region Validace dat = kontrola rozsahu a doplnění odvozených hodnot   ==>   Tato část nepatří na server:
        /// <summary>
        /// Validuje data = namísto null dosadí defaulty, odvodí barvy, zajistí omezení číselných hodnot...<br/>
        /// Výsledkem je plně použitelná sada dat pro fyzickou tvorbu SVG ikony = nullable hodnoty mají hodnotu, barvy jsou naplněny.
        /// <para/>
        /// Tuto metodu <b><u>není třeba volat</u></b> v procesu definice ikony ani před serializací, protože zbytečně prodlužuje název ikony !!!
        /// <para/>
        /// Meotdu volá klient před tvorbou SVG ikony, a může ji volat i kdokoliv jiný, aby získal plná validní data.
        /// </summary>
        public override void Validate()
        {
            // Pokud ikona dosud nebyla validovaná, tak provádíme první validaci a proto si nyní uložíme jméno ikony, odpovídající zadanému stavu:
            if (!__IsValidated)
            {
                __ImageName = this.SvgImageName;
                __IsValidated = true;
            }

            // Základní defaulty namísto NULL, a kontrola validního rozsahu:
            if (Text == null) Text = "";
            TextBold = getDefault(TextBold, false);
            TextFont = getDefault(TextFont, TextFontType.SansSerif);
            TextColorBW = getDefault(TextColorBW, true);
            BackgroundVisible = getDefault(BackgroundVisible, true);
            // BorderColorBW = ... Ponecháme i hodnotu NULL => opsat barvu písma !!!     BorderColorBW = getDefault(BorderColorBW, false);
            BorderVisible = getDefault(BorderVisible, true);
            Padding = getDefaultInt(Padding, 1, 0, 8);
            BorderWidth = getDefaultInt(BorderWidth, 1, 0, 8);
            Rounding = getDefaultInt(Rounding, 8, 0, 32);


            //  Barvy a jejich vzájemné doplnění - jaká data máme k dispozici?
            bool hasText = !String.IsNullOrEmpty(Text);
            bool hasBackground = BackgroundVisible.Value;
            bool hasBorder = BorderVisible.Value && BorderWidth.Value > 0;
            
            Color? textColor, backColor, borderColor;

            // Základní pro světlý skin:
            validateColor(TextColor, BackColor, BorderColor, false, out textColor, out backColor, out borderColor);
            TextColor = textColor;
            BackColor = backColor;
            BorderColor = borderColor;

            // Nepovinné pro tmavý skin:
            bool isValid = validateColor(TextColorDark, BackColorDark, BorderColorDark, true, out textColor, out backColor, out borderColor);
            TextColorDark = (isValid ? textColor : TextColor);
            BackColorDark = (isValid ? backColor : BackColor);
            BorderColorDark = (isValid ? borderColor : BorderColor);


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
            bool validateColor(Color? textInp, Color? backInp, Color? borderInp, bool isDarkSkin, out Color? textOut, out Color? backOut, out Color? borderOut)
            {
                bool hasTextColor = textInp.HasValue;
                bool hasBackColor = backInp.HasValue;
                bool hasBorderColor = borderInp.HasValue;

                textOut = textInp;
                backOut = backInp;
                borderOut = borderInp;

                // Pokud není zadané nic, a jde o DarkSkin (ten není povinný), pak končíme:
                if (!hasTextColor && !hasBackColor && !hasBorderColor && isDarkSkin) return false;

                // a) Mám text a nemám jeho barvu:
                if (hasText && !hasTextColor)
                {   // Nemám barvu textu => ta musí vycházet z barvy BackColor; a pokud ani ji nemám, pak její default je Bílá:
                    if (!hasBackColor)
                    {   // Základní barva pozadí světle šedá / tmavošedá:
                        backOut = !isDarkSkin ? Color.FromArgb(240, 240, 240) : Color.FromArgb(32, 32, 32);
                        hasBackColor = true;
                    }
                    // Barva textu bude kontrastní k barvě BackColor, a to buď černobílá, nebo shodný odstín a kontrastní světelnost a plná sytost:
                    textOut = GetContrastTextColor(backOut.Value, TextColorBW.Value, true);
                    hasTextColor = true;
                }

                // b) Mám mít pozadí a nemám jeho barvu:
                if (hasBackground && !hasBackColor)
                {   // Nemám barvu pozadí => buď vychází z barvy textu, jako světlá kontrastní; anebo bude bílá:
                    if (hasTextColor)
                    {
                        backOut = GetBackColor(textInp.Value);
                        hasBackColor = true;
                    }
                    else if (hasBorderColor)
                    {
                        backOut = GetBackColor(borderOut.Value);
                        hasBackColor = true;
                    }
                    else
                    {
                        backOut = Color.White;
                        hasBackColor = true;
                    }
                }

                // c) Mám mít border a nemám jeho barvu:
                if (hasBorder && !hasBorderColor)
                {
                    if (BorderColorBW.HasValue && hasBackColor)
                    {   // Máme BorderColor vytvořit jako Black&White variantu k barvě pozadí, a tu mám k dispozici:
                        borderOut = GetContrastBorderColor(backOut.Value, BorderColorBW.Value, true);
                        hasBorderColor = true;
                    }
                    else if (hasTextColor)
                    {
                        borderOut = textOut;
                        hasBorderColor = true;
                    }
                    else
                    {
                        borderOut = Color.Black;
                        hasBorderColor = true;
                    }
                }
                return true;
            }
        }
        #endregion
    }
    #region abstract base class SvgImageIcon
    /// <summary>
    /// Bázová třída, obsahuje support pro serializaci, deserializaci i validaci
    /// </summary>
    internal abstract class SvgImageIcon
    {
        #region Factory
        /// <summary>
        /// Deserializuje dodaný string do instance odpovídajícího typu (konkrétní potomek třídy <see cref="SvgImageIcon"/> a vrátí ji.
        /// Pokud vstupní string není validní, vrátí false.
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="svgImageIcon"></param>
        /// <returns></returns>
        public static bool TryParse(string serial, out SvgImageIcon svgImageIcon)
        {
            if (!String.IsNullOrEmpty(serial) && serial.Length > 3 && serial.StartsWith(BaseHeader))
            {
                if (SvgImageTextIcon.TryParse(serial, out var svgImageTextIcon)) { svgImageIcon = svgImageTextIcon; return true; }
                // další třídy...
            }
            svgImageIcon = null;
            return false;
        }
        /// <summary>
        /// Validuje data = namísto null dosadí defaulty, odvodí barvy, zajistí omezení číselných hodnot...<br/>
        /// Výsledkem je plně použitelná sada dat pro fyzickou tvorbu SVG ikony.
        /// <para/>
        /// Tuto metodu <b><u>není třeba volat</u></b> v procesu definice ikony ani před serializací, protože zbytečně prodlužuje název ikony !!!
        /// <para/>
        /// Meotdu volá klient před tvorbou SVG ikony, a může ji volat 
        /// </summary>
        public virtual void Validate() { }
        /// <summary>
        /// Obsahuje "jméno ikony" = klíčové slovo a plný obsah všech zadaných dat
        /// </summary>
        public string SvgImageName { get { return Serialize(); } set { Deserialize(value); } }
        /// <summary>
        /// Vrací text popisující všechna zadaná data v this instanci
        /// </summary>
        /// <returns></returns>
        protected abstract string Serialize();
        /// <summary>
        /// Do this instance vloží hodnoty z dodaného seializovaného textu
        /// </summary>
        /// <param name="serial"></param>
        protected abstract bool Deserialize(string serial);
        /// <summary>
        /// Vynuluje všechny property v tomto objektu
        /// </summary>
        protected virtual void Clear() { }
        /// <summary>
        /// Začátek názvu ikony = <c>"@"</c>
        /// </summary>
        protected const string BaseHeader = "@";
        #endregion
        #region Support pro Serializaci
        /// <summary>
        /// Zahájí proces serializace
        /// </summary>
        /// <param name="fullHeader"></param>
        protected void SerializeStart(string fullHeader)
        {
            __Serializer = new StringBuilder();
            __Serializer.Append(fullHeader);

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

            string serial = SerializeColor(value);

            // Null:
            if (serial is null) return;                             // Null neserializujeme

            // Delimiter:
            __Serializer.Append(Delimiter);

            // Key:
            if (key != null && key.Length == 1)
                __Serializer.Append(key);
            __Serializer.Append(serial);
        }
        /// <summary>
        /// Serialiuje danou barvu na string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string SerializeColor(Color? value)
        {
            if (!value.HasValue) return null;

            Color color = value.Value;
            if (color.IsNamedColor)
                return color.Name;
            else if (color.IsKnownColor)
                return color.ToKnownColor().ToString();
            else
                return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
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
            // Informace: KnownColor a NamedColor mají shodné názvy (viz serializace barvy: if (color.IsNamedColor) a else if (color.IsKnownColor) : ukládají Name barvy, které je shodné pro Name a KnownColor ...
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
        #region Color transformace   ==>   Tato část nepatří na server:
        /// <summary>
        /// Vrátí kontrastní barvu pro text, k barvě zadané pozadí <paramref name="baseColor"/>.
        /// <para/>
        /// Pokud hodnota <paramref name="contrastBW"/> je true, pak vrátí černou nebo bílou.<br/>
        /// Pokud <paramref name="contrastBW"/> je false a hodnota <paramref name="contrastRatio"/> je zadaná, pak vrací barvu , která je kontrastní k zadané barvě, v dané míře 0 - 1.
        /// </summary>
        /// <param name="baseColor">Výchozí barva</param>
        /// <param name="contrastBW">Požadavek na Black / White kontrastní barvu</param>
        /// <param name="contrastBrightness"></param>
        /// <param name="contrastRatio">Míra sytosti kontrastní barvy, vrozsahu 0 = šedá až 1 = plná sytá barva, opačná než zadaná</param>
        /// <returns></returns>
        protected static Color GetContrastTextColor(Color baseColor, bool contrastBW, bool contrastBrightness, float? contrastRatio = null)
        {
            var hslColor = Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.ColorToHsl(baseColor);
            if (contrastBW)
            {   // Požadavek: kontrastní barva má být Černá nebo Bílá = podle jasu bázové barvy:
                bool isLight = (hslColor.Light >= 50d);              // Pokud vstupní barva je světlá ...
                return (isLight ? ColorBlack : ColorWhite);          //  ... pak kontrastní je černá, a naopak.
            }
            else if (contrastBrightness)
            {   // Požadavek: máme vrátit kontrastní barvu stejného odstínu (ke světle modré => tmavomodoru):
                hslColor.Saturation = 95d;                           //  a bude plně sytá.
                hslColor.Light = getContrastLight(hslColor.Light);   //  ... pak kontrastní je tmavá, a naopak;
                return hslColor.ToColor();
            }
            else 
            {   // Požadavek: máme vrátit barvu opačnou = reverzní => opačný odstín a kontrastní:
                hslColor.Hue = hslColor.Hue + 180d;                  //  ... a opačná barva (kolo barev Hue má 360°)
                hslColor.Saturation = 95d;                           //  a bude plně sytá.
                hslColor.Light = getContrastLight(hslColor.Light);   //  ... pak kontrastní je tmavá, a naopak;
                return hslColor.ToColor();
            }

            double getContrastLight(double light)
            {
                if (light < 30d) return 85d;
                if (light < 50d) return 95d;
                if (light <= 70d) return 5d;
                return 15d;
            }
        }
        /// <summary>
        /// Vrátí kontrastní barvu pro Border k barvě zadané pozadí <paramref name="baseColor"/>.
        /// <para/>
        /// Pokud hodnota <paramref name="contrastBW"/> je true, pak vrátí černou nebo bílou.<br/>
        /// Pokud <paramref name="contrastBW"/> je false a hodnota <paramref name="contrastRatio"/> je zadaná, pak vrací barvu , která je kontrastní k zadané barvě, v dané míře 0 - 1.
        /// </summary>
        /// <param name="baseColor">Výchozí barva</param>
        /// <param name="contrastBW">Požadavek na Black / White kontrastní barvu</param>
        /// <param name="contrastBrightness"></param>
        /// <param name="contrastRatio">Míra sytosti kontrastní barvy, vrozsahu 0 = šedá až 1 = plná sytá barva, opačná než zadaná</param>
        /// <returns></returns>
        protected static Color GetContrastBorderColor(Color baseColor, bool contrastBW, bool contrastBrightness, float? contrastRatio = null)
        {
            var hslColor = Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.ColorToHsl(baseColor);
            if (contrastBW)
            {   // Požadavek: kontrastní barva má být Černá nebo Bílá = podle jasu bázové barvy:
                bool isLight = (hslColor.Light >= 50d);              // Pokud vstupní barva je světlá ...
                return (isLight ? ColorBlack : ColorWhite);          //  ... pak kontrastní je černá, a naopak.
            }
            else if (contrastBrightness)
            {   // Požadavek: máme vrátit kontrastní barvu stejného odstínu (ke světle modré => tmavomodoru):
                hslColor.Saturation = 95d;                           //  a bude plně sytá.
                hslColor.Light = getContrastLight(hslColor.Light);   //  ... pak kontrastní je tmavá, a naopak;
                return hslColor.ToColor();
            }
            else
            {   // Požadavek: máme vrátit barvu opačnou = reverzní => opačný odstín a kontrastní:
                hslColor.Hue = hslColor.Hue + 180d;                  //  ... a opačná barva (kolo barev Hue má 360°)
                hslColor.Saturation = 95d;                           //  a bude plně sytá.
                hslColor.Light = getContrastLight(hslColor.Light);   //  ... pak kontrastní je tmavá, a naopak;
                return hslColor.ToColor();
            }

            double getContrastLight(double light)
            {
                if (light < 30d) return 85d;
                if (light < 50d) return 95d;
                if (light <= 70d) return 5d;
                return 15d;
            }
        }
        /// <summary>
        /// Skoro černá barva, která ale neprovádí transformaci při změně skinu Světlý - Tmavý
        /// </summary>
        protected static Color ColorBlack { get { return Color.FromArgb(16, 16, 16); } }
        /// <summary>
        /// Skoro bílá barva, která ale neprovádí transformaci při změně skinu Světlý - Tmavý
        /// </summary>
        protected static Color ColorWhite { get { return Color.FromArgb(240, 240, 240); } }
        /// <summary>
        /// Vrátí barvu pro pozadí k dané barvě
        /// </summary>
        /// <param name="baseColor"></param>
        /// <returns></returns>
        protected static Color GetBackColor(Color baseColor)
        {
            var hslColor = Noris.Clients.Win.Components.AsolDX.Colors.ColorConverting.ColorToHsl(baseColor);
            bool isLight = (hslColor.Light >= 50d);                  // Pokud vstupní barva je světlá,
            hslColor.Light = (isLight ? 10d : 90d);                  //  ... pak kontrastní je téměř tmavá, a naopak;
            hslColor.Saturation = 90d;
            return hslColor.ToColor();
        }
        #endregion
        #region Support pro tvorbu SVG ikony

        #endregion
    }
    #endregion
    #endregion
}

namespace Noris.Clients.Win.Components.AsolDX.Colors
{
    /*   PÁR INFORMACÍ Z WIKI k obecnému pochopení barevných prostorů:
       https://www.designui.cz/lekce/co-jsou-to-barevne-modely-rgb-hsl-a-hsb-a-ktery-je-lepsi

    - HSL (Hue-Saturation-Lightness) je jiný formát, pro grafickou práci vhodnější, popisuje barvy přirozeně podle lidského vnímání: 
       Hue              = odstín (Červená 0° - Žlutá 60° - Zelená 120° - Tyrkysová 180° - Modrá 240° - Purpurová 300°), jako postupné barevné přechody
       Saturation       = sytost (0 = šedá, až po 100 = zcela sytá cílová barva podle Hue)
                            Pokud Saturation = 0, jde o šedou barvu bez ohledu na hodnotu Hue
       Light            = světlost (0 = černá, 50 = nejzřetelnější barva, 100 = bílá)
        => Pro snadnou práci s přirozenou barvou je HSL nejvhodnější.
         https://www.designui.cz/lekce/co-jsou-to-barevne-modely-rgb-hsl-a-hsb-a-ktery-je-lepsi#barevny-model-hsl-pod-lupou

    - HSV (Hue-Saturation-Value)  (nebo též HSB (Hue-Saturation-Brightness) je podobný jako HSL = má stejné paametry Hue a Saturation, ale místo Light používá Value (=Brightness) = Jasnost barvy.
       Saturation       = sytost (0 = šedá, až po 100 = zcela sytá cílová barva podle Hue)
                            Pokud Saturation = 0, jde o šedou barvu bez ohledu na hodnotu Hue
       Value            = Jas (0=černá, 100 = čistá barva podle odstínu). Ale pokud chci barvu světlejší, než plnou barvu, pak musím snižovat hodnotu Saturation směrem k 0 = až k bílé.
                            Bílá barva má tedy Saturation = 0 a Brightness = 100 (na Hue pak nezáleží).
         https://www.designui.cz/lekce/co-jsou-to-barevne-modely-rgb-hsl-a-hsb-a-ktery-je-lepsi#barevny-model-hsb
    
    - RGB (Red-Green-Blue) je technologický formát, kde jedna hodnota (R,G,B) ovládá jas jedné barvy LED diody. 
       Ale změna jasu nebo odstínu vyžaduje koordinovanou změnu všech tří složek. 
       Pro grafickou práci to není vhodný formát.

    - Online konvertory barevných systémů:
         https://www.rapidtables.com/convert/color/index.html
    
    */
    #region class HslColor
    /// <summary>
    /// Represents a HSL color space.<br/>
    /// HSL barva je vhodnější pro snadnou modifikaci odstínu, sytosti a světlosti než RGB i než HSV.
    /// <para/>
    /// <see href="http://en.wikipedia.org/wiki/HSV_color_space"/>
    /// </summary>
    internal sealed class HslColor : AnyColor
    {
        public HslColor(double hue, double saturation, double light, int alpha)
        {
            Hue = hue;
            Saturation = saturation;
            Light = light;
            Alpha = alpha;
        }
        /// <summary>
        /// Gets the precise hue. Values from 0 to 360.<br/>
        /// Hue = odstín (Červená 0° - Žlutá 60° - Zelená 120° - Tyrkysová 180° - Modrá 240° - Purpurová 300°), jako postupné barevné přechody
        /// </summary>
        public double Hue { get { return __Hue; } set { __Hue = Cycle(value, 360d); } } private double __Hue;
        /// <summary>
        /// Gets the precise saturation. Values from 0 to 100.<br/>
        /// Saturation = sytost (0 = šedá, až po 100 = zcela sytá cílová barva podle Hue)
        /// Pokud Saturation = 0, jde o šedou barvu bez ohledu na hodnotu Hue
        /// </summary>
        public double Saturation { get { return __Saturation; } set { __Saturation = Align(value, 0d, 100d); } } private double __Saturation;
        /// <summary>
        /// Gets the precise light. Values from 0 to 100.<br/>
        /// Light (nebo Value) = světlost (0 = černá, 50 = nejzřetelnější barva, 100 = bílá)
        /// </summary>
        public double Light { get { return __Light; } set { __Light = Align(value, 0d, 100d); } } private double __Light;
        /// <summary>
        /// Gets the alpha. Values from 0 to 255<br/>
        /// Alfa kanál = krytí barvy (neprůhlednost): 0 = zcela průhledná, 255 = zcela plná barva, podklad nebude prosvítat
        /// </summary>
        public int Alpha { get { return __Alpha; } set { __Alpha = Align(value, 0, 255); } } private int __Alpha;
        /// <summary>
        /// Gets the hue. Values from 0 to 360.
        /// </summary>
        public int IntHue => Convert.ToInt32(Hue);
        /// <summary>
        /// Gets the saturation. Values from 0 to 100.
        /// </summary>
        public int IntSaturation => Convert.ToInt32(Saturation);
        /// <summary>
        /// Gets the light. Values from 0 to 100.
        /// </summary>
        public int IntLight => Convert.ToInt32(Light);

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
                color.Hue,
                color.Saturation,
                color.Light,
                color.Alpha);
        }

        public static HslColor FromHsbColor(HsvColor color)
        {
            return FromRgbColor(color.ToRgbColor());
        }

        public override string ToString()
        {
            return $"Hue: {IntHue}; Saturation: {IntSaturation}; Light (=Value): {IntLight}; Alpha: {Alpha}";
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

        public HsvColor ToHsbColor()
        {
            return ColorConverting.RgbToHsv(ToRgbColor());
        }

        public override bool Equals(object obj)
        {
            var equal = false;

            if (obj is HslColor color)
            {
                if (Math.Abs(IntHue - color.Hue) < 0.00001 &&
                    Math.Abs(IntSaturation - color.Saturation) < 0.00001 &&
                    Math.Abs(IntLight - color.Light) < 0.00001 &&
                    Alpha == color.Alpha)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"H:{Hue}-S:{Saturation}-L:{Light}-A:{Alpha}".GetHashCode();
        }
    }
    #endregion
    #region class HsvColor
    /// <summary>
    /// Represents a HSV color space.
    /// <para/>
    /// <see href="http://en.wikipedia.org/wiki/HSV_color_space"/>
    /// </summary>
    internal sealed class HsvColor : AnyColor
    {
        public HsvColor(double hue, double saturation, double value, int alpha)
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
            Alpha = alpha;
        }
        /// <summary>
        /// Gets or sets the hue. Values from 0 to 360.<br/>
        /// odstín (Červená 0° - Žlutá 60° - Zelená 120° - Tyrkysová 180° - Modrá 240° - Purpurová 300°), jako postupné barevné přechody
        /// </summary>
        public double Hue { get { return __Hue; } set { __Hue = Cycle(value, 360d); } } private double __Hue;
        /// <summary>
        /// Gets or sets the saturation. Values from 0 to 100.<br/>
        /// Saturation = sytost (0 = šedá, až po 100 = zcela sytá cílová barva podle Hue);
        /// Pokud Saturation = 0, jde o šedou barvu bez ohledu na hodnotu Hue
        /// </summary>
        public double Saturation { get { return __Saturation; } set { __Saturation = Align(value, 0d, 100d); } } private double __Saturation;
        /// <summary>
        /// Gets or sets the brightness. Values from 0 to 100.<br/>
        /// Brightness = Jas (0=černá, 100 = čistá barva podle odstínu). Ale pokud chci barvu světlejší, než plnou barvu, pak musím snižovat hodnotu Saturation směrem k 0 = až k bílé.
        /// Bílá barva má tedy Saturation = 0 a Brightness = 100 (na Hue pak nezáleží).
        /// </summary>
        public double Value { get { return __Value; } set { __Value = Align(value, 0d, 100d); } } private double __Value;
        /// <summary>
        /// Gets or sets the alpha. Values from 0 to 255.<br/>
        /// Alfa kanál = krytí barvy (neprůhlednost): 0 = zcela průhledná, 255 = zcela plná barva, podklad nebude prosvítat
        /// </summary>
        public int Alpha { get { return __Alpha; } set { __Alpha = Align(value, 0, 255); } } private int __Alpha;
        /// <summary>
        /// Gets or sets the hue. Values from 0 to 360.
        /// </summary>
        public int IntHue => Convert.ToInt32(Hue);
        /// <summary>
        /// Gets or sets the saturation. Values from 0 to 100.
        /// </summary>
        public int IntSaturation => Convert.ToInt32(Saturation);
        /// <summary>
        /// Gets or sets the brightness. Values from 0 to 100.
        /// </summary>
        public int IntValue => Convert.ToInt32(Value);

        public static HsvColor FromColor(Color color)
        {
            return ColorConverting.ColorToRgb(color).ToHsvColor();
        }

        public static HsvColor FromRgbColor(RgbColor color)
        {
            return color.ToHsvColor();
        }

        public static HsvColor FromHsvColor(HsvColor color)
        {
            return new(color.Hue, color.Saturation, color.Value, color.Alpha);
        }

        public static HsvColor FromHslColor(HslColor color)
        {
            return FromRgbColor(color.ToRgbColor());
        }

        public override string ToString()
        {
            return $"Hue: {IntHue}; Saturation: {IntSaturation}; Brightness: {IntValue}; Alpha: {Alpha}";
        }

        public Color ToColor()
        {
            return ColorConverting.HsvToRgb(this).ToColor();
        }

        public RgbColor ToRgbColor()
        {
            return ColorConverting.HsvToRgb(this);
        }

        public HsvColor ToHsvColor()
        {
            return new(Hue, Saturation, Value, Alpha);
        }

        public HslColor ToHslColor()
        {
            return ColorConverting.RgbToHsl(ToRgbColor());
        }

        public override bool Equals(object obj)
        {
            var equal = false;

            if (obj is HsvColor color)
            {
                if (Math.Abs(Hue - color.Hue) < 0.00001 &&
                    Math.Abs(Saturation - color.Saturation) < 0.00001 &&
                    Math.Abs(Value - color.Value) < 0.00001 &&
                    Alpha == color.Alpha)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"H:{IntHue}-S:{IntSaturation}-B:{IntValue}-A:{Alpha}".GetHashCode();
        }
    }
    #endregion
    #region class RgbColor
    /// <summary>
    /// Represents a RGB color space.
    /// <para/>
    /// <see href="http://en.wikipedia.org/wiki/HSV_color_space"/>
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

        public static RgbColor FromHsvColor(HsvColor color)
        {
            return color.ToRgbColor();
        }

        public static RgbColor FromHslColor(HslColor color)
        {
            return color.ToRgbColor();
        }

        public override string ToString()
        {
            return $"Red: {Red}; Green: {Green}; Blue: {Blue}; Alpha: {Alpha}";
        }

        public Color ToColor()
        {
            return ColorConverting.RgbToColor(this);
        }

        public RgbColor ToRgbColor()
        {
            return this;
        }

        public HsvColor ToHsvColor()
        {
            return ColorConverting.RgbToHsv(this);
        }

        public HslColor ToHslColor()
        {
            return ColorConverting.RgbToHsl(this);
        }

        public override bool Equals(object obj)
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
    #region class ColorConverting a abstract class AnyColor
    /// <summary>
    /// Provides color conversion functionality.
    /// <para/>
    /// See: <see href="https://www.rapidtables.com/convert/color/index.html"/>;<br/>
    /// See: <see href="http://en.wikipedia.org/wiki/HSV_color_space"/>;<br/>
    /// See: <see href="http://www.easyrgb.com/math.php?MATH=M19#text19"/>;<br/>
    /// </summary>
    internal class ColorConverting : AnyColor
    {
        #region Public konverze finálních datových typů
        public static RgbColor HsvToRgb(HsvColor hsv)
        {
            _HsvToRgb(hsv.Hue, hsv.Saturation, hsv.Value, out var red, out var green, out var blue);
            return new RgbColor(red, green, blue, hsv.Alpha);
        }
        public static Color HsvToColor(HsvColor hsb)
        {
            _HsvToRgb(hsb.IntHue, hsb.IntSaturation, hsb.IntValue, out var red, out var green, out var blue);
            return Color.FromArgb(hsb.Alpha, red, green, blue);
        }

        public static RgbColor HslToRgb(HslColor hsl)
        {
            _HslToRgb(hsl.Hue, hsl.Saturation, hsl.Light, out int red, out int green, out int blue);
            return new RgbColor(red, green, blue, hsl.Alpha);
        }
        public static Color HslToColor(HslColor hsl)
        {
            _HslToRgb(hsl.Hue, hsl.Saturation, hsl.Light, out int red, out int green, out int blue);
            return Color.FromArgb(hsl.Alpha, red, green, blue);
        }

        public static RgbColor ColorToRgb(Color color)
        {
            return new RgbColor(color.R, color.G, color.B, color.A);
        }
        public static HsvColor ColorToHsv(Color color)
        {
            _RgbToHsv(color.R, color.G, color.B, out double hue, out double saturation, out double value);
            return new HsvColor(hue, saturation, value, color.A);
        }
        public static HslColor ColorToHsl(Color color)
        {
            _RgbToHsl(color.R, color.G, color.B, out double hue, out double saturation, out double light);
            return new HslColor(hue, saturation, light, color.A);
        }

        public static Color RgbToColor(RgbColor rgb)
        {
            return Color.FromArgb(rgb.Alpha, rgb.Red, rgb.Green, rgb.Blue);
        }
        public static HsvColor RgbToHsv(RgbColor rgb)
        {
            _RgbToHsv(rgb.Red, rgb.Green, rgb.Blue, out double hue, out double saturation, out double value);
            return new HsvColor(hue, saturation, value, rgb.Alpha);
        }
        public static HslColor RgbToHsl(RgbColor rgb)
        {
            _RgbToHsl(rgb.Red, rgb.Green, rgb.Blue, out double hue, out double saturation, out double light);
            return new HslColor(hue, saturation, light, rgb.Alpha);
        }
        #endregion
        #region Otestování
        public static void Test()
        {
            string errors = "";
            //                              RGB               HSV             HSL
            testOne(Color.LightYellow,     255, 255, 224,    60, 12, 100,    60, 100, 94);
            testOne(Color.DarkViolet,      148, 0, 211,      282, 100, 83,   282, 100, 41);
            testOne(Color.MediumTurquoise, 72, 209, 204,     178, 66, 82,    178, 60, 55);

            if (errors.Length > 0)
                throw new InvalidOperationException("ColorConverting error:\r\n" + errors);



            void testOne(Color color, int rgbR, int rgbG, int rgbB, int hsvH, int hsvS, int hsvV, int hslH, int hslS, int hslL)
            {
                string colorName = color.Name;
                checkOne(colorName, "SYS.R", color.R, rgbR);
                checkOne(colorName, "SYS:G", color.G, rgbG);
                checkOne(colorName, "SYS:B", color.B, rgbB);

                var rgbColor = ColorToRgb(color);
                checkOne(colorName, "RGB.Red", rgbColor.Red, rgbR);
                checkOne(colorName, "RGB:Green", rgbColor.Green, rgbG);
                checkOne(colorName, "RGB:Blue", rgbColor.Blue, rgbB);


                var hsvColor = ColorToHsv(color);
                checkOne(colorName, "HSV:Hue", hsvColor.Hue, hsvH);
                checkOne(colorName, "HSV:Saturation", hsvColor.Saturation, hsvS);
                checkOne(colorName, "HSV:Value", hsvColor.Value, hsvV);

                var hsvRgbResult = hsvColor.ToRgbColor();
                checkOne(colorName, "HSVtoRGB.Red", hsvRgbResult.Red, rgbR);
                checkOne(colorName, "HSVtoRGB:Green", hsvRgbResult.Green, rgbG);
                checkOne(colorName, "HSVtoRGB:Blue", hsvRgbResult.Blue, rgbB);


                var hslColor = ColorToHsl(color);
                checkOne(colorName, "HSL:Hue", hslColor.Hue, hslH);
                checkOne(colorName, "HSL:Saturation", hslColor.Saturation, hslS);
                checkOne(colorName, "HSL:Value", hslColor.Light, hslL);

                var hslRgbResult = hslColor.ToRgbColor();
                checkOne(colorName, "HSLtoRGB.Red", hslRgbResult.Red, rgbR);
                checkOne(colorName, "HSLtoRGB:Green", hslRgbResult.Green, rgbG);
                checkOne(colorName, "HSLtoRGB:Blue", hslRgbResult.Blue, rgbB);
            }


            void checkOne(string colorName, string valueName, double valueCurrent, double valueExpected)
            {
                var valueDiff = valueCurrent - valueExpected;
                if (Math.Abs(valueDiff) < 1d) return;               // OK
                errors += $"{colorName} fail: {valueName} = {valueCurrent}; Expected = {valueExpected}; Difference = {valueDiff}.\r\n";
            }


            /*
				    HEX         RGB			    Color.HSB		    PaintNet HSV		Convert HSV			Convert HSL		    C# HSV			C# HSL
LightYellow	    	FFFFE0	    255 255 224		60 100	94		    60 12 100	    	60 12 100			60 100 94	    	60 12 100		60 100 94
DarkViolet	    	9400D3	    148 0 211		282 100 41		    282 100 82	    	282 100 83			282 100 41	    	282 100 83		282 100 41
MediumTurquoise		48D1CC	    72 209 204		178 60 55		    177 65 81	    	178 66 82			178 60 55	    	178 66 82		178 60 55

            */
        }
        #endregion
        #region Primitivní privátní konverze barevných prostorů
        /// <summary>
        /// Konverze HSV to RGB
        /// </summary>
        /// <param name="hue">Inp HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Inp SATURATION v rozsahu 0 - 100d</param>
        /// <param name="value">Inp VALUE v rozsahu 0 - 100d</param>
        /// <param name="red">Out RED v rozsahu 0 - 255</param>
        /// <param name="green">Out GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Out BLUE v rozsahu 0 - 255</param>
        /// <returns></returns>
        private static void _HsvToRgb(double hue, double saturation, double value, out int red, out int green, out int blue)
        {
            double redRatio = 0d;
            double greenRatio = 0d;
            double blueRatio = 0d;

            hue = hue % 360d;
            while (hue < 0d) hue += 360d;
            var saturRatio = GetRatio(saturation, 100d, 0d, 1d);
            var valueRatio = GetRatio(value, 100d, 0d, 1d);

            if (Math.Abs(saturRatio - 0) < 0.00001)
            {
                redRatio = valueRatio;
                greenRatio = valueRatio;
                blueRatio = valueRatio;
            }
            else
            {
                var sectorPosition = hue / 60d;
                var sectorNumber = (int)Math.Floor(sectorPosition);
                var fractionalSector = sectorPosition - sectorNumber;

                var p = valueRatio * (1 - saturRatio);
                var q = valueRatio * (1 - saturRatio * fractionalSector);
                var t = valueRatio * (1 - saturRatio * (1 - fractionalSector));

                // Assign the fractional colors to r, g, and b
                // based on the sector the angle is in.
                switch (sectorNumber)
                {
                    case 0:
                        redRatio = valueRatio;
                        greenRatio = t;
                        blueRatio = p;
                        break;

                    case 1:
                        redRatio = q;
                        greenRatio = valueRatio;
                        blueRatio = p;
                        break;

                    case 2:
                        redRatio = p;
                        greenRatio = valueRatio;
                        blueRatio = t;
                        break;

                    case 3:
                        redRatio = p;
                        greenRatio = q;
                        blueRatio = valueRatio;
                        break;

                    case 4:
                        redRatio = t;
                        greenRatio = p;
                        blueRatio = valueRatio;
                        break;

                    case 5:
                        redRatio = valueRatio;
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
        /// Konverze RGB to HSV
        /// </summary>
        /// <param name="red">Inp RED v rozsahu 0 - 255</param>
        /// <param name="green">Inp GREEN v rozsahu 0 - 255</param>
        /// <param name="blue">Inp BLUE v rozsahu 0 - 255</param>
        /// <param name="hue">Out HUE v rozsahu 0 - 360d</param>
        /// <param name="saturation">Out SATURATION v rozsahu 0 - 100d</param>
        /// <param name="value">Out VALUE v rozsahu 0 - 100d</param>
        private static void _RgbToHsv(double red, double green, double blue, out double hue, out double saturation, out double value)
        {
            double redRatio = GetRatio(red, 255d, 0d, 1d);
            double greenRatio = GetRatio(green, 255d, 0d, 1d);
            double blueRatio = GetRatio(blue, 255d, 0d, 1d);

            // _NOTE #1: Even though we're dealing with a very small range of
            // numbers, the accuracy of all calculations is fairly important.
            // For this reason, I've opted to use double data types instead
            // of float, which gives us a little bit extra precision (recall
            // that precision is the number of significant digits with which
            // the result is expressed).
            var minValue = GetMinValue(redRatio, greenRatio, blueRatio);
            var maxValue = GetMaxValue(redRatio, greenRatio, blueRatio);
            var delta = maxValue - minValue;

            double hueRatio = 0d;
            double saturValue;
            var valuValue = maxValue * 100d;

            if (Math.Abs(maxValue - 0d) < 0.00001d || Math.Abs(delta - 0d) < 0.00001d)
            {
                hueRatio = 0d;
                saturValue = 0d;
            }
            else
            {
                // _NOTE #2: FXCop insists that we avoid testing for floating 
                // point equality (CA1902). Instead, we'll perform a series of
                // tests with the help of 0.00001 that will provide 
                // a more accurate equality evaluation.

                if (Math.Abs(minValue - 0) < 0.00001d)
                {
                    saturValue = 100;
                }
                else
                {
                    saturValue = delta / maxValue * 100d;
                }

                if (Math.Abs(redRatio - maxValue) < 0.00001d)
                {
                    hueRatio = (greenRatio - blueRatio) / delta;
                }
                else if (Math.Abs(greenRatio - maxValue) < 0.00001d)
                {
                    hueRatio = 2 + (blueRatio - redRatio) / delta;
                }
                else if (Math.Abs(blueRatio - maxValue) < 0.00001d)
                {
                    hueRatio = 4 + (redRatio - greenRatio) / delta;
                }
            }

            hue = Cycle(Math.Round(60d * hueRatio, 3), 360d);
            saturation = Math.Round(saturValue, 3);
            value = Math.Round(valuValue, 3);
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
            var redRatio = GetRatio(red, 255d, 0d, 1d);
            var greenRatio = GetRatio(green, 255d, 0d, 1d);
            var blueRatio = GetRatio(blue, 255d, 0d, 1d);

            var varMin = GetMinValue(redRatio, greenRatio, blueRatio);    // Min. value of RGB
            var varMax = GetMaxValue(redRatio, greenRatio, blueRatio);    // Max. value of RGB
            var delMax = varMax - varMin;                                 // Delta RGB value

            double hueRatio;
            double satRatio;
            var lightRatio = (varMax + varMin) / 2d;

            if (Math.Abs(delMax - 0) < 0.00001d) //This is a gray, no chroma...
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
            }

            hue = Cycle(Math.Round(hueRatio * 360.0d, 3), 360d);
            saturation = Math.Round(satRatio * 100.0d, 3);
            light = Math.Round(lightRatio * 100.0d, 3);
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

            var hueRatio = Cycle(hue, 360d) / 360.0d;
            var saturRatio = GetRatio(saturation, 100d, 0d, 1d);
            var lightRatio = GetRatio(light, 100d, 0d, 1d);

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

                redRatio = hue2Rgb(var1, var2, hueRatio + 1.0 / 3.0);
                greenRatio = hue2Rgb(var1, var2, hueRatio);
                blueRatio = hue2Rgb(var1, var2, hueRatio - 1.0 / 3.0);
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
        #endregion
    }
    /// <summary>
    /// Bázová třída pro barvy, obsahuje pouze pomocné metody pro zarovnání číselných hodnot
    /// </summary>
    internal class AnyColor
    {
        /// <summary>
        /// Vrátí danou hodnotu <paramref name="value"/> zarovnanou do mezí <paramref name="minValue"/> až <paramref name="maxValue"/>, včetně obou mezí.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected static int Align(int value, int minValue, int maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        /// <summary>
        /// Vrátí danou hodnotu <paramref name="value"/> zarovnanou do mezí <paramref name="minValue"/> až <paramref name="maxValue"/>, včetně obou mezí.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        protected static double Align(double value, double minValue, double maxValue)
        {
            return (value < minValue ? minValue : (value > maxValue ? maxValue : value));
        }
        /// <summary>
        /// Hodnotu <paramref name="value"/> vydělí dělitelem <paramref name="divider"/> a zarovná do mezí <paramref name="minRatio"/> až <paramref name="maxRatio"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="divider"></param>
        /// <param name="minRatio"></param>
        /// <param name="maxRatio"></param>
        /// <returns></returns>
        protected static double GetRatio(double value, double divider, double minRatio, double maxRatio)
        {
            double ratio = value / divider;
            ratio = (ratio < minRatio ? minRatio : (ratio > maxRatio ? maxRatio : ratio));
            return ratio;
        }
        /// <summary>
        /// Determines the maximum value of all of the numbers provided in the
        /// variable argument list.
        /// </summary>
        protected static double GetMaxValue(params double[] values)
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
        protected static double GetMinValue(params double[] values)
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
        /// <summary>
        /// Vrátí danou hodnotu cyklovanou do prostoru 0 až (cycle).<br/>
        /// Tedy pro vstup <paramref name="value"/> = 270 a <paramref name="cycle"/> = 360 vrátí 270;<br/>
        /// ale pro vstup <paramref name="value"/> = 420 a <paramref name="cycle"/> = 360 vrátí 60;<br/>
        /// Pro záporný vstup <paramref name="value"/> = -100 a <paramref name="cycle"/> = 360 vrátí 260;<br/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cycle"></param>
        /// <returns></returns>
        protected static double Cycle(double value, double cycle)
        {
            value = value % cycle;
            while (value < 0d)
                value += cycle;
            return value;
        }
    }
    #endregion
}