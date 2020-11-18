using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Djs.Tools.WebDownloader.UI
{
    public partial class PreviewForm : Form
    {
        public PreviewForm()
        {
            InitializeComponent();
        }
        public Download.WebAdress WebAdress { get { return this._WebAdress; } set { this._WebAdress = value; this._FillText(); } }

        private void _FillText()
        {
            this._PreviewTxt.Text = "";
            Download.WebAdress webAdress = this.WebAdress;
            if (webAdress == null) return;

            StringBuilder sb = new StringBuilder();
            for (int n = 0; n < 500; n++)
            {
                string url = webAdress.Text;
                string file = Download.DownloadItem.CreateLocalPath(url);
                sb.AppendLine(webAdress.Text + "\t=>\t" + file);
                if (webAdress.Increment()) break;
            }
            this._PreviewTxt.Text = sb.ToString();
        }
        private Download.WebAdress _WebAdress;
    }
}
