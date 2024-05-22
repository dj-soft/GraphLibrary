// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.
using DevExpress.XtraRichEdit.Import.Doc;
using DevExpress.XtraTreeList;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestDevExpress.AsolDX.News
{
    public class FontSizes
    {
        public static void CreateTable()
        {
            //   Jak určit velikost textu v situaci, kdy nemám Drawing ani Graphics, a chci to vcelku rychle:
            // - Použiju zdejší tabulku (viz dále)
            // - Vstupní text rozčlením na první písmeno, poté na dvojice: první+druhé, druhé+třetí, třetí+...+poslední, a pak sólo poslední písmeno
            // - Pro každý text najdu jeho velikost v tabulce, velikost reprezentuje vzdálenost mezi středy prvního a druhého písmena ve dvojici, anebo půlvelikost jednoho znaku
            // - Sečtu je.
            //   Příklad pro text: "ATdb\"
            // "A"  .. najdu velikost půlky textu = reprezentuje levou polovinu znaku
            // "AT" .. najdu vzdálenost středu = od poloviny prvního do poloviny druhého
            // "Td" ..  dtto
            // "db" ..  dtto
            // "b\" ..  dtto
            // "\"  .. velikost druhé půlky znaku
            //   Sečtu hodnoty a je hotovo.
            //   Když nenajdu kombinaci v tabulce, tak vyhledám jednotlivé písmeno první a druhé, a sečtu jejich půlvelikosti.

            //   Jak vypočítám tabulku:
            //  1. Vezmu jednotlivé znaky char(10) až char(266) a změřím šířku, uložím polovinu
            //  2. Sestavím kombinace dvojznaků, změřím velikost, odečtu půlvelikost jednotlivého znaku vlevo a vpravo, zbytek je velikost uprostřed, uložím
            //  Vznikne tedy tabulka: 0:znak pro jednotlivé, a znakA:znakB pro dvojznaky

            var textToBasicCode = _GenerateMethod_TextToBasic();
            var basicCharString = _CreateBasicCharString();
            var sizeTable = _CreateTextSizeTable(basicCharString);
            var sizeCode = _GenerateTextToSizeCode(sizeTable, basicCharString);




        }




        #region Generátory metod

        /// <summary>
        /// Vygeneruje základní kód metody _TextToBasic
        /// </summary>
        /// <returns></returns>
        private static string _GenerateMethod_TextToBasic()
        {
            StringBuilder code = new StringBuilder();

            // Vygeneruji kód, který ze vstupního textu (s libovolnou diakritikou) sestaví výstupní text bez diakritiky:
            code.Clear();
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Metoda vrátí vstupní text, v němž budou diakritické znaky a znaky neběžné nahrazeny běžnými.<br/>");
            code.AppendLine("        /// Pokud tedy na vstupu je: <c>\"Černé Poříčí 519/a\"</c>,<br/>");
            code.AppendLine("        /// pak na výstupu bude: <c>\"Cerne Porici 519/a\"</c>.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"text\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static string _TextToBasic(string text)");
            code.AppendLine("        {");
            code.AppendLine("            if (text == null || text.Length == 0) return text;");
            code.AppendLine("            ");
            code.AppendLine("            StringBuilder result = new StringBuilder();");
            code.AppendLine("            foreach (char c in text)");
            code.AppendLine("                result.Append(getBasic(c));");
            code.AppendLine("            return result.ToString();");
            code.AppendLine("            ");
            code.AppendLine("            char getBasic(char i)");
            code.AppendLine("            {");
            code.AppendLine("                switch ((int)i)");
            code.AppendLine("                {");

            for (int c = CharCode1; c < CharCode2; c++)
            {
                char c2 = (char)c;
                string t2 = c2.ToString();
                code.AppendLine($"                    case {c}: return '{t2}';         // '{t2}'");
            }

            code.AppendLine("                }");
            code.AppendLine("                return 'O';                      // Náhradní");
            code.AppendLine("            }");
            code.AppendLine("        }");

            return code.ToString();
        }

        /// <summary>
        /// Vrátí string (ne kód) obsahující basic znaky = tedy ty, které je třeba změřit
        /// </summary>
        /// <returns></returns>
        private static string _CreateBasicCharString()
        {
            var chars = new Dictionary<int, string>();
            for (int c = CharCode1; c <= CharCode2; c++)
            {
                string inpText = ((char)c).ToString();
                string outText = _TextToBasic(inpText);
                int outCode = (int)outText[0];
                if (!chars.ContainsKey(outCode))
                    chars.Add(outCode, outText);
            }
            var charList = chars.ToList();
            charList.Sort((a, b) => a.Key.CompareTo(b.Key));

            StringBuilder sb = new StringBuilder();
            foreach (var kv in charList)
                sb.Append(kv.Value);

            return sb.ToString();

        }
        /// <summary>
        /// Metoda provede reálné měření rozměru stringu: jedno- a dvou-znakových, a vepíše je do výstupní Dictionary.
        /// </summary>
        /// <param name="basicCharString"></param>
        /// <returns></returns>
        private static Dictionary<string, Info> _CreateTextSizeTable(string basicCharString)
        {
            var values = new Dictionary<string, Info>();

            var charArray = basicCharString.ToCharArray().Select(c => c.ToString()).ToArray();          // string[] vytvořené pro jednotlivé znaky z 'basicCharString'

            using (Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(144f, 144f);
                var family = SystemFonts.DefaultFont.FontFamily;
                float emSize = FontEmSize;
                using (Font fontR = new Font(family, emSize, FontStyle.Regular))
                using (Font fontB = new Font(family, emSize, FontStyle.Bold))
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    createNoneHeight(graphics, fontR, fontB);
                    createSingleChars(graphics, fontR, fontB);
                    createDoubleChars(graphics, fontR, fontB);
                }
            }

            return values;

            void createNoneHeight(Graphics gr, Font fR, Font fB)
            {
                string textHeight = "height";
                var sizeR = gr.MeasureString(textHeight, fR);
                var sizeB = gr.MeasureString(textHeight, fB);
                values.Add("", new Info() { Text = "", SizeR = sizeR, SizeB = sizeB });
            }
            void createSingleChars(Graphics gr, Font fR, Font fB)
            {
                foreach (var textSingle in charArray)
                {
                    var sizeR = gr.MeasureString(textSingle, fR);
                    var sizeB = gr.MeasureString(textSingle, fB);
                    values.Add(textSingle, new Info() { Text = textSingle, SizeR = sizeR, SizeB = sizeB });
                }
            }

            void createDoubleChars(Graphics gr, Font fR, Font fB)
            {
                foreach (var textDoubleA in charArray)
                    foreach (var textDoubleB in charArray)
                    {
                        var textDouble = textDoubleA + textDoubleB;
                        var sizeR = gr.MeasureString(textDouble, fR);
                        var sizeB = gr.MeasureString(textDouble, fB);
                        values.Add(textDouble, new Info() { Text = textDouble, SizeR = sizeR, SizeB = sizeB });
                    }
            }
        }
        /// <summary>
        /// Metoda vrátí kód metody, která vrací rozměr daného znaku: délky 0, 1 a 2 znaky.
        /// </summary>
        /// <param name="sizeTable"></param>
        /// <param name="basicCharString"></param>
        /// <param name="bold"></param>
        /// <returns></returns>
        private static string _GenerateTextToSizeCode(Dictionary<string, Info> sizeTable, string basicCharString)
        {
            StringBuilder code = new StringBuilder();



            code.AppendLine("");
            code.AppendLine("");

            // 1. Určím výšku znaku:
            float charHeight = sizeTable[""].SizeR.Height;

            // 2. Sestavím data pro šířku jednotlivého znaku:
            foreach (char cSingle in basicCharString)
            {
                string key = cSingle.ToString();
                var info = sizeTable[key];
                string data0 = _GetDataForWidths(info.SizeR.Width, info.SizeB.Width);
            }


            // String to Code:
            string basicCharStringCode = basicCharString
                .Replace("\"", "\\\"")
                .Replace("\\", "\\\\");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí kód dodaného znaku, anebo kód náhradního (existujícího) znaku");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"c\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static int _GetCharCode(char c)");
            code.AppendLine("        {");
            code.AppendLine("            var list = _TextCharList;");
            code.AppendLine("            int code = list.IndexOf(c);");
            code.AppendLine("            if (code == -1) code = list.IndexOf('D');");
            code.AppendLine("            return code;v        }");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Obsahuje všechny řešené Basic znaky, na odpovídajících pozicích");
            code.AppendLine("        /// </summary>");
            code.AppendLine($"        private const string _TextCharList = \"{basicCharStringCode}\";");

            /*



            */
            return code.ToString();
        }

        private class Info
        {
            public override string ToString()
            {
                return $"'{Text}': R='{SizeR}';  B='{SizeB}'";
            }
            public string Text;
            public SizeF SizeR;
            public SizeF SizeB;
        }

        private const int CharCode1 = 32;
        private const int CharCode2 = 565;
        private const float FontEmSize = 20f;

        #endregion

        #region Trvalé metody
        /// <summary>
        /// Metoda vrátí vstupní text, v němž budou diakritické znaky a znaky neběžné nahrazeny běžnými.<br/>
        /// Pokud tedy na vstupu je: <c>"Černé Poříčí 519/a"</c>,<br/>
        /// pak na výstupu bude: <c>"Cerne Porici 519/a"</c>.
        /// </summary>
        /// <param name="text">Vstupní text</param>
        /// <returns></returns>
        private static string _TextToBasic(string text)
        {
            if (text == null || text.Length == 0) return text;

            StringBuilder result = new StringBuilder();
            foreach (char c in text)
                result.Append(getBasic(c));
            return result.ToString();

            char getBasic(char i)
            {
                switch ((int)i)
                {
                    case 32: return ' ';         // ' '
                    case 33: return '!';         // '!'
                    case 34: return '"';         // '"'
                    case 35: return '#';         // '#'
                    case 36: return '$';         // '$'
                    case 37: return '%';         // '%'
                    case 38: return '&';         // '&'
                    case 39: return '\'';        // '''
                    case 40: return '(';         // '('
                    case 41: return ')';         // ')'
                    case 42: return '*';         // '*'
                    case 43: return '+';         // '+'
                    case 44: return ',';         // ','
                    case 45: return '-';         // '-'
                    case 46: return '.';         // '.'
                    case 47: return '/';         // '/'
                    case 48: return '0';         // '0'
                    case 49: return '1';         // '1'
                    case 50: return '2';         // '2'
                    case 51: return '3';         // '3'
                    case 52: return '4';         // '4'
                    case 53: return '5';         // '5'
                    case 54: return '6';         // '6'
                    case 55: return '7';         // '7'
                    case 56: return '8';         // '8'
                    case 57: return '9';         // '9'
                    case 58: return ':';         // ':'
                    case 59: return ';';         // ';'
                    case 60: return '<';         // '<'
                    case 61: return '=';         // '='
                    case 62: return '>';         // '>'
                    case 63: return '?';         // '?'
                    case 64: return '@';         // '@'
                    case 65: return 'A';         // 'A'
                    case 66: return 'B';         // 'B'
                    case 67: return 'C';         // 'C'
                    case 68: return 'D';         // 'D'
                    case 69: return 'E';         // 'E'
                    case 70: return 'F';         // 'F'
                    case 71: return 'G';         // 'G'
                    case 72: return 'H';         // 'H'
                    case 73: return 'I';         // 'I'
                    case 74: return 'J';         // 'J'
                    case 75: return 'K';         // 'K'
                    case 76: return 'L';         // 'L'
                    case 77: return 'M';         // 'M'
                    case 78: return 'N';         // 'N'
                    case 79: return 'O';         // 'O'
                    case 80: return 'P';         // 'P'
                    case 81: return 'Q';         // 'Q'
                    case 82: return 'R';         // 'R'
                    case 83: return 'S';         // 'S'
                    case 84: return 'T';         // 'T'
                    case 85: return 'U';         // 'U'
                    case 86: return 'V';         // 'V'
                    case 87: return 'W';         // 'W'
                    case 88: return 'X';         // 'X'
                    case 89: return 'Y';         // 'Y'
                    case 90: return 'Z';         // 'Z'
                    case 91: return '[';         // '['
                    case 92: return '\\';        // '\'
                    case 93: return ']';         // ']'
                    case 94: return '^';         // '^'
                    case 95: return '_';         // '_'
                    case 96: return '\'';        // '`'
                    case 97: return 'a';         // 'a'
                    case 98: return 'b';         // 'b'
                    case 99: return 'c';         // 'c'
                    case 100: return 'd';         // 'd'
                    case 101: return 'e';         // 'e'
                    case 102: return 'f';         // 'f'
                    case 103: return 'g';         // 'g'
                    case 104: return 'h';         // 'h'
                    case 105: return 'i';         // 'i'
                    case 106: return 'j';         // 'j'
                    case 107: return 'k';         // 'k'
                    case 108: return 'l';         // 'l'
                    case 109: return 'm';         // 'm'
                    case 110: return 'n';         // 'n'
                    case 111: return 'o';         // 'o'
                    case 112: return 'p';         // 'p'
                    case 113: return 'q';         // 'q'
                    case 114: return 'r';         // 'r'
                    case 115: return 's';         // 's'
                    case 116: return 't';         // 't'
                    case 117: return 'u';         // 'u'
                    case 118: return 'v';         // 'v'
                    case 119: return 'w';         // 'w'
                    case 120: return 'x';         // 'x'
                    case 121: return 'y';         // 'y'
                    case 122: return 'z';         // 'z'
                    case 123: return '{';         // '{'
                    case 124: return '|';         // '|'
                    case 125: return '}';         // '}'
                    case 126: return '~';         // '~'
                    case 160: return ' ';         // ' '
                    case 161: return '!';         // '¡'
                    case 162: return 'c';         // '¢'
                    case 163: return 'L';         // '£'
                    case 164: return 'x';         // '¤'
                    case 165: return 'Y';         // '¥'
                    case 166: return '|';         // '¦'
                    case 167: return '§';         // '§'
                    case 168: return '¨';         // '¨'
                    case 169: return 'O';         // '©'
                    case 170: return '¨';         // 'ª'
                    case 171: return '«';         // '«'
                    case 172: return '_';         // '¬'
                    case 173: return ' ';         // '­'
                    case 174: return 'O';         // '®'
                    case 175: return '_';         // '¯'
                    case 176: return 'o';         // '°'
                    case 177: return '+';         // '±'
                    case 178: return '¨';         // '²'
                    case 179: return '¨';         // '³'
                    case 180: return '\'';        // '´'
                    case 181: return 'u';         // 'µ'
                    case 182: return 'T';         // '¶'
                    case 183: return '.';         // '·'
                    case 184: return ',';         // '¸'
                    case 185: return '¨';         // '¹'
                    case 186: return 'o';         // 'º'
                    case 187: return '»';         // '»'
                    case 188: return 'X';         // '¼'
                    case 189: return 'X';         // '½'
                    case 190: return 'X';         // '¾'
                    case 191: return '?';         // '¿'
                    case 192: return 'A';         // 'À'
                    case 193: return 'A';         // 'Á'
                    case 194: return 'A';         // 'Â'
                    case 195: return 'A';         // 'Ã'
                    case 196: return 'A';         // 'Ä'
                    case 197: return 'A';         // 'Å'
                    case 198: return 'A';         // 'Æ'
                    case 199: return 'C';         // 'Ç'
                    case 200: return 'E';         // 'È'
                    case 201: return 'E';         // 'É'
                    case 202: return 'E';         // 'Ê'
                    case 203: return 'E';         // 'Ë'
                    case 204: return 'I';         // 'Ì'
                    case 205: return 'I';         // 'Í'
                    case 206: return 'I';         // 'Î'
                    case 207: return 'I';         // 'Ï'
                    case 208: return 'D';         // 'Ð'
                    case 209: return 'N';         // 'Ñ'
                    case 210: return 'O';         // 'Ò'
                    case 211: return 'O';         // 'Ó'
                    case 212: return 'O';         // 'Ô'
                    case 213: return 'O';         // 'Õ'
                    case 214: return 'O';         // 'Ö'
                    case 215: return '×';         // '×'
                    case 216: return 'O';         // 'Ø'
                    case 217: return 'U';         // 'Ù'
                    case 218: return 'U';         // 'Ú'
                    case 219: return 'U';         // 'Û'
                    case 220: return 'U';         // 'Ü'
                    case 221: return 'Y';         // 'Ý'
                    case 222: return 'P';         // 'Þ'
                    case 223: return 'B';         // 'ß'
                    case 224: return 'a';         // 'à'
                    case 225: return 'a';         // 'á'
                    case 226: return 'a';         // 'â'
                    case 227: return 'a';         // 'ã'
                    case 228: return 'a';         // 'ä'
                    case 229: return 'a';         // 'å'
                    case 230: return 'a';         // 'æ'
                    case 231: return 'c';         // 'ç'
                    case 232: return 'e';         // 'è'
                    case 233: return 'e';         // 'é'
                    case 234: return 'e';         // 'ê'
                    case 235: return 'e';         // 'ë'
                    case 236: return 'i';         // 'ì'
                    case 237: return 'i';         // 'í'
                    case 238: return 'i';         // 'î'
                    case 239: return 'i';         // 'ï'
                    case 240: return 'o';         // 'ð'
                    case 241: return 'n';         // 'ñ'
                    case 242: return 'o';         // 'ò'
                    case 243: return 'o';         // 'ó'
                    case 244: return 'o';         // 'ô'
                    case 245: return 'o';         // 'õ'
                    case 246: return 'o';         // 'ö'
                    case 247: return '-';         // '÷'
                    case 248: return 'o';         // 'ø'
                    case 249: return 'u';         // 'ù'
                    case 250: return 'u';         // 'ú'
                    case 251: return 'u';         // 'û'
                    case 252: return 'u';         // 'ü'
                    case 253: return 'y';         // 'ý'
                    case 254: return 'b';         // 'þ'
                    case 255: return 'y';         // 'ÿ'
                    case 256: return 'A';         // 'Ā'
                    case 257: return 'a';         // 'ā'
                    case 258: return 'A';         // 'Ă'
                    case 259: return 'a';         // 'ă'
                    case 260: return 'A';         // 'Ą'
                    case 261: return 'a';         // 'ą'
                    case 262: return 'C';         // 'Ć'
                    case 263: return 'c';         // 'ć'
                    case 264: return 'C';         // 'Ĉ'
                    case 265: return 'c';         // 'ĉ'
                    case 266: return 'C';         // 'Ċ'
                    case 267: return 'c';         // 'ċ'
                    case 268: return 'C';         // 'Č'
                    case 269: return 'c';         // 'č'
                    case 270: return 'D';         // 'Ď'
                    case 271: return 'd';         // 'ď'
                    case 272: return 'D';         // 'Đ'
                    case 273: return 'd';         // 'đ'
                    case 274: return 'E';         // 'Ē'
                    case 275: return 'e';         // 'ē'
                    case 276: return 'E';         // 'Ĕ'
                    case 277: return 'e';         // 'ĕ'
                    case 278: return 'E';         // 'Ė'
                    case 279: return 'e';         // 'ė'
                    case 280: return 'E';         // 'Ę'
                    case 281: return 'e';         // 'ę'
                    case 282: return 'E';         // 'Ě'
                    case 283: return 'e';         // 'ě'
                    case 284: return 'G';         // 'Ĝ'
                    case 285: return 'g';         // 'ĝ'
                    case 286: return 'G';         // 'Ğ'
                    case 287: return 'g';         // 'ğ'
                    case 288: return 'G';         // 'Ġ'
                    case 289: return 'g';         // 'ġ'
                    case 290: return 'G';         // 'Ģ'
                    case 291: return 'g';         // 'ģ'
                    case 292: return 'H';         // 'Ĥ'
                    case 293: return 'h';         // 'ĥ'
                    case 294: return 'H';         // 'Ħ'
                    case 295: return 'h';         // 'ħ'
                    case 296: return 'I';         // 'Ĩ'
                    case 297: return 'i';         // 'ĩ'
                    case 298: return 'I';         // 'Ī'
                    case 299: return 'i';         // 'ī'
                    case 300: return 'I';         // 'Ĭ'
                    case 301: return 'i';         // 'ĭ'
                    case 302: return 'I';         // 'Į'
                    case 303: return 'i';         // 'į'
                    case 304: return 'I';         // 'İ'
                    case 305: return 'i';         // 'ı'
                    case 306: return 'Ĳ';         // 'Ĳ'
                    case 307: return 'u';         // 'ĳ'
                    case 308: return 'J';         // 'Ĵ'
                    case 309: return 'j';         // 'ĵ'
                    case 310: return 'K';         // 'Ķ'
                    case 311: return 'k';         // 'ķ'
                    case 312: return 'k';         // 'ĸ'
                    case 313: return 'L';         // 'Ĺ'
                    case 314: return 'l';         // 'ĺ'
                    case 315: return 'L';         // 'Ļ'
                    case 316: return 'l';         // 'ļ'
                    case 317: return 'L';         // 'Ľ'
                    case 318: return 'l';         // 'ľ'
                    case 319: return 'L';         // 'Ŀ'
                    case 320: return 'l';         // 'ŀ'
                    case 321: return 'L';         // 'Ł'
                    case 322: return 'l';         // 'ł'
                    case 323: return 'N';         // 'Ń'
                    case 324: return 'n';         // 'ń'
                    case 325: return 'N';         // 'Ņ'
                    case 326: return 'n';         // 'ņ'
                    case 327: return 'N';         // 'Ň'
                    case 328: return 'n';         // 'ň'
                    case 329: return 'h';         // 'ŉ'
                    case 330: return 'N';         // 'Ŋ'
                    case 331: return 'n';         // 'ŋ'
                    case 332: return 'O';         // 'Ō'
                    case 333: return 'o';         // 'ō'
                    case 334: return 'O';         // 'Ŏ'
                    case 335: return 'o';         // 'ŏ'
                    case 336: return 'O';         // 'Ő'
                    case 337: return 'o';         // 'ő'
                    case 338: return 'E';         // 'Œ'
                    case 339: return 'e';         // 'œ'
                    case 340: return 'R';         // 'Ŕ'
                    case 341: return 'r';         // 'ŕ'
                    case 342: return 'R';         // 'Ŗ'
                    case 343: return 'r';         // 'ŗ'
                    case 344: return 'R';         // 'Ř'
                    case 345: return 'r';         // 'ř'
                    case 346: return 'S';         // 'Ś'
                    case 347: return 's';         // 'ś'
                    case 348: return 'S';         // 'Ŝ'
                    case 349: return 's';         // 'ŝ'
                    case 350: return 'S';         // 'Ş'
                    case 351: return 's';         // 'ş'
                    case 352: return 'S';         // 'Š'
                    case 353: return 's';         // 'š'
                    case 354: return 'T';         // 'Ţ'
                    case 355: return 't';         // 'ţ'
                    case 356: return 'T';         // 'Ť'
                    case 357: return 't';         // 'ť'
                    case 358: return 't';         // 'Ŧ'
                    case 359: return 'T';         // 'ŧ'
                    case 360: return 'U';         // 'Ũ'
                    case 361: return 'u';         // 'ũ'
                    case 362: return 'U';         // 'Ū'
                    case 363: return 'u';         // 'ū'
                    case 364: return 'U';         // 'Ŭ'
                    case 365: return 'u';         // 'ŭ'
                    case 366: return 'U';         // 'Ů'
                    case 367: return 'u';         // 'ů'
                    case 368: return 'U';         // 'Ű'
                    case 369: return 'u';         // 'ű'
                    case 370: return 'U';         // 'Ų'
                    case 371: return 'u';         // 'ų'
                    case 372: return 'W';         // 'Ŵ'
                    case 373: return 'w';         // 'ŵ'
                    case 374: return 'Y';         // 'Ŷ'
                    case 375: return 'y';         // 'ŷ'
                    case 376: return 'Y';         // 'Ÿ'
                    case 377: return 'Z';         // 'Ź'
                    case 378: return 'z';         // 'ź'
                    case 379: return 'Z';         // 'Ż'
                    case 380: return 'z';         // 'ż'
                    case 381: return 'Z';         // 'Ž'
                    case 382: return 'z';         // 'ž'
                    case 461: return 'A';         // 'Ǎ'
                    case 462: return 'a';         // 'ǎ'
                    case 463: return 'I';         // 'Ǐ'
                    case 464: return 'i';         // 'ǐ'
                    case 465: return 'O';         // 'Ǒ'
                    case 466: return 'o';         // 'ǒ'
                    case 467: return 'U';         // 'Ǔ'
                    case 468: return 'u';         // 'ǔ'
                    case 469: return 'U';         // 'Ǖ'
                    case 470: return 'u';         // 'ǖ'
                    case 471: return 'U';         // 'Ǘ'
                    case 472: return 'u';         // 'ǘ'
                    case 473: return 'U';         // 'Ǚ'
                    case 474: return 'u';         // 'ǚ'
                    case 475: return 'U';         // 'Ǜ'
                    case 476: return 'u';         // 'ǜ'
                    case 477: return 'e';         // 'ǝ'
                    case 478: return 'A';         // 'Ǟ'
                    case 479: return 'a';         // 'ǟ'
                    case 480: return 'A';         // 'Ǡ'
                    case 481: return 'a';         // 'ǡ'
                    case 482: return 'E';         // 'Ǣ'
                    case 483: return 'e';         // 'ǣ'
                    case 484: return 'G';         // 'Ǥ'
                    case 485: return 'g';         // 'ǥ'
                    case 486: return 'G';         // 'Ǧ'
                    case 487: return 'g';         // 'ǧ'
                    case 488: return 'K';         // 'Ǩ'
                    case 489: return 'k';         // 'ǩ'
                    case 490: return 'O';         // 'Ǫ'
                    case 491: return 'o';         // 'ǫ'
                    case 492: return 'O';         // 'Ǭ'
                    case 493: return 'o';         // 'ǭ'
                    case 494: return 'J';         // 'Ǯ'
                    case 495: return 'j';         // 'ǯ'
                    case 496: return 'j';         // 'ǰ'
                    case 497: return 'D';         // 'Ǳ'
                    case 498: return 'd';         // 'ǲ'
                    case 499: return 'd';         // 'ǳ'
                    case 500: return 'G';         // 'Ǵ'
                    case 501: return 'g';         // 'ǵ'
                    case 502: return 'F';         // 'Ƕ'
                    case 503: return 'f';         // 'Ƿ'
                    case 504: return 'N';         // 'Ǹ'
                    case 505: return 'n';         // 'ǹ'
                    case 506: return 'A';         // 'Ǻ'
                    case 507: return 'a';         // 'ǻ'
                    case 508: return 'A';         // 'Ǽ'
                    case 509: return 'a';         // 'ǽ'
                    case 510: return 'O';         // 'Ǿ'
                    case 511: return 'o';         // 'ǿ'
                    case 512: return 'A';         // 'Ȁ'
                    case 513: return 'a';         // 'ȁ'
                    case 514: return 'A';         // 'Ȃ'
                    case 515: return 'a';         // 'ȃ'
                    case 516: return 'E';         // 'Ȅ'
                    case 517: return 'e';         // 'ȅ'
                    case 518: return 'E';         // 'Ȇ'
                    case 519: return 'e';         // 'ȇ'
                    case 520: return 'I';         // 'Ȉ'
                    case 521: return 'i';         // 'ȉ'
                    case 522: return 'I';         // 'Ȋ'
                    case 523: return 'i';         // 'ȋ'
                    case 524: return 'O';         // 'Ȍ'
                    case 525: return 'o';         // 'ȍ'
                    case 526: return 'O';         // 'Ȏ'
                    case 527: return 'o';         // 'ȏ'
                    case 528: return 'R';         // 'Ȑ'
                    case 529: return 'r';         // 'ȑ'
                    case 530: return 'R';         // 'Ȓ'
                    case 531: return 'r';         // 'ȓ'
                    case 532: return 'U';         // 'Ȕ'
                    case 533: return 'u';         // 'ȕ'
                    case 534: return 'U';         // 'Ȗ'
                    case 535: return 'u';         // 'ȗ'
                    case 536: return 'S';         // 'Ș'
                    case 537: return 's';         // 'ș'
                    case 538: return 'T';         // 'Ț'
                    case 539: return 't';         // 'ț'
                    case 540: return 'X';         // 'Ȝ'
                    case 541: return 'X';         // 'ȝ'
                    case 542: return 'X';         // 'Ȟ'
                    case 543: return 'X';         // 'ȟ'
                    case 544: return 'X';         // 'Ƞ'
                    case 545: return 'X';         // 'ȡ'
                    case 546: return 'X';         // 'Ȣ'
                    case 547: return 'X';         // 'ȣ'
                    case 548: return 'Z';         // 'Ȥ'
                    case 549: return 'z';         // 'ȥ'
                    case 550: return 'A';         // 'Ȧ'
                    case 551: return 'a';         // 'ȧ'
                    case 552: return 'E';         // 'Ȩ'
                    case 553: return 'e';         // 'ȩ'
                    case 554: return 'O';         // 'Ȫ'
                    case 555: return 'o';         // 'ȫ'
                    case 556: return 'O';         // 'Ȭ'
                    case 557: return 'o';         // 'ȭ'
                    case 558: return 'O';         // 'Ȯ'
                    case 559: return 'o';         // 'ȯ'
                    case 560: return 'O';         // 'Ȱ'
                    case 561: return 'o';         // 'ȱ'
                    case 562: return 'Y';         // 'Ȳ'
                    case 563: return 'y';         // 'ȳ'
                }
                return 'O';                      // Náhradní
            }
        }
        /// <summary>
        /// Vrátí výšku textu
        /// </summary>
        /// <param name="sizeRatio"></param>
        /// <returns></returns>
        private static float _GetTextHeight(float? sizeRatio = null)
        {
            float height = 20.6f;

            // Korekce dle úpravy velikosti:
            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)
                height = sizeRatio.Value * height;

            return height;
        }
        /// <summary>
        /// Vypočte a vrátí šířku dodaného textu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isBold"></param>
        /// <param name="sizeRatio"></param>
        /// <returns></returns>
        private static float _GetTextWidth(string text, bool isBold = false, float? sizeRatio = null)
        {
            if (text == null || text.Length == 0) return 0f;
            string basicText = _TextToBasic(text);
            float width = 0f;
            int last = basicText.Length - 1;
            char cPrev = ' ';
            char cCurr;
            for (int i = 0; i <= last; i++)
            {
                cCurr = basicText[i];                                // Znak z Basic textu na aktuální pozici
                if (i == 0)
                {
                    width += _GetCharWidth1(cCurr, isBold);          // První znak: beru levou polovinu jeho plné šířky (levá == pravá)
                }
                else
                {
                    width += _GetCharWidth2(cPrev, cCurr, isBold);   // Každý průběžný znak: beru šířku mezi polovinou znaku Previous a polovinou znaku Current
                    if (i == last)
                        width += _GetCharWidth1(cCurr, isBold);      // Poslední znak: beru pravou polovinu jeho plné šířky (levá == pravá)
                }
                cPrev = cCurr;
            }

            // Korekce dle úpravy velikosti:
            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)
                width = sizeRatio.Value * width;

            return width;
        }
        /// <summary>
        /// Vrátí vzdálenost mezi středem znaku <paramref name="cPrev"/> a středem znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// </summary>
        /// <param name="cPrev"></param>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private static float _GetCharWidth2(char cPrev, char cCurr, bool isBold)
        {
            int code = _GetCharCode(cPrev);
            switch (code)
            {
                case 0: return _GetCharWidth(cCurr, isBold, "AvBbdDfghh..");
            }
            return 0f;
        }
        /// <summary>
        /// Vrátí polovinu šířky daného znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// </summary>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <returns></returns>
        private static float _GetCharWidth1(char cCurr, bool isBold)
        {
            return _GetCharWidth(cCurr, isBold, "AvBbdDfghh..");
        }
        /// <summary>
        /// Vrátí float hodnotu z datového stringu <paramref name="data"/> z pozice odpovídající danému znaku <paramref name="cCurr"/> pro styl písma <paramref name="isBold"/>.
        /// Datový blok obsahuje sekvenci dat: dva znaky pro Regular, dva znaky pro Bold, postupně pro všechny znaky převedené na kódy = odpovídající pozici.
        /// </summary>
        /// <param name="cCurr"></param>
        /// <param name="isBold"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static float _GetCharWidth(char cCurr, bool isBold, string data)
        {
            int code = _GetCharCode(cCurr);                          // Pozice dat pro daný znak v datovém poli = index v jednoznačném soupisu podporovaných znaků '_TextCharList'. Není záporné.
            int index = 4 * code + (isBold ? 2 : 0);                 // Index ukazuje na počátek dat pro daný znak a Bold: data jsou sekvence 4 pozic pro jeden znak (cCurr); vždy 2 znaky Regular + 2 znaky Bold, 
            return ((86f * getVal(index)) + getVal(index + 1)) / 70f;//  v kódování Char(40) až Char(126) v pořadí HL (kde Char(126) již není přítomno), tedy 40 až 125 = 86 hodnot na 1 znak... 2 znaky = 0..7395; děleno 70f = 0 až 105,65, s krokem 0,014 px

            float getVal(int i)
            {
                int val = (int)data[i] - 40;                         // Pokud na dané pozici je znak '(' = Char(40), odpovídá to hodnotě 0
                return (float)val;
            }
        }
        /// <summary>
        /// Vrátí text dlouhý 4 znaky, reprezentující dané šířky Regular <paramref name="widthR"/> a Bold <paramref name="widthB"/>, tak aby byl korektně dekódován v metodě <see cref="_GetCharWidth(char, bool, string)"/>,
        /// </summary>
        /// <param name="widthR"></param>
        /// <param name="widthB"></param>
        /// <returns></returns>
        private static string _GetDataForWidths(float widthR, float widthB)
        {
            split(widthR, out char rh, out char rl);
            split(widthB, out char bh, out char bl);
            return rh.ToString() + rl.ToString() + bh.ToString() + bl.ToString();

            // Konverze Float na dva znaky:
            void split(double width, out char h, out char l)
            {
                if (width <= 0f) { h = '('; l = '('; return; }       // Znak  '('  reprezentuje 0

                double w = 70d * width;                              // Pro zadanou šířku width = 34.975 bude w = 2448.25
                int hi = (int)Math.Truncate(w / 86d);                // Pro vstup w = 2448.25 je h = (2448.25 / 86d = 28.468023   => Truncate  =  28
                int lo = (int)Math.Round((w % 86d), 0);              // Pro vstup w = 2448.25 je l = (2448.25 % 86d = 40.25       => Round     =  40
                h = (char)(hi + 40);                                 // Char(40) = hodnota 0
                l = (char)(lo + 40);
            }
        }
        /// <summary>
        /// Vrátí kód (=index) dodaného znaku, anebo kód náhradního (existujícího) znaku pro daný znak.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int _GetCharCode(char c)
        {
            var list = _TextCharList;
            int code = list.IndexOf(c);
            if (code == -1) code = list.IndexOf('D');
            return code;
        }
        /// <summary>
        /// Obsahuje všechny řešené Basic znaky, na odpovídajících pozicích
        /// </summary>
        private const string _TextCharList = " ABCDabcd";
        #endregion
    }
}