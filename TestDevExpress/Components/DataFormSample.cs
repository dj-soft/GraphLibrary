using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

using Noris.Clients.Win.Components.AsolDX;
using Noris.Clients.Win.Components.AsolDX.DataForm;

namespace TestDevExpress.Components
{
    #region class DxDataFormSamples : Zdroj testovacích dat
    /// <summary>
    /// Třída, která generuje testovací předpisy a data pro testy <see cref="DxDataForm"/>
    /// </summary>
    public class DxDataFormSamples
    {
        /// <summary>
        /// Vrací true pro povolený SampleId
        /// </summary>
        /// <param name="sampleId"></param>
        /// <returns></returns>
        public static bool AllowedSampled(int sampleId)
        {
            return (sampleId == 10 || sampleId == 20 || sampleId == 30 || sampleId == 40 || sampleId == 101 || sampleId == 102 || sampleId == 103 || sampleId == 104);
        }
        /// <summary>
        /// Vytvoří a vrátí data pro definici DataFormu
        /// </summary>
        /// <param name="sampleId"></param>
        /// <param name="texts"></param>
        /// <param name="tooltips"></param>
        /// <returns></returns>
        public static IDataForm CreateSampleDefinition(int sampleId, string[] texts, string[] tooltips)
        {
            if (_Random is null) _Random = new System.Random();

            List<IDataFormPage> pages = new List<IDataFormPage>();

            switch (sampleId)
            {
                case 10:
                    pages.Add(CreateSamplePage(10, texts, tooltips, 60, "Základní stránka", "Obsahuje běžné informace",
                        0, 28, true, 24, 4, 12, 5, 6, new int[] { 140, 260, 40, 300, 120 }));
                    break;
                case 20:
                    pages.Add(CreateSamplePage(20, texts, tooltips, 2000, "Základní stránka", "Obsahuje běžné informace",
                        0, 28, true, 24, 2, 4, 2, 2, new int[] { 80, 150, 80, 60, 100, 120, 160, 40, 120, 180, 80, 40, 60, 250 }));
                    pages.Add(CreateSamplePage(21, texts, tooltips, 120, "Doplňková stránka", "Obsahuje další málo používané informace",
                        0, 28, true, 24, 2, 32, 2, 2, new int[] { 250, 250, 60, 250, 250, 60, 250 }));
                    break;
                case 30:
                    pages.Add(CreateSamplePage(30, texts, tooltips, 500, "Základní stránka", "Obsahuje běžné informace",
                        0, 28, true, 0, 0, 7, 6, 10, new int[] { 250, 250, 60, 250, 250, 60, 250 }));
                    pages.Add(CreateSamplePage(31, texts, tooltips, 70, "Sklady", null,
                        1, 26, false, 0, 0, 16, 4, 4, new int[] { 100, 75, 120, 100 }));
                    pages.Add(CreateSamplePage(32, texts, tooltips, 15, "Faktury", null,
                        1, 26, false, 0, 0, 12, 1, 1, new int[] { 70, 70, 70, 70 }));
                    pages.Add(CreateSamplePage(33, texts, tooltips, 25, "Zaúčtování", null,
                        1, 26, false, 0, 0, 8, 6, 10, new int[] { 400, 125, 75, 100 }));
                    pages.Add(CreateSamplePage(34, texts, tooltips, 480, "Výrobní čísla fixní zalomení", null,
                        3, 26, true, 0, 0, 5, 5, 5, new int[] { 100, 100, 70 }));
                    pages.Add(CreateSamplePage(35, texts, tooltips, 480, "Výrobní čísla automatické zalomení", null,
                        3, 26, true, 0, 0, 5, 5, 5, new int[] { 100, 100, 70 }));
                    break;
                case 40:
                    pages.Add(CreateSamplePage(40, texts, tooltips, 1, "Základní stránka", "Obsahuje běžné informace",
                        0, 0, false, 0, 0, 1, 5, 1, new int[] { 140, 260, 40, 300, 120, 80, 120, 80, 80, 80, 80, 250, 40, 250, 40, 250, 40, 250, 40, 250, 40 }));
                    break;


                case 101:
                case 102:
                case 103:
                case 104:
                    AddSamplePage10x(sampleId, pages);
                    break;
            }

            DataForm form = new DataForm() { Pages = pages };
            return form;
        }
        private static System.Random _Random;
        #region Generické vzorky
        /// <summary>
        /// Vytvoří, naplní a vrátí stránku daného druhu
        /// </summary>
        /// <param name="sampleId"></param>
        /// <param name="texts"></param>
        /// <param name="tooltips"></param>
        /// <param name="rowCount"></param>
        /// <param name="pageText"></param>
        /// <param name="pageToolTip"></param>
        /// <param name="borderSize"></param>
        /// <param name="headerHeight"></param>
        /// <param name="addGroupTitle"></param>
        /// <param name="lineY"></param>
        /// <param name="lineH"></param>
        /// <param name="groupRows">Počet řádků v jedné grupě</param>
        /// <param name="spaceX"></param>
        /// <param name="spaceY"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        private static DataFormPage CreateSamplePage(int sampleId, string[] texts, string[] tooltips, int rowCount, string pageText, string pageToolTip,
            int borderSize, int headerHeight, bool addGroupTitle, int lineY, int lineH, int groupRows, int spaceX, int spaceY, int[] widths)
        {
            var random = _Random;

            DataFormPage page = new DataFormPage();
            page.PageText = pageText;
            page.ToolTipTitle = pageText;
            page.ToolTipText = pageToolTip;

            DataFormGroup group = null;

            #region Appearances
            DataFormBackgroundAppearance borderAppearance = new DataFormBackgroundAppearance()
            {
                GradientStyle = GradientStyleType.Downward,
                BackColor = Color.FromArgb(64, 64, 64, 64),
                BackColorEnd = Color.FromArgb(32, 160, 160, 160),
                OnMouseBackColor = Color.FromArgb(160, 64, 64, 64),
                OnMouseBackColorEnd = Color.FromArgb(96, 160, 160, 160)
            };
            DataFormBackgroundAppearance headerAppearance1 = new DataFormBackgroundAppearance()
            {
                GradientStyle = GradientStyleType.ToRight,
                BackColor = Color.FromArgb(64, 64, 64, 64),
                BackColorEnd = Color.FromArgb(16, 160, 160, 160),
                OnMouseBackColor = Color.FromArgb(108, 192, 192, 64),
                OnMouseBackColorEnd = Color.FromArgb(16, 192, 192, 128)
            };
            DataFormBackgroundAppearance headerAppearance2 = new DataFormBackgroundAppearance()
            {
                GradientStyle = GradientStyleType.ToRight,
                BackColor = Color.FromArgb(128, 64, 64, 224),
                BackColorEnd = Color.FromArgb(4, 96, 96, 255),
                OnMouseBackColor = Color.FromArgb(255, 64, 64, 224),
                OnMouseBackColorEnd = Color.FromArgb(4, 96, 96, 255),
            };
            DataFormBackgroundAppearance lineAppearance = new DataFormBackgroundAppearance()
            {
                GradientStyle = GradientStyleType.ToRight,
                BackColor = Color.FromArgb(128, 128, 64, 224),
                BackColorEnd = Color.FromArgb(4, 140, 96, 255),
                OnMouseBackColor = Color.FromArgb(255, 128, 64, 224),
                OnMouseBackColorEnd = Color.FromArgb(4, 140, 96, 255),
            };
            DataFormColumnAppearance titleAppearance = new DataFormColumnAppearance()
            {
                FontSizeDelta = 1,
                FontStyleBold = true,
                ContentAlignment = ContentAlignment.MiddleLeft
            };
            DataFormColumnAppearance labelAppearance = new DataFormColumnAppearance()
            {
                ContentAlignment = ContentAlignment.MiddleRight
            };
            #endregion

            int textsCount = texts.Length;
            int tooltipsCount = tooltips.Length;

            string text, tooltip;
            int count = rowCount;
            int groupId = 0;
            int y = 0;
            int maxX = 0;
            int q;
            int px = 12;
            int py = ((sampleId == 40) ? 0 : 12);
            for (int r = 0; r < count; r++)
            {
                if ((r % groupRows) == 0)
                    group = null;

                if (group == null)
                {
                    y = 0;

                    groupId++;
                    group = new DataFormGroup();
                    group.DesignBorderRange = new Int32Range(1, 1 + borderSize);
                    group.BorderAppearance = borderAppearance;
                    group.DesignPadding = new Padding(px, py, px, py);
                    group.GroupId = "Group" + groupId.ToString();

                    if (sampleId == 34)
                    {   // Výrobní čísla - úzká pro force layout break
                        if ((page.Groups.Count % 20) == 0)
                            group.LayoutMode = DatFormGroupLayoutMode.ForceBreakToNewColumn;
                        else
                            group.LayoutMode = DatFormGroupLayoutMode.AllowBreakToNewColumn;
                    }
                    if (sampleId == 35)
                    {   // Výrobní čísla - úzká pro auto layout break
                        if ((page.Groups.Count % 3) == 0)
                            group.LayoutMode = DatFormGroupLayoutMode.AllowBreakToNewColumn;
                    }
                    if (sampleId == 40)
                    {
                        y = 0;
                    }

                    if (headerHeight > 0)
                    {
                        var groupTitle = new DataFormGroupHeader()
                        {   // Základ = výška a pozadí
                            DesignHeaderHeight = headerHeight,
                            BackgroundAppearance = (headerHeight == 2 ? headerAppearance2 : headerAppearance1)
                        };

                        if (addGroupTitle)
                        {   // Text
                            groupTitle.DesignTitlePadding = new Padding(px, 2, px, 2);
                            groupTitle.HeaderItems.Add(new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, ColumnId = "GroupTitleId" + groupId.ToString(), Text = "Skupina " + page.Groups.Count.ToString(), DesignBounds = new Rectangle(12, 0, 180, 18), Alignment = ContentAlignment.MiddleLeft, Appearance = titleAppearance });
                        }

                        if (lineH > 0)
                        {   // Linka pod textem
                            groupTitle.DesignLineRange = new Int32Range(lineY, lineY + lineH);
                            groupTitle.LineAppearance = lineAppearance;
                        }
                        group.GroupHeader = groupTitle;
                    }
                    page.Groups.Add(group);
                }

                // První prvek v každém řádku je Label:
                int x = 10;
                text = $"Atributy {(r + 1)}:";
                DataFormColumnImageText lbl = new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, Text = text, DesignBounds = new Rectangle(x, y, 75, 18) };
                lbl.Appearance = labelAppearance;
                group.Items.Add(lbl);
                x += 80;

