using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DjSoft.Tools.ProgramLauncher.Settings;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    #region class DialogForm : Formulář s prostorem pro data a s tlačítky OK / Cancel
    /// <summary>
    /// <see cref="DialogForm"/> : Formulář s prostorem pro data a s tlačítky OK / Cancel nebo jakýmikoli dalšími.<br/>
    /// Volající kód vloží svůj datový panel do <see cref="DataPanelHost"/> a nastaví v něm Dock = Fill.
    /// Dále pak ošetří event <see cref="DialogButtonClick"/>.
    /// </summary>
    public class DialogForm : BaseForm, IContentBounds
    {
        #region Konstruktor a privátní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DialogForm()
        {
            this.InitializeForm();
        }
        /// <summary>
        /// Inicializuje form a jeho obsah
        /// </summary>
        protected virtual void InitializeForm()
        {
            this.__DataPanelHost = new DPanel();
            this.__DataPanelHost.BackColor = SystemColors.ControlDark;
            this.__DataPanelHost.TabIndex = 0;

            this.__DialogButtonPanel = new DialogButtonPanel() { Buttons = new DialogButtonType[] { DialogButtonType.Ok, DialogButtonType.Cancel } };
            this.__DialogButtonPanel.DialogButtonClick += _PanelDialogButtonClick;
            this.__DialogButtonPanel.BackColor = SystemColors.Window;
            this.__DialogButtonPanel.TabIndex = 1;

            this.__StatusStrip = new DStatusStrip();
            this.__StatusStrip.TabIndex = 2;

            this.Controls.Add(this.__DataPanelHost);
            this.Controls.Add(this.__DialogButtonPanel);
            this.Controls.Add(this.__StatusStrip);

            this.Size = new Size(650, 320);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSizeChanged += _ClientSizeChanged;
            this.FirstShown += _FirstShown;
            this.ShowInTaskbar = false;

            this.LayoutContent();
        }
        private DPanel __DataPanelHost;
        private Control __DataControl;
        private DialogButtonPanel __DialogButtonPanel;
        private DStatusStrip __StatusStrip;

        /// <summary>
        /// První zobrazení formuláře: pokusíme se aplikovat optimální velikost DataPanelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FirstShown(object sender, EventArgs e)
        {
            ApplyOptimalBounds();
        }
        /// <summary>
        /// Po kliknutí na tlačítko v panleu <see cref="DialogButtonPanel"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _PanelDialogButtonClick(object sender, DialogButtonEventArgs args)
        {
            _RunDialogButtonClick(args.DialogButtonType);
        }
        /// <summary>
        /// Po změně prostoru ve formuláři
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this.DialogButtonPanel.LayoutContent();
            this.LayoutContent();
        }
        /// <summary>
        /// Aplikuje optimální velikost formuláře podle velikosti <see cref="DataControl"/>
        /// </summary>
        private void ApplyOptimalBounds()
        {
            var formBounds = this.Bounds;
            Rectangle? newFormBounds = null;
            var dataControl = this.DataControl;
            if (dataControl != null && dataControl is IDataControl iDataControl)
            {
                var dataSize = iDataControl.OptimalSize ?? iDataControl.ContentSize;
                if (dataSize.HasValue)
                {
                    var currentSize = dataControl.Size;
                    int diffWidth = formBounds.Width - currentSize.Width;
                    int diffHeight = formBounds.Height  - currentSize.Height;
                    newFormBounds = new Rectangle(this.Location, new Size(dataSize.Value.Width + diffWidth, dataSize.Value.Height + diffHeight));
                }
            }
            if (newFormBounds.HasValue && newFormBounds.Value != formBounds)
                this.Bounds = newFormBounds.Value.AlignToNearestMonitor(false);
        }
        /// <summary>
        /// Umístí svůj datový panel do disponibilního prostoru
        /// </summary>
        private void LayoutContent()
        {
            var contentBounds = this.ContentBounds;
            var buttonBound = this.DialogButtonPanel.Bounds;
            var dataBounds = this.DataPanelHost.Bounds;
            var side = ((PanelContentAlignmentPart)this.DialogButtonPanel.PanelContentAlignment) & PanelContentAlignmentPart.MaskSides;
            switch (side)
            {
                case PanelContentAlignmentPart.TopSide:
                    dataBounds = new Rectangle(contentBounds.Left, buttonBound.Bottom, contentBounds.Width, contentBounds.Height - buttonBound.Bottom);
                    break;
                case PanelContentAlignmentPart.RightSide:
                    dataBounds = new Rectangle(contentBounds.Left, contentBounds.Top, buttonBound.Left, contentBounds.Height);
                    break;
                case PanelContentAlignmentPart.BottomSide:
                    dataBounds = new Rectangle(contentBounds.Left, contentBounds.Top, contentBounds.Width, buttonBound.Top);
                    break;
                case PanelContentAlignmentPart.LeftSide:
                    dataBounds = new Rectangle(buttonBound.Right, contentBounds.Top, contentBounds.Width - buttonBound.Right, contentBounds.Height);
                    break;
                default:
                    dataBounds = new Rectangle(contentBounds.Left, contentBounds.Top, contentBounds.Width, contentBounds.Height);
                    break;
            }
            this.DataPanelHost.Bounds = dataBounds;
        }
        /// <summary>
        /// Obsahuje prostor, v němž se může nacházet užitečný obsah okna.
        /// Jde o <see cref="Control.ClientSize"/>, z níž je nahoře odečten ToolStrip a dole StatusStrip.
        /// </summary>
        public Rectangle ContentBounds
        {
            get
            {
                var clientSize = this.ClientSize;
                int topHeight = 0;                  // ToolStrip
                int bottomHeight = (this.__StatusStrip != null && this.__StatusStrip.VisibleInternal ? this.__StatusStrip.Height : 0);
                return new Rectangle(0, topHeight, clientSize.Width, clientSize.Height - (topHeight + bottomHeight));
            }
        }
        /// <summary>
        /// Setuje uživatelský datový control do zdejšího okna
        /// </summary>
        /// <param name="dataControl"></param>
        private void _SetDataControl(Control dataControl)
        {
            var oldControl = __DataControl;
            if (oldControl != null) 
            {
                oldControl.Dock = DockStyle.None;

            }
            this.DataPanelHost.Controls.Clear();
            __DataControl = null;

            var newControl = dataControl;
            if (newControl != null) 
            {
                newControl.Dock = DockStyle.Fill;
                this.DataPanelHost.Controls.Add(newControl);
                __DataControl = newControl;

                if (newControl is IDataControl iDataControl)
                {
                    this.Buttons = iDataControl.Buttons;
                    this.AcceptButton = this.DialogButtonPanel[iDataControl.AcceptButtonType];
                    this.CancelButton = this.DialogButtonPanel[iDataControl.CancelButtonType];
                }
            }
        }
        /// <summary>
        /// Po kliknutí na button ...
        /// </summary>
        /// <param name="dialogResult"></param>
        private void _RunDialogButtonClick(DialogButtonType dialogResult)
        {
            DialogButtonEventArgs args = new DialogButtonEventArgs(dialogResult);

            if (__DataControl is IDataControl iDataControl)
                iDataControl.DialogButtonClicked(args);

            OnDialogButtonClick(args);
            DialogButtonClick?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná po kliknutí na tlačítko
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnDialogButtonClick(DialogButtonEventArgs args) { }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Panel, do něhož (tj. do jeho Controls) se umísťuje datový obsah, typicky jako plně dokovaný Panel.
        /// Jednodušší je setovat datový obsah do property <see cref="DataControl"/>.
        /// </summary>
        public DPanel DataPanelHost { get { return __DataPanelHost; } }
        /// <summary>
        /// Do této property lze vložit instanci Controlu, který reprezentuje vlastní data formuláře.
        /// Setováním je tento Control vložen do <see cref="DataPanelHost"/> a zadokován.
        /// </summary>
        public Control DataControl { get { return __DataControl; } set { _SetDataControl(value); } }
        /// <summary>
        /// Panel obsahující tlačítka
        /// </summary>
        public DialogButtonPanel DialogButtonPanel { get { return __DialogButtonPanel; } }
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        public DialogButtonType[] Buttons { get { return DialogButtonPanel.Buttons; } set { DialogButtonPanel.Buttons = value; } }

        /// <summary>
        /// Událost po kliknutí na tlačítko
        /// </summary>
        public event DialogButtonEventHandler DialogButtonClick;
        #endregion
    }
    #endregion
    #region class BaseForm : Bázový formulář
    /// <summary>
    /// Bázový formulář
    /// </summary>
    public class BaseForm : Form
    {
        #region Konstruktor a aktivace/deaktivace
        public BaseForm()
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': Constructor.Begin");
            this._FormInit();
            this._ToolTipInit();
            this._PositionInit();
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': Constructor.End");
        }
        /// <summary>
        /// Základní inicializace vlastností formuláře
        /// </summary>
        private void _FormInit()
        {
            this.Icon = ControlSupport.StandardFormIcon;
        }
        protected override void OnNotifyMessage(Message m)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   OnNotifyMessage.Begin Msg={Win32.GetMsgName(m.Msg)}");
            base.OnNotifyMessage(m);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   OnNotifyMessage.End   Msg={Win32.GetMsgName(m.Msg)}");
        }
        public override bool PreProcessMessage(ref Message msg)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   PreProcessMessage.Begin Msg={Win32.GetMsgName(msg.Msg)}");
            var result = base.PreProcessMessage(ref msg);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   PreProcessMessage.End   Msg={Win32.GetMsgName(msg.Msg)}");
            return result;
        }
        protected override void WndProc(ref Message m)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   WndProc.Begin Msg={Win32.GetMsgName(m.Msg)}");

            if (m.Msg == Win32.WM.ACTIVATEAPP || m.Msg == Win32.WM.ACTIVATE) _CheckFormStateLoad();

            base.WndProc(ref m);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}':   WndProc.End   Msg={Win32.GetMsgName(m.Msg)}");
        }
        public override string Text { get { return __Text; } set { __Text = value; base.Text = value; } } private string __Text;
        /// <summary>
        /// Název formuláře v Settings, pokud chce ukládat a načítat svoji pozici do Settings.
        /// Pokud bude dané jméno, ale data v Settings pro toto okno nebudou ještě k dispozici (=úplně první spuštění s novým Configem), pak okno zavolá metodu <see cref="OnFormStateDefault"/>, kde potomek nastaví výchozí velikost a umístění okna.
        /// </summary>
        public virtual string SettingsName { get; set; }
        /// <summary>
        /// Aktivuje this okno, volitelně zvětší ze stavu Minimized do stavu předchozího
        /// </summary>
        /// <param name="withWindowState"></param>
        public virtual void Activate(bool withWindowState)
        {
            ReActivateForm();
            this.Activate();
        }
        /// <summary>
        /// Běžná aktivace formu zajistí jeho reaktivaci
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': OnActivated.Begin");
            ReActivateForm();
            base.OnActivated(e);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': OnActivated.End");
        }
        /// <summary>
        /// Reaktivace formuláře
        /// </summary>
        protected virtual void ReActivateForm()
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = __PrevWindowStateSize ?? FormWindowState.Normal;
            if (!this.Visible) this.Visible = true;
        }
        /// <summary>
        /// Obsahuje true v VisualStudio Debugger režimu, false při běžném Run mode
        /// </summary>
        protected bool IsDebugMode { get { return App.IsDebugMode; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný tooltip pro daný control
        /// </summary>
        /// <param name="control"></param>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(Control control, string text, string title = null)
        {
            if (!String.IsNullOrEmpty(title))
                _ToolTip.ToolTipTitle = title;

            _ToolTip.Active = false;
            var toolTipType = App.Settings.CurrentToolTip;
            if (toolTipType == Settings.ToolTipType.None) return;

            if (String.IsNullOrEmpty(text))
            {
                _ToolTip.SetToolTip(control, null);
            }
            else
            {
                _ToolTip.SetToolTip(control, text);
                _ToolTip.InitialDelay = getInitialDelay();
                _ToolTip.ReshowDelay = getReshowDelay();
                _ToolTip.AutoPopDelay = getPopDelay();
                _ToolTip.ToolTipIcon = ToolTipIcon.Info;
                _ToolTip.Active = true;
            }

            int getInitialDelay()
            {
                switch (toolTipType)
                {
                    case Settings.ToolTipType.Fast: return 350;
                    case Settings.ToolTipType.Slow: return 1100;
                }
                return 700;
            }
            int getReshowDelay()
            {
                switch (toolTipType)
                {
                    case Settings.ToolTipType.Fast: return 250;
                    case Settings.ToolTipType.Slow: return 900;
                }
                return 450;
            }
            int getPopDelay()
            {
                string totalText = $"{_ToolTip.ToolTipTitle}:{text}";
                int charCount = totalText.Length;
                switch (toolTipType)
                {
                    case Settings.ToolTipType.Fast: return 1200 + (charCount * 30);
                    case Settings.ToolTipType.Slow: return 2500 + (charCount * 65);
                }
                return 1600 + (charCount * 40);
            }
        }
        /// <summary>
        /// Inicializace ToolTipu
        /// </summary>
        private void _ToolTipInit()
        {
            _ToolTip = new ToolTip();
            _ToolTip.Active = true;
            _ToolTip.InitialDelay = 700;
            _ToolTip.IsBalloon = false;
            _ToolTip.ShowAlways = false;
            _ToolTip.UseAnimation = true;
            _ToolTip.UseFading = true;
        }
        private ToolTip _ToolTip;
        /// <summary>
        /// Vrátí položky do menu, které definujá varianty ToolTipu
        /// </summary>
        public static IEnumerable<IMenuItem> ToolTipMenuItems
        {
            get
            {
                List<IMenuItem> menuItems = new List<IMenuItem>();
                var toolTipType = App.Settings.CurrentToolTip;
                menuItems.Add(createMenuItem(App.Messages.ToolTipTypeNoneText, ToolTipType.None));
                menuItems.Add(createMenuItem(App.Messages.ToolTipTypeDefaultText, ToolTipType.Default));
                menuItems.Add(createMenuItem(App.Messages.ToolTipTypeFastText, ToolTipType.Fast));
                menuItems.Add(createMenuItem(App.Messages.ToolTipTypeSlowText, ToolTipType.Slow));
                return menuItems;

                IMenuItem createMenuItem(string text, ToolTipType tipType)
                {
                    var menuItem = new DataMenuItem()
                    {
                        Text = text,
                        UserData = tipType,
                        FontStyle = (toolTipType == tipType ? FontStyle.Bold : FontStyle.Regular)
                    };
                    return menuItem;
                }
            }
        }
        #endregion
        #region Sledování, ukládání a restore pozice
        private void _PositionInit()
        {
            this.SizeChanged += _SizeChanged;
            this.LocationChanged += _LocationChanged;
            this.Shown += _Shown;
        }
        /// <summary>
        /// Event po změně pozice okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _LocationChanged(object sender, EventArgs e)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': LocationChanged.Begin");
            if (__FormStateChanging) return;
            this._FormStateSave(FormStateChangeType.Location);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': LocationChanged.End");
        }
        /// <summary>
        /// Event po změně velikosti okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SizeChanged(object sender, EventArgs e)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': SizeChanged.Begin");
            if (__FormStateChanging) return;
            this._FormStateSave(FormStateChangeType.Size);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': SizeChanged.End");
        }
        protected override void OnShown(EventArgs e)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': OnShown.Begin; IsAfterShow: {__IsAfterShow}");
            base.OnShown(e);
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': OnShown.End; IsAfterShow: {__IsAfterShow}");
        }
        /// <summary>
        /// Event po zobrazení okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Shown(object sender, EventArgs e)
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': Shown.Begin; IsAfterShow: {__IsAfterShow}");
            if (!__IsAfterShow)
            {
                __IsAfterShow = true;
                this._CheckFormStateLoad();
                this.OnFirstShown();
                this.FirstShown?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.OnNextShown();
                this.NextShown?.Invoke(this, EventArgs.Empty);
            }
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': Shown.End; IsAfterShow: {__IsAfterShow}");
        }
        /// <summary>
        /// Obsahuje true, pokud formulář již prošel fází Show
        /// </summary>
        public bool IsShown { get { return __IsAfterShow; } } private bool __IsAfterShow;
        /// <summary>
        /// Proběhne při prvním zobrazení formuláře.
        /// V této době již má formulář nastavenou (restorovanou) pozici a stav.
        /// </summary>
        protected virtual void OnFirstShown() { }
        /// <summary>
        /// Proběhne při prvním zobrazení formuláře.
        /// V této době již má formulář nastavenou (restorovanou) pozici a stav.
        /// </summary>
        public event EventHandler FirstShown;
        /// <summary>
        /// Proběhne při druhém a dalším zobrazení formuláře, nikoli při prvním.
        /// </summary>
        protected virtual void OnNextShown() { }
        /// <summary>
        /// Proběhne při druhém a dalším zobrazení formuláře, nikoli při prvním.
        /// </summary>
        public event EventHandler NextShown;
        /// <summary>
        /// POkud dosud nebyla načtena pozice okna, načte ji nyní
        /// </summary>
        private void _CheckFormStateLoad()
        {
            if (!__FormStateIsLoaded) _FormStateLoad();
        }
        /// <summary>
        /// Zajistí načtení pozice okna ze <see cref="Settings"/> a jejich aplikaci do this formuláře
        /// </summary>
        private void _FormStateLoad()
        {
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': FormStateLoad.Begin");
            if (!_HasSettingsName) return;
            if (__FormStateChanging) return;

            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': FormStateLoad.Start");

            string savedState = App.Settings.FormPositionLoad(this.SettingsName);        // Načtu stav okna z dat uložených v Settings, a aplikuji jej do this okna
            if (!String.IsNullOrEmpty(savedState))
                _FormStateCurrent = savedState;
            else
                OnFormStateDefault();
            __FormStateSaved = _FormStateCurrent;                                        // Získám stav z okna a zapamatuji si jeho otisk pro případné porovnání po změnách
            __FormStateIsLoaded = true;
            if (IsDebugMode) App.LogInfo($"{this.GetType().Name} '{this.Text}': FormStateLoad.End");
        }
        /// <summary>
        /// Tuto metodu volá bázová třída okna (<see cref="BaseForm"/>) v okamžiku, kdy by měl být restorován stav okna (<see cref="Form.WindowState"/> a jeho Bounds) z dat uložených v Settings, ale tam dosud nic není.
        /// Potomek by v této metodě měl umístit okno do výchozí pozice.
        /// </summary>
        protected virtual void OnFormStateDefault() { }
        /// <summary>
        /// Zajistí sesbírání dat o pozice okna z this formuláře a jejich zapsání do <see cref="Settings"/>
        /// </summary>
        /// <param name="source"></param>
        private void _FormStateSave(FormStateChangeType source)
        {
            if (!__IsAfterShow) return;                                                  // Před prvotním zobrazením okna nic neřeším (nastavují se vlastnosti v design modu)
            if (__FormStateChanging) return;                                             // Právě se setuje stav okna, nebudu nic řešit ani ukládat.
            if (this.WindowState == FormWindowState.Minimized) return;                   // Minimalizovaný stav není stav okna, to neukládám.

            // Zpracování přechodu  Maximized <=> Normal, nebo údržba NormalBounds:
            _FormStatePrepareNormalBounds(source);

            if (!_HasSettingsName) return;
            var formState = _FormStateCurrent;
            if (String.Equals(formState, __FormStateSaved)) return;

            App.Settings.FormPositionSave(this.SettingsName, formState);
            __FormStateSaved = formState;
        }
        /// <summary>
        /// Reaguje na změnu souřadnic a změnu WindowState
        /// </summary>
        /// <param name="source"></param>
        private void _FormStatePrepareNormalBounds(FormStateChangeType source)
        {
            // Stav okna nyní a předtím; ani jeden nemá být Minimized:
            var currState = this.WindowState;
            var prevState = (source == FormStateChangeType.Location ? __PrevWindowStateLocation : __PrevWindowStateSize) ?? currState;

            // Vždy si zapamatuji aktuální stav, abych příště poznal změnu:
            //  Uložím si ho dřív, než provedu změnu DesktopBounds, protože ta rekurzivně vyvolá změnu...
            // Pořadí událostí LocationChange a SizeChange při jedné společné změně je: Location, Size.
            // Proto při změně Size si ukládám stav okna (currState) i do Location, protože:
            //  a) pokud byla předtím i změna Location, pak tím nic nezkazím (nynější stav už tam v __PrevWindowStateLocation je);
            //  b) pokud se nynější změna Size obešla bez předchozí změny Location, tak bych měl aktuální stav (currState) dát i do __PrevWindowStateLocation,
            //      protože pro některou příští změnu Location to reálně bude její výchozí stav:
            __PrevWindowStateLocation = currState;
            if (source == FormStateChangeType.Size)
                __PrevWindowStateSize = currState;

            // Upravím hodnoty, pokud je již neupravuji já sám:
            if (!__FormBoundsChanging)
            {
                try
                {
                    __FormBoundsChanging = true;                // Potlačím rekurzivní změnu velikosti

                    if (prevState == FormWindowState.Normal && currState == FormWindowState.Normal)
                    {   // Stále trvající stav Normal = ukládám si pozici Normal okna:
                        this.NormalBounds = this.DesktopBounds;
                    }
                    else if (prevState == FormWindowState.Maximized && currState == FormWindowState.Normal)
                    {   // Přechod z Maximized do Normal = pokusím se obnovit souřadnice Normal:
                        this.DesktopBounds = this.NormalBounds;
                    }
                }
                finally
                {
                    __FormBoundsChanging = false;
                }
            }
        }
        /// <summary>
        /// Stav okna před aktuální změnou typu <see cref="FormStateChangeType.Location"/>; null = dosud nevíme
        /// </summary>
        private FormWindowState? __PrevWindowStateLocation;
        /// <summary>
        /// Stav okna před aktuální změnou typu <see cref="FormStateChangeType.Size"/>; null = dosud nevíme
        /// </summary>
        private FormWindowState? __PrevWindowStateSize;
        /// <summary>
        /// Aktuálně provádíme řízenou změnu souřadnic a nechci na ni reagovat
        /// </summary>
        private bool __FormBoundsChanging;
        /// <summary>
        /// Druh změny souřadnic okna
        /// </summary>
        private enum FormStateChangeType { None, Location, Size }
        /// <summary>
        /// Pozice formuláře včetně jeho stavu (WindowState) a rozměrů "NormalBounds"
        /// </summary>
        private string _FormStateCurrent { get { return _GetFormStateCurrent(); } set { _SetFormStateCurrent(value); } }
        /// <summary>
        /// Vrátí aktuální pozici this formuláře ve formě stringu. Data lze následně uložit a poté aplikovat pomocí <see cref="_SetFormStateCurrent(string)"/>.
        /// </summary>
        /// <returns></returns>
        private string _GetFormStateCurrent()
        {
            var windowsState = this.WindowState;
            var desktopBounds = this.DesktopBounds;
            var normalBounds = this.NormalBounds;
            return $"{Convertor.EnumToString(windowsState)}{_FormStateDelimiter}{Convertor.RectangleToString(desktopBounds)}{_FormStateDelimiter}{Convertor.RectangleToString(normalBounds)}";
        }
        /// <summary>
        /// Nastaví pozici this formuláře podle dodaných dat. Data vytváří metoda <see cref="_GetFormStateCurrent"/>.
        /// </summary>
        /// <param name="state"></param>
        private void _SetFormStateCurrent(string state)
        {
            FormWindowState windowsState = this.WindowState;
            Rectangle desktopBounds = this.DesktopBounds;
            Rectangle normalBounds = this.RestoreBounds;

            if (!String.IsNullOrEmpty(state))
            {
                var parts = state.Split(new string[] { _FormStateDelimiter }, StringSplitOptions.None);
                if (parts.Length == 3)
                {
                    var dWindowsState = Convertor.StringToEnum<FormWindowState>(parts[0], FormWindowState.Normal);
                    var dDesktopBounds = (Rectangle)Convertor.StringToRectangle(parts[1]);
                    var dNormalBounds = (Rectangle)Convertor.StringToRectangle(parts[2]);

                    if (dDesktopBounds.HasContent() && dNormalBounds.HasContent())
                    {
                        // Zarovnat do prostoru existujících monitorů:
                        dDesktopBounds = dDesktopBounds.AlignToNearestMonitor(false, dWindowsState == FormWindowState.Maximized);
                        dNormalBounds = dNormalBounds.AlignToNearestMonitor();

                        // Hodnoty po korekci si uložíme a na konci použijeme:
                        windowsState = dWindowsState;
                        normalBounds = dNormalBounds;

                        bool formStateChanging = __FormStateChanging;
                        try
                        {
                            __FormStateChanging = true;

                            // Různé pořadí setování podle stavu Maximized / Normal:
                            if (windowsState == FormWindowState.Maximized)
                            {   // Otevíráme okno Maximized:
                                this.DesktopBounds = dDesktopBounds;     // Nejprve umístím okno na správné místo na multimonitoru, aby se maximalizovalo na tom správném z více monitorů
                                this.WindowState = dWindowsState;        // Teď ho tam maximalizuji
                                this.DesktopBounds = dNormalBounds;      // Tohle zajistí, že změna Maximized => Normal se pokusí okno vrátit na Normal souřadnice
                                this.NormalBounds = dNormalBounds;       // Tady odtud se načtou Normal souřadnice...
                            }
                            else
                            {   // Otevíráme okno Normalized:
                                this.DesktopBounds = dDesktopBounds;
                                this.NormalBounds = dNormalBounds;
                                this.WindowState = dWindowsState;
                            }
                        }
                        finally
                        {
                            __FormStateChanging = formStateChanging;
                        }
                    }
                }
            }
            this.NormalBounds = normalBounds;
            this.__PrevWindowStateLocation = windowsState;
            this.__PrevWindowStateSize = windowsState;
        }
        /// <summary>
        /// Oddělovač částí dat o pozici okna
        /// </summary>
        private const string _FormStateDelimiter = " # ";
        /// <summary>
        /// Stav formuláře naposledy uložený v metodě <see cref="_FormStateSave"/>.
        /// Pokud je tato metoda vyvolána, když stav okna je shodný, neřeší ukládání dat do Settings.
        /// </summary>
        private string __FormStateSaved;
        /// <summary>
        /// Právě probíhá změna stavu formuláře vyvolaná kódem, a nesmí se ukládat do Settings (pokud je false, pak rozměry mění uživatel a do Settings se ukládat má)
        /// </summary>
        private bool __FormStateChanging;
        /// <summary>
        /// Byla již načtena pozice okna z registru?
        /// Obsahuje false na začátku, true po načtení, testuje metoda <see cref="_CheckFormStateLoad"/>
        /// </summary>
        private bool __FormStateIsLoaded;
        /// <summary>
        /// Obsahuje true, pokud je vyplněno jméno <see cref="SettingsName"/> = je vhodné ukládat a číst uložené souřadnice okna
        /// </summary>
        private bool _HasSettingsName { get { return !String.IsNullOrEmpty(this.SettingsName); } }
        /// <summary>
        /// Souřadnice okna v situaci, kdy je ve stavu Normal. Udržuje okno samo.
        /// </summary>
        public Rectangle NormalBounds { get { return __NormalBounds ?? this.RestoreBounds; } protected set { __NormalBounds = value; } } private Rectangle? __NormalBounds;
        #endregion
    }
    #endregion
}

