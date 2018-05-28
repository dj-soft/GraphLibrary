namespace Asol.Tools.WorkScheduler.TestGUI
{
    partial class TestFormNew
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
            this.GControl = new Asol.Tools.WorkScheduler.Components.GInteractiveControl();
            this.SuspendLayout();
            // 
            // GControl
            // 
            this.GControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GControl.Location = new System.Drawing.Point(12, 12);
            this.GControl.Name = "GControl";
            this.GControl.RepaintAllItems = false;
            this.GControl.Size = new System.Drawing.Size(858, 434);
            this.GControl.TabIndex = 0;
            // 
            // TestFormNew
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1031, 458);
            this.Controls.Add(this.GControl);
            this.Name = "TestFormNew";
            this.Text = "TestFormNew";
            this.ResumeLayout(false);

        }

        #endregion

        private Components.GInteractiveControl GControl;

    }
}