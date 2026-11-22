using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DjSoft.Tools.SDCardTester.Workers;

namespace DjSoft.Tools.SDCardTester
{
    public partial class MainForm : Form
    {
        #region Konstrukce a inicializace
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            ToolbarInitialize();
            StatusBarInitialize();
            InitializeProgress();
            InitContent();
            _VisualMapInitialize();
            ShowProperties();
            InitEvents();
            ResumeLayouts();
            ShowControls(ActionState.Dialog, true);
            _PlacementRestore();
        }
        private void ResumeLayouts()
        {
            this.ToolBar.ResumeLayout(false);
            this.ToolBar.PerformLayout();
            this.UserPanel.ResumeLayout(false);
            this.ResultsInfoPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private void InitContent()
        {
            this.InitDrives();
        }
        private void InitEvents()
        {
            this.LinearMapControl.ActiveItemChanged += _VisualPanel_ActiveItemChanged;
            this.ClientSizeChanged += _ClientSizeChanged;
            this.DoLayout();
        }
        /// <summary>
        /// Toolbar font typu Bold
        /// </summary>
        private Font _ToolFontBold
        {
            get
            {
                if (__ToolFontBold is null)
                    __ToolFontBold = new Font(this.ToolDriveTypeButton.Font, FontStyle.Bold);
                return __ToolFontBold;
            }
        }
        private Font __ToolFontBold;
        /// <summary>
        /// Toolbar font typu Regular
        /// </summary>
        private Font _ToolFontRegular
        {
            get
            {
                if (__ToolFontRegular is null)
                    __ToolFontRegular = new Font(this.ToolDriveTypeButton.Font, FontStyle.Regular);
                return __ToolFontRegular;
            }
        }
        private Font __ToolFontRegular;
        #endregion
        #region Toolbar: vytvoření, základní obsluha událostí
        /// <summary>
        /// Vytvoří obsah Toolbaru
        /// </summary>
        protected void ToolbarInitialize()
        {
            this.ToolBar.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.ToolBar.Location = new System.Drawing.Point(0, 0);
            this.ToolBar.Name = "ToolBar";
            this.ToolBar.Size = new System.Drawing.Size(1015, 39);
            this.ToolBar.TabIndex = 2;
            this.ToolBar.Text = "";

            createLabel(this.ToolBar.Items, "Disk:", 38, out var _);
            createCombo(this.ToolBar.Items, ToolDriveCombo_SelectedIndexChanged, 300, out ToolDriveCombo, "");

            createDropDownButton(this.ToolBar.Items, "Nabízené typy disků", Properties.Resources.drive_removable_media_usb_pendrive_48, 45, out ToolDriveTypeButton, "Volba nabídky disků: výměnné nebo všechny?");
            createMenuItem(ToolDriveTypeButton.DropDownItems, "Jen vyměnitelné disky", Properties.Resources.drive_removable_media_usb_pendrive_32, ToolDriveTypeFlashButton_Click, 205, out ToolDriveTypeFlashButton, "Jen vyměnitelné disky");
            createMenuItem(ToolDriveTypeButton.DropDownItems, "Všechny disky", Properties.Resources.drive_raid_32, ToolDriveTypeAllButton_Click, 205, out ToolDriveTypeAllButton, "Všechny disky");

            createButton(this.ToolBar.Items, "Refresh", Properties.Resources.view_refresh_4_48, ToolDriveRefreshButton_Click, 36, out ToolDriveRefreshButton, "Refresh a zobrazení základních dat o disku v panelu vlevo");
            
            createSeparator(this.ToolBar.Items, out var _);
            createButton(this.ToolBar.Items, "Analýza", Properties.Resources.view_statistics_48, ToolActionAnalyseButton_Click, 84, out ToolActionAnalyseButton, "Analýza obsahu disku z hlediska typu dat");
            createButton(this.ToolBar.Items, "Záchrana", Properties.Resources.help_2_48, ToolActionRescueButton_Click, 84, out ToolActionRescueButton, "Pokusí se z jednoho souboru zachránit, co se dá");

            createSeparator(this.ToolBar.Items, out var _);
            createButton(this.ToolBar.Items, "Čitelnost", Properties.Resources.system_search_4_48, ToolActionReadAnyButton_Click, 90, out ToolActionReadAnyButton, "Prověří čitelnost každého souboru, nejen testovacího");
            createButton(this.ToolBar.Items, "Zápis", Properties.Resources.document_import_48, ToolActionWriteDataButton_Click, 88, out ToolActionWriteDataButton, "Zápis testovacích dat na disk");
            createButton(this.ToolBar.Items, "Kontrola", Properties.Resources.document_preview_48, ToolActionReadDataButton_Click, 88, out ToolActionReadDataButton, "Kontrola testovacích dat - shodnost obsahu");

            createSeparator(this.ToolBar.Items, out ToolFlowControlSeparator);
            createButton(this.ToolBar.Items, "Run", Properties.Resources.media_playback_start_3_48, ToolFlowControlRunButton_Click, 36, out ToolFlowControlRunButton, "Pokračuje v pozastavené akci");
            createButton(this.ToolBar.Items, "Pauza", Properties.Resources.media_playback_pause_3_48, ToolFlowControlPauseButton_Click, 36, out ToolFlowControlPauseButton, "Pozastaví běžící akci, bude možno v ní pokračovat");
            createButton(this.ToolBar.Items, "Stop", Properties.Resources.media_playback_stop_3_48, ToolFlowControlStopButton_Click, 36, out ToolFlowControlStopButton, "Zruší běžící akci a vrátí okno do výchozího stavu");

            void createSeparator(ToolStripItemCollection items, out ToolStripSeparator item)
            {
                item = new ToolStripSeparator()
                {
                };
                items?.Add(item);
            }
            void createLabel(ToolStripItemCollection items, string text, int width, out ToolStripLabel item)
            {
                item = new ToolStripLabel()
                {
                    Text = text,
                    Size = new Size(width, 36)
                };
                items?.Add(item);
            }
            void createButton(ToolStripItemCollection items, string text, Image image, EventHandler click, int width, out ToolStripButton item, string toolTip)
            {
                item = new ToolStripButton()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Text = text,
                    Image = image,
                    ImageTransparentColor = System.Drawing.Color.Magenta,
                    ToolTipText = toolTip,
                    Size = new Size(width, 36)
                };
                item.Click += click;
                items?.Add(item);
            }
            void createDropDownButton(ToolStripItemCollection items, string text, Image image, int width, out ToolStripDropDownButton item, string toolTip)
            {
                item = new ToolStripDropDownButton()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Text = text,
                    Image = image,
                    ImageTransparentColor = System.Drawing.Color.Magenta,
                    ToolTipText = toolTip,
                    Size = new Size(width, 36)
                };
                items?.Add(item);
            }
            void createCombo(ToolStripItemCollection items, EventHandler indexChanged, int width, out ToolStripComboBox item, string toolTip)
            {
                item = new ToolStripComboBox()
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    ToolTipText = toolTip,
                    Size = new Size(width, 39)
                };
                item.SelectedIndexChanged += indexChanged;
                items?.Add(item);
            }
            void createMenuItem(ToolStripItemCollection items, string text, Image image, EventHandler click, int width, out ToolStripMenuItem item, string toolTip)
            {
                item = new ToolStripMenuItem()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                    Text = text,
                    Image = image,
                    ImageTransparentColor = System.Drawing.Color.Magenta,
                    ToolTipText = toolTip,
                    Size = new Size(width, 36)
                };
                item.Click += click;
                items?.Add(item);
            }
        }
        #region Obsluha Toolbaru - eventhandlery: volba Drive a jeho Refresh, DriveType
        /// <summary>
        /// Inicializace dat pro zobrazení disků
        /// </summary>
        private void InitDrives()
        {
            this._SetIsOnlyRemovable(true, false, false);
            int driveCount = FillDrives();
            if (driveCount == 0)
            {
                this._SetIsOnlyRemovable(false, false, false);
                driveCount = FillDrives();
            }
            this._SetIsOnlyRemovable(this.IsOnlyRemovable, true, true);
        }
        /// <summary>
        /// Jsou zobrazeny pouze vyjímatelné disky?
        /// </summary>
        public bool IsOnlyRemovable
        {
            get { return __IsOnlyRemovable; }
            set { _SetIsOnlyRemovable(value, true, true); }
        }
        /// <summary>
        /// Nastaví hodnotu <see cref="IsOnlyRemovable"/> a promítne ji volitelně do Image v buttonu a do obsahu Combo
        /// </summary>
        /// <param name="isOnlyRemovable"></param>
        /// <param name="refreshDriveTypeButton"></param>
        /// <param name="runFillDrives"></param>
        private void _SetIsOnlyRemovable(bool isOnlyRemovable, bool refreshDriveTypeButton, bool runFillDrives)
        {
            __IsOnlyRemovable = isOnlyRemovable;
            if (refreshDriveTypeButton)
            {
                this.ToolDriveTypeButton.Image = (isOnlyRemovable ? _ImageDriveTypeFlash : _ImageDriveTypeAll);
                this.ToolDriveTypeButton.ToolTipText = (isOnlyRemovable ? "Nabízí se pouze vyměnitelné jednotky." : "Nabízí se všechny dostupné jednotky.");
                this.ToolDriveTypeFlashButton.Font = (isOnlyRemovable ? _ToolFontBold : _ToolFontRegular);
                this.ToolDriveTypeAllButton.Font = (!isOnlyRemovable ? _ToolFontBold : _ToolFontRegular);
            }
            if (runFillDrives)
                FillDrives();
        }
        /// <summary>
        /// Po změně vybraného disku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolDriveCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowControls(ActionState.Dialog, true);
            ShowProperties();
        }
        /// <summary>
        /// Po kliknutí na Toolbar "Zobrazit pouze vyměnitelné disky"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolDriveTypeFlashButton_Click(object sender, EventArgs e)
        {
            ShowControls(ActionState.Dialog, true);
            this.IsOnlyRemovable = true;
        }
        /// <summary>
        /// Po kliknutí na Toolbar "Zobrazit všechny typy disků"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolDriveTypeAllButton_Click(object sender, EventArgs e)
        {
            ShowControls(ActionState.Dialog, true);
            this.IsOnlyRemovable = false;
        }
        /// <summary>
        /// Po kliknutí na toolbar "Refresh disků"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolDriveRefreshButton_Click(object sender, EventArgs e)
        {
            FillDrives();
            //  ShowProperties();
        }
        /// <summary>
        /// Kliknutí na button Analýza stavu disku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolActionAnalyseButton_Click(object sender, EventArgs e)
        {
            RunDriveAnalyse();
        }
        /// <summary>
        /// Kliknutí na button Záchrana souboru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolActionRescueButton_Click(object sender, EventArgs e)
        {
            this.RunFileRescue();
        }
        /// <summary>
        /// Požadavek na start zápisu testovacích dat na disk
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolActionWriteDataButton_Click(object sender, EventArgs e)
        {
            RunDriveTest(ActionState.TestSave);
        }
        /// <summary>
        /// Požadavek na start čtení testovacích dat z disku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolActionReadDataButton_Click(object sender, EventArgs e)
        {
            RunDriveTest(ActionState.TestRead);
        }
        /// <summary>
        /// Požadavek na start čtení jakýchkoli dat z disku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolActionReadAnyButton_Click(object sender, EventArgs e)
        {
            RunDriveTest(ActionState.ContentRead);
        }

        /// <summary>Image odpovídající typu disku Only Removable</summary>
        private Image _ImageDriveTypeFlash { get { return this.ToolDriveTypeFlashButton.Image; } }
        /// <summary>Image odpovídající typu disku All drives</summary>
        private Image _ImageDriveTypeAll { get { return this.ToolDriveTypeAllButton.Image; } }
        /// <summary>Hodnota pro <see cref="IsOnlyRemovable"/></summary>
        private bool __IsOnlyRemovable;
        #endregion
        #region Proměnné pro ToolBarItemy
        private System.Windows.Forms.ToolStrip ToolBar;
        private System.Windows.Forms.ToolStripComboBox ToolDriveCombo;
        private System.Windows.Forms.ToolStripDropDownButton ToolDriveTypeButton;
        private System.Windows.Forms.ToolStripMenuItem ToolDriveTypeFlashButton;
        private System.Windows.Forms.ToolStripMenuItem ToolDriveTypeAllButton;
        private System.Windows.Forms.ToolStripButton ToolDriveRefreshButton;
        private System.Windows.Forms.ToolStripButton ToolActionAnalyseButton;
        private System.Windows.Forms.ToolStripButton ToolActionRescueButton;
        private System.Windows.Forms.ToolStripButton ToolActionWriteDataButton;
        private System.Windows.Forms.ToolStripButton ToolActionReadDataButton;
        private System.Windows.Forms.ToolStripButton ToolActionReadAnyButton;
        private System.Windows.Forms.ToolStripSeparator ToolFlowControlSeparator;
        private System.Windows.Forms.ToolStripButton ToolFlowControlRunButton;
        private System.Windows.Forms.ToolStripButton ToolFlowControlPauseButton;
        private System.Windows.Forms.ToolStripButton ToolFlowControlStopButton;

        #endregion
        #endregion
        #region StatusBar: vytvoření, datum buildu
        /// <summary>
        /// Vytvoří StatusBar dole na okně, obsahuje informaci o datu buildu aplikace
        /// </summary>
        protected void StatusBarInitialize()
        {
            this.StatusBar = new System.Windows.Forms.StatusStrip()
            {
                Name = "StatusBar",
                Dock = DockStyle.Bottom,
                SizingGrip = true,
                ShowItemToolTips = true
            };

            // Prázdný pružný label vlevo odsune informaci o buildu doprava:
            var springLabel = new ToolStripStatusLabel() { Spring = true, Text = "" };
            this.StatusBar.Items.Add(springLabel);

            DateTime? buildDate = _GetBuildDate();
            string version = typeof(MainForm).Assembly.GetName().Version.ToString();
            this.StatusBuildLabel = new ToolStripStatusLabel()
            {
                Name = "StatusBuildLabel",
                Text = (buildDate.HasValue ? $"Build: {buildDate.Value:dd.MM.yyyy HH:mm}" : "Build: neznámý"),
                ToolTipText = $"Datum a čas sestavení aplikace{Environment.NewLine}Verze: {version}"
            };
            this.StatusBar.Items.Add(this.StatusBuildLabel);

            // Přidání na konec kolekce Controls: dokuje se jako první, proto zabere celou šířku okna pod levým panelem i mapou
            this.Controls.Add(this.StatusBar);
        }
        /// <summary>
        /// Vrátí datum a čas buildu, vložený při sestavení do atributu <see cref="System.Reflection.AssemblyMetadataAttribute"/> s klíčem "BuildDate".
        /// Pokud atribut chybí nebo nejde přečíst, vrací null.
        /// </summary>
        /// <returns></returns>
        private static DateTime? _GetBuildDate()
        {
            try
            {
                var attributes = typeof(MainForm).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false);
                foreach (System.Reflection.AssemblyMetadataAttribute attribute in attributes)
                {
                    if (attribute.Key == "BuildDate" && DateTime.TryParseExact(attribute.Value, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var buildDate))
                        return buildDate;
                }
            }
            catch { }
            return null;
        }
        private System.Windows.Forms.StatusStrip StatusBar;
        private System.Windows.Forms.ToolStripStatusLabel StatusBuildLabel;
        #endregion
        #region Umístění okna: uložení při zavření, obnovení při otevření, přizpůsobení aktuálním monitorům
        /// <summary>
        /// Po standardním Load (včetně AutoScale) ověří, že obnovené Normal Bounds nebyly změněny přeškálováním, a případně je vrátí
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _PlacementCheckAfterLoad();
        }
        /// <summary>
        /// Při změně velikosti si pamatujeme poslední stav okna jiný než Minimized
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState != FormWindowState.Minimized)
                __LastNonMinimizedState = this.WindowState;
        }
        /// <summary>
        /// Při zavírání okna (pokud zavírání nikdo nezrušil) uloží jeho umístění do konfigurace
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel)
                _PlacementSave();
        }
        /// <summary>
        /// Uloží umístění okna do <see cref="AppSettings.MainFormPlacement"/>.
        /// Souřadnice jsou vždy Bounds stavu Normal: ve stavu Maximized i Minimized jde o <see cref="Form.RestoreBounds"/>.
        /// Pro zavření ve stavu Minimized se ukládá stav, který okno mělo před minimalizací.
        /// </summary>
        private void _PlacementSave()
        {
            try
            {
                var windowState = this.WindowState;
                var state = (windowState == FormWindowState.Minimized ? __LastNonMinimizedState : windowState);
                var bounds = (windowState == FormWindowState.Normal ? this.Bounds : this.RestoreBounds);
                if (bounds.Width <= 0 || bounds.Height <= 0) return;
                App.Settings.MainFormPlacement = _PlacementFormat(state == FormWindowState.Maximized, bounds);
            }
            catch { }
        }
        /// <summary>
        /// Obnoví umístění okna z <see cref="AppSettings.MainFormPlacement"/>: nastaví Bounds stavu Normal přizpůsobené aktuálním monitorům,
        /// a pokud bylo okno zavřeno jako Maximized, nastaví stav Maximized (Windows si přitom pamatují Normal Bounds pro „Obnovit“).
        /// Volá se na konci konstruktoru, tedy před prvním zobrazením okna: okno se pak rovnou zobrazí ve výsledném stavu bez probliknutí.
        /// Pokud konfigurace dosud neexistuje, ponechá výchozí umístění.
        /// </summary>
        private void _PlacementRestore()
        {
            __PlacementRestoredBounds = null;
            try
            {
                if (!_PlacementParse(App.Settings.MainFormPlacement, out bool isMaximized, out Rectangle bounds)) return;
                var workingAreas = Screen.AllScreens.Select(s => s.WorkingArea).ToArray();
                bounds = _PlacementFitToScreens(bounds, this.MinimumSize, workingAreas);
                this.StartPosition = FormStartPosition.Manual;
                this.WindowState = FormWindowState.Normal;
                this.Bounds = bounds;
                if (isMaximized)
                    this.WindowState = FormWindowState.Maximized;
                __PlacementRestoredBounds = bounds;
            }
            catch { }
        }
        /// <summary>
        /// Pojistka po <see cref="Form.OnLoad(EventArgs)"/>: pokud AutoScale (font, DPI) změnil obnovené Normal Bounds, vrátí je.
        /// Pro stav Normal jde o prosté nastavení Bounds. Pro stav Maximized nelze <see cref="Form.RestoreBounds"/> setovat,
        /// proto se v tom (výjimečném) případě okno krátce přepne do Normal a zpět, což může způsobit probliknutí.
        /// </summary>
        private void _PlacementCheckAfterLoad()
        {
            try
            {
                if (!__PlacementRestoredBounds.HasValue) return;
                var bounds = __PlacementRestoredBounds.Value;
                __PlacementRestoredBounds = null;
                switch (this.WindowState)
                {
                    case FormWindowState.Normal:
                        if (this.Bounds != bounds)
                            this.Bounds = bounds;
                        break;
                    case FormWindowState.Maximized:
                        if (this.RestoreBounds.Size != bounds.Size)
                        {
                            this.WindowState = FormWindowState.Normal;
                            this.Bounds = bounds;
                            this.WindowState = FormWindowState.Maximized;
                        }
                        break;
                }
            }
            catch { }
        }
        /// <summary>
        /// Normal Bounds obnovené z konfigurace v konstruktoru, pro kontrolu po Load. Po kontrole null.
        /// </summary>
        private Rectangle? __PlacementRestoredBounds;
        /// <summary>
        /// Vrátí text umístění okna pro konfiguraci: "Stav;X;Y;Šířka;Výška"
        /// </summary>
        /// <param name="isMaximized"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private static string _PlacementFormat(bool isMaximized, Rectangle bounds)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            return String.Join(";", (isMaximized ? PlacementMaximized : PlacementNormal),
                bounds.X.ToString(ci), bounds.Y.ToString(ci), bounds.Width.ToString(ci), bounds.Height.ToString(ci));
        }
        /// <summary>
        /// Rozloží text umístění okna z konfigurace. Vrací false, pokud text chybí nebo je neplatný.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="isMaximized"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private static bool _PlacementParse(string text, out bool isMaximized, out Rectangle bounds)
        {
            isMaximized = false;
            bounds = Rectangle.Empty;
            if (String.IsNullOrWhiteSpace(text)) return false;

            var parts = text.Split(';');
            if (parts.Length != 5) return false;
            if (parts[0] == PlacementMaximized) isMaximized = true;
            else if (parts[0] != PlacementNormal) return false;

            var values = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (!Int32.TryParse(parts[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out values[i])) return false;
            }
            if (values[2] <= 0 || values[3] <= 0) return false;

            bounds = new Rectangle(values[0], values[1], values[2], values[3]);
            return true;
        }
        /// <summary>
        /// Přizpůsobí Bounds okna aktuálním monitorům tak, aby bylo celé okno vidět na jednom monitoru.
        /// Monitor se vybere jako ten, s jehož pracovní plochou má okno největší průnik; pokud s žádnou nemá průnik, pak nejbližší.
        /// Okno větší než pracovní plocha se zmenší (nejvýše na <paramref name="minimumSize"/>), pak se posune dovnitř pracovní plochy.
        /// Okno, které již celé leží na pracovní ploše jednoho monitoru, zůstane beze změny.
        /// </summary>
        /// <param name="bounds">Bounds okna ve stavu Normal</param>
        /// <param name="minimumSize">Minimální velikost okna</param>
        /// <param name="workingAreas">Pracovní plochy aktuálních monitorů (bez hlavního panelu)</param>
        /// <returns></returns>
        private static Rectangle _PlacementFitToScreens(Rectangle bounds, Size minimumSize, Rectangle[] workingAreas)
        {
            if (workingAreas is null || workingAreas.Length == 0) return bounds;

            // Výběr monitoru: největší průnik, jinak nejmenší vzdálenost:
            Rectangle area = workingAreas[0];
            long bestIntersect = -1L;
            long bestDistance = Int64.MaxValue;
            foreach (var workingArea in workingAreas)
            {
                var intersect = Rectangle.Intersect(bounds, workingArea);
                long intersectSize = (intersect.Width > 0 && intersect.Height > 0 ? (long)intersect.Width * (long)intersect.Height : 0L);
                long distance = getDistance(bounds, workingArea);
                if (intersectSize > bestIntersect || (intersectSize == 0L && bestIntersect == 0L && distance < bestDistance))
                {
                    area = workingArea;
                    bestIntersect = intersectSize;
                    bestDistance = distance;
                }
            }

            // Velikost: nejvýše pracovní plocha, nejméně MinimumSize:
            int width = Math.Max(Math.Min(bounds.Width, area.Width), minimumSize.Width);
            int height = Math.Max(Math.Min(bounds.Height, area.Height), minimumSize.Height);

            // Pozice: celé okno uvnitř pracovní plochy (když se nevejde ani v MinimumSize, pak od levého horního rohu):
            int x = Math.Max(Math.Min(bounds.X, area.Right - width), area.Left);
            int y = Math.Max(Math.Min(bounds.Y, area.Bottom - height), area.Top);

            return new Rectangle(x, y, width, height);

            // Druhá mocnina vzdálenosti dvou obdélníků (0 = dotýkají se nebo se překrývají)
            long getDistance(Rectangle a, Rectangle b)
            {
                long dx = Math.Max(0, Math.Max(b.Left - a.Right, a.Left - b.Right));
                long dy = Math.Max(0, Math.Max(b.Top - a.Bottom, a.Top - b.Bottom));
                return dx * dx + dy * dy;
            }
        }
        /// <summary>
        /// Poslední stav okna jiný než Minimized, pro uložení při zavření z minimalizovaného okna
        /// </summary>
        private FormWindowState __LastNonMinimizedState = FormWindowState.Normal;
        private const string PlacementNormal = "Normal";
        private const string PlacementMaximized = "Maximized";
        #endregion
        #region Layout
        /// <summary>
        /// Změna velikosti upraví Layout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientSizeChanged(object sender, EventArgs e)
        {
            this.DoLayout();
        }
        /// <summary>
        /// Upraví Layout formu
        /// </summary>
        private void DoLayout()
        {
            if (this.UserPanel is null || this.DriveInfoPanel is null || this.ResultsInfoPanel is null) return;

            // Layout se týká pouze levého panelu: this.UserPanel
            // Obsahuje prvky: DriveInfoPanel, ResultsInfoPanel
            var clientSize = this.UserPanel.ClientSize;
            int width = clientSize.Width - 6;
            int height = clientSize.Height - 6;
            int mx = 3;
            int my = 3;
            int x = mx;
            int y = my;

            this.DriveInfoPanel.Bounds = new Rectangle(x, y, width, height);
            this.ResultsInfoPanel.Bounds = new Rectangle(x, y, width, height);
        }
        #endregion
        #region Aktuální stav okna a zobrazení odpovídajících controlů podle tohoto aktuálního stavu
        /// <summary>
        /// Zobrazí controly vhodné pro daný stav okna.
        /// Lze volat z Working threadů.
        /// </summary>
        /// <param name="actionState"></param>
        protected void ShowControls(ActionState actionState, bool withDataPanel)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => action(actionState, withDataPanel)));
            else
                action(actionState, withDataPanel);


            // Akce v GUI threadu
            void action(ActionState state, bool withPanel)
            {
                bool oldStateWorking = isWorkingState(CurrentState);
                bool newStateDialog = (state == ActionState.Dialog);
                bool newStateWorking = isWorkingState(state);

                ToolDriveCombo.Enabled = newStateDialog;
                ToolDriveTypeButton.Enabled = newStateDialog;
                ToolDriveTypeFlashButton.Enabled = newStateDialog;
                ToolDriveTypeAllButton.Enabled = newStateDialog;
                ToolDriveRefreshButton.Enabled = newStateDialog;

                if (withPanel)
                {
                    DriveInfoPanel.Visible = newStateDialog;
                    ResultsInfoPanel.Visible = newStateWorking;
                    CurrentDataPanelState = state;
                }

                this.ToolActionAnalyseButton.Enabled = newStateDialog;
                this.ToolActionRescueButton.Enabled = newStateDialog;
                this.ToolActionReadAnyButton.Enabled = newStateDialog;
                this.ToolActionWriteDataButton.Enabled = newStateDialog;
                this.ToolActionReadDataButton.Enabled = newStateDialog;

                this.ToolFlowControlPauseButton.Visible = newStateWorking;
                this.ToolFlowControlStopButton.Visible = newStateWorking;
                this.ToolFlowControlRunButton.Visible = newStateWorking;
                this.ToolFlowControlSeparator.Visible = newStateWorking;

                // Pokud nyní ZAČÍNÁ stav Working, pak nastavíme RunState na Run = ikonky v Toolbaru:
                if (!oldStateWorking && newStateWorking)
                {
                    //  Jen GUI, bez akcí: setter RunState by volal _SetRunState(withActions: true), a ten podle dosud nezměněného CurrentState (= Dialog)
                    //  zavolal ShowControls(Dialog) => vnořeně by vrátil Toolbar do stavu Dialog (akční buttony Enabled, FlowControl skryté).
                    _SetRunState(RunState.Run, true, false);
                    _TaskProgressState = ThumbnailProgressState.Normal;
                }

                // Pokud nyní KONČÍ stav Working, pak vrátíme titulek okna na standardní:
                if (oldStateWorking && !newStateWorking)
                {
                    this.AppTitleTextCurrent = this.__AppTitleTextStandard;
                    _TaskProgressState = ThumbnailProgressState.NoProgress;
                }

                CurrentState = state;
            }
            // Vrací true, pokud daný stav je nějaký pracovní
            bool isWorkingState(ActionState state)
            {
                return (state == ActionState.AnalyseContent || state == ActionState.TestSave || state == ActionState.TestRead || state == ActionState.ContentRead || state == ActionState.FileRescue);
            }
        }
        /// <summary>
        /// Zobrazí správně Enabled pro buttony skupiny FlowControl pro zadaný stav.
        /// </summary>
        /// <param name="runState"></param>
        protected void ShowFlowButtonsEnabled(RunState runState)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => action(runState)));
            else
                action(runState);


            // Akce v GUI threadu
            void action(RunState state)
            {
                this.ToolFlowControlPauseButton.Enabled = (state == RunState.Run);
                this.ToolFlowControlStopButton.Enabled = (state == RunState.Run || state == RunState.Pause);
                this.ToolFlowControlRunButton.Enabled = (state == RunState.Pause);
            }
        }
        /// <summary>
        /// Aktuální stav okna
        /// </summary>
        protected ActionState CurrentState;
        /// <summary>
        /// Aktuální stav panelu = který datový panel zůstal být vidět po doběhnutí akce
        /// </summary>
        protected ActionState CurrentDataPanelState;
        /// <summary>
        /// Stav okna podle aktuální akce
        /// </summary>
        protected enum ActionState
        {
            /// <summary>
            /// Stav "Dialog"
            /// </summary>
            Dialog,
            /// <summary>
            /// Analyzuje se obsah disku
            /// </summary>
            AnalyseContent,
            /// <summary>
            /// Zapisují se testovací data
            /// </summary>
            TestSave,
            /// <summary>
            /// Čtou se testovací data
            /// </summary>
            TestRead,
            /// <summary>
            /// Čtou se jakákoli data
            /// </summary>
            ContentRead,
            /// <summary>
            /// Zachraňují se data
            /// </summary>
            FileRescue
        }
        /// <summary>
        /// Smaže prvky <see cref="WorkingResultControl"/> z panelu informací <see cref="ResultsInfoPanel"/>
        /// </summary>
        private void ResultsInfoPanelClear()
        {
            for (int i = ResultsInfoPanel.Controls.Count - 1; i >= 0; i--)
            {
                var control = ResultsInfoPanel.Controls[i];
                if (control is WorkingResultControl)
                {
                    ResultsInfoPanel.Controls.RemoveAt(i);
                    control.Dispose();
                }
            }
        }
        #endregion
        #region LinearMapControl: vizualizační panel detailního obsahu
        private void _VisualMapInitialize()
        {
            Skin.Palette = Skin.PaletteType.Light;

            this.LinearMapControl.BackColor = System.Drawing.Color.Snow;
            this.LinearMapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LinearMapControl.Location = new System.Drawing.Point(251, 39);
            this.LinearMapControl.Size = new System.Drawing.Size(764, 740);
            this.LinearMapControl.TabIndex = 1;
        }
        /// <summary>
        /// Po změně aktivního prvku ve vizuálním panelu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VisualPanel_ActiveItemChanged(object sender, EventArgs e)
        {
            switch (CurrentDataPanelState)
            {
                case ActionState.AnalyseContent:
                    AnalyseActiveItemChanged(this.LinearMapControl.ActiveItem);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> načte a vepíše základní informace o daném disku <paramref name="drive"/> (velikost, obsazenost, testovací data).
        /// </summary>
        /// <param name="drive"></param>
        private void _VisualMapPanelFillBasicData(System.IO.DriveInfo drive)
        {
            var fileGroups = DriveAnalyser.GetFileGroupsForDrive(drive, DriveAnalyser.AnalyseCriteriaType.Default, out long totalSize);
            _VisualMapFillFromItems(fileGroups, totalSize);
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky mapy dle <paramref name="mapItems"/>. Volitelně nastaví <paramref name="totalSize"/>.
        /// </summary>
        /// <param name="mapItems"></param>
        private void _VisualMapFillFromItems(IEnumerable<ILinearMapControlItem> mapItems, long? totalSize = null)
        {
            if (totalSize.HasValue)
                _VisualMapPanelSetupHeight(totalSize.Value, false);

            this.LinearMapControl.MapItems = mapItems;
            this.LinearMapControl.Refresh();
        }
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky popisující stav obsazení disku podle dodaných <see cref="DriveAnalyser.FileGroup"/>.
        /// </summary>
        /// <param name="fileGroups"></param>
        private void _VisualMapPanelSetupHeight(long totalSize, bool refresh)
        {
            this.LinearMapControl.LineHeight = GetLineHeight(totalSize);
            this.LinearMapControl.TotalLength = totalSize;

            if (refresh)
                this.LinearMapControl.Refresh();
        }
        /// <summary>
        /// Vrátí výšku jedné vizuální linky pro danou velikost disku: menší disk = vyšší linky, velký disk = malé linky (víc se tam toho vejde)
        /// </summary>
        /// <param name="totalSize"></param>
        /// <returns></returns>
        protected int GetLineHeight(long totalSize)
        {
            long sizeMB = totalSize / 1048576L;
            long sizeGB = sizeMB / 1024L;
            long sizeTB = sizeGB / 1024L;
            if (sizeMB <= 256L) return 24;                 // Obstarožní média
            if (sizeMB <= 512L) return 22;
            if (sizeMB <= 1024L) return 21;
            if (sizeGB <= 2L) return 20;                   // SD karty
            if (sizeGB <= 4L) return 19;
            if (sizeGB <= 8L) return 18;
            if (sizeGB <= 16L) return 17;
            if (sizeGB <= 32L) return 16;
            if (sizeGB <= 64L) return 15;
            if (sizeGB <= 128L) return 14;                 // SSD disky
            if (sizeGB <= 256L) return 13;
            if (sizeGB <= 512L) return 12;
            if (sizeGB <= 1024L) return 11;
            if (sizeTB <= 2L) return 10;                   // Velkoplotnové disky
            if (sizeTB <= 4L) return 9;
            if (sizeTB <= 8L) return 8;
            if (sizeTB <= 16L) return 7;
            if (sizeTB <= 32L) return 6;
            return 5;
        }
        #endregion
        #region Načtení a zobrazení seznamu Drives a detailních vlastností o zvoleném disku, včetně mapy
        /// <summary>
        /// Aktuálně vybraný disk
        /// </summary>
        public System.IO.DriveInfo SelectedDrive
        {
            get
            {
                TextDataInfo selectedItem = this.ToolDriveCombo.SelectedItem as TextDataInfo;
                return selectedItem?.Data as System.IO.DriveInfo;
            }
            set
            {
                string name = value?.Name;
                object selectedItem = null;
                foreach (var item in this.ToolDriveCombo.Items)
                {
                    if (item is TextDataInfo info && info.Data is System.IO.DriveInfo driveInfo && String.Equals(driveInfo.Name, name))
                    {
                        selectedItem = item;
                        break;
                    }
                }
                this.ToolDriveCombo.SelectedItem = selectedItem;
            }
        }
        /// <summary>
        /// Naplní seznam Drives
        /// </summary>
        /// <returns></returns>
        private int FillDrives()
        {
            int driveCount = 0;
            bool onlyRemovable = this.IsOnlyRemovable;
            decimal oneKB = 1024m;
            decimal oneMB = oneKB * oneKB;
            decimal oneGB = oneKB * oneKB * oneKB;
            string selectedName = SelectedDrive?.Name;
            TextDataInfo selectedItem = null;

            var driveCombo = this.ToolDriveCombo;
            driveCombo.Items.Clear();
            var drives = System.IO.DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (!drive.IsReady) continue;
                if (onlyRemovable && drive.DriveType != System.IO.DriveType.Removable) continue;

                string name = drive.Name;
                string label = drive.VolumeLabel;
                string total = Math.Round((decimal)drive.TotalSize / oneGB, 1).ToString("### ##0.0") + " GB";
                string text = $"{name}    '{label}'    [{total}]";
                TextDataInfo info = new TextDataInfo() { Text = text, Data = drive };
                driveCombo.Items.Add(info);
                if (selectedItem is null || String.Equals(drive.Name, selectedName))
                    selectedItem = info;
                driveCount++;
            }
            driveCombo.SelectedItem = selectedItem;
            return driveCount;
        }
        /// <summary>
        /// Načte a zobrazí vlastnosti aktuálně vybraného <see cref="SelectedDrive"/>, provede aktuální načtení dat
        /// </summary>
        private void ShowProperties()
        {
            var selectedDrive = SelectedDrive;
            if (selectedDrive != null) selectedDrive = new System.IO.DriveInfo(selectedDrive.Name);     // = refresh

            this.DriveInfoPanel.ShowProperties(selectedDrive);
            _VisualMapPanelFillBasicData(selectedDrive);
        }
        #endregion
        #region Akce: Analýza obsahu disku
        /// <summary>
        /// Požadavek na start analýzy
        /// </summary>
        private void RunDriveAnalyse()
        {
            ResultsInfoPanelClear();
            ShowControls(ActionState.AnalyseContent, true);

            DriveAnalyser driveAnalyser = new DriveAnalyser();
            _AnalyseSummaryPanelPrepare(driveAnalyser);
            driveAnalyser.WorkingStep += DriveAnalyser_AnalyseStep;
            driveAnalyser.WorkingDone += _DriveAnalyser_AnalyseDone;
            _DriveAnalyser = driveAnalyser;
            driveAnalyser.Start(this.SelectedDrive);
        }
        /// <summary>
        /// Požadavek na zastavení analýzy
        /// </summary>
        private void RunPauseStopDriveAnalyse(RunState state)
        {
            if (_DriveAnalyser != null)
                _DriveAnalyser?.ChangeState(state);
            else
                ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Analyzer provedl krok analýzy a volá refresh dat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriveAnalyser_AnalyseStep(object sender, EventArgs e)
        {
            _DriveAnalyserRefresh();
        }
        /// <summary>
        /// Analyzer skončil svoji činnost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DriveAnalyser_AnalyseDone(object sender, EventArgs e)
        {
            _DriveAnalyserRefresh();
            _DriveAnalyser = null;
            // grupy si ponechám:   _DriveAnalyserGroups = null;
            ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Zajistí refresh dat po jednom každém postupném kroku analyzeru
        /// </summary>
        private void _DriveAnalyserRefresh()
        {
            var driveAnalyser = _DriveAnalyser;
            if (driveAnalyser is null) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => action(driveAnalyser)));
            else
                action(driveAnalyser);

            // Akce v GUI threadu
            void action(DriveAnalyser analyser)
            {
                _ResultsInfoPanelFillFromAnalyser(analyser);
                _VisualMapFillFromItems(driveAnalyser.FileGroups);
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> vygeneruje prvky <see cref="DriveAnalyseGroupControl"/> pro zobrazování dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void _AnalyseSummaryPanelPrepare(DriveAnalyser driveAnalyser)
        {
            var analyserGroups = new List<Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>>();
            var groups = driveAnalyser.FileGroups;
            int x = 3;
            int y = 3;
            int w = ResultsInfoPanel.ClientRectangle.Width - 6;
            int h = DriveAnalyseGroupControl.OptimalHeight;
            foreach (var group in groups)
            {
                var panel = new DriveAnalyseGroupControl(group.Name, group.Color);
                panel.Bounds = new Rectangle(x, y, w, h);
                ResultsInfoPanel.Controls.Add(panel);
                analyserGroups.Add(new Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>(group, panel));
                y += panel.Height;
            }
            _DriveAnalyserGroups = analyserGroups;
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> aktualizuje hodnoty v controlech <see cref="DriveAnalyseGroupControl"/> z dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void _ResultsInfoPanelFillFromAnalyser(DriveAnalyser driveAnalyser)
        {
            var analyserGroups = _DriveAnalyserGroups;
            if (analyserGroups is null || analyserGroups.Count == 0) return;
            long totalSize = analyserGroups.Sum(g => g.Item1.SizeTotal);
            foreach (var analyserGroup in analyserGroups)
            {
                long currentSize = analyserGroup.Item1.SizeTotal;
                analyserGroup.Item2.GroupRatio = (totalSize > 0L ? (double)currentSize / (double)totalSize : 0d);
                analyserGroup.Item2.Refresh();
            }
        }
        /// <summary>
        /// Volá se po aktivaci daného (nebo žádného) prvku - pohybem myší nad mapou.
        /// Vstupní prvek je <see cref="ILinearMapControlItem"/> = prvek, který byl vložen do vizuální mapy do <see cref="LinearMapControl.MapItems"/>. 
        /// Tento prvek byl vytvořen v metodě <see cref="_VisualMapFillFromItems(IEnumerable{ILinearMapControlItem}, long?)"/>, 
        /// a mělo by jít o ve své property <see cref="LinearMapControl.Item.Data"/> by měl obsahovat grupu z analyzeru <see cref="DriveAnalyser.FileGroup"/>.
        /// Tutéž grupu najdeme v panelu <see cref="ResultsInfoPanel"/> = tam se zobrazují jednotlivé skupiny souborů a jejich sumární hodnota, a tam bych rád zvýraznil grupu, která je aktivní v mapě.
        /// Grupu najdu v prvku v poli <see cref="_DriveAnalyserGroups"/>, kde je umístěna v Item1. 
        /// Tam pak jako párovou Item2 najdu instanci vizuálního controlu <see cref="DriveAnalyseGroupControl"/>.
        /// </summary>
        /// <param name="activeItem"></param>
        private void AnalyseActiveItemChanged(ILinearMapControlItem activeItem)
        {
            var activeGroup = activeItem as DriveAnalyser.FileGroup;
            var lastActivePanel = _DriveAnalyserGroups?.FirstOrDefault(t => t.Item2.IsActive)?.Item2;
            var currActivePanel = _DriveAnalyserGroups?.FirstOrDefault(t => Object.ReferenceEquals(t.Item1, activeGroup))?.Item2;

            if (lastActivePanel != null && (currActivePanel is null || (!Object.ReferenceEquals(lastActivePanel, currActivePanel))))
            {
                lastActivePanel.IsActive = false;
                lastActivePanel.Refresh();
                lastActivePanel = null;
            }

            if (currActivePanel != null && (lastActivePanel is null || (!Object.ReferenceEquals(currActivePanel, lastActivePanel))))
            {
                currActivePanel.IsActive = true;
                currActivePanel.Refresh();
            }
        }
        /// <summary>
        /// Instance analyzeru, po doběhnutí se nuluje
        /// </summary>
        private DriveAnalyser _DriveAnalyser;
        /// <summary>
        /// Instance analyzovaných skupin, nenuluje se po doběhnutí
        /// </summary>
        private List<Tuple<DriveAnalyser.FileGroup, DriveAnalyseGroupControl>> _DriveAnalyserGroups;
        #endregion
        #region Akce: Záchrana souboru
        private void RunFileRescue()
        {
            var inputInfo = _GetFileRescueInfo();
            if (inputInfo is null) return;
            RunFileRescue(inputInfo);
        }
        private void RunFileRescue(FileRescueInputInfo inputInfo)
        {
            ResultsInfoPanelClear();
            ShowControls(ActionState.FileRescue, true);
            _TaskProgressValue = 0;                             // TaskBar nesmí ukazovat hodnotu z předešlé akce

            var fileRescuer = new FileRescue();
            _FileRescueSummaryPanelPrepare();
            fileRescuer.WorkingStep += _FileRescuer_WorkingStep;
            fileRescuer.WorkingDone += _FileRescuer_WorkingDone;
            _FileRescuer = fileRescuer;

            /*
            var inputInfo = new FileRescueInputInfo()
            {
                InputFileNames = @"f:\diskF\SdCards\Koralína a svět za tajemnými dveřmi.ts1",
                OutputFilesPath = @"G:\Cdisk\FileRescueTestTarget",
                TestAddBadBlocks = true,
                TestAddSimulatedWaiting = true
            };
            */


            fileRescuer.Start(inputInfo);
        }
        private FileRescueInputInfo _GetFileRescueInfo()
        {
            var rescueInfo = new FileRescueInputInfo();
            rescueInfo.InputFileNames = App.Settings.LastFileRescueInputFileNames;
            rescueInfo.OutputFilesPath = App.Settings.LastFileRescueOutputFilesPath;
            using (var form = new FileRescueInputWindow())
            {
                form.InputData = rescueInfo;
                if (form.ShowDialog() == DialogResult.OK)
                {
                    rescueInfo = form.InputData;
                    App.Settings.LastFileRescueInputFileNames = rescueInfo.InputFileNames;
                    App.Settings.LastFileRescueOutputFilesPath = rescueInfo.OutputFilesPath;
                    return rescueInfo;
                }
                else
                    return null;
            }
        }
        /// <summary>
        /// Požadavek na zastavení běhu
        /// </summary>
        private void RunPauseStopFileRescue(RunState state)
        {
            if (_FileRescuer != null)
            {
                _FileRescuer?.ChangeState(state);
                _FileRescuerRefresh();                          // Promítne Pause / Run do TaskBaru okamžitě; v Pause nechodí WorkingStep
            }
            else
                ShowControls(ActionState.Dialog, false);
        }

        private void _FileRescuer_WorkingStep(object sender, EventArgs e)
        {
            _FileRescuerRefresh();
        }
        private void _FileRescuer_WorkingDone(object sender, EventArgs e)
        {
            _FileRescuerRefresh();
            _FileRescuer = null;
            ShowControls(ActionState.Dialog, false);            // false = panel výsledků zůstane zobrazen i po skončení akce (stejně jako u Analýzy a testů)
        }
        private void _FileRescuerRefresh()
        {
            var fileRescuer = _FileRescuer;
            if (fileRescuer is null) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => action(fileRescuer)));
            else
                action(fileRescuer);


            // Akce v GUI threadu
            //  Probíhá asynchronně (BeginInvoke): než se provede, thread Work už může být v jiném stavu, i na jiném souboru.
            //  Proto se celková délka nastavuje vždy, spolu s bloky, a obojí z jednoho konzistentního snímku.
            void action(FileRescue rescuer)
            {
                if (rescuer is null) return;
                var summary = rescuer.GetSummary();
                var status = rescuer.GetCurrentProgress(out long fileLength, out var blocks);
                var speedRange = FileRescue.SpeedShading.Apply(blocks, App.Settings.FileRescueSpeedShading);   // Rychlost a odstíny do bloků, rozsah pro legendu
                if (_FileRescueFileControl != null) _FileRescueFileControl.SpeedRange = speedRange;
                _FileRescueSummaryPanelFill(summary);
                _FileRescueTaskProgressFill(summary);
                if (status == FileRescue.FileStatus.None) return;
                _VisualMapFillFromItems(blocks, fileLength);
            }
        }
        /// <summary>
        /// Do panelu informací <see cref="ResultsInfoPanel"/> vloží controly Summary bloku záchrany souborů
        /// </summary>
        private void _FileRescueSummaryPanelPrepare()
        {
            int x = 3;
            int y = 3;
            int w = ResultsInfoPanel.ClientRectangle.Width - 6;

            var totalControl = new FileRescueTotalControl();
            totalControl.Bounds = new Rectangle(x, y, w, FileRescueTotalControl.OptimalHeight);
            ResultsInfoPanel.Controls.Add(totalControl);
            y += totalControl.Height + 3;

            var fileControl = new FileRescueFileControl();
            fileControl.Bounds = new Rectangle(x, y, w, FileRescueFileControl.OptimalHeight);
            fileControl.SpeedShadingToggled += _FileRescueSpeedShadingToggled;
            ResultsInfoPanel.Controls.Add(fileControl);
            y += fileControl.Height + 3;

            var healthControl = new MediaHealthControl();
            healthControl.Bounds = new Rectangle(x, y, w, MediaHealthControl.OptimalHeight);
            ResultsInfoPanel.Controls.Add(healthControl);

            _FileRescueTotalControl = totalControl;
            _FileRescueFileControl = fileControl;
            _FileRescueHealthControl = healthControl;
        }
        /// <summary>
        /// Uživatel přepnul "Barvit podle rychlosti": uloží volbu do nastavení a ihned přebarví bloky aktuálně zobrazené v mapě.
        /// Funguje i po skončení záchrany (mapa i Summary zůstávají zobrazeny).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FileRescueSpeedShadingToggled(object sender, EventArgs e)
        {
            bool enabled = !App.Settings.FileRescueSpeedShading;
            App.Settings.FileRescueSpeedShading = enabled;

            var blocks = this.LinearMapControl.MapItems?.OfType<FileRescue.SingleBlockInfo>().ToArray();
            var speedRange = FileRescue.SpeedShading.Apply(blocks, enabled);
            var fileControl = sender as FileRescueFileControl ?? _FileRescueFileControl;
            if (fileControl != null)
            {
                fileControl.SpeedRange = speedRange;
                fileControl.Refresh();
            }
            this.LinearMapControl.Refresh();
        }
        /// <summary>
        /// Do controlů Summary bloku záchrany souborů vloží aktuální data
        /// </summary>
        /// <param name="summary"></param>
        private void _FileRescueSummaryPanelFill(FileRescue.SummaryInfo summary)
        {
            _FileRescueTotalControl?.StoreInfo(summary);
            _FileRescueFileControl?.StoreInfo(summary);
            _FileRescueHealthControl?.StoreInfo(summary);
        }
        /// <summary>
        /// Celkový postup záchrany promítne do ProgressBaru v TaskBaru Windows a do titulku okna.
        /// Hodnota = stejný poměr jako celkový progress v Summary bloku (<see cref="FileRescue.SummaryInfo.ProcessedRatio"/>).
        /// Stav: Paused při pauze, Error pokud nějaký soubor (i aktuální) má nečitelné bloky nebo selhal, jinak Normal.
        /// Po skončení akce vrací TaskBar do stavu NoProgress a titulek na standardní metoda <see cref="ShowControls(ActionState, bool)"/>.
        /// </summary>
        /// <param name="summary"></param>
        private void _FileRescueTaskProgressFill(FileRescue.SummaryInfo summary)
        {
            if (summary is null || summary.IsFinished || CurrentState != ActionState.FileRescue) return;

            double ratio = summary.ProcessedRatio;
            int value = (int)Math.Round(ratio * (double)_TaskProgressMaximum, 0);
            if (value == 0 && ratio > 0d) value = 1;
            _TaskProgressValue = value;

            bool hasError = (summary.FilesWithErrors > 0 || summary.FilesFailed > 0 || summary.FilesDeferred > 0 || summary.SourceNotRespondingSeconds.HasValue || (summary.CurrentStatistic?.BadBlocksCount ?? 0) > 0);
            _TaskProgressState = (this.RunState == RunState.Pause ? ThumbnailProgressState.Paused :
                                 (hasError ? ThumbnailProgressState.Error : ThumbnailProgressState.Normal));

            // TaskBar: Titulek aplikace = "42% File Rescue"
            //  Procenta na začátku: Windows při více oknech v TaskBaru text zkracují zprava, procenta tak zůstanou vidět.
            this.AppTitleTextCurrent = $"{((int)Math.Floor(100d * ratio)).ToString()}% File Rescue";
        }
        private FileRescue _FileRescuer;
        /// <summary>
        /// Summary blok záchrany: celkový postup. Po doběhnutí se nenuluje, zůstává zobrazen.
        /// </summary>
        private FileRescueTotalControl _FileRescueTotalControl;
        /// <summary>
        /// Summary blok záchrany: aktuální soubor. Po doběhnutí se nenuluje, zůstává zobrazen.
        /// </summary>
        private FileRescueFileControl _FileRescueFileControl;
        /// <summary>
        /// Summary blok záchrany: Teploměr zdraví zdrojového média. Po doběhnutí se nenuluje, zůstává zobrazen.
        /// </summary>
        private MediaHealthControl _FileRescueHealthControl;
        #endregion
        #region Akce: Zápis a čtení testovacích dat na disk
        /// <summary>
        /// Požadavek na start zápisu / čtení dat z disku
        /// </summary>
        private void RunDriveTest(ActionState action)
        {
            var testAction = (action == ActionState.TestSave ? DriveTester.TestAction.SaveTestData :
                             (action == ActionState.TestRead ? DriveTester.TestAction.ReadTestData :
                             (action == ActionState.ContentRead ? DriveTester.TestAction.ReadContent : DriveTester.TestAction.None)));
            if (testAction == DriveTester.TestAction.None) return;

            ResultsInfoPanelClear();
            ShowControls(action, true);

            DriveTester driveTester = new DriveTester();
            TestInfoPanelPrepare(driveTester);
            driveTester.WorkingStep += _DriveTester_TestStep;
            driveTester.WorkingDone += _DriveTester_TestDone;
            _DriveTester = driveTester;
            driveTester.Start(this.SelectedDrive, testAction);
        }
        /// <summary>
        /// Požadavek na zastavení testu
        /// </summary>
        private void RunPauseStopDriveTest(RunState state)
        {
            if (_DriveTester != null)
                _DriveTester?.ChangeState(state);
            else
                ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Tester provedl krok testu a volá refresh dat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DriveTester_TestStep(object sender, EventArgs e)
        {
            _DriveTesterRefresh();
        }
        /// <summary>
        /// Tester skončil svoji činnost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DriveTester_TestDone(object sender, EventArgs e)
        {
            _DriveTesterRefresh();
            _DriveTester = null;
            ShowControls(ActionState.Dialog, false);
        }
        /// <summary>
        /// Zajistí refresh dat po jednom každém postupném kroku testeru
        /// </summary>
        private void _DriveTesterRefresh()
        {
            var driveTester = _DriveTester;
            if (driveTester is null) return;

            if (this.InvokeRequired)
                this.BeginInvoke(new Action(() => action(driveTester)));
            else
                action(driveTester);


            // Akce v GUI threadu
            void action(DriveTester tester)
            {
                if (tester != null)
                {
                    ResultsInfoPanelFillData(tester);
                    VisualMapFillFromDriveTester(tester);
                }
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> vygeneruje prvky <see cref="DriveTestTimePhaseControl"/> pro zobrazování dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void TestInfoPanelPrepare(DriveTester driveTester)
        {
            var testPhases = new Dictionary<DriveTester.TestPhase, DriveTestTimePhaseControl>();

            int x = 3;
            int y = 3;
            int w = ResultsInfoPanel.ClientRectangle.Width - 6;
            int h = DriveTestTimePhaseControl.OptimalHeight;

            addPhase(DriveTester.TestPhase.SaveShortFile);
            addPhase(DriveTester.TestPhase.ReadShortFile);
            addPhase(DriveTester.TestPhase.SaveLongFile);
            addPhase(DriveTester.TestPhase.ReadLongFile);

            _DriveTesterPhases = testPhases;

            // Zařadí control pro danou fázi
            void addPhase(DriveTester.TestPhase phase)
            {
                DriveTestTimePhaseControl panel = new DriveTestTimePhaseControl();
                panel.Bounds = new Rectangle(x, y, w, h);
                ResultsInfoPanel.Controls.Add(panel);
                testPhases.Add(phase, panel);
                y += panel.Height;
            }
        }
        /// <summary>
        /// Do prvků v panelu informací <see cref="ResultsInfoPanel"/> aktualizuje hodnoty v controlech <see cref="DriveAnalyseGroupControl"/> z dat skupin z analyzeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void ResultsInfoPanelFillData(DriveTester driveTester)
        {
            var testPhases = _DriveTesterPhases;
            if (testPhases != null && driveTester != null)
            {
                var testPhase = driveTester.CurrentTestPhase;
                testPhases[DriveTester.TestPhase.SaveShortFile].StoreInfo(driveTester.TimeInfoSaveShort, testPhase);
                testPhases[DriveTester.TestPhase.SaveLongFile].StoreInfo(driveTester.TimeInfoSaveLong, testPhase);
                testPhases[DriveTester.TestPhase.ReadShortFile].StoreInfo(driveTester.TimeInfoReadShort, testPhase);
                testPhases[DriveTester.TestPhase.ReadLongFile].StoreInfo(driveTester.TimeInfoReadLong, testPhase);

                decimal progressRatio = driveTester.ProgressRatio;

                int value = (int)Math.Round(progressRatio * (decimal)_TaskProgressMaximum, 0);
                if (value == 0 && driveTester.ProgressRatio > 0m) value = 1;
                _TaskProgressValue = value;

                bool hasError = (driveTester.TimeInfoSaveShort.ErrorBytes > 0 || driveTester.TimeInfoSaveLong.ErrorBytes > 0 || driveTester.TimeInfoReadShort.ErrorBytes > 0 || driveTester.TimeInfoReadLong.ErrorBytes > 0);
                _TaskProgressState = (!hasError ? ThumbnailProgressState.Normal : ThumbnailProgressState.Error);

                // TaskBar: Titulek aplikace = "58% SD Card Write H:"
                //  Procenta na začátku: Windows při více oknech v TaskBaru text zkracují zprava, procenta tak zůstanou vidět.
                //  Zaokrouhleno dolů, aby se 100% neukázalo před koncem (stejně jako u FileRescue).
                string drive = driveTester.Target.Name.Substring(0, 1).ToUpper();
                string appName = (testPhase == DriveTester.TestPhase.SaveShortFile || testPhase == DriveTester.TestPhase.SaveLongFile ? "SD Card Write" :
                                 (testPhase == DriveTester.TestPhase.ReadShortFile || testPhase == DriveTester.TestPhase.ReadLongFile ? "SD Card Verify" :
                                 (testPhase == DriveTester.TestPhase.ReadAnyContent ? "SD Card Read" : "")));
                string percent = ((int)Math.Floor(100m * progressRatio)).ToString();
                string title = $"{percent}% {appName} {drive}:";

                this.AppTitleTextCurrent = title;
            }
        }
        /// <summary>
        /// Titulek okna bez suffixu = titulek aplikace
        /// </summary>
        private string __AppTitleTextStandard;
        /// <summary>
        /// Titulek okna aktuální, napojený na aktuální Text = fyzický titulek okna.
        /// Pokud bude opakovaně setována stejná hodnota, detekuje se to podle fieldu <see cref="__AppTitleTextCurrent"/> a nebude se do property Text nic vkládat.
        /// </summary>
        private string AppTitleTextCurrent
        {
            get { return __AppTitleTextCurrent; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {   // Pouze neprázdný string:
                    if (!String.Equals(value, __AppTitleTextCurrent))
                    {   // Pouze změněný string:
                        __AppTitleTextCurrent = value;
                        this.Text = value;
                    }
                }
            }
        }
        private string __AppTitleTextCurrent;
        /// <summary>
        /// Do mapy <see cref="LinearMapControl"/> vloží prvky pocházející ze skupin z testeru
        /// </summary>
        /// <param name="driveAnalyser"></param>
        private void VisualMapFillFromDriveTester(DriveTester driveTester)
        {
            var fileGroups = driveTester.FileGroups;
            var totalSize = driveTester.TotalSize;
            _VisualMapFillFromItems(fileGroups, totalSize);
        }
        /// <summary>
        /// Instance testeru
        /// </summary>
        private DriveTester _DriveTester;
        private Dictionary<DriveTester.TestPhase, DriveTestTimePhaseControl> _DriveTesterPhases;
        #endregion
        #region Flow: řízení běhu akcí (Pauza - Stop - Run)
        protected RunState RunState 
        { 
            get { return __RunState; } 
            set  { _SetRunState(value, true, true); }
        }
        private void _SetRunState(RunState runState, bool withGui, bool withActions)
        {
            __RunState = runState;

            if (withGui)
                this.ShowFlowButtonsEnabled(runState);

            if (withActions)
            {
                switch (CurrentState)
                {
                    case ActionState.AnalyseContent:
                        RunPauseStopDriveAnalyse(runState);
                        break;
                    case ActionState.TestSave:
                    case ActionState.TestRead:
                    case ActionState.ContentRead:
                        RunPauseStopDriveTest(runState);
                        break;
                    case ActionState.FileRescue:
                        RunPauseStopFileRescue(runState);
                        break;
                    default:
                        ShowControls(ActionState.Dialog, false);
                        break;
                }
            }
        }
        private RunState __RunState;
        private void ToolFlowControlPauseButton_Click(object sender, EventArgs e)
        {
            RunState = RunState.Pause;
        }
        private void ToolFlowControlStopButton_Click(object sender, EventArgs e)
        {
            RunState = RunState.Stop;
        }
        private void ToolFlowControlRunButton_Click(object sender, EventArgs e)
        {
            RunState = RunState.Run;
        }
        #endregion
        #region Windows Taskbar Progress
        /// <summary>
        /// Inicializace komponenty pro zobrazení progresu v Taskbaru Windows
        /// </summary>
        private void InitializeProgress()
        {
            __TaskProgress = new TaskProgress(this);
            __TaskProgress.ProgressMaximum = 500;
            /*  Použití je jednoduché:
            var rand = new Random();
            var next = rand.Next(30);
            __TaskProgress.ProgressState = (next < 10 ? ThumbnailProgressState.Normal : next < 20 ? ThumbnailProgressState.Error : ThumbnailProgressState.Paused);
            __TaskProgress.ProgressValue = rand.Next(0, 100);
            */
            this.__AppTitleTextStandard = this.Text;
            this.__AppTitleTextCurrent = null;
        }
        protected override void WndProc(ref Message m)
        {
            __TaskProgress.FormWndProc(ref m);
            base.WndProc(ref m);
        }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 1 a více.
        /// Pokud bude setována hodnota nižší, než je aktuální <see cref="_TaskProgressValue"/>, tak bude <see cref="_TaskProgressValue"/> snížena na toto nově zadané maximum.
        /// </summary>
        private int _TaskProgressMaximum { get { return __TaskProgress.ProgressMaximum; } set { __TaskProgress.ProgressMaximum = value; } }
        /// <summary>
        /// Hodnota progresu.
        /// Musí být v rozsahu 0 až <see cref="ProgressMaximum"/>.
        /// </summary>
        private int _TaskProgressValue { get { return __TaskProgress.ProgressValue; } set { __TaskProgress.ProgressValue = value; } }
        /// <summary>
        /// Status progresu = odpovídá barvě
        /// </summary>
        private ThumbnailProgressState _TaskProgressState { get { return __TaskProgress.ProgressState; } set { __TaskProgress.ProgressState = value; } }
        /// <summary>
        /// Komponenta pro zobrazení progresu v Taskbaru Windows
        /// </summary>
        private TaskProgress __TaskProgress;
        #endregion
    }
}
