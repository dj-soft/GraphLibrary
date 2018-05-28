namespace Asol.Tools.WorkScheduler.Forms
{
    partial class EditorForm
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
            this._Editor = new TextParser.GuiRtfEditor();
            this.SuspendLayout();
            // 
            // _Editor
            // 
            this._Editor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._Editor.EditorEnabled = true;
            this._Editor.Location = new System.Drawing.Point(12, 12);
            this._Editor.Name = "_Editor";
            this._Editor.Size = new System.Drawing.Size(843, 511);
            this._Editor.TabIndex = 0;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 535);
            this.Controls.Add(this._Editor);
            this.Name = "EditorForm";
            this.Text = "EditorForm";
            this.ResumeLayout(false);

        }

        #endregion

        private TextParser.GuiRtfEditor _Editor;
    }
}