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
    public class GDataFormItem : InteractiveContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GDataFormItem()
        {
            this.BackColor = Skin.Control.ControlBackColor;

            this.LabelMain = new GLabel()
            {
                Text = "Popisek:",
                Bounds = new Rectangle(4, 4, 60, 20),
                Alignment = ContentAlignment.MiddleRight,
                PrepareToolTipInParent = true
            };
            this.AddItem(this.LabelMain);

            this.Text1 = new GTextEdit()
            {
                Bounds = new Rectangle(66, 4, 120, 20),
                Alignment = ContentAlignment.MiddleLeft,
                PrepareToolTipInParent = true
            };
            this.AddItem(this.Text1);

            this.Text2 = new GTextEdit()
            {
                Bounds = new Rectangle(66, 4, 0, 20),
                Alignment = ContentAlignment.MiddleLeft,
                PrepareToolTipInParent = true,
                Visible = false
            };
            this.AddItem(this.Text2);

            this.Size = new Size(218, 28);
        }
        /// <summary>
        /// Popisek
        /// </summary>
        public string Label { get { return this.LabelMain.Text; } set { this.LabelMain.Text = value; } }
        /// <summary>
        /// Hodnota prvku 1
        /// </summary>
        public string Value1 { get { return this.Text1.Text; } set { this.Text1.Text = value; } }
        /// <summary>
        /// Hodnota prvku 2
        /// </summary>
        public string Value2 { get { return this.Text2.Text; } set { this.Text2.Text = value; } }

        /// <summary>
        /// Typ vztahu - pro správné vykreslování (linka podtržení)
        /// </summary>
        public TextRelationType RelationType { get { return this.Text1.RelationType; } set { this.Text1.RelationType = value; this.Text2.RelationType = value; } }
        /// <summary>
        /// Typ borderu
        /// </summary>
        public BorderStyleType BorderStyle { get { return this.Text1.BorderStyle; } set { this.Text1.BorderStyle = value; this.Text2.BorderStyle = value; } }
        /// <summary>
        /// Je tento prvek Visible
        /// </summary>
        public bool Visible { get { return this.Is.Visible; } set { this.Is.Visible = value; } }
        /// <summary>
        /// Je tento prvek Enabled
        /// </summary>
        public bool Enabled { get { return this.Text1.Enabled; } set { this.LabelMain.Enabled = value; this.Text1.Enabled = value; this.Text2.Enabled = value; } }
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
        /// Hlavní label
        /// </summary>
        public GLabel LabelMain { get; private set; }
        /// <summary>
        /// První Textbox / Reference / Číslo
        /// </summary>
        public GTextEdit Text1 { get; private set; }
        /// <summary>
        /// Druhý Textbox / Název
        /// </summary>
        public GTextEdit Text2 { get; private set; }

    }
}
