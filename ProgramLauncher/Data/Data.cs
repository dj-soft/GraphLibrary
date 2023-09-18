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

        /// <summary>
        /// Příznak že prvek je aktivní. Pak používá pro své vlastní pozadí barvu <see cref="ColorSet.ActiveColor"/>
        /// </summary>
        public virtual bool IsActive { get; set; }
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
        InteractiveGraphicsControl IChildOfParent<InteractiveGraphicsControl>.Parent { get { return __Parent; } set { __Parent = value; } }
        private InteractiveGraphicsControl __Parent;
        #endregion
        #region Údaje získané z Layoutu
        /// <summary>
        /// Definice layoutu: buď je lokální (specifická), anebo převzatá z Parenta. 
        /// Parent je <see cref="InteractiveGraphicsControl"/>, ten má svůj daný layout.
        /// <para/>
        /// Definici lze setovat, pak má přednost před definicí z Parenta. Lze setovat null.
        /// </summary>
        protected virtual DataLayout DataLayout 
        {
            get { return __DataLayout ?? __Parent?.DataLayout; } 
            set { __DataLayout = value; }
        }
        private DataLayout __DataLayout;
        /// <summary>
        /// Souřadnice počátku prvku ve virtuálním prostoru
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
        /// Souřadnice celého prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="DataLayout.CellSize"/>
        /// </summary>
        public virtual Rectangle VirtualBounds
        {
            get
            {
                var adress = this.Adress;
                var size = this.DataLayout.CellSize;
                return new Rectangle(new Point(adress.X * size.Width, adress.Y * size.Height), size);
            }
        }
        /// <summary>
        /// Souřadnice vnitřního aktivního prostoru tohoto prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="DataLayout.CellSize"/>
        /// </summary>
        public virtual Rectangle VirtualContentBounds
        {
            get
            {
                var dataLayout = this.DataLayout;
                var location = this.VirtualLocation;
                var virtualContentBounds = dataLayout.ContentBounds.GetShiftedRectangle(location);
                return virtualContentBounds;
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
        /// <summary>
        /// Vrátí true, pokud tento prvek má svoji virtuální aktivní plochu na daném virtuálním bodu.
        /// Virtuální = v souřadném systému datových prvků, nikoli pixely vizuálního controlu. Mezi tím existuje posunutí dané Scrollbary.
        /// </summary>
        /// <param name="virtualPoint"></param>
        /// <returns></returns>
        public bool IsActiveOnVirtualPoint(Point virtualPoint)
        {
            var virtualContentBounds = this.VirtualContentBounds;
            return virtualContentBounds.Contains(virtualPoint);
        }
        #endregion
        #region Kreslení
        /// <summary>
        /// Interaktivní stav. Nastavuje Control, prvek na něj jen reaguje.
        /// </summary>
        public virtual InteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Zajistí vykreslení prvku
        /// </summary>
        /// <param name="e"></param>
        public virtual void Paint(PaintDataEventArgs e)
        {
            if (!HasParent) return;

            var dataLayout = this.DataLayout;
            var paletteSet = App.CurrentPalette;
            var virtualLocation = this.VirtualLocation;
            var clientLocation = this.Parent.GetControlPoint(virtualLocation);
            var activeBounds = dataLayout.ContentBounds.GetShiftedRectangle(clientLocation);

            e.Graphics.SetClip(activeBounds);

            InteractiveState interactiveState = this.InteractiveState;

            // Pozadí aktivní buňky:
            if (this.IsActive)
            {
                var color = paletteSet.ActiveContentColor.ActiveColor;
                if (color.HasValue)
                    e.Graphics.FillRectangle(activeBounds, color.Value);
            }

            // Podkreslení celé buňky v myšoaktivním stavu:
            if ((interactiveState == InteractiveState.MouseOn || interactiveState == InteractiveState.MouseDown) && paletteSet.ActiveContentColor != null)
            {
                var color = paletteSet.ActiveContentColor.GetColor(interactiveState);
                if (color.HasValue)
                    e.Graphics.FountainFill(activeBounds, color.Value);
            }

            // Rámeček a pozadí typu Border:
            var borderBounds = dataLayout.BorderBounds.GetShiftedRectangle(clientLocation);
            if (borderBounds.HasContent())
            {
                using (var borderPath = borderBounds.GetRoundedRectanglePath(dataLayout.BorderRound))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Výplň dáme pod border:
                    var color = paletteSet.ButtonBackColors.GetColor(interactiveState);
                    if (color.HasValue)
                        e.Graphics.FountainFill(borderPath, color.Value, interactiveState);

                    // Linka Border:
                    if (dataLayout.BorderWidth > 0f)
                    {
                        var pen = App.GetPen(paletteSet.BorderLineColors, interactiveState, dataLayout.BorderWidth);
                        if (pen != null) e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            }

            // Zvýraznit pozici myši:
            if (interactiveState == InteractiveState.MouseOn && dataLayout.MouseHighlightSize.HasContent() && paletteSet.ButtonBackColors.MouseHighlightColor.HasValue)
            {
                using (GraphicsPath mousePath = new GraphicsPath())
                {
                    var mousePoint = e.MouseState.LocationControl;
                    var mouseBounds = mousePoint.GetRectangleFromCenter(dataLayout.MouseHighlightSize);
                    new Rectangle(mousePoint.X - 24, mousePoint.Y - 16, 48, 32);
                    mousePath.AddEllipse(mouseBounds);
                    using (System.Drawing.Drawing2D.PathGradientBrush pgb = new PathGradientBrush(mousePath))       // points
                    {
                        pgb.CenterPoint = mousePoint;
                        pgb.CenterColor = paletteSet.ButtonBackColors.MouseHighlightColor.Value;
                        pgb.SurroundColors = new Color[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                        e.Graphics.FillPath(pgb, mousePath);
                    }
                }
            }

            // Vykreslit Image:
            var image = App.GetImage(this.ImageName, this.ImageContent);
            if (dataLayout.ImageBounds.HasContent() && image != null)
            {
                e.Graphics.ResetClip();
                e.Graphics.SmoothingMode = SmoothingMode.None;
                var imageBounds = dataLayout.ImageBounds.GetShiftedRectangle(clientLocation);
                e.Graphics.DrawImage(image, imageBounds);
            }

            // Vypsat text:
            if (dataLayout.MainTitleBounds.HasContent() && !String.IsNullOrEmpty(this.MainTitle))
            {
                var mainTitleBounds = dataLayout.MainTitleBounds.GetShiftedRectangle(clientLocation);
                e.Graphics.DrawText(this.MainTitle, mainTitleBounds, dataLayout.MainTitleAppearance, interactiveState);
            }

            e.Graphics.ResetClip();
        }
        #endregion
       
    }

    #region class DataLayout = Layout prvku: rozmístění, velikost, styl písma
    /// <summary>
    /// Layout prvku: rozmístění, velikost, styl písma
    /// </summary>
    public class DataLayout
    {
        #region Public properties
        /// <summary>
        /// Jméno stylu
        /// </summary>
        public string Name { get; set; }
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
        public TextAppearance MainTitleAppearance { get { return __MainTitleAppearance ?? App.CurrentPalette.MainTitleAppearance; } set { __MainTitleAppearance = value; } } private TextAppearance __MainTitleAppearance;
        #endregion
        #region Statické konstruktory konkrétních stylů
        /// <summary>
        /// Menší obdélník
        /// </summary>
        public static DataLayout SetSmallBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 48),
                    ContentBounds = new Rectangle(2, 2, 156, 44),
                    BorderBounds = new Rectangle(4, 4, 40, 40),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(8, 8, 24, 24),
                    MainTitleBounds = new Rectangle(46, 14, 95, 20),
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Střední obdélník
        /// </summary>
        public static DataLayout SetMidiBrick
        {
            get
            {
                DataLayout dataLayout = new DataLayout()
                {
                    Name = "Menší cihla",
                    CellSize = new Size(160, 64),
                    ContentBounds = new Rectangle(2, 2, 156, 60),
                    BorderBounds = new Rectangle(4, 4, 56, 56),
                    MouseHighlightSize = new Size(40, 24),
                    BorderRound = 4,
                    BorderWidth = 1f,
                    ImageBounds = new Rectangle(8, 8, 48, 48),
                    MainTitleBounds = new Rectangle(62, 24, 95, 20),
                };
                return dataLayout;
            }
        }
        /// <summary>
        /// Středně velký obdélník
        /// </summary>
        public static DataLayout SetMediumBrick
        {
            get
            {
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
                    MainTitleBounds = new Rectangle(82, 18, 95, 20),
                };
                return dataLayout;
            }
        }
        #endregion
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Stav prvku
    /// </summary>
    public enum InteractiveState
    {
        Default = 0,
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
