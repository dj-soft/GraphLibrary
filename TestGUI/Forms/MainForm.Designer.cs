namespace Djs.Common.TestGUI.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.gInteractiveControl1 = new Djs.Common.Components.GInteractiveControl();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1414, 46);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // statusStrip1
            // 
            this.statusStrip1.AutoSize = false;
            this.statusStrip1.Location = new System.Drawing.Point(0, 620);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1414, 38);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // gInteractiveControl1
            // 
            this.gInteractiveControl1.DefaultBackColor = System.Drawing.Color.PaleTurquoise;
            this.gInteractiveControl1.DefaultBorderColor = System.Drawing.Color.Black;
            this.gInteractiveControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gInteractiveControl1.Location = new System.Drawing.Point(0, 46);
            this.gInteractiveControl1.Name = "gInteractiveControl1";
            this.gInteractiveControl1.RepaintAllItems = false;
            this.gInteractiveControl1.Size = new System.Drawing.Size(1414, 574);
            this.gInteractiveControl1.TabIndex = 2;
            this.gInteractiveControl1.Text = "gInteractiveControl1";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1414, 658);
            this.Controls.Add(this.gInteractiveControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private Components.GInteractiveControl gInteractiveControl1;
    }
}