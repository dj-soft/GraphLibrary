﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Control, který zobrazuje sadu "štítků", z nichž uživatel kliknutím sestavuje sadu pro filtrování.
    /// </summary>
    public class TagFilter : InteractiveObject, ITagFilter
    {
        #region Public data
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TagFilter()
        {
            this.BackColor = Skin.TagFilter.BackColor;
            this._DrawItemBorder = true;
            this._SelectionMode = TagFilterSelectionMode.AnyItemsCount;
            this._SelectAllVisible = true;
            this._CurrentCheckedImage = null;
            this._CurrentRoundPercent = 0;
            this._CurrentItemHeight = Skin.TagFilter.ItemHeight;
            this._CurrentItemSpacing = Skin.TagFilter.ItemSpacing;
        }
        /// <summary>
        /// Sada štítků. 
        /// V daném pořadí budou zobrazovány.
        /// Po nasetování hodnoty bude vyvolán event <see cref="FilterChanged"/>.
        /// </summary>
        public TagInfo[] TagItems { get { return this._TagItems; } set { this._TagItems = value; this._TagItemsChanged(); this.CallFilterChanged(); } } private TagInfo[] _TagItems;
        /// <summary>
        /// Režim výběru položek
        /// </summary>
        public TagFilterSelectionMode SelectionMode { get { return this._SelectionMode; } set { this._SelectionMode = value; this._SelectionModeApply(); } }
        private TagFilterSelectionMode _SelectionMode;
        /// <summary>
        /// Hodnota true: jako první položka je tlačítko "Vše", která zruší výběr ostatních položek a tím zruší i filtr (true je default).
        /// Hodnota false: toto tlačítku nebude zobrazeno.
        /// </summary>
        public bool SelectAllVisible { get { return this._SelectAllVisible; } set { this._SelectAllVisible = value; this._TagItemsChanged(); } }
        private bool _SelectAllVisible;
        /// <summary>
        /// Barva pozadí tlačítka "Vše"
        /// </summary>
        public Color? SelectAllItemBackColor { get { return this._SelectAllItemBackColor; } set { this._SelectAllItemBackColor = value; this._TagItemsRepaint(); } }
        private Color? _SelectAllItemBackColor;
        /// <summary>
        /// Barva pozadí tlačítka "Vše", pokud jeho <see cref="TagInfo.Checked"/> = true
        /// </summary>
        public Color? SelectAllItemCheckedBackColor { get { return this._SelectAllItemCheckedBackColor; } set { this._SelectAllItemCheckedBackColor = value; this._TagItemsRepaint(); } }
        private Color? _SelectAllItemCheckedBackColor;
        /// <summary>
        /// Barva pozadí prvků
        /// </summary>
        public Color? ItemBackColor { get { return this._ItemBackColor; } set { this._ItemBackColor = value; this._TagItemsRepaint(); } }
        private Color? _ItemBackColor;
        /// <summary>
        /// Barva pozadí prvků, které jsou <see cref="TagInfo.Checked"/> = true
        /// </summary>
        public Color? ItemCheckedBackColor { get { return this._ItemCheckedBackColor; } set { this._ItemCheckedBackColor = value; this._TagItemsRepaint(); } }
        private Color? _ItemCheckedBackColor;
        /// <summary>
        /// Explicitně definovaná barva rámečku
        /// </summary>
        public Color? ItemBorderColor { get { return this._ItemBorderColor; } set { this._ItemBorderColor = value; this._TagItemsRepaint(); } }
        private Color? _ItemBorderColor;
        /// <summary>
        /// Explicitně definovaná barva textu
        /// </summary>
        public Color? ItemTextColor { get { return this._ItemTextColor; } set { this._ItemTextColor = value; this._TagItemsRepaint(); } }
        private Color? _ItemTextColor;
        /// <summary>
        /// Výška jednoho prvku.
        /// </summary>
        public int ItemHeight { get { return this._CurrentItemHeight; } set { this._CurrentItemHeight = value; this._TagItemsChanged(); } }
        /// <summary>
        /// Rozestupy mezi prvky
        /// </summary>
        public Size ItemSpacing { get { return this._CurrentItemSpacing; } set { this._CurrentItemSpacing = value; this._TagItemsChanged(); } }
        /// <summary>
        /// Procento kulatých krajů jednotlivých prvků.
        /// Null = 0 = hranaté prvky; 100 = 100% = čisté půlkruhy. Hodnoty mimo rozsah jsou zarovnané do rozsahu 0 až 100 (včetně).
        /// </summary>
        public int RoundItemPercent { get { return this._CurrentRoundPercent; } set { this._CurrentRoundPercent = value; this._TagItemsChanged(); } }
        /// <summary>
        /// Kreslit okraje okolo prvků?
        /// </summary>
        public bool DrawItemBorder { get { return this._DrawItemBorder; } set { this._DrawItemBorder = value; this._TagItemsRepaint(); } }
        private bool _DrawItemBorder;
        /// <summary>
        /// Image kreslený jako ikona pro stav "Prvek je vybrán"
        /// </summary>
        public Image CheckedImage { get { return this._CurrentCheckedImage; } set { this._CurrentCheckedImage = value; this._TagItemsChanged(); } }
        /// <summary>
        /// Optimální výška tohoto prvku <see cref="InteractiveObject.Bounds"/>.Height pro zobrazení jednoho řádku položek
        /// </summary>
        public int OptimalHeightOneRow { get { return ((this.ClientBorder.HasValue ? this.ClientBorder.Value.Vertical : 0) + 2 * this.ItemSpacing.Height + this.ItemHeight); } }
        /// <summary>
        /// Optimální výška tohoto prvku <see cref="InteractiveObject.Bounds"/>.Height pro zobrazení všech aktuálně připravených řádků s položkami
        /// </summary>
        public int OptimalHeightAllRows { get; private set; }
        #endregion
        #region Podpora pro externí řízení filtru (reset, set)
        /// <summary>
        /// Metoda zajistí, že TagFilter bude ukazovat všechny položky
        /// </summary>
        public bool TagFilterReset(ref bool callRefresh)
        {
            return this.TagFilterSet(null, ref callRefresh);
        }
        /// <summary>
        /// Metoda zajistí, že TagFilter bude ukazovat jen TagItem vyhovující dodanému filtru
        /// </summary>
        /// <param name="selector">Filtr, který pro danou položku filtru vrací příznak, že má být Checked</param>
        /// <param name="callRefresh">Nastaví se na true při požadavku na změnu</param>
        public bool TagFilterSet(Func<TagInfo, bool> selector, ref bool callRefresh)
        {
            bool isChange = false;
            var dataItems = this._DataItemList;
            foreach (TagItem gTagItem in dataItems)
            {
                bool oldValue = gTagItem.TagInfo.CheckedSilent;
                bool newValue = (selector != null ? selector(gTagItem.TagInfo) : false);
                if (newValue != oldValue)
                {
                    gTagItem.TagInfo.CheckedSilent = newValue;
                    isChange = true;
                    callRefresh = true;
                }
            }
            if (isChange)
                this.CallFilterChanged();

            return isChange;
        }
        #endregion
        #region Vnitřní prvky
        /// <summary>
        /// Grafické prvky <see cref="TagItem"/>, pole obsahuje pouze viditelné prvky z pole <see cref="TagItems"/>.
        /// </summary>
        private List<TagItem> _DataItemList;
        /// <summary>
        /// Veškeré child prvky = prvek "All" + prvky z <see cref="_DataItemList"/>
        /// </summary>
        private List<TagItem> _GItemList;
        /// <summary>
        /// Prvek "All" = zapíná zobrazení všech záznamů.
        /// Do všech prvků v <see cref="_DataItemList"/> vloží <see cref="TagItem.CheckedSilent"/> = false.
        /// </summary>
        private TagItem _SelectAllItem;
        /// <summary>
        /// Interaktivní child prvky
        /// </summary>
        protected override IEnumerable<IInteractiveItem> Childs { get { return this._GItemList; } }
        #endregion
        #region Přepočty a překreslení
        /// <summary>
        /// Je voláno po změně souřadnic <see cref="InteractiveObject.Bounds"/>, z metody <see cref="InteractiveObject.SetBounds(Rectangle, ProcessAction, EventSourceType)"/>, pokud je specifikována akce <see cref="ProcessAction.PrepareInnerItems"/>.
        /// Účelem je přepočítat souřadnice vnořených závislých prvků.
        /// </summary>
        /// <param name="oldBounds">Původní umístění, před změnou</param>
        /// <param name="newBounds">Nové umístění, po změnou. Používejme raději tuto hodnotu než this.Bounds</param>
        /// <param name="actions">Akce k provedení</param>
        /// <param name="eventSource">Zdroj této události</param>
        protected override void SetBoundsPrepareInnerItems(Rectangle oldBounds, Rectangle newBounds, ref ProcessAction actions, EventSourceType eventSource)
        {
            base.SetBoundsPrepareInnerItems(oldBounds, newBounds, ref actions, eventSource);
            if (oldBounds.Width != newBounds.Width)
                this._TagItemsPrepareLayout();
        }
        /// <summary>
        /// Volá se po změně obsahu nebo velikosti prvků v poli <see cref="_TagItems"/>.
        /// </summary>
        private void _TagItemsChanged()
        {
            this._DataItemList = this._CreateGTagItemList(this._TagItems);

            this._GItemList = new List<TagItem>();

            if (this.SelectAllVisible)
            {
                if (this._SelectAllItem == null)
                    this._SelectAllItem = this._CreateGTagItemAll();
                this._GItemList.Add(this._SelectAllItem);
            }

            if (this._DataItemList.Count > 0)
                this._GItemList.AddRange(this._DataItemList);

            this._SelectionModeApply();
            this._TagItemsPrepareLayout();
        }
        /// <summary>
        /// Vytvoří a vrátí grafický objekt pro tlačítko "All"
        /// </summary>
        /// <returns></returns>
        private TagItem _CreateGTagItemAll()
        {
            TagInfo item = new TagInfo() { Text = "Vše" };
            return this._CreateGTagItem(item, true);
        }
        /// <summary>
        /// Z dodaných datových prvků <see cref="TagInfo"/> vybere ty, které jsou Visible, vytvoří z nich pole grafických prvků a to vrátí.
        /// </summary>
        /// <param name="tagItems"></param>
        /// <returns></returns>
        private List<TagItem> _CreateGTagItemList(TagInfo[] tagItems)
        {
            List<TagItem> gItemList = new List<TagItem>();
            if (tagItems != null)
                gItemList.AddRange(tagItems.Where(i => i.Visible).Select(i => this._CreateGTagItem(i, false)));
            return gItemList;
        }
        /// <summary>
        /// Do daného datového <see cref="TagInfo"/> prvku vloží jeho vlastníka <see cref="IOwnerProperty{GTagFilter}.Owner"/> = this;
        /// z datového prvku vytvoří grafický prvek <see cref="TagItem"/>;
        /// a ten vrátí.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isSelectAll"></param>
        /// <returns></returns>
        private TagItem _CreateGTagItem(TagInfo item, bool isSelectAll)
        {
            ((IOwnerProperty<TagFilter>)item).Owner = this;
            TagItem gItem = new TagItem(isSelectAll) { TagInfo = item };
            return gItem;
        }
        /// <summary>
        /// Metoda připraví souřadnice Bounds pro své zobrazované prvky, podle jejich obsahu
        /// </summary>
        private void _TagItemsPrepareLayout()
        {
            if (this._GItemList == null || this._GItemList.Count == 0) return;
            using (Graphics graphics = (this.Host != null ? this.Host.CreateGraphics() : null))
            {
                this._TagItemsPrepareLayout(graphics, this._GItemList, this.ClientSize);
            }
        }
        /// <summary>
        /// Metoda připraví souřadnice Bounds pro své zobrazované prvky, podle jejich obsahu
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gItemList"></param>
        /// <param name="clientSize"></param>
        private void _TagItemsPrepareLayout(Graphics graphics, List<TagItem> gItemList, Size clientSize)
        {
            Size spacing = this._CurrentItemSpacing;
            Point point = new Point(spacing.Width, spacing.Height);
            List<TagItem> oneRowList = new List<TagItem>();
            foreach (TagItem gTagItem in gItemList)
                gTagItem.PrepareBlockLayout(graphics, clientSize, oneRowList, ref point);

            this._DetectOptimalHeight(gItemList);
        }
        /// <summary>
        /// Vypočte optimální výšky <see cref="OptimalHeightOneRow"/> a <see cref="OptimalHeightAllRows"/>.
        /// </summary>
        /// <param name="gItemList"></param>
        private void _DetectOptimalHeight(List<TagItem> gItemList)
        {
            int spacingY = this._CurrentItemSpacing.Height;
            int heightOne = 0;
            int heightAll = 0;
            System.Windows.Forms.Padding? border = this.ClientBorder;
            int y0 = (border.HasValue ? border.Value.Top : 0);
            int y1 = (border.HasValue ? border.Value.Bottom : 0);
            if (gItemList != null && gItemList.Count > 0)
            {
                heightOne = y0 + gItemList[0].Bounds.Bottom + spacingY + y1;
                heightAll = y0 + gItemList[gItemList.Count - 1].Bounds.Bottom + spacingY + y1;
            }
            else
            {
                heightOne = y0 + spacingY + y1;
                heightAll = heightOne;
            }

            this.OptimalHeightAllRows = heightAll;

            if (this.ExpandHeightOnMouse)
            {
                Rectangle bounds = this.Bounds;
                bounds.Height = this.OptimalHeightOneRow;
                this.Bounds = bounds;
                this._CurrentHeightState = TagFilterHeightState.OneRow;
            }
        }
        /// <summary>
        /// Požadavek na překreslení prvků
        /// </summary>
        private void _TagItemsRepaint()
        {
            this.Repaint();
        }
        /// <summary>
        /// Platná hodnota ItemRoundPercent (v rozsahu 0 až 100)
        /// </summary>
        private int _CurrentRoundPercent
        {
            get { return this.__CurrentRoundPercent; }
            set { this.__CurrentRoundPercent = (value < 0 ? 0 : (value > 100 ? 100 : value)); }
        }
        private int __CurrentRoundPercent;
        /// <summary>
        /// Platná hodnota ItemHeight
        /// </summary>
        private int _CurrentItemHeight
        {
            get { return this.__CurrentItemHeight; }
            set { this.__CurrentItemHeight = (value < 16 ? 16 : (value > 50 ? 50 : value)); }
        }
        private int __CurrentItemHeight;
        /// <summary>
        /// Platná hodnota ItemSpacing
        /// </summary>
        private Size _CurrentItemSpacing
        {
            get { return this.__CurrentItemSpacing; }
            set
            {
                int w = value.Width;
                w = (w < 0 ? 0 : (w > 20 ? 20 : w));
                int h = value.Height;
                h = (h < 0 ? 0 : (h > 20 ? 20 : h));
                this.__CurrentItemSpacing = new Size(w, h);
            }
        }
        private Size __CurrentItemSpacing;
        /// <summary>
        /// Aktuální ikona. Vložení hodnoty null reálně uloží defaultní ikonu.
        /// Vložení hodnoty z ní přečte její velikost a uloží ji do <see cref="_CurrentCheckedImageSize"/>.
        /// </summary>
        private Image _CurrentCheckedImage
        {
            get { return this.__CurrentCheckedImage; }
            set
            {
                Image icon = ((value != null) ? value : Skin.TagFilter.ItemSelectedImage);
                this.__CurrentCheckedImage = icon;
                this._CurrentCheckedImageSize = (icon != null ? icon.Size : Size.Empty);
            }
        }
        private Image __CurrentCheckedImage;
        /// <summary>
        /// Velikost ikony
        /// </summary>
        private Size _CurrentCheckedImageSize
        {
            get { return this.__CurrentCheckedImageSize; }
            set
            {
                int w = value.Width;
                if (w > 24) w = 24;
                int h = value.Height;
                if (h > 24) h = 24;
                this.__CurrentCheckedImageSize = new Size(w, h);
            }
        }
        private Size __CurrentCheckedImageSize;
        #endregion
        #region Interaktivita prvků, výběr prvků podle režimu, eventy
        /// <summary>
        /// Zavolá háček OnFilterChanged() a event FilterChanged
        /// </summary>
        protected void CallFilterChanged()
        {
            if (this._TagItems == null) return;

            EventArgs args = new EventArgs();
            this.OnFilterChanged(args);
            if (this.FilterChanged != null)
                this.FilterChanged(this, args);
        }
        /// <summary>
        /// Háček při změně ve filtru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFilterChanged(EventArgs args) { }
        /// <summary>
        /// Událost, kdy došlo ke změně ve filtru.
        /// Aktuálně vybrané položky si eventhandler najde v property <see cref="FilteredItems"/>.
        /// </summary>
        public event EventHandler FilterChanged;
        /// <summary>
        /// Souhrn položek filtru, které jsou aktuálně vybrané.
        /// Pokud je zde počet položek = 0, mívá to význam "Zobrazit vše".
        /// </summary>
        public TagInfo[] FilteredItems { get { return this._DataItemList.Where(g => g.CheckedSilent).Select(g => g.TagInfo).ToArray(); } }
        /// <summary>
        /// Uživatel kliknul na prvek
        /// </summary>
        /// <param name="clickedItem"></param>
        private void _OnItemClick(TagItem clickedItem)
        {
            var dataItems = this._DataItemList;
            List<TagItem> checkedItems = dataItems.Where(g => g.CheckedSilent).ToList();
            int checkedCount = checkedItems.Count;
            bool isChange = false;

            if (clickedItem.IsSelectAll)
            {   // Uživatel klikl na prvek "Vyber vše":
                isChange = (!clickedItem.CheckedSilent);
                clickedItem.CheckedSilent = true;
                checkedItems.ForEach(g => g.CheckedSilent = false);
            }
            else
            {   // Uživatel klikl na některý DATOVÝ prvek:
                TagItem selectAllItem = this._SelectAllItem;
                bool enabledAll = (this.SelectAllVisible && selectAllItem != null);
                switch (this._SelectionMode)
                {
                    case TagFilterSelectionMode.AnyItemsCount:
                        // Lze vybrat jakýkoli počet položek: nula, jedna i třeba všechny položky.
                        isChange = true;
                        clickedItem.CheckedSilent = !clickedItem.CheckedSilent;
                        this._SelectAllItemCheckByData();
                        break;

                    case TagFilterSelectionMode.OnlyOneItem:
                        // Lze vybrat pouze jednu položku, nebo žádnou (odznačením vybrané položky): nula nebo jedna položka.
                        if (clickedItem.CheckedSilent)
                        {   //  a) Pokud clickedItem byla dosud označená, 
                            //     tak ji odznačím; nemá to vliv na okolní datové položky:
                            isChange = true;
                            clickedItem.CheckedSilent = false;
                        }
                        else
                        {   //  b) Pokud clickedItem byla dosud neoznačená, a máme nějaké jiné datové označené položky, 
                            //     tak nejprve všechny označené odznačím, a poté clickedItem označím:
                            isChange = true;
                            checkedItems.ForEach(g => g.CheckedSilent = false);
                            clickedItem.CheckedSilent = true;
                        }
                        this._SelectAllItemCheckByData();
                        break;

                    case TagFilterSelectionMode.ExactOneItem:
                        // Lze vybrat právě jen jednu položku, nikoli žádnou (položku nelze odznačit): právě jedna položka.
                        if (!clickedItem.CheckedSilent)
                        {   //  Pokud clickedItem byla dosud neoznačená, a máme nějaké jiné datové označené položky, 
                            //     tak nejprve všechny označené odznačím, a poté clickedItem označím:
                            isChange = true;
                            checkedItems.ForEach(g => g.CheckedSilent = false);
                            clickedItem.CheckedSilent = true;
                        }   //  Pokud clickedItem byla dosud označená, tak ji tak necháme.
                        this._SelectAllItemCheckByData();
                        break;
                }
            }
            if (isChange)
                this.CallFilterChanged();

            this.Repaint();
        }
        /// <summary>
        /// Po změně režimu: zajistí platnost výběru položek (<see cref="TagItem.CheckedSilent"/>).
        /// Tato metoda neřeší překreslení obsahu prvku.
        /// </summary>
        private void _SelectionModeApply()
        {
            List<TagItem> dataItems = this._DataItemList;
            List<TagItem> checkedItems = dataItems.Where(g => g.CheckedSilent).ToList();
            int checkedCount = checkedItems.Count;
            TagItem selectAllItem = this._SelectAllItem;
            bool enabledAll = (this.SelectAllVisible && selectAllItem != null);

            switch (this._SelectionMode)
            {
                case TagFilterSelectionMode.AnyItemsCount:
                    // Lze vybrat jakýkoli počet položek: nula, jedna i třeba všechny položky.
                    this._SelectAllItemCheckByData();
                    break;

                case TagFilterSelectionMode.OnlyOneItem:
                    // Lze vybrat pouze jednu položku, nebo žádnou (odznačením vybrané položky): nula nebo jedna položka.
                    if (checkedCount > 1)
                    {   // Nyní je označeno více než jedna datová položka => ponecháme označenou jen první, a ostatní odznačíme:
                        for (int i = 1; i < checkedCount; i++)
                            checkedItems[i].CheckedSilent = false;
                    }
                    this._SelectAllItemCheckByData();
                    break;

                case TagFilterSelectionMode.ExactOneItem:
                    // Lze vybrat právě jen jednu položku, nikoli žádnou (položku nelze odznačit): právě jedna položka.
                    if (checkedCount > 1)
                    {   // Nyní je označeno více než jedna datová položka => ponecháme označenou jen první, a ostatní odznačíme:
                        for (int i = 1; i < checkedCount; i++)
                            checkedItems[i].CheckedSilent = false;
                    }
                    else if (checkedCount == 0 && !enabledAll && dataItems.Count > 0)
                    {   // Pokud není vybraná žádná datová položka, a není dostupný ani prvek "Vyber vše", 
                        //  pak musí být vybraná první položka datová:
                        dataItems[0].CheckedSilent = true;
                    }
                    this._SelectAllItemCheckByData();
                    break;
            }
        }
        /// <summary>
        /// Metoda zajistí, že bude korektně označen prvek "Vyber vše"
        /// </summary>
        private void _SelectAllItemCheckByData()
        {
            if (!(this.SelectAllVisible && this._SelectAllItem != null)) return;
            List<TagItem> checkedItems = this._DataItemList.Where(g => g.CheckedSilent).ToList();
            this._SelectAllItem.CheckedSilent = (checkedItems.Count == 0);
        }
        #endregion
        #region Automatické rozbalení na plnou výšku při MouseEnter
        /// <summary>
        /// Dovolí prvku, aby se "roztáhnul" na potřebnou výšku v situaci, kdy jeho aktuální výška je menší než je potebná pro zobrazení všech tlačítek.
        /// K roztáhnutí dojde v eventu MouseEnter, k následnému sbalení v eventu MouseLeave.
        /// Hodnota false: prvek nijak nereaguje ma MouseEnter.
        /// Pozor: změna této hodnoty v situaci, kdy je myš uvnitř prvku, nemá už vliv na zobrazení.
        /// </summary>
        public bool ExpandHeightOnMouse { get { return this._ExpandHeightOnMouse; } set { this._ExpandHeightOnMouse = value; } }
        private bool _ExpandHeightOnMouse;
        /// <summary>
        /// Metoda je volaná z <see cref="InteractiveObject.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro ChangeState = MouseEnter
        /// Přípravu tooltipu je vhodnější provést v metodě <see cref="InteractiveObject.PrepareToolTip(GInteractiveChangeStateArgs)"/>, ta je volaná hned poté.
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseEnter(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseEnter(e);
            if (this._ExpandHeightOnMouse)
            {
                Rectangle bounds = this.Bounds;
                int heightOld = bounds.Height;
                int heightNew = this.OptimalHeightAllRows;
                if (heightNew > heightOld)
                {
                    this._HeightOnMouseEnter = heightOld;
                    if (this._HeightAnimation)
                        this._HeightAnimationStart(heightOld, heightNew, 6, TagFilterHeightState.Expanding, TagFilterHeightState.FullHeight);
                    else
                        this.Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, heightNew);
                }
            }
            else
            {
                this._HeightOnMouseEnter = null;
            }
        }
        /// <summary>
        /// Metoda je volaná z <see cref="InteractiveObject.AfterStateChanged(GInteractiveChangeStateArgs)"/> pro ChangeState = MouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedMouseLeave(GInteractiveChangeStateArgs e)
        {
            base.AfterStateChangedMouseLeave(e);
            if (this._HeightOnMouseEnter.HasValue)
            {
                Rectangle bounds = this.Bounds;
                int heightOld = bounds.Height;
                int heightNew = this._HeightOnMouseEnter.Value;
                if (this._HeightAnimation)
                    this._HeightAnimationStart(heightOld, heightNew, 6, TagFilterHeightState.Collapsing, TagFilterHeightState.OneRow);
                else
                {
                    this.Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, heightNew);
                    this.Parent.Repaint();
                }
            }
            this._HeightOnMouseEnter = null;
        }
        #endregion
        #region Animace změny výšky
        /// <summary>
        /// Aktuální stav výšky objektu
        /// </summary>
        public TagFilterHeightState CurrentHeightState
        {
            get { return this._CurrentHeightState; }
        }
        private TagFilterHeightState _CurrentHeightState;
        /// <summary>
        /// Cílový stav výšky objektu
        /// </summary>
        private TagFilterHeightState _TargetHeightState;
        /// <summary>
        /// Zahájí animaci změny výšky
        /// </summary>
        /// <param name="valueBegin"></param>
        /// <param name="valueEnd"></param>
        /// <param name="stepCount"></param>
        /// <param name="animatedHeightState"></param>
        /// <param name="targetHeightState"></param>
        private void _HeightAnimationStart(int valueBegin, int valueEnd, int stepCount, TagFilterHeightState animatedHeightState, TagFilterHeightState targetHeightState)
        {
            if (this._HeightAnimationRunning)
            {
                this._HeightAnimationAbort(valueEnd, targetHeightState);
                return;
            }
            this._CurrentHeightState = animatedHeightState;
            this._TargetHeightState = targetHeightState;

            this._HeightAnimationSteps = new int[stepCount];
            for (int i = 0; i < stepCount; i++)
                this._HeightAnimationSteps[i] = this._HeightAnimationGetValue(valueBegin, valueEnd, i, stepCount);

            this._HeightAnimationStepIndex = 0;
            this._HeightAnimationRunning = true;
            this.Host.AnimationStart(this._HeightAnimationTick);
        }
        /// <summary>
        /// Vrátí hodnotu pro animaci daného kroku
        /// </summary>
        /// <param name="valueBegin"></param>
        /// <param name="valueEnd"></param>
        /// <param name="stepIndex"></param>
        /// <param name="stepCount"></param>
        /// <returns></returns>
        private int _HeightAnimationGetValue(int valueBegin, int valueEnd, int stepIndex, int stepCount)
        {
            // Vzdálenost, kterou musíme animovat:
            double size = (double)(valueEnd - valueBegin);
            // Hodnota ratio = 0.00 => na začátku (ale to vylučujeme)  --až--  1.00 => na konci:
            double ratio = ((double)(stepIndex + 1)) / ((double)(stepCount));
            double dist = size * Math.Sin(ratio * Math.PI / 2d);            // lineárně: dist = (ratio * size);
            double value = ((double)valueBegin) + dist;
            return (int)Math.Round(value, 0);
        }
        /// <summary>
        /// Nouzově zruší animaci a nastaví cílovou hodnotu.
        /// Používá se při požadavku na novou animaci v době, kdy aktuální ještě běží.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="heightState"></param>
        private void _HeightAnimationAbort(int value, TagFilterHeightState heightState)
        {
            this._HeightAnimationSteps = null;
            this._HeightAnimationStepIndex = -1;
            this._HeightAnimationRunning = false;
            this._CurrentHeightState = heightState;
            this._TargetHeightState = heightState;

            Rectangle bounds = this.Bounds;
            this.Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, value);
            this.Parent.Repaint();
        }
        /// <summary>
        /// Provede jeden krok animace.
        /// Metoda je volaná z Hostu, z jeho animačního timeru, v threadu na pozadí.
        /// Metoda nemá provádět jakýkoli pokus o Refresh nebo Invalidaci, jen má vrátit informaci o svých požadavcích.
        /// </summary>
        /// <returns></returns>
        private AnimationResult _HeightAnimationTick(AnimationArgs args)
        {
            AnimationResult result = this._HeightAnimationTickRun();
            if (result.HasFlag(AnimationResult.Stop))
            {
                this._HeightAnimationRunning = false;
                this._CurrentHeightState = this._TargetHeightState;
            }
            return result;
        }
        /// <summary>
        /// Provede jeden krok animace.
        /// </summary>
        /// <returns></returns>
        private AnimationResult _HeightAnimationTickRun()
        {
            int[] steps = this._HeightAnimationSteps;
            int index = this._HeightAnimationStepIndex;
            if (steps == null || steps.Length == 0 || index < 0) return AnimationResult.Stop;

            int count = steps.Length;
            if (index < 0 || index >= count) return AnimationResult.Stop;
            int value = steps[index];
            Rectangle bounds = this.Bounds;
            this.Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, value);
            this.Parent.Repaint();
            AnimationResult result = AnimationResult.DrawAll;
            index++;
            this._HeightAnimationStepIndex = index;
            if (index >= count)
                result |= AnimationResult.Stop;
            
            return result;
        }
        /// <summary>
        /// Kroky animace
        /// </summary>
        private int[] _HeightAnimationSteps;
        /// <summary>
        /// Index do pole <see cref="_HeightAnimationSteps"/>, odkud bude čtena hodnota pro následující Tick <see cref="_HeightAnimationTick(AnimationArgs)"/>.
        /// </summary>
        private int _HeightAnimationStepIndex;
        /// <summary>
        /// Obsahuje true v době, kdy animace běží, a false když neběží.
        /// </summary>
        private bool _HeightAnimationRunning;
        /// <summary>
        /// Výška, kterou měl objekt v eventu MouseEnter, před tím než byl zvětšen.
        /// </summary>
        private int? _HeightOnMouseEnter;
        /// <summary>
        /// Obsahuje true = pokud změna výšky má probíhat formou animace / false = skokově
        /// </summary>
        private bool _HeightAnimation = true;
        #endregion
        #region ITagFilter explicitní implementace
        int ITagFilter.CurrentRoundPercent { get { return this._CurrentRoundPercent; } }
        int ITagFilter.CurrentItemHeight { get { return this._CurrentItemHeight; } }
        Size ITagFilter.CurrentItemSpacing { get { return this._CurrentItemSpacing; } }
        Image ITagFilter.CurrentCheckedImage { get { return this._CurrentCheckedImage; } }
        Size ITagFilter.CurrentCheckedImageSize { get { return this._CurrentCheckedImageSize; } }
        void ITagFilter.TagItemsChanged() { this._TagItemsChanged(); }
        void ITagFilter.TagItemsRepaint() { this._TagItemsRepaint(); }
        void ITagFilter.OnItemClick(TagItem clickedItem) { this._OnItemClick(clickedItem); }
        #endregion
    }
    #region interface ITagFilter : pro interní práci s GTagFilter
    /// <summary>
    /// ITagFilter : pro interní práci s GTagFilter
    /// </summary>
    internal interface ITagFilter
    {
        /// <summary>
        /// Platná hodnota ItemRound
        /// </summary>
        int CurrentRoundPercent { get; }
        /// <summary>
        /// Platná hodnota ItemHeight
        /// </summary>
        int CurrentItemHeight { get; }
        /// <summary>
        /// Rozestupy mezi prvky
        /// </summary>
        Size CurrentItemSpacing { get; }
        /// <summary>
        /// Platná ikona pro Checked prvky
        /// </summary>
        Image CurrentCheckedImage { get; }
        /// <summary>
        /// Platná velikost ikony pro Checked prvky
        /// </summary>
        Size CurrentCheckedImageSize { get; }
        /// <summary>
        /// Požadavek na kompletní přepočet počtu a souřadnic prvků
        /// </summary>
        void TagItemsChanged();
        /// <summary>
        /// Požadavek na překreslení prvků
        /// </summary>
        void TagItemsRepaint();
        /// <summary>
        /// Po kliknutí na tlačítko
        /// </summary>
        /// <param name="clickedItem"></param>
        void OnItemClick(TagItem clickedItem);
    }
    #endregion
    #region enum GTagFilterSelectionMode, GTagFilterHeightState
    /// <summary>
    /// Režim výběru položek v <see cref="TagFilter"/>
    /// </summary>
    public enum TagFilterSelectionMode
    {
        /// <summary>
        /// Lze vybrat jakýkoli počet položek: nula, jedna i třeba všechny položky.
        /// </summary>
        AnyItemsCount,
        /// <summary>
        /// Lze vybrat pouze jednu položku, nebo žádnou (odznačením vybrané položky): nula nebo jedna položka.
        /// </summary>
        OnlyOneItem,
        /// <summary>
        /// Lze vybrat právě jen jednu položku, nikoli žádnou (položku nelze odznačit): právě jedna položka.
        /// </summary>
        ExactOneItem
    }
    /// <summary>
    /// Stav výšky objektu v procesu její animace
    /// </summary>
    public enum TagFilterHeightState
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Zobrazuje se jeden řádek
        /// </summary>
        OneRow,
        /// <summary>
        /// Výška se animuje z <see cref="OneRow"/> na <see cref="FullHeight"/>
        /// </summary>
        Expanding,
        /// <summary>
        /// Zobrazuje se plná výška
        /// </summary>
        FullHeight,
        /// <summary>
        /// Výška se animuje z <see cref="FullHeight"/> na <see cref="OneRow"/>
        /// </summary>
        Collapsing
    }
    #endregion
    #region class TagItem : Vizuální reprezentace jednoho prvku TagFilteru
    /// <summary>
    /// <see cref="TagItem"/> : Vizuální reprezentace jednoho prvku TagFilteru
    /// </summary>
    public class TagItem : InteractiveObject
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public TagItem() { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="isSelectAll"></param>
        public TagItem(bool isSelectAll) : this()
        {
            this.IsSelectAll = isSelectAll;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text + "; " + base.ToString();
        }
        #endregion
        #region Data TagItem
        /// <summary>
        /// Zobrazovaný text
        /// </summary>
        public string Text { get { return this._TagInfo.Text; } }
        /// <summary>
        /// Explicitně definovaná barva pozadí
        /// </summary>
        public Color? ItemBackColor { get { return this._TagInfo.BackColor; } }
        /// <summary>
        /// Explicitně definovaná barva pozadí ve stavu <see cref="CheckedSilent"/> = true
        /// </summary>
        public Color? ItemCheckedBackColor { get { return this._TagInfo.CheckedBackColor; } }
        /// <summary>
        /// Explicitně definovaná barva rámečku
        /// </summary>
        public Color? ItemBorderColor { get { return this._TagInfo.BorderColor; } }
        /// <summary>
        /// Explicitně definovaná barva textu
        /// </summary>
        public Color? ItemTextColor { get { return this._TagInfo.TextColor; } }
        /// <summary>
        /// Relativní velikost proti ostatním prvkům
        /// </summary>
        public float? RelativeSize { get { return this._TagInfo.RelativeSize; } }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public bool Visible { get { return this._TagInfo.Visible; } }
        /// <summary>
        /// Prvek je vybrán?
        /// Jde o Silent hodnotu: její setování nezpůsobí překreslení vizuálního controlu.
        /// V podstatě tuto hodnotu má nastavovat pouze vizuální control sám - jako důsledek interakce uživatele.
        /// </summary>
        public bool CheckedSilent { get { return this._TagInfo.CheckedSilent; } set { this._TagInfo.CheckedSilent = value; } }
        /// <summary>
        /// Vlastní datový prvek
        /// </summary>
        public TagInfo TagInfo { get { return this._TagInfo; } set { this._TagInfo = value; } }
        private TagInfo _TagInfo;
        #endregion
        #region Layout
        /// <summary>
        /// Pro všechny prvky v seznamu (oneRowList) určí jejich souřadnice.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="clientSize"></param>
        /// <param name="oneRowList"></param>
        /// <param name="point"></param>
        internal void PrepareBlockLayout(Graphics graphics, Size clientSize, List<TagItem> oneRowList, ref Point point)
        {
            // int height, int round, Size iconSize, int spacing,
            Size spacing = this.CurrentItemSpacing;

            Size itemSize = this.GetItemSize(graphics);
            // Pokud se do současného řádku (oneRowList) náš prvek už nevejde (z hlediska šířky řádku + šířky prvku > šířka prostoru):
            if (oneRowList.Count > 0)
            {   // Pokud už v řádku máme nějaké prvky,
                //  a this prvek se do současného řádku (oneRowList) už nevejde (z hlediska šířky řádku + šířky prvku > šířka prostoru):
                int right = (oneRowList[oneRowList.Count - 1].Bounds.Right + spacing.Width + itemSize.Width + spacing.Width);
                if (right > clientSize.Width)
                {   // Prvek se již do řádku nevejde -> dosavadní řádek zarovnáme do bloku, a začneme nový řádek:
                    AlignLayoutToWidth(oneRowList, spacing.Width, clientSize.Width);
                    Rectangle firstBounds = oneRowList[0].Bounds;
                    point = new Point(firstBounds.X, firstBounds.Bottom + spacing.Height);
                    oneRowList.Clear();
                }
            }

            Rectangle itemBounds = new Rectangle(point, itemSize);
            this.Bounds = itemBounds;
            oneRowList.Add(this);
            point = new Point(itemBounds.Right + spacing.Width, itemBounds.Y);
        }
        /// <summary>
        /// Vrátí minimální rozměr pro this prvek
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        protected Size GetItemSize(Graphics graphics)
        {
            int h = this.CurrentItemHeight;                     // výška prvku
            int r = h * this.CurrentRoundPercent / 100;         // šířka kulatého rohu = výška krát procento
            int s = this.CurrentCheckedImageSize.Width + 2;     // šířka ikony + prostor za ní
            int i = (h > s ? s : h);                            // šířka prostoru pro ikonu
            int w = r + i + Painter.MeasureString(graphics, this.Text, FontInfo.Caption).Width + 8;
            return new System.Drawing.Size(w, h);
        }
        /// <summary>
        /// Metoda prjde prvky daného řádku, a proporcionálně upraví jejich šířku tak, aby poslední prvek měl souřadnici Right zarovnanou doprava
        /// </summary>
        /// <param name="oneRowList"></param>
        /// <param name="spacingX"></param>
        /// <param name="clientWidth"></param>
        protected static void AlignLayoutToWidth(List<TagItem> oneRowList, int spacingX, int clientWidth)
        {
            int count = oneRowList.Count;
            int itemsWidth = oneRowList.Sum(i => i.Bounds.Width);              // Tolik pixelů je součet šířky prvků aktuálně
            int targetWidth = clientWidth - ((count + 1) * spacingX - 1);       // Tolik pixelů má být součet šířky prvků po zarovnání (odečítám spacing před i za blokem, a mezi prvky)
            float ratio = ((float)targetWidth / (float)itemsWidth);            // Poměr, jakým budu upravovat šířku prvků
            int iX = spacingX;
            float fX = (float)iX;
            for (int i = 0; i < count; i++)
            {
                TagItem item = oneRowList[i];
                Rectangle bounds = item.Bounds;
                float fWidth = ratio * (float)bounds.Width;
                if (i < (count - 1))
                {   // Prvky vyjma posledního:
                    float fRight = fX + fWidth;
                    int iWidth = (int)(Math.Round((fRight - fX), 0));
                    bounds = new Rectangle(iX, bounds.Y, iWidth, bounds.Height);
                    fX = fRight + (float)spacingX;
                }
                else
                {   // Poslední prvek bude jinak:
                    int iRight = clientWidth - spacingX;
                    bounds = new Rectangle(iX, bounds.Y, (iRight - iX), bounds.Height);
                }
                item.Bounds = bounds;
                iX = bounds.Right + spacingX;
            }
        }
        /// <summary>
        /// Vlastník grafického prvku = <see cref="TagFilter"/>
        /// </summary>
        protected TagFilter Owner
        {
            get
            {
                TagFilter owner = ((IOwnerProperty<TagFilter>)this.TagInfo).Owner;
                if (owner == null)
                    owner = this.SearchForParent(typeof(TagFilter)) as TagFilter;
                return owner;
            }
        }
        /// <summary>
        /// Vlastník <see cref="TagFilter"/> přetypovaný na interface <see cref="ITagFilter"/>, kvůli přístupu k vnitřním prvkům Ownera
        /// </summary>
        private ITagFilter IOwner { get { return this.Owner as ITagFilter; } }
        #endregion
        #region Interaktivita
        /// <summary>
        /// Metoda je volaná z InteractiveObject.AfterStateChanged() pro ChangeState = LeftClick
        /// </summary>
        /// <param name="e"></param>
        protected override void AfterStateChangedLeftClick(GInteractiveChangeStateArgs e)
        {
            this.IOwner.OnItemClick(this);
        }
        /// <summary>
        /// Obsahuje true pro prvek "Vyber vše", false pro běžné datové prvky
        /// </summary>
        internal bool IsSelectAll { get; private set; }
        #endregion
        #region Draw
        /// <summary>
        /// Metoda pro standardní vykreslení prvku.
        /// Bázová třída <see cref="InteractiveObject"/> v této metodě pouze vykreslí svůj prostor barvou pozadí <see cref="InteractiveObject.BackColor"/>.
        /// Pokud je předán režim kreslení drawMode, obsahující příznak <see cref="DrawItemMode.Ghost"/>, pak je barva pozadí modifikována tak, že její Alpha je 75% původního Alpha.
        /// </summary>
        /// <param name="e">Data pro kreslení</param>
        /// <param name="absoluteBounds">Absolutní souřadnice tohoto prvku, sem by se mělo fyzicky kreslit</param>
        /// <param name="absoluteVisibleBounds">Absolutní souřadnice tohoto prvku, oříznuté do viditelné oblasti.</param>
        /// <param name="drawMode">Režim kreslení (pomáhá řešit Drag and Drop procesy)</param>
        protected override void Draw(GInteractiveDrawArgs e, Rectangle absoluteBounds, Rectangle absoluteVisibleBounds, DrawItemMode drawMode)
        {
            // Nebudu volat Base, protože tam by se vyplnil celý Rectangle - ale GTagItem nemá tvar Rectangle, ale má kulaté okraje (a kolem okrajů je vidět podklad):
            //       base.Draw(e, absoluteBounds, absoluteVisibleBounds, drawMode);

            TagFilter gOwner = this.Owner;
            bool drawBorder = (gOwner != null ? gOwner.DrawItemBorder : false);
            Rectangle iconBounds, textBounds;
            using (var path = this.CreatePath(absoluteBounds, out iconBounds, out textBounds))
            {
                using (Painter.GraphicsUseSmooth(e.Graphics))
                {
                    Color backColor = this.CurrentBackColor;
                    using (Brush brush = Skin.CreateBrushForBackground(absoluteBounds, System.Windows.Forms.Orientation.Horizontal, this.InteractiveState, backColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    if (drawBorder)
                    {
                        Color borderColor = this.CurrentBorderColor;
                        e.Graphics.DrawPath(Skin.Pen(borderColor), path);
                    }
                }

                if (this.CheckedSilent && iconBounds.Width > 0)
                {
                    Image image = this.CurrentCheckedImage;
                    e.Graphics.DrawImage(image, iconBounds);
                }
                using (Painter.GraphicsUseText(e.Graphics))
                {
                    Color textColor = this.CurrentTextColor;
                    Painter.DrawString(e.Graphics, this.Text, FontInfo.Caption, textBounds, ContentAlignment.MiddleCenter, textColor);
                }
            }
        }
        /// <summary>
        /// Vytvoří tvar prvku a souřadnice ikony a textu
        /// </summary>
        /// <param name="absoluteBounds"></param>
        /// <param name="iconBounds"></param>
        /// <param name="textBounds"></param>
        /// <returns></returns>
        protected System.Drawing.Drawing2D.GraphicsPath CreatePath(Rectangle absoluteBounds, out Rectangle iconBounds, out Rectangle textBounds)
        {
            int round = this.CurrentRoundPercent;
            bool isRound = (round > 0);

            bool hasIcon = (this.CheckedSilent);
            int w = absoluteBounds.Width - 1;
            int h = absoluteBounds.Height - 1;

            int iw = 0;
            int ih = 0;
            if (hasIcon)
            {
                Image icon = this.CurrentCheckedImage;
                if (icon != null)
                {
                    Size iSize = icon.Size;
                    iw = iSize.Width;
                    ih = iSize.Height;
                    if (iw > 24) iw = 24;
                    if (ih > 24) ih = 24;
                }


                // int ih = (h > 26 ? 24 : h - 2);
            }
            int ex = (int)(h * round / 100);     // Šířka elipsy = daná poměrem round, k výšce
            int rx = ex / 2;                     // Radius v ose X
            int x0 = absoluteBounds.X;           // Začátek prvku
            int x1 = x0 + (isRound ? rx : 0);    // Začátek rovné vodorovné čáry
            int x2 = x1 + (isRound ? 1 : 4);     // Začátek ikony
            int x3 = x2 + (hasIcon ? iw : 0);    // Konec ikony  (24 pixel)
            int x4 = x3 + (hasIcon ? 2 : 0);     // Začátek textu
            int x9 = x0 + w;                     // Konec prvku
            int x8 = x9 - (isRound ? rx : 0);    // Konec rovné vodorovné čáry
            int x7 = x8 - 2;                     // Konec textu
            int x6 = x8 - (isRound ? rx : 0);    // Levá souřadnice kruhové výseče umístěné vpravo

            int ey = h;                          // Výška elipsy = vždy == výška
            int ry = ey / 2;                     // Radius v ose Y
            int y0 = absoluteBounds.Y;           // Začátek prvku
            int yc = y0 + ry;                    // Střed Y
            int y1 = y0 + 2;                     // Začátek textu
            int y2 = yc - (ih / 2);              // Začátek ikony
            int y3 = y2 + ih;                    // Konec ikony
            int y9 = y0 + h;                     // Konec prvku
            int y8 = y9 - 2;                     // Konec textu

            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            if (isRound)
            {
                path.AddArc(x0, y0, ex, ey, 90f, 180f);    // Levý půlkruh
                path.AddLine(x1, y0, x8, y0);              // Horní rovná čára
                path.AddArc(x6, y0, ex, ey, 270f, 180f);   // Pravý půlkruh
                path.AddLine(x8, y9, x1, y9);              // Dolní rovná čára
            }
            else
            {
                path.AddLine(x0, y9, x0, y0);              // Levá rovná čára
                path.AddLine(x0, y0, x9, y0);              // Horní rovná čára
                path.AddLine(x9, y0, x9, y9);              // Pravá rovná čára
                path.AddLine(x9, y9, x0, y9);              // Dolní rovná čára
            }
            path.CloseFigure();

            iconBounds = new Rectangle(x2, y2, ih, ih);
            textBounds = new Rectangle(x4, y1, x7 - x4, y8 - y1);

            return path;
        }
        /// <summary>
        /// Obsahuje aktuálně platnou barvu pro vykreslení pozadí.
        /// </summary>
        protected override Color CurrentBackColor
        {
            get
            {
                TagFilter owner = this.Owner;

                if (this.IsSelectAll)
                {   // Prvek "vyber vše" má jiné barevné schema:
                    if (this.CheckedSilent)
                        return GetFirstColor(owner?.SelectAllItemCheckedBackColor, Skin.TagFilter.SelectAllItemCheckedBackColor);
                    return GetFirstColor(owner?.SelectAllItemBackColor, Skin.TagFilter.SelectAllItemBackColor);
                }

                if (this.CheckedSilent)
                    return GetFirstColor(this.ItemCheckedBackColor, owner?.ItemCheckedBackColor, Skin.TagFilter.ItemCheckedBackColor);
                return GetFirstColor(this.ItemBackColor, owner?.ItemBackColor, Skin.TagFilter.ItemBackColor);
            }
        }
        /// <summary>
        /// Obsahuje aktuálně platnou barvu pro vykreslení rámečku.
        /// </summary>
        protected Color CurrentBorderColor
        {
            get
            {
                TagFilter owner = this.Owner;
                return GetFirstColor(this.ItemBorderColor, owner?.ItemBorderColor, Skin.TagFilter.ItemBorderColor);
            }
        }
        /// <summary>
        /// Obsahuje aktuálně platnou barvu pro vykreslení rámečku.
        /// </summary>
        protected Color CurrentTextColor
        {
            get
            {
                TagFilter owner = this.Owner;
                return GetFirstColor(this.ItemTextColor, owner?.ItemTextColor, Skin.TagFilter.ItemTextColor);
            }
        }
        /// <summary>
        /// Vrátí první barvu, která má hodnotu.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected static Color GetFirstColor(params Color?[] values)
        {
            foreach (Color? value in values)
            {
                if (value.HasValue) return value.Value;
            }
            return Color.Gray;
        }
        /// <summary>
        /// Obsahuje aktuálně platnou výšku prvku
        /// </summary>
        protected int CurrentItemHeight { get { ITagFilter iOwner = this.IOwner; return (iOwner != null ? iOwner.CurrentItemHeight : Skin.TagFilter.ItemHeight); } }
        /// <summary>
        /// Obsahuje aktuálně platné rozestupy mezi prvky
        /// </summary>
        protected Size CurrentItemSpacing { get { ITagFilter iOwner = this.IOwner; return (iOwner != null ? iOwner.CurrentItemSpacing : Skin.TagFilter.ItemSpacing); } }
        /// <summary>
        /// Obsahuje aktuálně platnou ikonu pro Checked prvky
        /// </summary>
        protected int CurrentRoundPercent { get { ITagFilter iOwner = this.IOwner; return (iOwner != null ? iOwner.CurrentRoundPercent : 0); } }
        /// <summary>
        /// Obsahuje aktuálně platnou ikonu pro Checked prvky
        /// </summary>
        protected Image CurrentCheckedImage { get { ITagFilter iOwner = this.IOwner; return (iOwner != null ? iOwner.CurrentCheckedImage : null); } }
        /// <summary>
        /// Obsahuje aktuálně platnou velikost ikony pro Checked prvky
        /// </summary>
        protected Size CurrentCheckedImageSize { get { ITagFilter iOwner = this.Owner as ITagFilter; return (iOwner != null ? iOwner.CurrentCheckedImageSize : new Size()); } }
        #endregion
    }
    #endregion
}
