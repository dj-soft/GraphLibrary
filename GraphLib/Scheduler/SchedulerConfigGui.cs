using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using WinForms = System.Windows.Forms;
using System.ComponentModel;

using Asol.Tools.WorkScheduler.Components;
using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class SchedulerConfig, GUI část
    public partial class SchedulerConfig
    {
        #region EditorItems : vizuální prvky pro formuláře Konfigurace
        /// <summary>
        /// Metoda vrací pole prvků, které slouží k editaci konfigurace.
        /// </summary>
        /// <returns></returns>
        public SchedulerEditorItem[] CreateEditorItems()
        {
            List<SchedulerEditorItem> itemList = new List<SchedulerEditorItem>();

            // Vkládám jednotlivé prvky: jejich titulek = text v Tree, jejich ToolTip, a jejich instanci editoru:
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapMouse, "", new ConfigSnapSetPanel() { Data = this.MoveSnapMouse, Caption = "Přichycení při přetahování prvku, bez klávesnice" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapCtrl, "", new ConfigSnapSetPanel() { Data = this.MoveSnapCtrl, Caption = "Přichycení při přetahování prvku, s klávesou CTRL" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapShift, "", new ConfigSnapSetPanel() { Data = this.MoveSnapShift, Caption = "Přichycení při přetahování prvku, s klávesou SHIFT" }));
            itemList.Add(new SchedulerEditorItem(EditTitle_Snap + EditTitle_Separator + EditTitle_SnapCtrlShift, "", new ConfigSnapSetPanel() { Data = this.MoveSnapCtrlShift, Caption = "Přichycení při přetahování prvku, s klávesou CTRL + SHIFT" }));

            itemList.Add(new SchedulerEditorItem(EditTitle_LinkLine, "", new ConfigLinkLinesSetPanel() { Data = this, Caption = "Zobrazení spojovacích linií v grafech" }));

            return itemList.ToArray();
        }
        /// <summary>
        /// Oddělovač úrovní jednotlivých nodů v kompletním názvu nodu
        /// </summary>
        public static string EditTitle_Separator { get { return "\\"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Přichycení při přetahování"
        /// </summary>
        protected static string EditTitle_Snap { get { return "Přichycení při přetahování"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Přetahování myší"
        /// </summary>
        protected static string EditTitle_SnapMouse { get { return "Přetahování myší"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Se stisknutým CTRL"
        /// </summary>
        protected static string EditTitle_SnapCtrl { get { return "Se stisknutým CTRL"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Se stisknutým SHIFT"
        /// </summary>
        protected static string EditTitle_SnapShift { get { return "Se stisknutým SHIFT"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Se stisknutým CTRL+SHIFT"
        /// </summary>
        protected static string EditTitle_SnapCtrlShift { get { return "Se stisknutým CTRL+SHIFT"; } }
        /// <summary>
        /// Text nodu v stromu konfigurace: "Spojovací linie"
        /// </summary>
        protected static string EditTitle_LinkLine { get { return "Spojovací linie"; } }
        #endregion
    }
    #endregion
    #region class SchedulerEditorItem : jedna položka v editoru konfigurace
    /// <summary>
    /// SchedulerEditorItem : jedna položka v editoru konfigurace
    /// </summary>
    public class SchedulerEditorItem
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="nodeText"></param>
        /// <param name="nodeToolTip"></param>
        /// <param name="visualControl"></param>
        public SchedulerEditorItem(string nodeText, string nodeToolTip, ISchedulerEditorControlItem visualControl)
        {
            this.NodeText = nodeText;
            this.NodeToolTip = nodeToolTip;
            this.VisualControl = visualControl;
        }
        /// <summary>
        /// Text položky konfigurace.
        /// Pokud bude obsahovat zpětné lomítko, jde o oddělovač v stromové hierarchii.
        /// </summary>
        public string NodeText { get; set; }
        /// <summary>
        /// ToolTip k položce stromu konfigurace
        /// </summary>
        public string NodeToolTip { get; set; }
        /// <summary>
        /// Vizuální control, který zobrazuje a edituje konfigurační data
        /// </summary>
        public ISchedulerEditorControlItem VisualControl { get; set; }
    }
    /// <summary>
    /// Předpis pro objekty, které mohou hrát roli editoru jedné položky konfigurace.
    /// Objekt musí umět načíst hodnoty z configu do vizuálních prvků <see cref="ReadFromData()"/>;
    /// uložit hodnoty z vizuálních prvků do configu <see cref="SaveToData()"/>;
    /// a poskytnout vizuální objekt pro zobrazování <see cref="Panel"/>.
    /// </summary>
    public interface ISchedulerEditorControlItem
    {
        /// <summary>
        /// Objekt načte hodnoty z configu do vizuálních prvků
        /// </summary>
        void ReadFromData();
        /// <summary>
        /// Objekt uloží hodnoty z vizuálních prvků do configu
        /// </summary>
        void SaveToData();
        /// <summary>
        /// Vizuální control zobrazovaný pro tuto položku konfigurace
        /// </summary>
        System.Windows.Forms.Panel Panel { get; }
    }
    #endregion
    #region SchedulerConfigForm : Okno s konfigurací
    /// <summary>
    /// SchedulerConfigForm : Okno s konfigurací
    /// </summary>
    internal class SchedulerConfigForm : WinFormButtons
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace formu, virtuální metoda volaná z konstruktoru.
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// <para/>
        /// Třída <see cref="WinFormButtons"/> inicializuje prvky <see cref="WinFormButtons.ButtonsPanel"/> a <see cref="WinFormButtons.DataPanel"/>, a pole buttonů.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            this.InitComponents();
            this.InitData();
        }
        /// <summary>
        /// Vygeneruje komponenty, včetně konkrétního obsahu stromu konfigurací
        /// </summary>
        protected void InitComponents()
        {
            this.ConfigTree = new System.Windows.Forms.TreeView()
            {
                Dock = WinForms.DockStyle.Fill,
                BorderStyle = WinForms.BorderStyle.None,
                CheckBoxes = false,
                DrawMode = WinForms.TreeViewDrawMode.OwnerDrawText,
                FullRowSelect = true,
                HideSelection = false,
                HotTracking = false,
                Indent = 16,
                LabelEdit = false,
                Scrollable = true,
                ShowLines = true,
                ShowNodeToolTips = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
            };
            this.ConfigTree.DrawNode += ConfigTree_DrawNode;
            this.ConfigTree.AfterSelect += ConfigTree_AfterSelect;
            this.ConfigContainer = new WinForms.SplitContainer() { FixedPanel = WinForms.FixedPanel.Panel1, SplitterDistance = 270, Dock = WinForms.DockStyle.Fill };
            this.NodeFontInfoMain = FontInfo.CaptionBoldBig;
            this.NodeFontMain = this.NodeFontInfoMain.CreateNewFont();
            this.NodeFontInfoStandard = FontInfo.CaptionBold;
            this.NodeFontStandard = this.NodeFontInfoStandard.CreateNewFont();

            ((System.ComponentModel.ISupportInitialize)(this.ConfigContainer)).BeginInit();
            this.ConfigContainer.Panel1.SuspendLayout();
            this.ConfigContainer.Panel2.SuspendLayout();
            this.ConfigContainer.SuspendLayout();
            this.SuspendLayout();

            this.ConfigContainer.Panel1.Controls.Add(this.ConfigTree);

            this.DataPanel.Controls.Add(this.ConfigContainer);

            this.Text = "Nastavení";
            this.Buttons = Noris.LCS.Base.WorkScheduler.GuiDialogButtons.OkCancel;
            this.ButtonsAlignment = ContentAlignment.MiddleRight;
            this.CloseOnClick = false;

            this.ConfigContainer.Panel1.ResumeLayout(false);
            this.ConfigContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConfigContainer)).EndInit();
            this.ConfigContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        /// <summary>
        /// Obsluha stisknutí tlačítka: zakázali jsme provedení CloseOnClick, proto musíme tlačítka obsloužit ručně. Řešíme tím uložení dat.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnButtonClick(GPropertyEventArgs<GuiDialogButtons> args)
        {
            base.OnButtonClick(args);
            switch (args.Value)
            {
                case GuiDialogButtons.Ok:
                case GuiDialogButtons.Save:
                    this.SaveConfig();
                    this.Close();
                    break;
                case GuiDialogButtons.Cancel:
                    this.Close();
                    break;
            }
        }
        /// <summary>
        /// Vykreslí label jednoho node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigTree_DrawNode(object sender, WinForms.DrawTreeNodeEventArgs e)
        {
            System.Windows.Forms.TreeNode node = e.Node;

            Rectangle nodeBounds = e.Bounds;
            Size treeSize = this.ConfigTree.ClientSize;

            Rectangle itemBounds = new Rectangle(nodeBounds.X, nodeBounds.Y, treeSize.Width - nodeBounds.X, nodeBounds.Height);
            Brush backBrush = Skin.Brush(this.ConfigTree.BackColor);
            e.Graphics.FillRectangle(backBrush, itemBounds);

            Color nodeColor = (e.Node.IsSelected ? Color.LightBlue : this.ConfigTree.BackColor);
            Rectangle rowBounds = new Rectangle(nodeBounds.X, nodeBounds.Y + 1, treeSize.Width - nodeBounds.X, nodeBounds.Height - 2);
            Painter.DrawAreaBase(e.Graphics, rowBounds, nodeColor, WinForms.Orientation.Horizontal, GInteractiveState.Enabled);

            Rectangle textBounds = new Rectangle(nodeBounds.X + 3, nodeBounds.Y + 1, treeSize.Width - nodeBounds.X - 6, nodeBounds.Height - 2);
            FontInfo fontInfo = this.NodeFontInfoStandard;
            Painter.DrawString(e.Graphics, e.Node.Text, fontInfo, textBounds, ContentAlignment.MiddleLeft, color: this.ConfigTree.ForeColor);

            e.DrawDefault = false;
        }
        /// <summary>
        /// Při prvním zobrazení formu
        /// </summary>
        protected override void OnFirstShown()
        {
            this.ConfigContainer.SplitterDistance = 270;
            this.ConfigTree.FullRowSelect = true;
            this.ConfigTree.ShowRootLines = true;
            this.ConfigTree.ShowPlusMinus = true;
        }
        /// <summary>
        /// Hlavní container
        /// </summary>
        protected WinForms.SplitContainer ConfigContainer;
        /// <summary>
        /// Strom obsahující položky konfigurace
        /// </summary>
        protected WinForms.TreeView ConfigTree;
        /// <summary>
        /// Font pro hlavní úroveň nodů
        /// </summary>
        protected Font NodeFontMain;
        /// <summary>
        /// Font pro nižší úrovně nodů
        /// </summary>
        protected Font NodeFontStandard;
        /// <summary>
        /// FontInfo pro hlavní úroveň nodů
        /// </summary>
        protected FontInfo NodeFontInfoMain;
        /// <summary>
        /// FontInfo pro nižší úrovně nodů
        /// </summary>
        protected FontInfo NodeFontInfoStandard;
        #endregion
        #region Vazby na instanci SchedulerConfig
        /// <summary>
        /// Instance konfigurace, z níž se zobrazují a editují data
        /// </summary>
        protected SchedulerConfig Config
        {
            get { return this._Config; }
            set { this._Config = value; this._ReadData(); }
        }
        private SchedulerConfig _Config;
        /// <summary>
        /// Inicializace interních dat
        /// </summary>
        private void InitData()
        {
            this._TreeNodeDict = new Dictionary<string, System.Windows.Forms.TreeNode>();
        }
        /// <summary>
        /// Do jednotlivých controlů načte data z Configu
        /// </summary>
        private void _ReadData()
        {
            this.ConfigTree.Nodes.Clear();
            this._TreeNodeDict.Clear();
            this.ConfigContainer.Panel2.Controls.Clear();
            this._EditorItems = null;
            this._CurrentEditorItem = null;

            SchedulerConfig config = this._Config;
            if (config == null) return;

            this._EditorItems = config.CreateEditorItems();
            foreach (SchedulerEditorItem editorItem in this._EditorItems)
                this._CreateNodeData(editorItem);

            this.ConfigTree.ItemHeight = 140 * this.NodeFontMain.Height / 100;
        }
        /// <summary>
        /// Pro daný objekt editoru vytvoří node a zajistí další akce
        /// </summary>
        /// <param name="editorItem"></param>
        private void _CreateNodeData(SchedulerEditorItem editorItem)
        {
            System.Windows.Forms.TreeNode node = _SearchTreeNode(editorItem.NodeText);
            node.Tag = editorItem;

            editorItem.VisualControl.ReadFromData();

            WinForms.Panel panel = editorItem.VisualControl.Panel;
            panel.Visible = false;
            panel.Dock = WinForms.DockStyle.Fill;
            this.ConfigContainer.Panel2.Controls.Add(panel);
        }
        /// <summary>
        /// Najde nebo vytvoří node s daným názvem
        /// </summary>
        /// <param name="nodeText"></param>
        /// <returns></returns>
        private System.Windows.Forms.TreeNode _SearchTreeNode(string nodeText)
        {
            if (String.IsNullOrEmpty(nodeText)) return null;
            System.Windows.Forms.TreeNode node;
            if (!this._TreeNodeDict.TryGetValue(nodeText, out node))
            {
                string text = null;
                WinForms.TreeNodeCollection parentCollection = null;
                Font font = null;
                string parentText = nodeText.SplitOn(SchedulerConfig.EditTitle_Separator, true);
                if (parentText.Length == 0)
                {   // Zadaný vstupní text nodeText neobsahuje separátor = jde o Root prvek:
                    parentCollection = this.ConfigTree.Nodes;
                    text = nodeText;
                    font = this.NodeFontMain;
                }
                else
                {   // Máme text před separátorem = jde o parent našeho hledaného node, necháme si ho rekurzivně najít:
                    System.Windows.Forms.TreeNode parentNode = this._SearchTreeNode(parentText);
                    parentCollection = parentNode.Nodes;
                    text = nodeText.SplitOn(SchedulerConfig.EditTitle_Separator, true, true);
                    font = this.NodeFontStandard;
                }
                node = new System.Windows.Forms.TreeNode(text);
                node.NodeFont = font;
                parentCollection.Add(node);
                this._TreeNodeDict.Add(nodeText, node);
            }
            return node;
        }
        /// <summary>
        /// Zajistí aktivaci editoru pro daný node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigTree_AfterSelect(object sender, WinForms.TreeViewEventArgs e)
        {
            System.Windows.Forms.TreeNode node = e.Node;
            SchedulerEditorItem editorItem = node.Tag as SchedulerEditorItem;

            if (editorItem == null && node.Nodes.Count > 0)
                editorItem = node.Nodes[0].Tag as SchedulerEditorItem;

            if (this._CurrentEditorItem != null && (editorItem == null || !Object.ReferenceEquals(this._CurrentEditorItem, editorItem)))
            {
                this._CurrentEditorItem.VisualControl.Panel.Visible = false;
                this._CurrentEditorItem = null;
            }

            if (editorItem != null)
            {
                this._CurrentEditorItem = editorItem;
                if (!this._CurrentEditorItem.VisualControl.Panel.Visible)
                    this._CurrentEditorItem.VisualControl.Panel.Visible = true;
            }
        }
        /// <summary>
        /// Aktuální prvek editace
        /// </summary>
        private SchedulerEditorItem _CurrentEditorItem;
        /// <summary>
        /// Položky editoru
        /// </summary>
        private SchedulerEditorItem[] _EditorItems;
        /// <summary>
        /// Dictionary obsahující TreeNode podle jejich názvu
        /// </summary>
        private Dictionary<string, System.Windows.Forms.TreeNode> _TreeNodeDict;
        /// <summary>
        /// Uloží data
        /// </summary>
        protected void SaveConfig()
        {
            if (this._EditorItems == null) return;
            using (this.Config.CreateEditingScope())
            {
                foreach (SchedulerEditorItem editorItem in this._EditorItems)
                {
                    editorItem.VisualControl.SaveToData();
                }
            }
        }
        #endregion
        #region Statické spouštění okna konfigurace
        /// <summary>
        /// Otevře okno konfigurace
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="config"></param>
        public static void ShowDialog(WinForms.Form owner, SchedulerConfig config)
        {
            using (SchedulerConfigForm form = new SchedulerConfigForm())
            {
                form.SetBounds(owner, 0.70f, 0.70f);
                form.Config = config;
                form.ShowDialog(owner);
            }
        }
        #endregion
    }
    #endregion
    #region ConfigSnapSetPanel : Panel pro zobrazení přichytávání všech konkrétních typů (obsahuje PresetPanel + 5 x ConfigSnapOnePanel)
    /// <summary>
    /// ConfigSnapSetPanel : Panel pro zobrazení přichytávání všech konkrétních typů (obsahuje PresetPanel + 5 x ConfigSnapOnePanel)
    /// </summary>
    public class ConfigSnapSetPanel : WinHorizontalLine, ISchedulerEditorControlItem
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// <para/>
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.PresetButtonsPanel = new WinHorizontalLine() { Bounds = new Rectangle(0, 0, 456, 70), Caption = "Rychlé nastavení", LineLengthMax = 456 };
            this.SnapSequencePanel = new ConfigSnapOnePanel() { ImageType = ConfigSnapImageType.Sequence, Caption = "Navazující operace" };
            this.SnapInnerPanel = new ConfigSnapOnePanel() { ImageType = ConfigSnapImageType.InnerItem, Caption = "Operace k pracovní době" };
            this.SnapOriginalTimeNearPanel = new ConfigSnapOnePanel() { ImageType = ConfigSnapImageType.OriginalTimeNear, Caption = "Výchozí čas - bez změny místa" };
            this.SnapOriginalTimeLongPanel = new ConfigSnapOnePanel() { ImageType = ConfigSnapImageType.OriginalTimeLong, Caption = "Výchozí čas - na jiném místě" };
            this.SnapGridTickPanel = new ConfigSnapOnePanel() { ImageType = ConfigSnapImageType.GridTick, Caption = "Zaokrouhlení času" };

            this.PresetKeyNoneButton = new WinButtonImage() { Bounds = new Rectangle(12, 28, 140, 36), Text = "Bez přichycení", Tag = SchedulerConfig.MoveSnapKeyType.Shift };
            this.PresetKeyNormalButton = new WinButtonImage() { Bounds = new Rectangle(158, 28, 140, 36), Text = "Standardní", Tag = SchedulerConfig.MoveSnapKeyType.None };
            this.PresetKeyBigButton = new WinButtonImage() { Bounds = new Rectangle(304, 28, 140, 36), Text = "Silné", Tag = SchedulerConfig.MoveSnapKeyType.Ctrl };
            this.PresetKeyNoneButton.Click += PresetKeyButton_Click;
            this.PresetKeyNormalButton.Click += PresetKeyButton_Click;
            this.PresetKeyBigButton.Click += PresetKeyButton_Click;
            this.PresetButtonsPanel.Controls.Add(this.PresetKeyNoneButton);
            this.PresetButtonsPanel.Controls.Add(this.PresetKeyNormalButton);
            this.PresetButtonsPanel.Controls.Add(this.PresetKeyBigButton);

            this.SuspendLayout();

            this.Controls.Add(this.PresetButtonsPanel);
            this.Controls.Add(this.SnapSequencePanel);
            this.Controls.Add(this.SnapInnerPanel);
            this.Controls.Add(this.SnapOriginalTimeNearPanel);
            this.Controls.Add(this.SnapOriginalTimeLongPanel);
            this.Controls.Add(this.SnapGridTickPanel);

            this.AutoScroll = true;

            this.ResumeLayout(false);
            this.PerformLayout();

            this.FontInfo.SizeRatio = 1.16f * this.FontInfo.SizeRatio;
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();

            int x = 3;
            int y = this.OneLineHeight + 3;
            int w = this.ClientSize.Width - 6;
            int h = ConfigSnapOnePanel.OptimalHeight;
            int s = h + 3;

            int bh = this.PresetButtonsPanel.Height;
            this.PresetButtonsPanel.Bounds = new Rectangle(x, y, w, bh); y += bh + 3;

            this.SnapSequencePanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.SnapInnerPanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.SnapOriginalTimeNearPanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.SnapOriginalTimeLongPanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.SnapGridTickPanel.Bounds = new Rectangle(x, y, w, h); y += s;
        }
        /// <summary>
        /// Panel s nabídkou přednastavení
        /// </summary>
        protected WinHorizontalLine PresetButtonsPanel;
        /// <summary>
        /// Button pro nabídku Defaultní nastavení - None
        /// </summary>
        protected WinButtonImage PresetKeyNoneButton;
        /// <summary>
        /// Button pro nabídku Defaultní nastavení - Normal
        /// </summary>
        protected WinButtonImage PresetKeyNormalButton;
        /// <summary>
        /// Button pro nabídku Defaultní nastavení - Big
        /// </summary>
        protected WinButtonImage PresetKeyBigButton;
        /// <summary>
        /// Panel pro ukázku <see cref="ConfigSnapImageType.Sequence"/>
        /// </summary>
        protected ConfigSnapOnePanel SnapSequencePanel;
        /// <summary>
        /// Panel pro ukázku <see cref="ConfigSnapImageType.InnerItem"/>
        /// </summary>
        protected ConfigSnapOnePanel SnapInnerPanel;
        /// <summary>
        /// Panel pro ukázku <see cref="ConfigSnapImageType.OriginalTimeNear"/>
        /// </summary>
        protected ConfigSnapOnePanel SnapOriginalTimeNearPanel;
        /// <summary>
        /// Panel pro ukázku <see cref="ConfigSnapImageType.OriginalTimeLong"/>
        /// </summary>
        protected ConfigSnapOnePanel SnapOriginalTimeLongPanel;
        /// <summary>
        /// Panel pro ukázku <see cref="ConfigSnapImageType.GridTick"/>
        /// </summary>
        protected ConfigSnapOnePanel SnapGridTickPanel;
        /// <summary>
        /// Po kliknutí na button Preset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PresetKeyButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == null || !(button.Tag is SchedulerConfig.MoveSnapKeyType)) return;
            this.PresetConfigFor((SchedulerConfig.MoveSnapKeyType)button.Tag);
        }
        #endregion
        #region Read, Save, Data
        /// <summary>
        /// Načte hodnoty z <see cref="Data"/> do this controlů
        /// </summary>
        protected void ReadFromData()
        {
            this.ReadFromData(this.Data);
        }
        /// <summary>
        /// Do this controlů vloží defaultní hodnot
        /// </summary>
        /// <param name="keyType"></param>
        protected void PresetConfigFor(SchedulerConfig.MoveSnapKeyType keyType)
        {
            SchedulerConfig.MoveSnapInfo data = new SchedulerConfig.MoveSnapInfo();
            data.CheckValid(keyType);
            this.ReadFromData(data);
        }
        /// <summary>
        /// Načte hodnoty z dodaného objektu do this controlů
        /// </summary>
        /// <param name="data">Data</param>
        protected void ReadFromData(SchedulerConfig.MoveSnapInfo data)
        {
            if (data == null) return;

            this.SnapSequencePanel.SnapActive = data.SequenceActive;
            this.SnapSequencePanel.SnapDistance = data.SequenceDistance;
            this.SnapInnerPanel.SnapActive = data.InnerItemActive;
            this.SnapInnerPanel.SnapDistance = data.InnerItemDistance;
            this.SnapOriginalTimeNearPanel.SnapActive = data.OriginalTimeNearActive;
            this.SnapOriginalTimeNearPanel.SnapDistance = data.OriginalTimeNearDistance;
            this.SnapOriginalTimeLongPanel.SnapActive = data.OriginalTimeLongActive;
            this.SnapOriginalTimeLongPanel.SnapDistance = data.OriginalTimeLongDistance;
            this.SnapGridTickPanel.SnapActive = data.GridTickActive;
            this.SnapGridTickPanel.SnapDistance = data.GridTickDistance;
        }
        /// <summary>
        /// Uloží hodnoty z this controlů do <see cref="Data"/>
        /// </summary>
        protected void SaveToData()
        {
            this.SaveToData(this.Data);
        }
        /// <summary>
        /// Uloží hodnoty z this controlů do dodaného objektu
        /// </summary>
        protected void SaveToData(SchedulerConfig.MoveSnapInfo data)
        {
            if (data == null) return;

            data.SequenceActive = this.SnapSequencePanel.SnapActive;
            data.SequenceDistance = this.SnapSequencePanel.SnapDistance;
            data.InnerItemActive = this.SnapInnerPanel.SnapActive;
            data.InnerItemDistance = this.SnapInnerPanel.SnapDistance;
            data.OriginalTimeNearActive = this.SnapOriginalTimeNearPanel.SnapActive;
            data.OriginalTimeNearDistance = this.SnapOriginalTimeNearPanel.SnapDistance;
            data.OriginalTimeLongActive = this.SnapOriginalTimeLongPanel.SnapActive;
            data.OriginalTimeLongDistance = this.SnapOriginalTimeLongPanel.SnapDistance;
            data.GridTickActive = this.SnapGridTickPanel.SnapActive;
            data.GridTickDistance = this.SnapGridTickPanel.SnapDistance;
        }
        /// <summary>
        /// Konfigurační data
        /// </summary>
        public SchedulerConfig.MoveSnapInfo Data { get; set; }
        #endregion
        #region ISchedulerEditorControlItem
        void ISchedulerEditorControlItem.ReadFromData() { this.ReadFromData(); }
        void ISchedulerEditorControlItem.SaveToData() { this.SaveToData(); }
        WinForms.Panel ISchedulerEditorControlItem.Panel { get { return this; } }
        #endregion
    }
    /// <summary>
    /// ConfigSnapOnePanel : Panel pro zobrazení přichytávání jednoho konkrétního typu (trackbar a obrázek)
    /// </summary>
    public class ConfigSnapOnePanel : WinHorizontalLine
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.SnapCheck = new WinForms.CheckBox() { Bounds = new Rectangle(14, 35, 174, 24), Text = "Přichytávat, na vzdálenost:", TabIndex = 0 };
            this.DistTrack = new System.Windows.Forms.TrackBar() { Bounds = new Rectangle(50, 61, 188, 45), Minimum = 0, Maximum = ConfigSnapSamplePanel.SnapMaxDistance, TickFrequency = 2, TickStyle = WinForms.TickStyle.TopLeft, TabIndex = 1 };
            this.PixelLabel = new System.Windows.Forms.Label() { Bounds = new Rectangle(194, 35, 59, 23), AutoSize = false, Text = "", TextAlign = ContentAlignment.MiddleLeft, TabIndex = 2 };
            this.SamplePanel = new ConfigSnapSamplePanel();

            this.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DistTrack)).BeginInit();

            this.SnapCheck.CheckedChanged += SnapCheck_CheckedChanged;
            this.DistTrack.ValueChanged += DistTrack_ValueChanged;

            this.Controls.Add(this.SamplePanel);
            this.Controls.Add(this.DistTrack);
            this.Controls.Add(this.SnapCheck);
            this.Controls.Add(this.PixelLabel);

            this.OnlyOneLine = false;
            this.Size = new Size(400, OptimalHeight);
            this.MinimumSize = new Size(350, OptimalHeight);
            this.MaximumSize = new Size(650, OptimalHeight);
            this.LineDistanceRight = ConfigSnapSamplePanel.OptimalWidth - 12;

            this.ResumeLayout(false);
            this.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DistTrack)).EndInit();

            this.SnapCheck.Checked = true;
            this.DistTrack.Value = 20;
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();

            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;
            this.SamplePanel.Location = new Point(width - 6 - ConfigSnapSamplePanel.OptimalWidth, (height - ConfigSnapSamplePanel.OptimalHeight) / 2);
        }
        /// <summary>
        /// Výška tohoto prvku
        /// </summary>
        internal static int OptimalHeight { get { return ConfigSnapSamplePanel.OptimalHeight + 12; } }
        /// <summary>
        /// CheckBox 
        /// </summary>
        protected WinForms.CheckBox SnapCheck;
        /// <summary>
        /// TrackBar 
        /// </summary>
        protected System.Windows.Forms.TrackBar DistTrack;
        /// <summary>
        /// Label 
        /// </summary>
        protected System.Windows.Forms.Label PixelLabel;
        /// <summary>
        /// ConfigSnapSamplePanel 
        /// </summary>
        protected ConfigSnapSamplePanel SamplePanel;
        #endregion
        #region privátní eventhandlery
        /// <summary>
        /// Změna hodnoty SnapActive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SnapCheck_CheckedChanged(object sender, EventArgs e)
        {
            this.CallSnapActiveChanged();
        }
        /// <summary>
        /// Akce po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        private void CallSnapActiveChanged()
        {
            bool value = this.SnapCheck.Checked;
            this.SamplePanel.SnapActive = value;
            this.DistTrack.Enabled = value;
            this.PixelLabel.Visible = value;
            this.OnSnapActiveChanged();
            if (this.SnapActiveChanged != null)
                this.SnapActiveChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        protected virtual void OnSnapActiveChanged() { }
        /// <summary>
        /// Event po změně hodnoty <see cref="SnapActive"/>
        /// </summary>
        public event EventHandler SnapActiveChanged;
        /// <summary>
        /// Změna hodnoty SnapDistance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DistTrack_ValueChanged(object sender, EventArgs e)
        {
            this.CallSnapDistanceChanged();
        }
        /// <summary>
        /// Akce po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        private void CallSnapDistanceChanged()
        {
            int value = this.DistTrack.Value;
            this.SamplePanel.SnapDistance = value;
            this.PixelLabel.Text = value.ToString() + " pixel";
            this.OnSnapDistanceChanged();
            if (this.SnapDistanceChanged != null)
                this.SnapDistanceChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        protected virtual void OnSnapDistanceChanged() { }
        /// <summary>
        /// Event po změně hodnoty <see cref="SnapDistance"/>
        /// </summary>
        public event EventHandler SnapDistanceChanged;
        #endregion
        #region Public properties vlastní
        /// <summary>
        /// Aktivita tohoto konkrétního přichycení
        /// </summary>
        [Description("Aktivita tohoto konkrétního přichycení")]
        [Category(WinConstants.DesignCategory)]
        public bool SnapActive
        {
            get { return this.SnapCheck.Checked; }
            set { this.SnapCheck.Checked = value; }
        }
        /// <summary>
        /// Vzdálenost přichycení v pixelech
        /// </summary>
        [Description("Vzdálenost přichycení v pixelech")]
        [Category(WinConstants.DesignCategory)]
        public int SnapDistance
        {
            get { return this.DistTrack.Value; }
            set { this.DistTrack.Value = (value < 0 ? 0 : (value > ConfigSnapSamplePanel.SnapMaxDistance ? ConfigSnapSamplePanel.SnapMaxDistance : value)); this.SamplePanel.SnapDistance = this.DistTrack.Value; }
        }
        /// <summary>
        /// Druh ilustračního obrázku
        /// </summary>
        [Description("Druh ilustračního obrázku")]
        [Category(WinConstants.DesignCategory)]
        public ConfigSnapImageType ImageType
        {
            get { return this.SamplePanel.ImageType; }
            set { this.SamplePanel.ImageType = value; }
        }
        #endregion
        #region Public properties linkované ze SamplePanelu
        /// <summary>
        /// Aktivita tohoto konkrétního přichycení
        /// </summary>
        [Description("Aktivita tohoto konkrétního přichycení")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool SampleSnapActive { get { return this.SamplePanel.SnapActive; } set { this.SamplePanel.SnapActive = value; } }
        /// <summary>
        /// Druh ilustračního obrázku
        /// </summary>
        [Description("Druh ilustračního obrázku")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(ConfigSnapImageType), "Sequence")]
        public ConfigSnapImageType SampleImageType { get { return this.SamplePanel.ImageType; } set { this.SamplePanel.ImageType = value; } }
        /// <summary>
        /// Vzdálenost přichycení v pixelech
        /// </summary>
        [Description("Vzdálenost přichycení v pixelech")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(16)]
        public int SampleSnapDistance { get { return this.SamplePanel.SnapDistance; } set { this.SamplePanel.SnapDistance = value; } }
        /// <summary>
        /// Barva pozadí pod vzorky
        /// </summary>
        [Description("Barva pozadí pod vzorky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGray")]
        public Color SampleBackColor { get { return this.SamplePanel.SampleBackColor; } set { this.SamplePanel.SampleBackColor = value; } }
        /// <summary>
        /// Barva prvku, který je pevný
        /// </summary>
        [Description("Barva prvku, který je pevný")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkGray")]
        public Color SampleFixedColor { get { return this.SamplePanel.SampleFixedColor; } set { this.SamplePanel.SampleFixedColor = value; } }
        /// <summary>
        /// Barva prvku, který je vnější okolo pohyblivého prvku, použit jako pozadí při vykreslení InnerItem
        /// </summary>
        [Description("Barva prvku, který je vnější okolo pohyblivého prvku, použit jako pozadí při vykreslení InnerItem")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Linen")]
        public Color SampleOuterColor { get { return this.SamplePanel.SampleOuterColor; } set { this.SamplePanel.SampleOuterColor = value; } }
        /// <summary>
        /// Barva prvku, který se pohybuje
        /// </summary>
        [Description("Barva prvku, který se pohybuje")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkSeaGreen")]
        public Color SampleMovedColor { get { return this.SamplePanel.SampleMovedColor; } set { this.SamplePanel.SampleMovedColor = value; } }
        /// <summary>
        /// Barva spojovací linky magnetu
        /// </summary>
        [Description("Barva spojovací linky magnetu")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkViolet")]
        public Color SampleMagnetLinkColor { get { return this.SamplePanel.SampleMagnetLinkColor; } set { this.SamplePanel.SampleMagnetLinkColor = value; } }
        /// <summary>
        /// Barva linky zobrazující originální čas
        /// </summary>
        [Description("Barva linky zobrazující originální čas")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Red")]
        public Color SampleOriginalTimeColor { get { return this.SamplePanel.SampleOriginalTimeColor; } set { this.SamplePanel.SampleOriginalTimeColor = value; } }
        #endregion
    }
    /// <summary>
    /// ConfigSnapSamplePanel : Panel pro zobrazení přichytávání
    /// </summary>
    public class ConfigSnapSamplePanel : WinPanel
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            this.SetStyle(WinForms.ControlStyles.OptimizedDoubleBuffer | WinForms.ControlStyles.AllPaintingInWmPaint | WinForms.ControlStyles.UserPaint, true);

            base.Initialize();

            this.Size = new Size(OptimalWidth, OptimalHeight);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            this._SnapActive = true;
            this._ImageType = ConfigSnapImageType.Sequence;
            this._SnapDistance = 20;

            this._SampleBackColor = Color.LightGray;
            this._SampleFixedColor = Color.DarkGray;
            this._SampleOuterColor = Color.Linen;
            this._SampleMovedColor = Color.DarkSeaGreen;
            this._SampleMagnetLinkColor = Color.DarkViolet;
            this._SampleOriginalTimeColor = Color.Red;
        }
        /// <summary>
        /// Šířka tohoto prvku
        /// </summary>
        internal static int OptimalWidth { get { return 150; } }
        /// <summary>
        /// Výška tohoto prvku
        /// </summary>
        internal static int OptimalHeight { get { return 90; } }
        /// <summary>
        /// Maximální hodnota SnapDistance
        /// </summary>
        internal static int SnapMaxDistance { get { return 40; } }
        /// <summary>
        /// Hodnota pro zobrazení SnapDistance, když <see cref="SnapActive"/> je false
        /// </summary>
        protected static int SnapDisabledDistance { get { return SnapMaxDistance / 2; } }
        #endregion
        #region Kreslení
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(WinForms.PaintEventArgs e)
        {
            e.Graphics.Clear(this.SampleBackColor);
            switch (this.ImageType)
            {
                case ConfigSnapImageType.None:
                    this.PaintNone(e);
                    break;
                case ConfigSnapImageType.Sequence:
                    this.PaintSequence(e);
                    break;
                case ConfigSnapImageType.InnerItem:
                    this.PaintInnerItem(e);
                    break;
                case ConfigSnapImageType.OriginalTimeNear:
                    this.PaintOriginalTimeNear(e);
                    break;
                case ConfigSnapImageType.OriginalTimeLong:
                    this.PaintOriginalTimeLong(e);
                    break;
                case ConfigSnapImageType.GridTick:
                    this.PaintGridTick(e);
                    break;
            }
        }
        /// <summary>
        /// Vykreslí sample typu: None
        /// </summary>
        /// <param name="e"></param>
        protected void PaintNone(WinForms.PaintEventArgs e)
        {
        }
        /// <summary>
        /// Vykreslí sample typu: Sequence
        /// </summary>
        /// <param name="e"></param>
        protected void PaintSequence(WinForms.PaintEventArgs e)
        {
            Color color;

            // Proměnné: první písmenko je typ koordinátu (x, y, w, h); druhé písmenko je typ objektu (Fixed, Movable, řádek A, řádek B, Tick, ...)
            int y = 30;
            int h = 25;
            int xF = 10;
            int wF = 45;
            int xM = xF + wF - 1;
            int wM = 45;

            // Souřadnice pohyblivého prvku:
            Rectangle boundsM = new Rectangle(xM + (this.SnapActive ? this.SnapDistance : SnapDisabledDistance), y, wM, h);

            // Souřadnice fixního prvku:
            Rectangle boundsF = new Rectangle(xF, y, wF, h);

            // Magnet:
            if (this.SnapActive)
                this.PaintMagnetLine(e, boundsM.GetPoint(ContentAlignment.MiddleLeft).Value, new Point(boundsF.GetPoint(ContentAlignment.MiddleRight).Value.X - 2, 0), true);

            // Fixní prvek:
            color = this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsF, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.Enabled });

            // Pohyblivý prvek, vpravo do fixního prvku:
            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsM, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.MouseOver });
        }
        /// <summary>
        /// Vykreslí sample typu: InnerItem
        /// </summary>
        /// <param name="e"></param>
        protected void PaintInnerItem(WinForms.PaintEventArgs e)
        {
            Color color;

            // Proměnné: první písmenko je typ koordinátu (x, y, w, h); druhé písmenko je typ objektu (Fixed, Movable, řádek A, řádek B, Tick, ...)
            int x = 10;
            int yM = 35;
            int wM = 51;
            int hM = 25;
            int yF = 6;
            int hF = OptimalHeight - 2 * yF;
            int wF = OptimalWidth - 2 * x;

            // Souřadnice pohyblivého prvku:
            Rectangle boundsM = new Rectangle(x + 1 + (this.SnapActive ? this.SnapDistance : SnapDisabledDistance), yM, wM, hM);

            // Souřadnice fixního prvku:
            Rectangle boundsF = new Rectangle(x, yF, wF, hF);

            // Fixní prvek, tvoří pozadí:
            color = this.SampleOuterColor;
            Painter.DrawButtonBase(e.Graphics, boundsF, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.Enabled });

            // Magnet:
            if (this.SnapActive)
                this.PaintMagnetLine(e, boundsM.GetPoint(ContentAlignment.MiddleLeft).Value, boundsF.GetPoint(ContentAlignment.MiddleLeft).Value, true);

            // Pohyblivý prvek, nad fixním prvek:
            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsM, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.MouseOver });
        }
        /// <summary>
        /// Vykreslí sample typu: OriginalTimeNear (v témže grafu)
        /// </summary>
        /// <param name="e"></param>
        protected void PaintOriginalTimeNear(WinForms.PaintEventArgs e)
        {
            Color color;

            // Proměnné: první písmenko je typ koordinátu (x, y, w, h); druhé písmenko je typ objektu (Fixed, Movable, řádek A, řádek B, Tick, ...)
            int x = 40;
            int yA = 6;
            int hR = 37;
            int yB = yA + hR + 1;
            int yM = yB + 2;
            int hM = 15;
            int wM = 51;
            int hF = 15;
            int yF = yB + hR - 2 - hF;
            int wF = 51;

            // Souřadnice pohyblivého prvku:
            Rectangle boundsM = new Rectangle(x + (this.SnapActive ? this.SnapDistance : SnapDisabledDistance), yM, wM, hM);

            // Souřadnice fixního prvku:
            Rectangle boundsF = new Rectangle(x, yF, wF, hF);

            // Řádek horní:
            Rectangle boundsA = new Rectangle(2, yA, OptimalWidth - 4, hR);
            Painter.DrawAreaBase(e.Graphics, boundsA, this.SampleBackColor, WinForms.Orientation.Horizontal, GInteractiveState.Enabled);

            // Řádek dolní:
            Rectangle boundsB = new Rectangle(2, yB, OptimalWidth - 4, hR);
            Painter.DrawAreaBase(e.Graphics, boundsB, this.SampleBackColor, WinForms.Orientation.Horizontal, GInteractiveState.MouseOver);

            // Linka originálního času:
            Pen pen = Skin.Pen(this.SampleOriginalTimeColor);
            e.Graphics.DrawLine(pen, x - 1, yA - 2, x - 1, yB + hR + 2);

            // Magnet:
            if (this.SnapActive)
                this.PaintMagnetLine(e, boundsM.GetPoint(ContentAlignment.MiddleLeft).Value, new Point(boundsF.GetPoint(ContentAlignment.MiddleLeft).Value.X - 1, 0), true);

            // Pohyblivý prvek, nad fixním prvek, ve stejném řádku B:
            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsM, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.MouseOver });

            // Fixní prvek, dole v řádku B:
            color = this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsF, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.Enabled });
        }
        /// <summary>
        /// Vykreslí sample typu: OriginalTimeLong (v jiném grafu)
        /// </summary>
        /// <param name="e"></param>
        protected void PaintOriginalTimeLong(WinForms.PaintEventArgs e)
        {
            Color color;

            // Proměnné: první písmenko je typ koordinátu (x, y, w, h); druhé písmenko je typ objektu (Fixed, Movable, řádek A, řádek B, Linka, ...)
            int x = 40;
            int yA = 6;
            int hR = 37;
            int yB = yA + hR + 1;
            int yM = yA + 12;
            int hM = 15;
            int wM = 51;
            int hF = 15;
            int yF = yB + hR - 2 - hF;
            int wF = 51;

            // Souřadnice pohyblivého prvku:
            Rectangle boundsM = new Rectangle(x + (this.SnapActive ? this.SnapDistance : SnapDisabledDistance), yM, wM, hM);

            // Souřadnice fixního prvku:
            Rectangle boundsF = new Rectangle(x, yF, wF, hF);

            // Řádek horní:
            Rectangle boundsA = new Rectangle(2, yA, OptimalWidth - 4, hR);
            Painter.DrawAreaBase(e.Graphics, boundsA, this.SampleBackColor, WinForms.Orientation.Horizontal, GInteractiveState.MouseOver);

            // Řádek dolní:
            Rectangle boundsB = new Rectangle(2, yB, OptimalWidth - 4, hR);
            Painter.DrawAreaBase(e.Graphics, boundsB, this.SampleBackColor, WinForms.Orientation.Horizontal, GInteractiveState.Enabled);

            // Linka originálního času:
            Pen pen = Skin.Pen(this.SampleOriginalTimeColor);
            e.Graphics.DrawLine(pen, x - 1, yA - 2, x - 1, yB + hR + 2);

            // Magnet:
            if (this.SnapActive)
                this.PaintMagnetLine(e, boundsM.GetPoint(ContentAlignment.MiddleLeft).Value, new Point(boundsF.GetPoint(ContentAlignment.MiddleLeft).Value.X - 1, 0), true);

            // Pohyblivý prvek, nad fixním prvek, v řádku A:
            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsM, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.MouseOver });

            // Fixní prvek, dole v řádku B:
            color = this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsF, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.Enabled });
        }
        /// <summary>
        /// Vykreslí sample typu: GridTick
        /// </summary>
        /// <param name="e"></param>
        protected void PaintGridTick(WinForms.PaintEventArgs e)
        {
            Color color;

            // Proměnné: první písmenko je typ koordinátu (x, y, w, h); druhé písmenko je typ objektu (Fixed, Movable, řádek A, řádek B, Linka, Tick big, Tick small, ...)
            int xT = 12;
            int sT = 10;
            int rT = OptimalWidth - 12;
            int yTb = 8;
            int yTs = 12;
            int bT = 17;

            int xL = 0;
            int yL = bT;
            int bL = OptimalHeight - 12;

            int yM = 36;
            int hM = 25;
            int wM = 51;

            // Vykreslit "Ticky":
            Pen pen = Skin.Pen(Color.Black);
            int n = 0;
            for (int x = xT; x < rT; x += sT)
            {
                bool isBig = ((n % 5) == 0);
                pen.Width = (isBig ? 2f : 1f);
                n++;
                e.Graphics.DrawLine(pen, x, (isBig ? yTb : yTs), x, bT);
                if (xL == 0 && x >= 30 && !isBig)
                    xL = x;
            }

            // Vykreslit vodící linku času:
            pen = Skin.Pen(this.SampleOriginalTimeColor);
            e.Graphics.DrawLine(pen, xL, yL, xL, bL);

            // Souřadnice pohyblivého prvku:
            Rectangle boundsM = new Rectangle(xL + 1 + (this.SnapActive ? this.SnapDistance : SnapDisabledDistance), yM, wM, hM);

            // Magnet:
            if (this.SnapActive)
                this.PaintMagnetLine(e, boundsM.GetPoint(ContentAlignment.MiddleLeft).Value, new Point(xL - 1, 0), true);

            // Pohyblivý prvek, nad fixním prvek, v řádku A:
            color = this.SnapActive ? this.SampleMovedColor : this.SampleFixedColor;
            Painter.DrawButtonBase(e.Graphics, boundsM, new DrawButtonArgs() { BackColor = color, InteractiveState = GInteractiveState.MouseOver });
        }
        /// <summary>
        /// Vykreslí spojovací magnetovou linku
        /// </summary>
        /// <param name="e"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="sameY"></param>
        protected void PaintMagnetLine(WinForms.PaintEventArgs e, Point source, Point target, bool sameY = false)
        {
            if (source.X == target.X) return;
            if (sameY) target.Y = source.Y;
            using (Painter.GraphicsUseSmooth(e.Graphics))
            {
                Pen pen = Skin.Pen(this.SampleMagnetLinkColor, width: 3, endCap: LineCap.ArrowAnchor);
                e.Graphics.DrawLine(pen, source, target);
            }
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Aktivita tohoto konkrétního přichycení
        /// </summary>
        [Description("Aktivita tohoto konkrétního přichycení")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool SnapActive
        {
            get { return this._SnapActive; }
            set { this._SnapActive = value; this.Refresh(); }
        }
        private bool _SnapActive;
        /// <summary>
        /// Druh ilustračního obrázku
        /// </summary>
        [Description("Druh ilustračního obrázku")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(ConfigSnapImageType), "Sequence")]
        public ConfigSnapImageType ImageType
        {
            get { return this._ImageType; }
            set { this._ImageType = value; this.Refresh(); }
        }
        private ConfigSnapImageType _ImageType;
        /// <summary>
        /// Vzdálenost přichycení v pixelech
        /// </summary>
        [Description("Vzdálenost přichycení v pixelech")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(16)]
        public int SnapDistance
        {
            get { return this._SnapDistance; }
            set { this._SnapDistance = (value < 0 ? 0 : (value > SnapMaxDistance ? SnapMaxDistance : value)); this.Refresh(); }
        }
        private int _SnapDistance;
        /// <summary>
        /// Barva pozadí pod vzorky
        /// </summary>
        [Description("Barva pozadí pod vzorky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGray")]
        public Color SampleBackColor
        {
            get { return this._SampleBackColor; }
            set { this._SampleBackColor = value; this.Refresh(); }
        }
        private Color _SampleBackColor;
        /// <summary>
        /// Barva prvku, který je pevný
        /// </summary>
        [Description("Barva prvku, který je pevný")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkGray")]
        public Color SampleFixedColor
        {
            get { return this._SampleFixedColor; }
            set { this._SampleFixedColor = value; this.Refresh(); }
        }
        private Color _SampleFixedColor;
        /// <summary>
        /// Barva prvku, který je vnější okolo pohyblivého prvku, použit jako pozadí při vykreslení InnerItem
        /// </summary>
        [Description("Barva prvku, který je vnější okolo pohyblivého prvku, použit jako pozadí při vykreslení InnerItem")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Linen")]
        public Color SampleOuterColor
        {
            get { return this._SampleOuterColor; }
            set { this._SampleOuterColor = value; this.Refresh(); }
        }
        private Color _SampleOuterColor;
        /// <summary>
        /// Barva prvku, který se pohybuje
        /// </summary>
        [Description("Barva prvku, který se pohybuje")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkSeaGreen")]
        public Color SampleMovedColor
        {
            get { return this._SampleMovedColor; }
            set { this._SampleMovedColor = value; this.Refresh(); }
        }
        private Color _SampleMovedColor;
        /// <summary>
        /// Barva spojovací linky magnetu
        /// </summary>
        [Description("Barva spojovací linky magnetu")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkViolet")]
        public Color SampleMagnetLinkColor
        {
            get { return this._SampleMagnetLinkColor; }
            set { this._SampleMagnetLinkColor = value; this.Refresh(); }
        }
        private Color _SampleMagnetLinkColor;
        /// <summary>
        /// Barva linky zobrazující originální čas
        /// </summary>
        [Description("Barva linky zobrazující originální čas")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "Red")]
        public Color SampleOriginalTimeColor
        {
            get { return this._SampleOriginalTimeColor; }
            set { this._SampleOriginalTimeColor = value; this.Refresh(); }
        }
        private Color _SampleOriginalTimeColor;
        #endregion
    }
    #region enum ConfigSnapImageType
    /// <summary>
    /// Typ ilustračního obrázku
    /// </summary>
    public enum ConfigSnapImageType
    {
        /// <summary>
        /// Bez přichytávání
        /// </summary>
        None,
        /// <summary>
        /// Sekvence = navazující prvky, za sebou jdoucí operace
        /// </summary>
        Sequence,
        /// <summary>
        /// Vnitřní prvek ve vnějším prvku, začátek operace k začátku směny
        /// </summary>
        InnerItem,
        /// <summary>
        /// Přidržet se původního času ve výchozím grafu (= "blízko")
        /// </summary>
        OriginalTimeNear,
        /// <summary>
        /// Přidržet se původního času ve ostatních grafech (= "daleko")
        /// </summary>
        OriginalTimeLong,
        /// <summary>
        /// Přichytávat k rastru
        /// </summary>
        GridTick
    }
    #endregion
    #endregion
    #region ConfigLinkLinesSetPanel : Panel pro zobrazení konfigurace Linků (obsahuje 3 x ConfigLinkLineOnePanel)
    /// <summary>
    /// ConfigLinkLinesSetPanel : Panel pro zobrazení konfigurace Linků
    /// </summary>
    public class ConfigLinkLinesSetPanel : WinHorizontalLine, ISchedulerEditorControlItem
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// <para/>
        /// Když se o tom předem ví, tak se to nechá uřídit :-)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.LineShapePanel = new ConfigLinkLineOnePanel() { LinkLineSetting = ConfigLinkLineSettingType.LineShape, Caption = "Tvar spojovací linie", ValueNoText = "Rovné přímé čáry", ValueYesText = "Plynulé křivky", GraphSample = 1, LinkWholeTask = true };
            this.LinkMousePanel = new ConfigLinkLineOnePanel() { LinkLineSetting = ConfigLinkLineSettingType.LinkMouse, Caption = "Linky vykreslované při pohybu myši", ValueNoText = "Pouze pro prvek pod myší", ValueYesText = "Celý postup (všechny operace)", GraphSample = 2 };
            this.LinkSelectPanel = new ConfigLinkLineOnePanel() { LinkLineSetting = ConfigLinkLineSettingType.LinkSelect, Caption = "Linky vykreslované při označení prvku", ValueNoText = "Pouze pro označený prvek", ValueYesText = "Celý postup (všechny operace)", GraphSample = 3 };

            this.SuspendLayout();

            this.LineShapePanel.CurrentValueChanged += LineShapePanel_CurrentValueChanged;

            this.Controls.Add(this.LineShapePanel);
            this.Controls.Add(this.LinkMousePanel);
            this.Controls.Add(this.LinkSelectPanel);

            this.AutoScroll = true;

            this.ResumeLayout(false);
            this.PerformLayout();

            this.FontInfo.SizeRatio = 1.16f * this.FontInfo.SizeRatio;
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();

            int x = 3;
            int y = this.OneLineHeight + 3;
            int w = this.ClientSize.Width - 6;
            int h = ConfigLinkLineOnePanel.OptimalHeight;
            int s = h + 3;

            this.LineShapePanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.LinkMousePanel.Bounds = new Rectangle(x, y, w, h); y += s;
            this.LinkSelectPanel.Bounds = new Rectangle(x, y, w, h); y += s;
        }
        /// <summary>
        /// Panel pro zadání tvaru linky
        /// </summary>
        protected ConfigLinkLineOnePanel LineShapePanel;
        /// <summary>
        /// Panel pro zadání rozsahu linků pro situaci MouseOver
        /// </summary>
        protected ConfigLinkLineOnePanel LinkMousePanel;
        /// <summary>
        /// Panel pro zadání rozsahu linků pro situaci Selected
        /// </summary>
        protected ConfigLinkLineOnePanel LinkSelectPanel;
        /// <summary>
        /// Po změně hodnoty "Kreslit čáru jako linku/křivku" v panelu <see cref="LineShapePanel"/>:
        /// tuto hodnotu nastavíme do ostatních panelů, aby reprezentovaly odpovídající tvar:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LineShapePanel_CurrentValueChanged(object sender, EventArgs e)
        {
            bool linkLineAsSCurve = this.LineShapePanel.CurrentValue;
            this.LinkMousePanel.LinkLineAsSCurve = linkLineAsSCurve;
            this.LinkSelectPanel.LinkLineAsSCurve = linkLineAsSCurve;
        }
        #endregion
        #region Read, Save, Data
        /// <summary>
        /// Načte hodnoty z <see cref="Data"/> do this controlů
        /// </summary>
        protected void ReadFromData()
        {
            this.ReadFromData(this.Data);
        }
        /// <summary>
        /// Načte hodnoty z dodaného objektu do this controlů
        /// </summary>
        /// <param name="data">Data</param>
        protected void ReadFromData(SchedulerConfig data)
        {
            if (data == null) return;

            this.LineShapePanel.CurrentValue = data.GuiEditShowLinkAsSCurve;
            this.LinkMousePanel.CurrentValue = data.GuiEditShowLinkMouseWholeTask;
            this.LinkMousePanel.LinkLineAsSCurve = data.GuiEditShowLinkAsSCurve;
            this.LinkSelectPanel.CurrentValue = data.GuiEditShowLinkSelectedWholeTask;
            this.LinkSelectPanel.LinkLineAsSCurve = data.GuiEditShowLinkAsSCurve;
        }
        /// <summary>
        /// Uloží hodnoty z this controlů do <see cref="Data"/>
        /// </summary>
        protected void SaveToData()
        {
            this.SaveToData(this.Data);
        }
        /// <summary>
        /// Uloží hodnoty z this controlů do dodaného objektu
        /// </summary>
        protected void SaveToData(SchedulerConfig data)
        {
            if (data == null) return;

            data.GuiEditShowLinkAsSCurve = this.LineShapePanel.CurrentValue;
            data.GuiEditShowLinkMouseWholeTask = this.LinkMousePanel.CurrentValue;
            data.GuiEditShowLinkSelectedWholeTask = this.LinkSelectPanel.CurrentValue;
        }
        /// <summary>
        /// Konfigurační data
        /// </summary>
        public SchedulerConfig Data { get; set; }
        #endregion
        #region ISchedulerEditorControlItem
        void ISchedulerEditorControlItem.ReadFromData() { this.ReadFromData(); }
        void ISchedulerEditorControlItem.SaveToData() { this.SaveToData(); }
        WinForms.Panel ISchedulerEditorControlItem.Panel { get { return this; } }
        #endregion
    }
    /// <summary>
    /// ConfigLinkLineOnePanel : panel zobrazující jednu předvolbu a odpovídající Sample
    /// </summary>
    public class ConfigLinkLineOnePanel : WinHorizontalLine
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.Radio0 = new WinForms.RadioButton() { Bounds = new Rectangle(14, 35, 200, 24), TabIndex = 0 };
            this.Radio1 = new WinForms.RadioButton() { Bounds = new Rectangle(14, 70, 200, 24), TabIndex = 0 };
            this.SamplePanel = new ConfigLinkLineSamplePanel();

            this.SuspendLayout();

            this.Radio0.CheckedChanged += Radio_CheckedChanged;
            this.Radio1.CheckedChanged += Radio_CheckedChanged;

            this.Controls.Add(this.SamplePanel);
            this.Controls.Add(this.Radio1);
            this.Controls.Add(this.Radio0);

            this.OnlyOneLine = false;
            this.Size = new Size(400, OptimalHeight);
            this.MinimumSize = new Size(350, OptimalHeight);
            this.MaximumSize = new Size(650, OptimalHeight);
            this.LineDistanceRight = ConfigSnapSamplePanel.OptimalWidth - 12;

            this.ResumeLayout(false);
            this.PerformLayout();

            this.Radio0.Checked = true;
        }
        /// <summary>
        /// Úprava layoutu po změně velikosti
        /// </summary>
        protected override void PrepareLayout()
        {
            base.PrepareLayout();

            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;
            this.SamplePanel.Location = new Point(width - 6 - ConfigLinkLineSamplePanel.OptimalWidth, (height - ConfigLinkLineSamplePanel.OptimalHeight) / 2);
        }
        /// <summary>
        /// Výška tohoto prvku
        /// </summary>
        internal static int OptimalHeight { get { return ConfigLinkLineSamplePanel.OptimalHeight + 12; } }
        /// <summary>
        /// RadioButton pro hodnotu 0
        /// </summary>
        protected WinForms.RadioButton Radio0;
        /// <summary>
        /// RadioButton pro hodnotu 1
        /// </summary>
        protected WinForms.RadioButton Radio1;
        /// <summary>
        /// ConfigSnapSamplePanel 
        /// </summary>
        protected ConfigLinkLineSamplePanel SamplePanel;
        #endregion
        #region Události
        /// <summary>
        /// Po změně Radio.Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Radio_CheckedChanged(object sender, EventArgs args)
        {
            this.CallCurrentValueChanged();
        }
        /// <summary>
        /// Provede akce po změně <see cref="CurrentValue"/>
        /// </summary>
        private void CallCurrentValueChanged()
        {
            this.ShowValue();
            this.OnCurrentValueChanged();
            if (this.CurrentValueChanged != null)
                this.CurrentValueChanged(this, new EventArgs());
        }
        /// <summary>
        /// Háček po změně <see cref="CurrentValue"/>
        /// </summary>
        protected virtual void OnCurrentValueChanged() { }
        /// <summary>
        /// Event po změně <see cref="CurrentValue"/>
        /// </summary>
        public event EventHandler CurrentValueChanged;
        /// <summary>
        /// Zajistí promítnutí hodnoty do sample panelu
        /// </summary>
        protected void ShowValue()
        {
            bool value = this.CurrentValue;
            switch (this.LinkLineSetting)
            {
                case ConfigLinkLineSettingType.LineShape:
                    this.LinkLineAsSCurve = value;
                    break;
                case ConfigLinkLineSettingType.LinkMouse:
                case ConfigLinkLineSettingType.LinkSelect:
                    this.LinkWholeTask = value;
                    break;
            }
        }
        #endregion
        #region Public properties vlastní
        /// <summary>
        /// Hodnota uvedená na první RadioButtonu, který odpovídá hodnotě <see cref="CurrentValue"/> = false
        /// </summary>
        [Description("Hodnota uvedená na první RadioButtonu, který odpovídá hodnotě CurrentValue = false")]
        [Category(WinConstants.DesignCategory)]
        public string ValueNoText
        {
            get { return this.Radio0.Text; }
            set { this.Radio0.Text = value; }
        }
        /// <summary>
        /// Hodnota uvedená na první RadioButtonu, který odpovídá hodnotě <see cref="CurrentValue"/> = true
        /// </summary>
        [Description("Hodnota uvedená na první RadioButtonu, který odpovídá hodnotě CurrentValue = true")]
        [Category(WinConstants.DesignCategory)]
        public string ValueYesText
        {
            get { return this.Radio1.Text; }
            set { this.Radio1.Text = value; }
        }
        /// <summary>
        /// Aktuální hodnota v tomto panelu:
        /// false = označen první RadioButton, true = označen druhý RadioButton
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CurrentValue
        {
            get { return this.Radio1.Checked; }
            set
            {
                bool value0 = !value;
                bool value1 = value;
                if (this.Radio0.Checked != value0) this.Radio0.Checked = value0;
                if (this.Radio1.Checked != value1) this.Radio1.Checked = value1;
                /* to samo vyvolá ShowValue() přes Radio_CheckedChanged() */
            }
        }
        /// <summary>
        /// Typ údaje, který se v tomto panelu edituje
        /// </summary>
        [Description("Typ údaje, který se v tomto panelu edituje")]
        [Category(WinConstants.DesignCategory)]
        public ConfigLinkLineSettingType LinkLineSetting
        {
            get { return this._LinkLineSetting; }
            set { this._LinkLineSetting = value; this.ShowValue(); }
        }
        private ConfigLinkLineSettingType _LinkLineSetting;
        #endregion
        #region Public properties linkované ze SamplePanelu
        /// <summary>
        /// Tvar spojovací linie jako křivka?
        /// false = přímá čára; true = S-křivka
        /// </summary>
        [Description("Tvar spojovací linie jako křivka? false = přímá čára; true = S-křivka")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool LinkLineAsSCurve { get { return this.SamplePanel.LinkLineAsSCurve; } set { this.SamplePanel.LinkLineAsSCurve = value; } }
        /// <summary>
        /// Zobrazovat celý řetěz?
        /// false = jen přímé sousedy; true = celý řetěz
        /// </summary>
        [Description("Zobrazovat celý řetěz? false = jen přímé sousedy; true = celý řetěz")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool LinkWholeTask { get { return this.SamplePanel.LinkWholeTask; } set { this.SamplePanel.LinkWholeTask = value; } }
        /// <summary>
        /// Barva pozadí pod vzorky
        /// </summary>
        [Description("Barva pozadí pod vzorky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGray")]
        public Color SampleBackColor { get { return this.SamplePanel.SampleBackColor; } set { this.SamplePanel.SampleBackColor = value; } }
        /// <summary>
        /// Barva prvku, který je neaktivní
        /// </summary>
        [Description("Barva prvku, který je neaktivní")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkGray")]
        public Color SampleFixedColor { get { return this.SamplePanel.SampleFixedColor; } set { this.SamplePanel.SampleFixedColor = value; } }
        /// <summary>
        /// Barva prvku, který je aktivní
        /// </summary>
        [Description("Barva prvku, který je aktivní")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkSeaGreen")]
        public Color SampleMovedColor { get { return this.SamplePanel.SampleMovedColor; } set { this.SamplePanel.SampleMovedColor = value; } }
        /// <summary>
        /// Barva spojovací linky
        /// </summary>
        [Description("Barva spojovací linky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGreen")]
        public Color LineColor { get { return this.SamplePanel.LineColor; } set { this.SamplePanel.LineColor = value; } }
        /// <summary>
        /// Šířka spojovací linky
        /// </summary>
        [Description("Šířka spojovací linky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(1)]
        public int LineWidth { get { return this.SamplePanel.LineWidth; } set { this.SamplePanel.LineWidth = value; } }
        /// <summary>
        /// Číslo vzorku dat, 1 - 5
        /// </summary>
        [Description("Číslo vzorku dat, 1 - 5")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(1)]
        public int GraphSample { get { return this.SamplePanel.GraphSample; } set { this.SamplePanel.GraphSample = value; } }
        #endregion
    }
    /// <summary>
    /// ConfigLinkLineSamplePanel : Panel pro grafické zobrazení Linků dle dané konfigurace
    /// </summary>
    public class ConfigLinkLineSamplePanel : WinPanel
    {
        #region Vnitřní život: inicializace
        /// <summary>
        /// Inicializace.
        /// Pozor, jde o virtuální metodu volanou z konstruktoru.
        /// Tyto controly obecně nemají používat konstruktor, ale tuto inicializaci (kde lze řídit místo výkonu base metody)
        /// </summary>
        protected override void Initialize()
        {
            this.SetStyle(WinForms.ControlStyles.OptimizedDoubleBuffer | WinForms.ControlStyles.AllPaintingInWmPaint | WinForms.ControlStyles.UserPaint, true);

            base.Initialize();

            this.Size = new Size(OptimalWidth, OptimalHeight);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            this._LinkLineAsSCurve = true;
            this._LinkWholeTask = true;

            this._SampleBackColor = Color.LightGray;
            this._SampleFixedColor = Color.DarkGray;
            this._SampleMovedColor = Color.DarkSeaGreen;
            this._LineColor = Color.LightGreen;
            this._LineWidth = 1;
        }
        /// <summary>
        /// Šířka tohoto prvku
        /// </summary>
        internal static int OptimalWidth { get { return 280; } }
        /// <summary>
        /// Výška tohoto prvku
        /// </summary>
        internal static int OptimalHeight { get { return 90; } }
        #endregion
        #region Kreslení
        /// <summary>
        /// Vykreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(WinForms.PaintEventArgs e)
        {
            e.Graphics.Clear(this.SampleBackColor);

            Rectangle[] items = this.CreateSample();

            // Vykreslit prvky:
            int activeItem = 2;
            for (int i = 0; i < items.Length; i++)
            {
                Color color = (i == activeItem ? this.SampleMovedColor : this.SampleFixedColor);
                GInteractiveState state = (i == activeItem ? GInteractiveState.MouseOver : GInteractiveState.Enabled);
                Painter.DrawButtonBase(e.Graphics, items[i], new DrawButtonArgs() { BackColor = color, InteractiveState = state });
            }

            // Linky:
            bool asSCurve = this.LinkLineAsSCurve;
            bool wholeTask = this.LinkWholeTask;
            int linkFrom = (wholeTask ? 0 : 1);
            int linkTo = (wholeTask ? items.Length - 1 : 3);
            Color lineColor = this.LineColor;
            int width = this.LineWidth;
            // Na grafiku nasadíme hladkou kresbu:
            using (Painter.GraphicsUse(e.Graphics, GraphicSetting.Smooth))
            {
                for (int l = linkFrom; l < linkTo; l++)
                {
                    Point? prevPoint = items[l].GetPoint(ContentAlignment.MiddleRight);
                    Point? nextPoint = items[l + 1].GetPoint(ContentAlignment.MiddleLeft);
                    using (GraphicsPath graphicsPath = Painter.CreatePathLinkLine(prevPoint, nextPoint, asSCurve))
                    {
                        Painter.DrawLinkPath(e.Graphics, graphicsPath, lineColor, null, width);
                    }
                }
            }
        }
        /// <summary>
        /// Metoda vrací pole s pěti souřadnicemi, podle zadaného čísla vzorku <see cref="GraphSample"/>
        /// </summary>
        /// <returns></returns>
        protected Rectangle[] CreateSample()
        {
            // Vykreslovat budeme 5 x Rectangle, z nichž prostřední je Aktivní; postupně jsou umístěny zleva doprava:
            // Prostor máme W=280, H=90:
            int h = 14;
            switch (this.GraphSample)
            {
                case 2:
                    h = 12;
                    return new Rectangle[]
                    {
                        new Rectangle(10, 20, 40, h),
                        new Rectangle(55, 68, 20, h),
                        new Rectangle(100, 10, 55, h),
                        new Rectangle(180, 58, 30, h),
                        new Rectangle(250, 15, 25, h)
                    };
                case 3:
                    h = 16;
                    return new Rectangle[]
                    {
                        new Rectangle(10, 10, 55, h),
                        new Rectangle(70, 69, 35, h),
                        new Rectangle(105, 15, 30, h),
                        new Rectangle(170, 15, 40, h),
                        new Rectangle(215, 60, 60, h)
                    };
                case 4:
                    return new Rectangle[]
                    {
                        new Rectangle(10, 15, 40, h),
                        new Rectangle(70, 66, 30, h),
                        new Rectangle(115, 38, 50, h),
                        new Rectangle(180, 20, 20, h),
                        new Rectangle(240, 55, 30, h)
                    };
                case 5:
                    return new Rectangle[]
                    {
                        new Rectangle(10, 15, 40, h),
                        new Rectangle(70, 66, 30, h),
                        new Rectangle(115, 38, 50, h),
                        new Rectangle(180, 20, 20, h),
                        new Rectangle(240, 55, 30, h)
                    };

            }

            // Defaultní sada:
            h = 14;
            return new Rectangle[]
            {
                new Rectangle(10, 15, 40, h),
                new Rectangle(70, 66, 30, h),
                new Rectangle(115, 38, 50, h),
                new Rectangle(180, 20, 20, h),
                new Rectangle(240, 55, 30, h)
            };
        }
        #endregion
        #region Public properties
        /// <summary>
        /// Tvar spojovací linie jako křivka?
        /// false = přímá čára; true = S-křivka
        /// </summary>
        [Description("Tvar spojovací linie jako křivka? false = přímá čára; true = S-křivka")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool LinkLineAsSCurve
        {
            get { return this._LinkLineAsSCurve; }
            set { this._LinkLineAsSCurve = value; this.Refresh(); }
        }
        private bool _LinkLineAsSCurve;
        /// <summary>
        /// Zobrazovat celý řetěz?
        /// false = jen přímé sousedy; true = celý řetěz
        /// </summary>
        [Description("Zobrazovat celý řetěz? false = jen přímé sousedy; true = celý řetěz")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(true)]
        public bool LinkWholeTask
        {
            get { return this._LinkWholeTask; }
            set { this._LinkWholeTask = value; this.Refresh(); }
        }
        private bool _LinkWholeTask;
        /// <summary>
        /// Barva pozadí pod vzorky
        /// </summary>
        [Description("Barva pozadí pod vzorky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGray")]
        public Color SampleBackColor
        {
            get { return this._SampleBackColor; }
            set { this._SampleBackColor = value; this.Refresh(); }
        }
        private Color _SampleBackColor;
        /// <summary>
        /// Barva prvku, který je neaktivní
        /// </summary>
        [Description("Barva prvku, který je neaktivní")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkGray")]
        public Color SampleFixedColor
        {
            get { return this._SampleFixedColor; }
            set { this._SampleFixedColor = value; this.Refresh(); }
        }
        private Color _SampleFixedColor;
        /// <summary>
        /// Barva prvku, který je aktivní
        /// </summary>
        [Description("Barva prvku, který je aktivní")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "DarkSeaGreen")]
        public Color SampleMovedColor
        {
            get { return this._SampleMovedColor; }
            set { this._SampleMovedColor = value; this.Refresh(); }
        }
        private Color _SampleMovedColor;
        /// <summary>
        /// Barva spojovací linky
        /// </summary>
        [Description("Barva spojovací linky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(typeof(Color), "LightGreen")]
        public Color LineColor
        {
            get { return this._LineColor; }
            set { this._LineColor = value; this.Refresh(); }
        }
        private Color _LineColor;
        /// <summary>
        /// Šířka spojovací linky
        /// </summary>
        [Description("Šířka spojovací linky")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(1)]
        public int LineWidth
        {
            get { return this._LineWidth; }
            set { this._LineWidth = value; this.Refresh(); }
        }
        private int _LineWidth;
        /// <summary>
        /// Číslo vzorku dat, 1 - 5
        /// </summary>
        [Description("Číslo vzorku dat, 1 - 5")]
        [Category(WinConstants.DesignCategory)]
        [AmbientValue(1)]
        public int GraphSample
        {
            get { return this._GraphSample; }
            set { this._GraphSample = (value < 1 ? 1 : value > 5 ? 5 : value); this.Refresh(); }
        }
        private int _GraphSample;
        #endregion
    }
    #region enum ConfigLinkLineSettingType
    /// <summary>
    /// Druh údaje, který se v konkrétním <see cref="ConfigLinkLineSamplePanel"/> edituje
    /// </summary>
    public enum ConfigLinkLineSettingType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None,
        /// <summary>
        /// Tvar linie: false = rovné čáry, true = S-křivky
        /// </summary>
        LineShape,
        /// <summary>
        /// Linky pro MouseOver: false = jen aktivní prvek, true = celá sada (dílec)
        /// </summary>
        LinkMouse,
        /// <summary>
        /// Linky pro Selected: false = jen aktivní prvek, true = celá sada (dílec)
        /// </summary>
        LinkSelect
    }
    #endregion
    #endregion
}
