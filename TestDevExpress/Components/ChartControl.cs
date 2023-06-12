using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using SIO = System.IO;

using WF = System.Windows.Forms;
using DC = DevExpress.XtraCharts;
using DB = DevExpress.XtraBars;
using DE = DevExpress.XtraEditors;
using DM = DevExpress.Utils.Menu;

using TestDevExpress.Components;


namespace Noris.Clients.Win.Components
{
    /// <summary>
    /// Control zobrazující graf
    /// </summary>
    public class ChartPanel : DE.PanelControl
    {
        #region Konstruktor, proměnné, obsluha minitoolbaru
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ChartPanel()
        {
            InitializeChart();
        }
        private void InitializeChart()
        {
            CreateBarManager();
            CreateChart();
            CreateTools();
            this.Dock = WF.DockStyle.Fill;
        }
        private DB.BarManager _BarMgr;
        private DC.ChartControl _Chart;
        private DE.PanelControl _Tools;
        private DE.ComboBoxEdit _SettingsCombo;
        private DM.DXPopupMenu _EditDxMenu;
        private DE.DropDownButton _EditButton;
        private void CreateBarManager()
        {
            _BarMgr = new DB.BarManager();
            _BarMgr.ForceInitialize();
        }
        private void CreateChart()
        {
            _Chart = new DC.ChartControl();
            _Chart.Dock = WF.DockStyle.Fill;
            _Chart.DoubleClick += Chart_DoubleClick;
            _Chart.HardwareAcceleration = true;
            _Chart.CacheToMemory = true;
            this.Controls.Add(_Chart);

            _ChartSettings = new List<ChartSetting>();
        }
        private void CreateTools()
        {
            _Tools = new DE.PanelControl() { Bounds = new System.Drawing.Rectangle(250, 6, 320, 28), Dock = WF.DockStyle.None, BorderStyle = DE.Controls.BorderStyles.NoBorder };
            _Tools.BackColor = System.Drawing.Color.Transparent;
            _ChartToolsAlignment = System.Drawing.ContentAlignment.TopRight;

            _SettingsCombo = new DE.ComboBoxEdit() { Bounds = new System.Drawing.Rectangle(0, 0, 260, 28), BorderStyle = DE.Controls.BorderStyles.UltraFlat };
            _SettingsCombo.Properties.TextEditStyle = DE.Controls.TextEditStyles.DisableTextEditor;
            _SettingsCombo.Properties.ButtonsStyle = DE.Controls.BorderStyles.UltraFlat;
            _SettingsCombo.Properties.UseReadOnlyAppearance = true;
            _SettingsCombo.Properties.Appearance.FontSizeDelta = 3;
            _SettingsCombo.Properties.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
            _SettingsCombo.Properties.AppearanceDropDown.FontSizeDelta = 3;

            _SettingsCombo.SelectedValueChanged += _SettingsValueChanged;
            _Tools.Controls.Add(_SettingsCombo);

            _EditButton = new DE.DropDownButton() { Bounds = new System.Drawing.Rectangle(262, 0, 30, 28), DropDownArrowStyle = DE.DropDownArrowStyle.Hide, PaintStyle = DE.Controls.PaintStyles.Light };
            _EditButton.ImageOptions.Image = AsolDX.DxComponent.GetBitmapImage("images/richedit/documentproperties_16x16.png", AsolDX.ResourceImageSizeType.Small);

            // _EditButton.ImageOptions.ImageToTextAlignment = DE.ImageAlignToText.BottomCenter;
            _EditButton.ImageOptions.ImageToTextAlignment = DE.ImageAlignToText.LeftCenter;

            MenuDeclare();

            _EditButton.DropDownControl = _EditDxMenu;

            _Tools.Controls.Add(_EditButton);

            _Chart.Controls.Add(_Tools);
            _Chart.SizeChanged += _Chart_SizeChanged;
            _SetToolsPositions();
        }
        private void _Chart_SizeChanged(object sender, EventArgs e)
        {
            _SetToolsPositions();
        }
        private void _SetToolsPositions()
        {
            _EditButton.Bounds = new Rectangle(_SettingsCombo.Right + 1, _SettingsCombo.Top, _EditButton.Width, _SettingsCombo.Height);
            Size toolSize = new Size(_EditButton.Right + 1, _SettingsCombo.Height + 1);
            Size parentSize = _Chart.ClientSize;
            WF.Padding margins = new WF.Padding(12, 6, 18, 4);
            Point toolLocation = Point.Empty;
            switch (this._ChartToolsAlignment)
            {
                case ContentAlignment.TopLeft:
                    toolLocation = new Point(margins.Left, margins.Top);
                    break;
                case ContentAlignment.TopCenter:
                    toolLocation = new Point((parentSize.Width - toolSize.Width) / 2, margins.Top);
                    break;
                case ContentAlignment.TopRight:
                    toolLocation = new Point((parentSize.Width - toolSize.Width - margins.Right), margins.Top);
                    break;
                case ContentAlignment.MiddleLeft:
                    toolLocation = new Point(margins.Left, (parentSize.Height - toolSize.Height) / 2);
                    break;
                case ContentAlignment.MiddleCenter:
                    toolLocation = new Point((parentSize.Width - toolSize.Width) / 2, (parentSize.Height - toolSize.Height) / 2);
                    break;
                case ContentAlignment.MiddleRight:
                    toolLocation = new Point((parentSize.Width - toolSize.Width - margins.Right), (parentSize.Height - toolSize.Height) / 2);
                    break;
                case ContentAlignment.BottomLeft:
                    toolLocation = new Point(margins.Left, (parentSize.Height - toolSize.Height - margins.Bottom));
                    break;
                case ContentAlignment.BottomCenter:
                    toolLocation = new Point((parentSize.Width - toolSize.Width) / 2, (parentSize.Height - toolSize.Height - margins.Bottom));
                    break;
                case ContentAlignment.BottomRight:
                    toolLocation = new Point((parentSize.Width - toolSize.Width - margins.Right), (parentSize.Height - toolSize.Height - margins.Bottom));
                    break;
            }
            _Tools.Bounds = new Rectangle(toolLocation, toolSize);
        }
        /// <summary>
        /// Uživatel nebo kód vybral jiný Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SettingsValueChanged(object sender, EventArgs e)
        {
            if (_SettingsCombo.SelectedItem is ChartSetting setting)
                _SelectSetting(setting, true);
        }

        private void Chart_DoubleClick(object sender, EventArgs e)
        {
            var sit = _Chart.SelectedItems;
            _Chart.SelectionMode = DC.ElementSelectionMode.Extended;
        }
        /// <summary>
        /// Data grafu
        /// </summary>
        private System.Data.DataTable _DataSource;
        /// <summary>
        /// Pole jednotlivých Settings grafu
        /// </summary>
        private List<ChartSetting> _ChartSettings;
        /// <summary>
        /// Aktuálně zobrazený setting grafu (obsahuje ID, název a layout).
        /// </summary>
        private ChartSetting _CurrentSetting;

