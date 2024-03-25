using Noris.Clients.Win.Components.AsolDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestDevExpress.Forms
{
    [RunFormInfo(groupText: "Testovací okna", buttonText: "Ant View", buttonOrder: 70, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře AntView prohlížeč (MS Edge based)", tabViewToolTip: "AntView Browser")]
    public partial class AntViewForm : Form
    {
        #region Konstrukce

        public AntViewForm()
        {
            InitializeComponent();
        }
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
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _Resources = new System.ComponentModel.ComponentResourceManager(typeof(AntViewForm));

            this._Button1 = new System.Windows.Forms.Button();
            this._Button2 = new System.Windows.Forms.Button();
            this._Button3 = new System.Windows.Forms.Button();
            this._Button4 = new System.Windows.Forms.Button();
            this._UrlAdress = new TextBox();

            this.SuspendLayout();
            // 
            // button1
            // 
            this._Button1.Location = new System.Drawing.Point(12, 12);
            this._Button1.Name = "button1";
            this._Button1.Size = new System.Drawing.Size(176, 28);
            this._Button1.TabIndex = 1;
            this._Button1.Text = "Seznam.cz";
            this._Button1.UseVisualStyleBackColor = true;
            this._Button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this._Button2.Location = new System.Drawing.Point(194, 12);
            this._Button2.Name = "button2";
            this._Button2.Size = new System.Drawing.Size(176, 28);
            this._Button2.TabIndex = 2;
            this._Button2.Text = "mapa.cz";
            this._Button2.UseVisualStyleBackColor = true;
            this._Button2.Click += new System.EventHandler(this.button2_Click);

            // 
            // button3
            // 
            this._Button3.Location = new System.Drawing.Point(376, 12);
            this._Button3.Name = "button3";
            this._Button3.Size = new System.Drawing.Size(176, 28);
            this._Button3.TabIndex = 3;
            this._Button3.Text = "google.com";
            this._Button3.UseVisualStyleBackColor = true;
            this._Button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this._Button4.Location = new System.Drawing.Point(558, 12);
            this._Button4.Name = "button4";
            this._Button4.Size = new System.Drawing.Size(176, 28);
            this._Button4.TabIndex = 4;
            this._Button4.Text = "Mapa Nephrite";
            this._Button4.UseVisualStyleBackColor = true;
            this._Button4.Click += new System.EventHandler(this.button4_Click);

            this._UrlAdress.Location = new Point(740, 14);
            this._UrlAdress.ReadOnly = true;

            // 
            // _EmptyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this._Button4);
            this.Controls.Add(this._Button3);
            this.Controls.Add(this._Button2);
            this.Controls.Add(this._Button1);
            this.Controls.Add(this._UrlAdress);

            this.Name = "_EmptyForm";
            this.Text = "_EmptyForm";

            // this.Load += new System.EventHandler(this._EmptyForm_Load);
            this.SizeChanged += _AntViewForm_SizeChanged;

            this.ResumeLayout(false);

        }

        private void _AntViewForm_SizeChanged(object sender, EventArgs e)
        {
            DoLayout();
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private AxAntViewAx.AxAntview _AxAntView;
        private System.Windows.Forms.TextBox _UrlAdress;
        private System.Windows.Forms.Button _Button1;
        private System.Windows.Forms.Button _Button2;
        private System.Windows.Forms.Button _Button3;
        private System.Windows.Forms.Button _Button4;
        #endregion

        private void _EmptyForm_Load(object sender, EventArgs e)
        {
            _PrepareAntView();
            _AxAntView.Navigate("https://www.seznam.cz/");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _PrepareAntView();
            _AxAntView.Navigate("https://www.seznam.cz/");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _PrepareAntView();
            _AxAntView.Navigate(@"https://mapy.cz/dopravni?l=0&x=15.8629028&y=50.2145999&z=17");        // https://mapy.cz/dopravni?x=14.5802973&y=50.5311090&z=14
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _PrepareAntView();
            _AxAntView.Navigate(@"https://google.com");
        }
        private void button4_Click(object sender, EventArgs e)
        {
            _PrepareAntView();
            string html = @"<!DOCTYPE html>
<html>
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=10"" />
<meta charset=""utf-8"" />
<script type=""text/javascript"" src=""http://api.mapy.cz/loader.js""></script>
<script type=""text/javascript"">Loader.load();</script>
</head>
<body style=""padding: 0; margin: 0"">
<div id=""m"" style=""padding: 0; margin: 0;height:441px;""></div>
</body>
<script type=""text/javascript"">
function afterMapLoaded()
{
window.external.callClientEvent(""AfterMapLoaded"","""");
}
var mapSignalsCallback = function(e) {
afterMapLoaded();
}
var center = SMap.Coords.fromWGS84(15.3351975,49.7420097);
var m = new SMap(JAK.gel(""m""), center, 7);
var mapSignals = m.getSignals();
mapSignals.addListener(null, ""tileset-load"", mapSignalsCallback);
m.addDefaultLayer(SMap.DEF_BASE).enable();
layer = new SMap.Layer.Marker();
var layerId = layer.getId();
m.addLayer(layer);
var sync = new SMap.Control.Sync({});
m.addControl(sync);
</script>
</html>
";

            bool isSuccess = true;
            var errStatus = AntViewAx.TxWebErrorStatus.wesUnknown;
            _AxAntView.NavigateToStringSync(html, ref isSuccess, ref errStatus);


            /*
            var targetPdf = System.IO.Path.Combine(DxComponent.ApplicationPath, "AntViewImage.pdf");
            _AxAntView.PrintToPdf(targetPdf, "pdf");

            var size = _AxAntView.Size;
            using (var bitmap = new Bitmap(size.Width, size.Height))
            {
                var targetBounds = new Rectangle(0, 0, size.Width, size.Height);
                var targetFile = System.IO.Path.Combine(DxComponent.ApplicationPath, "AntViewImage.png");
                _AxAntView.DrawToBitmap(bitmap, targetBounds);
                bitmap.Save(targetFile, System.Drawing.Imaging.ImageFormat.Png);
            }
            */
        }

        private void _PrepareAntView()
        {
            if (_AxAntView != null)
            {
                _AxAntView.Dispose();
                _AxAntView = null;
            }

            this._AxAntView = new AxAntViewAx.AxAntview();
            
            ((System.ComponentModel.ISupportInitialize)(this._AxAntView)).BeginInit();
            this.Controls.Add(this._AxAntView);
            this._AxAntView.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this._AxAntView.Location = new System.Drawing.Point(12, 49);
            this._AxAntView.Name = "_AxAntView";
            this._AxAntView.OcxState = ((System.Windows.Forms.AxHost.State)(_Resources.GetObject("axAntview1.OcxState")));
            this._AxAntView.Size = new System.Drawing.Size(776, 389);
            this._AxAntView.TabIndex = 0;
            this._AxAntView.OnNavigationStarting += _AxAntView_OnNavigationStarting;
            this._AxAntView.OnFrameNavigationStarting += _AxAntView_OnFrameNavigationStarting;
            this._AxAntView.OnFrameNavigationCompleted += _AxAntView_OnFrameNavigationCompleted;
            this._AxAntView.OnNavigationCompleted += _AxAntView_OnNavigationCompleted;
            this._AxAntView.OnSourceChanged += _AxAntView_OnSourceChanged;

            var ddl = this._AxAntView.DemoDaysLeft;

            ((System.ComponentModel.ISupportInitialize)(this._AxAntView)).EndInit();

            DoLayout();
        }

        private void _AxAntView_OnSourceChanged(object sender, AxAntViewAx.IAntViewEvents_OnSourceChangedEvent e)
        {
            _RefreshUrl(_AxAntView.Source);
        }

        private void _AxAntView_OnFrameNavigationStarting(object sender, AxAntViewAx.IAntViewEvents_OnFrameNavigationStartingEvent e)
        {
        }

        private void _AxAntView_OnFrameNavigationCompleted(object sender, AxAntViewAx.IAntViewEvents_OnFrameNavigationCompletedEvent e)
        {
        }

        private void _AxAntView_OnNavigationStarting(object sender, AxAntViewAx.IAntViewEvents_OnNavigationStartingEvent e)
        {
            _RefreshUrl(e.args.URI);
        }
        private void _AxAntView_OnNavigationCompleted(object sender, AxAntViewAx.IAntViewEvents_OnNavigationCompletedEvent e)
        {
        }

        private void _RefreshUrl(string uri)
        {
            _NavigatedUri = uri;
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(_RefreshUrlText));
            else
                _RefreshUrlText();
        }
        private void _RefreshUrlText()
        {
            this._UrlAdress.Text = _NavigatedUri;
        }
        private string _NavigatedUri;
        private void DoLayout()
        {
            var clientSize = this.ClientSize;
            if (_AxAntView != null)
            {
                _AxAntView.Bounds = new Rectangle(12, 50, clientSize.Width - 24, clientSize.Height - 59);
            }

            int w = clientSize.Width - this._UrlAdress.Left - 12;
            if (w < 100) w = 100;
            this._UrlAdress.Width = w;
        }
        ComponentResourceManager _Resources;

    }
}
