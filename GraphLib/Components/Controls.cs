﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region ControlBuffered : Control with buffered graphic
    /// <summary>
    /// ControlBuffered: Control with buffered graphic
    /// </summary>
    public class ControlBuffered : Control, IDisposable
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ControlBuffered()
        {
            this._GraphBufferInit();
        }
        #endregion
        #region BufferedGraphics - podpora pro kreslení do bufferu
        private void _GraphBufferInit()
        {
            this._PrepareBufferForSize(this.Size);
            
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
        }
        /// <summary>
        /// Prepare _BuffGraphContent and _BuffGraphics for specified size
        /// </summary>
        /// <param name="size"></param>
        private void _PrepareBufferForSize(Size size)
        {
            if (this._BuffGraphContent == null)
                this._BuffGraphContent = BufferedGraphicsManager.Current;
            this._BuffGraphContent.MaximumBuffer = new Size(size.Width + 1, size.Height + 1);

            if (this._BuffGraphics != null)
            {
                this._BuffGraphics.Dispose();
                this._BuffGraphics = null;
            }
            this._BuffGraphics = this._BuffGraphContent.Allocate(this.CreateGraphics(), new Rectangle(new Point(0, 0), size));
        }
        /// <summary>
        /// Dispose
        /// </summary>
        void IDisposable.Dispose()
        {
            if (this._BuffGraphics != null)
            {
                this._BuffGraphics.Dispose();
                this._BuffGraphics = null;
            }
            if (this._BuffGraphContent != null)
            {
                this._BuffGraphContent.Dispose();
                this._BuffGraphContent = null;
            }
        }
        /// <summary>
        /// Změna velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this._Resize(e);
        }
        /// <summary>
        /// Handler události OnResize: zajistí přípravu nového bufferu, vyvolání kreslení do bufferu, a zobrazení dat z bufferu
        /// </summary>
        /// <param name="e"></param>
        private void _Resize(EventArgs e)
        {
            if (!this.ReallyCanDraw) return;
            this._PrepareBufferForSize(this.Size);
            this._Draw();
        }
        /// <summary>
        /// Překreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this._Paint(e);
        }
        /// <summary>
        /// Fyzický Paint.
        /// Probíhá kdykoliv, když potřebuje okno překreslit.
        /// Aplikační logiku k tomu nepotřebuje, obrázek pro vykreslení má připravený v bufferu. Jen jej přesune na obrazovku.
        /// Aplikační logika kreslí v případě Resize (viz event Dbl_Resize) a v případě, kdy ona sama chce (když si vyvolá metodu Draw()).
        /// </summary>
        /// <param name="e"></param>
        private void _Paint(PaintEventArgs e)
        {
            if (this.PendingFullDraw)
                this.Draw();
            this._BuffGraphics.Render(e.Graphics);
        }
        /// <summary>
        /// Content of buffered graphics
        /// </summary>
        private BufferedGraphicsContext _BuffGraphContent;
        /// <summary>
        /// Control mechanism of buffered graphics
        /// </summary>
        private BufferedGraphics _BuffGraphics;
        #endregion
        #region Public data a eventy
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        [Category("Appearance")]
        [Description("Barva pozadí prvku.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; Draw(); }
        }
        /// <summary>
        /// Událost volaná pro vykreslení controlu do bufferované grafiky.
        /// </summary>
        public event PaintEventHandler PaintToBuffer;
        #endregion
        #region Řízení kreslení (vyvolávací metoda + virtual výkonná metoda)
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// (Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.)
        /// Draw() vyvolá událost OnPaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public void Draw()
        {
            this._Draw();
        }
        /// <summary>
        /// Interní spouštěč metody pro kreslení dat
        /// </summary>
        private void _Draw()
        {
            if (!this.ReallyCanDraw)
            {
                this.PendingFullDraw = true;
                return;
            }
            try
            {
                this.DrawInProgress = true;

                PaintEventArgs e = new PaintEventArgs(this._BuffGraphics.Graphics, new Rectangle(0, 0, this.Width, this.Height));
                this._OnPaintToBuffer(e);
                this.Refresh();
            }
            finally
            {
                this.DrawInProgress = false;
            }
        }
        /// <summary>
        /// Provede vykreslení přes virtual metodu a přes handler
        /// </summary>
        /// <param name="e"></param>
        private void _OnPaintToBuffer(PaintEventArgs e)
        {
            this.OnPaintToBuffer(this, e);
            if (this.PaintToBuffer != null)
                this.PaintToBuffer(this, e);
        }
        /// <summary>
        /// Metoda, která zajišťuje kreslení.
        /// Potomkové mohou využít, ale musí volat base(sender, e);
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
        }
        /// <summary>
        /// Určí souřadnici daného bodu (vztažená k this, tedy {0,0} je horní levý roh tohoto objektu) v souřadném systému hostitele.
        /// Hostitelem může být Form (pokud je parametr toForm = true) nebo Screen (pokud je toForm = false).
        /// </summary>
        /// <param name="point"></param>
        /// <param name="toForm"></param>
        /// <returns></returns>
        protected Point PointToHost(Point point, bool toForm)
        {
            Point result = point;
            Control current = this;
            while (true)
            {
                result = _PointAdd(result, current.Location);
                if (current.Parent == null) break;           // Pokud Control current nemá parenta, pak končíme.
                current = current.Parent;                    // Jinak se podíváme na jeho hostitele.
                if (current is Form && toForm) break;        // Pokud je hostitel Form, a nám to stačí, skončíme.
                result = _PointAdd(result, current.DisplayRectangle.Location); // Souřadnice počátku klientského prostoru
                result = _PointAdd(result, current.ClientRectangle.Location);  // Souřadnice počátku klientského prostoru
            }
            return result;
        }
        private Point _PointAdd(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Příznak, že skutečně může proběhnout kreslení
        /// </summary>
        protected bool ReallyCanDraw { get { return (this.CanDraw && !this.DrawInProgress && this.Width > 0 && this.Height > 0 && this.Parent != null); } }
        /// <summary>
        /// Descendant can override this property, and disable (with false) any drawing.
        /// </summary>
        protected virtual bool CanDraw { get { return true; } }
        /// <summary>
        /// Příznak, že právě nyní probíhá Draw
        /// </summary>
        protected bool DrawInProgress { get; private set; }
        /// <summary>
        /// Příznak, že poslední běh metody this.Draw() byl vynechán, protože nemohl být proveden (CanDraw bylo false, nebo _DrawInProgress bylo true)
        /// </summary>
        protected bool PendingFullDraw { get; set; }

        #endregion
    }
    #endregion
    #region ControlLayered : Třída určená ke kreslení grafiky na plochu. Varianta pro složitější interaktivní motivy.
    /// <summary>
    /// Třída určená ke kreslení grafiky na plochu - absolutně bez blikání.
    /// Varianta pro složitější interaktivní motivy: nabízí více vrstev pro kreslení.
    /// </summary>
    public class ControlLayered : Control, IAutoScrollContainer, IDisposable
    {
        #region Konstruktor, private event handlers
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ControlLayered()
        {
            this.PrepareLayers("Standard");
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResizeRedraw = true;
        }
        /// <summary>
        /// Změna velikosti controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            try
            {   // Tuhle metodu volá Windows podle potřeby, ...
                base.OnSizeChanged(e);               // WinForm kód: zde proběhnou volání eventhandlerů Resize i SizeChanged (v tomto pořadí)

                this._OnResizeControl();
                this._ResizeLayers(false);
                this.Draw();
            }
            catch (Exception exc)
            {   //  ... a jakákoli chyba by zbořila celou aplikaci:
                Application.App.ShowError(exc);
            }
        }
        /// <summary>
        /// Vykreslení controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);                     // WinForm kód: zde proběhnou volání eventhandleru Paint

            this._Paint(e);
        }
        /// <summary>
        /// Voláno z metody <see cref="OnPaint(PaintEventArgs)"/>, když chce control překreslit svůj obsah.
        /// Obsah se vezme z bufferovaných vrstev a vykreslí do předané grafiky.
        /// Není k tomu vyvolaná aplikační logika.
        /// </summary>
        /// <param name="e"></param>
        private void _Paint(PaintEventArgs e)
        {
            if (this.PendingFullDraw)
                this.Draw();
            this._RenderValidLayerTo(e.Graphics);
        }
        /// <summary>
        /// Metoda zajistí překreslení obsahu controlu: zavolá <see cref="Draw()"/> a poté <see cref="Control.Invalidate()"/>.
        /// Metoda může být volána i z threadu na pozadí, sama zajistí asynchronní invokaci GUI threadu.
        /// </summary>
        public override void Refresh()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(this.Refresh));
            }
            else
            {
                try
                {   // Tuhle metodu volá Windows podle potřeby, ...
                    this.Draw();
                    this.Invalidate();
                    base.Refresh();
                }
                catch (Exception exc)
                {   //  ... a jakákoli chyba by zbořila celou aplikaci:
                    Application.App.ShowError(exc);
                }
            }
        }
        #endregion
        #region Public members
        /// <summary>
        /// Barva pozadí prvku.
        /// </summary>
        [Category("Appearance")]
        [Description("Barva pozadí prvku.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; Draw(); }
        }
        /// <summary>
        /// Počet vrstev, v rozmezí 1 - 10.
        /// Změna hodnoty vyvolá ReDraw().
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LayerCount
        {
            get { return this._LayerCount; }
        }
        /// <summary>
        /// Metoda zajistí přípravu dat pro kreslící vrstvy, kde jednotlivé vrstvy mají daná jména
        /// </summary>
        /// <param name="layerNames"></param>
        protected void PrepareLayers(params string[] layerNames)
        {
            string[] names = layerNames.Where(n => n != null).ToArray();
            if (names.Length == 0) names = new string[] { "Default" };
            if (names.Length > 10) names = names.Take(10).ToArray();
            this._LayerNames = names;
            this._LayerCount = names.Length;
            this._CreateLayers();
            this.Draw();
        }
        /// <summary>
        /// Tato metoda se volá tehdy, když aplikace chce překreslit celý objekt.
        /// </summary>
        public void Draw()
        {
            this._Draw(null, null);
        }
        /// <summary>
        /// Tato metoda se volá tehdy, když aplikace chce překreslit celý objekt.
        /// </summary>
        public void Draw(params int[] layers)
        {
            if (!this.IsPainted)
                this._Draw(null, null);
            this._Draw(layers, null);
        }
        /// <summary>
        /// Tato metoda se volá tehdy, když aplikace chce překreslit celý objekt.
        /// </summary>
        public void Draw(object userData)
        {
            if (!this.IsPainted)
                // Pokud jsem dosud nebyl kreslen, pak ignorujeme explicitní požadavek (ten říká: kresli jen něco), a budeme kreslit vše:
                this._Draw(null, null);
            else
                // Kresli podle požadavku:
                this._Draw(null, userData);
        }
        /// <summary>
        /// Tato metoda se volá tehdy, když aplikace chce překreslit celý objekt.
        /// </summary>
        public void Draw(IEnumerable<int> drawLayers, object userData)
        {
            if (!this.IsPainted)
                // Pokud jsem dosud nebyl kreslen, pak ignorujeme explicitní požadavek (ten říká: kresli ken něco), a budeme kreslit vše:
                this._Draw(null, null);
            else
                // Kresli podle požadavku:
                this._Draw(drawLayers, userData);
        }
        /// <summary>
        /// Událost, kdy se má překreslit obsah vrstev controlu.
        /// Pokud někdo chce použít tuto třídu bez jejího dědění, pak může svoje kreslení provést v eventhandleru této události.
        /// </summary>
        public event LayeredPaintEventHandler PaintLayers;
        /// <summary>
        /// Call method OnPaintLayers() and event PaintLayers.
        /// </summary>
        /// <param name="e"></param>
        private void _OnPaintLayers(LayeredPaintEventArgs e)
        {
            if (!this._IsPainted) this._IsPainted = true;
            this.OnPaintLayers(e);
            if (this.PaintLayers != null)
                this.PaintLayers(this, e);
        }
        /// <summary>
        /// Háček, který se volá při potřebě překreslit obsah objektu.
        /// Typicky jej overriduje potomek a provádí kreslení.
        /// Bázová metoda provádí pouze Clear() vrstvy 0, a to jen tehdy pokud se má kreslit. Clear() vloží do vrstvy 0 barvu this.BackColor.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintLayers(LayeredPaintEventArgs e)
        {
            // Clear layer 0:
            if (e.NeedPaintToLayer(0))
                e.GetGraphicsForLayer(0, true)?.Clear(this.BackColor);
        }
        /// <summary>
        /// Occured in Resize process, after prepared graphics, before Draw() process.
        /// New Bounds of this control is now valid.
        /// Application can resize its components. 
        /// Code have does not call Draw() method, will be called immediatelly after this event.
        /// </summary>
        public event EventHandler ResizeControl;
        /// <summary>
        /// Call method OnResizeBeforeDraw() and event ResizeBeforeDraw.
        /// </summary>
        private void _OnResizeControl()
        {
            this.OnResizeControl();
            if (this.ResizeControl != null)
                this.ResizeControl(this, EventArgs.Empty);
        }
        /// <summary>
        /// After graphics resized, before Draw().
        /// Base class method is empty.
        /// </summary>
        protected virtual void OnResizeControl()
        { }
        /// <summary>
        /// Was at least once painted?
        /// </summary>
        protected bool IsPainted { get { return this._IsPainted; } } private bool _IsPainted = false;
        #endregion
        #region Řízení kreslení: private _Draw(); protected OnPaintLayers();
        /// <summary>
        /// Zajistí překreslení daných vrstev objektu
        /// </summary>
        /// <param name="drawLayers"></param>
        /// <param name="userData"></param>
        private void _Draw(IEnumerable<int> drawLayers, object userData)
        {
            if (!this.ReallyCanDraw)
            {
                this.PendingFullDraw = true;
                return;
            }
            try
            {
                this._RunOnDrawBefore();

                this.DrawInProgress = true;

                LayeredPaintEventArgs e = new LayeredPaintEventArgs(this._GraphicLayers, drawLayers, userData);
                this._OnPaintLayers(e);
                this.PendingFullDraw = false;
                this._RenderValidLayerTo(this.CreateGraphics());
            }
            finally
            {
                this.DrawInProgress = false;
            }

            this._RunOnDrawAfter();
        }
        /// <summary>
        /// Vyvolá se před každým vykreslením
        /// </summary>
        private void _RunOnDrawBefore()
        {
            if (!this._IsDoneFirstDrawBefore)
            {
                var form = this.FindForm();
                if (form != null && form.Visible)
                {
                    this._IsDoneFirstDrawBefore = true;
                    this.OnFirstDrawBefore();
                    this.FirstDrawBefore?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Vyvolá se po každém vykreslení
        /// </summary>
        private void _RunOnDrawAfter()
        {
            if (!this._IsDoneFirstDrawAfter)
            {
                var form = this.FindForm();
                if (form != null && form.Visible)
                {
                    this._IsDoneFirstDrawAfter = true;
                    this.OnFirstDrawAfter();
                    this.FirstDrawAfter?.Invoke(this, EventArgs.Empty);
                }
            }
            this.OnDrawAfter();
        }
        /// <summary>
        /// Obsahuje true po prvním reálném vykreslení, před provedením metody <see cref="OnFirstDrawBefore"/>
        /// </summary>
        private bool _IsDoneFirstDrawBefore = false;
        /// <summary>
        /// Obsahuje true po prvním reálném vykreslení, před provedením metody <see cref="OnFirstDrawAfter"/>
        /// </summary>
        private bool _IsDoneFirstDrawAfter = false;
        /// <summary>
        /// Metoda je volána POUZE PŘED PRVNÍM vykreslení obsahu
        /// </summary>
        protected virtual void OnFirstDrawBefore() { }
        /// <summary>
        /// Je voláno POUZE PŘED PRVNÍM vykreslení obsahu
        /// </summary>
        public event EventHandler FirstDrawBefore;
        /// <summary>
        /// Metoda je volána POUZE PO PRVNÍM vykreslení obsahu
        /// </summary>
        protected virtual void OnFirstDrawAfter() { }
        /// <summary>
        /// Je voláno POUZE PO PRVNÍM vykreslení obsahu
        /// </summary>
        public event EventHandler FirstDrawAfter;
        /// <summary>
        /// Metoda je volána po každém vykreslení obsahu
        /// </summary>
        protected virtual void OnDrawAfter() { }
        /// <summary>
        /// Fyzicky překreslí obsah bufferů do předané grafiky.
        /// </summary>
        /// <param name="targetGraphics">Cílová grafika pro kreslení</param>
        private void _RenderValidLayerTo(Graphics targetGraphics)
        {
            GraphicLayer sourceLaeyr = GraphicLayer.GetCurrentLayer(this._GraphicLayers);
            sourceLaeyr.RenderTo(targetGraphics);
        }
        /// <summary>
        /// Příznak, že skutečně může proběhnout kreslení - z hlediska příznaků v controlu <see cref="CanDraw"/> a <see cref="IsReadyToDraw"/> a <see cref="DrawInProgress"/>
        /// </summary>
        protected bool ReallyCanDraw { get { return (this.CanDraw && this.IsReadyToDraw && !this.DrawInProgress); } }
        /// <summary>
        /// Obsahuje true tehdy, když this control je umístěn na formuláři, a tento formulář je viditelný, a this control má kladné rozměry
        /// </summary>
        public bool IsReadyToDraw
        {
            get
            {
                return (this.Width > 0 && this.Height > 0 && this.Parent != null && this.IsVisibleParentForm);
            }
        }
        /// <summary>
        /// Obsahuje true tehdy, když this control je umístěn na formuláři, a tento formulář je viditelný
        /// </summary>
        protected bool IsVisibleParentForm
        {
            get
            {
                var form = (this.Parent != null ? this.FindForm() : null);
                return (form != null && form.Visible);
            }
        }
        /// <summary>
        /// Descendant can override this property, and disable (with false) any drawing.
        /// </summary>
        protected virtual bool CanDraw { get { return true; } }
        /// <summary>
        /// Příznak, že právě nyní probíhá Draw
        /// </summary>
        protected bool DrawInProgress { get; private set; }
        /// <summary>
        /// Příznak, že poslední běh metody this.Draw() byl vynechán, protože nemohl být proveden (CanDraw bylo false, nebo _DrawInProgress bylo true)
        /// </summary>
        protected bool PendingFullDraw { get; set; }
        #endregion
        #region Řízení vrstev: Create, Resize, Dispose. Nikoliv kreslení.
        /// <summary>
        /// Zajistí změnu kreslící velikosti
        /// </summary>
        /// <param name="callDraw"></param>
        private void _ResizeLayers(bool callDraw)
        {
            this._CreateLayers();
            if (callDraw)
                this.Draw();
        }
        /// <summary>
        /// Vytvoří new grafické vrstvy pro aktuální velikost <see cref="Size"/> a pro počet vrstev <see cref="LayerCount"/>.
        /// </summary>
        private void _CreateLayers()
        {
            this._DisposeLayers(false);

            int count = this._LayerCount;
            string[] names = this._LayerNames;

            Size size = this.Size;
            if (size.Width <= 0 || size.Height <= 0)
                size = new Size(1, 1);

            GraphicLayer[] allLayers = new GraphicLayer[count];
            for (int index = 0; index < count; index++)
                allLayers[index] = new GraphicLayer(this, size, allLayers, index, names[index]);
            
            this._GraphicLayers = allLayers;
        }
        /// <summary>
        /// Uvolní z paměti stávající grafické vrstvy
        /// </summary>
        /// <param name="resetCount"></param>
        private void _DisposeLayers(bool resetCount)
        {
            if (this._GraphicLayers != null)
            {
                foreach (GraphicLayer layer in this._GraphicLayers)
                {
                    if (layer != null && layer.Size.Width > 0 && layer.Size.Height > 0)
                        ((IDisposable)layer).Dispose();
                }
            }
            this._GraphicLayers = null;
            if (resetCount)
                this._LayerCount = 0;
        }
        private int _LayerCount;
        private string[] _LayerNames;
        private GraphicLayer[] _GraphicLayers;
        #endregion
        #region AutoScroll
        #region Public členy
        /// <summary>
        /// Hodnota true zapíná funkci AutoScroll = v případě potřeby se zobrazí posuvníky (pokud souřadnice položek přesahují aktuální velikost klientské velikosti controlu).
        /// Default : false = nic není zapnuté, ani není vytvářen objekt pro support AutoScroll
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AutoScroll
        {
            get
            {
                return this._HasAutoScroll;
            }
            set
            {
                bool autoScroll = value;
                if (autoScroll)
                {   // Nastavit na true => zajistit existenci instance AutoScrollSupport:
                    if (this._AutoScrollSupport == null)
                        this._AutoScrollSupport = new AutoScrollSupport(this); // Instanci vytvoříme až při nastavení na true (případné vypnutí se běžně nedělá, pak instanci ponecháme a nastavíme ji false)
                    this._AutoScrollSupport.AutoScroll = true;
                }
                else if (this._AutoScrollSupport != null)
                {   // Nastavit na false => vložit hodnotu do AutoScrollSupport jen pokud existuje:
                    this._AutoScrollSupport.AutoScroll = false;
                }

                // Po setování hodnoty, pokud existuje instance AutoScrollSupport, zavoláme její AutoScrollDetect():
                if (this._AutoScrollSupport != null)
                    this._AutoScrollSupport.AutoScrollDetect();
            }
        }
        /// <summary>
        /// Přídavek velikosti doprava a dolů za prostor obsazeny controly <see cref="ChildItems"/>.
        /// Metoda <see cref="AutoScrollDetect()"/> určí nejvyšší použitou souřadnici v ose X i Y, 
        /// a pokud by se prvky nevešly do viditelného prostoru, pak určí celkový zobrazovaný prostor (přidáním tohoto okraje doprava a dolů),
        /// a určí potřebné scrollbary.
        /// <para/>
        /// Tento Margin vizuálně zvýrazní, že za posledními prvky v daném směru už není nic.
        /// <para/>
        /// Default je hodnota: { 8, 8 }; setovat lze hodnoty v rozmezí 0 - 30 včetně.
        /// Pozor: pokud <see cref="AutoScroll"/> = false, pak nemá význam setovat hodnotu (neuloží se), a čtení hodnoty vrací <see cref="Size.Empty"/>.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Size AutoScrollMargin
        {
            get { return (this._HasAutoScroll ? this._AutoScrollSupport.AutoScrollMargin : Size.Empty); }
            set { if (this._HasAutoScroll) this._AutoScrollSupport.AutoScrollMargin = value; }
        }
        /// <summary>
        /// Režim AutoScroll je aktivní (=je povolen, a aktuálně je z důvodu potřeby zobrazen alespoň jeden ScrollBar)
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AutoScrollActive { get { return this._AutoScrollActive; } }
        /// <summary>
        /// Zajistí zobrazení daného prvku v AutoScroll containeru
        /// </summary>
        /// <param name="item"></param>
        public void SetAutoScrollContainerToItem(IInteractiveItem item)
        {
            if (item == null || !_AutoScrollActive) return;

            // Zatím věc vyřešíme jen pro jeden AutoScrollSupport v hierarchii, ale není to finální řešení - protože AutoScroll prvků může být více vnořených!!!
            var bounds = BoundsInfo.CreateForChild(item);
            this._AutoScrollSupport.ScrollToShow(bounds.CurrentItemAbsoluteBounds);
        }
        /// <summary>
        /// Velikost prostoru pro kreslení prvků, po odečtení prostoru pro Scrollbary.
        /// Pokud <see cref="AutoScroll"/> = false, pak vrací hodnotu ClientSize.
        /// </summary>
        public virtual Size ClientItemsSize { get { return (this._HasAutoScroll ? this._AutoScrollSupport.ClientItemsSize : this.ClientSize); } }
        /// <summary>
        /// Nativní souřadnice prostoru (tj. bez posunutí obsahu vlivem AutoScrollu) pro kreslení prvků, po odečtení prostoru pro Scrollbary
        /// Pokud <see cref="AutoScroll"/> = false, pak vrací hodnotu ClientRectangle.
        /// </summary>
        public virtual Rectangle ClientItemsRectangle { get { return (this._HasAutoScroll ? this._AutoScrollSupport.ClientItemsRectangle : this.ClientRectangle); } }
        /// <summary>
        /// Metodu musí zavolat potomek před každým vykreslením prvků v režimu kreslení DrawAllItems.
        /// Musí se volat ještě před sestavením kolekce prvků (DrawAllItems) ke kreslení, 
        /// protože tato metoda upravuje viditelné souřadnice this controlu i souřadnice svých ScrollBarů!
        /// <para/>
        /// Metoda změří fyzickou velikost controlu, rozsah kreslených prvků a určí potřebu kreslení ScrollBarů.
        /// Pokud budou scrollbary potřebné, zajistí jejich korektní zobrazení = viditelnost, souřadnice a zobrazené hodnoty.
        /// </summary>
        protected void AutoScrollDetect()
        {
            if (this._HasAutoScroll) this._AutoScrollSupport.AutoScrollDetect();
        }
        /// <summary>
        /// Horizontální (vodorovný) ScrollBar.
        /// </summary>
        protected ScrollBar AutoScrollBarH { get { return this._AutoScrollSupport?.ScrollBarH; } }
        /// <summary>
        /// Vertikální (svislý) ScrollBar
        /// </summary>
        protected ScrollBar AutoScrollBarV { get { return this._AutoScrollSupport?.ScrollBarV; } }
        /// <summary>
        /// Obsahuje true pokud this control má implementován a povolený AutoScroll,
        /// tedy existuje instance <see cref="_AutoScrollSupport"/> a má <see cref="AutoScrollSupport.AutoScroll"/> = true.
        /// POZOR: zdejší hodnota true říká, že AutoScroll může být aktivní podle potřeby, ale neříká že zrovna teď aktivní je. O tom mluví <see cref="_AutoScrollActive"/>.
        /// </summary>
        private bool _HasAutoScroll { get { return (this._AutoScrollSupport != null ? this._AutoScrollSupport.AutoScroll : false); } }
        /// <summary>
        /// Obsahuje true pokud this control má nyní aktivní AutoScroll (=je povolen, a aktuálně je z důvodu potřeby zobrazen alespoň jeden ScrollBar).
        /// </summary>
        private bool _AutoScrollActive { get { return (this._AutoScrollSupport != null ? this._AutoScrollSupport.AutoScrollActive : false); } }
        /// <summary>
        /// Instance pro podporu AutoScroll, vytváří se jen když je potřeba
        /// </summary>
        private AutoScrollSupport _AutoScrollSupport;
        #endregion
        #region Posunutí obsahu vyžádané externě
        /// <summary>
        /// Posune aktuální obsah o danou hodnotu.
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="change"></param>
        /// <param name="modifierKeys"></param>
        public void AutoScrollDoScrollBy(Orientation orientation, GInteractiveChangeState change, Keys modifierKeys = Keys.None)
        {
            if (this._AutoScrollActive) this._AutoScrollSupport.ScrollBy(orientation, change, modifierKeys);
        }
        #endregion
        #region virtual ChildItems a interface IAutoScrollContainer
        /// <summary>
        /// Zde potomek deklaruje souhrn svých prvků, z nichž se bude vypočítávat obsazená velikost v metodě <see cref="AutoScrollDetect()"/>.
        /// Tuto property musí řešit potomek this třídy <see cref="ControlLayered"/>, protože this třída nemá Child prvky.
        /// Tuto property využívá člen interface <see cref="IAutoScrollContainer.ChildItems"/>, kudy předává Child prvky do <see cref="AutoScrollSupport"/>.
        /// </summary>
        protected virtual IEnumerable<IInteractiveItem> ChildItems { get { return null; } }
        /// <summary>
        /// Režim AutoScroll je aktivní (=je povolen, a aktuálně je z důvodu potřeby zobrazen alespoň jeden ScrollBar)
        /// </summary>
        bool IAutoScrollContainer.AutoScrollActive { get { return this._AutoScrollActive; } }
        /// <summary>
        /// Souřadnice počátku = na které souřadnici našeho aktuálního prostoru se nachází virtuální bod 0/0.
        /// Příklad: Mějme AutoScrollContainer, jehož využitelný vnitřní prostor je 300 x 200 px, ale zobrazuje obsah o velikosti 600 x 800 px.
        /// Aktuálně máme odscrollováno tak, že první viditelný pixel obsahu má pozici X=10, Y=50 (to lze načíst ze scrollbarů jako jejich Value.Begin).
        /// Znamená to, že virtuální bod 0,0 leží doleva (o 10px) a nahoru (o 50px) od zobrazovaného prvního pixelu.
        /// V tomto příkladu je tedy <see cref="IAutoScrollContainer.VirtualOrigin"/> = { -10, -50 }.
        /// </summary>
        Point IAutoScrollContainer.VirtualOrigin { get { return (this._AutoScrollActive ? this._AutoScrollSupport.VirtualOrigin : Point.Empty); } }
        /// <summary>
        /// Souřadnice prostoru uvnitř klientské části this controlu, kde se zobrazují Child prvky.
        /// Pokud AutoScrollContainer je běžný prvek se scrollbary vpravo a dole, pak souřadnice <see cref="IAutoScrollContainer.VirtualBounds"/> jsou { 0, 0, Width-scrollbar, Height-scrollbar }.
        /// </summary>
        Rectangle IAutoScrollContainer.VirtualBounds { get { return (this._AutoScrollActive ? this._AutoScrollSupport.VirtualBounds : new Rectangle(Point.Empty, this.ClientSize)); } }
        /// <summary>
        /// Velikost celého fyzického prostoru hostitele, tj. včetně prostoru pro Scrollbary.
        /// AutoScroll jej může využít pro prostor <see cref="IAutoScrollContainer.VirtualBounds"/> a pro umístění Scrollbarů.
        /// </summary>
        Size IAutoScrollContainer.HostSize { get { return this.ClientSize; } }
        /// <summary>
        /// Tato metoda je volaná tehdy, když se změní stav <see cref="AutoScrollActive"/>
        /// </summary>
        void IAutoScrollContainer.OnAutoScrollActiveChanged() { }
        /// <summary>
        /// Tato metoda je volaná tehdy, když se změní pozice nebo velikost obsahu vlivem změny velikosti controlu nebo obsahu
        /// </summary>
        void IAutoScrollContainer.OnAutoScrollContentChanged() { }
        /// <summary>
        /// Tato metoda je volaná tehdy, když uživatel aktivně pomocí Scrollbaru (nebo jinak = kolečkem myši) změní pozici obsahu
        /// </summary>
        void IAutoScrollContainer.OnAutoScrollPositionChanged() { this.Draw(); }
        /// <summary>
        /// člen interface:
        /// Zobrazované interaktivní prvky, mimo ScrollBary
        /// </summary>
        IEnumerable<IInteractiveItem> IAutoScrollContainer.ChildItems { get { return this.ChildItems; } }
        /// <summary>
        /// člen interface:
        /// Scrollbary aktuálně přítomné v this prvku
        /// </summary>
        IEnumerable<IInteractiveItem> IAutoScrollContainer.ScrollBars { get { return this._AutoScrollSupport?.ScrollBars; } }
        #endregion
        #endregion
        #region Dispose
        void IDisposable.Dispose()
        {
            this._DisposeLayers(true);
            this._CallDisposed();
        }
        private void _CallDisposed()
        {
            this.OnAfterDisposed();
            if (this.AfterDisposed != null)
                this.AfterDisposed(this, new EventArgs());
        }
        /// <summary>
        /// Is called in Dispose process, after disposed GControlLayers layers
        /// </summary>
        protected virtual void OnAfterDisposed()
        { }
        /// <summary>
        /// Is called in Dispose process, after disposed GControlLayers layers
        /// </summary>
        public event EventHandler AfterDisposed;
        #endregion
    }
    #region class GraphicLayer : třída představující jednu grafickou vrstvu
    /// <summary>
    /// GraphicLayer : třída představující jednu grafickou vrstvu
    /// </summary>
    internal class GraphicLayer : IDisposable
    {
        #region Konstruktor a privátní proměnné
        /// <summary>
        /// Konstruktor jedné vrstvy
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="size"></param>
        /// <param name="allLayers">Reference na pole všech vrstev</param>
        /// <param name="index">Index této jedné vrstvy</param>
        /// <param name="layerName">Název vrstvy</param>
        public GraphicLayer(Control owner, Size size, GraphicLayer[] allLayers, int index, string layerName)
        {
            this._Owner = owner;
            this._Size = size;
            this._AllLayers = allLayers;
            this._Index = index;
            this._Name = layerName;
            this._ContainData = false;
            this._CreateLayer();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Layer: #" + this._Index.ToString() + "; Name: " + this._Name + "; ContainData: " + (this._ContainData ? "Yes" : "No");
        }
        private Control _Owner;
        private Size _Size;
        private GraphicLayer[] _AllLayers;
        private int _Index;
        private string _Name;
        private bool _ContainData;
        /// <summary>
        /// Controll mechanism for buffered graphics
        /// </summary>
        private BufferedGraphicsContext _GraphicsContext;
        /// <summary>
        /// Content of graphic buffer
        /// </summary>
        private BufferedGraphics _GraphicsData;
        #endregion
        #region Privátní tvorba grafiky, IDisposable
        /// <summary>
        /// Vytvoří bufferovaný layer pro kreslení do this vrstvy
        /// </summary>
        private void _CreateLayer()
        {
            if (this._GraphicsContext == null)
                this._GraphicsContext = BufferedGraphicsManager.Current;

            Size s = this._Size;
            if (s.Width <= 0) s.Width = 1;
            if (s.Height <= 0) s.Height = 1;

            this._GraphicsContext.MaximumBuffer = new Size(s.Width + 1, s.Height + 1);

            if (this._GraphicsData != null)
                Application.App.TryRun(() => this._GraphicsData.Dispose(), false);

            this._GraphicsData = this._GraphicsContext.Allocate(this._Owner.CreateGraphics(), new Rectangle(new Point(0, 0), s));
            this._ContainData = false;
        }
        void IDisposable.Dispose()
        {
            if (this._GraphicsContext != null)
                Application.App.TryRun(() => this._GraphicsContext.Dispose(), false);
            this._GraphicsContext = null;

            if (this._GraphicsData != null)
                Application.App.TryRun(() => this._GraphicsData.Dispose(), false);
            this._GraphicsData = null;

            this._Owner = null;
        }
        #endregion
        #region Public property a metody
        /// <summary>
        /// true pokud je objekt platný
        /// </summary>
        public bool IsValid { get { return (this._Size.Width > 0 && this._Size.Height > 0 && this._GraphicsContext != null && this._GraphicsData != null); } }
        /// <summary>
        /// Index této vrstvy
        /// </summary>
        public int Index { get { return this._Index; } }
        /// <summary>
        /// Aplikační příznak: tato vrstva obsahuje platná data?
        /// Výchozí je false.
        /// Aplikace nastavuje na true v situaci, kdy si aplikační kód vyzvedl zdejší grafiku (bude do ní kreslit).
        /// Aplikace nastavuje na false v situaci, kdy tato vrstva je na vyšším indexu, než je vrstva aktuálně vykreslovaná.
        /// </summary>
        public bool ContainData { get { return this._ContainData; } set { this._ContainData = value; } }
        /// <summary>
        /// Rozměr vrstvy.
        /// Lze nastavit, vrstva se upraví. Poté je třeba překreslit (vrstva sama si nevolá).
        /// </summary>
        public Size Size { get { return this._Size; } }
        /// <summary>
        /// Objekt Graphics, který dovoluje kreslit motivy do této vrstvy
        /// </summary>
        public Graphics LayerGraphics { get { return this._GraphicsData.Graphics; } }
        /// <summary>
        /// Kopíruje obsah vrstvy (sourceLayer) to vrstvy (targetLayer).
        /// Pokud některá vrstva (sourceLayer nebo targetLayer) je null, nekopíruje se nic.
        /// Pokud zdrojová vrstva má index rovný nebo vyšší jako cílová vrstva, nekopíruje se nic.
        /// </summary>
        /// <param name="sourceLayer">Vrstva obsahující zdrojový obraz</param>
        /// <param name="targetLayer">Vrstva obsahující cílový obraz</param>
        public static void CopyContentOfLayer(GraphicLayer sourceLayer, GraphicLayer targetLayer)
        {
            if (sourceLayer != null && targetLayer != null && sourceLayer._Index < targetLayer._Index)
                sourceLayer.RenderTo(targetLayer.LayerGraphics);
        }
        /// <summary>
        /// Vykreslí svůj obsah do dané cílové Graphics, typicky při kopírování obsahu mezi vrstvami, 
        /// a při kreslení Controlu (skládají se jednotlivé vrstvy).
        /// </summary>
        /// <param name="targetGraphics"></param>
        public void RenderTo(Graphics targetGraphics)
        {
            targetGraphics.CompositingMode = CompositingMode.SourceOver;
            this._GraphicsData.Graphics.CompositingMode = CompositingMode.SourceOver;
            this._GraphicsData.Render(targetGraphics);
        }
        /// <summary>
        /// Metoda najde a vrátí položku <see cref="GraphicLayer"/> z dodaného pole, která má (<see cref="GraphicLayer.ContainData"/> == true)
        /// a je na nejvyšším indexu.
        /// Pokud dodané pole je null nebo neiobsahuje žádnou položku, vrací se null.
        /// Pokud žádná položka v poli nemá (<see cref="GraphicLayer.ContainData"/> == true), vrací se položka na indexu [0].
        /// </summary>
        /// <param name="graphicLayers"></param>
        /// <returns></returns>
        public static GraphicLayer GetCurrentLayer(GraphicLayer[] graphicLayers)
        {
            if (graphicLayers == null) return null;
            int count = graphicLayers.Length;
            if (count == 0) return null;
            for (int index = count - 1; index >= 0; index--)
            {
                if (index == 0 || graphicLayers[index].ContainData) return graphicLayers[index];
            }
            return graphicLayers[0];   // Jen pro compiler. Reálně sem kód nikdy nedojde (poslední smyčka "for ..." se provádí pro (index == 0).
        }
        #endregion
    }
    #endregion
    #region Delegate LayeredPaintEventHandler, EventArgs LayeredPaintEventArgs
    /// <summary>
    /// Delegate for handler of event LayeredPaint
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LayeredPaintEventHandler(object sender, LayeredPaintEventArgs e);
    /// <summary>
    /// Argument for OnLayeredPaint() method / LayeredPaint event
    /// </summary>
    public class LayeredPaintEventArgs : EventArgs
    {
        #region Konstrukce a privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphicLayers"></param>
        /// <param name="layersToPaint"></param>
        /// <param name="userData"></param>
        internal LayeredPaintEventArgs(GraphicLayer[] graphicLayers, IEnumerable<int> layersToPaint, object userData)
        {
            this._GraphicLayers = graphicLayers;
            this._GraphicsSize = graphicLayers[0].Size;    // Všechny vrstvy mají stejnou velikost; vrstva [0] existuje vždy.
            this._LayersToPaint = ((layersToPaint != null) ? layersToPaint.GetDictionary(i => i, true) : null);
            this._LayerCount = graphicLayers.Length;
            this._UserData = userData;
        }
        /// <summary>
        /// Pole grafických vrstev
        /// </summary>
        private GraphicLayer[] _GraphicLayers;
        /// <summary>
        /// Velikost, kterou má grafika pro kreslení
        /// </summary>
        private Size _GraphicsSize;
        /// <summary>
        /// Vrstvy, které mají být kresleny
        /// </summary>
        private Dictionary<int, int> _LayersToPaint;
        /// <summary>
        /// Počet grafických vrstev; používá se při kontrole zadaného indexu vrstvy
        /// </summary>
        private int _LayerCount;
        /// <summary>
        /// UserData
        /// </summary>
        private object _UserData;
        #endregion
        #region Podpora pro kreslení
        /// <summary>
        /// Vrátí instanci <see cref="Graphics"/> pro danou vrstvu.
        /// Může vrátit null pro nesprávné číslo vrstvy.
        /// To lze předem otestovat pomocí metody <see cref="IsValidLayer(int)"/>.
        /// Tato metoda vrací grafiku i pro vrstvu, do které se nemá kreslit (tj. pro kterou metoda <see cref="NeedPaintToLayer(int)"/> vrací false).
        /// </summary>
        /// <param name="layer">Index vrstvy. Pokud nebude platný, vrátí se null.</param>
        /// <param name="clear">Požadavek true, aby byla daná vrstva naplněna daty z podkladové vrstvy bez ohledu na její stav <see cref="GraphicLayer.ContainData"/>.</param>
        /// <returns></returns>
        public Graphics GetGraphicsForLayer(int layer, bool clear)
        {
            if (!IsValidLayer(layer)) return null;
            Graphics graphics = this._GetGraphicsForLayer(layer, clear).LayerGraphics;
            graphics.ResetClip();
            return graphics;
        }
        /// <summary>
        /// Metoda vrátí <see cref="Graphics"/> pro tu nejvyšší vrstvu, která obsahuje platná data po aktuálním vykreslování.
        /// </summary>
        /// <returns></returns>
        public Graphics GetGraphicsCurrent()
        {
            Graphics graphics = GraphicLayer.GetCurrentLayer(this._GraphicLayers).LayerGraphics;
            graphics.ResetClip();
            return graphics;
        }
        /// <summary>
        /// Vrátí true, pokud dané číslo vrstvy je správné a je možno s touto vrstvou pracovat.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool IsValidLayer(int layer)
        {
            return (layer >= 0 && layer < this._LayerCount);
        }
        /// <summary>
        /// Vrací true, pokud daná vrstva je platná, a má se do ní kreslit.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool NeedPaintToLayer(int layer)
        {
            if (!this.IsValidLayer(layer)) return false;
            if (this._LayersToPaint == null) return true;
            return this._LayersToPaint.ContainsKey(layer);
        }
        /// <summary>
        /// Velikost, kterou má grafika pro kreslení
        /// </summary>
        public Size GraphicsSize { get { return this._GraphicsSize; } }
        /// <summary>
        /// Libovolná data, předaná do metody Draw.
        /// </summary>
        public object UserData { get { return this._UserData; } }
        #endregion
        #region Vlastní správa vrstev grafiky
        /// <summary>
        /// Metoda najde grafiku pro danou vrstvu, zajistí že bude mít platný obsah a vrátí ji.
        /// Tzn. pokud vrstva neobsahuje data (<see cref="GraphicLayer.ContainData"/> == false), pak do ní nakopíruje data z nejbližší nižší platné vrstvy.
        /// Poté nastaví do dané vrstvy, že obsahuje platná data, a do všech vrstev vyšších nastaví, že data neobsahují.
        /// </summary>
        /// <param name="layer">Index vrstvy. Pokud nebude platný, vrátí se vrstva [0].</param>
        /// <param name="clear">Požadavek true, aby byla daná vrstva naplněna daty z podkladové vrstvy bez ohledu na její stav <see cref="GraphicLayer.ContainData"/>.</param>
        /// <returns></returns>
        private GraphicLayer _GetGraphicsForLayer(int layer, bool clear)
        {
            GraphicLayer sourceLayer = null;
            GraphicLayer targetLayer = null;
            GraphicLayer[] graphicLayers = this._GraphicLayers;
            int count = this._LayerCount;
            for (int index = 0; index < count; index++)
            {
                GraphicLayer currentLayer = graphicLayers[index];
                if (index == 0 || (index > 0 && index < layer && currentLayer.ContainData))
                    sourceLayer = currentLayer;
                if (index == layer)
                    targetLayer = currentLayer;
                if (index > layer)
                    currentLayer.ContainData = false;
            }
            if (targetLayer == null) targetLayer = graphicLayers[0];

            // Pokud targetLayer neobsahuje platná data, pak do této cílové vrstvy překopírujeme data ze zdrojové vrstvy:
            if (!targetLayer.ContainData || clear)
                GraphicLayer.CopyContentOfLayer(sourceLayer, targetLayer);

            // Od teď obsahuje cílová vrstva platná data:
            targetLayer.ContainData = true;
            return targetLayer;
        }
        #endregion
    }
    #endregion
    #endregion
    #region AutoScrollSupport : Kompletní podpora pro AutoScroll jak pro vizuální Control, tak pro InteractiveContainery
    /// <summary>
    /// Kompletní podpora pro AutoScroll
    /// </summary>
    public class AutoScrollSupport
    {
        #region Obecné public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AutoScrollSupport(IAutoScrollContainer owner)
        {
            this._Owner = owner;
            int margin = Skin.Control.AutoScrollMargins;
            this._AutoScrollMargin = new Size(margin, margin);
            this._AutoScrollActive = false;
            this._FirstPixel = new Point();
            this._AutoScrollReset();
        }
        private IAutoScrollContainer _Owner;
        /// <summary>
        /// Velikost prostoru v hostiteli, do kterého je zobrazován obsah.
        /// Jde o celou velikost, včetně prostor Scrollbarů.
        /// </summary>
        protected Size HostSize { get { return this._Owner.HostSize; } }
        /// <summary>
        /// Hodnota true zapíná funkci AutoScroll = zobrazí se posuvníky, pokud souřadnice položek přesahují aktuální velikost klientské velikosti controlu
        /// </summary>
        public bool AutoScroll { get; set; }
        /// <summary>
        /// Přídavek velikosti doprava a dolů za prostor obsazený controly <see cref="IAutoScrollContainer.ChildItems"/>.
        /// Metoda <see cref="AutoScrollDetect()"/> určí nejvyšší použitou souřadnici v ose X i Y, 
        /// a pokud by se prvky nevešly do viditelného prostoru, pak určí celkový zobrazovaný prostor (přidáním tohoto okraje doprava a dolů),
        /// a určí potřebné scrollbary.
        /// <para/>
        /// Tento Margin vizuálně zvýrazní, že za posledními prvky v daném směru už není nic.
        /// <para/>
        /// Default je hodnota: { 8, 8 }; setovat lze hodnoty v rozmezí 0 - 30 včetně.
        /// </summary>
        public Size AutoScrollMargin
        {
            get { return this._AutoScrollMargin; }
            set
            {
                int w = value.Width;
                int h = value.Height;
                w = (w < 0 ? 0 : (w > 30 ? 30 : w));
                h = (h < 0 ? 0 : (h > 30 ? 30 : h));
                this._AutoScrollMargin = new Size(w, h);
            }
        }
        private Size _AutoScrollMargin;
        /// <summary>
        /// Obsahuje true v případě, kdy je aktivní Scroll (tj. když nejsme v nativním zobrazení obsahu)
        /// </summary>
        public bool AutoScrollActive { get { return this.AutoScroll && this._AutoScrollActive; } }
        private bool _AutoScrollActive;
        /// <summary>
        /// Souřadnice počátku = na které souřadnici našeho aktuálního prostoru se nachází virtuální bod 0/0.
        /// Příklad: Mějme AutoScrollContainer, jehož využitelný vnitřní prostor je 300 x 200 px, ale zobrazuje obsah o velikosti 600 x 800 px.
        /// Aktuálně máme odscrollováno tak, že první viditelný pixel obsahu má pozici X=10, Y=50 (to lze načíst ze scrollbarů jako jejich Value.Begin).
        /// Znamená to, že virtuální bod 0,0 leží doleva (o 10px) a nahoru (o 50px) od zobrazovaného prvního pixelu.
        /// V tomto příkladu je tedy <see cref="IAutoScrollContainer.VirtualOrigin"/> = { -10, -50 }.
        /// </summary>
        public Point VirtualOrigin { get { return (this.AutoScrollActive ? new Point(-this._FirstPixel.X, -this._FirstPixel.Y): Point.Empty); } }
        /// <summary>
        /// Souřadnice prostoru uvnitř klientské části this controlu, kde se zobrazují Child prvky.
        /// Pokud AutoScrollContainer je běžný prvek se scrollbary vpravo a dole, pak souřadnice <see cref="IAutoScrollContainer.VirtualBounds"/> jsou { 0, 0, Width-scrollbar, Height-scrollbar }.
        /// </summary>
        public Rectangle VirtualBounds { get { return (this.AutoScrollActive ? this._AutoScrollBounds : new Rectangle(Point.Empty, this.HostSize)); } }
        /// <summary>
        /// Scrollbary aktuálně přítomné v this prvku
        /// </summary>
        public IEnumerable<IInteractiveItem> ScrollBars { get { return (this.AutoScrollActive ? this._ScrollBars : null); } }
        /// <summary>
        /// Horizontální (vodorovný) ScrollBar, anebo null když není zobrazován a tedy není aktivní
        /// </summary>
        public ScrollBar ScrollBarH { get { return ((this.AutoScrollActive && this._ScrollVisibleH) ? this._ScrollBarH : null); } }
        /// <summary>
        /// Vertikální (svislý) ScrollBar, anebo null když není zobrazován a tedy není aktivní
        /// </summary>
        public ScrollBar ScrollBarV { get { return ((this.AutoScrollActive && this._ScrollVisibleV) ? this._ScrollBarV : null); } }
        /// <summary>
        /// Velikost prostoru pro kreslení prvků, po odečtení prostoru pro Scrollbary
        /// </summary>
        public Size ClientItemsSize { get {  return this.ClientItemsRectangle.Size; } }
        /// <summary>
        /// Nativní souřadnice prostoru (tj. bez posunutí obsahu vlivem AutoScrollu) pro kreslení prvků, po odečtení prostoru pro Scrollbary
        /// </summary>
        public Rectangle ClientItemsRectangle
        {
            get
            {
                Point origin = new Point(0, 0);
                Size size = this.HostSize;
                if (this.AutoScroll)
                {
                    if (this._ScrollVisibleV) size.Width = size.Width - this._ScrollBarThick;
                    if (this._ScrollVisibleH) size.Height = size.Height - this._ScrollBarThick;
                }
                return new Rectangle(origin, size);
            }
        }
        #endregion
        #region Detekce nutnosti Autoscrollu, určení viditelnosti Scrollbarů a rozsahu viditelné oblasti
        /// <summary>
        /// Metodu musí zavolat potomek před každým vykreslením prvků v režimu kreslení DrawAllItems.
        /// Metoda změří fyzickou velikost controlu, rozsah kreslených prvků a určí potřebu kreslení ScrollBarů.
        /// Pokud budou scrollbary potřebné, zajistí jejich korektní zobrazení.
        /// </summary>
        public void AutoScrollDetect()
        {
            if (!this.AutoScroll) return;

            bool visibleH, visibleV;
            int rulerThick, scrollBarThick;
            Size contentSize = this._GetContentSize();
            Rectangle virtualBounds = this._GetVisibleBounds(ref contentSize, out visibleH, out visibleV, out rulerThick, out scrollBarThick);

            if (!visibleH && !visibleV)
            {   // Pokud není problém ani Horizonal, ani Vertical => Obsah se vejde do controlu = není aktivní AutoScrolling:
                this._AutoScrollReset();
                return;
            }

            // Některý ScrollBar bude vidět - podle aktuálního prostoru a obsahu zajistím korekci bodu počátku:
            int firstPixelX = this._GetFirstVisiblePixel(this._FirstPixel.X, visibleH, contentSize.Width, virtualBounds.Width);
            int firstPixelY = this._GetFirstVisiblePixel(this._FirstPixel.Y, visibleV, contentSize.Height, virtualBounds.Height);

            this._AutoScrollSet(new Point(firstPixelX, firstPixelY), virtualBounds, visibleH, visibleV, contentSize, rulerThick, scrollBarThick);
        }
        /// <summary>
        /// Vrátí sumární velikost všech základních itemů v parametru
        /// </summary>
        /// <returns></returns>
        private Size _GetContentSize()
        {
            IEnumerable<IInteractiveItem> childItems = this._Owner.ChildItems;
            if (childItems == null) return Size.Empty;
            int r = 0;
            int b = 0;
            foreach (IInteractiveItem item in childItems)
            {
                Rectangle bounds = item.Bounds;
                if (r < bounds.Right) r = bounds.Right;
                if (b < bounds.Bottom) b = bounds.Bottom;
            }
            return new Size(r, b);
        }
        /// <summary>
        /// Metoda zjistí, zda aktuální obsah (jehož velikost je <paramref name="contentSize"/>) se bude zobrazovat nativně = bez ScrollBarů,
        /// anebo bude použito scrollování obsahu.
        /// Výstupem je souřadnice prostoru pro zobrazený obsah:
        /// Prostor je zmenšen o potřebná pravítka (vlevo a nahoře) a o potřebné scrollbary (vpravo a dole),
        /// a out parametry obsahují viditelnost scrollbarů a pravítek (vertikální a horizontální).
        /// <para/>
        /// Metoda v případě viditelnosti scrollbaru navýší velikost obsahu = parametr ref <paramref name="contentSize"/> o patřičnou hodnotu z <see cref="AutoScrollMargin"/>.
        /// </summary>
        /// <param name="contentSize">In/Out velikost prostoru obsahu (na výstupu může být navýšena o velikost okraje <see cref="AutoScrollMargin"/> v potřebných směrech)</param>
        /// <param name="visibleH">Out viditelnost vodorovného pravítka a scrollbaru</param>
        /// <param name="visibleV">Out viditelnost svislého pravítka a scrollbaru</param>
        /// <param name="rulerThick">Out šíře pravítka</param>
        /// <param name="scrollBarThick">Out šíře scrollbaru</param>
        /// <returns></returns>
        private Rectangle _GetVisibleBounds(ref Size contentSize, out bool visibleH, out bool visibleV, out int rulerThick, out int scrollBarThick)
        {
            Size hostSize = this.HostSize;
            int sizeW = hostSize.Width;
            int sizeH = hostSize.Height;
            int contentW = contentSize.Width;
            int contentH = contentSize.Height;
            visibleH = (contentW >= sizeW);
            visibleV = (contentH >= sizeH);
            rulerThick = 0;
            scrollBarThick = 0;
            if (visibleH || visibleV)
            {   // Některý scrollbar bude viditelný - budu zmenšovat velikost prostoru o prostor scrollbarů:
                rulerThick = 0;
                scrollBarThick = Skin.ScrollBar.ScrollThick;
                int thick = rulerThick + scrollBarThick;

                if (visibleH && visibleV)
                {   // Budou viditelné oba scrollbary: 
                    sizeW -= thick;
                    sizeH -= thick;
                }
                else if (visibleH && !visibleV)
                {   // Musím zobrazit Horizontální scrollbar:
                    sizeH -= thick;                        // Tak tedy zmenším výšku prostoru o výšku vodorovného scrollbaru
                    visibleV = (contentH >= sizeH);        //  a zjistím, jestli po zmenšení výšky nebudu muset zobrazit i svislý scrollbar
                    if (visibleV) sizeW -= thick;          //  pokud ano, zmenším i šířku prostoru o pravý svislý scrollbar
                }
                else if (!visibleH && visibleV)
                {   // Musím zobrazit Vertikální scrollbar:
                    sizeW -= thick;                        // Tak tedy zmenším šířku prostoru o šířku svislého scrollbaru
                    visibleH = (contentW >= sizeW);        //  a zjistím, jestli po zmenšení šířky nebudu muset zobrazit i vodorovný scrollbar
                    if (visibleH) sizeH -= thick;          //  pokud ano, zmenším i výšku prostoru o dolní vodorovný scrollbar
                }

                // Velikost obsahu (parametr ref contentSize) zvětším o Margin v každém směru, ve kterém bude zobrazen Scrollbar:
                //  (tento Margin vizuálně zvýrazní, že za posledními prvky v daném směru už není nic)
                Size margin = this.AutoScrollMargin;
                if (visibleH) contentW += margin.Width;
                if (visibleV) contentH += margin.Height;
                contentSize = new Size(contentW, contentH);
            }

            // Souřadnice prostoru pro virtuální obsah:
            if (sizeW < 0) sizeW = 0;
            if (sizeH < 0) sizeH = 0;
            return new Rectangle((visibleV ? rulerThick : 0), (visibleH ? rulerThick : 0), sizeW, sizeH);
        }
        /// <summary>
        /// Metoda vrátí novou souřadnici prvního viditelného pixelu (<see cref="_FirstPixel"/>) při zadání:
        /// dosavadní souřadnice prvního viditelného pixelu, viditelnost scrollbaru, velikost obsahu, velikost prostoru
        /// </summary>
        /// <param name="originCurrent">Dosavadní hodnota prvního viditelného pixelu (X/Y), pokud bude možno měli bychom se jí držet</param>
        /// <param name="visibleScrollbar">true pokud je scrollbar viditelný</param>
        /// <param name="contentLength">Velikost obsahu (souhrn z controlů) případně navýšený o Margin v daném směru (Width/Height)</param>
        /// <param name="visibleLength">Velikost viditelného prostoru (Width/Height)</param>
        /// <returns></returns>
        private int _GetFirstVisiblePixel(int originCurrent, bool visibleScrollbar, int contentLength, int visibleLength)
        {
            if (!visibleScrollbar) return 0;

            int originNew = originCurrent;

            // Určíme přesah viditelného prostoru (jeho konec = originCurrent + visibleLength) za datový obsah (contentLength):
            //  Pokud overlap bude kladné, pak o tolik pixelů ukazujeme vpravo/dole více, než je obsah:
            int overlap = originNew + visibleLength - contentLength;

            // Pokud máme kladný přesah, pak o něj posuneme origin doleva/nahoru:
            if (overlap > 0) originNew -= overlap;

            // Nikdy ale nemůže být výsledek menší než 0:
            if (originNew < 0) originNew = 0;

            return originNew;
        }
        /// <summary>
        /// Resetuje aktuální scrollbary
        /// </summary>
        private void _AutoScrollReset()
        {
            Size hostSize = this.HostSize;
            Rectangle virtualBounds = new Rectangle(Point.Empty, hostSize);
            this._AutoScrollReset(virtualBounds, hostSize);
        }
        /// <summary>
        /// Resetuje aktuální scrollbary
        /// </summary>
        /// <param name="virtualBounds"></param>
        /// <param name="contentSize"></param>
        private void _AutoScrollReset(Rectangle virtualBounds, Size contentSize)
        {
            bool callStateChanged = this._AutoScrollActive;          // Zavolat událost OnAutoScrollStateChanged: pokud dosavadní hodnota _AutoScrollActive byla true

            this._AutoScrollActive = false;
            this._AutoScrollBounds = virtualBounds;
            this._ContentSize = contentSize;
            this._ScrollVisibleH = false;
            this._ScrollVisibleV = false;
            this._ScrollBars = null;

            if (callStateChanged)
                this._Owner.OnAutoScrollActiveChanged();
        }
        /// <summary>
        /// Nastaví dané hodnoty jako stav AutoScroll
        /// </summary>
        /// <param name="firstPixel"></param>
        /// <param name="virtualBounds"></param>
        /// <param name="visibleH"></param>
        /// <param name="visibleV"></param>
        /// <param name="contentSize"></param>
        /// <param name="rulerThick"></param>
        /// <param name="scrollBarThick"></param>
        private void _AutoScrollSet(Point firstPixel, Rectangle virtualBounds, bool visibleH, bool visibleV, Size contentSize, int rulerThick, int scrollBarThick)
        {
            bool callStateChanged = !this._AutoScrollActive;         // Zavolat událost OnAutoScrollStateChanged: pokud dosavadní hodnota _AutoScrollActive byla false
            bool callScrollChanged = false;

            this._AutoScrollActive = true;

            bool isCordinateChange = ((firstPixel != this._FirstPixel) || (virtualBounds != this._AutoScrollBounds) || (contentSize != this._ContentSize) || (rulerThick != this._RulerThick) || (scrollBarThick != this._ScrollBarThick));
            bool isVisibleChange = ((visibleH != this._ScrollVisibleH) || (visibleV != this._ScrollVisibleV));

            if (isCordinateChange || isVisibleChange)
            {
                callScrollChanged = true;                            // Zavolat událost OnAutoScrollContentChanged: pokud došlo k nějaké změně hodnoty

                if (isCordinateChange)
                {
                    this._FirstPixel = firstPixel;
                    this._AutoScrollBounds = virtualBounds;
                    this._ContentSize = contentSize;
                    this._RulerThick = rulerThick;
                    this._ScrollBarThick = scrollBarThick;
                }

                if (isVisibleChange)
                {
                    this._ScrollVisibleH = visibleH;
                    this._ScrollVisibleV = visibleV;
                }

                // pravítka zatím nedělám...
                this._AutoScrollBarHSet();
                this._AutoScrollBarVSet();
                this._AutoScrollBarSSet();
                if (this._ScrollBars == null || isVisibleChange)
                {
                    this._ScrollBars = new List<IInteractiveItem>();
                    if (visibleH) this._ScrollBars.Add(this._ScrollBarH);
                    if (visibleV) this._ScrollBars.Add(this._ScrollBarV);
                    if (visibleH && visibleV) this._ScrollBars.Add(this._ScrollBarS);
                }
            }

            if (callStateChanged)
                this._Owner.OnAutoScrollActiveChanged();
            if (callScrollChanged)
                this._Owner.OnAutoScrollContentChanged();
        }
        /// <summary>
        /// Metoda určí novou souřadnici pro viditelný prostor na základě dané hodnoty ze Scrollbaru
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="value"></param>
        private void _AutoScrollValueSet(Orientation orientation, int value)
        {
            Point firstPixelOld = this._FirstPixel;
            Point firstPixelNew = (orientation == Orientation.Horizontal ? new Point(value, firstPixelOld.Y) : new Point(firstPixelOld.X, value));
            if (firstPixelNew != firstPixelOld)
            {
                this._FirstPixel = firstPixelNew;
                this._Owner.OnAutoScrollPositionChanged();
            }
        }
        /// <summary>
        /// Kompletně připraví <see cref="_ScrollBarH"/> (tj. existenci, eventhandlery, souřadnice, hodnoty)
        /// </summary>
        private void _AutoScrollBarHSet()
        {
            this._AutoScrollBarSet(ref this._ScrollBarH, this._ScrollVisibleH, Orientation.Horizontal, this._FirstPixel.X, this._AutoScrollBounds.Width, this._ContentSize.Width);
        }
        /// <summary>
        /// Kompletně připraví <see cref="_ScrollBarV"/> (tj. existenci, eventhandlery, souřadnice, hodnoty)
        /// </summary>
        private void _AutoScrollBarVSet()
        {
            this._AutoScrollBarSet(ref this._ScrollBarV, this._ScrollVisibleV, Orientation.Vertical, this._FirstPixel.Y, this._AutoScrollBounds.Height, this._ContentSize.Height);
        }
        /// <summary>
        /// Připraví objekt Space (rožek vpravo dole pro případ zobrazení obou Scrollbarů)
        /// </summary>
        private void _AutoScrollBarSSet()
        {
            if (!this.AutoScroll || !this._ScrollVisibleBoth) return;
            if (this._ScrollBarS == null)
            {
                this._ScrollBarS = new InteractiveObject(this._Owner as IInteractiveParent);
                this._ScrollBarS.Is.OnPhysicalBounds = true;
            }
            Size clientSize = this.HostSize;
            int scrollBarThick = this._ScrollBarThick;
            this._ScrollBarS.Bounds = new Rectangle(clientSize.Width - scrollBarThick, clientSize.Height - scrollBarThick, scrollBarThick, scrollBarThick);
            this._ScrollBarS.BackColor = Skin.ScrollBar.BackColorArea;
        }
        /// <summary>
        /// Kompletně připraví dodaný <see cref="ScrollBar"/> (tj. existenci, eventhandlery, souřadnice, hodnoty) podle parametrů
        /// </summary>
        private void _AutoScrollBarSet(ref ScrollBar scrollBar, bool isVisible, Orientation orientation, int value, int visibleLength, int totalLength)
        {
            if (!isVisible) return;

            if (scrollBar == null)
            {   // Scrollbary vytvářím až on-demand:
                scrollBar = new ScrollBar(this._Owner as IInteractiveParent)
                {
                    Orientation = orientation
                };
                scrollBar.Is.OnPhysicalBounds = true;
                scrollBar.ValueChanging += ScrollBar_ValueChanges;
                scrollBar.ValueChanged += ScrollBar_ValueChanges;
            }

            Size clientSize = this.HostSize;
            int scrollBarThick = this._ScrollBarThick;
            int endSpace = (this._ScrollVisibleBoth ? scrollBarThick : 0);
            Rectangle scrollBounds = (orientation == Orientation.Horizontal ?
                new Rectangle(0, clientSize.Height - scrollBarThick, clientSize.Width - endSpace, scrollBarThick) :
                new Rectangle(clientSize.Width - scrollBarThick, 0, scrollBarThick, clientSize.Height - endSpace));

            using (scrollBar.SuppressEvents())
            {   // Teď mě nezajímají eventy, které bude ScrollBar generovat při vkládání hodnot:
                scrollBar.Bounds = scrollBounds;
                scrollBar.ValueTotal = new DecimalNRange(0, totalLength);
                scrollBar.Value = new DecimalNRange(value, value + visibleLength);
            }
        }
        /// <summary>
        /// Obsluha událostí ValueChanging a ValueChanged pro oba ScrollBary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollBar_ValueChanges(object sender, GPropertyChangeArgs<DecimalNRange> e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            if (scrollBar == null || e.NewValue == null || !e.NewValue.Begin.HasValue) return;
            int value = (int)Math.Round(e.NewValue.Begin.Value, 0);
            this._AutoScrollValueSet(scrollBar.Orientation, value);
        }
        /// <summary>
        /// Souřadnice prvního logického pixelu, který je aktuálně viditelný.
        /// Má hodnotu {0,0} až {99999,99999}; nikdy není záporný, vyjadřuje první logický bod controlu který je zobrazen na souřadnici {0,0} fyzického controlu.
        /// Výchozí hodnota je {0,0}. 
        /// Když se fyzicky zvětší control, pak se posouvá zobrazovaná oblast tak, aby pokud možno koncové okraje logického controlu byly co nejblíže vpravo a dole 
        /// = zmenšuje se hodnota v <see cref="_FirstPixel"/> (=např. při zvětšování šířky controlu nedochází k tomu, že by logický konec obsahu
        /// zůstával hodně vlevo, a vpravo by se zobrazoval prázdný prostor)
        /// </summary>
        private Point _FirstPixel;
        /// <summary>
        /// Souřadnice prostoru uvnitř this prvku, kde se zobrazuje virtuální obsah.
        /// Typicky začíná bodem 0,0 (první pixel v controlu vlevo nahoře), 
        /// a má velikost vnitřku controlu (<see cref="HostSize"/>) zmenšenou o ScrollBary.
        /// Souvisí tedy s fyzickým controlem, nikoli s virtuální plochou - do zde daných souřadnic se virtuální plocha "promítá".
        /// </summary>
        private Rectangle _AutoScrollBounds;
        /// <summary>
        /// Aktuální velikost celkové oblasti controlů (Items [+ Margin])
        /// </summary>
        private Size _ContentSize;
        /// <summary>
        /// Obsahuje true pokud jsou zobrazovány oba Scrollbary (pak je vpravo dole zobrazen prázdný čtvereček)
        /// </summary>
        private bool _ScrollVisibleBoth { get { return (this._ScrollVisibleH && this._ScrollVisibleV); } }
        /// <summary>
        /// Objekt horizontálního (vodorovného) Scrollbaru
        /// </summary>
        private ScrollBar _ScrollBarH;
        /// <summary>
        /// Obsahuje true pokud je zobrazován vodorovný scrollbar
        /// </summary>
        private bool _ScrollVisibleH;
        /// <summary>
        /// Objekt vertikálního (svislého) Scrollbaru
        /// </summary>
        private ScrollBar _ScrollBarV;
        /// <summary>
        /// Toto není ScrollBar, ale jen výplň pravého dolního rohu mezi oběma Scrollbary (H + V), pokud jsou oba viditelné.
        /// </summary>
        private InteractiveObject _ScrollBarS;
        /// <summary>
        /// Obsahuje true pokud je zobrazován svislý scrollbar
        /// </summary>
        private bool _ScrollVisibleV;
        /// <summary>
        /// Šíře pravítek, se kterou je počítáno v layoutu
        /// </summary>
        private int _RulerThick;
        /// <summary>
        /// Šíře ScrollBaru, se kterou je počítáno v layoutu
        /// </summary>
        private int _ScrollBarThick;
        /// <summary>
        /// Pole Scrollbarů, pokud mají být aktivní.
        /// </summary>
        private List<IInteractiveItem> _ScrollBars;
        #endregion
        #region Posunutí obsahu vyžádané externě
        /// <summary>
        /// Posune aktuální obsah o danou hodnotu.
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="change"></param>
        /// <param name="modifierKeys"></param>
        public void ScrollBy(Orientation orientation, GInteractiveChangeState change, Keys modifierKeys = Keys.None)
        {
            if (!this._AutoScrollActive) return;
            switch (orientation)
            {
                case Orientation.Horizontal:
                    if (this._ScrollVisibleH)
                        this._ScrollBarH.DoScrollBy(change, modifierKeys);
                    break;
                case Orientation.Vertical:
                    if (this._ScrollVisibleV)
                        this._ScrollBarV.DoScrollBy(change, modifierKeys);
                    break;
            }
        }
        /// <summary>
        /// Posune aktuální obsah tak, aby byly fyzicky viditelné dané souřadnice.
        /// </summary>
        /// <param name="absoluteBounds"></param>
        internal void ScrollToShow(Rectangle absoluteBounds)
        {
            if (!this._AutoScrollActive) return;
            if (this._ScrollVisibleH)
            {
                int dx = this.VirtualOrigin.X;
                this._ScrollBarH.ScrollToShow(new DecimalNRange(absoluteBounds.Left - dx, absoluteBounds.Right - dx), AutoScrollMargin.Width);
            }
            if (this._ScrollVisibleV)
            {
                int dy = this.VirtualOrigin.Y;
                this._ScrollBarV.ScrollToShow(new DecimalNRange(absoluteBounds.Top - dy, absoluteBounds.Bottom - dy), AutoScrollMargin.Height);
            }
        }
        #endregion
    }
    #region interface IAutoScrollContainer : Předpis pro majitele AutoScrollSupport
    /// <summary>
    /// Předpis pro majitele <see cref="AutoScrollSupport"/>
    /// </summary>
    public interface IAutoScrollContainer
    {
        /// <summary>
        /// Režim AutoScroll je aktivní (=je povolen, a aktuálně je z důvodu potřeby zobrazen alespoň jeden ScrollBar)
        /// </summary>
        bool AutoScrollActive { get; }
        /// <summary>
        /// Souřadnice počátku = na které souřadnici našeho aktuálního prostoru se nachází virtuální bod 0/0.
        /// Příklad: Mějme AutoScrollContainer, jehož využitelný vnitřní prostor je 300 x 200 px, ale zobrazuje obsah o velikosti 600 x 800 px.
        /// Aktuálně máme odscrollováno tak, že první viditelný pixel obsahu má pozici X=10, Y=50 (to lze načíst ze scrollbarů jako jejich Value.Begin).
        /// Znamená to, že virtuální bod 0,0 leží doleva (o 10px) a nahoru (o 50px) od zobrazovaného prvního pixelu.
        /// V tomto příkladu je tedy <see cref="VirtualOrigin"/> = { -10, -50 }.
        /// </summary>
        Point VirtualOrigin { get; }
        /// <summary>
        /// Souřadnice prostoru uvnitř klientské části this controlu, kde se zobrazují Child prvky.
        /// Pokud AutoScrollContainer je běžný prvek se scrollbary vpravo a dole, pak souřadnice <see cref="VirtualBounds"/> jsou { 0, 0, Width-scrollbar, Height-scrollbar }.
        /// </summary>
        Rectangle VirtualBounds { get; }
        /// <summary>
        /// Velikost celého fyzického prostoru hostitele, tj. včetně prostoru pro Scrollbary.
        /// AutoScroll jej může využít pro prostor <see cref="IAutoScrollContainer.VirtualBounds"/> a pro umístění Scrollbarů.
        /// </summary>
        Size HostSize { get; }
        /// <summary>
        /// Tato metoda je volaná tehdy, když se změní stav <see cref="AutoScrollActive"/>
        /// </summary>
        void OnAutoScrollActiveChanged();
        /// <summary>
        /// Tato metoda je volaná tehdy, když se změní pozice nebo velikost obsahu vlivem změny velikosti controlu nebo obsahu
        /// </summary>
        void OnAutoScrollContentChanged();
        /// <summary>
        /// Tato metoda je volaná tehdy, když uživatel aktivně pomocí Scrollbaru (nebo jinak = kolečkem myši) změní pozici obsahu
        /// </summary>
        void OnAutoScrollPositionChanged();
        /// <summary>
        /// Zobrazované interaktivní prvky, mimo ScrollBary.
        /// Tuto property čte <see cref="AutoScrollSupport"/> ze svého Ownera, pro tyto prvky počítá jejich sumární zobrazené souřadnice.
        /// </summary>
        IEnumerable<IInteractiveItem> ChildItems { get; }
        /// <summary>
        /// Scrollbary aktuálně přítomné v this prvku. 
        /// Toto pole může obsahovat i "prázdný čtvereček" vpravo dole, pokud jsou zobrazeny oba ScrollBary = pro "překreslení" případných prvků (tam se běžné prvky nemají vyskytovat)
        /// Tuto property čtou metody pro vykreslování jednotlivých prvků v rámci controlu.
        /// </summary>
        IEnumerable<IInteractiveItem> ScrollBars { get; }
    }
    #endregion
    #endregion
}
