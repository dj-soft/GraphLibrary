
namespace DjSoft.Support.WinShutDown
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.TimeText = new System.Windows.Forms.MaskedTextBox();
            this.TimeLabel = new System.Windows.Forms.Label();
            this.OkButton = new System.Windows.Forms.Button();
            this.StopButton = new System.Windows.Forms.Button();
            this.TimePicture = new System.Windows.Forms.PictureBox();
            this.TimeCombo = new System.Windows.Forms.ComboBox();
            this.ModeCombo = new System.Windows.Forms.ComboBox();
            this.TimeTitle = new System.Windows.Forms.Label();
            this.ModeTitle = new System.Windows.Forms.Label();
            this.StatusText = new System.Windows.Forms.TextBox();
            this.TimeRemainingText = new System.Windows.Forms.TextBox();
            this.InactivityTrack = new System.Windows.Forms.TrackBar();
            this.TimeValueTitle = new System.Windows.Forms.Label();
            this.InactivityLabel = new System.Windows.Forms.Label();
            this.TopMostCheck = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.TimePicture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.InactivityTrack)).BeginInit();
            this.SuspendLayout();
            // 
            // TimeText
            // 
            this.TimeText.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TimeText.Location = new System.Drawing.Point(460, 34);
            this.TimeText.Margin = new System.Windows.Forms.Padding(4);
            this.TimeText.Mask = "00:00";
            this.TimeText.Name = "TimeText";
            this.TimeText.PromptChar = ' ';
            this.TimeText.Size = new System.Drawing.Size(72, 29);
            this.TimeText.TabIndex = 3;
            this.TimeText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.TimeText.ValidatingType = typeof(System.DateTime);
            this.TimeText.Visible = false;
            // 
            // TimeLabel
            // 
            this.TimeLabel.AutoSize = true;
            this.TimeLabel.Location = new System.Drawing.Point(536, 37);
            this.TimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.TimeLabel.Name = "TimeLabel";
            this.TimeLabel.Size = new System.Drawing.Size(69, 21);
            this.TimeLabel.TabIndex = 5;
            this.TimeLabel.Text = "Hod:Min";
            this.TimeLabel.Visible = false;
            // 
            // OkButton
            // 
            this.OkButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.OkButton.Image = global::DjSoft.Support.WinShutDown.Properties.Resources.arrow_right_2;
            this.OkButton.Location = new System.Drawing.Point(624, 34);
            this.OkButton.Margin = new System.Windows.Forms.Padding(4);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(198, 89);
            this.OkButton.TabIndex = 7;
            this.OkButton.Text = "OK";
            this.OkButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.OkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // StopButton
            // 
            this.StopButton.Enabled = false;
            this.StopButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.StopButton.Image = global::DjSoft.Support.WinShutDown.Properties.Resources.dialog_cancel_3;
            this.StopButton.Location = new System.Drawing.Point(611, 84);
            this.StopButton.Margin = new System.Windows.Forms.Padding(4);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(211, 39);
            this.StopButton.TabIndex = 8;
            this.StopButton.Text = "Zrušit";
            this.StopButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StopButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // TimePicture
            // 
            this.TimePicture.Image = global::DjSoft.Support.WinShutDown.Properties.Resources.ktimer_2;
            this.TimePicture.Location = new System.Drawing.Point(9, 24);
            this.TimePicture.Margin = new System.Windows.Forms.Padding(4);
            this.TimePicture.Name = "TimePicture";
            this.TimePicture.Size = new System.Drawing.Size(96, 99);
            this.TimePicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.TimePicture.TabIndex = 9;
            this.TimePicture.TabStop = false;
            // 
            // TimeCombo
            // 
            this.TimeCombo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TimeCombo.FormattingEnabled = true;
            this.TimeCombo.Location = new System.Drawing.Point(126, 34);
            this.TimeCombo.Margin = new System.Windows.Forms.Padding(4);
            this.TimeCombo.Name = "TimeCombo";
            this.TimeCombo.Size = new System.Drawing.Size(325, 29);
            this.TimeCombo.TabIndex = 11;
            // 
            // ModeCombo
            // 
            this.ModeCombo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ModeCombo.FormattingEnabled = true;
            this.ModeCombo.Location = new System.Drawing.Point(126, 94);
            this.ModeCombo.Margin = new System.Windows.Forms.Padding(4);
            this.ModeCombo.Name = "ModeCombo";
            this.ModeCombo.Size = new System.Drawing.Size(325, 29);
            this.ModeCombo.TabIndex = 12;
            // 
            // TimeTitle
            // 
            this.TimeTitle.AutoSize = true;
            this.TimeTitle.Location = new System.Drawing.Point(136, 9);
            this.TimeTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.TimeTitle.Name = "TimeTitle";
            this.TimeTitle.Size = new System.Drawing.Size(142, 21);
            this.TimeTitle.TabIndex = 13;
            this.TimeTitle.Text = "Kdy se má vypnout";
            // 
            // ModeTitle
            // 
            this.ModeTitle.AutoSize = true;
            this.ModeTitle.Location = new System.Drawing.Point(136, 69);
            this.ModeTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ModeTitle.Name = "ModeTitle";
            this.ModeTitle.Size = new System.Drawing.Size(138, 21);
            this.ModeTitle.TabIndex = 14;
            this.ModeTitle.Text = "Jak se má vypnout";
            // 
            // StatusText
            // 
            this.StatusText.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.StatusText.Location = new System.Drawing.Point(126, 137);
            this.StatusText.Name = "StatusText";
            this.StatusText.Size = new System.Drawing.Size(696, 26);
            this.StatusText.TabIndex = 16;
            this.StatusText.TabStop = false;
            this.StatusText.Enter += new System.EventHandler(this.NonActiveControl_Enter);
            // 
            // TimeRemainingText
            // 
            this.TimeRemainingText.BackColor = System.Drawing.Color.MidnightBlue;
            this.TimeRemainingText.Font = new System.Drawing.Font("Minecart LCD", 28F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TimeRemainingText.ForeColor = System.Drawing.Color.Chartreuse;
            this.TimeRemainingText.Location = new System.Drawing.Point(611, 14);
            this.TimeRemainingText.Name = "TimeRemainingText";
            this.TimeRemainingText.Size = new System.Drawing.Size(210, 63);
            this.TimeRemainingText.TabIndex = 17;
            this.TimeRemainingText.TabStop = false;
            this.TimeRemainingText.Text = "00:00";
            this.TimeRemainingText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.TimeRemainingText.Enter += new System.EventHandler(this.NonActiveControl_Enter);
            // 
            // InactivityTrack
            // 
            this.InactivityTrack.Location = new System.Drawing.Point(459, 32);
            this.InactivityTrack.Name = "InactivityTrack";
            this.InactivityTrack.Size = new System.Drawing.Size(146, 45);
            this.InactivityTrack.TabIndex = 18;
            this.InactivityTrack.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.InactivityTrack.Visible = false;
            // 
            // TimeValueTitle
            // 
            this.TimeValueTitle.AutoSize = true;
            this.TimeValueTitle.Location = new System.Drawing.Point(463, 9);
            this.TimeValueTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.TimeValueTitle.Name = "TimeValueTitle";
            this.TimeValueTitle.Size = new System.Drawing.Size(56, 21);
            this.TimeValueTitle.TabIndex = 19;
            this.TimeValueTitle.Text = "KB/sec";
            this.TimeValueTitle.Visible = false;
            // 
            // InactivityLabel
            // 
            this.InactivityLabel.Location = new System.Drawing.Point(460, 69);
            this.InactivityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.InactivityLabel.Name = "InactivityLabel";
            this.InactivityLabel.Size = new System.Drawing.Size(143, 26);
            this.InactivityLabel.TabIndex = 20;
            this.InactivityLabel.Text = "KB/sec";
            this.InactivityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.InactivityLabel.Visible = false;
            // 
            // TopMostCheck
            // 
            this.TopMostCheck.AutoSize = true;
            this.TopMostCheck.Location = new System.Drawing.Point(11, 135);
            this.TopMostCheck.Name = "TopMostCheck";
            this.TopMostCheck.Size = new System.Drawing.Size(92, 25);
            this.TopMostCheck.TabIndex = 21;
            this.TopMostCheck.TabStop = false;
            this.TopMostCheck.Text = "Top Most";
            this.TopMostCheck.UseVisualStyleBackColor = true;
            this.TopMostCheck.CheckedChanged += new System.EventHandler(this.TopMostCheck_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(852, 173);
            this.Controls.Add(this.TopMostCheck);
            this.Controls.Add(this.InactivityLabel);
            this.Controls.Add(this.TimeValueTitle);
            this.Controls.Add(this.TimeRemainingText);
            this.Controls.Add(this.StatusText);
            this.Controls.Add(this.ModeTitle);
            this.Controls.Add(this.TimeTitle);
            this.Controls.Add(this.ModeCombo);
            this.Controls.Add(this.TimeCombo);
            this.Controls.Add(this.TimeLabel);
            this.Controls.Add(this.TimeText);
            this.Controls.Add(this.TimePicture);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.InactivityTrack);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vypni stroj...";
            ((System.ComponentModel.ISupportInitialize)(this.TimePicture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.InactivityTrack)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MaskedTextBox TimeText;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button StopButton;
        private System.Windows.Forms.PictureBox TimePicture;
        private System.Windows.Forms.Label TimeLabel;
        private System.Windows.Forms.ComboBox TimeCombo;
        private System.Windows.Forms.ComboBox ModeCombo;
        private System.Windows.Forms.Label TimeTitle;
        private System.Windows.Forms.Label ModeTitle;
        private System.Windows.Forms.TextBox StatusText;
        private System.Windows.Forms.TextBox TimeRemainingText;
        private System.Windows.Forms.TrackBar InactivityTrack;
        private System.Windows.Forms.Label TimeValueTitle;
        private System.Windows.Forms.Label InactivityLabel;
        private System.Windows.Forms.CheckBox TopMostCheck;
    }
}

