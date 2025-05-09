﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DW = System.Drawing;
using WF = System.Windows.Forms;

using DXE = DevExpress.XtraEditors;

namespace Asol.Tools.WorkScheduler.DevExpressTest
{
    public class DxManager
    {
        public static WF.Control CreateWinFormDataFormControl() { return new DevExpressDataForm(); }
        public static void AddDataFormItems(WF.Control control, int countX, int countY)
        {
            if (control is DevExpressDataForm)
                (control as DevExpressDataForm).AddDataFormItems(countX, countY);
        }
    }

    internal class DevExpressDataForm : DXE.XtraUserControl
    {
        public DevExpressDataForm()
        {
            this.AutoScroll = true;
            _TitleFontActive = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            _TitleFontInactive = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            _ItemFontActive = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            _ItemFontInactive = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
        }
        DW.Font _TitleFontActive;
        DW.Font _TitleFontInactive;
        DW.Font _ItemFontActive;
        DW.Font _ItemFontInactive;

        /// <summary>
        /// Naplní controly
        /// </summary>
        public void AddDataFormItems(int countX, int countY)
        {
            this.SuspendLayout();

            #region Barvy, texty, fonty
            int lo = 224;
            int hi = 228;
            DW.Color[] colors = new DW.Color[]
            {
                DW.Color.FromArgb(hi, lo, lo),
                DW.Color.FromArgb(lo, hi, lo),
                DW.Color.FromArgb(lo, lo, hi),
                DW.Color.FromArgb(hi, hi, lo),
                DW.Color.FromArgb(lo, hi, hi),
                DW.Color.FromArgb(hi, lo, hi),
                DW.Color.FromArgb(lo, lo, lo),
                DW.Color.FromArgb(hi, hi, hi)
            };
            int colorCnt = colors.Length;
            string[] values = new string[] { "VFA-2019", "VP", "DOC-INT", "PRJ", "VYD", "IUD-2019", "MAT", "ZFO", "ČJ" };
            string[] suffixes = new string[] { "vyřešit ihned", "porada nutná", "rozhoduje pouze šéf", "neřešit, založit", "utajený dokument", "Extra délka doplňkového textu, přesahující 32 znaků" };
            int valuesCnt = values.Length;
            Random rand = new Random();

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

            this.Controls.Clear();
            TitleLabelPanel tab = null;
            int tabIdx = 0;
            DW.Rectangle bounds = DW.Rectangle.Empty;
            int n = 0;
            int lastX = countX - 1;
            for (int ny = 0; ny < countY; ny++)
            {
                if (tab == null || (ny % 3) == 0)
                {
                    tabX = tabX0;
                    tabY = (tab != null ? tab.Bounds.Bottom + tabYSpace : tabY0);
                    tab = new TitleLabelPanel()
                    {
                        Location = new DW.Point(tabX, tabY)
                    };
                    tabIdx++;
                    tab.TitleLabel.Text = $"Titulek skupiny {tabIdx}...";
                    tab.TitleLabel.Bounds = new DW.Rectangle(itemX0, 2, 350, 20);
                    tab.TitleLabel.Appearance.FinalAlign(System.Drawing.ContentAlignment.MiddleLeft, System.Windows.Forms.RightToLeft.No);
                    tab.LabelFontActive = _TitleFontActive;
                    tab.LabelFontInactive = _TitleFontInactive;
                    tab.LineBounds = new DW.Rectangle(itemX0, 23, 700, 2);
                    tab.LineColor = DW.Color.FromArgb(192, 64, 80, 128);
                    tab.SuspendLayout();

                    this.Controls.Add(tab);
                    itemY = 28 + itemY0;
                }

                itemX = itemX0;
                for (int nx = 0; nx < countX; nx++)
                {
                    n++;

                    // Nadefinování prvku Item - texty, font, overlay:
                    TitleTextPanel item = new TitleTextPanel()
                    {
                        BackColor = colors[rand.Next(colorCnt)],
                        Location = new DW.Point(itemX, itemY)
                    };
                    item.TitleText = "Item " + n.ToString();
                    item.TitleLabel.Appearance.FinalAlign(System.Drawing.ContentAlignment.MiddleRight, System.Windows.Forms.RightToLeft.No);
                    item.ValueText = values[rand.Next(valuesCnt)] + "_" + rand.Next(10000, 99999).ToString();
                    item.LabelFontActive = _ItemFontActive;
                    item.LabelFontInactive = _ItemFontInactive;
                    tab.Controls.Add(item);

                    if (rand.Next(10) <= 3) item.ValueText += ": " + suffixes[rand.Next(suffixes.Length)];
                    string toolTip = $@"Další informace k této položce
byste našli v této bublině.
Číslo prvku: {n}
Obsah prvku: {item.ValueText}
Pozice prvku: {nx}/{ny}
Souřadnice prvku: {itemX}/{itemY}";

                    item.ValueTextBox.ToolTip = toolTip;

                    item.BorderStyle = WF.BorderStyle.None;
                    //if (nx == 1) item.TitleLabel.FontModifier.Bold = true;
                    //if (nx == 2) item.TitleLabel.FontModifier.Italic = true;
                    //if (nx == 2) item.TitleLabel.FontModifier.SizeRatio = 0.85f;
                    //if (rand.Next(16) <= 2) item.ReadOnly = true;
                    //if (nx == 3 || nx == 4 || nx == lastX)       // (rand.Next(10) > 6)
                    //    item.OverlayText = overlays[rand.Next(overlays.Length)];

                    // Zvětšení velikosti Tabu tak, aby zobrazil i nově přidaný item:
                    bounds = item.Bounds;
                    int tabR = bounds.Right + itemX0;
                    int tabB = bounds.Bottom + itemY0;
                    var tabSize = tab.Size;
                    int tabW = (tabR > tabSize.Width ? tabR : tabSize.Width);
                    int tabH = (tabB > tabSize.Height ? tabB : tabSize.Height);
                    if (tabW > tabSize.Width || tabH > tabSize.Height)
                        tab.Size = new DW.Size(tabW, tabH);

                    // Posun X pro další Item:
                    itemX = bounds.Right + itemXSpace;
                }

                // Posun Y pro další Item:
                itemY = bounds.Bottom + itemYSpace;
            }

            foreach (WF.Control childTab in this.Controls)
            {
                childTab.ResumeLayout(false);
                childTab.PerformLayout();
            }

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
    internal class TitleTextPanel : TitleLabelPanel
    {
        public TitleTextPanel()
        {
            this.ValueTextBox = new DevExpress.XtraEditors.TextEdit();

            this.SuspendLayout();

            this.TitleLabel.Bounds = new DW.Rectangle(6, 6, 100, 20);

            this.ValueTextBox.Location = new System.Drawing.Point(110, 8);
            this.ValueTextBox.Name = "TextBox";
            this.ValueTextBox.Size = new System.Drawing.Size(87, 19);
            this.ValueTextBox.TabIndex = 0;
            this.ValueTextBox.Text = "TextBox";
            // this.ValueTextBox.TextAlign = WF.HorizontalAlignment.Left;

            this.Controls.Add(this.ValueTextBox);

            this.Size = new DW.Size(220, 30);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        public string ValueText { get { return this.ValueTextBox.Text; } set { this.ValueTextBox.Text = value; } }
        public DXE.TextEdit ValueTextBox { get; private set; }
    }
    internal class TitleLabelPanel : DXE.XtraUserControl
    {
        public TitleLabelPanel()
        {
            this.DoubleBuffered = true;

            this.TitleLabel = new DevExpress.XtraEditors.LabelControl();

            this.SuspendLayout();

            this.TitleLabel.Location = new System.Drawing.Point(6, 4);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(87, 19);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "TitleLabel";
            // this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.Controls.Add(this.TitleLabel);

            this.Size = new DW.Size(400, 28);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (HasLabelFonts) TitleLabel.Font = LabelFontActive;
        }
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            if (HasLabelFonts) TitleLabel.Font = LabelFontInactive;
        }
        protected override void OnPaint(WF.PaintEventArgs e)
        {
            base.OnPaint(e);
            var bounds = LineBounds;
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                using (var brush = new DW.SolidBrush(this.LineColor))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
            }
        }
        protected bool HasLabelFonts { get { return (LabelFontActive != null && LabelFontInactive != null); } }
        public string TitleText { get { return this.TitleLabel.Text; } set { this.TitleLabel.Text = value; } }
        public DW.Font LabelFontActive { get; set; }
        public DW.Font LabelFontInactive { get; set; }
        public DXE.LabelControl TitleLabel { get; set; }
        public DW.Rectangle LineBounds { get; set; }
        public DW.Color LineColor { get; set; }
    }
}
