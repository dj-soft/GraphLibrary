using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ShortcutParser
{
    /// <summary>
    /// Struktura reprezentující vykreslenou ikonu zástupce na formuláři
    /// </summary>
    public class DroppedShortcut
    {
        /// <summary>
        /// Pozice ikony na formuláři (levý horní roh)
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// Načtená informace o zástupci
        /// </summary>
        public ShortcutInfo ShortcutInfo { get; set; }

        /// <summary>
        /// Ikona aplikace
        /// </summary>
        public Icon Icon { get; set; }

        /// <summary>
        /// Bitmap ikony (pro kreslení)
        /// </summary>
        public Bitmap Bitmap { get; set; }

        /// <summary>
        /// Velikost ikony na obrazovce
        /// </summary>
        public Size IconSize { get; set; }

        /// <summary>
        /// Obdélník pro hit testing
        /// </summary>
        public Rectangle Bounds
        {
            get { return new Rectangle(Position, IconSize); }
        }

        public override string ToString()
        {
            return $"{ShortcutInfo?.LinkName} @ {Position}";
        }
    }

    /// <summary>
    /// Hlavní formulář s podporou Drag&Drop zástupců
    /// </summary>
    public partial class ShortcutDropForm : Form
    {
        private List<DroppedShortcut> droppedShortcuts = new List<DroppedShortcut>();
        private const int ICON_SIZE = 64;  // Velikost vykreslené ikony v pixelech
        private const int ICON_PADDING = 10;  // Padding mezi ikonami a jejich popisky
        private const int TOOLTIP_HEIGHT = 25;  // Výška pole pro popis

        // Font pro popis pod ikonou
        private Font tooltipFont = new Font("Segoe UI", 8);

        // Kontextové menu pro pravý klik
        private ContextMenuStrip contextMenu;

        public ShortcutDropForm()
        {
            InitializeComponent();

            // Nastavení formuláře
            this.Text = "Drag & Drop Zástupců";
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;  // Anti-flickering
            this.Padding = new Padding(10);

            // Povolení Drag & Drop
            this.AllowDrop = true;

            // Setup kontextového menu
            SetupContextMenu();

            // Event handlery
            this.DragEnter += ShortcutDropForm_DragEnter;
            this.DragDrop += ShortcutDropForm_DragDrop;
            this.Paint += ShortcutDropForm_Paint;
            this.MouseClick += ShortcutDropForm_MouseClick;
            this.MouseDown += ShortcutDropForm_MouseDown;

            // Výchozí text
            DrawPlaceholder();
        }

        /// <summary>
        /// Vytvoří a nastaví kontextové menu pro pravý klik
        /// </summary>
        private void SetupContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            
            // Položka: Vložit zástupce z Clipboardu
            var pasteItem = new ToolStripMenuItem("Vložit zástupce");
            pasteItem.Image = SystemIcons.Application.ToBitmap();
            pasteItem.Click += PasteShortcut_Click;
            contextMenu.Items.Add(pasteItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Položka: Vymazat vše
            var clearItem = new ToolStripMenuItem("Vymazat vše");
            clearItem.Click += ClearAll_Click;
            contextMenu.Items.Add(clearItem);

            this.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Event: Uživatel táhne objekty nad formulář
        /// </summary>
        private void ShortcutDropForm_DragEnter(object sender, DragEventArgs e)
        {
            // Kontrola, zda jsou v přesunutých datech soubory
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Přijmi drop pouze pokud jsou tam .lnk soubory
                if (files.Any(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }

            e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// Event: Uživatel pustil objekty nad formulář
        /// </summary>
        private void ShortcutDropForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                    return;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Point dropLocation = this.PointToClient(new Point(e.X, e.Y));

                // Filtruj pouze .lnk soubory
                var lnkFiles = files
                    .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (lnkFiles.Count == 0)
                {
                    MessageBox.Show(
                        "Přetáhněte prosím .lnk soubory (zástupce Windows)",
                        "Neplatný typ souboru",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Zpracuj každý zástupce
                foreach (string lnkFile in lnkFiles)
                {
                    ProcessDroppedShortcut(lnkFile, dropLocation);
                    
                    // Posun pozici pro další ikonu (kaskádové uspořádání)
                    dropLocation.X += ICON_SIZE + ICON_PADDING * 2;
                    if (dropLocation.X > this.Width - ICON_SIZE - 20)
                    {
                        dropLocation.X = 20;
                        dropLocation.Y += ICON_SIZE + TOOLTIP_HEIGHT + ICON_PADDING * 2;
                    }
                }

                // Překresli formulář
                this.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Chyba při zpracování zástupce: {ex.Message}",
                    "Chyba",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Zpracuj jeden přetažený zástupce
        /// </summary>
        private void ProcessDroppedShortcut(string lnkFilePath, Point position)
        {
            try
            {
                // 1. Načti informace o zástupci
                ShortcutInfo shortcutInfo = ShortcutLoader.LoadShortcut(lnkFilePath);

                if (shortcutInfo == null || string.IsNullOrWhiteSpace(shortcutInfo.TargetPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Zástupce nemá nastaven cíl: {lnkFilePath}");
                    return;
                }

                // 2. Extrahuj ikonu
                object iconResult = IconExtractor.GetIconFromShortcut(shortcutInfo, returnBitmap: true);
                
                if (iconResult == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Nepodařilo se extrahovat ikonu: {lnkFilePath}");
                    return;
                }

                // 3. Konvertuj na Bitmap pokud potřebujeme
                Bitmap bitmap = null;
                Icon icon = null;

                if (iconResult is Bitmap bmp)
                {
                    bitmap = bmp;
                }
                else if (iconResult is Icon ico)
                {
                    icon = ico;
                    bitmap = IconExtractor.IconToBitmap(ico);
                }

                if (bitmap == null)
                    return;

                // 4. Změní velikost ikony na ICON_SIZE x ICON_SIZE
                Bitmap resizedBitmap = ResizeBitmap(bitmap, ICON_SIZE, ICON_SIZE);

                // 5. Ulož do seznamu
                var droppedShortcut = new DroppedShortcut
                {
                    Position = position,
                    ShortcutInfo = shortcutInfo,
                    Bitmap = resizedBitmap,
                    Icon = icon,
                    IconSize = new Size(ICON_SIZE, ICON_SIZE)
                };

                droppedShortcuts.Add(droppedShortcut);

                System.Diagnostics.Debug.WriteLine(
                    $"✓ Zástupce přidán: {shortcutInfo.LinkName} @ {position}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba: {ex.Message}");
            }
        }

        /// <summary>
        /// Event: Kreslení formuláře
        /// </summary>
        private void ShortcutDropForm_Paint(object sender, PaintEventArgs e)
        {
            // Vykreslí prázdný formulář s instrukcemi
            if (droppedShortcuts.Count == 0)
            {
                DrawPlaceholder();
                return;
            }

            // Vykreslí všechny přetažené zástupce
            foreach (var dropped in droppedShortcuts)
            {
                DrawShortcut(e.Graphics, dropped);
            }
        }

        /// <summary>
        /// Vykresli jednu ikonu se popiskem
        /// </summary>
        private void DrawShortcut(Graphics g, DroppedShortcut dropped)
        {
            try
            {
                // 1. Vykresli ikonu
                if (dropped.Bitmap != null)
                {
                    g.DrawImage(
                        dropped.Bitmap,
                        dropped.Position.X,
                        dropped.Position.Y,
                        ICON_SIZE,
                        ICON_SIZE);
                }

                // 2. Vykresli popis pod ikonou
                string description = GetShortcutDescription(dropped.ShortcutInfo);
                
                Rectangle textBounds = new Rectangle(
                    dropped.Position.X,
                    dropped.Position.Y + ICON_SIZE + 2,
                    ICON_SIZE,
                    TOOLTIP_HEIGHT);

                // Styl textu
                StringFormat stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                g.DrawString(
                    description,
                    tooltipFont,
                    Brushes.Black,
                    textBounds,
                    stringFormat);

                // 3. Vykresli hover efekt (tenký rámeček)
                g.DrawRectangle(
                    new Pen(Color.LightGray, 1),
                    dropped.Bounds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba při kreslení: {ex.Message}");
            }
        }

        /// <summary>
        /// Vykresli výchozí text a instrukce
        /// </summary>
        private void DrawPlaceholder()
        {
            if (this.CreateGraphics() is Graphics g)
            {
                g.Clear(this.BackColor);
                
                string instruction = "Přetáhněte zástupce aplikace (.lnk soubory) z Windows Plochy nebo Exploreru";
                Rectangle bounds = new Rectangle(0, this.Height / 2 - 50, this.Width, 100);

                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(
                    instruction,
                    new Font("Segoe UI", 12),
                    new SolidBrush(Color.Gray),
                    bounds,
                    format);

                g.Dispose();
            }
        }

        /// <summary>
        /// Získej popis zástupce (jméno + cíl)
        /// </summary>
        private string GetShortcutDescription(ShortcutInfo shortcutInfo)
        {
            string name = shortcutInfo.LinkName;
            string target = Path.GetFileNameWithoutExtension(shortcutInfo.TargetPath);
            
            // Pokus se získat jméno z cíle, pokud jméno zástupce neexistuje
            if (string.IsNullOrWhiteSpace(name))
                name = target;

            return name.Length > 15 ? name.Substring(0, 12) + "..." : name;
        }

        /// <summary>
        /// Změní velikost Bitmap na danou velikost
        /// </summary>
        private Bitmap ResizeBitmap(Bitmap original, int width, int height)
        {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, width, height);
            }
            return resized;
        }

        /// <summary>
        /// Event: Klik na ikonu - zobraz detaily (levý klik)
        /// </summary>
        private void ShortcutDropForm_MouseClick(object sender, MouseEventArgs e)
        {
            // Pouze levý klik
            if (e.Button != MouseButtons.Left)
                return;

            // Najdi, kterou ikonu uživatel klikl
            foreach (var dropped in droppedShortcuts)
            {
                if (dropped.Bounds.Contains(e.Location))
                {
                    ShowShortcutDetails(dropped);
                    return;
                }
            }
        }

        /// <summary>
        /// Event: Pravý klik - zobraz menu nebo vložit ze Clipboardu
        /// </summary>
        private void ShortcutDropForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Pouze pravý klik
            if (e.Button != MouseButtons.Right)
                return;

            // Ulož pozici pro vložení
            this.Tag = e.Location;

            // Zobraz kontextové menu na pozici kliku
            contextMenu.Show(this, e.Location);
        }

        /// <summary>
        /// Callback: Vložit zástupce z Clipboardu
        /// </summary>
        private void PasteShortcut_Click(object sender, EventArgs e)
        {
            try
            {
                Point pasteLocation = (Point)(this.Tag ?? new Point(20, 20));

                if (!Clipboard.ContainsFileDropList())
                {
                    MessageBox.Show(
                        "Clipboard neobsahuje soubory.\nZkopírujte .lnk soubor do schránky.",
                        "Chyba",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var files = Clipboard.GetFileDropList();

                // Filtruj pouze .lnk soubory
                var lnkFiles = files
                    .Cast<string>()
                    .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (lnkFiles.Count == 0)
                {
                    MessageBox.Show(
                        "V Clipboardu nejsou .lnk soubory (zástupce).",
                        "Chyba",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Zpracuj každý zástupce
                foreach (string lnkFile in lnkFiles)
                {
                    ProcessDroppedShortcut(lnkFile, pasteLocation);

                    // Posun pozici pro další ikonu
                    pasteLocation.X += ICON_SIZE + ICON_PADDING * 2;
                    if (pasteLocation.X > this.Width - ICON_SIZE - 20)
                    {
                        pasteLocation.X = 20;
                        pasteLocation.Y += ICON_SIZE + TOOLTIP_HEIGHT + ICON_PADDING * 2;
                    }
                }

                // Překresli formulář
                this.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Chyba při vložení zástupce: {ex.Message}",
                    "Chyba",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Callback: Vymazat všechny ikony
        /// </summary>
        private void ClearAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Opravdu chcete vymazat všechny ikony?",
                "Potvrzení",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Vyčisti bitmaps
                foreach (var dropped in droppedShortcuts)
                {
                    dropped.Bitmap?.Dispose();
                    dropped.Icon?.Dispose();
                }

                droppedShortcuts.Clear();
                this.Invalidate();
            }
        }

        /// <summary>
        /// Zobraz detaily zástupce v MessageBox
        /// </summary>
        private void ShowShortcutDetails(DroppedShortcut dropped)
        {
            var info = dropped.ShortcutInfo;
            string details = $@"
Název: {info.LinkName}
Cíl: {info.TargetPath}
Argumenty: {info.Arguments}
Pracovní adresář: {info.WorkingDirectory}
Popis: {info.Description}
Ikona: {info.IconLocation}
Styl okna: {info.WindowStyle}
";
            MessageBox.Show(
                details,
                "Detaily zástupce",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Designer code placeholder
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Vyčisti bitmaps a ikony
                foreach (var dropped in droppedShortcuts)
                {
                    dropped.Bitmap?.Dispose();
                    dropped.Icon?.Dispose();
                }

                tooltipFont?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
