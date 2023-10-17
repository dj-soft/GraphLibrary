using DjSoft.Tools.ProgramLauncher.Components;
using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    #region class DialogForm : Formulář s prostorem pro data a s tlačítky OK / Cancel

    public class DialogForm : BaseForm
    {
        public DialogForm()
        {
            this.InitializeForm();
        }
        /// <summary>
        /// Inicializuje form a jeho obsah
        /// </summary>
        protected virtual void InitializeForm()
        {
            this.DataPanel = new DPanel();
            this.DataPanel.BackColor = SystemColors.ControlDark;
            this.Controls.Add(this.DataPanel);

            this.DialogButtonPanel = new DialogButtonPanel() { Buttons = new DialogResult[] { DialogResult.OK, DialogResult.Cancel } };
            this.DialogButtonPanel.BackColor = SystemColors.Window;
            this.Controls.Add(this.DialogButtonPanel);

            this.AcceptButton = this.DialogButtonPanel[DialogResult.OK];
            this.CancelButton = this.DialogButtonPanel[DialogResult.Cancel];

            this.Size = new Size(650, 320);
            this.ClientSizeChanged += _ClientSizeChanged;

            this.LayoutContent();
        }

        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this.DialogButtonPanel.LayoutContent();
            this.LayoutContent();
        }
        /// <summary>
        /// Umístí svůj datový panel do disponibilního prostoru
        /// </summary>
        private void LayoutContent()
        {
            var clientSize = this.ClientSize;
            var buttonBound = this.DialogButtonPanel.Bounds;
            var dataBounds = this.DataPanel.Bounds;
            var side = this.DialogButtonPanel.PanelContentAlignment.PanelSide();
            switch (side)
            {
                case PanelSide.TopSide:
                    dataBounds = new Rectangle(0, buttonBound.Bottom, clientSize.Width, clientSize.Height - buttonBound.Bottom);
                    break;
                case PanelSide.RightSide:
                    dataBounds = new Rectangle(0, 0, buttonBound.Left, clientSize.Height);
                    break;
                case PanelSide.BottomSide:
                    dataBounds = new Rectangle(0, 0, clientSize.Width, buttonBound.Top);
                    break;
                case PanelSide.LeftSide:
                    dataBounds = new Rectangle(buttonBound.Right, 0, clientSize.Width - buttonBound.Right, clientSize.Height);
                    break;
                default:
                    dataBounds = new Rectangle(0, 0, clientSize.Width, clientSize.Height);
                    break;
            }
            this.DataPanel.Bounds = dataBounds;
        }
        /// <summary>
        /// Panel, do něhož se umísťuje datový obsah, typicky jako plně dokovaný Panel
        /// </summary>
        public DPanel DataPanel { get; private set; }
        /// <summary>
        /// Panel obsahující tlačítka
        /// </summary>
        public DialogButtonPanel DialogButtonPanel { get; private set; }
    }
    #endregion
    #region class DialogButtonPanel : Panel s obecnými tlačítky typu DialogResult
    /// <summary>
    /// <see cref="DialogButtonPanel"/> : Panel s obecnými tlačítky typu DialogResult
    /// </summary>
    public class DialogButtonPanel : DPanel
    {
        #region Konstruktor a protected sféra
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DialogButtonPanel()
        {
            this.InitializePanel();
        }
        /// <summary>
        /// Evidovaný Parent
        /// </summary>
        private Control __Parent;
        /// <summary>
        /// Inicializuje panel a jeho obsah
        /// </summary>
        protected virtual void InitializePanel()
        {
            this.__ButtonsSize = ControlSupport.StandardButtonSize;
            this.__PanelContentAlignment = ControlSupport.StandardButtonPosition;
            this.__ButtonsSpacing = ControlSupport.StandardButtonSpacing;
            this.__ContentPadding = ControlSupport.StandardContentPadding;
            this.__ParentClientSize = Size.Empty;
            this.__PanelBounds = Rectangle.Empty;

            this.BorderStyle = BorderStyle.None;

            this.ParentChanged += _ParentChanged;
        }
        /// <summary>
        /// Po změně parenta zajistím správné umístění panelu a jeho controlů a navázání eventu 'ClientSizeChanged'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ParentChanged(object sender, EventArgs args)
        {
            if (this.__Parent != null)
                this.__Parent.ClientSizeChanged -= __Parent_ClientSizeChanged;

            this.__Parent = this.Parent;
            this.__ParentClientSize = Size.Empty;

            LayoutContent(true);

            if (this.__Parent != null)
                this.__Parent.ClientSizeChanged += __Parent_ClientSizeChanged;
        }
        /// <summary>
        /// Po změně prostoru uvnitř parenta přepočítám vnitřní layout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __Parent_ClientSizeChanged(object sender, EventArgs e)
        {
            this.LayoutContent();
        }
        #endregion
        #region Buttony
        /// <summary>
        /// VLoží buttony pro dané typy resultů
        /// </summary>
        /// <param name="buttonTypes"></param>
        private void _SetButtons(DialogResult[] buttonTypes)
        {
            var oldButtons = __DButtons;
            if (oldButtons != null)
            {
                foreach (var oldButton in oldButtons)
                {
                    this.Controls.Remove(oldButton);
                    oldButton.Dispose();
                }
                __DButtons = null;
            }
            __ButtonTypes = buttonTypes;

            List<DButton> newButtons = new List<DButton>();
            foreach (var buttonType in buttonTypes)
                newButtons.Add(_CreateDButton(buttonType));
            __DButtons = newButtons.ToArray();
        }
        /// <summary>
        /// Vytvoří a vrátí button pro daný typ
        /// </summary>
        /// <param name="buttonType"></param>
        /// <returns></returns>
        private DButton _CreateDButton(DialogResult buttonType)
        {
            switch (buttonType)
            {
                case DialogResult.Cancel: return DButton.Create(this, "Cancel", Properties.Resources.dialog_cancel_3_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.Abort: return DButton.Create(this, "Abort", Properties.Resources.process_stop_6_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.Retry: return DButton.Create(this, "Retry", Properties.Resources.view_refresh_4_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.Ignore: return DButton.Create(this, "Ignore", Properties.Resources.go_next_4_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.Yes: return DButton.Create(this, "Yes", Properties.Resources.dialog_ok_4_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.No: return DButton.Create(this, "No", Properties.Resources.dialog_no_3_22, __ButtonsSize, buttonType, _ButtonClick);
                case DialogResult.OK:
                default:
                    return DButton.Create(this, "OK", Properties.Resources.dialog_clean_22, __ButtonsSize, buttonType, _ButtonClick);
            }
        }
        /// <summary>
        /// Fyzické Buttony
        /// </summary>
        private DButton[] __DButtons;
        /// <summary>
        /// Po kliknutí na button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ButtonClick(object sender, EventArgs args)
        {
            if (sender is Control control && control.Tag is DialogResult buttonType)
                _RunClickResult(buttonType);
        }
        /// <summary>
        /// Po kliknutí na button ...
        /// </summary>
        /// <param name="dialogResult"></param>
        private void _RunClickResult(DialogResult dialogResult)
        {
            var form = this.FindForm();
            if (form != null) form.DialogResult = dialogResult;

            EventDialogResultArgs args = new EventDialogResultArgs(dialogResult);
            OnClickResult(args);
            ClickResult?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná po kliknutí na tlačítko
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnClickResult(EventDialogResultArgs args) { }
        #endregion
        #region Layout
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// Vlastní akci provede jen při <paramref name="force"/> = true nebo po změně prostoru v parentu.
        /// </summary>
        private void _LayoutContent(bool force)
        {
            var parent = this.__Parent;
            if (parent is null) return;

            var parentSize = parent.ClientSize;
            if (force || this.__ParentClientSize.IsEmpty || this.__ParentClientSize != parentSize)
                _RunLayoutContent();
        }
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// </summary>
        private void _RunLayoutContent()
        {
            var parentSize = this.__Parent.ClientSize;
            this.__ParentClientSize = parentSize;

            var buttons = this.__DButtons;
            int buttonsCount = buttons?.Length ?? 0;
            if (buttonsCount == 0)
            {
                this.Visible = false;
                return;
            }

            // Umístění panelu a buttonů určíme na základě zdejších hodnot:
            var alignment = this.PanelContentAlignment;
            var buttonSize = this.ButtonsSize;
            var spacing = this.ButtonsSpacing;
            var padding = this.ContentPadding;
            var side = alignment.PanelSide();
            bool isControlsVertical = (side == PanelSide.LeftSide || side == PanelSide.RightSide);

            // Velikost obsahu = velikost controlů + mezery mezi nimi:
            int contentWidth = (isControlsVertical ? buttonSize.Width : (buttonsCount * buttonSize.Width) + ((buttonsCount - 1) * spacing.Width));
            int contentHeight = (isControlsVertical ? (buttonsCount * buttonSize.Height) + ((buttonsCount - 1) * spacing.Height) : buttonSize.Height);

            // Umístit celý panel: podle velikosti obsahu a zarovnání panelu v parentu:
            var panelWidth = (isControlsVertical ? contentWidth + padding.Horizontal : parentSize.Width);
            var panelHeight = (isControlsVertical ? parentSize.Height : contentHeight + padding.Vertical);
            var panelLeft = (side == PanelSide.RightSide ? parentSize.Width - panelWidth : 0);
            var panelTop = (side == PanelSide.BottomSide ? parentSize.Height - panelHeight : 0);
            var panelBounds = new Rectangle(panelLeft, panelTop, panelWidth, panelHeight);
            this.Bounds = panelBounds;

            // Umístit controly:
            var position = alignment.ContentPosition();
            //  Hodnota 'ratio' vyjadřuje umístění obsahu: "na začátku" (0.0) / "uprostřed" (0.5) / "na konci" (1.0) v odpovídající ose:
            decimal ratio = (position == ContentPosition.OnBegin) ? 0m :
                            (position == ContentPosition.OnCenter) ? 0.5m :
                            (position == ContentPosition.OnEnd) ? 1.0m : 0.5m;

            int distance = (int)(Math.Round((ratio * (decimal)(isControlsVertical ? (parentSize.Height - padding.Vertical - contentHeight) : (parentSize.Width - padding.Horizontal - contentWidth))), 0));
            int buttonLeft = isControlsVertical ? padding.Left : padding.Top + distance;
            int buttonTop = isControlsVertical ? padding.Left + distance : padding.Top;
            foreach (var control in buttons)
            {
                control.Bounds = new Rectangle(buttonLeft, buttonTop, buttonSize.Width, buttonSize.Height);
                if (isControlsVertical)
                    buttonTop += buttonSize.Height + spacing.Height;
                else
                    buttonLeft += buttonSize.Width + spacing.Width;
            }

            // Viditelnost celého panelu:
            if (!this.Visible) this.Visible = true;

            // Změna Bounds?
            bool isBoundsChanged = (panelBounds != this.__PanelBounds);
            this.__PanelBounds = panelBounds;
            if (isBoundsChanged) this.RunPanelBoundsChanged();
        }
        /// <summary>
        /// Vyvolá metodu <see cref="OnPanelBoundsChanged(EventArgs)"/> a event <see cref="PanelBoundsChanged"/>
        /// </summary>
        protected void RunPanelBoundsChanged()
        {
            EventArgs args = EventArgs.Empty;
            OnPanelBoundsChanged(args);
            PanelBoundsChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná po změně souřadnic panelu v rámci Parenta
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPanelBoundsChanged(EventArgs args)
        { }
        /// <summary>
        /// Naposledy akceptovaná velikost Parenta
        /// </summary>
        private Size __ParentClientSize;
        /// <summary>
        /// Naposledy použité souřadnice panelu
        /// </summary>
        private Rectangle __PanelBounds;
        #endregion
        #region Public data a eventy
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        public DialogResult[] Buttons { get { return __ButtonTypes; } set { _SetButtons(value); } } private DialogResult[] __ButtonTypes;
        /// <summary>
        /// Vrátí fyzický button daného typu, anebo null.
        /// Typy přítomných buttonů lze setovat do <see cref="Buttons"/>.
        /// </summary>
        /// <param name="buttonType"></param>
        /// <returns></returns>
        public DButton this[DialogResult buttonType]
        {
            get
            {
                if (__DButtons is null) return null;
                return __DButtons.FirstOrDefault(b => b.Tag is DialogResult result && result == buttonType);
            }
        }
        /// <summary>
        /// Zarovnání panelu v rámci Parenta a umístění obsahu v něm
        /// </summary>
        public PanelContentAlignment PanelContentAlignment { get { return __PanelContentAlignment; } set { __PanelContentAlignment = value; LayoutContent(true); } } private PanelContentAlignment __PanelContentAlignment;
        /// <summary>
        /// Velikost buttonů
        /// </summary>
        public Size ButtonsSize { get { return __ButtonsSize; } set { __ButtonsSize = value; LayoutContent(true); } } private Size __ButtonsSize;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Size ButtonsSpacing { get { return __ButtonsSpacing; } set { __ButtonsSpacing = value; LayoutContent(true); } } private Size __ButtonsSpacing;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Padding ContentPadding { get { return __ContentPadding; } set { __ContentPadding = value; LayoutContent(true); } } private Padding __ContentPadding;
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// Vlastní akci provede jen při <paramref name="force"/> = true nebo po změně prostoru v parentu.
        /// </summary>
        public virtual void LayoutContent(bool force = false) { _LayoutContent(force); }
        /// <summary>
        /// Událost po kliknutí na tlačítko
        /// </summary>
        public event EventDialogResultHandler ClickResult;
        /// <summary>
        /// Event po změně souřadnic panelu v rámci Parenta
        /// </summary>
        public event EventHandler PanelBoundsChanged;
        #endregion
    }
    /// <summary>
    /// Umístění panelu a jeho obsahu
    /// </summary>
    public enum PanelContentAlignment
    {
        None,
        TopSideLeft,
        TopSideCenter,
        TopSideRight,
        RightSideTop,
        RightSideMiddle,
        RightSideBottom,
        BottomSideRight,
        BottomSideCenter,
        BottomSideLeft,
        LeftSideBottom,
        LeftSideMiddle,
        LeftSideTop
    }
    public enum PanelSide
    {
        None,
        TopSide,
        RightSide,
        BottomSide,
        LeftSide
    }
    public enum ContentPosition
    {
        None,
        OnBegin,
        OnCenter,
        OnEnd
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
            this._FormInit();
            this._PositionInit();
        }
        /// <summary>
        /// Základní inicializace vlastnéstí formuláře
        /// </summary>
        private void _FormInit()
        {
            this.Icon = ControlSupport.StandardFormIcon;
        }
        /// <summary>
        /// Název formuláře v Settings, pokud chce ukládat a načítat svoji pozici do Settings.
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
            ReActivateForm();
            base.OnActivated(e);
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
            if (__FormStateChanging) return;
            this._FormStateSave(FormStateChangeType.Location);
        }
        /// <summary>
        /// Event po změně velikosti okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SizeChanged(object sender, EventArgs e)
        {
            if (__FormStateChanging) return;
            this._FormStateSave(FormStateChangeType.Size);
        }
        /// <summary>
        /// Event po zobrazení okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Shown(object sender, EventArgs e)
        {
            if (!__IsAfterShow)
            {
                __IsAfterShow = true;
                this._FormStateLoad();
                this.OnFirstShown();
            }
            else
            {
                this.OnNextShown();
            }
        }
        private bool __IsAfterShow;
        /// <summary>
        /// Proběhne při prvním zobrazení formuláře.
        /// V této době již má formulář nastavenou (restorovanou) pozici a stav.
        /// </summary>
        protected virtual void OnFirstShown() { }
        /// <summary>
        /// Proběhne při druhém a dalším zobrazení formuláře, nikoli při prvním.
        /// </summary>
        protected virtual void OnNextShown() { }
        /// <summary>
        /// Zajistí načtení pozice okna ze <see cref="Settings"/> a jejich aplikaci do this formuláře
        /// </summary>
        private void _FormStateLoad()
        {
            if (!_HasSettingsName) return;
            if (__FormStateChanging) return;

            _FormStateCurrent = App.Settings.FormPositionLoad(this.SettingsName);        // Načtu stav okna z dat uložených v Settings, a aplikuji jej do this okna
            __FormStateSaved = _FormStateCurrent;                                        // Získám stav z okna a zapamatuji si jeho otisk pro případné porovnání po změnách
        }
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
    #region Bázové controly
    /// <summary>
    /// Button
    /// </summary>
    public class DButton : Button, IControlExtended
    {
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        public static DButton Create(Control parent, string text, Image image, Size size, object tag = null, EventHandler click = null)
        {
            DButton button = new DButton()
            {
                Text = text,
                Image = image,
                Size = size,
                Tag = tag
            };

            bool hasText = (!String.IsNullOrEmpty(text));
            bool hasImage = (image != null);
            if (hasText && hasImage)
            {
                button.ImageAlign = ContentAlignment.MiddleCenter;
                button.TextAlign = ContentAlignment.MiddleRight;
                button.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
            else if (hasText)
            {
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.TextImageRelation = TextImageRelation.Overlay;
            }
            else if (hasImage)
            {
                button.ImageAlign = ContentAlignment.MiddleCenter;
                button.TextImageRelation = TextImageRelation.Overlay;
            }

            if (click != null) button.Click += click;

            parent.Controls.Add(button);
            return button;
        }

    }
    /// <summary>
    /// Panel
    /// </summary>
    public class DPanel : Panel, IControlExtended
    {
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
    }

    public interface IControlExtended
    {
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        bool VisibleInternal { get; set; }
    }
    /// <summary>
    /// Událost, která přináší výsledek dialogu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void EventDialogResultHandler(object sender, EventDialogResultArgs args);
    /// <summary>
    /// Argument pro událost typu <see cref="EventDialogResultHandler"/>
    /// </summary>
    public class EventDialogResultArgs : EventArgs 
    {
        public EventDialogResultArgs(DialogResult dialogResult) 
        {
            this.DialogResult = dialogResult;
        }
        /// <summary>
        /// Výsledek dialogu
        /// </summary>
        public DialogResult DialogResult { get; }
    }
    
    #endregion
    #region Servis pro potomky a ostatní
    public class ControlSupport
    {
        public static DButton CreateDButton(Control parent, string text, Image image, Size size, EventHandler click = null) { return DButton.Create(parent, text, image, size, click); }
        /// <summary>
        /// Standardní ikona formulářů
        /// </summary>
        public static Icon StandardFormIcon { get { return Properties.Resources.klickety_2_64; } }
        /// <summary>
        /// Standardní umístění panelu a buttonů
        /// </summary>
        public static PanelContentAlignment StandardButtonPosition { get { return PanelContentAlignment.BottomSideLeft; } }
        /// <summary>
        /// Standardní velikost buttonu
        /// </summary>
        public static Size StandardButtonSize { get { return new Size(120, 32); } }
        /// <summary>
        /// Standardní odstupy buttonů
        /// </summary>
        public static Size StandardButtonSpacing { get { return new Size(12, 6); } }
        /// <summary>
        /// Standardní okraje uvnitř panelu
        /// </summary>
        public static Padding StandardContentPadding { get { return new Padding(6); } }
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

            this.SetChanged();
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
        #endregion
    }
}
