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
        #endregion
        #region Main Content
        /// <summary>
        /// Provede přípravu obsahu hlavního panelu <see cref="DxRibbonForm.DxMainPanel"/>. Panel je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected override void DxMainContentPrepare()
        {
            __MainPanel = DxComponent.CreateDxPanel(this.DxMainPanel, DockStyle.None, DevExpress.XtraEditors.Controls.BorderStyles.Office2003, 850, 450);

            __Threads = new List<ThreadListData>();

            var template = new DevExpress.XtraEditors.TableLayout.ItemTemplateBase() { Name = "Main" };

            var col0 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
            col0.Length.Value = 40d;
            template.Columns.Add(col0);

            var col1 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
            col1.Length.Value = 100d;
            template.Columns.Add(col1);

            var col2 = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
            col2.Length.Value = 200d;
            template.Columns.Add(col2);
            

            var row0 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
            row0.Length.Value = 20d;
            row0.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
            template.Rows.Add(row0);

            var row1 = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
            row1.Length.Value = 2d;
            row1.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
            row1.AutoHeight = true;
            template.Rows.Add(row1);

            var span0 = new DevExpress.XtraEditors.TableLayout.TableSpan() { ColumnIndex = 0, RowSpan = 2 };
            template.Spans.Add(span0);
            var span1 = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = 1, ColumnIndex = 1, ColumnSpan = 2 };
            template.Spans.Add(span1);

            string resource1 = "images/xaf/action_simpleaction_32x32.png";
            var elem00 = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement() { RowIndex = 0, ColumnIndex = 0, ImageToTextAlignment = DevExpress.XtraEditors.TileControlImageToTextAlignment.Right, ImageAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter, FieldName = "Text0", TextAlignment = DevExpress.XtraEditors.TileItemContentAlignment.MiddleCenter, Width = 35, Height = 40, Image = DxComponent.GetBitmapImage(resource1) };
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



            __ThreadListBox = DxComponent.CreateDxListBox(300, 28, 500, 410, __MainPanel);
            __ThreadListBox.Templates.Add(template);
            __ThreadListBox.DataSource = __Threads;
            __ThreadListBox.ItemAutoHeight = true;

            __Threads.Add(new ThreadListData() { Text0 = "1", Text1 = "T1 1", Text2 = "podrobný popisek t1", Text3 = Randomizer.GetSentence(5, 9, true) });
            __Threads.Add(new ThreadListData() { Text0 = "2", Text1 = "T1 2", Text2 = "podrobný popisek t2", Text3 = Randomizer.GetSentence(5, 9, true) });
            __Threads.Add(new ThreadListData() { Text0 = "3", Text1 = "T1 3", Text2 = "podrobný popisek t3", Text3 = Randomizer.GetSentence(5, 9, true) });
            __Threads.Add(new ThreadListData() { Text0 = "4", Text1 = "T1 4", Text2 = "podrobný popisek t4", Text3 = Randomizer.GetSentence(5, 9, true) });

        }
        private DxPanelControl __MainPanel;
        private DxListBoxControl __ThreadListBox;
        private List<ThreadListData> __Threads;
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




        internal class ThreadListData
        {
            public string Text0 { get; set; }
            public string Text1 { get; set; }
            public string Text2 { get; set; }
            public string Text3 { get; set; }
        }
    }
}