                // Prvky s danou šířkou:
                foreach (int width in widths)
                {
                    bool blank = (random.Next(100) == 68);
                    text = (!blank ? texts[random.Next(textsCount)] : "");
                    tooltip = (!blank ? tooltips[random.Next(tooltipsCount)] : "");

                    DataFormColumnType itemType = DataFormColumnType.TextBox;
                    if (sampleId != 40)
                    {
                        q = random.Next(100);
                        itemType = (q < 5 ? DataFormColumnType.None :
                                   (q < 10 ? DataFormColumnType.CheckBox :
                                   (q < 15 ? DataFormColumnType.Button :
                                   (q < 22 ? DataFormColumnType.Label :
                                   (q < 30 ? DataFormColumnType.TextBoxButton :
                                   (q < 40 ? DataFormColumnType.TextBox : // ComboBoxList :
                                   (q < 50 ? DataFormColumnType.TokenEdit :
                                             DataFormColumnType.TextBox)))))));
                    }

                    DataFormColumn item = null;
                    int shiftY = 0;
                    DataFormColumnIndicatorType indicators = DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold;
                    switch (itemType)
                    {
                        case DataFormColumnType.Label:
                            DataFormColumnImageText label = new DataFormColumnImageText() { Text = text };
                            shiftY = 0;
                            indicators = DataFormColumnIndicatorType.None;
                            item = label;
                            break;
                        case DataFormColumnType.TextBox:
                            DataFormColumnImageText textBox = new DataFormColumnImageText() { Text = text };
                            item = textBox;
                            break;
                        case DataFormColumnType.TextBoxButton:
                            DataFormColumnTextBoxButton textBoxButton = new DataFormColumnTextBoxButton() { Text = "TEXTBOX BUTTON" };
                            q = random.Next(100);
                            textBoxButton.ButtonsVisibleAllways = (q < 50);
                            q = random.Next(100);
                            textBoxButton.ButtonAs3D = (q < 25);
                            q = random.Next(100);
                            textBoxButton.ButtonKind = (q < 30 ? DataFormButtonKind.Ellipsis :
                                                       (q < 50 ? DataFormButtonKind.Search :
                                                       (q < 65 ? DataFormButtonKind.Right :
                                                       (q < 80 ? DataFormButtonKind.Plus :
                                                                 DataFormButtonKind.OK))));
                            item = textBoxButton;
                            break;
                        case DataFormColumnType.CheckBox:
                            DataFormColumnCheckBox checkBox = new DataFormColumnCheckBox() { Text = text };
                            shiftY = 0;
                            item = checkBox;
                            break;
                        case DataFormColumnType.ComboBoxList:
                            // musíme dodělat
                            DataFormColumnImageText comboBoxList = new DataFormColumnImageText() { Text = text };
                            item = comboBoxList;
                            break;
                        case DataFormColumnType.TokenEdit:
                            DataFormColumnMenuText tokenEdit = new DataFormColumnMenuText() { Text = "TOKEN EDIT" };
                            tokenEdit.MenuItems = CreateSampleMenuItems(texts, tooltips, 100, 300, random);
                            item = tokenEdit;
                            break;
                        case DataFormColumnType.Button:
                            DataFormColumnImageText button = new DataFormColumnImageText() { Text = text };
                            indicators = DataFormColumnIndicatorType.MouseOverBold | DataFormColumnIndicatorType.WithFocusBold;
                            item = button;
                            break;
                    }
                    if (item != null)
                    {
                        if (indicators != DataFormColumnIndicatorType.None)
                        {
                            q = random.Next(100);
                            if (q < 4)
                                indicators |= DataFormColumnIndicatorType.CorrectAllwaysThin;
                            else if (q < 8)
                                indicators |= DataFormColumnIndicatorType.CorrectAllwaysBold;
                            else if (q < 12)
                                indicators |= DataFormColumnIndicatorType.WarningAllwaysThin;
                            else if (q < 16)
                                indicators |= DataFormColumnIndicatorType.WarningAllwaysBold;
                            else if (q < 20)
                                indicators |= DataFormColumnIndicatorType.ErrorAllwaysThin;
                            else if (q < 24)
                                indicators |= DataFormColumnIndicatorType.ErrorAllwaysBold;
                            else if (q < 50)
                            {
                                indicators |= DataFormColumnIndicatorType.IndicatorColorAllwaysThin;
                                item.IndicatorColor = Color.FromArgb(0, random.Next(128, 200), random.Next(128, 200), random.Next(128, 200));
                            }
                        }
                        item.ColumnType = itemType;
                        item.ToolTipText = tooltip;
                        item.DesignBounds = new Rectangle(x, (y + shiftY), width, (20 - shiftY));
                        item.Indicators = indicators;
                        group.Items.Add(item);
                    }

                    x += (width + spaceX);
                }
                maxX = x;
                y += 20 + spaceY;
            }

            return page;
        }
        /// <summary>
        /// Vrátí pole prvků <see cref="IMenuItem"/>, v daném počtu prvků.
        /// </summary>
        /// <param name="texts"></param>
        /// <param name="tooltips"></param>
        /// <param name="countMin"></param>
        /// <param name="countMax"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        private static List<IMenuItem> CreateSampleMenuItems(string[] texts, string[] tooltips, int countMin, int countMax, System.Random random)
        {
            List<IMenuItem> menuItems = new List<IMenuItem>();
            int count = random.Next(countMin, countMax);
            for (int i = 0; i < count; i++)
            {
                DataMenuItem item = new DataMenuItem();
                item.ItemId = "MenuItem_" + i.ToString();
                item.Text = texts[random.Next(texts.Length)];
                item.ToolTipText = tooltips[random.Next(tooltips.Length)];
                menuItems.Add(item);
            }
            return menuItems;
        }
        #endregion
        #region Reálnější vzorky
        private static void AddSamplePage10x(int sampleId, List<IDataFormPage> pages)
        {
            DataFormPage page1 = new DataFormPage() { PageId = "f101a", PageText = "Základní data" };
            pages.Add(page1);

            #region Predefinice:
            var width = 900;
            var borderRange = new Int32Range(3, 4);
            var borderAppearance = new DataFormBackgroundAppearance()
            {
                GradientStyle = GradientStyleType.DownRight,
                BackColor = Color.FromArgb(192, 64, 64, 64),
                BackColorEnd = Color.FromArgb(32, 64, 64, 64)
            };
            var designPadding = new Padding(6);
            var headerAppearance = new DataFormBackgroundAppearance()
            {
                BackColor = Color.FromArgb(160, 190, 240, 160),
                BackColorEnd = Color.FromArgb(0, 190, 240, 160),
                OnMouseBackColor = Color.FromArgb(220, 190, 240, 160),
                OnMouseBackColorEnd = Color.FromArgb(64, 190, 240, 160),
                GradientStyle = GradientStyleType.DownRight
            };
            var groupHeader = new DataFormGroupHeader()
            {
                DesignHeaderHeight = 30,
                BackgroundAppearance = headerAppearance,
                DesignTitlePadding = new Padding(18, 2, 18, 2),
                DesignLineRange = new Int32Range(29, 30),
                LineAppearance = new DataFormBackgroundAppearance()
                {
                    GradientStyle = GradientStyleType.ToRight,
                    BackColor = Color.FromArgb(255, 90, 200, 80), BackColorEnd = Color.FromArgb(32, 90, 200, 80)
                }
            };
            var groupBgrAppearance = new DataFormBackgroundAppearance()
            {
                BackColor = Color.FromArgb(64, 192, 216, 255),
                BackColorEnd = Color.FromArgb(32, 220, 240, 255),
                GradientStyle = GradientStyleType.ToRight,
            };
            var titleTextAppearance = new DataFormColumnAppearance() { FontStyleBold = true, FontSizeDelta = 1 };
            #endregion
            #region Modifikace dle sampleId
            switch (sampleId)
            {
                case 101:
                    width = 900;
                    headerAppearance.OnMouseBackColor = Color.FromArgb(220, 190, 240, 160);
                    headerAppearance.OnMouseBackColorEnd = Color.FromArgb(64, 190, 240, 160);
                    headerAppearance.GradientStyle = GradientStyleType.DownRight;
                    break;
                case 102:
                    width = 800;
                    headerAppearance.OnMouseBackColor = Color.FromArgb(220, 120, 240, 140);
                    headerAppearance.OnMouseBackColorEnd = Color.FromArgb(64, 120, 240, 140);
                    headerAppearance.GradientStyle = GradientStyleType.Downward;
                    break;
                case 103:
                    width = 900;
                    borderRange = new Int32Range(0, 0);
                    designPadding = new Padding(2, 2, 2, 6);
                    headerAppearance.OnMouseBackColor = Color.FromArgb(220, 190, 240, 160);
                    headerAppearance.OnMouseBackColorEnd = Color.FromArgb(64, 190, 240, 160);
                    headerAppearance.GradientStyle = GradientStyleType.DownRight;
                    break;
                case 104:
                    width = 760;
                    borderRange = new Int32Range(0, 0);
                    designPadding = new Padding(2, 2, 2, 6);
                    headerAppearance.OnMouseBackColor = Color.FromArgb(220, 120, 240, 140);
                    headerAppearance.OnMouseBackColorEnd = Color.FromArgb(64, 120, 240, 140);
                    headerAppearance.GradientStyle = GradientStyleType.Downward;
                    break;
            }
            #endregion
            #region Grupa 1

            DataFormGroup group1 = new DataFormGroup()
            {
                GroupId = "Adresa",
                CollapseMode = DataFormGroupCollapseMode.AllowCollapseAllways,
                DesignPadding = designPadding,
                DesignHeight = 420,
                DesignWidth = width,
                DesignBorderRange = borderRange,
                BorderAppearance = borderAppearance
            };
            var group1BgrAppearance = groupBgrAppearance.CreateClone() as DataFormBackgroundAppearance;
            group1BgrAppearance.BackImageName = @"ImagesTest\BackCorners\Corner00028m.png";
            group1BgrAppearance.BackImageFill = ImageFillMode.Resize;
            group1BgrAppearance.BackImageAlignment = ContentAlignment.MiddleRight;
            group1.BackgroundAppearance = group1BgrAppearance;

            var group1Header = groupHeader.CreateClone() as DataFormGroupHeader;
            group1Header.HeaderItems.Add(new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, ColumnId = "adresa_header", Text = "Adresa", DesignBounds = new Rectangle(0, 4, 250, 20), Alignment = ContentAlignment.MiddleLeft, Appearance = titleTextAppearance });
            group1.GroupHeader = group1Header;

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_ulice_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 18, 100, 18), Text = "Ulice" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_ulice", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 40, 200, 20), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_cislopop", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(212, 40, 128, 20), Indicators = DataFormColumnIndicatorType.CorrectAllwaysBold });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_mesto_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 66, 100, 18), Text = "Město" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_psc", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 88, 60, 20), Indicators = DataFormColumnIndicatorType.RequiredAllwaysThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_mesto", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(72, 88, 268, 20), Indicators = DataFormColumnIndicatorType.RequiredAllwaysThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 114, 100, 18), Text = "Stát" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_refer", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 136, 80, 20) });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_nazev", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(92, 136, 190, 20) });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_nazev", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(284, 136, 56, 20) });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 162, 100, 18), Text = "Poznámka" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note", ColumnType = DataFormColumnType.EditBox, DesignBounds = new Rectangle(8, 184, 220, 80) });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_ineu", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(238, 190, 102, 20), Text = "Rezident EU" });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_inemu", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(256, 212, 84, 20), Text = "Platí €" });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_ingb", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(238, 234, 102, 20), Text = "Člen UK" });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note", ColumnType = DataFormColumnType.EditBox, DesignBounds = new Rectangle(350, 40, 400, 224) });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "filename_inp_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 270, 250, 18), Text = "Zadejte vstupní soubor:" });
            group1.Items.Add(new DataFormColumnTextBoxButton() { ColumnId = "filename_inp", ColumnType = DataFormColumnType.TextBoxButton, DesignBounds = new Rectangle(8, 292, 332, 20), ButtonKind = DataFormButtonKind.Ellipsis, ButtonsVisibleAllways = false });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "filename_out_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(354, 270, 250, 18), Text = "Zadejte výstupní soubor:" });
            group1.Items.Add(new DataFormColumnTextBoxButton() { ColumnId = "filename_out", ColumnType = DataFormColumnType.TextBoxButton, DesignBounds = new Rectangle(350, 292, 400, 20), ButtonKind = DataFormButtonKind.Ellipsis, ButtonsVisibleAllways = false });

            page1.Groups.Add(group1);

            #endregion
            #region Grupa 2

            DataFormGroup group2 = new DataFormGroup()
            {
                GroupId = "Kontaktní osoba",
                CollapseMode = DataFormGroupCollapseMode.AllowCollapseAllways,
                DesignPadding = designPadding,
                DesignWidth = width,
                DesignBorderRange = borderRange,
                BorderAppearance = borderAppearance
            };
            var group2BgrAppearance = groupBgrAppearance.CreateClone() as DataFormBackgroundAppearance;
            group2BgrAppearance.BackImageName = @"ImagesTest\BackCorners\Corner00010tr2.png";
            group2BgrAppearance.BackImageFill = ImageFillMode.Resize;
            group2BgrAppearance.BackImageAlignment = ContentAlignment.TopRight;
            group2.BackgroundAppearance = group2BgrAppearance;

            var group2Header = groupHeader.CreateClone() as DataFormGroupHeader;
            group2Header.HeaderItems.Add(new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, ColumnId = "jmeno_header", Text = "Kontaktní osoba", DesignBounds = new Rectangle(0, 4, 250, 20), Alignment = ContentAlignment.MiddleLeft, Appearance = titleTextAppearance });
            group2.GroupHeader = group2Header;
            
            group2.Items.Add(new DataFormColumnImageText() { ColumnId = "jmeno_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 18, 250, 18), Text = "Jméno a příjmení kontaktní osoby, tituly...:" });
            group2.Items.Add(new DataFormColumnImageText() { ColumnId = "jmeno_jmeno", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 40, 120, 20), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group2.Items.Add(new DataFormColumnImageText() { ColumnId = "jmeno_prijmeni", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(132, 40, 300, 20), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group2.Items.Add(new DataFormColumnImageText() { ColumnId = "jmeno_titul", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(436, 40, 150, 20), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });

            page1.Groups.Add(group2);

            #endregion
            #region Grupa 3

            DataFormGroup group3 = new DataFormGroup()
            {
                GroupId = "Podnikání",
                CollapseMode = DataFormGroupCollapseMode.AllowCollapseAllways,
                DesignPadding = designPadding,
                DesignWidth = width,
                DesignBorderRange = borderRange,
                BorderAppearance = borderAppearance
            };
            var group3BgrAppearance = groupBgrAppearance.CreateClone() as DataFormBackgroundAppearance;
            group3BgrAppearance.BackImageName = null;
            group3BgrAppearance.BackImageFill = ImageFillMode.Resize;
            group3BgrAppearance.BackImageAlignment = ContentAlignment.TopRight;
            group3.BackgroundAppearance = group3BgrAppearance;

            var group3Header = groupHeader.CreateClone() as DataFormGroupHeader;
            group3Header.HeaderItems.Add(new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, ColumnId = "podnikani_header", Text = "Způsob podnikání", DesignBounds = new Rectangle(0, 4, 250, 20), Alignment = ContentAlignment.MiddleLeft, Appearance = titleTextAppearance });
            group3.GroupHeader = group3Header;

            group3.Items.Add(new DataFormColumnImageText() { ColumnId = "podnikani_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 14, 250, 18), Text = "Předměty podnikání...:" });
            group3.Items.Add(new DataFormColumnImageText() { ColumnId = "podnikani_button", ColumnType = DataFormColumnType.Button, DesignBounds = new Rectangle(width - 200, 11, 180, 24), Text = "Ověřit v ISIN" });
            group3.Items.Add(new DataFormColumnImageText() { ColumnId = "podnikani", ColumnType = DataFormColumnType.EditBox, DesignBounds = new Rectangle(0, 36, width - 20, 80), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });

            page1.Groups.Add(group3);

            #endregion
            #region Modifikace dle sampleId
            switch (sampleId)
            {
                case 101:
                    group1BgrAppearance.BackImageName = @"ImagesTest\BackCorners\Corner00028m.png";
                    group1BgrAppearance.BackImageFill = ImageFillMode.Resize;
                    group1BgrAppearance.BackImageAlignment = ContentAlignment.MiddleRight;
                    break;
                case 102:
                    group1BgrAppearance.BackImageName = @"ImagesTest\BackCorners\Corner00032b.png";
                    group1BgrAppearance.BackImageFill = ImageFillMode.Resize;
                    group1BgrAppearance.BackImageAlignment = ContentAlignment.BottomCenter;
                    group2BgrAppearance.BackColor = Color.FromArgb(192, 255, 192, 255);
                    group2BgrAppearance.BackColorEnd = Color.FromArgb(64, 255, 192, 255);
                    break;
                case 103:
                    group1BgrAppearance.BackImageName = @"ImagesTest\BackCorners\Corner00028m.png";
                    group1BgrAppearance.BackImageFill = ImageFillMode.Resize;
                    group1BgrAppearance.BackImageAlignment = ContentAlignment.MiddleRight;
                    group3BgrAppearance.BackImageName = "images/miscellaneous/windows_32x32.png";
                    group3BgrAppearance.BackImageFill = ImageFillMode.Shrink;
                    group3BgrAppearance.BackImageAlignment = ContentAlignment.TopRight;
                    break;
                case 104:
                    group1BgrAppearance.BackImageName = null;
                    group2BgrAppearance.BackImageName = null;
                    group3BgrAppearance.BackImageName = "images/miscellaneous/windows_32x32.png";
                    group2BgrAppearance.BackColor = Color.FromArgb(192, 255, 192, 255);
                    group2BgrAppearance.BackColorEnd = Color.FromArgb(64, 255, 192, 255);
                    group3BgrAppearance.BackImageFill = ImageFillMode.Shrink;
                    group3BgrAppearance.BackImageAlignment = ContentAlignment.TopRight;
                    break;
            }
            #endregion
        }
        #endregion
    }
    #endregion
}
