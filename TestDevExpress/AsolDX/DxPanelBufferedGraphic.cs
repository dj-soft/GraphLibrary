// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using DevExpress.Utils.Drawing;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel, obsahující podporu pro bufferované kreslení grafiky ve vrstvách.
    /// Po vytvoření je vhodné definovat soupis potřebných grafických vrstev do <see cref="Layers"/> a navázat handler události <see cref="PaintLayer"/>.
    /// za provozu je možno volat invalidaci vrstev pomocí metod <see cref="InvalidateLayers(object)"/>.
    /// </summary>
    public class DxPanelBufferedGraphic : DxPanelControl
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor. Implementuje pouze vrstvu nativního pozadí.
        /// </summary>
        public DxPanelBufferedGraphic()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResizeRedraw = true;
            InitLayers();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeLayers();
        }
        #endregion
        #region Nativní události kreslení a nativní invalidace, a jejich napojení na vrstvy
        /// <summary>
        /// OnInvalidated
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            base.OnInvalidated(e);
            if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.OnInvalidated; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// OnPaintBackground
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;
            OnPaintStartTime = startTime;
            if (!_NativeBackgroundIsValid)
            {   // Pokud nemáme platná data ve vrstvě nativního pozadí, pak si necháme base třídou vykreslit Background do vhodného objektu grafiky:
                PaintEventArgs ee = _GetNativeBackgroundPaintArgs(e);     // Jiný argument = obsahuje grafiku z vrstvy _NativeBackgroundLayer. Do té grafiky se následně vykreslí Background.
                base.OnPaintBackground(ee);                               // Tady se vykreslí pouze barva pozadí, ale nikoli motiv skinu. Vykreslí se do argumentu 'ee', který obsahuje grafiku z vrstvy _NativeBackgroundLayer.
                if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.OnPaintBackground; Time: {DxComponent.LogTokenTimeMilisec}", startTime);
            }
        }
        /// <summary>
        /// OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!OnPaintStartTime.HasValue) OnPaintStartTime = DxComponent.LogTimeCurrent;

            try
            {
                IsPaintLayersInProgress = true;
                bool contentIsChanged = false;
                if (!_NativeBackgroundIsValid)
                {
                    var startTime = DxComponent.LogTimeCurrent;
                    PaintEventArgs ee = _GetNativeBackgroundPaintArgs(e);     // Jiný argument = obsahuje grafiku z vrstvy _NativeBackgroundLayer. Do té grafiky se následně vykreslí Background.
                    base.OnPaint(ee);                                         // Tady se do grafiky z vrstvy _NativeBackgroundLayer (která zatím obsahuje pouze barvu pozadí) vykreslí celý motiv pozadí (=obrázky skinu)
                    _NativeBackgroundIsValid = true;
                    contentIsChanged = true;
                    if (LogActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.Prepare(NativeBackground); Time: {DxComponent.LogTokenTimeMilisec}", startTime);
                }

                OnPaintLayers(e.Graphics, e.ClipRectangle, contentIsChanged);
            }
            catch (Exception exc)
            {
                DxComponent.LogAddLine($"DxPanelBufferedGraphic.OnPaint() error: {exc.Message}");
            }
            finally
            {
                IsPaintLayersInProgress = false;
            }
            if (LogActive && OnPaintStartTime.HasValue) DxComponent.LogAddLineTime($"DxBufferedGraphic.Paint(); TotalTime: {DxComponent.LogTokenTimeMilisec}", OnPaintStartTime.Value);
            OnPaintStartTime = null;
        }
        /// <summary>
        /// Obsahuje true v okamžiku, kdy probíhá proces vykreslování.
        /// </summary>
        public bool IsPaintLayersInProgress { get; private set; }
        /// <summary>
        /// Čas, kdy byl zahájen proces Paint, kvůli Logu
        /// </summary>
        protected long? OnPaintStartTime;
        /// <summary>
        /// OnDpiChangedAfterParent : po změně DPI = přesunutí na monitor s jiným DPI
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            this.InvalidateLayers();
        }
        /// <summary>
        /// OnStyleChanged : po změně skinu
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            this.InvalidateLayers();
        }
        /// <summary>
        /// Obsahuje true, pokud máme platný obsah vrstvy <see cref="_NativeBackgroundLayer"/> (tedy je platná grafika, a je v ní uložen obsah). 
        /// Pokud je true, pak nepotřebujeme kreslit nativní Background v systémových metodách.
        /// Lze setovat hodnotu, uloží se do <see cref="GraphicLayer.HasContent"/>.
        /// </summary>
        private bool _NativeBackgroundIsValid 
        { 
            get { return (_NativeBackgroundLayer?.IsValidContent ?? false); }
            set
            {
                if (_NativeBackgroundLayer != null)
                    _NativeBackgroundLayer.HasContent = value;
            }
        }
        /// <summary>
        /// Vrací argument pro kreslení nativního Backgroundu v systémových metodách.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private PaintEventArgs _GetNativeBackgroundPaintArgs(PaintEventArgs e)
        {
            var nativeBackgroundLayer = _NativeBackgroundLayer;
            if (nativeBackgroundLayer == null) return e;
            return new PaintEventArgs(nativeBackgroundLayer.Graphics, e.ClipRectangle);
        }
        #endregion
        #region Vrstvy - tvorba a řízení kreslení
        /// <summary>
        /// Inicializuje vrstvy
        /// </summary>
        private void InitLayers()
        {
            _Layers = new List<GraphicLayer>();
            _LayersDict = new Dictionary<DxBufferedLayer, GraphicLayer>();
            AddLayer(DxBufferedLayer.NativeBackground);
        }
        /// <summary>
        /// Obsahuje používané vrstvy. 
        /// Lze setovat, setování připraví požadované vrstvy a způsobí invalidaci.
        /// Setovat lze pole jednotlivých vrstev, nelze ale vložit jako jednu hodnotu kombinaci více vrstev (enum <see cref="DxBufferedLayer"/> má charakteristiku [Flags]).
        /// Tímto principem lze vyžádat konkrétní pořadí vrstev.
        /// </summary>
        public DxBufferedLayer[] Layers
        {
            get { return _Layers.Select(l => l.LayerId).Where(l => l != DxBufferedLayer.NativeBackground).ToArray(); }
            set { CreateLayers(value); this.Invalidate(); }
        }
        /// <summary>
        /// Vygeneruje všechny dané vrstvy, vždy přidá vrstvu <see cref="DxBufferedLayer.NativeBackground"/>
        /// </summary>
        /// <param name="layersId"></param>
        private void CreateLayers(DxBufferedLayer[] layersId)
        {
            ClearLayers();
            AddLayer(DxBufferedLayer.NativeBackground);
            if (layersId == null) return;
            foreach (var layerId in layersId)
            {
                if (layerId == DxBufferedLayer.None || layerId == DxBufferedLayer.NativeBackground) continue;          // Tyto dva požadavky tiše ignorujeme
                if (!((int)layerId).HasOneBit()) throw new ArgumentException($"DxBufferedGraphic.Layers : cannot create one layer with MultiID '{layerId}'.");
                if (_LayersDict.ContainsKey(layerId)) throw new ArgumentException($"DxBufferedGraphic.Layers : cannot create duplicite layer '{layerId}'.");
                AddLayer(layerId);
            }
        }
        /// <summary>
        /// Přidá novou vrstvu daného typu
        /// </summary>
        /// <param name="layerId"></param>
        private void AddLayer(DxBufferedLayer layerId)
        {
            GraphicLayer graphicLayer = new GraphicLayer(this, layerId);
            _Layers.Add(graphicLayer);
            _LayersDict.Add(layerId, graphicLayer);
            if (layerId == DxBufferedLayer.NativeBackground)
                _NativeBackgroundLayer = graphicLayer;
        }
        /// <summary>
        /// Metoda je volaná v procesu vykreslení controlu (po případném vykreslení pozadí controlu do vrstvy <see cref="_NativeBackgroundLayer"/>).
        /// Úkolem této metody je zpracovat potřebné vrstvy: zajistit jejich platnou grafiku, zajistit vykreslení objektů do grafiky, a postupně složit výsledný obraz do dané grafiky.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="clipRectangle"></param>
        /// <param name="contentIsChanged"></param>
        private void OnPaintLayers(Graphics graphics, Rectangle clipRectangle, bool contentIsChanged)
        {
            int count = _Layers.Count;
            GraphicLayer sourceLayer = null;                         // Nižší vrstva, která obsahuje data. Slouží jako výchozí obsah pro grafiku navazující vyšší vrstvy.
            for (int l = 0; l < count; l++)
            {
                var layer = _Layers[l];
                layer.PaintLayer(ref sourceLayer, ref contentIsChanged, LogActive, clipRectangle);
            }
            if (sourceLayer != null)
                sourceLayer.RenderTo(graphics, LogActive);

            InvalidateUserData = null;
        }
        /// <summary>
        /// Invaliduje všechny vrstvy a zajistí nové vykreslení celého controlu.
        /// Pro každou vrstvu bude volán event <see cref="PaintLayer"/>.
        /// </summary>
        /// <param name="invalidateUserData">Objekt, který bude předáván do události <see cref="PaintLayer"/> v argumentu, pro všechny kreslené vrstvy</param>
        public void InvalidateLayers(object invalidateUserData)
        {
            _InvalidateLayers(DxBufferedLayer.All, invalidateUserData, true);
        }
        /// <summary>
        /// Invaliduje zadané vrstvy a zajistí nové vykreslení celého controlu.
        /// Pro každou zadanou vrstvu bude volán event <see cref="PaintLayer"/>.
        /// </summary>
        /// <param name="layers"></param>
        public void InvalidateLayers(params DxBufferedLayer[] layers)
        {
            DxBufferedLayer layersSum = DxBufferedLayer.None;
            if (layers == null || layers.Length == 0)
                layersSum = DxBufferedLayer.All;                     // Default = všechny
            else
                layers.ForEachExec(l => layersSum |= l);             // Sečtu bity (DxBufferedLayer je bitové pole)

            _InvalidateLayers(layersSum, null, true);
        }
        /// <summary>
        /// Invaliduje zadané vrstvy a zajistí nové vykreslení celého controlu.
        /// Pro každou zadanou vrstvu bude volán event <see cref="PaintLayer"/>.
        /// </summary>
        /// <param name="layers">Vrstvy k invalidaci, složené do jedné hodnoty (jde o Flags)</param>
        /// <param name="invalidateUserData">Objekt, který bude předáván do události <see cref="PaintLayer"/> v argumentu, pro všechny kreslené vrstvy</param>
        /// <param name="invalidateControl">Fyzicky vyžádat invalidaci celého controlu = následovat bude překreslení</param>
        public void InvalidateLayers(DxBufferedLayer layers, object invalidateUserData, bool invalidateControl)
        {
            _InvalidateLayers(layers, invalidateUserData, invalidateControl);
        }
        /// <summary>
        /// Invalidace vrstev, volaná v libovolném threadu
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="invalidateUserData">Objekt, který bude předáván do události <see cref="PaintLayer"/> v argumentu, pro všechny kreslené vrstvy</param>
        /// <param name="invalidateControl"></param>
        private void _InvalidateLayers(DxBufferedLayer layers, object invalidateUserData, bool invalidateControl)
        {
            this.RunInGui(() => _InvalidateLayersGui(layers, invalidateUserData, invalidateControl));
        }
        /// <summary>
        /// Invalidace vrstev, volaná v GUI threadu
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="invalidateUserData"></param>
        /// <param name="invalidateControl"></param>
        private void _InvalidateLayersGui(DxBufferedLayer layers, object invalidateUserData, bool invalidateControl)
        { 
            InvalidateUserData = invalidateUserData;

            // Invalidace existujících vrstev (tj. které reálně evidujeme), a které jsou v požadavku (layers):
            _Layers.ForEachExec(layer =>
            {
                if (((int)layer.LayerId & (int)layers) != 0)
                    layer.Invalidate();
            });

            if (invalidateControl) this.Invalidate();
        }
        /// <summary>
        /// UserData předaná do metod <see cref="InvalidateLayers(object)"/>, a přenášená při kreslení do eventhandleru <see cref="PaintLayer"/>.
        /// </summary>
        protected object InvalidateUserData;
        /// <summary>
        /// Velikost prostoru, do kterého se vykresluje grafika. Na tuto velikost musí být vytvořena grafika v jednotlivých vrstvách.
        /// </summary>
        protected Size GraphicsSize { get { return this.ClientSize; } }
        /// <summary>
        /// Událost, která je volána pro vykreslení dat každé potřebné vstvy
        /// </summary>
        public event DxBufferedGraphicPaintHandler PaintLayer;
        /// <summary>
        /// Prostor pro potomka, pokud by chtěl vykreslit obsah dané vrstvy
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPaintLayer(DxBufferedGraphicPaintArgs args) { }
        /// <summary>
        /// Metoda vyvolá háček <see cref="OnPaintLayer(DxBufferedGraphicPaintArgs)"/> a event <see cref="PaintLayer"/>.
        /// </summary>
        /// <param name="args"></param>
        protected void RunOnPaintLayer(DxBufferedGraphicPaintArgs args)
        {
            OnPaintLayer(args);
            PaintLayer?.Invoke(this, args);
        }
        /// <summary>
        /// Odebere všechny vrstvy
        /// </summary>
        private void ClearLayers()
        {
            foreach (var layer in _Layers)
                layer.Dispose();
            _Layers.Clear();
            _LayersDict.Clear();
            _NativeBackgroundLayer = null;
        }
        /// <summary>
        /// Disposuje data vrstev
        /// </summary>
        private void DisposeLayers()
        {
            ClearLayers();
        }
        /// <summary>
        /// Zkopíruje obsah bufferované grafiky z <paramref name="sourceGraphicsData"/> do cílové grafiky <paramref name="targetGraphics"/>.
        /// </summary>
        /// <param name="sourceGraphicsData"></param>
        /// <param name="targetGraphics"></param>
        /// <param name="logActive"></param>
        /// <param name="logInfo"></param>
        internal static void CopyGraphicsData(BufferedGraphics sourceGraphicsData, Graphics targetGraphics, bool logActive = false, string logInfo = null)
        {
            if (sourceGraphicsData == null || targetGraphics == null) return;

            var startTime = DxComponent.LogTimeCurrent;

            targetGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            sourceGraphicsData.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            sourceGraphicsData.Render(targetGraphics);

            if (logActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.CopyGraphicsData({(logInfo ?? "")}); Time: {DxComponent.LogTokenTimeMilisec}", startTime);
        }
        /// <summary>
        /// Vizuální grafický kontext pro aktuální aplikační doménu.
        /// Používá se jako zdroj grafiky pro vrstvy.
        /// <para/>
        /// Gets the System.Drawing.BufferedGraphicsContext for the current application domain.
        /// </summary>
        protected BufferedGraphicsContext GraphicsContext { get { if (__GraphicsContext == null) __GraphicsContext = BufferedGraphicsManager.Current; return __GraphicsContext; } }
        private BufferedGraphicsContext __GraphicsContext;
        /// <summary>
        /// Vrstva nativního pozadí. Existuje vždy kromě dtavu po Dispose.
        /// </summary>
        private GraphicLayer _NativeBackgroundLayer;
        /// <summary>
        /// Úplný seznam všech vrstev, včetně nativního pozadí, v patřičném pořadí = počínaje <see cref="DxBufferedLayer.NativeBackground"/>, následují vrstvy zadané uživatelem do <see cref="Layers"/>.
        /// </summary>
        private List<GraphicLayer> _Layers;
        /// <summary>
        /// Index grafických vrstev
        /// </summary>
        private Dictionary<DxBufferedLayer, GraphicLayer> _LayersDict;
        #endregion
        #region class GraphicLayer
        private class GraphicLayer : IDisposable
        {
            #region Konstruktor a Dispose
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="layerId"></param>
            public GraphicLayer(DxPanelBufferedGraphic owner, DxBufferedLayer layerId)
            {
                _Owner = owner;
                _LayerId = layerId;
                _GraphicsData = null;
                _GraphicsSize = Size.Empty;
                _HasContent = false;
                _IsInvalidated = true;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Layer: {LayerId}; Size: W={_GraphicsSize.Width}, H={_GraphicsSize.Height}";
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                DisposeGraphicsData();
                _Owner = null;
            }
            /// <summary>
            /// Vlastník
            /// </summary>
            private DxPanelBufferedGraphic _Owner;
            /// <summary>
            /// ID vrstvy
            /// </summary>
            public DxBufferedLayer LayerId { get { return _LayerId; } } private DxBufferedLayer _LayerId;
            #endregion
            #region Kreslení
            /// <summary>
            /// Zajistí vykreslení this vrstvy.
            /// </summary>
            /// <param name="sourceLayer"></param>
            /// <param name="contentIsChanged"></param>
            /// <param name="logActive"></param>
            /// <param name="clipRectangle"></param>
            public void PaintLayer(ref GraphicLayer sourceLayer, ref bool contentIsChanged, bool logActive = false, Rectangle? clipRectangle = null)
            {
                if (this.LayerId != DxBufferedLayer.NativeBackground && (contentIsChanged || !this.IsGraphicsValid || this.IsInvalidated))
                {   // Vrstva NativeBackground se nekreslí touto metodou, ale je do ní kresleno v nativních metodách Ownera.
                    // Ostatní vrstvy: Tato vrstva neobsahuje validní data - budeme ji vykreslovat zde!

                    var startTime = DxComponent.LogTimeCurrent;
                    var size = this._GraphicsSize;
                    if (!clipRectangle.HasValue) clipRectangle = new Rectangle(Point.Empty, size);
                    DxBufferedGraphicPaintArgs args = new DxBufferedGraphicPaintArgs(this.Graphics, this.GraphicsCache, this.LayerId, sourceLayer._GraphicsData, size, clipRectangle.Value, this.InvalidateUserData, this.LayerUserData, logActive);
                    // Info: třída DxBufferedGraphicPaintArgs v sobě obsahuje referenci na zdejší grafiku (this.Graphics) a na zdrojovou grafiku (sourceLayer._GraphicsData).
                    // Při prvním použití zdejší grafiky (DxBufferedGraphicPaintArgs.Graphics) si do ní zkopíruje obsah zdrojové grafiky (sourceLayer._GraphicsData)
                    //  a nahodí příznak GraphicsIsUsed = true.
                    // Pokud ale v události RunOnPaintLayer() nikdo nepoužije grafiku DxBufferedGraphicPaintArgs.Graphics, pak tato vrstva nebude mít svůj obsah, a jako sourceLayer zůstane ta předchozí...
                    _Owner.RunOnPaintLayer(args);
                    this.LayerUserData = args.LayerUserData;
                    if (logActive) DxComponent.LogAddLineTime($"DxBufferedGraphic.PaintLayer({LayerId}); Time: {DxComponent.LogTokenTimeMilisec}", startTime);

                    this._IsInvalidated = false;
                    bool hasContent = args.GraphicsIsUsed;
                    this._HasContent = hasContent;
                    if (hasContent)
                        contentIsChanged = true;
                }
                if (this.HasContent)
                    sourceLayer = this;
            }
            /// <summary>
            /// Svůj obsah přenese do dané cílové grafiky
            /// </summary>
            /// <param name="targetGraphics"></param>
            /// <param name="logActive"></param>
            public void RenderTo(Graphics targetGraphics, bool logActive = false)
            {
                if (targetGraphics == null) return;
                DxPanelBufferedGraphic.CopyGraphicsData(this.GraphicsData, targetGraphics, logActive, this.LayerId.ToString());
            }
            #endregion
            /// <summary>
            /// Obsahuje true, pokud tato vrstva má vytvořenou platnou grafiku, a pokud má uložen obsah dle <see cref="HasContent"/>.
            /// </summary>
            public bool IsValidContent { get { return (IsGraphicsValid && HasContent && !IsInvalidated); } }
            /// <summary>
            /// Obsahuje true, pokud tato vrstva má vygenerovaný obsah.
            /// Hodnotu lze setovat pouze pro vrstvu <see cref="DxBufferedLayer.NativeBackground"/>.
            /// </summary>
            public bool HasContent
            {
                get { return _HasContent; }
                set
                {
                    if (LayerId == DxBufferedLayer.NativeBackground)
                    {
                        _HasContent = value;
                        _IsInvalidated = false;
                    }
                    else
                        throw new InvalidOperationException($"Nelze nastavit hodnotu 'HasContent' pro vrstvu typu '{LayerId}'.");
                }
            }
            private bool _HasContent;
            /// <summary>
            /// Obsahuje true u vrstvy, která není validní a v nejbližším Paintu bude znovu vykreslována.
            /// </summary>
            public bool IsInvalidated { get { return _IsInvalidated; } } private bool _IsInvalidated;
            /// <summary>
            /// Invaliduje obsah této vrstvy. V procesu kreslení bude kreslen nový.
            /// </summary>
            public void Invalidate()
            {
                _IsInvalidated = true;
                _HasContent = false;
            }
            #region Správa grafiky - kontrola platnosti a tvorba objektu bufferované grafiky (včetně Dispose)
            /// <summary>
            /// Objekt grafiky. Nedávejme Dispose!!! Jinak jsme v tahu :-( ...
            /// Tento objekt je vždy platný - pokud by došlo ke změně rozměrů, bude vygenerován nový.
            /// </summary>
            public Graphics Graphics { get { return GraphicsData.Graphics; } }
            /// <summary>
            /// Cache grafiky pro DevExpress
            /// </summary>
            public DevExpress.Utils.Drawing.GraphicsCache GraphicsCache { get { this._GraphicsDataCheckValidity(); return _GraphicsCache; } }
            /// <summary>
            /// Bufferovaná grafika, vždy platný objekt (v případě potřeby vygeneruje nový platný objekt).
            /// </summary>
            protected BufferedGraphics GraphicsData
            {
                get
                {
                    this._GraphicsDataCheckValidity();
                    return _GraphicsData;
                }
            }
            /// <summary>
            /// Ověří platnost dat Graphics, případně [staré Disposuje a] vytvoří nové platné objekty
            /// </summary>
            protected void _GraphicsDataCheckValidity()
            {
                if (IsGraphicsValid) return;

                var graphicsContext = OwnerGraphicsContext;
                Size graphicsSize = OwnerGraphicsSize;
                int w = (graphicsSize.Width <= 0 ? 1 : graphicsSize.Width);
                int h = (graphicsSize.Height <= 0 ? 1 : graphicsSize.Height);

                graphicsContext.MaximumBuffer = new Size(w + 1, h + 1);

                DisposeGraphicsData();

                _GraphicsData = graphicsContext.Allocate(this._Owner.CreateGraphics(), new Rectangle(0, 0, w, h));
                _GraphicsSize = graphicsSize;
                _GraphicsCache = new DevExpress.Utils.Drawing.GraphicsCache(_GraphicsData.Graphics);
            }
            /// <summary>
            /// V případě potřeby provede Dispose bufferované grafiky <see cref="_GraphicsData"/>
            /// </summary>
            protected void DisposeGraphicsData()
            {
                if (_GraphicsCache != null)
                {
                    DxComponent.TryRun(() => _GraphicsCache.Dispose(), true);
                    _GraphicsCache = null;
                }
                if (_GraphicsData != null)
                {
                    DxComponent.TryRun(() => _GraphicsData.Dispose(), true);
                    _GraphicsData = null;
                }
                _HasContent = false;
            }
            /// <summary>
            /// Vizuální grafický kontext pro aktuální aplikační doménu.
            /// Používá se jako zdroj grafiky pro vrstvy.
            /// </summary>
            protected BufferedGraphicsContext OwnerGraphicsContext { get { return _Owner.GraphicsContext; } }
            /// <summary>
            /// Velikost prostoru v Parent controlu, do kterého se vykresluje grafika. Na tuto velikost musí být vytvořena grafika v jednotlivých vrstvách.
            /// Pokud se změní velikost Parenta, projeví se to zde, a bude detekován rozdíl mezi <see cref="OwnerGraphicsSize"/> a <see cref="_GraphicsSize"/>, 
            /// proto bude <see cref="IsGraphicsValid"/> == false, a objekt <see cref="GraphicsData"/> bude vygenerován nový.
            /// </summary>
            protected Size OwnerGraphicsSize { get { return _Owner.GraphicsSize; } }
            /// <summary>
            /// UserData předaná do metod <see cref="DxPanelBufferedGraphic.InvalidateLayers(object)"/>, a přenášená při kreslení do eventhandleru <see cref="DxPanelBufferedGraphic.PaintLayer"/>.
            /// </summary>
            protected object InvalidateUserData { get { return _Owner.InvalidateUserData; } }
            /// <summary>
            /// UserData konkrétní vrstvy, řídí si aplikační logika
            /// </summary>
            protected object LayerUserData { get { return _LayerUserData; } set { _LayerUserData = value; } }
            private object _LayerUserData;
            /// <summary>
            /// Obsahuje true, pokud aktuálně existuje Bufferovaná grafika (<see cref="_GraphicsData"/>) a má velikost odpovídající požadované velikosti panelu <see cref="OwnerGraphicsSize"/>.
            /// </summary>
            protected bool IsGraphicsValid { get { return _GraphicsData != null && _GraphicsSize == OwnerGraphicsSize; } }
            /// <summary>
            /// Bufferovaná grafika, fyzické úložiště.
            /// Zde může být null nebo neplatný objekt.
            /// Content of graphic buffer
            /// </summary>
            private BufferedGraphics _GraphicsData;
            /// <summary>
            /// Cache grafiky pro DevExpress
            /// </summary>
            private DevExpress.Utils.Drawing.GraphicsCache _GraphicsCache;
            /// <summary>
            /// Velikost, na kterou je dimenzovaná zdejší bufferovaná grafika <see cref="_GraphicsData"/>.
            /// </summary>
            private Size _GraphicsSize;
            #endregion
        }
        #endregion
    }
    #region enum DxBufferedLayer
    /// <summary>
    /// Grafické vrstvy
    /// </summary>
    /// <remarks>Tento enum lze podle potřeby rozšiřovat, nově deklarované vrstvy pak lze ve správném pořadí vložit do <see cref="DxPanelBufferedGraphic.Layers"/> a budou fungovat.</remarks>
    [Flags]
    public enum DxBufferedLayer : int
    {
        /// <summary>
        /// Neurčeno, taková vrstva se negeneruje
        /// </summary>
        None = 0,
        /// <summary>
        /// Nativní pozadí panelu, není nutno explicitně požadovat, tuto vrstvu řeší <see cref="DxPanelBufferedGraphic"/> interně.
        /// Tuto vrstvu nekreslí aplikační logika, ale nativní control.
        /// </summary>
        NativeBackground = 0x01,
        /// <summary>
        /// Aplikační pozadí, typicky určeno pro kreslení přímo na pozadí panelu, pod vrstvu <see cref="MainLayer"/>.
        /// Není vždy nutné definovat.
        /// </summary>
        AppBackground = 0x10,
        /// <summary>
        /// Typicky hlavní vrstva, kam se kreslí kontroly.
        /// </summary>
        MainLayer = 0x20,
        /// <summary>
        /// Overlay, kreslí se typicky nad controly (nad vrstvu <see cref="MainLayer"/>).
        /// Není vždy nutné definovat.
        /// </summary>
        Overlay = 0x40,

        /// <summary>
        /// Všechny vrstvy. Lze použít pro Invalidaci, ale ne jako požadavek na vytváření vrstev <see cref="DxPanelBufferedGraphic.Layers"/> - tam se musí specifikovat pole jednotlivých vrstev!
        /// </summary>
        All = NativeBackground | AppBackground | MainLayer | Overlay
    }
    #endregion
    #region delegate DxBufferedGraphicPaintHandler, class DxBufferedGraphicPaintArgs
    /// <summary>
    /// Předpis pro eventhandlery události Paint v třídě <see cref="DxPanelBufferedGraphic"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxBufferedGraphicPaintHandler(object sender, DxBufferedGraphicPaintArgs args);
    /// <summary>
    /// Data pro událost 
    /// </summary>
    public class DxBufferedGraphicPaintArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="graphicsCache"></param>
        /// <param name="layerId"></param>
        /// <param name="sourceGraphicsData"></param>
        /// <param name="size"></param>
        /// <param name="clientRectangle"></param>
        /// <param name="invalidateUserData"></param>
        /// <param name="layerUserData"></param>
        /// <param name="logActive"></param>
        public DxBufferedGraphicPaintArgs(Graphics graphics, DevExpress.Utils.Drawing.GraphicsCache graphicsCache, DxBufferedLayer layerId, BufferedGraphics sourceGraphicsData, Size size, Rectangle clientRectangle, object invalidateUserData, object layerUserData, bool logActive = false)
        {
            _LayerId = layerId;
            _Graphics = graphics;
            _GraphicsCache = graphicsCache;
            _SourceGraphicsData = sourceGraphicsData;
            _GraphicsIsUsed = false;
            _Size = size;
            _InvalidateUserData = invalidateUserData;
            _LayerUserData = layerUserData;
            _LogActive = logActive;
        }
        private DxBufferedLayer _LayerId;
        private Graphics _Graphics;
        private DevExpress.Utils.Drawing.GraphicsCache _GraphicsCache;
        private BufferedGraphics _SourceGraphicsData;
        private bool _GraphicsIsUsed;
        private Size _Size;
        private object _InvalidateUserData;
        private object _LayerUserData;
        private bool _LogActive;
        /// <summary>
        /// ID vrstvy
        /// </summary>
        public DxBufferedLayer LayerId { get { return _LayerId; } }
        /// <summary>
        /// Grafika. Jakmile bude použita (bude čten obsah této property), nastaví se <see cref="GraphicsIsUsed"/> na true, a vrstva bude označena jako "Obsahující data".
        /// </summary>
        public Graphics Graphics 
        {
            get 
            {
                _CheckGraphics();
                return _Graphics; 
            }
        }
        /// <summary>
        /// Cache grafiky pro DevExpress
        /// </summary>
        public DevExpress.Utils.Drawing.GraphicsCache GraphicsCache
        {
            get
            {
                _CheckGraphics();
                return _GraphicsCache;
            }
        }
        private void _CheckGraphics()
        {
            if (_GraphicsIsUsed) return;
            if (_SourceGraphicsData != null) DxPanelBufferedGraphic.CopyGraphicsData(_SourceGraphicsData, _Graphics, _LogActive, _LayerId.ToString());
            _GraphicsIsUsed = true;
        }
        /// <summary>
        /// Obsahuje false na začátku, nebo true po prvním použití instance <see cref="Graphics"/>.
        /// </summary>
        public bool GraphicsIsUsed { get { return _GraphicsIsUsed; } }
        /// <summary>
        /// Metoda sdělí controlu <see cref="DxPanelBufferedGraphic"/>, že aktuální vrstva má být použita, i když je prázdná.
        /// </summary>
        public void UseBlankGraphics()
        {
            _CheckGraphics();
        }
        /// <summary>
        /// Velikost prostoru
        /// </summary>
        public Size Size { get { return _Size; } }
        /// <summary>
        /// Uživatelská data, předaná do invalidace <see cref="DxPanelBufferedGraphic.InvalidateLayers(object)"/>
        /// </summary>
        public object InvalidateUserData { get { return _InvalidateUserData; } }
        /// <summary>
        /// Uživatelská data, která jsou uložena permanentně v rámci jedné konkrétní vrstvy.
        /// Aplikační logika je sem může v procesu kreslení vepsat, i číst. Vepsaná data v jednom procesu PaintLayer budou k dispozici i v příštím procesu.
        /// Data se ztratí pouze při změně vrstev - po zápisu do <see cref="DxPanelBufferedGraphic.Layers"/>.
        /// </summary>
        public object LayerUserData { get { return _LayerUserData; } set { _LayerUserData = value; } }
    }
    #endregion
}
