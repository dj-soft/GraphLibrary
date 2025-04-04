// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.TableLayout;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel obsahující <see cref="DxListBoxControl"/> (potomek <see cref="DevExpress.XtraEditors.ImageListBoxControl"/>) plus tlačítka pro přesuny nahoru / dolů
    /// </summary>
    public class DxListBoxPanel : DxPanelControl
    {
        #region Konstruktor, tvorba, privátní proměnné, layout celkový
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxPanel()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace komponent a hodnot
        /// </summary>
        private void Initialize()
        {
            __ListBox = new DxListBoxControl();
            __Buttons = new List<DxSimpleButton>();
            __ButtonsPosition = ToolbarPosition.RightSideCenter;
            __ButtonsTypes = ControlKeyActionType.None;
            __ButtonsSize = ResourceImageSizeType.Medium;
            this.Controls.Add(__ListBox);
            this.Padding = new Padding(0);
            this.ClientSizeChanged += _ClientSizeChanged;
            this.Enter += _Panel_Enter;
            __ListBox.ListItemsChanged += __ListBox_ListItemsChanged;
            __ListBox.UndoRedoEnabled = false;
            __ListBox.UndoRedoEnabledChanged += _ListBox_UndoRedoEnabledChanged;
            __ListBox.SelectedItemsChanged += _ListBox_SelectedItemsChanged;
            __ListBox.ListActionBefore += _RunListActionBefore;
            __ListBox.ListActionAfter += _RunListActionAfter;
            _ItemClickInit();
            _RowFilterInitialize();
            DoLayout();
        }
        /// <summary>
        /// Obsahuje true, pokud List může dostat Focus
        /// </summary>
        public bool CanListFocus { get { return true; } }
        /// <summary>
        /// Aktivuje focus do Listu
        /// </summary>
        public void SetListFocus()
        {
            if (this.CanListFocus)
            {
                this.Select();
                this._MainControlFocus();
            }
        }
        /// <summary>
        /// Vstup do panelu dává vstup do ListBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Panel_Enter(object sender, EventArgs e)
        {
            this._MainControlFocus();
        }
        /// <summary>
        /// Dá Focus do main controlu
        /// </summary>
        private void _MainControlFocus()
        {
            this.__ListBox.Select();
            this.__ListBox.Focus();
        }
        /// <summary>
        /// Proběhne po změně v poli <see cref="ListItems"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __ListBox_ListItemsChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            _SetButtonsEnabledSelection();
        }
        /// <summary>
        /// Po změně velikosti se provede <see cref="DoLayout"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            DoLayout();
        }
        /// <summary>
        /// Po změně DPI
        /// </summary>
        protected override void OnCurrentDpiChanged()
        {
            base.OnCurrentDpiChanged();
            DoLayout();
        }
        /// <summary>
        /// Rozmístí prvky (tlačítka a ListBox) podle pravidel do svého prostoru
        /// </summary>
        protected virtual void DoLayout()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(DoLayout));
            }
            else
            {
                Rectangle innerBounds = this.GetInnerBounds();
                if (innerBounds.Width >= 30 && innerBounds.Height >= 30)
                {
                    _ButtonsLayout(ref innerBounds);
                    _RowFilterLayout(ref innerBounds);
                    __ListBox.Bounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width - 0, innerBounds.Height);
                }
            }
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                _RemoveButtons();
                _RowFilterDispose();
                __ListBox?.Dispose();
            }
            catch { /* Chyby v Dispose občas nastanou v DevExpress, který něco likviduje v GC threadu a nemá přístup do GUI. */ }
        }
        /// <summary>
        /// Instance ListBoxu
        /// </summary>
        private DxListBoxControl __ListBox;
        #endregion
        #region Public prvky
        /// <summary>
        /// ListBox
        /// </summary>
        public DxListBoxControl ListBox { get { return __ListBox; } }
        /// <summary>
        /// Varianta řádkového filtru. Default = None
        /// </summary>
        public FilterRowMode RowFilterMode { get { return __RowFilterMode; } set { _SetRowFilterMode(value); } }
        /// <summary>
        /// Typy dostupných tlačítek
        /// </summary>
        public ControlKeyActionType ButtonsTypes { get { return __ButtonsTypes; } set { __ButtonsTypes = value; _AcceptButtonsType(); DoLayout(); } }
        /// <summary>
        /// Umístění tlačítek
        /// </summary>
        public ToolbarPosition ButtonsPosition { get { return __ButtonsPosition; } set { __ButtonsPosition = value; DoLayout(); } }
        /// <summary>
        /// Velikost tlačítek
        /// </summary>
        public ResourceImageSizeType ButtonsSize { get { return __ButtonsSize; } set { __ButtonsSize = value; DoLayout(); } }
        #endregion
        #region Data = položky, a layout = Template
        /// <summary>
        /// Režim prvků v ListBoxu.
        /// </summary>
        public ListBoxItemsMode ItemsMode { get { return __ListBox.ItemsMode; } }
        #region Jednoduchý List postavený nad položkami IMenuItem
        /// <summary>
        /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
        /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
        /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
        /// </summary>
        public bool DuplicityEnabled { get { return __ListBox.DuplicityEnabled; } set { __ListBox.DuplicityEnabled = value; } }
        /// <summary>
        /// Prvky Listu typované na <see cref="IMenuItem"/>.
        /// Pokud v Listu budou obsaženy jiné prvky než <see cref="IMenuItem"/>, pak na jejich místě v tomto poli bude null.
        /// Toto pole má stejný počet prvků jako pole this.Items
        /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
        /// </summary>
        public IMenuItem[] ListItems { get { return __ListBox.MenuItems; } set { __ListBox.MenuItems = value; } }
        /// <summary>
        /// Aktuálně vybraný prvek typu <see cref="IMenuItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
        /// </summary>
        public IMenuItem SelectedListItem { get { return __ListBox.SelectedMenuItem; } set { __ListBox.SelectedMenuItem = value; } }
        /// <summary>
        /// Prvky Listu
        /// </summary>
        public DevExpress.XtraEditors.Controls.ImageListBoxItemCollection Items { get { return __ListBox.Items; } }

        /// <summary>
        /// Aktuálně označené objekty. Může jich být i více, nebo žádný.
        /// Objekty to mohou být různé, typicky <see cref="IMenuItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID označených řádků je v poli <see cref="SelectedItemsId"/>.
        /// </summary>
        public object[] SelectedItems { get { return __ListBox.SelectedItems; } }
        /// <summary>
        /// Pole obsahující ID selectovaných záznamů.
        /// </summary>
        public object[] SelectedItemsId { get { return __ListBox.SelectedItemsId; } }
        /// <summary>
        /// Prvek, na kterém je kurzor. Je jen jediný, nebo null.
        /// Objekty to mohou být různé, typicky <see cref="IMenuItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID aktivního řádku je v <see cref="CurrentItemId"/>.
        /// </summary>
        public object CurrentItem { get { return __ListBox.CurrentItem; } }
        /// <summary>
        /// Pole obsahující ID aktivního řádku.
        /// </summary>
        public object CurrentItemId { get { return __ListBox.CurrentItemId; } }

        #endregion
        #region Komplexní List postavený nad DataTable a Template
        /// <summary>
        /// Tabulka s daty
        /// </summary>
        public System.Data.DataTable DataTable { get { return __ListBox.DataTable; } set { __ListBox.DataTable = value; } }
        /// <summary>
        /// Šablona pro zobrazení dat z <see cref="DataTable"/>
        /// </summary>
        public DxListBoxTemplate DxTemplate { get { return __ListBox.DxTemplate; } set { __ListBox.DxTemplate = value; } }
        /// <summary>
        /// Metoda vytvoří Simple template pro ikonu a pro text
        /// </summary>
        /// <param name="columnNameItemId"></param>
        /// <param name="columnNameIcon"></param>
        /// <param name="columnNameText"></param>
        /// <param name="columnNameToolTip"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public DxListBoxTemplate CreateSimpleDxTemplate(string columnNameItemId, string columnNameIcon, string columnNameText, string columnNameToolTip = null, int? iconSize = null) { return __ListBox.CreateSimpleDxTemplate(columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize); }
        #endregion
        #endregion
        #region ItemClick
        /// <summary>
        /// Inicializace eventů pro Click a MouseClick
        /// </summary>
        private void _ItemClickInit()
        {
            __ListBox.ItemMouseClick += _ListBox_ItemMouseClick;
            __ListBox.ItemMouseDoubleClick += _ListBox_ItemMouseDoubleClick;
            __ListBox.ItemEnterKeyDown += _ListBox_ItemEnterKeyDown;
        }
        /// <summary>
        /// Eventhandler List.ItemMouseClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ListBox_ItemMouseClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            OnItemMouseClick(args);
            ItemMouseClick?.Invoke(this, args);
        }
        /// <summary>
        /// Eventhandler List.ItemMouseDoubleClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ListBox_ItemMouseDoubleClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            OnItemMouseDoubleClick(args);
            ItemMouseDoubleClick?.Invoke(this, args);
        }
        /// <summary>
        /// Eventhandler List.ItemEnterKeyDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ListBox_ItemEnterKeyDown(object sender, DxListBoxItemKeyEventArgs args)
        {
            OnItemEnterKeyDown(args);
            ItemEnterKeyDown?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po jednoduchém kliknutí na prvek
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemMouseClick(DxListBoxItemMouseClickEventArgs args) { }
        /// <summary>
        /// Proběhne po jednoduchém kliknutí na prvek
        /// </summary>
        public event DxListBoxItemMouseClickDelegate ItemMouseClick;
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemMouseDoubleClick(DxListBoxItemMouseClickEventArgs args) { }
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        public event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick;
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemEnterKeyDown(DxListBoxItemKeyEventArgs args) { }
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        public event DxListBoxItemKeyDelegate ItemEnterKeyDown;
        #endregion
        #region Ctrl+C a Ctrl+V, i mezi controly a mezi aplikacemi
        /// <summary>
        /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
        /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
        /// </summary>
        public string DataExchangeCurrentControlId { get { return __ListBox.DataExchangeCurrentControlId; } set { __ListBox.DataExchangeCurrentControlId = value; } }
        /// <summary>
        /// Režim výměny dat při pokusu o vkládání do tohoto objektu.
        /// Pokud některý jiný objekt provedl Ctrl+C, pak svoje data vložil do balíčku <see cref="DataExchangeContainer"/>,
        /// přidal k tomu svoje ID controlu (jako zdejší <see cref="DataExchangeCurrentControlId"/>) do <see cref="DataExchangeContainer.DataSourceId"/>,
        /// do balíčku se přidalo ID aplikace do <see cref="DataExchangeContainer.ApplicationGuid"/>, a tato data jsou uložena v Clipboardu.
        /// <para/>
        /// Pokud nyní zdejší control zaeviduje klávesu Ctrl+V, pak zjistí, zda v Clipboardu existuje balíček <see cref="DataExchangeContainer"/>,
        /// a pokud ano, pak prověří, zda this control může akceptovat data ze zdroje v balíčku uvedeného, na základě nastavení režimu výměny v <see cref="DataExchangeCrossType"/>
        /// a ID zdrojového controlu podle <see cref="DataExchangeAcceptSourceControlId"/>.
        /// </summary>
        public DataExchangeCrossType DataExchangeCrossType { get { return __ListBox.DataExchangeCrossType; } set { __ListBox.DataExchangeCrossType = value; } }
        /// <summary>
        /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
        /// </summary>
        public string DataExchangeAcceptSourceControlId { get { return __ListBox.DataExchangeAcceptSourceControlId; } set { __ListBox.DataExchangeAcceptSourceControlId = value; } }
        #endregion
        #region DxListBoxControl (přímý přístup na jeho prvky)
        /// <summary>
        /// Režim označování prvků
        /// </summary>
        public SelectionMode SelectionMode { get { return __ListBox.SelectionMode; } set { __ListBox.SelectionMode = value; } }
        /// <summary>
        /// Souhrn povolených akcí Drag and Drop
        /// </summary>
        public DxDragDropActionType DragDropActions { get { return __ListBox.DragDropActions; } set { __ListBox.DragDropActions = value; } }
        /// <summary>
        /// Povolené akce. Výchozí je <see cref="ControlKeyActionType.None"/>
        /// </summary>
        public ControlKeyActionType EnabledKeyActions { get { return __ListBox.EnabledKeyActions; } set { __ListBox.EnabledKeyActions = value; } }

        /// <summary>
        /// Volá se před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RunListActionBefore(object sender, DxListBoxActionCancelEventArgs args)
        {
            OnListActionBefore(args);
            ListActionBefore?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListActionBefore(DxListBoxActionCancelEventArgs e) { }
        /// <summary>
        /// Událost vyvolaná před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        public event DxListBoxActionCancelDelegate ListActionBefore;

        /// <summary>
        /// Volá se po provedení kteréhokoli požadavku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RunListActionAfter(object sender, DxListBoxActionEventArgs args)
        {
            OnListActionAfter(args);
            ListActionAfter?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po provedení kteréhokoli požadavku
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListActionAfter(DxListBoxActionEventArgs e) { }
        /// <summary>
        /// Událost vyvolaná po provedení kteréhokoli požadavku
        /// </summary>
        public event DxListBoxActionDelegate ListActionAfter;

        /// <summary>
        /// Při změně Selected prvků, libovolného typu
        /// </summary>
        private void _RunSelectedItemsChanged()
        {
            OnSelectedItemsChanged();
            SelectedItemsChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se po změně selected prvků.<br/>
        /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
        /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
        /// </summary>
        protected virtual void OnSelectedItemsChanged() { }
        /// <summary>
        /// Událost volaná po změně selected prvků.<br/>
        /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
        /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
        /// </summary>
        public event EventHandler SelectedItemsChanged;

        #endregion
        #region Filtrování položek: klientské / serverové
        /// <summary>
        /// Inicializace řádkového filtrování
        /// </summary>
        private void _RowFilterInitialize()
        {
            __RowFilterMode = FilterRowMode.None;
            this.__ListBox.ListActionBefore += _ListBox_ListActionBefore;
        }
        /// <summary>
        /// Panel je zaháčkovaný na akce ListBoxu, kde panel ošetřuje akce <see cref="ControlKeyActionType.ActivateFilter"/> a <see cref="ControlKeyActionType.FillKeyToFilter"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_ListActionBefore(object sender, DxListBoxActionCancelEventArgs e)
        {
            // Jiné akce ignoruji:
            bool isFilterAction = (e.Action == ControlKeyActionType.ActivateFilter || e.Action == ControlKeyActionType.FillKeyToFilter);
            if (!isFilterAction) return;

            // Pokud já nemám FilterRow, pak akci stornuji, tím si ListBox nebude nastavovat IsHandled = true, a případné klávesy pošle do nativního controlu:
            var filterMode = this.RowFilterMode;
            if (filterMode == FilterRowMode.None) { e.Cancel = true; return; }

            string text = ((e.Action == ControlKeyActionType.FillKeyToFilter) ? DxComponent.KeyConvertToChar(e.Keys, true)?.ToString() : (string)null);
            switch (filterMode)
            {
                case FilterRowMode.Client:
                    _RowFilterClientSetFocus(text);
                    break;
                case FilterRowMode.Server:
                    _RowFilterServerSetFocus(text);
                    __RowFilterServer.Focus();
                    break;
            }
        }
        /// <summary>
        /// Disposeřádkového filtrování
        /// </summary>
        private void _RowFilterDispose()
        {
            _RowFilterClientRemove();
            _RowFilterServerRemove();
            __RowFilterMode = FilterRowMode.None;
        }
        /// <summary>
        /// Aktivuje daný režim řádkového filtru
        /// </summary>
        /// <param name="filterMode"></param>
        private void _SetRowFilterMode(FilterRowMode filterMode)
        {
            this.RunInGui(() => _SetRowFilterModeGui(filterMode));
        }
        /// <summary>
        /// Aktivuje daný režim řádkového filtru, v GUI threadu
        /// </summary>
        /// <param name="newFilterMode"></param>
        private void _SetRowFilterModeGui(FilterRowMode newFilterMode)
        {
            var oldFilterMode = __RowFilterMode;

            if (newFilterMode != FilterRowMode.Client && _RowFilterClientExists)
                _RowFilterClientRemove();
            if (newFilterMode != FilterRowMode.Server && _RowFilterServerExists)
                _RowFilterServerRemove();

            if (newFilterMode == FilterRowMode.Client && !_RowFilterClientExists)
                _RowFilterClientPrepare();
            if (newFilterMode == FilterRowMode.Server && !_RowFilterServerExists)
                _RowFilterServerPrepare();

            switch (newFilterMode)
            {
                case FilterRowMode.Client:
                    if (!__RowFilterClient.IsSetVisible())
                        __RowFilterClient.Visible = true;
                    break;
                case FilterRowMode.Server:
                    if (!__RowFilterServer.IsSetVisible())
                        __RowFilterServer.Visible = true;
                    break;
            }

            if (newFilterMode != oldFilterMode)
            {
                __RowFilterMode = newFilterMode;
                DoLayout();
            }
        }
        /// <summary>
        /// Umístí FilterBox (pokud je Visible) do daného prostoru, a ten zmenší o velikost FilterBoxu
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _RowFilterLayout(ref Rectangle innerBounds)
        {
            var filterMode = __RowFilterMode;
            if (filterMode == FilterRowMode.Client && _RowFilterClientExists)
            {
                Rectangle filterBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, __RowFilterClient.Height);
                __RowFilterClient.Bounds = filterBounds;
                int y = __RowFilterClient.Bottom + 1;
                innerBounds = new Rectangle(innerBounds.X, y, innerBounds.Width, innerBounds.Bottom - y);
            }
            else if (filterMode == FilterRowMode.Server && _RowFilterServerExists)
            {
                Rectangle filterBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, __RowFilterServer.Height);
                __RowFilterServer.Bounds = filterBounds;
                int y = __RowFilterServer.Bottom + 1;
                innerBounds = new Rectangle(innerBounds.X, y, innerBounds.Width, innerBounds.Bottom - y);
            }
        }
        /// <summary>
        /// Varianty řádkového filtru
        /// </summary>
        public enum FilterRowMode
        {
            /// <summary>
            /// Není zobrazen, default
            /// </summary>
            None,
            /// <summary>
            /// Klientský: DevExpress
            /// </summary>
            Client,
            /// <summary>
            /// Serverový: události na server a reload nazpátek
            /// </summary>
            Server
        }
        /// <summary>
        /// Varianta řádkového filtru.
        /// </summary>
        private FilterRowMode __RowFilterMode;
        #region Klientský řádkový filtr = používá Searcher
        /// <summary>
        /// Aktuálně existuje řádkový filtr typu Client?
        /// </summary>
        private bool _RowFilterClientExists { get { return (__RowFilterClient != null); } }
        /// <summary>
        /// Inicializace klientského FilterBoxu, a jeho vložení do this.Controls
        /// </summary>
        private void _RowFilterClientPrepare()
        {
            __RowFilterClient = new SearchControl();
            __RowFilterClient.Client = __ListBox;
            __RowFilterClient.Properties.NullValuePrompt = DxComponent.Localize(MsgCode.DxFilterBoxNullValuePrompt);   // "Hledat"
            __RowFilterClient.TabStop = false;
            __RowFilterClient.Properties.ShowMRUButton = false;
            __RowFilterClient.AddingMRUItem += __RowFilterClient_AddingMRUItem;
            _RowFilterClientRegisterEvents();
            this.Controls.Add(__RowFilterClient);
        }
        /// <summary>
        /// DevExpress je ochoten evidovat MRU (Most Recent Unit), ale myslím že to spíš obtěžuje. Zakážu přidávání...
        /// My používáme šipku dolů (kurzor) na přechod do vlastního Listu, ale DevExpress na ní rozbaluje MRU menu, a pak se přes sebe pletou MRU položky a Itemy od Listu...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __RowFilterClient_AddingMRUItem(object sender, AddingMRUItemEventArgs e)
        {
            e.Cancel = true;
        }
        /// <summary>
        /// Aktivuje klientský RowFilter, volitelně do něj vepíše daný text (pokud není null)
        /// </summary>
        private void _RowFilterClientSetFocus(string text)
        {
            if (__RowFilterClient != null)
            {
                __RowFilterClient.Focus();
                if (text != null)
                {
                    __RowFilterClient.Text = text;
                    __RowFilterClient.SelectionStart = text.Length;
                }
            }
        }
        /// <summary>
        /// Odebere klientský RowFilter
        /// </summary>
        private void _RowFilterClientRemove()
        {
            if (__RowFilterClient != null)
            {
                _RowFilterClientUnRegisterEvents();
                __RowFilterClient.RemoveControlFromParent();
                __RowFilterClient.Dispose();
                __RowFilterClient = null;
            }
        }
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="__RowFilterClient"/>
        /// </summary>
        private void _RowFilterClientRegisterEvents()
        {
            var filter = __RowFilterClient;
            if (filter != null)
            {
                filter.PreviewKeyDown += _RowFilterClient_PreviewKeyDown;
            }
        }
        /// <summary>
        /// Odregistruje zdejší eventhandlery na události v nativním <see cref="__RowFilterClient"/>
        /// </summary>
        private void _RowFilterClientUnRegisterEvents()
        {
            var filter = __RowFilterClient;
            if (filter != null)
            {
                filter.PreviewKeyDown -= _RowFilterClient_PreviewKeyDown;
            }
        }
        /// <summary>
        /// Po stisku klávesy v řádkovém filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _RowFilterClient_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                e.IsInputKey = false;
                this.__ListBox.UnSelectAll();
                this.__ListBox.SelectedIndex = 0;
                this.__ListBox.Focus();
            }
        }
        /// <summary>
        /// Klientský RowFilter
        /// </summary>
        private DevExpress.XtraEditors.SearchControl __RowFilterClient;
        #endregion
        #region Serverový řádkový filtr = používá FilterBox
        /// <summary>
        /// Aktuálně existuje řádkový filtr typu Server?
        /// </summary>
        private bool _RowFilterServerExists { get { return (__RowFilterServer != null); } }
        /// <summary>
        /// Inicializace serverového FilterBoxu, a jeho vložení do this.Controls
        /// </summary>
        private void _RowFilterServerPrepare()
        {
            __RowFilterServer = new DxFilterBox() { Dock = DockStyle.None, Visible = false, TabIndex = 0 };
            __RowFilterServer.FilterOperators = DxFilterBox.CreateDefaultOperatorItems(FilterBoxOperatorItems.DefaultText);
            __RowFilterServer.FilterValueChangedSources = DxFilterBoxChangeEventSource.DefaultGreen;
            _RowFilterServerRegisterEvents();
            this.Controls.Add(__RowFilterServer);
        }
        /// <summary>
        /// Aktivuje klientský RowFilter, volitelně do něj vepíše daný text (pokud není null)
        /// </summary>
        private void _RowFilterServerSetFocus(string text)
        {
            if (__RowFilterServer != null)
            {
                __RowFilterServer.Focus();
                if (text != null)
                    __RowFilterServer.FilterText = text;
            }
        }
        /// <summary>
        /// Odebere serverový RowFilter
        /// </summary>
        private void _RowFilterServerRemove()
        {
            if (__RowFilterServer != null)
            {
                _RowFilterServerUnRegisterEvents();
                __RowFilterServer.RemoveControlFromParent();
                __RowFilterServer.Dispose();
                __RowFilterServer = null;
            }
        }
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="__RowFilterServer"/>
        /// </summary>
        private void _RowFilterServerRegisterEvents()
        {
            __RowFilterServer.FilterValueChanged += _RowFilterServer_Changed;      // Změna obsahu filtru a Enter
            __RowFilterServer.KeyEnterPress += _RowFilterServer_KeyEnter;
        }
        /// <summary>
        /// Odregistruje zdejší eventhandlery na události v nativním <see cref="__RowFilterServer"/>
        /// </summary>
        private void _RowFilterServerUnRegisterEvents()
        {
            __RowFilterServer.FilterValueChanged -= _RowFilterServer_Changed;      // Změna obsahu filtru a Enter
            __RowFilterServer.KeyEnterPress -= _RowFilterServer_KeyEnter;
        }
        /// <summary>
        /// Instance serverového řádkového filtru
        /// </summary>
        public DxFilterBox RowFilterServer { get { return __RowFilterServer; } }
        /// <summary>
        /// Aktuální text v řádkovém filtru
        /// </summary>
        public DxFilterBoxValue RowFilterServerText { get { return __RowFilterServer?.FilterValue; } set { if (_RowFilterServerExists) __RowFilterServer.FilterValue = value; } }
        /// <summary>
        /// Pole operátorů nabízených pod tlačítkem vlevo.
        /// Pokud bude vloženo null nebo prázdné pole, pak tlačítko vlevo nebude zobrazeno vůbec, a v hodnotě FilterValue bude Operator = null.
        /// </summary>
        public List<IMenuItem> RowFilterServerOperators { get { return __RowFilterServer?.FilterOperators; } set { if (_RowFilterServerExists) __RowFilterServer.FilterOperators = value; } }
        /// <summary>
        /// Za jakých událostí se volá event <see cref="RowFilterServerChanged"/>
        /// </summary>
        public DxFilterBoxChangeEventSource? RowFilterServerChangedSources { get { return __RowFilterServer?.FilterValueChangedSources; } set { if (_RowFilterServerExists) __RowFilterServer.FilterValueChangedSources = value ?? DxFilterBoxChangeEventSource.DefaultGreen; } }
        /// <summary>
        /// Událost volaná po hlídané změně obsahu filtru.
        /// Argument obsahuje hodnotu filtru a druh události, která vyvolala event.
        /// Druhy události, pro které se tento event volá, lze nastavit v <see cref="RowFilterServerChangedSources"/>.
        /// </summary>
        public event EventHandler<DxFilterBoxChangeArgs> RowFilterServerChanged;
        /// <summary>
        /// Provede se po stisku Enter v řádkovém filtru (i bez změny textu), vhodné pro řízení Focusu
        /// </summary>
        public event EventHandler RowFilterServerKeyEnter;
        
        /// <summary>
        /// Po jakékoli změně v řádkovém filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _RowFilterServer_Changed(object sender, DxFilterBoxChangeArgs args)
        {
            OnFilterBoxChanged(args);
            RowFilterServerChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po jakékoli změně v řádkovém filtru
        /// </summary>
        protected virtual void OnFilterBoxChanged(DxFilterBoxChangeArgs args) { }
        /// <summary>
        /// Po stisku Enter v řádkovém filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RowFilterServer_KeyEnter(object sender, EventArgs e)
        {
            _MainControlFocus();
            OnFilterBoxKeyEnter();
            RowFilterServerKeyEnter?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po stisku Enter v řádkovém filtru
        /// </summary>
        protected virtual void OnFilterBoxKeyEnter() { }
        /// <summary>
        /// DxFilterBox
        /// </summary>
        private DxFilterBox __RowFilterServer;
        #endregion
        #endregion
        #region Tlačítka
        /// <summary>
        /// Umístí tlačítka podle potřeby do daného vnitřního prostoru, ten zmenší o prostor zabraný tlačítky
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _ButtonsLayout(ref Rectangle innerBounds)
        {
            var layoutInfos = _GetButtonsInfo();
            if (layoutInfos != null)
            {
                Padding margins = DxComponent.GetDefaultInnerMargins(this.CurrentDpi);
                Size spacing = DxComponent.GetDefaultInnerSpacing(this.CurrentDpi);
                innerBounds = DxComponent.CalculateControlItemsLayout(innerBounds, layoutInfos, this.ButtonsPosition, margins, spacing);
            }
        }
        /// <summary>
        /// Načte a vrátí informace pro tvorbu layoutu buttonů
        /// </summary>
        /// <returns></returns>
        private ControlItemLayoutInfo[] _GetButtonsInfo()
        {
            var buttons = __Buttons;
            if (buttons == null || buttons.Count == 0) return null;

            Size buttonSize = DxComponent.GetImageSize(__ButtonsSize, true, this.CurrentDpi).Add(4, 4);
            Size spaceSize = new Size(buttonSize.Width / 8, buttonSize.Height / 8);
            List<ControlItemLayoutInfo> layoutInfos = new List<ControlItemLayoutInfo>();
            ControlKeyActionType group1 = ControlKeyActionType.MoveTop | ControlKeyActionType.MoveUp | ControlKeyActionType.MoveDown | ControlKeyActionType.MoveBottom;
            ControlKeyActionType group2 = ControlKeyActionType.Refresh | ControlKeyActionType.SelectAll | ControlKeyActionType.Delete;
            ControlKeyActionType group3 = ControlKeyActionType.ClipCopy | ControlKeyActionType.ClipCut | ControlKeyActionType.ClipPaste;
            int currentGroup = 0;
            for (int b = 0; b < buttons.Count; b++)
            {
                var button = buttons[b];

                // Zkusíme oddělit jednotlivé grupy od sebe:
                ControlKeyActionType buttonType = ((button.Tag is ControlKeyActionType) ? ((ControlKeyActionType)button.Tag) : ControlKeyActionType.None);
                int buttonGroup = (((buttonType & group1) != 0) ? 1 :
                                  (((buttonType & group2) != 0) ? 2 :
                                  (((buttonType & group3) != 0) ? 3 : 0)));
                if (currentGroup != 0 && buttonGroup != currentGroup)
                    // Změna grupy = vložíme před nynější button menší mezírku:
                    layoutInfos.Add(new ControlItemLayoutInfo() { Size = spaceSize });
               
                // Přidám button:
                layoutInfos.Add(new ControlItemLayoutInfo() { Control = buttons[b], Size = buttonSize });
                currentGroup = buttonGroup;
            }

            return layoutInfos.ToArray();
        }
        /// <summary>
        /// Aktuální povolená tlačítka promítne do panelu jako viditelná tlačítka, a i do ListBoxu jako povolené klávesové akce
        /// </summary>
        private void _AcceptButtonsType()
        {
            ControlKeyActionType validButtonsTypes = __ButtonsTypes;

            // Buttony z _ButtonsType převedu na povolené akce v ListBoxu a sloučím s akcemi dosud povolenými:
            ControlKeyActionType oldActions = __ListBox.EnabledKeyActions;
            ControlKeyActionType newActions = ConvertButtonsToActions(validButtonsTypes);
            __ListBox.EnabledKeyActions = (newActions | oldActions);

            // Odstraním stávající buttony:
            _RemoveButtons(true);

            // Vytvořím potřebné buttony:
            //   (vytvoří se jen ty buttony, které jsou vyžádané proměnné buttonsTypes, fyzické pořadí buttonů je dané pořadím těchto řádků)
            _AcceptButtonType(ControlKeyActionType.MoveTop, validButtonsTypes, "@arrowsmall|top|blue", MsgCode.DxKeyActionMoveTopTitle, MsgCode.DxKeyActionMoveTopText);
            _AcceptButtonType(ControlKeyActionType.MoveUp, validButtonsTypes, "@arrowsmall|up|blue", MsgCode.DxKeyActionMoveUpTitle, MsgCode.DxKeyActionMoveUpText);
            _AcceptButtonType(ControlKeyActionType.MoveDown, validButtonsTypes, "@arrowsmall|down|blue", MsgCode.DxKeyActionMoveDownTitle, MsgCode.DxKeyActionMoveDownText);
            _AcceptButtonType(ControlKeyActionType.MoveBottom, validButtonsTypes, "@arrowsmall|bottom|blue", MsgCode.DxKeyActionMoveBottomTitle, MsgCode.DxKeyActionMoveBottomText);
            _AcceptButtonType(ControlKeyActionType.Refresh, validButtonsTypes, "devav/actions/refresh.svg", MsgCode.DxKeyActionRefreshTitle, MsgCode.DxKeyActionRefreshText);   // qqq
            _AcceptButtonType(ControlKeyActionType.SelectAll, validButtonsTypes, "@editsmall|all|blue", MsgCode.DxKeyActionSelectAllTitle, MsgCode.DxKeyActionSelectAllText);
            _AcceptButtonType(ControlKeyActionType.Delete, validButtonsTypes, "@editsmall|del|red", MsgCode.DxKeyActionDeleteTitle, MsgCode.DxKeyActionDeleteText);       // "devav/actions/delete.svg"
            _AcceptButtonType(ControlKeyActionType.ClipCopy, validButtonsTypes, "devav/actions/copy.svg", MsgCode.DxKeyActionClipCopyTitle, MsgCode.DxKeyActionClipCopyText);
            _AcceptButtonType(ControlKeyActionType.ClipCut, validButtonsTypes, "devav/actions/cut.svg", MsgCode.DxKeyActionClipCutTitle, MsgCode.DxKeyActionClipCutText);
            _AcceptButtonType(ControlKeyActionType.ClipPaste, validButtonsTypes, "devav/actions/paste.svg", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);

            _AcceptButtonType(ControlKeyActionType.CopyToRightOne, validButtonsTypes, "@arrowsmall|right|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ControlKeyActionType.CopyToRightAll, validButtonsTypes, "@arrow|right|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ControlKeyActionType.CopyToLeftOne, validButtonsTypes, "@arrowsmall|left|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ControlKeyActionType.CopyToLeftAll, validButtonsTypes, "@arrow|left|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);

            _AcceptButtonType(ControlKeyActionType.Undo, validButtonsTypes, "svgimages/dashboards/undo.svg", MsgCode.DxKeyActionUndoTitle, MsgCode.DxKeyActionUndoText);
            _AcceptButtonType(ControlKeyActionType.Redo, validButtonsTypes, "svgimages/dashboards/redo.svg", MsgCode.DxKeyActionRedoTitle, MsgCode.DxKeyActionRedoText);

            // Pokud bylo povoleno UndoRedo, pak povolím i odpovídající funkcionalitu:
            if ((newActions.HasFlag(ControlKeyActionType.Undo) || newActions.HasFlag(ControlKeyActionType.Redo)) || !this.UndoRedoEnabled)
                this.UndoRedoEnabled = true;

            _SetButtonsEnabled();
        }
        /// <summary>
        /// Metoda vytvoří Button, pokud má být vytvořen. Tedy pokud typ buttonu v <paramref name="buttonType"/> bude přítomen v povolených buttonech v <paramref name="validButtonsTypes"/>.
        /// Pak vygeneruje odpovídající button a přidá jej do pole <see cref="__Buttons"/>.
        /// </summary>
        /// <param name="buttonType">Typ konkrétního jednoho buttonu</param>
        /// <param name="validButtonsTypes">Soupis požadovaných buttonů</param>
        /// <param name="imageName"></param>
        /// <param name="msgToolTipTitle"></param>
        /// <param name="msgToolTipText"></param>
        private void _AcceptButtonType(ControlKeyActionType buttonType, ControlKeyActionType validButtonsTypes, string imageName, MsgCode msgToolTipTitle, MsgCode msgToolTipText)
        {
            if (!validButtonsTypes.HasFlag(buttonType)) return;

            string toolTipTitle = DxComponent.Localize(msgToolTipTitle);
            string toolTipText = DxComponent.Localize(msgToolTipText);
            DxSimpleButton dxButton = DxComponent.CreateDxMiniButton(0, 0, 24, 24, this, this._ButtonClick, resourceName: imageName, toolTipTitle: toolTipTitle, toolTipText: toolTipText, tabStop: false, allowFocus: false, tag: buttonType);
            __Buttons.Add(dxButton);
        }
        /// <summary>
        /// Odebere všechny buttony přítomné v poli <see cref="__Buttons"/>
        /// </summary>
        private void _RemoveButtons(bool createEmptyList = false)
        {
            if (__Buttons != null && __Buttons.Count > 0)
            {
                foreach (var button in __Buttons)
                {
                    button.RemoveControlFromParent();
                    button.Dispose();
                }
                __Buttons.Clear();
            }
            if (__Buttons is null && createEmptyList)
                __Buttons = new List<DxSimpleButton>();
        }
        /// <summary>
        /// Promítne stav <see cref="UndoRedoController.UndoEnabled"/> a <see cref="UndoRedoController.RedoEnabled"/> 
        /// z controlleru <see cref="UndoRedoController"/> do buttonů.
        /// </summary>
        private void _ListBox_UndoRedoEnabledChanged(object sender, EventArgs e)
        {
            _SetButtonsEnabledUndoRedo();
        }
        /// <summary>
        /// Po změně Selected prvků reaguje Enabled buttonů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedItemsChanged(object sender, EventArgs e)
        {
            _SetButtonsEnabledSelection();
            _RunSelectedItemsChanged();
        }
        /// <summary>
        /// Nastaví Enabled buttonů
        /// </summary>
        private void _SetButtonsEnabled()
        {
            _SetButtonsEnabledUndoRedo();
            _SetButtonsEnabledSelection();
        }
        /// <summary>
        /// Nastaví Enabled buttonů typu UndoRedo
        /// </summary>
        private void _SetButtonsEnabledUndoRedo()
        {
            bool undoRedoEnabled = this.UndoRedoEnabled;
            _SetButtonEnabled(ControlKeyActionType.Undo, (undoRedoEnabled && this.UndoRedoController.UndoEnabled));
            _SetButtonEnabled(ControlKeyActionType.Redo, (undoRedoEnabled && this.UndoRedoController.RedoEnabled));
        }
        /// <summary>
        /// Nastaví Enabled buttonů typu OnSelected
        /// </summary>
        private void _SetButtonsEnabledSelection()
        {
            int selectedCount = this.__ListBox.SelectedIndices.Count;
            int totalCount = this.__ListBox.ItemCount;

            bool isAnySelected = selectedCount > 0;
            _SetButtonEnabled(ControlKeyActionType.ClipCopy, isAnySelected);
            _SetButtonEnabled(ControlKeyActionType.ClipCut, isAnySelected);
            _SetButtonEnabled(ControlKeyActionType.Delete, isAnySelected);

            bool canMove = (selectedCount > 0 && selectedCount < totalCount);
            _SetButtonEnabled(ControlKeyActionType.MoveTop, canMove);
            _SetButtonEnabled(ControlKeyActionType.MoveUp, canMove);
            _SetButtonEnabled(ControlKeyActionType.MoveDown, canMove);
            _SetButtonEnabled(ControlKeyActionType.MoveBottom, canMove);

            bool canSelectAll = (totalCount > 0 && selectedCount < totalCount);
            _SetButtonEnabled(ControlKeyActionType.SelectAll, canSelectAll);
        }
        /// <summary>
        /// Nastaví do daného buttonu stav enabled
        /// </summary>
        /// <param name="buttonType"></param>
        /// <param name="enabled"></param>
        private void _SetButtonEnabled(ControlKeyActionType buttonType, bool enabled)
        {
            if (__Buttons.TryGetFirst(b => b.Tag is ControlKeyActionType bt && bt == buttonType, out var button))
                button.Enabled = enabled;
        }
        /// <summary>
        /// Provede akci danou buttonem <paramref name="sender"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ButtonClick(object sender, EventArgs args)
        {
            if (sender is DxSimpleButton dxButton && dxButton.Tag is ControlKeyActionType buttonType)
            {
                ControlKeyActionType action = ConvertButtonsToActions(buttonType);
                __ListBox.DoKeyActions(action);
            }
        }
        /// <summary>
        /// Konvertuje hodnoty z typu <see cref="ControlKeyActionType"/> na hodnoty typu <see cref="ControlKeyActionType"/>
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public static ControlKeyActionType ConvertButtonsToActions(ControlKeyActionType buttons)
        {
            ControlKeyActionType actions =
                (buttons.HasFlag(ControlKeyActionType.ClipCopy) ? ControlKeyActionType.ClipCopy : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.ClipCut) ? ControlKeyActionType.ClipCut : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.ClipPaste) ? ControlKeyActionType.ClipPaste : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.Delete) ? ControlKeyActionType.Delete : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.Refresh) ? ControlKeyActionType.Refresh: ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.SelectAll) ? ControlKeyActionType.SelectAll : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.GoBegin) ? ControlKeyActionType.GoBegin : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.GoEnd) ? ControlKeyActionType.GoEnd : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.MoveTop) ? ControlKeyActionType.MoveTop : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.MoveUp) ? ControlKeyActionType.MoveUp : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.MoveDown) ? ControlKeyActionType.MoveDown : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.MoveBottom) ? ControlKeyActionType.MoveBottom : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.CopyToRightOne) ? ControlKeyActionType.CopyToRightOne : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.CopyToRightAll) ? ControlKeyActionType.CopyToRightAll : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.CopyToLeftOne) ? ControlKeyActionType.CopyToLeftOne : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.CopyToLeftAll) ? ControlKeyActionType.CopyToLeftAll : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.Undo) ? ControlKeyActionType.Undo : ControlKeyActionType.None) |
                (buttons.HasFlag(ControlKeyActionType.Redo) ? ControlKeyActionType.Redo : ControlKeyActionType.None);
            return actions;
        }
        /// <summary>
        /// Konvertuje hodnoty z typu <see cref="ControlKeyActionType"/> na hodnoty typu <see cref="ControlKeyActionType"/>
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static ControlKeyActionType ConvertActionsToButtons(ControlKeyActionType actions)
        {
            ControlKeyActionType buttons =
                (actions.HasFlag(ControlKeyActionType.ClipCopy) ? ControlKeyActionType.ClipCopy : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.ClipCut) ? ControlKeyActionType.ClipCut : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.ClipPaste) ? ControlKeyActionType.ClipPaste : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.Delete) ? ControlKeyActionType.Delete : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.Refresh) ? ControlKeyActionType.Refresh : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.SelectAll) ? ControlKeyActionType.SelectAll : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.GoBegin) ? ControlKeyActionType.GoBegin : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.GoEnd) ? ControlKeyActionType.GoEnd : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.MoveTop) ? ControlKeyActionType.MoveTop : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.MoveUp) ? ControlKeyActionType.MoveUp : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.MoveDown) ? ControlKeyActionType.MoveDown : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.MoveBottom) ? ControlKeyActionType.MoveBottom : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.Undo) ? ControlKeyActionType.Undo : ControlKeyActionType.None) |
                (actions.HasFlag(ControlKeyActionType.Redo) ? ControlKeyActionType.Redo : ControlKeyActionType.None);
            return buttons;
        }
        /// <summary>
        /// Typy dostupných tlačítek
        /// </summary>
        private ControlKeyActionType __ButtonsTypes;
        /// <summary>
        /// Umístění tlačítek
        /// </summary>
        private ToolbarPosition __ButtonsPosition;
        /// <summary>
        /// Velikost tlačítek
        /// </summary>
        private ResourceImageSizeType __ButtonsSize;
        /// <summary>
        /// Tlačítka, která mají být dostupná, v patřičném pořadí
        /// </summary>
        private List<DxSimpleButton> __Buttons;
        #endregion
        #region UndoRedo manager (přístup do vnitřního Listu)
        /// <summary>
        /// UndoRedoEnabled List má povoleny akce Undo a Redo?
        /// </summary>
        public bool UndoRedoEnabled { get { return __ListBox.UndoRedoEnabled; } set { __ListBox.UndoRedoEnabled = value; } }
        /// <summary>
        /// Controller UndoRedo.
        /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
        /// Pokud je povoleno, je zde vždy instance. 
        /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
        /// </summary>
        public UndoRedoController UndoRedoController { get { return __ListBox.UndoRedoController; } set { __ListBox.UndoRedoController = value; } }
        #endregion
    }
    /// <summary>
    /// ListBoxControl s podporou pro drag and drop a reorder
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ImageListBoxControl, IDxDragDropControl, IUndoRedoControl, IDxToolTipDynamicClient   // původně: ListBoxControl, nyní: https://docs.devexpress.com/WindowsForms/DevExpress.XtraEditors.ImageListBoxControl
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            _ItemClickInit();
            _KeyActionsInit();
            _DataExchangeInit();
            _DxDragDropInit(DxDragDropActionType.None);
            _ToolTipInit();
            OnImageInit();
            DuplicityEnabled = true;
            ItemSizeType = ResourceImageSizeType.Small;
            this.Items.ListChanged += _ListItemsChanged;
            DrawItem += _DrawItem;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        /// <summary>
        /// Dispose Listu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                _DxDragDropDispose();
                _ToolTipDispose();
                _DxTemplateDispose();
            }
            catch { /* Chyby v Dispose občas nastanou v DevExpress, který něco likviduje v GC threadu a nemá přístup do GUI. */ }
        }
        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
        /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
        /// Hodnota vyšší než 10 se akceptuje jako 10.
        /// </summary>
        public int ItemHeightPadding
        {
            get { return _ItemHeightPadding; }
            set
            {
                if (value > 0)
                {
                    int padding = (value > 10 ? 10 : value);
                    int fontheight = this.Appearance.GetFont().Height;
                    this.ItemAutoHeight = false;
                    this.ItemHeight = fontheight + (2 * padding);
                    _ItemHeightPadding = padding;
                }
                else
                {
                    this.ItemAutoHeight = true;
                    _ItemHeightPadding = 0;
                }
            }
        }
        private int _ItemHeightPadding = 0;
        #endregion
        #region Data = položky, a layout = Template
        /// <summary>
        /// Režim prvků v ListBoxu.
        /// </summary>
        public ListBoxItemsMode ItemsMode { get { return __ItemsMode; } }
        #region Jednoduchý List postavený nad položkami IMenuItem
        /// <summary>
        /// Prvky Listu typované na <see cref="IMenuItem"/>.
        /// Pokud v Listu budou obsaženy jiné prvky než <see cref="IMenuItem"/>, pak na jejich místě v tomto poli bude null.
        /// Toto pole má stejný počet prvků jako pole this.Items
        /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
        /// </summary>
        public IMenuItem[] MenuItems
        {
            get
            {
                return (__ItemsMode == ListBoxItemsMode.MenuItems ? this.Items.Select(i => i.Value as IMenuItem).ToArray() : null);
            }
            set
            {
                this.DataSource = null;

                if (value != null)
                {
                    var validItems = _GetOnlyValidItems(value, false);
                    this.Items.Clear();
                    this.Items.AddRange(validItems);

                    __ItemsMode = ListBoxItemsMode.MenuItems;
                }
                else
                {
                    __ItemsMode = ListBoxItemsMode.None;
                }
            }
        }
        /// <summary>
        /// Po změně v prvcích Items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ListItemsChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            // Pokud aktuálně nejsme v režimu MenuItems, a přitom máme položky v poli this.Items, pak se do režimu MenuItems přepneme nyní:
            //  Někdo asi vložil položky přímo do nativního soupisu...
            if (__ItemsMode != ListBoxItemsMode.MenuItems && this.Items.Count > 0)
            {
                __ItemsMode = ListBoxItemsMode.MenuItems;
                if (__DataTable != null) __DataTable = null;
            }
            _RunItemsListChanged(e);
        }
        /// <summary>
        /// Zavolá akce po změně v poli <see cref="MenuItems"/>
        /// </summary>
        /// <param name="e"></param>
        private void _RunItemsListChanged(System.ComponentModel.ListChangedEventArgs e)
        {
            OnListItemsChanged(e);
            ListItemsChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po změně v poli <see cref="MenuItems"/>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListItemsChanged(System.ComponentModel.ListChangedEventArgs e) { }
        /// <summary>
        /// Proběhne po změně v poli <see cref="MenuItems"/>
        /// </summary>
        public event System.ComponentModel.ListChangedEventHandler ListItemsChanged;
        /// <summary>
        /// Aktuálně vybraný prvek typu <see cref="IMenuItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
        /// </summary>
        public IMenuItem SelectedMenuItem
        {
            get
            {   // Vrátím IMenuItem nalezený v aktuálně vybraném prvku:
                return ((__ItemsMode == ListBoxItemsMode.MenuItems && this.Items.Count > 0 && _TryFindMenuItem(this.SelectedItem, out var menuItem)) ? menuItem : null);
            }
            set
            {   // Najdu první prvek zdejšího pole, který v sobě obsahuje IMenuItem, který je identický s dodanou value:
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    object selectedItem = null;
                    if (this.Items.Count > 0 && value != null)
                        selectedItem = this.Items.FirstOrDefault(i => (_TryFindMenuItem(i, out var iMenuItem) && Object.ReferenceEquals(iMenuItem, value)));
                    this.SelectedItem = selectedItem;
                }
            }
        }
        /// <summary>
        /// Metoda zkusí najít a vrátit <see cref="IMenuItem"/> z dodaného prvku ListBoxu
        /// </summary>
        /// <param name="item"></param>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private bool _TryFindMenuItem(object item, out IMenuItem menuItem)
        {
            if (__ItemsMode == ListBoxItemsMode.MenuItems && item != null)
            {
                if (item is DevExpress.XtraEditors.Controls.ImageListBoxItem listItem && listItem.Value is IMenuItem menuItem1)
                {
                    menuItem = menuItem1;
                    return true;
                }
                if (item is IMenuItem menuItem2)
                {
                    menuItem = menuItem2;
                    return true;
                }
            }
            menuItem = null;
            return false;
        }
        /// <summary>
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle>[] VisibleMenuItemsExt
        {
            get
            {
                List<Tuple<int, IMenuItem, Rectangle>> visibleMenuItems = null;
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    visibleMenuItems = new List<Tuple<int, IMenuItem, Rectangle>>();
                    var listItems = this.MenuItems;
                    int topIndex = this.TopIndex;
                    int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                    int count = this.ItemCount;
                    while (index < count)
                    {
                        Rectangle? bounds = GetItemBounds(index);
                        if (bounds.HasValue)
                            visibleMenuItems.Add(new Tuple<int, IMenuItem, Rectangle>(index, listItems[index], bounds.Value));
                        else if (index > topIndex)
                            break;
                        index++;
                    }
                }
                return visibleMenuItems.ToArray();
            }
        }
        /// <summary>
        /// Pole, obsahující informace o právě selectovaných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle?>[] SelectedMenuItemsExt
        {
            get
            {
                List<Tuple<int, IMenuItem, Rectangle?>> selectedMenuItems = null;
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    var listItems = this.MenuItems;
                    selectedMenuItems = new List<Tuple<int, IMenuItem, Rectangle?>>();
                    foreach (var index in this.SelectedIndices)
                    {
                        Rectangle? bounds = GetItemBounds(index);
                        selectedMenuItems.Add(new Tuple<int, IMenuItem, Rectangle?>(index, listItems[index], bounds));
                    }
                }
                return selectedMenuItems.ToArray();
            }
        }
        /// <summary>
        /// Obsahuje pole prvků, které jsou aktuálně Selected. 
        /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
        /// </summary>
        public IEnumerable<IMenuItem> SelectedMenuItems
        {
            get
            {
                var listItems = this.MenuItems;
                var selectedItems = new List<IMenuItem>();
                foreach (var index in this.SelectedIndices)
                    selectedItems.Add(listItems[index]);
                return selectedItems.ToArray();
            }
            set
            {
                var selectedItems = (value?.ToList() ?? new List<IMenuItem>());
                var listItems = this.MenuItems;
                int count = this.ItemCount;
                for (int i = 0; i < count; i++)
                {
                    object item = listItems[i];
                    bool isSelected = selectedItems.Any(s => Object.ReferenceEquals(s, item));
                    this.SetSelected(i, isSelected);
                }
            }
        }
        /// <summary>
        /// Obsahuje pole indexů prvků, které jsou aktuálně Selected. 
        /// Lze setovat. Setování nastaví stav Selected na určených prvcích this.Items. Ostatní budou not selected.
        /// </summary>
        public IEnumerable<int> SelectedIndexes
        {
            get
            {
                return this.SelectedIndices.ToArray();
            }
            set
            {
                int count = this.ItemCount;
                Dictionary<int, int> indexes = value.CreateDictionary(i => i, true);
                for (int i = 0; i < count; i++)
                {
                    bool isSelected = indexes.ContainsKey(i);
                    this.SetSelected(i, isSelected);
                }
            }
        }
        #endregion
        #region Komplexní List postavený nad DataTable a Template
        /// <summary>
        /// Tabulka s daty
        /// </summary>
        public System.Data.DataTable DataTable
        {
            get { return (__ItemsMode == ListBoxItemsMode.Table ? __DataTable : null); }
            set
            {
                this.DataSource = null;

                __DataTable = value;
                if (value != null)
                {
                    this.DataSource = value;
                    __ItemsMode = ListBoxItemsMode.Table;
                    _ReloadDxTemplate();
                }
                else
                {
                    __ItemsMode = ListBoxItemsMode.None;
                }
            }
        }
        private System.Data.DataTable __DataTable;
        /// <summary>
        /// Šablona pro zobrazení dat z <see cref="DataTable"/>
        /// </summary>
        public DxListBoxTemplate DxTemplate
        {
            get { return __DxTemplate; }
            set
            {
                __DxTemplate = value;
                _ReloadDxTemplate();
            }
        }
        private DxListBoxTemplate __DxTemplate;
        /// <summary>
        /// Aplikuje šablonu <see cref="DxTemplate"/> do this Listu
        /// </summary>
        private void _ReloadDxTemplate()
        {
            var dxTemplate = __DxTemplate;
            if (dxTemplate != null && __ItemsMode == ListBoxItemsMode.Table)
                dxTemplate.ApplyTemplateToList(this);
        }
        /// <summary>
        /// Dispose šablony
        /// </summary>
        private void _DxTemplateDispose()
        {
            var dxTemplate = __DxTemplate;
            if (dxTemplate != null)
                dxTemplate.Dispose();
        }
        /// <summary>
        /// Událost je volána 1x per 1 řádek Listu v procesu jeho kreslení, jako příprava, v režimu Table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBoxCustomizeItemTable(object sender, DevExpress.XtraEditors.CustomizeTemplatedItemEventArgs e)
        {
            var dxTemplate = this.DxTemplate;
            if (dxTemplate != null && e.Value is System.Data.DataRowView rowView)
                dxTemplate.ApplyRowDataToTemplateItem(rowView.Row, e.TemplatedItem);
        }
        /// <summary>
        /// Metoda vytvoří Simple template pro ikonu a pro text
        /// </summary>
        /// <param name="columnNameItemId"></param>
        /// <param name="columnNameIcon"></param>
        /// <param name="columnNameText"></param>
        /// <param name="columnNameToolTip"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public DxListBoxTemplate CreateSimpleDxTemplate(string columnNameItemId, string columnNameIcon, string columnNameText, string columnNameToolTip = null, int? iconSize = null)
        { return DxListBoxTemplate.CreateSimpleDxTemplate(this.DataTable, columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize); }
        #endregion
        /// <summary>
        /// Aktuální režim položek
        /// </summary>
        private ListBoxItemsMode __ItemsMode;
        #endregion
        #region Rozšířené property
        /// <summary>
        /// Obsahuje true u controlu, který sám by byl Visible, i když aktuálně je na Invisible parentu.
        /// <para/>
        /// Vrátí true, pokud control sám na sobě má nastavenou hodnotu <see cref="Control.Visible"/> = true.
        /// Hodnota <see cref="Control.Visible"/> běžně obsahuje součin všech hodnot <see cref="Control.Visible"/> od controlu přes všechny jeho parenty,
        /// kdežto tato vlastnost <see cref="VisibleInternal"/> vrací hodnotu pouze z tohoto controlu.
        /// Například každý control před tím, než je zobrazen jeho formulář, má <see cref="Control.Visible"/> = false, ale tato metoda vrací hodnotu reálně vloženou do <see cref="Control.Visible"/>.
        /// </summary>
        public bool VisibleInternal { get { return this.IsSetVisible(); } set { this.Visible = value; } }
        /// <summary>
        /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
        /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
        /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
        /// </summary>
        public bool DuplicityEnabled { get; set; }
        #endregion
        #region Overrides
        /// <summary>
        /// Při příchodu focusu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
        }
        /// <summary>
        /// Při odchodu focusu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
        }
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            __ItemImageSize = null;
            base.OnPaint(e);
            this.RunPaintList(e);
            this.MouseDragPaint(e);
        }
        /// <summary>
        /// Je voláno před vykreslením každého prvku. Může upravit jeho vzhled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DrawItem(object sender, ListBoxDrawItemEventArgs e)
        {
            if (__ItemsMode == ListBoxItemsMode.MenuItems && e.Item is IMenuItem iMenuItem && iMenuItem.FontStyle.HasValue)
            {
                e.Appearance.FontStyleDelta = iMenuItem.FontStyle.Value;
                e.Appearance.Options.UseTextOptions = true;
            }
        }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseItemIndex = -1;
        }
        /// <summary>
        /// Po změně vybraných prvků
        /// </summary>
        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();
            if (this.IsRealSelectionChanged(true))
            {
                this._ToolTipHide();
                this._RunSelectionChanged();
            }
        }
        /// <summary>
        /// Vrátí true, pokud aktuální stav <see cref="SelectedItems"/> ji jiný než posledně známý <see cref="__LastSelectedItems"/>.
        /// Volitelně (podle <paramref name="acceptCurrentState"/>) může aktuální stav (obsah pole <see cref="SelectedItems"/>) uložit do <see cref="__LastSelectedItems"/>.
        /// </summary>
        /// <param name="acceptCurrentState"></param>
        /// <returns></returns>
        protected bool IsRealSelectionChanged(bool acceptCurrentState = false)
        {
            var currSelectedItems = this.SelectedItems;
            var lastSelectedItems = this.__LastSelectedItems;
            int currCount = currSelectedItems?.Length ?? -1;
            int lastCount = lastSelectedItems?.Length ?? -1;
            bool isChanged = (currCount != lastCount);
            if (!isChanged && currCount > 0)
            {
                for (int i = 0; i < currCount; i++)
                {
                    if (!Object.Equals(currSelectedItems[i], lastSelectedItems[i]))
                    {
                        isChanged = true;
                        break;
                    }
                }
            }
            if (acceptCurrentState) __LastSelectedItems = currSelectedItems;
            return isChanged;
        }
        /// <summary>
        /// Posledně zapamatovaný stav <see cref="SelectedItems"/>
        /// </summary>
        private object[] __LastSelectedItems;
        #endregion
        #region Images
        /// <summary>
        /// Inicializace pro Images
        /// </summary>
        protected virtual void OnImageInit()
        {
            this.MeasureItem += _MeasureItem;
            this.CustomizeItem += _ListBoxCustomizeItem;                        // Aktualizuje Image pro buňku = pro TemplateItem
            this.__ItemImageSize = null;
        }
        /// <summary>
        /// Při kreslení pozadí ...
        /// </summary>
        /// <param name="pevent"></param>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }
        /// <summary>
        /// Vrátí Image pro daný index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Image GetItemImage(int index)
        {
            if (index >= 0 && __ItemsMode == ListBoxItemsMode.MenuItems)
            {
                var listItems = this.MenuItems;
                if (listItems != null && index < listItems.Length)
                {
                    var menuItem = listItems[index];
                    if (menuItem != null)
                    {
                        Size itemSize = _ItemImageSize;

                        //var skinProvider = DevExpress.LookAndFeel.UserLookAndFeel.Default;
                        //var svgState = DevExpress.Utils.Drawing.ObjectState.Normal;
                        //var svgPalette = DevExpress.Utils.Svg.SvgPaletteHelper.GetSvgPalette(skinProvider, svgState);

                        if (menuItem.Image != null) return menuItem.Image;
                        if (menuItem.SvgImage != null) return DxComponent.RenderSvgImage(menuItem.SvgImage, itemSize, null);
                        if (menuItem.ImageName != null) return DxComponent.GetBitmapImage(menuItem.ImageName, this.ItemSizeType, itemSize);
                    }
                }
            }
            return base.GetItemImage(index);
        }
        /// <summary>
        /// Vrátí ImageSize pro daný index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Size GetItemImageSize(int index)
        {
            return _ItemImageSize;
        }
        /// <summary>
        /// Určí výšku prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MeasureItem(object sender, MeasureItemEventArgs e)
        {
            //var items = this.ListItems;
            //if (e.Index >= 0 && e.Index < items.Length)
            //{
            //    var menuItem = this.ListItems[e.Index];
            //}
        }
        /// <summary>
        /// Událost je volána 1x per 1 řádek Listu v procesu jeho kreslení, jako příprava
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBoxCustomizeItem(object sender, DevExpress.XtraEditors.CustomizeTemplatedItemEventArgs e)
        {
            switch (__ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    break;
                case ListBoxItemsMode.Table:
                    _ListBoxCustomizeItemTable(sender, e);
                    break;

            }
        }
        /// <summary>
        /// Velikost ikon
        /// </summary>
        public ResourceImageSizeType ItemSizeType
        {
            get { return __ItemSizeType; }
            set
            {
                __ItemSizeType = value;
                __ItemImageSize = null;
                if (this.Parent != null) this.Invalidate();
            }
        }
        /// <summary>
        /// Velikost ikon
        /// </summary>
        private ResourceImageSizeType __ItemSizeType = ResourceImageSizeType.Small;
        /// <summary>
        /// Velikost ikony, vychází z <see cref="ItemSizeType"/> a aktuálního DPI.
        /// </summary>
        private Size _ItemImageSize
        {
            get
            {
                if (!__ItemImageSize.HasValue)
                    __ItemImageSize = DxComponent.GetImageSize(this.ItemSizeType, true, this.DeviceDpi);
                return __ItemImageSize.Value;
            }
        }
        /// <summary>
        /// Velikost ikon, null = je nutno spočítat
        /// </summary>
        private Size? __ItemImageSize;
        #endregion
        #region ItemClick + KeyDown.Enter
        /// <summary>
        /// Inicializace eventů pro Click a MouseClick
        /// </summary>
        private void _ItemClickInit()
        {
            this.MouseClick += _MouseClick;
            this.MouseDoubleClick += _MouseDoubleClick;
        }
        /// <summary>
        /// Eventhandler MouseClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseClick(object sender, MouseEventArgs e)
        {
            _RunMouseEvent(e.Location, e.Button, false);
        }
        /// <summary>
        /// Eventhandler MouseDoubleClick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MouseDoubleClick(object sender, MouseEventArgs e)
        {
            _RunMouseEvent(e.Location, e.Button, true);
        }
        /// <summary>
        /// Řešení události MouseClick a MouseDoubleClick
        /// </summary>
        /// <param name="location"></param>
        /// <param name="buttons"></param>
        /// <param name="isDoubleClick"></param>
        private void _RunMouseEvent(Point location, MouseButtons buttons, bool isDoubleClick)
        {
            bool hasItem = TryGetViewItemOnPoint(location, out var viewItem);
            if (hasItem)
            {
                var itemId = this.GetItemId(viewItem);

                DxListBoxItemMouseClickEventArgs args = new DxListBoxItemMouseClickEventArgs(buttons, location, Control.ModifierKeys, itemId);
                if (!isDoubleClick)
                {
                    OnItemMouseClick(args);
                    ItemMouseClick?.Invoke(this, args);
                }
                else
                {
                    OnItemMouseDoubleClick(args);
                    ItemMouseDoubleClick?.Invoke(this, args);
                }
            }
        }
        /// <summary>
        /// Obsluha KeyDown: Enter
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _DoKeyActionEnter(KeyEventArgs e)
        {
            bool hasItem = TryGetViewItemOnIndex(this.SelectedIndex, out var viewItem);
            if (!hasItem) return false;

            var itemId = this.GetItemId(viewItem);
            DxListBoxItemKeyEventArgs args = new DxListBoxItemKeyEventArgs(e, itemId);
            OnItemEnterKeyDown(args);
            ItemEnterKeyDown?.Invoke(this, args);
            return true;
        }
        /// <summary>
        /// Proběhne po jednoduchém kliknutí na prvek
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemMouseClick(DxListBoxItemMouseClickEventArgs args) { }
        /// <summary>
        /// Proběhne po jednoduchém kliknutí na prvek
        /// </summary>
        public event DxListBoxItemMouseClickDelegate ItemMouseClick;
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemMouseDoubleClick(DxListBoxItemMouseClickEventArgs args) { }
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        public event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick;
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemEnterKeyDown(DxListBoxItemKeyEventArgs args) { }
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        public event DxListBoxItemKeyDelegate ItemEnterKeyDown;
        #endregion
        #region ToolTip
        /// <summary>
        /// Inicializace ToolTipu, voláno z konstruktoru
        /// </summary>
        private void _ToolTipInit()
        {
            this.ToolTipAllowHtmlText = null;
            this.DxToolTipController = DxComponent.CreateNewToolTipController(ToolTipAnchor.Cursor);
            this.DxToolTipController.AddClient(this);                                               // Protože this třída implementuje IDxToolTipDynamicClient, bude volána metoda IDxToolTipDynamicClient.PrepareSuperTipForPoint()
            this.DxToolTipController.ToolTipDebugTextChanged += _ToolTipDebugTextChanged;           // Má význam jen pro Debug, nemusí být řešeno
        }
        /// <summary>
        /// Dispose ToolTipu
        /// </summary>
        private void _ToolTipDispose()
        {
            this.DxToolTipController?.Dispose();
        }
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy? Null = default
        /// </summary>
        public bool? ToolTipAllowHtmlText { get; set; }
        /// <summary>
        /// Controller ToolTipu
        /// </summary>
        public DxToolTipController DxToolTipController
        {
            get { return __DxToolTipController; }
            private set { __DxToolTipController = value; this.ToolTipController = value; }
        }
        private DxToolTipController __DxToolTipController;
        /// <summary>
        /// Zde control určí, jaký ToolTip má být pro danou pozici myši zobrazen
        /// </summary>
        /// <param name="args"></param>
        void IDxToolTipDynamicClient.PrepareSuperTipForPoint(Noris.Clients.Win.Components.AsolDX.DxToolTipDynamicPrepareArgs args)
        {
            bool hasItem = TryGetViewItemOnPoint(args.MouseLocation, out var viewItem);
            if (hasItem)
            {
                // Pokud myš nyní ukazuje na ten samý prvek, pro který už máme ToolTip vytvořen, pak nebudeme ToolTip připravovat:
                bool isSameAsLast = (args.DxSuperTip != null && Object.ReferenceEquals(args.DxSuperTip.ClientData, viewItem.Item));
                if (!isSameAsLast)
                {   // Připravíme data pro ToolTip:
                    var dxSuperTip = _CreateDxSuperTip(viewItem.Item);                   // Vytvořím new data ToolTipu
                    if (dxSuperTip != null)
                    {
                        if (ToolTipAllowHtmlText.HasValue) dxSuperTip.ToolTipAllowHtmlText = ToolTipAllowHtmlText;
                        dxSuperTip.ClientData = viewItem.Item;                           // Přibalím si do nich náš Node abych příště detekoval, zda jsme/nejsme na tom samém
                    }
                    args.DxSuperTip = dxSuperTip;
                    args.ToolTipChange = DxToolTipChangeType.NewToolTip;                 // Zajistím rozsvícení okna ToolTipu
                    //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "ToolTip: New item found");
                }
                else
                {
                    args.ToolTipChange = DxToolTipChangeType.SameAsLastToolTip;          // Není třeba nic dělat, nechme svítit stávající ToolTip
                }
            }
            else
            {   // Myš je mimo prvky:
                args.ToolTipChange = DxToolTipChangeType.NoToolTip;                      // Pokud ToolTip svítí, zhasneme jej
                //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, "ToolTip: NoToolTip");
            }
        }
        /// <summary>
        /// Vytvoří data pro ToolTip pro daný prvek Listu
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private DxSuperToolTip _CreateDxSuperTip(object item)
        {
            string toolTipTitle = null;
            string toolTipText = null;
            switch (__ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    // V režimu MenuItem máme prvky Listu postavené na interface IMenuItem;
                    // Ten implementuje ITextItem a tedy i IToolTipItem;
                    // A pro IToolTipItem umíme vytvořit DxSuperToolTip standardním postupem:
                    if (item is IMenuItem menuItem)
                        return DxComponent.CreateDxSuperTip(menuItem);
                    break;
                case ListBoxItemsMode.Table:
                    if (item is System.Data.DataRowView rowView && rowView.Row != null)
                    {   // Z datového řádku 
                        toolTipTitle = _GetTableToolTipTitle(rowView.Row);
                        toolTipText = _GetTableToolTipText(rowView.Row);
                        return DxComponent.CreateDxSuperTip(toolTipTitle, toolTipText);
                    }
                    break;
            }
            return null;
        }
        /// <summary>
        /// Explicitně skrýt ToolTip - typicky při nějaké myší akci (drag and drop, kontextové menu atd)
        /// </summary>
        /// <param name="message"></param>
        private void _ToolTipHide(string message = null)
        {
            this.DxToolTipController.HideTip();
            //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"TreeList.HideToolTip({message})");
        }
        /// <summary>
        /// V controlleru ToolTipu došlo k události, pošli ji do našeho eventu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ToolTipDebugTextChanged(object sender, DxToolTipArgs args)
        {
            //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, args.EventName);
        }
        #endregion
        #region VisibleItems, SelectedItems, ActiveItem a Id, a konverze ...
        /// <summary>
        /// Aktuálně viditelné a dostupné prvky v jejich pořadí.
        /// Pokud je aplikován řádkový filtr, pak jde o ty prvky, které mu vyhovují.
        /// Toto pole neobsahuje prvky, které nejsou ve viditelné oblasti = sice na ně může být nascrollováno, ale aktuálně z nich není vidět ani jeden pixel.
        /// <para/>
        /// Prvky jsou typu buď <see cref="IMenuItem"/> anebo <see cref="System.Data.DataRow"/>, podle režimu <see cref="ItemsMode"/>.
        /// </summary>
        public object[] VisibleItems { get { return this.VisibleViewItems.Select(li => GetDataItem(li)).ToArray(); } }             // ImageListBoxItem => Value => IMenuItem / DataRow
        /// <summary>
        /// Aktuálně viditelné a dostupné prvky, jejich ItemId, v jejich pořadí.
        /// Pokud je aplikován řádkový filtr, pak jde o ty prvky, které mu vyhovují.
        /// Toto pole neobsahuje prvky, které nejsou ve viditelné oblasti = sice na ně může být nascrollováno, ale aktuálně z nich není vidět ani jeden pixel.
        /// </summary>
        public object[] VisibleItemsId { get { return this.VisibleViewItems.Select(li => GetItemId(li)).ToArray(); } }             // ImageListBoxItem => Value => IMenuItem / DataRow => ItemId
        /// <summary>
        /// Pole aktuálně viditelných, typu DevExpress.ViewInfo.
        /// Pokud je aplikován řádkový filtr, pak jde o ty prvky, které mu vyhovují.
        /// Toto pole neobsahuje prvky, které nejsou ve viditelné oblasti = sice na ně může být nascrollováno, ale aktuálně z nich není vidět ani jeden pixel.
        /// </summary>
        protected DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo[] VisibleViewItems
        {
            get
            {
                var items = this.ViewInfo?.VisibleItems;
                if (items == null) return null;
                int count = items.Count;
                var itemsArray = new DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo[count];
                items.CopyTo(itemsArray, 0);
                return itemsArray;
            }
        }
        /// <summary>
        /// Aktuálně označené objekty. Může jich být i více, nebo žádný.
        /// Objekty to mohou být různé, typicky <see cref="IMenuItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID označených řádků je v poli <see cref="SelectedItemsId"/>.
        /// </summary>
        public new object[] SelectedItems { get { return base.SelectedItems.Select(li => GetDataItem(li)).ToArray(); } }         // ImageListBoxItem => Value => IMenuItem / DataRow
        /// <summary>
        /// Pole obsahující ID selectovaných záznamů.
        /// </summary>
        public object[] SelectedItemsId { get { return this.SelectedItems.Select(i => GetItemId(i)).ToArray(); } }               // ImageListBoxItem => Value => IMenuItem / DataRow => ItemId
        /// <summary>
        /// Prvek, na kterém je kurzor. Je jen jediný, nebo null.
        /// Objekty to mohou být různé, typicky <see cref="IMenuItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID aktivního řádku je v <see cref="CurrentItemId"/>.
        /// </summary>
        public object CurrentItem
        {
            get { return GetDataItem(base.SelectedItem); }                               // ImageListBoxItem => Value => IMenuItem / DataRow
        }
        /// <summary>
        /// Pole obsahující ID aktivního řádku.
        /// </summary>
        public object CurrentItemId
        {
            get { return GetItemId(CurrentItem); }
        }
        /// <summary>
        /// Metoda najde prvek this Listu, který se nachází na dané souřadnici.
        /// Souřadnice je v koordinátech controlu, tedy 0,0 = levý horní roh ListBoxu.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="foundItem"></param>
        /// <returns></returns>
        public bool TryGetViewItemOnPoint(Point point, out DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo foundItem)
        {
            foundItem = null;
            if (!this.ClientRectangle.Contains(point)) return false;

            var visibleItems = this.VisibleViewItems;
            if (visibleItems is null || visibleItems.Length == 0) return false;

            return visibleItems.TryGetFirst(i => i.Bounds.Contains(point), out foundItem);
        }
        /// <summary>
        /// Metoda najde prvek, který se nachází na daném indexu.
        /// Vyhledává pouze v dostupných prvcích = pokud je aplikován řádkový filtr, pak tedy jen v těch, které mu vyhovují.
        /// Dostupné = ty, co jsou vidět a nebo na ně může být nascrollováno (tedy nemusí být fyzicky vidět v aktuální oblasti, mohou být pod ní nebo nad ní).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="foundItem"></param>
        /// <returns></returns>
        public bool TryGetViewItemOnIndex(int index, out DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo foundItem)
        {
            foundItem = null;
            if (index < 0) return false;

            var visibleItems = this.VisibleViewItems;
            if (visibleItems is null || index >= visibleItems.Length) return false;
            foundItem = visibleItems[index];
            return true;
        }
        /// <summary>
        /// Metoda dostává Item v rámci ListBoxu, což může být <see cref="DevExpress.XtraEditors.Controls.ImageListBoxItem"/> anebo <see cref="System.Data.DataRowView"/>.
        /// Podle toho v nich vyhledá odpovídající datový prvek <see cref="IMenuItem"/> anebo <see cref="System.Data.DataRow"/> a ten vrátí.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected object GetDataItem(object item)
        {
            if (item is null) return null;
            if (item is DevExpress.XtraEditors.ViewInfo.ImageListBoxViewInfo.ImageItemInfo imgItem) item = imgItem.Item;
            if (item is DevExpress.XtraEditors.Controls.ImageListBoxItem lbxItem) item = lbxItem.Value;
            if (item is System.Data.DataRowView rowView) item = rowView.Row;

            switch (__ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    if (item is IMenuItem menuItem)
                        return menuItem;
                    break;
                case ListBoxItemsMode.Table:
                    if (item is System.Data.DataRow lbxRow)
                        return lbxRow;
                    break;
            }
            return null;
        }
        /// <summary>
        /// Metoda vrátí ID pro daný prvek.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected object GetItemId(object item)
        {
            if (item is null) return null;
            if (item is DevExpress.XtraEditors.ViewInfo.ImageListBoxViewInfo.ImageItemInfo imgItem) item = imgItem.Item;
            if (item is DevExpress.XtraEditors.Controls.ImageListBoxItem lbxItem) item = lbxItem.Value;
            if (item is System.Data.DataRowView rowView) item = rowView.Row;

            switch (__ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    if (item is IMenuItem menuItem) 
                        return menuItem.ItemId;
                    break;
                case ListBoxItemsMode.Table:
                    if (item is System.Data.DataRow row)
                        return _GetTableItemId(row);
                    break;
            }
            return null;
        }
        private object _GetTableItemId(System.Data.DataRow row)
        {
            string colName = this.DxTemplate.ColumnNameItemId;
            if (!String.IsNullOrEmpty(colName) && row != null && row.Table.Columns.Contains(colName))
                return row[colName];
            return null;
        }
        private string _GetTableToolTipTitle(System.Data.DataRow row)
        {
            string colName = this.DxTemplate.ColumnNameToolTipTitle;
            if (!String.IsNullOrEmpty(colName) && row != null && row.Table.Columns.Contains(colName))
                return row[colName] as string;
            return null;
        }
        private string _GetTableToolTipText(System.Data.DataRow row)
        {
            string colName = this.DxTemplate.ColumnNameToolTipText;
            if (!String.IsNullOrEmpty(colName) && row != null && row.Table.Columns.Contains(colName))
                return row[colName] as string;
            return null;
        }
        #endregion
        #region DoKeyActions; CtrlA, CtrlC, CtrlX, CtrlV, Delete; Move, Insert, Remove
        /// <summary>
        /// Povolené akce. Výchozí je <see cref="ControlKeyActionType.None"/>
        /// </summary>
        public ControlKeyActionType EnabledKeyActions { get; set; }
        /// <summary>
        /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="ControlKeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
        /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
        /// </summary>
        /// <param name="actions"></param>
        public void DoKeyActions(params ControlKeyActionType[] actions)
        {
            foreach (ControlKeyActionType action in actions)
                _DoKeyAction(action, null, true);
        }
        /// <summary>
        /// Inicializace eventhandlerů a hodnot pro KeyActions
        /// </summary>
        private void _KeyActionsInit()
        {
            this.KeyDown += _KeyDown;
            this.EnabledKeyActions = ControlKeyActionType.None;
        }
        /// <summary>
        /// Obsluha kláves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"KeyDown: KeyData: [{e.KeyData}]; KeyCode: [{e.KeyCode}]");

            var enabledActions = EnabledKeyActions;
            bool isHandled = false;
            switch (e.KeyData)
            {
                case Keys.Delete:
                    isHandled = _DoKeyAction(ControlKeyActionType.Delete, e);
                    break;
                case Keys.Control | Keys.A:
                    isHandled = _DoKeyAction(ControlKeyActionType.SelectAll, e);
                    break;
                case Keys.Control | Keys.C:
                    isHandled = _DoKeyAction(ControlKeyActionType.ClipCopy, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.X:
                    // Ctrl+X : pokud je povoleno, provedu to; pokud ale nelze provést Ctrl+X a přitom lze provést Ctrl+C, tak se provede to:
                    if (EnabledKeyActions.HasFlag(ControlKeyActionType.ClipCut))
                        isHandled = _DoKeyAction(ControlKeyActionType.ClipCut, e);
                    else if (EnabledKeyActions.HasFlag(ControlKeyActionType.ClipCopy))
                        isHandled = _DoKeyAction(ControlKeyActionType.ClipCopy, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.V:
                    isHandled = _DoKeyAction(ControlKeyActionType.ClipPaste, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Alt | Keys.Home:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveTop, e);
                    break;
                case Keys.Alt | Keys.Up:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveUp, e);
                    break;
                case Keys.Alt | Keys.Down:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveDown, e);
                    break;
                case Keys.Alt | Keys.End:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveBottom, e);
                    break;
                case Keys.Control | Keys.Z:
                    isHandled = _DoKeyAction(ControlKeyActionType.Undo, e);
                    break;
                case Keys.Control | Keys.Y:
                    isHandled = _DoKeyAction(ControlKeyActionType.Redo, e);
                    break;
                case Keys.Return:
                case Keys.Shift | Keys.Return:
                case Keys.Control | Keys.Return:
                    isHandled = _DoKeyActionEnter(e);
                    break;
                default:
                    ControlKeyActionType rowFilterAction = _IsActivateKeyForFilter(e);
                    if (rowFilterAction != ControlKeyActionType.None)
                        isHandled = _DoKeyAction(rowFilterAction, e);
                    break;
            }
            if (isHandled)
                e.Handled = true; 
        }
        /// <summary>
        /// Provede akce zadané jako bity v dané akci (<paramref name="actions"/>), s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="actions">Požadovaná akce</param>
        /// <param name="e">Data o klávese, může být null</param>
        /// <param name="force">Provede danou akci i tehdy, když List sám ji nemá povolenou v nastavení <see cref="EnabledKeyActions"/>! </param>
        private bool _DoKeyAction(ControlKeyActionType actions, KeyEventArgs e = null, bool force = false)
        {
            bool handled = false;
            
            // Akce okolo řádkového filtru jsou povoleny vždy:
            var enabledKeyActions = EnabledKeyActions | ControlKeyActionType.ActivateFilter | ControlKeyActionType.FillKeyToFilter;

            doSingleAction(ControlKeyActionType.Refresh, null);
            doSingleAction(ControlKeyActionType.SelectAll, _DoKeyActionCtrlA);
            doSingleAction(ControlKeyActionType.ClipCopy, _DoKeyActionCtrlC);
            doSingleAction(ControlKeyActionType.ClipCut, _DoKeyActionCtrlX);
            doSingleAction(ControlKeyActionType.ClipPaste, _DoKeyActionCtrlV);
            doSingleAction(ControlKeyActionType.MoveTop, _DoKeyActionMoveTop);
            doSingleAction(ControlKeyActionType.MoveUp, _DoKeyActionMoveUp);
            doSingleAction(ControlKeyActionType.MoveDown, _DoKeyActionMoveDown);
            doSingleAction(ControlKeyActionType.MoveBottom, _DoKeyActionMoveBottom);
            doSingleAction(ControlKeyActionType.Delete, _DoKeyActionDelete);
            doSingleAction(ControlKeyActionType.CopyToRightOne, null);                // Pozn. pokud není dodaná metoda pro akci (=null), pak tuto akci má řešit pouze nadřazený container
            doSingleAction(ControlKeyActionType.CopyToRightAll, null);                //  - pomocí eventhandlerů ListActionBefore a ListActionAfter
            doSingleAction(ControlKeyActionType.CopyToLeftOne, null);
            doSingleAction(ControlKeyActionType.CopyToLeftAll, null);
            doSingleAction(ControlKeyActionType.Undo, _DoKeyActionUndo);
            doSingleAction(ControlKeyActionType.Redo, _DoKeyActionRedo);
            doSingleAction(ControlKeyActionType.ActivateFilter, null);                // Měl by odchytit Parent container a případně přesměrovat
            doSingleAction(ControlKeyActionType.FillKeyToFilter, null);               //  obdobně
            return handled;

            // Zjistí, zda má být provedena daná akce, a pokud ano pak ji provede.
            void doSingleAction(ControlKeyActionType action, Action internalActionMethod)
            {
                if (!actions.HasFlag(action)) return;                          // Tato akce není požadována
                if (!force && !enabledKeyActions.HasFlag(action)) return;      // Tato akce sice je požadována, ale není povolena

                var argsBefore = new DxListBoxActionCancelEventArgs(actions, e);
                _RunListActionBefore(argsBefore);
                if (!argsBefore.Cancel)
                {
                    if (internalActionMethod != null) internalActionMethod();  // Provedu konkrétní akci, pokud je dodána; viz dole napž. _DoKeyActionCtrlA()
                    var argsAfter = new DxListBoxActionEventArgs(actions, e);
                    _RunListActionAfter(argsAfter);
                    handled = true;
                }
            }
        }
        /// <summary>
        /// Vrátí true, pokud dodaná klávesa může být aktivační klávesou pro přestup do řádkového filtru.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ControlKeyActionType _IsActivateKeyForFilter(KeyEventArgs e)
        {
            if ((e.KeyData == Keys.Up || e.KeyData == Keys.PageUp) && this.SelectedIndex <= 0) return ControlKeyActionType.ActivateFilter;   // Šipka/stránka nahoru na první (nebo žádné) pozici aktivuje řádkový filtr

            char? inputChar = DxComponent.KeyConvertToChar(e, true);
            if (inputChar.HasValue) return ControlKeyActionType.FillKeyToFilter;
            return ControlKeyActionType.None;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlA
        /// </summary>
        private void _DoKeyActionCtrlA()
        {
            this.SelectedMenuItems = this.MenuItems;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlC
        /// </summary>
        private void _DoKeyActionCtrlC()
        {
            var selectedItems = this.SelectedMenuItems;
            string textTxt = selectedItems.ToOneString();
            DataExchangeClipboardPublish(selectedItems, textTxt);
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlX
        /// </summary>
        private void _DoKeyActionCtrlX()
        {
            _DoKeyActionCtrlC();
            _DoKeyActionDelete();
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlV
        /// </summary>
        private void _DoKeyActionCtrlV()
        {
            if (!DataExchangeClipboardAcquire(out var data)) return;
            if (data is IEnumerable<IMenuItem> items) InsertItems(items, true, true);
        }
        /// <summary>
        /// Provedení klávesové akce: Delete
        /// </summary>
        private void _DoKeyActionDelete()
        {
            RemoveIndexes(this.SelectedIndexes);
        }
        /// <summary>
        /// Provedení klávesové akce: MoveTop
        /// </summary>
        private void _DoKeyActionMoveTop()
        {
            _MoveSelectedItems(items => 0);
        }
        /// <summary>
        /// Provedení klávesové akce: MoveUp
        /// </summary>
        private void _DoKeyActionMoveUp()
        {
            _MoveSelectedItems(
                items =>
                {   // Přesun o jeden prvek nahoru: najdeme nejmenší index z dodaných prvků, a přesuneme prvky na index o 1 menší, přinejmenším na 0:
                    // Vstupní pole není null a má nejméně jeden prvek!
                    int targetIndex = items.Select(i => i.Item1).Min() - 1;    // Cílový index je o 1 menší, než nejmenší vybraný index, 
                    if (targetIndex < 0) targetIndex = 0;                      //  anebo 0 (když je vybrán prvek na indexu 0)
                    return targetIndex;
                });
        }
        /// <summary>
        /// Provedení klávesové akce: MoveDown
        /// </summary>
        private void _DoKeyActionMoveDown()
        {
            _MoveSelectedItems(
                items =>
                {   // Přesun o jeden prvek dolů: najdeme nejmenší index z dodaných prvků, a přesuneme prvky na index o 1 vyšší, nebo null = na konec:
                    // Vstupní pole není null a má nejméně jeden prvek!
                    int targetIndex = items.Select(i => i.Item1).Min() + 1;    // Cílový index je o 1 větší, než nejmenší vybraný index, 
                    int countRemain = this.ItemCount - items.Length;           // Počet prvků v Listu, které NEJSOU přesouvány
                    if (targetIndex < countRemain) return targetIndex;         // Pokud cílový index bude menší než poslední zbývající prvek, pak prvky přesuneme pod něj
                    return null;                                               // null => prvky přemístíme na konec zbývajících prvků v Listu
                });
        }
        /// <summary>
        /// Provedení klávesové akce: MoveBottom
        /// </summary>
        private void _DoKeyActionMoveBottom()
        {
            _MoveSelectedItems(items => null);
        }
        /// <summary>
        /// Provedení klávesové akce: Undo
        /// </summary>
        private void _DoKeyActionUndo()
        {
            if (this.UndoRedoEnabled) this.UndoRedoController.DoUndo();
        }
        /// <summary>
        /// Provedení klávesové akce: Redo
        /// </summary>
        private void _DoKeyActionRedo()
        {
            if (this.UndoRedoEnabled) this.UndoRedoController.DoRedo();
        }
        /// <summary>
        /// Provedení akce: Move[někam].
        /// Metoda zjistí, které prvky jsou selectované (a pokud žádný, tak skončí).
        /// Metoda se zeptá dodaného lokátora, kam (na který index) chce přesunout vybrané prvky, ty předá lokátoru.
        /// </summary>
        /// <param name="targetIndexLocator">Lokátor: pro dodané selectované prvky vrátí index přesunu. Prvky nejsou null a je nejméně jeden. Jinak se přesun neprovádí.</param>
        private void _MoveSelectedItems(Func<Tuple<int, IMenuItem, Rectangle?>[], int?> targetIndexLocator)
        {
            var selectedItemsInfo = this.SelectedMenuItemsExt;
            if (selectedItemsInfo.Length == 0) return;

            var targetIndex = targetIndexLocator(selectedItemsInfo);
            _MoveItems(selectedItemsInfo, targetIndex);
        }
        /// <summary>
        /// Provedení akce: přesuň zdejší dodané prvky na cílovou pozici.
        /// </summary>
        private void _MoveItems(Tuple<int, IMenuItem, Rectangle?>[] selectedItemsInfo, int? targetIndex)
        {
            if (selectedItemsInfo is null || selectedItemsInfo.Length == 0) return;

            // Odebereme zdrojové prvky a vložíme je na zadaný index:
            RemoveIndexes(selectedItemsInfo.Select(t => t.Item1));
            InsertItems(selectedItemsInfo.Select(i => i.Item2), targetIndex, true);
        }
        /// <summary>
        /// Metoda zajistí přesunutí označených prvků na danou pozici.
        /// Pokud je zadaná pozice 0, pak jsou prvky přemístěny v jejich pořadí úplně na začátek Listu.
        /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou přemístěny za první prvek.
        /// Pokud je daná pozice null nebo větší než počet prvků, jsou prvky přemístěny na konec listu.
        /// </summary>
        /// <param name="targetIndex"></param>
        public void MoveSelectedItems(int? targetIndex)
        {
            _MoveItems(this.SelectedMenuItemsExt, targetIndex);
        }
        /// <summary>
        /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje aktuální pozicí vybraného prvku.
        /// </summary>
        /// <param name="sourceItems"></param>
        /// <param name="atCurrentIndex">Požadavek true = na pozici aktuálního prvku / false = na konec</param>
        /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
        public void InsertItems(IEnumerable<IMenuItem> sourceItems, bool atCurrentIndex, bool selectNewItems)
        {
            if (sourceItems is null || !sourceItems.Any()) return;

            int? insertIndex = (atCurrentIndex && this.Items.Count > 0 && this.SelectedIndex >= 0 ? (int?)this.SelectedIndex : (int?)null);
            InsertItems(sourceItems, insertIndex, true);
        }
        /// <summary>
        /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou pozicí <paramref name="insertIndex"/>.
        /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.
        /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.
        /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
        /// </summary>
        /// <param name="sourceItems"></param>
        /// <param name="insertIndex"></param>
        /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
        public void InsertItems(IEnumerable<IMenuItem> sourceItems, int? insertIndex, bool selectNewItems)
        {
            if (sourceItems is null || !sourceItems.Any()) return;

            var validItems = _GetOnlyValidItems(sourceItems, true);
            if (validItems.Length == 0) return;

            List<int> selectedIndexes = new List<int>();
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value < this.ItemCount)
            {
                int index = insertIndex.Value;
                foreach (var sourceItem in validItems)
                {
                    DevExpress.XtraEditors.Controls.ImageListBoxItem imgItem = new DevExpress.XtraEditors.Controls.ImageListBoxItem(sourceItem);
                    selectedIndexes.Add(index);
                    this.Items.Insert(index++, imgItem);
                }
            }
            else
            {
                int index = this.ItemCount;
                foreach (var sourceItem in validItems)
                    selectedIndexes.Add(index++);
                this.Items.AddRange(validItems);
            }

            if (selectNewItems)
                this.SelectedIndexes = selectedIndexes;
        }
        /// <summary>
        /// Z this Listu odebere prvky na daných indexech.
        /// </summary>
        /// <param name="removeIndexes"></param>
        public void RemoveIndexes(IEnumerable<int> removeIndexes)
        {
            if (removeIndexes == null) return;
            int count = this.ItemCount;
            var removeList = removeIndexes
                .CreateDictionary(i => i, true)                      // Odstraním duplicitní hodnoty indexů;
                .Keys.Where(i => (i >= 0 && i < count))              //  z klíčů (indexy) vyberu jen hodnoty, které reálně existují v ListBoxu;
                .ToList();                                           //  a vytvořím List pro další práci:
            removeList.Sort((a, b) => b.CompareTo(a));               // Setřídím indexy sestupně, pro korektní postup odebírání
            removeList.ForEachExec(i => this.Items.RemoveAt(i));     // A v sestupném pořadí indexů odeberu odpovídající prvky
        }
        /// <summary>
        /// Z this Listu odebere všechny dané prvky
        /// </summary>
        /// <param name="removeItems"></param>
        public void RemoveItems(IEnumerable<IMenuItem> removeItems)
        {
            if (removeItems == null) return;
            var removeArray = removeItems.ToArray();
            var listItems = this.MenuItems;
            for (int i = this.ItemCount - 1; i >= 0; i--)
            {
                var listItem = listItems[i];
                if (listItem != null && removeArray.Any(t => Object.ReferenceEquals(t, listItem)))
                    this.Items.RemoveAt(i);
            }
        }
        /// <summary>
        /// Metoda z dodané kolekce prvku vrátí jen ty platné.
        /// To jsou ty, které nejsou NULL a které dodržují pravidlo nonduplicity.
        /// Pokud NENÍ povolena duplicita (tj. <see cref="DuplicityEnabled"/> je false), pak:
        /// - Každý prvek musí mít nenulové <see cref="ITextItem.ItemId"/>;
        /// - Jedna hodnota <see cref="ITextItem.ItemId"/> se v Listu nesmí opakovat; opakovaný výskyt je ignorován
        /// </summary>
        /// <param name="items"></param>
        /// <param name="withCurrentList">Do kontroly duplicity zahrnout i stávající prvky Listu?</param>
        /// <returns></returns>
        private IMenuItem[] _GetOnlyValidItems(IEnumerable<IMenuItem> items, bool withCurrentList)
        {
            List<IMenuItem> validItems = new List<IMenuItem>();
            if (items != null)
            {
                bool duplicityEnabled = this.DuplicityEnabled;
                Dictionary<string, IMenuItem> itemIdDict = (duplicityEnabled ? null : 
                      (withCurrentList ? this.MenuItems.Where(i => i.ItemId != null).CreateDictionary(i => i.ItemId, true) : 
                      new Dictionary<string, IMenuItem>()));
                foreach (var item in items)
                {
                    if (item is null) continue;
                    if (!duplicityEnabled && (item.ItemId is null || itemIdDict.ContainsKey(item.ItemId))) continue;
                    validItems.Add(item);
                    if (!duplicityEnabled) itemIdDict.Add(item.ItemId, item);
                }
            }
            return validItems.ToArray();
        }
        #endregion
        #region DataExchange
        /// <summary>
        /// Inicializace hodnot pro DataExchange
        /// </summary>
        private void _DataExchangeInit()
        {
            DataExchangeCurrentControlId = DxComponent.CreateGuid();
        }
        /// <summary>
        /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
        /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
        /// </summary>
        public string DataExchangeCurrentControlId { get; set; }
        /// <summary>
        /// Režim výměny dat při pokusu o vkládání do tohoto objektu.
        /// Pokud některý jiný objekt provedl Ctrl+C, pak svoje data vložil do balíčku <see cref="DataExchangeContainer"/>,
        /// přidal k tomu svoje ID controlu (jako zdejší <see cref="DataExchangeCurrentControlId"/>) do <see cref="DataExchangeContainer.DataSourceId"/>,
        /// do balíčku se přidalo ID aplikace do <see cref="DataExchangeContainer.ApplicationGuid"/>, a tato data jsou uložena v Clipboardu.
        /// <para/>
        /// Pokud nyní zdejší control zaeviduje klávesu Ctrl+V, pak zjistí, zda v Clipboardu existuje balíček <see cref="DataExchangeContainer"/>,
        /// a pokud ano, pak prověří, zda this control může akceptovat data ze zdroje v balíčku uvedeného, na základě nastavení režimu výměny v <see cref="DataExchangeCrossType"/>
        /// a ID zdrojového controlu podle <see cref="DataExchangeAcceptSourceControlId"/>.
        /// </summary>
        public DataExchangeCrossType DataExchangeCrossType { get; set; }
        /// <summary>
        /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
        /// </summary>
        public string DataExchangeAcceptSourceControlId { get; set; }
        /// <summary>
        /// Dodaná data umístí do clipboardu 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="text"></param>
        private void DataExchangeClipboardPublish(object data, string text)
        {
            DxComponent.ClipboardInsert(DataExchangeCurrentControlId, data, text);
        }
        /// <summary>
        /// Pokusí se z Clipboardu získat data pro this control, podle aktuálního nastavení. Vrací true = máme data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool DataExchangeClipboardAcquire(out object data)
        {
            data = null;
            if (!DxComponent.ClipboardTryGetApplicationData(out DataExchangeContainer appDataContainer)) return false;
            if (!DxComponent.CanAcceptExchangeData(appDataContainer, this.DataExchangeCurrentControlId, this.DataExchangeCrossType, this.DataExchangeAcceptSourceControlId)) return false;
            data = appDataContainer.Data;
            return true;
        }
        #endregion
        #region Drag and Drop
        /// <summary>
        /// Souhrn povolených akcí Drag and Drop
        /// </summary>
        public DxDragDropActionType DragDropActions { get { return _DragDropActions; } set { _DxDragDropInit(value); } }
        private DxDragDropActionType _DragDropActions;
        /// <summary>
        /// Vrátí true, pokud je povolena daná akce
        /// </summary>
        /// <param name="action"></param>
        private bool _IsDragDropActionEnabled(DxDragDropActionType action) { return _DragDropActions.HasFlag(action); }
        /// <summary>
        /// Nepoužívejme v aplikačním kódu. 
        /// Místo toho používejme property <see cref="DragDropActions"/>.
        /// </summary>
        public override bool AllowDrop { get { return this._AllowDrop; } set { } }
        /// <summary>
        /// Obsahuje true, pokud this prvek může být cílem Drag and Drop
        /// </summary>
        private bool _AllowDrop
        {
            get
            {
                var actions = this._DragDropActions;
                return (actions.HasFlag(DxDragDropActionType.ReorderItems) || actions.HasFlag(DxDragDropActionType.ImportItemsInto));
            }
        }
        /// <summary>
        /// Inicializace controlleru Drag and Drop
        /// </summary>
        /// <param name="actions"></param>
        private void _DxDragDropInit(DxDragDropActionType actions)
        {
            if (actions != DxDragDropActionType.None && _DxDragDrop == null)
                _DxDragDrop = new DxDragDrop(this);
            _DragDropActions = actions;
        }
        /// <summary>
        /// Dispose controlleru Drag and Drop
        /// </summary>
        private void _DxDragDropDispose()
        {
            if (_DxDragDrop != null)
                _DxDragDrop.Dispose();
            _DxDragDrop = null;
        }
        /// <summary>
        /// Controller pro aktivitu Drag and Drop, vycházející z this objektu
        /// </summary>
        private DxDragDrop _DxDragDrop;
        /// <summary>
        /// Controller pro DxDragDrop v this controlu
        /// </summary>
        DxDragDrop IDxDragDropControl.DxDragDrop { get { return _DxDragDrop; } }
        /// <summary>
        /// Metoda volaná do objektu Source (zdroj Drag and Drop) při každé akci na straně zdroje.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void IDxDragDropControl.DoDragSource(DxDragDropArgs args)
        {
            switch (args.Event)
            {
                case DxDragDropEventType.DragStart:
                    DoDragSourceStart(args);
                    break;
                case DxDragDropEventType.DragDropAccept:
                    DoDragSourceDrop(args);
                    break;
            }
            return;
        }
        /// <summary>
        /// Metoda volaná do objektu Target (cíl Drag and Drop) při každé akci, pokud se myš nachází nad objektem který implementuje <see cref="IDxDragDropControl"/>.
        /// Předávaný argument <paramref name="args"/> je permanentní, dokud se myš pohybuje nad Source controlem nebo dokud probíhá Drag akce.
        /// </summary>
        /// <param name="args">Veškerá data o procesu Drag and Drop, permanentní po dobu výskytu myši nad Source objektem</param>
        void IDxDragDropControl.DoDragTarget(DxDragDropArgs args)
        {
            switch (args.Event)
            {
                case DxDragDropEventType.DragMove:
                    DoDragTargetMove(args);
                    break;
                case DxDragDropEventType.DragLeaveOfTarget:
                    DoDragTargetLeave(args);
                    break;
                case DxDragDropEventType.DragDropAccept:
                    DoDragTargetDrop(args);
                    break;
                case DxDragDropEventType.DragEnd:
                    DoDragTargetEnd(args);
                    break;
            }
        }
        /// <summary>
        /// Když začíná proces Drag, a this objekt je zdrojem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragSourceStart(DxDragDropArgs args)
        {
            var selectedItems = this.SelectedMenuItemsExt;
            if (selectedItems.Length == 0)
            {
                args.SourceDragEnabled = false;
            }
            else
            {
                args.SourceText = selectedItems.ToOneString(convertor: i => i.Item2.ToString());
                args.SourceObject = selectedItems;
                args.SourceDragEnabled = true;
            }
        }
        /// <summary>
        /// Když probíhá proces Drag, a this objekt je možným cílem.
        /// Objekt this může být současně i zdrojem akce (pokud probíhá Drag and Drop nad týmž objektem), pak jde o Reorder.
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetMove(DxDragDropArgs args)
        {
            Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
            IndexRatio index = DoDragSearchIndexRatio(targetPoint);
            if (!IndexRatio.IsEqual(index, MouseDragTargetIndex))
            {
                MouseDragTargetIndex = index;
                this.Invalidate();
            }
            args.CurrentEffect = args.SuggestedDragDropEffect;
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je zdrojem dat:
        /// Pokud (this objekt je zdrojem i cílem současně) anebo (this je jen zdroj, a režim je Move = přemístit prvky),
        /// pak musíme prvky z this listu (Source) odebrat, a pokud this je i cílem, pak před tím musíme podle pozice myši určit cílový index pro přemístění prvků.
        /// </summary>
        /// <param name="args"></param>
        private void DoDragSourceDrop(DxDragDropArgs args)
        {
            args.TargetIndex = null;
            args.InsertIndex = null;
            var selectedItemsInfo = args.SourceObject as Tuple<int, IMenuItem, Rectangle?>[];
            if (selectedItemsInfo != null && (args.TargetIsSource || args.CurrentEffect == DragDropEffects.Move))
            {
                // Pokud provádíme přesun v rámci jednoho Listu (tj. Target == Source),
                //  pak si musíme najít správný TargetIndex nyní = uživatel chce přemístit prvky před/za určitý prvek, a jeho index se odebráním prvků změní:
                if (args.TargetIsSource)
                {
                    Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
                    args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                    args.InsertIndex = args.TargetIndex.GetInsertIndex(selectedItemsInfo.Select(t => t.Item1));
                }
                // Odebereme zdrojové prvky:
                this.RemoveIndexes(selectedItemsInfo.Select(t => t.Item1));
            }
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je možným cílem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetDrop(DxDragDropArgs args)
        {
            if (args.TargetIndex == null)
            {
                Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
                args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                args.InsertIndex = null;
            }
            if (!args.InsertIndex.HasValue)
                args.InsertIndex = args.TargetIndex.GetInsertIndex();

            // Vložit prvky do this Listu, na daný index, a selectovat je:
            if (DxDragTargetTryGetItems(args, out var sourceItems))
                InsertItems(sourceItems, args.InsertIndex, true);

            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Pokusí se najít v argumentu <see cref="DxDragDropArgs"/> zdrojový objekt a z něj získat pole prvků <see cref="IMenuItem"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private bool DxDragTargetTryGetItems(DxDragDropArgs args, out IMenuItem[] items)
        {
            items = null;
            if (args.SourceObject is IEnumerable<IMenuItem> menuItems) { items = menuItems.ToArray(); return true; }
            if (args.SourceObject is IEnumerable<Tuple<int, IMenuItem, Rectangle?>> listItemsInfo) { items = listItemsInfo.Select(i => i.Item2).ToArray(); return true; }
            return false;
        }
        /// <summary>
        /// Když probíhá proces Drag, ale opouští this objekt, který dosud byl možným cílem (probíhala pro něj metoda <see cref="DoDragTargetMove(DxDragDropArgs)"/>)
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetLeave(DxDragDropArgs args)
        {
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Po skončení procesu Drag
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetEnd(DxDragDropArgs args)
        {
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Metoda vrátí data o prvku pod myší nebo poblíž myši, který je aktivním cílem procesu Drag, pro myš na daném bodě
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        private IndexRatio DoDragSearchIndexRatio(Point targetPoint)
        {
            return IndexRatio.Create(targetPoint, this.ClientRectangle, p => this.IndexFromPoint(p), i => GetItemBounds(i, false), this.ItemCount, Orientation.Vertical);
        }
        /// <summary>
        /// Informace o prvku, nad kterým je myš, pro umístění obsahu v procesu Drag and Drop.
        /// Pokud je null, pak pro this prvek neprobíhá Drag and Drop.
        /// <para/>
        /// Tuto hodnotu vykresluje metoda <see cref="MouseDragPaint(PaintEventArgs)"/>.
        /// </summary>
        private IndexRatio MouseDragTargetIndex;
        /// <summary>
        /// Obsahuje true, pokud v procesu Paint má být volána metoda <see cref="MouseDragPaint(PaintEventArgs)"/>.
        /// </summary>
        private bool MouseDragNeedRePaint { get { return (MouseDragTargetIndex != null); } }
        /// <summary>
        /// Volá se proto, aby this prvek mohl vykreslit Target pozici pro MouseDrag proces
        /// </summary>
        /// <param name="e"></param>
        private void MouseDragPaint(PaintEventArgs e)
        {
            if (!MouseDragNeedRePaint) return;
            var bounds = MouseDragTargetIndex.GetMarkLineBounds();
            if (!bounds.HasValue) return;
            var color = this.ForeColor;
            using (var brush = new SolidBrush(color))
                e.Graphics.FillRectangle(brush, bounds.Value);
        }
        /// <summary>
        /// Index prvku, nad kterým se pohybuje myš
        /// </summary>
        public int OnMouseItemIndex
        {
            get
            {
                if (_OnMouseItemIndex >= this.ItemCount)
                    _OnMouseItemIndex = -1;
                return _OnMouseItemIndex;
            }
            protected set
            {
                if (value != _OnMouseItemIndex)
                {
                    _OnMouseItemIndex = value;
                    this.Invalidate();
                }
            }
        }
        private int _OnMouseItemIndex = -1;
        /// <summary>
        /// Vrátí souřadnice prvku na daném indexu. Volitelně může provést kontrolu na to, zda daný prvek je ve viditelné oblasti Listu. Pokud prvek neexistuje nebo není vidět, vrací null.
        /// Vrácené souřadnice jsou relativní v prostoru this ListBoxu.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        public Rectangle? GetItemBounds(int itemIndex, bool onlyVisible = true)
        {
            if (itemIndex < 0 || itemIndex >= this.ItemCount) return null;

            Rectangle itemBounds = this.GetItemRectangle(itemIndex);
            if (onlyVisible)
            {   // Pokud chceme souřadnice pouze viditelného prvku, pak prověříme souřadnice prvku proti souřadnici prostoru v ListBoxu:
                Rectangle listBounds = this.ClientRectangle;
                if (itemBounds.Right <= listBounds.X || itemBounds.X >= listBounds.Right || itemBounds.Bottom <= listBounds.Y || itemBounds.Y >= listBounds.Bottom)
                    return null;   // Prvek není vidět
            }

            return itemBounds;
        }
        /// <summary>
        /// Vrátí souřadnici prostoru pro myší ikonu
        /// </summary>
        /// <param name="onMouseItemIndex"></param>
        /// <returns></returns>
        protected Rectangle? GetOnMouseIconBounds(int onMouseItemIndex)
        {
            Rectangle? itemBounds = this.GetItemBounds(onMouseItemIndex, true);
            if (!itemBounds.HasValue || itemBounds.Value.Width < 35) return null;        // Pokud prvek neexistuje, nebo není vidět, nebo je příliš úzký => vrátíme null

            int wb = 14;
            int x0 = itemBounds.Value.Right - wb - 6;
            int yc = itemBounds.Value.Y + itemBounds.Value.Height / 2;
            Rectangle iconBounds = new Rectangle(x0 - 1, itemBounds.Value.Y, wb + 1, itemBounds.Value.Height);
            return iconBounds;
        }
        #endregion
        #region UndoRedo manager + akce
        /// <summary>
        /// UndoRedoEnabled List má povoleny akce Undo a Redo?
        /// </summary>
        public bool UndoRedoEnabled 
        { 
            get { return _UndoRedoEnabled; } 
            set 
            {
                _UndoRedoEnabled = value;
                RunUndoRedoEnabledChanged();
            } 
        }
        private bool _UndoRedoEnabled;
        /// <summary>
        /// Controller UndoRedo.
        /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
        /// Pokud je povoleno, je zde vždy instance. 
        /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
        /// </summary>
        public UndoRedoController UndoRedoController
        {
            get 
            {
                if (!UndoRedoEnabled) return null;
                if (_UndoRedoController is null)
                    _UndoRedoControllerSet(new UndoRedoController());
                return _UndoRedoController;
            }
            set
            {
                _UndoRedoControllerSet(value);
            }
        }
        private UndoRedoController _UndoRedoController;
        /// <summary>
        /// Vloží do this instance dodaný controller <see cref="UndoRedoController"/>.
        /// Řeší odvázání eventhandleru od dosavadního controlleru, pak i navázání eventhandleru do nového controlleru, a ihned provede <see cref="RunUndoRedoEnabledChanged"/>.
        /// </summary>
        /// <param name="controller"></param>
        private void _UndoRedoControllerSet(UndoRedoController controller)
        {
            if (_UndoRedoController != null) _UndoRedoController.UndoRedoEnabledChanged -= _UndoRedoEnabledChanged;
            _UndoRedoController = controller;
            if (_UndoRedoController != null)
            {
                _UndoRedoController.UndoRedoEnabledChanged -= _UndoRedoEnabledChanged;
                _UndoRedoController.UndoRedoEnabledChanged += _UndoRedoEnabledChanged;
            }
            RunUndoRedoEnabledChanged();
        }
        /// <summary>
        /// Eventhandler události, kdy controller <see cref="UndoRedoController"/> 
        /// provedl změnu stavu <see cref="UndoRedoController.UndoEnabled"/> anebo <see cref="UndoRedoController.RedoEnabled"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _UndoRedoEnabledChanged(object sender, EventArgs args)
        {
            RunUndoRedoEnabledChanged();
        }
        /// <summary>
        /// Vyvolá háček <see cref="OnUndoRedoEnabledChanged"/> a událost <see cref="UndoRedoEnabledChanged"/>.
        /// </summary>
        private void RunUndoRedoEnabledChanged()
        {
            OnUndoRedoEnabledChanged();
            UndoRedoEnabledChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Po změně stavu Undo/Redo
        /// </summary>
        protected virtual void OnUndoRedoEnabledChanged() { }
        /// <summary>
        /// Po změně stavu Undo/Redo
        /// </summary>
        public event EventHandler UndoRedoEnabledChanged;
        void IUndoRedoControl.DoUndoStep(object state)
        { }
        void IUndoRedoControl.DoRedoStep(object state)
        { }
        #endregion
        #region Public eventy
        /// <summary>
        /// Volá se po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        /// <param name="e"></param>
        private void RunPaintList(PaintEventArgs e)
        {
            OnPaintList(e);
            PaintList?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintList(PaintEventArgs e) { }
        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event PaintEventHandler PaintList;

        /// <summary>
        /// Volá se před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        /// <param name="args"></param>
        private void _RunListActionBefore(DxListBoxActionCancelEventArgs args)
        {
            OnListActionBefore(args);
            ListActionBefore?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListActionBefore(DxListBoxActionCancelEventArgs e) { }
        /// <summary>
        /// Událost vyvolaná před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
        /// </summary>
        public event DxListBoxActionCancelDelegate ListActionBefore;

        /// <summary>
        /// Volá se po provedení kteréhokoli požadavku
        /// </summary>
        /// <param name="args"></param>
        private void _RunListActionAfter(DxListBoxActionEventArgs args)
        {
            OnListActionAfter(args);
            ListActionAfter?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po provedení kteréhokoli požadavku
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListActionAfter(DxListBoxActionEventArgs e) { }
        /// <summary>
        /// Událost vyvolaná po provedení kteréhokoli požadavku
        /// </summary>
        public event DxListBoxActionDelegate ListActionAfter;

        /// <summary>
        /// Volá se po změně selected prvků
        /// </summary>
        private void _RunSelectionChanged()
        {
            OnSelectedItemsChanged();
            SelectedItemsChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Volá se po změně selected prvků.<br/>
        /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
        /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
        /// </summary>
        protected virtual void OnSelectedItemsChanged() { }
        /// <summary>
        /// Událost volaná po změně selected prvků.<br/>
        /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
        /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
        /// </summary>
        public event EventHandler SelectedItemsChanged;
        #endregion
    }
    #region class DxListBoxTemplate : data pro tvorbu šablony v ListBoxu
    /// <summary>
    /// Šablona pro zobrazení prvku v <see cref="DxListBoxControl"/>
    /// </summary>
    public class DxListBoxTemplate : IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxTemplate()
        {
            __Elements = new List<IListBoxTemplateElement>();
            __TemplateCells = new Dictionary<string, TemplateCell>();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (__Elements != null)
            {
                __Elements.Clear();
                __Elements = null;
            }
            _ClearTemplateCells();
            __TemplateCells = null;
        }
        /// <summary>
        /// Disposuje a smaže obsah <see cref="__TemplateCells"/>, ale Dictionary ponechává s 0 záznamy.
        /// </summary>
        private void _ClearTemplateCells()
        {
            if (__TemplateCells != null)
            {
                __TemplateCells.ForEachExec(c => c.Value.Dispose());
                __TemplateCells.Clear();
            }
        }
        /// <summary>
        /// Všechny deklarované buňky
        /// </summary>
        private List<IListBoxTemplateElement> __Elements;
        /// <summary>
        /// Buňky umístěné v šabloně; Key = vygenerované jméno
        /// </summary>
        private Dictionary<string, TemplateCell> __TemplateCells;
        /// <summary>
        /// Jednotlivé buňky šablony
        /// </summary>
        public List<IListBoxTemplateElement> Elements { get { return __Elements; } }
        /// <summary>
        /// Sloupec tabulky, obsahující ItemId aktuálního řádku
        /// </summary>
        public string ColumnNameItemId { get; set; }
        /// <summary>
        /// Sloupec tabulky, obsahující titulek pro ToolTip
        /// </summary>
        public string ColumnNameToolTipTitle { get; set; }
        /// <summary>
        /// Sloupec tabulky, obsahující text pro ToolTip
        /// </summary>
        public string ColumnNameToolTipText { get; set; }
        /// <summary>
        /// Konvertuje zdejší data o layoutu jednotlivých buněk <see cref="Elements"/> = <see cref="IListBoxTemplateElement"/> do fyzické deklarace šablony Template do dodaného Listu.
        /// </summary>
        /// <param name="targetList"></param>
        public void ApplyTemplateToList(DxListBoxControl targetList)
        {
            targetList.Templates.Clear();

            _ClearTemplateCells();

            var elementGroups = __Elements.GroupBy(c => c.TemplateName ?? "");
            int id = 0;
            foreach (var elementGroup in elementGroups)
            {
                var templateName = elementGroup.Key;
                var elements = elementGroup.ToArray();
                var template = _CreateTemplateFromElements(templateName, elements, ref id);
                if (template != null)
                    targetList.Templates.Add(template);
            }

            targetList.ItemPadding = new Padding(2, 1, 2, 1);
            targetList.ItemAutoHeight = true;
        }
        /// <summary>
        /// Vytvoří jednu Template ze všech dodaných dat Elementů. Tato metoda neřeší MultiTemplate a tedy TemplateName.
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="iElements"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.TableLayout.ItemTemplateBase _CreateTemplateFromElements(string templateName, IListBoxTemplateElement[] iElements, ref int id)
        {
            var template = new DevExpress.XtraEditors.TableLayout.ItemTemplateBase() { Name = templateName };

            int[] colWidths = null;
            int[] rowHeights = null;

            createColumns();
            createRows();
            createSpans();
            createTemplateElements(ref id);

            return template;

            // Do vznikající šablony vytvoří sloupce
            void createColumns()
            {
                // Buňky, které jednoznačně určují šířky sloupců:
                var widths = getSingleSizes(iElements
                    .Where(c => c.ColSpan == 1 && c.Width.HasValue)                                // Buňky, které mají ColSpan = 1: určují šířku svého sloupce; a mají Width definované
                    .Select(c => new Tuple<int, int>(c.ColIndex, c.Width.Value)));                 // Tuple: Item1 = index sloupce; Item2 = definovaná šířka

                // Buňky, které mají větší ColSpan a mohou upravit šířky sloupců:
                verifySpanSizes(widths, iElements
                    .Where(c => c.ColSpan > 1 && c.Width.HasValue)                                 // Buňky, které mají ColSpan > 1: vyžadují více sloupců; a mají Width definované
                    .Select(c => new Tuple<int, int, int>(c.ColIndex, c.ColSpan, c.Width.Value))); // Tuple: Item1 = index sloupce; Item2 = ColSpan; Item3 = definovaná šířka

                colWidths = widths.ToArray();

                // Pro zjištěné velikosti vytvoří TableColumnDefinition:
                //  Na rozdíl od řádků (TableRowDefinition) tady pro sloupce není potřeba operovat s AutoWidth.
                foreach (var width in widths)
                {
                    var tColumn = new DevExpress.XtraEditors.TableLayout.TableColumnDefinition();
                    tColumn.Length.Value = (double)width;
                    tColumn.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                    tColumn.PaddingLeft = 1;
                    tColumn.PaddingRight = 1;
                    template.Columns.Add(tColumn);
                }
            }
            // Do vznikající šablony vytvoří řádky
            void createRows()
            {
                // Buňky, které jednoznačně určují výšky řádků:
                var heights = getSingleSizes(iElements
                    .Where(c => c.RowSpan == 1 && c.Height.HasValue)                               // Buňky, které mají RowSpan = 1: určují výšku svého řádku; a mají Height definované
                    .Select(c => new Tuple<int, int>(c.RowIndex, c.Height.Value)));                // Tuple: Item1 = index řádku; Item2 = definovaná výška

                // Buňky, které mají větší RowSpan a mohou upravit výšky řádků:
                verifySpanSizes(heights, iElements
                    .Where(c => c.RowSpan > 1 && c.Height.HasValue)                                // Buňky, které mají RowSpan > 1: vyžadují více řádků; a mají Height definované
                    .Select(c => new Tuple<int, int, int>(c.RowIndex, c.RowSpan, c.Height.Value))); // Tuple: Item1 = index řádku; Item2 = RowSpan; Item3 = definovaná výška

                rowHeights = heights.ToArray();

                // Pro zjištěné velikosti vytvoří TableRowDefinition:
                //  DevExpress má zajímavou vlastnost, kterou obcházím pomocí řízení .AutoHeight:
                //  - Pokud všem Rows nastavím .AutoHeight = true, pak je celý řádek ListBoxu strašně vysoký.
                //  - Pokud nastavím .AutoHeight = false, pak se všechny řádky zmastí do jednoho.
                //  - Ale když nastavím do prvního Row .AutoHeight = false, a do dalších potom .AutoHeight = true, pak je řádek ListBoxu OK...
                //  Ale když je jen jeden řádek, tak musím do toho jediného prvního dát .AutoHeight = true !!!
                bool isAutoSize = (heights.Count == 1);    // Pokud je jen jeden řádek, pak máme připraveno isAutoSize = true; pokud je jich víc, pak první bude mít isAutoSize = false, a další true!
                foreach (var height in heights)
                {
                    var tRow = new DevExpress.XtraEditors.TableLayout.TableRowDefinition();
                    tRow.Length.Value = (double)height;
                    tRow.Length.Type = DevExpress.XtraEditors.TableLayout.TableDefinitionLengthType.Pixel;
                    tRow.AutoHeight = isAutoSize;          // První má false, další mají true. Viz nahoře...
                    tRow.PaddingTop = 0;
                    tRow.PaddingBottom = 0;
                    template.Rows.Add(tRow);
                    isAutoSize = true;
                }
            }
            // Do vznikající šablony vytvoří Spans
            void createSpans()
            {
                var iElementsSpan = iElements.Where(c => c.ColSpan > 1 || c.RowSpan > 1).ToArray();
                foreach (var iElementSpan in iElementsSpan)
                {
                    var tSpan = new DevExpress.XtraEditors.TableLayout.TableSpan() { RowIndex = iElementSpan.RowIndex, ColumnIndex = iElementSpan.ColIndex, RowSpan = iElementSpan.RowSpan, ColumnSpan = iElementSpan.ColSpan };
                    template.Spans.Add(tSpan);
                }
            }
            // Do vznikající šablony vytvoří elementy
            void createTemplateElements(ref int elementId)
            {
                foreach (var iElement in iElements)
                {
                    elementId++;
                    string key = "C_" + elementId.ToString();
                    iElement.Key = key;

                    iElement.CellWidth = getCellSize(colWidths, iElement.ColIndex, iElement.ColSpan);
                    iElement.CellHeight = getCellSize(rowHeights, iElement.RowIndex, iElement.RowSpan);

                    // Jeden element v našem podání zobrazuje buď text, nebo obrázek. Proto máme na vstupu property ContentAlignment, kterou ukládáme jak do TextAlignment, tak i do ImageAlignment. A ImageToTextAlignment dávám None.
                    var tElement = new DevExpress.XtraEditors.TableLayout.TemplatedItemElement()
                    {
                        Name = key,
                        RowIndex = iElement.RowIndex,
                        ColumnIndex = iElement.ColIndex,
                        Width = iElement.Width ?? 0,
                        Height = iElement.Height ?? 0,
                        StretchHorizontal = iElement.StretchHorizontal,
                        StretchVertical = iElement.StretchVertical
                    };

                    switch (iElement.ElementContent)
                    {
                        case ElementContentType.Text:
                            tElement.FieldName = iElement.ColumnName;
                            tElement.TextAlignment = iElement.ContentAlignment ?? TileItemContentAlignment.MiddleLeft;
                            tElement.ImageVisible = false;
                            tElement.ImageToTextAlignment = TileControlImageToTextAlignment.None;
                            break;

                        case ElementContentType.Label:
                            tElement.Text = iElement.Label;
                            tElement.TextAlignment = iElement.ContentAlignment ?? TileItemContentAlignment.MiddleLeft;
                            tElement.ImageVisible = false;
                            tElement.ImageToTextAlignment = TileControlImageToTextAlignment.None;
                            break;

                        case ElementContentType.IconName:
                        case ElementContentType.ImageData:
                            tElement.FieldName = null;
                            tElement.ImageAlignment = iElement.ContentAlignment ?? TileItemContentAlignment.MiddleCenter;
                            tElement.ImageVisible = true;
                            tElement.ImageToTextAlignment = TileControlImageToTextAlignment.None;
                            break;
                    }

                    if (iElement.FontStyle.HasValue)
                        tElement.Appearance.Normal.FontStyleDelta = iElement.FontStyle.Value;

                    if (iElement.FontSizeRatio.HasValue)
                    {
                        tElement.Appearance.Normal.FontSizeDelta = DxComponent.GetFontSizeDelta(iElement.FontSizeRatio);
                        tElement.Appearance.Normal.Options.UseFont = true;
                    }

                    if (iElement.ElementContent == ElementContentType.IconName)
                        iElement.ImageSize = getImageSize(iElement.CellWidth, iElement.CellHeight);

                    template.Elements.Add(tElement);
                    var isDynamicImage = hasDynamicImage(iElement);
                    __TemplateCells.Add(key, new TemplateCell(key, iElement, isDynamicImage));
                }
            }
            // Vrátí lineární pole, obsahující velikosti (Width / Height) prvků SingleSpan na daném indexu
            List<int> getSingleSizes(IEnumerable<Tuple<int, int>> items)
            {
                var list = items.ToList();
                list.Sort((a, b) => a.Item1.CompareTo(b.Item1));     // Prvky setříděné podle pozice (Item1), vzestupně

                List<int> sizes = new List<int>();
                foreach (var item in list)
                {
                    int itemIndex = item.Item1;                      // Index sloupce nebo řádku (AdressX nebo AdressY)
                    if (itemIndex < 0) continue;
                    int itemSize = item.Item2;                       // Velikost prvku (Width nebo Height)

                    // V poli sizes musím mít prvek na daním indexu [itemIndex]: pokud itemIndex je velké (větší než dosavadní počet prvků 'sizes'), 
                    //  pak musím dolnit řadu prvků v 'sizes' o prvky s hodnotou 0:
                    while (sizes.Count <= itemIndex)
                        sizes.Add(0);

                    // Prvek na indexu [itemIndex] musí mít velikost nejméně danou itemSize:
                    if (sizes[itemIndex] < itemSize)
                        sizes[itemIndex] = itemSize;

                }
                return sizes;
            }
            // Zajistí, že pole velikostí pokryje i daný Span prvek
            void verifySpanSizes(List<int> sizes, IEnumerable<Tuple<int, int, int>> items)
            {
                foreach (var item in items)
                {
                    int itemFirst = item.Item1;                      // Index první dimenze, začátku prvku
                    int itemLast = itemFirst + item.Item2 - 1;       // Index dimenze, kde prvek končí
                    int itemSize = item.Item3;                       // Velikost prvku přes všechny dimenze

                    int sumSize = 0;
                    for (int s = itemFirst; s <= itemLast; s++)
                    {
                        if (s >= sizes.Count)                        // Pokud Span prvku jde až za dosavadní počet prvků, přidám dimenzi s velikostí 0
                            sizes.Add(0);                            // Např. mám Column na adrese X = 4 a ColSpan = 2, a přitom dosud na adrese X = 5 nebyl žádný jednotlivý prvek
                        sumSize += sizes[s];
                    }

                    int addToLast = itemSize - sumSize;
                    if (addToLast > 0)
                        sizes[itemLast] = sizes[itemLast] + addToLast;
                }
            }
            // Vrátí sumární velikost z daných jednotlivých velikostí, počínaje indexem, v počtu span
            int getCellSize(int[] sizes, int index, int span)
            {
                int size = 0;
                int length = sizes.Length;
                for (int o = 0; o < span; o++)
                {
                    int i = index + o;
                    if (i >= 0 && i < length)
                        size += sizes[i];
                }
                return size;
            }
            // Vrátí velikost obrázku podle dostupného prostoru
            ResourceImageSizeType getImageSize(int width, int height)
            {
                int size = (width < height ? width : height);
                if (size <= 18) return ResourceImageSizeType.Small;
                if (size <= 26) return ResourceImageSizeType.Medium;
                return ResourceImageSizeType.Large;
            }
            // Vrátí true, pokud daná definice buňky reprezentuje buňku s dynamicky definovaným obrázkem (ikona, Image)
            bool hasDynamicImage(IListBoxTemplateElement cell)
            {
                return (cell.ElementContent == ElementContentType.IconName || cell.ElementContent == ElementContentType.ImageData);
            }
        }
        /// <summary>
        /// Aplikuje data obsažená v dodaném řádku do prvku šablony v procesu jeho vykreslování.
        /// Typicky se zde načítají jména ikon a vkládají se odpovíající Image do šablony.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="templatedItem"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void ApplyRowDataToTemplateItem(DataRow row, TemplatedItem templatedItem)
        {
            // Najdu buňky, které obsahují proměnný Image:
            var templateCells = __TemplateCells;
            if (templateCells is null || templateCells.Count == 0) return;

            // Projdu elementy jednoho řádku Listu (odpovídají buňkám Cells), a pokud pro element nadju buňku Cell a ta má dynamický Image,
            //  tak do elementu vložím odpovídající Image:
            foreach (TemplatedItemElement element in templatedItem.Elements)
            {
                string key = element.Name;
                if (!String.IsNullOrEmpty(key) && templateCells.TryGetValue(key, out var cell))
                {   // element.Name = __TemplateCells.Key = interní přidělené ID elementu šablony, jednoznačné i přes vícero Templates:
                    if (cell.HasDynamicImage)
                    {
                        bool hasImage = false;
                        var elementContent = cell.Cell.ElementContent;
                        switch (elementContent)
                        {
                            case ElementContentType.IconName:
                                var imageName = cell.GetIconName(row);
                                if (!String.IsNullOrEmpty(imageName))
                                {
                                    element.Image = DxComponent.GetBitmapImage(imageName);
                                    element.ImageOptions.ImageScaleMode = TileItemImageScaleMode.Squeeze;
                                    hasImage = true;
                                }
                                break;
                            case ElementContentType.ImageData:
                                element.Image = cell.GetImage(row);
                                element.ImageOptions.ImageScaleMode = TileItemImageScaleMode.Squeeze;
                                hasImage = true;
                                break;
                        }
                        if (!hasImage)
                        {
                            element.Image = null;
                        }
                    }
                    else
                    {
                    }
                }
            }
        }
        /// <summary>
        /// Úložiště dat o jedné buňce, slouží k propojení mezi ListBoxem a jeho porcesem vykreslování, a definicí buňky <see cref="IListBoxTemplateElement"/>.
        /// </summary>
        private class TemplateCell : IDisposable
        {
            public TemplateCell(string key, IListBoxTemplateElement cell, bool hasDynamicImage)
            {
                this.Key = key;
                this.Cell = cell;
                this.HasDynamicImage = hasDynamicImage;
            }
            public readonly string Key;
            public readonly IListBoxTemplateElement Cell;
            public readonly bool HasDynamicImage;

            /// <summary>
            /// Dispose interních dat, povinný (pokud se používají fotky)
            /// </summary>
            public void Dispose()
            {
                var rowImages = __RowImages;
                if (rowImages != null)
                {
                    foreach (var imageInfo in rowImages.Values)
                    {
                        if (imageInfo != null)
                        {
                            try { imageInfo.Dispose(); }
                            catch { }
                        }
                    }
                    rowImages.Clear();
                    __RowImages = null;
                }
            }
            /// <summary>
            /// Pro daný řádek a tuto buňku vrátí název ikony.
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            public string GetIconName(DataRow row)
            {
                if (row is null || this.Cell.ElementContent != ElementContentType.IconName) return null;
                string columnName = this.Cell.ColumnName;
                if (!String.IsNullOrEmpty(columnName) && row.Table.Columns.Contains(columnName))
                {
                    object value = (!row.IsNull(columnName) ? row[columnName] : null);
                    if (value is string iconName) return iconName;
                }
                return null;
            }
            /// <summary>
            /// Pro daný řádek a tuto buňku vrátí fotku.
            /// Fotka se hledá v interní cache pro zadaný řádek.
            /// Při prvním volání si získá data z řádku pro this sloupec a vygeneruje Image, tu uloží pro daný řádek do interní cache.
            /// Dispose je poté povinný.
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            public Image GetImage(DataRow row)
            {
                if (row is null || this.Cell.ElementContent != ElementContentType.ImageData) return null;

                if (__RowImages is null) __RowImages = new Dictionary<DataRow, TemplateCellImageInfo>();
          
                if (!__RowImages.TryGetValue(row, out var imageInfo))
                {
                    // Redukce položek v cache před přidáním další: když přeteče horní mez (333), redukujeme položky na dolní mez (48):
                    if (__RowImages.Count > 333) _RemoveCache(48);

                    Image image = null;
                    string columnName = this.Cell.ColumnName;
                    if (!String.IsNullOrEmpty(columnName) && row.Table.Columns.Contains(columnName))
                    {
                        object value = (!row.IsNull(columnName) ? row[columnName] : null);
                        if (value != null && value is byte[] imageData)
                        { 
                            try { image = DxComponent.GetBitmapImage(imageData); }
                            catch { /* Binární data obsahují něco, co nelze považovat za Obrázek (např. XML, PDF, ZIP ... vývojáři sem mohou poslat cokoliv)*/ }
                        }
                    }
                    imageInfo = new TemplateCellImageInfo() { Image = image };
                    __RowImages.Add(row, imageInfo);
                }
                __LastUsed++;
                imageInfo.LastUsed = __LastUsed;
                return imageInfo.Image;
            }
            /// <summary>
            /// Odebere z cache nejméně použité položky, ponechá daný počet nejnověji potřebných
            /// </summary>
            private void _RemoveCache(int leaveCount)
            {
                // Pokud není důvod, nic neděláme:
                if (leaveCount < 0) leaveCount = 0;
                if (__RowImages.Count <= leaveCount) return;

                lock (__RowImages)
                {
                    // Všechny položky cache, setřídím LastUsed DESC = na indexu 0 bude nejnověji použitá, na konci budou nejstarší:
                    var images = __RowImages.ToList();
                    images.Sort((a, b) => b.Value.LastUsed.CompareTo(a.Value.LastUsed));

                    // Počínaje indexem [48] disposuji a odebírám položky = to jsou ty starší:
                    for (int i = leaveCount; i < images.Count; i++)
                    {
                        var imageInfoKvp = images[i];
                        imageInfoKvp.Value.Dispose();
                        __RowImages.Remove(imageInfoKvp.Key);
                    }
                }
            }
            /// <summary>
            /// Úložiště Images, per každý řádek.
            /// Key = DataRow = Reference instance; value = odpovídající fotka pro tuto buňku = element
            /// </summary>
            private Dictionary<DataRow, TemplateCellImageInfo> __RowImages;
            /// <summary>
            /// Postupné číslo použití ImageInfo, abychom označili ty nově použité a odlišili je od těch nepotřebných
            /// </summary>
            private long __LastUsed;
        }
        /// <summary>
        /// Jeden záznam v cache: Image, a kdy naposledy byl potřeba. Dispose.
        /// </summary>
        private class TemplateCellImageInfo : IDisposable
        {
            public Image Image;
            public long LastUsed;
            public void Dispose()
            {
                var image = Image;
                if (image != null)
                {
                    try { image.Dispose(); }
                    catch { }
                }
                Image = null;
            }
        }
        /// <summary>
        /// Metoda vytvoří Simple template pro ikonu a pro text
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="columnNameItemId"></param>
        /// <param name="columnNameIcon"></param>
        /// <param name="columnNameText"></param>
        /// <param name="columnNameToolTip"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public static DxListBoxTemplate CreateSimpleDxTemplate(System.Data.DataTable dataTable, string columnNameItemId, string columnNameIcon, string columnNameText, string columnNameToolTip = null, int? iconSize = null)
        {
            if (dataTable is null || dataTable.Columns.Count == 0) return null;

            // Sloupce do Dictionary - kvůli hledání a kvůli ienumerable:
            Dictionary<string, DataColumn> columns = new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn column in dataTable.Columns)
            {
                if (!columns.ContainsKey(column.ColumnName))
                    columns.Add(column.ColumnName, column);
            }

            // Pokud je na vstupu něco, co neexistuje v tabulce, tak to zahodím:
            columnNameItemId = checkColumnName(columnNameItemId);
            columnNameText = checkColumnName(columnNameText);
            columnNameIcon = checkColumnName(columnNameIcon);
            columnNameToolTip = checkColumnName(columnNameToolTip);

            // Pokud nemám zadaný sloupec s ID a s textem, najdu první vhodný sloupec:
            if (columnNameItemId is null) columnNameItemId = columns.Values.FirstOrDefault(c => c.DataType == typeof(int))?.ColumnName;
            if (columnNameText is null) columnNameText = columns.Values.FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;

            // Výsledná deklarace šablony:
            DxListBoxTemplate dxTemplate = new DxListBoxTemplate();
            dxTemplate.ColumnNameItemId = columnNameItemId;
            dxTemplate.ColumnNameToolTipTitle = columnNameText;           // Text zobrazený v Listu bude sloužit i jako titulek pro ToolTip
            dxTemplate.ColumnNameToolTipText = columnNameToolTip;         // Explicitně daný zdroj textu pro ToolTip
            int colIndex = 0;
            int rowHeight = 20;

            if (!String.IsNullOrEmpty(columnNameIcon))
            {
                int size = iconSize ?? 24;
                size = ((size < 20) ? 20 : ((size > 64) ? 64 : size));
                dxTemplate.Elements.Add(new DxListBoxTemplateElement()
                {
                    ColumnName = columnNameIcon,
                    ElementContent = ElementContentType.IconName,
                    ContentAlignment = TileItemContentAlignment.MiddleCenter,
                    RowIndex = 0,
                    ColIndex = colIndex,
                    Width = size + 4,
                    Height = size
                });
                rowHeight = size;
                colIndex++;
            }

            if (!String.IsNullOrEmpty(columnNameText))
            {
                dxTemplate.Elements.Add(new DxListBoxTemplateElement()
                {
                    ColumnName = columnNameText,
                    ElementContent = ElementContentType.Text,
                    ContentAlignment = TileItemContentAlignment.MiddleLeft,
                    RowIndex = 0,
                    ColIndex = colIndex,
                    Width = 300,
                    Height = rowHeight,
                    StretchHorizontal = true
                });
                colIndex++;
            }

            dxTemplate.ColumnNameToolTipText = columnNameToolTip;

            return dxTemplate;

            string checkColumnName(string columnName)
            {
                if (columnName != null && columnName.Trim().Length > 0 && !columns.ContainsKey(columnNameText)) return null;
                return columnName;
            }
        }
    }
    /// <summary>
    /// Definice jedné buňky layoutu. Implementuje <see cref="IListBoxTemplateElement"/>, lze ji vkládat do <see cref="DxListBoxTemplate.Elements"/>.
    /// Jde o prostou schránku na data, nemá funkcionalitu.
    /// </summary>
    public class DxListBoxTemplateElement : IListBoxTemplateElement
    {
        /// <summary>
        /// Konstruktor nastaví defaulty
        /// </summary>
        public DxListBoxTemplateElement()
        {
            ElementContent = ElementContentType.Text;
            Width = null;
            Height = null;
            ColIndex = 0;
            RowIndex = 0;
            RowSpan = 1;
            ColSpan = 1;
            StretchHorizontal = false;
            StretchVertical = false;
            FontSizeRatio = null;
            FontStyle = null;
            ContentAlignment = null;
        }
        /// <summary>
        /// Jednoznačné interní ID buňky. Přiděluje se v procesu tvorby, aplikace na něj nemá vliv a nijak jej nevyužije.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Název šablony. Elementy budou rozgrupovány podle tohoto jména a budou vytvořeny samostatné šablony.
        /// </summary>
        public string TemplateName { get; set; }
        /// <summary>
        /// Jméno sloupce v datech, jehož obsah je zde zobrazen, buď jako Text, nebo Ikona daného jména, nebo jako Image daného obsahu.
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// Způsob práce s obsahem - je možno zobrazit obsah jako Text / Label / Ikonu / Obrázek. Implicitní je Text.
        /// </summary>
        public ElementContentType ElementContent { get; set; }
        /// <summary>
        /// Fixní label = popisek, bude ve všech řádcích stejný. Sloupec tohoto jména tedy nemusí existovat v datové tabulce. Tento element musí mít nastaveno <see cref="ElementContent"/> = <see cref="ElementContentType.Label"/>
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Pozice Y buňky v matici = číslo řádku, počínaje 0.
        /// Pokud má buňka <see cref="RowSpan"/> větší než 1, jde pozici počátku buňky.
        /// </summary>
        public int RowIndex { get; set; }
        /// <summary>
        /// Počet řádků, které buňka překrývá. Default = 1, netřeba zadávat.
        /// </summary>
        public int RowSpan { get; set; }
        /// <summary>
        /// Pozice X buňky v matici = číslo sloupce, počínaje 0.
        /// Pokud má buňka <see cref="ColSpan"/> větší než 1, jde pozici počátku buňky.
        /// </summary>
        public int ColIndex { get; set; }
        /// <summary>
        /// Počet sloupců, které buňka překrývá. Default = 1, netřeba zadávat.
        /// </summary>
        public int ColSpan { get; set; }
        /// <summary>
        /// Šířka buňky v pixelech.
        /// Pokud má buňka <see cref="ColSpan"/> větší než 1, jde o šířku celkovou.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Výška buňky v pixelech.
        /// Pokud má buňka <see cref="RowSpan"/> větší než 1, jde o výšku celkovou.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Šířka buňky = součet ze Span Columns, setuje se při výpočtu
        /// </summary>
        public int CellWidth { get; set; }
        /// <summary>
        /// Výška buňky = součet ze Span Rows, setuje se při výpočtu
        /// </summary>
        public int CellHeight { get; set; }
        /// <summary>
        /// Velikost ikony odvozená od velikosti prostoru, pouze pro <see cref="ElementContent"/> == <see cref="ElementContentType.IconName"/>
        /// </summary>
        public ResourceImageSizeType? ImageSize { get; set; }
        /// <summary>
        /// Příznak, že tento element může být roztažen doprava na celou šířku
        /// </summary>
        public bool StretchHorizontal { get; set; }
        /// <summary>
        /// Příznak, že tento element může být roztažen dolů podle potřeby
        /// </summary>
        public bool StretchVertical { get; set; }
        /// <summary>
        /// Odchylka velikosti fontu od defaultu, null = default.
        /// </summary>
        public float? FontSizeRatio { get; set; }
        /// <summary>
        /// Styl fontu, null = default.
        /// </summary>
        public FontStyle? FontStyle { get; set; }
        /// <summary>
        /// Umístění textu v rámci prostoru buňky, null = default.
        /// </summary>
        public DevExpress.XtraEditors.TileItemContentAlignment? ContentAlignment { get; set; }
    }
    /// <summary>
    /// Deklarace požadavků na definici jedné buňky layoutu v <see cref="DxListBoxTemplate"/>, použité pro definici layoutu v <see cref="DxListBoxControl.DxTemplate"/>
    /// </summary>
    public interface IListBoxTemplateElement
    {
        /// <summary>
        /// Jednoznačné interní ID buňky. Přiděluje se v procesu tvorby, aplikace na něj nemá vliv a nijak jej nevyužije.
        /// </summary>
        string Key { get; set; }
        /// <summary>
        /// Název šablony. Elementy budou rozgrupovány podle tohoto jména a budou vytvořeny samostatné šablony.
        /// </summary>
        string TemplateName { get; }
        /// <summary>
        /// Jméno sloupce v datech, jehož obsah je zde zobrazen, buď jako Text, nebo Ikona daného jména, nebo jako Image daného obsahu.
        /// </summary>
        string ColumnName { get; }
        /// <summary>
        /// Způsob práce s obsahem - je možno zobrazit obsah jako Text / Label / Ikonu / Obrázek. Implicitní je Text.
        /// </summary>
        ElementContentType ElementContent { get; }
        /// <summary>
        /// Fixní label = popisek, bude ve všech řádcích stejný. Sloupec tohoto jména tedy nemusí existovat v datové tabulce. Tento element musí mít nastaveno <see cref="ElementContent"/> = <see cref="ElementContentType.Label"/>
        /// </summary>
        string Label { get; }
        /// <summary>
        /// Pozice Y buňky v matici = číslo řádku, počínaje 0.
        /// Pokud má buňka <see cref="RowSpan"/> větší než 1, jde pozici počátku buňky.
        /// </summary>
        int RowIndex { get; }
        /// <summary>
        /// Počet řádků, které buňka překrývá. Default = 1, netřeba zadávat.
        /// </summary>
        int RowSpan { get; }
        /// <summary>
        /// Pozice X buňky v matici = číslo sloupce, počínaje 0.
        /// Pokud má buňka <see cref="ColSpan"/> větší než 1, jde pozici počátku buňky.
        /// </summary>
        int ColIndex { get; }
        /// <summary>
        /// Počet sloupců, které buňka překrývá. Default = 1, netřeba zadávat.
        /// </summary>
        int ColSpan { get; }
        /// <summary>
        /// Šířka buňky v pixelech.
        /// Pokud má buňka <see cref="ColSpan"/> větší než 1, jde o šířku celkovou.
        /// </summary>
        int? Width { get; }
        /// <summary>
        /// Výška buňky v pixelech.
        /// Pokud má buňka <see cref="RowSpan"/> větší než 1, jde o výšku celkovou.
        /// </summary>
        int? Height { get; }
        /// <summary>
        /// Šířka buňky = součet ze Span Columns, setuje se při výpočtu
        /// </summary>
        int CellWidth { get; set; }
        /// <summary>
        /// Výška buňky = součet ze Span Rows, setuje se při výpočtu
        /// </summary>
        int CellHeight { get; set; }
        /// <summary>
        /// Velikost ikony odvozená od velikosti prostoru, pouze pro <see cref="ElementContent"/> == <see cref="ElementContentType.IconName"/>
        /// </summary>
        ResourceImageSizeType? ImageSize { get; set; }

        /// <summary>
        /// Příznak, že tento element může být roztažen doprava na celou šířku
        /// </summary>
        bool StretchHorizontal { get; }
        /// <summary>
        /// Příznak, že tento element může být roztažen dolů podle potřeby
        /// </summary>
        bool StretchVertical { get; }
        /// <summary>
        /// Odchylka velikosti fontu od defaultu, null = default.
        /// </summary>
        float? FontSizeRatio { get; }
        /// <summary>
        /// Styl fontu, null = default.
        /// </summary>
        FontStyle? FontStyle { get; }
        /// <summary>
        /// Umístění textu v rámci prostoru buňky, null = default.
        /// </summary>
        DevExpress.XtraEditors.TileItemContentAlignment? ContentAlignment { get; }
    }
    /// <summary>
    /// Druh obsahu v daném elementu
    /// </summary>
    public enum ElementContentType
    {
        /// <summary>
        /// Nic
        /// </summary>
        None,
        /// <summary>
        /// Fixní Label, definovaný v elementu
        /// </summary>
        Label,
        /// <summary>
        /// Text
        /// </summary>
        Text,
        /// <summary>
        /// Název ikony
        /// </summary>
        IconName,
        /// <summary>
        /// Binární data obrázku
        /// </summary>
        ImageData,

        /// <summary>
        /// Default = Text
        /// </summary>
        Default = Text
    }
    #endregion
    #region Event args + delegáti, public enumy
    /// <summary>
    /// Režim položek
    /// </summary>
    public enum ListBoxItemsMode
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Jednotlivé položky, <see cref="IMenuItem"/>, pole <see cref="DxListBoxControl.MenuItems"/>.
        /// Podporuje vykreslování ikon.
        /// </summary>
        MenuItems,
        /// <summary>
        /// Datová tabulka
        /// </summary>
        Table
    }
    /// <summary>
    /// Argumenty pro akci na <see cref="DxListBoxControl"/>
    /// </summary>
    public class DxListBoxActionEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="action">Probíhající akce</param>
        /// <param name="keys">Stisknutá klávesa, může být null</param>
        public DxListBoxActionEventArgs(ControlKeyActionType action, KeyEventArgs keys)
        {
            this.Action = action;
            this.Keys = keys;
        }
        /// <summary>
        /// Probíhající akce
        /// </summary>
        public ControlKeyActionType Action { get; }
        /// <summary>
        /// Stisknutá klávesa, může být null
        /// </summary>
        public KeyEventArgs Keys { get; }
    }
    /// <summary>
    /// Delegát pro událost Akce na <see cref="DxListBoxControl"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DxListBoxActionDelegate(object sender, DxListBoxActionEventArgs e);

    /// <summary>
    /// Argumenty pro akci Before na <see cref="DxListBoxControl"/>, kdy je možnost dát <see cref="Cancel"/> = true;
    /// </summary>
    public class DxListBoxActionCancelEventArgs : DxListBoxActionEventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="action">Probíhající akce</param>
        /// <param name="keys">Stisknutá klávesa, může být null</param>
        public DxListBoxActionCancelEventArgs(ControlKeyActionType action, KeyEventArgs e)
            : base(action, e)
        {
            Cancel = false;
        }
        /// <summary>
        /// Nastavením na true bude akce stornována
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// Delegát pro událost Akce Before na <see cref="DxListBoxControl"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DxListBoxActionCancelDelegate(object sender, DxListBoxActionCancelEventArgs e);

    /// <summary>
    /// Argumenty pro akci ItemMouseClick a ItemMouseDoubleClick
    /// </summary>
    public class DxListBoxItemMouseClickEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="location"></param>
        /// <param name="modifierKeys"></param>
        /// <param name="itemId"></param>
        public DxListBoxItemMouseClickEventArgs(MouseButtons buttons, Point location, Keys modifierKeys, object itemId)
        {
            this.Buttons = buttons;
            this.Location = location;
            this.ModifierKeys = modifierKeys;
            this.ItemId = itemId;
        }
        /// <summary>
        /// Tlačítko myši
        /// </summary>
        public MouseButtons Buttons { get; }
        /// <summary>
        /// Pozice myši
        /// </summary>
        public Point Location { get; }
        /// <summary>
        /// Modifikátorové klávesy (Ctrl, Shift, Alt)
        /// </summary>
        public Keys ModifierKeys { get; }
        /// <summary>
        /// ID prvku pod myší
        /// </summary>
        public object ItemId { get; }
    }
    /// <summary>
    /// Handler pro akci ItemMouseClick a ItemMouseDoubleClick
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxListBoxItemMouseClickDelegate(object sender, DxListBoxItemMouseClickEventArgs args);

    /// <summary>
    /// Argumenty pro akci Key
    /// </summary>
    public class DxListBoxItemKeyEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="keyArgs"></param>
        /// <param name="itemId"></param>
        public DxListBoxItemKeyEventArgs(KeyEventArgs keyArgs, object itemId)
        {
            this.KeyArgs = keyArgs;
            this.ItemId = itemId;
        }
        /// <summary>
        /// Data o KeyPress
        /// </summary>
        public KeyEventArgs KeyArgs { get; }
        /// <summary>
        /// ID prvku pod myší
        /// </summary>
        public object ItemId { get; }
    }
    /// <summary>
    /// Handler pro akci Key
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxListBoxItemKeyDelegate(object sender, DxListBoxItemKeyEventArgs args);
    #endregion
}