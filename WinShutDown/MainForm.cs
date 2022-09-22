using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Support.WinShutDown
{
    public partial class MainForm : Form
    {
        #region Konstruktor, tvorba, nativní eventy

        public MainForm()
        {
            InitializeComponent();
            InitializeContent();
            SetInteractiveState(true);
        }
        private void InitializeContent()
        {
            this.TimeCombo.Items.Clear();
            this.TimeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.TimeCombo.Items.Add(new TextData("Po uplynutí času (Minutka)", TimeModeType.AfterTime));
            this.TimeCombo.Items.Add(new TextData("Po dosažení času (Budík)", TimeModeType.AtTime));
            this.TimeCombo.Items.Add(new TextData("Při neaktivitě (Uspávač)", TimeModeType.Inactivity));
            this.TimeCombo.Items.Add(new TextData("Ihned", TimeModeType.Now));

            this.ModeCombo.Items.Clear();
            this.ModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            this.ModeCombo.Items.Add(new TextData("Usnout", ActionType.Sleep));
            this.ModeCombo.Items.Add(new TextData("Vypnout", ActionType.PowerOff));
            this.ModeCombo.Items.Add(new TextData("Restartovat", ActionType.Restart));

            this.CurrentTimeMode = TimeModeType.AfterTime;
            this.CurrentAction = ActionType.Sleep;
            this.TimeText.Text = "01:00";

            this.InactivityTrack.Minimum = 1;
            this.InactivityTrack.Maximum = 20;
            this.InactivityTrack.Value = 5;

            this.RestoreData();

            this.ShowTimeProperties();
            this.ShowInactivityTrackValue();

            this.TimeCombo.SelectedValueChanged += TimeCombo_SelectedValueChanged;
            this.InactivityTrack.ValueChanged += InactivityTrack_ValueChanged;
            this.FormClosing += MainForm_FormClosing;
        }
        private void TimeCombo_SelectedValueChanged(object sender, EventArgs e)
        {
            this.ShowTimeProperties();
        }
        private void InactivityTrack_ValueChanged(object sender, EventArgs e)
        {
            this.ShowInactivityTrackValue();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveData();
        }
        private void NonActiveControl_Enter(object sender, EventArgs e)
        {
            this.StopButton.Focus();
        }
        private void TopMostCheck_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMostReload();
            this.SaveData();
        }
        private void OkButton_Click(object sender, EventArgs e)
        {
            RunAction();
        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            StopAction();
        }
        #endregion
        #region Řízení zobrazení dat
        private void ShowTimeProperties()
        {
            TimeModeType timeMode = CurrentTimeMode;
            this.TimeValueTitle.Visible = (timeMode == TimeModeType.AtTime || timeMode == TimeModeType.AfterTime || timeMode == TimeModeType.Inactivity);
            this.TimeValueTitle.Text = (timeMode == TimeModeType.AtTime ? "V čase:" :
                                       (timeMode == TimeModeType.AfterTime ? "Po uplynutí:" :
                                       (timeMode == TimeModeType.Inactivity ? "Při rychlosti pod:" : "")));
            if (timeMode == TimeModeType.Inactivity) ShowInactivityTrackValue();
            this.TimeText.Visible = (timeMode == TimeModeType.AtTime || timeMode == TimeModeType.AfterTime);
            this.TimeLabel.Visible = (timeMode == TimeModeType.AtTime || timeMode == TimeModeType.AfterTime);
            this.InactivityTrack.Visible = (timeMode == TimeModeType.Inactivity);
            this.InactivityLabel.Visible = (timeMode == TimeModeType.Inactivity);
        }
        private void ShowInactivityTrackValue()
        {
            string label = "";
            TimeModeType timeMode = CurrentTimeMode;
            if (timeMode == TimeModeType.Inactivity)
            {
                int value = this.InactivityTrack.Value;
                int kbps = value;
                label = $"{kbps} KB/sec";
                if (_ShutDown != null)
                    _ShutDown.InactiveSpeedTreshold = kbps;
            }
            this.InactivityLabel.Text = label;
        }
        private void SetInteractiveState(bool active)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action<bool>(SetInteractiveState), active);
            else
            {
                this.TimeCombo.Enabled = active;
                this.TimeText.Enabled = active;
                this.InactivityTrack.Enabled = true;                 // Tuto hodnotu je možno změnit i za provozu, a propíše se i do běžícího ShutDownu!
                this.ModeCombo.Enabled = active;
                this.OkButton.Visible = active;
                this.OkButton.Enabled = active;

                this.TimeRemainingText.ReadOnly = true;
                this.TimeRemainingText.Visible = !active;
                this.StopButton.Visible = !active;
                this.StopButton.Enabled = !active;
                this.StatusText.ReadOnly = true;
                this.StatusText.Visible = !active;
            }
        }
        /// <summary>
        /// Do vlastnosti okna TopMost vloží hodnotu z <see cref="IsTopMostWindow"/> = tedy z checkboxu <see cref="TopMostCheck"/>.
        /// </summary>
        private void TopMostReload()
        {
            this.TopMost = this.IsTopMostWindow;
        }
        #endregion
        #region Aktuální data zadaná jako parametry v GUI
        /// <summary>
        /// Aktuálně zvolený režim času
        /// </summary>
        private TimeModeType CurrentTimeMode
        {
            get
            {
                if (this.TimeCombo.SelectedItem is TextData textData && textData.Data is TimeModeType timeMode) return timeMode;
                return TimeModeType.None;
            }
            set
            {
                TextData currentTextData = this.TimeCombo.SelectedItem as TextData;
                TextData selectedTextData = this.TimeCombo.Items.FindFirst<TextData>(item => item.Data is TimeModeType timeMode && timeMode == value);
                if (!Object.ReferenceEquals(currentTextData, selectedTextData))
                    this.TimeCombo.SelectedItem = selectedTextData;
            }
        }
        /// <summary>
        /// Aktuálně zadaný čas
        /// </summary>
        private string CurrentTimeValue
        {
            get { return this.TimeText.Text; }
            set { this.TimeText.Text = value; }
        }
        /// <summary>
        /// Aktuálně zadaný čas
        /// </summary>
        private int CurrentInactivityValue
        {
            get { return this.InactivityTrack.Value; }
            set { this.InactivityTrack.Value = value; }
        }
        /// <summary>
        /// Aktuálně zadaná cílová akce
        /// </summary>
        private ActionType CurrentAction
        {
            get
            {
                if (this.ModeCombo.SelectedItem is TextData textData && textData.Data is ActionType actionType) return actionType;
                return ActionType.None;
            }
            set
            {
                TextData currentTextData = this.ModeCombo.SelectedItem as TextData;
                TextData selectedTextData = this.ModeCombo.Items.FindFirst<TextData>(item => item.Data is ActionType actionType && actionType == value);
                if (!Object.ReferenceEquals(currentTextData, selectedTextData))
                    this.ModeCombo.SelectedItem = selectedTextData;
            }
        }
        /// <summary>
        /// Okno je vždy navrchu
        /// </summary>
        private bool IsTopMostWindow
        {
            get { return this.TopMostCheck.Checked; }
            set { this.TopMostCheck.Checked = value; TopMostReload(); }
        }
        #endregion
        #region Spouštění akce ShutDown a její storno
        private void RunAction()
        {
            if (!IsValidData(true)) return;

            if (_ShutDown != null)
            {
                if (_ShutDown.Running)
                {
                    _ShutDown.Cancel();
                    return;
                }
                _ShutDown.Clear();
            }

            SetInteractiveState(false);
            SaveData();
            _ShutDown = new ShutDown();
            _ShutDown.ShowStep += _ShutDown_ShowStep;
            _ShutDown.ShutDownStatusChanged += _ShutDown_ShutDownStatusChanged;
            _ShutDown.ShowDone += _ShutDown_ShowDone;
            ShowInactivityTrackValue();                   // Vepíše KBPS do _ShutDown.InactiveSpeedTreshold = kbps;
            _ShutDown.Start(this.CurrentTimeMode, CurrentTimeValue, this.CurrentAction);
        }
        private void StopAction()
        {
            if (_ShutDown != null)
                _ShutDown.Cancel();
            else
                SetInteractiveState(true);
        }
        private bool IsValidData(bool setValid)
        {
            bool timeValid = (this.CurrentTimeMode != TimeModeType.None);
            bool actionValid = (this.CurrentAction != ActionType.None);
            if (!timeValid && setValid) this.CurrentTimeMode = TimeModeType.AfterTime;
            if (!actionValid && setValid) this.CurrentAction = ActionType.Sleep;
            return (timeValid && actionValid);
        }
        private ShutDown _ShutDown;
        #endregion
        #region Eventhandlery z akce ShutDown = refresh GUI
        private void _ShutDown_ShowStep(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(ShowActionStatus));
            else
                ShowActionStatus();
        }
        private void _ShutDown_ShutDownStatusChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new Action(OnShutDownStatusChanged));
            else
                OnShutDownStatusChanged();
        }
        private void _ShutDown_ShowDone(object sender, EventArgs e)
        {
            _ShutDown = null;
            SetInteractiveState(true);
        }
        private void ShowActionStatus()
        {
            var shutDown = _ShutDown;
            if (shutDown is null) return;

            this.TimeRemainingText.BackColor = Color.MidnightBlue;
            this.TimeRemainingText.ForeColor = Color.Chartreuse;
            this.TimeRemainingText.Text = shutDown.TimeInfo;
            this.StatusText.Text = shutDown.StatusInfo;
            this.StatusText.ForeColor = Color.Black;
            this.StatusText.BackColor = shutDown.StatusColor;
        }
        /// <summary>
        /// Volá se po změn stavu <see cref="_ShutDown"/>, mělo by vynést okno do popředí
        /// </summary>
        private void OnShutDownStatusChanged()
        {
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
            bool isTopMost = this.IsTopMostWindow;
            if (isTopMost)
            {
                this.TopMost = false;
                this.TopMost = true;
            }
            else
            {
                this.TopMost = true;
                this.TopMost = false;
            }
            // this.TopMost = true;
            // this.Activate();
            // this.StopButton.Focus();
            // this.TopMost = isTopMost;
        }
        #endregion
        #region Úschova dat v registru Windows
        private void RestoreData()
        {
            var folder = WinRegFolder.CreateForProcessView(Microsoft.Win32.RegistryHive.CurrentUser, "Software\\DjSoft\\WinShutDown");
            using (var key = WinReg.OpenKey(folder))
            {
                int timeMode = WinReg.ReadInt32(key, "TimeMode", 0);
                int inactivityValue = WinReg.ReadInt32(key, "InactivityValue", 0);
                string timeText = WinReg.ReadString(key, "InactivityValue", "");
                int action = WinReg.ReadInt32(key, "Action", 0);
                int topMost = WinReg.ReadInt32(key, "TopMost", 0);

                if (timeMode > 0) this.CurrentTimeMode = (TimeModeType)timeMode;
                if (inactivityValue > 0) this.CurrentInactivityValue = inactivityValue;
                if (!String.IsNullOrEmpty(timeText)) this.CurrentTimeValue = timeText;
                if (action > 0) this.CurrentAction = (ActionType)action;
                this.IsTopMostWindow = (topMost > 0);
            }
        }
        private void SaveData()
        {
            var folder = WinRegFolder.CreateForProcessView(Microsoft.Win32.RegistryHive.CurrentUser, "Software\\DjSoft\\WinShutDown");
            using (var key = WinReg.OpenKey(folder, true))
            {
                int timeMode = (int)this.CurrentTimeMode;
                int inactivityValue = this.CurrentInactivityValue;
                string timeText = this.CurrentTimeValue;
                int action = (int)this.CurrentAction;
                int topMost = this.IsTopMostWindow ? 1 : 0;

                WinReg.WriteInt32(key, "TimeMode", timeMode);
                WinReg.WriteInt32(key, "InactivityValue", inactivityValue);
                WinReg.WriteString(key, "TargetTime", timeText);
                WinReg.WriteInt32(key, "Action", action);
                WinReg.WriteInt32(key, "TopMost", topMost);
            }
        }
        #endregion
    }
    public class TextData
    {
        public TextData(string text, object data)
        {
            Text = text;
            Data = data;
        }
        public override string ToString()
        {
            return Text;
        }
        public string Text { get; set; }
        public object Data { get; set; }
    }

    public static class Extensions
    {
        public static T FindFirst<T>(this System.Collections.IList iList, Func<T, bool> filter = null)
        {
            int count = iList.Count;
            bool hasFilter = (filter != null);
            for (int i = 0; i < count; i++)
            {
                if (iList[i] is T item && (!hasFilter || (hasFilter && filter(item)))) return item;
            }
            return default;
        }
    }
}
