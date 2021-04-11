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

namespace TestDevExpress.Components
{
    /// <summary>
    /// Panel, který vkládá controly do svých rámečků se Splittery
    /// </summary>
    public class DxLayoutPanel : DxPanelControl
    {
        #region Public prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutPanel()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this._Controls = new List<ControlParentInfo>();
        }
        /// <summary>
        /// Metoda najde a vrátí instanci <see cref="DxLayoutPanel"/>, do které patří daný control
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static DxLayoutPanel SearchParentLayoutPanel(Control control)
        {
            int timeout = 50;
            while (control != null && (--timeout) > 0)
            {
                if (control is DxLayoutPanel dxLayoutPanel) return dxLayoutPanel;
                control = control.Parent;
            }
            return null;
        }
        /// <summary>
        /// Pole všech uživatelských controlů (neobsahuje tedy SplitContainery). Pole je lineární, nezohledňuje aktuální rozložení.
        /// </summary>
        public Control[] AllControls { get { return _GetAllControls(); } }
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// </summary>
        public event EventHandler<TEventArgs<Control>> UserControlRemoved;
        /// <summary>
        /// Vyvolá se po odebrání posledního controlu.
        /// </summary>
        public event EventHandler LastControlRemoved;
        /// <summary>
        /// Vyvolá se po změně pozice splitteru.
        /// </summary>
        public event EventHandler<DxLayoutPanelSplitterChangedArgs> SplitterPositionChanged;
        /// <summary>
        /// Vyvolá se po změně orientace splitteru.
        /// </summary>
        public event EventHandler<DxLayoutPanelSplitterChangedArgs> SplitterOrientationChanged;
        #endregion
        #region Přidání, odebrání a evidence UserControlů
        /// <summary>
        /// Metoda přidá daný control do layoutu.
        /// Typicky se používá pro první control, ale může být použita pro kterýkoli další. Pak se přidá za posledně přidaný doprava.
        /// </summary>
        /// <param name="userControl"></param>
        public void AddControl(Control userControl)
        {
            if (userControl == null) return;
            _AddControlDefault(userControl, AddControlParams.Default);
        }
        /// <summary>
        /// Přidá nový control vedle controlu daného
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="previousControl"></param>
        /// <param name="position"></param>
        /// <param name="previousSize">Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)</param>
        /// <param name="currentSize"></param>
        /// <param name="previousSizeRatio"></param>
        /// <param name="currentSizeRatio"></param>
        public void AddControl(Control userControl, Control previousControl, LayoutPosition position,
            int? previousSize = null, int? currentSize = null, float? previousSizeRatio = null, float? currentSizeRatio = null)
        {
            if (userControl == null) return;

            AddControlParams parameters = new AddControlParams() { Position = position, PreviousSize = previousSize, CurrentSize = currentSize, PreviousSizeRatio = previousSizeRatio, CurrentSizeRatio = currentSizeRatio };
            int prevIndex = _GetIndexOfUserControl(previousControl);
            if (prevIndex < 0)
                _AddControlDefault(userControl, parameters);
            else
                _AddControlNear(userControl, prevIndex, parameters);
        }
        /// <summary>
        /// Najde daný control ve své evidenci, a pokud tam je, pak jej odebere a jeho prostor uvolní pro nejbližšího souseda.
        /// </summary>
        /// <param name="userControl"></param>
        public void RemoveControl(Control userControl)
        {
            int index = _GetIndexOfUserControl(userControl);
            _RemoveUserControl(index);
        }
        /// <summary>
        /// Přidá nový control na vhodné místo.
        /// Není určen Near control (tj. vedle kterého controlu má být nový umístěn).
        /// Typicky se používá pro první control, ten se vkládá přímo do this jako jediný.
        /// Může se použít i jindy, pak nový control přidá k posledně přidanému controlu.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="parameters"></param>
        private void _AddControlDefault(Control userControl, AddControlParams parameters)
        {
            int count = _Controls.Count;
            if (count == 0)
            {   // Zatím nemáme nic: nový control vložím přímo do this jako jediný (nebude se zatím používat SplitterContainer, není proč):
                // this._RemoveRootContainer();
                _AddControlTo(userControl, this);
            }
            else
            {   // Už něco máme, přidáme nový control poblíž posledního evidovaného prvku:
                _AddControlNear(userControl, count - 1, parameters);
            }
        }
        /// <summary>
        /// Dodaný control přidá poblíž jiného controlu, podle daných parametrů
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="nearIndex"></param>
        /// <param name="parameters"></param>
        private void _AddControlNear(Control userControl, int nearIndex, AddControlParams parameters)
        {
            // 1. Prvek s indexem [parentIndex] obsahuje control, vedle kterého budeme přidávat nově dodaný control
            ControlParentInfo nearInfo = _Controls[nearIndex];
            Control parent = nearInfo.Parent;

            // 2. Tento control (jeho hostitele) tedy odebereme z jeho dosavadního parenta:
            var nearHost = nearInfo.HostControl;                     // Podržím si referenci v lokální proměnné, jinak by mohl objekt zmizet, protože v nearInfo je jen WeakReference
            _RemoveControlFromParent(nearHost, parent);

            // 3. Do toho parenta vložíme místo controlu nový SplitterContainer a určíme panely pro stávající control a pro nový prvek (Panel1 a Panel2, podle parametru):
            DevExpress.XtraEditors.SplitContainerControl newSplitContainer = _CreateNewContainer(parameters, out DevExpress.XtraEditors.SplitGroupPanel currentControlPanel, out DevExpress.XtraEditors.SplitGroupPanel newControlPanel);
            parent.Controls.Add(newSplitContainer);
            newSplitContainer.SplitterPosition = GetSplitterPosition(parent.ClientSize, parameters);         // Až po vložení do Parenta
            newSplitContainer.SplitterMoved += _SplitterMoved;                                               // Až po nastavení pozice

            // 4. Stávající prvek vložíme jako Child do jeho nově určeného panelu, a vepíšeme to i do evidence:
            currentControlPanel.Controls.Add(nearHost);
            nearInfo.Parent = currentControlPanel;

            // 5. Nový UserControl vložíme do jeho panelu a vytvoříme pár a přidáme jej do evidence:
            _AddControlTo(userControl, newControlPanel);
        }
        /// <summary>
        /// Přidá daný control do daného parenta jako jeho Child, dá Dock = Fill, a přidá informaci do evidence v <see cref="_Controls"/>.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="parent"></param>
        private void _AddControlTo(Control userControl, Control parent)
        {
            DxLayoutItemPanel hostControl = new DxLayoutItemPanel();
            hostControl.UserControl = userControl;
            hostControl.TitleBarVisible = true;
            hostControl.TitleText = "Titulek";
            hostControl.DockButtonsEnabled = true;
            hostControl.CloseButtonVisible = true;

            parent.Controls.Add(hostControl);
            ControlParentInfo pair = new ControlParentInfo(parent, hostControl, userControl);
            _Controls.Add(pair);
        }
        /// <summary>
        /// Odebere daný control z parenta (vizuálně) i z evidence (datově)
        /// </summary>
        /// <param name="removeIndex"></param>
        private void _RemoveUserControl(int removeIndex)
        {
            if (removeIndex < 0) return;                             // To není můj control

            var removeInfo = _Controls[removeIndex];
            var parent = removeInfo.Parent;                          // Parent daného controlu je typicky this (když control je jediný), anebo Panel1 nebo Panel2 nějakého SplitContaineru
            var removeHost = removeInfo.HostControl;
            var removeControl = removeInfo.UserControl;
            if (parent != null && removeHost != null)
            {
                // 1. Odebereme daný control z jeho parenta a z evidence:
                _RemoveControlFromParent(removeHost, parent);
                _Controls.RemoveAt(removeIndex);
                OnUserControlRemoved(removeControl);

                // 2. Zjistíme, zda jeho parent je Panel1 nebo Panel2 z určitého SplitContaineru (najdeme SplitContainer a párový Panel) => pak najdeme párový Control a přemístíme jej nahoru:
                if (_IsParentSplitPanel(parent, out DevExpress.XtraEditors.SplitContainerControl splitContainer, out DevExpress.XtraEditors.SplitGroupPanel pairPanel))
                {
                    // 2a. Najdeme data o párovém panelu:
                    int pairIndex = _Controls.FindIndex(i => i.ContainsParent(pairPanel));
                    if (pairIndex < 0)
                    {   // Párový panel NEOBSAHUJE nijaký UserControl, měl by tedy obsahovat jiný SplitContainer, který obsahuje dva panely, v každém jeden UserControl.
                        // Najdeme tedy tento párový SplitContainer a přemístíme jej z pairPanel do splitContainer.Parent, a splitContainer (nyní už prázdný) jako takový zrušíme:
                        DevExpress.XtraEditors.SplitContainerControl pairContainer = pairPanel.Controls.OfType<DevExpress.XtraEditors.SplitContainerControl>().FirstOrDefault();
                        if (pairContainer != null)
                        {   // Našli jsme SplitContainerControl v sousedním panelu:
                            // Odebereme jej:
                            _RemoveControlFromParent(pairContainer, pairPanel);

                            // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                            var splitParent = splitContainer.Parent;
                            _RemoveControlFromParent(splitContainer, splitParent);

                            // Do parenta od dosavadního SplitContaineru vložíme ten párový control (ale nikam to nepíšeme, protože UserControly a jejich parenti se nemění):
                            splitParent.Controls.Add(pairContainer);
                        }
                    }
                    else
                    {   // Takže jsme odebrali prvek UserControl (pomocí jeho Host) z jednoho panelu, a na párovém panelu máme jiný prvek UserControl.
                        // Nechceme mít prázdný panel, a ani nechceme prázdný panel schovávat, chceme mít čistý layout = viditelné všechny panely a na nich vždy jeden control!
                        // Najdeme tedy párový control, odebereme SplitContaner, a na jeho místo vložíme ten párový Control:
                        var pairInfo = _Controls[pairIndex];
                        var pairControl = pairInfo.UserControl;                // Po dobu výměny si podržíme reference v proměnných, protože v pairInfo jsou jen WeakReference!
                        var pairHost = pairInfo.HostControl;

                        // Odebereme párový control z jeho dosavadního parenta (kterým je párový panel):
                        _RemoveControlFromParent(pairHost, pairPanel);

                        // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                        var splitParent = splitContainer.Parent;
                        _RemoveControlFromParent(splitContainer, splitParent);

                        // Do parenta od dosavadního SplitContaineru vložíme ten párový control (a vepíšeme to do jeho Infa).
                        // Tento Parent je buď nějaký Panel1 nebo Panel2 od jiného SplitContaineru, anebo to může být this:
                        splitParent.Controls.Add(pairHost);
                        pairInfo.Parent = splitParent;
                    }
                }

                // 3. Pokud parentem není SplitContainer, zjistíme zda Parent jsme my sami => pak jsme odebrali poslední control:
                else if (Object.ReferenceEquals(parent, this))
                {
                    this.OnLastControlRemoved();
                }
            }
        }
        /// <summary>
        /// Daný control odebere z daného parenta, pokud je vše správně zadáno
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        private void _RemoveControlFromParent(Control control, Control parent)
        {
            if (control != null && parent != null)
            {
                int index = parent.Controls.IndexOf(control);
                if (index >= 0)
                    parent.Controls.RemoveAt(index);
            }
        }
        /// <summary>
        /// Vrátí pole všech uživatelských controlů (neobsahuje tedy SplitContainery). Pole je lineární, nezohledňuje aktuální rozložení.
        /// </summary>
        /// <returns></returns>
        private Control[] _GetAllControls()
        {
            List<Control> controls = new List<Control>();
            foreach (var controlPair in _Controls)
            {
                var control = controlPair.UserControl;
                if (control != null)
                    controls.Add(control);
            }
            return controls.ToArray();
        }
        /// <summary>
        /// Metoda vytvoří a vrátí nový <see cref="DevExpress.XtraEditors.SplitContainerControl"/>.
        /// Současně určí (out parametry) panely, kam se má vložit stávající control a kam nový control, podle pozice v parametru <see cref="AddControlParams.Position"/>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="currentControlPanel"></param>
        /// <param name="newControlPanel"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.SplitContainerControl _CreateNewContainer(AddControlParams parameters, out DevExpress.XtraEditors.SplitGroupPanel currentControlPanel, out DevExpress.XtraEditors.SplitGroupPanel newControlPanel)
        {
            var container = new DxSplitContainerControl() { Dock = DockStyle.Fill };

            // parametry:
            container.IsSplitterFixed = parameters.IsSplitterFixed;
            container.FixedPanel = parameters.FixedPanel;
            container.Panel1.MinSize = parameters.MinSize;
            container.Panel2.MinSize = parameters.MinSize;

            // Horizontální panely (když se nový otevírá vlevo nebo vpravo):
            container.Horizontal = parameters.IsHorizontal;

            // Panely, do nichž se budou vkládat současný a nový control:
            bool newPositionIs2 = parameters.NewPanelIsPanel2;
            currentControlPanel = (newPositionIs2 ? container.Panel1 : container.Panel2);
            newControlPanel = (newPositionIs2 ? container.Panel2 : container.Panel1);

            container.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;

            container.MouseDown += _SplitContainerMouseDown;

            return container;
        }
        /// <summary>
        /// Vrátí pozici splitteru
        /// </summary>
        /// <param name="parentSize"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int GetSplitterPosition(Size parentSize, AddControlParams parameters)
        {
            // Celková velikost v pixelech podle orientace:
            int size = (parameters.IsHorizontal ? parentSize.Width : parentSize.Height);

            // Pixelová hodnota - pro CurrentPanel nebo PreviousPanel:
            if (parameters.CurrentSize.HasValue && parameters.CurrentSize.Value > 0)
            {
                if (parameters.NewPanelIsPanel1) return parameters.CurrentSize.Value;              // Pokud CurrentPanel je Panel1 (Left nebo Top), pak SplitterPosition je přímo hodnota
                if (parameters.NewPanelIsPanel2) return (size - parameters.CurrentSize.Value);     // ... velikost pro pro Panel2: SplitterPosition definuje ten druhý panel (počítáme Fixed = Panel1)
            }
            if (parameters.PreviousSize.HasValue && parameters.PreviousSize.Value > 0)
            {
                if (parameters.NewPanelIsPanel2) return parameters.PreviousSize.Value;
                if (parameters.NewPanelIsPanel1) return (size - parameters.PreviousSize.Value);
            }

            // Ratio hodnota = poměr z celé veliikosti panelu:
            if (parameters.CurrentSizeRatio.HasValue && parameters.CurrentSizeRatio.Value > 0f)
            {
                float ratio = (parameters.CurrentSizeRatio.Value < 1f ? parameters.CurrentSizeRatio.Value : 1f);
                float fsize = (float)size;
                if (parameters.NewPanelIsPanel1) return (int)(ratio * fsize);
                if (parameters.NewPanelIsPanel2) return (int)((1f - ratio) * fsize);
            }
            if (parameters.PreviousSizeRatio.HasValue && parameters.PreviousSizeRatio.Value > 0f)
            {
                float ratio = (parameters.PreviousSizeRatio.Value < 1f ? parameters.PreviousSizeRatio.Value : 1f);
                float fsize = (float)size;
                if (parameters.NewPanelIsPanel2) return (int)(ratio * fsize);
                if (parameters.NewPanelIsPanel1) return (int)((1f - ratio) * fsize);
            }

            return size / 2;
        }
        /// <summary>
        /// Metoda určí, zda dodaný <paramref name="parent"/> je jedním z panelů (<see cref="DevExpress.XtraEditors.SplitGroupPanel"/>) nějakého <see cref="DevExpress.XtraEditors.SplitContainerControl"/>.
        /// Pokud ano, pak do out parametrů vloží ten <see cref="DevExpress.XtraEditors.SplitContainerControl"/> a párový panel (Pokud na vstupu je Panel1, pak <paramref name="pairPanel"/> bude Panel2, a naopak), a vrátí true.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="splitContainer"></param>
        /// <param name="pairPanel"></param>
        /// <returns></returns>
        private bool _IsParentSplitPanel(Control parent, out DevExpress.XtraEditors.SplitContainerControl splitContainer, out DevExpress.XtraEditors.SplitGroupPanel pairPanel)
        {
            splitContainer = null;
            pairPanel = null;
            if (!(parent is DevExpress.XtraEditors.SplitGroupPanel parentPanel)) return false;

            if (!(parentPanel.Parent is DevExpress.XtraEditors.SplitContainerControl scc)) return false;
            splitContainer = scc;

            if (Object.ReferenceEquals(splitContainer.Panel1, parent))
            {
                pairPanel = splitContainer.Panel2;
                return true;
            }

            if (Object.ReferenceEquals(splitContainer.Panel2, parent))
            {
                pairPanel = splitContainer.Panel1;
                return true;
            }

            return false;
        }
        /// <summary>
        /// Vrátí index daného User controlu v poli zdejších controlů.
        /// </summary>
        /// <param name="userControl"></param>
        /// <returns></returns>
        private int _GetIndexOfUserControl(Control userControl)
        {
            if (userControl == null || _Controls.Count == 0) return - 1;
            return _Controls.FindIndex(c => c.ContainsUserControl(userControl));
        }
        /// <summary>
        /// Pole User controlů, v páru s jejich posledně známým parentem
        /// </summary>
        private List<ControlParentInfo> _Controls;
        #endregion
        #region Interaktivita vnitřní
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// Zavolá event <see cref="UserControlRemoved"/>.
        /// </summary>
        protected virtual void OnUserControlRemoved(Control control)
        {
            UserControlRemoved?.Invoke(this, new TEventArgs<Control>(control));
        }
        /// <summary>
        /// Vyvolá se po odebrání posledního controlu.
        /// Zavolá event <see cref="LastControlRemoved"/>.
        /// </summary>
        protected virtual void OnLastControlRemoved()
        {
            LastControlRemoved?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Vyvolá se po pohybu splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterMoved(object sender, EventArgs e)
        {
            if (sender is DevExpress.XtraEditors.SplitContainerControl splitContainer)
            {
                UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);
                OnSplitterPositionChanged(pair.CreateSplitterChangedArgs());
            }
        }
        /// <summary>
        /// Provede se po změně pozice splitteru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSplitterPositionChanged(DxLayoutPanelSplitterChangedArgs args)
        {
            SplitterPositionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se po změně orientace splitteru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSplitterOrientationChanged(DxLayoutPanelSplitterChangedArgs args)
        {
            SplitterOrientationChanged?.Invoke(this, args);
        }
        /// <summary>
        /// MouseDown: odchytí pravou myš a zobrazí řídící menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitContainerMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && sender is DevExpress.XtraEditors.SplitContainerControl splitContainer)
            {
                var splitterBounds = splitContainer.SplitterBounds;
                var mousePoint = e.Location;
                if (splitterBounds.Contains(mousePoint))
                {
                    this._SplitContainerShowContextMenu(splitContainer, mousePoint);
                }
            }
        }
        /// <summary>
        /// Zobrazí kontextové menu pro splitter: Horizontální / Vertikální
        /// </summary>
        /// <param name="splitContainer"></param>
        /// <param name="mousePoint"></param>
        private void _SplitContainerShowContextMenu(DevExpress.XtraEditors.SplitContainerControl splitContainer, Point mousePoint)
        {
            UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);

