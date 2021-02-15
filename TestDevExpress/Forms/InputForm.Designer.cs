namespace TestDevExpress.Forms
{
    partial class InputForm
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
            this._Label = new System.Windows.Forms.Label();
            this._TextBox = new System.Windows.Forms.TextBox();
            this._GroupBox = new System.Windows.Forms.GroupBox();
            this._OkBtn = new System.Windows.Forms.Button();
            this._CancelBtn = new System.Windows.Forms.Button();
            this._GroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this._Label.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._Label.Location = new System.Drawing.Point(14, 11);
            this._Label.Name = "_Label";
            this._Label.Size = new System.Drawing.Size(277, 20);
            this._Label.TabIndex = 0;
            this._Label.Text = "Hodnota";
            // 
            // textBox1
            // 
            this._TextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._TextBox.Location = new System.Drawing.Point(6, 28);
            this._TextBox.Name = "_TextBox";
            this._TextBox.Size = new System.Drawing.Size(285, 20);
            this._TextBox.TabIndex = 1;
            // 
            // groupBox1
            // 
            this._GroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._GroupBox.Controls.Add(this._TextBox);
            this._GroupBox.Controls.Add(this._Label);
            this._GroupBox.Location = new System.Drawing.Point(12, 4);
            this._GroupBox.Name = "_GroupBox";
            this._GroupBox.Size = new System.Drawing.Size(297, 53);
            this._GroupBox.TabIndex = 2;
            this._GroupBox.TabStop = false;
            // 
            // OkButton
            // 
            this._OkBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._OkBtn.Location = new System.Drawing.Point(12, 63);
            this._OkBtn.Name = "_OkBtn";
            this._OkBtn.Size = new System.Drawing.Size(100, 34);
            this._OkBtn.TabIndex = 3;
            this._OkBtn.Text = "OK";
            this._OkBtn.UseVisualStyleBackColor = true;
            // 
            // CancelBtn
            // 
            this._CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._CancelBtn.Location = new System.Drawing.Point(118, 63);
            this._CancelBtn.Name = "_CancelBtn";
            this._CancelBtn.Size = new System.Drawing.Size(100, 34);
            this._CancelBtn.TabIndex = 4;
            this._CancelBtn.Text = "Storno";
            this._CancelBtn.UseVisualStyleBackColor = true;
            // 
            // InputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(321, 106);
            this.Controls.Add(this._CancelBtn);
            this.Controls.Add(this._OkBtn);
            this.Controls.Add(this._GroupBox);
            this.MaximumSize = new System.Drawing.Size(1200, 145);
            this.MinimumSize = new System.Drawing.Size(300, 145);
            this.Name = "InputForm";
            this.Text = "Vyplňte hodnotu...";
            this._GroupBox.ResumeLayout(false);
            this._GroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _Label;
        private System.Windows.Forms.TextBox _TextBox;
        private System.Windows.Forms.GroupBox _GroupBox;
        private System.Windows.Forms.Button _OkBtn;
        private System.Windows.Forms.Button _CancelBtn;
    }
}