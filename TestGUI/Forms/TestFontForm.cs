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
            // _InputText.Text = "Cokoliv do tohoto políčka vepíšeme, to se vypíše v dolních polích";
            _InputText.Text = "A";
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
                FontMeasureParams parameters = new FontMeasureParams() { Origin = new Point(x,y), LineHeightRatio = 1.25f, WrapWord = true, Width = bounds.Width - 6 };
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

                text = NormalizeCrLf(text);

                int length = text.Length;
                FontPositionInfo[] result = new FontPositionInfo[length];
                Point origin = parameters?.Origin ?? Point.Empty;
                int? width = parameters?.Width;
                int originX = origin.X;
                int currentX = origin.X;
                int currentY = origin.Y;
                float locationX = currentX;
                RectangleF layoutBounds = _LayoutBounds;
                string prevChar = "";
                char cr = CR[0];
                for (int i = 0; i < length; i++)
                {
                    char c = text[i];
                    if (c == cr)
                    {   // Znak CR, za ním vždy následuje LF, oba zpracuji jako jednu pozici:
                        string currChar = CRLF;
                        Rectangle textBounds = new Rectangle(currentX, currentY, 1, _FontHeight);
                        result[i] = new FontPositionInfo(currChar, null, textBounds);
                        i++;
                        result[i] = new FontPositionInfo(currChar, null, textBounds);
                        currentX = origin.X;
                        currentY = currentY + _FontHeight;
                        locationX = currentX;
                        prevChar = "";
                    }
                    else
                    {   // Jiný znak než CrLf
                        string currChar = c.ToString();
                        string charKey = prevChar + currChar;

                        // Prostor obsazený znakem, relativně k předchozímu znaku:
                        RectangleF currBounds = _GetLastCharBounds(graphics, font, charKey, layoutBounds);       // Vrátí souřadnici posledního znaku v "charKey", tedy první nebo druhý znak (více znaků tam nikdy není). Souřadnice jsou relativně k layoutBounds, tedy k bodu (0,0). Offset _FirstOffsetX je již aplikován.
                        float charX = locationX + currBounds.X;
                        PointF textLocation = new PointF(charX - _FirstOffsetX, currentY);                       // Offset (_FirstOffsetX) se poprvé odečetl v metodě _GetLastCharBounds(), abychom měli souřadnice bodu odpovídající požadovanému bodu. Podruhé odečtu offset (_GetLastCharBounds) tady, tedy předsadím X doleva, a pak při finálním vykreslení znaku se ten znak dostane na požadovanou pozici...
                        locationX = charX;

                        // Souřadnice textBounds = celočíselné, na sebe navazující:
                        int textR = (int)Math.Floor(charX + currBounds.Width);
                        Rectangle textBounds = new Rectangle(currentX, currentY, textR - currentX, _FontHeight);
                        currentX = textR;

                        result[i] = new FontPositionInfo(currChar, textLocation, textBounds);
                        prevChar = currChar;
                    }
                }

                return result;
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
        /// <param name="textBounds"></param>
        /// <param name="bounds"></param>
        public FontPositionInfo(string text, PointF? textLocation, Rectangle textBounds)
        {
            this.Text = text;
            this.TextLocation = textLocation;
            this.TextBounds = textBounds;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Text: {Text}; Location: {TextLocation}; Bounds: {TextBounds}";
        }
        /// <summary>
        /// Text znaku
        /// </summary>
        public string Text { get; private set; }
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
    }
    #endregion
}
