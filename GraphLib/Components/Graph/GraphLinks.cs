using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components.Graphs
{
    #region GTimeGraphLinkArray : Vizuální pole, obsahující prvky GTimeGraphLinkItem
    /// <summary>
    /// <see cref="TimeGraphLinkArray"/> : Vizuální pole, obsahující prvky <see cref="TimeGraphLinkItem"/>. Jde o <see cref="InteractiveObject"/>, 
    /// který nemá implementovanou interaktivitu, ale je součástí tabulky <see cref="Grids.GTable"/> 
    /// (anebo je členem vizuálních prvků hlavního controlu Host), 
    /// a je vykreslován do vrstvy Dynamic.
    /// Graf samotný obsahuje referenci na tento objekt, referenci dohledává on-demand a případně ji vytváří a umisťuje tak, 
    /// aby objekt byl dostupný i dalším grafům.
    /// Toto jedno pole je společné všem grafům jedné tabulky (nebo jednoho hostitele).
    /// </summary>
    public class TimeGraphLinkArray : InteractiveObject
    {
        #region Konstrukce, úložiště linků, reference na ownera (tabulka / graf)
        /// <summary>
        /// Konstruktor pro graf
        /// </summary>
        /// <param name="ownerGraph"></param>
        public TimeGraphLinkArray(TimeGraph ownerGraph)
            : this()
        {
            this._OwnerGraph = ownerGraph;
        }
        /// <summary>
        /// Konstruktor pro tabulku
        /// </summary>
        /// <param name="ownerGTable"></param>
        public TimeGraphLinkArray(Grids.GTable ownerGTable)
            : this()
        {
            this._OwnerGTable = ownerGTable;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TimeGraphLinkArray()
        {
            this._LinkDict = new Dictionary<UInt64, LinkInfo>();
        }
        /// <summary>
        /// Obsahuje true, pokud má nějaké Linky
        /// </summary>
        public bool HasLinks { get { return (_LinkDict.Count > 0); } }
        /// <summary>
        /// Počet Linků v evidenci
        /// </summary>
        public int LinkCount { get { return _LinkDict.Count; } }
        /// <summary>
        /// Úložiště linků + přidaných dat
        /// </summary>
        private Dictionary<UInt64, LinkInfo> _LinkDict;
        /// <summary>
        /// true pokud this objekt je platný jen pro jeden Graf
        /// </summary>
        public bool IsForOneGraph { get { return (this._OwnerGraph != null && this._OwnerGTable == null); } }
        /// <summary>
        /// Graf, jehož jsme koordinátorem (může být null?)
        /// </summary>
        private TimeGraph _OwnerGraph;
        /// <summary>
        /// true pokud this objekt je platný pro celou GTable
        /// </summary>
        public bool IsForGTable { get { return (this._OwnerGTable != null); } }
        /// <summary>
        /// Tabulka, jíž jsme koordinátorem (může být null?)
        /// </summary>
        private Grids.GTable _OwnerGTable;
        #endregion
        #region Aktivní role = získání linků z datového zdroje
        /// <summary>
        /// Určuje aktuálně platný režim zobrazení spojovacích čar mezi prvky.
        /// </summary>
        public TimeGraphLinkMode CurrentLinksMode
        {
            get { return this._CurrentLinksMode; }
            set
            {
                this.ReadLinksForMode(true, value);
                this._CurrentLinksMode = value;
            }
        }
        private TimeGraphLinkMode _CurrentLinksMode;
        /// <summary>
        /// Určuje výchozí režim zobrazení spojovacích čar mezi prvky.
        /// Jeho setování vede k setování i do <see cref="CurrentLinksMode"/>, a to provede načtení linků podle zadaného režimu.
        /// </summary>
        public TimeGraphLinkMode DefaultLinksMode
        {
            get { return this._DefaultLinksMode; }
            set
            {
                this._DefaultLinksMode = value;
                this.CurrentLinksMode = value;
            }
        }
        private TimeGraphLinkMode _DefaultLinksMode;
        /// <summary>
        /// Zajistí aktuální načtení linků pro režim <see cref="CurrentLinksMode"/>
        /// </summary>
        public void ReloadLinks()
        {
            this.ReadLinksForMode(true, this.CurrentLinksMode);
        }
        /// <summary>
        /// Za pomoci datového zdroje <see cref="LinkDataSource"/> získá vztahy (linky) pro daný režim, a zajistí jejich zobrazení.
        /// Je voláno po změně režimu <see cref="CurrentLinksMode"/>, a explicitně z metody <see cref="ReloadLinks()"/>.
        /// </summary>
        /// <param name="clear">Nulovat aktuální soupis linků?</param>
        /// <param name="linksMode">Režim pro zobrazení nových linků</param>
        protected void ReadLinksForMode(bool clear, TimeGraphLinkMode linksMode)
        {
            if (clear)
                this.Clear();

            ITimeGraphLinkDataSource linkDataSource = this.LinkDataSource;
            if (linkDataSource == null) return;

            TimeGraphLinkMode itemMode = linksMode;
            if (linksMode.HasFlag(TimeGraphLinkMode.Allways)) itemMode = TimeGraphLinkMode.Allways;

            CreateAllLinksArgs args = null;

            if (linkDataSource != null)
            {
                // Tato varianta nemusí řešit režim Linků GTimeGraphLinkMode.MouseOver, protože linky MouseOver naskočí samy při pohybu myši :-).
                // Primárně jde o rozlišení Allways / Selected / None:
                if (linksMode.HasFlag(TimeGraphLinkMode.Allways))
                {
                    args = new CreateAllLinksArgs(TimeGraphLinkMode.Allways);
                    linkDataSource.CreateLinks(args);
                }
                else if (linksMode.HasFlag(TimeGraphLinkMode.Selected))
                {
                    if (this.Host != null && this.Host.Selector != null)
                    {
                        args = new CreateAllLinksArgs(TimeGraphLinkMode.Selected, this.Host.Selector.SelectedItems);
                        linkDataSource.CreateLinks(args);
                    }
                }
            }

            if (args != null && args.Links.Count > 0)
                this.AddLinks(args.Links, itemMode);
        }
        /// <summary>
        /// Datový zdroj, ze kterého mohou být čteny linky - v situaci, kdy GUI si samo chce vyžádat seznam linků
        /// </summary>
        public ITimeGraphLinkDataSource LinkDataSource { get; set; }
        #endregion
        #region Pasivní role = přidávání, odebírání, enumerace linků
        /// <summary>
        /// Přidá dané linky do paměti
        /// </summary>
        /// <param name="links">Souhrn linků k přidání</param>
        /// <param name="mode">Důvod zobrazení</param>
        public void AddLinks(IEnumerable<TimeGraphLinkItem> links, TimeGraphLinkMode mode)
        {
            if (links == null) return;
            if (mode == TimeGraphLinkMode.None) return;
            Dictionary<UInt64, LinkInfo> linkDict = this._LinkDict;
            foreach (TimeGraphLinkItem link in links)
            {
                if (link == null) continue;
                UInt64 key = link.Key;
                LinkInfo linkInfo;
                bool exists = linkDict.TryGetValue(key, out linkInfo);
                if (!exists)
                {   // Pro daný klíč (Prev-Next) dosud nemám link => založím si nový, a přidám do něj dodaná data:
                    linkInfo = new LinkInfo(this, link) { Mode = mode };
                    linkDict.Add(key, linkInfo);
                }
                else
                {   // Link máme => přidáme do něj případně nové bity do jeho režimu:
                    linkInfo.Mode |= mode;
                }
            }
            // Zajistíme překreslení všech vztahů:
            this.Repaint();
        }
        /// <summary>
        /// Odebere dané linky z paměti, pokud v ní jsou a pokud již neexistuje důvod pro jejich zobrazování.
        /// Důvod zobrazení: každý link v sobě eviduje souhrn důvodů, pro které byl zobrazen (metoda <see cref="AddLinks(IEnumerable{TimeGraphLinkItem}, TimeGraphLinkMode)"/>),
        /// důvody z opakovaných volání této metody se průběžně sčítají, a při odebírání se odečítají.
        /// A až tam nezbyde žádný, bude link ze seznamu odebrán.
        /// </summary>
        /// <param name="links">Souhrn linků k odebrání</param>
        /// <param name="mode">Důvod, pro který byl link zobrazen</param>
        public void RemoveLinks(IEnumerable<TimeGraphLinkItem> links, TimeGraphLinkMode mode)
        {
            if (links == null) return;
            if (mode == TimeGraphLinkMode.None) return;
            bool repaint = false;
            TimeGraphLinkMode reMode = TimeGraphLinkMode.All ^ mode;         // reMode nyní obsahuje XOR požadovanou hodnotu, použije se pro AND nulování
            Dictionary<UInt64, LinkInfo> linkDict = this._LinkDict;
            foreach (TimeGraphLinkItem link in links)
            {
                if (link == null) continue;
                UInt64 key = link.Key;
                LinkInfo linkInfo;
                bool exists = linkDict.TryGetValue(key, out linkInfo);
                if (!exists) continue;

                // Z Důvodu zobrazení odebereme zadaný režim, a pokud zůstane None pak odebereme celý prvek Linku:
                linkInfo.Mode &= reMode;                                       // Vstupní hodnota (mode) bude z hodnoty linkInfo.Mode vynulována
                if (linkInfo.Mode == TimeGraphLinkMode.None)                  // A pokud v Mode nezbyla žádná hodnota, link odebereme.
                {
                    linkDict.Remove(key);
                    repaint = true;
                }
            }
            if (repaint)
                this.Repaint();
        }
        /// <summary>
        /// Smaže všechny linky z this paměti. Tím není provedeno jejich odstranění z paměti tabulky, ale pouze z paměti vykreslování.
        /// Jednoduše: linky pro aktuální objekt zhasnou.
        /// </summary>
        public void Clear()
        {
            this._LinkDict.Clear();
            this.Repaint();
        }
        /// <summary>
        /// Obsahuje true, pokud this prvek v sobě obsahuje nějaké linky k vykreslení
        /// </summary>
        public bool ContainLinks { get { return (this._LinkDict.Count > 0); } }
        /// <summary>
        /// Souhrn všech aktuálních linků, bez dalších informací
        /// </summary>
        public IEnumerable<TimeGraphLinkItem> Links { get { return this._LinkDict.Values.Select(l => l.Link); } }
        /// <summary>
        /// Metoda vrací poměr průhlednosti pro daný režim linku.
        /// Průhlednost Linku = hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva).
        /// Na hodnoty průhlednosti má vliv i aktuální režim <see cref="CurrentLinksMode"/>.
        /// </summary>
        /// <param name="itemLinkMode"></param>
        /// <returns></returns>
        internal float GetVisibleRatioForMode(TimeGraphLinkMode itemLinkMode)
        {
            bool isMouseOver = itemLinkMode.HasFlag(TimeGraphLinkMode.MouseOver);

            TimeGraphLinkMode currentMode = this.CurrentLinksMode;
            if (currentMode.HasFlag(TimeGraphLinkMode.Allways))
            {   // Pokud aktuálně vidím všechny Linky, tak budu ignorovat bit Selected, a použiju dvě úrovně průhlednosti - podle přítomnosti myši nad prvkem:
                return (isMouseOver ? LinkVisibleRatioAllwaysWithMouse : LinkVisibleRatioAllwaysStandard);
            }

            // Nejsou zapnuty všechny linky dle režimu, tedy zobrazuji jen Linky pro prvky Selected + MouseOver:
            bool isSelected = itemLinkMode.HasFlag(TimeGraphLinkMode.Selected);
            return (isSelected ?
                     (isMouseOver ? LinkVisibleRatioSelectedWithMouse : LinkVisibleRatioSelectedStandard) :
                     (isMouseOver ? LinkVisibleRatioOnlyWithMouse : LinkVisibleRatioNone));
        }
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Allways, když prvek má aktuálně na sobě myš
        /// </summary>
        protected const float LinkVisibleRatioAllwaysWithMouse = 0.80f;
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Allways, bez myši
        /// </summary>
        protected const float LinkVisibleRatioAllwaysStandard = 0.40f;
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Not-Allways, když prvek je IsSelected a má aktuálně na sobě myš
        /// </summary>
        protected const float LinkVisibleRatioSelectedWithMouse = 0.80f;
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Not-Allways, když prvek je IsSelected a je bez myši
        /// </summary>
        protected const float LinkVisibleRatioSelectedStandard = 0.60f;
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Not-Allways, když prvek není IsSelected a má aktuálně na sobě myš
        /// </summary>
        protected const float LinkVisibleRatioOnlyWithMouse = 0.80f;
        /// <summary>
        /// Hodnota průhlednosti pro Link, zobrazený v globálním režimu Not-Allways, když prvek není IsSelected a je bez myši - takový Link by de facto neměl být zpracováván, protože není důvod jej zobrazit
        /// </summary>
        protected const float LinkVisibleRatioNone = 0.00f;

        #endregion
        #region Subclass LinkInfo: třída pro reálně ukládané prvky - obsahuje navíc i důvod zobrazení a transparentnost
        /// <summary>
        /// LinkInfo: třída pro reálně ukládané prvky - obsahuje navíc i důvod zobrazení a transparentnost
        /// </summary>
        private class LinkInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner">Vlastník grafického linku</param>
            /// <param name="link">Data o linku</param>
            public LinkInfo(TimeGraphLinkArray owner, TimeGraphLinkItem link)
            {
                this._Owner = owner;
                this._Link = link;
                this._Mode = TimeGraphLinkMode.None;
            }
            private TimeGraphLinkArray _Owner;
            private TimeGraphLinkItem _Link;
            private TimeGraphLinkMode _Mode;
            /// <summary>
            /// Objekt vztahu
            /// </summary>
            public TimeGraphLinkItem Link { get { return this._Link; } }
            /// <summary>
            /// Důvod zobrazení: zadává ten, kdo si přeje zobrazit
            /// </summary>
            public TimeGraphLinkMode Mode { get { return this._Mode; } set { this._Mode = value; } }
            /// <summary>
            /// Průhlednost Linku:
            /// hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva).
            /// Hodnotu určuje důvod zobrazení.
            /// </summary>
            public float VisibleRatio
            {
                get { return this._Owner.GetVisibleRatioForMode(this.Mode); }
            }
            /// <summary>
            /// Vykreslí tuto jednu linku
            /// </summary>
            /// <param name="e"></param>
            internal void Draw(GInteractiveDrawArgs e)
            {
                if (this.Link.NeedDraw)
                    this.Link.Draw(e, this.Mode, this.VisibleRatio);
            }
        }
        #endregion
        #region Podpora pro kreslení (InteractiveObject)
        /// <summary>
        /// Souřadnice linků = úplné souřadnice v rámci Parenta
        /// </summary>
        public override Rectangle Bounds
        {
            get
            {
                // Prvek sám nemá žádnou interaktivitu. Proto nemá souřadnice. 
                // Co se týká kreslení, pak díky tomu, že se kreslí do vrstvy Dynamic nebo None (viz StandardDrawToLayer), pak se do vykreslení dostává (viz InteractiveControl.NeedDrawCurrentItem())
                return Rectangle.Empty;
                // return base.Bounds;
                // if (this.Parent == null) return base.Bounds;
                // return new Rectangle(Point.Empty, this.Parent.ClientSize);
            }
            set { base.Bounds = value; }
        }
        /// <summary>
        /// Vrstvy, do nichž se běžně má vykreslovat tento objekt.
        /// Tato hodnota se v metodě <see cref="InteractiveObject.Repaint()"/> použije pro následující vykreslování objektů.
        /// Vrstva <see cref="GInteractiveDrawLayer.Standard"/> je běžná pro normální kreslení;
        /// vrstva <see cref="GInteractiveDrawLayer.Interactive"/> se používá při Drag and Drop;
        /// vrstva <see cref="GInteractiveDrawLayer.Dynamic"/> se používá pro kreslení linek mezi prvky nad vrstvou při přetahování.
        /// Vrstvy lze kombinovat.
        /// Vrstva <see cref="GInteractiveDrawLayer.None"/> je přípustná:  prvek se nekreslí, ale je přítomný a interaktivní.
        /// </summary>
        protected override GInteractiveDrawLayer StandardDrawToLayer { get { return (this.ContainLinks ? GInteractiveDrawLayer.Dynamic : GInteractiveDrawLayer.None); } }
        /// <summary>
        /// Vrstvy, do nichž se aktuálně (tj. v nejbližším kreslení) bude vykreslovat tento objekt.
        /// Po vykreslení se sem ukládá None, tím se šetří čas na kreslení (nekreslí se nezměněné prvky).
        /// </summary>
        protected override GInteractiveDrawLayer RepaintToLayers { get { return this.StandardDrawToLayer; } set { base.RepaintToLayers = value; } }
        /// <summary>
        /// Výchozí metoda pro kreslení prvku, volaná z jádra systému.
        /// </summary>
        /// <param name="e">Kreslící argument</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            if (e.DrawLayer != GInteractiveDrawLayer.Dynamic) return;

            // Najdeme oblast pro kreslení (Clip na oblast grafu, nebo na oblast grafu + dat v tabulce):
            Rectangle clip = this.GetClip(e.GraphicsBounds);

            // Na grafiku nasadíme clip a hladkou kresbu:
            using (GPainter.GraphicsUse(e.Graphics, clip, GraphicSetting.Smooth))
            {
                // Vykreslíme prvky:
                LinkInfo[] linkInfos = this._LinkDict.Values.ToArray();        // Zhmotníme kolekci kvůli enumeraci
                foreach (LinkInfo linkInfo in linkInfos)
                    linkInfo.Draw(e);
            }
        }
        /// <summary>
        /// Metoda najde prostor v absolutních souřadnicích, kam se mají vykreslovat linky:
        /// </summary>
        /// <param name="graphicsBounds"></param>
        /// <returns></returns>
        protected Rectangle GetClip(Rectangle graphicsBounds)
        {
            Rectangle clip = graphicsBounds;

            if (this._OwnerGTable != null)
            {   // Prostor pro data v rámci tabulky:
                Rectangle dataBounds = this._OwnerGTable.GetAbsoluteBoundsForArea(Grids.TableAreaType.RowData);
                clip = Rectangle.Intersect(clip, dataBounds);

                // Pokud v tabulce najdu alespoň jeden sloupec typu Graf (používá časovou osu)...
                var graphColumns = this._OwnerGTable.Columns.Where(c => c.UseTimeAxis).ToArray();
                if (graphColumns != null && graphColumns.Length > 0)
                {   // ...pak zmenším prostor clipu ve směru X pouze na prostor daný všemi grafy v tabulce (on nemusí být pouze jeden):
                    Rectangle c0 = graphColumns[0].ColumnHeader.BoundsAbsolute;  // Souřadnice (X) prvního sloupce s grafem
                    Rectangle c1 = (graphColumns.Length == 1 ? c0 : graphColumns[graphColumns.Length - 1].ColumnHeader.BoundsAbsolute); // Souřadnice (X) posledního sloupce s grafem
                    int x = (c0.X > clip.X ? c0.X : clip.X);                     // Left omezit podle prvního sloupce
                    int r = (c1.Right < clip.Right ? c1.Right : clip.Right);     // Right omezit podle posledního sloupce
                    clip.X = x;
                    clip.Width = (r - x);
                }
            }
            else if (this._OwnerGraph != null)
            {   // Prostor pro data v rámci grafu:
                Rectangle graphBounds = this._OwnerGraph.BoundsAbsolute;
                clip = Rectangle.Intersect(clip, graphBounds);
            }

            return clip;
        }
        #endregion
    }
    #endregion
    #region Enum GTimeGraphLinkMode, interface ITimeGraphLinkDataSource, třída CreateAllLinksArgs pro interface
    /// <summary>
    /// Důvod zobrazení Linku
    /// </summary>
    [Flags]
    public enum TimeGraphLinkMode
    {
        /// <summary>
        /// Není důvod
        /// </summary>
        None = 0,
        /// <summary>
        /// MouseEnter / MouseLeave
        /// </summary>
        MouseOver = 1,
        /// <summary>
        /// IsSelected / deselect
        /// </summary>
        Selected = 2,
        /// <summary>
        /// Požadavek na zobrazení všech linků
        /// </summary>
        Allways = 4,
        /// <summary>
        /// Defaultní 
        /// </summary>
        Default = MouseOver | Selected,
        /// <summary>
        /// Souhrn všech platných hodnot
        /// </summary>
        All = MouseOver | Selected | Allways
    }
    /// <summary>
    /// Deklarace zdroje dat pro linky v situaci, kdy sám prvek <see cref="TimeGraphLinkArray"/> si potřebuje vyžádat soupis vztahů = tedy když je aktivní on.
    /// Typicky to nastává po nasetování hodnoty do <see cref="TimeGraphLinkArray.CurrentLinksMode"/>.
    /// </summary>
    public interface ITimeGraphLinkDataSource
    {
        /// <summary>
        /// Najde a vrátí vztahy prvků pro režim a data dle argumentu
        /// </summary>
        /// <param name="args"></param>
        void CreateLinks(CreateAllLinksArgs args);
    }
    /// <summary>
    /// Data pro metodu <see cref="ITimeGraphLinkDataSource.CreateLinks(CreateAllLinksArgs)"/>
    /// </summary>
    public class CreateAllLinksArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="linksMode"></param>
        /// <param name="selectedItems"></param>
        public CreateAllLinksArgs(TimeGraphLinkMode linksMode, IInteractiveItem[] selectedItems = null)
        {
            this.LinksMode = linksMode;
            this.SelectedItems = selectedItems;
            this.Links = new List<TimeGraphLinkItem>();
        }
        /// <summary>
        /// Požadovaný režim linků
        /// </summary>
        public TimeGraphLinkMode LinksMode { get; private set; }
        /// <summary>
        /// Obsahuje souhrn všech aktuálně selectovaných prvků (=nejen pro daný graf)
        /// </summary>
        public IInteractiveItem[] SelectedItems { get; private set; }
        /// <summary>
        /// Pole linků. Je inicializováno na prázdný List = lze do něj vkládat i odebírat prvky, ale nelze vložit novou instanci.
        /// </summary>
        public List<TimeGraphLinkItem> Links { get; private set; }
    }
    #endregion
    #region class GTimeGraphLink : Datová třída, reprezentující spojení dvou prvků grafu.
    /// <summary>
    /// <see cref="TimeGraphLinkItem"/> : Datová třída, reprezentující spojení dvou prvků grafu.
    /// Nejde v pravém smyslu o interaktivní objekt.
    /// </summary>
    public class TimeGraphLinkItem
    {
        #region Konstrukce a základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TimeGraphLinkItem()
        { }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "GraphLink; IdPrev: " + this.ItemIdPrev + "; IdNext: " + this.ItemIdNext + "; LinkType: " + (this.LinkCenter ? "Center" : "End-Begin") + "; Shape: " + this.LineShape.ToString();
        }
        /// <summary>
        /// ID prvku předchozího
        /// </summary>
        public int ItemIdPrev { get; set; }
        /// <summary>
        /// Vizuální data prvku předchozího
        /// </summary>
        public TimeGraphItem ItemPrev { get; set; }
        /// <summary>
        /// ID prvku následujícího
        /// </summary>
        public int ItemIdNext { get; set; }
        /// <summary>
        /// Vizuální data prvku následujícího
        /// </summary>
        public TimeGraphItem ItemNext { get; set; }
        /// <summary>
        /// Druh spojení prvků: false = spojí se konec Prev a začátek Next (odpovídá <see cref="GuiGraphItemLinkType.PrevEndToNextBegin"/>),
        /// true = spojí se středy prvků (odpovídá <see cref="GuiGraphItemLinkType.PrevCenterToNextCenter"/>).
        /// </summary>
        public bool LinkCenter { get; set; }
        /// <summary>
        /// Tvar spojovací linie (čára, křivka, cikcak), podle zadání v datech.
        /// Pokud je null, pak se bude brát hodnota z lokální konfigrace v procesu kreslení.
        /// Aktuálně platná hodnota je uložena v <see cref="CurrentLineShape"/>.
        /// </summary>
        public LinkLineType? LineShape { get; set; }
        /// <summary>
        /// Tvar spojovací linie (čára, křivka, cikcak), aktuálně platná.
        /// </summary>
        protected LinkLineType CurrentLineShape { get; set; }
        /// <summary>
        /// Šířka linky, nezadáno = 1
        /// </summary>
        public int? LinkWidth { get; set; }
        /// <summary>
        /// Barva linky základní.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je větší nebo rovno Prev.End, pak se použije <see cref="LinkColorStandard"/>.
        /// Další barvy viz <see cref="LinkColorWarning"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorStandard { get; set; }
        /// <summary>
        /// Barva linky varovná.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.End, ale Next.Begin je větší nebo rovno Prev.Begin, pak se použije <see cref="LinkColorWarning"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorError"/>
        /// </summary>
        public Color? LinkColorWarning { get; set; }
        /// <summary>
        /// Barva linky chybová.
        /// Pro typ linky ve směru Prev - Next platí:
        /// v situaci, kdy Next.Begin je menší než Prev.Begin, pak se použije <see cref="LinkColorError"/>.
        /// Další barvy viz <see cref="LinkColorStandard"/> a <see cref="LinkColorWarning"/>
        /// </summary>
        public Color? LinkColorError { get; set; }
        /// <summary>
        /// Data z GUI, nepovinná (zdejší hodnoty jsou separátní)
        /// </summary>
        public GuiGraphLink GuiGraphLink { get; set; }
        #endregion
        #region Obousměrný přístup k prvkům a jejich ID
        /// <summary>
        /// Vrátí ID prvku na dané straně
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <returns></returns>
        internal int GetId(Direction side)
        {
            switch (side)
            {
                case Direction.Negative: return this.ItemIdPrev;
                case Direction.Positive: return this.ItemIdNext;
            }
            return 0;
        }
        /// <summary>
        /// Vrátí prvek na dané straně
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <returns></returns>
        internal TimeGraphItem GetItem(Direction side)
        {
            switch (side)
            {
                case Direction.Negative: return this.ItemPrev;
                case Direction.Positive: return this.ItemNext;
            }
            return null;
        }
        /// <summary>
        /// Uloží daný prvek na danou stranu
        /// </summary>
        /// <param name="side">Strana: Negative = Prev; Positive = Next</param>
        /// <param name="item">Prvek</param>
        /// <returns></returns>
        internal void SetItem(Direction side, TimeGraphItem item)
        {
            switch (side)
            {
                case Direction.Negative:
                    this.ItemPrev = item;
                    break;
                case Direction.Positive:
                    this.ItemNext = item;
                    break;
            }
        }
        /// <summary>
        /// Určí aktuálně platný tvar čáry <see cref="CurrentLineShape"/> podle hodnoty v datech <see cref="LineShape"/>, a pokud není zadáno pak vezme defaultní (parametr).
        /// </summary>
        /// <param name="defaultLineType">Výchozí tvar křivky dle konfigurace</param>
        internal void PrepareCurrentLine(LinkLineType defaultLineType)
        {
            this.CurrentLineShape = this.LineShape ?? defaultLineType;
        }
        #endregion
        #region Klíč linku
        /// <summary>
        /// UInt64 klíč tohoto prvku, obsahuje klíče <see cref="TimeGraphLinkItem.ItemIdPrev"/> a <see cref="TimeGraphLinkItem.ItemIdNext"/>
        /// </summary>
        public UInt64 Key { get { return GetLinkKey(this); } }
        /// <summary>
        /// Vrací složené číslo UInt64 obsahující klíče:
        /// v horních čtyřech bytech = <see cref="TimeGraphLinkItem.ItemIdPrev"/>;
        /// v dolních čtyřech bytech = <see cref="TimeGraphLinkItem.ItemIdNext"/>;
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        protected static UInt64 GetLinkKey(TimeGraphLinkItem link)
        {
            return (link != null ? GetKey(link.ItemIdPrev, link.ItemIdNext) : 0);
        }
        /// <summary>
        /// Vrací složené číslo UInt64 obsahující dvě dodaná čísla Int32:
        /// v horních čtyřech  bytech je uloženo číslo a, v dolních čtyřech bytech pak číslo b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static UInt64 GetKey(int a, int b)
        {
            ulong ua = ((ulong)(a)) & 0xffffffffL;
            ulong ub = ((ulong)(b)) & 0xffffffffL;
            return (ulong)((ua << 32) | ub);
        }
        #endregion
        #region Kreslení jednoho linku
        /// <summary>
        /// Obsahuje true, pokud se má linka kreslit (je viditelná a má oba objekty Prev i Next)
        /// </summary>
        internal bool NeedDraw { get { return (this.IsLinkTypeVisible && this.ItemPrev != null && this.ItemNext != null); } }
        /// <summary>
        /// Obsahuje true, pokud linka podle jejího tvaru je viditelná
        /// </summary>
        internal bool IsLinkTypeVisible { get { return (this.LineShape != LinkLineType.None); } }
        /// <summary>
        /// Vykreslí tuto jednu linku
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        internal void Draw(GInteractiveDrawArgs e, TimeGraphLinkMode mode, float ratio)
        {
            if (this.IsLinkTypeVisible)
            {
                if (this.LinkCenter)
                    this.DrawCenter(e, mode, ratio);
                else
                    this.DrawPrevNext(e, mode, ratio);
            }
        }
        /// <summary>
        /// Vykreslí přímou linku nebo křivku Prev.Center to Next.Center
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        protected void DrawCenter(GInteractiveDrawArgs e, TimeGraphLinkMode mode, float ratio)
        {
            TimeGraph graph = (this.ItemNext != null ? this.ItemNext.Graph : (this.ItemPrev != null ? this.ItemPrev.Graph : null));
            RelationState relationState = GetRelationState(this.ItemPrev, this.ItemNext);
            Color color1 = this.GetColorForState(relationState, graph);

            Point? prevPoint = GetPoint(this.ItemPrev, RectangleSide.CenterX | RectangleSide.CenterY, true, false);
            Point? nextPoint = GetPoint(this.ItemNext, RectangleSide.CenterX | RectangleSide.CenterY, true, false);
            if (!(prevPoint.HasValue && nextPoint.HasValue)) return;

            GPainter.DrawLinkLine(e.Graphics, prevPoint.Value, nextPoint.Value, color1, this.LinkWidth, System.Drawing.Drawing2D.LineCap.Round, System.Drawing.Drawing2D.LineCap.ArrowAnchor, ratio);
        }
        /// <summary>
        /// Vykreslí přímou linku nebo S křivku { Prev.End to Next.Begin }
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="mode">Důvody zobrazení</param>
        /// <param name="ratio">Poměr průhlednosti: hodnota v rozsahu 0.0 (neviditelná) - 1.0 (plná barva)</param>
        protected void DrawPrevNext(GInteractiveDrawArgs e, TimeGraphLinkMode mode, float ratio)
        {
            TimeGraph graph = (this.ItemNext != null ? this.ItemNext.Graph : (this.ItemPrev != null ? this.ItemPrev.Graph : null));
            RelationState relationState = GetRelationState(this.ItemPrev, this.ItemNext);
            Color color1 = this.GetColorForState(relationState, graph);

            Point? prevPoint = GetPoint(this.ItemPrev, RectangleSide.MiddleRight, true, true);
            Point? nextPoint = GetPoint(this.ItemNext, RectangleSide.MiddleLeft, true, true);

            LinkLineType lineType = this.CurrentLineShape;
            float? treshold = 4f * (float)(this.LinkWidth.HasValue ? this.LinkWidth.Value : 3);
            using (System.Drawing.Drawing2D.GraphicsPath graphicsPath = GPainter.CreatePathLink(lineType, prevPoint, nextPoint, treshold))
            {
                bool useRoundAnchor = (lineType == LinkLineType.ZigZagHorizontal || lineType == LinkLineType.ZigZagVertical || lineType == LinkLineType.ZigZagOptimal);
                System.Drawing.Drawing2D.LineCap startCap = (useRoundAnchor ? System.Drawing.Drawing2D.LineCap.RoundAnchor : System.Drawing.Drawing2D.LineCap.Round);
                System.Drawing.Drawing2D.LineCap endCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                GPainter.DrawLinkPath(e.Graphics, graphicsPath, color1, null, this.LinkWidth, startCap, endCap, ratio);
            }
        }
        /// <summary>
        /// Vrátí požadovaný bod, nacházející se na daném místě absolutní souřadnice daného prvku.
        /// Pokud je prvek neviditelný (on, nebo kterýkoli z jeho Parentů), může vrátit null pokud je požadavek "onlyVisible" = true.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="side"></param>
        /// <param name="onlyVisible">true = vracet bod pouze pro objekt, který může být viditelný (z hlediska Is.Visible jeho a všech jeho Parentů)</param>
        /// <param name="shiftInner">Posunout výsledný bod mírně směrem doprostřed objektu</param>
        /// <returns></returns>
        protected static Point? GetPoint(InteractiveObject item, RectangleSide side, bool onlyVisible, bool shiftInner)
        {
            if (item == null) return null;
            BoundsInfo boundsInfo = item.BoundsInfo;
            if (onlyVisible && !boundsInfo.CurrentItemIsVisible) return null;
            Rectangle absBounds = boundsInfo.CurrentItemAbsoluteBounds;
            if (shiftInner)
                absBounds = GetInnerBounds(absBounds);
            return absBounds.GetPoint(side);
        }
        /// <summary>
        /// Vrátí zadané souřadnice mírně zmenšené
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected static Rectangle GetInnerBounds(Rectangle bounds)
        {
            int m = (bounds.GetOrientation() == System.Windows.Forms.Orientation.Horizontal ? bounds.Height : bounds.Width) / 3;   // Menší z (Width, Height)
            if (m > 6) m = 6;
            return bounds.Enlarge(-m);
        }
        /// <summary>
        /// Vrací stav popisující vztah času Prev a Next
        /// </summary>
        /// <param name="itemPrev"></param>
        /// <param name="itemNext"></param>
        /// <returns></returns>
        protected static RelationState GetRelationState(TimeGraphItem itemPrev, TimeGraphItem itemNext)
        {
            if (itemPrev == null || itemNext == null) return RelationState.Standard;
            TimeRange timePrev = itemPrev.Item.Time;
            TimeRange timeNext = itemNext.Item.Time;
            if (timeNext.Begin.Value >= timePrev.End.Value) return RelationState.Standard;
            if (timeNext.Begin.Value >= timePrev.Begin.Value) return RelationState.Warning;
            return RelationState.Error;
        }
        /// <summary>
        /// Vrací barvu pro daný čas
        /// </summary>
        /// <param name="state"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        protected Color GetColorForState(RelationState state, TimeGraph graph = null)
        {
            switch (state)
            {
                case RelationState.Warning:
                    return (this.LinkColorWarning.HasValue ? this.LinkColorWarning.Value :
                           (graph != null && graph.LinkColorWarning.HasValue ? graph.LinkColorWarning.Value : Skin.Graph.LinkColorWarning));
                case RelationState.Error:
                    return (this.LinkColorError.HasValue ? this.LinkColorError.Value :
                           (graph != null && graph.LinkColorError.HasValue ? graph.LinkColorError.Value : Skin.Graph.LinkColorError));
                case RelationState.Standard:
                default:
                    return (this.LinkColorStandard.HasValue ? this.LinkColorStandard.Value :
                           (graph != null && graph.LinkColorStandard.HasValue ? graph.LinkColorStandard.Value : Skin.Graph.LinkColorStandard));
            }
        }
        /// <summary>
        /// Vztah prvků Prev - Next z hlediska času a určení barvy
        /// </summary>
        protected enum RelationState
        {
            /// <summary>
            /// Neurčen
            /// </summary>
            None,
            /// <summary>
            /// Standardní, kdy Next.Begin je v nebo za časem Prev.End
            /// </summary>
            Standard,
            /// <summary>
            /// Varování, kdy Next.Begin je v nebo za časem Prev.Begin, ale dříve než Prev.End
            /// </summary>
            Warning,
            /// <summary>
            /// Chyba, kdy Next.Begin je před časem Prev.Begin
            /// </summary>
            Error
        }
        #endregion
    }
    #endregion
}
