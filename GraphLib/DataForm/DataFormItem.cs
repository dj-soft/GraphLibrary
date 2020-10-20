using Asol.Tools.WorkScheduler.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.DataForm
{
    /// <summary>
    /// Jeden prvek v DataFormu
    /// </summary>
    public class GDataFormItem : InteractiveLabeledContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GDataFormItem()
        {
            this.TitleLabel.Text = "Popisek:";
            this.TitleLabel.Bounds = new Rectangle(4, 4, 95, 20);
            this.TitleLabel.Alignment = ContentAlignment.MiddleRight;
            this.TitleLabel.PrepareToolTipInParent = true;
            this.TitleLabel.Visible = true;

            this.Text1 = new GTextEdit()
            {
                Bounds = new Rectangle(104, 4, 120, 20),
                Alignment = ContentAlignment.MiddleLeft,
                PrepareToolTipInParent = true
            };
            this.AddItem(this.Text1);

            this.Text2 = new GTextEdit()
            {
                Bounds = new Rectangle(228, 4, 250, 20),
                Alignment = ContentAlignment.MiddleLeft,
                PrepareToolTipInParent = true,
                Visible = false
            };
            this.AddItem(this.Text2);

            this.Size = new Size(240, 28);
            // this.TitleLine.Bounds = new Rectangle(4, 1, 232, 3);
        }
        /// <summary>
        /// Popisek
        /// </summary>
        public string Label { get { return this.TitleLabel.Text; } set { this.TitleLabel.Text = value; } }
        /// <summary>
        /// Hodnota prvku 1
        /// </summary>
        public string Value1 { get { return this.Text1.Text; } set { this.Text1.Text = value; } }
        /// <summary>
        /// Hodnota prvku 2
        /// </summary>
        public string Value2 { get { return this.Text2.Text; } set { this.Text2.Text = value; } }
        /// <summary>
        /// Přídavné vykreslení přes Background, pod text
        /// </summary>
        public ITextEditOverlay OverlayBackground { get { return this.Text1.OverlayBackground; } set { this.Text1.OverlayBackground = value; this.Text2.OverlayBackground = value; } }
        /// <summary>
        /// Přídavné vykreslení přes Text
        /// </summary>
        public ITextEditOverlay OverlayText { get { return this.Text1.OverlayText; } set { this.Text1.OverlayText = value; this.Text2.OverlayText = value; } }
        /// <summary>
        /// Ikona vpravo
        /// </summary>
        public InteractiveIcon RightActiveIcon { get { return this.Text1.RightActiveIcon; } set { this.Text1.RightActiveIcon = value; this.Text2.RightActiveIcon = value; } }
        /// <summary>
        /// Typ borderu
        /// </summary>
        public BorderStyleType BorderStyle { get { return this.Text1.BorderStyle.Value; } set { this.Text1.BorderStyle = value; this.Text2.BorderStyle = value; } }
        /// <summary>
        /// Je tento prvek Visible
        /// </summary>
        public bool Visible { get { return this.Is.Visible; } set { this.Is.Visible = value; } }
        /// <summary>
        /// Je tento prvek Enabled?
        /// Do prvku, který NENÍ Enabled, nelze vstoupit Focusem (ani provést DoubleClick ani na ikoně / overlay).
        /// </summary>
        public bool Enabled { get { return this.Text1.Enabled; } set { this.TitleLabel.Enabled = value; this.Text1.Enabled = value; this.Text2.Enabled = value; } }
        /// <summary>
        /// Je tento prvek ReadOnly?
        /// Do prvku, který JE ReadOnly, lze vstoupit Focusem, lze provést DoubleClick včetně ikony / overlay.
        /// Ale nelze prvek editovat, a má vzhled prvku který není Enabled (=typicky má šedou barvu a nereaguje vizuálně na myš).
        /// </summary>
        public bool ReadOnly { get { return this.Text1.ReadOnly; } set { this.TitleLabel.Enabled = value; this.Text1.ReadOnly = value; this.Text2.ReadOnly = value; } }
        /// <summary>
        /// ToolTip text
        /// </summary>
        public string ToolTipText { get; set; }
        /// <summary>
        /// Připraví ToolTip
        /// </summary>
        /// <param name="e"></param>
        public override void PrepareToolTip(GInteractiveChangeStateArgs e)
        {
            string toolTipText = this.ToolTipText;
            if (!String.IsNullOrEmpty(toolTipText))
            {
                e.ToolTipData.IconType = IconImageType.Info;
                e.ToolTipData.TitleText = "Informace k prvku";
                e.ToolTipData.InfoText = toolTipText;
            }
        }
        /// <summary>
        /// První Textbox / Reference / Číslo
        /// </summary>
        public GTextEdit Text1 { get; private set; }
        /// <summary>
        /// Druhý Textbox / Název
        /// </summary>
        public GTextEdit Text2 { get; private set; }

        #region Interaktivita
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);

        }
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
        }
        #endregion

    }
}
