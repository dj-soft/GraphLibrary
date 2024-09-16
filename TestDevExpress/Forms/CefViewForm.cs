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
    /// <summary>
    /// Web viewer typu AntView
    /// </summary>
//  [RunFormInfo(groupText: "Testovací okna", buttonText: "Cef View", buttonOrder: 72, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře CefView prohlížeč (MS Edge based)", tabViewToolTip: "CefView Browser")]
    public partial class CefViewForm : Form
    {
        #region Konstrukce

        public CefViewForm()
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

            this._Button1 = new Button();
            this._Button2 = new Button();
            this._Button3 = new Button();
            this._Button4 = new Button();
            this._UrlAdress = new TextBox();
            this._ButtonGo = new Button();

            this.SuspendLayout();

            this._Button1.Bounds = new Rectangle(12, 12, 176, 28);
            this._Button1.TabIndex = 1;
            this._Button1.Text = "Seznam.cz";
            this._Button1.UseVisualStyleBackColor = true;
            this._Button1.Click += new System.EventHandler(this.button1_Click);

            this._Button2.Bounds = new Rectangle(194, 12, 176, 28);
            this._Button2.TabIndex = 2;
            this._Button2.Text = "mapa.cz";
            this._Button2.UseVisualStyleBackColor = true;
            this._Button2.Click += new System.EventHandler(this.button2_Click);


            this._Button3.Bounds = new Rectangle(376, 12, 176, 28);
            this._Button3.TabIndex = 3;
            this._Button3.Text = "google.com";
            this._Button3.UseVisualStyleBackColor = true;
            this._Button3.Click += new System.EventHandler(this.button3_Click);

            this._Button4.Bounds = new Rectangle(558, 12, 176, 28);
            this._Button4.TabIndex = 4;
            this._Button4.Text = "Mapa Nephrite";
            this._Button4.UseVisualStyleBackColor = true;
            this._Button4.Click += new System.EventHandler(this.button4_Click);

            this._UrlAdress.Location = new Point(740, 14);
            this._UrlAdress.ReadOnly = false;

            this._ButtonGo.Bounds = new Rectangle(1200, 12, 50, 28);
            this._ButtonGo.TabIndex = 6;
            this._ButtonGo.Text = "=>";
            this._ButtonGo.UseVisualStyleBackColor = true;
            this._ButtonGo.Click += new System.EventHandler(this.buttonGo_Click);

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.Controls.Add(this._Button1);
            this.Controls.Add(this._Button2);
            this.Controls.Add(this._Button3);
            this.Controls.Add(this._Button4);
            this.Controls.Add(this._UrlAdress);
            this.Controls.Add(this._ButtonGo);

            this.Text = "AntView (MS Edge Browser)";

            this.SizeChanged += _Form_SizeChanged;

            this.ResumeLayout(false);

        }
        private void _Form_SizeChanged(object sender, EventArgs e)
        {
            _DoLayout();
        }
        private Button _Button1;
        private Button _Button2;
        private Button _Button3;
        private Button _Button4;
        private TextBox _UrlAdress;
        private Button _ButtonGo;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        ComponentResourceManager _Resources;
        #endregion
        #region Buttony a jejich události
        private void button1_Click(object sender, EventArgs e)
        {
            _PrepareWView();
            _DoNavigate("https://www.seznam.cz/");
        }
        private void button2_Click(object sender, EventArgs e)
        {
            _PrepareWView();
            _DoNavigate(@"https://mapy.cz/dopravni?l=0&x=15.8629028&y=50.2145999&z=17");        // https://mapy.cz/dopravni?x=14.5802973&y=50.5311090&z=14
        }
        private void button3_Click(object sender, EventArgs e)
        {
            _PrepareWView();
            _DoNavigate(@"https://google.com");
        }
        private void button4_Click(object sender, EventArgs e)
        {
            _PrepareWView();
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
            _DoNavigate(html, true);
        }
        private void buttonGo_Click(object sender, EventArgs e)
        {
            _PrepareWView();
            string url = this._UrlAdress.Text;
            _DoNavigate(url, true);
        }
        #endregion
        #region Layout a _UrlAdress
        private void _DoLayout()
        {
            var clientSize = this.ClientSize;
            if (_WView != null)
            {
                _WView.Bounds = new Rectangle(12, 50, clientSize.Width - 24, clientSize.Height - 59);
            }

            int cw = clientSize.Width;
            int w = cw - this._UrlAdress.Left - this._ButtonGo.Width - 18;
            if (w < 100) w = 100;
            this._UrlAdress.Width = w;
            this._ButtonGo.Left = this._UrlAdress.Right + 6;
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
        #endregion
        #region Konkrétní WView
        private CefSharp.WinForms.Host.ChromiumHostControl _WView;
        private void _PrepareWView()
        {
            if (_WView != null)
            {
                DxComponent.TryRun(() => _WView.Dispose(), true);
                _WView = null;
            }

            this._WView = new CefSharp.WinForms.Host.ChromiumHostControl();

            // ((System.ComponentModel.ISupportInitialize)(this._WView)).BeginInit();

            this.Controls.Add(this._WView);
            // this._AxAntView.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            // this._AxAntView.ParentDoubleBuffered = true;
            // this._AxAntView.DoubleBuffered = true;
            this._WView.Location = new System.Drawing.Point(12, 49);
            this._WView.Name = "_AxAntView";
            this._WView.Size = new System.Drawing.Size(776, 389);
            this._WView.TabIndex = 0;

            /*
            this._WView.OnNavigationStarting += _AxAntView_OnNavigationStarting;
            this._WView.OnFrameNavigationStarting += _AxAntView_OnFrameNavigationStarting;
            this._WView.OnFrameNavigationCompleted += _AxAntView_OnFrameNavigationCompleted;
            this._WView.OnNavigationCompleted += _AxAntView_OnNavigationCompleted;
            this._WView.OnSourceChanged += _AxAntView_OnSourceChanged;

            var ddl = this._WView.DemoDaysLeft;
            */

            // ((System.ComponentModel.ISupportInitialize)(this._WView)).EndInit();

            _DoLayout();
        }
        private void _DoNavigate(string source, bool asAsync = false)
        {
            if (!asAsync)
            {
                _WView.LoadUrl(source);
            }
            else
            {
            }

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



        /*
        private void _AxAntView_OnSourceChanged(object sender, AxAntViewAx.IAntViewEvents_OnSourceChangedEvent e)
        {
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, $"AntView.OnSourceChanged: '{_WView.Source}'");
            _RefreshUrl(_WView.Source);
        }
        private void _AxAntView_OnFrameNavigationStarting(object sender, AxAntViewAx.IAntViewEvents_OnFrameNavigationStartingEvent e)
        {
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, $"AntView.OnFrameNavigationStarting: #{e.args.NavigationId}: '{e.args.URI}'");
        }
        private void _AxAntView_OnFrameNavigationCompleted(object sender, AxAntViewAx.IAntViewEvents_OnFrameNavigationCompletedEvent e)
        {
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, $"AntView.OnFrameNavigationCompleted: #{e.navigationId}");
        }
        private void _AxAntView_OnNavigationStarting(object sender, AxAntViewAx.IAntViewEvents_OnNavigationStartingEvent e)
        {
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, $"AntView.OnNavigationStarting: #{e.args.NavigationId}: '{e.args.URI}'");
            _RefreshUrl(e.args.URI);
        }
        private void _AxAntView_OnNavigationCompleted(object sender, AxAntViewAx.IAntViewEvents_OnNavigationCompletedEvent e)
        {
            DxComponent.LogAddLine(LogActivityKind.DataFormEvents, $"AntView.OnNavigationCompleted: '{_WView.Source}'");
        }
        */
        #endregion
    }
}

