namespace Djs.Tools.Screenshots
{
    partial class TestForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestForm));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this._StatusSavePathBtn = new System.Windows.Forms.ToolStripMenuItem();
            this._StatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this._ScreenPanel = new System.Windows.Forms.Panel();
            this._HelpBox = new System.Windows.Forms.GroupBox();
            this._HelpHideChk = new System.Windows.Forms.CheckBox();
            this._HelpOkBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this._FrequencyTrack = new System.Windows.Forms.TrackBar();
            this._HelpShowBtn = new System.Windows.Forms.Button();
            this._SnapStopBtn = new System.Windows.Forms.Button();
            this._SnapRecBtn = new System.Windows.Forms.Button();
            this._SnapOneBtn = new System.Windows.Forms.Button();
            this._FrequencyLabel = new System.Windows.Forms.Label();
            this._ImagePanel = new Djs.Tools.Screenshots.Components.ImagePanel();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.statusStrip1.SuspendLayout();
            this._ScreenPanel.SuspendLayout();
            this._HelpBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._FrequencyTrack)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSplitButton1,
            this.toolStripDropDownButton1,
            this._StatusText});
            this.statusStrip1.Location = new System.Drawing.Point(0, 404);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(742, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._StatusSavePathBtn});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(96, 20);
            this.toolStripDropDownButton1.Text = "Ukládat do:";
            // 
            // _StatusSavePathBtn
            // 
            this._StatusSavePathBtn.Name = "_StatusSavePathBtn";
            this._StatusSavePathBtn.Size = new System.Drawing.Size(152, 22);
            this._StatusSavePathBtn.Text = "Ukládat do:";
            // 
            // _StatusText
            // 
            this._StatusText.BackColor = System.Drawing.SystemColors.ButtonFace;
            this._StatusText.Name = "_StatusText";
            this._StatusText.Size = new System.Drawing.Size(568, 17);
            this._StatusText.Spring = true;
            this._StatusText.Text = "toolStripStatusLabel1";
            this._StatusText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ScreenPanel
            // 
            this._ScreenPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._ScreenPanel.Controls.Add(this._HelpBox);
            this._ScreenPanel.Location = new System.Drawing.Point(12, 12);
            this._ScreenPanel.Name = "_ScreenPanel";
            this._ScreenPanel.Size = new System.Drawing.Size(611, 377);
            this._ScreenPanel.TabIndex = 4;
            // 
            // _HelpBox
            // 
            this._HelpBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._HelpBox.Controls.Add(this._HelpHideChk);
            this._HelpBox.Controls.Add(this._HelpOkBtn);
            this._HelpBox.Controls.Add(this.label1);
            this._HelpBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._HelpBox.ForeColor = System.Drawing.Color.Gold;
            this._HelpBox.Location = new System.Drawing.Point(27, 72);
            this._HelpBox.Name = "_HelpBox";
            this._HelpBox.Size = new System.Drawing.Size(548, 218);
            this._HelpBox.TabIndex = 0;
            this._HelpBox.TabStop = false;
            this._HelpBox.Text = " N Á V O D ";
            // 
            // _HelpHideChk
            // 
            this._HelpHideChk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._HelpHideChk.AutoSize = true;
            this._HelpHideChk.Location = new System.Drawing.Point(107, 176);
            this._HelpHideChk.Name = "_HelpHideChk";
            this._HelpHideChk.Size = new System.Drawing.Size(171, 19);
            this._HelpHideChk.TabIndex = 2;
            this._HelpHideChk.Text = "Příště už nezobrazovat";
            this.toolTip1.SetToolTip(this._HelpHideChk, "Při příštím spuštění aplikace nebude tento help zobrazen.\r\nToto okno lze zobrazit" +
        " ikonkou s písmenem (i) vpravo.");
            this._HelpHideChk.UseVisualStyleBackColor = true;
            // 
            // _HelpOkBtn
            // 
            this._HelpOkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._HelpOkBtn.ForeColor = System.Drawing.Color.Black;
            this._HelpOkBtn.Location = new System.Drawing.Point(394, 166);
            this._HelpOkBtn.Name = "_HelpOkBtn";
            this._HelpOkBtn.Size = new System.Drawing.Size(137, 36);
            this._HelpOkBtn.TabIndex = 1;
            this._HelpOkBtn.Text = "OK";
            this.toolTip1.SetToolTip(this._HelpOkBtn, "Zhasne tento návod");
            this._HelpOkBtn.UseVisualStyleBackColor = true;
            this._HelpOkBtn.Click += new System.EventHandler(this._HelpOkBtn_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(16, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(501, 146);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // _FrequencyTrack
            // 
            this._FrequencyTrack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._FrequencyTrack.AutoSize = false;
            this._FrequencyTrack.Location = new System.Drawing.Point(683, 128);
            this._FrequencyTrack.Maximum = 30;
            this._FrequencyTrack.Name = "_FrequencyTrack";
            this._FrequencyTrack.Orientation = System.Windows.Forms.Orientation.Vertical;
            this._FrequencyTrack.Size = new System.Drawing.Size(46, 146);
            this._FrequencyTrack.TabIndex = 10;
            this._FrequencyTrack.TickFrequency = 3;
            this._FrequencyTrack.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.toolTip1.SetToolTip(this._FrequencyTrack, "RYCHLOST:\r\nZadejte rychlost ukládání snímků v kontinuálním režimu\r\n(po stisknutí " +
        "kulatého červeného tlačítka RECORD)");
            this._FrequencyTrack.Scroll += new System.EventHandler(this._FrequencyTrack_Scroll);
            // 
            // _HelpShowBtn
            // 
            this._HelpShowBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._HelpShowBtn.FlatAppearance.BorderSize = 0;
            this._HelpShowBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._HelpShowBtn.Image = global::Djs.Tools.Screenshots.Properties.Resources.help_contents;
            this._HelpShowBtn.Location = new System.Drawing.Point(629, 128);
            this._HelpShowBtn.Name = "_HelpShowBtn";
            this._HelpShowBtn.Size = new System.Drawing.Size(28, 26);
            this._HelpShowBtn.TabIndex = 12;
            this.toolTip1.SetToolTip(this._HelpShowBtn, "Zobrazí okno s nápovědou k používání této aplikace");
            this._HelpShowBtn.UseVisualStyleBackColor = true;
            this._HelpShowBtn.Click += new System.EventHandler(this._HelpShowBtn_Click);
            // 
            // _SnapStopBtn
            // 
            this._SnapStopBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._SnapStopBtn.FlatAppearance.BorderSize = 0;
            this._SnapStopBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._SnapStopBtn.Image = global::Djs.Tools.Screenshots.Properties.Resources.media_playback_stop_8;
            this._SnapStopBtn.Location = new System.Drawing.Point(701, 96);
            this._SnapStopBtn.Name = "_SnapStopBtn";
            this._SnapStopBtn.Size = new System.Drawing.Size(28, 26);
            this._SnapStopBtn.TabIndex = 9;
            this.toolTip1.SetToolTip(this._SnapStopBtn, "STOP:\r\nZastaví kontinuální ukládání fotografií.");
            this._SnapStopBtn.UseVisualStyleBackColor = true;
            this._SnapStopBtn.Click += new System.EventHandler(this._SnapStopBtn_Click);
            // 
            // _SnapRecBtn
            // 
            this._SnapRecBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._SnapRecBtn.FlatAppearance.BorderSize = 0;
            this._SnapRecBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._SnapRecBtn.Image = global::Djs.Tools.Screenshots.Properties.Resources.media_record_6;
            this._SnapRecBtn.Location = new System.Drawing.Point(667, 96);
            this._SnapRecBtn.Name = "_SnapRecBtn";
            this._SnapRecBtn.Size = new System.Drawing.Size(28, 26);
            this._SnapRecBtn.TabIndex = 7;
            this.toolTip1.SetToolTip(this._SnapRecBtn, "NAHRÁVÁNÍ:\r\nUkládá sadu fotografií, rychlostí zadanou v posuvníku dole.");
            this._SnapRecBtn.UseVisualStyleBackColor = true;
            this._SnapRecBtn.Click += new System.EventHandler(this._SnapRecBtn_Click);
            // 
            // _SnapOneBtn
            // 
            this._SnapOneBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._SnapOneBtn.FlatAppearance.BorderSize = 0;
            this._SnapOneBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._SnapOneBtn.Image = global::Djs.Tools.Screenshots.Properties.Resources.view_nofullscreen_2;
            this._SnapOneBtn.Location = new System.Drawing.Point(629, 96);
            this._SnapOneBtn.Name = "_SnapOneBtn";
            this._SnapOneBtn.Size = new System.Drawing.Size(28, 26);
            this._SnapOneBtn.TabIndex = 6;
            this.toolTip1.SetToolTip(this._SnapOneBtn, "SNÍMEK:\r\nUloží jednu fotografii");
            this._SnapOneBtn.UseVisualStyleBackColor = true;
            this._SnapOneBtn.Click += new System.EventHandler(this._SnapOneBtn_Click);
            // 
            // _FrequencyLabel
            // 
            this._FrequencyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._FrequencyLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._FrequencyLabel.ForeColor = System.Drawing.Color.Gold;
            this._FrequencyLabel.Location = new System.Drawing.Point(629, 270);
            this._FrequencyLabel.Name = "_FrequencyLabel";
            this._FrequencyLabel.Size = new System.Drawing.Size(100, 23);
            this._FrequencyLabel.TabIndex = 11;
            this._FrequencyLabel.Text = "label1";
            this._FrequencyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _ImagePanel
            // 
            this._ImagePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._ImagePanel.Image = null;
            this._ImagePanel.Location = new System.Drawing.Point(629, 12);
            this._ImagePanel.Name = "_ImagePanel";
            this._ImagePanel.Size = new System.Drawing.Size(100, 78);
            this._ImagePanel.TabIndex = 5;
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
            this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(32, 20);
            this.toolStripSplitButton1.Text = "toolStripSplitButton1";
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkBlue;
            this.ClientSize = new System.Drawing.Size(742, 426);
            this.Controls.Add(this._HelpShowBtn);
            this.Controls.Add(this._FrequencyLabel);
            this.Controls.Add(this._FrequencyTrack);
            this.Controls.Add(this._SnapStopBtn);
            this.Controls.Add(this._SnapRecBtn);
            this.Controls.Add(this._SnapOneBtn);
            this.Controls.Add(this._ImagePanel);
            this.Controls.Add(this._ScreenPanel);
            this.Controls.Add(this.statusStrip1);
            this.MaximizeBox = false;
            this.Name = "TestForm";
            this.Text = "Screenshots";
            this.TopMost = true;
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this._ScreenPanel.ResumeLayout(false);
            this._HelpBox.ResumeLayout(false);
            this._HelpBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._FrequencyTrack)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Panel _ScreenPanel;
        private Components.ImagePanel _ImagePanel;
        private System.Windows.Forms.Button _SnapOneBtn;
        private System.Windows.Forms.Button _SnapRecBtn;
        private System.Windows.Forms.Button _SnapStopBtn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TrackBar _FrequencyTrack;
        private System.Windows.Forms.Label _FrequencyLabel;
        private System.Windows.Forms.GroupBox _HelpBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button _HelpOkBtn;
        private System.Windows.Forms.CheckBox _HelpHideChk;
        private System.Windows.Forms.Button _HelpShowBtn;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripStatusLabel _StatusText;
        private System.Windows.Forms.ToolStripMenuItem _StatusSavePathBtn;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
    }
}

