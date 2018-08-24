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
    /// Je vytvořena instance <see cref="BoundsInfo"/> pro vizuální control <see cref="GInteractiveControl"/>, tato instance nabízí metody pro získání souřadnic konkrétního prvku v jedné úrovni,
    /// anebo umožňuje získání nové instance <see cref="BoundsInfo"/> pro jeden konkrétní z <see cref="IInteractiveItem.Childs"/>, kde tato instance bude vracet souřadnice pro jeho <see cref="IInteractiveItem.Childs"/>.
    /// <para/>
    /// b) Child to Parent = odshora dolů: pro zjištění potřebných údajů pro jeden Child prvek (pro jeden prvek kdekoli v hierarchii):
    /// kterýkoli prvek <see cref="IInteractiveItem"/> si může vytvořit instanci <see cref="BoundsInfo"/>, která reprezentuje souřadný systém pro tento prvek.
    /// Při vytváření této instance si algoritmus projde veškeré parenty daného prvku až k root prvku, jímž je vizuální control, a napočítá si absolutní souřadnice.
    /// <para/>
    /// Souřadný systém je "statický" = platí jen do té doby, dokud u některého z parentů nedojde ke změně jeho <see cref="IInteractiveItem.Bounds"/>. Pak je třeba souřadný systém zahodit, protože by poskytoval chybná data.
    /// </summary>
    public class BoundsInfo
    {
        #region Metody pro směr Parent to Child
        #region Konstruktory
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro danou velikost klientského prostoru
        /// </summary>
        /// <param name="clientSize"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForParent(Size clientSize)
        {
            return new BoundsInfo(0, 0, 0, 0, clientSize);
        }
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daný control
        /// </summary>
        /// <param name="item"></param>
        /// <param name="absOriginPoint"></param>
        /// <param name="absVisibleBounds"></param>
        /// <returns></returns>
        private static BoundsInfo CreateForParent(Point absOriginPoint, Rectangle absVisibleBounds)
        {
            return new BoundsInfo(absOriginPoint.X, absOriginPoint.Y, absVisibleBounds.X, absVisibleBounds.Y, absVisibleBounds.Right, absVisibleBounds.Bottom);
        }
        /// <summary>
        /// Konstruktor, dostává absolutní souřadnice počátku a absolutní souřadnice viditelného prostoru ve formě Rectangle.
        /// </summary>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="visibleBounds"></param>
        private BoundsInfo(int originX, int originY, Rectangle visibleBounds)
            : this(originX, originY, visibleBounds.Left, visibleBounds.Top, visibleBounds.Right, visibleBounds.Bottom)
        { }
        /// <summary>
        /// Konstruktor, dostává absolutní souřadnice počátku a absolutní souřadnice viditelného prostoru ve formě L-T-Size.
        /// </summary>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="visibleL"></param>
        /// <param name="visibleT"></param>
        /// <param name="visibleSize"></param>
        private BoundsInfo(int originX, int originY, int visibleL, int visibleT, Size visibleSize)
            : this(originX, originY, visibleL, visibleT, visibleL + visibleSize.Width, visibleT + visibleSize.Height)
        { }
        /// <summary>
        /// Základní konstruktor, dostává absolutní souřadnice počátku a absolutní souřadnice viditelného prostoru ve formě L-T-R-B.
        /// </summary>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="visibleL"></param>
        /// <param name="visibleT"></param>
        /// <param name="visibleR"></param>
        /// <param name="visibleB"></param>
        private BoundsInfo(int originX, int originY, int visibleL, int visibleT, int visibleR, int visibleB)
        {
            this._UseCache = false;
            this._OriginX = originX;
            this._OriginY = originY;
            this._VisibleL = visibleL;
            this._VisibleT = visibleT;
            this._VisibleR = visibleR;
            this._VisibleB = visibleB;
        }
        private bool _UseCache;
        private int _OriginX;
        private int _OriginY;
        private int _VisibleL;
        private int _VisibleT;
        private int _VisibleR;
        private int _VisibleB;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Origin: { X=" + this._OriginX + ", Y=" + this._OriginY + " };  " +
                   "Visible: { L=" + this._VisibleL + ", T=" + this._VisibleT + ", R=" + this._VisibleR + ", B=" + this._VisibleB + " }";
        }
        #endregion
        #region Zásadní hodnoty: AbsOrigin a AbsVisibleBounds
        /// <summary>
        /// Absolutní souřadnice bodu, který reprezentuje bod 0/0 aktuálního containeru.
        /// </summary>
        public Point AbsOrigin { get { return new Point(this._OriginX, this._OriginY); } }
        /// <summary>
        /// Absolutní souřadnice prostoru, který je nyní viditelný, a v němž je možno hledat interaktivní prvky.
        /// </summary>
        public Rectangle AbsVisibleBounds { get { return Rectangle.FromLTRB(this._VisibleL, this._VisibleT, this._VisibleR, this._VisibleB); } }
        #endregion
        #region Objekt CurrentItem a jeho pozice
        /// <summary>
        /// Aktuální prvek. Pro něj jsou nastaveny všechny properties typu Current*
        /// </summary>
        public IInteractiveItem CurrentItem { get { return this._CurrentItem; } set { this._ResetCurrentItemCache(); this._CurrentItem = value; } }
        /// <summary>
        /// Absolutní souřadnice aktuálního prvku <see cref="CurrentItem"/>.
        /// </summary>
        public Rectangle CurrentAbsBounds { get { if (!this._UseCache || !this._CurrentAbsBounds.HasValue) this._CurrentAbsBounds = this.GetAbsBounds(this.CurrentItem); return this._CurrentAbsBounds.Value; } }
        /// <summary>
        /// Absolutní souřadnice viditelné části aktuálního prvku <see cref="CurrentItem"/>.
        /// Toto je oblast, na kterou by měl být oříznut (Clip) grafiky při kreslení prvku.
        /// </summary>
        public Rectangle CurrentAbsVisibleBounds { get { if (!this._UseCache || !this._CurrentAbsVisibleBounds.HasValue) this._CurrentAbsVisibleBounds = this.GetAbsVisibleBounds(this.CurrentItem); return this._CurrentAbsVisibleBounds.Value; } }
        /// <summary>
        /// Absolutní souřadnice interaktivního prostoru aktuálního prvku <see cref="CurrentItem"/>.
        /// Pokud se bude myš nacházet v tomto prostoru, může být aktuální prvek tím aktivním.
        /// Prostor je oříznut pouze do viditelné oblasti, takže prvek by prvek tuto oblast přesahoval, pak jeho přesahující část nebude brána za interaktivní.
        /// Obecně nemáme metodu pro určení interaktivních souřadnic bez oříznutí do viditelné oblasti, protože to nedává smysl.
        /// </summary>
        public Rectangle CurrentAbsInteractiveBounds { get { if (!this._UseCache || !this._CurrentAbsInteractiveBounds.HasValue) this._CurrentAbsInteractiveBounds = this.GetAbsInteractiveBounds(this.CurrentItem); return this._CurrentAbsInteractiveBounds.Value; } }
        /// <summary>
        /// Absolutní souřadnice celého prostoru pro Childs prvky aktuálního prvku <see cref="CurrentItem"/>.
        /// Odpovídá souřadnici Bounds aktuálního prvku <see cref="CurrentItem"/>, zmenšeného o <see cref="IInteractiveItem.ClientBorder"/>.
        /// Tento prostor není oříznutý do viditelné oblasti.
        /// </summary>
        public Rectangle CurrentAbsChildsBounds { get { if (!this._UseCache || !this._CurrentAbsChildsBounds.HasValue) this._CurrentAbsChildsBounds = this.GetAbsChildsBounds(this.CurrentItem); return this._CurrentAbsChildsBounds.Value; } }
        /// <summary>
        /// Absolutní souřadnice viditelného prostoru pro Childs prvky aktuálního prvku <see cref="CurrentItem"/>.
        /// Toto je prostor, v němž se mohou vyskytovat (zobrazovat a být interaktivní) Childs prvky. Jejich případné části, které přisahují mimo tento prostor, jsou neviditelné a neaktivní.
        /// Jde o VIDITELNÝ prostor, jehož počáteční bod (<see cref="Rectangle.Location"/> nemusí být shodný s bodem počátku <see cref="CurrentAbsChildsOrigin"/>,
        /// protože počáteční pixely tohoto prostoru mohou být odsunuté nahoru/doleva ve svém Parentu a tudíž nejsou viditelné.
        /// </summary>
        public Rectangle CurrentAbsChildsVisibleBounds { get { if (!this._UseCache || !this._CurrentAbsChildsVisibleBounds.HasValue) this._CurrentAbsChildsVisibleBounds = this.GetAbsChildsVisibleBounds(this.CurrentItem); return this._CurrentAbsChildsVisibleBounds.Value; } }
        /// <summary>
        /// Relativní souřadnice viditelného prostoru pro Childs prvky aktuálního prvku <see cref="CurrentItem"/>.
        /// Relativní = v rámci parenta aktuálního prvku.
        /// Vrací <see cref="IInteractiveItem.Bounds"/> zmenšené o klientské okraje <see cref="IInteractiveItem.ClientBorder"/>.
        /// Pokud aktuální prvek nemá naplněny klientské okraje <see cref="IInteractiveItem.ClientBorder"/>, pak vrací nezměněné <see cref="IInteractiveItem.Bounds"/>.
        /// </summary>
        public Rectangle CurrentChildsBounds { get { if (!this._UseCache || !this._CurrentChildsBounds.HasValue) this._CurrentChildsBounds = GetChildBounds(this.CurrentItem); return this._CurrentChildsBounds.Value; } }
        /// <summary>
        /// Absolutní souřadnice bodu, který je počátkem souřadného systému Childs prvků aktuálního prvku <see cref="CurrentItem"/>.
        /// </summary>
        public Point CurrentAbsChildsOrigin { get { if (!this._UseCache || !this._CurrentAbsChildsOrigin.HasValue) this._CurrentAbsChildsOrigin = this.GetAbsChildsOrigin(this.CurrentItem); return this._CurrentAbsChildsOrigin.Value; } }
        /// <summary>
        /// Obsahuje nový objekt <see cref="BoundsInfo"/>, který bude určovat souřadnice pro Childs prvky uvnitř aktuálního prvku <see cref="CurrentItem"/>.
        /// Aktuální (=this) objekt <see cref="BoundsInfo"/> určuje souřadnice pro <see cref="CurrentItem"/>, ale ne pro jeho Childs.
        /// </summary>
        public BoundsInfo CurrentChildsSpider { get { if (!this._UseCache || this._CurrentChildsSpider == null) this._CurrentChildsSpider = _GetChildsSpider(this.CurrentAbsChildsOrigin, this.CurrentAbsVisibleBounds); return this._CurrentChildsSpider; } }

        /// <summary>
        /// Resetuje cache výsledných hodnot pro prvek <see cref="_CurrentItem"/>
        /// </summary>
        private void _ResetCurrentItemCache()
        {
            this._CurrentAbsBounds = null;
            this._CurrentAbsVisibleBounds = null;
            this._CurrentAbsInteractiveBounds = null;
            this._CurrentAbsChildsBounds = null;
            this._CurrentAbsChildsVisibleBounds = null;
            this._CurrentChildsBounds = null;
            this._CurrentAbsChildsOrigin = null;
            this._CurrentChildsSpider = null;
        }
        private IInteractiveItem _CurrentItem;
        private Rectangle? _CurrentAbsBounds;
        private Rectangle? _CurrentAbsVisibleBounds;
        private Rectangle? _CurrentAbsInteractiveBounds;
        private Rectangle? _CurrentAbsChildsBounds;
        private Rectangle? _CurrentAbsChildsVisibleBounds;
        private Rectangle? _CurrentChildsBounds;
        private Point? _CurrentAbsChildsOrigin;
        private BoundsInfo _CurrentChildsSpider;
        #endregion
        #region Metody pro získání souřadnic pro libovolný prvek (v aktuálním containeru)
        /// <summary>
        /// Metoda vrátí absolutní souřadnici Bounds daného prvku.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rectangle GetAbsBounds(IInteractiveItem item)
        {
            CheckItem(item, "AbsBounds");
            return this.GetAbsBounds(item.Bounds);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice viditelné části daného prvku.
        /// Toto je oblast, na kterou by měl být oříznut (Clip) grafiky při kreslení prvku.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rectangle GetAbsVisibleBounds(IInteractiveItem item)
        {
            CheckItem(item, "AbsVisibleBounds");
            return this.GetVisibleAbsBounds(item.Bounds);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice interaktivního prostoru daného prvku.
        /// Pokud se bude myš nacházet v tomto prostoru, může být daný prvek tím aktivním.
        /// Prostor je oříznut pouze do viditelné oblasti, takže pokud by prvek tuto oblast přesahoval, pak jeho přesahující část nebude brána za interaktivní.
        /// Obecně nemáme metodu pro určení interaktivních souřadnic bez oříznutí do viditelné oblasti, protože to nedává smysl.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rectangle GetAbsInteractiveBounds(IInteractiveItem item)
        {
            CheckItem(item, "AbsInteractiveBounds");
            Rectangle bounds = item.Bounds;                                                             // Základ = souřadnice prvku
            if (item.InteractivePadding.HasValue) bounds = bounds.Add(item.InteractivePadding.Value);   // Zvětšit je o interaktivní rozšíření
            return this.GetVisibleAbsBounds(bounds);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice celého prostoru pro Childs prvky daného prvku.
        /// Odpovídá souřadnici Bounds aktuálního prvku <see cref="CurrentItem"/>, zmenšeného o <see cref="IInteractiveItem.ClientBorder"/>.
        /// Tento prostor není oříznutý do viditelné oblasti.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rectangle GetAbsChildsBounds(IInteractiveItem item)
        {
            CheckItem(item, "AbsChildsBounds");
            Rectangle bounds = item.Bounds;                                                             // Základ = souřadnice prvku
            if (item.ClientBorder.HasValue) bounds = bounds.Sub(item.ClientBorder.Value);               // Zmenšit je o vnitřní klientský okraj
            return this.GetAbsBounds(bounds);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice viditelného prostoru pro Childs prvky daného prvku.
        /// Toto je prostor, v němž se mohou vyskytovat (zobrazovat a být interaktivní) Childs prvky. Jejich případné části, které přisahují mimo tento prostor, jsou neviditelné a neaktivní.
        /// Jde o VIDITELNÝ prostor, jehož počáteční bod (<see cref="Rectangle.Location"/> nemusí být shodný s bodem počátku <see cref="GetAbsChildsOrigin(IInteractiveItem)"/>,
        /// protože počáteční pixely tohoto prostoru mohou být odsunuté nahoru/doleva ve svém Parentu a tudíž nejsou viditelné.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rectangle GetAbsChildsVisibleBounds(IInteractiveItem item)
        {
            CheckItem(item, "AbsChildsVisibleBounds");
            Rectangle bounds = item.Bounds;                                                             // Základ = souřadnice prvku
            if (item.ClientBorder.HasValue) bounds = bounds.Sub(item.ClientBorder.Value);               // Zmenšit je o vnitřní klientský okraj
            return this.GetVisibleAbsBounds(bounds);
        }
        /// <summary>
        /// Metoda vrátí relativní souřadnice viditelného prostoru pro Childs prvky daného prvku.
        /// Relativní = v rámci parenta daného prvku, tedy ve stejném souřadném systému jako <see cref="IInteractiveItem.Bounds"/>.
        /// Vrací <see cref="IInteractiveItem.Bounds"/> zmenšené o klientské okraje <see cref="IInteractiveItem.ClientBorder"/>.
        /// Pokud prvek nemá naplněny klientské okraje <see cref="IInteractiveItem.ClientBorder"/>, pak vrací nezměněné <see cref="IInteractiveItem.Bounds"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Rectangle GetChildBounds(IInteractiveItem item)
        {
            Rectangle bounds = item.Bounds;                                                             // Základ = souřadnice prvku, Zmenšit je o vnitřní klientský okraj
            if (item.ClientBorder.HasValue) bounds = bounds.Sub(item.ClientBorder.Value);               // Zmenšit je o vnitřní klientský okraj
            return bounds;
        }
        /// <summary>
        /// Metoda vrátí velikost prostoru pro Childs prvky v rámci daného objektu.
        /// V podstatě vrací <see cref="IInteractiveItem.Bounds"/>.Size - <see cref="IInteractiveItem.ClientBorder"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Size GetClientSize(IInteractiveItem item)
        {
            Size size = item.Bounds.Size.Sub(item.ClientBorder);                                        // Základ = velikost prvku, Zmenšit je o vnitřní klientský okraj
            return size;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice bodu, který je počátkem souřadného systému Childs prvků daného prvku.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Point GetAbsChildsOrigin(IInteractiveItem item)
        {
            CheckItem(item, "AbsChildsOrigin");
            Rectangle bounds = this.GetAbsChildsBounds(item);
            return bounds.Location;
        }
        /// <summary>
        /// Metoda vrátí nový objekt <see cref="BoundsInfo"/>, který bude určovat souřadnice pro Childs prvky uvnitř daného prvku.
        /// Aktuální (=this) objekt <see cref="BoundsInfo"/> určuje souřadnice pro daný prvku, ale ne pro jeho Childs.
        /// </summary>
        public BoundsInfo GetChildsSpider(IInteractiveItem item)
        {
            CheckItem(item, "ChildsSpider");

            Point originPoint = this.GetAbsChildsOrigin(item);
            Rectangle clientVisibleBounds = this.GetAbsChildsVisibleBounds(item);
            return _GetChildsSpider(originPoint, clientVisibleBounds);
        }
        /// <summary>
        /// Metoda vrátí nový objekt <see cref="BoundsInfo"/>, který bude určovat souřadnice pro Childs prvky uvnitř daného prvku.
        /// Aktuální (=this) objekt <see cref="BoundsInfo"/> určuje souřadnice pro daný prvku, ale ne pro jeho Childs.
        /// </summary>
        /// <param name="originPoint"></param>
        /// <param name="clientVisibleBounds"></param>
        private static BoundsInfo _GetChildsSpider(Point originPoint, Rectangle clientVisibleBounds)
        {
            return BoundsInfo.CreateForParent(originPoint, clientVisibleBounds);
        }
        /// <summary>
        /// Vrátí danou relativní souřadnici posunutou do absolutních koordinátů (k souřadnici se přičte <see cref="_OriginX"/>, <see cref="_OriginY"/>)
        /// </summary>
        /// <param name="relativeBounds"></param>
        /// <returns></returns>
        public Rectangle GetAbsBounds(Rectangle relativeBounds)
        {
            return relativeBounds.Add(this._OriginX, this._OriginY);
        }
        /// <summary>
        /// Vrátí danou absolutní souřadnici posunutou do relativních koordinátů (k souřadnici se odečte <see cref="_OriginX"/>, <see cref="_OriginY"/>)
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <returns></returns>
        public Rectangle GetRelBounds(Rectangle absoluteBounds)
        {
            return absoluteBounds.Sub(this._OriginX, this._OriginY);
        }
        /// <summary>
        /// Vrátí danou relativní souřadnici posunutou do absolutních koordinátů (k souřadnici se přičte <see cref="_OriginX"/>, <see cref="_OriginY"/>),
        /// a oříznutou do viditelných souřadnic.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected Rectangle GetVisibleAbsBounds(Rectangle bounds)
        {
            int l = bounds.Left + this._OriginX;
            int t = bounds.Top + this._OriginY;
            int r = l + bounds.Width;
            int b = t + bounds.Height;

            if (l < this._VisibleL) l = this._VisibleL;
            if (t < this._VisibleT) t = this._VisibleT;
            if (r > this._VisibleR) r = this._VisibleR;
            if (b > this._VisibleB) b = this._VisibleB;

            return Rectangle.FromLTRB(l, t, r, b);
        }
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
        /// (např. <see cref="BoundsInfo.CurrentAbsBounds"/> bude obsahovat absolutní souřadnice daného prvku).
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
        /// <param name="currentLayer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag & Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        public static BoundsInfo CreateForContainer(IInteractiveParent currentContainer, GInteractiveDrawLayer currentLayer)
        {
            CheckItem(currentContainer, "CreateForContainer");
            return _CreateForItem(currentContainer, true, currentLayer);
        }
        /// <summary>
        /// Vrací <see cref="BoundsInfo"/> pro daného parenta a danou vrstvu souřadnic
        /// </summary>
        /// <param name="forItem"></param>
        /// <param name="asContainer"></param>
        /// <param name="currentLayer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag & Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        private static BoundsInfo _CreateForItem(IInteractiveParent forItem, bool asContainer, GInteractiveDrawLayer currentLayer)
        { 
            // Nejprve projdu postupně všechny parenty daného prvku, zpětně, až najdu poslední (=nejzákladnější) z nich, a nastřádám si pole jejich souřadnic:
            List<Rectangle> boundsList = new List<Rectangle>();
            IInteractiveParent item = (asContainer ? forItem : forItem.Parent);
            Dictionary<uint, object> scanned = new Dictionary<uint, object>();
            while (item != null)
            {
                if (scanned.ContainsKey(item.Id)) break;   // Zacyklení.
                scanned.Add(item.Id, null);
                boundsList.Add(GetParentClientBounds(item, currentLayer));
                item = item.Parent;                        // Krok na dalšího parenta
            }

            // Nyní projdu prvky v jejich grafickém pořadí = podle hierarchie od základního (root) až k parentovi našeho prvku currentItem (tento currentItem tam není!),
            //  a nastřádám si souřadnice Origin a Visible:
            int x = 0;
            int y = 0;
            int l = 0;
            int t = 0;
            int r = 16380;
            int b = 16380;

            for (int i = boundsList.Count - 1; i >= 0; i--)
            {   // Pole boundsList procházíme od posledního prvku, neboť tam je umístěn root Control:
                Rectangle relBounds = boundsList[i];
                Rectangle absBounds = relBounds.Add(x, y);
                x = absBounds.X;
                y = absBounds.Y;
                if (l < absBounds.Left) l = absBounds.Left;
                if (t < absBounds.Top) t = absBounds.Top;
                if (r < absBounds.Right) r = absBounds.Right;
                if (b < absBounds.Bottom) b = absBounds.Bottom;
            }

            // Výsledek bude mít nastaveny koordináty (Origin a Visible), a bude mít vložený CurrentItem (pokud je metoda volaná pro Item):
            BoundsInfo boundsInfo = new BoundsInfo(x, y, l, t, r, b);
            if (!asContainer && forItem is IInteractiveItem)
                boundsInfo.CurrentItem = forItem as IInteractiveItem;
            return boundsInfo;
        }
        #endregion
        #region Statické metody
        /// <summary>
        /// Metoda vrátí absolutní souřadnice daného objektu.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBounds(IInteractiveItem item)
        {
            BoundsInfo boundsInfo = BoundsInfo.CreateForChild(item);
            return boundsInfo.CurrentAbsBounds;
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prostoru, který je zadán jako relativní souřadnice v daném containeru.
        /// Pokud tedy například daný container je umístěn na (absolutní) souřadnici Bounds = { 100,20,200,50 }, a dané relativní souřadnice jsou { 5,5,10,10 },
        /// pak výsledné absolutní souřadnice jsou { 105,25,10,10 }.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="relativeBounds"></param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBoundsInContainer(IInteractiveParent container, Rectangle relativeBounds)
        {
            if (container == null) return relativeBounds;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container);
            return relativeBounds.Add(boundsInfo.AbsOrigin);
        }
        /// <summary>
        /// Metoda vrátí absolutní souřadnice prostoru, který je zadán jako relativní souřadnice v daném containeru.
        /// Pokud tedy například daný container je umístěn na (absolutní) souřadnici Bounds = { 100,20,200,50 }, a dané relativní souřadnice jsou { 5,5,10,10 },
        /// pak výsledné absolutní souřadnice jsou { 105,25,10,10 }.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="relativeBounds"></param>
        /// <param name="currentLayer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag & Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        public static Rectangle GetAbsoluteBoundsInContainer(IInteractiveParent container, Rectangle relativeBounds, GInteractiveDrawLayer currentLayer)
        {
            if (container == null) return relativeBounds;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container, currentLayer);
            return relativeBounds.Add(boundsInfo.AbsOrigin);
        }
        /// <summary>
        /// Metoda vrací relativní souřadnici bodu v daném containeru pro danou absolutní souřadnici.
        /// Metoda určí souřadný systém <see cref="BoundsInfo"/> uvnitř daného containeru, 
        /// získá jeho <see cref="BoundsInfo.AbsOrigin"/>, a vrátí rozdíl.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="absolutePoint"></param>
        /// <returns></returns>
        public static Point GetRelativePointInContainer(IInteractiveParent container, Point absolutePoint)
        {
            if (container == null) return absolutePoint;
            BoundsInfo boundsInfo = BoundsInfo.CreateForContainer(container);
            return absolutePoint.Sub(boundsInfo.AbsOrigin);
        }
        /// <summary>
        /// Vrátí rectangle, který reprezentuje souřadnice klientského prostoru, v rámci daného parenta
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="currentLayer">Vrstva, jejíž souřadnice řešíme. Každý prvek může mít souřadnice různé podle toho, o kterou vrstvu se jedná. 
        /// To je důsledek procesu Drag & Drop, kdy ve standardní vrstvě se prvek nachází na výchozích souřadnicích Bounds, 
        /// ale ve vrstvě <see cref="GInteractiveDrawLayer.Interactive"/> je na souřadnicích Drag.</param>
        /// <returns></returns>
        protected static Rectangle GetParentClientBounds(IInteractiveParent parent, GInteractiveDrawLayer currentLayer)
        {
            if (parent is IInteractiveItem)
            {
                IInteractiveItem item = parent as IInteractiveItem;
                Rectangle bounds = item.Bounds;
                if (currentLayer == GInteractiveDrawLayer.Interactive && item.BoundsInteractive.HasValue)
                    bounds = item.BoundsInteractive.Value;
                return bounds.Sub(item.ClientBorder);
            }
            return new Rectangle(new Point(0, 0), parent.ClientSize);
        }
        #endregion
    }
}
