using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.App.iCollect.Data
{
    /// <summary>
    /// Stav aplikace. Do této třídy se zapisují informace o stavu aplikace:
    /// o aktualizaci, o oknu Login, o oknu Desktop i FormBuilder: o procesu otevírání, života, zavírání.
    /// <para/>
    /// Formuláře: Každý z formulářů Login, Desktop a FormBuilder se ve svém konstruktoru zaeviduje prostým přiřazením:
    /// <see cref="ApplicationState.DesktopForm"/> = this;          // instance WDesktop se zaeviduje do registru stavu
    /// a o víc se starat nemusí, instance <see cref="ApplicationState"/> se zaháčkuje na eventy daného okna a hlídá si jeho stav sama.
    /// </summary>
    /// <remarks>DAJ 0070184 Chyba při zavírání klienta-Invoke do GUI</remarks>
    public sealed class ApplicationState
    {
        #region Public stav aplikace a oken
        /// <summary>
        /// Stav aplikace. Výchozí je <see cref="ApplicationStateType.Initializing"/>.
        /// Tuto hodnotu setuje aplikace ve význačných místech.
        /// </summary>
        public static ApplicationStateType State { get { return Instance._State; } set { Instance._State = value; } }
        /// <summary>
        /// Stav aplikace, instanční. Setování detekuje změnu a volá event <see cref="StateChanged"/>.
        /// </summary>
        private ApplicationStateType _State
        {
            get { return __State; }
            set
            {
                if (value == __State) return;
                __State = value;
                _StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private ApplicationStateType __State;
        /// <summary>
        /// Událost je volána po změně stavu <see cref="State"/>.
        /// </summary>
        public static event EventHandler StateChanged { add { Instance._StateChanged += value; } remove { Instance._StateChanged -= value; } }
        private event EventHandler _StateChanged;

        /// <summary>
        /// Obsahuje true v situaci, kdy aplikace končí anebo skončila.
        /// Tedy když <see cref="State"/> = <see cref="ApplicationStateType.DesktopClosing"/> nebo <see cref="ApplicationStateType.DesktopClosed"/>
        /// nebo <see cref="ApplicationStateType.ApplicationEnded"/>
        /// </summary>
        public static bool IsApplicationEnding { get { return Instance._IsApplicationEnding; } }
        /// <summary>
        /// Obsahuje true v situaci, kdy aplikace končí anebo skončila.
        /// </summary>
        private bool _IsApplicationEnding { get { var s = _State; return s == ApplicationStateType.DesktopClosing || s == ApplicationStateType.DesktopClosed || s == ApplicationStateType.ApplicationEnded; } }

        /// <summary>
        /// Obsahuje true v situaci, kdy aplikace již skončila.
        /// Tedy když <see cref="State"/> = <see cref="ApplicationStateType.DesktopClosed"/>
        /// nebo <see cref="ApplicationStateType.ApplicationEnded"/>.
        /// (Tedy obsahuje false v době ukončování aplikace, ve stavu <see cref="ApplicationStateType.DesktopClosing"/> !)
        /// </summary>
        public static bool IsApplicationEnd { get { return Instance._IsApplicationEnd; } }
        /// <summary>
        /// Obsahuje true v situaci, kdy aplikace končí anebo skončila.
        /// </summary>
        private bool _IsApplicationEnd { get { var s = _State; return s == ApplicationStateType.DesktopClosed || s == ApplicationStateType.ApplicationEnded; } }

        /// <summary>
        /// Okno Login. Může být null.
        /// </summary>
        public static Form LoginForm { get { return Instance._FormDict[FormType.Login].Form; } set { Instance.SetForm(FormType.Login, value); } }
        /// <summary>
        /// Stav okna Login.
        /// Po setování hodnoty <see cref="ApplicationFormStateType.Disposed"/> bude hodnota <see cref="LoginForm"/> nulována.
        /// </summary>
        public static ApplicationFormStateType LoginFormState { get { return Instance._FormDict[FormType.Login].State; } set { Instance.SetFormState(FormType.Login, value); } }

        /// <summary>
        /// Okno Desktop. Může být null.
        /// </summary>
        public static Form DesktopForm { get { return Instance._FormDict[FormType.Desktop].Form; } set { Instance.SetForm(FormType.Desktop, value); } }
        /// <summary>
        /// Stav okna Desktop.
        /// Po setování hodnoty <see cref="ApplicationFormStateType.Disposed"/> bude hodnota <see cref="DesktopForm"/> nulována.
        /// </summary>
        public static ApplicationFormStateType DesktopFormState { get { return Instance._FormDict[FormType.Desktop].State; } set { Instance.SetFormState(FormType.Desktop, value); } }

        /// <summary>
        /// Okno FormBuilder. Může být null.
        /// </summary>
        public static Form FormBuilderForm { get { return Instance._FormDict[FormType.FormBuilder].Form; } set { Instance.SetForm(FormType.FormBuilder, value); } }
        /// <summary>
        /// Stav okna FormBuilder.
        /// Po setování hodnoty <see cref="ApplicationFormStateType.Disposed"/> bude hodnota <see cref="DesktopForm"/> nulována.
        /// </summary>
        public static ApplicationFormStateType FormBuilderFormState { get { return Instance._FormDict[FormType.FormBuilder].State; } set { Instance.SetFormState(FormType.FormBuilder, value); } }
        #endregion
        #region Události z formulářů aktualizují stavy těchto formulářů
        /// <summary>
        /// Uložíme daný typ formuláře do interní evidence (registrují se eventhandlery a ukládá se WeakReference)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="form"></param>
        private void SetForm(FormType type, Form form)
        {
            var formInfo = _FormDict[type];
            ReleaseFormEvents(formInfo.Form);
            AttachFormEvents(form);
            SetInitialFormState(form, formInfo);
        }
        /// <summary>
        /// Zapojí eventhandlery
        /// </summary>
        /// <param name="form"></param>
        private void AttachFormEvents(Form form)
        {
            if (form is null) return;
            form.Shown += Form_Shown;
            form.Activated += Form_Activated;
            form.VisibleChanged += Form_VisibleChanged;
            form.FormClosing += Form_FormClosing;
            form.FormClosed += Form_FormClosed;
            form.Disposed += Form_Disposed;
        }
        /// <summary>
        /// Odpojí eventhandlery
        /// </summary>
        /// <param name="form"></param>
        private void ReleaseFormEvents(Form form)
        {
            if (form is null) return;
            form.Shown -= Form_Shown;
            form.Activated -= Form_Activated;
            form.VisibleChanged -= Form_VisibleChanged;
            form.FormClosing -= Form_FormClosing;
            form.FormClosed -= Form_FormClosed;
            form.Disposed -= Form_Disposed;
        }
        /// <summary>
        /// Eventhandler události Shown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Shown(object sender, EventArgs e)
        {
            SetFormState(sender as Form, ApplicationFormStateType.Shown);
        }
        /// <summary>
        /// Eventhandler události Activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Activated(object sender, EventArgs e)
        {
            SetFormState(sender as Form, ApplicationFormStateType.Visible);
        }
        /// <summary>
        /// Eventhandler události VisibleChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_VisibleChanged(object sender, EventArgs e)
        {
            if (sender is Form form)
                SetFormState(form, form.Visible ? ApplicationFormStateType.Visible : ApplicationFormStateType.Hidden);
        }
        /// <summary>
        /// Eventhandler události FormClosing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetFormState(sender as Form, ApplicationFormStateType.Closing);
        }
        /// <summary>
        /// Eventhandler události FormClosed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            SetFormState(sender as Form, ApplicationFormStateType.Closed);
        }
        /// <summary>
        /// Eventhandler události Disposed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Disposed(object sender, EventArgs e)
        {
            SetFormState(sender as Form, ApplicationFormStateType.Disposed);
        }
        /// <summary>
        /// Nastaví stav odpovídající aktuálnímu formuláři
        /// </summary>
        /// <param name="form"></param>
        /// <param name="formInfo"></param>
        private void SetInitialFormState(Form form, FormInfo formInfo)
        {
            formInfo.Form = form;
            if (form is null)
            {
                if (formInfo.State != ApplicationFormStateType.None)
                    formInfo.State = ApplicationFormStateType.Disposed;
            }
            else if (!form.IsHandleCreated)
                formInfo.State = ApplicationFormStateType.Initializing;
            else if (form.Disposing || form.IsDisposed)
                formInfo.State = ApplicationFormStateType.Disposed;
            else if (!form.Visible)
                formInfo.State = ApplicationFormStateType.Hidden;
            else
                formInfo.State = ApplicationFormStateType.Visible;

            SetAppStateAuto(formInfo);
        }
        /// <summary>
        /// Nastaví daný stav do odpovídajícího záznamu dle formuláře
        /// </summary>
        /// <param name="form"></param>
        /// <param name="state"></param>
        private void SetFormState(Form form, ApplicationFormStateType state)
        {
            if (form is null) return;
            FormInfo formInfo = _FormDict.Values.FirstOrDefault(fi => Object.ReferenceEquals(form, fi.Form));
            SetFormState(formInfo, state);
        }
        /// <summary>
        /// Nastaví daný stav do odpovídajícího záznamu dle formuláře.
        /// Při vložení stavu <see cref="ApplicationFormStateType.Disposed"/> bude okno releasováno z paměti.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="state"></param>
        private void SetFormState(FormType type, ApplicationFormStateType state)
        {
            SetFormState(_FormDict[type], state);
        }
        /// <summary>
        /// Nastaví daný stav do odpovídajícího záznamu dle formuláře.
        /// Při vložení stavu <see cref="ApplicationFormStateType.Disposed"/> bude okno releasováno z paměti.
        /// </summary>
        /// <param name="formInfo"></param>
        /// <param name="state"></param>
        private void SetFormState(FormInfo formInfo, ApplicationFormStateType state)
        {
            if (formInfo is null) return;

            if (state == ApplicationFormStateType.Disposed)
                ReleaseFormEvents(formInfo.Form);
            formInfo.State = state;

            // Automatika pro Stav aplikace:
            SetAppStateAuto(formInfo);
        }
        /// <summary>
        /// Automatika pro nastavení stavu aplikace podle stavu konkrétního formuláře
        /// </summary>
        /// <param name="formInfo"></param>
        private void SetAppStateAuto(FormInfo formInfo)
        {
            var appState = _State;
            var appStateN = (int)appState;
            var frmState = formInfo.State;
            var frmType = formInfo.Type;

            switch (frmType)
            {
                case FormType.Login:
                    switch (frmState)
                    {
                        case ApplicationFormStateType.Initializing:
                            _State = ApplicationStateType.LoginPreparing;
                            break;
                        case ApplicationFormStateType.Shown:
                        case ApplicationFormStateType.Visible:
                            _State = ApplicationStateType.LoginVisible;
                            break;
                        case ApplicationFormStateType.Closing:
                            if (appStateN < ((int)ApplicationStateType.LoginClosing))
                                _State = ApplicationStateType.LoginClosing;
                            break;
                        case ApplicationFormStateType.Closed:
                            if (appStateN < ((int)ApplicationStateType.LoginClosed))
                                _State = ApplicationStateType.LoginClosed;
                            break;
                    }
                    break;

                case FormType.Desktop:
                    switch (frmState)
                    {
                        case ApplicationFormStateType.Initializing:
                            _State = ApplicationStateType.DesktopPreparing;
                            break;
                        case ApplicationFormStateType.Shown:
                        case ApplicationFormStateType.Visible:
                            if (appStateN < ((int)ApplicationStateType.DesktopRunning))
                                _State = ApplicationStateType.DesktopRunning;
                            break;
                        case ApplicationFormStateType.Closing:
                            _State = ApplicationStateType.DesktopClosing;
                            break;
                        case ApplicationFormStateType.Closed:
                            _State = ApplicationStateType.DesktopClosed;
                            break;
                        case ApplicationFormStateType.Disposed:
                            _State = ApplicationStateType.DesktopClosed;
                            break;
                    }
                    break;
            }
        }
        #endregion
        #region Invokace GUI threadu
        /// <summary>
        /// Provede danou akci v GUI threadu, pokud takový existuje. Jinak provede akci v current threadu.
        /// Dodání Controlu je optional.
        /// Pokud stav aplikace končí, neprovede akci vůbec.
        /// </summary>
        /// <param name="action">Akce k provedení</param>
        /// <param name="useSyncInvoke">Způsob invokování akce: true = synchronní (Invoke) / false = asynchronní (BeginInvoke)</param>
        /// <param name="control">Optional control k invokování, bude otestován</param>
        public static void RunInGuiThread(Action action, bool useSyncInvoke = false, Control control = null) { Instance._RunInGuiThread(action, useSyncInvoke, control); }
        /// <summary>
        /// Provede danou akci v GUI threadu, pokud takový existuje. Jinak provede akci v current threadu.
        /// Dodání Controlu je optional.
        /// Pokud stav aplikace končí, neprovede akci vůbec.
        /// </summary>
        /// <param name="action">Akce k provedení</param>
        /// <param name="useSyncInvoke">Způsob invokování akce: true = synchronní (Invoke) / false = asynchronní (BeginInvoke)</param>
        /// <param name="control">Optional control k invokování, bude otestován</param>
        private void _RunInGuiThread(Action action, bool useSyncInvoke, Control control)
        {
            if (_TryGetUsableControl(control, out var guiControl) && guiControl.InvokeRequired)
            {
                if (useSyncInvoke)
                    guiControl.Invoke(action);
                else
                    guiControl.BeginInvoke(action);
            }
            else
                action();
        }
        /// <summary>
        /// Vrátí první použitelný Control
        /// </summary>
        /// <param name="explicitControl"></param>
        /// <param name="usableControl"></param>
        /// <returns></returns>
        private bool _TryGetUsableControl(Control explicitControl, out Control usableControl)
        {
            usableControl = null;
            Control testControl;

            testControl = explicitControl;
            if (_CanUseAsGuiWindow(testControl)) { usableControl = testControl; return true; }

            if (_FormDict[FormType.Desktop].CanUseAsGuiWindow)
            {
                testControl = _FormDict[FormType.Desktop].Form;
                if (_CanUseAsGuiWindow(testControl)) { usableControl = testControl; return true; }
            }

            if (_FormDict[FormType.FormBuilder].CanUseAsGuiWindow)
            {
                testControl = _FormDict[FormType.FormBuilder].Form;
                if (_CanUseAsGuiWindow(testControl)) { usableControl = testControl; return true; }
            }

            if (_FormDict[FormType.Login].CanUseAsGuiWindow)
            {
                testControl = _FormDict[FormType.Login].Form;
                if (_CanUseAsGuiWindow(testControl)) { usableControl = testControl; return true; }
            }

            return false;
        }
        /// <summary>
        /// Vrátí true, pokud dodaný Control lze použít pro volání a pro invokování.
        /// Musí být not null, vytvořen Handle a nikoli disposován.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private static bool _CanUseAsGuiWindow(Control control)
        {
            return (control != null && control.IsHandleCreated && !control.Disposing && !control.IsDisposed);
        }
        #endregion
        #region Singleton
        /// <summary>
        /// Instance singletonu
        /// </summary>
        private static ApplicationState Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (_Lock)
                    {
                        if (__Instance == null)
                            __Instance = new ApplicationState();
                    }
                }
                return __Instance;
            }
        }
        private static ApplicationState __Instance = null;
        private static object _Lock = new object();
        #endregion
        #region private konstruktor, enum FormType, class FormInfo, Dictionary _FormDict
        /// <summary>
        /// private konstruktor
        /// </summary>
        private ApplicationState()
        {
            _State = ApplicationStateType.Initializing;
            _FormDict = new Dictionary<FormType, FormInfo>();
            _FormDict.Add(FormType.Login, new FormInfo(FormType.Login));
            _FormDict.Add(FormType.Desktop, new FormInfo(FormType.Desktop));
            _FormDict.Add(FormType.FormBuilder, new FormInfo(FormType.FormBuilder));
        }
        /// <summary>
        /// Typy formulářů
        /// </summary>
        private enum FormType { None, Login, Desktop, FormBuilder }
        /// <summary>
        /// Info o jednom formuláři daného typu
        /// </summary>
        private class FormInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="type"></param>
            public FormInfo(FormType type)
            {
                Type = type;
                Form = null;
                State = ApplicationFormStateType.None;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{Type}: {State}";
            }
            /// <summary>
            /// Typ formuláře
            /// </summary>
            public FormType Type { get; private set; }
            /// <summary>
            /// Formulář.
            /// Ukládá se WeakReference na něj.
            /// </summary>
            public Form Form { get { return _Form?.Target; } set { _Form = value; } }
            /// <summary>
            /// Stav formuláře
            /// </summary>
            public ApplicationFormStateType State
            {
                get { return _State; }
                set
                {
                    _State = value;
                    if (value == ApplicationFormStateType.Disposed)
                        _Form = null;
                }
            }
            /// <summary>
            /// Obsahuje true, pokud zdejší Form lze použít jako GUI Window (není null, je v patřičném stavu a prošel kontrolou).
            /// </summary>
            public bool CanUseAsGuiWindow
            {
                get
                {
                    var state = State;
                    if (state == ApplicationFormStateType.Shown || state == ApplicationFormStateType.Visible || state == ApplicationFormStateType.Hidden)
                    {
                        var control = this.Form;
                        if (_CanUseAsGuiWindow(control)) return true;
                    }
                    return false;
                }
            }
            private ApplicationFormStateType _State;
            private WeakTarget<Form> _Form;
        }
        /// <summary>
        /// Dictionary, obsahuje informace pro formuláře typu <see cref="FormType.Login"/>, <see cref="FormType.Desktop"/>, <see cref="FormType.FormBuilder"/>
        /// </summary>
        private Dictionary<FormType, FormInfo> _FormDict;
        #endregion
    }
    #region enum ApplicationStateType : Stav aplikace jako celku;  ApplicationFormStateType : Stav určitého okna aplikace
    /// <summary>
    /// Stav aplikace jako celku
    /// </summary>
    public enum ApplicationStateType
    {
        /// <summary>
        /// Aplikace dosud nebyla rozběhnuta
        /// </summary>
        None = 0,
        /// <summary>
        /// Aplikace se spouští.
        /// </summary>
        Initializing = 0x01,
        /// <summary>
        /// Aplikace zahajuje update ze serveru.
        /// </summary>
        Updating = 0x02,
        /// <summary>
        /// Připravuje se Login okno.
        /// </summary>
        LoginPreparing = 0x10,
        /// <summary>
        /// Je aktivní Login okno. Uživatel se snaží přihlásit.
        /// </summary>
        LoginVisible = 0x12,
        /// <summary>
        /// Okno Login se zavírá.
        /// </summary>
        LoginClosing = 0x16,
        /// <summary>
        /// Okno Login se zavřeno.
        /// </summary>
        LoginClosed = 0x17,
        /// <summary>
        /// Vytváří se okno Desktop, ale dosud nebylo zobrazeno uživateli
        /// </summary>
        DesktopPreparing = 0x20,
        /// <summary>
        /// Je aktivní desktop okno. Většina času aplikace. Desktop může být vivitelný nebo neviditelný, ale již byl zobrazen a dosud se nezavírá.
        /// </summary>
        DesktopRunning = 0x22,
        /// <summary>
        /// Desktop se zavírá - probíhá zavírání jednotlivých oken a poté i vlastního okna Desktop. 
        /// Může být stornováno, pak přejde zpátky do <see cref="DesktopRunning"/>.
        /// Pokud není stornováno, přejde do <see cref="DesktopClosed"/>.
        /// </summary>
        DesktopClosing = 0x26,
        /// <summary>
        /// Konec desktopu = aplikace
        /// </summary>
        DesktopClosed = 0x27,
        /// <summary>
        /// Konec aplikace z jiného důvodu než desktopu. 
        /// Tuto hodnotu musí nastavit aplikace explicitně, tu není možno odvodit od změny stavu oken (z důvodu možných restartů oken nevím, kdy je konec definitivní).
        /// </summary>
        ApplicationEnded = 0xFF
    }
    /// <summary>
    /// Stav určitého okna aplikace
    /// </summary>
    public enum ApplicationFormStateType
    {
        /// <summary>
        /// Okno dosud nebylo vytvářeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Okno je nyní vytvářeno: nastavuje se na začátku konstruktoru, a v okamžiku Show okna přechází stav do <see cref="Visible"/>.
        /// </summary>
        Initializing,
        /// <summary>
        /// Okno je zobrazeno uživateli. Z hlediska viditelnosti je tento stav shodný s <see cref="Visible"/>, ale pochází z události Form.Shown
        /// </summary>
        Shown,
        /// <summary>
        /// Okno je vytvořeno (bylo <see cref="Initializing"/>) a je zobrazeno uživateli.
        /// Okno následně může být skryto (bude ve stavu <see cref="Hidden"/>) anebo se může začít zavírat (<see cref="Closing"/>).
        /// Okno je v tomto stavu plně použitelné.
        /// </summary>
        Visible,
        /// <summary>
        /// Okno je vytvořeno (bylo <see cref="Initializing"/>), ale nyní je uživateli skryto.
        /// Okno poté může být znovu zobrazeno <see cref="Visible"/> anebo se může začít zavírat <see cref="Closing"/>.
        /// Okno je v tomto stavu plně použitelné.
        /// </summary>
        Hidden,
        /// <summary>
        /// Okno se zavírá = proběhl pokus o jeho zavření, ale není jisté zda bude reálně zavřeno.
        /// Může se vrátit do stavu <see cref="Visible"/>, anebo přejít do <see cref="Closed"/>.
        /// Okno v tomto stavu není vhodné používat.
        /// </summary>
        Closing,
        /// <summary>
        /// Okno je zavřeno. Už se nemůže vrátit do života, bude disposováno.
        /// Okno v tomto stavu není použitelné.
        /// </summary>
        Closed,
        /// <summary>
        /// Okno je disposováno, reference na něj je nepoužitelná.
        /// </summary>
        Disposed
    }
    #endregion
}
