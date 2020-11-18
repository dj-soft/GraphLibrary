using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace Djs.Tools.WebDownloader.Download
{
    #region class WebItem : základní abstraktní datová třída jedné položky vzorce
    /// <summary>
    /// WebItem : základní abstraktní třída jedné položky vzorce
    /// </summary>
    public abstract class WebItem : WebData
    {
        #region Konstrukce a property
        public WebItem()
        {
            this.CarryOutIncrement = true;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        /// <summary>
        /// Typ prvku, podle typu se volí patřičný GUI editor
        /// </summary>
        public abstract WebItemType Type { get; }
        /// <summary>
        /// Klíč ve vzorci (WebAdress.Formula), který je nahrazován tímto prvkem.
        /// Typicky má formu {{1}}, běžně jej generuje parsování vzorku v metodě WebAdress.Parse()
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Úroveň, na které se provádí inkrementace tohoto prvku.
        /// Po každé jedné vygenerované adrese se inkrementují prvky s Level == 1, a pokud některý prvek vrátí true (=CarryOut), pak se inkrememntují prvky s Level == 2, atd.
        /// Jakmile dojde k inkrementaci, při níž vrátí prvek s nejvyšší Level v metodě Increment() hodnotu true (=přenos nahoru, a přitom není už kam), pak je hotovo.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// true, pokud tato hladina má po svém inkrementu po přechodu z konečné (nejvyšší) hodnoty zpět na výchozí hodnotu hlásít přenos do vyšší hladiny. Typicky ano = true.
        /// Výjimečně (pokud mám více prvků v jedné hladině) mohou některé prvky "cyklovat" ale svůj cyklus nemají přenášet nahoru, přenos zajistí prvek s jiným cyklem.
        /// </summary>
        public bool CarryOutIncrement { get; set; }
        /// <summary>
        /// true pokud jsou zadaná data korektní, false když ne
        /// </summary>
        public abstract bool Correct { get; }
        /// <summary>
        /// Aktuální hodnota prvku
        /// </summary>
        public abstract string Text { get; }
        /// <summary>
        /// Inkrementace o jeden krok nahoru. Pokud se prvek "přetočí" (z devítky na nulu), vrací true.
        /// </summary>
        /// <returns></returns>
        public abstract bool Increment();
        /// <summary>
        /// Vrací deep clone sebe sama
        /// </summary>
        public virtual WebItem Clone { get { return null; } }
        #endregion
        #region Load & Save, a podpora
        /// <summary>
        /// Vrátí text, který ukládá data této položky.
        /// Potomek může sestavit List položek KeyValuePair (string, string), a použít metodu SaveToString(). Tato metoda zformátuje string, předsadí do něj společná data a vrátí.
        /// </summary>
        /// <returns></returns>
        public string Save()
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>("Key", this.Key));
            list.Add(new KeyValuePair<string, string>("Level", this.Level.ToString()));
            list.Add(new KeyValuePair<string, string>("Carry", (this.CarryOutIncrement ? "Y" : "N")));

            this.OnSave(list);

            return CreateLine(this.Type.ToString(), list);
        }
        /// <summary>
        /// Zde potomek ze svých dat doplní do listu svoje proměnné
        /// </summary>
        /// <param name="list"></param>
        protected virtual void OnSave(List<KeyValuePair<string, string>> list)
        { }
        /// <summary>
        /// Naplní svoje data z dodaného seznamu.
        /// Bázová třída naplní svoje property a volá OnLoad().
        /// </summary>
        /// <returns></returns>
        public void Load(List<KeyValuePair<string, string>> list)
        {
            this.Key = GetValue(list, "Key", "");
            this.Level = GetValue(list, "Level", 1);
            this.CarryOutIncrement = GetValue(list, "Carry", true);

            this.OnLoad(list);
        }
        /// <summary>
        /// Zde potomek z listu proměnných načte svoje data
        /// </summary>
        /// <param name="list"></param>
        protected virtual void OnLoad(List<KeyValuePair<string, string>> list)
        { }
        #endregion
    }
    /// <summary>
    /// Typ položky vzorce
    /// </summary>
    public enum WebItemType
    {
        None,
        NumericRange,
        ValueList
    }
    #endregion
    #region class WebItemPanel : základní vizuální třída pro zobrazení jedné položky vzorce
    /// <summary>
    /// WebItemPanel : základní vizuální třída pro zobrazení jedné položky vzorce
    /// </summary>
    public class WebItemPanel : Panel
    {
        #region Konstrukce
        public WebItemPanel()
        {
            this.AutoScroll = true;
        }
        /// <summary>
        /// Inicializuje prvky zobrazující Key, Level, CarryOut
        /// </summary>
        /// <param name="y"></param>
        /// <param name="tabIndex"></param>
        protected void InitCommon(int x, ref int y, ref int tabIndex)
        {
            int w = x - 9;
            this._TypeLbl = new WebLabel("Typ položky:", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._TypeCmb = new WebCombo(new Rectangle(x, y, 275, 24), ref tabIndex, "Neurčeno", "Číselná řada", "Množina prvků");
            y += this._TypeCmb.Height + 1;
            this._KeyLbl = new WebLabel("Název proměnné ve vzorci:", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._KeyTxt = new WebText(new Rectangle(x, y, 120, 24), ref tabIndex);
            y += this._KeyTxt.Height + 1;
            this._LevelLbl = new WebLabel("Hladina při inkrementaci:", new Rectangle(7, y + 4, w, 16), ref tabIndex);
            this._LevelTxt = new WebNumeric(0, 9999, new Rectangle(x, y, 70, 24), ref tabIndex);
            this._CarryOutChk = new WebCheck("Po dokončení cyklu inkrementovat vyšší hladinu", new Rectangle(x+76, y + 2, 320, 20), ref tabIndex);
            y += this._LevelTxt.Height + 1;

            ((System.ComponentModel.ISupportInitialize)(this._LevelTxt)).BeginInit();
            this.Controls.Add(this._TypeLbl);
            this.Controls.Add(this._TypeCmb);
            this.Controls.Add(this._KeyLbl);
            this.Controls.Add(this._KeyTxt);
            this.Controls.Add(this._LevelLbl);
            this.Controls.Add(this._LevelTxt);
            this.Controls.Add(this._CarryOutChk);

            this._TypeCmb.SelectedIndexChanged += new EventHandler(ValueChanged);
            this._KeyTxt.TextChanged += new EventHandler(ValueChanged);
            this._LevelTxt.ValueChanged += new EventHandler(ValueChanged);
            this._CarryOutChk.CheckedChanged += new EventHandler(ValueChanged);

            ((System.ComponentModel.ISupportInitialize)(this._LevelTxt)).EndInit();
        }
        protected void ValueChanged(object sender, EventArgs e)
        {
            this.DataCollect();
        }
        protected WebLabel _TypeLbl;
        protected WebCombo _TypeCmb;
        protected WebLabel _KeyLbl;
        protected WebText _KeyTxt;
        protected WebLabel _LevelLbl;
        protected WebNumeric _LevelTxt;
        protected WebCheck _CarryOutChk;
        #endregion
        #region Data
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebItem WebItem { get { return this._WebItem; } set { this._WebItem = value; this.DataShow(); } }
        protected WebItem _WebItem;
        protected bool _DataNowChanging;
        /// <summary>
        /// Událost která je volaná po jakékoli změně dat na panelu
        /// </summary>
        public event EventHandler DataChanged;
        /// <summary>
        /// Vyvolá událost DataChanged
        /// </summary>
        protected virtual void OnDataChanged()
        {
            if (this.DataChanged != null)
                this.DataChanged(this, EventArgs.Empty);
        }
        /// <summary>
        /// Zobrazí všechna data z datového objektu WebItem do GUI.
        /// Bázová třída WebItemPanel ošetřuje _DataNowChanging, zobrazí Key, Level, CarryOut.
        /// Volá virtuální metodu OnDataShow(), kde zobrazuje svoje data potomek.
        /// </summary>
        protected void DataShow()
        {
            if (this._DataNowChanging) return;
            WebItem data = this._WebItem;
            if (data == null) return;

            this._DataNowChanging = true;

            this._TypeCmb.SelectedIndex = WebItemTypeToInt(data.Type);
            this._KeyTxt.Text = data.Key;
            this._LevelTxt.Value = data.Level;
            this._CarryOutChk.Checked = data.CarryOutIncrement;

            this.OnDataShow();

            this._DataNowChanging = false;
        }

        protected static int WebItemTypeToInt(WebItemType type)
        {
            switch (type)
            {
                case WebItemType.None: return 0;
                case WebItemType.NumericRange: return 1;
                case WebItemType.ValueList: return 2;
            }
            return 0;
        }
        /// <summary>
        /// Uloží všechna data z GUI do datového objektu WebItem.
        /// Bázová třída WebItemPanel ošetřuje _DataNowChanging, ukládá Key, Level, CarryOut.
        /// Volá virtuální metodu OnDataCollect(), kde zobrazuje svoje data potomek.
        /// Volá virtuální metodu OnDataChanged(), která volá event DataChanged.
        /// </summary>
        protected void DataCollect()
        {
            if (this._DataNowChanging) return;
            WebItem data = this._WebItem;
            if (data == null) return;

            this._DataNowChanging = true;

            data.Key = this._KeyTxt.Text;
            data.Level = (int)this._LevelTxt.Value;
            data.CarryOutIncrement = this._CarryOutChk.Checked;

            this.OnDataCollect();

            this.OnDataChanged();

            this._DataNowChanging = false;
        }
        /// <summary>
        /// Zde potomek zobrazuje svoje konkrétní data: z datového objektu WebItem do GUI.
        /// Bázová třída WebItemPanel zde nic nedělá.
        /// </summary>
        protected virtual void OnDataShow() { }
        /// <summary>
        /// Zde potomek ukládá svoje konkrétní data: z GUI do datového objektu WebItem.
        /// Bázová třída WebItemPanel zde nic nedělá.
        /// </summary>
        protected virtual void OnDataCollect() { }
        #endregion
    }
    #endregion
    #region class WebPanel, a další WinForm základní třídy
    public class WebPanel : Panel
    {
        public WebPanel()
        {
            this.InitPanel();
        }
        private void InitPanel()
        {
            this.BackColor = System.Drawing.SystemColors.Info;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        }
        /// <summary>
        /// Vytvoří button s akcí
        /// </summary>
        /// <param name="text"></param>
        protected void CreateActionButton(string text, ref int tabIndex)
        {
            this.CreateActionButton(text, null, ref tabIndex);
        }
        /// <summary>
        /// Vytvoří button s akcí
        /// </summary>
        /// <param name="text"></param>
        protected void CreateActionButton(string text, Image webImage, ref int tabIndex)
        {
            this.ActionButton = new WebButton(text, new Rectangle(831, 9, 145, 44), ref tabIndex) { Anchor = AnchTR };
            this.Controls.Add(this.ActionButton);
            this.ActionButton.WebImage = webImage;
            this.ActionButton.Click += new EventHandler(this.ActionButtonClick);
        }
        /// <summary>AnchorStyles: Top + Right</summary>
        protected static AnchorStyles AnchTR { get { return AnchorStyles.Top | AnchorStyles.Right; } }
        /// <summary>AnchorStyles: Bottom + Right</summary>
        protected static AnchorStyles AnchBR { get { return AnchorStyles.Bottom | AnchorStyles.Right; } }
        /// <summary>AnchorStyles: Top + Bottom + Right</summary>
        protected static AnchorStyles AnchTBR { get { return AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right; } }
        /// <summary>AnchorStyles: Left + Bottom</summary>
        protected static AnchorStyles AnchLB { get { return AnchorStyles.Left | AnchorStyles.Bottom; } }
        /// <summary>AnchorStyles: Left + Top + Bottom</summary>
        protected static AnchorStyles AnchLTB { get { return AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom; } }
        /// <summary>AnchorStyles: Left + Right + Top + Bottom</summary>
        protected static AnchorStyles AnchLRTB { get { return AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom; } }
        /// <summary>AnchorStyles: Bottom + Left + Right</summary>
        protected AnchorStyles AnchBLR { get { return AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; } }
        /// <summary>AnchorStyles: Top + Left + Right</summary>
        protected AnchorStyles AnchTLR { get { return AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; } }
        protected WebButton ActionButton;
        void ActionButtonClick(object sender, EventArgs e)
        {
            this.OnActionButton();
        }
        protected virtual void OnActionButton()
        {
        }
    }
    public class WebLabel : Label
    {
        public WebLabel() : base() { this.Init(); }
        public WebLabel(string text, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Text = text;
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        }
    }
    public class WebText : TextBox
    {
        public WebText() : base() { this.Init(); }
        public WebText(Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
        }
    }
    public class WebNumeric : NumericUpDown
    {
        public WebNumeric() : base() { this.Init(); }
        public WebNumeric(long minimum, long maximum, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Minimum = minimum;
            this.Maximum = maximum;
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
        }
    }
    public class WebCombo : ComboBox
    {
        public WebCombo() : base() { this.Init(); }
        public WebCombo(Rectangle bounds, ref int tabIndex, params object[] items)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.DropDownWidth = bounds.Width;
            if (items != null && items.Length > 0)
            {
                foreach (object item in items)
                    this.Items.Add(item);
            }
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.DropDownStyle = ComboBoxStyle.DropDownList;
        }
    }
    public class WebList : ListView
    {
        public WebList() : base() { this.Init(); }
        public WebList(Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Alignment = ListViewAlignment.Left;
            this.AllowColumnReorder = false;
            this.FullRowSelect = true;
            this.GridLines = false;
            this.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            this.HideSelection = false;
            this.HotTracking = false;
            this.HoverSelection = false;
            this.CheckBoxes = false;
            this.LabelEdit = false;
            this.MultiSelect = false;
            this.Scrollable = true;
            this.View = View.Details;
            this.UseCompatibleStateImageBehavior = false;
        }
    }
    public class WebCheck : CheckBox
    {
        public WebCheck() : base() { this.Init(); }
        public WebCheck(string text, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Text = text;
        }
        private void Init()
        {
            this.AutoSize = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.UseVisualStyleBackColor = true;
        }
    }
    public class WebButton : Button
    {
        public WebButton() : base() { this.Init(); }
        public WebButton(string text, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Text = text;
        }
        public WebButton(Image webImage, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Text = "";
            this.WebImage = webImage;
        }
        public WebButton(Image webImage, string text, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Text = text;
            this.WebImage = webImage;
        }
        private void Init()
        {
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.UseVisualStyleBackColor = true;
            // this.BackColor = SystemColors.ButtonFace;
        }
        public Image WebImage
        {
            get { return this.Image; }
            set
            {
                this.Image = value;
                if (value == null)
                {
                    this.ImageAlign = ContentAlignment.MiddleCenter;
                    this.TextAlign = ContentAlignment.MiddleCenter;
                    this.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
                }
                else
                {
                    this.ImageAlign = ContentAlignment.MiddleCenter;
                    this.TextAlign = ContentAlignment.MiddleRight;
                    this.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                }
            }
        }
    }
    public class WebTrack : TrackBar
    {
        public WebTrack() : base() { this.Init(); }
        public WebTrack(System.Windows.Forms.Orientation orientation, int min, int max, Rectangle bounds, ref int tabIndex)
        {
            this.Init();
            this.Orientation = orientation;
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TabIndex = tabIndex++;
            this.Minimum = min;
            this.Maximum = max;
            this.Value = min;
        }
        private void Init()
        {
        }
    }
    #endregion
}
