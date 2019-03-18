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
            this.Rows = new List<TableRowText>();
            this.Font = FontInfo.Default;
        }
        /// <summary>
        /// Řádky v této tabulce
        /// </summary>
        public List<TableRowText> Rows { get; private set; }
        /// <summary>
        /// Výchozí barva pozadí
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Výchozí barva textu
        /// </summary>
        public Color? TextColor { get; set; }
        /// <summary>
        /// Výchozí font. V objektu <see cref="TableText"/> je inicializován na hodnotu <see cref="FontInfo.Default"/>.
        /// Pro řádek obsahující titulek je vhodné použít font <see cref="FontInfo.DefaultBold"/>.
        /// </summary>
        public FontInfo Font { get; set; }
        #endregion
        #region Měření velikosti textu
        public void TextMeasure(Graphics graphics, bool extendColumnWidth)
        {

        }
        public Size? CurrentSize { get; private set; }
        #endregion
    }
    /// <summary>
    /// Reprezentuje jeden řádek textu
    /// </summary>
    public class TableRowText
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableRowText()
        {
            this.Cells = new List<TableOneText>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            foreach (var cell in this.Cells)
                text += (text.Length == 0 ? "| " : " ") + (cell.Text == null ? "{Null}" : cell.Text) + " |";
            return text;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="texts"></param>
        public TableRowText(params string[] texts)
            : this()
        {
            if (texts != null)
                this.Cells.AddRange(texts.Select(t => new TableOneText(t)));
        }
        /// <summary>
        /// Buňky v tomto řádku
        /// </summary>
        public List<TableOneText> Cells { get; private set; }
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
    }
    /// <summary>
    /// Jedna buňka textu
    /// </summary>
    public class TableOneText
    {
        #region Konstruktor a data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableOneText()
        {
            this.Alignment = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TableOneText(string text, ContentAlignment? alignment = null, int? width = null)
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
        /// Explicitní barva textu
        /// </summary>
        public Color? TextColor { get; set; }
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
        /// <summary>
        /// Velikost textu v pixelech, změřená pro danou grafiku
        /// </summary>
        internal Size? CurrentSize { get; set; }
    }
}
