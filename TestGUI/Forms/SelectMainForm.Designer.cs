namespace Asol.Tools.WorkScheduler.TestGUI.Forms
{
    partial class SelectMainForm
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
            this._ListTypes = new System.Windows.Forms.ListView();
            this._ButtonRun = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _ListTypes
            // 
            this._ListTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._ListTypes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._ListTypes.HideSelection = false;
            this._ListTypes.Location = new System.Drawing.Point(12, 12);
            this._ListTypes.Name = "_ListTypes";
            this._ListTypes.Size = new System.Drawing.Size(609, 269);
            this._ListTypes.TabIndex = 0;
            this._ListTypes.UseCompatibleStateImageBehavior = false;
            // 
            // _ButtonRun
            // 
            this._ButtonRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._ButtonRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this._ButtonRun.Location = new System.Drawing.Point(636, 226);
            this._ButtonRun.Name = "_ButtonRun";
            this._ButtonRun.Size = new System.Drawing.Size(152, 54);
            this._ButtonRun.TabIndex = 1;
            this._ButtonRun.Text = "Start !";
            this._ButtonRun.UseVisualStyleBackColor = true;
            this._ButtonRun.Click += new System.EventHandler(this._ButtonRun_Click);
            // 
            // SelectMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 293);
            this.Controls.Add(this._ButtonRun);
            this.Controls.Add(this._ListTypes);
            this.Name = "SelectMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vyberte formulář ke spuštění";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView _ListTypes;
        private System.Windows.Forms.Button _ButtonRun;
    }
}