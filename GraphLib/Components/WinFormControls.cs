using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

using GUI = Noris.LCS.Base.WorkScheduler;
using RES = Noris.LCS.Base.WorkScheduler.Resources;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class WinFormDialog : Třída formuláře pro Dialog
    /// <summary>
    /// WinFormDialog : Třída formuláře pro Dialog
    /// </summary>
    public class WinFormDialog : WinFormButtons
    {
        #region Konstrukce
        /// <summary>
        /// Inicializace
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();                   // Buttony

            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.HelpButton = false;

            this.IconBox = new PictureBox();
            FontInfo textFont = FontInfo.DefaultBold;
            textFont.ApplyZoom(1.45f);
            this.MessageTextLabel = new Label() { BorderStyle = BorderStyle.None, AutoSize = false, Font = textFont.CreateNewFont() };
            this.MessageTextBox = new TextBox() { ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Both, WordWrap = true, Multiline = true, Font = textFont.CreateNewFont() };
            this.MessageRtfBox = new RichTextBox() { ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Both, Multiline = true, WordWrap = true };
            this.MessageRtfBox.GotFocus += MessageRtfBox_GotFocus;
            this.DataPanel.Controls.Add(this.IconBox);
            this.DataPanel.Controls.Add(this.MessageTextLabel);
            this.DataPanel.Controls.Add(this.MessageTextBox);
            this.DataPanel.Controls.Add(this.MessageRtfBox);

            this.ButtonsAlignment = ContentAlignment.MiddleRight;

            //this.DataPanel.BackColor = Color.LightGreen;
            //this.IconBox.BackColor = Color.LightCyan;
            //this.MessageTextLabel.BackColor = Color.LightYellow;
        }
        /// <summary>
        /// Po příchodu focusu do RTF boxu předá focus do buttonu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageRtfBox_GotFocus(object sender, EventArgs e)
        {
            this.SetFocusTo(this.WinButtonWithFocus);
        }
        /// <summary>
        /// Rozmístí controly
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();
            if (!this.IsShown) return;

            int y = 9;
            int x = 12;
            int r = 12;
            int b = 9;
            int s = 12;

            // Ikonka:
            if (this.IconBox.Image != null)
            {
                Size iconSize = this.IconBox.Image.Size;
                if (iconSize.Width > 64) iconSize = iconSize.ZoomToWidth(64);
                this.IconBox.Bounds = new Rectangle(new Point(x, y), iconSize);
                if (!this.IconBox.Visible) this.IconBox.Visible = true;
                x = x + iconSize.Width + s;
            }
            else
            {
                if (this.IconBox.Visible) this.IconBox.Visible = false;
            }

            // Textové pole
            TextMode textmode = this.CurrentTextMode;
            Size dataSize = this.DataPanel.ClientSize;
            Rectangle textBounds = new Rectangle(x, y, dataSize.Width - r - x, dataSize.Height - b - y);
            this.MessageTextLabel.Visible = (textmode == TextMode.Label);
            this.MessageTextLabel.Bounds = textBounds;
            this.MessageTextBox.Visible = (textmode == TextMode.Text);
            this.MessageTextBox.Bounds = textBounds;
            this.MessageRtfBox.Visible = (textmode == TextMode.Rtf);
            this.MessageRtfBox.Bounds = textBounds;
        }
        /// <summary>
        /// Při prvním zobrazení
        /// </summary>
        protected override void OnFirstShown()
        {
            base.OnFirstShown();
            this.SetFocusTo(this.WinButtons.FirstOrDefault(wb => wb.Visible));
        }
        /// <summary>
        /// Dá focus do daného buttonu
        /// </summary>
        /// <param name="button"></param>
        protected void SetFocusTo(WinButtonBase button)
        {
            if (button != null && button.Visible)
                button.Focus();
        }
        /// <summary>
        /// Objekt pro zobrazení ikony
        /// </summary>
        protected PictureBox IconBox;
        /// <summary>
        /// Objekt pro zobrazení plain textu
        /// </summary>
        protected TextBox MessageTextBox;
        /// <summary>
        /// Objekt pro zobrazení plain textu
        /// </summary>
        protected Label MessageTextLabel;
        /// <summary>
        /// Objekt pro zobrazení RTF textu
        /// </summary>
        protected RichTextBox MessageRtfBox;
        /// <summary>
        /// Aktuální režim textu
        /// </summary>
        protected TextMode CurrentTextMode;
        /// <summary>
        /// Režim textu
        /// </summary>
        protected enum TextMode
        {
            /// <summary>
            /// Text nezadán
            /// </summary>
            None,
            /// <summary>
            /// Label
            /// </summary>
            Label,
            /// <summary>
            /// Prostý text
            /// </summary>
            Text,
            /// <summary>
            /// RTF text
            /// </summary>
            Rtf
        }
        /// <summary>
        /// Ikona
        /// </summary>
        public Image DialogIcon { get { return this.IconBox.Image; } set { this.IconBox.Image = value; this.PrepareLayout(); } }
        /// <summary>
        /// Text zprávy, může být prostý text nebo RTF
        /// </summary>
        public string DialogMessage
        {
            get { return this._DialogMessage; }
            set
            {
                string text = value;
                this._DialogMessage = text;
                if (String.IsNullOrEmpty(text))
                {
                    this.CurrentTextMode = TextMode.None;
                }
                else if (!text.StartsWith(@"{\rtf1\"))
                {
                    this.CurrentTextMode = TextMode.Label;
                    this.MessageTextLabel.Text = text;
                    this.MessageTextBox.Text = text;
                }
                else
                {
                    this.CurrentTextMode = TextMode.Rtf;
                    this.MessageRtfBox.Rtf = text;
                }
                this.PrepareLayout();
            }
        }
        /// <summary>
        /// Text zprávy
        /// </summary>
        private string _DialogMessage;
        /// <summary>
        /// Titulek okna
        /// </summary>
        public string DialogTitle { get { return this.Text; } set { this.Text = value; } }
        #endregion
        #region Statické vyvolání dialogu
        /// <summary>
        /// Zobrazí dialog a vrátí volbu uživatele
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="buttons"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static GUI.GuiDialogButtons ShowDialog(Form owner, string message, string title, GUI.GuiDialogButtons buttons = GUI.GuiDialogButtons.Ok, GUI.GuiImage icon = null)
        {
            GUI.GuiDialogButtons result = Noris.LCS.Base.WorkScheduler.GuiDialogButtons.None;
            using (WinFormDialog form = new WinFormDialog())
            {
                form.SetBounds(owner, 0.45f, 0.30f);
                form.DialogMessage = message;
                form.DialogTitle = (!String.IsNullOrEmpty(title) ? title : GetDefaultTitleForButtons(buttons));
                if (icon == null || icon.IsEmpty)
                    icon = GetDefaultIconForButtons(buttons);
                form.DialogIcon = Application.App.Resources.GetImage(icon);
                form.Buttons = buttons;
                form.ShowDialog(owner);
                result = form.GuiResult;
            }
            return result;
        }
        /// <summary>
        /// Metoda vrací defaultní ikonu pro formulář, podle zadaných tlačítek.
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        protected static GUI.GuiImage GetDefaultIconForButtons(GUI.GuiDialogButtons buttons)
        {
            int count = GetButtonsCount(buttons);
            if (count == 0) return GUI.GuiDialog.DialogIconWarning;
            if (count > 1) return GUI.GuiDialog.DialogIconQuestion;
            if ((buttons & (GUI.GuiDialogButtons.Ok | GUI.GuiDialogButtons.Ignore | GUI.GuiDialogButtons.Maybe | GUI.GuiDialogButtons.Save | GUI.GuiDialogButtons.Yes)) != 0)
                return GUI.GuiDialog.DialogIconInfo;       // Je pouze jeden button, a pokud je to Ok, Ignore, Maybe, Save nebo Yes, tak vracím ikonu (i)
            return GUI.GuiDialog.DialogIconError;
        }
        /// <summary>
        /// Metoda vrací defaultní titulek pro formulář, podle zadaných tlačítek.
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        protected static string GetDefaultTitleForButtons(GUI.GuiDialogButtons buttons)
        {
            int count = GetButtonsCount(buttons);
            if (count <= 1) return "Potvrďte prosím přečtení";
            return "Vyberte prosím odpověď...";
        }
        #endregion
    }
    #endregion
    #region class WinFormButtons : Bázová třída pro formuláře, které mají dole lištu s tlačítky a nad ní panel pro data
    /// <summary>
    /// WinFormButtons : Bázová třída pro formuláře, které mají dole lištu s tlačítky a nad ní panel pro data
    /// </summary>
    public class WinFormButtons : WinFormBase
    {
        #region Konstrukce
        /// <summary>
        /// Inicializace formu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// <para/>
        /// Třída <see cref="WinFormButtons"/> inicializuje prvky <see cref="WinFormButtons.ButtonsPanel"/> a <see cref="WinFormButtons.DataPanel"/>, a pole buttonů.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.DataPanel = new Panel() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };
            this.Controls.Add(this.DataPanel);
            this.ButtonsPanel = new Panel() { Height = ButtonPanelHeight, BorderStyle = BorderStyle.None, Dock = DockStyle.Bottom };
            this.Controls.Add(this.ButtonsPanel);
            this._PrepareButtons();
            this._CloseOnClick = true;
            this._ButtonsAlignment = ContentAlignment.MiddleLeft;
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti.
        /// Metoda je volána jedenkrát po dokončení inicializace, pak těsně před prvním zobrazením formuláře, a pak po změně rozměrů okna.
        /// Metoda není volaná před dokončením inicializace, když je <see cref="WinFormBase.IsInitialized"/> = false.
        /// Metoda může testovat, zda <see cref="WinFormBase.IsShown"/> je true.
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();
            if (!this.IsShown) return;

            this._ShowButtons(this._Buttons);
        }
        /// <summary>
        /// Panel, obsahující data
        /// </summary>
        protected Panel DataPanel;
        /// <summary>
        /// Panel, obsahující buttony
        /// </summary>
        protected Panel ButtonsPanel;
        #endregion
        #region Buttony
        /// <summary>
        /// Metoda připraví všechny buttony, všechny budou mít Visible = false
        /// </summary>
        private void _PrepareButtons()
        {
            this._Buttons = Noris.LCS.Base.WorkScheduler.GuiDialogButtons.Ok;

            this._WinButtonList = new List<WinButtonBase>();

            this.ButtonOk = this._PrepareButton(GUI.GuiDialogButtons.Ok, RES.Images.Actions24.DialogOkApply5Png, "Ok");
            this.ButtonYes = this._PrepareButton(GUI.GuiDialogButtons.Yes, RES.Images.Actions24.DialogOkApply5Png, "&Ano");
            this.ButtonNo = this._PrepareButton(GUI.GuiDialogButtons.No, RES.Images.Actions24.DialogCancel3Png, "&Ne");
            this.ButtonAbort = this._PrepareButton(GUI.GuiDialogButtons.Abort, RES.Images.Actions24.DialogCancel4Png, "&Přerušit");
            this.ButtonRetry = this._PrepareButton(GUI.GuiDialogButtons.Retry, RES.Images.Actions24.ViewRefresh7Png, "Znovu");
            this.ButtonIgnore = this._PrepareButton(GUI.GuiDialogButtons.Ignore, RES.Images.Actions24.EditRedoPng, "&Ignorovat");
            this.ButtonSave = this._PrepareButton(GUI.GuiDialogButtons.Save, RES.Images.Actions24.DocumentSaveAs6Png, "&Uložit");
            this.ButtonMaybe = this._PrepareButton(GUI.GuiDialogButtons.Maybe, RES.Images.Actions24.ToolsWizardPng, "&Možná...");
            this.ButtonCancel = this._PrepareButton(GUI.GuiDialogButtons.Cancel, RES.Images.Actions24.DialogCancel5Png, "&Storno");
        }
        /// <summary>
        /// Pole všech buttonů
        /// </summary>
        protected WinButtonBase[] WinButtons { get { return this._WinButtonList.ToArray(); } }
        private List<WinButtonBase> _WinButtonList;
        /// <summary>
        /// Vytvoří Button z daných dat, přidá jej do ButtonPanel.Controls a vrátí jej
        /// </summary>
        /// <param name="value"></param>
        /// <param name="image"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private WinButtonBase _PrepareButton(GUI.GuiDialogButtons value, string image, string text)
        {
            WinButtonBase button = new WinButtonBase()
            {
                Size = new Size(110, 32),
                Text = text,
                Image = Application.App.Resources.GetImage(image),
                Visible = false,
                Tag = value
            };
            button.Click += _Button_Click;
            button.GotFocus += _Button_GotFocus;
            this.ButtonsPanel.Controls.Add(button);
            this._WinButtonList.Add(button);
            return button;
        }
        /// <summary>
        /// Eventhandler pro událost Button.GotFocus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_GotFocus(object sender, EventArgs e)
        {
            this.WinButtonWithFocus = sender as WinButtonBase;
        }
        /// <summary>
        /// Obsahuje button, který má aktuálně Focus
        /// </summary>
        protected WinButtonBase WinButtonWithFocus { get; private set; }
        /// <summary>
        /// Je vyvoláno při prvním zobrazení formuláře
        /// </summary>
        protected override void OnFirstShown()
        {
            base.OnFirstShown();
            this.SelectAutoButtons();
        }
        /// <summary>
        /// Nastaví defaultní buttony pro formulář, na základě aktivních reálných buttonů
        /// </summary>
        protected void SelectAutoButtons()
        {
            this.AcceptButton = this._SearchAcceptButton();
            this.CancelButton = this._SearchCancelButton();
        }
        /// <summary>
        /// Vrací defaultní button pro funkci <see cref="Form.AcceptButton"/>
        /// </summary>
        /// <returns></returns>
        private WinButtonBase _SearchAcceptButton()
        {
            GUI.GuiDialogButtons buttons = this._Buttons;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Ok)) return this.ButtonOk;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Yes)) return this.ButtonYes;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Save)) return this.ButtonSave;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Retry)) return this.ButtonRetry;
            return null;
        }
        /// <summary>
        /// Vrací defaultní button pro funkci <see cref="Form.CancelButton"/>
        /// </summary>
        /// <returns></returns>
        private WinButtonBase _SearchCancelButton()
        {
            GUI.GuiDialogButtons buttons = this._Buttons;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Cancel)) return this.ButtonCancel;
            if (buttons.HasFlag(GUI.GuiDialogButtons.No)) return this.ButtonNo;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Abort)) return this.ButtonAbort;
            return null;
        }
        /// <summary>
        /// Metoda uloží do <see cref="_GuiResult"/> defaultní odpověď, která bude vrácena z dialogu pokud jej uživatel zavře křížkem.
        /// </summary>
        private void _DetectCloseResponse()
        {
            this._GuiResult = this._SearchCloseResponse();
        }
        /// <summary>
        /// Vrací výchozí hodnotu pro výsledek dialogu, který bude vrácen po zavření okna bez výběru konkrétního buttonu.
        /// </summary>
        /// <returns></returns>
        private GUI.GuiDialogButtons _SearchCloseResponse()
        {
            GUI.GuiDialogButtons buttons = this._Buttons;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Cancel)) return GUI.GuiDialogButtons.Cancel;
            if (buttons.HasFlag(GUI.GuiDialogButtons.No)) return GUI.GuiDialogButtons.No;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Abort)) return GUI.GuiDialogButtons.Abort;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Ignore)) return GUI.GuiDialogButtons.Ignore;
            if (buttons.HasFlag(GUI.GuiDialogButtons.Ok)) return GUI.GuiDialogButtons.Ok;
            return GUI.GuiDialogButtons.Cancel;
        }
        /// <summary>
        /// Handler události Button.Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Button_Click(object sender, EventArgs e)
        {
            WinButtonBase button = sender as WinButtonBase;
            if (button == null) return;
            if (!(button.Tag is GUI.GuiDialogButtons)) return;

            GUI.GuiDialogButtons result = (GUI.GuiDialogButtons)button.Tag;
            this._CallButtonClick(result);

            if (this.CloseOnClick)
            {
                this._GuiResult = result;
                this.Close();
            }
        }
        /// <summary>
        /// Zajistí vyvolání háčku a eventu 
        /// </summary>
        /// <param name="result"></param>
        private void _CallButtonClick(GUI.GuiDialogButtons result)
        {
            GPropertyEventArgs<GUI.GuiDialogButtons> args = new GPropertyEventArgs<GUI.GuiDialogButtons>(result);
            this.OnButtonClick(args);
            if (this.ButtonClick != null)
                this.ButtonClick(this, args);
        }
        /// <summary>
        /// Háček volaný při událost ButtonClick
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnButtonClick(GPropertyEventArgs<GUI.GuiDialogButtons> args) { }
        /// <summary>
        /// Událost volaná přio kliknutí na button
        /// </summary>
        public event GPropertyEventHandler<GUI.GuiDialogButtons> ButtonClick;
        /// <summary>
        /// Metoda zajistí zobrazení buttonů 
        /// </summary>
        private void _ShowButtons(GUI.GuiDialogButtons buttons)
        {
            int count = GetButtonsCount(buttons);          // Kolik buttonů budeme zobrazovat?
            if (count == 0)
            {
                buttons = GUI.GuiDialogButtons.Ok;
                count = 1;
            }
            bool isCancelIndented = (count >= 3 && buttons.HasFlag(GUI.GuiDialogButtons.Cancel));       // true pokud má být zobrazen button Cancel s odsazením
            float buttonWidth = 126;                         // Optimální šířka jednoho buttonu, když bude dost místa
            float borderWidth = 12;                          // Šířka okrajů
            float spaceWidth = 9;                            // Šířka mezery
            float spaceCancel = (isCancelIndented ? 24 : 0); // Extra odsazení buttonu Cancel
            float contentWidth = ((count * buttonWidth) + spaceCancel + ((count - 1) * spaceWidth));  // Tolik místa bychom potřebovali pro všechny buttony při optimální šířce jednoho buttonu
            float parentWidth = this.ButtonsPanel.Width;     // Tolik místa na buttony reálně máme v controlu
            float parentHeight = this.ButtonsPanel.Height;   // Výška prostoru
            float disponibleWidth = parentWidth - (2f * borderWidth);
            if (contentWidth > disponibleWidth)
            {
                buttonWidth = (disponibleWidth - spaceCancel - (spaceWidth * (count - 1))) / count;
                if (buttonWidth < 50f) buttonWidth = 50f;
                contentWidth = ((count * buttonWidth) + spaceCancel + ((count - 1) * spaceWidth));  // Tolik místa zaberou buttony při upravené šířce buttonWidth
            }

            float x, y;
            _AlignContent(parentWidth, parentHeight, contentWidth, borderWidth, this.ButtonsAlignment, out x, out y);

            this._ShowButton(this.ButtonOk, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonYes, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonNo, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonAbort, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonRetry, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonIgnore, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonSave, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonMaybe, buttons, ref x, y, buttonWidth, 0, spaceWidth);
            this._ShowButton(this.ButtonCancel, buttons, ref x, y, buttonWidth, spaceCancel, spaceWidth);
        }
        /// <summary>
        /// Určí souřadnice prostoru pro buttony
        /// </summary>
        /// <param name="parentWidth"></param>
        /// <param name="parentHeight"></param>
        /// <param name="contentWidth"></param>
        /// <param name="borderWidth"></param>
        /// <param name="buttonsAlignment"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected void _AlignContent(float parentWidth, float parentHeight, float contentWidth, float borderWidth, ContentAlignment buttonsAlignment, out float x, out float y)
        {
            int borderHeight = 6;
            SizeF contentSize = new SizeF(contentWidth, ButtonItemHeight);
            RectangleF totalBounds = new RectangleF(borderWidth, borderHeight, parentWidth - 2 * borderWidth, parentHeight - 2 * borderHeight);
            RectangleF contentBounds = contentSize.AlignTo(totalBounds, buttonsAlignment);
            x = contentBounds.X;
            y = contentBounds.Y;
        }
        /// <summary>
        /// Zajistí zobrazení jednoho buttonu
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttons"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="spaceBefore"></param>
        /// <param name="buttonWidth"></param>
        /// <param name="spaceWidth"></param>
        private void _ShowButton(WinButtonBase button, GUI.GuiDialogButtons buttons, ref float x, float y, float buttonWidth, float spaceBefore, float spaceWidth)
        {
            GUI.GuiDialogButtons buttonValue = (button.Tag is GUI.GuiDialogButtons ? (GUI.GuiDialogButtons)button.Tag : GUI.GuiDialogButtons.None);
            bool isVisible = ((buttons & buttonValue) != 0);
            button.Visible = isVisible;
            if (isVisible)
            {
                x += spaceBefore;
                int ix = (int)Math.Round(x, 0);
                int iy = (int)Math.Round(y, 0);
                int iw = (int)Math.Round(buttonWidth, 0);
                button.Bounds = new Rectangle(ix, iy, iw, ButtonItemHeight);
                x = x + buttonWidth + spaceWidth;
            }
        }
        /// <summary>
        /// Výška celého panelu pro Buttony
        /// </summary>
        protected static int ButtonPanelHeight { get { return 46; } }
        /// <summary>
        /// Výška jednotlivého Buttonu
        /// </summary>
        protected static int ButtonItemHeight { get { return 32; } }
        /// <summary>
        /// Metoda vrátí počet buttonů, které daná proměnná deklaruje
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        protected static int GetButtonsCount(GUI.GuiDialogButtons buttons)
        {
            return ((int)(buttons & Noris.LCS.Base.WorkScheduler.GuiDialogButtons.All)).GetBitsOneCount();
        }
        /// <summary>
        /// Tlačítko OK
        /// </summary>
        protected WinButtonBase ButtonOk;
        /// <summary>
        /// Tlačítko ANO
        /// </summary>
        protected WinButtonBase ButtonYes;
        /// <summary>
        /// Tlačítko NE
        /// </summary>
        protected WinButtonBase ButtonNo;
        /// <summary>
        /// Tlačítko ZRUŠIT
        /// </summary>
        protected WinButtonBase ButtonAbort;
        /// <summary>
        /// Tlačítko ZNOVU
        /// </summary>
        protected WinButtonBase ButtonRetry;
        /// <summary>
        /// Tlačítko IGNOROVAT
        /// </summary>
        protected WinButtonBase ButtonIgnore;
        /// <summary>
        /// Tlačítko ULOŽIT
        /// </summary>
        protected WinButtonBase ButtonSave;
        /// <summary>
        /// Tlačítko MOŽNÁ
        /// </summary>
        protected WinButtonBase ButtonMaybe;
        /// <summary>
        /// Tlačítko STORNO
        /// </summary>
        protected WinButtonBase ButtonCancel;
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Zobrazené knoflíky
        /// </summary>
        public GUI.GuiDialogButtons Buttons { get { return this._Buttons; } set { this._Buttons = value; this._ShowButtons(value); this.SelectAutoButtons(); this._DetectCloseResponse(); } }
        private GUI.GuiDialogButtons _Buttons;
        /// <summary>
        /// Výsledek okna = tlačítko, které bylo stisknuto a zavřelo formulář
        /// </summary>
        public GUI.GuiDialogButtons GuiResult { get { return this._GuiResult; } }
        private GUI.GuiDialogButtons _GuiResult;
        /// <summary>
        /// Zarovnání buttonů v jejich prostoru
        /// </summary>
        public ContentAlignment ButtonsAlignment { get { return this._ButtonsAlignment; } set { this._ButtonsAlignment = value; this._ShowButtons(this._Buttons); } }
        private ContentAlignment _ButtonsAlignment;
        /// <summary>
        /// Obsahuje true pokud se má okno zavřít po kliknutí na nějaký button.
        /// Pak bude typ buttonu zachycem v <see cref="WinFormButtons.GuiResult"/>.
        /// Pokud aplikace nastaví <see cref="WinFormButtons.CloseOnClick"/> na false, pak se okno samo nezavře, a tlačítko zachyceno nebude. 
        /// Aplikace pak musí obsloužit událost <see cref="WinFormButtons.ButtonClick"/>.
        /// Anebo potomek této třídy může využít háček <see cref="WinFormButtons.OnButtonClick(GPropertyEventArgs{GUI.GuiDialogButtons})"/>.
        /// </summary>
        public bool CloseOnClick { get { return this._CloseOnClick; } set { this._CloseOnClick = value; } }
        private bool _CloseOnClick;
        #endregion
    }
    #endregion
    #region class WinFormBase : Bázová třída pro formuláře
    /// <summary>
    /// WinFormBase : Bázová třída pro formuláře
    /// </summary>
    public class WinFormBase : Form
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public WinFormBase()
        {
            this.IsShown = false;
            this.IsInitialized = false;
            this.ShowInTaskbar = false;
            this.Initialize();
            this.IsInitialized = true;
            this.PrepareLayout();
        }
        /// <summary>
        /// Inicializace formu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected virtual void Initialize()
        {
        }
        /// <summary>
        /// Obsahuje true poté, kdy proběhl konstruktor a v něm kompletní inicializace.
        /// </summary>
        protected bool IsInitialized { get; private set; }
        /// <summary>
        /// Při změně velikosti okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            if (this.IsInitialized)
                this.PrepareLayout();
            base.OnSizeChanged(e);
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti.
        /// Metoda je volána jedenkrát po dokončení inicializace, pak těsně před prvním zobrazením formuláře, a pak po změně rozměrů okna.
        /// Metoda není volaná před dokončením inicializace, když je <see cref="WinFormBase.IsInitialized"/> = false.
        /// Metoda může testovat, zda <see cref="WinFormBase.IsShown"/> je true.
        /// </summary>
        protected virtual void PrepareLayout() { }
        #endregion
        #region Zobrazení
        /// <summary>
        /// Nastaví velikost this formuláře poměrem z velikosti klientské plochy owner formuláře.
        /// Nastaví StartPosition = FormStartPosition.CenterParent;
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="widthRatio"></param>
        /// <param name="heightRatio"></param>
        protected void SetBounds(Form owner, float widthRatio, float heightRatio)
        {
            if (owner != null)
                this.Size = owner.ClientSize.ZoomByRatio(widthRatio, heightRatio);
            this.StartPosition = FormStartPosition.CenterParent;
        }
        /// <summary>
        /// Při zobrazení formuláře
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            if (!this.IsShown)
            {   // Při prvním zobrazení
                this.IsShown = true;
                this.OnFirstShown();
                this.PrepareLayout();
            }
            base.OnShown(e);
        }
        /// <summary>
        /// Je vyvoláno při prvním zobrazení formuláře
        /// </summary>
        protected virtual void OnFirstShown() { }
        /// <summary>
        /// Obsahuje true poté, kdy byl formulář zobrazen.
        /// Platí i v metodě <see cref="PrepareLayout"/>.
        /// </summary>
        protected bool IsShown { get; private set; }
        #endregion
    }
    #endregion
    #region WinButtonImage : třída pro buttony s více obrázky pro různé stavy
    /// <summary>
    /// WinButtonImage : třída pro buttony s více obrázky pro různé stavy
    /// </summary>
    public class WinButtonImage : Button
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public WinButtonImage()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace buttonu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected virtual void Initialize()
        {
        }
        #endregion
        #region Aktivace patřičného Image podle stavu Buttonu
        /// <summary>
        /// MouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            this._IsHot = true;
            base.OnMouseEnter(e);
            this.Refresh();
        }
        /// <summary>
        /// MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            this._IsHot = false;
            base.OnMouseLeave(e);
            this.Refresh();
        }
        /// <summary>
        /// MouseDown
        /// </summary>
        /// <param name="mevent"></param>
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            this._IsDown = true;
            base.OnMouseDown(mevent);
            this.Refresh();
        }
        /// <summary>
        /// MouseUp
        /// </summary>
        /// <param name="mevent"></param>
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            this._IsDown = false;
            base.OnMouseUp(mevent);
            this.Refresh();
        }
        /// <summary>
        /// EnabledChanged
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.Refresh();
        }
        /// <summary>
        /// Aktualizuje Image
        /// </summary>
        public override void Refresh()
        {
            if (!this.Enabled && (_HasImageDisabled || _HasImageStd))
                this.Image = (_HasImageDisabled ? _ImageDisabled : _ImageStd);
            else if (this.Enabled && this._IsHot && !this._IsDown && this._HasImageHot)
                this.Image = this._ImageHot;
            else if (this.Enabled && this._IsDown && this._HasImageDown)
                this.Image = this._ImageDown;
            else if (this.Enabled && _HasImageStd)
                this.Image = this._ImageStd;

            base.Refresh();
        }
        private bool _HasImageStd { get { return (this._ImageStd != null); } }
        private bool _HasImageHot { get { return (this._ImageHot != null); } }
        private bool _HasImageDown { get { return (this._ImageDown != null); } }
        private bool _HasImageDisabled { get { return (this._ImageDisabled != null); } }
        private bool _IsHot;
        private bool _IsDown;
        #endregion
        #region Sady Images
        /// <summary>
        /// Image pro všední den
        /// </summary>
        public Image ImageStd { get { return this._ImageStd; } set { this._ImageStd = value; this.Refresh(); } } private Image _ImageStd;
        /// <summary>
        /// Image pro návštěvu myšky
        /// </summary>
        public Image ImageHot { get { return this._ImageHot; } set { this._ImageHot = value; this.Refresh(); } } private Image _ImageHot;
        /// <summary>
        /// Image pro zmáčknutou myšku
        /// </summary>
        public Image ImageDown { get { return this._ImageDown; } set { this._ImageDown = value; this.Refresh(); } } private Image _ImageDown;
        /// <summary>
        /// Image pro Disabled button
        /// </summary>
        public Image ImageDisabled { get { return this._ImageDisabled; } set { this._ImageDisabled = value; this.Refresh(); } } private Image _ImageDisabled;
        #endregion
    }
    #endregion
    #region WinButtonBase : bázová třída pro buttony
    /// <summary>
    /// WinButtonBase : bázová třída pro buttony
    /// </summary>
    public class WinButtonBase : Button
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public WinButtonBase()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace buttonu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected virtual void Initialize()
        {
            this.AutoSize = false;
            this.ImageAlign = ContentAlignment.MiddleCenter;
            this.TextAlign = ContentAlignment.MiddleRight;
            this.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.UseCompatibleTextRendering = true;
            this.UseMnemonic = true;
            this.UseVisualStyleBackColor = true;
        }
        #endregion
    }
    #endregion
    #region WinHorizontalLine : oddělovací vodorovná linka s textem
    /// <summary>
    ///  WinHorizontalLine : oddělovací vodorovná linka s textem
    /// </summary>
    public class WinHorizontalLine : WinPanel
    {
        #region Privátní život: inicializace, kreslení, určení rozměrů, optimální rozměry
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            base.Initialize();

            this._Caption = "Title";
            this._FontInfo = FontInfo.CaptionBold;
            this._FontInfo.ApplyZoom(1.2f);
            this._TextColor = Color.Black;
            this._LineColorTop = Color.Blue;
            this._LineColorBottom = Color.DarkBlue;
            this.DetectLineHeight();
        }
        /// <summary>
        /// Změna velikosti: zachováme si svou výšku
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            // zrušit? Řešíme pomocí MinimumSize a MaximumSize!
            int width = this.Size.Width;
            int height = this.Size.Height;
            if (height != this.OneLineHeight && this._OnlyOneLine)
                this.Size = new Size(width, this.OneLineHeight);
            base.OnSizeChanged(e);
        }
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle bounds;
            int clientWidth = this.ClientSize.Width;
            int titleWidth = clientWidth;
            int? distanceRight = this.LineDistanceRight;
            if (distanceRight.HasValue && distanceRight.Value > 0)
            {
                titleWidth = titleWidth - distanceRight.Value;
                if (titleWidth < 20) titleWidth = 20;
            }
            int? lineLengthMax = this.LineLengthMax;
            if (lineLengthMax.HasValue && lineLengthMax.Value > 20 && titleWidth > lineLengthMax.Value)
            {
                titleWidth = lineLengthMax.Value;
            }

            bounds = new Rectangle(0, this.TextLineHeight - 2, titleWidth, 1);
            using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, this._LineColorTop, Color.Transparent, 0f))
                e.Graphics.FillRectangle(lgb, bounds);

            bounds = new Rectangle(0, this.TextLineHeight - 1, titleWidth, 1);
            using (LinearGradientBrush lgb = new LinearGradientBrush(bounds, this._LineColorBottom, Color.Transparent, 0f))
                e.Graphics.FillRectangle(lgb, bounds);

            bounds = new Rectangle(2, 1, titleWidth - 4, this.TextLineHeight - 2);
            GPainter.DrawString(e.Graphics, bounds, this._Caption, Skin.Brush(this._TextColor), this._FontInfo, ContentAlignment.MiddleLeft);
        }
        /// <summary>
        /// Určí optimální výšku prvku pro zobrazení v režimu <see cref="OnlyOneLine"/> 
        /// a výšku titulkového textu pro určení pozice vodorovné linky
        /// </summary>
        protected void DetectLineHeight()
        {
            FontInfo fi = this.FontInfo;
            Size size = GPainter.MeasureString("ŽÁČKŮM vydejte", fi);     // Měřím hlavně výšku textu, proto jsou tam písmena "Ž" a "y". O šířku zde nejde.
            int h = size.Height + 4;
            this.TextLineHeight = h;
            h = h + 3;
            this.OneLineHeight = h;
            int minH = h;
            this.MinimumSize = new Size(10, minH);
            int maxH = (this._OnlyOneLine ? minH : 2048);
            this.MaximumSize = new Size(2048, maxH);
            int w = this.Width;
            this.Size = new Size(w, h);
        }
        /// <summary>
        /// Výška textu
        /// </summary>
        protected int TextLineHeight { get; private set; }
        /// <summary>
        /// Výška prvku v režimu <see cref="OnlyOneLine"/> 
        /// </summary>
        protected int OneLineHeight { get; private set; }
        #endregion
        #region Public properties
        /// <summary>
        /// Text v titulkovém řádku
        /// </summary>
        [Description("Text v titulkovém řádku")]
        [Category(WinConstants.DesignCategory)]
        public string Caption
        {
            get { return this._Caption; }
            set { this._Caption = value; this.Refresh(); }
        }
        private string _Caption;
        /// <summary>
        /// Definice fontu
        /// </summary>
        [Category(WinConstants.DesignCategory)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FontInfo FontInfo
        {
            get { return this._FontInfo; }
            set
            {
                if (value == null) return;
                this._FontInfo = value;
                this.DetectLineHeight();
                this.Refresh();
            }
        }
        private FontInfo _FontInfo;
        /// <summary>
        /// Prostor vpravo, do něhož není kreslena linka.
        /// Má smysl tehdy, když vpravo chceme vykreslit něco jiného.
        /// </summary>
        [Category(WinConstants.DesignCategory)]
        [Description("Prostor vpravo, do něhož není kreslena linka. Má význam tehdy, když vpravo chceme vykreslit něco jiného.")]
        [AmbientValue(null)]
        public int? LineDistanceRight
        {
            get { return this._LineDistanceRight; }
            set
            {
                this._LineDistanceRight = (value.HasValue ? (int?)(value.Value < 0 ? 0 : value.Value) : (int?)null);
                this.Refresh();
            }
        }
        private int? _LineDistanceRight;
        /// <summary>
        /// Délka linky maximální.
        /// Má smysl tehdy, když vpravo chceme vykreslit linku jen nad pevně daným obsahem.
        /// </summary>
        [Category(WinConstants.DesignCategory)]
        [Description("Délka linky maximální. Má smysl tehdy, když vpravo chceme vykreslit linku jen nad pevně daným obsahem.")]
        [AmbientValue(null)]
        public int? LineLengthMax
        {
            get { return this._LineLengthMax; }
            set
            {
                this._LineLengthMax = (value.HasValue ? (int?)(value.Value < 0 ? 0 : value.Value) : (int?)null);
                this.Refresh();
            }
        }
        private int? _LineLengthMax;
        /// <summary>
        /// Barva textu v titulkovém řádku
        /// </summary>
        [Description("Barva textu v titulkovém řádku")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Black")]
        public Color TextColor
        {
            get { return this._TextColor; }
            set { this._TextColor = value; this.Refresh(); }
        }
        private Color _TextColor;
        /// <summary>
        /// Barva linky v horní části (světlejší)
        /// </summary>
        [Description("Barva linky v horní části (světlejší)")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Blue")]
        public Color LineColorTop
        {
            get { return this._LineColorTop; }
            set { this._LineColorTop = value; this.Refresh(); }
        }
        private Color _LineColorTop;
        /// <summary>
        /// Barva linky v dolní části (tmavší)
        /// </summary>
        [Description("Barva linky v dolní části (tmavší)")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkBlue")]
        public Color LineColorBottom
        {
            get { return this._LineColorBottom; }
            set { this._LineColorBottom = value; this.Refresh(); }
        }
        private Color _LineColorBottom;
        /// <summary>
        /// Jednořádkový režim
        /// </summary>
        [Description("Jednořádkový režim")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(false)]
        public bool OnlyOneLine
        {
            get { return this._OnlyOneLine; }
            set
            {
                this._OnlyOneLine = value;
                this.DetectLineHeight();
                this.Refresh();
            }
        }
        private bool _OnlyOneLine;
        #endregion
    }
    #endregion
    #region WinPanel : bázová třída pro Panely
    /// <summary>
    /// WinPanel : bázová třída pro Panely
    /// </summary>
    public class WinPanel : Panel
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public WinPanel()
        {
            this.Initialize();
            this.IsInitialized = true;
            this.PrepareLayout();
        }
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// <para/>
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected virtual void Initialize()
        {
            this.IsInitialized = false;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        }
        /// <summary>
        /// Obsahuje true poté, kdy proběhl konstruktor a v něm kompletní inicializace.
        /// </summary>
        protected bool IsInitialized { get; private set; }
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (this.IsInitialized)
                this.PrepareLayout();
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti.
        /// Metoda není volaná před dokončením inicializace, když je <see cref="WinFormBase.IsInitialized"/> = false.
        /// Metoda je volána jedenkrát vyvolaná po dokončení inicializace, a pak při změně velikosti.
        /// </summary>
        protected virtual void PrepareLayout() { }
        #endregion
    }
    #endregion
    #region Constants
    /// <summary>
    /// Konstanty pro WinForm objekty
    /// </summary>
    public class WinConstants
    {
        /// <summary>
        /// Název kategorie "Asseco" v PropertyEditoru
        /// </summary>
        public const string DesignCategory = "Asseco";
    }
    #endregion
}
