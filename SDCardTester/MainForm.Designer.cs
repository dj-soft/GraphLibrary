
namespace DjSoftSDCardTester
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
            this.UserPanel = new System.Windows.Forms.Panel();
            this.DrivesPanel = new System.Windows.Forms.Panel();
            this.DriveCombo = new System.Windows.Forms.ComboBox();
            this.DriveLabel = new System.Windows.Forms.Label();
            this.OnlyRemovableCheck = new System.Windows.Forms.CheckBox();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.PropertiesPanel = new System.Windows.Forms.Panel();
            this.DriveNameLabel = new System.Windows.Forms.Label();
            this.DriveNameText = new System.Windows.Forms.TextBox();
            this.DriveVolumeLabel = new System.Windows.Forms.Label();
            this.DriveVolumeText = new System.Windows.Forms.TextBox();
            this.DriveCapacityLabel = new System.Windows.Forms.Label();
            this.DriveAvailableText = new System.Windows.Forms.TextBox();
            this.DriveCapacityText = new System.Windows.Forms.TextBox();
            this.DriveAvailableLabel = new System.Windows.Forms.Label();
            this.DriveFreeLabel = new System.Windows.Forms.Label();
            this.DriveTypeText = new System.Windows.Forms.TextBox();
            this.DriveFreeText = new System.Windows.Forms.TextBox();
            this.DriveTypeLabel = new System.Windows.Forms.Label();
            this.CommandsPanel = new System.Windows.Forms.Panel();
            this.AnalyseContentButton = new System.Windows.Forms.Button();
            this.TestSaveButton = new System.Windows.Forms.Button();
            this.TestReadButton = new System.Windows.Forms.Button();
            this.StopPanel = new System.Windows.Forms.Panel();
            this.StopButton = new System.Windows.Forms.Button();
            this.VisualMapPanel = new DjSoftSDCardTester.LinearMapControl();
            this.ResultsInfoPanel = new System.Windows.Forms.Panel();
            this.UserPanel.SuspendLayout();
            this.DrivesPanel.SuspendLayout();
            this.PropertiesPanel.SuspendLayout();
            this.CommandsPanel.SuspendLayout();
            this.StopPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // UserPanel
            // 
            this.UserPanel.Controls.Add(this.DrivesPanel);
            this.UserPanel.Controls.Add(this.PropertiesPanel);
            this.UserPanel.Controls.Add(this.CommandsPanel);
            this.UserPanel.Controls.Add(this.StopPanel);
            this.UserPanel.Controls.Add(this.ResultsInfoPanel);
            this.UserPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.UserPanel.Location = new System.Drawing.Point(0, 0);
            this.UserPanel.Name = "UserPanel";
            this.UserPanel.Size = new System.Drawing.Size(251, 600);
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
            this.RefreshButton.Image = global::DjSoftSDCardTester.Properties.Resources.view_refresh;
            this.RefreshButton.Location = new System.Drawing.Point(199, 49);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(40, 32);
            this.RefreshButton.TabIndex = 15;
            this.RefreshButton.TabStop = false;
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // PropertiesPanel
            // 
            this.PropertiesPanel.Controls.Add(this.DriveNameLabel);
            this.PropertiesPanel.Controls.Add(this.DriveNameText);
            this.PropertiesPanel.Controls.Add(this.DriveVolumeLabel);
            this.PropertiesPanel.Controls.Add(this.DriveVolumeText);
            this.PropertiesPanel.Controls.Add(this.DriveCapacityLabel);
            this.PropertiesPanel.Controls.Add(this.DriveAvailableText);
            this.PropertiesPanel.Controls.Add(this.DriveCapacityText);
            this.PropertiesPanel.Controls.Add(this.DriveAvailableLabel);
            this.PropertiesPanel.Controls.Add(this.DriveFreeLabel);
            this.PropertiesPanel.Controls.Add(this.DriveTypeText);
            this.PropertiesPanel.Controls.Add(this.DriveFreeText);
            this.PropertiesPanel.Controls.Add(this.DriveTypeLabel);
            this.PropertiesPanel.Location = new System.Drawing.Point(3, 93);
            this.PropertiesPanel.Name = "PropertiesPanel";
            this.PropertiesPanel.Size = new System.Drawing.Size(243, 299);
            this.PropertiesPanel.TabIndex = 20;
            // 
            // DriveNameLabel
            // 
            this.DriveNameLabel.AutoSize = true;
            this.DriveNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveNameLabel.Location = new System.Drawing.Point(6, 13);
            this.DriveNameLabel.Name = "DriveNameLabel";
            this.DriveNameLabel.Size = new System.Drawing.Size(102, 16);
            this.DriveNameLabel.TabIndex = 3;
            this.DriveNameLabel.Text = "Označení disku:";
            // 
            // DriveNameText
            // 
            this.DriveNameText.Enabled = false;
            this.DriveNameText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveNameText.Location = new System.Drawing.Point(55, 32);
            this.DriveNameText.Name = "DriveNameText";
            this.DriveNameText.ReadOnly = true;
            this.DriveNameText.Size = new System.Drawing.Size(176, 22);
            this.DriveNameText.TabIndex = 4;
            this.DriveNameText.TabStop = false;
            // 
            // DriveVolumeLabel
            // 
            this.DriveVolumeLabel.AutoSize = true;
            this.DriveVolumeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveVolumeLabel.Location = new System.Drawing.Point(6, 105);
            this.DriveVolumeLabel.Name = "DriveVolumeLabel";
            this.DriveVolumeLabel.Size = new System.Drawing.Size(107, 16);
            this.DriveVolumeLabel.TabIndex = 5;
            this.DriveVolumeLabel.Text = "Přidělený název:";
            // 
            // DriveVolumeText
            // 
            this.DriveVolumeText.Enabled = false;
            this.DriveVolumeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveVolumeText.Location = new System.Drawing.Point(55, 124);
            this.DriveVolumeText.Name = "DriveVolumeText";
            this.DriveVolumeText.ReadOnly = true;
            this.DriveVolumeText.Size = new System.Drawing.Size(176, 22);
            this.DriveVolumeText.TabIndex = 6;
            this.DriveVolumeText.TabStop = false;
            // 
            // DriveCapacityLabel
            // 
            this.DriveCapacityLabel.AutoSize = true;
            this.DriveCapacityLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveCapacityLabel.Location = new System.Drawing.Point(6, 151);
            this.DriveCapacityLabel.Name = "DriveCapacityLabel";
            this.DriveCapacityLabel.Size = new System.Drawing.Size(154, 16);
            this.DriveCapacityLabel.TabIndex = 7;
            this.DriveCapacityLabel.Text = "Celková kapacita [Byte]:";
            // 
            // DriveAvailableText
            // 
            this.DriveAvailableText.Enabled = false;
            this.DriveAvailableText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveAvailableText.Location = new System.Drawing.Point(55, 262);
            this.DriveAvailableText.Name = "DriveAvailableText";
            this.DriveAvailableText.ReadOnly = true;
            this.DriveAvailableText.Size = new System.Drawing.Size(176, 22);
            this.DriveAvailableText.TabIndex = 14;
            this.DriveAvailableText.TabStop = false;
            this.DriveAvailableText.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // DriveCapacityText
            // 
            this.DriveCapacityText.Enabled = false;
            this.DriveCapacityText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveCapacityText.Location = new System.Drawing.Point(55, 170);
            this.DriveCapacityText.Name = "DriveCapacityText";
            this.DriveCapacityText.ReadOnly = true;
            this.DriveCapacityText.Size = new System.Drawing.Size(176, 22);
            this.DriveCapacityText.TabIndex = 8;
            this.DriveCapacityText.TabStop = false;
            this.DriveCapacityText.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // DriveAvailableLabel
            // 
            this.DriveAvailableLabel.AutoSize = true;
            this.DriveAvailableLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveAvailableLabel.Location = new System.Drawing.Point(6, 243);
            this.DriveAvailableLabel.Name = "DriveAvailableLabel";
            this.DriveAvailableLabel.Size = new System.Drawing.Size(162, 16);
            this.DriveAvailableLabel.TabIndex = 13;
            this.DriveAvailableLabel.Text = "Dostupná kapacita [Byte]:";
            // 
            // DriveFreeLabel
            // 
            this.DriveFreeLabel.AutoSize = true;
            this.DriveFreeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveFreeLabel.Location = new System.Drawing.Point(6, 197);
            this.DriveFreeLabel.Name = "DriveFreeLabel";
            this.DriveFreeLabel.Size = new System.Drawing.Size(139, 16);
            this.DriveFreeLabel.TabIndex = 9;
            this.DriveFreeLabel.Text = "Volná kapacita [Byte]:";
            // 
            // DriveTypeText
            // 
            this.DriveTypeText.Enabled = false;
            this.DriveTypeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveTypeText.Location = new System.Drawing.Point(55, 78);
            this.DriveTypeText.Name = "DriveTypeText";
            this.DriveTypeText.ReadOnly = true;
            this.DriveTypeText.Size = new System.Drawing.Size(176, 22);
            this.DriveTypeText.TabIndex = 12;
            this.DriveTypeText.TabStop = false;
            // 
            // DriveFreeText
            // 
            this.DriveFreeText.Enabled = false;
            this.DriveFreeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveFreeText.Location = new System.Drawing.Point(55, 216);
            this.DriveFreeText.Name = "DriveFreeText";
            this.DriveFreeText.ReadOnly = true;
            this.DriveFreeText.Size = new System.Drawing.Size(176, 22);
            this.DriveFreeText.TabIndex = 10;
            this.DriveFreeText.TabStop = false;
            this.DriveFreeText.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // DriveTypeLabel
            // 
            this.DriveTypeLabel.AutoSize = true;
            this.DriveTypeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DriveTypeLabel.Location = new System.Drawing.Point(6, 59);
            this.DriveTypeLabel.Name = "DriveTypeLabel";
            this.DriveTypeLabel.Size = new System.Drawing.Size(70, 16);
            this.DriveTypeLabel.TabIndex = 11;
            this.DriveTypeLabel.Text = "Typ disku:";
            // 
            // CommandsPanel
            // 
            this.CommandsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CommandsPanel.Controls.Add(this.AnalyseContentButton);
            this.CommandsPanel.Controls.Add(this.TestSaveButton);
            this.CommandsPanel.Controls.Add(this.TestReadButton);
            this.CommandsPanel.Location = new System.Drawing.Point(3, 427);
            this.CommandsPanel.Name = "CommandsPanel";
            this.CommandsPanel.Size = new System.Drawing.Size(243, 170);
            this.CommandsPanel.TabIndex = 21;
            // 
            // AnalyseContentButton
            // 
            this.AnalyseContentButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.AnalyseContentButton.Image = global::DjSoftSDCardTester.Properties.Resources.office_chart_pie;
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
            this.TestSaveButton.Image = global::DjSoftSDCardTester.Properties.Resources.media_floppy_3_5_mount_2;
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
            this.TestReadButton.Image = global::DjSoftSDCardTester.Properties.Resources.document_revert_4;
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
            // StopPanel
            // 
            this.StopPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.StopPanel.Controls.Add(this.StopButton);
            this.StopPanel.Location = new System.Drawing.Point(3, 539);
            this.StopPanel.Name = "StopPanel";
            this.StopPanel.Size = new System.Drawing.Size(243, 58);
            this.StopPanel.TabIndex = 22;
            // 
            // StopButton
            // 
            this.StopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.StopButton.Image = global::DjSoftSDCardTester.Properties.Resources.road_sign_us_stop;
            this.StopButton.Location = new System.Drawing.Point(55, 3);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(176, 50);
            this.StopButton.TabIndex = 19;
            this.StopButton.Text = "Stop";
            this.StopButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StopButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // VisualPanel
            // 
            this.VisualMapPanel.BackColor = System.Drawing.Color.Snow;
            this.VisualMapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VisualMapPanel.Location = new System.Drawing.Point(251, 0);
            this.VisualMapPanel.Name = "VisualPanel";
            this.VisualMapPanel.Size = new System.Drawing.Size(764, 600);
            this.VisualMapPanel.TabIndex = 1;
            // 
            // AnalyseInfoPanel
            // 
            this.ResultsInfoPanel.Location = new System.Drawing.Point(3, 93);
            this.ResultsInfoPanel.Name = "AnalyseInfoPanel";
            this.ResultsInfoPanel.Size = new System.Drawing.Size(243, 428);
            this.ResultsInfoPanel.TabIndex = 23;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 600);
            this.Controls.Add(this.VisualMapPanel);
            this.Controls.Add(this.UserPanel);
            this.MinimumSize = new System.Drawing.Size(819, 617);
            this.Name = "MainForm";
            this.Text = "SD Card tester";
            this.UserPanel.ResumeLayout(false);
            this.DrivesPanel.ResumeLayout(false);
            this.DrivesPanel.PerformLayout();
            this.PropertiesPanel.ResumeLayout(false);
            this.PropertiesPanel.PerformLayout();
            this.CommandsPanel.ResumeLayout(false);
            this.StopPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel UserPanel;
        private System.Windows.Forms.ComboBox DriveCombo;
        private DjSoftSDCardTester.LinearMapControl VisualMapPanel;
        private System.Windows.Forms.Label DriveLabel;
        private System.Windows.Forms.CheckBox OnlyRemovableCheck;
        private System.Windows.Forms.TextBox DriveTypeText;
        private System.Windows.Forms.Label DriveTypeLabel;
        private System.Windows.Forms.TextBox DriveFreeText;
        private System.Windows.Forms.Label DriveFreeLabel;
        private System.Windows.Forms.TextBox DriveCapacityText;
        private System.Windows.Forms.Label DriveCapacityLabel;
        private System.Windows.Forms.TextBox DriveVolumeText;
        private System.Windows.Forms.Label DriveVolumeLabel;
        private System.Windows.Forms.TextBox DriveNameText;
        private System.Windows.Forms.Label DriveNameLabel;
        private System.Windows.Forms.TextBox DriveAvailableText;
        private System.Windows.Forms.Label DriveAvailableLabel;
        private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.Button AnalyseContentButton;
        private System.Windows.Forms.Button TestReadButton;
        private System.Windows.Forms.Button TestSaveButton;
        private System.Windows.Forms.Panel PropertiesPanel;
        private System.Windows.Forms.Panel CommandsPanel;
        private System.Windows.Forms.Panel DrivesPanel;
        private System.Windows.Forms.Panel StopPanel;
        private System.Windows.Forms.Button StopButton;
        private System.Windows.Forms.Panel ResultsInfoPanel;
    }
}

