using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using WF = System.Windows.Forms;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Components
{
    public class LayoutForm : DevExpress.XtraEditors.XtraForm
    {
        public LayoutForm() : this(false)
        { }
        public LayoutForm(bool useDevExpress)
        {
            _LayoutPanel = new DxLayoutPanel()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                SplitterContextMenuEnabled = true,
                DockButtonVisibility = ControlVisibility.OnMouse,
                CloseButtonVisibility = ControlVisibility.OnNonPrimaryPanelAllways,
                DockButtonLeftToolTip = "Přemístit tento panel doleva",
                DockButtonTopToolTip = "Přemístit tento panel nahoru",
                DockButtonBottomToolTip = "Přemístit tento panel dolů",
                DockButtonRightToolTip = "Přemístit tento panel doprava",
                CloseButtonToolTip = "Zavřít tento panel",
                UseSvgIcons = true
            };
            _LayoutPanel.LastControlRemoved += _LayoutPanel_LastControlRemoved;
            _LayoutPanel.SplitterPositionChanged += _LayoutPanel_SplitterPositionChanged;
            _LayoutPanel.LayoutPanelChanged += _LayoutPanel_LayoutPanelChanged;
            _LayoutPanel.XmlLayoutChanged += _LayoutPanel_XmlLayoutChanged;

            this.Controls.Add(_LayoutPanel);

            _FunctionPanel = DxComponent.CreateDxPanel(this, DockStyle.Top, height: 45);
            _CopyLayoutButton = DxComponent.CreateDxSimpleButton(10, 6, 150, 37, _FunctionPanel, "Copy XmlLayout", _CopyLayoutButtonClick, toolTipText: "Zkopíruje aktuální XML layout do schránky");
            _PasteLayoutButton = DxComponent.CreateDxSimpleButton(170, 6, 150, 37, _FunctionPanel, "Paste XmlLayout", _PasteLayoutButtonClick, toolTipText: "Vloží text ze schránky do XML layoutu");

            _SetLayout1Button = DxComponent.CreateDxSimpleButton(340, 6, 150, 37, _FunctionPanel, "Set Layout 1", _SetLayout1ButtonClick, toolTipText: "Vloží fixní layout 1");
            _SetLayout2Button = DxComponent.CreateDxSimpleButton(500, 6, 150, 37, _FunctionPanel, "Set Layout 2", _SetLayout2ButtonClick, toolTipText: "Vloží fixní layout 2");
            _SetLayout3Button = DxComponent.CreateDxSimpleButton(660, 6, 150, 37, _FunctionPanel, "Set Layout 3", _SetLayout3ButtonClick, toolTipText: "Vloží fixní layout 3");
            _SetLayout4Button = DxComponent.CreateDxSimpleButton(820, 6, 150, 37, _FunctionPanel, "Set Layout 4", _SetLayout4ButtonClick, toolTipText: "Vloží fixní layout 4");

            Rectangle monitorBounds = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            Rectangle formBounds = new Rectangle(monitorBounds.X + monitorBounds.Width * 1 / 10, monitorBounds.Y + monitorBounds.Height * 1 / 10, monitorBounds.Width * 8 / 10, monitorBounds.Height * 8 / 10);

            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Bounds = formBounds;

            this._Timer = new Timer() { Interval = 1800 };
            this._Timer.Tick += _Timer_Tick;
            this._Timer.Enabled = true;
        }
        private void _CopyLayoutButtonClick(object sender, EventArgs e)
        {
            string text = "";
            var xmlLayout = _LayoutPanel.XmlLayout;
            var controls = _LayoutPanel.DxLayoutItems;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                string space = "                ";
                string eol = Environment.NewLine;

                string areaIds = "";
                foreach (var control in controls)
                    areaIds += (areaIds.Length == 0 ? "" : "; ") + control.AreaId;

                string code = space + "string xmlLayout = @\"" + xmlLayout.Replace("\"", "'") + "\";" + eol +
                              space + "string areaIds = \"" + areaIds + "\";" + eol +
                              space + "ApplyLayout(xmlLayout, areaIds);" + eol;

                text = code;
            }
            else
            {
                text = xmlLayout;
            }

            Clipboard.Clear();
            Clipboard.SetText(text);
        }
        private void _PasteLayoutButtonClick(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                var xmlLayoutTest = _LayoutPanel.XmlLayout;
                var xmlLayoutClip = Clipboard.GetText();
                if (xmlLayoutClip != null && xmlLayoutClip.Length > 50 && xmlLayoutClip.Length < 25000 && xmlLayoutClip.Substring(0,38) == xmlLayoutTest.Substring(0, 38))
                    _LayoutPanel.XmlLayout = xmlLayoutClip;
            }
        }
        private void _SetLayout1ButtonClick(object sender, EventArgs e)
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-16 23:03:30.992' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxLayoutItemPanel' ControlID='1' IsSplitterFixed='false' />
 </id-data>
