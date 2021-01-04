using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Třída, která reprezentuje souřadný systém v rámci jednoho parent prvku.
    /// Instance této třídy určuje absolutní souřadnice prvků v hierarchii prvků <see cref="IInteractiveParent"/> a <see cref="IInteractiveItem"/>.
    /// Vizuální control, který vykresluje jednotlivé prvky <see cref="IInteractiveItem"/>, má přístup pro kreslení v celé ploše controlu (v absolutních souřadnicích controlu),
    /// ale jednotlivé prvky <see cref="IInteractiveItem"/> mají určeny své souřadnice <see cref="IInteractiveItem.Bounds"/> relativně ke svému objektu <see cref="IInteractiveParent.Parent"/>.
    /// Pak tedy tato třída (<see cref="BoundsInfo"/> slouží pro postupné výpočty absolutních souřadnic jednotlivých prvků, dále k určení absolutních souřadnic jejich viditelných oblastí, 
    /// a pro určení absolutních souřadnic interaktivních oblastí.
    /// <para/>
    /// Třída se používá ve dvou režimech:
    /// <para/>
    /// a) Parent to Child = zdola nahoru: při kompletním procházení stromu prvků od vizuálního Controlu, přes jednotlivé prvky typu Container k jejich Childs prvkům (z hlediska sumárního výkonu optimálnější):
    /// Je vytvořena instance <see cref="BoundsInfo"/> pro vizuální control <see cref="InteractiveControl"/>, tato instance nabízí metody pro získání souřadnic konkrétního prvku v jedné úrovni,
    /// anebo umožňuje získání nové instance <see cref="BoundsInfo"/> pro jeden konkrétní z <see cref="IInteractiveParent.Childs"/>, kde tato instance bude vracet souřadnice pro jeho <see cref="IInteractiveParent.Childs"/>.
    /// <para/>
    /// b) Child to Parent = odshora dolů: pro zjištění potřebných údajů pro jeden Child prvek (pro jeden prvek kdekoli v hierarchii):
    /// kterýkoli prvek <see cref="IInteractiveItem"/> si může vytvořit instanci <see cref="BoundsInfo"/>, která reprezentuje souřadný systém pro tento prvek.
    /// Při vytváření této instance si algoritmus projde veškeré parenty daného prvku až k root prvku, jímž je vizuální control, a napočítá si absolutní souřadnice.
    /// <para/>
    /// Souřadný systém je "statický" = platí jen do té doby, dokud u některého z parentů nedojde ke změně jeho <see cref="IInteractiveItem.Bounds"/>. Pak je třeba souřadný systém zahodit, protože by poskytoval chybná data.
    /// </summary>
    public class BoundsInfo
    {
        #region Něco málo vysvětlivek
        /*
        1. ÚČEL
           POZICE
        Kreslící systém ve třídách ControlBuffered a ControlLayered (a následně interaktivní control InteractiveControl) 
        je postaven na vykreslování jednotlivých prvků (child itemů) do globální grafiky celého WinForm Controlu.
        K tomu vykreslování je třeba znát absolutní pozici konkrétního child itemu vzhledem k WinForm Controlu.
        Přitom samozřejmě pozicování child itemů (=jejich Bounds) je relativní výhradně k jejich Parentu, 
        kterým je jiný child item (=standardní vnořená hierarchie).
        Jinými slovy, pokud přemístím určitý item na jiné (relativní) souřadnice, pak nemusím měnit souřadnice jeho child itemů.
        A tyto child itemy se budou nacházet na stejné relativní souřadnici Bounds, ale fyzicky jsou na jiné absolutní souřadnici v rámci WinForm Controlu.
        (To samé, co platí o vykreslování, je platné i pro interaktivitu podmíněnou akcemi myši.)

        Účelem třídy BoundsInfo je tedy vypočítat absolutní souřadnice konkrétního child itemu vzhledem k fyzickému WinForm Controlu.

           VIDITELNOST
        Dalším úkolem je určit, jaká část itemu je fyzicky viditelná.
        Pokud určitý child item leží částečně mimo zobrazovanou oblast WinForm Controlu, anebo mimo souřadnice Bounds svého parent itemu,
        pak child item může být částečně nebo zcela neviditelný.
        To má vliv na vykreslování (nebudeme vykreslovat item, který je zcela mimo viditelnou oblast),
        i na interaktivitu (prvek item nemůže zachytávat akce myši v oblasti, kde není viditelný třeba proto, že leží mimo prostor svého parenta).

        2. DALŠÍ FUNKCIONALITA = AutoScroll prvky
        Systém prvků dovoluje implementovat AutoScroll = postup, který detekuje rozmístění child itemů, určuje tak potřebný prostor, 
        porovnává jej s disponibilním prostorem v parent prvku (ať je to WinForm Control nebo běžný parent item);
        a pokud je disponibilní prostor menší než je třeba, pak aktivuje AutoScroll režim (=zobrazí se ScrollBary).
        Tím se stává prostor hostitelského prvku "virtuálním" = jeho souřadnice se posouvají vlivem scrollování.
        I tuto věc řeší třída BoundsInfo.

        3. POSTUP ŘEŠENÍ - pro směr Parent => Child
        a) Vstupem do souřadného systému je WinForm Control
        b) Souřadný systém zajišťuje přepočet relativní souřadnice určitého child itemu (jeho Bounds) do absolutní pozice v rámci WinForm Controlu
           (fyzicky jde o pozici bodu počátku, který se přičte k souřadnici počátku child itemu Bounds.Location, 
           a výsledkem je fyzická souřadnice na Controlu)
        c) Kvůli AutoScrollu máme dva souřadné systémy: Fyzický souřadný systém (FSS) a Virtuální (VSS)
        d) Většina child itemů (běžné) má souřadnice vztažené k VSS, a tedy při AutoScrollu se pohybují ve svém Parentu
        e) Některé child itemy (ScrollBary od AutoScrollu) mají svoje souřadnice vztažené k FSS, při provádění AutoScrollu se ve svém parentu nepohybují
        f) Child itemy si tedy pro určení sých Absolute Bounds vyberou "svůj" souřadný systém, jeho bod počátku a podle něj určí svoje absolutní souřadnice;
           a podle něj určí i své AbsoluteVisibleBounds
        f) Při vytváření BoundsInfo pro vnořené child itemy se bude vycházet z aktuálních AbsoluteBounds prvku (containeru),



















        */
        #endregion
        #region Metody pro směr Parent to Child
        #region Konstruktory
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daný WinForm Control. Ten může být i <see cref="IAutoScrollContainer"/>!
        /// </summary>
        /// <param name="control"></param>
        /// <param name="layer">Vrstva grafiky. Ovlivňuje výběr Bounds z dodaného prvku: pro vrstvu <see cref="GInteractiveDrawLayer.Interactive"/> 
        /// akceptuje souřadnice <see cref="IInteractiveItem.BoundsInteractive"/>, jinak bere standardně <see cref="IInteractiveItem.Bounds"/>.</param>
        /// <returns></returns>
        public static BoundsInfo CreateForControl(System.Windows.Forms.Control control, GInteractiveDrawLayer layer = GInteractiveDrawLayer.Standard)
        {
            Coordinates physicalCoordinates = Coordinates.FromSize(control.ClientSize);
            Coordinates virtualCoordinates = physicalCoordinates;
            if (control is IAutoScrollContainer)
            {
                IAutoScrollContainer autoScrollContainer = control as IAutoScrollContainer;
                if (autoScrollContainer.AutoScrollActive)
                {
                    Point virtualOrigin = autoScrollContainer.VirtualOrigin;
                    Rectangle virtualBounds = physicalCoordinates.GetVisibleBounds(autoScrollContainer.VirtualBounds);
                    virtualCoordinates = Coordinates.FromOrigin(virtualOrigin, virtualBounds);
                }
            }
            return new BoundsInfo(physicalCoordinates, virtualCoordinates, true, true, layer);
        }
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daný IInteractiveParent parent. Ten může být i <see cref="IAutoScrollContainer"/>!
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="layer">Vrstva grafiky. Ovlivňuje výběr Bounds z dodaného prvku: pro vrstvu <see cref="GInteractiveDrawLayer.Interactive"/> 
        /// akceptuje souřadnice <see cref="IInteractiveItem.BoundsInteractive"/>, jinak bere standardně <see cref="IInteractiveItem.Bounds"/>.</param>
        /// <returns></returns>
        public static BoundsInfo CreateForParent(IInteractiveParent parent, GInteractiveDrawLayer layer = GInteractiveDrawLayer.Standard)
        {
            Coordinates physicalCoordinates = Coordinates.FromSize(parent.ClientSize);
            Coordinates virtualCoordinates = physicalCoordinates;
            if (parent is IAutoScrollContainer)
            {
                IAutoScrollContainer autoScrollContainer = parent as IAutoScrollContainer;
                if (autoScrollContainer.AutoScrollActive)
                {
                    Point virtualOrigin = autoScrollContainer.VirtualOrigin;
                    Rectangle virtualBounds = physicalCoordinates.GetVisibleBounds(autoScrollContainer.VirtualBounds);
                    virtualCoordinates = Coordinates.FromOrigin(virtualOrigin, virtualBounds);
                }
            }
            return new BoundsInfo(physicalCoordinates, virtualCoordinates, true, true, layer);
        }
        /// <summary>
        /// Privátní konstruktor pro dané koordináty
        /// </summary>
        /// <param name="physicalCoordinates"></param>
        /// <param name="virtualCoordinates"></param>
        /// <param name="isVisible"></param>
        /// <param name="isEnabled"></param>
        /// <param name="layer">Vrstva grafiky. Ovlivňuje výběr Bounds z dodaného prvku: pro vrstvu <see cref="GInteractiveDrawLayer.Interactive"/> 
        /// akceptuje souřadnice <see cref="IInteractiveItem.BoundsInteractive"/>, jinak bere standardně <see cref="IInteractiveItem.Bounds"/>.</param>
        private BoundsInfo(Coordinates physicalCoordinates, Coordinates virtualCoordinates, bool isVisible, bool isEnabled, GInteractiveDrawLayer layer)
        {
            this._PhysicalCoordinates = physicalCoordinates;
            this._VirtualCoordinates = virtualCoordinates;
            this._IsVisible = isVisible;
            this._IsEnabled = isEnabled;
            this._CurrentLayer = layer;
        }
        private Coordinates _PhysicalCoordinates;
        private Coordinates _VirtualCoordinates;
        private bool _IsVisible;
        private bool _IsEnabled;
        private IInteractiveItem _CurrentItem;
        private GInteractiveDrawLayer _CurrentLayer;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Physical Coordinates: " + this._PhysicalCoordinates.ToString();
        }
        #endregion
        #region Hodnoty pro aktuální container, nikoli pro aktuální prvek CurrentItem
        /// <summary>
        /// Absolutní souřadnice bodu, který reprezentuje bod 0/0 aktuálního containeru.
        /// </summary>
        public Point AbsolutePhysicalOriginPoint { get { return this._PhysicalCoordinates.OriginPoint; } }
        /// <summary>
        /// Absolutní fyzické souřadnice prostoru, který je nyní viditelný.
        /// Slouží k provedení <see cref="Graphics.IntersectClip(Rectangle)"/> při kreslení.
        /// </summary>
        public Rectangle AbsolutePhysicalVisibleBounds { get { return this._PhysicalCoordinates.VisibleBounds; } }
        /// <summary>
        /// Je viditelný základní prostor (tj. on a jeho Parenti mají Is.Visible = true)?
        /// Tato property nemluví o konkrétním prvku <see cref="CurrentItem"/>, ale o jeho Parent prostoru.
        /// </summary>
        public bool IsVisible { get { return this._IsVisible; } }
        /// <summary>
        /// Je Enabled základní prostor (tj. on a jeho Parenti mají Is.Enabled = true)?
        /// Tato property nemluví o konkrétním prvku <see cref="CurrentItem"/>, ale o jeho Parent prostoru.
        /// </summary>
        public bool IsEnabled { get { return this._IsEnabled; } }

        #endregion
        #region CurrentItem a jeho hodnoty
        /// <summary>
        /// Aktuální prvek. Pro něj jsou platné všechny properties typu Current*
        /// </summary>
        public IInteractiveItem CurrentItem { get { return this._CurrentItem; } set { this._CurrentItem = value; } }
        /// <summary>
        /// Aktuální vrstva. Ovlivňuje výběr Bounds z dodaného prvku: pro vrstvu <see cref="GInteractiveDrawLayer.Interactive"/> 
        /// akceptuje souřadnice <see cref="IInteractiveItem.BoundsInteractive"/>, jinak bere standardně <see cref="IInteractiveItem.Bounds"/>.
        /// </summary>
        public GInteractiveDrawLayer CurrentLayer { get { return this._CurrentLayer; } set { this._CurrentLayer = value; } }
        /// <summary>
        /// Absolutní fyzické souřadnice aktuálního prvku <see cref="CurrentItem"/>.
        /// Tedy jde o souřadnice, na kterých je prvek fyzicky vykreslen v rámci controlu.
        /// Tato hodnota obsahuje i neviditelné části prvku, na rozdíl od <see cref="CurrentItemAbsoluteVisibleBounds"/>.
        /// </summary>
        public Rectangle CurrentItemAbsoluteBounds { get { CheckCurrentItem("AbsoluteBounds"); return this.GetAbsoluteBounds(this.CurrentItem, this.CurrentLayer); } }
        /// <summary>
        /// Absolutní fyzické souřadnice aktuálního prvku <see cref="CurrentItem"/>, na kterých je viditelný.
        /// Tedy jde o podmnožinu <see cref="CurrentItemAbsoluteBounds"/> = jen viditelné pixely.
        /// </summary>
        public Rectangle CurrentItemAbsoluteVisibleBounds { get { CheckCurrentItem("AbsoluteVisibleBounds"); return this.GetAbsoluteVisibleBounds(this.CurrentItem, this.CurrentLayer); } }
        /// <summary>
        /// Obsahuje true, pokud aktuální prvek <see cref="CurrentItem"/> je alespoň zčásti viditelný 
        /// (jeho <see cref="CurrentItemAbsoluteVisibleBounds"/> má nějaký viditelný pixel).
        /// </summary>
        public bool CurrentItemAbsoluteIsVisible { get { CheckCurrentItem("AbsoluteIsVisible"); return this.CurrentItemAbsoluteVisibleBounds.HasPixels(); } }
        /// <summary>
        /// Obsahuje true, pokud aktuální container je zobrazován (má <see cref="IsVisible"/> = true)
        /// a současně i aktuální prvek <see cref="CurrentItem"/> je zobrazován (jeho IsVisible = true).
        /// </summary>
        public bool CurrentItemIsVisible { get { CheckCurrentItem("IsVisible"); return (this._IsVisible && this.CurrentItem.Is.Visible); } }
        /// <summary>
        /// Obsahuje true, pokud aktuální container je zobrazován (má <see cref="IsEnabled"/> = true)
        /// a současně i aktuální prvek <see cref="CurrentItem"/> je zobrazován (jeho IsEnabled = true).
        /// </summary>
        public bool CurrentItemIsEnabled { get { CheckCurrentItem("IsVisible"); return (this._IsEnabled && this.CurrentItem.Is.Enabled); } }
        /// <summary>
        /// Obsahuje nový objekt <see cref="BoundsInfo"/>, který bude určovat souřadnice pro Childs prvky uvnitř aktuálního prvku <see cref="CurrentItem"/>.
        /// Aktuální (=this) objekt <see cref="BoundsInfo"/> určuje souřadnice pro <see cref="CurrentItem"/>, ale ne pro jeho Childs.
        /// </summary>
        public BoundsInfo CurrentChildsBoundsInfo { get { return GetChildsBoundsInfo(this.CurrentItem, this.CurrentLayer); } }
        /// <summary>
        /// Vrací souřadnice daného prvku <paramref name="item"/> pro danou vrstvu <paramref name="layer"/>.
        /// Pro vrstvu <see cref="GInteractiveDrawLayer.Interactive"/> vrací interaktivní souřadnice <see cref="IInteractiveItem.BoundsInteractive"/>, pokud jsou zadány.
        /// Standardně vrací <see cref="IInteractiveItem.Bounds"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        protected Rectangle GetItemBounds(IInteractiveItem item, GInteractiveDrawLayer layer)
        {
            if (item == null) return Rectangle.Empty;
            if (layer == GInteractiveDrawLayer.Interactive && item.BoundsInteractive.HasValue) return item.BoundsInteractive.Value;
            return item.Bounds;
        }
        /// <summary>
        /// Vrátí absolutní souřadnice (AbsoluteBounds) pro daný Child prvek.
        /// Souřadný systém prvku volí podle toho, zda prvek je umístěn ve fyzických nebo ve virtuálních souřadnicích.
        /// </summary>
        /// <param name="item">Prvek</param>
        /// <param name="layer">Vrstva, ovlivní výběr typu souřadnic</param>
        /// <returns></returns>
        protected Rectangle GetAbsoluteBounds(IInteractiveItem item, GInteractiveDrawLayer layer)
        {
            Coordinates parentCoordinates = (item.Is.OnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            Rectangle bounds = GetItemBounds(item, layer);
            return parentCoordinates.GetAbsoluteBounds(bounds);
        }
        /// <summary>
        /// Vrátí absolutní souřadnice (AbsoluteBounds) viditelné části pro daný Child prvek.
        /// Souřadný systém prvku volí podle toho, zda prvek je umístěn ve fyzických nebo ve virtuálních souřadnicích.
        /// </summary>
        /// <param name="item">Prvek</param>
        /// <param name="layer">Vrstva, ovlivní výběr typu souřadnic</param>
        /// <returns></returns>
        protected Rectangle GetAbsoluteVisibleBounds(IInteractiveItem item, GInteractiveDrawLayer layer)
        {
            Coordinates parentCoordinates = (item.Is.OnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            Rectangle bounds = GetItemBounds(item, layer);
            return parentCoordinates.GetVisibleBounds(parentCoordinates.GetAbsoluteBounds(bounds));
        }
        /// <summary>
        /// Vrátí novou instanci <see cref="BoundsInfo"/> pro daný Child objekt, který od té chvíle bude Parent objektem pro další, v něm vnořené prvky.
        /// Respektuje přitom, že daný Child objekt se může pohybovat ve virtuálních anebo ve fyzických souřadnicích aktuálního systému.
        /// </summary>
        /// <param name="item">Prvek</param>
        /// <param name="layer">Vrstva, ovlivní výběr typu souřadnic</param>
        /// <returns></returns>
        protected BoundsInfo GetChildsBoundsInfo(IInteractiveItem item, GInteractiveDrawLayer layer)
        {
            // Child prvek je umístěn ve svých souřadnicích Bounds, a to buď ve fyzickém nebo ve virtuálním prostoru (koordinátech):
            Coordinates parentCoordinates = (item.Is.OnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);

            // Určíme jeho absolutní souřadnice = jeho Bounds posunuté do odpovídajících (fyzické/virtuální) koordinátů:
            Rectangle bounds = GetItemBounds(item, layer);
            Rectangle absoluteBounds = parentCoordinates.GetAbsoluteBounds(bounds);

            // Prvek může zmenšit tento prostor o své vnitřní okraje. 
            //  Prostor okrajů patří do prvku (=prvek sám si je dokáže vykreslit), ale nepatří do prostoru, který prvek poskytuje svým child prvkům:
            // Toto jsou tedy absolutní souřadnice prostoru, ve kterém budou zobrazovány Child prvky:
            Rectangle clientBounds = absoluteBounds.Sub(item.ClientBorder);

            // Určíme viditelnou oblast (průsečík z prostoru pro childs s prostorem dosud viditelné oblasti):
            Rectangle visibleBounds = parentCoordinates.GetVisibleBounds(clientBounds);

            // V rámci těchto souřadnic prvek item může poskytovat svůj souřadný systém standardní (fyzický) anebo i virtuální:
            Coordinates physicalCoordinates = Coordinates.FromOrigin(clientBounds.Location, visibleBounds);
            Coordinates virtualCoordinates = physicalCoordinates;
            if (item is IAutoScrollContainer)
            {   // Child prvek (item) je AutoScrollContainer:
                IAutoScrollContainer autoScrollContainer = item as IAutoScrollContainer;
                if (autoScrollContainer.AutoScrollActive)
                {
                    Point virtualOrigin = parentCoordinates.GetAbsolutePoint(autoScrollContainer.VirtualOrigin);
                    Rectangle virtualBounds = parentCoordinates.GetVisibleBounds(parentCoordinates.GetAbsoluteBounds(autoScrollContainer.VirtualBounds));
                    virtualCoordinates = Coordinates.FromOrigin(virtualOrigin, virtualBounds);
                }
            }
            bool isVisible = this.IsVisible && item.Is.Visible;
            bool isEnabled = this.IsEnabled && item.Is.Enabled;
            return new BoundsInfo(physicalCoordinates, virtualCoordinates, isVisible, isEnabled, layer);
        }
        /// <summary>
        /// Prověří, že prvek <see cref="_CurrentItem"/> není null. Pokud je null, vyhodí chybu.
        /// </summary>
        /// <param name="action"></param>
        protected void CheckCurrentItem(string action) { CheckItem(this._CurrentItem, action); }
        /// <summary>
        /// Prověří, že daný prvek není null. Pokud je null, vyhodí chybu.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        protected static void CheckItem(IInteractiveParent item, string action)
        {
            if (item == null)
                throw new Application.GraphLibCodeException("Nelze provést akci BoundsInfo." + action + "(), dodaný prvek je null.");
        }
        #endregion
        #endregion
        #region Metody pro směr Child to Parent
        /// <summary>
        /// Pro daný prvek (který je na pozici Child) určí a vrátí souřadný systém, v kterém se pohybuje.
        /// Jde tedy o systém, v němž se daný prvek pohybuje, nikoli o systém který poskytuje svým Childs prvkům.
        /// Daný prvek bude umístěn do property <see cref="BoundsInfo.CurrentItem"/>, a v ostatních properties vráceného systému budou k dispozici jeho jednotlivé koordináty
        /// (např. <see cref="BoundsInfo.CurrentItemAbsoluteBounds"/> bude obsahovat absolutní souřadnice daného prvku).
        /// </summary>
        /// <param name="currentItem"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForChild(IInteractiveItem currentItem)
        {
            CheckItem(currentItem, "CreateForChild");
            return _CreateForItem(currentItem, false, GInteractiveDrawLayer.Standard);
        }
        /// <summary>
        /// Pro daný prvek určí a vrátí jeho vlastní souřadný systém, který poskytuje svým Childs.
        /// Jde tedy o systém, do jehož <see cref="CurrentItem"/> lze vložit kterýkoli jeho Childs, a systém bude vracet jeho souřadnice.
        /// Vrácený prvek má property <see cref="BoundsInfo.CurrentItem"/> neobsazenou.
        /// </summary>
        /// <param name="currentContainer"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForContainer(IInteractiveParent currentContainer)
        {
            CheckItem(currentContainer, "CreateForContainer");
            return _CreateForItem(currentContainer, true, GInteractiveDrawLayer.Standard);
        }
        /// <summary>
        /// Pro daný prvek určí a vrátí jeho vlastní souřadný systém, který poskytuje svým Childs.
        /// Jde tedy o systém, do jehož <see cref="CurrentItem"/> lze vložit kterýkoli jeho Childs, a systém bude vracet jeho souřadnice.
        /// Vrácený prvek má property <see cref="BoundsInfo.CurrentItem"/> neobsazenou.
        /// </summary>
        /// <param name="currentContainer"></param>
        /// <param name="layer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag and Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        public static BoundsInfo CreateForContainer(IInteractiveParent currentContainer, GInteractiveDrawLayer layer)
        {
            CheckItem(currentContainer, "CreateForContainer");
            return _CreateForItem(currentContainer, true, layer);
        }
        /// <summary>
        /// Vrací <see cref="BoundsInfo"/> pro daného parenta a danou vrstvu souřadnic
        /// </summary>
        /// <param name="forItem"></param>
        /// <param name="asContainer"></param>
        /// <param name="layer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag and Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag = <see cref="IInteractiveItem.BoundsInteractive"/>.</param>
        /// <returns></returns>
        private static BoundsInfo _CreateForItem(IInteractiveParent forItem, bool asContainer, GInteractiveDrawLayer layer)
        {
            // Nejprve si nastřádám řadu prvků počínaje daným prvkem/nebo jeho parentem, přes jeho Parenty, až k nejspodnějšímu prvku (který nemá parenta):
            List<IInteractiveParent> parents = new List<IInteractiveParent>();
            Dictionary<uint, object> scanned = new Dictionary<uint, object>();
            IInteractiveParent parent = (asContainer ? forItem : forItem.Parent);
            while (parent != null)
            {
                if (scanned.ContainsKey(parent.Id)) break;   // Zacyklení!
                parents.Add(parent);
                parent = parent.Parent;                      // Krok na dalšího parenta
            }

            // Pak vytvořím postupně řadu BoundsInfo, počínaje od posledního v poli parents (=fyzický Control):
            int last = parents.Count - 1;
            // Pokud na vstupu byl předán container = fyzický Control, pak pole má 0 prvků a výstupní BoundsInfo je vytvořeno pro daný Control!
            if (last < 0) return BoundsInfo.CreateForParent(forItem);
            // Poslední prvek pole = parents[last] = fyzický Control:
            BoundsInfo boundsInfo = BoundsInfo.CreateForParent(parents[last], layer);
            for (int i = last - 1; i >= 0; i--)
            {
                IInteractiveItem it = parents[i] as IInteractiveItem;
                if (it == null) continue;
                boundsInfo = boundsInfo.GetChildsBoundsInfo(it, layer);
            }

            // Na závěr do výsledného boundsInfo vepíšu dodaný Child prvek (forItem) jako Current prvek:
            if (!asContainer && forItem is IInteractiveItem)
                boundsInfo.CurrentItem = forItem as IInteractiveItem;
            return boundsInfo;
        }
        #endregion
        #region Public servis
        /// <summary>
        /// Vrátí absolutní pozici daného relativního bodu
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Point GetAbsolutePoint(Point relativePoint, bool isOnPhysicalBounds = false)
        {
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetAbsolutePoint(relativePoint);
        }
        /// <summary>
        /// Vrátí absolutní pozici daného relativního bodu
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Point? GetAbsolutePoint(Point? relativePoint, bool isOnPhysicalBounds = false)
        {
            if (!relativePoint.HasValue) return null;
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetAbsolutePoint(relativePoint.Value);
        }
        /// <summary>
        /// Vrátí absolutní hodnoty daného relativního prostoru
        /// </summary>
        /// <param name="relativeBounds"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Rectangle GetAbsoluteBounds(Rectangle relativeBounds, bool isOnPhysicalBounds = false)
        {
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetAbsoluteBounds(relativeBounds);
        }
        /// <summary>
        /// Vrátí absolutní hodnoty daného relativního prostoru
        /// </summary>
        /// <param name="relativeBounds"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Rectangle? GetAbsoluteBounds(Rectangle? relativeBounds, bool isOnPhysicalBounds = false)
        {
            if (!relativeBounds.HasValue) return null;
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetAbsoluteBounds(relativeBounds.Value);
        }
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Point GetRelativePoint(Point absolutePoint, bool isOnPhysicalBounds = false)
        {
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetRelativePoint(absolutePoint);
        }
        /// <summary>
        /// Vrátí relativní pozici daného absolutního bodu
        /// </summary>
        /// <param name="absolutePoint"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Point? GetRelativePoint(Point? absolutePoint, bool isOnPhysicalBounds = false)
        {
            if (!absolutePoint.HasValue) return null;
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetRelativePoint(absolutePoint.Value);
        }
        /// <summary>
        /// Vrátí relativní hodnoty daného absolutního prostoru
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Rectangle GetRelativeBounds(Rectangle absoluteBounds, bool isOnPhysicalBounds = false)
        {
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetRelativeBounds(absoluteBounds);
        }
        /// <summary>
        /// Vrátí relativní hodnoty daného absolutního prostoru
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="isOnPhysicalBounds"></param>
        /// <returns></returns>
        public Rectangle? GetRelativeBounds(Rectangle? absoluteBounds, bool isOnPhysicalBounds = false)
        {
            if (!absoluteBounds.HasValue) return null;
            Coordinates parentCoordinates = (isOnPhysicalBounds ? this._PhysicalCoordinates : this._VirtualCoordinates);
            return parentCoordinates.GetRelativeBounds(absoluteBounds.Value);
        }
        #endregion
        #region Static služby
        /// <summary>
        /// Metoda vrátí absolutní souřadnice daného objektu.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBounds(IInteractiveItem item)
        {
            BoundsInfo boundsInfo = BoundsInfo.CreateForChild(item);
            return boundsInfo.CurrentItemAbsoluteBounds;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prostoru, který je zadán jako relativní souřadnice v daném containeru.
        /// Pokud tedy například daný container je umístěn na (absolutní) souřadnici Bounds = { 100,20,200,50 }, a dané relativní souřadnice jsou { 5,5,10,10 },
        /// pak výsledné absolutní souřadnice jsou { 105,25,10,10 }.
        /// </summary>
        /// <param name="container">Container, ve kterém je umístěn nějaký prvek, a v němž jsou uvedeny relativní souřadnice</param>
        /// <param name="relativeBounds">Relativní souřadnice prostoru, relativně k containeru</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBoundsInContainer(IInteractiveParent container, Rectangle relativeBounds)
        {
            if (container == null) return relativeBounds;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container);
            return relativeBounds.Add(boundsInfo.AbsolutePhysicalOriginPoint);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prostoru, který je zadán jako relativní souřadnice v daném containeru.
        /// Pokud tedy například daný container je umístěn na (absolutní) souřadnici Bounds = { 100,20,200,50 }, a dané relativní souřadnice jsou { 5,5,10,10 },
        /// pak výsledné absolutní souřadnice jsou { 105,25,10,10 }.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="relativeBounds"></param>
        /// <param name="currentLayer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag and Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBoundsInContainer(IInteractiveParent container, Rectangle relativeBounds, GInteractiveDrawLayer currentLayer)
        {
            if (container == null) return relativeBounds;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container, currentLayer);
            return relativeBounds.Add(boundsInfo.AbsolutePhysicalOriginPoint);
        }
        /// <summary>
        /// Metoda vrací relativní souřadnici bodu v daném containeru pro danou absolutní souřadnici.
        /// Metoda určí souřadný systém <see cref="BoundsInfo"/> uvnitř daného containeru, 
        /// získá jeho <see cref="BoundsInfo.AbsolutePhysicalOriginPoint"/>, a vrátí rozdíl.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public static Point GetRelativePointInContainer(IInteractiveParent container, Point absolutePoint)
        {
            if (container == null) return absolutePoint;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container);
            return absolutePoint.Sub(boundsInfo.AbsolutePhysicalOriginPoint);
        }
        #endregion
        #region class Coordinates
        /// <summary>
        /// Střadač souřadnic
        /// </summary>
        protected class Coordinates
        {
            #region Konstruktory a základní property
            /// <summary>
            /// Výchozí prostor
            /// </summary>
            public static Coordinates Root { get { return new Coordinates(0, 0, 0, 0, 20480, 20480); } }
            /// <summary>
            /// Maximální zpracovaná souřadnice (Right, Bottom)
            /// </summary>
            public const int Max = 20480;
            /// <summary>
            /// Vrátí souřadný prostor odpovídající dané velikosti, s tím že prostor začíná souřadnicí 0,0
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            public static Coordinates FromSize(Size size)
            {
                return new Coordinates(0, 0, 0, 0, size.Width, size.Height);
            }
            /// <summary>
            /// Vrátí souřadný prostor odpovídající danému Rectangle, s tím že celý prostor Rectangle je viditelný
            /// </summary>
            /// <param name="bounds"></param>
            /// <returns></returns>
            public static Coordinates FromRectangle(Rectangle bounds)
            {
                return new Coordinates(bounds.X, bounds.Y, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            }
            /// <summary>
            /// Vrátí souřadný prostor, kde je dán bod počátku a souřadnice viditelného prostoru
            /// </summary>
            /// <param name="origin"></param>
            /// <param name="visible"></param>
            /// <returns></returns>
            public static Coordinates FromOrigin(Point origin, Rectangle visible)
            {
                return new Coordinates(origin.X, origin.Y, visible.Left, visible.Top, visible.Right, visible.Bottom);
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="originX"></param>
            /// <param name="originY"></param>
            /// <param name="visibleL"></param>
            /// <param name="visibleT"></param>
            /// <param name="visibleR"></param>
            /// <param name="visibleB"></param>
            public Coordinates(int originX, int originY, int visibleL, int visibleT, int visibleR, int visibleB)
            {
                this.OriginX = originX;
                this.OriginY = originY;
                this.VisibleL = visibleL;
                this.VisibleT = visibleT;
                this.VisibleR = visibleR;
                this.VisibleB = visibleB;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return "Origin: { X=" + this.OriginX + ", Y=" + this.OriginY + " };  " +
                       "Visible: { L=" + this.VisibleL + ", T=" + this.VisibleT + ", R=" + this.VisibleR + ", B=" + this.VisibleB + " }";
            }
            /// <summary>
            /// Absolutní souřadnice X bodu, který reprezentuje relativní 0/0 v aktuálním containeru.
            /// <see cref="Root"/> control pro fyzickou souřadnici zde má 0/0 = to je výchozí bod souřadného systému.
            /// Jeho Child control zde má svoji souřadnici Bounds.X; jeho vnořený control zde má součet souřadnic X, atd.
            /// Na rozdíl od toho je souřadnice <see cref="VisibleL"/> nastřádaná hodnota Max() z těchto souřadnic <see cref="OriginX"/>, 
            /// protože když nějaký control bude umístěn vlevo (jeho Bounds.X bude záporné), 
            /// pak <see cref="VisibleL"/> zůstane na své hodnotě = levá část vnořeného controlu není vidět.
            /// </summary>
            protected readonly int OriginX;
            /// <summary>
            /// Absolutní souřadnice Y bodu, který reprezentuje relativní 0/0 v aktuálním containeru.
            /// <see cref="Root"/> control pro fyzickou souřadnici zde má 0/0 = to je výchozí bod souřadného systému.
            /// Jeho Child control zde má svoji souřadnici Bounds.Y; jeho vnořený control zde má součet souřadnic Y, atd.
            /// Na rozdíl od toho je souřadnice <see cref="VisibleT"/> nastřádaná hodnota Max() z těchto souřadnic <see cref="OriginY"/>, 
            /// protože když nějaký control bude umístěn nahoře (jeho Bounds.Y bude záporné), 
            /// pak <see cref="VisibleT"/> zůstane na své hodnotě = horní část vnořeného controlu není vidět.
            /// </summary>
            protected readonly int OriginY;
            /// <summary>
            /// Absolutní hodnota Left v aktuálním containeru, která reprezentuje první viditelný pixel.
            /// </summary>
            protected readonly int VisibleL;
            /// <summary>
            /// Absolutní hodnota Top v aktuálním containeru, která reprezentuje první viditelný pixel.
            /// </summary>
            protected readonly int VisibleT;
            /// <summary>
            /// Absolutní hodnota Right v aktuálním containeru, která reprezentuje první už nezobrazovaný pixel doprava za posledním viditelným.
            /// Pokud tedy <see cref="VisibleR"/> == <see cref="VisibleL"/>, pak na ose X už není nic vidět.
            /// </summary>
            protected readonly int VisibleR;
            /// <summary>
            /// Absolutní hodnota Bottom v aktuálním containeru, která reprezentuje první už nezobrazovaný pixel dolů pod posledním viditelným.
            /// Pokud tedy <see cref="VisibleB"/> == <see cref="VisibleT"/>, pak na ose Y už není nic vidět.
            /// </summary>
            protected readonly int VisibleB;
            #endregion
            #region Public prvky
            /// <summary>
            /// Absolutní souřadnice bodu, který reprezentuje relativní 0/0 v aktuálním containeru.
            /// <see cref="Root"/> control pro fyzickou souřadnici zde má bod 0/0 = to je výchozí bod souřadného systému.
            /// Jeho Child control zde má svoji souřadnici Bounds.Location; jeho vnořený control zde má součet souřadnic Bounds.Location, atd.
            /// </summary>
            public Point OriginPoint { get { return new Point(this.OriginX, this.OriginY); } }
            /// <summary>
            /// Souřadnice viditelného prostoru
            /// </summary>
            public Rectangle VisibleBounds { get { return Rectangle.FromLTRB(this.VisibleL, this.VisibleT, this.VisibleR, this.VisibleB); } }
            /// <summary>
            /// Vrátí absolutní souřadnice daného bodu (vstupem je relativní souřadnice bodu).
            /// Daný relativní bod pouze posune o { <see cref="OriginX"/>, <see cref="OriginY"/> }.
            /// </summary>
            /// <param name="relativePoint">Relativní souřadnice</param>
            /// <returns></returns>
            public Point GetAbsolutePoint(Point relativePoint)
            {
                return new Point(relativePoint.X + this.OriginX, relativePoint.Y + this.OriginY);
            }
            /// <summary>
            /// Vrátí absolutní souřadnice daného prostoru (vstupem je typicky InteractiveObject.Bounds).
            /// Prostor pouze posune o { <see cref="OriginX"/>, <see cref="OriginY"/> }.
            /// </summary>
            /// <param name="relativeBounds">Relativní souřadnice</param>
            /// <returns></returns>
            public Rectangle GetAbsoluteBounds(Rectangle relativeBounds)
            {
                return new Rectangle(relativeBounds.X + this.OriginX, relativeBounds.Y + this.OriginY, relativeBounds.Width, relativeBounds.Height);
            }
            /// <summary>
            /// Vrátí relativní souřadnice daného bodu (vstupem je absolutní souřadnice bodu).
            /// Daný absolutní bod pouze posune o { mínus <see cref="OriginX"/>, mínus <see cref="OriginY"/> }.
            /// </summary>
            /// <param name="absolutePoint">Absolutní souřadnice</param>
            /// <returns></returns>
            public Point GetRelativePoint(Point absolutePoint)
            {
                return new Point(absolutePoint.X - this.OriginX, absolutePoint.Y - this.OriginY);
            }
            /// <summary>
            /// Vrátí relativní souřadnice daného prostoru (vstupem je absolutní souřadnice prostoru).
            /// Prostor pouze posune o { mínus <see cref="OriginX"/>, mínus <see cref="OriginY"/> }.
            /// </summary>
            /// <param name="absoluteBounds">Absolutní souřadnice</param>
            /// <returns></returns>
            public Rectangle GetRelativeBounds(Rectangle absoluteBounds)
            {
                return new Rectangle(absoluteBounds.X - this.OriginX, absoluteBounds.Y - this.OriginY, absoluteBounds.Width, absoluteBounds.Height);
            }
            /// <summary>
            /// Vrátí absolutní viditelné souřadnice daného absolutního prostoru.
            /// Výstup je tedy průnik daného prostoru a <see cref="VisibleBounds"/>.
            /// </summary>
            /// <param name="absoluteBounds">Absolutní souřadnice</param>
            /// <returns></returns>
            public Rectangle GetVisibleBounds(Rectangle absoluteBounds)
            {
                return Rectangle.Intersect(absoluteBounds, this.VisibleBounds);
            }
            /// <summary>
            /// Obsahuje true pokud <see cref="VisibleBounds"/> obsahuje nějaké reálně viditelné pixely.
            /// </summary>
            public bool IsVisible { get { return ((this.VisibleR > this.VisibleL) && (this.VisibleB > this.VisibleT)); } }
            #endregion
        }
        #endregion
    }
}
