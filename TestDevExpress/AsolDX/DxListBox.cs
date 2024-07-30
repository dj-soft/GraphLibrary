// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;

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
            __ButtonsTypes = ListBoxButtonType.None;
            __ButtonsSize = ResourceImageSizeType.Medium;
            this.Controls.Add(__ListBox);
            this.Padding = new Padding(0);
            this.ClientSizeChanged += _ClientSizeChanged;
            __ListBox.ListItemsChanged += __ListBox_ListItemsChanged;
            __ListBox.UndoRedoEnabled = false;
            __ListBox.UndoRedoEnabledChanged += _ListBox_UndoRedoEnabledChanged;
            __ListBox.SelectedItemsChanged += _ListBox_SelectedItemsChanged;
            __ListBox.SelectedMenuItemChanged += _ListBox_SelectedMenuItemChanged;
            __ListBox.ListActionBefore += _RunListActionBefore;
            __ListBox.ListActionAfter += _RunListActionAfter;
            _RowFilterInitialize();
            DoLayout();
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
        /// Dá Focus do main controlu
        /// </summary>
        private void _MainControlFocus()
        {
            __ListBox.Focus();
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _RemoveButtons();
            _RowFilterDispose();
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
        public ListBoxButtonType ButtonsTypes { get { return __ButtonsTypes; } set { __ButtonsTypes = value; _AcceptButtonsType(); DoLayout(); } }
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
        public IMenuItem[] ListItems { get { return __ListBox.ListItems; } set { __ListBox.ListItems = value; } }
        /// <summary>
        /// Aktuálně vybraný prvek typu <see cref="IMenuItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
        /// </summary>
        public IMenuItem SelectedListItem { get { return __ListBox.SelectedListItem; } set { __ListBox.SelectedListItem = value; } }
        /// <summary>
        /// Prvky Listu
        /// </summary>
        public DevExpress.XtraEditors.Controls.ImageListBoxItemCollection Items { get { return __ListBox.Items; } }
        #endregion
        #region Komplexní List postavený nad DataTable a Template

        #endregion

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
        /// Povolené akce. Výchozí je <see cref="KeyActionType.None"/>
        /// </summary>
        public KeyActionType EnabledKeyActions { get { return __ListBox.EnabledKeyActions; } set { __ListBox.EnabledKeyActions = value; } }

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
        /// <param name="e"></param>
        private void RunSelectedItemsChanged(TEventArgs<object> e)
        {
            OnSelectedItemsChanged(e);
            SelectedItemsChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se při změně Selected prvku, libovolného typu
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectedItemsChanged(TEventArgs<object> e) { }
        /// <summary>
        /// Událost, kdy v <see cref="ListBox"/> je vybrán nějaký konkrétní prvek.
        /// </summary>
        public event EventHandler<TEventArgs<object>> SelectedItemsChanged;

        /// <summary>
        /// Provede se když v <see cref="ListBox"/> je vybrán nějaký prvek typu <see cref="IMenuItem"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedMenuItemChanged(object sender, TEventArgs<IMenuItem> e)
        {
            OnSelectedMenuItemChanged(e);
            SelectedMenuItemChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se při změně Selected prvku typu typu <see cref="IMenuItem"/>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectedMenuItemChanged(TEventArgs<IMenuItem> e) { }
        /// <summary>
        /// Událost, kdy v <see cref="ListBox"/> je vybrán nějaký prvek typu <see cref="IMenuItem"/>
        /// </summary>
        public event EventHandler<TEventArgs<IMenuItem>> SelectedMenuItemChanged;
        #endregion
        #region Filtrování položek: klientské / serverové
        /// <summary>
        /// Inicializace řádkového filtrování
        /// </summary>
        private void _RowFilterInitialize()
        {
            __RowFilterMode = FilterRowMode.None;
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
            if (filterMode != FilterRowMode.Client && _RowFilterClientExists)
                _RowFilterClientRemove();
            if (filterMode != FilterRowMode.Server && _RowFilterServerExists)
                _RowFilterServerRemove();

            if (filterMode == FilterRowMode.Client && !_RowFilterClientExists)
                _RowFilterClientPrepare();
            if (filterMode == FilterRowMode.Server && !_RowFilterServerExists)
                _RowFilterServerPrepare();

            if (filterMode != __RowFilterMode)
            {
                __RowFilterMode = filterMode;
                this.RunInGui(_ShowRowFilterBox);
            }
        }
        /// <summary>
        /// Nastaví viditelnost pro aktuální FilterRow a upraví celkový Layout aby byl správně umístěn
        /// </summary>
        private void _ShowRowFilterBox()
        {
            var filterMode = __RowFilterMode;
            if (filterMode == FilterRowMode.Client && _RowFilterClientExists)
            {
                __RowFilterClient.Visible = true;
                DoLayout();
            }
            else if (filterMode == FilterRowMode.Server && _RowFilterServerExists)
            {
                __RowFilterServer.Visible = true;
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
            __RowFilterClient.Properties.NullValuePrompt = "Co byste chtěli najít?";
            _RowFilterClientRegisterEvents();
            this.Controls.Add(__RowFilterClient);
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
        }
        /// <summary>
        /// Odregistruje zdejší eventhandlery na události v nativním <see cref="__RowFilterClient"/>
        /// </summary>
        private void _RowFilterClientUnRegisterEvents()
        {
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
            ListBoxButtonType group1 = ListBoxButtonType.MoveTop | ListBoxButtonType.MoveUp | ListBoxButtonType.MoveDown | ListBoxButtonType.MoveBottom;
            ListBoxButtonType group2 = ListBoxButtonType.Refresh | ListBoxButtonType.SelectAll | ListBoxButtonType.Delete;
            ListBoxButtonType group3 = ListBoxButtonType.ClipCopy | ListBoxButtonType.ClipCut | ListBoxButtonType.ClipPaste;
            int currentGroup = 0;
            for (int b = 0; b < buttons.Count; b++)
            {
                var button = buttons[b];

                // Zkusíme oddělit jednotlivé grupy od sebe:
                ListBoxButtonType buttonType = ((button.Tag is ListBoxButtonType) ? ((ListBoxButtonType)button.Tag) : ListBoxButtonType.None);
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
            ListBoxButtonType validButtonsTypes = __ButtonsTypes;

            // Buttony z _ButtonsType převedu na povolené akce v ListBoxu a sloučím s akcemi dosud povolenými:
            KeyActionType oldActions = __ListBox.EnabledKeyActions;
            KeyActionType newActions = ConvertButtonsToActions(validButtonsTypes);
            __ListBox.EnabledKeyActions = (newActions | oldActions);

            // Odstraním stávající buttony:
            _RemoveButtons(true);

            // Vytvořím potřebné buttony:
            //   (vytvoří se jen ty buttony, které jsou vyžádané proměnné buttonsTypes, fyzické pořadí buttonů je dané pořadím těchto řádků)
            _AcceptButtonType(ListBoxButtonType.MoveTop, validButtonsTypes, "@arrowsmall|top|blue", MsgCode.DxKeyActionMoveTopTitle, MsgCode.DxKeyActionMoveTopText);
            _AcceptButtonType(ListBoxButtonType.MoveUp, validButtonsTypes, "@arrowsmall|up|blue", MsgCode.DxKeyActionMoveUpTitle, MsgCode.DxKeyActionMoveUpText);
            _AcceptButtonType(ListBoxButtonType.MoveDown, validButtonsTypes, "@arrowsmall|down|blue", MsgCode.DxKeyActionMoveDownTitle, MsgCode.DxKeyActionMoveDownText);
            _AcceptButtonType(ListBoxButtonType.MoveBottom, validButtonsTypes, "@arrowsmall|bottom|blue", MsgCode.DxKeyActionMoveBottomTitle, MsgCode.DxKeyActionMoveBottomText);
            _AcceptButtonType(ListBoxButtonType.Refresh, validButtonsTypes, "devav/actions/refresh.svg", MsgCode.DxKeyActionRefreshTitle, MsgCode.DxKeyActionRefreshText);   // qqq
            _AcceptButtonType(ListBoxButtonType.SelectAll, validButtonsTypes, "@editsmall|all|blue", MsgCode.DxKeyActionSelectAllTitle, MsgCode.DxKeyActionSelectAllText);
            _AcceptButtonType(ListBoxButtonType.Delete, validButtonsTypes, "@editsmall|del|red", MsgCode.DxKeyActionDeleteTitle, MsgCode.DxKeyActionDeleteText);       // "devav/actions/delete.svg"
            _AcceptButtonType(ListBoxButtonType.ClipCopy, validButtonsTypes, "devav/actions/copy.svg", MsgCode.DxKeyActionClipCopyTitle, MsgCode.DxKeyActionClipCopyText);
            _AcceptButtonType(ListBoxButtonType.ClipCut, validButtonsTypes, "devav/actions/cut.svg", MsgCode.DxKeyActionClipCutTitle, MsgCode.DxKeyActionClipCutText);
            _AcceptButtonType(ListBoxButtonType.ClipPaste, validButtonsTypes, "devav/actions/paste.svg", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);

            _AcceptButtonType(ListBoxButtonType.CopyToRightOne, validButtonsTypes, "@arrowsmall|right|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ListBoxButtonType.CopyToRightAll, validButtonsTypes, "@arrow|right|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ListBoxButtonType.CopyToLeftOne, validButtonsTypes, "@arrowsmall|left|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            _AcceptButtonType(ListBoxButtonType.CopyToLeftAll, validButtonsTypes, "@arrow|left|blue", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);

            _AcceptButtonType(ListBoxButtonType.Undo, validButtonsTypes, "svgimages/dashboards/undo.svg", MsgCode.DxKeyActionUndoTitle, MsgCode.DxKeyActionUndoText);
            _AcceptButtonType(ListBoxButtonType.Redo, validButtonsTypes, "svgimages/dashboards/redo.svg", MsgCode.DxKeyActionRedoTitle, MsgCode.DxKeyActionRedoText);

            // Pokud bylo povoleno UndoRedo, pak povolím i odpovídající funkcionalitu:
            if ((newActions.HasFlag(KeyActionType.Undo) || newActions.HasFlag(KeyActionType.Redo)) || !this.UndoRedoEnabled)
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
        private void _AcceptButtonType(ListBoxButtonType buttonType, ListBoxButtonType validButtonsTypes, string imageName, MsgCode msgToolTipTitle, MsgCode msgToolTipText)
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
        private void _ListBox_SelectedItemsChanged(object sender, TEventArgs<object> e)
        {
            _SetButtonsEnabledSelection();
            RunSelectedItemsChanged(e);
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
            _SetButtonEnabled(ListBoxButtonType.Undo, (undoRedoEnabled && this.UndoRedoController.UndoEnabled));
            _SetButtonEnabled(ListBoxButtonType.Redo, (undoRedoEnabled && this.UndoRedoController.RedoEnabled));
        }
        /// <summary>
        /// Nastaví Enabled buttonů typu OnSelected
        /// </summary>
        private void _SetButtonsEnabledSelection()
        {
            int selectedCount = this.__ListBox.SelectedIndices.Count;
            int totalCount = this.__ListBox.ItemCount;

            bool isAnySelected = selectedCount > 0;
            _SetButtonEnabled(ListBoxButtonType.ClipCopy, isAnySelected);
            _SetButtonEnabled(ListBoxButtonType.ClipCut, isAnySelected);
            _SetButtonEnabled(ListBoxButtonType.Delete, isAnySelected);

            bool canMove = (selectedCount > 0 && selectedCount < totalCount);
            _SetButtonEnabled(ListBoxButtonType.MoveTop, canMove);
            _SetButtonEnabled(ListBoxButtonType.MoveUp, canMove);
            _SetButtonEnabled(ListBoxButtonType.MoveDown, canMove);
            _SetButtonEnabled(ListBoxButtonType.MoveBottom, canMove);

            bool canSelectAll = (totalCount > 0 && selectedCount < totalCount);
            _SetButtonEnabled(ListBoxButtonType.SelectAll, canSelectAll);
        }
        /// <summary>
        /// Nastaví do daného buttonu stav enabled
        /// </summary>
        /// <param name="buttonType"></param>
        /// <param name="enabled"></param>
        private void _SetButtonEnabled(ListBoxButtonType buttonType, bool enabled)
        {
            if (__Buttons.TryGetFirst(b => b.Tag is ListBoxButtonType bt && bt == buttonType, out var button))
                button.Enabled = enabled;
        }
        /// <summary>
        /// Provede akci danou buttonem <paramref name="sender"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ButtonClick(object sender, EventArgs args)
        {
            if (sender is DxSimpleButton dxButton && dxButton.Tag is ListBoxButtonType buttonType)
            {
                KeyActionType action = ConvertButtonsToActions(buttonType);
                __ListBox.DoKeyActions(action);
            }
        }
        /// <summary>
        /// Konvertuje hodnoty z typu <see cref="ListBoxButtonType"/> na hodnoty typu <see cref="KeyActionType"/>
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
        public static KeyActionType ConvertButtonsToActions(ListBoxButtonType buttons)
        {
            KeyActionType actions =
                (buttons.HasFlag(ListBoxButtonType.ClipCopy) ? KeyActionType.ClipCopy : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.ClipCut) ? KeyActionType.ClipCut : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.ClipPaste) ? KeyActionType.ClipPaste : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.Delete) ? KeyActionType.Delete : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.Refresh) ? KeyActionType.Refresh: KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.SelectAll) ? KeyActionType.SelectAll : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.GoBegin) ? KeyActionType.GoBegin : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.GoEnd) ? KeyActionType.GoEnd : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveTop) ? KeyActionType.MoveTop : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveUp) ? KeyActionType.MoveUp : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveDown) ? KeyActionType.MoveDown : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveBottom) ? KeyActionType.MoveBottom : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.CopyToRightOne) ? KeyActionType.CopyToRightOne : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.CopyToRightAll) ? KeyActionType.CopyToRightAll : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.CopyToLeftOne) ? KeyActionType.CopyToLeftOne : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.CopyToLeftAll) ? KeyActionType.CopyToLeftAll : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.Undo) ? KeyActionType.Undo : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.Redo) ? KeyActionType.Redo : KeyActionType.None);
            return actions;
        }
        /// <summary>
        /// Konvertuje hodnoty z typu <see cref="ListBoxButtonType"/> na hodnoty typu <see cref="KeyActionType"/>
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static ListBoxButtonType ConvertActionsToButtons(KeyActionType actions)
        {
            ListBoxButtonType buttons =
                (actions.HasFlag(KeyActionType.ClipCopy) ? ListBoxButtonType.ClipCopy : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.ClipCut) ? ListBoxButtonType.ClipCut : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.ClipPaste) ? ListBoxButtonType.ClipPaste : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.Delete) ? ListBoxButtonType.Delete : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.Refresh) ? ListBoxButtonType.Refresh : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.SelectAll) ? ListBoxButtonType.SelectAll : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.GoBegin) ? ListBoxButtonType.GoBegin : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.GoEnd) ? ListBoxButtonType.GoEnd : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.MoveTop) ? ListBoxButtonType.MoveTop : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.MoveUp) ? ListBoxButtonType.MoveUp : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.MoveDown) ? ListBoxButtonType.MoveDown : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.MoveBottom) ? ListBoxButtonType.MoveBottom : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.Undo) ? ListBoxButtonType.Undo : ListBoxButtonType.None) |
                (actions.HasFlag(KeyActionType.Redo) ? ListBoxButtonType.Redo : ListBoxButtonType.None);
            return buttons;
        }
        /// <summary>
        /// Typy dostupných tlačítek
        /// </summary>
        private ListBoxButtonType __ButtonsTypes;
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
    #region enum ListBoxButtonType : Typy tlačítek dostupných u Listboxu pro jeho ovládání (vnitřní příkazy, nikoli Drag and Drop)
    /// <summary>
    /// Typy tlačítek dostupných u Listboxu pro jeho ovládání (vnitřní příkazy, nikoli Drag and Drop)
    /// </summary>
    [Flags]
    public enum ListBoxButtonType
    {
        /// <summary>
        /// Žádný button
        /// </summary>
        None = 0,

        /// <summary>
        /// Copy: Zkopírovat do schránky
        /// </summary>
        ClipCopy = 0x0001,
        /// <summary>
        /// Cut: Zkopírovat do schránky a smazat
        /// </summary>
        ClipCut = 0x0002,
        /// <summary>
        /// Paste: Vložit ze schránky
        /// </summary>
        ClipPaste = 0x0004,
        /// <summary>
        /// Smazat vybrané
        /// </summary>
        Delete = 0x0008,

        /// <summary>
        /// Akce Refresh
        /// </summary>
        Refresh = 0x0010,
        /// <summary>
        /// Vybrat vše
        /// </summary>
        SelectAll = 0x0020,
        /// <summary>
        /// Přejdi na začátek
        /// </summary>
        GoBegin = 0x0040,
        /// <summary>
        /// Přejdi na konec
        /// </summary>
        GoEnd = 0x0080,

        /// <summary>
        /// Přemístit úplně nahoru
        /// </summary>
        MoveTop = 0x0100,
        /// <summary>
        /// Přemístit o 1 nahoru
        /// </summary>
        MoveUp = 0x0200,
        /// <summary>
        /// Přemístit o 1 dolů
        /// </summary>
        MoveDown = 0x0400,
        /// <summary>
        /// Přemístit úplně dolů
        /// </summary>
        MoveBottom = 0x0800,

        /// <summary>
        /// Akce UNDO
        /// </summary>
        Undo = 0x1000,
        /// <summary>
        /// Akce REDO
        /// </summary>
        Redo = 0x2000,

        /// <summary>
        /// Kopírovat prvek / vybrané prvky zleva doprava
        /// </summary>
        CopyToRightOne = 0x00010000,
        /// <summary>
        /// Kopírovat všechny prvky zleva doprava
        /// </summary>
        CopyToRightAll = 0x00020000,
        /// <summary>
        /// Kopírovat prvek / vybrané prvky zprava doleva
        /// </summary>
        CopyToLeftOne = 0x00040000,
        /// <summary>
        /// Kopírovat všechny prvky zprava doleva
        /// </summary>
        CopyToLeftAll = 0x00080000,

        /// <summary>
        /// Souhrn všech pohybů
        /// </summary>
        MoveAll = MoveTop | MoveUp | MoveDown | MoveBottom,
        /// <summary>
        /// Všechny kopie doleva/doprava
        /// </summary>
        CopyAll = CopyToRightOne | CopyToRightAll | CopyToLeftOne | CopyToLeftAll,

        /// <summary>
        /// Souhrn Undo + Redo
        /// </summary>
        UndoRedo = Undo | Redo
    }
    #endregion
    /// <summary>
    /// ListBoxControl s podporou pro drag and drop a reorder
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.ImageListBoxControl, IDxDragDropControl, IUndoRedoControl   // původně :ListBoxControl, nyní: https://docs.devexpress.com/WindowsForms/DevExpress.XtraEditors.ImageListBoxControl
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            _KeyActionsInit();
            _DataExchangeInit();
            _DxDragDropInit(DxDragDropActionType.None);
            _ToolTipInit();
            _ImageInit();
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
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DxDragDropDispose();
            ToolTipDispose();
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
        public IMenuItem[] ListItems
        {
            get
            {
                return this.Items.Select(i => i.Value as IMenuItem).ToArray();
            }
            set
            {
                this.DataSource = null;

                var validItems = _GetOnlyValidItems(value, false);
                this.Items.Clear();
                this.Items.AddRange(validItems);

                __ItemsMode = ListBoxItemsMode.MenuItems;
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
                __ItemsMode = ListBoxItemsMode.MenuItems;
            _RunItemsListChanged(e);
        }
        /// <summary>
        /// Zavolá akce po změně v poli <see cref="ListItems"/>
        /// </summary>
        /// <param name="e"></param>
        private void _RunItemsListChanged(System.ComponentModel.ListChangedEventArgs e)
        {
            OnListItemsChanged(e);
            ListItemsChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po změně v poli <see cref="ListItems"/>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnListItemsChanged(System.ComponentModel.ListChangedEventArgs e) { }
        /// <summary>
        /// Proběhne po změně v poli <see cref="ListItems"/>
        /// </summary>
        public event System.ComponentModel.ListChangedEventHandler ListItemsChanged;

        /// <summary>
        /// Aktuálně vybraný prvek typu <see cref="IMenuItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
        /// </summary>
        public IMenuItem SelectedListItem
        {
            get
            {   // Vrátím IMenuItem nalezený v aktuálně vybraném prvku:
                return ((__ItemsMode == ListBoxItemsMode.MenuItems && this.Items.Count > 0 && _TryFindListItem(this.SelectedItem, out var menuItem)) ? menuItem : null);
            }
            set
            {   // Najdu první prvek zdejšího pole, který v sobě obsahuje IMenuItem, který je identický s dodanou value:
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    object selectedItem = null;
                    if (this.Items.Count > 0 && value != null)
                        selectedItem = this.Items.FirstOrDefault(i => (_TryFindListItem(i, out var iMenuItem) && Object.ReferenceEquals(iMenuItem, value)));
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
        private bool _TryFindListItem(object item, out IMenuItem menuItem)
        {
            if (__ItemsMode == ListBoxItemsMode.MenuItems && item is not null)
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
        public Tuple<int, IMenuItem, Rectangle>[] VisibleItems
        {
            get
            {
                List<Tuple<int, IMenuItem, Rectangle>> visibleItems = null;
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    visibleItems = new List<Tuple<int, IMenuItem, Rectangle>>();
                    var listItems = this.ListItems;
                    int topIndex = this.TopIndex;
                    int index = (topIndex > 0 ? topIndex - 1 : topIndex);
                    int count = this.ItemCount;
                    while (index < count)
                    {
                        Rectangle? bounds = GetItemBounds(index);
                        if (bounds.HasValue)
                            visibleItems.Add(new Tuple<int, IMenuItem, Rectangle>(index, listItems[index], bounds.Value));
                        else if (index > topIndex)
                            break;
                        index++;
                    }
                }
                return visibleItems.ToArray();
            }
        }
        /// <summary>
        /// Pole, obsahující informace o právě selectovaných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle?>[] SelectedItemsInfo
        {
            get
            {
                List<Tuple<int, IMenuItem, Rectangle?>> selectedItemsInfo = null;
                if (__ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    var listItems = this.ListItems;
                    selectedItemsInfo = new List<Tuple<int, IMenuItem, Rectangle?>>();
                    foreach (var index in this.SelectedIndices)
                    {
                        Rectangle? bounds = GetItemBounds(index);
                        selectedItemsInfo.Add(new Tuple<int, IMenuItem, Rectangle?>(index, listItems[index], bounds));
                    }
                }
                return selectedItemsInfo.ToArray();
            }
        }
        /// <summary>
        /// Obsahuje pole prvků, které jsou aktuálně Selected. 
        /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
        /// </summary>
        public new IEnumerable<IMenuItem> SelectedItems
        {
            get
            {
                var listItems = this.ListItems;
                var selectedItems = new List<IMenuItem>();
                foreach (var index in this.SelectedIndices)
                    selectedItems.Add(listItems[index]);
                return selectedItems.ToArray();
            }
            set
            {
                var selectedItems = (value?.ToList() ?? new List<IMenuItem>());
                var listItems = this.ListItems;
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

        #endregion
        /// <summary>
        /// Aktuální režim položek
        /// </summary>
        private ListBoxItemsMode __ItemsMode;
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
            /// Jednotlivé položky, <see cref="IMenuItem"/>, pole <see cref="ListItems"/>.
            /// Podporuje vykreslování ikon.
            /// </summary>
            MenuItems,
            /// <summary>
            /// Datová tabulka
            /// </summary>
            Table
        }
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
            if (e.Item is IMenuItem iMenuItem && iMenuItem.FontStyle.HasValue)
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

            TEventArgs<object> args = new TEventArgs<object>(this.SelectedItem);
            RunSelectedItemsChanged(args);

            _DetectSelectedMenuItemChanged();
        }
        /// <summary>
        /// Detekuje zda aktuálně vybraný prvek obsahuje jiný <see cref="IMenuItem"/>, než který byl posledně oznámen
        /// </summary>
        private void _DetectSelectedMenuItemChanged()
        {
            var selectedMenuItem = this.SelectedListItem;
            if (!Object.ReferenceEquals(selectedMenuItem, _LastSelectedItem))
            {
                this.RunSelectedMenuItemChanged(new TEventArgs<IMenuItem>(selectedMenuItem));
                _LastSelectedItem = selectedMenuItem;
            }
        }
        /// <summary>
        /// Objekt, který byl naposledy předán do metody <see cref="RunSelectedMenuItemChanged(TEventArgs{IMenuItem})"/>.
        /// Poku dbude napříště vybrán jiný objekt, bude předán i ten další.
        /// </summary>
        private IMenuItem _LastSelectedItem;
        #endregion
        #region Images
        /// <summary>
        /// Inicializace pro Images
        /// </summary>
        protected virtual void _ImageInit()
        {
            this.MeasureItem += _MeasureItem;
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
            var menuItem = this.ListItems[index];
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
        #region ToolTip
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy?
        /// </summary>
        public bool ToolTipAllowHtmlText { get; set; }
        private void _ToolTipInit()
        {
            this.ToolTipController = DxComponent.CreateNewToolTipController();
            this.ToolTipController.GetActiveObjectInfo += ToolTipController_GetActiveObjectInfo;
        }
        private void ToolTipDispose()
        {
            this.ToolTipController?.Dispose();
        }
        /// <summary>
        /// Připraví ToolTip pro aktuální Node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTipController_GetActiveObjectInfo(object sender, ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
            if (e.SelectedControl is DxListBoxControl listBox)
            {
                int index = listBox.IndexFromPoint(e.ControlMousePosition);
                if (index != -1 && index < listBox.ListItems.Length)
                {
                    var menuItem = listBox.ListItems[index];
                    if (menuItem != null)
                    {
                        string toolTipText = menuItem.ToolTipText;
                        string toolTipTitle = menuItem.ToolTipTitle ?? menuItem.Text;
                        var ttci = new DevExpress.Utils.ToolTipControlInfo(menuItem, toolTipText, toolTipTitle);
                        ttci.ToolTipType = ToolTipType.SuperTip;
                        ttci.AllowHtmlText = (ToolTipAllowHtmlText ? DefaultBoolean.True : DefaultBoolean.False);
                        e.Info = ttci;
                    }
                }
            }
        }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="title">Titulek</param>
        /// <param name="text">Text</param>
        /// <param name="defaultTitle">Náhradní titulek, použije se když je zadán text ale není zadán titulek</param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
        #region DoKeyActions; CtrlA, CtrlC, CtrlX, CtrlV, Delete; Move, Insert, Remove
        /// <summary>
        /// Povolené akce. Výchozí je <see cref="KeyActionType.None"/>
        /// </summary>
        public KeyActionType EnabledKeyActions { get; set; }
        /// <summary>
        /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="KeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
        /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
        /// </summary>
        /// <param name="actions"></param>
        public void DoKeyActions(params KeyActionType[] actions)
        {
            foreach (KeyActionType action in actions)
                _DoKeyAction(action, true);
        }
        /// <summary>
        /// Inicializace eventhandlerů a hodnot pro KeyActions
        /// </summary>
        private void _KeyActionsInit()
        {
            this.KeyDown += DxListBoxControl_KeyDown;
            this.EnabledKeyActions = KeyActionType.None;
        }
        /// <summary>
        /// Obsluha kláves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxListBoxControl_KeyDown(object sender, KeyEventArgs e)
        {
            var enabledActions = EnabledKeyActions;
            bool isHandled = false;
            switch (e.KeyData)
            {
                case Keys.Delete:
                    isHandled = _DoKeyAction(KeyActionType.Delete);
                    break;
                case Keys.Control | Keys.A:
                    isHandled = _DoKeyAction(KeyActionType.SelectAll);
                    break;
                case Keys.Control | Keys.C:
                    isHandled = _DoKeyAction(KeyActionType.ClipCopy);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.X:
                    // Ctrl+X : pokud je povoleno, provedu; pokud nelze provést Ctrl+X ale lze provést Ctrl+C, tak se provede to:
                    if (EnabledKeyActions.HasFlag(KeyActionType.ClipCut))
                        isHandled = _DoKeyAction(KeyActionType.ClipCut);
                    else if (EnabledKeyActions.HasFlag(KeyActionType.ClipCopy))
                        isHandled = _DoKeyAction(KeyActionType.ClipCopy);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.V:
                    isHandled = _DoKeyAction(KeyActionType.ClipPaste);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Alt | Keys.Home:
                    isHandled = _DoKeyAction(KeyActionType.MoveTop);
                    break;
                case Keys.Alt | Keys.Up:
                    isHandled = _DoKeyAction(KeyActionType.MoveUp);
                    break;
                case Keys.Alt | Keys.Down:
                    isHandled = _DoKeyAction(KeyActionType.MoveDown);
                    break;
                case Keys.Alt | Keys.End:
                    isHandled = _DoKeyAction(KeyActionType.MoveBottom);
                    break;
                case Keys.Control | Keys.Z:
                    isHandled = _DoKeyAction(KeyActionType.Undo);
                    break;
                case Keys.Control | Keys.Y:
                    isHandled = _DoKeyAction(KeyActionType.Redo);
                    break;
            }
            if (isHandled)
                e.Handled = true; 
        }
        /// <summary>
        /// Provede akce zadané jako bity v dané akci (<paramref name="action"/>), s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="force"></param>
        private bool _DoKeyAction(KeyActionType action, bool force = false)
        {
            bool handled = false;
            _DoKeyAction(action, KeyActionType.Refresh, force, null, ref handled);
            _DoKeyAction(action, KeyActionType.SelectAll, force, _DoKeyActionCtrlA, ref handled);
            _DoKeyAction(action, KeyActionType.ClipCopy, force, _DoKeyActionCtrlC, ref handled);
            _DoKeyAction(action, KeyActionType.ClipCut, force, _DoKeyActionCtrlX, ref handled);
            _DoKeyAction(action, KeyActionType.ClipPaste, force, _DoKeyActionCtrlV, ref handled);
            _DoKeyAction(action, KeyActionType.MoveTop, force, _DoKeyActionMoveTop, ref handled);
            _DoKeyAction(action, KeyActionType.MoveUp, force, _DoKeyActionMoveUp, ref handled);
            _DoKeyAction(action, KeyActionType.MoveDown, force, _DoKeyActionMoveDown, ref handled);
            _DoKeyAction(action, KeyActionType.MoveBottom, force, _DoKeyActionMoveBottom, ref handled);
            _DoKeyAction(action, KeyActionType.Delete, force, _DoKeyActionDelete, ref handled);
            _DoKeyAction(action, KeyActionType.CopyToRightOne, force, null, ref handled);
            _DoKeyAction(action, KeyActionType.CopyToRightAll, force, null, ref handled);
            _DoKeyAction(action, KeyActionType.CopyToLeftOne, force, null, ref handled);
            _DoKeyAction(action, KeyActionType.CopyToLeftAll, force, null, ref handled);
            _DoKeyAction(action, KeyActionType.Undo, force, _DoKeyActionUndo, ref handled);
            _DoKeyAction(action, KeyActionType.Redo, force, _DoKeyActionRedo, ref handled);
            return handled;
        }
        /// <summary>
        /// Pokud v soupisu akcí <paramref name="action"/> je příznak akce <paramref name="flag"/>, pak provede danou akci <paramref name="internalActionMethod"/>, 
        /// s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="flag"></param>
        /// <param name="force"></param>
        /// <param name="internalActionMethod"></param>
        /// <param name="handled">Nastaví na true, pokud byla provedena požadovaná akce</param>
        private void _DoKeyAction(KeyActionType action, KeyActionType flag, bool force, Action internalActionMethod, ref bool handled)
        {
            if (!action.HasFlag(flag)) return;
            if (!force && !EnabledKeyActions.HasFlag(flag)) return;

            var argsBefore = new DxListBoxActionCancelEventArgs(action);
            _RunListActionBefore(argsBefore);
            bool isCancelled = argsBefore.Cancel;
            if (!isCancelled)
            {
                if (internalActionMethod != null) internalActionMethod();
                var argsAfter = new DxListBoxActionEventArgs(action);
                _RunListActionAfter(argsAfter);
            }
            handled = true;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlA
        /// </summary>
        private void _DoKeyActionCtrlA()
        {
            this.SelectedItems = this.ListItems;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlC
        /// </summary>
        private void _DoKeyActionCtrlC()
        {
            var selectedItems = this.SelectedItems;
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
            var selectedItemsInfo = this.SelectedItemsInfo;
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
            _MoveItems(this.SelectedItemsInfo, targetIndex);
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
            var listItems = this.ListItems;
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
                      (withCurrentList ? this.ListItems.Where(i => i.ItemId != null).CreateDictionary(i => i.ItemId, true) : 
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
        private void DxDragDropDispose()
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
            var selectedItems = this.SelectedItemsInfo;
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
        /// Volá se po změně selected prvků
        /// </summary>
        /// <param name="e"></param>
        private void RunSelectedItemsChanged(TEventArgs<object> e)
        {
            OnSelectedItemsChanged(e);
            SelectedItemsChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se po změně selected prvků
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectedItemsChanged(TEventArgs<object> e) { }
        /// <summary>
        /// Událost volaná po změně selected prvků
        /// </summary>
        public event EventHandler<TEventArgs<object>> SelectedItemsChanged;

        /// <summary>
        /// Volá se po změně selected prvku
        /// </summary>
        /// <param name="e"></param>
        private void RunSelectedMenuItemChanged(TEventArgs<IMenuItem> e)
        {
            OnSelectedMenuItemChanged(e);
            SelectedMenuItemChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Volá se po změně selected prvku typu <see cref="IMenuItem"/>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectedMenuItemChanged(TEventArgs<IMenuItem> e) { }
        /// <summary>
        /// Událost volaná po změně selected prvku typu <see cref="IMenuItem"/>
        /// </summary>
        public event EventHandler<TEventArgs<IMenuItem>> SelectedMenuItemChanged;

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
        #endregion
    }
    /// <summary>
    /// Argumenty pro akci na <see cref="DxListBoxControl"/>
    /// </summary>
    public class DxListBoxActionEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="action"></param>
        public DxListBoxActionEventArgs(KeyActionType action)
        {
            this.Action = action;
        }
        /// <summary>
        /// Probíhající akce
        /// </summary>
        public KeyActionType Action { get; }
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
        /// <param name="action"></param>
        public DxListBoxActionCancelEventArgs(KeyActionType action)
            : base(action)
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
}
