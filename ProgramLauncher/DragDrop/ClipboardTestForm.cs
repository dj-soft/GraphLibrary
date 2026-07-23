using System;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace DjSoft.Tools.ProgramLauncher.ShortcutParser
{
    /// <summary>
    /// Testovací aplikace pro funkcionalitu Clipboard
    /// </summary>
    public class ClipboardTestForm : Form
    {
        private Button copyButton;
        private Button openMainFormButton;
        private ListBox clipboardContentList;
        private Label statusLabel;

        public ClipboardTestForm()
        {
            InitializeComponent();
            this.Text = "Testovací aplikace - Clipboard";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Label s instrukcemi
            statusLabel = new Label
            {
                Text = "Instrukce:\n1. Zkopíruj zástupce na ploše (Ctrl+C)\n2. Klikni 'Zobrazit Clipboard' a prověř\n3. Otevři hlavní aplikaci a vlepš pravým klikem",
                Top = 10,
                Left = 10,
                Width = 470,
                Height = 80,
                AutoSize = false
            };
            this.Controls.Add(statusLabel);

            // Tlačítko: Otevři hlavní aplikaci
            openMainFormButton = new Button
            {
                Text = "Otevřít hlavní aplikaci s Drag&Drop",
                Top = 100,
                Left = 10,
                Width = 470,
                Height = 40
            };
            openMainFormButton.Click += (s, e) => OpenMainForm();
            this.Controls.Add(openMainFormButton);

            // Tlačítko: Zobraz Clipboard
            copyButton = new Button
            {
                Text = "Zobrazit obsah Clipboardu",
                Top = 150,
                Left = 10,
                Width = 470,
                Height = 40
            };
            copyButton.Click += (s, e) => DisplayClipboardContent();
            this.Controls.Add(copyButton);

            // ListBox: Obsah Clipboardu
            clipboardContentList = new ListBox
            {
                Top = 200,
                Left = 10,
                Width = 470,
                Height = 150
            };
            this.Controls.Add(clipboardContentList);

            // Tlačítko: Vyčisti Clipboard
            var clearButton = new Button
            {
                Text = "Vyčisti Clipboard",
                Top = 360,
                Left = 10,
                Width = 470,
                Height = 30
            };
            clearButton.Click += (s, e) =>
            {
                Clipboard.Clear();
                MessageBox.Show("Clipboard vyčištěn");
                DisplayClipboardContent();
            };
            this.Controls.Add(clearButton);
        }

        private void DisplayClipboardContent()
        {
            clipboardContentList.Items.Clear();

            try
            {
                // Zkontroluj, zda je něco v Clipboardu
                if (!Clipboard.ContainsFileDropList())
                {
                    clipboardContentList.Items.Add("Clipboard je prázdný nebo neobsahuje soubory");
                    return;
                }

                // Čti seznam souborů
                StringCollection files = Clipboard.GetFileDropList();

                if (files.Count == 0)
                {
                    clipboardContentList.Items.Add("Žádné soubory v Clipboardu");
                    return;
                }

                clipboardContentList.Items.Add($"Počet souborů: {files.Count}\n");

                foreach (string file in files)
                {
                    bool isShortcut = file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
                    string marker = isShortcut ? "✓ [LNK]" : "  [Other]";

                    clipboardContentList.Items.Add($"{marker} {file}");
                }
            }
            catch (Exception ex)
            {
                clipboardContentList.Items.Add($"Chyba: {ex.Message}");
            }
        }

        private void OpenMainForm()
        {
            ShortcutDropForm mainForm = new ShortcutDropForm();
            mainForm.Show();

            // Zobraz nápovědu
            MessageBox.Show(
                @"Hlavní aplikace se spustila.

Nyní můžeš:

1. DRAG & DROP: Přetáhni zástupce ze schránky
   - Vlevo v Exploreru najdi .lnk soubor
   - Přetáhni jej do okna aplikace

2. CLIPBOARD VLOŽENÍ: Pravý klik v aplikaci
   - Zkopíruj zástupce na ploše (Ctrl+C)
   - Pravý klik do aplikace
   - Vyber 'Vložit zástupce'

3. KLIK NA IKONU: Zobrazit detaily
   - Klikni levým klikem na kteroukoli ikonu
   - Zobrazí se detaily zástupce",
                "Nápověda",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        static void MainTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClipboardTestForm());
        }
    }
}
