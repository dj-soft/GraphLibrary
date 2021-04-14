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
        /// Pole všech uživatelských controlů (neobsahuje tedy SplitContainery). Pole je lineární, nezohledňuje aktuální rozložení.
        /// </summary>
        public Control[] AllControls { get { return _GetAllControls(); } }
        /// <summary>
        /// Aktuální počet controlů
        /// </summary>
        public int ControlCount { get { return _Controls.Count; } }
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
        /// Používat SVG ikony (true) / PNG ikony (false): default = true
        /// </summary>
        public bool UseSvgIcons { get { return _UseSvgIcons; } set { _UseSvgIcons = value; _RefreshControls(); } }
        private bool _UseSvgIcons;
        /// <summary>
        /// Povolení pro zobrazování kontextového menu na Splitteru (pro změnu orientace Horizontální - Vertikální).
        /// Default = false.
        /// </summary>
        public bool SplitterContextMenuEnabled { get; set; }
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
        #endregion
        #region Přidání, refresh titulku, odebrání a evidence UserControlů
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
        /// <param name="previousSize">Nastavit tuto velikost v pixelech pro Previous control, null = neřešit (dá 50%)</param>
        /// <param name="currentSize"></param>
        /// <param name="previousSizeRatio"></param>
        /// <param name="currentSizeRatio"></param>
        public void AddControl(Control userControl, Control previousControl, LayoutPosition position, 
            string titleText = null,
            int? previousSize = null, int? currentSize = null, float? previousSizeRatio = null, float? currentSizeRatio = null)
        {
            if (userControl == null) return;

            AddControlParams parameters = new AddControlParams() { Position = position, TitleText = titleText,
                PreviousSize = previousSize, CurrentSize = currentSize, PreviousSizeRatio = previousSizeRatio, CurrentSizeRatio = currentSizeRatio };
            int prevIndex = _GetIndexOfUserControl(previousControl);
            if (prevIndex < 0)
                _AddControlDefault(userControl, parameters);
            else
                _AddControlNear(userControl, prevIndex, parameters);
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
            var hostControl = _Controls[index].HostControl;
            if (hostControl != null)
                hostControl.TitleText = titleText;
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
                _AddControlTo(userControl, this, LayoutPosition.None, parameters.TitleText);
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
            _RemoveControlFromParent(nearHost, parent);

            // 3. Do toho parenta vložíme místo controlu nový SplitterContainer a určíme panely pro stávající control a pro nový prvek (Panel1 a Panel2, podle parametru):
            DevExpress.XtraEditors.SplitContainerControl newSplitContainer = _CreateNewContainer(parameters, out DevExpress.XtraEditors.SplitGroupPanel currentControlPanel, out DevExpress.XtraEditors.SplitGroupPanel newControlPanel);
            parent.Controls.Add(newSplitContainer);
            newSplitContainer.SplitterPosition = _GetSplitterPosition(parent.ClientSize, parameters);        // Až po vložení do Parenta
            newSplitContainer.SplitterMoved += _SplitterMoved;                                               // Až po nastavení pozice

            // 4. Stávající prvek vložíme jako Child do jeho nově určeného panelu, a vepíšeme to i do evidence:
            currentControlPanel.Controls.Add(nearHost);
            nearInfo.Parent = currentControlPanel;
            nearInfo.DockButtonDisabledPosition = parameters.PairPosition;

            // 5. Nový UserControl vložíme do jeho panelu a vytvoříme pár a přidáme jej do evidence:
            _AddControlTo(userControl, newControlPanel, parameters.Position, parameters.TitleText);
        }
        /// <summary>
        /// Přidá daný control do daného parenta jako jeho Child, dá Dock = Fill, a přidá informaci do evidence v <see cref="_Controls"/>.
        /// </summary>
        /// <param name="userControl"></param>
        /// <param name="parent"></param>
        /// <param name="dockPosition"></param>
        /// <param name="titleText"></param>
        private void _AddControlTo(Control userControl, Control parent, LayoutPosition dockPosition, string titleText)
        {
            DxLayoutItemPanel hostControl = new DxLayoutItemPanel(this);
            hostControl.UserControl = userControl;
            hostControl.TitleBarVisible = true;
            hostControl.TitleText = titleText;
            hostControl.IsPrimaryPanel = (ControlCount == 0);
            hostControl.DockButtonDisabledPosition = dockPosition;
            hostControl.DockButtonClick += ItemPanel_DockButtonClick;
            hostControl.CloseButtonClick += ItemPanel_CloseButtonClick;

            parent.Controls.Add(hostControl);
            LayoutTileInfo tileInfo = new LayoutTileInfo(parent, hostControl, userControl);
            _Controls.Add(tileInfo);
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
            OnUserControlRemoveBefore(args);
            return !args.Cancel;
        }
        /// <summary>
        /// Reálně odebere daný panel layoutu ze struktury (vizuálně i datově).
        /// </summary>
        /// <param name="removeIndex"></param>
        /// <param name="removeInfo"></param>
        private void _RemoveUserControlInternal(int removeIndex, LayoutTileInfo removeInfo)
        {
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
                            // Nemění se ani pozice (DockPosition) controlů.
                            splitParent.Controls.Add(pairContainer);
                        }
                    }
                    else
                    {   // Takže jsme odebrali prvek UserControl (pomocí jeho Host) z jednoho panelu, a na párovém panelu máme jiný prvek UserControl.
                        // Nechceme mít prázdný panel, a ani nechceme prázdný panel schovávat, chceme mít čistý layout = viditelné všechny panely a na nich vždy jeden control!
                        // Najdeme a podržíme si tedy onen párový control; pak odebereme SplitContaner, a na jeho místo vložíme ten párový Control:
                        // Vyřešíme i jeho DockPosition...
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
                        pairInfo.DockButtonDisabledPosition = pairInfo.CurrentDockPosition;
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
        private static void _RemoveControlFromParent(Control control, Control parent)
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
                this.OnCloseButtonClickAfter(args);
                if (!args.Cancel)
                    _RemoveUserControl(index);
            }
        }
        #endregion
        #region Interaktivita vnitřní - reakce na odebrání controlu, na pohyb splitteru, na změnu orientace splitteru, na změnu dokování 
        /// <summary>
        /// Vyvolá se po odebrání každého uživatelského controlu.
        /// Zavolá event <see cref="UserControlRemoved"/>.
        /// </summary>
        protected virtual void OnCloseButtonClickAfter(TEventCancelArgs<Control> args)
        {
            CloseButtonClickAfter?.Invoke(this, args);
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
        /// Vyvolá se po pohybu splitteru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SplitterMoved(object sender, EventArgs e)
        {
            if (_SplitterMovedDisable) return;

            if (sender is DevExpress.XtraEditors.SplitContainerControl splitContainer)
            {
                UserControlPair pair = UserControlPair.CreateForContainer(splitContainer);
                _FillControlParentsToPair(pair);
                OnSplitterPositionChanged(pair.CreateSplitterChangedArgs());
            }
        }
        /// <summary>
        /// true = Dočasné potlačení volání události <see cref="OnSplitterPositionChanged(DxLayoutPanelSplitterChangedArgs)"/>.
        /// Používá se v době změny layoutu v metodě <see cref="_SetOrientation(UserControlPair, bool, bool)"/>, kdy se mění jak orientace, tak i pozice splitteru.
        /// Pak nechci volat samostatný event <see cref="SplitterPositionChanged"/>, protože bude následně volán event <see cref="LayoutPanelChanged"/>.
        /// </summary>
        private bool _SplitterMovedDisable;
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
        #endregion
        #region Kontextové menu pro změnu orientace splitteru
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
                DxLayoutPanelSplitterChangedArgs args = pair.CreateSplitterChangedArgs();
                OnLayoutPanelChanged(args);
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
        #region Třídy LayoutTileInfo (evidence UserControlů) a AddControlParams (parametry pro přidání UserControlu) a UserControlPair (data o jednom Splitteru)
        /// <summary>
        /// Třída obsahující WeakReference na 
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

                RefreshLayoutFromHost(true);
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
            public LayoutPosition CurrentDockPosition
            {
                get
                {
                    DxLayoutItemPanel hostControl = HostControl;
                    if (hostControl == null) return LayoutPosition.None;

                    bool isHorizontal = false;
                    int panelId = 0;


                    DevExpress.XtraEditors.SplitGroupPanel dxPanel = hostControl.SearchForParentOfType<DevExpress.XtraEditors.SplitGroupPanel>();
                    if (dxPanel != null && dxPanel.Parent is DevExpress.XtraEditors.SplitContainerControl dxSplitContainer)
                    {   // DevExpress SplitPanel:
                        isHorizontal = dxSplitContainer.Horizontal;
                        panelId = (Object.ReferenceEquals(dxPanel, dxSplitContainer.Panel1) ? 1 :
                                  (Object.ReferenceEquals(dxPanel, dxSplitContainer.Panel2) ? 2 : 0));
                    }
                    else
                    {   // WinForm SplitPanel?
                        SplitterPanel wfPanel = hostControl.SearchForParentOfType<SplitterPanel>();
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
            /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
            /// Pokud sem bude vložena hodnota <see cref="LayoutPosition.None"/>, pak dokovací buttony nebudou viditelné (bez ohledu na <see cref="DxLayoutPanel.DockButtonVisibility"/>.
            /// </summary>
            public LayoutPosition DockButtonDisabledPosition 
            { 
                get { var hostControl = HostControl; return hostControl?.DockButtonDisabledPosition ?? LayoutPosition.None; } 
                set { var hostControl = HostControl; if (hostControl != null) hostControl.DockButtonDisabledPosition = value; }
            }
            /// <summary>
            /// Provede načtení dat z <see cref="UserControl"/>, pokud tento je typu <see cref="ILayoutUserControl"/>.
            /// </summary>
            public void RefreshLayoutFromHost(bool registerEvents = false)
            {
                var iLayoutUserControl = this.ILayoutUserControl;
                if (iLayoutUserControl == null) return;

                if (registerEvents)
                {
                    iLayoutUserControl.TitleTextChanged += ILayoutUserControl_TitleTextChanged;
                }

                var hostControl = this.HostControl;
                if (hostControl == null) return;

                hostControl.TitleText = iLayoutUserControl.TitleText;
            }
            /// <summary>
            /// Po změně textu v UserControlu
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void ILayoutUserControl_TitleTextChanged(object sender, EventArgs e)
            {
                RefreshLayoutFromHost();
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
                        _RemoveControlFromParent(control1, parent1);
                        if (tileInfo1 != null)
                            tileInfo1.Parent = null;
                    }
                    if (parent2 != null && control2 != null)
                    {
                        _RemoveControlFromParent(control2, parent2);
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
    #region class DxLayoutItemPanel : panel hostující v sobě Title a UserControl
    /// <summary>
    /// Panel, který slouží jako hostitel pro jeden uživatelský control v rámci <see cref="DxLayoutPanel"/>.
    /// Obsahuje prostor pro titulek, zavírací křížek a OnMouse buttony pro předokování aktuálního prvku v rámci parent SplitContaineru.
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
        #endregion
        #region Naše vlastní data pro Titulkový panel, eventy
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
                if (_TitleBar != null && _TitleBar.VisibleInternal != !titleBarVisible)
                    _TitleBar.VisibleInternal = !titleBarVisible;
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
    /// Titulkový řádek. Obsahuje titulek a několi buttonů.
    /// </summary>
    public class DxLayoutTitlePanel : DxPanelControl
    {
        #region Konstuktor, vnitřní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLayoutTitlePanel()
        {
            this.Initialize(null);
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public DxLayoutTitlePanel(DxLayoutItemPanel owner)
        {
            this.Initialize(owner);
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        /// <param name="owner"></param>
        private void Initialize(DxLayoutItemPanel owner)
        {
            __Owner = owner;

            string[] icons = CurrentIcons;
            _DockLeftButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, resourceName: icons[0], visible: false, tag: LayoutPosition.Left);
            _DockTopButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, resourceName: icons[1], visible: false, tag: LayoutPosition.Top);
            _DockBottomButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, resourceName: icons[2], visible: false, tag: LayoutPosition.Bottom);
            _DockRightButton = DxComponent.CreateDxMiniButton(100, 2, 24, 24, this, _ClickDock, resourceName: icons[3], visible: false, tag: LayoutPosition.Right);
            _CloseButton = DxComponent.CreateDxMiniButton(200, 2, 24, 24, this, _ClickClose, resourceName: icons[4], visible: false);

            _TitleLabel = DxComponent.CreateDxLabel(12, 6, 200, this, "", LabelStyleType.MainTitle, hAlignment: HorzAlignment.Near, autoSizeMode: DevExpress.XtraEditors.LabelAutoSizeMode.Horizontal);

            this.AppliedSvgIcons = this.UseSvgIcons;

            this.Height = 35;
            this.Dock = DockStyle.Top;

            MouseActivityInit();
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
                DockButtonClick?.Invoke(this, new DxLayoutTitleDockPositionArgs(dockPosition));
            }
        }
        /// <summary>
        /// Po kliknutí na tlačítko Close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _ClickClose(object sender, EventArgs args)
        {
            CloseButtonClick?.Invoke(this, EventArgs.Empty);
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
        /// Zajistí rozmístění controlů
        /// </summary>
        public void DoLayout()
        {
            if (_TitleLabel == null) return;
            int width = this.ClientSize.Width;
            int height = _TitleLabel.Bottom + 6;
            if (height < 28) height = 28;
            if (this.Height != height)
            {
                this.Height = height;
                // Nastavení jiné výšky než je aktuální (=v minulém řádku) vyvolá rekurzivně this metodu, v té už nepůjdeme touto větví, ale nastavíme souřadnice buttonů (za else).
            }
            else
            {
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
        }
        private DxLabelControl _TitleLabel;
        private DxSimpleButton _DockLeftButton;
        private DxSimpleButton _DockTopButton;
        private DxSimpleButton _DockBottomButton;
        private DxSimpleButton _DockRightButton;
        private DxSimpleButton _CloseButton;
        #endregion
        #region Data získaná z Ownerů, vlastní eventy
        /// <summary>
        /// Text titulku
        /// </summary>
        public string TitleText { get { return Owner?.TitleText ?? ""; } }
        /// <summary>
        /// Obsahuje true pokud this panel je primární = první vytvořený.
        /// Takový panel může mít jiné chování (typicky nemá titulek, a nemá CloseButton), viz ...
        /// </summary>
        public bool IsPrimaryPanel { get { return Owner?.IsPrimaryPanel ?? false; } }
        /// <summary>
        /// Viditelnost buttonů Dock
        /// </summary>
        public ControlVisibility DockButtonVisibility { get { return LayoutPanel?.DockButtonVisibility ?? ControlVisibility.Default; } }
        /// <summary>
        /// Viditelnost buttonu Close
        /// </summary>
        public ControlVisibility CloseButtonVisibility { get { return LayoutPanel?.CloseButtonVisibility ?? ControlVisibility.Default; } }
        /// <summary>
        /// Pozice Dock buttonu, který je aktuálně Disabled. To je ten, na jehož straně je nyní panel dokován, a proto by neměl být tento button dostupný.
        /// </summary>
        public LayoutPosition DockButtonDisabledPosition { get { return Owner?.DockButtonDisabledPosition ?? LayoutPosition.None; } }
        /// <summary>
        /// Uživatel kliknul na button Dock (strana je v argumentu)
        /// </summary>
        public event EventHandler<DxLayoutTitleDockPositionArgs> DockButtonClick;
        /// <summary>
        /// Uživatel kliknul na button Close
        /// </summary>
        public event EventHandler CloseButtonClick;
        #endregion
        #region Pohyb myši a viditelnost buttonů
        /// <summary>
        /// Inicializace eventů a proměnných pro myší aktivity
        /// </summary>
        private void MouseActivityInit()
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
        private void RegisterMouseActivityEvents(Control control)
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
        private void Control_MouseActivityChanged(object sender, EventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Eventhandler pro detekci myší aktivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            MouseActivityDetect();
        }
        /// <summary>
        /// Provede se po myší aktivitě, zajistí Visible a Enabled pro buttony
        /// </summary>
        /// <param name="force"></param>
        private void MouseActivityDetect(bool force = false)
        {
            Point mousePoint = this.PointToClient(Control.MousePosition);
            bool isMouseOnControl = this.Bounds.Contains(mousePoint);
            if (force || isMouseOnControl != _IsMouseOnControl)
            {
                _IsMouseOnControl = isMouseOnControl;
                RefreshButtonVisibility();
            }
        }
        /// <summary>
        /// Obsahuje true, pokud je myš nad controlem (nad kterýmkoli prvkem), false když je myš mimo
        /// </summary>
        private bool _IsMouseOnControl;
        #endregion
        #region Refreshe (obsah, viditelnost, interaktivní tlačítka podle stavu myši)
        /// <summary>
        /// Aplikuje ikony požadovaného druhu
        /// </summary>
        internal void RefreshControl()
        {
            this.RefreshTitle();
            this.RefreshIcons();
            this.RefreshButtonVisibility();
        }
        /// <summary>
        /// Aktualizuje text titulku
        /// </summary>
        private void RefreshTitle()
        {
            this._TitleLabel.Text = this.TitleText;
        }
        /// <summary>
        /// Aktualizuje vzhled ikon, pokud je to nutné
        /// </summary>
        private void RefreshIcons(bool force = false)
        {
            bool appliedSvgIcons = AppliedSvgIcons;
            bool useSvgIcons = UseSvgIcons;

            if (!force && appliedSvgIcons == useSvgIcons) return;

            string[] icons = CurrentIcons;
            Size size = new Size(20, 20);

            DxComponent.ApplyImage(_DockLeftButton.ImageOptions, null, icons[0], size, true);
            DxComponent.ApplyImage(_DockTopButton.ImageOptions, null, icons[1], size, true);
            DxComponent.ApplyImage(_DockBottomButton.ImageOptions, null, icons[2], size, true);
            DxComponent.ApplyImage(_DockRightButton.ImageOptions, null, icons[3], size, true);
            DxComponent.ApplyImage(_CloseButton.ImageOptions, null, icons[4], size, true);

            this.AppliedSvgIcons = useSvgIcons;
        }
        /// <summary>
        /// Počet aktuálně zobrazených panelů v hlavním okně
        /// </summary>
        private int LayoutPanelControlCount { get { return (this.LayoutPanel?.ControlCount ?? 0); } }
        /// <summary>
        /// Mají se použít SVG ikony?
        /// </summary>
        private bool UseSvgIcons { get { return (this.LayoutPanel?.UseSvgIcons ?? true); } }
        /// <summary>
        /// Nyní jsou použité SVG ikony?
        /// </summary>
        private bool AppliedSvgIcons;
        /// <summary>
        /// Obsahuje pole ikon pro aktuální typ (SVG / PNG)
        /// </summary>
        private string[] CurrentIcons
        {
            get
            {
                bool useSvgIcons = UseSvgIcons;

                if (useSvgIcons)
                    return new string[]
                    {
                    "svgimages/align/alignverticalleft.svg",
                    "svgimages/align/alignhorizontaltop.svg",
                    "svgimages/align/alignhorizontalbottom.svg",
                    "svgimages/align/alignverticalright.svg",
                    "images/xaf/templatesv2images/action_delete.svg"
                    };
                else
                    return new string[]
                    {
                    "images/alignment/alignverticalleft_16x16.png",
                    "images/alignment/alignhorizontaltop_16x16.png",
                    "images/alignment/alignhorizontalbottom_16x16.png",
                    "images/alignment/alignverticalright_16x16.png",
                    "devav/actions/delete_16x16.png"
                    };
            }
        }
        /// <summary>
        /// Nastaví Visible a Enabled pro buttony podle aktuálního stavu a podle požadavků
        /// </summary>
        private void RefreshButtonVisibility()
        {
            bool isMouseOnControl = _IsMouseOnControl;
            bool isPrimaryPanel = this.IsPrimaryPanel;

            // Tlačítka pro dokování budeme zobrazovat pouze tehdy, když hlavní panel zobrazuje více než jeden prvek. Pro méně prvků nemá dokování význam!
            bool hasMorePanels = (this.LayoutPanelControlCount > 1);
            bool isDockVisible = hasMorePanels && GetItemVisibility(DockButtonVisibility, isMouseOnControl, isPrimaryPanel);
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

            // Tlačítko pro Close:
            bool isCloseVisible = GetItemVisibility(CloseButtonVisibility, isMouseOnControl, isPrimaryPanel);
            this._CloseButton.Visible = isCloseVisible;
        }
        /// <summary>
        /// Vrátí true pokud control s daným režimem viditelnosti má být viditelný, při daném stavu myši na controlu
        /// </summary>
        /// <param name="controlVisibility"></param>
        /// <param name="isMouseOnControl"></param>
        /// <param name="isPrimaryPanel"></param>
        /// <returns></returns>
        private bool GetItemVisibility(ControlVisibility controlVisibility, bool isMouseOnControl, bool isPrimaryPanel)
        {
            bool isAlways = (isPrimaryPanel ? controlVisibility.HasFlag(ControlVisibility.OnPrimaryPanelAllways) : controlVisibility.HasFlag(ControlVisibility.OnNonPrimaryPanelAllways));
            bool isOnMouse = (isPrimaryPanel ? controlVisibility.HasFlag(ControlVisibility.OnPrimaryPanelOnMouse) : controlVisibility.HasFlag(ControlVisibility.OnNonPrimaryPanelOnMouse));
            return (isAlways || (isOnMouse && isMouseOnControl));
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
        /// Default = pod myší vždy
        /// </summary>
        Default = OnMouse
    }
    /// <summary>
    /// Optional interface, který poskytuje UserControl pro LayoutPanel
    /// </summary>
    public interface ILayoutUserControl
    {
        /// <summary>
        /// Text do titulku
        /// </summary>
        string TitleText { get; }
        /// <summary>
        /// Viditelnost buttonu Close
        /// </summary>
        ControlVisibility CloseButtonVisibility { get; }
        /// <summary>
        /// Událost volaná po změně <see cref="TitleText"/>
        /// </summary>
        event EventHandler TitleTextChanged;
    }
    #endregion
}
