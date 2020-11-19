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
        /// <summary>
        /// Adresní definice
        /// </summary>
        public Download.WebAdress WebAdress { get; set; }
        /// <summary>
        /// Cílový lokální adresář, root
        /// </summary>
        public string TargetPath { get; set; }
        /// <summary>
        /// Vytvořit sadu ukázkových adres
        /// </summary>
        /// <param name="count"></param>
        public void CreateSampleUrls(int count = 100)
        {
            this._PreviewTxt.Text = "";

            Download.WebAdress webAdress = this.WebAdress;
            if (webAdress == null) return;

            count = (count < 10 ? 10 : (count > 1000 ? 1000 : count));

            string targetPath = TargetPath ?? "";

            StringBuilder sb = new StringBuilder();
            for (int n = 0; n < count; n++)
            {
                string url = webAdress.Text;
                string file = Download.DownloadItem.CreateLocalPath(url, targetPath);
                sb.AppendLine(webAdress.Text + "\t=>\t" + file);
                if (webAdress.Increment()) break;
            }
            this._PreviewTxt.Text = sb.ToString();
        }
        
    }
}
