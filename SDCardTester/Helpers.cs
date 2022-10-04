using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DjSoft.Tools.SDCardTester
{
    /// <summary>
    /// Skin: obsahuje definice barev používaných v controlech
    /// </summary>
    public class Skin
    {
        #region Public static
        public static Color BackgroundColor { get { return Instance._BackgroundColor; } set { Instance._BackgroundColor = value; } }
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

        public static Color TestFilesProcessingReadGroupColor { get { return Instance._TestFilesProcessingReadGroupColor; } set { Instance._TestFilesProcessingReadGroupColor = value; } }
        public static Color TestFilesExistingGroupColor { get { return Instance._TestFilesExistingGroupColor; } set { Instance._TestFilesExistingGroupColor = value; } }
        public static Color TestFilesProcessingSaveGroupColor { get { return Instance._TestFilesProcessingSaveGroupColor; } set { Instance._TestFilesProcessingSaveGroupColor = value; } }

        public static Color TestPhaseSaveShortFileBackColor { get { return Instance._TestPhaseSaveShortFileBackColor; } set { Instance._TestPhaseSaveShortFileBackColor = value; } }
        public static Color TestPhaseSaveLongFileBackColor { get { return Instance._TestPhaseSaveLongFileBackColor; } set { Instance._TestPhaseSaveLongFileBackColor = value; } }
        public static Color TestPhaseReadShortFileBackColor { get { return Instance._TestPhaseReadShortFileBackColor; } set { Instance._TestPhaseReadShortFileBackColor = value; } }
        public static Color TestPhaseReadLongFileBackColor { get { return Instance._TestPhaseReadLongFileBackColor; } set { Instance._TestPhaseReadLongFileBackColor = value; } }

        public static Color TestAsyncErrorColor { get { return Instance._TestAsyncErrorColor; } set { Instance._TestAsyncErrorColor = value; } }
        public static Color TestResultUndefinedBackColor { get { return Instance._TestResultUndefinedBackColor; } set { Instance._TestResultUndefinedBackColor = value; } }
        public static Color TestResultCorrectBackColor { get { return Instance._TestResultCorrectBackColor; } set { Instance._TestResultCorrectBackColor = value; } }
        public static Color TestResultErrorBackColor { get { return Instance._TestResultErrorBackColor; } set { Instance._TestResultErrorBackColor = value; } }
        public static Color TestResultMoreErrorsBackColor { get { return Instance._TestResultMoreErrorsBackColor; } set { Instance._TestResultMoreErrorsBackColor = value; } }

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
        private Color _BackgroundColor;
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
        private Color _TestFilesProcessingReadGroupColor;
        private Color _TestFilesExistingGroupColor;
        private Color _TestFilesProcessingSaveGroupColor;
        private Color _TestPhaseSaveShortFileBackColor;
        private Color _TestPhaseSaveLongFileBackColor;
        private Color _TestPhaseReadShortFileBackColor;
        private Color _TestPhaseReadLongFileBackColor;
        private Color _TestAsyncErrorColor;
        private Color _TestResultUndefinedBackColor;
        private Color _TestResultCorrectBackColor;
        private Color _TestResultErrorBackColor;
        private Color _TestResultMoreErrorsBackColor;

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
            switch (palette)
            {
                case PaletteType.Light:
                    _BackgroundColor = Color.FromArgb(240, 248, 255);
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
                    _TestFilesProcessingReadGroupColor = Color.FromArgb(196, 232, 204);
                    _TestFilesExistingGroupColor = Color.FromArgb(226, 160, 232);
                    _TestFilesProcessingSaveGroupColor = Color.FromArgb(200, 140, 210);
                    _TestPhaseSaveShortFileBackColor = Color.FromArgb(255, 190, 190);
                    _TestPhaseSaveLongFileBackColor = Color.FromArgb(255, 220, 220);
                    _TestPhaseReadShortFileBackColor = Color.FromArgb(190, 190, 255);
                    _TestPhaseReadLongFileBackColor = Color.FromArgb(220, 220, 255);
                    _TestAsyncErrorColor = Color.FromArgb(192, 64, 64);
                    _TestResultUndefinedBackColor = Color.FromArgb(216, 216, 216);
                    _TestResultCorrectBackColor = Color.FromArgb(192, 255, 192);
                    _TestResultErrorBackColor = Color.FromArgb(255, 216, 216);
                    _TestResultMoreErrorsBackColor = Color.FromArgb(255, 160, 160);
                    _Palette = palette;
                    break;
                case PaletteType.Pastel:
                    _BackgroundColor = Color.FromArgb(240, 248, 255);
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
                    _TestFilesProcessingReadGroupColor = Color.FromArgb(196, 232, 204);
                    _TestFilesExistingGroupColor = Color.FromArgb(226, 160, 232);
                    _TestFilesProcessingSaveGroupColor = Color.FromArgb(200, 140, 210);
                    _TestPhaseSaveShortFileBackColor = Color.FromArgb(255, 190, 190);
                    _TestPhaseSaveLongFileBackColor = Color.FromArgb(255, 220, 220);
                    _TestPhaseReadShortFileBackColor = Color.FromArgb(190, 190, 255);
                    _TestPhaseReadLongFileBackColor = Color.FromArgb(220, 220, 255);
                    _TestAsyncErrorColor = Color.FromArgb(192, 64, 64);
                    _TestResultUndefinedBackColor = Color.FromArgb(216, 216, 216);
                    _TestResultCorrectBackColor = Color.FromArgb(192, 255, 192);
                    _TestResultErrorBackColor = Color.FromArgb(255, 216, 216);
                    _TestResultMoreErrorsBackColor = Color.FromArgb(255, 160, 160);
                    _Palette = palette;
                    break;
                case PaletteType.Dark:
                    _BackgroundColor = Color.FromArgb(240, 248, 255);
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
                    _TestFilesProcessingReadGroupColor = Color.FromArgb(196, 232, 204);
                    _TestFilesExistingGroupColor = Color.FromArgb(226, 160, 232);
                    _TestFilesProcessingSaveGroupColor = Color.FromArgb(200, 140, 210);
                    _TestPhaseSaveShortFileBackColor = Color.FromArgb(255, 190, 190);
                    _TestPhaseSaveLongFileBackColor = Color.FromArgb(255, 220, 220);
                    _TestPhaseReadShortFileBackColor = Color.FromArgb(190, 190, 255);
                    _TestPhaseReadLongFileBackColor = Color.FromArgb(220, 220, 255);
                    _TestAsyncErrorColor = Color.FromArgb(192, 64, 64);
                    _TestResultUndefinedBackColor = Color.FromArgb(216, 216, 216);
                    _TestResultCorrectBackColor = Color.FromArgb(192, 255, 192);
                    _TestResultErrorBackColor = Color.FromArgb(255, 216, 216);
                    _TestResultMoreErrorsBackColor = Color.FromArgb(255, 160, 160);
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
            using (var brush = CreateBrush3D(color, bounds.Y - 1, bounds.Height))
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
            float bw = bounds.Width;
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
        /// Vrátí SolidBrush. Je k dispozici ihned, ale nesmí se Disposovat.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Brush GetSolidBrush(Color color)
        {
            if (_SolidBrush is null) _SolidBrush = new SolidBrush(Color.White);
            _SolidBrush.Color = color;
            return _SolidBrush;
        }
        private static SolidBrush _SolidBrush;
        /// <summary>
        /// Vrátí 3D Brush. Je vyroben na zakázku a musí se Disposovat.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Brush CreateBrush3D(Color color, int y, int height)
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
