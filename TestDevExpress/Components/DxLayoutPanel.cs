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
            this.SplitterContextMenuEnabled = false;
            this._Controls = new List<LayoutTileInfo>();
            this._DockButtonVisibility = ControlVisibility.Default;
            this._CloseButtonVisibility = ControlVisibility.Default;
            this._EmptyPanelButtons = EmptyPanelVisibleButtons.None;
            this._DockButtonLeftToolTip = null;
            this._DockButtonTopToolTip = null;
            this._DockButtonBottomToolTip = null;
            this._DockButtonRightToolTip = null;
            this._CloseButtonToolTip = null;
            this._UseSvgIcons = true;
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
        /// Pole všech uživatelských controlů (neobsahuje tedy SplitContainery). 
        /// Pole je lineární, nezohledňuje aktuální rozložení.
        /// Každý prvek pole obsahuje <see cref="DxLayoutItemInfo.AreaId"/> = ID prostoru v layoutu.
        /// Pole obsahuje jen ty prostory, kde je nějaký UserControl.
        /// <para/>
        /// Kompletní rozložení prostoru je popsáno v <see cref="XmlLayout"/>.
        /// </summary>
        public DxLayoutItemInfo[] DxLayoutItems { get { return GetLayoutData().Item2; } }
        /// <summary>
        /// Aktuální počet controlů
        /// </summary>
        public int ControlCount { get { return _Controls.Count; } }
        /// <summary>
        /// Je povoleno přemístění prvků pomocí Drag And Drop
        /// </summary>
        public bool DragDropEnabled { get; set; }
        /// <summary>
        /// Viditelnost buttonů Dock
        /// </summary>
        public ControlVisibility DockButtonVisibility { get { return _DockButtonVisibility; } set { _DockButtonVisibility = value; this.RunInGui(_RefreshControls); } }
        private ControlVisibility _DockButtonVisibility;
        /// <summary>
        /// Viditelnost buttonu Close
        /// </summary>
        public ControlVisibility CloseButtonVisibility { get { return _CloseButtonVisibility; } set { _CloseButtonVisibility = value; this.RunInGui(_RefreshControls); } }
        private ControlVisibility _CloseButtonVisibility;
        /// <summary>
        /// Jaké buttony budou zobrazeny na prázdných panelech
        /// </summary>
        public EmptyPanelVisibleButtons EmptyPanelButtons { get { return _EmptyPanelButtons; } set { _EmptyPanelButtons = value; this.RunInGui(_RefreshControls); } }
        private EmptyPanelVisibleButtons _EmptyPanelButtons;
        /// <summary>
        /// Tooltip na buttonu DockLeft
        /// </summary>
        public string DockButtonLeftToolTip { get { return _DockButtonLeftToolTip; } set { _DockButtonLeftToolTip = value; this.RunInGui(_RefreshControls); } }
        private string _DockButtonLeftToolTip;
        /// <summary>
        /// Tooltip na buttonu DockTop
        /// </summary>
        public string DockButtonTopToolTip { get { return _DockButtonTopToolTip; } set { _DockButtonTopToolTip = value; this.RunInGui(_RefreshControls); } }
        private string _DockButtonTopToolTip;
        /// <summary>
        /// Tooltip na buttonu DockBottom
        /// </summary>
        public string DockButtonBottomToolTip { get { return _DockButtonBottomToolTip; } set { _DockButtonBottomToolTip = value; this.RunInGui(_RefreshControls); } }
        private string _DockButtonBottomToolTip;
        /// <summary>
        /// Tooltip na buttonu DockRight
        /// </summary>
        public string DockButtonRightToolTip { get { return _DockButtonRightToolTip; } set { _DockButtonRightToolTip = value; this.RunInGui(_RefreshControls); } }
        private string _DockButtonRightToolTip;
        /// <summary>
        /// Tooltip na buttonu Close
        /// </summary>
        public string CloseButtonToolTip { get { return _CloseButtonToolTip; } set { _CloseButtonToolTip = value; this.RunInGui(_RefreshControls); } }
        private string _CloseButtonToolTip;
        /// <summary>
        /// Používat SVG ikony (true) / PNG ikony (false): default = true
        /// </summary>
        public bool UseSvgIcons { get { return _UseSvgIcons; } set { _UseSvgIcons = value; this.RunInGui(_RefreshControls); } }
        private bool _UseSvgIcons;
        /// <summary>
        /// Povolení pro zobrazování kontextového menu na Splitteru (pro změnu orientace Horizontální - Vertikální).
        /// Default = false.
        /// </summary>
        public bool SplitterContextMenuEnabled { get; set; }
        /// <summary>
        /// Vyvolá se po přidání nového controlu.
        /// </summary>
        public event EventHandler<TEventArgs<Control>> UserControlAdd;
        /// <summary>
        /// Vyvolá se po kliknutí na tlačítko 'CloseButton' na konkrétním panelu.
        /// Eventhandler může akci odebrání panelu zakázat = nastavením <see cref="TEventCancelArgs{T}.Cancel"/> na true.
        /// Pokud Cancel zůstane false, bude zahájeno odebrání panelu - stejně jako by kód vyvolal metodu <see cref="RemoveControl(Control)"/>,
        /// bude tedy vyvolán event <see cref="UserControlRemoveBefore"/> a následně i <see cref="UserControlRemoved"/>.
        /// <para/>
        /// Tento event <see cref="CloseButtonClickAfter"/> není volán z metody <see cref="RemoveControl(Control)"/>.
        /// </summary>
        public event EventHandler<TEventCancelArgs<Control>> CloseButtonClickAfter;
        /// <summary>
        /// Vyvolá se před odebráním prázdného prostoru (je vytvořen setováním XmlLayout, a dosud není obsazen) v layoutu.
        /// Aplikační kód tomu může zabránit nastavením Cancel = true.
        /// Pokud tomu aplikace nezabrání, pak aplikace nemůže do daného prostoru vložit UserControl, protože prostor nebude k dispozici.
        /// </summary>
        public event EventHandler<TEventCancelArgs<string>> EmptyAreaRemoveBefore;
        /// <summary>
        /// Vyvolá se před odebráním konkrétního uživatelského controlu. 
        /// Tato událost je vyvolána v průběhu provádění metody <see cref="RemoveControl(Control)"/> (a tedy i <see cref="RemoveAllControls()"/>).
        /// Je tedy volána i po kliknutí na tlačítko "Zavřít panel".
        /// Eventhandler může odebrání zakázat nastavením <see cref="TEventCancelArgs{T}.Cancel"/> na true,
        /// </summary>
        public event EventHandler<TEventCancelArgs<Control>> UserControlRemoveBefore;
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
        public event EventHandler<DxLayoutPanelSplitterChangedArgs> LayoutPanelChanged;
        /// <summary>
        /// Událost vyvolaná po každé změně <see cref="XmlLayout"/>, volá se vždy po:
        /// Přidání / odebrání controlu;
        /// Změna dokování;
        /// Změna pozice splitteru
        /// </summary>
        public event EventHandler XmlLayoutChanged;
        #endregion
        #region Přidání UserControlů, refresh titulku, odebrání a evidence UserControlů
        /// <summary>
        /// Vrátí true, pokud aktuálně existující layout obsahuje prostor daného klíče <paramref name="areaId"/> a pokud je možno do něj vložit UserControl.
        /// Parametr <paramref name="removeOld"/> specifikuje, zda lze akceptovat prostor, který už nějaký UserControl obsahuje.
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="removeOld"></param>
        /// <returns></returns>
        public bool CanAddUserControlTo(string areaId, bool removeOld = false)
        {
            if (String.IsNullOrEmpty(areaId)) return false;

            var hosts = GetLayoutData().Item3;                                 // Stávající struktura layoutu, obsahuje klíče AreaId a odpovídající panely
            if (!hosts.TryGetValue(areaId, out var hostInfo)) return false;    // Když ve struktuře vůbec není daný prostor...

            if (hostInfo.Parent.Controls.Count == 0) return true;              // Prostor tam je a je prázdný
            switch (hostInfo.ChildType)
            {
                case AreaContentType.Empty: return true;                       // Prázdno
                case AreaContentType.DxSplitContainer:
                case AreaContentType.WfSplitContainer: return false;           // Nějaký vnořený SplitContainer: to nejde použít pro UserControl.
                case AreaContentType.DxLayoutItemPanel: return removeOld;      // Pokud je tam control: vracím true (=Mohu přidat nový UserControl) tehdy, když je povoleno stávající UserControl odebrat.
            }
            return false;
        }
        /// <summary>
        /// Metoda přidá daný control do layoutu.
        /// Typicky se používá pro první control, ale může být použita pro kterýkoli další. Pak se přidá za posledně přidaný doprava.
        /// <para/>
        /// Pokud dodaný UserControl implementuje <see cref="ILayoutUserControl"/>, pak zdejší instance se stará o aktuálnost titulku.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="titleText"></param>
        public void AddControl(Control userControl, string titleText = null)
        {
            if (userControl == null) return;
            AddControlParams parameters = new AddControlParams() { TitleText = titleText };
            _AddControlDefault(userControl, parameters);
        }
        /// <summary>
        /// Přidá nový control vedle controlu daného, s danými parametry.
        /// <para/>
        /// Pokud dodaný UserControl implementuje <see cref="ILayoutUserControl"/>, pak zdejší instance se stará o aktuálnost titulku.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="previousControl"></param>
        /// <param name="position"></param>
        /// <param name="titleText"></param>
        /// <param name="fixedPanel"></param>
        /// <param name="isFixedSplitter"></param>
        /// <param name="previousSize">Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)</param>
        /// <param name="currentSize"></param>
        /// <param name="previousSizeRatio"></param>
        /// <param name="currentSizeRatio"></param>
        public void AddControl(Control userControl, Control previousControl, LayoutPosition position,
            string titleText = null,
            DevExpress.XtraEditors.SplitFixedPanel fixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, bool isFixedSplitter = false,
            int? previousSize = null, int? currentSize = null, float? previousSizeRatio = null, float? currentSizeRatio = null)
        {
            if (userControl == null) return;

            AddControlParams parameters = new AddControlParams() { Position = position, 
                TitleText = titleText,
                FixedPanel = fixedPanel, IsSplitterFixed = isFixedSplitter,
                PreviousSize = previousSize, CurrentSize = currentSize, PreviousSizeRatio = previousSizeRatio, CurrentSizeRatio = currentSizeRatio };
            int prevIndex = _GetIndexOfUserControl(previousControl);
            if (prevIndex < 0)
                _AddControlDefault(userControl, parameters);
            else
                _AddControlNear(userControl, prevIndex, parameters);
        }
        /// <summary>
        /// Vloží daný UserControl do daného prostoru.
        /// Kód prostoru je určen pomocí layoutu, viz property <see cref="DxLayoutItems"/>.
        /// Prostor má být před voláním této metody prázdný, anebo lze předat <paramref name="removeOld"/> = true, pak bude control odebrán a nahrazen novým.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="areaId"></param>
        /// <param name="titleText"></param>
        /// <param name="removeOld"></param>
        public void AddControlToArea(Control userControl, string areaId, string titleText = null, bool removeOld = false)
        {
            if (userControl == null) throw new ArgumentNullException("DxLayoutPanel.AddControlToArea() error: 'userControl' is null.");
            if (String.IsNullOrEmpty(areaId)) throw new ArgumentNullException("DxLayoutPanel.AddControlToArea() error: 'areaId' is empty.");
            var hosts = GetLayoutData().Item3;              // Stávající struktura layoutu, obsahuje klíče AreaId a odpovídající panely
            if (!hosts.TryGetValue(areaId, out var hostInfo)) throw new ArgumentNullException($"DxLayoutPanel.AddControlToArea() error: 'areaId' = '{areaId}' does not exists.");

            if (hostInfo.Parent.Controls.Count > 0)
            {   // Hele, ono tam (v požadovaném AreaId) už něco je!!!  Co s tím?
                if (hostInfo.ChildType == AreaContentType.DxSplitContainer || hostInfo.ChildType == AreaContentType.WfSplitContainer)
                    throw new ArgumentNullException($"DxLayoutPanel.AddControlToArea() error: 'areaId' = '{areaId}' contains any container ({hostInfo.ChildType}), can not insert any UserControl.");
                if (hostInfo.ChildType == AreaContentType.DxLayoutItemPanel && !removeOld)
                    throw new ArgumentNullException($"DxLayoutPanel.AddControlToArea() error: 'areaId' = '{areaId}' contains any UserControl, and is not specified removeOld=true, can not insert any UserControl.");
                if (hostInfo.ChildType == AreaContentType.DxLayoutItemPanel || hostInfo.ChildType == AreaContentType.EmptyLayoutPanel)
                    _RemoveUserControlOnly(hostInfo.ChildItemPanel);
                hostInfo.Parent.Controls.Clear();
            }

            _AddControlTo(userControl, hostInfo.Parent, (ControlCount == 0), titleText);
        }
        /// <summary>
        /// Přidá daný <paramref name="userControl"/> do daného <paramref name="parent"/>, kterým může být this, nebo Panel1 nebo Panel2 některého SplitContaineru.
        /// Podmínkou je, aby daný Parent dosud neobsahoval žádný child control!
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="parent"></param>
        /// <param name="isPrimaryPanel"></param>
        /// <param name="titleText"></param>
        protected void AddControlToParent(Control userControl, Control parent, bool isPrimaryPanel, string titleText)
        {
            if (userControl == null) throw new ArgumentNullException("DxLayoutPanel.AddControlToParent() error: 'userControl' is null.");
            if (parent == null) throw new ArgumentNullException("DxLayoutPanel.AddControlToParent() error: 'parent' is null.");
            if (parent.Controls.Count > 0) throw new ArgumentNullException("DxLayoutPanel.AddControlToParent() error: 'parent' already contains any child control.");

            _AddControlTo(userControl, parent, isPrimaryPanel, titleText);
        }
        /// <summary>
        /// Zkusí najít daný prostor a vrátit UserControl, pokud se tam nachází
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="userControl"></param>
        /// <returns></returns>
        public bool TryGetUserControl(string areaId, out Control userControl)
        {
            userControl = null;
            if (String.IsNullOrEmpty(areaId)) return false;

            var hosts = GetLayoutData().Item3;                                 // Stávající struktura layoutu, obsahuje klíče AreaId a odpovídající panely
            if (!hosts.TryGetValue(areaId, out var hostInfo)) return false;    // Když ve struktuře vůbec není daný prostor...

            if (hostInfo.ChildType != AreaContentType.DxLayoutItemPanel) return false;

            userControl = hostInfo.ChildItemPanel?.UserControl;
            return (userControl != null);
        }
        /// <summary>
        /// Zkusí najít UserControl, a vrátí jeho informace nebo nic
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="layoutItemInfo"></param>
        /// <returns></returns>
        public bool TryGetLayoutItemInfo(Control userControl, out DxLayoutItemInfo layoutItemInfo)
        {
            layoutItemInfo = null;
            if (userControl == null) return false;

            var dxLayoutItems = this.DxLayoutItems;
            layoutItemInfo = dxLayoutItems.FirstOrDefault(i => Object.ReferenceEquals(i.UserControl, userControl));

            return (layoutItemInfo != null);
        }
        /// <summary>
        /// Metoda najde panel s daným UserControlem, a zajistí aktualizaci jeho titulku.
        /// <para/>
        /// Pokud dodaný UserControl implementuje <see cref="ILayoutUserControl"/>, pak zdejší instance se stará o aktuálnost titulku sama.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="titleText"></param>
        public void UpdateTitle(Control userControl, string titleText)
        {
            int index = _GetIndexOfUserControl(userControl);
            if (index < 0) return;
            var tileInfo = _Controls[index];
            tileInfo.UpdateTitle(titleText);
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
        /// Metoda odebere všechny panely layoutu, počínaje od posledního.
        /// Typicky se volá před zavřením hostitelského okna.
        /// </summary>
        public void RemoveAllControls()
        {
            int count = this.ControlCount;
            for (int i = count - 1; i >= 0; i--)
                _RemoveUserControl(i);
        }
        /// <summary>
        /// Lze nastavit na true, pak nebudou prováděny žádné eventy
        /// </summary>
        public bool DisableAllEvents { get { return _AllEventsDisable; } set { _AllEventsDisable = value; } }
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
                _AddControlTo(userControl, this, true, parameters.TitleText);
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
            LayoutTileInfo nearInfo = _Controls[nearIndex];
            Control parent = nearInfo.Parent;

            // 2. Tento control (jeho hostitele) tedy odebereme z jeho dosavadního parenta:
            var nearHost = nearInfo.HostControl;                     // Podržím si referenci v lokální proměnné, jinak by mohl objekt zmizet, protože v nearInfo je jen WeakReference
            RemoveControlFromParent(nearHost, parent);

            // 3. Do toho parenta vložíme místo controlu nový SplitterContainer a určíme panely pro stávající control a pro nový prvek (Panel1 a Panel2, podle parametru):
            DevExpress.XtraEditors.SplitContainerControl newSplitContainer = _CreateNewContainer(parameters, parent, out DevExpress.XtraEditors.SplitGroupPanel currentControlPanel, out DevExpress.XtraEditors.SplitGroupPanel newControlPanel);

            // 4. Stávající prvek vložíme jako Child do jeho nově určeného panelu, a vepíšeme to i do evidence:
            currentControlPanel.Controls.Add(nearHost);
            nearInfo.Parent = currentControlPanel;
            nearInfo.DockButtonDisabledPosition = parameters.PairPosition;

            // 5. Nový UserControl vložíme do jeho panelu a vytvoříme pár a přidáme jej do evidence:
            bool isPrimaryPanel = (ControlCount == 0);
            _AddControlTo(userControl, newControlPanel, isPrimaryPanel, parameters.TitleText);
        }
        /// <summary>
        /// Přidá daný control do daného parenta jako jeho Child, dá Dock = Fill, a přidá informaci do evidence v <see cref="_Controls"/>.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="parent"></param>
        /// <param name="isPrimaryPanel"></param>
        /// <param name="titleText"></param>
        private void _AddControlTo(Control userControl, Control parent, bool isPrimaryPanel, string titleText)
        {
            DxLayoutItemPanel hostControl = new DxLayoutItemPanel(this);
            hostControl.UserControl = userControl;
            hostControl.TitleBarVisible = true;
            hostControl.TitleText = titleText;
            hostControl.IsPrimaryPanel = isPrimaryPanel;
            hostControl.DockButtonClick += ItemPanel_DockButtonClick;
            hostControl.CloseButtonClick += ItemPanel_CloseButtonClick;

            parent.Controls.Add(hostControl);
            hostControl.DockButtonDisabledPosition = hostControl.CurrentDockPosition;    // Až po vložení do parenta, protože podle něj se určuje CurrentDockPosition

            LayoutTileInfo tileInfo = new LayoutTileInfo(parent, hostControl, userControl);
            _Controls.Add(tileInfo);

            if (!_AllEventsDisable)
            {
                OnUserControlAdd(new TEventArgs<Control>(userControl));
                OnXmlLayoutChanged();
            }
        }
        /// <summary>
        /// Odebere daný control z parenta (vizuálně) i z evidence (datově).
        /// Před tím volá eventhandler <see cref="UserControlRemoveBefore"/> a reaguje na jeho požadavek Cancel (= pro Cancel = true přeruší proces odebrání panelu).
        /// </summary>
        /// <param name="removeIndex"></param>
        private void _RemoveUserControl(int removeIndex)
        {
            if (removeIndex < 0) return;                             // To není můj control
            var removeInfo = _Controls[removeIndex];
            bool enabled = _RemoveUserControlBefore(removeInfo);
            if (enabled)
                _RemoveUserControlInternal(removeIndex, removeInfo);
        }
        /// <summary>
        /// Metoda je volána pro daný panel layoutu před tím, než bude panel odebrán.
        /// Úkolem je zavolat eventhandler <see cref="UserControlRemoveBefore"/>.
        /// Metoda vrací true = je povoleno panel odebrat (vrací not Cancel).
        /// Pokud vrátí false, pak panel nelze odebrat a proces bude zastaven.
        /// </summary>
        /// <param name="removeInfo"></param>
        /// <returns></returns>
        private bool _RemoveUserControlBefore(LayoutTileInfo removeInfo)
        {
            var args = new TEventCancelArgs<Control>(removeInfo.UserControl);
            if (!_AllEventsDisable)
            {
                OnUserControlRemoveBefore(args);
            }
            return !args.Cancel;
        }
        /// <summary>
        /// Reálně odebere daný panel layoutu ze struktury (vizuálně i datově).
        /// </summary>
        /// <param name="removeIndex"></param>
        /// <param name="removeInfo"></param>
        private void _RemoveUserControlInternal(int removeIndex, LayoutTileInfo removeInfo)
        {
            var parent = removeInfo.Parent;                // Parent daného controlu je typicky this (když control je jediný), anebo Panel1 nebo Panel2 nějakého SplitContaineru
            var removeHost = removeInfo.HostControl;
            var removeControl = removeInfo.UserControl;
            bool removedLastControl = false;
            if (parent != null && removeHost != null)
            {
                _Controls.RemoveAt(removeIndex);           // Odebereme daný control z datové evidence
                removedLastControl = _RemovePanelFromParentWithLayout(removeHost);    // Odebereme hostitelský panel z jeho parenta a přeuspořádáme layout tak, aby obsadil uvolněné místo
                if (!_AllEventsDisable)
                {
                    OnUserControlRemoved(removeControl);   // Pošleme uživateli událost UserControlRemoved
                }
            }
            _RaiseEventsLayoutChange(removedLastControl);
        }
        /// <summary>
        /// Metoda odebere daný panel z layoutu, očekává se prázdný panel (nekontroluje se).
        /// </summary>
        /// <param name="removeHost"></param>
        private void _RemoveEmptyPanelFromParentWithLayout(DxLayoutItemPanel removeHost)
        {
            if (removeHost == null) return;

            bool cancel = false;
            if (!_AllEventsDisable)
            {
                string areaId = _SearchAreaIdForHost(removeHost);
                TEventCancelArgs<string> args = new TEventCancelArgs<string>(areaId);
                OnEmptyAreaRemoveBefore(args);
                cancel = args.Cancel;
            }
            if (!cancel)
            {
                bool removedLastControl = _RemovePanelFromParentWithLayout(removeHost);
                _RaiseEventsLayoutChange(removedLastControl);
            }
        }
        /// <summary>
        /// Odebere daný hostitelský panel z daného parenta.
        /// Najde parenta, najde párový panel a jeho obsah rozmístí tak, aby obsadil celý prostor (=zruší původní SplitContainer a na jeho místo dá obsah párového Panelu).
        /// Vrací true, pokud daný host byl posledním hostem layoutu.
        /// </summary>
        /// <param name="removeHost"></param>
        /// <returns></returns>
        private bool _RemovePanelFromParentWithLayout(DxLayoutItemPanel removeHost)
        {
            bool removedLastControl = false;

            // Jednoduše odeberu hostitele z jeho parenta (jako Control):
            Control parent = removeHost.Parent;
            RemoveControlFromParent(removeHost, parent);

            // Zjistíme, zda jeho parent je Panel1 nebo Panel2 z určitého SplitContaineru (najdeme SplitContainer a párový Panel) => pak najdeme párový Control a přemístíme jej nahoru:
            bool isDone = false;
            if (_IsParentSplitPanel(parent, out DevExpress.XtraEditors.SplitContainerControl splitContainer, out DevExpress.XtraEditors.SplitGroupPanel pairPanel))
            {   // Máme nalezený párový panel (pairPanel).
                // Měli bychom jej nějak spojit s panelem, z něhož byl nyní odebrán obsah:

                // 2a. Najdeme data o UserControlu v párovém panelu:
                int pairIndex = _Controls.FindIndex(i => i.ContainsParent(pairPanel));
                if (pairIndex >= 0)
                {   // Takže jsme odebrali prvek UserControl (pomocí jeho Host) z jednoho panelu, a na párovém panelu máme jiný prvek UserControl.
                    // Nechceme mít prázdný panel, a ani nechceme prázdný panel schovávat, chceme mít čistý layout = viditelné všechny panely a na nich vždy jeden control!
                    // Najdeme a podržíme si tedy onen párový control; pak odebereme SplitContaner, a na jeho místo vložíme ten párový Control:
                    // Vyřešíme i jeho DockPosition...
                    var pairInfo = _Controls[pairIndex];
                    var pairControl = pairInfo.UserControl;                // Po dobu výměny si podržíme reference v proměnných, protože v pairInfo jsou jen WeakReference!
                    var pairHost = pairInfo.HostControl;

                    // Odebereme párový control z jeho dosavadního parenta (kterým je párový panel):
                    RemoveControlFromParent(pairHost, pairPanel);

                    // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                    var splitParent = splitContainer.Parent;
                    RemoveControlFromParent(splitContainer, splitParent);

                    // Do parenta od dosavadního SplitContaineru vložíme ten párový control (a vepíšeme to do jeho Infa).
                    // Tento Parent je buď nějaký Panel1 nebo Panel2 od jiného SplitContaineru, anebo to může být this:
                    splitParent.Controls.Add(pairHost);
                    pairInfo.Parent = splitParent;
                    pairInfo.DockButtonDisabledPosition = pairInfo.CurrentDockPosition;

                    isDone = true;
                }

                if (!isDone)
                {   // V párovém panelu NEBYL UserControl, může tam být něco jiného:
                    // Může to být SplitContainer, nebo prázdný Panel, nebo jen Titulek, nebo něco jiného.
                    // Měli bychom tedy najít první control, který je umístěn v pairPanelu, a přemístit jej na celou plochu stávajícího Panel1 + Panel2:
                    Control pairControl = pairPanel.Controls.OfType<Control>().FirstOrDefault();
                    if (pairControl != null)
                    {   // Našli jsme jakýkoli Control v sousedním panelu:
                        // Odebereme jej z párového panelu:
                        RemoveControlFromParent(pairControl, pairPanel);

                        // Najdeme parenta od SplitContaineru, a z něj odebereme právě ten SplitContainer, tím jej zrušíme:
                        var splitParent = splitContainer.Parent;
                        RemoveControlFromParent(splitContainer, splitParent);

                        // Do parenta od dosavadního SplitContaineru vložíme ten párový control (ale nikam to nepíšeme, protože UserControly a jejich parenti se nemění):
                        // Nemění se ani pozice (DockPosition) controlů.
                        splitParent.Controls.Add(pairControl);

                        isDone = true;
                    }
                }
            }

            // Pokud parentem není SplitContainer, zjistíme zda Parent jsme my sami => pak jsme odebrali poslední control:
            else if (Object.ReferenceEquals(parent, this))
            {
                removedLastControl = true;
            }

            return removedLastControl;
        }
        /// <summary>
        /// Vyvolá události <see cref="XmlLayoutChanged"/> a volitelně <see cref="LastControlRemoved"/>, pokud nejsou eventy potlačeny
        /// </summary>
        /// <param name="removedLastControl"></param>
        private void _RaiseEventsLayoutChange(bool removedLastControl)
        {
            if (!_AllEventsDisable)
            {
                OnXmlLayoutChanged();
                if (removedLastControl)
                    this.OnLastControlRemoved();
            }
        }
        /// <summary>
        /// Odebere daný panel z evidence a z parenta, ale parenta ponechává beze změny na jeho dosavadním místě.
        /// Do parenta bude následně vkládán jiný UserControl.
        /// </summary>
        /// <param name="itemPanel"></param>
        private void _RemoveUserControlOnly(DxLayoutItemPanel itemPanel)
        {
            Control userControl = itemPanel.UserControl;
            bool hasUserControl = (userControl != null);
            if (hasUserControl) RemoveControlFromParent(userControl, itemPanel);

            RemoveControlFromParent(itemPanel, itemPanel.Parent);

            if (hasUserControl)
            {
                int removeIndex = _GetIndexOfUserControl(userControl);
                if (removeIndex >= 0)
                    _Controls.RemoveAt(removeIndex);

                if (!_AllEventsDisable)
                {
                    OnUserControlRemoved(userControl);
                }
            }
        }
        /// <summary>
        /// Daný control odebere z daného parenta, pokud je vše správně zadáno
        /// </summary>
        /// <param name="control"></param>
        /// <param name="parent"></param>
        internal static void RemoveControlFromParent(Control control, Control parent)
        {
            if (control != null && parent != null)
            {
                int index = parent.Controls.IndexOf(control);
                if (index >= 0)
                    parent.Controls.RemoveAt(index);
            }
        }
        /// <summary>
        /// Metoda vytvoří a vrátí nový <see cref="DevExpress.XtraEditors.SplitContainerControl"/>.
        /// Tento Container umístí do daného hostitele, nastaví mu korektní pozici splitteru a poté aktivuje jeho eventhandlery.
        /// Současně určí (out parametry) panely, kam se má vložit stávající control a kam nový control, podle pozice v parametru <see cref="AddControlParams.Position"/>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="host">Hostitel</param>
        /// <param name="currentControlPanel"></param>
        /// <param name="newControlPanel"></param>
        /// <returns></returns>
        private DevExpress.XtraEditors.SplitContainerControl _CreateNewContainer(AddControlParams parameters, Control host, out DevExpress.XtraEditors.SplitGroupPanel currentControlPanel, out DevExpress.XtraEditors.SplitGroupPanel newControlPanel)
        {
            var container = new DxSplitContainerControl() { Dock = DockStyle.Fill };

            // parametry:
            container.Horizontal = parameters.IsHorizontal;
            container.IsSplitterFixed = parameters.IsSplitterFixed;
            container.FixedPanel = parameters.FixedPanel;
            container.Panel1.MinSize = parameters.MinSize;
            container.Panel2.MinSize = parameters.MinSize;
            container.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;

            // Umístit do hostitele a podle jeho velikosti určit pozici splitteru:
            host.Controls.Add(container);
            container.SplitterPosition = _GetSplitterPosition(host.ClientSize, parameters);        // Až po vložení do Parenta
            container.MouseDown += _SplitContainerMouseDown;                                       // Eventhandlery až po nastavení pozice
            container.SplitterMoved += _SplitterMoved;

            // Out: Panely, do nichž se budou vkládat současný a nový control:
            bool newPositionIs2 = parameters.NewPanelIsPanel2;
            currentControlPanel = (newPositionIs2 ? container.Panel1 : container.Panel2);
            newControlPanel = (newPositionIs2 ? container.Panel2 : container.Panel1);

            return container;
        }
        /// <summary>
        /// Metoda vytvoří a vrátí nový <see cref="DevExpress.XtraEditors.SplitContainerControl"/>.
        /// Tento Container umístí do daného hostitele, nastaví mu korektní pozici splitteru a poté aktivuje jeho eventhandlery.
        /// </summary>
        /// <param name="area">Definice prostoru</param>
        /// <param name="host">Hostitel</param>
        /// <returns></returns>
        private DevExpress.XtraEditors.SplitContainerControl _CreateNewContainer(Area area, Control host)
        {
            var container = new DxSplitContainerControl() { Dock = DockStyle.Fill };

            // parametry:
            container.Horizontal = (area.SplitterOrientation == Orientation.Vertical);             // SplitterOrientation vyjadřuje pozici Splitteru, kdežto Horizontal vyjadřuje pozici panelů...
            container.IsSplitterFixed = area.IsSplitterFixed ?? false;
            container.FixedPanel = (area.FixedPanel.HasValue ? (area.FixedPanel == FixedPanel.Panel1 ? DevExpress.XtraEditors.SplitFixedPanel.Panel1 :
                                                               (area.FixedPanel == FixedPanel.Panel2 ? DevExpress.XtraEditors.SplitFixedPanel.Panel2 :
                                                                DevExpress.XtraEditors.SplitFixedPanel.None)) :
                                                             DevExpress.XtraEditors.SplitFixedPanel.Panel1);
            container.Panel1.MinSize = area.MinSize1 ?? 100;
            container.Panel2.MinSize = area.MinSize2 ?? 100;
            container.ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True;

            // Umístit do hostitele a podle jeho velikosti určit pozici splitteru:
            host.Controls.Add(container);
            container.SplitterPosition = _GetSplitterPosition(host.ClientSize, area);              // Až po vložení do Parenta
            container.MouseDown += _SplitContainerMouseDown;                                       // Eventhandlery až po nastavení pozice
            container.SplitterMoved += _SplitterMoved;

            return container;
        }
        /// <summary>
        /// Uloží informace o daném containeru do instance <see cref="Area"/> tak, aby později bylo možno vytvořit nový container podle těchto dat.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="area"></param>
        private void _FillContainerInfo(DevExpress.XtraEditors.SplitContainerControl container, Area area)
        {
            area.ContentType = AreaContentType.DxSplitContainer;
            area.SplitterOrientation = (container.Horizontal ? Orientation.Vertical : Orientation.Horizontal);    // SplitterOrientation vyjadřuje pozici Splitteru, kdežto Horizontal vyjadřuje pozici panelů...
            if (container.IsSplitterFixed) area.IsSplitterFixed = true;
            area.FixedPanel = (container.FixedPanel == DevExpress.XtraEditors.SplitFixedPanel.Panel1 ? FixedPanel.Panel1 :
                              (container.FixedPanel == DevExpress.XtraEditors.SplitFixedPanel.Panel2 ? FixedPanel.Panel2 :
                               FixedPanel.None));
            if (container.Panel1.MinSize != 100) area.MinSize1 = container.Panel1.MinSize;
            if (container.Panel2.MinSize != 100) area.MinSize2 = container.Panel2.MinSize;
            area.SplitterPosition = container.SplitterPosition;
            area.SplitterRange = (container.Horizontal ? container.Size.Width : container.ClientSize.Height);     // Vnější velikost containeru = ClientSize jeho hostitele
        }
        /// <summary>
        /// Vrátí pozici splitteru
        /// </summary>
        /// <param name="parentSize"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int _GetSplitterPosition(Size parentSize, AddControlParams parameters)
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
        /// Vrátí pozici splitteru
        /// </summary>
        /// <param name="parentSize"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private int _GetSplitterPosition(Size parentSize, Area area)
        {
            // Celková velikost v pixelech podle orientace splitteru: svislý splitter se pohybuje v rámci šířky prostoru:
            int size = (area.SplitterOrientation == Orientation.Vertical ? parentSize.Width : parentSize.Height);
            int position = area.SplitterPosition ?? (size / 2);
            int range = (area.SplitterRange ?? 1000);
            // Podle toho, který panel je fixní:
            switch (area.FixedPanel)
            {
                case FixedPanel.Panel1:
                    return position;
                case FixedPanel.Panel2:
                    int panel2Size = range - position;
                    return (size - panel2Size);
                default:
                    double ratio = (double)position / (double)range;
                    return (int)Math.Round((double)size * ratio, 0);
            }
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
        /// Pokud vrátí -1, pak control nebyl nalezen.
        /// </summary>
        /// <param name="userControl"></param>
        /// <returns></returns>
        private int _GetIndexOfUserControl(Control userControl)
        {
            if (userControl == null || _Controls.Count == 0) return -1;
            return _Controls.FindIndex(c => c.ContainsUserControl(userControl));
        }
        /// <summary>
        /// Vrátí <see cref="LayoutTileInfo"/> obsahující daný User controlu v poli zdejších controlů.
        /// Může vrátit null.
        /// </summary>
        /// <param name="userControl"></param>
        /// <returns></returns>
        private LayoutTileInfo _GetControlParentForUserControl(Control userControl)
        {
            int index = _GetIndexOfUserControl(userControl);
            return (index >= 0 ? _Controls[index] : null);
        }
        /// <summary>
        /// Najde index prvku, který obsahuje daný control na kterékoli pozici (<see cref="LayoutTileInfo.UserControl"/>, <see cref="LayoutTileInfo.HostControl"/>, <see cref="LayoutTileInfo.Parent"/>).
        /// Hledá i v rámci Parentů dodaného prvku.
        /// Pokud vrátí -1, pak control nebyl nalezen.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private int _SearchIndexOfAnyControl(Control control)
        {
            int index = -1;
            for (int t = 0; t < 50; t++)
            {   // t = timeout
                if (control == null) break;
                index = _Controls.FindIndex(c => c.ContainsAnyControl(control));
                if (index >= 0 || control.Parent == null) break;
                control = control.Parent;
            }
            return index;
        }
        /// <summary>
        /// Najde prvek <see cref="LayoutTileInfo"/> , který obsahuje daný control na kterékoli pozici (<see cref="LayoutTileInfo.UserControl"/>, <see cref="LayoutTileInfo.HostControl"/>, <see cref="LayoutTileInfo.Parent"/>).
        /// Hledá i v rámci Parentů dodaného prvku.
        /// Může vrátit null.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private LayoutTileInfo _SearchControlParentOfAnyControl(Control control)
        {
            int index = _SearchIndexOfAnyControl(control);
            return (index >= 0 ? _Controls[index] : null);
        }
        /// <summary>
        /// Refreshuje vlastnosti aktuálně přítomných controlů
        /// </summary>
        private void _RefreshControls()
        {
            foreach (LayoutTileInfo tileInfo in _Controls)
                _RefreshControl(tileInfo.HostControl);
        }
        /// <summary>
        /// Refreshuje vlastnosti daného controlu
        /// </summary>
        /// <param name="hostControl"></param>
        private void _RefreshControl(DxLayoutItemPanel hostControl)
        {
            hostControl?.RefreshControl();
        }
        /// <summary>
        /// Pole User controlů, v páru s jejich posledně známým parentem
        /// </summary>
        private List<LayoutTileInfo> _Controls;
        #endregion
        #region Obsluha titulkových tlačítek na prvku DxLayoutItemPanel
        /// <summary>
        /// Po kliknutí na DockButton v titulku: změní pozici odpovídajícího prvku layoutu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemPanel_DockButtonClick(object sender, DxLayoutTitleDockPositionArgs e)
        {
            int index = _SearchIndexOfAnyControl(sender as Control);
            if (index >= 0)
                _SetDockPosition(_Controls[index], e.DockPosition);
        }
        /// <summary>
        /// Po kliknutí na CloseButton v titulku: odebere odpovídající prvek layoutu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemPanel_CloseButtonClick(object sender, EventArgs e)
        {
            int index = _SearchIndexOfAnyControl(sender as Control);
            if (index >= 0)
            {
                LayoutTileInfo tileInfo = _Controls[index];
                var args = new TEventCancelArgs<Control>(tileInfo.UserControl);
                if (!_AllEventsDisable)
                {
                    this.OnCloseButtonClickAfter(args);
                }
                if (!args.Cancel)
                    _RemoveUserControl(index);
            }
        }
        #endregion
        #region Interaktivita vnitřní - reakce na odebrání controlu, na pohyb splitteru, na změnu orientace splitteru, na změnu dokování 
        /// <summary>
        /// Vyvolá se po přidání každého jednoho uživatelského controlu.
        /// Zavolá event <see cref="UserControlAdd"/>.
        /// </summary>
        protected virtual void OnUserControlAdd(TEventArgs<Control> args)
        {
            UserControlAdd?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// Zavolá event <see cref="UserControlRemoved"/>.
        /// </summary>
        protected virtual void OnCloseButtonClickAfter(TEventCancelArgs<Control> args)
        {
            CloseButtonClickAfter?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá se před odebráním prázdného panelu z layoutu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnEmptyAreaRemoveBefore(TEventCancelArgs<string> args)
        {
            EmptyAreaRemoveBefore?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// Zavolá event <see cref="UserControlRemoved"/>.
        /// </summary>
        protected virtual void OnUserControlRemoveBefore(TEventCancelArgs<Control> args)
        {
            UserControlRemoveBefore?.Invoke(this, args);
        }
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
        /// Provede se po změně pozice splitteru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnSplitterPositionChanged(DxLayoutPanelSplitterChangedArgs args)
        {
            SplitterPositionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Provede se po změně rozložení panelů pro dvě sousední stránky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnLayoutPanelChanged(DxLayoutPanelSplitterChangedArgs args)
        {
            LayoutPanelChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Vyvolá event <see cref="XmlLayoutChanged"/>
        /// </summary>
        protected virtual void OnXmlLayoutChanged()
        {
            XmlLayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// true = Dočasné potlačení volání události <see cref="OnSplitterPositionChanged(DxLayoutPanelSplitterChangedArgs)"/>.
        /// Používá se v době změny layoutu v metodě <see cref="_SetOrientation(UserControlPair, bool, bool)"/>, kdy se mění jak orientace, tak i pozice splitteru.
        /// Pak nechci volat samostatný event <see cref="SplitterPositionChanged"/>, protože bude následně volán event <see cref="LayoutPanelChanged"/>.
        /// </summary>
        private bool _SplitterMovedDisable;
        /// <summary>
        /// true = Dočasné potlačení volání všech událostí.
        /// Používá se v době aplikace nového layoutu v metodě <see cref="SetXmlLayout(string, IEnumerable{KeyValuePair{string, string}}, OrphanedControlMode, bool)"/>, kdy se mění všechno.
        /// </summary>
        private bool _AllEventsDisable;
        #endregion
        #region Kontextové menu pro změnu orientace splitteru, pohyb splitteru, změna dokování = výkonné akce
        /// <summary>
        /// MouseDown: odchytí pravou myš a zobrazí řídící menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitContainerMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && sender is DevExpress.XtraEditors.SplitContainerControl splitContainer && SplitterContextMenuEnabled)
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
        private Image _ContextMenuHorizontalGlyph { get { return DxComponent.GetImageFromResource("grayscaleimages/alignment/alignverticalcenter_16x16.png"); } }
        private const string _ContextMenuVerticalName = "Vertical";
        private string _ContextMenuVerticalText { get { return "Pod sebou"; } }
        private string _ContextMenuVerticalToolTip { get { return "Panely nahoře a dole, oddělovač je vodorovný"; } }
        private Image _ContextMenuVerticalGlyph { get { return DxComponent.GetImageFromResource("grayscaleimages/alignment/alignhorizontalcenter_16x16.png"); } }
        private const string _ContextMenuCloseName = "Vertical";
        private string _ContextMenuCloseText { get { return "Zavřít"; } }
        private string _ContextMenuCloseToolTip { get { return "Zavře nabídku bez změny vzhledu"; } }
        private Image _ContextMenuCloseGlyph { get { return DxComponent.GetImageFromResource("grayscaleimages/edit/delete_16x16.png"); } }
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
                        _SetOrientation(pair, true, false);
                    break;
                case _ContextMenuVerticalName:
                    if (hasPair)
                        _SetOrientation(pair, false, false);
                    break;
            }
        }
        /// <summary>
        /// Vyvolá se po pohybu splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterMoved(object sender, EventArgs e)
        {
            if (_SplitterMovedDisable) return;

            if (sender is DevExpress.XtraEditors.SplitContainerControl splitContainer)
            {
                if (!_AllEventsDisable)
                {
                    UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);
                    _FillControlParentsToPair(pair);
                    OnSplitterPositionChanged(pair.CreateSplitterChangedArgs());
                    OnXmlLayoutChanged();
                }
            }
        }
        /// <summary>
        /// Pro daný prvek (<paramref name="tileInfo"/>) nastaví jeho pozici dokování.
        /// </summary>
        /// <param name="tileInfo"></param>
        /// <param name="dockPosition"></param>
        private void _SetDockPosition(LayoutTileInfo tileInfo, LayoutPosition dockPosition)
        {
            if (tileInfo == null) return;
            DevExpress.XtraEditors.SplitContainerControl splitContainer = tileInfo.HostControl?.SearchForParentOfType<DevExpress.XtraEditors.SplitContainerControl>();
            if (splitContainer == null) return;

            // Není možné jen změnit příznak dokování, musíme změnit orientaci parent SplitContaineru a/nebo prohodit obsah panelů Panel1 <=> Panel2!
            bool horizontal = (dockPosition == LayoutPosition.Left || dockPosition == LayoutPosition.Right);
            LayoutPosition currentDockPosition = tileInfo.CurrentDockPosition;
            bool currentIsPanel1 = (currentDockPosition == LayoutPosition.Left || currentDockPosition == LayoutPosition.Top);
            bool targetIsPanel1 = (dockPosition == LayoutPosition.Left || dockPosition == LayoutPosition.Top);
            bool swap = (currentIsPanel1 != targetIsPanel1);

            UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);
            _SetOrientation(pair, horizontal, swap);
        }
        /// <summary>
        /// Provede změnu orientace splitteru, prohození controlů, včetně vyvolání eventu
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="horizontal">Nastavit orientaci Horizontal</param>
        /// <param name="swap">Prohodit navzájem obsah Panel1 - Panel2</param>
        private void _SetOrientation(UserControlPair pair, bool horizontal, bool swap)
        {
            if (pair == null || (pair.SplitContainer.Horizontal == horizontal && !swap)) return;
            _FillControlParentsToPair(pair);

            bool isChanged = false;
            bool splitterMovedDisable = _SplitterMovedDisable;
            try
            {
                _SplitterMovedDisable = true;
                using (this.ScopeSuspendParentLayout())
                    isChanged = pair.SetOrientation(horizontal, swap);
            }
            finally
            {
                _SplitterMovedDisable = splitterMovedDisable;
            }

            if (isChanged)
            {
                if (!_AllEventsDisable)
                {
                    DxLayoutPanelSplitterChangedArgs args = pair.CreateSplitterChangedArgs();
                    OnLayoutPanelChanged(args);
                    OnXmlLayoutChanged();
                }
            }
        }
        /// <summary>
        /// Do daného páru doplní <see cref="UserControlPair.TileInfo1"/> pro <see cref="UserControlPair.Control1"/> a totéž pro Control2.
        /// </summary>
        /// <param name="pair"></param>
        private void _FillControlParentsToPair(UserControlPair pair)
        {
            if (pair != null)
            {
                pair.TileInfo1 = _SearchControlParentOfAnyControl(pair.Control1);
                pair.TileInfo2 = _SearchControlParentOfAnyControl(pair.Control2);
            }
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
        #region XML Layout, Persistence layoutu
        /// <summary>
        /// Kompletní layout tohoto panelu.
        /// <para/>
        /// Layout lze setovat.
        /// Pokud v době setování nového layoutu jsou v obsazeny některé UserControly, pak budou přemístěny do stejnojmenného AreaId v novém layoutu 
        /// (pokud bude dostupný), anebo budou zahozeny.
        /// Je možno využít metodu <see cref="SetXmlLayout(string, IEnumerable{KeyValuePair{string, string}}, OrphanedControlMode, bool)"/> 
        /// a specifikovat tam mapu přemístění UserControlů i režim práce s neumístěnými UserControly.
        /// <para/>
        /// V době setování nejsou volány žádné eventy, i když probíhají veškeré události.
        /// <para/>
        /// UserControly, které do nového layoutu nebudou vloženy, budou na závěr uvolněny a bude volán event <see cref="UserControlRemoved"/>.
        /// </summary>
        public string XmlLayout { get { return GetLayoutData().Item1; } set { SetXmlLayout(value, null, OrphanedControlMode.ReleaseControls, false); } }
        /// <summary>
        /// Ověří, zda daný string může být akceptován jako <see cref="XmlLayout"/>
        /// </summary>
        /// <param name="xmlLayout"></param>
        /// <returns></returns>
        public static bool IsXmlLayoutValid(string xmlLayout)
        {
            if (String.IsNullOrEmpty(xmlLayout)) return false;
            try
            {
                Area area = Persist.Deserialize(xmlLayout) as Area;
                if (area != null) return true;
            }
            catch { }
            return false;
        }
        /// <summary>
        /// Ověří, zda daný string může být akceptován jako klíč prostoru AreaId
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        public static bool IsAreaIdValid(string areaId)
        {
            if (String.IsNullOrEmpty(areaId)) return false;
            var items = areaId.Split('/');
            return items.All(i => (i == "C" || i == "P1" || i == "P2"));
        }
        #region Get: čtení layoutu
        /// <summary>
        /// Zmapuje celý this layout.
        /// Vrátí tři prvky:
        /// Item1 = string XML layoutu celého panelu;
        /// Item2 = pole obsahující nalezené UserControly ve formě <see cref="DxLayoutItemInfo"/>;
        /// Item3 = Dictionary obsahující všechny Host controly a jejich ID. Host je this anebo každý Panel1 a Panel2 v celé stávající struktuře.
        /// </summary>
        /// <returns></returns>
        private (string, DxLayoutItemInfo[], Dictionary<string, HostAreaInfo>, Area) GetLayoutData()
        {
            var area = new Area();
            var items = new List<DxLayoutItemInfo>();
            var hosts = new Dictionary<string, HostAreaInfo>();
            GetXmlLayoutFillArea(area, this, "C", items, hosts);
            string xmlLayout = Persist.Serialize(area);
            var array = items.ToArray();
            return (xmlLayout, array, hosts, area);
        }
        /// <summary>
        /// Metoda provede analýzu obsahu daného controlu, kterým je container obsahující typicky jeden control.
        /// Tento první control detekuje a určí jeho typ (<see cref="DevExpress.XtraEditors.SplitContainerControl"/> nebo 
        /// <see cref="System.Windows.Forms.SplitContainer"/> nebo <see cref="DxLayoutItemPanel"/>.
        /// Do instance <paramref name="area"/> vepíše jeho typ a pokračuje rekurzivě analýzou panelů, pokud jde o SplitContainer.
        /// Pokud obnsahuje 
        /// </summary>
        /// <param name="area"></param>
        /// <param name="host"></param>
        /// <param name="areaId"></param>
        /// <param name="items">Průběžně vznikající lineární pole s UserControly a jejich AreaId</param>
        /// <param name="hosts">Průběžně vznikající lineární Dictionary se všemi Host controly, kde Key je jejich AreaId 
        /// a Value je Control, který v sobě může hostovat koncový panel typu <see cref="DxLayoutItemPanel"/>; 
        /// Value tedy je buď this, nebo Panel1 nebo Panel2 nějakého SplitContaineru.</param>
        private void GetXmlLayoutFillArea(Area area, Control host, string areaId, List<DxLayoutItemInfo> items, Dictionary<string, HostAreaInfo> hosts)
        {
            area.AreaId = areaId;
            HostAreaInfo hostInfo = new HostAreaInfo(areaId, host);
            hosts.Add(areaId, hostInfo);
            if (host.Controls.Count == 0)
            {
                area.ContentType = AreaContentType.None;
                return;
            }

            Control control = host.Controls[0];
            if (control is DevExpress.XtraEditors.SplitContainerControl dxSplit)
            {   // V našem hostiteli je SplitContainerControl od DevExpress:
                hostInfo.ChildContainer = dxSplit;
                _FillContainerInfo(dxSplit, area);

                area.Content1 = new Area();
                GetXmlLayoutFillArea(area.Content1, dxSplit.Panel1, areaId + "/P1", items, hosts);

                area.Content2 = new Area();
                GetXmlLayoutFillArea(area.Content2, dxSplit.Panel2, areaId + "/P2", items, hosts);
            }
            else if (control is System.Windows.Forms.SplitContainer wfSplit)
            {   // V našem hostiteli je SplitContainerControl od WinFormu:
                hostInfo.ChildContainer = wfSplit;
                area.ContentType = AreaContentType.WfSplitContainer;
                area.SplitterOrientation = wfSplit.Orientation;
                area.FixedPanel = wfSplit.FixedPanel;
                area.SplitterPosition = wfSplit.SplitterDistance;

                area.Content1 = new Area();
                GetXmlLayoutFillArea(area.Content1, wfSplit.Panel1, areaId + "/P1", items, hosts);

                area.Content2 = new Area();
                GetXmlLayoutFillArea(area.Content2, wfSplit.Panel2, areaId + "/P2", items, hosts);
            }
            else if (control is DxLayoutItemPanel itemPanel)
            {   // V našem hostiteli je Panel s UserControlem:
                hostInfo.ChildItemPanel = itemPanel;
                area.ContentType = (!itemPanel.IsEmpty ? AreaContentType.DxLayoutItemPanel : AreaContentType.EmptyLayoutPanel);
                Size areaSize = host.ClientSize;
                Control userControl = itemPanel.UserControl;
                bool isPrimaryPanel = itemPanel.IsPrimaryPanel;
                string titleText = itemPanel.TitleText;
                if (userControl != null)
                {
                    if (userControl is ILayoutUserControl iUserControl)
                    {
                        area.ControlId = iUserControl.Id;
                        area.ContentText = iUserControl.TitleText;
                    }
                    else 
                    {
                        area.ContentText = userControl.Text;
                    }
                    items.Add(new DxLayoutItemInfo(areaId, areaSize, area.ControlId, userControl, isPrimaryPanel, titleText));
                }
            }
            else
            {
                area.ContentType = AreaContentType.Empty;
            }
        }
        /// <summary>
        /// Najde a vrátí AreaId pro daný panel = obsah layoutu = hostitel pro titulek a UserControl
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private string _SearchAreaIdForHost(DxLayoutItemPanel host)
        {
            if (host == null) return null;
            var hosts = GetLayoutData().Item3;
            var found = hosts.FirstOrDefault(h => object.ReferenceEquals(h.Value.ChildItemPanel, host));
            return found.Value?.AreaId;
        }
        /// <summary>
        /// Informace o jednom každém prvku panelu a jeho obsazení
        /// </summary>
        private class HostAreaInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="areaId">ID prostoru</param>
            /// <param name="parent">Parent, kterým je this, nebo Panel1 nebo Panel2 nějakého SplitContaineru. Nikdy není null.</param>
            public HostAreaInfo(string areaId , Control parent)
            {
                this.AreaId = areaId;
                this.Parent = parent;
                this.ChildContainer = null;
                this.ChildItemPanel = null;
            }
            /// <summary>
            /// ID prostoru
            /// </summary>
            public string AreaId { get; private set; }
            /// <summary>
            /// Parent, kterým je this, nebo Panel1 nebo Panel2 nějakého SplitContaineru.
            /// Nikdy není null.
            /// </summary>
            public Control Parent { get; private set; }
            /// <summary>
            /// Druh obsahu
            /// </summary>
            public AreaContentType ChildType 
            { 
                get 
                {
                    if (ChildContainer != null)
                        return AreaContentType.DxSplitContainer;
                    if (ChildItemPanel != null)
                        return (!ChildItemPanel.IsEmpty ? AreaContentType.DxLayoutItemPanel : AreaContentType.EmptyLayoutPanel);
                    return AreaContentType.Empty; 
                }
            }
            /// <summary>
            /// Child prvek typu SplitContainer, nebo null
            /// </summary>
            public Control ChildContainer { get; set; }
            /// <summary>
            /// Child prvek typu DxLayoutItemPanel, nebo null
            /// </summary>
            public DxLayoutItemPanel ChildItemPanel { get; set; }
            /// <summary>
            /// Odstraní všechny Controly z parenta a ze svých referencí <see cref="ChildContainer"/> a <see cref="ChildItemPanel"/>
            /// </summary>
            public void ClearChilds()
            {
                this.ChildContainer = null;
                this.ChildItemPanel = null;
                var parent = this.Parent;
                if (parent != null)
                    parent.Controls.Clear();
            }
            /// <summary>
            /// Prostor je k dispozici pro umístění UserControlu (buď je zcela Empty, nebo obsahuje EmptyLayoutPanel).
            /// </summary>
            public bool IsDisponible
            {
                get
                {
                    var parent = this.Parent;
                    if (parent == null) return false;
                    var childType = this.ChildType;
                    return (childType == AreaContentType.Empty || childType == AreaContentType.EmptyLayoutPanel);
                }
            }
            /// <summary>
            /// Prostor je k dispozici pro cokoliv, neobsahuje žádný control
            /// </summary>
            public bool IsEmpty
            {
                get
                {
                    var parent = this.Parent;
                    if (parent == null) return false;
                    return (parent.Controls.Count == 0);
                }
            }
            /// <summary>
            /// Uvolní objekty v this prvku. Neprovádí Dispose.
            /// </summary>
            internal void ReleaseContent()
            {
                this.Parent = null;
                this.ChildContainer = null;
                this.ChildItemPanel = null;
            }
        }
        #endregion
        #region Set: aplikace layoutu
        /// <summary>
        /// Vloží daný layout do zdejšího objektu.
        /// Zahodí dosavadní layout.
        /// Pokusí se zachovat stávající UserControly: prioritně je po vložení nového layoutu umístí do stejnojmenných pozic (<see cref="DxLayoutItemInfo.AreaId"/>),
        /// </summary>
        /// <param name="xmlLayout"></param>
        /// <param name="areaIdMapping">Přechodová mapa (Key = staré AreaId, Value = nové AreaId)</param>
        /// <param name="lostControlMode">Režim práce s UserControly, které nejsou v mapě <paramref name="areaIdMapping"/></param>
        /// <param name="force">Vložit layout i když v něm nejsou změny? I pak může parametr <paramref name="lostControlMode"/> ovlivnit obsah okna</param>
        public void SetXmlLayout(string xmlLayout, IEnumerable<KeyValuePair<string, string>> areaIdMapping = null, OrphanedControlMode lostControlMode = OrphanedControlMode.ReleaseControls, bool force = false)
        {
            if (String.IsNullOrEmpty(xmlLayout))
                throw new ArgumentNullException($"Set to {_XmlLayoutName} error: value is empty.");

            Area area = Persist.Deserialize(xmlLayout) as Area;
            if (area == null)
                throw new ArgumentNullException($"Set to {_XmlLayoutName} error: XML string is not valid.");


            var layoutOld = GetLayoutData();           // Stávající layout, z něj využijeme strukturu (Item3) a následně pole UserControlů a jejich adres (Item2)
            if (!force)
            {   // Pokud není povinné změnit layout, a dodaný layout je obsahově identický, pak skončíme:
                if (Area.IsEqual(area, layoutOld.Item4)) return;
            }

            DxLayoutItemInfo[] lostControls = null;
            bool allEventsDisable = _AllEventsDisable;
            bool createEmptyControls = _CreateEmptyControlInEmptyPanel;
            try
            {
                using (this.ScopeSuspendParentLayout())
                {
                    _AllEventsDisable = true;
                    SetXmlLayoutClear(layoutOld.Item3);
                    SetXmlLayoutCreatePanels(area, createEmptyControls);    // Vytvoříme nové prázdné containery a jejich panely
                    var layoutNew = GetLayoutData();                      // Nový layout: využijeme z něj Item3 = nová struktura layoutu (klíče AreaId + hostitelé), ...

                    // Nyní vložíme stávající UserControly do jejich adres (podle mapy, podle parametru a podle starého seznamu Item2)
                    lostControls = SetXmlLayoutFill(layoutOld.Item2, layoutNew.Item3, areaIdMapping, lostControlMode);

                    // Hlášení zahozených UserControlů = přes eventy:
                    _AllEventsDisable = allEventsDisable;
                    ReportLostControls(lostControls);                     // Předáme aplikaci (skrze event UserControlRemoved) ty staré UserControly, které nebylo možno umístit do nového layoutu

                    // Uvolnění paměti (nikoli UserControlů):
                    ReleaseContent(layoutOld.Item2);
                    ReleaseContent(layoutOld.Item3.Values);
                    ReleaseContent(layoutNew.Item2);
                    ReleaseContent(layoutNew.Item3.Values);
                }
            }
            finally
            {
                _AllEventsDisable = allEventsDisable;
            }
        }
        /// <summary>
        /// Režim zpracování UserControlů při změně layoutu.
        /// Pokud se v <see cref="DxLayoutPanel"/> změní za provozu jeho <see cref="DxLayoutPanel.XmlLayout"/> (tedy když obsahuje nějaké UserControly),
        /// pak stávající controly je možno buď někam umístit, nebo zahodit.
        /// </summary>
        public enum OrphanedControlMode
        {
            /// <summary>
            /// Neurčeno, stávající UserControly budou zahozeny = stejně jako v <see cref="ReleaseControls"/>
            /// </summary>
            None,
            /// <summary>
            /// Stávající UserControly budou zahozeny 
            /// </summary>
            ReleaseControls,
            /// <summary>
            /// Stávající UserControly budou umístěny do nevyužitých prázdných políček, prioritně do stejnojmeného AreaId, pak do libovolného dalšího AreaId, 
            /// a pokud nebudou k dispozici, budou přidány do nově vytvořených políček
            /// </summary>
            MoveToUnusedArea,
            /// <summary>
            /// Stávající UserControly budou umístěny vždy do nově vytvořených políček
            /// </summary>
            MoveToNewArea
        }
        /// <summary>
        /// Korektně zruší stávající konstrukci layoutu
        /// </summary>
        /// <param name="hosts"></param>
        private void SetXmlLayoutClear(Dictionary<string, HostAreaInfo> hosts)
        {
            List<KeyValuePair<string, HostAreaInfo>> hostList = hosts.ToList();
            hostList.Sort((a, b) => CompareAreaIdDesc(a.Key, b.Key));
            foreach (var hostItem in hostList)
                SetXmlLayoutClearOne(hostItem.Value);
            _Controls.Clear();
        }
        /// <summary>
        /// Vymaže obsah daného hostitele (odebere všechny jeho Controls).
        /// Hostitelem je Panel1 nebo Panel2 nějakého SPlitContaineru, nebo this.
        /// </summary>
        /// <param name="hostInfo"></param>
        private void SetXmlLayoutClearOne(HostAreaInfo hostInfo)
        {
            DxLayoutItemPanel panel = hostInfo.ChildItemPanel;
            panel?.ReleaseUserControl();
            if (hostInfo.Parent.Controls.Count > 0)
                hostInfo.Parent.Controls.Clear();
            panel?.Dispose();
        }
        /// <summary>
        /// Třídí podle AreaId sestupně = na prvním místě bude nejhlubší Panel2, pak sousední Panel1...
        /// Vhodné pro postupné odebírání obsahu panelů
        /// </summary>
        /// <param name="areaId1"></param>
        /// <param name="areaId2"></param>
        /// <returns></returns>
        private static int CompareAreaIdDesc(string areaId1, string areaId2)
        {
            int len1 = areaId1.Length;
            int len2 = areaId2.Length;
            int cmp = len2.CompareTo(len1);
            if (cmp == 0)
                cmp = areaId2.CompareTo(areaId1);
            return cmp;
        }
        /// <summary>
        /// Vygeneruje nový layout, prázdný = bez UserControlů
        /// </summary>
        /// <param name="area"></param>
        /// <param name="createEmptyControls">Vytvářit titulkový panel</param>
        private void SetXmlLayoutCreatePanels(Area area, bool createEmptyControls)
        {
            SetXmlLayoutCreateContainer(this, area, createEmptyControls);
        }
        /// <summary>
        /// Vygeneruje do daného hostitele jednu úroveň layoutu, rekurzivní metoda
        /// </summary>
        /// <param name="host"></param>
        /// <param name="area"></param>
        /// <param name="createEmptyControls">Vytvářit titulkový panel</param>
        private void SetXmlLayoutCreateContainer(Control host, Area area, bool createEmptyControls)
        {
            if (area == null)
                throw new ArgumentException($"Set to {_XmlLayoutName} error: Area is null.");

            switch (area.ContentType)
            {
                case AreaContentType.DxSplitContainer:
                    var container = this._CreateNewContainer(area, host);
                    SetXmlLayoutCreateContainer(container.Panel1, area.Content1, createEmptyControls);
                    SetXmlLayoutCreateContainer(container.Panel2, area.Content2, createEmptyControls);
                    break;
                case AreaContentType.WfSplitContainer:
                    // Tento typ obecně nepoužíváme!
                    break;
                case AreaContentType.DxLayoutItemPanel:
                    if (createEmptyControls)
                        this._CreateEmptyPanel(host);
                    break;
            }
        }
        /// <summary>
        /// Vytvoří novou instanci panelu <see cref="DxLayoutItemPanel"/>, jako prázdnou (bez UserControlu), vloží ji do daného hosta a vrátí ji.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private DxLayoutItemPanel _CreateEmptyPanel(Control host)
        {
            DxLayoutItemPanel emptyPanel = new DxLayoutItemPanel(this);

            var buttons = this.EmptyPanelButtons;
            emptyPanel.TitleBarVisible = true;
            emptyPanel.CloseButtonVisibility = (buttons.HasFlag(EmptyPanelVisibleButtons.Close) ? this.CloseButtonVisibility : ControlVisibility.None);
            emptyPanel.DockButtonVisibility = (buttons.HasFlag(EmptyPanelVisibleButtons.Dock) ? this.DockButtonVisibility : ControlVisibility.None);
            emptyPanel.CloseButtonClick += Panel_CloseButtonClick;

            if (host != null) host.Controls.Add(emptyPanel);

            return emptyPanel;
        }
        /// <summary>
        /// Uživatel chce zavřít prázdný panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Panel_CloseButtonClick(object sender, EventArgs e)
        {
            if (sender is DxLayoutItemPanel removeHost)
                _RemoveEmptyPanelFromParentWithLayout(removeHost);
        }
        /// <summary>
        /// Obsahuje true, pokud se do nových prázdných panelů mají vytvářet prázdné panely s titulkem a tlačítky
        /// </summary>
        private bool _CreateEmptyControlInEmptyPanel
        {
            get
            {
                bool needButtons = ((this.EmptyPanelButtons & EmptyPanelVisibleButtons.All) != 0);
                return needButtons;
            }
        }
        /// <summary>
        /// Do stávajícího layoutu vloží prvky zachované z dřívějšího layoutu, pokud to půjde.
        /// Pokud to nepůjde, pak ty neumístěné controly vloží do pole, které je výstupem této metody.
        /// Volající je pak má korektně zahodit / oznámit aplikaci, že dané controly se zahazují (event <see cref="UserControlRemoved"/>).
        /// </summary>
        /// <param name="oldControls">Stávající controly</param>
        /// <param name="newHosts">Nové prostory</param>
        /// <param name="areaIdMapping">Přechodová mapa (Key = staré AreaId, Value = nové AreaId)</param>
        /// <param name="lostControlMode">Režim práce s UserControly, které nejsou v mapě <paramref name="areaIdMapping"/></param>
        private DxLayoutItemInfo[] SetXmlLayoutFill(DxLayoutItemInfo[] oldControls, Dictionary<string, HostAreaInfo> newHosts, IEnumerable<KeyValuePair<string, string>> areaIdMapping, OrphanedControlMode lostControlMode)
        {
            if (oldControls == null || oldControls.Length == 0) return new DxLayoutItemInfo[0];   // Zkratka

            // Nejprve umístíme stávající controly podle explicitní mapy:
            Dictionary<string, string> mapDict = CreateDictionary(areaIdMapping);
            List<DxLayoutItemInfo> remainingControls = new List<DxLayoutItemInfo>();
            if (mapDict.Count > 0)
            {
                foreach (DxLayoutItemInfo oldControl in oldControls)
                {
                    string oldAreaId = oldControl.AreaId;
                    string newAreaId = (mapDict.ContainsKey(oldAreaId) ? mapDict[oldAreaId] : null);
                    if (newAreaId != null && newHosts.TryGetValue(newAreaId, out var newHostInfo) && newHostInfo.IsDisponible)
                    {   // Pokud máme explicitně určenou cílovou oblast pomocí mapy, a oblast existuje a je prázdná,
                        // pak do ní dáme control bez ohledu na lostControlMode:
                        if (newHostInfo.ChildType == AreaContentType.EmptyLayoutPanel)
                            newHostInfo.ClearChilds();
                        AddControlToParent(oldControl.UserControl, newHostInfo.Parent, oldControl.IsPrimaryPanel, oldControl.TitleText);
                    }
                    else
                    {   // Tento prvek nemá svůj explicitní cíl (podle mapy): přidáme jej do seznamu do dalšího procesu:
                        remainingControls.Add(oldControl);
                    }
                }
            }
            else
            {   // Bez mapy: zbývající controly = všechny:
                remainingControls.AddRange(oldControls);
            }

            // Nyní umístíme zbývající controly podle zadaného režimu (lostControlMode):
            List<DxLayoutItemInfo> lostControls = new List<DxLayoutItemInfo>();
            if (remainingControls.Count > 0)
            {
                switch (lostControlMode)
                {
                    case OrphanedControlMode.MoveToUnusedArea:
                    case OrphanedControlMode.MoveToNewArea:
                        bool canUseExistingHosts = (lostControlMode == OrphanedControlMode.MoveToUnusedArea);
                        foreach (DxLayoutItemInfo oldControl in remainingControls)
                        {   // Tady už neřešíme mapu (původní => nové AreaId), protože co podle ní šlo vyřešit, už je vyřešeno.
                            string oldAreaId = oldControl.AreaId;
                            bool isAssigned = false;
                            // 1. Pokud můžeme využít existující oblasti:
                            if (canUseExistingHosts)
                            {
                                if (newHosts.TryGetValue(oldAreaId, out var newHostInfo) && newHostInfo.IsDisponible)
                                {   // Pokud najdeme původní oblast dle AreaId, a oblast existuje a je prázdná,
                                    // pak do ní tento control umístíme:
                                    if (newHostInfo.ChildType == AreaContentType.EmptyLayoutPanel)
                                        newHostInfo.ClearChilds();
                                    AddControlToParent(oldControl.UserControl, newHostInfo.Parent, oldControl.IsPrimaryPanel, oldControl.TitleText);
                                    isAssigned = true;
                                }
                                if (!isAssigned)
                                {   // Zkusíme najít libovolný existující prázdný prvek:
                                    var anyHostInfo = newHosts.Values.FirstOrDefault(h => h.IsDisponible);
                                    if (anyHostInfo != null)
                                    {
                                        if (anyHostInfo.ChildType == AreaContentType.EmptyLayoutPanel)
                                            anyHostInfo.ClearChilds();
                                        AddControlToParent(oldControl.UserControl, anyHostInfo.Parent, oldControl.IsPrimaryPanel, oldControl.TitleText);
                                        isAssigned = true;
                                    }
                                }
                            }

                            // 2. Pokud jsme nemohli využít existující oblasti, anebo už žádná volná není, pak přidáme novou oblast:
                            if (!isAssigned)
                            {   // Přidáme defaultně = nakonec, doprava
                                AddControl(oldControl.UserControl);
                                isAssigned = true;
                            }
                        }
                        break;
                    default:
                        lostControls = remainingControls;
                        break;
                }
            }

            return lostControls.ToArray();
        }
        /// <summary>
        /// Vrátí Dictionary z optional kolekce Key, Value
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private Dictionary<string, string> CreateDictionary(IEnumerable<KeyValuePair<string, string>> items)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.Key != null && !dictionary.ContainsKey(item.Key))
                        dictionary.Add(item.Key, item.Value);
                }
            }
            return dictionary;
        }
        /// <summary>
        /// Oznámí aplikaci prostřednictvím eventu <see cref="UserControlRemoved"/> ty stávající controly, které se do nového layoutu nedostaly, protože pro ně nebylo místo
        /// </summary>
        /// <param name="lostControls"></param>
        private void ReportLostControls(DxLayoutItemInfo[] lostControls)
        {
            if (_AllEventsDisable) return;
            foreach (DxLayoutItemInfo lostControl in lostControls)
                OnUserControlRemoved(lostControl.UserControl);
        }
        /// <summary>
        /// Uvolní objekty v dané kolekci
        /// </summary>
        /// <param name="items"></param>
        private void ReleaseContent(IEnumerable<DxLayoutItemInfo> items)
        {
            foreach (var item in items)
                item.ReleaseContent();
        }
        /// <summary>
        /// Uvolní objekty v dané kolekci
        /// </summary>
        /// <param name="items"></param>
        private void ReleaseContent(IEnumerable<HostAreaInfo> items)
        {
            foreach (var item in items)
                item.ReleaseContent();
        }
        /// <summary>
        /// Jméno property <see cref="XmlLayout"/> pro hlášky
        /// </summary>
        private string _XmlLayoutName { get { return nameof(DxLayoutPanel) + "." + nameof(XmlLayout); } }
        #endregion
        #region třídy layoutu: Area, enum AreaContentType
        /// <summary>
        /// Rozložení pracovní plochy, jedna plocha a její využití, rekurzivní třída.
        /// Obsah této třídy se persistuje do XML.
        /// POZOR tedy: neměňme jména [PropertyName("xxx")], jejich hodnoty jsou uloženy v XML tvaru na serveru 
        /// a podle atributu PropertyName budou načítána do aktuálních properties.
        /// Lze měnit jména properties.
        /// <para/>
        /// Obecně k XML persistoru: není nutno používat atribut [PropertyName("xxx")], ale pak musíme zajistit neměnnost názvů properties ve třídě.
        /// </summary>
        private class Area
        {
            #region Data
            /// <summary>
            /// ID prostoru
            /// </summary>
            [PropertyName("AreaID")]
            public string AreaId { get; set; }
            /// <summary>
            /// Typ obsahu = co v prostoru je
            /// </summary>
            [PropertyName("Content")]
            public AreaContentType ContentType { get; set; }
            /// <summary>
            /// Uživatelský identifikátor
            /// </summary>
            [PropertyName("ControlID")]
            public string ControlId { get; set; }
            /// <summary>
            /// Text controlu, typicky jeho titulek
            /// </summary>
            [PersistingEnabled(false)]
            public string ContentText { get; set; }
            /// <summary>
            /// Orientace splitteru
            /// </summary>
            [PropertyName("SplitterOrientation")]
            public Orientation? SplitterOrientation { get; set; }
            /// <summary>
            /// Fixovaný splitter?
            /// </summary>
            [PropertyName("IsSplitterFixed")]
            public bool? IsSplitterFixed { get; set; }
            /// <summary>
            /// Fixovaný panel
            /// </summary>
            [PropertyName("FixedPanel")]
            public FixedPanel? FixedPanel { get; set; }
            /// <summary>
            /// Minimální velikost pro Panel1
            /// </summary>
            [PropertyName("MinSize1")]
            public int? MinSize1 { get; set; }
            /// <summary>
            /// Minimální velikost pro Panel2
            /// </summary>
            [PropertyName("MinSize2")]
            public int? MinSize2 { get; set; }
            /// <summary>
            /// Pozice splitteru absolutní, zleva nebo shora
            /// </summary>
            [PropertyName("SplitterPosition")]
            public int? SplitterPosition { get; set; }
            /// <summary>
            /// Rozsah pohybu splitteru (šířka nebo výška prostoru).
            /// Podle této hodnoty a podle <see cref="FixedPanel"/> je následně restorována pozice při vkládání layoutu do nového objektu.
            /// <para/>
            /// Pokud původní prostor měl šířku 1000 px, pak zde je 1000. Pokud fixovaný panel byl Panel2, je to uvedeno v <see cref="FixedPanel"/>.
            /// Pozice splitteru zleva byla např. 420 (v <see cref="SplitterPosition"/>). Šířka fixního panelu tedy je (1000 - 420) = 580.
            /// Nyní budeme restorovat XmlLayout do nového prostoru, jehož šířka není 1000, ale 800px.
            /// Protože fixovaný panel je Panel2 (vpravo), pak nová pozice splitteru (zleva) je taková, aby Panel2 měl šířku stejnou jako původně (580): 
            /// nově tedy (800 - 580) = 220.
            /// <para/>
            /// Obdobné přepočty budou provedeny pro jinou situaci, kdy FixedPanel je None = splitter ke "gumový" = proporcionální.
            /// Pak se při restoru přepočte nová pozice splitteru pomocí poměru původní pozice ku Range.
            /// </summary>
            [PropertyName("SplitterRange")]
            public int? SplitterRange { get; set; }
            /// <summary>
            /// Obsah panelu 1 (rekurzivní instance téže třídy)
            /// </summary>
            [PropertyName("Content1")]
            public Area Content1 { get; set; }
            /// <summary>
            /// Obsah panelu 2 (rekurzivní instance téže třídy)
            /// </summary>
            [PropertyName("Content2")]
            public Area Content2 { get; set; }
            #endregion
            #region IsEqual
            public static bool IsEqual(Area area1, Area area2)
            {
                if (!_IsEqualNull(area1, area2)) return false;     // Jeden je null a druhý není
                if (area1 == null) return true;                    // area1 je null (a druhý taky) = jsou si rovny

                if (area1.ContentType != area2.ContentType) return false;    // Jiný druh obsahu
                // Obě area mají shodný typ obsahu:
                bool contentIsSplitted = (area1.ContentType == AreaContentType.DxSplitContainer || area1.ContentType == AreaContentType.WfSplitContainer);
                if (!contentIsSplitted) return true;               // Obsah NENÍ split container = z hlediska porovnání layoutu na koncovém obsahu nezáleží, jsou si rovny.

                // Porovnáme deklaraci vzhledu SplitterContaineru:
                if (!_IsEqualNullable(area1.SplitterOrientation, area1.SplitterOrientation)) return false;
                if (!_IsEqualNullable(area1.IsSplitterFixed, area1.IsSplitterFixed)) return false;
                if (!_IsEqualNullable(area1.FixedPanel, area1.FixedPanel)) return false;
                if (!_IsEqualNullable(area1.MinSize1, area1.MinSize1)) return false;
                if (!_IsEqualNullable(area1.MinSize2, area1.MinSize2)) return false;

                // Porovnáme deklarovanou pozici splitteru:
                if (area1._SplitterPositionComparable != area2._SplitterPositionComparable) return false;

                // Porovnáme rekurzivně definice :
                if (!IsEqual(area1.Content1, area2.Content1)) return false;
                if (!IsEqual(area1.Content2, area2.Content2)) return false;

                return true;
            }
            private static bool _IsEqualNull(object a, object b)
            {
                bool an = a is null;
                bool bn = b is null;
                return (an == bn);
            }
            private static bool _IsEqualNullable<T>(T? a, T? b) where T : struct, IComparable
            {
                bool av = a.HasValue;
                bool bv = b.HasValue;
                if (av && bv) return (a.Value.CompareTo(b.Value) == 0);         // Obě mají hodnotu: výsledek = jsou si hodnoty rovny?
                if (av || bv) return false;         // Některý má hodnotu? false, protože jen jeden má hodnotu (kdyby měly hodnotu oba, skončili bychom dřív)
                return true;                        // Obě jsou null
            }
            private int _SplitterPositionComparable
            {
                get
                {
                    var fixedPanel = this.FixedPanel ?? System.Windows.Forms.FixedPanel.Panel1;
                    switch (fixedPanel)
                    {
                        case System.Windows.Forms.FixedPanel.Panel1:
                            if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value;
                            return 0;
                        case System.Windows.Forms.FixedPanel.Panel2:
                            if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue) return this.SplitterPosition.Value - this.SplitterRange.Value;
                            return 0;
                        case System.Windows.Forms.FixedPanel.None:
                            if (this.SplitterPosition.HasValue && this.SplitterRange.HasValue && this.SplitterRange.Value > 0) return this.SplitterPosition.Value * 10000 / this.SplitterRange.Value;
                            return 0;
                    }
                    return 0;
                }
            }
            #endregion
        }
        /// <summary>
        /// Typ obsahu prostoru
        /// </summary>
        private enum AreaContentType 
        {
            /// <summary>
            /// Neurčeno
            /// </summary>
            None, 
            /// <summary>
            /// Žádný control
            /// </summary>
            Empty,
            /// <summary>
            /// Prázdný DxLayoutItemPanel
            /// </summary>
            EmptyLayoutPanel,
            /// <summary>
            /// Standardní naplněný DxLayoutItemPanel
            /// </summary>
            DxLayoutItemPanel, 
            /// <summary>
            /// SplitContainer typu DevExpress
            /// </summary>
            DxSplitContainer,
            /// <summary>
            /// SplitContainer typu WinForm
            /// </summary>
            WfSplitContainer
        }
        #endregion
        #endregion
        #region Třídy LayoutTileInfo (evidence UserControlů) a AddControlParams (parametry pro přidání UserControlu) a UserControlPair (data o jednom Splitteru)
        /// <summary>
        /// Třída obsahující WeakReference na UserControl (=uživatelův panel) a HostControl (zdejší panel obsahující titulek) a Parent (control, v němž bude / je / byl umístěn HostControl
        /// </summary>
        protected class LayoutTileInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="hostControl"></param>
            /// <param name="userControl"></param>
            public LayoutTileInfo(Control parent, DxLayoutItemPanel hostControl, Control userControl)
            {
                __Parent = parent;
                __HostControl = hostControl;
                __UserControl = userControl;

                if (userControl is ILayoutUserControl iLayoutUserControl)
                    iLayoutUserControl.TitleChanged += UserControlTitleChanged;

                RefreshTitleFromUserControl();
            }
            private WeakTarget<Control> __Parent;
            private WeakTarget<DxLayoutItemPanel> __HostControl;
            private WeakTarget<Control> __UserControl;
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
            /// Metoda vrátí true, pokud this objekt obsahuje daný control v kterékoli pozici (<see cref="UserControl"/>, <see cref="HostControl"/>, <see cref="Parent"/>).
            /// </summary>
            /// <param name="testControl"></param>
            /// <returns></returns>
            public bool ContainsAnyControl(Control testControl)
            {
                if (testControl == null) return false;
                Control anyControl;

                anyControl = this.UserControl;
                if (anyControl != null && Object.ReferenceEquals(anyControl, testControl)) return true;

                anyControl = this.HostControl;
                if (anyControl != null && Object.ReferenceEquals(anyControl, testControl)) return true;

                anyControl = this.Parent;
                if (anyControl != null && Object.ReferenceEquals(anyControl, testControl)) return true;

                return false;
            }
            /// <summary>
            /// Parent prvek
            /// </summary>
            public Control Parent { get { return __Parent; } set { __Parent = value; } }
            /// <summary>
            /// Hostitel pro control
            /// </summary>
            public DxLayoutItemPanel HostControl { get { return __HostControl; } }
            /// <summary>
            /// Vlastní control
            /// </summary>
            public Control UserControl { get { return __UserControl; } }
            /// <summary>
            /// User control přetypovaný na interface <see cref="ILayoutUserControl"/>.
            /// Může být null.
            /// </summary>
            public ILayoutUserControl ILayoutUserControl { get { return (UserControl as ILayoutUserControl); } }
            /// <summary>
            /// Aktuální reálná dokovaná pozice, odvozená od hostitelského containeru
            /// </summary>
            public LayoutPosition CurrentDockPosition { get { return HostControl?.CurrentDockPosition ?? LayoutPosition.None; } }
            /// <summary>
            /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
            /// Pokud sem bude vložena hodnota <see cref="LayoutPosition.None"/>, pak dokovací buttony nebudou viditelné (bez ohledu na <see cref="DxLayoutPanel.DockButtonVisibility"/>.
            /// </summary>
            public LayoutPosition DockButtonDisabledPosition 
            { 
                get { var hostControl = HostControl; return hostControl?.DockButtonDisabledPosition ?? LayoutPosition.None; } 
                set { var hostControl = HostControl; if (hostControl != null) hostControl.DockButtonDisabledPosition = value; }
            }
            /// <summary>
            /// Po změně textu v UserControlu
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void UserControlTitleChanged(object sender, EventArgs e)
            {
                RefreshTitleFromUserControl();
            }
            /// <summary>
            /// Aktualizuje titulek buď z dodaného textu nebo z 
            /// </summary>
            /// <param name="titleText"></param>
            internal void UpdateTitle(string titleText)
            {
                RefreshTitleFromUserControl(titleText);
            }
            /// <summary>
            /// Provede načtení dat o titulku z <see cref="UserControl"/>, pokud tento je typu <see cref="ILayoutUserControl"/>.
            /// </summary>
            /// <param name="titleText"></param>
            public void RefreshTitleFromUserControl(string titleText = null)
            {
                var hostControl = this.HostControl;
                if (hostControl == null) return;

                var iLayoutUserControl = this.ILayoutUserControl;
                if (iLayoutUserControl != null)
                    hostControl.ReloadTitleFrom(iLayoutUserControl);
                else
                    hostControl.TitleText = titleText;
            }
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
            /// Pozice, na kterou bude umístěn nový control
            /// </summary>
            public LayoutPosition Position { get; set; }
            /// <summary>
            /// Text titulku
            /// </summary>
            public string TitleText { get; set; }
            /// <summary>
            /// Je fixovaný Splitter? 
            /// Nezadáno = false
            /// </summary>
            public bool IsSplitterFixed { get; set; }
            public int? FixedSize { get; set; }
            public int MinSize { get; set; }
            /// <summary>
            /// Který panel je fixovaný (jeho velikost se nemění při změně velikosti celého SplitContaineru)?
            /// Nezadáno = Panel1
            /// </summary>
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
            /// <summary>
            /// Pozice párového panelu = párová strana k <see cref="Position"/>
            /// </summary>
            public LayoutPosition PairPosition
            {
                get
                {
                    switch (this.Position)
                    {
                        case LayoutPosition.Left: return LayoutPosition.Right;
                        case LayoutPosition.Top: return LayoutPosition.Bottom;
                        case LayoutPosition.Right: return LayoutPosition.Left;
                        case LayoutPosition.Bottom: return LayoutPosition.Top;
                    }
                    return LayoutPosition.None;
                }
            }
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
            /// Plná data k Controlu 1. Doplňuje se ručně.
            /// </summary>
            public LayoutTileInfo TileInfo1 { get; set; }
            /// <summary>
            /// Plná data k Controlu 2. Doplňuje se ručně.
            /// </summary>
            public LayoutTileInfo TileInfo2 { get; set; }
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
                DxLayoutPanelSplitterChangedArgs args = new DxLayoutPanelSplitterChangedArgs(TileInfo1?.UserControl, TileInfo2?.UserControl, SplitterOrientation, SplitContainer.SplitterPosition);
                return args;
            }
            /// <summary>
            /// Provede změnu orientace splitteru, prohození controlů. Neřeší eventy. Vrací true = došlo ke změně.
            /// </summary>
            /// <param name="horizontal">Nastavit orientaci Horizontal</param>
            /// <param name="swap">Prohodit navzájem obsah Panel1 - Panel2</param>
            public bool SetOrientation(bool horizontal, bool swap)
            {
                bool isChangeOrientation = false;
                bool isChanged = false;

                var control1 = this.Control1;
                var parent1 = control1?.Parent;
                var tileInfo1 = this.TileInfo1;

                var control2 = this.Control2;
                var parent2 = control2?.Parent;
                var tileInfo2 = this.TileInfo2;

                if (this.SplitContainer.Horizontal != horizontal)
                {
                    this.SplitContainer.Horizontal = horizontal;
                    isChangeOrientation = true;
                    isChanged = true;
                }

                if (swap)
                {
                    if (parent1 != null && control1 != null)
                    {
                        RemoveControlFromParent(control1, parent1);
                        if (tileInfo1 != null)
                            tileInfo1.Parent = null;
                    }
                    if (parent2 != null && control2 != null)
                    {
                        RemoveControlFromParent(control2, parent2);
                        if (tileInfo2 != null)
                            tileInfo2.Parent = null;
                    }

                    if (parent1 != null && control2 != null)
                        parent1.Controls.Add(control2);
                    if (tileInfo2 != null)
                        tileInfo2.Parent = parent1;

                    if (parent2 != null && control1 != null)
                        parent2.Controls.Add(control1);
                    if (tileInfo1 != null)
                        tileInfo1.Parent = parent2;

                    isChanged = true;
                }

                if (isChanged)
                {
                    if (tileInfo1 != null) tileInfo1.DockButtonDisabledPosition = tileInfo1.CurrentDockPosition;
                    if (tileInfo2 != null) tileInfo2.DockButtonDisabledPosition = tileInfo2.CurrentDockPosition;
                }

                // Pokud jsem provedl prohození obsahu panelů zleva doprava nebo shora dolů (tj. beze změny orientace splitteru), tak bych rád, aby stávající obsah měl dosavadní rozměr = 
                //  upravím pozici splitteru tak, aby byla symetrická k pozici dosavadní:
                if (swap && !isChangeOrientation )
                {
                    var clientSize = this.SplitContainer.ClientSize;
                    int splitSize = (this.SplitContainer.Horizontal ? clientSize.Width : clientSize.Height);
                    int splitPosition = splitSize - this.SplitContainer.SplitterPosition;
                    this.SplitContainer.SplitterPosition = splitPosition;
                }

                return isChanged;
            }
        }
        #endregion
    }
    #region class DxLayoutItemPanel : panel hostující v sobě TitlePanel a UserControl
    /// <summary>
    /// Panel, který slouží jako hostitel pro jeden uživatelský control v rámci <see cref="DxLayoutPanel"/>.
    /// Obsahuje panel pro titulek, zavírací křížek a OnMouse buttony pro předokování aktuálního prvku v rámci parent SplitContaineru.
    /// </summary>
    public class DxLayoutItemPanel : DxPanelControl
    {
        #region Konstruktor, public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutItemPanel()
        {
            this.Initialize(null);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public DxLayoutItemPanel(DxLayoutPanel owner)
        {
            this.Initialize(owner);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"UserControl: {this.UserControl?.GetType()}, Text: {this.UserControl?.Text}";
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="owner"></param>
        protected void Initialize(DxLayoutPanel owner)
        {
            this.__Owner = owner;
            this.Dock = DockStyle.Fill;

            this._DockButtonDisabledPosition = LayoutPosition.None;
        }
        /// <summary>
        /// Vlastník = kompletní layout
        /// </summary>
        public DxLayoutPanel Owner { get { return __Owner; } }
        private WeakTarget<DxLayoutPanel> __Owner;
        /// <summary>
        /// Aktuální reálná dokovaná pozice, odvozená od hostitelského containeru
        /// </summary>
        public LayoutPosition CurrentDockPosition
        {
            get
            {
                bool isHorizontal = false;
                int panelId = 0;

                DevExpress.XtraEditors.SplitGroupPanel dxPanel = this.SearchForParentOfType<DevExpress.XtraEditors.SplitGroupPanel>();
                if (dxPanel != null && dxPanel.Parent is DevExpress.XtraEditors.SplitContainerControl dxSplitContainer)
                {   // DevExpress SplitPanel:
                    isHorizontal = dxSplitContainer.Horizontal;
                    panelId = (Object.ReferenceEquals(dxPanel, dxSplitContainer.Panel1) ? 1 :
                              (Object.ReferenceEquals(dxPanel, dxSplitContainer.Panel2) ? 2 : 0));
                }
                else
                {   // WinForm SplitPanel?
                    SplitterPanel wfPanel = this.SearchForParentOfType<SplitterPanel>();
                    if (wfPanel != null && wfPanel.Parent is SplitContainer wfSplitContainer)
                    {
                        isHorizontal = (wfSplitContainer.Orientation == Orientation.Horizontal);
                        panelId = (Object.ReferenceEquals(wfPanel, wfSplitContainer.Panel1) ? 1 :
                                  (Object.ReferenceEquals(wfPanel, wfSplitContainer.Panel2) ? 2 : 0));
                    }
                }

                switch (panelId)
                {
                    case 1: return (isHorizontal ? LayoutPosition.Left : LayoutPosition.Top);            // Panel1 je (horizontálně) vlevo / (vertikálně) nahoře
                    case 2: return (isHorizontal ? LayoutPosition.Right : LayoutPosition.Bottom);        // Panel1 je (horizontálně) vpravo / (vertikálně) dole
                }

                return LayoutPosition.None;
            }
        }
        /// <summary>
        /// Odebere ze sebe UserControl - tak, aby nebyl součástí navazujícího Dispose()
        /// </summary>
        public void ReleaseUserControl()
        {
            this.UserControl = null;                     // Korektně odebere stávající UserControl z this.Controls i ze zdejší proměnné, ale nedisposuje jej
        }
        /// <summary>
        /// Uvolnění zdrojů
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this._Dispose();
        }
        /// <summary>
        /// Vnitřní Dispose
        /// </summary>
        private void _Dispose()
        {
            _TitleBarVisible = false;
            _IsPrimaryPanel = false;

            this.Controls.Clear();
            if (_TitleBar != null)
            {
                _TitleBar.Dispose();
                _TitleBar = null;
            }

            // Tady nesmí být Dispose, protože tohle je aplikační control...
            _UserControl = null;

            __Owner = null;
        }
        #endregion
        #region Naše vlastní data pro Titulkový panel, eventy
        /// <summary>
        /// Naplní data o titulku daty z dodaného objektu typu <see cref="ILayoutUserControl"/>
        /// </summary>
        /// <param name="iLayoutUserControl"></param>
        public void ReloadTitleFrom(ILayoutUserControl iLayoutUserControl)
        {
            if (iLayoutUserControl != null)
                this.RunInGui(() => _ReloadTitleFrom(iLayoutUserControl));
        }
        /// <summary>
        /// GUI thread: Naplní data o titulku daty z dodaného objektu typu <see cref="ILayoutUserControl"/>
        /// </summary>
        /// <param name="iLayoutUserControl"></param>
        private void _ReloadTitleFrom(ILayoutUserControl iLayoutUserControl)
        {
            _TitleBarVisible = iLayoutUserControl.TitleVisible;
            _TitleText = iLayoutUserControl.TitleText;
            _TitleIcon = iLayoutUserControl.TitleIcon;
            _LineWidth = iLayoutUserControl.LineWidth;
            _LineColor = iLayoutUserControl.LineColor;
            _LineColorEnd = iLayoutUserControl.LineColorEnd;
            _TitleBackMargins = iLayoutUserControl.TitleBackMargins;
            _TitleBackColor = iLayoutUserControl.TitleBackColor;
            _TitleBackColorEnd = iLayoutUserControl.TitleBackColorEnd;
            _TitleBarSetVisible(iLayoutUserControl.TitleVisible);
            RefreshControl();
        }
        /// <summary>
        /// Titulkový panel je viditelný?
        /// </summary>
        public bool TitleBarVisible { get { return _TitleBarVisible; } set { this.RunInGui(() => _TitleBarSetVisible(value)); } }
        private bool _TitleBarVisible;
        /// <summary>
        /// Obsahuje true pokud this panel je primární = první vytvořený.
        /// Takový panel může mít jiné chování (typicky nemá titulek, a nemá CloseButton), viz ...
        /// </summary>
        public bool IsPrimaryPanel { get { return _IsPrimaryPanel; } set { _IsPrimaryPanel = value; this.RunInGui(RefreshControl); } }
        private bool _IsPrimaryPanel;
        /// <summary>
        /// Text titulku
        /// </summary>
        public string TitleText { get { return _TitleText; } set { _TitleText = value; this.RunInGui(RefreshControl); } }
        private string _TitleText;
        /// <summary>
        /// Ikona titulku
        /// </summary>
        public Image TitleIcon { get { return _TitleIcon; } set { _TitleIcon = value; this.RunInGui(RefreshControl); } }
        private Image _TitleIcon;
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Záporná hodnota: vyjadřuje plnou barvu, udává odstup od horního a dolního okraje titulku.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        public int? LineWidth { get { return _LineWidth; } set { _LineWidth = value; this.RunInGui(RefreshControl); } }
        private int? _LineWidth;
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColor { get { return _LineColor; } set { _LineColor = value; this.RunInGui(RefreshControl); } }
        private Color? _LineColor;
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? LineColorEnd { get { return _LineColorEnd; } set { _LineColorEnd = value; this.RunInGui(RefreshControl); } }
        private Color? _LineColorEnd;
        /// <summary>
        /// Okraje mezi TitlePanel a barvou pozadí, default = 0
        /// </summary>
        public int? TitleBackMargins { get { return _TitleBackMargins; } set { _TitleBackMargins = value; this.RunInGui(RefreshControl); } }
        private int? _TitleBackMargins;
        /// <summary>
        /// Barva pozadí titulku.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? TitleBackColor { get { return _TitleBackColor; } set { _TitleBackColor = value; this.RunInGui(RefreshControl); } }
        private Color? _TitleBackColor;
        /// <summary>
        /// Barva pozadí titulku, konec gradientu vpravo, null = SolidColor.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public Color? TitleBackColorEnd { get { return _TitleBackColorEnd; } set { _TitleBackColorEnd = value; this.RunInGui(RefreshControl); } }
        private Color? _TitleBackColorEnd;
        /// <summary>
        /// Viditelnost buttonu Close.
        /// Lze setovat explicitní hodnotu, anebo hodnotu <see cref="ControlVisibility.ByParent"/> = bude se přebírat z <see cref="Owner"/> (=výchozí stav).
        /// </summary>
        public ControlVisibility CloseButtonVisibility
        {
            get { return (_CloseButtonVisibility ?? Owner?.CloseButtonVisibility ?? ControlVisibility.Default); }
            set { _CloseButtonVisibility = (value == ControlVisibility.ByParent ? (ControlVisibility?)null : (ControlVisibility?)value); }
        }
        private ControlVisibility? _CloseButtonVisibility;
        /// <summary>
        /// Viditelnost buttonů Dock.
        /// Lze setovat explicitní hodnotu, anebo hodnotu <see cref="ControlVisibility.ByParent"/> = bude se přebírat z <see cref="Owner"/> (=výchozí stav).
        /// </summary>
        public ControlVisibility DockButtonVisibility
        {
            get { return (_DockButtonVisibility ?? Owner?.DockButtonVisibility ?? ControlVisibility.Default); }
            set { _DockButtonVisibility = (value == ControlVisibility.ByParent ? (ControlVisibility?)null : (ControlVisibility?)value); }
        }
        private ControlVisibility? _DockButtonVisibility;
        /// <summary>
        /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
        /// Pokud sem bude vložena hodnota <see cref="LayoutPosition.None"/>, pak dokovací buttony nebudou viditelné bez ohledu na <see cref="DxLayoutPanel.DockButtonVisibility"/>.
        /// </summary>
        public LayoutPosition DockButtonDisabledPosition { get { return _DockButtonDisabledPosition; } set { _DockButtonDisabledPosition = value; this.RunInGui(RefreshControl); } }
        private LayoutPosition _DockButtonDisabledPosition;
        /// <summary>
        /// Refresh obsahu
        /// </summary>
        internal void RefreshControl()
        {
            if (_TitleBar != null && _TitleBarVisible)
                _TitleBar.RefreshControl();
        }
        /// <summary>
        /// Uživatel kliknul na button Dock (strana je v argumentu)
        /// </summary>
        public event EventHandler<DxLayoutTitleDockPositionArgs> DockButtonClick;
        /// <summary>
        /// Uživatel kliknul na button Close
        /// </summary>
        public event EventHandler CloseButtonClick;
        /// <summary>
        /// Obsahuje true, pokud <see cref="DockButtonDisabledPosition"/> obsahuje nějakou konkrétní hodnotu, nikoli None
        /// </summary>
        protected bool IsAnyDockPosition 
        {
            get 
            {
                LayoutPosition dockPosition = _DockButtonDisabledPosition;
                return (dockPosition == LayoutPosition.Left || dockPosition == LayoutPosition.Top || dockPosition == LayoutPosition.Bottom || dockPosition == LayoutPosition.Right);
            }
        }
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
                if (_TitleBar != null && _TitleBar.VisibleInternal)
                    _TitleBar.VisibleInternal = false;
            }
        }
        /// <summary>
        /// Inicializace TitleBaru
        /// </summary>
        private void _TitleBarInit()
        {
            _TitleBar = new DxLayoutTitlePanel(this);
            _TitleBar.DockButtonClick += _TitleBar_DockButtonClick;
            _TitleBar.CloseButtonClick += _TitleBar_CloseButtonClick;

            _FillPanelControls();
        }
        /// <summary>
        /// Po kliknutí na Dock button pře-vyvoláme náš event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TitleBar_DockButtonClick(object sender, DxLayoutTitleDockPositionArgs e)
        {
            this.DockButtonClick?.Invoke(this, e);
        }
        /// <summary>
        /// Po kliknutí na Close button pře-vyvoláme náš event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TitleBar_CloseButtonClick(object sender, EventArgs e)
        {
            this.CloseButtonClick?.Invoke(this, e);
        }
        /// <summary>
        /// Instance titulkového řádku
        /// </summary>
        private DxLayoutTitlePanel _TitleBar;
        #endregion
        #region UserControl a vkládání controlů (UserControl + _TitleBar) do this.Controls
        /// <summary>
        /// Obsahuje true pokud this panel je prázdný = neobsahuje <see cref="UserControl"/>
        /// </summary>
        public bool IsEmpty { get { return (this.UserControl == null); } }
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
                    DxLayoutPanel.RemoveControlFromParent(userControl, this);
                    _UserControl = null;
                }
                userControl = value;
                _UserControl = userControl;
                if (userControl != null)
                {
                    userControl.Dock = DockStyle.Fill;
                    this._FillPanelControls();
                }
            }
        }
        private Control _UserControl;
        /// <summary>
        /// Do this.Controls ve správném pořadí vloží existující prvky TitleBar a UserControl, před tím vyprázdní pole controlů.
        /// </summary>
        private void _FillPanelControls()
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
    #endregion
    #region class DxLayoutTitlePanel : titulkový řádek
    /// <summary>
    /// Titulkový řádek. Obsahuje titulek a několik buttonů (Dock a Close).
    /// </summary>
    public class DxLayoutTitlePanel : DxDockTitlePanel
    {
        #region Konstuktor, vnitřní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutTitlePanel()
            : base()
        { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public DxLayoutTitlePanel(DxLayoutItemPanel owner)
            : base()
        {
            __Owner = owner;
            RefreshButtons(true);
        }
        /// <summary>
        /// Vlastník titulku = <see cref="DxLayoutItemPanel"/> jednoho UserControlu.
        /// </summary>
        public DxLayoutItemPanel Owner { get { return __Owner; } }
        private WeakTarget<DxLayoutItemPanel> __Owner;
        /// <summary>
        /// Celý layout panel = všechny controly
        /// </summary>
        public DxLayoutPanel LayoutPanel {  get { return this.Owner?.Owner; } }
        #endregion
        #region Data získaná z Ownerů (override bázových hodnot, které jsou jednoduše setované)
        /// <summary>
        /// Mají se použít SVG ikony?
        /// </summary>
        public override bool UseSvgIcons { get { return (this.LayoutPanel?.UseSvgIcons ?? true); } set { } }
        /// <summary>
        /// Obsahuje true pokud this panel je primární = první vytvořený.
        /// Takový panel může mít jiné chování (typicky nemá titulek, a nemá CloseButton), viz ...
        /// </summary>
        public override bool IsPrimaryPanel { get { return Owner?.IsPrimaryPanel ?? false; } set { } }
        /// <summary>
        /// Ikona titulku
        /// </summary>
        public override Image TitleIcon { get { return Owner?.TitleIcon ?? null; } set { } }
        /// <summary>
        /// Text titulku
        /// </summary>
        public override string TitleText { get { return Owner?.TitleText ?? ""; } set { } }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Záporná hodnota: vyjadřuje plnou barvu, udává odstup od horního a dolního okraje titulku.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        public override int? LineWidth { get { return Owner?.LineWidth; } set { } }
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public override Color? LineColor { get { return Owner?.LineColor; } set { } }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public override Color? LineColorEnd { get { return Owner?.LineColorEnd; } set { } }
        /// <summary>
        /// Okraje mezi TitlePanel a barvou pozadí, default = 0
        /// </summary>
        public override int? TitleBackMargins { get { return Owner?.TitleBackMargins; } set { } }
        /// <summary>
        /// Barva pozadí titulku.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public override Color? TitleBackColor { get { return Owner?.TitleBackColor; } set { } }
        /// <summary>
        /// Barva pozadí titulku, konec gradientu vpravo, null = SolidColor.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public override Color? TitleBackColorEnd { get { return Owner?.TitleBackColorEnd; } set { } }
        /// <summary>
        /// Je povoleno přemístění titulku pomocí Drag And Drop
        /// </summary>
        public override bool DragDropEnabled { get { return (LayoutPanel?.DragDropEnabled ?? false); } set { } }
        /// <summary>
        /// Viditelnost buttonu Close.
        /// Lze setovat explicitní hodnotu, anebo hodnotu <see cref="ControlVisibility.ByParent"/> = bude se přebírat z <see cref="Owner"/> (=výchozí stav).
        /// </summary>
        public override ControlVisibility CloseButtonVisibility 
        {
            get { return (_CloseButtonVisibility ?? Owner?.CloseButtonVisibility ?? ControlVisibility.Default); } 
            set { _CloseButtonVisibility = (value == ControlVisibility.ByParent ? (ControlVisibility?)null : (ControlVisibility?)value); } 
        }
        private ControlVisibility? _CloseButtonVisibility;
        /// <summary>
        /// Tooltip na buttonu Close.
        /// Lze setovat explicitní hodnotu, anebo hodnotu NULL = bude se přebírat z <see cref="LayoutPanel"/> (=výchozí stav).
        /// </summary>
        public override string CloseButtonToolTip
        {
            get { return (_CloseButtonToolTip ?? LayoutPanel?.CloseButtonToolTip ?? ""); } 
            set { _CloseButtonToolTip = value; } 
        }
        private string _CloseButtonToolTip;
        /// <summary>
        /// Viditelnost buttonů Dock.
        /// Lze setovat explicitní hodnotu, anebo hodnotu <see cref="ControlVisibility.ByParent"/> = bude se přebírat z <see cref="Owner"/> (=výchozí stav).
        /// </summary>
        public override ControlVisibility DockButtonVisibility 
        { 
            get { return (_DockButtonVisibility ?? Owner?.DockButtonVisibility ?? ControlVisibility.Default); }
            set { _DockButtonVisibility = (value == ControlVisibility.ByParent ? (ControlVisibility?)null : (ControlVisibility?)value); }
        }
        private ControlVisibility? _DockButtonVisibility;
        /// <summary>
        /// Tooltip na buttonu DockLeft.
        /// Nemá význam setovat hodnotu, přebírá se z <see cref="LayoutPanel"/>
        /// </summary>
        public override string DockButtonLeftToolTip { get { return LayoutPanel?.DockButtonLeftToolTip; } set { } }
        /// <summary>
        /// Tooltip na buttonu DockTop.
        /// Nemá význam setovat hodnotu, přebírá se z <see cref="LayoutPanel"/>
        /// </summary>
        public override string DockButtonTopToolTip { get { return LayoutPanel?.DockButtonTopToolTip; } set { } }
        /// <summary>
        /// Tooltip na buttonu DockBottom.
        /// Nemá význam setovat hodnotu, přebírá se z <see cref="LayoutPanel"/>
        /// </summary>
        public override string DockButtonBottomToolTip { get { return LayoutPanel?.DockButtonBottomToolTip; } set { } }
        /// <summary>
        /// Tooltip na buttonu DockRight.
        /// Nemá význam setovat hodnotu, přebírá se z <see cref="LayoutPanel"/>
        /// </summary>
        public override string DockButtonRightToolTip { get { return LayoutPanel?.DockButtonRightToolTip; } set { } }
        /// <summary>
        /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
        /// </summary>
        public override LayoutPosition DockButtonDisabledPosition 
        {
            get { return (_DockButtonDisabledPosition ?? Owner?.DockButtonDisabledPosition ?? LayoutPosition.None); } 
            set { _DockButtonDisabledPosition = value; }
        }
        private LayoutPosition? _DockButtonDisabledPosition;
        /// <summary>
        /// Obsahuje-li true, budou zobrazována dokovací tlačítka podle situace v hlavním panelu, typicky reaguje na počet panelů.
        /// </summary>
        public override bool DockButtonsEnabled { get { return ((this.LayoutPanel?.ControlCount ?? 0) > 0); } set { } }
        #endregion
    }
    #endregion
    #region class DxDockTitlePanel : titulkový řádek samotný, s tlačítky Dock a Close, bez vztahu na layout
    /// <summary>
    /// titulkový řádek samotný, s tlačítky Dock a Close, bez vztahu na layout
    /// </summary>
    public class DxDockTitlePanel : DxTitlePanel
    {
        #region Konstuktor, vnitřní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDockTitlePanel()
            : base()
        { }
        /// <summary>
        /// Inicializace.
        /// POZOR: virtuální metoda volaná z konstruktoru předka!!!  Obecně nedoporučovaná technika.
        /// Tato metoda tedy proběhne dříve, než proběhne zdejší konstruktor!!!  (=nepoužívat referenci na __Owner)
        /// </summary>
        protected override void Initialize()
        {
            _DockLeftButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, visible: false, tag: LayoutPosition.Left);
            _DockTopButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, visible: false, tag: LayoutPosition.Top);
            _DockBottomButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, visible: false, tag: LayoutPosition.Bottom);
            _DockRightButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, visible: false, tag: LayoutPosition.Right);

            base.Initialize();

            MouseActivityInit();
        }
        /// <summary>
        /// Rozmístí svoje buttony, posouvá souřadnice podle umístěných buttonů.
        /// Výchozí pozice parametru x je vpravo. Buttony se umísťují zprava doleva.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected override void DoLayoutButtons(ref int x, ref int y)
        {
            base.DoLayoutButtons(ref x, ref y);

            // Buttony budeme sázet na jejich pozice v pořadí zprava:
            int bw = ButtonSize;
            int bs = ButtonSpace;
            int bd = bw + bs;

            x -= bw;
            _DockRightButton.Location = new Point(x, y);

            x -= bd;
            _DockBottomButton.Location = new Point(x, y);

            x -= bd;
            _DockTopButton.Location = new Point(x, y);

            x -= bd;
            _DockLeftButton.Location = new Point(x, y);

            x -= bs;
        }
        /// <summary>
        /// Po kliknutí na tlačítko Dock...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ClickDock(object sender, EventArgs args)
        {
            if (sender is Control control && control.Tag is LayoutPosition)
            {
                LayoutPosition dockPosition = (LayoutPosition)control.Tag;
                this.OnClickDock(new DxLayoutTitleDockPositionArgs(dockPosition));
            }
        }
        /// <summary>
        /// Po kliknutí na tlačítko Dock...
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnClickDock(DxLayoutTitleDockPositionArgs args)
        {
            DockButtonClick?.Invoke(this, args);
        }
        private DxSimpleButton _DockLeftButton;
        private DxSimpleButton _DockTopButton;
        private DxSimpleButton _DockBottomButton;
        private DxSimpleButton _DockRightButton;
        #endregion
        #region Public data
        /// <summary>
        /// Viditelnost buttonů Dock
        /// </summary>
        public virtual ControlVisibility DockButtonVisibility { get; set; }
        /// <summary>
        /// Tooltip na buttonu DockLeft
        /// </summary>
        public virtual string DockButtonLeftToolTip { get; set; }
        /// <summary>
        /// Tooltip na buttonu DockTop
        /// </summary>
        public virtual string DockButtonTopToolTip { get; set; }
        /// <summary>
        /// Tooltip na buttonu DockBottom
        /// </summary>
        public virtual string DockButtonBottomToolTip { get; set; }
        /// <summary>
        /// Tooltip na buttonu DockRight
        /// </summary>
        public virtual string DockButtonRightToolTip { get; set; }
        /// <summary>
        /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
        /// </summary>
        public virtual LayoutPosition DockButtonDisabledPosition { get; set; }
        /// <summary>
        /// Obsahuje-li true, budou zobrazována dokovací tlačítka podle situace v hlavním panelu, typicky reaguje na počet panelů.
        /// </summary>
        public virtual bool DockButtonsEnabled { get; set; }
        /// <summary>
        /// Uživatel kliknul na button Dock (strana je v argumentu)
        /// </summary>
        public event EventHandler<DxLayoutTitleDockPositionArgs> DockButtonClick;
        #endregion
        #region Ikony
        /// <summary>
        /// Aktualizuje vzhled ikon, pokud je to nutné
        /// </summary>
        protected override void RefreshButtons(bool force = false)
        {
            if (!NeedRefreshIcons(force)) return;

            base.RefreshButtons(force);

            int btnSize = ButtonSize;
            Size buttonSize = new Size(btnSize, btnSize);
            int imgSize = btnSize - 4;                          // 4 = okraje mezi buttonem a vnitřním Image
            Size imageSize = new Size(imgSize, imgSize);

            string[] icons = CurrentIcons;

            RefreshButton(_DockLeftButton, buttonSize, icons[1], imageSize, DockButtonLeftToolTip);
            RefreshButton(_DockTopButton, buttonSize, icons[2], imageSize, DockButtonTopToolTip);
            RefreshButton(_DockBottomButton, buttonSize, icons[3], imageSize, DockButtonBottomToolTip);
            RefreshButton(_DockRightButton, buttonSize, icons[4], imageSize, DockButtonRightToolTip);
        }
        /// <summary>
        /// Aktuální ikony
        /// </summary>
        protected override string[] CurrentIcons
        {
            get
            {
                var baseIcons = base.CurrentIcons;

                // Na pozici [0] je Close, pak jsou: Left, Top, Bottom, Right:
                bool useSvgIcons = UseSvgIcons;
                if (useSvgIcons)
                    return new string[]
                    {
                        baseIcons[0],
                        "svgimages/align/alignverticalleft.svg",
                        "svgimages/align/alignhorizontaltop.svg",
                        "svgimages/align/alignhorizontalbottom.svg",
                        "svgimages/align/alignverticalright.svg"
                    };
                else
                    return new string[]
                    {
                        baseIcons[0],
                        "images/alignment/alignverticalleft_16x16.png",
                        "images/alignment/alignhorizontaltop_16x16.png",
                        "images/alignment/alignhorizontalbottom_16x16.png",
                        "images/alignment/alignverticalright_16x16.png"
                    };
            }
        }
        #endregion
        #region Refreshe (obsah, viditelnost, interaktivní tlačítka podle stavu myši)
        /// <summary>
        /// Zajistí přenačtení hodnot z Ownera a jejich promítnutí do this titulku
        /// </summary>
        internal void RefreshControl()
        {
            this.RefreshTitle();
            this.RefreshButtonVisibility(false);
            this.DoLayout();
        }
        /// <summary>
        /// Nastaví Visible a Enabled pro buttony podle aktuálního stavu a podle požadavků
        /// </summary>
        /// <param name="doLayoutTitle">Po doběhnutí určení viditelnosti vyvolat <see cref="DxTitlePanel.DoLayoutTitleLabel()"/> ?</param>
        protected override void RefreshButtonVisibility(bool doLayoutTitle)
        {
            TitleLabelRight = this.ClientSize.Width - PanelMargin;

            // Tlačítka pro dokování budeme zobrazovat pouze tehdy, když hlavní panel zobrazuje více než jeden prvek. Pro méně prvků nemá dokování význam!
            bool hasMorePanels = this.DockButtonsEnabled;
            bool isDockVisible = hasMorePanels && GetItemVisibility(DockButtonVisibility, IsMouseOnControl, IsPrimaryPanel);
            if (isDockVisible)
            {
                LayoutPosition dockButtonDisable = DockButtonDisabledPosition;
                _DockLeftButton.Enabled = (dockButtonDisable != LayoutPosition.Left);
                _DockTopButton.Enabled = (dockButtonDisable != LayoutPosition.Top);
                _DockBottomButton.Enabled = (dockButtonDisable != LayoutPosition.Bottom);
                _DockRightButton.Enabled = (dockButtonDisable != LayoutPosition.Right);
            }
            _DockLeftButton.Visible = isDockVisible;
            _DockTopButton.Visible = isDockVisible;
            _DockBottomButton.Visible = isDockVisible;
            _DockRightButton.Visible = isDockVisible;

            base.RefreshButtonVisibility(false);

            // Šířka TitleLabelu:
            if (isDockVisible) TitleLabelRight = _DockLeftButton.Location.X - ButtonSpace;

            // Upravit šířku TitleLabelu:
            if (doLayoutTitle) DoLayoutTitleLabel();
        }
        #endregion
    }
    #endregion
    #region class DxTitlePanel : titulkový řádek samotný, s tlačítkem Close, bez vztahu na layout
    /// <summary>
    /// Titulkový řádek. Obsahuje titulek a button Close.
    /// </summary>
    public class DxTitlePanel : DxPanelControl, ISubscriberToZoomChange
    {
        #region Konstruktor, inicializace, proměnné TitleLabel a CloseButton
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTitlePanel()
        {
            Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected virtual void Initialize()
        {
            CloseButton = DxComponent.CreateDxMiniButton(200, 2, 24, 24, this, _ClickClose, visible: false);

            this.UseSvgIcons = true;
            // Pořadí má vliv: TitleLabel až nakonec => bude "pod" ikonami:
            // TitlePicture = new DevExpress.XtraEditors.PictureEdit() { ReadOnly = true, Bounds = new Rectangle(12, 6, 24, 24), Visible = false, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, BackColor = Color.Transparent };
            TitlePicture = new PictureBox() { Bounds = new Rectangle(12, 6, 24, 24), Visible = false, BorderStyle = System.Windows.Forms.BorderStyle.None, BackColor = Color.Transparent };
            this.Controls.Add(TitlePicture);

            TitleLabel = DxComponent.CreateDxLabel(12, 6, 200, this, "", LabelStyleType.MainTitle, hAlignment: HorzAlignment.Near, autoSizeMode: DevExpress.XtraEditors.LabelAutoSizeMode.Horizontal);
            TitleLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            TitleLabelRight = 200;

            DxComponent.SubscribeToZoomChange(this);
            this.DragAndDropInit();

            this.Height = 35;
            this.Dock = DockStyle.Top;
        }
        /// <summary>
        /// Po změně velikosti vyvolá <see cref="DoLayout()"/>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            DoLayout();
        }
        /// <summary>
        /// Zajistí rozmístění controlů.
        /// </summary>
        public virtual void DoLayout()
        {
            int height = this.Height;

            if (TitleLabel != null)
            {
                int fontHeight = TitleLabel.StyleController?.Appearance.Font.Height ?? TitleLabel.Appearance.Font.Height;
                if (TitleLabel.Height != fontHeight)
                    TitleLabel.Height = fontHeight;
                height = fontHeight + HeightAdd;
            }
            else
            {
                int fontHeight = DxComponent.ZoomToGuiInt(12);
                height = fontHeight + HeightAdd;
            }

            int buttonSize = ButtonSize;
            int minHeight = buttonSize + 4;
            if (height < minHeight) height = minHeight;
            if (this.Height != height)
            {
                this.Height = height;
                // Nastavení jiné výšky než je aktuální (=v minulém řádku) vyvolá rekurzivně this metodu, v té už nepůjdeme touto větví, ale nastavíme souřadnice buttonů (za else).
            }
            else
            {
                this.RefreshButtons();

                int y = (height - buttonSize) / 2;
                this.DoLayoutTitleIcon(ref y);

                int panelWidth = this.ClientSize.Width;
                int x = panelWidth - PanelMargin;
                TitleLabelRight = x;
                this.DoLayoutButtons(ref x, ref y);
                this.RefreshButtonVisibility(true);
            }
        }
        /// <summary>
        /// Umístí <see cref="TitlePicture"/>.
        /// </summary>
        /// <param name="y"></param>
        protected virtual void DoLayoutTitleIcon(ref int y)
        {
            int labelX = BeginX;
            Image image = this.TitleIcon;
            if (image != null)
            {
                int bs = ButtonSize;
                TitlePicture.SizeMode = PictureBoxSizeMode.Zoom;
                TitlePicture.Bounds = new Rectangle(labelX, y, bs, bs);
                if (TitlePicture.Image == null)
                    TitlePicture.Image = image;
                if (!TitlePicture.IsSetVisible())
                    TitlePicture.Visible = true;
                labelX += bs + ButtonSpace;
            }
            else
            {
                TitlePicture.Visible = false;
            }
            this.TitleLabel.Left = labelX;
        }
        /// <summary>
        /// Rozmístí buttony.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void DoLayoutButtons(ref int x, ref int y)
        {
            // Zjistím, zda bude někdy možno zobrazit Close button:
            bool isPrimaryPanel = this.IsPrimaryPanel;
            bool canCloseVisible = GetItemVisibility(CloseButtonVisibility, true, isPrimaryPanel) || GetItemVisibility(CloseButtonVisibility, false, isPrimaryPanel);
            if (canCloseVisible)
            {   // Button může mít nastaveno Visible = true; podle stavu myši:
                x -= ButtonSize;
                CloseButton.Location = new Point(x, y);
                x -= (2 * ButtonSpace);
            }
            else
            {   // Button bude mít stále Visible = false:
                CloseButton.Location = new Point(x, y);
            }
        }
        /// <summary>
        /// Souřadnice X kde začíná TitleIcon nebo TitleText
        /// Tato hodnota je upravená aktuálním Zoomem.
        /// </summary>
        protected virtual int BeginX { get { return DxComponent.ZoomToGuiInt(6); } }
        /// <summary>
        /// Přídavek Y k výšce Labelu, do výšky celého panelu.
        /// Tato hodnota je upravená aktuálním Zoomem.
        /// </summary>
        protected virtual int HeightAdd { get { return DxComponent.ZoomToGuiInt(12); } }
        /// <summary>
        /// Velikost buttonu Close a Dock, vnější. Button je čtvercový.
        /// Tato hodnota je upravená aktuálním Zoomem.
        /// </summary>
        protected virtual int ButtonSize { get { return DxComponent.ZoomToGuiInt(24); } }
        /// <summary>
        /// Mezera mezi sousedními buttony. Mezera mezi skupinami je dvojnásobná.
        /// </summary>
        protected virtual int ButtonSpace { get { return 3; } }
        /// <summary>
        /// Okraje panelu
        /// </summary>
        protected virtual int PanelMargin { get { return 3; } }
        /// <summary>
        /// Nastaví šířku pro <see cref="TitleLabel"/> podle aktuální hodnoty <see cref="TitleLabelRight"/>
        /// </summary>
        protected void DoLayoutTitleLabel()
        {
            int x = TitleLabel.Left;
            int w = (TitleLabelRight - x);
            int h = TitleLabel.Height;
            int y = y = (this.ClientSize.Height - h) / 2;   // this.Height - 3 - h;
            TitleLabel.Bounds = new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// Ikona titulku
        /// </summary>
        protected System.Windows.Forms.PictureBox TitlePicture;
        /// <summary>
        /// Label titulku
        /// </summary>
        protected DxLabelControl TitleLabel;
        /// <summary>
        /// Close button
        /// </summary>
        protected DxSimpleButton CloseButton;
        /// <summary>
        /// Pozice Right pro TitleLabel.
        /// Nastavuje se v metodách, které řídí Layout a Viditelnost buttonů, obsahuje pozici X nejkrajnějšího buttonu vlevo mínus Space
        /// </summary>
        protected int TitleLabelRight;
        #endregion
        #region Titulkový text, ikona, linka, barvy
        /// <summary>
        /// Obsahuje true pokud this panel je primární = první vytvořený.
        /// Takový panel může mít jiné chování (typicky nemá titulek, a nemá CloseButton), viz ...
        /// </summary>
        public virtual bool IsPrimaryPanel { get; set; } = true;
        /// <summary>
        /// Ikona titulku
        /// </summary>
        public virtual Image TitleIcon { get; set; }
        /// <summary>
        /// Text titulku
        /// </summary>
        public virtual string TitleText { get; set; }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Záporná hodnota: vyjadřuje plnou barvu, udává odstup od horního a dolního okraje titulku.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        public virtual int? LineWidth { get; set; }
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public virtual Color? LineColor { get; set; }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public virtual Color? LineColorEnd { get; set; }
        /// <summary>
        /// Okraje mezi TitlePanel a barvou pozadí, default = 0
        /// </summary>
        public virtual int? TitleBackMargins { get; set; }
        /// <summary>
        /// Barva pozadí titulku.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public virtual Color? TitleBackColor { get; set; }
        /// <summary>
        /// Barva pozadí titulku, konec gradientu vpravo, null = SolidColor.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        public virtual Color? TitleBackColorEnd { get; set; }
        /// <summary>
        /// Aktualizuje ikonu titulku. Neřeší pozice (Location, Bounds), to dělá <see cref="DoLayoutTitleIcon(ref int)"/>.
        /// </summary>
        protected void RefreshTitleIcon()
        {
            Image image = this.TitleIcon;
            if (image != null)
            {
                int bs = ButtonSize;
                TitlePicture.Size = new Size(bs, bs);
                TitlePicture.Image = image;
                TitlePicture.Visible = true;
            }
            else
            {
                TitlePicture.Visible = false;
            }
        }
        /// <summary>
        /// Aktualizuje text titulku
        /// </summary>
        protected void RefreshTitle()
        {
            this.TitleLabel.Text = this.TitleText;
        }
        #endregion
        #region Close button
        /// <summary>
        /// Viditelnost buttonu Close
        /// </summary>
        public virtual ControlVisibility CloseButtonVisibility { get; set; } = ControlVisibility.Allways;
        /// <summary>
        /// Tooltip na buttonu Close
        /// </summary>
        public virtual string CloseButtonToolTip { get; set; }
        /// <summary>
        /// Po kliknutí na tlačítko Close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ClickClose(object sender, EventArgs args)
        {
            OnClickClose();
        }
        /// <summary>
        /// Po kliknutí na tlačítko Dock...
        /// </summary>
        protected virtual void OnClickClose()
        {
            CloseButtonClick?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Uživatel kliknul na button Close
        /// </summary>
        public event EventHandler CloseButtonClick;
        #endregion
        #region Ikony
        /// <summary>
        /// Mají se použít SVG ikony?
        /// </summary>
        public virtual bool UseSvgIcons { get; set; }
        /// <summary>
        /// Aktualizuje vzhled ikon, pokud je to nutné (= pokud došlo ke změně typu ikon anebo velikosti ikon vlivem změny Zoomu).
        /// Aktualizuje i velikost buttonů a ikon.
        /// Bázová třída <see cref="DxTitlePanel"/> na závěr nastavuje <see cref="AppliedSvgIcons"/> = <see cref="UseSvgIcons"/>;
        /// </summary>
        protected virtual void RefreshButtons(bool force = false)
        {
            if (!NeedRefreshIcons(force)) return;

            int btnSize = ButtonSize;
            Size buttonSize = new Size(btnSize, btnSize);
            int imgSize = btnSize - 4;                          // 4 = okraje mezi buttonem a vnitřním Image
            Size imageSize = new Size(imgSize, imgSize);

            string[] icons = CurrentIcons;

            RefreshButton(CloseButton, buttonSize, icons[0], imageSize, CloseButtonToolTip);

            this.AppliedSvgIcons = UseSvgIcons;
            this.AppliedIconSize = btnSize;
        }
        /// <summary>
        /// Naplní daná data do buttonu
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonSize"></param>
        /// <param name="imageName"></param>
        /// <param name="imageSize"></param>
        /// <param name="toolTip"></param>
        protected void RefreshButton(DxSimpleButton button, Size buttonSize, string imageName, Size imageSize, string toolTip)
        {
            button.Size = buttonSize;
            DxComponent.ApplyImage(button.ImageOptions, imageName, null, imageSize, true);
            button.SetToolTip(toolTip);
        }
        /// <summary>
        /// Vrátí true, pokud je třeba provést Refresh ikon. 
        /// Hlídá typ ikon <see cref="UseSvgIcons"/> a velikost ikony <see cref="ButtonSize"/> proti hodnotám naposledy aplikovaným.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        protected bool NeedRefreshIcons(bool force = false)
        {
            return (force || UseSvgIcons != AppliedSvgIcons || ButtonSize != AppliedIconSize);
        }
        /// <summary>
        /// Obsahuje pole ikon pro aktuální typ (SVG / PNG)
        /// </summary>
        protected virtual string[] CurrentIcons
        {
            get
            {
                bool useSvgIcons = UseSvgIcons;
                if (useSvgIcons)
                    return new string[] { "svgimages/hybriddemoicons/bottompanel/hybriddemo_close.svg" };
                else
                    return new string[] { "devav/actions/delete_16x16.png" };
            }
        }
        /// <summary>
        /// Nyní jsou použité SVG ikony?
        /// </summary>
        protected bool AppliedSvgIcons { get; set; }
        /// <summary>
        /// Velikost ikon aplikovaná. Pomáhá řešit redraw ikon po změně Zoomu.
        /// </summary>
        protected int AppliedIconSize { get; set; }
        #endregion
        #region Paint Background and Line
        /// <summary>
        /// OnPaint
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (HasPaintBackground(out int margins, out Color backColor, out Color? backColorEnd))
                PaintBackground(e, margins, backColor, backColorEnd);

            if (HasPaintLine(out int lineWidth, out Color lineColor, out Color? lineColorEnd))
                PaintLine(e, lineWidth, lineColor, lineColorEnd);
        }
        /// <summary>
        /// Vrací true, pokud se má kreslit Backgrounds
        /// </summary>
        /// <param name="margins"></param>
        /// <param name="backColor"></param>
        /// <param name="backColorEnd"></param>
        /// <returns></returns>
        private bool HasPaintBackground(out int margins, out Color backColor, out Color? backColorEnd)
        {
            Color? bc = this.TitleBackColor;
            if (bc.HasValue)
            {
                margins = DxComponent.ZoomToGuiInt(this.TitleBackMargins ?? 0);
                backColor = bc.Value;
                backColorEnd = this.TitleBackColorEnd;
                return true;
            }
            margins = 0;
            backColor = Color.Empty;
            backColorEnd = null;
            return false;
        }
        /// <summary>
        /// Vykreslí Backgrounds
        /// </summary>
        /// <param name="e"></param>
        /// <param name="margins"></param>
        /// <param name="backColor"></param>
        /// <param name="backColorEnd"></param>
        private void PaintBackground(PaintEventArgs e, int margins, Color backColor, Color? backColorEnd)
        {
            Rectangle backBounds = this.ClientRectangle;
            if (margins > (backBounds.Height / 2)) return;
            if (margins > 0)
                backBounds = backBounds.Enlarge(-margins);
            DxComponent.PaintDrawLine(e.Graphics, backBounds, backColor, backColorEnd);
        }
        /// <summary>
        /// Vrací true, pokud se má kreslit Line
        /// </summary>
        /// <param name="lineWidth"></param>
        /// <param name="lineColor"></param>
        /// <param name="lineColorEnd"></param>
        /// <returns></returns>
        private bool HasPaintLine(out int lineWidth, out Color lineColor, out Color? lineColorEnd)
        {
            int? lw = this.LineWidth;
            Color? lc = this.LineColor;
            if (lw.HasValue && lw.Value != 0 && lc.HasValue)
            {
                lineWidth = DxComponent.ZoomToGuiInt(lw.Value);
                lineColor = lc.Value;
                lineColorEnd = this.LineColorEnd;
                return true;
            }
            lineWidth = 0;
            lineColor = Color.Empty;
            lineColorEnd = null;
            return false;
        }
        /// <summary>
        /// Vykreslí Line
        /// </summary>
        /// <param name="e"></param>
        /// <param name="lineWidth"></param>
        /// <param name="lineColor"></param>
        /// <param name="lineColorEnd"></param>
        private void PaintLine(PaintEventArgs e, int lineWidth, Color lineColor, Color? lineColorEnd)
        {
            Rectangle titleBounds = this.TitleLabel.Bounds;
            int ly = 0;
            int lb = 0;
            int mb = this.ClientSize.Height - 1;
            if (lineWidth < 0)
            {
                ly = 1 - lineWidth;
                lb = mb + lineWidth;
            }
            else
            {
                mb--;
                ly = titleBounds.Bottom;
                lb = ly + lineWidth;
                if (lb > mb)
                {
                    lb = mb;
                    ly = lb - lineWidth;
                    if (ly < 0) ly = 0;
                }
            }
            Rectangle lineBounds = new Rectangle(titleBounds.X - 2, ly, titleBounds.Width + 2, (lb - ly));
            DxComponent.PaintDrawLine(e.Graphics, lineBounds, lineColor, lineColorEnd);
        }
        #endregion
        #region Pohyb myši a viditelnost buttonů
        /// <summary>
        /// Inicializace eventů a proměnných pro myší aktivity
        /// </summary>
        protected void MouseActivityInit()
        {
            RegisterMouseActivityEvents(this);
            foreach (Control control in this.Controls)
                RegisterMouseActivityEvents(control);
            this.ParentChanged += Control_MouseActivityChanged;
            this.MouseActivityDetect(true);
        }
        /// <summary>
        /// Zaregistruje pro daný control eventhandlery, které budou řídit viditelnost prvků this panelu (buttony podle myši)
        /// </summary>
        /// <param name="control"></param>
        protected void RegisterMouseActivityEvents(Control control)
        {
            control.MouseEnter += Control_MouseActivityChanged;
            control.MouseLeave += Control_MouseActivityChanged;
            control.MouseMove += Control_MouseMove;
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Control_MouseActivityChanged(object sender, EventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Control_MouseMove(object sender, MouseEventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Provede se po myší aktivitě, zajistí Visible a Enabled pro buttony
        /// </summary>
        /// <param name="force"></param>
        protected void MouseActivityDetect(bool force = false)
        {
            if (this.IsDisposed || this.Disposing) return;

            bool isMouseOnControl = false;
            if (this.Parent != null)
            {
                Point absolutePoint = Control.MousePosition;
                Point relativePoint = this.PointToClient(absolutePoint);
                isMouseOnControl = this.ClientRectangle.Contains(relativePoint);
            }
            if (force || isMouseOnControl != IsMouseOnControl)
            {
                IsMouseOnControl = isMouseOnControl;
                RefreshButtonVisibility(true);
            }
        }
        /// <summary>
        /// Nastaví Visible a Enabled pro buttony podle aktuálního stavu a podle požadavků
        /// </summary>
        /// <param name="doLayoutTitle">Po doběhnutí určení viditelnosti vyvolat <see cref="DoLayoutTitleLabel()"/> ?</param>
        protected virtual void RefreshButtonVisibility(bool doLayoutTitle)
        {
            TitleLabelRight = this.ClientSize.Width - PanelMargin;

            // Tlačítko pro Close:
            bool isCloseVisible = GetItemVisibility(CloseButtonVisibility, IsMouseOnControl, IsPrimaryPanel);
            this.CloseButton.Visible = isCloseVisible;

            // Šířka TitleLabelu:
            if (isCloseVisible) TitleLabelRight = this.CloseButton.Location.X - ButtonSpace;

            // Upravit šířku TitleLabelu:
            if (doLayoutTitle) DoLayoutTitleLabel();
        }
        /// <summary>
        /// Vrátí true pokud control s daným režimem viditelnosti má být viditelný, při daném stavu myši na controlu
        /// </summary>
        /// <param name="controlVisibility"></param>
        /// <param name="isMouseOnControl"></param>
        /// <param name="isPrimaryPanel"></param>
        /// <returns></returns>
        protected bool GetItemVisibility(ControlVisibility controlVisibility, bool isMouseOnControl, bool isPrimaryPanel)
        {
            bool isAlways = (isPrimaryPanel ? controlVisibility.HasFlag(ControlVisibility.OnPrimaryPanelAllways) : controlVisibility.HasFlag(ControlVisibility.OnNonPrimaryPanelAllways));
            bool isOnMouse = (isPrimaryPanel ? controlVisibility.HasFlag(ControlVisibility.OnPrimaryPanelOnMouse) : controlVisibility.HasFlag(ControlVisibility.OnNonPrimaryPanelOnMouse));
            return (isAlways || (isOnMouse && isMouseOnControl));
        }
        /// <summary>
        /// Obsahuje true, pokud je myš nad controlem (nad kterýmkoli prvkem), false když je myš mimo
        /// </summary>
        protected bool IsMouseOnControl;
        #endregion
        #region Drag and Drop
        /// <summary>
        /// Je povoleno přemístění titulku pomocí Drag And Drop
        /// </summary>
        public virtual bool DragDropEnabled { get; set; }
        /// <summary>
        /// Init
        /// </summary>
        protected void DragAndDropInit()
        {
        }

        /*     TODO - nesmazat, doprogramovat - Drag and Drop panelů !!!

        /// <summary>
        /// Init
        /// </summary>
        protected void DragAndDropInit()
        {
            this.DragAndDropInit(this);
            this.DragAndDropInit(_TitleLabel);
            this.DragAndDropReset();
        }

        protected void DragAndDropInit(Control control)
        {
            control.MouseDown += DragAndDrop_MouseDown;
            control.MouseMove += DragAndDrop_MouseMove;
            control.MouseUp += DragAndDrop_MouseUp;
        }
        private void DragAndDropReset()
        {
            this.DragAndDrop_State = DragAndDrop_StateType.None;
            this.DragAndDrop_DownLocation = null;
            this.DragAndDrop_DownArea = null;
            this.DragAndDrop_VoidSize = SystemInformation.DragSize;
        }
        private void DragAndDrop_MouseDown(object sender, MouseEventArgs e)
        {
            Point mousePoint = Control.MousePosition;
            if (e.Button == MouseButtons.Left && DragDropEnabled)
            {
                this.DragAndDrop_State = DragAndDrop_StateType.MouseDown;
                this.DragAndDrop_DownLocation = mousePoint;
                Size voidSize = this.DragAndDrop_VoidSize;
                Point voidPoint = new Point(mousePoint.X - voidSize.Width / 2, mousePoint.Y - voidSize.Height / 2);
                this.DragAndDrop_DownArea = new Rectangle(voidPoint, voidSize);
            }
        }
        private void DragAndDrop_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = Control.MousePosition;
            switch (this.DragAndDrop_State)
            {
                case DragAndDrop_StateType.MouseDown:
                    if (this.DragAndDrop_DownArea.HasValue && !this.DragAndDrop_DownArea.Value.Contains(mousePoint))
                    {
                        this.DragAndDrop_State = DragAndDrop_StateType.MouseMove;
                        this.DragAndDrop_DownArea = null;
                        DragAndDrop_Start(mousePoint);
                        DragAndDrop_Move(mousePoint);
                    }
                    break;
                case DragAndDrop_StateType.MouseMove:
                    DragAndDrop_Move(mousePoint);
                    break;
            }
        }
        private void DragAndDrop_MouseUp(object sender, MouseEventArgs e)
        {
            Point mousePoint = Control.MousePosition;
            switch (this.DragAndDrop_State)
            {
                case DragAndDrop_StateType.MouseDown:
                case DragAndDrop_StateType.MouseCancel:
                    DragAndDrop_End(mousePoint);
                    break;
                case DragAndDrop_StateType.MouseMove:
                    DragAndDrop_Drop(mousePoint);
                    DragAndDrop_End(mousePoint);
                    break;
            }
        }
        protected void DragAndDrop_Start(Point mousePoint)
        {
            // this.DoDragDrop(this, DragDropEffects.Move);
        }

        protected void DragAndDrop_Move(Point mousePoint)
        {
        }
        protected void DragAndDrop_Drop(Point mousePoint)
        {

        }
        protected void DragAndDrop_End(Point mousePoint)
        {
            this.DoDragDrop(this, DragDropEffects.None);

            this.DragAndDrop_State = DragAndDrop_StateType.None;
            this.DragAndDrop_DownLocation = null;
            this.DragAndDrop_DownArea = null;
        }


        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
        }
        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
        {
            base.OnQueryContinueDrag(qcdevent);
        }
        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
        }
        protected override void OnDragOver(DragEventArgs drgevent)
        {
            base.OnDragOver(drgevent);
        }
        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
        }

        private DragAndDrop_StateType DragAndDrop_State;
        private Point? DragAndDrop_DownLocation;
        private Rectangle? DragAndDrop_DownArea;
        private Size DragAndDrop_VoidSize;
        private enum DragAndDrop_StateType { None, MouseDown, MouseMove, MouseCancel }



        */

        #endregion
        #region ISubscriberToZoomChange
        void ISubscriberToZoomChange.ZoomChanged()
        {
            DoLayout();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DxComponent.UnSubscribeToZoomChange(this);
            base.Dispose(disposing);
        }
        #endregion
    }
    #endregion
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
        /// První UserControl v Panel1 (ten vlevo nebo nahoře)
        /// </summary>
        public Control Control1 { get; private set; }
        /// <summary>
        /// První UserControl v Panel2 (ten vpravo nebo dole)
        /// </summary>
        public Control Control2 { get; private set; }
        /// <summary>
        /// Orientace splitteru (té dělící čáry): 
        /// <see cref="Orientation.Horizontal"/> když splitter je vodorovný, pak <see cref="Control1"/> je nahoře a <see cref="Control2"/> je dole;
        /// <see cref="Orientation.Vertical"/> když splitter je svislý, pak <see cref="Control1"/> je vlevo a <see cref="Control2"/> je vpravo.
        /// </summary>
        public Orientation SplitterOrientation { get; private set; }
        /// <summary>
        /// Pozice splitteru zleva (když <see cref="SplitterOrientation"/> = <see cref="Orientation.Vertical"/>);
        /// nebo shora (když <see cref="SplitterOrientation"/> = <see cref="Orientation.Horizontal"/>).
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
    /// <summary>
    /// Informace o požadovaném cíli dokování
    /// </summary>
    public class DxLayoutTitleDockPositionArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dockPosition"></param>
        public DxLayoutTitleDockPositionArgs(LayoutPosition dockPosition)
        {
            this.DockPosition = dockPosition;
        }
        /// <summary>
        /// Strana pro zadokování
        /// </summary>
        public LayoutPosition DockPosition { get; private set; }
    }
    /// <summary>
    /// Informace o prostoru v layoutu a o UserControlu v něm umístěném
    /// </summary>
    public class DxLayoutItemInfo
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="areaSize"></param>
        /// <param name="controlId"></param>
        /// <param name="userControl"></param>
        /// <param name="isPrimaryPanel"></param>
        /// <param name="titleText"></param>
        public DxLayoutItemInfo(string areaId, Size areaSize, string controlId, Control userControl, bool isPrimaryPanel, string titleText)
        {
            this.AreaId = areaId;
            this.AreaSize = areaSize;
            this.ControlId = controlId;
            this.UserControl = userControl;
            this.IsPrimaryPanel = isPrimaryPanel;
            this.TitleText = titleText;
        }
        /// <summary>
        /// ID prostoru
        /// </summary>
        public string AreaId { get; private set; }
        /// <summary>
        /// Velikost prostoru, typicky jej využívá celý Control pokud je Docked
        /// </summary>
        public Size AreaSize { get; private set; }
        /// <summary>
        /// ID controlu, pokud jej poskytuje prostřednictvím interface: <see cref="ILayoutUserControl.Id"/>
        /// </summary>
        public string ControlId { get; private set; }
        /// <summary>
        /// Control zde umístěný
        /// </summary>
        public Control UserControl { get; private set; }
        /// <summary>
        /// Jde o primární panel
        /// </summary>
        public bool IsPrimaryPanel { get; private set; }
        /// <summary>
        /// Text titulku
        /// </summary>
        public string TitleText { get; private set; }
        /// <summary>
        /// Uvolní objekty v this prvku. Neprovádí Dispose.
        /// </summary>
        internal void ReleaseContent()
        {
            this.UserControl = null;
        }
    }
    /// <summary>
    /// Viditelnost prvků v rámci controlů
    /// </summary>
    [Flags]
    public enum ControlVisibility
    {
        /// <summary>
        /// Prvek není viditelný nikdy
        /// </summary>
        None = 0,
        /// <summary>
        /// Pro primární panel:  Prvek je viditelný pouze tehdy, když je myš nad panelem
        /// </summary>
        OnPrimaryPanelOnMouse = 1,
        /// <summary>
        /// Pro primární panel: Prvek je viditelný vždy
        /// </summary>
        OnPrimaryPanelAllways = 2,
        /// <summary>
        /// Pro NONprimární panel:  Prvek je viditelný pouze tehdy, když je myš nad panelem
        /// </summary>
        OnNonPrimaryPanelOnMouse = 0x10,
        /// <summary>
        /// Pro NONprimární panel: Prvek je viditelný vždy
        /// </summary>
        OnNonPrimaryPanelAllways = 0x20,
        /// <summary>
        /// Pod myší, na všech panelech
        /// </summary>
        OnMouse = OnPrimaryPanelOnMouse | OnNonPrimaryPanelOnMouse,
        /// <summary>
        /// Vždy, na všech panelech
        /// </summary>
        Allways = OnPrimaryPanelAllways | OnNonPrimaryPanelAllways,
        /// <summary>
        /// Převzít nastavení parenta
        /// </summary>
        ByParent = 0x10000,
        /// <summary>
        /// Default = pod myší vždy
        /// </summary>
        Default = OnMouse
    }
    /// <summary>
    /// Jaké buttony budou zobrazeny na dosud nevyužitém panelu
    /// </summary>
    [Flags]
    public enum EmptyPanelVisibleButtons
    {
        /// <summary>
        /// Žádný
        /// </summary>
        None,
        /// <summary>
        /// Close button = prázdný panel bude možno zavřít
        /// </summary>
        Close = 0x01,
        /// <summary>
        /// Dock buttony = panel bude možno přemístit jinam
        /// </summary>
        Dock = 0x02,
        /// <summary>
        /// Close i Dock buttony
        /// </summary>
        All = Close | Dock
    }
    /// <summary>
    /// Optional interface, který poskytuje UserControl pro LayoutPanel
    /// </summary>
    public interface ILayoutUserControl
    {
        /// <summary>
        /// ID controlu, dostává se do LayoutInfo
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Viditelnost titulku
        /// </summary>
        bool TitleVisible { get; }
        /// <summary>
        /// Text do titulku
        /// </summary>
        string TitleText { get; }
        /// <summary>
        /// Ikonka před textem
        /// </summary>
        Image TitleIcon { get; }
        /// <summary>
        /// Šířka linky pod textem v pixelech. Násobí se Zoomem. Pokud je null nebo 0, pak se nekreslí.
        /// Záporná hodnota: vyjadřuje plnou barvu, udává odstup od horního a dolního okraje titulku.
        /// Může být extrémně vysoká, pak je barvou podbarven celý titulek.
        /// Barva je dána v <see cref="LineColor"/> a <see cref="LineColorEnd"/>.
        /// </summary>
        int? LineWidth { get; }
        /// <summary>
        /// Barva linky pod titulkem.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud je null, pak linka se nekreslí.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? LineColor { get; }
        /// <summary>
        /// Barva linky pod titulkem na konci (Gradient zleva doprava).
        /// Pokud je null, pak se nepoužívá gradientní barva.
        /// Šířka linky je dána v pixelech v <see cref="LineWidth"/>.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? LineColorEnd { get; }
        /// <summary>
        /// Okraje mezi TitlePanel a barvou pozadí, default = 0
        /// </summary>
        int? TitleBackMargins { get; }
        /// <summary>
        /// Barva pozadí titulku.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? TitleBackColor { get; }
        /// <summary>
        /// Barva pozadí titulku, konec gradientu vpravo, null = SolidColor.
        /// Pokud je null, pak titulek má defaultní barvu pozadí podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy pozadí = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? TitleBackColorEnd { get; }
        /// <summary>
        /// Barva písma titulku.
        /// Pokud je null, pak titulek má defaultní barvu písma podle skinu.
        /// Pokud má hodnotu, pak hodnota A (Alpha) vyjadřuje "průhlednost" barvy textu = míru překrytí defaultní barvy (dle skinu) barvou zde deklarovanou.
        /// </summary>
        Color? TitleTextColor { get; }
        /// <summary>
        /// Událost volaná po změně jakékoli hodnoty v <see cref="TitleText"/> nebo <see cref="TitleBackColor"/> nebo <see cref="TitleTextColor"/>
        /// </summary>
        event EventHandler TitleChanged;
    }
    #endregion
}
