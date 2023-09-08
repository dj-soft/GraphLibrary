
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
            this._ToolStrip = new System.Windows.Forms.ToolStrip();
            this._StatusStrip = new System.Windows.Forms.StatusStrip();
            this._MainContainer = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).BeginInit();
            this._MainContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // _ToolStrip
            // 
            this._ToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._ToolStrip.ImageScalingSize = new System.Drawing.Size(48, 48);
            this._ToolStrip.Location = new System.Drawing.Point(0, 0);
            this._ToolStrip.Name = "_ToolStrip";
            this._ToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._ToolStrip.Size = new System.Drawing.Size(969, 25);
            this._ToolStrip.TabIndex = 0;
            this._ToolStrip.Text = "toolStrip1";
            // 
            // _StatusStrip
            // 
            this._StatusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._StatusStrip.Location = new System.Drawing.Point(0, 399);
            this._StatusStrip.Name = "_StatusStrip";
            this._StatusStrip.Size = new System.Drawing.Size(969, 22);
            this._StatusStrip.TabIndex = 1;
            this._StatusStrip.Text = "";
            // 
            // _MainContainer
            // 
            this._MainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._MainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._MainContainer.IsSplitterFixed = true;
            this._MainContainer.Location = new System.Drawing.Point(0, 25);
            this._MainContainer.Name = "_MainContainer";
            this._MainContainer.Size = new System.Drawing.Size(969, 374);
            this._MainContainer.SplitterDistance = 144;
            this._MainContainer.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(969, 421);
            this.Controls.Add(this._MainContainer);
            this.Controls.Add(this._StatusStrip);
            this.Controls.Add(this._ToolStrip);
            this.Name = "MainForm";
            this.Text = "Starter";
            ((System.ComponentModel.ISupportInitialize)(this._MainContainer)).EndInit();
            this._MainContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _ToolStrip;
        private System.Windows.Forms.StatusStrip _StatusStrip;
        private System.Windows.Forms.SplitContainer _MainContainer;
    }
}

