using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace DjSoft.Tools.SDCardTester
{
    public class DriveInfoPanel : Panel
    {
        public DriveInfoPanel()
        {
            Initialize();
        }
        private void Initialize()
        {
            this.SuspendLayout();

            int y = 13;
            int tabIndex = 0;

            this.DriveNameLabel = createLabel(102, "Označení disku:");
            this.DriveNameText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveTypeLabel = createLabel(70, "Typ disku:");
            this.DriveTypeText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveVolumeLabel = createLabel(107, "Přidělený název:");
            this.DriveVolumeText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveCapacityLabel = createLabel(154, "Celková kapacita [Byte]:");
            this.DriveCapacityText = createTextBox(176, HorizontalAlignment.Right);
            this.DriveFreeLabel = createLabel(139, "Volná kapacita [Byte]:");
            this.DriveFreeText = createTextBox(176, HorizontalAlignment.Right);
            this.DriveAvailableLabel = createLabel(162, "Dostupná kapacita [Byte]:");
            this.DriveAvailableText = createTextBox(176, HorizontalAlignment.Right);

            this.Size = new System.Drawing.Size(243, 299);

            this.ResumeLayout(false);
            this.PerformLayout();

            Label createLabel(int w, string text)
            {
                Label label = new Label()
                {
                    AutoSize = true,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238))),
                    Bounds = new Rectangle(6, y, w, 16),
                    TabIndex = tabIndex++,
                    Text = text
                };
                this.Controls.Add(label);
                y += 19;
                return label;
            }
            TextBox createTextBox(int w, HorizontalAlignment textAlign)
            {
                TextBox textBox = new TextBox()
                {
                    Enabled = false,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238))),
                    Bounds = new Rectangle(55, y, w, 22),
                    ReadOnly = true,
                    TabIndex = tabIndex++,
                    TabStop = false,
                    TextAlign = textAlign
                };
                this.Controls.Add(textBox);
                y += 27;
                return textBox;
            }
        }
        private Label DriveNameLabel;
        private TextBox DriveNameText;
        private Label DriveTypeLabel;
        private TextBox DriveTypeText;
        private Label DriveVolumeLabel;
        private TextBox DriveVolumeText;
        private Label DriveCapacityLabel;
        private TextBox DriveCapacityText;
        private Label DriveFreeLabel;
        private TextBox DriveFreeText;
        private Label DriveAvailableLabel;
        private TextBox DriveAvailableText;
        /// <summary>
        /// Do svých prvků vepíše hodnoty z dodaného <see cref="DriveInfo"/>
        /// </summary>
        /// <param name="selectedDrive"></param>
        internal void ShowProperties(DriveInfo selectedDrive)
        {
            this.DriveNameText.Text = selectedDrive?.Name ?? "";
            this.DriveTypeText.Text = selectedDrive?.DriveType.ToString() ?? "";
            this.DriveVolumeText.Text = selectedDrive?.VolumeLabel ?? "";
            this.DriveCapacityText.Text = getCapacityText(selectedDrive?.TotalSize);
            this.DriveFreeText.Text = getCapacityText(selectedDrive?.TotalFreeSpace);
            this.DriveAvailableText.Text = getCapacityText(selectedDrive?.AvailableFreeSpace);

            string getCapacityText(long? capacity)
            {
                if (!capacity.HasValue) return "";
                return capacity.Value.ToString("### ### ### ### ##0");
            }
        }
    }
}
