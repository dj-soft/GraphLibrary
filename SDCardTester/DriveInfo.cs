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
    /// <summary>
    /// Vizuální control zobrazující informace o disku.
    /// Reálné informace o určitém disku se zobrazí voláním metody <see cref="ShowProperties(DriveInfo)"/>.
    /// </summary>
    public class DriveInfoPanel : Panel
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DriveInfoPanel()
        {
            Initialize();
        }
        private void Initialize()
        {
            this.SuspendLayout();

            int y = 13;
            int tabIndex = 0;

            this.DriveVolumeLabel = createLabel(107, "Pojmenování disku:");
            this.DriveVolumeText = createTextBox(154, HorizontalAlignment.Left, true, 0);
            this.DriveVolumeApply = createButton(208, DjSoft.Tools.SDCardTester.Properties.Resources.dialog_ok_apply_2_16);
            this.DriveNameLabel = createLabel(102, "Písmeno disku:");
            this.DriveNameText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveTypeLabel = createLabel(70, "Typ disku:");
            this.DriveTypeText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveFormatLabel = createLabel(70, "Formát disku:");
            this.DriveFormatText = createTextBox(176, HorizontalAlignment.Left);
            this.DriveCapacityLabel = createLabel(154, "Celková kapacita [Byte]:");
            this.DriveCapacityText = createTextBox(176, HorizontalAlignment.Right);
            this.DriveFreeLabel = createLabel(139, "Volná kapacita [Byte]:");
            this.DriveFreeText = createTextBox(176, HorizontalAlignment.Right);
            this.DriveAvailableLabel = createLabel(162, "Dostupná kapacita [Byte]:");
            this.DriveAvailableText = createTextBox(176, HorizontalAlignment.Right);

            this.Size = new Size(243, y + 6);
            this.MinimumSize = this.Size;

            this.ResumeLayout(false);
            this.PerformLayout();
            _VolumeEditInit();

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
            TextBox createTextBox(int w, HorizontalAlignment textAlign, bool editable = false, int stepY = 27)
            {
                TextBox textBox = new TextBox()
                {
                    Enabled = editable,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238))),
                    Bounds = new Rectangle(55, y, w, 22),
                    ReadOnly = !editable,
                    TabIndex = tabIndex++,
                    TabStop = false,
                    TextAlign = textAlign
                };
                this.Controls.Add(textBox);
                y += stepY;
                return textBox;
            }
            Button createButton(int x, Image image, int stepY = 27)
            {
                Button button = new Button()
                {
                    Text = "",
                    Image = image,
                    Bounds = new Rectangle(x, y - 1, 24, 24),
                    TabIndex = tabIndex++,
                    TabStop = false
                };
                this.Controls.Add(button);
                y += stepY;
                return button;
            }
        }
        private Label DriveVolumeLabel;
        private TextBox DriveVolumeText;
        private Button DriveVolumeApply;
        private Label DriveNameLabel;
        private TextBox DriveNameText;
        private Label DriveTypeLabel;
        private TextBox DriveTypeText;
        private Label DriveFormatLabel;
        private TextBox DriveFormatText;
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
            this.__VolumeLabelOriginal = selectedDrive?.VolumeLabel ?? "";
            this._VolumeLabelDisplayed = this.__VolumeLabelOriginal;
            this.DriveNameText.Text = selectedDrive?.Name ?? "";
            this.DriveTypeText.Text = selectedDrive?.DriveType.ToString() ?? "";
            this.DriveFormatText.Text = selectedDrive?.DriveFormat.ToString() ?? "";
            this.DriveCapacityText.Text = getCapacityText(selectedDrive?.TotalSize);
            this.DriveFreeText.Text = getCapacityText(selectedDrive?.TotalFreeSpace);
            this.DriveAvailableText.Text = getCapacityText(selectedDrive?.AvailableFreeSpace);

            this.__VolumeDriveName = selectedDrive?.Name ?? "";
            _DriveVolumeApplyRefreshEnabled();

            string getCapacityText(long? capacity)
            {
                if (!capacity.HasValue) return "";
                return capacity.Value.ToString("### ### ### ### ##0");
            }
        }
        #region Volumne = jméno disku
        private void _VolumeEditInit()
        {
            this.DriveVolumeApply.Click += _DriveVolumeApplyClick;
            this.DriveVolumeText.TextChanged += _DriveVolumeTextChanged;
        }
        /// <summary>
        /// Aktuální text Volume.
        /// Setování uloží hodnotu do <see cref="__VolumeLabelDisplayed"/> i do vizuálního controlu.
        /// Změna hodnoty vyvolá event <see cref="_DriveVolumeTextChanged(object, EventArgs)"/>, který řídí Enabled buttonu <see cref="DriveVolumeApply"/>.
        /// </summary>
        private string _VolumeLabelDisplayed
        {
            get { return __VolumeLabelDisplayed; }
            set
            {
                string text = value ?? "";
                __VolumeLabelDisplayed = text;
                if (!String.Equals(text, this.DriveVolumeText.Text))
                    this.DriveVolumeText.Text = text;
            }
        }
        /// <summary>
        /// Jméno disku, na kterém se nachází zde zobrazené informace
        /// </summary>
        private string __VolumeDriveName;
        /// <summary>
        /// Jméno disku načtené z disku
        /// </summary>
        private string __VolumeLabelOriginal;
        /// <summary>
        /// Jméno disku zobrazené v controlu
        /// </summary>
        private string __VolumeLabelDisplayed;
        /// <summary>
        /// Došlo ke změně textu Volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DriveVolumeTextChanged(object sender, EventArgs e)
        {
            __VolumeLabelDisplayed = this.DriveVolumeText.Text ?? "";
            _DriveVolumeApplyRefreshEnabled();
        }
        /// <summary>
        /// Pokud se liší text Volume Label mezi <see cref="__VolumeLabelOriginal"/> a <see cref="__VolumeLabelDisplayed"/>, pak nastaví Enabled pro button <see cref="DriveVolumeApply"/>.
        /// </summary>
        private void _DriveVolumeApplyRefreshEnabled()
        {
            DriveVolumeApply.Enabled = !String.Equals(__VolumeLabelDisplayed, __VolumeLabelOriginal);
        }
        /// <summary>
        /// Po kliknutí na Enabled zapíšeme daný název disku
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DriveVolumeApplyClick(object sender, EventArgs e)
        {
            _VolumeStoreToDrive(__VolumeDriveName, _VolumeLabelDisplayed);
            this.DriveVolumeText.Focus();
        }
        /// <summary>
        /// Setuje fyzicky do daného disku danou jmenovku.
        /// </summary>
        /// <param name="driveName"></param>
        /// <param name="volumeText"></param>
        private void _VolumeStoreToDrive(string driveName, string volumeText)
        {
            if (String.IsNullOrEmpty(driveName)) return;

            var volumeLabelOriginal = __VolumeLabelOriginal;
            volumeText = volumeText.Trim();
            string result = volumeText;
            try
            {
                System.IO.DriveInfo driveInfo = new DriveInfo(driveName);
                if (driveInfo.IsReady && !String.Equals(driveInfo.VolumeLabel, volumeText))
                {
                    driveInfo.VolumeLabel = volumeText;
                    driveInfo = new DriveInfo(driveName);
                    volumeLabelOriginal = driveInfo.VolumeLabel;
                }
            }
            catch (Exception exc)
            {
                System.Windows.Forms.MessageBox.Show(this.FindForm(), exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                try
                {
                    System.IO.DriveInfo driveInfo = new DriveInfo(driveName);
                    if (driveInfo.IsReady)
                        volumeLabelOriginal = driveInfo.VolumeLabel;
                }
                catch { }
            }

            __VolumeLabelOriginal = volumeLabelOriginal;
            _VolumeLabelDisplayed = volumeLabelOriginal;
            _DriveVolumeApplyRefreshEnabled();
        }
        #endregion
    }
}
