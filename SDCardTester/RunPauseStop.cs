using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace DjSoft.Tools.SDCardTester
{
    /// <summary>
    /// Control, který obsahuje vizuální tlačítka Run - Pause - Stop s řízením běhu
    /// </summary>
    public class RunPauseStop : Panel
    {
        public RunPauseStop()
        {
            Init();
        }
        private void Init()
        {
            int x = 5;
            _RunButton = createButton(DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_start_2, RunState.Run);
            _PauseButton = createButton(DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_pause_2, RunState.Pause);
            _StopButton = createButton(DjSoft.Tools.SDCardTester.Properties.Resources.media_playback_stop_2, RunState.Stop);

            this.Size = new Size(200, 32);
            this.MinimumSize = new Size(125, 28);

            this.__ButtonHeight = 28;
            this.__ButtonAlignment = ContentAlignment.MiddleCenter;
            this.DoLayout();

            this._ActivateState(RunState.Run, false);

            Button createButton(Image image, RunState runState)
            {
                Button button = new Button()
                {
                    Bounds = new Rectangle(x, 0, 40, 28),
                    Text = "",
                    Image = image,
                    Tag = runState
                };
                button.Click += _ButtonClick;
                x += 42;
                this.Controls.Add(button);
                return button;
            }
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            // DoLayout();
        }
        private void DoLayout()
        {
            if (_RunButton is null || _PauseButton is null || _StopButton is null) return;

            var innerSize = this.ClientSize;

            int innerHeight = innerSize.Height - 4;
            int buttonHeight = __ButtonHeight;
            if (buttonHeight > innerHeight) buttonHeight = innerHeight;

            int innerWidth = innerSize.Width - 4;
            int buttonWidth = 40 * buttonHeight / 28;
            int buttonSpace = 3;
            int contentWidth = 3 * buttonWidth + 2 * buttonSpace;
            if (contentWidth > innerWidth)
            {
                buttonWidth = (innerWidth - 2 * buttonSpace) / 3;
                contentWidth = 3 * buttonWidth + 2 * buttonSpace;
            }

            int x = 2;
            int dx = innerWidth - contentWidth;
            int y = 2;
            int dy = innerHeight - buttonHeight;

            var alignment = __ButtonAlignment;
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                    break;
                case ContentAlignment.TopCenter:
                    x += dx / 2;
                    break;
                case ContentAlignment.TopRight:
                    x += dx;
                    break;
                case ContentAlignment.MiddleLeft:
                    y += dy / 2;
                    break;
                case ContentAlignment.MiddleCenter:
                    x += dx / 2;
                    y += dy / 2;
                    break;
                case ContentAlignment.MiddleRight:
                    x += dx;
                    y += dy / 2;
                    break;
                case ContentAlignment.BottomLeft:
                    y += dy;
                    break;
                case ContentAlignment.BottomCenter:
                    x += dx / 2;
                    y += dy;
                    break;
                case ContentAlignment.BottomRight:
                    x += dx;
                    y += dy;
                    break;
            }

            _RunButton.Visible = true;
            _RunButton.Bounds = new Rectangle(x, y, buttonWidth, buttonHeight);
            x += (buttonWidth + buttonSpace);
            _PauseButton.Visible = true;
            _PauseButton.Bounds = new Rectangle(x, y, buttonWidth, buttonHeight);
            x += (buttonWidth + buttonSpace);
            _StopButton.Visible = true;
            _StopButton.Bounds = new Rectangle(x, y, buttonWidth, buttonHeight);

            _ActivateState(__State, false);
        }
        private void _ButtonClick(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is RunState state)
                this._ActivateState(state, true);
        }

        private Button _RunButton;
        private Button _PauseButton;
        private Button _StopButton;

        /// <summary>
        /// Zarovnání buttonů
        /// </summary>
        public ContentAlignment ButtonAlignment
        {
            get { return __ButtonAlignment; }
            set { __ButtonAlignment = value; DoLayout(); }
        }
        private ContentAlignment __ButtonAlignment;
        /// <summary>
        /// Výška buttonů
        /// </summary>
        public int ButtonHeight
        {
            get { return __ButtonHeight; }
            set { __ButtonHeight = (value < 25 ? 25 : (value > 100 ? 100 : value)); DoLayout(); }
        }
        private int __ButtonHeight;

        /// <summary>
        /// Aktuální stav.
        /// Lze setovat, aktualizuje se vzhled buttonů a při změně se vyvolá event <see cref="StateChanged"/>.
        /// </summary>
        public RunState State
        {
            get { return __State; }
            set { _ActivateState(value, true); }
        }
        /// <summary>
        /// Aktuální stav
        /// </summary>
        private RunState __State;
        /// <summary>
        /// Aktivuje daný stav
        /// </summary>
        /// <param name="state"></param>
        /// <param name="withEvent"></param>
        private void _ActivateState(RunState state, bool withEvent)
        {
            _RunButton.Enabled = (state == RunState.None || state == RunState.Pause);
            _PauseButton.Enabled = (state == RunState.None || state == RunState.Run);
            _StopButton.Enabled = (state == RunState.None || state == RunState.Run || state == RunState.Pause);

            if (state != __State)
            {
                __State = state;
                if (withEvent)
                {
                    OnStateChanged();
                    StateChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Po změně stavu
        /// </summary>
        protected virtual void OnStateChanged() { }
        /// <summary>
        /// Po změně stavu <see cref="State"/>
        /// </summary>
        public event EventHandler StateChanged;
    }
    /// <summary>
    /// Stav běhu akcí (Run - Pause - Stop)
    /// </summary>
    public enum RunState
    {
        None,
        Run,
        Pause,
        Stop
    }
}
