// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Green, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using Noris.Clients.Win.Components.AsolDX;

namespace Noris.Clients.Win.Components
{
    /// <summary>
    /// Univerzální dialogové okno
    /// </summary>
    public class DialogForm : DevExpress.XtraEditors.XtraForm, IListenerExcludeFromCaptureContentChanged, IEscapeHandler
    {
        #region Public aktivace, konstruktor, interní události
        /// <summary>
        /// Zobrazí modální dialog podle parametrů.
        /// <para/>
        /// Upozornění: toto okno při zobrazení modálního dialogu blokuje uživatelský vstup do všech ostatních oken aplikace,
        /// ale NEBLOKUJE provádění invokace do GUI threadu (typicky zpracování požadavků z vláken OnBackground v GUI threadu).
        /// <para/>
        /// Není tedy potřeba řešit modalitu pomocí <see cref="Application.DoEvents()"/> (se všemi riziky a s řešením blokování uživatelských vstupů).
        /// To nabízí metoda <see cref="DxComponent.DoEventsWithBlockingInput()"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object ShowDialog(DialogArgs args)
        {
            if (args is null)
                throw new ArgumentNullException("args", "Argument 'args' is null in DialogForm.ShowDialog(args) !");

            object dialogFormResult = args.DefaultResultValue;
            using (DialogForm form = new DialogForm())
            {
                //    form.DialogResult = DialogResult.None;                   // DAJ & AI 0080316: Pokud do XtraForm nastavím jakoukoli hodnotu do .DialogResult, pak zobrazení modálního okna ShowDialog() způsobí jeho okamžité zavření.  Nevím proč, ale je to tak...  PS: není to řešením, protože samotný WinForm nastavuje DialogResult = DialogResult.None ...
                form.DialogFormResult = dialogFormResult;
                form.CreateByArgs(args);
                form.__CloseAction = null;
                form._WriteDebug($"MessageBox.ShowDialog: {args}");

                // Owner okno vyjmu z argumentu, aby tam nezůstala reference na něj (= pak by se okno bránilo zavření!)
                var owner = args.Owner;
                args.Owner = null;
                try
                {
                    form.ShowDialog(owner);
                }
                catch (Exception exc)
                {

                }

                if (owner != null && owner is Form ownerForm && !ownerForm.IsDisposed)
                    ownerForm.Activate();

                dialogFormResult = form.DialogFormResult;
            }
            return dialogFormResult;
        }
        /// <summary>
        /// Zobrazí dialog podle parametrů, nemodálně = řízení se ihned vrátí.
        /// Až uživatel zavře okno, vyvolá se akce <paramref name="closeAction"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="closeAction"></param>
        /// <returns></returns>
        public static void Show(DialogArgs args, DialogFormClosingHandler closeAction)
        {
            if (args is null)
                throw new ArgumentNullException("args", "Argument 'args' is null in DialogForm.Show(args) !");

            object dialogFormResult = args.DefaultResultValue;
            DialogForm form = new DialogForm();
            //    form.DialogResult = DialogResult.None;                       // DAJ & AI 0080316: Pokud do XtraForm nastavím jakoukoli hodnotu do .DialogResult, pak zobrazení modálního okna ShowDialog() způsobí jeho okamžité zavření.  Nevím proč, ale je to tak...  PS: není to řešením, protože samotný WinForm nastavuje DialogResult = DialogResult.None ...
            form.DialogFormResult = dialogFormResult;
            form.CreateByArgs(args);
            form.__CloseAction = closeAction;
            form._WriteDebug($"MessageBox.Show: {args}");

            // Owner okno vyjmu z argumentu, aby tam nezůstala reference na něj (= pak by se okno bránilo zavření!)
            var owner = args.Owner;
            args.Owner = null;
            form.Show(owner);                                                  // NeModální zobrazení = řízení se vrací ihned, okno bude zobrazeno uživateli asynchronně
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        protected DialogForm()
        {
            InitializeComponent();
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.__BoundsStdText = null;
            this.__BoundsAltText = null;
            DxComponent.RegisterListener(this);
            __Buttons = new List<DevExpress.XtraEditors.SimpleButton>();
            __ZoomRatio = 1f;
        }
        /// <summary>
        /// WinForm designer, prázdné
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DialogForm
            // 
            this.ClientSize = new System.Drawing.Size(445, 207);
            this.Name = "DialogForm";
            this.ResumeLayout(false);

        }
        /// <summary>
        /// Při zobrazení okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this._WriteDebug($"MessageBox.OnShown: {__DialogArgs}");
            this.DialogForm_Shown();
        }
        /// <summary>
        /// Při aktivaci okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this._WriteDebug($"MessageBox.OnActivated: {__DialogArgs}");
        }
        /// <summary>
        /// Při deaktivaci okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this._WriteDebug($"MessageBox.OnDeativated: {__DialogArgs}");
        }
        /// <summary>
        /// DevExpress setuje Visible
        /// </summary>
        /// <param name="value"></param>
        protected override void SetVisibleCore(bool value)
        {
            // DevExpress při přidávání tohoto okna do DocumentManager.View.AddFloatDocument(form)
            //   tady:  WDesktop: public void AddFormAsFloating(Form form, bool activate = false, string iconName = null)
            //   nastavuje Visible pro celý Form na true ještě dříve, než dojde k Show formnuláře.
            // My potřebujeme v této situaci (setování Visible = true dříve než Show()) vypočítat layout okna a jeho velikost.
            // Proto volám FirstShown(), tam to všechno je. A pak se to už nebude provádět v prvním Show() okna.
            if (value && !__IsShown)
                this.FirstShown();

            base.SetVisibleCore(value);
        }
        /// <summary>
        /// Při vstupu focusu do okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this._WriteDebug($"MessageBox.OnGotFocus: {__DialogArgs}");
        }
        /// <summary>
        /// Při odchodu focusu z okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            this._WriteDebug($"MessageBox.OnLostFocus: {__DialogArgs}");
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this._RefreshLayout(null);
        }
        /// <summary>
        /// Zpracuje klávesu v okně
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            bool handled = ProcessKeyForButtons(keyData);
            if (!handled)
                handled = _ProcessKeyForClipboard(keyData);
            if (!handled)
                handled = base.ProcessDialogKey(keyData);
            return handled;
        }
        /// <summary>
        /// Při zavírání okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this._WriteDebug($"MessageBox.OnClosing: {__DialogArgs}");
            __DialogArgs.ResultValue = this.DialogFormResult;
            if (this.__CloseAction != null)
            {
                DialogFormClosingArgs args = new DialogFormClosingArgs(__DialogArgs);
                this.__CloseAction(this, args);
                e.Cancel = args.Cancel;
            }
        }
        /// <summary>
        /// Není to Event (ten je multitarget), je to prostá akce, která byla předaná do konstruktoru v metodě Nemodální Show.
        /// </summary>
        private DialogFormClosingHandler __CloseAction;
        /// <summary>
        /// Zkusí poslat daný string do debug containeru, pokud je specifikován v metodě <see cref="DialogArgs.DebugAction"/>.
        /// </summary>
        /// <param name="text"></param>
        private void _WriteDebug(string text)
        {
            var debugAction = __DialogArgs?.DebugAction;
            if (debugAction != null) debugAction(text);
        }
        /// <summary>
        /// Dispose okna
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            base.Dispose(disposing);
        }
        #endregion
        #region Public data
        /// <summary>
        /// Definice okna dialogu
        /// </summary>
        public DialogArgs DialogArgs { get { return __DialogArgs; } protected set { __DialogArgs = value; } }
        #endregion
        #region Skrývání obsahu formuláře pro externí aplikace nahrávající obsah (Capture content), listener IListenerNotCaptureWindowsChanged
        /// <summary>
        /// Pokud je true, pak obsah tohoto okna nebude zachycen aplikacemi jako Teams, Recording, PrintScreen atd.<br/>
        /// Výchozí je hodnota odpovídající (! <see cref="DxComponent.ExcludeFromCaptureContent"/>).
        /// <para/>
        /// Okno <see cref="DxRibbonForm"/> implementuje <see cref="IListenerExcludeFromCaptureContentChanged"/> a reaguje tak na hodnotu <see cref="DxComponent.ExcludeFromCaptureContent"/>,<br/>
        /// automaticky tedy nastavuje zdejší hodnotu <see cref="ExcludeFromCaptureContent"/> podle ! <see cref="DxComponent.ExcludeFromCaptureContent"/>.
        /// <para/>
        /// Využívá SetWindowDisplayAffinity : <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity"/>
        /// </summary>
        public bool ExcludeFromCaptureContent
        {
            get { return __ExcludeFromCaptureContent; }
            set
            {
                bool currentValue = __ExcludeFromCaptureContent;
                if (value != currentValue)
                {
                    __ExcludeFromCaptureContent = value;
                    AcceptExcludeFromCaptureContent();
                }
            }
        }
        private bool __ExcludeFromCaptureContent;
        /// <summary>
        /// Metoda zajistí, že toto okno bude mít nastavenu reálnou vlastnost <c>Winapi.WindowDisplayAffinity</c> podle zdejší hodnoty <see cref="__ExcludeFromCaptureContent"/>.
        /// </summary>
        protected void AcceptExcludeFromCaptureContent()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                DxComponent.SetWindowDisplayAffinity(this, __ExcludeFromCaptureContent);
            }
        }
        void IListenerExcludeFromCaptureContentChanged.ExcludeFromCaptureContentChanged() { OnExcludeFromCaptureContentChanged(); }
        /// <summary>
        /// Zavolá se tehdy, když aplikace změnila hodnotu v <see cref="DxComponent.ExcludeFromCaptureContent"/> = mění se stav <c>WinApi.SetWindowDisplayAffinity</c> (pomocí listeneru <see cref="IListenerExcludeFromCaptureContentChanged"/>).
        /// </summary>
        protected virtual void OnExcludeFromCaptureContentChanged()
        {
            this.ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
        }
        /// <summary>
        /// Po vytvoření Handle pro formulář si pro toto Window aktualizujeme <c>WindowDisplayAffinity</c>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            __ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
            AcceptExcludeFromCaptureContent();
        }
        #endregion
        #region Interaktivita
        /// <summary>
        /// Umístí focus do vhodného prvního prvku v dialogu (Inputbox / Initial Button)
        /// </summary>
        public void SetFocusToDialog()
        {
            _SetFocusToDialog();
        }
        /// <summary>
        /// Při zobrazení = zajistíme focus
        /// </summary>
        private void DialogForm_Shown()
        {   // Zajistíme TopMost pozici při zobrazení:
            if (!__IsShown)
                this.FirstShown();
        }
        /// <summary>
        /// První zobrazení řeší TopMost a Focus
        /// </summary>
        private void FirstShown()
        {
            __IsShown = true;
            this.TopMost = true;
            this._WriteDebug($"MessageBox.FirstShown: {__DialogArgs}");

            var formState = __DialogArgs.FormState;
            this.ShowInTaskbar = formState.HasFlag(DialogFormState.ShowInTaskbar);
            if (!formState.HasFlag(DialogFormState.TopMost))
                this.TopMost = false;

            this._WriteDebug($"MessageBox.Activating: {__DialogArgs}");
            this.Activate();
            this._WriteDebug($"MessageBox.Activated: {__DialogArgs}");

            // Událost:
            if (__DialogArgs.DialogFirstShowAction != null)
                __DialogArgs.DialogFirstShowAction(this);

            // Focus do vhodného controlu:
            _SetFocusToDialog();
        }
        private bool __IsShown = false;
        /// <summary>
        /// Umístí focus do vhodného prvního prvku v dialogu (Inputbox / Initial Button)
        /// </summary>
        private void _SetFocusToDialog()
        {
            if (this.__InputVisible)
            {
                __InputControl.Select();
                __InputControl.Focus();
            }
            else if (this.__ButtonsVisible)
            {
                Control initialButton = this.__Buttons.FirstOrDefault(b => b.Tag is DialogArgs.ButtonInfo buttonInfo && buttonInfo.IsInitialButton);
                if (initialButton is null)
                    initialButton = this.__Buttons[0];

                initialButton.Select();
                initialButton.Focus();
            }
        }
        /// <summary>
        /// Zpracuje stisknutou klávesu jako aktivaci Buttonu
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected virtual bool ProcessKeyForButtons(Keys keyData)
        {
            Control button = null;
            Keys modifiers = keyData & Keys.Modifiers;
            Keys keyCode = keyData ^ modifiers;
            if (modifiers == Keys.Control && keyCode == Keys.Enter)
                button = _FindButton(b => b.IsImplicitButton);
            else if (modifiers == Keys.None && keyCode == Keys.Escape)
                button = _FindButton(b => b.IsEscapeButton);
            else
                button = _FindButton(b => _IsButtonActivatedByKey(b, keyData));

            if (button == null) return false;

            button.Focus();
            _Button_Click(button, EventArgs.Empty);
            return true;
        }
        /// <summary>
        /// Najde button podle podmínky
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.SimpleButton _FindButton(Func<DialogArgs.ButtonInfo, bool> filter)
        {
            if (__Buttons == null || __Buttons.Count == 0) return null;
            return __Buttons.FirstOrDefault(b => b.Tag is DialogArgs.ButtonInfo buttonInfo && filter(buttonInfo));
        }
        /// <summary>
        /// Vrátí true, pokud daný button je aktivován danou klávesou
        /// </summary>
        /// <param name="buttonInfo"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private bool _IsButtonActivatedByKey(DialogArgs.ButtonInfo buttonInfo, Keys keyData)
        {
            if (!buttonInfo.ActiveKey.HasValue) return false;
            return (buttonInfo.ActiveKey.Value == keyData);
        }
        /// <summary>
        /// Zpracuje Ctrl+C
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private bool _ProcessKeyForClipboard(Keys keyData)
        {
            Keys modifiers = keyData & Keys.Modifiers;
            Keys keyCode = keyData ^ modifiers;
            if (modifiers == Keys.Control && keyCode == Keys.C)
            {
                _ClipboardCopy();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Zkopíruje text z okna do schránky
        /// </summary>
        private void _ClipboardCopy(bool asImage = false)
        {
            var args = __DialogArgs;
            string copyOk = args.StatusBarCtrlCInfo;
            bool showCopyOk = __StatusBar.Visible && !String.IsNullOrEmpty(copyOk);
            StringBuilder sb = (asImage ? null : new StringBuilder());

            if (!asImage)
            {   // Text
                if (!String.IsNullOrEmpty(args.Title))
                {
                    sb.AppendLine(args.Title);
                    sb.AppendLine("".PadRight(args.Title.Length, '='));
                }

                addText(args.MessageText, args.MessageTextContainsHtml);
                addText(args.AltMessageText, args.AltMessageTextContainsHtml);

                sb.AppendLine();
                string buttonsLine = "";
                string buttonsText = "";
                string space = "    ";
                foreach (var button in args.Buttons)
                {
                    string buttonText = (button.Text ?? "").Replace("&", "");
                    int length = buttonText.Length + 4;
                    buttonsLine += "".PadRight(length, '-') + space;
                    buttonsText += "[ " + buttonText + " ]" + space;
                }
                sb.AppendLine(buttonsLine.Trim());
                sb.AppendLine(buttonsText.Trim());
                sb.AppendLine(buttonsLine.Trim());

                DxComponent.ClipboardInsert(sb.ToString());
            }
            else
            {   // Image
                Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
                this.DrawToBitmap(bitmap, bounds);

                try
                {
                    System.Windows.Forms.Clipboard.Clear();
                    System.Windows.Forms.Clipboard.SetImage(bitmap);
                    if (showCopyOk) _ShowStatus2(copyOk);
                }
                catch { }
            }

            // Přidá daný text...
            void addText(string text, bool containsHtml)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    sb.AppendLine();
                    if (containsHtml)
                        text = ConvertFormat.HtmlToText(text, true);
                    sb.AppendLine(text);
                }
            }
        }
        private void _InputControl_MouseEnter(object sender, EventArgs e)
        {
            _ShowStatus1InputInfo();
        }
        private void _InputControl_MouseLeave(object sender, EventArgs e)
        {
            _ShowStatus1Current();
        }
        private void _InputControl_Enter(object sender, EventArgs e)
        {
            __ActiveInputControl = true;
            __ActiveButtonInfo = null;
            _ShowStatus1InputInfo();
        }
        private void _Button_MouseEnter(object sender, EventArgs e)
        {
            if (_TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
            {
                _ShowStatus1(buttonInfo);
            }
        }
        private void _Button_MouseLeave(object sender, EventArgs e)
        {
            _ShowStatus1(__ActiveButtonInfo);
        }
        private void _Button_Enter(object sender, EventArgs e)
        {
            if (_TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
            {
                __ActiveInputControl = false;
                __ActiveButtonInfo = buttonInfo;
                _ShowStatus1(buttonInfo);
            }
        }
        /// <summary>
        /// Uživatel kliknul na status bar button "COPY"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StatusCopyButton_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _ClipboardCopy();
        }
        /// <summary>
        /// Do statusbaru zobrazí info text k aktuálnímu prvku (<see cref="__ActiveButtonInfo"/> nebo <see cref="__ActiveInputControl"/>).
        /// </summary>
        private void _ShowStatus1Current()
        {
            if (__ActiveButtonInfo != null)
                _ShowStatus1(__ActiveButtonInfo);
            else if (__ActiveInputControl)
                _ShowStatus1InputInfo();
            else
                _ShowStatus1("");

        }
        /// <summary>
        /// Zobrazí info o daném buttonu do Status1, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        /// <param name="buttonInfo"></param>
        private void _ShowStatus1(DialogArgs.ButtonInfo buttonInfo)
        {
            if (!__StatusBar.Visible) return;
            string text = "";
            if (buttonInfo != null)
            {
                // Specialita: pokud máme jen jeden button, a ten nemá svůj StatusBarText, a nemám InputText, pak do statusbaru nic nepíšeme.
                // Protože: pokud mám jen button OK, pak by ve status baru svítilo "OK" bez možnosti změny:
                bool hasStatusBarText = !String.IsNullOrEmpty(buttonInfo.StatusBarText);
                bool hideText = (this.__Buttons.Count <= 1 && !hasStatusBarText && !this.__InputVisible);

                // Požadavek: pokud button neobsahuje přidanou informaci (StatusBarText), pak nevypisovat nic:
                if (!hasStatusBarText)
                    hideText = true;

                if (!hideText)
                    text = buttonInfo.Text + (hasStatusBarText ? text += " : " + buttonInfo.StatusBarText : "");
            }
            _ShowStatus1(text);
        }
        /// <summary>
        /// Zobrazí jako hlavní text ve statusbaru informaci pro InputText, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        private void _ShowStatus1InputInfo()
        {
            _ShowStatus1(__DialogArgs.InputTextStatusInfo);
        }
        /// <summary>
        /// Zobrazí dané info jako hlavní text ve statusbaru do Status1, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        /// <param name="text"></param>
        private void _ShowStatus1(string text)
        {
            __StatusLabel1.Caption = text;
            __StatusLabel1.Refresh();

            __StatusLabel2.Caption = "";
            __StatusLabel2.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;

            __StatusBar.Refresh();
        }
        /// <summary>
        /// Zobrazí daný text do Status2
        /// </summary>
        /// <param name="text"></param>
        private void _ShowStatus2(string text)
        {
            if (!__StatusBar.Visible) return;
            __StatusLabel2.Caption = text;

            if (__StatusLabel2.Visibility != DevExpress.XtraBars.BarItemVisibility.Always)
                __StatusLabel2.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;

            __StatusLabel2.Refresh();
            __StatusBar.Refresh();
        }
        /// <summary>
        /// Po kliknutí na kterýkoli result button dialogu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_Click(object sender, EventArgs e)
        {
            if (_TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
                this.OnButtonClicked(buttonInfo);
        }
        /// <summary>
        /// Zkusí najít <see cref="DialogArgs.ButtonInfo"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="buttonInfo"></param>
        /// <returns></returns>
        private bool _TryGetButtonInfo(object control, out DialogArgs.ButtonInfo buttonInfo)
        {
            buttonInfo = null;
            DevExpress.XtraEditors.BaseButton button = control as DevExpress.XtraEditors.BaseButton;
            if (button is null) return false;
            buttonInfo = button.Tag as DialogArgs.ButtonInfo;
            return (!(buttonInfo is null));
        }
        /// <summary>
        /// Akce po kliknutí na daný button
        /// </summary>
        /// <param name="buttonInfo"></param>
        protected virtual void OnButtonClicked(DialogArgs.ButtonInfo buttonInfo)
        {
            buttonInfo?.OnButtonClicked(this);
        }
        /// <summary>
        /// true pokud je focus ve vstupním prvku
        /// </summary>
        private bool __ActiveInputControl;
        /// <summary>
        /// Info o buttonu, který má nyní focus
        /// </summary>
        private DialogArgs.ButtonInfo __ActiveButtonInfo;
        /// <summary>
        /// Status bar bude zobrazen?
        /// Zde už je vyhodnocena reálná situace podle požadavků a podle reálného obsahu dat.
        /// </summary>
        private bool __StatusBarVisible;
        /// <summary>
        /// Výsledná hodnota dialogu
        /// </summary>
        public object DialogFormResult { get; internal set; }
        /// <summary>
        /// Obsahuje true, pokud this formulář se disposuje nebo již je disposován
        /// </summary>
        protected bool FormIsDisposed { get { return this.Disposing || this.IsDisposed; } }
        #endregion
        #region Vytváření Controlů podle argumentu
        /// <summary>
        /// Podle dodaných argumentů vytvoří obsah okna
        /// </summary>
        /// <param name="args"></param>
        protected void CreateByArgs(DialogArgs args)
        {
            _StoreArgs(args);
            _CreateControls();
            _PrepareInitialBounds();
        }
        /// <summary>
        /// Uloží argument a základní data z něj
        /// </summary>
        /// <param name="args"></param>
        private void _StoreArgs(DialogArgs args)
        {
            __DialogArgs = args;
            this.Text = args.TitleValid;
            this.__ZoomRatio = args.ZoomRatio;
            this.DialogFormResult = args.DefaultResultValue;
            this.__StatusBarVisible = (args.StatusBarVisibleCurrent || !String.IsNullOrEmpty(args.StatusBarCtrlCText) || args.StatusBarCtrlCVisible || args.Buttons.Any(b => !String.IsNullOrEmpty(b.StatusBarText)));

            _WriteDebug($"MessageBox.Initializing: {args}");
        }
        /// <summary>
        /// Podle dodaných argumentů vytvoří obsah okna
        /// </summary>
        private void _CreateControls()
        {
            DialogArgs args = __DialogArgs;

            _InitParentArea(args);

            _CreateFrames(args);
            _CreateIcon(args);
            _CreateInputText(args);                        // Nejprve vytvořím InputText, aby byl v ZOrder pořadí navrhu
            _CreateMessageControls(args);                  //  a teprve poté MessageLabel, to proto že oba prvky se částečně překrývají, a InputText má být nahoře...
            _CreateButtons(args);
            _CreateStatus(args);

            _LinkEvents(args);
        }
        /// <summary>
        /// Určí vizuální prostor pro dialogové okno (<see cref="__InitialMaximumBounds"/> a <see cref="__InitialCenterPoint"/>.
        /// </summary>
        /// <param name="args"></param>
        private void _InitParentArea(DialogArgs args)
        {
            Rectangle? centerInBounds = null;
            int? currentDpi = null;
            if (args != null)
                centerInBounds = args.CenterInBounds;

            if (!centerInBounds.HasValue || (centerInBounds.HasValue && (centerInBounds.Value.Width < 480 || centerInBounds.Value.Height < 360)))
            {
                centerInBounds = null;

                Form ownerForm = args?.Owner as Form;
                if (ownerForm != null)
                {
                    centerInBounds = ownerForm.Bounds;
                    currentDpi = ownerForm.DeviceDpi;
                }

                if (!centerInBounds.HasValue)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetCurrentProcess();
                        var formPtr = process.MainWindowHandle;
                        if (formPtr != IntPtr.Zero)
                        {
                            var form = Control.FromHandle(formPtr);
                            if (form != null)
                            {
                                centerInBounds = form.Bounds;
                                currentDpi = form.DeviceDpi;
                            }
                        }
                    }
                    catch { }
                }

                if (!centerInBounds.HasValue)
                {
                    var primaryScreen = Screen.PrimaryScreen;
                    centerInBounds = primaryScreen.WorkingArea;
                    var dpip = DpiMonitorUtil.GetDpiForScreen(primaryScreen);
                    currentDpi = (int)dpip.dpiX;
                }
            }

            Rectangle b = centerInBounds.Value;
            Point centerPoint = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);

            Screen targetScreen = Screen.FromPoint(centerPoint);
            Rectangle targetBounds = targetScreen.WorkingArea;
            int dx = targetBounds.Width / 16;
            int dy = targetBounds.Height / 16;
            Rectangle maximumBounds = new Rectangle(targetBounds.X + 2 * dx, targetBounds.Y + dy, 12 * dx, 14 * dy);

            if (!currentDpi.HasValue)
            {
                var dpip = DpiMonitorUtil.GetDpiForScreen(targetScreen);
                currentDpi = (int)dpip.dpiX;
            }

            __InitialCenterPoint = centerPoint;
            __InitialMaximumBounds = maximumBounds;
            __InitialDpi = currentDpi;
        }
        /// <summary>
        /// Vytvoří základní panely
        /// </summary>
        /// <param name="args"></param>
        private void _CreateFrames(DialogArgs args)
        {
            var panel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, TabStop = false, Dock = DockStyle.Fill, Name = "_StandardPanel" };
            panel.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            panel.Paint += _StandardPanel_Paint;
            __StandardPanel = panel;
            this.Controls.Add(__StandardPanel);

            var inputType = args.InputTextType;
            this.__InputVisible = (inputType == ShowInputTextType.TextBox || inputType == ShowInputTextType.MemoEdit);

            var ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();                 // Musí existovat, jinak nefunguje StatusBar
            var status = new DevExpress.XtraBars.Ribbon.RibbonStatusBar() { Dock = DockStyle.Bottom, Ribbon = ribbon, Name = "_StatusBar" };
            ribbon.CustomDrawItem += _Ribbon_CustomDrawItem;
            status.Visible = this.__StatusBarVisible;
            __StatusBar = status;
            this.Controls.Add(status);
        }
        /// <summary>
        /// Vytvoří ikonu
        /// </summary>
        /// <param name="args"></param>
        private void _CreateIcon(DialogArgs args)
        {
            this.Icon = _GetSystemIcon(DialogSystemIcon.Information);

            this.__IconVisible = false;
            this.__IconSize = null;

            // Máme nějak definovaný obrázek?
            var iconArea = new DxImageArea();
            if (args.Icon != null)
                iconArea.ImageObject = args.Icon;
            else if (!String.IsNullOrEmpty(args.IconName))
                iconArea.ImageName = args.IconName;
            else if (args.StandardIcon.HasValue)
                iconArea.ImageObject = _GetStandardIcon(args.StandardIcon.Value)?.ToBitmap();
            else if (args.SystemIcon.HasValue)
                iconArea.ImageObject = _GetSystemIcon(args.SystemIcon.Value)?.ToBitmap();
            if (iconArea.IsEmpty) return;

            // Velikost obrázku ikony:
            __IconArea = iconArea;
            __IconSize = createIconSize(iconArea.ImageSource);
            __IconVisible = true;


            // Vrátí velikost ikony/image
            Size createIconSize(DxImageArea.ImageSourceType imageSource)
            {
                var iw = args.IconSizeWidth;                                   // V argumentu zadaná šířka
                var ih = args.IconSizeHeight;                                  // V argumentu zadaná výška
                var width = iw ?? ih ?? 32;                                    // Šířka ikony z argumentu (šířka) nebo náhradní (výška) nebo defaultní 32px
                var height = ih ?? iw ?? 32;                                   // Výška ikony z argumentu (výška) nebo náhradní (šířka) nebo defaultní 32px
                bool isBigImage = (imageSource == DxImageArea.ImageSourceType.SvgContent || imageSource == DxImageArea.ImageSourceType.BinaryData || imageSource == DxImageArea.ImageSourceType.ExternalImage);   // Typ vstupu obrázku napovídá něco o velkém obrázku
                int maxw = (!isBigImage ? 64 : 768);
                int maxh = (!isBigImage ? 64 : 384);
                width = (width < 16 ? 16 : width > maxw ? maxw : width);
                height = (height < 16 ? 16 : height > maxh ? maxh : height);
                return new Size(width, height);
            }
        }
        /// <summary>
        /// Vytvoří prvky pro zobrazení textu
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void _CreateMessageControls(DialogArgs args)
        {
            var messagePanel = new DevExpress.XtraEditors.XtraScrollableControl() { TabStop = false, Name = "_MessagePanel" };
            this.__MessagePanel = messagePanel;
            this.__StandardPanel.Controls.Add(messagePanel);

            if (!this._ExistsAnyText) return;

            string text = _GetPrimaryMessageText(out bool allowHtml);
            if (String.IsNullOrEmpty(text)) return;

            __StyleText = new DevExpress.XtraEditors.StyleController();
            __StyleText.Appearance.FontSizeDelta = _GetZoomDelta();
            __StyleText.Appearance.Options.UseBorderColor = false;
            __StyleText.Appearance.Options.UseBackColor = false;
            __StyleText.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;

            DevExpress.XtraEditors.LabelControl label = new DevExpress.XtraEditors.LabelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Dock = DockStyle.Top, Name = "_MessageLabel" };
            // label.StyleController = _StyleText;
            label.Appearance.FontSizeDelta = _GetZoomDelta();
            label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            if (!this.__InputVisible)
            {   // Informativní text:
                label.Appearance.TextOptions.HAlignment = _ConvertHAlignment(args.MessageHorizontalAlignment);
                label.Appearance.Options.UseTextOptions = true;
            }
            else
            {   // Pokud máme zobrazen Input, pak Message má roli "Návodný label:
                label.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
                label.Appearance.TextOptions.VAlignment = VertAlignment.Bottom;
                label.Appearance.Options.UseTextOptions = true;
            }
            label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            __MessageLabel = label;
            this.__MessagePanel.Controls.Add(label);

            DevExpress.XtraEditors.MemoEdit memoEdit = new DevExpress.XtraEditors.MemoEdit() { ReadOnly = true, TabStop = false, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Visible = false, Name = "_MessageMemo" };
            memoEdit.StyleController = __StyleText;
            __MessageMemo = memoEdit;
            this.__MessagePanel.Controls.Add(memoEdit);

            _FillMessageText(text, allowHtml, false, null);
            _RefreshAltMsgButtonText();
        }
        /// <summary>
        /// Vytvoří objekty pro vstup textu
        /// </summary>
        /// <param name="args"></param>
        private void _CreateInputText(DialogArgs args)
        {
            if (!__InputVisible) return;

            if (args.InputTextType != ShowInputTextType.MemoEdit)
            {   // Jednořádkový vstup:
                var textbox = new DevExpress.XtraEditors.TextEdit() { StyleController = __StyleText, Text = args.InputTextValue, Name = "_InputControl " };

                if (args.InputTextPasswordChar.HasValue)
                    textbox.Properties.PasswordChar = args.InputTextPasswordChar.Value;
                if (args.InputTextMaxLength.HasValue && args.InputTextMaxLength.Value > 0)
                    textbox.Properties.MaxLength = args.InputTextMaxLength.Value;
                textbox.Properties.NullValuePrompt = args.InputTextEmptyHint;

                textbox.MouseEnter += _InputControl_MouseEnter;
                textbox.MouseLeave += _InputControl_MouseLeave;
                textbox.Enter += _InputControl_Enter;
                textbox.Leave += _InputControl_Leave;
                this.__StandardPanel.Controls.Add(textbox);
                __InputControl = textbox;
            }
            else
            {   // Víceřádkový Memo:
                var editbox = new DevExpress.XtraEditors.MemoEdit() { StyleController = __StyleText, Text = args.InputTextValue, Name = "_InputControl" };

                if (args.InputTextMaxLength.HasValue && args.InputTextMaxLength.Value > 0)
                    editbox.Properties.MaxLength = args.InputTextMaxLength.Value;
                editbox.Properties.NullValuePrompt = args.InputTextEmptyHint;

                editbox.MouseEnter += _InputControl_MouseEnter;
                editbox.MouseLeave += _InputControl_MouseLeave;
                editbox.Enter += _InputControl_Enter;
                editbox.Leave += _InputControl_Leave;
                this.__StandardPanel.Controls.Add(editbox);
                __InputControl = editbox;
            }
        }
        /// <summary>
        /// Při opouštění editoru si uložím hodnotu do args
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _InputControl_Leave(object sender, EventArgs e)
        {
            __DialogArgs.InputTextValue = __InputControl.Text;
        }
        /// <summary>
        /// Vytvoří panel pro buttony a vytvoří i jednotlivé buttony podle daných dat
        /// </summary>
        /// <param name="args"></param>
        private void _CreateButtons(DialogArgs args)
        {   // Patřičný Panel existuje i když blok není využit. Pak má Panel nastaveno Visible = false, a NEOBSAHUJE vnitřní controly:
            this.__ButtonsVisible = false;

            __Buttons = new List<DevExpress.XtraEditors.SimpleButton>();
            bool isButtonsVisible = (args.Buttons.Count > 0);

            var panel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, TabStop = false };
            panel.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            panel.Visible = isButtonsVisible;
            __ButtonPanel = panel;

            if (!isButtonsVisible) return;

            this.__StandardPanel.Controls.Add(__ButtonPanel);

            __StyleButton = new DevExpress.XtraEditors.StyleController();
            __StyleButton.Appearance.FontSizeDelta = _GetZoomDelta();
            Font font = __StyleButton.Appearance.GetFont();

            int buttonMaxWidth = 0;
            int buttonHeight = _ButtonHeight;
            int imageWidth = buttonHeight - 8;
            var imageSize = (buttonHeight <= 32 ? ResourceImageSizeType.Small : ResourceImageSizeType.Medium);
            foreach (var buttonInfo in args.Buttons)
            {
                DevExpress.XtraEditors.SimpleButton button = new DevExpress.XtraEditors.SimpleButton() { Text = buttonInfo.Text, Size = new Size(140, buttonHeight) };
                button.StyleController = __StyleButton;
                button.Tag = buttonInfo;

                // Obrázek u tlačítka:
                bool hasImage = false;
                if (buttonInfo.Image != null || !String.IsNullOrEmpty(buttonInfo.ImageName))
                {
                    DxComponent.ApplyImage(button.ImageOptions, buttonInfo.ImageName, buttonInfo.Image, imageSize);
                    button.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
                    button.ImageOptions.ImageToTextIndent = 3;
                    button.ImageOptions.SvgImageSize = new Size(imageWidth, imageWidth);
                    hasImage = true;
                }

                // ToolTip:
                button.SuperTip = DxComponent.CreateDxSuperTip(buttonInfo.ToolTipTitle, buttonInfo.ToolTipText, buttonInfo.Text);

                // Eventy buttonu:
                button.Enter += _Button_Enter;
                button.MouseEnter += _Button_MouseEnter;
                button.MouseLeave += _Button_MouseLeave;
                button.Click += _Button_Click;

                __Buttons.Add(button);
                __ButtonPanel.Controls.Add(button);

                var textSize = this.MeasureString(buttonInfo.Text, font);
                int buttonWidth = textSize.Width + buttonHeight;
                if (hasImage) buttonWidth += (imageWidth + 3);
                if (buttonMaxWidth < buttonWidth) buttonMaxWidth = buttonWidth;
            }

            if (buttonMaxWidth < _ButtonMinWidth) buttonMaxWidth = _ButtonMinWidth;
            if (buttonMaxWidth > _ButtonMaxWidth) buttonMaxWidth = _ButtonMaxWidth;
            foreach (var button in __Buttons)
                button.Width = buttonMaxWidth;

            this.__ButtonsVisible = true;
        }
        /// <summary>
        /// Vytvoří obsah pro StatusBar. Samotný StatusBar je součástí tvorby Frames v metodě <see cref="_CreateFrames(DialogArgs)"/>.
        /// </summary>
        /// <param name="args"></param>
        private void _CreateStatus(DialogArgs args)
        {
            var status = __StatusBar;

            int fontSizeDelta = _GetZoomDelta();

            if (this._ExistsBothText)
                __StatusAltTextCheckButton = _AddStatusCheckButton(args, args.StatusBarAltMsgButtonText, MsgCode.DialogFormAltMsgButtonText, args.StatusBarAltMsgButtonTooltip, MsgCode.DialogFormAltMsgButtonToolTip, args.StatusBarAltMsgButtonImage, 78, 18, fontSizeDelta, _StatusAltTextButton_ItemClick);

            __StatusLabel1 = DxComponent.CreateDxStatusLabel(status, "", DevExpress.XtraBars.BarStaticItemSize.Spring, true, fontSizeDelta);
            __StatusLabel2 = DxComponent.CreateDxStatusLabel(status, "", DevExpress.XtraBars.BarStaticItemSize.Content, false, fontSizeDelta);

            if (args.StatusBarCtrlCVisible || !String.IsNullOrEmpty(args.StatusBarCtrlCText))
                __StatusCopyButton = _AddStatusButton(args, args.StatusBarCtrlCText, MsgCode.DialogFormCtrlCText, args.StatusBarCtrlCTooltip, MsgCode.DialogFormCtrlCToolTip, args.StatusBarCtrlCImage, 78, 18, fontSizeDelta, _StatusCopyButton_ItemClick);

            _RefreshAltMsgButtonText();
        }
        /// <summary>
        /// Naváže k eventům v <paramref name="args"/> zdejší handlery (aktuálně <see cref="DialogArgs.ClickedButton"/>)
        /// </summary>
        /// <param name="args"></param>
        private void _LinkEvents(DialogArgs args)
        {
            args.ClickedButton += _Args_ClickedButton;
        }
        /// <summary>
        /// Aplikační kód chce kliknout na určitý button. Je voláno z libovolného threadu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Args_ClickedButton(object sender, TEventArgs<DialogArgs.ButtonInfo> e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => { doClickButton(e.Item); }));
            else
                doClickButton(e.Item);

            // Provede kliknutí na button. Jsme v GUI threadu.
            void doClickButton(DialogArgs.ButtonInfo button)
            {
                if (button != null)
                {   // Klik na button:
                    var dxButton = this._FindButton(b => Object.ReferenceEquals(b, button));
                    if (dxButton != null)
                    {
                        dxButton.Focus();
                        _Button_Click(dxButton, EventArgs.Empty);
                    }
                }
                else
                {   // Zavření okna křížkem:
                    this.Close();
                }
            }
        }
        /// <summary>
        /// Vykreslení panelu vykreslí ikonu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StandardPanel_Paint(object sender, PaintEventArgs e)
        {
            if (__IconVisible && __IconArea != null)
                __IconArea.OnPaint(e);
        }
        /// <summary>
        /// Podpora pro CustomDraw buttonu ve StatusBaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Ribbon_CustomDrawItem(object sender, DevExpress.XtraBars.BarItemCustomDrawEventArgs e)
        {
            var bItem = e.RibbonItemInfo.Item;
            if (bItem != null && bItem is DevExpress.XtraBars.BarItemLink itemLink && itemLink.Item is IBarItemCustomDrawing cdr)
                cdr.CustomDraw(e);
        }
        /// <summary>
        /// Přidá CheckButton do StatusBaru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="textCode"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipCode"></param>
        /// <param name="imageName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        private DxBarCheckItem _AddStatusCheckButton(DialogArgs args, string text, MsgCode textCode, string toolTipText, MsgCode toolTipCode, string imageName, int width, int height, int fontSizeDelta, DevExpress.XtraBars.ItemClickEventHandler clickHandler)
        {
            if (String.IsNullOrEmpty(text)) text = DxComponent.Localize(textCode);
            if (String.IsNullOrEmpty(toolTipText)) toolTipText = DxComponent.Localize(toolTipCode);

            var button = DxComponent.CreateDxStatusCheckButton(__StatusBar, text, width, height, args.StatusBarButtonsBorderStyles, null, toolTipText, true, fontSizeDelta, clickHandler);
            return button;
        }
        /// <summary>
        /// Přidá Button do StatusBaru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="textCode"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipCode"></param>
        /// <param name="imageName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        private DxBarButtonItem _AddStatusButton(DialogArgs args, string text, MsgCode textCode, string toolTipText, MsgCode toolTipCode, string imageName, int width, int height, int fontSizeDelta, DevExpress.XtraBars.ItemClickEventHandler clickHandler)
        {
            if (String.IsNullOrEmpty(text)) text = DxComponent.Localize(textCode);
            if (String.IsNullOrEmpty(toolTipText)) toolTipText = DxComponent.Localize(toolTipCode);

            var button = DxComponent.CreateDxStatusButton(__StatusBar, text, width, height, args.StatusBarButtonsBorderStyles, null, toolTipText, true, fontSizeDelta, clickHandler);
            DxComponent.ApplyImage(button.ImageOptions, imageName);
            return button;
        }
        /// <summary>
        /// Vrátí <see cref="Icon"/> pro danou standardní ikonu. Poznámka: tento Enum nepokrývá všechny Systémové ikony.
        /// </summary>
        /// <param name="standardIcon"></param>
        /// <returns></returns>
        private static Icon _GetStandardIcon(System.Windows.Forms.MessageBoxIcon standardIcon)
        {
            switch (standardIcon)
            {
                case System.Windows.Forms.MessageBoxIcon.None: return null;
                case System.Windows.Forms.MessageBoxIcon.Hand: return System.Drawing.SystemIcons.Hand;
                // Stop == Hand :  case System.Windows.Forms.MessageBoxIcon.Stop: return System.Drawing.SystemIcons.Error;
                // Error == Hand : case System.Windows.Forms.MessageBoxIcon.Error: return System.Drawing.SystemIcons.Error;
                case System.Windows.Forms.MessageBoxIcon.Question: return System.Drawing.SystemIcons.Question;
                case System.Windows.Forms.MessageBoxIcon.Exclamation: return System.Drawing.SystemIcons.Exclamation;
                // Warning == Exclamation : case System.Windows.Forms.MessageBoxIcon.Warning: return System.Drawing.SystemIcons.Warning;
                case System.Windows.Forms.MessageBoxIcon.Asterisk: return System.Drawing.SystemIcons.Asterisk;
                    // Information == Asterisk : case System.Windows.Forms.MessageBoxIcon.Information: return System.Drawing.SystemIcons.Information;
            }
            return null;
        }
        /// <summary>
        /// Vrátí <see cref="Icon"/> pro danou systémovou ikonu. Poznámka: tento Enum nepokrývá všechny Systémové ikony.
        /// </summary>
        /// <param name="systemIcon"></param>
        /// <returns></returns>
        private static Icon _GetSystemIcon(DialogSystemIcon systemIcon)
        {
            switch (systemIcon)
            {
                case DialogSystemIcon.Application: return System.Drawing.SystemIcons.Application;
                case DialogSystemIcon.Asterisk: return System.Drawing.SystemIcons.Asterisk;
                case DialogSystemIcon.Error: return System.Drawing.SystemIcons.Error;
                case DialogSystemIcon.Exclamation: return System.Drawing.SystemIcons.Exclamation;
                case DialogSystemIcon.Hand: return System.Drawing.SystemIcons.Hand;
                case DialogSystemIcon.Information: return System.Drawing.SystemIcons.Information;
                case DialogSystemIcon.Question: return System.Drawing.SystemIcons.Question;
                case DialogSystemIcon.Warning: return System.Drawing.SystemIcons.Warning;
                case DialogSystemIcon.WinLogo: return System.Drawing.SystemIcons.WinLogo;
                case DialogSystemIcon.Shield: return System.Drawing.SystemIcons.Shield;
            }
            return null;
        }
        /// <summary>
        /// Definice okna dialogu
        /// </summary>
        private DialogArgs __DialogArgs;
        /// <summary>
        /// Obsahuje true pokud má být viditelná ikona. Určeno je to při tvorbě. Hodnota se využívá namísto Icon.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool __IconVisible;
        /// <summary>
        /// Velikost ikony v reálných pixelech
        /// </summary>
        private Size? __IconSize;
        /// <summary>
        /// Obsahuje true pokud má být viditelný Input box. Určeno je to při tvorbě. Hodnota se využívá namísto InputPanel.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool __InputVisible;
        /// <summary>
        /// Obsahuje true pokud mají být viditelné Buttony. Určeno je to při tvorbě. Hodnota se využívá namísto ButtonsPanel.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool __ButtonsVisible;
        /// <summary>
        /// Obsahuje velikost containeru buttonů na jejich dokované straně:<br/>
        /// Pokud jsou buttony dokované vlevo a vpravo, pak je zde Width = šířka panelu buttonů (buttony odebírají šířku z formuláře), ale Height = 0 (buttony neodebírají výšku z formuláře).<br/>
        /// Pokud jsou buttony dokované dole, pak je zde Width = 0 (buttony neodebírají šířku z formuláře), ale Height = výška panelu buttonů (buttony odebírají výšku z formuláře).<br/>
        /// </summary>
        private Size __ButtonsDockSize;

        DevExpress.XtraEditors.StyleController __StyleText;
        DevExpress.XtraEditors.StyleController __StyleButton;
        System.Windows.Forms.Control __StandardPanel;
        DxImageArea __IconArea;
        System.Windows.Forms.Control __MessagePanel;
        DevExpress.XtraEditors.LabelControl __MessageLabel;
        System.Windows.Forms.Control __MessageMemo;
        System.Windows.Forms.Control __ButtonPanel;
        // System.Windows.Forms.Control _ExpanderPanel;
        DevExpress.XtraBars.Ribbon.RibbonStatusBar __StatusBar;
        DxBarCheckItem __StatusAltTextCheckButton;
        DxBarStaticItem __StatusLabel1;
        DxBarStaticItem __StatusLabel2;
        DxBarButtonItem __StatusCopyButton;
        List<DevExpress.XtraEditors.SimpleButton> __Buttons;
        /// <summary>
        /// Tento control reprezentuje vstupní políčko (_InputText nebo _InputMemo)
        /// </summary>
        DevExpress.XtraEditors.TextEdit __InputControl;
        #endregion
        #region Alternativní text
        /// <summary>
        /// Je zadán standardní nebo alternativní text?
        /// </summary>
        private bool _ExistsAnyText { get { return _ExistsStdText || _ExistsAltText; } }
        /// <summary>
        /// Je zadán standardní a současně alternativní text?
        /// </summary>
        private bool _ExistsBothText { get { return _ExistsStdText && _ExistsAltText; } }
        /// <summary>
        /// Je zadán standardní text?
        /// </summary>
        private bool _ExistsStdText { get { return this.DialogArgs?.StdMessageTextExists ?? false; } }
        /// <summary>
        /// Je zadán alternativní text?
        /// </summary>
        private bool _ExistsAltText { get { return this.DialogArgs?.AltMessageTextExists ?? false; } }
        /// <summary>
        /// Vrátí primární text
        /// </summary>
        /// <param name="allowHtml"></param>
        /// <returns></returns>
        private string _GetPrimaryMessageText(out bool allowHtml)
        {
            allowHtml = false;
            var args = this.DialogArgs;
            if (args == null) return "";

            __CurrentVisibleText = VisibleTextType.None;
            if (_ExistsStdText)
            {
                __CurrentVisibleText = VisibleTextType.Standard;
                allowHtml = args.MessageTextContainsHtml;
                return args.MessageText;
            }
            if (_ExistsAltText)
            {
                __CurrentVisibleText = VisibleTextType.Alternative;
                allowHtml = args.AltMessageTextContainsHtml;
                return args.AltMessageText;
            }
            return "";
        }
        /// <summary>
        /// Vymění zobrazený Message text mezi standardním a alternativním
        /// </summary>
        private void _AlternateCurrentText()
        {
            var args = this.DialogArgs;
            if (args == null) return;

            if (__CurrentVisibleText == VisibleTextType.Alternative && _ExistsStdText)
            {
                __BoundsAltText = this.Bounds;
                __CurrentVisibleText = VisibleTextType.Standard;
                _FillMessageText(args.MessageText, args.MessageTextContainsHtml, true, __BoundsStdText);
                _RefreshAltMsgButtonText();
            }
            else if (__CurrentVisibleText != VisibleTextType.Alternative && _ExistsAltText)
            {
                __BoundsStdText = this.Bounds;
                __CurrentVisibleText = VisibleTextType.Alternative;
                _FillMessageText(args.AltMessageText, args.AltMessageTextContainsHtml, true, __BoundsAltText);
                _RefreshAltMsgButtonText();
            }
        }
        /// <summary>
        /// Daný text vyplní do _MessageLabel.Text a _MessageLabel.AllowHtmlString, volitelně přepočítá Bounds formuláře
        /// </summary>
        /// <param name="text"></param>
        /// <param name="allowHtml"></param>
        /// <param name="recalcLayout"></param>
        /// <param name="boundsUser"></param>
        private void _FillMessageText(string text, bool allowHtml, bool recalcLayout, Rectangle? boundsUser)
        {
            __CurrentMessageText = text;
            __CurrentMessageTextContainsHtml = allowHtml;

            __MessageLabel.Text = text;
            __MessageLabel.AllowHtmlString = allowHtml;
            __MessageMemo.Text = text;
            __IsSmallText = (text.Length <= 80 && !text.Contains('\r'));

            if (recalcLayout)
            {
                __MessageLabel.Refresh();
                Tuple<Rectangle, Size> data = _CalculateOptimalCoordinates(null);
                var oldBounds = this.Bounds;
                var newBounds = data.Item1;
                if (boundsUser.HasValue)
                    this.Bounds = boundsUser.Value;
                else if ((newBounds.Width > oldBounds.Width) || (newBounds.Height > oldBounds.Height))
                    this.Bounds = newBounds;
                this.MinimumSize = data.Item2;
            }
        }
        /// <summary>
        /// Aktualizuje obsah buttonu <see cref="__StatusAltTextCheckButton"/> podle aktuálního stavu v <see cref="__CurrentVisibleText"/> a podle textů v <see cref="DialogArgs"/>.
        /// </summary>
        private void _RefreshAltMsgButtonText()
        {
            if (__StatusAltTextCheckButton == null) return;
            var args = this.DialogArgs;
            if (args == null) return;

            var currTextType = __CurrentVisibleText;

            string buttonText = (currTextType == VisibleTextType.Standard ? (args.StatusBarAltMsgButtonText ?? args.StatusBarStdMsgButtonText) : (args.StatusBarStdMsgButtonText ?? args.StatusBarAltMsgButtonText));
            if (String.IsNullOrEmpty(buttonText)) buttonText = DxComponent.Localize((currTextType == VisibleTextType.Standard ? MsgCode.DialogFormAltMsgButtonText : MsgCode.DialogFormStdMsgButtonText));

            string buttonToolTip = (currTextType == VisibleTextType.Standard ? (args.StatusBarAltMsgButtonTooltip ?? args.StatusBarStdMsgButtonTooltip) : (args.StatusBarStdMsgButtonTooltip ?? args.StatusBarAltMsgButtonTooltip));
            if (String.IsNullOrEmpty(buttonToolTip)) buttonToolTip = DxComponent.Localize((currTextType == VisibleTextType.Standard ? MsgCode.DialogFormAltMsgButtonToolTip : MsgCode.DialogFormStdMsgButtonToolTip));

            bool isChecked = getIsChecked();

            __StatusAltTextCheckButton.Caption = buttonText;
            __StatusAltTextCheckButton.Checked = isChecked;
            __StatusAltTextCheckButton.SetToolTip(buttonText, buttonToolTip);


            // Bude button ve stavu Checked?
            bool getIsChecked()
            {
                switch (args.StatusBarAltButtonType)
                {
                    case DialogAltButtonType.CheckButtonActiveOnAlt:
                        return (currTextType == VisibleTextType.Alternative);
                    case DialogAltButtonType.CheckButtonActiveOnStd:
                        return (currTextType != VisibleTextType.Alternative);
                }
                return false;
            }
        }
        /// <summary>
        /// Uživatel kliknul na status bar button "AltText"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StatusAltTextButton_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _AlternateCurrentText();
        }
        private string __CurrentMessageText;
        private bool __CurrentMessageTextContainsHtml;
        private VisibleTextType __CurrentVisibleText;
        private Rectangle? __BoundsStdText;
        private Rectangle? __BoundsAltText;
        private enum VisibleTextType { None, Standard, Alternative }
        #endregion
        #region Souřadnice a tvorba Layoutu
        /// <summary>
        /// Určí a nastaví souřadnice, kde bude okno zobrazeno.
        /// </summary>
        private void _PrepareInitialBounds()
        {
            // Metoda PrepareInitialBounds() běží jen jedenkrát, ještě před Show() okna, ale po vytvoření všech controlů.
            // Nejprve zde provedeme RefreshLayout(), tam se určí vnitřní souřadnice jednotlivých controlů podle vnějších rozměrů okna (to se provádí i po každém Resize),
            //   a teprve poté zde určíme optimální velikosti vnitřních prvků (podle textů, podle fontu...) 
            //   a podle nich a podle rozdílu mezi vnitřní a vnější velikostí určíme správnou velikost formuláře:

            Size targetSize = new Size(600, 400);
            // this.Size = new Size(600, 400);                                   // Tím vyvoláme this._RefreshLayout(); a to navíc pro defaultní rozměr, který dává výchozí prostor pro controly
            _RefreshLayout(targetSize);


            Tuple<Rectangle, Size> data = _CalculateOptimalCoordinates(targetSize);

            this.Bounds = data.Item1;
            this.StartPosition = FormStartPosition.Manual;
            this.MinimumSize = data.Item2;
        }
        /// <summary>
        /// Vypočítá optimální souřadnice formuláře
        /// </summary>
        /// <returns></returns>
        private Tuple<Rectangle, Size> _CalculateOptimalCoordinates(Size? targetSize)
        {
            DialogArgs args = this.__DialogArgs;

            Size formCurrentSize = this.Size;
            Size stdPanelSize = __StandardPanel.ClientSize;                              // Aktuální prostor pro prvky
            Size formAddSize = formCurrentSize - stdPanelSize;                           // Souhrn velikosti prvků v okně: TitleBar okna, Border okna, StatusBar (výška)...  Tedy suma prvků mezi __StandardPanel a vnějším rozměrem Form.
            Size formMaxSize = this.__InitialMaximumBounds.Size;
            var buttonDockSide = args.ButtonPanelDock;
            bool buttonsDockLeftRight = (buttonDockSide == DockStyle.Left || buttonDockSide == DockStyle.Right);

            Size containerSize = targetSize ?? stdPanelSize;                             // Do této klientské velikosti byl umístěn celý obsah (ikona, message, input, buttony)

            // 1. Určíme, jakou velikost formuláře bychom potřebovali:
            //  a) Ikona/Image
            bool iconVisible = this.__IconVisible;
            Size? iconSize = (iconVisible ? (Size?)this.__IconSize : (Size?)null);

            //  b) Pro Text:
            Size textCurrentSize = this.__MessageInputBounds.Size;
            Size textAddSize = containerSize - textCurrentSize;                          // Počet "okolních" pixelů mezi Containerem a vlastním MessageTextem (= Ikona + Input + Buttony), ale nikoliv režie formuláře (TitleBar a Borders)
            Size textMaxSize = formMaxSize - textAddSize;                                // Když bych Form zvětšil na povolené maximum, tak pro Text bude tato Max velikost
            Size textTestSize = new Size(textMaxSize.Width - 12, textMaxSize.Height);    // Rozměr pro měření textu, s rezervou vpravo
            Size textRealSize = _GetRealTextSize(textMaxSize, textTestSize);             // Text roztažený do Max velikosti (formuláře => textboxu) bude zabírat tuto velikost: tady se projeví "přetečení textu dolů" při zalomení řádků na danou šířku
            float ratio = (float)textRealSize.Width / (float)textRealSize.Height;        // Poměr šířka : výška, měli bychom jej udržet v rozmezí 3.5 : 1 (dost široký) až 1 : 2 (dost úzký).
            float ratioMax = 3.5f;
            int textOptWidth = textMaxSize.Width * 3 / 4;                                // Mezní šířka textu (75% maximální): pokud reálná šířka (tw) bude pod ní, pak nebudeme řešit zmenšení šířky.
            if (ratio > ratioMax && textRealSize.Width >= textOptWidth)
            {   // Máme to moc široké a málo vysoké, zmenšíme šířku a změříme text znovu:
                float ratioDown = ratioMax / ratio;                                      // Hodnota (ratioDown) je menší než 1 = poměr pro zmenšení šířky
                int textTestWidth = (int)(ratioDown * (float)textTestSize.Width);        // Upravená šířka pro text
                if (textTestWidth < textOptWidth) textTestWidth = textOptWidth;          // Neměla by jít pod 75% maximální šířky
                textTestSize.Width = textTestWidth;
                textRealSize = _GetRealTextSize(textMaxSize, textTestSize);              // Upravili jsme šířku, vypočítáme výšku
            }

            //  c) Input: určíme nejmenší šířku okna tak, aby byl dobře vidět Input prvek:
            //            Přičteme výšku pro input text k výšce message textu:
            int? inputWidth = null;                                                      // Vhodná šířka formuláře z pohledu InputTextu
            if (this.__InputVisible)
            {
                // _InputControlSize = vlastní velikost TextBoxu:
                var inputSize = _InputControlSize;
                // ipw obsahuje šířku panelu s Input controlem + pravý okraj uvnitř panelu:
                int ipw = inputSize.Width + _MarginsX;
                // inputWidth obsahuje nejmenší potřebnou šířku formuláře tak, aby se do něj vešel InputBox umístěný na souřadnici X a se svoji požadovanou šířkou:
                inputWidth = this.__CoordinateTextX + ipw;

                // Výška textu += výška mezery + inputu:
                textRealSize.Height = textRealSize.Height + _SpacingInputY + inputSize.Height;
            }

            // Pokud je vidět ikona, a je vyšší než je text, pak výšku textu určím podle výšky ikony:
            if (iconVisible && iconSize.Value.Height > textRealSize.Height)
                textRealSize.Height = iconSize.Value.Height;

            //  d) Určíme velikost z hlediska Buttonů:
            int? buttonWidth = null;                                                     // Vhodná velikost formuláře z pohledu tlačítek;
            int? buttonHeight = null;                                                    //  ...  null = v daném směru velikost neřeším
            if (this.__ButtonsVisible)
            {
                Size buttonCurrentSize = this.__ButtonsBounds.Size;                      // Současný prostor, ve kterém jsou Buttony umístěny
                if (buttonsDockLeftRight && __ButtonsTotalHeight.HasValue)
                {   // Tlačítka jsou vlevo nebo vpravo, a máme v evidenci jejich sumární výšku:
                    int minHeight = 4 * _MarginsY + __ButtonsTotalHeight.Value;  // Minimální vhodná výška panelu tlačítek, aby byly pěkně vidět     |    koeficient 4: součet volného místa nad + pod buttony, aby bylo vidět zarovnání Begin/Center/End
                    // Výška formuláře z hlediska buttonů = potřebná výška pro buttony (minHeight) + stávající "režijní pixely" mezi formulářem a buttony:
                    buttonHeight = minHeight + formAddSize.Height;
                }
                else if (__ButtonsTotalWidth.HasValue)
                {   // Tlačítka jsou dole, a máme v evidenci jejich sumární šířku:
                    int minWidth = 3 * _MarginsX + __ButtonsTotalWidth.Value;            // Minimální vhodná šířka panelu tlačítek, aby byly pěkně vidět     |    koeficient 3: součet volného místa vlevo + vpravo okolo buttonů, aby bylo vidět zarovnání Begin/Center/End
                    // Šířka formuláře z hlediska buttonů = potřebná šířka pro buttony (minWidth) + stávající "režijní pixely" mezi formulářem a buttony:
                    buttonWidth = minWidth + formAddSize.Width;
                }
            }


            // 2. Určíme výslednou velikost formuláře a jeho pozici:
            Size textFormSize = textRealSize + textAddSize + formAddSize;                // Velikost celého formuláře tak, aby text umístěný uvnitř měl svoji optimální velikost (+ režie uvnitř StdContainer + režie uvnitř Form)
            //  a) optimální pro text:
            int formWidth = textFormSize.Width;
            int formHeight = textFormSize.Height;

            //  b) zvětšit pro InputText:
            if (inputWidth.HasValue && formWidth < inputWidth.Value) formWidth = inputWidth.Value;

            //  c) zvětšit pro Buttony:
            if (buttonWidth.HasValue && formWidth < buttonWidth.Value) formWidth = buttonWidth.Value;
            if (buttonHeight.HasValue && formHeight < buttonHeight.Value) formHeight = buttonHeight.Value;

            // 3. Určíme MinSize pro Form:
            Size textMinSize = _CurrentMinTextSize;
            // Pokud je vidět ikona, a je vyšší než je textMinSize, pak výšku textMinSize určím podle výšky ikony:
            if (iconVisible && iconSize.Value.Height > textMinSize.Height)
                textMinSize.Height = iconSize.Value.Height;

            Size formMinSize = textMinSize + textAddSize + formAddSize;                  // Minimální velikost textu + režie uvnitř StdContainer + režie uvnitř Formu
            if (formWidth < formMinSize.Width) formWidth = formMinSize.Width;
            if (formHeight < formMinSize.Height) formHeight = formMinSize.Height;

            // Aplikovat maximální velikost, vycentrovat, a poté zarovnat do daného prostoru:
            Rectangle maxBounds = __InitialMaximumBounds;
            if (formWidth > maxBounds.Width) formWidth = maxBounds.Width;
            if (formHeight > maxBounds.Height) formHeight = maxBounds.Height;

            Point centerPoint = __InitialCenterPoint;
            int formX = centerPoint.X - (formWidth / 2);
            int formY = centerPoint.Y - (formHeight / 2);
            if ((formX + formWidth) > maxBounds.Right) formX = (maxBounds.Right - formWidth);
            if (formX < maxBounds.X) formX = maxBounds.X;
            if ((formY + formHeight) > maxBounds.Bottom) formY = (maxBounds.Bottom - formHeight);
            if (formY < maxBounds.Y) formY = maxBounds.Y;

            // Výsledky:
            Rectangle bounds = new Rectangle(formX, formY, formWidth, formHeight);
            return new Tuple<Rectangle, Size>(bounds, formMinSize);
        }
        /// <summary>
        /// Určí a vrátí reálnou velikost textu, akceptujíc dodané maximální a testovací rozměry:
        /// <see cref="_AddTextHeight"/> ani <see cref="_MinTextHeight"/> ani <see cref="_MinTextInputHeight"/> ani <see cref="_MinTextWidth"/>. 
        /// </summary>
        /// <param name="textMaxSize"></param>
        /// <param name="textTestSize"></param>
        /// <returns></returns>
        private Size _GetRealTextSize(Size textMaxSize, Size textTestSize)
        {
            Size textPreferredSize = _GetTextPreferredSize(textTestSize);       // Text roztažený do Max velikosti (formuláře => textboxu) bude zabírat tuto velikost: tady se projeví "přetečení textu dolů" při zalomení řádků na danou šířku
            int addTextHeight = (__InputVisible ? 0 : _AddTextHeight);
            Size minTextSize = _CurrentMinTextSize;
            int tw = _GetMin(textMaxSize.Width, textPreferredSize.Width, _MinTextWidth);
            int th = _GetMin(textMaxSize.Height, textPreferredSize.Height + addTextHeight, minTextSize.Height);
            return new Size(tw, th);
        }
        /// <summary>
        /// Minimální rozměry pro MessageText v aktuální situaci, včetně Zoomu
        /// </summary>
        private Size _CurrentMinTextSize
        {
            get
            {
                bool inputVisible = this.__InputVisible;
                bool isSmallText = this.__IsSmallText;
                int mtw = _MinTextWidth;
                int mth = (inputVisible ? _MinTextInputHeight : (isSmallText ? _MinTextSmallHeight : _MinTextHeight));
                return new Size(mtw, mth);
            }
        }
        /// <summary>
        /// Metoda vrací optimální rozměr pro text v aktuálním fontu pro danou maximální cílovou velikost.
        /// Tato metoda neřeší <see cref="_AddTextHeight"/> ani <see cref="_MinTextHeight"/> ani <see cref="_MinTextInputHeight"/> ani <see cref="_MinTextWidth"/>. 
        /// To řeší metoda <see cref="_GetRealTextSize(Size, Size)"/>.
        /// <para/>
        /// Tato metoda reaguje na <see cref="__CurrentMessageTextContainsHtml"/>, měří text <see cref="__CurrentMessageText"/> a reaguje na přídavek k velikosti <paramref name="addSize"/>.
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        private Size _GetTextPreferredSize(Size proposedSize, int? addSize = null)
        {
            if (this.FormIsDisposed || __MessageLabel == null) return proposedSize;

            string text = this.__CurrentMessageText;
            bool isHtml = this.__CurrentMessageTextContainsHtml;
            if (isHtml)
                text = _GetPlainTextFromHtml(text);

            var preferredSize = MeasureString(text, __MessageLabel.Appearance.Font, proposedSize);

            if (isHtml)
            {   // Formátovaný text může být větší, ale control to nedetekuje :-( 
                preferredSize.Width = preferredSize.Width * 13 / 10;                // HTML text může být citelně širší
                preferredSize.Height = preferredSize.Height * 12 / 10;              // Výška naskakuje hodně, protože HTML prezentuje CrLf jako dvojřádek :-(
            }
            if (addSize.HasValue && addSize.Value > 0)
            {
                preferredSize.Width += addSize.Value;
                preferredSize.Height += addSize.Value;
            }
            return preferredSize;
        }
        /// <summary>
        /// Vrátí velikost zadaného textu v daném fontu, volitelně zarovnanou do daného prostoru
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="proposedSize"></param>
        /// <returns></returns>
        private Size MeasureString(string text, Font font, Size? proposedSize = null)
        {
            if (proposedSize.HasValue)
                return TextRenderer.MeasureText(text, font, proposedSize.Value);
            else
                return TextRenderer.MeasureText(text, font);
        }
        /// <summary>
        /// Vrátí prostý text extrahovaný z dodaného HTML textu, pozor: dosti primitivně!
        /// </summary>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        private static string _GetPlainTextFromHtml(string htmlText)
        {
            if (String.IsNullOrEmpty(htmlText)) return "";
            if (!htmlText.Contains("<")) return htmlText;

            string text = htmlText
                    .Replace("<br>", "\r\n")
                    .Replace("<BR>", "\r\n")
                    .Replace("<p>", "\r\n")
                    .Replace("<P>", "\r\n");

            while (true)
            {
                int idx1 = text.IndexOf("<", StringComparison.Ordinal);
                if (idx1 < 0) break;
                int idx2 = text.IndexOf(">", idx1 + 1, StringComparison.Ordinal);
                if (idx2 < 0) break;
                text = text.Substring(0, idx1) + text.Substring(idx2 + 1);
            }

            return text;
        }
        /// <summary>
        /// Vrátí tu menší hodnotu z <paramref name="value1"/> a <paramref name="value2"/>, ale pokud by výsledek byl menší než <paramref name="minResult"/>, pak vrátí <paramref name="minResult"/>.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="minResult"></param>
        /// <returns></returns>
        private int _GetMin(int value1, int value2, int minResult)
        {
            int value = (value1 < value2 ? value1 : value2);
            return (value > minResult ? value : minResult);
        }
        /// <summary>
        /// Uspořádá prvky okna tak, aby využily celou disponibilní plochu okna podle pravidel.
        /// </summary>
        private void _RefreshLayout(Size? targetSize)
        {
            if (__StandardPanel == null) return;
            if (this.FormIsDisposed) return;

            DialogArgs args = __DialogArgs;
            if (args is null) return;

            Size clientSize = this.ClientSize;
            int dw = clientSize.Width - _LastClientSize.Width;
            int dh = clientSize.Height - _LastClientSize.Height;
            if ((dw < 3 && dw > -3) || (dh < 3 && dh > -3))
            { /*  Prostor pro breakpoint pro účely debugování změny rozměru ručně myší po malých kouskách ...  */ }

            // Určíme prostor pro jednotlivé prvky a umístíme je tam:
            _CreateLayout(targetSize);
            _ApplyLayoutToControl(__IconBounds, __IconArea);
            _ApplyLayoutToControl(__MessageBounds, __MessagePanel, _ApplyLayoutMessage);
            _ApplyLayoutToControl(__InputBounds, __InputControl, _ApplyLayoutInput);
            _ApplyLayoutToControl(__ButtonsBounds, __ButtonPanel, _ApplyLayoutButtons);

            _LastClientSize = clientSize;
        }
        private Size _LastClientSize;
        /// <summary>
        /// Určí souřadnice prvků v aktuálním prostoru formuláře podle obsahu a pravidel.
        /// </summary>
        private void _CreateLayout(Size? targetSize)
        {
            Rectangle iconBounds = Rectangle.Empty;
            Rectangle messageInputBounds = Rectangle.Empty;
            Rectangle messageBounds = Rectangle.Empty;
            Rectangle inputBounds = Rectangle.Empty;
            Rectangle buttonsBounds = Rectangle.Empty;
            Rectangle expanderBounds = Rectangle.Empty;

            DialogArgs args = __DialogArgs;

            int buttonHeight = _ButtonHeight;
            int marginsX = _MarginsX;
            int marginsY = _MarginsY;
            int marginsB = _MarginsB;
            int buttonsY = _ButtonsHorizontalY;
            int spacingX = _SpacingX;
            int spacingY = _SpacingY;
            int spacingTextX = _SpacingTextX;
            int spacingTextY = _SpacingTextY;

            Size dialogSize = targetSize ?? this.__StandardPanel.ClientSize;   // Prostor pro dialog. Pokud by existoval rozbalený ExpandPanel, pak není součástí StandardPanelu.

            int textX = marginsX;                                              // Souřadnice X pro text v případě, že není přítomna ikona
            int textY = marginsY;                                              // Souřadnice Y pro text v případě, že není přítomna ikona
            bool iconVisible = this.__IconVisible;
            if (iconVisible)
            {   // S ikonou:
                var iconSize = this.__IconSize;
                if (iconSize.HasValue && iconSize.Value.Width > 0)
                {
                    iconBounds = new Rectangle(marginsX, marginsY, iconSize.Value.Width, iconSize.Value.Height);
                    textX = iconBounds.Right + spacingTextX;                   // Souřadnice X pro text v případě, kdy máme ikonu: text od ní bude odsazen o totéž, o kolik je vlevo ikona od kraje okna
                    textY = getTextY(iconBounds);                              // Souřadnice Y pro text (Message) má být taková, aby jednořádkový text byl ve směru Y vystředěn k ikoně, přičemž maximální výška ikony pro tento účel je (32px × Zoom):
                }
            }

            // Pozice MessageTextu a ButtonPanelu:
            Size buttonsDockSize = Size.Empty;                                 // Velikost panelu Buttonů na té zadokované straně
            if (this.__ButtonsVisible)
            {   // S tlačítky:
                var buttons = this.__Buttons;
                if (args.ButtonPanelDock == System.Windows.Forms.DockStyle.Left)
                {   // Tlačítka vlevo [pod ikonou]:
                    int buttonsMaxWidth = buttons.Select(b => b.Width).Max();
                    int bx = 0;
                    int by = (iconVisible ? iconBounds.Bottom + spacingY : 0);
                    int bw = marginsX + buttonsMaxWidth + spacingX;
                    int bh = dialogSize.Height - by;
                    buttonsDockSize.Width = bw;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    // Pokud je zobrazena ikona, pak jí zarovnáme vlevo | na střed buttonů | doprava buttonu?  Řešíme zde, někdy jindy:

                    int tx1 = textX;
                    int tx2 = buttonsBounds.Right + spacingTextX;
                    int tx = (tx1 > tx2 ? tx1 : tx2);
                    int ty = textY;
                    int tw = dialogSize.Width - (marginsX + tx);
                    int th = dialogSize.Height - (textY + marginsY);
                    messageBounds = new Rectangle(tx, ty, tw, th);
                    if (tx > textX) textX = tx;                                // Hodnota textX slouží pro pozicování InputTextu, proto ji musíme udržovat společnou jako messageBounds.X !!!
                }
                else if (args.ButtonPanelDock == System.Windows.Forms.DockStyle.Right)
                {   // Tlačítka vpravo (po celé výšce vpravo):
                    int buttonsMaxWidth = buttons.Select(b => b.Width).Max();
                    int bw = spacingX + buttonsMaxWidth + marginsX;
                    int bx = dialogSize.Width - bw;
                    int by = 0;
                    int bh = dialogSize.Height;
                    buttonsDockSize.Width = bw;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    int tx = textX;
                    int ty = textY;
                    int tw = bx - spacingTextX - tx;
                    int th = dialogSize.Height - (textY + marginsY); ;
                    messageBounds = new Rectangle(tx, ty, tw, th);
                }
                else
                {   // Tlačítka v řadě dole (Green default):
                    int bx = 0;
                    int bh = buttonsY + buttonHeight + marginsB;
                    int by = dialogSize.Height - bh;
                    int bw = dialogSize.Width;
                    buttonsDockSize.Height = bh;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    bool isMessageCentered = (args.MessageHorizontalAlignment == AlignContentToSide.Center || args.AutoCenterSmallText) && textX <= 40;  // Text má být vystředěn v řádku, anebo tak může být:
                    isMessageCentered = (__MessageLabel.Appearance.TextOptions.HAlignment == HorzAlignment.Center);        // Text reálně je vystředěn:
                    int tx = textX;
                    int tr = (isMessageCentered ? textX : marginsX);           // Pokud se text zarovnává vodorovně na střed, pak celý Label musí být zarovnán na střed okna!
                    int ty = textY;
                    int tw = dialogSize.Width - (tx + tr);
                    int th = buttonsBounds.Y - (textY + spacingTextY);
                    messageBounds = new Rectangle(tx, ty, tw, th);
                }
            }
            else
            {   // Bez tlačítek:
                int tx = textX;
                int ty = textY;
                int tw = dialogSize.Width - (marginsX + textX);
                int th = dialogSize.Height - (textY + marginsY);
                messageBounds = new Rectangle(tx, ty, tw, th);
            }
            messageInputBounds = messageBounds;

            // InputBox:
            if (this.__InputVisible)
            {
                Size inputSize = _InputControlSize;                            // Vlastní Control

                // Reálná velikost prostoru potřebného pro zobrazení textu v MessageBounds, daná textem, fontem a šířkou;
                //  výška vlastního textu bude určitě menší, než výška přidělení pro MessageBoounds:
                var minTextSize = _CurrentMinTextSize;

                // Pozice Input Y bude maximálně dole na zmenšeném MessageBoxu, pokud se vejde:
                int ix = messageBounds.X;
                int iyMax = messageBounds.Bottom - inputSize.Height;
                // Pozice Input Y, pokud bude úplně těsně navazovat pod vlastním textem:
                int iyMin = messageBounds.Y + minTextSize.Height + _SpacingInputY;
                // Pozice Input Y reálná = optimálně těsně pod textem (iyMin), ale pokud by to bylo menší než iyMax pak iyMax:
                int iy = (iyMin < iyMax ? iyMin : iyMax);

                // MessageBounds zmenší svoji výšku tak, aby pod ním byl Input Text, ale aby MessageBounds.Height neklesl pod _CurrentMinTextSize:
                int mh = iy - messageBounds.Y;
                if (mh < minTextSize.Height) mh = minTextSize.Height;
                messageBounds.Height = mh;

                // Pokud disponibilní šířka pro Input (je dána šířkou dialogSize) je menší, než požadovaná velikost InputControl, pak šířku Input zmenšíme na disponibilní:
                int iw = inputSize.Width;
                int ir = dialogSize.Width - marginsX;                          // Souřadnice vpravo = šířka prostoru - MarginX
                if (ix + iw > ir) iw = ir - ix;                                // Pokud pravý okraj Input (ix + iw) > souřadnice vpravo (přesahujeme ven), pak zmenšíme šířku
                if (iw < 20) iw = 20;                                          // Zajistíme šířku alespoň 20px

                // Souřadnice Input:
                inputBounds = new Rectangle(ix, iy - _SpacingInputY, iw, inputSize.Height);
            }

            this.__IconBounds = iconBounds;
            this.__CoordinateTextX = textX;
            this.__MessageInputBounds = messageInputBounds;
            this.__MessageBounds = messageBounds;
            this.__InputBounds = inputBounds;
            this.__ButtonsBounds = buttonsBounds;
            this.__ButtonsDockSize = buttonsDockSize;
            this.__ExpanderBounds = expanderBounds;


            int getTextY(Rectangle iconBounds)
            {
                int acceptedHeight = iconBounds.Height;
                int maxHeight = _GetZoom(32);
                if (acceptedHeight > maxHeight) acceptedHeight = maxHeight;
                int centerY = iconBounds.Y + (acceptedHeight / 2);                       // Souřadnice Y středu ikony, a pokud je její výška větší než 32px, pak výška fiktivní ikony 32 pixelové

                var oneLineHeight = __MessageLabel.Appearance.Font.GetHeight();          // Výška jednoho řádku textu, který budeme zarovnávat
                var textDY = (int)(Math.Ceiling(0.6f * oneLineHeight));                  // Počet pixelů textu nad střed ikony, aby byl text na účaří

                return centerY - textDY;
            }
        }
        /// <summary>
        /// Obsahuje požadovanou velikost vstupního controlu (Text/Memo) v pixelech po provedení zoomu.
        /// Neobsahuje Spacing, neprovádí srovnání s okolními prvky. Vychází z argumentu, ze zoomu a z výšky TextBoxu.
        /// </summary>
        private Size _InputControlSize
        {
            get
            {
                DialogArgs args = __DialogArgs;
                bool isOneLine = (args.InputTextType != ShowInputTextType.MemoEdit);
                int iw = 0;
                int ih = 0;
                if (isOneLine)
                {   // Jednořádkový text:
                    if (args.InputTextSize.HasValue)
                        iw = args.InputTextSize.Value.Width;
                    else
                        iw = 200;
                    ih = (this.__InputControl != null ? this.__InputControl.Height : 20);
                }
                else
                {   // Memo:
                    if (args.InputTextSize.HasValue)
                    {
                        iw = args.InputTextSize.Value.Width;
                        ih = args.InputTextSize.Value.Height;
                    }
                    else
                    {
                        iw = 200;
                        ih = 60;
                    }
                }

                // defaulty, stále bez Zoomu:
                iw = (iw < 40 ? 40 : (iw > 800 ? 800 : iw));
                ih = (ih < 18 ? 18 : (ih > 400 ? 400 : ih));

                // Aplikujeme zoom:
                iw = _GetZoom(iw);
                ih = (isOneLine ? ih : _GetZoom(ih));

                return new Size(iw, ih);
            }
        }
        /// <summary>
        /// Aplikuje dané souřadnice na daný control, zajistí jeho Visible, a vyvolá optional akci.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="control"></param>
        /// <param name="applyInnerAction"></param>
        private void _ApplyLayoutToControl(Rectangle bounds, System.Windows.Forms.Control control, Action applyInnerAction = null)
        {
            if (control is null) return;
            if (bounds.Width > 0 && bounds.Height > 0)
            {   // Zobrazím pouze když může být vidět:
                control.Bounds = bounds;
                if (applyInnerAction != null) applyInnerAction();
                if (!control.Visible) control.Visible = true;
            }
            else
            {
                if (control.Visible) control.Visible = false;
            }
        }
        /// <summary>
        /// Aplikuje dané souřadnice na daný control, zajistí jeho Visible, a vyvolá optional akci.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="control"></param>
        /// <param name="applyInnerAction"></param>
        private void _ApplyLayoutToControl(Rectangle bounds, DxImageArea control, Action applyInnerAction = null)
        {
            if (control is null) return;
            if (bounds.Width > 0 && bounds.Height > 0)
            {   // Zobrazím pouze když může být vidět:
                control.Bounds = bounds;
                if (applyInnerAction != null) applyInnerAction();
                if (!control.Visible) control.Visible = true;
            }
            else
            {
                if (control.Visible) control.Visible = false;
            }
        }

        /// <summary>
        /// Uspořádá správně vnitřní prvky pro Message
        /// </summary>
        private void _ApplyLayoutMessage()
        {
            if (this.FormIsDisposed) return;

            Size clientSize = __MessagePanel.ClientSize;

            // Změříme, kolik prostoru potřebuje Label k zobrazení v dané šířce, natěsno:
            Size labelSize = _GetTextPreferredSize(clientSize, 0);

            var hAlign = __DialogArgs.MessageHorizontalAlignment;
            var vAlign = __DialogArgs.MessageVerticalAlignment;

            // Automatické centrování:
            bool autoCenterSmallText = __DialogArgs.AutoCenterSmallText;
            bool isAutoCentered = false;
            if (autoCenterSmallText)
            {
                int fontHeight = ((int)__MessageLabel.Appearance.Font.GetHeight()) + 4;
                if ((labelSize.Height) <= fontHeight)
                {   // Text má výšku odpovídající jednomu řádku textu => vycentrujeme jej svisle:
                    vAlign = AlignContentToSide.Center;
                    if ((labelSize.Width + 4) <= clientSize.Width)
                        // Text je na šířku menší než prostor pro něj => vycentrujeme jej v řádku:
                        hAlign = AlignContentToSide.Center;

                    // Zajistíme vložení správných hodnot do Labelu:
                    var dvAlign = _ConvertVAlignment(vAlign);
                    if (__MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) __MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    var dhAlign = _ConvertHAlignment(hAlign);
                    if (__MessageLabel.Appearance.TextOptions.HAlignment != dhAlign) __MessageLabel.Appearance.TextOptions.HAlignment = dhAlign;

                    __MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;        // Respektuj velikost Bounds
                    if (__MessageLabel.Dock != DockStyle.Fill) __MessageLabel.Dock = DockStyle.Fill;

                    isAutoCentered = true;
                }
            }

            if (!isAutoCentered)
            {   // Pokud jsme necentrovali jednořádkový text automaticky:
                // Musíme zajistit původní požadované centrování v řádku:
                var dhAlign = _ConvertHAlignment(hAlign);
                if (__MessageLabel.Appearance.TextOptions.HAlignment != dhAlign) __MessageLabel.Appearance.TextOptions.HAlignment = dhAlign;

                // Pokud výška labelu (s rezervou) bude menší, než je výška prostoru, a pokud je zarovnání požadované jinak než Top, pak na to reagujeme s pomocí VAlignment:
                if ((labelSize.Height + 6) < clientSize.Height && (vAlign == AlignContentToSide.Center || vAlign == AlignContentToSide.End))
                {   // Text (Label) umístíme do prostoru tak, aby byl v jeho prostoru správně zarovnán ve směru Y:
                    var dvAlign = _ConvertVAlignment(vAlign);
                    if (__MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) __MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    __MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;        // Respektuj velikost Bounds
                    if (__MessageLabel.Dock != DockStyle.Fill) __MessageLabel.Dock = DockStyle.Fill;
                }
                else
                {   // Text je zarovnaný k Begin, anebo je velký => bude Dock = Top a není nutno víc řešit jeho pozici. Případný scrollBar přidá jeho Parent:
                    var dvAlign = VertAlignment.Top;
                    if (__MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) __MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    __MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;    // Podle velikosti textu přetékej dolů
                    if (__MessageLabel.Dock != DockStyle.Top) __MessageLabel.Dock = DockStyle.Top;
                }
            }

            // Toto je zatím Invisible a nepoužité:
            __MessageMemo.Size = clientSize;
        }
        /// <summary>
        /// Uspořádá správně vnitřní prvky pro Input
        /// </summary>
        private void _ApplyLayoutInput()
        {
            if (this.FormIsDisposed) return;

            /*    už není třeba
            Size clientSize = __InputPanel.ClientSize;
            Size controlSize = _InputControlSize;
            int x, y, w, h;
            w = controlSize.Width;
            h = controlSize.Height;
            switch (__DialogArgs.InputTextType)
            {
                case ShowInputTextType.TextBox:
                    x = 0;                                                     // InputBox začíná na relativní souřadnici 0 = tak aby zarovnával pod MessageTextem, pokud ten je zarovnaný doleva
                    h = __InputControl.Height;
                    y = (clientSize.Height - h) / 2;
                    if (y < 0) y = 0;
                    __InputControl.Bounds = new Rectangle(x, y, w, h);
                    break;
                case ShowInputTextType.MemoEdit:
                    x = 0;                                                     // InputBox začíná na relativní souřadnici 0 = tak aby zarovnával pod MessageTextem, pokud ten je zarovnaný doleva
                    y = (clientSize.Height - h) / 2;
                    if (y < 0) y = 0;
                    __InputControl.Bounds = new Rectangle(x, y, w, h);
                    break;
            }
            */
        }
        /// <summary>
        /// Uspořádá správně vnitřní prvky Buttons
        /// </summary>
        private void _ApplyLayoutButtons()
        {
            if (this.FormIsDisposed) return;

            __ButtonsTotalWidth = null;
            __ButtonsTotalHeight = null;
            DialogArgs args = __DialogArgs;
            if (args is null) return;

            if (args.ButtonPanelDock == DockStyle.Left || args.ButtonPanelDock == DockStyle.Right)
                _ApplyLayoutButtonsVertical();
            else
                _ApplyLayoutButtonsHorizontal();
        }
        /// <summary>
        /// Rozmístí buttony ve směru vertikálním
        /// </summary>
        private void _ApplyLayoutButtonsVertical()
        {
            DialogArgs args = __DialogArgs;
            if (args is null) return;

            int buttonHeight = _ButtonHeight;
            int marginsX = _MarginsX;
            int marginsY = _MarginsY;
            int spacingX = _SpacingX;
            int spacingY = _SpacingY;

            var buttons = this.__Buttons;
            Size clientSize = this.__ButtonPanel.ClientSize;
            int clientHeight = clientSize.Height - (2 * marginsY);
            int buttonCount = buttons.Count;
            int sumHeight = (buttonCount * buttonHeight) + (buttonCount - 1) * spacingY;
            __ButtonsTotalHeight = sumHeight;

            int y0 = marginsY + _AlignSizeTo(sumHeight, clientHeight, args.ButtonsAlignment);
            int y = y0;
            bool toRight = (args.ButtonPanelDock == System.Windows.Forms.DockStyle.Right);
            int r = clientSize.Width - marginsX;
            foreach (var button in buttons)
            {
                int w = button.Width;
                int x = (toRight ? r - w : spacingX);
                button.Bounds = new Rectangle(x, y, w, buttonHeight);
                y = y + buttonHeight + spacingY;
            }
        }
        /// <summary>
        /// Rozmístí buttony ve směru horizontálním
        /// </summary>
        private void _ApplyLayoutButtonsHorizontal()
        {
            DialogArgs args = __DialogArgs;
            if (args is null) return;

            int buttonHeight = _ButtonHeight;
            int marginsX = _MarginsX;
            int marginsY = _MarginsY;
            int marginsB = _MarginsB;
            int spacingX = _SpacingX;
            int spacingY = _SpacingY;

            var buttons = this.__Buttons;
            Size clientSize = this.__ButtonPanel.ClientSize;
            int clientWidth = clientSize.Width - (2 * marginsX);
            int buttonCount = buttons.Count;
            int sumWidth = buttons.Select(b => b.Width).Sum() + (buttonCount - 1) * spacingX;
            __ButtonsTotalWidth = sumWidth;

            int x0 = marginsX + _AlignSizeTo(sumWidth, clientWidth, args.ButtonsAlignment);
            int x = x0;
            int y = clientSize.Height - marginsB - buttonHeight;
            foreach (var button in buttons)
            {
                int w = button.Width;
                button.Bounds = new Rectangle(x, y, w, buttonHeight);
                x = x + w + spacingX;
            }
        }
        /// <summary>
        /// Vrátí souřadnici počátku obsahu délky <paramref name="content"/> tak, aby v parentu délky <paramref name="parent"/> byl obsah zarovnán ve stylu <paramref name="align"/>.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="parent"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        private int _AlignSizeTo(int content, int parent, AlignContentToSide align)
        {
            switch (align)
            {
                case AlignContentToSide.Begin: return 0;
                case AlignContentToSide.Center: return (parent - content) / 2;
                case AlignContentToSide.End: return parent - content;
            }
            return 0;
        }
        /// <summary>
        /// Souřadnice středu podkladového formu, tam vystředíme náš Dialog při otevření
        /// </summary>
        private Point __InitialCenterPoint;
        /// <summary>
        /// Maximální souřadnice Formu, do kterých ho můžeme dát v rámci inicializace = 80% aktuálního monitoru
        /// </summary>
        private Rectangle __InitialMaximumBounds;
        /// <summary>
        /// DPI výchozího monitoru, kam se bude okno umisťovat jako výchozí
        /// </summary>
        private int? __InitialDpi;
        /// <summary>
        /// Optimální souřadnice pro Icon z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __IconBounds;
        /// <summary>
        /// Souřadnice X, kde začíná Text a Input
        /// </summary>
        private int __CoordinateTextX;
        /// <summary>
        /// Optimální souřadnice pro text Message plus InputBox z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __MessageInputBounds;
        /// <summary>
        /// Optimální souřadnice pro pouze text Message bez InputBox z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __MessageBounds;
        /// <summary>
        /// Optimální souřadnice pro text Input z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __InputBounds;
        /// <summary>
        /// Optimální souřadnice pro Buttons z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __ButtonsBounds;
        /// <summary>
        /// Optimální souřadnice pro Expander z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        private Rectangle __ExpanderBounds;
        /// <summary>
        /// Jde o malý text: bez CrLf a nejvýše 80 znaků
        /// </summary>
        private bool __IsSmallText;
        /// <summary>
        /// Celková šířka potřebná pro buttony (pouze suma šířek buttonů a jejich vnitřních mezer, nikoli Margins), buttony dokované dolů
        /// </summary>
        private int? __ButtonsTotalWidth;
        /// <summary>
        /// Celková výška potřebná pro buttony (pouze suma výšek buttonů a jejich vnitřních mezer, nikoli Margins), buttony dokované vlevo i vpravo
        /// </summary>
        private int? __ButtonsTotalHeight;
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální šířka buttonu
        /// </summary>
        private int _ButtonMinWidth { get { return _GetZoom(BMW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Maximální šířka buttonu
        /// </summary>
        private int _ButtonMaxWidth { get { return _GetZoom(BXW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Výška buttonu
        /// </summary>
        private int _ButtonHeight { get { return _GetZoom(__DialogArgs.ButtonHeight); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální šířka textu
        /// </summary>
        private int _MinTextWidth { get { return _GetZoom(MTW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je samostatný a větší
        /// </summary>
        private int _MinTextHeight { get { return _GetZoom(MTH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je samostatný a menší
        /// </summary>
        private int _MinTextSmallHeight { get { return _GetZoom(MTSH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je zobrazen i InputPanel
        /// </summary>
        private int _MinTextInputHeight { get { return _GetZoom(MTIH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Povinný přídavek k výšce textu
        /// </summary>
        private int _AddTextHeight { get { return _GetZoom(ATH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Margins X = okraje vnější
        /// </summary>
        private int _MarginsX { get { return _GetZoom(MX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Margins Y = okraje vnější
        /// </summary>
        private int _MarginsY { get { return _GetZoom(MY); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Margins Bottom = okraje dolní, pod Buttony, dané výškou buttonu * (2/3)
        /// </summary>
        private int _MarginsB { get { return _GetZoom(__DialogArgs.ButtonHeight, 0.667f); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing X = mezery vnitřní
        /// </summary>
        private int _SpacingX { get { return _GetZoom(SX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing Y = mezery vnitřní
        /// </summary>
        private int _SpacingY { get { return _GetZoom(SY); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing X okolo textu
        /// </summary>
        private int _SpacingTextX { get { return _GetZoom(STX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing Y okolo textu
        /// </summary>
        private int _SpacingTextY { get { return _GetZoom(STY); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing X okolo InputControlu
        /// </summary>
        private int _SpacingInputX { get { return _GetZoom(SIX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing Y okolo InputControlu
        /// </summary>
        private int _SpacingInputY { get { return _GetZoom(SIY); } }
        /// <summary>
        /// Pozice Y buttonů od svého horního okraje (=MarginTop panel buttonů) pro jejich horizontální rozložení
        /// </summary>
        private int _ButtonsHorizontalY { get { return _GetZoom(BHY); } }
        /// <summary>
        /// Vrátí DevExpress styl pro Horizontální zarovnání
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private HorzAlignment _ConvertHAlignment(AlignContentToSide alignment)
        {
            switch (alignment)
            {
                case AlignContentToSide.Begin: return HorzAlignment.Near;
                case AlignContentToSide.Center: return HorzAlignment.Center;
                case AlignContentToSide.End: return HorzAlignment.Far;
            }
            return HorzAlignment.Default;
        }
        /// <summary>
        /// Vrátí DevExpress styl pro Horizontální zarovnání
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private VertAlignment _ConvertVAlignment(AlignContentToSide alignment)
        {
            switch (alignment)
            {
                case AlignContentToSide.Begin: return VertAlignment.Top;
                case AlignContentToSide.Center: return VertAlignment.Center;
                case AlignContentToSide.End: return VertAlignment.Bottom;
            }
            return VertAlignment.Default;
        }
        /// <summary>
        /// Vrací FontSizeDelta tak, aby výsledný font byl změněn o daný základ plus aktuální Zoom.
        /// </summary>
        /// <returns></returns>
        private int _GetZoomDelta()
        {
            float rootSize = 9f;
            float targetSize = rootSize * __ZoomRatio;
            return (int)Math.Ceiling(targetSize - rootSize);
        }
        /// <summary>
        /// Vrátí počet pixelů přepočtený Zoomem [a volitelně koeficientem]
        /// </summary>
        /// <param name="size"></param>
        /// <param name="coefficient">Koeficient</param>
        /// <returns></returns>
        private int _GetZoom(int size, float? coefficient = null)
        {
            float zoomed = (float)size * __ZoomRatio;
            if (coefficient.HasValue) zoomed = coefficient.Value * zoomed;
            return (int)Math.Ceiling(zoomed);
        }
        /// <summary>
        /// Aktuální ratio Zoom: 1.0 = beze změny, 1.5 = +50%.
        /// Obsahuje hodnotu <see cref="DialogArgs.ZoomRatio"/> = <see cref="DialogArgs.ZoomRatio"/> * <see cref="DialogArgs.DialogZoomRatio"/>.
        /// </summary>
        private float __ZoomRatio;
        #endregion
        #region Výchozí hodnoty
        /// <summary>
        /// Defaultní hodnota pro základní zoom dialogu
        /// </summary>
        internal const float DefaultDialogZoomRatio = 1.00f;
        /// <summary>
        /// Výchozí strana dokování buttonů
        /// </summary>
        internal const DockStyle DefaultButtonDockSide = DockStyle.Bottom;
        /// <summary>
        /// Výchozí hodnota pro zarovnání buttonů
        /// </summary>
        internal const AlignContentToSide DefaultButtonAlignment = AlignContentToSide.Center;
        /// <summary>
        /// Výchozí hodnota pro Stav okna
        /// </summary>
        internal const DialogFormState DefaultFormState = DialogFormState.TopMost | DialogFormState.ShowInTaskbar;
        /// <summary>
        /// Výchozí hodnota pro výšku buttonu. Pozor, i výška se přepočítává Zoomem!
        /// Kdo chce nastavit výšku buttonu explicitně, musí ji do argumentu vložit dělenou Zoomem.
        /// </summary>
        internal const int DefaultButtonHeight = 24;
        /// <summary>
        /// Minimální šířka buttonu
        /// </summary>
        private const int BMW = 100;
        /// <summary>
        /// Maximální šířka buttonu
        /// </summary>
        private const int BXW = 240;
        /// <summary>
        /// Minimální šířka textu
        /// </summary>
        private const int MTW = 200;
        /// <summary>
        /// Minimální výška textu, pokud je větší
        /// </summary>
        private const int MTH = 45;
        /// <summary>
        /// Minimální výška textu, pokud je menší
        /// </summary>
        private const int MTSH = 25;
        /// <summary>
        /// Minimální výška textu, pokud je zobrazen i InputPanel
        /// </summary>
        private const int MTIH = 25;
        /// <summary>
        /// Povinný přídavek k výšce textu
        /// </summary>
        private const int ATH = 8;
        /// <summary>
        /// Margins X = okraje vnější
        /// </summary>
        private const int MX = 12;
        /// <summary>
        /// Margins Y = okraje vnější
        /// </summary>
        private const int MY = 9;
        /// <summary>
        /// Spacing X = mezery vnitřní
        /// </summary>
        private const int SX = 6;
        /// <summary>
        /// Spacing Y = mezery vnitřní
        /// </summary>
        private const int SY = 6;
        /// <summary>
        /// Spacing X okolo textu
        /// </summary>
        private const int STX = 6;
        /// <summary>
        /// Spacing Y okolo textu
        /// </summary>
        private const int STY = 3;
        /// <summary>
        /// Spacing X okolo InputControlu
        /// </summary>
        private const int SIX = 0;
        /// <summary>
        /// Spacing Y okolo InputControlu
        /// </summary>
        private const int SIY = 4;
        /// <summary>
        /// Pozice Y buttonů od svého horního okraje (=MarginTop panel buttonů) pro jejich horizontální rozložení
        /// </summary>
        private const int BHY = 4;
        #endregion
        #region Implementace IEscapeHandler
        /// <summary>
		/// Pokud je instance třídy aktivním controlem v době stisku klávesy Escape, pak dostane řízení do této metody <see cref="IEscapeHandler.HandleEscapeKey()"/>.
		/// Třída může / nemusí interně zareagovat (například se zavřít).
		/// Výstupní hodnota říká: true = já jsem Escape obsloužil, nehledej další control který by to měl zkoušet (false = mě se Escape netýká, najdi si někoho jiného).
		/// </summary>
		/// <returns>true = klávesu Escape jsem vyřešil / false = najdi si někoho jiného</returns>
        public bool HandleEscapeKey()
        {
            // Zkusíme najít tlačítko odpovídající Escape a vyřešit jej:
            ProcessKeyForButtons(Keys.Escape);
            // A volajícímu sdělíme: ANO já jsem to obsloužil:
            return true;
            // Pokud bych vrátil false, pak volající (=desktop klienta) bude hledat další okno, které by zavřel. A to bývá nějaký přehled nebo formulář pod tímto dialogem, 
            //  a to je fakt nesmysl !!!
        }
        #endregion
    }
    #region Argument DialogArgs
    /// <summary>
    /// Data pro okno dialogu
    /// </summary>
    public class DialogArgs
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DialogArgs()
        {
            Init();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            if (!String.IsNullOrEmpty(this.Title)) text += this.Title + ": ";
            if (!String.IsNullOrEmpty(this.MessageText)) text += (this.MessageText.Length <= 80 ? this.MessageText : this.MessageText.Substring(0, 77) + "...") + "; ";
            string delimiter = " ";
            foreach (var button in __Buttons)
            {
                text = text + $"{delimiter}[{button.Text}]";
                delimiter = ", ";
            }
            return text;
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void Init()
        {
            UserZoomRatio = 1f;
            DialogZoomRatio = DialogForm.DefaultDialogZoomRatio;
            StatusBarVisible = false;
            StatusBarButtonsBorderStyles = DevExpress.XtraEditors.Controls.BorderStyles.Default;        // UltraFlat: nemá border;  Default: nemá a je nižší;  Office2003: nemá;   Simple: nemá;  Style3D
            StatusBarCtrlCVisible = false;
            StatusBarCtrlCText = DxComponent.Localize(MsgCode.DialogFormCtrlCText);
            StatusBarCtrlCTooltip = DxComponent.Localize(MsgCode.DialogFormCtrlCToolTip);
            StatusBarCtrlCInfo = DxComponent.Localize(MsgCode.DialogFormCtrlCInfo);
            StatusBarCtrlCImage = null;
            StatusBarStdMsgButtonText = DxComponent.Localize(MsgCode.DialogFormStdMsgButtonText);
            StatusBarStdMsgButtonTooltip = DxComponent.Localize(MsgCode.DialogFormStdMsgButtonToolTip);
            StatusBarAltMsgButtonText = DxComponent.Localize(MsgCode.DialogFormAltMsgButtonText);
            StatusBarAltMsgButtonTooltip = DxComponent.Localize(MsgCode.DialogFormAltMsgButtonToolTip);
            StatusBarAltMsgButtonImage = null;
            StatusBarAltButtonType = DialogAltButtonType.StandardButton;
            ButtonPanelDock = DialogForm.DefaultButtonDockSide;
            ButtonHeight = DialogForm.DefaultButtonHeight;
            ButtonsAlignment = DialogForm.DefaultButtonAlignment;
            FormState = DialogForm.DefaultFormState;
            Buttons = new List<ButtonInfo>();
        }
        /// <summary>
        /// Okno vlastníka
        /// </summary>
        public IWin32Window Owner { get; set; }
        /// <summary>
        /// Titulek okna
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Titulek okna, platný = vhodný do Form.Text (zkrácený, jen první řádek)
        /// </summary>
        public string TitleValid
        {
            get
            {
                string text = this.Title ?? "";
                if (text.Length > 0)
                {
                    text = text.Trim('\r', '\n', ' ', '\t');
                    int index = text.IndexOfAny("\r\n".ToCharArray());
                    if (index > 0)
                        text = text.Substring(0, index);
                    if (text.Length > 120)
                        text = text.Substring(0, 117) + "...";
                }
                return text;
            }
        }
        /// <summary>
        /// Jméno ikonky.
        /// Vhodné ikony jsou v <see cref="ImageName"/>, typicky <see cref="ImageName.DxDialogIconInfo"/>, <see cref="ImageName.DxDialogIconWarning"/>, <see cref="ImageName.DxDialogIconError"/>.
        /// </summary>
        public string IconName { get; set; }
        /// <summary>
        /// Libovolná ikonka
        /// </summary>
        public Image Icon { get; set; }
        /// <summary>
        /// Standardní ikonka.
        /// Poznámka: použitý Enum nepokrývá všechny Systémové ikony.
        /// </summary>
        public System.Windows.Forms.MessageBoxIcon? StandardIcon { get; set; }
        /// <summary>
        /// Systémová ikonka.
        /// Poznámka: tento enum pokrývá všechny standardní ikony, na rozdíl od hodnoty v <see cref="StandardIcon"/>.
        /// </summary>
        public DialogSystemIcon? SystemIcon { get; set; }
        /// <summary>
        /// Velikost ikony, šířka. Akceptovaný rozsah je 16 - 512 px.
        /// Pokud nebude zadána šířka a bude zadána výška, použije se výška pro oba rozměry stejná hodnota. Implicitní je 32px.
        /// </summary>
        public int? IconSizeWidth { get; set; }
        /// <summary>
        /// Velikost ikony, výška. Akceptovaný rozsah je 16 - 256 px.
        /// Pokud nebude zadána výška a bude zadána šířka, použije se šířka pro oba rozměry stejná hodnota. Implicitní je 32px.
        /// </summary>
        public int? IconSizeHeight { get; set; }
        /// <summary>
        /// Vlastní text zprávy, ten smí obsahovat Light-HTML kódy (viz DevExpress: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting).
        /// HTML kódy musí být povoleny v <see cref="MessageTextContainsHtml"/> == true
        /// </summary>
        public string MessageText { get; set; }
        /// <summary>
        /// Text hlášky obsahuje HTML formátování.
        /// Pokud zde bude false, ale v textu budou obsaženy HTML kódy, pak tyto kódy budou čitelně zobrazeny uživateli.
        /// Není zde proveden Autodetect.
        /// </summary>
        public bool MessageTextContainsHtml { get; set; }
        /// <summary>
        /// Alternativní text zprávy, ten smí obsahovat Light-HTML kódy (viz DevExpress: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting).
        /// Alternativní text zprávy se zobrazí po kliknutí na tlačítko ve StatusBaru vlevo dole: "Zobrazit více".
        /// HTML kódy musí být povoleny v <see cref="MessageTextContainsHtml"/> == true
        /// </summary>
        public string AltMessageText { get; set; }
        /// <summary>
        /// Text alternativní hlášky obsahuje HTML formátování.
        /// Alternativní text zprávy se zobrazí po kliknutí na tlačítko ve StatusBaru vlevo dole: "Zobrazit více".
        /// Pokud zde bude false, ale v textu budou obsaženy HTML kódy, pak tyto kódy budou čitelně zobrazeny uživateli.
        /// Není zde proveden Autodetect.
        /// </summary>
        public bool AltMessageTextContainsHtml { get; set; }
        /// <summary>
        /// Obsahuje true pokud je zadán text <see cref="MessageText"/>
        /// </summary>
        public bool StdMessageTextExists { get { return !String.IsNullOrEmpty(MessageText); } }
        /// <summary>
        /// Obsahuje true pokud je zadán text <see cref="AltMessageText"/>
        /// </summary>
        public bool AltMessageTextExists { get { return !String.IsNullOrEmpty(AltMessageText); } }
        /// <summary>
        /// Obsahuje true, pokud má být zobrazen button pro zobrazení alternativního textu (tj. pokud existují zadané oba texty: <see cref="MessageText"/> i <see cref="AltMessageText"/>).
        /// </summary>
        public bool StatusBarAltMsgButtonVisible { get { return (StdMessageTextExists && AltMessageTextExists) && (StatusBarAltButtonType != DialogAltButtonType.None); } }
        /// <summary>
        /// Typ buttonu pro alternativní texty
        /// </summary>
        public DialogAltButtonType StatusBarAltButtonType { get; set; }
        /// <summary>
        /// Message okno může obsahovat alternativní text (<see cref="AltMessageText"/>). Pro jeho zobrazení je připraveno tlačítko ve statusbaru vlevo (dole).
        /// Jeho defaultní text je dán hláškou <see cref="MsgCode.DialogFormAltMsgButtonText"/> (typicky "Zobraz detaily"). V této property je možno text změnit.
        /// Zdejší text je přímo zobrazen, bez lokalizace.
        /// </summary>
        public string StatusBarAltMsgButtonText { get; set; }
        /// <summary>
        /// Tooltip k <see cref="StatusBarAltMsgButtonText"/>
        /// </summary>
        public string StatusBarAltMsgButtonTooltip { get; set; }
        /// <summary>
        /// ImageName pro StatusButton <see cref="StatusBarAltMsgButtonText"/>
        /// </summary>
        public string StatusBarAltMsgButtonImage { get; set; }
        /// <summary>
        /// Message okno může obsahovat alternativní text (<see cref="AltMessageText"/>). Pro jeho zobrazení je připraveno tlačítko ve statusbaru vlevo (dole).
        /// Tlačítko má ve výchozím stavu text dle <see cref="StatusBarAltMsgButtonText"/> (typicky "Zobraz detaily").
        /// Po zobrazení alternativního textu zprávy (typicky detaily) se text tlačítka změní na zdejší text, typicky "Skryj detaily".
        /// Jeho defaultní text je dán hláškou <see cref="MsgCode.DialogFormStdMsgButtonText"/>, v této property je možno text změnit.
        /// Zdejší text je přímo zobrazen, bez lokalizace.
        /// </summary>
        public string StatusBarStdMsgButtonText { get; set; }
        /// <summary>
        /// Tooltip k <see cref="StatusBarStdMsgButtonText"/>
        /// </summary>
        public string StatusBarStdMsgButtonTooltip { get; set; }
        /// <summary>
        /// Vodorovné zarovnání textu hlášky (vlevo, střed, vpravo).
        /// </summary>
        public AlignContentToSide MessageHorizontalAlignment { get; set; }
        /// <summary>
        /// Povolit změnu <see cref="MessageHorizontalAlignment"/> i <see cref="MessageVerticalAlignment"/> na <see cref="AlignContentToSide.Center"/> v případě, 
        /// že text <see cref="MessageText"/> je jednořádkový a menší než Minimální šířka.
        /// Pak vypadá centrovaný text lépe než zarovnaný doleva.
        /// </summary>
        public bool AutoCenterSmallText { get; set; }
        /// <summary>
        /// Svislé zarovnání textu hlášky.
        /// Uplatní se v případě, kdy hláška je malá (na výšku) a formulář aplikuje "zvětšení" formuláře na minimální výšku.
        /// Pak má smysl nastavit například <see cref="MessageVerticalAlignment"/> = <see cref="AlignContentToSide.Center"/>.
        /// </summary>
        public AlignContentToSide MessageVerticalAlignment { get; set; }
        /// <summary>
        /// Specifikace zobrazení okna
        /// </summary>
        public DialogFormState FormState { get; set; }
        /// <summary>
        /// Akce, kterou volá DialogForm v okamžiku svého prvního rozsvícení (FirstShow).
        /// Volající si pak může toto okno (parametr) zařadit do svého WindowManagementu.
        /// Volající může do okna zaháčkovat i svoje eventhandlery.
        /// </summary>
        public Action<Form> DialogFirstShowAction { get; set; }
        /// <summary>
        /// Akce, kterou volá DialogForm pro záznam Debug informace. Null = default = nevolá se nic.
        /// </summary>
        public Action<string> DebugAction { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// Výsledný zoom = <see cref="ZoomRatio"/> = <see cref="UserZoomRatio"/> * <see cref="DialogZoomRatio"/>.
        /// </summary>
        public float ZoomRatio { get { return _UserZoomRatio * _DialogZoomRatio; } }
        /// <summary>
        /// Měřítko nastavené uživatelem pro celou aplikaci. Default = 1.0; povolené rozmezí 0.3f až 5.0f včetně.
        /// Výchozí hodnota je 1.00.
        /// Výsledný zoom = <see cref="ZoomRatio"/> = <see cref="UserZoomRatio"/> * <see cref="DialogZoomRatio"/>.
        /// </summary>
        public float UserZoomRatio { get { return _UserZoomRatio; } set { _UserZoomRatio = (value < 0.3f ? 0.3f : (value > 5.0f ? 5.0f : value)); } }
        private float _UserZoomRatio;
        /// <summary>
        /// Měřítko dialogu nad rámec aplikačního zoomu. Default = 1.0; povolené rozmezí 0.3f až 5.0f včetně.
        /// Výchozí hodnota je 1.15.
        /// Výsledný zoom = <see cref="ZoomRatio"/> = <see cref="UserZoomRatio"/> * <see cref="DialogZoomRatio"/>.
        /// </summary>
        public float DialogZoomRatio { get { return _DialogZoomRatio; } set { _DialogZoomRatio = (value < 0.3f ? 0.3f : (value > 5.0f ? 5.0f : value)); } }
        private float _DialogZoomRatio;
        /// <summary>
        /// Reálná viditelnost status baru. Obsahuje true v případě, kdy je zadáno true do <see cref="StatusBarVisible"/> nebo do <see cref="StatusBarCtrlCVisible"/>,
        /// anebo když je zadán alternativní text a je viditelná odpovídající button, viz <see cref="StatusBarAltMsgButtonVisible"/>.
        /// </summary>
        public bool StatusBarVisibleCurrent { get { return (StatusBarVisible || StatusBarAltMsgButtonVisible || StatusBarCtrlCVisible); } }
        /// <summary>
        /// Požadovaná viditelnost status baru, default = false.
        /// </summary>
        public bool StatusBarVisible { get; set; }
        /// <summary>
        /// Styl borderu pro StatusBar buttony.
        /// Default = <see cref="DevExpress.XtraEditors.Controls.BorderStyles.Default"/>
        /// </summary>
        public DevExpress.XtraEditors.Controls.BorderStyles? StatusBarButtonsBorderStyles { get; set; }
        /// <summary>
        /// Viditelnost buttonu "Ctrl+C = Copy" ve Statusbaru.
        /// Pokud zde bude true, ale v textu <see cref="StatusBarCtrlCText"/> bude prázdný string, pak se text tlačítka získá lokalizací textu "CtrlCText".
        /// Default = false, aplikace musí o zobrazení požádat.
        /// </summary>
        public bool StatusBarCtrlCVisible { get; set; }
        /// <summary>
        /// StatusBar může obsahovat button "Ctrl+C = Copy". Jeho akce je fixní: Clipboard.Copy.
        /// Ve výchozím stavu obsahuje lokalizovanou hlášku dle <see cref="MsgCode.DialogFormCtrlCText"/>.
        /// Viditelnost tlačítka řídí výhradně <see cref="StatusBarCtrlCVisible"/>.
        /// Zdejší text je přímo zobrazen, bez lokalizace.
        /// </summary>
        public string StatusBarCtrlCText { get; set; }
        /// <summary>
        /// Tooltip k <see cref="StatusBarCtrlCText"/>. 
        /// Ve výchozím stavu obsahuje lokalizovanou hlášku dle <see cref="MsgCode.DialogFormCtrlCToolTip"/>.
        /// </summary>
        public string StatusBarCtrlCTooltip { get; set; }
        /// <summary>
        /// Po provedení Ctrl+C se v okně (ve statusbaru) může objevit text typu "Zkopírováno do schránky". Zde má být uveden tento text.
        /// Pokud zde bude empty string, nebud ehláška zobrazena.
        /// </summary>
        public string StatusBarCtrlCInfo { get; set; }
        /// <summary>
        /// ImageName pro StatusButton <see cref="StatusBarCtrlCText"/>
        /// </summary>
        public string StatusBarCtrlCImage { get; set; }
        /// <summary>
        /// Výsledná hodnota dialogu v případě, kdy uživatel zavře okno dialogu křížkem
        /// </summary>
        public object DefaultResultValue { get; set; }
        /// <summary>
        /// Výstupní hodnota dialogu
        /// </summary>
        public object ResultValue { get; set; }
        /// <summary>
        /// Zobrazit vstupní textbox?
        /// </summary>
        public ShowInputTextType InputTextType { get; set; }
        /// <summary>
        /// Velikost vstupu v pixelech. Pokud je null, bude defaultní. Pro <see cref="ShowInputTextType.TextBox"/> bude akceptována pouze šířka.
        /// </summary>
        public Size? InputTextSize { get; set; }
        /// <summary>
        /// Password char, pouze pro Text, nikoli MemoEdit
        /// </summary>
        public char? InputTextPasswordChar { get; set; }
        /// <summary>
        /// Maximální délka vstupního textu; null = neomezeno
        /// </summary>
        public int? InputTextMaxLength { get; set; }
        /// <summary>
        /// Obsah vstupního textu (na výstupu je vložena editovaná hodnota)
        /// </summary>
        public string InputTextValue { get; set; }
        /// <summary>
        /// Informace zobrazená ve Statusbaru při editaci InputTextu
        /// </summary>
        public string InputTextStatusInfo { get; set; }
        /// <summary>
        /// Nápovědný text zobrazený ve vstupním políčku, pokud zadaná hodnota je NULL
        /// </summary>
        public string InputTextEmptyHint { get; set; }
        /// <summary>
        /// Umístění panelu s tlačítky, defaultní je <see cref="System.Windows.Forms.DockStyle.Bottom"/>.
        /// </summary>
        public System.Windows.Forms.DockStyle ButtonPanelDock { get; set; }
        /// <summary>
        /// Výška tlačítka, bude upravena pomocí <see cref="ZoomRatio"/>.
        /// Default = 28; povolené rozmezí 20 až 70.
        /// Kdo chce nastavit výšku buttonu explicitně, musí ji do argumentu vložit dělenou Zoomem.
        /// </summary>
        public int ButtonHeight { get { return __ButtonHeight; } set { __ButtonHeight = (value < 20 ? 20 : (value > 70 ? 70 : value)); } } private int __ButtonHeight;
        /// <summary>
        /// Umístění tlačítek uvnitř panelu
        /// </summary>
        public AlignContentToSide ButtonsAlignment { get; set; }
        /// <summary>
        /// Pole tlačítek. Lze jej naplnit ručně, nebo použít metodu <see cref="PrepareButtons(DialogResult[])"/>.
        /// </summary>
        public List<ButtonInfo> Buttons { get { return __Buttons; } private set { __Buttons = value; } } private List<ButtonInfo> __Buttons;
        /// <summary>
        /// Souřadnice parent okna, do kterého by bylo vhodno dialogové okno vycentrovat.
        /// Pokud bude null, převezme se souřadnice hlavního okna aktuální aplikace.
        /// </summary>
        public Rectangle? CenterInBounds { get; set; }
        #endregion
        #region Tvorba argumentu pro různé příležitosti
        /// <summary>
        /// Vytvoří a vrátí argument pro zobrazení dodané chyby.
        /// </summary>
        /// <param name="exc"></param>
        /// <returns></returns>
        public static DialogArgs CreateForException(Exception exc)
        {
            DialogArgs dialogArgs = new DialogArgs();

            string textLabel = DxComponent.Localize(MsgCode.DialogFormTitlePrefix);
            var info = CreateTextsForException(exc, textLabel, true);

            dialogArgs.Title = DxComponent.Localize(MsgCode.DialogFormTitleError);
            dialogArgs.IconName = ImageName.DxDialogIconError;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.Buttons[0].IsInitialButton = true;
            dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
            dialogArgs.StatusBarCtrlCVisible = true;
            dialogArgs.StatusBarVisible = true;
            dialogArgs.MessageText = info.Item1;
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.AltMessageText = info.Item2;
            dialogArgs.AltMessageTextContainsHtml = true;

            return dialogArgs;
        }
        /// <summary>
        /// Vygeneruje a vrátí informaci o chybách (včetně InnerExceptions) ve dvou formách, do výstupu dává dva Items:
        /// Item1 = první jednodušší text
        /// Item2 = alternativí text, obsahuje StackTrace a oddělené vnořené chyby.
        /// <para/>
        /// Tuple.Item1 = "[1. ]Message (in: class.method())[EOL a tatáž informace z InnerExceptions];
        /// Tuple.Item2 = "Message EOL class.method() EOL StackTrace EOL delimiterline"
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="textLabel"></param>
        /// <param name="withHtml"></param>
        /// <returns></returns>
        public static Tuple<string, string> CreateTextsForException(Exception exc, string textLabel, bool withHtml = false)
        {
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            string line = "".PadRight(100, '-');
            string eol = (withHtml ? "\r\n" : "\r\n");
            if (exc != null)
            {
                if (!String.IsNullOrEmpty(textLabel))
                {
                    if (withHtml)
                    {
                        sb1.Append($"<size=+2>{textLabel}</size>{eol}");
                        sb2.Append($"<size=+2>{textLabel}</size>{eol}");
                        sb2.Append($"{line}{eol}");
                    }
                    else
                    {
                        sb1.Append($"{textLabel}{eol}");
                        sb2.Append($"{textLabel}{eol}");
                        sb2.Append($"{line}{eol}");
                    }
                }

                var ex = exc;
                bool hasInner = (exc.InnerException != null);
                int number = 1;
                while (ex != null)
                {
                    Type type = ex.GetType();
                    bool isTiExc = (type == typeof(System.Reflection.TargetInvocationException) && ex.InnerException != null);
                    bool isAgExc = (type == typeof(System.AggregateException) && ex.InnerException != null);
                    if (!isTiExc && !isAgExc)
                    {
                        string prefix = (!hasInner ? "" : $"{number}. ");
                        string message = ex.Message;
                        string method = (ex.TargetSite != null ? $"{ex.TargetSite.DeclaringType.FullName}.{ex.TargetSite.Name}()" : "");
                        string stack = ex.StackTrace ?? "";

                        if (withHtml)
                        {
                            stack = stack.Replace("\r\n", eol);
                            sb1.Append($"<size=+2>{prefix}<b>{message}</b></size>{eol}");
                            sb2.Append($"<size=+2>{prefix}<b>{message}</b>  ({type.Name})</size>{eol}");
                            sb2.Append($"   <b><size=+1>{type.FullName}</size></b>{eol}");
                            sb2.Append($"<i>{stack}</i>{eol}");
                            sb2.Append($"{line}{eol}");
                        }
                        else
                        {
                            sb1.Append($"{prefix}{message}{eol}");
                            sb2.Append($"{prefix}{message}  ({type.Name}){eol}");
                            sb2.Append($"   {type.FullName}{eol}");
                            sb2.Append($"{stack}{eol}");
                            sb2.Append($"{line}{eol}");
                        }
                        number++;
                    }

                    ex = ex.InnerException;
                }
            }
            return new Tuple<string, string>(sb1.ToString(), sb2.ToString());
        }
        #endregion
        #region Přidávání buttonů
        /// <summary>
        /// Připraví standardní sadu buttonů
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="defaultButton"></param>
        public void PrepareButtons(MessageBoxButtons buttons, MessageBoxDefaultButton? defaultButton = null)
        {
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    PrepareButtons(DialogResult.OK);
                    break;
                case MessageBoxButtons.OKCancel:
                    PrepareButtons(DialogResult.OK, DialogResult.Cancel);
                    break;
                case MessageBoxButtons.AbortRetryIgnore:
                    PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    PrepareButtons(DialogResult.Yes, DialogResult.No, DialogResult.Cancel);
                    break;
                case MessageBoxButtons.YesNo:
                    PrepareButtons(DialogResult.Yes, DialogResult.No);
                    break;
                case MessageBoxButtons.RetryCancel:
                    PrepareButtons(DialogResult.Retry, DialogResult.Cancel);
                    break;
            }

            if (defaultButton.HasValue)
            {
                int defaultIndex = (defaultButton == MessageBoxDefaultButton.Button1 ? 0 :
                                    defaultButton == MessageBoxDefaultButton.Button2 ? 1 :
                                    defaultButton == MessageBoxDefaultButton.Button3 ? 2 : -1);
                if (defaultIndex >= 0 && defaultIndex < this.__Buttons.Count)
                    this.__Buttons[defaultIndex].IsInitialButton = true;
            }
        }
        /// <summary>
        /// Do pole <see cref="Buttons"/> vloží tlačítka odpovídající požadovaným hodnotám.
        /// Pokud pole před voláním této metody obsahovalo nějaká tlačítka, budou zrušena.
        /// </summary>
        /// <param name="results"></param>
        public void PrepareButtons(params DialogResult[] results)
        {
            __Buttons.Clear();
            foreach (var result in results)
                AddButton(result);

            // Specialita: pokud je pouze button OK, pak má i vlastnost Escape:
            if (results.Length == 1 && results[0] == DialogResult.OK)
                this.__Buttons[0].IsEscapeButton = true;
        }
        /// <summary>
        /// Do pole <see cref="Buttons"/> přidá tlačítko odpovídající požadované hodnotě.
        /// Stávající obsah pole neruší.
        /// </summary>
        /// <param name="result"></param>
        public void AddButton(DialogResult result)
        {
            ButtonInfo buttonInfo = new ButtonInfo();
            buttonInfo.Text = LocalizeButtonText(result);
            buttonInfo.ResultValue = result;
            buttonInfo.ActiveKey = GetActiveKeyFrom(buttonInfo.Text, result);
            buttonInfo.IsImplicitButton = (result == DialogResult.OK || result == DialogResult.Yes);
            buttonInfo.IsEscapeButton = (result == DialogResult.Cancel || result == DialogResult.Abort || result == DialogResult.No);
            __Buttons.Add(buttonInfo);
        }
        /// <summary>
        /// Vrátí text do buttonu daného typu
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private string LocalizeButtonText(DialogResult result)
        {
            switch (result)
            {
                case DialogResult.OK: return DxComponent.Localize(MsgCode.DialogFormResultOk);
                case DialogResult.Cancel: return DxComponent.Localize(MsgCode.DialogFormResultCancel);
                case DialogResult.Abort: return DxComponent.Localize(MsgCode.DialogFormResultAbort);
                case DialogResult.Retry: return DxComponent.Localize(MsgCode.DialogFormResultRetry);
                case DialogResult.Ignore: return DxComponent.Localize(MsgCode.DialogFormResultIgnore);
                case DialogResult.Yes: return DxComponent.Localize(MsgCode.DialogFormResultYes);
                case DialogResult.No: return DxComponent.Localize(MsgCode.DialogFormResultNo);
            }
            return DxComponent.Localize(MsgCode.DialogFormResultOk);
        }
        /// <summary>
        /// Vrátí aktivační klávesu, což může být 0-9 anebo A-Z, které následuje v dodaném textu za Ampersandem
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private Keys? GetActiveKeyFrom(string text, DialogResult result)
        {
            if (String.IsNullOrEmpty(text)) return null;
            int index = text.IndexOf('&');
            if (index < 0 || index >= (text.Length - 1)) return null;
            int value = (int)text.ToUpper()[index + 1];
            if ((value >= 48 && value <= 57) ||
                (value >= 65 && value <= 90))
                return (Keys)value;
            return null;
        }
        /// <summary>
        /// Do pole <see cref="Buttons"/> přidá dané tlačítko.
        /// Stávající obsah pole neruší.
        /// Stejně tak lze napsat <see cref="Buttons"/>.Add(button);
        /// </summary>
        /// <param name="buttonInfo"></param>
        public void AddButton(ButtonInfo buttonInfo)
        {
            if (buttonInfo != null)
                __Buttons.Add(buttonInfo);
        }
        /// <summary>
        /// Metoda zajistí, že ve vytvořeném okně bude kliknuto na daný button. Buď ihned, anebo po daném čase.
        /// Pokud je zadán button = null, pak se provede zavření křížkem.
        /// </summary>
        /// <param name="buttonInfo"></param>
        /// <param name="afterMilliseconds"></param>
        public void ClickToButton(ButtonInfo buttonInfo, int? afterMilliseconds = null)
        {
            if (ClickedButton is null)
                throw new ArgumentException($"DialogArgs.ClickToButton error: has not handler for event 'ClickedButton'.");

            if (afterMilliseconds.HasValue && afterMilliseconds.Value > 0)
            {
                int time = (afterMilliseconds.Value < 20 ? 20 : afterMilliseconds.Value);
                WatchTimer.CallMeAfter(() => ClickedButton(this, new TEventArgs<ButtonInfo>(buttonInfo)), time);
            }
            else
            {
                ClickedButton(this, new TEventArgs<ButtonInfo>(buttonInfo));
            }
        }
        /// <summary>
        /// Event, který je vyvolán při aplikačním požadavku na kliknutí na daný button
        /// </summary>
        public event EventHandler<TEventArgs<ButtonInfo>> ClickedButton;
        #endregion
        #region class ButtonInfo
        /// <summary>
        /// Třída popisující jedno tlačítko
        /// </summary>
        public class ButtonInfo
        {
            #region Data
            /// <summary>
            /// Vlastní text zprávy, ten smí obsahovat Light-HTML kódy (viz DevExpress: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting)
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// Text vypisovaný do Statusbaru jako přidaná informace.
            /// Celý obsah infotextu v Statusbaru bude: "<see cref="Text"/> : <see cref="StatusBarText"/>".
            /// </summary>
            public string StatusBarText { get; set; }
            /// <summary>
            /// Ikona tlačítka
            /// </summary>
            public Image Image { get; set; }
            /// <summary>
            /// Jméno obrázku tlačítka. Obrázek generuje metoda dodaná jako parametr v konstruktoru argumentu <see cref="DialogArgs"/>.
            /// </summary>
            public string ImageName { get; set; }
            /// <summary>
            /// Aktivační klávesa tlačítka
            /// </summary>
            public System.Windows.Forms.Keys? ActiveKey { get; set; }
            /// <summary>
            /// Button, který bude mít Focus po otevření okna.
            /// Pokud nebude explicitně určen žádný, vybere se první v řadě.
            /// Pokud jich bude více, vybere se první takto označený.
            /// </summary>
            public bool IsInitialButton { get; set; }
            /// <summary>
            /// Button, který se aktivuje stiskem Ctrl+Enter.
            /// Pokud nebude explicitně určen žádný, klávesa neprovede nic.
            /// Pokud jich bude více, vybere se první takto označený.
            /// </summary>
            public bool IsImplicitButton { get; set; }
            /// <summary>
            /// Button, který se aktivuje stiskem Escape.
            /// Pokud nebude explicitně určen žádný, klávesa neprovede nic.
            /// Pokud jich bude více, vybere se první takto označený.
            /// </summary>
            public bool IsEscapeButton { get; set; }
            /// <summary>
            /// Titulek Tooltipu
            /// </summary>
            public string ToolTipTitle { get; set; }
            /// <summary>
            /// Text ToolTipu
            /// </summary>
            public string ToolTipText { get; set; }
            /// <summary>
            /// Výsledná hodnota dialogu, pokud bude stisknuto toto tlačítko
            /// </summary>
            public object ResultValue { get; set; }
            /// <summary>
            /// Libovolná data, která používá aplikace
            /// </summary>
            public object Tag { get; set; }
            #endregion
            #region Kliknutí na button
            /// <summary>
            /// Událost, když bylo kliknuto na button. Pokud nebude naplněna, pak button svoje hodnoty předá do formuláře a formulář bude zavřen.
            /// </summary>
            public event DialogArgsButtonClickHandler ButtonClicked;
            /// <summary>
            /// Háček pro potomky
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            protected virtual void OnButtonClicked(object sender, DialogArgsButtonClickArgs e) { }
            /// <summary>
            /// Bylo kliknuto na tento button. Je předán formulář.
            /// </summary>
            /// <param name="dialogForm"></param>
            internal void OnButtonClicked(DialogForm dialogForm)
            {
                var args = new DialogArgsButtonClickArgs();

                // Háčky a události:
                this.OnButtonClicked(dialogForm, args);
                this.ButtonClicked?.Invoke(dialogForm, args);

                // Pokud nikdo nezakázal pokračování, pak zdejší hodnoty vepíšeme do formuláře a týž zavřeme:
                if (!args.Cancel)
                {
                    dialogForm.DialogFormResult = this.ResultValue;
                    dialogForm.DialogResult = DialogResult.OK;
                    dialogForm.Close();
                }
            }
            #endregion
        }
        #endregion
        #region Lokalizace





        /*
        /// <summary>
        /// Funkce, která provádí překlady textů do aktuálního jazyka.
        /// Vstupem je kód hlášky, výstupem její text.
        /// </summary>
        public Func<string, string> TextLocalizer;
        /// <summary>
        /// Funkce, která vrátí Image pro ikonu podle jejího jména souboru (<see cref="IconFile"/>).
        /// </summary>
        public Func<string, Image> IconGenerator;
        /// <summary>
        /// Vrátí text pro button daného druhu.
        /// Pokud existuje funkce <see cref="TextLocalizer"/>, použije ji.
        /// </summary>
        /// <param name="dialogResult"></param>
        /// <returns></returns>
        private string GetButtonTextFor(DialogResult dialogResult)
        {
            string value = dialogResult.ToString();
            string code = DialogForm.MsgCode_DialogResultPrefix + value;
            return GetLocalizedText(code);
        }
        /// <summary>
        /// Vrátí lokalizovaný text pro daný kód
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        internal string GetLocalizedText(string code)
        {
            if (String.IsNullOrEmpty(code)) return code;

            if (TextLocalizer != null)
            {
                string text = TextLocalizer(code);
                if (text != null) return text;
            }

            string key = code.Trim();
            switch (key)
            {
                case DialogForm.MsgCode_CtrlCText: return "Ctrl+C";
                case DialogForm.MsgCode_CtrlCTooltip: return "Zkopíruje do schránky Windows celý text tohoto okna (titulek, informaci i texty tlačítek).";
                case DialogForm.MsgCode_CtrlCInfo: return "Zkopírováno do schránky";
            }

            if (key.StartsWith(DialogForm.MsgCode_DialogResultPrefix))
            {
                key = key.Substring(DialogForm.MsgCode_DialogResultPrefix.Length).ToLower();
                switch (key)
                {
                    case "ok": return "OK";
                    case "cancel": return "Storno";
                    case "abort": return "Abort";
                    case "retry": return "Znovu";
                    case "ignore": return "Ignoruj";
                    case "yes": return "Ano";
                    case "no": return "Ne";
                }
            }

            return code;
        }
        */
        #endregion
    }
    #endregion
    #region Delegáti, argumenty, enumy
    /// <summary>
    /// Předpis pro callback handler pro nemodální okno
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DialogFormClosingHandler(object sender, DialogFormClosingArgs args);
    /// <summary>
    /// Argumenty předávané do callback při nemodálním zobrazení
    /// </summary>
    public class DialogFormClosingArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dialogArgs"></param>
        public DialogFormClosingArgs(DialogArgs dialogArgs)
        {
            this.DialogArgs = dialogArgs;
            this.Cancel = false;
        }
        /// <summary>
        /// Argumenty, pro které je okno vytvořeno.
        /// Pokud jde o vstupní okno pro zadání textu, pak v argumentu v <see cref="DialogArgs.InputTextValue"/> je již text zadaný uživatelem.
        /// Výstupní hodnota dialogu je v 
        /// </summary>
        public DialogArgs DialogArgs { get; private set; }
        /// <summary>
        /// Aplikace může zakázat zavření okna nastavením <see cref="Cancel"/> = true;
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Typ buttonu pro alternativní texty
    /// </summary>
    public enum DialogAltButtonType
    {
        /// <summary>
        /// Žádný: Visible = false
        /// </summary>
        None,
        /// <summary>
        /// Standardní: vždy běžný button, bez stavu Checked
        /// </summary>
        StandardButton,
        /// <summary>
        /// CheckButton, který je "Aktivní" při zobrazeném textu Standard = upozorňuje, že lze zobrazit i Alternativní text
        /// </summary>
        CheckButtonActiveOnStd,
        /// <summary>
        /// CheckButton, který je "Aktivní" při zobrazeném textu Alternativní = signalizuje, že právě vidíme Alternativní text
        /// </summary>
        CheckButtonActiveOnAlt
    }
    /// <summary>
    /// Systémové ikony
    /// </summary>
    public enum DialogSystemIcon
    {
        /// <summary>Žádná ikona</summary>
        None,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Application"/></summary>
        Application,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Asterisk"/></summary>
        Asterisk,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Error"/></summary>
        Error,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Exclamation"/></summary>
        Exclamation,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Hand"/></summary>
        Hand,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Information"/></summary>
        Information,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Question"/></summary>
        Question,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Warning"/></summary>
        Warning,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.WinLogo"/></summary>
        WinLogo,
        /// <summary>Reprezentuje systémovou ikonu <see cref="SystemIcons.Shield"/></summary>
        Shield
    }
    /// <summary>
    /// Zobrazit vstupní políčko pro zadání textu? Jakého typu...
    /// </summary>
    public enum ShowInputTextType
    {
        /// <summary>
        /// Žádný vstup (default)
        /// </summary>
        None,
        /// <summary>
        /// Jednořádkový textbox
        /// </summary>
        TextBox,
        /// <summary>
        /// Víceřádkový editbox
        /// </summary>
        MemoEdit
    }
    /// <summary>
    /// Režim okna
    /// </summary>
    [Flags]
    public enum DialogFormState
    {
        /// <summary>
        /// Bez specialit
        /// </summary>
        None = 0,
        /// <summary>
        /// Ponechat okno jako TopMost
        /// </summary>
        TopMost = 0x01,
        /// <summary>
        /// Zobrazovat okno v Windows.Taskbar
        /// </summary>
        ShowInTaskbar = 0x02
    }

    /// <summary>
    /// 
    /// </summary>
    public class DialogArgsButtonClickArgs : CancelEventArgs
    {

    }
    /// <summary>
    /// Předpis pro handler události Bylo kliknuto na button <see cref="DialogArgs.ButtonInfo"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DialogArgsButtonClickHandler(object sender, DialogArgsButtonClickArgs args);
    public static class DpiMonitorUtil
    {
        private enum Monitor_DPI_Type : int
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2
        }

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }

        // Fallback via GetDC / GetDeviceCaps
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        /// <summary>
        /// Vrátí DPI (dpiX, dpiY) pro daný Screen (např. Screen.PrimaryScreen).
        /// Používá GetDpiForMonitor (Windows 8.1+), jinak fallback na GetDeviceCaps.
        /// </summary>
        public static (float dpiX, float dpiY) GetDpiForScreen(Screen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            // vezmeme bod uprostřed obrazovky a dle něj najdeme HMONITOR
            Rectangle b = screen.Bounds;
            var pt = new POINT(b.Left + b.Width / 2, b.Top + b.Height / 2);
            IntPtr hmon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            try
            {
                // pokus o per-monitor DPI (shcore.dll)
                uint dpiX, dpiY;
                int hr = GetDpiForMonitor(hmon, Monitor_DPI_Type.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                if (hr == 0) // S_OK
                    return (dpiX, dpiY);
            }
            catch (DllNotFoundException)
            {
                // shcore.dll nemusí být dostupný -> fallback níže
            }
            catch
            {
                // jiná chyba -> fallback
            }

            // Fallback: systémové DPI (může být pouze systémové, ne per-monitor)
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                try
                {
                    int x = GetDeviceCaps(hdc, LOGPIXELSX);
                    int y = GetDeviceCaps(hdc, LOGPIXELSY);
                    return (x, y);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }

            // úplný fallback
            return (96f, 96f);
        }
    }
    #endregion
}
