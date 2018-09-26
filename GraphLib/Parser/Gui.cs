using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Asol.Tools.WorkScheduler.TextParser
{
    /// <summary>
    /// Panel, který zapouzdřuje editor textu spojený s Parserem se zvýrazněním syntaxe (za pomoci RTF editoru).
    /// Editor má dvě zásadní property: ParserSetting (sem se vkládají pravidla pro parsování), EditedText (vstup / výstup textu), a událost EditedTextChanged (vyvolá se po každé změně textu).
    /// 
    /// </summary>
    public class GuiRtfEditor : Panel
    {
        #region Konstrukce, proměnné
        /// <summary>
        /// Vytvoří Editor
        /// </summary>
        public GuiRtfEditor()
        {
            this.InitComponents();
        }
        private void InitComponents()
        {
            this.DoubleBuffered = true;

            this.RtfEditor = new GuiRtfTextBox();
            this.RtfEditor.Dock = DockStyle.Fill;
            this.RtfEditor.TextChanged += new EventHandler(this._RtfEditor_TextChanged);
            this.Controls.Add(this.RtfEditor);

            this.GotFocus += new EventHandler(_Editor_GotFocus);
        }
        void _Editor_GotFocus(object sender, EventArgs e)
        {
            this.RtfEditor.Focus();
        }
        /// <summary>
        /// Objekt RTF editoru
        /// </summary>
        private GuiRtfTextBox RtfEditor;
        /// <summary>
        /// Úložiště pro property ParserSetting
        /// </summary>
        private ParserSetting _ParserSetting;
        #endregion
        #region Public property, eventy, metody
        /// <summary>
        /// Uložený objekt settingu, který slouží k parsování zadaného textu a k detekci jeho segmentů a klíčových slov.
        /// Výchozí setting je ParserDefaultSetting.MsSql.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ParserSetting ParserSetting
        {
            get
            {
                if (this._ParserSetting == null)
                    this._ParserSetting = ParserDefaultSetting.MsSql;  // ParserDefaultSetting.MsSql
                return this._ParserSetting;
            }
            set
            {
                this._ParserSetting = value;
                this.RtfTextParseAndColor();
            }
        }
        /// <summary>
        /// Editovaný text, celý.
        /// Po vložení není vložený text označen.
        /// Vyvolá se event o změně (this.EditedTextChanged).
        /// Pokud se text změní z eventu this.EditedTextChanged, nezpůsobí to nové vyvolání tohoto eventu (rekurzivní), Control tomu sám zabrání.
        /// Nicméně ostatní akce po změně textu proběhnou = nový text je syntakticky podbarvem a parsované segmenty jsou uloženy do this.EditorSegments.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Editovaný text")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EditedText
        {
            get { return this.RtfEditor.Text; }
            set { this.RtfEditor.Text = value; }
        }
        /// <summary>
        /// Editovaný text, aktuálně označená část (lze číst i vkládat).
        /// Po vložení textu je původní text přepsán (ztratí se) a vložený text je označen (Selected).
        /// Vyvolá se event o změně (this.EditedTextChanged).
        /// Pokud se text změní z eventu this.EditedTextChanged, nezpůsobí to nové vyvolání tohoto eventu (rekurzivní), Control tomu sám zabrání.
        /// Nicméně ostatní akce po změně textu proběhnou = nový text je syntakticky podbarvem a parsované segmenty jsou uloženy do this.EditorSegments.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get { return this.RtfEditor.SelectedText; }
            set
            {
                int selStart = this.RtfEditor.SelectionStart;
                this.RtfEditor.SelectedText = value;
                this.RtfEditor.SelectionStart = selStart;
                this.RtfEditor.SelectionLength = value.Length;
            }
        }
        /// <summary>
        /// Aktuální pozice kurzoru v textu
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get { return this.RtfEditor.SelectionStart; }
            set { this.RtfEditor.SelectionStart = value; }
        }
        /// <summary>
        /// Aktuální délka výběru (označený text) v textu
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionLength
        {
            get { return this.RtfEditor.SelectionLength; }
            set { this.RtfEditor.SelectionLength = value; }
        }
        /// <summary>
        /// Vybere (označí) daný blok textu, daný jeho pozicí.
        /// </summary>
        /// <param name="selectionStart"></param>
        /// <param name="selectionLength"></param>
        public void SelectRange(int selectionStart, int selectionLength)
        {
            this.SelectRange(selectionStart, selectionLength, false);
        }
        /// <summary>
        /// Vybere (označí) daný blok textu, daný jeho pozicí.
        /// 
        /// </summary>
        /// <param name="selectionStart"></param>
        /// <param name="selectionLength"></param>
        /// <param name="indexIsTxt"></param>
        public void SelectRange(int selectionStart, int selectionLength, bool indexIsTxt)
        {
            if (indexIsTxt)
            {
                int begin = this.RtfEditor.GetRtfCharIndex(selectionStart);
                int end = this.RtfEditor.GetRtfCharIndex(selectionStart + selectionLength);
                selectionStart = begin;
                selectionLength = end - begin;
            }

            this.RtfEditor.SelectionStart = selectionStart;
            this.RtfEditor.SelectionLength = selectionLength;
            if (this.RtfEditor.HideSelection)
                this.RtfEditor.HideSelection = false;
        }
        /// <summary>
        /// Povolení editace textu
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Povolení editace textu")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool EditorEnabled
        {
            get { return !this.RtfEditor.ReadOnly; }
            set
            {
                this.RtfEditor.ReadOnly = !value;
                this.RtfEditor.BackColor = (value ? SystemColors.ControlLightLight : SystemColors.ControlLight);
            }
        }
        /// <summary>
        /// Reference na editor pro další přímé zásahy.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GuiRtfTextBox RtfTextBox { get { return this.RtfEditor; } }
        /// <summary>
        /// Parsované segmenty aktuálního textu. Po každé změně textu jsou aktuální.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<ParserSegment> EditorSegments
        {
            get { return this._EditorSegments; }
            private set { this.OnEditorSegmentsChangedBefore();  this._EditorSegments = value; this.OnEditorSegmentsChangedAfter(); }
        }
        private IEnumerable<ParserSegment> _EditorSegments;
        /// <summary>
        /// Metoda vrátí index znaku v RTF controlu pro zadaný index znaku TXT.
        /// Rozdíl je dán jedním znakem za každý zlom řádku: v TXT formátu je konec řádku reprezentován dvěma znaky (CR a LF), 
        /// kdežto v RTF controlu se tentýž znak nachází na indexu o 1 menším za každý předchozí řádek textu.
        /// Tato metoda tedy 
        /// </summary>
        /// <param name="txtCharIndex"></param>
        /// <returns></returns>
        public int GetRtfCharIndex(int txtCharIndex)
        {
            return this.RtfEditor.GetRtfCharIndex(txtCharIndex);
        }
        /// <summary>
        /// Událost po změně textu SQL příkazu, proběhne po dokončení parsování.
        /// Pokud se v tomto eventu provede změna textu, (což by vyvolalo rekurzi), provede se další parsování textu, aktualizace pole segmentů this.EditorSegments,
        /// ale již se nevyvolá (rekurzivně) tento event EditedTextChanged.
        /// Tedy control sám aktivně brání zacyklení eventu.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Událost po změně textu SQL příkazu, proběhne po dokončení parsování")]
        public event EventHandler EditedTextChanged;
        /// <summary>
        /// Událost před změnou obsahu segmentů v this.EditorSegments, změna proběhne ihned po dokončení eventu.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Událost před změnou obsahu segmentů v this.EditorSegments, změna proběhne ihned po dokončení eventu.")]
        public event EventHandler EditorSegmentsChangedBefore;
        /// <summary>
        /// Událost před změnou obsahu segmentů v this.EditorSegments, změna proběhne ihned po dokončení eventu.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Událost po změně obsahu segmentů v this.EditorSegments, změna proběhla před vyvoláním tohoto eventu.")]
        public event EventHandler EditorSegmentsChangedAfter;
        /// <summary>
        /// Zpráva která má být zobrazena ve stavovém řádku.
        /// Je možno využít event StatusMessageChanged
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Zpráva která má být zobrazena ve stavovém řádku. Je možno využít event StatusMessageChanged.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string StatusMessage { get { return this._StatusMessage; } protected set { this._StatusMessage = value; this.OnStatusMessageChanged(); } } private string _StatusMessage;
        /// <summary>
        /// Čas posledního zpracování dat. Součástí zpracování je i kompletní práce v GUI.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Čas posledního zpracování dat. Součástí zpracování je i kompletní práce v GUI.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal? LastWorkingTime { get; private set; }
        /// <summary>
        /// Velikost posledně zpracovaných dat v Byte.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Velikost posledně zpracovaných dat v Byte.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? LastParsedLength { get; private set; }
        /// <summary>
        /// Čas posledního parsování dat. Jeho součástí není zpracování v GUI, jde o čistý čas parsování.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Čas posledního parsování dat. Jeho součástí není zpracování v GUI, jde o čistý čas parsování.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal? LastParsingTime { get; private set; }
        /// <summary>
        /// Událost po změně textu do stavového řádku
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Událost po změně textu do stavového řádku")]
        public event EventHandler StatusMessageChanged;
        /// <summary>
        /// Vyvolá event StatusMessageChanged
        /// </summary>
        protected virtual void OnStatusMessageChanged()
        {
            if (this.StatusMessageChanged != null)
                this.StatusMessageChanged(this, EventArgs.Empty);
        }
        #endregion
        #region Editace RTF textu, parsování, syntax coloring
        /// <summary>
        /// Změnila se hodnota textu v editoru. Mělo by proběhnout parsování a syntax coloring.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _RtfEditor_TextChanged(object sender, EventArgs e)
        {
            bool runEvent = false;
            if (!this._SuppressRtfEditorTextChanged)
            {
                string currentText = this.RtfEditor.Text;
                if (this._IsTextChanged(currentText))
                {
                    try
                    {
                        this._SuppressRtfEditorTextChanged = true;
                        if (currentText.Length < 1000)
                            this.RtfTextParseAndColor();
                        else
                            this._TimerToParsesStart();
                        runEvent = (this.EditedTextChanged != null);
                    }
                    finally
                    {
                        this._SuppressRtfEditorTextChanged = false;
                    }
                }
            }

            // Toto řešení, kdy vyvolání eventu EditedTextChanged je umístěno až za blok if (!this._SuppressRtfEditorTextChanged) umožňuje následující chování:
            //  a) Uživatel změní text => proběhne jeho parsování a SyntaxColoring (v bloku _SuppressRtfEditorTextChanged, takže se interně nezacyklí)
            //  b) Probíhající SyntaxColoring změní RTF text, ale uvnitř bloku _SuppressRtfEditorTextChanged, takže rekurzivní SyntaxColoring neproběhne, 
            //     a proměnná runEvent v druhé iteraci této metody _RtfEditor_TextChanged() zůstane false (druhá iterace neprovede nic)
            //  c) Po provedení SyntaxColoring (metoda RtfTextParseAndColor()) se nastaví runEvent na true (pokud je zadán delegát), 
            //     a protože implicitně není event potlačen (_SuppressEventEditedTextChanged), provede se event - ale po jeho dobu se potlačí jeho rekurze pomocí _SuppressEventEditedTextChanged.
            //  d) Nyní může eventhandler klidně změnit text (pokud například najde chyby), tím dojde k další iteraci této metody _RtfEditor_TextChanged(), 
            //     přičemž není potlačen SyntaxColoring => provede se (změna textu se barevně projeví),
            //     ale je potlačeno volání externího eventu (_SuppressEventEditedTextChanged je true), takže změna textu, kterou provede uživatel zevnitř svého eventu jej nevyvolá rekurzivně !!!
            //  Pokud ale uživatel změní text odjinud než ze svého eventu, pak se jeho event vyvolá (protože není nastaven _SuppressEventEditedTextChanged na true).
            if (runEvent && !this._SuppressEventEditedTextChanged)
            {   // Pokud se má volat externí event, a není potlačeno jeho volání:
                try
                {
                    this._SuppressEventEditedTextChanged = true;
                    this.EditedTextChanged(this, EventArgs.Empty);
                }
                finally
                {
                    this._SuppressEventEditedTextChanged = false;
                }
            }
        }
        /// <summary>
        /// Před změnou segmentu v editoru
        /// </summary>
        protected virtual void OnEditorSegmentsChangedBefore()
        {
            if (!this.SuppressEditorSegmentsChangedEvents && this.EditorSegmentsChangedBefore != null)
            {
                try
                {
                    this.SuppressEditorSegmentsChangedEvents = true;
                    this.EditorSegmentsChangedBefore(this, EventArgs.Empty);
                }
                finally
                {
                    this.SuppressEditorSegmentsChangedEvents = false;
                }
            }
        }
        /// <summary>
        /// Po změně segmentu v editoru
        /// </summary>
        protected virtual void OnEditorSegmentsChangedAfter()
        {
            if (!this.SuppressEditorSegmentsChangedEvents && this.EditorSegmentsChangedAfter != null)
            {
                try
                {
                    this.SuppressEditorSegmentsChangedEvents = true;
                    this.EditorSegmentsChangedAfter(this, EventArgs.Empty);
                }
                finally
                {
                    this.SuppressEditorSegmentsChangedEvents = false;
                }
            }
        }
        /// <summary>
        /// Potlačí provedení událostí <see cref="EditorSegmentsChangedBefore"/> a <see cref="EditorSegmentsChangedAfter"/>
        /// </summary>
        protected bool SuppressEditorSegmentsChangedEvents = false;
        /// <summary>
        /// Zjistí, zda daný text je jiný, než který se posledně parsoval
        /// </summary>
        /// <param name="currentText"></param>
        /// <returns></returns>
        private bool _IsTextChanged(string currentText)
        {
            if (currentText == null) return false;
            if (this._LastParsedText == null) return true;
            return !String.Equals(currentText, this._LastParsedText);
        }
        private string _LastParsedText = null;
        /// <summary>
        /// Zajistí syntaktické zpracování textu v RTF editoru
        /// </summary>
        private void RtfTextParseAndColor()
        {
            int visStart = this.RtfEditor.GetCharIndexFromPosition(new Point(2, 5));        // Index znaku, který je první viditelný vlevo nahoře v editoru
            int selStart = this.RtfEditor.SelectionStart;
            int selLength = this.RtfEditor.SelectionLength;
            string text = this.RtfEditor.Text;
            this.LastParsedLength = null;

            // RTF control do property Text vkládá konec řádku jako znak 0x0a (jen LF).
            // Parseru to nevadí, ale následně obsahuje taky jen LF a ne CR, a controly TextBox to nezobrazí dobře.
            // Proto LF rozšířím na CrLf:
            text = text.Replace("\n", "\r\n");

            this.StatusMessage = "Parsing text: " + (text.Length / 1024).ToString() + "KB";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            string rtfText = RtfTextGetSyntaxColor(text);
            sw.Stop();
            this.LastParsedLength = text.Length;

            if (rtfText != null)
            {
                try
                {
                    this.RtfEditor.SuspendPaint = true;
                    this.RtfEditor.WmMsg = "";
                    this.RtfEditor.Rtf = rtfText;
                    this.RtfEditor.SelectionStart = visStart;
                    this.RtfEditor.ScrollToCaret();
                    this.RtfEditor.SelectionStart = selStart;
                    if (selLength > 0)
                        this.RtfEditor.SelectionLength = selLength;
                }
                finally
                {
                    this.RtfEditor.SuspendPaint = false;
                }
            }

            this.LastWorkingTime = (decimal)sw.ElapsedTicks / (decimal)System.Diagnostics.Stopwatch.Frequency;
            this.StatusMessage = "Parsing text done.";

            this._LastParsedText = text;
        }
        /// <summary>
        /// Vrátí RTF text obsahující syntakticky zpracovaný vstupující holý text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string RtfTextGetSyntaxColor(string text)
        {
            ParserSetting setting = this.ParserSetting;

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            List<ParserSegment> segments = Parser.ParseString(text, setting);
            this.LastParsingTime = (decimal)sw.ElapsedTicks / (decimal)System.Diagnostics.Stopwatch.Frequency;

            this.EditorSegments = segments;                          // Tady se vyvolá event EditorSegmentsChangedBefore i EditorSegmentsChangedAfter. Pokud je na this editor napojen GuiTreeView, pak nyní dojde k jeho přenačtení...
            if (segments == null || segments.Count == 0) return null;
            string rtfText = ParserSegment.GetFormattedText(segments, this.RtfCoder);
            this.RtfCoder.Clear();                                   // RtfCoder si po použití ponechává v paměti data, ale my je už nepotřebujeme...
            return rtfText;
        }
        /// <summary>
        /// Instance RtfCoderu
        /// </summary>
        protected RtfCoder RtfCoder
        {
            get
            {
                if (this._RtfCoder == null)
                    this._RtfCoder = new RtfCoder();
                return this._RtfCoder;
            }
        }
        private RtfCoder _RtfCoder = null;
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            RtfCoder rtfCoder = this._RtfCoder;
            if (rtfCoder != null)
            {
                ((IDisposable)rtfCoder).Dispose();
                this._RtfCoder = null;
            }
        }
        /// <summary>
        /// true potlačí event _RtfEditor_TextChanged a tedy i EditedTextChanged.
        /// </summary>
        private bool _SuppressRtfEditorTextChanged;
        /// <summary>
        /// true potlačí externí event EditedTextChanged, ale ne interní _RtfEditor_TextChanged
        /// </summary>
        private bool _SuppressEventEditedTextChanged;
        #endregion
        #region Opožděný SyntaxColoring, pokud je text příliš dlouhý
        /// <summary>
        /// Zajistí, že za 650 milisekund od teď se provede metoda this.RtfTextParseAndColor().
        /// Pokud mezitím bude opět volána tato metoda, odloží se její start na 650 milisekund od příštího volání zdejší metody.
        /// Metoda this.RtfTextParseAndColor() tedy bude volána až po 650ms klidu.
        /// </summary>
        private void _TimerToParsesStart()
        {
            if (_Timer == null)
            {
                _Timer = new Timer();
                _Timer.Tick += new EventHandler(_Timer_Tick);
            }
            _Timer.Stop();
            _Timer.Interval = 650;
            _Timer.Start();
        }
        /// <summary>
        /// Uplynul potřebný čas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _Timer_Tick(object sender, EventArgs e)
        {
            _Timer.Stop();

            bool suppressInternalEvent = this._SuppressRtfEditorTextChanged;
            bool suppressExternalEvent = this._SuppressEventEditedTextChanged;
            try
            {
                this._SuppressRtfEditorTextChanged = true;
                this._SuppressEventEditedTextChanged = true;
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(this.RtfTextParseAndColor));
                else
                    this.RtfTextParseAndColor();
            }
            finally
            {
                this._SuppressRtfEditorTextChanged = suppressInternalEvent;
                this._SuppressEventEditedTextChanged = suppressExternalEvent;
            }
        }
        private Timer _Timer;
        #endregion
    }
    #region class GuiRtfTextBox : RTF editor (RichTextBox)
    /// <summary>
    /// EditorTextBox : RTF editor (RichTextBox)
    /// </summary>
    public class GuiRtfTextBox : RichTextBox
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiRtfTextBox()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DetectUrls = false;
        }
        /// <summary>
        /// Překreslení obsahu, pokud není pozastaveno
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Po dobu, kdy je nastaveno this.SuspendPaint se pokusím pozastavit vykreslování, aby okno s textboxem příliš neblikalo:
            // mimochodem, sem to nikdy nechodí :-) 
            if (!SuspendPaint)
                base.OnPaint(e);
        }
        /// <summary>
        /// Obsluha WndProc
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // Za stavu Suspend si střádám všechny message kvůli debugu:
            if (this.SuspendPaint)
                this.WmMsg += m.Msg.ToString("X4") + "; ";

            // Po dobu, kdy je nastaveno this.SuspendPaint se pokusím pozastavit vykreslování, aby okno s textboxem příliš neblikalo:
            // RTF chodí jen tudy!
            if (!(this.SuspendPaint && (m.Msg == WM_PAINT)))
                base.WndProc(ref m);
        }
        /// <summary>
        /// Pozastavení kreslení
        /// </summary>
        public bool SuspendPaint { get; set; }
        /// <summary>
        /// Konstanta události GetText
        /// </summary>
        public const int WM_GETTEXT = 0x000D;
        /// <summary>
        /// Konstanta události Paint
        /// </summary>
        public const int WM_PAINT = 0x000F;
        internal string WmMsg = "";
        #region Řádkování, pozicování textu, řešení CrLf - Cr
        /// <summary>
        /// Metoda vrátí index znaku v RTF controlu pro zadaný index znaku TXT.
        /// Rozdíl je dán jedním znakem za každý zlom řádku: v TXT formátu je konec řádku reprezentován dvěma znaky (CR a LF), 
        /// kdežto v RTF controlu se tentýž znak nachází na indexu o 1 menším za každý předchozí řádek textu.
        /// Tato metoda tedy 
        /// </summary>
        /// <param name="txtCharIndex"></param>
        /// <returns></returns>
        public int GetRtfCharIndex(int txtCharIndex)
        {
            if (txtCharIndex <= 0) return txtCharIndex;

            int len = this.TextLength;
            int idx = (txtCharIndex < len ? txtCharIndex : len);
            string txt = this.Text.Substring(0, idx);                // V této části textu najdeme výsledek...

            txt = txt.Replace("\n", "\r\n");                         // RTF má v textu jen LF, ale my v TXT formátu máme CR+LF
            txt = txt.Substring(0, idx);                             // Toto je text v délce hledaného indexu
            int lines = txt.Count(c => c == '\r');                   // Tolik konců řádků je PŘED hledaným indexem

            return txtCharIndex - lines;
        }
        #endregion
    }
    #endregion
    #region class GuiTreeView : zobrazovač struktury segmentů a hodnot
    /// <summary>
    /// GuiTreeView : zobrazovač struktury segmentů a hodnot
    /// </summary>
    public class GuiTreeView : System.Windows.Forms.TreeView
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public GuiTreeView()
        {
            this._WmIgnoreInit();
            this.AfterSelect += _AfterSelect;
        }
        #endregion
        #region Public property, vztah na data (editor nebo segmenty)
        /// <summary>
        /// RTF Editor, v jehož rámci zobrazujeme nebo i editujeme text, a jeho segmenty zde zobrazujeme
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GuiRtfEditor ParserEditor
        {
            get { return this._ParserEditor; }
            set
            {
                if (this._ParserEditor != null)
                    this._LinkEditor(this._ParserEditor, false);
                this._ParserEditor = value;
                if (this._ParserEditor != null)
                    this._LinkEditor(this._ParserEditor, true);
            }
        }
        private GuiRtfEditor _ParserEditor;
        /// <summary>
        /// Segmenty, které zde ve stromu zobrazujeme
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<ParserSegment> EditorSegments
        {
            get { return this._EditorSegments; }
            set { this._EditorSegments = value; this._TreeFill(); }
        }
        private IEnumerable<ParserSegment> _EditorSegments;
        /// <summary>
        /// Zaháčkuje/Odháčkuje this handler _EditorSegmentsChangedAfter do eventu editor.EditorSegmentsChangedAfter.
        /// Převezme segmenty které jsou aktuálně platné v editoru (nebo své segmenty nuluje).
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="link"></param>
        private void _LinkEditor(GuiRtfEditor editor, bool link)
        {
            if (editor == null) return;

            if (link)
            {
                editor.EditorSegmentsChangedAfter += _EditorSegmentsChangedAfter;
                this.EditorSegments = editor.EditorSegments;
            }
            else
            {
                editor.EditorSegmentsChangedAfter -= _EditorSegmentsChangedAfter;
                this.EditorSegments = null;
            }
        }
        private void _EditorSegmentsChangedAfter(object sender, EventArgs e)
        {
            this.EditorSegments = (this._ParserEditor == null ? null : this._ParserEditor.EditorSegments);
        }
        /// <summary>
        /// Zpráva která má být zobrazena ve stavovém řádku.
        /// Je možno využít event StatusMessageChanged
        /// </summary>
        public string StatusMessage { get { return this._StatusMessage; } protected set { this._StatusMessage = value; this.OnStatusMessageChanged(); } } private string _StatusMessage;
        /// <summary>
        /// Zobrazovat položky typu Komentář.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Zobrazovat položky typu Komentář.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowCommentItems { get; set; }
        /// <summary>
        /// Zobrazovat položky typu Blank.
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Zobrazovat položky typu Blank.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowBlankItems { get; set; }
        /// <summary>
        /// Znovu načte obsah stromu, akceptujíc nastavení ShowCommentItems a ShowBlankItems
        /// </summary>
        public void ReloadSegments()
        {
            this._TreeFill();
        }
        /// <summary>
        /// Čas posledního zpracování dat
        /// </summary>
        [Browsable(true)]
        [Category(GuiConst.CATEGORY_ASOL)]
        [Description("Čas posledního zpracování dat.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal? LastWorkingTime { get; private set; }
        /// <summary>
        /// Událost po změně textu do stavového řádku
        /// </summary>
        public event EventHandler StatusMessageChanged;
        /// <summary>
        /// Vyvolá event <see cref="StatusMessageChanged"/>
        /// </summary>
        protected virtual void OnStatusMessageChanged()
        {
            if (this.StatusMessageChanged != null)
                this.StatusMessageChanged(this, EventArgs.Empty);
        }
        #endregion
        #region Naplnění stromu this daty segmentů
        private void _TreeFill()
        {
            this.StatusMessage = "Loading parsed segments into Tree...";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            TreeNode node = new TreeNode();
            try
            {
                this.SuspendPaint = true;
                this._WmList = ((System.Diagnostics.Debugger.IsAttached) ? new List<WM>() : null);
                this.Visible = false;

                this.Nodes.Clear();
                this.ShowNodeToolTips = true;
                sw.Start();
                if (this._EditorSegments != null)
                {
                    foreach (ParserSegment segment in this._EditorSegments)
                        this._TreeFillOne(node.Nodes, segment);
                }
            }
            finally
            {
                TreeNode[] nodes = new TreeNode[node.Nodes.Count];
                node.Nodes.CopyTo(nodes, 0);
                if (nodes.Length == 1 && !nodes[0].IsExpanded)
                    nodes[0].Expand();

                this.Nodes.AddRange(nodes);
                sw.Stop();

                if (this._WmList != null && this._WmList.Count > 0)
                { }
                this.SuspendPaint = false;
                this.Visible = true;
                this.Invalidate();
                this.Refresh();
            }

            this.LastWorkingTime = (decimal)sw.ElapsedTicks / (decimal)System.Diagnostics.Stopwatch.Frequency;
            this.StatusMessage = "Loading segments done.";
        }
        private void _TreeFillOne(TreeNodeCollection collection, ParserSegment segment)
        {
            TreeNode node, child;
            node = new TreeNode(segment.SimplifiedText);
            node.ToolTipText = "Segment = head node";
            node.Tag = segment;

            bool addBlank = this.ShowBlankItems;
            bool addComment = this.ShowCommentItems;

            // collection.Add(node);
            foreach (ParserSegmentValue item in segment.Values)
            {
                switch (item.ValueType)
                {
                    case ParserSegmentValueType.Blank:
                    case ParserSegmentValueType.Delimiter:
                    case ParserSegmentValueType.Text:
                        if (item.ValueType == ParserSegmentValueType.Blank && !addBlank) break;

                        child = new TreeNode(item.Text);
                        
                        string tip = "Value";
                        if (!String.IsNullOrEmpty(item.ValueName)) tip += ", name: " + item.ValueName;
                        tip += ", type: " + item.ValueType.ToString();
                        child.ToolTipText = tip;
                    
                        child.Tag = item;
                        collection.Add(child);
                        break;
                    case ParserSegmentValueType.InnerSegment:
                        if (item.InnerSegment.IsComment && !addComment) break;

                        child = new TreeNode(item.Text);
                        child.ToolTipText = "InnerSegment, name: " + item.InnerSegment.SegmentName + ", item count: " + item.InnerSegment.ValueCount.ToString();
                        child.Tag = item;
                        collection.Add(child);
                        _TreeFillOne(child.Nodes, item.InnerSegment);
                        break;
                }
            }
        }
        #endregion
        #region Po aktivaci nodu aktivujeme odpovídající text v editoru
        void _AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this._ParserEditor == null) return;

            if (e.Node != null && e.Node.Tag != null)
                this._AfterSelectNode(e.Node);
        }
        private void _AfterSelectNode(TreeNode treeNode)
        {
            ParserSegmentValue item = treeNode.Tag as ParserSegmentValue;
            if (item == null) return;

            int begin = item.BeginAt;
            int end = item.EndBefore;
            this._ParserEditor.SelectRange(begin, end - begin, true);
        }
        #endregion
        #region Dočasné potlačení překreslování objektu
        private void _WmIgnoreInit()
        {
            // Následující WmMsg budeme ignorovat v režimu SuspendPaint:
            this._WmIgnore = new Hashtable();
            // this._WmIgnore.Add(0x000B, null);
            // this._WmIgnore.Add(0x000F, null);
            // this._WmIgnore.Add(0x0014, null);
            // this._WmIgnore.Add(0x1101, null);
            // this._WmIgnore.Add(0x110F, null);
            //   Tato msg (0x1132) se v době TreeFill vyskytuje, ale nesmí se potlačit - protože jinak se Tree vůbec nesestaví. this._WmIgnore.Add(0x1132, null);
            // this._WmIgnore.Add(0x113E, null);
            // this._WmIgnore.Add(0x113F, null);
            // this._WmIgnore.Add(0x204E, null);
        }
        /// <summary>
        /// Obsluha kreslení
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Po dobu, kdy je nastaveno this.SuspendPaint se pokusím pozastavit vykreslování, aby okno s textboxem příliš neblikalo:
            // mimochodem, sem to nikdy nechodí :-) 
            if (!SuspendPaint)
                base.OnPaint(e);
            else
            { }
        }
        /// <summary>
        /// Obsluha WndProc
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            if (!this.SuspendPaint)
            {
                base.WndProc(ref m);
            }
            else
            {
                if (!this._WmIgnore.ContainsKey(m.Msg))
                {
                    // Za stavu DebuggerAttached and Suspend si střádám všechny message které provedu - to kvůli debugu:
                    if (this._WmList != null)
                    {
                        try
                        {
                            WM wm = (WM)m.Msg;
                            this._WmList.Add(wm);
                        }
                        catch
                        { }
                    }

                    base.WndProc(ref m);
                }
            }
        }
        /// <summary>
        /// Je pozastaveno kreslení
        /// </summary>
        public bool SuspendPaint { get; set; }
        /// <summary>
        /// Ignorované zprávy WndProc
        /// </summary>
        private Hashtable _WmIgnore;
        /// <summary>
        /// Souhrn událostí WM
        /// </summary>
        private List<WM> _WmList = null;
        #endregion
    }
    #endregion
    #region GuiConst
    internal class GuiConst
    {
        public const string CATEGORY_ASOL = "Asseco Solutions";
    }
    #endregion
}
