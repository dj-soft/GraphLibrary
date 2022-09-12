using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace SDCardTester
{
    public class TestControl : Control
    {
        #region Konstruktor a public data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TestControl()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ContainerControl | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, false);
            this.MouseInit();
        }
        /// <summary>
        /// Seznam prvků. Není null. Setováním null se zde vytvoří new prázdná instance.
        /// <para/>
        /// Po změně hodnoty je třeba volat <see cref="Refresh"/>, stejně tak po změnách dat v prvcích. Samo se nerefreshuje.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<Item> Items 
        {
            get
            {
                if (_Items is null) _Items = new List<Item>();
                return _Items;
            }
            set { _Items = value; }
        }
        private List<Item> _Items;
        /// <summary>
        /// Celková délka dat, na kterou je prvek dimenzován. Do této délky se promítají prvky v <see cref="Items"/>. 
        /// Pokud je tam větší obsah, pak nadbytečný je ignorován. Pokud je tam méně, pak nevyužitá délka je prázdná.
        /// <para/>
        /// Po změně hodnoty je třeba volat <see cref="Refresh"/>. Samo se nerefreshuje.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long TotalLength { get; set; }
        /// <summary>
        /// Výška linky řádků. Může mít hodnotu od 4 do 32 včetně. Výchozí je 8.
        /// <para/>
        /// Po změně hodnoty je třeba volat <see cref="Refresh"/>. Samo se nerefreshuje.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LineHeight
        {
            get { return _LineHeight; }
            set { _LineHeight = (value < 4 ? 4 : (value > 32 ? 32 : value)); }
        }
        private int _LineHeight = 8;
        #endregion
        #region Pohyb myši a detekce prvku Item pod myší
        /// <summary>
        /// Inicializace eventů myši
        /// </summary>
        private void MouseInit()
        {
            this.MouseMove += _MouseMove;
        }
        /// <summary>
        /// Po každém pohybu myši
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            DetectActiveItem(e.Location);
        }
        /// <summary>
        /// Detekuje aktivní prvek na aktuálním bodu myši
        /// </summary>
        private void DetectActiveItem()
        {
            Point point = this.PointToClient(Control.MousePosition);
            DetectActiveItem(point);
        }
        /// <summary>
        /// Detekuje aktivní prvek na daném bodu myši (lokální souřadnice)
        /// </summary>
        /// <param name="point"></param>
        private void DetectActiveItem(Point point)
        {
            Item item = null;
            var lines = this.Lines;
            if (lines != null)
            {
                var line = lines.FirstOrDefault(l => l.ContainsPoint(point));
                if (line != null)
                    item = line.GetItemAtPoint(point);
            }
            if (!Object.ReferenceEquals(item, _ActiveItem))
                CallActiveItemChanged(item);
        }
        /// <summary>
        /// Aktivní prvek = nad tímto prvkem je myš. Při změně je volána událost <see cref="ActiveItemChanged"/>.
        /// </summary>
        public Item ActiveItem { get { return _ActiveItem; } }
        private Item _ActiveItem;
        private void CallActiveItemChanged(Item activeItem)
        {
            _ActiveItem = activeItem;
            OnActiveItemChanged();
            ActiveItemChanged?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnActiveItemChanged() { }
        public event EventHandler ActiveItemChanged;
        #endregion
        #region Refresh a vykreslení controlu
        /// <summary>
        /// Zajistí Refresh zobrazení. 
        /// Lze volat z Working threadů.
        /// </summary>
        public override void Refresh()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(base.Refresh));
            else
                base.Refresh();
        }
        /// <summary>
        /// Systémové kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintBackground(e);
            PaintItems(e);
            DetectActiveItem();
        }
        /// <summary>
        /// Vykreslí pozadí, určí souřadnice řádků do <see cref="Lines"/>
        /// </summary>
        /// <param name="e"></param>
        protected void PaintBackground(PaintEventArgs e)
        {
            var lines = new List<LineInfo>();

            var bounds = this.ClientRectangle;
            e.Graphics.FillRectangle(Brushes.WhiteSmoke, bounds);

            Padding innerPadding = new Padding(2);
            var innerBounds = new Rectangle(bounds.X + innerPadding.Left, bounds.Y + innerPadding.Top, bounds.Width - innerPadding.Horizontal, bounds.Height - innerPadding.Vertical);

            int pixelBegin = 0;
            int lineHeight = LineHeight;
            int lineX = innerBounds.X;
            int lineWidth = innerBounds.Width;
            int endY = innerBounds.Bottom - lineHeight;
            int y = innerBounds.Y;
            while (y <= endY)
            {
                Rectangle lineBounds = new Rectangle(lineX, y, lineWidth, lineHeight - 1);
                Painter.PaintBar3D(e.Graphics, Skin.FreeSpaceColor, lineBounds);

                var line = new LineInfo(lines.Count, pixelBegin, lineBounds);
                lines.Add(line);
                pixelBegin = line.PixelEnd;

                y += lineHeight;
            }
            Lines = lines;
        }
        /// <summary>
        /// Vykresluje jednotlivé prvky, zanáší jejich souřadnice do linek
        /// </summary>
        /// <param name="e"></param>
        protected void PaintItems(PaintEventArgs e)
        {
            var lines = Lines;
            if (lines is null || lines.Count == 0) return;
            int linesCount = lines.Count;
            int pixelLength = lines[linesCount - 1].PixelEnd;

            var items = this.Items;
            long startPos = 0L;
            long totalLength = this.TotalLength;
            foreach (var item in items)
            {
                if (startPos >= totalLength) break;                            // Prvek (jeho počátek) se nachází za pozicí konce prostoru => aktuální prvek ani následující prvky už není kam kreslit.

                var itemLength = item.Length;
                if (itemLength <= 0L) continue;                                // Prvek nemá reálnou velikost, nebude vidět

                decimal relativeBegin = getRelativePosition(startPos);         // Relativní pozice počátku v rozsahu 0 - 1
                startPos += itemLength;
                decimal relativeEnd= getRelativePosition(startPos);            // Relativní pozice konce v rozsahu 0 - 1

                if (!item.Color.HasValue) continue;                            // Prvek bez barvy není vidět

                // Aktuální prvek se může nacházet na jednom, dvou i více grafických řádcích:
                //  například: začíná v 30% řádku 2, řádek 2 obsazuje do konce, poté obsazuje celé řádky 3 a 4, a končí na řádku 5 na jeho 80%:
                //  anebo začíná na 75% řádku 2 a končí na 10% řádku 3
                //  anebo se nachází na řádku 6 v jeho rozmezí 25% - 35%:
                LinePointInfo begin = getLinePoint(relativeBegin, true);
                LinePointInfo end = getLinePoint(relativeEnd, false);
                if (begin is null || end is null) continue;

                for (int index = begin.LineIndex; index <= end.LineIndex; index++)
                {
                    var lineInfo = lines[index];
                    Rectangle? bounds = lineInfo.GetInnerBounds(begin, end);
                    if (bounds.HasValue)
                    {
                        Painter.PaintBar3D(e.Graphics, item.Color.Value, bounds.Value);
                        lineInfo.AddPart(item, bounds.Value);
                    }
                }
            }

            // Vrátí hodnotu v rozsahu { 0 až 1 } (včetně) pro danou Long pozici, relativně k celkové Long délce totalLength.
            decimal getRelativePosition(long position)
            {
                if (totalLength <= 0L) return 0m;
                if (position <= 0L) return 0m;
                if (position >= totalLength) return 1m;
                return ((decimal)position) / ((decimal)totalLength);
            }

            // Najde a vrátí pozici v prostoru jednotlivých linek pro daný bod
            LinePointInfo getLinePoint(decimal relativePoint, bool isBegin)
            {
                int pixelPosition = (int)Math.Round(relativePoint * (decimal)pixelLength, 0);      // Určíme pozici hledaného pixelu podle jeho relativního umístění (relativePoint); hodnota pixelLength je celkový počet pixelů na všech řádcích.
                var lineInfo = lines.FirstOrDefault(l => l.ContainsPixel(pixelPosition, !isBegin));// Najdeme řádek, který obsahuje daný pixel. Pokud je pixel == End řádku, akceptujeme ho jen pokud hledáme End tohoto úseku (isBegin je false)
                if (lineInfo is null)
                {
                    if (isBegin)                                               // Hledám začátek, a nenašel jsem - to by se stát nemělo:
                        throw new InvalidOperationException($"Nenalezen řádek pro Begin : relativePoint = {relativePoint}.");
                    throw new InvalidOperationException($"Nenalezen řádek pro End : relativePoint = {relativePoint}.");
                }

                int linePoint = pixelPosition - lineInfo.PixelBegin;
                return new LinePointInfo(lineInfo, linePoint);
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected List<LineInfo> Lines;
        #endregion
        #region Sub classes a interface
        public class Item
        {
            public Item() { }
            public Item(long length, Color? color, string text = null, object data = null)
            {
                Length = length;
                Color = color;
                Text = text;
                Data = data;
            }
            public override string ToString()
            {
                string length = Length.ToString("### ### ### ### ##0").Trim();
                return $"Length: {length}; Color: {Color}";
            }
            /// <summary>
            /// Délka dat v tomto prvku
            /// </summary>
            public long Length { get; set; }
            /// <summary>
            /// Barva prvku
            /// </summary>
            public Color? Color { get; set; }
            /// <summary>
            /// Text do ToolTipu
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// Aplikační data
            /// </summary>
            public object Data { get; set; }
        }
        /// <summary>
        /// Data o vizuální lince
        /// </summary>
        protected class LineInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="index"></param>
            /// <param name="pixelBegin"></param>
            /// <param name="bounds"></param>
            public LineInfo(int index, int pixelBegin, Rectangle bounds)
            {
                this.Index = index;
                this.PixelBegin = pixelBegin;
                this.Bounds = bounds;
                this._Parts = new List<Tuple<Rectangle, Item>>();
            }
            public override string ToString()
            {
                return $"Line; Index: {Index}; PixelBegin: {PixelBegin}; PixelLength: {PixelLength}; PixelEnd: {PixelEnd};";
            }
            /// <summary>
            /// Index řádky
            /// </summary>
            public int Index { get; private set; }
            /// <summary>
            /// Hodnota pixelu na začátku linie
            /// </summary>
            public int PixelBegin { get; private set; }
            /// <summary>
            /// Délka v pixelech
            /// </summary>
            public int PixelLength { get { return Bounds.Width; } }
            /// <summary>
            /// Hodnota pixelu za koncem linie = začátek příští linie
            /// </summary>
            public int PixelEnd { get { return PixelBegin + PixelLength; } }
            /// <summary>
            /// Souřadnice aktivního prostoru
            /// </summary>
            public Rectangle Bounds { get; private set; }
            /// <summary>
            /// Vrátí true, když daný <paramref name="pixel"/> leží uvnitř this řádku, anebo když <paramref name="acceptOnEnd"/> je true, pak vrátí true když <paramref name="pixel"/> == konec this řádku.
            /// </summary>
            /// <param name="pixel"></param>
            /// <param name="acceptOnEnd"></param>
            /// <returns></returns>
            public bool ContainsPixel(int pixel, bool acceptOnEnd)
            {
                return ((pixel >= this.PixelBegin && pixel < this.PixelEnd) || (acceptOnEnd && pixel == this.PixelEnd));
            }
            /// <summary>
            /// Vrátí true, pokud daný fyzický bod v controlu se nachází v prostoru tohoto řádku
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            public bool ContainsPoint(Point point)
            {
                return this.Bounds.Contains(point);
            }
            /// <summary>
            /// Vrátí prvek, který se v rámci this řádku nachází na dané souřadnici
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            public Item GetItemAtPoint(Point point)
            {
                Item item = null;
                if (_Parts != null)
                    item = _Parts.FirstOrDefault(t => t.Item1.Contains(point))?.Item2;
                return item;
            }
            /// <summary>
            /// Vrátí souřadnice prostoru, která má daný začátek a konec. Akceptuje i indexy řádku počátku a konce.
            /// </summary>
            /// <param name="begin"></param>
            /// <param name="end"></param>
            /// <returns></returns>
            public Rectangle? GetInnerBounds(LinePointInfo begin, LinePointInfo end)
            {
                var bounds = this.Bounds;
                int x0 = bounds.X;
                int x1 = ((begin is null || begin.LineIndex < this.Index) ? 0 : begin.RelativePoint);
                int x2 = ((end is null || end.LineIndex > this.Index) ? this.PixelLength : end.RelativePoint);
                if (x2 <= x1) return null;
                return new Rectangle(x0 + x1, bounds.Y, x2 - x1, bounds.Height);
            }
            /// <summary>
            /// Smaže přiřazení jednotlivých prvků do souřadnic
            /// </summary>
            public void ClearParts()
            {
                this._Parts.Clear();
            }
            /// <summary>
            /// Přidá do evidence přiřazení souřadnic a prvku zobrazeném na té souřadnici
            /// </summary>
            /// <param name="item"></param>
            /// <param name="bounds"></param>
            public void AddPart(Item item, Rectangle bounds)
            {
                this._Parts.Add(new Tuple<Rectangle, Item>(bounds, item));
            }
            /// <summary>
            /// Částice
            /// </summary>
            private List<Tuple<Rectangle, Item>> _Parts;
        }
        /// <summary>
        /// Definice pozice v řádku
        /// </summary>
        protected class LinePointInfo
        {
            public LinePointInfo(LineInfo lineInfo, int linePoint)
            {
                LineInfo = lineInfo;
                RelativePoint = linePoint;
            }
            public override string ToString()
            {
                return $"LinePoint; LineIndex: {LineInfo.Index}; RelativePoint: {RelativePoint}";
            }
            /// <summary>
            /// Data řádku
            /// </summary>
            public LineInfo LineInfo { get; private set; }
            /// <summary>
            /// Data řádku
            /// </summary>
            public int LineIndex { get { return LineInfo.Index; } }
            /// <summary>
            /// Souřadnice bodu X relativně k souřadnici řádku <see cref="LineInfo.Bounds"/>
            /// </summary>
            public int RelativePoint { get; private set; }
        }
        #endregion
    }
    public class Skin
    {
        #region Public static
        public static Color OtherSpaceColor { get { return Instance._OtherSpaceColor; } set { Instance._OtherSpaceColor = value; } }
        public static Color UsedSpaceColor { get { return Instance._UsedSpaceColor; } set { Instance._UsedSpaceColor = value; } }
        public static Color FreeSpaceColor { get { return Instance._FreeSpaceColor; } set { Instance._FreeSpaceColor = value; } }

        public static Color PictureGroupColor { get { return Instance._PictureGroupColor; } set { Instance._PictureGroupColor = value; } }
        public static Color MovieGroupColor { get { return Instance._MovieGroupColor; } set { Instance._MovieGroupColor = value; } }
        public static Color AudioGroupColor { get { return Instance._AudioGroupColor; } set { Instance._AudioGroupColor = value; } }
        public static Color DocumentsGroupColor { get { return Instance._DocumentsGroupColor; } set { Instance._DocumentsGroupColor = value; } }
        public static Color ApplicationGroupColor { get { return Instance._ApplicationGroupColor; } set { Instance._ApplicationGroupColor = value; } }
        public static Color DevelopmentGroupColor { get { return Instance._DevelopmentGroupColor; } set { Instance._DevelopmentGroupColor = value; } }
        public static Color ArchiveGroupColor { get { return Instance._ArchiveGroupColor; } set { Instance._ArchiveGroupColor = value; } }

        /// <summary>
        /// Typ palety
        /// </summary>
        public enum PaletteType
        {
            Light,
            Pastel,
            Dark
        }
        #endregion
        #region Singleton a private data
        protected static Skin Instance
        {
            get
            {
                if (__Instance is null)
                {
                    lock (__Lock)
                    {
                        if (__Instance is null)
                            __Instance = new Skin();
                    }
                }
                return __Instance;
            }
        }
        private static Skin __Instance;
        private static object __Lock = new object();
        private Skin()
        {
            _SetPalette(PaletteType.Light);
        }
        private Color _OtherSpaceColor;
        private Color _UsedSpaceColor;
        private Color _FreeSpaceColor;
        private Color _PictureGroupColor;
        private Color _MovieGroupColor;
        private Color _AudioGroupColor;
        private Color _DocumentsGroupColor;
        private Color _ApplicationGroupColor;
        private Color _DevelopmentGroupColor;
        private Color _ArchiveGroupColor;
        #endregion
        #region Přednastavené palety
        /// <summary>
        /// Aktuální paleta.
        /// </summary>
        public static PaletteType Palette
        {
            get { return Instance._Palette; }
            set { Instance._SetPalette(value); }
        }
        private PaletteType _Palette;
        private void _SetPalette(PaletteType palette)
        {
            switch(palette)
            {
                case PaletteType.Light:
                    _OtherSpaceColor = Color.FromArgb(160, 160, 160);
                    _UsedSpaceColor = Color.FromArgb(255, 114, 149);
                    _FreeSpaceColor = Color.FromArgb(191, 255, 170);
                    _PictureGroupColor = Color.FromArgb(181, 150, 247);
                    _MovieGroupColor = Color.FromArgb(243, 140, 247);
                    _AudioGroupColor = Color.FromArgb(138, 156, 247);
                    _DocumentsGroupColor = Color.FromArgb(242, 235, 227);
                    _ApplicationGroupColor = Color.FromArgb(141, 239, 239);
                    _DevelopmentGroupColor = Color.FromArgb(153, 196, 239);
                    _ArchiveGroupColor = Color.FromArgb(212, 239, 129);
                    _Palette = palette;
                    break;
                case PaletteType.Pastel:
                    _OtherSpaceColor = Color.FromArgb(0xF2CCFF);
                    _UsedSpaceColor = Color.FromArgb(0xFFDACE);
                    _FreeSpaceColor = Color.FromArgb(0xCEFFDA);
                    _PictureGroupColor = Color.FromArgb(0xFFCECE);
                    _MovieGroupColor = Color.FromArgb(0xFFE6CE);
                    _AudioGroupColor = Color.FromArgb(0xE6FFCE);
                    _DocumentsGroupColor = Color.FromArgb(0xCEF2FF);
                    _ApplicationGroupColor = Color.FromArgb(0xCEDAFF);
                    _DevelopmentGroupColor = Color.FromArgb(0xCEFFFF);
                    _ArchiveGroupColor = Color.FromArgb(0xFFF2CE);
                    _Palette = palette;
                    break;
                case PaletteType.Dark:
                    _OtherSpaceColor = Color.FromArgb(0xF2CCFF);
                    _UsedSpaceColor = Color.FromArgb(0xFFDACE);
                    _FreeSpaceColor = Color.FromArgb(0xCEFFDA);
                    _PictureGroupColor = Color.FromArgb(0xFFCECE);
                    _MovieGroupColor = Color.FromArgb(0xFFE6CE);
                    _AudioGroupColor = Color.FromArgb(0xE6FFCE);
                    _DocumentsGroupColor = Color.FromArgb(0xCEF2FF);
                    _ApplicationGroupColor = Color.FromArgb(0xCEDAFF);
                    _DevelopmentGroupColor = Color.FromArgb(0xCEFFFF);
                    _ArchiveGroupColor = Color.FromArgb(0xFFF2CE);
                    _Palette = palette;
                    break;
            }
        }
        #endregion
    }
    /// <summary>
    /// Painter
    /// </summary>
    public static class Painter
    {
        /// <summary>
        /// Vymaluje daný prostor s pomocí SolidBrush
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="color"></param>
        /// <param name="bounds"></param>
        public static void PaintRectangle(Graphics graphics, Color color, Rectangle bounds)
        {
            graphics.FillRectangle(GetSolidBrush(color), bounds);
        }
        /// <summary>
        /// Vymaluje daný prostor s pomocí 3D Brush
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="color"></param>
        /// <param name="bounds"></param>
        public static void PaintBar3D(Graphics graphics, Color color, Rectangle bounds)
        {
            using (var brush = GetBrush3D(color, bounds.Y - 1, bounds.Height))
                graphics.FillRectangle(brush, bounds);
        }
        /// <summary>
        /// Vepíše text
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        public static void PaintText(Graphics graphics, Font font, string text, Color color, Rectangle bounds, ContentAlignment alignment)
        {
            if (String.IsNullOrEmpty(text)) return;

            var textSize = graphics.MeasureString(text, font, 8192);
            var textBounds = AlignSizeTo(textSize, bounds, alignment);
            var clip = graphics.Clip;
            graphics.SetClip(bounds);
            graphics.DrawString(text, font, GetSolidBrush(color), textBounds.Location);
            graphics.Clip = clip;
        }
        /// <summary>
        /// Vrátí souřadnice prvku dané velikosti <paramref name="size"/>, zarovnané do prostoru <paramref name="bounds"/> ve stylu zarovnání <paramref name="alignment"/>.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bounds"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static RectangleF AlignSizeTo(SizeF size, RectangleF bounds, ContentAlignment alignment)
        {
            float bx = bounds.X;
            float by = bounds.Y;
            float bw= bounds.Width;
            float bh = bounds.Height;
            float sx = bx;
            float sy = by;
            float sw = size.Width;
            float sh = size.Height;
            if (sw > bw) sw = bw;
            if (sh > bh) sh = bh;
            float dw = bw - sw;
            float dh = bh - sh;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    sx = bx;
                    sy = by;
                    break;
                case ContentAlignment.TopCenter:
                    sx = bx + dw / 2f;
                    sy = by;
                    break;
                case ContentAlignment.TopRight:
                    sx = bx + dw;
                    sy = by;
                    break;
                case ContentAlignment.MiddleLeft:
                    sx = bx;
                    sy = by + dh / 2f;
                    break;
                case ContentAlignment.MiddleCenter:
                    sx = bx + dw / 2f;
                    sy = by + dh / 2f;
                    break;
                case ContentAlignment.MiddleRight:
                    sx = sx + dw;
                    sy = by + dh / 2f;
                    break;
                case ContentAlignment.BottomLeft:
                    sx = bx;
                    sy = by + dh;
                    break;
                case ContentAlignment.BottomCenter:
                    sx = bx + dw / 2f;
                    sy = by + dh;
                    break;
                case ContentAlignment.BottomRight:
                    sx = sx + dw;
                    sy = by + dh;
                    break;
            }
            return new RectangleF(sx, sy, sw, sh);
        }
        /// <summary>
        /// Vrátí SolidBrush
        /// </summary>
        /// <param name="color"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Brush GetSolidBrush(Color color)
        {
            if (_SolidBrush is null) _SolidBrush = new SolidBrush(Color.White);
            _SolidBrush.Color = color;
            return _SolidBrush;
        }
        private static SolidBrush _SolidBrush;
        /// <summary>
        /// Vrátí 3D Brush
        /// </summary>
        /// <param name="color"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Brush GetBrush3D(Color color, int y, int height)
        {
            GetColors(color, out Color color1, out Color color2, 16);
            return new LinearGradientBrush(new Point(0, y), new PointF(0, y + height), color1, color2);
        }
        /// <summary>
        /// Vygeneruje pár barev vycházející z dané barvy, z danou diferencí složek; 
        /// barva <paramref name="color1"/> bude mít diferenci kladnou (pro kladné <paramref name="diff"/> světlejší barva);
        /// barva <paramref name="color2"/> bude mít diferenci zápornou (pro kladné <paramref name="diff"/> tmavší barva);
        /// </summary>
        /// <param name="source"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <param name="diff"></param>
        public static void GetColors(Color source, out Color color1, out Color color2, int diff = 10)
        {
            diff = (diff < -48 ? -48 : (diff > 48 ? 48 : diff));
            color1 = GetColor(source, diff, diff, diff);
            color2 = GetColor(source, -diff, -diff, -diff);
        }
        /// <summary>
        /// Vrátí modifikovanou barvu. Kanál Alpha nemění.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="diffR"></param>
        /// <param name="diffG"></param>
        /// <param name="diffB"></param>
        /// <returns></returns>
        public static Color GetColor(Color source, int diffR, int diffG, int diffB)
        {
            return Color.FromArgb(source.A, getPart(source.R, diffR), getPart(source.G, diffG), getPart(source.B, diffB));

            int getPart(int s, int d)
            {
                int q = s + d;
                return (q < 0 ? 0 : (q > 255 ? 255 : q));
            }
        }
    }
}
