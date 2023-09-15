using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;
using static DjSoft.Tools.ProgramLauncher.App;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class DataItemGroup : DataItemBase
    {
    }
    public class DataItemApplication : DataItemBase
    {
    }
    public abstract class DataItemBase : IChildOfParent<InteractiveGraphicsControl>
    {
        /// <summary>
        /// Pozice prvku v matici X/Y
        /// </summary>
        public Point Adress { get; set; }

        public virtual string MainTitle { get; set; }
        public virtual string ImageName { get; set; }
        public virtual byte[] ImageContent { get; set; }

        #region Vztah na Parenta = EditablePanel, a z něj navázané údaje
        /// <summary>
        /// Odkaz na Parenta
        /// </summary>
        protected InteractiveGraphicsControl Parent { get { return __Parent; } }
        /// <summary>
        /// Obsahuje true když je umístěn na Parentu
        /// </summary>
        protected bool HasParent { get { return __Parent != null; } }
        /// <summary>
        /// Definice layoutu, převzatá z Parenta. Parent je <see cref="InteractiveGraphicsControl"/>, ten má svůj daný layout.
        /// </summary>
        protected virtual DataLayout DataLayout { get { return __Parent?.DataLayout; } }
        InteractiveGraphicsControl IChildOfParent<InteractiveGraphicsControl>.Parent { get { return __Parent; } set { __Parent = value; } }
        private InteractiveGraphicsControl __Parent;
        #endregion
        #region Údaje získané z Layoutu
        /// <summary>
        /// Souřadnice ve virtuálním prostoru
        /// </summary>
        public virtual Point VirtualLocation 
        {
            get
            {
                var adress = this.Adress;
                var size = this.DataLayout.CellSize;
                return new Point(adress.X * size.Width, adress.Y * size.Height);
            }
        }
        /// <summary>
        /// Vnější velikost objektu.
        /// Tyto velikosti jednotlivých objektů na sebe těsně navazují.
        /// Objekt by do této velikosti měl zahrnout i mezery (okraje) mezi sousedními objekty.
        /// Pokud konkrétní potomek neřeší výšku nebo šířku, může v dané hodnotě nechat 0.
        /// </summary>
        public virtual Size Size { get { return this.DataLayout.CellSize; } }

        #endregion
        #region Interaktivita

        #endregion
        #region Kreslení
        /// <summary>
        /// Zajistí vykreslení prvku
        /// </summary>
        /// <param name="e"></param>
        public virtual void Paint(PaintDataEventArgs e)
        {
            if (!HasParent) return;

            var dataLayout = this.DataLayout;
            var location = this.VirtualLocation;
            var activeBounds = dataLayout.ContentBounds.GetShiftedRectangle(location);

            var mousePoint = e.MouseState.LocationNative;
            bool hasMouse = activeBounds.Contains(mousePoint);
            InteractiveState state = (!hasMouse ? InteractiveState.Enabled : (e.MouseState.Buttons == MouseButtons.None ? InteractiveState.MouseOn : InteractiveState.MouseDown));

            e.Graphics.SetClip(activeBounds);

            Color color;

            // Podkreslení celé buňky v myšoaktivním stavu:
            if ((state == InteractiveState.MouseOn || state == InteractiveState.MouseDown) && dataLayout.ContentColor != null)
            {
                e.Graphics.FountainFill(activeBounds, dataLayout.ContentColor.GetColor(state));
            }

            var borderBounds = dataLayout.BorderBounds.GetShiftedRectangle(location);
            if (borderBounds.HasContent())
            {
                using (var borderPath = borderBounds.GetRoundedRectanglePath(dataLayout.BorderRound))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Výplň dáme pod border:
                    color = dataLayout.ButtonBackColors.GetColor(state);
                    e.Graphics.FountainFill(borderPath, color, state);

                    // Linka Border:
                    if (dataLayout.BorderWidth > 0f)
                        e.Graphics.DrawPath(App.GetPen(dataLayout.BorderLineColors, state, dataLayout.BorderWidth), borderPath);
                }
            }

            // Zvýraznit pozici myši:
            if (hasMouse && dataLayout.MouseHighlightSize.HasContent())
            {
                using (GraphicsPath mousePath = new GraphicsPath())
                {
                    var mouseBounds = mousePoint.GetRectangleFromCenter(dataLayout.MouseHighlightSize);
                    new Rectangle(mousePoint.X - 24, mousePoint.Y - 16, 48, 32);
                    mousePath.AddEllipse(mouseBounds);
                    using (System.Drawing.Drawing2D.PathGradientBrush pgb = new PathGradientBrush(mousePath))       // points
                    {
                        pgb.CenterPoint = mousePoint;
                        pgb.CenterColor = dataLayout.ButtonBackColors.MouseHighlightColor;
                        pgb.SurroundColors = new Color[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                        e.Graphics.FillPath(pgb, mousePath);
                    }
                }
            }

            // Vykreslit Image:
            var image = DjSoft.Tools.ProgramLauncher.Properties.Resources.system_settings_2_48;
            if (dataLayout.ImageBounds.HasContent() && image != null)
            {
                e.Graphics.ResetClip();
                e.Graphics.SmoothingMode = SmoothingMode.None;
                var imageBounds = dataLayout.ImageBounds.GetShiftedRectangle(location);
                e.Graphics.DrawImage(image, imageBounds);
            }

            // Vypsat text:
            if (dataLayout.MainTitleBounds.HasContent() && !String.IsNullOrEmpty(this.MainTitle))
            {
                var mainTitleBounds = dataLayout.MainTitleBounds.GetShiftedRectangle(location);
                e.Graphics.DrawText(this.MainTitle, mainTitleBounds, dataLayout.MainTitleAppearance);

            }

            e.Graphics.ResetClip();
        }
        #endregion
       
    }

    #region class DataLayout
    /// <summary>
    /// Definice vzhledu
    /// </summary>
    public class DataLayout
    {
        #region Public properties
        /// <summary>
        /// Jméno stylu
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Barva statického pozadí pod všemi prvky = celé okno
        /// </summary>
        public Color WorkspaceColor { get; set; }
        /// <summary>
        /// Velikost celé buňky
        /// </summary>
        public Size CellSize { get; set; }
        /// <summary>
        /// Souřadnice aktivního prostoru pro data: v tomto prostoru je obsah myšo-aktivní.
        /// Vnější prostor okolo těchto souřadnic je prázdný a neaktivní, odděluje od sebe sousední buňky.
        /// <para/>
        /// V tomto prostoru se stínuje pozice myši barvou <see cref="ButtonBackColors"/> : <see cref="ColorSet.MouseHighlightColor"/>.
        /// </summary>
        public Rectangle ContentBounds { get; set; }
        /// <summary>
        /// Barvy aktivního prostoru. Nepoužívá se pro stav Enabled a Disabled, pouze MouseOn a MouseDown.
        /// </summary>
        public ColorSet ContentColor { get; set; }
        /// <summary>
        /// Souřadnice prostoru s okrajem a vykresleným pozadím.
        /// V tomto prostoru je použita barva <see cref="BorderLineColors"/> a <see cref="ButtonBackColors"/>, 
        /// border má šířku <see cref="BorderWidth"/> a kulaté rohy <see cref="BorderRound"/>.
        /// <para/>
        /// Texty mohou být i mimo tento prostor.
        /// </summary>
        public Rectangle BorderBounds { get; set; }
        /// <summary>
        /// Zaoblení Borderu, 0 = ostře hranatý
        /// </summary>
        public int BorderRound { get; set; }
        /// <summary>
        /// Šířka linky Borderu, 0 = nekreslí se
        /// </summary>
        public float BorderWidth { get; set; }
        /// <summary>
        /// Sada barev pro čáru Border, kreslí se když <see cref="BorderWidth"/> je kladné
        /// </summary>
        public ColorSet BorderLineColors { get; set; }
        /// <summary>
        /// Sada barev pro pozadí pod buttonem, ohraničený prostorem Border
        /// </summary>
        public ColorSet ButtonBackColors { get; set; }
        /// <summary>
        /// Souřadnice prostoru pro ikonu
        /// </summary>
        public Rectangle ImageBounds { get; set; }
        /// <summary>
        /// Velikost prostoru stínování myši, lze zakázat zadáním prázdného prostoru
        /// </summary>
        public Size MouseHighlightSize { get; set; }

        /// <summary>
        /// Souřadnice prostoru pro hlavní text
        /// </summary>
        public Rectangle MainTitleBounds { get; set; }
        /// <summary>
        /// Vzhled hlavního textu
        /// </summary>
        public TextAppearance MainTitleAppearance { get; set; }
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// STředně velký obdélník
        /// </summary>
        public static DataLayout SetMediumBrick
        {
            get
            {
                int a0 = 40;
                int a1 = 80;
                int a2 = 120;
                int a3 = 160;
                int a4 = 220;
                int b1 = 180;
                int b2 = 200;
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Střední cihla",
                    CellSize = new Size(180, 92),
                    ContentBounds = new Rectangle(4, 4, 173, 85),
                    BorderBounds = new Rectangle(14, 14, 64, 64),
                    MouseHighlightSize = new Size(48, 32),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(22, 22, 48, 48),
                    WorkspaceColor = Color.FromArgb(64, 68, 72),

                    ContentColor = new ColorSet(
                        Color.Empty,
                        Color.Empty,
                        Color.FromArgb(a0, 200, 200, 230),
                        Color.FromArgb(a0, 180, 180, 210),
                        Color.Empty),
                    BorderLineColors = new ColorSet(
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b1, b1, b1),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2),
                        Color.FromArgb(a1, b2, b2, b2)),
                    ButtonBackColors = new ColorSet(
                        Color.FromArgb(a1, 216, 216, 216),
                        Color.FromArgb(a1, 120, 120, 120),
                        Color.FromArgb(a2, 200, 200, 230),
                        Color.FromArgb(a2, 180, 180, 210),
                        Color.FromArgb(a3, 180, 180, 240)),

                    MainTitleBounds = new Rectangle(82, 18, 95, 20),
                    MainTitleAppearance = new TextAppearance()
                    {
                        FontType = FontType.CaptionFont,
                        SizeRatio = 1.2f,
                        TextColors = new ColorSet(Color.Black)
                    }
                };
                return dataLayout;
            }
        }
        #endregion
    }
    #endregion
    #region class ColorSet
    /// <summary>
    /// Definice barev pro jednu oblast, liší se interaktivitou
    /// </summary>
    public class ColorSet
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ColorSet() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="allColors"></param>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(Color allColors, Color? enabledColor = null, Color? disabledColor = null, Color? mouseOnColor = null, Color? mouseDownColor = null, Color? mouseHighlightColor = null)
        {
            this.EnabledColor = enabledColor ?? allColors;
            this.DisabledColor = disabledColor ?? allColors;
            this.MouseOnColor = mouseOnColor ?? allColors;
            this.MouseDownColor = mouseDownColor ?? allColors;
            this.MouseHighlightColor = mouseHighlightColor ?? allColors;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="enabledColor"></param>
        /// <param name="disabledColor"></param>
        /// <param name="mouseOnColor"></param>
        /// <param name="mouseDownColor"></param>
        /// <param name="mouseHighlightColor"></param>
        public ColorSet(Color enabledColor, Color disabledColor, Color mouseOnColor, Color mouseDownColor, Color mouseHighlightColor)
        {
            this.EnabledColor = enabledColor;
            this.DisabledColor = disabledColor;
            this.MouseOnColor = mouseOnColor;
            this.MouseDownColor = mouseDownColor;
            this.MouseHighlightColor = mouseHighlightColor;
        }
        /// <summary>
        /// Barva ve stavu Enabled = bez myši, ale dostupné
        /// </summary>
        public Color EnabledColor { get; set; }
        /// <summary>
        /// Barva ve stavu Disabled = nedostupné
        /// </summary>
        public Color DisabledColor { get; set; }
        /// <summary>
        /// Barva ve stavu MouseOn = myš je na prvku
        /// </summary>
        public Color MouseOnColor { get; set; }
        /// <summary>
        /// Barva ve stavu MouseDown
        /// </summary>
        public Color MouseDownColor { get; set; }
        /// <summary>
        /// Barva zvýraznění prostoru myši
        /// </summary>
        public Color MouseHighlightColor { get; set; }
        /// <summary>
        /// Vrátí barvu pro daný stav
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Color GetColor(InteractiveState state)
        {
            switch (state)
            {
                case InteractiveState.Enabled: return this.EnabledColor;
                case InteractiveState.Disabled: return this.DisabledColor;
                case InteractiveState.MouseOn: return this.MouseOnColor;
                case InteractiveState.MouseDown: return this.MouseDownColor;
            }
            return this.EnabledColor;
        }
    }
    #endregion
    #region class TextAppearance
    /// <summary>
    /// Vzhled textu - font, styl, velikost
    /// </summary>
    public class TextAppearance
    {
        /// <summary>
        /// Typ systémového fontu
        /// </summary>
        public FontType? FontType { get; set; }
        /// <summary>
        /// Explicitně daná velikost, není optimální ji defiovat explicitně
        /// </summary>
        public float? EmSize { get; set; }
        /// <summary>
        /// Poměr velikosti aktuálního fontu ku fontu defaultnímu daného typu
        /// </summary>
        public float? SizeRatio { get; set; }
        /// <summary>
        /// Styl fontu; default = dle systémového fontu
        /// </summary>
        public FontStyle? FontStyle { get; set; }
        public ContentAlignment? TextAlignment { get; set; }
        /// <summary>
        /// Barvy písma
        /// </summary>
        public ColorSet TextColors { get; set; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Stav prvku
    /// </summary>
    public enum InteractiveState
    {
        None = 0,
        Disabled,
        Enabled,
        MouseOn,
        MouseDown
    }
    #endregion
    #region class PaintDataEventArgs
    /// <summary>
    /// Argument pro kreslení dat
    /// </summary>
    public class PaintDataEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="e"></param>
        /// <param name="mouseState"></param>
        /// <param name="virtualContainer"></param>
        public PaintDataEventArgs(PaintEventArgs e, Components.MouseState mouseState, Components.IVirtualContainer virtualContainer)
        {
            __Graphics = new WeakReference<Graphics>(e.Graphics);
            __ClipRectangle = e.ClipRectangle;
            __MouseState = mouseState;
            __VirtualContainer = virtualContainer;
        }
        private WeakReference<Graphics> __Graphics;
        private Rectangle __ClipRectangle;
        private Components.MouseState __MouseState;
        private Components.IVirtualContainer __VirtualContainer;
        void IDisposable.Dispose()
        {
            __Graphics = null;
        }
        /// <summary>
        /// Gets the graphics used to paint. <br/>
        /// The System.Drawing.Graphics object used to paint. The System.Drawing.Graphics object provides methods for drawing objects on the display device.
        /// </summary>
        public Graphics Graphics { get { return (__Graphics.TryGetTarget(out var graphics) ? graphics : null); } }
        /// <summary>
        /// Gets the rectangle in which to paint. <br/>
        /// The System.Drawing.Rectangle in which to paint.
        /// </summary>
        public Rectangle ClipRectangle { get { return __ClipRectangle; } }
        /// <summary>
        /// Pozice a stav myši
        /// </summary>
        public Components.MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Virtuální kontejner, do kterého je kresleno
        /// </summary>
        public Components.IVirtualContainer VirtualContainer { get { return __VirtualContainer; } }
    }
    #endregion
}
