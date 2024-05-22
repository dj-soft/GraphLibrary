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

            int stop = 0;

            _TestResults();
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
            code.AppendLine("                result.Append(_CharToBasic(c));");
            code.AppendLine("            return result.ToString();");
            code.AppendLine("        }");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Metoda vrátí Basic znak k znaku zadanému");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"c\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static char _CharToBasic(char c)");
            code.AppendLine("        {");
            code.AppendLine("            switch ((int)c)");
            code.AppendLine("            {");

            for (int c = CharCode1; c < CharCode2; c++)
            {
                char c2 = (char)c;
                string t2 = c2.ToString();
                code.AppendLine($"                case {c}: return '{t2}';         // '{t2}'");
            }

            code.AppendLine("            }");
            code.AppendLine("            return 'O';                      // Náhradní");
            code.AppendLine("        }");

            return code.ToString();
        }
        /// <summary>
        /// Vrátí string (ne kód) obsahující basic znaky = tedy ty, které je třeba změřit
        /// </summary>
        /// <returns></returns>
        private static string _CreateBasicCharString()
        {
            var chars = new Dictionary<int, char>();
            for (int c = CharCode1; c <= CharCode2; c++)
            {
                char inpChar = ((char)c);
                char outChar = _CharToBasic(inpChar);
                int outCode = (int)outChar;
                if (!chars.ContainsKey(outCode))
                    chars.Add(outCode, outChar);
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
            var sizeTable = new Dictionary<string, Info>();

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
                    createMarginWidth(graphics, fontR, fontB);
                    createSingleChars(graphics, fontR, fontB);
                    createDoubleChars(graphics, fontR, fontB);
                }
            }

            return sizeTable;

            void createNoneHeight(Graphics gr, Font fR, Font fB)
            {
                string textHeight = "height";
                var sizeR = gr.MeasureString(textHeight, fR);
                var sizeB = gr.MeasureString(textHeight, fB);
                sizeTable.Add("", new Info() { Text = "", SizeR = sizeR.Height, SizeB = sizeB.Height });
            }
            void createMarginWidth(Graphics gr, Font fR, Font fB)
            {
                // Zde určím šířku okraje, která je vždy přičtena k šířce neprázdného textu
                string t1 = "A";
                string t2 = "AA";
                var sizeR1 = gr.MeasureString(t1, fR).Width;         // Šířka textu  Margin+A   je např. 40,8138
                var sizeR2 = gr.MeasureString(t2, fR).Width;         // Šířka textu  Margin+AA  je např. 68,2942
                var sizeB1 = gr.MeasureString(t1, fB).Width;
                var sizeB2 = gr.MeasureString(t2, fB).Width;
                float wR0 = sizeR1 - (sizeR2 - sizeR1);              // Jeden samotný znak A je:  (Margin+AA: 68,29) - (Margin+A:  40,81) =  A: 27,48
                float wB0 = sizeB1 - (sizeB2 - sizeB1);              //  Margin tedy je (Margin+A) - A = 13,33
                sizeTable.Add(_Margin, new Info() { Text = "", SizeR = wR0, SizeB = wB0 });
            }
            void createSingleChars(Graphics gr, Font fR, Font fB)
            {
                var marginInfo = sizeTable[_Margin];
                var margR = marginInfo.SizeR;
                var margB = marginInfo.SizeB;
                foreach (var textSingle in charArray)
                {
                    var sizeR = gr.MeasureString(textSingle, fR).Width;
                    var sizeB = gr.MeasureString(textSingle, fB).Width;
                    sizeTable.Add(textSingle, new Info() { Text = textSingle, SizeR = (sizeR - margR), SizeB = (sizeB - margB) });
                }
            }

            void createDoubleChars(Graphics gr, Font fR, Font fB)
            {
                var marginInfo = sizeTable[_Margin];
                var margR = marginInfo.SizeR;
                var margB = marginInfo.SizeB;
                foreach (var textDoubleA in charArray)
                    foreach (var textDoubleB in charArray)
                    {
                        var textDouble = textDoubleA + textDoubleB;
                        var sizeR = gr.MeasureString(textDouble, fR).Width;
                        var sizeB = gr.MeasureString(textDouble, fB).Width;
                        sizeTable.Add(textDouble, new Info() { Text = textDouble, SizeR = (sizeR - margR), SizeB = (sizeB - margB) });
                    }
            }
        }
        private const string _Margin = "Margin";
        /// <summary>
        /// Metoda vrátí kód metody, která vrací rozměr daného znaku: délky 0, 1 a 2 znaky.
        /// </summary>
        /// <param name="sizeTable"></param>
        /// <param name="basicCharString"></param>
        /// <param name="bold"></param>
        /// <returns></returns>
        private static string _GenerateTextToSizeCode(Dictionary<string, Info> sizeTable, string basicCharString)
        {
            // 1. Určím výšku znaku a margin:
            var heightInfo = sizeTable[""];
            float charHeight = heightInfo.SizeR;

            var marginInfo = sizeTable[_Margin];
            var margR = marginInfo.SizeR;
            var margB = marginInfo.SizeB;

            // 2. Sestavím data pro šířku jednotlivého znaku:
            //    Data jsou string v kódování: Znaky v pořadí z basicCharString; pro každý znak dvě místa pro hodnotu Regular a dvě pro Bold, v kódování Base86 (pořadí H-L),
            //    obsahuje polovinu šířky samostatného znaku:
            //    Data jsou použita v metodě _GetCharWidth1()
            string singleCode = "            return _GetCharWidth(cCurr, isBold, @\"";
            foreach (char cSingle in basicCharString)
            {
                string key = cSingle.ToString();
                var info = sizeTable[key];
                float widthR = info.SizeR / 2f;                      // Šířka poloviny daného Single znaku, Regular
                float widthB = info.SizeB / 2f;                      // Šířka poloviny daného Single znaku, Bold
                string singleDataOne = _GetDataForWidths(widthR, widthB);
                singleCode += singleDataOne;
            }
            singleCode += "\");";


            // 3. Sestavím data pro vzdálenosti středu dvou znaků (znak p = levý = Prev, a n = pravý = Next)
            //    Data jsou použita v metodě _GetCharWidth2()
            List<string> doubleList = new List<string>();
            for (int p = 0; p < basicCharString.Length; p++)
            {
                string doubleData = "                case " + p.ToString() + ": return _GetCharWidth(cCurr, isBold, @\"";

                char cPrev = basicCharString[p];
                var infoPrev = sizeTable[cPrev.ToString()];          // Data o písmenu Prev
                foreach (char cNext in basicCharString)
                {
                    var infoNext = sizeTable[cNext.ToString()];      // Data o písmenu Next
                    string key = cPrev.ToString() + cNext.ToString();
                    var info = sizeTable[key];                       // Data o dvojznaku PrevNext
                    float widthR = info.SizeR.Width - (infoPrev.SizeR.Width / 2f) - (infoNext.SizeR.Width / 2f);       // Vzdálenost středu Prev k středu Next znaku, Regular
                    float widthB = info.SizeB.Width - (infoPrev.SizeB.Width / 2f) - (infoNext.SizeB.Width / 2f);
                    string doubleDataOne = _GetDataForWidths(widthR, widthB);
                    doubleData += doubleDataOne;
                }
                doubleData += "\");";
                doubleList.Add(doubleData);
            }


            // String to Code:
            string heightCode = Math.Round(charHeight, 2).ToString().Replace(",", ".") + "f";
            string basicCharStringCode = basicCharString
                .Replace("\"", "\\\"")
                .Replace("\\", "\\\\");

            StringBuilder code = new StringBuilder();

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí výšku textu");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"sizeRatio\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static float _GetTextHeight(float? sizeRatio = null)");
            code.AppendLine("        {");
            code.AppendLine("            float height = " + heightCode + ";");
            code.AppendLine("            ");
            code.AppendLine("            // Korekce dle úpravy velikosti:");
            code.AppendLine("            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)");
            code.AppendLine("                height = sizeRatio.Value * height;");
            code.AppendLine("            ");
            code.AppendLine("            return height;");
            code.AppendLine("        }");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vypočte a vrátí šířku dodaného textu");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"text\"></param>");
            code.AppendLine("        /// <param name=\"isBold\"></param>");
            code.AppendLine("        /// <param name=\"sizeRatio\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static float _GetTextWidth(string text, bool isBold = false, float? sizeRatio = null)");
            code.AppendLine("        {");
            code.AppendLine("            if (text == null || text.Length == 0) return 0f;");
            code.AppendLine("            float width = 0f;");
            code.AppendLine("            int last = text.Length - 1;");
            code.AppendLine("            char cPrev = ' ';");
            code.AppendLine("            char cCurr;");
            code.AppendLine("            for (int i = 0; i <= last; i++)");
            code.AppendLine("            {");
            code.AppendLine("                cCurr = _CharToBasic(text[i]);                       // Basic Znak ze vstupního textu na aktuální pozici");
            code.AppendLine("                if (i == 0)");
            code.AppendLine("                {");
            code.AppendLine("                    width += _GetCharWidth1(cCurr, isBold);          // První znak: beru levou polovinu jeho plné šířky (levá == pravá)");
            code.AppendLine("                }");
            code.AppendLine("                else");
            code.AppendLine("                {");
            code.AppendLine("                    width += _GetCharWidth2(cPrev, cCurr, isBold);   // Každý průběžný znak: beru vzdálenost mezi polovinou znaku Previous a polovinou znaku Current");
            code.AppendLine("                    if (i == last)");
            code.AppendLine("                        width += _GetCharWidth1(cCurr, isBold);      // Poslední znak: beru pravou polovinu jeho plné šířky (levá == pravá)");
            code.AppendLine("                }");
            code.AppendLine("                cPrev = cCurr;");
            code.AppendLine("            }");
            code.AppendLine("            ");
            code.AppendLine("            // Korekce dle úpravy velikosti:");
            code.AppendLine("            if (sizeRatio.HasValue && sizeRatio.Value > 0f && sizeRatio.Value != 1f)");
            code.AppendLine("                width = sizeRatio.Value * width;");
            code.AppendLine("            ");
            code.AppendLine("            return width;");
            code.AppendLine("        }");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí vzdálenost mezi středem znaku <paramref name=\"cPrev\"/> a středem znaku <paramref name=\"cCurr\"/> pro styl písma <paramref name=\"isBold\"/>.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"cPrev\"></param>");
            code.AppendLine("        /// <param name=\"cCurr\"></param>");
            code.AppendLine("        /// <param name=\"isBold\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static float _GetCharWidth2(char cPrev, char cCurr, bool isBold)");
            code.AppendLine("        {");
            code.AppendLine("            int code = _GetCharCode(cPrev);");
            code.AppendLine("            switch (code)");
            code.AppendLine("            {");

            // code.AppendLine("                case 0: return _GetCharWidth(cCurr, isBold, \"AvBbdDfghh..\");");
            foreach (var doubleCode in doubleList)
                code.AppendLine(doubleCode);

            code.AppendLine("            }");
            code.AppendLine("            return 0f;");
            code.AppendLine("        }");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí polovinu šířky daného znaku <paramref name=\"cCurr\"/> pro styl písma <paramref name=\"isBold\"/>.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"cCurr\"></param>");
            code.AppendLine("        /// <param name=\"isBold\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static float _GetCharWidth1(char cCurr, bool isBold)");
            code.AppendLine("        {");

            // code.AppendLine("            return _GetCharWidth(cCurr, isBold, \"AvBbdDfghh..\");");
            code.AppendLine(singleCode);

            code.AppendLine("        }");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí float hodnotu z datového stringu <paramref name=\"data\"/> z pozice odpovídající danému znaku <paramref name=\"cCurr\"/> pro styl písma <paramref name=\"isBold\"/>.");
            code.AppendLine("        /// Datový blok obsahuje sekvenci dat: dva znaky pro Regular, dva znaky pro Bold, postupně pro všechny znaky převedené na kódy = odpovídající pozici.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"cCurr\"></param>");
            code.AppendLine("        /// <param name=\"isBold\"></param>");
            code.AppendLine("        /// <param name=\"data\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static float _GetCharWidth(char cCurr, bool isBold, string data)");
            code.AppendLine("        {");
            code.AppendLine("            int code = _GetCharCode(cCurr);                          // Pozice dat pro daný znak v datovém poli = index v jednoznačném soupisu podporovaných znaků '_TextCharList'. Není záporné.");
            code.AppendLine("            int index = 4 * code + (isBold ? 2 : 0);                 // Index ukazuje na počátek dat pro daný znak a Bold: data jsou sekvence 4 pozic pro jeden znak (cCurr); vždy 2 znaky Regular + 2 znaky Bold, ");
            code.AppendLine("            return ((86f * getVal(index)) + getVal(index + 1)) / 70f;//  v kódování Char(40) až Char(126) v pořadí HL (kde Char(126) již není přítomno), tedy 40 až 125 = 86 hodnot na 1 znak... 2 znaky = 0..7395; děleno 70f = 0 až 105,65, s krokem 0,014 px");
            code.AppendLine("            ");
            code.AppendLine("            float getVal(int i)");
            code.AppendLine("            {");
            code.AppendLine("                int val = (int)data[i] - 40;                         // Pokud na dané pozici je znak '(' = Char(40), odpovídá to hodnotě 0");
            code.AppendLine("                return (float)val;");
            code.AppendLine("            }");
            code.AppendLine("        }");

            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Vrátí text dlouhý 4 znaky, reprezentující dané šířky Regular <paramref name=\"widthR\"/> a Bold <paramref name=\"widthB\"/>, tak aby byl korektně dekódován v metodě <see cref=\"_GetCharWidth(char, bool, string)\"/>,");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"widthR\"></param>");
            code.AppendLine("        /// <param name=\"widthB\"></param>");
            code.AppendLine("        /// <returns></returns>");
            code.AppendLine("        private static string _GetDataForWidths(float widthR, float widthB)");
            code.AppendLine("        {");
            code.AppendLine("            split(widthR, out char rh, out char rl);");
            code.AppendLine("            split(widthB, out char bh, out char bl);");
            code.AppendLine("            return rh.ToString() + rl.ToString() + bh.ToString() + bl.ToString();");
            code.AppendLine("            ");
            code.AppendLine("            // Konverze Float na dva znaky:");
            code.AppendLine("            void split(double width, out char h, out char l)");
            code.AppendLine("            {");
            code.AppendLine("                if (width <= 0f) { h = '('; l = '('; return; }       // Znak  '('  reprezentuje 0");
            code.AppendLine("                ");
            code.AppendLine("                double w = 70d * width;                              // Pro zadanou šířku width = 34.975 bude w = 2448.25");
            code.AppendLine("                int hi = (int)Math.Truncate(w / 86d);                // Pro vstup w = 2448.25 je h = (2448.25 / 86d = 28.468023   => Truncate  =  28");
            code.AppendLine("                int lo = (int)Math.Round((w % 86d), 0);              // Pro vstup w = 2448.25 je l = (2448.25 % 86d = 40.25       => Round     =  40");
            code.AppendLine("                h = (char)(hi + 40);                                 // Char(40) = hodnota 0");
            code.AppendLine("                l = (char)(lo + 40);");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("        ");

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

            return code.ToString();
        }

        private static void _TestResults()
        {
            using (Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                bmp.SetResolution(144f, 144f);
                var family = SystemFonts.DefaultFont.FontFamily;
                float emSize = FontEmSize;
                using (Font fontR = new Font(family, emSize, FontStyle.Regular))
                using (Font fontB = new Font(family, emSize, FontStyle.Bold))
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    for (int t = 0; t < 100; t++)
                        testOne(graphics, fontR, fontB);
                }
            }


            void testOne(Graphics gr, Font fR, Font fB)
            {
                string text = Randomizer.GetSentence(3, 7);
                float h = _GetTextHeight();
                float wr = _GetTextWidth(text, false);
                float wb = _GetTextWidth(text, true);
                var sr = gr.MeasureString(text, fR);
                var sb = gr.MeasureString(text, fB);
            }

        }
        private class Info
        {
            public override string ToString()
            {
                return $"'{Text}': R='{SizeR:F3}f';  B='{SizeB:F3}f'";
            }
            public string Text;
            public float SizeR;
            public float SizeB;
        }

        private const int CharCode1 = 32;
        private const int CharCode2 = 565;

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
                result.Append(_CharToBasic(c));
            return result.ToString();
        }
        /// <summary>
        /// Metoda vrátí Basic znak k znaku zadanému
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static char _CharToBasic(char c)
        {
            switch ((int)c)
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
            return 'O';                       // Náhradní
        }
        /// <summary>
        /// Vrátí výšku textu
        /// </summary>
        /// <param name="sizeRatio"></param>
        /// <returns></returns>
        private static float _GetTextHeight(float? sizeRatio = null)
        {
            float height = 50.27f;

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
            float width = 0f;
            int last = text.Length - 1;
            char cPrev = ' ';
            char cCurr;
            for (int i = 0; i <= last; i++)
            {
                cCurr = _CharToBasic(text[i]);                       // Basic Znak ze vstupního textu na aktuální pozici
                if (i == 0)
                {
                    width += _GetCharWidth1(cCurr, isBold);          // První znak: beru levou polovinu jeho plné šířky (levá == pravá)
                }
                else
                {
                    width += _GetCharWidth2(cPrev, cCurr, isBold);   // Každý průběžný znak: beru vzdálenost mezi polovinou znaku Previous a polovinou znaku Current
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
                case 0: return _GetCharWidth(cCurr, isBold, @"((((5X5u6r78:;:X:;:X?n@5</<L414N6R6o6R6o7M7j:d;+5X5u6R6o5X5u5X5u:;:X:;:X:;:X:;:X:;:X:;:X:;:X:;:X:;:X:;:X5X5u5X5u:d;+:d;+:d;+:;:XAwB?</<L</<L=)=F=)=F</<L;5;Q=y>@=)=F5X5u9A9^</<L:;:X>s?9=)=F=y>@</<L=y>@=)=F</<L;5;Q=)=F</<L@gA-</<L</<L;5;Q5X5u5X5u5X5u8j91:5:R:;:X:;:X9A9^:;:X:;:X5X5u:;:X:;:X4f5-4f5-9A9^4f5->s?9:;:X:;:X:;:X:;:X6R6o9A9^5X5u:;:X9A9^=)=F9A9^9A9^9A9^6T6p5>5[6T6p:d;+:;:X6R6o:;:X:;:X:d;+=y>@");
                case 1: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 2: return _GetCharWidth(cCurr, isBold, @"-z.A2]3@3v4Y7@7y7@7y<r=V949m161o3V4:3V4:4Q557h8L2]3@3V4:2]3@2]3@7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y7@7y2]3@2]3@7h8L7h8L7h8L7@7y>|?`949m949m:-:g:-:g949m898s:~;a:-:g2]3@6E7)949m7@7y;w<[:-:g:~;a949m:~;a:-:g949m898s:-:g949m=k>N949m949m898s2]3@2]3@2]3@5o6R7:7s7@7y7@7y6E7)7@7y7@7y2]3@7@7y7@7y1k2N1k2N6E7)1k2N;w<[7@7y7@7y7@7y7@7y3V4:6E7)2]3@7@7y6E7):-:g6E7)6E7)6E7)3X4;2C2|3X4;7h8L7@7y3V4:7@7y7@7y7h8L:~;a");
                case 3: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 4: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 5: return _GetCharWidth(cCurr, isBold, @"6v7=;Y<<<r=V@<@v@<@vEoFRB0Bj:2:k<R=6<R=6=N>1@eAH;Y<<<R=6;Y<<;Y<<@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v@<@v;Y<<;Y<<@eAH@eAH@eAH@<@vGxH\B0BjB0BjC*CdC*CdB0BjA6AoCzD^C*Cd;Y<<?B?{B0Bj@<@vDsEWC*CdCzD^B0BjCzD^C*CdB0BjA6AoC*CdB0BjFgGKB0BjB0BjA6Ao;Y<<;Y<<;Y<<>k?O@6@p@<@v@<@v?B?{@<@v@<@v;Y<<@<@v@<@v:g;K:g;K?B?{:g;KDsEW@<@v@<@v@<@v@<@v<R=6?B?{;Y<<@<@v?B?{C*Cd?B?{?B?{?B?{<T=8;?;x<T=8@eAH@<@v<R=6@<@v@<@v@eAHCzD^");
                case 6: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 7: return _GetCharWidth(cCurr, isBold, @"+9+V/r0U161o4U594U59:2:k6I7-.K/.0k1O0k1O1g2J4~5a/r0U0k1O/r0U/r0U4U594U594U594U594U594U594U594U594U594U59/r0U/r0U4~5a4~5a4~5a4U59<;<u6I7-6I7-7C7}7C7}6I7-5O628=8w7C7}/r0U3[4>6I7-4U59979p7C7}8=8w6I7-8=8w7C7}6I7-5O627C7}6I7-;+;d6I7-6I7-5O62/r0U/r0U/r0U3.3h4O534U594U593[4>4U594U59/r0U4U594U59/*/d/*/d3[4>/*/d979p4U594U594U594U590k1O3[4>/r0U4U593[4>7C7}3[4>3[4>3[4>0m1Q/X0<0m1Q4~5a4U590k1O4U594U594~5a8=8w");
                case 8: return _GetCharWidth(cCurr, isBold, @"-Z-w2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,:^;B");
                case 9: return _GetCharWidth(cCurr, isBold, @"-Z-w2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,:^;B");
                case 10: return _GetCharWidth(cCurr, isBold, @".U.r383q4Q557q8U7q8U=N>19e:H1g2J414k414k5,5f8C8}383q414k383q383q7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U7q8U383q383q8C8}8C8}8C8}7q8U?W@;9e:H9e:H:^;B:^;B9e:H8j9N;Y<<:^;B383q6v7Z9e:H7q8U<R=6:^;B;Y<<9e:H;Y<<:^;B9e:H8j9N:^;B9e:H>F?*9e:H9e:H8j9N383q383q383q6J7-7k8N7q8U7q8U6v7Z7q8U7q8U383q7q8U7q8U2F3)2F3)6v7Z2F3)<R=67q8U7q8U7q8U7q8U414k6v7Z383q7q8U6v7Z:^;B6v7Z6v7Z6v7Z434m2t3W434m8C8}7q8U414k7q8U7q8U8C8};Y<<");
                case 11: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 12: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 13: return _GetCharWidth(cCurr, isBold, @"-Z-w2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,:^;B");
                case 14: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 15: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 16: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 17: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 18: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 19: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 20: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 21: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 22: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 23: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 24: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 25: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 26: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 27: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 28: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 29: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 30: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 31: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 32: return _GetCharWidth(cCurr, isBold, @"9*9G=b>F>|?`BEC*BEC*GxH\D9Ds<;<u>[?@>[?@?W@;BnCR=b>F>[?@=b>F=b>FBEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*BEC*=b>F=b>FBnCRBnCRBnCRBEC*J+JfD9DsD9DsE3EmE3EmD9DsC?CyF-FgE3Em=b>FAKB/D9DsBEC*F}GaE3EmF-FgD9DsF-FgE3EmD9DsC?CyE3EmD9DsHqIUD9DsD9DsC?Cy=b>F=b>F=b>F@tAXB?ByBEC*BEC*AKB/BEC*BEC*=b>FBEC*BEC*<p=T<p=TAKB/<p=TF}GaBEC*BEC*BEC*BEC*>[?@AKB/=b>FBEC*AKB/E3EmAKB/AKB/AKB/>^?B=H>,>^?BBnCRBEC*>[?@BEC*BEC*BnCRF-Fg");
                case 33: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 34: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 35: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 36: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 37: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 38: return _GetCharWidth(cCurr, isBold, @"2=2Z6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e?A?z");
                case 39: return _GetCharWidth(cCurr, isBold, @"5,5H9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?SB0Bi");
                case 40: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 41: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 42: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 43: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 44: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 45: return _GetCharWidth(cCurr, isBold, @"5{6B:^;A;w<[?A?z?A?zDsEWA5An979p;W<;;W<;<R=6?i@M:^;A;W<;:^;A:^;A?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z:^;A:^;A?i@M?i@M?i@M?A?zF}GaA5AnA5AnB.BhB.BhA5An@:@tC)CbB.Bh:^;A>F?*A5An?A?zCxD\B.BhC)CbA5AnC)CbB.BhA5An@:@tB.BhA5AnElFPA5AnA5An@:@t:^;A:^;A:^;A=p>S?;?t?A?z?A?z>F?*?A?z?A?z:^;A?A?z?A?z9l:O9l:O>F?*9l:OCxD\?A?z?A?z?A?z?A?z;W<;>F?*:^;A?A?z>F?*B.Bh>F?*>F?*>F?*;Y<<:D:};Y<<?i@M?A?z;W<;?A?z?A?z?i@MC)Cb");
                case 46: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 47: return _GetCharWidth(cCurr, isBold, @"5,5H9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?SB0Bi");
                case 48: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 49: return _GetCharWidth(cCurr, isBold, @"5,5H9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?SB0Bi");
                case 50: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 51: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 52: return _GetCharWidth(cCurr, isBold, @"2=2Z6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e?A?z");
                case 53: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 54: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 55: return _GetCharWidth(cCurr, isBold, @"7o86<R=5=k>NA5AnA5AnFgGKC)Cb;+;d=K>/=K>/>F?*A]BA<R=5=K>/<R=5<R=5A5AnA5AnA5AnA5AnA5AnA5AnA5AnA5AnA5AnA5An<R=5<R=5A]BAA]BAA]BAA5AnHqIUC)CbC)CbCxD\CxD\C)CbB.BhDsEVCxD\<R=5@:@tC)CbA5AnElFPCxD\DsEVC)CbDsEVCxD\C)CbB.BhCxD\C)CbG`HCC)CbC)CbB.Bh<R=5<R=5<R=5?d@GA/AhA5AnA5An@:@tA5AnA5An<R=5A5AnA5An;`<C;`<C@:@t;`<CElFPA5AnA5AnA5AnA5An=K>/@:@t<R=5A5An@:@tCxD\@:@t@:@t@:@t=M>0<8<q=M>0A]BAA5An=K>/A5AnA5AnA]BADsEV");
                case 56: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 57: return _GetCharWidth(cCurr, isBold, @"383U7p8T949m<T=7<T=7B0Bj>H?+6I7-8j9N8j9N9e:H<|=_7p8T8j9N7p8T7p8T<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=7<T=77p8T7p8T<|=_<|=_<|=_<T=7D9Ds>H?+>H?+?A?{?A?{>H?+=M>0@<@u?A?{7p8T;Y<<>H?+<T=7A5An?A?{@<@u>H?+@<@u?A?{>H?+=M>0?A?{>H?+C)Cb>H?+>H?+=M>07p8T7p8T7p8T;-;f<N=1<T=7<T=7;Y<<<T=7<T=77p8T<T=7<T=77)7b7)7b;Y<<7)7bA5An<T=7<T=7<T=7<T=78j9N;Y<<7p8T<T=7;Y<<?A?{;Y<<;Y<<;Y<<8l9O7V8:8l9O<|=_<T=78j9N<T=7<T=7<|=_@<@u");
                case 58: return _GetCharWidth(cCurr, isBold, @"2=2Z6v7Y898s;Y<<;Y<<A6Ao=M>05O627o8S7o8S8j9N<+<e6v7Y7o8S6v7Y6v7Y;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<;Y<<6v7Y6v7Y<+<e<+<e<+<e;Y<<C?Cy=M>0=M>0>F?*>F?*=M>0<R=6?A?z>F?*6v7Y:^;B=M>0;Y<<@:@t>F?*?A?z=M>0?A?z>F?*=M>0<R=6>F?*=M>0B.Bh=M>0=M>0<R=66v7Y6v7Y6v7Y:2:k;S<6;Y<<;Y<<:^;B;Y<<;Y<<6v7Y;Y<<;Y<<6.6g6.6g:^;B6.6g@:@t;Y<<;Y<<;Y<<;Y<<7o8S:^;B6v7Y;Y<<:^;B>F?*:^;B:^;B:^;B7q8U6\7?7q8U<+<e;Y<<7o8S;Y<<;Y<<<+<e?A?z");
                case 59: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 60: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 61: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 62: return _GetCharWidth(cCurr, isBold, @"/s094U595o6R999r999r>k?O;-;f3.3h5O635O636J7-9a:D4U595O634U594U59999r999r999r999r999r999r999r999r999r999r4U594U599a:D9a:D9a:D999r@tAX;-;f;-;f;|<`;|<`;-;f:2:k<w=Z;|<`4U598>8w;-;f999r=p>S;|<`<w=Z;-;f<w=Z;|<`;-;f:2:k;|<`;-;f?d@G;-;f;-;f:2:k4U594U594U597h8K929l999r999r8>8w999r999r4U59999r999r3d4G3d4G8>8w3d4G=p>S999r999r999r999r5O638>8w4U59999r8>8w;|<`8>8w8>8w8>8w5Q644;4u5Q649a:D999r5O63999r999r9a:D<w=Z");
                case 63: return _GetCharWidth(cCurr, isBold, @"1>1Z5v6Z7:7s:Z;=:Z;=@6@p<N=14O536p7T6p7T7k8N;,;e5v6Z6p7T5v6Z5v6Z:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=:Z;=5v6Z5v6Z;,;e;,;e;,;e:Z;=B?By<N=1<N=1=G>+=G>+<N=1;S<6>A>{=G>+5v6Z9_:B<N=1:Z;=?;?t=G>+>A>{<N=1>A>{=G>+<N=1;S<6=G>+<N=1A/Ah<N=1<N=1;S<65v6Z5v6Z5v6Z929l:S;7:Z;=:Z;=9_:B:Z;=:Z;=5v6Z:Z;=:Z;=5/5h5/5h9_:B5/5h?;?t:Z;=:Z;=:Z;=:Z;=6p7T9_:B5v6Z:Z;=9_:B=G>+9_:B9_:B9_:B6r7U5\6@6r7U;,;e:Z;=6p7T:Z;=:Z;=;,;e>A>{");
                case 64: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 65: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 66: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 67: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 68: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 69: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 70: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 71: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 72: return _GetCharWidth(cCurr, isBold, @"+o,60Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@8s9V");
                case 73: return _GetCharWidth(cCurr, isBold, @"+o,60Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@8s9V");
                case 74: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 75: return _GetCharWidth(cCurr, isBold, @"+o,60Q151k2N555n555n:g;K7)7b/*/d1K2/1K2/2F3)5]6@0Q151K2/0Q150Q15555n555n555n555n555n555n555n555n555n555n0Q150Q155]6@5]6@5]6@555n<p=T7)7b7)7b7x8\7x8\7)7b6.6g8s9V7x8\0Q154:4s7)7b555n9l:O7x8\8s9V7)7b8s9V7x8\7)7b6.6g7x8\7)7b;`<C7)7b7)7b6.6g0Q150Q150Q153d4G5/5h555n555n4:4s555n555n0Q15555n555n/`0C/`0C4:4s/`0C9l:O555n555n555n555n1K2/4:4s0Q15555n4:4s7x8\4:4s4:4s4:4s1M20070q1M205]6@555n1K2/555n555n5]6@8s9V");
                case 76: return _GetCharWidth(cCurr, isBold, @"5{6B:^;A;w<[?A?z?A?zDsEWA5An979p;W<;;W<;<R=6?i@M:^;A;W<;:^;A:^;A?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z?A?z:^;A:^;A?i@M?i@M?i@M?A?zF}GaA5AnA5AnB.BhB.BhA5An@:@tC)CbB.Bh:^;A>F?*A5An?A?zCxD\B.BhC)CbA5AnC)CbB.BhA5An@:@tB.BhA5AnElFPA5AnA5An@:@t:^;A:^;A:^;A=p>S?;?t?A?z?A?z>F?*?A?z?A?z:^;A?A?z?A?z9l:O9l:O>F?*9l:OCxD\?A?z?A?z?A?z?A?z;W<;>F?*:^;A?A?z>F?*B.Bh>F?*>F?*>F?*;Y<<:D:};Y<<?i@M?A?z;W<;?A?z?A?z?i@MC)Cb");
                case 77: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 78: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 79: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 80: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 81: return _GetCharWidth(cCurr, isBold, @"-Z-w2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,:^;B");
                case 82: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 83: return _GetCharWidth(cCurr, isBold, @",a,}1C1}2]3@5|6`5|6`;Y<<7p8T/r0U2<2w2<2w383q6O721C1}2<2w1C1}1C1}5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`5|6`1C1}1C1}6O726O726O725|6`=b>F7p8T7p8T8j9N8j9N7p8T6v7Y9d:H8j9N1C1}5,5e7p8T5|6`:^;A8j9N9d:H7p8T9d:H8j9N7p8T6v7Y8j9N7p8T<R=57p8T7p8T6v7Y1C1}1C1}1C1}4U595v6Z5|6`5|6`5,5e5|6`5|6`1C1}5|6`5|6`0Q150Q155,5e0Q15:^;A5|6`5|6`5|6`5|6`2<2w5,5e1C1}5|6`5,5e8j9N5,5e5,5e5,5e2?2x1)1c2?2x6O725|6`2<2w5|6`5|6`6O729d:H");
                case 84: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 85: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 86: return _GetCharWidth(cCurr, isBold, @"414O8j9N:-:g=M>1=M>1C*Cd?A?{7C7}9c:H9c:H:^;B=u>Y8j9N9c:H8j9N8j9N=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>1=M>18j9N8j9N=u>Y=u>Y=u>Y=M>1E3Em?A?{?A?{@:@u@:@u?A?{>F?*A5Ao@:@u8j9N<R=6?A?{=M>1B.Bh@:@uA5Ao?A?{A5Ao@:@u?A?{>F?*@:@u?A?{CxD\?A?{?A?{>F?*8j9N8j9N8j9N;|<`=G>+=M>1=M>1<R=6=M>1=M>18j9N=M>1=M>17x8\7x8\<R=67x8\B.Bh=M>1=M>1=M>1=M>19c:H<R=68j9N=M>1<R=6@:@u<R=6<R=6<R=69e:I8P949e:I=u>Y=M>19c:H=M>1=M>1=u>YA5Ao");
                case 87: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 88: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 89: return _GetCharWidth(cCurr, isBold, @"0I0f5,5e6E7)9e:H9e:H?B?{;Y<<3[4>5{6_5{6_6v7Z:7:q5,5e5{6_5,5e5,5e9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H9e:H5,5e5,5e:7:q:7:q:7:q9e:HAKB/;Y<<;Y<<<R=6<R=6;Y<<:^;B=M>0<R=65,5e8j9N;Y<<9e:H>F?*<R=6=M>0;Y<<=M>0<R=6;Y<<:^;B<R=6;Y<<@:@t;Y<<;Y<<:^;B5,5e5,5e5,5e8>8w9_:B9e:H9e:H8j9N9e:H9e:H5,5e9e:H9e:H4:4s4:4s8j9N4:4s>F?*9e:H9e:H9e:H9e:H5{6_8j9N5,5e9e:H8j9N<R=68j9N8j9N8j9N5}6a4h5K5}6a:7:q9e:H5{6_9e:H9e:H:7:q=M>0");
                case 90: return _GetCharWidth(cCurr, isBold, @"-\-y2?2x3X4;6x7[6x7[<T=88l9O0m1Q383r383r434m7J8.2?2x383r2?2x2?2x6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[2?2x2?2x7J8.7J8.7J8.6x7[>^?B8l9O8l9O9e:I9e:I8l9O7q8U:`;C9e:I2?2x5}6a8l9O6x7[;Y<<9e:I:`;C8l9O:`;C9e:I8l9O7q8U9e:I8l9O=M>08l9O8l9O7q8U2?2x2?2x2?2x5Q646r7U6x7[6x7[5}6a6x7[6x7[2?2x6x7[6x7[1M201M205}6a1M20;Y<<6x7[6x7[6x7[6x7[383r5}6a2?2x6x7[5}6a9e:I5}6a5}6a5}6a3:3s1{2^3:3s7J8.6x7[383r6x7[6x7[7J8.:`;C");
                case 91: return _GetCharWidth(cCurr, isBold, @",G,c1)1c2C2|5b6F5b6F;?;x7V8:/X0<1x2]1x2]2t3W656n1)1c1x2]1)1c1)1c5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F5b6F1)1c1)1c656n656n656n5b6F=H>,7V8:7V8:8P948P947V8:6\7?9J:.8P941)1c4h5K7V8:5b6F:D:}8P949J:.7V8:9J:.8P947V8:6\7?8P947V8:<8<q7V8:7V8:6\7?1)1c1)1c1)1c4;4u5\6@5b6F5b6F4h5K5b6F5b6F1)1c5b6F5b6F070q070q4h5K070q:D:}5b6F5b6F5b6F5b6F1x2]4h5K1)1c5b6F4h5K8P944h5K4h5K4h5K1{2^0e1I1{2^656n5b6F1x2]5b6F5b6F656n9J:.");
                case 92: return _GetCharWidth(cCurr, isBold, @"-\-y2?2x3X4;6x7[6x7[<T=88l9O0m1Q383r383r434m7J8.2?2x383r2?2x2?2x6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[6x7[2?2x2?2x7J8.7J8.7J8.6x7[>^?B8l9O8l9O9e:I9e:I8l9O7q8U:`;C9e:I2?2x5}6a8l9O6x7[;Y<<9e:I:`;C8l9O:`;C9e:I8l9O7q8U9e:I8l9O=M>08l9O8l9O7q8U2?2x2?2x2?2x5Q646r7U6x7[6x7[5}6a6x7[6x7[2?2x6x7[6x7[1M201M205}6a1M20;Y<<6x7[6x7[6x7[6x7[383r5}6a2?2x6x7[5}6a9e:I5}6a5}6a5}6a3:3s1{2^3:3s7J8.6x7[383r6x7[6x7[7J8.:`;C");
                case 93: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 94: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 95: return _GetCharWidth(cCurr, isBold, @"-Z-w2<2w3V4:6v7Z6v7Z<R=68j9N0k1O363q363q414k7H8,2<2w363q2<2w2<2w6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z6v7Z2<2w2<2w7H8,7H8,7H8,6v7Z>[?@8j9N8j9N9c:H9c:H8j9N7o8S:^;B9c:H2<2w5{6_8j9N6v7Z;W<;9c:H:^;B8j9N:^;B9c:H8j9N7o8S9c:H8j9N=K>/8j9N8j9N7o8S2<2w2<2w2<2w5O636p7T6v7Z6v7Z5{6_6v7Z6v7Z2<2w6v7Z6v7Z1K2/1K2/5{6_1K2/;W<;6v7Z6v7Z6v7Z6v7Z363q5{6_2<2w6v7Z5{6_9c:H5{6_5{6_5{6_383r1x2]383r7H8,6v7Z363q6v7Z6v7Z7H8,:^;B");
                case 96: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 97: return _GetCharWidth(cCurr, isBold, @"1D1a5|6`7@7y:`;C:`;C@<@v<T=74U596v7Z6v7Z7q8U;2;k5|6`6v7Z5|6`5|6`:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C:`;C5|6`5|6`;2;k;2;k;2;k:`;CBEC*<T=7<T=7=M>1=M>1<T=7;Y<<>H?+=M>15|6`9e:H<T=7:`;C?A?z=M>1>H?+<T=7>H?+=M>1<T=7;Y<<=M>1<T=7A5An<T=7<T=7;Y<<5|6`5|6`5|6`999r:Z;=:`;C:`;C9e:H:`;C:`;C5|6`:`;C:`;C555n555n9e:H555n?A?z:`;C:`;C:`;C:`;C6v7Z9e:H5|6`:`;C9e:H=M>19e:H9e:H9e:H6x7[5b6F6x7[;2;k:`;C6v7Z:`;C:`;C;2;k>H?+");
                case 98: return _GetCharWidth(cCurr, isBold, @"1l236O727h8L;2;k;2;k@eAH<|=_4~5a7H8,7H8,8C8};Z<>6O727H8,6O726O72;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k;2;k6O726O72;Z<>;Z<>;Z<>;2;kBnCR<|=_<|=_=u>Y=u>Y<|=_<+<e>p?S=u>Y6O72:7:q<|=_;2;k?i@M=u>Y>p?S<|=_>p?S=u>Y<|=_<+<e=u>Y<|=_A]BA<|=_<|=_<+<e6O726O726O729a:D;,;e;2;k;2;k:7:q;2;k;2;k6O72;2;k;2;k5]6@5]6@:7:q5]6@?i@M;2;k;2;k;2;k;2;k7H8,:7:q6O72;2;k:7:q=u>Y:7:q:7:q:7:q7J8.656n7J8.;Z<>;2;k7H8,;2;k;2;k;Z<>>p?S");
                case 99: return _GetCharWidth(cCurr, isBold, @"5,5H9d:H:~;a>H?+>H?+CzD^@<@u8=8w:^;B:^;B;Y<<>p?S9d:H:^;B9d:H9d:H>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+>H?+9d:H9d:H>p?S>p?S>p?S>H?+F-Fg@<@u@<@uA5AoA5Ao@<@u?A?zB0BiA5Ao9d:H=M>0@<@u>H?+C)CbA5AoB0Bi@<@uB0BiA5Ao@<@u?A?zA5Ao@<@uDsEV@<@u@<@u?A?z9d:H9d:H9d:H<w=Z>A>{>H?+>H?+=M>0>H?+>H?+9d:H>H?+>H?+8s9V8s9V=M>08s9VC)Cb>H?+>H?+>H?+>H?+:^;B=M>09d:H>H?+=M>0A5Ao=M>0=M>0=M>0:`;C9J:.:`;C>p?S>H?+:^;B>H?+>H?+>p?SB0Bi");
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
            return _GetCharWidth(cCurr, isBold, @"-M-M2/2L3I3e6i7/6i7/<E<b8\8y0^0{3)3F3)3F3z4A7;7X2/2L3)3F2/2L2/2L6i7/6i7/6i7/6i7/6i7/6i7/6i7/6i7/6i7/6i7/2/2L2/2L7;7X7;7X7;7X6i7/>N>l8\8y8\8y9V9s9V9s8\8y7b8):P:m9V9s2/2L5n658\8y6i7/;J;f9V9s:P:m8\8y:P:m9V9s8\8y7b8)9V9s8\8y=>=Z8\8y8\8y7b8)2/2L2/2L2/2L5A5^6b7)6i7/6i7/5n656i7/6i7/2/2L6i7/6i7/1=1Z1=1Z5n651=1Z;J;f6i7/6i7/6i7/6i7/3)3F5n652/2L6i7/5n659V9s5n655n655n653+3G1k223+3G7;7X6i7/3)3F6i7/6i7/7;7X:P:m");
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
        /// <summary>
        /// Velikost písma, pro kterou je systém naplněn
        /// </summary>
        private const float FontEmSize = 20f;
        #endregion
    }
}