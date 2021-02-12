using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using DXB = DevExpress.XtraBars;
using DXE = DevExpress.XtraEditors;
using DXT = DevExpress.XtraTab;
using DXG = DevExpress.XtraGrid;
using DC = DevExpress.XtraCharts;
using DevExpress.XtraBars.Ribbon;


namespace Djs.Tools.CovidGraphs
{
    #region class DxComponent : Factory pro tvorbu DevExpress komponent
    /// <summary>
    /// <see cref="DxComponent"/> : Factory pro tvorbu DevExpress komponent
    /// </summary>
    public class DxComponent
    {
        #region Singleton
        /// <summary>
        /// Soukromý přístup k singletonu
        /// </summary>
        protected static DxComponent Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_InstanceLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new DxComponent();
                        }
                    }
                }
                return _Instance;
            }
        }
        private DxComponent()
        {
            this._InitStyles();
        }
        private static DxComponent _Instance;
        private static object _InstanceLock = new object();
        #endregion
        #region Styly
        private void _InitStyles()
        {
            var titleStyle = new DXE.StyleController();
            titleStyle.Appearance.FontSizeDelta = 2;
            titleStyle.Appearance.FontStyleDelta = FontStyle.Regular;
            titleStyle.Appearance.Options.UseBorderColor = false;
            titleStyle.Appearance.Options.UseBackColor = false;
            titleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var labelStyle = new DXE.StyleController();
            labelStyle.Appearance.FontSizeDelta = 1;
            labelStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            labelStyle.Appearance.Options.UseBorderColor = false;
            labelStyle.Appearance.Options.UseBackColor = false;
            labelStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var infoStyle = new DXE.StyleController();
            labelStyle.Appearance.FontSizeDelta = 0;
            labelStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            labelStyle.Appearance.Options.UseBorderColor = false;
            labelStyle.Appearance.Options.UseBackColor = false;
            labelStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var inputStyle = new DXE.StyleController();
            inputStyle.Appearance.FontSizeDelta = 1;
            inputStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            inputStyle.Appearance.Options.UseBorderColor = false;
            inputStyle.Appearance.Options.UseBackColor = false;
            inputStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            _TitleStyle = titleStyle;
            _LabelStyle = labelStyle;
            _InfoStyle = infoStyle;
            _InputStyle = inputStyle;

            _DetailXLabel = 16;
            _DetailXText = 12;
            _DetailYFirst = 9;
            _DetailYHeightLabel = 19;
            _DetailYHeightText = 22;
            _DetailYSpaceLabel = 2;
            _DetailYSpaceText = 3;
            _DetailXMargin = 6;
            _DetailYMargin = 4;

            _DefaultButtonPanelHeight = 40;
            _DefaultButtonWidth = 150;
            _DefaultButtonHeight = 32;
        }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Title"/>
        /// </summary>
        public static DXE.StyleController TitleStyle { get { return Instance._TitleStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Default"/>
        /// </summary>
        public static DXE.StyleController LabelStyle { get { return Instance._LabelStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Info"/>
        /// </summary>
        public static DXE.StyleController InfoStyle { get { return Instance._InfoStyle; } }
        /// <summary>
        /// Styl pro Input prvky
        /// </summary>
        public static DXE.StyleController InputStyle { get { return Instance._InputStyle; } }

        public static int DetailXLabel { get { return Instance._DetailXLabel; } }
        public static int DetailXText { get { return Instance._DetailXText; } }
        public static int DetailYFirst { get { return Instance._DetailYFirst; } }
        public static int DetailYHeightLabel { get { return Instance._DetailYHeightLabel; } }
        public static int DetailYHeightText { get { return Instance._DetailYHeightText; } }
        public static int DetailYSpaceLabel { get { return Instance._DetailYSpaceLabel; } }
        public static int DetailYSpaceText { get { return Instance._DetailYSpaceText; } }
        public static int DetailXMargin { get { return Instance._DetailXMargin; } }
        public static int DetailYMargin { get { return Instance._DetailYMargin; } }
        public static int DefaultButtonPanelHeight { get { return Instance._DefaultButtonPanelHeight; } }
        public static int DefaultButtonWidth { get { return Instance._DefaultButtonWidth; } }
        public static int DefaultButtonHeight { get { return Instance._DefaultButtonHeight; } }

        private DXE.StyleController _TitleStyle;
        private DXE.StyleController _LabelStyle;
        private DXE.StyleController _InfoStyle;
        private DXE.StyleController _InputStyle;
        private int _DetailXLabel;
        private int _DetailXText;
        private int _DetailYFirst;
        private int _DetailYHeightLabel;
        private int _DetailYHeightText;
        private int _DetailYSpaceLabel;
        private int _DetailYSpaceText;
        private int _DetailXMargin;
        private int _DetailYMargin;
        private int _DefaultButtonPanelHeight;
        private int _DefaultButtonWidth;
        private int _DefaultButtonHeight;
        #endregion
        #region Factory metody
        public static DxPanelControl CreateDxPanel(Control parent = null, 
            DockStyle? dock = null, DXE.Controls.BorderStyles? borderStyles = null,
            int? width = null, int? height = null,
            bool? visible = null)
        {
            var inst = Instance;

            var panel = new DxPanelControl();
            if (parent != null) parent.Controls.Add(panel);
            if (dock.HasValue) panel.Dock = dock.Value;
            if (width.HasValue) panel.Width = width.Value;
            if (height.HasValue) panel.Height = height.Value;
            if (borderStyles.HasValue) panel.BorderStyle = borderStyles.Value;
            if (visible.HasValue) panel.Visible = visible.Value;
            return panel;
        }
        public static DxSplitContainerControl CreateDxSplitContainer(Control parent = null, EventHandler splitterPositionChanged = null, DockStyle? dock = null, 
            Orientation splitLineOrientation = Orientation.Horizontal, DXE.SplitFixedPanel? fixedPanel = null,
            int? splitPosition = null, DXE.SplitPanelVisibility? panelVisibility = null,
            bool? showSplitGlyph = null, DXE.Controls.BorderStyles? borderStyles = null)
        {
            var inst = Instance;

            var container = new DxSplitContainerControl() { Horizontal = (splitLineOrientation == Orientation.Vertical) };
            if (parent != null) parent.Controls.Add(container);
            if (dock.HasValue) container.Dock = dock.Value;
            container.FixedPanel = (fixedPanel ?? DXE.SplitFixedPanel.None);
            container.SplitterPosition = (splitPosition ?? 200);
            container.PanelVisibility = (panelVisibility ?? DXE.SplitPanelVisibility.Both);
            if (borderStyles.HasValue) container.BorderStyle = borderStyles.Value;
            container.ShowSplitGlyph = (showSplitGlyph.HasValue ? (showSplitGlyph.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False) : DevExpress.Utils.DefaultBoolean.Default);

            return container;
        }
        public static DxLabelControl CreateDxLabel(int x, ref int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DXE.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool shiftY = false)
        {
            var inst = Instance;

            var label = new DxLabelControl() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightLabel), Text = text };
            label.StyleController = (styleType == LabelStyleType.Title ? inst._TitleStyle : (styleType == LabelStyleType.Info ? inst._InfoStyle : inst._LabelStyle));
            if (wordWrap.HasValue) label.Appearance.TextOptions.WordWrap = wordWrap.Value;
            if (hAlignment.HasValue) label.Appearance.TextOptions.HAlignment = hAlignment.Value;
            if (wordWrap.HasValue || hAlignment.HasValue) label.Appearance.Options.UseTextOptions = true;
            if (hAlignment.HasValue) label.Appearance.TextOptions.HAlignment = hAlignment.Value;
            if (autoSizeMode.HasValue) label.AutoSizeMode = autoSizeMode.Value;

            if (visible.HasValue) label.Visible = visible.Value;

            if (parent != null) parent.Controls.Add(label);
            if (shiftY) y = y + label.Height + inst._DetailYSpaceLabel;

            return label;
        }
        public static DxTextEdit CreateDxTextEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
            DXE.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var textEdit = new DxTextEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            textEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) textEdit.Visible = visible.Value;
            if (readOnly.HasValue) textEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) textEdit.TabStop = tabStop.Value;

            if (maskType.HasValue) textEdit.Properties.Mask.MaskType = maskType.Value;
            if (editMask != null) textEdit.Properties.Mask.EditMask = editMask;
            if (useMaskAsDisplayFormat.HasValue) textEdit.Properties.Mask.UseMaskAsDisplayFormat = useMaskAsDisplayFormat.Value;

            textEdit.SetToolTip(toolTipTitle, toolTipText);

            if (textChanged != null) textEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(textEdit);
            if (shiftY) y = y + textEdit.Height + inst._DetailYSpaceText;
            return textEdit;
        }
        public static DxMemoEdit CreateDxMemoEdit(int x, ref int y, int w, int h, Control parent, EventHandler textChanged = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var memoEdit = new DxMemoEdit() { Bounds = new Rectangle(x, y, w, h) };
            memoEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) memoEdit.Visible = visible.Value;
            if (readOnly.HasValue) memoEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) memoEdit.TabStop = tabStop.Value;

            if (textChanged != null) memoEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(memoEdit);
            if (shiftY) y = y + memoEdit.Height + inst._DetailYSpaceText;

            return memoEdit;
        }
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, ref int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var comboBox = new DxImageComboBoxEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            comboBox.StyleController = inst._InputStyle;
            if (visible.HasValue) comboBox.Visible = visible.Value;
            if (readOnly.HasValue) comboBox.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) comboBox.TabStop = tabStop.Value;

            if (itemsTabbed != null)
            {
                string[] items = itemsTabbed.Split('\t');
                for (int i = 0; i < items.Length; i++)
                    comboBox.Properties.Items.Add(items[i], i, 0);
            }
            if (selectedIndexChanged != null) comboBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(comboBox);
            if (shiftY) y = y + comboBox.Height + inst._DetailYSpaceText;
            return comboBox;
        }
        public static DxSpinEdit CreateDxSpinEdit(int x, ref int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DXE.Controls.SpinStyles? spinStyles = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var spinEdit = new DxSpinEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText) };
            spinEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) spinEdit.Visible = visible.Value;
            if (readOnly.HasValue) spinEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) spinEdit.TabStop = tabStop.Value;

            if (minValue.HasValue) spinEdit.Properties.MinValue = minValue.Value;
            if (maxValue.HasValue) spinEdit.Properties.MaxValue = maxValue.Value;
            if (increment.HasValue) spinEdit.Properties.Increment = increment.Value;
            if (mask != null)
            {
                spinEdit.Properties.EditMask = mask;
                spinEdit.Properties.DisplayFormat.FormatString = mask;
                spinEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                spinEdit.Properties.EditFormat.FormatString = mask;
            }
            if (spinStyles.HasValue) spinEdit.Properties.SpinStyle = spinStyles.Value;

            if (valueChanged != null) spinEdit.ValueChanged += valueChanged;
            if (parent != null) parent.Controls.Add(spinEdit);
            if (shiftY) y = y + spinEdit.Height + inst._DetailYSpaceText;
            return spinEdit;
        }
        public static DxCheckEdit CreateDxCheckEdit(int x, ref int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DXE.Controls.CheckBoxStyle? checkBoxStyle = null, DXE.Controls.BorderStyles? borderStyles = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var checkEdit = new DxCheckEdit() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightText), Text = text };
            checkEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) checkEdit.Visible = visible.Value;
            if (readOnly.HasValue) checkEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) checkEdit.TabStop = tabStop.Value;

            if (checkBoxStyle.HasValue) checkEdit.Properties.CheckBoxOptions.Style = checkBoxStyle.Value;
            if (borderStyles.HasValue) checkEdit.BorderStyle = borderStyles.Value;

            if (checkedChanged != null) checkEdit.CheckedChanged += checkedChanged;
            if (parent != null) parent.Controls.Add(checkEdit);
            if (shiftY) y = y + checkEdit.Height + inst._DetailYSpaceText;

            return checkEdit;
        }
        public static DxListBoxControl CreateDxListBox(DockStyle? dock = null, int? width = null, int? height = null, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            bool? visible = null, bool? tabStop = null)
        {
            int y = 0;
            int w = width ?? 0;
            int h = height ?? 0;
            return CreateDxListBox(0, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeightPadding, reorderByDragEnabled,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, ref int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            DxListBoxControl listBox = new DxListBoxControl() { Bounds = new Rectangle(x, y, w, h) };
            listBox.StyleController = inst._InputStyle;
            if (dock.HasValue) listBox.Dock = dock.Value;
            if (multiColumn.HasValue) listBox.MultiColumn = multiColumn.Value;
            if (selectionMode.HasValue) listBox.SelectionMode = selectionMode.Value;
            if (itemHeightPadding.HasValue) listBox.ItemHeightPadding = itemHeightPadding.Value;
            if (reorderByDragEnabled.HasValue) listBox.ReorderByDragEnabled= reorderByDragEnabled.Value;
            if (visible.HasValue) listBox.Visible = visible.Value;
            if (tabStop.HasValue) listBox.TabStop = tabStop.Value;

            if (selectedIndexChanged != null) listBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(listBox);
            if (shiftY) y = y + listBox.Height + inst._DetailYSpaceText;

            return listBox;
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var simpleButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            simpleButton.StyleController = inst._InputStyle;
            simpleButton.Text = text;
            if (visible.HasValue) simpleButton.Visible = visible.Value;
            if (tabStop.HasValue) simpleButton.TabStop = tabStop.Value;

            if (click != null) simpleButton.Click += click;
            if (parent != null) parent.Controls.Add(simpleButton);
            if (shiftY) y = y + simpleButton.Height + inst._DetailYSpaceText;

            return simpleButton;
        }

        #endregion
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Styl použitý pro Label
    /// </summary>
    public enum LabelStyleType
    {
        /// <summary>
        /// Běžný label u jednotlivých input prvků
        /// </summary>
        Default,
        /// <summary>
        /// Titulkový label
        /// </summary>
        Title,
        /// <summary>
        /// Dodatková informace
        /// </summary>
        Info
    }
    #endregion
    #region DxSplitContainerControl
    /// <summary>
    /// SplitContainerControl
    /// </summary>
    public class DxSplitContainerControl : DXE.SplitContainerControl
    { }
    #endregion
    #region DxPanelControl
    /// <summary>
    /// PanelControl
    /// </summary>
    public class DxPanelControl : DXE.PanelControl
    { }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// LabelControl
    /// </summary>
    public class DxLabelControl : DXE.LabelControl
    {
        public DxLabelControl()
        {
            BorderStyle = DXE.Controls.BorderStyles.NoBorder;
        }
    }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// TextEdit
    /// </summary>
    public class DxTextEdit : DXE.TextEdit
    {
        public DxTextEdit()
        {
            EnterMoveNextControl = true;
        }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string text)
        {
            this.SetToolTip(null, text);
        }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text)) return;

            this.SuperTip = new DevExpress.Utils.SuperToolTip();
            if (title != null) this.SuperTip.Items.AddTitle(title);
            this.SuperTip.Items.Add(text);
        }
    }
    #endregion
    #region DxMemoEdit
    /// <summary>
    /// MemoEdit
    /// </summary>
    public class DxMemoEdit : DXE.MemoEdit
    { }
    #endregion
    #region DxImageComboBoxEdit
    /// <summary>
    /// ImageComboBoxEdit
    /// </summary>
    public class DxImageComboBoxEdit : DXE.ImageComboBoxEdit
    { }
    #endregion
    #region DxSpinEdit
    /// <summary>
    /// SpinEdit
    /// </summary>
    public class DxSpinEdit : DXE.SpinEdit
    { }
    #endregion
    #region DxCheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    public class DxCheckEdit : DXE.CheckEdit
    { }
    #endregion
    #region DxListBoxControl
    /// <summary>
    /// ListBoxControl
    /// </summary>
    public class DxListBoxControl : DXE.ListBoxControl
    {
        public DxListBoxControl()
        {
            ReorderByDragEnabled = false;
            ReorderIconColor = Color.FromArgb(192, 116, 116, 96);
            ReorderIconColorHot = Color.FromArgb(220, 160, 160, 122);
        }
        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DXE.BaseListBoxControl.ItemAutoHeight"/> = true.
        /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
        /// Hodnota vyšší než 10 se akceptuje jako 10.
        /// </summary>
        public int ItemHeightPadding
        {
            get { return _ItemHeightPadding; }
            set
            {
                if (value > 0)
                {
                    int padding = (value > 10 ? 10 : value);
                    int fontheight = this.Appearance.GetFont().Height;
                    this.ItemAutoHeight = false;
                    this.ItemHeight = fontheight + (2 * padding);
                    _ItemHeightPadding = padding;
                }
                else
                {
                    this.ItemAutoHeight = true;
                    _ItemHeightPadding = 0;
                }
            }
        }
        private int _ItemHeightPadding = 0;

        #region Overrides
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintOnMouseItem(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.DetectOnMouseItemAbsolute(Control.MousePosition);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.DetectOnMouseItem(e.Location);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.DetectOnMouseItem(null);
        }
        #endregion
        #region Přesouvání prvků pomocí myši
        /// <summary>
        /// Povolení pro reorder prvků pomocí myši (pak se zobrazí ikona pro drag)
        /// </summary>
        public bool ReorderByDragEnabled { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColor { get; set; }
        /// <summary>
        /// Barva ikonky pro reorder <see cref="ReorderByDragEnabled"/>, v běžném stavu.
        /// Průhlednost je podporována.
        /// </summary>
        public Color ReorderIconColorHot { get; set; }
        /// <summary>
        /// Určí a uloží si index myšoaktivního prvku na dané absolutní souřadnici
        /// </summary>
        /// <param name="mouseAbsolutePosition"></param>
        protected void DetectOnMouseItemAbsolute(Point mouseAbsolutePosition)
        {
            Point relativePoint = this.PointToClient(mouseAbsolutePosition);
            DetectOnMouseItem(relativePoint);
        }
        /// <summary>
        /// Určí a uloží si index myšoaktivního prvku na dané relativní souřadnici
        /// </summary>
        /// <param name="mouseRelativePosition"></param>
        protected void DetectOnMouseItem(Point? mouseRelativePosition)
        {
            int index = -1;
            float ratio = 0f;
            if (mouseRelativePosition.HasValue)
            {
                index = this.IndexFromPoint(mouseRelativePosition.Value);
                float ratioX = (float)mouseRelativePosition.Value.X / (float)this.Width; // Relativní pozice myši na ose X v rámci celého controlu v rozmezí 0.0 až 1.0
                ratio = 0.30f + (ratioX < 0.5f ? 0f : 1.40f * (ratioX - 0.5f));          // 0.30 je pevná dolní hodnota. Ta platí pro pozici myši 0.0 až 0.5. Pak se ratio zvyšuje od 0.30 do 1.0.
            }
            OnMouseItemIndex = index;
            OnMouseItemAlphaRatio = ratio;
            this.DetectOnMouseCursor(mouseRelativePosition);
        }
        /// <summary>
        /// Určí a uloží si druh kurzoru podle dané relativní souřadnice
        /// </summary>
        /// <param name="mouseRelativePosition"></param>
        protected void DetectOnMouseCursor(Point? mouseRelativePosition)
        {
            bool isCursorActive = false;
            if (mouseRelativePosition.HasValue)
            {
                var iconBounds = OnMouseIconBounds;
                isCursorActive = (iconBounds.HasValue && iconBounds.Value.Contains(mouseRelativePosition.Value));
            }

            if (isCursorActive != _IsMouseOverReorderIcon)
            {
                _IsMouseOverReorderIcon = isCursorActive;
                if (ReorderByDragEnabled)
                {
                    this.Cursor = (isCursorActive ? Cursors.SizeNS : Cursors.Default);
                    this.Invalidate();                         // Překreslení => jiná barva ikony
                }
            }
        }
        /// <summary>
        /// Zajistí vykreslení ikony pro ReorderByDrag a případně i přemisťovaného prvku
        /// </summary>
        /// <param name="e"></param>
        private void PaintOnMouseItem(PaintEventArgs e)
        {
            if (!ReorderByDragEnabled) return;

            Rectangle? iconBounds = OnMouseIconBounds;
            if (!iconBounds.HasValue) return;

            int wb = iconBounds.Value.Width;
            int x0 = iconBounds.Value.X;
            int yc = iconBounds.Value.Y + iconBounds.Value.Height / 2;
            Color iconColor = (_IsMouseOverReorderIcon ? ReorderIconColorHot : ReorderIconColor);
            float alphaRatio = OnMouseItemAlphaRatio;
            int alpha = (int)((float)iconColor.A * alphaRatio);
            iconColor = Color.FromArgb(alpha, iconColor);
            using (SolidBrush brush = new SolidBrush(iconColor))
            {
                e.Graphics.FillRectangle(brush, x0, yc - 6, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc - 1, wb, 2);
                e.Graphics.FillRectangle(brush, x0, yc + 4, wb, 2);
            }
        }
        /// <summary>
        /// Příznak, že myš je nad ikonou pro přesouvání prvku, a kurzor je tedy upraven na šipku nahoru/dolů.
        /// Pak tedy MouseDown a MouseMove bude provádět Reorder prvku.
        /// </summary>
        private bool _IsMouseOverReorderIcon = false;
        /// <summary>
        /// Index prvku, nad kterým se pohybuje myš
        /// </summary>
        public int OnMouseItemIndex
        {
            get { return _OnMouseItemIndex; }
            protected set
            {
                if (value != _OnMouseItemIndex)
                {
                    _OnMouseItemIndex = value;
                    this.Invalidate();
                }
            }
        }
        private int _OnMouseItemIndex = -1;
        /// <summary>
        /// Blízkost myši k ikoně na ose X v poměru: 0.15 = úplně daleko, 1.00 = přímo na ikoně.
        /// Ovlivní průhlednost barvy (kanál Alpha).
        /// </summary>
        public float OnMouseItemAlphaRatio
        {
            get { return _OnMouseItemAlphaRatio; }
            protected set
            {
                float ratio = (value < 0.15f ? 0.15f : (value > 1.0f ? 1.0f : value));
                float diff = ratio - _OnMouseItemAlphaRatio;
                if (diff <= -0.04f || diff >= 0.04f)
                {
                    _OnMouseItemAlphaRatio = ratio;
                    this.Invalidate();
                }
            }
        }
        private float _OnMouseItemAlphaRatio = 0.15f;
        /// <summary>
        /// Souřadnice prostoru pro myší ikonu vpravo v myšoaktivním řádku
        /// </summary>
        protected Rectangle? OnMouseIconBounds
        {
            get
            {
                if (_OnMouseIconBoundsIndex != _OnMouseItemIndex)
                {
                    _OnMouseIconBounds = GetOnMouseIconBounds(_OnMouseItemIndex);
                    _OnMouseIconBoundsIndex = _OnMouseItemIndex;
                }
                return _OnMouseIconBounds;
            }
        }
        /// <summary>
        /// Souřadnice prostoru ikony vpravo pro myš
        /// </summary>
        private Rectangle? _OnMouseIconBounds = null;
        /// <summary>
        /// Index prvku, pro který je vypočtena souřadnice <see cref="_OnMouseIconBounds"/>
        /// </summary>
        private int _OnMouseIconBoundsIndex = -1;
        /// <summary>
        /// Vrátí souřadnici prostoru pro myší ikonu
        /// </summary>
        /// <param name="onMouseItemIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetOnMouseIconBounds(int onMouseItemIndex)
        {
            Rectangle? bounds = null;

            int mouseIndex = OnMouseItemIndex;
            if (mouseIndex < 0) return bounds;                       // Žádný prvek nemá myš

            int visibleIndex = this.GetVisibleIndex(mouseIndex);

            Rectangle listBounds = this.ClientRectangle;
            Rectangle itemBounds = this.GetItemRectangle(mouseIndex);
            if (itemBounds.Right <= listBounds.X || itemBounds.X >= listBounds.Right || itemBounds.Bottom <= listBounds.Y || itemBounds.Y >= listBounds.Bottom) return bounds;   // Prvek s myší není vidět

            if (itemBounds.Width < 35) return bounds;                // Příliš úzký prvek

            int wb = 14;
            int x0 = itemBounds.Right - wb - 6;
            int yc = itemBounds.Y + itemBounds.Height / 2;
            bounds = new Rectangle(x0 - 1, itemBounds.Y, wb + 1, itemBounds.Height);
            return bounds;
        }
        #endregion


    }
    #endregion
    #region DxTreeViewListSimple
    /// <summary>
    /// <see cref="DxTreeViewListSimple"/> : implementace TreeList pro výchozí potřeby Nephrite
    /// </summary>
    public class DxTreeViewListSimple : DevExpress.XtraTreeList.TreeList
    {
        #region Konstruktor a inicializace, privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTreeViewListSimple()
        {
            this._LastId = 0;
            this._NodesId = new Dictionary<int, NodePair>();
            this._NodesKey = new Dictionary<string, NodePair>();
            this.InitTreeView();
        }
        /// <summary>
        /// Incializace komponenty Simple
        /// </summary>
        protected void InitTreeView()
        {
            this.OptionsBehavior.PopulateServiceColumns = false;
            this._MainColumn = new DevExpress.XtraTreeList.Columns.TreeListColumn() { Name = "MainColumn", Visible = true, Width = 150, UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.String, Caption = "Sloupec1", AllowIncrementalSearch = true, FieldName = "Text", ShowButtonMode = DevExpress.XtraTreeList.ShowButtonModeEnum.ShowForFocusedRow, ToolTip = "Tooltip pro sloupec" };
            this.Columns.Add(this._MainColumn);

            this._MainColumn.OptionsColumn.AllowEdit = false;
            this._MainColumn.OptionsColumn.AllowSort = false;

            this.OptionsBehavior.AllowExpandOnDblClick = true;
            this.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsBehavior.Editable = true;
            this.OptionsBehavior.EditingMode = DevExpress.XtraTreeList.TreeListEditingMode.Inplace;
            this.OptionsBehavior.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;             // Kdy se zahájí editace (kurzor)? MouseUp: docela hezké; MouseDownFocused: po MouseDown ve stavu Focused (až na druhý klik)
            this.OptionsBehavior.ShowToolTips = true;
            this.OptionsBehavior.SmartMouseHover = true;

            this.OptionsBehavior.AllowExpandAnimation = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsBehavior.AutoNodeHeight = true;
            this.OptionsBehavior.AutoSelectAllInEditor = true;
            this.OptionsBehavior.CloseEditorOnLostFocus = true;

            this.OptionsNavigation.AutoMoveRowFocus = true;
            this.OptionsNavigation.EnterMovesNextColumn = false;
            this.OptionsNavigation.MoveOnEdit = false;
            this.OptionsNavigation.UseTabKey = false;

            this.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.OptionsSelection.EnableAppearanceHotTrackedRow = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsSelection.InvertSelection = true;

            this.ViewStyle = DevExpress.XtraTreeList.TreeListViewStyle.TreeView;

            this.NodeCellStyle += _OnNodeCellStyle;
            this.DoubleClick += _OnDoubleClick;
            this.KeyDown += _OnKeyDown;
            this.ShownEditor += _OnShownEditor;
            this.ValidatingEditor += _OnValidatingEditor;
            this.FocusedNodeChanged += _OnFocusedNodeChanged;
            this.BeforeExpand += _OnBeforeExpand;
            this.AfterCollapse += _OnAfterCollapse;

            this.LazyLoadNodeText = "Načítám záznamy...";
            this.LazyLoadNodeImageName = null;
        }
        DevExpress.XtraTreeList.Columns.TreeListColumn _MainColumn;
        private Dictionary<int, NodePair> _NodesId;
        private Dictionary<string, NodePair> _NodesKey;
        private int _LastId;
        private class NodePair
        {
            public NodePair(DxTreeViewListSimple owner, int nodeId, NodeItemInfo nodeInfo, DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, bool isLazyChild)
            {
                this.NodeId = nodeId;
                this.NodeInfo = nodeInfo;
                this.TreeNode = treeNode;
                this.IsLazyChild = isLazyChild;

                this.INodeItem.Id = nodeId;
                this.INodeItem.Owner = owner;
                this.TreeNode.Tag = nodeId;
            }
            public void ReleasePair()
            {
                this.INodeItem.Id = -1;
                this.INodeItem.Owner = null;
                this.TreeNode.Tag = null;

                this.NodeInfo = null;
                this.TreeNode = null;
                this.IsLazyChild = false;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return (this.NodeInfo?.ToString() ?? "<Empty>");
            }
            /// <summary>
            /// Konstantní ID tohoto nodu, nemění se
            /// </summary>
            public int NodeId { get; private set; }
            /// <summary>
            /// Aktuální interní ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
            /// Tato hodnota se mění při odebrání nodu z TreeView. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
            /// </summary>
            public int CurrentTreeNodeId { get { return TreeNode?.Id ?? -1; } }
            public string NodeKey { get { return NodeInfo?.NodeKey ; } }
            public NodeItemInfo NodeInfo { get; private set; }
            public DevExpress.XtraTreeList.Nodes.TreeListNode TreeNode { get; private set; }
            public bool IsLazyChild { get; private set; }
            private ITreeNodeItem INodeItem { get { return NodeInfo as ITreeNodeItem; } }
        }
        #endregion
        #region Interní události a jejich zpracování : Klávesa, DoubleClick, Editor, Specifika vykreslení, Expand, 
        /// <summary>
        /// Nastavení specifického stylu podle konkrétního Node (FontStyle, Colors, atd)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnNodeCellStyle(object sender, DevExpress.XtraTreeList.GetCustomNodeCellStyleEventArgs args)
        {
            NodeItemInfo nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo == null) return;

            if (nodeInfo.FontSizeDelta.HasValue)
                args.Appearance.FontSizeDelta = nodeInfo.FontSizeDelta.Value;
            if (nodeInfo.FontStyleDelta.HasValue)
                args.Appearance.FontStyleDelta = nodeInfo.FontStyleDelta.Value;
            if (nodeInfo.BackColor.HasValue)
            {
                args.Appearance.BackColor = nodeInfo.BackColor.Value;
                args.Appearance.Options.UseBackColor = true;
            }
            if (nodeInfo.ForeColor.HasValue)
            {
                args.Appearance.ForeColor = nodeInfo.ForeColor.Value;
                args.Appearance.Options.UseForeColor = true;
            }
        }
        /// <summary>
        /// Po fokusu do konkrétního node se nastaví jeho Editable a volá se public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnFocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs args)
        {
            NodeItemInfo nodeInfo = _GetNodeInfo(args.Node);

            _MainColumn.OptionsColumn.AllowEdit = (nodeInfo != null && nodeInfo.Editable);

            if (nodeInfo != null)
                this.OnNodeSelected(nodeInfo);
        }
        /// <summary>
        /// Po stisku klávesy Vpravo a Vlevo se pracuje s Expanded nodů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            DevExpress.XtraTreeList.Nodes.TreeListNode node;
            switch (e.KeyCode)
            {
                case Keys.Right:
                    node = this.FocusedNode;
                    if (node != null && node.HasChildren && !node.Expanded)
                    {
                        node.Expanded = true;
                        e.Handled = true;
                    }
                    break;
                case Keys.Left:
                    node = this.FocusedNode;
                    if (node != null)
                    {
                        if (node.HasChildren && node.Expanded)
                        {
                            node.Expanded = false;
                            e.Handled = true;
                        }
                        else if (node.ParentNode != null)
                        {
                            this.FocusedNode = node.ParentNode;
                            e.Handled = true;
                        }
                    }
                    break;
                case Keys.Delete:
                    if (this.EditorHelper.ActiveEditor == null)
                    {
                    }
                    break;
            }
        }
        /// <summary>
        /// Doubleclick převolá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnDoubleClick(object sender, EventArgs e)
        {
            NodeItemInfo nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnNodeDoubleClick(nodeInfo);
        }
        /// <summary>
        /// V okamžiku zahájení editace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnShownEditor(object sender, EventArgs e)
        {
            if (this.EditorHelper.ActiveEditor != null)
            {
                this.EditorHelper.ActiveEditor.DoubleClick -= _OnEditorDoubleClick;
                this.EditorHelper.ActiveEditor.DoubleClick += _OnEditorDoubleClick;
            }

            NodeItemInfo nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnActivatedEditor(nodeInfo);
        }
        /// <summary>
        /// Ukončení editoru volá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            NodeItemInfo nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnNodeEdited(nodeInfo, this.EditingValue);
        }
        /// <summary>
        /// Doubleclick v editoru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnEditorDoubleClick(object sender, EventArgs e)
        {
            NodeItemInfo nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnEditorDoubleClick(nodeInfo, this.EditingValue);
        }
        /// <summary>
        /// Před rozbalením nodu se volá public event <see cref="NodeExpanded"/>,
        /// a pokud node má nastaveno <see cref="NodeItemInfo.LazyLoadChilds"/> = true, pak se ovlá ještě <see cref="LazyLoadChilds"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnBeforeExpand(object sender, DevExpress.XtraTreeList.BeforeExpandEventArgs args)
        {
            NodeItemInfo nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                this.OnNodeExpanded(nodeInfo);                       // Zatím nevyužívám možnost zakázání Expand, kterou dává args.CanExpand...
                if (nodeInfo.LazyLoadChilds)
                    this.OnLazyLoadChilds(nodeInfo);
            }
        }
        /// <summary>
        /// Po zabalení nodu se volá public event <see cref="NodeCollapsed"/>,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnAfterCollapse(object sender, DevExpress.XtraTreeList.NodeEventArgs args)
        {
            NodeItemInfo nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                this.OnNodeCollapsed(nodeInfo);
            }
        }
        #endregion
        #region Správa nodů (přidání, odebrání, smazání, změny)
        /// <summary>
        /// Přidá jeden node. Není to příliš efektivní. Raději používejme <see cref="AddNodes(IEnumerable{NodeItemInfo})"/>.
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(NodeItemInfo node)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<NodeItemInfo>(AddNode), node); return; }
    
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            NodePair firstPair = null;
            this._AddNode(node, ref firstPair);
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// </summary>
        /// <param name="addNodes"></param>
        public void AddNodes(IEnumerable<NodeItemInfo> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<NodeItemInfo>>(AddNodes), addNodes); return; }

            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            this._RemoveAddNodes(null, addNodes);
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        /// <summary>
        /// Přidá řadu nodů, které jsou donačteny k danému parentu. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Nejprve najde daného parenta, a zruší z něj příznak LazyLoad (protože právě tímto načtením jsou jeho nody vyřešeny). Současně odebere "wait" node (prázdný node, simulující načítání dat).
        /// Pak teprve přidá nové nody.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <param name="addNodes"></param>
        public void AddLazyLoadNodes(string parentKey, IEnumerable<NodeItemInfo> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string, IEnumerable<NodeItemInfo>>(AddLazyLoadNodes), parentKey, addNodes); return; }

            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            bool isAnySelected = this._RemoveLazyLoadFromParent(parentKey);
            var firstPair = this._RemoveAddNodes(null, addNodes);
            if (firstPair != null && (isAnySelected || this.LazyLoadSelectFirstNode)) this.FocusedNode = firstPair.TreeNode;
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

            this.Refresh();
        }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// </summary>
        /// <param name="addNodes"></param>
        public void RemoveNodes(IEnumerable<string> removeNodeKeys)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<string>>(RemoveNodes), removeNodeKeys); return; }
    
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            this._RemoveAddNodes(removeNodeKeys, null);
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// </summary>
        /// <param name="addNodes"></param>
        public void RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<NodeItemInfo> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<string>, IEnumerable<NodeItemInfo>>(RemoveAddNodes), removeNodeKeys, addNodes); return; }

            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            this._RemoveAddNodes(removeNodeKeys, addNodes);
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        /// <summary>
        /// Smaže všechny nodes
        /// </summary>
        public new void ClearNodes()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(ClearNodes)); return; }

            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.BeginUnboundLoad();
            _ClearNodes();
            this.EndUnboundLoad();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }
        /// <summary>
        /// Odebere nody ze stromu a z evidence.
        /// Přidá více node do stromu a do evidence, neřeší blokování GUI.
        /// Metoda vrací první vytvořený <see cref="NodePair"/>.
        /// </summary>
        /// <param name="node"></param>
        private NodePair _RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<NodeItemInfo> addNodes)
        {
            NodePair firstPair = null;

            // Remove:
            if (removeNodeKeys != null)
            {
                foreach (var nodeKey in removeNodeKeys)
                    this._RemoveNode(nodeKey);
            }

            // Add:
            if (addNodes != null)
            {
                foreach (var node in addNodes)
                    this._AddNode(node, ref firstPair);

                // Expand nody: teď už by měly mít svoje Childs přítomné v TreeView:
                foreach (var node in addNodes.Where(n => n.Expanded))
                    this._NodesId[node.NodeId].TreeNode.Expanded = true;
            }

            return firstPair;
        }
        /// <summary>
        /// Vytvoří nový jeden vizuální node podle daných dat, a přidá jej do vizuálního prvku a do interní evidence, neřeší blokování GUI
        /// </summary>
        /// <param name="node"></param>
        private void _AddNode(NodeItemInfo node, ref NodePair firstPair)
        {
            if (node == null) return;

            NodePair nodePair = _AddNodeOne(node, false);  // Daný node (z aplikace) vloží do Tree a vrátí
            if (firstPair == null && nodePair != null)
                firstPair = nodePair;

            if (node.LazyLoadChilds)
                _AddNodeLazyLoad(node);                    // Pokud node má nastaveno LazyLoadChilds, pak pod něj vložím jako jeho Child nový node, reprezentující "načítání z databáze"
        }
        private void _AddNodeLazyLoad(NodeItemInfo parentNode)
        {
            string lazyChildKey = parentNode.NodeKey + "___«LazyLoadChildNode»___";
            string text = this.LazyLoadNodeText ?? "Načítám...";
            string imageName = this.LazyLoadNodeImageName;
            NodeItemInfo lazyNode = new NodeItemInfo(lazyChildKey, parentNode.NodeKey, text, imageName: imageName, fontStyleDelta: FontStyle.Italic);
            NodePair nodePair = _AddNodeOne(lazyNode, true);  // Daný node (z aplikace) vloží do Tree a vrátí
        }
        /// <summary>
        /// Fyzické přidání jednoho node do TreeView a do evidence
        /// </summary>
        /// <param name="nodeInfo"></param>
        private NodePair _AddNodeOne(NodeItemInfo nodeInfo, bool isLazyChild)
        {
            // 1. Vytvoříme TreeListNode:
            int parentId = _GetCurrentTreeNodeId(nodeInfo.ParentNodeKey);
            int imageIndex = _GetImageIndex(nodeInfo.ImageName);
            int selectImageIndex = (!String.IsNullOrEmpty(nodeInfo.ImageNameSelected) ? _GetImageIndex(nodeInfo.ImageNameSelected) : -1);
            if (selectImageIndex < 0) selectImageIndex = imageIndex;
            CheckState checkState = CheckState.Unchecked;
            var treeNode = this.AppendNode(new object[] { nodeInfo.Text }, parentId, imageIndex, selectImageIndex, -1,  checkState);

            // treeNode.Expanded = node.Expanded;                  // Nyní nemá význam, protože Node ještě nemá svoje Childs. Má smysl až po přidání dalších nodů, které jsou jeho Childs. Řeší se v AddNodes().
            // treeNode.StateImageIndex = -1;                      // Zatím nepoužívám druhou ikonu, používám ImageIndex, ta se nechá změnit podle Selected stavu

            // 2. Propojíme vizuální node a datový objekt - pouze přes int ID, nikoli vzájemné reference:
            int nodeId = ++_LastId;
            NodePair nodePair = new NodePair(this, nodeId, nodeInfo, treeNode, isLazyChild);

            // 3. Uložíme Pair do indexů podle ID a podle Key:
            this._NodesId.Add(nodePair.NodeId, nodePair);
            if (nodePair.NodeKey != null) this._NodesKey.Add(nodePair.NodeKey, nodePair);

            return nodePair;
        }
        /// <summary>
        /// Metoda najde a odebere Child prvky daného Parenta, kde tyto Child prvky jsou označeny jako <see cref="NodePair.IsLazyChild"/> = true.
        /// Metoda vrátí true, pokud některý z odebraných prvků byl Selected.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        private bool _RemoveLazyLoadFromParent(string parentKey)
        {
            NodeItemInfo nodeInfo = _GetNodeInfo(parentKey);
            if (nodeInfo == null || !nodeInfo.LazyLoadChilds) return false;

            nodeInfo.LazyLoadChilds = false;

            // Najdu stávající Child nody daného Parenta a všechny je odeberu. Měl by to být pouze jeden node = simulující načítání dat, přidaný v metodě :
            NodePair[] lazyChilds = this._NodesId.Values.Where(p => p.IsLazyChild && p.NodeInfo.ParentNodeKey == parentKey).ToArray();
            bool isAnySelected = (lazyChilds.Length > 0 && lazyChilds.Any(p => p.TreeNode.IsSelected));
            _RemoveAddNodes(lazyChilds.Select(p => p.NodeKey), null);

            return isAnySelected;
        }
        /// <summary>
        /// Smaže všechny nodes, neřeší blokování GUI
        /// </summary>
        private void _ClearNodes()
        {
            base.ClearNodes();

            foreach (NodePair nodePair in this._NodesId.Values)
                nodePair.ReleasePair();

            this._NodesId.Clear();
            this._NodesKey.Clear();
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="nodeId"></param>
        private void _RemoveNode(int nodeId)
        {
            if (nodeId < 0) throw new ArgumentException($"Argument 'nodeId' is negative in {CurrentClassName}.RemoveNode() method.");
            if (!this._NodesId.TryGetValue(nodeId, out var nodePair)) throw new ArgumentException($"Node with ID = '{nodeId}' is not found in {CurrentClassName} nodes."); ;
            _RemoveNode(nodePair);
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="nodeKey"></param>
        private void _RemoveNode(string nodeKey)
        {
            if (nodeKey == null) throw new ArgumentException($"Argument 'nodeKey' is null in {CurrentClassName}.RemoveNode() method.");
            if (!this._NodesKey.TryGetValue(nodeKey, out var nodePair)) throw new ArgumentException($"Node with Key = '{nodeKey}' is not found in {CurrentClassName} nodes."); ;
            _RemoveNode(nodePair);
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="node"></param>
        private void _RemoveNode(NodePair nodePair)
        {
            if (nodePair == null) return;

            // Odebrat z indexů:
            if (this._NodesId.ContainsKey(nodePair.NodeId)) this._NodesId.Remove(nodePair.NodeId);
            if (nodePair.NodeKey != null && this._NodesKey.ContainsKey(nodePair.NodeKey)) this._NodesKey.Remove(nodePair.NodeKey);

            // Reference na vizuální prvek:
            var treeNode = nodePair.TreeNode;

            // Rozpadnout pár:
            nodePair.ReleasePair();

            // Odebrat z vizuálního objektu:
            treeNode.Remove();
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, podle NodeId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private NodeItemInfo _GetNodeInfo(int nodeId)
        {
            if (nodeId >= 0 && this._NodesId.TryGetValue(nodeId, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        private NodeItemInfo _GetNodeInfo(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode)
        {
            int nodeId = ((treeNode != null && treeNode.Tag is int) ? (int)treeNode.Tag : -1);
            if (nodeId >= 0 && this._NodesId.TryGetValue(nodeId, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrací data nodu podle jeho klíče
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        private NodeItemInfo _GetNodeInfo(string nodeKey)
        {
            if (nodeKey != null && this._NodesKey.TryGetValue(nodeKey, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrátí ID nodu pro daný klíč
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        private int _GetNodeId(string nodeKey)
        {
            if (nodeKey != null && this._NodesKey.TryGetValue(nodeKey, out var nodePair)) return nodePair.NodeId;
            return -1;
        }
        /// <summary>
        /// Vrátí aktuální hodnotu interního ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
        /// Tato hodnota se mění při odebrání nodu z TreeView. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        private int _GetCurrentTreeNodeId(string nodeKey)
        {
            if (nodeKey != null && this._NodesKey.TryGetValue(nodeKey, out var nodePair)) return nodePair.CurrentTreeNodeId;
            return -1;
        }
        /// <summary>
        /// Vrací index image pro dané jméno obrázku. Používá funkci <see cref="ImageIndexSearcher"/>
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private int _GetImageIndex(string imageName)
        {
            return (ImageIndexSearcher != null ? ImageIndexSearcher(imageName) : -1);
        }
        /// <summary>
        /// FullName aktuální třídy
        /// </summary>
        protected string CurrentClassName { get { return this.GetType().FullName; } }
        #endregion
        #region Public vlastnosti
        /// <summary>
        /// Funkce, která pro název ikony vrátí její index v ImageListu
        /// </summary>
        public Func<string, int> ImageIndexSearcher { get; set; }
        public string LazyLoadNodeText { get; set; }
        public string LazyLoadNodeImageName { get; set; }
        /// <summary>
        /// Po LazyLoad aktivovat první načtený node?
        /// </summary>
        public bool LazyLoadSelectFirstNode { get; set; }
        /// <summary>
        /// Akce, která zahájí editaci buňky
        /// </summary>
        public DevExpress.XtraTreeList.TreeListEditorShowMode EditorShowMode
        {
            get { return this.OptionsBehavior.EditorShowMode; }
            set { this.OptionsBehavior.EditorShowMode = value; }
        }
        /// <summary>
        /// Aktuálně vybraný Node
        /// </summary>
        public NodeItemInfo FocusedNodeInfo { get { return _GetNodeInfo(this.FocusedNode); } }
        /// <summary>
        /// Najde node podle jeho klíče, pokud nenajde pak vrací false.
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        public bool TryGetNodeInfo(string nodeKey, out NodeItemInfo nodeInfo)
        {
            nodeInfo = null;
            if (nodeKey == null) return false;
            bool result = this._NodesKey.TryGetValue(nodeKey, out var nodePair);
            nodeInfo = nodePair.NodeInfo;
            return result;
        }
        /// <summary>
        /// Pole všech nodů = třída <see cref="NodeItemInfo"/> = data o nodech
        /// </summary>
        public NodeItemInfo[] NodeInfos { get { return this._NodesStandard.ToArray(); } }
        /// <summary>
        /// Najde a vrátí pole nodů, které jsou Child nody daného klíče.
        /// Reálně provádí Scan všech nodů.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        public NodeItemInfo[] GetChildNodeInfos(string parentKey)
        {
            if (parentKey == null) return null;
            return this._NodesStandard.Where(n => n.ParentNodeKey != null && n.ParentNodeKey == parentKey).ToArray();
        }
        /// <summary>
        /// Obsahuje kolekci všech nodů, které nejsou IsLazyChild.
        /// Node typu IsLazyChild je dočasně přidaný child node do těch nodů, jejichž Childs se budou načítat po rozbalení.
        /// </summary>
        private IEnumerable<NodeItemInfo> _NodesStandard { get { return this._NodesId.Values.Where(p => !p.IsLazyChild).Select(p => p.NodeInfo); } }
        #endregion
        #region Public eventy a jejich volání
        /// <summary>
        /// TreeView aktivoval určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeSelected;
        /// <summary>
        /// Vyvolá event <see cref="NodeSelected"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeSelected(NodeItemInfo nodeInfo)
        {
            if (NodeSelected != null) NodeSelected(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeSelected));
        }
        /// <summary>
        /// TreeView má Doubleclick na určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="NodeDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeDoubleClick(NodeItemInfo nodeInfo)
        {
            if (NodeDoubleClick != null) NodeDoubleClick(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeDoubleClick));
        }
        /// <summary>
        /// TreeView právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="NodeItemInfo.LazyLoadChilds"/>).
        /// </summary>
        public event DxTreeViewNodeHandler NodeExpanded;
        /// <summary>
        /// Vyvolá event <see cref="NodeExpanded"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeExpanded(NodeItemInfo nodeInfo)
        {
            if (NodeExpanded != null) NodeExpanded(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeExpanded));
        }
        /// <summary>
        /// TreeView právě sbaluje určitý Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeCollapsed;
        /// <summary>
        /// Vyvolá event <see cref="NodeCollapsed"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeCollapsed(NodeItemInfo nodeInfo)
        {
            if (NodeCollapsed != null) NodeCollapsed(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeCollapsed));
        }
        /// <summary>
        /// TreeView právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        public event DxTreeViewNodeHandler ActivatedEditor;
        /// <summary>
        /// Vyvolá event <see cref="ActivatedEditor"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        protected virtual void OnActivatedEditor(NodeItemInfo nodeInfo)
        {
            if (ActivatedEditor != null) ActivatedEditor(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.ActivatedEditor));
        }
        /// <summary>
        /// TreeView právě skončil editaci určitého Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeEdited;
        /// <summary>
        /// Vyvolá event <see cref="NodeEdited"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        protected virtual void OnNodeEdited(NodeItemInfo nodeInfo, object editedValue)
        {
            if (NodeEdited != null) NodeEdited(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeEdited, editedValue));
        }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        public event DxTreeViewNodeHandler EditorDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="EditorDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        protected virtual void OnEditorDoubleClick(NodeItemInfo nodeInfo, object editedValue)
        {
            if (EditorDoubleClick != null) EditorDoubleClick(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.EditorDoubleClick, editedValue));
        }
        /// <summary>
        /// TreeView rozbaluje node, který má nastaveno načítání ze serveru : <see cref="NodeItemInfo.LazyLoadChilds"/> je true.
        /// </summary>
        public event DxTreeViewNodeHandler LazyLoadChilds;
        /// <summary>
        /// Vyvolá event <see cref="LazyLoadChilds"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnLazyLoadChilds(NodeItemInfo nodeInfo)
        {
            if (LazyLoadChilds != null) LazyLoadChilds(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.LazyLoadChilds));
        }
        #endregion
    }
    #region Deklarace delegátů a tříd pro eventhandlery
    public delegate void DxTreeViewNodeHandler(object sender, DxTreeViewNodeArgs args);
    public class DxTreeViewNodeArgs : EventArgs
    {
        public DxTreeViewNodeArgs(NodeItemInfo nodeInfo, TreeViewActionType action, object editedValue = null)
        {
            this.NodeItemInfo = nodeInfo;
            this.Action = action;
            this.EditedValue = editedValue;
        }
        public NodeItemInfo NodeItemInfo { get; private set; }
        public TreeViewActionType Action { get; private set; }
        public object EditedValue { get; private set; }
    }
    public enum TreeViewActionType
    {
        None,
        NodeSelected,
        NodeDoubleClick,
        NodeExpanded,
        NodeCollapsed,
        ActivatedEditor,
        EditorDoubleClick,
        NodeEdited,
        LazyLoadChilds
    }
    #endregion
    #region class NodeItemInfo : Data o jednom Node
    /// <summary>
    /// Data o jednom Node
    /// </summary>
    public class NodeItemInfo : ITreeNodeItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <param name="parentNodeKey"></param>
        /// <param name="text"></param>
        /// <param name="editable"></param>
        /// <param name="expanded"></param>
        /// <param name="lazyLoadChilds"></param>
        /// <param name="imageName"></param>
        /// <param name="imageNameSelected"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="fontStyleDelta"></param>
        /// <param name="backColor"></param>
        /// <param name="foreColor"></param>
        public NodeItemInfo(string nodeKey, string parentNodeKey, string text,
            bool editable = false, bool expanded = false, bool lazyLoadChilds = false,
            string imageName = null, string imageNameSelected = null, string toolTipTitle = null, string toolTipText = null,
            int? fontSizeDelta = null, FontStyle? fontStyleDelta = null, Color? backColor = null, Color? foreColor = null)
        {
            _Id = -1;
            this.NodeKey = nodeKey;
            this.ParentNodeKey = parentNodeKey;
            this.Text = text;
            this.Editable = editable;
            this.Expanded = expanded;
            this.LazyLoadChilds = lazyLoadChilds;
            this.ImageName = imageName;
            this.ImageNameSelected = imageNameSelected;
            this.ToolTipTitle = toolTipTitle;
            this.ToolTipText = toolTipText;
            this.FontSizeDelta = fontSizeDelta;
            this.FontStyleDelta = fontStyleDelta;
            this.BackColor = backColor;
            this.ForeColor = foreColor;
        }
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// ID nodu v TreeView, pokud není v TreeView pak je -1 
        /// </summary>
        public int NodeId { get { return _Id; } }
        /// <summary>
        /// String klíč nodu, musí být unique
        /// </summary>
        public string NodeKey { get; private set; }
        public string ParentNodeKey { get; private set; }
        public string Text { get; private set; }
        public bool CanCheck { get; private set; }
        public bool IsChecked { get; private set; }
        public string ImageName { get; set; }
        public string ImageNameSelected { get; set; }
        public bool Editable { get; set; }
        public bool Expanded { get; set; }
        public bool LazyLoadChilds { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipText { get; set; }
        public int? FontSizeDelta { get; set; }
        public FontStyle? FontStyleDelta { get; set; }
        public Color? BackColor { get; set; }
        public Color? ForeColor { get; set; }

        #region Implementace ITreeViewItemId
        DxTreeViewListSimple ITreeNodeItem.Owner
        {
            get { if (_Owner != null && _Owner.TryGetTarget(out var owner)) return owner; return null; }
            set { _Owner = (value != null ? new WeakReference<DxTreeViewListSimple>(value) : null); }
        }
        int ITreeNodeItem.Id { get { return _Id; } set { _Id = value; } }
        WeakReference<DxTreeViewListSimple> _Owner;
        int _Id;
        #endregion
    }
    public interface ITreeNodeItem
    {
        DxTreeViewListSimple Owner { get; set; }
        int Id { get; set; }
    }
    #endregion
    #endregion
    #region DxSimpleButton
    /// <summary>
    /// SimpleButton
    /// </summary>
    public class DxSimpleButton : DXE.SimpleButton
    {
    }
    #endregion
}
