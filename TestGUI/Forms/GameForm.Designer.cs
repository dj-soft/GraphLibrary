namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class GameForm
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
            this._GameControl = new Asol.Tools.WorkScheduler.GameComponents.GameControl();
            this.SuspendLayout();
            // 
            // _GameControl
            // 
            this._GameControl.AutoScroll = false;
            this._GameControl.AutoScrollMargin = new System.Drawing.Size(0, 0);
            this._GameControl.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this._GameControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._GameControl.Location = new System.Drawing.Point(0, 0);
            this._GameControl.Name = "_GameControl";
            this._GameControl.Size = new System.Drawing.Size(826, 333);
            this._GameControl.TabIndex = 0;
            this._GameControl.Text = "_GameControl";
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(826, 333);
            this.Controls.Add(this._GameControl);
            this.Name = "GameForm";
            this.Text = "GameForm";
            this.ResumeLayout(false);

        }

        #endregion

        private GameComponents.GameControl _GameControl;
    }
}