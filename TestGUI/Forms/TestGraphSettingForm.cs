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

namespace Djs.Common.TestGUI.Forms
{
    public partial class TestGraphSettingForm : Form
    {
        public TestGraphSettingForm()
        {
            InitializeComponent();
            this._Settings = OneSetting.GetAll();
            InitializeScrollBar();
            this.ResizeRedraw = true;
        }
        private void InitializeScrollBar()
        {
            this._VScrollBar = new VScrollBar();
            this._VScrollBar.Dock = DockStyle.Right;
            this._VScrollBar.Value = 0;
            this._VScrollBar.Maximum = this._Settings[this._Settings.Length - 1].BoundsAbsolute.Bottom;
            this._VScrollBar.ValueChanged += _VScrollBar_ValueChanged;
            this.TestPanel.Controls.Add(this._VScrollBar);
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

            /*
            SmoothingMode[] smoothingModes = new SmoothingMode[] { SmoothingMode.Default, SmoothingMode.HighSpeed, SmoothingMode.HighQuality, SmoothingMode.None, SmoothingMode.AntiAlias };
            InterpolationMode[] interpolationModes = new InterpolationMode[] { InterpolationMode.Default, InterpolationMode.Low, InterpolationMode.High, InterpolationMode.Bilinear, InterpolationMode.Bicubic, InterpolationMode.NearestNeighbor, InterpolationMode.HighQualityBilinear, InterpolationMode.HighQualityBicubic };
            TextRenderingHint[] textRenderingHints = new TextRenderingHint[] { TextRenderingHint.SystemDefault, TextRenderingHint.SingleBitPerPixelGridFit, TextRenderingHint.SingleBitPerPixel, TextRenderingHint.AntiAliasGridFit, TextRenderingHint.AntiAlias, TextRenderingHint.ClearTypeGridFit };
            int w = 255;
            int l = 224;
            Color[] backColors = new Color[] { Color.FromArgb(w, l, l), Color.FromArgb(l, w, l), Color.FromArgb(l, l, w), Color.FromArgb(w, l, w), Color.FromArgb(w, w, l), Color.FromArgb(l, w, w) };
            int c = 0;
            int y = 5;
            using (Font font1 = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Regular))
            using (Font font2 = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold))
            {
                foreach (SmoothingMode smoothingMode in smoothingModes)
                    foreach (InterpolationMode interpolationMode in interpolationModes)
                        foreach (TextRenderingHint textRenderingHint in textRenderingHints)
                            TestPaint(e.Graphics, font1, font2, smoothingMode, interpolationMode, textRenderingHint, backColors[(c++) % (backColors.Length)], ref y);
            }
            */
        }
        private void TestPaint(Graphics graphics, Font font1, Font font2, SmoothingMode smoothingMode, InterpolationMode interpolationMode, TextRenderingHint textRenderingHint, Color backColor, ref int y)
        {
            graphics.SmoothingMode = smoothingMode;
            graphics.InterpolationMode = interpolationMode;
            graphics.TextRenderingHint = textRenderingHint;
            graphics.ResetClip();

            Rectangle boundsRow = new Rectangle(5, y, this.TestPanel.ClientSize.Width - 12, 24);
            graphics.IntersectClip(boundsRow);
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(backBrush, boundsRow);
            }

            Font sysFont = SystemFonts.DialogFont;

            Rectangle boundsSmo = new Rectangle(5, y + 3, 130, 24);
            string textSmo = "Smo:" + smoothingMode.ToString();
            graphics.DrawString(textSmo, sysFont, Brushes.Black, boundsSmo);

            Rectangle boundsInt = new Rectangle(140, y + 3, 170, 24);
            string textInt = "Int:" + interpolationMode.ToString();
            graphics.DrawString(textInt, sysFont, Brushes.Black, boundsInt);

            Rectangle boundsTxh = new Rectangle(315, y + 3, 170, 24);
            string textTxh = "Hint:" + textRenderingHint.ToString();
            graphics.DrawString(textTxh, sysFont, Brushes.Black, boundsTxh);

            y = boundsRow.Bottom + 1;
        }
        public class OneSetting
        {
            public SmoothingMode Smoothing { get; private set; }
            public InterpolationMode Interpolation { get; private set; }
            public TextRenderingHint TextRendering { get; private set; }
            public string SmoothingText { get; private set; }
            public Rectangle SmoothingBounds { get; private set; }
            public string InterpolationText { get; private set; }
            public Rectangle InterpolationBounds { get; private set; }
            public string TextRenderingText { get; private set; }
            public Rectangle TextRenderingBounds { get; private set; }
            public Color BackColor { get; private set; }
            public Rectangle BoundsAbsolute { get; private set; }

            private OneSetting(SmoothingMode smoothing, InterpolationMode interpolation, TextRenderingHint textRendering, Color backColor, ref int y)
            {
                this.Smoothing = smoothing;
                this.Interpolation = interpolation;
                this.TextRendering = textRendering;
                this.SmoothingText = "SmoothingMode." + smoothing.ToString();
                this.InterpolationText = "InterpolationMode." + interpolation.ToString();
                this.TextRenderingText = "TextRenderingHint." + textRendering.ToString();
                this.BackColor = backColor;

                int h = 25;
                this.BoundsAbsolute = new Rectangle(5, y, 2000, h);
                this.SmoothingBounds = new Rectangle(0, 0, 140, h);
                this.InterpolationBounds = new Rectangle(145, 0, 180, h);
                this.TextRenderingBounds = new Rectangle(330, 0, 180, h);

                y = this.BoundsAbsolute.Bottom + 1;
            }
            public void Paint(Graphics graphics, int drawY1, int drawY2)
            {
                Rectangle boundsAbsolute = this.BoundsAbsolute;
                if (drawY1 >= boundsAbsolute.Bottom || drawY2 <= boundsAbsolute.Y) return;
                int dy = boundsAbsolute.Y - drawY1;

                graphics.SmoothingMode = smoothingMode;
                graphics.InterpolationMode = interpolationMode;
                graphics.TextRenderingHint = textRenderingHint;
                graphics.ResetClip();

                Rectangle boundsRow = new Rectangle(5, y, this.TestPanel.ClientSize.Width - 12, 24);
                graphics.IntersectClip(boundsRow);
                using (SolidBrush backBrush = new SolidBrush(backColor))
                {
                    graphics.FillRectangle(backBrush, boundsRow);
                }

                Font sysFont = SystemFonts.DialogFont;

                Rectangle boundsSmo = new Rectangle(5, y + 3, 130, 24);
                string textSmo = "Smo:" + smoothingMode.ToString();
                graphics.DrawString(textSmo, sysFont, Brushes.Black, boundsSmo);

                Rectangle boundsInt = new Rectangle(140, y + 3, 170, 24);
                string textInt = "Int:" + interpolationMode.ToString();
                graphics.DrawString(textInt, sysFont, Brushes.Black, boundsInt);

                Rectangle boundsTxh = new Rectangle(315, y + 3, 170, 24);
                string textTxh = "Hint:" + textRenderingHint.ToString();
                graphics.DrawString(textTxh, sysFont, Brushes.Black, boundsTxh);


            }
            internal static OneSetting[] GetAll()
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
                            result.Add(new OneSetting(smoothingMode, interpolationMode, textRenderingHint, backColors[(c++) % (backColors.Length)], ref y));

                return result.ToArray();
            }
        }
    }
}