namespace DjSoft.Tools.ProgramLauncher
{
    partial class Settings
    {
        #region Část Settings, která ukládá a načítá pozici a stav formulářů
        /// <summary>
        /// Uloží dodanou pozici formuláře do Settings pro aktuální / obecnou konfiguraci monitorů.<br/>
        /// Dodanou pozici <paramref name="positionData"/> uloží pod daným jménem <paramref name="settingsName"/>, 
        /// a dále pod jménem rozšířeným o kód aktuálně přítomných monitorů <see cref="Monitors.CurrentMonitorsKey"/>.
        /// <para/>
        /// Důvodem je to, že při pozdějším načítání se primárně načte pozice okna pro aktuálně platnou sestavu přítomných monitorů <see cref="Monitors.CurrentMonitorsKey"/>.
        /// Pak bude okno restorováno do posledně známé pozice na konkrétním monitoru.<br/>
        /// Pokud pozice daného okna <paramref name="settingsName"/> pro aktuální konfiguraci monitorů nebude nalezena,
        /// pak se vyhledá pozice posledně známá bez ohledu na konfiguraci monitoru. Viz <see cref="FormPositionLoad(string)"/>.
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="positionData"></param>
        public void FormPositionSave(string settingsName, string positionData)
        {
            if (String.IsNullOrEmpty(settingsName)) return;

            var positions = _FormPositionsGetDictionary();
            string key;

            key = _FormPositionGetKey(settingsName, false);
            positions.StoreValue(key, positionData);

            key = _FormPositionGetKey(settingsName, true);
            positions.StoreValue(key, positionData);

            this.SetChanged("FormPositions");
        }
        /// <summary>
        /// Zkusí najít pozici pro formulář daného jména a aktuální / nebo obecnou konfiguraci monitorů.
        /// Může vrátit null když nenajde uloženou pozici.<br/>
        /// Metoda neřeší obsah vracených dat a tedy ani správnost souřadnic, jde čistě o string který si řeší volající.<br/>
        /// Zdejší metoda jen reaguje na aktuální konfiguraci monitorů.
        /// </summary>
        /// <param name="settingsName"></param>
        /// <returns></returns>
        public string FormPositionLoad(string settingsName)
        {
            if (String.IsNullOrEmpty(settingsName)) return null;

            var positions = _FormPositionsGetDictionary();
            string key;

            key = _FormPositionGetKey(settingsName, true);
            if (positions.TryGetValue(key, out var positionData1)) return positionData1;

            key = _FormPositionGetKey(settingsName, false);
            if (positions.TryGetValue(key, out var positionData2)) return positionData2;

            return null;
        }
        /// <summary>
        /// Vrátí klíč pro pozici formuláře
        /// </summary>
        /// <param name="settingsName"></param>
        /// <param name="withMonitorsKey"></param>
        /// <returns></returns>
        private static string _FormPositionGetKey(string settingsName, bool withMonitorsKey)
        {
            return settingsName + (withMonitorsKey ? " at " + Monitors.CurrentMonitorsKey : "");
        }
        /// <summary>
        /// Vrátí dictionary obsahující data s pozicemi formulářů
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> _FormPositionsGetDictionary()
        {
            if (_FormPositions is null)
                _FormPositions = new Dictionary<string, string>();
            return _FormPositions;
        }
        /// <summary>
        /// Dictionary obsahující data s pozicemi formulářů
        /// </summary>
        [PropertyName("form_positions")]
        private Dictionary<string, string> _FormPositions { get; set; }
        /// <summary>
        /// Druh tooltipu
        /// </summary>
        [PropertyName("tooltips")]
        public ToolTipType CurrentToolTip { get { return __CurrentToolTip; } set { __CurrentToolTip = value; SetChanged(nameof(CurrentToolTip)); } } private ToolTipType __CurrentToolTip;
        /// <summary>
        /// Druh zobrazení ToolTipu
        /// </summary>
        public enum ToolTipType
        {
            Default = 0,
            None,
            Fast,
            Slow
        }
        #endregion
    }
}
