
namespace DjSoft.Tools.SDCardTester
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.UserPanel = new System.Windows.Forms.Panel();
            this.DrivesPanel = new System.Windows.Forms.Panel();
            this.DriveCombo = new System.Windows.Forms.ComboBox();
            this.DriveLabel = new System.Windows.Forms.Label();
            this.OnlyRemovableCheck = new System.Windows.Forms.CheckBox();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.DriveInfoPanel = new DjSoft.Tools.SDCardTester.DriveInfoPanel();
            this.CommandsPanel = new System.Windows.Forms.Panel();
            this.AnalyseContentButton = new System.Windows.Forms.Button();
            this.TestSaveButton = new System.Windows.Forms.Button();
            this.TestReadButton = new System.Windows.Forms.Button();
            this.RunPauseStopPanel = new RunPauseStop();
            this.ResultsInfoPanel = new System.Windows.Forms.Panel();
            this.LinearMapControl = new DjSoft.Tools.SDCardTester.LinearMapControl();
            this.UserPanel.SuspendLayout();
            this.DrivesPanel.SuspendLayout();
            this.CommandsPanel.SuspendLayout();
            this.RunPauseStopPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // UserPanel
            // 
            this.UserPanel.Controls.Add(this.DrivesPanel);
            this.UserPanel.Controls.Add(this.DriveInfoPanel);
            this.UserPanel.Controls.Add(this.CommandsPanel);
            this.UserPanel.Controls.Add(this.RunPauseStopPanel);
            this.UserPanel.Controls.Add(this.ResultsInfoPanel);
            this.UserPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.UserPanel.Location = new System.Drawing.Point(0, 0);
            this.UserPanel.Name = "UserPanel";
            this.UserPanel.Size = new System.Drawing.Size(251, 609);
            this.UserPanel.TabIndex = 0;
            // 
            // DrivesPanel
            // 
            this.DrivesPanel.Controls.Add(this.DriveCombo);
            this.DrivesPanel.Controls.Add(this.DriveLabel);
            this.DrivesPanel.Controls.Add(this.OnlyRemovableCheck);
            this.DrivesPanel.Controls.Add(this.RefreshButton);
            this.DrivesPanel.Location = new System.Drawing.Point(3, 3);
            this.DrivesPanel.Name = "DrivesPanel";
            this.DrivesPanel.Size = new System.Drawing.Size(243, 86);
            this.DrivesPanel.TabIndex = 22;
            // 
            // DriveCombo
            // 
            this.DriveCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DriveCombo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveCombo.FormattingEnabled = true;
            this.DriveCombo.Location = new System.Drawing.Point(13, 22);
            this.DriveCombo.Name = "DriveCombo";
            this.DriveCombo.Size = new System.Drawing.Size(218, 24);
            this.DriveCombo.TabIndex = 0;
            // 
            // DriveLabel
            // 
            this.DriveLabel.AutoSize = true;
            this.DriveLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveLabel.Location = new System.Drawing.Point(16, 3);
            this.DriveLabel.Name = "DriveLabel";
            this.DriveLabel.Size = new System.Drawing.Size(75, 16);
            this.DriveLabel.TabIndex = 1;
            this.DriveLabel.Text = "Vyber disk:";
            // 
            // OnlyRemovableCheck
            // 
            this.OnlyRemovableCheck.AutoSize = true;
            this.OnlyRemovableCheck.Checked = true;
            this.OnlyRemovableCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OnlyRemovableCheck.Location = new System.Drawing.Point(23, 56);
            this.OnlyRemovableCheck.Name = "OnlyRemovableCheck";
            this.OnlyRemovableCheck.Size = new System.Drawing.Size(142, 17);
            this.OnlyRemovableCheck.TabIndex = 2;
            this.OnlyRemovableCheck.Text = "Pouze vyměnitelné disky";
            this.OnlyRemovableCheck.UseVisualStyleBackColor = true;
            // 
            // RefreshButton
            // 
            this.RefreshButton.FlatAppearance.BorderSize = 0;
            this.RefreshButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RefreshButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.view_refresh;
            this.RefreshButton.Location = new System.Drawing.Point(199, 49);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(40, 32);
            this.RefreshButton.TabIndex = 15;
            this.RefreshButton.TabStop = false;
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // DriveInfoPanel
            // 
            this.DriveInfoPanel.Location = new System.Drawing.Point(3, 93);
            this.DriveInfoPanel.MinimumSize = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.Name = "DriveInfoPanel";
            this.DriveInfoPanel.Size = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.TabIndex = 20;
            // 
            // CommandsPanel
            // 
            this.CommandsPanel.Controls.Add(this.AnalyseContentButton);
            this.CommandsPanel.Controls.Add(this.TestSaveButton);
            this.CommandsPanel.Controls.Add(this.TestReadButton);
            this.CommandsPanel.Location = new System.Drawing.Point(3, 436);
            this.CommandsPanel.Name = "CommandsPanel";
            this.CommandsPanel.Size = new System.Drawing.Size(243, 170);
            this.CommandsPanel.TabIndex = 21;
            // 
            // AnalyseContentButton
            // 
            this.AnalyseContentButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.AnalyseContentButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.office_chart_pie;
            this.AnalyseContentButton.Location = new System.Drawing.Point(55, 3);
            this.AnalyseContentButton.Name = "AnalyseContentButton";
            this.AnalyseContentButton.Size = new System.Drawing.Size(176, 50);
            this.AnalyseContentButton.TabIndex = 16;
            this.AnalyseContentButton.Text = "Analýza obsahu";
            this.AnalyseContentButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AnalyseContentButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.AnalyseContentButton.UseVisualStyleBackColor = true;
            this.AnalyseContentButton.Click += new System.EventHandler(this.AnalyseContentButton_Click);
            // 
            // TestSaveButton
            // 
            this.TestSaveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.TestSaveButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.media_floppy_3_5_mount_2;
            this.TestSaveButton.Location = new System.Drawing.Point(55, 59);
            this.TestSaveButton.Name = "TestSaveButton";
            this.TestSaveButton.Size = new System.Drawing.Size(176, 50);
            this.TestSaveButton.TabIndex = 17;
            this.TestSaveButton.Text = "Test zápisu";
            this.TestSaveButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.TestSaveButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.TestSaveButton.UseVisualStyleBackColor = true;
            this.TestSaveButton.Click += new System.EventHandler(this.TestSaveButton_Click);
            // 
            // TestReadButton
            // 
            this.TestReadButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.TestReadButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.document_revert_4;
            this.TestReadButton.Location = new System.Drawing.Point(55, 115);
            this.TestReadButton.Name = "TestReadButton";
            this.TestReadButton.Size = new System.Drawing.Size(176, 50);
            this.TestReadButton.TabIndex = 18;
            this.TestReadButton.Text = "Test čtení";
            this.TestReadButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.TestReadButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.TestReadButton.UseVisualStyleBackColor = true;
            this.TestReadButton.Click += new System.EventHandler(this.TestReadButton_Click);
            // 
            // RunPauseStopPanel
            // 
            this.RunPauseStopPanel.Location = new System.Drawing.Point(3, 548);
            this.RunPauseStopPanel.Name = "RunPauseStopPanel";
            this.RunPauseStopPanel.Size = new System.Drawing.Size(243, 40);
            this.RunPauseStopPanel.TabIndex = 22;
            this.RunPauseStopPanel.StateChanged += new System.EventHandler(this.RunPauseStopPanel_StateChanged);
            this.RunPauseStopPanel.ButtonHeight = 30;
            this.RunPauseStopPanel.ButtonAlignment = System.Drawing.ContentAlignment.BottomRight;
            // 
            // ResultsInfoPanel
            // 
            this.ResultsInfoPanel.Location = new System.Drawing.Point(3, 93);
            this.ResultsInfoPanel.Name = "ResultsInfoPanel";
            this.ResultsInfoPanel.Size = new System.Drawing.Size(243, 428);
            this.ResultsInfoPanel.TabIndex = 23;
            // 
            // LinearMapControl
            // 
            this.LinearMapControl.BackColor = System.Drawing.Color.Snow;
            this.LinearMapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LinearMapControl.Location = new System.Drawing.Point(251, 0);
            this.LinearMapControl.Name = "LinearMapControl";
            this.LinearMapControl.Size = new System.Drawing.Size(764, 609);
            this.LinearMapControl.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 609);
            this.Controls.Add(this.LinearMapControl);
            this.Controls.Add(this.UserPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(819, 648);
            this.Name = "MainForm";
            this.Text = "SD Card tester";
            this.UserPanel.ResumeLayout(false);
            this.DrivesPanel.ResumeLayout(false);
            this.DrivesPanel.PerformLayout();
            this.CommandsPanel.ResumeLayout(false);
            this.RunPauseStopPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel UserPanel;
        private System.Windows.Forms.ComboBox DriveCombo;
        private DjSoft.Tools.SDCardTester.LinearMapControl LinearMapControl;
        private System.Windows.Forms.Label DriveLabel;
        private System.Windows.Forms.CheckBox OnlyRemovableCheck;
        private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.Button AnalyseContentButton;
        private System.Windows.Forms.Button TestReadButton;
        private System.Windows.Forms.Button TestSaveButton;
        private DjSoft.Tools.SDCardTester.DriveInfoPanel DriveInfoPanel;
        private System.Windows.Forms.Panel CommandsPanel;
        private System.Windows.Forms.Panel DrivesPanel;
        private DjSoft.Tools.SDCardTester.RunPauseStop RunPauseStopPanel;
        private System.Windows.Forms.Panel ResultsInfoPanel;
    }
}

