using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Djs.Common.Components;

namespace Djs.Common.TestGUI.Forms
{
    public partial class TestGraphSettingForm : Form
    {
        public TestGraphSettingForm()
        {
            InitializeComponent();
            InitializeScrollBar();
            InitializeSetting();
            this.ResizeRedraw = true;
        }
        private void InitializeScrollBar()
        {
            this._VScrollBar = new VScrollBar();
            this._VScrollBar.Dock = DockStyle.Right;
            this._VScrollBar.Value = 0;
            this._VScrollBar.ValueChanged += _VScrollBar_ValueChanged;
            this.TestPanel.Controls.Add(this._VScrollBar);
        }
        private void InitializeSetting()
        {
            // this._Settings = OneSetting.GetAll();
            // this._Settings = OneSetting.GetAll((s,i,t) => (t == TextRenderingHint.SystemDefault || t == TextRenderingHint.AntiAliasGridFit));
            this._Settings = OneSetting.GetAll(
                (s, i, t) =>
                (s == SmoothingMode.AntiAlias || s == SmoothingMode.HighQuality) && 
                (t == TextRenderingHint.AntiAlias)
                );
            this._VScrollBar.Maximum = this._Settings[this._Settings.Length - 1].BoundsAbsolute.Bottom;


            // Výsledky pro Text:
            //  a) Správné změření a zarovnání textu (doprava) je možné pouze s TextRenderingHint.AntiAlias, ostatní nastavení nemají vliv na správnost zarovnání vpravo (tj. korektní výpočet rozměru textu)
            //  b) Pro zobrazení textu nemají další nastavení význam

            // Výsledky pro Line (vodorovné, svislé, i úhlopříčné):
            //  Nejlepší podání čar poskytne SmoothingMode.AntiAlias nebo SmoothingMode.HighQuality (nevidím rozdíl)

            // Výsledky pro Křivky jsou stejné jako pro Line:
            //  Nejlepší podání čar poskytne SmoothingMode.AntiAlias nebo SmoothingMode.HighQuality (nevidím rozdíl)

        }
        private void _VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            this.TestPanel.Invalidate();
        }

