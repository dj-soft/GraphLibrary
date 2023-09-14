using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using DjSoft.Tools.ProgramLauncher.Components;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    public class DataItemGroup : DataItemBase
    {
    }
    public class DataItemApplication : DataItemBase
    {
    }
    public abstract class DataItemBase : IChildOfParent<EditablePanel>
    {
        /// <summary>
        /// Pozice prvku v matici X/Y
        /// </summary>
        public Point Adress { get; set; }

        public virtual string ImageName { get; set; }
        public virtual byte[] ImageContent { get; set; }

        #region Vztah na Parenta = EditablePanel, a z něj navázané údaje
        /// <summary>
        /// Odkaz na Parenta
        /// </summary>
        protected EditablePanel Parent { get { return __Parent; } }
        /// <summary>
        /// Obsahuje true když je umístěn na Parentu
        /// </summary>
        protected bool HasParent { get { return __Parent != null; } }
        /// <summary>
        /// Definice layoutu, převzatá z Parenta. Parent je <see cref="EditablePanel"/>, ten má svůj daný layout.
        /// </summary>
        protected virtual DataLayout DataLayout { get { return __Parent?.DataLayout; } }
        EditablePanel IChildOfParent<EditablePanel>.Parent { get { return __Parent; } set { __Parent = value; } }
        private EditablePanel __Parent;
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
            var bounds = new Rectangle(this.VirtualLocation, this.Size);

            e.Graphics.SetClip(bounds);


            var mousePoint = e.MouseState.LocationNative;
            bool hasMouse = bounds.Contains(mousePoint);
            InteractiveState state = (!hasMouse ? InteractiveState.Enabled : (e.MouseState.Buttons == MouseButtons.None ? InteractiveState.MouseOn : InteractiveState.MouseDown));
            Color color;

            var innerBounds = RectangleAdd(dataLayout.ContentBounds, bounds.Location);
            using (var borderPath = GetRoundedRectangle(innerBounds, 8))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Výplň pod border:
                color = dataLayout.ButtonBackColors.GetColor(state);
                using (var brush = new SolidBrush(color))
                    e.Graphics.FillPath(brush, borderPath);

                // Border:
                if (dataLayout.BorderWidth > 0f)
                {
                    color = dataLayout.BorderLineColors.GetColor(state);
                    using (var pen = new Pen(color, dataLayout.BorderWidth))
                        e.Graphics.DrawPath(pen, borderPath);
                }

                // Zvýraznit pozici myši:
                if (hasMouse)
                {
                    e.Graphics.SetClip(innerBounds);
                    using (GraphicsPath mousePath = new GraphicsPath())
                    {
                        var mouseBounds = new Rectangle(mousePoint.X - 24, mousePoint.Y - 16, 48, 32);
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
                e.Graphics.ResetClip();
                e.Graphics.SmoothingMode = SmoothingMode.None;
                var image = DjSoft.Tools.ProgramLauncher.Properties.Resources.system_settings_2_48;
                var imageBounds = RectangleAdd(dataLayout.ImageBounds, bounds.Location);
                e.Graphics.DrawImage(image, imageBounds);
            }

            e.Graphics.ResetClip();
        }
        #endregion
        #region Kreslící support
        protected Rectangle GetInnerBounds(Rectangle bounds, int dx, int dy)
        {
            int dw = dx + dx;
            int dh = dy + dy;
            if (bounds.Width <= dw || bounds.Height <= dh) return Rectangle.Empty;
            return new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width - dw, bounds.Height - dh);
        }
        protected GraphicsPath GetRoundedRectangle(Rectangle bounds, int round)
        {
            GraphicsPath gp = new GraphicsPath();

            int minDim = (bounds.Width < bounds.Height ? bounds.Width : bounds.Height);
            int minRound = minDim / 3;
            if (round >= minRound) round = minRound;
            if (round <= 1)
            {   // Malý prostor nebo malý Round => bude to Rectangle
                gp.AddRectangle(bounds);
            }
            else
            {   // Máme bounds = vnější prostor, po něm jdou linie
                // a roundBounds = vnitřní prostor, určuje souřadnice začátku oblouku (Round):
                var roundBounds = GetInnerBounds(bounds, round, round);
                gp.AddLine(roundBounds.Left, bounds.Top, roundBounds.Right, bounds.Top);                                                                       // Horní rovná linka zleva doprava, její Left a Right jsou z Round souřadnic
                gp.AddBezier(roundBounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, bounds.Top, bounds.Right, roundBounds.Top);                // Pravý horní oblouk doprava a dolů
                gp.AddLine(bounds.Right, roundBounds.Top, bounds.Right, roundBounds.Bottom);                                                                   // Pravá rovná linka zhora dolů
                gp.AddBezier(bounds.Right, roundBounds.Bottom, bounds.Right, bounds.Bottom, bounds.Right, bounds.Bottom, roundBounds.Right, bounds.Bottom);    // Pravý dolní oblouk dolů a doleva
                gp.AddLine(roundBounds.Right, bounds.Bottom, roundBounds.Left, bounds.Bottom);                                                                 // Dolní rovná linka zprava doleva
                gp.AddBezier(roundBounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, bounds.Bottom, bounds.Left, roundBounds.Bottom);        // Levý dolní oblouk doleva a nahoru
                gp.AddLine(bounds.Left, roundBounds.Bottom, bounds.Left, roundBounds.Top);                                                                     // Levá rovná linka zdola nahoru
                gp.AddBezier(bounds.Left, roundBounds.Top, bounds.Left, bounds.Top, bounds.Left, bounds.Top, roundBounds.Left, bounds.Top);                    // Levý horní oblouk nahoru a doprava
                gp.CloseFigure();
            }
            return gp;
        }
        protected Rectangle RectangleAdd(Rectangle bounds, Point offset)
        {
            return new Rectangle(bounds.X + offset.X, bounds.Y + offset.Y, bounds.Width, bounds.Height);
        }
        #endregion
    }

    /// <summary>
    /// Definice vzhledu
    /// </summary>
    public class DataLayout
    {
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
        /// Souřadnice prostoru pro data: v tomto prostoru se vykresluje Border, v tomto prostoru je obsah myšo-aktivní.
        /// Okolní prostor odděluje sousední buňky.
        /// </summary>
        public Rectangle ContentBounds { get; set; }
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
        /// Souřadnice prostoru pro hlavní text
        /// </summary>
        public Rectangle MainTitleBounds { get; set; }



        #region Statické konstruktory konkrétních stylů
        public static DataLayout SetMediumBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Střední cihla",
                    WorkspaceColor = Color.FromArgb(64, 68, 72),
                    CellSize = new Size(160, 60),
                    ContentBounds = new Rectangle(6, 6, 148, 48),
                    BorderRound = 6,
                    BorderWidth = 1f,
                    BorderLineColors = new ColorSet(Color.FromArgb(160, 160, 160), Color.FromArgb(160, 160, 160), Color.FromArgb(160, 160, 160), Color.FromArgb(160, 160, 160), Color.FromArgb(160, 160, 160)),
                    ButtonBackColors = new ColorSet(Color.FromArgb(192, 216, 216, 216), Color.FromArgb(192, 120, 120, 120), Color.FromArgb(190, 190, 120), Color.FromArgb(190, 160, 190), Color.FromArgb(240, 200, 240)),
                    ImageBounds = new Rectangle(6, 6, 48, 48)
                };
                return dataLayout;
            }
        }
        #endregion

    }
    #region class PaintDataEventArgs
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
    #region class PaintDataEventArgs
    /// <summary>
    /// Vzhled textu - font, styl, velikost
    /// </summary>
    public class TextAppearance
    {
        public FontStyle FontStyle { get; set; }
        public float FontSize { get; set; }
        public ContentAlignment TextAlignment { get; set; }
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
