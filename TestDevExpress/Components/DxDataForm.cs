using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using DevExpress.XtraEditors;
using WF = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Panel reprezentující DataForm - včetně záložek a scrollování
    /// </summary>
    public partial class DxDataForm : DxPanelControl
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DxDataForm()
        {
            ScrollPanel = new DxDataFormScrollPanel(this);
            this.Controls.Add(ScrollPanel);
           
        }
        /// <summary>
        /// Režim práce s pamětí
        /// </summary>
        public DxDataFormMemoryMode MemoryMode { get; set; }

        /// <summary>
        /// Vrátí true pokud se control (s danými souřadnicemi) má brát jako viditelný v dané oblasti.
        /// Tato metoda může provádět optimalizaci v tom, že jako "viditelné" určí i controly nedaleko od reálně viditelné souřadnice.
        /// </summary>
        /// <param name="controlBounds"></param>
        /// <param name="visibleBounds"></param>
        /// <returns></returns>
        internal bool IsInVisibleBounds(Rectangle? controlBounds, Rectangle visibleBounds)
        {
            if (!controlBounds.HasValue) return false;
            int distX = 90;                                // Vzdálenost na ose X, kterou akceptujeme jako viditelnou 
            int distY = 60;                                //  ...Y... = (=rezerva okolo viditelné oblasti, kde už máme připravené fyzické controly)
            var cb = controlBounds.Value;

            if ((cb.Bottom + distY) < visibleBounds.Y) return false;
            if ((cb.Y - distY) > visibleBounds.Bottom) return false;

            if ((cb.Right + distX) < visibleBounds.X) return false;
            if ((cb.X - distX) > visibleBounds.Right) return false;

            return true;
        }


        //   NUTNO PŘEPACOVAT NA ZÁLOŽKOVNÍK A TEDY VÍCE PANELŮ!

        internal DxDataFormScrollPanel ScrollPanel { get; set; }
        internal DxDataFormContentPanel ContentPanel { get { return this.ScrollPanel.ContentPanel; } }

        internal void AddItems(IEnumerable<IDataFormItem> items)
        {
            ScrollPanel.AddItems(items);
        }
        internal void AddItem(IDataFormItem item)
        {
            ScrollPanel.AddItem(item);
        }

        internal void AddControls(IEnumerable<WF.Control> controls)
        {
            ScrollPanel.AddControls(controls);
        }
        internal void AddControl(WF.Control control)
        {
            ScrollPanel.AddControl(control);
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
    /// <summary>
    /// Container, který se dokuje do parenta = jeho velikost je omezená, 
    /// a hostuje v sobě <see cref="DxDataFormContentPanel"/>, který má velikost odpovídající svému obsahu a tento Content je pak posouván uvnitř this panelu = Scroll obsahu.
    /// </summary>
    internal class DxDataFormScrollPanel : DxAutoScrollPanelControl
    {
        public DxDataFormScrollPanel(DxDataForm dxDataForm)
        {
            __DxDataForm = dxDataForm;
            ContentPanel = new DxDataFormContentPanel();
            this.Controls.Add(ContentPanel);
            this.Dock = WF.DockStyle.Fill;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            Items = new List<DxDataFormControlItem>();
        }
        private DxDataForm __DxDataForm;
        protected override void OnVisibleBoundsChanged()
        {
            base.OnVisibleBoundsChanged();
            ContentViewChanged();
        }
        private void ContentViewChanged()
        {
            if (this.ContentPanel == null) return;                   // Voláno v procesu konstruktoru
            this.ContentPanel.ContentVisibleBounds = this.VisibleBounds;
            RefreshVisibleItems();
        }
        internal DxDataFormContentPanel ContentPanel { get; private set; }

        internal void AddItems(IEnumerable<IDataFormItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
                _AddItem(item);
            RefreshContentSize();
            RefreshVisibleItems();
        }
        internal void AddItem(IDataFormItem item)
        {
            _AddItem(item);
            RefreshContentSize();
            RefreshVisibleItems();
        }
        private void _AddItem(IDataFormItem item)
        {
            Items.Add(new DxDataFormControlItem(__DxDataForm, this.ContentPanel, item));
        }
        internal void AddControls(IEnumerable<WF.Control> controls)
        {
            if (controls == null) return;
            foreach (var control in controls)
                _AddControl(control);
            RefreshContentSize();
            RefreshVisibleItems();
        }
        internal void AddControl(WF.Control control)
        {
            _AddControl(control);
            RefreshContentSize();
            RefreshVisibleItems();
        }
        private void _AddControl(WF.Control control)
        {
            Items.Add(new DxDataFormControlItem(__DxDataForm, this.ContentPanel, control));
        }
        internal void RefreshContentSize()
        {
            int maxR = 0;
            int maxB = 0;
            int tabIndex = 0;
            foreach (var item in Items)
            {
                item.TabIndex = tabIndex++;
                var bounds = item.Bounds;
                if (bounds.HasValue)
                {
                    if (bounds.Value.Right > maxR) maxR = bounds.Value.Right;
                    if (bounds.Value.Bottom > maxB) maxB = bounds.Value.Bottom;
                }
            }
            maxR += 6;
            maxB += 6;
            this.ContentPanel.Bounds = new Rectangle(0, 0, maxR, maxB);
        }

        internal void RefreshVisibleItems()
        {
            var visibleBounds = this.VisibleBounds;
            foreach (var item in Items)
                item.RefreshVisibleItem(visibleBounds);
        }
        internal List<DxDataFormControlItem> Items { get; private set; }

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
            this.Dock = WF.DockStyle.None;
            this.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.AutoScroll = false;
        }

        public Rectangle ContentVisibleBounds { get { return _ContentVisibleBounds; } set { _SetContentVisibleBounds(value); } }
        private Rectangle _ContentVisibleBounds;
        private void _SetContentVisibleBounds(Rectangle contentVisibleBounds)
        {
            if (contentVisibleBounds == _ContentVisibleBounds) return;

            _ContentVisibleBounds = contentVisibleBounds;
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
    /// <summary>
    /// <see cref="DxDataFormControlItem"/> : Třída obsahující každý jeden prvek controlu v rámci DataFormu:
    /// jeho definici <see cref="IDataFormItem"/> i fyzický control.
    /// Umožňuje řešit jeho tvorbu a uvolnění OnDemand = podle viditelnosti v rámci Parenta.
    /// Šetří tak čas a paměťové nároky.
    /// </summary>
    internal class DxDataFormControlItem
    {
        public DxDataFormControlItem(DxDataForm dxDataForm, DxDataFormContentPanel hostPanel, WF.Control control)
        {
            if (control is null)
                throw new ArgumentNullException("control", "DxDataFormControlItem(control) is null.");

            __DataForm = dxDataForm;
            __HostPanel = hostPanel;
            __Control = control;
            __ControlIsExternal = true;
        }
        public DxDataFormControlItem(DxDataForm dxDataForm, DxDataFormContentPanel hostPanel, IDataFormItem dataFormItem)
        {
            if (dataFormItem is null)
                throw new ArgumentNullException("dataFormItem", "DxDataFormControlItem(dataFormItem) is null.");

            __DataForm = dxDataForm;
            __HostPanel = hostPanel;
            __DataFormItem = dataFormItem;
            __ControlIsExternal = false;
        }

        public DxDataForm DataForm { get { return __DataForm; } }
        private DxDataForm __DataForm;
        public DxDataFormContentPanel HostPanel { get { return __HostPanel; } }
        private DxDataFormContentPanel __HostPanel;
        public WF.Control Control { get { return __Control; } }
        private WF.Control __Control;
        /// <summary>
        /// Obsahuje true tehdy, když zdejší Control je dodán externě. Pak jej nemůžeme Disposovat a znovuvytvářet, ale musíme jej držet permanentně.
        /// </summary>
        private bool __ControlIsExternal;
        public IDataFormItem DataFormItem { get { return __DataFormItem; } }
        private IDataFormItem __DataFormItem;
        /// <summary>
        /// Index prvku pro procházení přes TAB
        /// </summary>
        public int TabIndex { get; set; }
        /// <summary>
        /// Souřadnice zjištěné primárně z <see cref="DataFormItem"/>, sekundárně z <see cref="Control"/>. Nebo null.
        /// </summary>
        public Rectangle? Bounds 
        {
            get 
            {
                if (__DataFormItem != null) return __DataFormItem.Bounds;
                if (__Control != null) return __Control.Bounds;
                return null;
            }
        }
        /// <summary>
        /// Obsahuje true pro prvek, který je aktuálně umístěn ve viditelném panelu
        /// </summary>
        public bool IsHosted { get; private set; }
        /// <summary>
        /// Zajistí, že this prvek bude zobrazen podle toho, zda se nachází v dané viditelné oblasti
        /// </summary>
        /// <param name="visibleBounds"></param>
        internal void RefreshVisibleItem(Rectangle visibleBounds)
        {
            var controlBounds = this.Bounds;
            bool isVisible = __DataForm.IsInVisibleBounds(controlBounds, visibleBounds); //.HasValue && IsInVisibleBounds( Rectangle.Intersect(visibleBounds, controlBounds.Value).HasPixels());
            bool isHosted = IsHosted && (__Control != null);

            if (isVisible)
            {
                if (!isHosted)
                {
                    WF.Control control = GetOrCreateControl();
                    __HostPanel.Controls.Add(control);
                    IsHosted = true;
                }
                RefreshItemValues();
            }
            else if (isHosted && !isVisible)
            {
                WF.Control control = __Control;
                __HostPanel.Controls.Remove(control);
                IsHosted = false;
                ReleaseControl(control);
            }



        }
        /// <summary>
        /// Aktualizuje hodnoty na controlu, který je právě viditelný
        /// </summary>
        private void RefreshItemValues()
        {
            if (__Control == null) return;
            if (__Control.TabIndex != this.TabIndex) __Control.TabIndex = this.TabIndex;
            if (this.__DataFormItem != null && __Control.Bounds != this.__DataFormItem.Bounds) __Control.Bounds = this.__DataFormItem.Bounds;
        }
        /// <summary>
        /// Najde nebo vytvoří nový vizuální control a vrátí jej
        /// </summary>
        /// <returns></returns>
        private WF.Control GetOrCreateControl()
        {
            WF.Control control = __Control;
            if (control == null || control.IsDisposed)
            {
                __Control = DxComponent.CreateDataFormControl(__DataFormItem);
                // Navázat eventy controlu k nám:
                RegisterControlEvents(__Control);
                control = __Control;
            }
            return control;
        }

        private void RegisterControlEvents(WF.Control control)
        {
            
        }

        private void ReleaseControl(WF.Control control)
        {
            if (control == null || control.IsDisposed || control.Disposing) return;
            var memoryMode = __DataForm.MemoryMode;

            if (memoryMode.HasFlag(DxDataFormMemoryMode.RemoveReleaseHandle))
            {
                if (control != null && control.IsHandleCreated && !control.RecreatingHandle)
                    DestroyWindow(control.Handle);
            }

            if (!__ControlIsExternal && memoryMode.HasFlag(DxDataFormMemoryMode.RemoveDispose))
            {
                try { control.Dispose(); }
                catch { }
                __Control = null;
            }

        }
        [DllImport("User32")]
        private static extern int DestroyWindow(IntPtr hWnd);
        private void _DestroyHandle(WF.Control control)
        {
            if (control != null && control.IsHandleCreated && !control.RecreatingHandle)
            {
                DestroyWindow(control.Handle);
            }
        }
    }
    /// <summary>
    /// Deklarace každého jednoho prvku v rámci DataFormu
    /// </summary>
    public class DataFormItem : IDataFormItem
    {
        public string ItemName { get; set; }
        public int? TabIndex { get; set; }
        public int PageId { get; set; }
        public string PageText { get; set; }
        public string PageIconName { get; set; }
        public DataFormItemType ItemType { get; set; }
        public Rectangle Bounds { get; set; }
        public string Text { get; set; }
        public bool? Visible { get; set; }
        public DevExpress.XtraEditors.Controls.BorderStyles? BorderStyle { get; set; }
        public LabelStyleType? LabelStyle { get; set; }
        public DevExpress.Utils.WordWrap? LabelWordWrap { get; set; }
        public DevExpress.XtraEditors.LabelAutoSizeMode? LabelAutoSize { get; set; }
        public DevExpress.Utils.HorzAlignment? LabelHAlignment { get; set; }
        public DevExpress.XtraEditors.Mask.MaskType? TextMaskType { get; set; }
        public string TextEditMask { get; set; }
        public bool? TextUseMaskAsDisplayFormat { get; set; }
        public DevExpress.XtraEditors.Controls.CheckBoxStyle? CheckBoxStyle { get; set; }
        public decimal? SpinMinValue { get; set; }
        public decimal? SpinMaxValue { get; set; }
        public decimal? SpinIncrement { get; set; }
        public DevExpress.XtraEditors.Controls.SpinStyles? SpinStyle { get; set; }
        public string ToolTipTitle { get; set; }
        public string ToolTipText { get; set; }
        public bool? Enabled { get; set; }
        public bool? ReadOnly { get; set; }
        public bool? TabStop { get; set; }


    }
    public interface IDataFormItem
    {
        string ItemName { get; }
        int? TabIndex { get; }
        int PageId { get; }
        string PageText { get; }
        string PageIconName { get; }
        DataFormItemType ItemType { get; }
        Rectangle Bounds { get; }
        string Text { get; }
        bool? Visible { get; }
        DevExpress.XtraEditors.Controls.BorderStyles? BorderStyle { get; }
        LabelStyleType? LabelStyle { get; }
        DevExpress.Utils.WordWrap? LabelWordWrap { get; }
        DevExpress.XtraEditors.LabelAutoSizeMode? LabelAutoSize { get; }
        DevExpress.Utils.HorzAlignment? LabelHAlignment { get; }
        DevExpress.XtraEditors.Mask.MaskType? TextMaskType { get; } 
        string TextEditMask { get; } 
        bool? TextUseMaskAsDisplayFormat { get; }
        DevExpress.XtraEditors.Controls.CheckBoxStyle? CheckBoxStyle { get; }
        decimal? SpinMinValue { get; }
        decimal? SpinMaxValue { get; }
        decimal? SpinIncrement { get; }
        DevExpress.XtraEditors.Controls.SpinStyles? SpinStyle { get; }
        string ToolTipTitle { get; } 
        string ToolTipText { get; }
        bool? Enabled { get; }
        bool? ReadOnly { get; } 
        bool? TabStop { get; }
    }

    /// <summary>
    /// Druh prvku v DataFormu
    /// </summary>
    public enum DataFormItemType
    {
        None = 0,
        Label,
        TextBox,
        EditBox,
        SpinnerBox,
        CheckBox,
        BreadCrumb,
        ComboBoxList,
        ComboBoxEdit,
        ListView,
        TreeView,
        RadioButtonBox,
        Button,
        DropDownButton,
        Image
    }
    /// <summary>
    /// Režim práce při zobrazování controlů v <see cref="DxDataForm"/>
    /// </summary>
    [Flags]
    public enum DxDataFormMemoryMode
    {
        /// <summary>
        /// Controly vůbec nezobrazovat = pouze pro testy paměti
        /// </summary>
        None = 0,
        /// <summary>
        /// Do parent containeru (Host) vkládat pouze controly ve viditelné oblasti, a po opuštění viditelné oblasti zase Controly z parenta odebírat
        /// </summary>
        HostOnlyVisible = 0x01,
        /// <summary>
        /// Do parent containeru vkládat vždy všechny controly a nechat je tam stále
        /// </summary>
        HostAllways = 0x02,
        /// <summary>
        /// Po odebrání controlu z parent containeru (Host) uvolnit handle pomocí User32.DestroyWindow()
        /// </summary>
        RemoveReleaseHandle = 0x10,
        /// <summary>
        /// Po odebrání controlu z parent containeru (Host) uvolnit handle pomocí User32.DestroyWindow() a samotný Control disposovat (v případě znovu potřeby bude vygenerován nový podle předpisu)
        /// </summary>
        RemoveDispose = 0x20,
        /// <summary>
        /// Optimální default pro runtime
        /// </summary>
        Default = HostOnlyVisible | RemoveReleaseHandle | RemoveDispose
    }
    #region Pouze pro testování, později smazat
    partial class DxDataForm 
    {   // Rozšíření standardu pro testy
        public void CreateSample(DxDataFormSample sample)
        {
            List<IDataFormItem> items = new List<IDataFormItem>();
            Random rand = new Random();
            DevExpress.XtraEditors.Controls.CheckBoxStyle[] styles = new DevExpress.XtraEditors.Controls.CheckBoxStyle[]
            {
                DevExpress.XtraEditors.Controls.CheckBoxStyle.Default,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.Radio,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.CheckBox,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgCheckBox1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgFlag1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgFlag2,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgHeart1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgHeart2,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgLock1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgRadio1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgRadio2,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgStar1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgStar2,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgThumb1,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1,
            };

            int w;
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 1)
                {
                    w = rand.Next(180, 350);
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 1)
                {
                    w = rand.Next(200, 250);
                    var style = styles[rand.Next(styles.Length)];
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.CheckBox, CheckBoxStyle = style, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a. (" + style.ToString() + ")"});
                    x += w + 6;
                }
                if (sample.LabelCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.Label, LabelHAlignment = DevExpress.Utils.HorzAlignment.Far, LabelAutoSize = LabelAutoSizeMode.None, Bounds = new Rectangle(x, y, w, 20), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += w + 6;
                }
                if (sample.TextCount >= 2)
                {
                    w = rand.Next(250, 450);
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.TextBox, Bounds = new Rectangle(x, y, w, 20) });
                    x += w + 6;
                }
                if (sample.CheckCount >= 2)
                {
                    w = rand.Next(100, 200);
                    items.Add(new DataFormItem() { ItemType = DataFormItemType.CheckBox, Bounds = new Rectangle(x, y, w, 20), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += w + 6;
                }
                y += 30;
            }

            this.AddItems(items);
        }
    }
    public class WfDataForm : WF.Panel
    {
        public void CreateSample(DxDataFormSample sample)
        {
            this.SuspendLayout();

            _Controls = new List<WF.Control>();
            int x = 6;
            int y = 8;
            for (int i = 0; i < sample.RowsCount; i++)
            {
                x = 6;
                if (sample.LabelCount >= 1)
                {
                    _Controls.Add(new WF.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 1)
                {
                    _Controls.Add(new WF.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 1)
                {
                    _Controls.Add(new WF.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "a." });
                    x += 126;
                }
                if (sample.LabelCount >= 2)
                {
                    _Controls.Add(new WF.Label() { Bounds = new System.Drawing.Rectangle(x, y, 120, 17), Text = "Řádek " + (i + 1).ToString() + ":" });
                    x += 126;
                }
                if (sample.TextCount >= 2)
                {
                    _Controls.Add(new WF.TextBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 220, 17) });
                    x += 226;
                }
                if (sample.CheckCount >= 2)
                {
                    _Controls.Add(new WF.CheckBox() { Bounds = new System.Drawing.Rectangle(x, y - 3, 120, 17), Text = "Volba " + (i + 1).ToString() + "b." });
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
        private List<WF.Control> _Controls;
        protected override void Dispose(bool disposing)
        {
            DisposeContent();
            base.Dispose(disposing);
        }
        protected void DisposeContent()
        {
            var controls = this.Controls.OfType<WF.Control>().ToArray();
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
