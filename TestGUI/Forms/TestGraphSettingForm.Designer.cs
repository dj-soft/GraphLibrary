namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class TestGraphSettingForm
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
            this.TestPanel = new DoubleBufferPanel();
            this.SuspendLayout();
            // 
            // TestPanel
            // 
            this.TestPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TestPanel.Location = new System.Drawing.Point(0, 0);
            this.TestPanel.Name = "TestPanel";
            this.TestPanel.Size = new System.Drawing.Size(1008, 557);
            this.TestPanel.TabIndex = 0;
            this.TestPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.TestPanel_Paint);
            // 
            // TestGraphSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 557);
            this.Controls.Add(this.TestPanel);
            this.Name = "TestGraphSettingForm";
            this.Text = "TestGraphSettingForm";
            this.ResumeLayout(false);

        }

        #endregion

        private DoubleBufferPanel TestPanel;
    }
}