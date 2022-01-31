// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Green, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Formulář, který nabízí prostor pro uživatelský control <see cref="ControlPanel"/>, a dole pak lištu s buttony a status bar (s volitelnou viditelností).
    /// Uživatel má svůj control vložit do <see cref="ControlPanel"/>, a nadefinovat buttony do pole <see cref="Buttons"/> (viz tam).
    /// </summary>
    public class DxControlForm : DxStdForm
    {
        #region Konstrukce - tvorba základních controlů
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxControlForm()
        {
            this.InitializeControls();
                
        }
        /// <summary>
        /// Při prvním zobrazení okna
        /// </summary>
        protected override void OnBeforeFirstShown()
        {
            base.OnBeforeFirstShown();
            MoveToVisibleScreen();
            DoLayout();
        }
        /// <summary>
        /// Vytvoření controlů
        /// </summary>
        private void InitializeControls()
        {
            _ControlPanel = DxComponent.CreateDxPanel(this, DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            _StatusBar = DxComponent.CreateDxStatusBar(this);

            _ButtonsVisible = null;
            _ButtonsDesignHeight = DxComponent.DefaultButtonHeight;
            _ButtonsPosition = ToolbarPosition.BottomSideLeft;
            _DesignMargins = DxComponent.DefaultInnerMargins;

            DialogResult = DialogResult.None;
            CloseOnClickButton = true;
        }
        private DxPanelControl _ControlPanel;
        private DxRibbonStatusBar _StatusBar;
        #endregion
        #region Layout
        /// <summary>
        /// Připraví pozici okna podle pozice myši nebo pro zobrazení na středu parent okna
        /// </summary>
        /// <param name="byMousePosition">Pokud je true, pak pozice okna bude začínat na aktuální pozici myši.</param>
        /// <param name="onlyOnMouseDown">Pouze pokud je myš stisknutá. Pozor, například akce menu se volají až po uvolnění myši!</param>
        /// <param name="formSize"></param>
        public void PrepareStartPosition(bool byMousePosition = false, bool onlyOnMouseDown = false, Size? formSize = null)
        {
            var mouseButtons = Control.MouseButtons;
            var mousePosition = Control.MousePosition;
            if (byMousePosition && (!onlyOnMouseDown || mouseButtons != MouseButtons.None))   // Podle pozice myši AND (bez ohledu na MouseDown anebo když zrovna je MouseDown):
            {
                this.StartPosition = FormStartPosition.Manual;
                Point point = mousePosition.Add(-30, -20);
                if (formSize.HasValue)
                {
                    Rectangle bounds = new Rectangle(point, formSize.Value);
                    this.Bounds = bounds.FitIntoMonitors();
                }
                else
                    this.Location = point;
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterParent;
                if (formSize.HasValue)
                    this.Size = formSize.Value;
            }
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayout();
        }
        /// <summary>
        /// Uspořádá prvky formuláře podle předepsané viditelnosti a rozměrů.
        /// </summary>
        private void DoLayout()
        {
            if (this.WasShown)
            {   // Nemá význam dělat layout dřív, než bude okno zobrazeno:
                DoLayoutStatusBar();
                DoLayoutButtons();
            }
        }
        /// <summary>
        /// Nastaví Layout pro Buttons - nastavuje Visible, Height, velikost a pozici buttonů.
        /// Pracuje s volnými panely pro obsah a pro buttony.
        /// Nastaví i souřadnice pro panel <see cref="_ControlPanel"/>.
        /// </summary>
        private void DoLayoutButtons()
        {
            // Patřičné dokování:
            if (_ControlPanel.Dock != DockStyle.None) _ControlPanel.Dock = DockStyle.None;

            // Prostor pro ControlPanel a pro buttony:
            Padding margins = DxComponent.ZoomToGui(this.DesignMargins, this.CurrentDpi);
            Rectangle innerBounds = this.GetInnerBounds(margins);
            if (_StatusBar.VisibleInternal && _StatusBar.Height > 0) innerBounds.Height = innerBounds.Height - _StatusBar.Height;

            // Pole buttonů:
            var buttons = GetLayoutButtons();

            // Rozmístit buttony a umístit Content:
            Rectangle contentBounds = DxComponent.CalculateControlItemsLayout(innerBounds, buttons, this.ButtonsPosition, margins);
            if (_ControlPanel.Bounds != contentBounds) _ControlPanel.Bounds = contentBounds;
        }
        /// <summary>
        /// Vrátí pole obsahující buttony připravené pro layout
        /// </summary>
        /// <returns></returns>
        private ControlItemLayoutInfo[] GetLayoutButtons()
        {
            List<ControlItemLayoutInfo> buttons = new List<ControlItemLayoutInfo>();

            // Vytvořím si pracovní pole buttonů, určíme max Width:
            int widthOneMax = 0;
            var panelSize = this.ClientSize;
            foreach (var button in _ButtonControls)
            {
                var preferresSize = button.GetPreferredSize(panelSize);
                if (widthOneMax < preferresSize.Width)
                    widthOneMax = preferresSize.Width;
                buttons.Add(new ControlItemLayoutInfo() { Control = button });
            }

            // Do pracovního pole vložím velikost buttonů = všechny stejně:
            int innerWidth = widthOneMax.Align(60, 300);
            int buttonHeight = DxComponent.ZoomToGui(this.ButtonsDesignHeight, this.CurrentDpi);
            int buttonWidth = innerWidth + (3 * buttonHeight / 2);
            Size buttonSize = new Size(buttonWidth, buttonHeight);
            buttons.ForEachExec(l => l.Size = buttonSize);

            return buttons.ToArray();
        }
        /// <summary>
        /// Nastaví Layout pro StatusBar - nastavuje Visible
        /// </summary>
        private void DoLayoutStatusBar()
        {
            // Visible:
            bool statusVisible = (StatusBarVisible ?? (StatusBar.ItemLinks.Count > 0));
            var statusBar = StatusBar;
            if (statusBar.VisibleInternal != statusVisible)
                statusBar.VisibleInternal = statusVisible;
        }
        #endregion
        #region Buttony privátně
        /// <summary>
        /// Do formuláře vytvoří buttony podle obsahu pole <see cref="_IButtons"/>.
        /// Buttony vytvoří, i když nemají být viditelné (<see cref="_ButtonsVisible"/>, to je úkolem metody <see cref="DoLayout"/>.
        /// </summary>
        private void CreateButtons()
        {
            RemoveButtons();

            var iButtons = _IButtons;
            if (iButtons is null) return;
            List<DxSimpleButton> buttonControls = new List<DxSimpleButton>();
            int x = 5;
            foreach (var iButton in iButtons)
            {
                var buttonControl = DxComponent.CreateDxSimpleButton(x, 0, 100, 20, this, iButton);
                buttonControl.Click += ButtonControl_Click;
                x += 105;
                buttonControls.Add(buttonControl);
            }
            _ButtonControls = buttonControls.ToArray();

            DoLayout();
        }
        /// <summary>
        /// Uživatel klikl na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonControl_Click(object sender, EventArgs e)
        {
            if (sender is Control control && control.Tag is IMenuItem buttonItem)
                RunButtonClick(buttonItem);
        }
        /// <summary>
        /// Odebere všechny buttony, které jsou v tuto chvíli přítomné
        /// </summary>
        private void RemoveButtons()
        {
            if (_ButtonControls is null) return;
            foreach (var button in _ButtonControls)
            {
                button.RemoveControlFromParent();
                button.Dispose();
            }
            _ButtonControls = null;
        }
        /// <summary>
        /// Uživatel zmáčkl button nebo jeho HotKey
        /// </summary>
        /// <param name="buttonItem"></param>
        private void RunButtonClick(IMenuItem buttonItem)
        {
            ClickedButton = buttonItem;
            if (buttonItem.Tag is DialogResult dialogResult)
                this.DialogResult = dialogResult;

            OnButtonClick(buttonItem);
            ButtonClick?.Invoke(this, new TEventArgs<IMenuItem>(buttonItem));

            if (CloseOnClickButton)
                this.Close();
        }
        /// <summary>
        /// Uživatel zmáčkl button nebo jeho HotKey
        /// </summary>
        /// <param name="buttonItem"></param>
        protected virtual void OnButtonClick(IMenuItem buttonItem) { }
        /// <summary>
        /// Uživatel zmáčkl button nebo jeho HotKey
        /// </summary>
        public event EventHandler<TEventArgs<IMenuItem>> ButtonClick;
        private DxSimpleButton[] _ButtonControls;
        #endregion
        #region Public rozhraní na prvky
        /// <summary>
        /// Panel, do kterého se má vložit uživatelský obsah.
        /// Pokud uživatelský obsah má nějakou minimální / maximální velikost, má být vepsána do <see cref="Control.MinimumSize"/> a do <see cref="Control.MaximumSize"/>.
        /// </summary>
        public DxPanelControl ControlPanel { get { return _ControlPanel; } }
        /// <summary>
        /// Minimální velikost požadovaná pro <see cref="ControlPanel"/>
        /// </summary>
        public Size? ControlPanelMinimumSize { get { return _ControlPanelMinimumSize; } set { _ControlPanelMinimumSize = value; this.DoLayout(); } }
        private Size? _ControlPanelMinimumSize;
        /// <summary>
        /// Maximální velikost požadovaná pro <see cref="ControlPanel"/>
        /// </summary>
        public Size? ControlPanelMaximumSize { get { return _ControlPanelMaximumSize; } set { _ControlPanelMaximumSize = value; this.DoLayout(); } }
        private Size? _ControlPanelMaximumSize;
        /// <summary>
        /// Button, který byl naposledy aktivován
        /// </summary>
        public IMenuItem ClickedButton { get; private set; }
        /// <summary>
        /// Zavřít okno po kliknutí na kterýkoli button? Default = true.
        /// Kliknutý button bude uchován v <see cref="ClickedButton"/>.
        /// Bude vyvolána událost <see cref="ButtonClick"/>.
        /// </summary>
        public bool CloseOnClickButton { get; private set; }
        /// <summary>
        /// Definice pro Buttony. Aplikace sem vkládá sadu buttonů, které chce v okně zobrazit. 
        /// Layout buttonů určují property <see cref="ButtonsPosition"/>, <see cref="ButtonsDesignHeight"/>, <see cref="DesignMargins"/> a <see cref="ButtonsVisible"/>.
        /// <para/>
        /// Aktivita buttonů:
        /// Aplikace může buď nastavit zdejší <see cref="CloseOnClickButton"/> na true, po kliknutí na button se zavře okno,
        /// a aplikace si pak vyhodnotí prvek uložený v <see cref="ClickedButton"/> = na něj uživatel klikl.
        /// Anebo si aplikace ošetří akci <see cref="IMenuItem.ClickAction"/> v konkrétních buttonech a sama reaguje,
        /// pak může sama zavřít okno bez závislosti na hodnotě <see cref="CloseOnClickButton"/>.
        /// <para/>
        /// Pokud button bude mít v <see cref="ITextItem.Tag"/> hodnotu typu <see cref="DialogResult"/>, 
        /// pak po kliknutí na button bude tato hodnota uložena v <see cref="Form.DialogResult"/>, jinak tam bude výchozí hodnota <see cref="DialogResult.None"/>.
        /// </summary>
        public IEnumerable<IMenuItem> Buttons { get { return _IButtons; } set { _IButtons = value?.ToArray(); this.CreateButtons(); } }
        private IMenuItem[] _IButtons;
        /// <summary>
        /// Počet definovaných buttonů
        /// </summary>
        public int ButtonsCount { get { return (_IButtons?.Length ?? 0); } }
        /// <summary>
        /// Viditelnost pole buttonů.
        /// Výchozí hodnota je null = řídí se podle přítomnosti nějakého buttonu.
        /// </summary>
        public bool? ButtonsVisible { get { return _ButtonsVisible; } set { _ButtonsVisible = value; this.DoLayout(); } }
        private bool? _ButtonsVisible;
        /// <summary>
        /// Výška jednotlivých buttonů, v design pixelech.
        /// Výchozí je 30. Platný rozsah 16 - 120.
        /// </summary>
        public int ButtonsDesignHeight { get { return _ButtonsDesignHeight; } set { _ButtonsDesignHeight = value.Align(16, 120); this.DoLayout(); } }
        private int _ButtonsDesignHeight;
        /// <summary>
        /// Vnitřní okraje mezo formulářem a obsahem, v design pixelech.
        /// </summary>
        public Padding DesignMargins { get { return _DesignMargins; } set { _DesignMargins = value; this.DoLayout(); } }
        private Padding _DesignMargins;
        /// <summary>
        /// Umístění buttonů. Výchozí hodnota je <see cref="ToolbarPosition.BottomSideLeft"/>.
        /// </summary>
        public ToolbarPosition ButtonsPosition { get { return _ButtonsPosition; } set { _ButtonsPosition = value; this.DoLayout(); } }
        private ToolbarPosition _ButtonsPosition;
        /// <summary>
        /// Status bar
        /// </summary>
        public DxRibbonStatusBar StatusBar { get { return _StatusBar; } }
        /// <summary>
        /// Viditelnost statusbaru.
        /// Výchozí hodnota je null = řídí se podle přítomnosti nějakého prvku.
        /// </summary>
        public bool? StatusBarVisible { get { return _StatusBarVisible; } set { _StatusBarVisible = value; this.DoLayout(); } }
        private bool? _StatusBarVisible;
        /// <summary>
        /// Refresh: uspořádá layout. Je vhodné volat po změně obsahu StatusBaru.
        /// </summary>
        public override void Refresh()
        {
            DoLayout();
            base.Refresh();
        }
        #endregion
    }
}
