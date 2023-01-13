using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DjSoft.Games.Animated.Components
{
    /// <summary>
    /// Sestava více vrstev bufferů grafiky.
    /// Konkrétně řízené vrstvy pro cílené využití (<see cref="BackgroundLayer"/>, <see cref="StandardLayer"/>, <see cref="OverlayLayer"/>, <see cref="ToolTipLayer"/>).
    /// </summary>
    public class LayeredGraphicStandard : LayeredGraphicBase
    {
        /// <summary>
        /// Konstruktor.
        /// Převezme referenci na owner control, zaháčkuje do něj svoje eventy, a při jeho Dispose (event Disposed) si provede svůj vlastní Dispose.
        /// </summary>
        /// <param name="owner"></param>
        public LayeredGraphicStandard(Control owner)
            : base(owner, 0)
        { }
        #region Jednotlivé konkrétní vrstvy a jejich přepínače
        /// <summary>
        /// Obsahuje true, pokud se používá alespoň jedna vrstva. Výchozí je false.
        /// </summary>
        public bool UseLayeredGraphic { get { return (__UseBackgroundLayer || __UseStandardLayer || __UseOverlayLayer || __UseToolTipLayer); } }
        /// <summary>
        /// Obsahuje true, pokud se používá <see cref="BackgroundLayer"/>. Výchozí je false.
        /// Je vhodné používat vrstvu pro Background, kde bude podkladová barva, protože se urychlí zahájení kreslení.
        /// </summary>
        public bool UseBackgroundLayer { get { return __UseBackgroundLayer; } set { __UseBackgroundLayer = value; _RecalcLayers(); } } private bool __UseBackgroundLayer;
        /// <summary>
        /// Vrstva pro kreslení Background
        /// </summary>
        public GraphicLayer BackgroundLayer { get { return (__UseBackgroundLayer ? this.Layers[__BackgroundLayerIndex] : null); } }
        /// <summary>
        /// Index vrstvy Background
        /// </summary>
        private int __BackgroundLayerIndex;
        /// <summary>
        /// Obsahuje true, pokud se používá <see cref="StandardLayer"/>. Výchozí je false.
        /// </summary>
        public bool UseStandardLayer { get { return __UseStandardLayer; } set { __UseStandardLayer = value; _RecalcLayers(); } } private bool __UseStandardLayer;
        /// <summary>
        /// Vrstva pro kreslení Standard
        /// </summary>
        public GraphicLayer StandardLayer { get { return (__UseStandardLayer ? this.Layers[__StandardLayerIndex] : null); } }
        /// <summary>
        /// Index vrstvy Standard
        /// </summary>
        private int __StandardLayerIndex;
        /// <summary>
        /// Obsahuje true, pokud se používá <see cref="OverlayLayer"/>. Výchozí je false.
        /// </summary>
        public bool UseOverlayLayer { get { return __UseOverlayLayer; } set { __UseOverlayLayer = value; _RecalcLayers(); } } private bool __UseOverlayLayer;
        /// <summary>
        /// Vrstva pro kreslení Overlay
        /// </summary>
        public GraphicLayer OverlayLayer { get { return (__UseOverlayLayer ? this.Layers[__OverlayLayerIndex] : null); } }
        /// <summary>
        /// Index vrstvy Overlay
        /// </summary>
        private int __OverlayLayerIndex;
        /// <summary>
        /// Obsahuje true, pokud se používá <see cref="ToolTipLayer"/>. Výchozí je false.
        /// </summary>
        public bool UseToolTipLayer { get { return __UseToolTipLayer; } set { __UseToolTipLayer = value; _RecalcLayers(); } } private bool __UseToolTipLayer;
        /// <summary>
        /// Vrstva pro kreslení ToolTip
        /// </summary>
        public GraphicLayer ToolTipLayer { get { return (__UseToolTipLayer ? this.Layers[__ToolTipLayerIndex] : null); } }
        /// <summary>
        /// Index vrstvy ToolTip
        /// </summary>
        private int __ToolTipLayerIndex;
        /// <summary>
        /// Přepočte počet vrstev a jednotlivé indexy.
        /// </summary>
        private void _RecalcLayers()
        {
            int index = 0;
            __BackgroundLayerIndex = (__UseBackgroundLayer ? index++ : -1);
            __StandardLayerIndex = (__UseBackgroundLayer ? index++ : -1);
            __OverlayLayerIndex = (__UseBackgroundLayer ? index++ : -1);
            __ToolTipLayerIndex = (__UseBackgroundLayer ? index++ : -1);
            this._LayerCount = index;
        }
        /// <summary>
        /// Aktuální počet vrstev. Nemá smysl pokoušet se setovat hodnotu.
        /// </summary>
        public override int LayerCount { get { return base.LayerCount; } set { } }
        #endregion
    }
    /// <summary>
    /// Sestava více vrstev bufferů grafiky. Volitelný počet vrstev, bez podpory konkrétního využití vrstev.
    /// Tato instance není <see cref="IDisposable"/>, ale hlídá si Dispose svého Ownera (=Control, v němž je umístěna), 
    /// a v jeho události Disposed je tato instance korektně uvolněna z paměti.
    /// </summary>
    public class LayeredGraphicBase : ILayeredGraphicBaseWorking
    {
        #region Konstruktor, Owner, Events
        /// <summary>
        /// Konstruktor.
        /// Převezme referenci na owner control, zaháčkuje do něj svoje eventy, a při jeho Dispose (event Disposed) si provede svůj vlastní Dispose.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="layerCount"></param>
        public LayeredGraphicBase(Control owner, int layerCount = 0)
        {
            if (owner is null) throw new ArgumentNullException("Třída 'LayeredGraphicBuffer' vyžaduje dodání vlastníka 'Control owner'.");
            this.Owner = owner;
            this.LayerCount = layerCount;
            _OwnerEventsAttach(owner);
        }
        /// <summary>WeakReference na Owner Control</summary>
        private WeakReference<System.Windows.Forms.Control> __Owner;
        /// <summary>
        /// Obsahuje true, pokud stále máme Ownera.
        /// </summary>
        protected bool HasOwner
        {
            get
            {
                var wr = __Owner;
                return (wr != null && wr.TryGetTarget(out var _));
            }
        }
        /// <summary>
        /// Owner control.
        /// </summary>
        protected Control Owner
        {
            get
            {
                var wr = __Owner;
                if (wr is null || !wr.TryGetTarget(out var owner)) return null;
                return owner;
            }
            set
            {
                __Owner = (value is null) ? null : new WeakReference<Control>(value);
            }
        }
        /// <summary>
        /// Velikost aktivního prostoru Ownera = <see cref="Control.ClientSize"/> = velikost bufferované grafiky.
        /// Pokud Ownera nemáme, nebo má rozměry menší než 1, pak je zde Size (1,1);
        /// </summary>
        protected Size OwnerSize
        {
            get
            {
                var owner = Owner;
                Size ownerSize = (owner?.ClientSize ?? Size.Empty);
                if (ownerSize.Width <= 0 || ownerSize.Height <= 0)
                {
                    int w = (ownerSize.Width > 0 ? ownerSize.Width : 1);
                    int h = (ownerSize.Height > 0 ? ownerSize.Height : 1);
                    ownerSize = new Size(w, h);
                }
                return ownerSize;
            }
        }
        /// <summary>
        /// Počet vrstev.
        /// Lze změnit za provozu.
        /// Povolený rozsah je 0 až 8 (včetně). Hodnota 0 = deaktivovaný systém.
        /// </summary>
        public virtual int LayerCount 
        {
            get { return _LayerCount; }
            set { _LayerCount = value; }
        }
        /// <summary>
        /// Počet vrstev.
        /// Povolený rozsah je 0 až 8 (včetně). Hodnota 0 = deaktivovaný systém.
        /// </summary>
        protected int _LayerCount
        {
            get { return __LayerCount; }
            set { __LayerCount = (value < 0 ? 0 : (value > 8 ? 8 : value)); /* není třeba nic invalidovat, protože získání vrstev kontroluje jejich platnost, tedy i počet */}
        }
        /// <summary>Počet vrstev</summary>
        private int __LayerCount;

        /// <summary>
        /// Zapojí svoje eventhandlery do událostí Owner controlu
        /// </summary>
        /// <param name="owner"></param>
        private void _OwnerEventsAttach(Control owner)
        {
            if (owner is null) return;
            owner.Disposed += _Owner_Disposed;
        }
        /// <summary>
        /// Odpojí svoje eventhandlery do událostí Owner controlu
        /// </summary>
        /// <param name="owner"></param>
        private void _OwnerEventsRemove(Control owner)
        {
            if (owner is null) return;
            owner.Disposed -= _Owner_Disposed;
        }
        /// <summary>
        /// Owner control se disposuje
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Owner_Disposed(object sender, EventArgs e)
        {
            _OwnerEventsRemove(Owner);
            _DisposeLayers();
            Owner = null;
        }
        /// <summary>
        /// Provede danou akci, chyby zahazuje. Používat s rozmyslem, víceméně pro Dispose.
        /// </summary>
        /// <param name="action"></param>
        protected static void TryRun(Action action)
        {
            try { action(); }
            catch (Exception) { }
        }
        #endregion
        #region Layers: tvorba, kontrola platnosti a Dispose jednotlivých vrstev
        /// <summary>
        /// Vytvoří new grafické vrstvy pro aktuální velikost <see cref="Size"/> a pro počet vrstev <see cref="LayerCount"/>.
        /// Automaticky na začátku volá <see cref="_DisposeLayers"/>.
        /// </summary>
        private void _CreateLayers()
        {
            this._DisposeLayers();

            int count = this.LayerCount;

            Size ownerSize = this.OwnerSize;

            GraphicLayer[] layers = new GraphicLayer[count];
            for (int index = 0; index < count; index++)
                layers[index] = new GraphicLayer(this, ownerSize, index);

            this.__Layers = layers;
            this.__LayersSize = ownerSize;
        }
        /// <summary>
        /// Uvolní z paměti stávající grafické vrstvy - vyvolá jejich Dispose, a poté do <see cref="__Layers"/> vloží null.
        /// </summary>
        private void _DisposeLayers()
        {
            var layers = this.__Layers;
            if (layers != null)
            {
                foreach (GraphicLayer layer in layers)
                {
                    if (layer != null)
                        ((IDisposable)layer).Dispose();
                }
            }
            this.__Layers = null;
        }
        /// <summary>
        /// Owner control vytvoří a vrátí svoji grafiku
        /// </summary>
        /// <returns></returns>
        protected Graphics CreateGraphics()
        {
            return this.Owner.CreateGraphics();
        }
        /// <summary>
        /// Obsahuje pole platných vrstev pro aktuální velikost Ownera a požadovaný počet vrstev.
        /// </summary>
        public GraphicLayer[] Layers
        {
            get
            {
                if (!IsValidLayers)
                    _CreateLayers();
                return __Layers;
            }
        }
        /// <summary>
        /// Obsahuje true, když aktuální seznam vrstev <see cref="__Layers"/> obsahuje platné vrstvy ve správném počtu a velikosti.<br/>
        /// Obsahuje false, když: seznam vrstev <see cref="__Layers"/>;
        /// nebo seznam vrstev <see cref="__Layers"/> má jiný počet vrstev než <see cref="LayerCount"/>;
        /// nebo když velikost vrstev <see cref="__LayersSize"/> se neshoduje s aktuální velikostí controlu <see cref="OwnerSize"/>.
        /// </summary>
        protected bool IsValidLayers
        {
            get
            {
                var layers = __Layers;
                if (layers is null) return false;
                if (layers.Length != this.LayerCount) return false;
                if (__LayersSize != OwnerSize) return false;
                return true;
            }
        }
        /// <summary>Fyzické úložiště vrstev</summary>
        private GraphicLayer[] __Layers;
        /// <summary>Velikost, pro kterou byly vytvářeny vrstvy v <see cref="__Layers"/></summary>
        private Size __LayersSize;
        /// <summary>
        /// Vrátí pole aktuálně přítomných vrstev, pokud mezi nimi je daná vrstva.
        /// Většinou vrátí běžné pole vrstev. Ale pokud je toto pole v dané chvíli nevalidní, pak vrátí null a negeneruje nové validní pole.
        /// Stejně tak pokud v mezidobí (od vytvoření dodané vrstvy do volání této metody) došlo ke změně pole, a dodaná vrstva už mezi novými vrstvami fyzicky není, pak vrací null.
        /// </summary>
        LayeredGraphicBase.GraphicLayer[] ILayeredGraphicBaseWorking.GetLayers(LayeredGraphicBase.GraphicLayer layer)
        {
            var layers = __Layers;
            var isValid = IsValidLayers;
            if (layer is null || !isValid || layer.Index < 0 || layer.Index >= layers.Length) return null;
            var testLayer = layers[layer.Index];
            if (!Object.ReferenceEquals(layer, testLayer)) return null;
            return layers;
        }
        /// <summary>
        /// Obsahuje (vyhledá a vrátí) položku <see cref="GraphicLayer"/> ze zdejších vrstev, která má (<see cref="GraphicLayer.IsActive"/> == true and <see cref="GraphicLayer.ContainData"/> == true ),
        /// a je na nejvyšším indexu.
        /// Pokud žádná položka v poli nemá (<see cref="GraphicLayer.ContainData"/> == true), vrací se položka na indexu [0].
        /// </summary>
        public GraphicLayer CurrentLayer
        {
            get
            {
                GraphicLayer[] graphicLayers = __Layers;
                if (graphicLayers == null) return null;
                int count = graphicLayers.Length;
                if (count == 0) return null;
                for (int index = count - 1; index >= 0; index--)
                {
                    if (index == 0 || (graphicLayers[index].IsActive && graphicLayers[index].ContainData)) return graphicLayers[index];
                }
                return graphicLayers[0];   // Jen pro compiler. Reálně sem kód nikdy nedojde (poslední smyčka "for ..." se provádí pro (index == 0).
            }
        }
        /// <summary>
        /// Kopíruje obsah vrstvy (sourceLayer) do vrstvy (targetLayer).
        /// Pokud některá vrstva (sourceLayer nebo targetLayer) je null, nekopíruje se nic.
        /// Pokud zdrojová vrstva má index rovný nebo vyšší jako cílová vrstva, nekopíruje se nic (kopíroval by se obraz z vyšší vrstvy do nižší, to je logický nesmysl).
        /// </summary>
        /// <param name="sourceLayer">Vrstva obsahující zdrojový obraz</param>
        /// <param name="targetLayer">Vrstva obsahující cílový obraz</param>
        public static void CopyContentOfLayer(GraphicLayer sourceLayer, GraphicLayer targetLayer)
        {
            if (sourceLayer != null && targetLayer != null && sourceLayer.Index < targetLayer.Index)
            {
                sourceLayer.RenderTo(targetLayer.Graphics);
                targetLayer.ContainData = false;           // Target vrstva sice obsahuje platná data ve smyslu že jsou zobrazitelná, ale v tuto chvíli jsou shodná se zdrojem a nemají nic navíc.
            }
        }
        #endregion
        #region class GraphicLayer : třída představující jednu grafickou vrstvu
        /// <summary>
        /// GraphicLayer : třída představující jednu grafickou vrstvu
        /// </summary>
        public class GraphicLayer : IDisposable
        {
            #region Konstruktor a privátní proměnné
            /// <summary>
            /// Konstruktor jedné vrstvy
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="size"></param>
            /// <param name="index">Index této jedné vrstvy</param>
            public GraphicLayer(LayeredGraphicBase owner, Size size, int index)
            {
                this.__Owner = owner;
                this.__Size = size;
                this.__Index = index;
                this.__ContainData = false;
                this.__IsActive = true;
                this._CreateLayer();
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Layer: #" + this.__Index.ToString() + "; ContainData: " + (this.__ContainData ? "Yes" : "No");
            }
            /// <summary>Vlastník = kompletní buffer</summary>
            private LayeredGraphicBase __Owner;
            /// <summary>Velikost vrstvy v pixelech</summary>
            private Size __Size;
            /// <summary>Index v poli vrstev</summary>
            private int __Index;
            /// <summary>Obsahuje nějaká data?</summary>
            private bool __ContainData;
            /// <summary>Vrstva je aktivní?</summary>
            private bool __IsActive;
            /// <summary>Řídící mechanismus pro bufferovanou grafiku</summary>
            private BufferedGraphicsContext __GraphicsContext;
            /// <summary>Obsah bufferované grafiky</summary>
            private BufferedGraphics __GraphicsData;
            /// <summary>
            /// Obsahuje pole platných vrstev pro aktuální velikost Controlu a aktuální počet vrstev, jehož jednou z vrstev jsme my (na našem indexu <see cref="__Index"/>).
            /// Jde tedy o naše rodné bratry/sestry. Pokud v mezidobí došlo k invalidaci, a naše rodina vrstev je již zrušena, pak je zde null.
            /// </summary>
            private GraphicLayer[] _Layers { get { return ((ILayeredGraphicBaseWorking)__Owner).GetLayers(this); } }
            /// <summary>
            /// Barva pozadí Controlu vlastníka
            /// </summary>
            private Color OwnerBackColor { get { return __Owner?.Owner?.BackColor ?? Control.DefaultBackColor; } }
            #endregion
            #region Privátní tvorba grafiky, IDisposable
            /// <summary>
            /// Vytvoří bufferovaný layer pro kreslení do this vrstvy
            /// </summary>
            private void _CreateLayer()
            {
                if (this.__GraphicsContext == null)
                    this.__GraphicsContext = BufferedGraphicsManager.Current;

                Size bufferSize = this.__Size;
                if (bufferSize.Width <= 0) bufferSize.Width = 1;
                if (bufferSize.Height <= 0) bufferSize.Height = 1;

                this.__GraphicsContext.MaximumBuffer = new Size(bufferSize.Width + 1, bufferSize.Height + 1);

                if (this.__GraphicsData != null)
                    TryRun(this.__GraphicsData.Dispose);

                this.__GraphicsData = this.__GraphicsContext.Allocate(this.__Owner.CreateGraphics(), new Rectangle(Point.Empty, bufferSize));
                this.__ContainData = false;
            }
            /// <summary>
            /// Dispose celého objektu
            /// </summary>
            void IDisposable.Dispose()
            {
                if (this.__GraphicsContext != null)
                    TryRun(this.__GraphicsContext.Dispose);
                this.__GraphicsContext = null;

                if (this.__GraphicsData != null)
                    TryRun(this.__GraphicsData.Dispose);
                this.__GraphicsData = null;

                this.__Owner = null;
            }
            #endregion
            #region Public property a metody
            /// <summary>
            /// true pokud je objekt platný
            /// </summary>
            public bool IsValid { get { return (this.__Size.Width > 0 && this.__Size.Height > 0 && this.__GraphicsContext != null && this.__GraphicsData != null); } }
            /// <summary>
            /// Index této vrstvy
            /// </summary>
            public int Index { get { return this.__Index; } }
            /// <summary>
            /// Aplikační příznak: tato vrstva obsahuje platná data?
            /// Výchozí je false.
            /// <para/>
            /// Aplikace nastavuje na true v situaci, kdy si aplikační kód vyzvedl zdejší grafiku (bude do ní kreslit).
            /// Nastavení true do této property projde všechny vyšší vrstvy (=vrstvy nad touto vrstvou) a do jejich <see cref="ContainData"/> vloží false.
            /// Tím se zajistí, že vyšší vrstvy budou muset být překresleny (pokud mají být použity) anebo nebudou brány jako platné (pokud se do nich kreslit nebude).
            /// <para/>
            /// Nastavit na false z aplikační logiky je možné, tím bude obsah této vrstvy skryt. 
            /// Nastavení false rovněž nastaví false do všech vyšších vrstev, protože v nich je nejspíš zanesen obraz z této vrstvy.
            /// </summary>
            public bool ContainData { get { return this.__ContainData; } set { this.__ContainData = value; this._InvalidateUpLayers(); } }
            /// <summary>
            /// Aplikační příznak: tato vrstva je aktvní?
            /// Výchozí je true.
            /// <para/>
            /// Aplikace může dočasně některé vrstvy deaktivovat (nastaví <see cref="IsActive"/> = false), když do nich nemá co vykreslit: typicky Overlay nebo ToolTip.
            /// Pak algoritmus kreslení tyto vrstvy zcela přeskakuje a ušetří čas (kopírování obsahu nižší vrstvy do vyšší).<br/>
            /// Setování jakékoli hodnoty do této property nastaví <see cref="ContainData"/> = false, ale jen do této vrstvy = nikoli do vrstev vyšších (jako to dělá setování do <see cref="ContainData"/>).
            /// </summary>
            public bool IsActive { get { return this.__IsActive; } set { this.__IsActive = value; this.__ContainData = false; } }
            /// <summary>
            /// Invaliduje data ve všech vyšších vrstvách nad vrstvou this.
            /// Metoda projde všechny vrstvy nad touto vrstvou (<see cref="__Index"/> + 1, a vyšší), a do jejich <see cref="__ContainData"/> vloží false.
            /// </summary>
            private void _InvalidateUpLayers()
            {
                var layers = _Layers;
                if (layers is null) return;
                for (int l = this.__Index + 1; l < layers.Length; l++)
                    layers[l].__ContainData = false;       // false setujeme do '__ContainData'. Pokud bych setoval false do 'ContainData', rozjel bych rekurzi.
            }
            /// <summary>
            /// Rozměr vrstvy.
            /// </summary>
            public Size Size { get { return this.__Size; } }
            /// <summary>
            /// Objekt Graphics, který dovoluje kreslit motivy do této vrstvy
            /// </summary>
            public Graphics Graphics { get { return this.__GraphicsData.Graphics; } }
            /// <summary>
            /// Zde je k dispozici nejbližší nižší vrstva, která obsahuje data.
            /// Tedy toto je vrstva, která obsahuje podkladový obraz pro naši vrstvu.
            /// Může zde být null.
            /// </summary>
            protected GraphicLayer SubLayerWithData
            {
                get
                {
                    int index = __Index;
                    if (index > 0)
                    {   // Pokud já jsem vyší vrstva než [0], pak mohu mít SubLayer:
                        var layers = _Layers;
                        if (layers != null)
                        {   // Pokud máme seznam našich platných vrstev:
                            for (int i = index - 1; i >= 0; i--)
                            {   // Prohledám vrstvy počínaje od nejbližší nižší, směrem dolů, a vrátím první aktivní vrstvu s daty:
                                if (layers[i].IsActive && layers[i].ContainData) return layers[i];
                            }
                        }
                    }
                    return null;
                }
            }
            /// <summary>
            /// Metoda vloží do this vrstvy obsah z grafiky nejbližší nižší vrstvy, která obshauje data (<jejíž <see cref="ContainData"/> je true).
            /// Volá se před zahájením kreslení z aplikačního kódu do this vrstvy.
            /// <para/>
            /// Tato metoda vepíše do this vrstvy <see cref="ContainData"/> = false (a tím i do vyšších vrstev) - pokud do ní byla vložena data z podkladové vrstvy.
            /// Aplikační kód, když provede kreslení do this vrstvy, má do ní vepsat <see cref="ContainData"/> = true.
            /// <para/>
            /// Pokud nebyla nalezena podkladová vrstva, a je povoleno <paramref name="clearWhenVoid"/>, vrstva je tedy naplněna barvou pozadí,
            /// pak do ní bude vepsáno <see cref="ContainData"/> = true : vrstva sama o sobě obsahuje platná data (barvu pozadí).
            /// </summary>
            /// <param name="clearWhenVoid">Pokud tato vrstva nemá žádnou podkladovou vrstvu, má být do ní vepsána barva pozadí controlu?</param>
            public void PrepareFromSubLayer(bool clearWhenVoid = false)
            {
                var subLayerWithData = this.SubLayerWithData;
                if (subLayerWithData != null)
                    CopyContentOfLayer(subLayerWithData, this);
                else if (clearWhenVoid)
                    this.ClearLayer(this.OwnerBackColor);
            }
            /// <summary>
            /// Do this grafiky nalije dodanou barvu.
            /// </summary>
            /// <param name="color"></param>
            public void ClearLayer(Color color)
            {
                this.__GraphicsData.Graphics.Clear(color);
                this.ContainData = true;                   // Prázdná vrstva obsahuje data = podkladovou barvu.
            }
            /// <summary>
            /// Vykreslí svůj obsah do dané cílové Graphics.
            /// Provádí se jednak při přenášení obsahu grafiky z dolních vrstev (nižší <see cref="Index"/>) do vyšších vrstev,
            /// a také při vykreslení finálního obsahu nejvyšší vrstvy do nativní Graphics Controlu.
            /// </summary>
            /// <param name="targetGraphics"></param>
            public void RenderTo(Graphics targetGraphics)
            {
                targetGraphics.CompositingMode = CompositingMode.SourceOver;
                this.__GraphicsData.Graphics.CompositingMode = CompositingMode.SourceOver;
                this.__GraphicsData.Render(targetGraphics);
            }
            #endregion
        }
        #endregion
    }
    /// <summary>
    /// Argument pro kreslení do vrstvy
    /// </summary>
    public class LayeredPaintEventArgs : EventArgs, IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="clipRect"></param>
        public LayeredPaintEventArgs(PaintEventArgs args)
        {
            __Graphics = args.Graphics;
            __ClipRectangle = args.ClipRectangle;
            __IsGraphicsUsed = false;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="clipRect"></param>
        public LayeredPaintEventArgs(Graphics graphics, Rectangle clipRectangle)
        {
            __Graphics = graphics;
            __ClipRectangle = clipRectangle;
            __IsGraphicsUsed = false;
        }
        void IDisposable.Dispose()
        {
            __Graphics = null;
        }
        private Graphics __Graphics;
        private Rectangle __ClipRectangle;
        /// <summary>
        /// Gets the graphics used to paint.
        /// </summary>
        public Graphics Graphics { get { __IsGraphicsUsed = true;  return this.__Graphics; } }
        /// <summary>
        /// Gets the graphics used to paint.
        /// </summary>
        public Rectangle ClipRectangle { get { __IsGraphicsUsed = true; return this.__ClipRectangle; } }
        /// <summary>
        /// Byla použita grafika <see cref="Graphics"/>?
        /// </summary>
        public bool IsGraphicsUsed { get { return __IsGraphicsUsed; } } private bool __IsGraphicsUsed;
    }
    /// <summary>
    /// Interface pro interní přístup do <see cref="LayeredGraphicBase"/>
    /// </summary>
    internal interface ILayeredGraphicBaseWorking
    {
        /// <summary>
        /// Vrátí pole aktuálně přítomných vrstev, pokud mezi nimi je daná vrstva.
        /// Většinou vrátí běžné pole vrstev, ale pokud je v dané chvíli nevalidní, pak vrátí null a negeneruje nové validní pole.
        /// Stejně tak pokud v mezidobí (od vytvořeníá vrstvy do volání této metody) došlo ke změně pole, a dodaná vrstva už mezi novými vrstvami není, pak vrací null.
        /// </summary>
        LayeredGraphicBase.GraphicLayer[] GetLayers(LayeredGraphicBase.GraphicLayer layer);
    }
}
