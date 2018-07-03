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
            BoundsInfo spider = new BoundsInfo(0, 0, 0, 0, clientSize);
            return spider;
        }
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daný control (pro jeho velikost)
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForParent(GInteractiveControl control)
        {
            BoundsInfo spider = new BoundsInfo(0, 0, 0, 0, control.ClientSize);
            return spider;
        }
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daný control
        /// </summary>
        /// <param name="item"></param>
        /// <param name="absOriginPoint"></param>
        /// <param name="absVisibleBounds"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForParent(Point absOriginPoint, Rectangle absVisibleBounds)
        {
            BoundsInfo spider = new BoundsInfo(absOriginPoint.X, absOriginPoint.Y, absVisibleBounds.X, absVisibleBounds.Y, absVisibleBounds.Right, absVisibleBounds.Bottom);
            return spider;
        }
        /// <summary>
        /// Vrátí instanci třídy <see cref="BoundsInfo"/> pro daného parenta
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static BoundsInfo CreateForParent(IInteractiveParent parent)
        {
            BoundsInfo spider = new BoundsInfo(0, 0, parent.BoundsClient);
            return spider;
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
        public BoundsInfo CurrentChildsSpider { get { if (!this._UseCache || this._CurrentChildsSpider == null) this._CurrentChildsSpider = this._GetChildsSpider(this.CurrentItem, this.CurrentAbsChildsOrigin, this.CurrentAbsVisibleBounds); return this._CurrentChildsSpider; } }

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
            return BoundsInfo.CreateForParent(originPoint, clientVisibleBounds);
        }
        /// <summary>
        /// Metoda vrátí nový objekt <see cref="BoundsInfo"/>, který bude určovat souřadnice pro Childs prvky uvnitř daného prvku.
        /// Aktuální (=this) objekt <see cref="BoundsInfo"/> určuje souřadnice pro daný prvku, ale ne pro jeho Childs.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="originPoint"></param>
        /// <param name="clientVisibleBounds"></param>
        private BoundsInfo _GetChildsSpider(IInteractiveItem item, Point originPoint, Rectangle clientVisibleBounds)
        {
            CheckItem(item, "ChildsSpider");

            return BoundsInfo.CreateForParent(originPoint, clientVisibleBounds);
        }
        /// <summary>
        /// Vrátí danou relativní souřadnici posunutou do absolutních koordinátů (k souřadnici se přičte <see cref="_OriginX"/>, <see cref="_OriginY"/>)
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected Rectangle GetAbsBounds(Rectangle bounds)
        {
            return bounds.Add(this._OriginX, this._OriginY);
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
        protected static void CheckItem(IInteractiveItem item, string action)
        {
            if (item == null)
                throw new Application.GraphLibCodeException("Nelze provést akci BoundsSpider." + action + "(), dodaný prvek je null.");
        }
        #endregion
        #endregion
    }
}
