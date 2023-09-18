using System;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace DjSoft.Tools.ProgramLauncher.Components
{
    /// <summary>
    /// Potomek třídy <see cref="Control"/>, který v sobě implementuje doublebuffer pro grafiku.
    /// Potomci této třídy nemusí zajišťovat bufferování grafiky.
    /// Potomci této třídy implementují vykreslování svého obsahu tím, že přepíšou metodu OnPaintToBuffer(), a v této metodě zajisté své vykreslení.
    /// Pro spuštění překreslení svého obsahu volají Draw() (namísto Invalidate()).
    /// </summary>
    public class GraphicsControl : ToolTipControl, IVirtualContainer, IDisposable
    {
        #region Konstruktor a Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GraphicsControl()
        {
            this._InitGraphics();
        }
        void IDisposable.Dispose()
        {
            if (MainBuffGraphics != null)
            {
                MainBuffGraphics.Dispose();
                MainBuffGraphics = null;
            }
            if (MainBuffGraphContent != null)
            {
                MainBuffGraphContent.Dispose();
                MainBuffGraphContent = null;
            }
            if (BackupBuffGraphics != null)
            {
                BackupBuffGraphics.Dispose();
                BackupBuffGraphics = null;
            }
            if (BackupBuffGraphContent != null)
            {
                BackupBuffGraphContent.Dispose();
                BackupBuffGraphContent = null;
            }
        }
        #endregion
        #region Řízení práce s BufferedGraphic (obecně přenosný mechanismus i do jiných tříd) a Virtuální souřadnice + Scrollbary
        private void _InitGraphics()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserMouse | ControlStyles.UserPaint, true);

            this._InitVirtualDimensions();
            this._MainGraphBufferInit();
            this._BackupGraphBufferInit();
        }
        #region Privátní řídící mechanismus - Main buffer, Backup buffer
        #region Main grafika
        /// <summary>
        /// Obsah bufferované grafiky, pro rychlejší překreslování a udržení obrazu v paměti i mimo plochu Controlu
        /// </summary>
        protected BufferedGraphicsContext MainBuffGraphContent;
        /// <summary>
        /// Řídící objekt bufferované grafiky
        /// </summary>
        protected BufferedGraphics MainBuffGraphics;
        /// <summary>
        /// Tato metoda do objektu this nastaví parametry pro doublebuffer grafiky.
        /// Tuto metodu voláme z konstruktoru objektu.
        /// </summary>
        private void _MainGraphBufferInit()
        {
            // Retrieves the BufferedGraphicsContext for the current application domain.
            MainBuffGraphContent = BufferedGraphicsManager.Current;

            // Sets the maximum size for the primary graphics buffer
            // of the buffered graphics context for the application
            // domain.  Any allocation requests for a buffer larger 
            // than this will create a temporary buffered graphics 
            // context to host the graphics buffer.
            MainBuffGraphContent.MaximumBuffer = _MaximumBufferSize;

            // Allocates a graphics buffer the size of this form
            // using the pixel format of the Graphics created by 
            // the Form.CreateGraphics() method, which returns a 
            // Graphics object that matches the pixel format of the form.
            MainBuffGraphics = MainBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);

            this.Resize += new EventHandler(_ResizeGraphics);
            this.Paint += new PaintEventHandler(_PaintGraphics);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.OptimizedDoubleBuffer, true);

            // Draw the first frame to the buffer.
            // _Draw(Rectangle.Empty);       Nemůžeme, protože jsme v konstruktoru, 
            // a Draw() nám vyvolá virtuální metodu OnPaintToBuffer(), tedy akci na třídě potomka, 
            // čímž porušíme zásadu Nevolat z konstruktoru virtuální metody.
            // Důsledek = třída potomka může mít v konstuktoru zajištěnou inicializaci proměnných pro Draw(), 
            // ale ten se vyvolá z konstruktoru předka ještě před potřebnou inicializací.

        }
        /// <summary>
        /// Vrací MaximumBufferSize pro BufferedGraphicsContext (=this.Size + 1)
        /// </summary>
        private Size _MaximumBufferSize
        {
            get
            {
                int w = (this.Width < 1 ? 1 : this.Width);
                int h = (this.Height < 1 ? 1 : this.Height);
                return new Size(w + 1, h + 1);
            }
        }
        /// <summary>
        /// Vrací Rectangle pro alokaci prostoru metodou BufferedGraphics.Allocate()
        /// </summary>
        private Rectangle _CurrentGraphicsRectangle
        {
            get
            {
                int w = (this.Width < 1 ? 1 : this.Width);
                int h = (this.Height < 1 ? 1 : this.Height);
                return new Rectangle(0, 0, w, h);
            }
        }
        #endregion
        #region Záložní grafika
        /// <summary>
        /// Záloha dat grafiky.
        /// Umožní uložit obraz grafiky do této zálohy (metodou BackupGraphicStore()) 
        /// anebo obsah zálohy načíst do pracovní grafiky (metodou BackupGraphicLoad()).
        /// Připravenost grafiky před použitím metody BackupGraphicLoad() lze testovat čtením property BackupGraphicIsReady.
        /// </summary>
        private BufferedGraphicsContext BackupBuffGraphContent;
        /// <summary>
        /// Řídící objekt zálohy grafiky.
        /// Umožní uložit obraz grafiky do této zálohy (metodou BackupGraphicStore()) 
        /// anebo obsah zálohy načíst do pracovní grafiky (metodou BackupGraphicLoad()).
        /// Připravenost grafiky před použitím metody BackupGraphicLoad() lze testovat čtením property BackupGraphicIsReady.
        /// </summary>
        private BufferedGraphics BackupBuffGraphics;
        /// <summary>
        /// Dimenze záložní grafiky. Musí odpovídat aktuálním dimenzím, jinak grafiku nelze použít.
        /// </summary>
        private Size BackupBuffSize;
        /// <summary>
        /// Iniciace dat záložní grafiky
        /// </summary>
        private void _BackupGraphBufferInit()
        {
            this.BackupBuffGraphContent = BufferedGraphicsManager.Current;
            this.BackupBuffSize = Size.Empty;
        }
        /// <summary>
        /// Uloží současný stav z hlavního grafického bufferu (do něhož se kreslí v metodě OnPaintToBuffer přes e.Graphics)
        /// do záložního grafického bufferu.
        /// Účel: současnou podobu grafiky si zazálohujeme jako "podklad", protože její vytvoření nás stálo mnoho úsilí.
        /// Následně je možno tento "podklad" okamžitě natáhnout ze zálohy (metodou BackupGraphicLoad()), a "počmárat" ji něčím rychlým a dočasným,
        /// pak vykreslit, a příště ji znovu vytáhnout ze zálohy a počmárat ji něčím jiným, s tím že náročný podklad se nemusí znovu vykreslovat.
        /// </summary>
        protected void BackupGraphicStore()
        {
            // 1. Je nutno alokovat prostor pro záložní grafiku (rozdíl Size) ?
            Size currentSize = _MaximumBufferSize;
            if (this.BackupBuffSize != currentSize)
            {
                BackupBuffGraphContent.MaximumBuffer = currentSize; ;
                if (BackupBuffGraphics != null)
                {
                    BackupBuffGraphics.Dispose();
                    BackupBuffGraphics = null;
                }
                BackupBuffGraphics = BackupBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);
                this.BackupBuffSize = currentSize;
            }

            // 2. Zazálohovat stav hlavní grafiky:
            MainBuffGraphics.Render(BackupBuffGraphics.Graphics);
        }
        /// <summary>
        /// Načte zálohu grafiky ze záložního grafického bufferu do hlavního (v němž se kreslí v metodě OnPaintToBuffer přes e.Graphics).
        /// Pozor: před použitím je třeba ověřit, zda lze data načíst, ověřením že (BackupGraphicIsReady == true).
        /// Použití: po některém dřívějším plném renderování grafiky lze výsledek zazálohovat (metodou BackupGraphicStore()).
        /// Následně, když je třeba nad touto grafikou vykreslit např. letícího motýla, je vhodné tuto zálohu načíst touto metodou (BackupGraphicLoad(e.Graphics))
        /// - tím se vrátíme do stavu po plném vyrenderování, a pak stačí nakreslit motýla, a je to hned.
        /// </summary>
        /// <param name="target">Cílová grafika, kam se má záloha přenést. Typicky v metodě OnPaintToBuffer() je to parametr e.Graphics.</param>
        protected void BackupGraphicLoad(Graphics target)
        {
            if (!BackupGraphicIsReady)
                App.ShowError("Byl proveden pokus o použití záložní grafiky za stavu, kdy to není přípustné.");
            BackupBuffGraphics.Render(target);
        }
        /// <summary>
        /// Informace o tom, že (true) záložní grafika obsahuje použitelná data, a že je tedy přípustné použít metodu BackupGraphicLoad().
        /// Pokud obsahuje false, pak záložní grafická data nejsou použitelná, a metoda BackupGraphicLoad() vyvolá chybu.
        /// </summary>
        protected bool BackupGraphicIsReady
        {
            get
            {
                return (MainBuffGraphics != null && this.BackupBuffSize == _MaximumBufferSize);
            }
        }
        #endregion
        #region EVENTY, ŘÍZENÍ METOD NA POTOMKOVI
        /// <summary>
        /// Fyzický Paint.
        /// Probíhá kdykoliv, když potřebuje okno překreslit.
        /// Aplikační logiku k tomu nepotřebuje, obrázek pro vykreslení má připravený v bufferu. Jen jej přesune na obrazovku.
        /// Aplikační logika kreslí v případě Resize (viz event Dbl_Resize) a v případě, kdy ona sama chce (když si vyvolá metodu Draw()).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PaintGraphics(object sender, PaintEventArgs e)
        {
            // Okno chce vykreslit svoji grafiku - okamžitě je vylijeme do okna z našeho bufferu:
            MainBuffGraphics.Render(e.Graphics);
        }
        /// <summary>
        /// Handler události OnResize: zajistí přípravu nového bufferu, vyvolání kreslení do bufferu, a zobrazení dat z bufferu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ResizeGraphics(object sender, EventArgs e)
        {
            _AcceptControlSize();

            // Re-create the graphics buffer for a new window size.
            MainBuffGraphContent.MaximumBuffer = _MaximumBufferSize;
            if (MainBuffGraphics != null)
            {
                MainBuffGraphics.Dispose();
                MainBuffGraphics = null;
            }
            MainBuffGraphics = MainBuffGraphContent.Allocate(this.CreateGraphics(), _CurrentGraphicsRectangle);

            ResizeAfter();
        }
        /// <summary>
        /// Po změně Visible.
        /// Při změně na true zajišťuje CheckToolTipInitialized() a Draw()
        /// </summary>
        /// <param name="e"></param>
		protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                this.Draw();
            }
        }
        /// <summary>
        /// Tuto metodu mají přepisovat potomkové, kteří chtějí reagovat na změnu velikosti.
        /// Až si připraví objekty, mají zavolat base.ResizeAfter(), kde se zajistí vyvolání Draw() => event PaintToBuffer().
        /// </summary>
        protected virtual void ResizeAfter()
        {
            this._Draw(Rectangle.Empty);
        }
        /// <summary>
        /// Interní spouštěč metody pro kreslení dat
        /// </summary>
        /// <param name="drawRectangle">
        /// Informace pro kreslící program o rozsahu překreslování.
        /// Nemusí nutně jít o grafický prostor, toto je pouze informace předáváná z parametru metody Draw() do handleru PaintToBuffer().
        /// V servisní třídě se nikdy nepoužije ve významu grafického prostoru.
        /// </param>
        private void _Draw(Rectangle drawRectangle)
        {
            if (_SuppressDrawing) return;              // Potlačené kreslení.
            if (!EnabledDrawing) return;               // Nevhodná situace pro kreslení
            if (this.CurrentlyDrawing) return;         // Už kreslím, nemohu kreslit podruhé
            lock (this.CurrentlyDrawingLock)           // Zamknu si a znovu otestuji hodnotu this.CurrentlyDrawing:
            {
                if (!this.CurrentlyDrawing)
                {
                    this.CurrentlyDrawing = true;
                    if (this.Width > 0 && this.Height > 0 && this.Visible)
                    {
                        PaintEventArgs e = new PaintEventArgs(this.MainBuffGraphics.Graphics, drawRectangle);
                        this.OnPaintToBuffer(this, e);
                        if (this.PaintToBuffer != null) this.PaintToBuffer(this, e);          // Event
                    }
                    if (this.InvokeRequired)
                        this.BeginInvoke(new Action(this.Refresh));
                    else
                        this.Refresh();
                    this.CurrentlyDrawing = false;
                }
            }
        }
        /// <summary>
        /// true když je důvod abych se vykresloval
        /// </summary>
        protected bool EnabledDrawing
        {
            get
            {
                if (IsInDesignMode) return true;       // V design modu se vykreslovat budu
                if (FormExists) return true;           // Když mám form, tak se vykreslovat budu
                return false;                          // Jinak se do vykreslování pouštět nemusíme.
            }
        }
        /// <summary>
        /// Příznak, že právě nyní probíhá kreslení.
        /// Pokud probíhá, pak další požadavky na vykreslení (Invalidate(), Refresh(), Draw()) jsou ignorovány.
        /// </summary>
        protected bool CurrentlyDrawing { get; private set; }
        /// <summary>
        /// Zámek pro nerušené kreslení
        /// </summary>
        private object CurrentlyDrawingLock = new object();
        /// <summary>
        /// true, pokud již existuje Form
        /// </summary>
        protected bool FormExists
        {
            get
            {
                if (_FormExists) return true;
                Form form = this.FindForm();
                if (form == null) return false;
                _FormExists = true;
                return true;
            }
        }
        private bool _FormExists = false;
        /// <summary>
        /// true, pokud jsem já nebo můj parent v design modu. Pak sice nemám žádný Form, ale přesto bych se měl vykreslovat.
        /// </summary>
        internal bool IsInDesignMode
        {
            get
            {
                return true;
            }
        }
        #endregion
        #endregion
        #region Public property
        /// <summary>
        /// Pokud chceme využít bufferovaného vykreslování této třídy bez toho, abychom ji dědili (použijeme nativní třídu),
        /// pak je nutno vykreslování umístit do tohoto eventu.
        /// Pracuje se zde zcela stejně, jako v eventu Paint(), ale vizuální rozdíl je zcela zásadní:
        /// Zatímco Paint() kreslí přímo do controlu, naživo, a pokaždé znovu,
        /// pak tato metoda PaintToBuffer() kreslí do bufferu do paměti, a control si přebírá výsledek najednou, optimalizovaně.
        /// </summary>
        [Browsable(true)]
        [Category("Paint")]
        [Description("Zde musí být implementováno uživatelské vykreslování obsahu objektu do grafického bufferu. Pracuje se zde zcela stejně, jako v eventu Paint(), ale fyzicky se metoda volá jen v nutných případech.")]
        public event PaintEventHandler PaintToBuffer;
        /// <summary>
        /// Barva pozadí prvku. Je využita pokud není nastaveno (BackgroundIsTransparent == true)
        /// </summary>
        [Category("Appearance")]
        [Description("Barva pozadí prvku. Je využita pokud není nastaveno (BackgroundIsTransparent == true)")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; Draw(); }
        }
        /// <summary>
        /// Okraj controlu
        /// </summary>
        [Description("Styl okraje prvku")]
        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(BorderStyle.None)]
        public virtual BorderStyle BorderStyle
        {
            get { return _BorderStyle; }
            set { _BorderStyle = value; Draw(); }
        }
        private BorderStyle _BorderStyle = BorderStyle.None;
        /// <summary>
        /// Vykreslované okraje controlu (strany borderu).
        /// Umožní řešit navazování různých controlů do jednoho borderu.
        /// </summary>
        [Description("Vykreslované okraje controlu (strany borderu). Umožní řešit navazování různých controlů do jednoho borderu.")]
        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(Border3DSide.All)]
        public virtual Border3DSide BorderSides
        {
            get { return _BorderSides; }
            set { _BorderSides = value; Draw(); }
        }
        private Border3DSide _BorderSides = Border3DSide.All;
        /// <summary>
        /// Prostor pro kreslení uvnitř tohoto prvku, s vynecháním aktuálního Borderu
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle ClientArea { get { return this._GetClientArea(); } }
        /// <summary>
        /// Šířka Borderu na jednotlivých okrajích prvku
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Padding BorderWidth { get { return this._GetBorderWidth(); } }
        #endregion
        #region ScrollBars, Virtuální souřadnice
        /*   Algoritmy
         - Základem je naplnění ContentSize = velikost potřebného prostoru k zobrazení = suma velikosti dat + rezerva vpravo a dole
             Pokud není naplněno, pak control pracuje v jednoduchém režimu, a nic z dalšího se neřeší!
         - Proti tomu stojí reálná velikost controlu = ClientSize = do tohoto prostoru vykreslujeme obsah dat
         - Pokud ContentSize <= ClientSize, pak nepoužijeme ScrollBary a hodnota __CurrentWindowBegin = {0,0} = zobrazujeme nativně = bez posouvání obsahu
         - Pokud ContentSize >= ClientSize v jednom směru, pak zobrazíme patřičný Scrollbar, ten odebere část prostoru z ClientSize a přepočteme i druhý směr
         - 



        */

        /// <summary>
        /// Povoluje se práce se Scrollbary, pokud bude zadána velikost obsahu <see cref="ContentSize"/>. Default = true.
        /// </summary>
        public bool ScrollbarsEnabled { get { return __ScrollbarsEnabled; } set { __ScrollbarsEnabled = value; _RefreshContentArea(); Draw(); } }
        private bool __ScrollbarsEnabled;

        /// <summary>
        /// Souřadnice počátku viditelného prostoru = souřadnice bodu ve virtuálním prostoru (prostordatového obsahu), který je zobrazen na vizuálním pixelu 0/0
        /// </summary>
        public Point VirtualBegin
        {
            get
            {
                var vx = __DimensionX.VirtualBegin;
                var vy = __DimensionY.VirtualBegin;
                return new Point(vx, vy);
            }
            set
            {
                __DimensionX.VirtualBegin = value.X;
                __DimensionY.VirtualBegin = value.Y;
            }
        }

        public Point GetVirtualPoint(Point controlPoint)
        {
            if (!_IsInVirtualMode) return controlPoint;
            return controlPoint.GetShiftedPoint(__DimensionX.VirtualBegin, __DimensionY.VirtualBegin);
        }
        public Point GetControlPoint(Point virtualPoint)
        {
            if (!_IsInVirtualMode) return virtualPoint;
            return virtualPoint.GetShiftedPoint(-__DimensionX.VirtualBegin, -__DimensionY.VirtualBegin);
        }
        /// <summary>
        /// Inicializuje data pro Virtuální souřadnice
        /// </summary>
        private void _InitVirtualDimensions()
        {
            this.MouseWheel += _MouseWheel;

            __ScrollbarsEnabled = true;

            __DimensionX = new VirtualDimension(this, Axis.X);
            __DimensionX.VirtualBeginChanged += __Dimension_VirtualBeginChanged;

            __ScrollBarX = new ScrollBarX() { Visible = false };
            __ScrollBarX.ValueChanged += __DimensionX.ScrollBarValueChanged;
            this.Controls.Add(__ScrollBarX);

            __DimensionY = new VirtualDimension(this, Axis.Y);
            __DimensionY.VirtualBeginChanged += __Dimension_VirtualBeginChanged;

            __ScrollBarY = new ScrollBarY() { Visible = false };
            __ScrollBarY.ValueChanged += __DimensionY.ScrollBarValueChanged;
            this.Controls.Add(__ScrollBarY);
        }
        /// <summary>
        /// Nativní událost MouseWheel na controlu - přeneseme ji do Scrollbaru Y, pokud to lze
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            if (_IsInVirtualMode && __DimensionY.UseScrollbar)
                __ScrollBarY.DoMouseWheel(e);
        }
        /// <summary>
        /// Po změně virtuálního počátku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __Dimension_VirtualBeginChanged(object sender, EventArgs e)
        {
            this.Draw();
        }

        /// <summary>
        /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
        /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
        /// </summary>
        public int VerticalScrollBarWidth { get { return __DimensionY.ScrollbarSize; } }
        /// <summary>
        /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
        /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
        /// </summary>
        public int HorizontalScrollBarHeight { get { return __DimensionX.ScrollbarSize; } }

        /// <summary>
        /// Fyzický Scrollbar vodorovný pro posun na ose X
        /// </summary>
        private ScrollBarX __ScrollBarX;
        /// <summary>
        /// Fyzický Scrollbar svislý pro posun na ose Y
        /// </summary>
        private ScrollBarY __ScrollBarY;
        /// <summary>
        /// Virtuální souřadnice ve směru osy X (Width)
        /// </summary>
        private VirtualDimension __DimensionX;
        /// <summary>
        /// Virtuální souřadnice ve směru osy Y (Height)
        /// </summary>
        private VirtualDimension __DimensionY;
        /// <summary>
        /// Velikost virtuálního obsahu = na něj se dimenzují Scrollbary. Null = nemáme virtuální souřadnice.
        /// </summary>
        private Size? __ContentSize;
        /// <summary>
        /// Obsahuje true, pokud objekt obsahuje platnou hodnotu <see cref="ContentSize"/> a mohl by být ve virtuálním modu.
        /// </summary>
        private bool __HasContentSize;
        /// <summary>
        /// Obsahuje true, pokud objekt reprezentuje virtuální prostor = má nastavenou velikost obsahu <see cref="ContentSize"/> (kladné rozměry)
        /// a má povoleno používat Scrollbary <see cref="ScrollbarsEnabled"/>.
        /// </summary>
        private bool _IsInVirtualMode { get { return (__HasContentSize && __ScrollbarsEnabled); } }

        /// <summary>
        /// Potřebná velikost obsahu. 
        /// Výchozí je null = control zobrazuje to, co je vidět, a nikdy nepoužívá Scrollbary.
        /// Lze setovat hodnotu = velikost zobrazených dat, pak se aktivuje virtuální režim se zobrazením výřezu.
        /// Při změně hodnoty se nenuluje souřadnice počátku <see cref="CurrentWindow"/>, změna velikosti obsahu jej tedy nutně nemusí přesunout na počátek.
        /// </summary>
        public virtual Size? ContentSize { get { return __ContentSize; } set { _SetContentSize(value); } }

        public Rectangle CurrentWindow { get { return this.ClientArea; } set { } }
        private Point? __CurrentWindowBegin;
        private Size __CurrentWindowSize;
        private void _SetContentSize(Size? contentSize)
        {
            __HasContentSize = (contentSize.HasValue && contentSize.Value.Width > 0 && contentSize.Value.Height > 0);
            __ContentSize = (__HasContentSize ? contentSize : null);
            _RefreshContentArea();
        }
        private void _RefreshContentArea()
        {
            _DetectScrollbars();

            if (!_IsInVirtualMode) return;


        }
        private void _AcceptControlSize()
        {
            Size windowSize = this.ClientSize;

            _DetectScrollbars();
            _ShowScrollBars();

            __CurrentWindowSize = windowSize;
        }
        /// <summary>
        /// Detekuje potřebu zobrazení Scrollbarů. Volá se jak po změně <see cref="ContentSize"/>, tak po Resize controlu.
        /// </summary>
        private void _DetectScrollbars()
        {
            __DimensionX.UseScrollbar = false;
            __DimensionY.UseScrollbar = false;                                 // Z této hodnoty bude vycházet __DimensionX.NeedScrollbar

            if (_IsInVirtualMode)
            {
                __DimensionX.UseScrollbar = __DimensionX.NeedScrollbar;        // Osa X (Width) si určí jen svoji potřebu Scrollbaru, bez přítomnosti Scrollbaru Y
                __DimensionY.UseScrollbar = __DimensionY.NeedScrollbar;        // Osa Y (Height) si určí ken svoji potřebu Scrollbaru, už se zohledněním Scrollbaru X
                if (__DimensionY.UseScrollbar && !__DimensionX.UseScrollbar)   // Pokud osa Y má Scrollbar a osa X jej dosud nemá, pak se zohledněním existence Scrollbaru Y (=zmenšení prostoru) si jej nyní může taky chtít použít...
                    __DimensionX.UseScrollbar = __DimensionX.NeedScrollbar;    // Osa X (Width) si určí potřebu Scrollbaru X, se zohledněním přítomnosti Scrollbaru Y
            }
        }
        /// <summary>
        /// Zajistí zobrazení Scrollbarů podle stavu a podle potřeby
        /// </summary>
        private void _ShowScrollBars()
        {
            if (_IsInVirtualMode)
            {
                __DimensionX.ShowScrollBar();
                __DimensionY.ShowScrollBar();
            }
            else
            {
                __ScrollBarX.Visible = false;
                __ScrollBarY.Visible = false;
            }
        }
        /// <summary>
        /// Třída pro řešení virtuální / nativní souřadnice v jedné ose (Velikost obsahu / reálný prostor) + Scrollbar pro tuto osu
        /// </summary>
        private class VirtualDimension
        {
            #region Konstruktor a privátní fieldy a základní metody
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="axis"></param>
            public VirtualDimension(GraphicsControl owner, Axis axis)
            {
                __Owner = owner;
                __Axis = axis;
                ReloadScrollbarSize();
            }
            /// <summary>
            /// Vlastník
            /// </summary>
            private GraphicsControl __Owner;
            /// <summary>
            /// Směr osy
            /// </summary>
            private Axis __Axis;
            /// <summary>
            /// Velikost zdejšího Scrollbaru, pokud bude zobrazen.
            /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
            /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
            /// </summary>
            private int __ScrollbarSize;
            /// <summary>
            /// Hodnota SmallChange na ScrollBaru
            /// </summary>
            private int __ScrollbarSmallChange;
            /// <summary>
            /// Vrátí hodnotu pro osu X nebo Y podle <see cref="__Axis"/>.
            /// Hodnotu čte pomocí dodané funkce.
            /// <para/>
            /// Myslím že je to optimálnější, než když bych očekával dva parametry obsahující prostá hotová data - protože při volání zdejší metody bych je nejprve musel oba vyhodnotit (náročnost), a pak bych jeden zahodil.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="funcValueX"></param>
            /// <param name="funcValueY"></param>
            /// <returns></returns>
            private T _GetValue<T>(Func<T> funcValueX, Func<T> funcValueY)
            {
                switch (__Axis)
                {
                    case Axis.X: return funcValueX();
                    case Axis.Y: return funcValueY();
                }
                return default(T);
            }
            #endregion
            #region Používání a velikost Scrollbaru
            /// <summary>
            /// V tomto směru by měl být zobrazen Scrollbar? Detekuje se z rozměrů (virtuální a nativní) a z přítomnosti a velikosti Scrollbaru v opačném směru
            /// </summary>
            public bool NeedScrollbar
            {
                get
                {
                    int? contentSize = this._ContentSize;
                    if (!contentSize.HasValue) return false;
                    int clientSize = this._ClientSize - this._OtherScrollbarSize;
                    if (clientSize <= (__ScrollbarSize + 10)) return false;
                    return contentSize.Value > clientSize;
                }
            }
            /// <summary>
            /// V tomto směru bude zobrazen Scrollbar? 
            /// Setuje <see cref="__Owner"/> jako výsledek výpočtů!
            /// Owner k tomu používá hodnotu <see cref="NeedScrollbar"/> z této dimenze, ale musí zohlednit křížovou potřebu Scrollbaru.
            /// Slovně: pokud osa X těsně nepotřebuje Scrollbar, ale osa Y jej potřebuje, tak zmenší vizuální prostor na ose X a poté i osa X potřebuje svůj Scrollbar - proto, že dostupný prostor na ose X zmenšil Scrollbar Y.
            /// </summary>
            public bool UseScrollbar { get; set; }
            /// <summary>
            /// Znovu načte velikost Scrollbar - je vhodné volat po změně DPI atd...
            /// </summary>
            public void ReloadScrollbarSize()
            {
                __ScrollbarSize = _GetValue(() => SystemInformation.HorizontalScrollBarHeight, () => SystemInformation.VerticalScrollBarWidth);
                __ScrollbarSmallChange = (int)(((float)SystemInformation.MouseWheelScrollLines) * App.GetSystemFont(App.FontType.DefaultFont).GetHeight());
            }
            /// <summary>
            /// Velikost zdejšího Scrollbaru, pokud bude zobrazen. Zde je tedy kladné číslo, i když scrollbar aktuálně není zobrazen.
            /// Pro dimenzi X (vodorovná, řeší X a Width) je zde Výška vodorovného Scrollbaru.
            /// Pro dimenzi Y (svislá, řeší Y a Height) je zde Šířka svislého Scrollbaru.
            /// </summary>
            public int ScrollbarSize { get { return __ScrollbarSize; } }
            /// <summary>
            /// Obsahuje true, pokud objekt reprezentuje virtuální prostor = má nastavenou velikost obsahu <see cref="__ContentSize"/> (kladné rozměry).
            /// V tom případě se v procesu Resize v metodě <see cref="_AcceptControlSize"/> 
            /// </summary>
            private bool _IsInVirtualMode { get { return __Owner._IsInVirtualMode; } }
            /// <summary>
            /// Velikost datového obsahu = virtuální velikost
            /// </summary>
            private int? _ContentSize { get { return _GetValue(() => __Owner.__ContentSize?.Width, () => __Owner.__ContentSize?.Height); } }
            /// <summary>
            /// Velikost viditelného prostoru, celková (tj. fyzický Control = obsah + případný scrollbar)
            /// </summary>
            private int _ClientSize { get { return _GetValue(() => __Owner.ClientSize.Width, () => __Owner.ClientSize.Height); } }
            /// <summary>
            /// Velikost viditelného datového prostoru, zmenšená o druhý (párový) Scrollbar = zde jsou zobrazena data
            /// </summary>
            private int _DataSize { get { return _GetValue(() => __Owner.ClientSize.Width - __Owner.__DimensionY._CurrentScrollbarSize, () => __Owner.ClientSize.Height - __Owner.__DimensionX._CurrentScrollbarSize); } }
            /// <summary>
            /// Aktuální velikost zdejšího Scrollbar, se zohledněním <see cref="UseScrollbar"/> (pokud se nepoužívá, je zde 0)
            /// </summary>
            private int _CurrentScrollbarSize { get { return (UseScrollbar ? __ScrollbarSize : 0); } }
            /// <summary>
            /// Aktuální velikost Scrollbar z opačné osy, se zohledněním jejího <see cref="UseScrollbar"/> (pokud se nepoužívá, je zde 0)
            /// </summary>
            private int _OtherScrollbarSize { get { return _GetValue(() => __Owner.__DimensionY._CurrentScrollbarSize, () => __Owner.__DimensionX._CurrentScrollbarSize); } }
            #endregion
            #region Aktivace a Eventhandler Scrollbaru, jeho VirtualBegin = počátek virtuálního prostoru
            /// <summary>
            /// Zobrazí Scrollbar na správném místě a se správnými hodnotami
            /// </summary>
            public void ShowScrollBar()
            {
                var scrollBar = _ScrollBar;
                if (scrollBar is null) return;
                if (!UseScrollbar)
                {   // Nezobrazovat:
                    if (scrollBar.Visible) scrollBar.Visible = false;
                    return;
                }

                try
                {
                    __SuppressScrollBarValueChange = true;

                    scrollBar.Bounds = _GetScrollbarBounds();

                    _GetScrollbarValues(out int minimum, out int maximum, out int value, out int largeChange, out int smallChange);
                    scrollBar.Minimum = minimum;
                    scrollBar.Maximum = maximum;           // Reprezentuje celý virtuální prostor = ContentSize
                    scrollBar.Value = value;               // Na této hodnotě je první zobrazený pixel z virtuální oblasti; Value má rozsah od Minimum do (Maximum - LargeChange + 1)
                    scrollBar.LargeChange = largeChange;   // Odpovídá viditelné oblasti = ClientSize, typicky je cca 90% = provede posun o "celou stránku" = při kliknutí na prostor nad / pod Thumbem
                    scrollBar.SmallChange = smallChange;   // Odpovídá scrollování myší nebo kliknutí na horní/dolní button, typicky 5% viditelné oblasti
                }
                finally
                {
                    __SuppressScrollBarValueChange = false;
                }

                if (!scrollBar.Visible) scrollBar.Visible = true;
            }
            /// <summary>
            /// Handler události, kdy patřičný ScrollBar zaregistroval pohyb / změnu hodnoty
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void ScrollBarValueChanged(object sender, EventArgs e)
            {
                if (__SuppressScrollBarValueChange) return;
                if (!_IsInVirtualMode) return;

                _SetVirtualBegin(_ScrollBar.Value);
            }
            /// <summary>
            /// Control Scrollbar pro tuto osu
            /// </summary>
            private ScrollBar _ScrollBar { get { return _GetValue<ScrollBar>(() => __Owner.__ScrollBarX, () => __Owner.__ScrollBarY); } }
            /// <summary>
            /// Vrátí fyzické souřadnice pro control Scrollbar
            /// </summary>
            /// <returns></returns>
            private Rectangle _GetScrollbarBounds()
            {
                var clientSize = this.__Owner.ClientSize;
                int x, y, w, h;
                switch (this.__Axis)
                {
                    case Axis.X:
                        w = clientSize.Width - _OtherScrollbarSize;
                        h = __ScrollbarSize;
                        x = 0;
                        y = clientSize.Height - h;
                        return new Rectangle(x, y, w, h);
                    case Axis.Y:
                        h = clientSize.Height - _OtherScrollbarSize;
                        w = __ScrollbarSize;
                        y = 0;
                        x = clientSize.Width - w;
                        return new Rectangle(x, y, w, h);
                }
                return Rectangle.Empty;
            }
            /// <summary>
            /// Určí a vrátí hodnoty pro fyzický Scrollbar
            /// </summary>
            /// <param name="minimum"></param>
            /// <param name="maximum"></param>
            /// <param name="value"></param>
            /// <param name="largeChange"></param>
            /// <param name="smallChange"></param>
            private void _GetScrollbarValues(out int minimum, out int maximum, out int value, out int largeChange, out int smallChange)
            {
                var dataSize = _DataSize;

                minimum = 0;
                maximum = _ContentSize ?? dataSize;
                value = VirtualBegin;
                largeChange = dataSize;
                smallChange = __ScrollbarSmallChange;
            }
            /// <summary>
            /// Aktuálně je potlačený event <see cref="ScrollBarValueChanged"/> = změny provádíme z kódu a nechceme se v nich zacyklit
            /// </summary>
            private bool __SuppressScrollBarValueChange;
            /// <summary>
            /// Hodnota Virtuálního počátku.
            /// Vyjadřuje souřadnici Virtuálního prostoru, která je zobrazena na viditelném pixelu 0 = první viditelný pixel virtuálního prostoru.
            /// <para/>
            /// Pokud je například <see cref="VirtualBegin"/> = 50; pak to značí, že ScrollBar mírně odroloval od začátku, a prvních 50 pixelů virtuálního (tj. celkového) obsahu je skryto vlevo/nahoře.
            /// </summary>
            public int VirtualBegin
            {
                get { return _GetVirtualBegin(); }
                set { _SetVirtualBegin(value); }
            }
            /// <summary>
            /// Událost volaná po jakékoli změně počátku virtuálního prostoru
            /// </summary>
            public event EventHandler VirtualBeginChanged;
            /// <summary>
            /// Metoda volaná po jakékoli změně počátku virtuálního prostoru
            /// </summary>
            /// <param name="args"></param>
            protected virtual void OnVirtualBeginChanged(EventArgs args) { }
            /// <summary>
            /// Vyvolá metodu <see cref="OnVirtualBeginChanged(EventArgs)"/> a event <see cref="VirtualBeginChanged"/>
            /// </summary>
            private void _RunVirtualBeginChanged()
            {
                EventArgs args = EventArgs.Empty;
                OnVirtualBeginChanged(args);
                VirtualBeginChanged?.Invoke(this, args);
            }

            /// <summary>
            /// Vrátí validní hodnotu počátku virtuálního prostoru
            /// </summary>
            /// <returns></returns>
            private int _GetVirtualBegin()
            {
                if (!UseScrollbar) return 0;

                int virtualBegin = _GetValidVirtualBegin(__VirtualBegin);
                if (virtualBegin != __VirtualBegin) __VirtualBegin = virtualBegin;
                return virtualBegin;
            }
            /// <summary>
            /// Uloží danou hodnotu po validaci jako hodnotu počátku virtuálního prostoru
            /// </summary>
            /// <param name="virtualBegin"></param>
            private void _SetVirtualBegin(int virtualBegin)
            {
                if (!_IsInVirtualMode) return;
                if (!UseScrollbar) return;

                var validVirtualBegin = _GetValidVirtualBegin(virtualBegin);
                if (validVirtualBegin != __VirtualBegin)
                {
                    __VirtualBegin = validVirtualBegin;
                    _RunVirtualBeginChanged();
                }
            }
            /// <summary>
            /// Vrátí hodnotu <paramref name="virtualBegin"/> zarovnanou do platných mezí tak, aby nebyla záporná a aby nepřesáhla konec
            /// </summary>
            /// <param name="virtualBegin"></param>
            /// <returns></returns>
            private int _GetValidVirtualBegin(int virtualBegin)
            {
                int clientSize = _ClientSize;
                int virtualEnd = virtualBegin + clientSize + 1;
                int? contentSize = _ContentSize;
                if (contentSize.HasValue && virtualEnd > contentSize.Value)
                {
                    virtualEnd = contentSize.Value;
                    virtualBegin = virtualEnd - clientSize;
                }
                if (virtualBegin < 0) virtualBegin = 0;
                return virtualBegin;
            }
            private int __VirtualBegin;
            #endregion
        }
        /// <summary>
        /// Orientace osy a Scrollbaru
        /// </summary>
        private enum Axis 
        {
            /// <summary>
            /// X: vodorovný scrollbar, je dole
            /// </summary>
            X,
            /// <summary>
            /// Y: svislý scrollbar, je vpravo
            /// </summary>
            Y
        }
        #endregion
        #region Řízení kreslení - vyvolávací metoda + virtual výkonná metoda
        /// <summary>
        /// Potlačení kreslení při provádění rozsáhlejších změn. Po ukončení je třeba nastavit na false !
        /// Default = false = kreslení není potlačeno.
        /// Při provádění rozsáhlejších změn je vhodné nastavit na true, a po dokončení změn vrátit na false => tím se automaticky vyvolá Draw.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool SuppressDrawing
        {
            get
            {
                return this._SuppressDrawing;
            }
            set
            {
                bool setDraw = (!value && this._SuppressDrawing);        // true při změně z true na false 
                this._SuppressDrawing = value;
                if (setDraw)
                    this.Draw();
                this._SuppressLevel = 0;
            }
        }
        private bool _SuppressDrawing = false;
        private int _SuppressLevel = 0;
        /// <summary>
        /// Umožní řídit potlačení vykreslování v hierarchii metod.
        /// Zajistí, že vykreslování bude potlačeno přinejmenším do párového vyvolání metody SuppressDrawingPop().
        /// Chování je obdobou chování Stacku: první Push zablokuje kreslení, následné Push a Pop to nezmění, poslední Pop to povolí.
        /// Podmínka: Push a Pop musí být v páru, jinak kreslení zamrzne.
        /// Řešení: je možno kdykoliv vložit SuppressDrawing = false a vykreslování ožije (nepárový zásobník se vynuluje).
        /// </summary>
        public virtual void SuppressDrawingPush()
        {
            if (this._SuppressLevel <= 0)        // První volání skutečně zablokuje Drawing:
                this.SuppressDrawing = true;
            this._SuppressLevel++;               // Každé volání zvýší level
        }
        /// <summary>
        /// Umožní řídit potlačení vykreslování v hierarchii metod.
        /// Zajistí, že vykreslování bude potlačeno přinejmenším do párového vyvolání metody SuppressDrawingPop().
        /// Chování je obdobou chování Stacku: první Push zablokuje kreslení, následné Push & Pop to nezmění, poslední Pop to povolí.
        /// Podmínka: Push a Pop musí být v páru, jinak kreslení zamrzne.
        /// Řešení: je možno kdykoliv vložit SuppressDrawing = false a vykreslování ožije (nepárový zásobník se vynuluje).
        /// </summary>
        public virtual void SuppressDrawingPop()
        {
            this._SuppressLevel--;               // Každé volání sníží level
            if (this._SuppressLevel <= 0)        // A až se dostaneme na 0, tak obnovíme Drawing:
                this.SuppressDrawing = false;
        }
        /// <summary>
        /// Metoda, která zajišťuje kreslení.
        /// Potomkové mohou využít, ale musí volat base(sender, e);
        /// base metoda zajišťuje e.Graphics.Clear(this.BackColor);
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnPaintToBuffer(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
        }
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual void Draw()
        {
            this._Draw(Rectangle.Empty);
        }
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        /// <param name="drawRectangle">
        /// Informace pro kreslící program o rozsahu překreslování.
        /// Nemusí nutně jít o grafický prostor, toto je pouze informace předáváná z parametru metody Draw() do handleru PaintToBuffer().
        /// V servisní třídě se nikdy nepoužije ve významu grafického prostoru.
        /// </param>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual void Draw(Rectangle drawRectangle)
        {
            this._Draw(drawRectangle);
        }
        #endregion
        #endregion
        #region Podpora kreslení - konverze barev, kreslení Borderu, Stringu, atd
        #region COLOR SHIFT
        /// <summary>
        /// Posune danou barvu o daný posun. Odstín ponechává, posouvá světlost.
        /// </summary>
        /// <param name="color">Vstupní barva</param>
        /// <param name="shift">Posun, zadaný v číslu (+- 255)</param>
        /// <returns>Upravená barva</returns>
        public static Color ColorShift(Color color, int shift)
        {
            int r = _ColorShiftOne(color.R, shift);
            int g = _ColorShiftOne(color.G, shift);
            int b = _ColorShiftOne(color.B, shift);
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Posune danou barvu o daný posun, v každé složce může být jiný.
        /// </summary>
        /// <param name="color">Vstupní barva</param>
        /// <param name="shift">Posun, zadaný v číslu (+- 255)</param>
        /// <returns>Upravená barva</returns>
        public static Color ColorShift(Color color, int shiftR, int shiftG, int shiftB)
        {
            int r = _ColorShiftOne(color.R, shiftR);
            int g = _ColorShiftOne(color.G, shiftG);
            int b = _ColorShiftOne(color.B, shiftB);
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Posune jednu barevnou složku o daný posun.
        /// </summary>
        /// <param name="colourComponent"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        private static int _ColorShiftOne(byte colourComponent, int shift)
        {
            int newColor = colourComponent + shift;
            return ((newColor < 0) ? 0 : ((newColor > 255) ? 255 : newColor));
        }
        #endregion
        #region BORDER
        /// <summary>
        /// Vykreslí rámeček okolo celého this controlu
        /// </summary>
        public void DrawBorder(Graphics graphics)
        {
            DrawBorder(graphics, new Point(0, 0), this.Size, this.BorderStyle, this.BorderSides);
        }
        /// <summary>
        /// Metoda vrátí aktuální prostor pro kreslení po odečtení Borderu od Size
        /// </summary>
        /// <returns></returns>
        private Rectangle _GetClientArea()
        {
            Padding borderPadd = _GetBorderWidth();
            Rectangle client = new Rectangle(
                borderPadd.Left,
                borderPadd.Top,
                this.Width - borderPadd.Left - borderPadd.Right,
                this.Height - borderPadd.Top - borderPadd.Bottom);
            return client;
        }
        /// <summary>
        /// Metoda vrátí šířku Borderu na jednotlivých okrajích prvku
        /// </summary>
        /// <returns></returns>
        private Padding _GetBorderWidth()
        {
            BorderStyle style = this.BorderStyle;
            int border = (style == BorderStyle.None ? 0 : (style == BorderStyle.FixedSingle ? 1 : 2));

            Border3DSide sides = this.BorderSides;
            Padding padd = new Padding();
            padd.Top = (((sides & Border3DSide.Top) == Border3DSide.Top) ? border : 0);
            padd.Left = (((sides & Border3DSide.Left) == Border3DSide.Left) ? border : 0);
            padd.Right = (((sides & Border3DSide.Right) == Border3DSide.Right) ? border : 0);
            padd.Bottom = (((sides & Border3DSide.Bottom) == Border3DSide.Bottom) ? border : 0);
            return padd;
        }
        /// <summary>
        /// Vykreslí rámeček do specifikovaného prostoru
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="rectangle">Prostor</param>
        public static void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            DrawBorder(graphics, rectangle.Location, rectangle.Size, BorderStyle.Fixed3D, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size)
        {
            DrawBorder(graphics, begin, size, BorderStyle.Fixed3D, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu, v daném stylu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        /// <param name="borderStyle">Styl borderu, default = BorderStyle.Fixed3D</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size, BorderStyle borderStyle)
        {
            DrawBorder(graphics, begin, size, borderStyle, Border3DSide.All);
        }
        /// <summary>
        /// Vykreslí rámeček okolo celého předaného controlu, v daném stylu
        /// </summary>
        /// <param name="graphics">Grafika</param>
        /// <param name="begin">Počátek</param>
        /// <param name="size">Velikost</param>
        /// <param name="borderStyle">Styl borderu, default = BorderStyle.Fixed3D</param>
        /// <param name="borderSide">Kreslené okraje borderu, default = Border3DSide.All</param>
        public static void DrawBorder(Graphics graphics, Point begin, Size size, BorderStyle borderStyle, Border3DSide borderSide)
        {
            if (borderStyle == BorderStyle.None) return;
            Point target = new Point(begin.X + size.Width - 1, begin.Y + size.Height - 1);
            Color controlColor = SystemColors.ControlDark;
            using (Pen border = new Pen(controlColor, 1F))
            {
                switch (borderStyle)
                {
                    case BorderStyle.FixedSingle:
                        border.Color = Color.Black;
                        graphics.DrawRectangle(border, begin.X, begin.Y, size.Width - 1, size.Height - 1);
                        break;
                    case BorderStyle.Fixed3D:
                        border.Color = SystemColors.ControlDark;
                        graphics.DrawLine(border, begin.X, begin.Y, target.X - 1, begin.Y);                 // Vnější horní čára
                        graphics.DrawLine(border, begin.X, begin.Y, begin.X, target.Y - 1);                 // Vnější levá čára
                        border.Color = SystemColors.ControlDarkDark;
                        graphics.DrawLine(border, begin.X + 1, begin.Y + 1, target.X - 2, begin.Y + 1);     // Vnitřní horní čára
                        graphics.DrawLine(border, begin.X + 1, begin.Y + 1, begin.X + 1, target.Y - 2);     // Vnitřní levá čára
                        border.Color = SystemColors.ControlLightLight;
                        graphics.DrawLine(border, target.X, begin.Y, target.X, target.Y);                   // Vnější pravá čára
                        graphics.DrawLine(border, begin.X, target.Y, target.X, target.Y);                   // Vnější dolní čára
                        border.Color = SystemColors.Control;
                        graphics.DrawLine(border, target.X - 1, begin.Y + 1, target.X - 1, target.Y - 1);   // Vnitřní pravá čára
                        graphics.DrawLine(border, begin.X + 1, target.Y - 1, target.X - 1, target.Y - 1);   // Vnitřní dolní čára
                        break;
                }
            }
        }
        #endregion
        #region DRAW STRING s řešením zarovnání
        /// <summary>
        /// Do daného prostoru vepíše text, se zarovnáním
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="brush"></param>
        /// <param name="textArea"></param>
        /// <param name="alignment"></param>
        public static void DrawString(Graphics graphics, string text, Font font, Brush brush, Rectangle textArea, ContentAlignment alignment, StringFormatFlags stringFormat)
        {
            StringFormat format = new StringFormat(stringFormat);
            bool isVertical = ((stringFormat & StringFormatFlags.DirectionVertical) == StringFormatFlags.DirectionVertical);
            int textWidth = (isVertical ? textArea.Height : textArea.Width);
            SizeF textSize = graphics.MeasureString(text, font, textWidth, format);
            RectangleF alignArea = AlignSizeIntoArea(textArea, textSize, alignment);
            graphics.DrawString(text, font, brush, alignArea, format);
        }
        /// <summary>
        /// Zarovná určitý prostor do daného prostoru v daném zarovnání.
        /// </summary>
        /// <param name="outerArea">Vnější prostor</param>
        /// <param name="innerSize">Vnitřní rámec</param>
        /// <param name="alignment">Styl zarovnání</param>
        /// <returns>Vnitřní rámec, zarovnaný do vnějšího prostoru</returns>
        public static RectangleF AlignSizeIntoArea(RectangleF outerArea, SizeF innerSize, ContentAlignment alignment)
        {
            // Zarovnání spočívá v určení pointu, kde se začne s psaním:
            PointF origin = PointF.Empty;
            // Svisle:
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    origin.Y = outerArea.Y;
                    break;
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    origin.Y = (outerArea.Y + 0.5F * (outerArea.Height - innerSize.Height));
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    origin.Y = (outerArea.Y + (outerArea.Height - innerSize.Height));
                    break;
            }

            // Vodorovně:
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    origin.X = outerArea.X;
                    break;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    origin.X = (outerArea.X + 0.5F * (outerArea.Width - innerSize.Width));
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    origin.X = (outerArea.X + (outerArea.Width - innerSize.Width));
                    break;
            }

            return new RectangleF(origin, innerSize);
        }

        #endregion
        #region FIND FOCUSED CONTROL
        /// <summary>
        /// Najde a vrátí objekt Control.
        /// Inspirace: http://windowsclient.net/blogs/faqs/archive/2006/05/26/how-do-i-find-out-which-control-has-focus.aspx
        /// </summary>
        /// <returns></returns>
        public static Control FindControlWithFocus()
        {
            Control focusControl = null;
            IntPtr focusHandle = GetFocus();
            if (focusHandle != IntPtr.Zero)
                // returns null if handle is not to a .NET control
                focusControl = Control.FromHandle(focusHandle);
            return focusControl;
        }
        // Import GetFocus() from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
        internal static extern IntPtr GetFocus();
        #endregion
        #endregion
    }
    #region class MouseState : stav myši
    /// <summary>
    /// Stav myši
    /// </summary>
    public class MouseState
    {
        /// <summary>
        /// Vrátí aktuální stav myši pro daný Control
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static MouseState CreateCurrent(Control control)
        {
            DateTime time = DateTime.Now;
            Point locationAbsolute = Control.MousePosition;
            MouseButtons buttons = Control.MouseButtons;
            Point locationNative = control.PointToClient(locationAbsolute);
            bool isOnControl = control.ClientRectangle.Contains(locationNative);
            return new MouseState(time, locationNative, locationAbsolute, buttons, isOnControl);
        }
        /// <summary>
        /// Vrátí stav myši pro dané hodnoty
        /// </summary>
        /// <param name="time"></param>
        /// <param name="locationNative">Souřadnice myši v koordinátech controlu (nativní prostor)</param>
        /// <param name="locationAbsolute"></param>
        /// <param name="buttons"></param>
        /// <param name="isOnControl"></param>
        public MouseState(DateTime time, Point locationNative, Point locationAbsolute, MouseButtons buttons, bool isOnControl)
        {
            __Time = time;
            __LocationNative = locationNative;
            __LocationAbsolute = locationAbsolute;
            __Buttons = buttons;
            __IsOnControl = isOnControl;
        }
        private DateTime __Time;
        private Point __LocationNative;
        private Point __LocationAbsolute;
        private MouseButtons __Buttons;
        private bool __IsOnControl;
        /// <summary>
        /// Čas akce myši; důležité pro případný doubleclick
        /// </summary>
        public DateTime Time { get { return __Time; } }
        /// <summary>
        /// Souřadnice myši v koordinátech controlu (nativní prostor)
        /// </summary>
        public Point LocationControl { get { return __LocationNative; } }
        /// <summary>
        /// Souřadnice myši v koordinátech absolutních (Screen)
        /// </summary>
        public Point LocationAbsolute { get { return __LocationAbsolute; } }
        /// <summary>
        /// Stav buttonů myši
        /// </summary>
        public MouseButtons Buttons { get { return __Buttons; } }
        /// <summary>
        /// Ukazatel myši se nachází nad controlem?
        /// </summary>
        public bool IsOnControl { get { return __IsOnControl; } }
    }
    #endregion
    #region interface IVirtualContainer
    public interface IVirtualContainer
    { }
    #endregion
    #region class ToolTipControl
    public class ToolTipControl : Control 
    {
        #region Podpora inicializace tooltipu - až poté, kdy je control umístěn na formuláři!
        /// <summary>
        /// Po změně Visible.
        /// Při změně na true zajišťuje CheckToolTipInitialized() a Draw()
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
                this.CheckToolTipInitialized();
        }
        /// <summary>
        /// Event, který je volán tehdy, když se má inicializovat Tooltip
        /// </summary>
        public event EventHandler ToolTipInitialize;
        /// <summary>
        /// Virtuální metoda, do které je vhodné v potomkovi vepsat kód pro inicializaci tooltipu.
        /// Metoda je volána jen tehdy, když objekt je již umístěn na Formu (tj. existuje již správce tooltipu), metoda je volána pouze jedenkrát.
        /// Je možné využít i eventu ToolTipInitialize.
        /// Bázová metoda na třídě DblGraphControl je prázdná.
        /// </summary>
        protected virtual void OnToolTipInitialize()
        { }
        /// <summary>
        /// Výkonná metoda, zajistí že bude inicializován Tooltip pro tento control.
        /// </summary>
        protected void CheckToolTipInitialized()
        {
            if (this.ToolTipInitialized) return;
            if (this.FindForm() == null) return;

            if (this.ToolTipInitialize != null)
                this.ToolTipInitialize(this, EventArgs.Empty);

            this.OnToolTipInitialize();

            this.ToolTipInitialized = true;
        }
        /// <summary>
        /// Příznak, že tooltip již prošel inicializací
        /// </summary>
        protected bool ToolTipInitialized = false;
        #endregion
    }
    #endregion
    #region ScrollBars
    /// <summary>
    /// Vodorovný ScrollBar
    /// </summary>
    public class ScrollBarX : HScrollBar
    {
        /// <summary>
        /// Zajistí provedení akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        public void DoMouseWheel(MouseEventArgs e)
        {
            this.OnMouseWheel(e);
        }
    }
    /// <summary>
    /// Svislý ScrollBar
    /// </summary>
    public class ScrollBarY : VScrollBar
    {
        /// <summary>
        /// Zajistí provedení akce MouseWheel
        /// </summary>
        /// <param name="e"></param>
        public void DoMouseWheel(MouseEventArgs e)
        {
            this.OnMouseWheel(e);
        }
    }
    #endregion
}
