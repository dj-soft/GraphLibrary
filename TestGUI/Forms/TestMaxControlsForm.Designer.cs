namespace TestMaxControl
{
    partial class TestMaxControlsForm
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
            this.SplitContainer = new System.Windows.Forms.SplitContainer();
            this.OpgDaj = new System.Windows.Forms.RadioButton();
            this.OpgWin = new System.Windows.Forms.RadioButton();
            this.NumCtrlLbl = new System.Windows.Forms.Label();
            this.RunCreateBtn = new System.Windows.Forms.Button();
            this.NumCtrlTrack = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).BeginInit();
            this.SplitContainer.Panel1.SuspendLayout();
            this.SplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumCtrlTrack)).BeginInit();
            this.SuspendLayout();
            // 
            // SplitContainer
            // 
            this.SplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SplitContainer.Location = new System.Drawing.Point(0, 0);
            this.SplitContainer.Name = "SplitContainer";
            this.SplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // SplitContainer.Panel1
            // 
            this.SplitContainer.Panel1.Controls.Add(this.OpgDaj);
            this.SplitContainer.Panel1.Controls.Add(this.OpgWin);
            this.SplitContainer.Panel1.Controls.Add(this.NumCtrlLbl);
            this.SplitContainer.Panel1.Controls.Add(this.RunCreateBtn);
            this.SplitContainer.Panel1.Controls.Add(this.NumCtrlTrack);
            this.SplitContainer.Panel1.Controls.Add(this.label1);
            this.SplitContainer.Size = new System.Drawing.Size(1254, 649);
            this.SplitContainer.SplitterDistance = 83;
            this.SplitContainer.TabIndex = 0;
            // 
            // OpgDaj
            // 
            this.OpgDaj.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpgDaj.AutoSize = true;
            this.OpgDaj.Checked = true;
            this.OpgDaj.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.OpgDaj.Location = new System.Drawing.Point(1003, 56);
            this.OpgDaj.Name = "OpgDaj";
            this.OpgDaj.Size = new System.Drawing.Size(65, 20);
            this.OpgDaj.TabIndex = 5;
            this.OpgDaj.TabStop = true;
            this.OpgDaj.Text = "AplCtrl";
            this.OpgDaj.UseVisualStyleBackColor = true;
            // 
            // OpgWin
            // 
            this.OpgWin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpgWin.AutoSize = true;
            this.OpgWin.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.OpgWin.Location = new System.Drawing.Point(917, 56);
            this.OpgWin.Name = "OpgWin";
            this.OpgWin.Size = new System.Drawing.Size(80, 20);
            this.OpgWin.TabIndex = 4;
            this.OpgWin.Text = "WinForm";
            this.OpgWin.UseVisualStyleBackColor = true;
            // 
            // NumCtrlLbl
            // 
            this.NumCtrlLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.NumCtrlLbl.AutoSize = true;
            this.NumCtrlLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.NumCtrlLbl.Location = new System.Drawing.Point(1027, 20);
            this.NumCtrlLbl.Name = "NumCtrlLbl";
            this.NumCtrlLbl.Size = new System.Drawing.Size(40, 16);
            this.NumCtrlLbl.TabIndex = 3;
            this.NumCtrlLbl.Text = "2500";
            // 
            // RunCreateBtn
            // 
            this.RunCreateBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RunCreateBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.RunCreateBtn.Location = new System.Drawing.Point(1092, 12);
            this.RunCreateBtn.Name = "RunCreateBtn";
            this.RunCreateBtn.Size = new System.Drawing.Size(150, 36);
            this.RunCreateBtn.TabIndex = 2;
            this.RunCreateBtn.Text = "Vygeneruj !";
            this.RunCreateBtn.UseVisualStyleBackColor = true;
            this.RunCreateBtn.Click += new System.EventHandler(this.RunCreateBtn_Click);
            // 
            // NumCtrlTrack
            // 
            this.NumCtrlTrack.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NumCtrlTrack.LargeChange = 500;
            this.NumCtrlTrack.Location = new System.Drawing.Point(121, 7);
            this.NumCtrlTrack.Maximum = 20000;
            this.NumCtrlTrack.Minimum = 1;
            this.NumCtrlTrack.Name = "NumCtrlTrack";
            this.NumCtrlTrack.Size = new System.Drawing.Size(886, 45);
            this.NumCtrlTrack.SmallChange = 100;
            this.NumCtrlTrack.TabIndex = 1;
            this.NumCtrlTrack.Text = "XXXXYYXXXX";
            this.NumCtrlTrack.TickFrequency = 100;
            this.NumCtrlTrack.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.NumCtrlTrack.Value = 1000;
            this.NumCtrlTrack.ValueChanged += new System.EventHandler(this.NumCtrlTrack_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Počet controlů";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1254, 649);
            this.Controls.Add(this.SplitContainer);
            this.Name = "Form1";
            this.Text = "Test maxima controlů";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.SplitContainer.Panel1.ResumeLayout(false);
            this.SplitContainer.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).EndInit();
            this.SplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.NumCtrlTrack)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer SplitContainer;
        private System.Windows.Forms.Button RunCreateBtn;
        private System.Windows.Forms.TrackBar NumCtrlTrack;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label NumCtrlLbl;
        private System.Windows.Forms.RadioButton OpgDaj;
        private System.Windows.Forms.RadioButton OpgWin;
    }
}

