// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;
// using BAR = DevExpress.XtraBars;
// using EDI = DevExpress.XtraEditors;
// using TAB = DevExpress.XtraTab;
// using GRD = DevExpress.XtraGrid;
// using CHT = DevExpress.XtraCharts;
// using RIB = DevExpress.XtraBars.Ribbon;
// using NOD = DevExpress.XtraTreeList.Nodes;

namespace Noris.Clients.Win.Components.AsolDX
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

            var mainTitleStyle = new DevExpress.XtraEditors.StyleController();
            mainTitleStyle.Appearance.FontSizeDelta = 4;
            mainTitleStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            mainTitleStyle.Appearance.Options.UseBorderColor = false;
            mainTitleStyle.Appearance.Options.UseBackColor = false;
            mainTitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var subTitleStyle = new DevExpress.XtraEditors.StyleController();
            subTitleStyle.Appearance.FontSizeDelta = 2;
            subTitleStyle.Appearance.FontStyleDelta = FontStyle.Bold;
            subTitleStyle.Appearance.Options.UseBorderColor = false;
            subTitleStyle.Appearance.Options.UseBackColor = false;
            subTitleStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var labelStyle = new DevExpress.XtraEditors.StyleController();
            labelStyle.Appearance.FontSizeDelta = 0;
            labelStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            labelStyle.Appearance.Options.UseBorderColor = false;
            labelStyle.Appearance.Options.UseBackColor = false;
            labelStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var infoStyle = new DevExpress.XtraEditors.StyleController();
            infoStyle.Appearance.FontSizeDelta = 0;
            infoStyle.Appearance.FontStyleDelta = FontStyle.Italic;
            infoStyle.Appearance.Options.UseBorderColor = false;
            infoStyle.Appearance.Options.UseBackColor = false;
            infoStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            var inputStyle = new DevExpress.XtraEditors.StyleController();
            inputStyle.Appearance.FontSizeDelta = 0;
            inputStyle.Appearance.FontStyleDelta = FontStyle.Regular;
            inputStyle.Appearance.Options.UseBorderColor = false;
            inputStyle.Appearance.Options.UseBackColor = false;
            inputStyle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.NoWrap;

            _MainTitleStyle = mainTitleStyle;
            _SubTitleStyle = subTitleStyle;
            _LabelStyle = labelStyle;
            _InfoStyle = infoStyle;
            _InputStyle = inputStyle;

            _DetailXLabel = 16;
            _DetailXText = 12;
            _DetailYFirst = 9;
            _DetailYHeightLabel = 19;
            _DetailYHeightText = 22;
            _DetailYOffsetLabelText = 5;
            _DetailYSpaceLabel = 2;
            _DetailYSpaceText = 3;
            _DetailXMargin = 6;
            _DetailYMargin = 4;

            _DefaultButtonPanelHeight = 40;
            _DefaultButtonWidth = 150;
            _DefaultButtonHeight = 32;
        }
        /// <summary>
        /// Vrátí styl labelu podle požadovaného typu
        /// </summary>
        /// <param name="styleType"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.IStyleController _GetLabelStyle(LabelStyleType? styleType)
        {
            switch (styleType)
            {
                case LabelStyleType.MainTitle: return _MainTitleStyle;
                case LabelStyleType.SubTitle: return _SubTitleStyle;
                case LabelStyleType.Default: return _LabelStyle;
                case LabelStyleType.Info: return _InfoStyle;
            }
            return _LabelStyle;
        }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.MainTitle"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController MainTitleStyle { get { return Instance._MainTitleStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.SubTitle"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController TitleStyle { get { return Instance._SubTitleStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Default"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController LabelStyle { get { return Instance._LabelStyle; } }
        /// <summary>
        /// Styl pro labely zobrazené jako <see cref="LabelStyleType.Info"/>
        /// </summary>
        public static DevExpress.XtraEditors.StyleController InfoStyle { get { return Instance._InfoStyle; } }
        /// <summary>
        /// Styl pro Input prvky
        /// </summary>
        public static DevExpress.XtraEditors.StyleController InputStyle { get { return Instance._InputStyle; } }
        /// <summary>
        /// Odsazení labelu od levého okraje X
        /// </summary>
        public static int DetailXLabel { get { return Instance._DetailXLabel; } }
        /// <summary>
        /// Odsazení textu od levého okraje X
        /// </summary>
        public static int DetailXText { get { return Instance._DetailXText; } }
        /// <summary>
        /// Odsazení prvního prvku od horního okraje Y
        /// </summary>
        public static int DetailYFirst { get { return Instance._DetailYFirst; } }
        /// <summary>
        /// Výchozí hodnota výšky labelu
        /// </summary>
        public static int DetailYHeightLabel { get { return Instance._DetailYHeightLabel; } }
        /// <summary>
        /// Výchozí hodnota výšky textu
        /// </summary>
        public static int DetailYHeightText { get { return Instance._DetailYHeightText; } }
        /// <summary>
        /// Posun labelu vůči textu v ose Y pro zarovnané úpatí textu
        /// </summary>
        public static int DetailYOffsetLabelText { get { return Instance._DetailYOffsetLabelText; } }
        /// <summary>
        /// Odsazení labelu dalšího řádku od předešlého textu
        /// </summary>
        public static int DetailYSpaceLabel { get { return Instance._DetailYSpaceLabel; } }
        /// <summary>
        /// Odsazení textu řádku od předešlého labelu
        /// </summary>
        public static int DetailYSpaceText { get { return Instance._DetailYSpaceText; } }
        /// <summary>
        /// Okraj v ose X
        /// </summary>
        public static int DetailXMargin { get { return Instance._DetailXMargin; } }
        /// <summary>
        /// Okraj v ose Y
        /// </summary>
        public static int DetailYMargin { get { return Instance._DetailYMargin; } }
        /// <summary>
        /// Defaultní výška panelu s buttony
        /// </summary>
        public static int DefaultButtonPanelHeight { get { return Instance._DefaultButtonPanelHeight; } }
        /// <summary>
        /// Defaultní šířka buttonu
        /// </summary>
        public static int DefaultButtonWidth { get { return Instance._DefaultButtonWidth; } }
        /// <summary>
        /// Defaultní výška buttonu
        /// </summary>
        public static int DefaultButtonHeight { get { return Instance._DefaultButtonHeight; } }

        private DevExpress.XtraEditors.StyleController _MainTitleStyle;
        private DevExpress.XtraEditors.StyleController _SubTitleStyle;
        private DevExpress.XtraEditors.StyleController _LabelStyle;
        private DevExpress.XtraEditors.StyleController _InfoStyle;
        private DevExpress.XtraEditors.StyleController _InputStyle;
        private int _DetailXLabel;
        private int _DetailXText;
        private int _DetailYFirst;
        private int _DetailYHeightLabel;
        private int _DetailYHeightText;
        private int _DetailYOffsetLabelText;
        private int _DetailYSpaceLabel;
        private int _DetailYSpaceText;
        private int _DetailXMargin;
        private int _DetailYMargin;
        private int _DefaultButtonPanelHeight;
        private int _DefaultButtonWidth;
        private int _DefaultButtonHeight;
        #endregion
        #region Factory metody pro jednořádkovou tvorbu běžných komponent
        public static DxPanelControl CreateDxPanel(Control parent = null,
            DockStyle? dock = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
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
            Orientation splitLineOrientation = Orientation.Horizontal, DevExpress.XtraEditors.SplitFixedPanel? fixedPanel = null,
            int? splitPosition = null, DevExpress.XtraEditors.SplitPanelVisibility? panelVisibility = null,
            bool? showSplitGlyph = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null)
        {
            var inst = Instance;

            var container = new DxSplitContainerControl() { Horizontal = (splitLineOrientation == Orientation.Vertical) };
            if (parent != null) parent.Controls.Add(container);
            if (dock.HasValue) container.Dock = dock.Value;
            container.FixedPanel = (fixedPanel ?? DevExpress.XtraEditors.SplitFixedPanel.None);
            container.SplitterPosition = (splitPosition ?? 200);
            container.PanelVisibility = (panelVisibility ?? DevExpress.XtraEditors.SplitPanelVisibility.Both);
            if (borderStyles.HasValue) container.BorderStyle = borderStyles.Value;
            container.ShowSplitGlyph = (showSplitGlyph.HasValue ? (showSplitGlyph.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False) : DevExpress.Utils.DefaultBoolean.Default);

            return container;
        }
        public static DxLabelControl CreateDxLabel(int x, int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DevExpress.XtraEditors.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool useLabelTextOffset = false)
        {
            return CreateDxLabel(x, ref y, w, parent, text,
                styleType, wordWrap, autoSizeMode, hAlignment,
                visible, useLabelTextOffset, false);
        }
        public static DxLabelControl CreateDxLabel(int x, ref int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, DevExpress.Utils.WordWrap? wordWrap = null, DevExpress.XtraEditors.LabelAutoSizeMode? autoSizeMode = null, DevExpress.Utils.HorzAlignment? hAlignment = null,
            bool? visible = null, bool useLabelTextOffset = false, bool shiftY = false)
        {
            var inst = Instance;

            int yOffset = (useLabelTextOffset ? inst._DetailYOffsetLabelText : 0);
            var label = new DxLabelControl() { Bounds = new Rectangle(x, y + yOffset, w, inst._DetailYHeightLabel), Text = text };
            label.StyleController = inst._GetLabelStyle(styleType);
            if (wordWrap.HasValue) label.Appearance.TextOptions.WordWrap = wordWrap.Value;
            if (hAlignment.HasValue) label.Appearance.TextOptions.HAlignment = hAlignment.Value;
            if (wordWrap.HasValue || hAlignment.HasValue) label.Appearance.Options.UseTextOptions = true;
            if (hAlignment.HasValue) label.Appearance.TextOptions.HAlignment = hAlignment.Value;
            if (autoSizeMode.HasValue) label.AutoSizeMode = autoSizeMode.Value;

            if (visible.HasValue) label.Visible = visible.Value;

            if (parent != null) parent.Controls.Add(label);
            if (shiftY) y = label.Bounds.Bottom + inst._DetailYSpaceLabel;

            return label;
        }
        public static DxTextEdit CreateDxTextEdit(int x, int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxTextEdit(x, ref y, w, parent, textChanged,
                maskType, editMask, useMaskAsDisplayFormat,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxTextEdit CreateDxTextEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
            DevExpress.XtraEditors.Mask.MaskType? maskType = null, string editMask = null, bool? useMaskAsDisplayFormat = null,
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
        public static DxMemoEdit CreateDxMemoEdit(int x, int y, int w, int h, Control parent, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxMemoEdit(x, ref y, w, h, parent, textChanged,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxMemoEdit CreateDxMemoEdit(int x, ref int y, int w, int h, Control parent, EventHandler textChanged = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var memoEdit = new DxMemoEdit() { Bounds = new Rectangle(x, y, w, h) };
            memoEdit.StyleController = inst._InputStyle;
            if (visible.HasValue) memoEdit.Visible = visible.Value;
            if (readOnly.HasValue) memoEdit.ReadOnly = readOnly.Value;
            if (tabStop.HasValue) memoEdit.TabStop = tabStop.Value;

            memoEdit.SetToolTip(toolTipTitle, toolTipText);

            if (textChanged != null) memoEdit.TextChanged += textChanged;
            if (parent != null) parent.Controls.Add(memoEdit);
            if (shiftY) y = y + memoEdit.Height + inst._DetailYSpaceText;

            return memoEdit;
        }
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxImageComboBox(x, ref y, w, parent, selectedIndexChanged, itemsTabbed,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxImageComboBoxEdit CreateDxImageComboBox(int x, ref int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
            string toolTipTitle = null, string toolTipText = null,
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

            comboBox.SetToolTip(toolTipTitle, toolTipText);

            if (selectedIndexChanged != null) comboBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(comboBox);
            if (shiftY) y = y + comboBox.Height + inst._DetailYSpaceText;
            return comboBox;
        }
        public static DxSpinEdit CreateDxSpinEdit(int x, int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DevExpress.XtraEditors.Controls.SpinStyles? spinStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxSpinEdit(x, ref y, w, parent, valueChanged,
                minValue, maxValue, increment, mask, spinStyles,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxSpinEdit CreateDxSpinEdit(int x, ref int y, int w, Control parent, EventHandler valueChanged = null,
            decimal? minValue = null, decimal? maxValue = null, decimal? increment = null, string mask = null, DevExpress.XtraEditors.Controls.SpinStyles? spinStyles = null,
            string toolTipTitle = null, string toolTipText = null,
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

            spinEdit.SetToolTip(toolTipTitle, toolTipText);

            if (valueChanged != null) spinEdit.ValueChanged += valueChanged;
            if (parent != null) parent.Controls.Add(spinEdit);
            if (shiftY) y = y + spinEdit.Height + inst._DetailYSpaceText;
            return spinEdit;
        }
        public static DxCheckEdit CreateDxCheckEdit(int x, int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DevExpress.XtraEditors.Controls.CheckBoxStyle? checkBoxStyle = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? readOnly = null, bool? tabStop = null)
        {
            return CreateDxCheckEdit(x, ref y, w, parent, text, checkedChanged,
                checkBoxStyle, borderStyles,
                toolTipTitle, toolTipText,
                visible, readOnly, tabStop, false);
        }
        public static DxCheckEdit CreateDxCheckEdit(int x, ref int y, int w, Control parent, string text, EventHandler checkedChanged = null,
            DevExpress.XtraEditors.Controls.CheckBoxStyle? checkBoxStyle = null, DevExpress.XtraEditors.Controls.BorderStyles? borderStyles = null,
            string toolTipTitle = null, string toolTipText = null,
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

            checkEdit.SetToolTip(toolTipTitle ?? text, toolTipText);

            if (checkedChanged != null) checkEdit.CheckedChanged += checkedChanged;
            if (parent != null) parent.Controls.Add(checkEdit);
            if (shiftY) y = y + checkEdit.Height + inst._DetailYSpaceText;

            return checkEdit;
        }
        public static DxListBoxControl CreateDxListBox(DockStyle? dock = null, int? width = null, int? height = null, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? tabStop = null)
        {
            int y = 0;
            int w = width ?? 0;
            int h = height ?? 0;
            return CreateDxListBox(0, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding, reorderByDragEnabled,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null)
        {
            return CreateDxListBox(x, ref y, w, h, parent, selectedIndexChanged,
                multiColumn, selectionMode, itemHeight, itemHeightPadding, reorderByDragEnabled,
                toolTipTitle, toolTipText,
                dock, visible, tabStop, false);
        }
        public static DxListBoxControl CreateDxListBox(int x, ref int y, int w, int h, Control parent = null, EventHandler selectedIndexChanged = null,
            bool? multiColumn = null, SelectionMode? selectionMode = null, int? itemHeight = null, int? itemHeightPadding = null, bool? reorderByDragEnabled = null,
            string toolTipTitle = null, string toolTipText = null,
            DockStyle? dock = null, bool? visible = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            DxListBoxControl listBox = new DxListBoxControl() { Bounds = new Rectangle(x, y, w, h) };
            listBox.StyleController = inst._InputStyle;
            if (dock.HasValue) listBox.Dock = dock.Value;
            if (multiColumn.HasValue) listBox.MultiColumn = multiColumn.Value;
            if (selectionMode.HasValue) listBox.SelectionMode = selectionMode.Value;
            if (itemHeight.HasValue) listBox.ItemHeight = itemHeight.Value;
            if (itemHeightPadding.HasValue) listBox.ItemHeightPadding = itemHeightPadding.Value;

            if (reorderByDragEnabled.HasValue) listBox.ReorderByDragEnabled = reorderByDragEnabled.Value;
            if (visible.HasValue) listBox.Visible = visible.Value;
            if (tabStop.HasValue) listBox.TabStop = tabStop.Value;

            listBox.SetToolTip(toolTipTitle, toolTipText);

            if (selectedIndexChanged != null) listBox.SelectedIndexChanged += selectedIndexChanged;
            if (parent != null) parent.Controls.Add(listBox);
            if (shiftY) y = y + listBox.Height + inst._DetailYSpaceText;

            return listBox;
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null)
        {
            return CreateDxSimpleButton(x, ref y, w, h, parent, text, click,
                paintStyles,
                toolTipTitle, toolTipText,
                visible, enabled, tabStop, false);
        }
        public static DxSimpleButton CreateDxSimpleButton(int x, ref int y, int w, int h, Control parent, string text, EventHandler click = null,
            DevExpress.XtraEditors.Controls.PaintStyles? paintStyles = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null, bool shiftY = false)
        {
            var inst = Instance;

            var simpleButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            simpleButton.StyleController = inst._InputStyle;
            simpleButton.Text = text;
            if (visible.HasValue) simpleButton.Visible = visible.Value;
            if (enabled.HasValue) simpleButton.Enabled = enabled.Value;
            if (tabStop.HasValue) simpleButton.TabStop = tabStop.Value;
            if (paintStyles.HasValue) simpleButton.PaintStyle = paintStyles.Value;

            simpleButton.SetToolTip(toolTipTitle ?? text, toolTipText);

            if (click != null) simpleButton.Click += click;
            if (parent != null) parent.Controls.Add(simpleButton);
            if (shiftY) y = y + simpleButton.Height + inst._DetailYSpaceText;

            return simpleButton;
        }
        public static DxSimpleButton CreateDxMiniButton(int x, int y, int w, int h, Control parent, EventHandler click = null,
            Image image = null, string resourceName = null,
            Image hotImage = null, string hotResourceName = null,
            string toolTipTitle = null, string toolTipText = null,
            bool? visible = null, bool? enabled = null, bool? tabStop = null,
            object tag = null)
        {
            var inst = Instance;

            var miniButton = new DxSimpleButton() { Bounds = new Rectangle(x, y, w, h) };
            miniButton.StyleController = inst._InputStyle;
            miniButton.Text = "";
            miniButton.Tag = tag;

            if (visible.HasValue) miniButton.Visible = visible.Value;
            if (enabled.HasValue) miniButton.Enabled = enabled.Value;
            miniButton.TabStop = tabStop ?? false;
            miniButton.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;

            DxComponent.ApplyImage(miniButton.ImageOptions, image, resourceName, new Size(w - 4, h - 4), true);

            miniButton.Padding = new Padding(0);
            miniButton.Margin = new Padding(0);

            miniButton.SetToolTip(toolTipTitle, toolTipText);

            if (click != null) miniButton.Click += click;
            if (parent != null) parent.Controls.Add(miniButton);

            return miniButton;
        }
        /// <summary>
        /// Vytvoří a vrátí standardní ToolTipController
        /// </summary>
        /// <returns></returns>
        public static ToolTipController CreateToolTipController()
        {
            ToolTipController toolTipController = new ToolTipController();

            return toolTipController;
        }
        /// <summary>
        /// Vytvoří a vrátí standardní SuperToolTip pro daný titulek a text
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static SuperToolTip CreateDxSuperTip(string title, string text)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(text)) return null;

            var superTip = new DevExpress.Utils.SuperToolTip();
            if (title != null) superTip.Items.AddTitle(title);
            superTip.Items.Add(text);

            return superTip;
        }
        #endregion
        #region ImageResource
        /// <summary>
        /// Vrací setříděný seznam DevExpress resources
        /// </summary>
        /// <param name="addPng">Akceptovat bitmapy (PNG)</param>
        /// <param name="addSvg">Akceptovat vektory (SVG)</param>
        /// <returns></returns>
        public static string[] GetResourceKeys(bool addPng = true, bool addSvg = true)
        {
            return Instance._GetResourceKeys(addPng, addSvg);
        }
        /// <summary>
        /// Vrací setříděný seznam DevExpress resources
        /// </summary>
        /// <param name="addPng">Akceptovat bitmapy (PNG)</param>
        /// <param name="addSvg">Akceptovat vektory (SVG)</param>
        /// <returns></returns>
        protected string[] _GetResourceKeys(bool addPng, bool addSvg)
        {
            if (!addPng && !addSvg) return new string[0];

            var keyList = _ImageResourceCache.GetAllResourceKeys()
                .Where(k => (addPng && k.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) || (addSvg && k.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            keyList.Sort();

            return keyList.ToArray();
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName, 
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return GetImageFromResource(resourceName, out Size size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static Image GetImageFromResource(string resourceName, out Size size,
            Size? maxSize = null, Size? optimalSvgSize = null, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            return Instance._GetImageFromResource(resourceName, out size,
                maxSize, optimalSvgSize, svgPalette, svgState);
        }
        /// <summary>
        /// Vrátí Image (bitmapu) pro daný název DevExpress zdroje.
        /// Vstupem může být SVG i PNG zdroj.
        /// <para/>
        /// Pro SVG obrázek je vhodné:
        /// 1. určit <paramref name="optimalSvgSize"/>, pak bude Image renderován exaktně na zadaný rozměr.
        /// 2. předat i paletu <paramref name="svgPalette"/>, tím bude SVG obrázek přizpůsoben danému skinu a stavu.
        /// 3. Pokud nebude předána paleta, lze zadat alespoň stav objektu <paramref name="svgState"/> (default = Normal), pak bude použit aktuální skin a daný stav objektu.
        /// </summary>
        /// <param name="resourceName">Název zdroje, zdroje jsou k dispozici v <see cref="GetResourceKeys(bool, bool)"/></param>
        /// <param name="size">Výstup konkrétní velikosti, odráží velikost bitmapy, nebo <paramref name="optimalSvgSize"/> pro SVG, je oříznuto na <paramref name="maxSize"/></param>
        /// <param name="maxSize"></param>
        /// <param name="optimalSvgSize">Cílová velikost, použije se pouze pro vykreslení SVG Image</param>
        /// <param name="svgPalette">Paleta pro vykreslení SVG Image</param>
        /// <param name="svgState">Stav objektu pro vykreslení SVG Image, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        protected Image _GetImageFromResource(string resourceName, out Size size,
            Size? maxSize, Size? optimalSvgSize, DevExpress.Utils.Design.ISvgPaletteProvider svgPalette, DevExpress.Utils.Drawing.ObjectState? svgState)
        {
            System.Drawing.Image image = null;
            size = new Size(32, 32);
            if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        if (svgPalette == null)
                            svgPalette = GetSvgPalette(DevExpress.LookAndFeel.UserLookAndFeel.Default, svgState);
                        if (optimalSvgSize.HasValue)
                            size = optimalSvgSize.Value;
                        else if (maxSize.HasValue)
                            size = maxSize.Value;
                        _ImageResourceRewindStream(resourceName);
                        image = _ImageResourceCache.GetSvgImage(resourceName, svgPalette, size);
                    }
                    else
                    {
                        image = _ImageResourceCache.GetImage(resourceName);
                        size = image.Size;
                        if (maxSize.HasValue)
                        {
                            if (maxSize.Value.Width > 0 && size.Width > maxSize.Value.Width) size.Width = maxSize.Value.Width;
                            if (maxSize.Value.Height > 0 && size.Height > maxSize.Value.Height) size.Height = maxSize.Value.Height;
                        }
                    }
                }
                catch (Exception exc)
                {
                    image = null;
                }
            }
            return image;
        }
        /// <summary>
        /// Vrátí SVG paletu [volitelně pro daný skin a pro daný stav objektu]
        /// </summary>
        /// <param name="skinProvider">Cílový skin, implicitně bude použit <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/></param>
        /// <param name="svgState">Stav objektu, implicitní je <see cref="DevExpress.Utils.Drawing.ObjectState.Normal"/></param>
        /// <returns></returns>
        public static DevExpress.Utils.Design.ISvgPaletteProvider GetSvgPalette(DevExpress.Skins.ISkinProvider skinProvider = null, DevExpress.Utils.Drawing.ObjectState? svgState = null)
        {
            if (skinProvider == null) skinProvider = DevExpress.LookAndFeel.UserLookAndFeel.Default;
            if (!svgState.HasValue) svgState = DevExpress.Utils.Drawing.ObjectState.Normal;
            return DevExpress.Utils.Svg.SvgPaletteHelper.GetSvgPalette(skinProvider, svgState.Value);
        }
        public static DevExpress.Utils.SvgImageCollection SvgImageCollection { get { return Instance._SvgImageCollection; } }
        public static DevExpress.Utils.Svg.SvgImage GetSvgImage(string key) { return Instance._GetSvgImage(key); }
        public static void ApplyImage(ImageCollectionImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null) { Instance._ApplyImage(imageOptions, image, resourceName, imageSize); }
        protected void _ApplyImage(ImageCollectionImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }
            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        _ImageResourceRewindStream(resourceName);
                        imageOptions.SvgImage = _ImageResourceCache.GetSvgImage(resourceName);
                        if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
                    }
                    else
                    {
                        imageOptions.Image = _ImageResourceCache.GetImage(resourceName);
                    }
                }
                catch { }
            }
        }
        public static void ApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false) { Instance._ApplyImage(imageOptions, image, resourceName, imageSize, smallButton); }
        protected void _ApplyImage(DevExpress.XtraEditors.SimpleButtonImageOptions imageOptions, Image image = null, string resourceName = null, Size? imageSize = null, bool smallButton = false)
        {
            if (image != null)
            {
                imageOptions.Image = image;
            }
            else if (!String.IsNullOrEmpty(resourceName))
            {
                try
                {
                    if (_IsImageNameSvg(resourceName))
                    {
                        _ImageResourceRewindStream(resourceName);
                        imageOptions.SvgImage = _ImageResourceCache.GetSvgImage(resourceName);
                        if (imageSize.HasValue) imageOptions.SvgImageSize = imageSize.Value;
                    }
                    else
                    {
                        imageOptions.Image = _ImageResourceCache.GetImage(resourceName);
                    }
                }
                catch { }
            }
            if (smallButton)
            {
                imageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
//                imageOptions.ImageToTextIndent = 0;
  //              imageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.BottomCenter;
            }
        }
        /// <summary>
        /// Vrátí true, pokud dané jméno zdroje končí příponou ".svg"
        /// </summary>
        /// <param name="imageUri"></param>
        /// <returns></returns>
        protected static bool _IsImageNameSvg(string imageUri)
        {
            return (!String.IsNullOrEmpty(imageUri) && imageUri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Napravuje chybu DevExpress, kdy v <see cref="DevExpress.Images.ImageResourceCache"/> pro SVG zdroje po jejich použití je jejich zdrojový stream na konci, a další použití je tak znemožněno.
        /// </summary>
        /// <param name="resourceName"></param>
        protected void _ImageResourceRewindStream(string resourceName)
        {
            if (String.IsNullOrEmpty(resourceName)) return;

            var imageResourceCache = _ImageResourceCache;
            var dictonaryField = imageResourceCache.GetType().GetField("resources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (dictonaryField == null) return;
            object dictonaryValue = dictonaryField.GetValue(imageResourceCache);

            if (!(dictonaryValue is Dictionary<string, System.IO.Stream> dictionary)) return;
            if (!dictionary.TryGetValue(resourceName, out System.IO.Stream stream)) return;

            var position = stream.Position;
            if (stream.Position > 0L && stream.CanSeek)
                stream.Seek(0L, System.IO.SeekOrigin.Begin);
        }
        protected DevExpress.Utils.Svg.SvgImage _GetSvgImage(string key)
        {
            if (String.IsNullOrEmpty(key)) return null;

            if (!key.StartsWith(ImageUriPrefix, StringComparison.OrdinalIgnoreCase)) key = ImageUriPrefix + key;
            var svgImageCollection = _SvgImageCollection;
            if (!svgImageCollection.ContainsKey(key))
                svgImageCollection.Add(key, key);

            return svgImageCollection[key];
        }
        /// <summary>
        /// Prefix pro ImageUri: "image://"
        /// </summary>
        protected static string ImageUriPrefix { get { return "image://"; } }
        /// <summary>
        /// Cache systémových image resources
        /// </summary>
        protected DevExpress.Images.ImageResourceCache _ImageResourceCache
        {
            get
            {
                if (__ImageResourceCache == null)
                    __ImageResourceCache = DevExpress.Images.ImageResourceCache.Default;
                return __ImageResourceCache;
            }
        }
        private DevExpress.Images.ImageResourceCache __ImageResourceCache;
        protected DevExpress.Utils.SvgImageCollection _SvgImageCollection 
        { 
            get 
            {
                if (__SvgImageCollection == null)
                    __SvgImageCollection = new SvgImageCollection();
                return __SvgImageCollection;
            }
        }
        private DevExpress.Utils.SvgImageCollection __SvgImageCollection;
        #endregion
    }
    #endregion
    #region class DrawingExtensions : Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// <summary>
    /// Extensions metody pro grafické třídy (z namespace System.Drawing)
    /// </summary>
    public static class DrawingExtensions
    {
        /// <summary>
        /// Vrátí IDisposable blok, který na svém počátku (při vyvolání této metody) provede control?.Parent.SuspendLayout(), 
        /// a na konci bloku (při Dispose) provede control?.Parent.ResumeLayout(false)
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static IDisposable ScopeSuspendParentLayout(this Control control)
        {
            return new UsingScope(
            (s) =>
            {   // OnBegin (Constructor):
                Control parent = control?.Parent;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.SuspendLayout();
                }
                s.UserData = parent;
            },
            (s) =>
            {   // OnEnd (Dispose):
                Control parent = s.UserData as Control;
                if (parent != null && !parent.IsDisposed)
                {
                    parent.ResumeLayout(false);
                    parent.PerformLayout();
                }
                s.UserData = null;
            }
            );
        }
        /// <summary>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato metoda <see cref="IsSetVisible(Control)"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool IsSetVisible(this Control control)
        {
            if (control is null) return false;
            var getState = control.GetType().GetMethod("GetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic);
            if (getState is null) return false;
            object visible = getState.Invoke(control, new object[] { (int)0x02  /*STATE_VISIBLE*/  });
            return (visible is bool ? (bool)visible : false);
        }
        /// <summary>
        /// Vrátí nejbližšího Parenta požadovaného typu pro this control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <returns></returns>
        public static T SearchForParentOfType<T>(this Control control) where T : Control
        {
            Control item = control?.Parent;                // Tímhle řádkem zajistím, že nebudu vracet vstupní objekt, i kdyby byl požadovaného typu = protože hledám Parenta, nikoli sebe.
            while (item != null)
            {
                if (item is T result) return result;
                item = item.Parent;
            }
            return null;
        }
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public static void RunInGui(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T GetGuiValue<T>(this Control control, Func<T> reader)
        {
            if (control.InvokeRequired)
                return (T)control.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public static void SetGuiValue<T>(this Control control, Action<T> writer, T value)
        {
            if (control.InvokeRequired)
                control.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
    }
    #endregion
    #region class UsingScope : Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd
    /// <summary>
    /// Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
    /// </summary>
    internal class UsingScope : IDisposable
    {
        /// <summary>
        /// Jednoduchý scope, který provede při vytvoření akci OnBegin, a při Dispose akci OnEnd.
        /// </summary>
        /// <param name="onBegin">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro uložení dat, budou k dispozici v akci <paramref name="onEnd"/></param>
        /// <param name="onEnd">Jako parametr je předán this scope, lze v něm použít property <see cref="UserData"/> pro čtení dat uložených v akci <paramref name="onBegin"/></param>
        /// <param name="userData">Volitelně UserData</param>
        public UsingScope(Action<UsingScope> onBegin, Action<UsingScope> onEnd, object userData = null)
        {
            this.UserData = userData;
            _OnEnd = onEnd;
            onBegin?.Invoke(this);
        }
        private Action<UsingScope> _OnEnd;
        void IDisposable.Dispose()
        {
            _OnEnd?.Invoke(this);
            _OnEnd = null;
        }
        /// <summary>
        /// Libovolná data.
        /// Typicky jsou vloženy v akci OnBegin, a v akci OnEnd jsou načteny. Výchozí hodnota je null.
        /// </summary>
        public object UserData { get; set; }
    }
    #endregion
    #region class RegexSupport
    /// <summary>
    /// Třída pro podporu konverze Wildcard patternu na <see cref="Regex"/>
    /// </summary>
    public static class RegexSupport
    {
        /// <summary>
        /// Metoda vrátí true, pokud hodnota value vyhovuje vzorci pattern.
        /// Pro opakované používání stejného patternu je vhodnější získat <see cref="Regex"/> metodou <see cref="CreateWildcardsRegex(string, bool, bool)"/>,
        /// a ten pak používat pro testování různých hodnot opakovaně.
        /// </summary>
        /// <param name="value">Hodnota, například "Abcdef ghij"</param>
        /// <param name="pattern">Vzorec, například "Abc??f *"</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static bool IsMatchWildcards(string value, string pattern, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            if (!IsWildcardsValid(pattern, checkIllegalFileCharacters)) return false;
            Regex regex = CreateWildcardsRegex(pattern, matchCase, checkIllegalFileCharacters);
            return regex.IsMatch(value);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daný pattern je formálně správný a může být použit v metodě <see cref="CreateWildcardsRegex(string, bool, bool)"/>.
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static bool IsWildcardsValid(string pattern, bool checkIllegalFileCharacters = false)
        {
            if (pattern == null) return false;
            pattern = pattern.Trim();
            if (pattern.Length == 0) return false;
            if (checkIllegalFileCharacters && IllegalCharactersRegex.IsMatch(pattern)) return false;
            return true;
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="patterns">Patterny s užitím standardních Wildcards * a ?, oddělené středníkem (nebo čárkami)</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů, default = false: nekontrolovat</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string patterns, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            string[] masks = patterns.Trim().Split(';');         // Prioritní oddělovač je středník
            if (masks.Length == 1 && patterns.Contains(','))     // Pokud není přítomen středník, ale je přítomna čárka...
                masks = patterns.Trim().Split(',');              //  ... pak i čárka může hrát roli oddělovače

            return CreateWildcardsRegexes(masks, matchCase, checkIllegalFileCharacters);
        }
        /// <summary>
        /// Metoda vrátí pole <see cref="Regex"/>, které dovolují porovnávat konkrétní texty se standardní Wildcards notací.
        /// Z dodané sady wildcard masek (odděleny středníkem) vrátí pole Regex výrazů pro jejich filtrování.
        /// Pokud je na vstupu Empty, vrací prázdné pole.
        /// Typický vstup: "*.tmp; *.js; *thumb*.*; *.htm*;" atd
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="patterns">Patterny s užitím standardních Wildcards * a ?, zde již rozdělené do pole jednotlivých patternů</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů</param>
        /// <returns></returns>
        public static Regex[] CreateWildcardsRegexes(string[] patterns, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            List<Regex> regexes = new List<Regex>();
            if (patterns != null && patterns.Length > 0)
            {
                foreach (string pattern in patterns)
                {
                    if (!String.IsNullOrEmpty(pattern))
                    {
                        Regex regex = CreateWildcardsRegex(pattern.Trim(), matchCase, checkIllegalFileCharacters);
                        if (regex != null)
                            regexes.Add(regex);
                    }
                }
            }

            return regexes.ToArray();
        }
        /// <summary>
        /// Vrátí dodanou kolekci textů filtrovanou podle dané kolekce regulárních výrazů.
        /// Kolekce <paramref name="data"/> typicky obsahuje seznam souborů nebo názvů;
        /// kolekce <paramref name="regexes"/> obsahuje výstup zdejší metody <see cref="CreateWildcardsRegexes(string, bool, bool)"/>;
        /// výstup zdejší metody pak obsahuje jen vyhovující soubory.
        /// <para/>
        /// Pokud <paramref name="data"/> je null, výstupem je null.
        /// Pokud <paramref name="regexes"/> je null nebo prázdná kolekce, pak výstupem je vstupní kolekce <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static IEnumerable<string> FilterByRegexes(IEnumerable<string> data, IEnumerable<Regex> regexes)
        {
            if (data == null) return null;
            if (regexes == null || regexes.Count() == 0) return data;
            return data.Where(t => IsTextMatchToAny(t, regexes));
        }
        /// <summary>
        /// Vrátí true, pokud daný text vyhovuje některé masce
        /// </summary>
        /// <param name="text"></param>
        /// <param name="regexes"></param>
        /// <returns></returns>
        public static bool IsTextMatchToAny(string text, IEnumerable<Regex> regexes)
        {
            if (text == null) return false;
            if (regexes == null) return false;
            return regexes.Any(mask => mask.IsMatch(text));
        }
        /// <summary>
        /// Metoda vrátí <see cref="Regex"/>, který dovoluje porovnávat texty se standardní Wildcards notací.
        /// Tedy: text "Abcdefg" vyhovuje patternu "Ab??e*".
        /// Volitelně lze požádat (<paramref name="matchCase"/>), aby <see cref="Regex"/> měl vypnutou/zapnutou option <see cref="RegexOptions.IgnoreCase"/>: 
        /// false = ignoruje velikost znaků, true = neignoruje, porovnává exaktně
        /// </summary>
        /// <param name="pattern">Pattern s užitím standardních Wildcards * a ?</param>
        /// <param name="matchCase">Kontrolovat shodu velikosti písmen, default = false: "abc" == "ABc"</param>
        /// <param name="checkIllegalFileCharacters">Vyhodit chybu, pokud pattern obsahuje zakázané znaky pro jména souborů</param>
        /// <returns></returns>
        public static Regex CreateWildcardsRegex(string pattern, bool matchCase = false, bool checkIllegalFileCharacters = false)
        {
            if (pattern == null) throw new ArgumentNullException();

            pattern = pattern.Trim();
            if (pattern.Length == 0) throw new ArgumentException("Pattern is empty.");

            if (checkIllegalFileCharacters && IllegalCharactersRegex.IsMatch(pattern)) throw new ArgumentException("Pattern contains illegal characters.");

            bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
            bool matchExact = false;
            if (HasQuestionMarkRegEx.IsMatch(pattern))
                matchExact = true;
            else if (hasExtension)
                matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;

            string regexString = Regex.Escape(pattern);
            regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
            regexString = Regex.Replace(regexString, @"\\\?", ".");
            if (!matchExact && hasExtension)
            {
                regexString += NonDotCharacters;
            }
            regexString += "$";
            RegexOptions regexOptions = (matchCase ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex regex = new Regex(regexString, regexOptions);
            return regex;
        }
        /// <summary>
        /// Pokud výraz obsahuje tzv. "Green" wildcards (% nebo _) a přitom neobsahuje standardní wildcards (* a ?), pak je nahradí standardními (* a ?)
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string ReplaceGreenWildcards(string filter)
        {
            if (String.IsNullOrEmpty(filter)) return "";
            bool containsGreen = (filter.Contains("%") || filter.Contains("_"));
            bool containsStandard = (filter.Contains("*") || filter.Contains("?"));

            if (containsGreen && !containsStandard)
                filter = filter
                    .Replace('%', '*')
                    .Replace('_', '?');

            return filter;
        }
        private static Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
        private static Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
        private static Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
        private static string NonDotCharacters = @"[^.]*";
    }
    #endregion
    #region class WeakTarget<T> : Typová obálka nad WeakReference
    /// <summary>
    /// Typová obálka nad <see cref="WeakReference"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class WeakTarget<T> where T : class
    {
        /// <summary>
        /// Konstruktor. Lze použít i implicitní konverzi:
        /// <see cref="WeakTarget{T}"/> wt = (T)target; a následně: T target2 = wt.Target;
        /// </summary>
        /// <param name="target"></param>
        public WeakTarget(T target)
        {
            this._Wref = (target != null ? new WeakReference(target) : null);
        }
        private readonly WeakReference _Wref;
        /// <summary>
        /// Obsahuje true pokud cíl je nyní dostupný a je správného typu
        /// </summary>
        public bool IsAlive { get { return ((this._Wref?.IsAlive ?? false) ? (this._Wref.Target is T) : false); } }
        /// <summary>
        /// Cíl daného typu
        /// </summary>
        public T Target { get { return ((this.IsAlive) ? this._Wref.Target as T : null); } }
        /// <summary>
        /// Implicitní konverze z typového WeakTargetu na originální objekt daného typu
        /// </summary>
        /// <param name="target"></param>
        public static implicit operator T(WeakTarget<T> target) { return target?.Target; }
        /// <summary>
        /// Implicitní konverze z originálního objektu daného typu na typový WeakTarget
        /// </summary>
        /// <param name="source"></param>
        public static implicit operator WeakTarget<T>(T source) { return (source is null ? null : new WeakTarget<T>(source)); }
    }
    #endregion

    #region class TEventArgs<T> : Třída argumentů obsahující jeden prvek generického typu Item
    /// <summary>
    /// Třída argumentů obsahující jeden prvek <see cref="Item"/> generického typu <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Libovolný typ, třída na něj nemá žádné požadavky</typeparam>
    public class TEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public TEventArgs(T item) { Item = item; }
        /// <summary>
        /// Konkrétní datový prvek
        /// </summary>
        public T Item { get; private set; }
    }
    /// <summary>
    /// Třída argumentů obsahující jeden prvek <see cref="Item"/> generického typu <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Libovolný typ, třída na něj nemá žádné požadavky</typeparam>
    public class TEventCancelArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        public TEventCancelArgs(T item) { Item = item; Cancel = false; }
        /// <summary>
        /// Konkrétní datový prvek
        /// </summary>
        public T Item { get; private set; }
        /// <summary>
        /// Požadavek true na zrušení akce, default = false = akce proběhne (není zakázaná)
        /// </summary>
        public bool Cancel { get; set; }
    }
    #endregion
    #region class TEventValueChangeArgs<T> : Třída argumentů obsahující dva prvky generického typu OldValue a NewValue
    /// <summary>
    /// Třída argumentů obsahující dva prvky generického typu <typeparamref name="T"/> s charakterem 
    /// Původní hodnota <see cref="OldValue"/> a Nová hodnota <see cref="NewValue"/>.
    /// Novou hodnotu <see cref="NewValue"/> lze upravit (setovat), a zdroj eventu ji možná převezme.
    /// Lze nastavit <see cref="Cancel"/> = true, a zdroj eventu možná akci zruší.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TEventValueChangeArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="source">Zdroj události</param>
        /// <param name="oldValue">Původní hodnota</param>
        /// <param name="newValue">Nová hodnota</param>
        public TEventValueChangeArgs(EventSource source, T oldValue, T newValue) { Source = source; OldValue = oldValue; _NewValue = newValue; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Change from: {OldValue}, to: {NewValue}, source: {Source}";
        }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSource Source { get; private set; }
        /// <summary>
        /// Původní hodnota. Nelze změnit.
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Nová hodnota. 
        /// Hodnotu lze změnit, a zdroj eventu ji možná převezme.
        /// Vložením hodnoty dojde k nastavení <see cref="Changed"/> na true.
        /// </summary>
        public T NewValue { get { return _NewValue; } set { Changed = true; _NewValue = value; } }
        private T _NewValue;
        /// <summary>
        /// Zrušit událost? default = false, lze nastavit.
        /// </summary>
        public bool Cancel { get; set; } = false;
        /// <summary>
        /// Bude nastaveno na true poté, kdy aplikace vloží novou hodnotu do <see cref="NewValue"/>.
        /// A to bez ohledu na změnu hodnoty.
        /// </summary>
        public bool Changed { get; private set; } = false;
    }
    /// <summary>
    /// Zdroj eventu
    /// </summary>
    public enum EventSource
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Zásah kódu
        /// </summary>
        Code,
        /// <summary>
        /// Interaktivní akce uživatele
        /// </summary>
        User
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
        /// Titulkový label menší, typicky grupa
        /// </summary>
        SubTitle,
        /// <summary>
        /// Titulkový label větší, jeden na formuláři
        /// </summary>
        MainTitle,
        /// <summary>
        /// Dodatková informace
        /// </summary>
        Info
    }
    #endregion
    #region DxPanelControl
    /// <summary>
    /// PanelControl
    /// </summary>
    public class DxPanelControl : DevExpress.XtraEditors.PanelControl
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPanelControl()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
        }
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DxSplitContainerControl
    /// <summary>
    /// SplitContainerControl
    /// </summary>
    public class DxSplitContainerControl : DevExpress.XtraEditors.SplitContainerControl
    { }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// LabelControl
    /// </summary>
    public class DxLabelControl : DevExpress.XtraEditors.LabelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLabelControl()
        {
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
    }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// TextEdit
    /// </summary>
    public class DxTextEdit : DevExpress.XtraEditors.TextEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTextEdit()
        {
            EnterMoveNextControl = true;
        }
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxMemoEdit
    /// <summary>
    /// MemoEdit
    /// </summary>
    public class DxMemoEdit : DevExpress.XtraEditors.MemoEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxImageComboBoxEdit
    /// <summary>
    /// ImageComboBoxEdit
    /// </summary>
    public class DxImageComboBoxEdit : DevExpress.XtraEditors.ImageComboBoxEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxSpinEdit
    /// <summary>
    /// SpinEdit
    /// </summary>
    public class DxSpinEdit : DevExpress.XtraEditors.SpinEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxCheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    public class DxCheckEdit : DevExpress.XtraEditors.CheckEdit
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxListBoxControl
    /// <summary>
    /// ListBoxControl
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ListBoxControl
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            ReorderByDragEnabled = false;
            ReorderIconColor = Color.FromArgb(192, 116, 116, 96);
            ReorderIconColorHot = Color.FromArgb(220, 160, 160, 122);
        }
        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event PaintEventHandler PaintList;
        /// <summary>
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, object, Rectangle>[] VisibleItems
        {
            get
            {
                List<Tuple<int, object, Rectangle>> visibleItems = new List<Tuple<int, object, Rectangle>>();
                int topIndex = this.TopIndex;
                int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                int count = this.ItemCount;
                while (index < count)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    if (bounds.HasValue)
                        visibleItems.Add(new Tuple<int, object, Rectangle>(index, this.Items[index], bounds.Value));
                    else if (index > topIndex)
                        break;
                    index++;
                }
                return visibleItems.ToArray();
            }
        }
        /// <summary>
        /// Pole, obsahující informace o právě selectovaných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, object, Rectangle?>[] SelectedItemsInfo
        {
            get
            {
                List<Tuple<int, object, Rectangle?>> selectedItems = new List<Tuple<int, object, Rectangle?>>();
                foreach (var index in this.SelectedIndices)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    selectedItems.Add(new Tuple<int, object, Rectangle?>(index, this.Items[index], bounds));
                }
                return selectedItems.ToArray();
            }
        }

        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
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
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region Overrides
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintList?.Invoke(this, e);
            this.PaintOnMouseItem(e);
        }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        /// <summary>
        /// Po vstupu myši: určíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.DetectOnMouseItemAbsolute(Control.MousePosition);
        }
        /// <summary>
        /// Po pohybu myši: určíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.DetectOnMouseItem(e.Location);
        }
        /// <summary>
        /// Po odchodu myši: zrušíme myšoaktivní prvek listu
        /// </summary>
        /// <param name="e"></param>
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
            get 
            {
                if (_OnMouseItemIndex >= this.ItemCount)
                    _OnMouseItemIndex = -1;
                return _OnMouseItemIndex;
            }
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
        /// Index prvku, pro který je vypočtena souřadnice a uložena v <see cref="_OnMouseIconBounds"/>
        /// </summary>
        private int _OnMouseIconBoundsIndex = -1;
        /// <summary>
        /// Vrátí souřadnice prvku na daném indexu. Volitelně může provést kontrolu na to, zda daný prvek je ve viditelné oblasti Listu. Pokud prvek neexistuje nebo není vidět, vrací null.
        /// Vrácené souřadnice jsou relativní v prostoru this ListBoxu.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        public Rectangle? GetItemBounds(int itemIndex, bool onlyVisible = true)
        {
            if (itemIndex < 0 || itemIndex >= this.ItemCount) return null;

            Rectangle itemBounds = this.GetItemRectangle(itemIndex);
            if (onlyVisible)
            {   // Pokud chceme souřadnice pouze viditelného prvku, pak prověříme souřadnice prvku proti souřadnici prostoru v ListBoxu:
                Rectangle listBounds = this.ClientRectangle;
                if (itemBounds.Right <= listBounds.X || itemBounds.X >= listBounds.Right || itemBounds.Bottom <= listBounds.Y || itemBounds.Y >= listBounds.Bottom) 
                    return null;   // Prvek není vidět
            }

            return itemBounds;
        }
        /// <summary>
        /// Vrátí souřadnici prostoru pro myší ikonu
        /// </summary>
        /// <param name="onMouseItemIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetOnMouseIconBounds(int onMouseItemIndex)
        {
            Rectangle? itemBounds = this.GetItemBounds(onMouseItemIndex, true);
            if (!itemBounds.HasValue || itemBounds.Value.Width < 35) return null;        // Pokud prvek neexistuje, nebo není vidět, nebo je příliš úzký => vrátíme null

            int wb = 14;
            int x0 = itemBounds.Value.Right - wb - 6;
            int yc = itemBounds.Value.Y + itemBounds.Value.Height / 2;
            Rectangle iconBounds = new Rectangle(x0 - 1, itemBounds.Value.Y, wb + 1, itemBounds.Value.Height);
            return iconBounds;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
    }
    #endregion
    #region DxSimpleButton
    /// <summary>
    /// SimpleButton
    /// </summary>
    public class DxSimpleButton : DevExpress.XtraEditors.SimpleButton
    {
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public void SetToolTip(string title, string text) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text); }
        #endregion
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxImagePickerListBox
    /// <summary>
    /// ListBox nabízející DevExpress Resources
    /// </summary>
    public class DxImagePickerListBox : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxImagePickerListBox()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Initialize()
        {
            _ClipboardCopyIndex = 0;

            _FilterClearButton = DxComponent.CreateDxMiniButton(0, 0, 20, 20, this, _FilterClearButtonClick, 
                resourceName: "svgimages/spreadsheet/clearfilter.svg",
                toolTipTitle: "Zrušit filtr", toolTipText: "Zruší filtr, budou zobrazeny všechny dostupné zdroje.");
            _FilterClearButton.MouseEnter += _AnyControlEnter;
            _FilterClearButton.Enter += _AnyControlEnter;

            _FilterText = DxComponent.CreateDxTextEdit(27, 0, 200, this,
                toolTipTitle: "Filtr Resources", toolTipText: "Vepište část názvu zdroje.\r\nLze použít filtrační znaky * a ?.\r\nLze zadat víc filtrů, oddělených středníkem nebo čárkou.\r\n\r\nNapříklad: 'add' zobrazí všechny položky obsahující 'add',\r\n'*close*.svg' zobrazí něco obsahující 'close' s příponou '.svg',\r\n'*close*.svg;*delete*' zobrazí prvky close nebo delete");
            _FilterText.MouseEnter += _AnyControlEnter;
            _FilterText.Enter += _AnyControlEnter;
            _FilterText.KeyUp += _FilterText_KeyUp;

            _ListCopyButton = DxComponent.CreateDxMiniButton(230, 0, 20, 20, this, _ListCopyButtonClick, 
                resourceName: "svgimages/xaf/action_copy.svg", hotResourceName: "svgimages/xaf/action_modeldifferences_copy.svg",
                toolTipTitle: "Zkopírovat", toolTipText: "Označené řádky v seznamu zdrojů vloží do schránky, jako Ctrl+C.");
            _ListCopyButton.MouseEnter += _AnyControlEnter;
            _ListCopyButton.Enter += _AnyControlEnter;

            _ListBox = DxComponent.CreateDxListBox(DockStyle.None, parent: this, selectionMode: SelectionMode.MultiExtended, itemHeight: 32,
                toolTipTitle: "Seznam Resources", toolTipText: "Označte jeden nebo více řádků, klávesou Ctrl+C zkopírujete názvy Resources jako kód C#.");
            _ListBox.MouseEnter += _AnyControlEnter;
            _ListBox.Enter += _AnyControlEnter;
            _ListBox.KeyUp += _ListBox_KeyUp;
            _ListBox.PaintList += _ListBox_PaintList;
            _ListBox.SelectedIndexChanged += _ListBox_SelectedIndexChanged;

            _ResourceNames = DxComponent.GetResourceKeys();
            _ResourceFilter = "";
            _StatusText = "";

            _FillListByFilter();
        }
        /// <summary>
        /// Po vstupu do jakéhokoli controlu nastavím výchozí status text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyControlEnter(object sender, EventArgs e)
        {
            _ResetStatusText();
        }
        /// <summary>
        /// Po změně velikosti provedu rozmístění prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Správně rozmístí prvky
        /// </summary>
        protected void DoLayout()
        {
            var size = this.ClientSize;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;
            int sx = 2;
            int sy = 2;
            int r = size.Width - mx;
            int w = size.Width - mx - mx;
            int h = size.Height - my - my;
            int txs = _FilterText.Height;
            int bts = txs + sx;
            
            _FilterClearButton.Bounds = new Rectangle(x, y, txs, txs);
            _FilterText.Bounds = new Rectangle(x + bts, y, w - bts - bts, txs);
            _ListCopyButton.Bounds = new Rectangle(r - txs, y, txs, txs);

            y = _FilterText.Bounds.Bottom + sy;
            _ListBox.Bounds = new Rectangle(x, y, w, h - y);
        }
        /// <summary>
        /// Clear filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterClearButtonClick(object sender, EventArgs e) 
        {
            _ResourceFilter = "";
            _FillListByFilter();
            _FilterText.Focus();
        }
        /// <summary>
        /// FilterText: Po klávese ve filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _ListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
            { }
            else
                _FillListByFilter();
        }
        /// <summary>
        /// Copy selected items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListCopyButtonClick(object sender, EventArgs e) 
        {
            _FilterText.Focus();
            _DoCopyClipboard();
        }
        /// <summary>
        /// ListBox: Obsluha kláves: detekce Ctrl+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C)) _DoCopyClipboard();
        }
        /// <summary>
        /// Po změně řádku v ListBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            StatusText = "Označeny řádky: " + selectedItems.Length.ToString();
        }
        /// <summary>
        /// Vykreslí obrázky do ListBoxu do viditelných prvků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_PaintList(object sender, PaintEventArgs e)
        {
            DevExpress.Utils.Design.ISvgPaletteProvider svgPalette = DxComponent.GetSvgPalette();
            var visibleItems = _ListBox.VisibleItems;
            foreach (var visibleItem in visibleItems)
            {
                string resourceName = visibleItem.Item2 as string;
                Rectangle itemBounds = visibleItem.Item3;
                var image = DxComponent.GetImageFromResource(resourceName, out Size size, maxSize: new Size(32, 32), optimalSvgSize: new Size(32, 32), svgPalette: svgPalette);
                if (image != null)
                {
                    Point imagePoint = new Point((itemBounds.Right - 24 - size.Width / 2), itemBounds.Top + ((itemBounds.Height - size.Height) / 2));
                    Rectangle imageBounds = new Rectangle(imagePoint, size);
                    e.Graphics.DrawImage(image, imageBounds);
                }
            }
        }
        DxSimpleButton _FilterClearButton;
        DxTextEdit _FilterText;
        DxSimpleButton _ListCopyButton;
        DxListBoxControl _ListBox;
        #region Seznam resources - získání, filtrování, tvorba Image, CopyToClipboard
        /// <summary>
        /// Do seznamu ListBox vloží zdroje odpovídající aktuálnímu filtru
        /// </summary>
        private void _FillListByFilter()
        {
            string[] resources = _GetResourcesByFilter();
            _FilteredItemsCount = resources.Length;

            _ListBox.SuspendLayout();
            _ListBox.Items.Clear();
            _ListBox.Items.AddRange(resources);
            _ListBox.ResumeLayout(false);
            _ListBox.PerformLayout();

            _ResetStatusText();
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících aktuálnímu filtru
        /// </summary>
        /// <returns></returns>
        private string[] _GetResourcesByFilter()
        {
            if (_ResourceNames == null || _ResourceNames.Length == 0) return new string[0];

            var filter = _ResourceFilter;
            if (String.IsNullOrEmpty(filter)) return _ResourceNames;

            filter = RegexSupport.ReplaceGreenWildcards(filter);

            string[] result = _GetResourcesByFilter(filter);
            if (result.Length == 0)
                result = _GetResourcesByFilter("*" + filter + "*");

            return result;
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících danému filtru
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] _GetResourcesByFilter(string filter)
        {
            var regexes = RegexSupport.CreateWildcardsRegexes(filter);
            var result = RegexSupport.FilterByRegexes(_ResourceNames, regexes);
            return result.ToArray();
        }
        /// <summary>
        /// Vloží do Clipboardu kód obsahující aktuálně vybrané texty
        /// </summary>
        private void _DoCopyClipboard()
        {
            var selectedItems = _ListBox.SelectedItemsInfo;
            int rowCount = selectedItems.Length;
            if (rowCount > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var selectedItem in selectedItems)
                {
                    _ClipboardCopyIndex++;
                    string resourceName = selectedItem.Item2 as string;
                    sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");
                }
                if (sb.Length > 0)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(sb.ToString());
                }

                StatusText = "Položky zkopírovány do schránky: " + rowCount.ToString();
            }
            else
            {
                StatusText = $"Nejsou označeny žádné položky.";
            }
        }
        /// <summary>
        /// Filtrační text z textboxu
        /// </summary>
        private string _ResourceFilter { get { return this._FilterText.Text.Trim(); } set { this._FilterText.Text = (value ?? ""); } }
        /// <summary>
        /// Jména všech zdrojů
        /// </summary>
        private string[] _ResourceNames;
        /// <summary>
        /// Číslo pro číslování proměnných do Clipboardu
        /// </summary>
        private int _ClipboardCopyIndex;
        /// <summary>
        /// Počet aktuálně filtrovaných položek
        /// </summary>
        private int _FilteredItemsCount;
        #endregion
        #region Podpora pro StatusBar
        /// <summary>
        /// Text vhodný pro zobrazení ve statusbaru. Setování nové hodnoty vyvolá event <see cref="StatusTextChanged"/>
        /// </summary>
        public string StatusText
        {
            get { return _StatusText; }
            protected set
            {
                bool isChanged = !String.Equals(value, _StatusText);
                _StatusText = value;
                if (isChanged)
                    StatusTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _StatusText;
        private void _ResetStatusText()
        {
            int allItemsCount = _ResourceNames.Length;
            int filteredItemsCount = _FilteredItemsCount;
            bool hasFilter = !String.IsNullOrEmpty(_ResourceFilter);
            string filter = (hasFilter ? "'" + _ResourceFilter + "'" : "");
            if (allItemsCount == 0)
                StatusText = $"Neexistují žádné položky";
            else if (!hasFilter)
                StatusText = $"Zobrazeny všechny položky: {allItemsCount}";
            else if (filteredItemsCount == 0)
                StatusText = $"Zadanému filtru {filter} nevyhovuje žádná položka";
            else if (filteredItemsCount == allItemsCount)
                StatusText = $"Zadanému filtru {filter} vyhovují všechny položky: {filteredItemsCount}";
            else
                StatusText = $"Zadanému filtru {filter} vyhovují zobrazené položky: {filteredItemsCount}";
        }
        /// <summary>
        /// Událost vyvolaná po změně textu v <see cref="StatusText"/>
        /// </summary>
        public event EventHandler StatusTextChanged;
        #endregion
    }
    #endregion
    #region DxChartControl
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.ChartControl"/> pro použití v ASOL.Nephrite
    /// </summary>
    internal class DxChartControl : DevExpress.XtraCharts.ChartControl
    {
        #region Support pro práci s grafem
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxChartControl()
        {
            this.HardwareAcceleration = true;
        }
        /// <summary>
        /// XML definice vzhledu grafu (Layout), aktuálně načtený z komponenty
        /// </summary>
        public string ChartXmlLayout
        {
            get { return GetGuiValue<string>(() => this._GetChartXmlLayout()); }
            set { SetGuiValue<string>(v => this._SetChartXmlLayout(v), value); }
        }
        /// <summary>
        /// Reference na data zobrazená v grafu
        /// </summary>
        public object ChartDataSource
        {
            get { return GetGuiValue<object>(() => this._GetChartDataSource()); }
            set { SetGuiValue<object>(v => this._SetChartDataSource(v), value); }
        }
        /// <summary>
        /// Vloží do grafu layout, a současně data, v jednom kroku
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        public void SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda DevExpress
        /// </summary>
        public void ShowChartWizard()
        {
            if (!IsChartWorking)
                throw new InvalidOperationException($"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            bool valid = DxChartDesigner.DesignChart(this, "Upravte graf...", true, false);

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            if (valid)
            {
                string newLayout = _GetLayoutFromControl();
                _ValidChartXmlLayout = newLayout;
                OnChartXmlLayoutEdited();
            }
            else
            {
                _RestoreChartXmlLayout();
            }
        }
        /// <summary>
        /// Je voláno po editaci layoutu, vyvolá event <see cref="ChartXmlLayoutEdited"/>
        /// </summary>
        protected virtual void OnChartXmlLayoutEdited()
        {
            ChartXmlLayoutEdited?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost, kdy uživatel změnil data (vyvoláno metodou <see cref="ShowChartWizard"/>), a v designeru uložil změnu dat.
        /// V tuto chvíli je již nový layout k dispozici v <see cref="ChartXmlLayout"/>.
        /// </summary>
        public event EventHandler ChartXmlLayoutEdited;
        #endregion
        #region private práce s layoutem a daty
        /// <summary>
        /// Vloží do grafu layout, ponechává data
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        private void _SetChartXmlLayout(string xmlLayout)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            var dataSource = _ChartDataSource;             // Načteme z WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí XML layout načtený přímo z grafu
        /// </summary>
        /// <returns></returns>
        private string _GetChartXmlLayout()
        {
            return _GetLayoutFromControl();
        }
        /// <summary>
        /// Vloží do grafu dříve platný layout (uložený v metodě <see cref="_SetChartXmlLayout(string)"/>), ponechává data.
        /// </summary>
        private void _RestoreChartXmlLayout()
        {
            var dataSource = _ChartDataSource;             // Tabulka z WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vloží do grafu data, ponechává layout (může dojít k chybě)
        /// </summary>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        private void _SetChartDataSource(object dataSource)
        {
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí data grafu
        /// </summary>
        /// <returns></returns>
        private object _GetChartDataSource()
        {
            return _ChartDataSource;
        }
        /// <summary>
        /// Do grafu korektně vloží data i layout, ve správném pořadí.
        /// Pozor: tato metoda neukládá dodané objekty do lokálních proměnných <see cref="_ValidChartXmlLayout"/> a <see cref="_ChartDataSource"/>, pouze do controlu grafu!
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="xmlLayout"></param>
        private void _SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            // Tato sekvence je důležitá, jinak dojde ke zbytečným chybám:
            try
            {
                this._SetLayoutToControl(xmlLayout);
            }
            finally
            {   // Datový zdroj do this uložím i po chybě, abych mohl i po vložení chybného layoutu otevřít editor:
                this.DataSource = dataSource;
            }
        }
        /// <summary>
        /// Fyzicky načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayoutFromControl()
        {
            string layout = null;
            if (IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    this.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Neřeší try catch, to má řešit volající včetně ošetření chyby.
        /// POZOR: tato metoda odpojí datový zdroj, proto je třeba po jejím skončení znovu vložit zdroj do _ChartControl.DataSource !
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControl(string layout)
        {
            if (IsChartValid)
            {
                try
                {
                    this.BeginInit();
                    this.Reset();
                    _SetLayoutToControlDirect(layout);
                }
                finally
                {
                    this.EndInit();
                }
            }
        }
        /// <summary>
        /// Vynuluje layout
        /// </summary>
        public void Reset()
        {
            this.DataSource = null;
            string emptyLayout = @"<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.6.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
  </Chart>
</ChartXmlSerializer>";
            emptyLayout = emptyLayout.Replace("'", "\"");
            _SetLayoutToControlDirect(emptyLayout);

            this.ResetLegendTextPattern();
                    this.Series.Clear();
                    this.Legends.Clear();
                    this.Titles.Clear();
            this.AutoLayout = false;
            this.CalculatedFields.Clear();
            this.ClearCache();
            this.Diagram = null;
                }
        /// <summary>
        /// Fyzicky vloží daný string do layoutu grafu
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControlDirect(string layout)
        {
            byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
            if (buffer.Length > 0)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                    this.LoadFromStream(ms);     // Pozor, zahodí data !!!
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) 
        /// </summary>
        public bool IsChartValid { get { return (!this.IsDisposed); } }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) a obsahuje data (má datový zdroj)
        /// </summary>
        public bool IsChartWorking { get { return (IsChartValid && this.DataSource != null); } }
        /// <summary>
        /// Offline uložený layout grafu, který byl setovaný zvenku; používá se při refreshi dat pro nové vložení stávajícího layoutu do grafu. 
        /// Při public čtení layoutu se nevrací tento string, ale fyzicky se načte aktuálně použitý layout z grafu.
        /// </summary>
        private string _ValidChartXmlLayout;
        /// <summary>
        /// Reference na tabulku s daty grafu, nebo null
        /// </summary>
        private object _ChartDataSource
        {
            get
            {
                var wr = _ChartDataSourceWR;
                return ((wr != null && wr.TryGetTarget(out var table)) ? table : null);
            }
            set
            {
                _ChartDataSourceWR = ((value != null) ? new WeakReference<object>(value) : null);
            }
        }
        /// <summary>
        /// WeakReference na tabulku s daty grafu
        /// </summary>
        private WeakReference<object> _ChartDataSourceWR;
        #endregion
        #region ASOL standardní rozšíření
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu
        /// </summary>
        /// <param name="action"></param>
        protected void RunInGui(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected T GetGuiValue<T>(Func<T> reader)
        {
            if (this.InvokeRequired)
                return (T)this.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected void SetGuiValue<T>(Action<T> writer, T value)
        {
            if (this.InvokeRequired)
                this.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
    }
    #endregion
    #region DxChartDesigner
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.Designer.ChartDesigner"/> pro editaci definice grafu
    /// </summary>
    internal class DxChartDesigner : DevExpress.XtraCharts.Designer.ChartDesigner, Noris.Clients.Win.Components.IEscapeHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        public DxChartDesigner(object chart) : base(chart) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="designerHost"></param>
        public DxChartDesigner(object chart, System.ComponentModel.Design.IDesignerHost designerHost) : base(chart, designerHost) { }
        /// <summary>
        /// Zobrazí designer pro daný graf, a vrátí true = OK
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="caption"></param>
        /// <param name="showActualData"></param>
        /// <param name="topMost"></param>
        /// <returns></returns>
        public static bool DesignChart(object chart, string caption, bool showActualData, bool topMost)
        {
            DxChartDesigner chartDesigner = new DxChartDesigner(chart);
            chartDesigner.Caption = caption;
            chartDesigner.ShowActualData = showActualData;
            var result = chartDesigner.ShowDialog(topMost);
            return (result == DialogResult.OK);
        }
        /// <summary>
        /// Desktop okno hlídá klávesu Escape: 
        /// c:\inetpub\wwwroot\Noris46\Noris\ClientImages\ClientWinForms\WinForms.Host\Windows\WDesktop.cs
        /// metoda escapeKeyFilter_EscapeKeyDown()
        /// Když dostane Escape, najde OnTop okno které implementuje IEscapeHandler, a zavolá zdejší metodu.
        /// My vrátíme true = OK, aby desktop dál ten Escape neřešil, a my se v klidu zavřeme nativním zpracováním klávesy Escape ve WinFormu.
        /// </summary>
        /// <returns></returns>
        bool Noris.Clients.Win.Components.IEscapeHandler.HandleEscapeKey()
        {
            return true;
        }
    }
    #endregion
}
