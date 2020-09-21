using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Components;
using System.Drawing;
using System.Windows.Forms;

using Asol.Tools.WorkScheduler.Services;
using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.DataForm
{
    /// <summary>
    /// DataFormControl
    /// </summary>
    /// <remarks>David Janáček, počátek 1.12.2019</remarks>
    public class GDataFormControl : GInteractiveControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GDataFormControl()
        {
            this.AddDataFormItems(10, 450);


#warning    JEN PRO TESTY:   this.AutoScroll = true;
            this.AutoScroll = true;


        }
        /// <summary>
        /// Naplní controly
        /// </summary>
        protected void AddDataFormItems(int countX, int countY)
        {
            this.ItemsList.Clear();
            int firstX = 8;
            int firstY = 8;
            int spaceX = 3;
            int spaceY = 3;
            // int topX = 1100;
            int maxX = 0;
            int maxY = 0;
            int currentX = firstX;
            int currentY = firstY;
            int lastX = countX - 1;

            int lo = 216;
            int hi = 240;
            Color[] colors = new Color[]
            {
                Color.FromArgb(hi, lo, lo),
                Color.FromArgb(lo, hi, lo),
                Color.FromArgb(lo, lo, hi),
                Color.FromArgb(hi, hi, lo),
                Color.FromArgb(lo, hi, hi),
                Color.FromArgb(hi, lo, hi),
                Color.FromArgb(lo, lo, lo),
                Color.FromArgb(hi, hi, hi)
            };
            int colorCnt = colors.Length;
            string[] values = new string[] { "VFA-2019", "VP", "DOC-INT", "PRJ", "VYD", "IUD-2019", "MAT", "ZFO", "ČJ" };
            string[] suffixes = new string[] { "vyřešit ihned", "porada nutná", "rozhoduje pouze šéf", "neřešit, založit", "utajený dokument" };
            int valuesCnt = values.Length;
            Random rand = new Random();

            ITextEditOverlay[] overlays = new ITextEditOverlay[]
            {
                new TextEditOverlayRelationLine(false, false),
                new TextEditOverlayRelationLine(false, true),
                new TextEditOverlayRelationLine(true, false),
                new TextEditOverlayRelationLine(true, true),
                new TextEditOverlayRelationIcon(false),
                new TextEditOverlayRelationIcon(true)
            };

            Rectangle bounds = Rectangle.Empty;
            int n = 0;
            for (int ny = 0; ny < countY; ny++)
            {
                currentX = firstX;
                for (int nx = 0; nx < countX; nx++)
                {
                    n++;
                    GDataFormItem item = new GDataFormItem()
                    {
                        BackColor = colors[rand.Next(colorCnt)],
                        Location = new Point(currentX, currentY),
                        Label = "Item " + n.ToString(),
                        Value1 = values[rand.Next(valuesCnt)] + "_" + rand.Next(10000, 99999).ToString()
                    };
                    item.ToolTipText = $@"Další informace k této položce
byste našli v této bublině.
Číslo prvku: {n}
Obsah prvku: {item.Value1}
Pozice prvku: {nx}/{ny}
Souřadnice prvku: {currentX}/{currentY}
";
                    if (rand.Next(10) <= 3) item.Value1 += ": " + suffixes[rand.Next(suffixes.Length)];
                    item.BorderStyle = BorderStyleType.Soft;
                    if (nx == 0) item.LabelMain.FontModifier.Bold = true;
                    if (nx == 1) item.LabelMain.FontModifier.Italic = true;
                    if (nx == 2) item.LabelMain.FontModifier.RelativeSize = 80;
                    if (nx == lastX) item.Enabled = false;

                    if (nx == 3 || nx == 4 || nx == lastX)       // (rand.Next(10) > 6)
                        item.OverlayText = overlays[rand.Next(overlays.Length)];
                    this.AddItem(item);
                    bounds = item.Bounds;

                    if (maxX < bounds.Right) maxX = bounds.Right;
                    currentX = bounds.Right + spaceX;
                }

                if (maxY < bounds.Bottom) maxY = bounds.Bottom;
                currentY = bounds.Bottom + spaceY;
            }

            // this.Size = new Size(maxX + firstX, maxY + firstY);
        }
    }
}
