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
    public class LayoutForm : DxStdForm
    {
        public LayoutForm() : this(false)
        { }
        public LayoutForm(bool useDevExpress)
        {
            this.Text = $"Test řízení LayoutPanel :: {DxComponent.FrameworkName}";

            _LayoutPanel = new DxLayoutPanel()
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                SplitterContextMenuEnabled = true,
                DragDropEnabled = true,
                DockButtonVisibility = ControlVisibility.OnMouse,
                CloseButtonVisibility = ControlVisibility.Allways,     // ControlVisibility.OnNonPrimaryPanelAllways,
                EmptyPanelButtons = EmptyPanelVisibleButtons.Close,
                DockButtonLeftToolTip = "Přemístit tento panel doleva",
                DockButtonTopToolTip = "Přemístit tento panel nahoru",
                DockButtonBottomToolTip = "Přemístit tento panel dolů",
                DockButtonRightToolTip = "Přemístit tento panel doprava",
                CloseButtonToolTip = "Zavřít tento panel",
                UseSvgIcons = true
            };
            _LayoutPanel.UserControlAdd += _LayoutPanel_UserControlAdd;
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
            _SetLayout5Button = DxComponent.CreateDxSimpleButton(980, 6, 150, 37, _FunctionPanel, "Set Void Layout", _SetLayout5ButtonClick, toolTipText: "Vloží fixní layout obsahující i prázdné panely");

            var resourcesSvg = DxComponent.GetResourceNames(".svg", true, false);
            _ImageNames = Random.GetItems(36, resourcesSvg);

            Rectangle monitorBounds = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            Rectangle formBounds = new Rectangle(monitorBounds.X + monitorBounds.Width * 1 / 10, monitorBounds.Y + monitorBounds.Height * 1 / 10, monitorBounds.Width * 8 / 10, monitorBounds.Height * 8 / 10);

            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Bounds = formBounds;

            this._Timer = new Timer() { Interval = 1800 };
            this._Timer.Tick += _Timer_Tick;
            this._Timer.Enabled = false;
        }
        #region XmlLayout
        private void _LayoutPanel_XmlLayoutChanged(object sender, EventArgs e)
        {
            var xmlLayout = _LayoutPanel.XmlLayout;
            int len = xmlLayout.Length;
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
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxLayoutItemPanel' ControlID='1' />
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
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='325' SplitterRange='1376'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='2' />
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='205' SplitterRange='781'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='3' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='809' SplitterRange='1046'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='278' SplitterRange='571'>
      <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1/P1' Content='DxLayoutItemPanel' ControlID='1' />
      <id-value id-value.Target='Content2' AreaID='C/P2/P2/P1/P2' Content='DxLayoutItemPanel' ControlID='5' />
     </id-value>
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='4' />
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
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='396' SplitterRange='1376'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxLayoutItemPanel' ControlID='1' />
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='256' SplitterRange='781'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='4' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxLayoutItemPanel' ControlID='5' />
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
<id-persistent Version='2.00' Created='2021-04-17 18:32:14.095' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='312' SplitterRange='781'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='124' SplitterRange='312'>
    <id-value id-value.Target='Content1' AreaID='C/P1/P1' Content='DxLayoutItemPanel' ControlID='39' />
    <id-value id-value.Target='Content2' AreaID='C/P1/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='293' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P1/P2/P1' Content='DxLayoutItemPanel' ControlID='41' />
     <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='846' SplitterRange='1078'>
      <id-value id-value.Target='Content1' AreaID='C/P1/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='38' />
      <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='42' />
     </id-value>
    </id-value>
   </id-value>
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='278' SplitterRange='464'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='37' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='679' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='40' />
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='43' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1/P1; C/P1/P2/P1; C/P1/P2/P2/P1; C/P1/P2/P2/P2; C/P2/P1; C/P2/P2/P1; C/P2/P2/P2";
            ApplyLayout(xmlLayout, areaIds);
        }
        private void _SetLayout5ButtonClick(object sender, EventArgs e)
        {
            string xmlLayout = @"<?xml version='1.0' encoding='utf-16'?>
<id-persistent Version='2.00' Created='2021-04-17 18:32:14.095' Creator='David'>
 <id-data>
  <id-value id-value.Type='Noris.Clients.Win.Components.AsolDX.DxLayoutPanel+Area' AreaID='C' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='312' SplitterRange='781'>
   <id-value id-value.Target='Content1' AreaID='C/P1' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='124' SplitterRange='312'>
    <id-value id-value.Target='Content1' AreaID='C/P1/P1' Content='DxLayoutItemPanel' ControlID='39' />
    <id-value id-value.Target='Content2' AreaID='C/P1/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='293' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P1/P2/P1' Content='DxLayoutItemPanel' ControlID='41' />
     <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='846' SplitterRange='1078'>
      <id-value id-value.Target='Content1' AreaID='C/P1/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='38' />
      <id-value id-value.Target='Content2' AreaID='C/P1/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='42' />
     </id-value>
    </id-value>
   </id-value>
   <id-value id-value.Target='Content2' AreaID='C/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Horizontal' SplitterPosition='278' SplitterRange='464'>
    <id-value id-value.Target='Content1' AreaID='C/P2/P1' Content='DxLayoutItemPanel' ControlID='37' />
    <id-value id-value.Target='Content2' AreaID='C/P2/P2' Content='DxSplitContainer' FixedPanel='Panel1' SplitterOrientation='Vertical' SplitterPosition='679' SplitterRange='1376'>
     <id-value id-value.Target='Content1' AreaID='C/P2/P2/P1' Content='DxLayoutItemPanel' ControlID='40' />
     <id-value id-value.Target='Content2' AreaID='C/P2/P2/P2' Content='DxLayoutItemPanel' ControlID='43' />
    </id-value>
   </id-value>
  </id-value>
 </id-data>
</id-persistent>";
            string areaIds = "C/P1/P1; C/P1/P2/P2/P2";
            ApplyLayout(xmlLayout, areaIds);
        }
        private void ApplyLayout(string xmlLayout, string areaIds)
        {
            using (_LayoutPanel.ScopeSuspendParentLayout())
            {
                _LayoutPanel.DisableAllEvents = true;
                _LayoutPanel.RemoveAllControls();
                _LayoutPanel.XmlLayout = xmlLayout.Replace("'", "\"");
                string[] areasId = areaIds.Split(';', ',');
                foreach (string areaId in areasId)
                {
                    LayoutTestPanel testPanel = new LayoutTestPanel();
                    PrepareTestPanel(testPanel);
                    _LayoutPanel.AddControlToArea(testPanel, areaId.Trim());
                }
                _LayoutPanel.DisableAllEvents = false;
            }
        }
        #endregion


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
                DxLayoutItemInfo layoutItem = Random.GetItem(layoutsItem);
                if (layoutItem != null)
                {
                    if (layoutItem.UserControl is LayoutTestPanel testPanel)
                    {
                        if (Random.IsTrue(70))
                        {   // 70% prvků bude mít náhodný textový suffix:
                            string title = testPanel.TitleTextBasic;
                            string appendix = Random.GetSentence(2, 5, false);
                            title = title + " (" + appendix + ")";
                            testPanel.TitleText = title;              // Set => Event => DxLayout eventhandler
                        }

                        if (Random.IsTrue(20))
                        {   // 20% prvků bude mít náhodně změněnou ikonu:
                            testPanel.TitleImageName = _GetIconName();
                        }
                    }

                    this._Timer.Interval = Random.Rand.Next(700, 3200);
                }
            }

            this._Timer.Enabled = true;
        }

        private void _LayoutPanel_UserControlAdd(object sender, TEventArgs<Control> e)
        {
            if (e.Item is LayoutTestPanel testPanel)
                PrepareTestPanel(testPanel);
        }
        private void PrepareTestPanel(LayoutTestPanel testPanel)
        {
            testPanel.TitleImageName = _GetIconName();

            if (Random.IsTrue(40))
            {   // 40% prvků bude mít podtržení:
                Color lineColor = Color.FromArgb(255, 255, 32);
                if (Random.IsTrue(20))
                    // 20% z nich bude mít náhodnou barvu podtržení:
                    lineColor = Random.GetColor(48, 160);
                testPanel.LineColor = Color.FromArgb(160, lineColor);
                testPanel.LineColorEnd = Color.FromArgb(12, lineColor);
                testPanel.LineWidth = 4;
            }

            if (Random.IsTrue(40))
            {   // 40% prvků bude mít BackColor:
                Color backColor = Random.GetColor(64, 256);
                testPanel.TitleBackColor = Color.FromArgb(160, backColor);
                if (Random.IsTrue(40))
                    // 40% z nich bude mít fadeout:
                    testPanel.TitleBackColorEnd = Color.FromArgb(0, backColor);
            }
        }

        private string _GetIconName()
        {
            return Random.GetItem(_ImageNames);
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
            if (control is LayoutTestPanel testPanel)
                PrepareTestPanel(testPanel);
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
        private DxSimpleButton _SetLayout5Button;
        private string[] _ImageNames;
        private Timer _Timer;
    }
    /// <summary>
    /// Testovací panel reprezentující UserControl v <see cref="DxLayoutPanel"/>, náhrada DynamicPage
    /// </summary>
    public class LayoutTestPanel : DxPanelControl, ILayoutUserControl // DevExpress.XtraEditors.PanelControl / System.Windows.Forms.Panel
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
        /// Základní text titulku, nemění se, nepropisuje se do <see cref="TitleText"/>
        /// </summary>
        public string TitleTextBasic { get; set; }
        /// <summary>
        /// Text v titulku
        /// </summary>
        public string TitleText 
        {
            get { return _TitleText; }
            set { _TitleText = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleText;
        /// <summary>
        /// Text v titulku náhradní
        /// </summary>
        public string TitleSubstitute
        {
            get { return _TitleSubstitute; }
            set { _TitleSubstitute = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleSubstitute;
        /// <summary>
        /// Ikona u titulku
        /// </summary>
        public string TitleImageName
        {
            get { return _TitleImageName; }
            set { _TitleImageName = value; TitleChanged?.Invoke(this, EventArgs.Empty); }
        }
        private string _TitleImageName;
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColor { get; set; }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColorEnd { get; set; }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        public int? LineWidth { get; set; }

        public int TitleBackMargins { get; set; }
        public Color? TitleBackColor { get; set; }
        public Color? TitleBackColorEnd { get; set; }
        public Color? TitleTextColor { get; set; }

        /// <summary>
        /// Došlo ke změně <see cref="TitleText"/>
        /// </summary>
        public event EventHandler TitleChanged;
        #endregion
        #region Inicializace, jednotlivé controly, jejich eventy
        /// <summary>
        /// Inicializace panelu
        /// </summary>
        protected void Initialize()
        {
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            Id = ++LastPanelId;
            this.TitleTextBasic = "Panel číslo " + Id.ToString();
            this.TitleText = this.TitleTextBasic;

            _AddRightButton = CreateDxButton("Otevřít další VPRAVO", LayoutPosition.Right);
            _AddBottomButton = CreateDxButton("Otevřít další DOLE", LayoutPosition.Bottom);
            _AddLeftButton = CreateDxButton("Otevřít další VLEVO", LayoutPosition.Left);
            _AddTopButton = CreateDxButton("Otevřít další NAHOŘE", LayoutPosition.Top);

            this.BackColorUser = Random.GetColor(64, 256, 64);

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
                Point absolutePoint = Control.MousePosition;
                Point relativePoint = this.PointToClient(absolutePoint);
                isMouseOnControl = this.ClientRectangle.Contains(relativePoint);
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
        bool ILayoutUserControl.TitleVisible { get { return true; } }
        string ILayoutUserControl.TitleText { get { return this.TitleText; } }
        string ILayoutUserControl.TitleSubstitute { get { return this.TitleSubstitute; } }
        string ILayoutUserControl.TitleImageName { get { return this.TitleImageName; } }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        int? ILayoutUserControl.LineWidth { get { return this.LineWidth; } }
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? ILayoutUserControl.LineColor { get { return this.LineColor; } }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? ILayoutUserControl.LineColorEnd { get { return this.LineColorEnd; } }
        /// <summary>
        /// Okraje mezi TitlePanel a barvou pozadí, default = 0
        /// </summary>
        int? ILayoutUserControl.TitleBackMargins { get { return this.TitleBackMargins; } }
        /// <summary>
        /// Barva pozadí titulku.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? ILayoutUserControl.TitleBackColor { get { return this.TitleBackColor; } }
        /// <summary>
        /// Barva pozadí titulku, konec gradientu vpravo, null = SolidColor.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? ILayoutUserControl.TitleBackColorEnd { get { return this.TitleBackColorEnd; } }
        Color? ILayoutUserControl.TitleTextColor { get { return this.TitleTextColor; } }
        event EventHandler ILayoutUserControl.TitleChanged { add { this.TitleChanged += value; } remove { this.TitleChanged -= value; } }
        #endregion
    }
}
