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
            this.AddDataFormItems(4500);


#warning    JEN PRO TESTY:   this.AutoScroll = true;
            this.AutoScroll = true;


        }
        /// <summary>
        /// Naplní controly
        /// </summary>
        protected void AddDataFormItems(int count)
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

            for (int n = 0; n < count; n++)
            {
                GDataFormItem item = new GDataFormItem()
                {
                    BackColor = colors[rand.Next(colorCnt)],
                    Location = new Point(currentX, currentY),
                    Label = "Item " + (n + 1).ToString(),
                    Value1 = values[rand.Next(valuesCnt)] + "_" + rand.Next(10000, 99999).ToString()
                };
                item.ToolTipText = $@"Další informace k této položce
byste našli v této bublině.
Číslo prvku: {(n+1)}
Obsah prvku: {item.Value1}
Souřadnice prvku: {currentX}/{currentY}
";
                if (rand.Next(10) > 6)
                    item.RelationType = (rand.Next(5) > 2) ? TextRelationType.ToRecord : TextRelationType.ToDocument;
                this.AddItem(item);

                Rectangle bounds = item.Bounds;
                int r = bounds.Right;
                int b = bounds.Bottom;
                if (maxX < r) maxX = r;
                if (maxY < b) maxY = b;
                if (r + spaceX + bounds.Width <= topX)
                {   // Za tento prvek se ještě vejde další => posunu pozici doprava:
                    currentX = r + spaceX;
                }
                else
                {   // Za tento prvek se žádný další už nevejde => posunu pozici dolů:
                    currentX = firstX;
                    currentY = b + spaceY;
                }
            }

            this.Size = new Size(maxX + firstX, maxY + firstY);


            /*
            this.AddItem(new GDataFormItem()
            {
                Location = new Point(10, 10), Label = "Ukázka 1:", Value1 = "VFA-12-2019",
                ToolTipText = "Další informace k této položce\r\nmohou být zde",
                RelationType = TextRelationType.ToRecord
            });
            this.AddItem(new GDataFormItem()
            {
                Location = new Point(310, 10), Label = "Ukázka 2:", Value1 = "VFA-13-2019",
                ToolTipText = "Další informace k této položce\r\nmohou být zde",
                RelationType = TextRelationType.ToDocument,
                BorderStyle = BorderStyleType.Soft
            });
            this.Size = new Size(1200, 850);
            */
        }
    }
}
