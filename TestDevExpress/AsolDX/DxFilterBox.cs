// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using DevExpress.Utils;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel obsahující malý filtr, používá se např. v <see cref="DxTreeList"/>
    /// </summary>
    public class DxFilterBox : DxPanelControl
    {
        #region Konstrukce a inicializace
        /// <summary>
        /// Konstruktor.
        /// Panel obsahující malý filtr, používá se např. v <see cref="DxTreeList"/>
        /// </summary>
        public DxFilterBox()
        {
            Initialize();
        }
        /// <summary>
        /// Inicializace panelu a jeho komponent
        /// </summary>
        protected void Initialize()
        {
            _Margins = 1;
            _OperatorButtonImageDefault = ImageName.DxFilterBoxMenu;
            _OperatorButtonImageName = null;
            _ClearButtonImageDefault = ImageName.DxFilterClearFilter;
            _ClearButtonImage = null;
            _ClearButtonToolTipTitle = DxComponent.Localize(MsgCode.DxFilterBoxClearTipTitle);
            _ClearButtonToolTipText = DxComponent.Localize(MsgCode.DxFilterBoxClearTipText);

            _OperatorButton = DxComponent.CreateDxMiniButton(0, 0, 24, 24, this, OperatorButton_Click, tabStop: false);
            _FilterText = DxComponent.CreateDxTextEdit(24, 0, 200, this, tabStop: true);
            _FilterText.KeyDown += FilterText_KeyDown;
            _FilterText.KeyUp += FilterText_KeyUp;
            _FilterText.EditValueChanged += _FilterText_EditValueChanged;

            _ClearButton = DxComponent.CreateDxMiniButton(224, 0, 24, 24, this, ClearButton_Click, tabStop: false);

            _FilterText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            this.Leave += DxFilterBox_Leave;

            AcceptOperators();
            _CurrentText = "";
            FilterValueChangedSources = DxFilterBoxChangeEventSource.Default;
            LastFilterValue = null;
        }
        private string _OperatorButtonImageDefault;
        private Image _OperatorButtonImage;
        private string _OperatorButtonImageName;
        private string _OperatorButtonToolTipTitle;
        private string _OperatorButtonToolTipText;
        private string _ClearButtonImageDefault;
        private string _ClearButtonImage;
        private string _ClearButtonToolTipTitle;
        private string _ClearButtonToolTipText;
        private DxSimpleButton _OperatorButton;
        private DxTextEdit _FilterText;
        private DxSimpleButton _ClearButton;
        #endregion
        #region Layout
        /// <summary>
        /// Po změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Po změně Parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Po změně Zoomu (pomocí <see cref="DxComponent.CallListeners{T}()"/>
        /// </summary>
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            this.DoLayout();
        }
        /// <summary>
        /// Po změně Stylu (pomocí <see cref="DxComponent.CallListeners{T}()"/>
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            this.DoLayout();
        }
        /// <summary>
        /// Rozmístí svoje prvky a upraví svoji výšku
        /// </summary>
        protected void DoLayout()
        {
            if (_InDoLayoutProcess) return;
            try
            {
                _InDoLayoutProcess = true;

                // tlačítko '_OperatorButton' může být neviditelné!
                bool operatorButtonVisible = _OperatorButton.VisibleInternal;

                // Výška textu, výška vnitřní, vnější (reagujeme i na Zoom a Skin):
                int margins = Margins;
                int margins2 = 2 * margins;
                int minButtonHeight = DxComponent.ZoomToGui(24);
                int minHeight = minButtonHeight + margins2;
                var clientSize = this.ClientSize;
                int currentHeight = this.Size.Height;
                int border = currentHeight - clientSize.Height;
                int textHeight = _FilterText.Height;
                int innerHeight = (textHeight < minHeight ? minHeight : textHeight);
                int outerHeight = innerHeight + border;
                if (currentHeight != outerHeight) this.Height = outerHeight;            // Tady se vyvolá událost OnClientSizeChanged() a z ní rekurzivně zdejší metoda, ale ignoruje se protože (_InDoLayoutProcess = true;)

                // Souřadnice buttonů a textu:
                int buttonCount = (operatorButtonVisible ? 2 : 1);
                int buttonSize = innerHeight - margins2;
                int spaceX = 1;
                int y = margins;
                int x = margins;
                int textWidth = clientSize.Width - margins2 - (buttonCount * (buttonSize + spaceX));
                int textY = (innerHeight - textHeight) / 2;
                if (operatorButtonVisible) { _OperatorButton.Bounds = new Rectangle(x, y, buttonSize, buttonSize); x += (buttonSize + spaceX); }
                _FilterText.Bounds = new Rectangle(x, textY, textWidth, textHeight); x += (textWidth + spaceX);
                _ClearButton.Bounds = new Rectangle(x, y, buttonSize, buttonSize); x += (buttonSize + spaceX);

                OperatorButtonRefresh();
                ClearButtonRefresh();
            }
            finally
            {
                _InDoLayoutProcess = false;
            }
        }
        /// <summary>
        /// Refreshuje button Operator (Image a ToolTip)
        /// </summary>
        protected void OperatorButtonRefresh() { ButtonRefresh(_OperatorButton, _OperatorButtonImageName, _OperatorButtonToolTipTitle, _OperatorButtonToolTipText, _OperatorButtonImageDefault); }
        /// <summary>
        /// Refreshuje button Clear (Image a ToolTip)
        /// </summary>
        protected void ClearButtonRefresh() { ButtonRefresh(_ClearButton, _ClearButtonImage, _ClearButtonToolTipTitle, _ClearButtonToolTipText, _ClearButtonImageDefault); }
        /// <summary>
        /// Pro daný button refreshuje jeho Image a ToolTip
        /// </summary>
        /// <param name="button"></param>
        /// <param name="imageName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="imageNameDefault"></param>
        private void ButtonRefresh(DxSimpleButton button, string imageName, string toolTipTitle, string toolTipText, string imageNameDefault = null)
        {
            if (button == null) return;
            if (String.IsNullOrEmpty(imageName)) imageName = imageNameDefault;
            int buttonSize = button.Width;
            Size imageSize = new Size(buttonSize - 4, buttonSize - 4);
            DxComponent.ApplyImage(button.ImageOptions, imageName, null, ResourceImageSizeType.Small, imageSize, true);
            button.SetToolTip(toolTipTitle, toolTipText);
        }
        /// <summary>
        /// Probíhá přepočet layoutu
        /// </summary>
        private bool _InDoLayoutProcess;
        #endregion
        #region Public properties
        /// <summary>
        /// Okraje kolem prvků, platný rozsah (0 - 10)
        /// </summary>
        public int Margins { get { return _Margins; } set { int m = value; _Margins = (m < 0 ? 0 : (m > 10 ? 10 : value)); this.RunInGui(DoLayout); } }
        private int _Margins;
        /// <summary>
        /// Položky v nabídce typů filtru. 
        /// Lze setovat, lze modifikovat. Pokud bude modifikován ten operátor, který je zrovna vybraný, pak je vhodné zavolat metodu <see cref="FilterOperatorsRefresh()"/>,
        /// aby se změny promítly do GUI.
        /// Pokud bude vloženo null nebo prázdné pole, pak tlačítko vlevo nebude zobrazeno vůbec, a v hodnotě FilterValue bude Operator = null.
        /// </summary>
        public List<IMenuItem> FilterOperators { get { return _FilterOperators; } set { _FilterOperators = value; this.RunInGui(AcceptOperators); } }
        private List<IMenuItem> _FilterOperators;
        /// <summary>
        /// Refreshuje operátory z pole <see cref="FilterOperators"/> do GUI
        /// </summary>
        public void FilterOperatorsRefresh() { this.RunInGui(AcceptOperators); }
        /// <summary>
        /// Vytvoří a vrátí defaultní položky menu
        /// </summary>
        /// <returns></returns>
        public static List<IMenuItem> CreateDefaultOperatorItems(FilterBoxOperatorItems items)
        {
            List<IMenuItem> menuItems = new List<IMenuItem>();

            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.Contains, "%", ImageName.DxFilterOperatorContains, MsgCode.DxFilterOperatorContainsText, MsgCode.DxFilterOperatorContainsTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.DoesNotContain, "!%", ImageName.DxFilterOperatorDoesNotContain, MsgCode.DxFilterOperatorDoesNotContainText, MsgCode.DxFilterOperatorDoesNotContainTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.StartsWith, "=", ImageName.DxFilterOperatorStartWith, MsgCode.DxFilterOperatorStartWithText, MsgCode.DxFilterOperatorStartWithTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.DoesNotStartWith, "!", ImageName.DxFilterOperatorDoesNotStartWith, MsgCode.DxFilterOperatorDoesNotStartWithText, MsgCode.DxFilterOperatorDoesNotStartWithTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.EndsWith, "/", ImageName.DxFilterOperatorEndWith, MsgCode.DxFilterOperatorEndWithText, MsgCode.DxFilterOperatorEndWithTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.DoesNotEndWith, "!/", ImageName.DxFilterOperatorDoesNotEndWith, MsgCode.DxFilterOperatorDoesNotEndWithText, MsgCode.DxFilterOperatorDoesNotEndWithTip, menuItems);

            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.Like, "", ImageName.DxFilterOperatorLike, MsgCode.DxFilterOperatorLikeText, MsgCode.DxFilterOperatorLikeTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.NotLike, "", ImageName.DxFilterOperatorNotLike, MsgCode.DxFilterOperatorNotLikeText, MsgCode.DxFilterOperatorNotLikeTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.Match, "", ImageName.DxFilterOperatorMatch, MsgCode.DxFilterOperatorMatchText, MsgCode.DxFilterOperatorMatchTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.DoesNotMatch, "", ImageName.DxFilterOperatorDoesNotMatch, MsgCode.DxFilterOperatorDoesNotMatchText, MsgCode.DxFilterOperatorDoesNotMatchTip, menuItems);

            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.LessThan, "<", ImageName.DxFilterOperatorLessThan, MsgCode.DxFilterOperatorLessThanText, MsgCode.DxFilterOperatorLessThanTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.LessThanOrEqualTo, "<=", ImageName.DxFilterOperatorLessThanOrEqualTo, MsgCode.DxFilterOperatorLessThanOrEqualToText, MsgCode.DxFilterOperatorLessThanOrEqualToTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.Equals, "=", ImageName.DxFilterOperatorEquals, MsgCode.DxFilterOperatorEqualsText, MsgCode.DxFilterOperatorEqualsTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.NotEquals, "<>", ImageName.DxFilterOperatorNotEquals, MsgCode.DxFilterOperatorNotEqualsText, MsgCode.DxFilterOperatorNotEqualsTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.GreaterThanOrEqualTo, ">=", ImageName.DxFilterOperatorGreaterThanOrEqualTo, MsgCode.DxFilterOperatorGreaterThanOrEqualToText, MsgCode.DxFilterOperatorGreaterThanOrEqualToTip, menuItems);
            CreateDefaultOperatorItem(items, FilterBoxOperatorItems.GreaterThan, ">", ImageName.DxFilterOperatorGreaterThan, MsgCode.DxFilterOperatorGreaterThanText, MsgCode.DxFilterOperatorGreaterThanTip, menuItems);

            return menuItems;
        }
        private static void CreateDefaultOperatorItem(FilterBoxOperatorItems items, FilterBoxOperatorItems value, string hotKey, string imageName, string textCode, string toolTipCode, List<IMenuItem> menuItems)
        {
            if (!items.HasFlag(value)) return;

            menuItems.Add(new DataMenuItem()
            {
                ItemId = value.ToString(),
                HotKey = hotKey,
                ImageName = imageName,
                Text = DxComponent.Localize(textCode),
                ToolTipText = DxComponent.Localize(toolTipCode),
                Checked = false,
                Tag = value
            });
        }
        /// <summary>
        /// Metoda v daném poli <paramref name="menuItems"/> zkusí najít takovou položku, jejíž <see cref="IMenuItem.HotKey"/> == začátek zadaného textu <paramref name="text"/>.
        /// Pokud ji najde, pak zadaný text zkrátí o konkrétní prefix a nalezenou položku vloží do out parametru, vrací true.
        /// Pokud nenajde, vrací false.
        /// <para/>
        /// Pokud tedy vstupní <paramref name="text"/> = "%kdekoliv" a v poli <paramref name="menuItems"/> existuje prvek, jehož <see cref="IMenuItem.HotKey"/> = "%",
        /// pak tento prvek bude umístěn do out <paramref name="menuItem"/>,  výstupní obsah <paramref name="text"/> = "kdekoliv" a výstupem bude true.
        /// </summary>
        /// <param name="menuItems"></param>
        /// <param name="text"></param>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        public static bool SearchMenuItemForHotKeyPrefix(IEnumerable<IMenuItem> menuItems, ref string text, out IMenuItem menuItem)
        {
            menuItem = null;
            if (menuItems == null || String.IsNullOrEmpty(text)) return false;
            string t = text;
            menuItem = menuItems.FirstOrDefault(i => (!String.IsNullOrEmpty(i.HotKey) && t.Length >= i.HotKey.Length && t.StartsWith(i.HotKey)));
            if (menuItem == null) return false;
            text = text.Substring(menuItem.HotKey.Length);
            return true;
        }
        /// <summary>
        /// Aktuální hodnota filtru. Lze setovat. Setování ale nevyvolá událost <see cref="FilterValueChanged"/>.
        /// <para/>
        /// Pozor - při setování hodnoty bude aktivován ten operátor ze zdejšího pole <see cref="FilterOperators"/>, 
        /// který má shodnou hodnotu v <see cref="ITextItem.ItemId"/>, jakou má dodaný operátor.
        /// </summary>
        public DxFilterBoxValue FilterValue
        {
            get { return this.CurrentFilterValue; }
            set
            {
                string operatorId = value?.FilterOperator?.ItemId;
                var filterOperators = this.FilterOperators;
                this._CurrentFilterOperator = (filterOperators != null && operatorId != null ? filterOperators.FirstOrDefault(op => String.Equals(op.ItemId, operatorId)) : null);

                string text = value?.FilterText ?? "";
                this._CurrentText = text;
                this._CurrentValue = value?.FilterValue;

                this.RunInGui(() => { ActivateCurrentFilterOperator(); FilterTextSetSilent(text); });
            }
        }
        /// <summary>
        /// Za jakých událostí se volá event <see cref="FilterValueChanged"/>
        /// </summary>
        public DxFilterBoxChangeEventSource FilterValueChangedSources { get; set; }
        /// <summary>
        /// Událost volaná po hlídané změně obsahu filtru.
        /// Argument obsahuje hodnotu filtru a druh události, která vyvolala event.
        /// Druhy události, pro které se tento event volá, lze nastavit v <see cref="FilterValueChangedSources"/>.
        /// </summary>
        public event EventHandler<DxFilterBoxChangeArgs> FilterValueChanged;
        /// <summary>
        /// Událost volaná po stisku klávesy Enter, vždy, tedy jak po změně textu i bez změny textu.
        /// Pokud dojde ke změně textu, pak je pořadí: <see cref="FilterValueChanged"/>, <see cref="KeyEnterPress"/>.
        /// </summary>
        public event EventHandler KeyEnterPress;
        /// <summary>
        /// Defaultní jméno ikony na tlačítku Operator
        /// </summary>
        public string OperatorButtonImageDefault { get { return _OperatorButtonImageDefault; } set { _OperatorButtonImageDefault = value; this.RunInGui(OperatorButtonRefresh); } }
        /// <summary>
        /// Jméno ikony na tlačítku Clear
        /// </summary>
        public string ClearButtonImage { get { return _ClearButtonImage; } set { _ClearButtonImage = value; this.RunInGui(ClearButtonRefresh); } }
        /// <summary>
        /// Titulek tooltipu na tlačítku Clear
        /// </summary>
        public string ClearButtonToolTipTitle { get { return _ClearButtonToolTipTitle; } set { _ClearButtonToolTipTitle = value; this.RunInGui(ClearButtonRefresh); } }
        /// <summary>
        /// Text tooltipu na tlačítku Clear
        /// </summary>
        public string ClearButtonToolTipText { get { return _ClearButtonToolTipText; } set { _ClearButtonToolTipText = value; this.RunInGui(ClearButtonRefresh); } }
        #endregion
        #region Privátní interaktivita
        /// <summary>
        /// Po vložení sady operátorů
        /// </summary>
        protected void AcceptOperators()
        {
            SetVisibleOperatorButton();
            if (!FilterOperatorsExists) return;
            ActivateFirstCheckedOperator(false);
            ReloadLastFilter();
        }
        /// <summary>
        /// Zajistí, že operátor <see cref="_CurrentFilterOperator"/> bude aktivní. Tedy bude Checked a bude zobrazen v tlačítku.
        /// </summary>
        protected void ActivateCurrentFilterOperator()
        {
            if (this.FilterOperators == null) return;
            ApplyCurrentOperator(true, true);
            ReloadLastFilter();
        }
        /// <summary>
        /// V poli <see cref="FilterOperators"/> najde první položku, která je ItemIsChecked (anebo první obecně) a tu prohlásí za vybranou.
        /// </summary>
        private void ActivateFirstCheckedOperator(bool runEvent)
        {
            IMenuItem activeOperator = null;
            var filterItems = this.FilterOperators;
            if (filterItems != null && filterItems.Count > 0)
            {
                activeOperator = filterItems.FirstOrDefault(f => (f.Checked ?? false));
                if (activeOperator == null) activeOperator = filterItems[0];
            }
            ActivateOperator(activeOperator, runEvent);
        }
        /// <summary>
        /// Aktivuje dodanou položku jako právě aktivní operátor.
        /// </summary>
        /// <param name="activeOperator"></param>
        /// <param name="runEvent"></param>
        private void ActivateOperator(IMenuItem activeOperator, bool runEvent)
        {
            _CurrentFilterOperator = activeOperator;
            ApplyCurrentOperator(true, true);
            if (runEvent && CallChangedEventOn(DxFilterBoxChangeEventSource.OperatorChange) && this.CurrentFilterIsChanged)
                RunFilterValueChanged(DxFilterBoxChangeEventSource.OperatorChange);
        }
        /// <summary>
        /// Nastaví viditelnost buttonu <see cref="_OperatorButton"/> podle existence nabídek operátorů.
        /// Pokud dojde ke změně viditelnosti, vyvolá přepočet layoutu this prvku = zajistí správné rozmístění controlů.
        /// </summary>
        private void SetVisibleOperatorButton()
        {
            if (_OperatorButton == null) return;
            bool operatorsExists = FilterOperatorsExists;
            bool buttonIsVisible = _OperatorButton.VisibleInternal;
            if (buttonIsVisible == operatorsExists) return;
            _OperatorButton.VisibleInternal = operatorsExists;
            DoLayout();
        }
        /// <summary>
        /// Aplikuje ikonu a tooltip z aktuální položky <see cref="_CurrentFilterOperator"/> do buttonu <see cref="_OperatorButton"/>.
        /// </summary>
        /// <param name="setChecked">Nastavit v poli operátorů <see cref="FilterOperators"/> hodnotu <see cref="IMenuItem.Checked"/> na true/false pro vybraný záznam</param>
        /// <param name="applyToButton"></param>
        protected virtual void ApplyCurrentOperator(bool setChecked, bool applyToButton)
        {
            var currentOperator = _CurrentFilterOperator;

            if (setChecked)
            {   // Označíme si odpovídající položku (podle ItemId) v nabídce jako Checked, ostatní jako UnChecked:
                string currentFilterId = currentOperator?.ItemId;
                var filterTypes = _FilterOperators;
                if (filterTypes != null)
                    filterTypes.ForEachExec(i => i.Checked = String.Equals(i.ItemId, currentFilterId));
            }

            if (applyToButton)
            {   // Z aktuálního filtru přečteme jeho data a promítneme je do tlačítka:
                _OperatorButtonImage = currentOperator?.Image;
                _OperatorButtonImageName = currentOperator?.ImageName;
                _OperatorButtonToolTipTitle = currentOperator?.ToolTipTitle;
                _OperatorButtonToolTipText = currentOperator?.ToolTipText;
                OperatorButtonRefresh();
            }
        }
        /// <summary>
        /// Aktivuje menu položek typů filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OperatorButton_Click(object sender, EventArgs args)
        {
            var filterItems = this.FilterOperators;
            if (filterItems == null || filterItems.Count == 0) return;
            var popup = DxComponent.CreateDXPopupMenu(filterItems, "", showCheckedAsBold: true, itemClick: OperatorItem_Click);
            Point location = new Point(0, this.Height);
            popup.ShowPopup(this, location);
        }
        /// <summary>
        /// Po výběru položky s typem filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OperatorItem_Click(object sender, TEventArgs<IMenuItem> args)
        {
            ActivateOperator(args.Item, true);
            _FilterText.Focus();
        }
        /// <summary>
        /// Po stisku klávesy v TextBoxu reagujeme na Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterText_KeyDown(object sender, KeyEventArgs e)
        {   // Down musí řešit jen Enter:
            if (e.KeyData == Keys.Enter)
            {   // Pouze samotný Enter, nikoli CtrlEnter nebo ShiftEnter:
                SearchHotKeyMenuItem();
                FilterText_OnKeyEnter();
                e.Handled = true;
            }
        }
        /// <summary>
        /// Po změně editované hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterText_EditValueChanged(object sender, EventArgs e)
        {
            FilterText_ValueChanged();
        }
        /// <summary>
        /// Po uvolnění stisku klávesy detekujeme změnu hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterText_KeyUp(object sender, KeyEventArgs e)
        {   // KeyUp neřeší Enter, ale řeší změny textu
            FilterText_ValueChanged();
        }
        /// <summary>
        /// Když focus opouští FilterBox, můžeme hlásit změnu hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxFilterBox_Leave(object sender, EventArgs e)
        {
            if (this.CallChangedEventOn(DxFilterBoxChangeEventSource.LostFocus) && this.CurrentFilterIsChanged)
                RunFilterValueChanged(DxFilterBoxChangeEventSource.LostFocus);
        }
        /// <summary>
        /// Metoda zkusí najít operátor podle prefixu v zadaném textu.
        /// Tato metoda nevolá událost <see cref="FilterValueChanged"/>.
        /// </summary>
        private void SearchHotKeyMenuItem()
        {
            string text = (_FilterText.Text ?? "");
            if (!SearchMenuItemForHotKeyPrefix(this.FilterOperators, ref text, out var hotItem)) return;
            _CurrentFilterOperator = hotItem;
            ApplyCurrentOperator(true, true);
            FilterTextSetSilent(text);
        }
        /// <summary>
        /// Po stisku Enter v textu filtru
        /// </summary>
        private void FilterText_OnKeyEnter()
        {
            if (this.CallChangedEventOn(DxFilterBoxChangeEventSource.KeyEnter) && this.CurrentFilterIsChanged)
                RunFilterValueChanged(DxFilterBoxChangeEventSource.KeyEnter);

            RunKeyEnterPress();
        }
        /// <summary>
        /// Po možné změně hodnoty v textu
        /// </summary>
        private void FilterText_ValueChanged()
        {
            _CurrentText = (_FilterText.Text ?? "");       // Stínování hodnoty: aby hodnota textboxu byla čitelná i z jiných threadů

            if (!FilterTextSilentChange && this.CallChangedEventOn(DxFilterBoxChangeEventSource.TextChange) && this.CurrentFilterIsChanged)
                RunFilterValueChanged(DxFilterBoxChangeEventSource.TextChange);
        }
        /// <summary>
        /// Metoda vloží daný text do textboxu, ale neprovede událost <see cref="RunFilterValueChanged(DxFilterBoxChangeEventSource)"/>.
        /// </summary>
        /// <param name="text"></param>
        private void FilterTextSetSilent(string text)
        {
            bool isSilent = FilterTextSilentChange;
            try
            {
                FilterTextSilentChange = true;
                _FilterText.Text = text;
                _CurrentText = text;
            }
            finally
            {
                FilterTextSilentChange = isSilent;
            }
        }
        /// <summary>
        /// Obsahuje true pokud změna textu v <see cref="_FilterText"/> NEMÁ vyvolat událost <see cref="RunFilterValueChanged(DxFilterBoxChangeEventSource)"/>.
        /// Běžně je false.
        /// </summary>
        private bool FilterTextSilentChange = false;
        /// <summary>
        /// Proběhne po stisku klávesy Enter v textboxu, vždy, i beze změny textu
        /// </summary>
        private void RunKeyEnterPress()
        {
            OnKeyEnterPress();
            KeyEnterPress?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po stisku klávesy Enter v textboxu, vždy, i beze změny textu
        /// </summary>
        protected virtual void OnKeyEnterPress() { }
        /// <summary>
        /// Tlačítko Clear smaže obsah filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ClearButton_Click(object sender, EventArgs args)
        {
            // Následující pořadí kroků zajistí, že provedení změny textu (_FilterText.Text = "") sice vyvolá nativní eventhandler FilterText_ValueChanged,
            // ale v té době už bude CurrentFilterIsChanged = false, protože tam se vyhodnocuje _CurrentText a to už bude shodné s LastValue
            _CurrentText = "";              // Stínování hodnoty: aby hodnota textboxu byla čitelná i z jiných threadů

            bool callEvent = (this.CallChangedEventOn(DxFilterBoxChangeEventSource.ClearButton) && this.CurrentFilterIsChanged);
            if (callEvent)                  // Jen pokud my budeme volat událost FilterValueChanged (tam se uživatel dozví o změně dané ClearButtonem). Pokud bychom my nevolali tento event (tj. když FilterValueChangedSources neobsahuje ClearButton), pak LastFilterValue necháme dosavadní, a změnu hodnoty textu zaregistruje event FilterText_ValueChanged.
                this.ReloadLastFilter();    // Tady se do LastFilterValue dostane text z _CurrentText, tedy ""
            FilterTextSetSilent("");        // Tady sice proběhne event FilterText_ValueChanged, ale nebudeme volat CurrentFilterChanged.
            if (callEvent)
                RunFilterValueChanged(DxFilterBoxChangeEventSource.ClearButton);

            _FilterText.Focus();
        }
        /// <summary>
        /// Proběhne po změně hodnoty filtru.
        /// Metoda vyvolá <see cref="OnFilterValueChanged(DxFilterBoxChangeArgs)"/> a event <see cref="FilterValueChanged"/>.
        /// <para/>
        /// Metoda nastaví <see cref="LastFilterValue"/> = <see cref="CurrentFilterValue"/> (tedy poslední známá hodntoa filtru = aktuální hodnota).
        /// Tím se změní hodnota <see cref="CurrentFilterIsChanged"/> na false = filtr od této chvíle neobsahuje změnu.
        /// </summary>
        private void RunFilterValueChanged(DxFilterBoxChangeEventSource eventSource)
        {
            var currentFilter = this.CurrentFilterValue;
            DxFilterBoxChangeArgs args = new DxFilterBoxChangeArgs(currentFilter, eventSource);
            this.LastFilterValue = currentFilter;          // Od teď bude hodnota CurrentFilterIsChanged = false;
            OnFilterValueChanged(args);
            FilterValueChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Po změně hodnoty filtru, dle nastavených zdrojů události
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFilterValueChanged(DxFilterBoxChangeArgs args) { }
        /// <summary>
        /// Vrátí true, pokud v <see cref="FilterValueChangedSources"/> je nastavený některý bit z dodané hodnoty.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected bool CallChangedEventOn(DxFilterBoxChangeEventSource source)
        {
            return this.FilterValueChangedSources.HasFlag(source);
        }
        /// <summary>
        /// Aktuální stav filtru: obsahuje typ filtru (<see cref="_CurrentFilterOperator"/>:ItemId) a aktuálně zadaný text (<see cref="_CurrentText"/>).
        /// Používá se po stisku klávesy Enter pro detekci změny hodnoty filtru (tam se zohlední i změna typu filtru bez změny zadaného textu).
        /// <para/>
        /// Nikdy není null, vždy obshauje new instanci, které v sobě obsahuje aktuálně platné hodnoty.
        /// </summary>
        protected DxFilterBoxValue CurrentFilterValue { get { return new DxFilterBoxValue(_CurrentFilterOperator, _CurrentText, _CurrentValue); } }
        /// <summary>
        /// Posledně známý obsah filtru <see cref="CurrentFilterValue"/>, který byl předán do události <see cref="FilterValueChanged"/>.
        /// Může být null (na počátku).
        /// Ve vhodném čase se vyvolá tato událost a aktualizuje se tato hodnota.
        /// Aktualizuje se rovněž po aktualizaci <see cref="FilterValue"/> z aplikace, aby se následný event hlásil jen při reálné změně.
        /// </summary>
        protected DxFilterBoxValue LastFilterValue { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud aktuální hodnota filtru <see cref="CurrentFilterValue"/> je jiná než předchozí hodnota <see cref="LastFilterValue"/>.
        /// Akceptuje tedy aktuální text v textboxu a aktuálně vybraný typ filtru (=Current), porovná s dřívějším stavem (Last).
        /// </summary>
        protected bool CurrentFilterIsChanged { get { return !CurrentFilterValue.IsEqual(LastFilterValue); } }
        /// <summary>
        /// Aktualizuje hodnotu <see cref="LastFilterValue"/> z hodnoty <see cref="CurrentFilterValue"/>.
        /// Po provedení této metody bude <see cref="CurrentFilterIsChanged"/> = false (tedy 'nemáme změnu filtru').
        /// </summary>
        protected void ReloadLastFilter() { LastFilterValue = CurrentFilterValue; }
        /// <summary>
        /// Obsahuje true, pokud existují zadané operátory
        /// </summary>
        protected bool FilterOperatorsExists { get { return (FilterOperators != null && FilterOperators.Count(i => (i != null)) > 0); } }
        /// <summary>
        /// Aktuální operátor filtru.
        /// </summary>
        private IMenuItem _CurrentFilterOperator;
        /// <summary>
        /// Aktuální text. V procesu editace je sem stínován.
        /// </summary>
        private string _CurrentText;
        /// <summary>
        /// Aktuální hodnota. V procesu editace je sem stínována.
        /// </summary>
        private object _CurrentValue;
        #endregion
    }
    #region Enumy DxFilterRowChangeEventSource a FilterBoxOperatorItems, třída DxFilterBoxValue = aktuální hodnota
    /// <summary>
    /// Spouštěcí události pro event <see cref="DxFilterBox.FilterValueChanged"/>
    /// </summary>
    [Flags]
    public enum DxFilterBoxChangeEventSource
    {
        /// <summary>Nikdy</summary>
        None = 0x00,
        /// <summary>Po jakékoli změně textu (i v procesu editace)</summary>
        TextChange = 0x01,
        /// <summary>Po změně textu v LostFocus (tedy odchod myší, tabulátorem aj.)</summary>
        LostFocus = 0x02,
        /// <summary>Pouze klávesou Enter</summary>
        KeyEnter = 0x04,
        /// <summary>Po změně operátoru</summary>
        OperatorChange = 0x10,
        /// <summary>Po vymazání textu tlačítkem Clear</summary>
        ClearButton = 0x20,
        /// <summary>Aplikační default = <see cref="LostFocus"/> + <see cref="KeyEnter"/> + <see cref="OperatorChange"/> + <see cref="ClearButton"/>;</summary>
        Default = LostFocus | KeyEnter | OperatorChange | ClearButton,
        /// <summary>Zelený default = <see cref="KeyEnter"/> + <see cref="ClearButton"/>;</summary>
        DefaultGreen = KeyEnter | ClearButton
    }
    /// <summary>
    /// Povolené operátory v controlu FilterBox. 
    /// Použivá se pro určení povolených operátorů v controlu FilterBox.
    /// Jde o přesnou kopii enumu: Noris.WS.DataContracts.DataTypes.FilterBoxOperatorItems včetně nesprávné hodnoty <see cref="Contains"/> = 0
    /// </summary>
    [Flags]
    public enum FilterBoxOperatorItems : uint
    {   // DAJ 0068975 5.8.2021 posunul numerické hodnoty o 1bit doleva (Contains z 0 na 1, atd), shodně v Noris.WS.DataContracts.DataTypes.FilterBoxOperatorItems
        /// <summary>Obsahuje</summary>
        Contains = 0x0001,
        /// <summary>Neobsahuje ...</summary>
        DoesNotContain = 0x0002,
        /// <summary>Nekončí na ...</summary>
        DoesNotEndWith = 0x0004,
        /// <summary>Neodpovídá ...</summary>
        DoesNotMatch = 0x0008,
        /// <summary>Nezačíná ...</summary>
        DoesNotStartWith = 0x0010,
        /// <summary>Končí na ...</summary>
        EndsWith = 0x0020,
        /// <summary>Je rovno</summary>
        Equals = 0x0040,
        /// <summary>Větší než</summary>
        GreaterThan = 0x0080,
        /// <summary>Větší nebo rovno</summary>
        GreaterThanOrEqualTo = 0x0100,
        /// <summary>Menší než</summary>
        LessThan = 0x0200,
        /// <summary>Menší nebo rovno</summary>
        LessThanOrEqualTo = 0x0400,
        /// <summary>Podobno</summary>
        Like = 0x0800,
        /// <summary>Odpovídá</summary>
        Match = 0x1000,
        /// <summary>Nerovno</summary>
        NotEquals = 0x2000,
        /// <summary>Nepodobno</summary>
        NotLike = 0x4000,
        /// <summary>Začíná na ...</summary>
        StartsWith = 0x8000,

        /// <summary>Default pro texty, ale bez Like a Match</summary>
        DefaultText = Contains | DoesNotContain | StartsWith | DoesNotStartWith | EndsWith | DoesNotEndWith,
        /// <summary>Default pro texty (=<see cref="DefaultText"/>), s přidáním Like a Match</summary>
        DefaultLikeText = DefaultText | Like | NotLike | Match | DoesNotMatch,
        /// <summary>Default pro čísla</summary>
        DefaultNumber = Equals | NotEquals | GreaterThan | GreaterThanOrEqualTo | LessThan | LessThanOrEqualTo
    }
    /// <summary>
    /// Data pro událost o změně filtru
    /// </summary>
    public class DxFilterBoxChangeArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="filterValue"></param>
        /// <param name="eventSource"></param>
        public DxFilterBoxChangeArgs(DxFilterBoxValue filterValue, DxFilterBoxChangeEventSource eventSource)
        {
            FilterValue = filterValue;
            EventSource = eventSource;
        }
        /// <summary>
        /// Hodnota filtru
        /// </summary>
        public DxFilterBoxValue FilterValue { get; private set; }
        /// <summary>
        /// Druh události
        /// </summary>
        public DxFilterBoxChangeEventSource EventSource { get; private set; }
    }
    /// <summary>
    /// Aktuální hodnota filtru
    /// </summary>
    public class DxFilterBoxValue
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="filterOperator"></param>
        /// <param name="filterText"></param>
        /// <param name="filterValue"></param>
        public DxFilterBoxValue(IMenuItem filterOperator, string filterText, object filterValue)
        {
            this.FilterOperator = filterOperator;
            this.FilterText = filterText;
            this.FilterValue = filterValue;
        }
        /// <summary>
        /// Operátor. Může být null.
        /// </summary>
        public IMenuItem FilterOperator { get; private set; }
        /// <summary>
        /// Textová hodnota
        /// </summary>
        public string FilterText { get; private set; }
        /// <summary>
        /// Datová hodnota odpovídající vybranému textu. Při ručním zadání textu je null.
        /// </summary>
        public object FilterValue { get; private set; }
        /// <summary>
        /// Obsahuje true pokud filtr je prázdný = neobsahuje text
        /// </summary>
        public bool IsEmpty { get { return String.IsNullOrEmpty(this.FilterText); } }
        /// <summary>
        /// Vrátí true pokud this instance obsahuje shodná data jako daná instance.
        /// U typu operátoru <see cref="FilterOperator"/> se porovnává hodnota <see cref="IMenuItem.ItemId"/>.
        /// Neporovnává se <see cref="FilterValue"/>, porovnává se text v <see cref="FilterText"/>.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsEqual(DxFilterBoxValue other)
        {
            if (other is null) return false;
            if (!String.Equals(this.FilterOperator?.ItemId, other.FilterOperator?.ItemId)) return false;
            if (!String.Equals(this.FilterText, other.FilterText)) return false;
            return true;
        }
    }
    #endregion
}
