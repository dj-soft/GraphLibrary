
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
            this.ToolBar = new System.Windows.Forms.ToolStrip();
            this.UserPanel = new System.Windows.Forms.Panel();
            this.DriveInfoPanel = new DjSoft.Tools.SDCardTester.DriveInfoPanel();
            this.ResultsInfoPanel = new System.Windows.Forms.Panel();
            this.LinearMapControl = new DjSoft.Tools.SDCardTester.LinearMapControl();
            this.ToolDriveCombo = new System.Windows.Forms.ToolStripComboBox();
            this.ToolDriveLabel = new System.Windows.Forms.ToolStripLabel();
            this.ToolDriveSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.ToolActionSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.ToolFlowControlSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.ToolDriveRefreshButton = new System.Windows.Forms.ToolStripButton();
            this.ToolActionAnalyseButton = new System.Windows.Forms.ToolStripButton();
            this.ToolActionWriteDataButton = new System.Windows.Forms.ToolStripButton();
            this.ToolActionReadDataButton = new System.Windows.Forms.ToolStripButton();
            this.ToolActionReadAnyButton = new System.Windows.Forms.ToolStripButton();
            this.ToolFlowControlRunButton = new System.Windows.Forms.ToolStripButton();
            this.ToolFlowControlPauseButton = new System.Windows.Forms.ToolStripButton();
            this.ToolFlowControlStopButton = new System.Windows.Forms.ToolStripButton();
            this.ToolDriveTypeButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.ToolDriveTypeFlashButton = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolDriveTypeAllButton = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolBar.SuspendLayout();
            this.UserPanel.SuspendLayout();
            this.ResultsInfoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ToolBar
            // 
            this.ToolBar.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.ToolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolDriveLabel,
            this.ToolDriveCombo,
            this.ToolDriveTypeButton,
            this.ToolDriveRefreshButton,
            this.ToolDriveSeparator,
            this.ToolActionAnalyseButton,
            this.ToolActionWriteDataButton,
            this.ToolActionReadDataButton,
            this.ToolActionReadAnyButton,
            this.ToolActionSeparator,
            this.ToolFlowControlRunButton,
            this.ToolFlowControlPauseButton,
            this.ToolFlowControlStopButton,
            this.ToolFlowControlSeparator});
            this.ToolBar.Location = new System.Drawing.Point(0, 0);
            this.ToolBar.Name = "ToolBar";
            this.ToolBar.Size = new System.Drawing.Size(1015, 39);
            this.ToolBar.TabIndex = 2;
            this.ToolBar.Text = "toolStrip1";
            // 
            // UserPanel
            // 
            this.UserPanel.Controls.Add(this.DriveInfoPanel);
            this.UserPanel.Controls.Add(this.ResultsInfoPanel);
            this.UserPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.UserPanel.Location = new System.Drawing.Point(0, 39);
            this.UserPanel.Name = "UserPanel";
            this.UserPanel.Size = new System.Drawing.Size(251, 740);
            this.UserPanel.TabIndex = 0;
            // 
            // DriveInfoPanel
            // 
            this.DriveInfoPanel.Location = new System.Drawing.Point(2, 137);
            this.DriveInfoPanel.MinimumSize = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.Name = "DriveInfoPanel";
            this.DriveInfoPanel.Size = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.TabIndex = 20;
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
            this.LinearMapControl.Location = new System.Drawing.Point(251, 39);
            this.LinearMapControl.Name = "LinearMapControl";
            this.LinearMapControl.Size = new System.Drawing.Size(764, 740);
            this.LinearMapControl.TabIndex = 1;
            // 
            // ToolDriveCombo
            // 
            this.ToolDriveCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ToolDriveCombo.Name = "ToolDriveCombo";
            this.ToolDriveCombo.Size = new System.Drawing.Size(300, 39);
            // 
            // ToolDriveLabel
            // 
            this.ToolDriveLabel.Name = "ToolDriveLabel";
            this.ToolDriveLabel.Size = new System.Drawing.Size(32, 36);
            this.ToolDriveLabel.Text = "Disk:";
            // 
            // ToolDriveSeparator
            // 
            this.ToolDriveSeparator.Name = "ToolDriveSeparator";
            this.ToolDriveSeparator.Size = new System.Drawing.Size(6, 39);
            // 
            // ToolActionSeparator
            // 
            this.ToolActionSeparator.Name = "ToolActionSeparator";
            this.ToolActionSeparator.Size = new System.Drawing.Size(6, 39);
            // 
            // ToolFlowControlSeparator
            // 
            this.ToolFlowControlSeparator.Name = "ToolFlowControlSeparator";
            this.ToolFlowControlSeparator.Size = new System.Drawing.Size(6, 39);
            // 
            // ToolDriveRefreshButton
            // 
            this.ToolDriveRefreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolDriveRefreshButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.view_refresh_4_32;
            this.ToolDriveRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolDriveRefreshButton.Name = "ToolDriveRefreshButton";
            this.ToolDriveRefreshButton.Size = new System.Drawing.Size(36, 36);
            this.ToolDriveRefreshButton.Text = "toolStripButton2";
            // 
            // ToolActionAnalyseButton
            // 
            this.ToolActionAnalyseButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.office_chart_pie_32;
            this.ToolActionAnalyseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolActionAnalyseButton.Name = "ToolActionAnalyseButton";
            this.ToolActionAnalyseButton.Size = new System.Drawing.Size(84, 36);
            this.ToolActionAnalyseButton.Text = "Analýza";
            this.ToolActionAnalyseButton.ToolTipText = "Analýza obsahu disku z hlediska typu dat";
            // 
            // ToolActionWriteDataButton
            // 
            this.ToolActionWriteDataButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.media_floppy_3_5_mount_2_32;
            this.ToolActionWriteDataButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolActionWriteDataButton.Name = "ToolActionWriteDataButton";
            this.ToolActionWriteDataButton.Size = new System.Drawing.Size(71, 36);
            this.ToolActionWriteDataButton.Text = "Zápis";
            this.ToolActionWriteDataButton.ToolTipText = "Zápis testovacích dat na disk";
            // 
            // ToolActionReadDataButton
            // 
            this.ToolActionReadDataButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.document_revert_4_32;
            this.ToolActionReadDataButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolActionReadDataButton.Name = "ToolActionReadDataButton";
            this.ToolActionReadDataButton.Size = new System.Drawing.Size(88, 36);
            this.ToolActionReadDataButton.Text = "Kontrola";
            this.ToolActionReadDataButton.ToolTipText = "Kontrola testovacích dat - shodnost obsahu";
            // 
            // ToolActionReadAnyButton
            // 
            this.ToolActionReadAnyButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.edit_find_7_32;
            this.ToolActionReadAnyButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolActionReadAnyButton.Name = "ToolActionReadAnyButton";
            this.ToolActionReadAnyButton.Size = new System.Drawing.Size(90, 36);
            this.ToolActionReadAnyButton.Text = "Čitelnost";
            this.ToolActionReadAnyButton.ToolTipText = "Prověří čitelnost každého souboru, nejen testovacího";
            // 
            // ToolFlowControlRunButton
            // 
            this.ToolFlowControlRunButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolFlowControlRunButton.Enabled = false;
            this.ToolFlowControlRunButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_start_2_32;
            this.ToolFlowControlRunButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolFlowControlRunButton.Name = "ToolFlowControlRunButton";
            this.ToolFlowControlRunButton.Size = new System.Drawing.Size(36, 36);
            this.ToolFlowControlRunButton.Text = "Run";
            this.ToolFlowControlRunButton.ToolTipText = "Pokračuje v pozastavené akci";
            // 
            // ToolFlowControlPauseButton
            // 
            this.ToolFlowControlPauseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolFlowControlPauseButton.Enabled = false;
            this.ToolFlowControlPauseButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_pause_2_32;
            this.ToolFlowControlPauseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolFlowControlPauseButton.Name = "ToolFlowControlPauseButton";
            this.ToolFlowControlPauseButton.Size = new System.Drawing.Size(36, 36);
            this.ToolFlowControlPauseButton.Text = "Pauza";
            this.ToolFlowControlPauseButton.ToolTipText = "Pozastaví běžící akci, bude možno v ní pokračovat";
            // 
            // ToolFlowControlStopButton
            // 
            this.ToolFlowControlStopButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolFlowControlStopButton.Enabled = false;
            this.ToolFlowControlStopButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_stop_2_32;
            this.ToolFlowControlStopButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolFlowControlStopButton.Name = "ToolFlowControlStopButton";
            this.ToolFlowControlStopButton.Size = new System.Drawing.Size(36, 36);
            this.ToolFlowControlStopButton.Text = "Stop";
            this.ToolFlowControlStopButton.ToolTipText = "Zruší běžící akci a vrátí okno do výchozího stavu";
            // 
            // ToolDriveTypeButton
            // 
            this.ToolDriveTypeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolDriveTypeButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolDriveTypeFlashButton,
            this.ToolDriveTypeAllButton});
            this.ToolDriveTypeButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.drive_removable_media_usb_pendrive_32;
            this.ToolDriveTypeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolDriveTypeButton.Name = "ToolDriveTypeButton";
            this.ToolDriveTypeButton.Size = new System.Drawing.Size(45, 36);
            this.ToolDriveTypeButton.Text = "Nabízené typy disku";
            this.ToolDriveTypeButton.ToolTipText = "Volba nabídky disků: výměnné nebo všechny?";
            // 
            // ToolDriveTypeFlashButton
            // 
            this.ToolDriveTypeFlashButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.drive_removable_media_usb_pendrive_32;
            this.ToolDriveTypeFlashButton.Name = "ToolDriveTypeFlashButton";
            this.ToolDriveTypeFlashButton.Size = new System.Drawing.Size(205, 38);
            this.ToolDriveTypeFlashButton.Text = "Jen vyměnitelné disky";
            // 
            // ToolDriveTypeAllButton
            // 
            this.ToolDriveTypeAllButton.Image = global::DjSoft.Tools.SDCardTester.Properties.Resources.drive_raid_32;
            this.ToolDriveTypeAllButton.Name = "ToolDriveTypeAllButton";
            this.ToolDriveTypeAllButton.Size = new System.Drawing.Size(205, 38);
            this.ToolDriveTypeAllButton.Text = "Všechny disky";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 779);
            this.Controls.Add(this.LinearMapControl);
            this.Controls.Add(this.UserPanel);
            this.Controls.Add(this.ToolBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(819, 648);
            this.Name = "MainForm";
            this.Text = "SD Card tester";
            this.ToolBar.ResumeLayout(false);
            this.ToolBar.PerformLayout();
            this.UserPanel.ResumeLayout(false);
            this.ResultsInfoPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip ToolBar;
        private System.Windows.Forms.Panel UserPanel;
        private DjSoft.Tools.SDCardTester.LinearMapControl LinearMapControl;
        private DjSoft.Tools.SDCardTester.DriveInfoPanel DriveInfoPanel;
        private System.Windows.Forms.Panel ResultsInfoPanel;
        private System.Windows.Forms.ToolStripButton ToolActionAnalyseButton;
        private System.Windows.Forms.ToolStripLabel ToolDriveLabel;
        private System.Windows.Forms.ToolStripComboBox ToolDriveCombo;
        private System.Windows.Forms.ToolStripButton ToolDriveRefreshButton;
        private System.Windows.Forms.ToolStripSeparator ToolDriveSeparator;
        private System.Windows.Forms.ToolStripButton ToolActionWriteDataButton;
        private System.Windows.Forms.ToolStripButton ToolActionReadDataButton;
        private System.Windows.Forms.ToolStripButton ToolActionReadAnyButton;
        private System.Windows.Forms.ToolStripSeparator ToolActionSeparator;
        private System.Windows.Forms.ToolStripButton ToolFlowControlRunButton;
        private System.Windows.Forms.ToolStripButton ToolFlowControlPauseButton;
        private System.Windows.Forms.ToolStripButton ToolFlowControlStopButton;
        private System.Windows.Forms.ToolStripSeparator ToolFlowControlSeparator;
        private System.Windows.Forms.ToolStripDropDownButton ToolDriveTypeButton;
        private System.Windows.Forms.ToolStripMenuItem ToolDriveTypeFlashButton;
        private System.Windows.Forms.ToolStripMenuItem ToolDriveTypeAllButton;
    }
}

