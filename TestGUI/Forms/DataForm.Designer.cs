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
            this.StatusStripPanel = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.ToolStripPanel = new System.Windows.Forms.Panel();
            this.labelType = new System.Windows.Forms.Label();
            this.RunButton = new System.Windows.Forms.Button();
            this.labelValue = new System.Windows.Forms.Label();
            this.labelNumber = new System.Windows.Forms.Label();
            this.trackBarNumber = new System.Windows.Forms.TrackBar();
            this.radioButtonAsol = new System.Windows.Forms.RadioButton();
            this.radioButtonInfrag = new System.Windows.Forms.RadioButton();
            this.radioButtonDevExpr = new System.Windows.Forms.RadioButton();
            this.radioButtonWinForm = new System.Windows.Forms.RadioButton();
            this.TestContentPanel = new System.Windows.Forms.Panel();
            this.StatusStripPanel.SuspendLayout();
            this.ToolStripPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarNumber)).BeginInit();
            this.SuspendLayout();
            // 
            // StatusStripPanel
            // 
            this.StatusStripPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.StatusStripPanel.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripLabel1});
            this.StatusStripPanel.Location = new System.Drawing.Point(0, 323);
            this.StatusStripPanel.Name = "StatusStripPanel";
            this.StatusStripPanel.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.StatusStripPanel.Size = new System.Drawing.Size(734, 24);
            this.StatusStripPanel.TabIndex = 1;
            // 
            // StatusStripLabel1
            // 
            this.StatusStripLabel1.AutoSize = false;
            this.StatusStripLabel1.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.StatusStripLabel1.Name = "StatusStripLabel1";
            this.StatusStripLabel1.Size = new System.Drawing.Size(719, 19);
            this.StatusStripLabel1.Spring = true;
            this.StatusStripLabel1.Text = "DataForm sample";
            this.StatusStripLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ToolStripPanel
            // 
            this.ToolStripPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ToolStripPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ToolStripPanel.Controls.Add(this.labelType);
            this.ToolStripPanel.Controls.Add(this.RunButton);
            this.ToolStripPanel.Controls.Add(this.labelValue);
            this.ToolStripPanel.Controls.Add(this.labelNumber);
            this.ToolStripPanel.Controls.Add(this.trackBarNumber);
            this.ToolStripPanel.Controls.Add(this.radioButtonAsol);
            this.ToolStripPanel.Controls.Add(this.radioButtonInfrag);
            this.ToolStripPanel.Controls.Add(this.radioButtonDevExpr);
            this.ToolStripPanel.Controls.Add(this.radioButtonWinForm);
            this.ToolStripPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.ToolStripPanel.Name = "ToolStripPanel";
            this.ToolStripPanel.Size = new System.Drawing.Size(734, 86);
            this.ToolStripPanel.TabIndex = 3;
            // 
            // labelType
            // 
            this.labelType.AutoSize = true;
            this.labelType.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelType.Location = new System.Drawing.Point(7, 11);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(88, 17);
            this.labelType.TabIndex = 8;
            this.labelType.Text = "Druh prvků";
            // 
            // RunButton
            // 
            this.RunButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.RunButton.Location = new System.Drawing.Point(580, 11);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(132, 63);
            this.RunButton.TabIndex = 7;
            this.RunButton.Text = "Vygeneruj!";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // labelValue
            // 
            this.labelValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelValue.Location = new System.Drawing.Point(356, 9);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(87, 19);
            this.labelValue.TabIndex = 6;
            this.labelValue.Text = "000";
            this.labelValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelNumber
            // 
            this.labelNumber.AutoSize = true;
            this.labelNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelNumber.Location = new System.Drawing.Point(245, 11);
            this.labelNumber.Name = "labelNumber";
            this.labelNumber.Size = new System.Drawing.Size(94, 17);
            this.labelNumber.TabIndex = 5;
            this.labelNumber.Text = "Počet prvků";
            // 
            // trackBarNumber
            // 
            this.trackBarNumber.LargeChange = 20;
            this.trackBarNumber.Location = new System.Drawing.Point(238, 31);
            this.trackBarNumber.Maximum = 400;
            this.trackBarNumber.Minimum = 1;
            this.trackBarNumber.Name = "trackBarNumber";
            this.trackBarNumber.Size = new System.Drawing.Size(321, 45);
            this.trackBarNumber.SmallChange = 2;
            this.trackBarNumber.TabIndex = 4;
            this.trackBarNumber.TickFrequency = 10;
            this.trackBarNumber.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBarNumber.Value = 50;
            this.trackBarNumber.Scroll += new System.EventHandler(this.trackBarNumber_Scroll);
            // 
            // radioButtonAsol
            // 
            this.radioButtonAsol.AutoSize = true;
            this.radioButtonAsol.Location = new System.Drawing.Point(149, 54);
            this.radioButtonAsol.Name = "radioButtonAsol";
            this.radioButtonAsol.Size = new System.Drawing.Size(53, 17);
            this.radioButtonAsol.TabIndex = 3;
            this.radioButtonAsol.TabStop = true;
            this.radioButtonAsol.Text = "ASOL";
            this.radioButtonAsol.UseVisualStyleBackColor = true;
            this.radioButtonAsol.CheckedChanged += new System.EventHandler(this._RadioButtonCheckedChanged);
            // 
            // radioButtonInfrag
            // 
            this.radioButtonInfrag.AutoSize = true;
            this.radioButtonInfrag.Location = new System.Drawing.Point(149, 33);
            this.radioButtonInfrag.Name = "radioButtonInfrag";
            this.radioButtonInfrag.Size = new System.Drawing.Size(70, 17);
            this.radioButtonInfrag.TabIndex = 2;
            this.radioButtonInfrag.TabStop = true;
            this.radioButtonInfrag.Text = "Infragistic";
            this.radioButtonInfrag.UseVisualStyleBackColor = true;
            this.radioButtonInfrag.CheckedChanged += new System.EventHandler(this._RadioButtonCheckedChanged);
            // 
            // radioButtonDevExpr
            // 
            this.radioButtonDevExpr.AutoSize = true;
            this.radioButtonDevExpr.Location = new System.Drawing.Point(10, 54);
            this.radioButtonDevExpr.Name = "radioButtonDevExpr";
            this.radioButtonDevExpr.Size = new System.Drawing.Size(82, 17);
            this.radioButtonDevExpr.TabIndex = 1;
            this.radioButtonDevExpr.TabStop = true;
            this.radioButtonDevExpr.Text = "DevExpress";
            this.radioButtonDevExpr.UseVisualStyleBackColor = true;
            this.radioButtonDevExpr.CheckedChanged += new System.EventHandler(this._RadioButtonCheckedChanged);
            // 
            // radioButtonWinForm
            // 
            this.radioButtonWinForm.AutoSize = true;
            this.radioButtonWinForm.Location = new System.Drawing.Point(10, 33);
            this.radioButtonWinForm.Name = "radioButtonWinForm";
            this.radioButtonWinForm.Size = new System.Drawing.Size(100, 17);
            this.radioButtonWinForm.TabIndex = 0;
            this.radioButtonWinForm.TabStop = true;
            this.radioButtonWinForm.Text = "Windows.Forms";
            this.radioButtonWinForm.UseVisualStyleBackColor = true;
            this.radioButtonWinForm.CheckedChanged += new System.EventHandler(this._RadioButtonCheckedChanged);
            // 
            // TestContentPanel
            // 
            this.TestContentPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.TestContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TestContentPanel.Location = new System.Drawing.Point(0, 86);
            this.TestContentPanel.Name = "TestContentPanel";
            this.TestContentPanel.Size = new System.Drawing.Size(734, 237);
            this.TestContentPanel.TabIndex = 4;
            // 
            // DataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 650);
            this.Controls.Add(this.TestContentPanel);
            this.Controls.Add(this.ToolStripPanel);
            this.Controls.Add(this.StatusStripPanel);
            this.MinimumSize = new System.Drawing.Size(750, 386);
            this.Name = "DataForm";
            this.Text = "Ukázka controlu DataForm";
            this.StatusStripPanel.ResumeLayout(false);
            this.StatusStripPanel.PerformLayout();
            this.ToolStripPanel.ResumeLayout(false);
            this.ToolStripPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarNumber)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip StatusStripPanel;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabel1;
        private System.Windows.Forms.Panel ToolStripPanel;
        private System.Windows.Forms.RadioButton radioButtonAsol;
        private System.Windows.Forms.RadioButton radioButtonInfrag;
        private System.Windows.Forms.RadioButton radioButtonDevExpr;
        private System.Windows.Forms.RadioButton radioButtonWinForm;
        private System.Windows.Forms.Panel TestContentPanel;
        private System.Windows.Forms.Label labelValue;
        private System.Windows.Forms.Label labelNumber;
        private System.Windows.Forms.TrackBar trackBarNumber;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.Button RunButton;
    }
}