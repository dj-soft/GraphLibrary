// Supervisor: David Janáček, od 01.11.2023
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using WinDraw = System.Drawing;
using WinForm = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DxInteractivePanel : interaktivní grafický panel
    /// <summary>
    /// <see cref="DxInteractivePanel"/> : interaktivní grafický panel.
    /// Řeší grafické vykreslení prvků a řeší interaktivitu myši a klávesnice.
    /// Pro fyzické vykreslení obsahu prvku volá jeho vlastní metodu, to neřeší panel.
    /// </summary>
    public class DxInteractivePanel : DxBufferedGraphicPanel
    {
        #region Info ... Vnoření prvků, souřadné systémy, řádky
        /*

        Třída DxInteractivePanel v sobě hostuje interaktivní prvky IInteractiveItem uložené v DxInteractivePanel.Items, ty tvoří Root úroveň.
        Mohou to být Containery { Panely, Záložky } nebo samotné prvky { TextBox, Label, Combo, Picture, CheckBox, atd }.
        Containery v sobě mohou obsahovat další Containery nebo samotné prvky.
        Containery obsahují svoje Child prvky v IInteractiveItem.Items. Jejich souřadný systém je pak relativní k jejich Parentu.
        Scrollování je podporováno jen na úrovni celého DataFormu, ale nikoliv na úrovni Containeru (=Panel ani Záložka nebude mít ScrollBary).
        DataForm podporuje Zoom. Výchozí je vložen při tvorbě DataFormu anebo při změně Zoomu, podporován je i interaktivní Zoom: Ctrl+MouseWheel.
        Jde o dvě hodnoty Zoomu: systémový × lokální.
        Prvek IInteractiveItem definuje svoji pozici v property DesignBounds, kde je souřadnice daná v Design pixelech (bez vlivu Zoomu), relativně k Parentu, v rámci řádku.

          Řádky / Prvky a zdejší třída DxInteractivePanel versus potomek DxDataFormPanel:
        Zdejší třída DxInteractivePanel NEZNÁ pojem řádek. Má sadu prvků IInteractiveItem uložené v DxInteractivePanel.Items a ty řeší.
         - velikost prostoru VirtualSize určuje tedy jen z těchto Items.
        Potomek třída DxDataFormPanel ŘEŠÍ řádky s daty (DataFormRows), eviduje vlastní zdroj definice vzhledu (DataFormLayout)
         a z nich teprve generuje jednotlivé interaktivní prvky IInteractiveItem, kde jeden Item odpovídá jednomu Column v jednom konkrétním řádku.
         Tedy tento IInteractiveItem reprezentuje obdobu Cell.
         - Potomek DxDataFormPanel tedy řeší VirtualSize jinak = jako součin počtu řádků × velikost layoutu pro jeden řádek.
         Více v třídě DxDataFormPanel.

        */
        #endregion
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxInteractivePanel()
        {
            _InitItems();
            _InitLayout();
            _InitInteractivity();
        }
        #endregion
        #region IInteractiveItem prvky - soupis aktivních prvků
        /// <summary>
        /// Prvky k zobrazení a interaktivní práci.
        /// Lze setovat, tím se dosavadní prvky zahodí a vloží se prvky z dodaného pole (ale instance dodaného pole se sem neukládá).
        /// </summary>
        public DxInteractiveItems Items
        {
            get { return __ItemsAll; }
            set
            {
                __ItemsAll.Clear();
                if (value != null)
                    __ItemsAll.AddRange(value);
            }
        }
        /// <summary>
        /// Inicializace oblasti Items
        /// </summary>
        private void _InitItems()
        {
            __ItemsAll = new DxInteractiveItems(this);
            __ItemsAll.CollectionChanged += _ItemsChanged;
        }
        /// <summary>
        /// Událost volaná po změně kolekce <see cref="Items"/>. Zajistí invalidaci příznaku platnosti <see cref="WinDraw.ContentAlignment"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ItemsChanged(object sender, EventArgs e)
        {
            ContentDesignSizeInvalidate();
            ItemsPaintedInteractiveInvalidate();
        }
        /// <summary>
        /// Interaktivní data = jednotlivé prvky. Úložiště pole.
        /// </summary>
        private DxInteractiveItems __ItemsAll;
        /// <summary>
        /// Veškeré prvky, které mohou připadat do úvahy pro vykreslení a interaktivitu.
        /// Virtuální property, potomek může přepsat a řešit tak zcela vlastní zdroj pro interaktivní prvky.
        /// Bázová třída <see cref="DxInteractivePanel"/> zde vrací pole <see cref="Items"/>.
        /// </summary>
        protected virtual IList<IInteractiveItem> ItemsAll { get { return __ItemsAll; } }
        /// <summary>
        /// Kolekce prvků, které byly naposledy vykresleny. Jde o prvky, které v procesu kreslení <see cref="PaintAllDataItems(PaintDataEventArgs, IEnumerable{IInteractiveItem})"/> 
        /// byly kresleny metodou <see cref="IInteractiveItem.Paint(PaintDataEventArgs)"/> a vrátily v ní true = prvek je vykreslen (false značí, že prvek je mimo viditelný prostor a nekreslí se).
        /// Pokud dosud nebyly prvky kresleny, je zde soupis všech prvků shodný s <see cref="ItemsAll"/>.
        /// </summary>
        protected virtual IList<IInteractiveItem> ItemsPainted 
        {
            get 
            {
                if (__ItemsPainted != null) return __ItemsPainted;
                return ItemsAll;
            }
        }
        /// <summary>
        /// Kolekce prvků, které byly naposledy vykresleny a mají příznak interaktivity.
        /// Jde o prvky, které v procesu kreslení <see cref="PaintAllDataItems(PaintDataEventArgs, IEnumerable{IInteractiveItem})"/> 
        /// byly kresleny metodou <see cref="IInteractiveItem.Paint(PaintDataEventArgs)"/> a vrátily v ní true = prvek je vykreslen (false značí, že prvek je mimo viditelný prostor a nekreslí se).
        /// Pokud dosud nebyly prvky kresleny, je zde soupis všech prvků shodný s <see cref="ItemsAll"/>.
        /// </summary>
        protected virtual IList<IInteractiveItem> ItemsInteractive
        {
            get
            {
                if (__ItemsInteractive != null) return __ItemsInteractive;
                return ItemsAll;
            }
        }
        /// <summary>
        /// Invaliduje vnitřní data po změně v poli prvků.
        /// Použvají potomci, kteří přepisují <see cref="ItemsAll"/> tak, že vracejí jiné pole, tuto metodu volají po změně ve svých datech.
        /// </summary>
        public virtual void ItemsAllChanged()
        {
            ItemsPaintedInteractiveInvalidate();
            __CurrentMouseItem = null;
            __CurrentMouseDownState = null;
            __MouseDragCurrentDataItem = null;
        }
        /// <summary>
        /// Invaliduje pole fyzicky vykreslených a interaktivních prvků. 
        /// Volá se po změně velikosti, po scrollu a po změnách interaktivních prvků v poli <see cref="ItemsAll"/>.
        /// Validuje se prvním vykreslením prvků. 
        /// Do té doby se za vykreslené a interaktivní berou všechny prvky.
        /// </summary>
        protected virtual void ItemsPaintedInteractiveInvalidate()
        {
            __ItemsPainted = null;
            __ItemsInteractive = null;
        }
        /// <summary>
        /// Kolekce prvků, které byly naposledy vykresleny bez ohledu na jejich interaktivitu. Úložiště.
        /// </summary>
        private List<IInteractiveItem> __ItemsPainted;
        /// <summary>
        /// Kolekce prvků, které byly naposledy vykresleny a mají příznak že jsou interaktivní. Úložiště.
        /// </summary>
        private List<IInteractiveItem> __ItemsInteractive;
        #endregion
        #region Layout prvků, velikost obsahu VirtualSize
        /// <summary>
        /// Souhrnná velikost obsahu = jednotlivé Items, včetně okrajů Padding
        /// </summary>
        public override WinDraw.Size? ContentDesignSize { get { ContentDesignSizeCheckValidity(); return __CurrentContentDesignSize.Value; } }
        /// <summary>
        /// Znovu napočte rozmístění prvků a volitelně vyvolá jejich překreslení
        /// </summary>
        /// <param name="draw"></param>
        public virtual void RefreshContent(bool draw = false)
        {
            ContentDesignSizeInvalidate();
            if (draw) this.Draw();
        }
        /// <summary>
        /// Metoda zneplatní příznak platné hodnoty <see cref="ContentDesignSize"/> 
        /// (volá se po změně definice <see cref="Items"/> a po změně layoutu a po dalších změnách, např. po změně počtu řádků na potomku).
        /// Následně bude volána metoda <see cref="CalculateContentDesignSize()"/>, která určí celkovou velikost obsahu, na kterou se následně nastaví ScrollBary.
        /// </summary>
        protected virtual void ContentDesignSizeInvalidate()
        {
            __CurrentContentDesignSize = null;
        }
        /// <summary>
        /// Metoda projde všechny své viditelné prvky a určí max potřebné souřadnice Right a Bottom. 
        /// Přičte odpovídající hodnoty z Padding a vrátí výsledek.
        /// Tato metoda nevepisuje aktuální souřadnice do prvků, to se řeší v procesu Draw.
        /// <para/>
        /// Potomek může velikost určovat jinak, než jen z přítomných <see cref="Items"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual WinDraw.Size CalculateContentDesignSize()
        {
            int r = 0;
            int b = 0;
            var items = ItemsAll;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.IsVisible)
                    {
                        var itemBounds = item.DesignBounds;
                        if (r < itemBounds.Right) r = itemBounds.Right;
                        if (b < itemBounds.Bottom) b = itemBounds.Bottom;
                    }
                }
            }

            var padding = this.Padding;
            return new WinDraw.Size(r + padding.Horizontal, b + padding.Vertical);
        }
        /// <summary>
        /// Inicializace oblasti Layout
        /// </summary>
        private void _InitLayout()
        {
            Padding = new WinForm.Padding(0);
            PaddingChanged += _PaddingChanged;
        }
        /// <summary>
        /// Po změně Padding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _PaddingChanged(object sender, EventArgs e)
        {
            ContentDesignSizeInvalidate();
            ItemsPaintedInteractiveInvalidate();
        }
        /// <summary>
        /// Sumární velikost obsahu aktuální. Pokud je null, pak je nevalidní, musí projít validací. Validaci provádí metoda <see cref="ContentDesignSizeCheckValidity(bool)"/>,
        /// kterou volá get property <see cref="ContentDesignSize"/> a je volána i před vykreslením.
        /// </summary>
        private WinDraw.Size? __CurrentContentDesignSize;
        /// <summary>
        /// Sumární velikost obsahu posledně známá, null = nedefinováno. Slouží k detekci změny.
        /// </summary>
        private WinDraw.Size? __LastContentDesignSize;
        /// <summary>
        /// Metoda zajistí, že velikost <see cref="DxInteractivePanel.ContentDesignSize"/> bude platná (bude odpovídat souhrnu velikosti prvků).
        /// Metoda je volána před každým Draw tohoto objektu.
        /// </summary>
        protected virtual void ContentDesignSizeCheckValidity(bool force = false)
        {
            if (force || !__CurrentContentDesignSize.HasValue)
            {
                var lastSize = __LastContentDesignSize;
                var currentSize = CalculateContentDesignSize();      // Souhrn Items + Padding
                bool isSizeChanged = (!lastSize.HasValue || (lastSize.HasValue && currentSize != lastSize.Value));
                __CurrentContentDesignSize = currentSize;
                if (isSizeChanged)                                   // event => jen po reálné změně hodnoty
                {
                    this._RunContentDesignSizeChanged();
                }
                __LastContentDesignSize = currentSize;
            }
        }
        /// <summary>
        /// Vyvolá <see cref="OnContentDesignSizeChanged(EventArgs)"/> a event <see cref="ContentDesignSizeChanged"/>
        /// </summary>
        private void _RunContentDesignSizeChanged()
        {
            EventArgs args = EventArgs.Empty;
            OnContentDesignSizeChanged(args);
            ContentDesignSizeChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda vyvolaná po změně velikosti <see cref="ContentDesignSize"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnContentDesignSizeChanged(EventArgs args) { }
        /// <summary>
        /// Událost vyvolaná po změně velikosti <see cref="ContentDesignSize"/>.
        /// </summary>
        public event EventHandler ContentDesignSizeChanged;
        #endregion
        #region Virtuální souřadnice - přepočty souřadnic s pomocí hostitelského panelu
        /// <summary>
        /// Přepočte souřadnici bodu z designového pixelu (kde se bod nachází v designovém návrhu) do souřadnice v pixelových koordinátech na this controlu (aplikuje Zoom a posun daný Scrollbary)
        /// </summary>
        /// <param name="designPoint">Souřadnice bodu v design pixelech</param>
        /// <returns></returns>
        public WinDraw.Point GetControlPoint(WinDraw.Point designPoint)
        {
            if (!HasVirtualPanel) return designPoint;
            return VirtualPanel.GetControlPoint(designPoint);
        }
        /// <summary>
        /// Přepočte souřadnici bodu v pixelových koordinátech na this controlu do souřadnice v designovém prostoru (aplikuje posun daný Scrollbary a Zoom)
        /// </summary>
        /// <param name="controlPoint">Souřadnice bodu fyzickém controlu</param>
        /// <returns></returns>
        public WinDraw.Point GetDesignPoint(WinDraw.Point controlPoint)
        {
            if (!HasVirtualPanel) return controlPoint;
            return VirtualPanel.GetDesignPoint(controlPoint);
        }
        /// <summary>
        /// Přepočte souřadnici prostoru z designového pixelu (kde se bod nachází v designovém návrhu) do souřadnice v pixelových koordinátech na this controlu (aplikuje Zoom a posun daný Scrollbary)
        /// </summary>
        /// <param name="designBounds">Souřadnice prostoru v design pixelech</param>
        /// <returns></returns>
        public WinDraw.Rectangle GetControlBounds(WinDraw.Rectangle designBounds)
        {
            if (!HasVirtualPanel) return designBounds;
            return VirtualPanel.GetControlBounds(designBounds);
        }
        /// <summary>
        /// Přepočte souřadnici prostoru v pixelových koordinátech na this controlu do souřadnice v designovém prostoru (aplikuje posun daný Scrollbary a Zoom)
        /// </summary>
        /// <param name="controlBounds">Souřadnice prostoru fyzickém controlu</param>
        /// <returns></returns>
        public WinDraw.Rectangle GetDesignBounds(WinDraw.Rectangle controlBounds)
        {
            if (!HasVirtualPanel) return controlBounds;
            return VirtualPanel.GetDesignBounds(controlBounds);
        }
        /// <summary>
        /// Je voláno po změně virtuálních koordinátů (změna Zoomu, změna velikosti, posun Scrollbarů)
        /// </summary>
        public void VirtualCoordinatesChanged()
        {
            ItemsPaintedInteractiveInvalidate();
            _MousePositionChanged();
        }
        /// <summary>
        /// Obsahuje true, pokud this interaktivní panel je umístěn ve <see cref="DxBufferedGraphicPanel.VirtualPanel"/>
        /// </summary>
        protected bool HasVirtualPanel { get { return this.VirtualPanel != null; } }
        #endregion
        #region Interaktivita
        #region Interaktivita nativní = eventy controlu
        /// <summary>
        /// Inicializace nativních eventů myši
        /// </summary>
        private void _InitInteractivity()
        {
            this.MouseEnter += _MouseEnter;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseLeave += _MouseLeave;
            this.KeyDown += _KeyDown;

            this._MouseDragReset();

            this.CursorTypeMouseDrag = CursorTypes.SizeAll;
            this.CursorTypeMouseOn = CursorTypes.Hand;
            this.CursorTypeMouseFrame = CursorTypes.Cross;
        }
        /// <summary>
        /// Nativní event MouseEnter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseEnter(object sender, EventArgs e)
        {
            var mouseState = _CreateMouseState();
            __CurrentMouseButtons = mouseState.Buttons;
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseMove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseMove(object sender, WinForm.MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            _MouseMove(mouseState);
        }
        /// <summary>
        /// Nativní event MouseDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDown(object sender, WinForm.MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            _MouseDown(mouseState);
        }
        /// <summary>
        /// Nativní event MouseUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseUp(object sender, WinForm.MouseEventArgs e)
        {
            var mouseState = _CreateMouseState();
            switch (__CurrentMouseDragState)
            {
                case MouseDragProcessState.MouseDragItem:
                    _MouseDragEnd(mouseState);
                    break;
                case MouseDragProcessState.MouseFrameArea:
                    _MouseFrameEnd(mouseState);
                    break;
                case MouseDragProcessState.Cancelled:
                    _MouseDragReset();
                    break;
                default:
                    _MouseClickUp(mouseState);
                    break;
            }
            _MouseUpEnd(mouseState);
        }
        /// <summary>
        /// Nativní event MouseLeave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseLeave(object sender, EventArgs e)
        {
            var mouseState = _CreateMouseState(true);
            _MouseMoveNone(mouseState, false);
        }
        /// <summary>
        /// Nativní event KeyDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _KeyDown(object sender, WinForm.KeyEventArgs e)
        {
            if (__CurrentMouseDragState == MouseDragProcessState.MouseDragItem || __CurrentMouseDragState == MouseDragProcessState.MouseFrameArea)
                _MouseDragKeyDown(e);
        }
        /// <summary>
        /// Metoda je volána zvenku, z <see cref="DxVirtualPanel"/>, po změně virtuální souřadnice = posunutí pomocí ScrollBarů.
        /// Nedošlo tedy k pohybu myši, ale přesto se její pozice změnila - přnejmenším designová pozice.
        /// V této metodě nasimulujeme "pohyb myši" i bez jejího pohybu...
        /// </summary>
        private void _MousePositionChanged()
        {
            var mouseState = _CreateMouseState();
            _MouseMove(mouseState);
        }
        #endregion
        #region Získání informace o pozici myši, o prvku pod myší a o adrese pod myší
        /// <summary>
        /// Vytvoří a korektně naplní objekt <see cref="MouseState"/> a vrátí jej
        /// </summary>
        /// <returns></returns>
        private MouseState _CreateMouseState(bool? isLeave = null)
        {
            var mouseState = MouseState.CreateCurrent(this);
            _RefreshMouseState(mouseState);
            return mouseState;
        }
        /// <summary>
        /// Znovu najde a naplní do daného objektu <see cref="MouseState"/> nynější 
        /// interaktivní prvek <see cref="MouseState.InteractiveItem"/>
        /// podle aktuální pozice myši v prvku uložené.
        /// </summary>
        /// <returns></returns>
        private void _RefreshMouseState(MouseState mouseState)
        {
            if (mouseState.IsOnControl)
            {
                mouseState.InteractiveItem = _GetMouseItem(mouseState);
            }
        }
        /// <summary>
        /// Najde nejvyšší aktivní prvek pro danou pozici myši
        /// </summary>
        /// <param name="mouseState"></param>
        /// <returns></returns>
        private IInteractiveItem _GetMouseItem(MouseState mouseState)
        {
            WinDraw.Point controlPoint = mouseState.LocationControl;
            WinDraw.Point designPoint = mouseState.LocationDesign;
            var items = ItemsInteractive;
            if (items != null)
            {
                int count = items.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (items[i].IsActiveOnPoint(controlPoint, designPoint))
                        return items[i];
                }
            }
            return null;
        }
        #endregion
        #region Interaktivita logicky řízená
        /// <summary>
        /// Pohyb myši
        /// </summary>
        private void _MouseMove(MouseState mouseState)
        {
            bool lastNone = (__CurrentMouseButtons == WinForm.MouseButtons.None);
            bool currNone = (mouseState.Buttons == WinForm.MouseButtons.None);

            if (lastNone && currNone)
            {   // Stále pohyb bez stisknutého tlačítka:
                _MouseMoveNone(mouseState);
            }
            else if (lastNone && !currNone)
            {   // Dříve bez tlačítka, nyní s tlačítkem (minuli jsme MouseDown):
                _MouseDown(mouseState);
                _MouseDragMove(mouseState);
            }
            else if (!currNone)
            {   // Nyní s tlačítkem
                _MouseDragMove(mouseState);
            }
        }
        /// <summary>
        /// Pohyb myši bez stisknutého tlačítka
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseMoveNone(MouseState mouseState)
        {
            _MouseMoveNone(mouseState, true);
        }
        /// <summary>
        /// Pohyb myši bez stisknutého tlačítka
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="isOnControl"></param>
        private void _MouseMoveNone(MouseState mouseState, bool isOnControl)
        {
            bool isChange = _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, DxInteractiveState.HasMouse, isOnControl);
            bool useMouseTrack = this.UseMouseTrack;
            if (useMouseTrack || isChange)
                this.Draw();
        }
        /// <summary>
        /// Stisk tlačítka myši
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDown(MouseState mouseState)
        {
            var state = ((mouseState.Buttons == WinForm.MouseButtons.Left) ? DxInteractiveState.MouseLeftDown :
                         (mouseState.Buttons == WinForm.MouseButtons.Right) ? DxInteractiveState.MouseRightDown : DxInteractiveState.HasMouse);
            _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, state, true);
            __CurrentMouseDownState = mouseState;
            __CurrentMouseButtons = mouseState.Buttons;
            _MouseDragDown(mouseState, mouseState.InteractiveItem);

            this.Draw();
        }
        /// <summary>
        /// Uvolnění tlačítka myši, nikoli v režimu MouseDrag = provádí se MouseClick, pokud máme nalezený Item nebo na ploše Area
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseClickUp(MouseState mouseState)
        {
            // Odlišení kliknutí nebo doubleclicku (zde ale nejsme v režimu DragAndDrop, to si řeší _MouseUp => _MouseDragEnd):
            var currentItem = __CurrentMouseItem;
            if (currentItem is null)
                _MouseAreaClick(mouseState);
            else
                _MouseItemClick(mouseState, currentItem);

            // Řešení MouseUp
            __PreviousMouseDownState = __CurrentMouseDownState;      // Aktuální stav myši odzálohuji do Previous, kvůli případnému doubleclicku

            // Ukončení Mouse DragAndDrop, které ani nezačalo (proto jsme tady):
            _MouseDragReset();
        }
        /// <summary>
        /// Po provedení MouseUp jak v režimu Click, tak i Drag.
        /// Najde prvek aktuálně pod myší se nacházející a zajistí <see cref="_MouseMoveCurrentExchange(MouseState, IInteractiveItem, DxInteractiveState, bool)"/> a <see cref="Draw()"/>.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseUpEnd(MouseState mouseState)
        {
            __CurrentMouseDownState = null;                          // Aktuálně nemáme myš Down
            __CurrentMouseButtons = WinForm.MouseButtons.None;       // Ani žádný button

            // Znovu najdeme prvek pod myší, ale nehledáme Cell:
            _RefreshMouseState(mouseState);
            _MouseMoveCurrentExchange(mouseState, mouseState.InteractiveItem, DxInteractiveState.HasMouse, mouseState.IsOnControl);

            // Vykreslíme:
            this.Draw();
        }
        /// <summary>
        /// Dokončení myšokliku = uvolnění tlačítka myši, když nebyl proces Drag, a pod myší je prvek.
        /// Zde se detekuje Click/DoubleClick.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="currentItem"></param>
        private void _MouseItemClick(MouseState mouseState, IInteractiveItem currentItem)
        {
            // Click se volá v době MouseUp, ale v procesu Click nás zajímá mj. tlačítka myši v době MouseDown,
            //  proto do eventu posílám objekt __CurrentMouseDownState (stav myši v době MouseDown) a nikoli currentItem (ten už má Buttons = None):
            _RunInteractiveItemClick(new InteractiveItemEventArgs(currentItem, __CurrentMouseDownState));

            // Jaký stav DxInteractiveState vepíšu do prvku currentItem?
            //  Pokud myš je stále nad tímto prvkem (poznám podle dat v mouseState), tal HasMouse, jinak Enabled:
            bool isMouseOverItem = (mouseState.IsOnControl && mouseState.InteractiveItem != null && Object.ReferenceEquals(mouseState.InteractiveItem, currentItem));
            currentItem.InteractiveState = (!isMouseOverItem ? DxInteractiveState.Enabled : DxInteractiveState.HasMouse);
        }
        /// <summary>
        /// Dokončení myšokliku = uvolnění tlačítka myši, když nebyl proces Drag, a pod myší není žádný prvek.
        /// Zde se detekuje Click/DoubleClick.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseAreaClick(MouseState mouseState)
        {
            // Click se volá v době MouseUp, ale v procesu Click nás zajímá mj. tlačítka myši v době MouseDown,
            //  proto do eventu posílám objekt __CurrentMouseDownState (stav myši v době MouseDown) a nikoli currentItem (ten už má Buttons = None):
            _RunInteractiveAreaClick(new InteractiveItemEventArgs(null, __CurrentMouseDownState));
        }
        /// <summary>
        /// Vyřeší výměnu prvku pod myší (dosavadní prvek je v instanční proměnné <see cref="__CurrentMouseItem"/>,
        /// nový je v parametru <paramref name="currentMouseItem"/>).
        /// Řeší detekci změny, vložení správného interaktivního stavu do <see cref="IInteractiveItem.InteractiveState"/>, 
        /// uložení nového do <see cref="__CurrentMouseItem"/>
        /// a vrací true když jde o změnu.
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="currentMouseItem"></param>
        /// <param name="currentState"></param>
        /// <param name="isOnControl"></param>
        /// <returns></returns>
        private bool _MouseMoveCurrentExchange(MouseState mouseState, IInteractiveItem currentMouseItem, DxInteractiveState currentState, bool isOnControl)
        {
            // Pozice myši nad controlem:
            bool lastOnControl = __MouseIsOnControl;
            bool changedOnControl = (isOnControl != lastOnControl);
            __MouseIsOnControl = isOnControl;

            IInteractiveItem lastMouseItem = __CurrentMouseItem;
            bool lastExists = (lastMouseItem != null);
            bool currentExists = (currentMouseItem != null);

            if (!lastExists && !currentExists) return changedOnControl;         // Stále mimo prvky

            if (!lastExists && currentExists)
            {   // Ze žádného prvku na nový prvek:
                currentMouseItem.InteractiveState = currentState;
                __CurrentMouseItem = currentMouseItem;
                _RunInteractiveItemMouseEnter(new InteractiveItemEventArgs(currentMouseItem, mouseState));
                return true;
            }

            if (lastExists && !currentExists)
            {   // Z dosavadního prvku na žádný prvek:
                lastMouseItem.InteractiveState = DxInteractiveState.Enabled;
                _RunInteractiveItemMouseLeave(new InteractiveItemEventArgs(lastMouseItem, mouseState));
                __CurrentMouseItem = null;
                return true;
            }

            // Z dosavadních podmínek je jisté, že máme oba prvky (lastExists && currentExists).
            // Pokud jsou stejné, pohybujeme se nad stále týmž prvkem:
            if (Object.ReferenceEquals(lastMouseItem, currentMouseItem))
            {
                if (currentMouseItem.InteractiveState != currentState)
                {   // Je na něm změna stavu:
                    currentMouseItem.InteractiveState = currentState;
                    return true;
                }
                // Prvek je stejný, ani nemá změnu stavu:
                return changedOnControl;
            }

            // Změna prvku z dosavadního na nový:
            lastMouseItem.InteractiveState = DxInteractiveState.Enabled;
            _RunInteractiveItemMouseLeave(new InteractiveItemEventArgs(lastMouseItem, mouseState));

            currentMouseItem.InteractiveState = currentState;
            __CurrentMouseItem = currentMouseItem;
            _RunInteractiveItemMouseEnter(new InteractiveItemEventArgs(currentMouseItem, mouseState));

            return true;
        }
        /// <summary>
        /// Aktuální tlačítka myši, zde je i None v době pohybu myši bez tlačítek
        /// </summary>
        private WinForm.MouseButtons __CurrentMouseButtons;
        /// <summary>
        /// Stav myši (tlačítko a souřadnice) v okamžiku MouseDown při aktuálním stavu MouseDown, pro řízení MouseDrag
        /// </summary>
        private MouseState __CurrentMouseDownState;
        /// <summary>
        /// Stav myši v předchozím MouseDown, pro detekci DoubleClick
        /// </summary>
        private MouseState __PreviousMouseDownState;
        /// <summary>
        /// Aktuální prvek pod myší, s ním se pracuje
        /// </summary>
        private IInteractiveItem __CurrentMouseItem;
        /// <summary>
        /// Myš se nachází nad Controlem
        /// </summary>
        private bool __MouseIsOnControl;
        #endregion
        #region Interaktivní proces DragAndDrop
        /// <summary>
        /// Volá se v okamžiku MouseDown a slouží k uložení dat pro případné budoucí zahájení procesu Mouse DragAndDrop
        /// </summary>
        /// <param name="mouseState"></param>
        /// <param name="mouseItem"></param>
        private void _MouseDragDown(MouseState mouseState, IInteractiveItem mouseItem)
        {
            __CurrentMouseDragState = MouseDragProcessState.BeginZone;
            __DragBeginMouseState = mouseState;
            __DragVirtualBeginZone = mouseState.LocationControl.CreateRectangleFromCenter(6, 6);
            __DragVirtualCurrentPoint = mouseState;
            __MouseDragCurrentDataItem = mouseItem;
        }
        /// <summary>
        /// Pohyb myši když je stisknuté tlačítko = řeší začátek a průběh DragAndDrop
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragMove(MouseState mouseState)
        {
            if (__CurrentMouseDragState == MouseDragProcessState.BeginZone)
            {   // Jsme v procesu čekání na výraznější pohyb myši = odtrhnutí od bodu MouseDown:
                if (!__DragVirtualBeginZone.HasValue || !__DragVirtualBeginZone.Value.Contains(mouseState.LocationControl))
                {   // Začíná Drag:
                    if (!EnabledDrag)
                    {   // Drag není povolen => rovnou přejdeme do stavu Cancelled:
                        _MouseMoveCurrentExchange(mouseState, null, DxInteractiveState.Enabled, true);
                        __CurrentMouseDragState = MouseDragProcessState.Cancelled;
                        this.Draw();
                    }
                    else if (__MouseDragCurrentDataItem != null)
                    {   // Pokud pod myší je prvek, a pokud se nechá Dragovat:
                        __CurrentMouseDragState = MouseDragProcessState.MouseDragItem;
                        this.CursorType = this.CursorTypeMouseDrag;
                    }
                    else
                    {   // Začal proces Drag, ale není tam prvek = budeme Framovat?
                        __CurrentMouseDragState = MouseDragProcessState.MouseFrameArea;
                        this.CursorType = this.CursorTypeMouseFrame;
                    }
                    __DragVirtualBeginZone = null;
                    __PreviousMouseDownState = null;               // Toto je podklad pro DoubleClick, a ten po Drag neplatí
                }
            }
            // Předchozí blok mohl nastavit aktuální režim __CurrentMouseDragState, proto až nyní následuje switch:
            switch (__CurrentMouseDragState)
            {
                case MouseDragProcessState.MouseDragItem:
                    // Probíhá DragAndDrop: najdeme cílový prvek:
                    __DragVirtualCurrentPoint = mouseState;
                    __MouseDragTargetDataItem = _GetMouseItem(mouseState);
                    this.Draw();
                    break;
                case MouseDragProcessState.MouseFrameArea:
                    // Probíhá MouseFrame: zapíšeme cílový bod a vykreslíme:
                    __DragVirtualCurrentPoint = mouseState;
                    this.Draw();
                    break;
            }
        }
        /// <summary>
        /// Volá se při KeyDown za stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseDragItem"/> nebo <see cref="MouseDragProcessState.MouseFrameArea"/>.
        /// Pokud je stisknut Escape, pak rušíme Drag = nastavíme <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.Cancelled"/> a vyvoláme Draw.
        /// </summary>
        /// <param name="e"></param>
        private void _MouseDragKeyDown(WinForm.KeyEventArgs e)
        {
            if (e.KeyCode == WinForm.Keys.Escape &&
                (__CurrentMouseDragState == MouseDragProcessState.BeginZone ||
                 __CurrentMouseDragState == MouseDragProcessState.MouseDragItem ||
                 __CurrentMouseDragState == MouseDragProcessState.MouseFrameArea))
            {
                __CurrentMouseDragState = MouseDragProcessState.Cancelled;
                this.CursorType = CursorTypes.Default;
                this.Draw();
            }
        }
        /// <summary>
        /// Volá se při události MouseUp při stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseDragItem"/>,
        /// tedy když reálně probíhá Mouse DragAndDrop a nyní je dokončováno = Drop.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseDragEnd(MouseState mouseState)
        {
            if (EnabledDrag)
            {
                // Znovu najdeme prvek pod aktuální pozicí myši, vyhledáme i Target Cell:
                _RefreshMouseState(mouseState);
                InteractiveDragItemEventArgs args = new InteractiveDragItemEventArgs(__MouseDragCurrentDataItem, __DragBeginMouseState, mouseState);
                _RunInteractiveItemDragAndDropEnd(args);
            }
            _MouseDragReset();
        }
        /// <summary>
        /// Volá se při události MouseUp při stavu <see cref="__CurrentMouseDragState"/> == <see cref="MouseDragProcessState.MouseFrameArea"/>,
        /// tedy když reálně probíhá Mouse Frame a nyní je dokončováno = FrameSelect.
        /// </summary>
        /// <param name="mouseState"></param>
        private void _MouseFrameEnd(MouseState mouseState)
        {
            if (EnabledDrag)
            {

            }

            _MouseDragReset();
        }
        /// <summary>
        /// Resetuje veškeré příznaky procesu DragAndDrop
        /// </summary>
        private void _MouseDragReset()
        {
            __CurrentMouseDragState = MouseDragProcessState.None;
            __DragBeginMouseState = null;
            __DragVirtualBeginZone = null;
            __DragVirtualCurrentPoint = null;
            __MouseDragCurrentDataItem = null;

            this.CursorType = CursorTypes.Default;
            this.Draw();
        }
        private MouseDragProcessState __CurrentMouseDragState;
        /// <summary>
        /// Kompletní stav myši v okamžiku, kdy byl zahájen proces DragAndDrop
        /// </summary>
        private MouseState __DragBeginMouseState;
        private WinDraw.Rectangle? __DragVirtualBeginZone;
        private MouseState __DragVirtualCurrentPoint;
        /// <summary>
        /// Prvek, který byl pod myší když začal proces DragAndDrop a je tedy přesouván na jinou pozici
        /// </summary>
        private IInteractiveItem __MouseDragCurrentDataItem;
        /// <summary>
        /// Prvek, který je nyní pod myší při přesouvání v procesu DragAndDrop, je tedy cílovým prvkem. Nikdy nejde o <see cref="__MouseDragCurrentDataItem"/>.
        /// </summary>
        private IInteractiveItem __MouseDragTargetDataItem;
        /// <summary>
        /// Fyzický stav procesu DragAndDrop
        /// </summary>
        private enum MouseDragProcessState
        {
            None,
            BeginZone,
            MouseDragItem,
            MouseFrameArea,
            Cancelled
        }
        #endregion
        #endregion
        #region Kreslení
        /// <summary>
        /// Tato metoda zajistí nové vykreslení objektu. Používá se namísto Invalidate() !!!
        /// Důvodem je to, že Invalidate() znovu vykreslí obsah bufferu - ale ten obsahuje "stará" data.
        /// Vyvolá událost PaintToBuffer() a pak přenese vykreslený obsah z bufferu do vizuálního controlu.
        /// </summary>
        public override void Draw()
        {
            this.ContentDesignSizeCheckValidity();
            base.Draw();
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
        public override void Draw(WinDraw.Rectangle drawRectangle)
        {
            this.ContentDesignSizeCheckValidity();
            base.Draw(drawRectangle);
        }
        /// <summary>
        /// Systémové kreslení
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnPaintToBuffer(object sender, WinForm.PaintEventArgs e)
        {
            var startTime = DxComponent.LogTimeCurrent;

            // this.OnPaintBackground(e);

            e.Graphics.Clear(this.BackColor);
            var items = ItemsAll;
            if (items != null)
            {
                var mouseState = _CreateMouseState();
                using (PaintDataEventArgs pdea = new PaintDataEventArgs(e, mouseState, this))
                {
                    PaintAllDataItems(pdea, items);
                    PaintDraggedItem(pdea);
                }
            }

            DxComponent.LogAddLineTime($"InteractivePanel.Paint() PaintCount: {LastPaintItemsCount}; Time: {DxComponent.LogTokenTimeMicrosec}; VisibleDesignBounds: {this.VirtualPanel?.VisibleDesignBounds}", startTime);
        }
        /// <summary>
        /// Vykreslí všechny interaktivní prvky v základní vrstvě.
        /// Validuje data v polích <see cref="__ItemsPainted"/> a <see cref="__ItemsInteractive"/>, pokud je toho zapotřebí.
        /// </summary>
        /// <param name="pdea">Argument pro kreslení</param>
        /// <param name="items">Vykreslované prvky</param>
        protected virtual void PaintAllDataItems(PaintDataEventArgs pdea, IEnumerable<IInteractiveItem> items)
        {
            // Bude validace?
            bool doValidate = (__ItemsPainted is null || __ItemsInteractive is null);
            List<IInteractiveItem> itemsPainted = (doValidate ? new List<IInteractiveItem>() : null);
            List<IInteractiveItem> itemsInteractive = (doValidate ? new List<IInteractiveItem>() : null);
            int paintCount = 0;

            // Vytvořím Dictionary, kde Key = ZOrder prvku, a z Dictionary udělám List<KeyValuePair<int = ZOrder, Value = IInteractiveItem[]>> :
            var zOrders = items.CreateDictionaryArray(i => GetZOrder(i)).ToList();             // Seskupím podle ZOrder
            if (zOrders.Count > 1) zOrders.Sort((a, b) => a.Key.CompareTo(b.Key));             // Setřídím podle ZOrder ASC
            foreach (var zOrder in zOrders)
            {   // Jednotlivé hladiny ZOrder:
                foreach (var item in zOrder.Value)
                {
                    bool isPainted = item.Paint(pdea);
                    if (isPainted)
                    {
                        paintCount++;
                        if (doValidate)
                        {
                            itemsPainted.Add(item);
                            if (item.IsInteractive)
                                itemsInteractive.Add(item);
                        }
                    }
                }
            }

            // Byla validace?
            if (doValidate)
            {
                __ItemsPainted = itemsPainted;
                __ItemsInteractive = itemsInteractive;
            }

            // Počet víceméně pro statistiku
            LastPaintItemsCount = paintCount;
        }
        /// <summary>
        /// Vykreslí jediný prvek, který je aktuálně Dragged
        /// </summary>
        /// <param name="pdea">Argument pro kreslení</param>
        protected virtual void PaintDraggedItem(PaintDataEventArgs pdea)
        {
            // Dragged item:
            if (this.__MouseDragCurrentDataItem != null && __CurrentMouseDragState == MouseDragProcessState.MouseDragItem)
            {
                var dragBeginPoint = __DragBeginMouseState.LocationControl;
                var dragCurrentPoint = __DragVirtualCurrentPoint.LocationControl;
                var dragShift = dragCurrentPoint.Sub(dragBeginPoint);
                pdea.MouseDragState = MouseDragState.MouseDragActiveCurrent;
                pdea.MouseDragCurrentBounds = this.__MouseDragCurrentDataItem.DesignBounds.Add(dragShift);
                this.__MouseDragCurrentDataItem.Paint(pdea);
            }
        }
        /// <summary>
        /// Počet prvků, které byly posledně vykresleny (v metodě <see cref="IInteractiveItem.Paint(PaintDataEventArgs)"/> vrátily true).
        /// </summary>
        protected int LastPaintItemsCount { get; set; }
        /// <summary>
        /// Explicitní barva pozadí.
        /// Zadaná bůže používat Alfa kanál (= průhlednost), pak pod touto barvou bude prosvítat barva pozadí dle palety.
        /// Lze zadat null = žádná extra barva, čistě barva dle palety.
        /// </summary>
        public WinDraw.Color? BackColorUser { get { return __BackColorUser; } set { __BackColorUser = value; this.Draw(); } } private WinDraw.Color? __BackColorUser;
        /// <summary>
        /// Používá se MouseTrack? To slouží k vykreslení místa s myší při pohybu myši po controlu. Default = false.
        /// </summary>
        public bool UseMouseTrack { get { return __UseMouseTrack; } set { __UseMouseTrack = value; this.Draw(); } } private bool __UseMouseTrack;
        /// <summary>
        /// Typ kurzoru aktuálně zobrazený
        /// </summary>
        public CursorTypes CursorType { get { return __CursorType; } set { __CursorType = value; this.Cursor = DxComponent.GetCursor(value); } } private CursorTypes __CursorType;
        /// <summary>
        /// Typ kurzoru, který bude aktivován po najetí myší na aktivní prvek
        /// </summary>
        public CursorTypes CursorTypeMouseOn { get { return __CursorTypeMouseOn; } set { __CursorTypeMouseOn = value; } } private CursorTypes __CursorTypeMouseOn;
        /// <summary>
        /// Typ kurzoru, který bude aktivován v procesu MouseDragDrop pro konkrétní prvek
        /// </summary>
        public CursorTypes CursorTypeMouseDrag { get { return __CursorTypeMouseDrag; } set { __CursorTypeMouseDrag = value; } } private CursorTypes __CursorTypeMouseDrag;
        /// <summary>
        /// Typ kurzoru, který bude aktivován v procesu MouseFrame
        /// </summary>
        public CursorTypes CursorTypeMouseFrame { get { return __CursorTypeMouseFrame; } set { __CursorTypeMouseFrame = value; } } private CursorTypes __CursorTypeMouseFrame;
        /// <summary>
        /// Vrátí ZOrder daného prvku.
        /// </summary>
        /// <param name="item"></param>
        public int GetZOrder(IInteractiveItem item)
        {
            if (item is null) return 0;
            if (GetIsSelected(item)) return 10;
            switch (item.InteractiveState)
            {
                case DxInteractiveState.Disabled: return 0;
                case DxInteractiveState.Enabled: return 1;
                case DxInteractiveState.HasMouse: return 2;
                case DxInteractiveState.MouseLeftDown: return 3;
                case DxInteractiveState.MouseRightDown: return 3;
                case DxInteractiveState.MouseDragging: return 3;
            }
            return 0;
        }
        /// <summary>
        /// Vrátí true, pokud daný prvek je Selected.
        /// </summary>
        /// <param name="item"></param>
        public bool GetIsSelected(IInteractiveItem item)
        {
            if (item is null) return false;
            var selectedItems = this.SelectedItems;
            return (selectedItems != null && selectedItems.Length > 0 && selectedItems.Any(i => Object.ReferenceEquals(i, item)));
        }
        /// <summary>
        /// Pole prvků, které jsou aktuálně Selected. Kreslí se do horní vrstvy.
        /// </summary>
        public IInteractiveItem[] SelectedItems { get { return __SelectedItems; } set { __SelectedItems = value; this.Draw(); } } private IInteractiveItem[] __SelectedItems;
        #endregion
        #region Public data a Eventy
        /// <summary>
        /// Je povoleno provádět ClickItem
        /// </summary>
        public bool EnabledClick { get; set; }
        /// <summary>
        /// Je povoleno provádět DragAndDrop
        /// </summary>
        public bool EnabledDrag { get; set; }
        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde není žádný prvek.
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveAreaClick;
        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde není žádný prvek.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveAreaClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveAreaClick(InteractiveItemEventArgs args)
        {
            OnInteractiveAreaClick(args);
            InteractiveAreaClick?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel vstoupil myší do konkrétního prvku
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseEnter;
        /// <summary>
        /// Uživatel vstoupil myší do konkrétního prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveItemMouseEnter(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseEnter(InteractiveItemEventArgs args)
        {
            this.CursorType = this.CursorTypeMouseOn;
            OnInteractiveItemMouseEnter(args);
            InteractiveItemMouseEnter?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel odešel myší z konkrétního prvku
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemMouseLeave;
        /// <summary>
        /// Uživatel odešel myší z konkrétního prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveItemMouseLeave(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemMouseLeave(InteractiveItemEventArgs args)
        {
            this.CursorType = CursorTypes.Default;
            OnInteractiveItemMouseLeave(args);
            InteractiveItemMouseLeave?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde je konkrétní prvek.
        /// </summary>
        public event EventHandler<InteractiveItemEventArgs> InteractiveItemClick;
        /// <summary>
        /// Uživatel kliknul (levou nebo pravou myší) v prostoru Controlu, kde je konkrétní prvek.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveItemClick(InteractiveItemEventArgs args) { }
        private void _RunInteractiveItemClick(InteractiveItemEventArgs args)
        {
            OnInteractiveItemClick(args);
            InteractiveItemClick?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel provedl DragAndDrop proces, nyní právě ukončil proces.
        /// </summary>
        public event EventHandler<InteractiveDragItemEventArgs> InteractiveItemDragAndDropEnd;
        /// <summary>
        /// Uživatel provedl DragAndDrop proces, nyní právě ukončil proces.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnInteractiveItemDragAndDropEnd(InteractiveDragItemEventArgs args) { }
        private void _RunInteractiveItemDragAndDropEnd(InteractiveDragItemEventArgs args)
        {
            OnInteractiveItemDragAndDropEnd(args);
            InteractiveItemDragAndDropEnd?.Invoke(this, args);
        }
        #endregion
    }
    #endregion
    #region DxInteractiveItems : seznam interaktivních prvků
    /// <summary>
    /// Kolekce interaktivních prvků
    /// </summary>
    public class DxInteractiveItems : ChildItems<DxInteractivePanel, IInteractiveItem>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        public DxInteractiveItems(DxInteractivePanel parent)
            : base(parent)
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="capacity"></param>
        public DxInteractiveItems(DxInteractivePanel parent, int capacity)
            : base(parent, capacity)
        { }
    }
    #endregion
    #region interface IInteractiveItem a implementace InteractiveSimpleItem a InteractiveContainerItem


    /// <summary>
    /// Rozhraní definující jeden jakýkoli (interaktivní i non-interaktivní) prvek umístěný v prostoru <see cref="DxInteractivePanel"/>.
    /// Prvky mohou být typu Container i Simple, rozhraní pokrývá oba druhy. Konkrétní potomek pak implementuje to, co potřebuje.
    /// Typicky prvků Simple (=Non-Container) je řádově více než Containerů, a pro práci vyžadují poměrně méně dat: implementuje je jednodušší třída.
    /// </summary>
    public interface IInteractiveItem : IChildOfParent<DxInteractivePanel>
    {
        /// <summary>
        /// Prvek je viditelný
        /// </summary>
        bool IsVisible { get; }
        /// <summary>
        /// Prvek je viditelný
        /// </summary>
        bool IsInteractive { get; }
        /// <summary>
        /// Souřadnice prvku v rámci jeho parenta. Souřadnice je "Design" = je daná designem. Do aktuálního controlu se souřadnice přepočítává.
        /// </summary>
        WinDraw.Rectangle DesignBounds { get; }
        /// <summary>
        /// Interaktivní stav prvku
        /// </summary>
        DxInteractiveState InteractiveState { get; set; }
        /// <summary>
        /// Provede vykreslení objektu.
        /// Pokud objekt není vykreslen
        /// </summary>
        /// <param name="pdea"></param>
        bool Paint(PaintDataEventArgs pdea);
        /// <summary>
        /// Vrátí true, pokud this prvek je aktivní na dané Control nebo Designové souřadnici (objekt si sám vybere, kterou souřadnici bude vyhodnocovat).
        /// </summary>
        /// <param name="controlPoint">Souřadnice fyzická na controlu</param>
        /// <param name="designPoint">Souřadnice designová</param>
        /// <returns></returns>
        bool IsActiveOnPoint(WinDraw.Point controlPoint, WinDraw.Point designPoint);
    }
    #endregion
    #region MouseState : stav myši v rámci interaktivních prvků
    /// <summary>
    /// Stav myši
    /// </summary>
    public class MouseState
    {
        /// <summary>
        /// Vrátí aktuální stav myši pro daný Control
        /// </summary>
        /// <param name="control"></param>
        /// <param name="isLeave"></param>
        /// <returns></returns>
        public static MouseState CreateCurrent(WinForm.Control control, bool? isLeave = null)
        {
            var time = DateTime.Now;
            var locationScreen = WinForm.Control.MousePosition;
            var buttons = WinForm.Control.MouseButtons;
            var modifierKeys = WinForm.Control.ModifierKeys;
            var locationControl = control.PointToClient(locationScreen);

            // Pokud isLeave je true, pak jsme volání z MouseLeave a jsme tedy mimo Control:
            var isOnControl = (isLeave.HasValue && isLeave.Value ? false : control.ClientRectangle.Contains(locationControl));

            var locationDesign = locationControl;
            if (control is DxVirtualPanel virtualControl) locationDesign = virtualControl.GetDesignPoint(locationControl);
            else if (control is DxInteractivePanel interactiveControl) locationDesign = interactiveControl.GetDesignPoint(locationControl);
            
            return new MouseState(time, locationControl, locationDesign, locationScreen, buttons, modifierKeys, isOnControl);
        }
        /// <summary>
        /// Vrátí stav myši pro dané hodnoty
        /// </summary>
        /// <param name="time"></param>
        /// <param name="locationControl">Souřadnice myši v koordinátech controlu (nativní prostor)</param>
        /// <param name="locationDesign">Souřadnice myši v koordinátech virtuálního prostoru vrámci Controlu</param>
        /// <param name="locationScreen">Souřadnice myši v absolutních koordinátech (<see cref="WinForm.Control.MousePosition"/>)</param>
        /// <param name="buttons">Stisknuté buttony</param>
        /// <param name="modifierKeys">Stav kláves Control, Alt, Shift</param>
        /// <param name="isOnControl">true pokud myš se nachází fyzicky nad Controlem</param>
        public MouseState(DateTime time, WinDraw.Point locationControl, WinDraw.Point locationDesign, WinDraw.Point locationScreen, WinForm.MouseButtons buttons, WinForm.Keys modifierKeys, bool isOnControl)
        {
            __Time = time;
            __LocationControl = locationControl;
            __LocationDesign = locationDesign;
            __LocationScreen = locationScreen;
            __Buttons = buttons;
            __ModifierKeys = modifierKeys;
            __IsOnControl = isOnControl;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Buttons: {__Buttons}; LocationNative: {__LocationControl}";
        }
        private DateTime __Time;
        private WinDraw.Point __LocationControl;
        private WinDraw.Point __LocationDesign;
        private WinDraw.Point __LocationScreen;
        private WinForm.MouseButtons __Buttons;
        private WinForm.Keys __ModifierKeys;
        private bool __IsOnControl;
        private IInteractiveItem __InteractiveItem;
        /// <summary>
        /// Čas akce myši; důležité pro případný doubleclick
        /// </summary>
        public DateTime Time { get { return __Time; } }
        /// <summary>
        /// Souřadnice myši v koordinátech controlu (nativní pixely zobrazovacího controlu)
        /// </summary>
        public WinDraw.Point LocationControl { get { return __LocationControl; } }
        /// <summary>
        /// Souřadnice myši v designovém prostoru (přepočtené)
        /// </summary>
        public WinDraw.Point LocationDesign { get { return __LocationDesign; } }
        /// <summary>
        /// Souřadnice myši v koordinátech absolutních (Screen)
        /// </summary>
        public WinDraw.Point LocationScreen { get { return __LocationScreen; } }
        /// <summary>
        /// Stav buttonů myši
        /// </summary>
        public WinForm.MouseButtons Buttons { get { return __Buttons; } }
        /// <summary>
        /// Stav kláves Control, Alt, Shift
        /// </summary>
        public WinForm.Keys ModifierKeys { get { return __ModifierKeys; } }
        /// <summary>
        /// Ukazatel myši se nachází nad controlem?
        /// </summary>
        public bool IsOnControl { get { return __IsOnControl; } }
        /// <summary>
        /// Prvek na pozici myši
        /// </summary>
        public IInteractiveItem InteractiveItem { get { return __InteractiveItem; } set { __InteractiveItem = value; } }
    }
    #endregion
    #region InteractiveItemEventArgs a InteractiveDragItemEventArgs : Třídy argumentů pro eventy
    /// <summary>
    /// Data pro interaktivní události s prvkem <see cref="IInteractiveItem"/> a stavem myši <see cref="MouseState"/>
    /// </summary>
    public class InteractiveItemEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="mouseState"></param>
        public InteractiveItemEventArgs(IInteractiveItem item, MouseState mouseState)
        {
            this.Item = item;
            this.MouseState = mouseState;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Item: {Item}";
        }
        /// <summary>
        /// Prvek
        /// </summary>
        public IInteractiveItem Item { get; private set; }
        /// <summary>
        /// Stav myši
        /// </summary>
        public MouseState MouseState { get; private set; }
    }
    /// <summary>
    /// Data pro interaktivní události DragAndDrop s prvkem <see cref="IInteractiveItem"/> a stavem myši <see cref="BeginMouseState"/>
    /// </summary>
    public class InteractiveDragItemEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="beginMouseState"></param>
        /// <param name="endMouseState"></param>
        public InteractiveDragItemEventArgs(IInteractiveItem item, MouseState beginMouseState, MouseState endMouseState)
        {
            this.Item = item;
            this.BeginMouseState = beginMouseState;
            this.EndMouseState = endMouseState;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Item: {Item}";
        }
        /// <summary>
        /// Prvek
        /// </summary>
        public IInteractiveItem Item { get; private set; }
        /// <summary>
        /// Stav myši na počátku procesu DragAndDrop
        /// </summary>
        public MouseState BeginMouseState { get; private set; }
        /// <summary>
        /// Stav myši v procesu DragAndDrop (průběh nebo konec)
        /// </summary>
        public MouseState EndMouseState { get; private set; }
    }
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
        /// <param name="interactivePanel"></param>
        public PaintDataEventArgs(WinForm.PaintEventArgs e, MouseState mouseState, DxInteractivePanel interactivePanel)
        {
            __Graphics = new WeakReference<WinDraw.Graphics>(e.Graphics);
            __ClipRectangle = e.ClipRectangle;
            __MouseState = mouseState;
            __InteractivePanel = interactivePanel;
            __ClientArea = interactivePanel.ClientArea;
        }
        private WeakReference<WinDraw.Graphics> __Graphics;
        private WinDraw.Rectangle __ClipRectangle;
        private WinDraw.Rectangle? __MouseDragCurrentBounds;
        private MouseDragState __MouseDragState;
        private MouseState __MouseState;
        private DxInteractivePanel __InteractivePanel;
        private WinDraw.Rectangle __ClientArea;
        void IDisposable.Dispose()
        {
            __Graphics = null;
        }
        /// <summary>
        /// Gets the graphics used to paint. <br/>
        /// The System.Drawing.Graphics object used to paint. The System.Drawing.Graphics object provides methods for drawing objects on the display device.
        /// </summary>
        public WinDraw.Graphics Graphics { get { return (__Graphics.TryGetTarget(out var graphics) ? graphics : null); } }
        /// <summary>
        /// Gets the rectangle in which to paint. <br/>
        /// The System.Drawing.Rectangle in which to paint.
        /// </summary>
        public WinDraw.Rectangle ClipRectangle { get { return __ClipRectangle; } }
        /// <summary>
        /// Souřadnice aktivního prvku, kam by byl přesunut v procesu Mouse DragAndDrop, když <see cref="MouseDragState"/> je <see cref="MouseDragState.MouseDragActiveCurrent"/>
        /// </summary>
        public WinDraw.Rectangle? MouseDragCurrentBounds { get { return __MouseDragCurrentBounds; } set { __MouseDragCurrentBounds = value; } }
        /// <summary>
        /// Stav procesu Mouse DragAndDrop pro aktuální vykreslovaný prvek
        /// </summary>
        public MouseDragState MouseDragState { get { return __MouseDragState; } set { __MouseDragState = value; } }
        /// <summary>
        /// Pozice a stav myši
        /// </summary>
        public MouseState MouseState { get { return __MouseState; } }
        /// <summary>
        /// Interaktivní panel, do kterého je kresleno
        /// </summary>
        public DxInteractivePanel InteractivePanel { get { return __InteractivePanel; } }
        /// <summary>
        /// Prostor pro kreslení uvnitř tohoto prvku, s vynecháním aktuálního Borderu. Souřadný systém Controlu.
        /// </summary>
        public WinDraw.Rectangle ClientArea { get { return __ClientArea; } }
    }
    #endregion
    #region Enumy a další
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
    /// <summary>
    /// Typ kurzoru. 
    /// Fyzický kurzor pro konkrétní typ vrátí <see cref="DxComponent.GetCursor(CursorTypes)"/>.
    /// </summary>
    public enum CursorTypes
    {
        /// <summary>
        /// Default
        /// </summary>
        Default,
        /// <summary>
        /// Hand
        /// </summary>
        Hand,
        /// <summary>
        /// Arrow
        /// </summary>
        Arrow,
        /// <summary>
        /// Cross
        /// </summary>
        Cross,
        /// <summary>
        /// IBeam
        /// </summary>
        IBeam,
        /// <summary>
        /// Help
        /// </summary>
        Help,
        /// <summary>
        /// AppStarting
        /// </summary>
        AppStarting,
        /// <summary>
        /// UpArrow
        /// </summary>
        UpArrow,
        /// <summary>
        /// WaitCursor
        /// </summary>
        WaitCursor,
        /// <summary>
        /// HSplit
        /// </summary>
        HSplit,
        /// <summary>
        /// VSplit
        /// </summary>
        VSplit,
        /// <summary>
        /// NoMove2D
        /// </summary>
        NoMove2D,
        /// <summary>
        /// NoMoveHoriz
        /// </summary>
        NoMoveHoriz,
        /// <summary>
        /// NoMoveVert
        /// </summary>
        NoMoveVert,
        /// <summary>
        /// SizeAll
        /// </summary>
        SizeAll,
        /// <summary>
        /// SizeNESW
        /// </summary>
        SizeNESW,
        /// <summary>
        /// SizeNS
        /// </summary>
        SizeNS,
        /// <summary>
        /// SizeNWSE
        /// </summary>
        SizeNWSE,
        /// <summary>
        /// SizeWE
        /// </summary>
        SizeWE,
        /// <summary>
        /// PanEast
        /// </summary>
        PanEast,
        /// <summary>
        /// PanNE
        /// </summary>
        PanNE,
        /// <summary>
        /// PanNorth
        /// </summary>
        PanNorth,
        /// <summary>
        /// PanNW
        /// </summary>
        PanNW,
        /// <summary>
        /// PanSE
        /// </summary>
        PanSE,
        /// <summary>
        /// PanSouth
        /// </summary>
        PanSouth,
        /// <summary>
        /// PanSW
        /// </summary>
        PanSW,
        /// <summary>
        /// PanWest
        /// </summary>
        PanWest,
        /// <summary>
        /// No
        /// </summary>
        No
    }
    #endregion
    #region RectangleExt : Souřadnice prvku snadno ukotvitelné nejen Left a Top (jako Rectangle) ale i Right a Bottom a Center.
    /// <summary>
    /// <see cref="RectangleExt"/>: Souřadnice prvku snadno ukotvitelné nejen Left a Top (jako Rectangle) ale i Right a Bottom a Center.
    /// <para/>
    /// V jednom každém směru (X, Y) může mít zadánu jednu až dvě souřadnice tak, aby bylo možno získat reálnou souřadnici v parent prostoru:
    /// Například: Left a Width, nebo Left a Right, nebo Width a Right, nebo jen Width. Tím se řeší různé ukotvení.
    /// <para/>
    /// Po zadání hodnot lze získat konkrétní souřadnice metodou <see cref="GetBounds"/>
    /// </summary>
    public struct RectangleExt
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="width"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="height"></param>
        /// <param name="bottom"></param>
        public RectangleExt(int? left, int? width, int? right, int? top, int? height, int? bottom)
        {
            Left = left;
            Width = width;
            Right = right;

            Top = top;
            Height = height;
            Bottom = bottom;
        }
        /// <summary>
        /// Souřadnice Left, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k levé hraně.
        /// Pokud je null, pak je vázaný k pravé hraně nebo na střed.
        /// </summary>
        public int? Left { get; set; }
        /// <summary>
        /// Souřadnice Top, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k horní hraně.
        /// Pokud je null, pak je vázaný k dolní hraně nebo na střed.
        /// </summary>
        public int? Top { get; set; }
        /// <summary>
        /// Souřadnice Right, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k pravé hraně.
        /// Pokud je null, pak je vázaný k levé hraně nebo na střed.
        /// </summary>
        public int? Right { get; set; }
        /// <summary>
        /// Souřadnice Bottom, zadaná. 
        /// Pokud má hodnotu, je prvek vázaný k dolní hraně.
        /// Pokud je null, pak je vázaný k horní hraně nebo na střed.
        /// </summary>
        public int? Bottom { get; set; }
        /// <summary>
        /// Pevná šířka, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou šířku.
        /// Pokud je null, pak je vázaný k pravé i levé hraně a má šířku proměnnou.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Pevná výška, zadaná.
        /// Pokud má hodnotu, je má prvek pevnou výšku.
        /// Pokud je null, pak je vázaný k horní i dolní hraně a má výšku proměnnou.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Textem vyjádřený obsah this prvku
        /// </summary>
        public string Text { get { return $"Left: {Left}, Width: {Width}, Right: {Right}, Top: {Top}, Height: {Height}, Bottom: {Bottom}"; } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
        /// <summary>
        /// Metoda vrátí konkrétní souřadnice v daném prostoru parenta.
        /// Při tom jsou akceptovány plovoucí souřadnice.
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <returns></returns>
        public WinDraw.Rectangle GetBounds(WinDraw.Rectangle parentBounds)
        {
            if (!IsValid)
                throw new InvalidOperationException($"Neplatně zadané souřadnice v {nameof(RectangleExt)}: {Text}");

            var rectangleExt = this;
            getBound(Left, Width, Right, parentBounds.Left, parentBounds.Right, out int x, out int w);
            getBound(Top, Height, Bottom, parentBounds.Top, parentBounds.Bottom, out int y, out int h);
            return new WinDraw.Rectangle(x, y, w, h);

            void getBound(int? defB, int? defS, int? defE, int parentB, int parentE, out int begin, out int size)
            {
                bool hasB = defB.HasValue;
                bool hasS = defS.HasValue;
                bool hasE = defE.HasValue;

                if (hasB && hasS && !hasE)
                {   // Mám Begin a Size a nemám End     => standardně jako Rectangle:
                    begin = parentB + defB.Value;
                    size = defS.Value;
                }
                else if (hasB && !hasS && hasE)
                {   // Mám Begin a End a nemám Size     => mám pružnou šířku:
                    begin = parentB + defB.Value;
                    size = parentE - defE.Value - begin;
                }
                else if (!hasB && hasS && hasE)
                {   // Mám Size a End a nemám Begin     => jsem umístěn od konce:
                    int end = parentE - defE.Value;
                    size = defS.Value;
                    begin = end - size;
                }
                else if (!hasB && hasS && !hasE)
                {   // Mám Size a nemám Begin ani End   => jsem umístěn Center:
                    int center = parentB + ((parentE - parentB) / 2);
                    size = defS.Value;
                    begin = center - (size / 2);
                }
                else
                {   // Nesprávné zadání:
                    throw new InvalidOperationException($"Chyba v datech {nameof(RectangleExt)}: {rectangleExt.ToString()}");
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je zcela nezadaný = prázdný.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // Pokud i jen jediná hodnota je zadaná, pak vrátím false = objekt NENÍ prázdný:
                return !(Left.HasValue || Width.HasValue || Right.HasValue || Top.HasValue || Height.HasValue || Bottom.HasValue);
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool IsValid
        {
            get
            {
                return isValid(Left, Width, Right) && isValid(Top, Height, Bottom);

                // Je zadaná sada hodnot platná?
                bool isValid(int? begin, int? size, int? end)
                {
                    bool hasB = begin.HasValue;
                    bool hasS = size.HasValue;
                    bool hasE = end.HasValue;

                    return ((hasB && hasS && !hasE)                  // Mám Begin a Size a nemám End     => standardně jako Rectangle
                         || (hasB && !hasS && hasE)                  // Mám Begin a End a nemám Size     => mám pružnou šířku
                         || (!hasB && hasS && hasE)                  // Mám Size a End a nemám Begin     => jsem umístěn od konce
                         || (!hasB && hasS && !hasE));               // Mám Size a nemám Begin ani End   => jsem umístěn Center
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud this prostor je korektně zadaný, a může mít kladný vnitřní prostor
        /// </summary>
        public bool HasContent
        {
            get
            {
                return IsValid && hasSize(Width) && hasSize(Height);

                // Je daná hodnota kladná nebo null ? (pro Null předpokládáme, že se dopočítá kladné číslo z Parent rozměru)
                bool hasSize(int? size)
                {
                    return (!size.HasValue || (size.HasValue && size.Value > 0));
                }
            }
        }
    }
    #endregion
}
