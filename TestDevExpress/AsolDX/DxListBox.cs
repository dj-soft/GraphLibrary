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
    /// Panel obsahující <see cref="DxListBoxControl"/> plus tlačítka pro přesuny nahoru / dolů
    /// </summary>
    public class DxListBoxPanel : DxPanelControl
    {
        #region Konstruktor, tvorba, privátní proměnné, layout celkový
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxListBoxPanel()
        {
            _ListBox = new DxListBoxControl();
            _Buttons = new List<DxSimpleButton>();
            _ButtonsPosition = ToolbarPosition.RightSideCenter;
            _ButtonsTypes = ListBoxButtonType.None;
            _ButtonsSize = ResourceImageSizeType.Medium;
            this.Controls.Add(_ListBox);
            this.Padding = new Padding(0);
            this.ClientSizeChanged += _ClientSizeChanged;
            _ListBox.UndoRedoEnabledChanged += _ListBox_UndoRedoEnabledChanged;
            DoLayout();
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
            Rectangle innerBounds = this.GetInnerBounds();
            if (innerBounds.Width < 30 || innerBounds.Height < 30) return;

            _DoLayoutButtons(ref innerBounds);
            _ListBox.Bounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width - 0, innerBounds.Height);
        }
        /// <summary>
        /// Instance ListBoxu
        /// </summary>
        private DxListBoxControl _ListBox;
        #endregion
        #region Public prvky
        /// <summary>
        /// ListBox
        /// </summary>
        public DxListBoxControl ListBox { get { return _ListBox; } }
        /// <summary>
        /// Typy dostupných tlačítek
        /// </summary>
        public ListBoxButtonType ButtonsTypes { get { return _ButtonsTypes; } set { _ButtonsTypes = value; AcceptButtonsType(); DoLayout(); } }
        /// <summary>
        /// Umístění tlačítek
        /// </summary>
        public ToolbarPosition ButtonsPosition { get { return _ButtonsPosition; } set { _ButtonsPosition = value; DoLayout(); } }
        /// <summary>
        /// Velikost tlačítek
        /// </summary>
        public ResourceImageSizeType ButtonsSize { get { return _ButtonsSize; } set { _ButtonsSize = value; DoLayout(); } }
        #endregion
        #region Tlačítka
        /// <summary>
        /// Umístí tlačítka podle potřeby do daného vnitřního prostoru, ten zmenší o prostor zabraný tlačítky
        /// </summary>
        /// <param name="innerBounds"></param>
        private void _DoLayoutButtons(ref Rectangle innerBounds)
        {
            var layoutInfos = GetButtonsInfo();
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
        private ControlItemLayoutInfo[] GetButtonsInfo()
        {
            var buttons = _Buttons;
            if (buttons == null || buttons.Count == 0) return null;

            Size buttonSize = DxComponent.GetImageSize(_ButtonsSize, true, this.CurrentDpi).Add(4, 4);
            Size spaceSize = new Size(buttonSize.Width / 8, buttonSize.Height / 8);
            List<ControlItemLayoutInfo> layoutInfos = new List<ControlItemLayoutInfo>();
            ListBoxButtonType group1 = ListBoxButtonType.MoveTop | ListBoxButtonType.MoveUp | ListBoxButtonType.MoveDown | ListBoxButtonType.MoveBottom;
            ListBoxButtonType group2 = ListBoxButtonType.SelectAll | ListBoxButtonType.Delete;
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
        private void AcceptButtonsType()
        {
            ListBoxButtonType validButtonsTypes = _ButtonsTypes;

            // Buttony z _ButtonsType převedu na povolené akce v ListBoxu a sloučím s akcemi dosud povolenými:
            KeyActionType oldActions = _ListBox.EnabledKeyActions;
            KeyActionType newActions = ConvertButtonsToActions(validButtonsTypes);
            _ListBox.EnabledKeyActions = (newActions | oldActions);

            // Odstraním stávající buttony:
            RemoveButtons();

            // Vytvořím potřebné buttony:
            //   (vytvoří se jen ty buttony, které jsou vyžádané proměnné buttonsTypes, fyzické pořadí buttonů je dané pořadím těchto řádků)
            AcceptButtonType(ListBoxButtonType.MoveTop, validButtonsTypes, "@arrowsmall|top|blue", MsgCode.DxKeyActionMoveTopTitle, MsgCode.DxKeyActionMoveTopText);
            AcceptButtonType(ListBoxButtonType.MoveUp, validButtonsTypes, "@arrowsmall|up|blue", MsgCode.DxKeyActionMoveUpTitle, MsgCode.DxKeyActionMoveUpText);
            AcceptButtonType(ListBoxButtonType.MoveDown, validButtonsTypes, "@arrowsmall|down|blue", MsgCode.DxKeyActionMoveDownTitle, MsgCode.DxKeyActionMoveDownText);
            AcceptButtonType(ListBoxButtonType.MoveBottom, validButtonsTypes, "@arrowsmall|bottom|blue", MsgCode.DxKeyActionMoveBottomTitle, MsgCode.DxKeyActionMoveBottomText);
            AcceptButtonType(ListBoxButtonType.SelectAll, validButtonsTypes, "@editsmall|all|blue", MsgCode.DxKeyActionSelectAllTitle, MsgCode.DxKeyActionSelectAllText);
            AcceptButtonType(ListBoxButtonType.Delete, validButtonsTypes, "@editsmall|del|red", MsgCode.DxKeyActionDeleteTitle, MsgCode.DxKeyActionDeleteText);  // "devav/actions/delete.svg"
            AcceptButtonType(ListBoxButtonType.ClipCopy, validButtonsTypes, "devav/actions/copy.svg", MsgCode.DxKeyActionClipCopyTitle, MsgCode.DxKeyActionClipCopyText);
            AcceptButtonType(ListBoxButtonType.ClipCut, validButtonsTypes, "devav/actions/cut.svg", MsgCode.DxKeyActionClipCutTitle, MsgCode.DxKeyActionClipCutText);
            AcceptButtonType(ListBoxButtonType.ClipPaste, validButtonsTypes, "devav/actions/paste.svg", MsgCode.DxKeyActionClipPasteTitle, MsgCode.DxKeyActionClipPasteText);
            AcceptButtonType(ListBoxButtonType.Undo, validButtonsTypes, "svgimages/dashboards/undo.svg", MsgCode.DxKeyActionUndoTitle, MsgCode.DxKeyActionUndoText);
            AcceptButtonType(ListBoxButtonType.Redo, validButtonsTypes, "svgimages/dashboards/redo.svg", MsgCode.DxKeyActionRedoTitle, MsgCode.DxKeyActionRedoText);

            SetButtonsEnabled();
        }
        /// <summary>
        /// Metoda vytvoří Button, pokud má být vytvořen. Tedy pokud typ buttonu v <paramref name="buttonType"/> bude přítomen v povolených buttonech v <paramref name="validButtonsTypes"/>.
        /// Pak vygeneruje odpovídající button a přidá jej do pole <see cref="_Buttons"/>.
        /// </summary>
        /// <param name="buttonType">Typ konkrétního jednoho buttonu</param>
        /// <param name="validButtonsTypes">Soupis požadovaných buttonů</param>
        /// <param name="imageName"></param>
        /// <param name="msgToolTipTitle"></param>
        /// <param name="msgToolTipText"></param>
        private void AcceptButtonType(ListBoxButtonType buttonType, ListBoxButtonType validButtonsTypes, string imageName, MsgCode msgToolTipTitle, MsgCode msgToolTipText)
        {
            if (!validButtonsTypes.HasFlag(buttonType)) return;

            string toolTipTitle = DxComponent.Localize(msgToolTipTitle);
            string toolTipText = DxComponent.Localize(msgToolTipText);
            DxSimpleButton dxButton = DxComponent.CreateDxMiniButton(0, 0, 24, 24, this, this._ButtonClick, resourceName: imageName, toolTipTitle: toolTipTitle, toolTipText: toolTipText, tabStop: false, allowFocus: false, tag: buttonType);
            _Buttons.Add(dxButton);
        }
        /// <summary>
        /// Odebere všechny buttony přítomné v poli <see cref="_Buttons"/>
        /// </summary>
        private void RemoveButtons()
        {
            if (_Buttons == null)
                _Buttons = new List<DxSimpleButton>();
            else if (_Buttons.Count > 0)
            {
                foreach (var button in _Buttons)
                {
                    button.RemoveControlFromParent();
                    button.Dispose();
                }
                _Buttons.Clear();
            }
        }
        /// <summary>
        /// Promítne stav <see cref="UndoRedoController.UndoEnabled"/> a <see cref="UndoRedoController.RedoEnabled"/> 
        /// z controlleru <see cref="UndoRedoController"/> do buttonů.
        /// </summary>
        private void _ListBox_UndoRedoEnabledChanged(object sender, EventArgs e)
        {
            SetButtonsEnabled();
        }
        /// <summary>
        /// Nastaví Enabled buttonů
        /// </summary>
        private void SetButtonsEnabled()
        {
            bool undoRedoEnabled = this.UndoRedoEnabled;
            SetButtonEnabled(ListBoxButtonType.Undo, (undoRedoEnabled && this.UndoRedoController.UndoEnabled));
            SetButtonEnabled(ListBoxButtonType.Redo, (undoRedoEnabled && this.UndoRedoController.RedoEnabled));
        }
        /// <summary>
        /// Nastaví do daného buttonu stav enabled
        /// </summary>
        /// <param name="buttonType"></param>
        /// <param name="enabled"></param>
        private void SetButtonEnabled(ListBoxButtonType buttonType, bool enabled)
        {
            if (_Buttons.TryGetFirst(b => b.Tag is ListBoxButtonType bt && bt == buttonType, out var button))
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
                _ListBox.DoKeyActions(action);
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
                (buttons.HasFlag(ListBoxButtonType.SelectAll) ? KeyActionType.SelectAll : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.GoBegin) ? KeyActionType.GoBegin : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.GoEnd) ? KeyActionType.GoEnd : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveTop) ? KeyActionType.MoveTop : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveUp) ? KeyActionType.MoveUp : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveDown) ? KeyActionType.MoveDown : KeyActionType.None) |
                (buttons.HasFlag(ListBoxButtonType.MoveBottom) ? KeyActionType.MoveBottom : KeyActionType.None) |
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
        private ListBoxButtonType _ButtonsTypes;
        /// <summary>
        /// Umístění tlačítek
        /// </summary>
        private ToolbarPosition _ButtonsPosition;
        /// <summary>
        /// Velikost tlačítek
        /// </summary>
        private ResourceImageSizeType _ButtonsSize;
        /// <summary>
        /// Tlačítka, která mají být dostupná, v patřičném pořadí
        /// </summary>
        private List<DxSimpleButton> _Buttons;
        #endregion
        #region UndoRedo manager (přístup do vnitřního Listu)
        /// <summary>
        /// UndoRedoEnabled List má povoleny akce Undo a Redo?
        /// </summary>
        public bool UndoRedoEnabled { get { return _ListBox.UndoRedoEnabled; } set { _ListBox.UndoRedoEnabled = value; } }
        /// <summary>
        /// Controller UndoRedo.
        /// Pokud není povoleno <see cref="UndoRedoController"/>, je zde null.
        /// Pokud je povoleno, je zde vždy instance. 
        /// Instanci lze setovat, lze ji sdílet mezi více / všemi controly na jedné stránce / okně.
        /// </summary>
        public UndoRedoController UndoRedoController { get { return _ListBox.UndoRedoController; } set { _ListBox.UndoRedoController = value; } }
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
        /// Vybrat vše
        /// </summary>
        SelectAll = 0x0010,
        /// <summary>
        /// Přejdi na začátek
        /// </summary>
        GoBegin = 0x0020,
        /// <summary>
        /// Přejdi na konec
        /// </summary>
        GoEnd = 0x0040,

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
        Redo = 0x2000
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
            KeyActionsInit();
            DxDragDropInit(DxDragDropActionType.None);
            ToolTipInit();
            ImageInit();
            ItemSizeType = ResourceImageSizeType.Small;
        }
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
        /// Pole, obsahující informace o právě viditelných prvcích ListBoxu a jejich aktuální souřadnice
        /// </summary>
        public Tuple<int, IMenuItem, Rectangle>[] VisibleItems
        {
            get
            {
                var listItems = this.ListItems;
                var visibleItems = new List<Tuple<int, IMenuItem, Rectangle>>();
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
                var listItems = this.ListItems;
                var selectedItemsInfo = new List<Tuple<int, IMenuItem, Rectangle?>>();
                foreach (var index in this.SelectedIndices)
                {
                    Rectangle? bounds = GetItemBounds(index);
                    selectedItemsInfo.Add(new Tuple<int, IMenuItem, Rectangle?>(index, listItems[index], bounds));
                }
                return selectedItemsInfo.ToArray();
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
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
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
                this.Items.Clear();
                this.Items.AddRange(value);
            }
        }
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
            this.OnPaintList(e);
            this.PaintList?.Invoke(this, e);
            this.MouseDragPaint(e);
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
        #endregion
        #region Images
        /// <summary>
        /// Inicializace pro Images
        /// </summary>
        protected virtual void ImageInit()
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
            get { return _ItemSizeType; }
            set
            {
                _ItemSizeType = value;
                __ItemImageSize = null;
                if (this.Parent != null) this.Invalidate();
            }
        }
        /// <summary>
        /// Velikost ikon
        /// </summary>
        private ResourceImageSizeType _ItemSizeType = ResourceImageSizeType.Small;
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
        private void ToolTipInit()
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
                if (index != -1)
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
        #region DoKeyActions; Delete, CtrlA, CtrlC, CtrlX, CtrlV; Move, Insert, Remove
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
        private void KeyActionsInit()
        {
            this.PreviewKeyDown += DxListBoxControl_PreviewKeyDown;
            this.KeyDown += DxListBoxControl_KeyDown;
            this.EnabledKeyActions = KeyActionType.None;
        }
        private void DxListBoxControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }
        /// <summary>
        /// Obsluha kláves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxListBoxControl_KeyDown(object sender, KeyEventArgs e)
        {
            var enabledActions = EnabledKeyActions;
            bool handled = false;
            switch (e.KeyData)
            {
                case Keys.Delete:
                    handled = _DoKeyAction(KeyActionType.Delete);
                    break;
                case Keys.Control | Keys.A:
                    handled = _DoKeyAction(KeyActionType.SelectAll);
                    break;
                case Keys.Control | Keys.C:
                    handled = _DoKeyAction(KeyActionType.ClipCopy);
                    break;
                case Keys.Control | Keys.X:
                    // Ctrl+X : pokud je povoleno, provedu; pokud nelze provést Ctrl+X ale lze provést Ctrl+C, tak se provede to:
                    if (EnabledKeyActions.HasFlag(KeyActionType.ClipCut))
                        handled = _DoKeyAction(KeyActionType.ClipCut);
                    else if (EnabledKeyActions.HasFlag(KeyActionType.ClipCopy))
                        handled = _DoKeyAction(KeyActionType.ClipCopy);
                    break;
                case Keys.Control | Keys.V:
                    handled = _DoKeyAction(KeyActionType.ClipPaste);
                    break;
                case Keys.Alt | Keys.Home:
                    handled = _DoKeyAction(KeyActionType.MoveTop);
                    break;
                case Keys.Alt | Keys.Up:
                    handled = _DoKeyAction(KeyActionType.MoveUp);
                    break;
                case Keys.Alt | Keys.Down:
                    handled = _DoKeyAction(KeyActionType.MoveDown);
                    break;
                case Keys.Alt | Keys.End:
                    handled = _DoKeyAction(KeyActionType.MoveBottom);
                    break;
                case Keys.Control | Keys.Z:
                    handled = _DoKeyAction(KeyActionType.Undo);
                    break;
                case Keys.Control | Keys.Y:
                    handled = _DoKeyAction(KeyActionType.Redo);
                    break;
            }
            if (handled)
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
            _DoKeyAction(action, KeyActionType.SelectAll, force, _DoKeyActionCtrlA, ref handled);
            _DoKeyAction(action, KeyActionType.ClipCopy, force, _DoKeyActionCtrlC, ref handled);
            _DoKeyAction(action, KeyActionType.ClipCut, force, _DoKeyActionCtrlX, ref handled);
            _DoKeyAction(action, KeyActionType.ClipPaste, force, _DoKeyActionCtrlV, ref handled);
            _DoKeyAction(action, KeyActionType.MoveTop, force, _DoKeyActionMoveTop, ref handled);
            _DoKeyAction(action, KeyActionType.MoveUp, force, _DoKeyActionMoveUp, ref handled);
            _DoKeyAction(action, KeyActionType.MoveDown, force, _DoKeyActionMoveDown, ref handled);
            _DoKeyAction(action, KeyActionType.MoveBottom, force, _DoKeyActionMoveBottom, ref handled);
            _DoKeyAction(action, KeyActionType.Delete, force, _DoKeyActionDelete, ref handled);
            _DoKeyAction(action, KeyActionType.Undo, force, _DoKeyActionUndo, ref handled);
            _DoKeyAction(action, KeyActionType.Redo, force, _DoKeyActionRedo, ref handled);
            return handled;
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
        /// Provedení klávesové akce: Delete
        /// </summary>
        private void _DoKeyActionDelete()
        {
            RemoveIndexes(this.SelectedIndexes);
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
            DxComponent.ClipboardInsert(selectedItems, textTxt);
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
            if (!DxComponent.ClipboardTryGetApplicationData(out var data)) return;
            if (!(data is IEnumerable<IMenuItem> items)) return;

            var itemList = items.ToList();
            if (itemList.Count == 0) return;

            foreach (var item in itemList)
                this.Items.Add(item);

            this.SelectedItem = itemList[0];
            this.SelectedItems = itemList;
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

            // int i = this.HotItemIndex;
            // var s = this.SelectedIndex;

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

            List<int> selectedIndexes = new List<int>();
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value < this.ItemCount)
            {
                int index = insertIndex.Value;
                foreach (var sourceItem in sourceItems)
                {
                    DevExpress.XtraEditors.Controls.ImageListBoxItem imgItem = new DevExpress.XtraEditors.Controls.ImageListBoxItem(sourceItem);
                    selectedIndexes.Add(index);
                    this.Items.Insert(index++, imgItem);
                }
            }
            else
            {
                int index = this.ItemCount;
                foreach (var sourceItem in sourceItems)
                    selectedIndexes.Add(index++);
                this.Items.AddRange(sourceItems.ToArray());
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
        #endregion
        #region Přesouvání prvků pomocí myši
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
        protected virtual void OnUndoRedoEnabledChanged() { }
        public event EventHandler UndoRedoEnabledChanged;
        void IUndoRedoControl.DoUndoRedoStep(object state)
        { }
        #endregion
        #region Public eventy

        /// <summary>
        /// Volá se po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintList(PaintEventArgs e) { }
        /// <summary>
        /// Událost volaná po vykreslení základu Listu, před vykreslením Reorder ikony
        /// </summary>
        public event PaintEventHandler PaintList;
        #endregion
    }
}