        #endregion
        #region Public rozhraní
        /// <summary>
        /// Řídí viditelnost minitoolbaru = { Combo typu grafu + Edit button }
        /// </summary>
        public bool ChartToolsVisible { get { return this._Tools.Visible; } set { this._Tools.Visible = value; } }
        /// <summary>
        /// Umístění minitoolbaru v rámci grafu
        /// </summary>
        public ContentAlignment ChartToolsAlignment { get { return this._ChartToolsAlignment; } set { this._ChartToolsAlignment = value; _SetToolsPositions(); } } private ContentAlignment _ChartToolsAlignment;
        /// <summary>
        /// Otevře editor aktuálního grafu, po jeho potvrzení [ OK ] se aktualizuje samotný graf, nový Setting se uloží do <see cref="CurrentSettings"/> a vyvolá se event 
        /// </summary>
        /// <returns></returns>
        public bool EditChartLayout()
        {
            return _MenuActionEdit(MenuAction.Edit);
        }
        /// <summary>
        /// Událost vyvolaná po jakékoli změně grafu
        /// </summary>
        public event EventHandler<ChartChangedArgs> ChartChanged;
        #region Settings, Layout a data
        /// <summary>
        /// Data grafu. Je třeba setovat jako první, před layoutem.
        /// </summary>
        public System.Data.DataTable DataSource
        {
            get { return _DataSource; }
            set { _SetDataSource(value); }
        }
        /// <summary>
        /// Pole všech settingů grafu. Lze vložit novou hodnotu. 
        /// Nelze provést odebrání jednoho nebo několika ze settings, ale lze převzít pole, upravit jeho obsah a vrátit jej zpátky.
        /// Pozor: vložení nové kolekce NEZMĚNÍ <see cref="CurrentSettings"/> ani aktuální layout, ale změní obsah Tools.Combo.
        /// Důvod chování: typicky máme vložit kolekci grafů do <see cref="ChartSettings"/> a poté vložit aktivní Settings do <see cref="CurrentSettings"/>.
        /// Pokud bychom po vložení nové kolekce vybrali některou položku jako aktivní, graf by blikl.
        /// </summary>
        public IEnumerable<ChartSetting> ChartSettings
        {
            get { return _ChartSettings.ToArray(); }
            set { _SetSettings(value); }
        }
        /// <summary>
        /// Aktuálně zobrazený setting grafu (obsahuje ID, název a layout).
        /// Lze vložit new instanci (nebo i null), pak bude dostupná v této property (i včetně null), a navíc:
        /// a) lze vložit NULL, tím bude aktuálně zobrazený graf vyprázdněn, ale nebude odstraněna žádná definice z <see cref="ChartSettings"/> = uživatel si bude moci vybrat co chce vidět pomocí Tools.Combo;
        /// b) lze vložit některý <see cref="ChartSetting"/> z pole <see cref="ChartSettings"/> = pak bude prostě aktivován;
        /// c) lze vložit nově vygenerovaný <see cref="ChartSetting"/> (který dosud není obsažen v poli <see cref="ChartSettings"/>) = pak bude nový objekt zařazen do tohoto pole i do nabídky Combo a bude zobrazen jeho graf
        /// <para/>
        /// Setování hodnoty vyvolá event <see cref="ChartChanged"/>.
        /// </summary>
        public ChartSetting CurrentSettings
        {
            get { return _CurrentSetting; }
            set { _SetSetting(value, callEvent: true); }
        }
        /// <summary>
        /// Aktuální vzhled grafu. Nesouvisí s ostatními prvky, pouze fyzický layout.
        /// </summary>
        public string CurrentChartLayout
        {
            get { return _GetLayout(); }
            set { _SetLayout(value); }
        }
        #endregion
        #endregion
        #region Private akce - Vložení layoutu atd
        /// <summary>
        /// Vloží nový datový zdroj do grafu.
        /// </summary>
        /// <param name="value"></param>
        private void _SetDataSource(DataTable value)
        {
            if (value != null)
                _Chart.DataSource = value;
            _DataSource = value;
        }
        /// <summary>
        /// Uloží nové pole všech settingů grafu.
        /// Pozor: vložení nové kolekce NEZMĚNÍ <see cref="CurrentSettings"/> ani aktuální layout, ale změní obsah Tools.Combo.
        /// Důvod chování: typicky máme vložit kolekci grafů do <see cref="ChartSettings"/> a poté vložit aktivní Settings do <see cref="CurrentSettings"/>.
        /// Pokud bychom po vložení nové kolekce vybrali některou položku jako aktivní, graf by blikl.
        /// </summary>
        /// <param name="value"></param>
        private void _SetSettings(IEnumerable<ChartSetting> value)
        {
            _ChartSettings = (value is null ? new List<ChartSetting>() : new List<ChartSetting>(value));
            _ComboItemsReload(selectIndex: -1);
        }
        /// <summary>
        /// Do comba <see cref="_SettingsCombo"/> znovunačte obsah pole <see cref="_ChartSettings"/>.
        /// Volitelně zachová index aktuálně vybraného prvku, nebo nastaví daný index.
        /// Při změně nevyvolává event <see cref="ChartChanged"/>.
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <param name="selectIndex"></param>
        /// <param name="keepCurrentItem"></param>
        private void _ComboItemsReload(ChartSetting selectedItem = null, int? selectIndex = null, bool keepCurrentItem = false)
        {
            try
            {
                if (selectedItem is null && keepCurrentItem)
                    selectedItem = _SettingsCombo.SelectedItem as ChartSetting;
                if (selectedItem is null && selectIndex.HasValue && selectIndex.Value >= 0 && selectIndex.Value < _ChartSettings.Count)
                    selectedItem = _ChartSettings[selectIndex.Value];

                _SuppressSettingsChange = true;
                if (_SettingsCombo.Properties.Items.Count > 0)
                    _SettingsCombo.Properties.Items.Clear();
                _SettingsCombo.Properties.Items.AddRange(_ChartSettings);

                _SettingsCombo.SelectedIndex = (selectedItem != null ? _ChartSettings.IndexOf(selectedItem) : -1);
            }
            finally
            {
                _SuppressSettingsChange = false;
            }
        }
        /// <summary>
        /// Vloží daný <see cref="ChartSetting"/> jako aktivní graf.
        /// Zajistí zobrazení jeho layoutu (viz parametr <paramref name="skipSetLayout"/>,
        /// zajistí uložení do <see cref="_CurrentSetting"/>, případné doplnění do pole <see cref="_ChartSettings"/> a aktivaci v <see cref="_SettingsCombo"/>.
        /// <para/>
        /// Tato metoda vyvolá event <see cref="ChartChanged"/>, pokud parametr <paramref name="callEvent"/> bude true.
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="skipSetLayout">true = přeskočit vložení Settingu do grafu. Používá se po editaci nového grau, kdy tento satting je právě načten z grafu.</param>
        /// <param name="callEvent">true = vyvolat event o změně</param>
        private void _SetSetting(ChartSetting setting, bool skipSetLayout = false, bool callEvent = false)
        {
            if (!skipSetLayout)
                _SetLayout(setting?.Layout);

            _CurrentSetting = setting;

            try
            {
                _SuppressSettingsChange = true;
                if (setting is null)
                {
                    _SettingsCombo.SelectedIndex = -1;
                }
                else
                {   // Je vložen nějaký not null objekt:
                    if (!_ChartSettings.Contains(setting))
                    {   // Dosud jsme ho v naší evidenci (List _ChartSettings) neměli:
                        _ChartSettings.Add(setting);
                        _SettingsCombo.Properties.Items.Add(setting);
                    }
                    int comboIndex = _SettingsCombo.Properties.Items.IndexOf(setting);
                    if (comboIndex < 0)
                    {   // Nenašli jsme dodaný prvek v Combu:
                        _SettingsCombo.Properties.Items.Add(setting);
                        comboIndex = _SettingsCombo.Properties.Items.Count - 1;
                    }
                    // Událost _SettingsCombo.SelectedIndexChanged => _SettingsValueChanged() jsme potlačili:
                    _SettingsCombo.SelectedIndex = comboIndex;
                }
                if (callEvent)
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
            }
            finally
            {
                _SuppressSettingsChange = false;
            }
        }
        /// <summary>
        /// V Tools.Combo byl vybrán daný Setting, možná na to budeme reagovat?
        /// <para/>
        /// Tato metoda vyvolá event <see cref="ChartChanged"/>, pokud parametr <paramref name="callEvent"/> bude true.
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="callEvent">true = vyvolat event o změně</param>
        private void _SelectSetting(ChartSetting setting, bool callEvent = false)
        {
            if (!_SuppressSettingsChange)
            {
                _CurrentSetting = setting;
                _SetLayout(setting?.Layout);
                if (callEvent)
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
            }
        }
        /// <summary>
        /// Načte a vrátí Layout z aktuálního grafu
        /// </summary>
        /// <returns></returns>
        private string _GetLayout()
        {
            string layout = null;
            if (_IsChartValid)
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    _Chart.SaveToStream(ms);
                    layout = Encoding.UTF8.GetString(ms.GetBuffer());
                }
            }
            return layout;
        }
        /// <summary>
        /// Vloží daný string jako Layout do grafu. 
        /// Tato metoda nevolá žádné eventy. 
        /// Nemění ani Tools.Combo.SelectedItem ani jeho Items.
        /// </summary>
        /// <param name="layout"></param>
        private void _SetLayout(string layout)
        {
            if (_IsChartValid)
            {
                try
                {
                    byte[] buffer = (!String.IsNullOrEmpty(layout) ? Encoding.UTF8.GetBytes(layout) : new byte[0]);
                    if (buffer.Length > 0)
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer))
                            _Chart.LoadFromStream(ms);     // Pozor, zahodí data !!!
                    }
                    else
                    {
                        _Chart.Series.Clear();
                        _Chart.Legends.Clear();
                        _Chart.Titles.Clear();
                    }
                }
                catch (DevExpress.XtraCharts.LayoutStreamException) { }
                finally
                {
                    _Chart.DataSource = _DataSource;       // Vrátíme data do grafu
                }
            }
        }
        /// <summary>
        /// Obsahuje true, pokud graf je platný (není null, má data a není Disposed)
        /// </summary>
        private bool _IsChartValid { get { return (_Chart != null && _Chart.DataSource != null && !_Chart.IsDisposed); } }
        /// <summary>
        /// Pokud je true, pak se neprování přenačtení layoutu v eventhandleru Tools.Combo.SelectedItemChanged
        /// </summary>
        private bool _SuppressSettingsChange;
        #endregion
        #region Private akce - Deklarace menu, akce dle menu
        /// <summary>
        /// Nadefinuje menu grafu
        /// </summary>
        private void MenuDeclare()
        {
            _EditDxMenu = new DM.DXPopupMenu();

            var imageNew = AsolDX.DxComponent.GetBitmapImage("images/actions/new_32x32.png");
            var imageClone = AsolDX.DxComponent.GetBitmapImage("images/xaf/action_clonemerge_clone_object_32x32.png");
            var imagePaste = AsolDX.DxComponent.GetBitmapImage("images/edit/paste_32x32.png");
            var imageRename = AsolDX.DxComponent.GetBitmapImage("images/richedit/highlight_32x32.png");
            var imageWizard = AsolDX.DxComponent.GetBitmapImage("images/miscellaneous/runchartdesigner_32x32.png");
            var imageEditor = AsolDX.DxComponent.GetBitmapImage("images/chart/chartchangestyle_32x32.png");
            var imageCopy = AsolDX.DxComponent.GetBitmapImage("images/edit/copy_32x32.png");
            var imageDelete = AsolDX.DxComponent.GetBitmapImage("images/actions/deletelist_32x32.png");
            var imagePrintPdf = AsolDX.DxComponent.GetBitmapImage("images/xaf/action_export_topdf_32x32.png");

            _EditDxMenu.Items.Add(CreateMenuItem("Nový prázdný", "Vytvoří novou prázdnou definici grafu nad týmiž daty.", imageNew, MenuAction.NewEmpty));
            _EditDxMenu.Items.Add(CreateMenuItem("Nový z aktuálního grafu", "Vytvoří novou definici grafu nad týmiž daty, vzhled převezme z aktuálního grafu.", imageClone, MenuAction.NewCopy));
            _EditDxMenu.Items.Add(CreateMenuItem("Nový ze schránky", "Pokusí se najít ve schránce (Clipboard) definici grafu a vloží ji jako novou.", imagePaste, MenuAction.PasteFromClipboard));
            _EditDxMenu.Items.Add(CreateMenuItem("Přejmenovat název nabídky", "Umožní změnit název aktuální definice grafu.", imageRename, MenuAction.Rename));
            _EditDxMenu.Items.Add(CreateMenuItem("Navrhnout vzhled", "Otevře Wizard vzhledu grafu.", imageWizard, MenuAction.Wizard));
            _EditDxMenu.Items.Add(CreateMenuItem("Upravit vzhled", "Otevře Editor vzhledu grafu.", imageEditor, MenuAction.Edit));
            _EditDxMenu.Items.Add(CreateMenuItem("Uložit definici grafu do schránky", "Aktuální definici vzhledu grafu uloží jako XML data do schránky (Clipboard), bude možno jej např. vložit jako soubor nebo přílohu mailu (Ctrl+V)", imageCopy, MenuAction.CopyXmlLayoutToClipboard));
            _EditDxMenu.Items.Add(CreateMenuItem("Uložit obrázek grafu do schránky jak SVG", "Aktuální obrázek grafu uloží do schránky (Clipboard), bude možno jej např. vložit jako soubor nebo přílohu mailu (Ctrl+V)", imageCopy, MenuAction.CopyChartImageToClipboardAsSvg));
            _EditDxMenu.Items.Add(CreateMenuItem("Odstranit definici", "Aktuální definici odebere a zahodí.", imageDelete, MenuAction.Delete));
            _EditDxMenu.Items.Add(CreateMenuItem("Tisk do PDF", "Aktuální graf uloží jako PDF.", imagePrintPdf, MenuAction.ExportPdf));
        }
        /// <summary>
        /// Vytvoří a vrátí jednu položku menu
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="toolTip"></param>
        /// <param name="image"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private DM.DXMenuItem CreateMenuItem(string caption, string toolTip, Image image, MenuAction action)
        {
            DM.DXMenuItem item = new DM.DXMenuItem(caption, _MenuItemClick, image) { Tag = action };
            item.SuperTip = new DevExpress.Utils.SuperToolTip();
            item.SuperTip.Items.AddTitle(caption);
            item.SuperTip.Items.AddSeparator();
            item.SuperTip.Items.Add(toolTip);
            return item;
        }
        /// <summary>
        /// Obsluha Menu.Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _MenuItemClick(object sender, EventArgs args)
        {
            if (!(sender is DM.DXMenuItem menuItem)) return;
            if (!(menuItem.Tag is MenuAction action)) return;
            MenuRunAction(action);
        }
        /// <summary>
        /// Akce v menu
        /// </summary>
        private enum MenuAction { None, NewEmpty, NewCopy, PasteFromClipboard, Wizard, Edit, Rename, CopyXmlLayoutToClipboard, CopyChartImageToClipboardAsSvg, Delete, ExportPdf }
        /// <summary>
        /// Provedení akce v menu
        /// </summary>
        /// <param name="action"></param>
        private void MenuRunAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.NewEmpty:
                    _MenuActionNewEmpty();
                    break;
                case MenuAction.NewCopy:
                    _MenuActionNewCopy();
                    break;
                case MenuAction.PasteFromClipboard:
                    _MenuActionPasteFromClipboard();
                    break;
                case MenuAction.Rename:
                    _MenuActionRename();
                    break;
                case MenuAction.Wizard:
                    _MenuActionEdit(MenuAction.Wizard);
                    break;
                case MenuAction.Edit:
                    _MenuActionEdit(MenuAction.Edit);
                    break;
                case MenuAction.CopyXmlLayoutToClipboard:
                    _MenuActionCopyXmlLayoutToClipboard();
                    break;
                case MenuAction.CopyChartImageToClipboardAsSvg:
                    _MenuActionCopyChartImageToClipboardAsSvg();
                    break;
                case MenuAction.Delete:
                    _MenuActionDelete();
                    break;
                case MenuAction.ExportPdf:
                    _MenuActionExportPdf();
                    break;
            }
        }
        private void _MenuActionNewEmpty()
        {
            string name = "Nový graf";
            if (_EditChartName(ref name))
            {
                string layout = "";
                if (_EditChartLayout(ref layout, MenuAction.Wizard))
                {
                    ChartSetting setting = new ChartSetting(name, layout);
                    _ChartChanged(ChartChangeType.NewSettings, setting);
                    _SetSetting(setting, true);
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
                }
            }
        }
        private void _MenuActionNewCopy()
        {
            string name = (_CurrentSetting != null ? (_CurrentSetting.Name + " 2") : "Nový graf");
            if (_EditChartName(ref name))
            {
                string layout = _GetLayout();
                if (_EditChartLayout(ref layout, MenuAction.Wizard))
                {
                    ChartSetting setting = new ChartSetting(name, layout);
                    _ChartChanged(ChartChangeType.NewSettings, setting);
                    _SetSetting(setting, true);
                    _ChartChanged(ChartChangeType.SelectSetting, setting);
                }
            }
        }
        private void _MenuActionPasteFromClipboard()
        {
            string definition = null;
            if (WF.Clipboard.ContainsText(WF.TextDataFormat.UnicodeText))
                definition = WF.Clipboard.GetText(WF.TextDataFormat.UnicodeText);
            else if (WF.Clipboard.ContainsText(WF.TextDataFormat.Text))
                definition = WF.Clipboard.GetText(WF.TextDataFormat.Text);
            if (String.IsNullOrEmpty(definition))
                return;

            ChartSetting setting = ChartSetting.CreateFromDefinition(definition);
            if (setting is null)
                return;

            string layout = setting.Layout;
            if (_EditChartLayout(ref layout, MenuAction.Wizard))
            {
                setting.Layout = layout;
                _ChartChanged(ChartChangeType.NewSettings, setting);
                _SetSetting(setting, true);
                _ChartChanged(ChartChangeType.SelectSetting, setting);
            }
        }
        private void _MenuActionRename()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            string name = setting.Name;
            if (_EditChartName(ref name, true))
            {
                setting.Name = name;
                _SettingsCombo.Text = name;      // Poznámka: nic jiného nefunguje, například Refresh();  RefreshEditValue();  SelectedItem = _CurrentSettings; => nic nepromítne aktuální text do comba!!!
                _ChartChanged(ChartChangeType.ChangeName, setting);
            }
        }
        /// <summary>
        /// Akce: edituj vzhled grafu
        /// </summary>
        /// <returns></returns>
        private bool _MenuActionEdit(MenuAction action)
        {
            var setting = _CurrentSetting;
            if (setting is null) return false;
            string layout = null;
            bool result = _EditChartLayout(ref layout, action);
            if (result)
            {
                setting.Layout = layout;
                _ChartChanged(ChartChangeType.ChangeLayout, setting);
            }
            return result;
        }
        private void _MenuActionCopyXmlLayoutToClipboard()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            WF.Clipboard.SetText(setting.Definition);
        }
        private void _MenuActionCopyChartImageToClipboardAsSvg()
        {
            var chart = _Chart;
            using (var stream = new SIO.MemoryStream())
            {
                chart.ExportToSvg(stream);
                var buffer = stream.ToArray();
                string content = System.Text.Encoding.UTF8.GetString(buffer);
            }
        }
        private void _MenuActionDelete()
        {
            var setting = _CurrentSetting;
            if (setting is null) return;
            int index = _ChartSettings.IndexOf(setting);
            if (index < 0) return;
            _ChartSettings.RemoveAt(index);
            _ChartChanged(ChartChangeType.Delete, setting);
            int count = _ChartSettings.Count;
            setting = (index < count ? _ChartSettings[index] : (count > 0 ? _ChartSettings[count - 1] : null));
            _ComboItemsReload(setting);
            CurrentSettings = setting;
        }
        private void _MenuActionExportPdf()
        {
            string fileName = null;
            using (var dialog = new WF.SaveFileDialog())
            {
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.DefaultExt = ".pdf";
                dialog.Filter = "PDF dokument|*.pdf|SVG grafika|*.svg";   // Řetězce pro různé volby filtrování rovněž musí být odděleny svislou čarou. Příklad: Textové soubory (*.txt)|*.txt|Všechny soubory (*.*)|*.*'

                var result = dialog.ShowDialog(this.FindForm());
                if (result == WF.DialogResult.OK)
                    fileName = dialog.FileName;
            }
            if (fileName is null) return;

            byte[] content = null;
            using (SIO.MemoryStream ms = new SIO.MemoryStream())
            {
                var options = new DevExpress.XtraPrinting.PdfExportOptions()
                {
                    ConvertImagesToJpeg = false,
                    ImageQuality = DevExpress.XtraPrinting.PdfJpegImageQuality.Highest,
                    PdfACompatibility = DevExpress.XtraPrinting.PdfACompatibility.PdfA1b,
                    RasterizationResolution = 300,
                    ShowPrintDialogOnOpen = false
                };
                // _Chart.ExportToDocx
                _Chart.ExportToPdf(ms, options);
                content = ms.ToArray();
            }

            if (fileName != null)
            {
                try
                {
                    SIO.File.WriteAllBytes(fileName, content);
                    var result = WF.MessageBox.Show(this.FindForm(), "Graf je uložen. Přejete si jej otevřít?", "Export grafu...", WF.MessageBoxButtons.YesNo, WF.MessageBoxIcon.Question);
                    if (result == WF.DialogResult.Yes)
                        System.Diagnostics.Process.Start(fileName);
                }
                catch (Exception exc)
                {
                    WF.MessageBox.Show(this.FindForm(), "Došlo k chybě: " + exc.Message, "Export grafu...", WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Exclamation);
                }
            }
        }
        /// <summary>
        /// Výkonná procedura pro změnu názvu (=vyvolání okna), ale nepracuje s <see cref="_CurrentSetting"/> ani nevolá eventy.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="detectChange"></param>
        /// <returns></returns>
        private bool _EditChartName(ref string name, bool detectChange = false)
        {
            string oldName = name;
            string newName = TestDevExpress.Forms.InputForm.InputDialogShow(this.FindForm(), null, "Název grafu:", oldName);
            if (String.IsNullOrEmpty(newName) || (detectChange && String.Equals(oldName, newName))) return false;
            name = newName;
            return true;
        }
        /// <summary>
        /// Výkonná procedura pro změnu vzhledu grafu (=vyvolání Designeru), ale nepracuje s <see cref="_CurrentSetting"/> ani nevolá eventy.
        /// <para/>
        /// Zajistí editaci vzhledu grafu pomocí designeru <see cref="DC.Designer.ChartDesigner"/>.
        /// Do grafu může předem vložit daný Layout z parametru <paramref name="layout"/> (pokud není NULL).
        /// Pokud v Designeru bude potvrzen zadaný Layout tlačítkem OK, pak získá Layout editovaného grafu a vloží jej do ref parametru <paramref name="layout"/>, a vrátí true.
        /// Layout neukládá do žádného <see cref="ChartSetting"/>.
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool _EditChartLayout(ref string layout, MenuAction action)
        {
            bool result = false;

            string oldLayout = null;
            if (layout != null)
            {
                oldLayout = _GetLayout();
                _SetLayout(layout);
            }

            var chart = _Chart;

            if (action == MenuAction.Wizard)
            {   // Wizard
                result = Noris.Clients.Win.Components.AsolDX.DxChartWizard.ShowWizard(chart, "Chart Wizard", true, false);
            }
            else
            {   // Editor
                result = Noris.Clients.Win.Components.AsolDX.DxChartDesigner.DesignChart(chart, "Chart Designer", true, false);
            }

            if (result)
            {
                layout = _GetLayout();
            }
            else if (oldLayout != null)
                // Storno v Designeru, a my máme uschován původní layout grafu => vrátíme jej:
                _SetLayout(oldLayout);

            return result;
        }
        /// <summary>
        /// Vyvolá event <see cref="ChartChanged"/> s danými hodnotami
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="setting"></param>
        private void _ChartChanged(ChartChangeType changeType, ChartSetting setting)
        {
            if (ChartChanged == null) return;
            ChartChangedArgs args = new ChartChangedArgs(changeType, setting);
            ChartChanged(this, args);
        }
        #endregion
        #region Public static: Create Samples
        /// <summary>
        /// Vygeneruje a vrátí sample data
        /// </summary>
        /// <returns></returns>
        public static System.Data.DataTable CreateSampleData()
        {
            System.Data.DataTable table = new System.Data.DataTable();

            table.Columns.Add(new System.Data.DataColumn("datetime") { AllowDBNull = true, Caption = "Datum", DataType = typeof(DateTime) });
            table.Columns.Add(new System.Data.DataColumn("temperature") { AllowDBNull = true, Caption = "Teplota", DataType = typeof(float) });
            table.Columns.Add(new System.Data.DataColumn("pressure") { AllowDBNull = true, Caption = "Tlak", DataType = typeof(float) });
            table.Columns.Add(new System.Data.DataColumn("humidity") { AllowDBNull = true, Caption = "Vlhkost", DataType = typeof(float) });
            table.Columns.Add(new System.Data.DataColumn("wind_speed") { AllowDBNull = true, Caption = "Rychlost větru", DataType = typeof(float) });
            table.Columns.Add(new System.Data.DataColumn("wind_azimuth") { AllowDBNull = true, Caption = "Směr větru", DataType = typeof(float) });

            DateTime start = DateTime.Now.Date;


            string data = @"
0 7 1014 70 3 60
2 7 1014 71 2 70
4 6 1013 73 3 70
6 6 1013 74 4 80
8 8 1013 70 4 120
10 9 1014 68 6 120
12 9 1014 66 6 130
14 10 1015 65 7 140
16 11 1016 63 6 140
18 10 1017 64 5 130
20 10 1017 65 5 140
22 9 1017 68 5 150
0 8 1018 71 4 160
2 8 1019 72 4 170
4 7 1020 74 3 180
6 8 1020 73 3 190
8 8 1021 72 4 160
10 10 1023 68 5 120
12 13 1023 66 8 80
14 14 1023 65 12 120
16 15 1024 64 14 160
18 14 1024 65 11 150
20 13 1024 67 9 110
22 12 1023 71 6 100
0 11 1023 76 3 25
2 10 1022 77 4 40
4 9 1022 81 1 10
6 8 1023 80 0 0
8 9 1022 76 0 0
10 11 1023 70 12 270
12 14 1025 67 16 300
14 15 1025 63 16 300
16 15 1024 60 18 310
18 14 1023 63 18 290
20 13 1022 64 16 280
22 11 1021 69 14 270
0 10 1020 71 8 270
2 9 1019 74 7 270
4 8 1018 75 5 270
6 8 1017 74 4 250
8 11 1018 70 4 250
10 14 1021 63 4 260
12 17 1024 60 3 260
14 19 1025 58 2 250
16 20 1026 57 1 250
18 18 1025 59 5 210
20 17 1024 64 6 200
22 15 1024 67 7 180
0 14 1023 71 8 90";

            DateTime date = DateTime.Now.Date;
            string[] rows = data.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int lastHour = 0;
            foreach (string row in rows)
            {
                string[] items = row.Split(' ');
                int hour = Int32.Parse(items[0]);
                float temp = Int32.Parse(items[1]);
                float press = Int32.Parse(items[2]);
                float humid = Int32.Parse(items[3]);
                float speed = Int32.Parse(items[4]);
                float azim = Int32.Parse(items[5]);

                if (hour < lastHour) date = date.AddDays(1d).Date;
                DateTime time = date.AddHours(hour);
                table.Rows.Add(time, temp, press, humid, speed, azim);
                lastHour = hour;
            }

            //   System.Windows.Forms.Clipboard.SetText(table.GetHtml(), WF.TextDataFormat.UnicodeText);

            return table;
        }
        /// <summary>
        /// Vytvoří a vrátí sadu Settings pro graf, použitelné pro data z metody <see cref="CreateSampleData()"/>
        /// </summary>
        /// <returns></returns>
        public static ChartSetting[] CreateSampleSettings()
        {
            List<ChartSetting> chartSettings = new List<ChartSetting>();
            chartSettings.Add(CreateSampleSetting1());
            chartSettings.Add(CreateSampleSetting2());
            return chartSettings.ToArray();
        }
        private static ChartSetting CreateSampleSetting1()
        {
            string layout = @"﻿<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='19.2.6.0'>
  <Chart AppearanceNameSerializable='Chameleon' BackColor='253, 234, 218' PaletteName='Chameleon' PaletteBaseColorNumber='1' SelectionMode='Extended' SeriesSelectionMode='Series' AnimationStartMode='OnDataChanged'>
    <DataContainer BoundSeriesSorting='None'>
      <SeriesSerializable>
        <Item1 Name='Series 1' ArgumentDataMember='datetime' ToolTipHintDataMember='wind_speed' ValueDataMembersSerializable='wind_speed' ArgumentScaleType='DateTime' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View LineTensionPercent='70' AggregateFunction='Average' TypeNameSerializable='SplineSeriesView'>
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
          </View>
          <DateTimeSummaryOptions MeasureUnit='Hour' MeasureUnitMultiplier='1' />
        </Item1>
        <Item2 Name='Series 2' LegendName='Default Legend' ArgumentDataMember='datetime' ValueDataMembersSerializable='temperature' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View TypeNameSerializable='LineSeriesView'>
            <SeriesPointAnimation TypeNameSerializable='XYMarkerWidenAnimation' />
          </View>
          <DateTimeSummaryOptions MeasureUnit='Hour' MeasureUnitMultiplier='1' />
        </Item2>
        <Item3 Name='Series 3' ArgumentDataMember='datetime' ValueDataMembersSerializable='pressure' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText=''>
          <View PaneName='Pane 1' AxisYName='Secondary AxisY 1' TypeNameSerializable='StackedSplineAreaSeriesView' />
        </Item3>
      </SeriesSerializable>
      <SeriesTemplate CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
    <Titles>
      <Item1 Text='Graf teploty' Font='Tahoma, 18pt' TextColor='' Antialiasing='true' EnableAntialiasing='Default' />
    </Titles>
    <Border Visibility='False' />
    <OptionsPrint SizeMode='Zoom' />
    <Diagram RuntimePaneCollapse='true' RuntimePaneResize='false' PaneLayoutDirection='Vertical' TypeNameSerializable='XYDiagram'>
      <AxisX StickToEnd='false' LabelVisibilityMode='AutoGeneratedAndCustom' Visibility='True' Thickness='3' VisibleInPanesSerializable='-1;0' ShowBehind='true' MinorCount='3'>
        <Title Visibility='Default' Text='Datum a čas' />
        <CustomLabels>
          <Item1 AxisValueSerializable='05/18/2020 22:00:00.000' GridLineVisible='true' BackColor='255, 255, 192' Name='Počátek' />
        </CustomLabels>
        <Label Staggered='true' TextPattern='{a:d.M H:mm}'>
          <Border Visibility='False' />
        </Label>
        <GridLines Visible='true' MinorVisible='true' MinorColor='242, 242, 242'>
          <MinorLineStyle DashStyle='Dash' />
        </GridLines>
        <DateTimeScaleOptions ScaleMode='Continuous'>
          <IntervalOptions />
        </DateTimeScaleOptions>
      </AxisX>
      <AxisY VisibleInPanesSerializable='-1' ShowBehind='false'>
        <Title Visibility='Default' Text='Teplota °C' Font='Tahoma, 12pt, style=Italic' />
      </AxisY>
      <SecondaryAxesY>
        <Item1 AxisID='0' Visibility='True' Alignment='Near' VisibleInPanesSerializable='0' ShowBehind='false' Name='Secondary AxisY 1'>
          <Title Visibility='Default' Text='Tlak hPa' Font='Tahoma, 12pt, style=Italic' />
          <VisualRange Auto='false' MinValueSerializable='980' MaxValueSerializable='1050' />
          <GridLines Visible='true' />
        </Item1>
      </SecondaryAxesY>
      <SelectionOptions />
      <DefaultPane BackColor='196, 189, 151'>
        <FillStyle FillMode='Gradient'>
          <Options GradientMode='BottomToTop' Color2='229, 185, 183' TypeNameSerializable='RectangleGradientFillOptions' />
        </FillStyle>
        <StackedBarTotalLabel />
      </DefaultPane>
      <Panes>
        <Item1 PaneID='0' BackColor='141, 179, 226' Name='Pane 1'>
          <StackedBarTotalLabel />
        </Item1>
      </Panes>
    </Diagram>
    <ToolTipOptions ShowForSeries='true' />
  </Chart>
</ChartXmlSerializer>"
            .Replace("'", "\"");
            return new ChartSetting("Dvojgraf Teplota + Tlak", layout);
        }
        private static ChartSetting CreateSampleSetting2()
        {
            string layout = @"﻿<?xml version='1.0' encoding='utf-8'?>
<ChartXmlSerializer version='19.2.6.0'>
  <Chart AppearanceNameSerializable='Chameleon' BackColor='253, 234, 218' PaletteName='Chameleon' PaletteBaseColorNumber='1' SelectionMode='Extended' SeriesSelectionMode='Series' AnimationStartMode='OnDataChanged'>
    <DataContainer BoundSeriesSorting='None'>
      <SeriesSerializable>
        <Item1 Name='Series 1' ArgumentDataMember='datetime' ValueDataMembersSerializable='temperature' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
      </SeriesSerializable>
      <SeriesTemplate ArgumentDataMember='datetime' ValueDataMembersSerializable='temperature' CrosshairContentShowMode='Default' CrosshairEmptyValueLegendText='' />
    </DataContainer>
    <Titles>
      <Item1 Text='Teplota' Font='Tahoma, 18pt' TextColor='' Antialiasing='true' EnableAntialiasing='Default' />
    </Titles>
    <Border Visibility='False' />
    <OptionsPrint SizeMode='Zoom' />
    <Diagram RuntimePaneCollapse='true' RuntimePaneResize='false' PaneLayoutDirection='Vertical' TypeNameSerializable='XYDiagram'>
      <AxisX StickToEnd='false' Visibility='True' VisibleInPanesSerializable='-1' ShowBehind='false' MinorCount='1'>
        <Tickmarks MinorVisible='false' />
        <Label Angle='315' MaxLineCount='12' TextPattern='{A:d. MMMM yyyy H:mm}' EnableAntialiasing='True'>
          <Border Visibility='False' />
        </Label>
        <GridLines Visible='true' />
        <DateTimeScaleOptions MeasureUnit='Hour' GridAlignment='Hour' AutoGrid='false' GridSpacing='8' AggregateFunction='None'>
          <IntervalOptions />
        </DateTimeScaleOptions>
      </AxisX>
      <AxisY VisibleInPanesSerializable='-1' ShowBehind='false' />
      <SelectionOptions />
      <DefaultPane BackColor='235, 241, 221'>
        <FillStyle FillMode='Gradient'>
          <Options Color2='251, 213, 181' TypeNameSerializable='RectangleGradientFillOptions' />
        </FillStyle>
        <StackedBarTotalLabel />
      </DefaultPane>
    </Diagram>
    <ToolTipOptions ShowForSeries='true' />
  </Chart>
</ChartXmlSerializer>"
            .Replace("'", "\"");
            return new ChartSetting("Sloupcový graf Teplota", layout);
        }
        #endregion
    }
    /// <summary>
    /// Argumenty pro eventhandler Změna grafu
    /// </summary>
    public class ChartChangedArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="changeType"></param>
        /// <param name="setting"></param>
        public ChartChangedArgs(ChartChangeType changeType, ChartSetting setting)
        {
            this.ChangeType = changeType;
            this.Setting = setting;
        }
        /// <summary>
        /// Druh změny
        /// </summary>
        public ChartChangeType ChangeType { get; private set; }
        /// <summary>
        /// Definice grafu
        /// </summary>
        public ChartSetting Setting { get; private set; }
    }
    /// <summary>
    /// Druhy změn grafu
    /// </summary>
    public enum ChartChangeType
    {
        /// <summary>
        /// Není změna
        /// </summary>
        None,
        /// <summary>
        /// Aktivovaná daná definice
        /// </summary>
        SelectSetting,
        /// <summary>
        /// Založena nová definice
        /// </summary>
        NewSettings,
        /// <summary>
        /// Změna názvu definice
        /// </summary>
        ChangeName,
        /// <summary>
        /// Změna vzhledu garfu
        /// </summary>
        ChangeLayout,
        /// <summary>
        /// Smazání definice
        /// </summary>
        Delete
    }
    /// <summary>
    /// Nastavení vzhledu grafu = ID, Název + obsah
    /// </summary>
    public class ChartSetting
    {
        #region Konstruktor, public property
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ChartSetting() { Id = AsolDX.DxComponent.CreateGuid(); Name = ""; Layout = ""; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layout"></param>
        public ChartSetting(string name, string layout) { Id = AsolDX.DxComponent.CreateGuid(); Name = name; Layout = layout; }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="layout"></param>
        public ChartSetting(string id, string name, string layout) { Id = id; Name = name; Layout = layout; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name ?? Id;
        }
        /// <summary>
        /// ID prvku, jazykově nezávislé
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Text prvku = název (titulek), lokalizovaný
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Jméno validní (bez CR, LF, TAB)
        /// </summary>
        public string NameValid
        {
            get
            {
                string name = this.Name;
                if (String.IsNullOrEmpty(name)) return name;
                name = name.Trim()
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " ");
                return name.Trim();
            }
        }
        /// <summary>
        /// Jméno pro soubor (validní, bez nepovolených znaků)
        /// </summary>
        public string NameFile
        {
            get
            {
                string name = NameValid;
                var ics = System.IO.Path.GetInvalidFileNameChars();
                foreach (char ic in ics)
                {
                    if (name.Contains(ic))
                        name = name.Replace(ic, '_');
                }
                return name;
            }
        }
        /// <summary>
        /// Obsah prvku, XML kód definující graf
        /// </summary>
        public string Layout { get; set; }
        /// <summary>
        /// Libovolná další data. 
        /// Udržuje si je aplikace (výkonné algoritmy grafů se o <see cref="Tag"/> nestarají).
        /// Hodnota se nijak neserializuje (hodnota <see cref="Tag"/> se nedostává ani do property <see cref="Definition"/> ani do metody <see cref="CreateFromDefinition(string)"/>).
        /// </summary>
        public object Tag { get; set; }
        #endregion
        #region Serializace
        /// <summary>
        /// Obsahuje plnou definici grafu (<see cref="Id"/>, <see cref="Name"/>, <see cref="Layout"/>).
        /// Z tohoto testu je možno vygenerovat novou instanci pomocí static metody <see cref="CreateFromDefinition(string)"/>.
        /// </summary>
        public string Definition
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<ChartDefinition Version=1.0>");
                sb.AppendLine($"  <Id='{Id}'>");
                sb.AppendLine($"  <Name='{NameValid}'>");
                sb.AppendLine("</ChartDefinition>");
                sb.Append(Layout);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Vytvoří new instanci z dodaného stringu definice. Může vrátit null, pokud dodaná definice není OK.
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static ChartSetting CreateFromDefinition(string definition)
        {
            if (String.IsNullOrEmpty(definition)) return null;

            string versionText = "<ChartDefinition Version=";
            int versionLength = versionText.Length;
            string version = null;
            string idText = "<Id='";
            int idLength = idText.Length;
            string id = null;
            string nameText = "<Name='";
            int nameLength = nameText.Length;
            string name = null;
            string endText = "</ChartDefinition>";
            bool endFound = false;
            while (definition.Length > 0)
            {
                string line = _ReadLine(ref definition, true);
                if (line.StartsWith(versionText))
                {
                    if (version is null) version = line.Substring(versionLength, 3);
                }
                else if (line.StartsWith(idText))
                {
                    if (id is null) id = line.Substring(idLength, line.Length - idLength - 2);
                }
                else if (line.StartsWith(nameText))
                {
                    if (name is null) name = line.Substring(nameLength, line.Length - nameLength - 2);
                }
                else if (line.StartsWith(endText))
                {
                    endFound = true;
                    break;
                }
            }
            if (version is null || id is null || name is null || !endFound || String.IsNullOrEmpty(definition)) return null;
            string layout = definition;
            return new ChartSetting(id, name, layout);
        }
        /// <summary>
        /// Vrátí další řádek odebraný z textu
        /// </summary>
        /// <param name="text"></param>
        /// <param name="trimLine"></param>
        /// <returns></returns>
        private static string _ReadLine(ref string text, bool trimLine = false)
        {
            if (text is null || text.Length == 0) return "";
            string line = "";
            int index = text.IndexOfAny(new char[] { '\r', '\n' });
            if (index < 0)
            {
                line = text;
                text = "";
            }
            else
            {
                line = (index == 0 ? "" : text.Substring(0, index));
                int length = text.Length;
                while (index < length && (text[index] == '\r' || text[index] == '\n'))
                    index++;
                text = (index < length ? text.Substring(index) : "");
            }
            if (trimLine && line.Length > 0)
                line = line.Trim();
            return line;
        }
        #endregion
    }

    // nepoužito:

    public abstract class ChartData : System.ComponentModel.IBindingList
    {
        #region IBindingList
        /// <summary>
        /// Gets whether you can add items to the list using System.ComponentModel.IBindingList.AddNew.
        /// </summary>
        bool IBindingList.AllowNew { get; }
        /// <summary>
        /// Gets whether you can update items in the list.
        /// </summary>
        bool IBindingList.AllowEdit { get; }
        /// <summary>
        /// Gets whether you can remove items from the list, using System.Collections.IList.Remove(System.Object) or System.Collections.IList.RemoveAt(System.Int32).
        /// </summary>
        bool IBindingList.AllowRemove { get; }
        /// <summary>
        /// Gets whether a System.ComponentModel.IBindingList.ListChanged event is raised
        /// when the list changes or an item in the list changes.
        /// </summary>
        bool IBindingList.SupportsChangeNotification { get; }
        /// <summary>
        /// Gets whether the list supports searching using the System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor,System.Object) method.
        /// </summary>
        bool IBindingList.SupportsSearching { get; }
        /// <summary>
        /// Gets whether the list supports sorting.
        /// </summary>
        bool IBindingList.SupportsSorting { get; }
        /// <summary>
        /// Gets whether the items in the list are sorted.
        /// </summary>
        bool IBindingList.IsSorted { get; }
        /// <summary>
        /// Gets the System.ComponentModel.PropertyDescriptor that is being used for sorting.
        /// </summary>
        PropertyDescriptor IBindingList.SortProperty { get; }
        /// <summary>
        /// Gets the direction of the sort.
        /// </summary>
        ListSortDirection IBindingList.SortDirection { get; }
        /// <summary>
        /// Occurs when the list changes or an item in the list changes.
        /// </summary>
        event ListChangedEventHandler IBindingList.ListChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Adds the System.ComponentModel.PropertyDescriptor to the indexes used for searching.
        /// </summary>
        /// <param name="property">The System.ComponentModel.PropertyDescriptor to add to the indexes used for searching.</param>
        void IBindingList.AddIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Adds a new item to the list.
        /// </summary>
        /// <returns>The item added to the list.</returns>
        object IBindingList.AddNew()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Sorts the list based on a System.ComponentModel.PropertyDescriptor and a System.ComponentModel.ListSortDirection.
        /// </summary>
        /// <param name="property">The System.ComponentModel.PropertyDescriptor to sort by.</param>
        /// <param name="direction">One of the System.ComponentModel.ListSortDirection values.</param>
        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Returns the index of the row that has the given System.ComponentModel.PropertyDescriptor.
        /// </summary>
        /// <param name="property">The System.ComponentModel.PropertyDescriptor to search on.</param>
        /// <param name="key">The value of the property parameter to search for.</param>
        /// <returns>The index of the row that has the given System.ComponentModel.PropertyDescriptor.</returns>
        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Removes the System.ComponentModel.PropertyDescriptor from the indexes used for searching.
        /// </summary>
        /// <param name="property">The System.ComponentModel.PropertyDescriptor to remove from the indexes used for searching.</param>
        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Removes any sort applied using System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection).
        /// </summary>
        void IBindingList.RemoveSort()
        {
            throw new NotImplementedException();
        }
        #endregion
        #region IList
        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        object IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <summary>
        /// Gets a value indicating whether the System.Collections.IList is read-only.
        /// </summary>
        bool IList.IsReadOnly => throw new NotImplementedException();
        /// <summary>
        /// Gets a value indicating whether the System.Collections.IList has a fixed size.
        /// </summary>
        bool IList.IsFixedSize => throw new NotImplementedException();
        /// <summary>
        /// Adds an item to the System.Collections.IList.
        /// </summary>
        /// <param name="value">The object to add to the System.Collections.IList.</param>
        /// <returns>The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.</returns>
        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Removes all items from the System.Collections.IList.
        /// </summary>
        void IList.Clear()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Determines whether the System.Collections.IList contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the System.Collections.IList.</param>
        /// <returns>true if the System.Object is found in the System.Collections.IList; otherwise, false.</returns>
        bool IList.Contains(object value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Determines the index of a specific item in the System.Collections.IList.
        /// </summary>
        /// <param name="value">The object to locate in the System.Collections.IList.</param>
        /// <returns>The index of value if found in the list; otherwise, -1.</returns>
        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Inserts an item to the System.Collections.IList at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The object to insert into the System.Collections.IList.</param>
        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Removes the first occurrence of a specific object from the System.Collections.IList.
        /// </summary>
        /// <param name="value">The object to remove from the System.Collections.IList.</param>
        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Removes the System.Collections.IList item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region ICollection


        int ICollection.Count => throw new NotImplementedException();

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => throw new NotImplementedException();


        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}


