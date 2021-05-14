using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.XtraEditors;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel reprezentující DataForm - včetně záložek a scrollování
    /// </summary>
    public partial class DxDataForm : DxPanelControl
    {
        public DxDataForm()
        {
            ScrollPanel = new DxDataFormScrollPanel();
            this.Controls.Add(ScrollPanel);
           
        }

        internal DxDataFormScrollPanel ScrollPanel { get; set; }
        internal DxDataFormContentPanel ContentPanel { get { return this.ScrollPanel.Content; } }
        internal ControlCollection ContentControls { get { return this.ScrollPanel.Content.Controls; } }


        /// <summary>
        /// Permanentní pole Child prvků
        /// </summary>
        private List<System.Windows.Forms.Control> _Controls;
        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
            _Controls.Clear();
        }
    }
    /// <summary>
    /// Container, který se dokuje do parenta = jeho velikost je omezená, 
    /// a hostuje v sobě <see cref="DxDataFormContentPanel"/>, který má velikost odpovídající svému obsahu a tento Content je pak posouván uvnitř this panelu = Scroll obsahu.
    /// </summary>
    internal class DxDataFormScrollPanel : DxAutoScrollPanelControl
    {
        public DxDataFormScrollPanel()
        {
            Content = new DxDataFormContentPanel();
            this.Controls.Add(Content);
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
        protected override void OnVisibleBoundsChanged()
        {
            base.OnVisibleBoundsChanged();
            ContentViewChanged();
        }
        private void ContentViewChanged()
        {
            var vis = this.VisibleBounds;
        }
        internal DxDataFormContentPanel Content { get; set; }

        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
        }
    }
    /// <summary>
    /// Hostitelský panel pro jednotlivé Controly.
    /// Tento panel si udržuje svoji velikost odpovídající všem svým Controlům, 
    /// není Dock, není AutoScroll (to je jeho Parent = <see cref="DxDataFormScrollPanel"/>).
    /// </summary>
    internal class DxDataFormContentPanel : DxPanelControl
    {
        public DxDataFormContentPanel()
        {
            this.Dock = System.Windows.Forms.DockStyle.None;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.AutoScroll = false;
        }

        protected override void OnLayout(System.Windows.Forms.LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (levent.AffectedProperty == nameof(Bounds))
            { }
        }
        protected override void OnScroll(System.Windows.Forms.ScrollEventArgs se)
        {
            base.OnScroll(se);
        }
        protected override void OnScroll(object sender, XtraScrollEventArgs e)
        {
            base.OnScroll(sender, e);
        }

        /// <summary>
        /// Dispose prvku
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            this.DisposeContent();
            base.Dispose(disposing);
        }
    }
    #region Pouze pro testování, později smazat
    partial class DxDataForm 
    {   // Rozšíření standardu pro testy
        public void CreateSample(DxDataFormSample sample)
        {
            this.SuspendLayout();
            this.BeginInit();

            _Controls = new List<System.Windows.Forms.Control>();
            int x = 6;
            int y = 8;
            int maxX = 0;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    _Controls.Add(DxComponent.CreateDxLabel(x, y, 120, null, "Řádek " + (i + 1).ToString() + ":"));
                    x += 126;
                }
                if (sample.TextCount >= 1)
                {
                    _Controls.Add(DxComponent.CreateDxTextEdit(x, y - 3, 220, null));
                    x += 226;
                }
                if (sample.CheckCount >= 1)
                {
                    _Controls.Add(DxComponent.CreateDxCheckEdit(x, y, 120, null, "Volba " + (i + 1).ToString() + "a."));
                    x += 126;
                }
                if (sample.LabelCount >= 2)
                {
                    _Controls.Add(DxComponent.CreateDxLabel(x, y, 120, null, "Řádek " + (i + 1).ToString() + ":"));
                    x += 126;
                }
                if (sample.TextCount >= 2)
                {
                    _Controls.Add(DxComponent.CreateDxTextEdit(x, y - 3, 220, null));
                    x += 226;
                }
                if (sample.CheckCount >= 2)
                {
                    _Controls.Add(DxComponent.CreateDxCheckEdit(x, y, 120, null, "Volba " + (i + 1).ToString() + "b."));
                    x += 126;
                }
                if (x > maxX) maxX = x;
                y += 30;
            }

            ContentPanel.Bounds = new System.Drawing.Rectangle(0, 0, maxX + 6, y + 6);

            if (!sample.NoAddControlsToPanel)
            {
                ContentPanel.Controls.AddRange(_Controls.ToArray());
                if (sample.Add50ControlsToPanel)
                    RemoveSampleItems(50);
            }

            this.EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private void RemoveSampleItems(int percent)
        {
            Random rand = new Random();
            var removeControls = _Controls.Where(c => rand.Next(100) < percent).ToArray();
            foreach (var removeControl in removeControls)
                ContentPanel.Controls.Remove(removeControl);
        }
    }
    public class WfDataForm : System.Windows.Forms.Panel
    {
        public void CreateSample(DxDataFormSample sample)
        {
            this.SuspendLayout();

            _Controls = new List<System.Windows.Forms.Control>();
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    _Controls.Add(new System.Windows.Forms.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 1)
                {
                    _Controls.Add(new System.Windows.Forms.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 1)
                {
                    _Controls.Add(new System.Windows.Forms.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += 126;
                }
                if (sample.LabelCount >= 2)
                {
                    _Controls.Add(new System.Windows.Forms.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 2)
                {
                    _Controls.Add(new System.Windows.Forms.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 2)
                {
                    _Controls.Add(new System.Windows.Forms.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "b." });
                    x += 126;
                }
                y += 30;
            }

            if (!sample.NoAddControlsToPanel)
            {
                this.Controls.AddRange(_Controls.ToArray());
                if (sample.Add50ControlsToPanel)
                    RemoveSampleItems(50);
            }

            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private void RemoveSampleItems(int percent)
        {
            Random rand = new Random();
            var removeControls = _Controls.Where(c => rand.Next(100) < percent).ToArray();
            foreach (var removeControl in removeControls)
                this.Controls.Remove(removeControl);
        }
        private List<System.Windows.Forms.Control> _Controls;
        protected override void Dispose(bool disposing)
        {
            DisposeContent();
            base.Dispose(disposing);
        }
        protected void DisposeContent()
        {
            var controls = this.Controls.OfType<System.Windows.Forms.Control>().ToArray();
            foreach (var control in controls)
            {
                if (control != null && !control.IsDisposed && !control.Disposing)
                {
                    this.Controls.Remove(control);
                    control.Dispose();
                }
            }
            _Controls.Clear();
        }
    }
    public class DxDataFormSample
    {
        public DxDataFormSample()
        { }
        public DxDataFormSample(int labelCount, int textCount, int checkCount, int rowsCount, int pagesCount)
        {
            this.LabelCount = labelCount;
            this.TextCount = textCount;
            this.CheckCount = checkCount;
            this.RowsCount = rowsCount;
            this.PagesCount = pagesCount;
        }
        public int LabelCount { get; set; }
        public int TextCount { get; set; }
        public int CheckCount { get; set; }
        public int RowsCount { get; set; }
        public int PagesCount { get; set; }
        public bool NoAddControlsToPanel { get; set; }
        public bool Add50ControlsToPanel { get; set; }
    }
    #endregion
}
