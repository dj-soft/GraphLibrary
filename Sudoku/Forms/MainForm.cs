using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjSoft.Games.Sudoku
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

            __MenuPanel = new Panel() { Height = 76, Dock = DockStyle.Top };
            Controls.Add(__MenuPanel);

            int x = 1;
            __SnakeButton = _CreateMenuButton(ref x, "Snake", DjSoft.Games.Sudoku.Properties.Resources.macromedia_luiscds_dreamweaver2004_128, _SnakeClick);


            x += 50;
            __ClearButton = _CreateMenuButton(ref x, "Clear", DjSoft.Games.Sudoku.Properties.Resources.Gloss_PNGShutdown_Quit_Dock, _ClearClick);
            _ContentClear();
        }
        private Button _CreateMenuButton(ref int x, string text, Image image, EventHandler click)
        {
            var button = new Button() { Bounds = new Rectangle(x, 1, 170, 74), Image = image, Text = text, TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleCenter, Padding = new Padding(0), Margin = new Padding(0) };
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.DarkGray;

            button.Click += click;
            __MenuPanel.Controls.Add(button);
            x += button.Width + 1;
            return button;
        }
        private void _ClearClick(object sender, EventArgs args)
        {
            if (_HasContent)
                _ContentClear();
            else
                this.Close();
        }
        Panel __MenuPanel;
        Panel __ContentPanel;
        Button __ClearButton;
        private bool _HasContent;
        private void _ContentClear()
        {
            __ContentPanel.Controls.Clear();
            _SnakeRemove();
            __ClearButton.Image = DjSoft.Games.Sudoku.Properties.Resources.Gloss_PNGShutdown_Quit_Dock;
            __ClearButton.Text = "Exit";
            _HasContent = false;
        }
        private void _AddContent(Control control)
        {
            __ContentPanel.Controls.Add(__SnakeControl);
            __ClearButton.Image = DjSoft.Games.Sudoku.Properties.Resources.Gloss_PNGStandby;
            __ClearButton.Text = "Clear";
            _HasContent = true;
        }
        #endregion
        #region Snake
        private void _SnakeClick(object sender, EventArgs args)
        {
            _ContentClear();
            __SnakeControl = new Components.SnakeControl() { Dock = DockStyle.Fill };
            _AddContent(__SnakeControl);
        }
        private void _SnakeRemove()
        {
            __SnakeControl?.Dispose();
            __SnakeControl = null;
        }
        Button __SnakeButton;
        Components.SnakeControl __SnakeControl;
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
