using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Djs.Tools.CopyFile
{
    internal class FileContentMapPanel : Panel
    {
        #region Konstrukce a layout
        public FileContentMapPanel()
        {
            this._CreateControls();
        }
        private void _CreateControls()
        {
            this.SuspendLayout();

            this._ProcessFileNameLabel = new Label() { Text = "Název souboru", Bounds = new Rectangle(28, 8, 509, 13), TextAlign = ContentAlignment.MiddleLeft };
            this._ProcessFileNameLabel.Font = new Font(this._ProcessFileNameLabel.Font, FontStyle.Bold);
            this.Controls.Add(this._ProcessFileNameLabel);

            this._ProcessCopyInfoLabel = new Label() { Text = "Průběh zpracování", Bounds = new Rectangle(28, 26, 509, 13), TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(this._ProcessCopyInfoLabel);

            this._ProgressMap = new Djs.Tools.CopyFile.Components.ProgressMap() { Bounds = new Rectangle(6, 45, 550, 174) };
            this.Controls.Add(this._ProgressMap);

            this._ErrorCountLabel = new Label() { Text = "Počet chyb:", Bounds = new Rectangle(15, 214, 99, 13) };
            this.Controls.Add(this._ErrorCountLabel);

            this._ErrorCountTxt = new TextBox() { Bounds = new Rectangle(18, 230, 82, 20), TextAlign = HorizontalAlignment.Right, Enabled = false };
            this.Controls.Add(this._ErrorCountTxt);

            this._ErrorPercentLabel = new Label() { Text = "Procento chyb:", Bounds = new Rectangle(135, 214, 145, 13) };
            this.Controls.Add(this._ErrorPercentLabel);

            this._ErrorPercentTxt = new TextBox() { Bounds = new Rectangle(138, 230, 82, 20), TextAlign = HorizontalAlignment.Right, Enabled = false };
            this.Controls.Add(this._ErrorPercentTxt);
           
            this.ResumeLayout(false);
            this.PerformLayout();

            this.ClientSizeChanged += _ClientSizeChanged;
            this._DoLayout();
        }

        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this._DoLayout();
        }
        private void _DoLayout()
        {
            var size = this.ClientSize;
            var w = size.Width;
            var h = size.Height;
            var b = h - 51;

            this._ProcessFileNameLabel.Bounds = new Rectangle(28, 8, w - 16, 13);
            this._ProcessCopyInfoLabel.Bounds = new Rectangle(28, 26, w - 16, 13);
            this._ProgressMap.Bounds = new Rectangle(6, 45, w - 12, b - 45);

            this._ErrorCountLabel.Bounds = new Rectangle(15, b + 8, 100, 13);
            this._ErrorCountTxt.Bounds = new Rectangle(18, b + 25, 82, 20);
            this._ErrorPercentLabel.Bounds = new Rectangle(135, b + 8, 100, 13);
            this._ErrorPercentTxt.Bounds = new Rectangle(138, b + 25, 82, 20);
        }
        private TextBox _ErrorPercentTxt;
        private Label _ErrorPercentLabel;
        private TextBox _ErrorCountTxt;
        private Label _ErrorCountLabel;
        private Label _ProcessCopyInfoLabel;
        private Label _ProcessFileNameLabel;
        private Components.ProgressMap _ProgressMap;
        #endregion
        #region Data
        internal string ProcessFileName 
        { 
            get { return this.__ProcessFileName; } 
            set 
            {
                bool isChange = !String.Equals(value, __ProcessFileName, StringComparison.Ordinal);
                if (isChange)
                {
                    this.__ProcessFileName = value;
                    this._ProcessFileNameLabel.Text = value;
                }
            }
        }
        private string __ProcessFileName;

        internal string ProcessCopyInfo 
        {
            get { return this.__ProcessCopyInfo; }
            set
            {
                bool isChange = !String.Equals(value, __ProcessCopyInfo, StringComparison.Ordinal);
                if (isChange)
                {
                    this.__ProcessCopyInfo = value;
                    this._ProcessCopyInfoLabel.Text = value;
                }
            }
        }
        private string __ProcessCopyInfo;

        internal int? ProcessErrorCount 
        {
            get { return this.__ProcessErrorCount; }
            set
            {
                bool isChange = (value != __ProcessErrorCount);
                if (isChange)
                {
                    this.__ProcessErrorCount = value;
                    this._ErrorCountTxt.Text = value?.ToString() ?? "";
                }
            }
        }
        private int? __ProcessErrorCount;

        internal int? ProcessErrorPercent 
        {
            get { return this.__ProcessErrorPercent; }
            set
            {
                bool isChange = (value != __ProcessErrorPercent);
                if (isChange)
                {
                    this.__ProcessErrorPercent = value;
                    this._ErrorPercentTxt.Text = value?.ToString() ?? "";
                }
            }
        }
        private int? __ProcessErrorPercent;
        #endregion
        #region Mapa
        public FileContentMap ContentMap { get { return __ContentMap; } set { __ContentMap = value; _ReloadContentMap(); } }
        private FileContentMap __ContentMap;
        private void _ReloadContentMap()
        {
            if (this.InvokeRequired)
                this.BeginInvoke((Delegate)new Action(_ReloadContentMapGui));
            else
                _ReloadContentMapGui();
        }
        private void _ReloadContentMapGui()
        {
            var contentMap = ContentMap;

            this.ProcessFileName = contentMap?.FileName ?? "";
            // this.ProcessCopyInfo = contentMap?.sta ?? "";
            // this.ProcessErrorCount = contentMap?.FileName ?? "";
            // this.ProcessErrorPercent = contentMap?.FileName ?? "";

        }
        #endregion
        #region Progress
        public void ResetProgress()
        {
            this.ProcessFileName = "";
            this.ProcessCopyInfo = "";
            this.ProcessErrorCount = 0;
            this.ProcessErrorPercent = 0;
        }
        public void ShowProgress(FileReadProgressEventArgs args)
        {
            switch (args.State)
            {
                case ProgressStateType.Begin:
                    //ProcessFileName = args.Name;
                    //ProcessPosition = 0;
                    //ProcessCopyInfo = "";
                    this.ProcessFileName = args.Name;
                    this.ProcessCopyInfo = args.StatusInfo;
                    break;
                case ProgressStateType.Read:
                case ProgressStateType.Written:
                case ProgressStateType.Error:
                    this.ProcessCopyInfo = args.StatusInfo;
                    break; ;
            }
        }
        #endregion

    }
}
