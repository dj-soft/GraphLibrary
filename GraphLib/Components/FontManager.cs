using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// <see cref="FontManagerInfo"/> : dokáže určit souřadnice znaků v konkrétním textu pro vykreslování tohoto textu do grafiky "per znak". Vhodné pro textový editor.
    /// </summary>
    public class FontManagerInfo
    {
        #region Singleton
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static FontManagerInfo Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Locker)
                    {
                        if (__Current == null)
                            __Current = new FontManagerInfo();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private FontManagerInfo()
        {
            __FontInfos = new Dictionary<string, FontManagerItem>();
            __EditorFormatFlags = StringFormat.GenericTypographic.FormatFlags | StringFormatFlags.MeasureTrailingSpaces;
            __StandardFormatFlags = StringFormat.GenericTypographic.FormatFlags;
            __DefaultGraphics = null;
            CR = "\r";
            LF = "\n";
            CRLF = CR + LF;
            CrLf = CRLF.ToCharArray();
            SPACE = " ";
        }
        /// <summary>
        /// Najde / založí objekt pro data pro jeden typ fontu. Řeší multithread zámek.
        /// </summary>
        /// <param name="fontKey"></param>
        /// <returns></returns>
        private FontManagerItem _GetFontInfo(string fontKey)
        {
            FontManagerItem fontInfo;
            if (!__FontInfos.TryGetValue(fontKey, out fontInfo))
            {
                lock (__FontInfos)
                {
                    if (!__FontInfos.TryGetValue(fontKey, out fontInfo))
                    {
                        fontInfo = new FontManagerItem(fontKey);
                        __FontInfos.Add(fontKey, fontInfo);
                    }
                }
            }
            return fontInfo;
        }
        /// <summary>
        /// Obsahuje referenci na stabilní objekt Graphics použitelný pro nouzové měření znaků.
        /// </summary>
        private static Graphics _DefaultGraphics
        {
            get
            {
                if (Current.__DefaultGraphics == null)
                {
                    Current.__Bitmap = new Bitmap(500, 250, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Current.__Bitmap.SetResolution(96f, 96f);
                    Current.__DefaultGraphics = Graphics.FromImage(Current.__Bitmap);
                }
                return Current.__DefaultGraphics;
            }
        }
        private static FontManagerInfo __Current = null;
        private static object __Locker = new object();
        private Dictionary<string, FontManagerItem> __FontInfos;
        private StringFormatFlags __EditorFormatFlags;
        private StringFormat __EditorStringFormat;
        private StringFormatFlags __StandardFormatFlags;
        private StringFormat __StandardStringFormat;
        private Graphics __DefaultGraphics;
        private Bitmap __Bitmap;
        #endregion
        #region Změření znaků
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontInfo"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static CharPositionInfo[] GetCharInfo(string text, FontInfo fontInfo, FontMeasureParams parameters = null)
        {
            return _GetCharInfo(text, _DefaultGraphics, fontInfo.Font, parameters);
        }
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static CharPositionInfo[] GetCharInfo(string text, Font font, FontMeasureParams parameters = null)
        {
            return _GetCharInfo(text, _DefaultGraphics, font, parameters);
        }
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="graphics"></param>
        /// <param name="fontInfo"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static CharPositionInfo[] GetCharInfo(string text, Graphics graphics, FontInfo fontInfo, FontMeasureParams parameters = null)
        {
            return _GetCharInfo(text, graphics, fontInfo.Font, parameters);
        }
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static CharPositionInfo[] GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
        {
            return _GetCharInfo(text, graphics, font, parameters);
        }
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static CharPositionInfo[] _GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
        {
            if (text == null || graphics == null || font == null) return null;
            int length = text.Length;
            if (length == 0) return new CharPositionInfo[0];

            string fontKey = FontManagerItem.GetFontKey(font);
            FontManagerItem fontInfo = Current._GetFontInfo(fontKey);
            return fontInfo.GetCharInfo(text, graphics, font, parameters);
        }
        #endregion
        #region Změření řádku
        /// <summary>
        /// Vrátí výšku jednoho řádku textu v daném fontu
        /// </summary>
        /// <param name="fontInfo"></param>
        /// <returns></returns>
        public static int GetFontHeight(FontInfo fontInfo)
        {
            return _GetFontHeight(fontInfo.Font, _DefaultGraphics);
        }
        /// <summary>
        /// Vrátí výšku jednoho řádku textu v daném fontu
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public static int GetFontHeight(Font font)
        {
            return _GetFontHeight(font, _DefaultGraphics);
        }
        /// <summary>
        /// Vrátí výšku jednoho řádku textu v daném fontu
        /// </summary>
        /// <param name="fontInfo"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static int GetFontHeight(FontInfo fontInfo, Graphics graphics)
        {
            return _GetFontHeight(fontInfo.Font, graphics);
        }
        /// <summary>
        /// Vrátí výšku jednoho řádku textu v daném fontu
        /// </summary>
        /// <param name="font"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static int GetFontHeight(Font font, Graphics graphics)
        {
            return _GetFontHeight(font, graphics);
        }
        /// <summary>
        /// Vrátí výšku jednoho řádku textu v daném fontu
        /// </summary>
        /// <param name="font"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        private static int _GetFontHeight(Font font, Graphics graphics)
        {
            if (graphics == null || font == null) return 0;
            string fontKey = FontManagerItem.GetFontKey(font);
            FontManagerItem fontInfo = Current._GetFontInfo(fontKey);
            return fontInfo.GetFontHeight(graphics, font);
        }
        #endregion
        #region StringFormats, NormalizeCrLf
        /// <summary>
        /// Obsahuje formátovací příznaky pro psaní textu = StringFormat.GenericTypographic.FormatFlags | MeasureTrailingSpaces
        /// </summary>
        public static StringFormatFlags EditorStringFormatFlags { get { return Current.__EditorFormatFlags; } }
        /// <summary>
        /// Obsahuje permanentní instanci StringFormat používanou pro rychlou práci s textem při editaci.
        /// Do této instance se nesmí nic vkládat, měnit ani nastavovat <see cref="StringFormat.SetMeasurableCharacterRanges(CharacterRange[])"/>!!!
        /// K tomu slouží new instance generovaná v metodě <see cref="CreateNewEditorStringFormat()"/>. Ta se má používat v using patternu.
        /// </summary>
        public static StringFormat EditorStringFormat { get { return Current._EditorStringFormat; } }
        /// <summary>
        /// Zajistí, že v <see cref="__EditorStringFormat"/> bude přítomna instance (nikoli NULL), vytvořená jako <see cref="CreateNewEditorStringFormat()"/>.
        /// </summary>
        private StringFormat _EditorStringFormat
        {
            get
            {
                if (__EditorStringFormat == null)
                    __EditorStringFormat = CreateNewEditorStringFormat();
                return __EditorStringFormat;
            }
        }
        /// <summary>
        /// Vygeneruje a vrátí new instanci <see cref="StringFormat"/> pro obecné použití při práci s textem
        /// </summary>
        /// <returns></returns>
        public static StringFormat CreateNewEditorStringFormat() { return CreateNewStandardStringFormat(Current.__EditorFormatFlags); }
        /// <summary>
        /// Vygeneruje a vrátí new instanci <see cref="StringFormat"/> pro obecné použití při práci s textem
        /// </summary>
        /// <returns></returns>
        public static StringFormat CreateNewEditorStringFormat(StringFormatFlags formatFlags)
        {
            StringFormat stringFormat = new StringFormat(formatFlags);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.Trimming = StringTrimming.None;
            return stringFormat;
        }
        /// <summary>
        /// Obsahuje formátovací příznaky pro psaní textu = StringFormat.GenericTypographic.FormatFlags
        /// </summary>
        public static StringFormatFlags StandardStringFormatFlags { get { return Current.__StandardFormatFlags; } }
        /// <summary>
        /// Obsahuje permanentní instanci StringFormat používanou pro rychlou práci s textem při prostém vypisování.
        /// Do této instance se nesmí nic vkládat, měnit ani nastavovat <see cref="StringFormat.SetMeasurableCharacterRanges(CharacterRange[])"/>!!!
        /// K tomu slouží new instance generovaná v metodě <see cref="CreateNewStandardStringFormat()"/>. Ta se má používat v using patternu.
        /// </summary>
        public static StringFormat StandardStringFormat { get { return Current._StandardStringFormat; } }
        /// <summary>
        /// Zajistí, že v <see cref="__StandardStringFormat"/> bude přítomna instance (nikoli NULL), vytvořená jako <see cref="CreateNewStandardStringFormat()"/>.
        /// </summary>
        private StringFormat _StandardStringFormat
        {
            get
            {
                if (__StandardStringFormat == null)
                    __StandardStringFormat = CreateNewStandardStringFormat();
                return __StandardStringFormat;
            }
        }
        /// <summary>
        /// Vygeneruje a vrátí new instanci <see cref="StringFormat"/> pro obecné použití při práci s textem při prostém vypisování.
        /// </summary>
        /// <returns></returns>
        public static StringFormat CreateNewStandardStringFormat() { return CreateNewStandardStringFormat(Current.__StandardFormatFlags); }
        /// <summary>
        /// Vygeneruje a vrátí new instanci <see cref="StringFormat"/> pro obecné použití při práci s textem při prostém vypisování.
        /// </summary>
        /// <returns></returns>
        public static StringFormat CreateNewStandardStringFormat(StringFormatFlags formatFlags)
        {
            StringFormat stringFormat = new StringFormat(formatFlags);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.Trimming = StringTrimming.EllipsisCharacter;
            return stringFormat;
        }
        /// <summary>
        /// Vrátí daný string s normalizovaným Cr+Lf
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string NormalizeCrLf(string text)
        {
            if (text == null) return "";
            if (text.IndexOfAny(CrLf) < 0) return text;              // Pokud v textu není ani CR ani LF, nebudu řešit nic...
            StringBuilder sb = new StringBuilder(text);
            sb = sb.Replace(CRLF, CR);
            sb = sb.Replace(LF, CR);
            sb = sb.Replace(CR, CRLF);
            return sb.ToString();
        }
        /// <summary>
        /// Text znaku CR
        /// </summary>
        protected static string CR;
        /// <summary>
        /// Text znaku LF
        /// </summary>
        protected static string LF;
        /// <summary>
        /// Text znaků CR a LF
        /// </summary>
        protected static string CRLF;
        /// <summary>
        /// Dva znaky CR, LF
        /// </summary>
        protected static char[] CrLf;
        /// <summary>
        /// Text znaku MEZERA
        /// </summary>
        protected static string SPACE;
        #endregion
        #region class FontMeasureOneInfo : Třída obsahující informace o jednom fontu
        /// <summary>
        /// <see cref="FontManagerItem"/> : Třída obsahující informace o jednom fontu, včetně určování souřadnic znaků v tomto fontu
        /// </summary>
        private class FontManagerItem
        {
            #region Konstrukce, základní property a proměnné
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="fontKey"></param>
            public FontManagerItem(string fontKey)
            {
                this._FontKey = fontKey;
                this._FontBounds = new Dictionary<string, RectangleF>();
            }
            /// <summary>
            /// Klíč fontu
            /// </summary>
            public string FontKey { get { return _FontKey; } }
            /// <summary>
            /// Vrátí název fontu = klíč do Dictionary <see cref="__FontInfos"/> = pro metodu <see cref="_GetFontInfo(string)"/>.
            /// </summary>
            /// <param name="font"></param>
            /// <returns></returns>
            public static string GetFontKey(Font font)
            {
                return font.Name + "#" + font.SizeInPoints.ToString("##0.0") + "#" + (font.Bold ? "B" : "") + (font.Italic ? "I" : "") + (font.Underline ? "U" : "");
            }
            /// <summary>
            /// Klíč fontu a grafiky
            /// </summary>
            private string _FontKey;
            /// <summary>
            /// Dictionary kombinací znaků a jejich souřadnic.
            /// Key = jeden znak (když je první v řádku nebo první za mezerou), pak Value je jehou šířka a výška.
            /// Anebo Key = dva znaky (string), když jde o posloupnost více znaků za sebou a svou roli hraje i vzájemný vztah znaků. Pak Value je souřadnice (na ose X) druhého znaku relativně k počátku (X) znaku prvního.
            /// </summary>
            private Dictionary<string, RectangleF> _FontBounds;
            #endregion
            #region Měření fontu
            /// <summary>
            /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
            /// </summary>
            /// <param name="text"></param>
            /// <param name="graphics"></param>
            /// <param name="fontInfo"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public CharPositionInfo[] GetCharInfo(string text, Graphics graphics, FontInfo fontInfo, FontMeasureParams parameters = null)
            {
                return GetCharInfo(text, graphics, fontInfo.Font, parameters);
            }
            /// <summary>
            /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
            /// </summary>
            /// <param name="text"></param>
            /// <param name="graphics"></param>
            /// <param name="font"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public CharPositionInfo[] GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
            {
                LoadBasicFontData(graphics, font);

                // Převezmeme data z parametru:
                bool multiline = parameters?.Multiline ?? false;
                Point origin = parameters?.Origin ?? Point.Empty;
                float paramWidth = (float)(parameters?.Width ?? 0);
                float paramLineHeightRatio = (parameters?.LineHeightRatio ?? 0f);
                float paramLineHeightAdd = (parameters?.LineHeightAdd ?? 0f);
                bool wrapWord = (parameters?.WrapWord ?? false);
                bool hasPasswordChar = (parameters?.PasswordChar.HasValue ?? false);
                string passwordChar = (hasPasswordChar ? parameters.PasswordChar.Value : ' ').ToString();

                int originX = origin.X;
                float right = (paramWidth > 0f ? (originX + paramWidth) : 0);
                int fontHeight = _FontHeight;
                float lineHeight = (float)fontHeight;
                if (paramLineHeightRatio > 0f) lineHeight = lineHeight * paramLineHeightRatio;
                lineHeight = lineHeight + paramLineHeightAdd;
                int rowHeight = (int)Math.Ceiling(lineHeight);

                CharPositionInfo[] characters;
                // Nyní máme dostatek informací (v proměnných) k tomu, abychom mohli vyřešit vstupní text nulové délky (nebo NULL):
                int length = text?.Length ?? 0;
                if (length == 0)
                {
                    characters = new CharPositionInfo[] { new CharPositionInfo("", "", 0, 0, 0, null, new Rectangle(origin.X, origin.Y, 1, fontHeight)) };
                    // Vytvořit řádky? Vytvoříme => jeden řádek pro pole obsahující jeden znak:
                    if (parameters.CreateLineInfo)
                        LinePositionInfo.CreateLine(characters);
                    return characters;
                }

                // Standardní postup pro neprázdný vstupní text:
                RectangleF layoutBounds = _LayoutBounds;
                char cr = CrLf[0];
                char lf = CrLf[1];
                characters = new CharPositionInfo[length];
                int targetIndex = 0;
                int currentX = origin.X;
                int currentY = origin.Y;
                float locationX = currentX;
                int rowIndex = 0;
                int charIndex = 0;
                bool skipNextLf;
                string prevChar = "";
                string currChar;
                for (int i = 0; i < length; i++)
                {
                    char c = text[i];
                    skipNextLf = false;
                    if ((c == cr || c == lf) && multiline)
                    {   // Znak CR (za ním typicky následuje LF), anebo samotné LF (zpracováváme NE-normalizovaný string), a je povolen Multiline => skočíme na nový řádek (zpracuji Cr + Lf je jako jednu pozici):
                        currChar = CRLF;
                        Rectangle textBounds = new Rectangle(currentX, currentY, 1, fontHeight);
                        currentX = origin.X;
                        currentY = currentY + rowHeight;
                        locationX = currentX;

                        // Uložíme CRLF jako poslední znak v řádku, a jdeme dál, na novém řádku, od pozice 0:
                        characters[targetIndex] = new CharPositionInfo(currChar, currChar, targetIndex, rowIndex, charIndex, null, textBounds);
                        rowIndex++;
                        charIndex = 0;

                        targetIndex++;
                        prevChar = "";

                        // Pokud nyní máme CR, pak zajistíme přeskočení následujícího LF:
                        skipNextLf = (c == cr);
                    }
                    else
                    {   // Jiný znak než CrLf, anebo (CrLf v režimu Ne-Multiline):
                        if (c == cr || c == lf)
                        {   // Máme tu CR nebo LF, ale NE-máme to řešit jako NewLine => vyřešíme to jako Mezeru:
                            currChar = SPACE;
                            // Pokud nyní máme CR, pak zajistíme přeskočení následujícího LF:
                            skipNextLf = (c == cr);
                        }
                        else
                        {
                            currChar = c.ToString();
                        }
                        string visibleChar = (hasPasswordChar ? passwordChar : currChar);
                        string charKey = prevChar + visibleChar;

                        // Prostor obsazený znakem, relativně k předchozímu znaku:
                        RectangleF currBounds = _GetLastCharBounds(graphics, font, charKey, layoutBounds);       // Vrátí souřadnici posledního znaku v "charKey", tedy první nebo druhý znak (více znaků tam nikdy není). Souřadnice jsou relativně k layoutBounds, tedy k bodu (0,0). Offset _FirstOffsetX je již aplikován.

                        // Zde bychom měli řešit Auto-Wrap line, pokud je to povoleno, požadováno, pokud je to možné a potřebné (Right znaku je za Width prostoru), přičemž Mezery nebudeme dávat na začátek nového řádku:
                        if (multiline && right > 0f && charIndex > 0 && ((locationX + currBounds.Right + 1f) >= right) && visibleChar != SPACE)
                            WrapToNewLine(characters, targetIndex, visibleChar, wrapWord, originX, rowHeight, ref rowIndex, ref charIndex, ref currentX, ref currentY, ref locationX, ref currBounds);

                        // Souřadnice znaku - tak, aby byl vypsán do požadovaného prostoru:
                        float charX = locationX + currBounds.X;
                        PointF textLocation = new PointF(charX - _FirstOffsetX, currentY);                       // Offset (_FirstOffsetX) se poprvé odečetl v metodě _GetLastCharBounds(), abychom měli souřadnice bodu odpovídající požadovanému bodu. Podruhé odečtu offset (_GetLastCharBounds) tady, tedy předsadím X doleva, a pak při finálním vykreslení znaku se ten znak dostane na požadovanou pozici...
                        locationX = charX;

                        // Souřadnice textBounds = celočíselné, na sebe navazující:
                        int textR = (int)Math.Floor(charX + currBounds.Width);
                        Rectangle textBounds = new Rectangle(currentX, currentY, textR - currentX, fontHeight);
                        currentX = textR;

                        characters[targetIndex] = new CharPositionInfo(currChar, visibleChar, targetIndex, rowIndex, charIndex, textLocation, textBounds);
                        charIndex++;
                        targetIndex++;
                        prevChar = visibleChar;
                    }

                    // Pokud máme přeskočit následující LF, a za znakem CR následuje další znak, a je to znak LF, pak jde o standardní oddělovač řádků CrLf; ten jsme již zpracovali (jako jeden dvojznak) => následující LF přeskočíme:
                    if (skipNextLf && i < (length - 1) && text[i + 1] == lf)
                        i++;
                }

                // Nyní máme v poli result počet znaků (length), ale protože jsme mohli vynechat znaky LF, pak poslední prvky pole result mohou být null
                //  (vstupní znaky jsme načítali z indexu [i] = 0..length; ale ukládali jsme je do indexu targetIndex)
                if (targetIndex < length)
                {
                    CharPositionInfo[] target = new CharPositionInfo[targetIndex];
                    Array.Copy(characters, 0, target, 0, targetIndex);
                    characters = target;
                }

                // Vytvořit řádky?
                if (parameters.CreateLineInfo)
                {
                    var lines = characters.GroupBy(p => p.RowIndex);
                    LinePositionInfo prevLine = null;
                    foreach (var line in lines)
                        LinePositionInfo.CreateLine(line, ref prevLine);
                }

                return characters;
            }
            /// <summary>
            /// Vrátí výšku jednoho řádku textu v daném fontu
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="font"></param>
            /// <returns></returns>
            public int GetFontHeight(Graphics graphics, Font font)
            {
                LoadBasicFontData(graphics, font);

                return _FontHeight;
            }
            /// <summary>
            /// Metoda je volána v situaci, kdy je třeba zalomit řádek, protože aktuální znak přesahuje za pravý okraj prostoru.
            /// Řeší dva úkoly: 
            /// 1. Pokud je požadováno <paramref name="wrapWord"/>: Najít začátek slova na aktuálním řádku a pokud to má smysl, pak jej celé přesunout na nový řádek
            /// 2. Pokud se neprovádí <paramref name="wrapWord"/>, anebo nebylo nalezeno vhodné slovo k přesunu na další řádek, pak pouze přesune pointery tak, aby aktuální znak začal na novém řádku
            /// </summary>
            /// <param name="characters"></param>
            /// <param name="targetIndex"></param>
            /// <param name="currChar"></param>
            /// <param name="wrapWord"></param>
            /// <param name="originX"></param>
            /// <param name="rowIndex"></param>
            /// <param name="rowHeight"></param>
            /// <param name="charIndex"></param>
            /// <param name="currentX"></param>
            /// <param name="currentY"></param>
            /// <param name="locationX"></param>
            /// <param name="currBounds"></param>
            private void WrapToNewLine(CharPositionInfo[] characters, int targetIndex, string currChar, bool wrapWord, int originX, int rowHeight, ref int rowIndex, ref int charIndex, ref int currentX, ref int currentY, ref float locationX, ref RectangleF currBounds)
            {
                // Najít začátek slova, které by mělo být celé přesunuté na nový řádek:
                int wordBegin = -1;
                if (currChar != SPACE && charIndex > 0 && characters[targetIndex - 1].TextVisible != SPACE && wrapWord)
                {   // Pokud aktuální znak NENÍ mezera, a jsme na pozici v řádku větší než 0 a předešlý znak NENÍ mezera, a mají se zalamovat slova, 
                    // pak zkusíme najít začátek slova (wordBegin), které bude přesunuto na další řádek:
                    for (int i = targetIndex - 1; i >= 0; i--)
                    {   // Projdu znaky před aktuální pozici, a hledám pozici znaku, který je první nemezera za mezerou na pozici v řádku větší než 0:
                        var character = characters[i];
                        if (character.CharIndex <= 0) break;                                            // Pokud by slovo začínalo na pozici 0, pak jej nebudeme wrapovat na nový řádek, protože to nemá smysl.
                        if (character.TextVisible != SPACE && characters[i - 1].TextVisible == SPACE)   // Pokud znak [i] NENÍ mezera, a předchozí znak JE mezera, pak znak [i] zalomíme na nový řádek:
                        {
                            wordBegin = i;
                            break;
                        }
                    }
                }

                if (wordBegin > 0)
                {   // Přesunout nalezené slovo na další řádek, na pozici X = originX ... :
                    var wordFirst = characters[wordBegin];
                    Point logicalOffset = new Point(-wordFirst.CharIndex, 1);
                    PointF fontOffset = new PointF((float)originX - (wordFirst.TextLocation.HasValue ? wordFirst.TextLocation.Value.X + _FirstOffsetX : (float)wordFirst.TextBounds.X), rowHeight);
                    Point boundsOffset = new Point(originX - wordFirst.TextBounds.X, rowHeight);

                    for (int i = wordBegin; i < targetIndex; i++)
                        characters[i].Move(logicalOffset, fontOffset, boundsOffset);

                    // Připravit nové pozice pro aktuální znakm, podle pozice posledního znaku:
                    CharPositionInfo lastChar = characters[targetIndex - 1];
                    rowIndex = lastChar.RowIndex;
                    charIndex = lastChar.CharIndex + 1;
                    currentX = lastChar.TextBounds.Right;
                    currentY = lastChar.TextBounds.Y;
                    locationX = (lastChar.TextLocation.HasValue ? lastChar.TextLocation.Value.X + _FirstOffsetX : (float)lastChar.TextBounds.X);
                }
                else
                {   // Nepřesouváme slovo, pouze aktuální znak (currChar, který teprve budeme ukládat do pole characters) budeme pozicovat na nový řádek:
                    rowIndex++;
                    charIndex = 0;
                    currentX = originX;
                    currentY += rowHeight;
                    locationX = originX;// - _FirstOffsetX;
                    currBounds.X = 0f;
                }
            }
            /// <summary>
            /// Vrátí souřadnici posledního znaku v daném textu.
            /// Využívá data v dodané Dictionary (data tam hledá, nebo je vypočte a uloží)
            /// </summary>
            /// <param name="graphics">Grafika</param>
            /// <param name="font">Font</param>
            /// <param name="charKey">Znak nebo dva znaky pro měření</param>
            /// <param name="layoutBounds">Prostor pro výpočty, nijak nesouvisí s výsledným prostorem</param>
            /// <returns></returns>
            private RectangleF _GetLastCharBounds(Graphics graphics, Font font, string charKey, RectangleF layoutBounds)
            {
                RectangleF charBounds;
                if (!this._FontBounds.TryGetValue(charKey, out charBounds))
                {
                    charBounds = _CalculateLastCharBounds(graphics, font, charKey, layoutBounds);
                    this._FontBounds.Add(charKey, charBounds);
                }
                return charBounds;
            }
            /// <summary>
            /// Vrátí souřadnici posledního znaku v daném textu.
            /// Pokud je dodán pouze jeden znak, pak vrátí jeho souřadnice = počátek (0,0) a velikost daného znaku.
            /// Pokud je dodáno více znaků, pak vrátí souřadnice posledního z nich.
            /// </summary>
            /// <param name="graphics">Grafika</param>
            /// <param name="font">Font</param>
            /// <param name="text">Znak nebo dva znaky pro měření</param>
            /// <param name="layoutBounds">Prostor pro výpočty, nijak nesouvisí s výsledným prostorem</param>
            /// <returns></returns>
            private RectangleF _CalculateLastCharBounds(Graphics graphics, Font font, string text, RectangleF layoutBounds)
            {
                if (text == null || text.Length == 0) return new RectangleF(0f, 0f, 0f, _FontHeight);
                var charBounds = _CalculateAllCharBounds(graphics, font, text, layoutBounds);
                int length = charBounds.Length;
                RectangleF lastBounds = charBounds[length - 1];
                lastBounds.X = (length == 1 ? 0f : lastBounds.X - charBounds[length - 2].X);
                return lastBounds;
            }
            /// <summary>
            /// Metoda změří a vrátí souřadnice každého znaku v daném textu, vrací pole souřadnic v délce shodné jako délka textu.
            /// Pokud je na vstupu text null nebo délky 0, pak výstupem je pole s 1 prvek, jehož šířka == 0 a výška = výška řádku.
            /// </summary>
            /// <param name="graphics">Grafika</param>
            /// <param name="font">Font</param>
            /// <param name="text">Znak nebo dva znaky pro měření</param>
            /// <param name="layoutBounds">Prostor pro výpočty, nijak nesouvisí s výsledným prostorem</param>
            /// <returns></returns>
            private RectangleF[] _CalculateAllCharBounds(Graphics graphics, Font font, string text, RectangleF layoutBounds)
            {
                if (text == null || text.Length == 0) return new RectangleF[0];
                int length = text.Length;
                if (length >= 32) throw new ArgumentException("FontMeasureOneInfo._CalculateCharBounds(): text is too long. Max length is 31 characters.");

                CharacterRange[] ranges = new CharacterRange[length];
                for (int i = 0; i < length; i++)
                    ranges[i] = new CharacterRange(i, 1);

                RectangleF[] charBounds = new RectangleF[length];

                using (var stringFormat = FontManagerInfo.CreateNewEditorStringFormat())
                {
                    stringFormat.SetMeasurableCharacterRanges(ranges);
                    var charRanges = graphics.MeasureCharacterRanges(text, font, layoutBounds, stringFormat);
                    length = charRanges.Length;

                    for (int i = 0; i < length; i++)
                        charBounds[i] = charRanges[i].GetBounds(graphics);
                }
                return charBounds;
            }
            /// <summary>
            /// Prostor pro výpočty, nijak nesouvisí s výsledným prostorem
            /// </summary>
            private static RectangleF _LayoutBounds { get { return new RectangleF(0f, 0f, 500f, 50f); } }
            #endregion
            #region Základní data o fontu
            /// <summary>
            /// Načte základní data o fontu = statická
            /// </summary>
            /// <param name="graphics"></param>
            /// <param name="font"></param>
            private void LoadBasicFontData(Graphics graphics, Font font)
            {
                if (font == null || _HasBasicData) return;
                float h = font.GetHeight(graphics);
                _FontHeight = font.Height;
                _SpaceSize = graphics.MeasureString(" ", font);
                _FontSize = font.Size;
                _SizeUnit = font.Unit;
                _FirstOffsetX = _CalculateAllCharBounds(graphics, font, "AA", _LayoutBounds)[0].X;
                _HasBasicData = true;
            }
            private bool _HasBasicData = false;
            private int _FontHeight;
            private SizeF _SpaceSize;
            private float _FontSize;
            private GraphicsUnit _SizeUnit;
            private float _FirstOffsetX;
            #endregion
        }
        #endregion
    }
    #region class CharPositionInfo :  Údaj o jednom znaku a jeho pozici, LinePositionInfo : Údaje o jednom řádku skládajícím se z několika znaků CharPositionInfo
    /// <summary>
    /// <see cref="CharPositionInfo"/> : Údaj o jednom znaku a jeho pozici
    /// </summary>
    public class CharPositionInfo : IFontPositionInfo
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="textVisible"></param>
        /// <param name="index"></param>
        /// <param name="rowIndex"></param>
        /// <param name="charIndex"></param>
        /// <param name="textLocation"></param>
        /// <param name="textBounds"></param>
        public CharPositionInfo(string text, string textVisible, int index, int rowIndex, int charIndex, PointF? textLocation, Rectangle textBounds)
        {
            this.Text = text;
            this.TextVisible = textVisible;
            this.Index = index;
            this.RowIndex = rowIndex;
            this.CharIndex = charIndex;
            this.TextLocation = textLocation;
            this.TextBounds = textBounds;
        }
        /// <summary>
        /// Vytvoří a vrátí pole <see cref="CharPositionInfo"/> pro zadaný text a virtuální souřadnice znaků, pouze pro testovací účely.
        /// Souřadnice znaků jsou v jednom nebo více řádcích, velikost jednoho znaku je fixní, daná parametrem <paramref name="charSize"/>.
        /// Pokud je zadán parametr <paramref name="maxX"/>, pak je text rozdělen do více řádků v šířce nejvýše zadané.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="charSize"></param>
        /// <param name="maxX"></param>
        /// <returns></returns>
        internal static CharPositionInfo[] CreateTestChars(string text, SizeF charSize, float? maxX = null)
        {
            if (text == null) return null;
            int length = text.Length;
            CharPositionInfo[] result = new CharPositionInfo[length];
            if (length == 0) return result;                      // Zkratka ven

            int rowIndex = 0;
            int charIndex = 0;
            float locationX = 0f;
            float locationY = 0f;
            float sizeW = charSize.Width;
            if (sizeW < 1f) sizeW = 1f;
            float sizeH = charSize.Height;
            if (sizeH < 1f) sizeH = 1f;
            int boundsX = 0;
            int boundsY = 0;
            int boundsB = (int)Math.Round(locationY + sizeH, 0);
            bool hasMaxX = (maxX.HasValue && maxX.Value > 0f);
            for (int i = 0; i < length; i++)
            {
                char item = text[i];
                float locationR = locationX + sizeW;
                if (hasMaxX && locationX > 0f && locationR > maxX.Value)
                {   // Přejdeme na nový řádek:
                    rowIndex++;
                    charIndex = 0;
                    locationX = 0f;
                    locationY = locationY + sizeH;
                    boundsX = 0;
                    boundsY = (int)Math.Round(locationY, 0);
                    boundsB = (int)Math.Round(locationY + sizeH, 0);
                }
                int boundsR = (int)Math.Round(locationR, 0);
                PointF? textLocation = new PointF(locationX, locationY);
                Rectangle textBounds = new Rectangle(boundsX, boundsY, boundsR - boundsX, boundsB - boundsY);
                string currChar = item.ToString();
                result[i] = new CharPositionInfo(currChar, currChar, i, rowIndex, charIndex, textLocation, textBounds);

                locationX = locationR;
                boundsX = boundsR;
            }
            return result;
        }
        /// <summary>
        /// Z this znaku odebere referenci na <see cref="Line"/>, a tento objekt zruší.
        /// </summary>
        public void RemoveLine()
        {
            var line = this._Line;
            if (line != null) line.RemoveLine();
            this._Line = null;
        }
        /// <summary>
        /// Z kolekce znaků vrátí čistý text
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static string GetText(IEnumerable<CharPositionInfo> positions)
        {
            if (positions == null) return "";
            StringBuilder sb = new StringBuilder();
            foreach (var position in positions.Where(p => p.HasText))
                sb.Append(position.Text);
            return sb.ToString();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Text: '{Text}'; Row: [{RowIndex}]; Char: [{CharIndex}]; Location: {TextLocation}; Bounds: {TextBounds}";
        }
        /// <summary>
        /// Text znaku - datový (reálně ukládaný)
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Text znaku zobrazovaný. 
        /// Od <see cref="Text"/> se liší tehdy, když se zobrazuje <see cref="TextEdit.PasswordChar"/>.
        /// </summary>
        public string TextVisible { get; private set; }
        /// <summary>
        /// Znak. Pokud <see cref="Text"/> je null nebo délky 0, pak je zde char(0). Jinak je zde první znak textu. V případě oddělovače řádků (CrLf) tedy CR.
        /// </summary>
        public char Content { get { return (Text == null || Text.Length == 0) ? (char)0 : Text[0]; } }
        /// <summary>
        /// Obsahuje true pokud <see cref="Text"/> má význam (není NULL a délka je větší než 0)
        /// </summary>
        public bool HasText { get { return (Text != null && Text.Length > 0); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="Text"/> se má reálně vypisovat do grafického prostoru (text je zadán, není prázdný, a máme souřadnice znaku pro vypsání písmena).
        /// Pokud je <see cref="IsDrawText"/> = false, nemá cenu vypisovat <see cref="Graphics.DrawString(string, Font, Brush, float, float, StringFormat)"/>, 
        /// ale pozadí znaku <see cref="TextBounds"/> je možné vyplnit barvou.
        /// </summary>
        public bool IsDrawText { get { return (!String.IsNullOrEmpty(Text) && TextLocation.HasValue); } }
        /// <summary>
        /// Index v textu, počínaje 0. Dvojznak CrLf je uložen v jedné instanci.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Index řádku, počínaje 0.
        /// </summary>
        public int RowIndex { get; private set; }
        /// <summary>
        /// Index znaku v rámci řádku, počínaje 0, kontinuální řada. Znak CrLf je poslední na řádku, následující znak na novém řádku má <see cref="CharIndex"/> = 0.
        /// </summary>
        public int CharIndex { get; private set; }
        /// <summary>
        /// Souřadnice bodu, na který se má vypsat znak <see cref="Text"/> v metodě <see cref="Graphics.DrawString(string, Font, Brush, PointF, StringFormat)"/>.
        /// Znak potom bude zobrazen víceméně přesně v prostoru <see cref="TextBounds"/>.
        /// <para/>
        /// Pokud nemá hodnotu, pak neobshauje nic co by se mělo vykreslovat (CR, LF, TAB) 
        /// </summary>
        public PointF? TextLocation { get; private set; }
        /// <summary>
        /// Souřadnice prostoru, ve kterém bude znak zobrazen. POZOR: z důvodů, které zná pouze Bill Gates, se NESHODUJE souřadnice <see cref="TextLocation"/> a prostor <see cref="TextBounds"/>.
        /// Tento prostor je celočíselný, a jednotlivé znaky za sebou jdoucí mají tento prostor na sebe v ose X těsně navazující.
        /// </summary>
        public Rectangle TextBounds { get; private set; }
        /// <summary>
        /// Reference na instanci řádku
        /// </summary>
        public LinePositionInfo Line { get { return _Line; } }
        /// <summary>
        /// <see cref="IFontPositionInfo.Line"/>
        /// </summary>
        LinePositionInfo IFontPositionInfo.Line { get { return _Line; } set { _Line = value; } }
        /// <summary>
        /// Proměnná pro <see cref="Line"/>
        /// </summary>
        private LinePositionInfo _Line;
        #endregion
        #region Přemístění pozice znaku
        /// <summary>
        /// Přesune znak na jinou pozici
        /// </summary>
        /// <param name="rowOffset"></param>
        /// <param name="charOffset"></param>
        /// <param name="fontOffset"></param>
        /// <param name="boundsOffset"></param>
        public void Move(int rowOffset, int charOffset, PointF fontOffset, Point boundsOffset)
        {
            RowIndex += rowOffset;
            CharIndex += charOffset;
            if (TextLocation.HasValue) TextLocation = new PointF(TextLocation.Value.X + fontOffset.X, TextLocation.Value.Y + fontOffset.Y);
            TextBounds = new Rectangle(TextBounds.X + boundsOffset.X, TextBounds.Y + boundsOffset.Y, TextBounds.Width, TextBounds.Height);
        }
        /// <summary>
        /// Přesune znak na jinou pozici
        /// </summary>
        /// <param name="logicalOffset">Posun logické souřadnice (<see cref="RowIndex"/> a <see cref="CharIndex"/>)</param>
        /// <param name="fontOffset">Posun pozice písmena <see cref="TextLocation"/></param>
        /// <param name="boundsOffset">Posun pozice prostoru <see cref="TextBounds"/></param>
        public void Move(Point logicalOffset, PointF fontOffset, Point boundsOffset)
        {
            RowIndex += logicalOffset.Y;
            CharIndex += logicalOffset.X;
            if (TextLocation.HasValue) TextLocation = new PointF(TextLocation.Value.X + (float)fontOffset.X, TextLocation.Value.Y + (float)fontOffset.Y);
            TextBounds = new Rectangle(TextBounds.X + boundsOffset.X, TextBounds.Y + boundsOffset.Y, TextBounds.Width, TextBounds.Height);
        }
        #endregion
        #region Vykreslení textu na pozici
        /// <summary>
        /// Vykreslí aktuální znak do dané grafiky
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="fontInfo"></param>
        /// <param name="textBounds">Absolutní souřadnice okénka pro vypsání textu</param>
        /// <param name="textShift">Posun textu vůči počátku prostoru <paramref name="textBounds"/> = scrollování.
        /// Posunutí obsahu textu proti souřadnici.
        /// Zde je uložena souřadnice relativního bodu v textu, který je zobrazen v levém horním rohu textovho okénka.
        /// Tedy: pokud je <paramref name="textShift"/> = (25, 0), pak v TextBoxu bude zobrazen první řádek (Y=0), ale až např. čtvrtý znak, jehož X = 25.
        /// Poznámka: souřadnice v <paramref name="textShift"/> by neměly být záporné, protože pak by obsah textu byl zobrazen odsunutý doprava/dolů.
        /// </param>
        /// <param name="backColor"></param>
        /// <param name="backBrush"></param>
        /// <param name="fontColor"></param>
        /// <param name="fontBrush"></param>
        public void DrawText(Graphics graphics, FontInfo fontInfo, Rectangle textBounds, Point? textShift, Color? backColor = null, Brush backBrush = null, Color? fontColor = null, Brush fontBrush = null)
        {
            // Posun souřadnic znaku (ty jsou uloženy v koordinátech 0/0) do souřadnic absolutních - včetně akceptování hodnoty Shiftu:
            Point offset = textBounds.Location;
            if (textShift.HasValue && !textShift.Value.IsEmpty) offset = offset.Sub(textShift.Value);

            // Prostor znaku - jednak pro kreslení pozadí, jednak pro test viditelnosti = který znak není ani trochu vidět, nebude se ani trochu kreslit:
            Rectangle backBounds = this.TextBounds.Add(offset);
            if (!textBounds.IntersectsWith(backBounds)) return;

            // Pozadí:
            if (backColor.HasValue || backBrush != null)
            {
                var brush = (backBrush != null ? backBrush : Skin.Brush(backColor.Value));
                using (GPainter.GraphicsUseSharp(graphics))
                    graphics.FillRectangle(brush, backBounds);
            }

            // Znak:
            if (this.IsDrawText && (fontColor.HasValue || fontBrush != null))
            {
                var brush = (fontBrush != null ? fontBrush : Skin.Brush(fontColor.Value));
                PointF fontLocation = this.TextLocation.Value.Add(offset);
                graphics.DrawString(this.TextVisible, fontInfo.Font, brush, fontLocation, FontManagerInfo.EditorStringFormat);
            }
        }
        /// <summary>
        /// Vrátí true, pokud this znak má být viditelný v daném prostoru.
        /// </summary>
        /// <param name="visibleBounds"></param>
        /// <returns></returns>
        public bool IsVisibleInBounds(Rectangle visibleBounds)
        {
            return (this.IsDrawText && visibleBounds.IntersectsWith(this.TextBounds));
        }
        #endregion
    }
    /// <summary>
    /// Interface pro internal přístup do <see cref="CharPositionInfo"/>
    /// </summary>
    internal interface IFontPositionInfo
    {
        /// <summary>
        /// Reference na instanci řádku, lze ji i setovat
        /// </summary>
        LinePositionInfo Line { get; set; }
    }
    /// <summary>
    /// Třída popisující jeden řádek (pozice, znaky na něm). Řádek obsahuje i referenci na první a poslední znak řádku, a na předchozí a následující řádek.
    /// Řádek vždy obsahuje přinejmenším jeden znak (i kdyby to bylo CrLf).
    /// </summary>
    public class LinePositionInfo
    {
        #region Tvorba z pole znaků a základní property
        /// <summary>
        /// Vytvoří řádek pro dané znaky v řádku.
        /// Číslo řádku <see cref="LinePositionInfo.RowIndex"/> přebírá z prvního znaku.
        /// Vypočítá souhrnný prostor <see cref="LinePositionInfo.TextBounds"/> z prostoru jednotlivých znaků.
        /// Do jednotlivých znaků vloží referenci na právě vytvořený řádek do <see cref="CharPositionInfo.Line"/>.
        /// </summary>
        /// <param name="chars">Znaky v řádku</param>
        /// <returns></returns>
        public static LinePositionInfo CreateLine(IEnumerable<CharPositionInfo> chars)
        {
            LinePositionInfo prevLine = null;
            return CreateLine(chars, ref prevLine);
        }
        /// <summary>
        /// Vytvoří řádek pro dané znaky v řádku.
        /// Číslo řádku <see cref="LinePositionInfo.RowIndex"/> přebírá z prvního znaku.
        /// Vypočítá souhrnný prostor <see cref="LinePositionInfo.TextBounds"/> z prostoru jednotlivých znaků.
        /// Do jednotlivých znaků vloží referenci na právě vytvořený řádek do <see cref="CharPositionInfo.Line"/>.
        /// </summary>
        /// <param name="chars">Znaky v řádku</param>
        /// <param name="prevLine">Předešlý řádek</param>
        /// <returns></returns>
        public static LinePositionInfo CreateLine(IEnumerable<CharPositionInfo> chars, ref LinePositionInfo prevLine)
        {
            if (chars == null) return null;
            CharPositionInfo[] characters = chars.ToArray();
            if (characters.Length == 0) return null;
            int index = characters[0].RowIndex;
            Rectangle textBounds = DrawingExtensions.SummaryVisibleRectangle(characters.Select(p => p.TextBounds));
            return new LinePositionInfo(index, characters, textBounds, ref prevLine);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="characters"></param>
        /// <param name="textBounds">Souřadnice řádku</param>
        /// <param name="prevLine">Předešlý řádek</param>
        private LinePositionInfo(int index, CharPositionInfo[] characters, Rectangle textBounds, ref LinePositionInfo prevLine)
        {
            this.RowIndex = index;
            this.Characters = characters;
            this.TextBounds = textBounds;

            foreach (var character in Characters)
                ((IFontPositionInfo)character).Line = this;

            // Posuvný registr PrevLine - this - NextLine:
            if (prevLine != null)
                prevLine.NextLine = this;
            this.PrevLine = prevLine;
            prevLine = this;
        }
        /// <summary>
        /// Z this instance odebere všechny reference na okolní instance.
        /// </summary>
        public void RemoveLine()
        {
            this.PrevLine = null;
            this.Characters = null;
            this.NextLine = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = CharPositionInfo.GetText(Characters);
            return $"Text: \"{text}\"; Row: [{RowIndex}]; Bounds: {TextBounds}";
        }
        /// <summary>
        /// Index řádku, počínaje 0 = odpovídá hodnotě <see cref="CharPositionInfo.RowIndex"/>
        /// </summary>
        public int RowIndex { get; private set; }
        /// <summary>
        /// Pole znaků v řádku. Nikdy není null, vždy obsahuje alespoň jeden prvek.
        /// Pozor: po rozpuštění řádku metodou <see cref="RemoveLine"/> je null, ale po této metodě se instance celá zahazuje a není odkud ji používat.
        /// </summary>
        public CharPositionInfo[] Characters { get; private set; }
        /// <summary>
        /// Počet znaků v tomto řádku. Vždy je větší než 0, i na prázdném řádku se nachází přinejmenším CrLf.
        /// </summary>
        public int CharactersCount { get { return Characters.Length; } }
        /// <summary>
        /// Souřadnice prostoru, ve kterém bude celý řádek zobrazen = souhrn souřadnic <see cref="CharPositionInfo.TextBounds"/> ze všech znaků v řádku.
        /// Tento prostor je celočíselný, a jednotlivé řádky za sebou jdoucí mají tento prostor na sebe v ose Y těsně navazující.
        /// </summary>
        public Rectangle TextBounds { get; private set; }
        /// <summary>
        /// První znak na řádku. Nikdy není null, i na prázdném řádku se nachází přinejmenším CrLf.
        /// </summary>
        public CharPositionInfo FirstChar { get { return Characters[0]; } }
        /// <summary>
        /// Poslední znak na řádku. Nikdy není null, i na prázdném řádku se nachází přinejmenším CrLf.
        /// </summary>
        public CharPositionInfo LastChar { get { return Characters[CharactersCount - 1]; } }
        /// <summary>
        /// Předešlý řádek, u prvního řádku je null
        /// </summary>
        public LinePositionInfo PrevLine { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud za tímto řádkem existuje další řádek v <see cref="PrevLine"/>
        /// </summary>
        public bool HasPrevLine { get { return (this.PrevLine != null); } }
        /// <summary>
        /// Následující řádek, u posledního řádku je null
        /// </summary>
        public LinePositionInfo NextLine { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud za tímto řádkem existuje další řádek v <see cref="NextLine"/>
        /// </summary>
        public bool HasNextLine { get { return (this.NextLine != null); } }
        #endregion
        #region Vyhledání řádku a znaku podle pozice
        /// <summary>
        /// Najde a vrátí znak, který nejblíže odpovídá danému bodu
        /// </summary>
        /// <param name="characters">Pole znaků textu</param>
        /// <param name="point">Zadaný bod, k němuž hledáme znak na nejbližší pozici</param>
        /// <param name="isRightSide">Out: zadaný bod leží v pravé polovině nalezeného znaku</param>
        /// <returns></returns>
        public static CharPositionInfo SearchCharacterByPosition(CharPositionInfo[] characters, Point point, out bool isRightSide)
        {
            isRightSide = false;

            if (characters == null) return null;
            int charCount = characters.Length;
            if (charCount == 0) return null;

            // 1. Najdeme řádek:
            int searchY = point.Y;
            LinePositionInfo lineFound = null;
            LinePositionInfo lineTest = characters[0].Line;
            if (!lineTest.HasNextLine || searchY < lineTest.TextBounds.Bottom)
            {   // Zkratka:
                lineFound = lineTest;
            }
            else
            {   // Plné hledání:
                while (lineTest != null)
                {   // Projdu všechny řádky od výchozího až do konce:
                    int bottomY = lineTest.TextBounds.Bottom;
                    // Řádek je vyhovující tehdy, když je poslední, anebo odpovídá souřadnice Y:
                    bool match = (!lineTest.HasNextLine || searchY < bottomY);
                    if (!match && lineTest.NextLine.TextBounds.Y > bottomY)
                    {   // Pokud řádek lineTest nevyhovuje, značí to mimo jiné to, že za ním existuje další řádek (match = !lineTest.HasNextLine...).
                        // A pokud následující řádek začíná na vyšší pozici Y, než na které končí aktuální řádek, pak bod searchY může být v té mezeře a blíže k lineTest:
                        int halfY = bottomY + ((lineTest.NextLine.TextBounds.Y - bottomY) / 2);
                        match = (searchY < halfY);
                    }
                    if (match)
                    {
                        lineFound = lineTest;
                        break;
                    }
                    lineTest = lineTest.NextLine;
                }
                // Plné hledání vždy najde nějaký řádek do lineFound - i když by to byl ten poslední!
            }

            // 2. V řádku najdeme znak:
            CharPositionInfo[] chars = lineFound.Characters;
            CharPositionInfo charInfo = null;
            if (charCount == 1 || point.X < chars[0].TextBounds.Right)
            {   // Zkratka:
                charInfo = chars[0];
            }
            else
            {   // Plné hledání:
                for (int i = 0; i < charCount; i++)
                {   // Projdu všechny znaky:
                    charInfo = chars[i];
                    if (i == (charCount - 1)) break;                 // Pokud jsem dosud nenašel, a mám poslední znak, pak on je tím hledaným prvkem!
                    int x = charInfo.TextBounds.Right;               // Pravé X našeho znaku
                    int xn = chars[i + 1].TextBounds.Left;           // Levé X následujícího znaku [+1]
                    if (xn > x) x += ((xn - x) / 2);                 // Pokud následující znak je pod koncem našeho znaku (tj. je mezi nimi mezera), pak zpracuji i levou polovinu mezery!
                    if (point.X < x) break;                          // Pokud hledaný bod leží před pravým okrajem znaku (případně včetně půlmezery za naším znakem), pak hledaný znak je on
                }
            }

            // 3. Pravá polovina znaku:
            int half = charInfo.TextBounds.X + (charInfo.TextBounds.Width / 2);
            isRightSide = (point.X >= half);

            return charInfo;
        }
        #endregion
    }
    #endregion
    #region class FontMeasureParams : Parametry pro funkce měření pozice znaku v metodě FontMeasureInfo.GetCharInfo(string, Graphics, Font, FontMeasureParams)
    /// <summary>
    /// <see cref="FontMeasureParams"/> : Parametry pro funkce měření pozice znaku v metodě <see cref="FontManagerInfo.GetCharInfo(string, Graphics, Font, FontMeasureParams)"/>
    /// </summary>
    public class FontMeasureParams
    {
        /// <summary>
        /// Souřadnice prvního znaku (Top,Left), od něj budou znaky umisťovány
        /// </summary>
        public Point? Origin { get; set; }
        /// <summary>
        /// Zástupný znak pro zobrazování hesla, null = default = běžné zobrazení
        /// </summary>
        public char? PasswordChar { get; set; }
        /// <summary>
        /// Povoluje se dělení vstupního textu na řádky.
        /// Default = false : celý vstupní text je povinně umístěn do jedné řádky, i kdyby obsahoval CrLf, i kdyby byla zadána šířka <see cref="Width"/> a požadováno <see cref="WrapWord"/>. 
        /// Namísto znaku CrLf bude zobrazena mezera.
        /// Hodnota true : dělit text na řádky = znak CrLf způsobí odskočení na nový řádek, při zadání šířky <see cref="Width"/> bude text zalamován, při požadavku <see cref="WrapWord"/> budou zalamována celá slova.
        /// </summary>
        public bool Multiline { get; set; }
        /// <summary>
        /// Maximální šířka řádku v pixelech pro automatické řádkování. Pokud by znak byť jen zčásti přesahoval nad tuto šířku, bude umístěn na nový řádek.
        /// Podle parametru <see cref="WrapWord"/> může být na nový řádek umístěno celé poslední slovo.
        /// Pokud bude <see cref="Width"/> = null, nebude se automaticky zalamovat.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Při zalamování textu do nového řádku zalamovat celá slova, ne jen poslední přesahující znak.
        /// </summary>
        public bool WrapWord { get; set; }
        /// <summary>
        /// Výška řádku relativně k fontu. Null = 1 = dle předpisu fontu.
        /// </summary>
        public float? LineHeightRatio { get; set; }
        /// <summary>
        /// Přídavek k výšce řádku v pixelech, default = 0
        /// </summary>
        public float? LineHeightAdd { get; set; }
        /// <summary>
        /// Vygenerovat i objekty <see cref="LinePositionInfo"/> pro celé řádky textu.
        /// Pokud bude true, bude vytvořena instance <see cref="LinePositionInfo"/> pro každý jeden řádek, a reference na tento objekt bude uložena do <see cref="CharPositionInfo.Line"/>.
        /// Pokud bude false, objekty <see cref="LinePositionInfo"/> se nebudou vytvářet (úspora paměti).
        /// </summary>
        public bool CreateLineInfo { get; set; }
    }
    #endregion
}