        private VScrollBar _VScrollBar;
        private OneSetting[] _Settings;
        private void TestPanel_Paint(object sender, PaintEventArgs e)
        {
            this.TestPaint(e);
        }
        private void TestPaint(PaintEventArgs e)
        {
            int drawY = this._VScrollBar.Value;
            int drawH = this.TestPanel.ClientSize.Height;
            foreach (OneSetting oneSetting in this._Settings)
                oneSetting.Paint(e.Graphics, drawY, drawH);
        }
    }
    public class OneSetting
    {
        #region Data
        public SmoothingMode Smoothing { get; private set; }
        public InterpolationMode Interpolation { get; private set; }
        public TextRenderingHint TextRendering { get; private set; }
        public string SmoothingText { get; private set; }
        public Rectangle SmoothingBounds { get; private set; }
        public string InterpolationText { get; private set; }
        public Rectangle InterpolationBounds { get; private set; }
        public string TextRenderingText { get; private set; }
        public Rectangle TextRenderingBounds { get; private set; }
        public Rectangle TextSampleBounds { get; private set; }
        public Rectangle LineSampleBounds { get; private set; }
        public Rectangle CurveSampleBounds { get; private set; }
        public Color BackColor { get; private set; }
        public Rectangle BoundsAbsolute { get; private set; }
        #endregion
        #region Konstrukce
        internal static OneSetting[] GetAll()
        {
            return GetAll(null);
        }
        internal static OneSetting[] GetAll(Func<SmoothingMode, InterpolationMode, TextRenderingHint, bool> filter)
        {
            List<OneSetting> result = new List<OneSetting>();

            SmoothingMode[] smoothingModes = new SmoothingMode[] { SmoothingMode.Default, SmoothingMode.HighSpeed, SmoothingMode.HighQuality, SmoothingMode.None, SmoothingMode.AntiAlias };
            InterpolationMode[] interpolationModes = new InterpolationMode[] { InterpolationMode.Default, InterpolationMode.Low, InterpolationMode.High, InterpolationMode.Bilinear, InterpolationMode.Bicubic, InterpolationMode.NearestNeighbor, InterpolationMode.HighQualityBilinear, InterpolationMode.HighQualityBicubic };
            TextRenderingHint[] textRenderingHints = new TextRenderingHint[] { TextRenderingHint.SystemDefault, TextRenderingHint.SingleBitPerPixelGridFit, TextRenderingHint.SingleBitPerPixel, TextRenderingHint.AntiAliasGridFit, TextRenderingHint.AntiAlias, TextRenderingHint.ClearTypeGridFit };
            int w = 255;
            int l = 224;
            Color[] backColors = new Color[] { Color.FromArgb(w, l, l), Color.FromArgb(l, w, l), Color.FromArgb(l, l, w), Color.FromArgb(w, l, w), Color.FromArgb(w, w, l), Color.FromArgb(l, w, w) };
            int c = 0;
            int y = 5;

            foreach (SmoothingMode smoothingMode in smoothingModes)
                foreach (InterpolationMode interpolationMode in interpolationModes)
                    foreach (TextRenderingHint textRenderingHint in textRenderingHints)
                    {
                        if (filter == null || filter(smoothingMode, interpolationMode, textRenderingHint))
                            result.Add(new OneSetting(smoothingMode, interpolationMode, textRenderingHint, backColors[(c++) % (backColors.Length)], ref y));
                    }

            return result.ToArray();
        }
        private OneSetting(SmoothingMode smoothing, InterpolationMode interpolation, TextRenderingHint textRendering, Color backColor, ref int y)
        {
            this.Smoothing = smoothing;
            this.Interpolation = interpolation;
            this.TextRendering = textRendering;
            this.SmoothingText = "SmoothingMode." + smoothing.ToString();
            this.InterpolationText = "InterpolationMode." + interpolation.ToString();
            this.TextRenderingText = "TextRenderingHint." + textRendering.ToString();
            this.BackColor = backColor;

            int h = ROW_HEIGHT;
            this.BoundsAbsolute = new Rectangle(5, y, 2000, h);
            int x = 0;
            this.SmoothingBounds = new Rectangle(x, 3, ITEM_SMOOTHING_WIDTH, h); x = this.SmoothingBounds.Right + 5;
            this.InterpolationBounds = new Rectangle(x, 3, ITEM_INTERPOLATION_WIDTH, h); x = this.InterpolationBounds.Right + 5;
            this.TextRenderingBounds = new Rectangle(x, 3, ITEM_RENDERING_WIDTH, h); x = this.TextRenderingBounds.Right + 5;
            this.TextSampleBounds = new Rectangle(x, 0, ITEM_TEXTSAMPLE_WIDTH, h); x = this.TextSampleBounds.Right + 5;
            this.LineSampleBounds = new Rectangle(x, 0, ITEM_DRAWSAMPLE_WIDTH, h); x = this.LineSampleBounds.Right + 5;
            this.CurveSampleBounds = new Rectangle(x, 0, ITEM_DRAWSAMPLE_WIDTH, h); x = this.CurveSampleBounds.Right + 5;
            y = this.BoundsAbsolute.Bottom + 2;
        }
        private const int ROW_HEIGHT = 34;
        private const int ITEM_SMOOTHING_WIDTH = 225;
        private const int ITEM_INTERPOLATION_WIDTH = 250;
        private const int ITEM_RENDERING_WIDTH = 250;
        private const int ITEM_TEXTSAMPLE_WIDTH = 175;
        private const int ITEM_DRAWSAMPLE_WIDTH = 75;
        #endregion
        #region Kreslení
        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="drawY">Souřadnice v BoundsAbsolute, která bude vykreslena na pixel 0, pohybuje se od 0 do cca 2000</param>
        /// <param name="drawH">Výška viditelného prostoru v pixelech</param>
        public void Paint(Graphics graphics, int drawY, int drawH)
        {
            Rectangle boundsAbsolute = this.BoundsAbsolute;                              // Y = 0 až 2488
            Rectangle boundsRelative = new Rectangle(boundsAbsolute.X, boundsAbsolute.Y - drawY, boundsAbsolute.Width, boundsAbsolute.Height);
            if (boundsRelative.Bottom <= 0 || boundsRelative.Y >= drawH) return;         // Prvek je mimo viditelnou oblast

            graphics.SmoothingMode = this.Smoothing;
            graphics.InterpolationMode = this.Interpolation;
            graphics.TextRenderingHint = this.TextRendering;
            graphics.ResetClip();

            graphics.IntersectClip(boundsRelative);
            using (SolidBrush backBrush = new SolidBrush(this.BackColor))
            {
                graphics.FillRectangle(backBrush, boundsRelative);
            }

            using (Font fontStd = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Regular))
            using (Font fontBold = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold))
            {
                this.PaintOne(graphics, fontStd, boundsRelative, this.SmoothingBounds, this.SmoothingText);
                this.PaintOne(graphics, fontStd, boundsRelative, this.InterpolationBounds, this.InterpolationText);
                this.PaintOne(graphics, fontStd, boundsRelative, this.TextRenderingBounds, this.TextRenderingText);
                this.PaintTextSample(graphics, fontStd, fontBold, boundsRelative);
            }
            this.PaintLineSample(graphics, boundsRelative);
            this.PaintCurveSample(graphics, boundsRelative);
        }
        private void PaintOne(Graphics graphics, Font font, Rectangle boundsRelative, Rectangle boundsItem, string text)
        {
            Rectangle bounds = boundsItem;
            bounds.X = bounds.X + boundsRelative.X;
            bounds.Y = bounds.Y + boundsRelative.Y;
            StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
            graphics.DrawString(text, font, Brushes.Black, bounds, sf);
        }
        private void PaintTextSample(Graphics graphics, Font fontStd, Font fontBold, Rectangle boundsRelative)
        {
            Rectangle bounds = this.TextSampleBounds;
            bounds.X = bounds.X + boundsRelative.X;
            bounds.Y = bounds.Y + boundsRelative.Y;

            StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
            string text = "[Libovolný text]";

            SizeF textSizeStd = graphics.MeasureString(text, fontStd, bounds.Width, sf);
            Rectangle textAreaStd = textSizeStd.AlignTo(bounds, ContentAlignment.TopRight, true);
            graphics.DrawString(text, fontStd, Brushes.Black, textAreaStd, sf);

            SizeF textSizeBold = graphics.MeasureString(text, fontBold, bounds.Width, sf);
            Rectangle textAreaBold = textSizeBold.AlignTo(bounds, ContentAlignment.BottomRight, true);
            graphics.DrawString(text, fontBold, Brushes.Black, textAreaBold, sf);

        }
        private void PaintLineSample(Graphics graphics, Rectangle boundsRelative)
        {
            Rectangle bounds = this.LineSampleBounds;
            bounds.X = bounds.X + boundsRelative.X;
            bounds.Y = bounds.Y + boundsRelative.Y;
            bounds = bounds.Enlarge(-1);

            using (Pen pen = new Pen(Color.Black, 1f))
            {
                graphics.DrawRectangle(pen, bounds);

                bounds = bounds.Enlarge(0, 0, -1, -1);
                graphics.DrawLine(pen, bounds.X, bounds.Y, bounds.Right, bounds.Bottom);
                graphics.DrawLine(pen, bounds.X, bounds.Bottom, bounds.Right, bounds.Y);
            }
        }
        private void PaintCurveSample(Graphics graphics, Rectangle boundsRelative)
        {
            Rectangle bounds = this.CurveSampleBounds;
            bounds.X = bounds.X + boundsRelative.X;
            bounds.Y = bounds.Y + boundsRelative.Y;
            bounds = bounds.Enlarge(-2);

            using (Pen pen = new Pen(Color.Black, 1f))
            {
                graphics.DrawEllipse(pen, bounds);

                Rectangle circle = bounds.Center().CreateRectangleFromCenter(bounds.Height - 2);
                graphics.DrawEllipse(pen, circle);
            }
        }

        #endregion

    }
    public class DoubleBufferPanel : Panel
    {
        public DoubleBufferPanel()
        {
            this.DoubleBuffered = true;
        }
    }
}
