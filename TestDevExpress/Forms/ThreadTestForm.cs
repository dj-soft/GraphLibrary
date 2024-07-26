using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using WinDraw = System.Drawing;
using DxDForm = Noris.Clients.Win.Components.AsolDX.DataForm;
using DxDData = Noris.Clients.Win.Components.AsolDX.DataForm.Data;
using DxLData = Noris.Clients.Win.Components.AsolDX.DataForm.Layout;
using System.Drawing;
using Noris.WS.DataContracts.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy Threads
    /// </summary>
    [RunFormInfo(groupText: "Testovací okna", buttonText: "Threads", buttonOrder: 120, buttonImage: "svgimages/dashboards/grouplabels.svg", buttonToolTip: "Testuje práci na pozadí a popředí přes GuiInject", tabViewToolTip: "Testuje práci na pozadí a popředí přes GuiInject")]
    internal class ThreadTestForm : DxRibbonForm
    {
        #region Inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ThreadTestForm()
        {
            this.Text = "Threads tester";
            this.ImageName = "svgimages/dashboards/grouplabels.svg";
            this.ImageNameAdd = "@text|D|#002266||B|3|#88AAFF|#CCEEFF";
        }
        protected override void Dispose(bool disposing)
        {
            _ThreadStop();
            base.Dispose(disposing);
        }
        #endregion
        #region Main Content
        /// <summary>
        /// Provede přípravu obsahu hlavního panelu <see cref="DxRibbonForm.DxMainPanel"/>. Panel je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected override void DxMainContentPrepare()
        {
            __MainPanel = DxComponent.CreateDxPanel(this.DxMainPanel, DockStyle.None, DevExpress.XtraEditors.Controls.BorderStyles.HotFlat, 850, 450);

            // Určitě hezčí:
            // _PrepareListBoxTyped();              // typová data
            _PrepareListBoxTable();                 // datatable


            // Není tak pěkný:
            // _PrepareListBoxHtml();

            _ThreadStart();
        }
        #region ListBox nad DataTable a HTML Template

        private void _PrepareListBoxHtml()
        {
            var template = createTemplate();
            var data = createData(120);

            __ThreadListBox = DxComponent.CreateDxListBox(300, 28, 544, 416, __MainPanel);
            __ThreadListBox.HtmlTemplates.Add(template);
            __ThreadListBox.DataSource = data;
            __ThreadListBox.ItemAutoHeight = true;
            __ThreadListBox.ContextButtons.Add(new DevExpress.Utils.ContextItem() { Visibility = DevExpress.Utils.ContextItemVisibility.Visible, Size = new WinDraw.Size(24, 24) });

            __Searcher = new DevExpress.XtraEditors.SearchControl();
            __Searcher.Bounds = new Rectangle(300, 6, 544, 20);
            __Searcher.Client = __ThreadListBox;
            __Searcher.Properties.NullValuePrompt = "Co byste chtěli najít?";
            __MainPanel.Controls.Add(__Searcher);


            DevExpress.Utils.Html.HtmlTemplate createTemplate()
            {
                //  https://docs.devexpress.com/WindowsForms/DevExpress.Utils.Html.HtmlTemplate
                //  https://docs.devexpress.com/WindowsForms/403397/common-features/html-css-based-desktop-ui
                DevExpress.Utils.Html.HtmlTemplate template = new DevExpress.Utils.Html.HtmlTemplate();

                string html, css;
                html = @"<div><img src='${iconname}'></div>";
                html = @"<div><b>Hlavička</b></div>";
                html = @"<div>
<table>
  <tr height='30'>
    <td width='60'>${refer}</td>
    <td width='120'>${nazev}</td>
    <td width='360'>${suffix}</td>
  </tr>
  <tr height='50'>
    <td colspan='3' width='540'>${note}</td>
  </tr>
</table>
</div>";

                html = @"<div>
<table>
  <tr height = 30>
    <td width = 60>${refer}</td>
    <td width = 120>${nazev}</td>
    <td width = 360>${suffix}</td>
  </tr>
  <tr height = 50>
    <td colspan = 3 width = 540>${note}</td>
  </tr>
</table>
</div>";
                css = "";


                // Dx wiki:
                html = @"<div class=""container"" id=""container"">    
    <div class=""avatarContainer"">       
        <img src=""${iconname}"" class=""avatar"">
        <div id=""uploadBtn"" onclick=""OnButtonClick"" class=""centered button"">Upload</div>
        <div id=""removeBtn"" onclick=""OnButtonClick"" class=""centered button"">Remove</div>
    </div>
    <div class=""separator""></div>
    <div class=""avatarContainer "">
        <div class=""field-container"">
            <div class=""field-header"">
                <b>Display name</b><b class=""hint"">Visible to other members</b>
            </div>
            <p>${refer}</p>           
        </div>
        <div class=""field-container with-left-margin"">
            <div class=""field-header"">
                <b>Full name</b><b class=""hint"">Not visible to other members</b>
            </div>
            <p>${nazev}</p>   
        </div>
    </div>
</div>
";

                css = @".container{
    background-color:@Window;
    display:flex;
    flex-direction: column;
    justify-content: space-between;
    border-radius: 10px;
    padding: 0px 12px 16px 12px;
    border-style: solid;
    border-width: 1px;
    border-color:@HideSelection;
    color: @ControlText;
}
.avatarContainer{
    display: flex;
    margin-top: 6px;
    margin-bottom: 6px;   
}
.avatar{
    width: 40px;
    height: 40px;
    border-radius: 40px;
    border-style: solid;
    border-width: 1px;
    border-color: @HideSelection;
}
.field-container{
    display:flex;
    flex-direction:column;
    justify-content: space-between;
    flex-grow: 1;
    flex-basis: 150px;
    padding-left: 6px;
    padding-right: 6px;
}
.with-left-margin{
    margin-left: 10px;
}
.field-header{
    display:flex;
    justify-content: space-between;
}
.button{
    display: inline-block;
    padding: 10px;
    margin-left: 10px;
    color: gray;
    background-color: @Window;
    border-width: 1px;
    border-style: solid;
    border-color: @HideSelection;
    border-radius: 5px;
    text-align: center;
    align-self:center;
    width: 70px;
}
.hint{
    color: @DisabledText;
    font-size:7.5pt;
}
.button:hover {
    background-color: @DisabledText;
    color: @White;
    border-color: @DisabledControl;
}
.separator{
    width:100%;
    height:1px;
    background-color:@HideSelection;
}";


                template.Template = html.Replace("'", "\"");
                template.Styles = css.Replace("'", "\"");
                return template;

            }
            System.Data.DataTable createData(int rowsCount)
            {
                System.Data.DataTable table = new System.Data.DataTable();
                table.Columns.Add("refer", typeof(string));
                table.Columns.Add("iconname", typeof(string));
                table.Columns.Add("nazev", typeof(string));
                table.Columns.Add("suffix", typeof(string));
                table.Columns.Add("note", typeof(string));

                var images = _GetImages32();
                for (int i = 0; i < rowsCount; i++)
                {
                    string refer = (i + 1).ToString();
                    string iconname = Randomizer.GetItem(images);
                    string nazev = Randomizer.GetWord(true);
                    string suffix = Randomizer.GetSentence(1, 3, false);
                    string note = Randomizer.GetSentence(5, 9, true);
                    table.Rows.Add(refer, iconname, nazev, suffix, note);
                }
                return table;
            }
        }
        #endregion
        #region ListBox s typovou Template a typovými daty
        private void _PrepareListBoxTyped()
        {
            var template = createTemplate();
            var data = new List<ThreadListData>();

            __ThreadListBox = DxComponent.CreateDxListBox(300, 28, 544, 416, __MainPanel);
            __ThreadListBox.Templates.Add(template);
            __ThreadListBox.DataSource = data;
            __ThreadListBox.ItemAutoHeight = true;

            __ThreadListBox.ContextButtons.Add(new DevExpress.Utils.ContextItem() { Visibility = DevExpress.Utils.ContextItemVisibility.Visible, Size = new WinDraw.Size(24, 24) });
            __ThreadListBox.HorizontalScrollbar = true;
            __ThreadListBox.HorzScrollStep = 1;

            __ThreadListBox.CustomizeItem += __ThreadListBoxTyped_CustomizeItem;
            __ThreadListBox.CustomItemDisplayText += __ThreadListBoxTyped_CustomItemDisplayText;
            __ThreadListBox.CustomItemTemplate += __ThreadListBoxTyped_CustomItemTemplate;

            data.AddRange(createData(120));

            __Searcher = new DevExpress.XtraEditors.SearchControl();
            __Searcher.Bounds = new Rectangle(300, 6, 544, 20);
            __Searcher.Client = __ThreadListBox;
            __Searcher.Properties.NullValuePrompt = "Co byste chtěli najít?";
            __MainPanel.Controls.Add(__Searcher);


            DevExpress.XtraEditors.TableLayout.ItemTemplateBase createTemplate()
            {
                var template = new DevExpress.XtraEditors.TableLayout.ItemTemplateBase() { Name = "Main" };

                // Columns:
                var col0 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col0.Length.Value = 48d;
                template.Columns.Add(col0);

                var col1 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col1.Length.Value = 100d;
                template.Columns.Add(col1);

                var col2 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col2.Length.Value = 200d;
                template.Columns.Add(col2);

                // Rows:
                var row0 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
                row0.Length.Value = 20d;
                row0.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                template.Rows.Add(row0);

                var row1 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
                row1.Length.Value = 2d;
                row1.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                row1.AutoHeight = true;
                template.Rows.Add(row1);

                // Spans:
                var span0 = new DevExpress.XtraEditors.TableLayout.TableSpan() { ColumnIndex = 0, RowSpan = 2 };
                template.Spans.Add(span0);
                var span1 = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = 1, ColumnIndex = 1, ColumnSpan = 2 };
                template.Spans.Add(span1);

                // Elements:
                string resource1 = "images/xaf/action_simpleaction_32x32.png";
                var elem00 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 0, ImageToTextAlignment = DevExpress.XtraEditors.TileControlImageToTextAlignment.Right, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter, FieldName = "Text0", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight, Width = 35, Height = 40, Image = DxComponent.GetBitmapImage(resource1) };
                elem00.Appearance.Normal.FontStyleDelta = FontStyle.Bold;
                elem00.Appearance.Normal.FontSizeDelta = 3;
                elem00.Appearance.Normal.Options.UseFont = true;
                template.Elements.Add(elem00);

                var elem01 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 1, FieldName = "Text1", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 120, Height = 28 };
                elem01.Appearance.Normal.FontStyleDelta = FontStyle.Bold;
                template.Elements.Add(elem01);

                var elem02 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 2, FieldName = "Text2", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 240, Height = 28 };
                elem02.Appearance.Normal.FontStyleDelta = FontStyle.Regular;
                template.Elements.Add(elem02);

                var elem11 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 1, ColumnIndex = 1, FieldName = "Text3", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 360, Height = 20 };
                elem11.Appearance.Normal.FontStyleDelta = FontStyle.Italic;
                template.Elements.Add(elem11);

                return template;

            }
            List<ThreadListData> createData(int rowsCount)
            {
                List<ThreadListData> dataList = new List<ThreadListData>();
                var images = _GetImages32();
                for (int i = 0; i < rowsCount; i++)
                {
                    var dataItem = new ThreadListData()
                    {
                        Text0 = (i + 1).ToString(),
                        Text1 = Randomizer.GetWord(true),
                        Text2 = Randomizer.GetSentence(1, 3, false),
                        Text3 = Randomizer.GetSentence(5, 9, true),
                        IconName = Randomizer.GetItem(images)
                    };

                    dataList.Add(dataItem);
                }
                return dataList;
            }


        }

        private void __ThreadListBoxTyped_CustomItemTemplate(object sender, DevExpress.XtraEditors.CustomItemTemplateEventArgs e)
        {
        }

        private void __ThreadListBoxTyped_CustomItemDisplayText(object sender, DevExpress.XtraEditors.CustomItemDisplayTextEventArgs e)
        {
            
        }

        private void __ThreadListBoxTyped_CustomizeItem(object sender, DevExpress.XtraEditors.CustomizeTemplatedItemEventArgs e)
        {
            if (e.Value is ThreadListData data)
                e.TemplatedItem.Image = DxComponent.GetBitmapImage(data.IconName);
        }

        private DxListBoxControl __ThreadListBox;
        private DevExpress.XtraEditors.SearchControl __Searcher;

        internal class ThreadListData
        {
            public string Text0 { get; set; }
            public string Text1 { get; set; }
            public string Text2 { get; set; }
            public string Text3 { get; set; }
            public string IconName { get; set; }
        }
        #endregion
        #region ListBox s typovou Template a DataTable
        private void _PrepareListBoxTable()
        {
            var template = createTemplate();
            var data = createData(120);

            __ThreadListBox = DxComponent.CreateDxListBox(300, 28, 544, 416, __MainPanel);
            __ThreadListBox.Templates.Add(template);
            __ThreadListBox.DataSource = data;
            __ThreadListBox.ItemAutoHeight = true;

            __ThreadListBox.ContextButtons.Add(new DevExpress.Utils.ContextItem() { Visibility = DevExpress.Utils.ContextItemVisibility.Visible, Size = new WinDraw.Size(24, 24) });
            __ThreadListBox.HorizontalScrollbar = true;
            __ThreadListBox.HorzScrollStep = 1;

            __ThreadListBox.CustomItemTemplate += _ThreadListBoxTable_CustomItemTemplate;         // Vybere pro konkrétní prvek jednu z vícero šablon
            //  __ThreadListBox.CustomItemDisplayText += _ThreadListBoxTable_CustomItemDisplayText;   // Modifikuje text zobrazovaný v buňce => ale pozor, má smysl jen pro primitivní ListBox (jeden řádek = jeden text)
            __ThreadListBox.CustomizeItem += _ThreadListBoxTable_CustomizeItem;                   // Aktualizuje Image pro buňku = pro TemplateItem

            __Searcher = new DevExpress.XtraEditors.SearchControl();
            __Searcher.Bounds = new Rectangle(300, 6, 544, 20);
            __Searcher.Client = __ThreadListBox;
            __Searcher.Properties.NullValuePrompt = "Co byste chtěli najít?";
            __MainPanel.Controls.Add(__Searcher);


            DevExpress.XtraEditors.TableLayout.ItemTemplateBase createTemplate()
            {
                var template = new DevExpress.XtraEditors.TableLayout.ItemTemplateBase() { Name = "Main" };

                // Columns:
                var col0 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col0.Length.Value = 48d;
                template.Columns.Add(col0);

                var col1 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col1.Length.Value = 100d;
                template.Columns.Add(col1);

                var col2 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col2.Length.Value = 200d;
                template.Columns.Add(col2);

                var col3 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                col3.Length.Value = 40d;
                template.Columns.Add(col3);

                // Rows:
                var row0 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
                row0.Length.Value = 20d;
                row0.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                template.Rows.Add(row0);

                var row1 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
                row1.Length.Value = 2d;
                row1.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                row1.AutoHeight = true;
                template.Rows.Add(row1);

                // Spans:
                var span0 = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = 0, ColumnIndex = 0, RowSpan = 2 };
                template.Spans.Add(span0);
                var span1 = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = 1, ColumnIndex = 1, ColumnSpan = 2 };
                template.Spans.Add(span1);
                var span2 = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = 0, ColumnIndex = 3, RowSpan = 2 };
                template.Spans.Add(span2);

                // Elements:
                var elem00 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 0, FieldName = "refer", ImageToTextAlignment = DevExpress.XtraEditors.TileControlImageToTextAlignment.Right, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter, TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleRight, Width = 35, Height = 40, Name = "imagename" };
                elem00.Appearance.Normal.FontStyleDelta = FontStyle.Bold;
                elem00.Appearance.Normal.FontSizeDelta = 3;
                elem00.Appearance.Normal.Options.UseFont = true;
                template.Elements.Add(elem00);

                var elem01 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 1, FieldName = "nazev", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 120, Height = 28 };
                elem01.Appearance.Normal.FontStyleDelta = FontStyle.Bold;
                template.Elements.Add(elem01);

                var elem02 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 2, FieldName = "suffix", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 240, Height = 28 };
                elem02.Appearance.Normal.FontStyleDelta = FontStyle.Regular;
                template.Elements.Add(elem02);

                var elem11 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 1, ColumnIndex = 1, FieldName = "note", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft, Width = 360, Height = 20 };
                elem11.Appearance.Normal.FontStyleDelta = FontStyle.Italic;
                template.Elements.Add(elem11);

                var imag03 = "office2013/actions/add_32x32.png";
                var elem03 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 3, FieldName = null, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter, Width = 40, Height = 28 };
                template.Elements.Add(elem03);

                return template;
            }
            System.Data.DataTable createData(int rowsCount)
            {
                System.Data.DataTable table = new System.Data.DataTable();
                table.Columns.Add("refer", typeof(string));
                table.Columns.Add("iconname1", typeof(string));
                table.Columns.Add("nazev", typeof(string));
                table.Columns.Add("suffix", typeof(string));
                table.Columns.Add("note", typeof(string));
                table.Columns.Add("iconname2", typeof(string));
                // table.Columns.Add("photo", typeof(byte[]));
                table.Columns.Add("photo", typeof(Image));

                var images32 = _GetImages32();
                var images16 = _GetImages16();
                var photoNames = _GetPhotoNames();
                var hasPhotoNames = (photoNames != null && photoNames.Length > 0);
                for (int i = 0; i < rowsCount; i++)
                {
                    string refer = (i + 1).ToString();
                    string iconname1 = Randomizer.GetItem(images32);
                    string nazev = Randomizer.GetWord(true);
                    string suffix = Randomizer.GetSentence(1, 3, false);
                    string note = Randomizer.GetSentence(5, 9, true);
                    string iconname2 = Randomizer.GetItem(images16);
                    Image photo = null;
                    if (hasPhotoNames && Randomizer.IsTrue(25))
                        photo = loadImage(Randomizer.GetItem(photoNames));
                    table.Rows.Add(refer, iconname1, nazev, suffix, note, iconname2, photo);
                }
                return table;
            }

            Image loadImage(string fileName)
            {
                using (var stream = System.IO.File.OpenRead(fileName))
                    return Image.FromStream(stream);
            }
        }
        /// <summary>
        /// Událost je volána 1x per 1 řádek Listu v procesu jeho kreslení, jako příprava
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ThreadListBoxTable_CustomizeItem(object sender, DevExpress.XtraEditors.CustomizeTemplatedItemEventArgs e)
        {
            var imageName1 = "iconname1";
            var imageName2 = "iconname2";
            var photoName = "photo";
            if (e.Value is System.Data.DataRowView rowView)
            {
                if (rowView.Row.Table.Columns.Contains(imageName1))
                    e.TemplatedItem.Elements[0].Image = DxComponent.GetBitmapImage(rowView.Row[imageName1] as string);

                bool hasImage2 = false;
                if (rowView.Row.Table.Columns.Contains(photoName))
                {
                    if (rowView.Row[photoName] is Image image)
                    {
                        e.TemplatedItem.Elements[4].Image = image;
                        e.TemplatedItem.Elements[4].ImageOptions.ImageScaleMode = DevExpress.XtraEditors.TileItemImageScaleMode.Squeeze;

                        hasImage2 = true;
                    }
                }
                if (!hasImage2 && rowView.Row.Table.Columns.Contains(imageName2))
                {
                    string name = rowView.Row[imageName2] as string;
                    e.TemplatedItem.Elements[4].Image = DxComponent.GetBitmapImage(name, ResourceImageSizeType.Small);
                    e.TemplatedItem.Elements[4].ImageOptions.ImageScaleMode = DevExpress.XtraEditors.TileItemImageScaleMode.Squeeze;
                    hasImage2 = true;
                }
                if (!hasImage2)
                {
                    e.TemplatedItem.Elements[4].Image = null;
                }
            }
        }
        private void _ThreadListBoxTable_CustomItemTemplate(object sender, DevExpress.XtraEditors.CustomItemTemplateEventArgs e)
        {
            
        }

        private void _ThreadListBoxTable_CustomItemDisplayText(object sender, DevExpress.XtraEditors.CustomItemDisplayTextEventArgs e)
        {
            if (e.Value is System.Data.DataRowView rowView)
            {
                var item = e.Item;
                e.DisplayText = e.DisplayText + " ??";
            }
        }

        #endregion
        private DxPanelControl __MainPanel;
        private string[] _GetImages32()
        {
            string[] images = new string[]
            {
                "images/xaf/action_clear_32x32.png",
                "images/xaf/action_clear_settings_32x32.png",
                "images/xaf/action_clonemerge_clone_object_32x32.png",
                "images/xaf/action_clonemerge_merge_object_32x32.png",
                "images/xaf/action_close_32x32.png",
                "images/xaf/action_createdashboard_32x32.png",
                "images/xaf/action_dashboard_showdesigner_32x32.png",
                "images/xaf/action_debug_breakpoint_toggle_32x32.png",
                "images/xaf/action_debug_start_32x32.png",
                "images/xaf/action_delete_32x32.png",
                "images/xaf/action_deny_32x32.png",
                "images/xaf/action_editmodel_32x32.png",
                "images/xaf/action_exit_32x32.png",
                "images/xaf/action_grant_32x32.png",
                "images/xaf/action_chart_printing_preview_32x32.png",
                "images/xaf/action_chart_showdesigner_32x32.png",
                "images/xaf/action_chartdatavertical_32x32.png",
                "images/xaf/action_chooseskin_32x32.png",
                "images/xaf/action_navigation_history_back_32x32.png",
                "images/xaf/action_navigation_history_forward_32x32.png",
                "images/xaf/action_navigation_next_object_32x32.png",
                "images/xaf/action_navigation_previous_object_32x32.png",
                "images/xaf/action_redo_32x32.png",
                "images/xaf/action_refresh_32x32.png",
                "images/xaf/action_reload_32x32.png"
            };
            return images;
        }
        private string[] _GetImages16()
        {
            string[] images = new string[]
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
            return images;
        }
        /// <summary>
        /// Vrátí pole obsahující obsah souborů typu Fotografie
        /// </summary>
        /// <param name="minCount"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        private byte[][] _GetPhotos(int minCount = 10, int maxCount = 100)
        {
            var names = _GetPhotoNames(minCount, maxCount);
            if (names is null || Name.Length == 0) return new byte[0][];
            List<byte[]> result = new List<byte[]>();
            foreach (var  name in names)
                result.Add(System.IO.File.ReadAllBytes(name));
            return result.ToArray();
        }
        /// <summary>
        /// Vrátí jména souborů JPG z adresáře 'c:\DavidPrac\Images\Small'
        /// </summary>
        /// <param name="minCount"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        private string[] _GetPhotoNames(int minCount = 10, int maxCount = 100)
        {
            string path = @"c:\DavidPrac\Images\Small";
            if (!System.IO.Directory.Exists(path)) return new string[0];
            var files = System.IO.Directory.GetFiles(path, "*.*")
                .Where(n => isImage(n))
                .ToArray();
            int count = Randomizer.Rand.Next(minCount, maxCount + 1);
            return Randomizer.GetItems(count, files);

            bool isImage(string name)
            {
                string extn = System.IO.Path.GetExtension(name).ToLower();
                return (extn == ".jpg" || extn == ".jpeg" || extn == ".png" || extn == ".bmp" || extn == ".pcx" || extn == ".tif" || extn == ".gif");
            }
        }
        #endregion
        #region Threads
        private void _ThreadStart()
        {

        }
        private void _ThreadStop()
        { }

        #endregion


        #region DxSample

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DevExpress.XtraEditors.TableLayout.ItemTemplateBase itemTemplateBase1 = new DevExpress.XtraEditors.TableLayout.ItemTemplateBase();
            DevExpress.XtraEditors.TableLayout.TableColumnDefinition tableColumnDefinition1 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
            DevExpress.XtraEditors.TableLayout.TemplatedItemElement templatedItemElement1 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement();
            DevExpress.XtraEditors.TableLayout.TemplatedItemElement templatedItemElement2 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement();
            DevExpress.XtraEditors.TableLayout.TableRowDefinition tableRowDefinition1 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
            DevExpress.XtraEditors.TableLayout.TableRowDefinition tableRowDefinition2 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
            this.regularTemplateListBox = new DevExpress.XtraEditors.ListBoxControl();
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            ((System.ComponentModel.ISupportInitialize)(this.regularTemplateListBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.categoriesBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // regularTemplateListBox
            // 
            this.regularTemplateListBox.DataSource = this.categoriesBindingSource;
            this.regularTemplateListBox.DisplayMember = "Description";
            this.regularTemplateListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.regularTemplateListBox.ItemHeight = 100;
            this.regularTemplateListBox.Location = new System.Drawing.Point(2, 23);
            this.regularTemplateListBox.Name = "regularTemplateListBox";
            this.regularTemplateListBox.Size = new System.Drawing.Size(256, 543);
            this.regularTemplateListBox.TabIndex = 0;
            itemTemplateBase1.Columns.Add(tableColumnDefinition1);
            templatedItemElement1.FieldName = "Description";
            templatedItemElement1.ImageOptions.ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter;
            templatedItemElement1.ImageOptions.ImageScaleMode = DevExpress.XtraEditors.TileItemImageScaleMode.ZoomInside;
            templatedItemElement1.RowIndex = 1;
            templatedItemElement1.Text = "Description";
            templatedItemElement1.TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft;
            templatedItemElement2.Appearance.Normal.FontSizeDelta = 4;
            templatedItemElement2.Appearance.Normal.Options.UseFont = true;
            templatedItemElement2.FieldName = "Name";
            templatedItemElement2.ImageOptions.ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter;
            templatedItemElement2.ImageOptions.ImageScaleMode = DevExpress.XtraEditors.TileItemImageScaleMode.ZoomInside;
            templatedItemElement2.Text = "Name";
            templatedItemElement2.TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleLeft;
            itemTemplateBase1.Elements.Add(templatedItemElement1);
            itemTemplateBase1.Elements.Add(templatedItemElement2);
            itemTemplateBase1.Name = "Main";
            tableRowDefinition1.Length.Value = 30D;
            tableRowDefinition2.AutoHeight = true;
            tableRowDefinition2.Length.Value = 60D;
            itemTemplateBase1.Rows.Add(tableRowDefinition1);
            itemTemplateBase1.Rows.Add(tableRowDefinition2);
            this.regularTemplateListBox.Templates.Add(itemTemplateBase1);
            this.regularTemplateListBox.ValueMember = "CategoryID";
            // 
            // groupControl1
            // 
            this.groupControl1.Controls.Add(this.regularTemplateListBox);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupControl1.GroupStyle = DevExpress.Utils.GroupStyle.Light;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(260, 568);
            this.groupControl1.TabIndex = 2;
            this.groupControl1.Text = "Regular Template";
          
           
            ((System.ComponentModel.ISupportInitialize)(this.regularTemplateListBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.categoriesBindingSource)).EndInit();
            this.ResumeLayout(false);
        }


        private DevExpress.XtraEditors.ListBoxControl regularTemplateListBox;
        private System.Windows.Forms.BindingSource categoriesBindingSource;
        private DevExpress.XtraEditors.GroupControl groupControl1;

        #endregion




    }
}
