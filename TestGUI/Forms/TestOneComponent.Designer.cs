namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class TestOneComponent
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
            this._CloseButton = new System.Windows.Forms.Button();
            this._Control = new Asol.Tools.WorkScheduler.Components.GInteractiveControl();
            this._OneRowButton = new System.Windows.Forms.Button();
            this._AllRowsButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _CloseButton
            // 
            this._CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._CloseButton.Location = new System.Drawing.Point(544, 289);
            this._CloseButton.Name = "_CloseButton";
            this._CloseButton.Size = new System.Drawing.Size(139, 55);
            this._CloseButton.TabIndex = 0;
            this._CloseButton.Text = "Close";
            this._CloseButton.UseVisualStyleBackColor = true;
            this._CloseButton.Click += new System.EventHandler(this._CloseButton_Click);
            // 
            // _Control
            // 
            this._Control.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._Control.BackColor = System.Drawing.Color.LightBlue;
            this._Control.DefaultBackColor = System.Drawing.Color.LightBlue;
            this._Control.DefaultBorderColor = System.Drawing.Color.Black;
            this._Control.Location = new System.Drawing.Point(13, 15);
            this._Control.Name = "_Control";
            this._Control.RepaintAllItems = false;
            this._Control.Size = new System.Drawing.Size(670, 268);
            this._Control.TabIndex = 1;
            this._Control.Text = "gInteractiveControl1";
            // 
            // _OneRowButton
            // 
            this._OneRowButton.Location = new System.Drawing.Point(13, 299);
            this._OneRowButton.Name = "_OneRowButton";
            this._OneRowButton.Size = new System.Drawing.Size(150, 34);
            this._OneRowButton.TabIndex = 2;
            this._OneRowButton.Text = "Jeden řádek";
            this._OneRowButton.UseVisualStyleBackColor = true;
            this._OneRowButton.Click += new System.EventHandler(this._OneRowButton_Click);
            // 
            // _AllRowsButton
            // 
            this._AllRowsButton.Location = new System.Drawing.Point(169, 299);
            this._AllRowsButton.Name = "_AllRowsButton";
            this._AllRowsButton.Size = new System.Drawing.Size(150, 34);
            this._AllRowsButton.TabIndex = 3;
            this._AllRowsButton.Text = "Všechny řádky";
            this._AllRowsButton.UseVisualStyleBackColor = true;
            this._AllRowsButton.Click += new System.EventHandler(this._AllRowsButton_Click);
            // 
            // TestOneComponent
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(689, 356);
            this.Controls.Add(this._AllRowsButton);
            this.Controls.Add(this._OneRowButton);
            this.Controls.Add(this._Control);
            this.Controls.Add(this._CloseButton);
            this.Name = "TestOneComponent";
            this.Text = "TestOneComponent";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button _CloseButton;
        private Components.GInteractiveControl _Control;
        private System.Windows.Forms.Button _OneRowButton;
        private System.Windows.Forms.Button _AllRowsButton;
    }
}