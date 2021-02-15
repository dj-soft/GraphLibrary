using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TestDevExpress;

namespace Noris.Clients.Win.Components
{
    /*         Rozdělení funkcionality mezi třídy SplitterControl (abstract předek) a SplitterBar:
       SplitterControl (abstraktní předek)
          - řídí fyzickou interaktivitu splitteru (myš, přesunutí, ukončení přesunu)
          - zajišťuje vykreslení splitteru v rámci jeho souřadnic, včetně reakcí na stav myši a na barevné schema, včetně DevEpxress themes
          - obsahuje podporu pro konverzi pozice splitteru na souřadnice objektu
          - má dané souřadnice WorkingBounds, v nichž se pohybuje a vykresluje (Min, Max pozice Splitteru, a pozice v neaktivním směru)
          - vyvolává eventy při změně hodnot a při interaktivitě
       SplitterBar (konkrétní implementace bez navázaných controlů)
          - pouze definuje potřebné abstraktní metody pro zajištění WorkingBounds
       SplitterManager (konkrétní implementace bez navázaných controlů)
          - přidává správu sousedních controlů, které přímo ovládá
    */

    #region class SplitterManager : samostatný funkční SplitterBar s navázanými controly
    /// <summary>
    /// <see cref="SplitterManager"/> : samostatný funkční SplitterBar s navázanými controly
    /// </summary>
    internal class SplitterManager : SplitterControl
    {
        #region Konstruktor, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SplitterManager()
        {
            Initialized = false;                                     // Předek na konci konstruktoru dal true, my hned poté dáme false, a na true nastavíme až na našem konci.
            _ControlsBefore = new EList<Control>();
            _ControlsBefore.CountChanged += _Controls_CountChanged;
            _ControlsAfter = new EList<Control>();
            _ControlsAfter.CountChanged += _Controls_CountChanged;
            _MinimalControlSizeBefore = 25;
            _MinimalControlSizeAfter = 25;
            Initialized = true;
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _ControlsBefore?.Clear();
            _ControlsAfter?.Clear();
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Controly vlevo/nahoře.
        /// Od controlů se odvozuje velikost splitteru v neaktivní dimenzi,
        /// Controly jsou posouvány při posunu splitteru.
        /// </summary>
        public IList<Control> ControlsBefore { get { return this._ControlsBefore; } }
        private EList<Control> _ControlsBefore;
        /// <summary>
        /// Controly vpravo/dole.
        /// Od controlů se odvozuje velikost splitteru v neaktivní dimenzi,
        /// Controly jsou posouvány při posunu splitteru.
        /// </summary>
        public IList<Control> ControlsAfter { get { return this._ControlsAfter; } }
        private EList<Control> _ControlsAfter;
        /// <summary>
        /// Minimální velikost platná pro všechny controly před splitterem (ve směru jeho aktivního posunu).
        /// Omezuje pohyb splitteru.
        /// Defaultní hodnota = 25. Lze nastavit pouze nezáporné hodnoty.
        /// </summary>
        public int MinimalControlSizeBefore { get { return _MinimalControlSizeBefore; } set { _MinimalControlSizeBefore = (value < 0 ? 0 : value); SetValidSplitPosition(null, actions: SplitterSetActions.Default); } }
        private int _MinimalControlSizeBefore;
        /// <summary>
        /// Minimální velikost platná pro všechny controly za splitterem (ve směru jeho aktivního posunu).
        /// Omezuje pohyb splitteru.
        /// Defaultní hodnota = 25. Lze nastavit pouze nezáporné hodnoty.
        /// </summary>
        public int MinimalControlSizeAfter { get { return _MinimalControlSizeAfter; } set { _MinimalControlSizeAfter = (value < 0 ? 0 : value); SetValidSplitPosition(null, actions: SplitterSetActions.Default); } }
        private int _MinimalControlSizeAfter;
        #endregion
        #region Určení souřadnic navázaných Controlů: Before a After podle hodnot Splitteru
        /// <summary>
        /// Po jakékoli změně počtu navázaných controlů (tj. po přidání i po odebrání) - přepočteme souřadnice a překreslíme splitter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Controls_CountChanged(object sender, EventArgs e)
        {
            if (!Initialized) return;
            RecalculateBounds(withInvalidate: true);
            ApplySplitterToControls(true);
        }
        /// <summary>
        /// Zajistí, že jednotlivé Controly (Before, After) budou vizuálně umístěny k this splitteru.
        /// Tedy provede totéž, co provádí interaktivní pohyb splitteru při režimu <see cref="SplitterControl.ActivityMode"/> 
        /// buď <see cref="SplitterControl.SplitterActivityMode.ResizeOnMoving"/> nebo <see cref="SplitterControl.SplitterActivityMode.ResizeAfterMove"/>.
        /// <para/>
        /// Tato metoda ale umístí navázané controly ke splitteru vždy = bez ohledu na nastavený režim <see cref="SplitterControl.ActivityMode"/>.
        /// </summary>
        public void ApplySplitterToControls()
        {
            ApplySplitterToControls(true);
        }
        /// <summary>
        /// Metoda zajistí změnu souřadnic sousedních objektů podle aktuální pozice splitteru.
        /// Proběhne pouze pokud režim aktivity <see cref="SplitterControl.ActivityMode"/> je buď <see cref="SplitterControl.SplitterActivityMode.ResizeOnMoving"/> nebo <see cref="SplitterControl.SplitterActivityMode.ResizeAfterMove"/>, 
        /// anebo pokud je zadáno <paramref name="force"/> true.
        /// </summary>
        /// <param name="force">Provést povinně, bez ohledu na režim <see cref="SplitterControl.ActivityMode"/></param>
        protected override void ApplySplitterToControls(bool force)
        {
            if (!Initialized) return;

            var activityMode = ActivityMode;
            if (!(force || activityMode == SplitterActivityMode.ResizeOnMoving || activityMode == SplitterActivityMode.ResizeAfterMove)) return;

            int position = CurrentSplitPosition;
            int th = SplitThick / 2;
            int end = position - th;
            int begin = position + th;
            using (this.ScopeSuspendParentLayout())
            {
                switch (this.Orientation)
                {
                    case Orientation.Horizontal:
                        SetControlsBoundsBySplitter(ControlsBefore, height: b => end - b.Top);
                        SetControlsBoundsBySplitter(ControlsAfter, top: b => begin, height: b => b.Bottom - begin);
                        break;
                    case Orientation.Vertical:
                        SetControlsBoundsBySplitter(ControlsBefore, width: b => end - b.Left);
                        SetControlsBoundsBySplitter(ControlsAfter, left: b => begin, width: b => b.Right - begin);
                        break;
                }
            }
        }
        /// <summary>
        /// Pro controly v dané kolekci upraví jejich souřadnice <see cref="Control.Bounds"/> s pomocí dodaných funkcí.
        /// Každá funkce má vrátit novou hodnotu pro jednu složku Bounds, na základě stávajících souřadnic controlu.
        /// Funkce smí být null, pak se daná hodnota nemění.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected void SetControlsBoundsBySplitter(IList<Control> controls, Func<Rectangle, int> left = null, Func<Rectangle, int> top = null, Func<Rectangle, int> width = null, Func<Rectangle, int> height = null)
        {
            if (controls == null || controls.Count == 0) return;
            bool isLeft = !(left is null);
            bool isTop = !(top is null);
            bool isWidth = !(width is null);
            bool isHeight = !(height is null);
            foreach (var control in controls)
            {
                if (control is null || control.IsDisposed) continue;
                Rectangle oldBounds = control.Bounds;
                Rectangle newBounds = oldBounds;
                if (isLeft) newBounds.X = left(oldBounds);
                if (isTop) newBounds.Y = top(oldBounds);
                if (isWidth) newBounds.Width = width(oldBounds);
                if (isHeight) newBounds.Height = height(oldBounds);
                if (newBounds != oldBounds)
                    control.Bounds = newBounds;
            }
        }
        #endregion
        #region Určení WorkingBounds pro pohyb Splitteru na základě navázaných Controlů: Before a After
        /// <summary>
        /// V této metodě potomek určí prostor, ve kterém se může pohybovat Splitter.
        /// <para/>
        /// Vrácený prostor má dva významy:
        /// <para/>
        /// a) V první řadě určuje rozsah pohybu Splitteru od-do: např. pro svislý splitter je klíčem hodnota Left a Right vráceného prostoru = odkud a kam může splitter jezdit
        /// (k tomu poznámka: jde o souřadnice vnějšího okraje splitteru, tedy včetně jeho tloušťky: 
        /// pokud tedy X = 0, pak splitter bude mít svůj levý okraj nejméně na pozici 0, a jeho <see cref="SplitterControl.SplitPosition"/> tedy bude o půl <see cref="SplitterControl.SplitThick"/> větší).
        /// Pro vodorovný Splitter je v tomto ohledu klíčová souřadnice Top a Bottom.
        /// <para/>
        /// b) V druhé řadě určuje vrácený prostor velikost Splitteru v "neaktivním" směru: např. pro svislý splitter bude kreslen nahoře od pozice Top dolů, jeho výška bude = Height.
        /// Vodorovný Splitter si pak převezme Left a Width.
        /// <para/>
        /// Metoda je volaná při změně hodnoty nebo orientace nebo tloušťky, a na začátku interaktivního přemísťování pomocí myši.
        /// <para/>
        /// Tato metoda dostává jako parametr maximální možnou velikost = prostor v parentu. Metoda ji může vrátit beze změny, pak Splitter bude "jezdit" v celém parentu bez omezení.
        /// Bázová metoda to tak dělá - vrací beze změny dodaný parametr.
        /// </summary>
        /// <param name="currentArea">Souřadnice ClientArea, ve kterých se může pohybovat Splitter v rámci svého parenta</param>
        /// <returns></returns>
        protected override Rectangle GetCurrentWorkingBounds(Rectangle currentArea)
        {
            Rectangle cwb = currentArea;
            int? l = null;
            int? t = null;
            int? r = null;
            int? b = null;
            int mb = MinimalControlSizeBefore;
            int ma = MinimalControlSizeAfter;
            switch (this.Orientation)
            {   // Vypočítáme Min a Max souřadnice neaktivní i aktivní:
                case Orientation.Horizontal:
                    t = 0;
                    CalculateWorkingBounds(this.ControlsBefore, bounds => bounds.Left, ref l, bounds => bounds.Right, ref r, bounds => { int v = bounds.Top + mb; return (v > t.Value ? v : t.Value); }, ref t);
                    b = cwb.Height;
                    CalculateWorkingBounds(this.ControlsAfter,  bounds => bounds.Left, ref l, bounds => bounds.Right, ref r, bounds => { int v = bounds.Bottom - ma; return (v < b.Value ? v : b.Value); }, ref b);
                    break;
                case Orientation.Vertical:
                    l = 0;
                    CalculateWorkingBounds(this.ControlsBefore, bounds => bounds.Top, ref t, bounds => bounds.Bottom, ref b, bounds => { int v = bounds.Left + mb; return (v > l.Value ? v : l.Value); }, ref l);
                    r = cwb.Width;
                    CalculateWorkingBounds(this.ControlsAfter,  bounds => bounds.Top, ref t, bounds => bounds.Bottom, ref b, bounds => { int v = bounds.Right - ma; return (v < r.Value ? v : r.Value); }, ref r);
                    break;
            }
            if (!l.HasValue) l = cwb.Left;
            if (!t.HasValue) t = cwb.Top;
            if (!r.HasValue) r = cwb.Right;
            if (!b.HasValue) b = cwb.Bottom;
            if (r.Value < l.Value) r = l;
            if (b.Value < t.Value) b = t;
            Rectangle currentWorkingBounds = Rectangle.FromLTRB(l.Value, t.Value, r.Value, b.Value);
            return currentWorkingBounds;
        }
        /// <summary>
        /// Z dodaných controlů najde viditelné controly, načte jejich souřadnice, a s pomocí dodaných selectorů určí agregátní hodnoty.
        /// </summary>
        /// <param name="controls">Pole controlů</param>
        /// <param name="beginSelector">Z Bounds vrátí hodnotu, jejíž Min() bude střádat do ref parametru <paramref name="begin"/>. Typicky vrátí (Top nebo Left) + rezerva</param>
        /// <param name="begin">Střádaný Min() počátek</param>
        /// <param name="endSelector">Z Bounds vrátí hodnotu, jejíž Max() bude střádat do ref parametru <paramref name="end"/>. Typicky vrátí (Bottom nebo Right) - rezerva</param>
        /// <param name="end">Střádaný Max() konec</param>
        /// <param name="valueSelector">Z Bounds vrátí hodnotu, kterou bude průběžně ukládat do ref parametru <paramref name="value"/>. Typicky vrátí Top + MinDistance, nebo Right - MinDistance, atd</param>
        /// <param name="value">Střádaná hodnota Value</param>
        protected void CalculateWorkingBounds(IList<Control> controls, Func<Rectangle, int> beginSelector, ref int? begin, Func<Rectangle, int> endSelector, ref int? end, Func<Rectangle, int> valueSelector, ref int? value)
        {
            if (controls is null || controls.Count == 0) return;
            foreach (Control control in controls)
            {
                if (control is null || !control.IsSetVisible()) continue;
                Rectangle bounds = control.Bounds;
                int b = beginSelector(bounds);
                if (!begin.HasValue || b < begin.Value) begin = b;   // Střádáme Min(Left nebo Top) ze všech sousedních Controlů
                int e = endSelector(bounds);
                if (!end.HasValue || e > end.Value) end = e;         // Střádáme Max(Right nebo Bottom) ze všech sousedních Controlů
                value = valueSelector(bounds);
            }
        }
        #endregion
    }
    #endregion
    #region class SplitterBar : samostatný funkční SplitterBar bez navázaných controlů
    /// <summary>
    /// <see cref="SplitterBar"/> : samostatný funkční SplitterBar bez navázaných controlů
    /// </summary>
    internal class SplitterBar : SplitterControl
    {
        #region Konstruktor, Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SplitterBar()
        {
            Initialized = false;                                     // Předek na konci konstruktoru dal true, my hned poté dáme false, a na true nastavíme až na našem konci.
            _MinimalDistanceBefore = 25;
            _MinimalDistanceAfter = 25;
            Initialized = true;
        }
        #endregion
        #region Umístění splitteru - distance před a za splitterem, pracovní prostor
        /// <summary>
        /// Minimální prostor od začátku Parent controlu do začátku splitteru (ve směru jeho aktivního posunu).
        /// Jde o nejmenší počet pixelů na výšku (pro orientaci Horizontal) nebo na šířku (Vertical), která je před splitterem.
        /// Defaultní hodnota = 0. Lze nastavit pouze nezáporné hodnoty.
        /// </summary>
        public int MinimalDistanceBefore { get { return _MinimalDistanceBefore; } set { _MinimalDistanceBefore = (value < 0 ? 0 : value); SetValidSplitPosition(null, actions: SplitterSetActions.Default); } }
        private int _MinimalDistanceBefore;
        /// <summary>
        /// Minimální prostor od konce splitteru do konce Parent controlu (ve směru jeho aktivního posunu).
        /// Jde o nejmenší počet pixelů na výšku (pro orientaci Horizontal) nebo na šířku (Vertical), která je za splitterem.
        /// Defaultní hodnota = 0. Lze nastavit pouze nezáporné hodnoty.
        /// </summary>
        public int MinimalDistanceAfter { get { return _MinimalDistanceAfter; } set { _MinimalDistanceAfter = (value < 0 ? 0 : value); SetValidSplitPosition(null, actions: SplitterSetActions.Default); } }
        private int _MinimalDistanceAfter;
        /// <summary>
        /// Metoda ze zadaných souřadnic odvodí hodnotu do <see cref="SplitInactiveRange"/>.
        /// Pak vyvolá bázovou metodu, která zajistí odvození hodnot <see cref="SplitterControl.SplitThick"/> a <see cref="SplitterControl.SplitPosition"/>.
        /// <para/>
        /// Tato metoda je volána pouze tehdy, když jsou změněny souřadnice splitteru, a tento má nastaveno <see cref="SplitterControl.AcceptBoundsToSplitter"/> = true.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="actions">Akce prováděné Splitterem, pokud nebude zadáno použije se <see cref="SplitterControl.SplitterSetActions.Default"/>.</param>
        protected override void SetSplitterByBounds(Rectangle bounds, SplitterSetActions? actions = null)
        {
            this.SplitInactiveRange = DetectInactiveRange(bounds, this.SplitInactiveRange);
            base.SetSplitterByBounds(bounds, actions);
        }
        /// <summary>
        /// Metoda vrátí hodnotu do <see cref="SplitInactiveRange"/> podle nově dodaných souřadnic splitteru,
        /// se zohledněním stávajícího prostoru <see cref="SplitInactiveRange"/> (fixace Begin nebo End ke konci parenta)
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="inactiveRange"></param>
        /// <returns></returns>
        protected Range<int> DetectInactiveRange(Rectangle bounds, Range<int> inactiveRange = null)
        {
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            int begin = (isHorizontal ? bounds.X : bounds.Y);                  // Počátek splitteru v neaktivním směru (vodorovný = X, svislý = Y)
            int end = (isHorizontal ? bounds.Right : bounds.Bottom);           // Konec splitteru...
            if (inactiveRange != null)
            {   // Pokud jsme dosud měli nějaký předpis pro SplitInactiveRange, tak zachováme jeho pravidla:
                Size size = this.CurrentParentSize;
                int length = (isHorizontal ? size.Width : size.Height);        // Délka prostoru v parentu v neaktivním směru (vodorovný = Width)
                if (inactiveRange.Begin < 0) begin = begin - length;           // Pokud stávající definice měla počátek záporný = fixovaný na konec parenta, pak i nová definice bude mít počátek fixovaný na konec parenta
                if (inactiveRange.End < 0) end = end - length;                 //  dtto pro definic neaktivního konce
            }
            return new Range<int>(begin, end);
        }
        /// <summary>
        /// Aktuální rozsah povolených hodnot pro <see cref="SplitterControl.SplitPosition"/>, 
        /// daný pouze velikostí parenta ve směru posunu splitteru a hodnotami <see cref="MinimalDistanceBefore"/> a <see cref="MinimalDistanceAfter"/> a šířkou splitteru <see cref="SplitterControl.SplitThick"/>.
        /// Vrací tedy Range, kde Begin = (<see cref="MinimalDistanceBefore"/> + (<see cref="SplitterControl.SplitThick"/> / 2));
        /// a End = <see cref="SplitterControl.CurrentParent"/>.ClientSize.{Width nebo Height} - <see cref="MinimalDistanceAfter"/> - (<see cref="SplitterControl.SplitThick"/> / 2));
        /// <para/>
        /// Tato hodnota vždy vychází z <see cref="MinimalDistanceBefore"/> a <see cref="MinimalDistanceAfter"/> a velikosti parent controlu (neukládá se do lokální proměnné). 
        /// Po změně velikosti parent controlu se tato hodnota přiměřeně změní.
        /// </summary>
        public Range<int> SplitPositionRange
        {
            get
            {
                var parentSize = this.CurrentParentSize;
                int size = (Orientation == Orientation.Horizontal ? parentSize.Height : parentSize.Width);
                int thick = 0; // SplitThick / 2;
                int begin = Align(MinimalDistanceBefore + thick, 0, size);
                int end = Align(size - MinimalDistanceAfter - thick, begin, size);
                return new Range<int>(begin, end);
            }
            set
            {
                int minBefore = 0;
                int minAfter = 0;
                if (value != null)
                {
                    var parentSize = this.CurrentParentSize;
                    int size = (Orientation == Orientation.Horizontal ? parentSize.Height : parentSize.Width);
                    int thick = 0; // SplitThick / 2;
                    minBefore = Align(value.Begin - thick, 0, size);
                    minAfter = Align(size - value.End + thick, 0, size);
                }
                _MinimalDistanceBefore = minBefore;
                _MinimalDistanceAfter = minAfter;
                SetValidSplitPosition(null, actions: SplitterSetActions.Default);
            }
        }
        /// <summary>
        /// Rozsah souřadnic splitteru v neaktivním směru (horizontální splitter tady má souřadnice na ose X, vertikální pak na ose Y).
        /// Pokud je null, pak je splitter vykreslen od počátku do konce parenta.
        /// <para/>
        /// Hodnota <see cref="Range{T}.Begin"/> určuje pozici Left pro vodorovný splitter nebo Top pro svislý splitter:
        /// Pokud je nula nebo kladná, pak jde o souřadnici měřenou od počátku prostoru (např. hodnota 25 pro vodorovný splitter říká, že splitter začne na souřadnici X = 25).
        /// Pokud je ale hodnota záporná, pak jde o souřadnici měřenou od konce prostoru (např. hodnota -300 pro vodorovný splitter a prostor široký 400px říká, že splitter začne na souřadnici X = 100 = (400 - 300)).
        /// <para/>
        /// Hodnota <see cref="Range{T}.End"/> určuje pozici Right pro vodorovný splitter nebo Bottom pro svislý splitter:
        /// Pokud je nula nebo kladná, pak jde o souřadnici měřenou od počátku prostoru (např. hodnota 150 pro vodorovný splitter říká, že splitter skončí na souřadnici X = 150 = Right).
        /// Pokud je ale hodnota záporná, pak jde o souřadnici měřenou od konce prostoru (např. hodnota -25 pro vodorovný splitter a prostor široký 400px říká, že splitter skončí na souřadnici X = 375 = (400 - 25)).
        /// </summary>
        public Range<int> SplitInactiveRange { get; set; } = null;
        /// <summary>
        /// V této metodě potomek určí prostor, ve kterém se může pohybovat Splitter.
        /// <para/>
        /// Vrácený prostor má dva významy:
        /// <para/>
        /// a) V první řadě určuje rozsah pohybu Splitteru od-do: např. pro svislý splitter je klíčem hodnota Left a Right vráceného prostoru = odkud a kam může splitter jezdit
        /// (k tomu poznámka: jde o souřadnice vnějšího okraje splitteru, tedy včetně jeho tloušťky: 
        /// pokud tedy X = 0, pak splitter bude mít svůj levý okraj nejméně na pozici 0, a jeho <see cref="SplitterControl.SplitPosition"/> tedy bude o půl <see cref="SplitterControl.SplitThick"/> větší).
        /// Pro vodorovný Splitter je v tomto ohledu klíčová souřadnice Top a Bottom.
        /// <para/>
        /// b) V druhé řadě určuje vrácený prostor velikost Splitteru v "neaktivním" směru: např. pro svislý splitter bude kreslen nahoře od pozice Top dolů, jeho výška bude = Height.
        /// Vodorovný Splitter si pak převezme Left a Width.
        /// <para/>
        /// Metoda je volaná při změně hodnoty nebo orientace nebo tloušťky, a na začátku interaktivního přemísťování pomocí myši.
        /// <para/>
        /// Tato metoda dostává jako parametr maximální možnou velikost = prostor v parentu. Metoda ji může vrátit beze změny, pak Splitter bude "jezdit" v celém parentu bez omezení.
        /// Bázová metoda to tak dělá - vrací beze změny dodaný parametr.
        /// </summary>
        /// <param name="currentArea">Souřadnice ClientArea, ve kterých se může pohybovat Splitter v rámci svého parenta</param>
        /// <returns></returns>
        protected override Rectangle GetCurrentWorkingBounds(Rectangle currentArea)
        {
            Rectangle cwb = currentArea;
            int l = cwb.Left;
            int t = cwb.Top;
            int r = cwb.Right;
            int b = cwb.Bottom;
            var inactive = SplitInactiveRange;
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    if (inactive != null)
                    {
                        if (inactive.Begin >= 0) l = cwb.Left + inactive.Begin; // Hodnota Begin = nula a kladné  => měřeno od Left
                        else l = cwb.Right + inactive.Begin;                    // Hodnota Begin = záporné        => měřeno od Right
                        if (inactive.End >= 0) r = cwb.Left + inactive.End;     // Hodnota End   = nula a kladné  => měřeno od Left
                        else r = cwb.Right + inactive.End;                      // Hodnota End   = záporné        => měřeno od Right
                    }
                    t = cwb.Top + MinimalDistanceBefore;
                    b = cwb.Bottom - MinimalDistanceAfter;
                    break;
                case Orientation.Vertical:
                    if (inactive != null)
                    {
                        if (inactive.Begin >= 0) t = cwb.Top + inactive.Begin;  // Hodnota Begin = nula a kladné  => měřeno od Top
                        else t = cwb.Bottom + inactive.Begin;                   // Hodnota Begin = záporné        => měřeno od Bottom
                        if (inactive.End >= 0) b = cwb.Top + inactive.End;      // Hodnota End   = nula a kladné  => měřeno od Top
                        else r = cwb.Bottom + inactive.End;                     // Hodnota End   = záporné        => měřeno od Bottom
                    }
                    l = cwb.Left + MinimalDistanceBefore;
                    r = cwb.Right - MinimalDistanceAfter;
                    break;
            }
            Rectangle currentWorkingBounds = Rectangle.FromLTRB(l, t, r, b);
            return currentWorkingBounds;
        }
        #endregion
    }
    #endregion
    #region class SplitterControl : abstract základ pro oddělovač mezi komponentami
    /// <summary>
    /// <see cref="SplitterControl"/> : abstract základ pro oddělovač mezi komponentami.
    /// </summary>
    internal abstract class SplitterControl : Control
    {
        #region Konstruktor, privátní eventhandlery
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SplitterControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            _CursorOrientation = null;
            _Orientation = Orientation.Horizontal;
            _ActivityMode = SplitterActivityMode.ResizeOnMoving;
            _VisualLogoMode = SplitterVisualLogoMode.Allways;        // Viditelnost grafiky = vždy
            _VisualLogoDotsCount = 4;
            _ChangeCursorOnSplitter = true;
            SetCursor();
            base.BackColor = Color.Transparent;
            SplitterColor = SystemColors.ControlDark;
            _SplitterActiveColor = Color.Yellow;
            _SplitterColorByParent = true;
            _SplitThick = 6;                                         // Opsáno z MS Outlook
            _AnchorType = SplitterAnchorType.Begin;
            _SplitterEnabled = true;
            _OnTopMode = SplitterOnTopMode.OnMouseEnter;
            _AcceptBoundsToSplitter = false;
            _CurrentMouseState = MouseState.None;                    // Výchozí stav
            DevExpressSkinEnabled = true;                            // Tady se z aktuálního skinu přečtou barvy a uloží do barev _SkinBackColor a _SkinActiveColor
            Enabled = true;
            Initialized = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Splitter: {Name}; Orientation: {Orientation}, SplitPosition: {SplitPosition}";
        }
        /// <summary>
        /// Hodnota true povoluje práci v instanci.
        /// Obsahuje true po dokončení konstruktoru.
        /// Na začátku Dispose se shodí na false.
        /// </summary>
        public bool Initialized { get; protected set; } = false;
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Initialized = false;
            DevExpressSkinEnabled = false;
            base.Dispose(disposing);
            if (_SolidBrush != null)
            {
                _SolidBrush.Dispose();
                _SolidBrush = null;
            }
        }
        #endregion
        #region Vzhled, kreslení, aktuální barvy, kreslící Brush, kurzor
        /// <summary>
        /// Refresh. 
        /// Je vhodné zavolat po změně souřadnic navázaných controlů, pak si Splitter podle nich určí svoji velikost.
        /// A dále zajistí úpravu souřadnic navázaných objektů podle režimu <see cref="ActivityMode"/>.
        /// </summary>
        public override void Refresh()
        {
            RecalculateBounds();
            ApplySplitterToControls(false);
            base.Refresh();
        }
        /// <summary>
        /// Po změně Enabled
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
        /// <summary>
        /// Po změně barvy pozadí parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Zajistí znovuvykreslení prvku
        /// </summary>
        protected virtual void PaintSplitter()
        {
            if (!Initialized) return;
            PaintEventArgs e = new PaintEventArgs(this.CreateGraphics(), this.ClientRectangle);
            PaintSplitter(e);
        }
        /// <summary>
        /// Provede kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Initialized) return;
            PaintSplitter(e);
        }
        /// <summary>
        /// Vykreslí Splitter
        /// </summary>
        /// <param name="e"></param>
        protected void PaintSplitter(PaintEventArgs e)
        {
            if (!Initialized) return;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    PaintHorizontal(e);
                    break;
                case Orientation.Vertical:
                    PaintVertical(e);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Horizontal
        /// </summary>
        /// <param name="e"></param>
        private void PaintHorizontal(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Height;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height + 1);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 90f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }

            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.Y - 1;
                int d = center.X - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(d + space * q, t, 2, 2));
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Vertical
        /// </summary>
        /// <param name="e"></param>
        private void PaintVertical(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Width;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 0f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }
            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.X - 1;
                int d = center.Y - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(t, d + space * q, 2, 2));
            }
        }
        /// <summary>
        /// Aktuální barva, reaguje na hodnotu <see cref="SplitterColorByParent"/> a na Parenta,
        /// na stav splitteru <see cref="CurrentSplitterState"/> a na zvolené barvy LineColor*
        /// </summary>
        protected Color CurrentColor { get { return GetCurrentColorFrom(this.CurrentColorBase); } }
        /// <summary>
        /// Aktuální základní barva: reaguje na <see cref="SplitterColorByParent"/>, <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterColor"/>
        /// </summary>
        protected Color CurrentColorBase
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinBackColor.HasValue)
                    // Dle skinu:
                    return GetCurrentColorFrom(_DevExpressSkinBackColor.Value);

                if (this.SplitterColorByParent && this.Parent != null)
                    // Dle parenta:
                    return GetCurrentColorFrom(this.Parent.BackColor);

                return _SplitterColor;
            }
        }
        /// <summary>
        /// Aktuální barva pro aktivní splitter: reaguje na <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterActiveColor"/>
        /// </summary>
        protected Color CurrentColorActive
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinActiveColor.HasValue)
                    // Dle skinu:
                    return _DevExpressSkinActiveColor.Value;

                return _SplitterActiveColor;
            }
        }
        /// <summary>
        /// Vrací danou barvu modifikovanou dle aktuálního stavu
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetCurrentColorFrom(Color color)
        {
            color = Color.FromArgb(255, color);
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return GetColorDisable(color);
                case SplitterState.Enabled: return GetColorEnabled(color);
                case SplitterState.Hot: return GetColorActive(color);
                case SplitterState.Down: return GetColorDrag(color);
                case SplitterState.Drag: return GetColorDrag(color);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně počátku (Top/Left)
        /// </summary>
        protected Color GetCurrentColor3DBegin(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.White, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.White, 0.25f);
                case SplitterState.Down: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.Black, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně konce (Bottom/Right)
        /// </summary>
        protected Color GetCurrentColor3DEnd(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.Black, 0.25f);
                case SplitterState.Down: return color.Morph(Color.White, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.White, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro zobrazení grafiky (čtyřtečka)
        /// </summary>
        protected Color CurrentDotColor { get { return CurrentColor.Contrast(64); } }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDisable(Color color) { return color.GrayScale(0.75f); }
        /// <summary>
        /// Vrátí barvu Enabled k barvě dané.
        /// Záleží na <see cref="SplitThick"/>: pokud je 2 (a menší), pak vrací danou barvu lehce kontrastní, aby byl splitter vidět.
        /// Pokud je 3 a více, pak vrací danou barvu beze změn, protože se bude vykreslovat 3D efektem.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorEnabled(Color color) { return (this.SplitThick <= 2 ? color.Contrast(12) : color); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorActive(Color color) { return color.Morph(CurrentColorActive, 0.40f); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDrag(Color color) { return color.Morph(CurrentColorActive, 0.60f); }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentColor"/>
        /// </summary>
        protected SolidBrush CurrentBrush
        {
            get
            {
                SolidBrush brush = SolidBrush;
                brush.Color = CurrentColor;
                return brush;
            }
        }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentDotColor"/>
        /// </summary>
        protected SolidBrush CurrentDotBrush
        {
            get
            {
                SolidBrush brush = SolidBrush;
                brush.Color = CurrentDotColor;
                return brush;
            }
        }
        /// <summary>
        /// Brush k obecnému použití
        /// </summary>
        protected SolidBrush SolidBrush
        {
            get
            {
                if (_SolidBrush == null)
                    _SolidBrush = new SolidBrush(Color.White);       // Barva bude vložena podle potřeby
                return _SolidBrush;
            }
        }
        private SolidBrush _SolidBrush;
        /// <summary>
        /// Má se aktuálně zobrazovat grafika (čtyřtečka) uvnitř Splitteru?
        /// </summary>
        protected bool CurrentShowDots
        {
            get
            {
                var mode = VisualLogoMode;
                switch (CurrentSplitterState)
                {
                    case SplitterState.Disabled: return (mode == SplitterVisualLogoMode.Allways);
                    case SplitterState.Enabled: return (mode == SplitterVisualLogoMode.Allways);
                    case SplitterState.Hot: return (mode == SplitterVisualLogoMode.Allways || mode == SplitterVisualLogoMode.OnMouse);
                    case SplitterState.Down: return (mode == SplitterVisualLogoMode.Allways || mode == SplitterVisualLogoMode.OnMouse);
                    case SplitterState.Drag: return (mode == SplitterVisualLogoMode.Allways || mode == SplitterVisualLogoMode.OnMouse);
                }
                return false;
            }
        }
        /// <summary>
        /// Nastaví typ kurzoru pro this prvek podle aktuální orientace.
        /// </summary>
        /// <param name="force"></param>
        protected void SetCursor(bool force = false)
        {
            System.Windows.Forms.Orientation orientation = _Orientation;
            if (force || !_CursorOrientation.HasValue || _CursorOrientation.Value != orientation)
                this.Cursor = (orientation == System.Windows.Forms.Orientation.Horizontal ? Cursors.HSplit : Cursors.VSplit);
            _CursorOrientation = orientation;
        }
        private System.Windows.Forms.Orientation? _CursorOrientation;
        #endregion
        #region Interaktivita splitteru - reakce Splitteru na akce a pohyby myši
        /// <summary>
        /// Při vstupu myši nad control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this._SplitterMouseEnter();
            CurrentSplitterEnabled = SplitterEnabled;
            if (!CurrentSplitterEnabled) return;
            BringSplitterToFront(true);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.Over;
            ChangeCursor(true);
            PaintSplitter();
        }
        /// <summary>
        /// Při odchodu myši z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.None;
            ChangeCursor(false);
            PaintSplitter();
        }
        /// <summary>
        /// Při stisknutí myši - příprava na možný Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!CurrentSplitterEnabled) return;
            if (e.Button != MouseButtons.Left) return;
            Point point = Control.MousePosition;
            MouseDownAbsolutePoint = point;
            MouseDownWorkingBounds = CurrentWorkingBounds;
            MouseDragAbsoluteSilentZone = new Rectangle(point.X - 2, point.Y - 2, 5, 5);
            MouseDragOriginalSplitPosition = SplitPosition;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Down;
            PaintSplitter();
        }
        /// <summary>
        /// Při pohybu myši - mžná provedeme Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!CurrentSplitterEnabled) return;
            Point point = Control.MousePosition;
            if (CurrentSplitterState == SplitterState.Down) DetectSilentZone(point);          // Pokud je zmáčknutá myš, je stav Down; pokud se pohne o malý kousek, přejde stav do Drag
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragMove(point);    // Ve stavu Drag řídíme přesun splitteru
        }
        /// <summary>
        /// Při zvednutí myši - pokud byl Drag and Drop, pak jej dokončíme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!CurrentSplitterEnabled) return;
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragDone();         // Pokud jsme ve stavu Drag, ukončíme přesun splitteru
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            MouseDragAbsoluteSilentZone = null;
            MouseDragOriginalSplitPosition = null;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Over;
            PaintSplitter();
        }
        /// <summary>
        /// Detekuje pohyb mimo <see cref="MouseDragAbsoluteSilentZone"/>.
        /// Pokud se myš pohybuje uvnitř (anebo pokud SilentZone už není), nic neprovádí.
        /// Pokud je ale SilentZone definovaná a myš se nachází mimo ni, pak SilentZone nuluje a nastaví <see cref="CurrentMouseState"/> = <see cref="MouseState.Drag"/>
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSilentZone(Point absolutePoint)
        {
            if (!MouseDragAbsoluteSilentZone.HasValue) return;
            if (MouseDragAbsoluteSilentZone.Value.Contains(absolutePoint)) return;
            MouseDragAbsoluteSilentZone = null;
            _SplitPositionDragBegin();
            CurrentMouseState = MouseState.Drag;
        }
        /// <summary>
        /// Detekuje pohyb myši ve stavu  <see cref="MouseState.Drag"/>, určuje novou hodnotu pozice a volá event 
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSplitterDragMove(Point absolutePoint)
        {
            if (!MouseDownAbsolutePoint.HasValue) return;
            Point originPoint = MouseDownAbsolutePoint.Value;
            Rectangle workingBounds = MouseDownWorkingBounds.Value;
            int distance = (Orientation == Orientation.Horizontal ? (absolutePoint.Y - originPoint.Y) : (absolutePoint.X - originPoint.X));
            int oldValue = MouseDragOriginalSplitPosition.Value;
            int newValue = oldValue + distance;                                          // Hodnota splitteru požadovaná posunem myši
            SetValidSplitPosition(newValue, useWorkingBounds: workingBounds, actions: SplitterSetActions.None);           // Korigovat danou myšovitou hodnotu, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
            int validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            if (!MouseDragLastSplitPosition.HasValue || MouseDragLastSplitPosition.Value != validValue)
            {
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, validValue);
                _SplitPositionDragMove(args);                                            // Tady voláme event SplitPositionDragMove
                _SplitPositionChanging(args);                                            // Tady voláme event PositionChanging (po reálné změně hodnoty, a event Changing - nikoli Changed)
                DetectSplitterEventsModify(args, ref validValue);
                MouseDragLastSplitPosition = SplitPosition;
                RecalculateBounds(workingBounds);
                if (ActivityMode == SplitterActivityMode.ResizeOnMoving)
                    ApplySplitterToControls(false);                                      // Tady posouváme navázané Controly (= podle režimu ActivityMode a podle hodnoty SplitPosition)
                PaintSplitter();
            }
        }
        /// <summary>
        /// Po dokončení procesu Drag vyvolá event <see cref="SplitPositionChanged"/>.
        /// </summary>
        protected void DetectSplitterDragDone()
        {
            if (!MouseDragOriginalSplitPosition.HasValue || MouseDragOriginalSplitPosition.Value != SplitPosition)
            {
                int oldValue = MouseDragOriginalSplitPosition ?? 0;
                int newValue = SplitPosition;
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, newValue);
                _SplitPositionDragDone(args);
                _SplitPositionChanged(args);
                bool isChanged = DetectSplitterEventsModify(args, ref newValue);
                MouseDragOriginalSplitPosition = SplitPosition;
                if (isChanged)
                    RecalculateBounds(MouseDownWorkingBounds);
                if (ActivityMode == SplitterActivityMode.ResizeAfterMove)
                    ApplySplitterToControls(false);
                PaintSplitter();
            }
        }
        /// <summary>
        /// Metoda zpracuje odpovědi v argumentu <paramref name="args"/>.
        /// Reaguje na Cancel, pak vrátí do <paramref name="validValue"/> původní hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.OldValue"/>;
        /// reaguje na Changed, pak do <paramref name="validValue"/> vloží nově zadanou hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.NewValue"/>;
        /// Pokud takto zaregistruje změnu, tak novou hodnotu vloží do SplitPosition a do Bounds a vrátí true.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="validValue"></param>
        /// <returns></returns>
        protected bool DetectSplitterEventsModify(TEventValueChangeArgs<double> args, ref int validValue)
        {
            bool changed = false;
            if (args.Cancel)
            {
                validValue = (int)args.OldValue;
                changed = true;
            }
            else if (args.Changed)
            {
                validValue = (int)args.NewValue;
                changed = true;
            }
            if (changed)
            {
                SetValidSplitPosition(validValue, useWorkingBounds: MouseDownWorkingBounds, actions: SplitterSetActions.None);     // Korigovat hodnotu dodanou aplikačním eventhandlerem, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
                validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            }
            return changed;
        }
        /// <summary>
        /// Hodnota <see cref="SplitterEnabled"/> zachycená při MouseEnter, po skončení eventu <see cref="SplitterMouseEnter"/>, platná pro aktuální akce myši.
        /// Tzn. pokud při MouseEnter bude v eventu <see cref="SplitterMouseEnter"/> určena hodnota <see cref="SplitterEnabled"/> = false, 
        /// pak až do odchodu myši ze splitteru a do nového příchodu platí tato hodnota.
        /// </summary>
        protected bool CurrentSplitterEnabled { get; set; }
        /// <summary>
        /// Souřadnice bodu, kde byla stisknuta myš - v absolutních koordinátech
        /// </summary>
        protected Point? MouseDownAbsolutePoint { get; set; }
        /// <summary>
        /// Aktuální pracovní souřadnice splitteru <see cref="CurrentWorkingBounds"/>, platné v okamžiku MouseDown. Jindy je null.
        /// </summary>
        protected Rectangle? MouseDownWorkingBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru, kde budeme ignorovat pohyb myši po jejím MouseDown (v absolutních koordinátech).
        /// Tím potlačíme malé pohyby před zahájením Drag.
        /// Pokud je zde null, a v <see cref="MouseDownAbsolutePoint"/> pak už myš opustila výchozí SilentZone a reagujeme na její pohyby.
        /// </summary>
        protected Rectangle? MouseDragAbsoluteSilentZone { get; set; }
        /// <summary>
        /// Počáteční hodnota <see cref="SplitPosition"/> před zahájením Drag
        /// </summary>
        protected int? MouseDragOriginalSplitPosition { get; set; }
        /// <summary>
        /// Předchozí hodnota <see cref="SplitPosition"/> při posledním volání eventu 
        /// </summary>
        protected int? MouseDragLastSplitPosition { get; set; }
        /// <summary>
        /// Aktuální stav myši. Změna hodnoty vyvolá invalidaci = překreslení.
        /// </summary>
        protected MouseState CurrentMouseState { get { return _CurrentMouseState; } set { _CurrentMouseState = value; Invalidate(); } }
        private MouseState _CurrentMouseState;
        /// <summary>
        /// Metoda zajistí změnu kurzoru podle nastavení <see cref="ChangeCursorOnSplitter"/>, dané aktivity a aktuální orientace splitteru.
        /// </summary>
        /// <param name="active"></param>
        protected void ChangeCursor(bool active)
        {
            if (ChangeCursorOnSplitter)
            {
                if (active)
                    this.Cursor = (this.Orientation == Orientation.Horizontal ? Cursors.HSplit : (this.Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.Default));
                else
                    this.Cursor = Cursors.Default;
            }
        }
        /// <summary>
        /// Aktuální stav Splitteru odpovídající stavu Enabled a stavu myši <see cref="CurrentMouseState"/>.
        /// </summary>
        protected SplitterState CurrentSplitterState
        {
            get
            {
                if (!this.Enabled) return SplitterState.Disabled;
                switch (this.CurrentMouseState)
                {
                    case MouseState.None: return SplitterState.Enabled;
                    case MouseState.Over: return SplitterState.Hot;
                    case MouseState.Down: return SplitterState.Down;
                    case MouseState.Drag: return SplitterState.Drag;
                }
                return SplitterState.Enabled;
            }
        }
        /// <summary>
        /// Stavy myši
        /// </summary>
        protected enum MouseState { None, Over, Down, Drag }
        /// <summary>
        /// Stavy prvku
        /// </summary>
        protected enum SplitterState { Enabled, Disabled, Hot, Down, Drag }
        #endregion
        #region Public properties - funkcionalita Splitteru (hodnota, orientace, šířka)
        /// <summary>
        /// Aktuální pozice splitteru = hodnota středového pixelu na ose X nebo Y, podle orientace.
        /// Setování této hodnoty VYVOLÁ event <see cref="SplitPositionChanged"/> a zajistí úpravu souřadnic navázaných objektů podle režimu <see cref="ActivityMode"/>.
        /// </summary>
        public int SplitPosition { get { return (int)Math.Round(_SplitPosition, 0); } set { SetValidSplitPosition(value, actions: SplitterSetActions.Default); } }
        /// <summary>
        /// Pozice splitteru uložená jako Double, slouží pro přesné výpočty pozic při <see cref="AnchorType"/> == <see cref="SplitterAnchorType.Relative"/>,
        /// kdy potřebujeme mít pozici i na desetinná místa.
        /// <para/>
        /// Interaktivní přesouvání vkládá vždy integer číslo, public hodnota <see cref="SplitPosition"/> je čtena jako Math.Round(<see cref="SplitPosition"/>, 0).
        /// Setovat double hodnotu je možno pomocí metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SplitterSetActions)"/>.
        /// </summary>
        private double _SplitPosition;
        /// <summary>
        /// Viditelná šířka splitteru. Nastavuje se automaticky na nejbližší vyšší sudé číslo.
        /// Tento počet pixelů bude vykreslován.
        /// Rozsah hodnot = 0 až 30 px.
        /// Hodnota 0 je přípustná, splitter pak nebude viditelný.
        /// </summary>
        public int SplitThick { get { return this._SplitThick; } set { SetValidSplitPosition(null, value, actions: SplitterSetActions.Silent); } }
        private int _SplitThick;
        /// <summary>
        /// Typ ukotvení splitteru. Default = Begin
        /// </summary>
        public SplitterAnchorType AnchorType { get { return this._AnchorType; } set { _AnchorType = value; } }
        private SplitterAnchorType _AnchorType;
        /// <summary>
        /// Režim udržování splitteru nahoře z pohledu ZOrder.
        /// Pozor: prvek, který se snaží udržet nahoře (metodou <see cref="Control.BringToFront()"/>) se přemísťuje v poli controlů svého parenta směrem k indexu [0].
        /// Pokud nějaká aplikace používá prvky v tomto pořadí například k rozložení na layoutu, pak nesmí být <see cref="OnTopMode"/> nastaveno jinak než na <see cref="SplitterOnTopMode.None"/>, 
        /// protože pak by se prvek i fyzicky přemístil na první pozici v poli Controls a tedy v Layoutu!
        /// </summary>
        public virtual SplitterOnTopMode OnTopMode { get { return this._OnTopMode; } set { _OnTopMode = value; if (value == SplitterOnTopMode.OnParentChanged) BringSplitterToFront(false); } }
        private SplitterOnTopMode _OnTopMode;
        /// <summary>
        /// Orientace splitteru = vodorovná nebo svislá
        /// </summary>
        public Orientation Orientation { get { return this._Orientation; } set { _Orientation = value; SetCursor(); SetValidSplitPosition(null, actions: SplitterSetActions.Silent); } }
        private Orientation _Orientation;
        /// <summary>
        /// Příznak, zda má Splitter reagovat na vložení souřadnic do <see cref="Control.Bounds"/>.
        /// Pokud je true, pak po vložení souřadnic se ze souřadnic odvodí <see cref="SplitPosition"/> a <see cref="SplitThick"/>, a vepíše se do Splitteru.
        /// Default = false: souřadnice splitteru nelze změnit vložením hodnoty do <see cref="Control.Bounds"/>, takový pokus bude ignorován.
        /// </summary>
        public bool AcceptBoundsToSplitter { get { return this._AcceptBoundsToSplitter; } set { _AcceptBoundsToSplitter = value; } }
        private bool _AcceptBoundsToSplitter;
        /// <summary>
        /// Povolení aktivity splitteru.
        /// Vyhodnocuje se při vstupu myši nad Splitter, po proběhnutí eventu <see cref="SplitterMouseEnter"/>.
        /// Pokud je true, je povolen MouseDrag, jinak není.
        /// </summary>
        public bool SplitterEnabled { get { return this._SplitterEnabled; } set { _SplitterEnabled = value; } }
        private bool _SplitterEnabled;
        #endregion
        #region Public properties - vzhled
        /// <summary>
        /// Barva pozadí je vždy Transparent, nemá význam ji setovat
        /// </summary>
        public override Color BackColor { get { return Color.Transparent; } set { Invalidate(); } }
        /// <summary>
        /// Barvu splitteru vždy přebírej z barvy pozadí Parenta.
        /// Default hodnota = true, má přednost před barvou Skinu.
        /// Při souběhu <see cref="DevExpressSkinEnabled"/> = true; a <see cref="SplitterColorByParent"/> = true; bude barva převzata z Parent controlu.
        /// Pokud bude <see cref="SplitterColorByParent"/> = false; a <see cref="DevExpressSkinEnabled"/> = true; pak se barva splitteru bude přebírat ze Skinu.
        /// Pokud budou obě false, pak barva Splitteru bude dána barvou <see cref="SplitterColor"/>.
        /// </summary>
        public bool SplitterColorByParent { get { return _SplitterColorByParent; } set { _SplitterColorByParent = value; Invalidate(); } }
        private bool _SplitterColorByParent;
        /// <summary>
        /// Základní barva splitteru.
        /// Pokud je ale nastaveno <see cref="SplitterColorByParent"/> = true, pak je hodnota <see cref="SplitterColor"/> čtena z Parent.BackColor.
        /// Setování hodnoty je sice interně uložena, ale setovaná hodnota nemá vliv na zobrazení (až do změny nastaveni <see cref="SplitterColorByParent"/> na false).
        /// </summary>
        public Color SplitterColor { get { return CurrentColorBase; } set { _SplitterColor = value; Invalidate(); } }
        private Color _SplitterColor;
        /// <summary>
        /// Barva aktivního splitteru.
        /// Toto je pouze vzdálená cílová barva; reálně má splitter v aktivním stavu barvu základní <see cref="SplitterColor"/>,
        /// jen mírně modifikovanou směrem k této aktivní barvě <see cref="SplitterActiveColor"/>.
        /// </summary>
        public Color SplitterActiveColor { get { return CurrentColorActive; } set { _SplitterActiveColor = value; Invalidate(); } }
        private Color _SplitterActiveColor;
        /// <summary>
        /// Režim aktivity splitteru v průběhu přesouvání myší vzhledem k navázaným sousedním objektům - volání metody <see cref="ApplySplitterToControls(bool)"/>.
        /// Výchozí hodnota je <see cref="SplitterActivityMode.ResizeOnMoving"/> = splitter sám provádí změnu souřadnic navázaných controlů, a to už v průběhu přesouvání myší (=Live).
        /// </summary>
        public SplitterActivityMode ActivityMode { get { return _ActivityMode; } set { _ActivityMode = value; ApplySplitterToControls(false); } }
        private SplitterActivityMode _ActivityMode;
        /// <summary>
        /// Režim zobrazování grafiky (čtyřtečka) uprostřed Splitteru.
        /// Výchozí hodnota je <see cref="SplitterVisualLogoMode.OnMouse"/>
        /// </summary>
        public SplitterVisualLogoMode VisualLogoMode { get { return _VisualLogoMode; } set { _VisualLogoMode = value; Invalidate(); } }
        private SplitterVisualLogoMode _VisualLogoMode;
        /// <summary>
        /// Počet teček zobrazovaných jako grafika ("čtyřtečka").
        /// Default = 4. Platné rozmezí = 0 až 30
        /// </summary>
        public int VisualLogoDotsCount { get { return _VisualLogoDotsCount; } set { _VisualLogoDotsCount = (value < 0 ? 0 : (value > 30 ? 30 : value)); Invalidate(); } }
        private int _VisualLogoDotsCount;
        /// <summary>
        /// Nastavit vhodný kurzor po příchodu myši na splitter?
        /// Default = true
        /// </summary>
        public bool ChangeCursorOnSplitter { get { return _ChangeCursorOnSplitter; } set { _ChangeCursorOnSplitter = value; } }
        private bool _ChangeCursorOnSplitter;
        #endregion
        #region DevExpress - reakce na změnu skinu, akceptování skinu pro vzhled Splitteru
        /// <summary>
        /// Obsahuje true, pokud this splitter je napojen na DevExpress skin.
        /// Výchozí hodnota je true.
        /// </summary>
        public bool DevExpressSkinEnabled
        {
            get { return _DevExpressSkinEnabled; }
            set
            {
                if (_DevExpressSkinEnabled)
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged -= DevExpressSkinChanged;
                _DevExpressSkinEnabled = value;
                if (_DevExpressSkinEnabled)
                {
                    DevExpressSkinLoad();
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpressSkinChanged;
                }
            }
        }
        private bool _DevExpressSkinEnabled;
        /// <summary>
        /// Provede se po změně DevExpress Skinu (event je volán z <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/> : <see cref="DevExpress.LookAndFeel.UserLookAndFeel.StyleChanged"/>)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpressSkinChanged(object sender, EventArgs e)
        {
            DevExpressSkinLoad();
        }
        /// <summary>
        /// Načte aktuální hodnoty DevExpress Skinu do this controlu
        /// </summary>
        private void DevExpressSkinLoad()
        {
            if (this.DevExpressSkinEnabled)
            {
                var skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
                var skin = DevExpress.Skins.SkinManager.Default.GetSkin(DevExpress.Skins.SkinProductId.Common, skinName);
                _DevExpressSkinBackColor = skin.GetSystemColor(SystemColors.ControlLight);
                _DevExpressSkinActiveColor = skin.GetSystemColor(SystemColors.HotTrack);
            }
        }
        /// <summary>
        /// Barva pozadí načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinBackColor;
        /// <summary>
        /// Barva aktivní načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinActiveColor;
        #endregion
        #region Abstraktní věci jsou, když tady nic není. Virtuální jsou ty, které sice jsou, ale stejně nic nedělají. GetWorkingBounds(), ApplySplitterToControls()
        /// <summary>
        /// V této metodě potomek určí prostor, ve kterém se může pohybovat Splitter.
        /// <para/>
        /// Vrácený prostor má dva významy:
        /// <para/>
        /// a) V první řadě určuje rozsah pohybu Splitteru od-do: např. pro svislý splitter je klíčem hodnota Left a Right vráceného prostoru = odkud a kam může splitter jezdit
        /// (k tomu poznámka: jde o souřadnice vnějšího okraje splitteru, tedy včetně jeho tloušťky: 
        /// pokud tedy X = 0, pak splitter bude mít svůj levý okraj nejméně na pozici 0, a jeho <see cref="SplitterControl.SplitPosition"/> tedy bude o půl <see cref="SplitterControl.SplitThick"/> větší).
        /// Pro vodorovný Splitter je v tomto ohledu klíčová souřadnice Top a Bottom.
        /// <para/>
        /// b) V druhé řadě určuje vrácený prostor velikost Splitteru v "neaktivním" směru: např. pro svislý splitter bude kreslen nahoře od pozice Top dolů, jeho výška bude = Height.
        /// Vodorovný Splitter si pak převezme Left a Width.
        /// <para/>
        /// Metoda je volaná při změně hodnoty nebo orientace nebo tloušťky, a na začátku interaktivního přemísťování pomocí myši.
        /// <para/>
        /// Tato metoda dostává jako parametr maximální možnou velikost = prostor v parentu. Metoda ji může vrátit beze změny, pak Splitter bude "jezdit" v celém parentu bez omezení.
        /// Bázová metoda to tak dělá - vrací beze změny dodaný parametr.
        /// </summary>
        /// <param name="currentArea">Souřadnice ClientArea, ve kterých se může pohybovat Splitter v rámci svého parenta</param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentWorkingBounds(Rectangle currentArea) { return currentArea; }
        /// <summary>
        /// Metoda zajistí změnu souřadnic sousedních objektů podle aktuální pozice splitteru.
        /// Metoda je volána po změnách souřadnic Splitteru, kdy je vhodné, aby sousední objekty navazovaly na Splitter.
        /// Bázová třída nedělá nic, ale volá tuto metodu v patřičných situacích.
        /// </summary>
        /// <param name="force">Provést povinně, bez ohledu na režim <see cref="SplitterControl.ActivityMode"/></param>
        protected virtual void ApplySplitterToControls(bool force) { }
        #endregion
        #region Jádro splitteru - vložení hodnoty do splitteru, kontroly, výpočty souřadnic
        /// <summary>
        /// Provede změnu pozice splitteru na zadanou hodnotu <paramref name="splitPosition"/> a/nebo <see cref="SplitThick"/>.
        /// Lze tedy zadat všechny hodnoty najednou a navázané výpočty proběhnou jen jedenkrát.
        /// Všechny tyto hodnoty mají nějaký vliv na pozici a souřadnice splitteru, proto je vhodnější je setovat jedním voláním, které je tedy optimálnější.
        /// Zadanou hodnotu zkontroluje s ohledem na vlastnosti splitteru, uloží hodnotu do <see cref="_SplitPosition"/>.
        /// <para/>
        /// Tato metoda se používá interně při interaktivních pohybech, při zadání limitujících hodnot i jako reakce na vložení hodnoty do property <see cref="SplitPosition"/>.
        /// Touto metodou lze vložit hodnotu <see cref="SplitPosition"/> typu <see cref="Double"/>, což se využívá při změně velikosti parenta a typu kotvy <see cref="SplitterAnchorType.Relative"/>.
        /// Tam by se s hodnotou typu <see cref="Int32"/> nedalo pracovat.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="splitPosition">Nová hodnota <see cref="SplitPosition"/>. Pokud bude NULL, vezme se stávající pozice.</param>
        /// <param name="splitThick">Nová hodnota <see cref="SplitThick"/>, hodnota null = beze změny</param>
        /// <param name="useWorkingBounds">Použít dané souřadnice jako WorkingBounds (=nvyhodnocovat <see cref="CurrentWorkingBounds"/>, ani neukládat do <see cref="LastWorkingBounds"/>)</param>
        /// <param name="actions"></param>
        protected void SetValidSplitPosition(double? splitPosition, int? splitThick = null, Rectangle? useWorkingBounds = null, SplitterSetActions actions = SplitterSetActions.Default)
        {
            if (SetValidSplitPositionInProgress) return;

            try
            {
                SetValidSplitPositionInProgress = true;

                // Nejprve zpracuji explicitně zadanou hodnotu SplitThick, protože ta může mít vliv na algoritmus GetValidSplitPosition():
                bool changedThick = false;
                if (splitThick.HasValue)
                {
                    int oldThick = _SplitThick;
                    int newThick = GetValidSplitThick(splitThick.Value);
                    changedThick = (oldThick != newThick);
                    if (changedThick)
                        _SplitThick = newThick;
                }

                // Změna WorkingBounds:
                bool changedBounds = false;
                Rectangle workingBounds;
                if (useWorkingBounds.HasValue)
                {
                    workingBounds = useWorkingBounds.Value;
                }
                else
                {
                    Rectangle oldWorkingBounds = LastWorkingBounds;
                    Rectangle newWorkingBounds = CurrentWorkingBounds;
                    changedBounds = (newWorkingBounds != oldWorkingBounds);
                    if (changedBounds)
                        LastWorkingBounds = newWorkingBounds;
                    workingBounds = newWorkingBounds;
                }

                // A poté zpracuji Position - tu zpracuji i když by na vstupu byla hodnota null (pak jako požadovanou novou hodnotu budu brát hodnotu současnou),
                //  protože v metodě GetValidSplitPosition() se aplikují veškeré limity pro hodnotu, a ty se mohly změnit => proto může být volána this metoda:
                double oldPosition = _SplitPosition;
                double newPosition = GetValidSplitPosition(splitPosition ?? oldPosition, workingBounds);
                bool changedPosition = (Math.Round(newPosition, 2) != Math.Round(oldPosition, 2));
                if (changedPosition)
                    _SplitPosition = newPosition;

                // Pokud není žádná změna, a není ani požadavek na ForceActions, pak skončíme:
                bool force = (actions.HasFlag(SplitterSetActions.ForceActions));
                if (!(changedThick || changedBounds || changedPosition || force)) return;

                // Nastavit souřadnice podle aktuální hodnoty:
                if (actions.HasFlag(SplitterSetActions.RecalculateBounds)) RecalculateBounds(workingBounds, true);

                // Podle pozice splitteru nastavit pozice řízených controlů (to si vyřeší konkrétní potomek):
                bool withControls = (actions.HasFlag(SplitterSetActions.MoveControlsAlways)
                                 || (actions.HasFlag(SplitterSetActions.MoveControlsByActivityMode) && (ActivityMode == SplitterActivityMode.ResizeOnMoving || ActivityMode == SplitterActivityMode.ResizeAfterMove)));
                if (withControls) ApplySplitterToControls(false);

                if (actions.HasFlag(SplitterSetActions.CallEventChanging)) _SplitPositionChanging(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
                if (actions.HasFlag(SplitterSetActions.CallEventChanged)) _SplitPositionChanged(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
            }
            finally
            {
                SetValidSplitPositionInProgress = false;
            }
        }
        /// <summary>
        /// Metoda ze zadaných souřadnic odvodí hodnoty splitPosition a splitThick a vloží je do this Splitteru.
        /// Pozor: potomek smí metodu přepsat, a z neaktivních souřadnic si může odvodit WorkingBounds, musí ale zavolat base.SetSplitterByBounds() ! Jinak nebude proveden základní výpočet.
        /// Základní výpočet ve třídě <see cref="SplitterControl"/> zajistí určení platné hodnoty <see cref="SplitThick"/> a <see cref="SplitPosition"/>, a jejich vložení do splitteru, 
        /// včetně validace hodnot a případné korekce souřadnic splitetru !
        /// <para/>
        /// Tato metoda je volána pouze tehdy, když jsou změněny souřadnice splitteru, a tento má nastaveno <see cref="SplitterControl.AcceptBoundsToSplitter"/> = true.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="actions">Akce prováděné Splitterem, pokud nebude zadáno použije se <see cref="SplitterSetActions.Default"/>.</param>
        protected virtual void SetSplitterByBounds(Rectangle bounds, SplitterSetActions? actions = null)
        {
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            int splitThick = GetValidSplitThick((isHorizontal ? bounds.Height : bounds.Width));
            int th = splitThick / 2;
            double splitPosition = (isHorizontal ? bounds.Y : bounds.X) + th;
            SetValidSplitPosition(splitPosition, splitThick, null, (actions ?? SplitterSetActions.Default));
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SplitterSetActions)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool SetValidSplitPositionInProgress { get; private set; } = false;
        /// <summary>
        /// Posledně platné pracovní souřadnice Splitteru. K těmto pracovním souřadnicím byly určeny souřadnice Splitteru.
        /// </summary>
        protected Rectangle LastWorkingBounds { get; private set; } = Rectangle.Empty;
        /// <summary>
        /// Metoda vrátí platnou hodnotu pro <see cref="SplitThick"/> pro libovolnou vstupní hodnotu.
        /// </summary>
        /// <param name="splitThick"></param>
        /// <returns></returns>
        protected static int GetValidSplitThick(int splitThick)
        {
            int t = splitThick;
            t = (t < 0 ? 0 : (t > 30 ? 30 : t));
            if ((t % 2) == 1) t++;               // Hodnota nesmí být lichá, to kvůli správnému počítání grafiky. Takže nejbližší vyšší sudá...
            return t;
        }
        /// <summary>
        /// Metoda ověří zadanou požadovanou pozici splitteru a vrátí hodnotu platnou.
        /// Potomek může metodu přepsat a hodnotu kontrolovat jinak.
        /// Na vstupu je požadovaná hodnota <see cref="SplitterControl.SplitPosition"/>
        /// a souřadnice pracovního prostoru, které vygenerovala metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>
        /// </summary>
        /// <param name="splitPosition">Zvenku daná pozice Splitteru, požadavek</param>
        /// <param name="currentWorkingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual double GetValidSplitPosition(double splitPosition, Rectangle currentWorkingBounds)
        {
            Rectangle logicalWorkingBounds = GetLogicalRectangle(currentWorkingBounds);
            double th = (double)SplitThick / 2d;
            double min = 0d;
            double max = (double)MaxSize;
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    min = (double)logicalWorkingBounds.Top + th;
                    max = (double)logicalWorkingBounds.Bottom - th;
                    break;
                case Orientation.Vertical:
                    min = (double)logicalWorkingBounds.Left + th;
                    max = (double)logicalWorkingBounds.Right - th;
                    break;
            }
            return Align(splitPosition, min, max);
        }
        /// <summary>
        /// Aktuální pozice splitteru posunutá o Scroll pozici aktuálního containeru.
        /// Pokud Parent container je AutoScroll, pak se Splitter má vykreslovat na jiných souřadnicích, než odpovídá hodnotě <see cref="SplitPosition"/> = právě o posun AutoScrollu.
        /// </summary>
        protected int CurrentSplitPosition
        {
            get
            {
                int splitPosition = SplitPosition;
                Point offset = CurrentOrigin;
                if (!offset.IsEmpty)
                {
                    switch (_Orientation)
                    {
                        case Orientation.Horizontal: return splitPosition + offset.Y;
                        case Orientation.Vertical: return splitPosition + offset.X;
                    }
                }
                return splitPosition;
            }
        }
        /// <summary>
        /// Vrátí danou hodnotou zarovnanou do daných mezí min-max. Pokud na vstupu je max menší než min, vrátí se min.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        protected static int Align(int value, int min, int max)
        {
            if (value > max) value = max;
            if (value < min) value = min;
            return value;
        }
        /// <summary>
        /// Vrátí zadanou hodnotu <paramref name="value"/> zarovnanou do mezí (<paramref name="min"/> ÷ <paramref name="max"/>), včetně
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        protected static double Align(double value, double min, double max)
        {
            if (value > max) value = max;
            if (value < min) value = min;
            return value;
        }
        /// <summary>
        /// Maximální velikost - použitá v případě, kdy není znám Parent
        /// </summary>
        protected const int MaxSize = 10240;
        #endregion
        #region Eventy, háčky a jejich spouštěče
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitterMouseEnter()"/> a event <see cref="SplitterMouseEnter"/>
        /// </summary>
        private void _SplitterMouseEnter()
        {
            OnSplitterMouseEnter();
            SplitterMouseEnter?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při vstupu myši na splitter. 
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou události <see cref="OnSplitPositionDragBegin()"/> (při zahájení pohybu),
        /// <see cref="OnSplitPositionDragMove"/> (po každém pixelu) a <see cref="OnSplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        protected virtual void OnSplitterMouseEnter() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém vstupu myši na splitter.
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou eventy <see cref="SplitPositionDragBegin"/> (při zahájení pohybu),
        /// <see cref="SplitPositionDragMove"/> (po každém pixelu) a <see cref="SplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitterMouseEnter;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragBegin()"/> a event <see cref="SplitPositionDragBegin"/>
        /// </summary>
        private void _SplitPositionDragBegin()
        {
            OnSplitPositionDragBegin();
            SplitPositionDragBegin?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// </summary>
        protected virtual void OnSplitPositionDragBegin() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// Následovat budou eventy <see cref="SplitPositionChanging"/> (po každém pixelu) a <see cref="SplitPositionChanged"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitPositionDragBegin;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragMove(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragMove"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragMove(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragMove(args);
            SplitPositionDragMove?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragMove(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při interaktivním přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragMove;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragDone(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragDone"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragDone(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragDone(args);
            SplitPositionDragDone?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragDone(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při dokončení interaktivního přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragDone;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanging(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanging"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionChanging(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanging(args);
            SplitPositionChanging?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionChanging(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> v procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanging;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanged(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanged"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionChanged(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanged(args);
            SplitPositionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po dokončení pohybu splitteru = po pohybu a po zvednutí myši.
        /// </summary>
        protected virtual void OnSplitPositionChanged(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> z kódu, a po dokončení procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanged;
        #endregion
        #region Souřadnice jsou věc specifická...   Vkládání souřadnic, konverze souřadnic při AutoScrollu (logické / aktuální)
        /// <summary>
        /// Tudy chodí externí setování souřadnic...
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="specified"></param>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // Po změnách datových hodnot Splitteru vkládáme jeho nové souřadnice v metodě RecalculateBounds() přímo do base metody SetBoundsCore().
            //    (takže vložení souřadnic do splitteru po vložení hodnoty Splitteru NEJDE touto metodou!)
            // Do této metody nám tedy vstupuje řízení pouze po EXTERNÍ změně souřadnic.

            if (this.CurrentControlIsExternal)
            {   // a) Pokud Splitter transferoval svoje projevy do jiného controlu (do jiného Parenta), tedy CurrentControlIsExternal je true, 
                //    pak dané souřadnice vložíme do this Splitteru potichu = bez reakcí = dodané souřadnice nemají vliv na hodnoty Splitteru:
                base.SetBoundsCore(x, y, width, height, specified);            // Pouze vložíme dané souřadnice...
            }
            else if (this.AcceptBoundsToSplitter)
            {   // b) Splitter není transferován: Pak pokud je aktivní příznak AcceptBoundsToSplitter, pak dodané souřadnice zpracujeme do souřadnic i do dat Splitteru:
                base.SetBoundsCore(x, y, width, height, specified);            // Nejprve vložíme souřadnice...
                this.SetSplitterByBounds(new Rectangle(x, y, width, height));  // A pak podle souřadnic nastavíme Splitter
            }
        }
        /// <summary>
        /// Vypočítá správné vnější souřadnice Splitteru a uloží je do base.Bounds; volitelně vyvolá invalidaci = překreslení.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <param name="withInvalidate"></param>
        protected void RecalculateBounds(Rectangle? workingBounds = null, bool withInvalidate = false)
        {
            if (RecalculateBoundsInProgress) return;
            try
            {
                RecalculateBoundsInProgress = true;

                Rectangle bounds = GetCurrentBounds(workingBounds);
                Control control = CurrentControl;
                if (!CurrentControlIsExternal)
                {   // Splitter umisťuje jen sám sebe:
                    if (bounds != this.Bounds)
                        base.SetBoundsCore(bounds.X, bounds.Y, bounds.Width, bounds.Height, BoundsSpecified.All);   // Tato metoda REÁLNĚ nastaví Bounds this controlu.
                }
                else if (control != null)
                {   // Splitter umisťuje svého Parenta, a sebe sama pak umisťuje do něj:
                    if (bounds != control.Bounds)
                        control.Bounds = bounds;                     // control je nějaký externí panel, v němž jsme umístěni = tam musíme přímo nastavovat jeho souřadnice
                    _RegisteredExternalControlBounds = bounds;       // Změny detekuje metoda DetectExternalControlBoundsChanged()...
                    Rectangle innerBounds = new Rectangle(0, 0, bounds.Width, bounds.Height);
                    if (innerBounds != this.Bounds)
                        base.SetBoundsCore(innerBounds.X, innerBounds.Y, innerBounds.Width, innerBounds.Height, BoundsSpecified.All);   // Tato metoda REÁLNĚ nastaví Bounds this controlu.
                }
                if (withInvalidate && Initialized) Invalidate();
            }
            finally
            {
                RecalculateBoundsInProgress = false;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SplitterControl.RecalculateBounds(Rectangle?, bool)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool RecalculateBoundsInProgress { get; private set; } = false;
        /// <summary>
        /// Vrátí aktuální souřadnice prvku (Bounds) pro jeho umístění = nikoli pro jeho vykreslení.
        /// Souřadnice určí na základě pozice splitteru <see cref="SplitterControl.SplitPosition"/> a jeho orientace <see cref="SplitterControl.Orientation"/>, 
        /// jeho šíři <see cref="SplitterControl.SplitThick"/>
        /// a na základě pracovních souřadnic dle parametru <paramref name="workingBounds"/>, viz i metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>.
        /// <para/>
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentBounds(Rectangle? workingBounds = null)
        {
            int sp = CurrentSplitPosition;
            int th = (SplitThick / 2);
            Rectangle cwb = workingBounds ?? CurrentWorkingBounds;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    return new Rectangle(cwb.X, sp - th, cwb.Width, SplitThick);
                case Orientation.Vertical:
                    return new Rectangle(sp - th, cwb.Y, SplitThick, cwb.Height);
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí souřadnice vizuální (akceptující AutoScroll) pro dané souřadnice logické
        /// </summary>
        /// <param name="logicalBounds"></param>
        /// <returns></returns>
        protected Rectangle GetCurrentRectangle(Rectangle logicalBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return logicalBounds;
            return new Rectangle(logicalBounds.X + currentOrigin.X, logicalBounds.Y + currentOrigin.Y, logicalBounds.Width, logicalBounds.Height);
        }
        /// <summary>
        /// Metoda vrátí souřadnice logické (akceptující původní bod 0/0) pro dané souřadnice vizuálně, akceptujíc AutoScroll
        /// </summary>
        /// <param name="currentBounds"></param>
        /// <returns></returns>
        protected Rectangle GetLogicalRectangle(Rectangle currentBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return currentBounds;
            return new Rectangle(currentBounds.X - currentOrigin.X, currentBounds.Y - currentOrigin.Y, currentBounds.Width, currentBounds.Height);
        }
        /// <summary>
        /// Souřadnice bodu 0/0.
        /// On totiž počáteční bod ve WinForm controlech může být posunutý, pokud Parent control je typu <see cref="ScrollableControl"/> s aktivním scrollingem.
        /// </summary>
        protected Point CurrentOrigin
        {
            get
            {
                if (!(CurrentParent is ScrollableControl parent) || !parent.AutoScroll) return Point.Empty;
                return parent.AutoScrollPosition;
            }
        }
        /// <summary>
        /// Obsahuje velikost plochy Parenta, ve které se může pohybovat splitter
        /// </summary>
        protected Size CurrentParentSize
        {
            get
            {
                var parent = this.CurrentParent;
                if (parent is null) return new Size(MaxSize, MaxSize);
                if (parent is ScrollableControl scrollParent)
                    return scrollParent.ClientSize;
                return parent.ClientSize;
            }
        }
        /// <summary>
        /// Aktuální pracovní souřadnice Splitteru. Určuje je potomek ve virtual metodě <see cref="GetCurrentWorkingBounds(Rectangle)"/>.
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        protected Rectangle CurrentWorkingBounds
        {
            get
            {
                Rectangle currentArea = new Rectangle(CurrentOrigin, CurrentParentSize);
                Rectangle currentWorkingBounds = GetCurrentWorkingBounds(currentArea);
                return currentWorkingBounds;
            }
        }
        #endregion
        #region Splitter si hlídá svého parenta, a reaguje na změny rozměru svého parenta, podle toho jak má nastavenou kotvu
        /// <summary>
        /// Po změně Parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            _TransferToParentCheck(true);
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Když si můj parent změní velikost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentParent_ClientSizeChanged(object sender, EventArgs e)
        {
            DetectParentSizeChanged();
        }
        /// <summary>
        /// Detekujeme změnu rozměrů aktuálního parenta, a po změně v ose, po které se pohybujeme 
        /// zavoláme virtual <see cref="OnParentLengthChanged(TEventValueChangeArgs{int})"/> a event <see cref="ParentLengthChanged"/>
        /// </summary>
        protected void DetectParentSizeChanged()
        {
            var parent = CurrentParent;
            if (parent is null) return;          // Pokud nemáme parenta, nebudeme řešit změny jeho velikosti.
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            Size oldSize = _RegisteredParentSize;
            Size newSize = parent.ClientSize;
            int oldValue = (isHorizontal ? oldSize.Height : oldSize.Width);
            int newValue = (isHorizontal ? newSize.Height : newSize.Width);
            if (oldValue != newValue)
            {
                TEventValueChangeArgs<int> args = new TEventValueChangeArgs<int>(EventSource.Code, oldValue, newValue);
                OnParentLengthChanged(args);
                ParentLengthChanged?.Invoke(this, args);
            }
            _RegisteredParentSize = newSize;
        }
        /// <summary>
        /// Virtuální metoda volaná tehdy, když dojde ke změně délky prostoru v parentu v tom směru, ve kterém splitter "jezdí":
        /// Splitter vodorovný jezdí nahoru a dolů, reaguje tedy na změnu Hight; a svislý jezdí vlevo a vpravo a reaguje na změnu Width.
        /// V této metodě se řeší automatické přemístění splitteru, pokud je kotven ke konci prostoru.
        /// <para/>
        /// Tato metoda v bázové třídě <see cref="SplitterControl"/> fyzicky provádí potřebné výpočty.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnParentLengthChanged(TEventValueChangeArgs<int> args)
        {
            double? newPosition = null;
            double oldPosition = _SplitPosition;
            double oldValue = (double)args.OldValue;
            double newValue = (double)args.NewValue;
            switch (this.AnchorType)
            {
                case SplitterAnchorType.Default:
                case SplitterAnchorType.Begin:
                    // Neděláme nic:
                    break;
                case SplitterAnchorType.Relative:
                    // Na stejné relativní pozici: vypočítáme a uložíme hodnotu Double, 
                    //  abychom při příštím Resize parenta mohli vycházet zase z Double hodnoty a neztrácela se nám přesnost relativní pozice 
                    //  (Pokud bychom pracovali s hodnotou splitteru typu Int32, pak by nám nefungovalo nic :-) )
                    newPosition = (oldValue > 1d ? (oldPosition * (newValue / oldValue)) : (newValue / 2d));
                    break;
                case SplitterAnchorType.End:
                    // Ke konci:
                    newPosition = oldPosition + (newValue - oldValue);
                    break;
            }
            if (newPosition.HasValue)
                this.SetValidSplitPosition(newPosition, actions: SplitterSetActions.Default);
        }
        /// <summary>
        /// Event volaný tehdy, když dojde ke změně délky prostoru v parentu v tom směru, ve kterém splitter "jezdí":
        /// Splitter vodorovný jezdí nahoru a dolů, reaguje tedy na změnu Hight; a svislý jezdí vlevo a vpravo a reaguje na změnu Width.
        /// V této metodě se řeší automatické přemístění splitteru, pokud je kotven ke konci prostoru.
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<int>> ParentLengthChanged;
        /// <summary>
        /// Aktuální velikost Parent.ClientSIze
        /// </summary>
        private Size _RegisteredParentSize;
        /// <summary>
        /// Eventhandler volaný tehdy, když se změní rozměry aktuálního transferovaného controlu.
        /// Tedy: Splitter je uzamčen v nadřízeném panelu, souřadnice splitteru se pohybem nemění, ale někdo změní souřadnice toho nadřízeného panelu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentExternalControl_BoundsChanged(object sender, EventArgs e)
        {
            DetectExternalControlBoundsChanged();
        }
        /// <summary>
        /// Metoda prověří, zda došlo ke změně souřadnic aktuálního controlu <see cref="CurrentControl"/>, pokud tento je externí (<see cref="CurrentControlIsExternal"/> = true).
        /// </summary>
        protected void DetectExternalControlBoundsChanged()
        {
            if (!CurrentControlIsExternal) return;                             // Pokud není Externí režim, ignorujeme volání
            if (this.SetValidSplitPositionInProgress) return;                  // Pokud probíhají změny z interních důvodů (Position), pak nereagujeme
            if (this.RecalculateBoundsInProgress) return;                      // Pokud probíhají změny z interních důvodů (Bounds), pak nereagujeme
            if (this.CurrentSplitterState == SplitterState.Drag) return;       // Pokud taháme splitter myší, nejde o externí změnu
            var control = CurrentControl;
            if (control is null) return;                                       // Pokud nemáme control, nebudeme řešit změny jeho velikosti.

            Rectangle oldBounds = _RegisteredExternalControlBounds;
            Rectangle newBounds = control.Bounds;
            if (oldBounds != newBounds)
            {
                TEventValueChangeArgs<Rectangle> args = new TEventValueChangeArgs<Rectangle>(EventSource.Code, oldBounds, newBounds);
                OnExternalControlBoundsChanged(args);
                ExternalControlBoundsChanged?.Invoke(this, args);
            }
            _RegisteredExternalControlBounds = newBounds;
        }
        /// <summary>
        /// Virtuální metoda volaná tehdy, když dojde ke změně souřadnic controlu <see cref="CurrentControl"/>, pokud je externí (<see cref="CurrentControlIsExternal"/> = true).
        /// <para/>
        /// Tato metoda v bázové třídě <see cref="SplitterControl"/> fyzicky neprovádí nic. Lze ji overridovat, není ale nutno volat base metodu.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnExternalControlBoundsChanged(TEventValueChangeArgs<Rectangle> args)
        {
            // V podstatě jde o událost, kdy nějaký aplikační kód vložil souřadnice do Controlu, který reprezentuje Splitter - i když to fyzicky není Splitter, ale je to jeho exkluzivní hostitel.
            // Je to tedy srovnatelná akce, jako nativní metoda this.SetBoundsCore().
            // Zdejší Splitter na SetBoundsCore() nereaguje, nebudeme reagovat ani my.
        }
        /// <summary>
        /// Event volaný tehdy, když dojde ke změně souřadnic controlu <see cref="CurrentControl"/>, pokud je externí (<see cref="CurrentControlIsExternal"/> = true).
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<Rectangle>> ExternalControlBoundsChanged;
        /// <summary>
        /// Aktuální velikost Parent.ClientSIze
        /// </summary>
        private Rectangle _RegisteredExternalControlBounds;
        #endregion
        #region Splitter je rád, když je úplně nahoře v Z-Order
        /// <summary>
        /// Přemístí this splitter nahoru v poli controlů našeho parenta.
        /// Pokud ale je nastaveno <see cref="OnTopMode"/> = <see cref="SplitterOnTopMode.None"/>, pak nic neprovádí.
        /// <para/>
        /// Parametr <paramref name="isMouseEnter"/> říká:
        /// true = metoda je volána z události MouseEnter, je požadováno aby this splitter byl naprosto navrchu;
        /// false = metoda je volána z události Parent.ControlAdded, je požadováno aby nad this splitterem byly už pouze jiné splittery.
        /// <para/>
        /// Metoda obecně reaguje podle hodnoty v <see cref="OnTopMode"/>.
        /// </summary>
        /// <param name="isMouseEnter">Informace: true = voláno z MouseEnter, false = volánoz Parent.ControlAdded</param>
        protected void BringSplitterToFront(bool isMouseEnter)
        {
            // Podle režimu OnTopMode: 
            //  pokud je None, pak neděláme nikdy nic. Anebo pokud je režim OnMouseEnter, ale aktuálně nejsme voláni z akce OnMouseEnter, pak nic neděláme:
            var onTopMode = this.OnTopMode;
            if (onTopMode == SplitterOnTopMode.None || (onTopMode == SplitterOnTopMode.OnMouseEnter && !isMouseEnter)) return;

            var thisControl = this.CurrentControl;         // Namísto "this" pracujeme s CurrentControl, což je buď nativní this, anebo některý z parentů
            if (thisControl is null) return;

            var allControls = AllControls;
            if (allControls.Count <= 1) return;            // Pokud nejsou žádné prvky (=blbost, jsem tu já), anebo je jen jeden prvek (to jsem já), pak není co řešit...
            int index = allControls.FindIndex(c => object.ReferenceEquals(c, thisControl));
            if (index <= 0) return;                        // Pokud já jsem na indexu [0] (tj. úplně nahoře), anebo tam nejsem vůbec (blbost), pak není co řešit

            // Já nejsem na pozici [0] = někdo je ještě nade mnou:
            bool bringToFront = false;
            if (isMouseEnter)
                // Máme být úplně navrchu:
                bringToFront = true;
            else
            {   // Nad námi smí být pouze jiné splittery:
                for (int i = 0; i < index && !bringToFront; i++)
                {
                    if (!(allControls[i] is SplitterControl))
                        bringToFront = true;
                }
            }

            // Dáme se (=Splitter = CurrentControl) nahoru:
            if (bringToFront)
                thisControl.BringToFront();
        }
        /// <summary>
        /// Pole Child controlů mého Parenta = "moji sourozenci včetně mě".
        /// Pokud ještě nemám parenta, pak toto pole obsahuje jen jeden prvek a to jsem já.
        /// Pokud má vrácené pole více prvků, pak někde v něm budu i já = <see cref="CurrentParent"/> :-).
        /// <para/>
        /// Index na pozici [0] je úplně nahoře nade všemi, postupně jsou prvky směrem dolů...
        /// Pozici aktuální prvku 
        /// </summary>
        protected List<Control> AllControls
        {
            get
            {
                var parent = this.CurrentParent;
                if (parent == null) return new List<Control> { this };
                return parent.Controls.OfType<Control>().ToList();
            }
        }
        /// <summary>
        /// Když si můj parent přidá jakýkoli nový control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentParent_ControlAdded(object sender, ControlEventArgs e)
        {
            BringSplitterToFront(false);
        }
        #endregion
        #region Specifikum Greenu: Splitter je někdy umístěn na samostatném panelu (vertikální splitter), ale my chceme, aby právě tento Parent "si hrál na Splittera" = aby panel hýbal okolními panely
        /// <summary>
        /// Specifikum Greenu: 
        /// Splitter je někdy umístěn na samostatném panelu (vertikální splitter), ale my chceme, aby tento Parent panel hýbal okolními panely
        /// <para/>
        /// Tato proměnná dovoluje zapnout uvedené chování. Vložíme funkci do <see cref="TransferToParentSelector"/> a nastavme true do <see cref="TransferToParentEnabled"/>,
        /// a splitter bude pohybovat vhodným parentem.
        /// </summary>
        public bool TransferToParentEnabled { get { return _TransferToParentEnabled; } set { _TransferToParentEnabled = value; _TransferToParentInvalidate(); } }
        private bool _TransferToParentEnabled;
        /// <summary>
        /// Specifikum Greenu: 
        /// Splitter je někdy umístěn na samostatném panelu (vertikální splitter), ale my chceme, aby tento Parent panel hýbal okolními panely
        /// <para/>
        /// Do této property se ukládá metoda, která dovoluje určit, který konkrétní Parent bude ovlivněn splitterem.
        /// Funkce je vyvolána v případě potřeby, dostává postupně jednotlivé controly počínaje od this splitteru přes jeho Parenty, otestuje je ,
        /// a pokud vrátí true, pak splitter bude používat daný control jako pohyblivý Splitter.
        /// <para/>
        /// Metoda je volána do té doby, než vrátí patřičný výsledek (<see cref="SplitterParentSelectorMode.Accept"/>), pak volána nebude.
        /// Je volána při změně Parenta a při potřebě znalosti Parenta.
        /// <para/>
        /// Postup: Vložíme funkci do <see cref="TransferToParentSelector"/> a nastavme true do <see cref="TransferToParentEnabled"/>,
        /// a splitter bude pohybovat vhodným parentem.
        /// </summary>
        public Func<Control, SplitterParentSelectorMode> TransferToParentSelector { get { return _TransferToParentSelector; } set { _TransferToParentSelector = value; _TransferToParentInvalidate(); } }
        private Func<Control, SplitterParentSelectorMode> _TransferToParentSelector;
        /// <summary>
        /// Aktuální control, který se používá namísto "this".
        /// Pokud je aktivní subsystém "TransferToParent", pak je zde některý z našich Parentů, který hraje roli Splitteru.
        /// V neaktivním subsystému "TransferToParent" je zde this.
        /// <para/>
        /// Pozor, výjimečně zde může být Null: to je tehdy, když splitter deklaruje, že může být transferován do některého Parenta, ale dosud není určeno kam.
        /// </summary>
        protected Control CurrentControl { get { _TransferToParentCheck(); return _CurrentControl; } }
        private Control _CurrentControl;
        /// <summary>
        /// Aktuální control, který se používá namísto "this.Parent".
        /// Vždy to je Parent od controlu <see cref="CurrentControl"/>.
        /// <para/>
        /// Pozor, výjimečně zde může být Null: to je tehdy, když splitter deklaruje, že může být transferován do některého Parenta, ale dosud není určeno kam.
        /// </summary>
        protected Control CurrentParent { get { _TransferToParentCheck(); return _CurrentParent; } }
        private Control _CurrentParent;
        /// <summary>
        /// Obsahuje true, pokud je aktuálně platný režim "TransferToParent" a tedy namísto práce s this controlem pracujeme s jeho některým parentem.
        /// Pokud je tedy true, pak například hodnota <see cref="CurrentControl"/> odkazuje na některý z našich Parentů, jehož souřadnice měníme a jehož dáváme BringToFront.
        /// Pokud je zde false, pak pracujeme nativně se svým vlastním controlem (this).
        /// <para/>
        /// Pozor na logiku: pokud je povolena technika "TransferToParent" (tj. <see cref="TransferToParentEnabled"/> je true) a je zadán selector <see cref="TransferToParentSelector"/>,
        /// pak tento selector dostává k posouzení i samotný Splitter this (jako první control k posouzení).
        /// Pokud selectoru tento Splitter vyhovuje, pak selector vrací hodnotu <see cref="SplitterParentSelectorMode.Accept"/> už pro Splitter.
        /// Následně je určeno, že řízený control je this, a tedy NEJDE o externí control, tedy hodnota <see cref="CurrentControlIsExternal"/> bude false!
        /// </summary>
        protected bool CurrentControlIsExternal { get { _TransferToParentCheck(); return _CurrentControlIsExternal; } }
        private bool _CurrentControlIsExternal;
        /// <summary>
        /// Prověří a zajistí platnost subsystému "TransferToParent"
        /// </summary>
        /// <param name="force">Vynutit validaci i při stavu (<see cref="_TransferToParentValid"/> == true)</param>
        private void _TransferToParentCheck(bool force = false)
        {
            if (_TransferToParentValid && !force) return;

            _TransferToParentDetect();
            _TransferToParentRegisterEvents();
        }
        /// <summary>
        /// Tato metoda fyzicky určí hodnoty pro <see cref="CurrentControl"/> a <see cref="CurrentParent"/> a <see cref="CurrentControlIsExternal"/>,
        /// a nastaví validitu dat do <see cref="_TransferToParentValid"/>.
        /// </summary>
        private void _TransferToParentDetect()
        {
            Control oldControl = _CurrentControl;

            _CurrentControl = null;
            _CurrentParent = null;
            _CurrentControlIsExternal = false;
            _TransferToParentValid = false;

            if (!_TransferToParentEnabled || _TransferToParentSelector is null)
            {   // "Nativně" (anebo dosud bez Selectoru => stejně jako Nativně):
                _CurrentControl = this;
                _CurrentParent = this.Parent;
                _CurrentControlIsExternal = false;
                _TransferToParentValid = (_CurrentParent != null);
            }
            else
            {   // "TransferToParent" a Máme Selector: nabídneme mu postupně sebe a naše Parenty, ať si selector vybere:
                var selector = _TransferToParentSelector;
                Control control = this;
                int timeout = 24;
                bool isDone = false;
                while (control != null && ((timeout--) > 0))
                {   // Dokud máme parenta, a dokud neuplynul Timeout:
                    SplitterParentSelectorMode result = selector(control);
                    switch (result)
                    {
                        case SplitterParentSelectorMode.Accept:
                            // Akceptujeme nalezený control jako aktivní:
                            _CurrentControl = control;
                            _CurrentParent = control.Parent;
                            _CurrentControlIsExternal = !Object.ReferenceEquals(control, this);
                            _TransferToParentValid = (_CurrentParent != null);
                            isDone = true;
                            break;
                        case SplitterParentSelectorMode.Cancel:
                            // Končíme s hledáním parenta bez úspěchu:
                            isDone = true;
                            break;
                    }
                    if (isDone) break;
                    // Hledáme dalšího parenta:
                    control = control.Parent;
                }
            }
        }
        /// <summary>
        /// Metoda zajistí, že do nového Parenta <see cref="_CurrentParent"/> budou zaháčkované naše eventy.
        /// Nejprve ale: Pokud je evidován starý Parent v <see cref="_RegisteredParent"/>, a je jiný než nový, pak z něj naše eventy odregistrujeme.
        /// <para/>
        /// POZOR: důsledkem této metody je uložení objektu z <see cref="_CurrentParent"/> do <see cref="_RegisteredParent"/>!
        /// </summary>
        private void _TransferToParentRegisterEvents()
        {
            // a) Eventy volané v Controlu, pokud je Externí:
            Control oldControl = _RegisteredControl;
            Control newControl = _CurrentControl;
            if (oldControl != null && (newControl == null || !Object.ReferenceEquals(oldControl, newControl)))
            {   // Pokud máme starého parenta, a nový neexistuje anebo nový je jiný než starý, pak ze starého odregistrujeme naše eventy:
                oldControl.ClientSizeChanged -= _CurrentExternalControl_BoundsChanged;
                oldControl.LocationChanged -= _CurrentExternalControl_BoundsChanged;
            }
            _RegisteredControl = null;

            if (newControl != null && !Object.ReferenceEquals(newControl, this) && !Object.ReferenceEquals(oldControl, newControl))
            {   // Eventy do Controlu registrujeme pouze tehdy, když nový Control je zadaný a je jiný než this (= pro this nemusíme hlídat události takhle složitě!) a je jiný než předchozí:
                _RegisteredControl = newControl;
                newControl.ClientSizeChanged += _CurrentExternalControl_BoundsChanged;
                newControl.LocationChanged += _CurrentExternalControl_BoundsChanged;
            }


            // b) Eventy volané v Parentu:
            Control oldParent = _RegisteredParent;
            Control newParent = _CurrentParent;
            if (oldParent != null && (newParent == null || !Object.ReferenceEquals(oldParent, newParent)))
            {   // Pokud máme starého parenta, a nový neexistuje anebo nový je jiný než starý, pak ze starého odregistrujeme naše eventy:
                oldParent.ControlAdded -= _CurrentParent_ControlAdded;
                oldParent.ClientSizeChanged -= _CurrentParent_ClientSizeChanged;
            }

            _RegisteredParent = newParent;

            if (newParent != null && (oldParent == null || !Object.ReferenceEquals(oldParent, newParent)))
            {   // Pokud máme nového parenta, a starý neexistuje anebo nový je jiný než starý, pak do nového zaregistrujeme naše eventy:
                BringSplitterToFront(false);
                DetectParentSizeChanged();
                newParent.ControlAdded += _CurrentParent_ControlAdded;
                newParent.ClientSizeChanged += _CurrentParent_ClientSizeChanged;
            }
        }
        /// <summary>
        /// Invaliduje hodnoty subsystému "TransferToParent"
        /// </summary>
        private void _TransferToParentInvalidate()
        {
            _TransferToParentValid = false;
        }
        /// <summary>
        /// true = Nalezený Parent pro funkcionalitu "TransferToParent" je platný.
        /// </summary>
        private bool _TransferToParentValid;
        /// <summary>
        /// Aktuální control, který reprezentuje Splitter (může to být některý z Parentů)
        /// </summary>
        private Control _RegisteredControl;
        /// <summary>
        /// Parent control, do kterého máme aktuálně zaregistrované naše eventy.
        /// </summary>
        private Control _RegisteredParent;
        #endregion
        #region Enumy těsně svázané se Splitterem
        /// <summary>
        /// Způsoby aktivity splitteru ve vztahu k sousedním objektům
        /// </summary>
        public enum SplitterActivityMode
        {
            /// <summary>
            /// Splitter sám nemění pozici sousedních objektů, to ať si řeší vyšší kód na základě eventů
            /// </summary>
            None = 0,
            /// <summary>
            /// Splitter sám bude měnit pozici sousedních objektů, a to už v průběhu přesouvání myší (=Live)
            /// </summary>
            ResizeOnMoving,
            /// <summary>
            /// Splitter sám bude měnit pozici sousedních objektů, ale až po dokončení přesouvání myší po MouseUp (=Offline)
            /// </summary>
            ResizeAfterMove
        }
        /// <summary>
        /// Režim zobrazování vizuálního loga (například čtyřtečka) uprostřed splitbaru (při velikosti <see cref="SplitThick"/> nad 4px)
        /// </summary>
        public enum SplitterVisualLogoMode
        {
            /// <summary>
            /// Nikdy
            /// </summary>
            None = 0,
            /// <summary>
            /// Jen pod myší
            /// </summary>
            OnMouse,
            /// <summary>
            /// Vždy
            /// </summary>
            Allways
        }
        /// <summary>
        /// Typ udržování splitteru nahoře v poli controlů v ose Z-Order.
        /// Pokud NENÍ splitter nahoře a jeho souřadnice jsou překryty jiným controlem, pak splitter může být částečně nebo úplně neviditelný a myší nedosažitelný.
        /// </summary>
        public enum SplitterOnTopMode
        {
            /// <summary>
            /// Pozice se neudržuje
            /// </summary>
            None,
            /// <summary>
            /// Splitter se přesune nahoru v ose Z-Order jen až po najetí myší
            /// </summary>
            OnMouseEnter,
            /// <summary>
            /// Splitter se přesune nahoru v ose Z-Order jak po najetí myší, tak v situaci kdy jeho Parent přidá další control = tak, aby splittery byly OnTop.
            /// </summary>
            OnParentChanged
        }
        /// <summary>
        /// Druh výsledku při hledání vhodného parenta pro Splitter v režimu "TransferToParent"
        /// </summary>
        public enum SplitterParentSelectorMode
        {
            /// <summary>
            /// Akceptujeme dodaný control jako toho parenta, kterým bude splitter pohybovat
            /// </summary>
            Accept,
            /// <summary>
            /// Daného parenta neakceptujeme, chceme hledat dalšího parenta
            /// </summary>
            SearchAnother,
            /// <summary>
            /// Daného parenta neakceptujeme, dalšího nechceme, aktuálně nenalezen, 
            /// ale příště to zkusíme znovu - zatím není sestavena kompletní struktura okna
            /// </summary>
            Cancel
        }
        /// <summary>
        /// Akce prováděné po vložení hodnot do splitteru
        /// </summary>
        [Flags]
        protected enum SplitterSetActions
        {
            /// <summary>
            /// Žádná akce
            /// </summary>
            None = 0,
            /// <summary>
            /// Povinně provést akce, i když nebude detekována žádná změna hodnoty
            /// </summary>
            ForceActions = 0x0001,
            /// <summary>
            /// Přepočítat souřadnice splitteru
            /// </summary>
            RecalculateBounds = 0x0010,
            /// <summary>
            /// Přemístit navázané controly podle režimu aktivity
            /// </summary>
            MoveControlsByActivityMode = 0x0100,
            /// <summary>
            /// Přemístit navázané controly vždy = bez ohledu na režim aktivity
            /// </summary>
            MoveControlsAlways = 0x0200,
            /// <summary>
            /// Vyvolat událost Changing (stále probíhá změna)
            /// </summary>
            CallEventChanging = 0x1000,
            /// <summary>
            /// Vyvolat událost Changed (změna proběhla a je dokončena)
            /// </summary>
            CallEventChanged = 0x2000,

            /// <summary>
            /// Defaultní sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/> + <see cref="CallEventChanged"/>, ale žádné násilí
            /// </summary>
            Default = RecalculateBounds | MoveControlsByActivityMode | CallEventChanged,
            /// <summary>
            /// Tichá sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/>, ale žádné eventy a žádné násilí
            /// </summary>
            Silent = RecalculateBounds | MoveControlsByActivityMode
        }
        #endregion
    }
    #region enum SplitterAnchorType
    /// <summary>
    /// Typ ukotvení splitteru v rámci parent containeru.
    /// Uplatní se při Resize parenta, kdy splitter může změnit svoji pozici.
    /// </summary>
    public enum SplitterAnchorType
    {
        /// <summary>
        /// Defaultní hodnota = shodná jako Begin
        /// </summary>
        Default = 0,
        /// <summary>
        /// Splitter je ukotven k počátku, tedy Resize parenta se ho víceméně nedotkne
        /// </summary>
        Begin,
        /// <summary>
        /// Splitter je ukotven ke konci, tedy při Resize parenta se splitter pohybuje stejně jako koncový bod parenta
        /// </summary>
        End,
        /// <summary>
        /// Relativní = splitter si drží svoji relativní pozici na daním procentu, mění se velikost navázaných controlů na obou stranách
        /// </summary>
        Relative
    }
    #endregion
    #endregion
}
