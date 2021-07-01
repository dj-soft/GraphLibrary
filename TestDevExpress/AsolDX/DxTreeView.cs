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
    #region DxTreeViewList
    /// <summary>
    /// Komponenta typu Panel, která v sobě obsahuje <see cref="DxTreeViewListNative"/>.
    /// </summary>
    public class DxTreeViewList : DxPanelControl
    {
        #region Konstruktor a vlastní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTreeViewList()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace komponent a hodnot
        /// </summary>
        private void Initialize()
        {
            _FilterBox = new DxFilterBox() { Dock = DockStyle.Top, Visible = false, TabIndex = 0 };
            _RegisterFilterRowEventHandlers();
            _TreeViewListNative = new DxTreeViewListNative() { Dock = DockStyle.Fill, TabIndex = 1 };
            _RegisterTreeViewEventHandlers();
            this.Controls.Add(_TreeViewListNative);
            this.Controls.Add(_FilterBox);
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        private DxFilterBox _FilterBox;
        private DxTreeViewListNative _TreeViewListNative;
        #endregion
        #region Vlastnosti DxTreeViewListNative
        /// <summary>
        /// Funkce, která pro název ikony vrátí její index v ImageListu
        /// </summary>
        public Func<string, int> ImageIndexSearcher { get { return _TreeViewListNative.ImageIndexSearcher; } set { _TreeViewListNative.ImageIndexSearcher = value; } }
        /// <summary>
        /// Text (lokalizovaný) pro text uzlu, který reprezentuje "LazyLoadChild", např. něco jako "Načítám data..."
        /// </summary>
        public string LazyLoadNodeText { get { return _TreeViewListNative.LazyLoadNodeText; } set { _TreeViewListNative.LazyLoadNodeText = value; } }
        /// <summary>
        /// Název ikony uzlu, který reprezentuje "LazyLoadChild", např. něco jako přesýpací hodiny...
        /// </summary>
        public string LazyLoadNodeImageName { get { return _TreeViewListNative.LazyLoadNodeImageName; } set { _TreeViewListNative.LazyLoadNodeImageName = value; } }
        /// <summary>
        /// Po LazyLoad aktivovat první načtený node?
        /// </summary>
        public TreeViewLazyLoadFocusNodeType LazyLoadFocusNode { get { return _TreeViewListNative.LazyLoadFocusNode; } set { _TreeViewListNative.LazyLoadFocusNode = value; } }
        /// <summary>
        /// Režim zobrazení Checkboxů. 
        /// Výchozí je <see cref="TreeViewCheckBoxMode.None"/>
        /// </summary>
        public TreeViewCheckBoxMode CheckBoxMode { get { return _TreeViewListNative.CheckBoxMode; } set { _TreeViewListNative.CheckBoxMode = value; } }
        /// <summary>
        /// Režim kreslení ikon u nodů.
        /// Výchozí je <see cref="TreeViewImageMode.Image0"/>.
        /// Aplikační kód musí dodat objekt do <see cref="ImageList"/>, jinak se ikony zobrazovat nebudou, 
        /// dále musí dodat metodu <see cref="ImageIndexSearcher"/> (která převede jméno ikony z nodu do indexu v <see cref="ImageList"/>)
        /// a musí plnit jména ikon do <see cref="ITreeListNode.ImageName0"/> atd.
        /// </summary>
        public TreeViewImageMode ImageMode { get { return _TreeViewListNative.ImageMode; } set { _TreeViewListNative.ImageMode = value; } }
        /// <summary>
        /// Knihovna ikon pro nody.
        /// Výchozí je null.
        /// Aplikační kód musí dodat objekt do <see cref="ImageList"/>, jinak se ikony zobrazovat nebudou, 
        /// dále musí dodat metodu <see cref="ImageIndexSearcher"/> (která převede jméno ikony z nodu do indexu v <see cref="ImageList"/>)
        /// a musí plnit jména ikon do <see cref="ITreeListNode.ImageName0"/> atd.
        /// <para/>
        /// Nepoužívejme přímo SelectImageList ani StateImageList.
        /// </summary>
        public ImageList ImageList { get { return _TreeViewListNative.ImageList; } set { _TreeViewListNative.ImageList = value; } }
        /// <summary>
        /// Akce, která zahájí editaci buňky
        /// </summary>
        public DevExpress.XtraTreeList.TreeListEditorShowMode EditorShowMode { get { return _TreeViewListNative.EditorShowMode; } set { _TreeViewListNative.EditorShowMode = value; } }
        /// <summary>
        /// Pozadí TreeListu je transparentní (pak je vidět podkladový Panel)
        /// </summary>
        public bool TransparentBackground { get { return _TreeViewListNative.TransparentBackground; } set { _TreeViewListNative.TransparentBackground = value; } }
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy?
        /// </summary>
        public bool ToolTipAllowHtmlText { get { return _TreeViewListNative.ToolTipAllowHtmlText; } set { _TreeViewListNative.ToolTipAllowHtmlText = value; } }
        /// <summary>
        /// Nastaví danou barvu jako všechny barvy pozadí
        /// </summary>
        /// <param name="backColor"></param>
        public void SetBackColor(System.Drawing.Color backColor) { _TreeViewListNative.SetBackColor(backColor); }
        #endregion
        #region Nody DxTreeViewListNative: aktivní node, kolekce nodů, vyhledání, přidání, odebrání
        /// <summary>
        /// Aktuálně vybraný Node
        /// </summary>
        public ITreeListNode FocusedNodeInfo { get { return _TreeViewListNative.FocusedNodeInfo; } }
        /// <summary>
        /// Najde node podle jeho klíče, pokud nenajde pak vrací false.
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        public bool TryGetNodeInfo(string nodeKey, out ITreeListNode nodeInfo) { return _TreeViewListNative.TryGetNodeInfo(nodeKey, out nodeInfo); }
        /// <summary>
        /// Pole všech nodů = třída <see cref="ITreeListNode"/> = data o nodech
        /// </summary>
        public ITreeListNode[] NodeInfos { get { return _TreeViewListNative.NodeInfos; } }
        /// <summary>
        /// Najde a vrátí pole nodů, které jsou Child nody daného klíče.
        /// Reálně provádí Scan všech nodů.
        /// </summary>
        /// <param name="parentKey"></param>
        /// <returns></returns>
        public ITreeListNode[] GetChildNodeInfos(string parentKey) { return _TreeViewListNative.GetChildNodeInfos(parentKey); }
        /// <summary>
        /// Přidá jeden node. Není to příliš efektivní. Raději používejme <see cref="AddNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        public void AddNode(ITreeListNode nodeInfo, int? atIndex = null) { _TreeViewListNative.AddNode(nodeInfo, atIndex); }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Na konci provede Refresh.
        /// </summary>
        /// <param name="addNodes"></param>
        public void AddNodes(IEnumerable<ITreeListNode> addNodes) { _TreeViewListNative.AddNodes(addNodes); }
        /// <summary>
        /// Přidá řadu nodů, které jsou donačteny k danému parentu. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Nejprve najde daného parenta, a zruší z něj příznak LazyLoad (protože právě tímto načtením jsou jeho nody vyřešeny). Současně odebere "wait" node (prázdný node, simulující načítání dat).
        /// Pak teprve přidá nové nody.
        /// Na konci provede Refresh.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <param name="addNodes"></param>
        public void AddLazyLoadNodes(string parentNodeId, IEnumerable<ITreeListNode> addNodes) { _TreeViewListNative.AddLazyLoadNodes(parentNodeId, addNodes); }
        /// <summary>
        /// Selectuje daný Node
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void SelectNode(ITreeListNode nodeInfo) { _TreeViewListNative.SelectNode(nodeInfo); }
        /// <summary>
        /// Odebere jeden daný node, podle klíče. Na konci provede Refresh.
        /// Pro odebrání více nodů je lepší použít <see cref="RemoveNodes(IEnumerable{string})"/>.
        /// </summary>
        /// <param name="removeNodeKey"></param>
        public void RemoveNode(string removeNodeKey) { _TreeViewListNative.RemoveNode(removeNodeKey); }
        /// <summary>
        /// Odebere řadu nodů, podle klíče. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        public void RemoveNodes(IEnumerable<string> removeNodeKeys) { _TreeViewListNative.RemoveNodes(removeNodeKeys); }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        public void RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes) { _TreeViewListNative.RemoveAddNodes(removeNodeKeys, addNodes); }
        /// <summary>
        /// Smaže všechny nodes. Na konci provede Refresh.
        /// </summary>
        public void ClearNodes() { _TreeViewListNative.ClearNodes(); }
        /// <summary>
        /// Zajistí refresh jednoho daného nodu. 
        /// Pro refresh více nodů použijme <see cref="RefreshNodes(IEnumerable{ITreeListNode})"/>!
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void RefreshNode(ITreeListNode nodeInfo) { _TreeViewListNative.RefreshNode(nodeInfo); }
        /// <summary>
        /// Zajistí refresh daných nodů.
        /// </summary>
        /// <param name="nodes"></param>
        public void RefreshNodes(IEnumerable<ITreeListNode> nodes) { _TreeViewListNative.RefreshNodes(nodes); }

        #endregion
        #region FilterRow
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="_FilterBox"/>
        /// </summary>
        private void _RegisterFilterRowEventHandlers()
        {
            _FilterBox.FilterValueChangedSources = DxFilterRowChangeEventSource.DefaultGreen;
            _FilterBox.FilterValueChanged += FilterBox_Changed;      // Změna obsahu filtru a Enter
            _FilterBox.KeyEnterPress += FilterBox_KeyEnter;
        }
        /// <summary>
        /// Zobrazovat řádkový filtr? Default = NE
        /// </summary>
        public bool FilterBoxVisible { get { return _FilterBoxVisible; } set { _FilterBoxVisible = value; this.RunInGui(FilterBoxSetVisible); } } private bool _FilterBoxVisible = false;
        /// <summary>
        /// Instance řádkového filtru
        /// </summary>
        public DxFilterBox FilterBox { get { return _FilterBox; } }
        /// <summary>
        /// Aktuální text v řádkovém filtru
        /// </summary>
        public DxFilterBoxValue FilterBoxValue { get { return _FilterBox.FilterValue; } set { _FilterBox.FilterValue = value; } }
        /// <summary>
        /// Pole operátorů nabízených pod tlačítkem vlevo.
        /// </summary>
        public List<IMenuItem> FilterBoxOperators { get { return _FilterBox.FilterOperators; } set { _FilterBox.FilterOperators = value; } }
        /// <summary>
        /// Provede se po jakékoli změně v řádkovém filtru:
        /// 1. Po zadání textu (včetně možné změny typu filtru) a klávese Enter;
        /// 2. Po smazání textu tlačítkem Clear;
        /// <para/>
        /// Nevolá se v těchto případech:
        /// a. Změna typu filtru v menu (pak přejde focus do textu a uživatel má dát Enter)
        /// b. Průběžná editace textu
        /// c. Odchod z textového políčka pomocí Tab nebo myši, i po změně obsahu textu: tak se choval původní prvek
        /// </summary>
        public event EventHandler<TEventArgs<DxFilterBoxValue>> FilterBoxChanged;
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
        /// <param name="e"></param>
        private void FilterBox_Changed(object sender, TEventArgs<DxFilterBoxValue> e)
        {
            OnFilterBoxChanged(e);
            FilterBoxChanged?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po jakékoli změně v řádkovém filtru
        /// </summary>
        protected virtual void OnFilterBoxChanged(TEventArgs<DxFilterBoxValue> e) { }
        /// <summary>
        /// Po stisku Enter v řádkovém filtru
        /// </summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterBox_KeyEnter(object sender, EventArgs e)
        {
            _TreeViewListNative.Focus();
            OnFilterBoxKeyEnter();
            FilterBoxKeyEnter?.Invoke(this, e);
        }
        /// <summary>
        /// Proběhne po stisku Enter v řádkovém filtru
        /// </summary>
        protected virtual void OnFilterBoxKeyEnter() { }
        #endregion
        #region Eventy a další akce DxTreeViewListNative
        /// <summary>
        /// Zaregistruje zdejší eventhandlery na události v nativním <see cref="_TreeViewListNative"/>
        /// </summary>
        private void _RegisterTreeViewEventHandlers()
        {
            _TreeViewListNative.NodeSelected += _TreeViewListNative_NodeSelected;
            _TreeViewListNative.NodeIconClick += _TreeViewListNative_NodeIconClick;
            _TreeViewListNative.NodeDoubleClick += _TreeViewListNative_NodeDoubleClick;
            _TreeViewListNative.NodeExpanded += _TreeViewListNative_NodeExpanded;
            _TreeViewListNative.NodeCollapsed += _TreeViewListNative_NodeCollapsed;
            _TreeViewListNative.ActivatedEditor += _TreeViewListNative_ActivatedEditor;
            _TreeViewListNative.EditorDoubleClick += _TreeViewListNative_EditorDoubleClick;
            _TreeViewListNative.NodeEdited += _TreeViewListNative_NodeEdited;
            _TreeViewListNative.NodeCheckedChange += _TreeViewListNative_NodeCheckedChange;
            _TreeViewListNative.NodeDelete += _TreeViewListNative_NodeDelete;
            _TreeViewListNative.LazyLoadChilds += _TreeViewListNative_LazyLoadChilds;
        }
        private void _TreeViewListNative_NodeSelected(object sender, DxTreeViewNodeArgs args) { this.OnNodeSelected(args); this.NodeSelected?.Invoke(this, args); }
        private void _TreeViewListNative_NodeIconClick(object sender, DxTreeViewNodeArgs args) { this.OnNodeIconClick(args); this.NodeIconClick?.Invoke(this, args); }
        private void _TreeViewListNative_NodeDoubleClick(object sender, DxTreeViewNodeArgs args) { this.OnNodeDoubleClick(args); this.NodeDoubleClick?.Invoke(this, args); }
        private void _TreeViewListNative_NodeExpanded(object sender, DxTreeViewNodeArgs args) { this.OnNodeExpanded(args); this.NodeExpanded?.Invoke(this, args); }
        private void _TreeViewListNative_NodeCollapsed(object sender, DxTreeViewNodeArgs args) { this.OnNodeCollapsed(args); this.NodeCollapsed?.Invoke(this, args); }
        private void _TreeViewListNative_ActivatedEditor(object sender, DxTreeViewNodeArgs args) { this.OnActivatedEditor(args); this.ActivatedEditor?.Invoke(this, args); }
        private void _TreeViewListNative_EditorDoubleClick(object sender, DxTreeViewNodeArgs args) { this.OnEditorDoubleClick(args); this.EditorDoubleClick?.Invoke(this, args); }
        private void _TreeViewListNative_NodeEdited(object sender, DxTreeViewNodeArgs args) { this.OnNodeEdited(args); this.NodeEdited?.Invoke(this, args); }
        private void _TreeViewListNative_NodeCheckedChange(object sender, DxTreeViewNodeArgs args) { this.OnNodeCheckedChange(args); this.NodeCheckedChange?.Invoke(this, args); }
        private void _TreeViewListNative_NodeDelete(object sender, DxTreeViewNodeArgs args) { this.OnNodeDelete(args); this.NodeDelete?.Invoke(this, args); }
        private void _TreeViewListNative_LazyLoadChilds(object sender, DxTreeViewNodeArgs args) { this.OnLazyLoadChilds(args); this.LazyLoadChilds?.Invoke(this, args); }

        /// <summary>
        /// TreeView aktivoval určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeSelected;
        /// <summary>
        /// Vyvolá event <see cref="NodeSelected"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeSelected(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView má Mouseclick na ikonu pro určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeIconClick;
        /// <summary>
        /// Vyvolá event <see cref="NodeIconClick"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeIconClick(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView má Doubleclick na určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="NodeDoubleClick"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeDoubleClick(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyLoadChilds"/>).
        /// </summary>
        public event DxTreeViewNodeHandler NodeExpanded;
        /// <summary>
        /// Vyvolá event <see cref="NodeExpanded"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeExpanded(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView právě sbaluje určitý Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeCollapsed;
        /// <summary>
        /// Vyvolá event <see cref="NodeCollapsed"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeCollapsed(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        public event DxTreeViewNodeHandler ActivatedEditor;
        /// <summary>
        /// Vyvolá event <see cref="ActivatedEditor"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnActivatedEditor(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        public event DxTreeViewNodeHandler EditorDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="EditorDoubleClick"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnEditorDoubleClick(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView právě skončil editaci určitého Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeEdited;
        /// <summary>
        /// Vyvolá event <see cref="NodeEdited"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeEdited(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        public event DxTreeViewNodeHandler NodeCheckedChange;
        /// <summary>
        /// Vyvolá event <see cref="NodeCheckedChange"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeCheckedChange(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// Uživatel dal Delete na uzlu, který se needituje.
        /// </summary>
        public event DxTreeViewNodeHandler NodeDelete;
        /// <summary>
        /// Vyvolá event <see cref="NodeDelete"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnNodeDelete(DxTreeViewNodeArgs args) { }
        /// <summary>
        /// TreeView rozbaluje node, který má nastaveno načítání ze serveru : <see cref="ITreeListNode.LazyLoadChilds"/> je true.
        /// </summary>
        public event DxTreeViewNodeHandler LazyLoadChilds;
        /// <summary>
        /// Vyvolá event <see cref="LazyLoadChilds"/>
        /// </summary>
        /// <param name="args">Data o události</param>
        protected virtual void OnLazyLoadChilds(DxTreeViewNodeArgs args) { }

        /// <summary>
        /// Zajistí provedení dodané akce s argumenty v GUI threadu a v jednom vizuálním zámku s jedním Refreshem na konci.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public void RunInLock(Delegate method, params object[] args) { _TreeViewListNative.RunInLock(method, args); }
        #endregion
    }
    #endregion
    #region DxTreeViewListNative
    /// <summary>
    /// <see cref="DxTreeViewListNative"/> : potomek <see cref="DevExpress.XtraTreeList.TreeList"/> s podporou pro použití v Greenu.
    /// Nemá se používat přímo, má se používat <see cref="DxTreeViewList"/>.
    /// </summary>
    public class DxTreeViewListNative : DevExpress.XtraTreeList.TreeList
    {
        #region Konstruktor a inicializace, privátní proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTreeViewListNative()
        {
            this._LastId = 0;
            this._NodesId = new Dictionary<int, NodePair>();
            this._NodesKey = new Dictionary<string, NodePair>();
            this.InitTreeView();
        }
        /// <summary>
        /// Incializace komponenty Simple
        /// </summary>
        protected void InitTreeView()
        {
            this.OptionsBehavior.PopulateServiceColumns = false;
            this._MainColumn = new DevExpress.XtraTreeList.Columns.TreeListColumn() { Name = "MainColumn", Visible = true, Width = 150, UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.String, Caption = "Sloupec1", AllowIncrementalSearch = true, FieldName = "Text", ShowButtonMode = DevExpress.XtraTreeList.ShowButtonModeEnum.ShowForFocusedRow, ToolTip = "Tooltip pro sloupec" };
            this.Columns.Add(this._MainColumn);

            this._MainColumn.OptionsColumn.AllowEdit = false;
            this._MainColumn.OptionsColumn.AllowSort = false;

            this.OptionsBehavior.AllowExpandOnDblClick = true;
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

            this.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.OptionsSelection.EnableAppearanceHotTrackedRow = DefaultBoolean.True; // DevExpress.Utils.DefaultBoolean.True;
            this.OptionsSelection.InvertSelection = true;

            this.ViewStyle = DevExpress.XtraTreeList.TreeListViewStyle.TreeView;

            // DirectX vypadá OK:
            this.UseDirectXPaint = DefaultBoolean.True;
            this.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.True;                // Běžně nezapínat, ale na DirectX to chodí!   Nezapínej to, DevExpress mají (v 20.1.6.0) problém s vykreslováním!

            // Tooltip:
            this.ToolTipController = DxComponent.DefaultToolTipController;
            this.ToolTipController.GetActiveObjectInfo += ToolTipController_GetActiveObjectInfo;

            // Eventy pro podporu TreeView (vykreslení nodu, atd):
            this.NodeCellStyle += _OnNodeCellStyle;
            this.CustomDrawNodeCheckBox += _OnCustomDrawNodeCheckBox;
            this.KeyDown += _OnKeyDown;

            // Nativní eventy:
            this.FocusedNodeChanged += _OnFocusedNodeChanged;
            this.MouseClick += _OnMouseClick;
            this.DoubleClick += _OnDoubleClick;
            this.ShownEditor += _OnShownEditor;
            this.ValidatingEditor += _OnValidatingEditor;
            this.BeforeCheckNode += _OnBeforeCheckNode;
            this.AfterCheckNode += _OnAfterCheckNode;
            this.BeforeExpand += _OnBeforeExpand;
            this.AfterCollapse += _OnAfterCollapse;

            // Preset:
            this.LazyLoadNodeText = "...";
            this.LazyLoadNodeImageName = null;
            this.CheckBoxMode = TreeViewCheckBoxMode.None;
            this.ImageMode = TreeViewImageMode.Image0;
        }
        DevExpress.XtraTreeList.Columns.TreeListColumn _MainColumn;
        private Dictionary<int, NodePair> _NodesId;
        private Dictionary<string, NodePair> _NodesKey;
        private int _LastId;
        /// <summary>
        /// Třída obsahující jeden pár dat: vizuální plus datová
        /// </summary>
        private class NodePair
        {
            public NodePair(DxTreeViewListNative owner, int nodeId, ITreeListNode nodeInfo, DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, bool isLazyChild)
            {
                this.Id = nodeId;
                this.NodeInfo = nodeInfo;
                this.TreeNode = treeNode;
                this.IsLazyChild = isLazyChild;

                this.NodeInfo.Id = nodeId;
                this.NodeInfo.Owner = owner;
                this.TreeNode.Tag = nodeId;
            }
            public void ReleasePair()
            {
                this.NodeInfo.Id = -1;
                this.NodeInfo.Owner = null;
                this.TreeNode.Tag = null;

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
            /// Konstantní ID tohoto nodu, nemění se
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// Aktuální interní ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
            /// Tato hodnota se mění při odebrání nodu z TreeView. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
            /// </summary>
            public int CurrentTreeNodeId { get { return TreeNode?.Id ?? -1; } }
            /// <summary>
            /// Klíč nodu, string
            /// </summary>
            public string NodeId { get { return NodeInfo?.FullNodeId; } }
            /// <summary>
            /// Datový objekt
            /// </summary>
            public ITreeListNode NodeInfo { get; private set; }
            /// <summary>
            /// Vizuální objekt
            /// </summary>
            public DevExpress.XtraTreeList.Nodes.TreeListNode TreeNode { get; private set; }
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
        /// Aplikovat průhledné pozadí
        /// </summary>
        private void _ApplyTransparentBackground()
        {
            if (_TransparentBackground)
            {
                Color backColor = Color.Transparent;
                // this.BackColor = backColor;
                this.Appearance.Empty.BackColor = backColor;
                this.Appearance.Empty.Options.UseBackColor = true;
                this.Appearance.Row.BackColor = backColor;
                this.Appearance.Row.Options.UseBackColor = true;
            }
            else
            {
                // this.BackColor = ;
                this.Appearance.Empty.Options.UseBackColor = false;
                this.Appearance.Row.Options.UseBackColor = false;
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
        #endregion
        #region ToolTipy pro nodes
        /// <summary>
        /// ToolTipy mohou obsahovat SimpleHtml tagy?
        /// </summary>
        public bool ToolTipAllowHtmlText { get; set; }

        /// <summary>
        /// Připraví ToolTip pro aktuální Node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTipController_GetActiveObjectInfo(object sender, ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
            if (e.SelectedControl is DevExpress.XtraTreeList.TreeList tree)
            {
                var hit = tree.CalcHitInfo(e.ControlMousePosition);
                if (hit.HitInfoType == DevExpress.XtraTreeList.HitInfoType.Cell || hit.HitInfoType == DevExpress.XtraTreeList.HitInfoType.SelectImage || hit.HitInfoType == DevExpress.XtraTreeList.HitInfoType.StateImage)
                {
                    var nodeInfo = this._GetNodeInfo(hit.Node);
                    if (nodeInfo != null && (!String.IsNullOrEmpty(nodeInfo.ToolTipTitle) || !String.IsNullOrEmpty(nodeInfo.ToolTipText)))
                    {
                        string toolTipText = nodeInfo.ToolTipText;
                        string toolTipTitle = nodeInfo.ToolTipTitle ?? nodeInfo.Text;
                        object cellInfo = new DevExpress.XtraTreeList.ViewInfo.TreeListCellToolTipInfo(hit.Node, hit.Column, null);
                        var ttci = new DevExpress.Utils.ToolTipControlInfo(cellInfo, toolTipText, toolTipTitle);
                        ttci.ToolTipType = ToolTipType.SuperTip;
                        ttci.AllowHtmlText = (ToolTipAllowHtmlText ? DefaultBoolean.True : DefaultBoolean.False);
                        e.Info = ttci;
                    }
                }
            }
        }
        #endregion
        #region Řízení specifického vykreslení TreeNodu podle jeho nastavení: font, barvy, checkbox, atd
        /// <summary>
        /// Vytvoří new instanci pro řízení vzhledu TreeView
        /// </summary>
        /// <returns></returns>
        protected override DevExpress.XtraTreeList.ViewInfo.TreeListViewInfo CreateViewInfo()
        {
            if (CurrentViewInfo == null)
                CurrentViewInfo = new DxTreeViewViewInfo(this);
            return CurrentViewInfo;
        }
        /// <summary>
        /// Při Dispose uvolním svůj lokální <see cref="CurrentViewInfo"/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (CurrentViewInfo != null)
            {
                CurrentViewInfo.Dispose();
                CurrentViewInfo = null;
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Instance pro řízení vzhledu TreeView
        /// </summary>
        protected DxTreeViewViewInfo CurrentViewInfo;
        /// <summary>
        /// Potomek pro řízení vzhledu s ohledem na [ne]vykreslení CheckBoxů
        /// </summary>
        protected class DxTreeViewViewInfo : DevExpress.XtraTreeList.ViewInfo.TreeListViewInfo, IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="treeList"></param>
            public DxTreeViewViewInfo(DxTreeViewListNative treeList) : base(treeList)
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
            private DxTreeViewListNative _Owner;
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
                showSpace = (checkMode == TreeViewCheckBoxMode.AllNodes || (checkMode == TreeViewCheckBoxMode.SpecifyByNode && (nodeInfo.CanCheck || nodeInfo.AddVoidCheckSpace)));
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
                isCheckable = (checkMode == TreeViewCheckBoxMode.AllNodes || (checkMode == TreeViewCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
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
                bool canCheck = (checkMode == TreeViewCheckBoxMode.AllNodes || (checkMode == TreeViewCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
                args.Handled = !canCheck;
            }
        }
        #endregion
        #region Interní události a jejich zpracování : Klávesa, Focus, DoubleClick, Editor, Specifika vykreslení, Expand, 
        /// <summary>
        /// Po stisku klávesy Vpravo a Vlevo se pracuje s Expanded nodů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            DevExpress.XtraTreeList.Nodes.TreeListNode node;
            switch (e.KeyData)
            {
                case Keys.Right:
                    node = this.FocusedNode;
                    if (node != null && node.HasChildren && !node.Expanded)
                    {
                        node.Expanded = true;
                        e.Handled = true;
                    }
                    break;
                case Keys.Left:
                    node = this.FocusedNode;
                    if (node != null)
                    {
                        if (node.HasChildren && node.Expanded)
                        {
                            node.Expanded = false;
                            e.Handled = true;
                        }
                        else if (node.ParentNode != null)
                        {
                            this.FocusedNode = node.ParentNode;
                            e.Handled = true;
                        }
                    }
                    break;
                case Keys.Delete:
                    if (!IsActiveEditor)
                    {   // Mimo editor reagujeme na Delete jako DeleteNode:
                        this._OnNodeDelete(this.FocusedNodeInfo);
                        e.Handled = true;
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
                                e.Handled = true;
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
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }
        }
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
                this.OnNodeSelected(nodeInfo);
        }
        /// <summary>
        /// MouseClick vyvolá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnMouseClick(object sender, MouseEventArgs e)
        {
            var hit = this.CalcHitInfo(e.Location);
            if (hit.HitInfoType == DevExpress.XtraTreeList.HitInfoType.StateImage || hit.HitInfoType == DevExpress.XtraTreeList.HitInfoType.SelectImage)
            {
                ITreeListNode nodeInfo = this.FocusedNodeInfo;
                if (nodeInfo != null)
                    this.OnNodeIconClick(nodeInfo);
            }
        }
        /// <summary>
        /// Doubleclick převolá public event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnDoubleClick(object sender, EventArgs e)
        {
            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnNodeDoubleClick(nodeInfo);
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
            }

            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnActivatedEditor(nodeInfo);
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
                this.OnNodeEdited(nodeInfo, this.EditingValue);
        }
        /// <summary>
        /// Doubleclick v editoru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnEditorDoubleClick(object sender, EventArgs e)
        {
            ITreeListNode nodeInfo = this.FocusedNodeInfo;
            if (nodeInfo != null)
                this.OnEditorDoubleClick(nodeInfo, this.EditingValue);
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
                args.CanCheck = (checkMode == TreeViewCheckBoxMode.AllNodes || (checkMode == TreeViewCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck));
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
            if (nodeInfo != null && (checkMode == TreeViewCheckBoxMode.AllNodes || (checkMode == TreeViewCheckBoxMode.SpecifyByNode && nodeInfo.CanCheck)))
            {
                bool isChecked = args.Node.Checked;
                nodeInfo.IsChecked = isChecked;
                this.OnNodeCheckedChange(nodeInfo, isChecked);
            }
        }
        /// <summary>
        /// Po klávese Delete nad nodem bez editace
        /// </summary>
        /// <param name="nodeInfo"></param>
        private void _OnNodeDelete(ITreeListNode nodeInfo)
        {
            if (nodeInfo != null && nodeInfo.CanDelete)
                this.OnNodeDelete(nodeInfo);
        }
        /// <summary>
        /// Před rozbalením nodu se volá public event <see cref="NodeExpanded"/>,
        /// a pokud node má nastaveno <see cref="ITreeListNode.LazyLoadChilds"/> = true, pak se ovlá ještě <see cref="LazyLoadChilds"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnBeforeExpand(object sender, DevExpress.XtraTreeList.BeforeExpandEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                nodeInfo.Expanded = true;
                this.OnNodeExpanded(nodeInfo);                       // Zatím nevyužívám možnost zakázání Expand, kterou dává args.CanExpand...
                if (nodeInfo.LazyLoadChilds)
                    this.OnLazyLoadChilds(nodeInfo);
            }
        }
        /// <summary>
        /// Po zabalení nodu se volá public event <see cref="NodeCollapsed"/>,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _OnAfterCollapse(object sender, DevExpress.XtraTreeList.NodeEventArgs args)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(args.Node);
            if (nodeInfo != null)
            {
                nodeInfo.Expanded = false;
                this.OnNodeCollapsed(nodeInfo);
            }
        }
        /// <summary>
        /// Obsahuje true pokud je zrovna aktivní editor textu v nodu
        /// </summary>
        private bool IsActiveEditor { get { return (this.EditorHelper.ActiveEditor != null); } }
        #endregion
        #region Správa nodů (přidání, odebrání, smazání, změny)
        /// <summary>
        /// Přidá jeden node. Není to příliš efektivní. Raději používejme <see cref="AddNodes(IEnumerable{ITreeListNode})"/>.
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
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Na konci provede Refresh.
        /// </summary>
        /// <param name="addNodes"></param>
        public void AddNodes(IEnumerable<ITreeListNode> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<ITreeListNode>>(AddNodes), addNodes); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(null, addNodes);
            }
        }
        /// <summary>
        /// Přidá řadu nodů, které jsou donačteny k danému parentu. Současné nody ponechává. Lze tak přidat například jednu podvětev.
        /// Nejprve najde daného parenta, a zruší z něj příznak LazyLoad (protože právě tímto načtením jsou jeho nody vyřešeny). Současně odebere "wait" node (prázdný node, simulující načítání dat).
        /// Pak teprve přidá nové nody.
        /// Na konci provede Refresh.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <param name="addNodes"></param>
        public void AddLazyLoadNodes(string parentNodeId, IEnumerable<ITreeListNode> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string, IEnumerable<ITreeListNode>>(AddLazyLoadNodes), parentNodeId, addNodes); return; }

            using (LockGui(true))
            {
                bool isAnySelected = this._RemoveLazyLoadFromParent(parentNodeId);
                var firstPair = this._RemoveAddNodes(null, addNodes);

                var focusType = this.LazyLoadFocusNode;
                if (firstPair != null && (isAnySelected || focusType == TreeViewLazyLoadFocusNodeType.FirstChildNode))
                    this.SetFocusedNode(firstPair.TreeNode);
                else if (focusType == TreeViewLazyLoadFocusNodeType.ParentNode)
                {
                    var parentPair = this._GetNodePair(parentNodeId);
                    if (parentPair != null)
                        this.FocusedNode = parentPair.TreeNode;
                }
            }
        }
        /// <summary>
        /// Selectuje daný Node
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void SelectNode(ITreeListNode nodeInfo)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<ITreeListNode>(SelectNode), nodeInfo); return; }

            using (LockGui(true))
            {
                if (nodeInfo != null && nodeInfo.Id >= 0 && this._NodesId.TryGetValue(nodeInfo.Id, out var nodePair))
                {
                    this.SetFocusedNode(nodePair.TreeNode);
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
                this._RemoveAddNodes(new string[] { removeNodeKey }, null);
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
                this._RemoveAddNodes(removeNodeKeys, null);
            }
        }
        /// <summary>
        /// Přidá řadu nodů. Současné nody ponechává. Lze tak přidat například jednu podvětev. Na konci provede Refresh.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        public void RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<IEnumerable<string>, IEnumerable<ITreeListNode>>(RemoveAddNodes), removeNodeKeys, addNodes); return; }

            using (LockGui(true))
            {
                this._RemoveAddNodes(removeNodeKeys, addNodes);
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
            if (nodeInfo.Id < 0) throw new ArgumentException($"Cannot refresh node '{nodeInfo.FullNodeId}': '{nodeInfo.Text}' if the node is not in TreeView.");

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
        #endregion
        #region Provádění akce v jednom zámku
        /// <summary>
        /// Zajistí provedení dodané akce s argumenty v GUI threadu a v jednom vizuálním zámku s jedním Refreshem na konci.
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
            if (IsLocked) return new LockTreeViewGuiInfo();
            return new LockTreeViewGuiInfo(this, withRefresh);
        }
        /// <summary>
        /// Příznak, zda je objekt odemčen (false) nebo zamčen (true).
        /// Objekt se zamkne vytvořením první instance <see cref="LockTreeViewGuiInfo"/>, 
        /// následující vytváření i Dispose nových instancí týchž objektů již stav nezmění, 
        /// a až Dispose posledního zámku objekt zase odemkne a volitelně provede Refresh.
        /// </summary>
        protected bool IsLocked;
        /// <summary>
        /// IDisposable objekt pro párové operace se zamknutím / odemčením GUI
        /// </summary>
        protected class LockTreeViewGuiInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor pro "vnořený" zámek, který nic neprovádí
            /// </summary>
            public LockTreeViewGuiInfo() { }
            /// <summary>
            /// Konstruktor standardní
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="withRefresh"></param>
            public LockTreeViewGuiInfo(DxTreeViewListNative owner, bool withRefresh)
            {
                if (owner != null)
                {
                    owner.IsLocked = true;
                    ((System.ComponentModel.ISupportInitialize)(owner)).BeginInit();
                    owner.BeginUnboundLoad();

                    _Owner = owner;
                    _WithRefresh = withRefresh;
                    _FocusedNodeId = owner.FocusedNodeInfo?.FullNodeId;
                }
            }
            void IDisposable.Dispose()
            {
                var owner = _Owner;
                if (owner != null)
                {
                    owner.EndUnboundLoad();
                    ((System.ComponentModel.ISupportInitialize)(owner)).EndInit();

                    if (_WithRefresh)
                        owner.Refresh();

                    owner.IsLocked = false;

                    var focusedNodeInfo = owner.FocusedNodeInfo;
                    string oldNodeId = _FocusedNodeId;
                    string newNodeId = focusedNodeInfo?.FullNodeId;
                    if (!String.Equals(oldNodeId, newNodeId))
                        owner.OnNodeSelected(focusedNodeInfo);
                }
            }
            private DxTreeViewListNative _Owner;
            private bool _WithRefresh;
            private string _FocusedNodeId;
        }
        #region Private sféra
        /// <summary>
        /// Odebere nody ze stromu a z evidence.
        /// Přidá více node do stromu a do evidence, neřeší blokování GUI.
        /// Metoda vrací první vytvořený <see cref="NodePair"/>.
        /// </summary>
        /// <param name="removeNodeKeys"></param>
        /// <param name="addNodes"></param>
        private NodePair _RemoveAddNodes(IEnumerable<string> removeNodeKeys, IEnumerable<ITreeListNode> addNodes)
        {
            NodePair firstPair = null;

            // Remove:
            if (removeNodeKeys != null)
            {
                foreach (var nodeKey in removeNodeKeys)
                    this._RemoveNode(nodeKey);
            }

            // Add:
            if (addNodes != null)
            {
                foreach (var node in addNodes)
                    this._AddNode(node, ref firstPair, null);

                // Expand nody: teď už by měly mít svoje Childs přítomné v TreeView:
                foreach (var node in addNodes.Where(n => n.Expanded))
                    this._NodesId[node.Id].TreeNode.Expanded = true;
            }

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

            if (nodeInfo.LazyLoadChilds)
                _AddNodeLazyLoad(nodeInfo);                                    // Pokud node má nastaveno LazyLoadChilds, pak pod něj vložím jako jeho Child nový node, reprezentující "načítání z databáze"
        }
        private void _AddNodeLazyLoad(ITreeListNode parentNode)
        {
            string lazyChildId = parentNode.FullNodeId + "___«LazyLoadChildNode»___";
            string text = this.LazyLoadNodeText ?? "Načítám...";
            string imageName = this.LazyLoadNodeImageName;
            ITreeListNode lazyNode = new TreeListNode(lazyChildId, parentNode.FullNodeId, text, nodeType: NodeItemType.OnExpandLoading, imageName: imageName, fontStyleDelta: FontStyle.Italic);
            NodePair nodePair = _AddNodeOne(lazyNode, true, null);             // Daný node (z aplikace) vloží do Tree a vrátí
        }
        /// <summary>
        /// Fyzické přidání jednoho node do TreeView a do evidence
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isLazyChild"></param>
        /// <param name="atIndex">Zařadit na danou pozici v kolekci Child nodů: 0=dá node na první pozici, 1=na druhou pozici, null = default = na poslední pozici.</param>
        private NodePair _AddNodeOne(ITreeListNode nodeInfo, bool isLazyChild, int? atIndex)
        {
            // Kontrola duplicity raději předem:
            string nodeId = nodeInfo.FullNodeId;
            if (nodeId != null && this._NodesKey.ContainsKey(nodeId)) throw new ArgumentException($"It is not possible to add an element because an element with the same key '{nodeId}' already exists in the TreeView.");

            // 1. Vytvoříme TreeListNode:
            object nodeData = new object[] { nodeInfo.Text };
            int parentId = _GetCurrentTreeNodeId(nodeInfo.ParentFullNodeId);
            var treeNode = this.AppendNode(nodeData, parentId);

            if (atIndex.HasValue)
                this.SetNodeIndex(treeNode, atIndex.Value);

            _FillTreeNode(treeNode, nodeInfo, false);

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
            if (nodeInfo != null && nodeInfo.FullNodeId != null && this._NodesKey.TryGetValue(nodeInfo.FullNodeId, out var nodePair))
            {
                _FillTreeNode(nodePair.TreeNode, nodePair.NodeInfo, true);
            }
        }
        /// <summary>
        /// Do daného <see cref="TreeListNode"/> vepíše všechny potřebné informace z datového <see cref="ITreeListNode"/>.
        /// Jde o: text, stav zaškrtnutí, ikony, rozbalení nodu.
        /// </summary>
        /// <param name="treeNode"></param>
        /// <param name="nodeInfo"></param>
        /// <param name="canExpand"></param>
        private void _FillTreeNode(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode, ITreeListNode nodeInfo, bool canExpand)
        {
            treeNode.SetValue(0, nodeInfo.Text);
            treeNode.Checked = nodeInfo.CanCheck && nodeInfo.IsChecked;
            int imageIndex = _GetImageIndex(nodeInfo.ImageName0, -1);
            treeNode.ImageIndex = imageIndex;                                                      // ImageIndex je vlevo, a může se změnit podle stavu Seleted
            treeNode.SelectImageIndex = _GetImageIndex(nodeInfo.ImageName0Selected, imageIndex);   // SelectImageIndex je ikona ve stavu Nodes.Selected, zobrazená vlevo místo ikony ImageIndex
            treeNode.StateImageIndex = _GetImageIndex(nodeInfo.ImageName1, -1);                    // StateImageIndex je vpravo, a nereaguje na stav Selected

            if (canExpand) treeNode.Expanded = nodeInfo.Expanded;                                  // Expanded se nastavuje pouze z Refreshe (tam má smysl), ale ne při tvorbě (tam ještě nemáme ChildNody)
        }
        /// <summary>
        /// Metoda najde a odebere Child prvky daného Parenta, kde tyto Child prvky jsou označeny jako <see cref="NodePair.IsLazyChild"/> = true.
        /// Metoda vrátí true, pokud některý z odebraných prvků byl Selected.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <returns></returns>
        private bool _RemoveLazyLoadFromParent(string parentNodeId)
        {
            ITreeListNode nodeInfo = _GetNodeInfo(parentNodeId);
            if (nodeInfo == null || !nodeInfo.LazyLoadChilds) return false;

            nodeInfo.LazyLoadChilds = false;

            // Najdu stávající Child nody daného Parenta a všechny je odeberu. Měl by to být pouze jeden node = simulující načítání dat, přidaný v metodě :
            NodePair[] lazyChilds = this._NodesId.Values.Where(p => p.IsLazyChild && p.NodeInfo.ParentFullNodeId == parentNodeId).ToArray();
            bool isAnySelected = (lazyChilds.Length > 0 && lazyChilds.Any(p => p.TreeNode.IsSelected));
            _RemoveAddNodes(lazyChilds.Select(p => p.NodeId), null);

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
        /// <param name="nodeId"></param>
        private void _RemoveNode(string nodeId)
        {
            if (nodeId == null) throw new ArgumentException($"Argument 'nodeKey' is null in {CurrentClassName}.RemoveNode() method.");
            if (this._NodesKey.TryGetValue(nodeId, out var nodePair))          // Nebudu hlásit Exception při smazání neexistujícího nodu, může k tomu dojít při multithreadu...
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
            treeNode.Remove();
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, podle NodeId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private NodePair _GetNodePair(string nodeId)
        {
            if (nodeId != null && this._NodesKey.TryGetValue(nodeId, out var nodePair)) return nodePair;
            return null;
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, podle NodeId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(int id)
        {
            if (id >= 0 && this._NodesId.TryGetValue(id, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrátí data nodu pro daný node, pro jeho <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Tag"/> as int
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(DevExpress.XtraTreeList.Nodes.TreeListNode treeNode)
        {
            int nodeId = ((treeNode != null && treeNode.Tag is int) ? (int)treeNode.Tag : -1);
            if (nodeId >= 0 && this._NodesId.TryGetValue(nodeId, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrací data nodu podle jeho klíče
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private ITreeListNode _GetNodeInfo(string nodeId)
        {
            if (nodeId != null && this._NodesKey.TryGetValue(nodeId, out var nodePair)) return nodePair.NodeInfo;
            return null;
        }
        /// <summary>
        /// Vrátí ID nodu pro daný klíč
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private int _GetNodeId(string nodeId)
        {
            if (nodeId != null && this._NodesKey.TryGetValue(nodeId, out var nodePair)) return nodePair.Id;
            return -1;
        }
        /// <summary>
        /// Vrátí aktuální hodnotu interního ID vizuálního nodu = <see cref="DevExpress.XtraTreeList.Nodes.TreeListNode.Id"/>.
        /// Tato hodnota se mění při odebrání nodu z TreeView. Tuto hodnotu lze tedy použít pouze v okamžiku jejího získání.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private int _GetCurrentTreeNodeId(string nodeId)
        {
            if (nodeId != null && this._NodesKey.TryGetValue(nodeId, out var nodePair)) return nodePair.CurrentTreeNodeId;
            return -1;
        }
        /// <summary>
        /// Vrací index image pro dané jméno obrázku. Používá funkci <see cref="ImageIndexSearcher"/>
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int _GetImageIndex(string imageName, int defaultValue)
        {
            int value = -1;
            if (!String.IsNullOrEmpty(imageName) && ImageIndexSearcher != null) value = ImageIndexSearcher(imageName);
            if (value < 0 && defaultValue >= 0) value = defaultValue;
            return value;
        }
        /// <summary>
        /// FullName aktuální třídy
        /// </summary>
        protected string CurrentClassName { get { return this.GetType().FullName; } }
        #endregion
        #endregion
        #region Public vlastnosti, kolekce nodů, vyhledání nodu podle klíče, vyhledání child nodů
        /// <summary>
        /// Funkce, která pro název ikony vrátí její index v ImageListu
        /// </summary>
        public Func<string, int> ImageIndexSearcher { get; set; }
        /// <summary>
        /// Text (lokalizovaný) pro text uzlu, který reprezentuje "LazyLoadChild", např. něco jako "Načítám data..."
        /// </summary>
        public string LazyLoadNodeText { get; set; }
        /// <summary>
        /// Název ikony uzlu, který reprezentuje "LazyLoadChild", např. něco jako přesýpací hodiny...
        /// </summary>
        public string LazyLoadNodeImageName { get; set; }
        /// <summary>
        /// Po LazyLoad aktivovat první načtený node?
        /// </summary>
        public TreeViewLazyLoadFocusNodeType LazyLoadFocusNode { get; set; }
        /// <summary>
        /// Režim zobrazení Checkboxů. 
        /// Výchozí je <see cref="TreeViewCheckBoxMode.None"/>
        /// </summary>
        public TreeViewCheckBoxMode CheckBoxMode
        {
            get { return _CheckBoxMode; }
            set
            {
                _CheckBoxMode = value;
                this.OptionsView.ShowCheckBoxes = (value == TreeViewCheckBoxMode.AllNodes || value == TreeViewCheckBoxMode.SpecifyByNode);
            }
        }
        private TreeViewCheckBoxMode _CheckBoxMode;
        /// <summary>
        /// Režim kreslení ikon u nodů.
        /// Výchozí je <see cref="TreeViewImageMode.Image0"/>.
        /// Aplikační kód musí dodat objekt do <see cref="ImageList"/>, jinak se ikony zobrazovat nebudou, 
        /// dále musí dodat metodu <see cref="ImageIndexSearcher"/> (která převede jméno ikony z nodu do indexu v <see cref="ImageList"/>)
        /// a musí plnit jména ikon do <see cref="ITreeListNode.ImageName0"/> atd.
        /// </summary>
        public TreeViewImageMode ImageMode
        {
            get { return _ImageMode; }
            set { _SetImageSetting(_ImageList, value); }
        }
        private TreeViewImageMode _ImageMode;
        /// <summary>
        /// Knihovna ikon pro nody.
        /// Výchozí je null.
        /// Aplikační kód musí dodat objekt do <see cref="ImageList"/>, jinak se ikony zobrazovat nebudou, 
        /// dále musí dodat metodu <see cref="ImageIndexSearcher"/> (která převede jméno ikony z nodu do indexu v <see cref="ImageList"/>)
        /// a musí plnit jména ikon do <see cref="ITreeListNode.ImageName0"/> atd.
        /// <para/>
        /// Nepoužívejme přímo SelectImageList ani StateImageList.
        /// </summary>
        public ImageList ImageList
        {
            get { return _ImageList; }
            set { _SetImageSetting(value, _ImageMode); }
        }
        private ImageList _ImageList;
        /// <summary>
        /// Zajistí nastavení režimu pro ikony
        /// </summary>
        /// <param name="imageList"></param>
        /// <param name="imageMode"></param>
        private void _SetImageSetting(ImageList imageList, TreeViewImageMode imageMode)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<ImageList, TreeViewImageMode>(_SetImageSetting), imageList, imageMode); return; }

            _ImageList = imageList;
            _ImageMode = imageMode;
            switch (imageMode)
            {
                case TreeViewImageMode.None:
                    this.SelectImageList = null;
                    this.StateImageList = null;
                    break;
                case TreeViewImageMode.Image0:
                    this.SelectImageList = imageList;
                    this.StateImageList = null;
                    break;
                case TreeViewImageMode.Image1:
                    this.SelectImageList = null;
                    this.StateImageList = imageList;
                    break;
                case TreeViewImageMode.Image01:
                    this.SelectImageList = imageList;
                    this.StateImageList = imageList;
                    break;
            }
        }
        /// <summary>
        /// Akce, která zahájí editaci buňky
        /// </summary>
        public DevExpress.XtraTreeList.TreeListEditorShowMode EditorShowMode
        {
            get { return this.OptionsBehavior.EditorShowMode; }
            set { this.OptionsBehavior.EditorShowMode = value; }
        }
        /// <summary>
        /// Aktuálně vybraný Node
        /// </summary>
        public ITreeListNode FocusedNodeInfo { get { return _GetNodeInfo(this.FocusedNode); } }
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
            return this._NodesStandard.Where(n => n.ParentFullNodeId != null && n.ParentFullNodeId == parentKey).ToArray();
        }
        /// <summary>
        /// Obsahuje kolekci všech nodů, které nejsou IsLazyChild.
        /// Node typu IsLazyChild je dočasně přidaný child node do těch nodů, jejichž Childs se budou načítat po rozbalení.
        /// </summary>
        private IEnumerable<ITreeListNode> _NodesStandard { get { return this._NodesId.Values.Where(p => !p.IsLazyChild).Select(p => p.NodeInfo); } }
        #endregion
        #region Public eventy a jejich volání
        /// <summary>
        /// TreeView aktivoval určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeSelected;
        /// <summary>
        /// Vyvolá event <see cref="NodeSelected"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeSelected(ITreeListNode nodeInfo)
        {
            if (NodeSelected != null) NodeSelected(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeSelected));
        }
        /// <summary>
        /// TreeView má NodeIconClick na určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeIconClick;
        /// <summary>
        /// Vyvolá event <see cref="NodeIconClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeIconClick(ITreeListNode nodeInfo)
        {
            if (NodeIconClick != null) NodeIconClick(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeIconClick));
        }
        /// <summary>
        /// TreeView má Doubleclick na určitý Node
        /// </summary>
        public event DxTreeViewNodeHandler NodeDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="NodeDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeDoubleClick(ITreeListNode nodeInfo)
        {
            if (NodeDoubleClick != null) NodeDoubleClick(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeDoubleClick));
        }
        /// <summary>
        /// TreeView právě rozbaluje určitý Node (je jedno, zda má nebo nemá <see cref="ITreeListNode.LazyLoadChilds"/>).
        /// </summary>
        public event DxTreeViewNodeHandler NodeExpanded;
        /// <summary>
        /// Vyvolá event <see cref="NodeExpanded"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeExpanded(ITreeListNode nodeInfo)
        {
            if (NodeExpanded != null) NodeExpanded(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeExpanded));
        }
        /// <summary>
        /// TreeView právě sbaluje určitý Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeCollapsed;
        /// <summary>
        /// Vyvolá event <see cref="NodeCollapsed"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeCollapsed(ITreeListNode nodeInfo)
        {
            if (NodeCollapsed != null) NodeCollapsed(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeCollapsed));
        }
        /// <summary>
        /// TreeView právě začíná editovat text daného node = je aktivován editor.
        /// </summary>
        public event DxTreeViewNodeHandler ActivatedEditor;
        /// <summary>
        /// Vyvolá event <see cref="ActivatedEditor"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnActivatedEditor(ITreeListNode nodeInfo)
        {
            if (ActivatedEditor != null) ActivatedEditor(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.ActivatedEditor));
        }
        /// <summary>
        /// Uživatel dal DoubleClick v políčku kde právě edituje text. Text je součástí argumentu.
        /// </summary>
        public event DxTreeViewNodeHandler EditorDoubleClick;
        /// <summary>
        /// Vyvolá event <see cref="EditorDoubleClick"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        protected virtual void OnEditorDoubleClick(ITreeListNode nodeInfo, object editedValue)
        {
            if (EditorDoubleClick != null) EditorDoubleClick(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.EditorDoubleClick, editedValue));
        }
        /// <summary>
        /// TreeView právě skončil editaci určitého Node.
        /// </summary>
        public event DxTreeViewNodeHandler NodeEdited;
        /// <summary>
        /// Vyvolá event <see cref="NodeEdited"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="editedValue"></param>
        protected virtual void OnNodeEdited(ITreeListNode nodeInfo, object editedValue)
        {
            if (NodeEdited != null) NodeEdited(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeEdited, editedValue));
        }
        /// <summary>
        /// Uživatel změnil stav Checked na prvku.
        /// </summary>
        public event DxTreeViewNodeHandler NodeCheckedChange;
        /// <summary>
        /// Vyvolá event <see cref="NodeCheckedChange"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="isChecked"></param>
        protected virtual void OnNodeCheckedChange(ITreeListNode nodeInfo, bool isChecked)
        {
            if (NodeCheckedChange != null) NodeCheckedChange(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeCheckedChange, isChecked));
        }
        /// <summary>
        /// Uživatel dal Delete na uzlu, který se needituje.
        /// </summary>
        public event DxTreeViewNodeHandler NodeDelete;
        /// <summary>
        /// Vyvolá event <see cref="NodeDelete"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnNodeDelete(ITreeListNode nodeInfo)
        {
            if (NodeDelete != null) NodeDelete(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.NodeDelete));
        }
        /// <summary>
        /// TreeView rozbaluje node, který má nastaveno načítání ze serveru : <see cref="ITreeListNode.LazyLoadChilds"/> je true.
        /// </summary>
        public event DxTreeViewNodeHandler LazyLoadChilds;
        /// <summary>
        /// Vyvolá event <see cref="LazyLoadChilds"/>
        /// </summary>
        /// <param name="nodeInfo"></param>
        protected virtual void OnLazyLoadChilds(ITreeListNode nodeInfo)
        {
            if (LazyLoadChilds != null) LazyLoadChilds(this, new DxTreeViewNodeArgs(nodeInfo, TreeViewActionType.LazyLoadChilds));
        }
        #endregion
    }
    #region Deklarace delegátů a tříd pro eventhandlery, další enumy
    /// <summary>
    /// Předpis pro eventhandlery
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxTreeViewNodeHandler(object sender, DxTreeViewNodeArgs args);
    /// <summary>
    /// Argument pro eventhandlery
    /// </summary>
    public class DxTreeViewNodeArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        /// <param name="editedValue"></param>
        public DxTreeViewNodeArgs(ITreeListNode node, TreeViewActionType action, object editedValue = null)
        {
            this.Node = node;
            this.Action = action;
            this.EditedValue = editedValue;
        }
        /// <summary>
        /// Data o aktuálním nodu
        /// </summary>
        public ITreeListNode Node { get; private set; }
        /// <summary>
        /// Druh akce
        /// </summary>
        public TreeViewActionType Action { get; private set; }
        /// <summary>
        /// Editovaná hodnota, je vyplněna pouze pro akce <see cref="TreeViewActionType.NodeEdited"/> a <see cref="TreeViewActionType.EditorDoubleClick"/>
        /// </summary>
        public object EditedValue { get; private set; }
    }
    /// <summary>
    /// Akce která proběhla v TreeList
    /// </summary>
    public enum TreeViewActionType
    {
        /// <summary>None</summary>
        None,
        /// <summary>NodeSelected</summary>
        NodeSelected,
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
        LazyLoadChilds
    }
    /// <summary>
    /// Který node se bude focusovat po LazyLoad child nodů?
    /// </summary>
    public enum TreeViewLazyLoadFocusNodeType
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
    /// Jaké ikony bude TreeView zobrazovat - a rezervovat pro ně prostor
    /// </summary>
    public enum TreeViewImageMode
    {
        /// <summary>
        /// Žádná ikona
        /// </summary>
        None,
        /// <summary>
        /// Pouze ikona 0, přebírá se z <see cref="ITreeListNode.ImageName0"/> a <see cref="ITreeListNode.ImageName0Selected"/>
        /// </summary>
        Image0,
        /// <summary>
        /// Pouze ikona 1, přebírá se z <see cref="ITreeListNode.ImageName1"/>
        /// </summary>
        Image1,
        /// <summary>
        /// Obě ikony 0 a 1
        /// </summary>
        Image01
    }
    /// <summary>
    /// Režim zobrazení CheckBoxů u nodu
    /// </summary>
    public enum TreeViewCheckBoxMode
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
    #endregion
    #endregion
    #region class TreeListNode a interface ITreeListNode : Data o jednom Node
    /// <summary>
    /// Data o jednom Node
    /// </summary>
    public class TreeListNode : ITreeListNode
    {
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
        /// <param name="lazyLoadChilds"></param>
        /// <param name="imageName"></param>
        /// <param name="imageNameSelected"></param>
        /// <param name="imageNameStatic"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="fontSizeDelta"></param>
        /// <param name="fontStyleDelta"></param>
        /// <param name="backColor"></param>
        /// <param name="foreColor"></param>
        public TreeListNode(string nodeId, string parentNodeId, string text,
            NodeItemType nodeType = NodeItemType.DefaultText, bool canEdit = false, bool canDelete = false, bool expanded = false, bool lazyLoadChilds = false,
            string imageName = null, string imageNameSelected = null, string imageNameStatic = null, string toolTipTitle = null, string toolTipText = null,
            int? fontSizeDelta = null, FontStyle? fontStyleDelta = null, Color? backColor = null, Color? foreColor = null)
        {
            _Id = -1;
            this.FullNodeId = nodeId;
            this.ParentFullNodeId = parentNodeId;
            this.Text = text;
            this.NodeType = nodeType;
            this.CanEdit = canEdit;
            this.CanDelete = canDelete;
            this.Expanded = expanded;
            this.LazyLoadChilds = lazyLoadChilds;
            this.ImageName0 = imageName;
            this.ImageName0Selected = imageNameSelected;
            this.ImageName1 = imageNameStatic;
            this.ToolTipTitle = toolTipTitle;
            this.ToolTipText = toolTipText;
            this.FontSizeDelta = fontSizeDelta;
            this.FontStyleDelta = fontStyleDelta;
            this.BackColor = backColor;
            this.ForeColor = foreColor;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Typ nodu
        /// </summary>
        public virtual NodeItemType NodeType { get; private set; }
        /// <summary>
        /// ID nodu v TreeView, pokud není v TreeView pak je -1 . Toto ID je přiděleno v rámci <see cref="DxTreeViewListNative"/> a po dobu přítomnosti nodu v TreeView se nemění.
        /// Pokud node bude odstraněn z Treeiew, pak hodnota <see cref="Id"/> bude -1, stejně tak jako v době, než bude Node do TreeView přidán.
        /// </summary>
        public virtual int Id { get { return _Id; } }
        /// <summary>
        /// String klíč nodu, musí být unique přes všechny Nodes!
        /// Po vytvoření nelze změnit.
        /// </summary>
        public virtual string FullNodeId { get; private set; }
        /// <summary>
        /// Klíč parent uzlu.
        /// Po vytvoření nelze změnit.
        /// </summary>
        public virtual string ParentFullNodeId { get; private set; }
        /// <summary>
        /// Text uzlu.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string Text { get; set; }
        /// <summary>
        /// Node zobrazuje zaškrtávátko.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool CanCheck { get; set; }
        /// <summary>
        /// Node má zobrazovat prostor pro zaškrtávátko, i když node sám zaškrtávátko nezobrazuje.
        /// Je to proto, aby nody nacházející se v jedné řadě pod sebou byly "svisle zarovnané" i když některé zaškrtávátko mají, a jiné nemají.
        /// </summary>
        public virtual bool AddVoidCheckSpace { get; set; }
        /// <summary>
        /// Node je zaškrtnutý.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool IsChecked { get; set; }
        /// <summary>
        /// Ikona základní, ta může reagovat na stav Selected (pak bude zobrazena ikona <see cref="ImageName0Selected"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string ImageName0 { get; set; }
        /// <summary>
        /// Ikona ve stavu Node.IsSelected, zobrazuje se místo ikony <see cref="ImageName0"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string ImageName0Selected { get; set; }
        /// <summary>
        /// Ikona statická, ta nereaguje na stav Selected, zobrazuje se vpravo od ikony <see cref="ImageName0"/>.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual string ImageName1 { get; set; }
        /// <summary>
        /// Uživatel může editovat text tohoto node, po ukončení editace je vyvolána událost <see cref="DxTreeViewListNative.NodeEdited"/>.
        /// Změnu této hodnoty není nutno refreshovat, načítá se po výběru konkrétního Node v TreeView a aplikuje se na něj.
        /// </summary>
        public virtual bool CanEdit { get; set; }
        /// <summary>
        /// Uživatel může stisknout Delete nad uzlem, bude vyvolána událost <see cref="DxTreeViewListNative.NodeDelete"/>
        /// </summary>
        public virtual bool CanDelete { get; set; }
        /// <summary>
        /// Node je otevřený.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        public virtual bool Expanded { get; set; }
        /// <summary>
        /// Node bude mít Child prvky, ale zatím nejsou dodány. Node bude zobrazovat rozbalovací ikonu a jeden node s textem "Načítám data...", viz <see cref="DxTreeViewListNative.LazyLoadNodeText"/>.
        /// Ikonu nastavíme v <see cref="DxTreeViewListNative.LazyLoadNodeImageName"/>. Otevřením tohoto nodu se vyvolá event <see cref="DxTreeViewListNative.LazyLoadChilds"/>.
        /// Třída <see cref="DxTreeViewListNative"/> si sama obhospodařuje tento "LazyLoadChildNode": vytváří jej a následně jej i odebírá.
        /// Aktivace tohoto nodu není hlášena jako event, node nelze editovat ani smazat uživatelem.
        /// </summary>
        public virtual bool LazyLoadChilds { get; set; }
        /// <summary>
        /// Titulek tooltipu. Pokud bude null, pak se převezme <see cref="Text"/>, což je optimální z hlediska orientace uživatele.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování ToolTipu.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Text tooltipu.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování ToolTipu.
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Relativní velikost písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        public virtual int? FontSizeDelta { get; set; }
        /// <summary>
        /// Změna stylu písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        public virtual FontStyle? FontStyleDelta { get; set; }
        /// <summary>
        /// Explicitní barva pozadí prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Explicitní barva písma prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        public virtual Color? ForeColor { get; set; }
        /// <summary>
        /// Pokud je node již umístěn v TreeView, pak tato metoda zajistí jeho refresh = promítne vizuální hodnoty do controlu
        /// </summary>
        public void Refresh()
        {
            var owner = Owner;
            if (owner != null)
                owner.RefreshNode(this);
        }
        #region Implementace ITreeViewItemId
        DxTreeViewListNative ITreeListNode.Owner
        {
            get { return Owner; }
            set { _Owner = (value != null ? new WeakReference<DxTreeViewListNative>(value) : null); }
        }
        int ITreeListNode.Id { get { return _Id; } set { _Id = value; } }
        /// <summary>
        /// Owner = TreeView, ve kterém je this prvek zobrazen. Může být null.
        /// </summary>
        protected DxTreeViewListNative Owner { get { if (_Owner != null && _Owner.TryGetTarget(out var owner)) return owner; return null; } }
        WeakReference<DxTreeViewListNative> _Owner;
        int _Id;
        #endregion
    }
    /// <summary>
    /// Data o jednom Node
    /// </summary>
    public interface ITreeListNode
    {
        /// <summary>
        /// Aktuální vlastník nodu
        /// </summary>
        DxTreeViewListNative Owner { get; set; }
        /// <summary>
        /// ID nodu v TreeView, pokud není v TreeView pak je -1 . Toto ID je přiděleno v rámci <see cref="DxTreeViewListNative"/> a po dobu přítomnosti nodu v TreeView se nemění.
        /// Pokud node bude odstraněn z Treeiew, pak hodnota <see cref="Id"/> bude -1, stejně tak jako v době, než bude Node do TreeView přidán.
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// Typ nodu
        /// </summary>
        NodeItemType NodeType { get; }
        /// <summary>
        /// String klíč nodu, musí být unique přes všechny Nodes!
        /// Po vytvoření nelze změnit.
        /// </summary>
        string FullNodeId { get; }
        /// <summary>
        /// Klíč parent uzlu.
        /// Po vytvoření nelze změnit.
        /// </summary>
        string ParentFullNodeId { get; }
        /// <summary>
        /// Text uzlu.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Node zobrazuje zaškrtávátko.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool CanCheck { get; }
        /// <summary>
        /// Node má zobrazovat prostor pro zaškrtávátko, i když node sám zaškrtávátko nezobrazuje.
        /// Je to proto, aby nody nacházející se v jedné řadě pod sebou byly "svisle zarovnané" i když některé zaškrtávátko mají, a jiné nemají.
        /// </summary>
        bool AddVoidCheckSpace { get; }
        /// <summary>
        /// Node je zaškrtnutý.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool IsChecked { get; set; }
        /// <summary>
        /// Ikona základní, ta může reagovat na stav Selected (pak bude zobrazena ikona <see cref="ImageName0Selected"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string ImageName0 { get; }
        /// <summary>
        /// Ikona ve stavu Node.IsSelected, zobrazuje se místo ikony <see cref="ImageName0"/>), zobrazuje se vlevo.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string ImageName0Selected { get; }
        /// <summary>
        /// Ikona statická, ta nereaguje na stav Selected, zobrazuje se vpravo od ikony <see cref="ImageName0"/>.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        string ImageName1 { get; }
        /// <summary>
        /// Uživatel může editovat text tohoto node, po ukončení editace je vyvolána událost <see cref="DxTreeViewListNative.NodeEdited"/>.
        /// Změnu této hodnoty není nutno refreshovat, načítá se po výběru konkrétního Node v TreeView a aplikuje se na něj.
        /// </summary>
        bool CanEdit { get; }
        /// <summary>
        /// Uživatel může stisknout Delete nad uzlem, bude vyvolána událost <see cref="DxTreeViewListNative.NodeDelete"/>
        /// </summary>
        bool CanDelete { get; }
        /// <summary>
        /// Node je otevřený.
        /// Pokud je změněn po vytvoření, je třeba provést <see cref="Refresh"/> tohoto uzlu.
        /// Pokud je změněno více uzlů, je vhodnější provést hromadný refresh: <see cref="DxTreeViewListNative.RefreshNodes(IEnumerable{ITreeListNode})"/>.
        /// </summary>
        bool Expanded { get; set; }
        /// <summary>
        /// Node bude mít Child prvky, ale zatím nejsou dodány. Node bude zobrazovat rozbalovací ikonu a jeden node s textem "Načítám data...", viz <see cref="DxTreeViewListNative.LazyLoadNodeText"/>.
        /// Ikonu nastavíme v <see cref="DxTreeViewListNative.LazyLoadNodeImageName"/>. Otevřením tohoto nodu se vyvolá event <see cref="DxTreeViewListNative.LazyLoadChilds"/>.
        /// Třída <see cref="DxTreeViewListNative"/> si sama obhospodařuje tento "LazyLoadChildNode": vytváří jej a následně jej i odebírá.
        /// Aktivace tohoto nodu není hlášena jako event, node nelze editovat ani smazat uživatelem.
        /// </summary>
        bool LazyLoadChilds { get; set; }
        /// <summary>
        /// Titulek tooltipu. Pokud bude null, pak se převezme <see cref="Text"/>, což je optimální z hlediska orientace uživatele.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování ToolTipu.
        /// </summary>
        string ToolTipTitle { get; }
        /// <summary>
        /// Text tooltipu.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování ToolTipu.
        /// </summary>
        string ToolTipText { get; }
        /// <summary>
        /// Relativní velikost písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        int? FontSizeDelta { get; }
        /// <summary>
        /// Změna stylu písma.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        FontStyle? FontStyleDelta { get; }
        /// <summary>
        /// Explicitní barva pozadí prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Explicitní barva písma prvku.
        /// Změnu této hodnoty není nutno refreshovat, načítá se odtud v okamžiku zobrazování každého Node.
        /// Je možno vynutit Refresh vizuální vrstvy TreeView metodou <see cref="DxTreeViewListNative"/>.Refresh();
        /// </summary>
        Color? ForeColor { get; }
        /// <summary>
        /// Pokud je node již umístěn v TreeView, pak tato metoda zajistí jeho refresh = promítne vizuální hodnoty do controlu
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
    #endregion
    #region class DxFilterBox
    /// <summary>
    /// Panel obsahující malý filtr, používá se např. v <see cref="DxTreeViewList"/>
    /// </summary>
    public class DxFilterBox : DxPanelControl
    {
        #region Konstrukce a inicializace
        /// <summary>
        /// Konstruktor.
        /// Panel obsahující malý filtr, používá se např. v <see cref="DxTreeViewList"/>
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
            _OperatorButtonImageDefault = "svgimages/dashboards/horizontallines.svg";
            _OperatorButtonImage = null;
            _ClearButtonImage = "svgimages/dashboards/clearfilter.svg";
            _ClearButtonToolTipTitle = "Smazat";
            _ClearButtonToolTipText = "Zruší zadaný filtr";

            _OperatorButton = DxComponent.CreateDxMiniButton(0, 0, 24, 24, this, OperatorButton_Click, tabStop: false);
            _FilterText = DxComponent.CreateDxTextEdit(24, 0, 200, this, tabStop: true);
            _FilterText.KeyDown += FilterText_KeyDown;
            _FilterText.KeyUp += FilterText_KeyUp;

            _ClearButton = DxComponent.CreateDxMiniButton(224, 0, 24, 24, this, ClearButton_Click, tabStop: false);

            _FilterText.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;

            _FilterOperators = CreateDefaultFilterItems(FilterBoxOperatorItems.DefaultNumber);
            ActivateFirstCheckedOperator(false);
            _CurrentText = "";
            FilterValueChangedSources = DxFilterRowChangeEventSource.Default;
            LastFilterValue = null;
        }
        private string _OperatorButtonImageDefault;
        private string _OperatorButtonImage;
        private string _MenuButtonToolTipTitle;
        private string _MenuButtonToolTipText;
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

                // Výška textu, výška vnitřní, vnější (reagujeme i na Zoom a Skin):
                int margins = Margins;
                int margins2 = 2 * margins;
                int minHeight = 24 + margins2;
                var clientSize = this.ClientSize;
                int currentHeight = this.Size.Height;
                int border = currentHeight - clientSize.Height;
                int textHeight = _FilterText.Height;
                int innerHeight = (textHeight < minHeight ? minHeight : textHeight);
                int outerHeight = innerHeight + border;
                if (currentHeight != outerHeight) this.Height = outerHeight;            // Tady se vyvolá událost OnClientSizeChanged() a z ní rekurzivně zdejší metoda, ale ignoruje se protože (_InDoLayoutProcess = true;)

                // Souřadnice buttonů a textu:
                int buttonSize = innerHeight - margins2;
                int spaceX = 1;
                int y = margins;
                int x = margins;
                int textWidth = clientSize.Width - 2 * (margins + buttonSize + spaceX);
                int textY = (innerHeight - textHeight) / 2;
                _OperatorButton.Bounds = new Rectangle(x, y, buttonSize, buttonSize); x += (buttonSize + spaceX);
                _FilterText.Bounds = new Rectangle(x, textY, textWidth, textHeight); x += (textWidth + spaceX);
                _ClearButton.Bounds = new Rectangle(x, y, buttonSize, buttonSize); x += (buttonSize + spaceX);

                MenuButtonRefresh();
                ClearButtonRefresh();
            }
            finally
            {
                _InDoLayoutProcess = false;
            }
        }
        /// <summary>
        /// Refreshuje button Menu (Image a ToolTip)
        /// </summary>
        protected void MenuButtonRefresh() { ButtonRefresh(_OperatorButton, (_OperatorButtonImage ?? _OperatorButtonImageDefault), _MenuButtonToolTipTitle, _MenuButtonToolTipText); }
        /// <summary>
        /// Refreshuje button Clear (Image a ToolTip)
        /// </summary>
        protected void ClearButtonRefresh() { ButtonRefresh(_ClearButton, _ClearButtonImage, _ClearButtonToolTipTitle, _ClearButtonToolTipText); }
        /// <summary>
        /// Pro daný button refreshuje jeho Image a ToolTip
        /// </summary>
        /// <param name="button"></param>
        /// <param name="imageName"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        private void ButtonRefresh(DxSimpleButton button, string imageName, string toolTipTitle, string toolTipText)
        {
            if (button == null) return;
            int buttonSize = button.Width;
            Size imageSize = new Size(buttonSize - 4, buttonSize - 4);
            DxComponent.ApplyImage(button.ImageOptions, imageName, null, imageSize, true);
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
        public int Margins { get { return _Margins; } set { int m = value; _Margins = (m < 0 ? 0 : (m > 10 ? 10 : value)); this.RunInGui(DoLayout); } } private int _Margins;
        /// <summary>
        /// Položky v nabídce typů filtru. Lze setovat, lze modifikovat. Pokud bude null nebo prázdné, pak tlačítko typu filtru nic nenabídne.
        /// </summary>
        public List<IMenuItem> FilterOperators { get { return _FilterOperators; } set { _FilterOperators = value; ActivateFirstCheckedOperator(false); ReloadLastFilter(); } } private List<IMenuItem> _FilterOperators;
        /// <summary>
        /// Vytvoří a vrátí defaultní položky menu
        /// </summary>
        /// <returns></returns>
        public static List<IMenuItem> CreateDefaultFilterItems(FilterBoxOperatorItems items)
        {
            string resourceC1 = "images/alignment/alignverticalcenter2_16x16.png";
            string resourceL1 = "images/alignment/alignverticalleft2_16x16.png";
            string resourceR1 = "images/alignment/alignverticalright2_16x16.png";
            string resourceC2 = "office2013/alignment/alignverticalcenter2_16x16.png";
            string resourceL2 = "office2013/alignment/alignverticalleft2_16x16.png";
            string resourceR2 = "office2013/alignment/alignverticalright2_16x16.png";
            List<IMenuItem> menuItems = new List<IMenuItem>();

            if (items.HasFlag(FilterBoxOperatorItems.Contains))
                menuItems.Add(new DataMenuItem() { ItemId = "Contains", ItemImage = resourceC1, ItemText = "Obsahuje", ItemIsChecked = false, ToolTipTitle = "Obsahuje:", ToolTip = "Vybere ty položky, které obsahují zadaný text", Tag = FilterBoxOperatorItems.Contains });
            if (items.HasFlag(FilterBoxOperatorItems.DoesNotContain))
                menuItems.Add(new DataMenuItem() { ItemId = "DoesNotContain", ItemImage = resourceC2, ItemText = "Neobsahuje", ItemIsChecked = false, ToolTipTitle = "Neobsahuje:", ToolTip = "Vybere ty položky, které neobsahují zadaný text", Tag = FilterBoxOperatorItems.DoesNotContain });
            if (items.HasFlag(FilterBoxOperatorItems.StartsWith))
                menuItems.Add(new DataMenuItem() { ItemId = "StartsWith", ItemImage = resourceL1, ItemText = "Začíná", ItemIsChecked = true, ToolTipTitle = "Začíná:", ToolTip = "Vybere ty položky, jejichž text začíná zadaným textem", Tag = FilterBoxOperatorItems.StartsWith });
            if (items.HasFlag(FilterBoxOperatorItems.DoesNotStartWith))
                menuItems.Add(new DataMenuItem() { ItemId = "DoesNotStartWith", ItemImage = resourceL2, ItemText = "Nezačíná", ItemIsChecked = false, ToolTipTitle = "Nezačíná:", ToolTip = "Vybere ty položky, jejichž text začíná jinak, než je zadáno", Tag = FilterBoxOperatorItems.DoesNotStartWith });
            if (items.HasFlag(FilterBoxOperatorItems.EndsWith))
                menuItems.Add(new DataMenuItem() { ItemId = "EndsWith", ItemImage = resourceR1, ItemText = "Končí", ItemIsChecked = false, ToolTipTitle = "Končí:", ToolTip = "Vybere ty položky, jejichž text končí zadaným textem", Tag = FilterBoxOperatorItems.EndsWith });
            if (items.HasFlag(FilterBoxOperatorItems.DoesNotEndWith))
                menuItems.Add(new DataMenuItem() { ItemId = "DoesNotEndWith", ItemImage = resourceR2, ItemText = "Nekončí", ItemIsChecked = false, ToolTipTitle = "Nekončí:", ToolTip = "Vybere ty položky, jejichž text končí jinak, než je zadáno", Tag = FilterBoxOperatorItems.DoesNotEndWith });

            return menuItems;
        }
        /// <summary>
        /// Aktuální hodnota filtru. Lze setovat. Setování ale nevyvolá událost <see cref="FilterValueChanged"/>.
        /// </summary>
        public DxFilterBoxValue FilterValue 
        {
            get { return this.CurrentFilterValue; }
            set
            {
                string text = value?.FilterText ?? "";
                this._CurrentFilterOperator = value?.FilterOperator;
                this._CurrentText = text;
                this._CurrentValue = value?.FilterValue;
                this.RunInGui(() => this._FilterText.Text = text);
            }
        }
        /// <summary>
        /// Za jakých událostí se volá event <see cref="FilterValueChanged"/>
        /// </summary>
        public DxFilterRowChangeEventSource FilterValueChangedSources { get; set; }
        /// <summary>
        /// Událost volaná po změně obsahu filtru, po potvrzení textu klávesou Enter (volitelně i po změně typu operátoru)
        /// </summary>
        public event EventHandler<TEventArgs<DxFilterBoxValue>> FilterValueChanged;
        /// <summary>
        /// Událost volaná po stisku klávesy Enter, vždy, tedy jak po změně textu i bez změny textu.
        /// Pokud dojde ke změně textu, pak je pořadí: <see cref="FilterValueChanged"/>, <see cref="KeyEnterPress"/>.
        /// Pokud uživatel stiskne tlačítko Clear (vpravo), provede se pouze <see cref="FilterValueChanged"/>, následně <see cref="CurrentFilterCleared"/>, ale focus se vrátí do TextBoxu a neprovádí se <see cref="KeyEnterPress"/>.
        /// Pokud uživatel vybere jiný druh filtru, proběhne jen událost <see cref="CurrentTypeChanged"/>.
        /// </summary>
        public event EventHandler KeyEnterPress;
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
        /// V poli <see cref="FilterOperators"/> najde první položku, která je ItemIsChecked (anebo první obecně) a tu prohlásí za vybranou.
        /// </summary>
        private void ActivateFirstCheckedOperator(bool runEvent)
        {
            IMenuItem activeOperator = null;
            var filterItems = this.FilterOperators;
            if (filterItems != null && filterItems.Count > 0)
            {
                activeOperator = filterItems.FirstOrDefault(f => (f.ItemIsChecked ?? false));
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
            bool isChangeOperator = !String.Equals(_CurrentFilterOperator?.ItemId, activeOperator?.ItemId);
            _CurrentFilterOperator = activeOperator;
            ApplyCurrentOperator();
            if (isChangeOperator && runEvent && CallChangedEventOn(DxFilterRowChangeEventSource.OperatorChange))
                RunFilterValueChanged();
        }
        /// <summary>
        /// Aplikuje ikonu a tooltip z aktuální položky <see cref="_CurrentFilterOperator"/> do buttonu Menu
        /// </summary>
        protected virtual void ApplyCurrentOperator()
        {
            var currentType = _CurrentFilterOperator;

            // Označíme si odpovídající položku (podle ItemId) v nabídce jako Checked:
            string currentFilterId = currentType?.ItemId;
            var filterTypes = _FilterOperators;
            if (filterTypes != null)
                filterTypes.ForEachExec(i => i.ItemIsChecked = String.Equals(i.ItemId, currentFilterId));

            // Z aktuálního filtru přečteme jeho data a promítneme je do tlačítka:
            _OperatorButtonImage = currentType?.ItemImage;
            _MenuButtonToolTipTitle = currentType?.ToolTipTitle;
            _MenuButtonToolTipText = currentType?.ToolTip;
            MenuButtonRefresh();
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
        }
        
        /// <summary>
        /// Po stisku klávesy v TextBoxu reagujeme na Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterText_KeyUp(object sender, KeyEventArgs e)
        {   // KeyUp neřeší Enter, ale řeší změny textu
            _CurrentText = (_FilterText.Text ?? "");       // Stínování hodnoty

            bool isChange = this.CurrentFilterIsChanged;
            if (isChange && this.CallChangedEventOn(DxFilterRowChangeEventSource.TextChange))
            {
                RunFilterValueChanged();
                isChange = false;                          // Po vyvolání události už není žádná změna
            }

            if (e.KeyData == Keys.Enter)
            {   // Pouze samotný Enter, nikoli CtrlEnter nebo ShiftEnter:
                if (isChange && this.CallChangedEventOn(DxFilterRowChangeEventSource.KeyEnter))
                {
                    RunFilterValueChanged();
                    isChange = false;                      // Po vyvolání události už není žádná změna
                }
                RunKeyEnterPress();
                e.Handled = true;
            }
        }
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
            _FilterText.Text = "";
            _CurrentText = "";                             // Stínování hodnoty

            bool isChange = this.CurrentFilterIsChanged;
            if (isChange && this.CallChangedEventOn(DxFilterRowChangeEventSource.ClearButton))
            {
                RunFilterValueChanged();
                isChange = false;                          // Po vyvolání události už není žádná změna
            }

            _FilterText.Focus();
        }
        /// <summary>
        /// Proběhne po změně hodnoty filtru
        /// </summary>
        private void RunFilterValueChanged()
        {
            var currentFilter = this.CurrentFilterValue;
            TEventArgs<DxFilterBoxValue> args = new TEventArgs<DxFilterBoxValue>(currentFilter);
            this.LastFilterValue = currentFilter;          // Od teď bude hodnota CurrentFilterIsChanged = false;
            OnFilterValueChanged(args);
            FilterValueChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Po změně hodnoty filtru, dle nastavených zdrojů události
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnFilterValueChanged(TEventArgs<DxFilterBoxValue> args) { }
        /// <summary>
        /// Hodnotu z proměnné <see cref="_CurrentText"/> vepíše do vizuálního textboxu, nevolá žádné eventy
        /// </summary>
        protected virtual void DoApplyCurrentText()
        {
            if (_FilterText == null) return;
            _FilterText.Text = _CurrentText;
        }
        /// <summary>
        /// Vrátí true, pokud v <see cref="FilterValueChangedSources"/> je nastavený některý bit z dodané hodnoty.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected bool CallChangedEventOn(DxFilterRowChangeEventSource source)
        {
            return this.FilterValueChangedSources.HasFlag(source);
        }
        /// <summary>
        /// Aktuální stav filtru: obsahuje typ filtru (<see cref="CurrentFilterOperator"/>:ItemId) a aktuálně zadaný text (<see cref="_FilterText"/>.Text), oddělené CrLf.
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
        /// Aktualizuje hodnotu <see cref="LastFilterValue"/> z hodnoty <see cref="CurrentFilterValue"/>
        /// </summary>
        protected void ReloadLastFilter() { LastFilterValue = CurrentFilterValue; }
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
    /// <summary>
    /// Spouštěcí události pro event <see cref="DxFilterBox.FilterValueChanged"/>
    /// </summary>
    [Flags]
    public enum DxFilterRowChangeEventSource
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
    /// Povolené operátory v controlu FilterBox. Použivá se pro určení povolených operátorů v controlu FilterBox.
    /// </summary>
    [Flags]
    public enum FilterBoxOperatorItems : uint
    {
        Contains = 0,
        DoesNotContain = 1,
        DoesNotEndWith = 2,
        DoesNotMatch = 4,
        DoesNotStartWith = 8,
        EndsWith = 16,
        Equals = 32,
        GreaterThan = 64,
        GreaterThanOrEqualTo = 128,
        LessThan = 256,
        LessThanOrEqualTo = 512,
        Like = 1024,
        Match = 2048,
        NotEquals = 4096,
        NotLike = 8192,
        StartsWith = 16384,

        DefaultText = Contains | DoesNotContain | StartsWith | DoesNotStartWith | EndsWith | DoesNotEndWith,
        DefaultNumber = Equals | NotEquals | GreaterThan | GreaterThanOrEqualTo | LessThan | LessThanOrEqualTo
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
