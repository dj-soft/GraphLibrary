using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using DevExpress.XtraBars;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Components
{
    /// <summary>
    /// Panel, který obsahuje log aplikace
    /// </summary>
    public class AppLogPanel : DxPanelControl, IListenerApplicationIdle
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AppLogPanel()
        {
            _InitToolbar();
            _InitAppLog();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _DisconnectAppLog();
            base.Dispose(disposing);

        }
        #region Toolbar
        /// <summary>
        /// Vytvoří a naplní Toolbar
        /// </summary>
        private void _InitToolbar()
        {
            __ToolBar = new DxSingleToolbar(this);
            __ToolBar.RibbonItems = _GetToolbarItems();
            __ToolBar.BarItemClick += __ToolBar_BarItemClick;
        }
        /// <summary>
        /// Uživatel klikl na Toolbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void __ToolBar_BarItemClick(object sender, TEventArgs<IRibbonItem> e)
        {
            switch (e.Item.ItemId)
            {
                case "delete":
                    _BreakPageId = 0;
                    DxComponent.LogClear();
                    break;
                case "addbreak":
                    bool isActive = this.AppLogActive;
                    if (!isActive) this.AppLogActive = true;
                    DxComponent.LogAddLine((_BreakPageId++).ToString() + " ".PadRight(50, '-'));
                    this._RefreshLogText(true);
                    if (!isActive) this.AppLogActive = false;
                    break;
                case "pause":
                    this.AppLogActive = false;
                    break;
                case "resume":
                    this.AppLogActive = true;
                    break;
                case "refresh":
                    this._RefreshLogText(true);
                    break;
            }
        }
        private int _BreakPageId;
        /// <summary>
        /// Vygeneruje a vrátí definici prvků v Toolbaru
        /// </summary>
        /// <returns></returns>
        private IRibbonItem[] _GetToolbarItems()
        {
            string imageDelete = "svgimages/xaf/action_delete.svg";
            string imageRefresh = "svgimages/xaf/action_refresh.svg";
            string imagePause = "svgimages/xaf/action_pauserecording.svg";               // "svgimages/xaf/action_debug_stop.svg"
            string imageResume = "svgimages/xaf/action_resumerecording.svg";             // "svgimages/xaf/action_debug_start.svg"
            string imageInsertBreak = "svgimages/snap/separatorlist.svg";                // "svgimages/snap/separatorlistnone.svg";     "svgimages/snap/separatorpagebreaklist.svg";

            bool isLogActive = this.AppLogActive;

            List<DataRibbonItem> items = new List<DataRibbonItem>();
            items.Add(new DataRibbonItem() { ItemId = "delete", ImageName = imageDelete, ToolTipTitle = "Clear", ToolTipText = "Smaže celý obsah", RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithoutText });
            items.Add(new DataRibbonItem() { ItemId = "addbreak", ImageName = imageInsertBreak, ToolTipTitle = "BreakPage", ToolTipText = "Do logu vloží oddělovací čáru", RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithoutText });
            items.Add(new DataRibbonItem() { ItemId = "pause", ImageName = imagePause, ToolTipTitle = "Pause", ToolTipText = "Pozastaví výpis logu", RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithoutText, ItemIsFirstInGroup = true, ItemType = RibbonItemType.CheckButton, Checked = !isLogActive, RadioButtonGroupName = "LogActivity" });
            items.Add(new DataRibbonItem() { ItemId = "resume", ImageName = imageResume, ToolTipTitle = "Resume", ToolTipText = "Obnoví aktualizace", RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithoutText, ItemType = RibbonItemType.CheckButton, Checked = isLogActive, RadioButtonGroupName = "LogActivity" });
            items.Add(new DataRibbonItem() { ItemId = "refresh", ImageName = imageRefresh, ToolTipTitle = "Refresh", ToolTipText = "Znovu načte obsah logu", RibbonStyle = Noris.Clients.Win.Components.AsolDX.RibbonItemStyles.SmallWithoutText });

            return items.ToArray();
        }
        private DxSingleToolbar __ToolBar;
        #endregion
        #region Aplikační log a Text
        private void _InitAppLog()
        {
            DxComponent.LogTextChanged += DxComponent_LogTextChanged;
            DxComponent.RegisterListener(this);
            __LogText = DxComponent.CreateDxMemoEdit(this, DockStyle.Fill, readOnly: true, tabStop: false);

            // __LogText = new TextBox() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true };
            // this.Controls.Add(__LogText);
        }
        private void _DisconnectAppLog()
        {
            DxComponent.LogTextChanged -= DxComponent_LogTextChanged;
            DxComponent.UnregisterListener(this);
        }
        /// <summary>
        /// Aplikační log je aktivní?
        /// </summary>
        private bool AppLogActive { get { return DxComponent.LogActive; } set { DxComponent.LogActive = value; } }
        /// <summary>
        /// Text Aplikačního logu
        /// </summary>
        private string AppLogText { get { return DxComponent.LogText; } }
        /// <summary>
        /// Po změně obsahu aplikačního logu si uděláme poznámku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DxComponent_LogTextChanged(object sender, EventArgs e)
        {
            _LogIsChanged = true;
        }
        /// <summary>
        /// Metoda je volaná v situaci, kdy aplikační kontext nemá co dělat
        /// </summary>
        void IListenerApplicationIdle.ApplicationIdle()
        {
            this._RefreshLogText(false);
        }
        private void _RefreshLogText(bool force)
        {
            if (force || _LogIsChanged)
            {
                __LogText.Text = _GetAppLogText();
//                 __LogText.Refresh();
            }
            _LogIsChanged = false;
        }
        private string _GetAppLogText()
        {
            var lines = this.AppLogText.Replace("\n", "").Split('\r').ToList();
            lines.Reverse();
            return lines.ToOneString("\r\n");
        }
        private bool _LogIsChanged;
        private DxMemoEdit __LogText;
        // private TextBox __LogText;
        #endregion
    }
    #region class DxSingleToolbar : obecný jednoduchý jednořádkový Toolbar
    /// <summary>
    /// Jednořádkový fixní Toolbar
    /// </summary>
    public class DxSingleToolbar : Bar
    {
        #region Konstruktor, tvorba, nastavení, plnění daty
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        public DxSingleToolbar(Control owner)
            : base()
        {
            this.CreateBarManager(owner);
            this.Initialize();
            this.InitEvents();
            this.FillBarManager();
            this.IsActive = true;
        }
        private BarManager __BarManager;
        private IRibbonItem[] __RibbonItems;
        private void CreateBarManager(Control owner)
        {
            __BarManager = new DevExpress.XtraBars.BarManager();
            __BarManager.Form = owner;

            __BarManager.BeginUpdate();
        }
        private void FillBarManager()
        {
            __BarManager.Bars.Add(this);
            __BarManager.EndUpdate();
        }
        /// <summary>
        /// Inicializace Toolbaru
        /// </summary>
        protected virtual void Initialize()
        {
            DockStyle = BarDockStyle.Top;
            DockRow = 0;
            CanDockStyle = BarCanDockStyle.Top | BarCanDockStyle.Bottom;

            OptionsBar.AllowCollapse = false;
            OptionsBar.AllowDelete = false;
            OptionsBar.AllowQuickCustomization = false;
            OptionsBar.AllowRename = false;
            OptionsBar.AutoPopupMode = BarAutoPopupMode.None;
            OptionsBar.DisableClose = true;
            OptionsBar.DisableCustomization = true;
            OptionsBar.DrawBorder = false;
            OptionsBar.DrawSizeGrip = false;
            OptionsBar.MultiLine = false;
            OptionsBar.UseWholeRow = true;
        }
        /// <summary>
        /// Vytvoří vizuální prvky podle prvků datových a vepíše je do this Toolbaru
        /// </summary>
        /// <param name="items"></param>
        private void _SetDataItems(IRibbonItem[] items)
        {
            __RibbonItems = items;
            __BarManager.BeginUpdate();
            this.ItemLinks.Clear();
            this.AddItems(DxComponent.CreateBarItems(items));
            __BarManager.EndUpdate();
        }
        #endregion
        #region Public property a eventy
        /// <summary>
        /// Je tento Ribbon aktivní?
        /// Výchozí hodnota (nastavená na konci konstruktoru) je true.
        /// Pokud je false, pak se neprovádí eventy Ribbonu (ItemClick, GroupClick).
        /// Setování hodnoty do <see cref="IsActive"/> ji setuje pouze do this instance, nikdy ne do Child mergovaných Ribbonů.
        /// Čtení hodnoty ji vyhodnocuje pouze z this instance.
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Obsah Toolbaru = jednotlivé prvky
        /// </summary>
        public IRibbonItem[] RibbonItems { get { return __RibbonItems; } set { _SetDataItems(value); } }

        #endregion
        #region Interaktivita
        /// <summary>
        /// Provede napojení zdejších eventhandlerů na události komponenty
        /// </summary>
        protected void InitEvents()
        {
            __BarManager.ItemClick += _BarManagerItemClick;
        }
        /// <summary>
        /// Uživatel kliknul na prvek Toolbaru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BarManagerItemClick(object sender, ItemClickEventArgs e)
        {
            if (!this.IsActive) return;

            if (e.Item?.Tag is IRibbonItem ribbonItem)
            {
                _BarItemTestCheckChanges(e.Item, ribbonItem);
                _RunBarItemClick(ribbonItem);
            }
        }
        /// <summary>
        /// Vyvolá metodu <see cref="OnBarItemClick(TEventArgs{IRibbonItem})"/> a event <see cref="BarItemClick"/>
        /// </summary>
        /// <param name="ribbonItem"></param>
        private void _RunBarItemClick(IRibbonItem ribbonItem)
        {
            var args = new TEventArgs<IRibbonItem>(ribbonItem);
            OnBarItemClick(args);
            BarItemClick?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po kliknutí na prvek Toolbaru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnBarItemClick(TEventArgs<IRibbonItem> args) { }
        /// <summary>
        /// Událost volaná po kliknutí na prvek Toolbaru
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> BarItemClick;
        #region CheckButtonClick, CheckBoxClick, RadioButtonClick...
        /// <summary>
        /// Tato metoda řeší změnu hodnoty <see cref="ITextItem.Checked"/> na daném prvku Ribbonu poté, kdy na něj uživatel klikl.
        /// Řeší tedy CheckBoxy, RadioButtony a CheckButtony obou režimů.
        /// U obou typů RadioButtonů řeší zhasnutí okolních (=ne-kliknutých) RadioButtonů v jejich grupě.
        /// <para/>
        /// Volá event <see cref="BarItemCheck"/> a související pro každý prvek, jemuž je změněna hodnota Checked.
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="iRibbonItem"></param>
        private void _BarItemTestCheckChanges(BarItem barItem, IRibbonItem iRibbonItem)
        {
            if (!this.IsActive) return;

            // Z vizuálního objektu BarCheckItem si opíšu jeho Checked do datového objektu:
            if (barItem is BarCheckItem checkItem)
            {   // CheckBox i RadioButton (jde o stejný prvek, pouze s jiným stylem zobrazení):
                _RibbonCheckBoxItemClick(checkItem, iRibbonItem);
            }
            else if (iRibbonItem.ItemType == RibbonItemType.CheckButton && barItem is BarBaseButtonItem barButton)
            {   // BarButton v obou režimech:
                _RibbonCheckButtonItemClick(barButton, iRibbonItem);
            }
        }
        /// <summary>
        /// Vyřeší stavy BarCheckItem po kliknutí na něj. 
        /// Zpracuje režim CheckBox (samostaný CheckButton) i RadioButton (skupinový CheckButton).
        /// </summary>
        /// <param name="checkButton"></param>
        /// <param name="iRibbonItem"></param>
        private void _RibbonCheckBoxItemClick(BarCheckItem checkButton, IRibbonItem iRibbonItem)
        {
            if (String.IsNullOrEmpty(iRibbonItem.RadioButtonGroupName))
            {   // Není daná grupa RadioButtonGroupName? Jde o obyčejný CheckBox:
                _RibbonItemSetChecked(iRibbonItem, !(iRibbonItem.Checked ?? false), true, false, null, true, checkButton);
            }
            else
            {   // Máme řešit Radio grupu:
                _RibbonRadioButtonItemClick(iRibbonItem, true, false, true);
            }
        }
        /// <summary>
        /// Vyřeší stavy CheckButtonu po kliknutí na něj. 
        /// Zpracuje režim CheckBox (samostaný CheckButton) i RadioButton (skupinový CheckButton).
        /// </summary>
        /// <param name="barButton"></param>
        /// <param name="iRibbonItem"></param>
        private void _RibbonCheckButtonItemClick(BarBaseButtonItem barButton, IRibbonItem iRibbonItem)
        {
            if (String.IsNullOrEmpty(iRibbonItem.RadioButtonGroupName))
            {   // Není daná grupa RadioButtonGroupName? Jde o obyčejný CheckBox:
                _RibbonItemSetChecked(iRibbonItem, !(iRibbonItem.Checked ?? false), true, true, barButton, false, null);
            }
            else
            {   // Máme řešit Radio grupu:
                _RibbonRadioButtonItemClick(iRibbonItem, true, true, false);
            }
        }
        /// <summary>
        /// Metoda najde všechny prvky jedné Radiogrupy (se shodným <see cref="IRibbonItem.RadioButtonGroupName"/> jako má dodaný prvek <paramref name="iRibbonItem"/>),
        /// a pro všechny prvky této grupy jiné než dodaný <paramref name="iRibbonItem"/> nastaví jejich Checked = false,
        /// a poté pro dodaný prvek nastaví jeho Checked = true.
        /// <para/>
        /// Podle požadavku volá event změny pro každý dotčený prvek, a nastaví do vizuálního prvku stav down a Checked.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="callEvent"></param>
        /// <param name="setDownState"></param>
        /// <param name="setChecked"></param>
        private void _RibbonRadioButtonItemClick(IRibbonItem iRibbonItem, bool callEvent, bool setDownState, bool setChecked)
        {
            // Získáme všechny prvky té skupiny RadioButtonGroupName, na jejíhož člena se kliklo:
            var groupName = iRibbonItem.RadioButtonGroupName;
            var groupItems = this.__RibbonItems?
                .Where(i => (!String.IsNullOrEmpty(i.RadioButtonGroupName) && i.RadioButtonGroupName == groupName))
                .ToArray();

            if (groupItems is null || groupItems.Length == 0) return;

            // Nejprve nastavím všechny ostatní prvky (nikoli ten aktivní) na Checked = false:
            groupItems.Where(i => !Object.ReferenceEquals(i, iRibbonItem)).ForEachExec(i => _RibbonItemSetChecked(i, false, callEvent, setDownState, null, setChecked, null));

            // A až poté nastavím ten jeden aktivní na Checked = true:
            _RibbonItemSetChecked(iRibbonItem, true, callEvent, setDownState, null, setChecked, null);
        }
        /// <summary>
        /// Do daného datového prvku <paramref name="iRibbonItem"/> vloží danou hodnotu Checked <paramref name="isChecked"/>.
        /// Pokud je požadováno nastavení hodnoty <see cref="BarBaseButtonItem.Down"/> pro odpovídajcí vizuální prvek, provede to 
        /// (může být předán button v parametru <paramref name="barButton"/>, anebo bude vyhledán v <see cref="IRibbonItem.RibbonItem"/>).
        /// Pokud došlo ke změně hodnoty IsChecked v <paramref name="iRibbonItem"/> a pokud je požadováno v <paramref name="callEvent"/>, 
        /// vyvolá se událost <see cref="BarItemCheck"/> a související.
        /// </summary>
        /// <param name="iRibbonItem"></param>
        /// <param name="isChecked"></param>
        /// <param name="callEvent"></param>
        /// <param name="setDownState"></param>
        /// <param name="barButton"></param>
        /// <param name="setChecked"></param>
        /// <param name="checkButton"></param>
        private void _RibbonItemSetChecked(IRibbonItem iRibbonItem, bool isChecked, bool callEvent, bool setDownState, BarBaseButtonItem barButton, bool setChecked, BarCheckItem checkButton)
        {
            bool oldChecked = (iRibbonItem.Checked ?? false);
            bool isChanged = (isChecked != oldChecked);
            iRibbonItem.Checked = isChecked;
            if (setDownState)
            {
                if (barButton is null) barButton = iRibbonItem.RibbonItem as BarButtonItem;
                if (barButton != null)
                {
                    barButton.Down = isChecked;                      // Nativní eventu DownChanged nehlídáme, tak mi nevadí že proběhne.
                    DxComponent.FillBarItemImageChecked(iRibbonItem, barButton);
                }
            }
            if (setChecked)
            {
                if (checkButton is null) checkButton = iRibbonItem.RibbonItem as BarCheckItem;
                if (checkButton != null)
                {
                    checkButton.Checked = isChecked;                 // Nativní eventu CheckedChanged nehlídáme, tak mi nevadí že proběhne.
                    DxComponent.FillBarItemImageChecked(iRibbonItem, checkButton);
                }
            }
            if (callEvent && isChanged)
                _RunBarItemCheck(iRibbonItem);
        }
        /// <summary>
        /// Vyvolá metodu <see cref="OnBarItemCheck(TEventArgs{IRibbonItem})"/> a event <see cref="BarItemCheck"/>
        /// </summary>
        /// <param name="ribbonItem"></param>
        private void _RunBarItemCheck(IRibbonItem ribbonItem)
        {
            var args = new TEventArgs<IRibbonItem>(ribbonItem);
            OnBarItemCheck(args);
            BarItemCheck?.Invoke(this, args);
        }
        /// <summary>
        /// Proběhne po změně Checked na prvku Toolbaru
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnBarItemCheck(TEventArgs<IRibbonItem> args) { }
        /// <summary>
        /// Událost volaná po změně Checked na prvku Toolbaru
        /// </summary>
        public event EventHandler<TEventArgs<IRibbonItem>> BarItemCheck;
        #endregion
        #endregion
    }
    #endregion
}
