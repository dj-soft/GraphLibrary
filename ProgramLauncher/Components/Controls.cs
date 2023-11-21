using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    #region class DialogButtonPanel : Panel s obecnými tlačítky typu DialogButtonType
    /// <summary>
    /// <see cref="DialogButtonPanel"/> : Panel s obecnými tlačítky typu DialogButtonType
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
            this.__LastParentContentBounds = Rectangle.Empty;
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
            this.__LastParentContentBounds = Rectangle.Empty;

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
        private void _SetButtons(DialogButtonType[] buttonTypes)
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
            var buttonSize = __ButtonsSize;
            foreach (var buttonType in buttonTypes)
                newButtons.Add(DButton.Create(this, buttonType, buttonSize, _ButtonClick));

            __DButtons = newButtons.ToArray();
            _LayoutContent(true);
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
            if (sender is Control control && control.Tag is DialogButtonType buttonType)
                _RunDialogButtonClick(buttonType);
        }
        /// <summary>
        /// Po kliknutí na button ...
        /// </summary>
        /// <param name="dialogResult"></param>
        private void _RunDialogButtonClick(DialogButtonType dialogResult)
        {
            DialogButtonEventArgs args = new DialogButtonEventArgs(dialogResult);
            OnDialogButtonClick(args);
            DialogButtonClick?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda volaná po kliknutí na tlačítko
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnDialogButtonClick(DialogButtonEventArgs args) { }
        #endregion
        #region Layout
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// Vlastní akci provede jen při <paramref name="force"/> = true nebo po změně prostoru v parentu.
        /// </summary>
        private void _LayoutContent(bool force)
        {
            var contentBounds = this.CurrentParentContentBounds;
            if (!contentBounds.HasValue || !contentBounds.Value.HasContent()) return;

            if (force || this.__LastParentContentBounds.IsEmpty || this.__LastParentContentBounds != contentBounds.Value)
                _RunLayoutContent(contentBounds.Value);
        }
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// </summary>
        private void _RunLayoutContent(Rectangle contentBounds)
        {
            this.__LastParentContentBounds = contentBounds;

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
            var side = ((PanelContentAlignmentPart)alignment) & PanelContentAlignmentPart.MaskSides;
            bool isControlsVertical = (side == PanelContentAlignmentPart.LeftSide || side == PanelContentAlignmentPart.RightSide);

            // Velikost obsahu = velikost controlů + mezery mezi nimi:
            int contentWidth = (isControlsVertical ? buttonSize.Width : (buttonsCount * buttonSize.Width) + ((buttonsCount - 1) * spacing.Width));
            int contentHeight = (isControlsVertical ? (buttonsCount * buttonSize.Height) + ((buttonsCount - 1) * spacing.Height) : buttonSize.Height);

            // Umístit celý panel: podle velikosti obsahu a zarovnání panelu v parentu:
            var panelWidth = (isControlsVertical ? contentWidth + padding.Horizontal : contentBounds.Width);
            var panelHeight = (isControlsVertical ? contentBounds.Height : contentHeight + padding.Vertical);
            var panelLeft = (side == PanelContentAlignmentPart.RightSide ? contentBounds.Right - panelWidth : contentBounds.Left);
            var panelTop = (side == PanelContentAlignmentPart.BottomSide ? contentBounds.Bottom - panelHeight : contentBounds.Top);
            var panelBounds = new Rectangle(panelLeft, panelTop, panelWidth, panelHeight);
            this.Bounds = panelBounds;

            // Umístit controly:
            var position = ((PanelContentAlignmentPart)alignment) & PanelContentAlignmentPart.MaskContents;
            //  Hodnota 'ratio' vyjadřuje umístění obsahu: "na začátku" (0.0) / "uprostřed" (0.5) / "na konci" (1.0) v odpovídající ose:
            decimal ratio = (position == PanelContentAlignmentPart.BeginContent) ? 0m :
                            (position == PanelContentAlignmentPart.CenterContent) ? 0.5m :
                            (position == PanelContentAlignmentPart.EndContent) ? 1.0m : 0.5m;

            int distance = (int)(Math.Round((ratio * (decimal)(isControlsVertical ? (contentBounds.Height - padding.Vertical - contentHeight) : (contentBounds.Width - padding.Horizontal - contentWidth))), 0));
            int buttonLeft = contentBounds.Left + (isControlsVertical ? padding.Left : padding.Left + distance);
            int buttonTop = contentBounds.Top + (isControlsVertical ? padding.Top + distance : padding.Top);
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
        /// Souřadnice prostoru pro klientský obsah v rámci Parenta. Pokud je null, není znám Parent.
        /// Pokud Parent implementuje <see cref="IContentBounds"/>, pak je zde <see cref="IContentBounds.ContentBounds"/>,
        /// jinak je zde <see cref="Control.ClientRectangle"/>.
        /// </summary>
        private Rectangle? CurrentParentContentBounds
        {
            get
            {
                var parent = this.__Parent;
                if (parent is null) return null;
                if (parent is IContentBounds iContentBounds) return iContentBounds.ContentBounds;
                return parent.ClientRectangle;
            }
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
        protected virtual void OnPanelBoundsChanged(EventArgs args) { }
        /// <summary>
        /// Naposledy akceptovaná velikost Parenta
        /// </summary>
        private Rectangle __LastParentContentBounds;
        /// <summary>
        /// Naposledy použité souřadnice panelu
        /// </summary>
        private Rectangle __PanelBounds;
        #endregion
        #region Public data a eventy
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        public DialogButtonType[] Buttons { get { return __ButtonTypes; } set { _SetButtons(value); } }
        private DialogButtonType[] __ButtonTypes;
        /// <summary>
        /// Vrátí fyzický button daného typu, anebo null.
        /// Typy přítomných buttonů lze setovat do <see cref="Buttons"/>.
        /// </summary>
        /// <param name="buttonType"></param>
        /// <returns></returns>
        public DButton this[DialogButtonType buttonType]
        {
            get
            {
                if (__DButtons is null) return null;
                return __DButtons.FirstOrDefault(b => b.Tag is DialogButtonType result && result == buttonType);
            }
        }
        /// <summary>
        /// Zarovnání panelu v rámci Parenta a umístění obsahu v něm
        /// </summary>
        public PanelContentAlignment PanelContentAlignment { get { return __PanelContentAlignment; } set { __PanelContentAlignment = value; LayoutContent(true); } }
        private PanelContentAlignment __PanelContentAlignment;
        /// <summary>
        /// Velikost buttonů
        /// </summary>
        public Size ButtonsSize { get { return __ButtonsSize; } set { __ButtonsSize = value; LayoutContent(true); } }
        private Size __ButtonsSize;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Size ButtonsSpacing { get { return __ButtonsSpacing; } set { __ButtonsSpacing = value; LayoutContent(true); } }
        private Size __ButtonsSpacing;
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Padding ContentPadding { get { return __ContentPadding; } set { __ContentPadding = value; LayoutContent(true); } }
        private Padding __ContentPadding;
        /// <summary>
        /// Umístí this panel v rámci svého Parenta, a poté umístí svoje Buttony v rámci this panelu. 
        /// Vlastní akci provede jen při <paramref name="force"/> = true nebo po změně prostoru v parentu.
        /// </summary>
        public virtual void LayoutContent(bool force = false) { _LayoutContent(force); }
        /// <summary>
        /// Událost po kliknutí na tlačítko
        /// </summary>
        public event DialogButtonEventHandler DialogButtonClick;
        /// <summary>
        /// Event po změně souřadnic panelu v rámci Parenta
        /// </summary>
        public event EventHandler PanelBoundsChanged;
        #endregion
    }
    #endregion
    #region Bázové controly - DLabel, DTextBox, DFileBox, DButton, DCheckBox, DPanel
    #region DLabel
    /// <summary>
    /// Label
    /// </summary>
    public class DLabel : Label, IControlExtended, IValueStorage
    {
        #region Konstruktor a IControlExtended
        public DLabel()
        {
            this.AutoSize = true;
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Text; } set { this.Text = Conversion.ToString(value); } }
        #endregion
    }
    #endregion
    #region DMemoBox
    /// <summary>
    /// DMemoBox
    /// </summary>
    public class DMemoBox : TextBox, IControlExtended, IValueStorage
    {
        #region Konstruktor a IControlExtended
        public DMemoBox()
        {
            this.Multiline = true;
            this._ActivityMonitorInit();
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        /// <summary>
        /// Vytvoří a vrátí button pro daná data
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        /// <param name="tag"></param>
        /// <param name="click"></param>
        /// <returns></returns>
        public static DMemoBox Create(Control parent, string text, Size size, object tag = null)
        {
            DMemoBox memoBox = new DMemoBox()
            {
                Text = text,
                Size = size,
                Tag = tag
            };

            if (parent != null) parent.Controls.Add(memoBox);
            return memoBox;
        }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Text; } set { this.Text = Conversion.ToString(value); } }
        #endregion
        #region ActivityMonitor
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _ActivityMonitorInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseLeave += _MouseLeave;
            this.Enter += _FocusEnter;
            this.Leave += _FocusLeave;
        }
        /// <summary>
        /// Vstup focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != true);
            __HasFocus = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != false);
            __HasFocus = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vstup myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != true);
            __HasMouse = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != false);
            __HasMouse = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vyvolá událost o změně aktivity
        /// </summary>
        private void _RunActivityChange()
        {
            OnActivityChange();
            ActivityChange?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnActivityChange() { }
        /// <summary>
        /// Event volaný po změně aktivity <see cref="HasFocus"/> nebo <see cref="HasMouse"/>
        /// </summary>
        public event EventHandler ActivityChange;
        /// <summary>
        /// Prvek má focus
        /// </summary>
        public bool HasFocus { get { return __HasFocus; } } private bool __HasFocus;
        /// <summary>
        /// Prvek má myš
        /// </summary>
        public bool HasMouse { get { return __HasMouse; } } private bool __HasMouse;
        /// <summary>
        /// Prvek je aktivní, tedy má focus nebo myš
        /// </summary>
        public bool IsActive { get { return __HasFocus || __HasMouse; } }
        #endregion
    }
    #endregion
    #region DTextBox
    /// <summary>
    /// TextBox
    /// </summary>
    public class DTextBox : TextBox, IControlExtended, IValueStorage
    {
        #region Konstruktor a IControlExtended
        public DTextBox()
        {
            this._ActivityMonitorInit();
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        /// <summary>
        /// Vytvoří a vrátí button pro daná data
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        /// <param name="tag"></param>
        /// <param name="click"></param>
        /// <returns></returns>
        public static DTextBox Create(Control parent, string text, Size size, object tag = null)
        {
            DTextBox textBox = new DTextBox()
            {
                Text = text,
                Size = size,
                Tag = tag
            };

            if (parent != null) parent.Controls.Add(textBox);
            return textBox;
        }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Text; } set { this.Text = Conversion.ToString(value); } }
        #endregion
        #region ActivityMonitor
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _ActivityMonitorInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseLeave += _MouseLeave;
            this.Enter += _FocusEnter;
            this.Leave += _FocusLeave;
        }
        /// <summary>
        /// Vstup focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != true);
            __HasFocus = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != false);
            __HasFocus = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vstup myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != true);
            __HasMouse = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != false);
            __HasMouse = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vyvolá událost o změně aktivity
        /// </summary>
        private void _RunActivityChange()
        {
            OnActivityChange();
            ActivityChange?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnActivityChange() { }
        /// <summary>
        /// Event volaný po změně aktivity <see cref="HasFocus"/> nebo <see cref="HasMouse"/>
        /// </summary>
        public event EventHandler ActivityChange;
        /// <summary>
        /// Prvek má focus
        /// </summary>
        public bool HasFocus { get { return __HasFocus; } }
        private bool __HasFocus;
        /// <summary>
        /// Prvek má myš
        /// </summary>
        public bool HasMouse { get { return __HasMouse; } }
        private bool __HasMouse;
        /// <summary>
        /// Prvek je aktivní, tedy má focus nebo myš
        /// </summary>
        public bool IsActive { get { return __HasFocus || __HasMouse; } }
        #endregion
    }
    #endregion
    #region DFileBox
    /// <summary>
    /// DFileBox
    /// </summary>
    public class DFileBox : DTextButtonBox, IControlExtended, IValueStorage
    {
        #region Soubor, kliknutí na button
        /// <summary>
        /// Obsahuje jméno souboru
        /// </summary>
        public string FileName { get { return TextValue; } set { TextValue = value; } }
        /// <summary>
        /// Defaultní cesta, která se otevře a nabídne v případě, kdy není zadán soubor, a uživatel klikne na button
        /// </summary>
        public string DefaultPath { get; set; }
        /// <summary>
        /// Konkrétní cesta, kterou uživatel vyhledal v dialogu <see cref="OpenFileDialog"/> a potvrdil OK.
        /// Volající aplikace ji odsud může přečíst (defaultně je zde null) a případně uložit na příště, kdy ji předpřipraví do <see cref="DefaultPath"/>.
        /// </summary>
        public string UserSelectedPath { get; private set; }
        /// <summary>
        /// Inicializace buttonu = dáme obrázek
        /// </summary>
        protected override void OnButtonInitialize()
        {
            this.Button.Image = Properties.Resources.folder_16;
        }
        /// <summary>
        /// Po kliknutí na button
        /// </summary>
        protected override void OnButtonClick()
        {
            string fileName = ApplicationData.GetCurrentFilePath(this.FileName);

            bool hasFileName = !String.IsNullOrEmpty(fileName);
            if (hasFileName)
            {
                bool isUriValid = Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out Uri uri);
                hasFileName = isUriValid && uri.IsFile;
            }
            string path = (hasFileName ? System.IO.Path.GetDirectoryName(fileName) : "");
            if (String.IsNullOrEmpty(path)) path = DefaultPath;
            if (String.IsNullOrEmpty(path)) path = UserSelectedPath;
            string name = (hasFileName ? System.IO.Path.GetFileName(fileName) : "");
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.AutoUpgradeEnabled = true;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.InitialDirectory = path;
                dialog.FileName = name;
                dialog.ValidateNames = true;
                dialog.RestoreDirectory = false;

                dialog.Title = "Vyber soubor";
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string selectedFile = dialog.FileName;
                    this.FileName = selectedFile;
                    if (!String.IsNullOrEmpty(selectedFile))
                        this.UserSelectedPath = System.IO.Path.GetDirectoryName(selectedFile);
                }
            }
        }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.FileName; } set { this.FileName = Conversion.ToString(value); } }
        #endregion
    }
    #endregion
    #region DTextButtonBox
    /// <summary>
    /// DTextButtonBox: TextBox + Button (pro FileBox, DateBox atd)
    /// </summary>
    public class DTextButtonBox : DPanel, IControlExtended
    {
        #region Konstruktor, proměnné, vykreslení buttonu ve správném místě a čase
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DTextButtonBox()
        {
            _InitializeControls();
        }
        private DTextBox __TextBox;
        private DButton __Button;
        private Guid? __LazyLayoutGuid;
        private void _InitializeControls()
        {
            __TextBox = DTextBox.Create(this, "", new Size(200, 20));
            __TextBox.Validated += _TextBoxValidated;
            __TextBox.ActivityChange += _SubActivityChange;
            __TextBox.DoubleClick += _TextBoxDoubleClick;
            __TextBox.KeyDown += _TextBoxKeyDown;

            __Button = DButton.Create(this, "", Properties.Resources.zoom_3_16, new Size(20, 20), null, _ButtonClick);
            __Button.TabStop = false;
            __Button.FlatStyle = FlatStyle.Flat;
            __Button.FlatAppearance.BorderSize = 0;
            __Button.Padding = new Padding(0);
            __Button.Margin = new Padding(0);
            __Button.ActivityChange += _SubActivityChange;

            OnButtonInitialize();

            this.ActivityChange += _SubActivityChange;
            DoLayout(false);
        }
        /// <summary>
        /// Po vyhodnocení obsahu TextBoxu opíšeme text do <see cref="TextValue"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _TextBoxValidated(object sender, EventArgs e)
        {
            this.TextValue = __TextBox.Text;
        }
        /// <summary>
        /// Po stisku klávesy: pokud je to Ctrl+Enter, jako by to byl ButtonClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                __TextBox.SelectAll();
                _RunButtonClick();
            }
        }
        /// <summary>
        /// Po DoubleClicku v TextBoxu: jako by to byl ButtonClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxDoubleClick(object sender, EventArgs e)
        {
            __TextBox.SelectAll();
            _RunButtonClick();
        }
        /// <summary>
        /// Po změně aktivity aktualizujeme layout (MouseEnter nebo FocusEnter zobrazí Button, a naopak)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SubActivityChange(object sender, EventArgs e)
        {
            _DoLayoutOnActivityChange();
        }
        private void _ButtonClick(object sender, EventArgs e)
        {
            _RunButtonClick();
            __TextBox.Focus();
        }
        /// <summary>
        /// Prvek je aktivní osobně nebo jeho Text nebo Button
        /// </summary>
        protected bool IsAnyActive { get { return (this.IsActive || this.IsMouseOnRightTexbBox || /* this.IsMouseOnMyBounds || */ __TextBox.IsActive || __Button.IsActive); } }
        protected override void OnActivityChange()
        {
            _DoLayoutOnActivityChange();
        }
        /// <summary>
        /// Zajistí provedení layoutu se zpožděním, po nějaké době
        /// </summary>
        private void _DoLayoutOnActivityChange()
        {
            if (_LayoutActiveIsChanged)
                __LazyLayoutGuid = WatchTimer.CallMeAfter(_DoLayoutOnActivityChangeLazy, 30, true, __LazyLayoutGuid);
        }
        /// <summary>
        /// Vyvolá přepočet layoutu <see cref="DoLayout(bool?)"/>, pokud aktuálně je jiný stav <see cref="IsAnyActive"/> než byl při posledním výpočtu
        /// </summary>
        private void _DoLayoutOnActivityChangeLazy()
        {
            if (_LayoutActiveIsChanged)
                DoLayout(IsAnyActive);
        }
        /// <summary>
        /// Po změně velikosti upravím rozložení prvků uvnitř
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Umístím interní prvky do prostoru this panelu
        /// </summary>
        /// <param name="isActive"></param>
        protected void DoLayout(bool? isActive = null)
        {
            if (__LayoutPending) return;
            __LayoutPending = true;

            // Výška celého panelu se řídí výškou textboxu + okraje:
            var textHeight = this.__TextBox.Height;

            int panelCurrentHeight = this.Height;
            int panelAdd = this.Height - this.ClientSize.Height;
            int panelTargetHeight = textHeight + panelAdd;
            if (panelCurrentHeight != panelTargetHeight)
                this.Height = panelTargetHeight;

            bool buttonVisible = (isActive ?? __Button.VisibleInternal);

            var width = this.ClientSize.Width;

            int buttonWH = textHeight - 1;
            int buttonX = width - (buttonWH + 0);
            __Button.Bounds = new Rectangle(buttonX, 0, buttonWH, buttonWH);

            var textWidth = width - (buttonVisible ? (buttonWH + 0) : 0);
            __TextBox.Bounds = new Rectangle(0, 0, textWidth, textHeight);

            if (isActive.HasValue)
            {
                __Button.Visible = isActive.Value;
                __LayoutIsActive = isActive.Value;
            }

            __LayoutPending = false;
        }
        /// <summary>
        /// Příznak, že právě probíhá <see cref="DoLayout"/>, a nebude se tedy provádět rekurzivně. 
        /// Což by mohlo být: Změna rozměru vnější = OnClientSizeChanged = DoLayout = Korekce výšky panelu (this.Height = panelTargetHeight) = OnClientSizeChanged = DoLayout !
        /// </summary>
        private bool __LayoutPending;
        /// <summary>
        /// Layout prvku je vytvořen pro aktivní stav <see cref="IsAnyActive"/> ?
        /// </summary>
        private bool __LayoutIsActive;
        /// <summary>
        /// Obsahuje true, pokud aktuální stav aktivity <see cref="IsAnyActive"/> je jiný, než pro jaký byl vytvořen layout prvku v metodě <see cref="DoLayout(bool?)"/>.
        /// </summary>
        private bool _LayoutActiveIsChanged
        {
            get
            {
                bool currentIsActive = IsAnyActive;
                bool layoutIsActive = __LayoutIsActive;
                return (currentIsActive != layoutIsActive);
            }
        }
        /// <summary>
        /// Obsahuje true, pokud myš se nachází uvnitř this panelu a přesně na pravém okraji TextBoxu.
        /// To je "mrtvé místo", kdy je myš fyicky uvnitř Panelu, ale přitom podle eventů není ani v TextBoxu a ani v Panelu.
        /// Z TextBoxu už myš odešla (proběhl MouseLeave), ale do Panelu ještě nepřišla (MouseEnter).
        /// Takže proběhne <see cref="OnActivityChange"/>, panel vyhodnotí že myš je úplně mimo panel (protože už není ani v TextBoxu a ještě není v Panelu),
        /// a vykreslí v panelu jen TextBox přes plnou šířku a skryje Button. V tu chvíli ale TextBox bude pod myší a proběhne MouseEnter a následně <see cref="OnActivityChange"/>,
        /// a tak začne panel blikat...
        /// <para/>
        /// Jakmile panel bude do testování <see cref="IsAnyActive"/> zahrnovat i tuto hodnotu <see cref="IsMouseOnRightTexbBox"/>, pak nebude blikat.
        /// </summary>
        protected bool IsMouseOnRightTexbBox
        {
            get
            {
                if (!this.IsHandleCreated || this.Disposing || this.IsDisposed) return false;

                var mousePoint = this.PointToClient(MousePosition);
                var textBoxBounds = this.__TextBox.Bounds;
                var mx = mousePoint.X;
                var my = mousePoint.Y;
                var tr = textBoxBounds.Right;
                var tt = textBoxBounds.Top;
                var tb = textBoxBounds.Bottom;
                return (mx >= (tr - 2) && mx <= tr && my > tt && my < (tb - 1));
            }
        }
        #endregion
        #region Pro potomka
        protected DTextBox TextBox { get { return __TextBox; } }
        protected DButton Button { get { return __Button; } }
        /// <summary>
        /// Obsahuje textovou hodnotu, setování ji vloží i do <see cref="TextValue"/>
        /// </summary>
        public override string Text { get { return __TextValue; } set { __TextBox.Text = value; __TextValue = value; } }
        /// <summary>
        /// Obsahuje textovou hodnotu, setování ji vloží i do controlu
        /// </summary>
        protected virtual string TextValue { get { return __TextValue; } set { __TextValue = value; __TextBox.Text = value; } } private string __TextValue;
        /// <summary>
        /// Umožní potomkovi inicializovat Button
        /// </summary>
        protected virtual void OnButtonInitialize()
        {
            this.Button.Image = Properties.Resources.zoom_3_16;
        }
        /// <summary>
        /// Vyvolá akce po kliknutí na tlačítko
        /// </summary>
        private void _RunButtonClick()
        {
            OnButtonClick();
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po kliknutí na tlačítko
        /// </summary>
        protected virtual void OnButtonClick() { }
        /// <summary>
        /// Po kliknutí na tlačítko
        /// </summary>
        public event EventHandler ButtonClick;
        #endregion
    }
    #endregion
    #region DButton
    /// <summary>
    /// Button
    /// </summary>
    public class DButton : Button, IControlExtended
    {
        #region Konstruktor a IControlExtended
        public DButton()
        {
            _ActivityMonitorInit();
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region Static tvorba
        /// <summary>
        /// Vytvoří a vrátí button pro daný typ
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonType"></param>
        /// <param name="size"></param>
        /// <param name="click"></param>
        /// <returns></returns>
        public static DButton Create(Control parent, DialogButtonType buttonType, Size size, EventHandler click = null)
        {
            switch (buttonType)
            {
                case DialogButtonType.Cancel: return DButton.Create(parent, App.Messages.DialogButtonCancelText, Properties.Resources.dialog_cancel_3_22, size, buttonType, click);
                case DialogButtonType.Abort: return DButton.Create(parent, App.Messages.DialogButtonAbortText, Properties.Resources.process_stop_6_22, size, buttonType, click);
                case DialogButtonType.Retry: return DButton.Create(parent, App.Messages.DialogButtonRetryText, Properties.Resources.view_refresh_4_22, size, buttonType, click);
                case DialogButtonType.Ignore: return DButton.Create(parent, App.Messages.DialogButtonIgnoreText, Properties.Resources.go_next_4_22, size, buttonType, click);
                case DialogButtonType.Yes: return DButton.Create(parent, App.Messages.DialogButtonYesText, Properties.Resources.dialog_ok_4_22, size, buttonType, click);
                case DialogButtonType.No: return DButton.Create(parent, App.Messages.DialogButtonNoText, Properties.Resources.dialog_no_3_22, size, buttonType, click);
                case DialogButtonType.Help: return DButton.Create(parent, App.Messages.DialogButtonHelpText, Properties.Resources.help_3_22, size, buttonType, click);
                case DialogButtonType.Next: return DButton.Create(parent, App.Messages.DialogButtonNextText, Properties.Resources.go_next_3_22, size, buttonType, click);
                case DialogButtonType.Prev: return DButton.Create(parent, App.Messages.DialogButtonPrevText, Properties.Resources.go_previous_3_22, size, buttonType, click);
                case DialogButtonType.Apply: return DButton.Create(parent, App.Messages.DialogButtonApplyText, Properties.Resources.dialog_ok_apply_22, size, buttonType, click);
                case DialogButtonType.Save: return DButton.Create(parent, App.Messages.DialogButtonSaveText, Properties.Resources.media_floppy_3_5_mount_2_22, size, buttonType, click);
                case DialogButtonType.Ok:
                default:
                    return DButton.Create(parent, App.Messages.DialogButtonOkText, Properties.Resources.dialog_clean_22, size, buttonType, click);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí button pro daná data
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="image"></param>
        /// <param name="size"></param>
        /// <param name="tag"></param>
        /// <param name="click"></param>
        /// <returns></returns>
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

            if (parent != null) parent.Controls.Add(button);
            return button;
        }
        #endregion
        #region ActivityMonitor
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _ActivityMonitorInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseLeave += _MouseLeave;
            this.Enter += _FocusEnter;
            this.Leave += _FocusLeave;
        }
        /// <summary>
        /// Vstup focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != true);
            __HasFocus = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != false);
            __HasFocus = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vstup myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != true);
            __HasMouse = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasMouse != false);
            __HasMouse = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vyvolá událost o změně aktivity
        /// </summary>
        private void _RunActivityChange()
        {
            OnActivityChange();
            ActivityChange?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnActivityChange() { }
        /// <summary>
        /// Event volaný po změně aktivity <see cref="HasFocus"/> nebo <see cref="HasMouse"/>
        /// </summary>
        public event EventHandler ActivityChange;
        /// <summary>
        /// Prvek má focus
        /// </summary>
        public bool HasFocus { get { return __HasFocus; } }
        private bool __HasFocus;
        /// <summary>
        /// Prvek má myš
        /// </summary>
        public bool HasMouse { get { return __HasMouse; } }
        private bool __HasMouse;
        /// <summary>
        /// Prvek je aktivní, tedy má focus nebo myš
        /// </summary>
        public bool IsActive { get { return __HasFocus || __HasMouse; } }
        #endregion
    }
    #endregion
    #region DColorBox
    /// <summary>
    /// DColorBox
    /// </summary>
    public class DColorBox : DPanel, IControlExtended, IValueStorage
    {
        #region Konstruktor, proměnné
        public DColorBox()
        {
            _InitializeControls();
        }
        private void _InitializeControls()
        {
            __HsvCheckBox = new GCheckButton();
            __HsvCheckBox.CheckButtonText = "Odstín";
            __HsvCheckBox.CheckButtonChecked = true;
            __HsvCheckBox.CheckButtonCheckedChanged += _HsvCheckBoxChecked;
            this.Controls.Add(__HsvCheckBox);

            __RgbCheckBox = new GCheckButton();
            __RgbCheckBox.CheckButtonText = "RGB";
            __RgbCheckBox.CheckButtonCheckedChanged += _RgbCheckBoxChecked;
            this.Controls.Add(__RgbCheckBox);

            __NonCheckBox = new GCheckButton();
            __NonCheckBox.CheckButtonText = "Žádná";
            __NonCheckBox.CheckButtonCheckedChanged += _NonCheckBoxChecked;
            this.Controls.Add(__NonCheckBox);
           
            __ColorHueTrackBar = new GColorTrackBar();
            __ColorHueTrackBar.ColorsGenerator = CreateColorBlendHue;
            __ColorHueTrackBar.ValueChanged += _ColorHueValueChanged;
            this.Controls.Add(__ColorHueTrackBar);

            __ColorSaturationTrackBar = new GColorTrackBar();
            __ColorSaturationTrackBar.ColorsGenerator = CreateColorBlendSaturation;
            __ColorSaturationTrackBar.ValueChanged += _ColorSaturationValueChanged;
            this.Controls.Add(__ColorSaturationTrackBar);

            __ColorValueTrackBar = new GColorTrackBar();
            __ColorValueTrackBar.ColorsGenerator = CreateColorBlendValue;
            __ColorValueTrackBar.ValueChanged += _ColorValueValueChanged;
            this.Controls.Add(__ColorValueTrackBar);

            __ColorRTrackBar = new GColorTrackBar();
            __ColorRTrackBar.ColorsGenerator = CreateColorRgbR;
            __ColorRTrackBar.ValueChanged += _ColorHueValueChanged;
            this.Controls.Add(__ColorRTrackBar);

            __ColorGTrackBar = new GColorTrackBar();
            __ColorGTrackBar.ColorsGenerator = CreateColorRgbG;
            __ColorGTrackBar.ValueChanged += _ColorSaturationValueChanged;
            this.Controls.Add(__ColorGTrackBar);

            __ColorBTrackBar = new GColorTrackBar();
            __ColorBTrackBar.ColorsGenerator = CreateColorRgbB;
            __ColorBTrackBar.ValueChanged += _ColorValueValueChanged;
            this.Controls.Add(__ColorBTrackBar);

            __ColorAlphaTrackBar = new GColorTrackBar();
            __ColorAlphaTrackBar.ColorsGenerator = CreateColorBlendAlpha;
            __ColorAlphaTrackBar.ValueChanged += _ColorAlphaValueChanged;
            this.Controls.Add(__ColorAlphaTrackBar);

            __GColorSample = new GColorSample();
            __GColorSample.BackHashStyle = HatchStyle.DottedDiamond;
            __GColorSample.CurrentColor = System.Drawing.Color.FromArgb(100, 32, 32, 200);
            this.Controls.Add(__GColorSample);

            this.Height = 60;
            this.ClientSizeChanged += _ClientSizeChanged;

            _SetColorMode(ColorValueMode.Hsv, false);
        }
        private GCheckButton __HsvCheckBox;
        private GCheckButton __RgbCheckBox;
        private GCheckButton __NonCheckBox;

        private GColorTrackBar __ColorHueTrackBar;
        private GColorTrackBar __ColorValueTrackBar;
        private GColorTrackBar __ColorSaturationTrackBar;

        private GColorTrackBar __ColorRTrackBar;
        private GColorTrackBar __ColorGTrackBar;
        private GColorTrackBar __ColorBTrackBar;
        
        private GColorTrackBar __ColorAlphaTrackBar;
        private GColorSample __GColorSample;
        #endregion
        #region Layout
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this.DoLayout();
        }
        private void DoLayout()
        {
            var size = this.ClientSize;

            int checkLeft = 4;
            int checkWidth = 85;

            int trackLeft = checkLeft + checkWidth + 3;
            int trackRight = size.Width - 3;
            int trackSpaceX = trackRight - trackLeft;
            int trackCenterX = trackLeft + (trackSpaceX / 2);
            int sampleWidth = 50;
            int sampleSpace = 4;
            int sampleWidthHalf = sampleWidth / 2;
            int sampleLeft = trackCenterX - sampleWidthHalf;

            int trackWidth = (trackSpaceX - sampleWidth - sampleSpace) / 2;
            int track1Left = trackLeft;
            int track2Left = trackRight - trackWidth;


            int checkHeight = 18;
            int checkSpaceY = 1;
            int trackHeight = 26;
            int trackSpaceY = 4;
            int track1Top = 2;
            int track2Top = trackHeight + trackSpaceY;
            int sampleHeight = 2 * trackHeight + trackSpaceY;

            // CheckBoxy: 2+18+1+18+1+18+2 = 60
            // TrackBary: 2+26+4+26+2 = 60
            int checkTop = 2;
            __HsvCheckBox.Bounds = new Rectangle(checkLeft, checkTop, checkWidth, checkHeight); checkTop = checkTop + checkHeight + checkSpaceY;
            __RgbCheckBox.Bounds = new Rectangle(checkLeft, checkTop, checkWidth, checkHeight); checkTop = checkTop + checkHeight + checkSpaceY;
            __NonCheckBox.Bounds = new Rectangle(checkLeft, checkTop, checkWidth, checkHeight); checkTop = checkTop + checkHeight + checkSpaceY;


            __ColorHueTrackBar.Bounds = new Rectangle(track1Left, track1Top, trackWidth, trackHeight);
            __ColorRTrackBar.Bounds = new Rectangle(track1Left, track1Top, trackWidth, trackHeight);

            __ColorSaturationTrackBar.Bounds = new Rectangle(track2Left, track1Top, trackWidth, trackHeight);
            __ColorGTrackBar.Bounds = new Rectangle(track2Left, track1Top, trackWidth, trackHeight);

            __ColorValueTrackBar.Bounds = new Rectangle(track1Left, track2Top, trackWidth, trackHeight);
            __ColorBTrackBar.Bounds = new Rectangle(track1Left, track2Top, trackWidth, trackHeight);

            __ColorAlphaTrackBar.Bounds = new Rectangle(track2Left, track2Top, trackWidth, trackHeight);
            __GColorSample.Bounds = new Rectangle(sampleLeft, track1Top, sampleWidth, sampleHeight);
        }
        #endregion
        #region Jednotlivé hodnoty, eventy po změně z TrackBarů
        private float _ColorHue { get { return 360f * __ColorHueTrackBar.Value; } set { __ColorHueTrackBar.Value = (value % 360f) / 360f; } }
        private float _ColorSaturation { get { return __ColorSaturationTrackBar.Value; } set { __ColorSaturationTrackBar.Value = _Align(value); } }
        private float _ColorValue { get { return __ColorValueTrackBar.Value; } set { __ColorValueTrackBar.Value = _Align(value); } }
        private float _ColorAlpha { get { return __ColorAlphaTrackBar.Value; } set { __ColorAlphaTrackBar.Value = _Align(value); } }

        private void _HsvCheckBoxChecked(object sender, EventArgs e) { _SetColorMode(ColorValueMode.Hsv, true); }
        private void _RgbCheckBoxChecked(object sender, EventArgs e) { _SetColorMode(ColorValueMode.Rgb, true); }
        private void _NonCheckBoxChecked(object sender, EventArgs e) { _SetColorMode(ColorValueMode.None, true); }
        private void _SetColorMode(ColorValueMode colorMode, bool runChanged)
        {
            bool isChanged = (__ColorMode != colorMode);
            bool isHsv = (colorMode == ColorValueMode.Hsv);
            bool isRgb = (colorMode == ColorValueMode.Rgb);
            bool isNone = (colorMode == ColorValueMode.None);
            __HsvCheckBox.CheckButtonCheckedSilent = isHsv;
            __RgbCheckBox.CheckButtonCheckedSilent = isRgb;
            __NonCheckBox.CheckButtonCheckedSilent = isNone;

            __ColorRTrackBar.Visible = isRgb;
            __ColorGTrackBar.Visible = isRgb;
            __ColorBTrackBar.Visible = isRgb;

            __ColorHueTrackBar.Visible = isHsv;
            __ColorValueTrackBar.Visible = isHsv;
            __ColorSaturationTrackBar.Visible = isHsv;

            __ColorAlphaTrackBar.Visible = (isRgb || isHsv);
            __GColorSample.Visible = (isRgb || isHsv);

            __ColorMode = colorMode;

            if (isChanged && runChanged)
            {

            }
        }
        private void _ColorHueValueChanged(object sender, EventArgs e)
        {
            ColorHSV colorHSV = ColorHSV.FromHSV(_ColorHue, 1d, 1d);
            __ColorSaturationTrackBar.BasicColor = colorHSV.Color;
            __ColorValueTrackBar.BasicColor = colorHSV.Color;
            __ColorAlphaTrackBar.BasicColor = colorHSV.Color;
            _RefreshSample();
        }
        private void _ColorSaturationValueChanged(object sender, EventArgs e)
        {
        }
        private void _ColorValueValueChanged(object sender, EventArgs e)
        {
            ColorHSV colorHSV = ColorHSV.FromHSV(_ColorHue, 1d, _ColorValue);
            __ColorSaturationTrackBar.BasicColor = colorHSV.Color;
            _RefreshSample();
        }
        private void _ColorAlphaValueChanged(object sender, EventArgs e)
        {
            _RefreshSample();
        }

        private void _RefreshSample()
        {
            float hue = _ColorHue;
            float value = _ColorValue;
            float saturation = _ColorSaturation;
            float alpha = _ColorAlpha;
            ColorHSV colorHSV = ColorHSV.FromAHSV(alpha, hue, saturation, value);
            __GColorSample.CurrentColor = colorHSV.Color;
        }
        private static float _Align(float value, float valueMin = 0f, float valueMax = 1f) { return (value > valueMax ? valueMax : (value < valueMin ? valueMin : value)); }
        #endregion


        public ColorValueMode ColorMode { get { return __ColorMode; } set { _SetColorMode(value, true); } } private ColorValueMode __ColorMode;
        public Color? Color { get { return __Color; } set { __Color = value; } } private Color? __Color;
        #region ColorBlenderGeneratory
        protected Color[] CreateColorBlendHue(Color color)
        {
            ColorHSV colorHsv = ColorHSV.FromHSV(0d, 1d, 1d);
            List<Color> colors = new List<Color>();
            for(int angle = 0; angle <= 360; angle += 15)
            {
                colorHsv.Hue = (double)angle;
                float position = (float)angle / 360f;
                colors.Add(colorHsv.Color);
            }
            return colors.ToArray();
        }
        protected Color[] CreateColorBlendSaturation(Color color)
        {
            ColorHSV colorHSV = ColorHSV.FromColor(color);
            colorHSV.Saturation = 0d;
            Color colorGray = colorHSV.Color;
            var colors = new Color[]
            {
                colorGray,
                color
            };
            return colors;
        }
        protected Color[] CreateColorBlendValue(Color color)
        {
            var colors = new Color[]
            {
                System.Drawing.Color.FromArgb(255,255,255),
                color,
                System.Drawing.Color.FromArgb(0,0,0)
            };
            return colors;
        }

        protected Color[] CreateColorRgbR(Color color)
        {
            var colors = new Color[]
            {
                System.Drawing.Color.FromArgb(0,0,0),
                System.Drawing.Color.FromArgb(255,0,0),
            };
            return colors;
        }
        protected Color[] CreateColorRgbG(Color color)
        {
            var colors = new Color[]
            {
                System.Drawing.Color.FromArgb(0,0,0),
                System.Drawing.Color.FromArgb(0,255,0),
            };
            return colors;
        }
        protected Color[] CreateColorRgbB(Color color)
        {
            var colors = new Color[]
            {
                System.Drawing.Color.FromArgb(0,0,0),
                System.Drawing.Color.FromArgb(0,0,255),
            };
            return colors;
        }
        protected Color[] CreateColorBlendAlpha(Color color)
        {
            var colors = new Color[]
            {
                System.Drawing.Color.FromArgb(0, 255,255,255),
                color
            };
            return colors;
        }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Color; } set { this.Color = Conversion.ToColorN(value); } }
        #endregion
    }
    /// <summary>
    /// Režim vstupu barvy
    /// </summary>
    public enum ColorValueMode
    {
        None,
        Rgb,
        Hsv
    }
    #endregion
    #region DCheckBox
    /// <summary>
    /// CheckBox
    /// </summary>
    public class DCheckBox : CheckBox, IControlExtended, IValueStorage
    {
        #region Konstruktor a IControlExtended
        public DCheckBox()
        {
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Checked; } set { this.Checked = Conversion.ToBoolean(value); } }
        #endregion
    }
    #endregion
    #region DPanel
    /// <summary>
    /// Panel
    /// </summary>
    public class DPanel : Panel, IControlExtended
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DPanel()
        {
            this.AutoScroll = true;
            _ActivityMonitorInit();
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #region ActivityMonitor, mírně upravený
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _ActivityMonitorInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseLeave += _MouseLeave;
            this.Enter += _FocusEnter;
            this.Leave += _FocusLeave;
        }
        /// <summary>
        /// Vstup focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusEnter(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != true);
            __HasFocus = true;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod focusu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FocusLeave(object sender, EventArgs e)
        {
            bool isChange = (__HasFocus != false);
            __HasFocus = false;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vstup myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            bool hasMouse = _IsMouseOnMyBounds();
            bool isChange = (__HasMouse != hasMouse);
            __HasMouse = hasMouse;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Odchod myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            bool hasMouse = _IsMouseOnMyBounds();
            bool isChange = (__HasMouse != hasMouse);
            __HasMouse = hasMouse;
            if (isChange) _RunActivityChange();
        }
        /// <summary>
        /// Vrátí true, pokud aktuální pozice myši <see cref="Control.MousePosition"/> se nachází uvnitř this panelu
        /// </summary>
        /// <returns></returns>
        private bool _IsMouseOnMyBounds()
        {
            var mousePoint = this.PointToClient(MousePosition);
            return this.ClientRectangle.Contains(mousePoint);
        }
        /// <summary>
        /// Vyvolá událost o změně aktivity
        /// </summary>
        private void _RunActivityChange()
        {
            OnActivityChange();
            ActivityChange?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnActivityChange() { }
        /// <summary>
        /// Event volaný po změně aktivity <see cref="HasFocus"/> nebo <see cref="HasMouse"/>
        /// </summary>
        public event EventHandler ActivityChange;
        /// <summary>
        /// Prvek má focus
        /// </summary>
        public bool HasFocus { get { return __HasFocus; } }
        private bool __HasFocus;
        /// <summary>
        /// Prvek má myš - přímo nad svým vlastním prostorem.
        /// Obsahuje false, když myš je sice nad this prvkem, ale v místě, kde je umístěn některý můj Child prvek.
        /// Pak lze testovat <see cref="IsMouseOnMyBounds"/>.
        /// </summary>
        public bool HasMouse { get { return __HasMouse; } }
        private bool __HasMouse;
        /// <summary>
        /// Prvek je aktivní, tedy má focus nebo myš
        /// </summary>
        public bool IsActive { get { return __HasFocus || __HasMouse; } }
        /// <summary>
        /// Obsahuje true, pokud aktuální pozice myši <see cref="Control.MousePosition"/> se nachází uvnitř this panelu.
        /// Zde je true i tehdy, když <see cref="HasMouse"/> je false = a to tehdy, když myš je nad některým mým Child prvkem.
        /// </summary>
        /// <returns></returns>
        public bool IsMouseOnMyBounds { get { return _IsMouseOnMyBounds(); } }
        #endregion
    }
    #endregion
    #region DSplitContainer
    /// <summary>
    /// DSplitContainer
    /// </summary>
    public class DSplitContainer : SplitContainer
    {
        #region Konstruktor a IControlExtended
        public DSplitContainer()
        {
            Dock = DockStyle.Fill;
            FixedPanel = FixedPanel.Panel1;
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DToolStrip
    /// <summary>
    /// DToolStrip
    /// </summary>
    public class DToolStrip : ToolStrip
    {
        #region Konstruktor a IControlExtended
        public DToolStrip()
        {
            Dock = DockStyle.Top;
            BackColor = SystemColors.AppWorkspace;
            GripStyle = ToolStripGripStyle.Hidden;
            ImageScalingSize = new Size(48, 48);
            RenderMode = ToolStripRenderMode.System;
            Size = new Size(800, 55);
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DStatusStrip
    /// <summary>
    /// DStatusStrip
    /// </summary>
    public class DStatusStrip : StatusStrip
    {
        #region Konstruktor a IControlExtended
        public DStatusStrip()
        {
            Dock = DockStyle.Bottom;
            BackColor = SystemColors.AppWorkspace;
            ImageScalingSize = new Size(24, 24);
            Size = new Size(800, 22);
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #endregion
    #region Grafické controly (potomci GraphicsControl)
    #region GColorTrackBar
    /// <summary>
    /// GColorTrackBar
    /// </summary>
    public class GColorTrackBar : GTrackBar
    {
        #region Konstruktor, public hodnoty
        public GColorTrackBar() { }
        protected override void OnInitializeTrackBar()
        {
            base.OnInitializeTrackBar();
            this.SetValueMinMax(0f, 1f);
        }
        #endregion
        #region Kreslení a ColorBlender
        /// <summary>
        /// Vykreslíme barevné pozadí pro Tracker
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        protected override void OnPaintTrackerBackground(PaintEventArgs e, Rectangle trackBarBounds, Orientation orientation)
        {
            // Nevoláme: base.OnPaintTrackerBackground(e, trackBarBounds, orientation); - tam se kreslí pozadí pro běžný TrackBar. To my nechceme.

            var colorBlend = ColorBlend;
            if (colorBlend is null) return;
            
            switch (orientation)
            {
                case Orientation.Horizontal:
                    using (var brush = new LinearGradientBrush(new Point(trackBarBounds.Left - 1, trackBarBounds.Y), new Point(trackBarBounds.Right + 1, trackBarBounds.Y), System.Drawing.Color.Blue, System.Drawing.Color.Green))
                    {
                        brush.InterpolationColors = ColorBlend;
                        e.Graphics.FillRectangle(brush, trackBarBounds);
                    }
                    break;

                case Orientation.Vertical:
                    using (var brush = new LinearGradientBrush(new Point(trackBarBounds.X, trackBarBounds.Bottom + 1), new Point(trackBarBounds.X, trackBarBounds.Top - 1), System.Drawing.Color.Blue, System.Drawing.Color.Green))
                    {
                        brush.InterpolationColors = ColorBlend;
                        e.Graphics.FillRectangle(brush, trackBarBounds);
                    }
                    break;
            }
        }
        #endregion
        #region Míchání barev
        /// <summary>
        /// Základní barva, která určuje barvu v paletě pro <see cref="ColorBlenderGenerator"/>. Nejde o barvu zde vybranou, ale o rozsah barev zde vykreslených.
        /// Setování vyvolá vyvolání <see cref="RefreshColorBlend"/> a tedy <see cref="ColorBlenderGenerator"/> = změnu <see cref="ColorBlend"/> a následné překreslení.
        /// </summary>
        public Color BasicColor { get { return __BasicColor; } set { __BasicColor = value; RefreshColorBlend(); } } private Color __BasicColor;
        /// <summary>
        /// Sem musí volající dodat referenci na funkci, která vygeneruje new instanci <see cref="ColorBlend"/>.
        /// </summary>
        public Func<Color, ColorBlend> ColorBlenderGenerator  { get { return __ColorBlenderGenerator; } set { __ColorBlenderGenerator = value; this.RefreshColorBlend(); this.Draw(); } }
        /// <summary>
        /// Sem musí volající dodat referenci na funkci, která vygeneruje pole barev, z něhož bude interně <see cref="ColorBlend"/>.
        /// </summary>
        public Func<Color, Color[]> ColorsGenerator { get { return __ColorsGenerator; } set { __ColorsGenerator = value; this.RefreshColorBlend(); this.Draw(); } }
        /// <summary>
        /// Metoda, která vrátí <see cref="ColorBlend"/> pro tento objekt a danou barvu
        /// </summary>
        private Func<Color, ColorBlend> __ColorBlenderGenerator;
        /// <summary>
        /// Metoda, která vrátí pole <see cref="Color"/> pro tento objekt a danou barvu
        /// </summary>
        private Func<Color, Color[]> __ColorsGenerator;
        /// <summary>
        /// Zajistí znovu vytvoření nástroje na míchání barev <see cref="ColorBlend"/>, jeho uložení do this objektu a na závěr provede překreslení controlu.
        /// </summary>
        public void RefreshColorBlend()
        {
            if (this.ColorBlenderGenerator != null)
                this.ColorBlend = this.ColorBlenderGenerator(this.BasicColor);
            else if (this.ColorsGenerator != null)
                this.ColorBlend = this._CreateColorBlender(this.ColorsGenerator(this.BasicColor));
            this.Draw();
        }
        /// <summary>
        /// Z dodaného pole barev vytvoří a vrátí <see cref="ColorBlend"/>,
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private ColorBlend _CreateColorBlender(Color[] colors)
        {
            int count = colors.Length;
            float last = (float)(count - 1);
            var positions = new float[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = (float)i / last;
            }

            var colorBlend = new ColorBlend(count);
            colorBlend.Colors = colors;
            colorBlend.Positions = positions;

            return colorBlend;
        }
        /// <summary>
        /// Objekt míchající barvy
        /// </summary>
        protected ColorBlend ColorBlend { get; set; }
        #endregion
    }
    #endregion
    #region GColorSample
    /// <summary>
    /// Vzorek barvy
    /// </summary>
    public class GColorSample : GraphicsControl, IControlExtended
    {
        #region Konstruktor, public hodnoty
        public GColorSample()
        {
            _InitializeControl();
        }
        private void _InitializeControl()
        {
            BorderRound = 8;
            BackHashColor1 = Color.FromArgb(160, 160, 160);
            BackHashColor2 = Color.FromArgb(180, 180, 180);
            SampleShape = BasicShapeType.Ellipse;
            BackHashStyle = HatchStyle.Percent25;
        }
        public Color? CurrentColor { get { return __CurrentColor; } set { __CurrentColor = value; this.Draw(); } } private Color? __CurrentColor;
        public Color BackHashColor1 { get { return __BackHashColor1; } set { __BackHashColor1 = value; this.Draw(); } } private Color __BackHashColor1;
        public Color BackHashColor2 { get { return __BackHashColor2; } set { __BackHashColor2 = value; this.Draw(); } } private Color __BackHashColor2;
        public BasicShapeType SampleShape { get { return __SampleShape; } set { __SampleShape = value; this.Draw(); } } private BasicShapeType __SampleShape;
        public HatchStyle? BackHashStyle { get { return __BackHashStyle; } set { __BackHashStyle = value; this.Draw(); } } private HatchStyle? __BackHashStyle;
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region Kreslení
        public int BorderRound { get; set; }
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            base.OnPaintToBuffer(sender, e);

            this.PaintBackground(e);
            this.PaintCurrentColor(e);
        }
        protected void PaintBackground(PaintEventArgs e)
        {
            var backBounds = this.ClientArea;
            var hashStyle = BackHashStyle;
            if (hashStyle.HasValue)
            {
                using (System.Drawing.Drawing2D.HatchBrush hb = new HatchBrush(hashStyle.Value, this.BackHashColor1, this.BackHashColor2))
                {
                    e.Graphics.FillRectangle(hb, backBounds);
                }
            }
        }
        protected void PaintCurrentColor(PaintEventArgs e)
        {
            var color = this.CurrentColor;

            if (color.HasValue)
            {
                var colorBounds = this.ClientArea.Enlarge(-2);
                switch (SampleShape)
                {
                    case BasicShapeType.Rectangle:
                        PaintCurrentColorRectangle(e, color.Value, colorBounds);
                        break;
                    case BasicShapeType.RoundedRectangle:
                        PaintCurrentColorRoundedRectangle(e, color.Value, colorBounds);
                        break;
                    case BasicShapeType.Ellipse:
                        PaintCurrentColorEllipse(e, color.Value, colorBounds);
                        break;
                }
            }
        }
        protected void PaintCurrentColorRectangle(PaintEventArgs e, Color color, Rectangle bounds)
        {
            bounds = bounds.Enlarge(-this.BorderRound);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(App.GetBrush(color), bounds);
        }
        protected void PaintCurrentColorRoundedRectangle(PaintEventArgs e, Color color, Rectangle bounds)
        {
            using (var colorPath = bounds.GetRoundedRectanglePath(this.BorderRound))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(App.GetBrush(color), colorPath);
            }
        }
        protected void PaintCurrentColorEllipse(PaintEventArgs e, Color color, Rectangle bounds)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(App.GetBrush(color), bounds);
        }
        #endregion
    }
    /// <summary>
    /// Základní tvary
    /// </summary>
    public enum BasicShapeType
    {
        None = 0,
        Rectangle,
        RoundedRectangle,
        Ellipse
    }
    #endregion
    #region GTrackBar
    /// <summary>
    /// DColorBox
    /// </summary>
    public class GTrackBar : GraphicsControl, IControlExtended, IValueStorage
    {
        #region Konstruktor, public hodnoty
        public GTrackBar()
        {
            _InitializeControls();
        }
        private void _InitializeControls()
        {
            __ValueMin = 0.0f;
            __ValueMax = 1.0f;
            __Value = 0.5f;
            __TrackBarPadding = new Padding(6, 2, 6, 2);         // Boční jsou větší kvůli přesahu Thumb ikony pod Min a přes Max. Zadání je pro Horizontální orientaci. 
            __ActivePartColor = SystemColors.Highlight;
            __InteractiveState = InteractiveState.Enabled;
            __TrackBarEnabled = true;
            _MouseInit();
            OnInitializeTrackBar();
        }
        /// <summary>
        /// V procesu inicializace si potomek připraví svoje data
        /// </summary>
        protected virtual void OnInitializeTrackBar() { }
        /// <summary>
        /// Počet čárek na trase TrackBaru. Null nebo 0 nebo záporné číslo = nekreslí se.
        /// Maximum = 1/3 pixelů TrackBaru.
        /// </summary>
        public int? TrackBarLines { get { return __TrackBarLines; } set { __TrackBarLines = value; this.Draw(); } } private int? __TrackBarLines;
        public Color ActivePartColor { get { return __ActivePartColor; } set { __ActivePartColor = value; this.Draw(); } } private Color __ActivePartColor;
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region Kreslení jednotlivých segmentů
        /// <summary>
        /// Vykreslení obsahu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            base.OnPaintToBuffer(sender, e);

            var trackBarBounds = TrackBarBounds;
            this.TrackBarActiveBounds = trackBarBounds;
            if (!trackBarBounds.IsEmpty) 
            {
                var orientation = ((trackBarBounds.Width < trackBarBounds.Height) ? Orientation.Vertical : Orientation.Horizontal);

                this.OnPaintTrackerBorder(e, trackBarBounds, orientation);
                this.OnPaintTrackerBackground(e, trackBarBounds, orientation);
                this.OnPaintTrackerLines(e, trackBarBounds, orientation);
                this.OnPaintTrackerThumb(e, trackBarBounds, orientation);
            }
        }
        /// <summary>
        /// Vykreslí rámeček Trackbaru, na souřadnici o 1 větší než <paramref name="trackBarBounds"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        protected virtual void OnPaintTrackerBorder(PaintEventArgs e, Rectangle trackBarBounds, Orientation orientation)
        {
            var textBoxBounds = trackBarBounds.Enlarge(1);
            System.Windows.Forms.TextBoxRenderer.DrawTextBox(e.Graphics, textBoxBounds, System.Windows.Forms.VisualStyles.TextBoxState.Assist);
        }
        /// <summary>
        /// Vykreslí barevné pozadí Trackbaru
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        protected virtual void OnPaintTrackerBackground(PaintEventArgs e, Rectangle trackBarBounds, Orientation orientation)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                if (orientation == Orientation.Horizontal)
                {
                    path.AddLine(trackBarBounds.Left, trackBarBounds.Bottom, trackBarBounds.Right, trackBarBounds.Top - 1);
                    path.AddLine(trackBarBounds.Right, trackBarBounds.Top - 1, trackBarBounds.Right, trackBarBounds.Bottom);
                    path.AddLine(trackBarBounds.Right, trackBarBounds.Bottom, trackBarBounds.Left, trackBarBounds.Bottom);
                }
                else
                {
                    path.AddLine(trackBarBounds.Left, trackBarBounds.Bottom, trackBarBounds.Left, trackBarBounds.Top - 1);
                    path.AddLine(trackBarBounds.Left, trackBarBounds.Top - 1, trackBarBounds.Right, trackBarBounds.Top - 1);
                    path.AddLine(trackBarBounds.Right, trackBarBounds.Top - 1, trackBarBounds.Left, trackBarBounds.Bottom);
                }
                path.CloseFigure();
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.FillPath(App.GetBrush(this.ActivePartColor), path);
            }
        }
        /// <summary>
        /// Vykreslí čárky na hodnotách TrackBaru, podle <see cref="GTrackBar.TrackBarLines"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        protected virtual void OnPaintTrackerLines(PaintEventArgs e, Rectangle trackBarBounds, Orientation orientation)
        {
            var trackBarLines = TrackBarLines;
            if (trackBarLines.HasValue && trackBarLines.Value > 0)
            {
                int maxLines = trackBarBounds.Width / 3;
                if (trackBarLines.Value > maxLines) trackBarLines = maxLines;

                var tickBounds = trackBarBounds;
                tickBounds.Y = tickBounds.Bottom - 4;
                tickBounds.Height = 3;

                TrackBarRenderer.DrawHorizontalTicks(e.Graphics, tickBounds, trackBarLines.Value, System.Windows.Forms.VisualStyles.EdgeStyle.Sunken);
            }
        }
        /// <summary>
        /// Vykreslí Thumb = button
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        protected virtual void OnPaintTrackerThumb(PaintEventArgs e, Rectangle trackBarBounds, Orientation orientation)
        {
            var image = GetThumbImage(orientation);
            if (image is null) return;

            var center = GetPixelFromValue(this.Value, trackBarBounds, orientation);
            var thumbBounds = center.GetRectangleFromCenter(11, 11);
            e.Graphics.DrawImage(image, thumbBounds.Location);

            this.TrackBarActiveThumb = thumbBounds.Enlarge(3);
        }
        /// <summary>
        /// Souřadnice vnitřního prostoru TrackBaru
        /// </summary>
        protected virtual Rectangle TrackBarBounds
        {
            get
            {
                var size = this.ClientSize;
                var padding = __TrackBarPadding;
                var minLength = TrackBarMinLength;
                var minThick = TrackBarMinThick;
                if (size.Width > size.Height)
                {   // Horizontal
                    int x = padding.Left;
                    int w = size.Width - padding.Horizontal;
                    int y = padding.Top;
                    int h = size.Height - padding.Vertical;
                    if (w >= minLength && h >= minThick)
                        return new Rectangle(x, y, w, h);
                }
                else
                {   // Vertical: otočíme význam Padding, protože ten je logicky postaven na orientaci Horizontal:
                    int x = padding.Top;
                    int w = size.Width - padding.Vertical;
                    int y = padding.Bottom;
                    int h = size.Height - padding.Horizontal;
                    if (h >= minLength && w >= minThick)
                        return new Rectangle(x, y, w, h);
                }
                return Rectangle.Empty;
            }
        }
        /// <summary>
        /// Orientace TrackBaru, je odvozena od fyzických souřadnic trackeru.
        /// </summary>
        protected virtual Orientation TrackBarOrientation { get { var size = this.ClientSize; return ((size.Width < size.Height) ? Orientation.Vertical : Orientation.Horizontal); } }
        /// <summary>
        /// true pokud rozměr TrackBaru je tak malý, že nebude kreslen
        /// </summary>
        protected virtual bool IsEmpty { get { var bounds = this.TrackBarBounds; return bounds.IsEmpty; } }
        /// <summary>
        /// Vnitřní okraje okolo TrackBaru. Jsou zadány pro polohu Horizontálně, tedy Left je před MinValue a Right je za MaxValue.
        /// </summary>
        protected virtual Padding TrackBarPadding { get { return __TrackBarPadding; } set { __TrackBarPadding = value; this.Draw(); } } private Padding __TrackBarPadding;
        /// <summary>
        /// Nejmenší délka aktivní části TrackBaru. Pokud reálná bude menší, nebude TrackBar kreslen a <see cref="IsEmpty"/> bude true.
        /// </summary>
        protected virtual int TrackBarMinLength { get { return 20; } }
        /// <summary>
        /// Nejmenší šířka aktivní části TrackBaru. Pokud reálná bude menší, nebude TrackBar kreslen a <see cref="IsEmpty"/> bude true.
        /// </summary>
        protected virtual int TrackBarMinThick { get { return 8; } }
        /// <summary>
        /// Vrátí Image pro aktuální TrackBar
        /// </summary>
        /// <returns></returns>
        protected Image GetThumbImage()
        {
            return GetThumbImage(TrackBarOrientation);
        }
        /// <summary>
        /// Vrátí Image pro aktuální TrackBar
        /// </summary>
        /// <returns></returns>
        protected virtual Image GetThumbImage(Orientation orientation)
        {
            InteractiveState state = this.InteractiveState & InteractiveState.MaskBasicStates;
            bool isHorizontal = (orientation == Orientation.Horizontal);
            switch (state)
            {
                case InteractiveState.Disabled: return Properties.Resources.thumb_h_d2_11;
                case InteractiveState.Enabled: return Properties.Resources.thumb_h_b3_11;
                case InteractiveState.MouseOn: return Properties.Resources.thumb_h_v3_11;
                case InteractiveState.MouseDown: return Properties.Resources.thumb_h_v4_11;
            }
            return Properties.Resources.thumb_h_b3_11;
        }
        #endregion
        #region Interaktivita
        private void _MouseInit()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseLeave += _MouseLeave;
        }
        private void _MouseEnter(object sender, EventArgs e)
        {
            if (TrackBarEnabled)
                InteractiveState = InteractiveState.MouseOn;
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && TrackBarEnabled && this.InteractiveState == InteractiveState.MouseDown)
                this.InteractiveValueChange(e.Location);
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && TrackBarEnabled && IsInActiveBounds(e.Location))
            {
                this.InteractiveState = InteractiveState.MouseDown;
                this.InteractiveValueChange(e.Location);
            }
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            bool isMouseOnControl = this.ClientRectangle.Contains(e.Location);
            InteractiveState = (isMouseOnControl ? InteractiveState.MouseOn : InteractiveState.Enabled);
        }
        /// <summary>
        /// Myš odchází z Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            InteractiveState = InteractiveState.Enabled;
        }
        /// <summary>
        /// Aktuální interaktivní stav
        /// </summary>
        protected InteractiveState InteractiveState 
        { 
            get { return (__TrackBarEnabled ? __InteractiveState : InteractiveState.Disabled); } 
            set
            {
                var oldState = __InteractiveState;
                var newState = value;
                if (oldState != newState) 
                {
                    __InteractiveState = newState;
                    __TrackBarEnabled = (newState != InteractiveState.Disabled);
                    OnInteractiveStateChanged();
                    InteractiveStateChanged?.Invoke(this, EventArgs.Empty);
                    this.Draw();
                }
            }
        } 
        private InteractiveState __InteractiveState;
        /// <summary>
        /// Došlo ke změně interaktivního stavu
        /// </summary>
        protected virtual void OnInteractiveStateChanged() { }
        public event EventHandler InteractiveStateChanged;
        /// <summary>
        /// TrackBar je Enabled?
        /// </summary>
        public bool TrackBarEnabled { get { return __TrackBarEnabled; } set { __TrackBarEnabled = value; this.Draw(); } } private bool __TrackBarEnabled;
        /// <summary>
        /// Metoda má přepočítat aktuální hodnotu podle souřadnice myši, a zavolat vykreslení controlu.
        /// </summary>
        /// <param name="mousePoint"></param>
        protected virtual void InteractiveValueChange(Point mousePoint)
        {
            if (__TrackBarEnabled) 
            {
                this.Value = GetValueFromPixel(mousePoint);
                this.Draw();
            }
        }
        /// <summary>
        /// Vrátí true, pokud daný bod je umístěn v aktivním prostoru
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected bool IsInActiveBounds(Point point)
        {
            return this.TrackBarActiveBounds.Contains(point) || this.TrackBarActiveThumb.Contains(point);
        }
        /// <summary>
        /// Souřadnice aktivního prostoru TrackBaru
        /// </summary>
        protected Rectangle TrackBarActiveBounds { get; set; }
        /// <summary>
        /// Souřadnice aktivního prostoru Thumbu
        /// </summary>
        protected Rectangle TrackBarActiveThumb { get; set; }
        #endregion
        #region Value a přepočty
        /// <summary>
        /// Obsahuje aktuální hodnotu v trackeru
        /// </summary>
        public float Value { get { return __Value; } set { SetValue(value); } } private float __Value;
        /// <summary>
        /// Obsahuje minimální hodnotu dostupnou v trackeru.Default = 0.0f
        /// </summary>
        public float ValueMin { get { return __ValueMin; } set { SetValueMinMax(value, this.ValueMax); } } private float __ValueMin;
        /// <summary>
        /// Obsahuje maximální hodnotu dostupnou v trackeru
        /// </summary>
        public float ValueMax { get { return __ValueMax; } set { SetValueMinMax(this.ValueMin, value); } } private float __ValueMax;
        /// <summary>
        /// Nastaví hodnotu Value, zarovnanou do aktuálních mezí.
        /// Po změně hodnoty vyvolá event.
        /// </summary>
        /// <param name="value"></param>
        protected void SetValue(float value)
        {
            float newValue = AlignValue(value);
            bool isChanged = !(newValue == __Value);
            __Value = newValue;
            if (isChanged)
            {
                EventArgs e = EventArgs.Empty;
                OnValueChanged(this, e);
                ValueChanged?.Invoke(this, e);
            }
        }
        /// <summary>
        /// Vrátí zadanou hodnotu <paramref name="value"/> do rozsahu <see cref="ValueMin"/> ÷ <see cref="ValueMax"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected float AlignValue(float value)
        {
            return (value > ValueMax ? ValueMax : (value < ValueMin ? ValueMin : value));
        }
        /// <summary>
        /// Po změně hodnoty ve <see cref="Value"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnValueChanged(object sender, EventArgs e) { }
        /// <summary>
        /// Po změně hodnoty ve <see cref="Value"/>
        /// </summary>
        public event EventHandler ValueChanged;
        /// <summary>
        /// Nastaví hodnoty ValueMin a ValueMax
        /// </summary>
        /// <param name="valueMin"></param>
        /// <param name="valueMax"></param>
        protected void SetValueMinMax(float valueMin, float valueMax)
        {
            __ValueMin = valueMin;
            valueMin += 0.001f;                          // Nejmenší rozdíl hodnot
            __ValueMax = (valueMax > valueMin ? valueMax : valueMin);
            SetValue(Value);
        }
        /// <summary>
        /// Vrátí Value odpovídající dané souřadnici (v koordinátech this controlu)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected float GetValueFromPixel(Point point)
        {
            float value = __Value;
            var trackBarBounds = TrackBarBounds;
            if (!trackBarBounds.IsEmpty)
            {
                var orientation = ((trackBarBounds.Width < trackBarBounds.Height) ? Orientation.Vertical : Orientation.Horizontal);
                return GetValueFromPixel(point, trackBarBounds, orientation);
            }
            return value;
        }
        /// <summary>
        /// Vrátí Value odpovídající dané souřadnici (v koordinátech this controlu)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        protected float GetValueFromPixel(Point point, Rectangle trackBarBounds, Orientation orientation)
        {
            // Vzdálenost bodu point od základí souřadnice, podle orientace: Horizontal = Left, doprava, Vertical = Bottom, nahoru:
            int dist = (orientation == Orientation.Horizontal ? point.X - trackBarBounds.Left : trackBarBounds.Bottom - point.Y);

            // Velikost aktivního prostoru (poslední pixel je fyzicky nedostupný):
            int size = (orientation == Orientation.Horizontal ? trackBarBounds.Width : trackBarBounds.Height) - 1;

            // Pozice (0 ÷ 1) ve vizuálním rozsahu:
            float ratio = (float)dist / (float)size;

            float valueMin = this.ValueMin;
            float valueMax = this.ValueMax;
            float value = valueMin + (ratio * (valueMax - valueMin));
            return AlignValue(value);
        }
        /// <summary>
        /// Vrátí souřadnici (v koordinátech this controlu) odpovídající dané hodnotě
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected Point GetPixelFromValue(float value)
        {
            Point point = Point.Empty;
            var trackBarBounds = TrackBarBounds;
            if (!trackBarBounds.IsEmpty)
            {
                var orientation = ((trackBarBounds.Width < trackBarBounds.Height) ? Orientation.Vertical : Orientation.Horizontal);
                point = GetPixelFromValue(value, trackBarBounds, orientation);
            }
            return point;
        }
        /// <summary>
        /// Vrátí souřadnici (v koordinátech this controlu) odpovídající dané hodnotě
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trackBarBounds"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        protected Point GetPixelFromValue(float value, Rectangle trackBarBounds, Orientation orientation)
        {
            // Vypočtu ratio = odpovídá pozici Value v prostoru (ValueMin ÷ ValueMax):
            float valueMin = this.ValueMin;
            float valueMax = this.ValueMax;
            float ratio = (AlignValue(value) - valueMin) / (valueMax - valueMin);

            // Velikost aktivního prostoru (poslední pixel je fyzicky nedostupný), vzdálenost hodnoty od počátku:
            int size = (orientation == Orientation.Horizontal ? trackBarBounds.Width : trackBarBounds.Height) - 1;
            int dist = (int)(Math.Round(ratio * (double)size, 0));

            // Souřadnice poloviny v neaktivním směru (pro Horizontal: svislý střed TrackBaru, pro Vertical: vodorovný střed)
            int half = (orientation == Orientation.Horizontal ? trackBarBounds.Y + (trackBarBounds.Height / 2) : trackBarBounds.X + (trackBarBounds.Width / 2));

            return (orientation == Orientation.Horizontal ?
                new Point(trackBarBounds.X + dist, half) :
                new Point(half, trackBarBounds.Bottom - dist));
        }
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.Value; } set { this.Value = Conversion.ToSingle(value); } }
        #endregion
    }
    #endregion
    #region GCheckButton
    public class GCheckButton : GraphicsControl, IControlExtended, IValueStorage
    {
        #region Konstruktor, public hodnoty
        public GCheckButton()
        {
            _MouseInit();
            _LayoutInit();
            _StateInit();
        }
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsVisibleInternal(); } set { this.Visible = value; } }
        #endregion
        #region Interaktivita
        private void _MouseInit()
        {
            __InteractiveState = InteractiveState.Enabled;
            __CheckBoxEnabled = true;
            this.MouseEnter += _MouseEnter;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseLeave += _MouseLeave;
        }
        private void _MouseEnter(object sender, EventArgs e)
        {
            if (CheckBoxEnabled)
                InteractiveState = InteractiveState.MouseOn;
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && CheckBoxEnabled && IsInActiveBounds(e.Location))
            {
                this.InteractiveState = InteractiveState.MouseDown;
                this.CheckButtonChecked = !this.CheckButtonChecked;            // Vyvolá event + Draw
            }
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            bool isMouseOnControl = this.ClientRectangle.Contains(e.Location);
            InteractiveState = (isMouseOnControl ? InteractiveState.MouseOn : InteractiveState.Enabled);
        }
        /// <summary>
        /// Myš odchází z Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            InteractiveState = InteractiveState.Enabled;
        }
        /// <summary>
        /// Aktuální interaktivní stav
        /// </summary>
        protected InteractiveState InteractiveState
        {
            get { return (CheckBoxEnabled ? __InteractiveState : InteractiveState.Disabled); }
            set
            {
                var oldState = __InteractiveState;
                var newState = value;
                if (oldState != newState)
                {
                    __InteractiveState = newState;
                    __CheckBoxEnabled = (newState != InteractiveState.Disabled);
                    OnInteractiveStateChanged();
                    InteractiveStateChanged?.Invoke(this, EventArgs.Empty);
                    this.Draw();
                }
            }
        }
        private InteractiveState __InteractiveState;
        /// <summary>
        /// Došlo ke změně interaktivního stavu
        /// </summary>
        protected virtual void OnInteractiveStateChanged() { }
        public event EventHandler InteractiveStateChanged;
        /// <summary>
        /// CheckBox je Enabled?
        /// </summary>
        public bool CheckBoxEnabled { get { return __CheckBoxEnabled; } set { __CheckBoxEnabled = value; this.Draw(); } } private bool __CheckBoxEnabled;
        /// <summary>
        /// Vrátí true, pokud daný bod je umístěn ve zdejším prostoru
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected bool IsInActiveBounds(Point point)
        {
            return this.ClientRectangle.Contains(point);
        }
        #endregion
        #region Kreslení jednotlivých segmentů
        /// <summary>
        /// Vykreslení obsahu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            base.OnPaintToBuffer(sender, e);

            this.OnPaintCheckBoxBackground(e);
            this.OnPaintCheckBoxImage(e);
            this.OnPaintCheckBoxText(e);
        }
        protected void OnPaintCheckBoxBackground(PaintEventArgs e)
        {
            e.Graphics.FillInteractiveBackArea(this.ClientRectangle, App.CurrentAppearance.ButtonBackColors, this.InteractiveState);
        }
        protected void OnPaintCheckBoxImage(PaintEventArgs e)
        {
            var image = GetImage();
            if (image != null)
                e.Graphics.DrawImage(image, this.CheckBoxImageBounds);
        }
        protected void OnPaintCheckBoxText(PaintEventArgs e)
        {
            var text = CheckButtonText;
            if (!String.IsNullOrEmpty(text))
                e.Graphics.DrawText(text, this.CheckBoxTextBounds, App.CurrentAppearance.StandardTextAppearance, this.InteractiveState, null, ContentAlignment.MiddleLeft);
        }
        protected Image GetImage()
        {
            bool isChecked = CheckButtonChecked;
            var image = (isChecked ? CheckButtonImageTrue : CheckButtonImageFalse);
            if (image is null)
                image = App.GetImage(CheckButtonImageName);
            return image;
        }
        #endregion
        #region Layout
        private void _LayoutInit()
        {
            this.__CheckButtonPadding = new Padding(3);
            this.ClientSizeChanged += _ClientSizeChanged;
        }

        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            DoLayout();
        }
        protected virtual void DoLayout()
        {
            var padding = this.CheckButtonPadding;
            var clientSize = this.ClientSize;
            int x = padding.Left;
            int y = padding.Top;
            var h = clientSize.Height - padding.Vertical;

            var image = GetImage();
            if (image != null)
            {
                this.CheckBoxImageBounds = new Rectangle(x, y, h, h);
                x = x + h + 3;
            }
            else
            {
                this.CheckBoxImageBounds = new Rectangle(x, y, 0, h);
            }

            int w = clientSize.Width - padding.Right - x;
            this.CheckBoxTextBounds = new Rectangle(x, y, w, h);

            Draw();
        }
        /// <summary>
        /// Souřadnice prostoru Image boxu
        /// </summary>
        protected Rectangle CheckBoxImageBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru Textu
        /// </summary>
        protected Rectangle CheckBoxTextBounds { get; set; }
        #endregion
        #region Hodnota, Ikona, text
        private void _StateInit()
        {
            __CheckButtonText = "Check";
            __CheckButtonChecked = false;
            __CheckButtonImageFalse = Properties.Resources.btn_g4_20;
            __CheckButtonImageTrue = Properties.Resources.btn_24_20;
        }
        public string CheckButtonText { get { return __CheckButtonText; } set { __CheckButtonText = value; Draw(); } } private string __CheckButtonText;
        public bool CheckButtonChecked { get { return __CheckButtonChecked; } set { _SetCheckButtonChecked(value, true); Draw(); } }
        public bool CheckButtonCheckedSilent { get { return __CheckButtonChecked; } set { _SetCheckButtonChecked(value, false); } } 
        private void _SetCheckButtonChecked(bool value, bool runChanged)
        {
            if (__CheckButtonChecked == value) return;
            __CheckButtonChecked = value;
            if (runChanged)
            {
                OnCheckButtonCheckedChanged();
                CheckButtonCheckedChanged?.Invoke(this, EventArgs.Empty);
            }
            this.Draw();
        }
        private bool __CheckButtonChecked;
        protected virtual void OnCheckButtonCheckedChanged() { }
        public event EventHandler CheckButtonCheckedChanged;
        public Image CheckButtonImageFalse { get { return __CheckButtonImageFalse; } set { __CheckButtonImageFalse = value; DoLayout(); } } private Image __CheckButtonImageFalse;
        public Image CheckButtonImageTrue { get { return __CheckButtonImageTrue; } set { __CheckButtonImageTrue = value; DoLayout(); } } private Image __CheckButtonImageTrue;
        public string CheckButtonImageName { get { return __CheckButtonImageName; } set { __CheckButtonImageName = value; DoLayout(); } } private string __CheckButtonImageName;
        public Padding CheckButtonPadding { get { return __CheckButtonPadding; } set { __CheckButtonPadding = value; DoLayout(); } } private Padding __CheckButtonPadding;
        #endregion
        #region IValueStorage
        /// <summary>
        /// Přístup na hodnotu
        /// </summary>
        object IValueStorage.Value { get { return this.CheckButtonChecked; } set { this.CheckButtonChecked = Conversion.ToBoolean(value); } }
        #endregion
    }
    #endregion
    #endregion
    #region Interface a delegates
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
    public interface IContentBounds
    {
        /// <summary>
        /// Obsahuje souřadnice, do kterých smí být umístěn obsah.
        /// </summary>
        Rectangle ContentBounds { get; }
    }
    /// <summary>
    /// Událost, která přináší výsledek dialogu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DialogButtonEventHandler(object sender, DialogButtonEventArgs args);
    /// <summary>
    /// Argument pro událost typu <see cref="DialogButtonEventHandler"/>
    /// </summary>
    public class DialogButtonEventArgs : EventArgs
    {
        public DialogButtonEventArgs(DialogButtonType dialogResult)
        {
            this.DialogButtonType = dialogResult;
        }
        /// <summary>
        /// Výsledek dialogu
        /// </summary>
        public DialogButtonType DialogButtonType { get; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Typ buttonu v dialogu.
    /// </summary>
    public enum DialogButtonType
    {
        None,
        Ok,
        Apply,
        Save,
        Cancel,
        Yes,
        No,
        Abort,
        Retry,
        Ignore,
        Prev,
        Next,
        Help
    }
    /// <summary>
    /// Umístění panelu a jeho obsahu
    /// </summary>
    public enum PanelContentAlignment : int
    {
        None = 0,
        TopSideLeft = PanelContentAlignmentPart.TopSide | PanelContentAlignmentPart.BeginContent,
        TopSideCenter = PanelContentAlignmentPart.TopSide | PanelContentAlignmentPart.CenterContent,
        TopSideRight = PanelContentAlignmentPart.TopSide | PanelContentAlignmentPart.EndContent,
        RightSideTop = PanelContentAlignmentPart.RightSide | PanelContentAlignmentPart.BeginContent,
        RightSideMiddle = PanelContentAlignmentPart.RightSide | PanelContentAlignmentPart.CenterContent,
        RightSideBottom = PanelContentAlignmentPart.RightSide | PanelContentAlignmentPart.EndContent,
        BottomSideRight = PanelContentAlignmentPart.BottomSide | PanelContentAlignmentPart.EndContent,
        BottomSideCenter = PanelContentAlignmentPart.BottomSide | PanelContentAlignmentPart.CenterContent,
        BottomSideLeft = PanelContentAlignmentPart.BottomSide | PanelContentAlignmentPart.BeginContent,
        LeftSideBottom = PanelContentAlignmentPart.LeftSide | PanelContentAlignmentPart.EndContent,
        LeftSideMiddle = PanelContentAlignmentPart.LeftSide | PanelContentAlignmentPart.CenterContent,
        LeftSideTop = PanelContentAlignmentPart.LeftSide | PanelContentAlignmentPart.BeginContent
    }
    /// <summary>
    /// Složky hodnot enumu <see cref="PanelContentAlignment"/> pro umístění panelu a jeho obsahu
    /// </summary>
    [Flags]
    public enum PanelContentAlignmentPart : int
    {
        None = 0,
        LeftSide = 0x0001,
        TopSide = 0x0002,
        RightSide = 0x0004,
        BottomSide = 0x0008,
        BeginContent = 0x0100,
        CenterContent = 0x0200,
        EndContent = 0x0400,

        MaskVerticals = LeftSide | RightSide,
        MaskHorizontals = TopSide | BottomSide,
        MaskSides = LeftSide | TopSide | RightSide | BottomSide,
        MaskContents = BeginContent | CenterContent | EndContent
    }
    #endregion
    #region Servis pro potomky a ostatní
    public class ControlSupport
    {
        /// <summary>
        /// Vytvoří control daného typu a přidá jej do parent controlu
        /// </summary>
        /// <param name="controlType"></param>
        /// <param name="text"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Control CreateControl(ControlType controlType, string text = null, Control parent = null)
        {
            Control control = null;
            switch (controlType)
            {
                case ControlType.None:
                    return null;
                case ControlType.Label:
                    var label = new DLabel() { Text = text };
                    control = label;
                    break;
                case ControlType.Button:
                    var button = new DButton() { Text = text };
                    control = button;
                    break;
                case ControlType.TextBox:
                case ControlType.DateBox:
                case ControlType.IntegerBox:
                case ControlType.DecimalBox:
                    var textBox = new DTextBox() { Text = text };
                    control = textBox;
                    break;
                case ControlType.MemoBox:
                    var memoBox = new DMemoBox() { Text = text };
                    control = memoBox;
                    break;
                case ControlType.FileBox:
                    var fileBox = new DFileBox() { Text = text };
                    control = fileBox;
                    break;
                case ControlType.ColorBox:
                    var colorBox = new DColorBox() { Text = text };
                    control = colorBox;
                    break;
                case ControlType.CheckBox:
                    var checkBox = new DCheckBox() { Text = text };
                    control = checkBox;
                    break;
            }
            if (control != null && parent != null) parent.Controls.Add(control);
            return control;
        }
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
        public static Size StandardButtonSize { get { return new Size(120, 36); } }
        /// <summary>
        /// Standardní odstupy buttonů
        /// </summary>
        public static Size StandardButtonSpacing { get { return new Size(9, 6); } }
        /// <summary>
        /// Standardní okraje uvnitř panelu
        /// </summary>
        public static Padding StandardContentPadding { get { return new Padding(12, 6, 12, 6); } }
    }
    public enum ControlType
    {
        None,
        Label,
        Button,
        TextBox,
        MemoBox,
        DateBox,
        IntegerBox,
        DecimalBox,
        ComboBox,
        FileBox,
        CheckBox,
        ColorBox,
        Image
    }
    #endregion
}
