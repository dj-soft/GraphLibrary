
namespace SchedulerMapAnalyser
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
            this._RunButton = new System.Windows.Forms.Button();
            this._StopButton = new System.Windows.Forms.Button();
            this._ProgressText = new System.Windows.Forms.TextBox();
            this._StatusBar = new System.Windows.Forms.StatusStrip();
            this._StatusProcessText = new System.Windows.Forms.ToolStripStatusLabel();
            this._StatusAnalyserText = new System.Windows.Forms.ToolStripStatusLabel();
            this._Timer = new System.Windows.Forms.Timer(this.components);
            this._OnlyProcessedCheck = new System.Windows.Forms.CheckBox();
            this._SimulCycleText = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this._StatusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._SimulCycleText)).BeginInit();
            this.SuspendLayout();
            // 
            // _RunButton
            // 
            this._RunButton.Location = new System.Drawing.Point(37, 29);
            this._RunButton.Name = "_RunButton";
            this._RunButton.Size = new System.Drawing.Size(185, 44);
            this._RunButton.TabIndex = 0;
            this._RunButton.Text = "START";
            this._RunButton.UseVisualStyleBackColor = true;
            this._RunButton.Click += new System.EventHandler(this._StartClick);
            // 
            // _StopButton
            // 
            this._StopButton.Location = new System.Drawing.Point(37, 193);
            this._StopButton.Name = "_StopButton";
            this._StopButton.Size = new System.Drawing.Size(185, 44);
            this._StopButton.TabIndex = 1;
            this._StopButton.Text = "STOP";
            this._StopButton.UseVisualStyleBackColor = true;
            this._StopButton.Click += new System.EventHandler(this._StopClick);
            // 
            // _ProgressText
            // 
            this._ProgressText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._ProgressText.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._ProgressText.Location = new System.Drawing.Point(235, 32);
            this._ProgressText.Multiline = true;
            this._ProgressText.Name = "_ProgressText";
            this._ProgressText.ReadOnly = true;
            this._ProgressText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._ProgressText.Size = new System.Drawing.Size(725, 467);
            this._ProgressText.TabIndex = 2;
            this._ProgressText.WordWrap = false;
            // 
            // _StatusBar
            // 
            this._StatusBar.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._StatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._StatusProcessText,
            this._StatusAnalyserText});
            this._StatusBar.Location = new System.Drawing.Point(0, 502);
            this._StatusBar.Name = "_StatusBar";
            this._StatusBar.Size = new System.Drawing.Size(972, 22);
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
            this._StatusAnalyserText.Size = new System.Drawing.Size(707, 17);
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
            this._OnlyProcessedCheck.Location = new System.Drawing.Point(37, 82);
            this._OnlyProcessedCheck.Name = "_OnlyProcessedCheck";
            this._OnlyProcessedCheck.Size = new System.Drawing.Size(192, 31);
            this._OnlyProcessedCheck.TabIndex = 4;
            this._OnlyProcessedCheck.Text = "Pouze zpracované položky";
            this._OnlyProcessedCheck.UseVisualStyleBackColor = true;
            // 
            // _SimulCycleText
            // 
            this._SimulCycleText.Location = new System.Drawing.Point(168, 114);
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 116);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "SImuluj zacyklení:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(972, 524);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._SimulCycleText);
            this.Controls.Add(this._OnlyProcessedCheck);
            this.Controls.Add(this._StatusBar);
            this.Controls.Add(this._ProgressText);
            this.Controls.Add(this._StopButton);
            this.Controls.Add(this._RunButton);
            this.Name = "Form1";
            this.Text = "Scheduler map analyser";
            this._StatusBar.ResumeLayout(false);
            this._StatusBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._SimulCycleText)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _RunButton;
        private System.Windows.Forms.Button _StopButton;
        private System.Windows.Forms.TextBox _ProgressText;
        private System.Windows.Forms.StatusStrip _StatusBar;
        private System.Windows.Forms.ToolStripStatusLabel _StatusProcessText;
        private System.Windows.Forms.Timer _Timer;
        private System.Windows.Forms.ToolStripStatusLabel _StatusAnalyserText;
        private System.Windows.Forms.CheckBox _OnlyProcessedCheck;
        private System.Windows.Forms.NumericUpDown _SimulCycleText;
        private System.Windows.Forms.Label label1;
    }
}

