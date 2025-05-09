﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors.Filtering.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Formulář pro měření spotřeby GDI handles
    /// </summary>
    ///   [RunFormInfo(groupText: "Tools", groupOrder: 100, buttonText: "Handles", buttonOrder: 210, buttonImage: "svgimages/dashboards/scatterchart.svg", buttonToolTip: "Otevře okno sledování GUI Handles")]
    public class HandleScanForm : DxControlForm
    {
        public HandleScanForm()
        {
            this.InitSplitContainer();
            this.InitProcessList();
            this.InitMemoryScan();
            DxComponent.LogClear();
        }
        //string resource3 = "devav/actions/printpreview.svg";
        //string resource4 = "svgimages/dashboards/scatterchart.svg";

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            string log = DxComponent.LogText;
        }
        protected override void Dispose(bool disposing)
        {
            this.DoneMemoryScan();
            base.Dispose(disposing);
        }
        protected override void InitializeForm()
        {
            this.Text = "Měření počtu SystemHandles u běžících procesů";
            base.InitializeForm();
        }
        protected override void OnFirstShownAfter()
        {
            base.OnFirstShownAfter();
            this._RunProcessRefresh();
        }
        private void InitSplitContainer()
        {
            _SplitContainer = new DxSplitContainerControl()
            {
                SplitterOrientation = System.Windows.Forms.Orientation.Vertical,
                SplitterPosition = 300,
                FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1,
                IsSplitterFixed = false,
                Dock = System.Windows.Forms.DockStyle.Fill,
            };
            this.ControlPanel.Controls.Add(_SplitContainer);
        }
        private DxSplitContainerControl _SplitContainer;

        #region Seznam procesů

        private void InitProcessList()
        {
            _ProcessListBox = new DxListBoxPanel()
            {
                ButtonsPosition = ToolbarPosition.BottomSideRight,
                ButtonsTypes = ControlKeyActionType.Refresh,
                ButtonsSize = ResourceImageSizeType.Medium,
                RowFilterMode = DxListBoxPanel.FilterRowMode.Server,
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            _ProcessListBox.ListActionAfter += _ProcessListBox_ActionAfter;
            _ProcessListBox.RowFilterServerKeyEnter += _ProcessListBox_FilterBoxKeyEnter;
            _ProcessListBox.SelectedItemsChanged += _ProcessListBox_SelectedMenuItemChanged;
            _SplitContainer.Panel1.Controls.Add(_ProcessListBox);
        }

        private void _ProcessListBox_SelectedMenuItemChanged(object sender, EventArgs e)
        {
            RunMemoryScanAsync();
        }

        private void _ProcessListBox_ActionAfter(object sender, DxListBoxActionEventArgs args)
        {
            if (args.Action == ControlKeyActionType.Refresh)
                _RunProcessRefresh();
        }

        private void _ProcessListBox_FilterBoxKeyEnter(object sender, EventArgs e)
        {
        }

        private void _RunProcessRefresh()
        {
            var currentSelectedProcess = this.SelectedProcess;
            var currentProcessInfos = ProcessInfo.SearchProcesses(out var currentProcess);         // Nové instance informací o aktuálních procesech
            var processInfos = ProcessInfo.MergeProcesses(_ProcessInfos, currentProcessInfos);     // Sloučit s dosavadními informacemi: dosavadní informace nebudu zahazovat!
            var selectId = (currentSelectedProcess ?? currentProcess)?.ProcessId ?? 0;
            var newSelectedProcess = processInfos.FirstOrDefault(p => p.ProcessId == selectId);    // Nový SelectedMenuItem musí být instance z processInfos
            _ProcessListBox.ListItems = processInfos;
            _ProcessListBox.SelectedListItem = newSelectedProcess;
            _ProcessInfos = processInfos;
        }

        private ProcessInfo SelectedProcess { get { return _ProcessListBox.SelectedListItem as ProcessInfo; } set { _ProcessListBox.SelectedListItem = value; } }
        private ProcessInfo[] _ProcessInfos;
        private DxListBoxPanel _ProcessListBox;
        #endregion
        #region Scan paměti procesu, ukládání a vyvolání Paint
        private void InitMemoryScan()
        {
            _ScanLock = new object();
            _CallMeTimerGuid = Noris.Clients.Win.Components.WatchTimer.CallMeEvery(RunMemoryScanSilent, 1000);
        }
        /// <summary>
        /// Zastaví timer, který spouští <see cref="RunMemoryScan(bool)"/> (jeho Guid je v <see cref="_CallMeTimerGuid"/>).
        /// </summary>
        private void DoneMemoryScan()
        {
            if (_CallMeTimerGuid.HasValue)
                Noris.Clients.Win.Components.WatchTimer.Remove(_CallMeTimerGuid.Value);
            _CallMeTimerGuid = null;
        }
        /// <summary>
        /// Provede jednu akci pro scanování paměti pro aktuální proces <see cref="SelectedProcess"/>.
        /// Volá se po změně vybraného procesu, z GUI threadu, je třeba provést asynchronně
        /// </summary>
        private void RunMemoryScanAsync()
        {
            ThreadManager.AddAction(() => RunMemoryScan(true));
        }
        /// <summary>
        /// Provede jednu akci pro scanování paměti pro aktuální proces <see cref="SelectedProcess"/>.
        /// Volá se občas, z threadu na pozadí.
        /// </summary>
        private void RunMemoryScanSilent()
        {
            RunMemoryScan(false);
        }
        /// <summary>
        /// Provede jednu akci pro scanování paměti pro aktuální proces <see cref="SelectedProcess"/>.
        /// Volá se občas, z threadu na pozadí. Anebo na popředí po změně procesu.
        /// </summary>
        private void RunMemoryScan(bool forcePaint)
        {
            if (_ScanRunning) return;            // Lehký test bez locku

            ProcessInfo process = SelectedProcess;
            bool hasChanges = false;
            lock (_ScanLock)
            {
                if (!_ScanRunning)               // Zodpovědný test včetně locku
                {
                    try
                    {
                        _ScanRunning = true;
                        if (process != null)
                        {
                            process.RunMemoryScan(out hasChanges);
                        }
                    }
                    finally
                    {
                        _ScanRunning = false;
                    }
                }
            }
            if (hasChanges || forcePaint)
                RunPaintProcessInfo(process);
        }

        private void RunPaintProcessInfo(ProcessInfo process)
        {
            
        }

        /// <summary>
        /// Guid timeru, který volá <see cref="RunMemoryScan(bool)"/>. 
        /// Při Dispose je nutno jej zastavit.
        /// </summary>
        private Guid? _CallMeTimerGuid;
        /// <summary>
        /// Příznak běžícího procesu <see cref="RunMemoryScan(bool)"/>
        /// </summary>
        private bool _ScanRunning;
        /// <summary>
        /// Lock pro proces <see cref="RunMemoryScan(bool)"/>
        /// </summary>
        private Object _ScanLock;
        #endregion
    }
    #region class ProcessInfo
    /// <summary>
    /// Data o jednom procesu. Tuto třídu není nutno Disposovat, nedrží si handles procesu, ale jen ID procesu.
    /// </summary>
    internal class ProcessInfo : IMenuItem
    {
        #region Získání seznamu, sloučení dvou seznamů (dosavadní a nový)
        /// <summary>
        /// Najde a vrátí informace o aktuálně běžících procesech.
        /// </summary>
        /// <returns></returns>
        public static ProcessInfo[] SearchProcesses()
        {
            return SearchProcesses(out var _);
        }
        /// <summary>
        /// Najde a vrátí informace o aktuálně běžících procesech.
        /// Do out parametru vloží info o procesu aktuálním.
        /// </summary>
        /// <param name="currentProcessInfo"></param>
        /// <returns></returns>
        public static ProcessInfo[] SearchProcesses(out ProcessInfo currentProcessInfo)
        {
            var processInfos = new List<ProcessInfo>();
            var processes = System.Diagnostics.Process.GetProcesses();
            var currentProcessId = DxComponent.WinProcessInfo.CurrentProcessId;
            currentProcessInfo = null;
            foreach (var process in processes)
            {
                try
                {
                    if (ProcessInfo.AcceptProcess(process))
                    {
                        bool isCurrentProcess = (process.Id == currentProcessId);
                        ProcessInfo processInfo = new ProcessInfo(process, isCurrentProcess);
                        processInfos.Add(processInfo);
                        if (isCurrentProcess) currentProcessInfo = processInfo;
                    }
                }
                catch { }

                // Dispose vždy:
                try { process.Dispose(); } catch { }
            }

            processInfos.Sort(ProcessInfo.CompareByText);
            return processInfos.ToArray();
        }
        /// <summary>
        /// Vrátí true, pokud daný proces se má zařadit do pole sledovaných procesů.
        /// Některé systémové procesy, a některé známé obludy vyřazujeme.
        /// Vyřazujeme i procesy bez okna = sledujeme hlavně GDI, a tyto procesy je nemají.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        internal static bool AcceptProcess(System.Diagnostics.Process process)
        {
            try
            {
                string processName = process.ProcessName.Trim().ToLower();
                if (processName == "firefox" || processName == "svchost" || processName == "explorer" || processName == "teams" || processName == "runtimebroker") return false;

                var handle = process.MainWindowHandle;
                if (handle.ToInt64() == 0L) return false;

                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Vrátí sloučené pole informací o procesech
        /// </summary>
        /// <param name="processInfos"></param>
        /// <param name="newProcessInfos"></param>
        /// <returns></returns>
        internal static ProcessInfo[] MergeProcesses(ProcessInfo[] processInfos, ProcessInfo[] newProcessInfos)
        {
            // Dosavadní procesy zaeviduji do výstupní Dictionary přednostně, mohou obsahovat celou historii informací:
            var summaryDict = (processInfos != null ? processInfos.CreateDictionary(p => p.ProcessId, true) : new Dictionary<int, ProcessInfo>());
            // Dosavadním procesům nastavím IsAlive = false; uvidíme v dalším kroku, zda jsou živé:
            summaryDict.Values.ForEachExec(p => p.IsAlive = false);
            // Do výstupní Dictionary přidám / aktualizuje nově nalezené procesy:
            if (newProcessInfos != null)
            {
                foreach (var newProcessInfo in newProcessInfos)
                {
                    if (!summaryDict.TryGetValue(newProcessInfo.ProcessId, out var oldProcessInfo))
                    {
                        newProcessInfo.IsAlive = true;
                        summaryDict.Add(newProcessInfo.ProcessId, newProcessInfo);
                    }
                    else
                    {
                        oldProcessInfo.IsAlive = true;
                    }
                }
            }
            // Vytvoříme výsledný soupis, setřídíme podle Textu:
            var summaryList = summaryDict.Values.ToList();
            summaryList.Sort(CompareByText);
            return summaryList.ToArray();
        }
        #endregion
        #region Konstruktor a základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="process"></param>
        /// <param name="isCurrentProcess"></param>
        public ProcessInfo(System.Diagnostics.Process process, bool isCurrentProcess)
        {
            ProcessId = process.Id;
            ProcessName = process.ProcessName;
            try { WindowTitle = process.MainWindowTitle; } catch { }
            IsCurrentProcess = isCurrentProcess;
        }
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// ID procesu
        /// </summary>
        public readonly int ProcessId;
        public readonly string ProcessName;
        public readonly string WindowTitle;
        public readonly bool IsCurrentProcess;
        public string Text { get { return (!String.IsNullOrEmpty(WindowTitle) ? WindowTitle : $"({ProcessName})"); } }
        public float? ItemFontSizeRatio { get { return null; } }
        public FontStyle? ItemFontStyle
        {
            get
            {
                FontStyle? result = null;
                if (this.IsCurrentProcess) result = FontStyle.Bold;
                else if (!this.IsAlive) result = FontStyle.Italic;
                return result;
            }
        }
        public string ToolTip { get; set; }
        /// <summary>
        /// Obsahuje true, pokud je proces živý. Je určeno při každém procesu <see cref="MergeProcesses(ProcessInfo[], ProcessInfo[])"/>.
        /// </summary>
        public bool IsAlive { get; private set; }
        /// <summary>
        /// Informace o procesu. Může být null, když už je po něm. 
        /// Používat v using() patternu!
        /// </summary>
        private System.Diagnostics.Process GetProcess()
        {
            try { return System.Diagnostics.Process.GetProcessById(ProcessId); }
            catch { return null; }
        }
        public static int CompareByText(ProcessInfo a, ProcessInfo b)
        {
            return String.Compare(a.Text, b.Text);
        }
        #endregion
        #region MemoryScan
        /// <summary>
        /// Načte informace o this procesu a uloží si je do paměti, pokud jsou jiné než poslední.
        /// </summary>
        internal void RunMemoryScan()
        {
            RunMemoryScan(out var _);
        }
        /// <summary>
        /// Načte informace o this procesu a uloží si je do paměti, pokud jsou jiné než poslední.
        /// Tento příznak i vrací.
        /// </summary>
        /// <param name="hasChange"></param>
        internal void RunMemoryScan(out bool hasChange)
        {
            using (var process = GetProcess())
            {
                var info = Noris.Clients.Win.Components.AsolDX.DxComponent.WinProcessInfo.GetInfoForProcess(process);
                this.AddInfo(info, out hasChange);
            }
        }
        private void AddInfo(DxComponent.WinProcessInfo info, out bool hasChange)
        {
            hasChange = false;
            var lastInfo = (_MemoryInfoList != null && _MemoryInfoList.Count > 0 ? _MemoryInfoList[_MemoryInfoList.Count - 1] : null);
            if (lastInfo != null && DxComponent.WinProcessInfo.EqualsContent(lastInfo, info)) return;             // Nebudu si množit stejná data
            hasChange = true;
            MemoryInfoList.Add(info);
        }
        public IList<Noris.Clients.Win.Components.AsolDX.DxComponent.WinProcessInfo> MemoryInfos { get { return MemoryInfoList; } }
        private List<Noris.Clients.Win.Components.AsolDX.DxComponent.WinProcessInfo> MemoryInfoList { get { if (_MemoryInfoList is null) _MemoryInfoList = new List<DxComponent.WinProcessInfo>(); return _MemoryInfoList; } }
        private List<Noris.Clients.Win.Components.AsolDX.DxComponent.WinProcessInfo> _MemoryInfoList;
        #endregion
        #region IMenuItem - instance této třídy se může prezentovat vizuálně pomocí tohoto interface
        IMenuItem IMenuItem.ParentItem { get { return null; } set { } }
        MenuItemType IMenuItem.ItemType { get { return MenuItemType.MenuItem; } }
        ContentChangeMode IMenuItem.ChangeMode { get { return ContentChangeMode.Add; } }
        Keys? IMenuItem.HotKeys { get { return null; } }
        Shortcut? IMenuItem.Shortcut { get { return null; } }
        string IMenuItem.HotKey { get { return null; } }
        string IMenuItem.ShortcutText { get { return null; } }
        IEnumerable<IMenuItem> IMenuItem.SubItems { get { return null; } }
        bool IMenuItem.SubItemsIsOnDemand { get { return false; } }
        string ITextItem.ItemId { get { return ProcessId.ToString(); } }
        string ITextItem.Text { get { return Text; } }
        int ITextItem.ItemOrder { get; set; }
        bool ITextItem.ItemIsFirstInGroup { get { return false; } }
        bool ITextItem.Visible { get { return true; } }
        bool ITextItem.Enabled { get { return this.IsAlive; } }
        bool? ITextItem.Checked { get; set; }
        string ITextStyleItem.StyleName { get { return null; } }
        float? ITextStyleItem.FontSizeRatio { get { return ItemFontSizeRatio; } }
        FontStyle? ITextStyleItem.FontStyle { get { return ItemFontStyle; } }
        float? ITextStyleItem.FontSizeRelativeToZoom { get { return null; } }
        float? ITextStyleItem.FontSizeRelativeToDesign { get { return null; } }
        Color? ITextStyleItem.BackColor { get { return null; } }
        Color? ITextStyleItem.ForeColor { get { return null; } }
        Image ITextItem.Image { get { return null; } }
        SvgImage ITextItem.SvgImage { get { return null; } }
        string ITextItem.ImageName { get { return null; } }
        string ITextItem.ImageNameUnChecked { get { return null; } }
        string ITextItem.ImageNameChecked { get { return null; } }
        ImageFromCaptionType ITextItem.ImageFromCaptionMode { get { return ImageFromCaptionType.Disabled; } }
        BarItemPaintStyle ITextItem.ItemPaintStyle { get { return BarItemPaintStyle.CaptionGlyph; } }
        object ITextItem.Tag { get { return null; } }
        string IToolTipItem.ToolTipTitle { get { return Text; } }
        string IToolTipItem.ToolTipIcon { get { return null; } }
        string IToolTipItem.ToolTipText { get { return ToolTip; } }
        bool? IToolTipItem.ToolTipAllowHtml { get { return null; } }
        Action<IMenuItem> IMenuItem.ClickAction { get { return null; } }
        Action<IMenuItem> IMenuItem.CheckAction { get { return null; } }
        #endregion
    }
    #endregion
}