            DevExpress.XtraBars.PopupMenu menu = _CreateContextMenu(pair);
            BarManager.SetPopupContextMenu(this, menu);

            Point absolutePoint = splitContainer.PointToScreen(mousePoint);
            absolutePoint.X = (absolutePoint.X < 20 ? 0 : absolutePoint.X - 20);
            absolutePoint.Y = (absolutePoint.Y < 10 ? 0 : absolutePoint.Y - 10);
            menu.ShowPopup(BarManager, absolutePoint);
        }
        /// <summary>
        /// Vytvoří a vrátí kontextovém menu pro splitter: Horizontální / Vertikální
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        private DevExpress.XtraBars.PopupMenu _CreateContextMenu(UserControlPair pair)
        {
            DevExpress.XtraBars.PopupMenu menu = new DevExpress.XtraBars.PopupMenu
            {
                DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True,
                DrawMenuRightIndent = DevExpress.Utils.DefaultBoolean.True,
                MenuDrawMode = DevExpress.XtraBars.MenuDrawMode.SmallImagesText,
                Name = "menu"
            };

            menu.AddItem(new DevExpress.XtraBars.BarHeaderItem() { Caption = _ContextMenuTitleText });

            bool isHorizontal = (pair.SplitContainer.Horizontal);

            DevExpress.XtraBars.BarCheckItem itemH = new DevExpress.XtraBars.BarCheckItem(_BarManager) { Name = _ContextMenuHorizontalName, Caption = _ContextMenuHorizontalText, Hint = _ContextMenuHorizontalToolTip, Glyph = _ContextMenuHorizontalGlyph, Tag = pair, Checked = isHorizontal };
            itemH.ItemAppearance.Normal.FontStyleDelta = (isHorizontal ? FontStyle.Bold : FontStyle.Regular);
            itemH.ItemClick += _ContextMenuItemClick;
            menu.AddItem(itemH);

            DevExpress.XtraBars.BarCheckItem itemV = new DevExpress.XtraBars.BarCheckItem(_BarManager) { Name = _ContextMenuVerticalName, Caption = _ContextMenuVerticalText, Hint = _ContextMenuVerticalToolTip, Glyph = _ContextMenuVerticalGlyph, Tag = pair, Checked = !isHorizontal };
            itemV.ItemAppearance.Normal.FontStyleDelta = (!isHorizontal ? FontStyle.Bold : FontStyle.Regular);
            itemV.ItemClick += _ContextMenuItemClick;
            menu.AddItem(itemV);

            var closeBtn = new DevExpress.XtraBars.BarButtonItem(_BarManager, _ContextMenuCloseText) { Name = _ContextMenuCloseName, Hint = _ContextMenuCloseToolTip, Glyph = _ContextMenuCloseGlyph };
            closeBtn.ItemClick += _ContextMenuItemClick;
            menu.AddItem(closeBtn);
            closeBtn.Links[0].BeginGroup = true;

            return menu;
        }
        private string _ContextMenuTitleText { get { return "Orientace"; } }
        private const string _ContextMenuHorizontalName = "Horizontal";
        private string _ContextMenuHorizontalText { get { return "Vedle sebe"; } }
        private string _ContextMenuHorizontalToolTip { get { return "Panely vlevo a vpravo, oddělovač je svislý"; } }
        private Image _ContextMenuHorizontalGlyph { get { return TestDevExpress.Properties.Resources.distribute_horizontal_margin_24_; } }
        private const string _ContextMenuVerticalName = "Vertical";
        private string _ContextMenuVerticalText { get { return "Pod sebou"; } }
        private string _ContextMenuVerticalToolTip { get { return "Panely nahoře a dole, oddělovač je vodorovný"; } }
        private Image _ContextMenuVerticalGlyph { get { return TestDevExpress.Properties.Resources.distribute_vertical_margin_24_; } }
        private const string _ContextMenuCloseName = "Vertical";
        private string _ContextMenuCloseText { get { return "Zavřít"; } }
        private string _ContextMenuCloseToolTip { get { return "Zavře nabídku bez změny vzhledu"; } }
        private Image _ContextMenuCloseGlyph { get { return TestDevExpress.Properties.Resources.dialog_no_2_24_; } }
        /// <summary>
        /// Po kliknutí na položku kontextového menu pro splitter: Horizontální / Vertikální
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ContextMenuItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            UserControlPair pair = e.Item.Tag as UserControlPair;
            bool hasPair = (pair != null);
            switch (e.Item.Name)
            {
                case _ContextMenuHorizontalName:
                    if (hasPair)
                        _SetOrientation(pair, true);
                    break;
                case _ContextMenuVerticalName:
                    if (hasPair)
                        _SetOrientation(pair, false);
                    break;
            }
        }
        /// <summary>
        /// Provede změnu orientace splitteru, včetně vyvolání eventu
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="horizontal"></param>
        private void _SetOrientation(UserControlPair pair, bool horizontal)
        {
            if (pair == null || pair.SplitContainer.Horizontal == horizontal) return;

            pair.SplitContainer.Horizontal = horizontal;

            DxLayoutPanelSplitterChangedArgs args = pair.CreateSplitterChangedArgs();
            OnSplitterOrientationChanged(args);
        }
        /// <summary>
        /// BarManager, OnDemand
        /// </summary>
        protected DevExpress.XtraBars.BarManager BarManager
        {
            get
            {
                if (_BarManager == null)
                    _InitializeBarManager();
                return _BarManager;
            }
        }
        private DevExpress.XtraBars.BarManager _BarManager;
        private void _InitializeBarManager()
        {
            DevExpress.XtraBars.BarManager barManager = new DevExpress.XtraBars.BarManager
            {
                Form = this,
                ToolTipController = new DevExpress.Utils.ToolTipController()
            };
            barManager.ToolTipController.AddClientControl(this);

            barManager.ToolTipController.ShowShadow = true;
            barManager.ToolTipController.Active = true;
            barManager.ToolTipController.Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            barManager.ToolTipController.AutoPopDelay = 500;
            barManager.ToolTipController.InitialDelay = 800;
            barManager.ToolTipController.KeepWhileHovered = true;
            barManager.ToolTipController.ReshowDelay = 2000;
            barManager.ToolTipController.Rounded = true;
            barManager.ToolTipController.RoundRadius = 25;
            barManager.ToolTipController.ShowShadow = true;
            barManager.ToolTipController.ToolTipStyle = DevExpress.Utils.ToolTipStyle.Windows7;
            barManager.ToolTipController.ToolTipType = DevExpress.Utils.ToolTipType.Standard;

            _BarManager = barManager;
        }
        #endregion
        #region Třídy ControlParentPair (evidence UserControlů) a AddControlParams (parametry pro přidání UserControlu) a UserControlPair (data o jednom Splitteru)
        /// <summary>
        /// Třída obsahující WeakReference na 
        /// </summary>
        private class ControlParentInfo
        {
            public ControlParentInfo(Control parent, DxLayoutItemPanel hostControl, Control userControl)
            {
                _Parent = new WeakReference<Control>(parent);
                _HostControl = new WeakReference<DxLayoutItemPanel>(hostControl);
                _UserControl = new WeakReference<Control>(userControl);
            }
            private WeakReference<Control> _Parent;
            private WeakReference<DxLayoutItemPanel> _HostControl;
            private WeakReference<Control> _UserControl;
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Control: {UserControl?.Name}; Parent: {Parent?.Name}";
            }
            /// <summary>
            /// Vrátí true pokud v this objektu je jako <see cref="Parent"/> uložen dodaný objekt.
            /// </summary>
            /// <param name="testParent"></param>
            /// <returns></returns>
            public bool ContainsParent(Control testParent)
            {
                var myParent = this.Parent;
                return (testParent != null && myParent != null && Object.ReferenceEquals(myParent, testParent));
            }
            /// <summary>
            /// Vrátí true pokud v this objektu je jako <see cref="UserControl"/> uložen dodaný objekt.
            /// </summary>
            /// <param name="testControl"></param>
            /// <returns></returns>
            public bool ContainsUserControl(Control testControl)
            {
                var myControl = this.UserControl;
                return (testControl != null && myControl != null && Object.ReferenceEquals(myControl, testControl));
            }
            /// <summary>
            /// Parent prvek
            /// </summary>
            public Control Parent { get { return (_Parent.TryGetTarget(out var parent) ? parent : null); } set { _Parent = (value != null ? new WeakReference<Control>(value) : null); } }
            /// <summary>
            /// Hostitel pro control
            /// </summary>
            public DxLayoutItemPanel HostControl { get { return (_HostControl.TryGetTarget(out var control) ? control : null); } }
            /// <summary>
            /// Vlastní control
            /// </summary>
            public Control UserControl { get { return (_UserControl.TryGetTarget(out var control) ? control : null); } }
        }
        /// <summary>
        /// Parametry pro přidání controlu
        /// </summary>
        private class AddControlParams
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public AddControlParams()
            {
                Position = LayoutPosition.Right;
                IsSplitterFixed = false;
                FixedSize = null;
                FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1;
                MinSize = 100;
            }
            /// <summary>
            /// Defaultní parametry
            /// </summary>
            public static AddControlParams Default { get { return new AddControlParams(); } }
            /// <summary>
            /// POzice, na kterou bude umístěn nový control
            /// </summary>
            public LayoutPosition Position { get; set; }
            public bool IsSplitterFixed { get; set; }
            public int? FixedSize { get; set; }
            public int MinSize { get; set; }
            public DevExpress.XtraEditors.SplitFixedPanel FixedPanel { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)
            /// </summary>
            public int? PreviousSize { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v pixelech pro Current control, null = neřešit (dá 50%)
            /// </summary>
            public int? CurrentSize { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v poměru k celku pro Previous control, null = neřešit (dá 50%)
            /// </summary>
            public float? PreviousSizeRatio { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v poměru k celku pro Current control, null = neřešit (dá 50%)
            /// </summary>
            public float? CurrentSizeRatio { get; set; }
            /// <summary>
            /// Obsahuje true pokud panely jsou uspořádány horizontálně (Left a/nebo Right), tj. oddělovač je svislý
            /// </summary>
            public bool IsHorizontal { get { return (this.Position == LayoutPosition.Left || this.Position == LayoutPosition.Right); } }
            /// <summary>
            /// Obsahuje true pokud pozice nového panelu odpovídá Panel1 (Left nebo Top)
            /// </summary>
            public bool NewPanelIsPanel1 { get { return (this.Position == LayoutPosition.Left || this.Position == LayoutPosition.Top); } }
            /// <summary>
            /// Obsahuje true pokud pozice nového panelu odpovídá Panel2 (Right nebo Bottom)
            /// </summary>
            public bool NewPanelIsPanel2 { get { return (this.Position == LayoutPosition.Right || this.Position == LayoutPosition.Bottom); } }
        }
        /// <summary>
        /// Třída obsahující controly 1 a 2 a orientaci splitteru
        /// </summary>
        protected class UserControlPair
        {
            /// <summary>
            /// Vytvoří a vrátí instanci pro daný container
            /// </summary>
            /// <param name="splitContainer"></param>
            /// <returns></returns>
            public static UserControlPair CreateForContainer(DevExpress.XtraEditors.SplitContainerControl splitContainer)
            {
                if (splitContainer == null) return null;
                Control control1 = splitContainer.Panel1.Controls.Count > 0 ? splitContainer.Panel1.Controls[0] : null;
                Control control2 = splitContainer.Panel2.Controls.Count > 0 ? splitContainer.Panel2.Controls[0] : null;
                Orientation splitterOrietnation = splitContainer.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
                return new UserControlPair() { SplitContainer = splitContainer, Control1 = control1, Control2 = control2, SplitterOrientation = splitterOrietnation };
            }
            private UserControlPair()
            { }
            /// <summary>
            /// Container, kterého se data týkají
            /// </summary>
            public DevExpress.XtraEditors.SplitContainerControl SplitContainer { get; private set; }
            /// <summary>
            /// První control v Panel1
            /// </summary>
            public Control Control1 { get; private set; }
            /// <summary>
            /// První control v Panel2
            /// </summary>
            public Control Control2 { get; private set; }
            /// <summary>
            /// Orientace splitteru (té dělící čáry)
            /// </summary>
            public Orientation SplitterOrientation { get; private set; }
            /// <summary>
            /// Vrací instanci <see cref="DxLayoutPanelSplitterChangedArgs"/> ze zdejších dat
            /// </summary>
            /// <returns></returns>
            public DxLayoutPanelSplitterChangedArgs CreateSplitterChangedArgs()
            {
                DxLayoutPanelSplitterChangedArgs args = new DxLayoutPanelSplitterChangedArgs(Control1, Control2, SplitterOrientation, SplitContainer.SplitterPosition);
                return args;
            }
        }
        #endregion
    }
    /// <summary>
    /// Panel, který slouží jako hostitel pro jeden uživatelský control v rámci <see cref="DxLayoutPanel"/>.
    /// Obsahuje prostor pro titulek, zavírací křížek a OnMouse buttony pro předokování aktuálního prvku v rámci parent SplitContaineru.
    /// </summary>
    public class DxLayoutItemPanel : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutItemPanel()
        {
            this.Dock = DockStyle.Fill;
        }
        #region Titulkový panel a jeho buttony
        /// <summary>
        /// Titulkový panel je viditelný?
        /// </summary>
        public bool TitleBarVisible { get { return _TitleBarVisible; } set { this.RunInGui(() => _TitleBarSetVisible(value)); } }
        private bool _TitleBarVisible;
        public string TitleText { get { return _TitleText; } set { _TitleText = value; this.RunInGui(() => _TitleBarRefresh()); } }
        private string _TitleText;
        public bool CloseButtonVisible { get { return _CloseButtonVisible; } set { _CloseButtonVisible = value; this.RunInGui(() => _TitleBarRefresh()); } }
        private bool _CloseButtonVisible;
        public bool DockButtonsEnabled { get { return _DockButtonsEnabled; } set { _DockButtonsEnabled = value; this.RunInGui(() => _TitleBarRefresh()); } }
        private bool _DockButtonsEnabled;
        /// <summary>
        /// Zajistí správnou existenci a viditelnost titulkového baru podle jeho požadované viditelnosti, a jeho vložení do this Controls
        /// </summary>
        /// <param name="titleBarVisible"></param>
        private void _TitleBarSetVisible(bool titleBarVisible)
        {
            _TitleBarVisible = titleBarVisible;
            if (titleBarVisible)
            {   // Pokud má být viditelný:
                if (_TitleBar == null)
                    _TitleBarInit();
                _TitleBar.Visible = true;
            }
            else
            {   // Pokud nemá být viditelný:
                if (_TitleBar != null && _TitleBar.VisibleInternal != !titleBarVisible)
                    _TitleBar.VisibleInternal = !titleBarVisible;
            }
        }
        private void _TitleBarInit()
        {
            _TitleBar = new DxLayoutTitlePanel();

            _TitleBarRefresh();
            _FillControls();
            _TitleBar.DoLayout();
        }
        private void _TitleBarRefresh()
        {
            if (_TitleBar == null || !_TitleBarVisible) return;
         //   _TitleBar.Title = _TitleText ?? "";
         //   _CloseButton.Visible = _CloseButtonVisible;
        }
      
        private DxLayoutTitlePanel _TitleBar;
       
        #endregion
        #region Vnitřní události
        private void _ClickClose(object sender, EventArgs args)
        { }
        #endregion
        #region UserControl
        /// <summary>
        /// Uživatelský control, obsazuje většinu prostoru this panelu, pod titulkem
        /// </summary>
        public Control UserControl
        {
            get { return _UserControl; }
            set
            {
                Control userControl = _UserControl;
                if (userControl != null)
                {
                    this.Controls.Remove(userControl);
                    _UserControl = null;
                }
                userControl = value;
                _UserControl = userControl;
                if (userControl != null)
                {
                    userControl.Dock = DockStyle.Fill;
                    this._FillControls();
                }
            }
        }
        private Control _UserControl;
        /// <summary>
        /// Do this.Controls ve správném pořadí vloží existující prvky TitleBar a UserControl, před tím vyprázdní pole controlů.
        /// </summary>
        private void _FillControls()
        {
            this.SuspendLayout();

            //  Na pořadí při vkládání dokovaných prvků záleží, protože jinak bude size TitleBar nahoře a UserControl všude,
            // ale UserControl by při chybném pořadí měl UserControl.Top = 0, kdežto správně má mít UserControl.Top = TitleBar.Bottom !
            // Vkládáme jen ty prvky, které reálně existují. Neřešíme zde TitleBar.Visible.
            this.Controls.Clear();
            if (_UserControl != null) this.Controls.Add(_UserControl);
            if (_TitleBar != null) this.Controls.Add(_TitleBar);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
    public class DxLayoutTitlePanel : DxPanelControl
    {
        public DxLayoutTitlePanel()
        {
            this.Initialize();
        }
        private void Initialize()
        {
            _TitleLabel = DxComponent.CreateDxLabel(12, 6, 200, this, "", LabelStyleType.Title, hAlignment: HorzAlignment.Near, autoSizeMode: DevExpress.XtraEditors.LabelAutoSizeMode.Horizontal);

            _DockLeftButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickClose, resourceName: "svgimages/align/alignverticalleft.svg", visible: false);
            _DockTopButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickClose, resourceName: "svgimages/align/alignhorizontaltop.svg", visible: false);
            _DockBottomButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickClose, resourceName: "svgimages/align/alignhorizontalbottom.svg", visible: false);
            _DockRightButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickClose, resourceName: "svgimages/align/alignverticalright.svg", visible: false);

            _CloseButton = DxComponent.CreateDxMiniButton(200, 2, 24, 24, this, _ClickClose, resourceName: "images/xaf/templatesv2images/action_delete.svg");


            _DockLeftButton.Visible = true;
            _DockTopButton.Visible = true;
            _DockTopButton.Enabled = false;
            _DockBottomButton.Visible = true;
            _DockRightButton.Visible = true;

            this.Height = 35;
            this.Dock = DockStyle.Top;

            _TitleBarVisible = true;
        }
        private void _ClickClose(object sender, EventArgs args)
        { }
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayout();
        }
        public void DoLayout()
        {
            if (_TitleLabel == null || !_TitleBarVisible) return;
            int width = this.ClientSize.Width;
            int height = _TitleLabel.Bottom + 6;
            if (this.Height != height) this.Height = height;

            _TitleLabel.Width = (width - _TitleLabel.Left - 35);
            int y = (height - 24) / 2;
            int dx = 27;
            int x = width - (5 * dx) - 9;

            _DockLeftButton.Location = new Point(x, y); x += dx;
            _DockTopButton.Location = new Point(x, y); x += dx;
            _DockBottomButton.Location = new Point(x, y); x += dx;
            _DockRightButton.Location = new Point(x, y); x += dx + 6;
            _CloseButton.Location = new Point(x, y); x += dx;
        }
        private DxLabelControl _TitleLabel;
        private DxSimpleButton _DockLeftButton;
        private DxSimpleButton _DockTopButton;
        private DxSimpleButton _DockBottomButton;
        private DxSimpleButton _DockRightButton;
        private DxSimpleButton _CloseButton;


        private bool _TitleBarVisible;
    }
    #region Třídy pro eventy, enumy pro zadávání a pro eventy
    /// <summary>
    /// Informace předávaná po změně splitteru (pozice, orientace)
    /// </summary>
    public class DxLayoutPanelSplitterChangedArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="control1"></param>
        /// <param name="control2"></param>
        /// <param name="splitterOrientation"></param>
        /// <param name="splitterPosition"></param>
        public DxLayoutPanelSplitterChangedArgs(Control control1, Control control2, Orientation splitterOrientation, int splitterPosition)
        {
            this.Control1 = control1;
            this.Control2 = control2;
            this.SplitterOrientation = splitterOrientation;
            this.SplitterPosition = splitterPosition;
        }
        /// <summary>
        /// První control v Panel1 (ten vlevo nebo nahoře)
        /// </summary>
        public Control Control1 { get; private set; }
        /// <summary>
        /// První control v Panel2 (ten vpravo nebo dole)
        /// </summary>
        public Control Control2 { get; private set; }
        /// <summary>
        /// Orientace splitteru (té dělící čáry)
        /// </summary>
        public Orientation SplitterOrientation { get; private set; }
        /// <summary>
        /// Pozice splitteru
        /// </summary>
        public int SplitterPosition { get; set; }
    }
    /// <summary>
    /// Pozice, kam budeme přidávat nový control vzhledem ke stávajícímu controlu
    /// </summary>
    public enum LayoutPosition
    {
        /// <summary>
        /// Nikam
        /// </summary>
        None,
        /// <summary>
        /// Doprava od stávajícího (=svislý splitter)
        /// </summary>
        Right,
        /// <summary>
        /// Dolů od stávajícího (=vodorovný splitter)
        /// </summary>
        Bottom,
        /// <summary>
        /// Doleva od stávajícího (=svislý splitter)
        /// </summary>
        Left,
        /// <summary>
        /// Nahoru od stávajícího (=vodorovný splitter)
        /// </summary>
        Top
    }
    #endregion
}
