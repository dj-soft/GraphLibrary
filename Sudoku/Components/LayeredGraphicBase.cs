using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DjSoft.Games.Sudoku.Components
{
    public class LayeredGraphicStandard : LayeredGraphicBase
    {
        /// <summary>
        /// Konstruktor
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
    public class LayeredGraphicBase
    {
        #region Konstruktor, Owner, Events
        /// <summary>
        /// Konstruktor
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
        /// Obsahuje (vyhledá a vrátí) položku <see cref="GraphicLayer"/> ze zdejších vrstev, která má (<see cref="GraphicLayer.ContainData"/> == true),
        /// a je na nejvyšším indexu.
        /// Pokud dodané pole je null nebo neobsahuje žádnou položku, vrací se null.
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
                    if (index == 0 || graphicLayers[index].ContainData) return graphicLayers[index];
                }
                return graphicLayers[0];   // Jen pro compiler. Reálně sem kód nikdy nedojde (poslední smyčka "for ..." se provádí pro (index == 0).
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
            /// <summary>Řídící mechanismus pro bufferovanou grafiku</summary>
            private BufferedGraphicsContext __GraphicsContext;
            /// <summary>Obsah bufferované grafiky</summary>
            private BufferedGraphics __GraphicsData;
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
            /// Aplikace nastavuje na true v situaci, kdy si aplikační kód vyzvedl zdejší grafiku (bude do ní kreslit).
            /// Aplikace nastavuje na false v situaci, kdy tato vrstva je na vyšším indexu, než je vrstva aktuálně vykreslovaná.
            /// </summary>
            public bool ContainData { get { return this.__ContainData; } set { this.__ContainData = value; } }
            /// <summary>
            /// Rozměr vrstvy.
            /// </summary>
            public Size Size { get { return this.__Size; } }
            /// <summary>
            /// Objekt Graphics, který dovoluje kreslit motivy do této vrstvy
            /// </summary>
            public Graphics LayerGraphics { get { return this.__GraphicsData.Graphics; } }
            /// <summary>
            /// Kopíruje obsah vrstvy (sourceLayer) to vrstvy (targetLayer).
            /// Pokud některá vrstva (sourceLayer nebo targetLayer) je null, nekopíruje se nic.
            /// Pokud zdrojová vrstva má index rovný nebo vyšší jako cílová vrstva, nekopíruje se nic.
            /// </summary>
            /// <param name="sourceLayer">Vrstva obsahující zdrojový obraz</param>
            /// <param name="targetLayer">Vrstva obsahující cílový obraz</param>
            public static void CopyContentOfLayer(GraphicLayer sourceLayer, GraphicLayer targetLayer)
            {
                if (sourceLayer != null && targetLayer != null && sourceLayer.__Index < targetLayer.__Index)
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
                this.__GraphicsData.Graphics.CompositingMode = CompositingMode.SourceOver;
                this.__GraphicsData.Render(targetGraphics);
            }
            #endregion
        }
        #endregion
    }
}
