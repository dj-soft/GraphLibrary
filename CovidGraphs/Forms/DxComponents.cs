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
        #endregion
        #region Factory metody
        public static DxPanelControl CreateDxePanel(Control parent = null, 
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
        public static DxLabelControl CreateDxeLabel(int x, ref int y, int w, Control parent, string text,
            LabelStyleType? styleType = null, bool? visible = null, bool shiftY = false)
        {
            var inst = Instance;

            var label = new DxLabelControl() { Bounds = new Rectangle(x, y, w, inst._DetailYHeightLabel), Text = text };
            label.StyleController = (styleType == LabelStyleType.Title ? inst._TitleStyle : (styleType == LabelStyleType.Info ? inst._InfoStyle : inst._LabelStyle));
            if (visible.HasValue) label.Visible = visible.Value;
            if (parent != null) parent.Controls.Add(label);
            if (shiftY) y = y + label.Height + inst._DetailYSpaceLabel;
            return label;
        }
        public static DxTextEdit CreateDxeTextEdit(int x, ref int y, int w, Control parent, EventHandler textChanged = null,
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
        public static DxMemoEdit CreateDxeMemoEdit(int x, ref int y, int w, int h, Control parent, EventHandler textChanged = null,
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
        public static DxImageComboBoxEdit CreateDxeImageComboBox(int x, ref int y, int w, Control parent, EventHandler selectedIndexChanged = null, string itemsTabbed = null,
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
        public static DxSpinEdit CreateDxeSpinEdit(int x, ref int y, int w, Control parent, EventHandler valueChanged = null,
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
        public static DxCheckEdit CreateDxeCheckEdit(int x, ref int y, int w, Control parent, string text, EventHandler checkedChanged = null,
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


        public static DxListBoxControl CreateDxeListBox()
        {
            return new DxListBoxControl();
        }
        #endregion
        /// <summary>
        /// Styl použitý pro Label
        /// </summary>
        public enum LabelStyleType { Default, Title, Info }
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
            ReorderByDragEnabled = true;
            ReorderIconColor = Color.FromArgb(192, 116, 116, 96);
            ReorderIconColorHot = Color.FromArgb(220, 160, 160, 122);
        }


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
        /// Zajistí vykreslení ikony pro ReorderByDrag a případně i přemisťovaného prvku
        /// </summary>
        /// <param name="e"></param>
        private void PaintOnMouseItem(PaintEventArgs e)
        {
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
                this.Cursor = (isCursorActive ? Cursors.SizeNS : Cursors.Default);
                _IsMouseOverReorderIcon = isCursorActive;
                this.Invalidate();                         // Překreslení => jiná barva ikony
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
}
