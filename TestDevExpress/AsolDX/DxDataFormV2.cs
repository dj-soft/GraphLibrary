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
    public class DxDataFormV2 : DevExpress.XtraGrid.GridControl
    {
        public DxDataFormV2()
        {
            System.Data.DataTable employeesBindingSource = new System.Data.DataTable();
            employeesBindingSource.Columns.Add("FirstName", typeof(string));
            employeesBindingSource.Columns.Add("LastName", typeof(string));
            employeesBindingSource.Columns.Add("Address", typeof(string));
            employeesBindingSource.Columns.Add("City", typeof(string));
            employeesBindingSource.Columns.Add("Country", typeof(string));
            employeesBindingSource.Columns.Add("Photo", typeof(byte[]));
            employeesBindingSource.Rows.Add("Jan", "Novotný", "Nádražní 46", "Kolín", "CZE", null);
            employeesBindingSource.Rows.Add("Markéta", "Králová", "Pobřežní 1A", "Chrudim", "CZE", null);
            employeesBindingSource.Rows.Add("Karel", "Starý", "Severní 15", "Přelouč", "CZE", null);
            employeesBindingSource.Rows.Add("Lucie", "Vodičková", "Točitá 8", "Skuteč", "CZE", null);


            DevExpress.XtraGrid.Views.Layout.LayoutView lView = new DevExpress.XtraGrid.Views.Layout.LayoutView(this);
            this.MainView = lView;
            lView.OptionsBehavior.AutoPopulateColumns = false;
            lView.OptionsHeaderPanel.ShowCarouselModeButton = false;

            // this.Dock = DockStyle.Fill;

            this.DataSource = employeesBindingSource;

            // Create columns.
            var colFirstName = lView.Columns.AddVisible("FirstName") as DevExpress.XtraGrid.Columns.LayoutViewColumn;
            var colLastName = lView.Columns.AddVisible("LastName") as DevExpress.XtraGrid.Columns.LayoutViewColumn;
            var colAddress = lView.Columns.AddVisible("Address") as DevExpress.XtraGrid.Columns.LayoutViewColumn;
            var colCity = lView.Columns.AddVisible("City") as DevExpress.XtraGrid.Columns.LayoutViewColumn;
            var colCountry = lView.Columns.AddVisible("Country") as DevExpress.XtraGrid.Columns.LayoutViewColumn;
            var colPhoto = lView.Columns.AddVisible("Photo") as DevExpress.XtraGrid.Columns.LayoutViewColumn;

            // Access corresponding card fields.
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldFirstName = colFirstName.LayoutViewField;
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldLastName = colLastName.LayoutViewField;
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldAddress = colAddress.LayoutViewField;
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldCity = colCity.LayoutViewField;
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldCountry = colCountry.LayoutViewField;
            DevExpress.XtraGrid.Views.Layout.LayoutViewField fieldPhoto = colPhoto.LayoutViewField;

            // Position the FirstName field to the right of the Photo field.
            fieldFirstName.Move(new DevExpress.XtraLayout.Customization.LayoutItemDragController(fieldFirstName, fieldPhoto,
                DevExpress.XtraLayout.Utils.InsertLocation.After, DevExpress.XtraLayout.Utils.LayoutType.Horizontal));

            // Position the LastName field below the FirstName field.
            fieldLastName.Move(new DevExpress.XtraLayout.Customization.LayoutItemDragController(fieldLastName, fieldFirstName,
                DevExpress.XtraLayout.Utils.InsertLocation.After, DevExpress.XtraLayout.Utils.LayoutType.Vertical));

            // Create an Address Info group.
            var groupAddress = new DevExpress.XtraLayout.LayoutControlGroup();
            groupAddress.Text = "Address Info";
            groupAddress.Name = "addressInfoGroup";

            // Move the Address, City and Country fields to this group.
            groupAddress.AddItem(fieldAddress);
            fieldCity.Move(fieldAddress, DevExpress.XtraLayout.Utils.InsertType.Bottom);
            fieldCountry.Move(fieldCity, DevExpress.XtraLayout.Utils.InsertType.Bottom);

            lView.TemplateCard.AddGroup(groupAddress, fieldLastName, DevExpress.XtraLayout.Utils.InsertType.Bottom);

            // Assign editors to card fields.
            var riPictureEdit = this.RepositoryItems.Add("PictureEdit") as DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit;
            riPictureEdit.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            colPhoto.ColumnEdit = riPictureEdit;

            // Customize card field options.
            colFirstName.Caption = "First Name";
            colLastName.Caption = "Last Name";
            // Set the card's minimum size.
            lView.CardMinSize = new Size(250, 180);

            fieldPhoto.TextVisible = false;
            fieldPhoto.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            fieldPhoto.MaxSize = fieldPhoto.MinSize = new Size(150, 150);

        }

        public int ItemsCount;
        public void TestPerformance(int count, bool forceRefresh) { }
        public Size ContentSize;
    }
    public class DxDataFormV2yyy : DxScrollableContent, IDxDataFormV2
    {
        public DxDataFormV2yyy()
        {
            this.DoubleBuffered = true;

            InitializeContent();

            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis" };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 300, 20), Text = "Pokus" };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(410, 50, 100, 20), Text = "Předvolba" };
            _ContentPanel.Controls.Add(_CheckBox);



            _Items = new List<DxDataFormItemV2>();

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 2") { DesignBounds = new Rectangle(20, 82, 70, 18) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 3") { DesignBounds = new Rectangle(20, 112, 70, 18) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Popisek 4") { DesignBounds = new Rectangle(20, 142, 70, 18) });

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 2") { DesignBounds = new Rectangle(100, 80, 80, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 3") { DesignBounds = new Rectangle(100, 110, 80, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 4") { DesignBounds = new Rectangle(100, 140, 80, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 5") { DesignBounds = new Rectangle(100, 170, 80, 20) });

            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 2") { DesignBounds = new Rectangle(210, 80, 100, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 3") { DesignBounds = new Rectangle(210, 110, 100, 20) });
            _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Předvolba 4") { DesignBounds = new Rectangle(210, 140, 100, 20) });

            this.ContentTotalSize = new Size(1100, 650);
        }
        public int ItemsCount { get { return _Items.Count; } }
        public DxDataFormItemV2[] Items { get { return _Items.ToArray(); } }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;
        private List<DxDataFormItemV2> _Items;
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            this.InvalidateItems();
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
        protected override void OnContentVirtualBoundsChanged()
        {
            base.OnContentVirtualBoundsChanged();
            this.TextBox.Text = this.ContentVirtualBounds.ToString();
            this.InvalidateItems();
        }

        protected void InvalidateItems()
        {
            _Items.ForEachExec(i => i.Invalidate());
            this._ContentPanel.Invalidate();
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
        public Size ContentSize { get { return _ContentSize; } set { _ContentSize = value; DoLayoutContent(); } }
        private Size _ContentSize;
        private void InitializeContent()
        {
            _ContentPanel = new DxDataFormContentV2(this) { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
            _ContentPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            this.ContentControl = _ContentPanel;
        }
        DxDataFormContentV2 _ContentPanel;
        #endregion
    }
    public class DxDataFormV2xxxX : DxPanelControl, IDxDataFormV2
    {
        public DxDataFormV2xxxX()
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
            base.OnSizeChanged(e);
            DoLayoutContent();
        }
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DoLayoutContent();
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
            _VScrollBar = new DxVScrollBar() { Visible = false };
            this.Controls.Add(_VScrollBar);
            _HScrollBar = new DxHScrollBar() { Visible = false };
            _HScrollBar.ValueChanged += _HScrollBar_ValueChanged;
            this.Controls.Add(_HScrollBar);
            
            DoLayoutContent();
        }

        private void DoLayoutContent()
        {
            if (this.Parent == null) return;

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
            if (vVisible)
            {
                _VScrollBar.SetBounds(new Rectangle(clientWidth, 0, vScrollSize, clientHeight));
                _VScrollBar.Minimum = 0;
                _VScrollBar.Maximum = contentSize.Height - clientHeight;

            }
            if (hVisible)
            {
                _HScrollBar.SetBounds(new Rectangle(0, clientHeight, clientWidth, hScrollSize));
                _HScrollBar.Minimum = 0;
                int maximum = contentSize.Width - clientWidth; ;
                int bigChange = clientWidth - 15;
                int maxChange = maximum / 2;
                if (bigChange > maxChange) bigChange = maxChange;
                int minChange = bigChange / 4;
                if (minChange > 25) minChange = 25;


                maximum = 500;
                bigChange = 50;
                minChange = 25;

                _HScrollBar.Maximum = maximum;
                _HScrollBar.LargeChange = bigChange;
                _HScrollBar.SmallChange = minChange;
            }

            if (_VScrollBar.IsSetVisible() != vVisible) _VScrollBar.Visible = vVisible;
            if (_HScrollBar.IsSetVisible() != hVisible) _HScrollBar.Visible = hVisible;
        }

        private void _HScrollBar_ValueChanged(object sender, EventArgs e)
        {
            int oldValue = _HScrollValue;
            int newValue = _HScrollBar.Value;
            _HScrollValue = newValue;
        }
        private int _HScrollValue;

        private DxDataFormContentV2 _ContentPanel;
        private DxVScrollBar _VScrollBar;
        private DxHScrollBar _HScrollBar;
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
            PaintContentToGraphic(e, forceRefresh, offset);
            return;


            var bounds = this.CurrentBounds;
            bool withOffset = (offset.HasValue && !offset.Value.IsEmpty);

            /*   Časomíra:
                            Získat control   Vložit Bounds    Vložit Text   Selection   DrawToBitmap   PaintImage      Čas mikrosekund
            0. Nic:                                                                                                   :        6  (režie smyčky)
            1. PaintImage                                                                                  ANO        :       15
            2. Bez Bounds        ANO                                                                                  :       25
            3. Bez Text          ANO             ANO                                                                  :      320
            4. Bez Selection     ANO             ANO             ANO                                                  :      470
            5. Bez obrázku       ANO             ANO             ANO           ANO                                    :      470
            6. Bez kreslení      ANO             ANO             ANO           ANO                         ANO        :      500
            7. Komplet           ANO             ANO             ANO           ANO         ANO             ANO        :      990

            */
            if (_Image == null || forceRefresh)
            {
                _Image = CreateImageNative(bounds, !withOffset);
            }
            Point location = bounds.Location;
            if (withOffset) location = location.Add(offset.Value);
            if (_Image != null) e.Graphics.DrawImage(_Image, location);
        }

        private void PaintContentToGraphic(PaintEventArgs e, bool forceRefresh, Point? offset)
        {
            DevExpress.Utils.AppearanceObject app = DevExpress.Utils.AppearanceObject.ControlAppearance;
            // DevExpress.Utils.Drawing.BorderPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);
            // DevExpress.Utils.Drawing.TextFlatBorderPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);
            // DevExpress.Utils.Drawing.ToggleObjectPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);

            DevExpress.Utils.Drawing.Border3DSunkenPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);

            //this.gra
            //DevExpress.XtraEditors.Drawing.TextEditPainter p = new DevExpress.XtraEditors.Drawing.TextEditPainter();
            //DevExpress.XtraEditors.ViewInfo.TextEditViewInfo tve = new DevExpress.XtraEditors.ViewInfo.TextEditViewInfo();
            //DevExpress.Utils.Drawing.GraphicsCache c = DevExpress.Utils.Drawing.GraphicsCache.
            //DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs args = new DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs(tve, 
            //    new DevExpress.XtraEditors.ViewInfo.BaseControlViewInfo)
            //p.Draw(new DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs)
        }

        private Bitmap CreateImageNative(Rectangle bounds, bool storeValues)
        {
            int w = bounds.Width;
            int h = bounds.Height;
            Bitmap image = new Bitmap(w, h);

            //DevExpress.Utils.Drawing.BorderPainter.DrawTextOnGlass
            //    DevExpress.Utils.Drawing.SimpleBorderPainter.
            //DevExpress.XtraEditors.RangeControlBorderPainter.


            int r = w - 1;
            int b = h - 1;
            image.SetPixel(0, 0, Color.DarkBlue);
            image.SetPixel(r, 0, Color.DarkViolet);
            image.SetPixel(0, b, Color.DarkGreen);
            image.SetPixel(r, b, Color.DarkMagenta);

            return image;
        }


        private Bitmap CreateImageWithControl(Rectangle bounds, bool storeValues)
        {
            /*   Časomíra:
                           Získat control   Vložit Bounds    Vložit Text   Selection   DrawToBitmap   PaintImage      Čas mikrosekund
           0. Nic:                                                                                                   :        6  (režie smyčky)
           1. PaintImage                                                                                  ANO        :       15
           2. Bez Bounds        ANO                                                                                  :       25
           3. Bez Text          ANO             ANO                                                                  :      320
           4. Bez Selection     ANO             ANO             ANO                                                  :      470
           5. Bez obrázku       ANO             ANO             ANO           ANO                                    :      470
           6. Bez kreslení      ANO             ANO             ANO           ANO                         ANO        :      500
           7. Komplet           ANO             ANO             ANO           ANO         ANO             ANO        :      990

           */
            var source = _Owner.GetControl(this._ItemType, DxDataFormControlMode.Inactive);
            if (source == null) return null;

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
            if (storeValues)
            {
                DefaultCursor = cursor ?? Cursors.Default;
                ActiveBounds = bounds.Sub(source.Margin);
            }
            return image;
        }
        public bool IsActivePoint(Point point)
        {
            return (ActiveBounds.Contains(point));
        }
    }
}
