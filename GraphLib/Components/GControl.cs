using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Asol.Tools.WorkScheduler.Components
{
    #region GControl : Control with buffered graphic
    /// <summary>
    /// GControl: Control with buffered graphic
    /// </summary>
    public class GControl : Control, IDisposable
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GControl()
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
    #region GControlLayered : Třída určená ke kreslení grafiky na plochu. Varianta pro složitější interaktivní motivy.
    /// <summary>
    /// Třída určená ke kreslení grafiky na plochu - absolutně bez blikání.
    /// Varianta pro složitější interaktivní motivy: nabízí více vrstev pro kreslení.
    /// </summary>
    public class GControlLayered : Control, IDisposable
    {
        #region Konstruktor, private event handlers
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GControlLayered()
        {
            this._LayerList = new List<GraphicLayer>();
            this.LayerCount = 1;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResizeRedraw = true;
        }
        /// <summary>
        /// Změna velikosti controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);               // WinForm kód: zde proběhnou volání eventhandlerů Resize i SizeChanged (v tomto pořadí)

            this._OnResizeControl();
            this._ResizeLayers(false);
            this.Draw();
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
        /// </summary>
        public override void Refresh()
        {
            this.Draw();
            this.Invalidate();
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
            set
            {
                int cnt = value;
                if (cnt < 1) cnt = 1;
                if (cnt > 10) cnt = 10;
                if (cnt == this._LayerCount) return;
                this._LayerCount = cnt;
                this._CreateLayers();
                this.Draw();
            }
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
                this._Draw(null, null);
            this._Draw(null, userData);
        }
        /// <summary>
        /// Tato metoda se volá tehdy, když aplikace chce překreslit celý objekt.
        /// </summary>
        public void Draw(IEnumerable<int> drawLayers, object userData)
        {
            if (!this.IsPainted)
                this._Draw(null, null);
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
            if (e.HasPaintLayer(0))
                e.GraphicsForLayer(0).Clear(this.BackColor);
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
                this.DrawInProgress = true;
            
                LayeredPaintEventArgs e = new LayeredPaintEventArgs(this._LayerCount, this._GetGraphicsForLayer, this._CopyContentOfLayer, drawLayers, userData);
                this._OnPaintLayers(e);
                this._ValidLayer = e.ValidLayer;
                this.PendingFullDraw = false;
                this._RenderValidLayerTo(this.CreateGraphics());
            }
            finally
            {
                this.DrawInProgress = false;
            }
        }
        /// <summary>
        /// Číslo Layeru, který obsahuje validní informaci, která se bude renderovat do controlu.
        /// Určuje ji argument LayeredPaintEventArgs spolu s metodami OnPaintLayers().
        /// </summary>
        private int _ValidLayer;
        /// <summary>
        /// Vrátí objekt Graphics pro danou vrstvu layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private Graphics _GetGraphicsForLayer(int layer)
        {
            Graphics graphics = null;
            if (this._LayerList != null && layer >= 0 && layer < this._LayerList.Count)
            {
                graphics = this._LayerList[layer].LayerGraphics;
                graphics.ResetClip();
            }
            return graphics;
        }
        /// <summary>
        /// Kopíruje obsah vrstvy (layerFrom) to vrstvy (layerTo)
        /// </summary>
        /// <param name="layerFrom"></param>
        /// <param name="layerTo"></param>
        private void _CopyContentOfLayer(int layerFrom, int layerTo)
        {
            if (layerFrom > layerTo) return;
            GraphicLayer layerSource = this._LayerList[layerFrom];
            Graphics graphicsTarget = this._LayerList[layerTo].LayerGraphics;
            layerSource.RenderTo(graphicsTarget);
        }
        /// <summary>
        /// Fyzicky překreslí obsah bufferů do předané grafiky.
        /// </summary>
        /// <param name="graphics"></param>
        private void _RenderValidLayerTo(Graphics graphics)
        {
            int validLayer = this._ValidLayer;
            if (validLayer >= 0 && validLayer < this._LayerCount)
                this._LayerList[validLayer].RenderTo(graphics);
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
        #region Řízení vrstev: Create, Resize, Dispose. Nikoliv kreslení.
        private void _CreateLayers()
        {
            Size size = this.Size;
            this._LayerList = new List<GraphicLayer>();
            for (int l = 0; l < this._LayerCount; l++)
                this._LayerList.Add(new GraphicLayer(this, this.Size));
        }
        private void _ResizeLayers(bool callDraw)
        {
            this._CreateLayers();
            if (callDraw)
                this.Draw();
        }
        private void _DisposeLayers()
        {
            if (this._LayerList != null)
            {
                foreach (GraphicLayer layer in this._LayerList)
                    ((IDisposable)layer).Dispose();
            }
            this._LayerList = null;
            this._LayerCount = 0;
        }
        private int _LayerCount;
        private List<GraphicLayer> _LayerList;
        #endregion
        #region Dispose
        void IDisposable.Dispose()
        {
            this._DisposeLayers();
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
        #region class GraphicLayer : třída představující jednu grafickou vrstvu
        /// <summary>
        /// GraphicLayer : třída představující jednu grafickou vrstvu
        /// </summary>
        protected class GraphicLayer : IDisposable
        {
            #region Constructor
            /// <summary>
            /// Constructor (owner = Control, Size)
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="size"></param>
            public GraphicLayer(Control owner, Size size)
            {
                this._Owner = owner;
                this.Size = size;
            }
            private Control _Owner;
            /// <summary>
            /// Controll mechanism for buffered graphics
            /// </summary>
            private BufferedGraphicsContext _GraphicsContext;
            /// <summary>
            /// Content of graphic buffer
            /// </summary>
            private BufferedGraphics _GraphicsData;
            /// <summary>
            /// Size of buffered sheet
            /// </summary>
            private Size _Size;
            #endregion
            #region Public property a metody
            /// <summary>
            /// true pokud je objekt platný
            /// </summary>
            public bool IsValid
            {
                get { return (this._Size.Width > 0 && this._Size.Height > 0 && this._GraphicsContext != null && this._GraphicsData != null); }
            }
            /// <summary>
            /// Rozměr vrstvy.
            /// Lze nastavit, vrstva se upraví. Poté je třeba překreslit (vrstva sama si nevolá).
            /// </summary>
            public Size Size
            {
                get { return this._Size; }
                set
                {
                    if (this.IsValid && value == this._Size) return;         // Není důvod ke změně
                    this._Size = value;
                    this.CreateLayer();
                }
            }
            /// <summary>
            /// Objekt Graphics, který dovoluje kreslit motivy do této vrstvy
            /// </summary>
            public Graphics LayerGraphics { get { return this._GraphicsData.Graphics; } }
            /// <summary>
            /// Vykreslí svůj obsah do dané cílové Graphics, typicky při kreslení Controlu (skládají se jednotlivé vrstvy).
            /// </summary>
            /// <param name="targetGraphic"></param>
            public void RenderTo(Graphics targetGraphic)
            {
                targetGraphic.CompositingMode = CompositingMode.SourceOver;
                this._GraphicsData.Graphics.CompositingMode = CompositingMode.SourceOver;
                this._GraphicsData.Render(targetGraphic);
            }
            #endregion
            #region Privátní tvorba grafiky, IDisposable
            /// <summary>
            /// Vytvoří bufferovaný layer pro kreslení do this vrstvy
            /// </summary>
            protected void CreateLayer()
            {
                if (this._GraphicsContext == null)
                    this._GraphicsContext = BufferedGraphicsManager.Current;

                Size s = this._Size;
                if (s.Width <= 0) s.Width = 1;
                if (s.Height <= 0) s.Height = 1;

                this._GraphicsContext.MaximumBuffer = new Size(s.Width + 1, s.Height + 1);

                if (this._GraphicsData != null)
                    this._GraphicsData.Dispose();

                this._GraphicsData = this._GraphicsContext.Allocate(this._Owner.CreateGraphics(), new Rectangle(new Point(0, 0), s));
            }
            void IDisposable.Dispose()
            {
                if (this._GraphicsContext != null)
                    this._GraphicsContext.Dispose();
                this._GraphicsContext = null;

                if (this._GraphicsData != null)
                    this._GraphicsData.Dispose();
                this._GraphicsData = null;

                this._Owner = null;
            }
            #endregion
        }
        #endregion
    }
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
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="layerCount"></param>
        /// <param name="getGraphics"></param>
        /// <param name="copyContentOfLayer"></param>
        /// <param name="layersToPaint"></param>
        /// <param name="userData"></param>
        public LayeredPaintEventArgs(int layerCount, Func<int, Graphics> getGraphics, Action<int, int> copyContentOfLayer, IEnumerable<int> layersToPaint, object userData)
        {
            this._LayerCount = layerCount;
            this._GetGraphics = getGraphics;
            this._CopyContentOfLayer = copyContentOfLayer;

            this._LayersToPaint = new Dictionary<int, object>();
            if (layersToPaint != null)
            {
                foreach (int layer in layersToPaint)
                {
                    if (this.LayerExists(layer) && !this._LayersToPaint.ContainsKey(layer))
                        this._LayersToPaint.Add(layer, null);
                }
            }
            else
            {
                for (int layer = 0; layer < layerCount; layer++)
                    this._LayersToPaint.Add(layer, null);
            }

            this.UserData = userData;
        }
        /// <summary>Number of layers, used for check of layer number</summary>
        private int _LayerCount;
        /// <summary>Pointer to method, which returns an Graphics object for specified layer</summary>
        private Func<int, Graphics> _GetGraphics;
        /// <summary>Pointer to method, which clone content of layer (1) to layer (2)</summary>
        private Action<int, int> _CopyContentOfLayer;
        /// <summary>Sum of layers, which will be painted</summary>
        private Dictionary<int, object> _LayersToPaint;
        /// <summary>
        /// Return a true, if specified layer number exists (and can be create Graphics object for this layer)
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool LayerExists(int layer)
        {
            if (layer < 0 || layer >= this._LayerCount) return false;
            return true;
        }
        /// <summary>
        /// Return a true, if need paint to specific layer
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool HasPaintLayer(int layer)
        {
            if (!this.LayerExists(layer)) return false;
            if (this._LayersToPaint == null) return true;
            return this._LayersToPaint.ContainsKey(layer);
        }
        /// <summary>
        /// Sum of layers, which will be painted
        /// </summary>
        public IEnumerable<int> LayersToPaint { get { return this._LayersToPaint.Keys; } }
        /// <summary>
        /// Returns an Graphics object for specified layer.
        /// Returns no-null object even for layers, for which is paint unnecessary.
        /// Returns null object for non existing layers.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public Graphics GraphicsForLayer(int layer)
        {
            this.ValidLayer = layer;
            return this._GetGraphics(layer);
        }
        /// <summary>
        /// Zkopíruje obsah vrstvy (layerFrom) do vrstvy (layerTo).
        /// Používá se při kreslení jen části motivu, kdy se jako podklad přebírá již dříve připravený obsah.
        /// </summary>
        /// <param name="layerFrom"></param>
        /// <param name="layerTo"></param>
        public void CopyContentOfLayer(int layerFrom, int layerTo)
        {
            if (layerFrom < layerTo)
                this._CopyContentOfLayer(layerFrom, layerTo);
            this.ValidLayer = layerTo;
        }
        /// <summary>
        /// Libovolná data, předaná do metody ReDraw.
        /// </summary>
        public object UserData { get; private set; }
        /// <summary>
        /// Index vrstvy, která obsahuje validní data.
        /// Automaticky se zde udržuje index vrstvy, která byla posledním cílem operace CopyContentOfLayer(), anebo která byla naposledy vyzvednuta ke kreslení metodou GraphicsForLayer().
        /// Nicméně aplikace může na konci metody override OnPaintLayers() vložit do argumentu do property ValidLayer libovolnou vrstvu, která se bude používat jako zdroj obrazu pro vykreslení controlu.
        /// </summary>
        public int ValidLayer { get; set; }
    }
    #endregion
    #endregion
}
