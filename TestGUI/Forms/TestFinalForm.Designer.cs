namespace Asol.Tools.WorkScheduler.TestGUI
{
    partial class TestFinalForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestFinalForm));
            this.mainControl1 = new Asol.Tools.WorkScheduler.Scheduler.MainControl();
            this.SuspendLayout();
            // 
            // mainControl1
            // 
            this.mainControl1.DefaultBackColor = System.Drawing.Color.LightBlue;
            this.mainControl1.DefaultBorderColor = System.Drawing.Color.Black;
            this.mainControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainControl1.Location = new System.Drawing.Point(0, 0);
            this.mainControl1.Name = "mainControl1";
            this.mainControl1.RepaintAllItems = false;
            this.mainControl1.Size = new System.Drawing.Size(928, 387);
            this.mainControl1.TabIndex = 0;
            this.mainControl1.Text = "mainControl1";
            // 
            // TestFinalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(928, 387);
            this.Controls.Add(this.mainControl1);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TestFinalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Dílenská tabule v.0";
            this.ResumeLayout(false);

        }

        #endregion

        private Scheduler.MainControl mainControl1;
    }
}