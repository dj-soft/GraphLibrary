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
    public class DxDataFormV2 : DxDataFormV2source
    { }
    public class DxDataFormV2grid : DevExpress.XtraGrid.GridControl
    {
        public DxDataFormV2grid()
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
            groupAddress.Text = "Addresa";
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
            colFirstName.Caption = "Křestní jméno";
            colLastName.Caption = "Příjmení";
            colAddress.Caption = "Ulice a č.p.";
            colCity.Caption = "Městečko";
            colCountry.Caption = "Okres / země";
            colPhoto.Caption = "Foťka";
            // Set the card's minimum size.
            lView.CardMinSize = new Size(350, 220);
            lView.CardCaptionFormat = "Xxxxx xxx";

            fieldLastName.Location = new Point(220, 20);

            fieldPhoto.TextVisible = false;
            fieldPhoto.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            fieldPhoto.MinSize = new Size(150, 150);
            fieldPhoto.MaxSize = new Size(200, 200);

        }

        public int ItemsCount;
        public void TestPerformance(int count, bool forceRefresh) { }
        public Size ContentSize;
    }
    public class DxDataFormV2paint : DxScrollableContent, IDxDataFormV2
    {
        public DxDataFormV2paint()
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
        protected override void OnInvalidateContentAfter()
        {
            this.ContentControl.Invalidate();
        }
        void IDxDataFormV2.OnPaintContent(PaintEventArgs e)
        {
            if (_PaintingItems) return;
            int count = (_PaintingPerformaceTestCount > 1 ? _PaintingPerformaceTestCount : 1);
            var size = this.ContentVisualSize;
            bool forceRefresh = _PaintingPerformaceForceRefresh;
            try
            {
                _PaintingItems = true;
                if (count == 1 && !forceRefresh)
                {   // Standard:
                    _Items.ForEachExec(i => PaintItem(i, e));
                }
                else
                {   // Performance test:
                    int x = 0;
                    int y = 0;
                    while (count > 0)
                    {
                        Point offset = new Point(x, y);
                        _Items.ForEachExec(i => PaintItem(i, e, forceRefresh, offset));
                        y += 12;
                        if (y >= size.Height)
                        {
                            y = 0;
                            x += 36;
                            if (x >= size.Width)
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

        private void PaintItem(DxDataFormItemV2 item, PaintEventArgs e, bool forceRefresh = false, Point? offset = null)
        {
            DevExpress.Utils.AppearanceObject app = DevExpress.Utils.AppearanceObject.ControlAppearance;
            // DevExpress.Utils.Drawing.BorderPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);
            // DevExpress.Utils.Drawing.TextFlatBorderPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);
            // DevExpress.Utils.Drawing.ToggleObjectPainter.DrawTextOnGlass(e.Graphics, app, this._Text, this.CurrentBounds);

            DevExpress.Utils.Drawing.Border3DSunkenPainter.DrawTextOnGlass(e.Graphics, app, item.Text, item.CurrentBounds);

            //this.gra
            //DevExpress.XtraEditors.Drawing.TextEditPainter p = new DevExpress.XtraEditors.Drawing.TextEditPainter();
            //DevExpress.XtraEditors.ViewInfo.TextEditViewInfo tve = new DevExpress.XtraEditors.ViewInfo.TextEditViewInfo();
            //DevExpress.Utils.Drawing.GraphicsCache c = DevExpress.Utils.Drawing.GraphicsCache.
            //DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs args = new DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs(tve, 
            //    new DevExpress.XtraEditors.ViewInfo.BaseControlViewInfo)
            //p.Draw(new DevExpress.XtraEditors.Drawing.ControlGraphicsInfoArgs)
        }

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
            this._ContentPanel.Invalidate();
        }
        int IDxDataFormV2.DeviceDpi { get { return this.DeviceDpi; } }
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
    public class DxDataFormV2source : DxScrollableContent, IDxDataFormV2
    {
        public DxDataFormV2source()
        {
            this.DoubleBuffered = true;

            InitializeContent();
            InitializeItems();
            InitializeSampleControls();
        }
        private void InitializeContent()
        {
            _ContentPanel = new DxDataFormContentV2(this) { Visible = true, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat };
            _ContentPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            this.ContentControl = _ContentPanel;
        }
        private DxDataFormContentV2 _ContentPanel;


        private void InitializeSampleControls()
        {
            _Label = new DxLabelControl() { Bounds = new Rectangle(20, 52, 70, 18), Text = "Popis" };
            _ContentPanel.Controls.Add(_Label);
            _TextBox = new DxTextEdit() { Bounds = new Rectangle(100, 50, 80, 20), Text = "Pokus" };
            _ContentPanel.Controls.Add(_TextBox);
            _CheckBox = new DxCheckEdit() { Bounds = new Rectangle(210, 50, 100, 20), Text = "Předvolba" };
            _ContentPanel.Controls.Add(_CheckBox);
        }
        public DxTextEdit TextBox { get { return _TextBox; } }
        private DxLabelControl _Label;
        private DxTextEdit _TextBox;
        private DxCheckEdit _CheckBox;

        private void InitializeItems()
        {
            _Items = new List<DxDataFormItemV2>();
            _VisibleItems = new List<DxDataFormItemV2>();
            _ContentPadding = new Padding(0);

            string text;
            int[] widths = new int[] { 80, 150, 80, 40, 100, 120, 160, 40, 120 };
            int count = 50;
            int y = 80;
            int maxX = 0;
            for (int r = 0; r < count; r++)
            {
                int x = 20;
                text = $"Řádek {(r + 1)}";
                _Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, text) { DesignBounds = new Rectangle(x, y + 2, 70, 18) });
                x += 80;
                foreach (int width in widths)
                {
                    text = $"Hodnota {width}";
                    _Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, text) { DesignBounds = new Rectangle(x, y, width, 20) });
                    x += width + 3;
                }
                maxX = x;
                y += 23;
            }

            this.ContentTotalSize = new Size(maxX + 12, y + 12);

            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 2") { DesignBounds = new Rectangle(20, 82, 70, 18) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 3") { DesignBounds = new Rectangle(20, 112, 70, 18) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.Label, "Popisek 4") { DesignBounds = new Rectangle(20, 142, 70, 18) });

            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 2") { DesignBounds = new Rectangle(100, 80, 80, 20) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 3") { DesignBounds = new Rectangle(100, 110, 80, 20) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 4") { DesignBounds = new Rectangle(100, 140, 80, 20) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.TextBox, "Pokus 5") { DesignBounds = new Rectangle(20, 170, 290, 20) });

            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 2") { DesignBounds = new Rectangle(210, 80, 100, 20) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 3") { DesignBounds = new Rectangle(210, 110, 100, 20) });
            //_Items.Add(new DxDataFormItemV2(this, DataFormItemType.CheckBox, "Předvolba 4") { DesignBounds = new Rectangle(210, 140, 100, 20) });
        }
        /// <summary>
        /// Okraje kolem vlastních prvků
        /// </summary>
        public Padding ContentPadding { get { return _ContentPadding; } set { _ContentPadding = value; RecalculateContentTotalSize(); InvalidateContent(); } }
        private Padding _ContentPadding;
        /// <summary>
        /// Počet prvků
        /// </summary>
        public int ItemsCount { get { return _Items.Count; } }
        public DxDataFormItemV2[] Items { get { return _Items.ToArray(); } }
        private List<DxDataFormItemV2> _Items;
        private List<DxDataFormItemV2> _VisibleItems;
        /// <summary>
        /// Souhrnná souřadnice všech prvků v <see cref="Items"/>
        /// </summary>
        public Rectangle? ItemsSummaryBounds { get { return DrawingExtensions.SummaryRectangle(_Items.Select(i => (Rectangle?)i.CurrentBounds)); } }
        /// <summary>
        /// Je vyvoláno po změně DPI, po změně Zoomu a po změně skinu. Volá se po přepočtu layoutu.
        /// Může vést k invalidaci interních dat v <see cref="DxScrollableContent.ContentControl"/>.
        /// </summary>
        protected override void OnInvalidateContentAfter()
        {
            this.RecalculateContentTotalSize();
            this.InvalidateCache();
            this.InvalidateContent();
        }
        /// <summary>
        /// Podle aktuálního seznamu prvků a jejich velikostí určí sumární velikost obsahu.
        /// Za poslední prvek přidává okraj definovaný v 
        /// </summary>
        protected void RecalculateContentTotalSize()
        {
            var summaryBounds = ItemsSummaryBounds;
            if (summaryBounds.HasValue)
                ContentTotalSize = new Size(summaryBounds.Value.Right + 6, summaryBounds.Value.Bottom + 6);
        }
        /// <summary>
        /// Je voláno pokud dojde ke změně hodnoty <see cref="DxScrollableContent.ContentVirtualBounds"/>, před eventem <see cref="DxScrollableContent.ContentVirtualBoundsChanged"/>
        /// </summary>
        protected override void OnContentVirtualBoundsChanged()
        {
            base.OnContentVirtualBoundsChanged();
            this.InvalidateContent();
        }
        /// <summary>
        /// Invaliduje prvek <see cref="DxScrollableContent.ContentControl"/> = vyžádá si jeho překreslení.
        /// Má se provádět po změnách souřadnic nebo po změnách prvků. 
        /// </summary>
        private void InvalidateContent()
        {
            this._VisibleItems = GetVisibleItems(this.ContentVirtualBounds);
            this.ContentControl.Invalidate();
        }
        /// <summary>
        /// Vrátí List prvků z pole <see cref="Items"/>, které jsou alespoň zčásti viditelné v aktuálním prostoru
        /// </summary>
        /// <returns></returns>
        private List<DxDataFormItemV2> GetVisibleItems(Rectangle bounds)
        {
            List<DxDataFormItemV2> visibleItems = _Items.Where(i => i.IsVisible && bounds.Contains(i.CurrentBounds, true)).ToList();
            return visibleItems;
        }
        void IDxDataFormV2.OnPaintContent(PaintEventArgs e)
        {
            if (_PaintingItems) return;
            var size = this.ContentVisualSize;
            int count = (_PaintingPerformaceTestCount > 1 ? _PaintingPerformaceTestCount : 1);
            bool forceRefresh = _PaintingPerformaceForceRefresh;
            try
            {
                _PaintingItems = true;
                if (count == 1 && !forceRefresh)
                {   // Standard:
                    _VisibleItems.ForEachExec(i => this.PaintItem(i, e));
                }
                else
                {   // Performance test:
                    int x = 0;
                    int y = 0;
                    Rectangle? sumBounds = ItemsSummaryBounds;
                    int maxX = size.Width - (sumBounds?.Right ?? 0) - 12;
                    int maxY = size.Height - (sumBounds?.Bottom ?? 0) - 12;
                    while (count > 0)
                    {
                        Point offset = new Point(x, y);
                        _VisibleItems.ForEachExec(i => PaintItem(i, e, forceRefresh, offset));
                        y += 12;
                        if (y >= maxY)
                        {
                            y = 0;
                            x += 36;
                            if (x >= maxX)
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
        private void PaintItem(DxDataFormItemV2 item, PaintEventArgs e, bool forceRefresh = false, Point? offset = null)
        {
            var image = GetImage(item);

            if (image != null)
            {
                var origin = this.ContentVirtualLocation;
                var bounds = item.CurrentBounds;
                bool withOffset = (offset.HasValue && !offset.Value.IsEmpty);
                Point location = bounds.Location.Sub(origin);
                if (withOffset) location = location.Add(offset.Value);
                e.Graphics.DrawImage(image, location);
            }

            //var bounds = this.CurrentBounds;
            //bool withOffset = (offset.HasValue && !offset.Value.IsEmpty);
            //if (_Image == null || forceRefresh)
            //    _Image = CreateImageWithControl(bounds, !withOffset);
            //Point location = bounds.Location;
            //if (withOffset) location = location.Add(offset.Value);
            //if (_Image != null) e.Graphics.DrawImage(_Image, location);
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
                if (_VisibleItems.TryGetFirst(i => i.IsActivePoint(e.Location), out var found) && found.DefaultCursor != null)
                    cursor = found.DefaultCursor;
                this.Cursor = cursor;
            }
        }
        int IDxDataFormV2.DeviceDpi { get { return this.DeviceDpi; } }



        #region Bitmap cache
        /// <summary>
        /// Zruší veškerý obsah z cache uložených Image <see cref="_ItemBitmapCache"/>, kde jsou uloženy obrázky pro jednotlivé ne-aktivní controly...
        /// Je nutno volat po změně skinu nebo Zoomu.
        /// </summary>
        private void InvalidateCache()
        {
            _ItemBitmapCache = null;
        }
        /// <summary>
        /// Najde a vrátí <see cref="Image"/> pro obsah daného prvku.
        /// Obrázek hledá nejprve v cache, a pokud tam není pak jej vygeneruje a do cache uloží.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Image GetImage(DxDataFormItemV2 item)
        {
            if (_ItemBitmapCache == null) _ItemBitmapCache = new Dictionary<string, Image>();

            string key = item.ContentKey;
            if (key == null) return null;
            if (_ItemBitmapCache.TryGetValue(key, out Image image)) return image;

            image = CreateBitmapForItem(item);

            lock (_ItemBitmapCache)
            {   // Do cache přidám i image == null, tím ušetřím opakované vytváření / testování obrázku.
                // Pro přidávání aplikuji lock(), i když tedy tahle činnost má probíhat jen v jednom threadu = GUI:
                if (!_ItemBitmapCache.ContainsKey(key))
                    _ItemBitmapCache.Add(key, image);
            }

            return image;
        }
        private Dictionary<string, Image> _ItemBitmapCache;
        private Image CreateBitmapForItem(DxDataFormItemV2 item)
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
            var source = GetControl(item.ItemType, DxDataFormControlMode.Inactive);
            if (source == null) return null;

            var bounds = item.CurrentBounds;

            Cursor cursor = null;

            // source.SetBounds(bounds);                  // Nastavím správné umístění, to kvůli obrázkům na pozadí panelu (různé skiny!), aby obrázky odpovídaly aktuální pozici...
            Rectangle sourceBounds = new Rectangle(4, 4, bounds.Width, bounds.Height);
            source.SetBounds(sourceBounds);
            source.Text = item.Text;

            var size = source.Size;
            if (size != bounds.Size)
            {
                item.CurrentSize = size;
                bounds = item.CurrentBounds;
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
            //if (storeValues)
            //{
            //    DefaultCursor = cursor ?? Cursors.Default;
            //    ActiveBounds = bounds.Sub(source.Margin);
            //}
            return image;
        }
        private Control GetControl(DataFormItemType itemType, DxDataFormControlMode mode)
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
        private void ResetImage(DxDataFormItemV2 item)
        {
            if (_ItemBitmapCache == null) return;

            string key = item.ContentKey;
            if (key == null) return;

            lock (_ItemBitmapCache)
            {
                if (_ItemBitmapCache.ContainsKey(key))
                    _ItemBitmapCache.Remove(key);
            }
        }
        #endregion
        public void TestPerformance(int count, bool forceRefresh)
        {
            _PaintingPerformaceTestCount = count;
            _PaintingPerformaceForceRefresh = forceRefresh;
            this._ContentPanel.Invalidate();
            Application.DoEvents();
        }
    }
    public interface IDxDataFormV2
    {
        int DeviceDpi { get; }
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
    public class DxDataFormItemV2 : DataTextItem
    {
        public DxDataFormItemV2(IDxDataFormV2 owner, DataFormItemType itemType, string text)
            : base()
        {
            _Owner = owner;
            _ItemType = itemType;
            Text = text;
            IsVisible = true;
        }

        private IDxDataFormV2 _Owner;
        private DataFormItemType _ItemType;
        private string _Text;

        public DataFormItemType ItemType { get { return _ItemType; } }
        public bool IsVisible { get; set; }

        /// <summary>
        /// Klíč obsahu. 
        /// Pokud dva různé prvky budou mít shodný klíč obsahu, pak oba prvky vypadají shodně.
        /// Klíč pokrývá: Typ objektu, velikost, obsah, vzhled, barvy, písmo.
        /// Slouží k tomu, aby v paměti bylo možno uložit sadu Image pro jednotlivé prvky, ale nikoli zbytečně opakované shodné Image.
        /// </summary>
        public string ContentKey
        {
            get
            {
                var size = CurrentBounds.Size;
                string text = Text ?? "";
                string key = $"{size.Width}.{size.Height}.{__CurrentDpi}::{text}";
                return key;
            }
        }
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
            }
        }
        private Rectangle __DesignBounds;
        private int __DesignDpi;
        public Rectangle CurrentBounds { get { this.CheckDesignBounds(); return __CurrentBounds.Value; } }
        /// <summary>
        /// Aktuální velikost prvku. Lze setovat (nezmění se umístění = <see cref="CurrentBounds"/>.Location).
        /// <para/>
        /// Setujme opatrně a jen v případě nutné potřeby, typicky tehdy, když konkrétní vizuální control nechce akceptovat předepsanou velikost (např. výška textboxu v jiném než očekávaném fontu).
        /// Vložená hodnota zde zůstane (a bude obsažena i v <see cref="CurrentBounds"/>) do doby, než se změní Zoom nebo Skin aplikace.
        /// </summary>
        public Size CurrentSize 
        { 
            get { this.CheckDesignBounds(); return __CurrentBounds.Value.Size; }
            set
            {
                this.CheckDesignBounds();
                __CurrentBounds = new Rectangle(__CurrentBounds.Value.Location, value);
            }
        }
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
        }

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


        public bool IsActivePoint(Point point)
        {
            return (ActiveBounds.Contains(point));
        }
    }
}
