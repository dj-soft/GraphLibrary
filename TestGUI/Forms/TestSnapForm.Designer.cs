namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class TestSnapForm
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
            this.ConfigTree = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.configSnapSetPanel1 = new Asol.Tools.WorkScheduler.Scheduler.ConfigSnapSetPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConfigTree
            // 
            this.ConfigTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConfigTree.Location = new System.Drawing.Point(0, 0);
            this.ConfigTree.Name = "ConfigTree";
            this.ConfigTree.Size = new System.Drawing.Size(243, 703);
            this.ConfigTree.TabIndex = 7;
            // 
            // splitContainer1
            // 
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ConfigTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.configSnapSetPanel1);
            this.splitContainer1.Size = new System.Drawing.Size(854, 703);
            this.splitContainer1.SplitterDistance = 243;
            this.splitContainer1.TabIndex = 8;
            // 
            // configSnapSetPanel1
            // 
            this.configSnapSetPanel1.AutoScroll = true;
            this.configSnapSetPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configSnapSetPanel1.Location = new System.Drawing.Point(0, 0);
            this.configSnapSetPanel1.Name = "configSnapSetPanel1";
            this.configSnapSetPanel1.Size = new System.Drawing.Size(607, 703);
            this.configSnapSetPanel1.TabIndex = 0;
            // 
            // TestSnapForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(865, 755);
            this.Controls.Add(this.splitContainer1);
            this.Name = "TestSnapForm";
            this.Text = "TestSnapForm";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TreeView ConfigTree;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private Scheduler.ConfigSnapSetPanel configSnapSetPanel1;
    }
}