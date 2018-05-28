namespace Asol.Tools.WorkScheduler.TestGUI
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
            this._GInteractiveControl = new Asol.Tools.WorkScheduler.Components.GInteractiveControl();
            this.button1 = new System.Windows.Forms.Button();
            this._AxisEnabledCheck = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this._DocumentEditor = new Asol.Tools.WorkScheduler.Components.GDocumentEditor();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // _GInteractiveControl
            // 
            this._GInteractiveControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._GInteractiveControl.Location = new System.Drawing.Point(3, 3);
            this._GInteractiveControl.Name = "_GInteractiveControl";
            this._GInteractiveControl.RepaintAllItems = false;
            this._GInteractiveControl.Size = new System.Drawing.Size(683, 387);
            this._GInteractiveControl.TabIndex = 0;
            this._GInteractiveControl.Text = "_GInteractiveControl";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button1.Location = new System.Drawing.Point(12, 431);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(91, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "set SizeAxis";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // _AxisEnabledCheck
            // 
            this._AxisEnabledCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._AxisEnabledCheck.AutoSize = true;
            this._AxisEnabledCheck.Checked = true;
            this._AxisEnabledCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this._AxisEnabledCheck.Location = new System.Drawing.Point(125, 437);
            this._AxisEnabledCheck.Name = "_AxisEnabledCheck";
            this._AxisEnabledCheck.Size = new System.Drawing.Size(87, 17);
            this._AxisEnabledCheck.TabIndex = 2;
            this._AxisEnabledCheck.Text = "Axis Enabled";
            this._AxisEnabledCheck.UseVisualStyleBackColor = true;
            this._AxisEnabledCheck.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(697, 419);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this._GInteractiveControl);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(689, 393);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this._DocumentEditor);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(689, 393);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // _DocumentEditor
            // 
            this._DocumentEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._DocumentEditor.Location = new System.Drawing.Point(3, 3);
            this._DocumentEditor.Name = "_DocumentEditor";
            this._DocumentEditor.RepaintAllItems = false;
            this._DocumentEditor.Size = new System.Drawing.Size(683, 387);
            this._DocumentEditor.TabIndex = 0;
            this._DocumentEditor.Text = "gDocumentEditor1";
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 466);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this._AxisEnabledCheck);
            this.Controls.Add(this.button1);
            this.Name = "TestForm";
            this.Text = "Form1";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Components.GInteractiveControl _GInteractiveControl;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox _AxisEnabledCheck;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private Components.GDocumentEditor _DocumentEditor;
    }
}

