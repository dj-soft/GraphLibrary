using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    public class DxDataFormV2 : DxPanelControl, IDxDataFormV2
    {
        public DxDataFormV2()
        {
            this.DoubleBuffered = true;

            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis" };
            this.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus" };
            this.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba" };
            this.Controls.Add(_CheckBox);

            _VScrollBar = new DevExpress.XtraEditors.VScrollBar();
            this.Controls.Add(_VScrollBar);
            _HScrollBar = new DevExpress.XtraEditors.HScrollBar();
            this.Controls.Add(_HScrollBar);
            DoLayoutScrollBars();


            Items = new List<DxDataFormItemV2>();

            Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 2") { DesignBounds = new Rectangle(20, 82, 70, 18) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 3") { DesignBounds = new Rectangle(20, 112, 70, 18) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 4") { DesignBounds = new Rectangle(20, 142, 70, 18) });

            Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 2") { DesignBounds = new Rectangle(100, 80, 80, 20) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 3") { DesignBounds = new Rectangle(100, 110, 80, 20) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 4") { DesignBounds = new Rectangle(100, 140, 80, 20) });

            Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 2") { DesignBounds = new Rectangle(210, 80, 100, 20) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 3") { DesignBounds = new Rectangle(210, 110, 100, 20) });
            Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 4") { DesignBounds = new Rectangle(210, 140, 100, 20) });
        }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;
        private DevExpress.XtraEditors.VScrollBar _VScrollBar;
        private DevExpress.XtraEditors.HScrollBar _HScrollBar;
        private List<DxDataFormItemV2> Items;
        protected override void OnSizeChanged(EventArgs e)
        {
            DoLayoutScrollBars();
            base.OnSizeChanged(e);
        }
        protected override void OnDpiChangedBeforeParent(EventArgs e)
        {
            base.OnDpiChangedBeforeParent(e);
        }
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            this.DxAfterDpiChanged();
        }
        private void DoLayoutScrollBars()
        {
            bool vVisible = true;
            bool hVisible = true;

            if (vVisible || hVisible)
            {
                Size clientSize = this.ClientSize;
                int vSize = vVisible ? _VScrollBar.GetDefaultVerticalScrollBarWidth() : 0;
                int hSize = hVisible ? _HScrollBar.GetDefaultHorizontalScrollBarHeight() : 0;
                if (vSize > 0) _VScrollBar.Bounds = new Rectangle(clientSize.Width - vSize, 0, vSize, clientSize.Height - hSize);
                if (hSize > 0) _HScrollBar.Bounds = new Rectangle(0, clientSize.Height - hSize, clientSize.Width - vSize, hSize);
            }
            if (_VScrollBar.IsSetVisible() != vVisible) _VScrollBar.Visible = vVisible;
            if (_HScrollBar.IsSetVisible() != hVisible) _HScrollBar.Visible = hVisible;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_PaintingItems)
            {
                try
                {
                    _PaintingItems = true;
                    Items.ForEachExec(i => i.OnPaint(e));
                }
                finally
                {
                    _PaintingItems = false;
                }
            }
        }
        private bool _PaintingItems = false;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.None)
            {
                var cursor = Cursors.Default;
                if (Items.TryGetFirst(i => i.IsActivePoint(e.Location), out var found) && found.DefaultCursor != null)
                    cursor = found.DefaultCursor;
                this.Cursor = cursor;
            }
        }
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            DxAfterStyleChanged();
            this.Invalidate();
        }


        protected void DxAfterStyleChanged()
        {
            this.DoLayoutScrollBars();
            Items.ForEachExec(i => i.Invalidate());
        }
        protected void DxAfterDpiChanged()
        {
            Items.ForEachExec(i => i.Invalidate());
        }
        int IDxDataFormV2.DeviceDpi { get { return this.DeviceDpi; } }
        Control IDxDataFormV2.GetControl(DataFormItemType itemType, DxDataFormControlMode mode)
        {
            if (_DataFormControls == null) _DataFormControls = new Dictionary<DataFormItemType, Dictionary<DxDataFormControlMode, Control>>();
            var dataFormControls = _DataFormControls;
            Dictionary<DxDataFormControlMode, Control> modeControls;
            if (!dataFormControls.TryGetValue(itemType, out modeControls))
            {
                modeControls = new Dictionary<DxDataFormControlMode, Control>();
                dataFormControls.Add(itemType, modeControls);
            }
            Control control;
            if (!modeControls.TryGetValue(mode, out control))
            {
                control = (itemType == DataFormItemType.Label ? (Control)new DxLabelControl() :
                          (itemType == DataFormItemType.TextBox ? (Control)new DxTextEdit() :
                          (itemType == DataFormItemType.CheckBox ? (Control)new DxCheckEdit() : (Control)null)));

                modeControls.Add(mode, control);
                control.Visible = false;
                this.Controls.Add(control);
            }
            return control;
        }
        private Dictionary<DataFormItemType, Dictionary<DxDataFormControlMode, Control>> _DataFormControls;
    }
    public interface IDxDataFormV2
    {
        int DeviceDpi { get; }
        Control GetControl(DataFormItemType itemType, DxDataFormControlMode mode);
    }
    public enum DxDataFormControlMode
    {
        None,
        Inactive,
        HotMouse,
        Focused
    }
    public class DxDataFormItemV2
    {
        public DxDataFormItemV2(IDxDataFormV2 owner, DataFormItemType itemType, string text)
        {
            _Owner = owner;
            _ItemType = itemType;
            _Text = text;
        }

        private IDxDataFormV2 _Owner;
        private DataFormItemType _ItemType;
        private string _Text;
        private Bitmap _Image;

        public Rectangle DesignBounds 
        { 
            get { return __DesignBounds; } 
            set 
            {
                var currentDpi = _Owner.DeviceDpi;
                __DesignBounds = value;
                __DesignDpi = currentDpi;
                __CurrentBounds = value;
                __CurrentDpi = currentDpi;
                _Image = null; 
            }
        }
        private Rectangle __DesignBounds;
        private int __DesignDpi;
        public Rectangle CurrentBounds { get { this.CheckDesignBounds(); return __CurrentBounds.Value; } }
        private Rectangle? __CurrentBounds;
        private int __CurrentDpi;
        public Rectangle ActiveBounds { get; set; }
        public Cursor DefaultCursor { get; set; }

        private void CheckDesignBounds()
        {
            var ownerDpi = _Owner.DeviceDpi;
            var currentDpi = __CurrentDpi;
            if (__CurrentBounds.HasValue && currentDpi == ownerDpi) return;
            var designDpi = __DesignDpi;
            __CurrentBounds = __DesignBounds.ConvertToDpi(designDpi, ownerDpi);
            __CurrentDpi = ownerDpi;
            _Image = null;
        }
        public void Invalidate()
        {
            _Image = null;
        }
        public void OnPaint(PaintEventArgs e)
        {
            var bounds = this.CurrentBounds;
            if (_Image == null)
            {
                var source = _Owner.GetControl(this._ItemType, DxDataFormControlMode.Inactive);
                if (source == null) return;

                Cursor cursor = null;
                if (source.Size != bounds.Size)
                    source.Size = bounds.Size;
                source.Text = this._Text;

                var size = source.Size;
                if (size != bounds.Size)
                {
                    bounds = new Rectangle(bounds.Location, size);
                    this.__CurrentBounds = bounds;
                }
                int w = size.Width;
                int h = size.Height;
                Bitmap image = new Bitmap(w, h);
                if (source is DxTextEdit textEdit)
                {
                    textEdit.DeselectAll();
                    textEdit.SelectionStart = 0;
                    cursor = Cursors.IBeam;
                }
                source.DrawToBitmap(image, new Rectangle(0, 0, w, h));
                _Image = image;
                DefaultCursor = cursor ?? Cursors.Default;
                ActiveBounds = CurrentBounds.Sub(source.Margin);
            }
            e.Graphics.DrawImage(_Image, CurrentBounds.Location);
        }
        public bool IsActivePoint(Point point)
        {
            return (ActiveBounds.Contains(point));
        }
    }
}
