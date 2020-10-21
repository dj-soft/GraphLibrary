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

#warning    JEN PRO TESTY:   this.AutoScroll = true;
            this.AutoScroll = true;


        }
        /// <summary>
        /// Naplní controly
        /// </summary>
        public void AddDataFormItems(int countX, int countY)
        {
            #region Barvy, texty, fonty
            int lo = 224;
            int hi = 228;
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
            string[] suffixes = new string[] { "vyřešit ihned", "porada nutná", "rozhoduje pouze šéf", "neřešit, založit", "utajený dokument", "Extra délka doplňkového textu, přesahující 32 znaků" };
            int valuesCnt = values.Length;
            Random rand = new Random();

            ITextEditOverlay[] overlays = new ITextEditOverlay[]
            {
                new TextEditOverlayUnderline(false, false),
                new TextEditOverlayUnderline(false, true),
                new TextEditOverlayUnderline(true, false),
                new TextEditOverlayUnderline(true, true),
                new TextEditOverlayRightSideIcon(false),
                new TextEditOverlayRightSideIcon(true)
            };

            FontModifierInfo titleFontOnFocus = new FontModifierInfo() { Bold = true, SizeRatio = 1.15f };
            Color? titleColorOnFocus = Color.DarkBlue;
            FontModifierInfo labelFont = new FontModifierInfo() { SizeRatio = 1.10f };
            FontModifierInfo labelFontOnFocus = new FontModifierInfo() { Bold = true, SizeRatio = 1.08f };
            Color? labelColorOnFocus = Skin.Control.ControlTextFocusColor;
            #endregion
            #region Pozice, mezery, okraje tabů a itemů
            int tabX0 = 6;
            int tabY0 = 6;
            int tabX = tabX0;
            int tabY = 0;
            int tabYSpace = 0;

            int itemX0 = 8;
            int itemXSpace = 0;
            int itemY0 = 4;
            int itemYSpace = 0;
            int itemX = 0;
            int itemY = 0;
            #endregion

            this.ItemsList.Clear();
            InteractiveLabeledContainer tab = null;
            int tabIdx = 0;
            Rectangle bounds = Rectangle.Empty;
            int n = 0;
            int lastX = countX - 1;
            for (int ny = 0; ny < countY; ny++)
            {
                if (tab == null || (ny % 3) == 0)
                {
                    tabX = tabX0;
                    tabY = (tab != null ? tab.Bounds.Bottom + tabYSpace : tabY0);
                    tab = new InteractiveLabeledContainer()
                    {
                        Location = new Point(tabX, tabY),
                        TitleLabelVisible = true,
                        TitleLineVisible = true
                    };
                    tabIdx++;
                    tab.TitleLabel.Text = $"Titulek skupiny {tabIdx}...";
                    tab.TitleLabel.Bounds = new Rectangle(itemX0, 2, 350, 20);
                    tab.TitleLine.Bounds = new Rectangle(itemX0, 23, 700, 2);
                    tab.TitleLine.LineColor = Color.FromArgb(192, 48, 80, 48);
                    tab.TitleLine.LineColorEnd = Color.FromArgb(8, 48, 80, 48);
                    tab.TitleLine.Border3D = 0;
                    tab.TitleLabel.FontModifier = labelFont;
                    tab.TitleFontModifierOnFocus = titleFontOnFocus;
                    tab.TitleTextColorOnFocus = titleColorOnFocus;

                    this.AddItem(tab);
                    itemY = 28 + itemY0;
                }

                itemX = itemX0;
                for (int nx = 0; nx < countX; nx++)
                {
                    n++;

                    // Nadefinování prvku Item - texty, font, overlay:
                    GDataFormItem item = new GDataFormItem()
                    {
                        BackColor = colors[rand.Next(colorCnt)],
                        Location = new Point(itemX, itemY),
                        Label = "Item " + n.ToString(),
                        Value1 = values[rand.Next(valuesCnt)] + "_" + rand.Next(10000, 99999).ToString(),
                        TitleFontModifierOnFocus = labelFontOnFocus,
                        TitleTextColorOnFocus = labelColorOnFocus
                    };
                    tab.AddItem(item);

                    if (rand.Next(10) <= 3) item.Value1 += ": " + suffixes[rand.Next(suffixes.Length)];
                    item.ToolTipText = $@"Další informace k této položce
byste našli v této bublině.
Číslo prvku: {n}
Obsah prvku: {item.Value1}
Pozice prvku: {nx}/{ny}
Souřadnice prvku: {itemX}/{itemY}";

                    item.BorderStyle = BorderStyleType.Soft;
                    if (nx == 1) item.TitleLabel.FontModifier.Bold = true;
                    if (nx == 2) item.TitleLabel.FontModifier.Italic = true;
                    if (nx == 2) item.TitleLabel.FontModifier.SizeRatio = 0.85f;
                    if (rand.Next(16) <= 2) item.ReadOnly = true;
                    if (nx == 3) item.RightActiveIcon = InteractiveIcon.RelationRecord;
                    if (nx == 4) item.RightActiveIcon = InteractiveIcon.RelationDocument;
                    if (nx == 6) item.RightActiveIcon = InteractiveIcon.Calculator;
                    if (nx == 7) item.RightActiveIcon = InteractiveIcon.OpenFolder;
                    if (nx == 9) item.RightActiveIcon = InteractiveIcon.Calendar;

                    item.Text1.Tag = $"Řádek [{ny}]; Sloupec [{nx}];{ Environment.NewLine}Výchozí hodnota: \"{item.Value1}\";{Environment.NewLine}";
                    item.Text1.RightIconClick += _TextRightIconClick;
                    item.Text1.TextDoubleClick += _TextDoubleClick;

                    // Zvětšení velikosti Tabu tak, aby zobrazil i nově přidaný item:
                    bounds = item.Bounds;
                    int tabR = bounds.Right + itemX0;
                    int tabB = bounds.Bottom + itemY0;
                    var tabSize = tab.Size;
                    int tabW = (tabR > tabSize.Width ? tabR : tabSize.Width);
                    int tabH = (tabB > tabSize.Height ? tabB : tabSize.Height);
                    if (tabW > tabSize.Width || tabH > tabSize.Height)
                        tab.Size = new Size(tabW, tabH);
                    var lineBounds = tab.TitleLine.Bounds;
                    if (bounds.Right > lineBounds.Right)
                        tab.TitleLine.Size = new Size(bounds.Right - lineBounds.Left, lineBounds.Height);

                    // Posun X pro další Item:
                    itemX = bounds.Right + itemXSpace;
                }

                // Posun Y pro další Item:
                itemY = bounds.Bottom + itemYSpace;
            }
        }
        private void _TextRightIconClick(object sender, GInteractiveChangeStateArgs e)
        {
            GTextEdit textEdit = sender as GTextEdit;
            string text = textEdit?.Tag as string;
            text = $"RightIcon.Click na prvku:{Environment.NewLine}{text}Aktuální hodnota: \"{textEdit.Text}\"; ";
            System.Windows.Forms.MessageBox.Show(text, "RightIconClick", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void _TextDoubleClick(object sender, GInteractiveChangeStateArgs e)
        {
            GTextEdit textEdit = sender as GTextEdit;
            string text = textEdit?.Tag as string;
            text = $"Text.DoubleClick na prvku:{Environment.NewLine}{text}Aktuální hodnota: \"{textEdit.Text}\"; ";
            System.Windows.Forms.MessageBox.Show(text, "TextDoubleClick", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
