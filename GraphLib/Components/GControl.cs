using System;
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
                this.Draw();
                this.Invalidate();
                base.Refresh();
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
                // Pokud jsem dosud nebyl kreslen, pak ignorujeme explicitní požadavek (ten říká: kresli ken něco), a budeme kreslit vše:
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
                e.GetGraphicsForLayer(0, true).Clear(this.BackColor);
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
        /// Vyvolá se po každém vykreslení
        /// </summary>
        private void _RunOnDrawAfter()
        {
            if (!this._IsAtLeastOnceDrawed)
            {
                var form = this.FindForm();
                if (form != null && form.Visible)
                {
                    this._IsAtLeastOnceDrawed = true;
                    this.OnFirstDrawAfter();
                }
            }
            this.OnDrawAfter();
        }
        private bool _IsAtLeastOnceDrawed = false;
        /// <summary>
        /// Metoda je volána POUZE PO PRVNÍM vykreslení obsahu
        /// </summary>
        protected virtual void OnFirstDrawAfter() { }
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



    /// <summary>
    /// Argument for OnLayeredPaint() method / LayeredPaint event
    /// </summary>
    public class oldLayeredPaintEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="layerCount"></param>
        /// <param name="getGraphics"></param>
        /// <param name="copyContentOfLayer"></param>
        /// <param name="layersToPaint"></param>
        /// <param name="userData"></param>
        public oldLayeredPaintEventArgs(int layerCount, Func<int, Graphics> getGraphics, Action<int, int> copyContentOfLayer, IEnumerable<int> layersToPaint, object userData)
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
        /// <summary>
        /// Počet grafických vrstev; používá se při kontrole zadaného indexu vrstvy
        /// </summary>
        private int _LayerCount;
        /// <summary>
        /// Odkaz na metodu, která vrací instanci <see cref="Graphics "/> pro danou vrstvu
        /// </summary>
        private Func<int, Graphics> _GetGraphics;
        /// <summary>
        /// Odkaz na metodu, která překopíruje obsah vrstvy (1) do vrstvy (2)
        /// </summary>
        private Action<int, int> _CopyContentOfLayer;
        /// <summary>
        /// Vrstvy, které mají být kresleny
        /// </summary>
        private Dictionary<int, object> _LayersToPaint;
        /// <summary>
        /// Vrátí true, pokud dané číslo vrstvy je správné a je možno s touto vrstvou pracovat
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool LayerExists(int layer)
        {
            return (layer > 0 && layer < this._LayerCount);
        }
        /// <summary>
        /// Vrací true, pokud se do dané vrstvy má kreslit
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
        /// Souhrn vrstev, do kterých se má kreslit
        /// </summary>
        public IEnumerable<int> LayersToPaint { get { return this._LayersToPaint.Keys; } }
        /// <summary>
        /// Vrátí instanci <see cref="Graphics"/> pro danou vrstvu.
        /// Může vrátit null pro nesprávné číslo vrstvy.
        /// Nastavuje danou vrstvu do <see cref="ValidLayer"/>.
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
        /// Nastavuje danou cílovou vrstvu (layerTo) do <see cref="ValidLayer"/>.
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
        /// Libovolná data, předaná do metody Draw.
        /// </summary>
        public object UserData { get; private set; }
        /// <summary>
        /// Index vrstvy, která obsahuje validní data.
        /// Automaticky se zde udržuje index vrstvy, která byla posledním cílem operace CopyContentOfLayer(), anebo která byla naposledy vyzvednuta ke kreslení metodou GraphicsForLayer().
        /// Nicméně aplikace může na konci metody override OnPaintLayers() vložit do argumentu do property ValidLayer libovolnou vrstvu, která se bude používat jako zdroj obrazu pro vykreslení controlu.
        /// </summary>
        public int ValidLayer { get; private set; }
    }
    #endregion
    #endregion
}
