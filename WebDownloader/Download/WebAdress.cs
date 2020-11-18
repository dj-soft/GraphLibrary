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
    #region clas WebAdress
    /// <summary>
    /// Webová adresa s možnostmi inkrementace
    /// </summary>
    public class WebAdress : WebData
    {
        #region Konstrukce
        public WebAdress()
        {
            this.Items = new List<WebItem>();
            this.Clear(true);
        }
        /// <summary>
        /// Vzorec obsahující klíčové názvy proměnných
        /// </summary>
        public string Formula { get; set; }
        /// <summary>
        /// Seznam proměnných
        /// </summary>
        public List<WebItem> Items { get; private set; }
        /// <summary>
        /// Maximální počet simultánních downloadů
        /// </summary>
        public int ThreadMaxCount { get { int t = this._ThreadMaxCount; return (t < 1 ? 1 : (t > 12 ? 12 : t)); } set { int t = value; this._ThreadMaxCount = (t < 1 ? 1 : (t > 12 ? 12 : t)); } } private int _ThreadMaxCount;
        /// <summary>
        /// true pokud je aktuální adresa validní
        /// </summary>
        public bool IsValid { get { return !String.IsNullOrEmpty(this.Formula) && !String.IsNullOrEmpty(this.Text) && Uri.IsWellFormedUriString(this.Text, UriKind.Absolute); } }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        #endregion
        #region Klonování
        /// <summary>
        /// Obsahuje full-deep klon sebe sama
        /// </summary>
        public WebAdress Clone
        {
            get
            {
                WebAdress clone = new WebAdress();
                clone.Formula = this.Formula;
                clone.Items = new List<WebItem>();
                foreach (WebItem item in this.Items)
                    clone.Items.Add(item.Clone);
                return clone;
            }
        }
        #endregion
        #region Parsování vzorku
        /// <summary>
        /// Z dodaného vzorku adresy vrátí generátor adres
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static WebAdress CreateFromSample(string sample)
        {
            WebAdress adress = new WebAdress();
            if (String.IsNullOrEmpty(sample))
                return adress;

            adress.FillFromSample(sample);
            return adress;
        }
        /// <summary>
        /// Naplní do sebe data z dodaného vzorku
        /// </summary>
        /// <param name="sample"></param>
        public void FillFromSample(string sample)
        {
            List<WebNumericItem> items = new List<WebNumericItem>();
            string formula = "";
            string numeric = "";
            int length = sample.Length;

            for (int i = 0; i < length; i++)
            {
                char c = sample[i];
                bool isDigit = Char.IsDigit(c);
                bool isLast = (i == (length - 1));
                if (isDigit)
                {
                    numeric += c.ToString();
                }
                if (!isDigit || (isLast && numeric.Length > 0))
                {
                    if (numeric.Length > 0)
                    {
                        string key = "{{" + (items.Count + 1).ToString() + "}}";                   // {{1}} až {{999}}
                        WebNumericItem item = WebNumericItem.Parse(numeric, key);
                        numeric = "";
                        items.Add(item);
                        formula += key;
                    }
                    if (!isDigit)
                    {
                        formula += c.ToString();
                    }
                }
            }

            int level = items.Count;
            foreach (WebNumericItem item in items)
                item.Level = level--;

            this.Clear(true);
            this.Formula = formula;
            this.Items.AddRange(items);
        }
        /// <summary>
        /// Vyprázdní this
        /// </summary>
        public void Clear(bool clearConfigFile)
        {
            this.Formula = "";
            this.Items.Clear();
            this.ThreadMaxCount = 2;
            if (clearConfigFile)
                this.ConfigFileName = null;
        }
        #endregion
        #region Generování adresy, inkrementace
        /// <summary>
        /// Text aktuální adresy. Nedochází k inkrementaci.
        /// </summary>
        public string Text
        {
            get
            {
                string text = this.Formula;
                foreach (WebItem item in this.Items)
                {
                    string key = item.Key;
                    if (text.Contains(key))
                        text = text.Replace(key, item.Text);
                }
                return text;
            }
        }
        /// <summary>
        /// Navýší hodnotu v prvcích o jeden krok.
        /// Vrací true = jsme na konci (čítače se vynulovaly) / false = máme další platnou adresu.
        /// </summary>
        /// <returns></returns>
        public bool Increment()
        {
            bool carryOut = true;                                              // Pokud bych neměl žádnou proměnnou (žádný prvek), pak vrátím true = cyklus skončil
            List<IGrouping<int, WebItem>> groups = this.Items.GroupBy(i => i.Level).ToList();
            groups.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (IGrouping<int, WebItem> group in groups)
            {   // Skupiny se stejnou hladinou (teoreticky můžu mít dva prvky, jejichž hladina == 1, a jsou tedy inkrementovány společně):
                carryOut = false;                                              // Každá hladina začíná se stavem "CarryOut = false"
                foreach (WebItem item in group)
                    // Jednotlivé prvky se stejnou hladinou:
                    carryOut |= item.Increment();                              // Pokud alespoň jeden prvek ohlásí true, pak máme přenos nahoru (CarryOut).
                if (!carryOut)
                    break;                                                     // Pokud žádný prvek neohlásí true, pak nepůjdeme na další hladinu.
            }
            return carryOut;
        }
        #endregion
        #region Load & Save
        /// <summary>
        /// true pokud tuto konfiguraci je možno uložit (má název nebo název lze vytvořit)
        /// </summary>
        public bool CanSave
        {
            get
            {
                string fileName = GetConfigFileName();
                return !String.IsNullOrEmpty(fileName);
            }
        }
        /// <summary>
        /// Uloží doklad, pokud lze určit název souboru. Pokud nelze, neukládá ani nehlásí problém.
        /// Nehlásí ani problémy při ukládání (práva atd).
        /// </summary>
        public void SaveAuto()
        {
            if (!this.CanSave) return;
            try
            {
                this.Save();
            }
            catch { }
        }
        /// <summary>
        /// Uloží konfiguraci do daného souboru
        /// </summary>
        /// <returns></returns>
        public void Save()
        {
            string fileName = GetConfigFileName();
            if (String.IsNullOrEmpty(fileName))
                throw new AppException("Adresu nelze uložit, není zadán platný vzorec adresy (plný URL odkaz).");
            this.Save(fileName);
        }
        /// <summary>
        /// Uloží konfiguraci do daného souboru
        /// </summary>
        /// <returns></returns>
        public void Save(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new AppException("Pro uložení adresy není zadán název cílového souboru.");
            App.CreatePath(Path.GetDirectoryName(fileName));
            this.ConfigFileName = fileName;
            using (FileStream stream = File.OpenWrite(fileName))
            {
                this.Save(stream);
            }
        }
        /// <summary>
        /// Uloží konfiguraci do dodaného streamu
        /// </summary>
        /// <returns></returns>
        public void Save(Stream stream)
        {
            SaveLine(stream, NAME_TITLE);
            SaveLine(stream, CreatePair(NAME_FORMULA, this.Formula, false));
            SaveLine(stream, CreatePair(NAME_THREADMAXCOUNT, this.ThreadMaxCount, false));
            foreach (WebItem item in this.Items)
                SaveLine(stream, item.Save());
        }
        /// <summary>
        /// Načte konfiguraci ze souboru
        /// </summary>
        /// <param name="fileName"></param>
        public void Load(string fileName)
        {
            if (String.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return;
            using (FileStream stream = File.OpenRead(fileName))
            {
                this.Load(stream);
            }
            this.ConfigFileName = fileName;
        }
        /// <summary>
        /// Načte do sebe definici z textu
        /// </summary>
        /// <param name="data"></param>
        public void Load(Stream stream)
        {
            this.Clear(false);

            string line;
            line = LoadLine(stream);                  // První řádek může být NAME_TITLE nebo může obsahovat NAME_FORMULA, nebo může být NULL
            if (line == null) return;
            if (line == NAME_TITLE)                   // Řádek s titulkem přeskočíme. Může a nemusí být na začátku, on může sloužit i jako identifikátor bloku s daty.
            {
                line = LoadLine(stream);
                if (line == null) return;
            }

            string name = NAME_FORMULA + KEYVALUE_SEPARATOR;
            if (!line.StartsWith(name))
                return;                               // Stream má začínat řádkem obsahujícím vzorec, například: "Formula:http://www.server.net/paths{{1}}/file{{2}}.jpeg"
            this.Formula = line.Substring(name.Length);

            while (stream.Position < stream.Length)
            {   // Jeden řádek = jedna položka vzorce, nebo jiná data: typicky "NumericRange Key:{{1}},Level:2,CarryOut:Y,Value:45,Length:3,RangeFrom:100,RangeTo:999,Step:1"
                line = LoadLine(stream);
                if (line == null) break;

                List<KeyValuePair<string, string>> list;
                name = LoadFromString(line, out list);
                switch (name)
                {   // Název řádku určuje jeho obsah
                    case "None":
                        break;
                    case "NumericRange":
                        WebNumericItem item = new WebNumericItem();
                        item.Load(list);
                        this.Items.Add(item);
                        break;
                    case "ValueList":
                        break;
                }
            }
        }
        /// <summary>
        /// Vrátí jméno pro ukládání souboru.
        /// Vrátí null pokud neexistuje a nelze jej ani vytvořit.
        /// </summary>
        /// <returns></returns>
        protected string GetConfigFileName()
        {
            string fileName = this.ConfigFileName;
            if (String.IsNullOrEmpty(fileName))
                fileName = this.CreateDefaultConfigFileName();
            return fileName;
        }
        /// <summary>
        /// Vytvoří a vrátí plné jméno pro uložení této adresy jako konfigurační soubor
        /// </summary>
        /// <returns></returns>
        public string CreateDefaultConfigFileName()
        {
            string url = this.Text;
            if (String.IsNullOrEmpty(url)) return null;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            string name = uri.Host;             // "www.seznam.cz"

            string configPath = PresetPath;     // AppData/Djs/WebDownloader/presets
            App.CreatePath(configPath);

            for (int i = 1; i < 9999; i++)
            {
                string fullName = Path.Combine(configPath, name + "_" + i.ToString().PadLeft(4, '0') + "." + EXTENSION);
                if (!File.Exists(fullName))
                    return fullName;
            }
            return null;
        }
        /// <summary>
        /// Obsahuje adresář, kam se ukládají data konfigurací adres.
        /// Typicky: "(WinAppData)/Djs/WebDownloader/presets"
        /// </summary>
        public static string PresetPath { get { return Path.Combine(App.ConfigPath, "presets"); } }
        /// <summary>
        /// Název souboru, v němž je uložena konfigurace.
        /// Po parsování nového textu je vynulován.
        /// </summary>
        public string ConfigFileName { get; private set; }
        /// <summary>== WebDownloader v3.0 data ==</summary>
        protected const string NAME_TITLE = "== WebDownloader v3.0 data ==";
        /// <summary>Formula</summary>
        protected const string NAME_FORMULA = "Formula";
        /// <summary>ThreadMaxCount</summary>
        protected const string NAME_THREADMAXCOUNT = "ThreadMaxCount";
        /// <summary>wdp</summary>
        public const string EXTENSION = "wdp";
        #endregion
    }
    #endregion
    #region UI
    #region WebSamplePanel
    public class WebSamplePanel : WebPanel
    {
        public WebSamplePanel()
        {
            this.Init();
        }
        protected void Init()
        {
            int tabIndex = 0;
            this._SampleLbl = new WebLabel("Ukázka adresy:", new Rectangle(11, 9, 102, 16), ref tabIndex);
            this._SampleTxt = new WebText(new Rectangle(14, 29, 805, 24), ref tabIndex) { Anchor = AnchTLR };
            this.SuspendLayout();

            this.Controls.Add(this._SampleLbl);
            this.Controls.Add(this._SampleTxt);

            this.CreateActionButton("NAJDI", Properties.Resources.view_nofullscreen_2, ref tabIndex); // system_search_4

            this.Size = new System.Drawing.Size(993, 62);

            this.ResumeLayout(false);
        }
        protected override void OnActionButton()
        {
            if (this.Parse != null)
                this.Parse(this, EventArgs.Empty);
        }
        private System.Windows.Forms.TextBox _SampleTxt;
        private System.Windows.Forms.Label _SampleLbl;
        /// <summary>
        /// Událost, kdy se má parsovat text this.SampleText
        /// </summary>
        public event EventHandler Parse;
        /// <summary>
        /// Text vzorku
        /// </summary>
        public string SampleText
        {
            get { return this._SampleTxt.Text; }
            set { this._SampleTxt.Text = value; }
        }
    }
    #endregion
    #region WebAdressPanel
    /// <summary>
    /// Adresní panel (vzorec, seznam, panel s detailem konkrétní položky)
    /// </summary>
    public class WebAdressPanel : WebPanel
    {
        #region Konstrukce
        public WebAdressPanel()
        {
            this._WebAdress = new WebAdress();
            this.Init();
        }
        protected void Init()
        {
            int tabIndex = 0;
            this._FormulaLbl = new WebLabel("Vzorec adresy, nalezené číselné řady:", new Rectangle(11, 9, 300, 16), ref tabIndex) { TextAlign = ContentAlignment.MiddleLeft };
            this._FormulaTxt = new WebText(new Rectangle(14, 29, 805, 24), ref tabIndex) { Anchor = AnchTLR };
            this._FormulaTxt.TextChanged += new EventHandler(_FormulaTxt_TextChanged);
            this._ItemsLst = new WebList(new Rectangle(14, 59, 250, 168), ref tabIndex) { Anchor = AnchLTB };
            this.InitItemsLstColumns();
            this._ItemsLst.SelectedIndexChanged += new EventHandler(_ItemsLst_SelectedIndexChanged);
            this._ItemNumericPanel = new WebNumericPanel();
            this._ItemNumericPanel.DataChanged += new EventHandler(_ItemNumericPanel_DataChanged);
            this._ThreadLbl = new WebLabel("Současně", new Rectangle(871, 58, 69, 16), ref tabIndex) { TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchTR };
            this._ThreadTrc = new WebTrack(Orientation.Vertical, 1, 12, new Rectangle(886, 77, 45, 151), ref tabIndex) { LargeChange = 2, TickStyle = TickStyle.TopLeft, Anchor = AnchTBR };
            this._ThreadTrc.ValueChanged += new EventHandler(_ThreadTrc_ValueChanged);
            this._ThreadTxt = new WebText(new Rectangle(886, 230, 39, 20), ref tabIndex) { Enabled = false, TextAlign = HorizontalAlignment.Center, Anchor = AnchBR };
            this._CurrentTxt = new WebText(new Rectangle(14, 230, 805, 24), ref tabIndex) { ReadOnly = true, Anchor = AnchBLR };
            
            ((System.ComponentModel.ISupportInitialize)(this._ThreadTrc)).BeginInit();
            this._ItemNumericPanel.SuspendLayout();
            this.SuspendLayout();

            this.Controls.Add(this._FormulaLbl);
            this.Controls.Add(this._FormulaTxt);
            this.Controls.Add(this._ItemsLst);
            this.Controls.Add(this._ItemNumericPanel);
            this.Controls.Add(this._ThreadLbl);
            this.Controls.Add(this._ThreadTrc);
            this.Controls.Add(this._ThreadTxt);
            this.Controls.Add(this._CurrentTxt);


            // _ItemPanel
            this._ItemNumericPanel.Location = new System.Drawing.Point(270, 59);
            this._ItemNumericPanel.Size = new System.Drawing.Size(549, 168);
            // this._ItemNumericPanel.MinimumSize = this._ItemNumericPanel.Size;
            this._ItemNumericPanel.TabIndex = tabIndex++;
            this._ItemNumericPanel.Anchor = AnchLRTB;
            this._ItemNumericPanel.Visible = false;

            this.CreateActionButton("NÁHLED", Properties.Resources.text_x_preview, ref tabIndex);

            this.Size = new System.Drawing.Size(993, 263);

            ((System.ComponentModel.ISupportInitialize)(this._ThreadTrc)).EndInit();
            this._ItemNumericPanel.ResumeLayout(false);
            this._ItemNumericPanel.PerformLayout();
            this.ResumeLayout(false);
        }
        private void InitItemsLstColumns()
        {
            this._ItemsLst.Columns.Clear();
            this._ItemsLst.Columns.Add("Key");
            this._ItemsLst.Columns.Add("Level");
            this._ItemsLst.Columns.Add("Value");
            this._ItemsLst.Columns[0].Width = 70;
            this._ItemsLst.Columns[1].Width = 50;
            this._ItemsLst.Columns[2].Width = 100;
        }
        void _FormulaTxt_TextChanged(object sender, EventArgs e)
        {
            if (this._WebAdress != null)
            {
                this._WebAdress.Formula = this._FormulaTxt.Text;
                this.OnDataChanged();
                this.DataShowCurrent();
            }
        }
        private void _ThreadTrc_ValueChanged(object sender, EventArgs e)
        {
            this._ThreadTxt.Text = this._ThreadTrc.Value.ToString();

            if (this._WebAdress != null)
            {
                this._WebAdress.ThreadMaxCount = this._ThreadTrc.Value;
                this.OnDataChanged();
            }

            this.OnThreadMaxCountChanged();
        }
        private void _ItemNumericPanel_DataChanged(object sender, EventArgs e)
        {
            this.ItemListRefresh(false);
            if (this._WebAdress != null)
            {
                this.OnDataChanged();
            }
        }
        private WebLabel _FormulaLbl;
        private WebText _FormulaTxt;
        private WebList _ItemsLst;
        private WebNumericPanel _ItemNumericPanel;
        private WebLabel _ThreadLbl;
        private WebTrack _ThreadTrc;
        private WebText _ThreadTxt;
        private WebText _CurrentTxt;
        /// <summary>
        /// Událost, kdy uživatel změnil data adresy, někdo na to může chtít reagovat.
        /// Panel sám zajišťuje automatické ukládání dat po změně v závislosti na konfiguraci, to se už nemusí řešit.
        /// </summary>
        public event EventHandler DataChanged;
        /// <summary>
        /// Událost, kdy uživatel změnil hodnotu ThreadMaxCount.
        /// </summary>
        public event EventHandler ThreadMaxCountChanged;
        /// <summary>
        /// Hodnota ThreadMaxCount načtená přímo z odpovídajícího controlu
        /// </summary>
        public int ThreadMaxCount { get { return this._ThreadTrc.Value; } }
        /// <summary>
        /// Událost, kdy se má zobrazit náhled adres (uživatel klikl na tlačítko NÁHLED)
        /// </summary>
        public event EventHandler Preview;
        protected override void OnActionButton()
        {
            if (this.Preview != null)
                this.Preview(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po každé změně dat v this.WebAdress.
        /// Zajistí AutoSave() a event DataChanged.
        /// </summary>
        protected virtual void OnDataChanged()
        {
            if (App.Config.SaveAutomatic)
                this.WebAdress.SaveAuto();
            if (this.DataChanged != null)
                this.DataChanged(this, EventArgs.Empty);
        }
        /// <summary>
        /// Je voláno po změně hodnoty this.ThreadMaxCount.
        /// Vnější aplikace může reagovat tak, že tuto hodnotu živě předá do downloadu.
        /// </summary>
        protected virtual void OnThreadMaxCountChanged()
        {
            if (this.ThreadMaxCountChanged != null)
                this.ThreadMaxCountChanged(this, EventArgs.Empty);
        }
        
        #endregion
        #region Data
        /// <summary>
        /// Do this objektu rozepíše daný text jako vzorek (provede parsování a zobrazení dat).
        /// </summary>
        /// <param name="sample"></param>
        public void Parse(string sample)
        {
            this.WebAdress.FillFromSample(sample);
            this.DataShow();
        }
        private void _ItemsLst_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ListItemChanged();
        }
        /// <summary>
        /// Zobrazí všechna data.
        /// Smaže obsah listu a znovu jej vygeneruje (což bliká), selectuje první řádek listu.
        /// </summary>
        private void DataShow()
        {
            WebAdress data = this._WebAdress;
            if (data == null || this._FormulaTxt == null) return;

            this._FormulaTxt.Text = data.Formula;

            this._ItemsLst.Items.Clear();
            this._ItemsLst.FocusedItem = null;
            foreach (WebItem item in data.Items)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = item.Key;
                lvi.SubItems.Add(item.Level.ToString());
                lvi.SubItems.Add(item.Text);
                lvi.Tag = item;
                this._ItemsLst.Items.Add(lvi);
            }

            if (this._ItemsLst.Items.Count > 0)
            {
                this._ItemsLst.FocusedItem = this._ItemsLst.Items[0];
                this._ItemsLst.SelectedIndices.Add(0);
            }
            this.ListItemChanged();
            this._ThreadTrc.Value = data.ThreadMaxCount;
            this._ThreadTxt.Text = data.ThreadMaxCount.ToString();
            this.DataShowCurrent();
        }
        /// <summary>
        /// Zobrazí aktuální podobu adresy vygenerovanou z WebAdress jako Text, v textboxu this._CurrentTxt
        /// </summary>
        private void DataShowCurrent()
        {
            WebAdress data = this._WebAdress;
            if (data == null || this._FormulaTxt == null) return;
            this._CurrentTxt.Text = data.Text;
        }
        /// <summary>
        /// Provede se po změně vybraného řádku v Listu prvků.
        /// Zajistí zobrazení detailu dat položky a zobrazení CurrentText.
        /// </summary>
        private void ListItemChanged()
        {
            WebItem item = (this._ItemsLst.FocusedItem == null ? null : this._ItemsLst.FocusedItem.Tag) as WebItem;
            this._WebItem = item;
            this.DataShowItem();
            this.DataShowCurrent();
        }
        private void DataShowItem()
        {
            WebItemType type = (this._WebItem != null ? this._WebItem.Type : WebItemType.None);

            this._ItemNumericPanel.Visible = (type == WebItemType.NumericRange);
            if (type == WebItemType.NumericRange)
                this._ItemNumericPanel.WebItem = this._WebItem as WebNumericItem;
        }
        private void ItemListRefresh(bool allItems)
        {
            if (allItems)
            {
                foreach (ListViewItem lvi in this._ItemsLst.Items)
                    this.ItemListRefreshOne(lvi);
            }
            else
            {
                if (this._ItemsLst.FocusedItem != null)
                    this.ItemListRefreshOne(this._ItemsLst.FocusedItem);
            }
            this.DataShowCurrent();
        }
        private void ItemListRefreshOne(ListViewItem lvi)
        {
            if (lvi == null) return;
            WebItem item = lvi.Tag as WebItem;
            if (item != null)
            {
                lvi.Text = item.Key;
                lvi.SubItems[1].Text = item.Level.ToString();
                lvi.SubItems[2].Text = item.Text;

                this._ItemsLst.Refresh();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebAdress WebAdress { get { return this._WebAdress; } set { this._WebAdress = value; this.DataShow(); } }
        private WebAdress _WebAdress;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebItem WebItem { get { return this._WebItem; } }
        private WebItem _WebItem;
        #endregion
        #region Buttony
        protected  void xOnActionButton()
        {
            /*
            using (UI.PreviewForm pvf = new UI.PreviewForm())
            {
                pvf.WebAdress = this.WebAdress.Clone;
                pvf.ShowDialog(this.FindForm());
            }
            */

            /*
            byte[] data = null;
            string text = null;

            using (System.IO.MemoryStream sw = new MemoryStream())
            {
                this.WebAdress.Save(sw);
                data = sw.ToArray();
                text = WebData.Encoding.GetString(data);
            }
            data = WebData.Encoding.GetBytes(text);
            using (System.IO.MemoryStream sw = new MemoryStream(data))
            {
                sw.Seek(0L, SeekOrigin.Begin);
                this.WebAdress.Load(sw);
            }
            this.DataShow();
             */
        }

        void _LoadBtn_Click(object sender, EventArgs e)
        {
            string url = this.WebAdress.Text;
            DownloadItem dwnl = new DownloadItem();
            dwnl.Start(0, url);
        }
        
        #endregion
    }
    #endregion
    #endregion
}
