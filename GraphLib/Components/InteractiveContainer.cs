using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Obecný předek pro všechny prvky, které obsahují kolekci svých Child prvků.
    /// Tato třída neobsahuje vizuální styl, protože je určena k implementaci konkrétních potomků a ti si budou definovat svůj konkrétní vizuální styl. Například <see cref="InteractiveLabeledContainer"/>.
    /// </summary>
    public class InteractiveContainer : InteractiveObject, IInteractiveItem, IInteractiveParent
    {
        #region Constructor
        /// <summary>
        /// InteractiveContainer constructor : create ItemList
        /// </summary>
        public InteractiveContainer() : base()
        {
            this._ChildList = new EList<IInteractiveItem>();
            this._ChildList.ItemAddBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemAddBefore);
            this._ChildList.ItemAddAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemAddAfter);
            this._ChildList.ItemRemoveBefore += new EList<IInteractiveItem>.EListEventBeforeHandler(_ItemList_ItemRemoveBefore);
            this._ChildList.ItemRemoveAfter += new EList<IInteractiveItem>.EListEventAfterHandler(_ItemList_ItemRemoveAfter);
        }
        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + "; " + this._ChildList.Count.ToString() + " child items";
        }
        #endregion
        #region Items Add/Remove events
        /// <summary>
        /// Zajistí akce před přidáním prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemAddBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            args.Item.Parent = this;
            this.OnItemAddBefore(args);
            if (this.ItemAddBefore != null)
                this.ItemAddBefore(this, args);
        }
        /// <summary>
        /// Háček volaný před přidáním prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemAddBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        /// <summary>
        /// Event volaný před přidáním prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemAddBefore;
        /// <summary>
        /// Zajistí akce po přidání prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemAddAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemAddAfter(args);
            if (this.ItemAddAfter != null)
                this.ItemAddAfter(this, args);
        }
        /// <summary>
        /// Háček volaný po přidání prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemAddAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        /// <summary>
        /// Event volaný po přidání prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemAddAfter;
        /// <summary>
        /// Zajistí akce před odebráním prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemRemoveBefore(object sender, EList<IInteractiveItem>.EListBeforeEventArgs args)
        {
            this.OnItemRemoveBefore(args);
            if (this.ItemRemoveBefore != null)
                this.ItemRemoveBefore(this, args);
        }
        /// <summary>
        /// Háček volaný před odebráním prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemRemoveBefore(EList<IInteractiveItem>.EListBeforeEventArgs args) { }
        /// <summary>
        /// Event volaný před odebráním prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventBeforeHandler ItemRemoveBefore;
        /// <summary>
        /// Zajistí akce po odebrání prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void _ItemList_ItemRemoveAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            this.OnItemRemoveAfter(args);
            if (this.ItemRemoveAfter != null)
                this.ItemRemoveAfter(this, args);
            args.Item.Parent = null;
        }
        /// <summary>
        /// Háček volaný po odebrání prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemRemoveAfter(EList<IInteractiveItem>.EListAfterEventArgs args) { }
        /// <summary>
        /// Event volaný po odebrání prvku
        /// </summary>
        public event EList<IInteractiveItem>.EListEventAfterHandler ItemRemoveAfter;

        #endregion
        #region Items
        /// <summary>
        /// Child prvky tohoto prvku
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this.ChildList; } }
        /// <summary>
        /// Interactive items in this container.
        /// Any collection can be stored.
        /// Set of value does not trigger Draw().
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<IInteractiveItem> Items
        {
            get { return this.ChildList; }
            set
            {
                this._ChildList.ClearSilent();
                this._ChildList.AddRangeSilent(value);
            }
        }
        /// <summary>
        /// Add one interactive item.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="item"></param>
        public virtual void AddItem(IInteractiveItem item)
        {
            this.ChildList.Add(item);
        }
        /// <summary>
        /// Odebere daný Child prvek
        /// </summary>
        /// <param name="item"></param>
        public virtual void RemoveItem(IInteractiveItem item)
        {
            this.ChildList.Remove(item);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(params IInteractiveItem[] items)
        {
            this.ChildList.AddRange(items);
        }
        /// <summary>
        /// Add more interactive items.
        /// Does not trigger Draw().
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(IEnumerable<IInteractiveItem> items)
        {
            this.ChildList.AddRange(items);
        }
        /// <summary>
        /// Vymaže všechny prvky z pole Items.
        /// </summary>
        public virtual void ClearItems()
        {
            this.ChildList.Clear();
        }
        /// <summary>
        /// Soupis Child prvků
        /// </summary>
        protected EList<IInteractiveItem> ChildList
        {
            get
            {
                if (this._ChildList == null)
                {
                    this._ChildList = new EList<IInteractiveItem>();
                    this._ChildList.ItemAddAfter += _ChildList_ItemAddAfter;
                    this._ChildList.ItemRemoveAfter += _ChildList_ItemRemoveAfter;
                }
                return this._ChildList;
            }
        }
        private void _ChildList_ItemAddAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            args.Item.Parent = this;
        }
        private void _ChildList_ItemRemoveAfter(object sender, EList<IInteractiveItem>.EListAfterEventArgs args)
        {
            args.Item.Parent = null;
        }
        private EList<IInteractiveItem> _ChildList;
        #endregion
        #region Interactive property and methods
       
        #endregion
        #region Draw to Graphic
        /// <summary>
        /// InteractiveContainer.Draw(): call PaintBackground
        /// </summary>
        /// <param name="e"></param>
        /// <param name="absoluteBounds"></param>
        /// <param name="absoluteVisibleBounds"></param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds)
        {
            this.PaintBackground(e);
            base.Draw(e, absoluteBounds, absoluteVisibleBounds);
        }
        /// <summary>
        /// Vykreslení pozadí
        /// </summary>
        /// <param name="e"></param>
        protected virtual void PaintBackground(GInteractiveDrawArgs e)
        {
        }
        #endregion
    }
    #region InteractiveLabeledContainer : Interaktivní container, nabízející Label a titulkovou oddělovací čáru
    /// <summary>
    /// <see cref="InteractiveLabeledContainer"/> : Interaktivní container, nabízející Label a titulkovou oddělovací čáru
    /// </summary>
    public class InteractiveLabeledContainer : InteractiveContainer
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public InteractiveLabeledContainer()
        {
        }

        #region Vizuální styl objektu
        /// <summary>
        /// Styl tohoto konkrétního panelu. 
        /// Zahrnuje veškeré vizuální vlastnosti.
        /// Výchozí je null, pak se styl přebírá z <see cref="StyleParent"/>, anebo společný z <see cref="Styles.Panel"/>.
        /// Prvním čtením této property se vytvoří new instance. Lze ji tedy kdykoliv přímo použít.
        /// <para/>
        /// Do této property se typicky vkládá new instance, která řeší vzhled jednoho konkrétního prvku.
        /// </summary>
        public PanelStyle Style { get { if (_Style == null) _Style = new PanelStyle(); return _Style; } set { _Style = null; } } private PanelStyle _Style;
        /// <summary>
        /// Obsahuje true tehdy, když <see cref="Style"/> je pro tento objekt deklarován.
        /// </summary>
        public bool HasStyle { get { return (_Style != null && !_Style.IsEmpty); } }
        /// <summary>
        /// Společný styl, deklarovaný pro více panelů. 
        /// Zde je reference na tuto instanci. 
        /// Modifikace hodnot v této instanci se projeví ve všech ostatních textboxech, které ji sdílejí.
        /// Výchozí je null, pak se styl přebírá společný z <see cref="Styles.Panel"/>.
        /// <para/>
        /// Do této property se typicky vkládá odkaz na instanci, která je primárně uložena jinde, a řeší vzhled ucelené skupiny prvků.
        /// </summary>
        public PanelStyle StyleParent { get; set; }
        /// <summary>
        /// Aktuální styl, nikdy není null. 
        /// Obsahuje <see cref="Style"/> ?? <see cref="StyleParent"/> ?? <see cref="Styles.Panel"/>.
        /// </summary>
        protected IPanelStyle StyleCurrent { get { return (this._Style ?? this.StyleParent ?? Styles.Panel); } }
        #endregion
        #region Vizuální vlastnosti containeru
        /// <summary>
        /// Aktuální barva pozadí, používá se při kreslení. Potomek může přepsat...
        /// </summary>
        protected override Color CurrentBackColor
        {
            get
            {
                var style = this.StyleCurrent;
                var backColor = style.GetBackColor(this.InteractiveState);
                return backColor;
            }
        }
        #endregion
        #region TitleLabel a TitleLine, a vizuální styly pro TitleLabel
        /// <summary>
        /// Obsahuje true, pokud je viditelný label <see cref="TitleLabel"/>.
        /// Pokud objekt není viditelný (<see cref="TitleLabelVisible"/> je false), pak objekt <see cref="TitleLabel"/> je null.
        /// Výchozí hodnota <see cref="TitleLabelVisible"/> je false = instance labelu neexistuje a šetří se tak paměť.
        /// Pokud je label nějakou dobu používán a pak je nastaveno <see cref="TitleLabelVisible"/> = false, pak se label zahodí i se všemi nastavenými vlastnostmi,
        /// a následné nastavení <see cref="TitleLabelVisible"/> = true vygeneruje new instanci s výchozími hodnotami.
        /// Pokud chceme zapínat a vypínat viditelnost, ale ponechat vlastnosti, pak je správné řídit přímo viditelnost Labelu <see cref="TitleLabel"/>.Visible.
        /// </summary>
        public bool TitleLabelVisible
        {
            get { return (_TitleLabel != null ? _TitleLabel.Visible : false); }
            set
            {
                if (value)
                {   // Zobrazit: použijeme property TitleLabel, tam se v případě potřeby vytvoří new instance, a do ní vložíme Visible = true:
                    TitleLabel.Visible = true;
                }
                else
                {   // Zneviditelnit = zlikvidovat:
                    if (_TitleLabel != null)
                    {
                        this.RemoveItem(_TitleLabel);
                        _TitleLabel = null;
                    }
                }
            }
        }
        /// <summary>
        /// Titulkový label. Pokud je čtena hodnota, která by dosud byla null (tzn. <see cref="TitleLabelVisible"/> je false), pak bude nejprve vytvořena new instance a ta vrácena,
        /// ale její vlastní Visible bude false.
        /// <para/>
        /// Důsledek: dokud nebude použit titulek (nastaven jeho text atd), nebude vytvořena jeho instance (úspora paměti).
        /// </summary>
        public Label TitleLabel
        {
            get
            {
                if (_TitleLabel == null)
                {
                    _TitleLabel = new Label()
                    {
                        Text = "TitleLabel",
                        Bounds = new Rectangle(4, 4, 180, 20),
                        Alignment = ContentAlignment.TopLeft,
                        PrepareToolTipInParent = true
                    };
                    _TitleLabel.Visible = false;
                    this.AddItem(this._TitleLabel);
                }
                return _TitleLabel;
            }
        }
        private Label _TitleLabel;
        /// <summary>
        /// Modifikátor fontu pro titulkový label <see cref="TitleLabel"/> platný v době, kdy this Container má Focus
        /// </summary>
        public FontModifierInfo TitleFontModifierOnFocus { get; set; }
        /// <summary>
        /// Barva textu pro titulkový label <see cref="TitleLabel"/> platná v době, kdy this Container má Focus
        /// </summary>
        public Color? TitleTextColorOnFocus { get; set; }
        /// <summary>
        /// Obsahuje true, pokud je viditelný label <see cref="TitleLine"/>.
        /// Pokud objekt není viditelný (<see cref="TitleLineVisible"/> je false), pak objekt <see cref="TitleLine"/> je null.
        /// Výchozí hodnota <see cref="TitleLineVisible"/> je false = instance labelu neexistuje a šetří se tak paměť.
        /// Pokud je label nějakou dobu používán a pak je nastaveno <see cref="TitleLineVisible"/> = false, pak se label zahodí i se všemi nastavenými vlastnostmi,
        /// a následné nastavení <see cref="TitleLineVisible"/> = true vygeneruje new instanci s výchozími hodnotami.
        /// Pokud chceme zapínat a vypínat viditelnost, ale ponechat vlastnosti, pak je správné řídit přímo viditelnost Labelu <see cref="TitleLine"/>.Visible.
        /// </summary>
        public bool TitleLineVisible
        {
            get { return (_TitleLine != null ? _TitleLine.Visible : false); }
            set
            {
                if (value)
                {   // Zobrazit: použijeme property TitleLine, tam se v případě potřeby vytvoří new instance, a do ní vložíme Visible = true:
                    TitleLine.Visible = true;
                }
                else
                {   // Zneviditelnit = zlikvidovat:
                    if (_TitleLine != null)
                    {
                        this.RemoveItem(_TitleLine);
                        _TitleLine = null;
                    }
                }
            }
        }
        /// <summary>
        /// Titulkový label. Pokud je čtena hodnota, která by dosud byla null (tzn. <see cref="TitleLineVisible"/> je false), pak bude nejprve vytvořena new instance a ta vrácena,
        /// ale její vlastní Visible bude false.
        /// </summary>
        public Line3D TitleLine
        {
            get
            {
                if (_TitleLine == null)
                {
                    _TitleLine = new Line3D()
                    {
                        Bounds = new Rectangle(9, 26, 200, 2),
                        PrepareToolTipInParent = true
                    };
                    _TitleLine.Visible = false;
                    this.AddItem(this._TitleLine);
                }
                return _TitleLine;
            }
        }
        private Line3D _TitleLine;
        /// <summary>
        /// Obsahuje aktualizovaný styl pro titulek panelu ve stavu bez focusu
        /// </summary>
        protected LabelStyle TitleLabelStyle
        {
            get
            {
                var labelStyle = _TitleLabelStyle;
                if (labelStyle == null)
                {
                    labelStyle = new LabelStyle();
                    _TitleLabelStyle = labelStyle;
                }
                var panelStyle = this.StyleCurrent;
                labelStyle.FontModifier = panelStyle.TitleFontModifier;
                labelStyle.TextColor = panelStyle.TitleTextColor;
                return labelStyle;
            }
        }
        private static LabelStyle _TitleLabelStyle;
        /// <summary>
        /// Obsahuje aktualizovaný styl pro titulek panelu s focusem
        /// </summary>
        protected LabelStyle TitleLabelStyleFocused
        {
            get
            {
                var labelStyle = _TitleLabelStyleFocused;
                if (labelStyle == null)
                {
                    labelStyle = new LabelStyle();
                    _TitleLabelStyleFocused = labelStyle;
                }
                var panelStyle = this.StyleCurrent;
                labelStyle.FontModifier = panelStyle.TitleFontModifierFocused;
                labelStyle.TextColor = panelStyle.TitleTextColorFocused;
                return labelStyle;
            }
        }
        private static LabelStyle _TitleLabelStyleFocused;
        #endregion
        #region Interaktivita
        /// <summary>
        /// Po vstupu Focusu do Containeru. Třída <see cref="InteractiveLabeledContainer"/> řídí modifikaci fontu a barvy titulku
        /// (vložením <see cref="TitleFontModifierOnFocus"/> a <see cref="TitleTextColorOnFocus"/> do objektu <see cref="TitleLabel"/>).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusEnter(e);
            if (TitleLabelVisible)
            {
                TitleLabel.StyleParent = TitleLabelStyleFocused;
                TitleLabel.Invalidate();
            }
        }
        /// <summary>
        /// Po odchodu Focusu z Containeru. Třída <see cref="InteractiveLabeledContainer"/> řídí modifikaci fontu a barvy titulku
        /// (odebráním <see cref="TitleFontModifierOnFocus"/> a <see cref="TitleTextColorOnFocus"/> z objektu <see cref="TitleLabel"/>).
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedFocusLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedFocusLeave(e);
            if (TitleLabelVisible)
            {
                TitleLabel.StyleParent = TitleLabelStyle;
                TitleLabel.Invalidate();
            }
            TitleLabel.Invalidate();
        }
        #endregion
    }
    #endregion
}
