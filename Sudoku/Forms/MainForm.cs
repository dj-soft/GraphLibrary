using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Games.Animated
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializeMenuContent();
        }
        #region Menu, Buttons, Content
        private void InitializeMenuContent()
        {
            __ContentPanel = new Panel() { Dock = DockStyle.Fill };
            Controls.Add(__ContentPanel);

            __MenuPanel = new Panel() { Height = 78, Dock = DockStyle.Top };
            Controls.Add(__MenuPanel);

            int x = 1;
            _SnakeCreateButton(ref x);
            _SudokuCreateButton(ref x);
            _SpinnerCreateButton(ref x);
            _ClearCreateButton(ref x);

            _ContentClear();
            _DoLayout();

            __MenuPanel.ClientSizeChanged += _MenuSizeChanged;
            this.ClientSizeChanged += _MenuSizeChanged;
        }
        private void _MenuSizeChanged(object sender, EventArgs e)
        {
            _DoLayout();
        }
        private void _DoLayout()
        {
            
            if (__ContentPanel != null && __ClearButton != null)
                __ClearButton.Left = __ContentPanel.ClientSize.Width - 2 - __ClearButton.Width;
        }
        private Button _CreateMenuButton(ref int x, string text, Image image, EventHandler click)
        {
            var height = __MenuPanel.Height - 2;
            var button = new Button()
            {
                Bounds = new Rectangle(x, 1, 170, height),
                Image = image,
                Text = text,
                TextAlign = ContentAlignment.MiddleRight,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.BorderColor = Color.DarkGray;

            button.Click += click;
            __MenuPanel.Controls.Add(button);
            x += button.Width + 1;
            return button;
        }
        Panel __MenuPanel;
        Panel __ContentPanel;
        private bool _HasContent;
        private void _AddContent(Control control)
        {
            __ContentPanel.Controls.Add(control);
            __ClearButton.Image = DjSoft.Games.Animated.Properties.Resources.Gloss_PNGStandby;
            __ClearButton.Text = "Clear";
            _HasContent = true;
        }
        private void _HighlightButton(Button button)
        {
            foreach (var control in __MenuPanel.Controls)
            {
                if (control is Button b)
                {
                    bool isHighlight = (Object.ReferenceEquals(b, button));
                    b.FlatAppearance.BorderSize = (isHighlight ? 2 : 0);
                }
            }
        }
        #endregion
        #region Clear / Exit
        private void _ClearCreateButton(ref int x)
        {
            x += 50;
            __ClearButton = _CreateMenuButton(ref x, "Clear", DjSoft.Games.Animated.Properties.Resources.Gloss_PNGShutdown_Quit_Dock, _ClearClick);
        }
        private void _ClearClick(object sender, EventArgs args)
        {
            _HighlightButton(__ClearButton);
            if (_HasContent)
                _ContentClear();
            else
                this.Close();
        }
        private void _ContentClear()
        {
            __ContentPanel.Controls.Clear();
            _SnakeRemove();
            _SudokuRemove();
            _SpinnerRemove();
            __ClearButton.Image = DjSoft.Games.Animated.Properties.Resources.Gloss_PNGShutdown_Quit_Dock;
            __ClearButton.Text = "Exit";
            _HasContent = false;
        }
        Button __ClearButton;
        #endregion
        #region Snake
        private void _SnakeCreateButton(ref int x)
        {
            __SnakeButton = _CreateMenuButton(ref x, "Snake", DjSoft.Games.Animated.Properties.Resources.macromedia_luiscds_dreamweaver2004_128, _SnakeClick);
        }
        private void _SnakeClick(object sender, EventArgs args)
        {
            _ContentClear();
            _HighlightButton(__SnakeButton);
            __SnakeControl = new Snake.SnakeControl() { Dock = DockStyle.Fill };
            _AddContent(__SnakeControl);
        }
        private void _SnakeRemove()
        {
            __SnakeControl?.Dispose();
            __SnakeControl = null;
        }
        Button __SnakeButton;
        Snake.SnakeControl __SnakeControl;
        #endregion
        #region Sudoku
        private void _SudokuCreateButton(ref int x)
        {
            __SudokuButton = _CreateMenuButton(ref x, "Sudoku", DjSoft.Games.Animated.Properties.Resources.macromedia_luiscds_course_builder128x128, _SudokuClick);
        }
        private void _SudokuClick(object sender, EventArgs args)
        {
            _ContentClear();
            _HighlightButton(__SudokuButton);
            __SudokuControl = new Sudoku.SudokuControl() { Dock = DockStyle.Fill };
            _AddContent(__SudokuControl);
        }
        private void _SudokuRemove()
        {
            __SudokuControl?.Dispose();
            __SudokuControl = null;
        }
        Button __SudokuButton;
        Sudoku.SudokuControl __SudokuControl;
        #endregion
        #region Spinner
        private void _SpinnerCreateButton(ref int x)
        {
            __SpinnerButton = _CreateMenuButton(ref x, "Spinner", DjSoft.Games.Animated.Properties.Resources.macromedia_luiscds_flash5_128x128, _SpinnerClick);
        }
        private void _SpinnerClick(object sender, EventArgs args)
        {
            _ContentClear();
            _HighlightButton(__SpinnerButton);
            __SpinnerControl = new Gadgets.SpinnerControl() { Dock = DockStyle.Fill };
            _AddContent(__SpinnerControl);
        }
        private void _SpinnerRemove()
        {
            __SpinnerControl?.Dispose();
            __SpinnerControl = null;
        }
        Button __SpinnerButton;
        Gadgets.SpinnerControl __SpinnerControl;
        #endregion
        #region Windows Form Designer generated code
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.ControlBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Sud oku";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
