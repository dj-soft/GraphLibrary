using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ShortcutParser
{
    /// <summary>
    /// Pokročilejší verze formuláře s PictureBox komponentami
    /// a schopností přesouvat/odstraňovat ikony
    /// </summary>
    public partial class AdvancedShortcutDropForm : Form
    {
        private FlowLayoutPanel flowPanel;
        private Label instructionLabel;
        private Dictionary<ShortcutIconControl, DroppedShortcut> iconControls = 
            new Dictionary<ShortcutIconControl, DroppedShortcut>();

        public AdvancedShortcutDropForm()
        {
            InitializeComponent();
            SetupUI();
            SetupDragDrop();
        }

        private void SetupUI()
        {
            this.Text = "Drag & Drop Zástupců - Pokročilá verze";
            this.Size = new Size(900, 700);
            this.BackColor = SystemColors.Control;
            this.Padding = new Padding(10);

            // Instrukční text
            instructionLabel = new Label
            {
                Text = "Přetáhněte zástupce (.lnk soubory) z Windows Plochy nebo Exploreru",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            this.Controls.Add(instructionLabel);

            // Flow layout panel pro ikony
            flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(10),
                AllowDrop = true  // Panel také přijímá drops
            };

            flowPanel.DragEnter += FlowPanel_DragEnter;
            flowPanel.DragDrop += FlowPanel_DragDrop;

            this.Controls.Add(flowPanel);
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            ValidateDragData(e);
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDroppedFiles(e);
        }

        private void FlowPanel_DragEnter(object sender, DragEventArgs e)
        {
            ValidateDragData(e);
        }

        private void FlowPanel_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDroppedFiles(e);
        }

        private void ValidateDragData(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Any(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void ProcessDroppedFiles(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var lnkFiles = files
                .Where(f => f.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (lnkFiles.Count == 0)
            {
                MessageBox.Show("Přetáhněte .lnk soubory", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (string lnkFile in lnkFiles)
            {
                AddShortcutIcon(lnkFile);
            }

            instructionLabel.Visible = flowPanel.Controls.Count == 0;
        }

        private void AddShortcutIcon(string lnkFilePath)
        {
            try
            {
                ShortcutInfo shortcutInfo = ShortcutLoader.LoadShortcut(lnkFilePath);

                if (shortcutInfo == null || string.IsNullOrWhiteSpace(shortcutInfo.TargetPath))
                    return;

                object iconResult = IconExtractor.GetIconFromShortcut(shortcutInfo, returnBitmap: true);
                if (iconResult == null)
                    return;

                Bitmap bitmap = iconResult as Bitmap ?? 
                    IconExtractor.IconToBitmap((Icon)iconResult);

                if (bitmap == null)
                    return;

                // Vytvoř custom control s ikonou
                ShortcutIconControl control = new ShortcutIconControl(shortcutInfo, bitmap)
                {
                    Margin = new Padding(5),
                    Cursor = Cursors.Hand
                };

                control.DeleteClicked += (s, args) =>
                {
                    flowPanel.Controls.Remove(control);
                    iconControls.Remove(control);
                    instructionLabel.Visible = flowPanel.Controls.Count == 0;
                };

                control.DetailsClicked += (s, args) =>
                {
                    ShowShortcutDetails(shortcutInfo);
                };

                flowPanel.Controls.Add(control);
                iconControls[control] = new DroppedShortcut
                {
                    ShortcutInfo = shortcutInfo,
                    Bitmap = bitmap
                };

                instructionLabel.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowShortcutDetails(ShortcutInfo info)
        {
            string details = $@"
╔════ DETAILY ZÁSTUPCE ════╗

Název:           {info.LinkName}
Cíl:             {info.TargetPath}
Argumenty:       {info.Arguments}
Pracovní adresář: {info.WorkingDirectory}
Popis:           {info.Description}
Ikona:           {info.IconLocation}
Styl okna:       {info.WindowStyle}
Horká klávesa:   {info.HotKey}
Relativní cesta: {info.RelativePath}
";
            MessageBox.Show(details, "Detaily zástupce", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }

    /// <summary>
    /// Custom control reprezentující jednu ikonu zástupce
    /// </summary>
    public class ShortcutIconControl : UserControl
    {
        private PictureBox pictureBox;
        private Label nameLabel;
        private Label targetLabel;
        private Button deleteButton;
        private Button detailsButton;
        private ShortcutInfo shortcutInfo;

        public event EventHandler DeleteClicked;
        public event EventHandler DetailsClicked;

        public ShortcutIconControl(ShortcutInfo shortcutInfo, Bitmap bitmap)
        {
            this.shortcutInfo = shortcutInfo;
            InitializeControl(bitmap);
        }

        private void InitializeControl(Bitmap bitmap)
        {
            this.Width = 140;
            this.Height = 160;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // PictureBox s ikonou
            pictureBox = new PictureBox
            {
                Image = bitmap,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Width = 120,
                Height = 90,
                Left = 10,
                Top = 10
            };
            this.Controls.Add(pictureBox);

            // Label s jménem
            nameLabel = new Label
            {
                Text = GetShortcutName(),
                AutoSize = false,
                Width = 120,
                Height = 20,
                Left = 10,
                Top = 105,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.TopLeft
            };
            this.Controls.Add(nameLabel);

            // Tlačítko Detaily
            detailsButton = new Button
            {
                Text = "...",
                Width = 25,
                Height = 20,
                Left = 10,
                Top = 130,
                Font = new Font("Segoe UI", 7)
            };
            detailsButton.Click += (s, e) => DetailsClicked?.Invoke(this, e);
            this.Controls.Add(detailsButton);

            // Tlačítko Smazat
            deleteButton = new Button
            {
                Text = "✕",
                Width = 25,
                Height = 20,
                Left = 105,
                Top = 130,
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7)
            };
            deleteButton.Click += (s, e) => DeleteClicked?.Invoke(this, e);
            this.Controls.Add(deleteButton);
        }

        private string GetShortcutName()
        {
            string name = shortcutInfo.LinkName;
            if (string.IsNullOrWhiteSpace(name))
                name = Path.GetFileNameWithoutExtension(shortcutInfo.TargetPath);

            return name.Length > 15 ? name.Substring(0, 12) + "..." : name;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox?.Image?.Dispose();
                nameLabel?.Dispose();
                targetLabel?.Dispose();
                deleteButton?.Dispose();
                detailsButton?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