</id-persistent>";
            string areaIds = "C";
            ApplyLayout(xmlLayout, areaIds);
        }
        private void _SetLayout2ButtonClick(object sender, EventArgs e)
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-16 23:20:20.977' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Vertical' SplitterPosition='325' SplitterRange='1376'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='2' IsSplitterFixed='false' />
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Horizontal' SplitterPosition='205' SplitterRange='781'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='3' IsSplitterFixed='false' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Vertical' SplitterPosition='809' SplitterRange='1046'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Horizontal' SplitterPosition='278' SplitterRange='571'>
      <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1/P1' Content='DxLayoutItemPanel' ControlID='1' IsSplitterFixed='false' />
      <id-value id-value.Target='Content2' AreaID='C/P2/P2/P1/P2' Content='DxLayoutItemPanel' ControlID='5' IsSplitterFixed='false' />
     </id-value>
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='4' IsSplitterFixed='false' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1; C/P2/P1; C/P2/P2/P1/P1; C/P2/P2/P1/P2; C/P2/P2/P2";
            ApplyLayout(xmlLayout, areaIds);
        }
        private void _SetLayout3ButtonClick(object sender, EventArgs e)
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-16 23:35:06.115' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Vertical' SplitterPosition='396' SplitterRange='1376'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='1' IsSplitterFixed='false' />
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Horizontal' SplitterPosition='256' SplitterRange='781'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='4' IsSplitterFixed='false' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxLayoutItemPanel' ControlID='5' IsSplitterFixed='false' />
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1; C/P2/P1; C/P2/P2";
            ApplyLayout(xmlLayout, areaIds);
        }
        private void _SetLayout4ButtonClick(object sender, EventArgs e)
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-16 23:37:10.083' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Horizontal' SplitterPosition='162' SplitterRange='781'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='26' IsSplitterFixed='false' />
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Vertical' SplitterPosition='342' SplitterRange='1376'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='28' IsSplitterFixed='false' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Vertical' SplitterPosition='694' SplitterRange='1029'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxSplitContainer' FixedPanel='Panel1' IsSplitterFixed='false' MinSize1='100' MinSize2='100' SplitterOrientation='Horizontal' SplitterPosition='247' SplitterRange='614'>
      <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1/P1' Content='DxLayoutItemPanel' ControlID='29' IsSplitterFixed='false' />
      <id-value id-value.Target='Content2' AreaID='C/P2/P2/P1/P2' Content='DxLayoutItemPanel' ControlID='31' IsSplitterFixed='false' />
     </id-value>
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='30' IsSplitterFixed='false' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1; C/P2/P1; C/P2/P2/P1/P1; C/P2/P2/P1/P2; C/P2/P2/P2";
            ApplyLayout(xmlLayout, areaIds);

        }
        private void ApplyLayout(string xmlLayout, string areaIds)
        {
            _LayoutPanel.DisableAllEvents = true;
            _LayoutPanel.RemoveAllControls();
            _LayoutPanel.XmlLayout = xmlLayout.Replace("'", "\"");
            string[] areasId = areaIds.Split(';', ',');
            foreach (string areaId in areasId)
                _LayoutPanel.AddControlToArea(new LayoutTestPanel(), areaId.Trim());
            _LayoutPanel.DisableAllEvents = false;
        }

        private void _LayoutPanel_XmlLayoutChanged(object sender, EventArgs e)
        {
            var xmlLayout = _LayoutPanel.XmlLayout;
            int len = xmlLayout.Length;
        }

        /// <summary>
        /// Po změně layoutu (pozice prvků)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LayoutPanelChanged(object sender, DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        /// <summary>
        /// Po změně pozice splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_SplitterPositionChanged(object sender, DxLayoutPanelSplitterChangedArgs e)
        {
            _ShowLayout(e);
        }
        private void _ShowLayout(DxLayoutPanelSplitterChangedArgs e)
        {
            LayoutTestPanel panel1 = e.Control1 as LayoutTestPanel;
            LayoutTestPanel panel2 = e.Control2 as LayoutTestPanel;
            var orientation = e.SplitterOrientation;
            var position = e.SplitterPosition;
        }
        /// <summary>
        /// Občas změním titulek některého panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Timer_Tick(object sender, EventArgs e)
        {
            this._Timer.Enabled = false;

            // Najdeme si náhodně nějaký panel:
            var layoutsItem = _LayoutPanel.DxLayoutItems;
            int count = layoutsItem.Length;
            if (count > 0)
            {
                Random rand = new Random();
                DxLayoutItemInfo layoutItem = layoutsItem[rand.Next(count)];
                if (layoutItem != null)
                {
                    if (layoutItem.UserControl is LayoutTestPanel testPanel)
                    {
                        string title = testPanel.TitleText;
                        if (rand.Next(10) > 3)
                        {
                            string appendix = RandomText.GetRandomSentence(2, 5, false);
                            title = title + " (" + appendix + ")";
                        }

                        _LayoutPanel.UpdateTitle(testPanel, title);
                    }

                    this._Timer.Interval = rand.Next(700, 3200);
                }
            }

            this._Timer.Enabled = true;
        }
        /// <summary>
        /// Chtěl bych zavřít formulář
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            this._LayoutPanel.RemoveAllControls();
            if (this._LayoutPanel.ControlCount > 0)
                e.Cancel = true;
        }
        /// <summary>
        /// Někdo zavřel poslední panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LayoutPanel_LastControlRemoved(object sender, EventArgs e)
        {
            this.Close();
        }

        public void AddControl(WF.Control control)
        {
            _LayoutPanel.AddControl(control);
        }
        /// <summary>
        /// Panel layoutu
        /// </summary>
        public DxLayoutPanel LayoutPanel { get { return _LayoutPanel; } }

        private DxLayoutPanel _LayoutPanel;
        private DxPanelControl _FunctionPanel;
        private DxSimpleButton _CopyLayoutButton;
        private DxSimpleButton _PasteLayoutButton;
        private DxSimpleButton _SetLayout1Button;
        private DxSimpleButton _SetLayout2Button;
        private DxSimpleButton _SetLayout3Button;
        private DxSimpleButton _SetLayout4Button;
        System.Windows.Forms.Timer _Timer;
    }
    /// <summary>
    /// Testovací panel reprezentující UserControl v <see cref="DxLayoutPanel"/>, náhrada DynamicPage
    /// </summary>
    public class LayoutTestPanel : System.Windows.Forms.Panel, ILayoutUserControl // DevExpress.XtraEditors.PanelControl
    {
        #region Public vrstva: konstruktor, property, eventy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public LayoutTestPanel()
        {
            this.Initialize();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"TestPanel Id: {Id}";
        }
        /// <summary>
        /// ID tohoto panelu
        /// </summary>
        protected int Id { get; private set; }
        /// <summary>
        /// ID panelu naposledy vytvořeného
        /// </summary>
        protected static int LastPanelId = 0;
        /// <summary>
        /// Text v titulku
        /// </summary>
        public string TitleText 
        {
            get { return _TitleText; }
            set { _TitleText = value; TitleTextChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleText;
        /// <summary>
        /// Došlo ke změně <see cref="TitleText"/>
        /// </summary>
        public event EventHandler TitleTextChanged;
        #endregion
        #region Inicializace, jednotlivé controly, jejich eventy
        /// <summary>
        /// Inicializace panelu
        /// </summary>
        protected void Initialize()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            Id = ++LastPanelId;
            this.TitleText = "Panel číslo " + Id.ToString();

            _AddRightButton = CreateDxButton("Otevřít další VPRAVO", LayoutPosition.Right);
            _AddBottomButton = CreateDxButton("Otevřít další DOLE", LayoutPosition.Bottom);
            _AddLeftButton = CreateDxButton("Otevřít další VLEVO", LayoutPosition.Left);
            _AddTopButton = CreateDxButton("Otevřít další NAHOŘE", LayoutPosition.Top);

            Random rand = new Random();
            int r = rand.Next(160, 240);
            int g = rand.Next(160, 240);
            int b = rand.Next(160, 240);
            this.BackColor = Color.FromArgb(r, g, b);

            MouseActivityInit();
        }
        /// <summary>
        /// Vytvoří a vrátí button
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.SimpleButton CreateDxButton(string text, LayoutPosition position)
        {
            var button = new DevExpress.XtraEditors.SimpleButton() { Text = text, Size = ButtonSize, Tag = position };
            button.Click += _AnyButton_Click;
            this.Controls.Add(button);
            return button;
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            this.DoLayout();
        }
        /// <summary>
        /// Rozmístí svoje controly do aktuálního prostoru
        /// </summary>
        protected void DoLayout()
        {
            var cs = this.ClientSize;
            int cw = cs.Width;
            int ch = cs.Height;

            var bs = ButtonSize;
            int bw = bs.Width;
            int bh = bs.Height;

            int mx = 12;
            int my = 9;
            int dw = cw - bw;
            int dh = ch - bh;

            _AddRightButton.Location = new Point(dw - mx, dh / 2);             // Vpravo, svisle uprostřed
            _AddBottomButton.Location = new Point(dw / 2, dh - my);            // Vodorovně uprostřed, dole
            _AddLeftButton.Location = new Point(mx, dh / 2);                   // Vlevo, svisle uprostřed
            _AddTopButton.Location = new Point(dw / 2, my);                    // Vodorovně uprostřed, nahoře
        }
        /// <summary>
        /// Obsahuje (najde) control, který řídí layout a vkládání nových prvků a odebírání existujících
        /// </summary>
        protected DxLayoutPanel LayoutPanel { get { return DxLayoutPanel.SearchParentLayoutPanel(this); } }
        /// <summary>
        /// Doporučená velikost buttonů
        /// </summary>
        protected Size ButtonSize { get { return new Size(120, 32); } }
        /// <summary>
        /// Po kliknutí na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyButton_Click(object sender, EventArgs e)
        {
            if (!(sender is DevExpress.XtraEditors.SimpleButton button)) return;
            if (!(button.Tag is LayoutPosition)) return;
            LayoutPosition position = (LayoutPosition)button.Tag;
            if (position == LayoutPosition.Left || position == LayoutPosition.Top || position == LayoutPosition.Bottom || position == LayoutPosition.Right)
            {
                int size = ((position == LayoutPosition.Left || position == LayoutPosition.Right) ? ButtonSize.Width * 4 : ButtonSize.Height * 6);
                LayoutTestPanel newPanel = new LayoutTestPanel();

                float ratio = 0.4f;
                LayoutPanel.AddControl(newPanel, this, position, currentSizeRatio: ratio);
                this.MouseActivityDetect();
                newPanel.MouseActivityDetect();
            }
        }
        DevExpress.XtraEditors.SimpleButton _AddRightButton;
        DevExpress.XtraEditors.SimpleButton _AddBottomButton;
        DevExpress.XtraEditors.SimpleButton _AddLeftButton;
        DevExpress.XtraEditors.SimpleButton _AddTopButton;
        #endregion
        #region Pohyb myši a viditelnost buttonů
        /// <summary>
        /// Inicializace eventů a proměnných pro myší aktivity
        /// </summary>
        private void MouseActivityInit()
        {
            RegisterMouseActivityEvents(this);
            foreach (Control control in this.Controls)
                RegisterMouseActivityEvents(control);
            this.ParentChanged += Control_MouseActivityChanged;
            this.MouseActivityDetect(true);
        }
        /// <summary>
        /// Zaregistruje pro daný control eventhandlery, které budou řídit viditelnost prvků this panelu (buttony podle myši)
        /// </summary>
        /// <param name="control"></param>
        private void RegisterMouseActivityEvents(Control control)
        {
            control.MouseEnter += Control_MouseActivityChanged;
            control.MouseLeave += Control_MouseActivityChanged;
            control.MouseMove += Control_MouseMove;
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseActivityChanged(object sender, EventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Provede se po myší aktivitě, zajistí Visible a Enabled pro buttony
        /// </summary>
        /// <param name="force"></param>
        private void MouseActivityDetect(bool force = false)
        {
            if (this.IsDisposed || this.Disposing) return;

            bool isMouseOnControl = false;
            if (this.Parent != null)
            {
                Point mousePoint = this.PointToClient(Control.MousePosition);
                isMouseOnControl = this.Bounds.Contains(mousePoint);
            }
            if (force || isMouseOnControl != _IsMouseOnControl)
            {
                _IsMouseOnControl = isMouseOnControl;
                RefreshButtonVisibility();
            }
        }
        private void RefreshButtonVisibility()
        {
            bool isVisible = this._IsMouseOnControl;
            _AddRightButton.Visible = isVisible;
            _AddBottomButton.Visible = isVisible;
            _AddLeftButton.Visible = isVisible;
            _AddTopButton.Visible = isVisible;
        }
        /// <summary>
        /// Obsahuje true, pokud je myš nad controlem (nad kterýmkoli prvkem), false když je myš mimo
        /// </summary>
        private bool _IsMouseOnControl;
        #endregion
        #region ILayoutUserControl implementace
        string ILayoutUserControl.Id { get { return this.Id.ToString(); } }
        string ILayoutUserControl.TitleText { get { return this.TitleText; } }
        ControlVisibility ILayoutUserControl.CloseButtonVisibility { get { return ControlVisibility.Allways; } }
        event EventHandler ILayoutUserControl.TitleTextChanged { add { this.TitleTextChanged += value; } remove { this.TitleTextChanged -= value; } }
        #endregion
    }
}
