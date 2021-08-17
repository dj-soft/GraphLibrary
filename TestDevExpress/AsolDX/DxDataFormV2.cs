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

            InitializeContent();

            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis" };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus" };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba" };
            _ContentPanel.Controls.Add(_CheckBox);



            _Items = new List<DxDataFormItemV2>();

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 2") { DesignBounds = new Rectangle(20, 82, 70, 18) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 3") { DesignBounds = new Rectangle(20, 112, 70, 18) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 4") { DesignBounds = new Rectangle(20, 142, 70, 18) });

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 2") { DesignBounds = new Rectangle(100, 80, 80, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 3") { DesignBounds = new Rectangle(100, 110, 80, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 4") { DesignBounds = new Rectangle(100, 140, 80, 20) });

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 2") { DesignBounds = new Rectangle(210, 80, 100, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 3") { DesignBounds = new Rectangle(210, 110, 100, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 4") { DesignBounds = new Rectangle(210, 140, 100, 20) });
        }
        public int ItemsCount { get { return _Items.Count; } }
        public DxDataFormItemV2[] Items { get { return _Items.ToArray(); } }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;
        private List<DxDataFormItemV2> _Items;
        protected override void OnSizeChanged(EventArgs e)
        {
            DoLayoutContent();
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
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
        void IDxDataFormV2.OnPaintContent(PaintEventArgs e)
        {
            if (_PaintingItems) return;
            int count = (_PaintingPerformaceTestCount > 1 ? _PaintingPerformaceTestCount : 1);
            bool forceRefresh = _PaintingPerformaceForceRefresh;
            try
            {
                _PaintingItems = true;
                if (count == 1 && !forceRefresh)
                {   // Standard:
                    _Items.ForEachExec(i => i.OnPaint(e));
                }
                else
                {   // Performance test:
                    int x = 0;
                    int y = 0;
                    while (count > 0)
                    {
                        Point offset = new Point(x, y);
                        _Items.ForEachExec(i => i.OnPaint(e, forceRefresh, offset));
                        y += 12;
                        if (y > 500)
                        {
                            y = 0;
                            x += 36;
                            if (x > 1200)
                                x = 0;
                        }
                        count--;
                    }
                }
            }
            finally
            {
                _PaintingItems = false;
                _PaintingPerformaceTestCount = 1;
                _PaintingPerformaceForceRefresh = false;
            }
        }
        private bool _PaintingItems = false;
        private int _PaintingPerformaceTestCount;
        private bool _PaintingPerformaceForceRefresh;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.None)
            {
                var cursor = Cursors.Default;
                if (_Items.TryGetFirst(i => i.IsActivePoint(e.Location), out var found) && found.DefaultCursor != null)
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
            this.DoLayoutContent();
            _Items.ForEachExec(i => i.Invalidate());
        }
        protected void DxAfterDpiChanged()
        {
            _Items.ForEachExec(i => i.Invalidate());
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

                if (control != null)
                {
                    control.Location = new Point(5, 5);
                    control.Visible = false;
                    _ContentPanel.Controls.Add(control);
                }
                modeControls.Add(mode, control);
            }
            return control;
        }
        private Dictionary<DataFormItemType, Dictionary<DxDataFormControlMode, Control>> _DataFormControls;

        public void TestPerformance(int count, bool forceRefresh)
        {
            _PaintingPerformaceTestCount = count;
            _PaintingPerformaceForceRefresh = forceRefresh;
            this._ContentPanel.Invalidate();
            Application.DoEvents();
        }

        #region ContentPanel, ScrollBars a velikost obsahu

        /// <summary>
        /// Velikost dat obsažených v tomto containeru, má vliv na Scrollbary a posouvání
        /// </summary>
        public Size ContentSize { get { return _ContentSize; } set { _ContentSize = value; DoLayoutContent();  } }
        private Size _ContentSize;
        private void InitializeContent()
        {
            _ContentPanel = new DxDataFormContentV2(this) { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            _ContentPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            this.Controls.Add(_ContentPanel);
            _VScrollBar = new DevExpress.XtraEditors.VScrollBar() { Visible = false };
            this.Controls.Add(_VScrollBar);
            _HScrollBar = new DevExpress.XtraEditors.HScrollBar() { Visible = false };
            this.Controls.Add(_HScrollBar);
            
            DoLayoutContent();
        }

        private void DoLayoutContent()
        {
            Size contentSize = this.ContentSize;
            Size clientSize = this.ClientSize;
            int clientWidth = clientSize.Width;
            int clientHeight = clientSize.Height;

            // Vertikální (svislý) ScrollBar: bude viditelný, když výška obsahu je větší než výška klienta, a zmenší šířku klienta:
            bool vVisible = (contentSize.Height > clientHeight);
            int vScrollSize = (vVisible ? _VScrollBar.GetDefaultVerticalScrollBarWidth() : 0);
            if (vVisible) clientWidth -= vScrollSize;

            // Horizontální (vodorovný) ScrollBar: bude viditelný, když šířka obsahu je větší než šířka klienta, a zmenší výšku klienta:
            bool hVisible = (contentSize.Width > clientWidth);
            int hScrollSize = (hVisible ? _VScrollBar.GetDefaultHorizontalScrollBarHeight() : 0);
            if (hVisible) clientHeight -= hScrollSize;

            // Pokud dosud nebyl viditelný Vertikální (svislý) ScrollBar, ale je viditelný Horizontální (vodorovný) ScrollBar:
            //  pak Horizontální ScrollBar zmenšil výšku obsahu (clientHeight), a může se stát, že bude třeba zobrazit i Vertikální ScrollBar:
            if (!vVisible && hVisible && (contentSize.Height > clientHeight))
            {
                vVisible = true;
                vScrollSize = _VScrollBar.GetDefaultVerticalScrollBarWidth();
                clientWidth -= vScrollSize;
            }

            // Pokud je přílš malá šířka a je viditelný Vertikální (svislý) ScrollBar: vrátit plnou šířku a zrušit scrollBar:
            if (clientWidth < 10 && vVisible)
            {
                clientWidth = clientSize.Width;
                vVisible = false;
                vScrollSize = 0;
            }
            // Pokud je přílš malá výška a je viditelný Horizontální (vodorovný) ScrollBar: vrátit plnou výšku a zrušit scrollBar:
            if (clientHeight < 10 && hVisible)
            {
                clientHeight = clientSize.Height;
                hVisible = false;
                hScrollSize = 0;
            }

            _ContentPanel.SetBounds(new Rectangle(0, 0, clientWidth, clientHeight));
            if (vVisible) _VScrollBar.SetBounds(new Rectangle(clientWidth, 0, vScrollSize, clientHeight));
            if (hVisible) _HScrollBar.SetBounds(new Rectangle(0, clientHeight, clientWidth, hScrollSize));

            if (_VScrollBar.IsSetVisible() != vVisible) _VScrollBar.Visible = vVisible;
            if (_HScrollBar.IsSetVisible() != hVisible) _HScrollBar.Visible = hVisible;
        }
        private DxDataFormContentV2 _ContentPanel;
        private DevExpress.XtraEditors.VScrollBar _VScrollBar;
        private DevExpress.XtraEditors.HScrollBar _HScrollBar;
        #endregion
    }
    public interface IDxDataFormV2
    {
        int DeviceDpi { get; }
        Control GetControl(DataFormItemType itemType, DxDataFormControlMode mode);
        void OnPaintContent(PaintEventArgs e);
    }
    public enum DxDataFormControlMode
    {
        None,
        Inactive,
        HotMouse,
        Focused
    }
    public class DxDataFormContentV2 : DxPanelControl
    {
        public DxDataFormContentV2(IDxDataFormV2 owner)
        {
            _Owner = owner;
        }
        private IDxDataFormV2 _Owner;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _Owner?.OnPaintContent(e);
        }

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
        public void OnPaint(PaintEventArgs e, bool forceRefresh = false, Point? offset = null)
        {
            var bounds = this.CurrentBounds;
            bool withOffset = (offset.HasValue && !offset.Value.IsEmpty);
            if (_Image == null || forceRefresh)
            {
                var source = _Owner.GetControl(this._ItemType, DxDataFormControlMode.Inactive);
                if (source == null) return;

                Cursor cursor = null;
                source.SetBounds(bounds);                  // Nastavím správné umístění, to kvůli obrázkům na pozadí panelu (různé skiny!), aby obrázky odpovídaly aktuální pozici...
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
                if (!withOffset)
                {
                    DefaultCursor = cursor ?? Cursors.Default;
                    ActiveBounds = bounds.Sub(source.Margin);
                }
            }
            Point location = bounds.Location;
            if (withOffset) location = location.Add(offset.Value);
            e.Graphics.DrawImage(_Image, location);
        }
        public bool IsActivePoint(Point point)
        {
            return (ActiveBounds.Contains(point));
        }
    }
}
