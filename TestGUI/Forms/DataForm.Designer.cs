namespace Asol.Tools.WorkScheduler.TestGUI
{
    partial class DataForm
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
            this.DataFormCtrl = new Asol.Tools.WorkScheduler.DataForm.GDataFormControl();    // GDataFormControl   /    DataFormContainer
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.ExitButton = new System.Windows.Forms.Button();
            this.StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // DataFormCtrl
            // 
            this.DataFormCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataFormCtrl.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.DataFormCtrl.Location = new System.Drawing.Point(12, 12);
            this.DataFormCtrl.Name = "DataFormCtrl";
            this.DataFormCtrl.Size = new System.Drawing.Size(811, 393);
            this.DataFormCtrl.TabIndex = 0;
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripLabel1});
            this.StatusStrip.Location = new System.Drawing.Point(0, 418);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.StatusStrip.Size = new System.Drawing.Size(1035, 22);
            this.StatusStrip.TabIndex = 1;
            // 
            // StatusStripLabel1
            // 
            this.StatusStripLabel1.AutoSize = false;
            this.StatusStripLabel1.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusStripLabel1.Name = "StatusStripLabel1";
            this.StatusStripLabel1.Size = new System.Drawing.Size(180, 17);
            this.StatusStripLabel1.Text = "DataForm sample";
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.ExitButton.Location = new System.Drawing.Point(840, 343);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(182, 61);
            this.ExitButton.TabIndex = 2;
            this.ExitButton.Text = "Konec";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // DataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1035, 440);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.DataFormCtrl);
            this.Name = "DataForm";
            this.Text = "Ukázka controlu DataForm";
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WorkScheduler.DataForm.GDataFormControl DataFormCtrl;          // GDataFormControl    /    DataFormContainer
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabel1;
        private System.Windows.Forms.Button ExitButton;
    }
}