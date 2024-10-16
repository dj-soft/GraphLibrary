// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using DevExpress.Utils;
using DevExpress.Utils.Extensions;

using XS = Noris.WS.Parser.XmlSerializer;
using System.ComponentModel;
using DevExpress.XtraBars.Controls;
using DevExpress.Utils.Drawing;
using DevExpress.Utils.Text;
using DevExpress.XtraTab;
using DevExpress.XtraEditors.Controls;
using Noris.Clients.Win.Components.Obsoletes.DataForm;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region ControlFactory : generuje instance controlů
    /// <summary>
    /// Vytvoří a vrátí TabPage určitého typu
    /// </summary>
    public class ControlFactory
    {
        /// <summary>
        /// Vytvoří a vrátí TabPage požadovaného typu
        /// </summary>
        /// <param name="controlType"></param>
        /// <returns></returns>
        public static Control CreateControl(FactoryControlType controlType)
        {
            switch (controlType)
            {
                case FactoryControlType.DxTabPane:
                    return new DxTabPane();
                case FactoryControlType.DxXtraTabControl:
                    return new DxXtraTabControl();
                default:
                    //if (DxComponent.IsDebuggerActive)
                    //    return new DxXtraTabControl();
                    return new DxTabPane();
            }
        }
    }
    /// <summary>
    /// Typ controlu, který má vrátit factory <see cref="ControlFactory"/>
    /// </summary>
    public enum FactoryControlType
    {
        /// <summary>
        /// Nic
        /// </summary>
        None,
        /// <summary>
        /// Výchozí typ, obecně Control implementující <see cref="AsolDX.ITabHeaderControl"/>
        /// </summary>
        ITabHeaderControl,
        /// <summary>
        /// Explicitně určený <see cref="AsolDX.DxTabPane"/>
        /// </summary>
        DxTabPane,
        /// <summary>
        /// Explicitně určený <see cref="AsolDX.DxXtraTabControl"/>
        /// </summary>
        DxXtraTabControl
    }
    #endregion
    #region DxStdForm
    /// <summary>
    /// Základní formulář bez Ribbonu a StatusBaru
    /// </summary>
    public class DxStdForm : DevExpress.XtraEditors.XtraForm, IDxControlWithIcons, IFormWorking, IListenerZoomChange, IListenerStyleChanged, IListenerExcludeFromCaptureContentChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxStdForm()
        {
            this.ActivityStateInit();
            this.InitializeForm();
            this.ImageName = AsolDX.ImageName.DxFormIcon;
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        /// <summary>
        /// Inicializace samotného formuláře, probíhá úplně první, ještě není vytvořen žádný control
        /// </summary>
        protected virtual void InitializeForm()
        { }
        /// <summary>
        /// Při zobrazení okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            if (!_WasShown)
            {
                this.ActivityState = WindowActivityState.FirstShow;
                this.OnFirstShownBefore();
                this.FirstShownBefore?.Invoke(this, EventArgs.Empty);
                base.OnShown(e);
                _WasShown = true;
                this.OnFirstShownAfter();
                this.FirstShownAfter?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                base.OnShown(e);
                this.OnNextShown();
                this.NextShown?.Invoke(this, EventArgs.Empty);
            }
            this.ActivityState = WindowActivityState.Visible;
        }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        protected virtual void OnFirstShownBefore() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        public event EventHandler FirstShownBefore;
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        protected virtual void OnFirstShownAfter() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        public event EventHandler FirstShownAfter;
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        protected virtual void OnNextShown() { }
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        public event EventHandler NextShown;
        /// <summary>
        /// Obsahuje true poté, kdy formulář byl zobrazen. 
        /// Obsahuje true již v metodě <see cref="OnFirstShownAfter"/> a v eventu <see cref="FirstShownAfter"/>.
        /// </summary>
        public bool WasShown { get { return _WasShown; } }
        /// <summary>
        /// Okno již bylo zobrazeno?
        /// </summary>
        private bool _WasShown = false;
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno již prošlo fází zobrazení (<see cref="WasShown"/> je true) a dosud nebylo disposováno.
        /// </summary>
        public bool IsLive { get { return _WasShown && !Disposing && !IsDisposed; } }
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno je ve viditelném stavu = je živé <see cref="IsLive"/> a není minimalizované.
        /// V tomto stavu má smysl provádět Layout okna.
        /// </summary>
        public bool IsDisplayed { get { return IsLive && WindowState != FormWindowState.Minimized; } }
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this.DeviceDpi; } }
        /// <summary>
        /// Umístí toto okno do viditelných souřadnic monitorů.
        /// Pokud je parametr <paramref name="force"/> = false (default), 
        /// pak to provádí jen když pozice okna <see cref="Form.StartPosition"/> je <see cref="FormStartPosition.Manual"/>.
        /// Pokud parametr <paramref name="force"/> = true, provede to vždy.
        /// </summary>
        /// <param name="force"></param>
        public void MoveToVisibleScreen(bool force = false)
        {
            if (!force && this.StartPosition != FormStartPosition.Manual) return;
            this.Bounds = this.Bounds.FitIntoMonitors(true, false, true);
        }
        /// <summary>
        /// Najde ovládací prvek odpovídající aktuální klávese.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (DxStdForm.SearchKeyDownButtons(this, keyData))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
        /// <summary>
        /// Najde ovládací prvek odpovídající aktuální klávese. Hledá rekurzivně. Hledá pouze prvky, které jsou Enabled a Visible.
        /// Hledá prvek implementující <see cref="IHotKeyControl"/> s klávesou <see cref="IHotKeyControl.HotKey"/> odpovídající aktuální klávese <paramref name="keyData"/>.
        /// Pokud najde, provede jeho <see cref="IHotKeyControl.PerformClick"/> a vrátí true.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        internal static bool SearchKeyDownButtons(Control control, Keys keyData)
        {
            if (control == null || !control.Enabled || !control.Visible || control.Controls.Count == 0) return false;
            foreach (Control child in control.Controls)
            {
                if (child == null || !child.Enabled || !child.Visible) continue;
                if ((child is IHotKeyControl hotKeyControl) && hotKeyControl.HotKey == keyData)
                {
                    hotKeyControl.PerformClick();
                    return true;
                }
                if (SearchKeyDownButtons(child, keyData))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.ActivityState = WindowActivityState.Disposing;
            DxComponent.UnregisterListener(this);
            DestroyContent();
            base.Dispose(disposing);
            this.ActivityState = WindowActivityState.Disposed;
        }
        /// <summary>
        /// Zruší veškerý svůj obsah v procesu Dispose. Volá base.DestroyContent() !!!
        /// </summary>
        protected virtual void DestroyContent()
        {
            FirstShownBefore = null;
            FirstShownAfter = null;
            NextShown = null;
        }
        #endregion
        #region Ikona okna
        /// <summary>
        /// Název obrázku, který reprezentuje ikonu tohoto okna
        /// </summary>
        public string ImageName { get { return _ImageName; } set { _ImageName = value; DxComponent.ApplyImage(this.IconOptions, value, sizeType: ResourceImageSizeType.Large); } } private string _ImageName;
        /// <summary>
        /// Název obrázku, který reprezentuje přidanou ikonu tohoto okna
        /// </summary>
        public string ImageNameAdd { get { return _ImageNameAdd; } set { _ImageNameAdd = value; } } private string _ImageNameAdd;
        string IDxControlWithIcons.IconNameBasic { get { return ImageName; } }
        string IDxControlWithIcons.IconNameAdd { get { return ImageNameAdd; } }
        #endregion
        #region Skrývání obsahu formuláře pro externí aplikace nahrávající obsah (Capture content), listener IListenerNotCaptureWindowsChanged
        /// <summary>
        /// Pokud je true, pak obsah tohoto okna nebude zachycen aplikacemi jako Teams, Recording, PrintScreen atd.<br/>
        /// Výchozí je hodnota odpovídající (! <see cref="DxComponent.ExcludeFromCaptureContent"/>).
        /// <para/>
        /// Okno <see cref="DxRibbonForm"/> implementuje <see cref="IListenerExcludeFromCaptureContentChanged"/> a reaguje tak na hodnotu <see cref="DxComponent.ExcludeFromCaptureContent"/>,<br/>
        /// automaticky tedy nastavuje zdejší hodnotu <see cref="ExcludeFromCaptureContent"/> podle ! <see cref="DxComponent.ExcludeFromCaptureContent"/>.
        /// <para/>
        /// Využívá SetWindowDisplayAffinity : <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity"/>
        /// </summary>
        public bool ExcludeFromCaptureContent
        {
            get { return __ExcludeFromCaptureContent; }
            set
            {
                bool currentValue = __ExcludeFromCaptureContent;
                if (value != currentValue)
                {
                    __ExcludeFromCaptureContent = value;
                    AcceptExcludeFromCaptureContent();
                }
            }
        }
        private bool __ExcludeFromCaptureContent;
        /// <summary>
        /// Metoda zajistí, že toto okno bude mít nastavenu reálnou vlastnost <c>Winapi.WindowDisplayAffinity</c> podle zdejší hodnoty <see cref="__ExcludeFromCaptureContent"/>.
        /// </summary>
        protected void AcceptExcludeFromCaptureContent()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
                DxComponent.SetWindowDisplayAffinity(this, __ExcludeFromCaptureContent);
        }
        void IListenerExcludeFromCaptureContentChanged.ExcludeFromCaptureContentChanged() { OnExcludeFromCaptureContentChanged(); }
        /// <summary>
        /// Zavolá se tehdy, když aplikace změnila hodnotu v <see cref="DxComponent.ExcludeFromCaptureContent"/> = mění se stav <c>WinApi.SetWindowDisplayAffinity</c> (pomocí listeneru <see cref="IListenerExcludeFromCaptureContentChanged"/>).
        /// </summary>
        protected virtual void OnExcludeFromCaptureContentChanged()
        {
            this.ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
        }
        /// <summary>
        /// Po vytvoření Handle pro formulář si pro toto Window aktualizujeme <c>WindowDisplayAffinity</c>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            __ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
            AcceptExcludeFromCaptureContent();
        }
        #endregion
        #region Stav okna, změny stavu
        /// <summary>
        /// Stav aktivity okna. Při změně je volána událost <see cref="ActivityStateChanged"/>.
        /// </summary>
        public WindowActivityState ActivityState
        {
            get { return _ActivityState; }
            protected set
            {
                var oldValue = _ActivityState;
                var newValue = value;
                if (newValue != oldValue)
                {
                    _ActivityState = newValue;
                    RunActivityStateChanged(oldValue, newValue);
                }
            }
        }
        /// <summary>Stav okna</summary>
        private WindowActivityState _ActivityState;
        /// <summary>
        /// Inicializace stavu okna a odpovídajících handlerů
        /// </summary>
        private void ActivityStateInit()
        {
            this.ActivityState = WindowActivityState.Creating;

            this.Activated += _ActivityStateDetect_Activated;
            this.Deactivate += _ActivityStateDetect_Deactivate;
            this.GotFocus += _ActivityStateDetect_GotFocus;
            this.LostFocus += _ActivityStateDetect_LostFocus;
            this.VisibleChanged += _ActivityStateDetect_VisibleChanged;
            this.FormClosing += _ActivityStateDetect_FormClosing;
            this.FormClosed += _ActivityStateDetect_FormClosed;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Activated(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Deactivate(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_GotFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_LostFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_VisibleChanged(object sender, EventArgs e)
        {
            this.ActivityState = (this.Visible ? WindowActivityState.Visible : WindowActivityState.Invisible);
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closing;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closed;
        }
        /// <summary>
        /// Vyvolá <see cref="OnActivityStateChanged(TEventValueChangedArgs{WindowActivityState})"/> a event <see cref="ActivityStateChanged"/>.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void RunActivityStateChanged(WindowActivityState oldValue, WindowActivityState newValue)
        {
            var args = new TEventValueChangedArgs<WindowActivityState>(EventSource.None, oldValue, newValue);
            OnActivityStateChanged(args);
            ActivityStateChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda proběhne při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args) { }
        /// <summary>
        /// Událost volaná při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        public event EventHandler<TEventValueChangedArgs<WindowActivityState>> ActivityStateChanged;
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        #endregion
    }
    #endregion
    #region DxRibbonForm
    /// <summary>
    /// Formulář s ribbonem.
    /// Obsahuje připravený Ribbon <see cref="DxRibbonBaseForm.DxRibbon"/> a připravený StatusBar <see cref="DxRibbonBaseForm.DxStatusBar"/>, 
    /// a hlavní Panel <see cref="DxMainPanel"/> nacházející se mezi Ribbonem a StatusBarem.
    /// </summary>
    public class DxRibbonForm : DxRibbonBaseForm
    {
        #region MainPanel
        /// <summary>
        /// Hlavní panel, mezi Ribbonem a StatusBarem
        /// </summary>
        public DxPanelControl DxMainPanel { get { return _DxMainPanel; } }
        /// <summary>
        /// Provede tvorbu hlavního panelu okna <see cref="DxMainPanel"/> a jeho přidání do okna včetně zadokování.
        /// Provádí se před vytvořením Ribbonu a Status baru, aby <see cref="DxMainPanel"/> byl správně umístěn na Z ose.
        /// </summary>
        protected override void DxMainContentCreate()
        {
            this._DxMainPanel = DxComponent.CreateDxPanel(this, System.Windows.Forms.DockStyle.Fill, borderStyles: DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
            this._DxMainPanel.ClientSizeChanged += _DxMainPanel_ClientSizeChanged;
        }
        /// <summary>
        /// Provede se po změně velikosti ClientSize panelu <see cref="DxRibbonForm.DxMainPanel"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DxMainPanel_ClientSizeChanged(object sender, EventArgs e)
        {
            DxMainContentDoLayout();
        }
        /// <summary>
        /// Provede přípravu obsahu hlavního panelu <see cref="DxRibbonForm.DxMainPanel"/>. Panel je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected override void DxMainContentPrepare()
        {
            this._DxMainPanel.Visible = true;
        }
        private DxPanelControl _DxMainPanel;
        #endregion
        #region FormRibbonVisibility
        /// <summary>
        /// Viditelnost Ribbonu a titulkového řádku okna
        /// </summary>
        public FormRibbonVisibilityMode FormRibbonVisibility
        {
            get { return _FormRibbonVisibility; }
            set
            {
                switch (value)
                {
                    case FormRibbonVisibilityMode.Nothing:
                        this.RibbonVisibility = DevExpress.XtraBars.Ribbon.RibbonVisibility.Hidden;
                        this.DxRibbon.Visible = false;
                        break;
                    case FormRibbonVisibilityMode.FormTitleRow:
                        this.RibbonVisibility = DevExpress.XtraBars.Ribbon.RibbonVisibility.Hidden;
                        this.DxRibbon.Visible = true;
                        break;
                    case FormRibbonVisibilityMode.Standard:
                        this.RibbonVisibility = DevExpress.XtraBars.Ribbon.RibbonVisibility.Visible;
                        this.DxRibbon.Visible = true;
                        break;
                }
                _FormRibbonVisibility = value;
            }
        }
        /// <summary>Viditelnost Ribbonu a titulkového řádku okna</summary>
        private FormRibbonVisibilityMode _FormRibbonVisibility = FormRibbonVisibilityMode.Standard;
        #endregion
    }
    /// <summary>
    /// Formulář s ribbonem.
    /// Obsahuje připravený Ribbon <see cref="DxRibbon"/> a připravený StatusBar <see cref="DxStatusBar"/>.
    /// </summary>
    public abstract class DxRibbonBaseForm : DevExpress.XtraBars.Ribbon.RibbonForm, IDxControlWithIcons, IFormWorking, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle, IListenerExcludeFromCaptureContentChanged
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRibbonBaseForm()
        {
            this._FormStatusInit();
            this._ActivityStateInit();
            this._FormPositionInit();
            this.ImageName = AsolDX.ImageName.DxFormIcon;
            this.InitDxRibbonForm();
            DxComponent.RegisterListener(this);
            this.DxMainContentDoLayout();
            this.EndInitDxRibbonForm();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        /// <summary>
        /// Při zobrazení okna
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            if (!_WasShown)
            {
                this.ActivityState = WindowActivityState.FirstShow;
                this._FormPositionApply(true);
                this.OnFirstShownBefore();
                this.FirstShownBefore?.Invoke(this, EventArgs.Empty);
                this.ActivityState = WindowActivityState.ShowBefore;
                base.OnShown(e);
                _WasShown = true;
                this.OnFirstShownAfter();
                this.FirstShownAfter?.Invoke(this, EventArgs.Empty);
                this.ActivityState = WindowActivityState.ShowAfter;
            }
            else
            {
                base.OnShown(e);
                this.OnNextShown();
                this.NextShown?.Invoke(this, EventArgs.Empty);
                this.ActivityState = WindowActivityState.ShowAfter;
            }
            this.ActivityState = WindowActivityState.Visible;
        }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        protected virtual void OnFirstShownBefore() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně před prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = false.
        /// </summary>
        public event EventHandler FirstShownBefore;
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        protected virtual void OnFirstShownAfter() { }
        /// <summary>
        /// Je vyvoláno jedenkrát v životě okna, těsně po prvním zobrazením okna. Nyní je hodnota <see cref="WasShown"/> = true.
        /// </summary>
        public event EventHandler FirstShownAfter;
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        protected virtual void OnNextShown() { }
        /// <summary>
        /// Je vyvoláno těsně po druhém a každém dalším zobrazením okna. Nikoli po prvním.
        /// </summary>
        public event EventHandler NextShown;
        /// <summary>
        /// Obsahuje true poté, kdy formulář byl zobrazen. 
        /// Obsahuje true již v metodě <see cref="OnFirstShownAfter"/> a v eventu <see cref="FirstShownAfter"/>.
        /// </summary>
        public bool WasShown { get { return _WasShown; } }
        /// <summary>
        /// Okno již bylo zobrazeno?
        /// </summary>
        private bool _WasShown = false;
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno již prošlo fází zobrazení (<see cref="WasShown"/> je true) a dosud nebylo disposováno.
        /// </summary>
        public bool IsLive { get { return _WasShown && !Disposing && !IsDisposed; } }
        /// <summary>
        /// Obsahuje true v situaci, kdy toto okno je ve viditelném stavu = je živé <see cref="IsLive"/> a není minimalizované.
        /// V tomto stavu má smysl provádět Layout okna.
        /// </summary>
        public bool IsDisplayed { get { return IsLive && WindowState != FormWindowState.Minimized; } }
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this.DeviceDpi; } }
        /// <summary>
        /// Najde ovládací prvek odpovídající aktuální klávese.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (DxStdForm.SearchKeyDownButtons(this, keyData))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
        /// <summary>
        /// Dispose panelu
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.ActivityState = WindowActivityState.Disposing;
            DxComponent.UnregisterListener(this);
            DestroyContent();
            base.Dispose(disposing);
            this.ActivityState = WindowActivityState.Disposed;
        }
        /// <summary>
        /// Zruší veškerý svůj obsah v procesu Dispose. Volá base.DestroyContent() !!!
        /// </summary>
        protected virtual void DestroyContent()
        {
            FirstShownBefore = null;
            FirstShownAfter = null;
            NextShown = null;
        }
        #endregion
        #region FormStatus
        private void _FormStatusInit()
        {
            __FormStatus = new FormStatusInfo(this);
        }
        private FormStatusInfo __FormStatus;
        public WindowActivityState ActivityState
        {
            get { return __FormStatus.ActivityState; }
            private set { __FormStatus.ActivityState = value; }
        }
        #endregion
        #region Ikona okna
        /// <summary>
        /// Název obrázku, který reprezentuje ikonu tohoto okna
        /// </summary>
        public string ImageName { get { return _ImageName; } set { _ImageName = value; DxComponent.ApplyImage(this.IconOptions, value, sizeType: ResourceImageSizeType.Large); } } private string _ImageName;
        /// <summary>
        /// Název obrázku, který reprezentuje přidanou ikonu tohoto okna
        /// </summary>
        public string ImageNameAdd { get { return _ImageNameAdd; } set { _ImageNameAdd = value; } } private string _ImageNameAdd;
        string IDxControlWithIcons.IconNameBasic { get { return ImageName; } }
        string IDxControlWithIcons.IconNameAdd { get { return ImageNameAdd; } }
        #endregion
        #region Skrývání obsahu formuláře pro externí aplikace nahrávající obsah (Capture content), listener IListenerNotCaptureWindowsChanged
        /// <summary>
        /// Pokud je true, pak obsah tohoto okna nebude zachycen aplikacemi jako Teams, Recording, PrintScreen atd.<br/>
        /// Výchozí je hodnota odpovídající (! <see cref="DxComponent.ExcludeFromCaptureContent"/>).
        /// <para/>
        /// Okno <see cref="DxRibbonForm"/> implementuje <see cref="IListenerExcludeFromCaptureContentChanged"/> a reaguje tak na hodnotu <see cref="DxComponent.ExcludeFromCaptureContent"/>,<br/>
        /// automaticky tedy nastavuje zdejší hodnotu <see cref="ExcludeFromCaptureContent"/> podle ! <see cref="DxComponent.ExcludeFromCaptureContent"/>.
        /// <para/>
        /// Využívá SetWindowDisplayAffinity : <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity"/>
        /// </summary>
        public bool ExcludeFromCaptureContent
        {
            get { return __ExcludeFromCaptureContent; }
            set
            {
                bool currentValue = __ExcludeFromCaptureContent;
                if (value != currentValue)
                {
                    __ExcludeFromCaptureContent = value;
                    AcceptExcludeFromCaptureContent();
                }
            }
        }
        private bool __ExcludeFromCaptureContent;
        /// <summary>
        /// Metoda zajistí, že toto okno bude mít nastavenu reálnou vlastnost <c>Winapi.WindowDisplayAffinity</c> podle zdejší hodnoty <see cref="__ExcludeFromCaptureContent"/>.
        /// </summary>
        protected void AcceptExcludeFromCaptureContent()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                DxComponent.SetWindowDisplayAffinity(this, __ExcludeFromCaptureContent);
            }
        }
        void IListenerExcludeFromCaptureContentChanged.ExcludeFromCaptureContentChanged() { OnExcludeFromCaptureContentChanged(); }
        /// <summary>
        /// Zavolá se tehdy, když aplikace změnila hodnotu v <see cref="DxComponent.ExcludeFromCaptureContent"/> = mění se stav <c>WinApi.SetWindowDisplayAffinity</c> (pomocí listeneru <see cref="IListenerExcludeFromCaptureContentChanged"/>).
        /// </summary>
        protected virtual void OnExcludeFromCaptureContentChanged()
        {
            this.ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
        }
        /// <summary>
        /// Po vytvoření Handle pro formulář si pro toto Window aktualizujeme <c>WindowDisplayAffinity</c>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            __ExcludeFromCaptureContent = DxComponent.ExcludeFromCaptureContent;
            AcceptExcludeFromCaptureContent();
        }
        #endregion
        #region Pozice okna
        /// <summary>
        /// Umístí toto okno do viditelných souřadnic monitorů.
        /// Pokud je parametr <paramref name="force"/> = false (default), 
        /// pak to provádí jen když pozice okna <see cref="Form.StartPosition"/> je <see cref="FormStartPosition.Manual"/>.
        /// Pokud parametr <paramref name="force"/> = true, provede to vždy.
        /// </summary>
        /// <param name="force"></param>
        public void MoveToVisibleScreen(bool force = false)
        {
            if (!force && this.StartPosition != FormStartPosition.Manual) return;
            this.Bounds = this.Bounds.FitIntoMonitors(true, false, true);
        }
        /// <summary>
        /// Inicializace hlídání pozice okna
        /// </summary>
        private void _FormPositionInit()
        {
            this._FormPositionRestore();
            this.SizeChanged += _FormPositionChanged;
            this.LocationChanged += _FormPositionChanged;
            this.FormClosed += _FormPosition_FormClosed;
            this._FormPositionApply(false);

            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }
        /// <summary>
        /// Notifies the control of Windows messages.
        /// </summary>
        /// <param name="m">A System.Windows.Forms.Message that represents the Windows message.</param>
        protected override void OnNotifyMessage(Message m)
        {
            if (NeedLogMessage(m))
            {   // Včetně logování WinMessages:
                string suffix = (m.Msg == DxWin32.WM.WINDOWPOSCHANGING || m.Msg == DxWin32.WM.WINDOWPOSCHANGED) ? "; Bounds: " + Convertor.RectangleToString(Bounds) : "";
                DxComponent.LogAddMessage(m, this, "Start.", suffix);
                base.OnNotifyMessage(m);
                DxComponent.LogAddMessage(m, this, "  End.", suffix);
            }
            else
            {   // Bez logování WinMessages:
                base.OnNotifyMessage(m);
            }
        }
        /// <summary>
        /// Vrátí true pokud daná message se má logovat
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private bool NeedLogMessage(Message m)
        {
            // Pokud log obecně není aktivní, neloguji žádnou zprávu:
            if (!DxComponent.LogActive) return false;

            // Pokud log pro toto okno není aktivní, neloguji žádnou zprávu:
            if (!this.LogActive) return false;

            // Pro zprávy zde vyjmenované vrátím false, takže se NEBUDOU logovat:
            return (!(m.Msg == DxWin32.WM.STYLECHANGED || m.Msg == DxWin32.WM.STYLECHANGING || m.Msg == DxWin32.WM.NCCALCSIZE || m.Msg == DxWin32.WM.CAPTURECHANGED));
        }
        /// <summary>
        /// Obsahuje true, pokud log je aktivní. Default = ne.
        /// </summary>
        public bool LogActive { get; set; }

        /// <summary>
        /// Při změně umístění
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLocationChanged(EventArgs e)
        {
            AddLogPosition("OnLocationChanged.Before: ");
            PrepareNormalBounds();
            base.OnLocationChanged(e);
            AddLogPosition("OnLocationChanged.After: ");
        }
        /// <summary>
        /// Při změně velikosti
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSizeChanged(EventArgs e)
        {
            AddLogPosition("OnSizeChanged.Before: ");
            PrepareNormalBounds();
            base.OnSizeChanged(e);
            AddLogPosition("OnSizeChanged.After: ");
        }
        private void PrepareNormalBounds()
        {
            if (_FormBoundsNormal.HasValue && this.WindowState == FormWindowState.Normal)
            {
                Rectangle boundsNormal = _FormBoundsNormal.Value;
                if (this.LogActive) DxComponent.LogAddLine(LogActivityKind.Ribbon, $"PrepareNormalBounds: BoundsNormal={Convertor.RectangleToString(boundsNormal)}");
                _FormBoundsNormal = null;
                this.Bounds = boundsNormal;
            }
        }
        /// <summary>
        /// Loguje pozici a stav okna
        /// </summary>
        /// <param name="prefix"></param>
        private void AddLogPosition(string prefix)
        {
            if (!this.LogActive) return;
            string line = $"{prefix}WindowState={WindowState}; Bounds={Convertor.RectangleToString(this.Bounds)}; RestoreBounds={Convertor.RectangleToString(this.RestoreBounds)}";
            DxComponent.LogAddLine(LogActivityKind.Ribbon, line);
        }
        /// <summary>
        /// Připraví si pozici okna - načte ji z aplikace pomocí metody <see cref="OnFormPositionLoad()"/> a správně si výsledek zapamatuje.
        /// </summary>
        private void _FormPositionRestore()
        {
            string position = OnFormPositionLoad();
            _FormPositionRegister = position;
            FormPositionInfo.TryParse(position, out _FormPositionInfo);
        }
        /// <summary>
        /// Aplikuje souřadnice do okna.
        /// Je nutno setovat jednak v konstruktoru, a druhak v OnFirstShownBefore(), kvůli UHD grafice a přepočtu rozměrů !!!
        /// </summary>
        /// <param name="isShown"></param>
        private void _FormPositionApply(bool isShown)
        {
            AddLogPosition("FormPositionApply.Before: ");
            var formPosition = _FormPositionInfo;
            if (formPosition is null)
            {
                // this.WindowState = FormWindowState.Maximized;
                return;
            }
            if (formPosition.WindowState == FormWindowState.Maximized)
            {
                var normalBounds = formPosition.NormalBounds.FitIntoMonitors(true, false, true);
                if (!isShown)
                {
                    //  if (this.WindowState != FormWindowState.Normal) this.WindowState = FormWindowState.Normal;
                    //  if (this.Bounds != normalBounds) this.Bounds = normalBounds;
                }
                var maximizedBounds = formPosition.MaximizedBounds.FitIntoMonitors(true, false, true);
                if (this.WindowState != FormWindowState.Maximized) this.WindowState = FormWindowState.Maximized;
                // if (this.Bounds != maximizedBounds) this.Bounds = maximizedBounds;

                this._FormBoundsNormal = normalBounds;

            }
            else
            {
                var normalBounds = formPosition.NormalBounds.FitIntoMonitors(true, false, true);
                if (this.WindowState != FormWindowState.Normal) this.WindowState = FormWindowState.Normal;
                if (this.StartPosition != FormStartPosition.Manual) this.StartPosition = FormStartPosition.Manual;
                if (this.Bounds != normalBounds) this.Bounds = normalBounds;
                this._FormBoundsNormal = null;
            }
            AddLogPosition("FormPositionApply.After: ");
        }
        /// <summary>
        /// Hlídá změnu pozice okna - průběžně
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FormPositionChanged(object sender, EventArgs e)
        {
            this.FormPositionOnChange(false);
        }
        /// <summary>
        /// Hlídá změnu pozice okna - finálně
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FormPosition_FormClosed(object sender, FormClosedEventArgs e)
        {
            string text = DxComponent.LogText;

            this.FormPositionOnChange(true);
        }
        /// <summary>
        /// Pokud již this okno bylo zobrazeno = předáno uživateli, pak zdejší metoda hlásí změny rozměru do metody <see cref="OnFormPositionSave(string, bool)"/>.
        /// </summary>
        /// <param name="isFinal"></param>
        private void FormPositionOnChange(bool isFinal)
        {
            if (this.WasShown)
            {
                string position = FormPositionInfo.CreatePosition(this);
                OnFormPositionSave(position, isFinal);
            }
        }
        /// <summary>
        /// Zde formulář umožňuje potomkovi, aby odněkud načetl pozici a stav okna pro obnovení uložené pozice při otevírání okna, a zde ji vrátil.
        /// Pro uložení pozice okna po interaktivní změně je určena metoda <see cref="DxRibbonBaseForm._FormPositionRestore()"/>.
        /// <para/>
        /// Tato virtual metoda je volaná z konstruktoru - proto nemá smysl, aby existoval párový eventhandler - není kdy ho zaregistrovat (a po provedení konstruktoru je pozdě).
        /// </summary>
        /// <returns></returns>
        protected virtual string OnFormPositionLoad() { return null; }
        /// <summary>
        /// Zde formulář oznamuje potomkovi, že změnil svoji velikost a pozici, a potomek si ji může uložit do své konfigurace. Současně se potomkovi sděluje parametrem <paramref name="isFinal"/>,
        /// zda určená pozice je průběžná (při jakékoli změně za života formuláře), anebo již finální (při ukončování formuláře).
        /// </summary>
        /// <param name="position">Pozice okna, tak jak ji následně očekává metoda <see cref="DxRibbonBaseForm.FormPositionOnChange(bool)"/></param>
        /// <param name="isFinal">Pozice je finální? false = při každé změně / true = při zavírání formuláře</param>
        protected virtual void OnFormPositionSave(string position, bool isFinal) { }
        /// <summary>
        /// Registr pozice okna, která je známa aplikaci (potomkovi).
        /// </summary>
        private string _FormPositionRegister;
        private FormPositionInfo _FormPositionInfo;
        private Rectangle? _FormBoundsNormal;
        #endregion
        #region Stav okna, změny stavu
        /// <summary>
        /// Stav aktivity okna. Při změně je volána událost <see cref="ActivityStateChanged"/>.
        /// </summary>
        public WindowActivityState ActivityState
        {
            get { return _ActivityState; }
            protected set
            {
                var oldValue = _ActivityState;
                var newValue = value;
                if (newValue != oldValue)
                {
                    _ActivityState = newValue;
                    RunActivityStateChanged(oldValue, newValue);
                }
            }
        }
        /// <summary>Stav okna</summary>
        private WindowActivityState _ActivityState;
        /// <summary>
        /// Inicializace stavu okna a odpovídajících handlerů
        /// </summary>
        private void _ActivityStateInit()
        {
            this.ActivityState = WindowActivityState.Creating;

            this.Activated += _ActivityStateDetect_Activated;
            this.Deactivate += _ActivityStateDetect_Deactivate;
            this.GotFocus += _ActivityStateDetect_GotFocus;
            this.LostFocus += _ActivityStateDetect_LostFocus;
            this.VisibleChanged += _ActivityStateDetect_VisibleChanged;
            this.FormClosing += _ActivityStateDetect_FormClosing;
            this.FormClosed += _ActivityStateDetect_FormClosed;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Activated(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Deactivate(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_GotFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_LostFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_VisibleChanged(object sender, EventArgs e)
        {
            if (this.WasShown)
                this.ActivityState = (this.Visible ? WindowActivityState.Visible : WindowActivityState.Invisible);
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closing;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closed;
        }
        /// <summary>
        /// Vyvolá <see cref="OnActivityStateChanged(TEventValueChangedArgs{WindowActivityState})"/> a event <see cref="ActivityStateChanged"/>.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void RunActivityStateChanged(WindowActivityState oldValue, WindowActivityState newValue)
        {
            var args = new TEventValueChangedArgs<WindowActivityState>(EventSource.None, oldValue, newValue);
            OnActivityStateChanged(args);
            ActivityStateChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Metoda proběhne při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args) { }
        /// <summary>
        /// Událost volaná při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        public event EventHandler<TEventValueChangedArgs<WindowActivityState>> ActivityStateChanged;
        #endregion
        #region Listener: Style, Zoom, NotCaptureWindows Changed; ApplicationIdle
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        #endregion
        #region Ribbon a StatusBar
        /// <summary>
        /// Ribbon
        /// </summary>
        public DxRibbonControl DxRibbon { get { return _DxRibbon; } }
        /// <summary>
        /// Status bar
        /// </summary>
        public DxRibbonStatusBar DxStatusBar { get { return _DxStatusBar; } }
        /// <summary>
        /// Inicializace Ribbonu a StatusBaru. Volá se v konstruktoru třídy <see cref="DxRibbonForm"/>!
        /// </summary>
        protected virtual void InitDxRibbonForm()
        {
            this.DxMainContentCreate();

            this._DxRibbon = new DxRibbonControl() { Visible = true };
            ((System.ComponentModel.ISupportInitialize)(_DxRibbon)).BeginInit();
            this.Ribbon = _DxRibbon;
            this.Controls.Add(this._DxRibbon);

            this._DxStatusBar = new DxRibbonStatusBar() { Visible = true };
            this._DxStatusBar.Ribbon = this._DxRibbon;
            this.StatusBar = _DxStatusBar;
            this.Controls.Add(this._DxStatusBar);

            this.DxRibbonPrepare();
            this.DxStatusPrepare();
            this.DxMainContentPrepare();
        }
        /// <summary>
        /// Je voláno na konci konstruktoru třídy <see cref="DxRibbonBaseForm"/>.
        /// Typicky je zde ukončen cyklus BeginInit jednoltivých komponent.
        /// <para/>
        /// Je povinné volat base metodu, typicky na konci metody override.
        /// </summary>
        protected virtual void EndInitDxRibbonForm()
        {
            ((System.ComponentModel.ISupportInitialize)(_DxRibbon)).EndInit();

            // Form Layout:
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        /// <summary>
        /// Provede tvorbu hlavního obsahu okna, podle jeho typu, a jeho přidání do okna včetně zadokování.
        /// Provádí se před vytvořením Ribbonu a Status baru, aby obsah byl správně umístěn na Z ose.
        /// </summary>
        protected abstract void DxMainContentCreate();
        /// <summary>
        /// Provede přípravu obsahu Ribbonu.
        /// Pozor: Bázová třída <see cref="DxRibbonForm"/> pouze nastaví <see cref="DxRibbonBaseForm.DxRibbon"/>.Visible = false; nic jiného neprovádí !!!
        /// To proto, když by potomek nijak s Ribbonem nepracoval, pak nebude Ribbon zobrazen.
        /// U této metody tedy není vhodné volat base metodu, anebo je třeba po jejím volání nastavit viditelnost Ribbonu na true.
        /// </summary>
        protected virtual void DxRibbonPrepare() { this._DxRibbon.Visible = false; }
        /// <summary>
        /// Provede přípravu obsahu StatusBaru.
        /// Pozor: Bázová třída <see cref="DxRibbonForm"/> nastaví <see cref="DxRibbonBaseForm.DxStatusBar"/>.Visible = false; nic jiného neprovádí !!!
        /// To proto, když by potomek nijak se StatusBarem nepracoval, pak nebude StatusBar zobrazen.
        /// U této metody tedy není vhodné volat base metodu, anebo je třeba po jejím volání nastavit viditelnost StatusBaru na true.
        /// </summary>
        protected virtual void DxStatusPrepare() { this._DxStatusBar.Visible = false; }
        /// <summary>
        /// Provede přípravu obsahu hlavního obsahu okna. Obsah je již vytvořen a umístěn v okně, Ribbon i StatusBar existují.<br/>
        /// Zde se typicky vytváří obsah do hlavního panelu.
        /// </summary>
        protected abstract void DxMainContentPrepare();
        /// <summary>
        /// Provede se po změně velikosti ClientSize panelu <see cref="DxRibbonForm.DxMainPanel"/>
        /// </summary>
        protected virtual void DxMainContentDoLayout() { }
        /// <summary>
        /// Vytvoří a vrátí standardní stránku Home pro Ribbon, volitelně do ní přidá grupu Design s danými prvky (defaultně = None).
        /// </summary>
        /// <param name="designGroupParts"></param>
        /// <returns></returns>
        protected virtual DataRibbonPage CreateRibbonHomePage(FormRibbonDesignGroupPart designGroupParts = FormRibbonDesignGroupPart.None)
        {
            DataRibbonPage homePage = new DataRibbonPage() 
            {
                PageId = "DxHomePage", 
                PageText = "Domů",
                MergeOrder = 1, 
                PageOrder = 1
            };

            if (designGroupParts != FormRibbonDesignGroupPart.None)
            {
                var group = DxRibbonControl.CreateDesignHomeGroup(designGroupParts, "Design") as DataRibbonGroup;
                homePage.Groups.Add(group);
            }
            return homePage;
        }
        private DxRibbonControl _DxRibbon;
        private DxRibbonStatusBar _DxStatusBar;
        #endregion
    }
    /// <summary>
    /// Prvky, které mohou být přidány do grupy Design v ribbon page HomePage
    /// </summary>
    [Flags]
    public enum FormRibbonDesignGroupPart
    {
        /// <summary>
        /// Žádný prvek, ani grupa pro Design prvky
        /// </summary>
        None = 0,
        /// <summary>
        /// Button pro volbu Skinu
        /// </summary>
        SkinButton = 0x0001,
        /// <summary>
        /// Button pro volbu Barevné palety
        /// </summary>
        PaletteButton = 0x0002,
        /// <summary>
        /// Galerie pro volbu Barevné palety
        /// </summary>
        PaletteGallery = 0x0004,
        /// <summary>
        /// Button pro přepínání režimu UHD Paint
        /// </summary>
        UhdSupport = 0x0010,
        /// <summary>
        /// Okno pro výběr ikon DevExpress
        /// </summary>
        ImageGallery = 0x0020,
        /// <summary>
        /// Aktivita systémového logu
        /// </summary>
        LogActivity = 0x0040,
        /// <summary>
        /// Zakázat zachycování oken (Capture: Printscreen / Recording / Teams)
        /// </summary>
        NotCaptureWindows = 0x0080,

        /// <summary>
        /// Základ = <see cref="SkinButton"/> + <see cref="PaletteButton"/> + <see cref="ImageGallery"/>
        /// </summary>
        Basic = SkinButton | PaletteButton | ImageGallery,
        /// <summary>
        /// Běžná sada = <see cref="SkinButton"/> + <see cref="PaletteButton"/> + <see cref="UhdSupport"/> + <see cref="ImageGallery"/>
        /// </summary>
        Default = SkinButton | PaletteButton | UhdSupport | ImageGallery,
        /// <summary>
        /// Všechno = <see cref="SkinButton"/> + <see cref="PaletteButton"/> + <see cref="UhdSupport"/> + <see cref="ImageGallery"/> + <see cref="LogActivity"/>
        /// </summary>
        All = SkinButton | PaletteButton | UhdSupport | ImageGallery | LogActivity | NotCaptureWindows,
    }
    /// <summary>
    /// Režim viditelnosti titulkového řádku okna a Ribbonu
    /// </summary>
    public enum FormRibbonVisibilityMode
    {
        /// <summary>
        /// Není zobrazen ani Ribbon, ani TitleRow okna!!!
        /// </summary>
        Nothing,
        /// <summary>
        /// Je zobrazen TitleRow okna, ale není Ribbon, a to ani Toolbar ani Search ani PageHeaders. Jako by Ribbon nebyl.
        /// </summary>
        FormTitleRow,
        /// <summary>
        /// Standardní zobrazení TitleRow okna a Ribbonu
        /// </summary>
        Standard
    }
    /// <summary>
    /// Třída určená pro uchování pozice formuláře, pro jeho persistenci
    /// </summary>
    internal class FormPositionInfo
    {
        /// <summary>
        /// Vrátí řetězec popisující pozici daného formuláře
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public static string CreatePosition(Form form)
        {
            if (form is null) return null;
            FormWindowState windowState = form.WindowState;
            Rectangle normalBounds = (windowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds);
            Rectangle maximizedBounds = form.Bounds;
            return CreatePosition(windowState, normalBounds, maximizedBounds);
        }
        /// <summary>
        /// Vrátí řetězec popisující pozici daného formuláře
        /// </summary>
        /// <param name="windowState"></param>
        /// <param name="normalBounds"></param>
        /// <param name="maximizedBounds"></param>
        /// <returns></returns>
        private static string CreatePosition(FormWindowState windowState, Rectangle normalBounds, Rectangle maximizedBounds)
        {
            string state = (windowState == FormWindowState.Maximized ? "X" : "N");
            return $"{state};{Convertor.RectangleToString(normalBounds, ',')};{Convertor.RectangleToString(maximizedBounds, ',')}";
        }
        /// <summary>
        /// Z daného řetězce zkusí parsovat původní stav formuláře
        /// </summary>
        /// <param name="position"></param>
        /// <param name="positionInfo"></param>
        /// <returns></returns>
        public static bool TryParse(string position, out FormPositionInfo positionInfo)
        {
            positionInfo = null;
            if (String.IsNullOrEmpty(position)) return false;
            var parts = position.Split(';');
            if (parts.Length < 2) return false;
            FormWindowState windowState = (parts[0] == "X" ? FormWindowState.Maximized : parts[0] == "N" ? FormWindowState.Normal : FormWindowState.Minimized);
            if (windowState == FormWindowState.Minimized) return false;
            Rectangle normalBounds = (Rectangle)Convertor.StringToRectangle(parts[1], ',');
            if (normalBounds.IsEmpty || normalBounds.Width <= 0 || normalBounds.Height <= 0) return false;
            Rectangle maximizedBounds = Rectangle.Empty;
            if (parts.Length >= 3)
                maximizedBounds = (Rectangle)Convertor.StringToRectangle(parts[2], ',');
            if (maximizedBounds.IsEmpty || maximizedBounds.Width <= 0 || maximizedBounds.Height <= 0)
                maximizedBounds = normalBounds;
            positionInfo = new FormPositionInfo(windowState, normalBounds, maximizedBounds);
            return true;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="windowState"></param>
        /// <param name="normalBounds"></param>
        /// <param name="maximizedBounds"></param>
        private FormPositionInfo(FormWindowState windowState, Rectangle normalBounds, Rectangle maximizedBounds)
        {
            this.WindowState = windowState;
            this.NormalBounds = normalBounds;
            this.MaximizedBounds = maximizedBounds;
        }
        /// <summary>
        /// Stav okna <see cref="FormWindowState.Maximized"/> nebo <see cref="FormWindowState.Normal"/>
        /// </summary>
        public FormWindowState WindowState { get; private set; }
        /// <summary>
        /// Souřadnice ve stavu <see cref="FormWindowState.Normal"/>
        /// </summary>
        public Rectangle NormalBounds { get; private set; }
        /// <summary>
        /// Souřadnice ve stavu <see cref="FormWindowState.Maximized"/>
        /// </summary>
        public Rectangle MaximizedBounds { get; private set; }
        /// <summary>
        /// Stringová pozice
        /// </summary>
        public string Position { get { return CreatePosition(WindowState, NormalBounds, MaximizedBounds); } }
    }
    /// <summary>
    /// Interface, který zaručuje přítomnost property s názvem základní a rozšířené ikony okna
    /// </summary>
    public interface IDxControlWithIcons
    {
        /// <summary>
        /// Jméno základní ikony
        /// </summary>
        string IconNameBasic { get; }
        /// <summary>
        /// Jméno přidané ikony (zobrazuje se v TabHeaderu)
        /// </summary>
        string IconNameAdd { get; }
    }
    /// <summary>
    /// Informace o životním stavu formuláře (proces otevírání, zavírání atd), a o jeho pozici, rozměrech a maximalizaci
    /// </summary>
    internal class FormStatusInfo
    {
        #region Konstruktor, Owner, Dispose...
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="owner"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FormStatusInfo(Form owner)
        {
            if (owner is null) throw new ArgumentNullException($"FormStatusInfo(Form owner): 'owner' can not be null.");

            this._Owner = owner;
            owner.Disposed += _Owner_Disposed;
            this._LinkStatusEvents(owner);
            this._LinkBoundsEvents(owner);
        }
        /// <summary>
        /// Formulář byl Disposován
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Owner_Disposed(object sender, EventArgs e)
        {
            var owner = _Owner;
            this._UnLinkStatusEvents(owner);
            this._UnLinkBoundsEvents(owner);
            _Owner = null;
        }
        /// <summary>
        /// Vlastník = formulář, ukládá se WeakTarget
        /// </summary>
        private Form _Owner
        {
            get { return ((__Owner != null && __Owner.IsAlive) ? __Owner.Target : null); }
            set { __Owner = (value != null ? new WeakTarget<Form>(value) : null); }
        }
        /// <summary>
        /// Vlastník = formulář, pokud implementuje <see cref="IFormWorking"/>. Jinak je null.
        /// </summary>
        private IFormWorking _IOwner { get { return _Owner as IFormWorking; } }
        /// <summary>
        /// WeakTarget na Vlastník = formulář
        /// </summary>
        private WeakTarget<Form> __Owner;
        /// <summary>
        /// Obsahuje hodnotu Visible z formuláře <see cref="_Owner"/>
        /// </summary>
        private bool _OwnerVisible { get { return (_Owner?.Visible ?? false); } }
        /// <summary>
        /// Obsahuje hodnotu WasShown z formuláře <see cref="_Owner"/> (as <see cref="IFormWorking"/>)
        /// </summary>
        private bool _OwnerWasShown { get { return (_IOwner?.WasShown ?? false); } }
        #endregion
        #region Stav okna v jeho životním cyklu: ActivityState
        /// <summary>
        /// Stav aktivity okna. Při změně je volána událost <see cref="ActivityStateChanged"/>.
        /// </summary>
        public WindowActivityState ActivityState
        {
            get { return __ActivityState; }
            set
            {   // Setování může provádět i Owner form:
                var oldValue = __ActivityState;
                var newValue = value;
                if (newValue != oldValue)
                {
                    __ActivityState = newValue;
                    _RunActivityStateChanged(oldValue, newValue);
                }
            }
        }
        /// <summary>Stav okna</summary>
        private WindowActivityState __ActivityState;
        /// <summary>
        /// Aktivuje svoje eventhandlery do daného fornuláře pro sledování <see cref="ActivityState"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _LinkStatusEvents(Form owner)
        {
            this.ActivityState = WindowActivityState.Creating;

            if (owner != null)
            {
                owner.Activated += _ActivityStateDetect_Activated;
                owner.Deactivate += _ActivityStateDetect_Deactivate;
                owner.GotFocus += _ActivityStateDetect_GotFocus;
                owner.LostFocus += _ActivityStateDetect_LostFocus;
                owner.VisibleChanged += _ActivityStateDetect_VisibleChanged;
                owner.FormClosing += _ActivityStateDetect_FormClosing;
                owner.FormClosed += _ActivityStateDetect_FormClosed;
            }
        }
        /// <summary>
        /// Deaktivuje svoje eventhandlery z daného fornuláře pro sledování <see cref="ActivityState"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _UnLinkStatusEvents(Form owner)
        {
            this.ActivityState = WindowActivityState.Creating;

            if (owner != null)
            {
                owner.Activated -= _ActivityStateDetect_Activated;
                owner.Deactivate -= _ActivityStateDetect_Deactivate;
                owner.GotFocus -= _ActivityStateDetect_GotFocus;
                owner.LostFocus -= _ActivityStateDetect_LostFocus;
                owner.VisibleChanged -= _ActivityStateDetect_VisibleChanged;
                owner.FormClosing -= _ActivityStateDetect_FormClosing;
                owner.FormClosed -= _ActivityStateDetect_FormClosed;
            }
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Activated(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_Deactivate(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_GotFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Active;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_LostFocus(object sender, EventArgs e)
        {
            this.ActivityState = WindowActivityState.Inactive;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_VisibleChanged(object sender, EventArgs e)
        {
            if (this._OwnerWasShown)
                this.ActivityState = (this._OwnerVisible ? WindowActivityState.Visible : WindowActivityState.Invisible);
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closing;
        }
        /// <summary>
        /// Hlídáme změny stavu <see cref="ActivityState"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ActivityStateDetect_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.ActivityState = WindowActivityState.Closed;
        }
        /// <summary>
        /// Vyvolá <see cref="OnActivityStateChanged(TEventValueChangedArgs{WindowActivityState})"/> a event <see cref="ActivityStateChanged"/>.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void _RunActivityStateChanged(WindowActivityState oldValue, WindowActivityState newValue)
        {
            var args = new TEventValueChangedArgs<WindowActivityState>(EventSource.None, oldValue, newValue);
            OnActivityStateChanged(args);
            ActivityStateChanged?.Invoke(_Owner, args);
        }
        /// <summary>
        /// Metoda proběhne při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivityStateChanged(TEventValueChangedArgs<WindowActivityState> args) { }
        /// <summary>
        /// Událost volaná při změně stavu <see cref="ActivityState"/>, těsně po nastavení nového stavu do <see cref="ActivityState"/>.
        /// </summary>
        public event EventHandler<TEventValueChangedArgs<WindowActivityState>> ActivityStateChanged;
        #endregion
        #region Pozice a velikost okna a jeho WindowState
        /// <summary>
        /// Aktivuje svoje eventhandlery do daného fornuláře pro sledování <see cref="Bounds"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _LinkBoundsEvents(Form owner)
        {
            this._FormPositionRestore(owner);
            if (owner != null)
            {
                owner.LocationChanged += _Position_LocationChanged;
                owner.SizeChanged += _Position_SizeChanged;
                owner.FormClosed += _Position_FormClosed;
            }
            this._FormPositionApply(false);
        }
        /// <summary>
        /// Aktivuje svoje eventhandlery do daného fornuláře pro sledování <see cref="Bounds"/>
        /// </summary>
        /// <param name="owner"></param>
        private void _UnLinkBoundsEvents(Form owner)
        {
            this._FormPositionRestore(owner);
            if (owner != null)
            {
                owner.LocationChanged += _Position_LocationChanged;
                owner.SizeChanged += _Position_SizeChanged;
                owner.FormClosed += _Position_FormClosed;
            }
            this._FormPositionApply(false);
        }
        private void _Position_FormClosed(object sender, FormClosedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _Position_SizeChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _Position_LocationChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
    /// <summary>
    /// Interface popisující rozšířené pracovní vlastnosti Formulářů
    /// </summary>
    internal interface IFormWorking
    {
        /// <summary>
        /// Obsahuje true poté, kdy formulář byl zobrazen. 
        /// Obsahuje true již v metodě <c>OnFirstShownAfter</c> a v eventu <c>FirstShownAfter</c>.
        /// </summary>
        bool WasShown { get; }
    }
    #endregion
    #region DxPanelControl + IDxPanelPaintedItem
    /// <summary>
    /// PanelControl
    /// </summary>
    public class DxPanelControl : DevExpress.XtraEditors.PanelControl, IListenerZoomChange, IListenerStyleChanged, IListenerApplicationIdle
    {
        #region Konstruktor a základní vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPanelControl()
        {
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);
            this.LogActive = false;
            this._CurrentDpi = DxComponent.DesignDpi;
            this._LastDpi = DxComponent.DesignDpi;           // ??? anebo   0 ?
            this._PaintedItems = new List<IDxPanelPaintedItem>();
            this.AllowTransparency = true;
            this.LogActive = DxComponent.LogActive;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose panelu a všech Child prvků.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.__IsDispose = true;
            DxComponent.UnregisterListener(this);
            DestroyContent();
            this.DisposeContent();
            base.Dispose(disposing);
        }
        /// <summary>
        /// Zruší veškerý svůj obsah v procesu Dispose.
        /// </summary>
        protected virtual void DestroyContent()
        {
            _PaintedItems?.Clear();
            _PaintedItems = null;
        }
        /// <summary>
        /// Barva pozadí uživatelská, má přednost před skinem, aplikuje se na hotový skin, může obsahovat Alpha kanál = pak skrz tuto barvu prosvítá podkladový skin.
        /// Silent = setování této hodnoty neprovádí Invalidate.
        /// </summary>
        internal Color? BackColorUserSilent
        {
            get { return _BackColorUser; }
            set
            {
                if (value != _BackColorUser)
                {
                    _BackColorUser = value;
                }
            }
        }
        /// <summary>
        /// Barva pozadí uživatelská, má přednost před skinem, aplikuje se na hotový skin, může obsahovat Alpha kanál = pak skrz tuto barvu prosvítá podkladový skin
        /// </summary>
        public Color? BackColorUser 
        { 
            get { return _BackColorUser; } 
            set 
            {
                if (value != _BackColorUser)
                {
                    _BackColorUser = value;
                    Invalidate();
                }
            }
        }
        private Color? _BackColorUser;
        /// <summary>
        /// Počet pixelů aktuálního rámečku (na každé straně)
        /// </summary>
        public int BorderWidth
        {
            get
            {
                switch (this.BorderStyle)
                {
                    case DevExpress.XtraEditors.Controls.BorderStyles.NoBorder: return 0;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Simple: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Flat: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.HotFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Style3D: return 2;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Office2003: return 1;
                    case DevExpress.XtraEditors.Controls.BorderStyles.Default: return 1;
                }
                return 0;
            }
        }
        /// <summary>
        /// Gets or sets the panel's border style.
        /// </summary>
        public new DevExpress.XtraEditors.Controls.BorderStyles BorderStyle
        {
            get { return base.BorderStyle; }
            set
            {
                if (value != base.BorderStyle)
                {
                    base.BorderStyle = value;
                    OnBorderStyleChanged();
                    BorderStyleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        protected virtual void OnBorderStyleChanged() { }
        /// <summary>
        /// Při změně <see cref="BorderStyle"/>
        /// </summary>
        public event EventHandler BorderStyleChanged;
        /// <summary>
        /// Povolit průhlednost panelu?
        /// </summary>
        public bool AllowTransparency { get; set; }
        /// <summary>
        /// Povolit průhlednost panelu?
        /// Hodnotu čte DevExpress při zpracování panelu. V DxPanelControl ji umožníme nastavit.
        /// </summary>
        protected override bool AllowTotalTransparency { get { return AllowTransparency; /* namísto base.AllowTotalTransparency */ } }
        /// <summary>
        /// Souřadnice vnitřního prostoru panelu.
        /// Pokud Panel má nějaký Border, který je vykreslován uvnitř <see cref="Control.ClientRectangle"/>, 
        /// pak <see cref="InnerRectangle"/> je o tento Border zmenšený.
        /// </summary>
        public Rectangle InnerRectangle
        {
            get
            {
                var size = Size;
                var clientSize = ClientSize;
                var borderWidth = BorderWidth;
                if (clientSize.Width == size.Width && borderWidth > 0)
                {   // DevExpress s oblibou tvrdí, že ClientSize == Size, a přitom mají Border nenulové velikosti. Pak by se nám obsah kreslil přes Border.
                    int b2 = 2 * borderWidth;
                    return new Rectangle(borderWidth, borderWidth, size.Width - b2, size.Height - b2);
                }
                return new Rectangle(Point.Empty, clientSize);
            }
        }
        /// <summary>
        /// Jsou aktivní zápisy do logu? Default = <see cref="DxComponent.LogActive"/>
        /// </summary>
        public virtual bool LogActive { get; set; }
        #endregion
        #region HasMouse a InteractiveState
        /// <summary>
        /// Panel má na sobě myš?
        /// Pozor, tato property signalizuje, že myš se nachází přímo na panelu na místě, kde není žádný Child control!
        /// Pokud na panelu je Child control a myš přejde na tento control, pak myš "odchází" z panelu a zde v <see cref="HasMouse"/> bude false!
        /// Lze ale testovat property <see cref="IsMouseOnPanel"/>.
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        protected virtual void OnHasMouseChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasMouseChanged;
        /// <summary>
        /// Obsahuje true, pokud myš se nachází nad klientským prostorem this panelemu, nebo nad některým z jeho Child prvků.
        /// Testuje polohu myši a pozici panelu.
        /// </summary>
        public bool IsMouseOnPanel
        {
            get
            {
                var mousePosition = this.PointToClient(Control.MousePosition);
                return this.ClientRectangle.Contains(mousePosition);
            }
        }
        /// <summary>
        /// Interaktivní stav tohoto prvku z hlediska Enabled, Mouse, Focus, Selected
        /// </summary>
        public virtual DxInteractiveState InteractiveState 
        {
            get
            {
                if (!this.Enabled) return DxInteractiveState.Disabled;
                DxInteractiveState state = DxInteractiveState.Enabled;
                if (IsMouseOnPanel) state |= DxInteractiveState.HasMouse;
                if (this.Focused) state |= DxInteractiveState.HasFocus;
                return state;
            }
        }
        /// <summary>
        /// DevExpress vyjádření interaktivního stavu tohoto panelu, vychází z <see cref="InteractiveState"/>
        /// </summary>
        public virtual ObjectState InteractiveObjectState
        {
            get
            {
                var interactiveState = this.InteractiveState;
                if (interactiveState.HasFlag(DxInteractiveState.Disabled)) return ObjectState.Disabled;

                ObjectState state = ObjectState.Normal;
                if (interactiveState.HasFlag(DxInteractiveState.HasMouse)) state |= ObjectState.Hot;
                if (interactiveState.HasFlag(DxInteractiveState.HasFocus)) state |= ObjectState.Selected;
                return state;
            }
        }
        #endregion
        #region Paint
        /// <summary>
        /// Základní kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.PaintBackColorUser(e);
            this.PaintItems(e);
        }
        /// <summary>
        /// Overlay kreslení BackColorUser
        /// </summary>
        /// <param name="e"></param>
        protected void PaintBackColorUser(PaintEventArgs e)
        {
            var backColorUser = BackColorUser;
            if (!backColorUser.HasValue) return;
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColorUser.Value), this.ClientRectangle);
        }
        #endregion
        #region PaintedItems
        /// <summary>
        /// Prvky, které se vykreslují přímo na podklad panelu
        /// </summary>
        public List<IDxPanelPaintedItem> PaintedItems { get { return _PaintedItems; } }
        private List<IDxPanelPaintedItem> _PaintedItems;
        /// <summary>
        /// Zajistí, že pro prvky v poli <see cref="PaintedItems"/> bude provedena jejich metoda <see cref="IDxPanelPaintedItem.OnPaint(PaintEventArgs)"/>
        /// </summary>
        /// <param name="e"></param>
        protected virtual void PaintItems(PaintEventArgs e)
        {
            PaintedItems.ForEachExec(i => i.OnPaint(e));
        }
        #endregion
        #region Style & Zoom Changed
        void IListenerZoomChange.ZoomChanged() { OnZoomChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně zoomu
        /// </summary>
        protected virtual void OnZoomChanged() { }
        void IListenerStyleChanged.StyleChanged() { OnStyleChanged(); DeviceDpiCheck(false); OnContentSizeChanged(); }
        /// <summary>
        /// Volá se po změně skinu
        /// </summary>
        protected virtual void OnStyleChanged() { }
        void IListenerApplicationIdle.ApplicationIdle() { OnApplicationIdle(); }
        /// <summary>
        /// Zavolá se v situaci, kdy aplikace nemá zrovna co na práci
        /// </summary>
        protected virtual void OnApplicationIdle() { }
        /// <summary>
        /// Po změně Parenta prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Po změně DPI v parentu prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            DeviceDpiCheck(true);
        }
        /// <summary>
        /// Při invalidaci prověříme DPI a případně zareagujeme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            DeviceDpiCheck(true);
            base.OnInvalidated(e);
        }
        /// <summary>
        /// Tento háček je vyvolán po jakékoli akci, která může vést k přepočtu vnitřních velikostí controlů.
        /// Je volán: po změně Zoomu, po změně Skinu, po změně DPI hostitelského okna.
        /// <para/>
        /// Potomek by v této metodě měl provést přepočty velikosti svých controlů, pokud závisejí na Zoomu a DPI (a možná Skinu) (rozdílnost DesignSize a CurrentSize).
        /// <para/>
        /// Metoda není volána po změně velikosti controlu samotného ani po změně ClientBounds, ta změna nezakládá důvod k přepočtu velikosti obsahu
        /// </summary>
        protected virtual void OnContentSizeChanged() { }
        #endregion
        #region DPI - podpora pro MultiMonitory s různým rozlišením / pro jiné DPI než designové
        /// <summary>
        /// Aktuálně platná hodnota DeviceDpi
        /// </summary>
        public int CurrentDpi { get { return this._CurrentDpi; } }
        /// <summary>
        /// Aktuální hodnota DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        private int _CurrentDpi;
        /// <summary>
        /// Znovu načte hodnotu DeviceDpi z formuláře / anebo z this controlu
        /// </summary>
        /// <returns></returns>
        private int _ReloadCurrentDpi()
        {
            _CurrentDpi = this.FindForm()?.DeviceDpi ?? this.DeviceDpi;
            return _CurrentDpi;
        }
        /// <summary>
        /// Hodnota DeviceDpi, pro kterou byly naposledy přepočteny souřadnice prostoru
        /// </summary>
        private int _LastDpi;
        /// <summary>
        /// Obsahuje true, pokud se nyní platné DPI liší od DPI posledně použitého pro přepočet souřadnic
        /// </summary>
        private bool _DpiChanged { get { return (this._CurrentDpi != this._LastDpi); } }
        /// <summary>
        /// Ověří, zda nedošlo ke změně DeviceDpi, a pokud ano pak zajistí vyvolání metod <see cref="OnCurrentDpiChanged()"/> a eventu <see cref="CurrentDpiChanged"/>.
        /// Pokud this panel není umístěn na formuláři, neprovede nic, protože DPI nemůže být platné.
        /// </summary>
        /// <param name="callContentSizeChanged">Pokud došlo ke změně DPI, má být volán háček <see cref="OnContentSizeChanged()"/>? Někdy to není nutné, protože se bude volat po této metodě vždy (i bez změny DPI).</param>
        protected void DeviceDpiCheck(bool callContentSizeChanged)
        {
            if (this.FindForm() == null) return;
            var currentDpi = _ReloadCurrentDpi();
            if (_DpiChanged)
            {
                OnCurrentDpiChanged();
                if (callContentSizeChanged)
                    OnContentSizeChanged();
                CurrentDpiChanged?.Invoke(this, EventArgs.Empty);
                _LastDpi = currentDpi;
            }
        }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        protected virtual void OnCurrentDpiChanged() { }
        /// <summary>
        /// Po jakékoli změně DPI
        /// </summary>
        public event EventHandler CurrentDpiChanged;
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
        /// Obsahuje true pro panel, který je nebo již byl disposován.
        /// Používejme přednostně před <see cref="Control.Disposing"/> nebo <see cref="Control.IsDisposed"/>.
        /// </summary>
        public bool IsDispose { get { return (__IsDispose || Disposing || IsDisposed); } } private bool __IsDispose;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
    }
    /// <summary>
    /// Interface, který zajistí, že konkrétní prvek bude mít možnost se vykreslit do svého hostitele, bude volána metoda <see cref="OnPaint(PaintEventArgs)"/>
    /// </summary>
    public interface IDxPanelPaintedItem
    {
        /// <summary>
        /// Hostitel žádá své prvky o vykreslení
        /// </summary>
        /// <param name="e"></param>
        void OnPaint(PaintEventArgs e);
        /// <summary>
        /// Libovolná aplikační data
        /// </summary>
        object Tag { get; }
    }
    #endregion
    #region DxSplitContainerControl
    /// <summary>
    /// SplitContainerControl
    /// </summary>
    public class DxSplitContainerControl : DevExpress.XtraEditors.SplitContainerControl
    {
        /// <summary>
        /// Zde je uvedena orientace oddělovací čáry. Lze setovat.
        /// Pokud jsou panely vlevo a vpravo, pak dělící čára má orientaci <see cref="SplitterOrientation"/> = <see cref="Orientation.Vertical"/> = je svislá;
        /// Pokud jsou panely nahoře a dole, pak dělící čára má orientaci <see cref="SplitterOrientation"/> = <see cref="Orientation.Horizontal"/> = je vodorovná;
        /// </summary>
        public Orientation SplitterOrientation
        {
            get { return (Horizontal ? Orientation.Vertical : Orientation.Horizontal); }
            set { Horizontal = (value == Orientation.Vertical); }
        }
    }
    #endregion
    #region DxLabelControl
    /// <summary>
    /// LabelControl
    /// </summary>
    public class DxLabelControl : DevExpress.XtraEditors.LabelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxLabelControl()
        {
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
    }
    #endregion
    #region DxImageArea : neinteraktivní obrázek s definovaným zdrojem a umístěním (v rámci nějakého parent controlu)
    /// <summary>
    /// Prvek, který může být vykreslen přímo do panelu <see cref="DxPanelControl"/> (a i jiných).
    /// Prvek lze přidat do seznamu <see cref="DxPanelControl.PaintedItems"/>, nelze jej přidat do <see cref="Control.Controls"/> (on to není <see cref="Control"/>).
    /// Prvek obsahuje souřadnice a definici obrázku, a tento obrázek je vykreslován do panelu na dané souřadnice.
    /// Prvek nemá žádnou interaktivitu.
    /// </summary>
    public class DxImageArea : IDxPanelPaintedItem
    {
        /// <summary>
        /// Konstruktor.
        /// Prvek lze přidat do seznamu <see cref="DxPanelControl.PaintedItems"/>, nikoliv do <see cref="Control.Controls"/>.
        /// </summary>
        public DxImageArea()
        {
            Alignment = ContentAlignment.MiddleCenter;
            Visible = true;
            BackColor = null;
            BorderColor = null;
            DotColor = null;
        }
        /// <summary>
        /// Souřadnice.
        /// Setování hodnoty neprovádí refresh parent panelu.
        /// </summary>
        public Rectangle Bounds { get; set; }
        /// <summary>
        /// Umístění levého horního rohu. Lze setovat, velikost se nezmění.
        /// </summary>
        public Point Location
        {
            get { return Bounds.Location; }
            set { Bounds = new Rectangle(value, Bounds.Size); }
        }
        /// <summary>
        /// Velikost objektu. Lze setovat, umístění se nezmění.
        /// </summary>
        public Size Size
        {
            get { return Bounds.Size; }
            set { Bounds = new Rectangle(Bounds.Location, value); }
        }
        /// <summary>
        /// Prvek je viditelný
        /// </summary>
        public bool Visible { get; set; }
        /// <summary>
        /// Použít testovací paletu?
        /// </summary>
        public bool UseCustomPalette { get; set; }
        /// <summary>
        /// Jméno obrázku.
        /// Default = null. Nekreslí se nic.
        /// Setování hodnoty neprovádí refresh parent panelu.
        /// <para/>
        /// Jméno je vyhledáno ve zdrojích aplikačních i DevExpress, smí to být vektor i bitmapa i skládaný vektorový obrázek.
        /// Nepodporujeme náhradní obrázek vytvořený pro Caption.
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Jméno obrázku je exaktně dané.
        /// Pokud je zde true, a ve jménu <see cref="ImageName"/> je jméno se suffixem velikosti a příponou, bude akceptováno. 
        /// Default = false.
        /// Setování hodnoty neprovádí refresh parent panelu.
        /// </summary>
        public bool ExactName { get; set; }
        /// <summary>
        /// Jaký obrázek má být preferován pro kreslení, pokud je na výběr? 
        /// true preferuje vektor, false bitmapu, null podle systému <see cref="DxComponent.IsPreferredVectorImage"/>.
        /// Default = null.
        /// Setování hodnoty neprovádí refresh parent panelu.
        /// </summary>
        public bool? IsPreferredVectorImage { get; set; }
        /// <summary>
        /// Barva pozadí, default: null = nekreslí se. Akceptuje se Alpha channel.
        /// Kreslí se v souřadnici <see cref="Bounds"/>.
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Barva rámečku, default: null = nekreslí se. Akceptuje se Alpha channel.
        /// Kreslí se v souřadnici <see cref="Bounds"/>.
        /// </summary>
        public Color? BorderColor { get; set; }
        /// <summary>
        /// Barva teček vyznačujících pixely.
        /// <para/>
        /// Doporučuje se použít průhlednost (A kanál), protože prvek vykresluje každou pátou tečku 2x přes sebe, tím jsou tyto tečky výraznější.
        /// Ale pokud NENÍ barva zadána s určitou průhledností, pak se tento efekt neprojeví.
        /// </summary>
        public Color? DotColor { get; set; }
        /// <summary>
        /// Barva hrany prostoru pro Image, default: null = nekreslí se. Akceptuje se Alpha channel.
        /// Kreslí se v souřadnici vlastního obrázku po jeho zarovnání.
        /// </summary>
        public Color? EdgeColor { get; set; }
        /// <summary>
        /// Zarovnání obrazu do <see cref="Bounds"/>. 
        /// Obraz bude zoomován do daného prostoru se zachováním proporcí.
        /// </summary>
        public ContentAlignment Alignment { get; set; }
        /// <summary>
        /// Nastavit vyhlazování grafiky? false = nikdy / true = ano / null = pokud poměr velikosti ikony a prostoru nebude == 1.00
        /// </summary>
        public bool? SetSmoothing { get; set; }
        /// <summary>
        /// Jakýkoli aplikační údaj
        /// </summary>
        public object Tag { get; set; }
        /// <summary>
        /// Prvek bude vykreslen do panelu
        /// </summary>
        /// <param name="e"></param>
        void IDxPanelPaintedItem.OnPaint(PaintEventArgs e)
        {
            var bounds = this.Bounds;

            int d = 0;
            if (BackColor.HasValue)
                e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(BackColor.Value), bounds);
            if (BorderColor.HasValue)
            {
                Rectangle borderBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width + d, bounds.Height + d);
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(BorderColor.Value), borderBounds);
            }

            var sizeTypeV = DxComponent.GetImageSizeTypeVector(bounds.Size);
            var sizeTypeB = DxComponent.GetImageSizeTypeBitmap(bounds.Size);
            if (sizeTypeV == ResourceImageSizeType.None) return;
            string imageName = this.ImageName;
            if (String.IsNullOrEmpty(imageName)) return;

            Rectangle? edgeBounds = null;
            Rectangle? viewBounds = null;
            bool isPainted = false;
            try
            {
                bool preferVector = this.IsPreferredVectorImage ?? DxComponent.IsPreferredVectorImage;
                if (preferVector)
                    isPainted = TryPaintVector(e.Graphics, imageName, sizeTypeV, bounds, out edgeBounds, out viewBounds);
                if (!isPainted)
                    isPainted = TryPaintBitmap(e.Graphics, imageName, sizeTypeB, bounds, out edgeBounds, out viewBounds);
                if (!isPainted)
                    isPainted = TryPaintVector(e.Graphics, imageName, sizeTypeV, bounds, out edgeBounds, out viewBounds);
            }
            catch { }

            // Okolo prostoru reálného Image mohu vykreslit linku v barvě EdgeColor:
            if (isPainted && edgeBounds.HasValue && this.EdgeColor.HasValue)
            {
                Rectangle borderBounds = new Rectangle(edgeBounds.Value.X, edgeBounds.Value.Y, edgeBounds.Value.Width + d, edgeBounds.Value.Height + d);
                e.Graphics.DrawRectangle(DxComponent.PaintGetPen(EdgeColor.Value), borderBounds);

                // Mohu vykreslit tečky v místech pixelů:
                if (viewBounds.HasValue && this.DotColor.HasValue)
                    PaintPixelDots(e.Graphics, edgeBounds.Value, viewBounds.Value);
            }
        }
        /// <summary>
        /// Zkusí najít vektorový obrázek a vykreslit jej
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="bounds"></param>
        /// <param name="edgeBounds"></param>
        /// <param name="viewBounds"></param>
        /// <returns></returns>
        private bool TryPaintVector(Graphics graphics, string imageName, ResourceImageSizeType sizeType, Rectangle bounds, out Rectangle? edgeBounds, out Rectangle? viewBounds)
        {
            viewBounds = null;
            /*
            var svgPaletteName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSvgPaletteName;

            var skin = DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
            var svgPalettes = skin.SvgPalettes;
            var customSvgPalettes = skin.CustomSvgPalettes;
            var svgPalette = customSvgPalettes.Values.LastOrDefault();
            */

            DevExpress.Utils.Svg.SvgPalette palette = null;

            if (this.UseCustomPalette)
            {
                palette = new DevExpress.Utils.Svg.SvgPalette();
                palette.Colors.Add(new DevExpress.Utils.Svg.SvgColor("Modrá", Color.FromArgb(33, 33, 240)));
                palette.Colors.Add(new DevExpress.Utils.Svg.SvgColor("Fialová", Color.FromArgb(240, 33, 240)));
                palette.Colors.Add(new DevExpress.Utils.Svg.SvgColor("Zelenkavá", Color.FromArgb(190, 210, 190)));
                DevExpress.Utils.Svg.SvgPaletteKey key = new DevExpress.Utils.Svg.SvgPaletteKey(99, "Nephrite");

                var commonSkin = DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
                var ribbonSkin = DevExpress.Skins.RibbonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
                var barSkin = DevExpress.Skins.BarSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
                ribbonSkin.CustomSvgPalettes.GetOrAdd(key, k => palette);
                ribbonSkin.SvgPalettes.GetOrAdd(key, k => palette);

                if (commonSkin.SvgPalettes[DevExpress.Skins.Skin.DefaultSkinPaletteName] != null)
                    commonSkin.SvgPalettes[DevExpress.Skins.Skin.DefaultSkinPaletteName].CustomPalette = palette;
            }

            edgeBounds = null;
            var svgImage = DxComponent.GetVectorImage(imageName, this.ExactName, sizeType);
            if (svgImage == null) return false;
            DxSvgImage.RenderTo(svgImage, graphics, bounds, out var imageBounds, Alignment, palette, this.SetSmoothing);
            if (imageBounds.HasValue) edgeBounds = Rectangle.Ceiling(imageBounds.Value);
            viewBounds = Rectangle.Ceiling((svgImage is DxSvgImage dxSvgImage) ? dxSvgImage.ViewBounds : DxSvgImage.Create(svgImage).ViewBounds);
            return true;
        }
        /// <summary>
        /// Zkusí najít bitmapový obrázek a vykreslit jej
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="imageName"></param>
        /// <param name="sizeType"></param>
        /// <param name="bounds"></param>
        /// <param name="edgeBounds"></param>
        /// <param name="viewBounds"></param>
        /// <returns></returns>
        private bool TryPaintBitmap(Graphics graphics, string imageName, ResourceImageSizeType sizeType, Rectangle bounds, out Rectangle? edgeBounds, out Rectangle? viewBounds)
        {
            edgeBounds = null;
            viewBounds = null;
            var bmpImage = DxComponent.GetBitmapImage(imageName, sizeType, exactName: this.ExactName);
            if (bmpImage == null) return false;
            var imageSize = bmpImage.Size;
            RectangleF imageBounds = ((SizeF)imageSize).ZoomTo((RectangleF)bounds, Alignment);
            graphics.DrawImage(bmpImage, imageBounds);
            edgeBounds = Rectangle.Ceiling(imageBounds);
            viewBounds = new Rectangle(Point.Empty, imageSize);
            return true;
        }
        /// <summary>
        /// Vykreslí tečky na hranách pixelů
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="edgeBounds"></param>
        /// <param name="viewBounds"></param>
        private void PaintPixelDots(Graphics graphics, Rectangle edgeBounds, Rectangle viewBounds)
        {
            if (!edgeBounds.HasPixels() || !viewBounds.HasPixels()) return;

            float left = edgeBounds.Left;
            float right = edgeBounds.Right;
            float top = edgeBounds.Top;
            float bottom = edgeBounds.Bottom;
            float addX = (float)edgeBounds.Width / (float)viewBounds.Width;
            float addY = (float)edgeBounds.Height / (float)viewBounds.Height;
            var brush = DxComponent.PaintGetSolidBrush(this.DotColor.Value);
            Rectangle r = new Rectangle(0, 0, 1, 1);
            int ix = 0;
            for (float x = left; x <= right; x += addX)
            {
                int iy = 0;
                for (float y = top; y <= bottom; y += addY)
                {
                    r.X = (int)Math.Round(x, 0);
                    r.Y = (int)Math.Round(y, 0);
                    graphics.FillRectangle(brush, r);
                    if (((ix % 5) == 0) || ((iy % 5) == 0))
                        graphics.FillRectangle(brush, r);
                    iy++;
                }
                ix++;
            }
        }
    }
    #endregion
    #region DxTextEdit
    /// <summary>
    /// TextEdit
    /// </summary>
    public class DxTextEdit : DevExpress.XtraEditors.TextEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTextEdit()
        {
            EnterMoveNextControl = true;
        }
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxButtonEdit
    /// <summary>
    /// Třída spojující TextEdit + sadu buttonů, implicitně jeden vpravo.
    /// </summary>
    public class DxButtonEdit : DevExpress.XtraEditors.ButtonEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxButtonEdit()
        {
            _ButtonsVisibility = DxChildControlVisibility.Allways;
            _ButtonsIsVisible = null;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        #region Buttony mohou být viditelné jen na 'aktivním' prvku
        /// <summary>
        /// Viditelnost buttonů z hlediska aktivity. Implicitní je <see cref="DxChildControlVisibility.Allways"/>
        /// </summary>
        public DxChildControlVisibility ButtonsVisibility { get { return _ButtonsVisibility; } set { _ButtonsVisibility = value; RefreshButtonsVisibility(); } }
        private DxChildControlVisibility _ButtonsVisibility;
        /// <summary>
        /// Předdefinovaný druh prvního buttonu. 
        /// Slouží k jednoduchému nastavení pro jednobuttonový prvek. Interně pracuje s <see cref="DxButtonEdit"/>.Properties.Buttons[0]
        /// </summary>
        public DevExpress.XtraEditors.Controls.ButtonPredefines ButtonKind
        {
            get 
            {
                if (this.Properties.Buttons.Count == 0) return ButtonPredefines.Separator;
                return this.Properties.Buttons[0].Kind; 
            }
            set 
            {
                if (this.Properties.Buttons.Count == 0)
                    this.Properties.Buttons.Add(new EditorButton(value));
                else
                    this.Properties.Buttons[0].Kind = value;
            }
        }
        /// <summary>
        /// Jméno obrázku prvního buttonu (na indexu [0]). Lze číst i zapisovat. Setování NULL skryje button.<br/>
        /// Slouží k jednoduchému nastavení pro jednobuttonový prvek. Interně pracuje s <see cref="DxButtonEdit"/>.Properties.Buttons[0]
        /// <para/>
        /// Poznámka: viditelnost buttonů v závislosti na MouseOn nebo Focus řídí property <see cref="ButtonsVisibility"/>.
        /// <para/>
        /// Pokud aplikace přidá více buttonů, pak ostatní buttony nejsou setováním ovlivněny.<br/>
        /// Pokud aplikace přímo změní charakter buttonu [0], může následující čtení jména obrázku <see cref="ButtonImage"/> vrátit nesprávný název (posledně setovaný).
        /// </summary>
        public string ButtonImage
        {
            get
            {
                if (this.Properties.Buttons.Count == 0) return null;
                if (this.Properties.Buttons[0].Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph) return null;
                return __ButtonImage;
            }
            set
            {
                string imageName = value;
                bool hasImage = !String.IsNullOrEmpty(imageName);

                // Button:
                DevExpress.XtraEditors.Controls.EditorButton button = null;
                if (this.Properties.Buttons.Count == 0)
                {
                    if (hasImage)
                    {
                        button = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
                        this.Properties.Buttons.Add(button);
                    }
                }
                else
                {
                    button = this.Properties.Buttons[0];
                }

                if (hasImage)
                {
                    button.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                    DxComponent.ApplyImage(button.ImageOptions, imageName, sizeType: ResourceImageSizeType.Small);
                    button.Visible = true;
                    __ButtonImage = imageName;
                }
                else
                {
                    button.Visible = false;
                    __ButtonImage = null;
                }
            }
        }
        private string __ButtonImage;
        /// <summary>
        /// Předdefinovaný styl zobrazení buttonů. 
        /// </summary>
        public DevExpress.XtraEditors.Controls.BorderStyles ButtonsStyle
        {
            get { return this.Properties.ButtonsStyle; }
            set { this.Properties.ButtonsStyle = value; }
        }
        /// <summary>
        /// Nastaví aktuální viditelnost buttonu podle definice a podle aktuálního stavu
        /// </summary>
        private void RefreshButtonsVisibility()
        {
            var visibility = ButtonsVisibility;
            bool isVisible = (visibility.HasFlag(DxChildControlVisibility.Allways) ||
                              (visibility.HasFlag(DxChildControlVisibility.OnMouse) && HasMouse) ||
                              (visibility.HasFlag(DxChildControlVisibility.OnFocus) && HasFocus));
            if (!_ButtonsIsVisible.HasValue || isVisible != _ButtonsIsVisible.Value)
            {
                _ButtonsIsVisible = isVisible;
                this.Properties.Buttons.ForEachExec(b => b.Visible = isVisible);
            }
        }
        private bool? _ButtonsIsVisible;
        #endregion
        #region Buttony obecně
        /// <summary>
        /// Přidá button
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="isLeft"></param>
        /// <param name="tag"></param>
        /// <param name="width"></param>
        public void AddButton(DevExpress.XtraEditors.Controls.ButtonPredefines kind, bool isLeft = false, string toolTipText = null, string toolTipTitle = null, object tag = null, int? width = null)
        {
            var button = _CreateEditorButton(kind, null, isLeft, toolTipText, toolTipTitle, tag, width);
            this.Properties.Buttons.Add(button);
        }
        /// <summary>
        /// Přidá button
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipTitle"></param>
        /// <param name="isLeft"></param>
        /// <param name="tag"></param>
        /// <param name="width"></param>
        public void AddButton(string imageName, bool isLeft = false, string toolTipText = null, string toolTipTitle = null, object tag = null, int? width = null)
        {
            var button = _CreateEditorButton(ButtonPredefines.Glyph, imageName, isLeft, toolTipText, toolTipTitle, tag, width);
            this.Properties.Buttons.Add(button);
        }
        private static EditorButton _CreateEditorButton(ButtonPredefines kind, string imageName, bool isLeft = false, string toolTipText = null, string toolTipTitle = null, object tag = null, int? width = null)
        {
            EditorButton button = new EditorButton();

            button.Kind = kind;
            if (kind == ButtonPredefines.Glyph && !String.IsNullOrEmpty(imageName))
                DxComponent.ApplyImage(button.ImageOptions, imageName, sizeType: ResourceImageSizeType.Small);

            // ToolTip může být s titulkem nebo bez něj:
            if (DxComponent.PrepareToolTipTexts(toolTipTitle, toolTipText, null, out var tipTitle, out var tipText))
            {
                if (!String.IsNullOrEmpty(tipTitle))
                    button.SuperTip = DxComponent.CreateDxSuperTip(tipTitle, tipText);
                else
                    button.ToolTip = tipText;
                button.ToolTipAnchor = ToolTipAnchor.Cursor;
            }

            button.IsLeft = isLeft;
            button.Tag = tag;
            if (width.HasValue && width.Value > 0) 
                button.Width = width.Value;

            return button;
        }
        #endregion
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
                    RefreshButtonsVisibility();
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
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    RefreshButtonsVisibility();
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxRichEditControl
    /// <summary>
    /// DxRichEditControl
    /// </summary>
    public class DxRichEditControl : DevExpress.XtraRichEdit.RichEditControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxRichEditControl()
        {
            this.ActiveViewType = DevExpress.XtraRichEdit.RichEditViewType.Simple;
        }
    }
    #endregion
    #region DxMemoEdit
    /// <summary>
    /// MemoEdit
    /// </summary>
    public class DxMemoEdit : DevExpress.XtraEditors.MemoEdit
    {
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
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxTokenEdit
    /// <summary>
    /// <see cref="DxTokenEdit"/>
    /// </summary>
    public class DxTokenEdit : DevExpress.XtraEditors.TokenEdit
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTokenEdit()
        {
            _Tokens = new List<IMenuItem>();
            ShowDropDown = true;
        }
        #region Tokeny = položky v nabídce
        /// <summary>
        /// Soupis nabídek v tomto prvku. Lze setovat.
        /// </summary>
        public IEnumerable<IMenuItem> Tokens
        {
            get { return _Tokens; }
            set { _AddTokens(value, true); }
        }
        /// <summary>
        /// Zobrazovat DropDown?
        /// </summary>
        public bool ShowDropDown { get { return this.Properties.ShowDropDown; } set { this.Properties.ShowDropDown = value; } }
        /// <summary>
        /// Počet nabídek v tomto prvku.
        /// </summary>
        public int TokensCount { get { return _Tokens.Count; } }
        /// <summary>
        /// Smaže stávající tokeny
        /// </summary>
        public void TokensClear()
        {
            this.Properties.Tokens.Clear();
            _Tokens.Clear();
        }
        /// <summary>
        /// Přidá další prvky do nabídek v tomto prvku.
        /// </summary>
        /// <param name="tokens"></param>
        public void TokensAddRange(IEnumerable<IMenuItem> tokens)
        {
            _AddTokens(tokens, false);
        }
        /// <summary>
        /// Do this prvku přidá dané tokeny. Volitelně před tím dosavadní prvky odstraní.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="clear"></param>
        private void _AddTokens(IEnumerable<IMenuItem> tokens, bool clear)
        {
            var dxTokens = _CreateDxTokens(tokens);

            this.Properties.BeginUpdate();
            if (clear)
            {
                this.Properties.Tokens.Clear();
                _Tokens.Clear();
            }
            if (dxTokens.Count > 0)
            {
                this.Properties.Tokens.AddRange(dxTokens);
                _Tokens.AddRange(tokens);
            }
            this.Properties.EndUpdate();
        }
        /// <summary>
        /// Z dodaných dat typu <see cref="IMenuItem"/> vrátí pole prvků natvního typu pro TokenEdit : <see cref="DevExpress.XtraEditors.TokenEditToken"/>.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private List<DevExpress.XtraEditors.TokenEditToken> _CreateDxTokens(IEnumerable<IMenuItem> tokens)
        {
            List<DevExpress.XtraEditors.TokenEditToken> dxTokens = new List<DevExpress.XtraEditors.TokenEditToken>();
            if (tokens != null)
            {
                foreach (var token in tokens)
                    dxTokens.Add(new DevExpress.XtraEditors.TokenEditToken(token.Text, token.ItemId));
            }
            return dxTokens;
        }
        private List<IMenuItem> _Tokens;
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
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
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

    }
    #endregion
    #region DxImageComboBoxEdit
    /// <summary>
    /// ImageComboBoxEdit
    /// </summary>
    public class DxImageComboBoxEdit : DevExpress.XtraEditors.ImageComboBoxEdit
    {
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
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxSpinEdit
    /// <summary>
    /// SpinEdit
    /// </summary>
    public class DxSpinEdit : DevExpress.XtraEditors.SpinEdit
    {
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxCheckEdit
    /// <summary>
    /// CheckEdit
    /// </summary>
    public class DxCheckEdit : DevExpress.XtraEditors.CheckEdit, IHotKeyControl
    {
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        /// <summary>
        /// Klávesa, která aktivuje button
        /// </summary>
        public Keys HotKey { get; set; }
        /// <summary>
        /// Provede kliknutí na CheckBox
        /// </summary>
        public void PerformClick()
        {
            this.OnClick(EventArgs.Empty);
        }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxSimpleButton
    /// <summary>
    /// SimpleButton
    /// </summary>
    public class DxSimpleButton : DevExpress.XtraEditors.SimpleButton, IHotKeyControl
    {
        #region Button skryté property
        /// <summary>
        /// Styl borderu
        /// </summary>
        public new BorderStyles BorderStyle { get { return base.BorderStyle; } set { base.BorderStyle = value; } }
        /// <summary>
        /// Jméno ikony
        /// </summary>
        public string ImageName
        {
            get
            {
                return __ImageName;
            }
            set
            {
                if (!String.Equals(__ImageName, value, StringComparison.InvariantCulture))
                {
                    __Image = null;
                    __ImageName = value;
                    DxComponent.ApplyImage(this.ImageOptions, value, null, null, null, true);
                    this.PrepareSizeSvgImage(true);
                    this.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
                    this.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
                }
            }
        }
        private string __ImageName;
        /// <summary>
        /// Obrázek ikony
        /// </summary>
        public new Image Image
        {
            get
            {
                return __Image;
            }
            set
            {
                __ImageName = null;
                __Image = value;
                DxComponent.ApplyImage(this.ImageOptions, null, value, null, null, true);
            }
        }
        private Image __Image;
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        /// <summary>
        /// Klávesa, která aktivuje button
        /// </summary>
        public Keys HotKey { get; set; }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region HasFocus
        /// <summary>
        /// TextBox má v sobě focus = kurzor?
        /// </summary>
        public bool HasFocus
        {
            get { return _HasFocus; }
            private set
            {
                if (value != _HasFocus)
                {
                    _HasFocus = value;
                    OnHasFocusChanged();
                    HasFocusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool _HasFocus;
        /// <summary>
        /// Událost, když přišel nebo odešel focus = kurzor
        /// </summary>
        protected virtual void OnHasFocusChanged() { }
        /// <summary>
        /// Událost, když přišla nebo odešla myš
        /// </summary>
        public event EventHandler HasFocusChanged;
        /// <summary>
        /// OnEnter
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.HasFocus = true;
        }
        /// <summary>
        /// OnLeave
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.HasFocus = false;
        }
        #endregion
        #region ToolTip
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
        #region Resize a SvgImage
        /// <summary>
        /// Automaticky upravovat velikost SvgImage podle výšky bttonu
        /// </summary>
        public bool AutoResizeSvgImage { get; set; } = true;
        /// <summary>
        /// Po změně velikosti vnitřního prostoru
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.PrepareSizeSvgImage();
        }
        /// <summary>
        /// Pokud button má SvgImage, pak si upraví jeho velikost
        /// </summary>
        /// <param name="force">Upravit velikost povinně, tj. i když <see cref="AutoResizeSvgImage"/> = false</param>
        public void PrepareSizeSvgImage(bool force = true)
        {
            if ((force || this.AutoResizeSvgImage) && this.ImageOptions.SvgImage != null)
            {   // Pokud je extra požadavek, nebo je AutoResize, a máme dán SvgImage, tak upravíme jeho velikost:
                int d = this.Padding.Vertical + this.Margin.Vertical + 4;
                int h = this.Height - d;
                Size s = new Size(h, h);
                this.ImageOptions.SvgImageSize = s;
            }
        }
        #endregion
    }
    #endregion
    #region DxDropDownButton
    /// <summary>
    /// DxDropDownButton
    /// </summary>
    public class DxDropDownButton : DevExpress.XtraEditors.DropDownButton
    {
        #region Specifické property
        /// <summary>
        /// Rozbalit DropDown menu po kliknutí na Button?
        /// </summary>
        public bool OpenDropDownOnButtonClick { get; set; }
        /// <summary>
        /// Po kliknutí na samotný Button může dojít k rozbalení DropDown menu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (this.OpenDropDownOnButtonClick)
            {
                this.DoShowDropDown();
            }
        }
        /// <summary>
        /// Po kliknutí na samotný Button může dojít k rozbalení DropDown menu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (this.OpenDropDownOnButtonClick) 
            {
                this.DoShowDropDown();
            }
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
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.GetTypeName(); }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
    }
    #endregion
    #region DxCheckButton
    /// <summary>
    /// CheckButton
    /// </summary>
    public class DxCheckButton : DevExpress.XtraEditors.CheckButton, IHotKeyControl
    {
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
        public override string ToString() { return this.GetTypeName() + ": '" + (this.Text ?? "NULL") + "'"; }
        /// <summary>
        /// Klávesa, která aktivuje button
        /// </summary>
        public Keys HotKey { get; set; }
        #endregion
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
            base.OnMouseLeave(e);
            this.HasMouse = false;
        }
        #endregion
        #region ToolTip
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
        #region Resize a SvgImage

        #endregion
    }
    #endregion
    #region DxToolTipController
    /// <summary>
    /// ToolTipController s přidanou hodnotou
    /// </summary>
    public class DxToolTipController : ToolTipController
    {
        #region Konstruktor + Default Setting + Dispose
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="toolTipAnchor">Ukotvení ToolTipu se odvozuje od ...</param>
        /// <param name="toolTipLocation">Pozice ToolTipu je ... od ukotvení</param>
        public DxToolTipController(ToolTipAnchor toolTipAnchor = ToolTipAnchor.Object, ToolTipLocation toolTipLocation = ToolTipLocation.RightBottom)
            : base()
        {
            _InitializeStopWatch();
            SetDefaultSettings(toolTipAnchor, toolTipLocation);
            _InitializeEvents();
        }
        /// <summary>
        /// Defaultní nastavení
        /// </summary>
        /// <param name="toolTipAnchor">Ukotvení ToolTipu se odvozuje od ...</param>
        /// <param name="toolTipLocation">Pozice ToolTipu je ... od ukotvení</param>
        private void SetDefaultSettings(ToolTipAnchor toolTipAnchor = ToolTipAnchor.Object, ToolTipLocation toolTipLocation = ToolTipLocation.RightBottom)
        {
            Active = true;
            InitialDelay = 1000;
            ReshowDelay = 500;
            AutoPopDelay = 10000;
            SlowMouseMovePps = DEFAULT_SILENT_PIXEL_PER_SECONDS;
            AutoHideAdaptive = true;
            KeepWhileHovered = false;
            Rounded = true;
            RoundRadius = 20;
            ShowShadow = true;
            ToolTipAnchor = toolTipAnchor;
            ToolTipLocation = toolTipLocation;
            ToolTipStyle = DevExpress.Utils.ToolTipStyle.Windows7;
            ToolTipType = DevExpress.Utils.ToolTipType.SuperTip;          // Standard   Flyout   SuperTip;
            ToolTipIndent = 20;
            IconSize = DevExpress.Utils.ToolTipIconSize.Large;
            CloseOnClick = DevExpress.Utils.DefaultBoolean.True;
            ShowBeak = true;                                              // Callout beaks are not supported for SuperToolTip objects.
            CloseOnClick = DefaultBoolean.True;

            Appearance.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;

            __Clients = new List<ClientInfo>();
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _ClientsDispose();
            base.Dispose(disposing);
        }
        #endregion
        #region Public
        /// <summary>
        /// Počet milisekund mezi zastavením myši a rozsvícením ToolTipu.
        /// Platí pro první tooltip po najetí myši na celý Control. 
        /// Pokud se pohybujeme nad jedním Controlem a měníme Tooltipy (pro různé prvky na jednom Controlu) = poté, kdy už byl první ToolTip rozsvícen a zhasnut,
        /// pak pro druhý a další ToolTipy platí čas <see cref="HoverNextMiliseconds"/>.<br/>
        /// 0 = zobrazit ihned = stále.<br/>
        /// Má rozsah 0 až 1 minuta.<br/>
        /// Odpovídá <see cref="ToolTipController.InitialDelay"/>.
        /// </summary>
        public int HoverFirstMiliseconds
        {
            get
            {
                var ms = InitialDelay;
                return (ms < 0 ? 0 : (ms > 60000 ? 60000 : ms));
            }
            set { InitialDelay = value; }
        }
        /// <summary>
        /// Počet milisekund mezi zastavením myši a rozsvícením ToolTipu.
        /// Platí pro druhý a další ToolTip od dalšího prvku v rámci stejného Controlu = pokud se myš drží nad jedním Controlem. Jakmile z Controlu odejde a poté se na něj vrátí, platí interval <see cref="HoverFirstMiliseconds"/>.<br/>
        /// 0 = zobrazit ihned = stále.<br/>
        /// Má rozsah 0 až 1 minuta.<br/>
        /// Odpovídá <see cref="ToolTipController.InitialDelay"/>.
        /// </summary>
        public int HoverNextMiliseconds
        {
            get
            {
                var ms = ReshowDelay;
                return (ms < 0 ? 0 : (ms > 60000 ? 60000 : ms));
            }
            set { ReshowDelay = value; }
        }
        /// <summary>
        /// Počet milisekund mezi rozsvícením ToolTipu a jeho automatickým zhasnutím.
        /// 0 = nezhasínat nikdy.<br/>
        /// Má rozsah 0 až 10 minut.<br/>
        /// Odpovídá <see cref="ToolTipController.AutoPopDelay"/>.
        /// </summary>
        public int AutoHideMiliseconds
        {
            get
            {
                var ms = AutoPopDelay;
                return (ms < 0 ? 0 : (ms > 600000 ? 600000 : ms));
            }
            set { AutoPopDelay = value; }
        }
        /// <summary>
        /// Čas zobrazení ToolTipu <see cref="AutoHideMiliseconds"/> má být upraven podle délky textu ToolTipu.<br/>
        /// Pokud je false, pak čas <see cref="AutoHideMiliseconds"/> je použit jako konstanta vždy.<br/>
        /// Pokud je true, pak <see cref="AutoHideMiliseconds"/> je dolní hodnota pro texty do 120 znaků, pro texty delší je čas navyšován podle délky textu a ž na 5-ti násobek tohoto času.
        /// </summary>
        public bool AutoHideAdaptive { get; set; }
        /// <summary>
        /// Rychlost myši v pixelech za sekundu, pod kterou se myš považuje za "stojící" a umožní tak rozsvícení ToolTipu i při pomalém pohybu.
        /// Výchozí hodnota je 120 px/sec. Lze nastavit hodnotu 20 - 10000.
        /// Menší hodnota = myš musí skoro stát aby se rozsvítil ToolTip.
        /// Větší hodnota = ToolTip se rozsvítí i za pohybu.
        /// </summary>
        public double SlowMouseMovePps
        {
            get { return __SlowMouseMovePps; }
            set { __SlowMouseMovePps = (value < 20d ? 20d : (value > 10000d ? 10000d : value)); }
        }
        private double __SlowMouseMovePps;

        /// <summary>
        /// Vzdálenost mezi ukazatelem myši a ToolTipem v pixelech. Výchozí je 20. Platné hodnoty jsou 0 - 64 px.
        /// </summary>
        public int ToolTipIndent
        {
            get { return __ToolTipIndent; }
            set
            {
                __ToolTipIndent = (value < 0 ? 0 : (value > 64 ? 64 : value));
            }
        }
        private int __ToolTipIndent;
        #endregion
        #region Eventy ToolTipu
        private void _InitializeEvents()
        {
            this.BeforeShow += _BeforeShow;
        }
        /// <summary>
        /// Před zobrazením ToolTipu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.ToolTip) && __ActiveClientInfo != null && __ActiveClientInfo.HasContentForSuperTip())
            {
                // Pokud se aktuálně pohybuji nad klientem, který poskytuje specifický ToolTip a má pro něj data (obsah), pak defaultní Tooltip zruším:
                //  => případ DxGridu, který do ToolTipu posílá titulek ColumnHeaderu, když se mu nevejde zobrazit v celé šíři sloupce:
                e.ToolTip = "";
            }
        }
        #endregion
        #region Clients - klientské Controly: evidence, eventhandlery, vyhledání, předání ke konkrétní práci, dispose
        /// <summary>
        /// Přidá klienta.
        /// Pokud klient implementuje <see cref="IDxToolTipClient"/>, 
        /// pak Controller ve vhodném okamžiku volá metodu pro získání ToolTipu pro konkrétní pozici klientského controlu.
        /// </summary>
        /// <param name="client"></param>
        public void AddClient(Control client)
        {
            if (client is null) return;

            _AttachClient(client);
            _AddClient(client);
        }
        /// <summary>
        /// Zapojí eventhandlery do klienta
        /// </summary>
        /// <param name="client"></param>
        private void _AttachClient(Control client)
        {
            _DetachClient(client);
            client.MouseEnter += _Client_MouseEnter;
            client.MouseMove += _Client_MouseMove;
            client.MouseLeave += _Client_MouseLeave;
            client.MouseDown += _Client_MouseDown;
            client.Leave += _Client_Leave;
            client.Disposed += _Client_Disposed;
        }
        /// <summary>
        /// Odpojí eventhandlery z klienta
        /// </summary>
        /// <param name="client"></param>
        private void _DetachClient(Control client)
        {
            client.MouseEnter -= _Client_MouseEnter;
            client.MouseMove -= _Client_MouseMove;
            client.MouseLeave -= _Client_MouseLeave;
            client.MouseDown -= _Client_MouseDown;
            client.Leave -= _Client_Leave;
            client.Disposed -= _Client_Disposed;
        }
        /// <summary>
        /// Event MouseEnter z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseEnter(object sender, EventArgs e)
        {
            _ResetHoverInterval();
        }
        /// <summary>
        /// Event MouseMove z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None && this._TrySearchClient(sender, out var clientInfo))
                _ClientMouseMoveNone(clientInfo, e);
        }
        /// <summary>
        /// Event MouseDown z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseDown(object sender, MouseEventArgs e)
        {
            if (this._TrySearchClient(sender, out var clientInfo))
                _ClientMouseDown(clientInfo, e);
        }
        /// <summary>
        /// Event MouseLeave z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_MouseLeave(object sender, EventArgs e)
        {
            // if (this._TrySearchClient(sender, out var clientInfo))
            //     _ClientMouseLeave(clientInfo, e);
            _ClientMouseLeave();
        }
        /// <summary>
        /// Event Leave z any Controlu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_Leave(object sender, EventArgs e)
        {
            // if (this._TrySearchClient(sender, out var clientInfo))
            //    _ClientLeave(clientInfo, e);
            _ClientLeave();
        }
        /// <summary>
        /// Disposuje evidenci klientů
        /// </summary>
        private void _ClientsDispose()
        {
            if (__Clients is null) return;
            var clients = __Clients.ToArray();
            foreach (var client in clients)
            {
                if (client != null && client.Client != null)
                    _DetachClient(client.Client);
            }
            __Clients.Clear();
        }
        /// <summary>
        /// Po Dispose klienta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Client_Disposed(object sender, EventArgs e)
        {
            if (sender is Control client)
            {
                _DetachClient(client);
                _RemoveClient(client);
            }
        }
        /// <summary>
        /// Přidá daného klienta
        /// </summary>
        /// <param name="client"></param>
        private void _AddClient(Control client)
        {
            lock (__Clients)
            {
                if (!__Clients.Any(c => Object.ReferenceEquals(c, client)))
                {
                    int id = ++_LastClientId;
                    __Clients.Add(new ClientInfo(id, client));
                }
            }
        }
        /// <summary>
        /// Zkusí najít klienta odpovídající danému senderu (což by měl být Control)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="clientInfo"></param>
        /// <returns></returns>
        private bool _TrySearchClient(object sender, out ClientInfo clientInfo)
        {
            clientInfo = null;
            return (sender != null && sender is Control control && __Clients.TryGetFirst(ci => Object.ReferenceEquals(ci.Client, control), out clientInfo));
        }
        /// <summary>
        /// Odebere daného klienta
        /// </summary>
        /// <param name="client"></param>
        private void _RemoveClient(Control client)
        {
            lock (__Clients)
            {
                __Clients.RemoveWhere(ci => (Object.ReferenceEquals(ci, client)), ci => ci.Dispose());
            }
        }
        private List<ClientInfo> __Clients;
        /// <summary>
        /// ID posledně přidaného klienta
        /// </summary>
        private int _LastClientId = 0;
        #endregion
        #region class ClientInfo : Balíček informací o jednom klientovi (řízený Control)
        /// <summary>
        /// Balíček informací o jednom klientovi
        /// </summary>
        private class ClientInfo : IDisposable
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="id"></param>
            /// <param name="client"></param>
            public ClientInfo(int id, Control client)
            {
                Id = id;
                Client = client;
                HasIDynamicClient = (client is IDxToolTipDynamicClient);
                IDynamicClient = (HasIDynamicClient ? client as IDxToolTipDynamicClient : null);
                HasIClient = !HasIDynamicClient && (client is IDxToolTipClient);
                IClient = (HasIClient ? client as IDxToolTipClient : null);
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string clientType = this.Client?.GetType().Name;
                string clientText = this.Client?.Text;
                return $"Client {clientType}: '{clientText}'{(HasIDynamicClient ? "; is IDxToolTipDynamicClient" : "")}{(HasIClient ? "; is IClient" : "")}.";
            }
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                Client = null;
                HasIClient = false;
                IClient = null;
            }
            /// <summary>
            /// Jednoduché ID klienta
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// Control klienta
            /// </summary>
            public Control Client { get; private set; }
            /// <summary>
            /// Klient implementuje <see cref="IDxToolTipDynamicClient"/>?
            /// </summary>
            public bool HasIDynamicClient { get; private set; }
            /// <summary>
            /// Klient jako <see cref="IDxToolTipDynamicClient"/>
            /// </summary>
            public IDxToolTipDynamicClient IDynamicClient { get; private set; }
            /// <summary>
            /// Klient implementuje <see cref="IDxToolTipClient"/>?
            /// </summary>
            public bool HasIClient { get; private set; }
            /// <summary>
            /// Klient jako <see cref="IDxToolTipClient"/>
            /// </summary>
            public IDxToolTipClient IClient { get; private set; }
            /// <summary>
            /// Souřadnice myši aktuální
            /// </summary>
            public Point MouseLocation { get; set; }
            /// <summary>
            /// Souřadnice myši minulá, pro detekci malého pohybu
            /// </summary>
            public Point? LastMouseLocation { get; set; }
            /// <summary>
            /// Čas souřadnice <see cref="LastMouseLocation"/>, pro detekci malého pohybu
            /// </summary>
            public long? LastMouseLocationTime { get; set; }
            /// <summary>
            /// Posledně získaný SuperTip z tohoto klienta
            /// </summary>
            public DxSuperToolTip LastSuperTip { get; set; }
            /// <summary>
            /// Posledně použitý argument <see cref="DxToolTipDynamicPrepareArgs"/> pro tohoto klienta, po <see cref="Reset"/> je null
            /// </summary>
            protected DxToolTipDynamicPrepareArgs LastPrepareArgs { get; set; }
            /// <summary>
            /// Vyvolá danou akci v GUI threadu aktuálního controlu <see cref="Client"/>
            /// </summary>
            /// <param name="action"></param>
            public void InvokeGui(Action action)
            {
                var client = Client;
                if (client is null || client.IsDisposed || client.Disposing) return;
                if (client.IsHandleCreated && client.InvokeRequired) client.BeginInvoke(action, null);
                else action();
            }
            /// <summary>
            /// Zkusí získat SuperTip z Controlu, podle jeho typu.<br/>
            /// Metoda vrací:<br/>
            /// true = máme něco s ToolTipem udělat? Rozsvítit nebo zhasnout: podle obsahu out <paramref name="superTip"/>;<br/>
            /// false = nemáme nic dělat, ani zhasínat ani rozsvěcet.
            /// </summary>
            /// <param name="isMouseHover">Jsme voláni po zastavení myši (když chceme rozsvítit ToolTip) = true / nebo za pohybu, když ToolTip svítí (když bychom jej měli zhasnout nebo vyměnit) = false</param>
            /// <param name="isTipVisible">Vstup: ToolTip aktuálně svítí a myš se pohybuje? Má vliv na vyhodnocení</param>
            /// <param name="superTip">Výstup ToolTipu, má význam pouze pokud výstupní hodnota je true: pokud je <paramref name="superTip"/> == null, pak máme zhasnout; pokud není null, pak se má rozsvítit. Pokud by byl shodný jako dosud, pak výstupem je false.</param>
            /// <returns></returns>
            internal DxToolTipChangeType TryGetSuperTip(bool isMouseHover, bool isTipVisible, out DxSuperToolTip superTip)
            {
                superTip = null;
                if (this.HasIDynamicClient)
                {
                    DxToolTipDynamicPrepareArgs args = this.LastPrepareArgs;
                    if (args is null) args = new DxToolTipDynamicPrepareArgs();          // args používám opakovaně, jen ho naplním aktuálními daty
                    args.MouseLocation = this.MouseLocation;
                    args.IsMouseHover = isMouseHover;
                    args.IsTipVisible = isTipVisible;
                    args.DxSuperTip = this.LastSuperTip;
                    args.ToolTipChange = (this.LastSuperTip is null ? DxToolTipChangeType.NoToolTip : DxToolTipChangeType.SameAsLastToolTip);

                    this.IDynamicClient.PrepareSuperTipForPoint(args);
                    this.LastSuperTip = args.DxSuperTip;                                 // Uložím si data pro příští volání, nulovat se budou při resetu
                    this.LastPrepareArgs = args;

                    superTip = args.DxSuperTip;
                    return args.ToolTipChange;
                }

                if (this.HasIClient)
                {
                    // Výměna a uložení nového globálního ToolTipu ze statického klienta:
                    var oldSuperTip = this.LastSuperTip;
                    var newSuperTip = this.IClient?.SuperTip;
                    superTip = newSuperTip;
                    this.LastSuperTip = newSuperTip;

                    // Druh změny:
                    bool oldExists = (oldSuperTip != null);
                    bool newExists = (newSuperTip != null);
                    if (oldExists && newExists)
                    {
                        if (DxSuperToolTip.IsEqualContent(oldSuperTip, newSuperTip))
                            return DxToolTipChangeType.SameAsLastToolTip;
                        return DxToolTipChangeType.NewToolTip;
                    }
                    if (oldExists && !newExists)
                        return DxToolTipChangeType.NoToolTip;
                    if (!oldExists && newExists)
                        return DxToolTipChangeType.NewToolTip;
                    return DxToolTipChangeType.None;
                }

                return DxToolTipChangeType.None;
            }

            /// <summary>
            /// Má control obsah pro SuperTip? Stejný způsob získání obsahu pro Super tip jako v <see cref="TryGetSuperTip"/>, ale neukládájí se žádné stavy.<br/>
            /// Metoda vrací:<br/>
            /// true = mám nějaké data pro SuperToolTip
            /// false = nemám žádná data pro SuperToolTip.
            /// </summary>
            /// <returns></returns>
            internal bool HasContentForSuperTip()
            {//RMC 0071608 08.01.2024 BROWSE2e - začlenění 2
                if (this.HasIDynamicClient)
                {
                    DxToolTipDynamicPrepareArgs args = new DxToolTipDynamicPrepareArgs();
                    args.MouseLocation = this.MouseLocation;
                    args.ToolTipChange = DxToolTipChangeType.NoToolTip;
                    this.IDynamicClient.PrepareSuperTipForPoint(args);
                    return args.ToolTipChange != DxToolTipChangeType.NoToolTip;
                }
                if (this.HasIClient)
                {
                    // Výměna a uložení nového globálního ToolTipu ze statického klienta:
                    return this.IClient?.SuperTip != null;
                }

                return false;
            }

            /// <summary>
            /// Je voláno při zhasínání ToolTipu, ať už jsou důvody jakékoli
            /// </summary>
            internal void Reset()
            {
                LastSuperTip = null;
                LastPrepareArgs = null;
                ResetLastPoint();
            }
            /// <summary>
            /// Nuluje poslední pozici myši
            /// </summary>
            internal void ResetLastPoint()
            {
                LastMouseLocation = null;
                LastMouseLocationTime = null;
            }
        }
        #endregion
        #region Client ToolTip: nalezení klienta, určení jeho konkrétního jeho ToolTipu
        /// <summary>
        /// Myš se pohybuje bez stisknutého tlačítka nad daným klientem.
        /// Můžeme čekat na její zastavení a rozsvítit Tooltip, nebo jej rozsvěcet okamžitě; anebo když ToolTip svítí, tak jej zhasnout...
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="e"></param>
        private void _ClientMouseMoveNone(ClientInfo clientInfo, MouseEventArgs e)
        {
            _ClientMouseMoveDetectClientChange(clientInfo, e);
            if (!IsHintShown)
                _ClientMouseMoveWaitShow();
            else
                _ClientMouseMoveWaitHide();
        }
        /// <summary>
        /// Kontroluje aktuálního klienta: pokud nějakého máme uloženého odminule, a nyní je klient pod myší jiný, a svítí nám ToolTip, tak jej zhasnu.
        /// Aktuálního klienta si uložím (do <see cref="__ActiveClientInfo"/>) a vložím do něj aktuální pozici myši.
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="e"></param>
        private void _ClientMouseMoveDetectClientChange(ClientInfo clientInfo, MouseEventArgs e)
        {
            // Pokud se myš pohybuje nad jiným Controlem než dříve, musíme starý tooltip zhasnout a zahodit:
            if (__ActiveClientInfo != null && !Object.ReferenceEquals(__ActiveClientInfo, clientInfo))
            {
                _HideTip(true);
                _ResetHoverInterval();
            }

            // Uložíme si aktuálního klienta a do něj pozici myši:
            __ActiveClientInfo = clientInfo;
            __ActiveClientInfo.MouseLocation = e.Location;
        }
        /// <summary>
        /// ToolTip nesvítí a nad klientem se pohybuje myš: čekáme na její zastavení nebo zpomalení (podle <see cref="HoverCurrentMiliseconds"/>) 
        /// a pak zajistíme rozsvícení ToolTipu.
        /// </summary>
        private void _ClientMouseMoveWaitShow()
        {
            var hoverMiliseconds = HoverCurrentMiliseconds;

            // Pokud nám běží časovač __HoverTimerGuid, a známe poslední pozici a čas myši, a nynější pozice a čas odpovídá malé rychlosti pohybu myši,
            //  a pokud čas 'hoverMiliseconds' je kladný, tak nebudeme resetovat Timer = jako by se myš nepohnula, ale stála na místě...:
            if (hoverMiliseconds > 1 && __HoverTimerGuid.HasValue && _ClientMouseMoveIsSlow()) return;

            if (hoverMiliseconds > 1)
                // Máme TooTip rozsvítit až po nějaké době od zastavení myši:
                // Toto volání Timeru (s předaným Guid __HoverTimerGuid) zajistí, že budeme zavoláni (metoda _ClientActivateTip) až po zadané době od posledního "načasování budíka".
                // Průběžné pohyby myši v kratším čase provedou "přenastavení toho samého budíka" na nový čas:
                __HoverTimerGuid = WatchTimer.CallMeAfter(_ClientMouseHoverTimerShowTip, hoverMiliseconds, false, __HoverTimerGuid);
            else
                // 0 = máme dát ToolTip ihned?
                _ClientMouseHoverShowTip();                   // Aktuálně jsme v eventu MouseMove, tedy v GUI threadu...
        }
        /// <summary>
        /// Zkus aktivovat ToolTip, uplynul čas čekání od posledního pohybu myši <see cref="HoverCurrentMiliseconds"/>, 
        /// a nyní jsme volání z threadu na pozadí z třídy <see cref="WatchTimer"/>.
        /// </summary>
        private void _ClientMouseHoverTimerShowTip()
        {
            __HoverTimerGuid = null;
            __ActiveClientInfo?.InvokeGui(_ClientMouseHoverShowTip);
        }
        /// <summary>
        /// Zkus aktivovat ToolTip, uplynul čas čekání od posledního pohybu myši <see cref="HoverCurrentMiliseconds"/> a jsme invokování do GUI threadu,
        /// anebo se má ToolTip rozsvítit okamžitě.
        /// </summary>
        private void _ClientMouseHoverShowTip()
        {
            if (!_CanShowTipOverActiveClient())
            {
                _HideTip(true);
                _RaiseToolTipDebugTextChanged($"ClientMouseHoverShowTip(superTip): cancel - ActiveClient is not valid");
                return;
            }
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return;

            var changeType = clientInfo.TryGetSuperTip(true, __IsHintShown, out DxSuperToolTip superTip);
            switch (changeType)
            {   // Myš se zastavila, možná rozsvítíme ToolTip?
                case DxToolTipChangeType.NewToolTip:
                    _ShowDxSuperTip(superTip);
                    break;
                case DxToolTipChangeType.SameAsLastToolTip:
                    break;
                case DxToolTipChangeType.NoToolTip:
                    _HideTip(false);
                    break;
            }
        }
        /// <summary>
        /// ToolTip svítí a pohybujeme se nad klientem (=máme event MouseMove).
        /// Zhasneme ToolTip? Nebo jej vyměníme? Nebo jej necháme beze změny?
        /// </summary>
        private void _ClientMouseMoveWaitHide()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return;
            var changeType = clientInfo.TryGetSuperTip(false, __IsHintShown, out DxSuperToolTip superTip);
            switch (changeType)
            {   // ToolTip svítí, možná jej necháme, anebo jej zhasneme?
                case DxToolTipChangeType.NewToolTip:
                    _HideTip(true);                        // Skrýt ToolTip včetně resetu = zapomenout na aktuální pozici a prvek a tooltip, abychom po zhasnutí a po konci pohybu myši detekovali NewToolTip a nikoli SameAsLastToolTip...
                    _ClientMouseMoveWaitShow();            // Tady jsme v eventu MouseMove: nastartujeme HoverTimer a po jeho uplynutí vyhodnotíme od nuly pozici myši a prvek na této pozici.
                    break;
                case DxToolTipChangeType.SameAsLastToolTip:
                    break;
                case DxToolTipChangeType.NoToolTip:
                    _HideTip(true);
                    _ClientMouseMoveWaitShow();            // Tady jsme v eventu MouseMove, tak se sluší nastartovat HoverTimer
                    break;
            }
            clientInfo.ResetLastPoint();
        }

        private void _ClientMouseDown(ClientInfo clientInfo, MouseEventArgs e)
        {
            _HideTip(false);
            _ResetHoverInterval();
        }
        private void _ClientMouseLeave()
        {
            _RaiseToolTipDebugTextChanged($"ClientMouseLeave(): ActiveClient = {__ActiveClientInfo}");
            _ResetHoverInterval();
            _HideTip(true);
        }
        private void _ClientLeave()
        {
            _RaiseToolTipDebugTextChanged($"ClientLeave(): ActiveClient = {__ActiveClientInfo}");
            _ResetHoverInterval();
            _HideTip(true);
        }
        private ClientInfo __ActiveClientInfo;
        private Guid? __HoverTimerGuid;
        /// <summary>
        /// Metoda vrátí true, pokud nynější pozice myši v aktuálním klientu se neliší příliš mnoho od předchozí pozice v tomtéž klientu.
        /// Pokud vrátí true, je to stejné jako by myš stála na místě. Pokud vrátí false, pohybuje se docela rychle.
        /// Metoda si udržuje časoprostorové povědomí v properties klienta <see cref="ClientInfo.LastMouseLocation"/> a <see cref="ClientInfo.LastMouseLocationTime"/>.
        /// </summary>
        /// <returns></returns>
        private bool _ClientMouseMoveIsSlow()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return false;

            // Najdeme předchozí časoprostorové souřadnice, a načteme i aktuální:
            var lastPoint = clientInfo.LastMouseLocation;
            var lastTime = clientInfo.LastMouseLocationTime;
            var currentPoint = clientInfo.MouseLocation;
            var currentTime = __StopWatch.ElapsedTicks;
            // Aktuální uložíme do předchozích (nulují se až v ClientInfo.ResetLastPoint()):
            clientInfo.LastMouseLocation = currentPoint;
            clientInfo.LastMouseLocationTime = currentTime;

            // Nemáme předešlé souřadnice => nemůže sejednat o malý pohyb (jde asi o první událost MouseMove):
            if (!(lastPoint.HasValue && lastTime.HasValue)) return false;

            // Kolik pixelů za sekundu máme pohybu:
            int dx = lastPoint.Value.X - currentPoint.X;
            if (dx < 0) dx = -dx;
            int dy = lastPoint.Value.Y - currentPoint.Y;
            if (dy < 0) dy = -dy;
            double pixelDistance = (dx > dy ? dx : dy);                        // Kladná hodnota rozdílu souřadnic, ta větší ze směru X | Y;
            double seconds = ((double)(currentTime - lastTime.Value)) / __StopWatchFrequency;    // Čas v sekundách mezi minulým a současným měřením polohy myši
            if (pixelDistance <= 0d || seconds <= 0d) return true;             // Pokud jsme nedetekovali pohyb nebo čas, je to jako by se myš nepohnula.

            double pixelPerSeconds = pixelDistance / seconds;
            bool isSlowMotion = (pixelPerSeconds <= SlowMouseMovePps);

            if (isSlowMotion)
                _RaiseToolTipDebugTextChanged($"Is..slow..motion: {pixelPerSeconds:F3} pixel/seconds    <=   {SlowMouseMovePps}");
            else
                _RaiseToolTipDebugTextChanged($"IsFASTMotion: {pixelPerSeconds:F3} pixel/seconds    >   {SlowMouseMovePps}");

            return isSlowMotion;
        }
        /// <summary>
        /// Počet pixelů za sekundu v pohybu myši, který se ještě považuje za pomalý pohyb a neresetuje časovač odloženého startu ToolTipu, výchozí hodnota.
        /// </summary>
        private const double DEFAULT_SILENT_PIXEL_PER_SECONDS = 600d;
        /// <summary>
        /// Provede inicializaci hodin reálného času pro měření rychlosti pohybu myši
        /// </summary>
        private void _InitializeStopWatch()
        {
            __StopWatchFrequency = Stopwatch.Frequency;
            __StopWatch = new Stopwatch();
            __StopWatch.Start();
        }
        private System.Diagnostics.Stopwatch __StopWatch;
        private double __StopWatchFrequency;
        #endregion
        #region Fyzický ToolTip
        /// <summary>
        /// Skrýt ToolTip
        /// </summary>
        public void HideTip()
        {
            _HideTip(true);
        }
        /// <summary>
        /// Zobrazit ToolTip
        /// </summary>
        /// <param name="superTip"></param>
        private void _ShowDxSuperTip(DxSuperToolTip superTip)
        {
            if (superTip is null)
            {
                _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): cancel - superTip is null");
                return;
            }

            if (!_CanShowTipOverActiveClient())
            {
                _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): cancel - ActiveClient is not valid");
                return;
            }

            var args = new ToolTipControllerShowEventArgs();
            args.ToolTipType = ToolTipType.SuperTip;
            args.SuperTip = superTip;
            args.ToolTipAnchor = this.ToolTipAnchor;
            args.ToolTipLocation = this.ToolTipLocation;
            args.ToolTipIndent = this.ToolTipIndent;
            args.Show = true;
            args.ShowBeak = this.ShowBeak;

            _RaiseToolTipDebugTextChanged($"ShowDxSuperTip(superTip): ShowHint");
            this.ShowHint(args);

            __IsHintShown = true;
            __ActiveClientInfoHasShowAnyToolTip = true;

            _StartHideTimer(superTip);
        }
        /// <summary>
        /// Vrátí true, pokud je možno zobrazit klientský Tooltip = máme platnou instanci aktuálního klienta, a myš se nachází v jeho prostoru.
        /// </summary>
        /// <returns></returns>
        private bool _CanShowTipOverActiveClient()
        {
            var clientInfo = __ActiveClientInfo;
            if (clientInfo is null) return false;
            var mousePoint = Control.MousePosition;
            var screenBounds = clientInfo.Client.RectangleToScreen(clientInfo.Client.ClientRectangle);
            return screenBounds.Contains(mousePoint);
        }
        /// <summary>
        /// Skrýt ToolTip, a pokud je dán parametr <paramref name="reset"/> = true, pak proveď i Reset informace o klientu.
        /// </summary>
        /// <param name="reset"></param>
        private void _HideTip(bool reset)
        {
            WatchTimer.RemoveRef(ref __HoverTimerGuid);
            WatchTimer.RemoveRef(ref __HideTimerGuid);
            if (reset)
            {   // Když myš opouští prostor daného klienta, tak na něj mohu s klidem zapomenout:
                if (__ActiveClientInfo != null) __ActiveClientInfo.Reset();
                __ActiveClientInfo = null;
            }
            if (__IsHintShown)
            {
                this.HideHint();
                __IsHintShown = false;
            }
        }
        /// <summary>
        /// ToolTip byl zobrazen a měl by nyní svítit?
        /// </summary>
        public bool IsHintShown { get { return __IsHintShown; } }
        private bool __IsHintShown;
        /// <summary>
        /// Počet milisekund, za které se rozsvítí ToolTip od zastavení myši, v aktuálním stavu.
        /// Obsahuje <see cref="HoverFirstMiliseconds"/> nebo <see cref="HoverNextMiliseconds"/>, podle toho, zda pro aktuální Control (Client) už byl / nebyl rozsvícen ToolTip.
        /// </summary>
        protected int HoverCurrentMiliseconds { get { return (!__ActiveClientInfoHasShowAnyToolTip ? HoverFirstMiliseconds : HoverNextMiliseconds); } }
        /// <summary>
        /// Resetuje příznak prvního / následujícího ToolTipu, provádí se tehdy, když chceme mít příští interval <see cref="HoverCurrentMiliseconds"/> jako počáteční = <see cref="HoverFirstMiliseconds"/>.
        /// </summary>
        private void _ResetHoverInterval()
        {
            __ActiveClientInfoHasShowAnyToolTip = false;
        }
        /// <summary>
        /// Příznak, že aktuální klient neměl (false) / měl (true) už zobrazen nějaký ToolTip.
        /// Nastaví se na true v <see cref="_ShowDxSuperTip(DxSuperToolTip)"/> a zůstává true i po zhasnutí ToolTipu,
        /// nuluje se na false při opuštění klienta (nebo MouseDown) v metodě <see cref="_ResetHoverInterval"/>,
        /// používá se v <see cref="HoverCurrentMiliseconds"/> pro určení času pro aktuální interval čekání na MouseHover, 
        /// tam se zvolí buď <see cref="HoverFirstMiliseconds"/> nebo <see cref="HoverNextMiliseconds"/>.
        /// </summary>
        private bool __ActiveClientInfoHasShowAnyToolTip;
        /// <summary>
        /// Nastartuj časovač pro automatické skrytí ToolTipu
        /// </summary>
        private void _StartHideTimer(DxSuperToolTip superTip)
        {
            var autoHideMiliseconds = this.AutoHideMiliseconds;
            if (autoHideMiliseconds <= 0) return;

            if (AutoHideAdaptive)
                _ModifyAutoHideAdaptive(ref autoHideMiliseconds, superTip);

            __HideTimerGuid = WatchTimer.CallMeAfter(_AutoHideToolTipTimer, autoHideMiliseconds, false, __HideTimerGuid);
        }
        /// <summary>
        /// Upraví čas <paramref name="autoHideMiliseconds"/> podle délky textu v dodaném ToolTipu
        /// </summary>
        /// <param name="autoHideMiliseconds"></param>
        /// <param name="superTip"></param>
        private void _ModifyAutoHideAdaptive(ref int autoHideMiliseconds, DxSuperToolTip superTip)
        {
            if (superTip is null) return;
            int textLength = (superTip.Text ?? "").Trim().Length;
            if (textLength <= 120) return;
            float ratio = ((float)textLength / 120f);
            if (ratio > 5f) ratio = 5f;
            autoHideMiliseconds = (int)(ratio * (float)autoHideMiliseconds);
        }
        /// <summary>
        /// Uplynul patřičný čas pro schování ToolTipu
        /// </summary>
        private void _AutoHideToolTipTimer()
        {
            var clienInfo = __ActiveClientInfo;
            if (clienInfo != null)
                clienInfo.InvokeGui(_AutoHideToolTip);
            else
                _AutoHideToolTip();
        }
        /// <summary>
        /// Provede automatické schování ToolTipu po čase daním Timerem <see cref="AutoHideMiliseconds"/>.
        /// </summary>
        private void _AutoHideToolTip()
        {
            WatchTimer.RemoveRef(ref __HideTimerGuid);
            _HideTip(false);           // false = bez resetu => budeme si pamatovat, nad kterým prvekm stojím. Pak se pro ten prvek neprovede reaktivace ToolTipu, protože z klienta přijde ChangeType = SameAsLast, a to tooltip nerozsvítíme...
        }
        /// <summary>
        /// ID Timeru, který řídí časové skrytí ToolTipu
        /// </summary>
        private Guid? __HideTimerGuid;
        #endregion
        #region Eventy Tooltipu
        /// <summary>
        /// Vyvolej události <see cref="ToolTipDebugTextChanged"/>;
        /// </summary>
        /// <param name="eventName"></param>
        private void _RaiseToolTipDebugTextChanged(string eventName)
        {
            if (DxComponent.IsDebuggerActive)
            {
                DxToolTipArgs args = new DxToolTipArgs(eventName);
                OnToolTipDebugTextChanged(args);
                ToolTipDebugTextChanged?.Invoke(this, args);
            }
        }
        /// <summary>
        /// ToolTip má událost.
        /// Používá se pouze pro výpisy debugovacích informací do logu společného s klientským controlem. Běžně netřeba.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnToolTipDebugTextChanged(DxToolTipArgs args) { }
        /// <summary>
        /// ToolTip má událost.
        /// Používá se pouze pro výpisy debugovacích informací do logu společného s klientským controlem. Běžně netřeba.
        /// </summary>
        public event DxToolTipHandler ToolTipDebugTextChanged;
        #endregion
    }
    /// <summary>
    /// Data o události v ToolTipu
    /// </summary>
    public class DxToolTipArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="eventName"></param>
        public DxToolTipArgs(string eventName)
        {
            EventName = eventName;
        }
        /// <summary>
        /// Jméno události
        /// </summary>
        public string EventName { get; private set; }
    }
    /// <summary>
    /// Eventhandler události v ToolTipu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void DxToolTipHandler(object sender, DxToolTipArgs args);
    /// <summary>
    /// Interface pro Control, který zobrazuje jeden konstantní ToolTip pro celou svoji plochu
    /// </summary>
    public interface IDxToolTipClient
    {
        /// <summary>
        /// ToolTip pro Control
        /// </summary>
        DxSuperToolTip SuperTip { get; }
    }
    /// <summary>
    /// Interface pro Control, který chce určovat ToolTip dynamicky podle konkrétní pozice myši na controlu
    /// </summary>
    public interface IDxToolTipDynamicClient
    {
        /// <summary>
        /// Zde control určí, jaký ToolTip má být pro danou pozici myši zobrazen
        /// </summary>
        /// <param name="args"></param>
        void PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs args);
    }
    /// <summary>
    /// Data pro událost <see cref="IDxToolTipDynamicClient.PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs)"/>
    /// </summary>
    public class DxToolTipDynamicPrepareArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxToolTipDynamicPrepareArgs()
        {
            ToolTipChange = DxToolTipChangeType.None;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mouseLocation"></param>
        /// <param name="dxSuperTip"></param>
        public DxToolTipDynamicPrepareArgs(Point mouseLocation, DxSuperToolTip dxSuperTip)
        {
            MouseLocation = mouseLocation;
            DxSuperTip = dxSuperTip;
            ToolTipChange = (dxSuperTip is null ? DxToolTipChangeType.NoToolTip : DxToolTipChangeType.SameAsLastToolTip);
        }
        /// <summary>
        /// Aktuální souřadnice myši v kordinátech kontrolu, pochází z eventu MouseMove.
        /// </summary>
        public Point MouseLocation { get; set; }
        /// <summary>
        /// Jsme voláni po zastavení myši na prvku (Hover = true) anebo při jejím pohybu (false)
        /// </summary>
        public bool IsMouseHover { get; set; }
        /// <summary>
        /// ToolTip je nyní viditelný?
        /// </summary>
        public bool IsTipVisible { get; set; }
        /// <summary>
        /// SuperTip: na vstupu je ten, který byl vygenerován nebo odsouhlasen posledně, 
        /// na výstupu z metody <see cref="IDxToolTipDynamicClient.PrepareSuperTipForPoint(DxToolTipDynamicPrepareArgs)"/> může být nově připravený.
        /// </summary>
        public DxSuperToolTip DxSuperTip { get; set; }
        /// <summary>
        /// Typ akce
        /// </summary>
        public DxToolTipChangeType ToolTipChange { get; set; }
    }
    /// <summary>
    /// Informace o nalezeném tooltipu v klientu typu <see cref="IDxToolTipDynamicClient"/>
    /// </summary>
    public enum DxToolTipChangeType
    {
        /// <summary>
        /// Neurčeno... Neřešit ToolTip
        /// </summary>
        None,
        /// <summary>
        /// Pro danou pozici nemá být tooltip, zhasni dosavadní pokud je zobrazen.
        /// </summary>
        NoToolTip,
        /// <summary>
        /// Pro danou pozici je tooltip nový, jiný než dosud.
        /// V tomto případě se aplikuje zhasnutí a čekání po patřičnou dobu, než se zobrazí nový ToolTip.
        /// </summary>
        NewToolTip,
        /// <summary>
        /// Pro danou pozici je stále stejný tooltip jako minule, nech jej svítit.
        /// </summary>
        SameAsLastToolTip
    }
    #endregion
    #region DxSuperToolTip
    /// <summary>
    /// SuperToolTip s přímým přístupem do standardních textů v ToolTipu.
    /// Obsahuje (v plné konfiguraci) Title + Separator + Text.
    /// Prvky jsou automaticky řízeny podle přítomnosti titulku a textu.
    /// </summary>
    public class DxSuperToolTip : DevExpress.Utils.SuperToolTip
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSuperToolTip()
            : base()
        {
            __TitleText = null;
            __TitleIcon = null;
            __TitleItem = null;

            __SeparatorItem = null;

            __TextText = null;
            __ToolTipAllowHtmlText = null;
            __TextItem = null;

            __AcceptTitleOnlyAsValid = false;
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="toolTipItem"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(IToolTipItem toolTipItem)
        {
            if (toolTipItem == null) return null;
            return CreateDxSuperTip(toolTipItem.ToolTipTitle, toolTipItem.ToolTipText, null, toolTipItem.ToolTipIcon, toolTipItem.ToolTipAllowHtml);
        }
        /// <summary>
        /// Vytvoří a vrátí SuperTooltip
        /// </summary>
        /// <param name="textItem"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(ITextItem textItem)
        {
            if (textItem == null) return null;
            return CreateDxSuperTip(textItem.ToolTipTitle, textItem.ToolTipText, textItem.Text, textItem.ToolTipIcon, textItem.ToolTipAllowHtml);
        }
        /// <summary>
        /// Vytvoří a vrátí standardní SuperToolTip pro daný titulek a text.
        /// Pokud nebude zadán text <paramref name="text"/>, ani titulek (<paramref name="title"/> nebo <paramref name="defaultTitle"/>), pak vrátí null.
        /// Pokud je zadán text v <paramref name="defaultTitle"/>, a k němu je dán jen jeden z <paramref name="title"/> anebo <paramref name="text"/>, a ten je shodný,
        /// pak se ToolTip negeneruje = obsahoval by totéž, co už je uvedeno v prvku.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="text"></param>
        /// <param name="defaultTitle"></param>
        /// <param name="toolTipIcon"></param>
        /// <param name="toolTipAllowHtml"></param>
        /// <returns></returns>
        public static DxSuperToolTip CreateDxSuperTip(string title, string text, string defaultTitle = null, string toolTipIcon = null, bool? toolTipAllowHtml = null)
        {
            if (!DxComponent.PrepareToolTipTexts(title, text, defaultTitle, out string toolTipTitle, out string toolTipText)) return null;

            var superTip = new DxSuperToolTip();
            superTip.LoadValues(toolTipTitle, toolTipText, toolTipIcon, toolTipAllowHtml);

            return superTip;
        }
        /// <summary>
        /// Do target instance přenese všechna data ze source instance
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void Synchronize(DxSuperToolTip source, DxSuperToolTip target)
        {
            if (source is null || target is null) return;
            target.__TitleText = source.__TitleText;
            target.__TitleIcon = source.__TitleIcon;
            target.__TextText = source.__TextText;
            target.__ToolTipAllowHtmlText = source.__ToolTipAllowHtmlText;
            target._RefreshContent();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Title={Title}; Text={Text}";
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public override void Dispose()
        {
            _CreateComponents(false, false, false);
            base.Dispose();
        }
        private string __TitleText;
        private string __TitleIcon;
        private ToolTipTitleItem __TitleItem;

        private ToolTipSeparatorItem __SeparatorItem;

        private string __TextText;
        private bool? __ToolTipAllowHtmlText;
        private ToolTipItem __TextItem;

        private bool __AcceptTitleOnlyAsValid;
        /// <summary>
        /// Hodnoty z proměnných vepíše do objektu SuperTip = vytvoří/zruš/nastaví itemy.
        /// </summary>
        private void _RefreshContent()
        {
            bool needTitleText = (!String.IsNullOrEmpty(__TitleText));
            bool needTitleIcon = (!String.IsNullOrEmpty(__TitleIcon));
            bool needTitle = (needTitleText || needTitleIcon);
            bool needText = (!String.IsNullOrEmpty(__TextText));
            bool needSeparator = (needTitle && needText);

            bool hasTitle = (__TitleItem != null);
            bool hasSeparator = (__SeparatorItem != null);
            bool hasText = (__TextItem != null);

            bool isCreated = (needTitle == hasTitle && needSeparator == hasSeparator && needText == hasText);
            if (!isCreated)
                _CreateComponents(needTitle, needSeparator, needText);

            if (needTitleText)
            {
                __TitleItem.Text = __TitleText;
            }

            if (needTitleIcon)
            {
                if (__TitleItem.ImageOptions.Images is null) __TitleItem.ImageOptions.Images = DxComponent.GetBitmapImageList(ResourceImageSizeType.Large);
                __TitleItem.ImageOptions.ImageToTextDistance = 12;
                __TitleItem.ImageOptions.ImageIndex = DxComponent.GetBitmapImageIndex(__TitleIcon, ResourceImageSizeType.Large);
            }

            if (needText)
            {
                __TextItem.Text = __TextText;
                __TextItem.AllowHtmlText = DxComponent.ConvertBool(DxComponent.AllowHtmlText(__TextText, __ToolTipAllowHtmlText));
            }
        }
        /// <summary>
        /// Vytvoří požadované komponenty
        /// </summary>
        /// <param name="createTitle"></param>
        /// <param name="createSeparator"></param>
        /// <param name="createText"></param>
        private void _CreateComponents(bool createTitle, bool createSeparator, bool createText)
        {
            this.__TitleItem?.Dispose();
            this.__TitleItem = null;
            this.__SeparatorItem?.Dispose();
            this.__SeparatorItem = null;
            this.__TextItem?.Dispose();
            this.__TextItem = null;

            this.Items.Clear();

            if (createTitle) this.__TitleItem = this.Items.AddTitle("");
            if (createSeparator) this.__SeparatorItem = this.Items.AddSeparator();
            if (createText) this.__TextItem = this.Items.Add("");
        }

        /// <summary>
        /// Text titulku
        /// </summary>
        public string Title { get { return __TitleText; } set { __TitleText = value; _RefreshContent(); } }
        /// <summary>
        /// Jméno ikony
        /// </summary>
        public string IconName { get { return __TitleIcon; } set { __TitleIcon = value; _RefreshContent(); } }
        /// <summary>
        /// Text tooltipu
        /// </summary>
        public string Text { get { return __TextText; } set { __TextText = value; _RefreshContent(); } }
        /// <summary>
        /// Text tooltipu může obsahovat HTML kódy
        /// </summary>
        public bool? ToolTipAllowHtmlText { get { return __ToolTipAllowHtmlText; } set { __ToolTipAllowHtmlText = value; _RefreshContent(); } }
        /// <summary>
        /// Libovolná data klienta, typicky objekt pod myší.
        /// Instance je udržována v Controlleru.
        /// </summary>
        public object ClientData { get; set; }

        /// <summary>
        /// Řídí určení hodnoty <see cref="IsValid"/> (= ToolTip je platný) :
        /// <para/>
        /// a) Pokud <see cref="AcceptTitleOnlyAsValid"/> = true: pak <see cref="IsValid"/> je true tehdy, když je vyplněn i jen titulek.
        /// Tedy, když je zadán <see cref="Title"/> a nemusí být zadán <see cref="Text"/> = tehdy se zobrazuje ToolTip obsahující pouze titulek
        /// <para/>
        /// b) Pokud <see cref="AcceptTitleOnlyAsValid"/> = false: pak <see cref="IsValid"/> je true jen tehdy, když je vyplněn text (bez ohledu na vyplnění titulku).
        /// Tedy, když je zadán <see cref="Text"/> (a může i nemusí být zadán <see cref="Title"/>) = tedy zobrazuje se ToolTip obsahující [titulek když je], a obsahuje Text.
        /// <para/>
        /// Výchozí je false = pro platný ToolTip je třeba zadat jeho <see cref="Text"/>, nestačí zadat pouze <see cref="Title"/>.
        /// </summary>
        public bool AcceptTitleOnlyAsValid { get { return __AcceptTitleOnlyAsValid; } set { __AcceptTitleOnlyAsValid = value; } }
        /// <summary>
        /// Obsahuje true v případě, že ToolTip má text alespoň v titulku <see cref="Title"/> anebo v textu <see cref="Text"/>, pak má význam aby byl zobrazen.
        /// Pokud texty nemá, neměl by být zobrazován.
        /// </summary>
        public bool IsValid
        {
            get
            {
                bool acceptTitleOnlyAsValid = this.AcceptTitleOnlyAsValid;
                bool hasTitle = !String.IsNullOrEmpty(this.Title);
                bool hasText = !String.IsNullOrEmpty(this.Text);
                return ((acceptTitleOnlyAsValid && hasTitle) || hasText);
            }
        }
        /// <summary>
        /// Vrátí true, pokud dva dodané objekty mají shodná data (tedy true i kdyby to byly dvě různé instance)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsEqualContent(DxSuperToolTip a, DxSuperToolTip b)
        {
            bool an = a is null;
            bool bn = b is null;
            if (an && bn) return true;           // Oba jsou NULL
            if (an || bn) return false;          // Jen jeden je NULL
            return (String.Equals(a.__TitleText, b.__TitleText)
                 && String.Equals(a.__TitleIcon, b.__TitleIcon)
                 && String.Equals(a.__TextText, b.__TextText)
                 && (a.__ToolTipAllowHtmlText == b.__ToolTipAllowHtmlText));
        }
        /// <summary>
        /// Nuluje svůj obsah
        /// </summary>
        public void ClearValues()
        {
            __TitleText = null;
            __TitleIcon = null;

            __TextText = null;
            __ToolTipAllowHtmlText = null;

            _RefreshContent();
        }
        /// <summary>
        /// Naplní do sebe hodnoty z dané definice
        /// </summary>
        /// <param name="toolTipItem"></param>
        public void LoadValues(IToolTipItem toolTipItem)
        {
            if (toolTipItem == null)
                ClearValues();
            else
                LoadValues(toolTipItem.ToolTipTitle, toolTipItem.ToolTipText, toolTipItem.ToolTipIcon, toolTipItem.ToolTipAllowHtml);
        }
        /// <summary>
        /// Naplní do sebe hodnoty z dané definice
        /// </summary>
        /// <param name="textItem"></param>
        public void LoadValues(ITextItem textItem)
        {
            if (textItem == null)
                ClearValues();
            else
            {
                DxComponent.PrepareToolTipTexts(textItem.ToolTipTitle, textItem.ToolTipText, textItem.Text, out string toolTipTitle, out string toolTipText);
                LoadValues(toolTipTitle, toolTipText, textItem.ToolTipIcon, textItem.ToolTipAllowHtml);
            }
        }
        /// <summary>
        /// Naplní do sebe hodnoty z daných parametrů
        /// </summary>
        /// <param name="toolTipTitle"></param>
        /// <param name="toolTipText"></param>
        /// <param name="toolTipIcon"></param>
        /// <param name="toolTipAllowHtml"></param>
        public void LoadValues(string toolTipTitle, string toolTipText, string toolTipIcon = null, bool? toolTipAllowHtml = null)
        {
            if (String.IsNullOrEmpty(toolTipTitle) && String.IsNullOrEmpty(toolTipText))
                ClearValues();
            else
            {
                __TitleText = toolTipTitle;
                __TitleIcon = toolTipIcon;

                __TextText = toolTipText;
                __ToolTipAllowHtmlText = toolTipAllowHtml;

                _RefreshContent();
            }
        }
    }
    #endregion
    #region DxStatus - prvky
    /// <summary>
    /// StatusBar : Statický prvek = Label
    /// </summary>
    public class DxBarStaticItem : DevExpress.XtraBars.BarStaticItem
    { }
    /// <summary>
    /// StatusBar : Button
    /// </summary>
    public class DxBarButtonItem : DevExpress.XtraBars.BarButtonItem, IBarItemCustomDrawing
    {
        #region IBarItemCustomDrawing
        void IBarItemCustomDrawing.CustomDraw(DevExpress.XtraBars.BarItemCustomDrawEventArgs e)
        {
            DxBarButtonItem.CustomDrawButton(this, e);

        }
        internal static void CustomDrawButton(DevExpress.XtraBars.BarBaseButtonItem button, DevExpress.XtraBars.BarItemCustomDrawEventArgs e)
        {
            // Pokud je button v nějakém jiném stabu než klidovém, pak do kreslení nezasahujeme:
            if (e.State != DevExpress.XtraBars.ViewInfo.BarLinkState.Normal) return;

            // Pokud border je Default nebo NoBorder, pak nic specifického nekreslíme:
            var border = button.Border;
            if (border == BorderStyles.NoBorder || border == BorderStyles.Default) return;

            // Vykreslíme jinou barvu pozadí a přes ni potom ikonu a text:
            e.DrawBackground();

            Color backColor = Color.FromArgb(64, 96, 96, 160);
            e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(backColor), e.Bounds);

            //Color? backColor = DxComponent.SkinColorSet.NamedControl ?? DxComponent.SkinColorSet.NamedHotTrackedColor;
            //if (backColor.HasValue) e.Graphics.FillRectangle(DxComponent.PaintGetSolidBrush(Color.FromArgb(64, backColor.Value)), e.Bounds);

            e.DrawBorder();
            e.DrawGlyph();
            e.DrawText();
            e.Handled = true;
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="defaultTitle"></param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    /// <summary>
    /// StatusBar : CheckItem
    /// </summary>
    public class DxBarCheckItem : DevExpress.XtraBars.BarCheckItem, IBarItemCustomDrawing
    {
        #region IBarItemCustomDrawing
        void IBarItemCustomDrawing.CustomDraw(DevExpress.XtraBars.BarItemCustomDrawEventArgs e)
        {
            DxBarButtonItem.CustomDrawButton(this, e);
        }
        #endregion
        #region ToolTip
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        public void SetToolTip(string text) { this.SuperTip = DxComponent.CreateDxSuperTip(null, text); }
        /// <summary>
        /// Nastaví daný text a titulek pro tooltip
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="defaultTitle"></param>
        public void SetToolTip(string title, string text, string defaultTitle = null) { this.SuperTip = DxComponent.CreateDxSuperTip(title, text, defaultTitle); }
        #endregion
    }
    /// <summary>
    /// Umožní prvku jeho self-drawing
    /// </summary>
    public interface IBarItemCustomDrawing
    {
        /// <summary>
        /// Umožní prvku jeho self-drawing.
        /// </summary>
        /// <param name="e"></param>
        void CustomDraw(DevExpress.XtraBars.BarItemCustomDrawEventArgs e);
    }
    #endregion
    #region DxImagePickerListBox
    /// <summary>
    /// ListBox nabízející DevExpress Resources
    /// </summary>
    public class DxImagePickerListBox : DxPanelControl
    {
        #region Konstrukce a vnitřní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxImagePickerListBox()
        {
            this.Initialize();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        protected void Initialize()
        {
            _ClipboardCopyIndex = 0;

            _FilterClearButton = DxComponent.CreateDxMiniButton(0, 0, 20, 20, this, _FilterClearButtonClick,
                resourceName: ImageName.DxImagePickerClearFilter,
                toolTipTitle: "Zrušit filtr", toolTipText: "Zruší filtr, budou zobrazeny všechny dostupné zdroje.");
            _FilterClearButton.MouseEnter += _AnyControlEnter;
            _FilterClearButton.Enter += _AnyControlEnter;

            _FilterText = DxComponent.CreateDxTextEdit(27, 0, 200, this,
                toolTipTitle: "Filtr Resources", toolTipText: "Vepište část názvu zdroje.\r\nLze použít filtrační znaky * a ?.\r\nLze zadat víc filtrů, oddělených středníkem nebo čárkou.\r\n\r\nNapříklad: 'add' zobrazí všechny položky obsahující 'add',\r\n'*close*.svg' zobrazí něco obsahující 'close' s příponou '.svg',\r\n'*close*.svg;*delete*' zobrazí prvky close nebo delete");
            _FilterText.MouseEnter += _AnyControlEnter;
            _FilterText.Enter += _AnyControlEnter;
            _FilterText.KeyUp += _FilterText_KeyUp;

            _ListCopyButton = DxComponent.CreateDxMiniButton(230, 0, 20, 20, this, _ListCopyButtonClick,
                resourceName: ImageName.DxImagePickerClipboarCopy,
                hotResourceName: ImageName.DxImagePickerClipboarCopyHot,
                toolTipTitle: "Zkopírovat", toolTipText: "Označené řádky v seznamu zdrojů vloží do schránky, jako Ctrl+C.");
            _ListCopyButton.MouseEnter += _AnyControlEnter;
            _ListCopyButton.Enter += _AnyControlEnter;

            _ListBox = DxComponent.CreateDxListBox(DockStyle.None, parent: this, selectionMode: SelectionMode.MultiExtended, itemHeight: 32,
                toolTipTitle: "Seznam Resources", toolTipText: "Označte jeden nebo více řádků, klávesou Ctrl+C zkopírujete názvy Resources jako kód C#.");
            _ListBox.ItemSizeType = ResourceImageSizeType.None;
            _ListBox.MouseEnter += _AnyControlEnter;
            _ListBox.Enter += _AnyControlEnter;
            _ListBox.KeyDown += _ListBox_KeyDown;
            _ListBox.PaintList += _ListBox_PaintList;
            _ListBox.SelectedIndexChanged += _ListBox_SelectedIndexChanged;

            _ResourceNames = DxComponent.GetResourceNames(withApplication: false, withDevExpress: true);
            _ResourceFilter = "";
            _StatusText = "";

            _FillListByFilter();
        }
        /// <summary>
        /// Po vstupu do jakéhokoli controlu nastavím výchozí status text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _AnyControlEnter(object sender, EventArgs e)
        {
            _ResetStatusText();
        }
        /// <summary>
        /// Po změně velikosti provedu rozmístění prvků
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            this.DoLayout();
        }
        /// <summary>
        /// Správně rozmístí prvky
        /// </summary>
        protected void DoLayout()
        {
            var size = this.ClientSize;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;
            int sx = 2;
            int sy = 2;
            int r = size.Width - mx;
            int w = size.Width - mx - mx;
            int h = size.Height - my - my;
            int txs = _FilterText.Height;
            int bts = txs + sx;

            _FilterClearButton.Bounds = new Rectangle(x, y, txs, txs);
            _FilterText.Bounds = new Rectangle(x + bts, y, w - bts - bts, txs);
            _ListCopyButton.Bounds = new Rectangle(r - txs, y, txs, txs);

            y = _FilterText.Bounds.Bottom + sy;
            _ListBox.Bounds = new Rectangle(x, y, w, h - y);
        }
        /// <summary>
        /// Clear filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterClearButtonClick(object sender, EventArgs e)
        {
            _ResourceFilter = "";
            _FillListByFilter();
            _FilterText.Focus();
        }
        /// <summary>
        /// FilterText: Po klávese ve filtru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FilterText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                _ListBox.Focus();
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End || e.KeyCode == Keys.Up /* || e.KeyCode == Keys.Down */ || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape)
            { }
            else if (e.Modifiers == Keys.Control)
            { }
            else
                _FillListByFilter();
        }
        /// <summary>
        /// Copy selected items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListCopyButtonClick(object sender, EventArgs e)
        {
            _FilterText.Focus();
            _DoCopyClipboard();
        }
        /// <summary>
        /// ListBox: Obsluha kláves: detekce Ctrl+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C)) _DoCopyClipboard();
        }
        /// <summary>
        /// Po změně řádku v ListBoxu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItems = _ListBox.SelectedMenuItemsExt;
            StatusText = "Označeny řádky: " + selectedItems.Length.ToString();
        }
        /// <summary>
        /// Vykreslí obrázky do ListBoxu do viditelných prvků
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ListBox_PaintList(object sender, PaintEventArgs e)
        {
            try { _ListBox_PaintListIcons(e); }
            catch { }
        }
        private void _ListBox_PaintListIcons(PaintEventArgs e)
        {
            var visibleItems = _ListBox.VisibleMenuItemsExt;
            foreach (var visibleItem in visibleItems)
            {
                string resourceName = visibleItem.Item2?.Text;
                Rectangle itemBounds = visibleItem.Item3;
                using (var image = DxComponent.CreateBitmapImage(resourceName, optimalSvgSize: new Size(32, 32)))
                {
                    if (image != null)
                    {
                        Size size = image.Size;
                        Point imagePoint = new Point((itemBounds.Right - 24 - size.Width / 2), itemBounds.Top + ((itemBounds.Height - size.Height) / 2));
                        Rectangle imageBounds = new Rectangle(imagePoint, size);
                        e.Graphics.DrawImage(image, imageBounds);
                    }
                }
            }
        }
        DxSimpleButton _FilterClearButton;
        DxTextEdit _FilterText;
        DxSimpleButton _ListCopyButton;
        DxListBoxControl _ListBox;
        #endregion
        #region Seznam resources - získání, filtrování, tvorba Image, CopyToClipboard
        /// <summary>
        /// Do seznamu ListBox vloží zdroje odpovídající aktuálnímu filtru
        /// </summary>
        private void _FillListByFilter()
        {
            string[] resources = _GetResourcesByFilter();
            IMenuItem[] items = resources.Select(s => new DataMenuItem() { Text = s }).ToArray();
            _FilteredItemsCount = resources.Length;

            _ListBox.SuspendLayout();
            _ListBox.Items.Clear();
            _ListBox.Items.AddRange(items);
            _ListBox.ResumeLayout(false);
            _ListBox.PerformLayout();

            _ResetStatusText();
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících aktuálnímu filtru
        /// </summary>
        /// <returns></returns>
        private string[] _GetResourcesByFilter()
        {
            if (_ResourceNames == null || _ResourceNames.Length == 0) return new string[0];

            var filter = _ResourceFilter;
            if (String.IsNullOrEmpty(filter)) return _ResourceNames;

            filter = RegexSupport.ReplaceGreenWildcards(filter);

            string[] result = _GetResourcesByFilter(filter);
            if (result.Length == 0)
                result = _GetResourcesByFilter("*" + filter + "*");

            return result;
        }
        /// <summary>
        /// Vrátí pole zdrojů vyhovujících danému filtru
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] _GetResourcesByFilter(string filter)
        {
            var regexes = RegexSupport.CreateWildcardsRegexes(filter);
            var result = RegexSupport.FilterByRegexes(_ResourceNames, regexes);
            return result.ToArray();
        }
        /// <summary>
        /// Vloží do Clipboardu kód obsahující aktuálně vybrané texty
        /// </summary>
        private void _DoCopyClipboard()
        {
            var selectedItems = _ListBox.SelectedMenuItemsExt;
            int rowCount = selectedItems.Length;
            int rowLast = rowCount - 1;
            StringBuilder sb = new StringBuilder();
            if (rowCount > 5)
            {   // Do pole:
                sb.AppendLine("  string[] resources = new string[] ");
                sb.AppendLine("  {");
                for (int i = 0; i < rowCount; i++)
                {
                    var selectedItem = selectedItems[i];
                    string resourceName = selectedItem.Item2?.Text;
                    string suffix = (i < rowLast ? "," : "");
                    sb.AppendLine($"    \"{resourceName}\"{suffix}");
                }
                sb.AppendLine("  };");
                if (sb.Length > 0)
                    DxComponent.ClipboardInsert(sb.ToString());

                StatusText = "Položky zkopírovány do schránky: " + rowCount.ToString();
            }
            else if (rowCount > 1)
            {   // Do proměnných:
                foreach (var selectedItem in selectedItems)
                {
                    _ClipboardCopyIndex++;
                    string resourceName = selectedItem.Item2?.Text;
                    if (!String.IsNullOrEmpty(resourceName))
                        sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");
                }
                if (sb.Length > 0)
                    DxComponent.ClipboardInsert(sb.ToString());

                StatusText = "Položky zkopírovány do schránky: " + rowCount.ToString();
            }
            else if (rowCount == 1)
            {
                _ClipboardCopyIndex++;
                var selectedItem = selectedItems[0];
                string resourceName = selectedItem.Item2?.Text;
                if (!String.IsNullOrEmpty(resourceName))
                    sb.AppendLine($"  string resource{_ClipboardCopyIndex} = \"{resourceName}\";");

                bool exists = DxComponent.TryGetResourceContentType(resourceName, ResourceImageSizeType.Large, out var contentType, true);
                if (exists && contentType == ResourceContentType.Vector)
                {
                    var svgImage = DxComponent.CreateVectorImage(resourceName, true);
                    if (svgImage != null)
                    {
                        string xmlImage = svgImage.ToXmlString();
                        sb.AppendLine($"  string content{_ClipboardCopyIndex} = @\"{xmlImage}\";");
                    }
                }

                if (sb.Length > 0)
                    DxComponent.ClipboardInsert(sb.ToString());

                StatusText = "Položka zkopírována do schránky: " + rowCount.ToString();
            }
            else
            {
                StatusText = $"Nejsou označeny žádné položky.";
            }
        }
        /// <summary>
        /// Filtrační text z textboxu
        /// </summary>
        private string _ResourceFilter { get { return this._FilterText.Text.Trim(); } set { this._FilterText.Text = (value ?? ""); } }
        /// <summary>
        /// Jména všech zdrojů
        /// </summary>
        private string[] _ResourceNames;
        /// <summary>
        /// Číslo pro číslování proměnných do Clipboardu
        /// </summary>
        private int _ClipboardCopyIndex;
        /// <summary>
        /// Počet aktuálně filtrovaných položek
        /// </summary>
        private int _FilteredItemsCount;
        #endregion
        #region Podpora pro StatusBar
        /// <summary>
        /// Text vhodný pro zobrazení ve statusbaru. Setování nové hodnoty vyvolá event <see cref="StatusTextChanged"/>
        /// </summary>
        public string StatusText
        {
            get { return _StatusText; }
            protected set
            {
                bool isChanged = !String.Equals(value, _StatusText);
                _StatusText = value;
                if (isChanged)
                    StatusTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _StatusText;
        private void _ResetStatusText()
        {
            int allItemsCount = _ResourceNames.Length;
            int filteredItemsCount = _FilteredItemsCount;
            bool hasFilter = !String.IsNullOrEmpty(_ResourceFilter);
            string filter = (hasFilter ? "'" + _ResourceFilter + "'" : "");
            if (allItemsCount == 0)
                StatusText = $"Neexistují žádné položky";
            else if (!hasFilter)
                StatusText = $"Zobrazeny všechny položky: {allItemsCount}";
            else if (filteredItemsCount == 0)
                StatusText = $"Zadanému filtru {filter} nevyhovuje žádná položka";
            else if (filteredItemsCount == allItemsCount)
                StatusText = $"Zadanému filtru {filter} vyhovují všechny položky: {filteredItemsCount}";
            else
                StatusText = $"Zadanému filtru {filter} vyhovují zobrazené položky: {filteredItemsCount}";
        }
        /// <summary>
        /// Událost vyvolaná po změně textu v <see cref="StatusText"/>
        /// </summary>
        public event EventHandler StatusTextChanged;
        #endregion
    }
    #endregion
    #region DxSimpleSplitter
    /// <summary>
    /// Jednoduchý splitter
    /// </summary>
    public class DxSimpleSplitter : Control
    {
        #region Konstruktor, privátní eventhandlery
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxSimpleSplitter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            _CursorOrientation = null;
            _Orientation = Orientation.Horizontal;
            _VisualLogoMode = LogoMode.Allways;                      // Viditelnost grafiky = vždy
            _VisualLogoDotsCount = 4;
            SetCursor();
            base.BackColor = Color.Transparent;
            SplitterColor = SystemColors.ControlDark;
            _SplitterActiveColor = Color.Yellow;
            _SplitterColorByParent = true;
            _SplitThick = 6;                                         // Opsáno z MS Outlook
            _SplitterEnabled = true;
            _AcceptBoundsToSplitter = false;
            _CurrentMouseState = MouseState.None;                    // Výchozí stav
            DevExpressSkinEnabled = true;                            // Tady se z aktuálního skinu přečtou barvy a uloží do barev _SkinBackColor a _SkinActiveColor
            Enabled = true;
            Initialized = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Splitter: {Name}; Orientation: {Orientation}, SplitPosition: {SplitPosition}";
        }
        /// <summary>
        /// Hodnota true povoluje práci v instanci.
        /// Obsahuje true po dokončení konstruktoru.
        /// Na začátku Dispose se shodí na false.
        /// </summary>
        public bool Initialized { get; protected set; } = false;
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Initialized = false;
            DevExpressSkinEnabled = false;
            base.Dispose(disposing);
        }
        #endregion
        #region Vzhled, kreslení, aktuální barvy, kreslící Brush, kurzor
        /// <summary>
        /// Refresh. 
        /// Je vhodné zavolat po změně souřadnic navázaných controlů, pak si Splitter podle nich určí svoji velikost.
        /// </summary>
        public override void Refresh()
        {
            RecalculateBounds();
            base.Refresh();
        }
        /// <summary>
        /// Po změně Enabled
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
        /// <summary>
        /// Po změně barvy pozadí parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Zajistí znovuvykreslení prvku
        /// </summary>
        protected virtual void PaintSplitter()
        {
            if (!Initialized) return;
            PaintEventArgs e = new PaintEventArgs(this.CreateGraphics(), this.ClientRectangle);
            PaintSplitter(e);
        }
        /// <summary>
        /// Provede kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Initialized) return;
            PaintSplitter(e);
        }
        /// <summary>
        /// Vykreslí Splitter
        /// </summary>
        /// <param name="e"></param>
        protected void PaintSplitter(PaintEventArgs e)
        {
            if (!Initialized) return;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    PaintHorizontal(e);
                    break;
                case Orientation.Vertical:
                    PaintVertical(e);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Horizontal
        /// </summary>
        /// <param name="e"></param>
        private void PaintHorizontal(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Height;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height + 1);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 90f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }

            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.Y - 1;
                int d = center.X - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(d + space * q, t, 2, 2));
            }
        }
        /// <summary>
        /// Vykreslí Splitter v orientaci Vertical
        /// </summary>
        /// <param name="e"></param>
        private void PaintVertical(PaintEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int size = bounds.Width;
            if (size <= 2)
            {   // Tenký splitter do 2px:
                e.Graphics.FillRectangle(CurrentBrush, bounds);
                return;
            }
            Rectangle brushBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height);
            Color color = CurrentColor;
            using (System.Drawing.Drawing2D.LinearGradientBrush lgb = new System.Drawing.Drawing2D.LinearGradientBrush(brushBounds, GetCurrentColor3DBegin(color), GetCurrentColor3DEnd(color), 0f))
            {
                e.Graphics.FillRectangle(lgb, bounds);
            }
            if (size > 4 && CurrentShowDots && VisualLogoDotsCount > 0)
            {
                int numbers = VisualLogoDotsCount;
                int space = (int)Math.Round(((double)SplitThick * 0.4d), 0);
                if (space < 4) space = 4;
                Point center = bounds.Center();
                int t = center.X - 1;
                int d = center.Y - ((space * numbers / 2) - 1);
                var dotBrush = CurrentDotBrush;
                for (int q = 0; q < numbers; q++)
                    e.Graphics.FillRectangle(dotBrush, new Rectangle(t, d + space * q, 2, 2));
            }
        }
        /// <summary>
        /// Aktuální barva, reaguje na hodnotu <see cref="SplitterColorByParent"/> a na Parenta,
        /// na stav splitteru <see cref="CurrentSplitterState"/> a na zvolené barvy LineColor*
        /// </summary>
        protected Color CurrentColor { get { return GetCurrentColorFrom(this.CurrentColorBase); } }
        /// <summary>
        /// Aktuální základní barva: reaguje na <see cref="SplitterColorByParent"/>, <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterColor"/>
        /// </summary>
        protected Color CurrentColorBase
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinBackColor.HasValue)
                    // Dle skinu:
                    return GetCurrentColorFrom(_DevExpressSkinBackColor.Value);

                if (this.SplitterColorByParent && this.Parent != null)
                    // Dle parenta:
                    return GetCurrentColorFrom(this.Parent.BackColor);

                return _SplitterColor;
            }
        }
        /// <summary>
        /// Aktuální barva pro aktivní splitter: reaguje na <see cref="DevExpressSkinEnabled"/> 
        /// a případně vrací <see cref="_SplitterActiveColor"/>
        /// </summary>
        protected Color CurrentColorActive
        {
            get
            {
                if (DevExpressSkinEnabled && _DevExpressSkinActiveColor.HasValue)
                    // Dle skinu:
                    return _DevExpressSkinActiveColor.Value;

                return _SplitterActiveColor;
            }
        }
        /// <summary>
        /// Vrací danou barvu modifikovanou dle aktuálního stavu
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetCurrentColorFrom(Color color)
        {
            color = Color.FromArgb(255, color);
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return GetColorDisable(color);
                case SplitterState.Enabled: return GetColorEnabled(color);
                case SplitterState.Hot: return GetColorActive(color);
                case SplitterState.Down: return GetColorDrag(color);
                case SplitterState.Drag: return GetColorDrag(color);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně počátku (Top/Left)
        /// </summary>
        protected Color GetCurrentColor3DBegin(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.White, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.White, 0.25f);
                case SplitterState.Down: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.Black, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro 3D zobrazení na straně konce (Bottom/Right)
        /// </summary>
        protected Color GetCurrentColor3DEnd(Color color)
        {
            switch (CurrentSplitterState)
            {
                case SplitterState.Disabled: return color.Morph(Color.LightGray, 0.25f);
                case SplitterState.Enabled: return color.Morph(Color.Black, 0.15f);
                case SplitterState.Hot: return color.Morph(Color.Black, 0.25f);
                case SplitterState.Down: return color.Morph(Color.White, 0.15f);
                case SplitterState.Drag: return color.Morph(Color.White, 0.15f);
            }
            return color;
        }
        /// <summary>
        /// Aktuální barva použitá pro zobrazení grafiky (čtyřtečka)
        /// </summary>
        protected Color CurrentDotColor { get { return CurrentColor.Contrast(64); } }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDisable(Color color) { return color.GrayScale(0.75f); }
        /// <summary>
        /// Vrátí barvu Enabled k barvě dané.
        /// Záleží na <see cref="SplitThick"/>: pokud je 2 (a menší), pak vrací danou barvu lehce kontrastní, aby byl splitter vidět.
        /// Pokud je 3 a více, pak vrací danou barvu beze změn, protože se bude vykreslovat 3D efektem.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorEnabled(Color color) { return (this.SplitThick <= 2 ? color.Contrast(12) : color); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorActive(Color color) { return color.Morph(CurrentColorActive, 0.40f); }
        /// <summary>
        /// Vrátí barvu Disabled k barvě dané
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Color GetColorDrag(Color color) { return color.Morph(CurrentColorActive, 0.60f); }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentColor"/>
        /// </summary>
        protected SolidBrush CurrentBrush { get { return DxComponent.PaintGetSolidBrush(CurrentColor); } }
        /// <summary>
        /// Brush s aktuální barvou <see cref="CurrentDotColor"/>
        /// </summary>
        protected SolidBrush CurrentDotBrush { get { return DxComponent.PaintGetSolidBrush(CurrentDotColor); } }
        /// <summary>
        /// Má se aktuálně zobrazovat grafika (čtyřtečka) uvnitř Splitteru?
        /// </summary>
        protected bool CurrentShowDots
        {
            get
            {
                var mode = VisualLogoMode;
                switch (CurrentSplitterState)
                {
                    case SplitterState.Disabled: return (mode == LogoMode.Allways);
                    case SplitterState.Enabled: return (mode == LogoMode.Allways);
                    case SplitterState.Hot: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                    case SplitterState.Down: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                    case SplitterState.Drag: return (mode == LogoMode.Allways || mode == LogoMode.OnMouse);
                }
                return false;
            }
        }
        /// <summary>
        /// Nastaví typ kurzoru pro this prvek podle aktuální orientace.
        /// </summary>
        /// <param name="force"></param>
        protected void SetCursor(bool force = false)
        {
            System.Windows.Forms.Orientation orientation = _Orientation;
            if (force || !_CursorOrientation.HasValue || _CursorOrientation.Value != orientation)
                this.Cursor = (orientation == System.Windows.Forms.Orientation.Horizontal ? Cursors.HSplit : Cursors.VSplit);
            _CursorOrientation = orientation;
        }
        private System.Windows.Forms.Orientation? _CursorOrientation;
        #endregion
        #region Interaktivita splitteru - reakce Splitteru na akce a pohyby myši
        /// <summary>
        /// Při vstupu myši nad control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this._SplitterMouseEnter();
            CurrentSplitterEnabled = SplitterEnabled;
            if (!CurrentSplitterEnabled) return;
            BringSplitterToFront(true);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.Over;
            ChangeCursor(true);
            PaintSplitter();
        }
        /// <summary>
        /// Při odchodu myši z controlu
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            CurrentMouseState = MouseState.None;
            ChangeCursor(false);
            PaintSplitter();
        }
        /// <summary>
        /// Při stisknutí myši - příprava na možný Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!CurrentSplitterEnabled) return;
            if (e.Button != MouseButtons.Left) return;
            Point point = Control.MousePosition;
            MouseDownAbsolutePoint = point;
            MouseDownWorkingBounds = CurrentWorkingBounds;
            MouseDragAbsoluteSilentZone = new Rectangle(point.X - 2, point.Y - 2, 5, 5);
            MouseDragOriginalSplitPosition = SplitPosition;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Down;
            PaintSplitter();
        }
        /// <summary>
        /// Při pohybu myši - mžná provedeme Drag and Drop
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!CurrentSplitterEnabled) return;
            Point point = Control.MousePosition;
            if (CurrentSplitterState == SplitterState.Down) DetectSilentZone(point);          // Pokud je zmáčknutá myš, je stav Down; pokud se pohne o malý kousek, přejde stav do Drag
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragMove(point);    // Ve stavu Drag řídíme přesun splitteru
        }
        /// <summary>
        /// Při zvednutí myši - pokud byl Drag and Drop, pak jej dokončíme
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!CurrentSplitterEnabled) return;
            if (CurrentSplitterState == SplitterState.Drag) DetectSplitterDragDone();         // Pokud jsme ve stavu Drag, ukončíme přesun splitteru
            MouseDownAbsolutePoint = null;
            MouseDownWorkingBounds = null;
            MouseDragAbsoluteSilentZone = null;
            MouseDragOriginalSplitPosition = null;
            MouseDragLastSplitPosition = null;
            CurrentMouseState = MouseState.Over;
            PaintSplitter();
        }
        /// <summary>
        /// Detekuje pohyb mimo <see cref="MouseDragAbsoluteSilentZone"/>.
        /// Pokud se myš pohybuje uvnitř (anebo pokud SilentZone už není), nic neprovádí.
        /// Pokud je ale SilentZone definovaná a myš se nachází mimo ni, pak SilentZone nuluje a nastaví <see cref="CurrentMouseState"/> = <see cref="MouseState.Drag"/>
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSilentZone(Point absolutePoint)
        {
            if (!MouseDragAbsoluteSilentZone.HasValue) return;
            if (MouseDragAbsoluteSilentZone.Value.Contains(absolutePoint)) return;
            MouseDragAbsoluteSilentZone = null;
            _SplitPositionDragBegin();
            CurrentMouseState = MouseState.Drag;
        }
        /// <summary>
        /// Detekuje pohyb myši ve stavu  <see cref="MouseState.Drag"/>, určuje novou hodnotu pozice a volá event 
        /// </summary>
        /// <param name="absolutePoint"></param>
        protected void DetectSplitterDragMove(Point absolutePoint)
        {
            if (!MouseDownAbsolutePoint.HasValue) return;
            Point originPoint = MouseDownAbsolutePoint.Value;
            Rectangle workingBounds = MouseDownWorkingBounds.Value;
            int distance = (Orientation == Orientation.Horizontal ? (absolutePoint.Y - originPoint.Y) : (absolutePoint.X - originPoint.X));
            int oldValue = MouseDragOriginalSplitPosition.Value;
            int newValue = oldValue + distance;                                          // Hodnota splitteru požadovaná posunem myši
            SetValidSplitPosition(newValue, useWorkingBounds: workingBounds, actions: SetActions.None);           // Korigovat danou myšovitou hodnotu, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
            int validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            if (!MouseDragLastSplitPosition.HasValue || MouseDragLastSplitPosition.Value != validValue)
            {
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, validValue);
                _SplitPositionDragMove(args);                                            // Tady voláme event SplitPositionDragMove
                RunSplitPositionChanging(args);                                            // Tady voláme event PositionChanging (po reálné změně hodnoty, a event Changing - nikoli Changed)
                DetectSplitterEventsModify(args, ref validValue);
                MouseDragLastSplitPosition = SplitPosition;
                RecalculateBounds(workingBounds);
                PaintSplitter();
            }
        }
        /// <summary>
        /// Po dokončení procesu Drag vyvolá event <see cref="SplitPositionChanged"/>.
        /// </summary>
        protected void DetectSplitterDragDone()
        {
            if (!MouseDragOriginalSplitPosition.HasValue || MouseDragOriginalSplitPosition.Value != SplitPosition)
            {
                int oldValue = MouseDragOriginalSplitPosition ?? 0;
                int newValue = SplitPosition;
                TEventValueChangeArgs<double> args = new TEventValueChangeArgs<double>(EventSource.User, oldValue, newValue);
                _SplitPositionDragDone(args);
                RunSplitPositionChanged(args);
                bool isChanged = DetectSplitterEventsModify(args, ref newValue);
                MouseDragOriginalSplitPosition = SplitPosition;
                if (isChanged)
                    RecalculateBounds(MouseDownWorkingBounds);
                PaintSplitter();
            }
        }
        /// <summary>
        /// Metoda zpracuje odpovědi v argumentu <paramref name="args"/>.
        /// Reaguje na Cancel, pak vrátí do <paramref name="validValue"/> původní hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.OldValue"/>;
        /// reaguje na Changed, pak do <paramref name="validValue"/> vloží nově zadanou hodnotu z argumentu = <see cref="TEventValueChangeArgs{T}.NewValue"/>;
        /// Pokud takto zaregistruje změnu, tak novou hodnotu vloží do SplitPosition a do Bounds a vrátí true.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="validValue"></param>
        /// <returns></returns>
        protected bool DetectSplitterEventsModify(TEventValueChangeArgs<double> args, ref int validValue)
        {
            bool changed = false;
            if (args.Cancel)
            {
                validValue = (int)args.OldValue;
                changed = true;
            }
            else if (args.Changed)
            {
                validValue = (int)args.NewValue;
                changed = true;
            }
            if (changed)
            {
                SetValidSplitPosition(validValue, useWorkingBounds: MouseDownWorkingBounds, actions: SetActions.None);     // Korigovat hodnotu dodanou aplikačním eventhandlerem, ale neměnit ani Bounds, ani Controls ani nevolat event PositionChanged
                validValue = SplitPosition;                                              // Hodnota po korekci (se zohledněním Min distance Before a After)
            }
            return changed;
        }
        /// <summary>
        /// Hodnota <see cref="SplitterEnabled"/> zachycená při MouseEnter, po skončení eventu <see cref="SplitterMouseEnter"/>, platná pro aktuální akce myši.
        /// Tzn. pokud při MouseEnter bude v eventu <see cref="SplitterMouseEnter"/> určena hodnota <see cref="SplitterEnabled"/> = false, 
        /// pak až do odchodu myši ze splitteru a do nového příchodu platí tato hodnota.
        /// </summary>
        protected bool CurrentSplitterEnabled { get; set; }
        /// <summary>
        /// Souřadnice bodu, kde byla stisknuta myš - v absolutních koordinátech
        /// </summary>
        protected Point? MouseDownAbsolutePoint { get; set; }
        /// <summary>
        /// Aktuální pracovní souřadnice splitteru <see cref="CurrentWorkingBounds"/>, platné v okamžiku MouseDown. Jindy je null.
        /// </summary>
        protected Rectangle? MouseDownWorkingBounds { get; set; }
        /// <summary>
        /// Souřadnice prostoru, kde budeme ignorovat pohyb myši po jejím MouseDown (v absolutních koordinátech).
        /// Tím potlačíme malé pohyby před zahájením Drag.
        /// Pokud je zde null, a v <see cref="MouseDownAbsolutePoint"/> pak už myš opustila výchozí SilentZone a reagujeme na její pohyby.
        /// </summary>
        protected Rectangle? MouseDragAbsoluteSilentZone { get; set; }
        /// <summary>
        /// Počáteční hodnota <see cref="SplitPosition"/> před zahájením Drag
        /// </summary>
        protected int? MouseDragOriginalSplitPosition { get; set; }
        /// <summary>
        /// Předchozí hodnota <see cref="SplitPosition"/> při posledním volání eventu 
        /// </summary>
        protected int? MouseDragLastSplitPosition { get; set; }
        /// <summary>
        /// Aktuální stav myši. Změna hodnoty vyvolá invalidaci = překreslení.
        /// </summary>
        protected MouseState CurrentMouseState { get { return _CurrentMouseState; } set { _CurrentMouseState = value; Invalidate(); } }
        private MouseState _CurrentMouseState;
        /// <summary>
        /// Metoda zajistí změnu kurzoru podle dané aktivity a aktuální orientace splitteru.
        /// </summary>
        /// <param name="active"></param>
        protected void ChangeCursor(bool active)
        {
            if (active)
                this.Cursor = (this.Orientation == Orientation.Horizontal ? Cursors.HSplit : (this.Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.Default));
            else
                this.Cursor = Cursors.Default;
        }
        /// <summary>
        /// Aktuální stav Splitteru odpovídající stavu Enabled a stavu myši <see cref="CurrentMouseState"/>.
        /// </summary>
        protected SplitterState CurrentSplitterState
        {
            get
            {
                if (!this.Enabled) return SplitterState.Disabled;
                switch (this.CurrentMouseState)
                {
                    case MouseState.None: return SplitterState.Enabled;
                    case MouseState.Over: return SplitterState.Hot;
                    case MouseState.Down: return SplitterState.Down;
                    case MouseState.Drag: return SplitterState.Drag;
                }
                return SplitterState.Enabled;
            }
        }
        /// <summary>
        /// Stavy myši
        /// </summary>
        protected enum MouseState
        {
            /// <summary>
            /// Neurčeno
            /// </summary>
            None,
            /// <summary>
            /// Myš je nad prvkem
            /// </summary>
            Over,
            /// <summary>
            /// Myš je dole ale nepohybuje se
            /// </summary>
            Down,
            /// <summary>
            /// Myš je dole a posunuje prvek
            /// </summary>
            Drag
        }
        /// <summary>
        /// Stavy prvku
        /// </summary>
        protected enum SplitterState
        {
            /// <summary>
            /// Enabled
            /// </summary>
            Enabled,
            /// <summary>
            /// Disabled
            /// </summary>
            Disabled,
            /// <summary>
            /// Hot
            /// </summary>
            Hot,
            /// <summary>
            /// MouseDown
            /// </summary>
            Down,
            /// <summary>
            /// Drag
            /// </summary>
            Drag
        }
        #endregion
        #region Public properties - funkcionalita Splitteru (hodnota, orientace, šířka)
        /// <summary>
        /// Aktuální pozice splitteru = hodnota středového pixelu na ose X nebo Y, podle orientace.
        /// Setování této hodnoty VYVOLÁ event <see cref="SplitPositionChanged"/> a zajistí úpravu souřadnic navázaných objektů podle režimu 'ActivityMode'.
        /// </summary>
        public int SplitPosition { get { return (int)Math.Round(_SplitPosition, 0); } set { SetValidSplitPosition(value, actions: SetActions.Default); } }
        /// <summary>
        /// Pozice splitteru uložená jako Double, slouží pro přesné výpočty pozic při 'AnchorType' == <see cref="SplitterAnchorType.Relative"/>,
        /// kdy potřebujeme mít pozici i na desetinná místa.
        /// <para/>
        /// Interaktivní přesouvání vkládá vždy integer číslo, public hodnota <see cref="SplitPosition"/> je čtena jako Math.Round(<see cref="SplitPosition"/>, 0).
        /// Setovat double hodnotu je možno pomocí metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SetActions)"/>.
        /// </summary>
        private double _SplitPosition;
        /// <summary>
        /// Viditelná šířka splitteru. Nastavuje se automaticky na nejbližší vyšší sudé číslo.
        /// Tento počet pixelů bude vykreslován.
        /// Rozsah hodnot = 0 až 30 px.
        /// Hodnota 0 je přípustná, splitter pak nebude viditelný.
        /// </summary>
        public int SplitThick { get { return this._SplitThick; } set { SetValidSplitPosition(null, value, actions: SetActions.Silent); } }
        private int _SplitThick;
        /// <summary>
        /// Orientace splitteru = vodorovná nebo svislá
        /// </summary>
        public Orientation Orientation { get { return this._Orientation; } set { _Orientation = value; SetCursor(); SetValidSplitPosition(null, actions: SetActions.Silent); } }
        private Orientation _Orientation;
        /// <summary>
        /// Příznak, zda má Splitter reagovat na vložení souřadnic do <see cref="Control.Bounds"/>.
        /// Pokud je true, pak po vložení souřadnic se ze souřadnic odvodí <see cref="SplitPosition"/> a <see cref="SplitThick"/>, a vepíše se do Splitteru.
        /// Default = false: souřadnice splitteru nelze změnit vložením hodnoty do <see cref="Control.Bounds"/>, takový pokus bude ignorován.
        /// </summary>
        public bool AcceptBoundsToSplitter { get { return this._AcceptBoundsToSplitter; } set { _AcceptBoundsToSplitter = value; } }
        private bool _AcceptBoundsToSplitter;
        /// <summary>
        /// Povolení aktivity splitteru.
        /// Vyhodnocuje se při vstupu myši nad Splitter, po proběhnutí eventu <see cref="SplitterMouseEnter"/>.
        /// Pokud je true, je povolen MouseDrag, jinak není.
        /// </summary>
        public bool SplitterEnabled { get { return this._SplitterEnabled; } set { _SplitterEnabled = value; } }
        private bool _SplitterEnabled;
        #endregion
        #region Public properties - vzhled
        /// <summary>
        /// Barva pozadí je vždy Transparent, nemá význam ji setovat
        /// </summary>
        public override Color BackColor { get { return Color.Transparent; } set { Invalidate(); } }
        /// <summary>
        /// Barvu splitteru vždy přebírej z barvy pozadí Parenta.
        /// Default hodnota = true, má přednost před barvou Skinu.
        /// Při souběhu <see cref="DevExpressSkinEnabled"/> = true; a <see cref="SplitterColorByParent"/> = true; bude barva převzata z Parent controlu.
        /// Pokud bude <see cref="SplitterColorByParent"/> = false; a <see cref="DevExpressSkinEnabled"/> = true; pak se barva splitteru bude přebírat ze Skinu.
        /// Pokud budou obě false, pak barva Splitteru bude dána barvou <see cref="SplitterColor"/>.
        /// </summary>
        public bool SplitterColorByParent { get { return _SplitterColorByParent; } set { _SplitterColorByParent = value; Invalidate(); } }
        private bool _SplitterColorByParent;
        /// <summary>
        /// Základní barva splitteru.
        /// Pokud je ale nastaveno <see cref="SplitterColorByParent"/> = true, pak je hodnota <see cref="SplitterColor"/> čtena z Parent.BackColor.
        /// Setování hodnoty je sice interně uložena, ale setovaná hodnota nemá vliv na zobrazení (až do změny nastaveni <see cref="SplitterColorByParent"/> na false).
        /// </summary>
        public Color SplitterColor { get { return CurrentColorBase; } set { _SplitterColor = value; Invalidate(); } }
        private Color _SplitterColor;
        /// <summary>
        /// Barva aktivního splitteru.
        /// Toto je pouze vzdálená cílová barva; reálně má splitter v aktivním stavu barvu základní <see cref="SplitterColor"/>,
        /// jen mírně modifikovanou směrem k této aktivní barvě <see cref="SplitterActiveColor"/>.
        /// </summary>
        public Color SplitterActiveColor { get { return CurrentColorActive; } set { _SplitterActiveColor = value; Invalidate(); } }
        private Color _SplitterActiveColor;
        /// <summary>
        /// Režim zobrazování grafiky (čtyřtečka) uprostřed Splitteru.
        /// Výchozí hodnota je <see cref="LogoMode.OnMouse"/>
        /// </summary>
        public LogoMode VisualLogoMode { get { return _VisualLogoMode; } set { _VisualLogoMode = value; Invalidate(); } }
        private LogoMode _VisualLogoMode;
        /// <summary>
        /// Počet teček zobrazovaných jako grafika ("čtyřtečka").
        /// Default = 4. Platné rozmezí = 0 až 30
        /// </summary>
        public int VisualLogoDotsCount { get { return _VisualLogoDotsCount; } set { _VisualLogoDotsCount = (value < 0 ? 0 : (value > 30 ? 30 : value)); Invalidate(); } }
        private int _VisualLogoDotsCount;
        #endregion
        #region DevExpress - reakce na změnu skinu, akceptování skinu pro vzhled Splitteru
        /// <summary>
        /// Obsahuje true, pokud this splitter je napojen na DevExpress skin.
        /// Výchozí hodnota je true.
        /// </summary>
        public bool DevExpressSkinEnabled
        {
            get { return _DevExpressSkinEnabled; }
            set
            {
                if (_DevExpressSkinEnabled)
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged -= DevExpressSkinChanged;
                _DevExpressSkinEnabled = value;
                if (_DevExpressSkinEnabled)
                {
                    DevExpressSkinLoad();
                    DevExpress.LookAndFeel.UserLookAndFeel.Default.StyleChanged += DevExpressSkinChanged;
                }
            }
        }
        private bool _DevExpressSkinEnabled;
        /// <summary>
        /// Provede se po změně DevExpress Skinu (event je volán z <see cref="DevExpress.LookAndFeel.UserLookAndFeel.Default"/> : <see cref="DevExpress.LookAndFeel.UserLookAndFeel.StyleChanged"/>)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DevExpressSkinChanged(object sender, EventArgs e)
        {
            DevExpressSkinLoad();
        }
        /// <summary>
        /// Načte aktuální hodnoty DevExpress Skinu do this controlu
        /// </summary>
        private void DevExpressSkinLoad()
        {
            if (this.DevExpressSkinEnabled)
            {
                var skinName = DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveSkinName;
                var skin = DevExpress.Skins.SkinManager.Default.GetSkin(DevExpress.Skins.SkinProductId.Common, skinName);
                _DevExpressSkinBackColor = skin.GetSystemColor(SystemColors.ControlLight);
                _DevExpressSkinActiveColor = skin.GetSystemColor(SystemColors.HotTrack);
            }
        }
        /// <summary>
        /// Barva pozadí načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinBackColor;
        /// <summary>
        /// Barva aktivní načtená z aktuálního Skinu.
        /// Bude akceptována, pokud je zadána a pokud <see cref="DevExpressSkinEnabled"/> je true.
        /// </summary>
        private Color? _DevExpressSkinActiveColor;
        #endregion
        #region Abstraktní věci jsou, když tady nic není. Virtuální jsou ty, které sice jsou, ale stejně nic nedělají. GetWorkingBounds(), ApplySplitterToControls()
        /// <summary>
        /// V této metodě potomek určí prostor, ve kterém se může pohybovat Splitter.
        /// <para/>
        /// Vrácený prostor má dva významy:
        /// <para/>
        /// a) V první řadě určuje rozsah pohybu Splitteru od-do: např. pro svislý splitter je klíčem hodnota Left a Right vráceného prostoru = odkud a kam může splitter jezdit
        /// (k tomu poznámka: jde o souřadnice vnějšího okraje splitteru, tedy včetně jeho tloušťky: 
        /// pokud tedy X = 0, pak splitter bude mít svůj levý okraj nejméně na pozici 0, a jeho <see cref="SplitterControl.SplitPosition"/> tedy bude o půl <see cref="SplitterControl.SplitThick"/> větší).
        /// Pro vodorovný Splitter je v tomto ohledu klíčová souřadnice Top a Bottom.
        /// <para/>
        /// b) V druhé řadě určuje vrácený prostor velikost Splitteru v "neaktivním" směru: např. pro svislý splitter bude kreslen nahoře od pozice Top dolů, jeho výška bude = Height.
        /// Vodorovný Splitter si pak převezme Left a Width.
        /// <para/>
        /// Metoda je volaná při změně hodnoty nebo orientace nebo tloušťky, a na začátku interaktivního přemísťování pomocí myši.
        /// <para/>
        /// Tato metoda dostává jako parametr maximální možnou velikost = prostor v parentu. Metoda ji může vrátit beze změny, pak Splitter bude "jezdit" v celém parentu bez omezení.
        /// Bázová metoda to tak dělá - vrací beze změny dodaný parametr.
        /// </summary>
        /// <param name="currentArea">Souřadnice ClientArea, ve kterých se může pohybovat Splitter v rámci svého parenta</param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentWorkingBounds(Rectangle currentArea) { return currentArea; }
        #endregion
        #region Jádro splitteru - vložení hodnoty do splitteru, kontroly, výpočty souřadnic
        /// <summary>
        /// Provede změnu pozice splitteru na zadanou hodnotu <paramref name="splitPosition"/> a/nebo <see cref="SplitThick"/>.
        /// Lze tedy zadat všechny hodnoty najednou a navázané výpočty proběhnou jen jedenkrát.
        /// Všechny tyto hodnoty mají nějaký vliv na pozici a souřadnice splitteru, proto je vhodnější je setovat jedním voláním, které je tedy optimálnější.
        /// Zadanou hodnotu zkontroluje s ohledem na vlastnosti splitteru, uloží hodnotu do <see cref="_SplitPosition"/>.
        /// <para/>
        /// Tato metoda se používá interně při interaktivních pohybech, při zadání limitujících hodnot i jako reakce na vložení hodnoty do property <see cref="SplitPosition"/>.
        /// Touto metodou lze vložit hodnotu <see cref="SplitPosition"/> typu <see cref="Double"/>, což se využívá při změně velikosti parenta a typu kotvy <see cref="SplitterAnchorType.Relative"/>.
        /// Tam by se s hodnotou typu <see cref="Int32"/> nedalo pracovat.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="splitPosition">Nová hodnota <see cref="SplitPosition"/>. Pokud bude NULL, vezme se stávající pozice.</param>
        /// <param name="splitThick">Nová hodnota <see cref="SplitThick"/>, hodnota null = beze změny</param>
        /// <param name="useWorkingBounds">Použít dané souřadnice jako WorkingBounds (=nvyhodnocovat <see cref="CurrentWorkingBounds"/>, ani neukládat do <see cref="LastWorkingBounds"/>)</param>
        /// <param name="actions"></param>
        protected void SetValidSplitPosition(double? splitPosition, int? splitThick = null, Rectangle? useWorkingBounds = null, SetActions actions = SetActions.Default)
        {
            if (SetValidSplitPositionInProgress) return;

            try
            {
                SetValidSplitPositionInProgress = true;

                // Nejprve zpracuji explicitně zadanou hodnotu SplitThick, protože ta může mít vliv na algoritmus GetValidSplitPosition():
                bool changedThick = false;
                if (splitThick.HasValue)
                {
                    int oldThick = _SplitThick;
                    int newThick = GetValidSplitThick(splitThick.Value);
                    changedThick = (oldThick != newThick);
                    if (changedThick)
                        _SplitThick = newThick;
                }

                // Změna WorkingBounds:
                bool changedBounds = false;
                Rectangle workingBounds;
                if (useWorkingBounds.HasValue)
                {
                    workingBounds = useWorkingBounds.Value;
                }
                else
                {
                    Rectangle oldWorkingBounds = LastWorkingBounds;
                    Rectangle newWorkingBounds = CurrentWorkingBounds;
                    changedBounds = (newWorkingBounds != oldWorkingBounds);
                    if (changedBounds)
                        LastWorkingBounds = newWorkingBounds;
                    workingBounds = newWorkingBounds;
                }

                // A poté zpracuji Position - tu zpracuji i když by na vstupu byla hodnota null (pak jako požadovanou novou hodnotu budu brát hodnotu současnou),
                //  protože v metodě GetValidSplitPosition() se aplikují veškeré limity pro hodnotu, a ty se mohly změnit => proto může být volána this metoda:
                double oldPosition = _SplitPosition;
                double newPosition = GetValidSplitPosition(splitPosition ?? oldPosition, workingBounds);
                bool changedPosition = (Math.Round(newPosition, 2) != Math.Round(oldPosition, 2));
                if (changedPosition)
                    _SplitPosition = newPosition;

                // Pokud není žádná změna, a není ani požadavek na ForceActions, pak skončíme:
                bool force = (actions.HasFlag(SetActions.ForceActions));
                if (!(changedThick || changedBounds || changedPosition || force)) return;

                // Nastavit souřadnice podle aktuální hodnoty:
                if (actions.HasFlag(SetActions.RecalculateBounds)) RecalculateBounds(workingBounds, true);

                // Události:
                if (actions.HasFlag(SetActions.CallEventChanging)) RunSplitPositionChanging(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
                if (actions.HasFlag(SetActions.CallEventChanged)) RunSplitPositionChanged(new TEventValueChangeArgs<double>(EventSource.Code, oldPosition, newPosition));
            }
            finally
            {
                SetValidSplitPositionInProgress = false;
            }
        }
        /// <summary>
        /// Metoda ze zadaných souřadnic odvodí hodnoty splitPosition a splitThick a vloží je do this Splitteru.
        /// Pozor: potomek smí metodu přepsat, a z neaktivních souřadnic si může odvodit WorkingBounds, musí ale zavolat base.SetSplitterByBounds() ! Jinak nebude proveden základní výpočet.
        /// Základní výpočet ve třídě <see cref="SplitterControl"/> zajistí určení platné hodnoty <see cref="SplitThick"/> a <see cref="SplitPosition"/>, a jejich vložení do splitteru, 
        /// včetně validace hodnot a případné korekce souřadnic splitetru !
        /// <para/>
        /// Tato metoda je volána pouze tehdy, když jsou změněny souřadnice splitteru, a tento má nastaveno <see cref="SplitterControl.AcceptBoundsToSplitter"/> = true.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="actions">Akce prováděné Splitterem, pokud nebude zadáno použije se <see cref="SetActions.Default"/>.</param>
        protected virtual void SetSplitterByBounds(Rectangle bounds, SetActions? actions = null)
        {
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            int splitThick = GetValidSplitThick((isHorizontal ? bounds.Height : bounds.Width));
            int th = splitThick / 2;
            double splitPosition = (isHorizontal ? bounds.Y : bounds.X) + th;
            SetValidSplitPosition(splitPosition, splitThick, null, (actions ?? SetActions.Default));
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SetValidSplitPosition(double?, int?, Rectangle?, SetActions)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool SetValidSplitPositionInProgress { get; private set; } = false;
        /// <summary>
        /// Posledně platné pracovní souřadnice Splitteru. K těmto pracovním souřadnicím byly určeny souřadnice Splitteru.
        /// </summary>
        protected Rectangle LastWorkingBounds { get; private set; } = Rectangle.Empty;
        /// <summary>
        /// Metoda vrátí platnou hodnotu pro <see cref="SplitThick"/> pro libovolnou vstupní hodnotu.
        /// </summary>
        /// <param name="splitThick"></param>
        /// <returns></returns>
        protected static int GetValidSplitThick(int splitThick)
        {
            int t = splitThick.Align(0, 30);
            if ((t % 2) == 1) t++;               // Hodnota nesmí být lichá, to kvůli správnému počítání grafiky. Takže nejbližší vyšší sudá...
            return t;
        }
        /// <summary>
        /// Metoda ověří zadanou požadovanou pozici splitteru a vrátí hodnotu platnou.
        /// Potomek může metodu přepsat a hodnotu kontrolovat jinak.
        /// Na vstupu je požadovaná hodnota <see cref="SplitterControl.SplitPosition"/>
        /// a souřadnice pracovního prostoru, které vygenerovala metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>
        /// </summary>
        /// <param name="splitPosition">Zvenku daná pozice Splitteru, požadavek</param>
        /// <param name="currentWorkingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual double GetValidSplitPosition(double splitPosition, Rectangle currentWorkingBounds)
        {
            Rectangle logicalWorkingBounds = GetLogicalRectangle(currentWorkingBounds);
            double th = (double)SplitThick / 2d;
            double min = 0d;
            double max = (double)MaxSize;
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    min = (double)logicalWorkingBounds.Top + th;
                    max = (double)logicalWorkingBounds.Bottom - th;
                    break;
                case Orientation.Vertical:
                    min = (double)logicalWorkingBounds.Left + th;
                    max = (double)logicalWorkingBounds.Right - th;
                    break;
            }
            return splitPosition.Align(min, max);
        }
        /// <summary>
        /// Aktuální pozice splitteru posunutá o Scroll pozici aktuálního containeru.
        /// Pokud Parent container je AutoScroll, pak se Splitter má vykreslovat na jiných souřadnicích, než odpovídá hodnotě <see cref="SplitPosition"/> = právě o posun AutoScrollu.
        /// </summary>
        protected int CurrentSplitPosition
        {
            get
            {
                int splitPosition = SplitPosition;
                Point offset = CurrentOrigin;
                if (!offset.IsEmpty)
                {
                    switch (_Orientation)
                    {
                        case Orientation.Horizontal: return splitPosition + offset.Y;
                        case Orientation.Vertical: return splitPosition + offset.X;
                    }
                }
                return splitPosition;
            }
        }
        /// <summary>
        /// Maximální velikost - použitá v případě, kdy není znám Parent
        /// </summary>
        protected const int MaxSize = 10240;
        #endregion
        #region Eventy, háčky a jejich spouštěče
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitterMouseEnter()"/> a event <see cref="SplitterMouseEnter"/>
        /// </summary>
        private void _SplitterMouseEnter()
        {
            OnSplitterMouseEnter();
            SplitterMouseEnter?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při vstupu myši na splitter. 
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou události <see cref="OnSplitPositionDragBegin()"/> (při zahájení pohybu),
        /// <see cref="OnSplitPositionDragMove"/> (po každém pixelu) a <see cref="OnSplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        protected virtual void OnSplitterMouseEnter() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém vstupu myši na splitter.
        /// Důsledkem události může být změna stavu splitteru v property <see cref="SplitterEnabled"/>.
        /// <see cref="SplitterControl"/> si po proběhnutí této události uschová hodnotu <see cref="SplitterEnabled"/> do soukromé proměnné, 
        /// která následně řídí funkcionalitu splitteru i jeho vykreslování jako reakci na pohyb myši.
        /// <para/>
        /// Následovat budou eventy <see cref="SplitPositionDragBegin"/> (při zahájení pohybu),
        /// <see cref="SplitPositionDragMove"/> (po každém pixelu) a <see cref="SplitPositionDragDone"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitterMouseEnter;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragBegin()"/> a event <see cref="SplitPositionDragBegin"/>
        /// </summary>
        private void _SplitPositionDragBegin()
        {
            OnSplitPositionDragBegin();
            SplitPositionDragBegin?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Volá se při zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// </summary>
        protected virtual void OnSplitPositionDragBegin() { }
        /// <summary>
        /// Událost volaná jedenkrát při každém zahájení interaktivního přesunu splitteru pomocí myši (po stisknutí myši a prvním pohybu).
        /// Následovat budou eventy <see cref="SplitPositionChanging"/> (po každém pixelu) a <see cref="SplitPositionChanged"/> (po zvednutí myši).
        /// </summary>
        public event EventHandler SplitPositionDragBegin;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragMove(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragMove"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragMove(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragMove(args);
            SplitPositionDragMove?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragMove(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při interaktivním přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragMove;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionDragDone(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionDragDone"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void _SplitPositionDragDone(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionDragDone(args);
            SplitPositionDragDone?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionDragDone(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> při dokončení interaktivního přemísťování splitteru myší
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionDragDone;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanging(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanging"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void RunSplitPositionChanging(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanging(args);
            SplitPositionChanging?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se v průběhu pohybu splitteru
        /// </summary>
        protected virtual void OnSplitPositionChanging(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> v procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanging;
        /// <summary>
        /// Vyvolá metodu <see cref="OnSplitPositionChanged(TEventValueChangeArgs{double})"/> a event <see cref="SplitPositionChanged"/>
        /// </summary>
        /// <param name="args">Argument pro handler</param>
        private void RunSplitPositionChanged(TEventValueChangeArgs<double> args)
        {
            OnSplitPositionChanged(args);
            SplitPositionChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Volá se po dokončení pohybu splitteru = po pohybu a po zvednutí myši.
        /// </summary>
        protected virtual void OnSplitPositionChanged(TEventValueChangeArgs<double> args) { }
        /// <summary>
        /// Událost volaná po každé změně hodnoty <see cref="SplitPosition"/> z kódu, a po dokončení procesu interaktivního přemísťování
        /// </summary>
        public event EventHandler<TEventValueChangeArgs<double>> SplitPositionChanged;
        #endregion
        #region Souřadnice jsou věc specifická...   Vkládání souřadnic, konverze souřadnic při AutoScrollu (logické / aktuální)
        /// <summary>
        /// Tudy chodí externí setování souřadnic...
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="specified"></param>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // Po změnách datových hodnot Splitteru vkládáme jeho nové souřadnice v metodě RecalculateBounds() přímo do base metody SetBoundsCore().
            //    (takže vložení souřadnic do splitteru po vložení hodnoty Splitteru NEJDE touto metodou!)
            // Do této metody nám tedy vstupuje řízení pouze po EXTERNÍ změně souřadnic.

            if (this.AcceptBoundsToSplitter)
            {   // Pokud je aktivní příznak AcceptBoundsToSplitter, pak dodané souřadnice zpracujeme do souřadnic i do dat Splitteru:
                base.SetBoundsCore(x, y, width, height, specified);            // Nejprve vložíme souřadnice...
                this.SetSplitterByBounds(new Rectangle(x, y, width, height));  // A pak podle souřadnic nastavíme Splitter
            }
        }
        /// <summary>
        /// Vypočítá správné vnější souřadnice Splitteru a uloží je do base.Bounds; volitelně vyvolá invalidaci = překreslení.
        /// <para/>
        /// Tato metoda se aktivně brání rekurzivnímu vyvolání (k čemuž může dojít při použití techniky "TransferToParent").
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <param name="withInvalidate"></param>
        protected void RecalculateBounds(Rectangle? workingBounds = null, bool withInvalidate = false)
        {
            if (RecalculateBoundsInProgress) return;
            try
            {
                RecalculateBoundsInProgress = true;

                Rectangle bounds = GetCurrentBounds(workingBounds);
                // Splitter umisťuje jen sám sebe:
                if (bounds != this.Bounds)
                    base.SetBoundsCore(bounds.X, bounds.Y, bounds.Width, bounds.Height, BoundsSpecified.All);   // Tato metoda REÁLNĚ nastaví Bounds this controlu.
                if (withInvalidate && Initialized) Invalidate();
            }
            finally
            {
                RecalculateBoundsInProgress = false;
            }
        }
        /// <summary>
        /// Obsahuje true, pokud aktuálně probíhá výkon metody <see cref="SplitterControl.RecalculateBounds(Rectangle?, bool)"/>, v té době nebude spouštěna další iterace této metody
        /// </summary>
        protected bool RecalculateBoundsInProgress { get; private set; } = false;
        /// <summary>
        /// Vrátí aktuální souřadnice prvku (Bounds) pro jeho umístění = nikoli pro jeho vykreslení.
        /// Souřadnice určí na základě pozice splitteru <see cref="SplitterControl.SplitPosition"/> a jeho orientace <see cref="SplitterControl.Orientation"/>, 
        /// jeho šíři <see cref="SplitterControl.SplitThick"/>
        /// a na základě pracovních souřadnic dle parametru <paramref name="workingBounds"/>, viz i metoda <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/>.
        /// <para/>
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        /// <param name="workingBounds">Pracovní souřadnice Splitteru = vnější, jsou získané metodou <see cref="SplitterControl.GetCurrentWorkingBounds(Rectangle)"/></param>
        /// <returns></returns>
        protected virtual Rectangle GetCurrentBounds(Rectangle? workingBounds = null)
        {
            int sp = CurrentSplitPosition;
            int th = (SplitThick / 2);
            Rectangle cwb = workingBounds ?? CurrentWorkingBounds;
            switch (_Orientation)
            {
                case Orientation.Horizontal:
                    return new Rectangle(cwb.X, sp - th, cwb.Width, SplitThick);
                case Orientation.Vertical:
                    return new Rectangle(sp - th, cwb.Y, SplitThick, cwb.Height);
            }
            return Rectangle.Empty;
        }
        /// <summary>
        /// Metoda vrátí souřadnice vizuální (akceptující AutoScroll) pro dané souřadnice logické
        /// </summary>
        /// <param name="logicalBounds"></param>
        /// <returns></returns>
        protected Rectangle GetCurrentRectangle(Rectangle logicalBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return logicalBounds;
            return new Rectangle(logicalBounds.X + currentOrigin.X, logicalBounds.Y + currentOrigin.Y, logicalBounds.Width, logicalBounds.Height);
        }
        /// <summary>
        /// Metoda vrátí souřadnice logické (akceptující původní bod 0/0) pro dané souřadnice vizuálně, akceptujíc AutoScroll
        /// </summary>
        /// <param name="currentBounds"></param>
        /// <returns></returns>
        protected Rectangle GetLogicalRectangle(Rectangle currentBounds)
        {
            Point currentOrigin = CurrentOrigin;
            if (currentOrigin.IsEmpty) return currentBounds;
            return new Rectangle(currentBounds.X - currentOrigin.X, currentBounds.Y - currentOrigin.Y, currentBounds.Width, currentBounds.Height);
        }
        /// <summary>
        /// Souřadnice bodu 0/0.
        /// On totiž počáteční bod ve WinForm controlech může být posunutý, pokud Parent control je typu <see cref="ScrollableControl"/> s aktivním scrollingem.
        /// </summary>
        protected Point CurrentOrigin
        {
            get
            {
                if (!(this.Parent is ScrollableControl parent) || !parent.AutoScroll) return Point.Empty;
                return parent.AutoScrollPosition;
            }
        }
        /// <summary>
        /// Obsahuje velikost plochy Parenta, ve které se může pohybovat splitter
        /// </summary>
        protected Size CurrentParentSize
        {
            get
            {
                var parent = this.Parent;
                if (parent is null) return new Size(MaxSize, MaxSize);
                if (parent is ScrollableControl scrollParent)
                    return scrollParent.ClientSize;
                return parent.ClientSize;
            }
        }
        /// <summary>
        /// Aktuální pracovní souřadnice Splitteru. Určuje je potomek ve virtual metodě <see cref="GetCurrentWorkingBounds(Rectangle)"/>.
        /// Výsledné souřadnice posune o AutoScroll position <see cref="CurrentOrigin"/>.
        /// </summary>
        protected Rectangle CurrentWorkingBounds
        {
            get
            {
                Rectangle currentArea = new Rectangle(CurrentOrigin, CurrentParentSize);
                Rectangle currentWorkingBounds = GetCurrentWorkingBounds(currentArea);
                return currentWorkingBounds;
            }
        }
        #endregion
        #region Splitter si hlídá svého parenta, aby zareagoval na jeho barvy
        /// <summary>
        /// Po změně Parenta
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            DetectCurrentParent();
            if (this.SplitterColorByParent) this.Invalidate();
        }
        /// <summary>
        /// Reaguje na změnu parenta
        /// </summary>
        protected void DetectCurrentParent()
        {
            Control parentNew = this.Parent;
            Control parentOld = _ParentOld;
            if (Object.ReferenceEquals(parentNew, parentOld)) return;        // Pokud oba jsou null, nebo oba jsou totéž, nemusím nic dělat

            if (parentOld != null)
            {
                parentOld.ControlAdded -= _CurrentParent_ControlAdded;
                _ParentOld = null;
            }
            if (parentNew != null)
            {
                parentNew.ControlAdded += _CurrentParent_ControlAdded;
                _ParentOld = parentNew;
            }
        }
        /// <summary>
        /// Když si můj parent přidá jakýkoli nový control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CurrentParent_ControlAdded(object sender, ControlEventArgs e)
        {
            BringSplitterToFront(false);
        }
        private Control _ParentOld;
        #endregion
        #region Splitter je rád, když je úplně nahoře v Z-Order
        /// <summary>
        /// Přemístí this splitter nahoru v poli controlů našeho parenta.
        /// <para/>
        /// Parametr <paramref name="isMouseEnter"/> říká:
        /// true = metoda je volána z události MouseEnter, je požadováno aby this splitter byl naprosto navrchu;
        /// false = metoda je volána z události Parent.ControlAdded, je požadováno aby nad this splitterem byly už pouze jiné splittery.
        /// </summary>
        /// <param name="isMouseEnter">Informace: true = voláno z MouseEnter, false = volánoz Parent.ControlAdded</param>
        protected void BringSplitterToFront(bool isMouseEnter)
        {
            var allControls = AllControls;
            if (allControls.Count <= 1) return;            // Pokud nejsou žádné prvky (=blbost, jsem tu já), anebo je jen jeden prvek (to jsem já), pak není co řešit...
            int index = allControls.FindIndex(c => object.ReferenceEquals(c, this));
            if (index <= 0) return;                        // Pokud já jsem na indexu [0] (tj. úplně nahoře), anebo tam nejsem vůbec (blbost), pak není co řešit

            // Já nejsem na pozici [0] = někdo je ještě nade mnou:
            bool bringToFront = false;
            if (isMouseEnter)
                // Máme být úplně navrchu:
                bringToFront = true;
            else
            {   // Nad námi smí být pouze jiné splittery:
                for (int i = 0; i < index && !bringToFront; i++)
                {
                    if (!(allControls[i] is DxSimpleSplitter))
                        bringToFront = true;
                }
            }

            // Dáme sebe (=Splitter) nahoru:
            if (bringToFront)
                this.BringToFront();
        }
        /// <summary>
        /// Pole Child controlů mého Parenta = "moji sourozenci včetně mě".
        /// Pokud ještě nemám parenta, pak toto pole obsahuje jen jeden prvek a to jsem já.
        /// Pokud má vrácené pole více prvků, pak někde v něm budu i já = 'CurrentParent' :-).
        /// <para/>
        /// Index na pozici [0] je úplně nahoře nade všemi, postupně jsou prvky směrem dolů...
        /// Pozici aktuální prvku 
        /// </summary>
        protected List<Control> AllControls
        {
            get
            {
                var parent = this.Parent;
                if (parent == null) return new List<Control> { this };
                return parent.Controls.OfType<Control>().ToList();
            }
        }
        #endregion
        #region Enumy těsně svázané se Splitterem
        /// <summary>
        /// Režim zobrazování vizuálního loga (například čtyřtečka) uprostřed splitbaru (při velikosti <see cref="SplitThick"/> nad 4px)
        /// </summary>
        public enum LogoMode
        {
            /// <summary>
            /// Nikdy
            /// </summary>
            None = 0,
            /// <summary>
            /// Jen pod myší
            /// </summary>
            OnMouse,
            /// <summary>
            /// Vždy
            /// </summary>
            Allways
        }
        /// <summary>
        /// Akce prováděné po vložení hodnot do splitteru
        /// </summary>
        [Flags]
        protected enum SetActions
        {
            /// <summary>
            /// Žádná akce
            /// </summary>
            None = 0,
            /// <summary>
            /// Povinně provést akce, i když nebude detekována žádná změna hodnoty
            /// </summary>
            ForceActions = 0x0001,
            /// <summary>
            /// Přepočítat souřadnice splitteru
            /// </summary>
            RecalculateBounds = 0x0010,
            /// <summary>
            /// Přemístit navázané controly podle režimu aktivity
            /// </summary>
            MoveControlsByActivityMode = 0x0100,
            /// <summary>
            /// Přemístit navázané controly vždy = bez ohledu na režim aktivity
            /// </summary>
            MoveControlsAlways = 0x0200,
            /// <summary>
            /// Vyvolat událost Changing (stále probíhá změna)
            /// </summary>
            CallEventChanging = 0x1000,
            /// <summary>
            /// Vyvolat událost Changed (změna proběhla a je dokončena)
            /// </summary>
            CallEventChanged = 0x2000,

            /// <summary>
            /// Defaultní sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/> + <see cref="CallEventChanged"/>, ale žádné násilí
            /// </summary>
            Default = RecalculateBounds | MoveControlsByActivityMode | CallEventChanged,
            /// <summary>
            /// Tichá sada akcí: <see cref="RecalculateBounds"/> + <see cref="MoveControlsByActivityMode"/>, ale žádné eventy a žádné násilí
            /// </summary>
            Silent = RecalculateBounds | MoveControlsByActivityMode
        }
        #endregion
    }
    #endregion
    #region DxPopupMenu
    /// <summary>
    /// <see cref="DxPopupMenu"/> : potomek <see cref="DevExpress.XtraBars.PopupMenu"/>
    /// </summary>
    public class DxPopupMenu : DevExpress.XtraBars.PopupMenu
    {
        #region Konstruktor, public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxPopupMenu() : base()
        {
            _Initialise();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="barManager"></param>
        public DxPopupMenu(DevExpress.XtraBars.BarManager barManager) : base(barManager)
        {
            _Initialise();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="container"></param>
        public DxPopupMenu(IContainer container) : base(container)
        {
            _Initialise();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void _Initialise()
        {
            DrawMenuSideStrip = DefaultBoolean.True;
            MenuDrawMode = DevExpress.XtraBars.MenuDrawMode.SmallImagesText;
        }
        #endregion
        #region Static tvorba prvků menu
        /// <summary>
        /// Vytvoří a vrátí <see cref="DxPopupMenu"/> s danými prvky a parametry
        /// </summary>
        /// <param name="menuItems">Prvky definující menu</param>
        /// <param name="menuItemClick">Handler události MenuItemClick</param>
        /// <param name="onDemandLoad">Handler události OnDemandLoad</param>
        /// <param name="explicitManager">Explicitně dodaný BarManager</param>
        /// <returns></returns>
        public static DxPopupMenu CreateDxPopupMenu(IEnumerable<IMenuItem> menuItems, EventHandler<MenuItemClickArgs> menuItemClick = null, EventHandler<OnDemandLoadArgs> onDemandLoad = null, DevExpress.XtraBars.BarManager explicitManager = null)
        {
            var barManager = explicitManager ?? DxComponent.DefaultBarManager;

            var dxPopupMenu = new DxPopupMenu(barManager);
            barManager.ShowScreenTipsInMenus = true;
            DevExpress.XtraBars.BarItem[] barItems = _CreateDxPopupMenuItems(dxPopupMenu, menuItems);
            dxPopupMenu.AddItems(barItems);
            if (menuItemClick != null) dxPopupMenu.MenuItemClick += menuItemClick;
            if (onDemandLoad != null) dxPopupMenu.OnDemandLoad += onDemandLoad;

            return dxPopupMenu;
        }
        /// <summary>
        /// Z dodané kolekce datových prvků <see cref="IMenuItem"/> vytvoří a vrátí pole vizuálních prvků <see cref="DevExpress.XtraBars.BarItem"/>.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="menuItems"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem[] _CreateDxPopupMenuItems(DxPopupMenu dxPopupMenu, IEnumerable<IMenuItem> menuItems)
        {
            var barItems = new List<DevExpress.XtraBars.BarItem>();
            IMenuHeaderItem currentHeaderItem = null;
            if (menuItems != null)
            {
                foreach (var menuItem in menuItems)
                {
                    if (menuItem is null) continue;

                    // Různé podtypy prvků menu:
                    if (menuItem is IMenuHeaderItem headerItem) _AddMenuHeader(dxPopupMenu, barItems, headerItem, ref currentHeaderItem);
                    else if (menuItem.SubItemsIsOnDemand) _AddMenuOnDemandItem(dxPopupMenu, barItems, menuItem, currentHeaderItem);
                    else if (menuItem.SubItems != null) _AddMenuSubMenu(dxPopupMenu, barItems, menuItem, currentHeaderItem);
                    else _AddMenuItem(dxPopupMenu, barItems, menuItem, currentHeaderItem);
                }
            }
            return barItems.ToArray();
        }
        /// <summary>
        /// Vytvoří a vrátí pole prvků pro OnDemand SubMenu, když nejsou dodány žádné explicitní prvky.
        /// Výstupní pole bude obsahovat jeden prvek s obyčejným buttonem s textem "...".
        /// Pokud by nadřízený prvek <see cref="DxBarSubItem"/> neobsahoval žádný prvek (tedy ani trojtečku), 
        /// pak by DevExpress neměl snahu toto menu otevřít, neproběhne event Popup a neprovede se žádost o donačtení reálných prvků menu <see cref="OnDemandLoad"/>.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem[] _CreateDxPopupOnDemandMockItem(DxPopupMenu dxPopupMenu)
        {
            var barItems = new List<DevExpress.XtraBars.BarItem>();
            var stdButton = new DevExpress.XtraBars.BarButtonItem(dxPopupMenu.Manager, "...");
            barItems.Add(stdButton);
            return barItems.ToArray();
        }
        /// <summary>
        /// Do pole <paramref name="barItems"/> přidá prvek Header odpovídající dané deklaraci <paramref name="headerItem"/>. Tuto deklaraci umístí do ref parametru <paramref name="currentHeaderItem"/>, 
        /// protože pro následující prvky reprezentuje deklaraci "jejich" skupiny.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="barItems"></param>
        /// <param name="headerItem"></param>
        /// <param name="currentHeaderItem"></param>
        private static void _AddMenuHeader(DxPopupMenu dxPopupMenu, List<DevExpress.XtraBars.BarItem> barItems, IMenuHeaderItem headerItem, ref IMenuHeaderItem currentHeaderItem)
        {
            if (headerItem is null) return;

            bool isHeaderVisible = true;              // Není cesta, jak Header nechat funkční (tj. včetně MultiColumns) a přitom jej nezobrazit...
            bool isMultiColumn = (headerItem.IsMultiColumn && headerItem.ColumnCount.HasValue && headerItem.ColumnCount.Value > 1);
            var header = new DxBarHeaderItem();
            header.Caption = (isHeaderVisible ? headerItem.Text : "");
            header.IsHidden = !isHeaderVisible;
            header.RibbonStyle = (headerItem.UseLargeImages ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large : DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText);
            header.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionInMenu;
            header.ContentHorizontalAlignment = DevExpress.XtraBars.BarItemContentAlignment.Near;
            header.ImageToTextAlignment = DevExpress.XtraBars.BarItemImageToTextAlignment.BeforeText;
            // header.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;              // Header není zobrazen, ale buttony pak nejsou řízeny zdejším OptionMultiColumns...

            header.MultiColumn = (isMultiColumn ? DefaultBoolean.True : DefaultBoolean.False);
            header.OptionsMultiColumn.ColumnCount = (isMultiColumn ? headerItem.ColumnCount.Value : 1);
            header.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Left;
            header.OptionsMultiColumn.ImageVerticalAlignment = DevExpress.Utils.Drawing.ItemVerticalAlignment.Top;
            header.OptionsMultiColumn.LargeImages = (headerItem.UseLargeImages ? DefaultBoolean.True : DefaultBoolean.False);
            header.OptionsMultiColumn.UseMaxItemWidth = DevExpress.Utils.DefaultBoolean.False;
            barItems.Add(header);

            currentHeaderItem = headerItem;
        }
        /// <summary>
        /// Do pole <paramref name="barItems"/> přidá prvek <see cref="DxBarSubItem"/> definující OnDemand SubMenu.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="barItems"></param>
        /// <param name="menuItem"></param>
        /// <param name="currentHeaderItem"></param>
        private static void _AddMenuOnDemandItem(DxPopupMenu dxPopupMenu, List<DevExpress.XtraBars.BarItem> barItems, IMenuItem menuItem, IMenuHeaderItem currentHeaderItem)
        {
            var barSubItem = _GetMenuItem(dxPopupMenu, menuItem, currentHeaderItem, true) as DxBarSubItem;
            var subItems = (menuItem.SubItems != null && menuItem.SubItems.Any() ?
                _CreateDxPopupMenuItems(dxPopupMenu, menuItem.SubItems) :
                _CreateDxPopupOnDemandMockItem(dxPopupMenu));
            barSubItem.Popup += dxPopupMenu._BarSubItemPopup;
            barSubItem.AddItems(subItems);
            barItems.Add(barSubItem);
        }
        /// <summary>
        /// Do pole <paramref name="barItems"/> přidá prvek <see cref="DxBarSubItem"/> definující Static SubMenu a jeho zadané prvky.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="barItems"></param>
        /// <param name="menuItem"></param>
        /// <param name="currentHeaderItem"></param>
        private static void _AddMenuSubMenu(DxPopupMenu dxPopupMenu, List<DevExpress.XtraBars.BarItem> barItems, IMenuItem menuItem, IMenuHeaderItem currentHeaderItem)
        {
            var barSubItem = _GetMenuItem(dxPopupMenu, menuItem, currentHeaderItem, true) as DxBarSubItem;
            var subItems = _CreateDxPopupMenuItems(dxPopupMenu, menuItem.SubItems);
            barSubItem.AddItems(subItems);
            barItems.Add(barSubItem);
        }
        /// <summary>
        /// Do pole <paramref name="barItems"/> přidá prvek typu <see cref="DevExpress.XtraBars.BarItem"/> podle zadané deklarace.
        /// Tato metoda neřeší prvky SubMenu, to řeší nadřízená metoda <see cref="_CreateDxPopupMenuItems(DxPopupMenu, IEnumerable{IMenuItem})"/>. 
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="barItems"></param>
        /// <param name="menuItem"></param>
        /// <param name="currentHeaderItem"></param>
        private static void _AddMenuItem(DxPopupMenu dxPopupMenu, List<DevExpress.XtraBars.BarItem> barItems, IMenuItem menuItem, IMenuHeaderItem currentHeaderItem)
        {
            var barItem = _GetMenuItem(dxPopupMenu, menuItem, currentHeaderItem, false);
            barItems.Add(barItem);
        }
        /// <summary>
        /// Vytvoří a vrátí prvek typu <see cref="DevExpress.XtraBars.BarItem"/> pro danou deklaraci.
        /// Pokud v parametru <paramref name="createSubItem"/> je true, vždy vytvoří <see cref="DxBarSubItem"/>.
        /// Tato metoda neřeší prvky SubMenu, to řeší nadřízená metoda <see cref="_CreateDxPopupMenuItems(DxPopupMenu, IEnumerable{IMenuItem})"/> nebo některá další, tato metoda vytvoří jen Parent prvek.
        /// </summary>
        /// <param name="dxPopupMenu"></param>
        /// <param name="menuItem"></param>
        /// <param name="currentHeaderItem"></param>
        /// <param name="createSubItem"></param>
        /// <returns></returns>
        private static DevExpress.XtraBars.BarItem _GetMenuItem(DxPopupMenu dxPopupMenu, IMenuItem menuItem, IMenuHeaderItem currentHeaderItem, bool createSubItem)
        {
            bool isMultiColumn = (currentHeaderItem != null && currentHeaderItem.IsMultiColumn);

            DevExpress.XtraBars.BarItem barItem = null;
            bool isSubItem = false;
            if (createSubItem || (menuItem.SubItems != null && menuItem.SubItems.Any()))
            {   // Parent od SubMenu:
                var subItem = new DxBarSubItem(dxPopupMenu.Manager, menuItem.Text);
                isSubItem = true;
                barItem = subItem;
            }
            else
            {   // Jednotkový prvek:
                switch (menuItem.ItemType)
                {
                    case MenuItemType.MenuItem:
                        var stdButton = new DevExpress.XtraBars.BarButtonItem(dxPopupMenu.Manager, menuItem.Text);
                        stdButton.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.Default;
                        barItem = stdButton;
                        break;
                    case MenuItemType.DownButton:
                        var downButton = new DevExpress.XtraBars.BarButtonItem(dxPopupMenu.Manager, menuItem.Text);
                        downButton.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.Check;
                        downButton.Down = menuItem.Checked ?? false;
                        barItem = downButton;
                        break;
                    case MenuItemType.CheckBox:
                        var checkBoxButton = new DevExpress.XtraBars.BarCheckItem(dxPopupMenu.Manager);
                        checkBoxButton.Caption = menuItem.Text;
                        checkBoxButton.CheckStyle = DevExpress.XtraBars.BarCheckStyles.Standard;
                        checkBoxButton.Checked = menuItem.Checked ?? false;
                        barItem = checkBoxButton;
                        break;
                }
            }
            if (barItem is null) return null;

            // Společné nastavení:
            barItem.Name = menuItem.ItemId;
            barItem.Enabled = menuItem.Enabled;
            barItem.Visibility = (menuItem.Visible ? DevExpress.XtraBars.BarItemVisibility.Always : DevExpress.XtraBars.BarItemVisibility.Never);
            if (menuItem.FontStyle.HasValue) barItem.ItemAppearance.Normal.FontStyleDelta = menuItem.FontStyle.Value;

            barItem.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.Standard;
            barItem.RibbonStyle = (isMultiColumn ? DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large : DevExpress.XtraBars.Ribbon.RibbonItemStyles.SmallWithText);
            barItem.AllowGlyphSkinning = DefaultBoolean.False;
            barItem.Tag = menuItem;

            DxComponent.ApplyImage(barItem.ImageOptions, menuItem.ImageName);

            if (!isSubItem)
            {   // Obyčejné prvky, nikoliv Parent od SubMenu:
                barItem.SuperTip = DxComponent.CreateDxSuperTip(menuItem);
                DxComponent.FillBarItemHotKey(barItem, menuItem);
                barItem.AllowRightClickInMenu = false;
                barItem.ItemClick += dxPopupMenu._BarItemClick;
            }

            return barItem;
        }
        #endregion
        #region Události
        protected override void OnPopup()
        {
            base.OnPopup();
        }
        protected override PopupMenuBarControl CreatePopupControl(DevExpress.XtraBars.BarManager manager)
        {
            var control = base.CreatePopupControl(manager);
            return control;
        }

        protected override PopupMenuBarControl CreateSubControl(DevExpress.XtraBars.BarManager manager)
        {
            var control = base.CreateSubControl(manager);
            return control;
        }

        protected override void OnCloseUp(CustomPopupBarControl prevControl)
        {
            base.OnCloseUp(prevControl);
        }
        /// <summary>
        /// Po kliknutí na prvek menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BarItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (e.Item.Tag is IMenuItem menuItem)
                this.RunMenuItemClick(new MenuItemClickArgs(menuItem));
        }
        /// <summary>
        /// Událost vyvolaná v okamžiku, kdy potřebujeme donačíst OnDemand položky
        /// </summary>
        public event EventHandler<OnDemandLoadArgs> OnDemandLoad;
        /// <summary>
        /// Proběhne v okamžiku, kdy potřebujeme donačíst OnDemand položky
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnOnDemandLoad(OnDemandLoadArgs args)
        { }
        /// <summary>
        /// Zavolá metodu <see cref="OnOnDemandLoad(OnDemandLoadArgs)"/> a handler <see cref="OnDemandLoad"/>
        /// </summary>
        /// <param name="args"></param>
        protected void RunOnDemandLoad(OnDemandLoadArgs args)
        {
            OnOnDemandLoad(args);
            OnDemandLoad?.Invoke(this, args);
        }

        /// <summary>
        /// Událost vyvolaná v okamžiku, kdy uživatel klikl na nějaký prvek menu
        /// </summary>
        public event EventHandler<MenuItemClickArgs> MenuItemClick;
        /// <summary>
        /// Proběhne v okamžiku, kdy uživatel klikl na nějaký prvek menu
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnMenuItemClick(MenuItemClickArgs args)
        { }
        /// <summary>
        /// Zavolá metodu <see cref="OnMenuItemClick(MenuItemClickArgs)"/> a handler <see cref="MenuItemClick"/>
        /// </summary>
        /// <param name="args"></param>
        protected void RunMenuItemClick(MenuItemClickArgs args)
        {
            OnMenuItemClick(args);
            MenuItemClick?.Invoke(this, args);
        }
        #endregion
        #region OnDemand SubItems - řízení, donačtení
        /// <summary>
        /// Událost, kdy se otevírá SubMenu s režimem OnDemand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _BarSubItemPopup(object sender, EventArgs e)
        {
            if (sender is DxBarSubItem barSubItem)
            {   // Popup budu řešit jen jedenkrát:
                barSubItem.Popup -= this._BarSubItemPopup;
                if (barSubItem.Tag is IMenuItem menuItem && menuItem.SubItemsIsOnDemand)
                {
                    OnDemandLoadArgs args = new OnDemandLoadArgs(this, barSubItem, menuItem);
                    this.RunOnDemandLoad(args);
                }
            }
        }
        /// <summary>
        /// Zajistí zobrazení reálných položek SubItem pro OnDemand menu
        /// </summary>
        /// <param name="args"></param>
        private void _BarSubItemFill(OnDemandLoadArgs args)
        {
            if (args.MenuItem.SubItems is null) return;
            DxComponent.InvokeGuiThread(() => _BarSubItemFillGui(args), this.Manager.Form, true);
        }
        /// <summary>
        /// Zajistí zobrazení reálných položek SubItem pro OnDemand menu. Volat v GUI threadu.
        /// </summary>
        /// <param name="args"></param>
        private void _BarSubItemFillGui(OnDemandLoadArgs args)
        {
            var barSubItem = args.BarSubItem;
            barSubItem.Items = _CreateDxPopupMenuItems(this, args.MenuItem?.SubItems);
            if (args.IsDynamic)
                barSubItem.Popup += this._BarSubItemPopup;
        }
        /// <summary>
        /// Data pro metodu, která načítá SubItem v režimu OnDemand
        /// </summary>
        public class OnDemandLoadArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="dxPopupMenu"></param>
            /// <param name="barSubItem"></param>
            /// <param name="menuItem"></param>
            internal OnDemandLoadArgs(DxPopupMenu dxPopupMenu, DxBarSubItem barSubItem, IMenuItem menuItem)
            {
                this.PopupMenu = dxPopupMenu;
                this.BarSubItem = barSubItem;
                this.MenuItem = menuItem;
                this.IsDynamic = false;
            }
            /// <summary>
            /// Fyzické menu, které je rozsvíceno
            /// </summary>
            internal DxPopupMenu PopupMenu { get; private set; }
            /// <summary>
            /// Fyzické submenu, které má být naplněno
            /// </summary>
            internal DxBarSubItem BarSubItem { get; private set; }
            /// <summary>
            /// Položka definující prvek, jehož SubMenu se načítá
            /// </summary>
            public IMenuItem MenuItem { get; set; }
            /// <summary>
            /// Menu je plně dynamické = příští otevření má opět vyvolat událost <see cref="OnDemandLoad"/>.
            /// Default = false = po prvním načtení SubItems se opakovaně neprovádí.
            /// </summary>
            public bool IsDynamic { get; set; }
            /// <summary>
            /// Jednoduše zajistí vytvoření reálných aktuálních položek SubMenu.
            /// </summary>
            public void FillSubMenu()
            {
                this.PopupMenu._BarSubItemFill(this);
            }
        }
        #endregion
    }
    #region DxBarHeaderItem
    internal class DxBarHeaderItem : DevExpress.XtraBars.BarHeaderItem
    {
        public DxBarHeaderItem()
        {

        }

        public bool IsHidden { get; set; }
    }
    #endregion
    #region DxBarSubItem
    /// <summary>
    /// Prvek menu obsahující SubMenu
    /// </summary>
    internal class DxBarSubItem : DevExpress.XtraBars.BarSubItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxBarSubItem() : base()
        {
            Initialise();
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="caption"></param>
        public DxBarSubItem(DevExpress.XtraBars.BarManager manager, string caption) : base(manager, caption)
        {
            Initialise();
        }
        /// <summary>
        /// Inicializace
        /// </summary>
        private void Initialise()
        { }
        public override void PerformClick()
        {

            base.PerformClick();

        }
        /// <summary>
        /// Prvky menu. Lze setovat = dojde k nahrazení stávajícího obsahu.
        /// </summary>
        public DevExpress.XtraBars.BarItem[] Items
        {
            get { return this.ItemLinks.Select(l => l.Item).ToArray(); }
            set { _SetItems(value); }
        }
        /// <summary>
        /// Setuje prvky menu
        /// </summary>
        /// <param name="items"></param>
        private void _SetItems(DevExpress.XtraBars.BarItem[] items)
        {
            var isOpened = this.IsOpened();
            this.OnCloseUp();

            this.BeginUpdate();
            this.ItemLinks.Clear();
            this.EndUpdate();
            this.Refresh();

            this.BeginUpdate();
            if (items != null && items.Length > 0)
                this.AddItems(items);
            this.EndUpdate();
            this.Refresh();


            //  Hledal jsem způsob, jak donutit DevExpress komponentu, aby po načtení nových položek menu znovu vypočítala umístění Popup,
            //  tak aby se zespodu monitoru posunulo kousek nahoru, ale nedaří se:
            // this.Reset();
            // this.Refresh();
            // this.PerformClick();
        }
    }
    #endregion
    #endregion
    #region DxImageAreaMap : klikací mapa, obsahuje obrázek a jednotlivé prostory v něm, které jsou různě aktivní.
    /// <summary>
    /// <see cref="DxImageAreaMap"/> : klikací mapa, obsahuje obrázek a jednotlivé prostory v něm, které jsou různě aktivní.
    /// <para/>
    /// Klikací mapa se používá typicky v tomto scénáři:
    /// <code>
    /// DxImageAreaMap __ImageMap;
    /// __ImageMap = new DxImageAreaMap();
    /// __ImageMap.ContentImage = (bytové pole se zdrojem obrázku);
    /// __ImageMap.Zoom = 0.40f;                                    // velikost obrázku vůči ploše controlu
    /// __ImageMap.RelativePosition = new PointF(0.04f, 0.96f);     // relativní umístění obrázku v rámci plochy controlu
    /// __ImageMap.AddActiveArea(...);                // definice aktivního prostoru, lze jich zadat více
    ///   nebo
    /// __ImageMap.AddActiveWholeArea(...);           // zjedodušená aktivace celého prostoru
    /// __ImageMap.Click += zdejší handler události kliknutí na mapu
    /// __ImageMap.OwnerControl = ownerControl;       // Napojení na Control, v němž má být mapa aktivní
    /// ...
    /// // na konci života formuláře:
    /// __ImageMap.Dispose()                          
    /// ...
    /// // Je nezbytné zajistit vykreslení mapy, tedy do události ownerControl.Paint přidat kód:
    /// var clientBounds = this.ClientRectangle;       // a uvnitř celého prostoru ownera vyhradíme disponibilní prostor:
    /// var innerBounds = Rectangle.FromLTRB(clientBounds.Left + 36, clientBounds.Top + 48, clientBounds.Right - 36, clientBounds.Bottom - 36);
    /// __ImageMap.PaintImageMap(e.GraphicsCache, innerBounds);     // Vykreslí image do patřičného místa v daném prostoru a zapamatuje si aktuální souřadnice aktivních prostor
    ///    nebo
    /// __ImageMap.PaintImageMap(e.Graphics, innerBounds);          // Vykreslí image - pouze ale Bitmapu!!! do patřičného místa v daném prostoru a zapamatuje si aktuální souřadnice aktivních prostor
    /// </code>
    /// </summary>
    internal class DxImageAreaMap : IDisposable
    {
        #region Konstruktor, Dispose, akceptování a reset objektů
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxImageAreaMap() 
        {
            __AreaItems = new List<Area>();
            __Zoom = 1f;
            __RelativePosition = new PointF(0.50f, 0.50f);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _DeactivateDelays();
            _ResetOwnerControl();
            _ResetBitmapImage();
            _ResetContentImage();
            __SvgImage = null;
        }
        private Control __OwnerControl;
        private byte[] __ContentImage;
        private Image __BmpImage;
        private bool? __BitmapImageIsExternal;
        private DevExpress.Utils.Svg.SvgImage __SvgImage;
        private float __Zoom;
        private float? __BmpZoomMaxRatio;
        private PointF __RelativePosition;
        private TimeSpan? __InitialDelay;
        private TimeSpan? __ResizeDelay;
        private bool __HasImage;
        /// <summary>
        /// Naváže se do daného Controlu (naváže zdejší handlery na události Controlu)
        /// </summary>
        /// <param name="ownerControl"></param>
        private void _SetOwnerControl(Control ownerControl)
        {
            _ResetOwnerControl();
            if (ownerControl != null)
            {
                ownerControl.ClientSizeChanged += _OwnerControl_ClientSizeChanged;
                ownerControl.MouseEnter += _OwnerControl_MouseEnter;
                ownerControl.MouseMove += _OwnerControl_MouseMove;
                ownerControl.MouseDown += _OwnerControl_MouseDown;
                ownerControl.MouseUp += _OwnerControl_MouseUp;
                ownerControl.MouseLeave += _OwnerControl_MouseLeave;
                _ActivateInitialDelay();
            }
            __OwnerControl = ownerControl;
        }
        /// <summary>
        /// Odváže zdejší handlery od události stávajícího Controlu a nuluje referenci na něj
        /// </summary>
        private void _ResetOwnerControl()
        {
            var ownerControl = __OwnerControl;
            if (ownerControl != null)
            {
                ownerControl.MouseEnter -= _OwnerControl_MouseEnter;
                ownerControl.MouseMove -= _OwnerControl_MouseMove;
                ownerControl.MouseDown -= _OwnerControl_MouseDown;
                ownerControl.MouseUp -= _OwnerControl_MouseUp;
                ownerControl.MouseLeave -= _OwnerControl_MouseLeave;
            }
            __OwnerControl = null;
            _DeactivateDelays();
        }
        /// <summary>
        /// Uloží si dodaný obrázek jako externí
        /// </summary>
        /// <param name="bmpImage"></param>
        private void _SetBitmapImage(Image bmpImage)
        {
            _ResetContentImage();
            __BmpImage = bmpImage;
            __BitmapImageIsExternal = ((bmpImage != null) ? (bool?)true : null);      // true = Image je externí (tedy pokud je nějaký dodán)
            _LoadBmpImageSize(bmpImage);
        }
        /// <summary>
        /// Zruší referenci na bitmapu, a pokud byla interní (tj. námi vytvořená), tak ji nejprve Disposuje
        /// </summary>
        private void _ResetBitmapImage()
        {
            var bitmapImage = __BmpImage;
            if (bitmapImage != null && __BitmapImageIsExternal.HasValue && !__BitmapImageIsExternal.Value)
                bitmapImage.Dispose();
            __BmpImage = null;
            __BitmapImageIsExternal = null;
            __HasImage = false;
        }
        /// <summary>
        /// Uloží si bytové pole obsahující obrázek, a zkusí z něj vytvořit vhodný Image.
        /// </summary>
        /// <param name="contentImage"></param>
        private void _SetContentImage(byte[] contentImage)
        {
            _ResetContentImage();
            if (contentImage is null || contentImage.Length < 16) return;

            // Odhadneme, zda jde o SVG (svg začíná znaky:  <?xml  ):
            bool canBeSvg = (contentImage[0] == '<' &&
                             contentImage[1] == '?' &&
                             contentImage[2] == 'x' &&
                             contentImage[3] == 'm' &&
                             contentImage[4] == 'l' );

            if (canBeSvg && tryLoadSvg()) return;          // Když to vypadá jako SVG, zkusíme to primárně takhle
            if (tryLoadBmp()) return;                      // Zkusíme BMP
            if (!canBeSvg && tryLoadSvg()) return;         // BMP to není, a pokud jsme dosud nezkusili SVG, zkusíme to jako poslední šanci...

            return;

            // Zkusí zpracovat BMP:
            bool tryLoadBmp()
            {
                if (_TryGetBmpImage(contentImage, out var bmpImage))
                {
                    __BmpImage = bmpImage;
                    __BitmapImageIsExternal = false;
                    _LoadBmpImageSize(bmpImage);
                    return true;
                }
                return false;
            }

            // Zkusí zpracovat SVG:
            bool tryLoadSvg()
            { 
                if (_TryGetSvgImage(contentImage, out var svgImage))
                {
                    __SvgImage = svgImage;
                    _LoadSvgImageSize(svgImage);
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Metoda zkusí z dodaného pole byte vytvořit bitmapový obrázek
        /// </summary>
        /// <param name="contentImage"></param>
        /// <param name="bmpImage"></param>
        /// <returns></returns>
        private bool _TryGetBmpImage(byte[] contentImage, out Image bmpImage)
        {
            bmpImage = null;
            try
            {
                using (var ms = new System.IO.MemoryStream(contentImage))
                {
                    bmpImage = Bitmap.FromStream(ms);
                }
            }
            catch
            {
                bmpImage = null;
            }
            return (bmpImage != null);
        }
        /// <summary>
        /// Metoda zkusí z dodaného pole byte vytvořit vektorový obrázek
        /// </summary>
        /// <param name="contentImage"></param>
        /// <param name="svgImage"></param>
        /// <returns></returns>
        private bool _TryGetSvgImage(byte[] contentImage, out DevExpress.Utils.Svg.SvgImage svgImage)
        {
            svgImage = null;

            try
            {
                using (var ms = new System.IO.MemoryStream(contentImage))
                {
                    svgImage = DevExpress.Utils.Svg.SvgImage.FromStream(ms);
                }
            }
            catch
            {
                svgImage = null;
            }
            return (svgImage != null);
        }
        /// <summary>
        /// Zahodí bytové pole s daty obrázku, zahodí bitmapu i SVG image
        /// </summary>
        private void _ResetContentImage()
        {
            _ResetBitmapImage();
            __ContentImage = null;
            __SvgImage = null;
            __HasImage = false;
            _Invalidate();
        }
        /// <summary>
        /// Uloží si dodaný SvgImage
        /// </summary>
        /// <param name="svgImage"></param>
        private void _SetSvgImage(DevExpress.Utils.Svg.SvgImage svgImage)
        {
            _ResetContentImage();
            __SvgImage = svgImage;
            _LoadSvgImageSize(svgImage);
        }
        /// <summary>
        /// Uloží platný Zoom
        /// </summary>
        /// <param name="zoom"></param>
        private void _SetZoom(float zoom)
        {
            __Zoom = _AlignRatio(zoom);
            _Invalidate();
        }
        /// <summary>
        /// Uloží platnou relativní pozici
        /// </summary>
        /// <param name="position"></param>
        private void _SetRelativePosition(PointF position)
        {
            __RelativePosition = new PointF(_AlignRatio(position.X), _AlignRatio(position.Y));
            _Invalidate();
        }
        /// <summary>
        /// Uloží platnou hodnotu do <see cref="__BmpZoomMaxRatio"/>
        /// </summary>
        /// <param name="ratio"></param>
        private void _SetBmpZoomMaxRatio(float? ratio)
        {
            __BmpZoomMaxRatio = (ratio.HasValue ? (ratio.Value > 1f ? ratio : (float?)1) : (float?)null);
            _Invalidate();
        }
        /// <summary>
        /// Zarovná hodnotu do rozmezí 0-1 včetně
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float _AlignRatio(float value)
        {
            return (value < 0f ? 0f : (value > 1f ? 1f : value));
        }
        #endregion
        #region Public rozhraní
        /// <summary>
        /// Obsahuje true, pokud reálně máme navázán Control
        /// </summary>
        public bool HasOwnerControl { get { return (__OwnerControl != null); } }
        /// <summary>
        /// Obsahuje true, pokud byl setován Control (i kdyby bylo setováno null). Je to příznak toho, že se o to volající pokusil.
        /// </summary>
        public bool WasStoredControl { get { return (__OwnerControl != null); } }
        /// <summary>
        /// Vlastník, na něm se odchytávají události myši tak, aby klikací mapa mohla reagovat a klikat.
        /// </summary>
        public Control OwnerControl { get { return __OwnerControl; } set { _SetOwnerControl(value); } }
        /// <summary>
        /// Čas, který uplyne mezi zadáním vlastníka do <see cref="OwnerControl"/> a prvním fyzickým vykreslením Image na něj.
        /// Před uplynutím tohoto času se obrázek nekreslí; důvodem je pravděpodobnost změn velikosti prostoru (umístění okna Desktopu, vkládání dokovacích panelů, Ribbonu, úpravy dokování a velikosti otevřených panelů, atd).
        /// V této inicializační době vykreslování obrázku je víceméně uživatelsky rušivé a může způsobovat problémy i vlastním komponentám.
        /// <para/>
        /// Pokud je zde null (výchozí stav) nebo prázdný nebo záporný čas, akce se provádí okamžitě bez Delay.
        /// </summary>
        public TimeSpan? InitialDelay { get { return __InitialDelay; } set { _SetInitialDelay(value); } }
        /// <summary>
        /// Čas, který uplyne mezi poslední událostí Resize v <see cref="OwnerControl"/> a fyzickým vykreslením vykreslením Image na něj.
        /// Pokud Owner provádí více akcí Resize za sebou, pak pokaždé provede svůj Repaint, a tím by provedl i Repaint zdejšího Image. To vede vizuálně k ne moc pěknému poskakování Image.
        /// Zadáním zdejšího času <see cref="ResizeDelay"/> se po jednotlivém Resize neprovádí přepočet <u>velikosti</u> obrázku, ale jen jeho <u>umístění</u>.
        /// Teprve po uplynutí intervalu je obrázek dopočten přesně a vykreslen znovu.
        /// <para/>
        /// Pokud je zde null (výchozí stav) nebo prázdný nebo záporný čas, akce se provádí okamžitě bez Delay.
        /// </summary>
        public TimeSpan? ResizeDelay { get { return __ResizeDelay; } set { _SetResizeDelay(value); } }
        /// <summary>
        /// Obsahuje true tehdy, když máme připravený obrázek k vykreslení
        /// </summary>
        public bool HasImage { get { return __HasImage; } }
        /// <summary>
        /// Obsah obrázku jako Bytové pole. Interně se rozpozná, zda jde o Image nebo SvgImage.
        /// </summary>
        public byte[] ContentImage { get { return __ContentImage; } set { _SetContentImage(value); } }
        /// <summary>
        /// Máme BMP image?
        /// </summary>
        public bool HasBmpImage { get { return (__BmpImage != null); } }
        /// <summary>
        /// Bitmapový obrázek. 
        /// Pokud bude setován, pak zdejší třída neprovádí jeho Dispose.
        /// Může zde být bitmapa vygenerovaná z dat v <see cref="ContentImage"/>.
        /// </summary>
        public Image BmpImage { get { return __BmpImage; } set { _SetBitmapImage(value); } }
        /// <summary>
        /// Máme SVG image?
        /// </summary>
        public bool HasSvgImage { get { return (__SvgImage != null); } }
        /// <summary>
        /// Vektorový obrázek.
        /// Může zde být SvgImage vygenerovaný z dat v <see cref="ContentImage"/>.
        /// </summary>
        public DevExpress.Utils.Svg.SvgImage SvgImage { get { return __SvgImage; } set { _SetSvgImage(value); } }
        /// <summary>
        /// Měřítko obrázku. Default = 1.00 = vyplní celý dostupný prostor.
        /// Pro obrázek zabírající například 20% plochy vepište hodnotu 0.20f
        /// </summary>
        public float Zoom { get { return __Zoom; } set { _SetZoom(value); } }
        /// <summary>
        /// Maximální poměr mezi nativním rozlišením bitmapy a cílovým vykresleným obrázkem.
        /// Pokud na vstupu bude bitmapa s velikostí 250 x 50 pixel, a cílový prostor by byl 2160 x 1080, pak by se vstupní bitmapa zvětšila cca 8x, a to už bitmapa vypadá spíš pro ostudu.
        /// Je tedy vhodné nastavit zdejší hodnotu na Ratio = 2 až 4; obrázek pak bude zvětšen nanejvýš v tomto poměru, i kdyby <see cref="Zoom"/> byl 1.00 a obrázek by tedy mohl mít velikost 2160 x 432  (odpovídající poměru zdroje 250 x 50).
        /// <para/>
        /// Zdejší hodnota bude ignorována pro SVG obrázek, tam nedochází k pixelovému rozmazání.<br/>
        /// Hodnota NULL = bez omezení (lze tedy obrázek zvětšit bez omezení).<br/>
        /// Hodnoty menší než 1 budou chápány jako 1 (nemá význam zmenšovat vstupní obrázek kvůli kvalitě výsledného obrazu).
        /// </summary>
        public float? BmpZoomMaxRatio { get { return __BmpZoomMaxRatio; } set { _SetBmpZoomMaxRatio(value); } }
        /// <summary>
        /// Umístění obrázku relativně v ploše. Default = { 0.50, 0.50 } = uprostřed.
        /// Hodnota X udává pozici na ose X zleva doprava v rozsahu 0.00 - 1.00;
        /// Hodnota Y udává pozici na ose Y zhora dolů v rozsahu 0.00 - 1.00;
        /// Například pro umístění úplně vlevo téměř dole vepište hodnotu { 0.00f, 0.90f }
        /// </summary>
        public PointF RelativePosition { get { return __RelativePosition; } set { _SetRelativePosition(value); } }
        /// <summary>
        /// Reálná fyzická souřadnice obrázku v koordinátech controlu, v těchto souřadnicích je vykreslen a je aktivní
        /// </summary>
        public Rectangle? CurrentImageBounds { get { return __CurrentImageBounds; } }
        /// <summary>
        /// Smaže deklarace všech prostorů, které byly zadány v <see cref="AddActiveArea(RectangleF, object, DxCursorType, string, string)"/>
        /// </summary>
        public void ClearActiveArea()
        {
            __AreaItems.Clear();
            _Invalidate();
        }
        /// <summary>
        /// Aktivuje celou oblast obrázku jako aktivní.
        /// Po kliknutí kamkoliv na obrázek bude vyvolán event <see cref="Click"/> a v property <see cref="AreaClickArgs.UserData"/> bude zdejší parametr <paramref name="userData"/>.
        /// </summary>
        /// <param name="userData">Identifikace prostoru, bude předána v eventu <see cref="Click"/> do handleru, v property <see cref="AreaClickArgs.UserData"/>.</param>
        /// <param name="cursorType">Typ kurzoru na oblasti</param>
        public void AddActiveWholeArea(object userData = null, DxCursorType cursorType = DxCursorType.Default)
        {
            __AreaItems.Add(new Area(new RectangleF(0f, 0f, 1f, 1f), userData, cursorType, null, null));
            _Invalidate();
        }
        /// <summary>
        /// Přidá další aktivní prostor: jeho umístění relativně v obrázku, jeho identifikátor, volitelně kurzor (a do budoucna Tooltip).
        /// Souřadnice je dána relativne v hodnotách 0.00 - 1.00, relativně k celému obrázku.
        /// Pokud bude definováno více oblastí nad jedním místem (tzn. budou se překrývat na ose Z), pak Aktivní oblast pro konkrétní pozici myši bude ta posledně přidaná pro danou souřadnici.
        /// Ta oblast (resp. její UserData) bude po kliknutí předána do eventu <see cref="Click"/> v property <see cref="AreaClickArgs.UserData"/>.
        /// <para/>
        /// Pokud aplikace nepotřebuje vytvářet detailní klikací mapu, ale stačí jí aktivní celý obrázek, může využít zjednodušenou metodu 
        /// <see cref="AddActiveArea(RectangleF, object, DxCursorType, string, string)"/>, kerá zajistí že celý obrázek bude reprezentovat jednu ucelenou aktivní plochu.
        /// </summary>
        /// <param name="relativeBounds">Relativní souřadnice v rámci celého obrázku, v rozsahu 0.00 - 1.00 </param>
        /// <param name="userData">Identifikace prostoru, bude předána v eventu <see cref="Click"/> do handleru, v property <see cref="AreaClickArgs.UserData"/>.</param>
        /// <param name="cursorType">Typ kurzoru na oblasti</param>
        /// <param name="toolTipTitle">Titulek tooltipu</param>
        /// <param name="toolTipText">Text tooltipu</param>
        public void AddActiveArea(RectangleF relativeBounds, object userData, DxCursorType cursorType = DxCursorType.Hand, string toolTipTitle = null, string toolTipText = null)
        {
            __AreaItems.Add(new Area(relativeBounds, userData, cursorType, toolTipTitle, toolTipText));
            _Invalidate();
        }
        /// <summary>
        /// Obsahuje true, pokud this mapa má nějaké definované oblasti. Pokud nemá, pak celý prostor obrázku tvoří jednu oblast.
        /// </summary>
        public bool HasActiveArea { get { return (__AreaItems.Count > 0); } }
        /// <summary>
        /// Obsahuje true, pokud klikací mapa má použitelné souřadnice
        /// </summary>
        public bool HasValidBounds { get { return __CurrentImageBounds.HasValue; } }

        /// <summary>
        /// Událost, když koncový uživatel klikne v aktivním prostoru.
        /// Pokud volající kód nezadá aktivní prostory (metoda <see cref="AddActiveArea(RectangleF, object, DxCursorType, string, string)"/>),
        /// pak žádná obast obrázku nebude aktivní a nikdy nebude vyvolána tato událost!
        /// Není problém zadat jednu aktivní oblast přes celý obrázek
        /// </summary>
        public event EventHandler<AreaClickArgs> Click;
        /// <summary>
        /// Argument pro událost <see cref="DxImageAreaMap.Click"/>. V property <see cref="UserData"/> je uložena identifikace prostoru.
        /// </summary>
        public class AreaClickArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="userData"></param>
            public AreaClickArgs(object userData)
            {
                this.UserData = userData;
            }
            /// <summary>
            /// Data identifikující cílový prostor, kde uživatel kliknul.
            /// Data byla zadána v metodě <see cref="DxImageAreaMap.AddActiveArea(RectangleF, object, DxCursorType, string, string)"/> jako druhý parametr.
            /// </summary>
            public object UserData { get; private set; }
        }
        #endregion
        #region Požadavek na vykreslení a jeho provedení, akceptování Delay
        /// <summary>
        /// Vykreslí do dané grafiky a v rámci daného prostoru svůj obrázek, a uloží si potřebné souřadnice pro interaktivní mapu
        /// </summary>
        /// <param name="graphicsCache"></param>
        /// <param name="ownerBounds"></param>
        public void PaintImageMap(DevExpress.Utils.Drawing.GraphicsCache graphicsCache, Rectangle ownerBounds)
        {
            if (_IsPaintDelayed(ownerBounds.Size)) return;                   // Odložené kreslení? => posečkáme...

            var imageBounds = this._CalculateImageBounds(ownerBounds);       // Reálný prostor obrázku (odsud si pamatujeme podklady pro interaktivitu)
            if (imageBounds.HasValue)
            {
                if (this.HasBmpImage)
                    graphicsCache.DrawImage(this.BmpImage, imageBounds.Value);
                else if (this.HasSvgImage)
                    graphicsCache.DrawSvgImage(this.SvgImage, imageBounds.Value, null);
            }
        }
        /// <summary>
        /// Vykreslí do dané grafiky a v rámci daného prostoru svůj obrázek, a uloží si potřebné souřadnice pro interaktivní mapu.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="ownerBounds"></param>
        public void PaintImageMap(System.Drawing.Graphics graphics, Rectangle ownerBounds)
        {
            if (_IsPaintDelayed(ownerBounds.Size)) return;                   // Odložené kreslení? => posečkáme...

            var imageBounds = this._CalculateImageBounds(ownerBounds);       // Reálný prostor obrázku (odsud si pamatujeme podklady pro interaktivitu)
            if (imageBounds.HasValue)
            {
                if (this.HasBmpImage)
                    graphics.DrawImage(this.BmpImage, imageBounds.Value);
                else if (this.HasSvgImage)
                {   // Kterak vykreslíme SvgImage do bitmapové grafiky?   No, převedeme SvgImage na bitmapu, a pak pokračujeme jako s bitmapou :-) :
                    using (var svgBitmap = DxComponent.RenderSvgImage(this.SvgImage, Size.Round(imageBounds.Value.Size)))
                    {
                        graphics.DrawImage(svgBitmap, imageBounds.Value);
                    }
                }
            }
        }
        /// <summary>
        /// Metoda vrátí souřadnice pro Image v daném prostoru, s ohledem na měřítko a zarovnání.
        /// Tuto metodu je nutno volat při každém kreslení controlu v jeho metodě Paint.
        /// <para/>
        /// Součástí této metody je i:<br/>
        /// - Zohlednění měřítka <see cref="Zoom"/> a relativné pozice <see cref="RelativePosition"/>;<br/>
        /// - Určení souřadnic obrázku v rámci dodaného prostoru;<br/>
        /// - Kompletní výpočet fyzických souřadnic aktivních prostorů.
        /// </summary>
        /// <param name="ownerBounds"></param>
        /// <returns></returns>
        private Rectangle? _CalculateImageBounds(Rectangle ownerBounds)
        {
            if (!this.HasImage || !this.__ImageSize.HasValue) return null;
            _CheckValidityOwnerBounds(ownerBounds);
            var imageBounds = __CurrentImageBounds;
            return (imageBounds.HasValue ? (Rectangle?)Rectangle.Round(imageBounds.Value) : null);
        }
        /// <summary>
        /// Zajistí vykreslení Controlu <see cref="OwnerControl"/> z aktuálního threadu (i Working)
        /// </summary>
        private void _RunOwnerRepaint()
        {
            var ownerControl = this.OwnerControl;
            if (ownerControl is null || !ownerControl.IsHandleCreated || ownerControl.IsDisposed || ownerControl.Disposing) return;

            if (ownerControl.InvokeRequired)
                ownerControl.BeginInvoke(new Action(_RunOwnerRepaintGui));
            else
                _RunOwnerRepaintGui();
        }
        /// <summary>
        /// Zajistí vykreslení Controlu <see cref="OwnerControl"/> v aktuálním (=GUI) threadu
        /// </summary>
        private void _RunOwnerRepaintGui()
        {
            var ownerControl = this.OwnerControl;
            if (ownerControl is null || !ownerControl.IsHandleCreated || ownerControl.IsDisposed || ownerControl.Disposing) return;

            __OwnerSizeLastPainted = __OwnerSizeLastRequested;
            ownerControl.Invalidate();
        }
        #endregion
        #region Delay v zobrazení
        /// <summary>
        /// Nastaví a aktivuje <see cref="InitialDelay"/>
        /// </summary>
        /// <param name="initialDelay"></param>
        private void _SetInitialDelay(TimeSpan? initialDelay)
        {
            __InitialDelay = ((initialDelay.HasValue && initialDelay.Value.TotalMilliseconds >= 50d) ? initialDelay : (TimeSpan?)null);
            _ActivateInitialDelay();
        }
        /// <summary>
        /// Nastaví a aktivuje <see cref="ResizeDelay"/>
        /// </summary>
        /// <param name="resizeDelay"></param>
        private void _SetResizeDelay(TimeSpan? resizeDelay)
        {
            __ResizeDelay = ((resizeDelay.HasValue && resizeDelay.Value.TotalMilliseconds >= 50d) ? resizeDelay : (TimeSpan?)null);
            // Toto delay se neaktivuje nyní, ale až po prvním následujícím Resize, který vyvolá nějakou zdejší metodu 
        }
        /// <summary>
        /// Aktivuje časovač pro InitialDelay
        /// </summary>
        private void _ActivateInitialDelay()
        {
            __IsActiveInitialDelay = (__InitialDelay.HasValue);
        }
        /// <summary>
        /// Důležitá metoda, řídící tyto algoritmy: vykreslení, start časovače Delay (a volbu odpovídajícího intervalu) i detekci Resize controlu a start jeho časovače...
        /// Metoda je volána při požadavku na vykreslení. Pokud vrátí true (=paint je delayed), pak fyzický Paint se neprovede!!!
        /// </summary>
        /// <returns></returns>
        private bool _IsPaintDelayed(Size ownerSize)
        {
            var initialDelay = __InitialDelay;
            if (__IsActiveInitialDelay && initialDelay.HasValue && initialDelay.Value.Ticks > 0L)
            {   // Je aktivní InitialDelay = to je od okamžiku setování OwnerControl nebo InitialDelay, pokud je InitialDelay kladné:
                // Následující řádek nastartuje časovač, anebo pokud už běží (když __DelayTimerId má hodnotu) tak znovunastartuje timer:
                __DelayTimerId = WatchTimer.CallMeAfter(_InitialDelayElapsed, (int)initialDelay.Value.TotalMilliseconds, false, __DelayTimerId);
                // A po tuto dobu nebudeme kreslit nic = opravdu bude prázdná plocha!
                return true;            // Časovač InitialDelay právě běží..  Nebudeme nic vykreslovat!
            }
            // Není aktivní delay:
            return false;
        }
        /// <summary>
        /// Doběhl časovač času InitialDelay. 
        /// Běžel od prvního pokusu o vykreslení, a každý další pokus o vykreslení jej znovunastavil na výchozí čas __InitialDelay.
        /// Nyní je tedy aplikace v relativně klidném stavu a měli bychom vykreslit obrázek.
        /// Pozor, jsme ve Working threadu.
        /// </summary>
        private void _InitialDelayElapsed()
        {
            __IsActiveInitialDelay = false;
            __DelayTimerId = null;
            _RunOwnerRepaint();
        }
        private void _DeactivateDelays()
        {
            var id = __DelayTimerId;
            __DelayTimerId = null;
            WatchTimer.Remove(id);
        }
        private bool __IsActiveInitialDelay;
        private bool __IsRunningInitialDelay;
        private bool __IsRunningResizeDelay;
        private Size? __OwnerSizeLastRequested;
        private Size? __OwnerSizeLastPainted;
        private Guid? __DelayTimerId;
        #endregion
        #region Fyzické souřadnice obrázku, odvozené souřadnice klikací mapy
        /// <summary>
        /// Uloží si velikost obrázku BMP
        /// </summary>
        /// <param name="bmpImage"></param>
        private void _LoadBmpImageSize(Image bmpImage)
        {
            _Invalidate();
            __ImageSize = null;
            if (bmpImage is null) return;
            var bmpSize = bmpImage.Size;
            __ImageSize = new SizeF((float)bmpSize.Width, (float)bmpSize.Height);
            __HasImage = true;
        }
        /// <summary>
        /// Uloží si velikost obrázku SVG
        /// </summary>
        /// <param name="svgImage"></param>
        private void _LoadSvgImageSize(DevExpress.Utils.Svg.SvgImage svgImage)
        {
            _Invalidate();
            __ImageSize = null;
            if (svgImage is null) return;
            __ImageSize = new SizeF((float)svgImage.Width, (float)svgImage.Height);
            __HasImage = true;
        }
        /// <summary>
        /// Invaliduje platnost souřadnic.
        /// Volá se po každé změně zdejších public dat:
        /// změna obrázku;
        /// <see cref="Zoom"/> a <see cref="RelativePosition"/>;
        /// <see cref="AddActiveArea(RectangleF, object, DxCursorType, string, string)"/>,
        /// <see cref="ClearActiveArea()"/>.
        /// </summary>
        private void _Invalidate()
        {
            __LastOwnerBounds = null;
        }
        /// <summary>
        /// Metoda zajistí platnost všech souřadnic s ohledem na zadané souřadnice vlastníka, aktuální <see cref="Zoom"/> a <see cref="RelativePosition"/>, 
        /// a zajistí platnost souřadnic aktivních prostor <see cref="__AreaItems"/>.
        /// Pokud jsou hodnoty shodné jako posledně, nemusí nic počítat.
        /// </summary>
        /// <param name="ownerBounds"></param>
        private void _CheckValidityOwnerBounds(Rectangle ownerBounds)
        {
            var imageSizeF = this.__ImageSize;
            if (!this.HasImage || !imageSizeF.HasValue) return;

            if (__LastOwnerBounds.HasValue && __LastOwnerBounds.Value == ownerBounds) return;   // Data jsou validní, netřeba je přepočítávat pokaždé.

            // Malý prostor => nemůžeme pracovat:
            if (ownerBounds.Width < 48 || ownerBounds.Height < 48)
            {
                __CurrentImageBounds = null;
                return;
            }

            // Maximální poměr zvětšení (jen pro bitmapu), dolní hodnota = 1f:
            float? maxRatio = null;
            if (this.HasBmpImage && this.__BmpZoomMaxRatio.HasValue)
                maxRatio = (this.__BmpZoomMaxRatio.Value <= 1f ? 1f : this.__BmpZoomMaxRatio.Value);

            Rectangle ownerBoundsF = ownerBounds;                    // Int32 => float
            var zoom = this.__Zoom;
            SizeF imageSize = imageSizeF.Value.ZoomTo(ownerBoundsF.Size, zoom, maxRatio);

            // Malý prostor => nemůžeme pracovat:
            if (imageSize.Width < 32 || imageSize.Height < 32)
            {
                __CurrentImageBounds = null;
                return;
            }

            // Umístím cílovou velikost obrázku na relativní pozici:
            float dx = ownerBoundsF.Width - imageSize.Width;          // dx a dy je "volný prostor" kolem obrázku v dané ose, v rámci přiděleného prostoru (Size) v ownerBounds
            float dy = ownerBoundsF.Height - imageSize.Height;
            var position = this.RelativePosition;
            float px = dx * position.X;                              // px, py: Pixelová pozice X a Y v rámci přiděleného prostoru (Size) v ownerBounds
            float py = dy * position.Y;

            // Finální souřadnice obrázku jako celku:
            RectangleF imageBounds = new RectangleF(ownerBoundsF.X + px, ownerBoundsF.Y + py, imageSize.Width, imageSize.Height);

            // Určíme souřadnice jednotlivých aktivních prostorů v rámci souřadnic celého obrázku (=klikací mapa):
            foreach (var area in __AreaItems)
                area.CalculateCurrentBounds(imageBounds);

            __CurrentImageBounds = Rectangle.Round(imageBounds);
        }
        /// <summary>
        /// Souřadnice vnějšího prostoru, pro které byly posledně vypočteny souřadnice naše.
        /// Pokud je null anebo pokud nové souřadnice jsou jiné, je nutno vše přepočítat.
        /// </summary>
        private Rectangle? __LastOwnerBounds;
        /// <summary>
        /// Velikost obrázku získaná přímo z něj, slouží jako základ pro další výpočty jeho reálné souřadnice
        /// </summary>
        private SizeF? __ImageSize;
        /// <summary>
        /// Reálná fyzická souřadnice obrázku v koordinátech controlu, v těchto souřadnicích je vykreslen a je aktivní
        /// </summary>
        private Rectangle? __CurrentImageBounds;
        /// <summary>
        /// Jednotlivé prvky
        /// </summary>
        private List<Area> __AreaItems;
        /// <summary>
        /// Popis jednoho klikacího prostoru
        /// </summary>
        private class Area
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="relativeBounds">Relativní souřadnice oblasti v hodnotách 0.00 až 1.00 v obou osách, vztahují se k ploše obrázku</param>
            /// <param name="userData">Uživatelova data = identifikátor oblasti, nebo cílová akce na oblasti. Předává se do eventu Click.</param>
            /// <param name="cursorType">Typ kurzoru na oblasti</param>
            /// <param name="toolTipTitle">Titulek tooltipu</param>
            /// <param name="toolTipText">Text tooltipu</param>
            public Area(RectangleF relativeBounds, object userData, DxCursorType cursorType, string toolTipTitle, string toolTipText)
            {
                float l = _AlignRatio(relativeBounds.Left);
                float t = _AlignRatio(relativeBounds.Top);
                float r = _AlignRatio(relativeBounds.Right);
                float b = _AlignRatio(relativeBounds.Bottom);
                this.RelativeBounds = RectangleF.FromLTRB(l, t, r, b);
                this.UserData = userData;
                this.CursorType = cursorType;
                this.ToolTipTitle = toolTipTitle;
                this.ToolTipText = toolTipText;

                this.CurrentBounds = null;
            }
            /// <summary>
            /// Relativní souřadnice v hodnotách 0.00 - 1.00
            /// </summary>
            public RectangleF RelativeBounds { get; private set; }
            /// <summary>
            /// Uživatelova data = identifikátor oblasti, nebo cílová akce na oblasti. Předává se do eventu Click.
            /// </summary>
            public object UserData { get; private set; }
            /// <summary>
            /// Typ kurzoru na oblasti
            /// </summary>
            public DxCursorType CursorType { get; private set; }
            /// <summary>
            /// Titulek tooltipu
            /// </summary>
            public string ToolTipTitle { get; private set; }
            /// <summary>
            /// Text tooltipu
            /// </summary>
            public string ToolTipText { get; private set; }
            /// <summary>
            /// Aktuální souřadnice v pixelech na aktuálním controlu
            /// </summary>
            internal Rectangle? CurrentBounds { get; private set; }
            /// <summary>
            /// Vypočítá <see cref="CurrentBounds"/> podle <see cref="RelativeBounds"/> a aktuálního prostoru obrázku <paramref name="imageBounds"/>.
            /// </summary>
            /// <param name="imageBounds"></param>
            internal void CalculateCurrentBounds(RectangleF imageBounds)
            {
                float bx = imageBounds.X;
                float by = imageBounds.Y;
                float iw = imageBounds.Width;
                float ih = imageBounds.Height;

                var relBounds = this.RelativeBounds;

                float ax = bx + relBounds.X * iw;
                float ay = by + (relBounds.Y * ih);
                float aw = relBounds.Width * iw;
                float ah = relBounds.Height * ih;

                this.CurrentBounds = Rectangle.Round(new RectangleF(ax, ay, aw, ah));
            }
        }
        #endregion
        #region Eventhandlery vizuálního controlu = interaktivita, a její řešení
        /// <summary>
        /// Obsahuje true, pokud máme živý Control
        /// </summary>
        protected bool HasLiveOwnerControl { get { var c = this.OwnerControl; return (c != null && c.IsHandleCreated && !c.Disposing && !c.IsDisposed); } }
        /// <summary>
        /// Handler události ClientSizeChanged: invaliduje souřadnice. 
        /// Poté musí přijít nějaké kreslení s aktuálně platnými souřadnicemi, aby se určily zdejší souřadnice a abychom mohli řešit interaktivitu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_ClientSizeChanged(object sender, EventArgs e)
        {
            __CurrentImageBounds = null;
        }
        /// <summary>
        /// Handler události MouseEnter: nuluje stavy myši a vyvolá 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_MouseEnter(object sender, EventArgs e)
        {
            // Tenhle event proběhne, když je aktivovaná aplikace poté, kdy byla aktivní jiná.
            // A myš může zrovna stát na našem Image a měla by mít aktivní správný kurzor.
            // Anebo myš před tím stála na našem Image, pak se aktivovala jiná aplikace, pohnula se myš a nyní je jinde.
            // Prostě potřebujeme nastavit proměnné a kurzor myši podle aktuální pozice...
            __IsMouseCursorOnImage = false;
            __OnMouseAreaItem = null;

            if (!this.HasLiveOwnerControl) return;
            if (!this.HasValidBounds) return;

            // Zajistíme inicializaci stavu myši na obrázku, jako by se zde pohnula:
            Point mouseLocation = this.OwnerControl.PointToClient(Control.MousePosition);
            _OwnerControl_MouseMove(mouseLocation);
        }
        /// <summary>
        /// Handler události MouseMove: řeší změnu kurzoru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_MouseMove(object sender, MouseEventArgs e)
        {
            _OwnerControl_MouseMove(e.Location);
        }
        /// <summary>
        /// Handler události MouseMove: řeší změnu kurzoru
        /// </summary>
        /// <param name="mouseLocation"></param>
        private void _OwnerControl_MouseMove(Point mouseLocation)
        {
            if (!this.HasActiveArea || !this.HasLiveOwnerControl) return;
            if (!this.HasValidBounds) return;

            bool oldIsOnImage = __IsMouseCursorOnImage;
            bool newIsOnImage = __CurrentImageBounds.HasValue && __CurrentImageBounds.Value.Contains(mouseLocation);
            if (newIsOnImage)
            {   // Nyní jsem na obrázku:
                // Najdeme aktivní prostor:
                Area oldArea = __OnMouseAreaItem;
                bool oldIsOnArea = (oldArea != null);
                bool newIsOnArea = _TryGetArea(mouseLocation, out Area newArea);

                // Vyřešíme změny aktivního prostoru:
                if (!oldIsOnArea && newIsOnArea)
                {   // Vstup odnikud na Area:
                    activateArea(newArea);
                }
                else if (oldIsOnArea && newIsOnArea && !Object.ReferenceEquals(oldArea, newArea))
                {   // Přestup ze starého Area do jiného nového Area:
                    activateArea(newArea);
                }
                else if (oldIsOnArea && !newIsOnArea)
                {   // Odchod ze starého Area:
                    deactivateArea();
                }

                __IsMouseCursorOnImage = true;
            }
            else 
            {   // Myš je mimo obrázek:
                if (oldIsOnImage)
                    _MouseLeaveImage();
            }

            // Aktivuje daný prostor
            void activateArea(Area area)
            {
                __OnMouseAreaItem = area;
                _ActivateCursor(area.CursorType);
            }

            // Deaktivuje prostor
            void deactivateArea()
            {
                __OnMouseAreaItem = null;
                _ActivateCursor(null);
            }
        }
        /// <summary>
        /// Handler události MouseDown: řeší kliknutí na obrázek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_MouseDown(object sender, MouseEventArgs e)
        {
            __MouseDownPoint = null;

            if (!this.HasActiveArea || e.Button != MouseButtons.Left) return;
            if (!this.HasValidBounds) return;

            // Nebudeme spouštět Target akci v okamžiku MouseDown, ale až v MouseUp. Je to tak zvykem ve Windows.
            // A taky proto, že sideEffect po spuštění akce v MouseDown je ohavný:
            //  Pokud by MouseDown spustila akci => otevře se jiná aplikace (a ta se dostane do popředí),
            //  a proto zdejší Control už nedostane událost MouseUp, a tak si stále myslí, že je myš dole.
            //  Když se pak zdejší aplikce zase stane aktivní, tak si pořád myslí, že Mouse je Down (a ona přitom už dávno není),
            //  a tak aplikace při pohybu myši provádí MouseDrag - a přitom myš není zmáčknutá!
            Point mouseLocation = e.Location;
            bool isOnBounds = __CurrentImageBounds.HasValue && __CurrentImageBounds.Value.Contains(mouseLocation);
            Area newArea = null;
            bool newIsOn = isOnBounds && _TryGetArea(mouseLocation, out newArea);
            if (newIsOn)
                __MouseDownPoint = __OwnerControl.PointToScreen(mouseLocation);
        }
        /// <summary>
        /// Handler události MouseUp: řeší kliknutí na obrázek
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (!this.HasActiveArea || e.Button != MouseButtons.Left) return;
            if (!this.HasValidBounds) return;
            if (!this.__MouseDownPoint.HasValue) return;

            Point mouseLocation = e.Location;

            // Pokud nynější souřadnice myši je poměrně více vzdálená od souřadnice při MouseDown, pak nejde o Click ale o Drag:
            Point screenLocation = __OwnerControl.PointToScreen(mouseLocation);
            Rectangle silentBounds = this.__MouseDownPoint.Value.CreateRectangleFromCenter(8);
            if (!silentBounds.Contains(screenLocation)) return;

            // Myš se nijak nevzdálila od okamžiku Down do Up, jde tedy o kliknutí:
            bool isOnBounds = __CurrentImageBounds.HasValue && __CurrentImageBounds.Value.Contains(mouseLocation);
            Area newArea = null;
            bool newIsOn = isOnBounds && _TryGetArea(mouseLocation, out newArea);
            if (newIsOn)
                _RunClick(newArea.UserData);
        }
        /// <summary>
        /// Souřadnice typu Screen, kde byl proveden MouseDown s Left Buttonem. Pokud i MouseUp je poblíž, jde o Click a bude se vyhodnocovat.
        /// </summary>
        private Point? __MouseDownPoint;
        /// <summary>
        /// Handler události MouseLeave: řeší změnu kurzoru na Default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OwnerControl_MouseLeave(object sender, EventArgs e)
        {
            _MouseLeaveImage();
        }
        /// <summary>
        /// Odchod myší z obrázku: default kurzor
        /// </summary>
        private void _MouseLeaveImage()
        {
            if (this.HasLiveOwnerControl)
            {
                if (__OnMouseCursorType.HasValue)
                    _ActivateCursor(null);
            }
            __OnMouseAreaItem = null;
            __OnMouseCursorType = null;
            __IsMouseCursorOnImage = false;
        }
        /// <summary>
        /// Pro aktuální Control aktivuje daný kurzor
        /// </summary>
        /// <param name="cursorType"></param>
        private void _ActivateCursor(DxCursorType? cursorType)
        {
            DxComponent.SetCursorToControl(__OwnerControl, cursorType);
            __OnMouseCursorType = cursorType;
        }
        /// <summary>
        /// Zkusí najít poslední prvek v <see cref="__AreaItems"/>, který se nachází na dané souřadnici
        /// </summary>
        /// <param name="point"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private bool _TryGetArea(Point point, out Area area)
        {
            return __AreaItems.TryGetLast(a => a.CurrentBounds.HasValue && a.CurrentBounds.Value.Contains(point), out area);
        }
        /// <summary>
        /// Vyvolá událost <see cref="Click"/>
        /// </summary>
        /// <param name="userData"></param>
        private void _RunClick(object userData)
        {
            this.Click?.Invoke(this, new AreaClickArgs(userData));
        }
        /// <summary>
        /// Prvek nacházející se pod myší; 
        /// </summary>
        private Area __OnMouseAreaItem;
        /// <summary>
        /// Kurzor aktivovaný právě nyní pro <see cref="OwnerControl"/>, daný aktivním prostorem <see cref="__OnMouseAreaItem"/>.
        /// null = myš je mimo aktivní prostor.
        /// </summary>
        private DxCursorType? __OnMouseCursorType;
        /// <summary>
        /// Myš se nachází nad obrázkem jako celek
        /// </summary>
        private bool __IsMouseCursorOnImage;
        #endregion
    }
    #endregion
    #region DxTabHeaderImagePainter : třída, která vykreslí další ikonu do záhlaví TabHeaderu
    /// <summary>
    /// <see cref="DxTabHeaderImagePainter"/> : třída, která vykreslí další ikonu do záhlaví TabHeaderu, a dovolí i vykreslovat základní ikonu.
    /// </summary>
    internal class DxTabHeaderImagePainter : IListenerZoomChange, IDisposable
    {
        #region Konstruktor a public vlastnosti
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxTabHeaderImagePainter()
        {
            ImagePosition = ImagePositionType.None;
            ImageSizeType = ResourceImageSizeType.Medium;
            DxComponent.RegisterListener(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            DxComponent.UnregisterListener(this);
            TabbedView = null;
            ImageNameAddGenerator = null;
        }
        /// <summary>
        /// Reference na TabbedView, v jehož TabHeaderech se bude vykreslovat přidaná ikona
        /// </summary>
        public DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView TabbedView
        {
            get { return __TabbedView; }
            set
            {
                if (__TabbedView != null)
                {   // detach:
                    __TabbedView.CustomDrawTabHeader -= _CustomDrawTabHeader;
                    __TabbedView.DocumentAdded -= _DocumentAdded;
                    __TabbedView.Manager.MdiParent.DpiChanged -= _DpiChanged;
                }

                __TabbedView = value;

                if (__TabbedView != null)
                {   // attach:
                    __TabbedView.CustomDrawTabHeader += _CustomDrawTabHeader;
                    __TabbedView.DocumentAdded += _DocumentAdded;
                    __TabbedView.Manager.MdiParent.DpiChanged += _DpiChanged;
                }
            }
        }
        /// <summary>
        /// Instance TabbedView
        /// </summary>
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView __TabbedView;
        /// <summary>
        /// Metoda, která dostane jako parametr Control reprezentující obsah dokumentu v TabHeader (typicky Formulář), a najde v něm jméno základní ikony (ImageNameBasic).
        /// Reaguje např. na typ formuláře, nebo na jeho obsah...
        /// Pokud je zde null, anebo funkce vrátí prázdný string, ikona se nebude kreslit.
        /// <para/>
        /// Může zůstat null, pokud se zobrazují pouze formuláře které implementují interface <see cref="IDxControlWithIcons"/>.
        /// Z takových formulářů si jméno ikony přečteme sami z property <see cref="IDxControlWithIcons.IconNameBasic"/>, a zdejší metodu nevoláme.
        /// </summary>
        public Func<Control, string> ImageNameBasicGenerator { get; set; }
        /// <summary>
        /// Metoda, která dostane jako parametr Control reprezentující obsah dokumentu v TabHeader (typicky Formulář), a najde v něm jméno přidané ikony (ImageNameAdd).
        /// Reaguje např. na typ formuláře, nebo na jeho obsah...
        /// Pokud je zde null, anebo funkce vrátí prázdný string, ikona se nebude kreslit.
        /// <para/>
        /// Může zůstat null, pokud se zobrazují pouze formuláře které implementují interface <see cref="IDxControlWithIcons"/>.
        /// Z takových formulářů si jméno ikony přečteme sami z property <see cref="IDxControlWithIcons.IconNameAdd"/>, a zdejší metodu nevoláme.
        /// </summary>
        public Func<Control, string> ImageNameAddGenerator { get; set; }
        /// <summary>
        /// Velikost ikony, varianta
        /// </summary>
        public ResourceImageSizeType ImageSizeType { get; set; }
        /// <summary>
        /// Velikost ikony, pixely
        /// </summary>
        public Size ImageSize { get { return DxComponent.GetImageSize(this.ImageSizeType, true, CurrentDpi); } }
        /// <summary>
        /// Pozice ikony
        /// </summary>
        public ImagePositionType ImagePosition { get; set; }
        /// <summary>
        /// Aktuální DPI
        /// </summary>
        private int? CurrentDpi { get { return (__TabbedView?.Manager?.MdiParent?.DeviceDpi); } }
        /// <summary>
        /// Po přidání dokumentu (=nové okno) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DocumentAdded(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            ImageInfo imageInfo = ImageInfo.Create(this, e.Document);
            if (imageInfo != null)
                e.Document.ImageOptions.SvgImageSize = imageInfo.TotalImageSize;
            e.Document.Tag = imageInfo;
        }
        /// <summary>
        /// Po změně DPI na formuláři, který nás hostuje = má vliv na velikost ikon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DpiChanged(object sender, DpiChangedEventArgs e)
        {
            _RefreshIconSizes();
        }
        /// <summary>
        /// Je voláno po změně Zoomu
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        void IListenerZoomChange.ZoomChanged()
        {
            _RefreshIconSizes();
        }
        /// <summary>
        /// Aktualizuje velikost ikon
        /// </summary>
        private void _RefreshIconSizes()
        {
            var documents = __TabbedView?.Documents;
            if (documents is null) return;

            foreach (var document in documents)
            {
                if (document.Tag is ImageInfo imageInfo)
                    imageInfo.RefreshIconSizes(this, document);
            }
        }
        #endregion
        #region Enumy
        /// <summary>
        /// Umístění přidané ikony
        /// </summary>
        public enum ImagePositionType
        {
            /// <summary>
            /// Nebude kreslena
            /// </summary>
            None,
            /// <summary>
            /// Namísto buttonu Close, pouze v neaktivních TabHeader
            /// </summary>
            InsteadCloseButton,
            /// <summary>
            /// Namísto buttonu Pin, pouze v neaktivních TabHeader
            /// </summary>
            InsteadPinButton,
            /// <summary>
            /// Uprostřed pole Buttonů vpravo, pouze v neaktivních TabHeader
            /// </summary>
            CenterControlArea,
            /// <summary>
            /// Namísto standardní ikony vlevo
            /// </summary>
            InsteadStandardIcon,
            /// <summary>
            /// Vedle standardní ikony, napravo od ikony, před textem
            /// </summary>
            AfterStandardIcon
        }
        #endregion
        #region Privátní oblast, kreslení jednotlivých variant
        /// <summary>
        /// Vykreslení jednotlivého TabHeaderu = doplnění ikony do neaktivního headeru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CustomDrawTabHeader(object sender, DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e)
        {
            ImageInfo imageInfo = _GetImageInfo(e);
            if (imageInfo is null || String.IsNullOrEmpty(imageInfo.ImageNameAdd)) return;

            switch (ImagePosition)
            {
                case ImagePositionType.None: return;                 // Vykreslí DevExpress = defaultně
                case ImagePositionType.InsteadCloseButton: _DrawTabHeaderInControlBox(e, imageInfo, 0); break;
                case ImagePositionType.InsteadPinButton: _DrawTabHeaderInControlBox(e, imageInfo, 1); break;
                case ImagePositionType.CenterControlArea: _DrawTabHeaderInControlBox(e, imageInfo, -1); break;
                case ImagePositionType.InsteadStandardIcon: _DrawTabHeaderInsteadStandardIcon(e, imageInfo); break;
                case ImagePositionType.AfterStandardIcon: _DrawTabHeaderAfterStandardIcon(e, imageInfo); break;
            }
        }
        /// <summary>
        /// Vrátí informace o ikonách pro daný TabHeader
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ImageInfo _GetImageInfo(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e)
        {
            var documentInfo = e.TabHeaderInfo.Page as DevExpress.XtraBars.Docking2010.Views.Tabbed.IDocumentInfo;
            var imageInfo = documentInfo?.Document?.Tag as ImageInfo;
            return imageInfo;
        }
        /// <summary>
        /// Vykreslí celý TabHeader s přidaným Image na místo ControlBoxu (Buttony vpravo)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageInfo"></param>
        /// <param name="index"></param>
        private void _DrawTabHeaderInControlBox(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e, ImageInfo imageInfo, int index)
        {
            e.DefaultDrawBackground();
            _DrawImageBasic(e, imageInfo);
            e.DefaultDrawText();

            bool isTabActive = (e.TabHeaderInfo.IsActiveState || e.TabHeaderInfo.IsHotState);
            if (!isTabActive)
            {   // Image kreslíme jen do neaktivního TabHeaderu:
                var imageBounds = _GetImageBoundsInControlBox(e, index, ImageSize);
                var image = DxComponent.GetBitmapImage(imageInfo.ImageNameAdd, this.ImageSizeType);
                e.Cache.DrawImage(image, imageBounds);
            }
            else
            {   // Do aktivního TabHeaderu kreslíme defaultní Buttony:
                e.DefaultDrawButtons();
            }
            e.Handled = true;
        }
        /// <summary>
        /// Vrátí souřadnice pro doplňkovou ikonu. Souřadnice jsou v místě zadaného tlačítka v rámci prostoru Buttons = vpravo (Close nebo Pin), anebo uprostřed tohoto prostoru.
        /// Index <paramref name="index"/> odkazuje na pozici tlačítka. Počítá se zprava (0=Close, 1=Pin). Hodnota -1 = uprostřed.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="index"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        private static Rectangle _GetImageBoundsInControlBox(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e, int index, Size iconSize)
        {
            var controlBounds = e.TabHeaderInfo.ControlBox;
            var imageBounds = e.TabHeaderInfo.Image;
            int imageCY = imageBounds.Y + (imageBounds.Height / 2);
            int imageCX;

            var buttons = e.TabHeaderInfo.ButtonsPanel?.ViewInfo?.Buttons;
            if (buttons != null && buttons.Count > 0)
            {
                if (index >= 0)
                {   // Konkrétní button:
                    var buttonIndex = (index < 0 ? 0 : (index >= buttons.Count ? buttons.Count - 1 : index));
                    var buttonBounds = buttons[buttonIndex].Bounds;
                    imageCX = buttonBounds.X + (buttonBounds.Width / 2);
                }
                else
                {   // Střed všech buttonů:
                    imageCX = controlBounds.X + (controlBounds.Width / 2);
                }
            }
            else
            {
                // Vpravo:
                var contentBounds = e.TabHeaderInfo.Content;
                imageCX = contentBounds.Right - (iconSize.Width / 2);
            }

            // Ze souřadnice středu ikony a její velikosti vytvořím Rectangle pro ikonu:
            var result = new Point(imageCX, imageCY).CreateRectangleFromCenter(iconSize);

            // Zarovnat do prostoru ControlBox:
            if (result.X < controlBounds.X) result.X = controlBounds.X;
            if (result.Right > controlBounds.Right) result.X = controlBounds.Right - result.Width;

            return result;
        }
        /// <summary>
        /// Vykreslí celý TabHeader s přidaným Image na místo ikony okna (vlevo)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageInfo"></param>
        private void _DrawTabHeaderInsteadStandardIcon(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e, ImageInfo imageInfo)
        {
            e.DefaultDrawBackground();

            _DrawImageAsBasic(e, imageInfo.ImageNameAdd);

            e.DefaultDrawText();
            e.DefaultDrawButtons();
            e.Handled = true;
        }
        /// <summary>
        /// Vykreslí celý TabHeader s přidaným Image na místo ikony okna (vlevo)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageInfo"></param>
        private void _DrawTabHeaderAfterStandardIcon(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs e, ImageInfo imageInfo)
        {
            e.DefaultDrawBackground();

            var basicBounds = _DrawImageBasic(e, imageInfo);
            var addBounds = new Rectangle(basicBounds.X + imageInfo.ImageAddOffsetX, basicBounds.Y, basicBounds.Width, basicBounds.Height);
            _DrawImageToBounds(e, imageInfo.ImageNameAdd, addBounds);

            e.DefaultDrawText();
            // DrawHeaderPageText(e);

            e.DefaultDrawButtons();
            e.Handled = true;
        }
        /// <summary>
        /// Vykreslí základní ikonu na její odpovídající pozici
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageInfo"></param>
        private Rectangle _DrawImageBasic(TabHeaderCustomDrawEventArgs e, ImageInfo imageInfo)
        {
            return _DrawImageAsBasic(e, imageInfo.ImageNameBasic);
        }
        /// <summary>
        /// Vykreslí danou ikonu na pozici základní ikony
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageName"></param>
        private Rectangle _DrawImageAsBasic(TabHeaderCustomDrawEventArgs e, string imageName)
        {
            var imageSize = this.ImageSize;
            var totalImageBounds = e.TabHeaderInfo.Image;
            int x = totalImageBounds.X;
            int y = totalImageBounds.Y + (totalImageBounds.Height / 2) - (imageSize.Height / 2);
            var imageBounds = new Rectangle(x, y, imageSize.Width, imageSize.Height);
            _DrawImageToBounds(e, imageName, imageBounds);
            return imageBounds;
        }
        /// <summary>
        /// Vykreslí danou ikonu na pozici základní ikony
        /// </summary>
        /// <param name="e"></param>
        /// <param name="imageName"></param>
        /// <param name="imageBounds"></param>
        private void _DrawImageToBounds(TabHeaderCustomDrawEventArgs e, string imageName, Rectangle imageBounds)
        {
            if (!String.IsNullOrEmpty(imageName))
            {
                var image = DxComponent.GetBitmapImage(imageName, this.ImageSizeType);
                e.Cache.DrawImage(image, imageBounds);
            }
        }
        private void DrawHeaderPageText(DevExpress.XtraTab.TabHeaderCustomDrawEventArgs args)
        {
            DevExpress.XtraTab.Drawing.TabDrawArgs e = args.ControlInfo;
            DevExpress.XtraTab.ViewInfo.BaseTabPageViewInfo pInfo = args.TabHeaderInfo;

            int angle = 0;
            if (e.ViewInfo.HeaderInfo.RealPageOrientation ==  DevExpress.XtraTab.TabOrientation.Vertical)
            {
                angle = 90;
                if (e.ViewInfo.HeaderInfo.IsLeftLocation || e.ViewInfo.HeaderInfo.IsTopLocation)
                {
                    angle = 270;
                }
            }
            AppearanceObject paintAppearance = pInfo.PaintAppearance;
            HKeyPrefix? hKeyPrefix = ((paintAppearance.TextOptions.HotkeyPrefix == HKeyPrefix.Default && pInfo.UseHotkeyPrefixDrawModeOverride) ? (HKeyPrefix?)pInfo.HotkeyPrefixDrawModeOverride : null);
            HKeyPrefix hotkeyPrefix = paintAppearance.TextOptions.HotkeyPrefix;
            if (hKeyPrefix.HasValue)
            {
                paintAppearance.TextOptions.SetHotKeyPrefix(hKeyPrefix.Value);
            }
            if (e.ViewInfo.PropertiesEx.AllowHtmlDraw)
            {
                FrozenAppearance frozenAppearance = new FrozenAppearance(paintAppearance);
                frozenAppearance.ForeColor = pInfo.PaintAppearance.ForeColor;
                // args.Cache.DrawString(pInfo.Page.Text, )
                StringPainter.Default.DrawString(e.Cache, frozenAppearance, pInfo.Page.Text, pInfo.Text, frozenAppearance.TextOptions, args.ControlInfo);
            }
            else
            {
                args.Cache.DrawVString(pInfo.Page.Text, paintAppearance.GetFont(), e.Cache.GetSolidBrush(pInfo.PaintAppearance.ForeColor), pInfo.Text, DxComponent.GetStringFormatFor(ContentAlignment.MiddleLeft), angle);
            }
            paintAppearance.TextOptions.SetHotKeyPrefix(hotkeyPrefix);
        }
        #endregion
        #region class ImageInfo a její získání
        /// <summary>
        /// <see cref="ImageInfo"/>: informace o jménech obrázků, kreslených jako ikona v TabHeader
        /// </summary>
        private class ImageInfo
        {
            /// <summary>
            /// Sestaví a vrátí info o ikonách v dokumentu
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="document"></param>
            /// <returns></returns>
            public static ImageInfo Create(DxTabHeaderImagePainter owner, DevExpress.XtraBars.Docking2010.Views.BaseDocument document)
            {
                var imageInfo = new ImageInfo();
                imageInfo.RefreshIconNames(owner, document);
                imageInfo.RefreshIconSizes(owner, document);
                return imageInfo;
            }
            /// <summary>
            /// Aktualizuje názvy ikon z formuláře dodaného v dokumentu
            /// </summary>
            public void RefreshIconNames(DxTabHeaderImagePainter owner, DevExpress.XtraBars.Docking2010.Views.BaseDocument document)
            {
                Control form = document?.Control;
                if (form is IDxControlWithIcons iconsForm)
                {
                    this.ImageNameBasic = iconsForm.IconNameBasic;
                    this.ImageNameAdd = iconsForm.IconNameAdd;
                }
                else
                {
                    this.ImageNameBasic = owner.ImageNameBasicGenerator?.Invoke(form);
                    this.ImageNameAdd = owner.ImageNameAddGenerator?.Invoke(form);
                }
                RefreshIconSizes(owner, document);
            }
            /// <summary>
            /// Aktualizuje velikost ikon
            /// </summary>
            public void RefreshIconSizes(DxTabHeaderImagePainter owner, DevExpress.XtraBars.Docking2010.Views.BaseDocument document)
            {
                var imageSize = owner.ImageSize;
                var totalImageSize = imageSize;

                // V režimu AfterStandardIcon: totalImageSize musí mít šířku pro dvě standardní ikony + 2/8 [nebo 1/8 ?] rozestup mezi nimi:
                //  (pokud přidaná ikona ImageNameAdd je definovaná)
                int imageAddOffsetX = 0;
                string imageNameAdd = this.ImageNameAdd;
                if (owner.ImagePosition == ImagePositionType.AfterStandardIcon && !String.IsNullOrEmpty(imageNameAdd))
                {
                    int w = totalImageSize.Width;
                    imageAddOffsetX = w + totalImageSize.Width / 8;
                    totalImageSize.Width = totalImageSize.Width + imageAddOffsetX;
                }

                this.ImageSize = imageSize;
                this.TotalImageSize = totalImageSize;
                this.ImageAddOffsetX = imageAddOffsetX;

                if (document != null)
                    document.ImageOptions.SvgImageSize = totalImageSize;
            }
            /// <summary>
            /// Privátní konstruktor
            /// </summary>
            private ImageInfo() { }
            /// <summary>
            /// Velikost jedné ikony.
            /// </summary>
            public Size ImageSize { get; private set; }
            /// <summary>
            /// Velikost prostoru pro ikonu vlevo od názvu.
            /// Typicky je zde velikost = <see cref="ImageSize"/> (prostor pro jednu standardní ikonu).
            /// Pokud ale <see cref="DxTabHeaderImagePainter"/> je nastaven na kreslení dvou ikon vedle sebe vlevo od textu, pak je zde velikost obou ikon plus mezery mezi nimi.
            /// Tuto velikost ukládá <see cref="DxTabHeaderImagePainter"/> do SvgImageSize, a odtud ji následně DX přebírá a alokuje potřebné místo pro ikonu vlevo od textu TabHeaderu.
            /// </summary>
            public Size TotalImageSize { get; private set; }
            /// <summary>
            /// Pokud se ikona <see cref="ImageNameAdd"/> kreslí v režimu <see cref="ImagePositionType.AfterStandardIcon"/>, pak její souřadnice X = souřadnice základní Image.X + tento offset <see cref="ImageAddOffsetX"/>.
            /// </summary>
            public int ImageAddOffsetX { get; private set; }
            /// <summary>
            /// Jméno image základního = odpovídá ikoně formuláře
            /// </summary>
            public string ImageNameBasic { get; private set; }
            /// <summary>
            /// Jméno image přidaného = zobrazuje se jako druhý obrázek podle předvolby
            /// </summary>
            public string ImageNameAdd { get; private set; }
        }
        #endregion
    }
    #endregion
    #region DxChartControl
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.ChartControl"/> pro použití v ASOL.Nephrite
    /// </summary>
    internal class DxChartControl : DevExpress.XtraCharts.ChartControl
    {
        #region Support pro práci s grafem
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxChartControl()
        {
            this.HardwareAcceleration = true;
        }
        /// <summary>
        /// XML definice vzhledu grafu (Layout), aktuálně načtený z komponenty
        /// </summary>
        public string ChartXmlLayout
        {
            get { return GetGuiValue<string>(() => this._GetChartXmlLayout()); }
            set { SetGuiValue<string>(v => this._SetChartXmlLayout(v), value); }
        }
        /// <summary>
        /// Reference na data zobrazená v grafu
        /// </summary>
        public object ChartDataSource
        {
            get { return GetGuiValue<object>(() => this._GetChartDataSource()); }
            set { SetGuiValue<object>(v => this._SetChartDataSource(v), value); }
        }
        /// <summary>
        /// Vloží do grafu layout, a současně data, v jednom kroku
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        public void SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Zobrazí Wizard nebo Designer grafu, podle obsahu definice <see cref="ChartXmlLayout"/>.
        /// </summary>
        public void ShowChartWizardOrDesigner()
        {
            _ShowChartEditor(EditorType.Auto);
        }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda nebo Designera DevExpress
        /// </summary>
        public void ShowChartWizard()
        {
            _ShowChartEditor(EditorType.Wizard);
        }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda nebo Designera DevExpress
        /// </summary>
        public void ShowChartDesigner()
        {
            _ShowChartEditor(EditorType.Designer);
        }
        /// <summary>
        /// Druh editoru
        /// </summary>
        private enum EditorType { None, Auto, Wizard, Designer }
        /// <summary>
        /// Zajistí editaci grafu pomocí Wizarda nebo Designera DevExpress
        /// </summary>
        private void _ShowChartEditor(EditorType editorType)
        {
            if (!IsChartWorking)
                throw new InvalidOperationException(DxComponent.Localize(MsgCode.DxChartEditorNotPrepared)); // $"V tuto chvíli nelze editovat graf, dosud není načten nebo není definován.");

            bool hasDefinition = !String.IsNullOrEmpty(this._ValidChartXmlLayout);                 // true, když máme nějakou definici grafu
            if (editorType == EditorType.Auto)
            {   // Automaticky:
                editorType = (hasDefinition ? EditorType.Designer : EditorType.Wizard);
            }
            bool acceptDefinition = false;
            string editorTitle;
            switch (editorType)
            {
                case EditorType.Wizard:
                    // První editace grafu = Wizard:
                    var msgCode = (hasDefinition ? MsgCode.DxChartEditorTitleDesigner : MsgCode.DxChartEditorTitleWizard);
                    editorTitle = DxComponent.Localize(msgCode);                                   // "Vytvořte nový graf..." / "Upravte graf..."
                    acceptDefinition = DxChartWizard.ShowWizard(this, editorTitle, true, false);
                    break;
                case EditorType.Designer:
                    // Další editace grafu = Designer:
                    editorTitle = DxComponent.Localize(MsgCode.DxChartEditorTitleDesigner);        // "Upravte graf..."
                    acceptDefinition = DxChartDesigner.DesignChart(this, editorTitle, true, false);
                    break;
            }

            // Wizard pracuje nad naším controlem, veškeré změny ve Wizardu provedené se ihned promítají do našeho grafu.
            // Pokud uživatel dal OK, chceme změny uložit i do příště,
            // pokud ale dal Cancel, pak změny chceme zahodit a vracíme se k původnímu layoutu:
            if (acceptDefinition)
            {
                string newLayout = _GetLayoutFromControl();
                _ValidChartXmlLayout = newLayout;
                OnChartXmlLayoutEdited();
            }
            else
            {
                _RestoreChartXmlLayout();
            }
        }
        /// <summary>
        /// Je voláno po editaci layoutu, vyvolá event <see cref="ChartXmlLayoutEdited"/>
        /// </summary>
        protected virtual void OnChartXmlLayoutEdited()
        {
            ChartXmlLayoutEdited?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Událost, kdy uživatel změnil data (vyvoláno metodou <see cref="ShowChartWizard"/>), a v designeru uložil změnu dat.
        /// V tuto chvíli je již nový layout k dispozici v <see cref="ChartXmlLayout"/>.
        /// </summary>
        public event EventHandler ChartXmlLayoutEdited;
        #endregion
        #region private práce s layoutem a daty
        /// <summary>
        /// Vloží do grafu layout, ponechává data
        /// </summary>
        /// <param name="xmlLayout">XML definice vzhledu grafu</param>
        private void _SetChartXmlLayout(string xmlLayout)
        {
            _ValidChartXmlLayout = xmlLayout;              // Uloží se požadovaný text definující layout
            var dataSource = _ChartDataSource;             // Načteme z WeakReference
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí XML layout načtený přímo z grafu
        /// </summary>
        /// <returns></returns>
        private string _GetChartXmlLayout()
        {
            return _GetLayoutFromControl();
        }
        /// <summary>
        /// Vloží do grafu dříve platný layout (uložený v metodě <see cref="_SetChartXmlLayout(string)"/>), ponechává data.
        /// </summary>
        private void _RestoreChartXmlLayout()
        {
            var dataSource = _ChartDataSource;             // Tabulka z WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vloží do grafu data, ponechává layout (může dojít k chybě)
        /// </summary>
        /// <param name="dataSource">Tabulka s daty, zdroj dat grafu</param>
        private void _SetChartDataSource(object dataSource)
        {
            _ChartDataSource = dataSource;                 // Uloží se WeakReference
            string xmlLayout = _ValidChartXmlLayout;       // Uložený layout
            _SetChartXmlLayoutAndDataSource(xmlLayout, dataSource);
        }
        /// <summary>
        /// Vrátí data grafu
        /// </summary>
        /// <returns></returns>
        private object _GetChartDataSource()
        {
            return _ChartDataSource;
        }
        /// <summary>
        /// Do grafu korektně vloží data i layout, ve správném pořadí.
        /// Pozor: tato metoda neukládá dodané objekty do lokálních proměnných <see cref="_ValidChartXmlLayout"/> a <see cref="_ChartDataSource"/>, pouze do controlu grafu!
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="xmlLayout"></param>
        private void _SetChartXmlLayoutAndDataSource(string xmlLayout, object dataSource)
        {
            // Tato sekvence je důležitá, jinak dojde ke zbytečným chybám:
            try
            {
                this._SetLayoutToControl(xmlLayout);
            }
            finally
            {   // Datový zdroj do this uložím i po chybě, abych mohl i po vložení chybného layoutu otevřít editor:
                this.DataSource = dataSource;
            }
        }
        /// <summary>
        /// Fyzicky načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayoutFromControl()
        {
            string layout = null;
            if (IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    this.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Neřeší try catch, to má řešit volající včetně ošetření chyby.
        /// POZOR: tato metoda odpojí datový zdroj, proto je třeba po jejím skončení znovu vložit zdroj do _ChartControl.DataSource !
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControl(string layout)
        {
            if (IsChartValid)
            {
                try
                {
                    this.BeginInit();
                    this.Reset();
                    _SetLayoutToControlDirect(layout);
                }
                finally
                {
                    this.EndInit();
                }
            }
        }
        /// <summary>
        /// Vynuluje layout
        /// </summary>
        public void Reset()
        {
            this.DataSource = null;
            string emptyLayout = @"<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='20.1.6.0'>
  <Chart AppearanceNameSerializable='Default' SelectionMode='None' SeriesSelectionMode='Series'>
    <DataContainer ValidateDataMembers='true' BoundSeriesSorting='None'>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
  </Chart>
</ChartXmlSerializer>";
            emptyLayout = emptyLayout.Replace("'", "\"");
            _SetLayoutToControlDirect(emptyLayout);

            this.ResetLegendTextPattern();
            this.Series.Clear();
            this.Legends.Clear();
            this.Titles.Clear();
            this.AutoLayout = false;
            this.CalculatedFields.Clear();
            this.ClearCache();
            this.Diagram = null;
        }
        /// <summary>
        /// Fyzicky vloží daný string do layoutu grafu
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayoutToControlDirect(string layout)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Windows.Forms.Clipboard.SetText(layout);
                layout = System.Windows.Forms.Clipboard.GetText();
            }

            byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
            if (buffer.Length > 0)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                    this.LoadFromStream(ms);     // Pozor, zahodí data !!!
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) 
        /// </summary>
        public bool IsChartValid { get { return (!this.IsDisposed); } }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null a není Disposed) a obsahuje data (má datový zdroj)
        /// </summary>
        public bool IsChartWorking { get { return (IsChartValid && this.DataSource != null); } }
        /// <summary>
        /// Offline uložený layout grafu, který byl setovaný zvenku; používá se při refreshi dat pro nové vložení stávajícího layoutu do grafu. 
        /// Při public čtení layoutu se nevrací tento string, ale fyzicky se načte aktuálně použitý layout z grafu.
        /// </summary>
        private string _ValidChartXmlLayout;
        /// <summary>
        /// Reference na tabulku s daty grafu, nebo null
        /// </summary>
        private object _ChartDataSource
        {
            get
            {
                var wr = _ChartDataSourceWR;
                return ((wr != null && wr.TryGetTarget(out var table)) ? table : null);
            }
            set
            {
                _ChartDataSourceWR = ((value != null) ? new WeakReference<object>(value) : null);
            }
        }
        /// <summary>
        /// WeakReference na tabulku s daty grafu
        /// </summary>
        private WeakReference<object> _ChartDataSourceWR;
        #endregion
        #region ASOL standardní rozšíření
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
        #endregion
        #region Invoke to GUI: run, get, set
        /// <summary>
        /// Metoda provede danou akci v GUI threadu
        /// </summary>
        /// <param name="action"></param>
        protected void RunInGui(Action action)
        {
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action();
        }
        /// <summary>
        /// Metoda vrátí hodnotu z GUI prvku, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected T GetGuiValue<T>(Func<T> reader)
        {
            if (this.InvokeRequired)
                return (T)this.Invoke(reader);
            else
                return reader();
        }
        /// <summary>
        /// Metoda vloží do GUI prvku danou hodnotu, zajistí si invokaci GUI threadu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        protected void SetGuiValue<T>(Action<T> writer, T value)
        {
            if (this.InvokeRequired)
                this.Invoke(writer, value);
            else
                writer(value);
        }
        #endregion
    }
    #endregion
    #region DxChartDesigner
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.Designer.ChartDesigner"/> pro editaci definice grafu
    /// </summary>
    internal class DxChartDesigner : DevExpress.XtraCharts.Designer.ChartDesigner, IEscapeHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        public DxChartDesigner(object chart) : base(chart) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="designerHost"></param>
        public DxChartDesigner(object chart, System.ComponentModel.Design.IDesignerHost designerHost) : base(chart, designerHost) { }
        /// <summary>
        /// Zobrazí designer pro daný graf, a vrátí true = OK
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="caption"></param>
        /// <param name="showActualData"></param>
        /// <param name="topMost"></param>
        /// <returns></returns>
        public static bool DesignChart(object chart, string caption, bool showActualData, bool topMost)
        {
            DxChartDesigner chartDesigner = new DxChartDesigner(chart);
            chartDesigner.Caption = caption;
            chartDesigner.ShowActualData = showActualData;
            var result = chartDesigner.ShowDialog(topMost);
            return (result == DialogResult.OK);
        }
        /// <summary>
        /// Desktop okno hlídá klávesu Escape: 
        /// c:\inetpub\wwwroot\Noris46\Noris\ClientImages\ClientWinForms\WinForms.Host\Windows\WDesktop.cs
        /// metoda escapeKeyFilter_EscapeKeyDown()
        /// Když dostane Escape, najde OnTop okno které implementuje IEscapeHandler, a zavolá zdejší metodu.
        /// My vrátíme true = OK, aby desktop dál ten Escape neřešil, a my se v klidu zavřeme nativním zpracováním klávesy Escape ve WinFormu.
        /// </summary>
        /// <returns></returns>
        public bool HandleEscapeKey()
        {
            return true;
        }
    }
    #endregion
    #region DxChartWizard
    /// <summary>
    /// Přímý potomek <see cref="DevExpress.XtraCharts.Wizard.ChartWizard"/> pro editaci definice grafu
    /// </summary>
    internal class DxChartWizard : DevExpress.XtraCharts.Wizard.ChartWizard, IEscapeHandler
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        public DxChartWizard(object chart) : base(chart) { }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="designerHost"></param>
        public DxChartWizard(object chart, System.ComponentModel.Design.IDesignerHost designerHost) : base(chart, designerHost) { }
        /// <summary>
        /// Zobrazí Wizard pro daný graf, a vrátí true = OK
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="caption"></param>
        /// <param name="showActualData"></param>
        /// <param name="topMost"></param>
        /// <returns></returns>
        public static bool ShowWizard(object chart, string caption, bool showActualData, bool topMost)
        {
            DxChartWizard chartWizard = new DxChartWizard(chart);
            chartWizard.Caption = caption;
            var result = chartWizard.ShowDialog(topMost);
            return (result == DialogResult.OK);
        }
        /// <summary>
        /// Desktop okno hlídá klávesu Escape: 
        /// c:\inetpub\wwwroot\Noris46\Noris\ClientImages\ClientWinForms\WinForms.Host\Windows\WDesktop.cs
        /// metoda escapeKeyFilter_EscapeKeyDown()
        /// Když dostane Escape, najde OnTop okno které implementuje IEscapeHandler, a zavolá zdejší metodu.
        /// My vrátíme true = OK, aby desktop dál ten Escape neřešil, a my se v klidu zavřeme nativním zpracováním klávesy Escape ve WinFormu.
        /// </summary>
        /// <returns></returns>
        public bool HandleEscapeKey()
        {
            return true;
        }
    }
    #endregion
    #region DataMenuHeaderItem + DataMenuItem + DataTextItem, interface IMenuHeaderItem + IMenuItem + ITextItem + IToolTipItem
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataMenuHeaderItem : DataMenuItem, IMenuHeaderItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataMenuHeaderItem() : base() { }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataMenuHeaderItem"/>, které bude obsahovat data z dodané <see cref="IMenuHeaderItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataMenuHeaderItem CreateClone(IMenuHeaderItem source, Action<DataMenuHeaderItem> modifier = null)
        {
            if (source == null) return null;
            DataMenuHeaderItem clone = new DataMenuHeaderItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(IMenuHeaderItem source)
        {
            base.FillFrom((IMenuItem)source);

            IsMultiColumn = source.IsMultiColumn;
            UseLargeImages = source.UseLargeImages;
            ColumnCount = source.ColumnCount;
            ItemDisplayMode = source.ItemDisplayMode;
        }
        /// <summary>
        /// Záhlaví definuje skupinu zobrazující více sloupců vedle sebe
        /// </summary>
        public virtual bool IsMultiColumn { get; set; }
        /// <summary>
        /// Velikost obrázků je Large?
        /// </summary>
        public virtual bool UseLargeImages { get; set; }
        /// <summary>
        /// Počet sloupců
        /// </summary>
        public virtual int? ColumnCount { get; set; }
        /// <summary>
        /// Režim zobrazení prvků v této skupině
        /// </summary>
        public virtual MenuItemDisplayMode ItemDisplayMode { get; set; }
    }
    /// <summary>
    /// Definice prvku, který reprezentuje Header v menu. Header dovoluje specifikovat vzhled "své" skupiny = položky následující za tímto Headerem.
    /// Typicky se použije pro skupinu prvků menu se zobrazením MultiColumn.
    /// Další skupina s jiným nastavením musí být zahájena novou instanci <see cref="IMenuHeaderItem"/>.
    /// </summary>
    public interface IMenuHeaderItem : IMenuItem
    {
        /// <summary>
        /// Záhlaví definuje skupinu zobrazující více sloupců vedle sebe
        /// </summary>
        bool IsMultiColumn { get; }
        /// <summary>
        /// Velikost obrázků je Large?
        /// </summary>
        bool UseLargeImages { get; }
        /// <summary>
        /// Počet sloupců
        /// </summary>
        int? ColumnCount { get; }
        /// <summary>
        /// Režim zobrazení prvků v této skupině
        /// </summary>
        MenuItemDisplayMode ItemDisplayMode { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataMenuItem : DataTextItem, IMenuItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataMenuItem() : base() 
        {
            this.ItemType = MenuItemType.MenuItem;
            this.ChangeMode = ContentChangeMode.ReFill;
            this.SubItems = null;

        }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataMenuItem"/>, které bude obsahovat data z dodané <see cref="IMenuItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataMenuItem CreateClone(IMenuItem source, Action<DataMenuItem> modifier = null)
        {
            if (source == null) return null;
            DataMenuItem clone = new DataMenuItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(IMenuItem source)
        {
            base.FillFrom((ITextItem)source);

            ParentItem = source.ParentItem;
            ItemType = source.ItemType;
            ChangeMode = source.ChangeMode;
            HotKeys = source.HotKeys;
            Shortcut = source.Shortcut;
            HotKey = source.HotKey;
            SubItems = (source.SubItems != null ? new List<IMenuItem>(source.SubItems) : null);
            ClickAction = source.ClickAction;
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="DataTextItem.ToString()"/>
        /// </summary>
        protected override string DebugText
        {
            get
            {
                string debugText = $"Id: {_ItemId}; Text: {Text}; Type: {ItemType}";
                if (this.SubItems != null)
                    debugText += $"; SubItems: {this.SubItems.Count}";
                return debugText;
            }
        }
        /// <summary>
        /// Z dodané kolekce prvků sestaví setříděný List a vrátí jej
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<IMenuItem> SortItems(IEnumerable<IMenuItem> items)
        {
            List<IMenuItem> list = new List<IMenuItem>();
            if (items != null)
                list.AddRange(items.Where(p => p != null));
            if (list.Count > 1)
            {
                int itemOrder = 0;
                foreach (var item in list)
                {
                    if (item.ItemOrder == 0) item.ItemOrder = ++itemOrder; else if (item.ItemOrder > itemOrder) itemOrder = item.ItemOrder;
                }
                list.Sort((a, b) => a.ItemOrder.CompareTo(b.ItemOrder));
            }
            return list;
        }
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Typ položky
        /// </summary>
        public virtual MenuItemType ItemType { get; set; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        public virtual ContentChangeMode ChangeMode { get; set; }
        /// <summary>
        /// Přímo definovaná HotKey, má přednost před <see cref="Shortcut"/> i před <see cref="HotKey"/>
        /// </summary>
        public virtual Keys? HotKeys { get; set; }
        /// <summary>
        /// Klávesová zkratka, má přednost před <see cref="HotKey"/>
        /// </summary>
        public virtual Shortcut? Shortcut { get; set; }
        /// <summary>
        /// Klávesa
        /// </summary>
        public virtual string HotKey { get; set; }
        /// <summary>
        /// Uživatelsky čitelný text HotKey. Nemusí být naplněn, pak se vytvoří systémový popisek.
        /// </summary>
        public virtual string ShortcutText { get; set; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu.
        /// Výchozí hodnota je null.
        /// </summary>
        public virtual List<IMenuItem> SubItems { get; set; }
        /// <summary>
        /// V deklaraci interface je IEnumerable...
        /// </summary>
        IEnumerable<IMenuItem> IMenuItem.SubItems { get { return this.SubItems; } }
        /// <summary>
        /// SubPoložky tohoto prvku jsou načítány OnDemand?
        /// <para/>
        /// Hodnota true = ano: po rozbalení menu je zobrazen jen zástupný prvek a je vyvolána událost <see cref="DxPopupMenu.OnDemandLoad"/>.
        /// Obsluha události najde reálné SubItems, vepíše je do prvku, a vyvolá metodu <see cref="DxPopupMenu.OnDemandLoadArgs.FillSubMenu()"/>.
        /// Položky ze SubItems se zobrazí v podřízeném menu.
        /// <para/>
        /// Pokud položky menu mají být refreshovány pokaždé (tzn i pro další a další zobrazení Popup), 
        /// pak obsluha eventu musí nastavit <see cref="DxPopupMenu.OnDemandLoadArgs.IsDynamic"/> = true.
        /// <para/>
        /// Hodnota false (default) = SubMenu je statické.
        /// </summary>
        public virtual bool SubItemsIsOnDemand { get; set; }
        /// <summary>
        /// Titulek ToolTipu (pokud není zadán explicitně) se přebírá z textu prvku
        /// </summary>
        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? Text; } }
        /// <summary>
        /// Explicitně daná akce po aktivaci této položky menu
        /// </summary>
        [XS.PersistingEnabled(false)]
        public Action<IMenuItem> ClickAction { get; set; }
        /// <summary>
        /// Explicitně daná akce po změně hodnoty <see cref="ITextItem.Checked"/> této položky menu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public Action<IMenuItem> CheckAction { get; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd)
    /// </summary>
    public interface IMenuItem : ITextItem
    {
        /// <summary>
        /// Parent prvku = jiný prvek <see cref="IMenuItem"/>
        /// </summary>
        IMenuItem ParentItem { get; set; }
        /// <summary>
        /// Typ položky
        /// </summary>
        MenuItemType ItemType { get; }
        /// <summary>
        /// Režim pro vytvoření / refill / remove tohoto prvku
        /// </summary>
        ContentChangeMode ChangeMode { get; }
        /// <summary>
        /// Přímo definovaná HotKey, má přednost před <see cref="Shortcut"/> i před <see cref="HotKey"/>
        /// </summary>
        Keys? HotKeys { get; }
        /// <summary>
        /// Klávesová zkratka, má přednost před <see cref="HotKey"/>
        /// </summary>
        Shortcut? Shortcut { get; }
        /// <summary>
        /// Klávesa
        /// </summary>
        string HotKey { get; }
        /// <summary>
        /// Uživatelsky čitelný text HotKey. Nemusí být naplněn, pak se vytvoří systémový popisek.
        /// </summary>
        string ShortcutText { get; }
        /// <summary>
        /// Subpoložky (definují prvky Menu, DropDown, SplitButton). Mohou být rekurzivně naplněné = vnořená menu
        /// </summary>
        IEnumerable<IMenuItem> SubItems { get; }
        /// <summary>
        /// SubPoložky tohoto prvku jsou načítány OnDemand?
        /// <para/>
        /// Hodnota true = ano: po rozbalení menu je zobrazen jen zástupný prvek a je vyvolána událost <see cref="DxPopupMenu.OnDemandLoad"/>.
        /// Obsluha události najde reálné SubItems, vepíše je do prvku, a vyvolá metodu <see cref="DxPopupMenu.OnDemandLoadArgs.FillSubMenu()"/>.
        /// Položky ze SubItems se zobrazí v podřízeném menu.
        /// <para/>
        /// Pokud položky menu mají být refreshovány pokaždé (tzn i pro další a další zobrazení Popup), 
        /// pak obsluha eventu musí nastavit <see cref="DxPopupMenu.OnDemandLoadArgs.IsDynamic"/> = true.
        /// <para/>
        /// Hodnota false (default) = SubMenu je statické.
        /// </summary>
        bool SubItemsIsOnDemand { get; }
        /// <summary>
        /// Explicitně daná akce po aktivaci této položky menu
        /// </summary>
        Action<IMenuItem> ClickAction { get; }
        /// <summary>
        /// Explicitně daná akce po změně hodnoty <see cref="ITextItem.Checked"/> této položky menu.
        /// V době volání této akce už je hodnota změněna. Volající garantuje, že skutečně došlo ke změně.
        /// </summary>
        Action<IMenuItem> CheckAction { get; }
    }
    /// <summary>
    /// Třída pro handlery, které dostávají prvek Menu, na který bylo kliknuto
    /// </summary>
    public class MenuItemClickArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="item">Prvek, na který bylo kliknuto</param>
        public MenuItemClickArgs(IMenuItem item)
        {
            this.Item = item;
        }
        /// <summary>
        /// Prvek, na který bylo kliknuto
        /// </summary>
        public IMenuItem Item { get; private set; }
    }
    /// <summary>
    /// Definice prvku umístěného jako stránka v záložkovníku
    /// </summary>
    public class DataPageItem : DataTextItem, IPageItem
    {
        /// <summary>
        /// Identifikátor stránky z pohledu aplikace.
        /// Tato hodnota může být prázdná nebo null, a v tom stavu i zůstane.
        /// Tato hodnota se posílá aplikaci, když aplikace chce identifikovat objekt stránky. Aplikaci nikdy neposíláme ItemId!
        /// Tato hodnota, pokud je neprázdná, se ukládá i do <see cref="ITextItem.ItemId"/>, protože pak slouží jako reálná identifikace objektu stránky.
        /// Pokud je ale <see cref="PageId"/> prázdné, pak ji do <see cref="ITextItem.ItemId"/> neukládáme, a defaultní implementace hodnoty <see cref="ITextItem.ItemId"/>
        /// ji naplní unikátním GUIDem, který slouží jako identifikátor stránky uvnitř komponenty.
        /// <para/>
        /// Tedy shrnutí:
        /// 1. Klíč stránky z aplikace uložíme vždy do <see cref="PageId"/>, a pokud není prázdný pak i do <see cref="ITextItem.ItemId"/>;
        /// (toto setování zajišťuje přímo tato třída <see cref="DataPageItem"/> při setování hodnoty do této property <see cref="PageId"/>!)
        /// 2. S komponentou komunikujeme přes <see cref="ITextItem.ItemId"/>;
        /// 3. S aplikací komunikujeme přes <see cref="PageId"/>.
        /// Do aplikace nikdy neposílám <see cref="ITextItem.ItemId"/>, protože tam může být privátní GUID, který aplikace nezná.
        /// </summary>
        public virtual string PageId 
        { 
            get { return _PageId; }
            set
            {
                _PageId = value;
                if (!String.IsNullOrEmpty(value))
                    base.ItemId = value;
            }
        }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        public override string ItemId 
        {
            get { return base.ItemId; }     // Pro čtení platí pravidla ItemId: čteme reálnou hodnotu uloženou v _ItemId, nebo (pro null) vygenerujeme new GUID a ten pak používáme.
            set { this.PageId = value; }    // Pro zápis platí pravidla PageId: setujeme jen neprázdnou hodnotu.
        }
        private string _PageId;
        /// <summary>
        /// Zobrazit Close button?
        /// </summary>
        public virtual bool CloseButtonVisible { get; set; }
        /// <summary>
        /// Prvek stránky reprezentující vizuální control
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual Control PageControl { get { return _PageControl?.Target; } set { _PageControl = value; } }
        private WeakTarget<Control> _PageControl;
    }
    /// <summary>
    /// Definice prvku umístěného jako stránka v záložkovníku
    /// </summary>
    public interface IPageItem : ITextItem
    {
        /// <summary>
        /// Identifikátor stránky z pohledu aplikace.
        /// Tato hodnota může být prázdná nebo null, a v tom stavu i zůstane.
        /// Tato hodnota se posílá aplikaci, když aplikace chce identifikovat objekt stránky. Aplikaci nikdy neposíláme ItemId!
        /// Tato hodnota, pokud je neprázdná, se ukládá i do <see cref="ITextItem.ItemId"/>, protože pak slouží jako reálná identifikace objektu stránky.
        /// Pokud je ale <see cref="PageId"/> prázdné, pak ji do <see cref="ITextItem.ItemId"/> neukládáme, a defaultní implementace hodnoty <see cref="ITextItem.ItemId"/>
        /// ji naplní unikátním GUIDem, který slouží jako identifikátor stránky uvnitř komponenty.
        /// <para/>
        /// Tedy shrnutí:
        /// 1. Klíč stránky z aplikace uložíme vždy do <see cref="PageId"/>, a pokud není prázdný pak i do <see cref="ITextItem.ItemId"/>;
        /// 2. S komponentou komunikujeme přes <see cref="ITextItem.ItemId"/>;
        /// 3. S aplikací komunikujeme přes <see cref="PageId"/>
        /// </summary>
        string PageId { get; }
        /// <summary>
        /// Zobrazit Close button?
        /// </summary>
        bool CloseButtonVisible { get; }
        /// <summary>
        /// Prvek stránky reprezentující vizuální control
        /// </summary>
        Control PageControl { get; set; }
    }
    /// <summary>
    /// Definice prvku umístěného v Ribbonu nebo podpoložka prvku Ribbonu (položka menu / split ribbonu atd) nebo jako prvek ListBoxu nebo ComboBoxu
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class DataTextItem : ITextItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataTextItem()
        {
            this._ItemId = null;
            this.Visible = true;
            this.Enabled = true;
            this.ImageFromCaptionMode = ImageFromCaptionType.OnlyForRootMenuLevel;
        }
        /// <summary>
        /// Metoda vytvoří new instanci třídy <see cref="DataMenuItem"/>, které bude obsahovat data z dodané <see cref="IMenuItem"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static DataTextItem CreateClone(ITextItem source, Action<DataTextItem> modifier = null)
        {
            if (source == null) return null;
            DataTextItem clone = new DataTextItem();
            clone.FillFrom(source);
            if (modifier != null) modifier(clone);
            return clone;
        }
        /// <summary>
        /// Do this instance přenese patřičné hodnoty ze source instance
        /// </summary>
        /// <param name="source"></param>
        protected void FillFrom(ITextItem source)
        {
            ItemId = source.ItemId;
            Text = source.Text;
            ItemOrder = source.ItemOrder;
            ItemIsFirstInGroup = source.ItemIsFirstInGroup;
            Visible = source.Visible;
            Enabled = source.Enabled;
            Checked = source.Checked;
            Image = source.Image;
            ImageName = source.ImageName;
            ImageNameUnChecked = source.ImageNameUnChecked;
            ImageNameChecked = source.ImageNameChecked;
            ImageFromCaptionMode = source.ImageFromCaptionMode;
            ItemPaintStyle = source.ItemPaintStyle;
            ToolTipIcon = source.ToolTipIcon;
            ToolTipTitle = source.ToolTipTitle;
            ToolTipText = source.ToolTipText;
            ToolTipAllowHtml = source.ToolTipAllowHtml;
            Tag = source.Tag;
        }
        /// <summary>
        /// Vizualizace = pro přímé použití v GUI objektech (např. jako prvek ListBoxu)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (this.Text ?? "");
        }
        /// <summary>
        /// Text zobrazovaný v debuggeru namísto <see cref="ToString()"/>
        /// </summary>
        protected virtual string DebugText
        {
            get
            {
                string debugText = $"Id: {_ItemId}; Text: {Text}";
                return debugText;
            }
        }
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        public virtual string ItemId 
        {
            get
            {
                if (_ItemId == null) _ItemId = DxComponent.CreateGuid();
                return _ItemId;
            }
            set { _ItemId = value; }
        }
        /// <summary>
        /// Reálně uložené ID
        /// </summary>
        protected string _ItemId;
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        public virtual string Text { get; set; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        public virtual int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje true tehdy, když před prvkem má být oddělovač
        /// </summary>
        public virtual bool ItemIsFirstInGroup { get; set; }
        /// <summary>
        /// Prvek je Visible?
        /// </summary>
        public virtual bool Visible { get; set; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        public virtual bool Enabled { get; set; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Menu / Ribbonu (uživatelem) je do této property setována aktuální hodnota z Menu / Ribbonu,
        /// a poté je vyvolána odpovídající událost ItemClick.
        /// Zadaná hodnota může být null (pak ikona je <see cref="ImageName"/>), pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat)
        /// </summary>
        public virtual bool? Checked { get; set; }
        /// <summary>
        /// Styl písma, null = neměnit
        /// </summary>
        public virtual FontStyle? FontStyle { get; set; }
        /// <summary>
        /// Fyzický obrázek ikony.
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual Image Image { get; set; }
        /// <summary>
        /// Fyzický vektor ikony
        /// </summary>
        [XS.PersistingEnabled(false)]
        public virtual DevExpress.Utils.Svg.SvgImage SvgImage { get; set; }
        /// <summary>
        /// Jméno běžné ikony.
        /// Pro prvek typu <see cref="RibbonItemType.CheckBoxToggle"/> a <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/> tato ikona reprezentuje stav, kdy <see cref="Checked"/> = NULL.
        /// </summary>
        public virtual string ImageName { get; set; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/> a <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/>.
        /// Pokud je prázdné, pak se pro stav UnChecked použije <see cref="ImageName"/>.
        /// </summary>
        public virtual string ImageNameUnChecked { get; set; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/> a <see cref="RibbonItemType.CheckButton"/> a <see cref="RibbonItemType.CheckButtonPassive"/>.
        /// Pokud je prázdné, pak se pro stav Checked použije <see cref="ImageName"/>.
        /// </summary>
        public virtual string ImageNameChecked { get; set; }
        /// <summary>
        /// Povoluje se vytvoření obrázku podle textu? Používá se v Nephrite pro ikony například vztahů...
        /// </summary>
        public virtual ImageFromCaptionType ImageFromCaptionMode { get; set; }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        public virtual BarItemPaintStyle ItemPaintStyle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se <see cref="Text"/>.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Text ToolTipu smí obsahovat HTML? Hodnota null = AutoDetect
        /// </summary>
        public virtual bool? ToolTipAllowHtml { get; set; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        [XS.PersistingEnabled(false)]
        public object Tag { get; set; }
        /// <summary>
        /// Titulek okna ToolTip (může obsahovat Text, když to dává smysl)
        /// </summary>
        string IToolTipItem.ToolTipTitle 
        {
            get 
            {
                if (!String.IsNullOrEmpty(ToolTipTitle)) return ToolTipTitle;  // Explicitně zadaný titulek je jasný
                if (!String.IsNullOrEmpty(ToolTipText)) return Text;           // Toť oříšek... : Pokud je specifikován 'ToolTipText' (tedy budeme zobrazovat nějaký rozšiřující text), pak jako Titulek použijeme základní Text prvku (button, záhlaví stránky, atd)
                return null;                                                   // Ale když uživatel nezadal ani explicitní titulek, ani text ToolTipu, tak samotný náhradní Titulek nemá smysl.
            }
        }
    }
    /// <summary>
    /// Definice jednoduchého prvku, který nese ID, text, ikony, tooltip a Tag
    /// </summary>
    public interface ITextItem : IToolTipItem
    {
        /// <summary>
        /// Stringová identifikace prvku, musí být jednoznačná v rámci nadřízeného prvku
        /// </summary>
        string ItemId { get; }
        /// <summary>
        /// Hlavní text v prvku
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Pořadí prvku, použije se pro setřídění v rámci nadřazeného prvku
        /// </summary>
        int ItemOrder { get; set; }
        /// <summary>
        /// Obsahuje tre tehdy, když před prvkem má být oddělovač
        /// </summary>
        bool ItemIsFirstInGroup { get; }
        /// <summary>
        /// Prvek je Visible?
        /// </summary>
        bool Visible { get; }
        /// <summary>
        /// Prvek je Enabled?
        /// </summary>
        bool Enabled { get; }
        /// <summary>
        /// Určuje, zda CheckBox je zaškrtnutý.
        /// Po změně zaškrtnutí v Ribbonu (uživatelem) je do této property setována aktuální hodnota z Ribbonu 
        /// a poté je vyvolána událost <see cref="DxRibbonControl.RibbonItemClick"/>.
        /// Hodnota může být null, pak první kliknutí nastaví false, druhé true, třetí zase false (na NULL se interaktivně nedá doklikat).
        /// <para/>
        /// Pokud konkrétní prvek nepodporuje null, akceptuje null jako false.
        /// </summary>
        bool? Checked { get; set; }
        /// <summary>
        /// Styl písma, null = neměnit
        /// </summary>
        FontStyle? FontStyle { get; }
        /// <summary>
        /// Fyzický obrázek ikony.
        /// </summary>
        Image Image { get; }
        /// <summary>
        /// Fyzický vektor ikony
        /// </summary>
        DevExpress.Utils.Svg.SvgImage SvgImage { get; }
        /// <summary>
        /// Jméno ikony.
        /// Pro prvek typu CheckBox tato ikona reprezentuje stav, kdy <see cref="Checked"/> = NULL.
        /// </summary>
        string ImageName { get; }
        /// <summary>
        /// Jméno ikony pro stav UnChecked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ImageNameUnChecked { get; }
        /// <summary>
        /// Jméno ikony pro stav Checked u typu <see cref="RibbonItemType.CheckBoxToggle"/>
        /// </summary>
        string ImageNameChecked { get; }
        /// <summary>
        /// Povoluje se vytvoření obrázku podle textu? Používá se v Nephrite pro ikony například vztahů...
        /// </summary>
        ImageFromCaptionType ImageFromCaptionMode { get; }
        /// <summary>
        /// Styl zobrazení
        /// </summary>
        BarItemPaintStyle ItemPaintStyle { get; }
        /// <summary>
        /// Libovolná data aplikace
        /// </summary>
        object Tag { get; }
    }
    /// <summary>
    /// Interface definující vlastnosti prvku, který může nabídnout ToolTip
    /// </summary>
    public interface IToolTipItem
    {
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        string ToolTipIcon { get; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        string ToolTipTitle { get; }
        /// <summary>
        /// Text ToolTipu
        /// </summary>
        string ToolTipText { get; }
        /// <summary>
        /// Text ToolTipu smí obsahovat HTML? Hodnota null = AutoDetect
        /// </summary>
        bool? ToolTipAllowHtml { get; }
    }
    /// <summary>
    /// Interface
    /// </summary>
    public interface IHotKeyControl
    {
        /// <summary>
        /// Definice klávesy
        /// </summary>
        Keys HotKey { get; }
        /// <summary>
        /// Byla detekována zdejší <see cref="HotKey"/>, má být provedena odpovídající akce
        /// </summary>
        void PerformClick();
    }
    #endregion
    #region class ControlItemLayoutInfo
    /// <summary>
    /// Třída pro výpočty layoutu controlů
    /// </summary>
    internal class ControlItemLayoutInfo
    {
        /// <summary>
        /// Fyzický control, smí být null
        /// </summary>
        public Control Control { get; set; }
        /// <summary>
        /// Požadovaná velikost
        /// </summary>
        public Size Size { get; set; }
        /// <summary>
        /// Umístění.
        /// Setování hodnoty se propíše i do <see cref="Control"/>, pokud ten není null.
        /// </summary>
        public Rectangle Bounds 
        {
            get { return _Bounds; }
            set
            {
                _Bounds = value;
                if (Control != null)
                    Control.Bounds = value;
            }
        }
        private Rectangle _Bounds;
    }
    #endregion
    #region class UndoRedoController
    /// <summary>
    /// Kontroller pro akce Undo a Redo
    /// </summary>
    public class UndoRedoController
    {
        /// <summary>
        /// Konstruktor, lze zadat maximální počet kroků Undo
        /// </summary>
        /// <param name="maxStepCount"></param>
        public UndoRedoController(int maxStepCount = 64)
        {
            _MaxStepCount = (maxStepCount < 4 ? 4 : (maxStepCount > 1024 ? 1024 : maxStepCount));
            _Steps = new List<UndoRedoStep>();
            _Pointer = 0;
        }
        /// <summary>
        /// Maximální počet evidovaných kroků
        /// </summary>
        private int _MaxStepCount;
        /// <summary>
        /// Uložené stavy = jednotlivé kroky zpět a vpřed, podle pointeru <see cref="_Pointer"/>
        /// </summary>
        private List<UndoRedoStep> _Steps;
        /// <summary>
        /// Ukazatel.
        /// <para/>
        /// Výchozí hodnota po inicializaci je 0. Protože i <see cref="_Steps"/> má 0 prvků, nelze provést ani Undo, ani Redo.
        /// Undo lze provést tehdy, když <see cref="_Pointer"/> je větší než 0.
        /// Redo lze provést tehdy, když <see cref="_Pointer"/> je menší než poslední index.
        /// <para/>
        /// Po přidání prvního (a jakéhokoli dalšího) stavu je do <see cref="_Pointer"/> vložena hodnota <see cref="_Steps"/>.Count.
        /// <see cref="_Pointer"/> tedy ukazuje za posledně přidaný Step.
        /// Pokud <see cref="_Pointer"/> je větší než 0, lze provést Undo.
        /// <para/>
        /// Provedení akce Undo (metoda <see cref="DoUndo()"/>) vezme stav <see cref="_Pointer"/>, sníží jej o 1, 
        /// a výslednou hodnotu použije jako index do pole <see cref="_Steps"/>, odkud vezme stav pro vrácení aplikaci.
        /// 
        /// 
        /// </summary>
        private int _Pointer;
        /// <summary>
        /// Aplikace provedla změny dat a nyní je chce uložit do UndoRedo containeru.
        /// Typicky první stav se vkládá po dokončení inicializace controlu = naplnění daty, těsně před představením uživateli.
        /// Poté uživatel může provést změny dat, po dokončení jeho změny se stav dat opět uloží do this UndoRedo containeru.
        /// <para/>
        /// Pokud aplikace před vložením stavu pomocí této metody provedla nějaké kroky Undo (tj. byla tu možnost dát Redo), 
        /// pak zdejší metoda <see cref="AddState(IUndoRedoControl, object)"/> zahodí kroky Redo (=odrolované kroky), už nebude možné se k nim vrátit.
        /// To vychází z elementární logiky: Pokud provedu kroky editace A1, A2, A3, A4; potom krok A4 zruším a vrátím se na A3, a provedu B4, 
        /// pak je nesmysl provádět Redo do kroku A4.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="state"></param>
        public void AddState(IUndoRedoControl control, object state)
        {
            _ClearRedoSteps();
            _ClearDeadControls();
            _ClearOldMaxSteps(1);
            _Steps.Add(new UndoRedoStep(control, state));
            _Pointer = _Count;
        }
        /// <summary>
        /// Požádá o provedení kroku UNDO.
        /// Najde se nejbližší krok zpět, najde se jeho control a provede se.
        /// </summary>
        public void DoUndo()
        {
            int start = _Pointer - 1;
            for (int pointer = start; pointer >= 0; pointer--)
            {
                var step = _Steps[pointer];
                if (step != null && step.IsAlive)
                {
                    _Pointer = (pointer > 0 ? pointer : 0);
                    step.DoUndoStep();
                    break;
                }
            }
        }
        /// <summary>
        /// Požádá o provedení kroku REDO.
        /// Najde se nejbližší krok vpřed, najde se jeho control a provede se.
        /// </summary>
        public void DoRedo()
        {
            int start = _Pointer;
            for (int pointer = start; pointer < _Steps.Count; pointer++)
            {
                var step = _Steps[pointer];
                if (step != null && step.IsAlive)
                {
                    _Pointer = (pointer > 0 ? pointer : 0);
                    step.DoRedoStep();
                    break;
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud uživatel může dát UNDO
        /// </summary>
        public bool UndoEnabled { get { return (_Pointer > 0); } }
        /// <summary>
        /// Obsahuje true, pokud uživatel může dát REDO
        /// </summary>
        public bool RedoEnabled { get { return (_Count > 0 && _Pointer <= _Count); } }
        /// <summary>
        /// Událost je vyvolaná tehdy, když se změní stav <see cref="UndoEnabled"/> nebo <see cref="RedoEnabled"/>.
        /// Eventhandler této události pak může reagovat změnou Enabled odpovídajících buttonů.
        /// </summary>
        public event EventHandler UndoRedoEnabledChanged;
        private int _Count { get { return _Steps.Count; } }
        /// <summary>
        /// Odstraní dostupné kroky REDO.
        /// Používá se před vložením nového stavu v metodě <see cref="AddState(IUndoRedoControl, object)"/>.
        /// </summary>
        private void _ClearRedoSteps()
        {
            int oldCount = _Count;
            int pointer = _Pointer;
            if (pointer >= oldCount) return;

        }
        /// <summary>
        /// Odstraní kroky, které náleží controlům, které už neexistují.
        /// </summary>
        private void _ClearDeadControls()
        {
            _Steps.RemoveAll(s => !s.IsAlive);
        }
        /// <summary>
        /// Odstraní staré stavy před přidáním nového jednoho. Účelem je nepřekročit počet uložených stavů <see cref="_MaxStepCount"/>.
        /// </summary>
        /// <param name="addCount">Kolik pozic chci odebrat navíc protože je budu přidávat? Typicky 1 v <see cref="AddState(IUndoRedoControl, object)"/>.</param>
        private void _ClearOldMaxSteps(int addCount)
        {
            int oldCount = _Count;
            int newCount = oldCount + addCount;
            int maxCount = _MaxStepCount;
            if (newCount <= maxCount) return;

            int index = 1;
            int remCount = newCount - maxCount;
            _Steps.RemoveRange(index, remCount);
            _Pointer -= remCount;
            if (_Pointer < 1) _Pointer = 1;
        }
        /// <summary>
        /// Jeden krok v zásobníku UndoRedo. Obsahuje control, který bude krok provádět, a jeho data.
        /// </summary>
        private class UndoRedoStep
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="control"></param>
            /// <param name="state"></param>
            public UndoRedoStep(IUndoRedoControl control, object state)
            {
                __Control = control != null ? new WeakTarget<IUndoRedoControl>(control) : null;
                State = state;
            }
            /// <summary>
            /// Control, který bude provádět kroky Undo/Redo pomocí metody <see cref="IUndoRedoControl.DoUndoStep(object)"/> a <see cref="IUndoRedoControl.DoRedoStep(object)"/>
            /// </summary>
            public IUndoRedoControl Control { get { return __Control?.Target; } }
            private WeakTarget<IUndoRedoControl> __Control;
            /// <summary>
            /// Obsahuje true, pokud je control <see cref="Control"/> živý.
            /// </summary>
            public bool IsAlive { get { return __Control != null && __Control.IsAlive; } }
            /// <summary>
            /// Stavová data
            /// </summary>
            public object State { get; set; }
            /// <summary>
            /// Control <see cref="Control"/> provede svoji akci <see cref="IUndoRedoControl.DoUndoStep(object)"/> pro zde uložená data
            /// </summary>
            public void DoUndoStep()
            {
                if (IsAlive)
                    Control.DoUndoStep(State);
            }
            /// <summary>
            /// Control <see cref="Control"/> provede svoji akci <see cref="IUndoRedoControl.DoRedoStep(object)"/> pro zde uložená data
            /// </summary>
            public void DoRedoStep()
            {
                if (IsAlive)
                    Control.DoRedoStep(State);
            }
        }
    }
    /// <summary>
    /// Rozhraní, které musí implementovat jakýkoli prvek, aby mohl provádět kroky Undo a Redo.
    /// Každý takový krok je prováděn tak, že daný Control je zavolán z controlleru <see cref="UndoRedoController"/> 
    /// do metody <see cref="IUndoRedoControl.DoUndoStep(object)"/> nebo <see cref="IUndoRedoControl.DoRedoStep(object)"/>,
    /// controlu je předán jeho vlastní uložený stav, a control podle něj nastaví svoje hodnoty.
    /// <para/>
    /// Tato akce je výsledkem volání <see cref="UndoRedoController.DoUndo()"/> nebo <see cref="UndoRedoController.DoRedo()"/>, typicky z klávesové zkratky nebo z tlačítka (Ribbon, Button).
    /// Vkládaný stav (parametr 'state' metod <see cref="IUndoRedoControl.DoUndoStep(object)"/> a <see cref="IUndoRedoControl.DoRedoStep(object)"/>) 
    /// si daný control uložil ve vhodném okamžiku do controlleru <see cref="UndoRedoController"/>, 
    /// typicky při provádění nějakého uživatelského editačního kroku.
    /// </summary>
    public interface IUndoRedoControl
    {
        /// <summary>
        /// Control si má nastavit svoje vizuální data podle hodnot z dodaného objektu <paramref name="state"/>, pro akci Undo = zrušit změny provedené v daném kroku.
        /// Tento objekt si control uložil do controlleru voláním metody <see cref="UndoRedoController.AddState(IUndoRedoControl, object)"/>.
        /// </summary>
        /// <param name="state"></param>
        void DoUndoStep(object state);
        /// <summary>
        /// Control si má nastavit svoje vizuální data podle hodnot z dodaného objektu <paramref name="state"/>, pro akci Redo = znovou provést daný krok.
        /// Tento objekt si control uložil do controlleru voláním metody <see cref="UndoRedoController.AddState(IUndoRedoControl, object)"/>.
        /// </summary>
        /// <param name="state"></param>
        void DoRedoStep(object state);
    }
    #endregion
    #region class PdfPrinter
    /// <summary>
    /// Support pro DirectPdfPrint
    /// </summary>
    public class PdfPrinter
    {
        /// <summary>
        /// DirectPdfPrint ze souboru na disku do zadané tiskárny, pomocí tiskového procesoru <see cref="DevExpress.Pdf.PdfDocumentProcessor"/>, který je nevizuální.
        /// Režie s vytvořením procesoru je zanedbatelná vůči času LoadDocument a Print.
        /// </summary>
        /// <param name="pdfFile"></param>
        /// <param name="printArgs"></param>
        public static void PrintWithProcess(string pdfFile, PrintArgs printArgs = null)
        {
            using (DevExpress.Pdf.PdfDocumentProcessor processor = new DevExpress.Pdf.PdfDocumentProcessor())
            {
                processor.LoadDocument(pdfFile);
                processor.Document.Title = System.IO.Path.GetFileNameWithoutExtension(pdfFile);

                if (printArgs is null) printArgs = PrintArgs.Default;
                var pdfSettings = printArgs.CreateSettings(processor.Document.Pages.Count);
                processor.Print(pdfSettings);

                processor.CloseDocument();
            }
        }
        /// <summary>
        /// DirectPdfPrint ze souboru na disku do zadané tiskárny, pomocí GUI controlu <see cref="DevExpress.XtraPdfViewer.PdfViewer"/>, který není zobrazován.
        /// </summary>
        /// <param name="pdfFile"></param>
        /// <param name="printArgs"></param>
        public static void PrintWithControl(string pdfFile, PrintArgs printArgs = null)
        {
            using (DevExpress.XtraPdfViewer.PdfViewer pdfViewer = new DevExpress.XtraPdfViewer.PdfViewer())
            {
                pdfViewer.LoadDocument(pdfFile);

                if (printArgs is null) printArgs = PrintArgs.Default;
                var pdfSettings = printArgs.CreateSettings(pdfViewer.PageCount);

                pdfViewer.ShowPrintStatusDialog = printArgs.ShowPrintStatusDialog;
                pdfViewer.Print(pdfSettings);

                pdfViewer.CloseDocument();
            }
        }
        /// <summary>
        /// Instalované tiskárny
        /// </summary>
        public static string[] InstalledPrinters
        {
            get
            {
                var printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                string[] result = new string[printers.Count];
                printers.CopyTo(result, 0);
                return result;

                /*  Odvážnější řešení:
                
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");

        foreach (ManagementObject printer in searcher.Get())
        {
            string printerName = printer["Name"].ToString().ToLower();
            Console.WriteLine("Printer :" + printerName);

            PrintProps(printer, "Caption");
            PrintProps(printer, "ExtendedPrinterStatus");
            PrintProps(printer, "Availability");
            PrintProps(printer, "Default");
            PrintProps(printer, "DetectedErrorState");
            PrintProps(printer, "ExtendedDetectedErrorState");
            PrintProps(printer, "ExtendedPrinterStatus");
            PrintProps(printer, "LastErrorCode");
            PrintProps(printer, "PrinterState");
            PrintProps(printer, "PrinterStatus");
            PrintProps(printer, "Status");
            PrintProps(printer, "WorkOffline");
            PrintProps(printer, "Local");
        }

                */
            }
        }
        /// <summary>
        /// Defaultní tiskárna
        /// </summary>
        public static string DefaultPrinterName
        {
            get
            {
                var setting = new System.Drawing.Printing.PrinterSettings();
                return setting.PrinterName;
            }
        }
        /// <summary>
        /// Třída argumentů pro tisk
        /// </summary>
        public class PrintArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public PrintArgs()
            {
                PrinterName = null;
                Copies = 1;
                Collate = false;
                EnableLegacyPrinting = false;
                PrintInGrayscale = false;
                PageRange = null;
                PageNumbers = null;
                PageOrientation = PageOrientation.Auto;
                ScaleMode = PrintScaleMode.ActualSize;
                Scale = null;
                ShowPrintStatusDialog = false;
            }
            /// <summary>
            /// Defaultní nastavení
            /// </summary>
            public static PrintArgs Default { get { return new PrintArgs(); } }
            /// <summary>
            /// Jméno tiskárny; prázdné = výchozí systémová
            /// </summary>
            public string PrinterName { get; set; }
            /// <summary>
            /// Počet kopií, validní hodnota = 1 až 99
            /// </summary>
            public short Copies { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether the printed document is collated.
            /// true if the printed document is collated; otherwise, false. The default is false.
            /// </summary>
            public bool Collate { get; set; }
            /// <summary>
            /// Gets or sets whether to enable the legacy printing engine.
            /// true to use the old printing engine; otherwise - false.
            /// </summary>
            public bool EnableLegacyPrinting { get; set; }
            /// <summary>
            /// Gets or sets a value which indicates whether to print the document content in grayscale.
            /// true to print a document content in grayscale; false the current printer settings are used.
            /// </summary>
            public bool PrintInGrayscale { get; set; }
            /// <summary>
            /// Rozsah tištěných stránek ve formě: "-3, 5, 12-15,  21..25; 22, 48, 47-"
            /// (čárkou oddělené jednotlivé stránky nebo rozsahy, pomlčkou nebo dvojtečkou oddělené Min-Max rozsahy, mezery jsou optional.
            /// </summary>
            public string PageRange { get; set; }
            /// <summary>
            /// Explicitně zadaná čísla stránek k tisku. Pokud není null a má nějaký prvek, pak se převezme a poté se ignoruje <see cref="PageRange"/>.
            /// </summary>
            public int[] PageNumbers { get; set; }
            /// <summary>
            /// Specifies the orientation of pages to be printed.
            /// A DevExpress.Pdf.PdfPrintPageOrientation value. The default value is DevExpress.Pdf.PdfPrintPageOrientation.Auto.
            /// </summary>
            public PageOrientation PageOrientation { get; set; }
            /// <summary>
            /// Specifies the page scale mode when a document is printing.
            /// Default je <see cref="PrintScaleMode.ActualSize"/>.
            /// </summary>
            public PrintScaleMode ScaleMode { get; set; }
            /// <summary>
            /// Měřítko tisku pokud <see cref="ScaleMode"/> = <see cref="PrintScaleMode.CustomScale"/>, v procentech. Default = null odpovídá 100f = 100%
            /// </summary>
            public float? Scale { get; set; }
            /// <summary>
            /// Zobrazit PrintStatus okno v době tisku
            /// </summary>
            public bool ShowPrintStatusDialog { get; set; }
            /// <summary>
            /// Maximální počet kopií
            /// </summary>
            public const short MaxCopies = 99;
            /// <summary>
            /// Ze svých dat vytvoří a vrátí <see cref="DevExpress.Pdf.PdfPrinterSettings"/> pro zadaný počet stran.
            /// Ten má vliv na hodnotu <see cref="DevExpress.Pdf.PdfPrinterSettings.PageNumbers"/>.
            /// </summary>
            /// <param name="pagesCount">Počet stran aktuálního dokumentu</param>
            /// <returns></returns>
            public DevExpress.Pdf.PdfPrinterSettings CreateSettings(int? pagesCount = null)
            {
                var sysSettings = new System.Drawing.Printing.PrinterSettings();
                if (!String.IsNullOrEmpty(this.PrinterName)) sysSettings.PrinterName = this.PrinterName;
                if (this.Copies > 0) sysSettings.Copies = (this.Copies <= MaxCopies ? this.Copies : MaxCopies);
                sysSettings.Collate = this.Collate;

                var pdfSettings = new DevExpress.Pdf.PdfPrinterSettings(sysSettings);
                pdfSettings.EnableLegacyPrinting = this.EnableLegacyPrinting;
                pdfSettings.PrintInGrayscale = this.PrintInGrayscale;

                pdfSettings.PageNumbers = _GetPageNumbers(pagesCount, this.PageNumbers, this.PageRange);

                pdfSettings.PageOrientation = (this.PageOrientation == PageOrientation.Portrait ? DevExpress.Pdf.PdfPrintPageOrientation.Portrait :
                                              (this.PageOrientation == PageOrientation.Landscape ? DevExpress.Pdf.PdfPrintPageOrientation.Landscape : DevExpress.Pdf.PdfPrintPageOrientation.Auto));
                pdfSettings.ScaleMode = (this.ScaleMode == PrintScaleMode.ActualSize ? DevExpress.Pdf.PdfPrintScaleMode.ActualSize :
                                        (this.ScaleMode == PrintScaleMode.CustomScale ? DevExpress.Pdf.PdfPrintScaleMode.CustomScale : DevExpress.Pdf.PdfPrintScaleMode.Fit));
                pdfSettings.Scale = ((this.ScaleMode == PrintScaleMode.CustomScale && this.Scale.HasValue && (this.Scale.Value != 0f && this.Scale.Value != 100f)) ? this.Scale.Value : 100f);

                return pdfSettings;
            }
            /// <summary>
            /// Vrátí pole, obsahující čísla stránek
            /// </summary>
            /// <param name="pagesCount"></param>
            /// <param name="pageNumbers"></param>
            /// <param name="pageRange"></param>
            /// <returns></returns>
            private static int[] _GetPageNumbers(int? pagesCount, int[] pageNumbers, string pageRange)
            {
                Dictionary<int, int> pages = new Dictionary<int, int>();
                bool hasCount = (pagesCount.HasValue && pagesCount.Value > 0);
                bool hasData = false;
                if (pageNumbers != null)
                {   // Exaktní seznam
                    foreach (int pn in pageNumbers)
                        addPage(pn);
                }

                else if (!String.IsNullOrEmpty(pageRange))
                {   // User požadavek: "1-4, 5; 12, 20 ÷ 26;  30..33"

                    // Odstraním mezery před a po pomlčce:  "20 - 25" => "20-25" :
                    //  protože následně umožním oddělovat prvky i v mezeře: "1 2 25 40", a okolo znaku "-" by mi mezera vadila!
                    // Současně nahradím znak  ÷  za jednotný znak  -
                    while (pageRange.Contains("...")) pageRange = pageRange.Replace("...", "..");  // Z mnohonásobných teček udělám nakonec dvojtečku
                    if (pageRange.Contains("..")) pageRange = pageRange.Replace("..", "-");        // A z dvojtečky udělám standardní pomlčku
                    if (pageRange.Contains("÷")) pageRange = pageRange.Replace("÷", "-");          // Z  ÷  udělám standardní pomlčku
                    while (pageRange.Contains(" -")) pageRange = pageRange.Replace(" -", "-");     // Pomlčky zbavím sousedních mezer
                    while (pageRange.Contains("- ")) pageRange = pageRange.Replace("- ", "-");

                    var parts = pageRange.Split(',', ';', ' ');
                    foreach (var part in parts)
                    {
                        if (part.Length > 0)
                        {
                            if (part.Contains("-"))
                                addRange(part);
                            else
                                addSingle(part);
                        }
                    }
                }

                if (!hasData) return null;

                var list = pages.Keys.ToList();
                list.Sort();
                return list.ToArray();

                // Přidá stránky v daném rozsahu "5-7"   nebo  "12-"   nebo  "-4"
                void addRange(string text)
                {   // Na vstupu je string, který obsahuje mezeru, a následující Split tedy vždy vytvoří nejméně dva prvky, i kdyby prázdné stringy...
                    var minMax = text.Split('-');
                    if (minMax.Length < 2)
                    {
                        addSingle(text);
                        return;
                    }
                    bool hasMin = hasNumb(minMax[0], out var min);
                    bool hasMax = hasNumb(minMax[1], out var max);
                    if (hasMin && hasMax)
                    {   //  "12-15"   =>   vepíšu zadané, včetně poslední
                        for (int val = min; val <= max; val++)
                            addPage(val);
                    }
                    else if (hasMin)
                    {   // "12-"      =>   od zadané do poslední
                        if (hasCount)
                        {   // Od dané stránky do poslední:
                            for (int val = min; val <= pagesCount.Value; val++)
                                addPage(val);
                        }
                        else
                        {   // Nevím, která je poslední: tak jen danou stránku
                            addPage(min);
                        }
                    }
                    else if (hasMax)
                    {   // "-8"      =>   od první do zadané včetně
                        for (int val = 1; val <= max; val++)
                            addPage(val);
                    }
                }
                // testuje zda daný text je číslo
                bool hasNumb(string text, out int numb)
                {
                    if (!String.IsNullOrEmpty(text) && Int32.TryParse(text.Trim(), out numb)) return true;
                    numb = 0;
                    return false;
                }
                // přidá jednu stránku, pokud je zadané číslo
                void addSingle(string text)
                {
                    if (Int32.TryParse(text, out var number))
                        addPage(number);
                }
                // přidá číslo stránky, pokud je v platném rozsahu a pokud je to poprvé
                void addPage(int pageNumber)
                {
                    if (pageNumber <= 0 || (hasCount && pageNumber > pagesCount.Value)) return;    // Záporné, 0 a větší než Count ignoruji
                    if (!pages.ContainsKey(pageNumber)) pages.Add(pageNumber, pageNumber);         // Akceptuji jen první
                    hasData = true;
                }
            }
        }
        /// <summary>
        /// Lists the available document orientation modes.
        /// </summary>
        public enum PageOrientation
        {
            /// <summary>
            /// The orientation is defined automatically to fit the page content to the specific paper type.
            /// </summary>
            Auto,
            /// <summary>
            /// Orientation of the document pages is portrait.
            /// </summary>
            Portrait,
            /// <summary>
            /// Orientation of the document pages is landscape.
            /// </summary>
            Landscape
        }
        /// <summary>
        /// Lists the available document scale modes.
        /// </summary>
        public enum PrintScaleMode
        {
            /// <summary>
            /// A printed page is scaled to fit a specific paper size.
            /// </summary>
            Fit,
            /// <summary>
            /// A printed page is not scaled.
            /// </summary>
            ActualSize,
            /// <summary>
            /// A printed page is scaled by a specified percentage scale factor.
            /// </summary>
            CustomScale
        }
    }
    #endregion
    #region Enumy: LabelStyleType, RectangleSide, RectangleCorner, ...
    /// <summary>
    /// Druh položky menu
    /// </summary>
    public enum MenuItemType
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None,
        /// <summary>
        /// Položka menu
        /// </summary>
        MenuItem,
        /// <summary>
        /// CheckBox - má zaškrtávátko
        /// </summary>
        CheckBox,
        /// <summary>
        /// DownButton - ve stavu Checked je vykreslen jako zamáčknutý
        /// </summary>
        DownButton,
        /// <summary>
        /// Záhlaví grupy, pak prvek má implementovat <see cref="IMenuHeaderItem"/>
        /// </summary>
        Header
    }
    /// <summary>
    /// Druh změny obsahu aktuálního prvku
    /// </summary>
    public enum ContentChangeMode
    {
        /// <summary>
        /// Nezadáno explicitně, použije se defaultní hodnota (typicky <see cref="Add"/>)
        /// </summary>
        None = 0,
        /// <summary>
        /// Přidat nový obsah ke stávajícímu obsahu, prvky se shodným ID aktualizovat, nic neodebírat
        /// </summary>
        Add,
        /// <summary>
        /// Znovu naplnit prvek: pokud prvek existuje, nejprve bude jeho obsah odstraněn, a poté bude vložen nově definovaný obsah.
        /// Pokud prvek neexistuje, bude vytvořen nový a prázdný.
        /// </summary>
        ReFill,
        /// <summary>
        /// Odstranit prvek: pokud existuje, bude zahozen jeho obsah i prvek samotný. Pokud neexistuje, nebude vytvářen.
        /// Pokud definice prvku má režim <see cref="Remove"/>, pak případný definovaný obsah prvku nebude použit.
        /// </summary>
        Remove
    }
    /// <summary>
    /// Způsob práce s prvky SubItems (Static / OnDemand)
    /// </summary>
    public enum MenuOnDemandLoadMode
    {
        /// <summary>
        /// Prvek obsahuje již ve své definici seznam reálných prvků, není třeba je donačítat On-Demand při aktivaci.
        /// Pokud prvek má toto nastavení a nemá definované položky, pak je prostě nemá a mít nebude.
        /// </summary>
        Static,
        /// <summary>
        /// Prvek typicky neobsahuje definici podřízených prvků při inicializaci, ale bude se donačítat ze serveru až při své aktivaci.
        /// Po jejich načtení bude seznam konstantní (jde o odložené načtení fixního seznamu).
        /// </summary>
        OnDemandLoadOnce,
        /// <summary>
        /// Prvek neobsahuje definici podřízených prvků při inicializaci, ale bude se při každé aktivaci submenu načítat ze serveru.
        /// Po jejich načtení bude seznam zobrazen, ale při další aktivaci stránky / prvku bude ze serveru načítán znovu.
        /// Jde o dynamický soupis prvků.
        /// </summary>
        OnDemandLoadEveryTime
    }
    /// <summary>
    /// Režim zobrazení prvku v menu
    /// </summary>
    public enum MenuItemDisplayMode
    {
        /// <summary>
        /// Výchozí = podle komponenty a situace
        /// </summary>
        Default,
        /// <summary>
        /// Pouze ikona
        /// </summary>
        Image,
        /// <summary>
        /// Pouze text bez ikony
        /// </summary>
        Text,
        /// <summary>
        /// Ikona plus text
        /// </summary>
        Both
    }
    /// <summary>
    /// Styl použitý pro Label
    /// </summary>
    public enum LabelStyleType
    {
        /// <summary>
        /// Běžný label u jednotlivých input prvků
        /// </summary>
        Default,
        /// <summary>
        /// Titulkový label menší, typicky grupa
        /// </summary>
        SubTitle,
        /// <summary>
        /// Titulkový label větší, jeden na formuláři
        /// </summary>
        MainTitle,
        /// <summary>
        /// Dodatková informace
        /// </summary>
        Info
    }
    /// <summary>
    /// Druh zarovnání obsahu v jedné ose (X, Y, číslená...)
    /// </summary>
    public enum AlignContentToSide
    {
        /// <summary>
        /// K začátku (Top, Left, 0)
        /// </summary>
        Begin,
        /// <summary>
        /// Na střed
        /// </summary>
        Center,
        /// <summary>
        /// Ke konci (Bottom, Right, nekonečno)
        /// </summary>
        End
    }
    /// <summary>
    /// Vyjádření názvu hrany na objektu Rectangle (Horní, Vpravo, Dolní, Vlevo).
    /// Enum povoluje sčítání hodnot, ale různé funkce nemusejí sečtené hodnoty akceptovat (z důvodu jejich logiky).
    /// </summary>
    [Flags]
    public enum RectangleSide
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo svislá na ose X</summary>
        Left = 0x01,
        /// <summary>Střed na ose X</summary>
        CenterX = 0x02,
        /// <summary>Vpravo svislá na ose X</summary>
        Right = 0x04,
        /// <summary>Horní vodorovná na ose Y</summary>
        Top = 0x10,
        /// <summary>Střed na ose Y</summary>
        CenterY = 0x20,
        /// <summary>Dolní vodorovná na ose Y</summary>
        Bottom = 0x40,
        /// <summary>Vodorovné = Top + Bottom</summary>
        Horizontal = Top | Bottom,
        /// <summary>Svislé = Left + Right</summary>
        Vertical = Left | Right,
        /// <summary>
        /// Prostřední bod
        /// </summary>
        Center = CenterX | CenterY,
        /// <summary>
        /// Horní levý bod
        /// </summary>
        TopLeft = Top | Left,
        /// <summary>
        /// Horní prostřední bod
        /// </summary>
        TopCenter = Top | CenterX,
        /// <summary>
        /// Horní pravý bod
        /// </summary>
        TopRight = Top | Right,
        /// <summary>
        /// Střední levý bod
        /// </summary>
        MiddleLeft = CenterY | Left,
        /// <summary>
        /// Úplně střední bod (X i Y)
        /// </summary>
        MiddleCenter = CenterY | CenterX,
        /// <summary>
        /// Střední pravý bod
        /// </summary>
        MiddleRight = CenterY | Right,
        /// <summary>
        /// Dolní levý bod
        /// </summary>
        BottomLeft = Bottom | Left,
        /// <summary>
        /// Dolní prostřední bod
        /// </summary>
        BottomCenter = Bottom | CenterX,
        /// <summary>
        /// Dolní pravý roh
        /// </summary>
        BottomRight = Bottom | Right,
        /// <summary>Všechny</summary>
        All = Left | Top | Right | Bottom
    }
    /// <summary>
    /// Umístění sady objektů (typicky tlačítka toolbaru) na okraji prostoru
    /// </summary>
    public enum ToolbarPosition
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Na horní straně, zarovnáno vlevo
        /// </summary>
        TopSideLeft,
        /// <summary>
        /// Na horní straně, zarovnáno uprostřed
        /// </summary>
        TopSideCenter,
        /// <summary>
        /// Na horní straně, zarovnáno doprava
        /// </summary>
        TopSideRight,
        /// <summary>
        /// Na pravé straně, zarovnáno nahoru
        /// </summary>
        RightSideTop,
        /// <summary>
        /// Na pravé straně, zarovnáno uprostřed
        /// </summary>
        RightSideCenter,
        /// <summary>
        /// Na pravé straně, zarovnáno dolů
        /// </summary>
        RightSideBottom,
        /// <summary>
        /// Na dolní straně, zarovnáno doprava
        /// </summary>
        BottomSideRight,
        /// <summary>
        /// Na dolní straně, zarovnáno uprostřed
        /// </summary>
        BottomSideCenter,
        /// <summary>
        /// Na dolní straně, zarovnáno doleva
        /// </summary>
        BottomSideLeft,
        /// <summary>
        /// Na levé straně, zarovnáno dolů
        /// </summary>
        LeftSideBottom,
        /// <summary>
        /// Na levé straně, zarovnáno uprostřed
        /// </summary>
        LeftSideCenter,
        /// <summary>
        /// Na levé straně, zarovnáno nahoru
        /// </summary>
        LeftSideTop
    }
    /// <summary>
    /// Je povoleno vytvářet ikonu pro prvek, který nemá Image ale jen caption, z výchozích písmen textu?
    /// </summary>
    public enum ImageFromCaptionType
    {
        /// <summary>
        /// Nikdy neprovádět
        /// </summary>
        Disabled,
        /// <summary>
        /// Pouze pro prvky v Root úrovni
        /// </summary>
        OnlyForRootMenuLevel,
        /// <summary>
        /// Pro všechny prvky povoleno
        /// </summary>
        Enabled
    }
    /// <summary>
    /// Vyjádření názvu rohu na objektu Rectangle (Vlevo nahoře, Vpravo nahoře, ...)
    /// </summary>
    public enum RectangleCorner
    {
        /// <summary>Neurčeno</summary>
        None = 0,
        /// <summary>Vlevo nahoře</summary>
        LeftTop,
        /// <summary>Vpravo nahoře</summary>
        TopRight,
        /// <summary>Vpravo dole</summary>
        RightBottom,
        /// <summary>Vlevo dole</summary>
        BottomLeft
    }
    /// <summary>
    /// Jaké hodnoty zachovat při změně obsahu dat
    /// </summary>
    [Flags]
    public enum PreservePropertiesMode
    {
        /// <summary>
        /// Nic nezachovat, vše bude resetováno do výchozího stavu
        /// </summary>
        None = 0,
        /// <summary>
        /// Zachovat první viditelný prvek (řádek, sloupec)
        /// </summary>
        FirstVisibleItem = 0x0001,
        /// <summary>
        /// Zachovat první viditelný pixel
        /// </summary>
        FirstVisiblePixel = 0x0002,
        /// <summary>
        /// Zachovat stav vybraných prvků
        /// </summary>
        SelectedItems = 0x0010
    }
    /// <summary>
    /// Viditelnost některého prvku v rámci parenta s ohledem na interaktivitu
    /// </summary>
    [Flags]
    public enum DxChildControlVisibility
    {
        /// <summary>
        /// Prvek není vidět nikdy
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek je vidět tehdy, když parent má na sobě myš
        /// </summary>
        OnMouse = 0x0001,
        /// <summary>
        /// Prvek je vidět tehdy, když parent má v sobě klávesový focus (kurzor)
        /// </summary>
        OnFocus = 0x0002,
        /// <summary>
        /// Prvek je vidět vždy
        /// </summary>
        Allways = 0x0004,

        /// <summary>
        /// Prvek je vidět pod myší anebo s focusem
        /// </summary>
        OnActiveControl = OnMouse | OnFocus
    }
    /// <summary>
    /// Klávesové a jiné akce, mohou sloužit i jako označení typu buttonu
    /// </summary>
    [Flags]
    public enum ControlKeyActionType : long
    {
        /// <summary>
        /// Žádná akce
        /// </summary>
        None = 0,

        /// <summary>
        /// Klávesa CtrlC: zkopírovat
        /// </summary>
        ClipCopy = 0x0001,
        /// <summary>
        /// Klávesa CtrlX: vyjmout
        /// </summary>
        ClipCut = 0x0002,
        /// <summary>
        /// Klávesa CtrlV: vložit
        /// </summary>
        ClipPaste = 0x0004,
        /// <summary>
        /// Klávesa Delete: Smazat vybrané
        /// </summary>
        Delete = 0x0008,

        /// <summary>
        /// Klávesa CtrlR: Refresh
        /// </summary>
        Refresh = 0x0010,
        /// <summary>
        /// Klávesa CtrlA: vybrat vše
        /// </summary>
        SelectAll = 0x0020,
        /// <summary>
        /// Klávesa Ctrl Home: přejdi na začátek
        /// </summary>
        GoBegin = 0x0040,
        /// <summary>
        /// Klávesa Ctrl End: přejdi na konec
        /// </summary>
        GoEnd = 0x0080,

        /// <summary>
        /// Klávesa CtrlAltUp (kurzor): přemístit na první pozici
        /// </summary>
        MoveTop = 0x0100,
        /// <summary>
        /// Klávesa AltUp (kurzor): přemístit o jednu pozici nahoru
        /// </summary>
        MoveUp = 0x0200,
        /// <summary>
        /// Klávesa AltDown (kurzor): přemístit o jednu pozici dolů
        /// </summary>
        MoveDown = 0x0400,
        /// <summary>
        /// Klávesa CtrlAltDown (kurzor): přemístit na poslední pozici
        /// </summary>
        MoveBottom = 0x0800,

        /// <summary>
        /// Klávesa CtrlZ: Undo (odvolat poslední změnu)
        /// </summary>
        Undo = 0x1000,
        /// <summary>
        /// Klávesa CtrlY: Redo (znovu provést poslední změnu)
        /// </summary>
        Redo = 0x2000,
        /// <summary>
        /// Klávesa Šipka nahoru nebo PageUp na první pozici Listu: aktivuj řádkový filtr
        /// </summary>
        ActivateFilter = 0x4000,
        /// <summary>
        /// Jiné klávesy (znaky): aktivuj řádkový filtr a vlož do něj dodaný znak
        /// </summary>
        FillKeyToFilter = 0x8000,

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
        /// Všechny práce s clipboardem
        /// </summary>
        ClipboardAll = ClipCopy | ClipCut | ClipPaste,
        /// <summary>
        /// Všechny pohyby kurzoru
        /// </summary>
        GoAll = GoBegin | GoEnd,
        /// <summary>
        /// Všechny přesuny nahoru/dolů
        /// </summary>
        MoveAll = MoveTop | MoveUp | MoveDown | MoveBottom,
        /// <summary>
        /// Všechny kopie doleva/doprava
        /// </summary>
        CopyAll = CopyToRightOne | CopyToRightAll | CopyToLeftOne | CopyToLeftAll,
        /// <summary>
        /// Undo a Redo
        /// </summary>
        UndoRedoAll = Undo | Redo,

        /// <summary>
        /// Všechny akce
        /// </summary>
        All = ClipboardAll | Delete | SelectAll | GoAll | MoveAll | UndoRedoAll | ActivateFilter | ActivateFilter
    }
    /// <summary>
    /// Stav aktivity okna
    /// </summary>
    public enum WindowActivityState
    {
        /// <summary>
        /// Výchozí stav, v němž se okno nikdy nenachází
        /// </summary>
        None,
        /// <summary>
        /// Stav počínaje konstruktorem, stav končí prvním zobrazením
        /// </summary>
        Creating,
        /// <summary>
        /// Právě bylo zahájeno první zobrazení okna
        /// </summary>
        FirstShow,
        /// <summary>
        /// Právě bylo zahájeno zobrazení okna (jak první, tak následující), volá se před fyzickým Show
        /// </summary>
        ShowBefore,
        /// <summary>
        /// Právě bylo dokončeno zobrazení okna
        /// </summary>
        ShowAfter,
        /// <summary>
        /// Okno již bylo zobrazeno a je viditelné, nyní může být aktivováno, nebo skryto nebo zavřeno
        /// </summary>
        Visible,
        /// <summary>
        /// Okno je viditelné a aktivní (má Focus)
        /// </summary>
        Active,
        /// <summary>
        /// Okno je viditelné, ale nemá focus
        /// </summary>
        Inactive,
        /// <summary>
        /// Okno je skryto
        /// </summary>
        Invisible,
        /// <summary>
        /// Bylo zahájeno zavírání okna
        /// </summary>
        Closing,
        /// <summary>
        /// Okno je zavřeno, ale dosud neprošlo Dispose
        /// </summary>
        Closed,
        /// <summary>
        /// Začal proces Dispose
        /// </summary>
        Disposing,
        /// <summary>
        /// Ukončen proces Dispose
        /// </summary>
        Disposed
    }
    /// <summary>
    /// Stav objektu z hlediska jeho základní situace (Enabled / Disabled / ReadOnly).
    /// Hodnotově přesně odpovídá typu <see cref="DxInteractiveState"/> v základním stavu prvku (neaktivní, bez myší, bez focusu).
    /// </summary>
    [Flags]
    public enum DxItemState : int
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek je vidět (Visible = true). Bez této hodnoty je prvek neviditelný a nezobrazuje se.
        /// </summary>
        Visible = 0x01,
        /// <summary>
        /// Prvek je Disabled: nemůže získat Focus, nemůže měnit hodnotu, nereaguje na kliknutí.
        /// </summary>
        Disabled = 0x02,
        /// <summary>
        /// Prvek je ReadOnly: může získat Focus, ale nemůže měnit svoji hodnotu. Může být na něj kliknuto i na jeho SubButtony a provedou akci. Může rozbalit Combo nabídku. Nemůže ale změnit hodnotu.
        /// </summary>
        ReadOnly = 0x04,
        /// <summary>
        /// Enabled: může získat Focus, může měnit svoji hodnotu. Může být na něj kliknuto i na jeho SubButtony.
        /// </summary>
        Enabled = 0x08,
        /// <summary>
        /// TabStop: může získat prostým procházením ze sousedního prvku (Tab, Enter, Šipka). Pokud nemá tento příznak, pak může získat focus kliknutím myši, ale nikoli klávesou.
        /// </summary>
        TabStop = 0x10,

        /// <summary>
        /// Prvek je Aktivní = nemusí mít Focus, ale mezi ostatními prvky je Aktivní (např. pokud Button je Down nebo Selected)
        /// </summary>
        Active = 0x40,
    }
    /// <summary>
    /// Stav objektu z hlediska myši a focusu
    /// </summary>
    [Flags]
    public enum DxInteractiveState : int
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek je vidět (Visible = true). Bez této hodnoty je prvek neviditelný a nezobrazuje se.
        /// </summary>
        Visible = 0x01,
        /// <summary>
        /// Prvek je Disabled: nemůže získat Focus, nemůže měnit hodnotu, nereaguje na kliknutí.
        /// </summary>
        Disabled = 0x02,
        /// <summary>
        /// Prvek je ReadOnly: může získat Focus, ale nemůže měnit svoji hodnotu. Může být na něj kliknuto i na jeho SubButtony a provedou akci. Může rozbalit Combo nabídku. Nemůže ale změnit hodnotu.
        /// </summary>
        ReadOnly = 0x04,
        /// <summary>
        /// Enabled: může získat Focus, může měnit svoji hodnotu. Může být na něj kliknuto i na jeho SubButtony.
        /// </summary>
        Enabled = 0x08,
        /// <summary>
        /// TabStop: může získat prostým procházením ze sousedního prvku (Tab, Enter, Šipka). Pokud nemá tento příznak, pak může získat focus kliknutím myši, ale nikoli klávesou.
        /// </summary>
        TabStop = 0x10,

        /// <summary>
        /// Prvek je Aktivní = nemusí mít Focus, ale mezi ostatními prvky je Aktivní (např. pokud Button je Down nebo Selected)
        /// </summary>
        Active = 0x40,

        /// <summary>
        /// Myš vstoupila nad prvek = prvek je Hot
        /// </summary>
        HasMouse = 0x100,
        /// <summary>
        /// Levá myš je stisknuta
        /// </summary>
        MouseLeftDown = 0x200,
        /// <summary>
        /// Pravá myš je stisknuta
        /// </summary>
        MouseRightDown = 0x400,
        /// <summary>
        /// Prvek je přesouván myší
        /// </summary>
        MouseDragging = 0x800,

        /// <summary>
        /// Kurzor je v prvku, prvek má klávesový Focus
        /// </summary>
        HasFocus = 0x1000,

        /// <summary>
        /// Maska základního stavu prvku
        /// </summary>
        MaskItemState = Visible | Disabled | ReadOnly | Enabled | TabStop | Active,
        /// <summary>
        /// Maska myší aktivity
        /// </summary>
        MaskMouse = HasMouse | MouseLeftDown | MouseRightDown | MouseDragging,
        /// <summary>
        /// Maska klávesnicové aktivity
        /// </summary>
        MaskKeyboard = HasFocus,
        /// <summary>
        /// Maska jakékoli interaktivity
        /// </summary>
        MaskInteractive = MaskMouse | MaskKeyboard,

        /// <summary>
        /// Maska stavů, kdy prvek má mít připraven nativní control - protože se očekává jeho nativní interaktivita
        /// </summary>
        MaskUseNativeControl = HasMouse | MouseLeftDown | MouseRightDown | MouseDragging | HasFocus
    }
    #endregion
    #region enum DxCursorType
    /// <summary>
    /// Druh kurzoru
    /// </summary>
    public enum DxCursorType
    {
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the default cursor.
        /// </summary>
        Default = 0,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears when an application starts.
        /// </summary>
        AppStarting,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically downward and to the left.
        /// </summary>
        PanSW,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling vertically in a downward direction.
        /// </summary>
        PanSouth,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically downward and to the right.
        /// </summary>
        PanSE,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically upward and to the left.
        /// </summary>
        PanNW,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling vertically in an upward direction.
        /// </summary>
        PanNorth,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally and vertically upward and to the right.
        /// </summary>
        PanNE,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally to the right.
        /// </summary>
        PanEast,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is not moving.
        /// </summary>
        NoMoveVert,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is not moving.
        /// </summary>
        NoMoveHoriz,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is not moving.
        /// </summary>
        NoMove2D,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears when the mouse is positioned over a vertical splitter bar.
        /// </summary>
        VSplit,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears when the mouse is positioned over a horizontal splitter bar.
        /// </summary>
        HSplit,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the Help cursor.
        /// </summary>
        Help,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the wait cursor.
        /// </summary>
        WaitCursor,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the up arrow cursor.
        /// </summary>
        UpArrow,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the two-headed horizontal (west/east) sizing cursor.
        /// </summary>
        SizeWE,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the two-headed diagonal (northwest/southeast) sizing cursor.
        /// </summary>
        SizeNWSE,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the two-headed vertical (north/south) sizing cursor.
        /// </summary>
        SizeNS,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents two-headed diagonal (northeast/southwest) sizing cursor.
        /// </summary>
        SizeNESW,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the four-headed sizing cursor.
        /// </summary>
        SizeAll,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that indicates that a particular region is invalid for the current operation.
        /// </summary>
        No,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the I-beam cursor.
        /// </summary>
        IBeam,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the crosshair cursor.
        /// </summary>
        Cross,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the arrow cursor.
        /// </summary>
        Arrow,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the cursor that appears during wheel operations when the mouse is moving and the window is scrolling horizontally to the left.
        /// </summary>
        PanWest,
        /// <summary>
        /// The System.Windows.Forms.Cursor that represents the hand cursor.
        /// </summary>
        Hand
    }
    #endregion
}
