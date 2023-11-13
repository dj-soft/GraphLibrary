using DjSoft.Tools.ProgramLauncher.Data;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    /// <summary>
    /// Třída reprezentující jeden vizuální prvek v rámci interaktivního controlu <see cref="InteractiveGraphicsControl"/>
    /// </summary>
    public class InteractiveItem : IChildOfParent<InteractiveGraphicsControl>
    {
        public InteractiveItem()
        {
            __Visible = true;
            __Enabled = true;
            __Interactive = true;
        }
        #region Public zobrazovaná data
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.MainTitle};  Adress: [{this.Adress.X},{this.Adress.Y}]; Layout: '{this.DataLayout?.Name}'";
        }
        /// <summary>
        /// Pozice prvku v matici X/Y
        /// </summary>
        public virtual Point Adress { get { return __Adress; } set { __Adress = value; ResetParentLayout(); } } private Point __Adress;
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public virtual bool Visible { get { return __Visible; } set { __Visible = value; } } private bool __Visible;
        /// <summary>
        /// Prvek je Enabled (false = Disabled)?
        /// </summary>
        public virtual bool Enabled { get { return __Enabled; } set { __Enabled = value; } } private bool __Enabled;
        /// <summary>
        /// Prvek je interaktivní?
        /// </summary>
        public virtual bool Interactive { get { return __Interactive; } set { __Interactive = value; } } private bool __Interactive;
        /// <summary>
        /// Statická podkladová barva pozadí, specifická pro tento prvek, teprve na ni se nanáší barva určená z <see cref="CellBackColor"/> (která je proměnná podle <see cref="InteractiveState"/>)
        /// </summary>
        public virtual Color? BackColor { get { return __BackColor; } set { __BackColor = value; } } private Color? __BackColor;
        /// <summary>
        /// Barvy pozadí celé buňky. Pokud obsahuje null, nekreslí se.
        /// </summary>
        public virtual ColorSet CellBackColor { get { return __CellBackColor ?? App.CurrentAppearance.CellBackColor; } set { __CellBackColor = value; } } private ColorSet __CellBackColor;
        /// <summary>
        /// Main titulek
        /// </summary>
        public virtual string MainTitle { get { return __MainTitle; } set { __MainTitle = value; } } private string __MainTitle;
        /// <summary>
        /// Popisek
        /// </summary>
        public virtual string Description { get { return __Description; } set { __Description = value; } } private string __Description;
        /// <summary>
        /// Text do ToolTipu
        /// </summary>
        public virtual string ToolTipText { get { return this.__ToolTipText; } set { this.__ToolTipText = value; } } private string __ToolTipText;
        /// <summary>
        /// Příznak že prvek je "zamáčknutý" (jakoby aktivovaný button). Pak používá pro své vlastní pozadí barvu <see cref="ColorSet.DownColor"/>
        /// </summary>
        public virtual bool IsDown { get { return __IsDown; } set { __IsDown = value; } } private bool __IsDown;
        /// <summary>
        /// Jméno obrázku
        /// </summary>
        public virtual string ImageName { get { return __ImageName; } set { __ImageName = value; } } private string __ImageName;
        /// <summary>
        /// Data obrázku
        /// </summary>
        public virtual byte[] ImageContent { get { return __ImageContent; } set { __ImageContent = value; } } private byte[] __ImageContent;
        /// <summary>
        /// Prostor pro definiční data tohoto prvku
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Prostor pro dočasné poznámky o tomto prvku
        /// </summary>
        public object Tag { get; set; }
        #endregion
        #region Vztah na Parenta = InteractiveGraphicsControl, a z něj navázané údaje
        /// <summary>
        /// Zajistí znovuvykreslení vizuálního controlu
        /// </summary>
        public void Refresh()
        {
            Parent?.Draw();
        }
        /// <summary>
        /// Odkaz na Parenta
        /// </summary>
        protected InteractiveGraphicsControl Parent { get { return __Parent; } }
        /// <summary>
        /// Obsahuje true když je umístěn na Parentu
        /// </summary>
        protected bool HasParent { get { return __Parent != null; } }
        /// <summary>
        /// ZOrder tohoto prvku. Určuje Parent na základě aktivity prvku.
        /// </summary>
        public int ZOrder { get { return (__Parent?.GetZOrder(this) ?? 0); } }
        /// <summary>
        /// Obsahuje true, pokud this prvek je vybrán v parentu v poli <see cref="InteractiveGraphicsControl.SelectedItems"/>.
        /// </summary>
        public bool IsSelected { get { return (__Parent?.GetIsSelected(this) ?? false); } }
        /// <summary>
        /// Zruší platnost layoutu jednotlivých prvků přítomných v Parentu
        /// </summary>
        protected virtual void ResetParentLayout()
        {
            Parent?.ResetItemLayout();
        }
        InteractiveGraphicsControl IChildOfParent<InteractiveGraphicsControl>.Parent { get { return __Parent; } set { __Parent = value; } } private InteractiveGraphicsControl __Parent;
        #endregion
        #region Údaje získané z Layoutu
        /// <summary>
        /// Definice layoutu: buď je lokální (specifická) podle <see cref="LayoutKind"/>, 
        /// anebo převzatá z Parenta (ten má svůj <see cref="InteractiveGraphicsControl.DefaultLayoutKind"/>).
        /// </summary>
        public virtual LayoutItemInfo DataLayout { get { return __Parent?.GetLayout(LayoutKind); } }
        /// <summary>
        /// Druh Layoutu tohoto prvku. Default = null = přebírá se z panelu, na kterém je umístěn.
        /// <para/>
        /// Definici lze setovat, pak má přednost před definicí z Parenta. Lze setovat null, tím se vrátíme k defaultní z Parenta.
        /// </summary>
        public virtual DataLayoutKind? LayoutKind { get { return __LayoutKind; } set { __LayoutKind = value; } } private DataLayoutKind? __LayoutKind;
        /// <summary>
        /// Souřadnice celého prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="InteractiveItem.CellSize"/>.
        /// Pokud prvek nemá správnou adresu <see cref="Adress"/> (záporné hodnoty), pak má <see cref="VirtualBounds"/> = null! Pak nebude ani interaktivní.
        /// Pokud prvek má adresu OK, pak má <see cref="VirtualBounds"/> přidělenou i když jeho <see cref="Visible"/> by bylo false.
        /// </summary>
        public virtual Rectangle? VirtualBounds { get { return __VirtualBounds; } set { __VirtualBounds = value; } } private Rectangle? __VirtualBounds;
        /// <summary>
        /// Velikost celé buňky.
        /// Základ pro tvorbu layoutu = poskládání jednotlivých prvků do matice v controlu. Používá se společně s adresou buňky <see cref="InteractiveItem.Adress"/>.
        /// <para/>
        /// Může mít zápornou šířku, pak obsazuje disponibilní šířku v controlu ("Spring").
        /// V případě, že určitý řádek (prvky na stejné adrese X) obsahuje pouze takové prvky, jejichž <see cref="CellSize"/>.Width je záporné, pak tyto prvky obsadí celou šířku, 
        /// která je určena těmi řádky, které neobsahují žádné "Spring" prvky.
        /// Pokud žádný řádek není celý složený z Fixed prvků (celý layout je Spring), pak se fyzická šířka určuje z defaultního layoutu a maximálního počtu prvků v řádku (Max(Adress.X)).
        /// <para/>
        /// Nelze odvozovat šířku celého řádku od vizuálního controlu, vždy jen od fixních prvků.
        /// </summary>
        public Size CellSize { get { return __CellSize ?? this.DataLayout?.CellSize ?? Size.Empty; } set { __CellSize = value; ResetParentLayout(); } } private Size? __CellSize;
        /// <summary>
        /// Souřadnice vnitřního aktivního prostoru tohoto prvku ve virtuálním prostoru (tj. velikost odpovídá <see cref="LayoutItemInfo.CellSize"/>.
        /// Pokud prvek nemá správnou adresu <see cref="Adress"/> (záporné hodnoty), pak má <see cref="VirtualBounds"/> = null! Pak nebude ani interaktivní.
        /// Pokud prvek má adresu OK, pak má <see cref="VirtualBounds"/> přidělenou i když jeho <see cref="Visible"/> by bylo false.
        /// </summary>
        public virtual Rectangle? VirtualContentBounds
        {
            get
            {
                Rectangle? virtualContentBounds = null;
                var virtualBounds = this.VirtualBounds;
                if (virtualBounds.HasValue)
                {
                    var dataLayout = this.DataLayout;
                    virtualContentBounds = dataLayout.ContentBounds.GetBounds(virtualBounds.Value);
                }
                return virtualContentBounds;
            }
        }
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
            return virtualContentBounds.HasValue && virtualContentBounds.Value.Contains(virtualPoint);
        }
        #endregion
        #region Kreslení
        /// <summary>
        /// Interaktivní stav.
        /// Nastavuje Control, prvek na něj jen reaguje. V této hodnotě se neobjevuje příznak Selected.
        /// </summary>
        public virtual InteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Platný interaktivní stav. Vychází z hodnoty <see cref="InteractiveState"/>, ale promítá do ní i <see cref="Enabled"/> (tedy stav <see cref="InteractiveState.Disabled"/>)
        /// a <see cref="IsSelected"/> a <see cref="IsDown"/>.
        /// </summary>
        public virtual InteractiveState CurrentInteractiveState 
        {
            get
            {
                if (!this.Enabled) return InteractiveState.Disabled;
                var state = this.InteractiveState;
                if (this.IsSelected) state |= InteractiveState.AndSelected;
                if (this.IsDown) state |= InteractiveState.AndDown;
                return state;
            }
        }
        /// <summary>
        /// Zajistí vykreslení prvku
        /// </summary>
        /// <param name="e"></param>
        public virtual void Paint(PaintDataEventArgs e)
        {
            if (!HasParent || !Visible) return;

            bool paintGhost = (e.MouseDragState == MouseDragState.MouseDragActiveCurrent);         // true => kreslíme "ducha" = prvek, který je přesouván, má určitou průhlednost / nebo jen rámeček?
            var virtualBounds = (paintGhost ? e.MouseDragCurrentBounds : this.VirtualBounds);      // Kreslení "ducha" je na jiné souřadnici, než na místě prvku samotného
            if (!virtualBounds.HasValue) return;

            ItemPaintArgs paintArgs = new ItemPaintArgs(e);
            paintArgs.VirtualBounds = virtualBounds.Value;
            paintArgs.Alpha = (paintGhost ? (float?)App.CurrentAppearance.MouseDragActiveCurrentAlpha : (float?)null);
            paintArgs.ClientBounds = this.Parent.GetControlBounds(virtualBounds.Value);            // 

            float? alpha = (paintGhost ? (float?)App.CurrentAppearance.MouseDragActiveCurrentAlpha : (float?)null);

            var dataLayout = this.DataLayout;
            var paletteSet = App.CurrentAppearance;
            var clientBounds = this.Parent.GetControlBounds(virtualBounds.Value);                  // Souřadnice v systému souřadnic nativního controlu, v nich je vykreslován obsah prvku = jednotlivé prostory dané DataLayoutem
            var activeBounds = dataLayout.ContentBounds.GetBounds(clientBounds);
            var workspaceColor = App.CurrentAppearance.WorkspaceColor;

            Color? color;
            e.Graphics.SetClip(clientBounds);

            var currentInteractiveState = this.CurrentInteractiveState;                            // Kompletní stav, definuje všechny stavy a barvy
            var interactiveState = (currentInteractiveState & InteractiveState.MaskBasicStates);   // Obsahuje jen základní stav, daný myší
            var isSelected = IsSelected;
            if (isSelected)
            { }

            // this.OnPaintBack;
            // Celé pozadí buňky (buňka může mít explicitně danou barvu pozadí):
            color = this.BackColor.Morph(this.CellBackColor?.GetColor(currentInteractiveState));   // Statická barva pozadí + proměnná dle stavu
            if (color.HasValue)
            {   // Barva buňky se smíchá s barvou WorkspaceColor a vykreslí se celé její pozadí,
                // a tato barva se pak stává základnou pro Morphování a kreslení všech dalších barev v různých oblastech:
                workspaceColor = workspaceColor.Morph(color.Value);
                e.Graphics.FillRectangle(clientBounds, workspaceColor, alpha);
            }
            // Pozadí aktivní části buňky:
            if (this.IsDown)
            {
                color = paletteSet.ActiveContentColor.DownColor;
                if (color.HasValue)
                    e.Graphics.FillRectangle(activeBounds, workspaceColor.Morph(color.Value), alpha);
            }

            // Podkreslení celé buňky v myšoaktivním stavu:
            if ((interactiveState == InteractiveState.MouseOn || interactiveState == InteractiveState.MouseDown) && paletteSet.ActiveContentColor != null)
            {
                color = paletteSet.ActiveContentColor.GetColor(interactiveState);
                if (color.HasValue)
                    e.Graphics.FountainFill(activeBounds, workspaceColor.Morph(color.Value), Components.InteractiveState.Enabled, alpha);
            }

            // Rámeček a pozadí typu Border:
            if (dataLayout.BorderBounds.HasContent)
            {
                var borderBounds = dataLayout.BorderBounds.GetBounds(clientBounds);
                if (borderBounds.HasContent())
                {
                    using (var borderPath = borderBounds.GetRoundedRectanglePath(dataLayout.BorderRound))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        // Výplň dáme pod border:
                        color = paletteSet.ButtonBackColors.GetColor(interactiveState);
                        if (color.HasValue)
                            e.Graphics.FountainFill(borderPath, workspaceColor.Morph(color.Value), interactiveState, alpha);

                        // Linka Border:
                        if (dataLayout.BorderWidth > 0f)
                        {
                            var pen = App.GetPen(paletteSet.BorderLineColors, interactiveState, dataLayout.BorderWidth, alpha);
                            if (pen != null) e.Graphics.DrawPath(pen, borderPath);
                        }
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
                    mousePath.AddEllipse(mouseBounds);
                    using (System.Drawing.Drawing2D.PathGradientBrush pgb = new PathGradientBrush(mousePath))
                    {
                        e.Graphics.ResetClip();
                        pgb.CenterPoint = mousePoint;
                        pgb.CenterColor = workspaceColor.Morph(paletteSet.ButtonBackColors.MouseHighlightColor).GetAlpha(alpha);
                        pgb.SurroundColors = new Color[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                        e.Graphics.FillPath(pgb, mousePath);
                        e.Graphics.SetClip(clientBounds);
                    }
                }
            }

            // Vykreslit Image:
            var image = App.GetImage(this.ImageName, this.ImageContent);
            if (dataLayout.ImageBounds.HasContent && image != null)
            {
                var imageBounds = dataLayout.ImageBounds.GetBounds(clientBounds);
                if (imageBounds.HasContent())
                {
                    e.Graphics.ResetClip();
                    e.Graphics.SmoothingMode = SmoothingMode.None;
                    e.Graphics.DrawImage(image, imageBounds, alpha);
                    e.Graphics.SetClip(clientBounds);
                }
            }

            // Vypsat text:
            if (dataLayout.MainTitleBounds.HasContent && !String.IsNullOrEmpty(this.MainTitle))
            {
                var mainTitleBounds = dataLayout.MainTitleBounds.GetBounds(clientBounds);
                if (mainTitleBounds.HasContent())
                {
                    e.Graphics.DrawText(this.MainTitle, mainTitleBounds, dataLayout.MainTitleAppearance, interactiveState, alpha);
                }
            }

            e.Graphics.ResetClip();
        }
        protected class ItemPaintArgs
        {
            public ItemPaintArgs(PaintDataEventArgs paintData)
            {
                this.PaintData = paintData;
            }
            /// <summary>
            /// Vstupní data, obshaují i údaje o myši atd
            /// </summary>
            public PaintDataEventArgs PaintData { get; set; }
            /// <summary>
            /// Grafika, cíl kreslení
            /// </summary>
            public Graphics Graphics { get { return PaintData.Graphics; } }
            /// <summary>
            /// Obsahuje true když se kreslí "přesouvaný duch"
            /// </summary>
            public bool PaintGhost { get; set; }
            /// <summary>
            /// Souřadnice ve virtuálním prostoru, odpovídá kompletním datům, před posouváním pomocí ScrollBarů
            /// </summary>
            public Rectangle VirtualBounds { get; set; }
            /// <summary>
            /// Souřadnice v systému souřadnic nativního controlu, v nich je vykreslován obsah prvku = jednotlivé prostory dané DataLayoutem
            /// </summary>
            public Rectangle ClientBounds { get; set; }
            /// <summary>
            /// Průhlednost všech barev a prvků, používá se při DragAndDrop. Null = kreslíme základní prvek.
            /// </summary>
            public float? Alpha { get; set; }

            public LayoutItemInfo DataLayout { get; set; }
            public AppearanceInfo PaletteSet { get; set; }
            public InteractiveState CurrentInteractiveState { get; set; }

            public InteractiveState BasicnteractiveState { get; set; }

            public Rectangle ActiveBounds { get; set; }

            public Color WorkspaceColor { get; set; }
        }
        #endregion
    }
    #region Enumy
    /// <summary>
    /// Stav prvku
    /// </summary>
    [Flags]
    public enum InteractiveState
    {
        /// <summary>
        /// Enabled, ale nijak neaktivní
        /// </summary>
        Enabled = 0,
        /// <summary>
        /// Disabled
        /// </summary>
        Disabled = 1,
        /// <summary>
        /// S myší
        /// </summary>
        MouseOn = 3,
        /// <summary>
        /// Stisknuto
        /// </summary>
        MouseDown = 4,
        /// <summary>
        /// V procesu DragAndDrop
        /// </summary>
        Dragged = 5,
        /// <summary>
        /// Maska pro základní stavy <see cref="Disabled"/> | <see cref="Enabled"/> | <see cref="MouseOn"/> | <see cref="MouseDown"/> | <see cref="Dragged"/>.
        /// </summary>
        MaskBasicStates = 0x00FF,
        /// <summary>
        /// Přídavek Selected = přidává se k základnímu stavu
        /// </summary>
        AndSelected = 0x0100,
        /// <summary>
        /// Přídavek Down = přidává se k základnímu stavu
        /// </summary>
        AndDown = 0x0200
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
        private Rectangle? __MouseDragCurrentBounds;
        private MouseDragState __MouseDragState;
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
        /// Souřadnice aktivního prvku, kam by byl přesunut v procesu Mouse DragAndDrop, když <see cref="MouseDragState"/> je <see cref="MouseDragState.MouseDragActiveCurrent"/>
        /// </summary>
        public Rectangle? MouseDragCurrentBounds { get { return __MouseDragCurrentBounds; } set { __MouseDragCurrentBounds = value; } }
        /// <summary>
        /// Stav procesu Mouse DragAndDrop pro aktuální vykreslovaný prvek
        /// </summary>
        public MouseDragState MouseDragState { get { return __MouseDragState; } set { __MouseDragState = value; } }
        /// <summary>
        /// Pozice a stav myši
        /// </summary>
        public Components.MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Virtuální kontejner, do kterého je kresleno
        /// </summary>
        public Components.IVirtualContainer VirtualContainer { get { return __VirtualContainer; } }
    }
    /// <summary>
    /// Stav procesu Mouse DragAndDrop
    /// </summary>
    public enum MouseDragState
    {
        /// <summary>
        /// Nejedná se o Mouse DragAndDrop
        /// </summary>
        None,
        /// <summary>
        /// Aktuální vykreslovaný prvek je "pod" myší v procesu Mouse DragAndDrop = jde o běžný prvek, který není přesouván, ale leží na místě, kde se nachází myš v tomto procesu
        /// </summary>
        MouseDragTarget,
        /// <summary>
        /// Aktuální vykreslovaný prvek je ten, který se přesouvá v procesu Mouse DragAndDrop.
        /// V tomto stavu se má vykreslit ve své původní pozici (Source).
        /// </summary>
        MouseDragActiveOriginal,
        /// <summary>
        /// Aktuální vykreslovaný prvek je ten, který se přesouvá v procesu Mouse DragAndDrop.
        /// V tomto stavu se má vykreslit ve své cílové pozici, kde je zrovna umístěn při přetažení myší.
        /// Pak se má pro kreslení použít souřadnice <see cref="PaintDataEventArgs.MouseDragCurrentBounds"/>
        /// </summary>
        MouseDragActiveCurrent
    }
    #endregion
}
