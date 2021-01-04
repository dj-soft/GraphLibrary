using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Reprezentuje text v tabulce
    /// </summary>
    public class TableText
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableText()
        {
            this.Rows = new List<TableTextRow>();
            this.DrawHorizontalLines = true;
            this.DrawVerticalLines = true;
            this.Font = FontInfo.Default;
        }
        /// <summary>
        /// Řádky v této tabulce
        /// </summary>
        public List<TableTextRow> Rows { get; private set; }
        /// <summary>
        /// Hodnota true (default) = kreslit vodorovné linky pod řádky, false = nekreslit
        /// </summary>
        public bool DrawHorizontalLines { get; set; }
        /// <summary>
        /// Hodnota true (default) = kreslit svislé linky za sloupci, false = nekreslit
        /// </summary>
        public bool DrawVerticalLines { get; set; }
        /// <summary>
        /// Výchozí barva pozadí
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Výchozí barva textu
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Aktuální barva vodorovné (Horizontální) linie. Může být null.
        /// Pokud je barva zadaná, pak se linka kreslí.
        /// </summary>
        public Color? LineHColor { get; set; }
        /// <summary>
        /// Aktuální barva svislé (Vertikální) linie. Může být null.
        /// Pokud je barva zadaná, pak se linka kreslí.
        /// </summary>
        public Color? LineVColor { get; set; }
        /// <summary>
        /// Aktuální barva okraje tabulky. Může být null.
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Vnitřní okraje buňky mezi jejími okraji a textem
        /// </summary>
        public Padding? ContentPadding { get; set; }
        /// <summary>
        /// Výchozí font. V objektu <see cref="TableText"/> je inicializován na hodnotu <see cref="FontInfo.Default"/>.
        /// Pro řádek obsahující titulek je vhodné použít font <see cref="FontInfo.DefaultBold"/>.
        /// </summary>
        public FontInfo Font { get; set; }
        #endregion
        #region Měření velikosti textu
        /// <summary>
        /// Obsahuje true, pokud this objekt potřebuje provést změření velikosti obsahu pomocí metody TextMeasure()
        /// </summary>
        public bool NeedMeasure
        {
            get
            {
                if (!this.CurrentSize.HasValue) return true;
                return this.Rows.Any(r => r.NeedMeasure);
            }
        }
        /// <summary>
        /// Změří velikost textu this tabulky, uloží ji do <see cref="CurrentSize"/>
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="extendColumnWidth"></param>
        public void TextMeasure(Graphics graphics, bool extendColumnWidth = false)
        {
            Size currentSize = Size.Empty;
            if (this.Rows != null && this.Rows.Count > 0)
            {
                TableTextRow titleRow = this.Rows[0];
                for (int i = 0; i < this.Rows.Count; i++)
                    this.Rows[i].TextMeasure(this, graphics, extendColumnWidth, i, titleRow, ref currentSize);
                
                currentSize.Width = titleRow.PrepareColumnsWidth();
            }
            this.CurrentSize = currentSize.Add(3, 3);
        }
        /// <summary>
        /// Velikost tabulky v pixelech, změřená pro danou grafiku
        /// </summary>
        public Size? CurrentSize { get; private set; }
        #endregion
        #region Podpora pro kreslení
        /// <summary>
        /// Pozice jednotlivých sloupců
        /// </summary>
        public List<Data.Int32Range> ColumnRanges
        {
            get
            {
                List<Data.Int32Range> result = new List<Data.Int32Range>();

                if (this.Rows != null && this.Rows.Count > 0)
                {
                    TableTextRow titleRow = this.Rows[0];
                    int x = 0;
                    foreach (var cell in titleRow.Cells)
                    {
                        int r = x + cell.CurrentSize.Value.Width;
                        result.Add(new Data.Int32Range(x, r));
                        x = r;
                    }
                }

                return result;
            }
        }
        /// <summary>
        /// Aktuální font.
        /// Nikdy není null, jako default vrací <see cref="FontInfo.Default"/>.
        /// </summary>
        public FontInfo CurrentFont
        {
            get
            {
                if (this.Font != null) return this.Font;
                return FontInfo.Default;
            }
        }
        /// <summary>
        /// Aktuální barva textu.
        /// </summary>
        public Color CurrentTextColor
        {
            get
            {
                if (this.TextColor.HasValue) return this.TextColor.Value;
                return Skin.Control.ControlTextColor;
            }
        }
        /// <summary>
        /// Aktuální barva pozadí. Nikdy není null.
        /// </summary>
        public Color CurrentBackColor
        {
            get
            {
                if (this.BackColor.HasValue) return this.BackColor.Value;
                return Skin.Control.ControlBackColor;
            }
        }
        /// <summary>
        /// Aktuální barva vodorovné (Horizontální) linie. Může být null, pak se tato linie nekreslí.
        /// </summary>
        public Color? CurrentLineHColor
        {
            get
            {
                if (this.LineHColor.HasValue) return this.LineHColor;
                if (!this.DrawHorizontalLines) return null;
                return Skin.Grid.BorderLineColor;
            }
        }
        /// <summary>
        /// Aktuální barva svislé (Vertikální) linie. Může být null, pak se tato linie nekreslí.
        /// </summary>
        public Color? CurrentLineVColor
        {
            get
            {
                if (this.LineVColor.HasValue) return this.LineVColor;
                if (!this.DrawVerticalLines) return null;
                return Skin.Grid.BorderLineColor;
            }
        }
        /// <summary>
        /// Aktuální barva okraje tabulky.
        /// </summary>
        public Color CurrentBorderColor
        {
            get
            {
                if (this.BorderColor.HasValue) return this.BorderColor.Value;
                return Skin.Grid.BorderLineColor;
            }
        }
        /// <summary>
        /// Vnitřní okraje buňky mezi jejími okraji a textem
        /// </summary>
        public Padding CurrentContentPadding
        {
            get
            {
                if (this.ContentPadding.HasValue) return this.ContentPadding.Value;
                return new Padding(1);
            }
        }
        #endregion
    }
    /// <summary>
    /// Reprezentuje jeden řádek textu
    /// </summary>
    public class TableTextRow
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableTextRow()
        {
            this.Cells = new List<TableTextCell>();
            this.RowIndex = -1;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="texts"></param>
        public TableTextRow(params string[] texts)
            : this()
        {
            if (texts != null)
                this.Cells.AddRange(texts.Select(t => new TableTextCell(t)));
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            foreach (var cell in this.Cells)
                text += (text.Length == 0 ? "| " : " ") + (cell.Text ?? "{Null}") + " |";
            return "Row[" + this.RowIndex.ToString() + "] = " + text;
        }
        /// <summary>
        /// Reference na Parent tabulku. Může být null, před prvním měřením velikosti obsahu.
        /// </summary>
        public TableText ParentTable { get; private set; }
        /// <summary>
        /// Index this řádku
        /// </summary>
        public int RowIndex { get; private set; }
        /// <summary>
        /// Buňky v tomto řádku
        /// </summary>
        public List<TableTextCell> Cells { get; private set; }
        /// <summary>
        /// Explicitní barva pozadí pro tento řádek
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// 3D efekt barvy na pozadí
        /// </summary>
        public float? BackEffect3D { get; set; }
        /// <summary>
        /// Explicitní barva textu pro tento řádek
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Výchozí font pro buňky tohoto řádku. Výchozí hodnota = null, font se převezme z <see cref="TableText"/>.
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Výška řádku v pixelech, daná uživatelem
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Výška řádku po jeho změření
        /// </summary>
        public int? CurrentHeight { get; set; }
        #endregion
        #region Měření velikosti textu
        /// <summary>
        /// Obsahuje true, pokud this objekt potřebuje provést změření velikosti obsahu pomocí metody TextMeasure()
        /// </summary>
        internal bool NeedMeasure
        {
            get
            {
                if (!this.CurrentSize.HasValue) return true;
                return this.Cells.Any(r => r.NeedMeasure);
            }
        }
        /// <summary>
        /// Změří velikost textu this tabulky, uloží ji do <see cref="CurrentSize"/>
        /// </summary>
        /// <param name="parentTable"></param>
        /// <param name="graphics"></param>
        /// <param name="extendColumnWidth"></param>
        /// <param name="rowIndex"></param>
        /// <param name="titleRow"></param>
        /// <param name="tableSize">Souhrnná velikost</param>
        internal void TextMeasure(TableText parentTable, Graphics graphics, bool extendColumnWidth, int rowIndex, TableTextRow titleRow, ref Size tableSize)
        {
            Size currentSize = Size.Empty;
            this.ParentTable = parentTable;
            this.RowIndex = rowIndex;
            if (this.Cells != null && this.Cells.Count > 0)
            {
                for (int i = 0; i < this.Cells.Count; i++)
                    this.Cells[i].TextMeasure(this, graphics, extendColumnWidth, rowIndex, i, titleRow, ref currentSize);
            }
            this.CurrentSize = currentSize;

            if (tableSize.Width < currentSize.Width) tableSize.Width = currentSize.Width;
            tableSize.Height += currentSize.Height;
        }
        /// <summary>
        /// Metoda je volána pouze pro titulní řádek.
        /// Pro svoje buňky (které de facto představují Headery sloupců) určí šířku sloupců: buď podle explicitně zadané, anebo podle aktuální šířky obsahu.
        /// Na základě toho si tento titulkový řádek upraví i svoji vlastní šířku.
        /// </summary>
        internal int PrepareColumnsWidth()
        {
            int width = 0;
            
            for (int i = 0; i < this.Cells.Count; i++)
                width += this.Cells[i].PrepareTitleColumnWidth();

            this.CurrentSize = new Size(width, this.CurrentSize.Value.Height);

            return width;
        }
        /// <summary>
        /// Velikost řádku v pixelech, změřená pro danou grafiku
        /// </summary>
        public Size? CurrentSize { get; private set; }
        #endregion
        #region Podpora pro kreslení
        /// <summary>
        /// Aktuální font.
        /// Nikdy není null, jako default vrací <see cref="FontInfo.Default"/>.
        /// </summary>
        public FontInfo CurrentFont
        {
            get
            {
                if (this.Font != null) return this.Font;
                if (this.ParentTable != null) return this.ParentTable.CurrentFont;
                return FontInfo.Default;
            }
        }
        /// <summary>
        /// Aktuální barva textu. Nikdy není null.
        /// </summary>
        public Color CurrentTextColor
        {
            get
            {
                if (this.TextColor.HasValue) return this.TextColor.Value;
                if (this.ParentTable != null) return this.ParentTable.CurrentTextColor;
                return Skin.Control.ControlTextColor;
            }
        }
        /// <summary>
        /// Aktuální barva pozadí. Může být null, pak se pro řádek nekreslí explicitní pozadí, nechává se podklad z tabulky.
        /// </summary>
        public Color? CurrentBackColor
        {
            get
            {
                return this.BackColor;
            }
        }
        #endregion
    }
    /// <summary>
    /// Jedna buňka textu
    /// </summary>
    public class TableTextCell
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableTextCell()
        {
            this.Alignment = null;
            this.CellIndex = -1;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableTextCell(string text, ContentAlignment? alignment = null, int? width = null)
            : this()
        {
            this.Text = text;
            this.Alignment = alignment;
            this.Width = width;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Reference na Parent řádek. Může být null, před prvním měřením velikosti obsahu.
        /// </summary>
        public TableTextRow ParentRow { get; private set; }
        /// <summary>
        /// Index this sloupce
        /// </summary>
        public int CellIndex { get; private set; }
        /// <summary>
        /// Reference na Parent tabulku. Může být null, před prvním měřením velikosti obsahu.
        /// </summary>
        public TableText ParentTable { get { return this.ParentRow?.ParentTable; } }
        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Zarovnání textu v buňce
        /// </summary>
        public ContentAlignment? Alignment { get; set; }
        /// <summary>
        /// Explicitní barva pozadí
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// 3D efekt barvy na pozadí
        /// </summary>
        public float? BackEffect3D { get; set; }
        /// <summary>
        /// Explicitní barva textu
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Aktuální barva vodorovné (Horizontální) linie. Může být null.
        /// </summary>
        public Color? LineHColor { get; set; }
        /// <summary>
        /// Aktuální barva svislé (Vertikální) linie. Může být null.
        /// </summary>
        public Color? LineVColor { get; set; }
        /// <summary>
        /// Explicitní font pro tuto buňku.
        /// Null = převezme se z řádku nebo z tabulky.
        /// </summary>
        public FontInfo Font { get; set; }
        /// <summary>
        /// Šířka buňky v pixelech, daná uživatelem.
        /// Pokud je null, převezme se z prvního řádku z buňky na tomtéž indexu z property <see cref="CurrentWidth"/>.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Šířka sloupce v pixelech.
        /// Tato hodnota je naplněna jen v řádku [0].
        /// </summary>
        public int? CurrentWidth { get; set; }
        #endregion
        #region Měření velikosti textu
        /// <summary>
        /// Obsahuje true, pokud this objekt potřebuje provést změření velikosti obsahu pomocí metody TextMeasure()
        /// </summary>
        internal bool NeedMeasure
        {
            get
            {
                if (!this.CurrentSize.HasValue) return true;
                return false;
            }
        }
        /// <summary>
        /// Změří velikost textu this tabulky, uloží ji do <see cref="CurrentSize"/>
        /// </summary>
        /// <param name="parentRow"></param>
        /// <param name="graphics"></param>
        /// <param name="extendColumnWidth"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellIndex"></param>
        /// <param name="titleRow"></param>
        /// <param name="rowSize">Souhrnná velikost</param>
        internal void TextMeasure(TableTextRow parentRow, Graphics graphics, bool extendColumnWidth, int rowIndex, int cellIndex, TableTextRow titleRow, ref Size rowSize)
        {
            this.ParentRow = parentRow;
            this.CellIndex = cellIndex;
            // Velikost textu + Padding + 1px (linky):
            Size currentSize = Painter.MeasureString(graphics, this.Text, this.CurrentFont)
                .Add(this.CurrentContentPadding)
                .Add(1, 1);

            this.CurrentSize = currentSize;

            this.StoreColumnWidth(currentSize.Width, rowIndex, cellIndex, titleRow);

            // Přiměřeně zvětšit velikost řádku:
            if (rowSize.Height < currentSize.Height) rowSize.Height = currentSize.Height;
            rowSize.Width += currentSize.Width;
        }
        /// <summary>
        /// Nastaví si svoji šířku (sloupce), pro this = buňka v titulkovém řádku
        /// </summary>
        internal int PrepareTitleColumnWidth()
        {
            int width = -1;
            if (this.Width.HasValue && this.Width.Value >= 0)
                width = this.Width.Value;
            else if (this.CurrentSize.HasValue && this.CurrentSize.Value.Width > 0)
                width = this.CurrentSize.Value.Width;
            if (width >= 0)
                this.CurrentSize = new Size(width, this.CurrentSize.Value.Height);
            return width;
        }
        /// <summary>
        /// Metoda určí správnou šířku sloupce, pro zjištěnou reálnou šířku (dle textu, fontu, a okrajů), a pro další parametry.
        /// </summary>
        /// <param name="contentWidth"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellIndex"></param>
        /// <param name="titleRow"></param>
        /// <returns></returns>
        protected void StoreColumnWidth(int contentWidth, int rowIndex, int cellIndex, TableTextRow titleRow)
        {
            // Jak zacházet se šířkou sloupce:
            if (rowIndex == 0)
            {   // Jsme "titulní" řádek:
                this.ContentMaxWidth = contentWidth;
            }
            else
            {   // Jsme běžný datový řádek:
                this.ContentMaxWidth = null;
                // Najdeme buňku z titulního řádku, pro stejný sloupec jako this buňka:
                TableTextCell titleCell = ((titleRow != null && titleRow.Cells.Count > cellIndex) ? titleRow.Cells[cellIndex] : null);
                if (titleCell != null)
                {   // Nastřádáme Max():
                    if (!titleCell.ContentMaxWidth.HasValue || (titleCell.ContentMaxWidth.HasValue && titleCell.ContentMaxWidth.Value < contentWidth))
                        titleCell.ContentMaxWidth = contentWidth;
                }
            }
        }
        /// <summary>
        /// Velikost buňky v pixelech, změřená pro danou grafiku
        /// </summary>
        internal Size? CurrentSize { get; set; }
        /// <summary>
        /// Šířka obsahu maximální, bez ohledu na šířku <see cref="Width"/>.
        /// Tato hodnota je naplněna pouze v řádku [0], jinde je null.
        /// </summary>
        internal int? ContentMaxWidth { get; private set; }
        #endregion
        #region Podpora pro kreslení
        /// <summary>
        /// Metoda vrátí souřadnice prostoru pro text uvnitř dané buňky
        /// </summary>
        /// <param name="cellBounds"></param>
        /// <returns></returns>
        public Rectangle GetTextBounds(Rectangle cellBounds)
        {
            return cellBounds.Sub(this.CurrentContentPadding);
        }
        /// <summary>
        /// Aktuální font.
        /// Nikdy není null, jako default vrací <see cref="FontInfo.Default"/>.
        /// </summary>
        public FontInfo CurrentFont
        {
            get
            {
                if (this.Font != null) return this.Font;
                if (this.ParentRow != null) return this.ParentRow.CurrentFont;
                return FontInfo.Default;
            }
        }
        /// <summary>
        /// Aktuální zarovnání obsahu.
        /// </summary>
        public ContentAlignment CurrentAlignment
        {
            get
            {
                if (this.Alignment.HasValue) return this.Alignment.Value;
                return ContentAlignment.MiddleLeft;
            }
        }
        /// <summary>
        /// Aktuální barva textu.
        /// </summary>
        public Color CurrentTextColor
        {
            get
            {
                if (this.TextColor.HasValue) return this.TextColor.Value;
                if (this.ParentRow != null) return this.ParentRow.CurrentTextColor;
                return Skin.Control.ControlTextColor;
            }
        }
        /// <summary>
        /// Aktuální barva pozadí. Může být null, pak se pro buňku ani pro řádek nekreslí explicitní pozadí, nechává se podklad z tabulky.
        /// </summary>
        public Color? CurrentBackColor
        {
            get
            {
                if (this.BackColor.HasValue) return this.BackColor;
                if (this.ParentRow != null) return this.ParentRow.CurrentBackColor;
                return null;
            }
        }
        /// <summary>
        /// 3D efekt barvy na pozadí
        /// </summary>
        public float? CurrentBackEffect3D
        {
            get
            {
                if (this.BackEffect3D.HasValue) return this.BackEffect3D;
                if (this.ParentRow != null) return this.ParentRow.BackEffect3D;
                return null;
            }
        }
        /// <summary>
        /// Aktuální barva vodorovné (Horizontální) linie. Může být null, pak se tato linie nekreslí.
        /// </summary>
        public Color? CurrentLineHColor
        {
            get
            {
                if (this.LineHColor.HasValue) return this.LineHColor;
                if (this.ParentTable != null) return this.ParentTable.CurrentLineHColor;
                return null;
            }
        }
        /// <summary>
        /// Aktuální barva svislé (Vertikální) linie. Může být null, pak se tato linie nekreslí.
        /// </summary>
        public Color? CurrentLineVColor
        {
            get
            {
                if (this.LineVColor.HasValue) return this.LineVColor;
                if (this.ParentTable != null) return this.ParentTable.CurrentLineVColor;
                return null;
            }
        }
        /// <summary>
        /// Vnitřní okraje buňky mezi jejími okraji a textem
        /// </summary>
        public Padding CurrentContentPadding
        {
            get
            {
                if (this.ParentTable != null) return this.ParentTable.CurrentContentPadding;
                return new Padding(1);
            }
        }
        #endregion
    }
}
