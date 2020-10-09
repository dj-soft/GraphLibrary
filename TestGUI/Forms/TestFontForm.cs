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
                    e.Graphics.DrawString(text, font, brush, bounds, stringFormat);
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
            Brush[] backBrushes = new Brush[] { new SolidBrush(Color.FromArgb(240, 255, 255)), new SolidBrush(Color.FromArgb(240, 240, 255)) };
            int bbi = 0;
                
            using (var font = CreateFont())
            {
                PrepareGraphics(e.Graphics, bounds);

                float x = bounds.X + 3;
                float y = bounds.Y + 3;
                FontMeasureParams parameters = new FontMeasureParams() { Origin = new PointF(x,y), LineHeightRatio = 1.25f, WrapWord = true, Width = bounds.Width - 6 };
                var characters = FontMeasureInfo.GetCharInfo(text, e.Graphics, font, parameters);

                StringFormat stringFormat = new StringFormat(StringFormatFlags.NoClip);
                foreach (var character in characters)
                    e.Graphics.DrawString(character.Text.ToString(), font, brush, character.Bounds.Location);

                /*
                float w = 7f;
                float h = 24f;
                foreach (char c in text)
                {
                    PointF p = new PointF(x, y);
                    var size = e.Graphics.MeasureString(c.ToString(), font);
                    RectangleF charBounds = new RectangleF(x, y, size.Width, size.Height);

                    RectangleF backBounds = charBounds;
                    if (bbi == 0) backBounds.Y = backBounds.Y - 1;
                    e.Graphics.FillRectangle(backBrushes[bbi], backBounds);
                    bbi++;
                    if (bbi >= backBrushes.Length) bbi = 0;

                    e.Graphics.DrawString(c.ToString(), font, brush, charBounds);

                    x += charBounds.Width;

                    if (x > bounds.Right) break;
                }
                */
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
    internal class FontMeasureInfo
    {
        #region Změření znaků
        public static FontMeasureCharInfo[] GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters = null)
        {
            return Current._GetCharInfo(text, graphics, font, parameters);
        }
        private FontMeasureCharInfo[] _GetCharInfo(string text, Graphics graphics, Font font, FontMeasureParams parameters)
        {
            if (text == null || graphics == null || font == null) return null;
            int length = text.Length;
            FontMeasureCharInfo[] result = new FontMeasureCharInfo[length];
            if (length == 0) return result;

            string fontKey = _GetFontKey(font);
            Dictionary<string, RectangleF> charDict = _GetCharDict(fontKey);
            PointF origin = parameters?.Origin ?? PointF.Empty;
            float? width = parameters?.Width;
            float originX = origin.X;
            float currentX = origin.X;
            float currentY = origin.Y;

            string prevChar = "";
            for (int i = 0; i < length; i++)
            {
                char c = text[i];

                string currChar = c.ToString();
                string charKey = prevChar + currChar;
                RectangleF currBounds = _GetCharBounds(graphics, font, charKey, charDict);
                RectangleF charBounds = new RectangleF(currentX + currBounds.X, currentY + currBounds.Y, currBounds.Width, currBounds.Height);
                currentX = charBounds.X;
                result[i] = new FontMeasureCharInfo(c, charBounds);
                prevChar = currChar;
            }

            return result;
        }
        /// <summary>
        /// Vrátí název fontu = klíč do Dictionary
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        private static string _GetFontKey(Font font)
        {
            return font.Name + "#" + font.SizeInPoints.ToString("##0.0") + "#" + (font.Bold ? "B" : "") + (font.Italic ? "I" : "") + (font.Underline ? "U" : "");
        }
        /// <summary>
        /// Najde / založí data pro jeden typ fontu. Řeší multithread zámek.
        /// </summary>
        /// <param name="fontKey"></param>
        /// <returns></returns>
        private Dictionary<string, RectangleF> _GetCharDict(string fontKey)
        {
            Dictionary<string, RectangleF> charDict;
            if (!__FontSizes.TryGetValue(fontKey, out charDict))
            {
                lock (__FontSizes)
                {
                    if (!__FontSizes.TryGetValue(fontKey, out charDict))
                    {
                        charDict = new Dictionary<string, RectangleF>();
                        __FontSizes.Add(fontKey, charDict);
                    }
                }
            }
            return charDict;
        }
        private RectangleF _GetCharBounds(Graphics graphics, Font font, string charKey, Dictionary<string, RectangleF> charDict)
        {
            RectangleF charBounds;
            if (!charDict.TryGetValue(charKey, out charBounds))
            {
                charBounds = _CalculateLastCharBounds(graphics, font, charKey);
                charDict.Add(charKey, charBounds);
            }
            return charBounds;
        }
        /// <summary>
        /// Vrátí souřadnici posledního znaku v daném textu
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private RectangleF _CalculateLastCharBounds(Graphics graphics, Font font, string text)
        {
            using (StringFormat stringFormat = StringFormat.GenericTypographic.Clone() as StringFormat)
            {
                stringFormat.SetMeasurableCharacterRanges(new CharacterRange[] { new CharacterRange(text.Length - 1, 1) });
                var charRanges = graphics.MeasureCharacterRanges(text, font, __LayoutRect, stringFormat);
                RectangleF charBounds = charRanges[0].GetBounds(graphics);
                return charBounds;
            }
        }
        #endregion


        #region Singleton
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static FontMeasureInfo Current
        {
            get
            {
                if (__Current == null)
                {
                    lock (__Locker)
                    {
                        if (__Current == null)
                            __Current = new FontMeasureInfo();
                    }
                }
                return __Current;
            }
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private FontMeasureInfo()
        {
            __FontSizes = new Dictionary<string, Dictionary<string, RectangleF>>();
            __LayoutRect = new Rectangle(0, 0, 500, 250);
        }
        private static FontMeasureInfo __Current = null;
        private static object __Locker = new object();
        private Dictionary<string, Dictionary<string, RectangleF>> __FontSizes;
        private Rectangle __LayoutRect;
        #endregion
    }
    public class FontMeasureParams
    {
        /// <summary>
        /// Souřadnice prvního znaku (Top,Left), od něj budou znaky umisťovány
        /// </summary>
        public PointF? Origin { get; set; }
        /// <summary>
        /// Maximální šířka řádku v pixelech pro automatické řádkování. Pokud by znak byť jen zčásti přesahoval nad tuto šířku, bude umístěn na nový řádek.
        /// Podle parametru <see cref="WrapWord"/> může být na nový řádek umístěno celé poslední slovo.
        /// Pokud bude <see cref="Width"/> = null, nebude se automaticky zalamovat.
        /// </summary>
        public float? Width { get; set; }
        /// <summary>
        /// Při zalamování textu do nového řádku zalamovat celá slova, ne jen poslední přesahující znak.
        /// </summary>
        public bool WrapWord { get; set; }
        /// <summary>
        /// Výška řádku relativně k fontu. Null = 1 = dle předpisu fontu.
        /// </summary>
        public float? LineHeightRatio { get; set; }
    }
    /// <summary>
    /// Údaj o jednom znaku a jeho pozici
    /// </summary>
    public class FontMeasureCharInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        public FontMeasureCharInfo(char text, RectangleF bounds)
        {
            this.Text = text;
            this.Bounds = bounds;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Text: {Text}; Bounds: {Bounds}";
        }
        /// <summary>
        /// Text znaku
        /// </summary>
        public char Text { get; private set; }
        /// <summary>
        /// Pozice znaku
        /// </summary>
        public RectangleF Bounds { get; private set; }
    }
}
