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
using Noris.Clients.Win.Components.AsolDX;

namespace Noris.Clients.Win.Components
{
    /// <summary>
    /// Univerzální dialogové okno
    /// </summary>
    public class DialogForm : DevExpress.XtraEditors.XtraForm, IEscapeKeyHandler
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
                form.DialogResult = DialogResult.None;
                form.DialogFormResult = dialogFormResult;
                form.CreateByArgs(args);
                form.CloseAction = null;
                form.WriteDebug($"MessageBox.ShowDialog: {args}");

                // Owner okno vyjmu z argumentu, aby tam nezůstala reference na něj (= pak by se okno bránilo zavření!)
                var owner = args.Owner;
                args.Owner = null;
                form.ShowDialog(owner);

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
            form.DialogResult = DialogResult.None;
            form.DialogFormResult = dialogFormResult;
            form.CreateByArgs(args);
            form.CloseAction = closeAction;
            form.WriteDebug($"MessageBox.Show: {args}");

            // Owner okno vyjmu z argumentu, aby tam nezůstala reference na něj (= pak by se okno bránilo zavření!)
            var owner = args.Owner;
            args.Owner = null;
            form.Show(owner);                              // Modální zobrazení = řízení se vrací ihned, okno bude zobrazeno uživateli asynchronně
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private DialogForm()
        {
            InitializeComponent();
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.BoundsStdText = null;
            this.BoundsAltText = null;
            _Buttons = new List<DevExpress.XtraEditors.SimpleButton>();
            ZoomRatio = 1f;
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
            this.WriteDebug($"MessageBox.OnShown: {_DialogArgs}");
            this.DialogForm_Shown();
        }
        /// <summary>
        /// Při aktivaci okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.WriteDebug($"MessageBox.OnActivated: {_DialogArgs}");
        }
        /// <summary>
        /// Při deaktivaci okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.WriteDebug($"MessageBox.OnDeativated: {_DialogArgs}");
        }
        /// <summary>
        /// Při vstupu focusu do okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.WriteDebug($"MessageBox.OnGotFocus: {_DialogArgs}");
        }
        /// <summary>
        /// Při odchodu focusu z okna: píšeme debug
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            this.WriteDebug($"MessageBox.OnLostFocus: {_DialogArgs}");
        }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.RefreshLayout();
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
                handled = ProcessKeyForClipboard(keyData);
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
            this.WriteDebug($"MessageBox.OnClosing: {_DialogArgs}");
            _DialogArgs.ResultValue = this.DialogFormResult;
            if (this.CloseAction != null)
            {
                DialogFormClosingArgs args = new DialogFormClosingArgs(_DialogArgs);
                this.CloseAction(this, args);
                e.Cancel = args.Cancel;
            }
        }
        /// <summary>
        /// Není to Event (ten je multitarget), je to prostá akce, která byla předaná do konstruktoru v metodě Nemodální Show.
        /// </summary>
        private DialogFormClosingHandler CloseAction;
        /// <summary>
        /// Zkusí poslat daný string do debug containeru, pokud je specifikován v metodě <see cref="DialogArgs.DebugAction"/>.
        /// </summary>
        /// <param name="text"></param>
        private void WriteDebug(string text)
        {
            var debugAction = _DialogArgs?.DebugAction;
            if (debugAction != null) debugAction(text);
        }
        #endregion
        #region Public data
        /// <summary>
        /// Definice okna dialogu
        /// </summary>
        public DialogArgs DialogArgs { get { return _DialogArgs; } }
        #endregion
        #region Interaktivita
        /// <summary>
        /// Při zobrazení = zajistíme focus
        /// </summary>
        private void DialogForm_Shown()
        {   // Zajistíme TopMost pozici při zobrazení:
            if (!_IsShown)
                this.FirstShown();
        }
        /// <summary>
        /// První zobrazení řeší TopMost a Focus
        /// </summary>
        private void FirstShown()
        {
            _IsShown = true;
            this.TopMost = true;
            this.WriteDebug($"MessageBox.FirstShown: {_DialogArgs}");

            //       this.Focus();

            // Focus do vhodného controlu:
            if (this.InputVisible)
            {
                _InputControl.Focus();
            }
            else if (this.ButtonsVisible)
            {
                Control initialButton = this._Buttons.FirstOrDefault(b => b.Tag is DialogArgs.ButtonInfo buttonInfo && buttonInfo.IsInitialButton);
                if (initialButton is null)
                    initialButton = this._Buttons[0];

                initialButton.Focus();
            }

            var formState = _DialogArgs.FormState;
            this.ShowInTaskbar = formState.HasFlag(DialogFormState.ShowInTaskbar);
            if (!formState.HasFlag(DialogFormState.TopMost))
                this.TopMost = false;

            this.WriteDebug($"MessageBox.Activating: {_DialogArgs}");
            this.Activate();
            this.WriteDebug($"MessageBox.Activated: {_DialogArgs}");

            // Událost:
            if (_DialogArgs.DialogFirstShowAction != null)
                _DialogArgs.DialogFirstShowAction(this);
        }
        private bool _IsShown = false;

        /// <summary>
        /// Zpracuje stisknutou klávesu jako aktivaci Buttonu
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private bool ProcessKeyForButtons(Keys keyData)
        {
            Control button = null;
            Keys modifiers = keyData & Keys.Modifiers;
            Keys keyCode = keyData ^ modifiers;
            if (modifiers == Keys.Control && keyCode == Keys.Enter)
                button = FindButton(b => b.IsImplicitButton);
            else if (modifiers == Keys.None && keyCode == Keys.Escape)
                button = FindButton(b => b.IsEscapeButton);
            else
                button = FindButton(b => IsButtonActivatedByKey(b, keyData));

            if (button == null) return false;

            button.Focus();
            Button_Click(button, EventArgs.Empty);
            return true;
        }
        /// <summary>
        /// Najde button podle podmínky
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private Control FindButton(Func<DialogArgs.ButtonInfo, bool> filter)
        {
            if (_Buttons == null || _Buttons.Count == 0) return null;
            return _Buttons.FirstOrDefault(b => b.Tag is DialogArgs.ButtonInfo buttonInfo && filter(buttonInfo));
        }
        /// <summary>
        /// Vrátí true, pokud daný button je aktivován danou klávesou
        /// </summary>
        /// <param name="buttonInfo"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private bool IsButtonActivatedByKey(DialogArgs.ButtonInfo buttonInfo, Keys keyData)
        {
            if (!buttonInfo.ActiveKey.HasValue) return false;
            return (buttonInfo.ActiveKey.Value == keyData);
        }
        /// <summary>
        /// Zpracuje Ctrl+C
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        private bool ProcessKeyForClipboard(Keys keyData)
        {
            Keys modifiers = keyData & Keys.Modifiers;
            Keys keyCode = keyData ^ modifiers;
            if (modifiers == Keys.Control && keyCode == Keys.C)
            {
                ClipboardCopy();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Zkopíruje text z okna do schránky
        /// </summary>
        private void ClipboardCopy(bool asImage = false)
        {
            var args = _DialogArgs;
            string copyOk = args.StatusBarCtrlCInfo;
            bool showCopyOk = _StatusBar.Visible && !String.IsNullOrEmpty(copyOk);

            if (!asImage)
            {   // Text
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrEmpty(args.Title))
                {
                    sb.AppendLine(args.Title);
                    sb.AppendLine("".PadRight(args.Title.Length, '='));
                }
                string text = CurrentMessageText;
                if (CurrentMessageTextContainsHtml)
                    text = ConvertFormat.HtmlToText(text, true);
                sb.AppendLine(text);

                string buttonsLine = "";
                string buttonsText = "";
                string space = "    ";
                foreach (var button in args.Buttons)
                {
                    string buttonText = button.Text ?? "";
                    int length = buttonText.Length + 4;
                    buttonsLine += "".PadRight(length, '-') + space;
                    buttonsText += "[ " + buttonText + " ]" + space;
                }
                sb.AppendLine(buttonsLine.Trim());
                sb.AppendLine(buttonsText.Trim());
                sb.AppendLine(buttonsLine.Trim());

                try
                {
                    System.Windows.Forms.Clipboard.Clear();
                    System.Windows.Forms.Clipboard.SetText(sb.ToString());
                    if (showCopyOk) ShowStatus2(copyOk);
                }
                catch { }
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
                    if (showCopyOk) ShowStatus2(copyOk);
                }
                catch { }
            }
        }
        private void InputControl_MouseEnter(object sender, EventArgs e)
        {
            ShowStatus1InputInfo();
        }
        private void InputControl_MouseLeave(object sender, EventArgs e)
        {
            ShowStatus1Current();
        }
        private void InputControl_Enter(object sender, EventArgs e)
        {
            ActiveInputControl = true;
            ActiveButtonInfo = null;
            ShowStatus1InputInfo();
        }
        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
            {
                ShowStatus1(buttonInfo);
            }
        }
        private void Button_MouseLeave(object sender, EventArgs e)
        {
            ShowStatus1(ActiveButtonInfo);
        }
        private void Button_Enter(object sender, EventArgs e)
        {
            if (TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
            {
                ActiveInputControl = false;
                ActiveButtonInfo = buttonInfo;
                ShowStatus1(buttonInfo);
            }
        }
        /// <summary>
        /// Uživatel kliknul na status bar button "COPY"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusCopyButton_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ClipboardCopy();
        }
        /// <summary>
        /// Do statusbaru zobrazí info text k aktuálnímu prvku (<see cref="ActiveButtonInfo"/> nebo <see cref="ActiveInputControl"/>).
        /// </summary>
        private void ShowStatus1Current()
        {
            if (ActiveButtonInfo != null)
                ShowStatus1(ActiveButtonInfo);
            else if (ActiveInputControl)
                ShowStatus1InputInfo();
            else
                ShowStatus1("");

        }
        /// <summary>
        /// Zobrazí info o daném buttonu do Status1, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        /// <param name="buttonInfo"></param>
        private void ShowStatus1(DialogArgs.ButtonInfo buttonInfo)
        {
            if (!_StatusBar.Visible) return;
            string text = "";
            if (buttonInfo != null)
            {
                // Specialita: pokud máme jen jeden button, a ten nemá svůj StatusBarText, a nemám InputText, pak do statusbaru nic nepíšeme.
                // Protože: pokud mám jen button OK, pak by ve status baru svítilo "OK" bez možnosti změny:
                bool hasStatusBarText = !String.IsNullOrEmpty(buttonInfo.StatusBarText);
                bool hideText = (this._Buttons.Count <= 1 && !hasStatusBarText && !this.InputVisible);

                // Požadavek: pokud button neobsahuje přidanou informaci (StatusBarText), pak nevypisovat nic:
                if (!hasStatusBarText)
                    hideText = true;

                if (!hideText)
                    text = buttonInfo.Text + (hasStatusBarText ? text += " : " + buttonInfo.StatusBarText : "");
            }
            ShowStatus1(text);
        }
        /// <summary>
        /// Zobrazí jako hlavní text ve statusbaru informaci pro InputText, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        private void ShowStatus1InputInfo()
        {
            ShowStatus1(_DialogArgs.InputTextStatusInfo);
        }
        /// <summary>
        /// Zobrazí dané info jako hlavní text ve statusbaru do Status1, současně zhasne Status2 (tam je pouze CopyOK)
        /// </summary>
        /// <param name="text"></param>
        private void ShowStatus1(string text)
        {
            StatusLabel1.Caption = text;
            StatusLabel1.Refresh();

            StatusLabel2.Caption = "";
            StatusLabel2.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;

            _StatusBar.Refresh();
        }
        /// <summary>
        /// Zobrazí daný text do Status2
        /// </summary>
        /// <param name="text"></param>
        private void ShowStatus2(string text)
        {
            if (!_StatusBar.Visible) return;
            StatusLabel2.Caption = text;

            if (StatusLabel2.Visibility != DevExpress.XtraBars.BarItemVisibility.Always)
                StatusLabel2.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;

            StatusLabel2.Refresh();
            _StatusBar.Refresh();
        }
        /// <summary>
        /// Po kliknutí na kterýkoli result button dialogu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, EventArgs e)
        {
            if (TryGetButtonInfo(sender, out DialogArgs.ButtonInfo buttonInfo))
            {
                this.DialogFormResult = buttonInfo.ResultValue;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        /// <summary>
        /// Zkusí najít <see cref="DialogArgs.ButtonInfo"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="buttonInfo"></param>
        /// <returns></returns>
        private bool TryGetButtonInfo(object control, out DialogArgs.ButtonInfo buttonInfo)
        {
            buttonInfo = null;
            DevExpress.XtraEditors.BaseButton button = control as DevExpress.XtraEditors.BaseButton;
            if (button is null) return false;
            buttonInfo = button.Tag as DialogArgs.ButtonInfo;
            return (!(buttonInfo is null));
        }
        /// <summary>
        /// true pokud je focus ve vstupním prvku
        /// </summary>
        private bool ActiveInputControl;
        /// <summary>
        /// Info o buttonu, který má nyní focus
        /// </summary>
        private DialogArgs.ButtonInfo ActiveButtonInfo;
        /// <summary>
        /// Status bar bude zobrazen?
        /// Zde už je vyhodnocena reálná situace podle požadavků a podle reálného obsahu dat.
        /// </summary>
        private bool StatusBarVisible;
        /// <summary>
        /// Výsledná hodnota dialogu
        /// </summary>
        public object DialogFormResult { get; private set; }
        #endregion
        #region Vytváření Controlů podle argumentu
        /// <summary>
        /// Podle dodaných argumentů vytvoří obsah okna
        /// </summary>
        /// <param name="args"></param>
        private void CreateByArgs(DialogArgs args)
        {
            StoreArgs(args);
            CreateControls();
            PrepareInitialBounds();
        }
        /// <summary>
        /// Uloží argument a základní data z něj
        /// </summary>
        /// <param name="args"></param>
        private void StoreArgs(DialogArgs args)
        {
            _DialogArgs = args;
            this.Text = args.Title;
            this.ZoomRatio = args.ZoomRatio;
            this.DialogFormResult = args.DefaultResultValue;
            this.StatusBarVisible = (args.StatusBarVisible || !String.IsNullOrEmpty(args.StatusBarCtrlCText) || args.StatusBarCtrlCVisible || args.Buttons.Any(b => !String.IsNullOrEmpty(b.StatusBarText)));

            WriteDebug($"MessageBox.Initializing: {args}");
        }
        /// <summary>
        /// Podle dodaných argumentů vytvoří obsah okna
        /// </summary>
        private void CreateControls()
        {
            DialogArgs args = _DialogArgs;

            InitParentArea(args);

            using (var graphics = this.CreateGraphics())
            {
                CreateFrames(args, graphics);
                CreateIcon(args, graphics);
                CreateMessageControls(args, graphics);
                CreateInputText(args, graphics);
                CreateButtons(args, graphics);
                CreateStatus(args, graphics);
            }
        }
        /// <summary>
        /// Určí vizuální prostor pro dialogové okno (<see cref="_InitialMaximumBounds"/> a <see cref="_InitialCenterPoint"/>.
        /// </summary>
        /// <param name="args"></param>
        private void InitParentArea(DialogArgs args)
        {
            Rectangle? centerInBounds = null;
            if (args != null)
                centerInBounds = args.CenterInBounds;


            if (!centerInBounds.HasValue || (centerInBounds.HasValue && (centerInBounds.Value.Width < 480 || centerInBounds.Value.Height < 360)))
            {
                centerInBounds = null;

                Form ownerForm = args?.Owner as Form;
                if (ownerForm != null)
                    centerInBounds = ownerForm.Bounds;

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
                                centerInBounds = form.Bounds;
                        }
                    }
                    catch { }
                }

                if (!centerInBounds.HasValue)
                    centerInBounds = Screen.PrimaryScreen.WorkingArea;
            }

            Rectangle b = centerInBounds.Value;
            Point centerPoint = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);

            Screen targetScreen = Screen.FromPoint(centerPoint);
            Rectangle targetBounds = targetScreen.WorkingArea;
            int dx = targetBounds.Width / 16;
            int dy = targetBounds.Height / 16;
            Rectangle maximumBounds = new Rectangle(targetBounds.X + 2 * dx, targetBounds.Y + dy, 12 * dx, 14 * dy);

            _InitialCenterPoint = centerPoint;
            _InitialMaximumBounds = maximumBounds;
        }
        /// <summary>
        /// Vytvoří základní panely
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        private void CreateFrames(DialogArgs args, Graphics graphics)
        {
            var panel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, TabStop = false, Dock = DockStyle.Fill, Name = "_StandardPanel" };
            panel.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            _StandardPanel = panel;
            this.Controls.Add(_StandardPanel);

            var ribbon = new DevExpress.XtraBars.Ribbon.RibbonControl();                 // Musí existovat
            var status = new DevExpress.XtraBars.Ribbon.RibbonStatusBar() { Dock = DockStyle.Bottom, Ribbon = ribbon, Name = "_StatusBar" };
            status.Visible = this.StatusBarVisible;
            _StatusBar = status;
            this.Controls.Add(status);

            // expander:
        }
        /// <summary>
        /// Vytvoří ikonu
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        private void CreateIcon(DialogArgs args, Graphics graphics)
        {
            this.Icon = GetSystemIcon(DialogSystemIcon.Information);
            this.IconVisible = false;

            Image image = null;
            if (args.Icon != null) image = args.Icon;
            else if (args.IconFile != null) image = DxComponent.CreateBitmapImage(args.IconFile);
            if (image == null)
            {
                if (args.StandardIcon.HasValue) image = GetStandardIcon(args.StandardIcon.Value)?.ToBitmap();
                else if (args.SystemIcon.HasValue) image = GetSystemIcon(args.SystemIcon.Value).ToBitmap();
            }
            if (image == null) return;

            Size imageSize = image.Size;
            var maxIconSize = args.MaxIconSize;
            if (maxIconSize.HasValue && maxIconSize.Value > 0)
            {
                if (imageSize.Width > maxIconSize.Value) imageSize.Width = maxIconSize.Value;
                if (imageSize.Height > maxIconSize.Value) imageSize.Height = maxIconSize.Value;
            }

            this._Icon = new System.Windows.Forms.PictureBox() { Bounds = new System.Drawing.Rectangle(8, 8, imageSize.Width, imageSize.Height), Image = image };

            this._StandardPanel.Controls.Add(_Icon);
            this.IconVisible = true;
        }
        /// <summary>
        /// Vytvoří prvky pro zobrazení textu
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        private void CreateMessageControls(DialogArgs args, Graphics graphics)
        {
            var panel = new DevExpress.XtraEditors.XtraScrollableControl() { TabStop = false, Name = "_MessagePanel" };
            this._MessagePanel = panel;
            this._StandardPanel.Controls.Add(panel);

            if (!this.ExistsAnyText) return;

            string text = GetPrimaryMessageText(out bool allowHtml);
            if (String.IsNullOrEmpty(text)) return;

            _StyleText = new DevExpress.XtraEditors.StyleController();
            _StyleText.Appearance.FontSizeDelta = GetZoomDelta();
            _StyleText.Appearance.Options.UseBorderColor = false;
            _StyleText.Appearance.Options.UseBackColor = false;
            _StyleText.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;

            DevExpress.XtraEditors.LabelControl label = new DevExpress.XtraEditors.LabelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Dock = DockStyle.Top, Name = "_MessageLabel" };
            // label.StyleController = _StyleText;
            label.Appearance.FontSizeDelta = GetZoomDelta();
            label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            label.Appearance.TextOptions.HAlignment = ConvertHAlignment(args.MessageHorizontalAlignment);
            label.Appearance.Options.UseTextOptions = true;
            label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            _MessageLabel = label;
            this._MessagePanel.Controls.Add(label);

            DevExpress.XtraEditors.MemoEdit memoEdit = new DevExpress.XtraEditors.MemoEdit() { ReadOnly = true, TabStop = false, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Visible = false, Name = "_MessageMemo" };
            memoEdit.StyleController = _StyleText;
            _MessageMemo = memoEdit;
            this._MessagePanel.Controls.Add(memoEdit);

            FillMessageText(text, allowHtml, false, null);
            RefreshAltMsgButtonText();
        }
        /// <summary>
        /// Vytvoří objekty pro vstup textu
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        private void CreateInputText(DialogArgs args, Graphics graphics)
        {   // Patřičný Panel existuje i když blok není využit. Pak má Panel nastaveno Visible = false, a NEOBSAHUJE vnitřní controly:
            this.InputVisible = false;

            var type = args.InputTextType;
            bool isInputTextVisible = (type == ShowInputTextType.TextBox || type == ShowInputTextType.MemoEdit);
            var panel = new DevExpress.XtraEditors.XtraScrollableControl() { TabStop = false, Name = "_InputPanel" };
            panel.Visible = isInputTextVisible;
            this._InputPanel = panel;

            if (!isInputTextVisible) return;

            this._StandardPanel.Controls.Add(panel);
            this.InputVisible = true;

            bool isOneLine = (args.InputTextType != ShowInputTextType.MemoEdit);
            if (isOneLine)
            {
                var textbox = new DevExpress.XtraEditors.TextEdit() { StyleController = _StyleText, Text = args.InputTextValue, Name = "_InputControl " };
                textbox.MouseEnter += InputControl_MouseEnter;
                textbox.MouseLeave += InputControl_MouseLeave;
                textbox.Enter += InputControl_Enter;
                textbox.Leave += InputControl_Leave;
                this._InputPanel.Controls.Add(textbox);
                _InputControl = textbox;
            }
            else
            {
                var editbox = new DevExpress.XtraEditors.MemoEdit() { StyleController = _StyleText, Text = args.InputTextValue, Name = "_InputControl" };
                editbox.MouseEnter += InputControl_MouseEnter;
                editbox.MouseLeave += InputControl_MouseLeave;
                editbox.Enter += InputControl_Enter;
                editbox.Leave += InputControl_Leave;
                this._InputPanel.Controls.Add(editbox);
                _InputControl = editbox;
            }
        }
        /// <summary>
        /// Při opouštění editoru si uložím hodnotu do args
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputControl_Leave(object sender, EventArgs e)
        {
            _DialogArgs.InputTextValue = _InputControl.Text;
        }
        /// <summary>
        /// Vytvoří panel pro buttony a vytvoří i jednotlivé buttony podle daných dat
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        private void CreateButtons(DialogArgs args, Graphics graphics)
        {   // Patřičný Panel existuje i když blok není využit. Pak má Panel nastaveno Visible = false, a NEOBSAHUJE vnitřní controly:
            this.ButtonsVisible = false;

            _Buttons = new List<DevExpress.XtraEditors.SimpleButton>();
            bool isButtonsVisible = (args.Buttons.Count > 0);

            var panel = new DevExpress.XtraEditors.PanelControl() { BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, TabStop = false };
            panel.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            panel.Visible = isButtonsVisible;
            _ButtonPanel = panel;

            if (!isButtonsVisible) return;

            this._StandardPanel.Controls.Add(_ButtonPanel);

            _StyleButton = new DevExpress.XtraEditors.StyleController();
            _StyleButton.Appearance.FontSizeDelta = GetZoomDelta();
            Font font = _StyleButton.Appearance.GetFont();

            int buttonMaxWidth = 0;
            int buttonHeight = ButtonHeight;
            int imageWidth = 2 * buttonHeight / 3 + 3;
            foreach (var buttonInfo in args.Buttons)
            {
                DevExpress.XtraEditors.SimpleButton button = new DevExpress.XtraEditors.SimpleButton() { Text = buttonInfo.Text, Size = new Size(140, buttonHeight) };
                button.StyleController = _StyleButton;
                button.Tag = buttonInfo;

                // Obrázek u tlačítka:
                Image image = null;
                if (buttonInfo.Image != null) image = buttonInfo.Image;
                else if (buttonInfo.ImageFile != null) image = DxComponent.CreateBitmapImage(buttonInfo.ImageFile, ResourceImageSizeType.Medium);
                if (image != null)
                {
                    button.ImageOptions.Image = image;
                    button.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
                    button.ImageOptions.ImageToTextIndent = 3;
                }
                button.Enter += Button_Enter;
                button.MouseEnter += Button_MouseEnter;
                button.MouseLeave += Button_MouseLeave;
                button.Click += Button_Click;
                _Buttons.Add(button);
                _ButtonPanel.Controls.Add(button);

                SizeF textSize = graphics.MeasureString(buttonInfo.Text, font);
                int buttonWidth = (int)textSize.Width + buttonHeight;
                if (image != null) buttonWidth += imageWidth;
                if (buttonMaxWidth < buttonWidth) buttonMaxWidth = buttonWidth;
            }

            if (buttonMaxWidth < ButtonMinWidth) buttonMaxWidth = ButtonMinWidth;
            if (buttonMaxWidth > ButtonMaxWidth) buttonMaxWidth = ButtonMaxWidth;
            foreach (var button in _Buttons)
                button.Width = buttonMaxWidth;

            this.ButtonsVisible = true;
        }
        /// <summary>
        /// Vytvoří obsah pro StatusBar. Samotný StatusBar je součástí tvorby Frames v metodě <see cref="CreateFrames(DialogArgs, Graphics)"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="graphics"></param>
        private void CreateStatus(DialogArgs args, Graphics graphics)
        {
            var status = _StatusBar;

            int fontSizeDelta = GetZoomDelta();

            if (this.ExistsBothText)
                StatusAltTextCheckButton = AddStatusCheckButton(args, args.StatusBarAltMsgButtonText, MsgCode.DialogFormAltMsgButtonText, args.StatusBarAltMsgButtonTooltip, MsgCode.DialogFormAltMsgButtonToolTip, 78, 18, fontSizeDelta, StatusAltTextButton_ItemClick);

            StatusLabel1 = DxComponent.CreateDxStatusLabel(status, "", DevExpress.XtraBars.BarStaticItemSize.Spring, true, fontSizeDelta);
            StatusLabel2 = DxComponent.CreateDxStatusLabel(status, "", DevExpress.XtraBars.BarStaticItemSize.Content, false, fontSizeDelta);

            if (args.StatusBarCtrlCVisible || !String.IsNullOrEmpty(args.StatusBarCtrlCText))
                StatusCopyButton = AddStatusButton(args, args.StatusBarCtrlCText, MsgCode.DialogFormCtrlCText, args.StatusBarCtrlCTooltip, MsgCode.DialogFormCtrlCToolTip, 78, 18, fontSizeDelta, StatusCopyButton_ItemClick);

            RefreshAltMsgButtonText();
        }
        /// <summary>
        /// Přidá CheckButton do StatusBaru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="textCode"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipCode"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        private DxBarCheckItem AddStatusCheckButton(DialogArgs args, string text, string textCode, string toolTipText, string toolTipCode, int width, int height, int fontSizeDelta, DevExpress.XtraBars.ItemClickEventHandler clickHandler)
        {
            if (String.IsNullOrEmpty(text)) text = DxComponent.Localize(textCode);
            if (String.IsNullOrEmpty(toolTipText)) toolTipText = DxComponent.Localize(toolTipCode);

            return DxComponent.CreateDxStatusCheckButton(_StatusBar, text, width, height, null, toolTipText, true, fontSizeDelta, clickHandler);
        }
        /// <summary>
        /// Přidá Button do StatusBaru
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="textCode"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipCode"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        private DxBarButtonItem AddStatusButton(DialogArgs args, string text, string textCode, string toolTipText, string toolTipCode, int width, int height, int fontSizeDelta, DevExpress.XtraBars.ItemClickEventHandler clickHandler)
        {
            if (String.IsNullOrEmpty(text)) text = DxComponent.Localize(textCode);
            if (String.IsNullOrEmpty(toolTipText)) toolTipText = DxComponent.Localize(toolTipCode);

            return DxComponent.CreateDxStatusButton(_StatusBar, text, width, height, null, toolTipText, true, fontSizeDelta, clickHandler);
        }
        /// <summary>
        /// Vrátí <see cref="Icon"/> pro danou standardní ikonu. Poznámka: tento Enum nepokrývá všechny Systémové ikony.
        /// </summary>
        /// <param name="standardIcon"></param>
        /// <returns></returns>
        private static Icon GetStandardIcon(System.Windows.Forms.MessageBoxIcon standardIcon)
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
        private static Icon GetSystemIcon(DialogSystemIcon systemIcon)
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
        DialogArgs _DialogArgs;
        /// <summary>
        /// Obsahuje true pokud má být viditelná ikona. Určeno je to při tvorbě. Hodnota se využívá namísto Icon.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool IconVisible { get; set; }
        /// <summary>
        /// Obsahuje true pokud má být viditelný Input box. Určeno je to při tvorbě. Hodnota se využívá namísto InputPanel.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool InputVisible { get; set; }
        /// <summary>
        /// Obsahuje true pokud mají být viditelné Buttony. Určeno je to při tvorbě. Hodnota se využívá namísto ButtonsPanel.Visible, která je nastavena na true až po Show.
        /// </summary>
        private bool ButtonsVisible { get; set; }

        DevExpress.XtraEditors.StyleController _StyleText;
        DevExpress.XtraEditors.StyleController _StyleButton;
        System.Windows.Forms.Control _StandardPanel;
        System.Windows.Forms.Control _Icon;
        System.Windows.Forms.Control _MessagePanel;
        DevExpress.XtraEditors.LabelControl _MessageLabel;
        System.Windows.Forms.Control _MessageMemo;
        System.Windows.Forms.Control _InputPanel;
        System.Windows.Forms.Control _ButtonPanel;
        // System.Windows.Forms.Control _ExpanderPanel;
        DevExpress.XtraBars.Ribbon.RibbonStatusBar _StatusBar;
        DxBarCheckItem StatusAltTextCheckButton;
        DxBarStaticItem StatusLabel1;
        DxBarStaticItem StatusLabel2;
        DxBarButtonItem StatusCopyButton;
        List<DevExpress.XtraEditors.SimpleButton> _Buttons;
        /// <summary>
        /// Tento control reprezentuje vstupní políčko (_InputText nebo _InputMemo)
        /// </summary>
        DevExpress.XtraEditors.TextEdit _InputControl;
        #endregion
        #region Alternativní text
        /// <summary>
        /// Je zadán standardní nebo alternativní text?
        /// </summary>
        private bool ExistsAnyText { get { return ExistsStdText || ExistsAltText; } }
        /// <summary>
        /// Je zadán standardní a současně alternativní text?
        /// </summary>
        private bool ExistsBothText { get { return ExistsStdText && ExistsAltText; } }
        /// <summary>
        /// Je zadán standardní text?
        /// </summary>
        private bool ExistsStdText { get { return this.DialogArgs?.StdMessageTextExists ?? false; } }
        /// <summary>
        /// Je zadán alternativní text?
        /// </summary>
        private bool ExistsAltText { get { return this.DialogArgs?.AltMessageTextExists ?? false; } }
        /// <summary>
        /// Vrátí primární text
        /// </summary>
        /// <param name="allowHtml"></param>
        /// <returns></returns>
        private string GetPrimaryMessageText(out bool allowHtml)
        {
            allowHtml = false;
            var args = this.DialogArgs;
            if (args == null) return "";

            CurrentVisibleText = 0;
            if (ExistsStdText)
            {
                CurrentVisibleText = 1;
                allowHtml = args.MessageTextContainsHtml;
                return args.MessageText;
            }
            if (ExistsAltText)
            {
                CurrentVisibleText = 2;
                allowHtml = args.AltMessageTextContainsHtml;
                return args.AltMessageText;
            }
            return "";
        }
        /// <summary>
        /// Vymění zobrazený Message text mezi standardním a alternativním
        /// </summary>
        private void AlternateCurrentText()
        {
            var args = this.DialogArgs;
            if (args == null) return;

            if (CurrentVisibleText == 2 && ExistsStdText)
            {
                BoundsAltText = this.Bounds;
                CurrentVisibleText = 1;
                FillMessageText(args.MessageText, args.MessageTextContainsHtml, true, BoundsStdText);
                RefreshAltMsgButtonText();
            }
            else if (CurrentVisibleText != 2 && ExistsAltText)
            {
                BoundsStdText = this.Bounds;
                CurrentVisibleText = 2;
                FillMessageText(args.AltMessageText, args.AltMessageTextContainsHtml, true, BoundsAltText);
                RefreshAltMsgButtonText();
            }
        }
        /// <summary>
        /// Daný text vyplní do _MessageLabel.Text a _MessageLabel.AllowHtmlString, volitelně přepočítá Bounds formuláře
        /// </summary>
        /// <param name="text"></param>
        /// <param name="allowHtml"></param>
        /// <param name="recalcLayout"></param>
        /// <param name="boundsUser"></param>
        private void FillMessageText(string text, bool allowHtml, bool recalcLayout, Rectangle? boundsUser)
        {
            CurrentMessageText = text;
            CurrentMessageTextContainsHtml = allowHtml;

            _MessageLabel.Text = text;
            _MessageLabel.AllowHtmlString = allowHtml;
            _MessageMemo.Text = text;
            IsSmallText = (text.Length <= 80 && !text.Contains('\r'));

            if (recalcLayout)
            {
                _MessageLabel.Refresh();
                Tuple<Rectangle, Size> data = CalculateOptimalCoordinates();
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
        /// Aktualizuje obsah buttonu <see cref="StatusAltTextCheckButton"/> podle aktuálního stavu v <see cref="CurrentVisibleText"/> a podle textů v <see cref="DialogArgs"/>.
        /// </summary>
        private void RefreshAltMsgButtonText()
        {
            if (StatusAltTextCheckButton == null) return;
            var args = this.DialogArgs;
            if (args == null) return;

            string buttonText = (CurrentVisibleText == 1 ? (args.StatusBarAltMsgButtonText ?? args.StatusBarStdMsgButtonText) : (args.StatusBarStdMsgButtonText ?? args.StatusBarAltMsgButtonText));
            string buttonToolTip = (CurrentVisibleText == 1 ? (args.StatusBarAltMsgButtonTooltip ?? args.StatusBarStdMsgButtonTooltip) : (args.StatusBarStdMsgButtonTooltip ?? args.StatusBarAltMsgButtonTooltip));
            StatusAltTextCheckButton.Caption = buttonText;
            StatusAltTextCheckButton.SetToolTip(buttonText, buttonToolTip);
        }
        /// <summary>
        /// Uživatel kliknul na status bar button "AltText"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusAltTextButton_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            AlternateCurrentText();
        }
        private string CurrentMessageText;
        private bool CurrentMessageTextContainsHtml;
        private int CurrentVisibleText;
        private Rectangle? BoundsStdText;
        private Rectangle? BoundsAltText;

        #endregion
        #region Souřadnice a tvorba Layoutu
        /// <summary>
        /// Určí a nastaví souřadnice, kde bude okno zobrazeno.
        /// </summary>
        private void PrepareInitialBounds()
        {
            // Metoda PrepareInitialBounds() běží jen jedenkrát, ještě před Show() okna, ale po vytvoření všech controlů.
            // Nejprve zde provedeme RefreshLayout(), tam se určí vnitřní souřadnice jednotlivých controlů podle vnějších rozměrů okna (to se provádí i po každém Resize),
            //   a teprve poté zde určíme optimální velikosti vnitřních prvků (podle textů, podle fontu...) 
            //   a podle nich a podle rozdílu mezi vnitřní a vnější velikostí určíme správnou velikost formuláře:
            this.Size = new Size(600, 400);                                   // Tím vyvoláme this.RefreshLayout(); a to navíc pro defaultní rozměr, který dává výchozí prostor pro controly

            Tuple<Rectangle, Size> data = CalculateOptimalCoordinates();

            this.Bounds = data.Item1;
            this.StartPosition = FormStartPosition.Manual;
            this.MinimumSize = data.Item2;
        }
        /// <summary>
        /// Vypočítá optimální souřadnice formuláře
        /// </summary>
        /// <returns></returns>
        private Tuple<Rectangle, Size> CalculateOptimalCoordinates()
        {
            DialogArgs args = this._DialogArgs;

            Size formCurrentSize = this.Size;
            Size messageClientSize = _StandardPanel.ClientSize;                // Prostor pro prvky
            Size clientAddSize = formCurrentSize - messageClientSize;
            Size formMaxSize = this._InitialMaximumBounds.Size;
            var buttonDockSide = args.ButtonPanelDock;
            bool buttonsDockLeftRight = (buttonDockSide == DockStyle.Left || buttonDockSide == DockStyle.Right);

            // 1. Určíme, jakou velikost formuláře bychom potřebovali:
            //  a) Pro Text:
            Size textCurrentSize = this.MessageBounds.Size;
            Size textAddSize = formCurrentSize - textCurrentSize;              // Počet "okolních" pixelů mezi Formem a Textem
            Size textMaxSize = formMaxSize - textAddSize;                      // Když bych Form zvětšil na povolené maximum, tak pro Text bude tato Max velikost
            Size textTestSize = new Size(textMaxSize.Width - 12, textMaxSize.Height);    // Rozměr pro měření textu, s rezervou vpravo
            Size textRealSize = GetRealTextSize(textMaxSize, textTestSize);              // Text roztažený do Max velikosti (formuláře => textboxu) bude zabírat tuto velikost: tady se projeví "přetečení textu dolů" při zalomení řádků na danou šířku
            float ratio = (float)textRealSize.Width / (float)textRealSize.Height;        // Poměr šířka : výška, měli bychom jej udržet v rozmezí 3.5 : 1 (dost široký) až 1 : 2 (dost úzký).
            float ratioMax = 3.5f;
            int textOptWidth = textMaxSize.Width * 3 / 4;                                // Mezní šířka textu (75% maximální): pokud reálná šířka (tw) bude pod ní, pak nebudeme řešit zmenšení šířky.
            if (ratio > ratioMax && textRealSize.Width >= textOptWidth)
            {   // Máme to moc široké a málo vysoké, zmenšíme šířku a změříme text znovu:
                float ratioDown = ratioMax / ratio;                                      // Hodnota (ratioDown) je menší než 1 = poměr pro zmenšení šířky
                int textTestWidth = (int)(ratioDown * (float)textTestSize.Width);        // Upravená šířka pro text
                if (textTestWidth < textOptWidth) textTestWidth = textOptWidth;          // Neměla by jít pod 75% maximální šířky
                textTestSize.Width = textTestWidth;
                textRealSize = GetRealTextSize(textMaxSize, textTestSize);               // Upravili jsme šířku, vypočítáme výšku
            }

            //  b) Input: určíme nejmenší šířku okna tak, aby byl dobře vidět Input prvek:
            int? inputWidth = null;                                            // Vhodná šířka formuláře z pohledu InputTextu
            if (this.InputVisible && args.InputTextSize.HasValue)
            {
                int iw = (2 * MarginsX) + (2 * SpacingX) + args.InputTextSize.Value.Width + clientAddSize.Width;
                if (buttonsDockLeftRight && _ButtonsTotalHeight.HasValue)
                    iw += SpacingX + _ButtonsTotalHeight.Value;
                inputWidth = iw;
            }

            //  c) Určíme velikost z hlediska Buttonů:
            int? buttonWidth = null;                                           // Vhodná velikost formuláře z pohledu tlačítek;
            int? buttonHeight = null;                                          //  ...  null = v daném směru velikost neřeším
            if (this.ButtonsVisible)
            {
                Size buttonCurrentSize = this.ButtonsBounds.Size;              // Současný prostor, ve kterém jsou Buttony umístěny
                if (buttonsDockLeftRight && _ButtonsTotalHeight.HasValue)
                {   // Tlačítka jsou vlevo nebo vpravo, a máme v evidenci jejich sumární výšku:
                    int minHeight = 4 * MarginsY + _ButtonsTotalHeight.Value;  // Minimální vhodná výška panelu tlačítek, aby byly pěkně vidět     |    koeficient 4: součet volného místa nad + pod buttony, aby bylo vidět zarovnání Begin/Center/End
                    // Výška formuláře z hlediska buttonů = potřebná výška pro buttony (minHeight) + stávající "režijní pixely" mezi formulářem a buttony:
                    buttonHeight = minHeight + clientAddSize.Height;
                }
                else if (_ButtonsTotalWidth.HasValue)
                {   // Tlačítka jsou dole, a máme v evidenci jejich sumární šířku:
                    int minWidth = 3 * MarginsX + _ButtonsTotalWidth.Value;    // Minimální vhodná šířka panelu tlačítek, aby byly pěkně vidět     |    koeficient 3: součet volného místa vlevo + vpravo okolo buttonů, aby bylo vidět zarovnání Begin/Center/End
                    // Šířka formuláře z hlediska buttonů = potřebná šířka pro buttony (minWidth) + stávající "režijní pixely" mezi formulářem a buttony:
                    buttonWidth = minWidth + clientAddSize.Width;
                }
            }

            // 2. Určíme výslednou velikost formuláře a jeho pozici:
            Size textFormSize = textRealSize + textAddSize;                    // Velikost celého formuláře tak, aby text umístěný uvnitř měl svoji optimální velikost
            //  a) optimální pro text:
            int formWidth = textFormSize.Width;
            int formHeight = textFormSize.Height;

            //  b) zvětšit pro InputText:
            if (inputWidth.HasValue && formWidth < inputWidth.Value) formWidth = inputWidth.Value;
            
            //  c) zvětšit pro Buttony:
            if (buttonWidth.HasValue && formWidth < buttonWidth.Value) formWidth = buttonWidth.Value;
            if (buttonHeight.HasValue && formHeight < buttonHeight.Value) formHeight = buttonHeight.Value;

            // 3. Určíme MinSize pro Form:
            Size textMinSize = CurrentMinTextSize;
            Size formMinSize = textMinSize + textAddSize;
            if (formWidth < formMinSize.Width) formWidth = formMinSize.Width;
            if (formHeight < formMinSize.Height) formHeight = formMinSize.Height;


            // Aplikovat maximální velikost, vycentrovat, a poté zarovnat do daného prostoru:
            Rectangle maxBounds = _InitialMaximumBounds;
            if (formWidth > maxBounds.Width) formWidth = maxBounds.Width;
            if (formHeight > maxBounds.Height) formHeight = maxBounds.Height;

            Point centerPoint = _InitialCenterPoint;
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
        /// <see cref="AddTextHeight"/> ani <see cref="MinTextHeight"/> ani <see cref="MinTextInputHeight"/> ani <see cref="MinTextWidth"/>. 
        /// </summary>
        /// <param name="textMaxSize"></param>
        /// <param name="textTestSize"></param>
        /// <returns></returns>
        private Size GetRealTextSize(Size textMaxSize, Size textTestSize)
        {
            Size textPreferredSize = GetTextPreferredSize(textTestSize);       // Text roztažený do Max velikosti (formuláře => textboxu) bude zabírat tuto velikost: tady se projeví "přetečení textu dolů" při zalomení řádků na danou šířku
            int addTextHeight = (InputVisible ? 0 : AddTextHeight);
            Size minTextSize = CurrentMinTextSize;
            int tw = GetMin(textMaxSize.Width, textPreferredSize.Width, MinTextWidth);
            int th = GetMin(textMaxSize.Height, textPreferredSize.Height + addTextHeight, minTextSize.Height);
            return new Size(tw, th);
        }
        private Size CurrentMinTextSize
        {
            get
            {
                bool inputVisible = this.InputVisible;
                bool isSmallText = this.IsSmallText;
                int mtw = MinTextWidth;
                int mth = (inputVisible ? MinTextInputHeight : (isSmallText ? MinTextSmallHeight : MinTextHeight));
                return new Size(mtw, mth);
            }
        }
        /// <summary>
        /// Metoda vrací optimální rozměr pro text v aktuálním fontu pro danou maximální cílovou velikost.
        /// Tato metoda neřeší <see cref="AddTextHeight"/> ani <see cref="MinTextHeight"/> ani <see cref="MinTextInputHeight"/> ani <see cref="MinTextWidth"/>. 
        /// To řeší metoda <see cref="GetRealTextSize(Size, Size)"/>.
        /// <para/>
        /// Tato metoda reaguje na <see cref="CurrentMessageTextContainsHtml"/>, měří text <see cref="CurrentMessageText"/> a reaguje na přídavek k velikosti <paramref name="addSize"/>.
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <param name="addSize"></param>
        /// <returns></returns>
        private Size GetTextPreferredSize(Size proposedSize, int? addSize = null)
        {
            if (_MessageLabel == null) return proposedSize;

            Size preferredSize = Size.Empty;

            int proposedWidth = proposedSize.Width;
            if (this.CurrentMessageTextContainsHtml)
                proposedWidth = proposedWidth * 10 / 12;

            using (var graphics = this.CreateGraphics())
            {
                preferredSize = _MessageLabel.Appearance.CalcTextSize(graphics, this.CurrentMessageText, proposedWidth).ToSize();
            }
            if (this.CurrentMessageTextContainsHtml)
            {   // Formátovaný text může být větší, ale control to nedetekuje :-( 
                preferredSize.Width = preferredSize.Width * 11 / 10;
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
        /// Vrátí tu menší hodnotu z <paramref name="value1"/> a <paramref name="value2"/>, ale pokud by výsledek byl menší než <paramref name="minResult"/>, pak vrátí <paramref name="minResult"/>.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="minResult"></param>
        /// <returns></returns>
        private int GetMin(int value1, int value2, int minResult)
        {
            int value = (value1 < value2 ? value1 : value2);
            return (value > minResult ? value : minResult);
        }
        /// <summary>
        /// Uspořádá prvky okna tak, aby využily jeho plochu podle pravidel.
        /// </summary>
        private void RefreshLayout()
        {
            if (_StandardPanel == null) return;

            DialogArgs args = _DialogArgs;
            if (args is null) return;

            Size clientSize = this.ClientSize;
            int dw = clientSize.Width - _LastClientSize.Width;
            if (dw < 0 && dw > -5)
            { }

            // Určíme prostor pro jednotlivé prvky a umístíme je tam:
            CreateLayout();
            ApplyLayoutToControl(IconBounds, _Icon);
            ApplyLayoutToControl(MessageBounds, _MessagePanel, ApplyLayoutMessage);
            ApplyLayoutToControl(InputBounds, _InputPanel, ApplyLayoutInput);
            ApplyLayoutToControl(ButtonsBounds, _ButtonPanel, ApplyLayoutButtons);

            _LastClientSize = clientSize;
        }
        private Size _LastClientSize;
        /// <summary>
        /// Určí souřadnice prvků v aktuálním prostoru formuláře podle obsahu a pravidel.
        /// </summary>
        private void CreateLayout()
        {
            Rectangle iconBounds = Rectangle.Empty;
            Rectangle messageBounds = Rectangle.Empty;
            Rectangle inputBounds = Rectangle.Empty;
            Rectangle buttonsBounds = Rectangle.Empty;
            Rectangle expanderBounds = Rectangle.Empty;

            DialogArgs args = _DialogArgs;

            int buttonHeight = ButtonHeight;
            int marginsX = MarginsX;
            int marginsY = MarginsY;
            int spacingX = SpacingX;
            int spacingY = SpacingY;
            int spacingTextX = SpacingTextX;
            int spacingTextY = SpacingTextY;

            Size dialogSize = this._StandardPanel.ClientSize;                  // Prostor pro dialog. Pokud by existoval rozbalený ExpandPanel, pak není součástí StandardPanelu.

            bool iconVisible = this.IconVisible;
            int textX = marginsX;                                              // Souřadnice X pro text v případě, že není přítomna ikona
            if (iconVisible)
            {   // S ikonou:
                Size iconSize = _Icon.Size;
                if (iconSize.Width > 0)
                {
                    iconBounds = new Rectangle(marginsX, marginsY, iconSize.Width, iconSize.Height);
                    textX = iconBounds.Right + marginsX;                       // Souřadnice X pro text v případě, kdy máme ikonu: text od ní bude odsazen o totéž, o kolik je vlevo ikona od kraje okna
                }
            }

            if (this.ButtonsVisible)
            {   // S tlačítky:
                var buttons = this._Buttons;
                if (args.ButtonPanelDock == System.Windows.Forms.DockStyle.Left)
                {   // Tlačítka vlevo [pod ikonou]:
                    int buttonsMaxWidth = buttons.Select(b => b.Width).Max();
                    int bx = 0;
                    int by = (iconVisible ? iconBounds.Bottom + spacingY : 0);
                    int bw = marginsX + buttonsMaxWidth + spacingX;
                    int bh = dialogSize.Height - by;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    int tx1 = textX;
                    int tx2 = buttonsBounds.Right + spacingTextX;
                    int tx = (tx1 > tx2 ? tx1 : tx2);
                    int ty = marginsY;
                    int tw = dialogSize.Width - (marginsX + tx);
                    int th = dialogSize.Height - (2 * marginsY);
                    messageBounds = new Rectangle(tx, ty, tw, th);
                }
                else if (args.ButtonPanelDock == System.Windows.Forms.DockStyle.Right)
                {   // Tlačítka vpravo ():
                    int buttonsMaxWidth = buttons.Select(b => b.Width).Max();
                    int bw = spacingX + buttonsMaxWidth + marginsX;
                    int bx = dialogSize.Width - bw;
                    int by = 0;
                    int bh = dialogSize.Height;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    int tx = textX;
                    int ty = marginsY;
                    int tw = bx - spacingTextX - tx;
                    int th = dialogSize.Height - (2 * marginsY);
                    messageBounds = new Rectangle(tx, ty, tw, th);
                }
                else
                {   // Tlačítka v řadě dole (Green default):
                    int bx = 0;
                    int bh = spacingY + buttonHeight + marginsY;
                    int by = dialogSize.Height - bh;
                    int bw = dialogSize.Width;
                    buttonsBounds = new Rectangle(bx, by, bw, bh);

                    bool isMessageCentered = (args.MessageHorizontalAlignment == AlignContentToSide.Center || args.AutoCenterSmallText);  // Text má být vystředěn v řádku, anebo tak může být:
                    int tx = textX;
                    int tr = (isMessageCentered ? textX : marginsX);           // Pokud se text zarovnává vodorovně na střed, pak celý Label musí být zarovnán na střed okna!
                    int ty = marginsY;
                    int tw = dialogSize.Width - (tx + tr);
                    int th = buttonsBounds.Y - (spacingY + ty);
                    messageBounds = new Rectangle(tx, ty, tw, th);
                }
            }
            else
            {   // Bez tlačítek:
                int tx = textX;
                int ty = marginsY;
                int tw = dialogSize.Width - (marginsX + textX);
                int th = dialogSize.Height - (2 * marginsY);
                messageBounds = new Rectangle(tx, ty, tw, th);
            }

            // InputBox:
            if (this.InputVisible)
            {
                Size inputSize = InputControlSize;                             // Vlastní Control
                int iw = inputSize.Width + 2 * spacingX;                       // Panel s controlem je o 2 okraje větší
                int ih = inputSize.Height + 2 * spacingY;

                // InputBox se odkrojí z dolní části TextBoxu:
                messageBounds.Height = messageBounds.Height - ih;              // Zmenšíme prostor pro Message na úkor Input (Zoomed Height)

                // Šířka má být společná = ta větší:
                iw = (iw > messageBounds.Width ? iw : messageBounds.Width);
                if (messageBounds.Width < iw) messageBounds.Width = iw;        // Message můžeme rozšířit tak, aby korespondovala s Input

                int ix = messageBounds.X;
                int iy = messageBounds.Bottom;
                inputBounds = new Rectangle(ix, iy, iw, ih);
            }

            this.IconBounds = iconBounds;
            this.MessageBounds = messageBounds;
            this.InputBounds = inputBounds;
            this.ButtonsBounds = buttonsBounds;
            this.ExpanderBounds = expanderBounds;
        }
        /// <summary>
        /// Obsahuje požadovanou velikost vstupního controlu (Text/Memo) v pixelech po provedení zoomu.
        /// Neobsahuje Spacing, neprovádí srovnání s okolními prvky. Vychází z argumentu, ze zoomu a z výšky TextBoxu.
        /// </summary>
        private Size InputControlSize
        {
            get
            {
                DialogArgs args = _DialogArgs;
                bool isOneLine = (args.InputTextType != ShowInputTextType.MemoEdit);
                int iw = 0;
                int ih = 0;
                if (isOneLine)
                {   // Jednořádkový text:
                    if (args.InputTextSize.HasValue)
                        iw = args.InputTextSize.Value.Width;
                    else
                        iw = 200;
                    ih = (this._InputControl != null ? this._InputControl.Height : 20);
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
                iw = GetZoom(iw);
                ih = (isOneLine ? ih : GetZoom(ih));

                return new Size(iw, ih);
            }
        }
        /// <summary>
        /// Aplikuje dané souřadnice na daný control, zajistí jeho Visible, a vyvolá optional akci.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="control"></param>
        /// <param name="applyInnerAction"></param>
        private void ApplyLayoutToControl(Rectangle bounds, System.Windows.Forms.Control control, Action applyInnerAction = null)
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
        private void ApplyLayoutMessage()
        {
            Size clientSize = _MessagePanel.ClientSize;

            // Změříme, kolik prostoru potřebuje Label k zobrazení v dané šířce, natěsno:
            Size labelSize = GetTextPreferredSize(clientSize, 0);

            var hAlign = _DialogArgs.MessageHorizontalAlignment;
            var vAlign = _DialogArgs.MessageVerticalAlignment;

            // Automatické centrování:
            bool autoCenterSmallText = _DialogArgs.AutoCenterSmallText;
            bool isAutoCentered = false;
            if (autoCenterSmallText)
            {
                int fontHeight = ((int)_MessageLabel.Appearance.Font.GetHeight()) + 4;
                if ((labelSize.Height) <= fontHeight)
                {   // Text má výšku odpovídající jednomu řádku textu => vycentrujeme jej svisle:
                    vAlign = AlignContentToSide.Center;
                    if ((labelSize.Width + 4) <= clientSize.Width)
                        // Text je na šířku menší než prostor pro něj => vycentrujeme jej v řádku:
                        hAlign = AlignContentToSide.Center;

                    // Zajistíme vložení správných hodnot do Labelu:
                    var dvAlign = ConvertVAlignment(vAlign);
                    if (_MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) _MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    var dhAlign = ConvertHAlignment(hAlign);
                    if (_MessageLabel.Appearance.TextOptions.HAlignment != dhAlign) _MessageLabel.Appearance.TextOptions.HAlignment = dhAlign;

                    _MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;        // Respektuj velikost Bounds
                    if (_MessageLabel.Dock != DockStyle.Fill) _MessageLabel.Dock = DockStyle.Fill;

                    isAutoCentered = true;
                }
            }

            if (!isAutoCentered)
            {   // Pokud jsme necentrovali jednořádkový text automaticky:
                // Musíme zajistit původní požadované centrování v řádku:
                var dhAlign = ConvertHAlignment(hAlign);
                if (_MessageLabel.Appearance.TextOptions.HAlignment != dhAlign) _MessageLabel.Appearance.TextOptions.HAlignment = dhAlign;

                // Pokud výška labelu (s rezervou) bude menší, než je výška prostoru, a pokud je zarovnání požadované jinak než Top, pak na to reagujeme s pomocí VAlignment:
                if ((labelSize.Height + 6) < clientSize.Height && (vAlign == AlignContentToSide.Center || vAlign == AlignContentToSide.End))
                {   // Text (Label) umístíme do prostoru tak, aby byl v jeho prostoru správně zarovnán ve směru Y:
                    var dvAlign = ConvertVAlignment(vAlign);
                    if (_MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) _MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    _MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;        // Respektuj velikost Bounds
                    if (_MessageLabel.Dock != DockStyle.Fill) _MessageLabel.Dock = DockStyle.Fill;
                }
                else
                {   // Text je zarovnaný k Begin, anebo je velký => bude Dock = Top a není nutno víc řešit jeho pozici. Případný scrollBar přidá jeho Parent:
                    var dvAlign = VertAlignment.Top;
                    if (_MessageLabel.Appearance.TextOptions.VAlignment != dvAlign) _MessageLabel.Appearance.TextOptions.VAlignment = dvAlign;

                    _MessageLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;    // Podle velikosti textu přetékej dolů
                    if (_MessageLabel.Dock != DockStyle.Top) _MessageLabel.Dock = DockStyle.Top;
                }
            }

            // Toto je zatím Invisible a nepoužité:
            _MessageMemo.Size = clientSize;
        }
        /// <summary>
        /// Uspořádá správně vnitřní prvky pro Input
        /// </summary>
        private void ApplyLayoutInput()
        {
            Size clientSize = _InputPanel.ClientSize;
            Size controlSize = InputControlSize;
            int x, y, w, h;
            w = controlSize.Width;
            h = controlSize.Height;
            switch (_DialogArgs.InputTextType)
            {
                case ShowInputTextType.TextBox:
                    x = SpacingX;
                    h = _InputControl.Height;
                    y = (clientSize.Height - h) / 2;
                    if (y < 0) y = 0;
                    _InputControl.Bounds = new Rectangle(x, y, w, h);
                    break;
                case ShowInputTextType.MemoEdit:
                    x = SpacingX;
                    y = (clientSize.Height - h) / 2;
                    if (y < 0) y = 0;
                    _InputControl.Bounds = new Rectangle(x, y, w, h);
                    break;
            }
        }
        /// <summary>
        /// Uspořádá správně vnitřní prvky Buttons
        /// </summary>
        private void ApplyLayoutButtons()
        {
            _ButtonsTotalWidth = null;
            _ButtonsTotalHeight = null;
            DialogArgs args = _DialogArgs;
            if (args is null) return;

            if (args.ButtonPanelDock == DockStyle.Left || args.ButtonPanelDock == DockStyle.Right)
                ApplyLayoutButtonsVertical();
            else
                ApplyLayoutButtonsHorizontal();
        }
        /// <summary>
        /// Rozmístí buttony ve směru vertikálním
        /// </summary>
        private void ApplyLayoutButtonsVertical()
        {
            DialogArgs args = _DialogArgs;
            if (args is null) return;

            int buttonHeight = ButtonHeight;
            int marginsX = MarginsX;
            int marginsY = MarginsY;
            int spacingX = SpacingX;
            int spacingY = SpacingY;

            var buttons = this._Buttons;
            Size clientSize = this._ButtonPanel.ClientSize;
            int clientHeight = clientSize.Height - (2 * marginsY);
            int buttonCount = buttons.Count;
            int sumHeight = (buttonCount * buttonHeight) + (buttonCount - 1) * spacingY;
            _ButtonsTotalHeight = sumHeight;

            int y0 = marginsY + AlignSizeTo(sumHeight, clientHeight, args.ButtonsAlignment);
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
        private void ApplyLayoutButtonsHorizontal()
        {
            DialogArgs args = _DialogArgs;
            if (args is null) return;

            int buttonHeight = ButtonHeight;
            int marginsX = MarginsX;
            int marginsY = MarginsY;
            int spacingX = SpacingX;
            int spacingY = SpacingY;

            var buttons = this._Buttons;
            Size clientSize = this._ButtonPanel.ClientSize;
            int clientWidth = clientSize.Width - (2 * marginsX);
            int buttonCount = buttons.Count;
            int sumWidth = buttons.Select(b => b.Width).Sum() + (buttonCount - 1) * spacingX;
            _ButtonsTotalWidth = sumWidth;

            int x0 = marginsX + AlignSizeTo(sumWidth, clientWidth, args.ButtonsAlignment);
            int x = x0;
            int y = clientSize.Height - marginsY - buttonHeight;
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
        private int AlignSizeTo(int content, int parent, AlignContentToSide align)
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
        Point _InitialCenterPoint;
        /// <summary>
        /// Maximální souřadnice Formu, do kterých ho můžeme dát v rámci inicializace = 80% aktuálního monitoru
        /// </summary>
        Rectangle _InitialMaximumBounds;
        /// <summary>
        /// Optimální souřadnice pro Icon z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        Rectangle IconBounds { get; set; }
        /// <summary>
        /// Optimální souřadnice pro text Message z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        Rectangle MessageBounds { get; set; }
        /// <summary>
        /// Optimální souřadnice pro text Input z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        Rectangle InputBounds { get; set; }
        /// <summary>
        /// Optimální souřadnice pro Buttons z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        Rectangle ButtonsBounds { get; set; }
        /// <summary>
        /// Optimální souřadnice pro Expander z hlediska prostoru v okně. zde může být i záporná velikost, pokud by se do daného okna prvek nevešel. To pak musí řešit někdo jiný.
        /// </summary>
        Rectangle ExpanderBounds { get; set; }
        /// <summary>
        /// Jde o malý text: bez CrLf a nejvýše 80 znaků
        /// </summary>
        bool IsSmallText { get; set; }
        /// <summary>
        /// Celková šířka potřebná pro buttony (pouze suma šířek buttonů a jejich vnitřních mezer, nikoli Margins), buttony dokované dolů
        /// </summary>
        int? _ButtonsTotalWidth;
        /// <summary>
        /// Celková výška potřebná pro buttony (pouze suma výšek buttonů a jejich vnitřních mezer, nikoli Margins), buttony dokované vlevo i vpravo
        /// </summary>
        int? _ButtonsTotalHeight;
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální šířka buttonu
        /// </summary>
        private int ButtonMinWidth { get { return GetZoom(BMW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Maximální šířka buttonu
        /// </summary>
        private int ButtonMaxWidth { get { return GetZoom(BXW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Výška buttonu
        /// </summary>
        private int ButtonHeight { get { return GetZoom(_DialogArgs.ButtonHeight); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální šířka textu
        /// </summary>
        private int MinTextWidth { get { return GetZoom(MTW); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je samostatný a větší
        /// </summary>
        private int MinTextHeight { get { return GetZoom(MTH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je samostatný a menší
        /// </summary>
        private int MinTextSmallHeight { get { return GetZoom(MTSH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Minimální výška textu, pokud je zobrazen i InputPanel
        /// </summary>
        private int MinTextInputHeight { get { return GetZoom(MTIH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Povinný přídavek k výšce textu
        /// </summary>
        private int AddTextHeight { get { return GetZoom(ATH); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Margins X = okraje vnější
        /// </summary>
        private int MarginsX { get { return GetZoom(MX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Margins Y = okraje vnější
        /// </summary>
        private int MarginsY { get { return GetZoom(MY); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing X = mezery vnitřní
        /// </summary>
        private int SpacingX { get { return GetZoom(SX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing Y = mezery vnitřní
        /// </summary>
        private int SpacingY { get { return GetZoom(SY); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing X okolo textu
        /// </summary>
        private int SpacingTextX { get { return GetZoom(STX); } }
        /// <summary>
        /// Aktuální velikost (dle Zoomu): Spacing Y okolo textu
        /// </summary>
        private int SpacingTextY { get { return GetZoom(STY); } }
        /// <summary>
        /// Vrátí DevExpress styl pro Horizontální zarovnání
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        private HorzAlignment ConvertHAlignment(AlignContentToSide alignment)
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
        private VertAlignment ConvertVAlignment(AlignContentToSide alignment)
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
        private int GetZoomDelta()
        {
            float rootSize = 9f;
            float targetSize = rootSize * ZoomRatio;
            return (int)Math.Ceiling(targetSize - rootSize);
        }
        /// <summary>
        /// Vrátí počet pixelů přepočtený Zoomem
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private int GetZoom(int size)
        {
            float zoomed = (float)size * ZoomRatio;
            return (int)Math.Ceiling(zoomed);
        }
        /// <summary>
        /// Aktuální ratio Zoom: 1.0 = beze změny, 1.5 = +50%.
        /// Obsahuje hodnotu <see cref="DialogArgs.ZoomRatio"/> = <see cref="DialogArgs.ZoomRatio"/> * <see cref="DialogArgs.DialogZoomRatio"/>.
        /// </summary>
        private float ZoomRatio;
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
        internal const int DefaultButtonHeight = 27;
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
        private const int MTSH = 30;
        /// <summary>
        /// Minimální výška textu, pokud je zobrazen i InputPanel
        /// </summary>
        private const int MTIH = 30;
        /// <summary>
        /// Povinný přídavek k výšce textu
        /// </summary>
        private const int ATH = 16;
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
        private const int STX = 12;
        /// <summary>
        /// Spacing Y okolo textu
        /// </summary>
        private const int STY = 12;
        #endregion
        #region Implementace IEscapeKeyHandler
        /// <summary>
		/// Pokud je instance třídy aktivním controlem v době stisku klávesy Escape, pak dostane řízení do této metody <see cref="IEscapeKeyHandler.HandleEscapeKey()"/>.
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
            string text = this.Title;
            string delimiter = ": ";
            foreach (var button in _Buttons)
            {
                text = text + $"{delimiter}[{button.ResultValue}]";
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
            StatusBarCtrlCVisible = false;
            StatusBarCtrlCText = DxComponent.Localize(MsgCode.DialogFormCtrlCText);
            StatusBarCtrlCTooltip = DxComponent.Localize(MsgCode.DialogFormCtrlCToolTip);
            StatusBarCtrlCInfo = DxComponent.Localize(MsgCode.DialogFormCtrlCInfo);
            StatusBarStdMsgButtonText = DxComponent.Localize(MsgCode.DialogFormStdMsgButtonText);
            StatusBarStdMsgButtonTooltip = DxComponent.Localize(MsgCode.DialogFormStdMsgButtonToolTip);
            StatusBarAltMsgButtonText = DxComponent.Localize(MsgCode.DialogFormAltMsgButtonText);
            StatusBarAltMsgButtonTooltip = DxComponent.Localize(MsgCode.DialogFormAltMsgButtonToolTip);
            ButtonPanelDock = DialogForm.DefaultButtonDockSide;
            ButtonHeight = DialogForm.DefaultButtonHeight;
            ButtonsAlignment = DialogForm.DefaultButtonAlignment;
            FormState = DialogForm.DefaultFormState;
            _Buttons = new List<ButtonInfo>();
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
        /// Jméno ikonky.
        /// Konkrétní Image pro toto jméno získává metoda 
        /// </summary>
        public string IconFile { get; set; }
        /// <summary>
        /// Největší povolená velikost ikony. Může být null, nebo 16 až 128 (vždy po 16px).
        /// Pokud není zadáno, nebude omezena.
        /// Je vhodno zadat při použití explicitního Image
        /// </summary>
        public int? MaxIconSize { get { return _MaxIconSize; } set { if (value.HasValue) _MaxIconSize = (value < 16 ? 16 : (value > 128 ? 128 : 16 * (value / 16))); else _MaxIconSize = null; } }
        private int? _MaxIconSize;
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
        public bool StatusBarAltMsgButtonVisible { get { return (StdMessageTextExists && AltMessageTextExists); } }
        /// <summary>
        /// Message okno může obsahovat alternativní text (<see cref="AltMessageText"/>). Pro jeho zobrazení je připraveno tlačítko ve statusbaru vlevo (dole).
        /// Jeho defaultní text je dán hláškou <see cref="DialogForm.MsgCode_AltMsgButtonText"/> (typicky "Zobraz detaily"). V této property je možno text změnit.
        /// Zdejší text je přímo zobrazen, bez lokalizace.
        /// </summary>
        public string StatusBarAltMsgButtonText { get; set; }
        /// <summary>
        /// Tooltip k <see cref="StatusBarAltMsgButtonText"/>
        /// </summary>
        public string StatusBarAltMsgButtonTooltip { get; set; }
        /// <summary>
        /// Message okno může obsahovat alternativní text (<see cref="AltMessageText"/>). Pro jeho zobrazení je připraveno tlačítko ve statusbaru vlevo (dole).
        /// Tlačítko má ve výchozím stavu text dle <see cref="StatusBarAltMsgButtonText"/> (typicky "Zobraz detaily").
        /// Po zobrazení alternativního textu zprávy (typicky detaily) se text tlačítka změní na zdejší text, typicky "Skryj detaily".
        /// Jeho defaultní text je dán hláškou <see cref="DialogForm.MsgCode_StdMsgButtonText"/>, v této property je možno text změnit.
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
        /// Viditelnost buttonu "Ctrl+C = Copy" ve Statusbaru.
        /// Pokud zde bude true, ale v textu <see cref="StatusBarCtrlCText"/> bude prázdný string, pak se text tlačítka získá lokalizací textu "CtrlCText".
        /// Default = false, aplikace musí o zobrazení požádat.
        /// </summary>
        public bool StatusBarCtrlCVisible { get; set; }
        /// <summary>
        /// StatusBar může obsahovat button "Ctrl+C = Copy". Jeho akce je fixní: Clipboard.Copy.
        /// Ve výchozím stavu obsahuje lokalizovanou hlášku dle <see cref="DialogForm.MsgCode_CtrlCText"/>.
        /// Viditelnost tlačítka řídí výhradně <see cref="StatusBarCtrlCVisible"/>.
        /// Zdejší text je přímo zobrazen, bez lokalizace.
        /// </summary>
        public string StatusBarCtrlCText { get; set; }
        /// <summary>
        /// Tooltip k <see cref="StatusBarCtrlCText"/>. 
        /// Ve výchozím stavu obsahuje lokalizovanou hlášku dle <see cref="DialogForm.MsgCode_CtrlCTooltip"/>.
        /// </summary>
        public string StatusBarCtrlCTooltip { get; set; }
        /// <summary>
        /// Po provedení Ctrl+C se v okně (ve statusbaru) může objevit text typu "Zkopírováno do schránky". Zde má být uveden tento text.
        /// Pokud zde bude empty string, nebud ehláška zobrazena.
        /// </summary>
        public string StatusBarCtrlCInfo { get; set; }
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
        /// Obsah vstupního textu (na výstupu je vložena editovaná hodnota)
        /// </summary>
        public string InputTextValue { get; set; }
        /// <summary>
        /// Informace zobrazená ve Statusbaru při editaci InputTextu
        /// </summary>
        public string InputTextStatusInfo { get; set; }
        /// <summary>
        /// Obsah vstupního textu (na výstupu je vložena editovaná hodnota)
        /// </summary>
        public string InputTextEmptyHint { get; set; }
        /// <summary>
        /// Umístění panelu s tlačítky, defaultní je <see cref="System.Windows.Forms.DockStyle.Bottom"/>.
        /// </summary>
        public System.Windows.Forms.DockStyle ButtonPanelDock { get; set; }
        /// <summary>
        /// Výška tlačítka, bude upravena pomocí <see cref="ZoomRatio"/>.
        /// Default = 36; povolené rozmezí 20 až 70.
        /// Kdo chce nastavit výšku buttonu explicitně, musí ji do argumentu vložit dělenou Zoomem.
        /// </summary>
        public int ButtonHeight { get { return _ButtonHeight; } set { _ButtonHeight = (value < 20 ? 20 : (value > 70 ? 70 : value)); } }
        private int _ButtonHeight;
        /// <summary>
        /// Umístění tlačítek uvnitř panelu
        /// </summary>
        public AlignContentToSide ButtonsAlignment { get; set; }
        /// <summary>
        /// Pole tlačítek. Lze jej naplnit ručně, nebo použít metodu <see cref="PrepareButtons(DialogResult[])"/>.
        /// </summary>
        public List<ButtonInfo> Buttons { get { return _Buttons; } }
        private List<ButtonInfo> _Buttons;
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
            dialogArgs.SystemIcon = DialogSystemIcon.Warning;
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
        /// Vygeneruje a vrátí informaci o chybách (včetně InnerExceptions) ve dvou formách:
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
                    string prefix = (!hasInner ? "" : $"{number}. ");
                    string message = ex.Message;
                    string method = (ex.TargetSite != null ? $"{ex.TargetSite.DeclaringType.FullName}.{ex.TargetSite.Name}()" : "");
                    string stack = ex.StackTrace;

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

                    ex = ex.InnerException;
                    number++;
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
                if (defaultIndex >= 0 && defaultIndex < this._Buttons.Count)
                    this._Buttons[defaultIndex].IsInitialButton = true;
            }
        }
        /// <summary>
        /// Do pole <see cref="Buttons"/> vloží tlačítka odpovídající požadovaným hodnotám.
        /// Pokud pole před voláním této metody obsahovalo nějaká tlačítka, budou zrušena.
        /// </summary>
        /// <param name="results"></param>
        public void PrepareButtons(params DialogResult[] results)
        {
            _Buttons.Clear();
            foreach (var result in results)
                AddButton(result);

            // Specialita: pokud je pouze button OK, pak má i vlastnost Escape:
            if (results.Length == 1 && results[0] == DialogResult.OK)
                this._Buttons[0].IsEscapeButton = true;
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
            _Buttons.Add(buttonInfo);
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
                _Buttons.Add(buttonInfo);
        }
        #endregion
        #region class ButtonInfo
        /// <summary>
        /// Třída popisující jedno tlačítko
        /// </summary>
        public class ButtonInfo
        {
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
            public string ImageFile { get; set; }
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
            /// Výsledná hodnota dialogu, pokud bude stisknuto toto tlačítko
            /// </summary>
            public object ResultValue { get; set; }
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
    /// Druh zarovnání obsahu v jedné ose (X, Y, číslená...)
    /// </summary>
    public enum AlignContentToSide
    {
        /// <summary>
        /// K začátku (Top, Left, 0)
        /// </summary>
        Begin,
        /// <summary>
        /// Na střed
        /// </summary>
        Center,
        /// <summary>
        /// Ke konci (Bottom, Right, nekonečno)
        /// </summary>
        End
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
    #endregion
}
