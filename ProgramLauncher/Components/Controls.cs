using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            var side = ((PanelContentAlignmentPart)alignment) & PanelContentAlignmentPart.MaskSides;
            bool isControlsVertical = (side == PanelContentAlignmentPart.LeftSide || side == PanelContentAlignmentPart.RightSide);

            // Velikost obsahu = velikost controlů + mezery mezi nimi:
            int contentWidth = (isControlsVertical ? buttonSize.Width : (buttonsCount * buttonSize.Width) + ((buttonsCount - 1) * spacing.Width));
            int contentHeight = (isControlsVertical ? (buttonsCount * buttonSize.Height) + ((buttonsCount - 1) * spacing.Height) : buttonSize.Height);

            // Umístit celý panel: podle velikosti obsahu a zarovnání panelu v parentu:
            var panelWidth = (isControlsVertical ? contentWidth + padding.Horizontal : parentSize.Width);
            var panelHeight = (isControlsVertical ? parentSize.Height : contentHeight + padding.Vertical);
            var panelLeft = (side == PanelContentAlignmentPart.RightSide ? parentSize.Width - panelWidth : 0);
            var panelTop = (side == PanelContentAlignmentPart.BottomSide ? parentSize.Height - panelHeight : 0);
            var panelBounds = new Rectangle(panelLeft, panelTop, panelWidth, panelHeight);
            this.Bounds = panelBounds;

            // Umístit controly:
            var position = ((PanelContentAlignmentPart)alignment) & PanelContentAlignmentPart.MaskContents;
            //  Hodnota 'ratio' vyjadřuje umístění obsahu: "na začátku" (0.0) / "uprostřed" (0.5) / "na konci" (1.0) v odpovídající ose:
            decimal ratio = (position == PanelContentAlignmentPart.BeginContent) ? 0m :
                            (position == PanelContentAlignmentPart.CenterContent) ? 0.5m :
                            (position == PanelContentAlignmentPart.EndContent) ? 1.0m : 0.5m;

            int distance = (int)(Math.Round((ratio * (decimal)(isControlsVertical ? (parentSize.Height - padding.Vertical - contentHeight) : (parentSize.Width - padding.Horizontal - contentWidth))), 0));
            int buttonLeft = isControlsVertical ? padding.Left : padding.Left + distance;
            int buttonTop = isControlsVertical ? padding.Top + distance : padding.Top;
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
    #region DataControlPanel : Panel určený pro zobrazení sady dat
    /// <summary>
    /// <see cref="DataControlPanel"/> : Panel určený pro zobrazení sady dat
    /// </summary>
    public class DataControlPanel : DPanel, IDataControl
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataControlPanel()
        {
            this.InitializePanel();
        }
        /// <summary>
        /// Inicializuje panel a jeho obsah
        /// </summary>
        protected virtual void InitializePanel()
        {
            this.__ContentPadding = ControlSupport.StandardContentPadding;
            this.__Cells = new Dictionary<string, CellInfo>();
            this.BorderStyle = BorderStyle.None;
            this.Buttons = new DialogButtonType[] { DialogButtonType.Ok };
            this.AcceptButtonType = DialogButtonType.Ok;
            this.CancelButtonType = DialogButtonType.Cancel;
        }
        /// <summary>
        /// Mezery mezi buttony
        /// </summary>
        public Padding ContentPadding { get { return __ContentPadding; } set { __ContentPadding = value; LayoutContent(true); } } private Padding __ContentPadding;
        /// <summary>
        /// Optimální velikost
        /// </summary>
        public virtual Size? OptimalSize { get; set; }
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        public virtual DialogButtonType[] Buttons { get; set; }
        public virtual DialogButtonType AcceptButtonType { get; set; }
        public virtual DialogButtonType CancelButtonType { get; set; }

        /// <summary>
        /// Po kliknutí na některý button
        /// </summary>
        /// <param name="args"></param>
        public virtual void DialogButtonClicked(DialogButtonEventArgs args)
        {
            switch (args.DialogButtonType)
            {
                case DialogButtonType.Ok:
                    DataStore();
                    this.CloseForm(DialogResult.OK);
                    break;
                case DialogButtonType.Apply:
                    DataStore();
                    break;
                case DialogButtonType.Cancel:
                    this.CloseForm(DialogResult.Cancel);
                    break;
            }
        }
        protected void CloseForm(DialogResult dialogResult)
        {
            var form = this.FindForm();
            if (form != null)
            {
                form.DialogResult = dialogResult;
                form.Close();
            }
        }
        #endregion
        #region Vkládání prvků AddCell(), buňky Cells, třída CellInfo

        public void AddCell(ControlType controlType, string label, string propertyName, int left, int top, int width)
        {
            CellInfo cell = new CellInfo(propertyName, controlType);

            if (!String.IsNullOrEmpty(label) && (!(controlType == ControlType.Label || controlType == ControlType.CheckBox)))
            {
                cell.LabelControl = ControlSupport.CreateControl(ControlType.Label, label, this);
                cell.LabelBounds = new Rectangle(left + 2, top - 15, width - 8, 15);
                LayoutContentOne(cell.LabelControl, cell.LabelBounds.Value);
            }

            if (controlType != ControlType.None)
            {
                string text = (controlType == ControlType.Label || controlType == ControlType.CheckBox) ? label : "";
                cell.InputControl = ControlSupport.CreateControl(controlType, text, this);
                cell.InputBounds = new Rectangle(left, top, width, cell.InputControl?.Height ?? 20);
                LayoutContentOne(cell.InputControl, cell.InputBounds.Value);
            }

            __Cells.Add(propertyName, cell);
        }
        /// <summary>
        /// Buňky
        /// </summary>
        private Dictionary<string, CellInfo> __Cells;
        /// <summary>
        /// Jedna buňka s daty
        /// </summary>
        private class CellInfo
        {
            public CellInfo(string propertyName, ControlType inputType)
            {
                PropertyName = propertyName;
                InputType = inputType;
            }
            public string PropertyName { get; private set; }
            public ControlType InputType { get; private set; }
            public Rectangle? LabelBounds { get; set; }
            public Rectangle? InputBounds { get; set; }
            public Control LabelControl { get; set; }
            public Control InputControl { get; set; }
        }
        #endregion
        #region Fyzické umístění controlů v panelu podle buněk a padding
        protected virtual void LayoutContent(bool force)
        {

        }
        protected virtual void LayoutContentOne(Control control, Rectangle bounds)
        {
            var padding = this.ContentPadding;
            if (control != null)
                control.Bounds = new Rectangle(bounds.X + padding.Left, bounds.Y + padding.Top, bounds.Width, bounds.Height);
        }
        #endregion
        #region Datový objekt, načtení a uložení dat

        public object DataObject { get { return __DataObject; } set { __DataObject = value; this.DataShow(); } }
        private object __DataObject;
        /// <summary>
        /// Převezme data z properties z objektu <see cref="DataObject"/> a vloží je do vizuálních controlů.
        /// </summary>
        protected virtual void DataShow()
        {
            var dataObject = __DataObject;
            var properties = (dataObject != null ? dataObject.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) : null);
            foreach (var cell in __Cells.Values)
            {
                if (cell.InputControl is null) continue;

                object value = null;
                if (properties != null)
                {
                    var property = properties.FirstOrDefault(p => p.Name == cell.PropertyName);
                    if (property != null && property.GetGetMethod() != null)
                        value = property.GetValue(dataObject);
                }

                cell.InputControl.Text = value?.ToString();
            }
        }
        /// <summary>
        /// Získá data z vizuálních controlů a uloží je do properties do objektu <see cref="DataObject"/>
        /// </summary>
        protected virtual void DataStore()
        { }
        #endregion
    }
    #endregion
    #region Bázové controly - DLabel, DTextBox, DFileBox, DButton, DCheckBox, DPanel
    #region DLabel
    /// <summary>
    /// Label
    /// </summary>
    public class DLabel : Label, IControlExtended
    {
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
    }
    #endregion
    #region DTextBox
    /// <summary>
    /// TextBox
    /// </summary>
    public class DTextBox : TextBox, IControlExtended
    {
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

            parent.Controls.Add(textBox);
            return textBox;
        }
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
    /// TextBox
    /// </summary>
    public class DFileBox : DTextButtonBox, IControlExtended
    {
        #region Soubor, kliknutí na button
        /// <summary>
        /// Obsahuje jméno souboru
        /// </summary>
        public string FileName { get { return TextValue; } set { TextValue = value; } }
        protected override void OnButtonInitialize()
        {
            this.Button.Image = Properties.Resources.folder_16;
        }
        protected override void OnButtonClick()
        {
            string fileName = ApplicationData.GetCurrentFilePath(this.FileName);
            string path = (!String.IsNullOrEmpty(fileName) ? System.IO.Path.GetDirectoryName(fileName) : "");
            string name = (!String.IsNullOrEmpty(fileName) ? System.IO.Path.GetFileName(fileName) : "");
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
                    this.FileName = dialog.FileName;
            }
        }
        #endregion
    }
    #endregion
    #region DTextButtonBox
    /// <summary>
    /// TextBox + Button (pro FileBox, DateBox atd)
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
            __TextBox.ActivityChange += _SubActivityChange;

            __Button = DButton.Create(this, "", Properties.Resources.zoom_3_16, new Size(20, 20), null, _ButtonClick);
            __Button.FlatStyle = FlatStyle.Flat;
            __Button.FlatAppearance.BorderSize = 0;
            __Button.Padding = new Padding(0);
            __Button.Margin = new Padding(0);
            __Button.ActivityChange += _SubActivityChange;

            OnButtonInitialize();

            this.ActivityChange += _SubActivityChange;
            DoLayout(false);
        }
        private void _SubActivityChange(object sender, EventArgs e)
        {
            _DoLayoutOnActivityChange();
        }
        private void _ButtonClick(object sender, EventArgs e)
        {
            OnButtonClick();
            __TextBox.Focus();
        }
        /// <summary>
        /// Prvek je aktivní osobně nebo jeho Text nebo Button
        /// </summary>
        protected bool IsAnyActive { get { return (this.IsActive || __TextBox.IsActive || __Button.IsActive); } }
        protected override void OnActivityChange()
        {
            _DoLayoutOnActivityChange();
        }
        /// <summary>
        /// Zajistí provedení layoutu se zpožděním, po nějaké době
        /// </summary>
        private void _DoLayoutOnActivityChange()
        {
            __LazyLayoutGuid = WatchTimer.CallMeAfter(_DoLayoutOnActivityChangeLazy, 30, true, __LazyLayoutGuid);
        }
        private void _DoLayoutOnActivityChangeLazy()
        {
            bool isActive = IsAnyActive;
            DoLayout(isActive);
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
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
                __Button.Visible = isActive.Value;

            __LayoutPending = false;
        }
        /// <summary>
        /// Příznak, že právě probíhá <see cref="DoLayout"/>, a nebude se tedy provádět rekurzivně. 
        /// Což by mohlo být: Změna rozměru vnější = OnClientSizeChanged = DoLayout = Korekce výšky panelu (this.Height = panelTargetHeight) = OnClientSizeChanged = DoLayout !
        /// </summary>
        private bool __LayoutPending;
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
        protected string TextValue { get { return __TextValue; } set { __TextValue = value; __TextBox.Text = value; } }
        private string __TextValue;

        protected virtual void OnButtonInitialize()
        {
            this.Button.Image = Properties.Resources.zoom_3_16;
        }
        /// <summary>
        /// Po kliknutí na tlačítko
        /// </summary>
        protected virtual void OnButtonClick() { }
        #endregion
    }
    #endregion
    #region DButton
    /// <summary>
    /// Button
    /// </summary>
    public class DButton : Button, IControlExtended
    {
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
                case DialogButtonType.Cancel: return DButton.Create(parent, "Cancel", Properties.Resources.dialog_cancel_3_22, size, buttonType, click);
                case DialogButtonType.Abort: return DButton.Create(parent, "Abort", Properties.Resources.process_stop_6_22, size, buttonType, click);
                case DialogButtonType.Retry: return DButton.Create(parent, "Retry", Properties.Resources.view_refresh_4_22, size, buttonType, click);
                case DialogButtonType.Ignore: return DButton.Create(parent, "Ignore", Properties.Resources.go_next_4_22, size, buttonType, click);
                case DialogButtonType.Yes: return DButton.Create(parent, "Yes", Properties.Resources.dialog_ok_4_22, size, buttonType, click);
                case DialogButtonType.No: return DButton.Create(parent, "No", Properties.Resources.dialog_no_3_22, size, buttonType, click);
                case DialogButtonType.Help: return DButton.Create(parent, "Help", Properties.Resources.help_3_22, size, buttonType, click);
                case DialogButtonType.Next: return DButton.Create(parent, "Next", Properties.Resources.go_next_3_22, size, buttonType, click);
                case DialogButtonType.Prev: return DButton.Create(parent, "Prev", Properties.Resources.go_previous_3_22, size, buttonType, click);
                case DialogButtonType.Apply: return DButton.Create(parent, "Apply", Properties.Resources.dialog_ok_apply_22, size, buttonType, click);
                case DialogButtonType.Save: return DButton.Create(parent, "Save", Properties.Resources.media_floppy_3_5_mount_2_22, size, buttonType, click);
                case DialogButtonType.Ok:
                default:
                    return DButton.Create(parent, "OK", Properties.Resources.dialog_clean_22, size, buttonType, click);
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

            parent.Controls.Add(button);
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
    #region DCheckBox
    /// <summary>
    /// CheckBox
    /// </summary>
    public class DCheckBox : CheckBox, IControlExtended
    {
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
    public interface IDataControl
    {
        /// <summary>
        /// Optimální velikost
        /// </summary>
        Size? OptimalSize { get; }
        /// <summary>
        /// Přítomné buttony v odpovídajícím pořadí
        /// </summary>
        DialogButtonType[] Buttons { get; }
        DialogButtonType AcceptButtonType { get; }
        DialogButtonType CancelButtonType { get; }
        /// <summary>
        /// Po kliknutí na některý button
        /// </summary>
        /// <param name="args"></param>
        void DialogButtonClicked(DialogButtonEventArgs args);
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
        public static DButton CreateDButton(Control parent, string text, Image image, Size size, EventHandler click = null) { return DButton.Create(parent, text, image, size, click); }

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
                case ControlType.MemoBox:
                case ControlType.DateBox:
                case ControlType.IntegerBox:
                case ControlType.DecimalBox:
                    var textBox = new DTextBox() { Text = text };
                    control = textBox;
                    break;
                case ControlType.FileBox:
                    var fileBox = new DFileBox() { Text = text };
                    control = fileBox;
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
        Image
    }
    #endregion
}
