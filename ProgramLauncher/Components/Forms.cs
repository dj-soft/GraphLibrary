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

namespace DjSoft.Tools.ProgramLauncher.Components
{
    #region class DialogForm : Formulř s prostorem pro data a s tlačítky OK / Cancel

    public class DialogForm : BaseForm
    {
        public DialogForm()
        {
            this.InitializeForm();
        }
        protected virtual void InitializeForm()
        {
            this.OkCancelPanel = new OkCancelPanel();
            this.OkCancelPanel.BackColor = SystemColors.Window;

            this.Controls.Add(this.OkCancelPanel);
            this.AcceptButton = this.OkCancelPanel.AcceptButton;
            this.CancelButton = this.OkCancelPanel.CancelButton;

            this.Size = new Size(650, 320);
        }
        public OkCancelPanel OkCancelPanel { get; private set; }
    }
    #endregion
    #region class OkCancelPanel : Panel s tlačítky OK / Cancel
    /// <summary>
    /// <see cref="OkCancelPanel"/> : Panel s tlačítky OK / Cancel
    /// </summary>
    public class OkCancelPanel : Panel
    {
        public OkCancelPanel()
        {
            this.InitializePanel();
        }
        private Control __Parent;
        protected virtual void InitializePanel()
        {
            this.__ButtonsSize = ControlSupport.StandardButtonSize;
            this.__PanelAlignment = ContentAlignment.BottomCenter;
            this.__ButtonsAlignment = ContentAlignment.BottomLeft;
            this.__ButtonsSpacing = ControlSupport.StandardButtonSpacing;
            this.__ContentPadding = ControlSupport.StandardContentPadding;

            this.AcceptButton = ControlSupport.CreateButton(this, "OK", Properties.Resources.dialog_clean_22, __ButtonsSize);
            this.CancelButton = ControlSupport.CreateButton(this, "Storno", Properties.Resources.process_stop_6_22, __ButtonsSize);

            this.ParentChanged += _ParentChanged;
        }
        private void _ParentChanged(object sender, EventArgs args)
        {
            if (this.__Parent != null)
                this.__Parent.ClientSizeChanged -= __Parent_ClientSizeChanged;

            this.__Parent = this.Parent;

            LayoutContent();

            if (this.__Parent != null)
                this.__Parent.ClientSizeChanged += __Parent_ClientSizeChanged;
        }

        private void __Parent_ClientSizeChanged(object sender, EventArgs e)
        {
            this.LayoutContent();
        }
        /// <summary>
        /// Button OK
        /// </summary>
        public virtual Button AcceptButton { get; private set; }
        /// <summary>
        /// Button Cancel
        /// </summary>
        public virtual Button CancelButton { get; private set; }
        /// <summary>
        /// Vrátí pole controlů, které mají být zarovnány do this panelu v tomto pořadí.
        /// </summary>
        protected virtual Control[] AlignedControls { get { return new Control[] { AcceptButton, CancelButton }; } }
        /// <summary>
        /// Zarovnání panelu v rámci Parenta
        /// </summary>
        public ContentAlignment PanelAlignment { get { return __PanelAlignment; } set { __PanelAlignment = value; LayoutContent(); } } private ContentAlignment __PanelAlignment;
        /// <summary>
        /// Zarovnání buttonů v rámci this panelu
        /// </summary>
        public ContentAlignment ButtonsAlignment { get { return __ButtonsAlignment; } set { __ButtonsAlignment = value; LayoutContent(); } } private ContentAlignment __ButtonsAlignment;
        /// <summary>
        /// Velikost buttonů
        /// </summary>
        public Size ButtonsSize { get { return __ButtonsSize; } set { __ButtonsSize = value; LayoutContent(); } } private Size __ButtonsSize;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Size ButtonsSpacing { get { return __ButtonsSpacing; } set { __ButtonsSpacing = value; LayoutContent(); } } private Size __ButtonsSpacing;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Padding ContentPadding { get { return __ContentPadding; } set { __ContentPadding = value; LayoutContent(); } } private Padding __ContentPadding;

        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu
        /// </summary>
        protected virtual void LayoutContent()
        {
            var parent = this.__Parent;
            if (parent is null) return;

            var parentSize = parent.ClientSize;

            var controls = this.AlignedControls;
            if (controls is null || controls.Length == 0) return;



            this.Bounds = new Rectangle(6, 30, 450, 40);
            this.AcceptButton.Location = new Point(20, 3);
            this.CancelButton.Location = new Point(220, 3);
        }
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
            this._PositionInit();
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
    #region Servis pro potomky a ostatní
    public class ControlSupport
    {
        public static Button CreateButton(Control parent, string text, Image image, Size size)
        {
            Button button = new Button()
            {
                Text = text,
                Image = image,
                Size = size
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

            parent.Controls.Add(button);
            return button;
        }
        public static Size StandardButtonSize { get { return new Size(120, 38); } }
        public static Size StandardButtonSpacing { get { return new Size(12, 6); } }
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