// nepoužito:
namespace Noris.Clients.Win.Components
{
    using System.IO;
    using System.Text;
    using System.Data;
    using System.Web.UI;
    using System.Web.UI.HtmlControls;            // System.Web.dll

    public static class Extensions
    {
        public static String GetHtml(this DataTable dataTable)
        {
            StringBuilder sbControlHtml = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter())
            {
                using (HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter))
                {
                    using (var htmlTable = new HtmlTable())
                    {
                        // Add table header row  
                        using (var headerRow = new HtmlTableRow())
                        {
                            foreach (DataColumn dataColumn in dataTable.Columns)
                            {
                                using (var htmlColumn = new HtmlTableCell())
                                {
                                    htmlColumn.InnerText = dataColumn.ColumnName;
                                    headerRow.Cells.Add(htmlColumn);
                                }
                            }
                            htmlTable.Rows.Add(headerRow);
                        }
                        // Add data rows  
                        foreach (DataRow row in dataTable.Rows)
                        {
                            using (var htmlRow = new HtmlTableRow())
                            {
                                foreach (DataColumn column in dataTable.Columns)
                                {
                                    using (var htmlColumn = new HtmlTableCell())
                                    {
                                        htmlColumn.InnerText = row[column].ToString();
                                        htmlRow.Cells.Add(htmlColumn);
                                    }
                                }
                                htmlTable.Rows.Add(htmlRow);
                            }
                        }
                        htmlTable.RenderControl(htmlWriter);
                        sbControlHtml.Append(stringWriter.ToString());
                    }
                }
            }
            return sbControlHtml.ToString();
        }
    }
}  
