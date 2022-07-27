
namespace DjSoft.SchedulerMap.Analyser
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._ProgressText = new System.Windows.Forms.TextBox();
            this._StatusBar = new System.Windows.Forms.StatusStrip();
            this._StatusProcessText = new System.Windows.Forms.ToolStripStatusLabel();
            this._StatusAnalyserText = new System.Windows.Forms.ToolStripStatusLabel();
            this._Timer = new System.Windows.Forms.Timer(this.components);
            this._OnlyProcessedCheck = new System.Windows.Forms.CheckBox();
            this._SimulCycleText = new System.Windows.Forms.NumericUpDown();
            this._SimulCycleLabel = new System.Windows.Forms.Label();
            this._FileText = new System.Windows.Forms.TextBox();
            this._ByProductCheck = new System.Windows.Forms.CheckBox();
            this._FileButton = new System.Windows.Forms.Button();
            this._StopButton = new System.Windows.Forms.Button();
            this._AnalyseStartButton = new System.Windows.Forms.Button();
            this._VisualiserButton = new System.Windows.Forms.Button();
            this._AnalyseTitleLabel = new System.Windows.Forms.Label();
            this._MapTitleLabel = new System.Windows.Forms.Label();
            this._ReloadFileCheck = new System.Windows.Forms.CheckBox();
            this._MainPanel = new System.Windows.Forms.Panel();
            this._VisualiserPanel = new DjSoft.SchedulerMap.Analyser.VisualiserControl();
            this._StatusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._SimulCycleText)).BeginInit();
            this._MainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _ProgressText
            // 
            this._ProgressText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._ProgressText.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._ProgressText.Location = new System.Drawing.Point(29, 14);
            this._ProgressText.Multiline = true;
            this._ProgressText.Name = "_ProgressText";
            this._ProgressText.ReadOnly = true;
            this._ProgressText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._ProgressText.Size = new System.Drawing.Size(441, 505);
            this._ProgressText.TabIndex = 9;
            this._ProgressText.WordWrap = false;
            // 
            // _StatusBar
            // 
            this._StatusBar.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._StatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._StatusProcessText,
            this._StatusAnalyserText});
            this._StatusBar.Location = new System.Drawing.Point(0, 604);
            this._StatusBar.Name = "_StatusBar";
            this._StatusBar.Size = new System.Drawing.Size(1307, 22);
            this._StatusBar.TabIndex = 3;
            this._StatusBar.Text = "_StatusBar";
            // 
            // _StatusProcessText
            // 
            this._StatusProcessText.AutoSize = false;
            this._StatusProcessText.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this._StatusProcessText.Name = "_StatusProcessText";
            this._StatusProcessText.Size = new System.Drawing.Size(250, 17);
            this._StatusProcessText.Text = "_StatusProcessText";
            this._StatusProcessText.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _StatusAnalyserText
            // 
            this._StatusAnalyserText.Name = "_StatusAnalyserText";
            this._StatusAnalyserText.Size = new System.Drawing.Size(1042, 17);
            this._StatusAnalyserText.Spring = true;
            this._StatusAnalyserText.Text = "_StatusAnalyserText";
            this._StatusAnalyserText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _Timer
            // 
            this._Timer.Interval = 500;
            this._Timer.Tick += new System.EventHandler(this._Timer_Tick);
            // 
            // _OnlyProcessedCheck
            // 
            this._OnlyProcessedCheck.Location = new System.Drawing.Point(13, 54);
            this._OnlyProcessedCheck.Name = "_OnlyProcessedCheck";
            this._OnlyProcessedCheck.Size = new System.Drawing.Size(192, 31);
            this._OnlyProcessedCheck.TabIndex = 2;
            this._OnlyProcessedCheck.Text = "Pouze zpracované položky";
            this._OnlyProcessedCheck.UseVisualStyleBackColor = true;
            // 
            // _SimulCycleText
            // 
            this._SimulCycleText.Location = new System.Drawing.Point(143, 176);
            this._SimulCycleText.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this._SimulCycleText.Name = "_SimulCycleText";
            this._SimulCycleText.Size = new System.Drawing.Size(54, 20);
            this._SimulCycleText.TabIndex = 6;
            this._SimulCycleText.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _SimulCycleLabel
            // 
            this._SimulCycleLabel.AutoSize = true;
            this._SimulCycleLabel.Location = new System.Drawing.Point(29, 178);
            this._SimulCycleLabel.Name = "_SimulCycleLabel";
            this._SimulCycleLabel.Size = new System.Drawing.Size(92, 13);
            this._SimulCycleLabel.TabIndex = 5;
            this._SimulCycleLabel.Text = "Simuluj zacyklení:";
            // 
            // _FileText
            // 
            this._FileText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._FileText.Location = new System.Drawing.Point(210, 12);
            this._FileText.Name = "_FileText";
            this._FileText.Size = new System.Drawing.Size(1083, 20);
            this._FileText.TabIndex = 2;
            // 
            // _ByProductCheck
            // 
            this._ByProductCheck.Location = new System.Drawing.Point(13, 144);
            this._ByProductCheck.Name = "_ByProductCheck";
            this._ByProductCheck.Size = new System.Drawing.Size(192, 31);
            this._ByProductCheck.TabIndex = 4;
            this._ByProductCheck.Text = "Mapuj i Vedlejší produkty";
            this._ByProductCheck.UseVisualStyleBackColor = true;
            // 
            // _FileButton
            // 
            this._FileButton.Image = global::DjSoft.SchedulerMap.Analyser.Properties.Resources.folder_blue_24;
            this._FileButton.Location = new System.Drawing.Point(13, 12);
            this._FileButton.Name = "_FileButton";
            this._FileButton.Size = new System.Drawing.Size(184, 36);
            this._FileButton.TabIndex = 1;
            this._FileButton.Text = "Soubor...";
            this._FileButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._FileButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this._FileButton.UseVisualStyleBackColor = true;
            this._FileButton.Click += new System.EventHandler(this._FileButton_Click);
            // 
            // _StopButton
            // 
            this._StopButton.Image = global::DjSoft.SchedulerMap.Analyser.Properties.Resources.media_playback_stop_2_24;
            this._StopButton.Location = new System.Drawing.Point(13, 455);
            this._StopButton.Name = "_StopButton";
            this._StopButton.Size = new System.Drawing.Size(184, 44);
            this._StopButton.TabIndex = 8;
            this._StopButton.Text = "STOP";
            this._StopButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._StopButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this._StopButton.UseVisualStyleBackColor = true;
            this._StopButton.Click += new System.EventHandler(this._StopClick);
            // 
            // _AnalyseStartButton
            // 
            this._AnalyseStartButton.Image = global::DjSoft.SchedulerMap.Analyser.Properties.Resources.go_next_2_24;
            this._AnalyseStartButton.Location = new System.Drawing.Point(13, 217);
            this._AnalyseStartButton.Name = "_AnalyseStartButton";
            this._AnalyseStartButton.Size = new System.Drawing.Size(184, 44);
            this._AnalyseStartButton.TabIndex = 7;
            this._AnalyseStartButton.Text = "ANALYZUJ";
            this._AnalyseStartButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._AnalyseStartButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this._AnalyseStartButton.UseVisualStyleBackColor = true;
            this._AnalyseStartButton.Click += new System.EventHandler(this._AnalyseStartClick);
            // 
            // _VisualiserButton
            // 
            this._VisualiserButton.Image = global::DjSoft.SchedulerMap.Analyser.Properties.Resources.fsview_24;
            this._VisualiserButton.Location = new System.Drawing.Point(13, 359);
            this._VisualiserButton.Name = "_VisualiserButton";
            this._VisualiserButton.Size = new System.Drawing.Size(184, 44);
            this._VisualiserButton.TabIndex = 10;
            this._VisualiserButton.Text = "ZOBRAZ";
            this._VisualiserButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._VisualiserButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this._VisualiserButton.UseVisualStyleBackColor = true;
            this._VisualiserButton.Click += new System.EventHandler(this._VisualiserButton_Click);
            // 
            // _AnalyseTitleLabel
            // 
            this._AnalyseTitleLabel.AutoSize = true;
            this._AnalyseTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._AnalyseTitleLabel.Location = new System.Drawing.Point(29, 126);
            this._AnalyseTitleLabel.Name = "_AnalyseTitleLabel";
            this._AnalyseTitleLabel.Size = new System.Drawing.Size(110, 15);
            this._AnalyseTitleLabel.TabIndex = 11;
            this._AnalyseTitleLabel.Text = "ANALÝZA MAPY:";
            // 
            // _MapTitleLabel
            // 
            this._MapTitleLabel.AutoSize = true;
            this._MapTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._MapTitleLabel.Location = new System.Drawing.Point(29, 290);
            this._MapTitleLabel.Name = "_MapTitleLabel";
            this._MapTitleLabel.Size = new System.Drawing.Size(128, 15);
            this._MapTitleLabel.TabIndex = 12;
            this._MapTitleLabel.Text = "ZOBRAZENÍ MAPY:";
            // 
            // _ReloadFileCheck
            // 
            this._ReloadFileCheck.Location = new System.Drawing.Point(13, 83);
            this._ReloadFileCheck.Name = "_ReloadFileCheck";
            this._ReloadFileCheck.Size = new System.Drawing.Size(192, 31);
            this._ReloadFileCheck.TabIndex = 3;
            this._ReloadFileCheck.Text = "Vždy znovu načíst data";
            this._ReloadFileCheck.UseVisualStyleBackColor = true;
            // 
            // _MainPanel
            // 
            this._MainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._MainPanel.Controls.Add(this._VisualiserPanel);
            this._MainPanel.Controls.Add(this._ProgressText);
            this._MainPanel.Location = new System.Drawing.Point(210, 45);
            this._MainPanel.Name = "_MainPanel";
            this._MainPanel.Size = new System.Drawing.Size(1083, 556);
            this._MainPanel.TabIndex = 13;
            // 
            // _VisualiserPanel
            // 
            this._VisualiserPanel.Location = new System.Drawing.Point(503, 17);
            this._VisualiserPanel.MapSegment = null;
            this._VisualiserPanel.Name = "_VisualiserPanel";
            this._VisualiserPanel.Size = new System.Drawing.Size(482, 501);
            this._VisualiserPanel.TabIndex = 10;
            this._VisualiserPanel.Text = "visualiser1";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1307, 626);
            this.Controls.Add(this._MainPanel);
            this.Controls.Add(this._ReloadFileCheck);
            this.Controls.Add(this._MapTitleLabel);
            this.Controls.Add(this._AnalyseTitleLabel);
            this.Controls.Add(this._VisualiserButton);
            this.Controls.Add(this._ByProductCheck);
            this.Controls.Add(this._FileText);
            this.Controls.Add(this._FileButton);
            this.Controls.Add(this._SimulCycleLabel);
            this.Controls.Add(this._SimulCycleText);
            this.Controls.Add(this._OnlyProcessedCheck);
            this.Controls.Add(this._StatusBar);
            this.Controls.Add(this._StopButton);
            this.Controls.Add(this._AnalyseStartButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Scheduler map analyser";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this._StatusBar.ResumeLayout(false);
            this._StatusBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._SimulCycleText)).EndInit();
            this._MainPanel.ResumeLayout(false);
            this._MainPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _AnalyseStartButton;
        private System.Windows.Forms.Button _StopButton;
        private System.Windows.Forms.TextBox _ProgressText;
        private System.Windows.Forms.StatusStrip _StatusBar;
        private System.Windows.Forms.ToolStripStatusLabel _StatusProcessText;
        private System.Windows.Forms.Timer _Timer;
        private System.Windows.Forms.ToolStripStatusLabel _StatusAnalyserText;
        private System.Windows.Forms.CheckBox _OnlyProcessedCheck;
        private System.Windows.Forms.NumericUpDown _SimulCycleText;
        private System.Windows.Forms.Label _SimulCycleLabel;
        private System.Windows.Forms.Button _FileButton;
        private System.Windows.Forms.TextBox _FileText;
        private System.Windows.Forms.CheckBox _ByProductCheck;
        private System.Windows.Forms.Button _VisualiserButton;
        private System.Windows.Forms.Label _AnalyseTitleLabel;
        private System.Windows.Forms.Label _MapTitleLabel;
        private System.Windows.Forms.CheckBox _ReloadFileCheck;
        private System.Windows.Forms.Panel _MainPanel;
        private VisualiserControl _VisualiserPanel;
    }
}

