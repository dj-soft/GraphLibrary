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
using DevExpress.XtraTreeList.Nodes;
using System.Diagnostics;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DxTreeList : panel obsahující DxFilterBox a DxTreeListNative
    /// <summary>
    /// Komponenta typu Panel, která v sobě obsahuje <see cref="DxFilterBox"/> a <see cref="DxTreeListNative"/>.
    /// </summary>
    public class DxTreeList : DxPanelControl
    {
        #region Konstruktor a vlastní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTreeList()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace komponent a hodnot
        /// </summary>
        private void Initialize()
        {
            _TreeListNative = new DxTreeListNative() { Dock = DockStyle.Fill, TabIndex = 1 };
            _RegisterTreeListEventHandlers();
            this.Controls.Add(_TreeListNative);
            _FilterBoxInitialize();
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        private DxTreeListNative _TreeListNative;
        #endregion
        #region Vlastnosti DxTreeListNative
        /// <summary>
        /// Fyzický control <see cref="DxTreeListNative"/>, umístěný v this panelu
        /// </summary>
        internal DxTreeListNative TreeListNative { get { return _TreeListNative; } }
        /// <summary>
        /// Text (lokalizovaný) pro text uzlu, který reprezentuje "LazyLoadChild", např. něco jako "Načítám data..."
        /// </summary>
        public string LazyLoadNodeText { get { return _TreeListNative.LazyLoadNodeText; } set { _TreeListNative.LazyLoadNodeText = value; } }
        /// <summary>
        /// Název ikony uzlu, který reprezentuje "LazyLoadChild", např. něco jako přesýpací hodiny...
        /// </summary>
        public string LazyLoadNodeImageName { get { return _TreeListNative.LazyLoadNodeImageName; } set { _TreeListNative.LazyLoadNodeImageName = value; } }
        /// <summary>
        /// Po LazyLoad aktivovat první načtený node?
        /// </summary>
        public TreeListLazyLoadFocusNodeType LazyLoadFocusNode { get { return _TreeListNative.LazyLoadFocusNode; } set { _TreeListNative.LazyLoadFocusNode = value; } }
        /// <summary>
        /// Režim zobrazení Checkboxů. 
        /// Výchozí je <see cref="TreeListCheckBoxMode.None"/>
        /// </summary>
        public TreeListCheckBoxMode CheckBoxMode { get { return _TreeListNative.CheckBoxMode; } set { _TreeListNative.CheckBoxMode = value; } }
        /// <summary>
        /// Režim kreslení ikon u nodů.
        /// Výchozí je <see cref="TreeListImageMode.ImageStatic"/> = zobrazuje se standardní ikona <see cref="ITextItem.ImageName"/> 
        /// (ale nezobrazují se ikony <see cref="ITreeListNode.ImageDynamicDefault"/> a <see cref="ITreeListNode.ImageDynamicSelected"/>).
        /// Aplikační kód musí plnit jména ikon do <see cref="ITextItem.ImageName"/>, <see cref="ITreeListNode.ImageDynamicDefault"/> a <see cref="ITreeListNode.ImageDynamicSelected"/>.
        /// </summary>
        public TreeListImageMode ImageMode { get { return _TreeListNative.ImageMode; } set { _TreeListNative.ImageMode = value; } }
        /// <summary>
        /// Velikost obrázků u nodů. Lze setovat jen když TreeList nemá žádné nody = pokud <see cref="NodesCount"/> == 0
        /// </summary>
        public ResourceImageSizeType NodeImageSize { get { return _TreeListNative.NodeImageSize; } set { _TreeListNative.NodeImageSize = value; } }
        /// <summary>
        /// V nodu se povoluje HTML text fomrátování
        /// </summary>
        public bool NodeAllowHtmlText { get { return _TreeListNative.NodeAllowHtmlText; } set { _TreeListNative.NodeAllowHtmlText = value; } }
        /// <summary>
        /// Typ ikon: výchozí je <see cref="ResourceContentType.None"/>, lze nastavit jen na <see cref="ResourceContentType.Bitmap"/> nebo <see cref="ResourceContentType.Vector"/> a to jen když nejsou položky.
        /// Není povinné nastavovat, nastaví se podle typu prvního obrázku. Pak ale musí být všechny obrázky stejného typu.
        /// Pokud bude nastaveno, pak i první obrázek musí odpovídat.
        /// </summary>
        public ResourceContentType NodeImageType { get { return _TreeListNative.NodeImageType; } set { _TreeListNative.NodeImageType = value; } }
        /// <summary>
        /// Akce, která zahájí editaci buňky.
        /// Výchozí je MouseUp (nejhezčí), ale je možno nastavit i jinak.
        /// </summary>
        public DevExpress.XtraTreeList.TreeListEditorShowMode EditorShowMode { get { return _TreeListNative.EditorShowMode; } set { _TreeListNative.EditorShowMode = value; } }
        /// <summary>
        /// Režim inkrementálního vyhledávání (=psaní na klávesnici).
        /// Default = <see cref="TreeListIncrementalSearchMode.InExpandedNodesOnly"/>
        /// </summary>
        public TreeListIncrementalSearchMode IncrementalSearchMode { get { return _TreeListNative.IncrementalSearchMode; } set { _TreeListNative.IncrementalSearchMode = value; } }
        /// <summary>
        /// Pozadí TreeListu je transparentní (pak je vidět podkladový Panel)
        /// </summary>
        public bool TransparentBackground { get { return _TreeListNative.TransparentBackground; } set { _TreeListNative.TransparentBackground = value; } }
        /// <summary>
        /// TreeList povoluje provést MultiSelect = označit více nodů.
        /// Default = false.
        /// </summary>
        public bool MultiSelectEnabled { get { return this._TreeListNative.MultiSelectEnabled; } set { this._TreeListNative.MultiSelectEnabled = value; } }
        /// <summary>
        /// Má být selectován ten node, pro který se právě chystáme zobrazit kontextovém menu?
        /// <para/>
        /// Pokud je zobrazováno kontextové menu nad určitým nodem, a tento node není selectován, pak hodnota true zajistí, že tento node bude nejprve selectován.
        /// Hodnota true je defaultní.
        /// <para/>
        /// Pokud bude false, pak neselectovaný node bude ponechán neselectovaný.
        /// Událost <see cref="ShowContextMenu"/> dostává argument, v němž je definován ten node na který bylo kliknuto, i když není Selected.
        /// </summary>
        public bool SelectNodeBeforeShowContextMenu { get { return this._TreeListNative.SelectNodeBeforeShowContextMenu; } set { this._TreeListNative.SelectNodeBeforeShowContextMenu = value; } }
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy? Null = default
        /// </summary>
        public bool? ToolTipAllowHtmlText { get { return _TreeListNative.ToolTipAllowHtmlText; } set { _TreeListNative.ToolTipAllowHtmlText = value; } }
        /// <summary>
        /// Gets or sets whether to use animation effects when expanding/collapsing nodes using the expand button.
        /// </summary>
        public DevExpress.Utils.DefaultBoolean AllowOptionExpandAnimation { get { return _TreeListNative.AllowOptionExpandAnimation; } set { _TreeListNative.AllowOptionExpandAnimation = value; } }
        /// <summary>
        /// Gets or sets the animation mode, which identifies cells for which animation is enabled.
        /// </summary>
        public DevExpress.XtraTreeList.TreeListAnimationType AnimationType { get { return _TreeListNative.AnimationType; } set { _TreeListNative.AnimationType = value; } }
        /// <summary>
        /// Gets or sets a value that specifies how the focus rectangle is painted.
        /// </summary>
        public DevExpress.XtraTreeList.DrawFocusRectStyle FocusRectStyle { get { return _TreeListNative.FocusRectStyle; } set { _TreeListNative.FocusRectStyle = value; } }
        /// <summary>
        /// Co provede DoubleClick na textu anebo Click na ikoně
        /// </summary>
        public NodeMainClickMode MainClickMode { get { return _TreeListNative.MainClickMode; } set { _TreeListNative.MainClickMode = value; } }
        /// <summary>
        /// Zobrazovat Root node?
        /// Má se nastavit po inicializaci nebo po <see cref="ClearNodes"/>. Změna nastavení později nemá význam.
        /// </summary>
        public bool RootNodeVisible { get { return _TreeListNative.RootNodeVisible; } set { _TreeListNative.RootNodeVisible = value; } }
        /// <summary>
        /// Souhrn povolených akcí Drag and Drop v rámci TreeListu
        /// </summary>
        public DxDragDropActionType DragDropActions { get { return _TreeListNative.DragDropActions; } set { _TreeListNative.DragDropActions = value; } }
        /// <summary>
        /// Aktuální hodnota pro zobrazení Root nodu.
        /// Nastavuje se před přidáním prvního nodu, podle hodnoty <see cref="RootNodeVisible"/>.
        /// Jakmile v evidenci je už nějaký node, pak se tato hodnota nemění.
        /// </summary>
        public bool CurrentRootNodeVisible { get { return _TreeListNative.CurrentRootNodeVisible; } }
        /// <summary>
        /// Počet aktuálních fyzických nodů
        /// </summary>
        public int NodesCount { get { return _TreeListNative.NodesCount; } }
        /// <summary>
        /// Povolené akce. Výchozí je <see cref="KeyActionType.None"/>
        /// </summary>
        public KeyActionType EnabledKeyActions { get { return _TreeListNative.EnabledKeyActions; } set { _TreeListNative.EnabledKeyActions = value; } }
        /// <summary>
        /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="KeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
        /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
        /// </summary>
        /// <param name="actions"></param>
        public void DoKeyActions(params KeyActionType[] actions) { _TreeListNative.DoKeyActions(actions); }
        /// <summary>
        /// Seznam HotKeys = klávesy, pro které se volá událost <see cref="NodeKeyDown"/>.
        /// </summary>
        public IEnumerable<Keys> HotKeys { get { return _TreeListNative.HotKeys; } set { _TreeListNative.HotKeys = value; } }
        /// <summary>
        /// Nastaví danou barvu jako všechny barvy pozadí
        /// </summary>
        /// <param name="backColor"></param>
        public void SetBackColor(System.Drawing.Color backColor) { _TreeListNative.SetBackColor(backColor); }
        /// <summary>
        /// Zajistí provedení dodané akce s argumenty v GUI threadu a v jednom vizuálním zámku s jedním Refreshem na konci.
        /// <para/>
        /// Víš jak se píše Delegate pro cílovou metodu s konkrétním parametrem typu bool? 
        /// RunInLock(new Action&lt;bool&gt;(volaná_metoda), hodnota_bool)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public void RunInLock(Delegate method, params object[] args) { _TreeListNative.RunInLock(method, args); }
        /// <summary>
        /// Dá Focus do main controlu
        /// </summary>
        private void _MainControlFocus()
        {
            _TreeListNative.Focus();
        }
        #endregion
        #region Nody DxTreeListNative: aktivní node, kolekce nodů, vyhledání, přidání, odebrání
        /// <summary>
        /// Aktuálně vybraný Node
        /// </summary>
        public ITreeListNode FocusedNodeInfo { get { return _TreeListNative.FocusedNodeInfo; } }
        /// <summary>
        /// Obsahuje <see cref="ITextItem.ItemId"/> aktuálně focusovaného nodu.
        /// Lze setovat. Pokud bude setován neexistující ID, pak focusovaný node bude null.
        /// </summary>
        public string FocusedNodeFullId { get { return _TreeListNative.FocusedNodeFullId; } set { _TreeListNative.FocusedNodeFullId = value; } }
        /// <summary>
        /// Najde node podle jeho klíče, pokud nenajde pak vrací false.
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        public bool TryGetNodeInfo(string nodeKey, out ITreeListNode nodeInfo) { return _TreeListNative.TryGetNodeInfo(nodeKey, out nodeInfo); }
        /// <summary>
        /// Pole všech nodů = třída <see cref="ITreeListNode"/> = data o nodech
        /// </summary>
        public ITreeListNode[] NodeInfos { get { return _TreeListNative.NodeInfos; } }
        /// <summary>
        /// Najde a vrátí pole nodů, které jsou Child nody daného klíče.
        /// Reálně provádí Scan všech nodů.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        public ITreeListNode[] GetChildNodeInfos(string parentKey) { return _TreeListNative.GetChildNodeInfos(parentKey); }
        /// <summary>
        /// Přidá jeden node. Není to příliš efektivní. Raději používejme <see cref="AddNodes(IEnumerable{ITreeListNode}, bool, PreservePropertiesMode)"/>.
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        public void AddNode(ITreeListNode nodeInfo, int? atIndex = null) { _TreeListNative.AddNode(nodeInfo, atIndex); }
        /// <summary>
        /// Přidá řadu nodů. 
        /// Současné nody ponechává (pokud parametr <paramref name="clear"/> je false).
        /// Pokud je zadán parametr <paramref name="clear"/> = true, pak smaže všechny aktuální nody, a provede to v jednom vizuálním zámku s přidáním nodů.
        /// Lze tak přidat například jednu podvětev.
        /// Na konci provede Refresh.
        /// <br/>
        /// Po dobu provádění této akce nejsou volány žádné události.
        /// </summary>
        /// <param name="addNodes"></param>
        /// <param name="clear">Smazat všechny aktuální nody?</param>
        /// <param name="preserveProperties"></param>
        public void AddNodes(IEnumerable<ITreeListNode> addNodes, bool clear = false, PreservePropertiesMode preserveProperties = PreservePropertiesMode.None)
        {
            _TreeListNative.AddNodes(addNodes, clear, preserveProperties);
        }
        /// <summary>
        /// Přidá řadu nodů, které jsou donačteny k danému parentu. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Nejprve najde daného parenta, a zruší z něj příznak LazyLoad (protože právě tímto načtením jsou jeho nody vyřešeny). Současně odebere "wait" node (prázdný node, simulující načítání dat).
        /// Pak teprve přidá nové nody.
        /// Na konci provede Refresh.
        /// <br/>
        /// Po dobu provádění této akce nejsou volány žádné události.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <param name="addNodes"></param>
        public void AddLazyLoadNodes(string parentNodeId, IEnumerable<ITreeListNode> addNodes) { _TreeListNative.AddLazyLoadNodes(parentNodeId, addNodes); }
        /// <summary>
        /// Selectuje daný Node
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void SetFocusToNode(ITreeListNode nodeInfo) { _TreeListNative.SetFocusToNode(nodeInfo); }
        /// <summary>
        /// Pole všech nodů, které jsou aktuálně Selected
        /// </summary>
        public ITreeListNode[] SelectedNodes { get { return _TreeListNative.SelectedNodes; } }
        /// <summary>
        /// Odebere jeden daný node, podle klíče. Na konci provede Refresh.
        /// Pro odebrání více nodů je lepší použít <see cref="RemoveNodes(IEnumerable{string})"/>.
        /// </summary>
        /// <param name="removeNodeKey"></param>
        public void RemoveNode(string removeNodeKey) { _TreeListNative.RemoveNode(removeNodeKey); }
        /// <summary>
        /// Odebere řadu nodů, podle klíče. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        public void RemoveNodes(IEnumerable<string> removeNodeKeys) { _TreeListNative.RemoveNodes(removeNodeKeys); }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        /// <param name="preserveProperties"></param>
        public void RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes, PreservePropertiesMode preserveProperties = PreservePropertiesMode.None) { _TreeListNative.RemoveAddNodes(removeNodeKeys, addNodes, preserveProperties); }
        /// <summary>
        /// Smaže všechny nodes. Na konci provede Refresh.
        /// </summary>
        public void ClearNodes() { _TreeListNative.ClearNodes(); }
        /// <summary>
        /// Zajistí refresh jednoho daného nodu. 
        /// Pro refresh více nodů použijme <see cref="RefreshNodes(IEnumerable{ITreeListNode})"/>!
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void RefreshNode(ITreeListNode nodeInfo) { _TreeListNative.RefreshNode(nodeInfo); }
        /// <summary>
        /// Zajistí refresh daných nodů.
        /// </summary>
        /// <param name="nodes"></param>
        public void RefreshNodes(IEnumerable<ITreeListNode> nodes) { _TreeListNative.RefreshNodes(nodes); }
        #endregion
        #region FilterRow
        /// <summary>
        /// Fyzický control <see cref="DxFilterBox"/>, umístěný v this panelu
        /// </summary>
        internal DxFilterBox FilterBox { get { return _FilterBox; } }
        /// <summary>
        /// Inicializace FilterBoxu, a jeho vložení do this.Controls
        /// </summary>
        private void _FilterBoxInitialize()
        {
            _FilterBox = new DxFilterBox() { Dock = DockStyle.Top, Visible = false, TabIndex = 0 };
            _FilterBox.FilterOperators = DxFilterBox.CreateDefaultOperatorItems(FilterBoxOperatorItems.DefaultText);
            _FilterBoxVisible = false;
            _RegisterFilterRowEventHandlers();
            this.Controls.Add(_FilterBox);
        }
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="_FilterBox"/>
        /// </summary>
        private void _RegisterFilterRowEventHandlers()
        {
            _FilterBox.FilterValueChangedSources = DxFilterBoxChangeEventSource.DefaultGreen;
            _FilterBox.FilterValueChanged += FilterBox_Changed;      // Změna obsahu filtru a Enter
            _FilterBox.KeyEnterPress += FilterBox_KeyEnter;
        }
        /// <summary>
        /// Zobrazovat řádkový filtr? Default = NE
        /// </summary>
        public bool FilterBoxVisible { get { return _FilterBoxVisible; } set { _FilterBoxVisible = value; this.RunInGui(FilterBoxSetVisible); } } private bool _FilterBoxVisible = false;
        /// <summary>
        /// Aktuální text v řádkovém filtru
        /// </summary>
        public DxFilterBoxValue FilterBoxValue { get { return _FilterBox.FilterValue; } set { _FilterBox.FilterValue = value; } }
        /// <summary>
        /// Pole operátorů nabízených pod tlačítkem vlevo.
        /// Pokud bude vloženo null nebo prázdné pole, pak tlačítko vlevo nebude zobrazeno vůbec, a v hodnotě FilterValue bude Operator = null.
        /// </summary>
        public List<IMenuItem> FilterBoxOperators { get { return _FilterBox.FilterOperators; } set { _FilterBox.FilterOperators = value; } }
        /// <summary>
        /// Za jakých událostí se volá event <see cref="FilterBoxChanged"/>
        /// </summary>
        public DxFilterBoxChangeEventSource FilterBoxChangedSources { get { return _FilterBox.FilterValueChangedSources; } set { _FilterBox.FilterValueChangedSources = value; } }
        /// <summary>
        /// Událost volaná po hlídané změně obsahu filtru.
        /// Argument obsahuje hodnotu filtru a druh události, která vyvolala event.
        /// Druhy události, pro které se tento event volá, lze nastavit v <see cref="FilterBoxChangedSources"/>.
        /// </summary>
        public event EventHandler<DxFilterBoxChangeArgs> FilterBoxChanged;
        /// <summary>
        /// Provede se po stisku Enter v řádkovém filtru (i bez změny textu), vhodné pro řízení Focusu
        /// </summary>
        public event EventHandler FilterBoxKeyEnter;
        /// <summary>
        /// Aplikuje viditelnost pro FilterRow
        /// </summary>
        private void FilterBoxSetVisible()
        {
            _FilterBox.Visible = _FilterBoxVisible;
        }
        /// <summary>
        /// Po jakékoli změně v řádkovém filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void FilterBox_Changed(object sender, DxFilterBoxChangeArgs args)
        {
            OnFilterBoxChanged(args);
            FilterBoxChanged?.Invoke(this, args);
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
        private void FilterBox_KeyEnter(object sender, EventArgs e)
        {
            _MainControlFocus();
            OnFilterBoxKeyEnter();
            FilterBoxKeyEnter?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po stisku Enter v řádkovém filtru
        /// </summary>
        protected virtual void OnFilterBoxKeyEnter() { }
        /// <summary>
        /// DxFilterBox
        /// </summary>
        private DxFilterBox _FilterBox;
        #endregion
        #region Eventy a další akce DxTreeListNative
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="_TreeListNative"/>
        /// </summary>
        private void _RegisterTreeListEventHandlers()
        {
            _TreeListNative.NodeKeyDown += _TreeListNative_NodeKeyDown;
            _TreeListNative.NodeFocusedChanged += _TreeListNative_NodeFocusedChanged;
            _TreeListNative.SelectedNodesChanged += _TreeListNative_SelectedNodesChanged;
            _TreeListNative.ShowContextMenu += _TreeListNative_ShowContextMenu;
            _TreeListNative.NodeIconClick += _TreeListNative_NodeIconClick;
            _TreeListNative.NodeDoubleClick += _TreeListNative_NodeDoubleClick;
            _TreeListNative.NodeExpanded += _TreeListNative_NodeExpanded;
            _TreeListNative.NodeCollapsed += _TreeListNative_NodeCollapsed;
            _TreeListNative.ActivatedEditor += _TreeListNative_ActivatedEditor;
            _TreeListNative.EditorDoubleClick += _TreeListNative_EditorDoubleClick;
            _TreeListNative.NodeEdited += _TreeListNative_NodeEdited;
            _TreeListNative.NodeCheckedChange += _TreeListNative_NodeCheckedChange;
            _TreeListNative.NodesDelete += _TreeListNative_NodesDelete;
            _TreeListNative.LazyLoadChilds += _TreeListNative_LazyLoadChilds;
            _TreeListNative.ToolTipChanged += _TreeListNative_ToolTipChanged;
        }
        private void _TreeListNative_NodeKeyDown(object sender, DxTreeListNodeKeyArgs args) { this.OnNodeKeyDown(args); this.NodeKeyDown?.Invoke(this, args); }
        private void _TreeListNative_NodeFocusedChanged(object sender, DxTreeListNodeArgs args) { this.OnNodeFocusedChanged(args); this.NodeFocusedChanged?.Invoke(this, args); }
        private void _TreeListNative_SelectedNodesChanged(object sender, DxTreeListNodeArgs args) { this.OnSelectedNodesChanged(args); this.SelectedNodesChanged?.Invoke(this, args); }
        private void _TreeListNative_ShowContextMenu(object sender, DxTreeListNodeContextMenuArgs args) { this.OnShowContextMenu(args); this.ShowContextMenu?.Invoke(this, args); }
        private void _TreeListNative_NodeIconClick(object sender, DxTreeListNodeArgs args) { this.OnNodeIconClick(args); this.NodeIconClick?.Invoke(this, args); }
        private void _TreeListNative_NodeDoubleClick(object sender, DxTreeListNodeArgs args) { this.OnNodeDoubleClick(args); this.NodeDoubleClick?.Invoke(this, args); }
        private void _TreeListNative_NodeExpanded(object sender, DxTreeListNodeArgs args) { this.OnNodeExpanded(args); this.NodeExpanded?.Invoke(this, args); }
        private void _TreeListNative_NodeCollapsed(object sender, DxTreeListNodeArgs args) { this.OnNodeCollapsed(args); this.NodeCollapsed?.Invoke(this, args); }
        private void _TreeListNative_ActivatedEditor(object sender, DxTreeListNodeArgs args) { this.OnActivatedEditor(args); this.ActivatedEditor?.Invoke(this, args); }
        private void _TreeListNative_EditorDoubleClick(object sender, DxTreeListNodeArgs args) { this.OnEditorDoubleClick(args); this.EditorDoubleClick?.Invoke(this, args); }
        private void _TreeListNative_NodeEdited(object sender, DxTreeListNodeArgs args) { this.OnNodeEdited(args); this.NodeEdited?.Invoke(this, args); }
        private void _TreeListNative_NodeCheckedChange(object sender, DxTreeListNodeArgs args) { this.OnNodeCheckedChange(args); this.NodeCheckedChange?.Invoke(this, args); }
        private void _TreeListNative_NodesDelete(object sender, DxTreeListNodesArgs args) { this.OnNodesDelete(args); this.NodesDelete?.Invoke(this, args); }
        private void _TreeListNative_LazyLoadChilds(object sender, DxTreeListNodeArgs args) { this.OnLazyLoadChilds(args); this.LazyLoadChilds?.Invoke(this, args); }
        private void _TreeListNative_ToolTipChanged(object sender, DxToolTipArgs args) { this.OnToolTipChanged(args); this.ToolTipChanged?.Invoke(this, args); }

        /// <summary>
        /// TreeList má KeyUp na určitém Node.
        /// Pozor, aby se tento event vyvolal, je třeba nejdřív nastavit kolekci <see cref="HotKeys"/>!
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeKeyDown(DxTreeListNodeKeyArgs args) { }
        /// <summary>
        /// TreeList má KeyUp na určitém Node.
        /// Pozor, aby se tento event vyvolal, je třeba nejdřív nastavit kolekci <see cref="HotKeys"/>!
        /// </summary>
        public event DxTreeListNodeKeyHandler NodeKeyDown;
        /// <summary>
        /// TreeList aktivoval určitý Node
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeFocusedChanged(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList aktivoval určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeFocusedChanged;
        /// <summary>
        /// TreeList změnil seznam <see cref="SelectedNodes"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnSelectedNodesChanged(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList změnil seznam <see cref="SelectedNodes"/>
        /// </summary>
        public event DxTreeListNodeHandler SelectedNodesChanged;
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnShowContextMenu(DxTreeListNodeContextMenuArgs args) { }
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        public event DxTreeListNodeContextMenuHandler ShowContextMenu;
        /// <summary>
        /// TreeList má Mouseclick na ikonu pro určitý Node
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeIconClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList má Mouseclick na ikonu pro určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeIconClick;
        /// <summary>
        /// TreeList má Doubleclick na určitý Node
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeDoubleClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList má Doubleclick na určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeDoubleClick;
        /// <summary>
        /// TreeList právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyExpandable"/>).
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeExpanded(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyExpandable"/>).
        /// </summary>
        public event DxTreeListNodeHandler NodeExpanded;
        /// <summary>
        /// TreeList právě sbaluje určitý Node.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeCollapsed(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě sbaluje určitý Node.
        /// </summary>
        public event DxTreeListNodeHandler NodeCollapsed;
        /// <summary>
        /// TreeList právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnActivatedEditor(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        public event DxTreeListNodeHandler ActivatedEditor;
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnEditorDoubleClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        public event DxTreeListNodeHandler EditorDoubleClick;
        /// <summary>
        /// TreeList právě skončil editaci určitého Node.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeEdited(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě skončil editaci určitého Node.
        /// </summary>
        public event DxTreeListNodeHandler NodeEdited;
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeCheckedChange(DxTreeListNodeArgs args) { }
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        public event DxTreeListNodeHandler NodeCheckedChange;
        /// <summary>
        /// Uživatel dal Delete na uzlu, který se needituje.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodesDelete(DxTreeListNodesArgs args) { }
        /// <summary>
        /// Uživatel dal Delete na uzlu, který se needituje.
        /// </summary>
        public event DxTreeListNodesHandler NodesDelete;
        /// <summary>
        ///  TreeList rozbaluje node, který má nastaveno načítání ze serveru : <see cref="ITreeListNode.LazyExpandable"/> je true.
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnLazyLoadChilds(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList rozbaluje node, který má nastaveno načítání ze serveru : <see cref="ITreeListNode.LazyExpandable"/> je true.
        /// </summary>
        public event DxTreeListNodeHandler LazyLoadChilds;
        /// <summary>
        /// ToolTip v TreeListu má událost
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToolTipChanged(DxToolTipArgs args) { }
        /// <summary>
        /// ToolTip v TreeListu má událost
        /// </summary>
        public event DxToolTipHandler ToolTipChanged;
        #endregion
    }
    #endregion
    #region DxTreeListNative : potomek DevExpress.XtraTreeList.TreeList
    /// <summary>
    /// <see cref="DxTreeListNative"/> : potomek <see cref="DevExpress.XtraTreeList.TreeList"/> s podporou pro použití v Greenu.
    /// Nemá se používat přímo, má se používat <see cref="DxTreeList"/>.
    /// </summary>
    public class DxTreeListNative : DevExpress.XtraTreeList.TreeList, IListenerStyleChanged, IDxDragDropControl, IDxToolTipDynamicClient
    {
        #region Konstruktor a inicializace, privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTreeListNative()
        {
            this._LastId = 0;
            this._NodesId = new Dictionary<int, NodePair>();
            this._NodesKey = new Dictionary<string, NodePair>();
            this.RootNodeVisible = true;
            InitTreeList();
            DataExchangeInit();
            DxDragDropInit(DxDragDropActionType.None);
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnregisterListener(this);
            CurrentViewDispose();
            base.Dispose(disposing);
            DxDragDropDispose();
        }
        /// <summary>
        /// Incializace komponenty Simple
        /// </summary>
        protected void InitTreeList()
        {
            this.OptionsBehavior.PopulateServiceColumns = false;
            this._MainColumn = new DevExpress.XtraTreeList.Columns.TreeListColumn() { Name = "MainColumn", Visible = true, Width = 150, UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.String, Caption = "Sloupec1", AllowIncrementalSearch = true, FieldName = "Text", ShowButtonMode = DevExpress.XtraTreeList.ShowButtonModeEnum.ShowForFocusedRow, ToolTip = "Tooltip pro sloupec" };
            this.Columns.Add(this._MainColumn);

            this._MainColumn.OptionsColumn.AllowEdit = false;
            this._MainColumn.OptionsColumn.AllowSort = false;

            // this.OptionsBehavior.AllowExpandOnDblClick = false;             // Neřeš to explicitně, to řeší property this.MainClickMode !!!
            this.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.False;                // Nezapínej to, DevExpress mají (v 20.1.6.0) problém s vykreslováním!
            this.OptionsBehavior.Editable = true;
            this.OptionsBehavior.EditingMode = DevExpress.XtraTreeList.TreeListEditingMode.Inplace;
            this.OptionsBehavior.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;    // Kdy se zahájí editace (kurzor)? MouseUp: docela hezké; MouseDownFocused: po MouseDown ve stavu Focused (až na druhý klik)
            this.OptionsBehavior.ShowToolTips = true;
            this.OptionsBehavior.SmartMouseHover = true;
            this.OptionsBehavior.AllowExpandAnimation = DevExpress.Utils.DefaultBoolean.True;
            this.OptionsBehavior.AutoNodeHeight = true;
            this.OptionsBehavior.AutoSelectAllInEditor = true;
            this.OptionsBehavior.CloseEditorOnLostFocus = true;

            this.OptionsNavigation.AutoMoveRowFocus = true;
            this.OptionsNavigation.EnterMovesNextColumn = false;
            this.OptionsNavigation.MoveOnEdit = false;
            this.OptionsNavigation.UseTabKey = false;

            this.OptionsMenu.ShowExpandCollapseItems = false;
            this.OptionsMenu.EnableNodeMenu = false;

            this.IncrementalSearchMode = TreeListIncrementalSearchMode.InExpandedNodesOnly;
            this.SelectNodeBeforeShowContextMenu = true;
            this.MainClickMode = NodeMainClickMode.RunEvent;

            this.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.OptionsSelection.EnableAppearanceHotTrackedRow = DefaultBoolean.True;
            this.OptionsSelection.InvertSelection = true;
            this.OptionsSelection.MultiSelect = false;
            this.OptionsSelection.MultiSelectMode = DevExpress.XtraTreeList.TreeListMultiSelectMode.RowSelect;

            this.ViewStyle = DevExpress.XtraTreeList.TreeListViewStyle.TreeView;

            // DirectX NELZE použít, protože na sekundárním monitoru 4K se zoomem 200% se vykresluje TreeList 2x:
            //   jednou nativně od DirectX, na souřadnicích 1/2 (poloviční velikost a poloviční Location, a neaktivní = obraz od DirectX)
            //   a podruhé na korektním místě (1:1) ... (monitor je vlevo = má záporné souřadnice)
            // Sice na jiných monitorech je vše OK, ale není to spolehlivé!
            this.UseDirectXPaint = DefaultBoolean.False;
            this.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.False;                // Běžně nezapínat, ale na DirectX to chodí!   Nezapínej to, DevExpress mají (v 20.1.6.0) problém s vykreslováním!

            // Tooltip:
            _ToolTipInit();

            // Eventy pro podporu TreeList (vykreslení nodu, atd):
            this.NodeCellStyle += _OnNodeCellStyle;
            this.CustomDrawNodeCheckBox += _OnCustomDrawNodeCheckBox;
            this.PreviewKeyDown += _OnPreviewKeyDown;
            this.KeyDown += _OnKeyDown;
            this.KeyUp += _OnKeyUp;

            // Nativní eventy:
            this.FocusedNodeChanged += _OnFocusedNodeChanged;
            this.SelectionChanged += _OnSelectionChanged;
            this.MouseClick += _OnMouseClick;
            this.MouseUp += _OnMouseUp;
            this.PopupMenuShowing += _OnPopupMenuShowing;
            this.DoubleClick += _OnDoubleClick;
            this.ShownEditor += _OnShownEditor;
            this.ValidatingEditor += _OnValidatingEditor;
            this.BeforeCheckNode += _OnBeforeCheckNode;
            this.AfterCheckNode += _OnAfterCheckNode;
            this.BeforeExpand += _OnBeforeExpand;
            this.AfterExpand += _OnAfterExpand;
            this.BeforeCollapse += _OnBeforeCollapse;
            this.AfterCollapse += _OnAfterCollapse;
            
            // Preset:
            this.LazyLoadNodeText = "...";
            this.LazyLoadNodeImageName = null;
            this.CheckBoxMode = TreeListCheckBoxMode.None;
            this._NodeImageType = ResourceContentType.None;
            this._NodeImageSize = ResourceImageSizeType.Small;
            this._ImageMode = TreeListImageMode.ImageStatic;
        }
        /// <summary>
        /// Jediný column
        /// </summary>
        DevExpress.XtraTreeList.Columns.TreeListColumn _MainColumn;
        #endregion
        #region Úložiště dat nodů, a třída NodePair
        /// <summary>
        /// Index podle permanentního Int32 klíče, klíč je přidělen při tvorbě nodu jako běžné ID (1+++)
        /// </summary>
        private Dictionary<int, NodePair> _NodesId;
        /// <summary>
        /// Index podle FullNodeId = stringová identifikace nodu
        /// </summary>
        private Dictionary<string, NodePair> _NodesKey;
        private int _LastId;
        /// <summary>
        /// Třída obsahující jeden pár dat: vizuální plus datová
        /// </summary>
        private class NodePair
        {
            public NodePair(DxTreeListNative owner, int nodeId, ITreeListNode nodeInfo, DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, bool isLazyChild)
            {
                this.Id = nodeId;
                this.NodeInfo = nodeInfo;
                this.TreeNode = treeNode;
                this.IsLazyChild = isLazyChild;

                this.NodeInfo.Id = nodeId;
                this.NodeInfo.Owner = owner;
                OriginalTreeNodeId = CurrentTreeNodeId;              // Pokud není dodán TreeNode, pak CurrentTreeNodeId je -1.
                if (this.TreeNode != null)                           // TreeNode může být null, pokud aktuální prvek reprezentuje RootNode, který se nezobrazuje
                    this.TreeNode.Tag = nodeId;
            }
            public void ReleasePair()
            {
                this.NodeInfo.Id = -1;
                this.NodeInfo.Owner = null;
                if (this.TreeNode != null) this.TreeNode.Tag = null;

                this.NodeInfo = null;
                this.TreeNode = null;
                this.IsLazyChild = false;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return (this.NodeInfo?.ToString() ?? "<Empty>");
            }
            /// <summary>
            /// Konstantní ID tohoto nodu, nemění se. Je přiděleno v <see cref="DxTreeListNative"/> a tam je i evidováno.
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// Originální interní ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
            /// Tato hodnota je sem vepsána ihned po vytvoření nodu do TreeList a později se neaktualizuje, nelze ji tedy použít pro pozdější vyhledání nodu - TreeList totiž ID průběžně mění :-).
            /// Pokud this instance nemá TreeNode, pak obsahuje -1.
            /// </summary>
            public int OriginalTreeNodeId { get; private set; }
            /// <summary>
            /// Aktuální interní ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
            /// Tato hodnota se mění při odebrání nodu z TreeList. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
            /// Pokud this instance nemá TreeNode, pak vrací -1.
            /// </summary>
            public int CurrentTreeNodeId { get { return TreeNode?.Id ?? -1; } }
            /// <summary>
            /// Klíč nodu, string
            /// </summary>
            public string NodeId { get { return NodeInfo?.ItemId; } }
            /// <summary>
            /// Datový objekt
            /// </summary>
            public ITreeListNode NodeInfo { get; private set; }
            /// <summary>
            /// Vizuální objekt.
            /// POZOR: TreeNode může být null, pokud aktuální prvek reprezentuje RootNode, který se nezobrazuje
            /// </summary>
            public DevExpress.XtraTreeList.Nodes.TreeListNode TreeNode { get; private set; }
            /// <summary>
            /// Obsahuje true pokud this instance má reálný vizuální node <see cref="TreeNode"/>
            /// </summary>
            public bool HasTreeNode { get { return (TreeNode != null); } }
            /// <summary>
            /// Obsahuje true pokud this má TreeNode a ten je Selected
            /// </summary>
            public bool IsTreeNodeSelected { get { return this.HasTreeNode && this.TreeNode.IsSelected; } }
            /// <summary>
            /// Tento prvek je zaveden jako LazyChild = reprezetuje fakt, že reálné Child nody budou teprve donačteny.
            /// </summary>
            public bool IsLazyChild { get; private set; }
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
        #endregion
        #region Vzhled
        /// <summary>
        /// Pozadí TreeListu je transparentní (pak je vidět podkladový Panel)
        /// </summary>
        public bool TransparentBackground { get { return _TransparentBackground; } set { _TransparentBackground = value; _ApplyTransparentBackground(); } } private bool _TransparentBackground;
        /// <summary>
        /// Gets or sets whether to use animation effects when expanding/collapsing nodes using the expand button.
        /// </summary>
        public DevExpress.Utils.DefaultBoolean AllowOptionExpandAnimation { get { return OptionsBehavior.AllowExpandAnimation; } set { OptionsBehavior.AllowExpandAnimation = value; } }
        /// <summary>
        /// Gets or sets the animation mode, which identifies cells for which animation is enabled.
        /// </summary>
        public DevExpress.XtraTreeList.TreeListAnimationType AnimationType { get { return OptionsView.AnimationType; } set { OptionsView.AnimationType = value; } }
        /// <summary>
        /// Gets or sets a value that specifies how the focus rectangle is painted.
        /// </summary>
        public DevExpress.XtraTreeList.DrawFocusRectStyle FocusRectStyle { get { return OptionsView.FocusRectStyle; } set { OptionsView.FocusRectStyle = value; } }
        /// <summary>
        /// TreeList povoluje provést MultiSelect = označit více nodů.
        /// Default = false.
        /// </summary>
        public bool MultiSelectEnabled { get { return this.OptionsSelection.MultiSelect; } set { this.OptionsSelection.MultiSelect = value; } }
        /// <summary>
        /// Aplikovat průhledné pozadí
        /// </summary>
        private void _ApplyTransparentBackground()
        {
            // Color? foreColor = (_TransparentBackground ? DxComponent.GetSkinColor(SkinElementColor.CommonSkins_InfoText) : null);
            Color? foreColor = (_TransparentBackground ? DxComponent.GetSkinColor(SkinElementColor.Control_LabelForeColor) : null);

            if (foreColor.HasValue)
            {
                Color backColor = Color.Transparent;
                this.Appearance.Empty.BackColor = backColor;
                this.Appearance.Empty.Options.UseBackColor = true;
                this.Appearance.Row.BackColor = backColor;
                this.Appearance.Row.Options.UseBackColor = true;

                this.Appearance.Empty.ForeColor = foreColor.Value;
                this.Appearance.Empty.Options.UseForeColor = true;
                this.Appearance.Row.ForeColor = foreColor.Value;
                this.Appearance.Row.Options.UseForeColor = true;
            }
            else
            {
                this.Appearance.Empty.Options.UseBackColor = false;
                this.Appearance.Row.Options.UseBackColor = false;

                this.Appearance.Empty.Options.UseForeColor = false;
                this.Appearance.Row.Options.UseForeColor = false;
            }
        }
        /// <summary>
        /// Nastaví danou barvu jako všechny barvy pozadí
        /// </summary>
        /// <param name="backColor"></param>
        public void SetBackColor(System.Drawing.Color backColor)
        {
            // this.BackColor = backColor;
            this.Appearance.Empty.BackColor = backColor;
            this.Appearance.Empty.Options.UseBackColor = true;
            this.Appearance.FocusedCell.BackColor = backColor;
            this.Appearance.FocusedCell.Options.UseBackColor = true;
            this.Appearance.FocusedRow.BackColor = backColor;
            this.Appearance.FocusedRow.Options.UseBackColor = true;
            this.Appearance.Row.BackColor = backColor;
            this.Appearance.Row.Options.UseBackColor = true;
            this.Appearance.SelectedRow.BackColor = backColor;
            this.Appearance.SelectedRow.Options.UseBackColor = true;
        }
        /// <summary>
        /// Je voláno vždy po změně skinu
        /// </summary>
        void IListenerStyleChanged.StyleChanged()
        {
            _ApplyTransparentBackground();
        }
        #endregion
        #region ToolTipy pro nodes, HasMouse a IsFocused
        /// <summary>
        /// Inicializace ToolTipu, voláno z konstruktoru
        /// </summary>
        private void _ToolTipInit()
        {
            this.ToolTipAllowHtmlText = null;
            this.DxToolTipController = DxComponent.CreateNewToolTipController(ToolTipAnchor.Cursor);
            this.DxToolTipController.AddClient(this);      // Protože this třída implementuje IDxToolTipDynamicClient, bude volána metoda IDxToolTipDynamicClient.PrepareSuperTipForPoint()
            this.DxToolTipController.ToolTipChanged += _ToolTipChanged;        // Má význam jen pro Debug, nemusí být řešeno
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
            var hit = _GetNodeHit(args.MouseLocation);
            if (hit.IsInImagesOrCell)
            {
                // Pokud myš nyní ukazuje na ten samý Node, pro který už máme ToolTip vytvořen, pak nebudeme ToolTip připravovat:
                bool isSameAsLast = (args.DxSuperTip != null && Object.ReferenceEquals(args.DxSuperTip.ClientData, hit.NodeInfo));
                if (!isSameAsLast)
                {   // Připravíme data pro ToolTip:
                    var dxSuperTip = DxComponent.CreateDxSuperTip(hit.NodeInfo);        // Vytvořím new data ToolTipu
                    if (dxSuperTip != null)
                    {
                        if (ToolTipAllowHtmlText.HasValue) dxSuperTip.ToolTipAllowHtmlText = ToolTipAllowHtmlText;
                        dxSuperTip.ClientData = hit.NodeInfo;                           // Přibalím si do nich náš Node abych příště detekoval, zda jsme/nejsme na tom samém
                    }
                    args.DxSuperTip = dxSuperTip;
                    args.ToolTipChange = DxToolTipChangeType.NewToolTip;                 // Zajistím rozsvícení okna ToolTipu
                }
                else
                {
                    args.ToolTipChange = DxToolTipChangeType.SameAsLastToolTip;          // Není třeba nic dělat, nechme svítit stávající ToolTip
                }
            }
            else
            {   // Myš je mimo nody:
                args.ToolTipChange = DxToolTipChangeType.NoToolTip;                      // Pokud ToolTip svítí, zhasneme jej
            }
            RaiseToolTipChanged($"TreeList.PrepareSuperTipForPoint(ToolTipChange={args.ToolTipChange}, ToolTip={args.DxSuperTip?.ToString()})");
        }
        /// <summary>
        /// Explicitně skrýt ToolTip
        /// </summary>
        /// <param name="message"></param>
        private void _ToolTipHide(string message)
        {
            this.DxToolTipController.HideTip();
            RaiseToolTipChanged($"TreeList.HideToolTip({message})");
        }
        /// <summary>
        /// V controlleru ToolTipu došlo k události, pošli ji do našeho eventu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _ToolTipChanged(object sender, DxToolTipArgs args)
        {
            RaiseToolTipChanged(args);
        }
        #region HasMouse
        /// <summary>
        /// Panel má na sobě myš?
        /// </summary>
        public bool HasMouse
        {
            get { return _HasMouse; }
            private set
            {
                if (value != _HasMouse)
                {
                    _HasMouse = value;
                    OnHasMouseChanged();
                    HasMouseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasMouse;
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Panel.OnMouseEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.HasMouse = true;
        }
        /// <summary>
        /// Panel.OnMouseLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            Point location = this.PointToClient(Control.MousePosition);
            base.OnMouseLeave(e);
            if (!this.ClientRectangle.Contains(location))
                this.HasMouse = false;
        }
        #endregion
        #region IsFocused
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool IsFocused
        {
            get { return _IsFocused; }
            private set
            {
                if (value != _IsFocused)
                {
                    _IsFocused = value;
                    OnIsFocusedChanged();
                    IsFocusedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _IsFocused;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnIsFocusedChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler IsFocusedChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.IsFocused = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.IsFocused = false;
        }
        #endregion
        #endregion
        #region Řízení specifického vykreslení TreeNodu podle jeho nastavení: font, barvy, checkbox, atd
        /// <summary>
        /// Vytvoří new instanci pro řízení vzhledu TreeList
        /// </summary>
        /// <returns></returns>
        protected override DevExpress.XtraTreeList.ViewInfo.TreeListViewInfo CreateViewInfo()
        {
            if (CurrentViewInfo == null)
                CurrentViewInfo = new DxTreeListViewInfo(this);
            return CurrentViewInfo;
        }
        /// <summary>
        /// Při Dispose uvolním svůj lokální <see cref="CurrentViewInfo"/>
        /// </summary>
        protected void CurrentViewDispose()
        {
            if (CurrentViewInfo != null)
            {
                CurrentViewInfo.Dispose();
                CurrentViewInfo = null;
            }
        }
        /// <summary>
        /// Instance pro řízení vzhledu TreeList
        /// </summary>
        protected DxTreeListViewInfo CurrentViewInfo;
        /// <summary>
        /// Potomek pro řízení vzhledu s ohledem na [ne]vykreslení CheckBoxů
        /// </summary>
        protected class DxTreeListViewInfo : DevExpress.XtraTreeList.ViewInfo.TreeListViewInfo, IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="treeList"></param>
            public DxTreeListViewInfo(DxTreeListNative treeList) : base(treeList)
            {
                _Owner = treeList;
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public new void Dispose()
            {
                _Owner = null;
                base.Dispose();
            }
            private DxTreeListNative _Owner;
            /// <summary>
            /// Vrátí šířku pro CheckBox pro daný node
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            protected override int GetActualCheckBoxWidth(DevExpress.XtraTreeList.Nodes.TreeListNode node)
            {
                bool showSpace = _Owner.NeedNodeShowCheckBoxSpace(node);
                if (!showSpace) return 0;
                return base.GetActualCheckBoxWidth(node);
            }
        }
        /// <summary>
        /// Volá se před Check node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="prevState"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected override DevExpress.XtraTreeList.CheckNodeEventArgs RaiseBeforeCheckNode(DevExpress.XtraTreeList.Nodes.TreeListNode node, System.Windows.Forms.CheckState prevState, System.Windows.Forms.CheckState state)
        {
            DevExpress.XtraTreeList.CheckNodeEventArgs e = base.RaiseBeforeCheckNode(node, prevState, state);
            e.CanCheck = IsNodeCheckable(e.Node);
            return e;
        }
        /// <summary>
        /// Volá se před vykreslením Checkboxu
        /// </summary>
        /// <param name="e"></param>
        protected override void RaiseCustomDrawNodeCheckBox(DevExpress.XtraTreeList.CustomDrawNodeCheckBoxEventArgs e)
        {
            bool canCheckNode = IsNodeCheckable(e.Node);
            if (canCheckNode)
                return;
            e.ObjectArgs.State = DevExpress.Utils.Drawing.ObjectState.Disabled;
            e.Handled = true;

            base.RaiseCustomDrawNodeCheckBox(e);
        }
        /// <summary>
        /// Vrátí true, pokud daný node má zobrazovat prostor pro CheckBox.
        /// To je tehdy, když daný node má reálně zobrazovat CheckBox (<see cref="ITreeListNode.CanCheck"/> = true),
        /// anebo má definováno že má daný prostor alokovat (<see cref="ITreeListNode.AddVoidCheckSpace"/> = true).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected bool NeedNodeShowCheckBoxSpace(DevExpress.XtraTreeList.Nodes.TreeListNode node)
        {
            return NeedNodeShowCheckBoxSpace(_GetNodeInfo(node));
        }
        /// <summary>
        /// Vrátí true, pokud daný node má zobrazovat prostor pro CheckBox.
        /// To je tehdy, když daný node má reálně zobrazovat CheckBox (<see cref="ITreeListNode.CanCheck"/> = true),
        /// anebo má definováno že má daný prostor alokovat (<see cref="ITreeListNode.AddVoidCheckSpace"/> = true).
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        protected bool NeedNodeShowCheckBoxSpace(ITreeListNode nodeInfo)
        {
            bool showSpace = true;
            if (nodeInfo != null)
            {   // Podle režimu zobrazíme prostor CheckBoxu pro daný Node:
                var checkMode = this.CheckBoxMode;
                showSpace = (checkMode == TreeListCheckBoxMode.AllNodes || (checkMode == TreeListCheckBoxMode.SpecifyByNode && (nodeInfo.CanCheck || nodeInfo.AddVoidCheckSpace)));
            }
            return showSpace;
        }
        /// <summary>
        /// Vrací true, pokud daný node má zobrazovat CheckBox
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected bool IsNodeCheckable(DevExpress.XtraTreeList.Nodes.TreeListNode node)
        {
            return IsNodeCheckable(_GetNodeInfo(node));
        }
        /// <summary>
        /// Vrací true, pokud daný node má zobrazovat CheckBox
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        protected bool IsNodeCheckable(ITreeListNode nodeInfo)
        {
            bool isCheckable = true;
            if (nodeInfo != null)
            {   // Podle režimu povolíme Check pro daný Node:
                var checkMode = this.CheckBoxMode;
                isCheckable = (checkMode == TreeListCheckBoxMode.AllNodes || (checkMode == TreeListCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
            }
            return isCheckable;
        }
        /// <summary>
        /// Nastavení specifického stylu podle konkrétního Node (FontStyle, Colors, atd)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnNodeCellStyle(object sender, DevExpress.XtraTreeList.GetCustomNodeCellStyleEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo == null) return;

            if (nodeInfo.FontSizeDelta.HasValue)
                args.Appearance.FontSizeDelta = nodeInfo.FontSizeDelta.Value;
            if (nodeInfo.FontStyleDelta.HasValue)
                args.Appearance.FontStyleDelta = nodeInfo.FontStyleDelta.Value;
            if (nodeInfo.BackColor.HasValue)
            {
                args.Appearance.BackColor = nodeInfo.BackColor.Value;
                args.Appearance.Options.UseBackColor = true;
            }
            if (nodeInfo.ForeColor.HasValue)
            {
                args.Appearance.ForeColor = nodeInfo.ForeColor.Value;
                args.Appearance.Options.UseForeColor = true;
            }
        }
        /// <summary>
        /// Specifika krteslení CheckBox pro nodes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnCustomDrawNodeCheckBox(object sender, DevExpress.XtraTreeList.CustomDrawNodeCheckBoxEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {   // Podle režimu povolíme Check pro daný Node:
                var checkMode = this.CheckBoxMode;
                bool canCheck = (checkMode == TreeListCheckBoxMode.AllNodes || (checkMode == TreeListCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
                args.Handled = !canCheck;
            }
        }
        #endregion
        #region Interní události a jejich zpracování : Klávesa, Focus, DoubleClick, Editor, Specifika vykreslení, Expand, 
        /// <summary>
        /// Předtest, zda daná klávesa se má zpracovávat.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (_IsHotKeyInEditor(e.KeyData))
                e.IsInputKey = true;
        }
        /// <summary>
        /// Po stisku klávesy Vpravo a Vlevo se pracuje s Expanded nodů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            bool isHandled = _OnKeyDownFocusExpand(e);
            if (!isHandled)
                isHandled = _OnKeyDownClipboardDelete(e);
            if (!isHandled)
                isHandled = _OnKeyDownHotKey(e);

            if (isHandled)
                e.Handled = true;
            _CurrentKeyHandled = isHandled;
        }
        /// <summary>
        /// Řeší stisk klávesy - změny focusu a Expand nodu.
        /// <para/>
        /// Tyto události považujeme za natolik nativní akce v TreeListu, že je řešíme bez vlivu nějakých konfigurací.
        /// Jejich důsledkem je: změna vybraného nodu, Expand nebo Collapse nodu, stejně jako po kliknutí myší.
        /// Je na ně reagováno stejně jako na myš = v handlerech: <see cref="_OnFocusedNodeChanged(object, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs)"/>;
        /// <see cref="_OnBeforeExpand(object, DevExpress.XtraTreeList.BeforeExpandEventArgs)"/> atd...
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _OnKeyDownFocusExpand(KeyEventArgs e)
        {
            bool isHandled = false;
            DevExpress.XtraTreeList.Nodes.TreeListNode node;
            switch (e.KeyData)
            {
                case Keys.Right:
                    node = this.FocusedNode;
                    if (node != null && node.HasChildren && !node.Expanded)
                    {
                        node.Expanded = true;
                        isHandled = true;
                    }
                    break;
                case Keys.Left:
                    node = this.FocusedNode;
                    if (node != null)
                    {
                        if (node.HasChildren && node.Expanded)
                        {
                            node.Expanded = false;
                            isHandled = true;
                        }
                        else if (node.ParentNode != null)
                        {
                            this.FocusedNode = node.ParentNode;
                            isHandled = true;
                        }
                    }
                    break;
                case Keys.Up | Keys.Control:
                    if (!IsActiveEditor)
                    {   // Mimo editor: najedeme na první Node v naší úrovni:
                        node = this.FocusedNode;
                        if (node != null && node.ParentNode != null)
                        {
                            var newNode = node.ParentNode.FirstNode;
                            if (newNode != null)
                            {
                                this.FocusedNode = newNode;
                                isHandled = true;
                            }
                        }
                    }
                    break;
                case Keys.Down | Keys.Control:
                    if (!IsActiveEditor)
                    {   // Mimo editor: najedeme na poslední Node v naší úrovni:
                        node = this.FocusedNode;
                        if (node != null && node.ParentNode != null)
                        {
                            var newNode = node.ParentNode.LastNode;
                            if (newNode != null)
                            {
                                this.FocusedNode = newNode;
                                isHandled = true;
                            }
                        }
                    }
                    break;
            }
            return isHandled;
        }
        /// <summary>
        /// Po uvolnění klávesy ji můžeme předat do vyššího procesu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnKeyUp(object sender, KeyEventArgs e)
        {
            // Nebudeme reagovat v KeyUp, protože ostatní komponenty reagují v KeyDown.
            // A pak by vznikaly neshody: jedna komponenta zareaguje v KeyDown, přejde focus jinam,
            // a zdejší komponenta zareaguje v KeyUp = na stejnou klávesu mám dvě reakce!!!
        }
        private bool _CurrentKeyHandled;
        /// <summary>
        /// Po fokusu do konkrétního node se nastaví jeho Editable a volá se public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnFocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);

            _MainColumn.OptionsColumn.AllowEdit = (nodeInfo != null && nodeInfo.CanEdit);

            if (nodeInfo != null && !this.IsLocked)
            {
                this.RaiseNodeFocusedChanged(nodeInfo);
                if (!this.MultiSelectEnabled)
                    this._OnSelectedNodesChanged();                  // Pokud NENÍ nastaveno MultiSelectEnabled, pak TreeList nevyvolá svůj event _OnSelectionChanged, ale nás to může zajímat
            }
        }
        /// <summary>
        /// Po změně selectovaných nodů v <see cref="SelectedNodes"/>, volá se pouze při <see cref="MultiSelectEnabled"/> = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnSelectionChanged(object sender, EventArgs e)
        {
            if (!this.IsLocked)
                this._OnSelectedNodesChanged();
        }
        /// <summary>
        /// Volá se po každé změně stavu Selected.
        /// Zajistí nastavení odpovídajícího příznaku do <see cref="ITreeListNode.Selected"/>, a pokud dojde ke změně, pak volá <see cref="OnSelectedNodesChanged(DxTreeListNodeArgs)"/>.
        /// </summary>
        private void _OnSelectedNodesChanged()
        {
            var synchronize = _SynchronizeINodes();
            if (synchronize.Item1)
                this.RaiseSelectedNodesChanged();
        }
        /// <summary>
        /// Před rozbalením nodu:
        /// - lze tomu zabránit, pokud pro daný node evidujeme DoubleClick
        /// - volá se public event <see cref="NodeExpanded"/>,
        ///  a pokud node má nastaveno <see cref="ITreeListNode.LazyExpandable"/> = true, pak se ovlá ještě <see cref="LazyLoadChilds"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnBeforeExpand(object sender, DevExpress.XtraTreeList.BeforeExpandEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                nodeInfo.Expanded = true;
                this.RaiseNodeExpanded(nodeInfo);
                bool isExpanded = nodeInfo.Expanded;                 // DAJ 0070650: hodnota byla setována se zpožděním - asynchronně, opravno tam (v Noris.Clients.Controllers.ObservableObjectFacadeBusySupport), zde čteno do proměnné pro porovnání
                if (isExpanded)
                {   // Instance ITreeListNode mohla potlačit stav Expanded = true (nastavila zpátky false) anebo k tomu došlo v eventu, pak NEPROVEDEME další akce:
                    if (nodeInfo.LazyExpandable)
                        this.RaiseLazyLoadChilds(nodeInfo);
                }
                else
                {
                    args.CanExpand = false;
                }
            }
        }
        /// <summary>
        /// Po rozbalení nodu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnAfterExpand(object sender, DevExpress.XtraTreeList.NodeEventArgs e)
        {
        }
        /// <summary>
        /// Před zabalením nodu se volá public event <see cref="NodeCollapsed"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnBeforeCollapse(object sender, DevExpress.XtraTreeList.BeforeCollapseEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                nodeInfo.Expanded = false;
                this.RaiseNodeCollapsed(nodeInfo);
                bool isExpanded = nodeInfo.Expanded;                 // DAJ 0070650: hodnota byla setována se zpožděním - asynchronně, opravno tam (v Noris.Clients.Controllers.ObservableObjectFacadeBusySupport), zde čteno do proměnné pro porovnání
                if (!isExpanded)
                {   // Instance ITreeListNode mohla potlačit stav Expanded = false (nastavila zpátky true) anebo k tomu došlo v eventu, pak NEPROVEDEME další akce:
                }
                else
                {
                    args.CanCollapse = false;
                }
            }
        }
        /// <summary>
        /// Po zabalení nodu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnAfterCollapse(object sender, DevExpress.XtraTreeList.NodeEventArgs args)
        {
        }
        /// <summary>
        /// MouseClick vyvolá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnMouseClick(object sender, MouseEventArgs e)
        {
            var hit = _GetNodeHit(e.Location);
            if (hit.IsInImages)
            {
                ITreeListNode nodeInfo = this.FocusedNodeInfo;
                if (nodeInfo != null)
                {
                    if (_IsMainActionRunEvent(nodeInfo, hit.IsInCell))
                        this.RaiseNodeIconClick(nodeInfo, hit.PartType);
                    if (_IsMainActionExpandCollapse(nodeInfo, hit.IsInCell))
                        this._NodeExpandCollapse(hit);
                }
            }
        }
        /// <summary>
        /// MouseUp řeší kontextové menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnMouseUp(object sender, MouseEventArgs e)
        {
        }
        /// <summary>
        /// Uživatel by chtěl vidět kontextové menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnPopupMenuShowing(object sender, DevExpress.XtraTreeList.PopupMenuShowingEventArgs e)
        {
            this._ToolTipHide("OnPopupMenuShowing");

            e.Allow = false;

            var hitInfo = this._GetNodeHit(e.HitInfo);
            var treeNode = hitInfo.Node;
            if (treeNode != null && !treeNode.IsSelected && this.SelectNodeBeforeShowContextMenu)
            {
                this.SelectNode(treeNode);
                this.FocusedNode = treeNode;
            }

            this.RaiseShowContextMenu(hitInfo);
        }
        /// <summary>
        /// Doubleclick převolá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnDoubleClick(object sender, EventArgs e)
        {
            var hit = _GetNodeHit();
            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
            {
                if (_IsMainActionRunEvent(nodeInfo, hit.IsInCell))
                    this.RaiseNodeDoubleClick(nodeInfo, hit.PartType);
                if (_IsMainActionExpandCollapse(nodeInfo, hit.IsInCell))
                    this._NodeExpandCollapse(hit);
            }
        }
        /// <summary>
        /// V okamžiku zahájení editace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnShownEditor(object sender, EventArgs e)
        {
            if (this.IsActiveEditor)
            {
                this.EditorHelper.ActiveEditor.DoubleClick -= _OnEditorDoubleClick;
                this.EditorHelper.ActiveEditor.DoubleClick += _OnEditorDoubleClick;
                this.EditorHelper.ActiveEditor.KeyUp -= _OnEditorKeyUp;
                this.EditorHelper.ActiveEditor.KeyUp += _OnEditorKeyUp;
            }

            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.RaiseActivatedEditor(nodeInfo);
        }
        /// <summary>
        /// Po klávese (KeyUp) v editoru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnEditorKeyUp(object sender, KeyEventArgs e)
        {
            _OnKeyDownHotKey(e);
        }
        /// <summary>
        /// Ukončení editoru volá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
            {
                nodeInfo.TextEdited = this.EditingValue as string;
                this.RaiseNodeEdited(nodeInfo, this.EditingValue);
            }
        }
        /// <summary>
        /// Doubleclick v editoru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnEditorDoubleClick(object sender, EventArgs e)
        {
            this._ToolTipHide("OnEditorDoubleClick");
            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
            {
                nodeInfo.TextEdited = this.EditingValue as string;
                if (_IsMainActionRunEvent(nodeInfo, true))
                    this.RaiseEditorDoubleClick(nodeInfo, this.EditingValue);
                if (_IsMainActionExpandCollapse(nodeInfo, true))
                    this._NodeExpandCollapse(nodeInfo);
            }
        }
        /// <summary>
        /// Před změnou Checked stavu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnBeforeCheckNode(object sender, DevExpress.XtraTreeList.CheckNodeEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {   // Podle režimu povolíme Check pro daný Node:
                var checkMode = this.CheckBoxMode;
                args.CanCheck = (checkMode == TreeListCheckBoxMode.AllNodes || (checkMode == TreeListCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
            }
        }
        /// <summary>
        /// Po změně Checked stavu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnAfterCheckNode(object sender, DevExpress.XtraTreeList.NodeEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            var checkMode = this.CheckBoxMode;
            if (nodeInfo != null && (checkMode == TreeListCheckBoxMode.AllNodes || (checkMode == TreeListCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck)))
            {
                bool isChecked = args.Node.Checked;
                nodeInfo.NodeChecked = isChecked;
                this.RaiseNodeCheckedChange(nodeInfo, isChecked);
            }
        }
        /// <summary>
        /// Vrátí true pokud se po hlavní akci má provést RunEvent odpovídající aktuální aktivitě
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isOnText"></param>
        /// <returns></returns>
        private bool _IsMainActionRunEvent(ITreeListNode nodeInfo, bool isOnText)
        {
            switch (this.MainClickMode)
            {
                case NodeMainClickMode.RunEvent:
                case NodeMainClickMode.ExpandCollapseRunEvent:
                    return true;
                case NodeMainClickMode.AcceptNodeSetting:
                    if (nodeInfo != null)
                    {
                        var actions = nodeInfo.MainClickAction;
                        return ((actions.HasFlag(NodeMainClickActionType.IconClickRunEvent) && !isOnText) ||
                                (actions.HasFlag(NodeMainClickActionType.TextDoubleClickRunEvent) && isOnText));
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// Vrátí true pokud se po hlavní akci má provést explicitní Expand/Collapse nodu.
        /// Tedy: pokud TreeList samo má nastaveno DoubleClick (ted: OptionsBehavior.AllowExpandOnDblClick) = true;
        /// pak vracíme false - protože Expand/Collapse si provádí TreeList automaticky a nebudeme mu do toho mluvit.
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isOnText"></param>
        /// <returns></returns>
        private bool _IsMainActionExpandCollapse(ITreeListNode nodeInfo, bool isOnText)
        {
            switch (this.MainClickMode)
            {
                // Následující řádky jsou sice pravdivé, ale zbytečné:
                //    pro dané dva režimy vrátíme false = protože Expand/Collapse provádí TreeList automaticky,
                //    protože jsme mu nastavili: OptionsBehavior.AllowExpandOnDblClick = true (v metodě _MainClickModeSet(NodeMainClickMode mainClickMode)).
                // case NodeMainClickMode.ExpandCollapse:
                // case NodeMainClickMode.ExpandCollapseRunEvent:
                //    return false;

                case NodeMainClickMode.AcceptNodeSetting:
                    if (nodeInfo != null)
                    {
                        var actions = nodeInfo.MainClickAction;
                        return ((actions.HasFlag(NodeMainClickActionType.IconClickExpandCollapse) && !isOnText) ||
                                (actions.HasFlag(NodeMainClickActionType.TextDoubleClickExpandCollapse) && isOnText));
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// Zajistí provedení Expand/Collapse daného nodu
        /// </summary>
        /// <param name="hit"></param>
        private void _NodeExpandCollapse(TreeListVisualNodeInfo hit)
        {
            var treeNode = hit?.TreeHit?.Node;
            if (treeNode != null)
                treeNode.Expanded = !treeNode.Expanded;
        }
        /// <summary>
        /// Zajistí provedení Expand/Collapse daného nodu
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void _NodeExpandCollapse(ITreeListNode nodeInfo)
        {
            if (this._TryGetTreeNode(nodeInfo?.ItemId, out var treeNode))
                treeNode.Expanded = !treeNode.Expanded;
        }
        /// <summary>
        /// Najde node a jeho část, na které se nachází daný relativní bod.
        /// Pokud bod není daný (je null), pak použije aktuální pozici myši.
        /// </summary>
        /// <param name="relativePoint"></param>
        /// <returns></returns>
        private TreeListVisualNodeInfo _GetNodeHit(Point? relativePoint = null)
        {
            if (!relativePoint.HasValue)
                relativePoint = this.PointToClient(Control.MousePosition);
            DevExpress.XtraTreeList.TreeListHitInfo treeHit = this.CalcHitInfo(relativePoint.Value);
            return _GetNodeHit(treeHit);
        }
        /// <summary>
        /// Vrátí <see cref="TreeListVisualNodeInfo"/> pro daný <see cref="DevExpress.XtraTreeList.TreeListHitInfo"/>.
        /// Tedy dohledá odpovídající <see cref="ITreeListNode"/>.
        /// </summary>
        /// <param name="treeHit"></param>
        /// <returns></returns>
        private TreeListVisualNodeInfo _GetNodeHit(DevExpress.XtraTreeList.TreeListHitInfo treeHit)
        {
            ITreeListNode nodeInfo = this._GetNodeInfo(treeHit.Node);
            return new TreeListVisualNodeInfo(treeHit, nodeInfo);
        }
        /// <summary>
        /// Obsahuje true, pokud je zrovna aktivní editor textu v aktuálním nodu
        /// </summary>
        private bool IsActiveEditor { get { return (this.EditorHelper.ActiveEditor != null); } }
        #endregion
        #region Správa nodů (přidání, odebrání, smazání, změny)
        /// <summary>
        /// Přidá jeden node. Není to příliš efektivní. Raději používejme <see cref="AddNodes(IEnumerable{ITreeListNode}, bool, PreservePropertiesMode)"/>.
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        public void AddNode(ITreeListNode nodeInfo, int? atIndex = null)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<ITreeListNode, int?>(AddNode), nodeInfo, atIndex); return; }

            using (LockGui(true))
            {
                NodePair firstPair = null;
                this._AddNode(nodeInfo, ref firstPair, atIndex);
            }
        }
        /// <summary>
        /// Přidá řadu nodů. 
        /// Současné nody ponechává (pokud parametr <paramref name="clear"/> je false).
        /// Pokud je zadán parametr <paramref name="clear"/> = true, pak smaže všechny aktuální nody, a provede to v jednom vizuálním zámku s přidáním nodů.
        /// Lze tak přidat například jednu podvětev.
        /// Na konci provede Refresh.
        /// <br/>
        /// Po dobu provádění této akce nejsou volány žádné události.
        /// </summary>
        /// <param name="addNodes"></param>
        /// <param name="clear">Smazat všechny aktuální nody?</param>
        /// <param name="preserveProperties"></param>
        public void AddNodes(IEnumerable<ITreeListNode> addNodes, bool clear = false, PreservePropertiesMode preserveProperties = PreservePropertiesMode.None)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<ITreeListNode>, bool, PreservePropertiesMode>(AddNodes), addNodes, clear, preserveProperties); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(clear, null, addNodes, preserveProperties);
            }
        }
        /// <summary>
        /// Přidá řadu nodů, které jsou donačteny k danému parentu. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Nejprve najde daného parenta, a zruší z něj příznak LazyLoad (protože právě tímto načtením jsou jeho nody vyřešeny). Současně odebere "wait" node (prázdný node, simulující načítání dat).
        /// Pak teprve přidá nové nody.
        /// Na konci provede Refresh.
        /// <br/>
        /// Po dobu provádění této akce nejsou volány žádné události.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <param name="addNodes"></param>
        public void AddLazyLoadNodes(string parentNodeId, IEnumerable<ITreeListNode> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string, IEnumerable<ITreeListNode>>(AddLazyLoadNodes), parentNodeId, addNodes); return; }

            using (LockGui(true))
            {
                bool isAnySelected = this._RemoveLazyLoadFromParent(parentNodeId);
                var firstPair = this._RemoveAddNodes(false, null, addNodes, PreservePropertiesMode.None);

                var focusType = this.LazyLoadFocusNode;
                if (firstPair != null && (isAnySelected || focusType == TreeListLazyLoadFocusNodeType.FirstChildNode) && firstPair.HasTreeNode)
                    this.SetFocusedNode(firstPair.TreeNode);
                else if (focusType == TreeListLazyLoadFocusNodeType.ParentNode)
                {
                    var parentPair = this._GetNodePair(parentNodeId);
                    if (parentPair != null && parentPair.HasTreeNode)
                        this.FocusedNode = parentPair.TreeNode;
                }
            }
        }
        /// <summary>
        /// Selectuje daný Node
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void SetFocusToNode(ITreeListNode nodeInfo)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<ITreeListNode>(SetFocusToNode), nodeInfo); return; }

            using (LockGui(true))
            {
                if (nodeInfo != null && nodeInfo.Id >= 0 && this._NodesId.TryGetValue(nodeInfo.Id, out var nodePair) && nodePair.HasTreeNode)
                {
                    this.SetFocusedNode(nodePair.TreeNode);
                }
            }
        }
        /// <summary>
        /// Pole informací o vybraných nodech
        /// </summary>
        private NodePair[] SelectedNodePairs
        {
            get
            {
                List<NodePair> selectedNodePairs = new List<NodePair>();
                foreach (var treeNode in this.Selection)
                {
                    if (_TryGetNodePair(treeNode, out var nodeInfo))
                        selectedNodePairs.Add(nodeInfo);
                }
                return selectedNodePairs.ToArray();
            }
        }
        /// <summary>
        /// Pole všech nodů, které jsou aktuálně Selected
        /// </summary>
        public ITreeListNode[] SelectedNodes
        {
            get
            {
                List<ITreeListNode> selectedNodes = new List<ITreeListNode>();
                foreach (var treeNode in this.Selection)
                {
                    if (_TryGetNodeInfo(treeNode, out var nodeInfo))
                        selectedNodes.Add(nodeInfo);
                }
                return selectedNodes.ToArray();
            }
            set
            {
                try
                {
                    this.Selection.BeginSelect();
                    this.Selection.UnselectAll();
                    List<TreeListNode> treeNodes = new List<TreeListNode>();
                    var selectedNodes = value;
                    bool multiSelectEnabled = this.MultiSelectEnabled;
                    if (selectedNodes != null)
                    {
                        foreach (var nodeInfo in selectedNodes)
                        {
                            if (_TryGetTreeNode(nodeInfo?.ItemId, out var treeNode))
                                treeNodes.Add(treeNode);
                            if (!multiSelectEnabled && treeNodes.Count >= 1)
                                break;
                        }
                    }
                    this.Selection.SelectNodes(treeNodes);
                }
                finally
                {
                    this.Selection.EndSelect();
                }
            }
        }
        /// <summary>
        /// Odebere jeden daný node, podle klíče. Na konci provede Refresh.
        /// Pro odebrání více nodů je lepší použít <see cref="RemoveNodes(IEnumerable{string})"/>.
        /// </summary>
        /// <param name="removeNodeKey"></param>
        public void RemoveNode(string removeNodeKey)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string>(RemoveNode), removeNodeKey); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(false, new string[] { removeNodeKey }, null, PreservePropertiesMode.None);
            }
        }
        /// <summary>
        /// Odebere řadu nodů, podle klíče. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        public void RemoveNodes(IEnumerable<string> removeNodeKeys)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<string>>(RemoveNodes), removeNodeKeys); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(false, removeNodeKeys, null, PreservePropertiesMode.None);
            }
        }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        /// <param name="preserveProperties"></param>
        public void RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes, PreservePropertiesMode preserveProperties = PreservePropertiesMode.None)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<string>, IEnumerable<ITreeListNode>, PreservePropertiesMode>(RemoveAddNodes), removeNodeKeys, addNodes, preserveProperties); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(false, removeNodeKeys, addNodes, preserveProperties);
            }
        }
        /// <summary>
        /// Smaže všechny nodes. Na konci provede Refresh.
        /// </summary>
        public new void ClearNodes()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(ClearNodes)); return; }

            using (LockGui(true))
            {
                _ClearNodes();
            }
        }
        /// <summary>
        /// Zajistí refresh jednoho daného nodu. 
        /// Pro refresh více nodů použijme <see cref="RefreshNodes(IEnumerable{ITreeListNode})"/>!
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void RefreshNode(ITreeListNode nodeInfo)
        {
            if (nodeInfo == null) return;
            if (nodeInfo.Id < 0) throw new ArgumentException($"Cannot refresh node '{nodeInfo.ItemId}': '{nodeInfo.Text}' if the node is not in TreeList.");

            if (this.InvokeRequired) { this.Invoke(new Action<ITreeListNode>(RefreshNode), nodeInfo); return; }

            using (LockGui(true))
            {
                this._RefreshNode(nodeInfo);
            }
        }
        /// <summary>
        /// Zajistí refresh daných nodů.
        /// </summary>
        /// <param name="nodes"></param>
        public void RefreshNodes(IEnumerable<ITreeListNode> nodes)
        {
            if (nodes == null) return;

            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<ITreeListNode>>(RefreshNodes), nodes); return; }

            using (LockGui(true))
            {
                foreach (var nodeInfo in nodes)
                    this._RefreshNode(nodeInfo);
            }
        }
        /// <summary>
        /// Projde všechny nody tohoto Tree, rekurzivně, a pro každý node vyvolá danou akci.
        /// Pokud je dodána funkce <paramref name="scanSubNodesFilter"/>, pak rekurzi pro SubNodes provede jen tehdy, když daný node vyhoví dané funkci. 
        /// Pokud funkce není dodána, provede se rekurze pro každý node, který má SubNodes.
        /// </summary>
        /// <param name="nodeAction">Akce pro každý node</param>
        /// <param name="scanSubNodesFilter">Filtr na nody, jejichž SubNodes se mají rekurzivně procházet.</param>
        private void _ScanNodes(Action<DevExpress.XtraTreeList.Nodes.TreeListNode> nodeAction, Func<DevExpress.XtraTreeList.Nodes.TreeListNode, bool> scanSubNodesFilter = null)
        {
            if (nodeAction == null) return;
            _ScanNodes(this.Nodes, nodeAction, scanSubNodesFilter);
        }
        /// <summary>
        /// Pro každý node z dodané kolekce provede danou akci, a rekurzivně vyvolá this metodu pro kolekci SubNodes každého nodu.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="nodeAction">Akce pro každý node</param>
        /// <param name="scanSubNodesFilter">Filtr na nody, jejichž SubNodes se mají rekurzivně procházet.</param>
        private void _ScanNodes(DevExpress.XtraTreeList.Nodes.TreeListNodes nodes, Action<DevExpress.XtraTreeList.Nodes.TreeListNode> nodeAction, Func<DevExpress.XtraTreeList.Nodes.TreeListNode, bool> scanSubNodesFilter = null)
        {
            if (nodes == null || nodes.Count == 0) return;
            bool hasFilter = (scanSubNodesFilter != null);
            DevExpress.XtraTreeList.Nodes.TreeListNode[] array = nodes.ToArray();
            foreach (DevExpress.XtraTreeList.Nodes.TreeListNode node in array)
            {
                nodeAction(node);
                if (!hasFilter || scanSubNodesFilter(node))
                    _ScanNodes(node.Nodes, nodeAction, scanSubNodesFilter);
            }
        }
        /// <summary>
        /// Zajistí synchronizaci hodnot <see cref="ITreeListNode.Selected"/> a <see cref="ITextItem.Checked"/> ve všech evidovaných nodech podle aktuálního stavu v komponentě.
        /// Vrací Tuple, jehož:
        /// Item1 = byla nalezena změna v hodnotě <see cref="ITreeListNode.Selected"/>,
        /// Item2 = byla nalezena změna v hodnotě <see cref="ITextItem.Checked"/>.
        /// </summary>
        /// <remarks>Nebyl by problém vracet pole nodů, kde došlo ke změně, ale zatím není důvod.</remarks>
        /// <returns></returns>
        private Tuple<bool, bool> _SynchronizeINodes()
        {
            bool isChangedSelect = false;
            bool isChangedCheck = false;
            foreach (var nodePair in this._NodesId.Values)
            {
                var node = nodePair.TreeNode;
                if (node == null) continue;

                bool nodeIsSelected = node.IsSelected;
                if (nodePair.NodeInfo.Selected != nodeIsSelected)
                {
                    nodePair.NodeInfo.Selected = nodeIsSelected;
                    isChangedSelect = true;
                }

                bool nodeIsChecked = node.Checked;
                if (nodePair.NodeInfo.NodeChecked != nodeIsChecked)
                {
                    nodePair.NodeInfo.NodeChecked = nodeIsChecked;
                    isChangedCheck = true;
                }
            }

            return new Tuple<bool, bool>(isChangedSelect, isChangedCheck);
        }
        #endregion
        #region Provádění akce v jednom zámku
        /// <summary>
        /// Zajistí provedení dodané akce s argumenty v GUI threadu a v jednom vizuálním zámku s jedním Refreshem na konci.
        /// <para/>
        /// Víš jak se píše Delegate pro cílovou metodu s konkrétním parametrem typu bool? 
        /// RunInLock(new Action&lt;bool&gt;(volaná_metoda), hodnota_bool)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public void RunInLock(Delegate method, params object[] args)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<Delegate, object[]>(RunInLock), method, args); return; }

            using (LockGui(true))
            {
                method.Method.Invoke(method.Target, args);
            }
        }
        /// <summary>
        /// Po dobu using bloku zamkne GUI this controlu. Při Dispose jej odemkne a volitelně zajistí Refresh.
        /// Pokud je metoda volána rekurzivně = v době, kdy je objekt zamčen, pak vrátí "void" zámek = vizuálně nefunkční, ale formálně korektní.
        /// </summary>
        /// <returns></returns>
        protected IDisposable LockGui(bool withRefresh)
        {
            if (IsLocked) return new LockTreeListGuiInfo();
            return new LockTreeListGuiInfo(this, withRefresh);
        }
        /// <summary>
        /// Příznak, zda je objekt odemčen (false) nebo zamčen (true).
        /// Objekt se zamkne vytvořením první instance <see cref="LockTreeListGuiInfo"/>, 
        /// následující vytváření i Dispose nových instancí týchž objektů již stav nezmění, 
        /// a až Dispose posledního zámku objekt zase odemkne a volitelně provede Refresh.
        /// </summary>
        protected bool IsLocked;
        /// <summary>
        /// IDisposable objekt pro párové operace se zamknutím / odemčením GUI
        /// </summary>
        protected class LockTreeListGuiInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor pro "vnořený" zámek, který nic neprovádí
            /// </summary>
            public LockTreeListGuiInfo() { }
            /// <summary>
            /// Konstruktor standardní
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="withRefresh"></param>
            public LockTreeListGuiInfo(DxTreeListNative owner, bool withRefresh)
            {
                if (owner != null)
                {
                    owner.IsLocked = true;
                    ((System.ComponentModel.ISupportInitialize)(owner)).BeginInit();
                    owner.BeginUnboundLoad();

                    _Owner = owner;
                    _SilentMode = owner._SilentMode;
                    _WithRefresh = withRefresh;
                    _FocusedNodeId = owner.FocusedNodeInfo?.ItemId;

                    _Owner._SilentMode = true;
                }
            }
            void IDisposable.Dispose()
            {
                var owner = _Owner;
                if (owner != null)
                {
                    string focusedNodeFullId = owner.FocusedNodeFullId;
                    int topVisibleIndex = owner.TopVisibleNodeIndex;
                    int topVisiblePixel = owner.TopVisibleNodePixel;

                    owner._SilentMode = _SilentMode;
                    owner.EndUnboundLoad();
                    ((System.ComponentModel.ISupportInitialize)(owner)).EndInit();

                    owner.TopVisibleNodeIndex = topVisibleIndex;
                    owner.TopVisibleNodePixel = topVisiblePixel;

                    if (_WithRefresh)
                        owner.Refresh();

                    owner.IsLocked = false;

                    var focusedNodeInfo = owner.FocusedNodeInfo;
                    string oldNodeId = _FocusedNodeId;
                    string newNodeId = focusedNodeInfo?.ItemId;
                    if (!String.Equals(oldNodeId, newNodeId))
                        owner.RaiseNodeFocusedChanged(focusedNodeInfo);
                }
            }
            private DxTreeListNative _Owner;
            private bool _SilentMode;
            private bool _WithRefresh;
            private string _FocusedNodeId;
        }
        #endregion
        #region Private sféra - přidávání nodů, odebírání, Clear, tvorba nodu...
        /// <summary>
        /// Smaže všechny nody (podle <paramref name="clearAll"/>).
        /// Odebere nody ze stromu a z evidence (podle <paramref name="removeNodeKeys"/>).
        /// Přidá více node do stromu a do evidence (z <paramref name="addNodes"/>).
        /// Neřeší blokování GUI.
        /// Metoda vrací první vytvořený <see cref="NodePair"/>.
        /// <br/>
        /// Po dobu provádění této akce nejsou volány žádné události.
        /// </summary>
        /// <param name="clearAll"></param>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        /// <param name="preserveProperties"></param>
        private NodePair _RemoveAddNodes(bool clearAll, IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes, PreservePropertiesMode preserveProperties)
        {
            NodePair nodePair = null;
            bool oldSilentMode = _SilentMode;
            try
            {
                _SilentMode = true;
                nodePair = _RemoveAddNodesSilent(clearAll, removeNodeKeys, addNodes, preserveProperties);
            }
            finally
            {
                _SilentMode = oldSilentMode;
            }
            return nodePair;
        }
        /// <summary>
        /// Smaže všechny nody (podle <paramref name="clearAll"/>).
        /// Odebere nody ze stromu a z evidence (podle <paramref name="removeNodeKeys"/>).
        /// Přidá více node do stromu a do evidence (z <paramref name="addNodes"/>).
        /// Neřeší blokování GUI.
        /// Metoda vrací první vytvořený <see cref="NodePair"/>.
        /// </summary>
        /// <param name="clearAll"></param>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        /// <param name="preserveProperties"></param>
        private NodePair _RemoveAddNodesSilent(bool clearAll, IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes, PreservePropertiesMode preserveProperties)
        {
            NodePair firstPair = null;

            string focusedNodeFullId = this.FocusedNodeFullId;
            int topVisibleIndex = this.TopVisibleNodeIndex;
            int topVisiblePixel = this.TopVisibleNodePixel;

            // Clear:
            if (clearAll)
            {
                this._ClearNodes();
            }
            // anebo Remove:
            else if (removeNodeKeys != null)
            {
                foreach (var nodeKey in removeNodeKeys)
                    this._RemoveNode(nodeKey);
            }

            // Add:
            if (addNodes != null)
            {
                // Tyto nody by měly být selectovány:
                //   1. ty z nově dodaných, které mají nastaveno ITreeListNode.Selected = true
                //   2. ty stávající (SelectedNodes)
                List<ITreeListNode> selectedNodes = new List<ITreeListNode>();
                selectedNodes.AddRange(addNodes.Where(n => n.Selected));
                selectedNodes.AddRange(this.SelectedNodes);

                // Fyzicky přidat nody:
                foreach (var node in addNodes)
                    this._AddNode(node, ref firstPair, null);

                // Expand nody: teď už by měly mít svoje Childs přítomné v TreeList:
                foreach (var node in addNodes.Where(n => n.Expanded))
                {
                    if (this._NodesId.TryGetValue(node.Id, out var expandNodePair) && expandNodePair.HasTreeNode)
                        expandNodePair.TreeNode.Expanded = true;
                }

                // Selectovat požadované nody:
                this.SelectedNodes = selectedNodes.ToArray();
            }

            if (preserveProperties.HasFlag(PreservePropertiesMode.SelectedItems)) this.FocusedNodeFullId = focusedNodeFullId;
            if (preserveProperties.HasFlag(PreservePropertiesMode.FirstVisibleItem)) this.TopVisibleNodeIndex = topVisibleIndex;
            if (preserveProperties.HasFlag(PreservePropertiesMode.FirstVisiblePixel)) this.TopVisibleNodePixel = topVisiblePixel;

            return firstPair;
        }
        /// <summary>
        /// Vytvoří nový jeden vizuální node podle daných dat, a přidá jej do vizuálního prvku a do interní evidence, neřeší blokování GUI
        /// </summary>
        /// <param name="nodeInfo">Data pro tvorbu nodu</param>
        /// <param name="firstPair">Ref první vytvořený pár</param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        private void _AddNode(ITreeListNode nodeInfo, ref NodePair firstPair, int? atIndex)
        {
            if (nodeInfo == null) return;

            NodePair nodePair = _AddNodeOne(nodeInfo, false, atIndex);         // Daný node (z aplikace) vloží do Tree a vrátí
            if (firstPair == null && nodePair != null)
                firstPair = nodePair;

            if (nodeInfo.LazyExpandable)
                _AddNodeLazyLoad(nodeInfo);                                    // Pokud node má nastaveno LazyExpandable, pak pod něj vložím jako jeho Child nový node, reprezentující "načítání z databáze"
        }
        private void _InsertNodes(IEnumerable<ITreeListNode> nodeInfos)
        { }
        /// <summary>
        /// Pod daného parenta přidá Child node typu LazyLoad
        /// </summary>
        /// <param name="parentNode"></param>
        private void _AddNodeLazyLoad(ITreeListNode parentNode)
        {
            string lazyChildId = parentNode.ItemId + "___«LazyLoadChildNode»___";
            string text = this.LazyLoadNodeText ?? "...";
            string imageName = this.LazyLoadNodeImageName;
            ITreeListNode lazyNode = new DataTreeListNode(lazyChildId, parentNode.ItemId, text, nodeType: NodeItemType.OnExpandLoading, imageName: imageName, fontStyleDelta: FontStyle.Italic);
            NodePair nodePair = _AddNodeOne(lazyNode, true, null);             // Daný node (z aplikace) vloží do Tree a vrátí
        }
        /// <summary>
        /// Fyzické přidání jednoho node do TreeList a do evidence
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isLazyChild"></param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        private NodePair _AddNodeOne(ITreeListNode nodeInfo, bool isLazyChild, int? atIndex)
        {
            // Kontrola duplicity raději předem:
            string nodeId = nodeInfo.ItemId;
            if (nodeId != null && this._NodesKey.ContainsKey(nodeId)) throw new ArgumentException($"It is not possible to add an element because an element with the same key '{nodeId}' already exists in the TreeList.");

            // Zakonzervuji CurrentRootNodeVisible:
            if (NativeNodesCount == 0) _CurrentRootNodeVisible = RootNodeVisible;

            // Vyřeším situaci, kdy přidávám node (jiný než fiktivní NodeItemType.OnExpandLoading) a přitom náš Parent existuje, má příznak LazyExpandable, a mezi jeho Child nody je přítomný fiktivní NodeItemType.OnExpandLoading.
            // To je v Nephrite běžný stav, kdy někdo nastaví u nodu "Parent" příznak LazyExpandable = true, my na základě toho příznaku vygenerujeme fiktivní NodeItemType.OnExpandLoading,
            //  a mezi následujícími přidanými nody budou reálné Child nody toho Parent nodu:
            _RemoveLazyLoadFromParent(nodeInfo);

            // Pokud budeme node vytvářet (=není Root, anebo máme Root node generovat):
            DevExpress.XtraTreeList.Nodes.TreeListNode treeNode = null;
            string parentNodeFullId = nodeInfo.ParentNodeFullId;
            if (!String.IsNullOrEmpty(parentNodeFullId) || _CurrentRootNodeVisible)
            {
                // Vytvoříme TreeListNode:
                object nodeData = new object[] { nodeInfo.Text };
                var parentPair = this._GetNodePair(parentNodeFullId);

                // Přidám nový node buď jako Child do existujícího Parenta, anebo jako nový další do úrovně Root:
                if (parentPair != null && parentPair.HasTreeNode)
                    treeNode = parentPair.TreeNode.Nodes.Add(nodeData);
                else
                    treeNode = this.AppendNode(nodeData, null);

                if (atIndex.HasValue)
                    this.SetNodeIndex(treeNode, atIndex.Value);

                _FillTreeNode(treeNode, nodeInfo, false);
            }

            // 2. Propojíme vizuální node a datový objekt - pouze přes int ID, nikoli vzájemné reference:
            int id = ++_LastId;
            NodePair nodePair = new NodePair(this, id, nodeInfo, treeNode, isLazyChild);

            // 3. Uložíme Pair do indexů podle ID a podle Key:
            this._NodesId.Add(nodePair.Id, nodePair);
            if (nodePair.NodeId != null) this._NodesKey.Add(nodePair.NodeId, nodePair);

            return nodePair;
        }
        /// <summary>
        /// Refresh jednoho Node
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void _RefreshNode(ITreeListNode nodeInfo)
        {
            if (nodeInfo != null && nodeInfo.ItemId != null && this._NodesKey.TryGetValue(nodeInfo.ItemId, out var nodePair) && nodePair.HasTreeNode)
            {
                _FillTreeNode(nodePair.TreeNode, nodePair.NodeInfo, true);
            }
        }
        /// <summary>
        /// Do daného <see cref="DataTreeListNode"/> vepíše všechny potřebné informace z datového <see cref="ITreeListNode"/>.
        /// Jde o: text, stav zaškrtnutí, ikony, rozbalení nodu.
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="nodeInfo"></param>
        /// <param name="canExpand"></param>
        private void _FillTreeNode(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, ITreeListNode nodeInfo, bool canExpand)
        {
            treeNode.SetValue(0, nodeInfo.Text);
            treeNode.Checked = nodeInfo.CanCheck && nodeInfo.NodeChecked;

            int imageIndex = _GetImageIndex(nodeInfo.ImageDynamicDefault, -1);
            treeNode.ImageIndex = imageIndex;                                                      // ImageIndex je vlevo, a může se změnit podle stavu Seleted
            treeNode.SelectImageIndex = _GetImageIndex(nodeInfo.ImageDynamicSelected, imageIndex); // SelectImageIndex je ikona ve stavu Nodes.Selected, zobrazená vlevo místo ikony ImageIndex
            treeNode.StateImageIndex = _GetImageIndex(nodeInfo.ImageName, -1);                     // StateImageIndex je vpravo, a nereaguje na stav Selected

            if (canExpand) treeNode.Expanded = nodeInfo.Expanded;                                  // Expanded se nastavuje pouze z Refreshe (tam má smysl), ale ne při tvorbě (tam ještě nemáme ChildNody)
        }
        /// <summary>
        /// Metoda najde Parent node dodaného nodu, a pokud daný Parent node má příznak <see cref="ITreeListNode.LazyExpandable"/>, 
        /// pak z jeho Childs nodů odebere ty nody, které jsou typu <see cref="NodeItemType.OnExpandLoading"/>.
        /// Metoda nastaví do Parent nodu příznak <see cref="ITreeListNode.LazyExpandable"/> = false: tento Parent už nemá fungovat jako LazyExpandable,
        /// protože právě do něj plníme reálné subnody a jeho fiktivní <see cref="NodeItemType.OnExpandLoading"/> nody jsme právě odebrali.
        /// <para/>
        /// Tato metoda se provede jen tehdy, pokud na vstupu je node jiného typu než <see cref="NodeItemType.OnExpandLoading"/>.
        /// <para/>
        /// Metoda vrátí true, pokud některý z odebraných prvků byl Selected.
        /// </summary>
        /// <param name="childNodeInfo">Node, z jehož Parenta máme odebrat ty nody, které jsou typu <see cref="NodeItemType.OnExpandLoading"/></param>
        /// <returns></returns>
        private bool _RemoveLazyLoadFromParent(ITreeListNode childNodeInfo)
        {
            if (childNodeInfo == null || String.IsNullOrEmpty(childNodeInfo.ParentNodeFullId) || childNodeInfo.NodeType == NodeItemType.OnExpandLoading) return false;
            return _RemoveLazyLoadFromParent(childNodeInfo.ParentNodeFullId);
        }
        /// <summary>
        /// Metoda najde daný Parent node (podle daného ID), a pokud daný Parent node má příznak <see cref="ITreeListNode.LazyExpandable"/>, 
        /// pak z jeho Childs nodů odebere ty nody, které jsou typu <see cref="NodeItemType.OnExpandLoading"/>.
        /// Metoda nastaví do tohoto Parent nodu příznak <see cref="ITreeListNode.LazyExpandable"/> = false: tento Parent už nemá fungovat jako LazyExpandable,
        /// protože právě do něj plníme reálné subnody a jeho fiktivní <see cref="NodeItemType.OnExpandLoading"/> nody jsme právě odebrali.
        /// <para/>
        /// Tato metoda se provede jen tehdy, pokud na vstupu je node jiného typu než <see cref="NodeItemType.OnExpandLoading"/>.
        /// <para/>
        /// Metoda vrátí true, pokud některý z odebraných prvků byl Selected.
        /// </summary>
        /// <param name="parentNodeFullId"></param>
        /// <returns></returns>
        private bool _RemoveLazyLoadFromParent(string parentNodeFullId)
        {
            ITreeListNode parentNodeInfo = _GetNodeInfo(parentNodeFullId);
            if (parentNodeInfo == null || !parentNodeInfo.LazyExpandable) return false;

            parentNodeInfo.LazyExpandable = false;

            // Najdu stávající Child nody daného Parenta a všechny je odeberu. Měl by to být pouze jeden node = simulující načítání dat, přidaný v metodě _AddNodeLazyLoad():
            NodePair[] lazyChilds = this._NodesId.Values.Where(p => p.IsLazyChild && p.NodeInfo.ParentNodeFullId == parentNodeFullId).ToArray();
            bool isAnySelected = (lazyChilds.Length > 0 && lazyChilds.Any(p => p.IsTreeNodeSelected));
            _RemoveAddNodes(false, lazyChilds.Select(p => p.NodeId), null, PreservePropertiesMode.None);

            return isAnySelected;
        }
        /// <summary>
        /// Smaže všechny nodes, neřeší blokování GUI
        /// </summary>
        private void _ClearNodes()
        {
            base.ClearNodes();

            foreach (NodePair nodePair in this._NodesId.Values)
                nodePair.ReleasePair();

            this._NodesId.Clear();
            this._NodesKey.Clear();

            this._NodeImageType = ResourceContentType.None;

            _LastId = 0;
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="id"></param>
        private void _RemoveNode(int id)
        {
            if (id < 0) throw new ArgumentException($"Argument 'nodeId' is negative in {CurrentClassName}.RemoveNode() method.");
            if (!this._NodesId.TryGetValue(id, out var nodePair)) throw new ArgumentException($"Node with ID = '{id}' is not found in {CurrentClassName} nodes."); ;
            _RemoveNode(nodePair);
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="fullNodeId"></param>
        private void _RemoveNode(string fullNodeId)
        {
            if (fullNodeId == null) throw new ArgumentException($"Argument 'nodeKey' is null in {CurrentClassName}.RemoveNode() method.");
            if (this._NodesKey.TryGetValue(fullNodeId, out var nodePair))          // Nebudu hlásit Exception při smazání neexistujícího nodu, může k tomu dojít při multithreadu...
                _RemoveNode(nodePair);
        }
        /// <summary>
        /// Odebere jeden node ze stromu a z evidence, neřeší blokování GUI.
        /// Klíčem je string, který se jako unikátní ID používá v aplikačních datech.
        /// Tato metoda si podle stringu najde int ID i záznamy v evidenci.
        /// </summary>
        /// <param name="nodePair"></param>
        private void _RemoveNode(NodePair nodePair)
        {
            if (nodePair == null) return;

            // Odebrat z indexů:
            if (this._NodesId.ContainsKey(nodePair.Id)) this._NodesId.Remove(nodePair.Id);
            if (nodePair.NodeId != null && this._NodesKey.ContainsKey(nodePair.NodeId)) this._NodesKey.Remove(nodePair.NodeId);

            // Reference na vizuální prvek:
            var treeNode = nodePair.TreeNode;

            // Rozpadnout pár:
            nodePair.ReleasePair();

            // Odebrat z vizuálního objektu:
            if (treeNode != null) treeNode.Remove();
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, podle <paramref name="fullNodeId"/>
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <returns></returns>
        private NodePair _GetNodePair(string fullNodeId)
        {
            _TryGetNodePair(fullNodeId, out NodePair nodePair);
            return nodePair;
        }
        /// <summary>
        /// Vyhledá data nodu pro daný node, podle <paramref name="fullNodeId"/>
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <param name="nodePair"></param>
        /// <returns></returns>
        private bool _TryGetNodePair(string fullNodeId, out NodePair nodePair)
        {
            nodePair = null;
            if (fullNodeId != null)
                return this._NodesKey.TryGetValue(fullNodeId, out nodePair);
            return false;
        }
        /// <summary>
        /// Vyhledá párová data nodu (Info + TreeNode) pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int.
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="nodePair"></param>
        /// <returns></returns>
        private bool _TryGetNodePair(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, out NodePair nodePair)
        {
            int id = ((treeNode != null && treeNode.Tag is int) ? (int)treeNode.Tag : -1);
            return _TryGetNodePair(id, out nodePair);
        }
        /// <summary>
        /// Vyhledá párová data nodu (Info + TreeNode) pro daný node, pro jeho ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nodePair"></param>
        /// <returns></returns>
        private bool _TryGetNodePair(int id, out NodePair nodePair)
        {
            nodePair = null;
            bool result = false;
            result = (id >= 0 && this._NodesId.TryGetValue(id, out nodePair));
            return result;
        }
        /// <summary>
        /// Vrátí vizuální node podle <paramref name="fullNodeId"/>
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <returns></returns>
        private DevExpress.XtraTreeList.Nodes.TreeListNode _GetTreeNode(string fullNodeId)
        {
            _TryGetTreeNode(fullNodeId, out DevExpress.XtraTreeList.Nodes.TreeListNode treeNode);
            return treeNode;
        }
        /// <summary>
        /// Vyhledá vizuální node podle <paramref name="fullNodeId"/>
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        private bool _TryGetTreeNode(string fullNodeId, out DevExpress.XtraTreeList.Nodes.TreeListNode treeNode)
        {
            treeNode = null;
            bool result = false;
            if (fullNodeId != null && this._NodesKey.TryGetValue(fullNodeId, out var nodePair) && nodePair != null && nodePair.HasTreeNode)
            {
                treeNode = nodePair.TreeNode;
                result = true;
            }
            return result;
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode)
        {
            _TryGetNodeInfo(treeNode, out var nodeInfo);
            return nodeInfo;
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, podle NodeId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(int id)
        {
            _TryGetNodeInfo(id, out var nodeInfo);
            return nodeInfo;
        }
        /// <summary>
        /// Vyhledá data nodu pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int.
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        private bool _TryGetNodeInfo(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, out ITreeListNode nodeInfo)
        {
            int id = ((treeNode != null && treeNode.Tag is int) ? (int)treeNode.Tag : -1);
            return _TryGetNodeInfo(id, out nodeInfo);
        }
        /// <summary>
        /// Vyhledá data nodu pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        private bool _TryGetNodeInfo(int id, out ITreeListNode nodeInfo)
        {
            nodeInfo = null;
            bool result = false;
            if (id >= 0 && this._NodesId.TryGetValue(id, out var nodePair))
            {
                nodeInfo = nodePair.NodeInfo;
                result = true;
            }
            return result;
        }
        /// <summary>
        /// Vrací data nodu podle jeho klíče
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(string fullNodeId)
        {
            if (fullNodeId != null && this._NodesKey.TryGetValue(fullNodeId, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrátí ID nodu pro daný klíč
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <returns></returns>
        private int _GetNodeId(string fullNodeId)
        {
            if (fullNodeId != null && this._NodesKey.TryGetValue(fullNodeId, out var nodePair)) return nodePair.Id;
            return -1;
        }
        /// <summary>
        /// Vrátí aktuální hodnotu interního ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
        /// Tato hodnota se mění při odebrání nodu z TreeList. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
        /// Pokud this instance nemá TreeNode, pak vrací -1.
        /// </summary>
        /// <param name="fullNodeId"></param>
        /// <returns></returns>
        private int _GetCurrentTreeNodeId(string fullNodeId)
        {
            int id = -1;
            if (fullNodeId != null && this._NodesKey.TryGetValue(fullNodeId, out var nodePair) && nodePair.HasTreeNode)
                id = nodePair.CurrentTreeNodeId;
            return id;
        }
        /// <summary>
        /// FullName aktuální třídy
        /// </summary>
        protected string CurrentClassName { get { return this.GetType().FullName; } }
        /// <summary>
        /// Aktuální hodnota pro zobrazení Root nodu.
        /// Nastavuje se před přidáním prvního nodu (v metodě <see cref="_AddNode(ITreeListNode, ref NodePair, int?)"/>) podle hodnoty <see cref="RootNodeVisible"/>.
        /// Jakmile v evidenci je už nějaký node, pak se tato hodnota nemění.
        /// </summary>
        public bool CurrentRootNodeVisible { get { return (NativeNodesCount == 0 ? RootNodeVisible : _CurrentRootNodeVisible);  /* Dokud nemám žádné nody, pak vracím RootNodeVisible. Jakmile už mám nody, vracím 'konzervu'. */ } }
        private bool _CurrentRootNodeVisible;
        /// <summary>
        /// Počet aktuálních nodů. Jsou v tom započítané i nody typu LazyLoad, AddNew, RunLoad.
        /// </summary>
        protected int NativeNodesCount { get { return _NodesId.Count; } }
        /// <summary>
        /// Počet aktuálních fyzických nodů
        /// </summary>
        public int NodesCount { get { return _NodesId.Count(n => n.Value.HasTreeNode); } }
        #endregion
        #region DataExchange
        private void DataExchangeInit()
        {
            ExchangeCurrentDataId = DxComponent.CreateGuid();
        }
        /// <summary>
        /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
        /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
        /// </summary>
        public string ExchangeCurrentDataId { get; set; }
        /// <summary>
        /// Režim výměny dat při pokusu o vkládání do tohoto objektu.
        /// Pokud některý jiný objekt provedl Ctrl+C, pak svoje data vložil do balíčku <see cref="DataExchangeContainer"/>,
        /// přidal k tomu svoje ID controlu (jako zdejší <see cref="ExchangeCurrentDataId"/>) do <see cref="DataExchangeContainer.DataSourceId"/>,
        /// do balíčku se přidalo ID aplikace do <see cref="DataExchangeContainer.ApplicationGuid"/>, a tato data jsou uložena v Clipboardu.
        /// <para/>
        /// Pokud nyní zdejší control zaeviduje klávesu Ctrl+V, pak zjistí, zda v Clipboardu existuje balíček <see cref="DataExchangeContainer"/>,
        /// a pokud ano, pak prověří, zda this control může akceptovat data ze zdroje v balíčku uvedeného, na základě nastavení režimu výměny v <see cref="ExchangeCrossType"/>
        /// a ID zdrojového controlu podle <see cref="ExchangeAcceptSourceDataId"/>.
        /// </summary>
        public DataExchangeCrossType ExchangeCrossType { get; set; }
        /// <summary>
        /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
        /// </summary>
        public string ExchangeAcceptSourceDataId { get; set; }
        /// <summary>
        /// Dodaná data umístí do clipboardu 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="text"></param>
        private void DataExchangeClipboardPublish(object data, string text)
        {
            DxComponent.ClipboardInsert(ExchangeCurrentDataId, data, text);
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
            if (!DxComponent.CanAcceptExchangeData(appDataContainer, this.ExchangeCurrentDataId, this.ExchangeCrossType, this.ExchangeAcceptSourceDataId)) return false;
            data = appDataContainer.Data;
            return true;
        }
        #endregion
        #region Drag and Drop
        /// <summary>
        /// Souhrn povolených akcí Drag and Drop
        /// </summary>
        public DxDragDropActionType DragDropActions { get { return _DragDropActions; } set { DxDragDropInit(value); } }
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
        private void DxDragDropInit(DxDragDropActionType actions)
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
            var selectedItems = this.SelectedNodes;
            if (selectedItems.Length == 0)
            {
                args.SourceDragEnabled = false;
            }
            else
            {
                args.SourceText = selectedItems.ToOneString(convertor: i => i.Text);
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
            IndexRatio index = null; // TODO DoDragSearchIndexRatio(targetPoint);
            if (!IndexRatio.IsEqual(index, MouseDragTargetIndex))
            {
                MouseDragTargetIndex = index;
                this.Invalidate();
            }
            args.CurrentEffect = args.SuggestedDragDropEffect;
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je zdrojem
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
                    // TODO args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                    args.InsertIndex = args.TargetIndex.GetInsertIndex(selectedItemsInfo.Select(t => t.Item1));
                }
                // Odebereme zdrojové prvky:
                // TODO this.RemoveIndexes(selectedItemsInfo.Select(t => t.Item1));
            }
        }
        /// <summary>
        /// Když úspěšně končí proces Drag, a this objekt je možným cílem
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetDrop(DxDragDropArgs args)
        {
            // TODO 

            //if (args.TargetIndex == null)
            //{
            //    Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
            //    args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
            //    args.InsertIndex = null;
            //}
            //if (!args.InsertIndex.HasValue)
            //    args.InsertIndex = args.TargetIndex.GetInsertIndex();

            //List<int> selectedIndexes = new List<int>();
            //var selectedItemsInfo = args.SourceObject as Tuple<int, IMenuItem, Rectangle?>[];
            //if (selectedItemsInfo != null)
            //{
            //    IMenuItem[] selectedItems = selectedItemsInfo.Select(t => t.Item2).ToArray();
            //    if (args.InsertIndex.HasValue && args.InsertIndex.Value >= 0 && args.InsertIndex.Value < this.ItemCount)
            //    {
            //        int insertIndex = args.InsertIndex.Value;
            //        foreach (var selectedItem in selectedItems)
            //        {
            //            DevExpress.XtraEditors.Controls.ImageListBoxItem imgItem = new DevExpress.XtraEditors.Controls.ImageListBoxItem(selectedItem);
            //            selectedIndexes.Add(insertIndex);
            //            this.Items.Insert(insertIndex++, imgItem);
            //        }
            //    }
            //    else
            //    {
            //        int addIndex = this.ItemCount;
            //        foreach (var selectedItem in selectedItems)
            //            selectedIndexes.Add(addIndex++);
            //        this.Items.AddRange(selectedItems);
            //    }
            //    this.SelectedIndexes = selectedIndexes;
            //}

            //MouseDragTargetIndex = null;
            //this.Invalidate();
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
        /// Ukončení procesu Drag and Drop
        /// </summary>
        /// <param name="args"></param>
        private void DoDragTargetEnd(DxDragDropArgs args)
        {
            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Informace o prvku, nad kterým je myš, pro umístění obsahu v procesu Drag and Drop.
        /// Pokud je null, pak pro this prvek neprobíhá Drag and Drop.
        /// <para/>
        /// Tuto hodnotu vykresluje metoda Paint
        /// </summary>
        private IndexRatio MouseDragTargetIndex;
        #endregion
        #region Public vlastnosti, kolekce nodů, nastavení práce, vyhledání nodu podle klíče, vyhledání child nodů
        /// <summary>
        /// Pole všech nodů = třída <see cref="ITreeListNode"/> = data o nodech
        /// </summary>
        public ITreeListNode[] NodeInfos { get { return this._NodesStandard.ToArray(); } }
        /// <summary>
        /// Najde a vrátí pole nodů, které jsou Child nody daného klíče.
        /// Reálně provádí Scan všech nodů.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        public ITreeListNode[] GetChildNodeInfos(string parentKey)
        {
            if (parentKey == null) return null;
            return this._NodesStandard.Where(n => n.ParentNodeFullId != null && n.ParentNodeFullId == parentKey).ToArray();
        }
        /// <summary>
        /// Aktuálně vybraný Node
        /// </summary>
        public ITreeListNode FocusedNodeInfo { get { return _GetNodeInfo(this.FocusedNode); } }
        /// <summary>
        /// Obsahuje <see cref="ITextItem.ItemId"/> aktuálně focusovaného nodu.
        /// Lze setovat. Pokud bude setován neexistující ID, pak focusovaný node bude null.
        /// </summary>
        public string FocusedNodeFullId
        {
            get { return FocusedNodeInfo?.ItemId; }
            set
            {
                DevExpress.XtraTreeList.Nodes.TreeListNode focusedNode = null;
                string fullNodeId = value;
                if (!String.IsNullOrEmpty(fullNodeId))
                {
                    var nodePair = this._GetNodePair(fullNodeId);
                    if (nodePair != null && nodePair.HasTreeNode)
                        focusedNode = nodePair.TreeNode;
                }
                this.FocusedNode = focusedNode;
            }
        }
        /// <summary>
        /// Najde node podle jeho klíče, pokud nenajde pak vrací false.
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        public bool TryGetNodeInfo(string nodeKey, out ITreeListNode nodeInfo)
        {
            nodeInfo = null;
            if (nodeKey == null) return false;
            bool result = this._NodesKey.TryGetValue(nodeKey, out var nodePair);
            nodeInfo = nodePair.NodeInfo;
            return result;
        }
        /// <summary>
        /// Režim zobrazení Checkboxů. 
        /// Výchozí je <see cref="TreeListCheckBoxMode.None"/>
        /// </summary>
        public TreeListCheckBoxMode CheckBoxMode
        {
            get { return _CheckBoxMode; }
            set
            {
                _CheckBoxMode = value;
                this.OptionsView.ShowCheckBoxes = (value == TreeListCheckBoxMode.AllNodes || value == TreeListCheckBoxMode.SpecifyByNode);
            }
        }
        private TreeListCheckBoxMode _CheckBoxMode;
        /// <summary>
        /// Zobrazovat Root node?
        /// Má se nastavit po inicializaci nebo po <see cref="ClearNodes"/>. Změna nastavení později nemá význam.
        /// </summary>
        public bool RootNodeVisible { get; set; }
        /// <summary>
        /// Po LazyLoad aktivovat první načtený node?
        /// </summary>
        public TreeListLazyLoadFocusNodeType LazyLoadFocusNode { get; set; }
        /// <summary>
        /// Akce, která zahájí editaci buňky.
        /// Výchozí je MouseUp (nejhezčí), ale je možno nastavit i jinak.
        /// </summary>
        public DevExpress.XtraTreeList.TreeListEditorShowMode EditorShowMode
        {
            get { return this.OptionsBehavior.EditorShowMode; }
            set { this.OptionsBehavior.EditorShowMode = value; }
        }
        /// <summary>
        /// Režim inkrementálního vyhledávání (=psaní na klávesnici).
        /// Default = <see cref="TreeListIncrementalSearchMode.InExpandedNodesOnly"/>
        /// </summary>
        public TreeListIncrementalSearchMode IncrementalSearchMode
        {
            get { return _IncrementalSearchMode; }
            set
            {
                _IncrementalSearchMode = value;
                this.OptionsFind.AllowIncrementalSearch = (value == TreeListIncrementalSearchMode.InExpandedNodesOnly || value == TreeListIncrementalSearchMode.InAllNodes);
                this.OptionsFind.ExpandNodesOnIncrementalSearch = (value == TreeListIncrementalSearchMode.InAllNodes);
            }
        }
        private TreeListIncrementalSearchMode _IncrementalSearchMode;
        /// <summary>
        /// Má být selectován ten node, pro který se právě chystáme zobrazit kontextovém menu?
        /// <para/>
        /// Pokud je zobrazováno kontextové menu nad určitým nodem, a tento node není selectován, pak hodnota true zajistí, že tento node bude nejprve selectován.
        /// Hodnota true je defaultní.
        /// <para/>
        /// Pokud bude false, pak neselectovaný node bude ponechán neselectovaný.
        /// Událost <see cref="ShowContextMenu"/> dostává argument, v němž je definován ten node na který bylo kliknuto, i když není Selected.
        /// </summary>
        public bool SelectNodeBeforeShowContextMenu { get; set; }
        /// <summary>
        /// Co provede DoubleClick na textu anebo Click na ikoně
        /// </summary>
        public NodeMainClickMode MainClickMode { get { return _MainClickMode; } set { _MainClickModeSet(value); } }
        private NodeMainClickMode _MainClickMode;
        /// <summary>
        /// Text (lokalizovaný) pro text uzlu, který reprezentuje "LazyLoadChild", např. něco jako "Načítám data..."
        /// </summary>
        public string LazyLoadNodeText { get; set; }
        /// <summary>
        /// Název ikony uzlu, který reprezentuje "LazyLoadChild", např. něco jako přesýpací hodiny...
        /// </summary>
        public string LazyLoadNodeImageName { get; set; }
        /// <summary>
        /// Uloží daný režim do proměnné a nastaví podle něj nativní chování TreeListu
        /// </summary>
        /// <param name="mainClickMode"></param>
        private void _MainClickModeSet(NodeMainClickMode mainClickMode)
        {
            bool allowExpandOnDblClick = (mainClickMode == NodeMainClickMode.ExpandCollapse || mainClickMode == NodeMainClickMode.ExpandCollapseRunEvent);
            this.OptionsBehavior.AllowExpandOnDblClick = allowExpandOnDblClick;
            _MainClickMode = mainClickMode;
        }
        /// <summary>
        /// Obsahuje kolekci všech nodů, které nejsou IsLazyChild.
        /// Node typu IsLazyChild je dočasně přidaný child node do těch nodů, jejichž Childs se budou načítat po rozbalení.
        /// </summary>
        private IEnumerable<ITreeListNode> _NodesStandard { get { return this._NodesId.Values.Where(p => !p.IsLazyChild).Select(p => p.NodeInfo); } }
        #endregion
        #region DoKeyActions; CtrlA, CtrlC, CtrlX, CtrlV, Delete
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
        /// Řeší stisk klávesy - Delete a Clipboard.
        /// <para/>
        /// Tyto události jsou předány do obecnější metody 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _OnKeyDownClipboardDelete(KeyEventArgs e)
        {
            // Pokud je aktivní editor, pak tyto klávesy (Delete, Ctrl+C, Ctrl+V, ...) neřešíme vůbec - ty patří do editoru:
            if (IsActiveEditor) return false;

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
            }
            return isHandled;
        }
        /// <summary>
        /// Provede akce zadané jako bity v dané akci (<paramref name="action"/>), s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="force"></param>
        private bool _DoKeyAction(KeyActionType action, bool force = false)
        {
            bool isHandled = false;
            _DoKeyAction(action, KeyActionType.SelectAll, force, _DoKeyActionCtrlA, ref isHandled);
            _DoKeyAction(action, KeyActionType.ClipCopy, force, _DoKeyActionCtrlC, ref isHandled);
            _DoKeyAction(action, KeyActionType.ClipCut, force, _DoKeyActionCtrlX, ref isHandled);
            _DoKeyAction(action, KeyActionType.ClipPaste, force, _DoKeyActionCtrlV, ref isHandled);
            /*
            _DoKeyAction(action, KeyActionType.MoveTop, force, _DoKeyActionMoveTop, ref isHandled);
            _DoKeyAction(action, KeyActionType.MoveUp, force, _DoKeyActionMoveUp, ref isHandled);
            _DoKeyAction(action, KeyActionType.MoveDown, force, _DoKeyActionMoveDown, ref isHandled);
            _DoKeyAction(action, KeyActionType.MoveBottom, force, _DoKeyActionMoveBottom, ref isHandled);
            */
            _DoKeyAction(action, KeyActionType.Delete, force, _DoKeyActionDelete, ref isHandled);
            _DoKeyAction(action, KeyActionType.Undo, force, _DoKeyActionUndo, ref isHandled);
            _DoKeyAction(action, KeyActionType.Redo, force, _DoKeyActionRedo, ref isHandled);
            return isHandled;
        }
        /// <summary>
        /// Pokud v soupisu akcí <paramref name="action"/> je příznak akce <paramref name="flag"/>, pak provede danou akci <paramref name="runMethod"/>, 
        /// s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="flag"></param>
        /// <param name="force"></param>
        /// <param name="runMethod"></param>
        /// <param name="handled">Nastaví na true, pokud byla provedena požadovaná akce</param>
        private void _DoKeyAction(KeyActionType action, KeyActionType flag, bool force, Action runMethod, ref bool handled)
        {
            if (!action.HasFlag(flag)) return;
            if (!force && !EnabledKeyActions.HasFlag(flag)) return;
            runMethod();
            handled = true;
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlA
        /// </summary>
        private void _DoKeyActionCtrlA() { }
        /// <summary>
        /// Provedení klávesové akce: CtrlC
        /// </summary>
        private void _DoKeyActionCtrlC()
        {
            var selectedNodes = this.SelectedNodes;
            string textTxt = selectedNodes.ToOneString();
            DataExchangeClipboardPublish(selectedNodes, textTxt);
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
            if (data is IEnumerable<ITreeListNode> nodes) _InsertNodes(nodes);
        }
        /// <summary>
        /// Provedení klávesové akce: Delete
        /// </summary>
        private void _DoKeyActionDelete()
        {
            var deleteNodes = this.SelectedNodes.Where(n => n.CanDelete).ToArray();
            if (deleteNodes.Length > 0)
                this.RaiseNodesDelete(deleteNodes);
        }
        /// <summary>
        /// Provedení klávesové akce: Undo
        /// </summary>
        private void _DoKeyActionUndo()
        {
            // if (this.UndoRedoEnabled) this.UndoRedoController.DoUndo();
        }
        /// <summary>
        /// Provedení klávesové akce: Redo
        /// </summary>
        private void _DoKeyActionRedo()
        {
            // if (this.UndoRedoEnabled) this.UndoRedoController.DoRedo();
        }
        /// <summary>
        /// Seznam HotKeys = klávesy, pro které se volá událost <see cref="NodeKeyDown"/>.
        /// </summary>
        public IEnumerable<Keys> HotKeys
        { 
            get { return _HotKeys?.Keys; } 
            set { _HotKeys = value.CreateDictionary(k => k, true); }
        }
        private Dictionary<Keys, Keys> _HotKeys;
        /// <summary>
        /// Řeší HotKey deklarované uživatelem
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool _OnKeyDownHotKey(KeyEventArgs e)
        {
            bool isHandled = _IsHotKeyInEditor(e.KeyData);
            if (isHandled)
            {
                e.Handled = true;
                this.RaiseNodeKeyDown(e);
            }
            return isHandled;
        }
        /// <summary>
        /// Vrátí true, pokud daná klávesa je v seznamu HotKey
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool _IsHotKeyInEditor(Keys key)
        {
            return _HotKeys != null && _HotKeys.ContainsKey(key);
        }
        #endregion
        #region Obrázky - ImageListy, režim ImageMode, NodeImageType (Png / Svg), NodeImageSize (Small?)
        /// <summary>
        /// Režim kreslení ikon u nodů.
        /// Výchozí je <see cref="TreeListImageMode.ImageStatic"/> = zobrazuje se standardní ikona <see cref="ITextItem.ImageName"/> 
        /// (ale nezobrazují se ikony <see cref="ITreeListNode.ImageDynamicDefault"/> a <see cref="ITreeListNode.ImageDynamicSelected"/>).
        /// Aplikační kód musí plnit jména ikon do <see cref="ITextItem.ImageName"/>, <see cref="ITreeListNode.ImageDynamicDefault"/> a <see cref="ITreeListNode.ImageDynamicSelected"/>.
        /// </summary>
        public TreeListImageMode ImageMode
        {
            get { return _ImageMode; }
            set { _ImageMode = value; _ImageListApply(); }
        }
        /// <summary>
        /// Typ ikon: výchozí je <see cref="ResourceContentType.None"/>, lze nastavit jen na <see cref="ResourceContentType.Bitmap"/> nebo <see cref="ResourceContentType.Vector"/> a to jen když nejsou položky.
        /// Není povinné nastavovat, nastaví se podle typu prvního obrázku. Pak ale musí být všechny obrázky stejného typu.
        /// Pokud bude nastaveno, pak i první obrázek musí odpovídat.
        /// </summary>
        public ResourceContentType NodeImageType
        {
            get { return _NodeImageType; }
            set
            {
                var imageType = value;
                if (imageType != _NodeImageType)
                {
                    if (imageType == ResourceContentType.None || imageType == ResourceContentType.Vector || imageType == ResourceContentType.Bitmap)
                    {
                        if (this.AllNodesCount == 0)
                        {
                            _NodeImageType = imageType;
                            _ImageListApply();
                        }
                        else
                            throw new InvalidOperationException($"Value stored into TreeList.NodeImageType can be set only when TreeList is empty, current TreeList has {this.AllNodesCount} nodes.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Value stored into TreeList.NodeImageType must be only: None or Vector or Bitmap, current value is {imageType}.");
                    }
                }
            }
        }
        /// <summary>
        /// Velikost obrázků u nodů. Lze setovat jen když TreeList nemá žádné nody = pokud <see cref="NodesCount"/> == 0
        /// </summary>
        public ResourceImageSizeType NodeImageSize
        {
            get { return _NodeImageSize; }
            set
            {
                var sizeType = value;
                if (sizeType != _NodeImageSize)
                {
                    if (this.AllNodesCount == 0)
                    {
                        _NodeImageSize = sizeType;
                        _ImageListApply();
                    }
                    else
                        throw new InvalidOperationException($"Value stored into TreeList.NodeImageSize can be set only when TreeList is empty, current TreeList has {this.AllNodesCount} nodes.");
                }
            }
        }
        /// <summary>
        /// Velikost ikonek
        /// </summary>
        private ResourceImageSizeType _NodeImageSize
        {
            get { return __NodeImageSize; }
            set
            {
                var sizeType = value;
                var minHeight = this.MinRowHeight;
                var rowHeight = DxComponent.GetDefaultImageSize(sizeType).Height;
                this.RowHeight = (rowHeight > minHeight ? rowHeight : minHeight);
                this.__NodeImageSize = sizeType;
            }
        }
        /// <summary>
        /// V nodu se povoluje HTML text fomrátování
        /// </summary>
        public bool NodeAllowHtmlText { get; set; }
        /// <summary>
        /// Aplikuje režim obrázků do TreeListu
        /// </summary>
        private void _ImageListApply()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(_ImageListApply)); return; }

            var imageList = _GetImageList();
            switch (_ImageMode)
            {
                case TreeListImageMode.None:
                    this.SelectImageList = null;
                    this.StateImageList = null;
                    break;
                case TreeListImageMode.ImageDynamic:
                    this.SelectImageList = imageList;
                    this.StateImageList = null;
                    break;
                case TreeListImageMode.ImageStatic:
                    this.SelectImageList = null;
                    this.StateImageList = imageList;
                    break;
                case TreeListImageMode.ImageBoth:
                    this.SelectImageList = imageList;
                    this.StateImageList = imageList;
                    break;
            }
        }
        /// <summary>
        /// Vrátí patřičný objekt ImageListu nebo SvgListu pro aktuální typ a velikost.
        /// </summary>
        /// <returns></returns>
        private object _GetImageList()
        {
            switch (_NodeImageType)
            {
                case ResourceContentType.Bitmap:
                    return DxComponent.GetBitmapImageList(_NodeImageSize);
                case ResourceContentType.Vector: 
                    return DxComponent.GetVectorImageList(_NodeImageSize);
            }
            return null;
        }
        /// <summary>
        /// Vrací index image pro dané jméno obrázku pro aktuální typ a velikost.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int _GetImageIndex(string imageName, int defaultValue)
        {
            int index = -1;
            if (!String.IsNullOrEmpty(imageName) && _PrepareImageListFor(imageName))
            {
                switch (_NodeImageType)
                {
                    case ResourceContentType.Bitmap:
                        index = DxComponent.GetBitmapImageIndex(imageName, _NodeImageSize);
                        break;
                    case ResourceContentType.Vector:
                        index = DxComponent.GetVectorImageIndex(imageName, _NodeImageSize);
                        break;
                }
            }
            if (index < 0 && defaultValue >= 0) index = defaultValue;
            return index;
        }
        /// <summary>
        /// Pro dodaný obrázek určí jeho typ, prověří dosavadní používaný typ obrázku (Bitmap / Vector) a zajistí:
        /// a) pro první obrázek: aktivaci potřebného typu
        /// b) pro další obrázky: kontrolu shodnosti typu (nemíchat Bitmap x Vector), hlášku chyby
        /// <para/>
        /// Vrací true pokud se obrázek smí použít
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        private bool _PrepareImageListFor(string imageName)
        {
            if (String.IsNullOrEmpty(imageName)) return false;
            bool preferVector = (_NodeImageType == ResourceContentType.Vector || (_NodeImageType == ResourceContentType.None && DxComponent.IsPreferredVectorImage));
            if (!DxComponent.TryGetResourceContentType(imageName, _NodeImageSize, out ResourceContentType contentType, preferVector)) return false;

            if (this.AllNodesCount == 0 || _NodeImageType == ResourceContentType.None)
            {   // Dosud není určen typ obrázků, a nyní už typ obrázku víme:
                _NodeImageType = contentType;
                _NodeImageTypeMismatchShowed = false;
                _ImageListApply();
                return true;
            }
            if (contentType == _NodeImageType) return true;          // Nová ikona (imageName) je stejného druhu, jaký už používáme (_NodeImageType) = to je v pořádku.

            if (!_NodeImageTypeMismatchShowed)
            {
                DxComponent.ShowMessageWarning($"DxTreeList NodeImage typ mismatch: Current image type={_NodeImageType}, new image type={contentType}, imageName='{imageName}'.");
                _NodeImageTypeMismatchShowed = true;
            }
            return false;
        }
        /// <summary>
        /// Zobrazované ikony
        /// </summary>
        private TreeListImageMode _ImageMode;
        /// <summary>
        /// Typ ikon: výchozí je <see cref="ResourceContentType.None"/>, lze nastavit jen na <see cref="ResourceContentType.Bitmap"/> nebo <see cref="ResourceContentType.Vector"/> a to jen když nejsou položky.
        /// </summary>
        private ResourceContentType _NodeImageType;
        /// <summary>
        /// Velikost ikonek
        /// </summary>
        private ResourceImageSizeType __NodeImageSize;
        /// <summary>
        /// Obsahuje true poté, kdy došlo k prvnímu nesouladu typu ikonek, a nesoulad byl hlášen.
        /// </summary>
        private bool _NodeImageTypeMismatchShowed;
        #endregion
        #region Public eventy a jejich volání
        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeFocusedChanged(DxTreeListNodeArgs)"/> a event <see cref="NodeFocusedChanged"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void RaiseNodeFocusedChanged(ITreeListNode nodeInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeFocusedChanged, TreeListPartType.None, this.IsActiveEditor);
            OnNodeFocusedChanged(args);
            NodeFocusedChanged?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList aktivoval určitý Node
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeFocusedChanged(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList aktivoval určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeFocusedChanged;

        /// <summary>
        /// Vyvolá metodu <see cref="OnSelectedNodesChanged(DxTreeListNodeArgs)"/> a event <see cref="SelectedNodesChanged"/>
        /// </summary>
        private void RaiseSelectedNodesChanged()
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(null, TreeListActionType.SelectedNodesChanged, TreeListPartType.None, this.IsActiveEditor);
            OnSelectedNodesChanged(args);
            SelectedNodesChanged?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList změnil seznam <see cref="SelectedNodes"/>
        /// </summary>
        protected virtual void OnSelectedNodesChanged(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList změnil seznam <see cref="SelectedNodes"/>
        /// </summary>
        public event DxTreeListNodeHandler SelectedNodesChanged;

        /// <summary>
        /// Vyvolá metodu <see cref="OnShowContextMenu(DxTreeListNodeContextMenuArgs)"/> a event <see cref="ShowContextMenu"/>
        /// </summary>
        /// <param name="hitInfo"></param>
        private void RaiseShowContextMenu(TreeListVisualNodeInfo hitInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodeContextMenuArgs args = new DxTreeListNodeContextMenuArgs(hitInfo.NodeInfo, TreeListActionType.ShowContextMenu, this.IsActiveEditor, hitInfo);
            OnShowContextMenu(args);
            ShowContextMenu?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnShowContextMenu(DxTreeListNodeContextMenuArgs args) { }
        /// <summary>
        /// Uživatel chce zobrazit kontextové menu
        /// </summary>
        public new event DxTreeListNodeContextMenuHandler ShowContextMenu;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeIconClick(DxTreeListNodeArgs)"/> a event <see cref="NodeIconClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="partType"></param>
        private void RaiseNodeIconClick(ITreeListNode nodeInfo, TreeListPartType partType)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeIconClick, partType, this.IsActiveEditor);
            OnNodeIconClick(args);
            NodeIconClick?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá event <see cref="NodeIconClick"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeIconClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList má NodeIconClick na určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeIconClick;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeDoubleClick(DxTreeListNodeArgs)"/> a event <see cref="NodeDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="partType"></param>
        private void RaiseNodeDoubleClick(ITreeListNode nodeInfo, TreeListPartType partType)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeDoubleClick, partType, this.IsActiveEditor);
            OnNodeDoubleClick(args);
            NodeDoubleClick?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList má Doubleclick na určitý Node
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeDoubleClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList má Doubleclick na určitý Node
        /// </summary>
        public event DxTreeListNodeHandler NodeDoubleClick;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeExpanded(DxTreeListNodeArgs)"/> a event <see cref="NodeExpanded"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void RaiseNodeExpanded(ITreeListNode nodeInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeExpanded, TreeListPartType.None, this.IsActiveEditor);
            OnNodeExpanded(args);
            NodeExpanded?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyExpandable"/>).
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeExpanded(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyExpandable"/>).
        /// </summary>
        public event DxTreeListNodeHandler NodeExpanded;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeCollapsed(DxTreeListNodeArgs)"/> a event <see cref="NodeCollapsed"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void RaiseNodeCollapsed(ITreeListNode nodeInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeCollapsed, TreeListPartType.None, this.IsActiveEditor);
            OnNodeCollapsed(args);
            NodeCollapsed?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList právě sbaluje určitý Node.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeCollapsed(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě sbaluje určitý Node.
        /// </summary>
        public event DxTreeListNodeHandler NodeCollapsed;

        /// <summary>
        /// Vyvolá metodu <see cref="OnActivatedEditor(DxTreeListNodeArgs)"/> a event <see cref="ActivatedEditor"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void RaiseActivatedEditor(ITreeListNode nodeInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.ActivatedEditor, TreeListPartType.None, this.IsActiveEditor);
            OnActivatedEditor(args);
            ActivatedEditor.Invoke(this, args);
        }
        /// <summary>
        /// TreeList právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivatedEditor(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        public event DxTreeListNodeHandler ActivatedEditor;

        /// <summary>
        /// Vyvolá metodu <see cref="OnEditorDoubleClick(DxTreeListNodeArgs)"/> a event <see cref="EditorDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        private void RaiseEditorDoubleClick(ITreeListNode nodeInfo, object editedValue)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.EditorDoubleClick, TreeListPartType.Cell, this.IsActiveEditor, editedValue);
            OnEditorDoubleClick(args);
            EditorDoubleClick?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnEditorDoubleClick(DxTreeListNodeArgs args) { }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        public event DxTreeListNodeHandler EditorDoubleClick;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeEdited(DxTreeListNodeArgs)"/> a event <see cref="NodeEdited"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        private void RaiseNodeEdited(ITreeListNode nodeInfo, object editedValue)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeEdited, TreeListPartType.Cell, this.IsActiveEditor, editedValue);
            OnNodeEdited(args);
            NodeEdited?.Invoke(this, args);
        }
        /// <summary>
        /// TreeList právě skončil editaci určitého Node.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeEdited(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList právě skončil editaci určitého Node.
        /// </summary>
        public event DxTreeListNodeHandler NodeEdited;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeKeyDown(DxTreeListNodeKeyArgs)"/> a event <see cref="NodeKeyDown"/>
        /// </summary>
        /// <param name="e"></param>
        private void RaiseNodeKeyDown(KeyEventArgs e)
        {
            if (_SilentMode) return;

            DxTreeListNodeKeyArgs args = new DxTreeListNodeKeyArgs(this.FocusedNodeInfo, TreeListActionType.KeyDown, this.IsActiveEditor, e);
            OnNodeKeyDown(args);
            NodeKeyDown?.Invoke(this, args);
        }
        /// <summary>
        /// Je voláno po stisku klávesy na určitém node.
        /// Pozor, aby se tento event vyvolal, je třeba nejdřív nastavit kolekci <see cref="HotKeys"/>!
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeKeyDown(DxTreeListNodeKeyArgs args) { }
        /// <summary>
        /// TreeList má KeyDown na určitém Node.
        /// Pozor, aby se tento event vyvolal, je třeba nejdřív nastavit kolekci <see cref="HotKeys"/>!
        /// </summary>
        public event DxTreeListNodeKeyHandler NodeKeyDown;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodeCheckedChange(DxTreeListNodeArgs)"/> a event <see cref="NodeCheckedChange"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isChecked"></param>
        private void RaiseNodeCheckedChange(ITreeListNode nodeInfo, bool isChecked)
        {
            if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.NodeCheckedChange, TreeListPartType.NodeCheckBox, this.IsActiveEditor, isChecked);
            OnNodeCheckedChange(args);
            NodeCheckedChange?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodeCheckedChange(DxTreeListNodeArgs args) { }
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        public event DxTreeListNodeHandler NodeCheckedChange;

        /// <summary>
        /// Vyvolá metodu <see cref="OnNodesDelete(DxTreeListNodesArgs)"/> a event <see cref="NodesDelete"/>
        /// </summary>
        /// <param name="nodesInfo"></param>
        private void RaiseNodesDelete(IEnumerable<ITreeListNode> nodesInfo)
        {
            if (_SilentMode) return;

            DxTreeListNodesArgs args = new DxTreeListNodesArgs(nodesInfo, TreeListActionType.NodeDelete);
            OnNodesDelete(args);
            NodesDelete?.Invoke(this, args);
        }
        /// <summary>
        /// Uživatel dal Delete na nodech (mimo editor)
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnNodesDelete(DxTreeListNodesArgs args) { }
        /// <summary>
        /// Uživatel dal Delete na nodech (mimo editor)
        /// </summary>
        public event DxTreeListNodesHandler NodesDelete;

        /// <summary>
        /// Vyvolá metodu <see cref="OnLazyLoadChilds(DxTreeListNodeArgs)"/> a event <see cref="LazyLoadChilds"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void RaiseLazyLoadChilds(ITreeListNode nodeInfo)
        {
            // Tato událost má výjimku, protože donačíst LazyChild se musí i když jsme Silent...:
            // if (_SilentMode) return;

            DxTreeListNodeArgs args = new DxTreeListNodeArgs(nodeInfo, TreeListActionType.LazyLoadChilds, TreeListPartType.None, this.IsActiveEditor);
            OnLazyLoadChilds(args);
            LazyLoadChilds?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá event <see cref="LazyLoadChilds"/>
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnLazyLoadChilds(DxTreeListNodeArgs args) { }
        /// <summary>
        /// TreeList rozbaluje node, který má nastaveno načítání ze serveru : <see cref="ITreeListNode.LazyExpandable"/> je true.
        /// </summary>
        public event DxTreeListNodeHandler LazyLoadChilds;

        /// <summary>
        /// Vyvolá metodu <see cref="OnToolTipEvent(DxToolTipArgs)"/> a event <see cref="ToolTipChanged"/>
        /// </summary>
        /// <param name="eventName"></param>
        private void RaiseToolTipChanged(string eventName)
        {
            RaiseToolTipChanged(new DxToolTipArgs(eventName));
        }
        /// <summary>
        /// Vyvolá metodu <see cref="OnToolTipEvent(DxToolTipArgs)"/> a event <see cref="ToolTipChanged"/>
        /// </summary>
        /// <param name="args"></param>
        private void RaiseToolTipChanged(DxToolTipArgs args)
        {
            OnToolTipEvent(args);
            ToolTipChanged?.Invoke(this, args);
        }
        /// <summary>
        /// ToolTip v TreeListu má událost
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToolTipEvent(DxToolTipArgs args) { }
        /// <summary>
        /// ToolTip v TreeListu má událost
        /// </summary>
        public event DxToolTipHandler ToolTipChanged;

        /// <summary>
        /// Tichý režim = bez eventů.
        /// Na true se aktivuje pouze po dobu odebírání a přidávání nodů v metodě <see cref="_RemoveAddNodesSilent(bool, IEnumerable{string}, IEnumerable{ITreeListNode}, PreservePropertiesMode)"/>.
        /// </summary>
        private bool _SilentMode;
        #endregion
    }
    #region Deklarace delegátů a tříd pro eventhandlery, další enumy
    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxTreeListNodeContextMenuHandler(object sender, DxTreeListNodeContextMenuArgs args);
    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxTreeListNodeContextMenuArgs : DxTreeListNodeArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        /// <param name="isActiveEditor"></param>
        /// <param name="hitInfo"></param>
        public DxTreeListNodeContextMenuArgs(ITreeListNode node, TreeListActionType action, bool isActiveEditor, TreeListVisualNodeInfo hitInfo)
            : base(node, action, hitInfo.PartType, isActiveEditor, null)
        {
            this.HitInfo = hitInfo;
        }
        /// <summary>
        /// Data o HitInfo
        /// </summary>
        public TreeListVisualNodeInfo HitInfo { get; private set; }
    }
    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxTreeListNodeKeyHandler(object sender, DxTreeListNodeKeyArgs args);
    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxTreeListNodeKeyArgs : DxTreeListNodeArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        /// <param name="isActiveEditor"></param>
        /// <param name="keyArgs"></param>
        public DxTreeListNodeKeyArgs(ITreeListNode node, TreeListActionType action, bool isActiveEditor, KeyEventArgs keyArgs)
            : base(node, action, TreeListPartType.None, isActiveEditor, null)
        {
            this.KeyArgs = keyArgs;
        }
        /// <summary>
        /// Data o klávese
        /// </summary>
        public KeyEventArgs KeyArgs { get; private set; }
    }
    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxTreeListNodesHandler(object sender, DxTreeListNodesArgs args);
    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxTreeListNodesArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="action"></param>
        public DxTreeListNodesArgs(IEnumerable<ITreeListNode> nodes, TreeListActionType action)
        {
            this.MousePosition = Control.MousePosition;
            this.Nodes = nodes;
            this.Action = action;
        }
        /// <summary>
        /// Absolutní pozice myši v době události
        /// </summary>
        public Point MousePosition { get; private set; }
        /// <summary>
        /// Data o aktuálních nodech
        /// </summary>
        public IEnumerable<ITreeListNode> Nodes { get; private set; }
        /// <summary>
        /// Druh akce
        /// </summary>
        public TreeListActionType Action { get; private set; }
    }
    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxTreeListNodeHandler(object sender, DxTreeListNodeArgs args);
    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxTreeListNodeArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        /// <param name="partType"></param>
        /// <param name="isActiveEditor"></param>
        /// <param name="editedValue"></param>
        public DxTreeListNodeArgs(ITreeListNode node, TreeListActionType action, TreeListPartType partType, bool isActiveEditor, object editedValue = null)
        {
            this.MousePosition = Control.MousePosition;
            this.Node = node;
            this.Action = action;
            this.PartType = partType;
            this.IsActiveEditor = isActiveEditor;
            this.EditedValue = editedValue;
        }
        /// <summary>
        /// Absolutní pozice myši v době události
        /// </summary>
        public Point MousePosition { get; private set; }
        /// <summary>
        /// Data o aktuálním nodu
        /// </summary>
        public ITreeListNode Node { get; private set; }
        /// <summary>
        /// Druh akce
        /// </summary>
        public TreeListActionType Action { get; private set; }
        /// <summary>
        /// Pozice, kde bylo kliknuto. None, když kliknuto nebylo.
        /// </summary>
        public TreeListPartType PartType { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud je právě aktivní editor
        /// </summary>
        public bool IsActiveEditor { get; private set; }
        /// <summary>
        /// Editovaná hodnota, je vyplněna pouze pro akce <see cref="TreeListActionType.NodeEdited"/> a <see cref="TreeListActionType.EditorDoubleClick"/>
        /// </summary>
        public object EditedValue { get; private set; }
    }
    /// <summary>
    /// Informace o nodu, na který bylo kliknuto a kam
    /// </summary>
    public class TreeListVisualNodeInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="treeHit"></param>
        /// <param name="nodeInfo"></param>
        public TreeListVisualNodeInfo(DevExpress.XtraTreeList.TreeListHitInfo treeHit, ITreeListNode nodeInfo)
        {
            this.TreeHit = treeHit;
            this.NodeInfo = nodeInfo;
            this.PartType = (TreeListPartType)((int)treeHit.HitInfoType);    // Zdejší enum TreeListPartType má shodné numerické hodnoty jako DevExpress typ DevExpress.XtraTreeList.HitInfoType
        }
        /// <summary>
        /// Info DevExpress
        /// </summary>
        public DevExpress.XtraTreeList.TreeListHitInfo TreeHit { get; private set; }
        /// <summary>
        /// Node
        /// </summary>
        public ITreeListNode NodeInfo { get; private set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public TreeListPartType PartType { get; private set; }
        /// <summary>
        /// Obsahuje true, pokud <see cref="PartType"/> je (SelectImage nebo StateImage)
        /// </summary>
        public bool IsInImages { get { return IsType(TreeListPartType.StateImage, TreeListPartType.SelectImage); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="PartType"/> je (Cell)
        /// </summary>
        public bool IsInCell { get { return IsType(TreeListPartType.Cell); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="PartType"/> je (Cell nebo SelectImage nebo StateImage)
        /// </summary>
        public bool IsInImagesOrCell { get { return IsType(TreeListPartType.Cell, TreeListPartType.StateImage, TreeListPartType.SelectImage); } }
        /// <summary>
        /// Vrátí true, pokud aktuální typ <see cref="PartType"/> odpovídá některé ze zadaných hodnot
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public bool IsType(params TreeListPartType[] types)
        {
            TreeListPartType type = this.PartType;
            return types.Any(t => t == type);
        }
        /// <summary>
        /// Souřadice myši při kliknutí, v prostoru TreeList
        /// </summary>
        public Point MousePoint { get { return TreeHit.MousePoint; } }
        /// <summary>
        /// Nativní Node
        /// </summary>
        public DevExpress.XtraTreeList.Nodes.TreeListNode Node { get { return TreeHit.Node; } }
        /// <summary>
        /// Nativní Band
        /// </summary>
        public DevExpress.XtraTreeList.Columns.TreeListBand Band { get { return TreeHit.Band; } }
        /// <summary>
        /// Nativní Column
        /// </summary>
        public DevExpress.XtraTreeList.Columns.TreeListColumn Column { get { return TreeHit.Column; } }

    }
    /// <summary>
    /// Akce která proběhla v TreeList
    /// </summary>
    public enum TreeListActionType
    {
        /// <summary>None</summary>
        None,
        /// <summary>FocusedNodeChanged</summary>
        NodeFocusedChanged,
        /// <summary>SelectedNodesChanged</summary>
        SelectedNodesChanged,
        /// <summary>NodeIconClick</summary>
        NodeIconClick,
        /// <summary>NodeDoubleClick</summary>
        NodeDoubleClick,
        /// <summary>NodeExpanded</summary>
        NodeExpanded,
        /// <summary>NodeCollapsed</summary>
        NodeCollapsed,
        /// <summary>ActivatedEditor</summary>
        ActivatedEditor,
        /// <summary>EditorDoubleClick</summary>
        EditorDoubleClick,
        /// <summary>NodeEdited</summary>
        NodeEdited,
        /// <summary>NodeDelete</summary>
        NodeDelete,
        /// <summary>NodeCheckedChange</summary>
        NodeCheckedChange,
        /// <summary>LazyLoadChilds</summary>
        LazyLoadChilds,
        /// <summary>KeyDown</summary>
        KeyDown,
        /// <summary>ShowContextMenu</summary>
        ShowContextMenu
    }
    /// <summary>
    /// Který node se bude focusovat po LazyLoad child nodů?
    /// </summary>
    public enum TreeListLazyLoadFocusNodeType
    {
        /// <summary>
        /// Žádný
        /// </summary>
        None,
        /// <summary>
        /// Parent node
        /// </summary>
        ParentNode,
        /// <summary>
        /// První Child node
        /// </summary>
        FirstChildNode
    }
    /// <summary>
    /// Jaké ikony bude TreeList zobrazovat - a rezervovat pro ně prostor
    /// </summary>
    public enum TreeListImageMode
    {
        /// <summary>
        /// Žádná ikona
        /// </summary>
        None,
        /// <summary>
        /// Pouze ikona vlevo, dynamicky reaguje na stav Selected, přebírá se z <see cref="ITreeListNode.ImageDynamicDefault"/> a <see cref="ITreeListNode.ImageDynamicSelected"/>
        /// </summary>
        ImageDynamic,
        /// <summary>
        /// Pouze ikona vpravo, statická, přebírá se z <see cref="ITextItem.ImageName"/>
        /// </summary>
        ImageStatic,
        /// <summary>
        /// Obě ikony = <see cref="ImageDynamic"/> (dynamická, vlevo) i <see cref="ImageStatic"/> (statická vpravo)
        /// </summary>
        ImageBoth
    }
    /// <summary>
    /// Režim zobrazení CheckBoxů u nodu
    /// </summary>
    public enum TreeListCheckBoxMode
    {
        /// <summary>
        /// Nezobrazovat nikde
        /// </summary>
        None,
        /// <summary>
        /// Jednotlivě podle hodnoty <see cref="ITreeListNode.CanCheck"/>
        /// </summary>
        SpecifyByNode,
        /// <summary>
        /// Automaticky u všech nodů
        /// </summary>
        AllNodes
    }
    /// <summary>
    /// Režim hledání nodu při typování na klávesnici
    /// </summary>
    public enum TreeListIncrementalSearchMode
    {
        /// <summary>
        /// IncrementalSearch se nepoužívá
        /// </summary>
        None,
        /// <summary>
        /// IncrementalSearch vyhledává pouze v nodech, které jsou Expanded (exaktně řečeno: v nodech, jejichž Parent je Expanded).
        /// Pokud by hledaný text byl v nodu, který není Expanded, pak nebude nalezen.
        /// </summary>
        InExpandedNodesOnly,
        /// <summary>
        /// IncrementalSearch vyhledává i v nodexh, které nejsou Expanded.
        /// </summary>
        InAllNodes
    }
    /// <summary>
    /// Část TreeList, na kterou bylo kliknuto
    /// </summary>
    public enum TreeListPartType
    {
        /// <summary>
        /// A point is outside the Tree List control.
        /// </summary>
        None = 0,
        /// <summary>
        /// A point is over the empty area.
        /// </summary>
        Empty = 1,
        /// <summary>
        /// A point is over the column button.
        /// </summary>
        ColumnButton = 2,
        /// <summary>
        /// A point is over the blank column header.
        /// </summary>
        BehindColumn = 3,
        /// <summary>
        /// A point is over a column header.
        /// </summary>
        Column = 4,
        /// <summary>
        /// A point is over a column edge.
        /// </summary>
        ColumnEdge = 5,
        /// <summary>
        /// A point is over a node indicator cell.
        /// </summary>
        RowIndicator = 6,
        /// <summary>
        /// A point is on a row indicator's edge.
        /// </summary>
        RowIndicatorEdge = 7,
        /// <summary>
        /// A point is over an area that separates a row from its corresponding indicator
        /// cell. This value is returned only when the DevExpress.XtraTreeList.TreeListOptionsView.ShowIndentAsRowStyle
        /// option is enabled. Otherwise, an Empty value is returned when a point is over
        /// this area.
        /// </summary>
        RowIndent = 8,
        /// <summary>
        /// A point is over a node area not occupied by any of the node's elements.
        /// </summary>
        Row = 9,
        /// <summary>
        /// A point is over a preview section.
        /// </summary>
        RowPreview = 10,
        /// <summary>
        /// A point is over a row (group) footer.
        /// </summary>
        RowFooter = 11,
        /// <summary>
        /// A point is over a cell.
        /// </summary>
        Cell = 12,
        /// <summary>
        /// A point is over an expand button.
        /// </summary>
        Button = 13,
        /// <summary>
        /// A point is over a node's state image.
        /// </summary>
        StateImage = 14,
        /// <summary>
        /// A point is over a node's select image.
        /// </summary>
        SelectImage = 15,
        /// <summary>
        /// A point is over the summary footer.
        /// </summary>
        SummaryFooter = 16,
        /// <summary>
        /// A point is over the Customization Form.
        /// </summary>
        CustomizationForm = 17,
        /// <summary>
        /// The test point belongs to the Tree List's vertical scroll bar.
        /// </summary>
        VScrollBar = 18,
        /// <summary>
        /// The test point belongs to the Tree List's horizontal scroll bar.
        /// </summary>
        HScrollBar = 19,
        /// <summary>
        /// The test point belongs to the left fixed line.
        /// </summary>
        FixedLeftDiv = 20,
        /// <summary>
        /// The test point belongs to the right fixed line.
        /// </summary>
        FixedRightDiv = 21,
        /// <summary>
        /// The test point belongs to a node's check box.
        /// </summary>
        NodeCheckBox = 22,
        /// <summary>
        /// A point is over the Automatic Filtering Row.
        /// </summary>
        AutoFilterRow = 23,
        /// <summary>
        ///  A point is over the Filter Panel.
        /// </summary>
        FilterPanel = 24,
        /// <summary>
        /// A point is over the Close Filter Button in the Filter Panel.
        /// </summary>
        FilterPanelCloseButton = 25,
        /// <summary>
        /// A point is over the check box displayed within in the Filter Panel and used to
        /// enable/disable the filter.
        /// </summary>
        FilterPanelActiveButton = 26,
        /// <summary>
        /// A point is over the filter string displayed within the Filter Panel.
        /// </summary>
        FilterPanelText = 27,
        /// <summary>
        /// A point is over the MRU Filter Button in the Filter Panel.
        /// </summary>
        FilterPanelMRUButton = 28,
        /// <summary>
        /// A point is over the 'Edit Filter' button displayed within the Filter Panel and used to invoke the Filter Editor.
        /// </summary>
        FilterPanelCustomizeButton = 29,
        /// <summary>
        /// A point is over a Filter Button.
        /// </summary>
        ColumnFilterButton = 30,
        /// <summary>
        /// A point is over a column header panel's area not occupied by a column header, blank column header, filter button, column button, or a column edge.
        /// </summary>
        ColumnPanel = 31,
        /// <summary>
        /// A point is over a band header.
        /// </summary>
        Band = 32,
        /// <summary>
        /// A point is over the band panel.
        /// </summary>
        BandPanel = 33,
        /// <summary>
        /// A point is over the band button.
        /// </summary>
        BandButton = 34,
        /// <summary>
        /// A point is over a band edge.
        /// </summary>
        BandEdge = 35,
        /// <summary>
        /// A point is over the Caption Panel.
        /// </summary>
        Caption = 36,
        /// <summary>
        /// A point is over a column separator.
        /// </summary>
        Separator = 37
    }
    #endregion
    #endregion
    #region class DataTreeListNode a interface ITreeListNode : Data o jednom Node
    /// <summary>
    /// Data o jednom Node
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataTreeListNode : DataMenuItem, ITreeListNode
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataTreeListNode() : base()
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="parentNodeId"></param>
        /// <param name="text"></param>
        /// <param name="nodeType"></param>
        /// <param name="canEdit"></param>
        /// <param name="canDelete"></param>
        /// <param name="expanded"></param>
        /// <param name="lazyExpandable"></param>
        /// <param name="imageName"></param>
        /// <param name="imageNameSelected"></param>
        /// <param name="imageNameStatic"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="fontStyleDelta"></param>
        /// <param name="backColor"></param>
        /// <param name="foreColor"></param>
        /// <param name="mainClickAction"></param>
        public DataTreeListNode(string nodeId, string parentNodeId, string text,
            NodeItemType nodeType = NodeItemType.DefaultText, bool canEdit = false, bool canDelete = false, bool expanded = false, bool lazyExpandable = false,
            string imageName = null, string imageNameSelected = null, string imageNameStatic = null, string toolTipTitle = null, string toolTipText = null,
            int? fontSizeDelta = null, FontStyle? fontStyleDelta = null, Color? backColor = null, Color? foreColor = null, NodeMainClickActionType? mainClickAction = null)
        {
            _Id = -1;
            this.ItemId = nodeId;
            this.ParentNodeFullId = parentNodeId;
            this.Text = text;
            this.NodeType = nodeType;
            this.CanEdit = canEdit;
            this.CanDelete = canDelete;
            this.Expanded = expanded;
            this.LazyExpandable = lazyExpandable;
            this.ImageDynamicDefault = imageName;
            this.ImageDynamicSelected = imageNameSelected;
            this.ImageName = imageNameStatic;
            this.ToolTipTitle = toolTipTitle;
            this.ToolTipText = toolTipText;
            this.FontSizeDelta = fontSizeDelta;
            this.FontStyleDelta = fontStyleDelta;
            this.BackColor = backColor;
            this.ForeColor = foreColor;
            this.MainClickAction = mainClickAction ?? NodeMainClickActionType.RunEvent;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text ?? "";
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected override string DebugText
        {
            get
            {
                ITreeListNode node = this as ITreeListNode;
                string debugText = $"Id: {this._ItemId}; Text: {node.Text}; Parent: {node.ParentNodeFullId}";
                return debugText;
            }
        }
        /// <summary>
        /// Typ nodu
        /// </summary>
        public virtual NodeItemType NodeType { get; private set; }
        /// <summary>
        /// ID nodu v TreeList, pokud není v TreeList pak je -1 . Toto ID je přiděleno v rámci <see cref="DxTreeListNative"/> a po dobu přítomnosti nodu v TreeList se nemění.
        /// Pokud node bude odstraněn z Treeiew, pak hodnota <see cref="Id"/> bude -1, stejně tak jako v době, než bude Node do TreeList přidán.
        /// </summary>
        public virtual int Id { get { return _Id; } }
        /// <summary>
        /// Klíč parent uzlu.
        /// Po vytvoření nelze změnit.
        /// </summary>
        public virtual string ParentNodeFullId { get; private set; }
        /// <summary>
        /// Text v rámci editace (je sem setován při DoubleClicku a po ukončení editace).
        /// </summary>
        public virtual string TextEdited { get; set; }
        /// <summary>
        /// Node zobrazuje zaškrtávátko.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool CanCheck { get; set; }
        /// <summary>
        /// Node má zobrazovat prostor pro zaškrtávátko, i když node sám zaškrtávátko nezobrazuje.
        /// Je to proto, aby nody nacházející se v jedné řadě pod sebou byly "svisle zarovnané" i když některé zaškrtávátko mají, a jiné nemají.
        /// </summary>
        public virtual bool AddVoidCheckSpace { get; set; }
        /// <summary>
        /// Node je vybraný (Selected).
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool Selected { get; set; }
        /// <summary>
        /// Node je zaškrtnutý. Na rozdíl od <see cref="ITextItem.Checked"/> (což je bool?) je zdejší property pouze bool.
        /// </summary>
        public virtual bool NodeChecked { get { return this.Checked ?? false; } set { this.Checked = value; } }
        /// <summary>
        /// Ikona základní, ta může reagovat na stav Selected (pak bude zobrazena ikona <see cref="ImageDynamicSelected"/>), zobrazuje se vlevo.
        /// Pokud je změněna po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string ImageDynamicDefault { get; set; }
        /// <summary>
        /// Ikona ve stavu Node.IsSelected, zobrazuje se místo ikony <see cref="ImageDynamicDefault"/>), zobrazuje se vlevo.
        /// Pokud je změněna po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string ImageDynamicSelected { get; set; }
        /// <summary>
        /// Uživatel může editovat text tohoto node, po ukončení editace je vyvolána událost <see cref="DxTreeListNative.NodeEdited"/>.
        /// Změnu této hodnoty není nutno refreshovat, načítá se po výběru konkrétního Node v TreeList a aplikuje se na něj.
        /// </summary>
        public virtual bool CanEdit { get; set; }
        /// <summary>
        /// Uživatel může stisknout Delete nad uzlem, bude vyvolána událost <see cref="DxTreeListNative.NodesDelete"/>
        /// </summary>
        public virtual bool CanDelete { get; set; }
        /// <summary>
        /// Co provede DoubleClick na textu anebo Click na ikoně?
        /// Pro akceptování této hodnoty musí <see cref="DxTreeList"/> mít nastaveno <see cref="DxTreeList.MainClickMode"/> == <see cref="NodeMainClickMode.AcceptNodeSetting"/>.
        /// </summary>
        public virtual NodeMainClickActionType MainClickAction { get; set; }
        /// <summary>
        /// Node je otevřený.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool Expanded { get; set; }
        /// <summary>
        /// Node bude mít Child prvky, ale zatím nejsou dodány. 
        /// Node bude zobrazovat rozbalovací ikonu a jeden node s textem "Načítám data...", viz <see cref="DxTreeListNative.LazyLoadNodeText"/>.
        /// Ikonu nastavíme v <see cref="DxTreeListNative.LazyLoadNodeImageName"/>. Otevřením tohoto nodu se vyvolá event <see cref="DxTreeListNative.LazyLoadChilds"/>.
        /// Třída <see cref="DxTreeListNative"/> si sama obhospodařuje tento "LazyLoadChildNode": vytváří jej a následně jej i odebírá.
        /// Aktivace tohoto nodu není hlášena jako event, node nelze editovat ani smazat uživatelem.
        /// </summary>
        public virtual bool LazyExpandable { get; set; }
        /// <summary>
        /// Relativní velikost písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        public virtual int? FontSizeDelta { get; set; }
        /// <summary>
        /// Změna stylu písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        public virtual FontStyle? FontStyleDelta { get; set; }
        /// <summary>
        /// Explicitní barva pozadí prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Explicitní barva písma prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        public virtual Color? ForeColor { get; set; }
        /// <summary>
        /// Pokud je node již umístěn v TreeList, pak tato metoda zajistí jeho refresh = promítne vizuální hodnoty do controlu
        /// </summary>
        public void Refresh()
        {
            var owner = Owner;
            if (owner != null)
                owner.RefreshNode(this);
        }
        #region Implementace ITreeListNode
        DxTreeListNative ITreeListNode.Owner
        {
            get { return Owner; }
            set { _Owner = (value != null ? new WeakReference<DxTreeListNative>(value) : null); }
        }
        int ITreeListNode.Id { get { return _Id; } set { _Id = value; } }
        /// <summary>
        /// Owner = TreeList, ve kterém je this prvek zobrazen. Může být null.
        /// </summary>
        protected DxTreeListNative Owner { get { if (_Owner != null && _Owner.TryGetTarget(out var owner)) return owner; return null; } }
        WeakReference<DxTreeListNative> _Owner;
        int _Id;
        #endregion
    }
    /// <summary>
    /// Data o jednom Node.
    /// Pokud aplikace změní data nodu, měla by ve vhodný okamžik vyvolat <see cref="Refresh"/> tohoto uzlu.
    /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
    /// </summary>
    public interface ITreeListNode : IMenuItem
    {
        /// <summary>
        /// Aktuální vlastník nodu
        /// </summary>
        DxTreeListNative Owner { get; set; }
        /// <summary>
        /// ID nodu v TreeList, pokud není v TreeList pak je -1 . Toto ID je přiděleno v rámci <see cref="DxTreeListNative"/> a po dobu přítomnosti nodu v TreeList se nemění.
        /// Pokud node bude odstraněn z Treeiew, pak hodnota <see cref="Id"/> bude -1, stejně tak jako v době, než bude Node do TreeList přidán.
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// Typ nodu
        /// </summary>
        NodeItemType NodeType { get; }
        /// <summary>
        /// Klíč parent uzlu.
        /// Po vytvoření nelze změnit.
        /// </summary>
        string ParentNodeFullId { get; }
        /// <summary>
        /// Text v rámci editace (je sem setován při DoubleClicku a po ukončení editace).
        /// </summary>
        string TextEdited { get; set; }
        /// <summary>
        /// Node zobrazuje zaškrtávátko.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool CanCheck { get; }
        /// <summary>
        /// Uživatel může editovat text tohoto node, po ukončení editace je vyvolána událost <see cref="DxTreeListNative.NodeEdited"/>.
        /// Změnu této hodnoty není nutno refreshovat, načítá se po výběru konkrétního Node v TreeList a aplikuje se na něj.
        /// </summary>
        bool CanEdit { get; }
        /// <summary>
        /// Uživatel může stisknout Delete nad uzlem, bude vyvolána událost <see cref="DxTreeListNative.NodesDelete"/>
        /// </summary>
        bool CanDelete { get; }
        /// <summary>
        /// Co provede DoubleClick na textu anebo Click na ikoně?
        /// Pro akceptování této hodnoty musí <see cref="DxTreeList"/> mít nastaveno <see cref="DxTreeList.MainClickMode"/> == <see cref="NodeMainClickMode.AcceptNodeSetting"/>.
        /// </summary>
        NodeMainClickActionType MainClickAction { get; }
        /// <summary>
        /// Node má zobrazovat prostor pro zaškrtávátko, i když node sám zaškrtávátko nezobrazuje.
        /// Je to proto, aby nody nacházející se v jedné řadě pod sebou byly "svisle zarovnané" i když některé zaškrtávátko mají, a jiné nemají.
        /// </summary>
        bool AddVoidCheckSpace { get; }
        /// <summary>
        /// Node je vybraný (Selected).
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool Selected { get; set; }
        /// <summary>
        /// Node je zaškrtnutý. Na rozdíl od <see cref="ITextItem.Checked"/> (což je bool?) je zdejší property pouze bool.
        /// </summary>
        bool NodeChecked { get; set; }
        /// <summary>
        /// Node je otevřený.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool Expanded { get; set; }
        /// <summary>
        /// Ikona základní, ta může reagovat na stav Selected (pak bude zobrazena ikona <see cref="ImageDynamicSelected"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string ImageDynamicDefault { get; }
        /// <summary>
        /// Ikona ve stavu Node.IsSelected, zobrazuje se místo ikony <see cref="ImageDynamicDefault"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string ImageDynamicSelected { get; }
        /// <summary>
        /// Node bude mít Child prvky, ale zatím nejsou dodány. 
        /// Node bude zobrazovat rozbalovací ikonu a jeden node s textem "Načítám data...", viz <see cref="DxTreeListNative.LazyLoadNodeText"/>.
        /// Ikonu nastavíme v <see cref="DxTreeListNative.LazyLoadNodeImageName"/>. Otevřením tohoto nodu se vyvolá event <see cref="DxTreeListNative.LazyLoadChilds"/>.
        /// Třída <see cref="DxTreeListNative"/> si sama obhospodařuje tento "LazyLoadChildNode": vytváří jej a následně jej i odebírá.
        /// Aktivace tohoto nodu není hlášena jako event, node nelze editovat ani smazat uživatelem.
        /// </summary>
        bool LazyExpandable { get; set; }
        /// <summary>
        /// Relativní velikost písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        int? FontSizeDelta { get; }
        /// <summary>
        /// Změna stylu písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        FontStyle? FontStyleDelta { get; }
        /// <summary>
        /// Explicitní barva pozadí prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Explicitní barva písma prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeList metodou <see cref="DxTreeListNative"/>.Refresh();
        /// </summary>
        Color? ForeColor { get; }
        /// <summary>
        /// Pokud je node již umístěn v TreeList, pak tato metoda zajistí jeho refresh = promítne vizuální hodnoty do controlu
        /// </summary>
        void Refresh();
    }
    /// <summary>
    /// Typ uzlu
    /// </summary>
    public enum NodeItemType
    {
        /// <summary>
        /// Neurčeno, použije se <see cref="DefaultText"/>
        /// </summary>
        None = 0,
        /// <summary>
        /// Běžný prvek s daným textem
        /// </summary>
        DefaultText,
        /// <summary>
        /// Prázdný prvek na první pozici v poli Nodes, slouží k zadání NewItem
        /// </summary>
        BlankAtFirstPosition,
        /// <summary>
        /// Prázdný prvek na poslední pozici v poli Nodes, slouží k zadání NewItem
        /// </summary>
        BlankAtLastPosition,
        /// <summary>
        /// Prvek umístěný v jako jediný Child node v prvku, který bude donačítat svoje data OnDemand. 
        /// Tento prvek reprezentuje text "Zde budou data, až se načtou..."
        /// </summary>
        OnExpandLoading,
        /// <summary>
        /// Prvek umístěný jako poslední v poli Childs, reprezentuje např. text "DoubleKlik načte další data..."
        /// </summary>
        OnDoubleClickLoadNext
    }
    /// <summary>
    /// Druh akce, kterou provede DoubleClick na textu anebo Click na ikoně
    /// </summary>
    public enum NodeMainClickMode
    {
        /// <summary>
        /// Neprovede se nic
        /// </summary>
        None = 0,
        /// <summary>
        /// Provede se Expand node nebo Collapse node, ale nevolá se odpovídající událost
        /// </summary>
        ExpandCollapse,
        /// <summary>
        /// Volá se odpovídající událost, ale neprovede se Expand node nebo Collapse node
        /// </summary>
        RunEvent,
        /// <summary>
        /// Provede se Expand node nebo Collapse node, a zavolá se odpovídající událost
        /// </summary>
        ExpandCollapseRunEvent,
        /// <summary>
        /// Provede se pouze aktivita, uvedená na konkrétním nodu - podle hodnoty <see cref="ITreeListNode.MainClickAction"/>.
        /// <para/>
        /// V tomto případě je nezbytné nastavit na každém nodu tuto vlastnost, jinak bude default = None = nebude se dělat nic!!!
        /// </summary>
        AcceptNodeSetting
    }
    /// <summary>
    /// Druh akce na Click na ikonu anebo text.
    /// Akceptuje se pouze tehdy, když (<see cref="DxTreeList.MainClickMode"/> == <see cref="NodeMainClickMode.AcceptNodeSetting"/>);
    /// </summary>
    [Flags]
    public enum NodeMainClickActionType
    {
        /// <summary>
        /// Neprovádí se nic, typicky je aktivita řízena pro všechny nody společně pomocí <see cref="DxTreeList.MainClickMode"/>
        /// </summary>
        None = 0,
        /// <summary>
        /// Jedno kliknutí na ikonu provede Expand / Collapse nodu, pokud to je možné
        /// </summary>
        IconClickExpandCollapse = 0x01,
        /// <summary>
        /// Jedno kliknutí na ikonu vyvolá event <see cref="DxTreeList.NodeIconClick"/>
        /// </summary>
        IconClickRunEvent = 0x02,
        /// <summary>
        /// Dvojklik na text provede Expand / Collapse nodu, pokud to je možné
        /// </summary>
        TextDoubleClickExpandCollapse = 0x110,
        /// <summary>
        /// Dvojklik na text vyvolá event <see cref="DxTreeList.NodeDoubleClick"/>
        /// </summary>
        TextDoubleClickRunEvent = 0x20,

        /// <summary>
        /// Souhrn <see cref="IconClickExpandCollapse"/> + <see cref="TextDoubleClickExpandCollapse"/>
        /// </summary>
        ExpandCollapse = IconClickExpandCollapse | TextDoubleClickExpandCollapse,
        /// <summary>
        /// Souhrn <see cref="IconClickRunEvent"/> + <see cref="TextDoubleClickRunEvent"/>
        /// </summary>
        RunEvent = IconClickRunEvent | TextDoubleClickRunEvent
    }
    #endregion
}
