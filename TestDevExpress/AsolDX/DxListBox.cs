// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.PivotGrid.OLAP;
using DevExpress.Utils;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Drawing;
using DevExpress.XtraEditors.TableLayout;
using DevExpress.XtraEditors.ViewInfo;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region class DxDblListBoxPanel : Panel obsahující dva panely s nativní obsluhou přenášení položek listů
    /// <summary>
    /// <see cref="DxDblListBoxPanel"/> : Panel obsahující dva panely s nativní obsluhou přenášení položek listů ze Source do Target
    /// </summary>
    public class DxDblListBoxPanel : DxSplitContainerControl
    {
        #region Konstruktor, tvorba, privátní proměnné, layout celkový
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDblListBoxPanel()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace komponent a hodnot
        /// </summary>
        private void Initialize()
        {
            this.SplitterOrientation = Orientation.Horizontal;
            this.FixedPanel = SplitFixedPanel.Panel1;
            this.Width = 600;
            this.SplitterPosition = 300;

            var listPanelSource = new DxListBoxPanel();
            listPanelSource.DxProperties.ItemMouseDoubleClick += _SourceList_MouseDoubleClick;
            listPanelSource.Dock = DockStyle.Fill;
            this.Panel1.Controls.Add(listPanelSource);
            this.Panel1.MinSize = 120;
            __SourceListPanel = listPanelSource;

            var listPanelTarget = new DxListBoxPanel();
            listPanelTarget.Dock = DockStyle.Fill;
            listPanelTarget.DxProperties.ItemMouseDoubleClick += _TargetList_MouseDoubleClick;
            this.Panel2.Controls.Add(listPanelTarget);
            this.Panel2.MinSize = 120;
            __TargetListPanel = listPanelTarget;

            _InitButtons();                                // Buttony přidávám až po ListBoxu do Panel1, protože jsou Dock = Right a ListBox je Dock = Fill. A takhle to funguje správně.

            _InitProperties();
        }
        /// <summary>
        /// Změna Zoomu může mít vliv na velikost buttonů
        /// </summary>
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            this._DoLayoutButtons();
        }
        /// <summary>
        /// Změna Skinu může mít vliv na velikost mezery vedle buttonů
        /// </summary>
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            this._DoLayoutButtons();
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
                __SourceListPanel?.Dispose();
                __TargetListPanel?.Dispose();
            }
            catch { /* Chyby v Dispose občas nastanou v DevExpress, který něco likviduje v GC threadu a nemá přístup do GUI. */ }
        }
        /// <summary>
        /// Instance ListBoxPanelu pro levý = Source
        /// </summary>
        private DxListBoxPanel __SourceListPanel;
        /// <summary>
        /// Instance ListBoxPanelu pro levý = Source
        /// </summary>
        private DxListBoxPanel __TargetListPanel;
        #endregion
        #region Definice vzhledu a chování - základní
        /// <summary>
        /// Inicializace vlastností
        /// </summary>
        private void _InitProperties()
        {
            // Základní vlastnosti:
            DxSourceProperties.DuplicityEnabled = true;
            DxSourceProperties.SelectionMode = SelectionMode.MultiExtended;
            DxSourceProperties.RowFilterMode = RowFilterBoxMode.None;

            DxTargetProperties.DuplicityEnabled = true;
            DxTargetProperties.SelectionMode = SelectionMode.MultiExtended;
            DxTargetProperties.RowFilterMode = RowFilterBoxMode.None;

            // DoubleListBox vlastnosti:
            __DblListMode = DblListModeType.Mode_FixedSourceToFreeTarget;
            __ButtonsPosition = DblButtonsPositionType.BottomAndCenter;
            __CopyItemTargetPosition = DblItemTargetPositionType.AtListEnd;
            __MoveAllEnabled = false;
            __DragAndDropEnabled = true;
            __ClipboardActionsEnabled = false;
            __DoubleClickEnabled = true;
            _AcceptButtonsPosition();
            _AcceptListStyles();

            // Eventy:
            DxSourceProperties.SelectedItemsChanged += _SourceSelectedItemsChanged;
            DxTargetProperties.SelectedItemsChanged += _TargetSelectedItemsChanged;
            DxSourceProperties.ListActionAfter += _ListActionAfter;
            DxTargetProperties.ListActionAfter += _ListActionAfter;
        }
        /// <summary>
        /// Po změně Selected v Listu Source = vlevo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SourceSelectedItemsChanged(object sender, EventArgs e)
        {
            this._SetButtonsEnabled();
        }
        /// <summary>
        /// Po změně Selected v Listu Target = vpravo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TargetSelectedItemsChanged(object sender, EventArgs e)
        {
            this._SetButtonsEnabled();
        }
        /// <summary>
        /// Instance ListBoxPanelu pro levý = Source panel
        /// </summary>
        protected DxListBoxPanel SourceListPanel { get { return __SourceListPanel; } }
        /// <summary>
        /// Instance ListBox Controlu pro levý = Source panel
        /// </summary>
        protected DxListBoxControl SourceListBox { get { return __SourceListPanel.ListBox; } }
        /// <summary>
        /// Instance ListBoxPanelu pro levý = Target panel
        /// </summary>
        protected DxListBoxPanel TargetListPanel { get { return __TargetListPanel; } }
        /// <summary>
        /// Instance ListBox Controlu pro levý = Target panel
        /// </summary>
        protected DxListBoxControl TargetListBox { get { return __TargetListPanel.ListBox; } }
        #endregion
        #region Definice vzhledu a chování - propojené pro oba ListBoxy
        /// <summary>
        /// Text zobrazený nad řádkovým filtrem v panelu Source jako titulek controlu.
        /// </summary>
        protected string SourceTitleText { get { return DxSourceProperties.TitleText; } set { DxSourceProperties.TitleText = value; _SetDblTitleTextVisible(); } }
        /// <summary>
        /// Text zobrazený nad řádkovým filtrem v panelu Target jako titulek controlu.
        /// </summary>
        protected string TargetTitleText { get { return DxTargetProperties.TitleText; } set { DxTargetProperties.TitleText = value; _SetDblTitleTextVisible(); } }
        /// <summary>
        /// Síla podtržení titulku, null = default = 2 pixely. Hodnota 0 = zrušit podtržení.
        /// </summary>
        protected int? TitleTextLineWidth { get { return __TitleTextLineWidth; } set { __TitleTextLineWidth = value; DxSourceProperties.TitleTextLineWidth = value; DxTargetProperties.TitleTextLineWidth = value; } } private int? __TitleTextLineWidth;
        /// <summary>
        /// Podle obsahu titulku v <see cref="DxSourceProperties"/> anebo <see cref="DxTargetProperties"/> určí, zda titulky budou zobrazeny nebo ne. 
        /// Jejich viditelnost (shodná pro oba panely) pak nastaví do obou panelů.
        /// </summary>
        private void _SetDblTitleTextVisible()
        {
            bool hasTitles = !String.IsNullOrEmpty(SourceTitleText) || !String.IsNullOrEmpty(TargetTitleText);
            DxSourceProperties.TitleTextVisible = hasTitles;
            DxTargetProperties.TitleTextVisible = hasTitles;
        }
        /// <summary>
        /// Režim chování <see cref="DxDblListBoxPanel"/>.
        /// <para/>
        /// Výchozí hodnota je <see cref="DblListModeType.Mode_FixedSourceToFreeTarget"/>.
        /// </summary>
        protected DblListModeType DblListMode { get { return __DblListMode; } set { __DblListMode = value; _AcceptListStyles(); } } private DblListModeType __DblListMode;
        /// <summary>
        /// Režim řádkového filtru pro oba panely.
        /// <para/>
        /// Výchozí hodnota je <see cref="RowFilterBoxMode.None"/>.
        /// </summary>
        protected RowFilterBoxMode RowFilterMode { get { return DxSourceProperties.RowFilterMode; } set { DxSourceProperties.RowFilterMode = value; DxTargetProperties.RowFilterMode = value; } }
        /// <summary>
        /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
        /// </summary>
        protected bool DrawImageDirectly { get { return DxSourceProperties.DrawImageDirectly; } set { DxSourceProperties.DrawImageDirectly = value; DxTargetProperties.DrawImageDirectly = value; } }
        /// <summary>
        /// Umístění buttonů v rámci <see cref="DxDblListBoxPanel"/>.
        /// <para/>
        /// Výchozí hodnota je <see cref="DblButtonsPositionType.BottomAndCenter"/>.
        /// </summary>
        protected DblButtonsPositionType ButtonsPosition { get { return __ButtonsPosition; } set { __ButtonsPosition = value; _AcceptButtonsPosition(); } } private DblButtonsPositionType __ButtonsPosition;
        /// <summary>
        /// Umístění společných buttonů pro přesun prvků. Nastavuje se od <see cref="ButtonsPosition"/>.
        /// </summary>
        protected ToolbarPosition CommonButtonsPosition { get { return __CommonButtonsPosition; } set { __CommonButtonsPosition = value; _AcceptListStyles(true, true); } } private ToolbarPosition __CommonButtonsPosition;
        /// <summary>
        /// Kam bude přesouvat tlačítko Doprava / Doleva prvky: za aktuální index nebo na konec seznamu?
        /// <para/>
        /// Výchozí hodnota je <see cref="DblItemTargetPositionType.AtListEnd"/>.
        /// </summary>
        protected DblItemTargetPositionType CopyItemTargetPosition { get { return __CopyItemTargetPosition; } set { __CopyItemTargetPosition = value; } } private DblItemTargetPositionType __CopyItemTargetPosition;
        /// <summary>
        /// Je povolena akce Přesunout vše?
        /// <para/>
        /// Výchozí hodnota je <c>false</c>.
        /// </summary>
        public bool MoveAllEnabled { get { return __MoveAllEnabled; } set { __MoveAllEnabled = value; _AcceptListStyles(true, true); } } private bool __MoveAllEnabled;
        /// <summary>
        /// Jsou povoleny akce DragAndDrop?
        /// <para/>
        /// Výchozí hodnota je <c>true</c>.
        /// </summary>
        protected bool DragAndDropEnabled { get { return __DragAndDropEnabled; } set { __DragAndDropEnabled = value; _AcceptListStyles(true, true); } } private bool __DragAndDropEnabled;
        /// <summary>
        /// Jsou povoleny akce přenesení prvků pomocí Clipboardu?
        /// <para/>
        /// Výchozí hodnota je <c>false</c>.
        /// </summary>
        protected bool ClipboardActionsEnabled { get { return __ClipboardActionsEnabled; } set { __ClipboardActionsEnabled = value; _AcceptListStyles(true, true); } } private bool __ClipboardActionsEnabled;
        /// <summary>
        /// Pro tento Double ListBox je povolena akce DoubleClick myší: provede přesun / kopii vybraného prvku na druhou stranu, pokud to lze provést.
        /// <para/>
        /// Výchozí hodnota je <c>true</c>.
        /// </summary>
        protected bool DoubleClickEnabled { get { return __DoubleClickEnabled; } set { __DoubleClickEnabled = value; } } private bool __DoubleClickEnabled;
        /// <summary>
        /// Akceptuje hodnotu pozice buttonůdo <see cref="ButtonsPosition"/>
        /// </summary>
        private void _AcceptButtonsPosition()
        {
            DblButtonsPositionType position = __ButtonsPosition;
            ToolbarPosition sourcePosition = ToolbarPosition.None;
            ToolbarPosition targetPosition = ToolbarPosition.None;
            ToolbarPosition commonPosition = ToolbarPosition.None;
            switch (position)
            {
                case DblButtonsPositionType.None:
                    sourcePosition = ToolbarPosition.None;
                    targetPosition = ToolbarPosition.None;
                    commonPosition = ToolbarPosition.None;
                    break;
                case DblButtonsPositionType.Bottom:
                    sourcePosition = ToolbarPosition.BottomSideCenter;
                    targetPosition = ToolbarPosition.BottomSideCenter;
                    commonPosition = ToolbarPosition.None;
                    break;
                case DblButtonsPositionType.Center:
                    sourcePosition = ToolbarPosition.RightSideCenter;
                    targetPosition = ToolbarPosition.LeftSideCenter;
                    commonPosition = ToolbarPosition.None;
                    break;
                case DblButtonsPositionType.Right:
                    sourcePosition = ToolbarPosition.RightSideCenter;
                    targetPosition = ToolbarPosition.RightSideCenter;
                    commonPosition = ToolbarPosition.None;
                    break;
                case DblButtonsPositionType.BottomAndCenter:
                    sourcePosition = ToolbarPosition.BottomSideCenter;
                    targetPosition = ToolbarPosition.BottomSideCenter;
                    commonPosition = ToolbarPosition.RightSideCenter;
                    break;
            }
            __ButtonsPosition = position;
            DxSourceProperties.ButtonsPosition = sourcePosition;
            DxTargetProperties.ButtonsPosition = targetPosition;
            CommonButtonsPosition = commonPosition;                 // Vyvolá AcceptListStyles
            _DoLayoutButtons();
        }
        /// <summary>
        /// Akceptuje zadané hodnoty <see cref="DblListMode"/>, <see cref="MoveAllEnabled"/>, <see cref="DragAndDropEnabled"/>, <see cref="ClipboardActionsEnabled"/>, <see cref="CommonButtonsPosition"/>
        /// a vloží patřičné hodnoty do svých property i do jednotlivých Listů. Řeší i CommonButtony.
        /// </summary>
        /// <param name="acceptToSource"></param>
        /// <param name="acceptToTarget"></param>
        private void _AcceptListStyles(bool acceptToSource = true, bool acceptToTarget = true)
        {
            if (acceptToSource) _AcceptSourceListStyles();
            if (acceptToTarget) _AcceptTargetListStyles();
            _AcceptCommonButtons();
        }
        /// <summary>
        /// Podle hodnot <see cref="DblListMode"/>, <see cref="MoveAllEnabled"/>, <see cref="DragAndDropEnabled"/>, <see cref="ClipboardActionsEnabled"/>, <see cref="CommonButtonsPosition"/>
        /// vloží patřičné hodnoty do Source Listu.
        /// </summary>
        private void _AcceptSourceListStyles()
        {
            bool moveAllEnabled = __MoveAllEnabled;
            bool dragAndDropEnabled = __DragAndDropEnabled;
            bool clipboardActionsEnabled = __ClipboardActionsEnabled;
            bool moveItemsInPanel = (__CommonButtonsPosition == ToolbarPosition.None);   // Pokud pozice pro "společné buttony" je None, pak tyto buttony dávám přímo do ListPanelu.
            bool duplicityEnabled = false;
            bool autoSortItems = false;

            var buttonTypes = new List<ControlKeyActionType>();
            var keyActions = ControlKeyActionType.None;
            var dragDropActions = DxDragDropActionType.None;
            var exchangeCrossType = DataExchangeCrossType.None;
            var exchangeSourceId = "";

            // SelectAll je povolen vždy:
            buttonTypes.Add(ControlKeyActionType.SelectAll);
            keyActions |= ControlKeyActionType.SelectAll;

            // Pokud je povolen Clipboard, pak přidáme Delimiter a ClipCopy [a možná podle editovatelnosti i Cut a Paste]:
            if (clipboardActionsEnabled)
            {
                buttonTypes.Add(ControlKeyActionType.Delimiter);

                // Copy:
                if (this._HasAnyMode(DblListModeType.SourceToTargetCopy))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipCopy);
                    keyActions |= ControlKeyActionType.ClipCopy;
                }

                // Cut:
                if (this._HasAnyMode(DblListModeType.SourceToTargetMove))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipCut);
                    keyActions |= ControlKeyActionType.ClipCut;
                }

                // Paste:
                if (this._HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipPaste);
                    keyActions |= ControlKeyActionType.ClipPaste;
                }

                exchangeCrossType = DataExchangeCrossType.CurrentApplication | DataExchangeCrossType.OwnControl | DataExchangeCrossType.OtherSelectedControls;
                exchangeSourceId = DxSourceProperties.DataExchangeCurrentControlId + Environment.NewLine + DxTargetProperties.DataExchangeCurrentControlId;
            }

            // Delete:
            if (this._HasAnyMode(DblListModeType.SourceCanDelete))
            {
                buttonTypes.Add(ControlKeyActionType.Delete);
                keyActions |= ControlKeyActionType.Delete;
            }

            // Fyzické buttony v rámci Source Listu pro CopyToRight:
            if (moveItemsInPanel && this._HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove))
            {   // Buttony mohou někdy být v ListPanelu dole = takto (anebo budou v DblPanelu uprostřed):
                buttonTypes.Add(ControlKeyActionType.Delimiter);
                buttonTypes.Add(ControlKeyActionType.CopyToTargetOneE);
                if (moveAllEnabled)
                    buttonTypes.Add(ControlKeyActionType.CopyToTargetAllE);
            }

            // Klávesové akce a DragAndDrop odsud musíme povolit do Source panelu bez ohledu na umístění Buttonů:
            if (this._HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove))
            {
                keyActions |= (ControlKeyActionType.CopyToTargetOneE | ControlKeyActionType.CopyToTargetOneC);
                if (moveAllEnabled)
                    keyActions |= (ControlKeyActionType.CopyToTargetAllE | ControlKeyActionType.CopyToTargetAllC);

                if (dragAndDropEnabled)
                {
                    if (this._HasAnyMode(DblListModeType.SourceToTargetCopy))
                        dragDropActions |= DxDragDropActionType.CopyItemsFrom;
                    if (this._HasAnyMode(DblListModeType.SourceToTargetMove))
                        dragDropActions |= DxDragDropActionType.MoveItemsFrom;
                }
            }

            // DragAndDrop sem = jakožto do SourceListu = do cíle kopírování / přesouvání z Target Listu:
            if (dragAndDropEnabled && this._HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove))
            {
                dragDropActions |= DxDragDropActionType.ImportItemsInto;
            }

            // AutoSort:
            autoSortItems = this._HasAnyMode(DblListModeType.SourceIsAutoSort);

            // Reorder items:
            if (!autoSortItems && this._HasAnyMode(DblListModeType.SourceCanReorder))
            {
                buttonTypes.Add(ControlKeyActionType.Delimiter);
                buttonTypes.Add(ControlKeyActionType.MoveTop);
                buttonTypes.Add(ControlKeyActionType.MoveUp);
                buttonTypes.Add(ControlKeyActionType.MoveDown);
                buttonTypes.Add(ControlKeyActionType.MoveBottom);

                keyActions |= ControlKeyActionType.Move_All;

                if (dragAndDropEnabled)
                    dragDropActions |= DxDragDropActionType.ReorderItems;
            }

            // Duplicity:
            duplicityEnabled = !this._HasAnyMode(DblListModeType.SourceIsUnique);

            // Výsledky do Properties Source Listu:
            var dxSourceProperties = DxSourceProperties;
            dxSourceProperties.ButtonsTypes = buttonTypes.ToArray();
            dxSourceProperties.EnabledKeyActions = keyActions;
            dxSourceProperties.DuplicityEnabled = duplicityEnabled;
            dxSourceProperties.AutoSortItems = autoSortItems;
            dxSourceProperties.DragDropActions = dragDropActions;
            dxSourceProperties.DataExchangeCrossType = exchangeCrossType;
            dxSourceProperties.DataExchangeAcceptSourceControlId = exchangeSourceId;
        }
        /// <summary>
        /// Podle hodnot <see cref="DblListMode"/>, <see cref="MoveAllEnabled"/>, <see cref="DragAndDropEnabled"/>, <see cref="ClipboardActionsEnabled"/>, <see cref="CommonButtonsPosition"/>
        /// vloží patřičné hodnoty do Target Listu.
        /// </summary>
        private void _AcceptTargetListStyles()
        {
            bool moveAllEnabled = __MoveAllEnabled;
            bool dragAndDropEnabled = __DragAndDropEnabled;
            bool clipboardActionsEnabled = __ClipboardActionsEnabled;
            bool moveItemsInPanel = (__CommonButtonsPosition == ToolbarPosition.None);   // Pokud pozice pro "společné buttony" je None, pak tyto buttony dávám přímo do ListPanelu.
            bool duplicityEnabled = false;
            bool autoSortItems = false;

            var buttonTypes = new List<ControlKeyActionType>();
            var keyActions = ControlKeyActionType.None;
            var dragDropActions = DxDragDropActionType.None;
            var exchangeCrossType = DataExchangeCrossType.None;
            var exchangeSourceId = "";

            // SelectAll je povolen vždy:
            buttonTypes.Add(ControlKeyActionType.SelectAll);
            keyActions |= ControlKeyActionType.SelectAll;

            // Pokud je povolen ClipBoard, pak přidáme Delimiter a ClipCopy a Cut a Paste:
            if (clipboardActionsEnabled)
            {
                buttonTypes.Add(ControlKeyActionType.Delimiter);

                // Copy:
                if (this._HasAnyMode(DblListModeType.TargetToSourceCopy))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipCopy);
                    keyActions |= ControlKeyActionType.ClipCopy;
                }

                // Cut:
                if (this._HasAnyMode(DblListModeType.TargetToSourceMove))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipCut);
                    keyActions |= ControlKeyActionType.ClipCut;
                }

                // Paste:
                if (this._HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove))
                {
                    buttonTypes.Add(ControlKeyActionType.ClipPaste);
                    keyActions |= ControlKeyActionType.ClipPaste;
                }

                exchangeCrossType = DataExchangeCrossType.CurrentApplication | DataExchangeCrossType.OwnControl | DataExchangeCrossType.OtherSelectedControls;
                exchangeSourceId = DxSourceProperties.DataExchangeCurrentControlId + Environment.NewLine + DxTargetProperties.DataExchangeCurrentControlId;
            }

            // Delete:
            if (this._HasAnyMode(DblListModeType.TargetCanDelete))
            {
                buttonTypes.Add(ControlKeyActionType.Delete);
                keyActions |= ControlKeyActionType.Delete;
            }

            // Fyzické buttony v rámci Target Listu pro CopyToLeft:
            if (moveItemsInPanel && this._HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove))
            {   // Buttony mohou někdy být v ListPanelu dole = takto (anebo budou v DblPanelu uprostřed):
                buttonTypes.Add(ControlKeyActionType.Delimiter);
                buttonTypes.Add(ControlKeyActionType.CopyToSourceOneE);
                if (moveAllEnabled)
                    buttonTypes.Add(ControlKeyActionType.CopyToSourceAllE);
            }

            // Klávesové akce a DragAndDrop odsud musíme povolit do Target panelu bez ohledu na umístění Buttonů:
            if (this._HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove))
            {
                keyActions |= (ControlKeyActionType.CopyToSourceOneE | ControlKeyActionType.CopyToSourceOneC);
                if (moveAllEnabled)
                    keyActions |= (ControlKeyActionType.CopyToSourceAllE | ControlKeyActionType.CopyToSourceAllC);

                if (dragAndDropEnabled)
                {
                    if (this._HasAnyMode(DblListModeType.TargetToSourceCopy))
                        dragDropActions |= DxDragDropActionType.CopyItemsFrom;
                    if (this._HasAnyMode(DblListModeType.TargetToSourceMove))
                        dragDropActions |= DxDragDropActionType.MoveItemsFrom;
                }
            }

            // DragAndDrop sem = jakožto do TargetListu = do cíle kopírování / přesouvání z Source Listu:
            if (dragAndDropEnabled && this._HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove))
            {
                dragDropActions |= DxDragDropActionType.ImportItemsInto;
            }

            // AutoSort:
            autoSortItems = this._HasAnyMode(DblListModeType.TargetIsAutoSort);

            // Reorder items:
            if (!autoSortItems && this._HasAnyMode(DblListModeType.TargetCanReorder))
            {
                buttonTypes.Add(ControlKeyActionType.Delimiter);
                buttonTypes.Add(ControlKeyActionType.MoveTop);
                buttonTypes.Add(ControlKeyActionType.MoveUp);
                buttonTypes.Add(ControlKeyActionType.MoveDown);
                buttonTypes.Add(ControlKeyActionType.MoveBottom);

                keyActions |= ControlKeyActionType.Move_All;

                if (dragAndDropEnabled)
                    dragDropActions |= DxDragDropActionType.ReorderItems;
            }

            // Duplicity:
            duplicityEnabled = !this._HasAnyMode(DblListModeType.SourceIsUnique);

            // Výsledky do Properties Target Listu:
            var dxTargetProperties = DxTargetProperties;
            dxTargetProperties.ButtonsTypes = buttonTypes.ToArray();
            dxTargetProperties.EnabledKeyActions = keyActions;
            dxTargetProperties.DuplicityEnabled = duplicityEnabled;
            dxTargetProperties.AutoSortItems = autoSortItems;
            dxTargetProperties.DragDropActions = dragDropActions;
            dxTargetProperties.DataExchangeCrossType = exchangeCrossType;
            dxTargetProperties.DataExchangeAcceptSourceControlId = exchangeSourceId;
        }
        /// <summary>
        /// Vrátí true, pokud aktuální režim <see cref="DblListMode"/> má nastaven některý ze zadaných příznaků, stačí jeden jediný a vrátí se true
        /// </summary>
        /// <param name="modes"></param>
        /// <returns></returns>
        private bool _HasAnyMode(params DblListModeType[] modes)
        {
            var currentMode = __DblListMode;
            return modes.Length > 0 && modes.Any(m => ((m & currentMode) != 0));
        }
        /// <summary>
        /// Režim práce s dvěma Listy: co který List umožňuje
        /// </summary>
        [Flags]
        public enum DblListModeType
        {
            /// <summary>
            /// Nic
            /// </summary>
            None = 0,

            /// <summary>
            /// Zdrojový List (vlevo): je možno v něm přesouvat prvky nahoru/dolů
            /// </summary>
            SourceCanReorder = 0x0001,
            /// <summary>
            /// Zdrojový List (vlevo): po vložení prvků budou všechny prvky automaticky setříděny podle hodnoty <see cref="ITextItem.ItemOrder"/>
            /// </summary>
            SourceIsAutoSort = 0x0002,
            /// <summary>
            /// Zdrojový List (vlevo): je možno v něm smazat prvky
            /// </summary>
            SourceCanDelete = 0x0004,
            /// <summary>
            /// Zdrojový List (vlevo): je možno z něj kopírovat prvky do Target doprava, ve zdroji poté zůstávají
            /// </summary>
            SourceToTargetCopy = 0x0010,
            /// <summary>
            /// Zdrojový List (vlevo): je možno z něj přesouvat prvky do Target doprava, ze zdroje se poté odeberou
            /// </summary>
            SourceToTargetMove = 0x0020,
            /// <summary>
            /// Zdrojový List (vlevo): pokud do něj jsou vkládány prvky zprava, pak vlevo mohou být prvky pouze Unique = nonduplicitní.
            /// Porovnání duplicity se provádí podle hodnoty <see cref="ITextItem.ItemId"/>.
            /// </summary>
            SourceIsUnique = 0x0040,

            /// <summary>
            /// Cílový  List (vpravo): je možno v něm přesouvat prvky nahoru/dolů
            /// </summary>
            TargetCanReorder = 0x0100,
            /// <summary>
            /// Cílový  List (vpravo): po vložení prvků budou všechny prvky automaticky setříděny podle hodnoty <see cref="ITextItem.ItemOrder"/>
            /// </summary>
            TargetIsAutoSort = 0x0200,
            /// <summary>
            /// Cílový  List (vpravo): je možno v něm smazat prvky
            /// </summary>
            TargetCanDelete = 0x0400,
            /// <summary>
            /// Cílový  List (vpravo): je možno z něj kopírovat prvky do Source doleva, v cíli poté zůstávají
            /// </summary>
            TargetToSourceCopy = 0x1000,
            /// <summary>
            /// Cílový  List (vpravo): je možno z něj přesouvat prvky do Source doleva, z cíle se poté odeberou
            /// </summary>
            TargetToSourceMove = 0x2000,
            /// <summary>
            /// Cílový  List (vpravo): pokud do něj jsou vkládány prvky zleva, pak vpravo mohou být prvky pouze Unique = nonduplicitní.
            /// Porovnání duplicity se provádí podle hodnoty <see cref="ITextItem.ItemId"/>.
            /// </summary>
            TargetIsUnique = 0x4000,

            /// <summary>
            /// Kombinovaná hodnota, kde Source je pevně dán a nebude měněn, prvky z něj lze kopírovat do Target, Target lze přeuspořádat i smazat, povoluje se duplicita v Target.
            /// </summary>
            Mode_FixedSourceToFreeTarget = SourceToTargetCopy | TargetCanReorder | TargetCanDelete,
            /// <summary>
            /// Kombinovaná hodnota, kde Source a Target obsahují vzájemně přesouvané prvky: prvek je buď vlevo (Source) anebo vpravo (Target), 
            /// přičemž vlevo platí AutoSort a vpravo si uživatel může prvky setřídit sám
            /// </summary>
            Mode_Ballance = SourceToTargetMove | SourceIsAutoSort | TargetCanReorder | TargetToSourceMove
        }
        /// <summary>
        /// Druh umístění buttonů v rámci <see cref="DxDblListBoxPanel"/>.
        /// </summary>
        public enum DblButtonsPositionType
        {
            /// <summary>
            /// Nezobrazovat
            /// </summary>
            None,
            /// <summary>
            /// Na spodní straně každého Listu
            /// </summary>
            Bottom,
            /// <summary>
            /// Uprostřed
            /// </summary>
            Center,
            /// <summary>
            /// Vždy na pravé straně Listů
            /// </summary>
            Right,
            /// <summary>
            /// Jednotlivé Listy (Source a Target) mají svoje vlastní dole, a uprostřed jsou "společné" buttony pro přesouvání doprava / doleva
            /// </summary>
            BottomAndCenter,
            /// <summary>
            /// Nastaveno manuálně pro každý List
            /// </summary>
            Custom
        }
        /// <summary>
        /// Určuje možné cílové pozice pro vložení nebo přesunutí položky v rámci kolekce.
        /// </summary>
        /// <remarks>
        /// Tento výčet slouží k označení umístění položky vzhledem k aktuálnímu stavu seznamu nebo kolekce. 
        /// Hodnoty představují výchozí pozici (dle buttonu), anebo explicitně vyžádanou pozici : za aktuální index nebo konec seznamu.
        /// </remarks>
        public enum DblItemTargetPositionType
        {
            /// <summary>
            /// Nezadáno = Default
            /// </summary>
            None,
            /// <summary>
            /// Default = dle tlačítka
            /// </summary>
            Default,
            /// <summary>
            /// Bez ohledu na použité tlačítko: za aktuální index
            /// </summary>
            AtCurrentIndex,
            /// <summary>
            /// Bez ohledu na použité tlačítko: na konec seznamu
            /// </summary>
            AtListEnd
        }
        #endregion
        #region Tlačítka uprostřed
        /// <summary>
        /// Inicializace pro středová tlačítka
        /// </summary>
        private void _InitButtons()
        {
            var buttonsPanel = new DxPanelControl();
            buttonsPanel.Width = 32;
            buttonsPanel.Dock = DockStyle.Right;
            buttonsPanel.Visible = false;
            buttonsPanel.ClientSizeChanged += _ButtonsPanel_ClientSizeChanged;
            this.Panel1.Controls.Add(buttonsPanel);
            __ButtonsPanel = buttonsPanel;
        }
        /// <summary>
        /// Po změně velikosti panelu pro buttony vypočítáme jejich umístění
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonsPanel_ClientSizeChanged(object sender, EventArgs e)
        {
            _DoLayoutButtons();
        }
        /// <summary>
        /// Společné buttony uprostřed mají být zobrazeny?
        /// </summary>
        private bool _ButtonsRequired { get { return (this.__CommonButtonsPosition != ToolbarPosition.None); } }
        /// <summary>
        /// Umístí středové buttony
        /// </summary>
        private void _DoLayoutButtons()
        {
            this.RunInGui(_DoLayoutButtonsGui);
        }
        /// <summary>
        /// Umístí středové buttony, je voláno v GUI threadu
        /// </summary>
        private void _DoLayoutButtonsGui()
        {
            // Viditelnost:
            bool currentPanelVisible = this.__ButtonsPanel.IsSetVisible();
            bool requestPanelVisible = this._ButtonsRequired && (__Buttons != null && __Buttons.Count > 0);

            // Panel nemá být viditelný:
            if (!requestPanelVisible)
            {
                if (currentPanelVisible)
                    this.__ButtonsPanel.Visible = false;
            }
            else
            {
                // Panel má být viditelný:
                var splitterThick = this.CurrentSplitterThick;
                Size buttonSize = ActionButtonsHelper.GetCurrentButtonSize(this.DxSourceProperties.ButtonsSize, this.__ButtonsPanel.CurrentDpi);
                this.__ButtonsPanel.Width = splitterThick + buttonSize.Width;

                Rectangle innerBounds = this.__ButtonsPanel.GetInnerBounds();
                ActionButtonsHelper.DoButtonsLayout(__Buttons, ref innerBounds, this.__CommonButtonsPosition, this.DxSourceProperties.ButtonsSize, this.__ButtonsPanel.CurrentDpi);

                if (!currentPanelVisible)
                    this.__ButtonsPanel.Visible = true;
            }
        }
        /// <summary>
        /// Určí buttony zobrazované uprostřed
        /// </summary>
        private void _AcceptCommonButtons()
        {
            bool moveAllEnabled = __MoveAllEnabled;
            bool moveItemsInCommon = (__CommonButtonsPosition != ToolbarPosition.None);   // Pokud pozice pro "společné buttony" je None, pak tyto buttony dávám přímo do ListPanelu.

            var buttonTypes = new List<ControlKeyActionType>();
            if (moveItemsInCommon)
            {
                bool canSourceToTarget = _HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove);
                bool canTargetToSource = _HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove);

                if (canSourceToTarget)
                    buttonTypes.Add(ControlKeyActionType.CopyToTargetOneE);
                if (canTargetToSource)
                    buttonTypes.Add(ControlKeyActionType.CopyToSourceOneE);

                if (moveAllEnabled)
                {
                    buttonTypes.Add(ControlKeyActionType.Delimiter);
                    if (canSourceToTarget)
                        buttonTypes.Add(ControlKeyActionType.CopyToTargetAllE);
                    if (canTargetToSource)
                        buttonTypes.Add(ControlKeyActionType.CopyToSourceAllE);
                }
            }

            __CommonButtonsTypes = buttonTypes.ToArray();

            _AcceptButtonsType();
        }
        /// <summary>
        /// Aktuální povolená tlačítka promítne do panelu jako viditelná tlačítka
        /// </summary>
        private void _AcceptButtonsType()
        {
            // Odstraním stávající buttony:
            ActionButtonsHelper.RemoveButtons(ref __Buttons, true);

            // Vytvoříme a umístíme buttony:
            ActionButtonsHelper.CreateActionButtons(__CommonButtonsTypes, ref __Buttons, __ButtonsPanel, this._ButtonClick, out var enabledButtonsActions);
            _DoLayoutButtons();

            // Enabled na Buttony podle jejich akce a podle stavu ListBoxu:
            _SetButtonsEnabled();
        }
        /// <summary>
        /// Nastaví Enabled buttonů
        /// </summary>
        private void _SetButtonsEnabled()
        {
            if (!this._ButtonsRequired) return;

            int totalLeftCount = this.DxSourceProperties.MenuItems?.Length ?? 0;
            int selectedLeftCount = this.DxSourceProperties.SelectedMenuItems?.Length ?? 0;
            int totalRightCount = this.DxTargetProperties.MenuItems?.Length ?? 0;
            int selectedRightCount = this.DxTargetProperties.SelectedMenuItems?.Length ?? 0;
            bool isEditable = true;
            bool clipEnabled = this.DxProperties.ClipboardActionsEnabled;
            bool undoRedoEnabled = this.DxTargetProperties.UndoRedoEnabled;

            ActionButtonsHelper.EnableButtons(__Buttons, totalLeftCount, selectedLeftCount, totalRightCount, selectedRightCount, isEditable, clipEnabled, undoRedoEnabled);
        }
        /// <summary>
        /// Provede akci danou buttonem <paramref name="sender"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ButtonClick(object sender, EventArgs args)
        {
            if (sender is DxSimpleButton dxButton && dxButton.Tag is ControlKeyActionType actionType)
            {
                switch (actionType)
                {
                    case ControlKeyActionType.CopyToTargetOneE:
                    case ControlKeyActionType.CopyToTargetOneC:
                    case ControlKeyActionType.CopyToTargetAllE:
                    case ControlKeyActionType.CopyToTargetAllC:
                    case ControlKeyActionType.CopyToSourceOneE:
                    case ControlKeyActionType.CopyToSourceOneC:
                    case ControlKeyActionType.CopyToSourceAllE:
                    case ControlKeyActionType.CopyToSourceAllC:
                        DoCommonButtonClick(actionType, DxItemsChangeType.HelperButton);
                        break;
                }
            }
        }
        /// <summary>
        /// Typy dostupných tlačítek.
        /// <para/>
        /// Nadeklarujme zde jednotlivá tlačítka v požadovaném pořadí.
        /// Pokud bude definice obsahovat vícekrát jedno stejné tlačítko, bude fyzicky přidáno pouze jedenkrát, poprvé.
        /// <para/>
        /// Pokud jeden prvek v tomto poli bude obsahovat více hodnot (jde o Flags), budou jednotlivé buttony přidány postupně v jejich nativním pořadí.
        /// </summary>
        private ControlKeyActionType[] __CommonButtonsTypes;
        /// <summary>
        /// Tlačítka, která mají být dostupná, v patřičném pořadí
        /// </summary>
        private List<DxSimpleButton> __Buttons;
        /// <summary>
        /// Panel, který zobrazuje tlačítka
        /// </summary>
        private DxPanelControl __ButtonsPanel;
        #endregion
        #region Interakce mezi panely
        /// <summary>
        /// Provede danou společnou akci mezi dvěma Listy
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="changeType"></param>
        protected void DoCommonButtonClick(ControlKeyActionType actionType, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            ITextItem[] actionItems;
            bool isAllItems = ((actionType & (ControlKeyActionType.CopyToTargetAllE | ControlKeyActionType.CopyToTargetAllC | ControlKeyActionType.CopyToSourceAllE | ControlKeyActionType.CopyToSourceAllC)) != 0);
            bool atCurrentIndex = acceptTargetPosition(((actionType & (ControlKeyActionType.CopyToTargetOneC | ControlKeyActionType.CopyToTargetAllC | ControlKeyActionType.CopyToSourceAllC | ControlKeyActionType.CopyToSourceAllC)) != 0));
            bool removeFrom = this._HasAnyMode(DblListModeType.SourceToTargetMove, DblListModeType.TargetToSourceMove);

            bool isSourceToTarget = ((actionType & (ControlKeyActionType.CopyToTargetOneE | ControlKeyActionType.CopyToTargetOneC | ControlKeyActionType.CopyToTargetAllE | ControlKeyActionType.CopyToTargetAllC)) != 0);
            bool isTargetToSource = ((actionType & (ControlKeyActionType.CopyToSourceOneE | ControlKeyActionType.CopyToSourceOneC | ControlKeyActionType.CopyToSourceAllE | ControlKeyActionType.CopyToSourceAllC)) != 0);
            if (isSourceToTarget && this._HasAnyMode(DblListModeType.SourceToTargetCopy, DblListModeType.SourceToTargetMove))
            {
                // Co půjde doprava:
                actionItems = (isAllItems ? DxSourceProperties.MenuItems : DxSourceProperties.SelectedMenuItems);
                // Kam to půjde: na aktuální pozici nebo na konec:
                DxTargetProperties.InsertItems(actionItems, atCurrentIndex, true, changeType);
                // Ze vstupního listu (Source = vlevo) se má smazat:
                if (removeFrom)
                    DxSourceProperties.RemoveItems(actionItems);
            }
            if (isTargetToSource && this._HasAnyMode(DblListModeType.TargetToSourceCopy, DblListModeType.TargetToSourceMove))
            {   // Co půjde doleva:
                actionItems = (isAllItems ? DxTargetProperties.MenuItems : DxTargetProperties.SelectedMenuItems);
                // Kam to půjde: na aktuální pozici nebo na konec:
                DxSourceProperties.InsertItems(actionItems, atCurrentIndex, true, changeType);
                // Ze vstupního listu (Target = vpravo) se má smazat:
                if (removeFrom)
                    DxTargetProperties.RemoveItems(actionItems);
            }


            // Akceptuje nastavení CopyItemTargetPosition, podle něj ovlivní dodanou hodnotu cílové pozice 'atCurrentIndex'
            bool acceptTargetPosition(bool target)
            {
                // Explicitně vyžádaná cílová pozice podle CopyItemTargetPosition:
                switch (this.CopyItemTargetPosition)
                {
                    case DblItemTargetPositionType.AtCurrentIndex: return true;          // true  = Insert za pozici SelectedIndex
                    case DblItemTargetPositionType.AtListEnd: return false;              // false = Add na konec Listu
                }
                // ostatní hodnoty 'copyTargetPosition' nemění hodnotu 'target' = 'atCurrentIndex', zůstává podle vnějšího požadavku:
                return target;
            }
        }
        /// <summary>
        /// Událost po provedení jakékoli akce v Source Listu.
        /// Zde je prostor na tuto akci navázat, anebo vyřešit takové akce, které sám jednoduchý List nedokáže provést = akce předávání mezi dvěma Listy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListActionAfter(object sender, DxListBoxActionEventArgs e)
        {
            switch (e.Action)
            {
                case ControlKeyActionType.CopyToTargetOneE:
                case ControlKeyActionType.CopyToTargetOneC:
                case ControlKeyActionType.CopyToTargetAllE:
                case ControlKeyActionType.CopyToTargetAllC:
                case ControlKeyActionType.CopyToSourceOneE:
                case ControlKeyActionType.CopyToSourceOneC:
                case ControlKeyActionType.CopyToSourceAllE:
                case ControlKeyActionType.CopyToSourceAllC:
                    DoCommonButtonClick(e.Action, e.ChangeType);
                    break;
            }
        }
        /// <summary>
        /// DoubleClick na ListBoxu Source = vlevo: podle nastavení může provést akci ControlKeyActionType.CopyToRightOneEnd
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _SourceList_MouseDoubleClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            if (this.DoubleClickEnabled && args.Item != null)
                DoCommonButtonClick(ControlKeyActionType.CopyToTargetOneE, DxItemsChangeType.UserInteractive);
        }
        /// <summary>
        /// DoubleClick na ListBoxu Target = vpravo: podle nastavení může provést akci ControlKeyActionType.CopyToLeftOneEnd
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _TargetList_MouseDoubleClick(object sender, DxListBoxItemMouseClickEventArgs args)
        {
            if (this.DoubleClickEnabled && args.Item != null)
                DoCommonButtonClick(ControlKeyActionType.CopyToSourceOneE, DxItemsChangeType.UserInteractive);
        }
        #endregion
        #region DxProperties : property + třída, která do sebe shrnuje čistě jen Nephrite vlastnosti
        /// <summary>
        /// Souhrn vlastností (data a eventy), které tato třída poskytuje systému Nephrite
        /// </summary>
        public DxPropertiesInfo DxProperties
        {
            get
            {
                if (__DxProperties is null)
                    __DxProperties = new DxPropertiesInfo(this);
                return __DxProperties;
            }
        }
        private DxPropertiesInfo __DxProperties;
        /// <summary>
        /// Vlastnosti panelu vlevo = Source
        /// </summary>
        public DxListBoxPanel.DxPropertiesInfo DxSourceProperties { get { return __SourceListPanel.DxProperties; } }
        /// <summary>
        /// Vlastnosti panelu vpravo = Target
        /// </summary>
        public DxListBoxPanel.DxPropertiesInfo DxTargetProperties { get { return __TargetListPanel.DxProperties; } }
        /// <summary>
        /// Třída pro Nephrite vlastnosti
        /// </summary>
        public class DxPropertiesInfo
        {
            #region Konstruktor
            internal DxPropertiesInfo(DxDblListBoxPanel owner)
            {
                __Owner = owner;
            }
            private DxDblListBoxPanel __Owner;
            #endregion
            #region Vlastnosti
            /// <summary>
            /// Text zobrazený nad řádkovým filtrem v panelu Source jako titulek controlu.
            /// </summary>
            public string SourceTitleText { get { return __Owner.SourceTitleText; } set { __Owner.SourceTitleText = value; } }
            /// <summary>
            /// Text zobrazený nad řádkovým filtrem v panelu Target jako titulek controlu.
            /// </summary>
            public string TargetTitleText { get { return __Owner.TargetTitleText; } set { __Owner.TargetTitleText = value; } }
            /// <summary>
            /// Síla podtržení titulku, null = default = 2 pixely. Hodnota 0 = zrušit podtržení.
            /// </summary>
            public int? TitleTextLineWidth { get { return __Owner.TitleTextLineWidth; } set { __Owner.TitleTextLineWidth = value; } }
            /// <summary>
            /// Instance ListBoxPanelu pro levý = Source panel
            /// </summary>
            public DxListBoxPanel SourceListPanel { get { return __Owner.SourceListPanel; } }
            /// <summary>
            /// Instance ListBox Controlu pro levý = Source panel
            /// </summary>
            public DxListBoxControl SourceListBox { get { return __Owner.SourceListPanel.ListBox; } }
            /// <summary>
            /// Instance ListBoxPanelu pro levý = Target panel
            /// </summary>
            public DxListBoxPanel TargetListPanel { get { return __Owner.TargetListPanel; } }
            /// <summary>
            /// Instance ListBox Controlu pro levý = Target panel
            /// </summary>
            public DxListBoxControl TargetListBox { get { return __Owner.TargetListPanel.ListBox; } }
            /// <summary>
            /// Režim chování <see cref="DxDblListBoxPanel"/>.
            /// <para/>
            /// Výchozí hodnota je <see cref="DblListModeType.Mode_FixedSourceToFreeTarget"/>.
            /// </summary>
            public DblListModeType DblListMode { get { return __Owner.DblListMode; } set { __Owner.DblListMode = value; } }
            /// <summary>
            /// Režim řádkového filtru pro oba panely.
            /// <para/>
            /// Výchozí hodnota je <see cref="RowFilterBoxMode.None"/>.
            /// </summary>
            public RowFilterBoxMode RowFilterMode { get { return __Owner.RowFilterMode; } set { __Owner.RowFilterMode = value; } }
            /// <summary>
            /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
            /// </summary>
            public bool DrawImageDirectly { get { return __Owner.DrawImageDirectly; } set { __Owner.DrawImageDirectly = value; } }
            /// <summary>
            /// Pozice splitteru
            /// </summary>
            public int SplitterPosition { get { return __Owner.SplitterPosition; } set { __Owner.SplitterPosition = value; } }
            /// <summary>
            /// Umístění buttonů v rámci <see cref="DxDblListBoxPanel"/>.
            /// <para/>
            /// Výchozí hodnota je <see cref="DblButtonsPositionType.BottomAndCenter"/>.
            /// </summary>
            public DblButtonsPositionType ButtonsPosition { get { return __Owner.ButtonsPosition; } set { __Owner.ButtonsPosition = value; } }
            /// <summary>
            /// Kam bude přesouvat tlačítko Doprava / Doleva prvky: za aktuální index nebo na konec seznamu?
            /// <para/>
            /// Výchozí hodnota je <see cref="DblItemTargetPositionType.AtListEnd"/>.
            /// </summary>
            protected DblItemTargetPositionType CopyItemTargetPosition { get { return __Owner.CopyItemTargetPosition; } set { __Owner.CopyItemTargetPosition = value; } }
            /// <summary>
            /// Je povolena akce Přesunout vše?
            /// <para/>
            /// Výchozí hodnota je <c>false</c>.
            /// </summary>
            public bool MoveAllEnabled { get { return __Owner.MoveAllEnabled; } set { __Owner.MoveAllEnabled = value; } }
            /// <summary>
            /// Jsou povoleny akce DragAndDrop?
            /// <para/>
            /// Výchozí hodnota je <c>true</c>.
            /// </summary>
            public bool DragAndDropEnabled { get { return __Owner.DragAndDropEnabled; } set { __Owner.DragAndDropEnabled = value; } }
            /// <summary>
            /// Jsou povoleny akce přenesení prvků pomocí Clipboardu?
            /// <para/>
            /// Výchozí hodnota je <c>false</c>.
            /// </summary>
            public bool ClipboardActionsEnabled { get { return __Owner.ClipboardActionsEnabled; } set { __Owner.ClipboardActionsEnabled = value; } }
            /// <summary>
            /// Pro tento Double ListBox je povolena akce DoubleClick myší: provede přesun / kopii vybraného prvku na druhou stranu, pokud to lze provést.
            /// <para/>
            /// Výchozí hodnota je <c>true</c>.
            /// </summary>
            public bool DoubleClickEnabled { get { return __Owner.DoubleClickEnabled; } set { __Owner.DoubleClickEnabled = value; } }
            #endregion
            #region MenuItems
            /// <summary>
            /// Prvky zdrojového Listu (vlevo) typované na <see cref="ITextItem"/>.
            /// </summary>
            public ITextItem[] SourceMenuItems { get { return __Owner.DxSourceProperties.MenuItems; } set { __Owner.DxSourceProperties.MenuItems = value; } }
            /// <summary>
            /// Obsahuje pole prvků zdrojového Listu (vlevo), které jsou aktuálně Selected. 
            /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
            /// </summary>
            public ITextItem[] SourceSelectedMenuItems { get { return __Owner.DxSourceProperties.SelectedMenuItems; } set { __Owner.DxSourceProperties.SelectedMenuItems = value; } }
            /// <summary>
            /// Prvky cílového Listu (vpravo) typované na <see cref="ITextItem"/>.
            /// </summary>
            public ITextItem[] TargetMenuItems { get { return __Owner.DxTargetProperties.MenuItems; } set { __Owner.DxTargetProperties.MenuItems = value; } }
            /// <summary>
            /// Obsahuje pole prvků cílového Listu (vpravo), které jsou aktuálně Selected. 
            /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
            /// </summary>
            public ITextItem[] TargetSelectedMenuItems { get { return __Owner.DxTargetProperties.SelectedMenuItems; } set { __Owner.DxTargetProperties.SelectedMenuItems = value; } }
            #endregion
            #region Eventy
            /// <summary>
            /// Událost volaná po změně prvků Target Listu typu MenuItems.<br/>
            /// </summary>
            public event DxListBoxMenuItemsChangedDelegate SourceMenuItemsChanged { add { __Owner.DxSourceProperties.MenuItemsChanged += value; } remove { __Owner.DxSourceProperties.MenuItemsChanged -= value; } }
            /// <summary>
            /// Událost volaná po změně prvků Target Listu typu MenuItems.<br/>
            /// </summary>
            public event DxListBoxMenuItemsChangedDelegate TargetMenuItemsChanged { add { __Owner.DxTargetProperties.MenuItemsChanged += value; } remove { __Owner.DxTargetProperties.MenuItemsChanged -= value; } }
            #endregion
            #region Akce, metody
            /// <summary>
            /// Provede danou společnou akci mezi dvěma Listy
            /// </summary>
            /// <param name="actionType"></param>
            /// <param name="changeType"></param>
            public void DoCommonButtonClick(ControlKeyActionType actionType, DxItemsChangeType changeType = DxItemsChangeType.Code) { __Owner.DoCommonButtonClick(actionType, changeType); }
            #endregion
        }
        #endregion
    }
    #endregion
    #region class DxListBoxPanel : Panel obsahující jeden DxListBoxControl + pole buttonů
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
            __TitleLabel = new DxTitleLabelControl() { Text = "", Visible = false };
            __ListBox = new DxListBoxControl();
            __Buttons = new List<DxSimpleButton>();
            __ButtonsPosition = ToolbarPosition.RightSideCenter;
            __ButtonsTypes = null;
            __ButtonsSize = ResourceImageSizeType.Medium;
            this.Controls.Add(__TitleLabel);
            this.Controls.Add(__ListBox);
            this.Padding = new Padding(0);
            this.ClientSizeChanged += _ClientSizeChanged;
            this.Enter += _Panel_Enter;

            // Tyto eventy mají vliv na zobrazování Enabled pro buttony:
            __ListBox.DxProperties.UndoRedoEnabled = false;
            __ListBox.DxProperties.UndoRedoEnabledChanged += _ListBox_UndoRedoEnabledChanged;
            __ListBox.DxProperties.ListItemsChanged += __ListBox_ListItemsChanged;
            __ListBox.DxProperties.SelectedItemsChanged += _ListBox_SelectedItemsChanged;

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
            this.__ListBox.ListBoxNative.Select();
            this.__ListBox.ListBoxNative.Focus();
        }
        /// <summary>
        /// Proběhne po změně v poli <see cref="MenuItems"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __ListBox_ListItemsChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            _SetButtonsEnabled();
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
        /// Rozmístí prvky (Titulek, filtr, tlačítka, ListBox) podle pravidel do svého prostoru
        /// </summary>
        protected virtual void DoLayout()
        {
            this.RunInGui(doLayout);

            void doLayout()
            {
                Rectangle innerBounds = this.GetInnerBounds();
                if (innerBounds.Width >= 30 && innerBounds.Height >= 30)
                {
                    _ButtonsLayout(ref innerBounds);
                    _TitleLayout(ref innerBounds);
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
                ActionButtonsHelper.RemoveButtons(ref __Buttons, false);
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
        protected RowFilterBoxMode RowFilterMode { get { return __RowFilterMode; } set { _SetRowFilterMode(value); } }
        /// <summary>
        /// Vyprázdní obsah řádkového filtru
        /// </summary>
        protected void RowFilterClear() { this._RowFilterClear(); }
        #endregion
        #region Titulek
        /// <summary>
        /// Text zobrazený nad řádkovým filtrem jako titulek controlu. Jeho zobrazování řídí hodnota <see cref="TitleTextVisible"/>.
        /// </summary>
        protected string TitleText { get { return __TitleText; } set { __TitleText = value; _AcceptTitle(); } } private string __TitleText;
        /// <summary>
        /// Zobrazovat titulkový text?
        /// <para/>
        /// null = výchozí: titulek bude zobrazen, pokud bude neprázdný. Pokud nebude zobrazen, budou ostatní controly začínat nahoře na souřadnici Y = 0.<br/>
        /// false: titulek nebude zobrazen, bez ohledu na obsah. Ostatní controly budou začínat nahoře na souřadnici Y = 0.<br/>
        /// true: titulek bude zobrazen, i když by byl prázdný. Ostatní controly budou začínat pod prostorem titulku.
        /// <para/>
        /// Toto chování je zde proto, abychom v <see cref="DxDblListBoxPanel"/> mohli mít titulek například jen na jedné straně, ale sousední ListBox pak zobrazuje prázdný prostor.
        /// </summary>
        protected bool? TitleTextVisible { get { return __TitleTextVisible; } set { __TitleTextVisible = value; _AcceptTitle(); } } private bool? __TitleTextVisible;
        /// <summary>
        /// Síla podtržení titulku, null = default = 2 pixely. Hodnota 0 = zrušit podtržení.
        /// </summary>
        protected int? TitleTextLineWidth { get { return __TitleTextLineWidth; } set { __TitleTextLineWidth = value; _AcceptTitle(); } } private int? __TitleTextLineWidth;
        /// <summary>
        /// Akceptuje hodnoty zadané do <see cref="TitleText"/> a <see cref="TitleTextVisible"/>, naství viditelnost titulku a zajistí Layout.
        /// </summary>
        private void _AcceptTitle()
        {
            // Obsah textu - pokud by se změnil, a přitom se nezmění jeho Visible (již nastavené na true):
            var titleText = TitleText;
            var requestText = titleText ?? "";
            var currentText = __TitleLabel.Text;
            if (!String.Equals(requestText, currentText, StringComparison.InvariantCulture))
                __TitleLabel.Text = requestText;

            var requestLine = __TitleTextLineWidth ?? 2;
            if (__TitleLabel.TitleLineWidth != requestLine)
                __TitleLabel.TitleLineWidth = requestLine;

            var titleTextVisible = TitleTextVisible;
            bool currentIsVisible = (titleTextVisible.HasValue ? titleTextVisible.Value : !String.IsNullOrEmpty(requestText));

            if (currentIsVisible != __TitleTextVisibleCurrent)
            {
                __TitleTextVisibleCurrent = currentIsVisible;
                DoLayout();
            }
        }
        /// <summary>
        /// Zajistí layout pro TitleText
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _TitleLayout(ref Rectangle innerBounds)
        {
            bool currentVisible = __TitleLabel.Visible;
            bool requestVisible = __TitleTextVisibleCurrent;
            if (!requestVisible)
            {   // Bez titulku:
                if (currentVisible)
                    __TitleLabel.Visible = false;
            }
            else
            {   // S titulkem:
                var currentText = __TitleLabel.Text;
                var requestText = TitleText;
                if (!String.Equals(requestText, currentText, StringComparison.InvariantCulture))
                    __TitleLabel.Text = requestText ?? "";

                var tHeight = __TitleLabel.HeightOptimal;
                var tBounds = new Rectangle(innerBounds.X, innerBounds.Y + 2, innerBounds.Width, tHeight);
                if (__TitleLabel.Bounds != tBounds)
                    __TitleLabel.Bounds = tBounds;

                var innerTop = tBounds.Bottom + 3;
                innerBounds = new Rectangle(innerBounds.X, innerTop, innerBounds.Width, innerBounds.Height - innerTop);

                if (!currentVisible)
                    __TitleLabel.Visible = true;
            }
        }
        /// <summary>
        /// Objekt pro label titulek
        /// </summary>
        private DxTitleLabelControl __TitleLabel;
        /// <summary>
        /// Aktuálně platná hodnota viditelnosti titulku
        /// </summary>
        private bool __TitleTextVisibleCurrent;
        #endregion
        #region Data = položky, a layout = Template
        /// <summary>
        /// Režim prvků v ListBoxu.
        /// </summary>
        #region Jednoduchý List postavený nad položkami ITextItem
        /// <summary>
        /// Aktuálně označené objekty. Může jich být i více, nebo žádný.
        /// Objekty to mohou být různé, typicky <see cref="ITextItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID označených řádků je v poli <see cref="SelectedItemsId"/>.
        /// </summary>
        public object[] SelectedItems { get { return __ListBox.DxProperties.SelectedItems; } }
        /// <summary>
        /// Pole obsahující ID selectovaných záznamů.
        /// </summary>
        public object[] SelectedItemsId { get { return __ListBox.DxProperties.SelectedItemsId; } }
        /// <summary>
        /// Prvek, na kterém je kurzor. Je jen jediný, nebo null.
        /// Objekty to mohou být různé, typicky <see cref="ITextItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID aktivního řádku je v <see cref="CurrentItemId"/>.
        /// </summary>
        public object CurrentItem { get { return __ListBox.DxProperties.CurrentItem; } }
        /// <summary>
        /// Pole obsahující ID aktivního řádku.
        /// </summary>
        public object CurrentItemId { get { return __ListBox.DxProperties.CurrentItemId; } }
        #endregion
        #endregion
        #region Filtrování položek: klientské / serverové
        /// <summary>
        /// Inicializace řádkového filtrování
        /// </summary>
        private void _RowFilterInitialize()
        {
            __RowFilterMode = RowFilterBoxMode.None;
            this.DxProperties.ListActionBefore += _ListBox_ListActionBefore;
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
            if (filterMode == RowFilterBoxMode.None) { e.Cancel = true; return; }

            string text = ((e.Action == ControlKeyActionType.FillKeyToFilter) ? DxComponent.KeyConvertToChar(e.Keys, true)?.ToString() : (string)null);
            switch (filterMode)
            {
                case RowFilterBoxMode.Client:
                    _RowFilterClientSetFocus(text);
                    break;
                case RowFilterBoxMode.Server:
                    _RowFilterServerSetFocus(text);
                    __RowFilterServer.Focus();
                    break;
            }
        }
        /// <summary>
        /// Dispose řádkového filtrování
        /// </summary>
        private void _RowFilterDispose()
        {
            _RowFilterClientRemove();
            _RowFilterServerRemove();
            __RowFilterMode = RowFilterBoxMode.None;
        }
        /// <summary>
        /// Aktivuje daný režim řádkového filtru
        /// </summary>
        /// <param name="filterMode"></param>
        private void _SetRowFilterMode(RowFilterBoxMode filterMode)
        {
            this.RunInGui(() => _SetRowFilterModeGui(filterMode));
        }
        /// <summary>
        /// Aktivuje daný režim řádkového filtru, v GUI threadu
        /// </summary>
        /// <param name="newFilterMode"></param>
        private void _SetRowFilterModeGui(RowFilterBoxMode newFilterMode)
        {
            var oldFilterMode = __RowFilterMode;

            if (newFilterMode != RowFilterBoxMode.Client && _RowFilterClientExists)
                _RowFilterClientRemove();
            if (newFilterMode != RowFilterBoxMode.Server && _RowFilterServerExists)
                _RowFilterServerRemove();

            if (newFilterMode == RowFilterBoxMode.Client && !_RowFilterClientExists)
                _RowFilterClientPrepare();
            if (newFilterMode == RowFilterBoxMode.Server && !_RowFilterServerExists)
                _RowFilterServerPrepare();

            switch (newFilterMode)
            {
                case RowFilterBoxMode.Client:
                    if (!__RowFilterClient.IsSetVisible())
                        __RowFilterClient.Visible = true;
                    break;
                case RowFilterBoxMode.Server:
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
        /// Vyprázdní obsah řádkového filtru
        /// </summary>
        private void _RowFilterClear()
        {
            switch (__RowFilterMode)
            {
                case RowFilterBoxMode.Client:
                    this.RunInGui(() => _RowFilterClearClientGui());
                    break;
                case RowFilterBoxMode.Server:
                    this.RunInGui(() => _RowFilterClearServerGui());
                    break;
            }
        }
        /// <summary>
        /// Vyprázdní obsah klientského řádkového filtru v GUI threadu
        /// </summary>
        private void _RowFilterClearClientGui()
        {
            __RowFilterClient.ClearFilter();
        }
        /// <summary>
        /// Vyprázdní obsah klientského řádkového filtru v GUI threadu
        /// </summary>
        private void _RowFilterClearServerGui()
        {
            __RowFilterServer.FilterText = "";
        }
        /// <summary>
        /// Umístí FilterBox (pokud je Visible) do daného prostoru, a ten zmenší o velikost FilterBoxu
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _RowFilterLayout(ref Rectangle innerBounds)
        {
            var filterMode = __RowFilterMode;
            if (filterMode == RowFilterBoxMode.Client && _RowFilterClientExists)
            {
                Rectangle filterBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, __RowFilterClient.Height);
                __RowFilterClient.Bounds = filterBounds;
                int y = __RowFilterClient.Bottom + 1;
                innerBounds = new Rectangle(innerBounds.X, y, innerBounds.Width, innerBounds.Bottom - y);
            }
            else if (filterMode == RowFilterBoxMode.Server && _RowFilterServerExists)
            {
                Rectangle filterBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, __RowFilterServer.Height);
                __RowFilterServer.Bounds = filterBounds;
                int y = __RowFilterServer.Bottom + 1;
                innerBounds = new Rectangle(innerBounds.X, y, innerBounds.Width, innerBounds.Bottom - y);
            }
        }
        /// <summary>
        /// Varianta řádkového filtru.
        /// </summary>
        private RowFilterBoxMode __RowFilterMode;
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
            __RowFilterClient = new DxSearchControl();
            __RowFilterClient.Client = __ListBox.ListBoxNative;
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
                this.__ListBox.DxProperties.UnSelectAll();
                this.__ListBox.DxProperties.SelectedIndex = 0;
                this._MainControlFocus();
            }
        }
        /// <summary>
        /// Klientský RowFilter
        /// </summary>
        private DxSearchControl __RowFilterClient;
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
        protected DxFilterBox RowFilterServer { get { return __RowFilterServer; } }
        /// <summary>
        /// Aktuální text v řádkovém filtru
        /// </summary>
        protected DxFilterBoxValue RowFilterServerText { get { return __RowFilterServer?.FilterValue; } set { if (_RowFilterServerExists) __RowFilterServer.FilterValue = value; } }
        /// <summary>
        /// Pole operátorů nabízených pod tlačítkem vlevo.
        /// Pokud bude vloženo null nebo prázdné pole, pak tlačítko vlevo nebude zobrazeno vůbec, a v hodnotě FilterValue bude Operator = null.
        /// </summary>
        protected List<IMenuItem> RowFilterServerOperators { get { return __RowFilterServer?.FilterOperators; } set { if (_RowFilterServerExists) __RowFilterServer.FilterOperators = value; } }
        /// <summary>
        /// Za jakých událostí se volá event <see cref="RowFilterServerChanged"/>
        /// </summary>
        protected DxFilterBoxChangeEventSource? RowFilterServerChangedSources { get { return __RowFilterServer?.FilterValueChangedSources; } set { if (_RowFilterServerExists) __RowFilterServer.FilterValueChangedSources = value ?? DxFilterBoxChangeEventSource.DefaultGreen; } }
        /// <summary>
        /// Událost volaná po hlídané změně obsahu filtru.
        /// Argument obsahuje hodnotu filtru a druh události, která vyvolala event.
        /// Druhy události, pro které se tento event volá, lze nastavit v <see cref="RowFilterServerChangedSources"/>.
        /// </summary>
        protected event EventHandler<DxFilterBoxChangeArgs> RowFilterServerChanged;
        /// <summary>
        /// Provede se po stisku Enter v řádkovém filtru (i bez změny textu), vhodné pro řízení Focusu
        /// </summary>
        protected event EventHandler RowFilterServerKeyEnter;
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
        /// Typy dostupných tlačítek.
        /// <para/>
        /// Nadeklarujme zde jednotlivá tlačítka v požadovaném pořadí.
        /// Pokud bude definice obsahovat vícekrát jedno stejné tlačítko, bude fyzicky přidáno pouze jedenkrát, poprvé.
        /// <para/>
        /// Pokud jeden prvek v tomto poli bude obsahovat více hodnot (jde o Flags), budou jednotlivé buttony přidány postupně v jejich nativním pořadí.
        /// </summary>
        protected ControlKeyActionType[] ButtonsTypes { get { return __ButtonsTypes; } set { __ButtonsTypes = value; _AcceptButtonsType(); DoLayout(); } }
        /// <summary>
        /// Umístění tlačítek
        /// </summary>
        protected ToolbarPosition ButtonsPosition { get { return __ButtonsPosition; } set { __ButtonsPosition = value; DoLayout(); } }
        /// <summary>
        /// Velikost tlačítek
        /// </summary>
        protected ResourceImageSizeType ButtonsSize { get { return __ButtonsSize; } set { __ButtonsSize = value; DoLayout(); } }

        /// <summary>
        /// Aktuální povolená tlačítka promítne do panelu jako viditelná tlačítka, a i do ListBoxu jako povolené klávesové akce
        /// </summary>
        private void _AcceptButtonsType()
        {
            // Odstraním stávající buttony:
            ActionButtonsHelper.RemoveButtons(ref __Buttons, true);

            // Vytvoříme buttony:
            ActionButtonsHelper.CreateActionButtons(__ButtonsTypes, ref __Buttons, this, this._ButtonClick, out var enabledButtonsActions);

            // Uložíme si vedlejší výsledky:
            // Povolené akce ListBoxu dané Buttony:
            DxProperties.EnabledButtonsActions = enabledButtonsActions;

            // Pokud bylo povoleno UndoRedo, pak povolím i odpovídající funkcionalitu:
            var isEnabledUndoRedo = (enabledButtonsActions.HasFlag(ControlKeyActionType.Undo) || enabledButtonsActions.HasFlag(ControlKeyActionType.Redo));
            if (isEnabledUndoRedo && !DxProperties.UndoRedoEnabled)
                DxProperties.UndoRedoEnabled = true;

            // Enabled na Buttony podle jejich akce a podle stavu ListBoxu:
            _SetButtonsEnabled();
        }
        /// <summary>
        /// Umístí tlačítka podle potřeby do daného vnitřního prostoru, ten zmenší o prostor zabraný tlačítky
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _ButtonsLayout(ref Rectangle innerBounds)
        {
            ActionButtonsHelper.DoButtonsLayout(__Buttons, ref innerBounds, ButtonsPosition, __ButtonsSize, CurrentDpi);
        }
        /// <summary>
        /// Promítne stav <see cref="UndoRedoController.UndoEnabled"/> a <see cref="UndoRedoController.RedoEnabled"/> 
        /// z controlleru <see cref="UndoRedoController"/> do buttonů.
        /// </summary>
        private void _ListBox_UndoRedoEnabledChanged(object sender, EventArgs e)
        {
            _SetButtonsEnabled();
        }
        /// <summary>
        /// Po změně Selected prvků reaguje Enabled buttonů
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedItemsChanged(object sender, EventArgs e)
        {
            _SetButtonsEnabled();
        }
        /// <summary>
        /// Nastaví Enabled buttonů
        /// </summary>
        private void _SetButtonsEnabled()
        {
            int totalCount = this.__ListBox.DxProperties.VisibleItemsCount;
            int selectedCount = this.__ListBox.DxProperties.SelectedItemsCount;
            bool undoRedoEnabled = this.DxProperties.UndoRedoEnabled;
            ActionButtonsHelper.EnableButtons(__Buttons, totalCount, selectedCount, true, true, undoRedoEnabled);
        }
        /// <summary>
        /// Provede akci danou buttonem <paramref name="sender"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ButtonClick(object sender, EventArgs args)
        {
            if (sender is DxSimpleButton dxButton && dxButton.Tag is ControlKeyActionType keyAction)
            {
                __ListBox.DxProperties.DoKeyActions(DxItemsChangeType.HelperButton, keyAction);
            }
        }
        /// <summary>
        /// Typy dostupných tlačítek.
        /// <para/>
        /// Nadeklarujme zde jednotlivá tlačítka v požadovaném pořadí.
        /// Pokud bude definice obsahovat vícekrát jedno stejné tlačítko, bude fyzicky přidáno pouze jedenkrát, poprvé.
        /// <para/>
        /// Pokud jeden prvek v tomto poli bude obsahovat více hodnot (jde o Flags), budou jednotlivé buttony přidány postupně v jejich nativním pořadí.
        /// </summary>
        private ControlKeyActionType[] __ButtonsTypes;
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
        
        #region DxProperties : property + třída, která do sebe shrnuje čistě jen Nephrite vlastnosti
        /// <summary>
        /// Souhrn vlastností (data a eventy), které tato třída poskytuje systému Nephrite
        /// </summary>
        public DxPropertiesInfo DxProperties
        {
            get
            {
                if (__DxProperties is null)
                    __DxProperties = new DxPropertiesInfo(this);
                return __DxProperties;
            }
        }
        private DxPropertiesInfo __DxProperties;
        /// <summary>
        /// Třída pro Nephrite vlastnosti
        /// </summary>
        public class DxPropertiesInfo
        {
            #region Konstruktor
            internal DxPropertiesInfo(DxListBoxPanel owner)
            {
                __Owner = owner;
            }
            private DxListBoxPanel __Owner;
            /// <summary>
            /// Properties vlastního ListBoxu
            /// </summary>
            protected DxListBoxControl.DxPropertiesInfo DxListProperties { get { return __Owner.ListBox.DxProperties; } }
            #endregion
            #region Vlastnosti
            /// <summary>
            /// Text zobrazený nad řádkovým filtrem jako titulek controlu. Jeho zobrazování řídí hodnota <see cref="TitleTextVisible"/>.
            /// </summary>
            public string TitleText { get { return __Owner.TitleText; } set { __Owner.TitleText = value; } }
            /// <summary>
            /// Zobrazovat titulkový text?
            /// <para/>
            /// null = výchozí: titulek bude zobrazen, pokud bude neprázdný. Pokud nebude zobrazen, budou ostatní controly začínat nahoře na souřadnici Y = 0.<br/>
            /// false: titulek nebude zobrazen, bez ohledu na obsah. Ostatní controly budou začínat nahoře na souřadnici Y = 0.<br/>
            /// true: titulek bude zobrazen, i když by byl prázdný. Ostatní controly budou začínat pod prostorem titulku.
            /// <para/>
            /// Toto chování je zde proto, abychom v <see cref="DxDblListBoxPanel"/> mohli mít titulek například jen na jedné straně, ale sousední ListBox pak zobrazuje prázdný prostor.
            /// </summary>
            public bool? TitleTextVisible { get { return __Owner.TitleTextVisible; } set { __Owner.TitleTextVisible = value; } }
            /// <summary>
            /// Síla podtržení titulku, null = default = 2 pixely. Hodnota 0 = zrušit podtržení.
            /// </summary>
            public int? TitleTextLineWidth { get { return __Owner.TitleTextLineWidth; } set { __Owner.TitleTextLineWidth = value; } }
            /// <summary>
            /// Výška prvku
            /// </summary>
            public int ItemHeight { get { return DxListProperties.ItemHeight; } set { DxListProperties.ItemHeight = value; } }
            /// <summary>
            /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
            /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
            /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
            /// Hodnota vyšší než 10 se akceptuje jako 10.
            /// </summary>
            public int ItemHeightPadding { get { return DxListProperties.ItemHeightPadding; } set { DxListProperties.ItemHeightPadding = value; } }
            /// <summary>
            /// Režim prvků v ListBoxu.
            /// </summary>
            public ListBoxItemsMode ItemsMode { get { return DxListProperties.ItemsMode; } }
            /// <summary>
            /// Povoluje se výběr více prvků pomocí Ctrl+Mouse?
            /// </summary>
            public bool MultiSelectEnabled { get { return DxListProperties.MultiSelectEnabled; } set { DxListProperties.MultiSelectEnabled = value; } }
            /// <summary>
            /// Konkrétní režim MultiSelectu
            /// </summary>
            public SelectionMode SelectionMode { get { return DxListProperties.SelectionMode; } set { DxListProperties.SelectionMode = value; } }
            /// <summary>
            /// Gets or sets whether to arrange items across multiple columns if the ListBox cannot fit all items vertically.
            /// true to arrange items across multiple columns; otherwise, false.
            /// </summary>
            public virtual bool MultiColumn { get { return DxListProperties.MultiColumn; } set { DxListProperties.MultiColumn = value; } }
            /// <summary>
            /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
            /// </summary>
            public bool DrawImageDirectly { get { return DxListProperties.DrawImageDirectly; } set { DxListProperties.DrawImageDirectly = value; } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>
            /// </summary>
            public int[] MenuItemColumnWidths { get { return DxListProperties.MenuItemColumnWidths; } set { DxListProperties.MenuItemColumnWidths = value; } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>, validované, aktuální platné dle Zoomu a DPI, namísto případných záporných hodnot obsahuje 0.
            /// </summary>
            public int[] MenuItemColumnWidthsCurrent { get { return DxListProperties.MenuItemColumnWidthsCurrent; } }
            /// <summary>
            /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
            /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
            /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
            /// </summary>
            public bool DuplicityEnabled { get { return DxListProperties.DuplicityEnabled; } set { DxListProperties.DuplicityEnabled = value; } }
            /// <summary>
            /// Pokud obsahuje true, pak List si po každé změně položek zajistí Sort svých položek v <see cref="DxListBoxControl.MenuItems"/> podle hodnoty <see cref="ITextItem.ItemOrder"/>.
            /// Pokud je false, pak se nic takového neprovádí.
            /// Není dobré kombinovat s akcemi MoveUp/Down, protože nedávají smysl.
            /// </summary>
            public bool AutoSortItems { get { return DxListProperties.AutoSortItems; } set { DxListProperties.AutoSortItems = value; } }
            /// <summary>
            /// Povolené akce dané klávesami v ListBoxu.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledKeyActions { get { return DxListProperties.EnabledKeyActions; } set { DxListProperties.EnabledKeyActions = value; } }
            /// <summary>
            /// Velikost ikon
            /// </summary>
            public ResourceImageSizeType ItemSizeType { get { return DxListProperties.ItemSizeType; } set { DxListProperties.ItemSizeType = value; } }
            /// <summary>
            /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
            /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
            /// </summary>
            public string DataExchangeCurrentControlId { get { return DxListProperties.DataExchangeCurrentControlId; } set { DxListProperties.DataExchangeCurrentControlId = value; } }
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
            public DataExchangeCrossType DataExchangeCrossType { get { return DxListProperties.DataExchangeCrossType; } set { DxListProperties.DataExchangeCrossType = value; } }
            /// <summary>
            /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
            /// </summary>
            public string DataExchangeAcceptSourceControlId { get { return DxListProperties.DataExchangeAcceptSourceControlId; } set { DxListProperties.DataExchangeAcceptSourceControlId = value; } }
            /// <summary>
            /// Souhrn povolených akcí Drag and Drop
            /// </summary>
            public DxDragDropActionType DragDropActions { get { return DxListProperties.DragDropActions; } set { DxListProperties.DragDropActions = value; } }
            /// <summary>
            /// UndoRedoEnabled List má povoleny akce Undo a Redo?
            /// </summary>
            public bool UndoRedoEnabled { get { return DxListProperties.UndoRedoEnabled; } set { DxListProperties.UndoRedoEnabled = value; } }
            /// <summary>
            /// Controller UndoRedo.
            /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
            /// Pokud je povoleno, je zde vždy instance. 
            /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
            /// </summary>
            public UndoRedoController UndoRedoController { get { return DxListProperties.UndoRedoController; } set { DxListProperties.UndoRedoController = value; } }
            /// <summary>
            /// Controller ToolTipu
            /// </summary>
            public DxToolTipController DxToolTipController { get { return DxListProperties.DxToolTipController; } }

            //  -----------------   Panel :

            /// <summary>
            /// Varianta řádkového filtru. Default = None
            /// </summary>
            public RowFilterBoxMode RowFilterMode { get { return __Owner.RowFilterMode; } set { __Owner.RowFilterMode = value; } }
            /// <summary>
            /// Typy dostupných tlačítek.
            /// <para/>
            /// Nadeklarujme zde jednotlivá tlačítka v požadovaném pořadí.
            /// Pokud bude definice obsahovat vícekrát jedno stejné tlačítko, bude fyzicky přidáno pouze jedenkrát, poprvé.
            /// <para/>
            /// Pokud jeden prvek v tomto poli bude obsahovat více hodnot (jde o Flags), budou jednotlivé buttony přidány postupně v jejich nativním pořadí.
            /// </summary>
            public ControlKeyActionType[] ButtonsTypes { get { return __Owner.ButtonsTypes; } set { __Owner.ButtonsTypes = value; } }
            /// <summary>
            /// Povolené akce dané buttony. Buttony přidává Panel, o nich ListBox netuší. Proto se mu externě dodává pole povolených akcí od Buttonů, aby ListBox věděl, co může provádět za akce.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledButtonsActions { get { return DxListProperties.EnabledButtonsActions; } set { DxListProperties.EnabledButtonsActions = value; } }
            /// <summary>
            /// Umístění tlačítek
            /// </summary>
            public ToolbarPosition ButtonsPosition { get { return __Owner.ButtonsPosition; } set { __Owner.ButtonsPosition = value; } }
            /// <summary>
            /// Velikost tlačítek
            /// </summary>
            public ResourceImageSizeType ButtonsSize { get { return __Owner.ButtonsSize; } set { __Owner.ButtonsSize = value; } }
            #endregion
            #region MenuItems
            /// <summary>
            /// Prvky Listu typované na <see cref="ITextItem"/>.
            /// Pokud v Listu budou obsaženy jiné prvky než <see cref="ITextItem"/>, pak na jejich místě v tomto poli bude null.
            /// Toto pole má stejný počet prvků jako pole this.Items
            /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
            /// </summary>
            public ITextItem[] MenuItems { get { return DxListProperties.MenuItems; } set { DxListProperties.MenuItems = value; } }
            /// <summary>
            /// Obsahuje pole prvků, které jsou aktuálně Selected. 
            /// Lze setovat. Setování nastaví stav Selected na těch prvcích this.Items, které jsou Object.ReferenceEquals() shodné s některým dodaným prvkem. Ostatní budou not selected.
            /// </summary>
            public ITextItem[] SelectedMenuItems { get { return DxListProperties.SelectedMenuItems; } set { DxListProperties.SelectedMenuItems = value; } }
            /// <summary>
            /// Aktuálně vybraný prvek typu <see cref="ITextItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
            /// </summary>
            public ITextItem SelectedMenuItem { get { return DxListProperties.SelectedMenuItem; } set { DxListProperties.SelectedMenuItem = value; } }
            /// <summary>
            /// Pole aktuálně dostupných položek z hlediska filtrování.
            /// <para/>
            /// Pole obsahuje pouze ty položky z <see cref="MenuItems"/>, které jsou aktuálně <b>vyhovující řádkovému filtru</b>.<br/>
            /// Prvky v tomto poli MAJÍ naplněny hodnoty <see cref="DxListBoxNative.ListMenuItemInfo.AbsoluteIndex"/> a <see cref="DxListBoxNative.ListMenuItemInfo.MenuItem"/> a <see cref="DxListBoxNative.ListMenuItemInfo.FilteredIndex"/>.<br/>
            /// Prvky v tomto poli NEMAJÍ naplněny hodnoty <see cref="DxListBoxNative.ListMenuItemInfo.DisplayedIndex"/> a <see cref="DxListBoxNative.ListMenuItemInfo.Bounds"/>.<br/>
            /// Pole není null. Může mít 0 prvků.
            /// </summary>
            public DxListBoxNative.ListMenuItemInfo[] FilteredMenuItems { get { return DxListProperties.FilteredMenuItems; } }
            /// <summary>
            /// Pole nyní aktuálně viditelných prvků = aktuálně <b>zafiltrované</b> pomocí klientského filtru a <b>ve viditelné oblasti ListBoxu</b>.
            /// </summary>
            public DxListBoxNative.ListMenuItemInfo[] CurrentVisibleMenuItems { get { return DxListProperties.CurrentVisibleMenuItems; } }
            #endregion
            #region Table + Template
            /// <summary>
            /// Tabulka s daty
            /// </summary>
            public System.Data.DataTable DataTable { get { return DxListProperties.DataTable; } set { DxListProperties.DataTable = value; } }
            /// <summary>
            /// Šablona pro zobrazení dat z <see cref="DataTable"/>
            /// </summary>
            public DxListBoxTemplate DxTemplate { get { return DxListProperties.DxTemplate; } set { DxListProperties.DxTemplate = value; } }
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
            {
                return DxListProperties.CreateSimpleDxTemplate(columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize);
            }
            #endregion
            #region Akce = metody
            /// <summary>
            /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="ControlKeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
            /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
            /// </summary>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            /// <param name="actions"></param>
            public void DoKeyActions(DxItemsChangeType changeType, params ControlKeyActionType[] actions)
            {
                DxListProperties.DoKeyActions(changeType, actions);
            }
            /// <summary>
            /// Metoda zajistí přesunutí označených prvků na danou pozici.
            /// Pokud je zadaná pozice 0, pak jsou prvky přemístěny v jejich pořadí úplně na začátek Listu.
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou přemístěny za první prvek.
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou prvky přemístěny na konec listu.
            /// </summary>
            /// <param name="targetIndex"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void MoveSelectedItems(int? targetIndex, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.MoveSelectedItems(targetIndex, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje aktuální pozicí vybraného prvku.
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="atCurrentIndex">Požadavek true = na pozici aktuálního prvku / false = na konec</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, bool atCurrentIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.InsertItems(sourceItems, atCurrentIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou pozicí <paramref name="insertAbsoluteIndex"/>.<br/>
            /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.<br/>
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.<br/>
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
            /// <para/>
            /// Pozor: daná pozice <paramref name="insertAbsoluteIndex"/> se vztahuje k prvkům pole <see cref="MenuItems"/> = veškeré prvky, nikoliv <see cref="CurrentVisibleMenuItems"/> = aktuálně zafiltrované prvky!
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="insertAbsoluteIndex">Absolutní index, kam do Items insertujeme první dodanou položku: 0=bude první na začátku, 1=bude za první existující, (Items.Count -1) = bude před poslední, Items.Count nebo null = ude Add na konec.</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, int? insertAbsoluteIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.InsertItems(sourceItems, insertAbsoluteIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Z this Listu odebere prvky na daných indexech. Index jsou absolutní, nikoli v rámci Visible (filtrovaných) prvků.
            /// </summary>
            /// <param name="removeAbsoluteIndexes"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveIndexes(IEnumerable<int> removeAbsoluteIndexes, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.RemoveIndexes(removeAbsoluteIndexes, changeType);
            }
            /// <summary>
            /// Z this Listu odebere všechny dané prvky
            /// </summary>
            /// <param name="removeItems"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveItems(IEnumerable<ITextItem> removeItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.RemoveItems(removeItems, changeType);
            }
            /// <summary>
            /// Vyprázdní obsah řádkového filtru
            /// </summary>
            public void RowFilterClear() { __Owner.RowFilterClear(); }
            #endregion
            #region Eventy
            /// <summary>
            /// Proběhne po jednoduchém kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseClick { add { DxListProperties.ItemMouseClick += value; } remove { DxListProperties.ItemMouseClick -= value; } }
            /// <summary>
            /// Proběhne po double kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick { add { DxListProperties.ItemMouseDoubleClick += value; } remove { DxListProperties.ItemMouseDoubleClick -= value; } }
            /// <summary>
            /// Proběhne po stisku klávesy Enter na prvku
            /// </summary>
            public event DxListBoxItemKeyDelegate ItemEnterKeyDown { add { DxListProperties.ItemEnterKeyDown += value; } remove { DxListProperties.ItemEnterKeyDown -= value; } }
            /// <summary>
            /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
            /// </summary>
            public event PaintEventHandler PaintList { add { DxListProperties.PaintList += value; } remove { DxListProperties.PaintList -= value; } }
            /// <summary>
            /// Událost vyvolaná před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
            /// </summary>
            public event DxListBoxActionCancelDelegate ListActionBefore { add { DxListProperties.ListActionBefore += value; } remove { DxListProperties.ListActionBefore -= value; } }
            /// <summary>
            /// Událost vyvolaná po provedení kteréhokoli požadavku
            /// </summary>
            public event DxListBoxActionDelegate ListActionAfter { add { DxListProperties.ListActionAfter += value; } remove { DxListProperties.ListActionAfter -= value; } }
            /// <summary>
            /// Událost volaná po změně selected prvků.<br/>
            /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
            /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
            /// </summary>
            public event EventHandler SelectedItemsChanged { add { DxListProperties.SelectedItemsChanged += value; } remove { DxListProperties.SelectedItemsChanged -= value; } }
            /// <summary>
            /// Proběhne po změně v poli <see cref="MenuItems"/>
            /// </summary>
            public event System.ComponentModel.ListChangedEventHandler ListItemsChanged { add { DxListProperties.ListItemsChanged += value; } remove { DxListProperties.ListItemsChanged -= value; } }
            /// <summary>
            /// Událost volaná po změně prvků Listu typu MenuItems.<br/>
            /// </summary>
            public event DxListBoxMenuItemsChangedDelegate MenuItemsChanged { add { DxListProperties.MenuItemsChanged += value; } remove { DxListProperties.MenuItemsChanged -= value; } }
            /// <summary>
            /// Po změně stavu Undo/Redo
            /// </summary>
            public event EventHandler UndoRedoEnabledChanged { add { DxListProperties.UndoRedoEnabledChanged += value; } remove { DxListProperties.UndoRedoEnabledChanged -= value; } }

            #endregion
            #region Řádkový filtr typu server
            /// <summary>
            /// Instance serverového řádkového filtru
            /// </summary>
            public DxFilterBox RowFilterServer { get { return __Owner.RowFilterServer; } }
            /// <summary>
            /// Aktuální text v řádkovém filtru
            /// </summary>
            public DxFilterBoxValue RowFilterServerText { get { return __Owner.RowFilterServerText; } set { __Owner.RowFilterServerText = value; } }
            /// <summary>
            /// Pole operátorů nabízených pod tlačítkem vlevo.
            /// Pokud bude vloženo null nebo prázdné pole, pak tlačítko vlevo nebude zobrazeno vůbec, a v hodnotě FilterValue bude Operator = null.
            /// </summary>
            public List<IMenuItem> RowFilterServerOperators { get { return __Owner.RowFilterServerOperators; } set { __Owner.RowFilterServerOperators = value; } }
            /// <summary>
            /// Za jakých událostí se volá event <see cref="RowFilterServerChanged"/>
            /// </summary>
            public DxFilterBoxChangeEventSource? RowFilterServerChangedSources { get { return __Owner.RowFilterServerChangedSources; } set { __Owner.RowFilterServerChangedSources = value; } }
            /// <summary>
            /// Událost volaná po hlídané změně obsahu filtru.
            /// Argument obsahuje hodnotu filtru a druh události, která vyvolala event.
            /// Druhy události, pro které se tento event volá, lze nastavit v <see cref="RowFilterServerChangedSources"/>.
            /// </summary>
            public event EventHandler<DxFilterBoxChangeArgs> RowFilterServerChanged { add { __Owner.RowFilterServerChanged += value; } remove { __Owner.RowFilterServerChanged -= value; } }
            /// <summary>
            /// Provede se po stisku Enter v řádkovém filtru (i bez změny textu), vhodné pro řízení Focusu
            /// </summary>
            public event EventHandler RowFilterServerKeyEnter { add { __Owner.RowFilterServerKeyEnter += value; } remove { __Owner.RowFilterServerKeyEnter -= value; } }
            #endregion
        }
        #endregion
    }
    #endregion
    #region class DxListBoxControl : samotný ListBox, obsahuje potomka DevExpress.XtraEditors.ImageListBoxControl
    /// <summary>
    /// ListBoxControl s podporou pro drag and drop a reorder
    /// </summary>
    public class DxListBoxControl : DevExpress.XtraEditors.PanelControl, IDxDragDropControl, IUndoRedoControl, IDxToolTipDynamicClient, IListenerZoomChange, ISearchControlClient
    {
        #region Konstruktor
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxControl()
        {
            this._Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Initialize()
        {
            this.BorderStyle = BorderStyles.NoBorder;
            this.Size = new Size(350, 500);

            __ListBoxNative = new DxListBoxNative() { TabStop = true, TabIndex = 0 };
            __HScrollBar = new DevExpress.XtraEditors.HScrollBar();
            __HScrollBar.Opacity = 0.60f;
            __HScrollBar.ValueChanged += _HScrollBar_ValueChanged;
            this.Controls.Add(__ListBoxNative);
            this.Controls.Add(__HScrollBar);

            _HScrollCurrentState = null;
            _DoLayout(null);

            this.ClientSizeChanged += _ClientSizeChanged;
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            __ListBoxNative.Focus();
        }
        /// <summary>
        /// Změna velikosti vyvolá úpravu layoutu včetně výpočtu 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this._DoLayout(_HScrollBarRequiredState);
        }
        /// <summary>
        /// Vlastní ListBox
        /// </summary>
        public DxListBoxNative ListBoxNative { get { return __ListBoxNative; } }
        private DxListBoxNative __ListBoxNative;
        private DevExpress.XtraEditors.HScrollBar __HScrollBar;
        #endregion
        #region Layout a Dolní ScrollBar
        /// <summary>
        /// Aktuální hodnota nastavená na dolním ScrollBaru
        /// </summary>
        internal int HScrollBarValue { get { return __HScrollBar?.Value ?? 0; } }
        /// <summary>
        /// Zajistí layout ListBoxu + ScrollBaru včetně všech 
        /// </summary>
        /// <param name="requiredState"></param>
        private void _DoLayout(HScrollBarInfo requiredState)
        {
            var listBox = this.__ListBoxNative;
            var scrollBar = this.__HScrollBar;
            if (listBox is null || scrollBar is null) return;

            var currentState = _HScrollCurrentState;

            // Pokud dolní ScrollBar je nyní viditelný, ale nemá být, tak jej zhasnu hned na začátku:
            bool currentVisible = scrollBar.IsSetVisible();
            bool requiredVisible = requiredState?.Visible ?? false;
            if (currentVisible && !requiredVisible)
                scrollBar.Visible = false;

            var clientBounds = this.ClientRectangle;
            var listBounds = clientBounds;
            if (requiredVisible)
            {
                var scrH = DxComponent.ZoomToGui(12, this.DeviceDpi);
                var scrW = clientBounds.Width - (requiredState.LeftSpace + requiredState.RightSpace);
                var scrollBounds = new Rectangle(requiredState.LeftSpace, clientBounds.Height - scrH, scrW, scrH);
                __HScrollBar.Bounds = scrollBounds;
                __HScrollBar.Minimum = 0;
                __HScrollBar.Maximum = requiredState.ColumnsSumWidth;
                __HScrollBar.LargeChange = scrW;
                listBounds.Height = listBounds.Height - scrH;
            }
            __ListBoxNative.Bounds = listBounds;

            // Pokud dolní ScrollBar není viditelný, ale nemá být, tak jej zobrazím až na konci:
            if (!currentVisible && requiredVisible)
                scrollBar.Visible = true;

            // Aktuální stav si uložíme pro budoucí porovnání:
            _HScrollCurrentState = requiredState;
        }
        /// <summary>
        /// Pokud dolní Scrollbar má být vidět a není, anebo naopak nemá být vidět ale je, pak to zajistí včetně layoutu
        /// </summary>
        private void _CheckHScrollBar()
        {
            var currentState = __HScrollCurrentState;
            var requiredState = _HScrollBarRequiredState;
            if (!HScrollBarInfo.HasEqualContent(currentState, requiredState))
                _DoLayout(requiredState);
        }
        /// <summary>
        /// Aktuální souhrnné informace pro zobrazení dolního Scrollbaru. Vždy aktuálně vypočítané pro aktuální rozměr panelu, DPI a Zoom, a sadu sloupců.
        /// </summary>
        private HScrollBarInfo _HScrollBarRequiredState
        {
            get
            {
                if (DxProperties.ItemsMode != ListBoxItemsMode.MenuItems) return null;                       // HScrollBar potřebuji pouze v režimu MenuItems
                if (!DxProperties.MenuItemDrawColumns) return null;                                          //  ... a když používáme Columns

                var columnWidths = DxProperties.MenuItemColumnWidthsCurrent;                                 // GUI šířky sloupců
                var clientBounds = this.ClientRectangle;
                if (clientBounds.Width < 50 || clientBounds.Height < 50) return null;                        // Možná bychom HScrollBar potřebovali, ale není na něj místo

                var columnsSumWidth = columnWidths.Sum();                                                    // Suma šířek sloupců dle zadání, již v GUI pixelech

                var cLeftMargin = DxComponent.ZoomToGui(2, this.DeviceDpi);                                  // Šířka levého Margin v ListBoxu, v GUI pixelech
                var cImageSize = DxComponent.GetImageSize(DxProperties.ItemSizeType, true, this.DeviceDpi);  // Aktuální velikost ikony, v GUI pixelech
                var leftSpace = cLeftMargin + cImageSize.Width;

                var rightScroll = ScrollBarBase.GetVerticalScrollBarWidth();                                 // Šířka scrollbaru vpravo, v GUI pixelech, do rozmezí 10-50, budu ji rezervovat vždy!!!
                rightScroll = (rightScroll < 10 ? 10 : (rightScroll > 50 ? 50 : rightScroll));
                var rightSpace = rightScroll + 1;                                                            // Šířka prostoru vpravo, v aktuálních pixelech (to už vrací přepočtené DevExpress)

                var itemTextWidth = clientBounds.Width - (leftSpace + rightSpace);                           // Fyzická šířka prostoru uvnitř ListBoxu, kde se zobrazuje text Itemu

                var visible = (columnsSumWidth > itemTextWidth);                                             // dolní Scrollbar potřebujeme tehdy, když šířka sloupců je větší, než je prostor pro text Itemu

                return new HScrollBarInfo()
                {
                    Visible = visible,
                    ContainerClientBounds = clientBounds,
                    ListTotalWidth = clientBounds.Width,
                    LeftSpace = leftSpace,
                    RightSpace = rightSpace,
                    ItemTextWidth = itemTextWidth,
                    ColumnsSumWidth = columnsSumWidth,
                };
            }
        }
        /// <summary>
        /// Po změně pozice na ScrollBaru vyvoláme překreslení Listu = vložíme do něj aktuální hodnotu z ScrollBaru, a ListBox si ji validuje a zareaguje na změnu překreslením
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void _HScrollBar_ValueChanged(object sender, EventArgs e)
        {
            this.__ListBoxNative.DxProperties.MenuItemColumnOffset = this.__HScrollBar.Value;
        }
        /// <summary>
        /// Aktuální stav Scrollbaru. Setování do této property zajistí přenos Visible do fyzického Controlu. Nepřenáší jiné hodnoty.
        /// </summary>
        private HScrollBarInfo _HScrollCurrentState
        {
            get { return __HScrollCurrentState; }
            set { __HScrollCurrentState = value; }
        }
        private HScrollBarInfo __HScrollCurrentState;
        /// <summary>
        /// Informace pro dolní ScrollBar
        /// </summary>
        private class HScrollBarInfo
        {
            /// <summary>
            /// Vrátí true, pokud dva dodané objekty obsahují shodná data
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static bool HasEqualContent(HScrollBarInfo a, HScrollBarInfo b)
            {
                var an = a is null;
                var bn = b is null;
                if (an && bn) return true;
                if (an || bn) return false;

                return ((a.Visible == b.Visible)
                     && (a.ListTotalWidth == b.ListTotalWidth)
                     && (a.LeftSpace == b.LeftSpace)
                     && (a.RightSpace == b.RightSpace)
                     && (a.ItemTextWidth == b.ItemTextWidth)
                     && (a.ColumnsSumWidth == b.ColumnsSumWidth));
            }
            /// <summary>
            /// ScrollBar má být viditelný
            /// </summary>
            public bool Visible { get; set; }
            /// <summary>
            /// Prostor pro klientské objekty v Parentu: do této šířky se vypočítává velikost Listu a ScrollBaru
            /// </summary>
            public Rectangle ContainerClientBounds { get; set; }
            /// <summary>
            /// Celková šířka prostoru pro ListBox (v aktuálních GUI pixelech)
            /// </summary>
            public int ListTotalWidth { get; set; }
            /// <summary>
            /// Režijní prostor vlevo (pod ikonou) (v aktuálních GUI pixelech)
            /// </summary>
            public int LeftSpace { get; set; }
            /// <summary>
            /// Režijní prostor vpravo (pod pravým ScrollBarem) (v aktuálních GUI pixelech)
            /// </summary>
            public int RightSpace { get; set; }
            /// <summary>
            /// Fyzická šířka prostoru pro obsah textu (v aktuálních GUI pixelech)
            /// </summary>
            public int ItemTextWidth { get; set; }
            /// <summary>
            /// Celková šířka sloupců = šířka virtuálního obsah (v aktuálních GUI pixelech)
            /// </summary>
            public int ColumnsSumWidth { get; set; }
        }
        #endregion
        #region Implementace všech interface skrz __ListBox
        DxDragDrop IDxDragDropControl.DxDragDrop => ((IDxDragDropControl)__ListBoxNative).DxDragDrop;
        DxDragDropActionType IDxDragDropControl.DragDropActions => ((IDxDragDropControl)__ListBoxNative).DragDropActions;
        void IDxDragDropControl.DoDragSource(DxDragDropArgs args) { ((IDxDragDropControl)__ListBoxNative).DoDragSource(args); }
        void IDxDragDropControl.DoDragTarget(DxDragDropArgs args) { ((IDxDragDropControl)__ListBoxNative).DoDragTarget(args); }
        void IUndoRedoControl.DoUndoStep(object state) { ((IUndoRedoControl)__ListBoxNative).DoUndoStep(state); }
        void IUndoRedoControl.DoRedoStep(object state) { ((IUndoRedoControl)__ListBoxNative).DoRedoStep(state); }
        void IDxToolTipDynamicClient.PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs args) { ((IDxToolTipDynamicClient)__ListBoxNative).PrepareSuperTipForPoint(args); }
        void IListenerZoomChange.ZoomChanged() { ((IListenerZoomChange)__ListBoxNative).ZoomChanged(); }
        void ISearchControlClient.SetSearchControl(ISearchControl searchControl) { ((ISearchControlClient)__ListBoxNative).SetSearchControl(searchControl); }
        SearchControlProviderBase ISearchControlClient.CreateSearchProvider() { return ((ISearchControlClient)__ListBoxNative).CreateSearchProvider(); }
        void ISearchControlClient.ApplyFindFilter(SearchInfoBase searchInfo) { ((ISearchControlClient)__ListBoxNative).ApplyFindFilter(searchInfo); }
        bool ISearchControlClient.IsAttachedToSearchControl => ((ISearchControlClient)__ListBoxNative).IsAttachedToSearchControl;
        #endregion
        #region DxProperties : property + třída, která do sebe shrnuje čistě jen Nephrite vlastnosti
        /// <summary>
        /// Souhrn vlastností (data a eventy), které tato třída poskytuje systému Nephrite
        /// </summary>
        public DxPropertiesInfo DxProperties
        {
            get
            {
                if (__DxProperties is null)
                    __DxProperties = new DxPropertiesInfo(this);
                return __DxProperties;
            }
        }
        private DxPropertiesInfo __DxProperties;
        /// <summary>
        /// Třída pro Nephrite vlastnosti
        /// </summary>
        public class DxPropertiesInfo
        {
            #region Konstruktor
            internal DxPropertiesInfo(DxListBoxControl owner)
            {
                __Owner = owner;
            }
            private DxListBoxControl __Owner;
            /// <summary>
            /// Přímo instance<see cref="DxListBoxNative"/>
            /// </summary>
            private DxListBoxNative ListBoxNative { get { return __Owner.ListBoxNative; } }
            /// <summary>
            /// Přímo vlastnosti <see cref="DxListBoxNative"/>
            /// </summary>
            private DxListBoxNative.DxPropertiesInfo DxListProperties { get { return ListBoxNative.DxProperties; } }
            #endregion
            #region Vlastnosti
            /// <summary>
            /// Výška prvku
            /// </summary>
            public int ItemHeight { get { return DxListProperties.ItemHeight; } set { DxListProperties.ItemHeight = value; } }
            /// <summary>
            /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
            /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
            /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
            /// Hodnota vyšší než 10 se akceptuje jako 10.
            /// </summary>
            public int ItemHeightPadding { get { return DxListProperties.ItemHeightPadding; } set { DxListProperties.ItemHeightPadding = value; } }
            /// <summary>
            /// Režim prvků v ListBoxu.
            /// </summary>
            public ListBoxItemsMode ItemsMode { get { return DxListProperties.ItemsMode; } }
            /// <summary>
            /// Povoluje se výběr více prvků pomocí Ctrl+Mouse?
            /// </summary>
            public bool MultiSelectEnabled { get { return DxListProperties.MultiSelectEnabled; } set { DxListProperties.MultiSelectEnabled = value; } }
            /// <summary>
            /// Konkrétní režim MultiSelectu
            /// </summary>
            public SelectionMode SelectionMode { get { return DxListProperties.SelectionMode; } set { DxListProperties.SelectionMode = value; } }
            /// <summary>
            /// Gets or sets whether to arrange items across multiple columns if the ListBox cannot fit all items vertically.
            /// true to arrange items across multiple columns; otherwise, false.
            /// </summary>
            public virtual bool MultiColumn { get { return DxListProperties.MultiColumn; } set { DxListProperties.MultiColumn = value; } }
            /// <summary>
            /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
            /// </summary>
            public bool DrawImageDirectly { get { return DxListProperties.DrawImageDirectly; } set { DxListProperties.DrawImageDirectly = value; } }
            /// <summary>
            /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
            /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
            /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
            /// </summary>
            public bool DuplicityEnabled { get { return DxListProperties.DuplicityEnabled; } set { DxListProperties.DuplicityEnabled = value; } }
            /// <summary>
            /// Pokud obsahuje true, pak List si po každé změně položek zajistí Sort svých položek v <see cref="DxListBoxControl.MenuItems"/> podle hodnoty <see cref="ITextItem.ItemOrder"/>.
            /// Pokud je false, pak se nic takového neprovádí.
            /// Není dobré kombinovat s akcemi MoveUp/Down, protože nedávají smysl.
            /// </summary>
            public bool AutoSortItems { get { return DxListProperties.AutoSortItems; } set { DxListProperties.AutoSortItems = value; } }
            /// <summary>
            /// Povolené akce dané klávesami v ListBoxu.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledKeyActions { get { return DxListProperties.EnabledKeyActions; } set { DxListProperties.EnabledKeyActions = value; } }
            /// <summary>
            /// Povolené akce dané buttony. Buttony přidává Panel, o nich ListBox netuší. Proto se mu externě dodává pole povolených akcí od Buttonů, aby ListBox věděl, co může provádět za akce.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledButtonsActions { get { return DxListProperties.EnabledButtonsActions; } set { DxListProperties.EnabledButtonsActions = value; } }
            /// <summary>
            /// Velikost ikon
            /// </summary>
            public ResourceImageSizeType ItemSizeType { get { return DxListProperties.ItemSizeType; } set { DxListProperties.ItemSizeType = value; } }
            /// <summary>
            /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
            /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
            /// </summary>
            public string DataExchangeCurrentControlId { get { return DxListProperties.DataExchangeCurrentControlId; } set { DxListProperties.DataExchangeCurrentControlId = value; } }
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
            public DataExchangeCrossType DataExchangeCrossType { get { return DxListProperties.DataExchangeCrossType; } set { DxListProperties.DataExchangeCrossType = value; } }
            /// <summary>
            /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
            /// </summary>
            public string DataExchangeAcceptSourceControlId { get { return DxListProperties.DataExchangeAcceptSourceControlId; } set { DxListProperties.DataExchangeAcceptSourceControlId = value; } }
            /// <summary>
            /// Souhrn povolených akcí Drag and Drop
            /// </summary>
            public DxDragDropActionType DragDropActions { get { return DxListProperties.DragDropActions; } set { DxListProperties.DragDropActions = value; } }
            /// <summary>
            /// UndoRedoEnabled List má povoleny akce Undo a Redo?
            /// </summary>
            public bool UndoRedoEnabled { get { return DxListProperties.UndoRedoEnabled; } set { DxListProperties.UndoRedoEnabled = value; } }
            /// <summary>
            /// Controller UndoRedo.
            /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
            /// Pokud je povoleno, je zde vždy instance. 
            /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
            /// </summary>
            public UndoRedoController UndoRedoController { get { return DxListProperties.UndoRedoController; } set { DxListProperties.UndoRedoController = value; } }
            /// <summary>
            /// Controller ToolTipu
            /// </summary>
            public DxToolTipController DxToolTipController { get { return DxListProperties.DxToolTipController; } }

            /// <summary>
            /// Gets or sets the style controller component that provides style settings for this control.
            /// </summary>
            public IStyleController StyleController { get { return ListBoxNative.StyleController; } set { ListBoxNative.StyleController = value; } }
            /// <summary>
            /// Gets or sets the index of the currently selected item.
            /// </summary>
            public int SelectedIndex { get { return ListBoxNative.SelectedIndex; } set { ListBoxNative.SelectedIndex = value; } }
            /// <summary>
            /// Allows you to respond to item selection.
            /// </summary>
            public event EventHandler SelectedIndexChanged { add { ListBoxNative.SelectedIndexChanged += value; } remove { ListBoxNative.SelectedIndexChanged -= value; } }
            /// <summary>
            /// Počet položek zbylých po vyfiltrování
            /// </summary>
            public int VisibleItemsCount { get { return ListBoxNative.ItemCount; } }
            /// <summary>
            /// Počet položek označených
            /// </summary>
            public int SelectedItemsCount { get { return ListBoxNative.SelectedIndices.Count; } }
            /// <summary>
            /// Aktuálně označené objekty. Může jich být i více, nebo žádný.
            /// Objekty to mohou být různé, typicky <see cref="ITextItem"/> nebo <see cref="System.Data.DataRowView"/>.
            /// ID označených řádků je v poli <see cref="SelectedItemsId"/>.
            /// </summary>
            public object[] SelectedItems { get { return ListBoxNative.SelectedItems; } }
            /// <summary>
            /// Pole obsahující ID selectovaných záznamů.
            /// </summary>
            public object[] SelectedItemsId { get { return ListBoxNative.SelectedItemsId; } }
            /// <summary>
            /// Gets or sets the index of the currently selected item.
            /// </summary>
            public int CurrentItem { get { return ListBoxNative.SelectedIndex; } set { ListBoxNative.SelectedIndex = value; } }
            /// <summary>
            /// Gets or sets the index of the currently selected item.
            /// </summary>
            public int CurrentItemId { get { return ListBoxNative.SelectedIndex; } set { ListBoxNative.SelectedIndex = value; } }

            /// <summary>
            /// Gets or sets whether item height is automatically calculated to fit item contents. In auto-height mode, different items may have different heights.
            /// </summary>
            public bool ItemAutoHeight { get { return ListBoxNative.ItemAutoHeight; } set { ListBoxNative.ItemAutoHeight = value; } }
            /// <summary>
            /// Provides access to the collection of context buttons displayed in the control.
            /// </summary>
            public ContextItemCollection ContextButtons { get { return ListBoxNative.ContextButtons; } }
            /// <summary>
            /// Gets or sets whether a horizontal scroll bar is enabled when the control’s items are displayed in a single column and some of them exceed the control’s width.
            /// </summary>
            public bool HorizontalScrollbar { get { return ListBoxNative.HorizontalScrollbar; } set { ListBoxNative.HorizontalScrollbar = value; } }
            /// <summary>
            /// Gets or sets the offset by which the control is scrolled horizontally when the
            /// scrollbar buttons are clicked. This property is in effect in single column display
            /// mode, when the BaseListBoxControl.HorizontalScrollbar property is set to true.
            /// </summary>
            public int HorzScrollStep { get { return ListBoxNative.HorzScrollStep; } set { ListBoxNative.HorzScrollStep = value; } }

            #endregion
            #region MenuItems
            /// <summary>
            /// Prvky Listu typované na <see cref="ITextItem"/>.
            /// <para/>
            /// Jde o <b>všechny přítomné prvky Listu</b>: pokud bude aplikován klientský řádkový filtr, pak zde budou stále všechny prvky!<br/>
            /// POkud na některé pozici nebude prvek typu <see cref="ITextItem"/>, pak na jeho pozici je zde NULL.<br/>
            /// Pro získání pouze Zobrazitelných prvků listu je třeba načíst pole <see cref="CurrentVisibleMenuItems"/>.
            /// <para/>
            /// Pokud v Listu budou obsaženy jiné prvky než <see cref="ITextItem"/>, pak na jejich místě v tomto poli bude null.
            /// Toto pole má stejný počet prvků jako pole this.Items
            /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
            /// </summary>
            public ITextItem[] MenuItems { get { return DxListProperties.MenuItems; } set { DxListProperties.MenuItems = value; __Owner._CheckHScrollBar(); } }
            /// <summary>
            /// Obsahuje true, pokud aktuální ListBox je v režimu <see cref="ListBoxItemsMode.MenuItems"/> a má zadané šířky sloupců do <see cref="MenuItemColumnWidths"/>, pak tedy pracuje v režimu více buněk
            /// </summary>
            public bool MenuItemDrawColumns { get { return DxListProperties.MenuItemDrawColumns; } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>
            /// </summary>
            public int[] MenuItemColumnWidths { get { return DxListProperties.MenuItemColumnWidths; } set { DxListProperties.MenuItemColumnWidths = value; __Owner._CheckHScrollBar(); } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>, validované, aktuální platné dle Zoomu a DPI, namísto případných záporných hodnot obsahuje 0.
            /// </summary>
            public int[] MenuItemColumnWidthsCurrent { get { return DxListProperties.MenuItemColumnWidthsCurrent; } }
            /// <summary>
            /// Aktuálně vybraný prvek typu <see cref="ITextItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
            /// </summary>
            public ITextItem SelectedMenuItem { get { return DxListProperties.SelectedMenuItem; } set { DxListProperties.SelectedMenuItem = value; } }
            /// <summary>
            /// Aktuálně SELECTOVANÉ prvky <see cref="ITextItem"/>
            /// </summary>
            public ITextItem[] SelectedMenuItems { get { return DxListProperties.SelectedMenuItems; } set { DxListProperties.SelectedMenuItems = value; } }
            /// <summary>
            /// Pole aktuálně dostupných položek z hlediska filtrování.
            /// <para/>
            /// Pole obsahuje pouze ty položky z <see cref="MenuItems"/>, které jsou aktuálně <b>vyhovující řádkovému filtru</b>.<br/>
            /// Prvky v tomto poli MAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.AbsoluteIndex"/> a <see cref="ListMenuItemInfo.MenuItem"/> a <see cref="ListMenuItemInfo.FilteredIndex"/>.<br/>
            /// Prvky v tomto poli NEMAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.DisplayedIndex"/> a <see cref="ListMenuItemInfo.Bounds"/>.<br/>
            /// Pole není null. Může mít 0 prvků.
            /// </summary>
            public DxListBoxNative.ListMenuItemInfo[] FilteredMenuItems { get { return DxListProperties.FilteredMenuItems; } }
            /// <summary>
            /// Pole nyní aktuálně viditelných prvků = aktuálně <b>zafiltrované</b> pomocí klientského filtru a <b>ve viditelné oblasti ListBoxu</b>.
            /// </summary>
            public DxListBoxNative.ListMenuItemInfo[] CurrentVisibleMenuItems { get { return DxListProperties.CurrentVisibleMenuItems; } }
            #endregion
            #region Table + Template
            /// <summary>
            /// Tabulka s daty
            /// </summary>
            public System.Data.DataTable DataTable { get { return DxListProperties.DataTable; } set { DxListProperties.DataTable = value; __Owner._CheckHScrollBar(); } }
            /// <summary>
            /// Šablona pro zobrazení dat z <see cref="DataTable"/>
            /// </summary>
            public DxListBoxTemplate DxTemplate { get { return DxListProperties.DxTemplate; } set { DxListProperties.DxTemplate = value; __Owner._CheckHScrollBar(); } }
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
            { return DxListProperties.CreateSimpleDxTemplate(columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize); }
            /// <summary>
            /// Stores HTML and CSS-based templates used to render listbox items.
            /// </summary>
            public DevExpress.Utils.Html.HtmlTemplateCollection HtmlTemplates { get { return ListBoxNative.HtmlTemplates; } }
            /// <summary>
            /// Stores regular item templates used to render listbox items.
            /// </summary>
            public ItemTemplateRepositoryBase Templates { get { return ListBoxNative.Templates; } }
            /// <summary>
            /// Gets or sets the data source that provides items to display in the control.
            /// </summary>
            public object DataSource { get { return ListBoxNative.DataSource; } set { ListBoxNative.DataSource = value; __Owner._CheckHScrollBar(); } }
            #endregion
            #region Akce = metody
            /// <summary>
            /// Zajistí, že položka na daném indexu bude ve viditelné oblasti
            /// </summary>
            /// <param name="absoluteIndex"></param>
            public void MakeItemVisible(int absoluteIndex)
            {
                DxListProperties.MakeItemVisible(absoluteIndex);
            }
            /// <summary>
            /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="ControlKeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
            /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
            /// </summary>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            /// <param name="actions"></param>
            public void DoKeyActions(DxItemsChangeType changeType, params ControlKeyActionType[] actions)
            {
                DxListProperties.DoKeyActions(changeType, actions);
            }
            /// <summary>
            /// Metoda zajistí přesunutí označených prvků na danou pozici.
            /// Pokud je zadaná pozice 0, pak jsou prvky přemístěny v jejich pořadí úplně na začátek Listu.
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou přemístěny za první prvek.
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou prvky přemístěny na konec listu.
            /// </summary>
            /// <param name="targetIndex"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void MoveSelectedItems(int? targetIndex, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.MoveSelectedItems(targetIndex, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje aktuální pozicí vybraného prvku.
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="atCurrentIndex">Požadavek true = na pozici aktuálního prvku / false = na konec</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, bool atCurrentIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.InsertItems(sourceItems, atCurrentIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou pozicí <paramref name="insertAbsoluteIndex"/>.<br/>
            /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.<br/>
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.<br/>
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
            /// <para/>
            /// Pozor: daná pozice <paramref name="insertAbsoluteIndex"/> se vztahuje k prvkům pole <see cref="MenuItems"/> = veškeré prvky, nikoliv <see cref="CurrentVisibleMenuItems"/> = aktuálně zafiltrované prvky!
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="insertAbsoluteIndex">Absolutní index, kam do Items insertujeme první dodanou položku: 0=bude první na začátku, 1=bude za první existující, (Items.Count -1) = bude před poslední, Items.Count nebo null = ude Add na konec.</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, int? insertAbsoluteIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.InsertItems(sourceItems, insertAbsoluteIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Z this Listu odebere prvky na daných indexech. Index jsou absolutní, nikoli v rámci Visible (filtrovaných) prvků.
            /// </summary>
            /// <param name="removeAbsoluteIndexes"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveIndexes(IEnumerable<int> removeAbsoluteIndexes, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.RemoveIndexes(removeAbsoluteIndexes, changeType);
            }
            /// <summary>
            /// Z this Listu odebere všechny dané prvky
            /// </summary>
            /// <param name="removeItems"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveItems(IEnumerable<ITextItem> removeItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                DxListProperties.RemoveItems(removeItems, changeType);
            }
            /// <summary>
            /// Unselects all items when multiple item selection is enabled.
            /// </summary>
            public void UnSelectAll()
            {
                ListBoxNative.UnSelectAll();
            }
            #endregion
            #region Eventy
            /// <summary>
            /// Proběhne po jednoduchém kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseClick { add { DxListProperties.ItemMouseClick += value; } remove { DxListProperties.ItemMouseClick -= value; } }
            /// <summary>
            /// Proběhne po double kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick { add { DxListProperties.ItemMouseDoubleClick += value; } remove { DxListProperties.ItemMouseDoubleClick -= value; } }
            /// <summary>
            /// Proběhne po stisku klávesy Enter na prvku
            /// </summary>
            public event DxListBoxItemKeyDelegate ItemEnterKeyDown { add { DxListProperties.ItemEnterKeyDown += value; } remove { DxListProperties.ItemEnterKeyDown -= value; } }
            /// <summary>
            /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
            /// </summary>
            public event PaintEventHandler PaintList { add { DxListProperties.PaintList += value; } remove { DxListProperties.PaintList -= value; } }
            /// <summary>
            /// Událost vyvolaná před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
            /// </summary>
            public event DxListBoxActionCancelDelegate ListActionBefore { add { DxListProperties.ListActionBefore += value; } remove { DxListProperties.ListActionBefore -= value; } }
            /// <summary>
            /// Událost vyvolaná po provedení kteréhokoli požadavku
            /// </summary>
            public event DxListBoxActionDelegate ListActionAfter { add { DxListProperties.ListActionAfter += value; } remove { DxListProperties.ListActionAfter -= value; } }
            /// <summary>
            /// Událost volaná po změně selected prvků.<br/>
            /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
            /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
            /// </summary>
            public event EventHandler SelectedItemsChanged { add { DxListProperties.SelectedItemsChanged += value; } remove { DxListProperties.SelectedItemsChanged -= value; } }
            /// <summary>
            /// Proběhne po změně v poli <see cref="MenuItems"/>
            /// </summary>
            public event System.ComponentModel.ListChangedEventHandler ListItemsChanged { add { DxListProperties.ListItemsChanged += value; } remove { DxListProperties.ListItemsChanged -= value; } }
            /// <summary>
            /// Událost volaná po změně prvků Listu typu MenuItems.<br/>
            /// </summary>
            public event DxListBoxMenuItemsChangedDelegate MenuItemsChanged { add { DxListProperties.MenuItemsChanged += value; } remove { DxListProperties.MenuItemsChanged -= value; } }
            /// <summary>
            /// Po změně stavu Undo/Redo
            /// </summary>
            public event EventHandler UndoRedoEnabledChanged { add { DxListProperties.UndoRedoEnabledChanged += value; } remove { DxListProperties.UndoRedoEnabledChanged -= value; } }
            /// <summary>
            /// Allows you to customize templated items dynamically.
            /// </summary>
            public event CustomizeTemplatedItemEventHandler CustomizeItem { add { ListBoxNative.CustomizeItem += value; } remove { ListBoxNative.CustomizeItem -= value; } }
            /// <summary>
            /// Enables custom display text to be provided for control items.
            /// </summary>
            public event CustomItemDisplayTextEventHandler CustomItemDisplayText { add { ListBoxNative.CustomItemDisplayText += value; } remove { ListBoxNative.CustomItemDisplayText -= value; } }
            /// <summary>
            /// Allows you to assign custom templates to listbox items.
            /// </summary>
            public event CustomItemTemplateEventHandler CustomItemTemplate { add { ListBoxNative.CustomItemTemplate += value; } remove { ListBoxNative.CustomItemTemplate -= value; } }
            #endregion
        }
        #endregion
    }
    /// <summary>
    /// ListBoxControl s podporou pro drag and drop a reorder, ale bez ScrollBaru pro Columns
    /// </summary>
    public class DxListBoxNative : DevExpress.XtraEditors.ImageListBoxControl, IDxDragDropControl, IUndoRedoControl, IDxToolTipDynamicClient, IListenerZoomChange
    {
        #region Public členy
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxNative()
        {
            _ItemClickInit();
            _KeyActionsInit();
            _DataExchangeInit();
            _DxDragDropInit(DxDragDropActionType.None);
            _ToolTipInit();
            OnImageInit();
            DuplicityEnabled = true;
            ItemSizeType = ResourceImageSizeType.Small;
            _ApplyDesignHeight();
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
        /// Výška prvku, fyzická hodnota v ListBoxu
        /// </summary>
        public override int ItemHeight
        {
            get { return base.ItemHeight; }
            set { base.ItemHeight = value; }
        }
        /// <summary>
        /// Výška prvku, Design hodnota (na ní se aplikuje Zoom a DPI)
        /// </summary>
        public int ItemHeightDesign
        {
            get { return __ItemHeightDesign; }
            set { bool isChanged = (value != __ItemHeightDesign); __ItemHeightDesign = value; if (isChanged) _ApplyDesignHeight(); }
        }
        private int __ItemHeightDesign = 0;
        /// <summary>
        /// Přídavek k výšce jednoho řádku ListBoxu v Design pixelech = Design hodnota (na ní se aplikuje Zoom a DPI).
        /// <para/>
        /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
        /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPaddingDesign"/>.
        /// Hodnota vyšší než 10 se akceptuje jako 10.
        /// </summary>
        protected int ItemHeightPaddingDesign
        {
            get { return __ItemHeightPaddingDesign; }
            set { bool isChanged = (value != __ItemHeightPaddingDesign); __ItemHeightPaddingDesign = value; if (isChanged) _ApplyDesignHeight(); }
        }
        private int __ItemHeightPaddingDesign = 0;
        /// <summary>
        /// Aplikuje hodnoty <see cref="ItemHeightDesign"/> a <see cref="ItemHeightPaddingDesign"/> plus DPI a ZOOM do <see cref="ItemHeight"/>
        /// </summary>
        private void _ApplyDesignHeight()
        {
            var itemHeightPaddingGui = DxComponent.ZoomToGui(__ItemHeightPaddingDesign, this.DeviceDpi);
            var itemHeightGui = DxComponent.ZoomToGui(__ItemHeightDesign, this.DeviceDpi);

            if (itemHeightPaddingGui > 0)
            {
                int padding = (itemHeightPaddingGui > 10 ? 10 : itemHeightPaddingGui);
                int fontheight = this.Appearance.GetFont().Height;
                this.ItemAutoHeight = false;
                this.ItemHeight = fontheight + (2 * padding);
            }
            else if (itemHeightGui > 0)
            {
                this.ItemAutoHeight = false;
                this.ItemHeight = itemHeightGui;
            }
            else
            {
                this.ItemAutoHeight = true;
            }
        }
        #endregion
        #region Zoom a DPI
        void IListenerZoomChange.ZoomChanged() { OnDpiZoomChanged(); }
        /// <summary>
        /// Po změně Zoomu / DPI resetujeme...
        /// </summary>
        protected override void OnScaleDpiChanged()
        {
            base.OnScaleDpiChanged();
            OnDpiZoomChanged();
        }
        /// <summary>
        /// Po změně Zoomu a DPI
        /// </summary>
        protected virtual void OnDpiZoomChanged()
        {
            ImageSizeCacheReset();
            _ApplyDesignHeight();
        }
        #endregion
        #region Data = položky, a layout = Template
        /// <summary>
        /// Režim prvků v ListBoxu.
        /// </summary>
        protected ListBoxItemsMode ItemsMode { get { return _ItemsMode; } }
        #region Jednoduchý List postavený nad položkami ITextItem
        /// <summary>
        /// Prvky Listu typované na <see cref="ITextItem"/>.
        /// <para/>
        /// Jde o <b>všechny přítomné prvky Listu</b>: pokud bude aplikován klientský řádkový filtr, pak zde budou stále všechny prvky!<br/>
        /// POkud na některé pozici nebude prvek typu <see cref="ITextItem"/>, pak na jeho pozici je zde NULL.<br/>
        /// Pro získání pouze Zobrazitelných prvků listu je třeba načíst pole <see cref="CurrentVisibleMenuItems"/>.
        /// <para/>
        /// Pokud v Listu budou obsaženy jiné prvky než <see cref="ITextItem"/>, pak na jejich místě v tomto poli bude null.
        /// Toto pole má stejný počet prvků jako pole this.Items
        /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
        /// </summary>
        protected ITextItem[] MenuItems
        {
            get
            {
                return (_ItemsMode == ListBoxItemsMode.MenuItems ? this.Items.Select(i => i.Value as ITextItem).ToArray() : null);
            }
            set
            {
                this.DataSource = null;
                _InvalidateFilteredItems();

                if (value != null)
                {
                    // Pouze nonduplicitní prvky, setříděné podle Order - vše podle nastavených parametrů:
                    var validItems = _GetOnlyValidItems(value, false);
                    if (AutoSortItems)
                        validItems = _GetSortedItems(validItems);

                    _ItemsMode = ListBoxItemsMode.MenuItems;
                    _StoreItemsSilent(validItems);
                    _SetItemsAsPrevious(validItems);
                }
                else
                {
                    this.Items.Clear();
                    _ItemsMode = ListBoxItemsMode.None;
                }
            }
        }
        /// <summary>
        /// Prvky Listu v páru: <see cref="ImageListBoxItem"/> a <see cref="ITextItem"/>.
        /// <para/>
        /// Jde o <b>všechny přítomné prvky Listu</b>: pokud bude aplikován klientský řádkový filtr, pak zde budou stále všechny prvky!<br/>
        /// POkud na některé pozici nebude prvek typu <see cref="ITextItem"/>, pak na jeho pozici je zde NULL.<br/>
        /// Pro získání pouze Zobrazitelných prvků listu je třeba načíst pole <see cref="CurrentVisibleMenuItems"/>.
        /// <para/>
        /// Pokud v Listu budou obsaženy jiné prvky než <see cref="ITextItem"/>, pak na jejich místě v tomto poli bude null.
        /// Toto pole má stejný počet prvků jako pole this.Items
        /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
        /// </summary>
        protected Tuple<ImageListBoxItem, ITextItem>[] MenuDblItems
        {
            get
            {
                return (_ItemsMode == ListBoxItemsMode.MenuItems ? this.Items.Select(i => new Tuple<ImageListBoxItem, ITextItem>(i, i.Value as ITextItem)).ToArray() : null);
            }
        }
        /// <summary>
        /// Vrátí pole připravených instancí <see cref="ImageListBoxItem"/> z dodaných <see cref="ITextItem"/>,
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        protected ImageListBoxItem[] CreateListBoxItems(IEnumerable<ITextItem> menuItem)
        {
            return menuItem.Select(mi => CreateListBoxItem(mi)).ToArray();
        }
        /// <summary>
        /// Vrátí instanci <see cref="ImageListBoxItem"/> z dodané <see cref="ITextItem"/>,
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        protected ImageListBoxItem CreateListBoxItem(ITextItem menuItem)
        {
            var lbxItem = new ImageListBoxItem()
            {
                Value = menuItem,
                Description = menuItem.Text,
                Tag = menuItem
            };
            DxComponent.ApplyImage(lbxItem.ImageOptions, menuItem.ImageName, menuItem.Image);
            return lbxItem;
        }
        /// <summary>
        /// Obsahuje true, pokud jsme v režimu 
        /// </summary>
        private bool _NeedSortItems { get { return (this._ItemsMode == ListBoxItemsMode.MenuItems && this.AutoSortItems); } }
        /// <summary>
        /// Účelem metody je zajistit, že tento <see cref="DxListBoxNative"/> bude mít setříděné položky v <c>Items</c>, pokud tomu tak má být.
        /// Metoda se má volat po jakémkoli přidání položek (protože po odebrání položek není třeba data třídit), a po změně hodnoty <see cref="AutoSortItems"/>.
        /// </summary>
        private void _CheckSortedMenuItems()
        {
            if (!_NeedSortItems) return;
            var menuItems = this.MenuItems;
            if (menuItems.Length <= 1) return;

            var selectedItems = this.SelectedMenuItems;

            var sortedItems = _GetSortedItems(menuItems);
            _StoreItemsSilent(sortedItems);

            this.SelectedMenuItems = selectedItems;
        }
        /// <summary>
        /// Vrátí dodané pole setříděné podle hodnoty <see cref="ITextItem.ItemOrder"/>.
        /// Tato metoda neřeší podmínku <see cref="_NeedSortItems"/> ani <see cref="AutoSortItems"/>, prostě vrátí setříděné pole.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private ITextItem[] _GetSortedItems(ITextItem[] items)
        {
            if (items is null || items.Length <= 1) return items;              // Není co třídit

            int n = 0;
            var list = items.Select(t => new Tuple<int, int, ITextItem>(t.ItemOrder, n++, t)).ToList();
            list.Sort((a, b) => comparer(a, b));
            return list.Select(t => t.Item3).ToArray();


            // Comparer podle (Item1 ASC, Item2 ASC)   ==>   ItemOrder, a v případě shodné hodnoty pak nativní pořadí prvku ve vstupním soupisu:
            static int comparer(Tuple<int, int, ITextItem> a, Tuple<int, int, ITextItem> b)
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                if (cmp == 0) cmp = a.Item2.CompareTo(b.Item2);
                return cmp;
            }
        }
        /// <summary>
        /// Do this ListBoxu vloží dodané Items, pokud možno nenápadně
        /// </summary>
        /// <param name="items"></param>
        private void _StoreItemsSilent(ITextItem[] items)
        {
            var hasRowFilter = HasRowFilter;
            var rowFilter = RowFilterCondition;
            if (hasRowFilter) RowFilterCondition = null;

            using (this.ScopeSuspendParentLayout())
            {
                var scrollInfo = this.ViewInfo.ScrollInfo;
                var hscv = this.ViewInfo.ScrollInfo.HScroll.Value;
                this.Items.Clear();
                this.Items.AddRange(CreateListBoxItems(items));
                if (hasRowFilter) RowFilterCondition = rowFilter;

                this.ViewInfo.ScrollInfo.HScroll.Value = hscv;
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
        private ITextItem[] _GetOnlyValidItems(IEnumerable<ITextItem> items, bool withCurrentList)
        {
            List<ITextItem> validItems = new List<ITextItem>();
            if (items != null)
            {
                bool duplicityEnabled = this.DuplicityEnabled;
                Dictionary<string, ITextItem> itemIdDict = (duplicityEnabled ? null :
                      (withCurrentList ? this.MenuItems.Where(i => i.ItemId != null).CreateDictionary(i => i.ItemId, true) :
                      new Dictionary<string, ITextItem>()));
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
        /// <summary>
        /// Metoda zkusí najít a vrátit <see cref="ITextItem"/> z dodaného prvku ListBoxu
        /// </summary>
        /// <param name="item"></param>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private bool _TryFindMenuItem(object item, out ITextItem menuItem)
        {
            if (_ItemsMode == ListBoxItemsMode.MenuItems && item != null)
            {
                if (item is DevExpress.XtraEditors.Controls.ImageListBoxItem listItem && listItem.Value is ITextItem menuItem1)
                {
                    menuItem = menuItem1;
                    return true;
                }
                if (item is ITextItem menuItem2)
                {
                    menuItem = menuItem2;
                    return true;
                }
            }
            menuItem = null;
            return false;
        }
        /// <summary>
        /// Po změně v prvcích Items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListItemsChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            // Pokud aktuálně nejsme v režimu MenuItems, a přitom máme položky v poli this.Items, pak se do režimu MenuItems přepneme nyní:
            //  Někdo asi vložil položky přímo do nativního soupisu...
            if (_ItemsMode != ListBoxItemsMode.MenuItems && this.Items.Count > 0)
            {
                _ItemsMode = ListBoxItemsMode.MenuItems;
                if (__DataTable != null) __DataTable = null;
                _InvalidateFilteredItems();
            }
            _RunItemsListChanged(e);
        }
        /// <summary>
        /// Metoda zkusí najít prvek <see cref="ITextItem"/> pro zadaný absolutní index.<br/>
        /// Jde o párovou metodu k <see cref="_TryGetAbsoluteIndexOfMenuItem(ITextItem, out int?)"/>.
        /// <para/>
        /// Tyto dvě metody slouží k nalezení prvku a jeho indexu na základě prvního výskytu datového prvku <see cref="ITextItem"/> (v poli <see cref="MenuItems"/>).
        /// Upozornění: v tomto poli se může vyskytovat jedna instance <see cref="ITextItem"/> na více pozicích (pokud jsou povoleny duplicity), takže tato sada metod nemusí vracet jednoznačné výsledky.
        /// Vhodnější metody pro jednoznačné vyhledání jsou <see cref="_TryGetListItemAtAbsoluteIndex(int?, out ImageListBoxItem)"/> a <see cref="_TryGetAbsoluteIndexOfListItem(ImageListBoxItem, out int?)"/>.
        /// </summary>
        /// <param name="absoluteIndex"></param>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        private bool _TryGetMenuItemAtAbsoluteIndex(int? absoluteIndex, out ITextItem menuItem)
        {
            if (this.ItemsMode == ListBoxItemsMode.MenuItems && absoluteIndex.HasValue && absoluteIndex.Value >= 0)
            {
                var menuItems = this.MenuItems;
                if (absoluteIndex.Value < menuItems.Length)
                {
                    var item = menuItems[absoluteIndex.Value];
                    if (item != null)
                    {
                        menuItem = item;
                        return true;
                    }
                }
            }
            menuItem = null;
            return false;
        }
        /// <summary>
        /// Metoda zkusí najít absolutní index pro daný prvek <see cref="ITextItem"/>.<br/>
        /// Jde o párovou metodu k <see cref="_TryGetMenuItemAtAbsoluteIndex(int?, out ITextItem)"/>.
        /// <para/>
        /// Tyto dvě metody slouží k nalezení prvku a jeho indexu na základě prvního výskytu datového prvku <see cref="ITextItem"/> (v poli <see cref="MenuItems"/>).
        /// Upozornění: v tomto poli se může vyskytovat jedna instance <see cref="ITextItem"/> na více pozicích (pokud jsou povoleny duplicity), takže tato sada metod nemusí vracet jednoznačné výsledky.
        /// Vhodnější metody pro jednoznačné vyhledání jsou <see cref="_TryGetListItemAtAbsoluteIndex(int?, out ImageListBoxItem)"/> a <see cref="_TryGetAbsoluteIndexOfListItem(ImageListBoxItem, out int?)"/>.
        /// </summary>
        /// <param name="menuItem"></param>
        /// <param name="absoluteIndex"></param>
        /// <returns></returns>
        private bool _TryGetAbsoluteIndexOfMenuItem(ITextItem menuItem, out int? absoluteIndex)
        {
            if (this.ItemsMode == ListBoxItemsMode.MenuItems && menuItem != null)
            {
                var menuItems = this.MenuItems;
                if (menuItems.TryFindFirstIndex(i => Object.ReferenceEquals(i, menuItem), out var index))
                {
                    absoluteIndex = index;
                    return true;
                }
            }
            absoluteIndex = null;
            return false;
        }
        /// <summary>
        /// Metoda zkusí najít prvek <see cref="ImageListBoxItem"/> pro zadaný absolutní index..<br/>
        /// Jde o párovou metodu k <see cref="_TryGetAbsoluteIndexOfListItem(ImageListBoxItem, out int?)"/>.
        /// <para/>
        /// Tyto dvě metody slouží k nalezení prvku a jeho indexu jednoznačně přes celý List, a to i v situaci, kdy List obsahuje ve více svých položkách (Items) tentýž objekt <see cref="ITextItem"/> (v poli <see cref="MenuItems"/>).
        /// K tomu dochází tehdy, když do Target Listu opakovaně vkládáme jednu položku ze Source listu = tedy pokud josu např. povoleny duplicity.
        /// Pak není možné jednoznačně najít index prvku podle objektu <see cref="IMenuItem"/>, protože vyhledá první nalezenou instanci z několika přítomných.
        /// </summary>
        /// <param name="absoluteIndex"></param>
        /// <param name="listItem"></param>
        /// <returns></returns>
        private bool _TryGetListItemAtAbsoluteIndex(int? absoluteIndex, out ImageListBoxItem listItem)
        {
            if (this.ItemsMode == ListBoxItemsMode.MenuItems && absoluteIndex.HasValue && absoluteIndex.Value >= 0)
            {
                var listItems = this.Items;
                if (absoluteIndex.Value < listItems.Count)
                {
                    var item = listItems[absoluteIndex.Value];
                    if (item != null)
                    {
                        listItem = item;
                        return true;
                    }
                }
            }
            listItem = null;
            return false;
        }
        /// <summary>
        /// Metoda zkusí najít absolutní index pro daný prvek <see cref="ImageListBoxItem"/>.<br/>
        /// Jde o párovou metodu k <see cref="_TryGetListItemAtAbsoluteIndex(int?, out ImageListBoxItem)"/>.
        /// <para/>
        /// Tyto dvě metody slouží k nalezení prvku a jeho indexu jednoznačně přes celý List, a to i v situaci, kdy List obsahuje ve více svých položkách (Items) tentýž objekt <see cref="ITextItem"/> (v poli <see cref="MenuItems"/>).
        /// K tomu dochází tehdy, když do Target Listu opakovaně vkládáme jednu položku ze Source listu = tedy pokud josu např. povoleny duplicity.
        /// Pak není možné jednoznačně najít index prvku podle objektu <see cref="IMenuItem"/>, protože vyhledá první nalezenou instanci z několika přítomných.
        /// </summary>
        /// <param name="listItem"></param>
        /// <param name="absoluteIndex"></param>
        /// <returns></returns>
        private bool _TryGetAbsoluteIndexOfListItem(ImageListBoxItem listItem, out int? absoluteIndex)
        {
            if (this.ItemsMode == ListBoxItemsMode.MenuItems && listItem != null)
            {
                var listItems = this.Items;
                var listCount = listItems.Count;
                for (int index = 0; index < listCount; index++)
                {
                    if (Object.ReferenceEquals(listItems[index], listItem))
                    {
                        absoluteIndex = index;
                        return true;
                    }
                }
            }
            absoluteIndex = null;
            return false;
        }
        #region SelectedMenuItems, FilteredItems, CurrentVisibleItems
        /*     Popis indexů a metod:

Další otázka: pořadový index prvku v poli imageListBox.ViewInfo.VisibleItems je počítán od nuly pro první aktuálně viditelný prvek v Listu? Tedy pokud List obsahuje mnoho prvků a je odscrollováno na např. 12. viditelný prvek, pak v ViewInfo.VisibleItems je na pozici [0]?
Uveď do souvislosti:
Pozice prvku v poli Items;
Do Listu je aplikován řádkový filtr;
Vstupní a Výstupní hodnota metody int GetVisibleIndex(int);
Vstupní hodnota metody GetItemBounds(int);
Obsah pole imageListBox.ViewInfo.VisibleItems z hlediska pozice prvku v této kolekci a vztah k předchozím indexům



Scénář:
Items má 20 prvků (indexy 0-19)
Filtr skryje prvky: 2, 5, 7, 15, 18 (zůstane 15 viditelných filtrovaných prvků)
Filtrované Items: 0, 1, 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19
ListBox je scrollován tak, že 5. filtrovaný prvek (Items[8]) je první viditelný na obrazovce
Tabulka vztahů:
Code
┌──────────────────┬────────────────────────────┬──────────────┬──────────────────┐
│ Pozice v Items   │ Filtrován? (Search Filter) │ GetVisibleIdx│ ViewInfo.Visible │
│ (absolutní)      │                            │ Output       │ Items Index      │
├──────────────────┼────────────────────────────┼──────────────┼──────────────────┤
│ 0                │ ✓ Viditelný filtrací       │ 0            │ -                │
│ 1                │ ✓ Viditelný filtrací       │ 1            │ -                │
│ 2                │ ✗ Skrytý filtrem           │ -1           │ -                │
│ 3                │ ✓ Viditelný filtrací       │ 2            │ -                │
│ 4                │ ✓ Viditelný filtrací       │ 3            │ -                │
│ 5                │ ✗ Skrytý filtrem           │ -1           │ -                │
│ 6                │ ✓ Viditelný filtrací       │ 4            │ -                │
│ 8                │ ✓ Viditelný filtrací       │ 5            │ [0] ← SCROLL!    │
│ 9                │ ✓ Viditelný filtrací       │ 6            │ [1]              │
│ 10               │ ✓ Viditelný filtrací       │ 7            │ [2]              │
│ 11               │ ✓ Viditelný filtrací       │ 8            │ [3]              │
│ 12               │ ✓ Viditelný filtrací       │ 9            │ [4]              │
│ 13               │ ✓ Viditelný filtrací       │ 10           │ [5]              │
│ 14               │ ✓ Viditelný filtrací       │ 11           │ -                │
│ 16               │ ✓ Viditelný filtrací       │ 12           │ -                │
│ 17               │ ✓ Viditelný filtrací       │ 13           │ -                │
│ 19               │ ✓ Viditelný filtrací       │ 14           │ -                │
└──────────────────┴────────────────────────────┴──────────────┴──────────────────┘
Konkrétní příklady:
Příklad 1: Chci informace o prvku na Items[8]
C#
int itemIndex = 8;  // Pozice v Items

// Krok 1: Je filtrován?
int visibleIndex = imageListBox.GetVisibleIndex(itemIndex);  // Vrátí 5

if (visibleIndex >= 0)
{
    // Je viditelný (vyhovuje filtru)
    // visibleIndex = 5 znamená, že je to 6. filtrovaný prvek (počítáno od 0)
    
    // Krok 2: Jaké jsou jeho souřadnice na obrazovce?
    Rectangle bounds = imageListBox.GetItemBounds(visibleIndex);  // Input: 5
    
    // Krok 3: Je fyzicky viditelný na obrazovce (v ViewInfo.VisibleItems)?
    bool isPhysicallyVisible = imageListBox.ViewInfo.VisibleItems.Contains(
        imageListBox.Items[itemIndex]
    );
    // V našem scénáři = true, protože je na pozici [0] v VisibleItems
}
Příklad 2: Iterace přes fyzicky viditelné prvky
C#
// ViewInfo.VisibleItems obsahuje POUZE fyzicky viditelné prvky na obrazovce
// V našem scénáriu: Items[8], Items[9], Items[10], Items[11], Items[12], Items[13]

foreach (var visibleItem in imageListBox.ViewInfo.VisibleItems)
{
    // Získat absolutní index v Items
    int itemIndex = imageListBox.Items.IndexOf(visibleItem);
    
    // Získat vizuální index (filtrovaný index)
    int visibleIdx = imageListBox.GetVisibleIndex(itemIndex);
    
    // GetItemBounds potřebuje vizuální index!
    Rectangle bounds = imageListBox.GetItemBounds(visibleIdx);
}
Shrnutí hierarchie indexů:
Code
┌─────────────────────────────────────┐
│ Items[absoluteIndex]                │  ← Vstup pro GetVisibleIndex()
│ (0-19 v našem příkladu)             │
└──────────────┬──────────────────────┘
               │ GetVisibleIndex(absoluteIndex)
               ↓
┌─────────────────────────────────────┐
│ visibleIndex (filtrovaný index)     │  ← Výstup GetVisibleIndex()
│ (0-14 v našem příkladu)             │     Vstup pro GetItemBounds()
│ -1 = skrytý filtrem                 │
└──────────────┬──────────────────────┘
               │ GetItemBounds(visibleIndex)
               ↓
┌─────────────────────────────────────┐
│ Rectangle bounds                    │  ← Fyzické souřadnice na obrazovce
│ (pokud je fyzicky viditelný)        │
└─────────────────────────────────────┘
               
              Nezávisle:
               │
               ↓
┌─────────────────────────────────────┐
│ ViewInfo.VisibleItems[0..n]         │  ← POUZE fyzicky viditelné
│ Počítáno od prvního viditelného     │     (scrolling ovlivňuje!)
│ na obrazovce                        │
└─────────────────────────────────────┘
Odpověď na vaši původní otázku:
Ano! ViewInfo.VisibleItems[0] je vždy první fyzicky viditelný prvek na obrazovce, bez ohledu na to, zda je to Items[0] nebo Items[100]. Indexy v VisibleItems se počítají od nuly pro první viditelný prvek v okně.

Konkrétně v našem scénáři:

VisibleItems[0] = Items[8] (visibleIndex = 5)
VisibleItems[1] = Items[9] (visibleIndex = 6)
atd.
Jasné? 👍


        ----------------------------


Metoda ListBox.SetSelected(int, bool) má na vstupu index: absolutní anebo vizuální?
Copilot said: Výborná otázka! **`SetSelected(int, bool)` má na vstupu
Výborná otázka! SetSelected(int, bool) má na vstupu absolutní index (index v Items poli).

Praktický příklad:
C#
// Máme Items s 20 prvky
// Filtr skryje prvky: 2, 5, 7

// SPRÁVNĚ - absolutní index
imageListBox.SetSelected(8, true);   // Vybere prvek Items[8]

// Pokud bychom chtěli vybrat pomocí vizuálního indexu:
int itemIndex = 8;
int visibleIndex = imageListBox.GetVisibleIndex(itemIndex);

// GetVisibleIndex vrátí např. 5 (je to 6. filtrovaný prvek)
// ALE: SetSelected stále potřebuje absolutní index!
imageListBox.SetSelected(itemIndex, true);  // ✓ Správně
imageListBox.SetSelected(visibleIndex, true);  // ✗ ŠPATNĚ!
Rozdíl mezi metodami vstupu:
Metoda	Vstup
SetSelected(int, bool)	Absolutní index (Items)
GetVisibleIndex(int)	Absolutní index → Vrací vizuální
GetItemBounds(int)	Vizuální index
SelectedIndices	Absolutní indexy (Items)
Praktický příklad - výběr prvku ze VisibleItems:
C#
// Máte prvek z ViewInfo.VisibleItems a chcete jej vybrat
var visibleItem = imageListBox.ViewInfo.VisibleItems[0];

// Najít absolutní index
int absoluteIndex = imageListBox.Items.IndexOf(visibleItem);

// Nyní správně vybrat
imageListBox.SetSelected(absoluteIndex, true);  // ✓ Správně
Shrnutí:
SetSelected() pracuje s absolutními indexy - stejně jako SelectedIndices či Items[].

Pokud máte vizuální index a potřebujete vybrat prvek, musíte si sám vyhledat odpovídající absolutní index! 👍


        -------------------------------------------

  Co                             Typ indexu
SelectedIndices[]               Absolutní (Items)
Items[]                         Absolutní
GetVisibleIndex() - vstup       Absolutní
GetVisibleIndex() - výstup      Vizuální
GetItemBounds() - vstup         Vizuální
SetSelected() - vstup           Absolutní

        */
        /// <summary>
        /// Aktuálně SELECTOVANÉ prvky <see cref="ITextItem"/>
        /// </summary>
        protected ITextItem[] SelectedMenuItems
        {
            get
            {
                return this.SelectedMenuInfos.Select(i => i.MenuItem).Where(m => m != null).ToArray();
            }
            set
            {
                var selectedIndexes = new List<int>();
                var selectedItems = value;
                if (selectedItems != null && selectedItems.Length > 0)
                {
                    var menuItems = this.MenuItems;
                    for (int i = 0; i < menuItems.Length; i++)
                    {   // Projdu naše Itemy, a pokud v poli k výběru bude tento prvek uveden,
                        // pak zdejší absolutní index prvku přidám do selectedIndexes,
                        // a tyto prvky pak Selectujeme:
                        if (selectedItems.Any(s => Object.ReferenceEquals(s, menuItems[i])))
                            selectedIndexes.Add(i);
                    }
                }
                this.SelectedAbsoluteIndexes = selectedIndexes.ToArray();
            }
        }
        /// <summary>
        /// Aktuálně vybraný prvek typu <see cref="ITextItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
        /// </summary>
        protected ITextItem SelectedMenuItem
        {
            get
            {   // Vrátím ITextItem nalezený v aktuálně vybraném prvku:
                return ((_ItemsMode == ListBoxItemsMode.MenuItems && this.Items.Count > 0 && _TryFindMenuItem(this.SelectedItem, out var menuItem)) ? menuItem : null);
            }
            set
            {   // Najdu první prvek zdejšího pole, který v sobě obsahuje ITextItem, který je identický s dodanou value:
                if (_ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    object selectedItem = null;
                    if (this.Items.Count > 0 && value != null)
                        selectedItem = this.Items.FirstOrDefault(i => (_TryFindMenuItem(i, out var iMenuItem) && Object.ReferenceEquals(iMenuItem, value)));
                    this.SelectedItem = selectedItem;
                }
            }
        }
        /// <summary>
        /// Pole informací o Selected prvcích.
        /// <para/>
        /// Pole obsahuje pouze ty položky z <see cref="MenuItems"/>, které jsou aktuálně Selected podle <see cref="SelectedAbsoluteIndexes"/>.<br/>
        /// Prvky v tomto poli MAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.AbsoluteIndex"/> a <see cref="ListMenuItemInfo.MenuItem"/> a <see cref="ListMenuItemInfo.FilteredIndex"/>.<br/>
        /// Prvky v tomto poli NEMAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.DisplayedIndex"/> a <see cref="ListMenuItemInfo.Bounds"/>.<br/>
        /// Pole není null. Může mít 0 prvků.
        /// </summary>
        protected ListMenuItemInfo[] SelectedMenuInfos
        {
            get
            {
                var selIdxs = this.SelectedAbsoluteIndexes;
                var menuDblItems = this.MenuDblItems;
                var result = new List<ListMenuItemInfo>();
                foreach (var absoluteIndex in selIdxs)
                {
                    if (absoluteIndex >= 0 && absoluteIndex < menuDblItems.Length)
                    {
                        var filteredIndex = this.GetFilteredIndexFromAbsolute(absoluteIndex);
                        result.Add(new ListMenuItemInfo(absoluteIndex, menuDblItems[absoluteIndex], filteredIndex));
                    }
                }
                return result.ToArray();
            }
        }
        /// <summary>
        /// Absolutní indexy prvků, které jsou Selected = jde o absolutní indexy!  
        /// Lze setovat; Selected budou ty existující prvky, jejichž hodnota bude dodána.
        /// </summary>
        protected int[] SelectedAbsoluteIndexes
        {
            // AI píše, že SelectedIndices i SetSelected() pracují s absolutním indexem...
            // Pravdou ale je, že SelectedIndices a SetSelected() pracují s vizuálním indexem. Ono to dává víc smyslu...
            get
            {
                var filteredIndexes = SelectedFilteredIndexes;                           // Aktuální Selected prvky, jejich vizuální Filtered indexy
                return GetAbsoluteIndexesFromFiltered(filteredIndexes);                  // Konvertujeme Filtered => Absolute a vrátíme
            }
            set
            {
                var filteredIndexes = GetFilteredIndexesFromAbsolute(value);             // Z dodaných Absolute indexů vyhledám Filtered indexy
                SelectedFilteredIndexes = filteredIndexes;                               // A ty Filtered indexy nastavím jako Selected
            }
        }
        /// <summary>
        /// Vizuální indexy prvků, které jsou Selected = jde o indexy v rámci zafiltovaných prvků!  
        /// Lze setovat; Selected budou ty existující prvky, jejichž hodnota bude dodána.
        /// </summary>
        protected int[] SelectedFilteredIndexes
        {
            // AI píše, že SelectedIndices i SetSelected() pracují s absolutním indexem...
            // Pravdou ale je, že SelectedIndices a SetSelected() pracují s vizuáoním indexem. Ono to dává víc smyslu...
            get
            {   // Vracíme Filtered = přímo SelectedIndices:
                return this.SelectedIndices.ToArray();
            }
            set
            {   // Procházíme Filtered items, a nastavujeme SetSelected pro vizuální Item podle jeh opřítomnosti v filteredKeys = value.
                var filteredKeys = value?.CreateDictionary(i => i, true) ?? new Dictionary<int, int>();
                var count = this.ItemCount;
                for (int i = 0; i < count; i++)
                    this.SetSelected(i, filteredKeys.ContainsKey(i));
            }
        }
        /// <summary>
        /// Absolutní index aktivního prvku
        /// </summary>
        protected int? CurrentAbsoluteIndex
        {
            get
            {
                var listItem = base.SelectedItem;
                var index = this.Items.IndexOf(listItem);                // Items pracují s Absolutním indexem
                return (index >= 0 ? index : null);
            }
            set
            {
                var listItem = (value.HasValue && value.Value >= 0 && value.Value < this.Items.Count ? this.Items[value.Value] : null);
                base.SelectedItem = listItem;                            // base.SelectedItem ve výsledku setuje SelectedIndex!
                base.MakeItemVisible(base.SelectedIndex);
            }
        }
        /// <summary>
        /// Filtrovaný index aktivního prvku
        /// </summary>
        protected int? CurrentFilteredIndex
        {
            get
            {
                var filteredItems = this.FilteredMenuItems;
                var listItem = base.SelectedItem;
                if (filteredItems.TryGetFirst(i => Object.ReferenceEquals(i, listItem), out var filtItem))
                    return filtItem.FilteredIndex;
                return null;
            }
            set
            {
                var filteredItems = this.FilteredMenuItems;
                if (value.HasValue && filteredItems.TryGetFirst(i => (i.FilteredIndex.HasValue && i.FilteredIndex.Value == value.Value), out var filtItem))
                {
                    base.SelectedItem = filtItem.ListBoxItem;        // base.SelectedItem ve výsledku setuje SelectedIndex!
                    base.MakeItemVisible(base.SelectedIndex);
                }
            }
        }
        /// <summary>
        /// Zajistí, že položka na daném absolutním indexu bude ve viditelné oblasti
        /// </summary>
        /// <param name="absoluteIndex"></param>
        protected void MakeItemVisibleAtAbsoluteIndex(int absoluteIndex)
        {
            var filteredIndex = this.GetFilteredIndexFromAbsolute(absoluteIndex);
            if (filteredIndex.HasValue)
                this.MakeItemVisible(filteredIndex.Value);
        }
        /// <summary>
        /// Pole aktuálně dostupných položek z hlediska filtrování.
        /// <para/>
        /// Pole obsahuje pouze ty položky z <see cref="MenuItems"/>, které jsou aktuálně <b>vyhovující řádkovému filtru</b>.<br/>
        /// Prvky v tomto poli MAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.AbsoluteIndex"/> a <see cref="ListMenuItemInfo.MenuItem"/> a <see cref="ListMenuItemInfo.FilteredIndex"/>.<br/>
        /// Prvky v tomto poli NEMAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.DisplayedIndex"/> a <see cref="ListMenuItemInfo.Bounds"/>.<br/>
        /// Pole není null. Může mít 0 prvků.
        /// </summary>
        protected ListMenuItemInfo[] FilteredMenuItems { get { _CheckFilteredItems(); return __FilteredMenuItems; } }
        /// <summary>
        /// Pole prvků, které jsou zafiltrovány
        /// </summary>
        private ListMenuItemInfo[] __FilteredMenuItems;
        /// <summary>
        /// Pole nyní aktuálně viditelných prvků = aktuálně <b>zafiltrované</b> pomocí klientského filtru a <b>ve viditelné oblasti ListBoxu</b>.
        /// </summary>
        protected ListMenuItemInfo[] CurrentVisibleMenuItems
        {
            get
            {
                var visibleItems = new List<ListMenuItemInfo>();
                var menuDblItems = this.MenuDblItems;
                var viewItems = this.ViewInfo?.VisibleItems;
                if (viewItems != null)
                {
                    foreach (DevExpress.XtraEditors.ViewInfo.ImageListBoxViewInfo.ImageItemInfo viewItem in viewItems)
                    {
                        var listItem = viewItem.Item;
                        int absoluteIndex = this.Items.IndexOf(listItem);
                        var menuDblItem = (absoluteIndex >= 0 && absoluteIndex < menuDblItems.Length ? menuDblItems[absoluteIndex] : null);
                        var filteredIndex = this.GetFilteredIndexFromAbsolute(absoluteIndex);
                        Rectangle? bounds = (filteredIndex.HasValue ? GetItemBounds(filteredIndex.Value, true) : null);
                        visibleItems.Add(new ListMenuItemInfo(absoluteIndex, menuDblItem, filteredIndex, visibleItems.Count, bounds));
                    }
                }
                return visibleItems.ToArray();
            }
        }
        /// <summary>
        /// Vrátí pole obsahující Filtrovaný index (na němž bude prvek zobrazen po zafiltrování) pro daný Absolutní index. 
        /// Vrátí prázdné pole, pokud index je mimo rozsah anebo daný prvek není zobrazitelný.
        /// </summary>
        /// <param name="absoluteIndexes"></param>
        /// <returns></returns>
        protected int[] GetFilteredIndexesFromAbsolute(IEnumerable<int> absoluteIndexes)
        {
            var filteredIndexes = new List<int>();
            if (absoluteIndexes != null)
            {
                var filteredItems = this.FilteredMenuItems;
                if (filteredItems != null && filteredItems.Length > 0)
                {
                    foreach (var absoluteIndex in absoluteIndexes)
                    {
                        if (filteredItems.TryFindFirst(out var foundInfo, i => i.AbsoluteIndex == absoluteIndex && i.FilteredIndex.HasValue))
                            filteredIndexes.Add(foundInfo.FilteredIndex.Value);
                    }
                }
            }
            return filteredIndexes.ToArray();
        }
        /// <summary>
        /// Vrátí Filtrovaný index (na němž bude prvek zobrazen po zafiltrování) pro daný Absolutní index. Vrátí null, pokud index je mimo rozsah anebo daný prvek není zobrazitelný.
        /// </summary>
        /// <param name="absoluteIndex"></param>
        /// <returns></returns>
        protected int? GetFilteredIndexFromAbsolute(int absoluteIndex)
        {
            if (absoluteIndex >= 0)
            {
                var filteredItems = this.FilteredMenuItems;
                if (filteredItems != null && filteredItems.Length > 0 && filteredItems.TryFindFirst(out var foundInfo, i => i.AbsoluteIndex == absoluteIndex && i.FilteredIndex.HasValue))
                    return foundInfo.FilteredIndex;
            }
            return null;
        }
        /// <summary>
        /// Vrátí pole obsahující Absolutní index pro index Filtrovaný = vizuální. 
        /// Vrátí prázdné pole, pokud index je mimo rozsah.
        /// </summary>
        /// <param name="filteredIndexes"></param>
        /// <returns></returns>
        protected int[] GetAbsoluteIndexesFromFiltered(IEnumerable<int> filteredIndexes)
        {
            var absoluteIndexes = new List<int>();
            if (filteredIndexes != null)
            {
                var filteredItems = this.FilteredMenuItems;
                if (filteredItems != null && filteredItems.Length > 0)
                {
                    foreach (var filteredIndex in filteredIndexes)
                    {
                        if (filteredItems.TryFindFirst(out var foundInfo, i => i.FilteredIndex.HasValue && i.FilteredIndex.Value == filteredIndex))
                            absoluteIndexes.Add(foundInfo.AbsoluteIndex);
                    }
                }
            }
            return absoluteIndexes.ToArray();
        }
        /// <summary>
        /// Vrátí Absolutní index pro index Filtrovaný = vizuální. Vrátí null, pokud index je mimo rozsah.
        /// </summary>
        /// <param name="filteredIndex"></param>
        /// <returns></returns>
        protected int? GetAbsoluteIndexFromFiltered(int filteredIndex)
        {
            if (filteredIndex >= 0)
            {
                var filteredItems = this.FilteredMenuItems;
                if (filteredItems != null && filteredItems.Length > 0 && filteredItems.TryFindFirst(out var foundInfo, i => i.FilteredIndex.HasValue && i.FilteredIndex.Value == filteredIndex))
                    return foundInfo.AbsoluteIndex;
            }
            return null;
        }
        /// <summary>
        /// Informace o prvku
        /// </summary>
        public class ListMenuItemInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="absoluteIndex"></param>
            /// <param name="menuDblItem"></param>
            /// <param name="filteredIndex"></param>
            /// <param name="displayedIndex"></param>
            /// <param name="bounds"></param>
            public ListMenuItemInfo(int absoluteIndex, Tuple<ImageListBoxItem, ITextItem> menuDblItem, int? filteredIndex = null, int? displayedIndex = null, Rectangle? bounds = null)
            {
                AbsoluteIndex = absoluteIndex;
                ListBoxItem = menuDblItem.Item1;
                MenuItem = menuDblItem.Item2;
                FilteredIndex = filteredIndex;
                DisplayedIndex = displayedIndex;
                Bounds = bounds;
            }
            /// <summary>
            /// Stringová reprezentace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Text;
            }
            /// <summary>
            /// DisplayText
            /// </summary>
            public string Text { get { return $"[{AbsoluteIndex}]: '{MenuItem?.Text}'"; } }
            /// <summary>
            /// Absolutní index = pozice v poli Items.
            /// </summary>
            public int AbsoluteIndex { get; private set; }
            /// <summary>
            /// Nativní Item v ListBoxu
            /// </summary>
            public ImageListBoxItem ListBoxItem { get; private set; }
            /// <summary>
            /// Data prvku. Pokud na této pozici není <see cref="ITextItem"/>, je zde null.
            /// </summary>
            public ITextItem MenuItem { get; private set; }
            /// <summary>
            /// Text prvku tak jak je filtrován: protože Search control generuje filtr s odkazem na property jménem 'Column', tak tímto názvem se bude odkazuvat na zde uvedený text...
            /// </summary>
            public string Column { get { return this.MenuItem.Text; } }
            /// <summary>
            /// Index v seznamu po aplikování filtru. Pokud je zde null, pak tento prvek nemůže být zobrazen.
            /// Zde je umožněno setování hodnoty do 
            /// </summary>
            public int? FilteredIndex { get; set; }
            /// <summary>
            /// Pozice prvku v rámci aktuálně fyzicky zobrazených prvků v ListBoxu; první pixel má index 0. Nicméně nad ním mohou být předchozí prvky, které aktuálně nejsou vidět, a zde mají null, ale mají <see cref="FilteredIndex"/> zadané.
            /// </summary>
            public int? DisplayedIndex { get; set; }
            /// <summary>
            /// Fyzické souřadnice, pokud je prvek ve viditelné oblasti Listu
            /// </summary>
            public Rectangle? Bounds { get; set; }
        }
        #endregion
        #region Spolupráce s klientským řádkovým filtrem
        /// <summary>
        /// Obsahuje true, pokud máme aktivní řádkový filtr. 
        /// Ten je uložen v <see cref="__RowFilterCriteria"/>.
        /// Vkládá se tam pomocí metody <see cref="ApplyItemsFilter(CriteriaOperator)"/>.
        /// </summary>
        protected bool HasRowFilter { get { return !(__RowFilterCriteria is null); } }
        /// <summary>
        /// Filtrační podmínka. Lze setovat.
        /// </summary>
        protected CriteriaOperator RowFilterCondition { get { return __RowFilterCriteria; } set { ApplyItemsFilter(value); } }
        /// <summary>
        /// Po změně obsahu klientského řádkového filtru invaliduje pole vyfiltrovaných řádků v komponentě.
        /// Nastavuje aktivitu filtru: true = je třeba brát ohled na filtr / false = všechny položky Listu jsou platné / null = neměnit hodnotu (volající neví o stavu filtru).
        /// </summary>
        /// <param name="criteria">Aktuálně platný filtr</param>
        private void _InvalidateFilteredItems(CriteriaOperator criteria)
        {
            __FilteredMenuItems = null;
            __RowFilterCriteria = criteria;
        }
        /// <summary>
        /// Po změně obsahu klientského řádkového filtru invaliduje pole vyfiltrovaných řádků v komponentě.
        /// Nastavuje aktivitu filtru: true = je třeba brát ohled na filtr / false = všechny položky Listu jsou platné / null = neměnit hodnotu (volající neví o stavu filtru).
        /// </summary>
        private void _InvalidateFilteredItems()
        {
            __FilteredMenuItems = null;
        }
        /// <summary>
        /// Zajistí, že pole <see cref="__FilteredMenuItems"/> bude obsahovat not null platné filtrované položky
        /// </summary>
        private void _CheckFilteredItems()
        {
            if (__FilteredMenuItems is null)
            {
                ListMenuItemInfo[] filteredItems = null;
                if (_ItemsMode == ListBoxItemsMode.MenuItems)
                {
                    var menuDblItems = this.MenuDblItems;                     // = ListItem + ITextItem
                    var rowFilter = __RowFilterCriteria;
                    var filterIsActive = !(rowFilter is null);
                    if (filterIsActive)
                    {   // Máme filtr => vytvoříme prvotní soupis všech prvků včetně Absolute indexu:
                        int absIdx = 0;
                        var absMenuItems = menuDblItems.Select(i => new ListMenuItemInfo(absIdx++, i)).ToList();          // Absolutní index 0++; a všechny MenuItems
                        // Vytvoříme evaluator pro typ ListMenuItemInfo, a pro CriteriaOperator: rowFilter
                        var evaluator = new DevExpress.Data.Filtering.Helpers.ExpressionEvaluator(System.ComponentModel.TypeDescriptor.GetProperties(typeof(ListMenuItemInfo)), rowFilter, false);
                        // Vytvoříme pole prvků, které vyhovují zadané podmínce (ale pole stále nemá naplěné hodnoty FilteredIndex):
                        filteredItems = evaluator.Filter(absMenuItems).OfType<ListMenuItemInfo>().ToArray();
                        // Nyní prostě projdu pole filteredItems (které nyní osbahuje jen filtrované prvky) a do jednotlivých objektů vepíšu postupně FilteredIndex:
                        int visIdx = 0;
                        filteredItems.ForEachExec(i => i.FilteredIndex = visIdx++);
                    }
                    else
                    {   // Nemáme filtr => všechny MenuItems jsou Filtered:
                        int absIdx = 0;
                        int visIdx = 0;
                        filteredItems = menuDblItems.Select(i => new ListMenuItemInfo(absIdx++, i, visIdx++)).ToArray();     // Absolutní index 0++; a všechny MenuItems, a FilteredIndex++
                    }
                }
                else
                {
                    filteredItems = new ListMenuItemInfo[0];
                }
                __FilteredMenuItems = filteredItems;
            }
        }
        /// <summary>
        /// Aktuálně platný řádkový filtr, který byl naposledy aplikován
        /// </summary>
        private CriteriaOperator __RowFilterCriteria;
        /// <summary>
        /// Volá ListBox vždy, když z objektu Search je sestavena filtrační podmínka a ta je vložena do ListBoxu
        /// </summary>
        /// <param name="criteria"></param>
        protected override void ApplyItemsFilter(CriteriaOperator criteria)
        {
            base.ApplyItemsFilter(criteria);
            _InvalidateFilteredItems(criteria);
        }
        /// <summary>
        /// Volá ListBox vždy, když z objektu Search je sestavena filtrační podmínka a ta je vložena do ListBoxu
        /// </summary>
        /// <param name="criteria"></param>
        protected override void ApplyInlineSearch(CriteriaOperator criteria)
        {
            base.ApplyInlineSearch(criteria);
            _InvalidateFilteredItems(criteria);
        }
        #endregion
        #region CurrentItems a PreviousItems: položky ukládané do vedlejšího registru, pro zajištění detekce reálné změny položek
        /// <summary>
        /// Vrátí new instanci pole items, aktuálně nyní vytvořenou kopií z pole <see cref="MenuItems"/>.
        /// Obsahuje tedy všechny prvky (i neviditelné po aplikování filtru), ale jde o new instanci pole.
        /// Toto pole lze porovnat metodou <see cref="_IsCurrentItemsChanged(ITextItem[])"/> s privátně uloženou instanci pole (uložené pomocí metody <see cref="_SetItemsAsPrevious(ITextItem[])"/>).
        /// Obecně slouží k porovnání, zda aktuální Current stav (zde získaný) je změněný oproti stavu uloženému dříve, a po změně pak vyvoláme event <see cref="MenuItemsChanged"/>.
        /// </summary>
        /// <returns></returns>
        private ITextItem[] _GetCurrentItems()
        {
            return CloneArray(this.MenuItems);
        }
        /// <summary>
        /// Vrátí true, pokud dodané aktuální pole se liší od pole uloženého dříve pomocí metody <see cref="_SetItemsAsPrevious(ITextItem[])"/>.
        /// </summary>
        /// <param name="currentItems"></param>
        /// <returns></returns>
        private bool _IsCurrentItemsChanged(ITextItem[] currentItems)
        {
            var previousItems = __PreviousMenuItems;

            var cn = currentItems is null;
            var pn = previousItems is null;
            if (cn && pn) return false;                    // null == null              není změna
            if (cn || pn) return true;                     // null a not null           je změna

            var cl = currentItems.Length;
            var pl = previousItems.Length;
            if (cl != pl) return true;                     // Jiný počet prvků          je změna
            if (cl == 0) return false;                     // Stejný počet prvků == 0   není změna

            for (int i = 0; i < cl; i++)
            {
                if (!Object.ReferenceEquals(currentItems[i], previousItems[i]))
                    return true;                           // Na stejném indexu je jiný objekt      je změna
            }
            return false;                                  // Žádný rozdíl              není změna
        }
        /// <summary>
        /// Uloží dodané pole jako prvky, které jsme naposledy ohlásili pomocí eventu <see cref="MenuItemsChanged"/> vnější aplikaci (uloží se do <see cref="__PreviousMenuItems"/>).
        /// Před příštím vyvoláním eventu bychom měli ověřit, zda aktuální prvky (získané pomocí <see cref="_GetCurrentItems"/>) jsou změny oproti uloženému stavu (pomocí metody <see cref="_IsCurrentItemsChanged(ITextItem[])"/>), a teprve po změně je budeme hlásit.
        /// </summary>
        /// <param name="currentItems"></param>
        private void _SetItemsAsPrevious(ITextItem[] currentItems)
        {
            __PreviousMenuItems = CloneArray(currentItems);
        }
        /// <summary>
        /// Vrátí new kopii dodaného pole (nebo null)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        private static T[] CloneArray<T>(T[] array)
        {
            if (array is null) return null;
            var length = array.Length;
            var result = new T[length];
            Array.Copy(array, result, length);
            return result;
        }
        /// <summary>
        /// Pole prvků Listu, které bylo naposledy oznámeno vnějšímu aplikačnímu kódu.
        /// <para/>
        /// 1. Jde o pole setované zvenku do <see cref="MenuItems"/> (při jeho setování se nevolá event <see cref="MenuItemsChanged"/>);<br/>
        /// 2. Jde o pole, které je porovnávané s aktuálním stavem <see cref="MenuItems"/> poté, kdy je provedena pravděpodobná změna (která ale nemusí způsobit reálnou změnu obsahu pole);<br/>
        /// 3. Jde o pole, které bylo naposledy ověřeno na změny a do této proměnné uloženo jako "nově platný známý stav"
        /// </summary>
        private ITextItem[] __PreviousMenuItems;
        #endregion
        #endregion
        #region Komplexní List postavený nad DataTable a Template
        /// <summary>
        /// Tabulka s daty
        /// </summary>
        protected System.Data.DataTable DataTable
        {
            get { return (_ItemsMode == ListBoxItemsMode.Table ? __DataTable : null); }
            set
            {
                this.DataSource = null;

                __DataTable = value;
                if (value != null)
                {
                    this.DataSource = value;
                    _ItemsMode = ListBoxItemsMode.Table;
                    _ReloadDxTemplate();
                }
                else
                {
                    _ItemsMode = ListBoxItemsMode.None;
                }
            }
        }
        private System.Data.DataTable __DataTable;
        /// <summary>
        /// Šablona pro zobrazení dat z <see cref="DataTable"/>
        /// </summary>
        protected DxListBoxTemplate DxTemplate
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
            if (dxTemplate != null && _ItemsMode == ListBoxItemsMode.Table)
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
        protected DxListBoxTemplate CreateSimpleDxTemplate(string columnNameItemId, string columnNameIcon, string columnNameText, string columnNameToolTip = null, int? iconSize = null)
        { return DxListBoxTemplate.CreateSimpleDxTemplate(this.DataTable, columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize); }
        /// <summary>
        /// Vrátí ItemId z DataRow pro ListBox nad tabulkou
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private object _GetTableItemId(System.Data.DataRow row)
        {
            string colName = this.DxTemplate.ColumnNameItemId;
            if (!String.IsNullOrEmpty(colName) && row != null && row.Table.Columns.Contains(colName))
                return row[colName];
            return null;
        }
        #endregion
        /// <summary>
        /// Aktuální režim položek
        /// </summary>
        private ListBoxItemsMode _ItemsMode { get { return __ItemsMode; } set { _SetItemsMode(value); } } private ListBoxItemsMode __ItemsMode;
        private void _SetItemsMode(ListBoxItemsMode itemsMode)
        {
            __ItemsMode = itemsMode;
            _RefreshMenuItemDrawColumns();
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
        /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
        /// </summary>
        protected bool DrawImageDirectly { get { return __DrawImageDirectly; } set { __DrawImageDirectly = value; this.Invalidate(); } } private bool __DrawImageDirectly;
        /// <summary>
        /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
        /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
        /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
        /// </summary>
        protected bool DuplicityEnabled { get; set; }
        /// <summary>
        /// Pokud obsahuje true, pak List si po každé změně položek zajistí Sort svých položek v <see cref="DxListBoxNative.MenuItems"/> podle hodnoty <see cref="ITextItem.ItemOrder"/>.
        /// Pokud je false, pak se nic takového neprovádí.
        /// Není dobré kombinovat s akcemi MoveUp/Down, protože nedávají smysl.
        /// </summary>
        protected bool AutoSortItems { get { return __AutoSortItems; } set { bool isChange = (value != __AutoSortItems); __AutoSortItems = value; if (isChange && value) _CheckSortedMenuItems(); } } private bool __AutoSortItems;
        /// <summary>
        /// Povoluje se výběr více prvků pomocí Ctrl+Mouse?
        /// </summary>
        protected bool MultiSelectEnabled { get { return (this.SelectionMode == SelectionMode.MultiSimple || this.SelectionMode == SelectionMode.MultiExtended); } set { this.SelectionMode = (value ? SelectionMode.MultiExtended : SelectionMode.One); } }
        #endregion
        #region Overrides
        /// <summary>
        /// Při příchodu focusu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            this.IsFocused = true;
            base.OnEnter(e);
            CheckFocused();
        }
        /// <summary>
        /// Při odchodu focusu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.IsFocused = false;
        }
        /// <summary>
        /// Řeší nemilé chování, kdy ListBox poprvé dostane kliknutí myší na prvek, obslouží si OnSelectionChanged, ale nedá do sebe Focus.
        /// Takže pak klávesový stisk nechodí do ListBoxu.
        /// </summary>
        protected void CheckFocused()
        {
            if (!base.Focused && IsFocused)
                base.Focus();
        }
        /// <summary>
        /// true = Prvek prošel metodou OnEnter / false = odešel z metody OnLeave
        /// </summary>
        protected bool IsFocused { get; set; }
        /// <summary>
        /// Po stisku klávesy
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            OnMouseVisibleItemIndex = -1;
        }
        /// <summary>
        /// Po změně vybraných prvků
        /// </summary>
        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();
            CheckFocused();
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
        #region MenuItemColumnWidths : ListBox s více sloupci vedle sebe z jednoho ITextItem, support pro dolní Scrollbar
        /// <summary>
        /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>
        /// </summary>
        protected int[] MenuItemColumnWidths { get { return __MenuItemColumnWidths; } set { _SetMenuItemColumnWidths(value); } } private int[] __MenuItemColumnWidths;
        /// <summary>
        /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>, validované, aktuální platné dle Zoomu a DPI, namísto případných záporných hodnot obsahuje 0.
        /// </summary>
        protected int[] MenuItemColumnWidthsCurrent
        {
            get
            {
                var columnWidths = MenuItemColumnWidths;
                if (columnWidths is null) return null;
                var deviceDpi = this.DeviceDpi;
                return columnWidths.Select(w => (w > 0 ? DxComponent.ZoomToGui(w, deviceDpi) : 0)).ToArray();
            }
        }
        /// <summary>
        /// Obsahuje true, pokud aktuální ListBox je v režimu <see cref="ListBoxItemsMode.MenuItems"/> a má zadané šířky sloupců do <see cref="MenuItemColumnWidths"/>, pak tedy pracuje v režimu více buněk
        /// </summary>
        protected bool MenuItemDrawColumns { get { return __MenuItemDrawColumns; } }
        /// <summary>
        /// Podle hodnot <see cref="ItemsMode"/> a <see cref="MenuItemColumnWidths"/> určí hodnotu pro <see cref="MenuItemDrawColumns"/>
        /// </summary>
        private void _RefreshMenuItemDrawColumns()
        {
            var itemsMode = this._ItemsMode;
            var columnWidths = this.__MenuItemColumnWidths;
            __MenuItemDrawColumns = (itemsMode == ListBoxItemsMode.MenuItems && columnWidths != null && ((columnWidths.Length == 1 && columnWidths[0] > 0) || columnWidths.Length > 1));
        }
        private bool __MenuItemDrawColumns;
        /// <summary>
        /// Setuje šířky sloupců a refreshuje <see cref="MenuItemDrawColumns"/>
        /// </summary>
        /// <param name="columnWidths"></param>
        private void _SetMenuItemColumnWidths(int[] columnWidths)
        {
            __MenuItemColumnWidths = columnWidths;
            _RefreshMenuItemDrawColumns();
        }
        /// <summary>
        /// Offset vodorovný = posun obsahu textu v režimu MutliColumn.
        /// Kladné číslo říká, který obsahový pixel bude zobrazen na souřadnici X = 0 v textovém prostoru.
        /// Tedy kladné číslo = odsouvání vodorovného scrollbaru doprava.
        /// </summary>
        protected int MenuItemColumnOffset
        {
            get { return __MenuItemColumnOffset; }
            set
            {
                var oldValue = __MenuItemColumnOffset;
                var newValue = value;

                if (__MenuItemDrawColumns)
                {
                    if (newValue < 0) newValue = 0;
                    if (newValue > 0)
                    {
                        var columnWidth = MenuItemColumnWidthsCurrent.Sum();
                        if (newValue > columnWidth) newValue = columnWidth;
                    }
                }

                __MenuItemColumnOffset = newValue;

                if (__MenuItemDrawColumns && newValue != oldValue)
                    this.Refresh();
            }
        }
        private int __MenuItemColumnOffset;
        #endregion
        #region OnPaint a další akce kolem vykreslení
        /// <summary>
        /// Při vykreslování
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            _PreparePaintMode();

            base.OnPaint(e);
            this._RunPaintList(e);
            this.MouseDragPaint(e);
        }
        /// <summary>
        /// Je voláno před vykreslením každého prvku. Může upravit jeho vzhled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DrawItem(object sender, ListBoxDrawItemEventArgs e)
        {
            var mode = CurrentPaintMode;
            if (mode.HasFlag(PaintModeType.TextItems))
            {
                // Nastavíme styl písma pro tuto položku:
                if (e.Item is ITextStyleItem iStyleItem && iStyleItem.FontStyle.HasValue)
                {
                    e.Appearance.FontStyleDelta = iStyleItem.FontStyle.Value;
                    e.Appearance.Options.UseTextOptions = true;
                }
            }

            // Kreslení položky následně přebírá Painter tohoto ListBoxu, kterým je naše třída DxListBoxPainter, a jeho metoda DrawItemCore

            /*
            if (_ItemsMode == ListBoxItemsMode.MenuItems && e.Item is ITextItem iTextItem)
            {
                // Nastavíme styl písma pro tuto položku:
                if (iTextItem is ITextStyleItem iStyleItem && iStyleItem.FontStyle.HasValue)
                {
                    e.Appearance.FontStyleDelta = iStyleItem.FontStyle.Value;
                    e.Appearance.Options.UseTextOptions = true;
                }

                // Pokud je režim kreslení ikony "Directly" a máme danou ikonu, pak v této metodě zajistíme vykreslení obrázku napřímo bez rozmazání:
                var imageName = iTextItem.ImageName;
                if (__DrawImageDirectly && String.IsNullOrEmpty(imageName))
                {
                    // Nebude se kreslit obrázek, protože:
                    //  ViewInfo.IsTemplate je nyní true, a v takové situaci DevExpress v metodě painteru DevExpress.XtraEditors.Drawing.PainterImageListBox : DrawItemCore() zjistí,
                    //  že e.ViewInfo.IsTemplateMode je true, a kreslení Image přeskočí!
                    __DxViewInfo.SkipPaintMenuImage = true;

                    // Standardní vykreslení ListItemu: 
                    e.DefaultDraw();

                    // Nyní vykreslíme Image svými prostředky:


                    // Vrátíme přeskočení kreslení a vrátíme příznak ViewInfo.IsTemplate na původní hodnotu (false):
                    __DxViewInfo.SkipPaintMenuImage = false;
                }
            }
            */
        }
        /// <summary>
        /// Vykreslí Image v režimu <see cref="_ItemsMode"/> == <see cref="ListBoxItemsMode.MenuItems"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="iMenuItem"></param>
        private void _DrawIMenuItemImageDirect(ListBoxDrawItemEventArgs e, ITextItem iMenuItem)
        {
            if (!__DrawImageDirectly) return;
            if (iMenuItem is null) return;

            var imageName = iMenuItem.ImageName;
            if (String.IsNullOrEmpty(imageName)) return;

            // Standardní vykreslení ListItemu: Nebude kreslit obrázek, protože metoda public override Image GetItemImage(int index) jej nevrátí:
            e.DefaultDraw();

            // Vykreslíme obrázek vlastními silami:
            paintImage();


            // Vykreslení obrázku:
            void paintImage()
            {
                // Lepší kvalita výstupu:
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Určíme, do kterého místa patří ikona:
                var itemBounds = e.Bounds;
                var imageSize = _ItemImageSize;
                int dy = (itemBounds.Height - imageSize.Height) / 2;
                var imageBounds = new Rectangle(itemBounds.X + dy, itemBounds.Y + dy, imageSize.Width, imageSize.Height);

                // Direct kreslení:
                DxComponent.PaintImage(e.Graphics, imageName, imageBounds, ImageFillMode.Resize, ContentAlignment.MiddleCenter);
            }
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
        public override Image GetItemImage(int index)
        {
            return base.GetItemImage(index);
        }
        /// <summary>
        /// Podle aktuálních proměnných určí režim kreslení do <see cref="CurrentPaintMode"/>.
        /// </summary>
        void _PreparePaintMode()
        {
            var mode = PaintModeType.None;
            switch (__ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    mode = PaintModeType.TextItems;
                    if (this.MenuItemDrawColumns)
                        mode |= PaintModeType.Columns;
                    if (DrawImageDirectly)
                        mode |= PaintModeType.DirectImage;
                    break;
                case ListBoxItemsMode.Table:
                    mode = PaintModeType.Template;
                    break;
            }
            __CurrentPaintMode = mode;
            __CurrentDeviceDpi = this.DeviceDpi;
        }
        /// <summary>
        /// Aktuální režim kreslení
        /// </summary>
        protected PaintModeType CurrentPaintMode { get { return __CurrentPaintMode; } } private PaintModeType __CurrentPaintMode;
        /// <summary>
        /// Aktuální DPI
        /// </summary>
        protected int CurrentDeviceDpi { get { return __CurrentDeviceDpi; } } private int __CurrentDeviceDpi;
        /// <summary>
        /// Režimy kreslení položky ListBoxu
        /// </summary>
        [Flags]
        protected enum PaintModeType
        {
            /// <summary>
            /// Neurčeno, nic specifického
            /// </summary>
            None = 0,
            /// <summary>
            /// Textová položka, nikoli šablona
            /// </summary>
            TextItems = 0x0001,
            /// <summary>
            /// Jednotlivé sloupce včetně posunu pomocí ScrollBaru
            /// </summary>
            Columns = 0x0002,
            /// <summary>
            /// Ikonu vykreslit Directly
            /// </summary>
            DirectImage = 0x0004,
            /// <summary>
            /// Komplexní šablona
            /// </summary>
            Template = 0x0100
        }
        #endregion
        #region DxListBoxPainter : umožní vykreslit více sloupců vedle sebe z jednoho ITextItem.Cells
        /// <summary>
        /// Vytvoří náš vlastní painter
        /// </summary>
        /// <returns></returns>
        protected override BaseControlPainter CreatePainter()
        {
            var painter = new DxListBoxPainter(this);
            return painter;
        }
        /// <summary>
        /// Painter, který dovoluje vykreslit jeden Item s více sloupci
        /// </summary>
        protected class DxListBoxPainter : DevExpress.XtraEditors.Drawing.PainterImageListBox
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            public DxListBoxPainter(DxListBoxNative owner)
            {
                __Owner = owner;
            }
            DxListBoxNative __Owner;
            /// <summary>
            /// Aktuální režim kreslení
            /// </summary>
            protected PaintModeType CurrentPaintMode { get { return __Owner.CurrentPaintMode; } }
            /// <summary>
            /// Objekt <see cref="DxImageListBoxViewInfo"/>
            /// </summary>
            protected DxImageListBoxViewInfo DxViewInfo { get { return __Owner.__DxViewInfo; } }
            /// <summary>
            /// Vykreslení jednoho prvku ListBoxu
            /// </summary>
            /// <param name="info"></param>
            /// <param name="itemInfo"></param>
            /// <param name="e"></param>
            protected override void DrawItemCore(ControlGraphicsInfoArgs info, BaseListBoxViewInfo.ItemInfo itemInfo, ListBoxDrawItemEventArgs e)
            {
                var textItem = (e.Item as ITextItem);
                var hasTextItem = (textItem != null);

                var mode = CurrentPaintMode;
                bool isColumns = (hasTextItem && mode.HasFlag(PaintModeType.Columns));
                bool isDirectImage = (mode.HasFlag(PaintModeType.DirectImage));

                if (isColumns && isDirectImage)
                {   // Sloupce i direct ikonu:
                    this.DrawItemText(info, itemInfo, e, false, "");
                    this.DrawItemCells(info, itemInfo, e, textItem);
                    this.DrawItemIcon(info, itemInfo, e, textItem);
                }
                else if (isColumns && !isDirectImage)
                {   // Sloupce a default ikonu:
                    this.DrawItemText(info, itemInfo, e, true, "");
                    this.DrawItemCells(info, itemInfo, e, textItem);
                }
                else if (!isColumns && isDirectImage)
                {   // Text (ne sloupce) a direct ikonu:
                    // base.DrawItemCore(info, itemInfo, e);
                    this.DrawItemText(info, itemInfo, e, false, null);
                    this.DrawItemIcon(info, itemInfo, e, textItem);
                }
                else
                {   // Bez nejmenších specialit:
                    base.DrawItemCore(info, itemInfo, e);
                }

                /*

                                if (__Owner.MenuItemDrawColumns && e.Item is ITextItem textItem)
                                {   // Prvek s vykreslením Columns
                                    itemInfo.Text = "";
                                    base.DrawItemCore(info, itemInfo, e);
                                    this.DrawItemCells(info, itemInfo, e, textItem);
                                }
                                else
                                {   // Prvek standardní:
                                    base.DrawItemCore(info, itemInfo, e);
                                }
                */
            }
            /// <summary>
            /// Vykreslí základní textový ListItem, s Background, bez ikony
            /// </summary>
            /// <param name="info"></param>
            /// <param name="itemInfo"></param>
            /// <param name="e"></param>
            /// <param name="drawIcon"></param>
            /// <param name="text">Explicitní text</param>
            protected void DrawItemText(ControlGraphicsInfoArgs info, BaseListBoxViewInfo.ItemInfo itemInfo, ListBoxDrawItemEventArgs e, bool drawIcon, string text = null)
            {
                if (text != null)
                    itemInfo.Text = text;

                if (drawIcon)
                    base.DrawItemCore(info, itemInfo, e);
                else
                    this.DxViewInfo.DrawItemText(info, itemInfo, e);
            }
            /// <summary>
            /// Vykreslí ikonu přímo
            /// </summary>
            /// <param name="info"></param>
            /// <param name="itemInfo"></param>
            /// <param name="e"></param>
            /// <param name="textItem"></param>
            protected void DrawItemIcon(ControlGraphicsInfoArgs info, BaseListBoxViewInfo.ItemInfo itemInfo, ListBoxDrawItemEventArgs e, ITextItem textItem)
            {
                var itemBounds = itemInfo.Bounds;
                var textBounds = itemInfo.TextRect;
                var maxH = itemBounds.Height - 2;
                var maxW = textBounds.X - itemBounds.X - 2;
                var maxS = maxH < maxW ? maxH : maxW;

                var imageArea = new Rectangle(itemBounds.X + 1, itemBounds.Y + 1, maxW, maxH);
                var imageSize = DxComponent.GetImageSize(__Owner.ItemSizeType, true, __Owner.CurrentDeviceDpi, 8, maxS);
                var imageBounds = imageSize.AlignTo(imageArea, ContentAlignment.MiddleCenter);
                DxComponent.PaintAnyImage(textItem.ImageName, __Owner.ItemSizeType, info.Cache, imageBounds);
            }
            /// <summary>
            /// Vykreslí buňky (Cells) do pozice Itemu v ListBoxu, akceptuje Offset daný scrollbarem v Owneru <see cref="DxListBoxNative.MenuItemColumnOffset"/>
            /// </summary>
            /// <param name="info"></param>
            /// <param name="itemInfo"></param>
            /// <param name="e"></param>
            /// <param name="textItem"></param>
            protected void DrawItemCells(ControlGraphicsInfoArgs info, BaseListBoxViewInfo.ItemInfo itemInfo, ListBoxDrawItemEventArgs e, ITextItem textItem)
            {
                var clipState = info.Cache.SaveClip();

                var font = e.Appearance.Font;
                bool isScale = (e.Cache.Graphics.DpiX > 0 && e.Cache.Graphics.DpiX != __Owner.CurrentDeviceDpi);
                if (isScale)
                {
                    float scale = ((float)__Owner.CurrentDeviceDpi / (float)e.Cache.Graphics.DpiX);
                    font = new Font(e.Appearance.Font.FontFamily, e.Appearance.Font.Size * scale, e.Appearance.Font.Style);
                }

                try
                {
                    // Vykreslíme jednotlivé texty z jednotlivých Cells do "virtuálních" sloupců v ListBoxu.
                    string[] cells;
                    if (textItem is ICellsItem cellsItem && cellsItem.Cells != null)
                        // Pokud dodaný objekt implementuje ICellsItem, a jeho Cells jsou zadané, pak je akceptujeme jako buňky:
                        cells = cellsItem.Cells;
                    else
                        // Jinak budeme kreslit jednu buňku do prvního Columnu, a její obsah = ITextItem.Text:
                        //   Tímto způsobem dovolujeme "posouvat" = scrollovat obsah jedné textové buňky:
                        cells = new string[] { textItem.Text };

                    var cellsCount = cells?.Length ?? 0;
                    var offset = __Owner.MenuItemColumnOffset;
                    var itemBounds = itemInfo.TextRect;
                    var textX = itemBounds.X - offset;
                    var columns = __Owner.MenuItemColumnWidthsCurrent;
                    for (int c = 0; c < columns.Length; c++)
                    {   // Všechny definované sloupce:
                        var colWidth = columns[c];
                        // Buňka (v cells) nemusí být pro tento sloupec deklarovaná:
                        var text = (c < cellsCount ? cells[c] : "");
                        drawText(itemBounds, text, ref textX, columns[c]);
                    }
                }
                finally
                {
                    info.Cache.RestoreClipRelease(clipState);
                }

                // Vykreslí text jedné buňky
                void drawText(Rectangle outerBounds, string txt, ref int tx, int cw)
                {
                    if (!String.IsNullOrEmpty(txt))
                    {
                        var textBounds = new Rectangle(tx + 1, outerBounds.Y, cw - 2, outerBounds.Height);
                        var clipBounds = Rectangle.Intersect(outerBounds, textBounds);
                        if (clipBounds.Width > 2)
                        {
                            // Vykreslí text na posunuté pozici (bude správně posunutý do textBounds, ale bude oříznutý na hranicích (outerBounds × textBounds))
                            info.Cache.SetClip(clipBounds);
                            TextRenderer.DrawText(
                                e.Cache.Graphics,
                                txt,
                                font,
                                textBounds,                                                        // new Point(textBounds.X, textBounds.Y),
                                e.Appearance.ForeColor,
                                TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter);      // TextFormatFlags.VerticalCenter 
                        }
                    }
                    // Průběžná pozice X pro následující sloupec:
                    tx += cw;
                }
            }
        }
        #endregion
        #region ViewInfo : umožní potlačit kreslení Image pomocí DevExpress, tak aby byl vykreslen Directly
        /// <summary>
        /// Vrací new ViewInfo
        /// </summary>
        /// <returns></returns>
        protected override BaseStyleControlViewInfo CreateViewInfo()
        {
            __DxViewInfo = new DxImageListBoxViewInfo(this);
            return __DxViewInfo;
        }
        private DxImageListBoxViewInfo __DxViewInfo;
        protected class DxImageListBoxViewInfo : ImageListBoxViewInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="listBox"></param>
            public DxImageListBoxViewInfo(DxListBoxNative listBox) : base(listBox)
            {
                __Owner = listBox;
            }
            private DxListBoxNative __Owner;
            /// <summary>
            /// Aktuální režim kreslení
            /// </summary>
            protected PaintModeType CurrentPaintMode { get { return __Owner.CurrentPaintMode; } }
            /// <summary>
            /// Vykreslí základní textový ListItem, s Background, bez ikony
            /// </summary>
            /// <param name="info"></param>
            /// <param name="itemInfo"></param>
            /// <param name="e"></param>
            public void DrawItemText(ControlGraphicsInfoArgs info, BaseListBoxViewInfo.ItemInfo itemInfo, ListBoxDrawItemEventArgs e)
            {
                this.ListBoxItemInfoArgs.Cache = info.Cache;
                this.ListBoxItemInfoArgs.AssignFromItemInfo(itemInfo);
                this.ListBoxItemInfoArgs.AllowDrawSkinBackground = e.AllowDrawSkinBackground;
                try
                {
                    this.ListBoxItemPainter.DrawObject(this.ListBoxItemInfoArgs);
                }
                finally
                {
                    this.ListBoxItemInfoArgs.AllowDrawSkinBackground = true;
                    this.ListBoxItemInfoArgs.Cache = null;
                    this.ListBoxItemInfoArgs.PaintAppearance = null;
                }
            }

            protected override Rectangle CalcImageRect(Rectangle itemBounds, Size imageSize)
            {
                return base.CalcImageRect(itemBounds, imageSize);
            }
            protected override ItemInfo CalcItemInfo(Rectangle bounds, int index)
            {
                return base.CalcItemInfo(bounds, index);
            }
            public override void CalcViewInfo(Graphics g)
            {
                base.CalcViewInfo(g);
            }
            protected override ListBoxItemObjectInfoArgs CreateListBoxItemInfoArgs()
            {
                return base.CreateListBoxItemInfoArgs();
            }

        }
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
        /// Určí výšku prvku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MeasureItem(object sender, MeasureItemEventArgs e)
        {
        }
        /// <summary>
        /// Událost je volána 1x per 1 řádek Listu v procesu jeho kreslení, jako příprava
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBoxCustomizeItem(object sender, DevExpress.XtraEditors.CustomizeTemplatedItemEventArgs e)
        {
            switch (_ItemsMode)
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
        protected ResourceImageSizeType ItemSizeType
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
                    __ItemImageSize = DxComponent.GetImageSize(this.ItemSizeType, true, this.DeviceDpi, 4);
                return __ItemImageSize.Value;
            }
        }
        /// <summary>
        /// Po změně Zoomu / DPI resetujeme...
        /// </summary>
        private void ImageSizeCacheReset()
        {
            __ItemImageSize = null;
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
                var item = this.GetDataItem(viewItem);
                var itemId = this.GetItemId(viewItem);

                DxListBoxItemMouseClickEventArgs args = new DxListBoxItemMouseClickEventArgs(buttons, location, Control.ModifierKeys, item, itemId);
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
        protected event DxListBoxItemMouseClickDelegate ItemMouseClick;
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemMouseDoubleClick(DxListBoxItemMouseClickEventArgs args) { }
        /// <summary>
        /// Proběhne po double kliknutí na prvek
        /// </summary>
        protected event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick;
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemEnterKeyDown(DxListBoxItemKeyEventArgs args) { }
        /// <summary>
        /// Proběhne po stisku klávesy Enter na prvku
        /// </summary>
        protected event DxListBoxItemKeyDelegate ItemEnterKeyDown;
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
        protected DxToolTipController DxToolTipController
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
            switch (_ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    // V režimu MenuItem máme prvky Listu postavené na interface ITextItem;
                    // Ten implementuje ITextItem a tedy i IToolTipItem;
                    // A pro IToolTipItem umíme vytvořit DxSuperToolTip standardním postupem:
                    if (item is ITextItem menuItem)
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
        #region SelectedItems, CurrentItem
        /// <summary>
        /// Aktuálně označené objekty. Může jich být i více, nebo žádný.
        /// Objekty to mohou být různé, typicky <see cref="ITextItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID označených řádků je v poli <see cref="SelectedItemsId"/>.
        /// </summary>
        public new object[] SelectedItems { get { return base.SelectedItems.Select(li => GetDataItem(li)).ToArray(); } }         // ImageListBoxItem => Value => ITextItem / DataRow
        /// <summary>
        /// Pole obsahující ID selectovaných záznamů.
        /// </summary>
        public object[] SelectedItemsId { get { return this.SelectedItems.Select(i => GetItemId(i)).ToArray(); } }               // ImageListBoxItem => Value => ITextItem / DataRow => ItemId
        /// <summary>
        /// Prvek, na kterém je kurzor. Je jen jediný, nebo null.
        /// Objekty to mohou být různé, typicky <see cref="ITextItem"/> nebo <see cref="System.Data.DataRowView"/>.
        /// ID aktivního řádku je v <see cref="CurrentItemId"/>.
        /// </summary>
        public object CurrentItem
        {
            get { return GetDataItem(base.SelectedItem); }                               // ImageListBoxItem => Value => ITextItem / DataRow
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
        protected bool TryGetViewItemOnPoint(Point point, out DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo foundItem)
        {
            foundItem = null;
            if (!this.ClientRectangle.Contains(point)) return false;

            var visibleItems = this.CurrentVisibleViewItems;
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
        protected bool TryGetViewItemOnIndex(int index, out DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo foundItem)
        {
            foundItem = null;
            if (index < 0) return false;

            var visibleItems = this.CurrentVisibleViewItems;
            if (visibleItems is null || index >= visibleItems.Length) return false;
            foundItem = visibleItems[index];
            return true;
        }
        /// <summary>
        /// Pole aktuálně viditelných prvků typu DevExpress.ViewInfo.
        /// Pokud je aplikován řádkový filtr, pak jde o ty prvky, které mu vyhovují.
        /// Toto pole neobsahuje prvky, které nejsou ve viditelné oblasti = sice na ně může být nascrollováno, ale aktuálně z nich není vidět ani jeden pixel.
        /// </summary>
        protected DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo[] CurrentVisibleViewItems { get { return this.ViewInfo?.VisibleItems?.OfType<DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo>().ToArray(); } }
        /// <summary>
        /// Metoda dostává Item v rámci ListBoxu, což může být <see cref="DevExpress.XtraEditors.Controls.ImageListBoxItem"/> anebo <see cref="System.Data.DataRowView"/>.
        /// Podle toho v nich vyhledá odpovídající datový prvek <see cref="ITextItem"/> anebo <see cref="System.Data.DataRow"/> a ten vrátí.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected object GetDataItem(object item)
        {
            if (item is null) return null;
            if (item is DevExpress.XtraEditors.ViewInfo.ImageListBoxViewInfo.ImageItemInfo imgItem) item = imgItem.Item;
            if (item is DevExpress.XtraEditors.Controls.ImageListBoxItem lbxItem) item = lbxItem.Value;
            if (item is System.Data.DataRowView rowView) item = rowView.Row;

            switch (_ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    if (item is ITextItem menuItem)
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

            switch (_ItemsMode)
            {
                case ListBoxItemsMode.MenuItems:
                    if (item is ITextItem menuItem)
                        return menuItem.ItemId;
                    break;
                case ListBoxItemsMode.Table:
                    if (item is System.Data.DataRow row)
                        return _GetTableItemId(row);
                    break;
            }
            return null;
        }
        #endregion
        #region DoKeyActions; CtrlA, CtrlC, CtrlX, CtrlV, Delete; Move, Insert, Remove
        /// <summary>
        /// Povolené akce dané klávesami v ListBoxu.
        /// Výchozí je <see cref="ControlKeyActionType.None"/>
        /// </summary>
        protected ControlKeyActionType EnabledKeyActions { get; set; }
        /// <summary>
        /// Povolené akce dané buttony. Buttony přidává Panel, o nich ListBox netuší. Proto se mu externě dodává pole povolených akcí od Buttonů, aby ListBox věděl, co může provádět za akce.
        /// Výchozí je <see cref="ControlKeyActionType.None"/>
        /// </summary>
        protected ControlKeyActionType EnabledButtonsActions { get; set; }
        /// <summary>
        /// Souhrn povolených akcí přímo Listu <see cref="EnabledKeyActions"/> + akcí tlačítek <see cref="EnabledButtonsActions"/>. Toto se používá interně v ListBoxu pro filtrování akcí.
        /// </summary>
        protected ControlKeyActionType EnabledActions { get { return this.EnabledKeyActions | this.EnabledButtonsActions; } }
        /// <summary>
        /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="ControlKeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
        /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        /// <param name="actions"></param>
        protected void DoKeyActions(DxItemsChangeType changeType, params ControlKeyActionType[] actions)
        {
            foreach (ControlKeyActionType action in actions)
                _DoKeyAction(action, changeType, null, true);
            _RunMenuItemsChanged(changeType);                        // Event o změně prvků volá "public" metoda DoKeyActions
        }
        /// <summary>
        /// Inicializace eventhandlerů a hodnot pro KeyActions
        /// </summary>
        private void _KeyActionsInit()
        {
            this.KeyDown += _KeyDown;
            this.EnabledKeyActions = ControlKeyActionType.None;
            this.EnabledButtonsActions = ControlKeyActionType.None;
        }
        /// <summary>
        /// Obsluha kláves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            //  DxComponent.LogAddLine(LogActivityKind.DevExpressEvents, $"KeyDown: KeyData: [{e.KeyData}]; KeyCode: [{e.KeyCode}]");

            bool isHandled = false;
            bool isChanged = false;
            switch (e.KeyData)
            {
                case Keys.Delete:
                    isHandled = _DoKeyAction(ControlKeyActionType.Delete, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.A:
                    isHandled = _DoKeyAction(ControlKeyActionType.SelectAll, DxItemsChangeType.UserInteractive, e);
                    break;
                case Keys.Control | Keys.C:
                    isHandled = _DoKeyAction(ControlKeyActionType.ClipCopy, DxItemsChangeType.UserInteractive, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.X:
                    // Ctrl+X : pokud je povoleno, provedu to; pokud ale nelze provést Ctrl+X a přitom lze provést Ctrl+C, tak se provede to:
                    if (EnabledKeyActions.HasFlag(ControlKeyActionType.ClipCut))
                    {
                        isHandled = _DoKeyAction(ControlKeyActionType.ClipCut, DxItemsChangeType.UserInteractive, e);
                        isChanged = true;
                    }
                    else if (EnabledKeyActions.HasFlag(ControlKeyActionType.ClipCopy))
                        isHandled = _DoKeyAction(ControlKeyActionType.ClipCopy, DxItemsChangeType.UserInteractive, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    break;
                case Keys.Control | Keys.V:
                    isHandled = _DoKeyAction(ControlKeyActionType.ClipPaste, DxItemsChangeType.UserInteractive, e);
                    isHandled = true;            // I kdyby tato akce NEBYLA povolena, chci ji označit jako Handled = nechci, aby v případě NEPOVOLENÉ akce dával objekt nativně věci do clipbardu.
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Home:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveTop, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Up:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveUp, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Down:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveDown, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.End:
                    isHandled = _DoKeyAction(ControlKeyActionType.MoveBottom, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Right:
                    isHandled = _DoKeyAction(ControlKeyActionType.CopyToTargetOneE, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Left:
                    isHandled = _DoKeyAction(ControlKeyActionType.CopyToSourceOneE, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Z:
                    isHandled = _DoKeyAction(ControlKeyActionType.Undo, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Control | Keys.Y:
                    isHandled = _DoKeyAction(ControlKeyActionType.Redo, DxItemsChangeType.UserInteractive, e);
                    isChanged = true;
                    break;
                case Keys.Return:
                case Keys.Shift | Keys.Return:
                case Keys.Control | Keys.Return:
                    isHandled = _DoKeyActionEnter(e);
                    break;
                default:
                    ControlKeyActionType rowFilterAction = _IsActivateKeyForFilter(e);
                    if (rowFilterAction != ControlKeyActionType.None)
                        isHandled = _DoKeyAction(rowFilterAction, DxItemsChangeType.UserInteractive, e);
                    break;
            }
            if (isChanged)
                _RunMenuItemsChanged(DxItemsChangeType.UserInteractive);                        // Event o změně prvků volá "výchozí event" _KeyDown

            if (isHandled)
                e.Handled = true;
        }
        /// <summary>
        /// Provede akce zadané jako bity v dané akci (<paramref name="actions"/>), s testem povolení dle <see cref="EnabledKeyActions"/> nebo povinně (<paramref name="force"/>)
        /// </summary>
        /// <param name="actions">Požadovaná akce</param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        /// <param name="e">Data o klávese, může být null</param>
        /// <param name="force">Provede danou akci i tehdy, když List sám ji nemá povolenou v nastavení <see cref="EnabledKeyActions"/>! </param>
        private bool _DoKeyAction(ControlKeyActionType actions, DxItemsChangeType changeType, KeyEventArgs e = null, bool force = false)
        {
            var isHandled = false;

            // Akce okolo řádkového filtru jsou povoleny vždy:
            var enabledActions = EnabledActions | ControlKeyActionType.ActivateFilter | ControlKeyActionType.FillKeyToFilter;

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
            doSingleAction(ControlKeyActionType.CopyToTargetOneC, null);                   // Pozn. pokud není dodaná metoda pro akci (=null), pak tuto akci má řešit pouze nadřazený container
            doSingleAction(ControlKeyActionType.CopyToTargetOneE, null);                //  - pomocí eventhandlerů ListActionBefore a ListActionAfter
            doSingleAction(ControlKeyActionType.CopyToTargetAllC, null);
            doSingleAction(ControlKeyActionType.CopyToTargetAllE, null);
            doSingleAction(ControlKeyActionType.CopyToSourceOneC, null);
            doSingleAction(ControlKeyActionType.CopyToSourceOneE, null);
            doSingleAction(ControlKeyActionType.CopyToSourceAllC, null);
            doSingleAction(ControlKeyActionType.CopyToSourceAllE, null);
            doSingleAction(ControlKeyActionType.Undo, _DoKeyActionUndo);
            doSingleAction(ControlKeyActionType.Redo, _DoKeyActionRedo);
            doSingleAction(ControlKeyActionType.ActivateFilter, null);                   // Měl by odchytit Parent container a případně přesměrovat
            doSingleAction(ControlKeyActionType.FillKeyToFilter, null);                  //  obdobně
            return isHandled;

            // Zjistí, zda má být provedena daná akce, a pokud ano pak ji provede.
            void doSingleAction(ControlKeyActionType action, Action<DxItemsChangeType> internalActionMethod)
            {
                if (!actions.HasFlag(action)) return;                                    // Tato akce není požadována
                if (!force && !enabledActions.HasFlag(action)) return;                   // Tato akce sice je požadována, ale není povolena

                var argsBefore = new DxListBoxActionCancelEventArgs(actions, changeType, e);
                _RunListActionBefore(argsBefore);
                if (!argsBefore.Cancel)
                {
                    if (internalActionMethod != null) internalActionMethod(changeType);  // Provedu konkrétní akci, pokud je dodána; viz dole napž. _DoKeyActionCtrlA()
                    var argsAfter = new DxListBoxActionEventArgs(actions, changeType, e);
                    _RunListActionAfter(argsAfter);
                    isHandled = true;
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
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionCtrlA(DxItemsChangeType changeType)
        {
            this.SelectedAbsoluteIndexes = this.FilteredMenuItems.Select(t => t.AbsoluteIndex).ToArray();          // Ctrl+A = SelectAll označí všechny viditelné prvky
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlC
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionCtrlC(DxItemsChangeType changeType)
        {
            var selectedItems = this.SelectedMenuItems;
            string textTxt = selectedItems.ToOneString();
            DataExchangeClipboardPublish(selectedItems, textTxt);
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlX
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionCtrlX(DxItemsChangeType changeType)
        {
            _DoKeyActionCtrlC(changeType);
            _DoKeyActionDelete(changeType);
        }
        /// <summary>
        /// Provedení klávesové akce: CtrlV
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionCtrlV(DxItemsChangeType changeType)
        {
            if (!DataExchangeClipboardAcquire(out var data)) return;
            if (data is IEnumerable<ITextItem> items) InsertItems(items, true, true, DxItemsChangeType.Clipboard);
        }
        /// <summary>
        /// Provedení klávesové akce: Delete
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionDelete(DxItemsChangeType changeType)
        {
            RemoveIndexes(this.SelectedAbsoluteIndexes);                               // Odebrat prvky, které jsou Selected, definované absolutním indexem
        }
        /// <summary>
        /// Provedení klávesové akce: MoveTop
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionMoveTop(DxItemsChangeType changeType)
        {
            _MoveSelectedItems(items => 0, changeType);
        }
        /// <summary>
        /// Provedení klávesové akce: MoveUp
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionMoveUp(DxItemsChangeType changeType)
        {
            _MoveSelectedItems(getIndexUp, changeType);

            // Metoda vrátí Target AbsoluteIndex pro přesun daných Selected prvků ve směru "O jednu pozici nahoru (k nižšímu indexu)":
            int? getIndexUp(ListMenuItemInfo[] selectedItems)
            {
                if (selectedItems is null || selectedItems.Length == 0) return 0;

                // Filtrované prvky = ty, které vyhovují řádkovému filtru a mohou být zobrazeny uživateli:
                var filteredItems = this.FilteredMenuItems;

                // 1. Najdu Min Absolute index z prvků Selected:
                var minSelAbsIndex = selectedItems.Select(i => i.AbsoluteIndex).Min();
                //   Pokud mezi vybranými prvky je prvek na indexu 0, pak výstupem je 0 = všechny Selected prvky umístíme na pozici 0 a následující:
                //   Pokud je vybrán pouze jeden prvek, a ten je na pozici 0, pak není třeba přenos provádět (vrátíme -1):
                if (minSelAbsIndex <= 0) return (selectedItems.Length == 1 ? -1 : 0);
                //   Anebo prvek na první zafiltrované pozici:
                if (filteredItems.Length > 0 && minSelAbsIndex == filteredItems[0].AbsoluteIndex) return 0;

                // 2. Nyní musím najít takový Filtered prvek, jehož Absolutní index je těsně před minSelAbsIndex, a jeho Absolut index bude Target pro přesun:
                //  a) najdu fyzickou pozici našeho MinSelAbs prvku:
                if (filteredItems.TryFindFirstIndex(i => i.AbsoluteIndex == minSelAbsIndex, out var minSelPosition))
                {   // Pokud první Selectovaný prvek je mezi filtrovanými prvky druhý anebo další:
                    //  pak Target Index = AbsoluteIndex prvku, který je mezi Filtrovanými prvky ten těsně předcházející
                    //  Tady se právě řeší ono "Přesuneme prvek na vizuálním indexu 3 na pozici, na které je předchozí prvek na vizuálním indexu 2":
                    // A tím, že pracuji s polem Filtered (=tedy ty prvky vyhovující řádkovému filtru), a v něm určuji pozice AbsoluteIndex, tím umožním přemístit prvky na výslednou fyzickou pozici = Absolute
                    if (minSelPosition > 0)
                        return filteredItems[minSelPosition - 1].AbsoluteIndex;
                }

                return 0;
            }
        }
        /// <summary>
        /// Provedení klávesové akce: MoveDown
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionMoveDown(DxItemsChangeType changeType)
        {
            _MoveSelectedItems(getIndexDown, changeType);

            // Metoda vrátí Target AbsoluteIndex pro přesun daných Selected prvků ve směru "O jednu pozici dolů (k vyššímu indexu)":
            int? getIndexDown(ListMenuItemInfo[] selectedItems)
            {
                if (selectedItems is null || selectedItems.Length == 0) return null;

                // Filtrované prvky = ty, které vyhovují řádkovému filtru a mohou být zobrazeny uživateli:
                var filteredItems = this.FilteredMenuItems;
                var filteredCount = filteredItems.Length;

                // 1a. Najdu Min Absolute index z prvků Selected:
                var minSelAbsIndex = selectedItems.Select(i => i.AbsoluteIndex).Min();
                // 1b. Najdu Man Absolute index z prvků Selected:
                var maxSelAbsIndex = selectedItems.Select(i => i.AbsoluteIndex).Max();
                // Pokud nejvyšší AbsoluteIndex z vybraných prvků == AbsoluteIndex ze všech filtrovaných prvků, pak vrátíme null =>
                //   všechny Selected prvky se přemístí na úplný konec seznamu:
                if (filteredCount > 0 && maxSelAbsIndex == filteredItems[filteredCount - 1].AbsoluteIndex) return null;

                // 2. Nyní musím najít takový Filtered prvek, jehož Absolutní index je těsně za maxSelAbsIndex, a jeho Absolut index bude Target pro přesun:
                //  a) najdu fyzickou pozici našeho MaxSelAbs prvku:
                if (filteredItems.TryFindFirstIndex(i => i.AbsoluteIndex == maxSelAbsIndex, out var maxSelPosition))
                {   // Pokud první Selectovaný prvek je mezi filtrovanými prvky druhý anebo další:
                    //  pak Target Index = AbsoluteIndex prvku, který je mezi Filtrovanými prvky ten těsně předcházející
                    //  Tady se právě řeší ono "Přesuneme prvek na vizuálním indexu 3 na pozici, na které je předchozí prvek na vizuálním indexu 2":
                    // A tím, že pracuji s polem Filtered (=tedy ty prvky vyhovující řádkovému filtru), a v něm určuji pozice AbsoluteIndex, tím umožním přemístit prvky na výslednou fyzickou pozici = Absolute
                    if (maxSelPosition < (filteredCount - 1))
                        return filteredItems[maxSelPosition + 1].AbsoluteIndex + 1;
                }

                return null;
            }
        }
        /// <summary>
        /// Provedení klávesové akce: MoveBottom
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionMoveBottom(DxItemsChangeType changeType)
        {
            _MoveSelectedItems(items => null, changeType);
        }
        /// <summary>
        /// Provedení klávesové akce: Undo
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionUndo(DxItemsChangeType changeType)
        {
            if (this.UndoRedoEnabled) this.UndoRedoController.DoUndo();
        }
        /// <summary>
        /// Provedení klávesové akce: Redo
        /// </summary>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _DoKeyActionRedo(DxItemsChangeType changeType)
        {
            if (this.UndoRedoEnabled) this.UndoRedoController.DoRedo();
        }
        /// <summary>
        /// Metoda zajistí přesunutí označených prvků na danou pozici.
        /// Pokud je zadaná pozice 0, pak jsou prvky přemístěny v jejich pořadí úplně na začátek Listu.
        /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou přemístěny za první prvek.
        /// Pokud je daná pozice null nebo větší než počet prvků, jsou prvky přemístěny na konec listu.
        /// </summary>
        /// <param name="targetIndex"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        protected void MoveSelectedItems(int? targetIndex, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            _MoveItems(this.SelectedMenuInfos, targetIndex, changeType);
            _RunMenuItemsChanged(changeType);
        }
        /// <summary>
        /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje aktuální pozicí vybraného prvku.
        /// </summary>
        /// <param name="sourceItems"></param>
        /// <param name="atCurrentIndex">Požadavek true = na pozici aktuálního prvku / false = na konec</param>
        /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        protected void InsertItems(IEnumerable<ITextItem> sourceItems, bool atCurrentIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            if (sourceItems is null || !sourceItems.Any()) return;

            int? insertAbsoluteIndex = null;
            if (atCurrentIndex)
            {
                var filteredIndex = this.SelectedIndex;                                  // SelectedIndex se vztahuje k prvkům viditelným, nikoli ke všem
                insertAbsoluteIndex = this.GetAbsoluteIndexFromFiltered(filteredIndex);  // Tady získám Absolute index prvku, na kterém je kurzor
                insertAbsoluteIndex++;                                                   // Ale InsertAtIndex bude až za tento prvek !!!  Bez tohoto incrementu by byl 'před aktuální prvek' !
            }
            _InsertItems(sourceItems, insertAbsoluteIndex, true, changeType);
            _RunMenuItemsChanged(changeType);
        }
        /// <summary>
        /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou pozicí <paramref name="insertAbsoluteIndex"/>.<br/>
        /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.<br/>
        /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.<br/>
        /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
        /// <para/>
        /// Pozor: daná pozice <paramref name="insertAbsoluteIndex"/> se vztahuje k prvkům pole <see cref="MenuItems"/> = veškeré prvky, nikoliv <see cref="CurrentVisibleMenuItems"/> = aktuálně zafiltrované prvky!
        /// </summary>
        /// <param name="sourceItems"></param>
        /// <param name="insertAbsoluteIndex">Absolutní index, kam do Items insertujeme první dodanou položku: 0=bude první na začátku, 1=bude za první existující, (Items.Count -1) = bude před poslední, Items.Count nebo null = ude Add na konec.</param>
        /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        protected void InsertItems(IEnumerable<ITextItem> sourceItems, int? insertAbsoluteIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            _InsertItems(sourceItems, insertAbsoluteIndex, selectNewItems, changeType);
            _RunMenuItemsChanged(changeType);
        }
        /// <summary>
        /// Z this Listu odebere prvky na daných indexech. Index jsou absolutní, nikoli v rámci Visible (filtrovaných) prvků.
        /// </summary>
        /// <param name="removeAbsoluteIndexes"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        protected void RemoveIndexes(IEnumerable<int> removeAbsoluteIndexes, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            this._RemoveIndexes(removeAbsoluteIndexes, changeType);
            _RunMenuItemsChanged(changeType);
        }
        /// <summary>
        /// Z this Listu odebere všechny dané prvky
        /// </summary>
        /// <param name="removeItems"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        protected void RemoveItems(IEnumerable<ITextItem> removeItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
        {
            this._RemoveItems(removeItems, changeType);
            _RunMenuItemsChanged(changeType);
        }
        #endregion
        #region Fyzické metody pro provedení změny obsahu Listu
        /// <summary>
        /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou absolutní pozicí <paramref name="insertAbsoluteIndex"/>.
        /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.
        /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.
        /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
        /// <para/>
        /// Cílový index je absolutní = vztahuje se k poli <see cref="MenuItems"/>, nikoli viditelné zafiltrované prvky dle <see cref="CurrentVisibleMenuItems"/>.
        /// Pokud index určuje vizuální proces DragAndDrop, pak musí vizuální index přepočítat pomocí <see cref="GetAbsoluteIndexFromFiltered(int)"/> z indexu vizuálního.
        /// </summary>
        /// <param name="sourceItems"></param>
        /// <param name="insertAbsoluteIndex">Absolutní index, kam do Items insertujeme první dodanou položku: 0=bude první na začátku, 1=bude za první existující, (Items.Count -1) = bude před poslední, Items.Count nebo null = ude Add na konec.</param>
        /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _InsertItems(IEnumerable<ITextItem> sourceItems, int? insertAbsoluteIndex, bool selectNewItems, DxItemsChangeType changeType)
        {
            if (sourceItems is null || !sourceItems.Any()) return;

            var validItems = _GetOnlyValidItems(sourceItems, true);            // Vyberu prvky, které vyhovují NonDuplicitě ItemId (vstupující + stávající dohromady)
            if (validItems.Length == 0) return;

            int totalCount = this.Items.Count;
            var selectedIndexes = new List<int>();                             // Tyto absolutní indexy budou selected = nově vložené prvky
            if (insertAbsoluteIndex.HasValue && insertAbsoluteIndex.Value >= 0 && insertAbsoluteIndex.Value < totalCount)
            {   // Vkládáme někam před poslední prvek:
                int targetIndex = insertAbsoluteIndex.Value;
                foreach (var sourceItem in validItems)
                {
                    selectedIndexes.Add(targetIndex);                          // Budoucí Selected prvky: pokud první dávám Insert na pozici [0], tak [0] bude Selected
                    if (!HasRowFilter)
                    {   // Pokud nemám řádkový filtr, je to triviální:
                        this.Items.Insert(targetIndex++, CreateListBoxItem(sourceItem));
                    }
                    else
                    {   // S řádkovým filtrem je potíž:
                        var criteria = this.__RowFilterCriteria;
                        this.ApplyItemsFilter(null);
                        // Insertovat prvky budeme bez řádkového filtru:
                        this.Items.Insert(targetIndex++, CreateListBoxItem(sourceItem));
                        // A filtr aplikujeme poté:
                        this.ApplyItemsFilter(criteria);
                    }
                }
            }
            else
            {
                int targetIndex = totalCount;
                foreach (var sourceItem in validItems)
                    selectedIndexes.Add(targetIndex++);
                this.Items.AddRange(CreateListBoxItems(validItems));
            }

            this._InvalidateFilteredItems();

            if (selectNewItems)
            {
                if (selectedIndexes.Count > 0)
                    this.CurrentAbsoluteIndex = selectedIndexes[0];
                this.SelectedAbsoluteIndexes = selectedIndexes.ToArray();
                this._UpdateViewInfo();
            }

            _CheckSortedMenuItems();
        }
        /// <summary>
        /// Provedení akce: Move[někam].
        /// Metoda zjistí, které prvky jsou selectované (a pokud žádný, tak skončí).
        /// Metoda se zeptá dodaného lokátora, kam (na který index) chce přesunout vybrané prvky, ty předá lokátoru.
        /// </summary>
        /// <param name="targetIndexLocator">Lokátor: pro dodané selectované prvky vrátí index přesunu. Prvky nejsou null a je nejméně jeden. Jinak se přesun neprovádí.</param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _MoveSelectedItems(Func<ListMenuItemInfo[], int?> targetIndexLocator, DxItemsChangeType changeType)
        {
            var selectedItemsInfo = this.SelectedMenuInfos;
            if (selectedItemsInfo.Length == 0) return;

            var insertAbsoluteIndex = targetIndexLocator(selectedItemsInfo);
            _MoveItems(selectedItemsInfo, insertAbsoluteIndex, changeType);
        }
        /// <summary>
        /// Provedení akce: přesuň zdejší dodané prvky na cílovou pozici.
        /// </summary>
        /// <param name="selectedItemsInfo"></param>
        /// <param name="insertAbsoluteIndex"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _MoveItems(ListMenuItemInfo[] selectedItemsInfo, int? insertAbsoluteIndex, DxItemsChangeType changeType)
        {
            if (selectedItemsInfo is null || selectedItemsInfo.Length == 0) return;

            // Toto je cesta, jak neprovádět přesun: typicky máme označen jeden prvek na indexu 0, a přesun je směrem nahoru.
            //   Pak metoda _DoKeyActionMoveUp() najde Selected prvek na indexu 0, a pokud prvek je jen jeden, tak nemá smysl provádět přesun => vyhodnotí insertAbsoluteIndex = -1:
            if (insertAbsoluteIndex.HasValue && insertAbsoluteIndex.Value < 0) return;

            // Před odebráním prvků si najdeme cílový Target prvek (ListItem prvek) podle indexu insertAbsoluteIndex = za tento prvek chceme provést Insert:
            _TryGetListItemAtAbsoluteIndex(insertAbsoluteIndex, out var insertTargetItem);

            // Odebereme zdrojové prvky:
            _RemoveIndexes(selectedItemsInfo.Select(t => t.AbsoluteIndex), DxItemsChangeType.None);

            // Po odebrání prvků (selectedItemsInfo) se změnily indexy zbývajících prvků v poli, nyní tedy najdu aktuální absolutní index právě toho prvku (ListItem prvek), za který máme insertovat dodané prvky:
            _TryGetAbsoluteIndexOfListItem(insertTargetItem, out var targetAbsoluteIndex);

            // Pokud na vstupu byl dán InsertIndex, pro který jsme našli InsertItem, 
            //   ale po odebrání Selected prvků jsme už pro InsertItem nenašli jeho TargetIndex,
            //   pak to znamená, že tento prvek (InsertItem) byl mezi těmi, které jsme odebrali!
            // To typicky nastane pouze při přesunu nahoru, kdy máme vybrány prvky např. 0,1,3 a insertAbsoluteIndex je pak roven 0.
            //   V tom případě jako targetAbsoluteIndex akceptujeme ten vstupní, namísto null => to by dalo přesun na konec!
            if (insertAbsoluteIndex.HasValue && insertTargetItem != null && !targetAbsoluteIndex.HasValue) targetAbsoluteIndex = insertAbsoluteIndex;

            // A vložíme je na aktuálně platný absolutní target index:
            _InsertItems(selectedItemsInfo.Select(i => i.MenuItem), targetAbsoluteIndex, true, changeType);
        }
        /// <summary>
        /// Z this Listu odebere prvky na daných indexech.
        /// </summary>
        /// <param name="removeAbsoluteIndexes"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _RemoveIndexes(IEnumerable<int> removeAbsoluteIndexes, DxItemsChangeType changeType)
        {
            if (removeAbsoluteIndexes == null) return;
            int totalCount = this.Items.Count;                       // _RemoveIndexes pracuje se všemi prvky bez ohledu na filtr
            var removeList = removeAbsoluteIndexes
                .CreateDictionary(i => i, true)                      // Odstraním duplicitní hodnoty indexů;
                .Keys.Where(i => (i >= 0 && i < totalCount))         //  z klíčů (indexy) vyberu jen hodnoty, které reálně existují v ListBoxu;
                .ToList();                                           //  a vytvořím List pro další práci:
            removeList.Sort((a, b) => b.CompareTo(a));               // Setřídím indexy sestupně, pro korektní postup odebírání
            removeList.ForEachExec(i => this.Items.RemoveAt(i));     // A v sestupném pořadí indexů odeberu odpovídající prvky

            this._InvalidateFilteredItems();
        }
        /// <summary>
        /// Z this Listu odebere všechny dané prvky
        /// </summary>
        /// <param name="removeItems"></param>
        /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
        private void _RemoveItems(IEnumerable<ITextItem> removeItems, DxItemsChangeType changeType)
        {
            if (removeItems == null) return;
            if (this._ItemsMode != ListBoxItemsMode.MenuItems) return;

            var removeArray = removeItems.ToArray();
            var listItems = this.MenuItems;                          // _RemoveItems musí odebírat prvky z kompletního pole, ne jen z viditelných prvků
            int totalCount = this.Items.Count;                       // _RemoveItems pracuje se všemi prvky bez ohledu na filtr
            for (int i = totalCount - 1; i >= 0; i--)
            {
                var listItem = listItems[i];
                if (listItem != null && removeArray.Any(t => Object.ReferenceEquals(t, listItem)))
                    this.Items.RemoveAt(i);
            }

            this._InvalidateFilteredItems();
        }
        /// <summary>
        /// Aktualizuje ViewInfo
        /// </summary>
        private void _UpdateViewInfo()
        {
            this.ViewInfo.Reset();
            this.RecalcViewInfoState();
            foreach (DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo item in this.ViewInfo.VisibleItems)
            {
                this.ViewInfo.UpdateItemState(item);
                this.ViewInfo.UpdateItem(item);
            }
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
        protected string DataExchangeCurrentControlId { get; set; }
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
        protected DataExchangeCrossType DataExchangeCrossType { get; set; }
        /// <summary>
        /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
        /// </summary>
        protected string DataExchangeAcceptSourceControlId { get; set; }
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
        protected DxDragDropActionType DragDropActions { get { return _DragDropActions; } set { _DxDragDropInit(value); } } private DxDragDropActionType _DragDropActions;
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

            if (_DxDragDrop != null)
                _DxDragDrop.IsActive = (actions != DxDragDropActionType.None);
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
        /// Souhrn povolených akcí Drag and Drop
        /// </summary>
        DxDragDropActionType IDxDragDropControl.DragDropActions { get { return this.DragDropActions; } }
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
            var selectedItems = this.SelectedMenuInfos;
            if (selectedItems.Length == 0)
            {
                args.SourceDragEnabled = false;
            }
            else
            {
                args.TitleText = DxComponent.Localize(MsgCode.DxDragDropSourceItemsFormTitle);
                args.SourceText = selectedItems.ToOneString(convertor: i => i.MenuItem?.ToString());
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
            if ((args.TargetIsSource || args.CurrentEffect == DragDropEffects.Move) && DxDragTargetTryGetMenuInfos(args, out var selectedItemsInfo))
            {   // Pokud (Cíl == Zdroj (provádíme přesun v rámci jednoho Listu) anebo efekt DragAndDrop je Move) pak musíme zdrojové prvky odstranit:
                var changeType = DxItemsChangeType.DragAndDrop;
                if (args.TargetIsSource)
                {   // Pokud provádíme přesun v rámci jednoho Listu (tj. Target == Source),
                    //  pak si musíme najít správný TargetIndex nyní = uživatel chce přemístit prvky před/za určitý prvek, a jeho index se odebráním prvků změní:
                    Point targetPoint = this.PointToClient(args.ScreenMouseLocation);
                    args.TargetIndex = DoDragSearchIndexRatio(targetPoint);
                    args.InsertIndex = args.TargetIndex.GetInsertIndex(selectedItemsInfo.Select(i => i.FilteredIndex ?? -1));
                    // Protože my (this) jsme Source (proto jsme tady) ale budeme i cíl (protože args.TargetIsSource je true),
                    //  pak zdejší akce _RemoveIndexes by neměla volat eventhandler o změně Items,
                    //  protože jej my budeme volat za chvilku v metodě DoDragTargetDrop(), a tam předáme changeType = DxItemsChangeType.DragAndDrop:
                    changeType = DxItemsChangeType.None;
                }
                // Odebereme zdrojové prvky:
                this._RemoveIndexes(selectedItemsInfo.Select(t => t.AbsoluteIndex), changeType);
                // Vyvoláme event, changeType je buď DragAndDrop (když Source <> Target) anebo None (když TargetIsSource), pak se fyzicky event nevyvolá:
                _RunMenuItemsChanged(changeType);
            }
        }
        /// <summary>
        /// Pokusí se najít v argumentu <see cref="DxDragDropArgs"/> zdrojový objekt <c>SourceObject</c>, a z něj získat pole prvků <see cref="ListMenuItemInfo"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private bool DxDragTargetTryGetMenuInfos(DxDragDropArgs args, out ListMenuItemInfo[] items)
        {
            items = null;
            if (args.SourceObject is IEnumerable<ListMenuItemInfo> listItemsInfo) { items = listItemsInfo.ToArray(); return true; }
            return false;
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
            {
                var insertAbsoluteIndex = (args.InsertIndex.HasValue ? GetAbsoluteIndexFromFiltered(args.InsertIndex.Value) : null);
                _InsertItems(sourceItems, insertAbsoluteIndex, true, DxItemsChangeType.DragAndDrop);
                _RunMenuItemsChanged(DxItemsChangeType.DragAndDrop);
            }

            MouseDragTargetIndex = null;
            this.Invalidate();
        }
        /// <summary>
        /// Pokusí se najít v argumentu <see cref="DxDragDropArgs"/> zdrojový objekt <c>SourceObject</c>, a z něj získat pole prvků <see cref="ITextItem"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private bool DxDragTargetTryGetItems(DxDragDropArgs args, out ITextItem[] items)
        {
            items = null;
            if (args.SourceObject is IEnumerable<ITextItem> menuItems) { items = menuItems.ToArray(); return true; }
            if (args.SourceObject is IEnumerable<ListMenuItemInfo> listItemsInfo) { items = listItemsInfo.Select(i => i.MenuItem).ToArray(); return true; }
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
            var visibleCount = this.ItemCount;                                 // DragAndDrop pracuje s pouze filtrovanými řádky
            return IndexRatio.Create(targetPoint, this.ClientRectangle, p => this.IndexFromPoint(p), i => GetItemBounds(i, false), visibleCount, Orientation.Vertical);
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
        /// Jejím účelem je vykreslit Target linku pro Drop pozici (Drag prvky a DragDrop mezi dva existující prvky).
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
        /// Viditelný Index prvku (viz <see cref="CurrentVisibleMenuItems"/>), nad kterým se pohybuje myš. Slouží k invalidaci a překreslení při změně pozice myši nebo při akci klávesy.
        /// </summary>
        protected int OnMouseVisibleItemIndex
        {
            get
            {
                var visibleCount = this.ItemCount;                   // OnMouseVisibleItemIndex pracuje pouze s Viditelnými prvky po filtrování
                if (_OnMouseItemIndex >= visibleCount)
                    _OnMouseItemIndex = -1;
                return _OnMouseItemIndex;
            }
            set
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
        /// <param name="visibleItemIndex">Index prvku v poli viditelných prvků = po filtrování. Nikoli index prvku v <see cref="MenuItems"/></param>
        /// <param name="onlyVisible"></param>
        /// <returns></returns>
        protected Rectangle? GetItemBounds(int visibleItemIndex, bool onlyVisible = true)
        {
            var visibleCount = this.ItemCount;                       // GetItemBounds pracuje s indexem Visible prvků
            if (visibleItemIndex < 0 || visibleItemIndex >= visibleCount) return null;

            Rectangle itemBounds = this.GetItemRectangle(visibleItemIndex);
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
        protected bool UndoRedoEnabled
        {
            get { return __UndoRedoEnabled; }
            set
            {
                __UndoRedoEnabled = value;
                _RunUndoRedoEnabledChanged();
            }
        }
        private bool __UndoRedoEnabled;
        /// <summary>
        /// Controller UndoRedo.
        /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
        /// Pokud je povoleno, je zde vždy instance. 
        /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
        /// </summary>
        protected UndoRedoController UndoRedoController
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
        /// Řeší odvázání eventhandleru od dosavadního controlleru, pak i navázání eventhandleru do nového controlleru, a ihned provede <see cref="_RunUndoRedoEnabledChanged"/>.
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
            _RunUndoRedoEnabledChanged();
        }
        /// <summary>
        /// Eventhandler události, kdy controller <see cref="UndoRedoController"/> 
        /// provedl změnu stavu <see cref="UndoRedoController.UndoEnabled"/> anebo <see cref="UndoRedoController.RedoEnabled"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _UndoRedoEnabledChanged(object sender, EventArgs args)
        {
            _RunUndoRedoEnabledChanged();
        }
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
        private void _RunPaintList(PaintEventArgs e)
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
        protected event PaintEventHandler PaintList;

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
        protected event DxListBoxActionCancelDelegate ListActionBefore;

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
        protected event DxListBoxActionDelegate ListActionAfter;

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
        protected event EventHandler SelectedItemsChanged;

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
        protected event System.ComponentModel.ListChangedEventHandler ListItemsChanged;

        /// <summary>
        /// Volá se po změně prvků Listu typu MenuItems
        /// </summary>
        /// <param name="changeType">Důvod změny</param>
        /// <param name="force">Vyvolat i v situaci, kdy nejsou detekovány změny v Items</param>
        private void _RunMenuItemsChanged(DxItemsChangeType changeType, bool force = false)
        {
            if (changeType != DxItemsChangeType.None)
            {
                var currentItems = _GetCurrentItems();
                bool runEvent = (force || _IsCurrentItemsChanged(currentItems));
                if (runEvent)
                {
                    _SetItemsAsPrevious(currentItems);
                    var args = new DxListBoxMenuItemsChangedEventArgs(changeType);
                    OnMenuItemsChanged(args);
                    MenuItemsChanged?.Invoke(this, args);
                }
            }
        }
        /// <summary>
        /// Volá se po změně prvků Listu typu MenuItems.
        /// </summary>
        protected virtual void OnMenuItemsChanged(DxListBoxMenuItemsChangedEventArgs args) { }
        /// <summary>
        /// Událost volaná po změně prvků Listu typu MenuItems.<br/>
        /// </summary>
        protected event DxListBoxMenuItemsChangedDelegate MenuItemsChanged;

        /// <summary>
        /// Vyvolá háček <see cref="OnUndoRedoEnabledChanged"/> a událost <see cref="UndoRedoEnabledChanged"/>.
        /// </summary>
        private void _RunUndoRedoEnabledChanged()
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
        protected event EventHandler UndoRedoEnabledChanged;

        #endregion
        #region DxProperties : property + třída, která do sebe shrnuje čistě jen Nephrite vlastnosti
        /// <summary>
        /// Souhrn vlastností (data a eventy), které tato třída poskytuje systému Nephrite
        /// </summary>
        public DxPropertiesInfo DxProperties
        {
            get
            {
                if (__DxProperties is null)
                    __DxProperties = new DxPropertiesInfo(this);
                return __DxProperties;
            }
        }
        private DxPropertiesInfo __DxProperties;
        /// <summary>
        /// Třída pro Nephrite vlastnosti
        /// </summary>
        public class DxPropertiesInfo
        {
            #region Konstruktor
            internal DxPropertiesInfo(DxListBoxNative owner)
            {
                __Owner = owner;
            }
            private DxListBoxNative __Owner;
            #endregion
            #region Vlastnosti
            /// <summary>
            /// Výška prvku
            /// </summary>
            public int ItemHeight { get { return __Owner.ItemHeightDesign; } set { __Owner.ItemHeightDesign = value; } }
            /// <summary>
            /// Přídavek k výšce jednoho řádku ListBoxu v pixelech.
            /// Hodnota 0 a záporná: bude nastaveno <see cref="DevExpress.XtraEditors.BaseListBoxControl.ItemAutoHeight"/> = true.
            /// Kladná hodnota přidá daný počet pixelů nad a pod text = zvýší výšku řádku o 2x <see cref="ItemHeightPadding"/>.
            /// Hodnota vyšší než 10 se akceptuje jako 10.
            /// </summary>
            public int ItemHeightPadding { get { return __Owner.ItemHeightPaddingDesign; } set { __Owner.ItemHeightPaddingDesign = value; } }
            /// <summary>
            /// Režim prvků v ListBoxu.
            /// </summary>
            public ListBoxItemsMode ItemsMode { get { return __Owner.ItemsMode; } }
            /// <summary>
            /// Povoluje se výběr více prvků pomocí Ctrl+Mouse?
            /// </summary>
            public bool MultiSelectEnabled { get { return __Owner.MultiSelectEnabled; } set { __Owner.MultiSelectEnabled = value; } }
            /// <summary>
            /// Konkrétní režim MultiSelectu
            /// </summary>
            public SelectionMode SelectionMode { get { return __Owner.SelectionMode; } set { __Owner.SelectionMode = value; } }
            /// <summary>
            /// Gets or sets whether to arrange items across multiple columns if the ListBox cannot fit all items vertically.
            /// true to arrange items across multiple columns; otherwise, false.
            /// </summary>
            public virtual bool MultiColumn { get { return __Owner.MultiColumn; } set { __Owner.MultiColumn = value; } }
            /// <summary>
            /// Vykreslit ikonu položek v základním režimu fyzicky = přímo, možná lepší vzhled
            /// </summary>
            public bool DrawImageDirectly { get { return __Owner.DrawImageDirectly; } set { __Owner.DrawImageDirectly = value; } }
            /// <summary>
            /// Obsahuje true, pokud aktuální ListBox je v režimu <see cref="ListBoxItemsMode.MenuItems"/> a má zadané šířky sloupců do <see cref="MenuItemColumnWidths"/>, pak tedy pracuje v režimu více buněk
            /// </summary>
            public bool MenuItemDrawColumns { get { return __Owner.MenuItemDrawColumns; } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>
            /// </summary>
            public int[] MenuItemColumnWidths { get { return __Owner.MenuItemColumnWidths; } set { __Owner.MenuItemColumnWidths = value; } }
            /// <summary>
            /// Šířky sloupců zobrazených v režimu <see cref="ItemsMode"/>: <see cref="ListBoxItemsMode.MenuItems"/>, validované, aktuální platné dle Zoomu a DPI, namísto případných záporných hodnot obsahuje 0.
            /// </summary>
            public int[] MenuItemColumnWidthsCurrent { get { return __Owner.MenuItemColumnWidthsCurrent; } }
            /// <summary>
            /// Offset vodorovný = posun obsahu textu v režimu MutliColumn.
            /// Kladné číslo říká, který obsahový pixel bude zobrazen na souřadnici X = 0 v textovém prostoru.
            /// Tedy kladné číslo = odsouvání vodorovného scrollbaru doprava.
            /// </summary>
            public int MenuItemColumnOffset { get { return __Owner.MenuItemColumnOffset; } set { __Owner.MenuItemColumnOffset = value; } }
            /// <summary>
            /// Pokud obsahuje true, pak List smí obsahovat duplicitní klíče (defaultní hodnota je true).
            /// Pokud je false, pak vložení dalšího záznamu s klíčem, který už v Listu je, bude ignorováno.
            /// Pozor, pokud List obsahuje nějaké duplicitní záznamy a poté bude nastaveno <see cref="DuplicityEnabled"/> na false, NEBUDOU duplicitní záznamy odstraněny.
            /// </summary>
            public bool DuplicityEnabled { get { return __Owner.DuplicityEnabled; } set { __Owner.DuplicityEnabled = value; } }
            /// <summary>
            /// Pokud obsahuje true, pak List si po každé změně položek zajistí Sort svých položek v <see cref="DxListBoxControl.MenuItems"/> podle hodnoty <see cref="ITextItem.ItemOrder"/>.
            /// Pokud je false, pak se nic takového neprovádí.
            /// Není dobré kombinovat s akcemi MoveUp/Down, protože nedávají smysl.
            /// </summary>
            public bool AutoSortItems { get { return __Owner.AutoSortItems; } set { __Owner.AutoSortItems = value; } }
            /// <summary>
            /// Povolené akce dané klávesami v ListBoxu.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledKeyActions { get { return __Owner.EnabledKeyActions; } set { __Owner.EnabledKeyActions = value; } }
            /// <summary>
            /// Povolené akce dané buttony. Buttony přidává Panel, o nich ListBox netuší. Proto se mu externě dodává pole povolených akcí od Buttonů, aby ListBox věděl, co může provádět za akce.
            /// Výchozí je <see cref="ControlKeyActionType.None"/>
            /// </summary>
            public ControlKeyActionType EnabledButtonsActions { get { return __Owner.EnabledButtonsActions; } set { __Owner.EnabledButtonsActions = value; } }
            /// <summary>
            /// Velikost ikon
            /// </summary>
            public ResourceImageSizeType ItemSizeType { get { return __Owner.ItemSizeType; } set { __Owner.ItemSizeType = value; } }
            /// <summary>
            /// ID tohoto objektu, je vkládáno do balíčku s daty při CtrlC, CtrlX a při DragAndDrop z tohoto zdroje.
            /// Je součástí Exchange dat uložených do <see cref="DataExchangeContainer.DataSourceId"/>.
            /// </summary>
            public string DataExchangeCurrentControlId { get { return __Owner.DataExchangeCurrentControlId; } set { __Owner.DataExchangeCurrentControlId = value; } }
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
            public DataExchangeCrossType DataExchangeCrossType { get { return __Owner.DataExchangeCrossType; } set { __Owner.DataExchangeCrossType = value; } }
            /// <summary>
            /// Povolené zdroje dat pro vkládání do this controlu pomocí výměnného balíčku <see cref="DataExchangeContainer"/>.
            /// </summary>
            public string DataExchangeAcceptSourceControlId { get { return __Owner.DataExchangeAcceptSourceControlId; } set { __Owner.DataExchangeAcceptSourceControlId = value; } }
            /// <summary>
            /// Souhrn povolených akcí Drag and Drop
            /// </summary>
            public DxDragDropActionType DragDropActions { get { return __Owner.DragDropActions; } set { __Owner.DragDropActions = value; } }
            /// <summary>
            /// UndoRedoEnabled List má povoleny akce Undo a Redo?
            /// </summary>
            public bool UndoRedoEnabled { get { return __Owner.UndoRedoEnabled; } set { __Owner.UndoRedoEnabled = value; } }
            /// <summary>
            /// Controller UndoRedo.
            /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
            /// Pokud je povoleno, je zde vždy instance. 
            /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
            /// </summary>
            public UndoRedoController UndoRedoController { get { return __Owner.UndoRedoController; } set { __Owner.UndoRedoController = value; } }
            /// <summary>
            /// Controller ToolTipu
            /// </summary>
            public DxToolTipController DxToolTipController { get { return __Owner.DxToolTipController; } }
            #endregion
            #region MenuItems
            /// <summary>
            /// Prvky Listu typované na <see cref="ITextItem"/>.
            /// <para/>
            /// Jde o <b>všechny přítomné prvky Listu</b>: pokud bude aplikován klientský řádkový filtr, pak zde budou stále všechny prvky!<br/>
            /// POkud na některé pozici nebude prvek typu <see cref="ITextItem"/>, pak na jeho pozici je zde NULL.<br/>
            /// Pro získání pouze Zobrazitelných prvků listu je třeba načíst pole <see cref="CurrentVisibleMenuItems"/>.
            /// <para/>
            /// Pokud v Listu budou obsaženy jiné prvky než <see cref="ITextItem"/>, pak na jejich místě v tomto poli bude null.
            /// Toto pole má stejný počet prvků jako pole this.Items
            /// Pole jako celek lze setovat: vymění se obsah, ale zachová se pozice.
            /// </summary>
            public ITextItem[] MenuItems { get { return __Owner.MenuItems; } set { __Owner.MenuItems = value; } }
            /// <summary>
            /// Aktuálně vybraný prvek typu <see cref="ITextItem"/>. Lze setovat, ale pouze takový prvek, kteý je přítomen (hledá se <see cref="Object.ReferenceEquals(object, object)"/>).
            /// </summary>
            public ITextItem SelectedMenuItem { get { return __Owner.SelectedMenuItem; } set { __Owner.SelectedMenuItem = value; } }
            /// <summary>
            /// Aktuálně SELECTOVANÉ prvky <see cref="ITextItem"/>
            /// </summary>
            public ITextItem[] SelectedMenuItems { get { return __Owner.SelectedMenuItems; } set { __Owner.SelectedMenuItems = value; } }
            /// <summary>
            /// Pole aktuálně dostupných položek z hlediska filtrování.
            /// <para/>
            /// Pole obsahuje pouze ty položky z <see cref="MenuItems"/>, které jsou aktuálně <b>vyhovující řádkovému filtru</b>.<br/>
            /// Prvky v tomto poli MAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.AbsoluteIndex"/> a <see cref="ListMenuItemInfo.MenuItem"/> a <see cref="ListMenuItemInfo.FilteredIndex"/>.<br/>
            /// Prvky v tomto poli NEMAJÍ naplněny hodnoty <see cref="ListMenuItemInfo.DisplayedIndex"/> a <see cref="ListMenuItemInfo.Bounds"/>.<br/>
            /// Pole není null. Může mít 0 prvků.
            /// </summary>
            public ListMenuItemInfo[] FilteredMenuItems { get { return __Owner.FilteredMenuItems; } }
            /// <summary>
            /// Pole nyní aktuálně viditelných prvků = aktuálně <b>zafiltrované</b> pomocí klientského filtru a <b>ve viditelné oblasti ListBoxu</b>.
            /// </summary>
            public ListMenuItemInfo[] CurrentVisibleMenuItems { get { return __Owner.CurrentVisibleMenuItems; } }
            #endregion
            #region Table + Template
            /// <summary>
            /// Tabulka s daty
            /// </summary>
            public System.Data.DataTable DataTable { get { return __Owner.DataTable; } set { __Owner.DataTable = value; } }
            /// <summary>
            /// Šablona pro zobrazení dat z <see cref="DataTable"/>
            /// </summary>
            public DxListBoxTemplate DxTemplate { get { return __Owner.DxTemplate; } set { __Owner.DxTemplate = value; } }
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
            { return __Owner.CreateSimpleDxTemplate(columnNameItemId, columnNameIcon, columnNameText, columnNameToolTip, iconSize); }
            #endregion
            #region Akce = metody
            /// <summary>
            /// Zajistí, že položka na daném indexu bude ve viditelné oblasti
            /// </summary>
            /// <param name="absoluteIndex"></param>
            public void MakeItemVisible(int absoluteIndex)
            {
                __Owner.MakeItemVisibleAtAbsoluteIndex(absoluteIndex);
            }
            /// <summary>
            /// Provede zadané akce v pořadí jak jsou zadány. Pokud v jedné hodnotě je více akcí (<see cref="ControlKeyActionType"/> je typu Flags), pak jsou prováděny v pořadí bitů od nejnižšího.
            /// Upozornění: požadované akce budou provedeny i tehdy, když v <see cref="EnabledKeyActions"/> nejsou povoleny = tamní hodnota má za úkol omezit uživatele, ale ne aplikační kód, který danou akci může provést i tak.
            /// </summary>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            /// <param name="actions"></param>
            public void DoKeyActions(DxItemsChangeType changeType, params ControlKeyActionType[] actions)
            {
                __Owner.DoKeyActions(changeType, actions);
            }
            /// <summary>
            /// Metoda zajistí přesunutí označených prvků na danou pozici.
            /// Pokud je zadaná pozice 0, pak jsou prvky přemístěny v jejich pořadí úplně na začátek Listu.
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou přemístěny za první prvek.
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou prvky přemístěny na konec listu.
            /// </summary>
            /// <param name="targetIndex"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void MoveSelectedItems(int? targetIndex, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                __Owner.MoveSelectedItems(targetIndex, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje aktuální pozicí vybraného prvku.
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="atCurrentIndex">Požadavek true = na pozici aktuálního prvku / false = na konec</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, bool atCurrentIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                __Owner.InsertItems(sourceItems, atCurrentIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Do this listu vloží další prvky <paramref name="sourceItems"/>, počínaje danou pozicí <paramref name="insertAbsoluteIndex"/>.<br/>
            /// Pokud je zadaná pozice 0, pak jsou prvky vloženy v jejich pořadí úplně na začátek Listu.<br/>
            /// Pokud je daná pozice 1, a stávající List má alespoň jeden prvek, pak dané prvky jsou vloženy za první prvek.<br/>
            /// Pokud je daná pozice null nebo větší než počet prvků, jsou dané prvky přidány na konec listu.
            /// <para/>
            /// Pozor: daná pozice <paramref name="insertAbsoluteIndex"/> se vztahuje k prvkům pole <see cref="MenuItems"/> = veškeré prvky, nikoliv <see cref="CurrentVisibleMenuItems"/> = aktuálně zafiltrované prvky!
            /// </summary>
            /// <param name="sourceItems"></param>
            /// <param name="insertAbsoluteIndex">Absolutní index, kam do Items insertujeme první dodanou položku: 0=bude první na začátku, 1=bude za první existující, (Items.Count -1) = bude před poslední, Items.Count nebo null = ude Add na konec.</param>
            /// <param name="selectNewItems">Nově vložené prvky mají být po vložení vybrané (Selected)?</param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void InsertItems(IEnumerable<ITextItem> sourceItems, int? insertAbsoluteIndex, bool selectNewItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                __Owner.InsertItems(sourceItems, insertAbsoluteIndex, selectNewItems, changeType);
            }
            /// <summary>
            /// Z this Listu odebere prvky na daných indexech. Index jsou absolutní, nikoli v rámci Visible (filtrovaných) prvků.
            /// </summary>
            /// <param name="removeAbsoluteIndexes"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveIndexes(IEnumerable<int> removeAbsoluteIndexes, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                __Owner.RemoveIndexes(removeAbsoluteIndexes, changeType);
            }
            /// <summary>
            /// Z this Listu odebere všechny dané prvky
            /// </summary>
            /// <param name="removeItems"></param>
            /// <param name="changeType">Důvod změny, bude uveden v argumentech události </param>
            public void RemoveItems(IEnumerable<ITextItem> removeItems, DxItemsChangeType changeType = DxItemsChangeType.Code)
            {
                __Owner.RemoveItems(removeItems, changeType);
            }
            #endregion
            #region Eventy
            /// <summary>
            /// Proběhne po jednoduchém kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseClick { add { __Owner.ItemMouseClick += value; } remove { __Owner.ItemMouseClick -= value; } }
            /// <summary>
            /// Proběhne po double kliknutí na prvek
            /// </summary>
            public event DxListBoxItemMouseClickDelegate ItemMouseDoubleClick { add { __Owner.ItemMouseDoubleClick += value; } remove { __Owner.ItemMouseDoubleClick -= value; } }
            /// <summary>
            /// Proběhne po stisku klávesy Enter na prvku
            /// </summary>
            public event DxListBoxItemKeyDelegate ItemEnterKeyDown { add { __Owner.ItemEnterKeyDown += value; } remove { __Owner.ItemEnterKeyDown -= value; } }
            /// <summary>
            /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
            /// </summary>
            public event PaintEventHandler PaintList { add { __Owner.PaintList += value; } remove { __Owner.PaintList -= value; } }
            /// <summary>
            /// Událost vyvolaná před provedením kteréhokoli požadavku, eventhandler může cancellovat akci
            /// </summary>
            public event DxListBoxActionCancelDelegate ListActionBefore { add { __Owner.ListActionBefore += value; } remove { __Owner.ListActionBefore -= value; } }
            /// <summary>
            /// Událost vyvolaná po provedení kteréhokoli požadavku
            /// </summary>
            public event DxListBoxActionDelegate ListActionAfter { add { __Owner.ListActionAfter += value; } remove { __Owner.ListActionAfter -= value; } }
            /// <summary>
            /// Událost volaná po změně selected prvků.<br/>
            /// Aktuální vybrané prvky jsou k dispozici v <see cref="SelectedItems"/>, jejich ID v <see cref="SelectedItemsId"/>.
            /// Prvek s kurzorem je v <see cref="CurrentItem"/>, jeho ID je v <see cref="CurrentItemId"/>.
            /// </summary>
            public event EventHandler SelectedItemsChanged { add { __Owner.SelectedItemsChanged += value; } remove { __Owner.SelectedItemsChanged -= value; } }
            /// <summary>
            /// Proběhne po změně v poli <see cref="MenuItems"/>
            /// </summary>
            public event System.ComponentModel.ListChangedEventHandler ListItemsChanged { add { __Owner.ListItemsChanged += value; } remove { __Owner.ListItemsChanged -= value; } }
            /// <summary>
            /// Událost volaná po změně prvků Listu typu MenuItems.<br/>
            /// </summary>
            public event DxListBoxMenuItemsChangedDelegate MenuItemsChanged { add { __Owner.MenuItemsChanged += value; } remove { __Owner.MenuItemsChanged -= value; } }
            /// <summary>
            /// Po změně stavu Undo/Redo
            /// </summary>
            public event EventHandler UndoRedoEnabledChanged { add { __Owner.UndoRedoEnabledChanged += value; } remove { __Owner.UndoRedoEnabledChanged -= value; } }
            #endregion
        }
        #endregion
    }
    #endregion
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
        public void ApplyTemplateToList(DxListBoxNative targetList)
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
    #region DxSearchControl : DevExpress SearchControl + event FilterChanged
    /// <summary>
    /// DxSearchControl : DevExpress SearchControl + event FilterChanged
    /// </summary>
    public class DxSearchControl : SearchControl
    {
        protected override void ActionSearch()
        {
            base.ActionSearch();
            FilterIsActive = true;
            _RaiseFilterChanged();
        }
        protected override void ActionClear()
        {
            base.ActionClear();
            FilterIsActive = false;
            _RaiseFilterChanged();
        }
        private void _RaiseFilterChanged()
        {
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Filtr je aktivní / smazaný
        /// </summary>
        public bool FilterIsActive { get; private set; }
        /// <summary>
        /// Událost volaná po změně filtru
        /// </summary>
        public event EventHandler FilterChanged;
    }
    #endregion
    #region class ActionButtonsHelper : pomocná třída pro práci s tlačítky okolo ListBoxu a TreeListu
    /// <summary>
    /// <see cref="ActionButtonsHelper"/> : pomocná třída pro práci s tlačítky okolo ListBoxu a TreeListu
    /// </summary>
    internal class ActionButtonsHelper
    {
        #region internal static podpora pro akční buttony v panelu
        /// <summary>
        /// Metoda do ref listu <paramref name="buttons"/> vytvoří new instance buttonů pro zadané požadované akce, 
        /// buttony uloží do daného vizuálního controlu <paramref name="parent"/> a naváže jim handler <paramref name="clickHandler"/>.<br/>
        /// Do out parametru <paramref name="enabledButtonsActions"/> dá souhrn validních akcí ze všech požadavků.
        /// </summary>
        /// <param name="requestedActionsArray"></param>
        /// <param name="buttons"></param>
        /// <param name="parent"></param>
        /// <param name="clickHandler"></param>
        /// <param name="enabledButtonsActions"></param>
        internal static void CreateActionButtons(ControlKeyActionType[] requestedActionsArray, ref List<DxSimpleButton> buttons, Control parent, EventHandler clickHandler, out ControlKeyActionType enabledButtonsActions)
        {
            if (buttons is null) buttons = new List<DxSimpleButton>();

            var enabledActions = ControlKeyActionType.None;
            var buttonList = buttons;

            // Vytvořím požadované buttony:
            if (requestedActionsArray != null && requestedActionsArray.Length > 0)
            {
                foreach (var requestedActions in requestedActionsArray)
                {
                    addOneButton(ControlKeyActionType.Delimiter, requestedActions, "", MsgCode.None, MsgCode.None);

                    addOneButton(ControlKeyActionType.MoveTop, requestedActions, ImageName.DxKeyActionMoveTop, MsgCode.DxKeyActionMoveTopTitle, MsgCode.DxKeyActionMoveTopText);
                    addOneButton(ControlKeyActionType.MoveUp, requestedActions, ImageName.DxKeyActionMoveUp, MsgCode.DxKeyActionMoveUpTitle, MsgCode.DxKeyActionMoveUpText);
                    addOneButton(ControlKeyActionType.MoveDown, requestedActions, ImageName.DxKeyActionMoveDown, MsgCode.DxKeyActionMoveDownTitle, MsgCode.DxKeyActionMoveDownText);
                    addOneButton(ControlKeyActionType.MoveBottom, requestedActions, ImageName.DxKeyActionMoveBottom, MsgCode.DxKeyActionMoveBottomTitle, MsgCode.DxKeyActionMoveBottomText);
                    addOneButton(ControlKeyActionType.Refresh, requestedActions, ImageName.DxKeyActionRefresh, MsgCode.DxKeyActionRefreshTitle, MsgCode.DxKeyActionRefreshText);
                    addOneButton(ControlKeyActionType.SelectAll, requestedActions, ImageName.DxKeyActionSelectAll, MsgCode.DxKeyActionSelectAllTitle, MsgCode.DxKeyActionSelectAllText);
                    addOneButton(ControlKeyActionType.Delete, requestedActions, ImageName.DxKeyActionDelete, MsgCode.DxKeyActionDeleteTitle, MsgCode.DxKeyActionDeleteText);
                    addOneButton(ControlKeyActionType.ClipCopy, requestedActions, ImageName.DxKeyActionClipCopy, MsgCode.DxKeyActionClipCopyTitle, MsgCode.DxKeyActionClipCopyText);
                    addOneButton(ControlKeyActionType.ClipCut, requestedActions, ImageName.DxKeyActionClipCut, MsgCode.DxKeyActionClipCutTitle, MsgCode.DxKeyActionClipCutText);
                    addOneButton(ControlKeyActionType.ClipPaste, requestedActions, ImageName.DxKeyActionClipPaste, MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);

                    addOneButton(ControlKeyActionType.CopyToTargetOneC, requestedActions, ImageName.DxKeyActionCopyToRightOneC, MsgCode.DxKeyActionCopyToRightOneTitle, MsgCode.DxKeyActionCopyToRightOneText);
                    addOneButton(ControlKeyActionType.CopyToTargetOneE, requestedActions, ImageName.DxKeyActionCopyToRightOneE, MsgCode.DxKeyActionCopyToRightOneTitle, MsgCode.DxKeyActionCopyToRightOneText);
                    addOneButton(ControlKeyActionType.CopyToTargetAllC, requestedActions, ImageName.DxKeyActionCopyToRightAllC, MsgCode.DxKeyActionCopyToRightAllTitle, MsgCode.DxKeyActionCopyToRightAllText);
                    addOneButton(ControlKeyActionType.CopyToTargetAllE, requestedActions, ImageName.DxKeyActionCopyToRightAllE, MsgCode.DxKeyActionCopyToRightAllTitle, MsgCode.DxKeyActionCopyToRightAllText);
                    addOneButton(ControlKeyActionType.CopyToSourceOneC, requestedActions, ImageName.DxKeyActionCopyToLeftOneC, MsgCode.DxKeyActionCopyToLeftOneTitle, MsgCode.DxKeyActionCopyToLeftOneText);
                    addOneButton(ControlKeyActionType.CopyToSourceOneE, requestedActions, ImageName.DxKeyActionCopyToLeftOneE, MsgCode.DxKeyActionCopyToLeftOneTitle, MsgCode.DxKeyActionCopyToLeftOneText);
                    addOneButton(ControlKeyActionType.CopyToSourceAllC, requestedActions, ImageName.DxKeyActionCopyToLeftAllC, MsgCode.DxKeyActionCopyToLeftAllTitle, MsgCode.DxKeyActionCopyToLeftAllText);
                    addOneButton(ControlKeyActionType.CopyToSourceAllE, requestedActions, ImageName.DxKeyActionCopyToLeftAllE, MsgCode.DxKeyActionCopyToLeftAllTitle, MsgCode.DxKeyActionCopyToLeftAllText);

                    addOneButton(ControlKeyActionType.Undo, requestedActions, ImageName.DxKeyActionUndo, MsgCode.DxKeyActionUndoTitle, MsgCode.DxKeyActionUndoText);
                    addOneButton(ControlKeyActionType.Redo, requestedActions, ImageName.DxKeyActionRedo, MsgCode.DxKeyActionRedoTitle, MsgCode.DxKeyActionRedoText);
                }
            }

            // out informace:
            enabledButtonsActions = enabledActions;


            // Přidá jeden button
            void addOneButton(ControlKeyActionType buttonType, ControlKeyActionType requestTypes, string imageName, MsgCode msgToolTipTitle, MsgCode msgToolTipText)
            {
                // Pokud tento typ buttonu není požadován, skončíme:
                if (!requestTypes.HasFlag(buttonType)) return;

                if (buttonType == ControlKeyActionType.Delimiter)
                {   // Oddělovač = mezírka mezi Buttony: tu přidáváme vždy, i když už tam nějaká je, a neevidujeme ji jako enabledButtonsActions:
                    //  realizujeme ji jako prvek null = mezera mezi buttony:
                    buttonList.Add(null);
                }
                else if (!enabledActions.HasFlag(buttonType))
                {   // Jiný button než Delimiter => přidáme button, pouze pokud dosud nebyl přidán (přidané typy střádáme do enabledButtonsActions):
                    //   Buttony přidáváme na pozici 0,0 a Visible = false.
                    //   Teprve při výpočtu Layoutu buttonů v metodě DoButtonsLayout() nastavujeme explicitně Visible = true - a to až po nastavení Bounds!
                    string toolTipTitle = DxComponent.Localize(msgToolTipTitle);
                    string toolTipText = DxComponent.Localize(msgToolTipText);
                    DxSimpleButton dxButton = DxComponent.CreateDxMiniButton(0, 0, 24, 24, parent, clickHandler, resourceName: imageName, toolTipTitle: toolTipTitle, toolTipText: toolTipText, visible: false, tabStop: false, allowFocus: false, tag: buttonType);
                    buttonList.Add(dxButton);
                    enabledActions |= buttonType;
                }
            }
        }
        /// <summary>
        /// Z dodaného pole buttonů všechny odebere
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="createEmptyList"></param>
        internal static void RemoveButtons(ref List<DxSimpleButton> buttons, bool createEmptyList = false)
        {
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    if (button != null)
                    {
                        button.RemoveControlFromParent();
                        button.Dispose();
                    }
                }
                buttons.Clear();
            }
            if (buttons is null && createEmptyList)
                buttons = new List<DxSimpleButton>();
        }
        /// <summary>
        /// Nastaví Enabled na všechny buttony v dodaném poli, podle stavu objektu
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="totalCount"></param>
        /// <param name="selectedCount"></param>
        /// <param name="isEditable"></param>
        /// <param name="clipEnabled"></param>
        /// <param name="undoRedoEnabled"></param>
        internal static void EnableButtons(List<DxSimpleButton> buttons, int totalCount, int selectedCount, bool isEditable, bool clipEnabled, bool undoRedoEnabled)
        {
            EnableButtons(buttons, totalCount, selectedCount, totalCount, selectedCount, isEditable, clipEnabled, undoRedoEnabled);
        }
        /// <summary>
        /// Nastaví Enabled na všechny buttony v dodaném poli, podle stavu objektu
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="totalLeftCount"></param>
        /// <param name="selectedLeftCount"></param>
        /// <param name="totalRightCount"></param>
        /// <param name="selectedRightCount"></param>
        /// <param name="isEditable"></param>
        /// <param name="clipEnabled"></param>
        /// <param name="undoRedoEnabled"></param>
        internal static void EnableButtons(List<DxSimpleButton> buttons, int totalLeftCount, int selectedLeftCount, int totalRightCount, int selectedRightCount, bool isEditable, bool clipEnabled, bool undoRedoEnabled)
        {
            if (buttons is null || buttons.Count == 0) return;
            foreach (var button in buttons)
            {
                if (button != null && button.Tag is ControlKeyActionType actionType)
                    button.Enabled = isEnabled(actionType);
            }

            bool isEnabled(ControlKeyActionType actionType)
            {
                switch (actionType)
                {
                    case ControlKeyActionType.ClipCopy: return clipEnabled;
                    case ControlKeyActionType.ClipCut: return clipEnabled && isEditable;
                    case ControlKeyActionType.ClipPaste: return clipEnabled && isEditable;
                    case ControlKeyActionType.Delete: return selectedLeftCount > 0 && isEditable;
                    case ControlKeyActionType.Refresh: return true;
                    case ControlKeyActionType.SelectAll: return totalLeftCount > 0 && selectedLeftCount < totalLeftCount;
                    case ControlKeyActionType.GoBegin: return totalLeftCount > 0;
                    case ControlKeyActionType.GoEnd: return totalLeftCount > 0;
                    case ControlKeyActionType.MoveTop: return isEditable && totalLeftCount > 0 && selectedLeftCount > 0;
                    case ControlKeyActionType.MoveUp: return isEditable && totalLeftCount > 0 && selectedLeftCount > 0;
                    case ControlKeyActionType.MoveDown: return isEditable && totalLeftCount > 0 && selectedLeftCount > 0;
                    case ControlKeyActionType.MoveBottom: return isEditable && totalLeftCount > 0 && selectedLeftCount > 0;
                    case ControlKeyActionType.Undo: return isEditable && undoRedoEnabled;
                    case ControlKeyActionType.Redo: return isEditable && undoRedoEnabled;
                    case ControlKeyActionType.ActivateFilter: return true;
                    case ControlKeyActionType.FillKeyToFilter: return true;
                    case ControlKeyActionType.CopyToTargetOneC: return selectedLeftCount > 0;
                    case ControlKeyActionType.CopyToTargetOneE: return selectedLeftCount > 0;
                    case ControlKeyActionType.CopyToTargetAllC: return totalLeftCount > 0;
                    case ControlKeyActionType.CopyToTargetAllE: return totalLeftCount > 0;
                    case ControlKeyActionType.CopyToSourceOneC: return selectedRightCount > 0;
                    case ControlKeyActionType.CopyToSourceOneE: return selectedRightCount > 0;
                    case ControlKeyActionType.CopyToSourceAllC: return totalRightCount > 0;
                    case ControlKeyActionType.CopyToSourceAllE: return totalRightCount > 0;
                }
                return true;
            }
        }
        /// <summary>
        /// Zajistí umístění dodaných buttonů do daného prostoru, na danou pozici, v dané velikosti ikony / buttonu.
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="innerBounds"></param>
        /// <param name="buttonsPosition"></param>
        /// <param name="buttonSizeType"></param>
        /// <param name="currentDpi"></param>
        internal static void DoButtonsLayout(List<DxSimpleButton> buttons, ref Rectangle innerBounds, ToolbarPosition buttonsPosition, ResourceImageSizeType buttonSizeType, int currentDpi)
        {
            var layoutInfos = getLayoutInfo(buttons);
            if (layoutInfos != null)
            {
                Padding margins = DxComponent.GetDefaultInnerMargins(currentDpi);
                Size spacing = DxComponent.GetDefaultInnerSpacing(currentDpi);
                innerBounds = DxComponent.CalculateControlItemsLayout(innerBounds, layoutInfos, buttonsPosition, margins, spacing);
            }

            // Vrátí podklady pro layout z dodaných buttonů
            ControlItemLayoutInfo[] getLayoutInfo(List<DxSimpleButton> buttons)
            {
                if (buttons == null || buttons.Count == 0) return null;

                var layoutInfos = new List<ControlItemLayoutInfo>();

                // Fyzické velikosti buttonu a delimiteru:
                Size buttonSize = GetCurrentButtonSize(buttonSizeType, currentDpi);
                Size spaceSize = new Size(buttonSize.Width / 6, buttonSize.Height / 6);

                // Pokud mezi Buttony najdeme nějaký Delimiter (prvek pole, který je null), pak oddělovače skupin nebudeme řešit implicitně, ale exaktně pomocí těchto Buttonů...
                bool useDelimiters = buttons.Any(b => b is null);
                ControlKeyActionType[] groups = new ControlKeyActionType[]
                {
                    ControlKeyActionType.Clipboard_All,
                    ControlKeyActionType.Others_All,
                    ControlKeyActionType.Go_All,
                    ControlKeyActionType.Move_All,
                    ControlKeyActionType.UndoRedo_All,
                    ControlKeyActionType.Filter_All,
                    ControlKeyActionType.Copy_All
                };
                int currentGroupIndex = -1;
                for (int b = 0; b < buttons.Count; b++)
                {
                    var button = buttons[b];
                    var isDelimiter = (button is null);
                    var buttonType = (isDelimiter ? ControlKeyActionType.Delimiter : (button != null && button.Tag is ControlKeyActionType bt) ? bt : ControlKeyActionType.None);
                    if (buttonType == ControlKeyActionType.Delimiter) isDelimiter = true;

                    if (buttonType != ControlKeyActionType.None)
                    {
                        if (!useDelimiters)
                        {   // Pokud sada buttonů NEobshauje Delimitery, pak řeším, zda před aktuální button předsadím mezeru:
                            // Najdu index skupiny akcí (v poli groups), do které patří aktuální button:
                            if (groups.TryFindFirstIndex(g => ((g & buttonType) != 0), out var buttonGroupIndex))
                            {
                                if (currentGroupIndex >= 0 && buttonGroupIndex != currentGroupIndex)
                                    // Aktuálně už máme nějakou grupu, a aktuální button patří do jiné?
                                    //  => Změna grupy => vložíme před nynější button menší mezírku:
                                    layoutInfos.Add(new ControlItemLayoutInfo() { Size = spaceSize });
                                currentGroupIndex = buttonGroupIndex;
                            }
                        }
                        else
                        {   // Používáme Delimitery : pokud nyní je button typu Delimiter, pak do pole layoutu vložíme mezeru právě nyní:
                            if (isDelimiter)
                                layoutInfos.Add(new ControlItemLayoutInfo() { Size = spaceSize });
                        }

                        // Vložíme vlastní button, kromě buttonu typu Delimtier:
                        if (!isDelimiter)
                            layoutInfos.Add(new ControlItemLayoutInfo() { Control = button, Visible = true, Size = buttonSize });
                    }
                }

                return layoutInfos.ToArray();
            }
        }
        /// <summary>
        /// Vrátí velikost pro Button pro daný typ velikosti, aktuální Zoom a cílové DPI
        /// </summary>
        /// <param name="buttonSizeType"></param>
        /// <param name="currentDpi"></param>
        /// <returns></returns>
        internal static Size GetCurrentButtonSize(ResourceImageSizeType buttonSizeType, int currentDpi)
        {
            return DxComponent.GetImageSize(buttonSizeType, true, currentDpi).Add(4, 4);
        }
        #endregion
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
        /// Jednotlivé položky, <see cref="ITextItem"/>, pole <see cref="DxListBoxControl.MenuItems"/>.
        /// Podporuje vykreslování ikon.
        /// </summary>
        MenuItems,
        /// <summary>
        /// Datová tabulka
        /// </summary>
        Table
    }
    /// <summary>
    /// Důvod změny dat
    /// </summary>
    public enum DxItemsChangeType
    {
        /// <summary>
        /// Pokud bude dodán tento druh změny, nebude se volat event o změně dat
        /// </summary>
        None,
        /// <summary>
        /// Změnu dat vyžádal aplikační kód, například setováním dat
        /// </summary>
        Code,
        /// <summary>
        /// Změnu provedl uživatel přímým zadáním dat například klávesou Delete
        /// </summary>
        UserInteractive,
        /// <summary>
        /// Změnu provedl uživatel pomocí Ctrl+V nebo Ctrl+X
        /// </summary>
        Clipboard,
        /// <summary>
        /// Změnu provedl uživatel pomocí vložení hodnoty pomocným tlačítkem, například "Dnešní den" nebo "Přidat položky odjinud tlačítkem" atd
        /// </summary>
        HelperButton,
        /// <summary>
        /// Změna byla provedena pomocí DragAndDrop myši
        /// </summary>
        DragAndDrop
    }
    /// <summary>
    /// Argumenty pro akci na <see cref="DxListBoxControl"/>
    /// </summary>
    public class DxListBoxMenuItemsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="changeType"></param>
        public DxListBoxMenuItemsChangedEventArgs(DxItemsChangeType changeType)
        {
            this.ChangeType = changeType;
        }
        /// <summary>
        /// Důvod změny
        /// </summary>
        public DxItemsChangeType ChangeType { get; }
    }
    /// <summary>
    /// Delegát pro událost Změna MenuItems na <see cref="DxListBoxControl"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DxListBoxMenuItemsChangedDelegate(object sender, DxListBoxMenuItemsChangedEventArgs e);

    /// <summary>
    /// Argumenty pro akci na <see cref="DxListBoxControl"/>
    /// </summary>
    public class DxListBoxActionEventArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="action">Probíhající akce</param>
        /// <param name="changeType">Důvod změny</param>
        /// <param name="keys">Stisknutá klávesa, může být null</param>
        public DxListBoxActionEventArgs(ControlKeyActionType action, DxItemsChangeType changeType, KeyEventArgs keys)
        {
            this.Action = action;
            this.ChangeType = changeType;
            this.Keys = keys;
        }
        /// <summary>
        /// Probíhající akce
        /// </summary>
        public ControlKeyActionType Action { get; }
        /// <summary>
        /// Důvod změny
        /// </summary>
        public DxItemsChangeType ChangeType { get; }
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
        /// <param name="changeType">Důvod změny</param>
        /// <param name="keys">Stisknutá klávesa, může být null</param>
        public DxListBoxActionCancelEventArgs(ControlKeyActionType action, DxItemsChangeType changeType, KeyEventArgs keys)
            : base(action, changeType, keys)
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
        /// <param name="item"></param>
        /// <param name="itemId"></param>
        public DxListBoxItemMouseClickEventArgs(MouseButtons buttons, Point location, Keys modifierKeys, object item, object itemId)
        {
            this.Buttons = buttons;
            this.Location = location;
            this.ModifierKeys = modifierKeys;
            this.Item = item;
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
        /// Prvek pod myší: může to být <see cref="ITextItem"/> anebo <see cref="System.Data.DataRow"/>, podle režimu Listu
        /// </summary>
        public object Item { get; }
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