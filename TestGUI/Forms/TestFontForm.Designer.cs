namespace Asol.Tools.WorkScheduler.TestGUI
{
    partial class TestFontForm
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
            this._SplitMain = new System.Windows.Forms.SplitContainer();
            this._SplitDraw = new System.Windows.Forms.SplitContainer();
            this._Panel1 = new System.Windows.Forms.Panel();
            this._Panel2 = new System.Windows.Forms.Panel();
            this._InputText = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this._SplitMain)).BeginInit();
            this._SplitMain.Panel1.SuspendLayout();
            this._SplitMain.Panel2.SuspendLayout();
            this._SplitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._SplitDraw)).BeginInit();
            this._SplitDraw.Panel1.SuspendLayout();
            this._SplitDraw.Panel2.SuspendLayout();
            this._SplitDraw.SuspendLayout();
            this.SuspendLayout();
            // 
            // _SplitMain
            // 
            this._SplitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._SplitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._SplitMain.Location = new System.Drawing.Point(0, 0);
            this._SplitMain.Name = "_SplitMain";
            this._SplitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _SplitMain.Panel1
            // 
            this._SplitMain.Panel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this._SplitMain.Panel1.Controls.Add(this._InputText);
            // 
            // _SplitMain.Panel2
            // 
            this._SplitMain.Panel2.Controls.Add(this._SplitDraw);
            this._SplitMain.Size = new System.Drawing.Size(866, 509);
            this._SplitMain.SplitterDistance = 155;
            this._SplitMain.TabIndex = 0;
            // 
            // _SplitDraw
            // 
            this._SplitDraw.Dock = System.Windows.Forms.DockStyle.Fill;
            this._SplitDraw.Location = new System.Drawing.Point(0, 0);
            this._SplitDraw.Name = "_SplitDraw";
            // 
            // _SplitDraw.Panel1
            // 
            this._SplitDraw.Panel1.Controls.Add(this._Panel1);
            // 
            // _SplitDraw.Panel2
            // 
            this._SplitDraw.Panel2.Controls.Add(this._Panel2);
            this._SplitDraw.Size = new System.Drawing.Size(866, 350);
            this._SplitDraw.SplitterDistance = 430;
            this._SplitDraw.TabIndex = 0;
            // 
            // _Panel1
            // 
            this._Panel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this._Panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._Panel1.Location = new System.Drawing.Point(0, 0);
            this._Panel1.Name = "_Panel1";
            this._Panel1.Size = new System.Drawing.Size(430, 350);
            this._Panel1.TabIndex = 0;
            // 
            // _Panel2
            // 
            this._Panel2.BackColor = System.Drawing.SystemColors.ControlDark;
            this._Panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._Panel2.Location = new System.Drawing.Point(0, 0);
            this._Panel2.Name = "_Panel2";
            this._Panel2.Size = new System.Drawing.Size(432, 350);
            this._Panel2.TabIndex = 1;
            // 
            // _InputText
            // 
            this._InputText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._InputText.Location = new System.Drawing.Point(12, 12);
            this._InputText.Multiline = true;
            this._InputText.Name = "_InputText";
            this._InputText.Size = new System.Drawing.Size(842, 131);
            this._InputText.TabIndex = 0;
            // 
            // TestFontForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(866, 509);
            this.Controls.Add(this._SplitMain);
            this.Name = "TestFontForm";
            this.Text = "Test vykreslení fontu";
            this._SplitMain.Panel1.ResumeLayout(false);
            this._SplitMain.Panel1.PerformLayout();
            this._SplitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._SplitMain)).EndInit();
            this._SplitMain.ResumeLayout(false);
            this._SplitDraw.Panel1.ResumeLayout(false);
            this._SplitDraw.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._SplitDraw)).EndInit();
            this._SplitDraw.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer _SplitMain;
        private System.Windows.Forms.SplitContainer _SplitDraw;
        private System.Windows.Forms.Panel _Panel1;
        private System.Windows.Forms.Panel _Panel2;
        private System.Windows.Forms.TextBox _InputText;
    }
}