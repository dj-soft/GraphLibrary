using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Djs.Common.TextParser
{
    #region class RtfCoder : třída pro sestavování RTF textu z jednotlivých složek, a následnou kompletaci do standardního RTF kódu
    /// <summary>
    /// RtfCoder : třída pro sestavování RTF textu z jednotlivých složek, a následnou kompletaci do standardního RTF kódu
    /// </summary>
    public class RtfCoder : IFormattedTextAssembler, IDisposable
    {
        #region Konstrukce
        public RtfCoder()
        {
            this.RtfItems = new List<FormatCode>();
            this.ReadOnly = true;
        }
        public RtfCoder(bool readOnly)
        {
            this.RtfItems = new List<FormatCode>();
            this.ReadOnly = readOnly;
        }
        void IDisposable.Dispose()
        {
            this.RtfItems = null;
        }
        #endregion
        #region IFormattedTextAssembler implementace
        void IFormattedTextAssembler.Clear() { this.Clear(); }
        void IFormattedTextAssembler.Add(FormatCode item) { this.Add(item); }
        void IFormattedTextAssembler.AddRange(IEnumerable<FormatCode> items) { this.AddRange(items); }
        string IFormattedTextAssembler.Text { get { return this.RtfText; } }
        #endregion
        #region Public property
        /// <summary>
        /// Standardní RTF hlavička
        /// </summary>
        public static string Header
        {
            get
            {
                return @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset238{\*\fname Courier New;}Courier New CE;}}
{\*\generator Msftedit 5.41.15.1515;}\viewkind4\uc1\pard\lang1029\f0\fs20 ";
            }
        }
        /// <summary>
        /// Seznam RTF položek
        /// </summary>
        public List<FormatCode> RtfItems { get; private set; }
        /// <summary>
        /// Příznak, zda RTF text má být read-only
        /// </summary>
        public bool ReadOnly { get; set; }
        /// <summary>
        /// Obsahuje vždy platný aktuální RTF text
        /// </summary>
        public string RtfText
        {
            get { return this._CreateRtfText(); }
        }
        #endregion
        #region Přidávání a mazání položek RTF textu
        /// <summary>
        /// Do RTF dokumentu přidá další segmenty.
        /// Segmenty se vytvářejí pomocí statických konstruktorů třídy RtfItem, typicky: .Add(RtfItem.NewText("text do RTF dokumentu."))
        /// </summary>
		/// <param name="items"></param>
        public void Add(params FormatCode[] items)
        {
            if (items != null)
                this.AddRange((IEnumerable<FormatCode>)items);
        }
        /// <summary>
        /// Do RTF dokumentu přidá další segmenty.
        /// Segmenty se vytvářejí pomocí statických konstruktorů třídy RtfItem, typicky: .Add(RtfItem.NewText("text do RTF dokumentu."))
        /// </summary>
		/// <param name="items"></param>
        public void AddRange(IEnumerable<FormatCode> items)
        {
            if (items != null)
            {
                foreach (FormatCode item in items)
                {
                    if (!item.IsEmpty)
                        this.RtfItems.Add(item);
                }
            }
        }
        /// <summary>
        /// Vymaže svůj obsah
        /// </summary>
        public void Clear()
        {
            this.RtfItems.Clear();
        }
        #endregion
        #region Kompletace RTF textu
        /// <summary>
        /// Zkompletuje RTF text
        /// </summary>
        /// <returns></returns>
        private string _CreateRtfText()
        {
            // Nejdřív musím projít položky RtfItems, protože tím se mi vytvoří tabulka fontů a tabulka barev, kteréžto patří do záhlaví před vlastní text:
            StringBuilder txt = new StringBuilder();

            List<RtfFont> fontTable = new List<RtfFont>();
            fontTable.Add(new RtfFont(0, 238, "nil", "Courier New", "Courier New CE"));

            List<RtfColor> colorTable = new List<RtfColor>();
            colorTable.Add(new RtfColor(0, Color.Empty));            // Takhle je zajištěna default barva: v RTF kódu má číslo "0", a do color tabulky se explicitně nevepisuje.

            Dictionary<int, string> codeTable = _CreateRtfCodeTable();
            bool inCode = true;
            foreach (FormatCode item in this.RtfItems)
            {
                string fragment = null;
                switch (item.ItemType)
                {
                    case FormatItemType.None:
                        break;
                    case FormatItemType.Text:
                        fragment = _ItemCreateRtfText(item, ref inCode, codeTable);
                        break;
                    case FormatItemType.Code:
                        fragment = _ItemCreateRtfCode(item, ref inCode);
                        break;
                    case FormatItemType.FontName:
                        fragment = _ItemCreateRtfFont(item, ref inCode, fontTable);
                        break;
                    case FormatItemType.FontSize:
                        fragment = _ItemCreateRtfSize(item, ref inCode);
                        break;
                    case FormatItemType.FontStyle:
                        fragment = _ItemCreateRtfStyle(item, ref inCode);
                        break;
                    case FormatItemType.ForeColor:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.BackColor:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.Highlight:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.ProtectState:
                        break;
                    case FormatItemType.ParagraphAlignment:
                        fragment = _ItemCreateRtfAlign(item, ref inCode);
                        break;
                }
                if (fragment != null)
                    txt.Append(fragment);
            }

            // Kompletace RTF: hlavička
            StringBuilder rtf = new StringBuilder();
            rtf.Append(@"{\rtf1\ansi\ansicpg1250\deff0");

            // fonty
            if (fontTable.Count > 0)
            {
                rtf.Append(@"{\fonttbl");
                foreach (RtfFont font in fontTable)
                    rtf.Append(font.RtfTable);
                rtf.Append(@"}");
            }
            // barvy
            if (colorTable.Count > 1)
            {
                rtf.AppendLine();
                rtf.Append(@"{\colortbl ");
                foreach (RtfColor color in colorTable)
                    rtf.Append(color.RtfTable);
                rtf.Append(@"}");
            }
            // záhlaví před prvním textem:
            rtf.AppendLine();
            rtf.Append(@"{\*\generator Msftedit 5.41.21.2510;}\viewkind4" + (this.ReadOnly ? @"\allprot" : "") + @"\uc1\pard\lang1029\f0\fs20");

            // text:
            rtf.Append(txt.ToString());

            // zápatí:
            rtf.AppendLine("\\par");
            rtf.AppendLine("}");
            rtf.Append(" ");

            return rtf.ToString();
        }
        private Dictionary<int, string> _CreateRtfCodeTable()
        {
            Dictionary<int, string> codeTable = new Dictionary<int, string>();

            codeTable.Add(09, @"\tab");           // TAB
            codeTable.Add(10, "");                // LF = nic, ale musím zajistit že ve vstupním textu bude vždy CR (viz metoda ConvertCrLf())
            codeTable.Add(13, @"\par" + "\r\n");  // CR = text "\par" + CrLf (vizuální oddělení textu)
            codeTable.Add(92, @"\\");             // Jedno zpětné lomítko => nahradit dvěma
            codeTable.Add((int)'{', @"\{");
            codeTable.Add((int)'}', @"\}");

            return codeTable;
        }
        private string _ItemCreateRtfText(FormatCode item, ref bool inCode, Dictionary<int, string> codeTable)
        {
            StringBuilder sb = new StringBuilder();
            if (item.Text != null)
            {
                string text = ConvertCrLf(item.Text);       // Zajistí korektní přítomnost CR
                foreach (char c in text)
                {
                    int i = (int)c;
                    string code;
                    // Znaky 128 a vyšší:
                    if (i >= 128 && !codeTable.ContainsKey(i))
                    {
                        if (i <= 255)
                        {   // Unicode znaku se vejde do jednoho byte:
                            code = @"\'" + i.ToString("X2").ToLower();
                        }
                        else
                        {   // Unicode je větší než 1 byte, vypíšu jej jinak:
                            code = @"\u" + i.ToString() + "?";
                        }

                        //Encoding ec = Encoding.GetEncoding(852);
                        //byte[] dc = ec.GetBytes(new char[] { c });

                        //Encoding ec2 = Encoding.GetEncoding(1250);
                        //byte[] dc2 = ec2.GetBytes(new char[] { c });


                        //code = @"\'";
                        //int h = dc[0];
                        //if (dc.Length == 1)
                        //    code += (dc[0]).ToString("X2");
                        //else if (dc.Length == 2)
                        //    code += (dc[0] + dc[1] * 0x100).ToString("X4");

                        codeTable.Add(i, code);
                    }

                    // Pokud znak je obsažen v codeTable, vypíšu jej jako kód:
                    if (codeTable.TryGetValue(i, out code))
                    {
                        sb.Append(code);
                        inCode = (i < 32 && i != 13);    // Řídící kód je to jen pro znaky menší než 128. Znaky s vyšším ASCII kódem jsou písmena a za nimi se mezera nepřidává...  A taky ne za \par (tam je fyzicky CrLf)
                    }
                    else
                    {
                        if (inCode)
                        {	// Po kódu, před textem se vkládá mezera:
                            sb.Append(" ");
                            inCode = false;
                        }
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Metoda upraví vstupující text tak, aby obsahoval pouze znaky CR.
        /// Namísto znaků CrLf dá Cr, a pak namísto Lf dá Cr.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ConvertCrLf(string text)
        {
            if (text == null) return null;
            string result = text.Replace("\r\n", "\r").Replace("\n", "\r");
            return result;
        }
        private string _ItemCreateRtfCode(FormatCode item, ref bool inCode)
        {
            inCode = true;
            return item.Code;
        }
        private string _ItemCreateRtfFont(FormatCode item, ref bool inCode, List<RtfFont> fontTable)
        {
            RtfFont font = fontTable.FirstOrDefault(ft => ft.FontName == item.FontName);
            if (font == null)
            {
                font = new RtfFont(fontTable.Count, 238, "swiss", item.FontName, item.FontName);
                fontTable.Add(font);
            }
            inCode = true;
            return font.RtfMark;
        }
        private string _ItemCreateRtfSize(FormatCode item, ref bool inCode)
        {
            inCode = true;
            return @"\fs" + (2 * item.FontSize).ToString();
        }
        private string _ItemCreateRtfStyle(FormatCode item, ref bool inCode)
        {
            string result = "";
            if ((item.FontStyle & FormatFontStyle.Bold) > 0)
                result += @"\b";
            if ((item.FontStyle & FormatFontStyle.BoldEnd) > 0)
                result += @"\b0";
            if ((item.FontStyle & FormatFontStyle.Italic) > 0)
                result += @"\i";
            if ((item.FontStyle & FormatFontStyle.ItalicEnd) > 0)
                result += @"\i0";
            if ((item.FontStyle & FormatFontStyle.Underline) > 0)
                result += @"\ul";
            if ((item.FontStyle & FormatFontStyle.UnderlineEnd) > 0)
                result += @"\ulnone";
            if ((item.FontStyle & FormatFontStyle.StrikeOut) > 0)
                result += @"\strike";
            if ((item.FontStyle & FormatFontStyle.StrikeOutEnd) > 0)
                result += @"\strike0";
            if (result.Length > 0)
                inCode = true;
            return result;
        }
        private string _ItemCreateRtfColor(FormatCode item, ref bool inCode, List<RtfColor> colorTable)
        {
            RtfColor rtfColor = colorTable.FirstOrDefault(rc => rc.ColorValue.Equals(item.Color));   // Najdu položku RtfColor odpovídající zadané barvě
            if (rtfColor == null)
            {
                rtfColor = new RtfColor(colorTable.Count, item.Color);
                colorTable.Add(rtfColor);
            }
            switch (item.ItemType)
            {
                case FormatItemType.ForeColor:
                    inCode = true;
                    return rtfColor.RtfMarkF;
                case FormatItemType.BackColor:
                    inCode = true;
                    return rtfColor.RtfMarkB;
                case FormatItemType.Highlight:
                    inCode = true;
                    return rtfColor.RtfMarkH;
            }
            return "";
        }
        private string _ItemCreateRtfAlign(FormatCode item, ref bool inCode)
        {
            switch (item.Alignment)
            {
                case FormatAlignment.Left:
                    inCode = true;
                    return @"";
                case FormatAlignment.Center:
                    inCode = true;
                    return @"\qc";
                case FormatAlignment.Right:
                    inCode = true;
                    return @"\qr";
                case FormatAlignment.Justify:
                    inCode = true;
                    return @"\qj";
            }
            return @"";
        }
        #endregion
    }
    #endregion
    #region classes RtfFont, RtfColor
    /// <summary>
    /// Úložiště jednoho fontu v tabulce fontů RTF
    /// </summary>
    internal class RtfFont
    {
        public RtfFont(int index, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = "238";
            this.FontType = "nil";
            this.FontFamily = name;
            this.FontName = name;
        }
        public RtfFont(int index, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = "238";
            this.FontType = "nil";
            this.FontFamily = family;
            this.FontName = name;
        }
        public RtfFont(int index, int charSet, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = charSet.ToString();
            this.FontType = "nil";
            this.FontFamily = family;
            this.FontName = name;
        }
        public RtfFont(int index, int charSet, string type, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = charSet.ToString();
            this.FontType = type;
            this.FontFamily = family;
            this.FontName = name;
        }
        public string FontCode { get; private set; }
        public string FontCharset { get; private set; }
        public string FontType { get; private set; }
        public string FontFamily { get; private set; }
        public string FontName { get; private set; }
        /// <summary>
        /// Značka fontu do flow textu: \f3   
        /// </summary>
        public string RtfMark { get { return @"\f" + this.FontCode; } }
        /// <summary>
        /// Plný text fontu do tabulky fontů: {\f3\fnil\fcharset238{\*\fname Arial;}Arial CE;}
        /// </summary>
        public string RtfTable
        {
            get
            {
                return @"{\f" + this.FontCode + @"\f" + this.FontType + @"\fcharset" + this.FontCharset + @"{\*\fname " + this.FontFamily + @";}" + this.FontName + ";}";
            }
        }
    }
    /// <summary>
    /// Úložiště jedné barvy v paletě barev
    /// </summary>
    internal class RtfColor
    {
        public RtfColor(int index, Color color)
        {
            this.ColorCode = index.ToString();
            this.ColorValue = color;
        }
        /// <summary>
        /// Obsahuje číslenou část kódu barvy = text "0" až "199"
        /// </summary>
        public string ColorCode { get; private set; }
        /// <summary>
        /// Obsahuje barvu. Pokud je IsEmpty, jde o default barvu která se do tabulky nevypisuje (její hodnoty), ale jen se z aní vkládá středník.
        /// </summary>
        public Color ColorValue { get; private set; }
        /// <summary>
        /// Značka fontu do flow textu: \cf1   (pro barvu ForeColor)
        /// </summary>
        public string RtfMarkF { get { return @"\cf" + this.ColorCode; } }
        /// <summary>
        /// Značka fontu do flow textu: \cb1   (pro barvu BackColor)
        /// </summary>
        public string RtfMarkB { get { return @"\cb" + this.ColorCode; } }
        /// <summary>
        /// Značka fontu do flow textu: \highlight1   (pro barvu Highlight = BackColor)
        /// </summary>
        public string RtfMarkH { get { return @"\highlight" + this.ColorCode; } }
        /// <summary>
        /// Text barvy v RTF kódu: "red0\green128\blue64;"
        /// </summary>
        public string RtfTable
        {
            get
            {
                if (this.ColorValue.IsEmpty)
                    return ";";
                return "\\red" + this.ColorValue.R.ToString() + "\\green" + this.ColorValue.G.ToString() + "\\blue" + this.ColorValue.B.ToString() + ";";
            }
        }
    }
    #endregion
}
