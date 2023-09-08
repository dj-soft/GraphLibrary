
namespace DjSoft.Tools.ProgramLauncher
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
            this._MainContainer = new System.Windows.Forms.SplitContainer();
            this._ToolStrip = new System.Windows.Forms.ToolStrip();
            this._ToolEditButton = new System.Windows.Forms.ToolStripButton();
            this._StatusStrip = new System.Windows.Forms.StatusStrip();
            this._StatusVersionLabel = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).BeginInit();
            this._MainContainer.SuspendLayout();
            this._ToolStrip.SuspendLayout();
            this._StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _MainContainer
            // 
            this._MainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._MainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._MainContainer.IsSplitterFixed = true;
            this._MainContainer.Location = new System.Drawing.Point(0, 55);
            this._MainContainer.Name = "_MainContainer";
            this._MainContainer.Size = new System.Drawing.Size(974, 398);
            this._MainContainer.SplitterDistance = 150;
            this._MainContainer.SplitterWidth = 1;
            this._MainContainer.TabIndex = 2;
            // 
            // _ToolStrip
            // 
            this._ToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._ToolStrip.ImageScalingSize = new System.Drawing.Size(48, 48);
            this._ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._ToolEditButton});
            this._ToolStrip.Location = new System.Drawing.Point(0, 0);
            this._ToolStrip.Name = "_ToolStrip";
            this._ToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._ToolStrip.Size = new System.Drawing.Size(974, 55);
            this._ToolStrip.TabIndex = 0;
            this._ToolStrip.Text = "toolStrip1";
            // 
            // _ToolEditButton
            // 
            this._ToolEditButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._ToolEditButton.Image = global::DjSoft.Tools.ProgramLauncher.Properties.Resources.edit_6_48;
            this._ToolEditButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._ToolEditButton.Name = "_ToolEditButton";
            this._ToolEditButton.Size = new System.Drawing.Size(52, 52);
            this._ToolEditButton.Text = "Upravit";
            // 
            // _StatusStrip
            // 
            this._StatusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._StatusVersionLabel});
            this._StatusStrip.Location = new System.Drawing.Point(0, 453);
            this._StatusStrip.Name = "_StatusStrip";
            this._StatusStrip.Size = new System.Drawing.Size(974, 22);
            this._StatusStrip.TabIndex = 1;
            // 
            // _StatusVersionLabel
            // 
            this._StatusVersionLabel.Name = "_StatusVersionLabel";
            this._StatusVersionLabel.Size = new System.Drawing.Size(43, 17);
            this._StatusVersionLabel.Text = "Verze 0";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(974, 475);
            this.Controls.Add(this._MainContainer);
            this.Controls.Add(this._StatusStrip);
            this.Controls.Add(this._ToolStrip);
            this.Name = "MainForm";
            this.Text = "Starter";
            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).EndInit();
            this._MainContainer.ResumeLayout(false);
            this._ToolStrip.ResumeLayout(false);
            this._ToolStrip.PerformLayout();
            this._StatusStrip.ResumeLayout(false);
            this._StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _ToolStrip;
        private System.Windows.Forms.StatusStrip _StatusStrip;
        private System.Windows.Forms.SplitContainer _MainContainer;
        private System.Windows.Forms.ToolStripButton _ToolEditButton;
        private System.Windows.Forms.ToolStripStatusLabel _StatusVersionLabel;
    }
}

