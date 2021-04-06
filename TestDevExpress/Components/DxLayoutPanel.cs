using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXB = DevExpress.XtraBars;
using DXE = DevExpress.XtraEditors;
using WF = System.Windows.Forms;

namespace TestDevExpress.Components
{
    /// <summary>
    /// Panel, který vkládá controly do svých rámečků se Splittery
    /// </summary>
    public class DxLayoutPanel : DXE.PanelControl
    {
        #region Public prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutPanel()
        {
            this.BorderStyle = DXE.Controls.BorderStyles.NoBorder;
            // this._RootContainer = null;
            this._Controls = new List<ControlParentInfo>();
        }
        /// <summary>
        /// Metoda najde a vrátí instanci <see cref="DxLayoutPanel"/>, do které patří daný control
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static DxLayoutPanel SearchParentLayoutPanel(WF.Control control)
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
        public WF.Control[] AllControls { get { return _GetAllControls(); } }
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// </summary>
        public event EventHandler<TEventArgs<WF.Control>> UserControlRemoved;
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
        /// <param name="control"></param>
        public void AddControl(WF.Control control)
        {
            if (control == null) return;
            _AddControlDefault(control, AddControlParams.Default);
        }
        /// <summary>
        /// Přidá nový control vedle controlu daného
        /// </summary>
        /// <param name="control"></param>
        /// <param name="previousControl"></param>
        /// <param name="position"></param>
        /// <param name="previousSize">Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)</param>
        /// <param name="currentSize"></param>
        public void AddControl(WF.Control control, WF.Control previousControl, LayoutPosition position, int? previousSize = null, int? currentSize = null)
        {
            if (control == null) return;

            AddControlParams parameters = new AddControlParams() { Position = position, PreviousSize = previousSize, CurrentSize = currentSize };
            int prevIndex = _GetIndexOfControl(previousControl);
            if (prevIndex < 0)
                _AddControlDefault(control, parameters);
            else
                _AddControlNear(control, prevIndex, parameters);
        }
        /// <summary>
        /// Přidá nový control na vhodné místo.
        /// Není určen Near control (tj. vedle kterého controlu má být nový umístěn).
        /// Typicky se používá pro první control, ten se vkládá přímo do this jako jediný.
        /// Může se použít i jindy, pak nový control přidá k posledně přidanému controlu.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parameters"></param>
        private void _AddControlDefault(WF.Control control, AddControlParams parameters)
        {
            int count = _Controls.Count;
            if (count == 0)
            {   // Zatím nemáme nic: nový control vložím přímo do this jako jediný (nebude se zatím používat SplitterContainer, není proč):
                // this._RemoveRootContainer();
                _AddControlTo(control, this);
            }
            else
            {   // Už něco máme, přidáme nový control poblíž posledního evidovaného prvku:
                _AddControlNear(control, count - 1, parameters);
            }
        }
        /// <summary>
        /// Dodaný control přidá poblíž jiného controlu, podle daných parametrů
        /// </summary>
        /// <param name="control"></param>
        /// <param name="nearIndex"></param>
        /// <param name="parameters"></param>
        private void _AddControlNear(WF.Control control, int nearIndex, AddControlParams parameters)
        {
            // 1. Prvek s indexem [parentIndex] obsahuje control, vedle kterého budeme přidávat nově dodaný control
            ControlParentInfo nearInfo = _Controls[nearIndex];
            WF.Control parent = nearInfo.Parent;

            // 2. Tento control tedy odebereme z jeho dosavadního parenta:
            WF.Control nearControl = nearInfo.Control;
            int idx = parent.Controls.IndexOf(nearControl);
            if (idx >= 0)
                parent.Controls.RemoveAt(idx);

            // 3. Do toho parenta vložíme místo controlu nový SplitterContainer a určíme panely pro stávající control a pro nový prvek (Panel1 a Panel2, podle parametru):
            DXE.SplitContainerControl newSplitContainer = _CreateNewContainer(parameters, out DXE.SplitGroupPanel currentControlPanel, out DXE.SplitGroupPanel newControlPanel);
            parent.Controls.Add(newSplitContainer);
            newSplitContainer.SplitterPosition = GetSplitterPosition(parent.ClientSize, parameters);         // Až po vložení do Parenta
            newSplitContainer.SplitterMoved += _SplitterMoved;                                               // Až po nastavení pozice

            // 4. Stávající prvek vložíme jako Child do jeho nově určeného panelu, a vepíšeme to i do evidence:
            currentControlPanel.Controls.Add(nearControl);
            nearInfo.Parent = currentControlPanel;

            // 5. Nový prvek vložíme do jeho panelu a vytvoříme pár a přidáme jej do evidence
            _AddControlTo(control, newControlPanel);
        }
        /// <summary>
        /// Přidá daný control do daného parenta jako jeho Child, dá Dock = Fill, a přidá informaci do evidence v <see cref="_Controls"/>.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        private void _AddControlTo(WF.Control control, WF.Control parent)
        {
            control.Dock = WF.DockStyle.Fill;
            parent.Controls.Add(control);
            ControlParentInfo pair = new ControlParentInfo(parent, control);
            _Controls.Add(pair);
        }
        /// <summary>
        /// Najde daný control ve své evidenci, a pokud tam je, pak jej odebere a jeho prostor uvolní pro nejbližšího souseda.
        /// </summary>
        /// <param name="control"></param>
        public void RemoveControl(WF.Control control)
        {
            int index = _GetIndexOfControl(control);
            _RemoveControl(index);
        }
        /// <summary>
        /// Odebere daný control z parenta (vizuálně) i z evidence (datově)
        /// </summary>
        /// <param name="removeIndex"></param>
        private void _RemoveControl(int removeIndex)
        {
            if (removeIndex < 0) return;                             // To není můj control

            var removeInfo = _Controls[removeIndex];
            var parent = removeInfo.Parent;                          // Parent daného controlu je typicky this (když control je jediný), anebo Panel1 nebo Panel2 nějakého SplitContaineru
            var removeControl = removeInfo.Control;
            if (parent != null && removeControl != null)
            {
                // 1. Odebereme daný control z jeho parenta a z evidence:
                parent.Controls.Remove(removeControl);
                _Controls.RemoveAt(removeIndex);
                OnUserControlRemoved(removeControl);

                // 2. Zjistíme, zda jeho parent je Panel1 nebo Panel2 z určitého SplitContaineru (najdeme SplitContainer a párový Panel) => pak najdeme párový Control a přemístíme jej nahoru:
                if (_IsParentSplitPanel(parent, out DXE.SplitContainerControl splitContainer, out DXE.SplitGroupPanel pairPanel))
                {
                    // 2a. Najdeme data o párovém panelu:
                    int pairIndex = _Controls.FindIndex(i => i.ContainsParent(pairPanel));
                    if (pairIndex < 0)
                    {   // Párový panel NEOBSAHUJE nijaký UserControl, měl by tedy obsahovat jiný SplitContainer, který obsahuje dva panely, v každém jeden UserControl.
                        // Najdeme tedy tento párový SplitContainer a přemístíme jej z pairPanel do splitContainer.Parent, a splitContainer (nyní už prázdný) jako takový zrušíme:
                        DXE.SplitContainerControl pairContainer = pairPanel.Controls.OfType<DXE.SplitContainerControl>().FirstOrDefault();
                        if (pairContainer != null)
                        {   // Našli jsme SplitContainerControl v sousedním panelu:
                            // Odebereme jej:
                            int pairIdx = pairPanel.Controls.IndexOf(pairContainer);
                            if (pairIdx >= 0)
                                pairPanel.Controls.RemoveAt(pairIdx);

                            // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                            var splitParent = splitContainer.Parent;
                            int contIdx = splitParent.Controls.IndexOf(splitContainer);
                            if (contIdx >= 0)
                                splitParent.Controls.RemoveAt(contIdx);

                            // Do parenta od dosavadního SplitContaineru vložíme ten párový control (ale nikam to nepíšeme, protože UserControly a jejich parenti se nemění):
                            splitParent.Controls.Add(pairContainer);
                        }
                    }
                    else
                    {   // Takže jsme odebrali prvek UserControl z jednoho panelu, a na párovém panelu máme jiný prvek UserControl.
                        // Nechceme mít prázdný panel, a ani nechceme prázdný panel schovávat, chceme mít čistý layout = viditelné panely a na nich jeden control!
                        // Najdeme tedy párový control, odebereme SplitContaner, a na jeho místo vložíme ten párový Control:
                        var pairInfo = _Controls[pairIndex];
                        var pairControl = pairInfo.Control;

                        // Odebereme párový control z jeho dosavadního parenta (kterým je párový panel):
                        int panelIdx = pairPanel.Controls.IndexOf(pairControl);
                        if (panelIdx >= 0)
                            pairPanel.Controls.RemoveAt(panelIdx);

                        // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                        var splitParent = splitContainer.Parent;
                        int contIdx = splitParent.Controls.IndexOf(splitContainer);
                        if (contIdx >= 0)
                            splitParent.Controls.RemoveAt(contIdx);

                        // Do parenta od dosavadního SplitContaineru vložíme ten párový control (a vepíšeme to do jeho Infa).
                        // Tento Parent je buď nějaký Panel1 nebo Panel2 od jiného SplitContaineru, anebo to může být this:
                        splitParent.Controls.Add(pairControl);
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
        /// Vrátí pole všech uživatelských controlů (neobsahuje tedy SplitContainery). Pole je lineární, nezohledňuje aktuální rozložení.
        /// </summary>
        /// <returns></returns>
        private WF.Control[] _GetAllControls()
        {
            List<WF.Control> controls = new List<WF.Control>();
            foreach (var controlPair in _Controls)
            {
                var control = controlPair.Control;
                if (control != null)
                    controls.Add(control);
            }
            return controls.ToArray();
        }
        /// <summary>
        /// Metoda vytvoří a vrátí nový <see cref="DXE.SplitContainerControl"/>.
        /// Současně určí (out parametry) panely, kam se má vložit stávající control a kam nový control, podle pozice v parametru <see cref="AddControlParams.Position"/>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="currentControlPanel"></param>
        /// <param name="newControlPanel"></param>
        /// <returns></returns>
        private DXE.SplitContainerControl _CreateNewContainer(AddControlParams parameters, out DXE.SplitGroupPanel currentControlPanel, out DXE.SplitGroupPanel newControlPanel)
        {
            var container = new DXE.SplitContainerControl() { Dock = WF.DockStyle.Fill };

            // parametry:
            container.IsSplitterFixed = parameters.IsSplitterFixed;
            container.FixedPanel = parameters.FixedPanel;
            container.Panel1.MinSize = parameters.MinSize;
            container.Panel2.MinSize = parameters.MinSize;

            // Horizontální panely (když se nový otevírá vlevo nebo vpravo):
            container.Horizontal = parameters.IsHorizontal;

            // Panely, do nichž se budou vkládat současný a nová control:
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
            int size = (parameters.IsHorizontal ? parentSize.Width : parentSize.Height);

            if (parameters.CurrentSize.HasValue && parameters.CurrentSize.Value > 0)
            {
                if (parameters.NewPanelIsPanel1) return parameters.CurrentSize.Value;
                if (parameters.NewPanelIsPanel2) return (size - parameters.CurrentSize.Value);
            }
            if (parameters.PreviousSize.HasValue && parameters.PreviousSize.Value > 0)
            {
                if (parameters.NewPanelIsPanel2) return parameters.PreviousSize.Value;
                if (parameters.NewPanelIsPanel1) return (size - parameters.PreviousSize.Value);
            }

            return size / 2;
        }
        /// <summary>
        /// Metoda určí, zda dodaný <paramref name="parent"/> je jedním z panelů (<see cref="DXE.SplitGroupPanel"/>) nějakého <see cref="DXE.SplitContainerControl"/>.
        /// Pokud ano, pak do out parametrů vloží ten <see cref="DXE.SplitContainerControl"/> a párový panel (Pokud na vstupu je Panel1, pak <paramref name="pairPanel"/> bude Panel2, a naopak), a vrátí true.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="splitContainer"></param>
        /// <param name="pairPanel"></param>
        /// <returns></returns>
        private bool _IsParentSplitPanel(WF.Control parent, out DXE.SplitContainerControl splitContainer, out DXE.SplitGroupPanel pairPanel)
        {
            splitContainer = null;
            pairPanel = null;
            if (!(parent is DXE.SplitGroupPanel parentPanel)) return false;

            if (!(parentPanel.Parent is DXE.SplitContainerControl scc)) return false;
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
        /// <param name="control"></param>
        /// <returns></returns>
        private int _GetIndexOfControl(WF.Control control)
        {
            if (control == null || _Controls.Count == 0) return - 1;
            return _Controls.FindIndex(c => c.ContainsControl(control));
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
        protected virtual void OnUserControlRemoved(WF.Control control)
        {
            UserControlRemoved?.Invoke(this, new TEventArgs<WF.Control>(control));
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
            if (sender is DXE.SplitContainerControl splitContainer)
            {
                UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);
                OnSplitterPositionChanged(pair);
            }
        }
        /// <summary>
        /// Provede se po změně pozice splitteru
        /// </summary>
        /// <param name="pair"></param>
        protected virtual void OnSplitterPositionChanged(UserControlPair pair)
        {
            if (SplitterPositionChanged != null)
                SplitterPositionChanged(this, pair.CreateSplitterChangedArgs());
        }
        /// <summary>
        /// Provede se po změně orientace splitteru
        /// </summary>
        /// <param name="pair"></param>
        protected virtual void OnSplitterOrientationChanged(UserControlPair pair)
        {
            if (SplitterOrientationChanged != null)
                SplitterOrientationChanged(this, pair.CreateSplitterChangedArgs());
        }
        /// <summary>
        /// MouseDown: odchytí pravou myš a zobrazí řídící menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitContainerMouseDown(object sender, WF.MouseEventArgs e)
        {
            if (e.Button == WF.MouseButtons.Right && sender is DXE.SplitContainerControl splitContainer)
            {
                var splitterBounds = splitContainer.SplitterBounds;
                var mousePoint = e.Location;
                if (splitterBounds.Contains(mousePoint))
                {
                    this._SplitContainerShowContextMenu(splitContainer, mousePoint);
                }
            }

        }
        private void _SplitContainerShowContextMenu(DXE.SplitContainerControl splitContainer, Point mousePoint)
        {
            UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);

            DXB.PopupMenu menu = _CreateContextMenu(pair);
            BarManager.SetPopupContextMenu(this, menu);

            Point absolutePoint = splitContainer.PointToScreen(mousePoint);
            absolutePoint.X = (absolutePoint.X < 20 ? 0 : absolutePoint.X - 20);
            absolutePoint.Y = (absolutePoint.Y < 10 ? 0 : absolutePoint.Y - 10);
            menu.ShowPopup(BarManager, absolutePoint);

        }
        private DXB.PopupMenu _CreateContextMenu(UserControlPair pair)
        {
            DXB.PopupMenu menu = new DXB.PopupMenu
            {
                DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True,
                DrawMenuRightIndent = DevExpress.Utils.DefaultBoolean.True,
                MenuDrawMode = DXB.MenuDrawMode.SmallImagesText,
                Name = "menu"
            };

            menu.AddItem(new DXB.BarHeaderItem() { Caption = "Orientace" });

            bool isHorizontal = (pair.SplitContainer.Horizontal);

            DXB.BarCheckItem itemH = new DXB.BarCheckItem(_BarManager) { Name = "Horizontal", Caption = "Vedle sebe", Hint = "Panely vlevo a vpravo, oddělovač je svislý", Glyph = TestDevExpress.Properties.Resources.distribute_horizontal_margin_24_, Tag = pair, Checked = isHorizontal };
            itemH.ItemClick += _ContextMenuItemClick;
            menu.AddItem(itemH);

            DXB.BarCheckItem itemV = new DXB.BarCheckItem(_BarManager) { Name = "Vertical", Caption = "Pod sebou", Hint = "Panely nahoře a dole, oddělovač je vodorovný", Glyph = TestDevExpress.Properties.Resources.distribute_vertical_margin_24_, Tag = pair, Checked = !isHorizontal };
            itemV.ItemClick += _ContextMenuItemClick;
            menu.AddItem(itemV);

            var closeBtn = new DXB.BarButtonItem(_BarManager, "Zavřít") { Name = "Close", Glyph = TestDevExpress.Properties.Resources.dialog_no_2_24_ };
            closeBtn.ItemClick += _ContextMenuItemClick;
            menu.AddItem(closeBtn);
            closeBtn.Links[0].BeginGroup = true;

            return menu;
        }

        private void _ContextMenuItemClick(object sender, DXB.ItemClickEventArgs e)
        {
            UserControlPair pair = e.Item.Tag as UserControlPair;
            bool hasPair = (pair != null);
            switch (e.Item.Name)
            {
                case "Horizontal":
                    if (hasPair)
                        _SetOrientation(pair, true);
                    break;
                case "Vertical":
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
            OnSplitterOrientationChanged(pair);
        }
        /// <summary>
        /// BarManager
        /// </summary>
        protected DXB.BarManager BarManager
        {
            get
            {
                if (_BarManager == null)
                    _InitializeBarManager();
                return _BarManager;
            }
        }
        private DXB.BarManager _BarManager;
        private void _InitializeBarManager()
        {
            DXB.BarManager barManager = new DXB.BarManager();
            barManager.Form = this;

            barManager.ToolTipController = new DevExpress.Utils.ToolTipController();
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
        #region Třídy ControlParentPair (evidence UserControlů) a AddControlParams (parametry pro přidání UserControlu)
        /// <summary>
        /// Třída obsahující WeakReference na 
        /// </summary>
        private class ControlParentInfo
        {
            public ControlParentInfo(WF.Control parent, WF.Control control)
            {
                _Parent = new WeakReference<WF.Control>(parent);
                _Control = new WeakReference<WF.Control>(control);
            }
            private WeakReference<WF.Control> _Parent;
            private WeakReference<WF.Control> _Control;
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"Control: {Control?.Name}; Parent: {Parent?.Name}";
            }
            /// <summary>
            /// Vrátí true pokud v this objektu je jako <see cref="Parent"/> uložen dodaný objekt.
            /// </summary>
            /// <param name="testParent"></param>
            /// <returns></returns>
            public bool ContainsParent(WF.Control testParent)
            {
                var myParent = this.Parent;
                return (testParent != null && myParent != null && Object.ReferenceEquals(myParent, testParent));
            }
            /// <summary>
            /// Vrátí true pokud v this objektu je jako <see cref="Control"/> uložen dodaný objekt.
            /// </summary>
            /// <param name="testControl"></param>
            /// <returns></returns>
            public bool ContainsControl(WF.Control testControl)
            {
                var myControl = this.Control;
                return (testControl != null && myControl != null && Object.ReferenceEquals(myControl, testControl));
            }
            /// <summary>
            /// Parent prvek
            /// </summary>
            public WF.Control Parent { get { return (_Parent.TryGetTarget(out var parent) ? parent : null); } set { _Parent = (value != null ? new WeakReference<WF.Control>(value) : null); } }
            /// <summary>
            /// Vlastní control
            /// </summary>
            public WF.Control Control { get { return (_Control.TryGetTarget(out var control) ? control : null); } }
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
                FixedPanel = DXE.SplitFixedPanel.Panel1;
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
            public DXE.SplitFixedPanel FixedPanel { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)
            /// </summary>
            public int? PreviousSize { get; set; }
            /// <summary>
            /// Nastavit tuto velikost v pixelech pro Current control, null = neřešit (dá 50%)
            /// </summary>
            public int? CurrentSize { get; set; }
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
            public static UserControlPair CreateForContainer(DXE.SplitContainerControl splitContainer)
            {
                if (splitContainer == null) return null;
                WF.Control control1 = splitContainer.Panel1.Controls.Count > 0 ? splitContainer.Panel1.Controls[0] : null;
                WF.Control control2 = splitContainer.Panel2.Controls.Count > 0 ? splitContainer.Panel2.Controls[0] : null;
                WF.Orientation splitterOrietnation = splitContainer.Horizontal ? WF.Orientation.Vertical : WF.Orientation.Horizontal;
                return new UserControlPair() { SplitContainer = splitContainer, Control1 = control1, Control2 = control2, SplitterOrientation = splitterOrietnation };
            }
            private UserControlPair()
            { }
            /// <summary>
            /// Container, kterého se data týkají
            /// </summary>
            public DXE.SplitContainerControl SplitContainer { get; private set; }
            /// <summary>
            /// První control v Panel1
            /// </summary>
            public WF.Control Control1 { get; private set; }
            /// <summary>
            /// První control v Panel2
            /// </summary>
            public WF.Control Control2 { get; private set; }
            /// <summary>
            /// Orientace splitteru (té dělící čáry)
            /// </summary>
            public WF.Orientation SplitterOrientation { get; private set; }
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
        public DxLayoutPanelSplitterChangedArgs (WF.Control control1, WF.Control control2, WF.Orientation splitterOrientation, int splitterPosition)
        {
            this.Control1 = control1;
            this.Control2 = control2;
            this.SplitterOrientation = splitterOrientation;
            this.SplitterPosition = splitterPosition;
        }
        /// <summary>
        /// První control v Panel1
        /// </summary>
        public WF.Control Control1 { get; private set; }
        /// <summary>
        /// První control v Panel2
        /// </summary>
        public WF.Control Control2 { get; private set; }
        /// <summary>
        /// Orientace splitteru (té dělící čáry)
        /// </summary>
        public WF.Orientation SplitterOrientation { get; private set; }
        /// <summary>
        /// Pozice splitteru
        /// </summary>
        public int SplitterPosition { get; private set; }

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
