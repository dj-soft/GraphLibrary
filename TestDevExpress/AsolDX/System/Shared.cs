using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XmlSerializer = Noris.WS.Parser.XmlSerializer;

namespace Noris.WS.DataContracts.Desktop.Forms
{
    #region třídy layoutu: FormLayout, Area; enum AreaContentType
    /// <summary>
    /// Deklarace layoutu: obsahuje popis okna a popis rozvržení vnitřních prostor
    /// </summary>
    internal class FormLayout
    {
        /// <summary>
        /// Okno je tabované (true) nebo plovoucí (false)
        /// </summary>
        public bool? IsTabbed { get; set; }
        /// <summary>
        /// Stav okna (Maximized / Normal); stav Minimized se sem neukládá, za stavu <see cref="IsTabbed"/> se hodnota ponechá na předešlé hodnotě
        /// </summary>
        public FormWindowState? FormState { get; set; }
        /// <summary>
        /// Souřadnice okna platné při <see cref="FormState"/> == <see cref="FormWindowState.Normal"/> a ne <see cref="IsTabbed"/>
        /// </summary>
        public System.Drawing.Rectangle? FormNormalBounds { get; set; }
        /// <summary>
        /// Zoom aktuální
        /// </summary>
        public decimal Zoom { get; set; }
        /// <summary>
        /// Laoyut struktury prvků - základní úroveň, obsahuje rekurzivně další instance <see cref="Area"/>
        /// </summary>
        public Area RootArea { get; set; }
    }
    /// <summary>
    /// Rozložení pracovní plochy, jedna plocha a její využití, rekurzivní třída.
    /// Obsah této třídy se persistuje do XML.
    /// POZOR tedy: neměňme jména [PropertyName("xxx")], jejich hodnoty jsou uloženy v XML tvaru na serveru 
    /// a podle atributu PropertyName budou načítána do aktuálních properties.
    /// Lze měnit jména properties.
    /// <para/>
    /// Obecně k XML persistoru: není nutno používat atribut [PropertyName("xxx")], ale pak musíme zajistit neměnnost názvů properties ve třídě.
    /// </summary>
    internal class Area
    {
        #region Data
        /// <summary>
        /// ID prostoru
        /// </summary>
        [XmlSerializer.PropertyName("AreaID")]
        public string AreaId { get; set; }
        /// <summary>
        /// Typ obsahu = co v prostoru je
        /// </summary>
        [XmlSerializer.PropertyName("Content")]
        public AreaContentType ContentType { get; set; }
        /// <summary>
        /// Uživatelský identifikátor
        /// </summary>
        [XmlSerializer.PropertyName("ControlID")]
        public string ControlId { get; set; }
        /// <summary>
        /// Text controlu, typicky jeho titulek
        /// </summary>
        [XmlSerializer.PersistingEnabled(false)]
        public string ContentText { get; set; }
        /// <summary>
        /// Orientace splitteru
        /// </summary>
        [XmlSerializer.PropertyName("SplitterOrientation")]
        public Orientation? SplitterOrientation { get; set; }
        /// <summary>
        /// Fixovaný splitter?
        /// </summary>
        [XmlSerializer.PropertyName("IsSplitterFixed")]
        public bool? IsSplitterFixed { get; set; }
        /// <summary>
        /// Fixovaný panel
        /// </summary>
        [XmlSerializer.PropertyName("FixedPanel")]
        public FixedPanel? FixedPanel { get; set; }
        /// <summary>
        /// Minimální velikost pro Panel1
        /// </summary>
        [XmlSerializer.PropertyName("MinSize1")]
        public int? MinSize1 { get; set; }
        /// <summary>
        /// Minimální velikost pro Panel2
        /// </summary>
        [XmlSerializer.PropertyName("MinSize2")]
        public int? MinSize2 { get; set; }
        /// <summary>
        /// Pozice splitteru absolutní, zleva nebo shora
        /// </summary>
        [XmlSerializer.PropertyName("SplitterPosition")]
        public int? SplitterPosition { get; set; }
        /// <summary>
        /// Rozsah pohybu splitteru (šířka nebo výška prostoru).
        /// Podle této hodnoty a podle <see cref="FixedPanel"/> je následně restorována pozice při vkládání layoutu do nového objektu.
        /// <para/>
        /// Pokud původní prostor měl šířku 1000 px, pak zde je 1000. Pokud fixovaný panel byl Panel2, je to uvedeno v <see cref="FixedPanel"/>.
        /// Pozice splitteru zleva byla např. 420 (v <see cref="SplitterPosition"/>). Šířka fixního panelu tedy je (1000 - 420) = 580.
        /// Nyní budeme restorovat XmlLayout do nového prostoru, jehož šířka není 1000, ale 800px.
        /// Protože fixovaný panel je Panel2 (vpravo), pak nová pozice splitteru (zleva) je taková, aby Panel2 měl šířku stejnou jako původně (580): 
        /// nově tedy (800 - 580) = 220.
        /// <para/>
        /// Obdobné přepočty budou provedeny pro jinou situaci, kdy FixedPanel je None = splitter ke "gumový" = proporcionální.
        /// Pak se při restoru přepočte nová pozice splitteru pomocí poměru původní pozice ku Range.
        /// </summary>
        [XmlSerializer.PropertyName("SplitterRange")]
        public int? SplitterRange { get; set; }
        /// <summary>
        /// Obsah panelu 1 (rekurzivní instance téže třídy)
        /// </summary>
        [XmlSerializer.PropertyName("Content1")]
        public Area Content1 { get; set; }
        /// <summary>
        /// Obsah panelu 2 (rekurzivní instance téže třídy)
        /// </summary>
        [XmlSerializer.PropertyName("Content2")]
        public Area Content2 { get; set; }
        #endregion
        #region IsEqual
        public static bool IsEqual(Area area1, Area area2)
        {
            if (!_IsEqualNull(area1, area2)) return false;     // Jeden je null a druhý není
            if (area1 == null) return true;                    // area1 je null (a druhý taky) = jsou si rovny

            if (area1.ContentType != area2.ContentType) return false;    // Jiný druh obsahu
                                                                         // Obě area mají shodný typ obsahu:
            bool contentIsSplitted = (area1.ContentType == AreaContentType.DxSplitContainer || area1.ContentType == AreaContentType.WfSplitContainer);
            if (!contentIsSplitted) return true;               // Obsah NENÍ split container = z hlediska porovnání layoutu na koncovém obsahu nezáleží, jsou si rovny.

            // Porovnáme deklaraci vzhledu SplitterContaineru:
            if (!_IsEqualNullable(area1.SplitterOrientation, area1.SplitterOrientation)) return false;
            if (!_IsEqualNullable(area1.IsSplitterFixed, area1.IsSplitterFixed)) return false;
            if (!_IsEqualNullable(area1.FixedPanel, area1.FixedPanel)) return false;
            if (!_IsEqualNullable(area1.MinSize1, area1.MinSize1)) return false;
            if (!_IsEqualNullable(area1.MinSize2, area1.MinSize2)) return false;

            // Porovnáme deklarovanou pozici splitteru:
            if (area1._SplitterPositionComparable != area2._SplitterPositionComparable) return false;

            // Porovnáme rekurzivně definice :
            if (!IsEqual(area1.Content1, area2.Content1)) return false;
            if (!IsEqual(area1.Content2, area2.Content2)) return false;

            return true;
        }
        private static bool _IsEqualNull(object a, object b)
        {
            bool an = a is null;
            bool bn = b is null;
            return (an == bn);
        }
        private static bool _IsEqualNullable<T>(T? a, T? b) where T : struct, IComparable
        {
            bool av = a.HasValue;
            bool bv = b.HasValue;
            if (av && bv) return (a.Value.CompareTo(b.Value) == 0);         // Obě mají hodnotu: výsledek = jsou si hodnoty rovny?
            if (av || bv) return false;         // Některý má hodnotu? false, protože jen jeden má hodnotu (kdyby měly hodnotu oba, skončili bychom dřív)
            return true;                        // Obě jsou null
        }
        private int _SplitterPositionComparable
        {
            get
            {
                var fixedPanel = this.FixedPanel ?? Forms.FixedPanel.Panel1;
                switch (fixedPanel)
                {
                    case Forms.FixedPanel.Panel1:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value;
                        return 0;
                    case Forms.FixedPanel.Panel2:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value - this.SplitterRange.Value;
                        return 0;
                    case Forms.FixedPanel.None:
                        if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue && this.SplitterRange.Value > 0) return this.SplitterPosition.Value * 10000 / this.SplitterRange.Value;
                        return 0;
                }
                return 0;
            }
        }
        #endregion
    }
    /// <summary>
    /// Typ obsahu prostoru
    /// </summary>
    internal enum AreaContentType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Žádný control
        /// </summary>
        Empty,
        /// <summary>
        /// Prázdný DxLayoutItemPanel
        /// </summary>
        EmptyLayoutPanel,
        /// <summary>
        /// Standardní naplněný DxLayoutItemPanel
        /// </summary>
        DxLayoutItemPanel,
        /// <summary>
        /// SplitContainer typu DevExpress
        /// </summary>
        DxSplitContainer,
        /// <summary>
        /// SplitContainer typu WinForm
        /// </summary>
        WfSplitContainer
    }
    /// <summary>
    /// Specifies how a form window is displayed.
    /// Like: System.Windows.Forms.FormWindowState
    /// </summary>
    public enum FormWindowState
    {
        /// <summary>
        /// A default sized window.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// A minimized window.
        /// </summary>
        Minimized = 1,
        /// <summary>
        /// A maximized window.
        /// </summary>
        Maximized = 2
    }
    /// <summary>
    /// Specifies the orientation of controls or elements of controls.
    /// Like:  System.Windows.Forms.Orientation
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// The control or element is oriented horizontally.
        /// </summary>
        Horizontal = 0,
        /// <summary>
        /// The control or element is oriented vertically.
        /// </summary>
        Vertical = 1
    }
    /// <summary>
    /// Specifies that System.Windows.Forms.SplitContainer.Panel1, System.Windows.Forms.SplitContainer.Panel2, or neither panel is fixed.
    /// Like:  System.Windows.Forms.FixedPanel
    /// </summary>
    public enum FixedPanel
    {
        /// <summary>
        /// Specifies that neither System.Windows.Forms.SplitContainer.Panel1, System.Windows.Forms.SplitContainer.Panel2 is fixed. A System.Windows.Forms.Control.Resize event affects both panels.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that System.Windows.Forms.SplitContainer.Panel1 is fixed. A System.Windows.Forms.Control.Resize event affects only System.Windows.Forms.SplitContainer.Panel2.
        /// </summary>
        Panel1 = 1,
        /// <summary>
        /// Specifies that System.Windows.Forms.SplitContainer.Panel2 is fixed. A System.Windows.Forms.Control.Resize event affects only System.Windows.Forms.SplitContainer.Panel1.
        /// </summary>
        Panel2 = 2
    }
    #endregion
}
namespace Noris.WS.DataContracts.Desktop.Data
{
    using System.Drawing;
    #region SvgImageArrayInfo a SvgImageArrayItem : Třída, která obsahuje data o sadě ikon SVG, pro jejich kombinaci do jedné výsledné ikony
    /// <summary>
    /// Třída, která obsahuje data o sadě ikon SVG, pro jejich kombinaci do jedné výsledné ikony (základní ikona plus jiná ikona jako její overlay).
    /// </summary>
    internal class SvgImageArrayInfo
    {
        #region Tvorba a public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageArrayInfo()
        {
            Items = new List<SvgImageArrayItem>();
        }
        /// <summary>
        /// Konstruktor, rovnou přidá první obrázek do plného umístění (100%)
        /// </summary>
        /// <param name="name"></param>
        public SvgImageArrayInfo(string name)
            : this()
        {
            Add(name);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Count: {Items.Count}";
        }
        /// <summary>
        /// Pole jednotlivých obrázků a jejich umístění
        /// </summary>
        public List<SvgImageArrayItem> Items { get; private set; }
        /// <summary>
        /// Přidá další obrázek, v plném rozměru
        /// </summary>
        /// <param name="name"></param>
        public void Add(string name)
        {
            if (!String.IsNullOrEmpty(name))
                Items.Add(new SvgImageArrayItem(name));
        }
        /// <summary>
        /// Přidá další obrázek, do daného prostoru.
        /// Velikost musí být nejméně 10, jinak nebude provedeno.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        public void Add(string name, Rectangle bounds)
        {
            if (!String.IsNullOrEmpty(name) && bounds.Width >= 10 && bounds.Height >= 10)
                Items.Add(new SvgImageArrayItem(name, bounds));
        }
        /// <summary>
        /// Přidá další obrázek, do daného umístění.
        /// Velikost musí být nejméně 10, jinak nebude provedeno.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contentAlignment"></param>
        /// <param name="percent"></param>
        public void Add(string name, ContentAlignment contentAlignment, int percent = 50)
        {
            if (!String.IsNullOrEmpty(name) && percent >= 10)
                Items.Add(new SvgImageArrayItem(name, GetRectangle(contentAlignment, percent)));
        }
        /// <summary>
        /// Přidá další prvek.
        /// </summary>
        /// <param name="item"></param>
        public void Add(SvgImageArrayItem item)
        {
            if (item != null)
                Items.Add(item);
        }
        /// <summary>
        /// Obsahuje true, pokud je objekt prázdný
        /// </summary>
        public bool IsEmpty { get { return (Items.Count == 0); } }
        /// <summary>
        /// Vymaže všechny obrázky
        /// </summary>
        public void Clear() { Items.Clear(); }
        #endregion
        #region Podpora
        /// <summary>
        /// Oddělovač dvou prvků <see cref="SvgImageArrayItem.Key"/> v rámci jednoho <see cref="SvgImageArrayInfo.Key"/>
        /// </summary>
        internal const string KeySplitDelimiter = KeyItemEnd + KeyItemBegin;
        /// <summary>
        /// Značka Begin jednoho prvku
        /// </summary>
        internal const string KeyItemBegin = "«";
        /// <summary>
        /// Značka End jednoho prvku
        /// </summary>
        internal const string KeyItemEnd = "»";
        /// <summary>
        /// Vrátí souřadnici prostoru v dané relativní pozici k základnímu prostoru { 0, 0, 100, 100 }.
        /// Lze specifikovat velikost cílového prostoru, ta musí být v rozmezí 16 až <see cref="BaseSize"/> (včetně).
        /// Jde o prostor, do kterého se promítne ikona, v rámci finální velikosti <see cref="BaseSize"/> x <see cref="BaseSize"/>.
        /// </summary>
        /// <param name="contentAlignment"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Rectangle GetRectangle(ContentAlignment contentAlignment, int percent = 50)
        {
            percent = (percent < 10 ? 10 : (percent > 100 ? 100 : percent));   // Platné rozmezí procent je 10 až 100
            int size = SvgImageArrayInfo.BaseSize * percent / 100;             // Procento => velikost v rozsahu 0-120
            int de = BaseSize - size;                                          // Velikost celého volného prostoru
            int dc = de / 2;                                                   // Velikost pro Center
            switch (contentAlignment)
            {
                case ContentAlignment.TopLeft: return new Rectangle(0, 0, size, size);
                case ContentAlignment.TopCenter: return new Rectangle(dc, 0, size, size);
                case ContentAlignment.TopRight: return new Rectangle(de, 0, size, size);
                case ContentAlignment.MiddleLeft: return new Rectangle(0, dc, size, size);
                case ContentAlignment.MiddleCenter: return new Rectangle(dc, dc, size, size);
                case ContentAlignment.MiddleRight: return new Rectangle(de, dc, size, size);
                case ContentAlignment.BottomLeft: return new Rectangle(0, de, size, size);
                case ContentAlignment.BottomCenter: return new Rectangle(dc, de, size, size);
                case ContentAlignment.BottomRight: return new Rectangle(de, de, size, size);
            }
            return new Rectangle(dc, dc, size, size);
        }
        /// <summary>
        /// Základní velikost
        /// </summary>
        public const int BaseSize = 120;
        #endregion
        #region Serializace
        /// <summary>
        /// Obsahuje (vygeneruje) serializovaný string z this instance
        /// </summary>
        public string Serial { get { return XmlSerializer.Persist.Serialize(this, XmlSerializer.PersistArgs.MinimalXml); } }
        /// <summary>
        /// Klíč: obsahuje klíče všech obrázků <see cref="SvgImageArrayItem.Key"/>.
        /// Lze jej použít jako klíč do Dictionary, protože dvě instance <see cref="SvgImageArrayInfo"/> se stejným klíčem budou mít stejný vzhled výsledného obrázku.
        /// </summary>
        public string Key
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in Items)
                    sb.Append(item.Key);
                string key = sb.ToString();
                return key;
            }
        }
        /// <summary>
        /// Zkusí provést deserializaci
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryDeserialize(string serial, out SvgImageArrayInfo result)
        {   // <?xml version="1.0" encoding="utf-16"?><id-persistent Version="2.00"><id-data><id-value id-value.Type="Noris.Clients.Win.Components.AsolDX.SvgImageArrayInfo"><Items><id-item><id-value ImageName="devav/actions/printexcludeevaluations.svg" /></id-item><id-item><id-value ImageName="devav/actions/about.svg" ImageRelativeBounds="60;60;60;60" /></id-item></Items></id-value></id-data></id-persistent>
            if (!String.IsNullOrEmpty(serial))
            {
                serial = serial.Trim();
                if (serial.StartsWith("<?xml version=") && serial.EndsWith("</id-persistent>"))
                {   // Ze Serial:
                    object data = XmlSerializer.Persist.Deserialize(serial);
                    if (data != null && data is SvgImageArrayInfo array)
                    {
                        result = array;
                        return true;
                    }
                }
                else if (serial.StartsWith(SvgImageArrayInfo.KeyItemBegin) && serial.EndsWith(SvgImageArrayInfo.KeyItemEnd))   //  serial.Contains(SvgImageArrayInfo.KeySplitDelimiter))
                {   // Z Key = ten je ve tvaru:  «name1»«name2<X.Y.W.H>»    rozdělím v místě oddělovače »« ,  získám dva prvky   «name1    a    name2<X.Y.W.H>»   (v prvcích tedy může / nemusí být značka   «   anebo   »     (nemusí být u druhého prvku ze tří :-) )
                    SvgImageArrayInfo array = new SvgImageArrayInfo();
                    string[] serialItems = serial.Split(new string[] { SvgImageArrayInfo.KeySplitDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serialItem in serialItems)
                    {
                        if (SvgImageArrayItem.TryDeserialize(serialItem, out SvgImageArrayItem item))
                            array.Add(item);
                    }
                    if (!array.IsEmpty)
                    {
                        result = array;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
        #endregion
    }
    /// <summary>
    /// Jedna ikona, obsažená v <see cref="SvgImageArrayInfo"/> = název ikony <see cref="ImageName"/>
    /// a její relativní umístění v prostoru výsledné ikony <see cref="ImageRelativeBounds"/>.
    /// </summary>
    internal class SvgImageArrayItem
    {
        #region Konstruktor a public data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public SvgImageArrayItem()
        {   // Toto používá víceméně jen deserializace
            ImageName = "";
            ImageRelativeBounds = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        public SvgImageArrayItem(string name)
        {
            ImageName = name;
            ImageRelativeBounds = null;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bounds"></param>
        public SvgImageArrayItem(string name, Rectangle bounds)
        {
            ImageName = name;
            ImageRelativeBounds = bounds;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = $"Name: {ImageName}";
            if (ImageRelativeBounds.HasValue)
                text += $"; Bounds: {ImageRelativeBounds}";
            return text;
        }
        /// <summary>
        /// Pokusí se z dodaného stringu vytvořit a vrátit new instanci.
        /// String se očekává ve formě <see cref="Key"/>.
        /// </summary>
        /// <param name="serialItem"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static bool TryDeserialize(string serialItem, out SvgImageArrayItem item)
        {
            item = null;
            if (String.IsNullOrEmpty(serialItem)) return false;

            serialItem = serialItem
                .Replace(SvgImageArrayInfo.KeyItemBegin, "")
                .Replace(SvgImageArrayInfo.KeyItemEnd, "")
                .Trim();                                      // Odstraníme zbývající Begin a End značky   «   a   »  (pokud tam jsou)
            if (!serialItem.StartsWith("@") && serialItem.IndexOfAny("*?:\t\r\n".ToCharArray()) >= 0) return false;  // Znakem @ začíná GenericSvg, tam jsou pravidla mírnější...

            var parts = serialItem.Split('<', '>');           // Z textu "imagename<0.0.60.30>" vytvořím tři prvky:    "imagename",    "0.0.60.30",    ""
            int count = parts.Length;
            if (parts.Length == 0) return false;
            string name = parts[0];
            if (String.IsNullOrEmpty(name)) return false;
            name = name.Trim();
            Rectangle? bounds = null;
            if (parts.Length > 1)
            {
                var coords = parts[1].Split('.');             // "0.0.60.30";
                if (coords.Length == 4)
                {
                    if (Int32.TryParse(coords[0], out int x) && (x >= 0 && x <= 120) &&
                        Int32.TryParse(coords[1], out int y) && (y >= 0 && y <= 120) &&
                        Int32.TryParse(coords[2], out int w) && (w >= 0 && w <= 120) &&
                        Int32.TryParse(coords[3], out int h) && (h >= 0 && h <= 120))
                        bounds = new Rectangle(x, y, w, h);
                }
            }
            if (!bounds.HasValue)
                item = new SvgImageArrayItem(name);
            else
                item = new SvgImageArrayItem(name, bounds.Value);
            return true;
        }
        /// <summary>
        /// Jméno SVG obrázku
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Souřadnice umístění obrázku v cílovém prostoru { 0, 0, <see cref="SvgImageArrayInfo.BaseSize"/>, <see cref="SvgImageArrayInfo.BaseSize"/> }.
        /// Pokud je zde null, bude obrázek umístěn do celého prostoru.
        /// </summary>
        public Rectangle? ImageRelativeBounds { get; set; }
        /// <summary>
        /// Klíč: obsahuje název obrázku a cílový prostor <see cref="ImageRelativeBounds"/>, pokud je zadán, ve formě:
        /// «image&lt;X.Y.W.H&gt;»
        /// </summary>
        public string Key
        {
            get
            {
                string key = SvgImageArrayInfo.KeyItemBegin + this.ImageName.Trim().ToLower();
                if (ImageRelativeBounds.HasValue)
                {
                    var bounds = ImageRelativeBounds.Value;
                    key += $"<{bounds.X}.{bounds.Y}.{bounds.Width}.{bounds.Height}>";
                }
                key += SvgImageArrayInfo.KeyItemEnd;
                return key;
            }
        }
        #endregion
    }
    #endregion
}
