namespace Djs.Tools.WebDownloader.UI
{
    partial class PreviewForm
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
            this._PreviewTxt = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _PreviewTxt
            // 
            this._PreviewTxt.Dock = System.Windows.Forms.DockStyle.Fill;
            this._PreviewTxt.Location = new System.Drawing.Point(0, 0);
            this._PreviewTxt.MaxLength = 65535;
            this._PreviewTxt.Multiline = true;
            this._PreviewTxt.Name = "_PreviewTxt";
            this._PreviewTxt.ReadOnly = true;
            this._PreviewTxt.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._PreviewTxt.Size = new System.Drawing.Size(927, 410);
            this._PreviewTxt.TabIndex = 0;
            this._PreviewTxt.WordWrap = false;
            // 
            // PreviewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(927, 410);
            this.Controls.Add(this._PreviewTxt);
            this.MinimizeBox = false;
            this.Name = "PreviewForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Preview";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _PreviewTxt;
    }
}