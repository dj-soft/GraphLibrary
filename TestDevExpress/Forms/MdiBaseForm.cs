using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DXR = DevExpress.XtraBars.Ribbon;
using TestDevExpress.Components;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    public partial class MdiBaseForm : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        public MdiBaseForm()
        {
            InitializeComponent();
            AsolInitializeBaseControls();
            AsolInitializeControls();
            AsolFillRibbon();
        }
        private void AsolInitializeBaseControls()
        {
            _AsolPanel = new AsolSamplePanel()
            {
                Name = "DynamicPage",
                Dock = DockStyle.Fill,
                Shape = AsolSamplePanel.ShapeType.Star8AcuteAngles

            };
            this.Controls.Add(_AsolPanel);

            _AsolRibbon = new DxRibbonControl() // RibbonControl()
            {
                ApplicationButtonText = " DJ soft "
            };
            _AsolRibbon.RibbonItemClick += _Ribbon_RibbonItemClick;
            this.Controls.Add(_AsolRibbon);

            this.RibbonVisibility = DXR.RibbonVisibility.Visible;
        }
        private void _Ribbon_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            if (e.Item is null) return;
            if (e.Item.RibbonItemType == RibbonItemType.Menu) return;           // Pouze došlo k aktivaci Menu, nikoli k výběru konkrétní položky...
            this.OnRibbonItemClick(e.Item);
        }
        protected virtual void OnRibbonItemClick(IMenuItem ribbonData) { }
        public override DevExpress.XtraBars.Ribbon.RibbonControl Ribbon { get { return this._AsolRibbon; } set { } }
        public DxRibbonControl AsolRibbon { get { return _AsolRibbon; } } private DxRibbonControl _AsolRibbon;
        public TestDevExpress.AsolPanel AsolPanel { get { return _AsolPanel; } } private TestDevExpress.AsolPanel _AsolPanel;
        protected virtual void AsolInitializeControls() { }
        protected virtual void AsolFillRibbon() { }

    }
}
