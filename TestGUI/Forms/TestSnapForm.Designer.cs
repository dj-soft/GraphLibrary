namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class TestSnapForm
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
            this.configSnapPanel1 = new Asol.Tools.WorkScheduler.Scheduler.ConfigSnapPanel();
            this.configSnapPanel2 = new Asol.Tools.WorkScheduler.Scheduler.ConfigSnapPanel();
            this.SuspendLayout();
            // 
            // configSnapPanel1
            // 
            this.configSnapPanel1.Caption = "Title";
            this.configSnapPanel1.ImageType = Asol.Tools.WorkScheduler.Scheduler.ConfigSnapImageType.Sequence;
            this.configSnapPanel1.LineColorBottom = System.Drawing.Color.DarkBlue;
            this.configSnapPanel1.LineColorTop = System.Drawing.Color.Blue;
            this.configSnapPanel1.Location = new System.Drawing.Point(24, 12);
            this.configSnapPanel1.MaximumSize = new System.Drawing.Size(400, 128);
            this.configSnapPanel1.MinimumSize = new System.Drawing.Size(400, 128);
            this.configSnapPanel1.Name = "configSnapPanel1";
            this.configSnapPanel1.OnlyOneLine = false;
            this.configSnapPanel1.Size = new System.Drawing.Size(400, 128);
            this.configSnapPanel1.SnapActive = true;
            this.configSnapPanel1.SnapDistance = 16;
            this.configSnapPanel1.TabIndex = 1;
            this.configSnapPanel1.TextColor = System.Drawing.Color.Black;
            // 
            // configSnapPanel2
            // 
            this.configSnapPanel2.Caption = "Title";
            this.configSnapPanel2.ImageType = Asol.Tools.WorkScheduler.Scheduler.ConfigSnapImageType.InnerItem;
            this.configSnapPanel2.LineColorBottom = System.Drawing.Color.DarkBlue;
            this.configSnapPanel2.LineColorTop = System.Drawing.Color.Blue;
            this.configSnapPanel2.Location = new System.Drawing.Point(24, 146);
            this.configSnapPanel2.MaximumSize = new System.Drawing.Size(2048, 2048);
            this.configSnapPanel2.MinimumSize = new System.Drawing.Size(10, 26);
            this.configSnapPanel2.Name = "configSnapPanel2";
            this.configSnapPanel2.OnlyOneLine = false;
            this.configSnapPanel2.Size = new System.Drawing.Size(400, 128);
            this.configSnapPanel2.SnapActive = true;
            this.configSnapPanel2.SnapDistance = 16;
            this.configSnapPanel2.TabIndex = 3;
            this.configSnapPanel2.TextColor = System.Drawing.Color.Black;
            // 
            // TestSnapForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(865, 755);
            this.Controls.Add(this.configSnapPanel2);
            this.Controls.Add(this.configSnapPanel1);
            this.Name = "TestSnapForm";
            this.Text = "TestSnapForm";
            this.ResumeLayout(false);

        }

        #endregion

        private Scheduler.ConfigSnapPanel configSnapPanel1;
        private Scheduler.ConfigSnapPanel configSnapPanel2;
    }
}