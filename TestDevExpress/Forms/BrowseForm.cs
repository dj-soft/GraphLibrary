using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors.Filtering.Templates;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář obsahující Ribbon a Browse
    /// </summary>
    public class DxBrowseStandardForm : DxRibbonForm
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBrowseStandardForm()
        {
            this.CreateBrowse();
        }
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.DxRibbon.DebugName = "BrowseRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.ApplicationButtonText = " SYSTEM ";
            this.DxRibbon.LogActive = true;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ", MergeOrder = 1, PageOrder = 1 };
            pages.Add(page);

            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: false) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            page.Groups.Add(group);
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.Button, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10", Text = "Vlož 10", ToolTipText = "Do Gridu vloží 10 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add50", Text = "Vlož 50", ToolTipText = "Do Gridu vloží 50 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100", Text = "Vlož 100", ToolTipText = "Do Gridu vloží 100 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500", Text = "Vlož 500", ToolTipText = "Do Gridu vloží 500 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 1000", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add10k", Text = "Vlož 10000", ToolTipText = "Do Gridu vloží 10 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add100k", Text = "Vlož 100000", ToolTipText = "Do Gridu vloží 100 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add500k", Text = "Vlož 500000", ToolTipText = "Do Gridu vloží 500 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Refresh", Text = "Refresh", ToolTipText = "Občas něco změní - odebere řádek, vymění hodnoty, přidá řádky", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.Clear":
                    TargetRowCount = 0;
                    HasAllRows = true;
                    FillBrowse(0);
                    break;
                case "Dx.Test.Add10":
                    TargetRowCount = 10;
                    HasAllRows = false;
                    FillBrowse(10, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add50":
                    TargetRowCount = 50;
                    HasAllRows = false;
                    FillBrowse(20, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add100":
                    TargetRowCount = 100;
                    HasAllRows = false;
                    FillBrowse(20, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add500":
                    TargetRowCount = 500;
                    HasAllRows = false;
                    FillBrowse(50, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add1k":
                    TargetRowCount = 1000;
                    HasAllRows = false;
                    FillBrowse(200, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add10k":
                    TargetRowCount = 10000;
                    HasAllRows = false;
                    FillBrowse(200, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.Add100k":
                    TargetRowCount = 100000;
                    HasAllRows = false;
                    FillBrowse(200, Random.WordBookType.TaborSvatych);
                    break;
                case "Dx.Test.Add500k":
                    FillBrowse(500000, Random.WordBookType.CampOfSaints);
                    break;
                case "Dx.Test.BestFit":
                    BestFitBrowse();
                    break;
                case "Dx.Test.Refresh":
                    GridRefresh();
                    break;
            }
        }
        /// <summary>
        /// Připraví obsah StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();

            // V tomto pořadí budou StatusItemy viditelné (tady je zatím jen jeden):
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTitle);
        }
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(int? fontSizeDelta = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeDelta.HasValue)
                item.Appearance.FontSizeDelta = fontSizeDelta.Value;
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        /// <summary>
        /// Text ve status baru
        /// </summary>
        public string StatusText { get { return _StatusItemTitle?.Caption; } set { if (_StatusItemTitle != null) _StatusItemTitle.Caption = value; } }
        #endregion
        #region Browse
        private DevExpress.XtraGrid.GridSplitContainer _GridContainer;
        private DevExpress.XtraGrid.GridControl _Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView _View;
        private System.Data.DataTable _DataSource;
        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            _GridContainer = new DevExpress.XtraGrid.GridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);
            
            timeStart = DxComponent.LogTimeCurrent;
            this.DxMainPanel.Controls.Add(_GridContainer);
            var grid = _GridContainer.Grid;    // new DevExpress.XtraGrid.GridControl();
            _Grid = grid;
            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Specify a data source:
            _DataSource = _CreateGridDataTable();
            _Grid.DataSource = _DataSource;
            // string dataLog = FillData(rowCount, Random.WordBookType.TriMuziNaToulkach);
            string dataLog = AddDataRows(500);

            timeStart = DxComponent.LogTimeCurrent;
            var view = grid.AvailableViews["GridView"].CreateView(grid) as DevExpress.XtraGrid.Views.Grid.GridView;
            _View = view;

            view.VertScrollTipFieldName = "nazev";
            view.ScrollStyle = DevExpress.XtraGrid.Views.Grid.ScrollStyleFlags.LiveHorzScroll | DevExpress.XtraGrid.Views.Grid.ScrollStyleFlags.LiveVertScroll;
            view.VertScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;
            view.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFullFocus;

            view.OptionsFind.AlwaysVisible = true;
            view.OptionsView.ShowAutoFilterRow = true;
            view.OptionsView.ShowGroupPanelColumnsAsSingleRow = true;             // to je dobrý!
            //   view.OptionsView.ShowPreview = true;    preview je přidaný řádek pod každý řádek s daty

            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            view.OptionsBehavior.Editable = false;
            view.OptionsBehavior.SmartVertScrollBar = true;
            view.OptionsScrollAnnotations.ShowCustomAnnotations = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsScrollAnnotations.ShowFocusedRow = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsScrollAnnotations.ShowSelectedRows = DevExpress.Utils.DefaultBoolean.True;
            view.CustomScrollAnnotation += View_CustomScrollAnnotation;

            // Ošetřit v době plnění daty:
            view.TopRowChanged += View_TopRowChanged;
            view.RowCountChanged += View_RowCountChanged;
            
            // ScrollBar:
            view.CustomDrawScroll += View_CustomDrawScroll;
           
            grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // EditStyle for "status":
            PrepareEditStyleForStatus(view);

            // CustomDraw jako CodeTable:
            view.CustomDrawCell += View_CustomDrawCell;

            StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";
        }
        private void View_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.Column.FieldName == "status")
            {
                DrawStatusCellCodeTable(sender as DevExpress.XtraGrid.Views.Grid.GridView, e);
                DrawStatusCellDirectPaint(sender as DevExpress.XtraGrid.Views.Grid.GridView, e);
            }
        }

        #region Buňka s ImageComboBox jako CodeTable
        private void PrepareEditStyleForStatus(DevExpress.XtraGrid.Views.Grid.GridView view)
        {
            string[] displayValues = new string[]
            {
                "Akorát", "Beze všeho", "Co byste ještě chtěli", "Děkujeme", "Extra přídavek", "Fakturovat", "Grupování", "Hotovo"
            };
            string[] iconNames = new string[]
            {
    "images/scales/bluewhitered_16x16.png",
    "images/scales/geenyellow_16x16.png",
    "images/scales/greenwhite_16x16.png",
    "images/scales/greenwhitered_16x16.png",
    "images/scales/greenyellowred_16x16.png",
    "images/scales/redwhite_16x16.png",
    "images/scales/redwhiteblue_16x16.png",
    "images/scales/redwhitegreen_16x16.png",
    "images/scales/redyellowgreen_16x16.png",
    "images/scales/whitegreen_16x16.png",
    "images/scales/whitered_16x16.png",
    "images/scales/yellowgreen_16x16.png"
            };
            Color[] backColors = new Color[]
            {
                Color.FromArgb(255, 210, 255), Color.FromArgb(255, 210, 255), Color.FromArgb(255, 255, 210), Color.FromArgb(255, 255, 210),
                Color.FromArgb(210, 255, 255), Color.FromArgb(210, 255, 255), Color.FromArgb(210, 255, 210), Color.FromArgb(210, 255, 210)
            };
            Color backColor2 = Color.White;
            Color textColor = Color.Black;

            StatusCodeTable = new ComboCodeTable
                (
                new ComboCodeTable.Item() { Value = "A", DisplayText = displayValues[0], IconName = iconNames[0], BackColor1 = backColors[0], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "B", DisplayText = displayValues[1], IconName = iconNames[1], BackColor1 = backColors[1], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "C", DisplayText = displayValues[2], IconName = iconNames[2], BackColor1 = backColors[2], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "D", DisplayText = displayValues[3], IconName = iconNames[3], BackColor1 = backColors[3], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "E", DisplayText = displayValues[4], IconName = iconNames[4], BackColor1 = backColors[4], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "F", DisplayText = displayValues[5], IconName = iconNames[5], BackColor1 = backColors[5], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "G", DisplayText = displayValues[6], IconName = iconNames[6], BackColor1 = backColors[6], BackColor2 = backColor2, TextColor = textColor },
                new ComboCodeTable.Item() { Value = "H", DisplayText = displayValues[7], IconName = iconNames[7], BackColor1 = backColors[7], BackColor2 = backColor2, TextColor = textColor }
                );


            DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox repoCombo = StatusCodeTable.CreateRepositoryCombo();
            _RepoCombo = repoCombo;
            var colStatus = view.Columns["status"];
            colStatus.ColumnEdit = repoCombo;
        }
        private void DrawStatusCellCodeTable(DevExpress.XtraGrid.Views.Grid.GridView view, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            StatusCodeTable.DrawStatusCellCodeTable(view, e);
        }
        private DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox _RepoCombo;
        /// <summary>
        /// 
        /// </summary>
        private ComboCodeTable StatusCodeTable;
        /// <summary>
        /// Tabulka obsahující položky editačního stylu
        /// </summary>
        internal class ComboCodeTable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="items"></param>
            public ComboCodeTable(params Item[] items)
            {
                _ItemsDict = items.CreateDictionary(i => i.Value);
                GetValidResourceType();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"CodeTable; {ItemsCount} items.";
            }
            /// <summary>
            /// Prvky
            /// </summary>
            public Item[] Items { get { return _ItemsDict.Values.ToArray(); } }
            public int ItemsCount { get { return this._ItemsDict.Count; } }
            /// <summary>
            /// Prvky
            /// </summary>
            private Dictionary<object, Item> _ItemsDict;
            public DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox CreateRepositoryCombo()
            {
                DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox repoCombo = new DevExpress.XtraEditors.Repository.RepositoryItemImageComboBox();
                var resourceType = GetValidResourceType();
                var imageCollection = GetImageCollection(ResourceImageSizeType.Small, resourceType);
                repoCombo.SmallImages = imageCollection;
                foreach (var item in _ItemsDict.Values)
                    repoCombo.Items.Add(item.CreateComboItem(ResourceImageSizeType.Small, resourceType));
                int count = ItemsCount;
                repoCombo.DropDownRows = (count < 3 ? 3 : count < 12 ? count : 12);
                return repoCombo;
            }
            /// <summary>
            /// Pole obsahující Distinct typy ikon.
            /// Zde lze kontrolovat, že jsou zadány ikony shodného typu (pak má pole jen jeden prvek).
            /// Prvky bez ikony jsou ignorovány.
            /// </summary>
            private ResourceContentType[] ResourceTypes { get { return this._ItemsDict.Values.Select(i => i.ContentType).Where(t => t != ResourceContentType.None).Distinct().ToArray(); } }
            /// <summary>
            /// Metoda vrátí použitelný typ ikon. Pokud je zadán mix (vektor i bitmapy), vyhodí chybu.
            /// </summary>
            /// <returns></returns>
            private ResourceContentType GetValidResourceType()
            {
                var resourceTypes = ResourceTypes;
                if (resourceTypes.Length == 0) return ResourceContentType.None;
                if (resourceTypes.Length > 1) throw new InvalidOperationException($"Nelze v jedné CodeTable kombinovat různé typy ikon (bitmapy a vektory), aktuálně jsou detekovány: {resourceTypes.ToOneString(",")}.");
                return resourceTypes[0];
            }
            /// <summary>
            /// Vrátí ImageList pro aktuální druh ikon (vektor / bitmapa)
            /// </summary>
            /// <param name="sizeType"></param>
            /// <returns></returns>
            private object GetImageCollection(ResourceImageSizeType sizeType)
            {
                ResourceContentType resourceType = GetValidResourceType();
                return GetImageCollection(sizeType, resourceType);
            }
            /// <summary>
            /// Vrátí ImageList pro daný druh ikon (vektor / bitmapa)
            /// </summary>
            /// <param name="sizeType"></param>
            /// <param name="resourceType"></param>
            /// <returns></returns>
            private object GetImageCollection(ResourceImageSizeType sizeType, ResourceContentType resourceType)
            {
                switch (resourceType)
                {
                    case ResourceContentType.None:
                        return null;
                    case ResourceContentType.Bitmap:
                        return DxComponent.GetBitmapImageList(sizeType);
                    case ResourceContentType.Vector:
                        return DxComponent.GetVectorImageList(sizeType);
                    default:
                        throw new InvalidOperationException($"V CodeTable nelze použít jako ikony druh zdroje '{resourceType}'.");
                }
            }

            public void DrawStatusCellCodeTable(DevExpress.XtraGrid.Views.Grid.GridView view, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
            {
                object value = e.CellValue;
                if (value is null || !_ItemsDict.TryGetValue(value, out var item)) return;

                e.Appearance.BackColor = item.BackColor1.Value;
                e.Appearance.BackColor2 = item.BackColor2 ?? Color.Empty;
                e.Appearance.ForeColor = item.TextColor ?? Color.Empty;
                e.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
                e.DefaultDraw();
                e.Handled = true;
            }
            /// <summary>
            /// Jeden prvek CodeTable
            /// </summary>
            public class Item
            {
                /// <summary>
                /// Vizualizace
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return $"Value: '{Value}'; DisplayText: '{DisplayText}'";
                }
                public object Value;
                public string DisplayText;
                public string IconName;
                public Color? BackColor1;
                public Color? BackColor2;
                public Color? TextColor;
                /// <summary>
                /// Druh obrázku podle přípony <see cref="IconName"/>
                /// </summary>
                public ResourceContentType ContentType
                {
                    get
                    {
                        string iconName = this.IconName;
                        if (String.IsNullOrEmpty(iconName)) return ResourceContentType.None;
                        string extension = System.IO.Path.GetExtension(iconName);
                        if (String.IsNullOrEmpty(extension)) return ResourceContentType.None;
                        return DxComponent.GetContentTypeFromExtension(extension);
                    }
                }

                public DevExpress.XtraEditors.Controls.ImageComboBoxItem CreateComboItem(ResourceImageSizeType sizeType, ResourceContentType resourceType)
                {
                    int imageIndex = GetImageIndex(this.IconName, sizeType, resourceType);
                    var comboItem = new DevExpress.XtraEditors.Controls.ImageComboBoxItem(this.DisplayText, this.Value, imageIndex);
                    return comboItem;
                }
                private int GetImageIndex(string imageName, ResourceImageSizeType sizeType, ResourceContentType resourceType)
                {
                    switch (resourceType)
                    {
                        case ResourceContentType.Bitmap:
                            return DxComponent.GetBitmapImageIndex(imageName, sizeType);
                        case ResourceContentType.Vector:
                            return DxComponent.GetVectorImageIndex(imageName, sizeType);
                    }
                    return -1;
                }
            }
        }
        #endregion
        #region Custom kreslení buňky jako CodeTable
        private void DrawStatusCellDirectPaint(DevExpress.XtraGrid.Views.Grid.GridView view, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            // zrušeno:
            return;




            string value = (string)e.CellValue;             // (sender as GridView).GetRowCellValue(e.RowHandle, e.Column);

            if (!IsStatusValid(value)) return;

            e.DisplayText = "      " + GetStatusText(value);
            e.Appearance.ForeColor = GetStatusTextColor(value);
            e.Appearance.BackColor = GetStatusBackColor(value);
            e.Appearance.BackColor2 = Color.White;
            e.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            e.DefaultDraw();

            string imageName = GetStatusImage(value);
            if (!String.IsNullOrEmpty(imageName))
            {
                var image = DxComponent.GetBitmapImage(imageName);
                Rectangle bounds = new Rectangle(e.Bounds.X + 2, (e.Bounds.Y + (e.Bounds.Height / 2)) - 8, 16, 16);
                e.Graphics.DrawImage(image, bounds);
            }

            e.Handled = true;

        }
        private bool IsStatusValid(string statusCode)
        {
            switch (statusCode)
            {
                case "A":
                case "B":
                case "C":
                case "D":
                case "E":
                case "F":
                case "G":
                case "H": return true;
            }
            return false;
        }
        private string GetStatusText(string statusCode)
        {
            switch (statusCode)
            {
                case "A": return "Akorát";
                case "B": return "Beze všeho";
                case "C": return "Co byste ještě chtěli";
                case "D": return "Děkujeme";
                case "E": return "Extra přídavek";
                case "F": return "Fakturovat";
                case "G": return "Grupování";
                case "H": return "Hotovo";
            }
            return statusCode;
        }
        private Color GetStatusTextColor(string statusCode)
        {
            switch (statusCode)
            {
                case "A": return Color.FromArgb(0, 0, 0);
                case "B": return Color.FromArgb(0, 0, 40);
                case "C": return Color.FromArgb(0, 40, 0);
                case "D": return Color.FromArgb(0, 40, 40);
                case "E": return Color.FromArgb(40, 0, 0);
                case "F": return Color.FromArgb(40, 0, 40);
                case "G": return Color.FromArgb(40, 40, 0);
                case "H": return Color.FromArgb(40, 40, 40);
            }
            return Color.FromArgb(0, 0, 0);
        }
        private Color GetStatusBackColor(string statusCode)
        {
            switch (statusCode)
            {
                case "A": return Color.FromArgb(255, 210, 255);
                case "B": return Color.FromArgb(255, 210, 255);
                case "C": return Color.FromArgb(255, 255, 210);
                case "D": return Color.FromArgb(255, 255, 210);
                case "E": return Color.FromArgb(210, 255, 255);
                case "F": return Color.FromArgb(210, 255, 255);
                case "G": return Color.FromArgb(210, 255, 210);
                case "H": return Color.FromArgb(210, 255, 210);
            }
            return Color.FromArgb(0, 0, 0);
        }
        private string GetStatusImage(string statusCode)
        {
            string[] resources = new string[]
            {
    "images/scales/bluewhitered_16x16.png",
    "images/scales/geenyellow_16x16.png",
    "images/scales/greenwhite_16x16.png",
    "images/scales/greenwhitered_16x16.png",
    "images/scales/greenyellowred_16x16.png",
    "images/scales/redwhite_16x16.png",
    "images/scales/redwhiteblue_16x16.png",
    "images/scales/redwhitegreen_16x16.png",
    "images/scales/redyellowgreen_16x16.png",
    "images/scales/whitegreen_16x16.png",
    "images/scales/whitered_16x16.png",
    "images/scales/yellowgreen_16x16.png"
            };
            switch (statusCode)
            {
                case "A": return resources[0];
                case "B": return resources[1];
                case "C": return resources[2];
                case "D": return resources[3];
                case "E": return resources[4];
                case "F": return resources[5];
                case "G": return resources[6];
                case "H": return resources[7];
            }
            return "";
        }
        #endregion
        #region Scrollbar a Auto LoadNext
        /// <summary>
        /// Cílový počet řádků, null = bez omezení
        /// </summary>
        protected int? TargetRowCount { get; set; } = null;
        /// <summary>
        /// Jsou načteny všechny řádky?
        /// </summary>
        protected bool HasAllRows { get; set; } = false;
        private void View_CustomDrawScroll(object sender, DevExpress.XtraEditors.ScrollBarCustomDrawEventArgs e)
        {
            var v = e.Info.ScrollBar.Value;

            if (e.Info.ScrollBar.ScrollBarType == DevExpress.XtraEditors.ScrollBarType.Vertical)
            {
                Int32Range total = new Int32Range(e.Info.ScrollBar.Minimum, e.Info.ScrollBar.Maximum);
                Int32Range visible = new Int32Range(e.Info.ScrollBar.Value, e.Info.ScrollBar.Value + e.Info.ScrollBar.LargeChange);
                if (!HasAllRows && (visible.End > (total.End - 2 * visible.Size)))
                {
                    this.AddDataRowsAsync(4 * visible.Size);
                }

            }

        }
        private void View_CustomScrollAnnotation(object sender, DevExpress.XtraGrid.Views.Grid.GridCustomScrollAnnotationsEventArgs e)
        {
            e.Annotations = new List<DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo>();
            e.Annotations.Add(new DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo() { Index = 32, Color = System.Drawing.Color.DarkBlue, RowHandle = 32 });
            e.Annotations.Add(new DevExpress.XtraGrid.Views.Grid.GridScrollAnnotationInfo() { Index = 480, Color = System.Drawing.Color.Violet, RowHandle = 480 });
        }
        private void View_TopRowChanged(object sender, EventArgs e)
        {
        }
        private void View_RowCountChanged(object sender, EventArgs e)
        {
        }
        #endregion

        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        private void FillBrowse(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            if (rowCount <= 0)
            {
                _DataSource.Rows.Clear();
                _NextRowId = 0;
            }
            else
            {
                string dataLog = AddDataRows(rowCount, wordBookType);
                StatusText = dataLog;
            }
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        private void BestFitBrowse()
        {
            var timeStart = DxComponent.LogTimeCurrent;
            _View.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);
            StatusText = $"BestFitColumns: {timeFitColumns} sec";
        }
        /// <summary>
        /// Refresh dat
        /// </summary>
        private void GridRefresh()
        {
            var source = _DataSource;
            Dictionary<int, object[]> changes = _CreateDataChanges(source.Rows.Count);

            int cnt = this._View.DataRowCount;
            var firstVisibleIndex = this._View.TopRowIndex;
            int focusedRowIndex = this._View.GetFocusedDataSourceRowIndex();
            int focusedRowVisibleOffset = focusedRowIndex - firstVisibleIndex;


            var firstVisibleId = (firstVisibleIndex >= 0 && firstVisibleIndex < cnt) ? source.Rows[firstVisibleIndex][0] : null;
            var focusedRowId = (focusedRowIndex >= 0 && focusedRowIndex < cnt) ? source.Rows[focusedRowIndex][0] : null;


            // Handle se odvolává na total index, kdežto VisibleIndex pracuje jen nad filtrovanými řádky.
            // VisibleIndex ale nijak nepracuje s reálně viditelnými řádky v CurrentVisibleArea.
            int visInd = this._View.GetVisibleIndex(this._View.FocusedRowHandle);

            var dh = this._Grid.FocusedView.DetailHeight;          // 350 = nejde o plochu dat

            var focusRow = _View.GetFocusedDataRow();

            if (this._View.FocusedRowHandle >= 0)
            {
                int vi = _View.GetVisibleIndex(this._View.FocusedRowHandle);
            }



            _ApplyDataChanges(source, changes, ref focusedRowIndex);


            firstVisibleIndex = focusedRowIndex - focusedRowVisibleOffset;
            this._View.TopRowIndex = firstVisibleIndex;
            int newRowHandle = this._View.GetRowHandle(focusedRowIndex);
            this._View.FocusedRowHandle = newRowHandle;

        }
        /// <summary>
        /// Vytvoří a vrátí Dictionary obsahující změny dat pro cílovou tabulku se stávajícím daným počtem vět
        /// </summary>
        /// <param name="currCount"></param>
        /// <returns></returns>
        private Dictionary<int, object[]> _CreateDataChanges(int currCount)
        {
            Dictionary<int, object[]> changes = new Dictionary<int, object[]>();

            int deleteCount = 0;
            int modifyCount = 0;
            int appendCount = 0;

            // Některé řádky smažeme:
            if (currCount >= 20)
            {
                deleteCount = (currCount <= 60 ? currCount / 10 : currCount / 15);       // currCount je dozajista 20 a více. Pokud je do 60, pak deleteCount je 2 až 6; pro větší je 4 a více (1/15 = 6.66%)
                deleteCount = Random.Rand.Next(1, deleteCount);                          // deleteCount je 1 až 10% počtu pro malé seznamy, nebo 1 až 6.66% pro velké...
                for (int i = 0; i < deleteCount; i++)
                {
                    int index = Random.Rand.Next(0, currCount);                          // Index řádku k vymazání
                    if (!changes.ContainsKey(index))                                     // Když index ještě není použit, přidáme jej; Value NULL = odebrat
                        changes.Add(index, null);
                    else
                        i--;                                                             // Pokud náhodný index už v seznamu máme, dáme si další pokus...
                }
            }

            // Některé řádky modifikujeme:
            if (currCount >= 20)
            {
                modifyCount = (currCount <= 60 ? currCount / 10 : currCount / 15);       // currCount je dozajista 20 a více. Pokud je do 60, pak modifyCount je 2 až 6; pro větší je 4 a více (1/15 = 6.66%)
                modifyCount = Random.Rand.Next(1, modifyCount);                          // modifyCount je 1 až 10% počtu pro malé seznamy, nebo 1 až 6.66% pro velké...
                for (int i = 0; i < modifyCount; i++)
                {
                    int index = Random.Rand.Next(0, currCount);                          // Index řádku k modifikování
                    if (!changes.ContainsKey(index))                                     // Když index ještě není použit, přidáme jej; Value dostane ID = (index + 1)
                        changes.Add(index, _CreateDataRow(index + 1));
                    else
                        i--;                                                             // Pokud náhodný index už v seznamu máme, dáme si další pokus...
                }
            }

            // Několik řádků přidáme:   přidáme přesně tolik řádků, kolik jsme jich smazali!
            // appendCount = Random.Rand.Next(10, 20 + (currCount / 20));
            appendCount = deleteCount;
            int rowId = currCount;
            for (int i = 0; i < appendCount; i++)
            {
                changes.Add(rowId++, _CreateNewDataRow());
            }

            return changes;
        }
        /// <summary>
        /// Do dané tabulky aplikuje dodané změny
        /// </summary>
        /// <param name="source"></param>
        /// <param name="changes"></param>
        /// <param name="focusedRowIndex">Index řádku s focusem v rámci tabulky.
        /// Pokud tento řádek v rámci aplikace změn smažu, měl bych do této proměnné vložit nejbližší vyšší řádek. 
        /// Pokud smažu řádky nižší, měl bych zmenšit tento index, aby ukazoval na stále tentýž řádek.
        /// Na výstupu by měla být hodnota 0 až (RowCount-1).</param>
        private void _ApplyDataChanges(System.Data.DataTable source, Dictionary<int, object[]> changes, ref int focusedRowIndex)
        {
            int currCount = source.Rows.Count;

            // Modifikovat řádky (provést před smazáním, aby platily indexy): ty, které mají index menší než Count, a které mají data:
            var modifyRows = changes.Where(c => c.Key < currCount && c.Value != null).ToList();
            modifyRows.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var modifyRow in modifyRows)
                source.Rows[modifyRow.Key].ItemArray = modifyRow.Value;

            // Smazat řádky (setřídit podle indexu sestupně): ty, které mají index menší než Count, a které nemají data:
            var deleteRows = changes.Where(c => c.Value is null).ToList();
            deleteRows.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (var deleteRow in deleteRows)
            {
                int deleteIndex = deleteRow.Key;
                if (deleteIndex == focusedRowIndex) focusedRowIndex++;
                else if (deleteIndex < focusedRowIndex) focusedRowIndex++;
                source.Rows.RemoveAt(deleteIndex);
            }

            // Přidat nové řádky (v pořadí jejich indexu): ty, které mají index menší než výchozí Count, a které mají data:
            var appendRows = changes.Where(c => c.Key >= currCount && c.Value != null).ToList();
            appendRows.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var appendRow in appendRows)
                source.Rows.Add(appendRow.Value);

            int rowCount = source.Rows.Count;
            if (focusedRowIndex >= rowCount) focusedRowIndex = rowCount - 1;
            if (focusedRowIndex < 0) focusedRowIndex = (rowCount == 0 ? -1 : 0);
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private string AddDataRowsAsync(int rowCount)
        {
            ThreadManager.AddAction(() => AddDataRowsBgr(rowCount));
            return "Background...";
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        private void AddDataRowsBgr(int rowCount)
        {
            System.Threading.Thread.Sleep(500);                      // Něco jako uděláme...  - doba pro krátké čekání na data ze serveru

            this.RunInGui(() => AddDataRows(rowCount));              // Naplnění dáme do GUI
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private string AddDataRows(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            var timeStart = DxComponent.LogTimeCurrent;
            var rows = _CreateDataRows(rowCount, wordBookType);
            var timeCreateData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // nevolat event View_RowCountChanged o změně počtu řádků:
            _Grid.BeginInit();
            _Grid.SuspendLayout();
            timeStart = DxComponent.LogTimeCurrent;
            foreach (var row in rows)
                _DataSource.Rows.Add(row);
            _Grid.ResumeLayout();
            _Grid.EndInit();

            var timeSetData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            string logText = $"Generování řádků [{rowCount}]: {timeCreateData} sec;     Vložení DataTable do Gridu: {timeSetData} sec;     ";
            return logText;
        }
        /// <summary>
        /// Vytvoří a vrátí data pro hlavní tabulku, bez řádků
        /// </summary>
        /// <returns></returns>
        private System.Data.DataTable _CreateGridDataTable()
        {
            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "id", Caption = "ID", DataType = typeof(int) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "refer", Caption = "Reference", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "nazev", Caption = "Název", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "category", Caption = "Kategorie", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "status_code", Caption = "Status", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "status", Caption = "Stav", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "period", Caption = "Období", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_inp", Caption = "Datum vstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_out", Caption = "Datum výstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "quantity", Caption = "Počet kusů", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_unit", Caption = "Cena jednotková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_total", Caption = "Cena celková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "note", Caption = "Poznámka", DataType = typeof(string) });

            return table;
        }
        /// <summary>
        /// Vytvoří a vrátí řádky pro hlavní tabulku
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private List<object[]> _CreateDataRows(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            List<object[]> rows = new List<object[]>();

            var currWords = Random.ActiveWordBook;
            Random.ActiveWordBook = wordBookType;

            int? targetRowCount = this.TargetRowCount;
            int currCount = _DataSource.Rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                int id = currCount + i + 1;
                if (targetRowCount.HasValue && id > targetRowCount.Value)
                {
                    HasAllRows = true;
                    break;
                }
                rows.Add(_CreateNewDataRow());
            }

            Random.ActiveWordBook = currWords;

            return rows;
        }
        private object[] _CreateNewDataRow()
        {
            int rowId = ++_NextRowId;
            return _CreateDataRow(rowId);
        }
        private object[] _CreateDataRow(int rowId)
        {
            string refer = "DL:" + Random.Rand.Next(100000, 1000000).ToString();
            string nazev = Random.GetSentence(1, 3, false);
            string category = Random.GetItem(Categories);
            string status = Random.GetItem(Statuses);
            DateTime dateInp = DateFirst.AddDays(Random.Rand.Next(0, 730));
            DateTime dateOut = dateInp.AddDays(Random.Rand.Next(7, 90));
            string period = dateInp.Year.ToString() + "-" + dateInp.Month.ToString("00");
            decimal qty = (decimal)(Random.Rand.Next(10, 1000)) / 10m;
            decimal price1 = (decimal)(Random.Rand.Next(10, 10000)) / 10m;
            decimal priceT = qty * price1;
            string note = Random.GetSentence(5, 9, true);

            object[] row = new object[] { rowId, refer, nazev, category, status, status, period, dateInp, dateOut, qty, price1, priceT, note };
            return row;
        }
        private string[] Categories 
        {
            get
            {
                if (_Categories is null)
                    _Categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
                return _Categories;
            }
        }
        private string[] _Categories = null;
        private string[] Statuses
        {
            get
            {
                if (_Statuses is null)
                    _Statuses = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
                return _Statuses;
            }
        }
        private string[] _Statuses = null;

        private DateTime DateFirst
        {
            get
            {
                if (!_DateFirst.HasValue)
                    _DateFirst = new DateTime(DateTime.Now.Year, 1, 1);
                return _DateFirst.Value;
            }
        }
        private DateTime? _DateFirst = null;

        private int _NextRowId = 0;
        #endregion
    }

    /// <summary>
    /// Formulář obsahující Ribbon a Browse Virtual
    /// </summary>
    public class DxBrowseVirtualForm : DxRibbonForm
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBrowseVirtualForm()
        {
            this.CreateBrowse();
        }
        #endregion
        #region Ribbon a StatusBar - obsah a rozcestník
        /// <summary>
        /// Připraví obsah Ribbonu
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            this.DxRibbon.DebugName = "BrowseRibbon";
            this.DxRibbon.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.DxRibbon.ApplicationButtonText = " SYSTEM ";
            this.DxRibbon.LogActive = true;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ", MergeOrder = 1, PageOrder = 1 };
            pages.Add(page);

            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: false) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupId = "params", GroupText = "BROWSE TEST" };
            page.Groups.Add(group);
            string resourceClear = "svgimages/spreadsheet/removetablerows.svg";
            string resourceAddIcon = "svgimages/richedit/addparagraphtotableofcontents.svg";
            string resourceBestFit = "svgimages/richedit/tableautofitwindow.svg";

            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Clear", Text = "Smaž řádky", ToolTipText = "Do Gridu vloží tabulku bez řádků", ItemType = RibbonItemType.Button, ImageName = resourceClear, RibbonStyle = RibbonItemStyles.Large });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.Add1k", Text = "Vlož 1000", ToolTipText = "Do Gridu vloží 1 000 řádek", ItemType = RibbonItemType.Button, ImageName = resourceAddIcon, RibbonStyle = RibbonItemStyles.Large, ItemIsFirstInGroup = true });
            group.Items.Add(new DataRibbonItem() { ItemId = "Dx.Test.BestFit", Text = "Uprav šířky", ToolTipText = "V Gridu upraví šířky sloupců podle jejich obsahu", ItemType = RibbonItemType.Button, ImageName = resourceBestFit, RibbonStyle = RibbonItemStyles.Large });

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            this.DxRibbon.RibbonItemClick += _DxRibbonControl_RibbonItemClick;
        }
        private void _DxRibbonControl_RibbonItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "Dx.Test.Clear":
                    FillBrowse(0);
                    break;
                case "Dx.Test.Add1k":
                    FillBrowse(1000, Random.WordBookType.TriMuziNaToulkach);
                    break;
                case "Dx.Test.BestFit":
                    BestFitBrowse();
                    break;
            }
        }
        /// <summary>
        /// Připraví obsah StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this._StatusItemTitle = CreateStatusBarItem();

            // V tomto pořadí budou StatusItemy viditelné (tady je zatím jen jeden):
            this.DxStatusBar.ItemLinks.Add(this._StatusItemTitle);
        }
        private DevExpress.XtraBars.BarStaticItem CreateStatusBarItem(int? fontSizeDelta = null)
        {
            DevExpress.XtraBars.BarStaticItem item = new DevExpress.XtraBars.BarStaticItem();
            item.MinWidth = 240;
            item.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            if (fontSizeDelta.HasValue)
                item.Appearance.FontSizeDelta = fontSizeDelta.Value;
            item.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            return item;
        }
        private DevExpress.XtraBars.BarStaticItem _StatusItemTitle;
        /// <summary>
        /// Text ve status baru
        /// </summary>
        public string StatusText { get { return _StatusItemTitle?.Caption; } set { if (_StatusItemTitle != null) _StatusItemTitle.Caption = value; } }
        #endregion
        #region Browse
        private DevExpress.XtraGrid.GridSplitContainer _GridContainer;
        private DevExpress.XtraGrid.GridControl _Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView _View;
        private DevExpress.Data.VirtualServerModeSource _VirtualDataSource;
        /// <summary>
        /// Vytvoří objekt Browse a dá mu základní vzhled.
        /// Vloží tabulku s daty s daným počtem řádků, default = 0 (tzn. budou tam jen sloupce!).
        /// </summary>
        protected void CreateBrowse(int rowCount = 0)
        {
            var timeStart = DxComponent.LogTimeCurrent;

            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource
            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource.AcquireInnerList
            // https://docs.devexpress.com/CoreLibraries/DevExpress.Data.VirtualServerModeSource.CanPerformColumnServerAction
            // 
            _VirtualDataSource = new DevExpress.Data.VirtualServerModeSource();
            _VirtualDataSource.RowType = null;   // typeof(Product);
            _VirtualDataSource.AcquireInnerList += _VirtualDataSource_AcquireInnerList;
            _VirtualDataSource.ConfigurationChanged += _VirtualDataSource_ConfigurationChanged;
            _VirtualDataSource.MoreRows += _VirtualDataSource_MoreRows;

            _GridContainer = new DevExpress.XtraGrid.GridSplitContainer();
            _GridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            _GridContainer.Initialize();
            _GridContainer.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;
            var timeInit = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            this.DxMainPanel.Controls.Add(_GridContainer);
            var grid = _GridContainer.Grid;
            _Grid = grid;
            _Grid.DataSource = _VirtualDataSource;

            // _Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            var timeAdd = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Specify a data source:
            string dataLog = FillData(rowCount, Random.WordBookType.TriMuziNaToulkach);

            timeStart = DxComponent.LogTimeCurrent;
            var view = grid.AvailableViews["GridView"].CreateView(grid) as DevExpress.XtraGrid.Views.Grid.GridView;
            _View = view;
            view.OptionsFind.AlwaysVisible = true;
            view.OptionsDetail.DetailMode = DevExpress.XtraGrid.Views.Grid.DetailMode.Embedded;
            grid.MainView = view;
            var timeCreateView = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            // Resize columns according to their values:
            timeStart = DxComponent.LogTimeCurrent;
            view.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            StatusText = $"Tvorba GridSplitContainer: {timeInit} sec;     Přidání na Form: {timeAdd} sec;     {dataLog}Generování View: {timeCreateView} sec;     BestFitColumns: {timeFitColumns} sec";
        }

        private void _VirtualDataSource_AcquireInnerList(object sender, DevExpress.Data.VirtualServerModeAcquireInnerListEventArgs e)
        {
            e.ClearAndAddRowsFunc = _VirtualDataSource_ClearAndAddRowsFunc;
            e.AddMoreRowsFunc = _VirtualDataSource_AddMoreRowsFunc;
            e.InnerList = new System.Collections.ArrayList();
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.InnerList.Add(_CreateGridTableCells());
            e.ReleaseAction = _VirtualDataSource_ReleaseAction;
        }
        private System.Collections.IList _VirtualDataSource_ClearAndAddRowsFunc(System.Collections.IList list1, System.Collections.IEnumerable list2)
        {
            return null;
        }
        private System.Collections.IList _VirtualDataSource_AddMoreRowsFunc(System.Collections.IList list1, System.Collections.IEnumerable list2)
        {
            return null;
        }
        private void _VirtualDataSource_ReleaseAction(System.Collections.IList list1)
        {
        }

        private void _VirtualDataSource_ConfigurationChanged(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
        {
            
        }

        private void _VirtualDataSource_MoreRows(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
        {
            
        }

        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        private void FillBrowse(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            string dataLog = FillData(rowCount, wordBookType);
            StatusText = dataLog;
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, a do Status baru vloží odpovídající text (časy)
        /// </summary>
        private void BestFitBrowse()
        {
            var timeStart = DxComponent.LogTimeCurrent;
            _View.BestFitColumns();
            var timeFitColumns = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);
            StatusText = $"BestFitColumns: {timeFitColumns} sec";
        }
        /// <summary>
        /// Vytvoří Main data a vloží je do <see cref="_Grid"/>, sestaví a vrátí text (obsahující časy) určený do Status baru (ale nevkládá jej tam)
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private string FillData(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            return "";





            var timeStart = DxComponent.LogTimeCurrent;
            var data = _CreateGridDataTable(rowCount, wordBookType);
            var timeCreateData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            timeStart = DxComponent.LogTimeCurrent;
            _Grid.DataSource = data;
            var timeSetData = DxComponent.LogGetTimeElapsed(timeStart, DxComponent.LogTokenTimeSec);

            string logText = $"Generování DataTable[{rowCount}]: {timeCreateData} sec;     Vložení DataTable do Gridu: {timeSetData} sec;     ";
            return logText;
        }
        /// <summary>
        /// Vytvoří a vrátí data pro hlavní tabulku
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="wordBookType"></param>
        /// <returns></returns>
        private System.Data.DataTable _CreateGridDataTable(int rowCount, Random.WordBookType wordBookType = Random.WordBookType.TriMuziNaToulkach)
        {
            var currWords = Random.ActiveWordBook;
            Random.ActiveWordBook = wordBookType;

            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "id", Caption = "ID", DataType = typeof(int) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "refer", Caption = "Reference", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "nazev", Caption = "Název", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "category", Caption = "Kategorie", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "period", Caption = "Období", DataType = typeof(string) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_inp", Caption = "Datum vstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "date_out", Caption = "Datum výstupu", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "quantity", Caption = "Počet kusů", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_unit", Caption = "Cena jednotková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "price_total", Caption = "Cena celková", DataType = typeof(decimal) });
            table.Columns.Add(new System.Data.DataColumn() { ColumnName = "note", Caption = "Poznámka", DataType = typeof(string) });

            string[] categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
            int year = DateTime.Now.Year;
            DateTime dateBase = new DateTime(year, 1, 1);
            for (int i = 0; i < rowCount; i++)
            {
                var cells = _CreateGridTableCells(i + 1, categories, dateBase);
                table.Rows.Add(cells);
            }

            Random.ActiveWordBook = currWords;

            return table;
        }
        private object[] _CreateGridTableCells()
        {
            string[] categories = new string[] { "NÁKUP", "PRODEJ", "SKLAD", "TUZEMSKO", "EXPORT", "IMPORT" };
            int year = DateTime.Now.Year;
            DateTime dateBase = new DateTime(year, 1, 1);
            return _CreateGridTableCells(0, categories, dateBase);
        }
        private object[] _CreateGridTableCells(int id, string[] categories, DateTime dateBase)
        {
            string refer = "DL:" + Random.Rand.Next(100000, 1000000).ToString();
            string nazev = Random.GetSentence(1, 3, false);
            string category = Random.GetItem(categories);
            DateTime dateInp = dateBase.AddDays(Random.Rand.Next(0, 730));
            DateTime dateOut = dateInp.AddDays(Random.Rand.Next(7, 90));
            string period = dateInp.Year.ToString() + "-" + dateInp.Month.ToString("00");
            decimal qty = (decimal)(Random.Rand.Next(10, 1000)) / 10m;
            decimal price1 = (decimal)(Random.Rand.Next(10, 10000)) / 10m;
            decimal priceT = qty * price1;
            string note = Random.GetSentence(5, 9, true);
            return new object[] { id, refer, nazev, category, period, dateInp, dateOut, qty, price1, priceT, note };
        }
        #endregion
    }

}
