using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    [IsMainForm("Testy vykreslování textu", MainFormMode.Default, 4)]
    public partial class TestFontForm : Form
    {
        public TestFontForm()
        {
            InitializeComponent();
            InitEvents();
        }
        protected void InitEvents()
        {
            _InputText.Text = "Cokoliv do tohoto políčka vepíšeme, to se vypíše v dolních polích";
            // _InputText.Text = "A";
            _InputText.TextChanged += _InputText_TextChanged;
            _Panel1.Resize += _Panel1_Resize;
            _Panel1.Paint += _Panel1_Paint;
            _Panel2.Resize += _Panel2_Resize;
            _Panel2.Paint += _Panel2_Paint;
        }
        private void _InputText_TextChanged(object sender, EventArgs e)
        {
            this._Panel1.Invalidate();
            this._Panel2.Invalidate();
        }
        private void _Panel1_Resize(object sender, EventArgs e)
        {
            this._Panel1.Invalidate();
        }
        private void _Panel2_Resize(object sender, EventArgs e)
        {
            this._Panel2.Invalidate();
        }
        private void _Panel1_Paint(object sender, PaintEventArgs e)
        {
            Rectangle panelBounds = _Panel1.ClientRectangle;
            Rectangle bounds = new Rectangle(panelBounds.X + 12, panelBounds.Y + 6, panelBounds.Width - 24, panelBounds.Height - 12);
            e.Graphics.FillRectangle(SystemBrushes.ControlLight, bounds);

            Brush brush = SystemBrushes.ControlText;
            string text = _InputText.Text;
            using (var font = CreateFont())
            {
                bounds = new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6);
                PrepareGraphics(e.Graphics, bounds);
                using (StringFormat stringFormat = StringFormat.GenericTypographic.Clone() as StringFormat)   // new StringFormat(StringFormatFlags.NoWrap))
                {
                    e.Graphics.DrawString(text, font, brush, bounds.Location, stringFormat);
                }
            }
        }
        private void _Panel2_Paint(object sender, PaintEventArgs e)
        {
            Rectangle panelBounds = _Panel2.ClientRectangle;
            Rectangle bounds = new Rectangle(panelBounds.X + 12, panelBounds.Y + 6, panelBounds.Width - 24, panelBounds.Height - 12);
            e.Graphics.FillRectangle(SystemBrushes.ControlLight, bounds);

            Brush brush = SystemBrushes.ControlText;
            string text = _InputText.Text;
            Brush[] backBrushes = new Brush[] { new SolidBrush(Color.FromArgb(224, 255, 255)), new SolidBrush(Color.FromArgb(240, 225, 255)), new SolidBrush(Color.FromArgb(200, 255, 200)) };
                
            using (var font = CreateFont())
            {
                PrepareGraphics(e.Graphics, bounds);

                int x = bounds.X + 3;
                int y = bounds.Y + 3;
                FontMeasureParams parameters = new FontMeasureParams() { Origin = new Point(x,y), LineHeightRatio = 1.25f, WrapWord = true, Width = bounds.Width - 6, Multiline = true };
                var characters = FontManagerInfo.GetCharInfo(text, e.Graphics, font, parameters);

                using (StringFormat stringFormat = FontManagerInfo.CreateNewStandardStringFormat())
                {
                    foreach (var character in characters)
                    {
                        if ("AEIOUYaeiouy".IndexOf(character.Text) >= 0)
                            e.Graphics.FillRectangle(backBrushes[0], character.TextBounds);
                        else if (" ".IndexOf(character.Text) >= 0)
                            e.Graphics.FillRectangle(backBrushes[2], character.TextBounds);
                        else
                            e.Graphics.FillRectangle(backBrushes[1], character.TextBounds);
                    }
                    foreach (var character in characters)
                    {
                        if (character.TextLocation.HasValue)
                            e.Graphics.DrawString(character.Text.ToString(), font, brush, character.TextLocation.Value, stringFormat);
                    }
                }
            }
        }
        private static Font CreateFont()
        {
            return new Font(FontFamily.GenericSerif, 12f, FontStyle.Regular);
        }
        private static void PrepareGraphics(Graphics graphics, Rectangle? bounds)
        {
            //    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;       // Na Font nemá vliv, ovlivní kreslení Rectangle a Line
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;          // Ovlivní kreslení Fontu

            if (bounds.HasValue && bounds.Value.Width > 0 && bounds.Value.Height > 0)
                graphics.SetClip(bounds.Value);
        }
    }
    public class PanelDBF : Panel
    {
        public PanelDBF()
        {
            this.DoubleBuffered = true;
        }
    }



    /// <summary>
    /// <see cref="FontManagerInfo"/> : dokáže určit souřadnice znaků v konkrétním textu pro vykreslování tohoto textu do grafiky "per znak". Vhodné pro textový editor.
    /// </summary>
    internal class FontManagerInfo
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
            __FormatFlags = StringFormat.GenericTypographic.FormatFlags | StringFormatFlags.MeasureTrailingSpaces;
            CR = "\r";
            LF = "\n";
            CRLF = CR + LF;
            CrLf = CRLF.ToCharArray();
            SPACE = " ";
        }
        private static FontManagerInfo __Current = null;
        private static object __Locker = new object();
        private Dictionary<string, FontManagerItem> __FontInfos;
        private StringFormatFlags __FormatFlags;
        private StringFormat __StandardStringFormat;
        #endregion
        #region Změření znaků
        /// <summary>
        /// Vrátí informace o souřadnicích znaků (v daném textu, fontu, grafice, dle parametrů)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static FontPositionInfo[] GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
        {
            if (text == null || graphics == null || font == null) return null;
            int length = text.Length;
            if (length == 0) return new FontPositionInfo[0];

            string fontKey = FontManagerItem.GetFontKey(font);
            FontManagerItem fontInfo = Current._GetFontInfo(fontKey);
            return fontInfo.GetCharInfo(text, graphics, font, parameters);
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
        /// Obsahuje permanentní instanci StringFormat používanou pro rychlou práci s textem.
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
        /// Vygeneruje a vrátí new instanci <see cref="StringFormat"/> pro obecné použití při práci s textem
        /// </summary>
        /// <returns></returns>
        public static StringFormat CreateNewStandardStringFormat()
        {
            var ff = Current.__FormatFlags;
            StringFormat stringFormat = new StringFormat(ff);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.Trimming = StringTrimming.EllipsisCharacter;
            return stringFormat;
        }
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
        protected static string CR;
        protected static string LF;
        protected static string CRLF;
        protected static char[] CrLf;
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
            /// <param name="font"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public FontPositionInfo[] GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
            {
                LoadBasicFontData(graphics, font);

                int length = text.Length;
                FontPositionInfo[] characters = new FontPositionInfo[length];
                bool multiline = parameters?.Multiline ?? false;
                Point origin = parameters?.Origin ?? Point.Empty;
                float paramWidth = (float)(parameters?.Width ?? 0);
                float paramLineHeightRatio = (parameters?.LineHeightRatio ?? 0f);
                float paramLineHeightAdd = (parameters?.LineHeightAdd ?? 0f);

                int originX = origin.X;
                float right = (paramWidth > 0f ? (originX + paramWidth) : 0);
                bool wrapWord = (parameters?.WrapWord ?? false);
                float lineHeight = _FontHeight;
                if (paramLineHeightRatio > 0f) lineHeight = lineHeight * paramLineHeightRatio;
                lineHeight = lineHeight + paramLineHeightAdd;
                int rowHeight = (int)Math.Ceiling(lineHeight);

                int currentX = origin.X;
                int currentY = origin.Y;
                float locationX = currentX;
                RectangleF layoutBounds = _LayoutBounds;
                string prevChar = "";
                char cr = CrLf[0];
                char lf = CrLf[1];
                int targetIndex = 0;
                int rowIndex = 0;
                int charIndex = 0;
                string currChar;
                bool skipNextLf;
                for (int i = 0; i < length; i++)
                {
                    char c = text[i];
                    skipNextLf = false;
                    if ((c == cr || c == lf) && multiline)
                    {   // Znak CR (za ním typicky následuje LF), anebo samotné LF (zpracováváme NE-normalizovaný string), a je povolen Multiline => skočíme na nový řádek (zpracuji Cr + Lf je jako jednu pozici):
                        currChar = CRLF;
                        Rectangle textBounds = new Rectangle(currentX, currentY, 1, _FontHeight);
                        currentX = origin.X;
                        currentY = currentY + rowHeight;
                        locationX = currentX;

                        // Uložíme CRLF jako poslední znak v řádku, a jdeme dál, na novém řádku, od pozice 0:
                        characters[targetIndex] = new FontPositionInfo(currChar, rowIndex, charIndex, null, textBounds);
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
                        string charKey = prevChar + currChar;

                        // Prostor obsazený znakem, relativně k předchozímu znaku:
                        RectangleF currBounds = _GetLastCharBounds(graphics, font, charKey, layoutBounds);       // Vrátí souřadnici posledního znaku v "charKey", tedy první nebo druhý znak (více znaků tam nikdy není). Souřadnice jsou relativně k layoutBounds, tedy k bodu (0,0). Offset _FirstOffsetX je již aplikován.

                        // Zde bychom měli řešit Auto-Wrap line, pokud je to povoleno, požadováno, pokud je to možné a potřebné (Right znaku je za Width prostoru):
                        if (multiline && right > 0f && charIndex > 0 && ((locationX + currBounds.Right + 1f) >= right) && currChar != SPACE)
                            WrapToNewLine(characters, targetIndex, currChar, wrapWord, originX, rowHeight, ref rowIndex, ref charIndex, ref currentX, ref currentY, ref locationX, ref currBounds);

                        // Souřadnice znaku - tak, aby byl vypsán do požadovaného prostoru:
                        float charX = locationX + currBounds.X;
                        PointF textLocation = new PointF(charX - _FirstOffsetX, currentY);                       // Offset (_FirstOffsetX) se poprvé odečetl v metodě _GetLastCharBounds(), abychom měli souřadnice bodu odpovídající požadovanému bodu. Podruhé odečtu offset (_GetLastCharBounds) tady, tedy předsadím X doleva, a pak při finálním vykreslení znaku se ten znak dostane na požadovanou pozici...
                        locationX = charX;

                        // Souřadnice textBounds = celočíselné, na sebe navazující:
                        int textR = (int)Math.Floor(charX + currBounds.Width);
                        Rectangle textBounds = new Rectangle(currentX, currentY, textR - currentX, _FontHeight);
                        currentX = textR;

                        characters[targetIndex] = new FontPositionInfo(currChar, rowIndex, charIndex, textLocation, textBounds);
                        charIndex++;
                        targetIndex++;
                        prevChar = currChar;
                    }

                    // Pokud máme přeskočit následující LF, a za znakem CR následuje další znak, a je to znak LF, pak jde o standardní oddělovač řádků CrLf; ten jsme již zpracovali (jako jeden dvojznak) => následující LF přeskočíme:
                    if (skipNextLf && i < (length - 1) && text[i + 1] == lf)
                        i++;
                }

                // Nyní máme v poli result počet znaků (length), ale protože jsme mohli vynechat znaky LF, pak poslední prvky pole result mohou být null
                //  (vstupní znaky jsme načítali z indexu [i] = 0..length; ale ukládali jsme je do indexu targetIndex)
                if (targetIndex < length)
                {
                    FontPositionInfo[] target = new FontPositionInfo[targetIndex];
                    Array.Copy(characters, 0, target, 0, targetIndex);
                    characters = target;
                }

                return characters;
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
            /// <param name="rowHeight"></param>
            /// <param name="charIndex"></param>
            /// <param name="currentX"></param>
            /// <param name="currentY"></param>
            /// <param name="locationX"></param>
            /// <param name="currBounds"></param>
            private void WrapToNewLine(FontPositionInfo[] characters, int targetIndex, string currChar, bool wrapWord, int originX, int rowHeight, ref int rowIndex, ref int charIndex, ref int currentX, ref int currentY, ref float locationX, ref RectangleF currBounds)
            {
                // Najít začátek slova, které by mělo být celé přesunuté na nový řádek:
                int wordBegin = -1;
                if (currChar != SPACE && charIndex > 0 && characters[targetIndex-1].Text != SPACE && wrapWord)
                {   // Pokud aktuální znak NENÍ mezera, a jsme na pozici v řádku větší než 0 a předešlý znak NENÍ mezera, a mají se zalamovat slova, 
                    // pak zkusíme najít začátek slova (wordBegin), které bude přesunuto na další řádek:
                    for (int i = targetIndex - 1; i >= 0; i--)
                    {   // Projdu znaky před aktuální pozici, a hledám pozici znaku, který je první nemezera za mezerou na pozici v řádku větší než 0:
                        var character = characters[i];
                        if (character.CharIndex <= 0) break;                             // Pokud by slovo začínalo na pozici 0, pak jej nebudeme wrapovat na nový řádek, protože to nemá smysl.
                        if (character.Text != SPACE && characters[i-1].Text == SPACE)    // Pokud znak [i] NENÍ mezera, a předchozí znak JE mezera, pak znak [i] zalomíme na nový řádek:
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
                    
                    //int charOffset = -wordFirst.CharIndex;
                    //int xOffset = originX - wordFirst.TextBounds.X;
                    //PointF textLocationOffset = new PointF(xOffset /*originX - wordFirst.TextLocation.Value.X*/, rowHeight);
                    //Point textBoundsOffset = new Point(xOffset, rowHeight);
                    
                    for (int i = wordBegin; i < targetIndex; i++)
                        characters[i].Move(logicalOffset, fontOffset, boundsOffset);

                    FontPositionInfo lastChar = characters[targetIndex - 1];
                    rowIndex = lastChar.RowIndex;
                    charIndex = lastChar.CharIndex + 1;
                    currentX = lastChar.TextBounds.Right;
                    currentY = lastChar.TextBounds.Y;
                    locationX = (lastChar.TextLocation.HasValue ? lastChar.TextLocation.Value.X + _FirstOffsetX: (float)lastChar.TextBounds.X);
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
            /// <param name="charKey">Znak nebo dva znaky pro měření</param>
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
            /// <param name="charKey">Znak nebo dva znaky pro měření</param>
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

                using (var stringFormat = FontManagerInfo.CreateNewStandardStringFormat())
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
                // _FontHeight = font.GetHeight(graphics);
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
    #region class FontPositionInfo :  Údaj o jednom znaku a jeho pozici
    /// <summary>
    /// <see cref="FontPositionInfo"/> : Údaj o jednom znaku a jeho pozici
    /// </summary>
    public class FontPositionInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rowIndex"></param>
        /// <param name="charIndex"></param>
        /// <param name="textBounds"></param>
        /// <param name="bounds"></param>
        public FontPositionInfo(string text, int rowIndex, int charIndex, PointF? textLocation, Rectangle textBounds)
        {
            this.Text = text;
            this.RowIndex = rowIndex;
            this.CharIndex = charIndex;
            this.TextLocation = textLocation;
            this.TextBounds = textBounds;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Text: {Text}; Row: {RowIndex}; Char: {CharIndex}; Location: {TextLocation}; Bounds: {TextBounds}";
        }
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
        /// <param name="logicalOffset"></param>
        /// <param name="visualOffset"></param>
        public void Move(Point logicalOffset, PointF fontOffset, Point boundsOffset)
        {
            RowIndex += logicalOffset.Y;
            CharIndex += logicalOffset.X;
            if (TextLocation.HasValue) TextLocation = new PointF(TextLocation.Value.X + (float)fontOffset.X, TextLocation.Value.Y + (float)fontOffset.Y);
            TextBounds = new Rectangle(TextBounds.X + boundsOffset.X, TextBounds.Y + boundsOffset.Y, TextBounds.Width, TextBounds.Height);
        }
        /// <summary>
        /// Text znaku
        /// </summary>
        public string Text { get; private set; }
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
        /// Tento prostor je celočíselný, a znaky za sebou jdoucí mají tento prostor na sebe navazující.
        /// </summary>
        public Rectangle TextBounds { get; private set; }
    }
    #endregion
    #region FontMeasureParams : Parametry pro funkce měření pozice znaku v metodě FontMeasureInfo.GetCharInfo(string, Graphics, Font, FontMeasureParams)
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
    }
    #endregion
}
