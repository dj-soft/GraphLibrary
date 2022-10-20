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
                        0, 2, true, 28, 5, 6, new int[] { 140, 260, 40, 300, 120 }));
                    break;
                case 20:
                    pages.Add(CreateSamplePage(20, texts, tooltips, 2000, "Základní stránka", "Obsahuje běžné informace",
                        0, 2, true, 28, 2, 2, new int[] { 80, 150, 80, 60, 100, 120, 160, 40, 120, 180, 80, 40, 60, 250 }));
                    pages.Add(CreateSamplePage(21, texts, tooltips, 120, "Doplňková stránka", "Obsahuje další málo používané informace",
                        0, 2, true, 28, 2, 2, new int[] { 250, 250, 60, 250, 250, 60, 250 }));
                    break;
                case 30:
                    pages.Add(CreateSamplePage(30, texts, tooltips, 500, "Základní stránka", "Obsahuje běžné informace",
                        0, 24, true, 28, 6, 10, new int[] { 250, 250, 60, 250, 250, 60, 250 }));
                    pages.Add(CreateSamplePage(31, texts, tooltips, 70, "Sklady", null,
                        1, 24, false, 26, 4, 4, new int[] { 100, 75, 120, 100 }));
                    pages.Add(CreateSamplePage(32, texts, tooltips, 15, "Faktury", null,
                        1, 24, false, 26, 1, 1, new int[] { 70, 70, 70, 70 }));
                    pages.Add(CreateSamplePage(33, texts, tooltips, 25, "Zaúčtování", null,
                        1, 24, false, 26, 6, 10, new int[] { 400, 125, 75, 100 }));
                    pages.Add(CreateSamplePage(34, texts, tooltips, 480, "Výrobní čísla fixní zalomení", null,
                        3, 24, true, 26, 5, 5, new int[] { 100, 100, 70 }));
                    pages.Add(CreateSamplePage(35, texts, tooltips, 480, "Výrobní čísla automatické zalomení", null,
                        3, 24, true, 26, 5, 5, new int[] { 100, 100, 70 }));
                    break;
                case 40:
                    pages.Add(CreateSamplePage(40, texts, tooltips, 1, "Základní stránka", "Obsahuje běžné informace",
                        0, 0, false, 0, 5, 1, new int[] { 140, 260, 40, 300, 120, 80, 120, 80, 80, 80, 80, 250, 40, 250, 40, 250, 40, 250, 40, 250, 40 }));
                    break;


                case 101:
                    AddSamplePage101(pages);
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
        /// <param name="beginY"></param>
        /// <param name="spaceX"></param>
        /// <param name="spaceY"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        private static DataFormPage CreateSamplePage(int sampleId, string[] texts, string[] tooltips, int rowCount, string pageText, string pageToolTip,
            int borderSize, int headerHeight, bool addGroupTitle, int beginY, int spaceX, int spaceY, int[] widths)
        {
            var random = _Random;

            DataFormPage page = new DataFormPage();
            page.PageText = pageText;
            page.ToolTipTitle = pageText;
            page.ToolTipText = pageToolTip;

            DataFormGroup group = null;

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
            DataFormColumnAppearance titleAppearance = new DataFormColumnAppearance()
            {
                FontSizeDelta = 2,
                FontStyleBold = true,
                ContentAlignment = ContentAlignment.MiddleLeft
            };
            DataFormColumnAppearance labelAppearance = new DataFormColumnAppearance()
            {
                ContentAlignment = ContentAlignment.MiddleRight
            };

            int textsCount = texts.Length;
            int tooltipsCount = tooltips.Length;

            string text, tooltip;
            int count = rowCount;
            int y = 0;
            int maxX = 0;
            int q;
            int px = 12;
            int py = ((sampleId == 40) ? 0 : 12);
            for (int r = 0; r < count; r++)
            {
                if ((r % 10) == 0)
                    group = null;

                if (group == null)
                {
                    y = 1 + borderSize;

                    group = new DataFormGroup();
                    group.DesignPadding = new Padding(px, py, px, py);
                    group.DesignHeaderHeight = headerHeight;
                    if (headerHeight > 0)
                        group.HeaderAppearance = (headerHeight == 2 ? headerAppearance2 : headerAppearance1);
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
                    group.DesignBorderRange = new Int32Range(1, 1 + borderSize);
                    group.BorderAppearance = borderAppearance;

                    page.Groups.Add(group);

                    int contentY = y + headerHeight;
                    if (addGroupTitle)
                    {
                        bool isThinLine = (headerHeight < 10);
                        int titleY = (isThinLine ? y + headerHeight + 3 : y + 1);
                        // titleY - py ... ?  Každý běžný prvek bude odsunut o Padding, ale titulek posouvat o Padding nechci, takže jej 'předsunu':
                        DataFormColumnImageText title = new DataFormColumnImageText() { ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(60, titleY - py, 150, 20) };
                        title.Text = "Skupina " + page.Groups.Count.ToString();
                        title.Appearance = titleAppearance;
                        group.Items.Add(title);
                        y += (isThinLine ? titleY + 20 : y + headerHeight + 2);
                        if (y < contentY)
                            y = contentY;
                    }
                    else
                    {
                        y = contentY;
                    }
                }

                // První prvek v řádku je Label:
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
        private static void AddSamplePage101(List<IDataFormPage> pages)
        {
            DataFormPage page1 = new DataFormPage() { PageId = "f101a", PageText = "Základní data" };
            pages.Add(page1);

            DataFormGroup group1 = new DataFormGroup() { GroupId = "Adresa", GroupText = "Adresa", DesignHeaderHeight = 30, CollapseMode = DataFormGroupCollapseMode.AllowCollapseAllways,  };
            page1.Groups.Add(group1);

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_ulice_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 18, 100, 18), Text = "Ulice" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_ulice", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 40, 200, 20), Indicators = DataFormColumnIndicatorType.CorrectOnDemandThin | DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_cislopop", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(212, 40, 120, 20), Indicators = DataFormColumnIndicatorType.CorrectAllwaysBold });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_mesto_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 66, 100, 18), Text = "Město" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_psc", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 88, 60, 20), Indicators = DataFormColumnIndicatorType.MouseOverThin | DataFormColumnIndicatorType.WithFocusBold });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_mesto", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(72, 88, 260, 20) });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 114, 100, 18), Text = "Stát" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_refer", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(8, 136, 80, 20) });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_zeme_nazev", ColumnType = DataFormColumnType.TextBox, DesignBounds = new Rectangle(92, 136, 240, 20) });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 162, 100, 18), Text = "Poznámka" });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note", ColumnType = DataFormColumnType.EditBox, DesignBounds = new Rectangle(8, 184, 220, 80) });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_ineu", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(238, 190, 100, 20), Text = "Rezident EU" });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_inemu", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(256, 212, 100, 20), Text = "Platí €" });
            group1.Items.Add(new DataFormColumnCheckBox() { ColumnId = "adresa_ingb", ColumnType = DataFormColumnType.CheckBox, DesignBounds = new Rectangle(238, 234, 100, 20), Text = "Člen UK" });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "adresa_note", ColumnType = DataFormColumnType.EditBox, DesignBounds = new Rectangle(350, 40, 400, 224) });

            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "filename_inp_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(12, 270, 250, 18), Text = "Zadejte vstupní soubor:" });
            group1.Items.Add(new DataFormColumnTextBoxButton() { ColumnId = "filename_inp", ColumnType = DataFormColumnType.TextBoxButton, DesignBounds = new Rectangle(8, 292, 332, 20), ButtonKind = DataFormButtonKind.Ellipsis, ButtonsVisibleAllways = false });
            group1.Items.Add(new DataFormColumnImageText() { ColumnId = "filename_out_label", ColumnType = DataFormColumnType.Label, DesignBounds = new Rectangle(354, 270, 250, 18), Text = "Zadejte výstupní soubor:" });
            group1.Items.Add(new DataFormColumnTextBoxButton() { ColumnId = "filename_out", ColumnType = DataFormColumnType.TextBoxButton, DesignBounds = new Rectangle(350, 292, 400, 20), ButtonKind = DataFormButtonKind.Ellipsis, ButtonsVisibleAllways = false });

        }
        #endregion
    }
    #endregion
}
