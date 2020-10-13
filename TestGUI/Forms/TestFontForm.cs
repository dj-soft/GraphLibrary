using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Components;

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
            // Brush[] backBrushes = new Brush[] { new SolidBrush(Color.FromArgb(224, 255, 255)), new SolidBrush(Color.FromArgb(240, 225, 255)), new SolidBrush(Color.FromArgb(200, 255, 200)) };
            Brush[] backBrushes = new Brush[] { new SolidBrush(Color.FromArgb(240, 240, 240)), new SolidBrush(Color.FromArgb(240, 240, 240)), new SolidBrush(Color.FromArgb(240, 240, 240)) };

            using (var font = CreateFont())
            {
                PrepareGraphics(e.Graphics, bounds);

                int x = bounds.X + 3;
                int y = bounds.Y + 3;
                FontMeasureParams parameters = new FontMeasureParams() { Origin = new Point(x,y), LineHeightRatio = 1.00f, WrapWord = true, Width = bounds.Width - 6, Multiline = true };
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
}
