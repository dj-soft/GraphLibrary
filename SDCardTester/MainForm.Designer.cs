
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

            this.ToolBar.SuspendLayout();
            this.UserPanel.SuspendLayout();
            this.ResultsInfoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // UserPanel
            // 
            this.UserPanel.Controls.Add(this.DriveInfoPanel);
            this.UserPanel.Controls.Add(this.ResultsInfoPanel);
            this.UserPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.UserPanel.Location = new System.Drawing.Point(0, 39);
            this.UserPanel.Size = new System.Drawing.Size(251, 740);
            this.UserPanel.TabIndex = 0;
            // 
            // DriveInfoPanel
            // 
            this.DriveInfoPanel.Location = new System.Drawing.Point(2, 137);
            this.DriveInfoPanel.MinimumSize = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.Size = new System.Drawing.Size(243, 341);
            this.DriveInfoPanel.TabIndex = 20;
            // 
            // ResultsInfoPanel
            // 
            this.ResultsInfoPanel.Location = new System.Drawing.Point(3, 93);
            this.ResultsInfoPanel.Size = new System.Drawing.Size(243, 428);
            this.ResultsInfoPanel.TabIndex = 23;
           
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

        }

        #endregion

        private System.Windows.Forms.Panel UserPanel;
        private DjSoft.Tools.SDCardTester.LinearMapControl LinearMapControl;
        private DjSoft.Tools.SDCardTester.DriveInfoPanel DriveInfoPanel;
        private System.Windows.Forms.Panel ResultsInfoPanel;
    }
}

