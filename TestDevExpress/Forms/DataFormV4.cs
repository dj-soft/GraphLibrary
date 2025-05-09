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
using Noris.Clients.Win.Components.AsolDX.DxForm;
using Noris.Clients.Win.Components.AsolDX.DataForm;
using TestDevExpress.Components;

using DevExpress.XtraEditors;
using DevExpress.XtraLayout;

// using DevExpress.XtraLayout;
// using DevExpress.XtraLayout.Utils;

using DXR = DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Layout;


namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro testy komponenty <see cref="DxDataFormX"/>
    /// </summary>
    [RunTestForm(groupText: "Testovací okna", buttonText: "DataForm V4", buttonOrder: 14, buttonImage: "svgimages/spreadsheet/showcompactformpivottable.svg", buttonToolTip: "Otevře okno DataForm verze 4", tabViewToolTip: "Okno zobrazující nový DataForm")]
    public class DataFormV4 : DxRibbonForm  //, IControlInfoSource
    {
        protected override void DxRibbonPrepare()
        {
            var ribbonContent = new DataRibbonContent();
            ribbonContent.StatusBarItems.Add(new DataRibbonItem() { ItemType = RibbonItemType.Static, Text = "Obsah GDI" });
            this.DxRibbon.RibbonContent = ribbonContent;
        }
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            PrepareGridLayoutGitHub();
            // PrepareGridCards();
            // PrepareXtraLayout();
            // PrepareLayoutCustom();
            // PrepareGridLayout();
        }

        protected void PrepareXtraLayoutControl()
        {

            // https://docs.devexpress.com/WindowsForms/114577/controls-and-libraries/form-layout-managers
            // https://docs.devexpress.com/WindowsForms/11359/controls-and-libraries/application-ui-manager
            // https://docs.devexpress.com/WindowsForms/DevExpress.XtraLayout.LayoutRepositoryItem
            // https://docs.devexpress.com/WindowsForms/2170/controls-and-libraries/form-layout-managers/layout-and-data-layout-controls/tabbed-group

            // https://www.youtube.com/watch?v=qwjvR4tX790

            var start = DateTime.Now;

            __Layout = new DevExpress.XtraLayout.LayoutControl() { Dock = DockStyle.Fill };
            __Layout.SuspendLayout();
            int cnt = (new Random()).Next(50, 200);
            for (int q = 0; q < cnt; q++)
            {
                var repoItem = new DXR.RepositoryItemTextEdit();
                repoItem.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat;
                repoItem.NullText = "xxxxxxxxxxxx";

                var layoutItem = new DevExpress.XtraLayout.LayoutRepositoryItem(repoItem);
                layoutItem.Name = "RepoItem" + q.ToString();
                layoutItem.Text = "RepositoryItem " + q.ToString() + ":";
                layoutItem.Size = new WinDraw.Size(360, 20);
                layoutItem.EditValue = Randomizer.GetSentence(2, 6, true);

                __Layout.Root.Add(layoutItem);
            }
            __Layout.ResumeLayout(false);
            this.DxMainPanel.Controls.Add(__Layout);

            var stop = DateTime.Now;
            var time = stop - start;
            var time1 = time.TotalMilliseconds / (double)cnt;

            MainAppForm.CurrentInstance.StatusBarText = $"Create XtraLayout.LayoutControl {cnt} items in {time.TotalSeconds} secs;   1 item in {time1} milisecond. ";
        }
        DevExpress.XtraLayout.LayoutControl __Layout;


        protected void PrepareGridCards()
        {
            __GridControl = new DevExpress.XtraGrid.GridControl() { Dock = DockStyle.Fill };
            __CardView = new DevExpress.XtraGrid.Views.Card.CardView(__GridControl);
            __GridControl.MainView = __CardView;

            // Přizpůsobení karet
            __CardView.CardWidth = 300; // Nastavení šířky karet
            __CardView.CardCaptionFormat = "Záznam: {0}"; // Formát nadpisu karty

            // Přidání sloupců
            __CardView.Columns.Add(new DevExpress.XtraGrid.Columns.GridColumn() { FieldName = "FirstName", Caption = "Jméno" });
            __CardView.Columns.Add(new DevExpress.XtraGrid.Columns.GridColumn() { FieldName = "LastName", Caption = "Příjmení" });
            __CardView.Columns.Add(new DevExpress.XtraGrid.Columns.GridColumn() { FieldName = "Address", Caption = "Address" });

            // Datový zdroj
            __GridControl.DataSource = _CreateDataTable(24);     // Vaše metoda pro načtení dat        
            this.DxMainPanel.Controls.Add(__GridControl);
        }

        protected void PrepareGridLayoutGitHub()
        {
            // Vytvoření GridControl
            var gridControl = new GridControl
            {
                Dock = DockStyle.Fill
            };

            // Vytvoření LayoutView
            var layoutView = new LayoutView(gridControl);
            gridControl.MainView = layoutView;
            gridControl.ViewCollection.Add(layoutView);

            // Nastavení LayoutView
            layoutView.OptionsView.ViewMode = LayoutViewMode.SingleRecord; // Zobrazení jednoho záznamu najednou
            layoutView.OptionsBehavior.Editable = true; // Povolení úprav
            layoutView.CardMinSize = new WinDraw.Size(400, 300); // Minimální velikost karty

            // Definice TemplateCard (rozložení záznamu)
            var templateCard = new DevExpress.XtraGrid.Views.Layout.LayoutViewCard();
            layoutView.TemplateCard = templateCard;

            // Vytvoření datového zdroje
            gridControl.DataSource = _CreateDataTable(24); // GetData();

            // Přidání sloupců

            // Obrázek
            var imageColumn = new DevExpress.XtraGrid.Columns.LayoutViewColumn
            {
                FieldName = "Photo1",
                Caption = "Obrázek",
                LayoutViewField = new LayoutViewField() { TextVisible = false } // Obrázek bez popisku
            };
            var repositoryImage = new RepositoryItemPictureEdit();
            gridControl.RepositoryItems.Add(repositoryImage);
            imageColumn.ColumnEdit = repositoryImage;
            layoutView.Columns.Add(imageColumn);

            // Textové pole 1 (krátký text)
            var shortTextColumn = new DevExpress.XtraGrid.Columns.LayoutViewColumn
            {
                FieldName = "FirstName",
                Caption = "Krátký text"
            };
            layoutView.Columns.Add(shortTextColumn);

            // Textové pole 2 (dlouhý text)
            var longTextColumn = new DevExpress.XtraGrid.Columns.LayoutViewColumn
            {
                FieldName = "SurName",
                Caption = "Dlouhý text"
            };
            layoutView.Columns.Add(longTextColumn);

            // ComboBox
            var comboBoxColumn = new DevExpress.XtraGrid.Columns.LayoutViewColumn
            {
                FieldName = "ComboBox",
                Caption = "Výběr"
            };
            var repositoryComboBox = new RepositoryItemComboBox();
            repositoryComboBox.Items.AddRange(new[] { "Možnost 1", "Možnost 2", "Možnost 3" });
            gridControl.RepositoryItems.Add(repositoryComboBox);
            comboBoxColumn.ColumnEdit = repositoryComboBox;
            layoutView.Columns.Add(comboBoxColumn);


            this.DxMainPanel.Controls.Add(gridControl);
        }

        protected void PrepareGridLayout()
        {
            /*
            https://docs.devexpress.com/WindowsForms/114636/controls-and-libraries/data-grid/views
            https://docs.devexpress.com/WindowsForms/113894/controls-and-libraries/data-grid/getting-started-with-data-grid-and-views
            https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.Views.Layout.LayoutView

            */
            var start = DateTime.Now;

            __GridControl = new DevExpress.XtraGrid.GridControl() { Dock = DockStyle.Fill };
            __GridControl.SuspendLayout();

            __GridLayout = new DevExpress.XtraGrid.Views.Layout.LayoutView(__GridControl);
            __GridControl.MainView = __GridLayout;
            __GridLayout.OptionsBehavior.AutoPopulateColumns = false;

            __GridControl.DataSource = _CreateDataTable(12);

            PrepareGridLayoutRegular();

            __GridControl.ResumeLayout(false);
            this.DxMainPanel.Controls.Add(__GridControl);

            var stop = DateTime.Now;
            var time = stop - start;

            MainAppForm.CurrentInstance.StatusBarText = $"Create XtraGrid.GridControl in {time.TotalSeconds} secs";

        }

        protected void PrepareGridLayoutTable()
        {
            // Create columns.
            var colFirstName = __GridLayout.Columns.AddVisible("FirstName");
            var colLastName = __GridLayout.Columns.AddVisible("LastName");
            var colAddress = __GridLayout.Columns.AddVisible("Address");
            var colCity = __GridLayout.Columns.AddVisible("City");
            var colCountry = __GridLayout.Columns.AddVisible("Country");
            var colPhoto = __GridLayout.Columns.AddVisible("Photo");

            // Access corresponding card fields.
            var fieldFirstName = colFirstName.LayoutViewField;
            var fieldLastName = colLastName.LayoutViewField;
            var fieldAddress = colAddress.LayoutViewField;
            var fieldCity = colCity.LayoutViewField;
            var fieldCountry = colCountry.LayoutViewField;
            var fieldPhoto = colPhoto.LayoutViewField;

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
            groupAddress.TextLocation = DevExpress.Utils.Locations.Top;
            groupAddress.PaintAppearanceItemCaption.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

            groupAddress.LayoutMode = DevExpress.XtraLayout.Utils.LayoutMode.Regular;
            groupAddress.DefaultLayoutType = DevExpress.XtraLayout.Utils.LayoutType.Horizontal;
            groupAddress.ExpandButtonVisible = true;
            groupAddress.ExpandButtonMode = DevExpress.Utils.Controls.ExpandButtonMode.Normal;

            groupAddress.OptionsTableLayoutGroup.ColumnDefinitions.Clear();
            groupAddress.OptionsTableLayoutGroup.ColumnDefinitions.Add(new DevExpress.XtraLayout.ColumnDefinition() { SizeType = SizeType.Absolute, Width = 320 });

            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Clear();
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });


            // Move the Address, City and Country fields to this group.
            var itemAdress = groupAddress.AddItem(fieldAddress);
            var itemCity = groupAddress.AddItem(fieldCity);
            var itemCountry = groupAddress.AddItem(fieldCountry);

            itemAdress.Location = new Point(35, 12);
            itemAdress.Size = new WinDraw.Size(250, 30);
            itemAdress.OptionsTableLayoutItem.ColumnIndex = 0;
            itemAdress.OptionsTableLayoutItem.RowIndex = 0;

            itemCity.OptionsTableLayoutItem.ColumnIndex = 0;
            itemCity.OptionsTableLayoutItem.RowIndex = 1;

            itemCountry.OptionsTableLayoutItem.ColumnIndex = 0;
            itemCountry.OptionsTableLayoutItem.RowIndex = 2;



            /*
            fieldCity.Move(fieldAddress, DevExpress.XtraLayout.Utils.InsertType.Bottom);
            fieldCountry.Move(fieldCity, DevExpress.XtraLayout.Utils.InsertType.Bottom);
            fieldCountry.Size = new WinDraw.Size(300, 20);
            */
            __GridLayout.TemplateCard.AddGroup(groupAddress, fieldLastName, DevExpress.XtraLayout.Utils.InsertType.Bottom);

            // Assign editors to card fields.
            var riPictureEdit = __GridControl.RepositoryItems.Add("PictureEdit") as DXR.RepositoryItemPictureEdit;
            riPictureEdit.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            colPhoto.ColumnEdit = riPictureEdit;

            // Customize card field options.
            colFirstName.Caption = "First Name";
            colLastName.Caption = "Last Name";
            // Set the card's minimum size.
            __GridLayout.CardMinSize = new WinDraw.Size(250, 180);

            fieldPhoto.TextVisible = false;
            fieldPhoto.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            fieldPhoto.MaxSize = fieldPhoto.MinSize = new WinDraw.Size(150, 150);

        }

        protected void PrepareGridLayoutFlow()
        { }

        protected void PrepareGridLayoutRegular()
        {
            // Create columns.
            var colFirstName = __GridLayout.Columns.AddVisible("FirstName");
            var colLastName = __GridLayout.Columns.AddVisible("LastName");
            var colAddress = __GridLayout.Columns.AddVisible("Address");
            var colCity = __GridLayout.Columns.AddVisible("City");
            var colCountry = __GridLayout.Columns.AddVisible("Country");
            var colPhoto = __GridLayout.Columns.AddVisible("Photo");

            // Access corresponding card fields.
            var fieldFirstName = colFirstName.LayoutViewField;
            var fieldLastName = colLastName.LayoutViewField;
            var fieldAddress = colAddress.LayoutViewField;
            var fieldCity = colCity.LayoutViewField;
            var fieldCountry = colCountry.LayoutViewField;
            var fieldPhoto = colPhoto.LayoutViewField;

         //   __GridLayout.lay

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
            groupAddress.TextLocation = DevExpress.Utils.Locations.Top;
            groupAddress.PaintAppearanceItemCaption.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            
            groupAddress.LayoutMode = DevExpress.XtraLayout.Utils.LayoutMode.Regular;
            groupAddress.DefaultLayoutType = DevExpress.XtraLayout.Utils.LayoutType.Horizontal;
            groupAddress.ExpandButtonVisible = true;
            groupAddress.ExpandButtonMode = DevExpress.Utils.Controls.ExpandButtonMode.Normal;

            groupAddress.OptionsTableLayoutGroup.ColumnDefinitions.Clear();
            groupAddress.OptionsTableLayoutGroup.ColumnDefinitions.Add(new DevExpress.XtraLayout.ColumnDefinition() { SizeType = SizeType.Absolute, Width = 320 });

            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Clear();
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });
            groupAddress.OptionsTableLayoutGroup.RowDefinitions.Add(new DevExpress.XtraLayout.RowDefinition() { SizeType = SizeType.Absolute, Height = 25 });


            // Move the Address, City and Country fields to this group.
            var itemAdress = groupAddress.AddItem(fieldAddress);
            var itemCity = groupAddress.AddItem(fieldCity);
            var itemCountry = groupAddress.AddItem(fieldCountry);

            itemAdress.Location = new Point(35, 12);
            itemAdress.Size = new WinDraw.Size(250, 30);
            itemAdress.OptionsTableLayoutItem.ColumnIndex = 0;
            itemAdress.OptionsTableLayoutItem.RowIndex = 0;

            itemCity.OptionsTableLayoutItem.ColumnIndex = 0;
            itemCity.OptionsTableLayoutItem.RowIndex = 1;

            itemCountry.OptionsTableLayoutItem.ColumnIndex = 0;
            itemCountry.OptionsTableLayoutItem.RowIndex = 2;



            /*
            fieldCity.Move(fieldAddress, DevExpress.XtraLayout.Utils.InsertType.Bottom);
            fieldCountry.Move(fieldCity, DevExpress.XtraLayout.Utils.InsertType.Bottom);
            fieldCountry.Size = new WinDraw.Size(300, 20);
            */
            __GridLayout.TemplateCard.AddGroup(groupAddress, fieldLastName, DevExpress.XtraLayout.Utils.InsertType.Bottom);

            // Assign editors to card fields.
            var riPictureEdit = __GridControl.RepositoryItems.Add("PictureEdit") as DXR.RepositoryItemPictureEdit;
            riPictureEdit.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            colPhoto.ColumnEdit = riPictureEdit;

            // Customize card field options.
            colFirstName.Caption = "First Name";
            colLastName.Caption = "Last Name";
            // Set the card's minimum size.
            __GridLayout.CardMinSize = new WinDraw.Size(250, 180);

            fieldPhoto.TextVisible = false;
            fieldPhoto.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            fieldPhoto.MaxSize = fieldPhoto.MinSize = new WinDraw.Size(150, 150);


        }
        DevExpress.XtraGrid.GridControl __GridControl;
        DevExpress.XtraGrid.Views.Layout.LayoutView __GridLayout;
        DevExpress.XtraGrid.Views.Card.CardView __CardView;

        private void InitializeLayoutControlAA()
        {
            // Vytvoření LayoutControl
            var layoutControl = new LayoutControl
            {
                Dock = DockStyle.Fill
            };

            // Zakázání přizpůsobení na úrovni celého LayoutControl
  //          layoutControl.OptionsCustomization.AllowCustomize = false;

            // Vytvoření hlavní skupiny
            var layoutGroup = new LayoutControlGroup
            {
                Text = "Pevná skupina"
            };

            // Zakázání přizpůsobení dětí v této skupině
   //         layoutGroup.OptionsTableLayoutGroup.AllowCustomizeChildren = false;

            // Přidání skupiny do LayoutControl
            layoutControl.Root = layoutGroup;

            // TextBox 1
            var textBox1 = new TextEdit
            {
                Name = "TextBox1",
                Text = "Text 1"
            };

            var layoutItem1 = new LayoutControlItem
            {
                Control = textBox1,
                Text = "Pole 1"
            };

            // Zakázání přizpůsobení pro konkrétní prvek
    //        layoutItem1.OptionsTableLayoutItem.AllowCustomize = false;

            layoutGroup.AddItem(layoutItem1);

            // Přidání LayoutControl do formuláře
            this.Controls.Add(layoutControl);
        }
        private void InitializeLayoutControlAB()
        {
            // Vytvoření LayoutControl
            var layoutControl = new LayoutControl
            {
                Dock = DockStyle.Fill,
                AllowCustomization = false // Zakázání přizpůsobení na úrovni LayoutControl
            };

            // Vytvoření hlavní skupiny (LayoutControlGroup)
            var layoutGroup = new LayoutControlGroup
            {
                Text = "Pevná skupina",
                CustomizationFormText = "Skupina není upravitelná" // Text v panelu přizpůsobení
            };

            // Přidání skupiny do LayoutControl
            layoutControl.Root = layoutGroup;

            // TextBox 1
            var textBox1 = new TextEdit
            {
                Name = "TextBox1",
                Text = "Text 1"
            };

            var layoutItem1 = new LayoutControlItem
            {
                Control = textBox1,
                Text = "Pole 1",
                CustomizationFormText = "Prvek není upravitelný" // Text v panelu přizpůsobení
            };

            layoutGroup.AddItem(layoutItem1);

            // Přidání LayoutControl do formuláře
            this.Controls.Add(layoutControl);
        }


        protected void PrepareLayoutCustom()
        {

            // Vytvoření LayoutControl
            var layoutControl = new LayoutControl
            {
                Dock = DockStyle.Fill
            };
            layoutControl.AllowCustomization = false;

            layoutControl.OptionsView.AutoSizeInLayoutControl = AutoSizeModes.UseMinAndMaxSize;
            layoutControl.OptionsView.FitControlsToDisplayAreaHeight = false;
            layoutControl.OptionsView.FitControlsToDisplayAreaWidth = false;



            // Vytvoření hlavní skupiny (LayoutControlGroup)
            var layoutGroup = new LayoutControlGroup
            {
                Text = "Pevná skupina",
                // SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom, // Nastavení vlastního omezení velikosti
                MinSize = new System.Drawing.Size(200, 100), // Minimální velikost
                MaxSize = new System.Drawing.Size(800, 100)  // Maximální velikost
            };
            layoutGroup.Size = new WinDraw.Size(450, 100);
            layoutGroup.LayoutMode = DevExpress.XtraLayout.Utils.LayoutMode.Regular;
            layoutGroup.TextLocation = DevExpress.Utils.Locations.Left;

            // layoutGroup.                 OptionsTableLayoutGroup.AllowCustomizeChildren: Když nastavíte false, zakážete přizpůsobení prvků uvnitř této konkrétní skupiny.
            layoutControl.Root = layoutGroup;

            // TextBox 1
            var textBox1 = new TextEdit
            {
                Name = "TextBox1",
                Text = "Text 1"
            };
            var layoutItem1 = new LayoutControlItem
            {
                Control = textBox1,
                Text = "Pole 1",
                SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom,

            };
            layoutItem1.Size = new System.Drawing.Size(150, 24); // Vlastní šířka a výška
            layoutItem1.Location = new System.Drawing.Point(0, 0); // Souřadnice
            layoutGroup.AddItem(layoutItem1);

            // TextBox 2
            var textBox2 = new TextEdit
            {
                Name = "TextBox2",
                Text = "Text 2"
            };
            var layoutItem2 = new LayoutControlItem
            {
                Control = textBox2,
                Text = "Pole 2 krátké",
                SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom,
            };
            layoutItem2.Size = new System.Drawing.Size(200, 24); // Vlastní šířka a výška
            layoutItem2.Location = new System.Drawing.Point(0, 30); // Souřadnice
            layoutGroup.AddItem(layoutItem2);

            // TextBox 3
            var textBox3 = new TextEdit
            {
                Name = "TextBox3",
                Text = "Text 3"
            };
            var layoutItem3 = new LayoutControlItem
            {
                Control = textBox3,
                Text = "Pole 3 s labelem dlouhé",
                SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom,
            };
            layoutItem3.Size = new System.Drawing.Size(250, 24); // Vlastní šířka a výška
            layoutItem3.Location = new System.Drawing.Point(0, 60); // Souřadnice
            layoutGroup.AddItem(layoutItem3);

            // Přidání LayoutControl do formuláře
            this.DxMainPanel.Controls.Add(layoutControl);





            /*


            // Vytvoření hlavní skupiny (LayoutControlGroup)
            var layoutGroup = new LayoutControlGroup
            {
                Text = "Hlavní skupina"
            };
            layoutControl.Root = layoutGroup;

            // TextBox (TextEdit)
            var textBox = new TextEdit
            {
                Name = "TextBox1",
                Text = "Text 1"
            };

            // Vytvoření LayoutControlItem pro TextBox
            var layoutItem = new LayoutControlItem
            {
                Control = textBox,
                Text = "Pole 1",
                SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom, // Povolení vlastních rozměrů
                MinSize = new System.Drawing.Size(200, 0), // Minimální šířka 200 px
                MaxSize = new System.Drawing.Size(300, 0)  // Maximální šířka 300 px
            };

            layoutGroup.AddItem(layoutItem); // Přidání položky do skupiny
            */
        }

        private System.Data.DataTable _CreateDataTable(int rowsCount)
        {
            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add("FirstName", typeof(string));
            table.Columns.Add("LastName", typeof(string));
            table.Columns.Add("Address", typeof(string));
            table.Columns.Add("City", typeof(string));
            table.Columns.Add("Country", typeof(string));
            table.Columns.Add("Wage1", typeof(decimal));
            table.Columns.Add("Wage2", typeof(decimal));
            table.Columns.Add("Photo", typeof(Image));

            var firstNames = new string[] { "Oliver", "Amelia", "Jack", "Olivia", "Harry", "Isla", "Jacob", "Emily", "Charlie", "Poppy", "Thomas", "Ava", "George", "Isabella", "Oscar", "Jessica", "James", "Lily", "William", "Sophie" };
            var surNames = new string[] { "SMITH", "JONES", "WILLIAMS", "TAYLOR", "BROWN", "DAVIES", "EVANS", "WILSON", "THOMAS", "JOHNSON", "ROBERTS", "ROBINSON", "THOMPSON", "WRIGHT", "WALKER", "WHITE", "EDWARDS", "HUGHES", "GREEN", "HALL", "LEWIS", "HARRIS", "CLARKE", "PATEL", "JACKSON", "WOOD", "TURNER", "MARTIN", "COOPER", "HILL", "WARD", "MORRIS", "MOORE", "CLARK", "LEE", "KING", "BAKER", "HARRISON", "MORGAN", "ALLEN", "JAMES", "SCOTT", "PHILLIPS", "WATSON", "DAVIS", "PARKER", "PRICE", "BENNETT", "YOUNG", "GRIFFITHS", "MITCHELL", "KELLY", "COOK", "CARTER", "RICHARDSON", "BAILEY", "COLLINS", "BELL", "SHAW", "MURPHY", "MILLER", "COX", "RICHARDS" };
            var cities = new string[] { "New York", "Los Angeles ", "Chicago ", "Houston ", "Phoenix ", "San Antonio ", "San Diego ", "Dallas ", "San Jose ", "Austin ", "Fort Worth ", "Columbus ", "Charlotte ", "San Francisco[g] ", "Seattle ", "Denver[h] ", "Washington[j] ", "Nashville[i] ", "Oklahoma City ", "El Paso ", "Boston ", "Portland ", "Las Vegas ", "Detroit ", "Memphis ", "Louisville[k] ", "Baltimore[l] ", "Milwaukee ", "Albuquerque ", "Tucson ", "Fresno ", "Sacramento ", "Kansas City ", "Mesa ", "Atlanta ", "Omaha ", "Colorado Springs ", "Raleigh ", "Long Beach ", "Virginia Beach[l] ", "Miami ", "Oakland ", "Minneapolis ", "Tulsa ", "Bakersfield ", "Wichita ", "Arlington ", "Aurora ", "Tampa ", "New Orleans[m] ", "Cleveland ", "Honolulu[n] ", "Anaheim ", "Lexington[o] ", "Stockton ", "Corpus Christi ", "Henderson ", "Riverside ", "Newark ", "Saint Paul ", "Santa Ana ", "Cincinnati ", "Irvine ", "Orlando ", "Pittsburgh ", "St. Louis[l] ", "Greensboro ", "Jersey City ", "Anchorage[p] ", "Lincoln ", "Plano ", "Durham ", "Buffalo ", "Chandler ", "Chula Vista ", "Toledo ", "Madison ", "Gilbert ", "Reno ", "Fort Wayne ", "North Las Vegas ", "St. Petersburg ", "Lubbock ", "Irving ", "Laredo ", "Winston-Salem ", "Chesapeake[l] ", "Glendale ", "Garland ", "Scottsdale ", "Norfolk[l] ", "Boise[q] ", "Fremont ", "Spokane ", "Santa Clarita ", "Baton Rouge[r] ", "Richmond[l] ", "Hialeah ", "San Bernardino ", "Tacoma ", "Modesto ", "Huntsville ", "Des Moines ", "Yonkers ", "Rochester ", "Moreno Valley ", "Fayetteville ", "Fontana ", "Columbus[s] ", "Worcester ", "Port St. Lucie ", "Little Rock ", "Augusta[t] ", "Oxnard ", "Birmingham ", "Montgomery ", "Frisco ", "Amarillo ", "Salt Lake City ", "Grand Rapids ", "Huntington Beach ", "Overland Park ", "Glendale ", "Tallahassee ", "Grand Prairie ", "McKinney ", "Cape Coral ", "Sioux Falls ", "Peoria ", "Providence ", "Vancouver ", "Knoxville ", "Akron ", "Shreveport ", "Mobile ", "Brownsville ", "Newport News[l] ", "Fort Lauderdale ", "Chattanooga ", "Tempe ", "Aurora ", "Santa Rosa ", "Eugene ", "Elk Grove ", "Salem ", "Ontario ", "Cary ", "Rancho Cucamonga ", "Oceanside ", "Lancaster ", "Garden Grove ", "Pembroke Pines ", "Fort Collins ", "Palmdale ", "Springfield ", "Clarksville ", "Salinas ", "Hayward ", "Paterson ", "Alexandria[l] ", "Macon[u] ", "Corona ", "Kansas City[v] ", "Lakewood ", "Springfield ", "Sunnyvale ", "Jackson" };
            var countries = new string[] { "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming", "District of Columbia", "American Samoa", "Guam", "Northern Mariana Islands", "Puerto Rico", "United States Minor Outlying Islands", "Virgin Islands, U.S.", "Albania", "Andorra", "Armenia", "Austria", "Azerbaijan", "Belarus", "Belgium", "Bosnia and Herzegovina", "Bulgaria", "Croatia", "Cyprus", "Czechia", "Denmark", "Estonia", "Finland", "France", "Georgia", "Germany", "Greece", "Holy See", "Hungary", "Iceland", "Ireland", "Italy", "Kosovo", "Latvia", "Liechtenstein", "Lithuania", "Luxembourg", "Malta", "Moldova", "Monaco", "Montenegro", "Netherlands", "North Macedonia", "Norway", "Poland", "Portugal", "Romania", "Russia", "San Marino", "Serbia", "Slovakia", "Slovenia", "Spain", "Sweden", "Switzerland", "Turkey (Türkiye)", "Ukraine", "United Kingdom" };
            var adrSuffix = new string[] { "", "A", "", "B", "", "C", "", "/2", "", "D", "", "/x", "", "E", "", "/y", "", "F" };
            var oldWordBook = Randomizer.ActiveWordBook;
            Randomizer.ActiveWordBook = Randomizer.WordBookType.CampOfSaints;

            for (int r = 0; r < rowsCount; r++)
            {
                table.Rows.Add(
                    Randomizer.GetItem(firstNames),
                    Randomizer.GetItem(surNames),
                    Randomizer.GetSentence(1, 3, false) + ", " + Randomizer.Rand.Next(16, 950).ToString() + Randomizer.GetItem(adrSuffix),
                    Randomizer.GetItem(cities),
                    Randomizer.GetItem(countries),
                    Randomizer.Rand.Next(15000, 95000),
                    Randomizer.Rand.Next(15000, 95000),
                    getRandomImage()
                    );
            }
            Randomizer.ActiveWordBook = oldWordBook;

            return table;

            Image getRandomImage()
            {
                int index = Randomizer.Rand.Next(14);
                switch (index)
                {
                    case 0: return global::TestDevExpress.Properties.Resources._16128___Nimhue;
                    case 1: return global::TestDevExpress.Properties.Resources._18127___Incantation;
                    case 2: return global::TestDevExpress.Properties.Resources._30180___damnation;
                    case 3: return global::TestDevExpress.Properties.Resources._32305___VIOLET_EYES;
                    case 4: return global::TestDevExpress.Properties.Resources._34204___ASIAN_ELF;
                    case 5: return global::TestDevExpress.Properties.Resources._36819___Frost_Witch;
                    case 6: return global::TestDevExpress.Properties.Resources._38717___Dihla;
                    case 7: return global::TestDevExpress.Properties.Resources._38737___Icy_Dreams;
                    case 8: return global::TestDevExpress.Properties.Resources._53252___Elven_Youth;
                    case 9: return global::TestDevExpress.Properties.Resources._54126___woman;
                    case 10: return global::TestDevExpress.Properties.Resources._54830___woman;
                    case 11: return global::TestDevExpress.Properties.Resources._57257___Marganna;
                    case 12: return global::TestDevExpress.Properties.Resources._6892___Triffidia_Beauty;
                    default: return null;
                }
            }
        }

    }
}
