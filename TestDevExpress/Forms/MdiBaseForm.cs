using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using TestDevExpress.Components;

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

            _AsolRibbon = new RibbonControl()
            {
                ApplicationButtonText = " DJ soft "
            };
            _AsolRibbon.RibbonItemClick += _Ribbon_RibbonItemClick;
            this.Controls.Add(_AsolRibbon);

            this.RibbonVisibility = RibbonVisibility.Visible;
        }
        private void _Ribbon_RibbonItemClick(object sender, TEventArgs<IRibbonData> e)
        {
            if (e.Item is null) return;
            if (e.Item.ItemType == RibbonItemType.Menu) return;           // Pouze došlo k aktivaci Menu, nikoli k výběru konkrétní položky...
            this.OnRibbonItemClick(e.Item);
        }
        protected virtual void OnRibbonItemClick(IRibbonData ribbonData) { }
        public override DevExpress.XtraBars.Ribbon.RibbonControl Ribbon { get { return this._AsolRibbon; } set { this._AsolRibbon = value as TestDevExpress.RibbonControl; } }
        public TestDevExpress.RibbonControl AsolRibbon { get { return _AsolRibbon; } } private TestDevExpress.RibbonControl _AsolRibbon;
        public TestDevExpress.AsolPanel AsolPanel { get { return _AsolPanel; } } private TestDevExpress.AsolPanel _AsolPanel;
        protected virtual void AsolInitializeControls() { }
        protected virtual void AsolFillRibbon() { }

    }
}
