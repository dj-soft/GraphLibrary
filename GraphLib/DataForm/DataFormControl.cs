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
    /// WinForm Panel obsahující DataForm control
    /// </summary>
    public class DataFormContainer : Panel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormContainer()
        {
            this.GDataForm = new GDataFormControl();
            this.Controls.Add(this.GDataForm);
            this.AutoScroll = true;

        }
        /// <summary>
        /// Vlastní control
        /// </summary>
        protected GDataFormControl GDataForm;
    }
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
            int topX = 1100;
            int maxX = 0;
            int maxY = 0;
            int currentX = firstX;
            int currentY = firstY;

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
            int valuesCnt = values.Length;
            Random rand = new Random();

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
                    if (rand.Next(10) > 6)
                        item.RelationType = (rand.Next(5) > 2) ? TextRelationType.ToRecord : TextRelationType.ToDocument;
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
