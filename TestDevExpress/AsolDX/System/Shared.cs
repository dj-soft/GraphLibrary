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
