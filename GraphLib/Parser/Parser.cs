using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;

namespace Djs.Common.TextParser
{
    #region class Parser : parser textu na segmenty (text, závorky, stringy, komentáře, atd) ale nikoliv významový lexer
    /// <summary>
    /// StringParser : parser textu na segmenty (text, závorky, stringy, komentáře, atd).
    /// Nejde o významový lexer (neprovádí významovou analýzu nalezených textů).
    /// </summary>
    public class Parser : IDisposable
    {
        #region Konstrukce a proměnné
        /// <summary>
        /// Vytvoří pracovní instanci a vloží do ní settings + provede jeho analýzu.
        /// Pokud je settings nevyhovující, dojde k chybě.
        /// </summary>
        /// <param name="setting"></param>
        public Parser(ParserSetting setting)
        {
            this._Setting = setting;
            this._SegmentSettings = new Dictionary<string, SegmentSettingAnalysed>();
            this._CurrentSettingStack = new Stack<SegmentSettingAnalysed>();
            this._AnalyseSetting();
        }
        private ParserSetting _Setting;
        private Dictionary<string, SegmentSettingAnalysed> _SegmentSettings;
        /// <summary>
        /// Aktuálně zpracovávaný segment.
        /// Je to ten, který se nachází na vrcholu zásobníku _CurrentSettingStack.
        /// Pokud by byl zásobník prázdný, vrátí referenci na this._InitialSegmentSetting.
        /// </summary>
        private SegmentSettingAnalysed _CurrentSetting
        {
            get
            {
                if (this._CurrentSettingStack.Count == 0)
                    return this._InitialSegmentSetting;
                return this._CurrentSettingStack.Peek();
            }
        }
        /// <summary>
        /// Celá hloubka aktuálně zpracovávaných segmentů
        /// </summary>
        private Stack<SegmentSettingAnalysed> _CurrentSettingStack;
        /// <summary>
        /// Výchozí segment, jeho deklarace
        /// </summary>
        private SegmentSettingAnalysed _InitialSegmentSetting;
        void IDisposable.Dispose()
        {
            this._SegmentSettings = null;
            this._Setting = null;
        }
        #endregion
        #region Analýza Settings (volá se z privátního konstruktoru třídy StringParser)
        /// <summary>
        /// Provede analýzu aktuálního Settings.
        /// Volá se z privátního konstruktoru třídy StringParser.
        /// </summary>
        private void _AnalyseSetting()
        {
            this._SegmentSettings.Clear();
            this._CurrentSettingStack.Clear();

            // Do dictionary nasypu vstupující segmentSettingy:
            foreach (ParserSegmentSetting segmentSetting in this._Setting.Segments)
            {
                string segmentName = segmentSetting.SegmentName;
                if (this._SegmentSettings.ContainsKey(segmentName))
                    throw new ParserSettingException("Segment with name \"" + segmentName + "\" is duplicite.", segmentName);
                this._SegmentSettings.Add(segmentName, new SegmentSettingAnalysed(segmentSetting));
            }

            // Nyní do každého analyzovaného settingu vložím jeho inner segmenty:
            foreach (SegmentSettingAnalysed settingAnalysed in this._SegmentSettings.Values)
            {
                if (settingAnalysed.ContainInnerSegments)
                {
                    for (int i = 0; i < settingAnalysed.SegmentSetting.InnerSegmentsNames.Length; i++)
                    {
                        string innerName = settingAnalysed.SegmentSetting.InnerSegmentsNames[i];
                        SegmentSettingAnalysed innerSegm;
                        if (this._SegmentSettings.TryGetValue(innerName, out innerSegm))
                            settingAnalysed.InnerSegments[i] = innerSegm;
                        else
                            throw new ParserSettingException("Segment \"" + settingAnalysed.SegmentName + "\" specifies the inner segment named \"" + innerName + "\", but segment with this name does not exist.", innerName);
                    }
                }
            }

            // Nastavím si výchozí i aktuální segment = podle jména uvedeného v InitialSegmentName:
            this._InitialSegmentSetting = this._SegmentSettings[this._Setting.InitialSegmentName];
            this._CurrentSettingStack.Push(this._InitialSegmentSetting);
        }
        #endregion
        #region Řízení parsování textu
        /// <summary>
        /// Metoda provede parsování textu na jednotlivé segmenty a values.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static List<ParserSegment> ParseString(string text, ParserSetting setting)
        {
            using (Parser parser = new Parser(setting))
            {
                List<ParserSegment> result = parser.ParseString(text);
                return result;
            }
        }
        /// <summary>
        /// Výkonný kód parsování, instanční prostředí s analyzovaným settingem.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public List<ParserSegment> ParseString(string text)
        {
            return this._ParseString(text);
        }
        /// <summary>
        /// Fyzické 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private List<ParserSegment> _ParseString(string input)
        {
            List<ParserSegment> result = new List<ParserSegment>();

            ParserSegment segment = new ParserSegment(null, this._InitialSegmentSetting.SegmentSetting);
            result.Add(segment);

            char[] codeChar = input.ToCharArray();
            int pointer = 0;
            int length = codeChar.Length;
            while (true)
            {
                SegmentSettingAnalysed currentSetting = this._CurrentSetting;

                int pointerBegin = pointer;
                SegmentSettingAnalysed nextSetting;
                string text;
                SegmentCurrentType change = currentSetting.DetectChange(codeChar, ref pointer, out text, out nextSetting);

                // Nějaká forma konce:
                if (change == SegmentCurrentType.None)
                    break;

                // Pokud není změna segmentu (počátek, konec) a přitom není změna pozice, jde o chybu algoritmu:
                bool changeSegment = (change == SegmentCurrentType.BeginNewSegment || change == SegmentCurrentType.EndCurrentSegment || change == SegmentCurrentType.StopCurrentSegment);
                if (!changeSegment && pointer == pointerBegin)
                    throw new ParserProcessException("Method DetectChange() could not handle the character '" + codeChar[pointer].ToString() + "' at position [" + pointer.ToString() + "] in segment " + segment.SegmentName, pointer, codeChar[pointer].ToString(), segment.SegmentName);

                // Nějaká forma pokroku:
                bool popSegment = false;
                int oneLen = pointer - pointerBegin;
                switch (change)
                {
                    case SegmentCurrentType.BeginNewSegment:
                        ParserSegment nextSegment = segment.AddInnerSegment(pointerBegin, v => new ParserSegment(v, nextSetting.SegmentSetting));
                        nextSegment.BeginAt = pointerBegin;
                        nextSegment.Begin = text;
                        segment = nextSegment;
                        this._CurrentSettingStack.Push(nextSetting);
                        break;

                    case SegmentCurrentType.Text:
                        if (currentSetting.EnableMergeDotSpaceText && segment.ValueCount > 1 && segment.ValueList[segment.ValueCount - 1].ValueType == ParserSegmentValueType.Blank)
                            this._DetectAndMergeDotSpaceText(segment, text);
                        segment.AddText(pointerBegin, text, oneLen);
                        break;

                    case SegmentCurrentType.TextEnd:
                        if (currentSetting.EnableMergeDotSpaceText && segment.ValueCount > 1 && segment.ValueList[segment.ValueCount - 1].ValueType == ParserSegmentValueType.Blank)
                            this._DetectAndMergeDotSpaceText(segment, text);
                        segment.AddText(pointerBegin, text, oneLen);
                        segment.End = "";
                        segment.EndBefore = pointer;
                        segment = segment.ParentSegment;
                        popSegment = true;
                        break;

                    case SegmentCurrentType.Blank:
                        segment.AddBlank(pointerBegin, text, oneLen);
                        break;

                    case SegmentCurrentType.Delimiter:
                        segment.AddDelimiter(pointerBegin, text, oneLen);
                        break;

                    case SegmentCurrentType.EndCurrentSegment:
                        // End: nalezený text představuje znak[y] ukončující segment = dostane se do segment.End, a pointer bude ukazovat na znak za posledním znakem textu:
                        segment.End = text;
                        segment.EndBefore = pointer;
                        segment = segment.ParentSegment;
                        popSegment = true;
                        break;

                    case SegmentCurrentType.StopCurrentSegment:
                        // Stop: nalezený text představuje znak[y] náležející do dalšího textu nebo segmentu, a v dalším kroku proběhne jejich analýza.
                        // Nalezený text nepatří do segment.End, a pointer bude ukazovat na výchozí znak, jen se ukončí aktuální segment a vrátíme se do segment.ParentSegment:
                        pointer = pointerBegin;
                        segment.End = "";
                        segment.EndBefore = pointer;
                        segment = segment.ParentSegment;
                        popSegment = true;
                        break;

                    case SegmentCurrentType.Illegal:
                        throw new ParserProcessException("Illegal text \"" + text + "\" at position [" + pointerBegin.ToString() + "], in segment " + segment.SegmentName + ".", pointerBegin, text, segment.SegmentName);

                }
                if (popSegment)
                {
                    if (segment == null)
                    {
                        segment = new ParserSegment(null, this._InitialSegmentSetting.SegmentSetting);
                        result.Add(segment);
                    }
                    if (this._CurrentSettingStack.Count > 0)
                        this._CurrentSettingStack.Pop();
                }
            }

            // Provedu detekci klíčových slov:
            this.DetectKeywords(result);

            return result;
        }
        /// <summary>
        /// Umožní spojovat text oddělený tečkou a Blank prvky do jednoho předchozího textu.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="text"></param>
        private void _DetectAndMergeDotSpaceText(ParserSegment segment, string text)
        {
            // Sem se dostávám tehdy, když mám Text (právě začíná), a před ním není text.
            // Zjistím, zda poslední prvek je typu Blank. Pokud ne, Merge neproběhne.
            int valueCount = segment.ValueCount;
            if (valueCount < 2) return;                                        // Málo položek: Merge nelze provést.
            ParserSegmentValue value = segment.ValueList[valueCount - 1];      // Hodnota poslední
            if (value.ValueType != ParserSegmentValueType.Blank) return;       // Musí být Blank, jinak Merge neprovedeme.
            value = segment.ValueList[valueCount - 2];                         // Hodnota před Blank
            if (value.ValueType != ParserSegmentValueType.Text) return;        // Musí být Text, jinak Merge neprovedeme.
            string textPrev = value.Content;                                   // Vyhodnotíme texty Prev a nový.
            if (!textPrev.EndsWith(".") && text != ".") return;                // Pokud nový text nezačíná tečkou a minulý tečkou nekončí, nebude Merge.

            // Delete Blank samo zajistí přidávání nového textu do předešlé hodnoty:
            segment.ValueList.RemoveAt(valueCount - 1);
        }
        #endregion
        #region Detekce Keywords
        /// <summary>
        /// Provede detekci Keywords v dodaných segmentech
        /// </summary>
        protected void DetectKeywords(IEnumerable<ParserSegment> segments)
        {
            foreach (ParserSegment segment in segments)
                this.DetectKeywords(segment);
        }
        /// <summary>
        /// Privátní metoda pro označování klíčových slov, interně je rekurzivní.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="keywords"></param>
        protected void DetectKeywords(ParserSegment segment)
        {
            SegmentSettingAnalysed setting = this._SegmentSettings[segment.SegmentName];
            foreach (ParserSegmentValue value in segment.Values)
            {
                if (value.ValueType == ParserSegmentValueType.Text)
                {
                    bool isKeyword = setting.IsKeyword(value.Content);
                    value.ValueName = (isKeyword ? VALUENAME_KEYWORD : "");
                }

                // Čistá rekurze:
                if (value.HasInnerSegment)
                    this.DetectKeywords(value.InnerSegment);
            }
        }
        /// <summary>
        /// Hodnota, která se ukládá do ParserSegmentValue.ValueName pro texty, které jsou klíčovým slovem.
        /// </summary>
        public const string VALUENAME_KEYWORD = "Keyword";
        #endregion
        #region sub class SegmentSettingAnalysed : deklarace jednoho segmentu, rozšířená a obohacená o funkce pro analýzy
        /// <summary>
        /// SegmentSettingAnalysed : Třída obsahující analyzovaný vstupní setting,
        /// rozšiřující funkce a seznam povolených vnitřních segmentů (obsahuje přímo referenci na jejich analyzovaný setting)
        /// </summary>
        protected class SegmentSettingAnalysed
        {
            #region Konstrukce
            internal SegmentSettingAnalysed(ParserSegmentSetting segmentSetting)
            {
                this.SegmentSetting = segmentSetting;
                int length;

                length = segmentSetting.InnerSegmentsNames == null ? 0 : segmentSetting.InnerSegmentsNames.Length;
                this.InnerSegments = new SegmentSettingAnalysed[length];      // jen připravím prostor, ale obsah do tohoto pole se bude vkládat až v druhé fázi analýzy celého settingu.

                bool containAny;
                this.BeginWithChars = (segmentSetting.BeginWith == null ? new char[0] : segmentSetting.BeginWith.ToCharArray());
                this.HasBegin = (this.BeginWithChars.Length > 0);
                this.SpecialTextsChar = this.AnalyseSpecialTexts(segmentSetting.SpecialTexts, out containAny);
                this.HasSpecialTexts = containAny;
                this.PermittedsChar = this.AnalyseStringsToChars2(segmentSetting.Permitteds, out containAny);
                this.HasPermitteds = containAny;
                this.BlanksChar = this.AnalyseStringsToChars2(segmentSetting.Blanks, out containAny);
                this.HasBlanks = containAny;
                this.DelimitersChar = this.AnalyseStringsToChars2(segmentSetting.Delimiters, out containAny);
                this.HasDelimiters = containAny;
                this.ContentLength = segmentSetting.ContentLength;
                this.HasFixedContentLength = (segmentSetting.ContentLength.HasValue && segmentSetting.ContentLength.Value > 0);
                this.EndWithChar = this.AnalyseStringsToChars2(segmentSetting.EndWith, out containAny);
                this.HasEndWith = containAny;
                this.StopWithChar = this.AnalyseStringsToChars2(segmentSetting.StopWith, out containAny);
                this.HasStopWith = containAny;
                this.IllegalChar = this.AnalyseStringsToChars2(segmentSetting.Illegal, out containAny);
                this.HasIllegal = containAny;
                this.IsComment = segmentSetting.IsComment;
                this.Keywords = this.AnalyseKeywords(segmentSetting.Keywords, segmentSetting.KeywordsCaseSensitive, out containAny);
                this.HasKeywords = containAny;
            }
            /// <summary>
            /// Převede pole stringů na pole polí znaků (tzv. zubaté pole).
            /// Na vstupu může být null nebo pole stringů.
            /// Vytvoří se výstupní pole, jehož počet prvků odpovídá počtu prvků vstupujícího pole stringů.
            /// Do každého prvku výstupního pole se vloží další pole, které obsahuje korespondující vstupní string rozložený do pole char.
            /// </summary>
            /// <param name="strings">Pole stringů</param>
            /// <param name="containAny">out: Obsahuje pole nějaká data?</param>
            /// <returns></returns>
            private char[][] AnalyseStringsToChars2(string[] strings, out bool containAny)
            {
                int length = (strings == null ? 0 : strings.Length);
                char[][] result = new char[length][];
                for (int i = 0; i < length; i++)
                {
                    string input = strings[i];
                    if (input != null)
                        result[i] = input.ToCharArray();
                    else
                        result[i] = new char[0];
                }
                containAny = (length > 0);
                return result;
            }
            /// <summary>
            /// Převede pole ParserSegmentSpecialTexts na pole polí znaků (tzv. zubaté pole), 
            /// převádí texty z property ParserSegmentSpecialTexts.InputText
            /// Na vstupu může být null nebo pole.
            /// Vytvoří se výstupní pole, jehož počet prvků odpovídá počtu prvků vstupujícího pole stringů.
            /// Do každého prvku výstupního pole se vloží další pole, které obsahuje hodnotu korespondující 
            /// ParserSegmentSpecialTexts.InputText rozložené do pole char.
            /// </summary>
            /// <param name="specialTexts">Pole speciálních textů</param>
            /// <param name="containAny">out: Obsahuje pole nějaká data?</param>
            /// <returns></returns>
            private char[][] AnalyseSpecialTexts(ParserSegmentSpecialTexts[] specialTexts, out bool containAny)
            {
                int length = (specialTexts == null ? 0 : specialTexts.Length);
                char[][] result = new char[length][];
                for (int i = 0; i < length; i++)
                {
                    ParserSegmentSpecialTexts input = specialTexts[i];
                    if (input != null && input.InputText != null)
                        result[i] = input.InputText.ToCharArray();
                    else
                        result[i] = new char[0];
                }
                containAny = (length > 0);
                return result;
            }
            /// <summary>
            /// Vrátí dictionary vytvořenou z dodaných slov.
            /// Duplicitní slova, null a empty vynechává.
            /// V případě, že je zadáno caseSensitive = false převádí slova na Upper().
            /// </summary>
            /// <param name="keywords">Pole klíčových slov</param>
            /// <param name="caseSensitive">Klíčová slova jsou case-sensitive?</param>
            /// <param name="containAny">out: Obsahuje pole nějaká data?</param>
            /// <returns></returns>
            private Dictionary<string, object> AnalyseKeywords(IEnumerable<string> keywords, bool caseSensitive, out bool containAny)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                if (keywords != null)
                {
                    foreach (string keyword in keywords)
                    {
                        if (!String.IsNullOrEmpty(keyword))
                        {
                            string key = (caseSensitive ? keyword : keyword.ToUpper());
                            if (!result.ContainsKey(key))
                                result.Add(key, null);
                        }
                    }
                }
                containAny = (result.Count > 0);
                return result;
            }
            /// <summary>
            /// Zjistí, zda daný text odpovídá některému klíčovému slovu tohoto segmentu
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public bool IsKeyword(string text)
            {
                if (this.Keywords.Count == 0) return false;
                if (String.IsNullOrEmpty(text)) return false;
                string key = (this.KeywordsCaseSensitive ? text : text.ToUpper()).Trim();
                return this.Keywords.ContainsKey(key);
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.SegmentSetting.ToString();
            }
            #endregion
            #region Property
            /// <summary>
            /// Vstupní data setting = od zadavatele
            /// </summary>
            internal ParserSegmentSetting SegmentSetting { get; private set; }
            /// <summary>
            /// Pole vnořených definic segmentů (již analyzovné definice)
            /// </summary>
            internal SegmentSettingAnalysed[] InnerSegments { get; private set; }
            /// <summary>
            /// Příznak, zda tento segment má deklarovaný počátečení text (this.BeginWithChars)
            /// </summary>
            internal bool HasBegin { get; set; }
            /// <summary>
            /// Deklarace začátku tohoto segmentu
            /// </summary>
            internal char[] BeginWithChars { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v EndWithChar
            /// </summary>
            internal bool HasEndWith { get; set; }
            /// <summary>
            /// Deklarace možných zakončení tohoto segmentu, 
            /// tyto znaky konce jsou ukládány do konce segmentu a nevyhodnocují se jako následující znaky textu.
            /// </summary>
            internal char[][] EndWithChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v StopWithChar
            /// </summary>
            internal bool HasStopWith { get; set; }
            /// <summary>
            /// Deklarace znaků, které již nepatří do tohoto segmentu,
            /// tyto znaky se nevkládají do konce segmentu ale vyhodnocují se jako následující znaky textu.
            /// </summary>
            internal char[][] StopWithChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v IllegalChar
            /// </summary>
            internal bool HasIllegal { get; set; }
            /// <summary>
            /// <para>
            /// Texty, které - pokud budou nalezeny - budou vyhodnoceny jako nepřípustné v daném segmentu. 
            /// Při parsování takového textu dojde k chybě typu ParserProcessException, kde bude uveden segment, chybný text a jeho pozice ve vstupním textu.
            /// Typicky se zde uvádějí ukončovací závorky povolených vnitřních segmentů, které ale nebyly zahájeny.
            /// </para>
            /// <para>
            /// Příklad chybného textu: "...(aaa(bb])..", kde "]" je nepovolený text. Pokud by text byl: "...(aaa[bb])..", byl by v pořádku protože znak ] by ukončoval segment [bb].
            /// </para>
            /// </summary>
            internal char[][] IllegalChar { get; private set; }
            /// <summary>
            /// Tento segment je komentářem, nenese hodnotné informace
            /// </summary>
            public bool IsComment { get; set; }
            /// <summary>
            /// true pokud obsahuje něco v BlanksChar
            /// </summary>
            internal bool HasBlanks { get; set; }
            /// <summary>
            /// Deklarace možných textů Blank v tomto segmentu
            /// </summary>
            internal char[][] BlanksChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v DelimitersChar
            /// </summary>
            internal bool HasDelimiters { get; set; }
            /// <summary>
            /// Deklarace možných textů Delimiter v tomto segmentu
            /// </summary>
            internal char[][] DelimitersChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v PermittedsChar
            /// </summary>
            internal bool HasPermitteds { get; set; }
            /// <summary>
            /// Deklarace možných (přípustných) textů povolených znaků v tomto segmentu. Pokud je prázdné, jsou povoleny všechny znaky.
            /// </summary>
            internal char[][] PermittedsChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v SpecialTextsChar
            /// </summary>
            internal bool HasSpecialTexts { get; set; }
            /// <summary>
            /// Deklarace vstupních hodnot speciálních textů v tomto segmentu (this.SpecialTexts[].InputText rozdělené na zubaté pole znaků)
            /// </summary>
            internal char[][] SpecialTextsChar { get; private set; }
            /// <summary>
            /// true pokud obsahuje něco v Keywords
            /// </summary>
            internal bool HasKeywords { get; set; }
            /// <summary>
            /// Seznam klíčových slov.
            /// Prvky tohoto segmentu, které jsou typu Text, se porovnávají s těmito klíčovými slovy, 
            /// a odpovídající texty dostanou ValueName = Parser.KEYWORD.
            /// </summary>
            internal Dictionary<string, object> Keywords { get; private set; }
            /// <summary>
            /// true pokud tento segment má pevně danou délku (ContentLength má hodnotu větší než 0)
            /// </summary>
            public bool HasFixedContentLength { get; private set; }
            /// <summary>
            /// Explicitně daná délka segmentu = počet znaků.
            /// Standardně = null, délka je proměnná.
            /// V určitých případech lze nastavit na kladnou hodnotu, pak se do segmentu vezme jako obsah daný počet znaků ze vstupu - bez jakéhokoliv hledání konce (End nebo Stop, ty v tomto případě nemají význam).
            /// </summary>
            public Int32? ContentLength { get; private set; }
            /// <summary>
            /// Specifikuje, zda klíčová slova se mají vyhledávat case-sensitive.
            /// </summary>
            internal bool KeywordsCaseSensitive { get { return this.SegmentSetting.KeywordsCaseSensitive; } }
            /// <summary>
            /// Příznak, který povoluje spojování textů do jednoho prvku (Value) typu Text, pokud jde o následující kombinaci:
            /// {Text1} {Blank} {Text "."} {Blank} {Text2}
            /// Jinými slovy: při zahájení hodnoty Text2 se otestuje, zda předchozí hodnoty nejsou uvedené konfigurace
            /// (přičemž Blank jsou nepovinné, a teček může být více za sebou).
            /// Pokud ano, tak se předcházející Blank odeberou, texty "." se shrnou a vše se přidá k předchozímu Text1.
            /// Zjednodušeně: SQL jazyk dovoluje psát názvy objektů ve formě "lcs . tabulka . sloupec", což je následně nedetekovatelné, 
            /// protože texty jsou uloženy v oddělených Values.
            /// Tento EnableJoinDotSpaceText zajistí, že se tato specialita vyřeší již při parsování textu.
            /// </summary>
            internal bool EnableMergeDotSpaceText { get { return this.SegmentSetting.EnableMergeDotSpaceText; } }
            /// <summary>
            /// Deklarace speciálních textů, pole deklarací párů (přímý přístup do this.SegmentSetting.SpecialTexts)
            /// </summary>
            internal ParserSegmentSpecialTexts[] SpecialTexts { get { return this.SegmentSetting.SpecialTexts; } }
            /// <summary>
            /// Název segmentu, klíčové slovo
            /// </summary>
            public string SegmentName { get { return this.SegmentSetting.SegmentName; } }
            /// <summary>
            /// Seznam segmentů, které se mohou vyskytovat uvnitř tohoto elementu.
            /// Typicky v závorce může být string nebo další závorky.
            /// </summary>
            public string[] InnerSegmentsNames { get { return this.SegmentSetting.InnerSegmentsNames; } }
            /// <summary>
            /// true, pokud tento segment může obsahovat speciální bloky textu.
            /// </summary>
            public bool ContainSpecialTexts { get { return (this.SpecialTexts != null && this.SpecialTexts.Length > 0); } }
            /// <summary>
            /// true, pokud tento segment může obsahovat nějaké vnořené vnitřní segmenty. Typicky v závorce může být string nebo další závorky.
            /// </summary>
            public bool ContainInnerSegments { get { return (this.InnerSegmentsNames != null && this.InnerSegmentsNames.Length > 0); } }
            #endregion
            #region Detekce vstupního textu
            /// <summary>
            /// Určí, co to v textu je. Zda náš segment pokračuje, nebo končí, nebo začíná jiný segment.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text"></param>
            /// <param name="nextSetting"></param>
            /// <returns></returns>
            internal SegmentCurrentType DetectChange(char[] input, ref int pointer, out string text, out SegmentSettingAnalysed nextSetting)
            {
                nextSetting = null;
                text = null;

                // Pokud je vstup prázdný:
                if (input == null || pointer >= input.Length)
                    return SegmentCurrentType.None;

                // Zjistíme, zda vstupující text je/není nějaký náš speciální znak:
                if (this.IsSpecialText(input, ref pointer, out text))
                    return SegmentCurrentType.Text;

                // Zjistíme, zda tento segment má mít pevnou délku, a pokud ano tak ji načteme:
                if (this.IsFixedContentLength(input, ref pointer, out text))
                    return SegmentCurrentType.TextEnd;

                // Zjistíme, zda vstupující text je/není koncem aktuálního segmentu:
                if (this.IsEndOfThisSegment(input, ref pointer, out text))
                    return SegmentCurrentType.EndCurrentSegment;

                // Zjistíme, zda vstupující text je/není stop textem aktuálního segmentu:
                if (this.IsStopOfThisSegment(input, ref pointer, out text))
                    return SegmentCurrentType.StopCurrentSegment;

                // Zjistíme, zda vstupující text je/není začátkem některého z našich vnořených segmentů:
                if (this.IsBeginOfInnerSegment(input, ref pointer, out text, out nextSetting))
                    return SegmentCurrentType.BeginNewSegment;

                // Detekujeme Blank:
                if (this.IsBlank(input, ref pointer, out text))
                    return SegmentCurrentType.Blank;

                // Detekujeme Delimiter:
                if (this.IsDelimiter(input, ref pointer, out text))
                    return SegmentCurrentType.Delimiter;

                // Zjistíme, zda vstupující text je/není ilegální:
                if (this.IsIllegal(input, ref pointer, out text))
                    return SegmentCurrentType.Illegal;

                // Zjistíme, zda vstupující text je/není povolený z hlediska Permitteds:
                SegmentCurrentType permittedType;
                if (this.IsPermitted(input, ref pointer, out text, out permittedType))
                    return permittedType;

                // Aktuální text není nic zvláštního, je to běžný znak:
                text = input[pointer].ToString();
                pointer++;
                return SegmentCurrentType.Text;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá některému speciálnímu textu.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat Segment.Value.Content</param>
            /// <returns></returns>
            protected bool IsSpecialText(char[] input, ref int pointer, out string text)
            {
                int index;
                if (this.HasSpecialTexts && IsMatching(input, ref pointer, this.SpecialTextsChar, out text, out index))
                {	// Našli jsme odpovídající vstupní text, ale vracet budeme text výstupní (převod Input => Output)
                    text = this.SpecialTexts[index].OutputText;
                    return true;
                }
                else
                {
                    text = null;
                }
                return false;
            }
            /// <summary>
            /// Vyhodnocuje pole povolených textů PermittedsChar.
            /// Pokud je pole naplněné, pouze pak je tato metoda aktivní, a vrátí true aby volající metoda zareagovala.
            /// Pokud je pole PermittedsChar prázdné, pak neraguje a vrací false.
            /// V případě vyplněného pole PermittedsChar tedy prověří, zda vstupní text odpovídá některému prvku z PermittedsChar.
            /// Pokud ano, pak se tento znak akceptuje jako vstupující text, vkládá se do výstupu (jako text) a nastavuje se permittedType = Text, vrací se true.
            /// Pokud se znak neakceptuje, pak se text nechá NULL a do permittedType se vloží EndCurrentSegment, vrací se true.
            /// </summary>
            /// <param name="input"></param>
            /// <param name="pointer"></param>
            /// <param name="text"></param>
            /// <param name="permittedType"></param>
            /// <returns></returns>
            protected bool IsPermitted(char[] input, ref int pointer, out string text, out SegmentCurrentType permittedType)
            {
                text = null;
                permittedType = SegmentCurrentType.None;
                // Pokud tento segment NEobsahuje zadání permitted textů, pak nereagujeme:
                if (!this.HasPermitteds) return false;

                // V segmentu jsou zadané povolené znaky. Aktuální znak je povolený?
                if (IsMatching(input, ref pointer, this.PermittedsChar, out text))
                {   // Povolený znak => jde o součást textu v segmentu:
                    permittedType = SegmentCurrentType.Text;
                    return true;
                }
                // Nepovolený znak => pointer se neposunul, text obsahuje null, nastavíme typ prvku = Konec segmentu a vrátíme true, tím bude segment ukončen, a přitom aktuální znak (nepovolený) se zpracuje do dalšího segmentu:
                permittedType = SegmentCurrentType.EndCurrentSegment;
                return true;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální segment má mít pevnou délku.
            /// Pak ze vstupu odebere patřičný počet znaků do out text, posune pointer a vrátí true.
            /// Pokud nejde o segment s pevnou délkou, vrací false.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsFixedContentLength(char[] input, ref int pointer, out string text)
            {
                if (this.HasFixedContentLength)
                {
                    text = "";
                    int len = input.Length - pointer;                // Kolik znaků máme ještě na vstupu, které jsme nezpracovali?
                    int c = this.ContentLength.Value;                // Kolik znaků má mít tento segment
                    if (len < c) c = len;                            // Nemůže mít víc, než máme k dispozici...
                    for (int i = 0; i < c; i++)
                        text += input[pointer++].ToString();         // Sumace znaků do textu, posun pointeru. Pokud (c) je nula nebo záporné (protože nemáme znaky), neprovede se.
                    return true;
                }
                text = null;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá konci aktuálního segmentu.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsEndOfThisSegment(char[] input, ref int pointer, out string text)
            {
                if (this.HasEndWith)
                    return IsMatching(input, ref pointer, this.EndWithChar, out text);
                text = null;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá stop znakům aktuálního segmentu.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsStopOfThisSegment(char[] input, ref int pointer, out string text)
            {
                if (this.HasStopWith)
                    return IsMatching(input, ref pointer, this.StopWithChar, out text);
                text = null;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá začátku některého z našich vnitřních segmentů.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// Ukládá referenci na následující segment do out parametru.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do nového segmentu do Segment.Begin</param>
            /// <param name="nextSetting">Reference na objekt Setting, který tímto začíná (pokud je vráceno true)</param>
            /// <returns></returns>
            protected bool IsBeginOfInnerSegment(char[] input, ref int pointer, out string text, out SegmentSettingAnalysed nextSetting)
            {
                if (this.ContainInnerSegments)
                {
                    // Pozor: segmentů může být více, a mohou i podobně začínat.
                    //    My si z takových vybereme ten segment, jehož začátek je nejdelší.
                    // Příklad: máme C# komentář začínající //
                    // a přitom máme XML komentář začínající ///
                    // Pokud ve vstupujícím textu najdeme /// pak jde o XML komentář:
                    int maxIndex = -1;
                    int maxLength = 0;
                    for (int i = 0; i < this.InnerSegments.Length; i++)
                    {
                        SegmentSettingAnalysed settingAnalysed = this.InnerSegments[i];
                        if (settingAnalysed.HasBegin && StartWith(input, pointer, settingAnalysed.BeginWithChars))
                        {
                            int currLength = settingAnalysed.BeginWithChars.Length;
                            if (currLength > maxLength)
                            {
                                maxIndex = i;
                                maxLength = currLength;
                            }
                        }
                    }

                    // Pokud jsem našel nějaký segment, pak jej vrátíme:
                    if (maxIndex >= 0)
                    {
                        nextSetting = this.InnerSegments[maxIndex];
                        text = GetStringFromChars(nextSetting.BeginWithChars);
                        pointer += nextSetting.BeginWithChars.Length;
                        return true;
                    }
                }
                nextSetting = null;
                text = null;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá některému textu Blank.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsBlank(char[] input, ref int pointer, out string text)
            {
                if (this.HasBlanks)
                    return IsMatching(input, ref pointer, this.BlanksChar, out text);
                text = null;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá některému textu Delimiters.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsDelimiter(char[] input, ref int pointer, out string text)
            {
                return IsMatching(input, ref pointer, this.DelimitersChar, out text);
            }
            /// <summary>
            /// Vrátí true, pokud aktuální text odpovídá některému textu Illegal.
            /// Pokud ano, pak vrací true, nastavuje out text a posouvá pointer o délku vstupního textu, 
            /// takže ukazuje na první následující znak.
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="text">Výstup textu, který se má zapsat do Segment.End</param>
            /// <returns></returns>
            protected bool IsIllegal(char[] input, ref int pointer, out string text)
            {
                if (this.HasIllegal)
                    return IsMatching(input, ref pointer, this.IllegalChar, out text);
                text = null;
                return false;
            }
            /// <summary>
            /// Zjistí, zda text ve vstupním bufferu odpovídá některému ze vzorků uvedených v poli patterns.
            /// Pokud ano, vrátí true a nastaví odpovídající text do out parametru text, a posune pointer o odpovídající délku.
            /// Pokud ne, vrátí false a pointer neposouvá.
            /// Ze zadaných vzorků se pokouší najít ten nejdelší odpovídající.
            /// </summary>
            /// <param name="input">Vstupní buffer</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="patterns">Sada hledaných vzorků, uložených jako zubaté pole</param>
            /// <param name="text">Výstup nalezeního textu</param>
            /// <param name="index">Index, na kterém je nalezený vzorek</param>
            /// <returns></returns>
            internal static bool IsMatching(char[] input, ref int pointer, char[][] patterns, out string text)
            {
                int index;
                return IsMatching(input, ref pointer, patterns, out text, out index);
            }
            /// <summary>
            /// Zjistí, zda text ve vstupním bufferu odpovídá některému ze vzorků uvedených v poli patterns.
            /// Pokud ano, vrátí true a nastaví odpovídající text do out parametru text, a posune pointer o odpovídající délku.
            /// Pokud ne, vrátí false a pointer neposouvá.
            /// Ze zadaných vzorků se pokouší najít ten nejdelší odpovídající.
            /// </summary>
            /// <param name="input">Vstupní buffer</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="patterns">Sada hledaných vzorků, uložených jako zubaté pole</param>
            /// <param name="text">Výstup nalezeního textu</param>
            /// <param name="index">Index, na kterém je nalezený vzorek</param>
            /// <returns></returns>
            internal static bool IsMatching(char[] input, ref int pointer, char[][] patterns, out string text, out int index)
            {
                if (patterns != null)
                {
                    // Hledám nejdelší shodný vzorek:
                    int matchIndex = -1;
                    int maxLength = 0;
                    int length = patterns.Length;
                    for (int i = 0; i < length; i++)
                    {
                        char[] pattern = patterns[i];
                        if (StartWith(input, pointer, pattern))
                        {
                            if (pattern.Length > maxLength)
                            {
                                matchIndex = i;
                                maxLength = pattern.Length;
                            }
                        }
                    }
                    // Pokud jsme našli, nastavím out hodnoty a skončím s true:
                    if (matchIndex >= 0)
                    {
                        index = matchIndex;
                        char[] pattern = patterns[index];
                        pointer += pattern.Length;
                        text = GetStringFromChars(pattern);
                        return true;
                    }
                }
                // Nenašli anebo nebylo možno hledat:
                text = null;
                index = -1;
                return false;
            }
            /// <summary>
            /// Vrátí true, pokud vstupní buffer počínaje od dané pozice obsahuje text daný v patternu
            /// </summary>
            /// <param name="input">Vstupní text uložený v bufferu</param>
            /// <param name="pointer">Ukazatel do bufferu</param>
            /// <param name="pattern">Hledaný vzorek</param>
            /// <returns></returns>
            internal static bool StartWith(char[] input, int pointer, char[] pattern)
            {
                if (pattern == null) return false;
                int patternLength = pattern.Length;              // Délka hledaného textu
                int inputLength = input.Length - pointer;        // Délka vstupního textu
                if (patternLength > inputLength) return false;   // Na vstupu je menší text než hledáme.
                for (int i = 0; i < patternLength; i++)
                {
                    if (input[pointer + i] != pattern[i]) return false;
                }
                return true;
            }
            /// <summary>
            /// Vrátí text vytvořený z daného pole znaků
            /// </summary>
            /// <param name="pattern"></param>
            /// <returns></returns>
            internal static string GetStringFromChars(char[] pattern)
            {
                return String.Concat(pattern);
            }
            #endregion
        }
        #endregion
        #region enum SegmentCurrentType : druh textu v aktuální pozici bufferu, řídí zpracování textu do segmentů
        /// <summary>
        /// SegmentCurrentType : druh textu v aktuální pozici bufferu, řídí zpracování textu do segmentů.
        /// </summary>
        protected enum SegmentCurrentType
        {
            /// <summary>
            /// Není text, konec.
            /// </summary>
            None = 0,
            /// <summary>
            /// Mezera mezi texty.
            /// Stále jsme uvnitř aktuálního segmentu.
            /// </summary>
            Blank,
            /// <summary>
            /// Text.
            /// Stále jsme uvnitř aktuálního segmentu.
            /// </summary>
            Text,
            /// <summary>
            /// Text a konec segmentu (jde o segment s fixní délkou).
            /// Nalezený text je textem segmentu, End je prázdný, pointer ukazuje na následující znak za naším segmentem, nás segment končí, vracíme se (Pop) na jeho ParentSegment.
            /// </summary>
            TextEnd,
            /// <summary>
            /// Oddělovat textů.
            /// Stále jsme uvnitř aktuálního segmentu.
            /// </summary>
            Delimiter,
            /// <summary>
            /// Na aktuální pozici začíná nový vnořený segment.
            /// </summary>
            BeginNewSegment,
            /// <summary>
            /// Na aktuální pozici končí aktuální segment. 
            /// Vracíme se k segmentu, který je naším parentem.
            /// Segment končí některým ze znaků End = zakončující znaky se vkládají do End segmentu, a nezpracovávají se jako další text; na rozdíl od StopCurrentSegment.
            /// </summary>
            EndCurrentSegment,
            /// <summary>
            /// Na aktuální pozici končí aktuální segment a začínají další znaky dalšího segmentu/textu.
            /// Vracíme se k segmentu, který je naším parentem.
            /// Segment končí některým ze znaků Stop = zakončující znaky se nevkládají do End segmentu, ale zpracovávají se jako další text; na rozdíl od EndCurrentSegment.
            /// </summary>
            StopCurrentSegment,
            /// <summary>
            /// Ilegální text = výskyt zakázaného textu v tomto segmentu.
            /// </summary>
            Illegal
        }
        #endregion
        #region Statické služby Parseru pro SQL syntax
        /// <summary>
        /// Metoda v daném textu výrazu vyhledá proměnné obsahující tečku/tečky,
        /// a část před poslední tečkou (tuto část považujeme za název tabulky) odešle do funkce, která je předaná jako parametr aliasGenerator.
        /// Předaná funkce může reagovat na název tabulky, a vrátit jiný název = typicky alias.
        /// Zdejší metoda pak vrácený text vloží namísto původního názvu tabulky.
        /// Pokud funkce vrátí null, pak se nahrazovat nebude nic.
        /// Pokud funkce vrátí prázdný řetězec, pak se tabulka z proměné odstraní včetně tečky za tabulkou ("lcs.tabulka.sloupec" převede na "sloupec").
        /// </summary>
        /// <param name="expression">Výraz</param>
        /// <param name="aliasGenerator">Převáděč jmen tabulek na jejich aliasy. Odkaz na funkci.</param>
        /// <returns></returns>
        public static string ChangeSqlTableNameInExpression(string expression, Func<string, string> aliasGenerator)
        {
            List<ParserSegment> segments = ParseString(expression, ParserDefaultSetting.MsSql);
            StringBuilder sb = new StringBuilder();
            foreach (ParserSegment segment in segments)
            {
                ChangeSqlTableNameInSegment(segment, aliasGenerator);
                sb.Append(segment.Text);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Metoda v daném segmentu (rekurzivně do hloubky) vyhledá proměnné obsahující tečku/tečky, 
        /// a část před poslední tečkou (tuto část považujeme za název tabulky) odešle do funkce, která je předaná jako parametr aliasGenerator.
        /// Předaná funkce může reagovat na název tabulky, a vrátit jiný název = typicky alias.
        /// Zdejší metoda pak vrácený text vloží namísto původního názvu tabulky.
        /// Pokud funkce vrátí null, pak se nahrazovat nebude nic.
        /// Pokud funkce vrátí prázdný řetězec, pak se tabulka z proměné odstraní včetně tečky za tabulkou ("lcs.tabulka.sloupec" převede na "sloupec").
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="aliasGenerator">Převáděč jmen tabulek na jejich aliasy. Odkaz na funkci.</param>
        public static void ChangeSqlTableNameInSegment(ParserSegment segment, Func<string, string> aliasGenerator)
        {
            foreach (ParserSegmentValue value in segment.Values)
            {
                switch (value.ValueType)
                {
                    case ParserSegmentValueType.Text:
                        string text = value.Content;

                        string tableName;
                        string columnName;
                        if (SplitFullDbColumn(text, out tableName, out columnName) && tableName.Length > 0)
                        {
                            string tableAlias = aliasGenerator(tableName);
                            if (tableAlias != null)
                            {   // Pokud se vrátil alias not null:
                                if (tableAlias.Length == 0)
                                    // Pokud se vrátil prázdný string, pak výsledek bude původní text za poslední tečkou:
                                    text = columnName;
                                else
                                    // Jinak se vrácený text předřadí před tečku, tečka se zachová i text za ní:
                                    text = tableAlias + "." + columnName;
                                value.SetContent(text, text.Length);
                            }
                        }
                        break;
                    case ParserSegmentValueType.InnerSegment:
                        if (value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_PARENTHESIS)
                            ChangeSqlTableNameInSegment(value.InnerSegment, aliasGenerator);
                        break;
                }
            }
        }
        /// <summary>
        /// Rozdělí daný plný název sloupce (owner.table.column) na tabulku (owner.table) a sloupec (column), rozdělí v místě poslední tečky.
        /// Vrací true, pokud zadaná data (fullColumnName) není prázdné, a nezačíná číslicí. Pak provede rozdělení.
        /// Vrací false, když fullColumnName nemůže být sloupcem.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool SplitFullDbColumn(string text, out string tableName, out string columnName)
        {
            tableName = null;
            columnName = null;
            if (String.IsNullOrEmpty(text)) return false;

            tableName = "";
            columnName = "";
            string[] parts = text.Split('.');
            int cnt = parts.Length;
            int last = cnt - 1;
            for (int i = 0; i < cnt; i++)
            {
                string part = parts[i].Trim();
                if (i == last)
                    columnName = part;
                else
                    tableName += ((i == 0) ? "" : ".") + part;
            }
            return true;
        }
        #endregion
        #region Záměna textu ve výrazech
        /// <summary>
        /// Metoda v daném textu výrazu vyhledá jednotlivé prvky typu Text,
        /// a tyto texty odešle do funkce, která je předaná jako parametr textChanger.
        /// Daný text je rozebrán na segmenty pomocí daného settingu. Settingy lze najít mj. ve třídě ParserDefaultSetting.
        /// Předaná funkce (textChanger) může reagovat na daný text a vrátit jiný text = typicky aliasovaný sloupec.
        /// Předaná funkce dostává dva parametry: string = text (slovo, název tabulky, oddělovač, atd), a ParserSegmentValue = kompletní popis prvku (typ, ParentSegment = v čem se slovo vyskytuje, atd).
        /// Funkce má vrátit string, který se vloží namísto původního stringu, nebo může vrátit "" = text se odstraní, nebo vrátí NULL = nic se měnit nebude.
        /// Zdejší metoda pak vrácený text vloží namísto původního názvu textu.
        /// Pokud funkce vrátí null, pak se nahrazovat nebude nic.
        /// </summary>
        /// <param name="expression">Výraz</param>
        /// <param name="textChanger">Převáděč textů. Odkaz na funkci.</param>
        /// <returns></returns>
        public static string ChangeTextInExpression(string expression, ParserSetting setting, Func<string, ParserSegmentValue, string> textChanger)
        {
            List<ParserSegment> segments = ParseString(expression, setting);
            StringBuilder sb = new StringBuilder();
            foreach (ParserSegment segment in segments)
            {
                ChangeTextInSegment(segment, textChanger);
                sb.Append(segment.Text);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Metoda v daném segmentu (rekurzivně do hloubky) vyhledá jednotlivé prvky typu Text,
        /// a tyto texty odešle do funkce, která je předaná jako parametr textChanger.
        /// Předaná funkce (textChanger) může reagovat na daný text a vrátit jiný text = typicky aliasovaný sloupec.
        /// Předaná funkce dostává dva parametry: string = text (slovo, název tabulky, oddělovač, atd), a ParserSegmentValue = kompletní popis prvku (typ, ParentSegment = v čem se slovo vyskytuje, atd).
        /// Funkce má vrátit string, který se vloží namísto původního stringu, nebo může vrátit "" = text se odstraní, nebo vrátí NULL = nic se měnit nebude.
        /// Zdejší metoda pak vrácený text vloží namísto původního názvu textu.
        /// Pokud funkce vrátí null, pak se nahrazovat nebude nic.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="textChanger">Převáděč jmen tabulek na jejich aliasy. Odkaz na funkci.</param>
        public static void ChangeTextInSegment(ParserSegment segment, Func<string, ParserSegmentValue, string> textChanger)
        {
            foreach (ParserSegmentValue value in segment.Values)
            {
                switch (value.ValueType)
                {
                    case ParserSegmentValueType.Text:
                        string changedText = textChanger(value.Content, value);
                        if (changedText != null)
                            value.SetContent(changedText, changedText.Length);
                        break;
                    case ParserSegmentValueType.InnerSegment:
                        if (value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_PARENTHESIS)
                            ChangeTextInSegment(value.InnerSegment, textChanger);
                        break;
                }
            }
        }
        #endregion
    }
    #endregion
    #region class ParserSegment : Segment obsahující data (text a/nebo řadu dalších vnořených segmentů)
    /// <summary>
    /// ParserSegment : Segment obsahující data (text a/nebo řadu dalších vnořených segmentů)
    /// Je uvedeno jeho jméno a parent.
    /// Vše, co obsahuje, je uloženo v řadě hodnot Values.
    /// </summary>
    public class ParserSegment
    {
        #region Konstrukce a property
        /// <summary>
        /// Vytvoří nový segment
        /// </summary>
        /// <param name="parentValue">Parent value, v níž je tento segment hostován jako InnerSegment. Může být null.</param>
        /// <param name="segmentSetting">Setting segmentu, podle kterého byl tento segment detekován a vytvořen. Mimo jiné nese informace o jeho formátování.</param>
        public ParserSegment(ParserSegmentValue parentValue, ParserSegmentSetting segmentSetting)
        {
            this.ParentValue = parentValue;
            this.SegmentSetting = segmentSetting;
            this._Values = new List<ParserSegmentValue>();
        }
        /// <summary>
        /// Vytvoří nový segment
        /// </summary>
        /// <param name="parentValue">Parent value, v níž je tento segment hostován jako InnerSegment. Může být null.</param>
        /// <param name="likeSegment">Segment, který má shodný charakter jako segment právě vytvářený. 
        /// Nový segment si z něj převezme nastavení (Setting) a hodnoty Begin a End. 
        /// Nepřebírá ParentValue (tu dostává explicitně), nepřebírá pozice BeginAt a EndBefore, ani obsah Values.
        /// </param>
        public ParserSegment(ParserSegmentValue parentValue, ParserSegment likeSegment)
        {
            this.ParentValue = parentValue;
            this.SegmentSetting = likeSegment.SegmentSetting;
            this.Begin = likeSegment.Begin;
            this.End = likeSegment.End;
            this._Values = new List<ParserSegmentValue>();
        }
        /// <summary>
        /// Parent segment, v jehož některé Values je tento segment umístěn (konkrétní Value, v níž je tento segment hostován, je uložena v this.ParentValue).
        /// Typicky při vnořování závorek: vnitřní závorka zde má odkaz na vnější závorku.
        /// String může mít odkaz na závorku v níž se nachází nebo na vnější kód (code).
        /// </summary>
        public ParserSegment ParentSegment { get { return (this.ParentValue == null ? null : this.ParentValue.Parent); } }
        /// <summary>
        /// Parent value, níž je tento segment hostován jako InnerSegment.
        /// Může být null, když this segment je vrcholový.
        /// </summary>
        public ParserSegmentValue ParentValue { get; private set; }
        /// <summary>
        /// Root segment = nejvyšší Parent segment, v němž je tato hodnota umístěna.
        /// Pokud this segment není nikde hostován, pak on sám je Root.
        /// Je nastaveno vždy.
        /// </summary>
        public ParserSegment Root 
        {
            get 
            {
                // Jistěže umím napsat jednoduchý výraz:
                //    return (this.ParentValue != null ? this.ParentValue.Root : this);
                // Ale takový výraz je nebezpečný z hlediska:
                //   a) Zacyklení
                //   b) Délky řetězu, který se vyhodnocuje pravou rekurzí = opakované volání metody this.ParentValue.get_Root(), která teoreticky může zahltit zásobník procesu.
                // Proto to napíšu ne tak elegantně, ale rozhodně spolehlivě:
                ParserSegment root = this;
                for (int timeOut = 0; timeOut < 2000; timeOut++)
                {
                    if (root.ParentValue == null) return root;
                    root = root.ParentValue.Parent;
                }
                return root;
            }
        }
        /// <summary>
        /// Nastavení tohoto segmentu. Mimo jiné nese informace o jeho formátování.
        /// </summary>
        protected ParserSegmentSetting SegmentSetting { get; private set; }
        /// <summary>
        /// Název segmentu - vychází z jeho definice
        /// </summary>
        public string SegmentName { get { return this.SegmentSetting.SegmentName; } }
        /// <summary>
        /// Tento segment je komentářem, nenese hodnotné informace - vychází z jeho definice
        /// </summary>
        public bool IsComment { get { return this.SegmentSetting.IsComment; } }
        /// <summary>
        /// Text, kterým segment začíná
        /// </summary>
        public string Begin { get; set; }
        /// <summary>
        /// Příznak, že tento segment má neprázdný this.Begin
        /// </summary>
        public bool HasBegin { get { return (this.Begin != null && this.Begin.Length > 0); } }
        /// <summary>
        /// Pozice ve vstupním textu (bufferu), na které segment začíná
        /// </summary>
        public int BeginAt { get; set; }
        /// <summary>
        /// Obsah segmentu (texty, segmenty)
        /// </summary>
        public IEnumerable<ParserSegmentValue> Values { get { return this._Values; } }
        /// <summary>
        /// Počet prvků Values v poli ValueList
        /// </summary>
        public int ValueCount { get { return this._Values.Count; } }
        /// <summary>
        /// Obsah segmentu (texty, segmenty), vrácený přímo jako List - nehrajte si s ním víc než je nutné !!!
        /// </summary>
        public List<ParserSegmentValue> ValueList { get { return this._Values; } }
        private List<ParserSegmentValue> _Values;
        /// <summary>
        /// Text, kterým segment končí
        /// </summary>
        public string End { get; set; }
        /// <summary>
        /// Příznak, že tento segment má neprázdný this.End
        /// </summary>
        public bool HasEnd { get { return (this.End != null && this.End.Length > 0); } }
        /// <summary>
        /// Pozice ve vstupním textu (bufferu), před kterou segment končí.
        /// Rozdíl (EndBefore - BeginAt) = počet znaků tohoto segmentu včetně Inner.
        /// Na pozici EndBefore je první znak dalšího segmentu.
        /// </summary>
        public int EndBefore { get; set; }
        /// <summary>
        /// Plný text segmentu, včetně úvodního a koncového textu (uvozovky, závorky) a včetně vnořených hodnot (rozvinutých do textu).
        /// I vnitřní vnořené hodnoty mají uvedeny i svoje úvodní a koncové texty.
        /// Příklad: text v závorce: (abc * def * (x + y)) je tedy vrácen shodně: (abc * def * (x + y))
        /// Text v oddělovačích «{proměnná}» je vrácen včetně oddělovačů: «{proměnná}»
        /// Na rozdíl od property InnerText, která vrací text bez vnějších oddělovačů.
        /// </summary>
        public string Text
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (this.Begin != null) sb.Append(this.Begin);
                foreach (ParserSegmentValue value in this.Values)
                    sb.Append(value.ToString());
                if (this.End != null) sb.Append(this.End);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Text segmentu, bez úvodního a koncového textu (uvozovky, závorky), ale obsahuje vnořené vnitřní hodnoty.
        /// Pozor: vnitřní vnořené hodnoty ale mají uvedeny i svoje úvodní a koncové texty.
        /// Příklad: text v závorce: (abc * def * (x + y)) je tedy vrácen bez vnějších závorek, ale s vnitřními závorkami: abc * def * (x + y)
        /// Text v oddělovačích «{proměnná}» je vrácen bez oddělovačů: proměnná
        /// Na rozdíl od property Text, která vrací text včetně vnějších oddělovačů.
        /// </summary>
        public string InnerText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (ParserSegmentValue value in this.Values)
                    sb.Append(value.ToString());
                return sb.ToString();
            }
        }
        /// <summary>
        /// Text segmentu, bez úvodního a koncového textu (uvozovky, závorky), 
        /// a navíc namísto vnitřních segmentů jsou tři tečky (ohraničené originálním oddělovačem).
        /// </summary>
        public string SimpleText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (ParserSegmentValue value in this.Values)
                    sb.Append(value.SimpleText);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Zjednodušený text: obsahuje úvodní oddělovač (Begin), tři tečky a koncový oddělovač.
        /// </summary>
        public string SimplifiedText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (this.Begin != null) sb.Append(this.Begin);
                sb.Append("...");
                if (this.End != null) sb.Append(this.End);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        #endregion
        #region Přidávání dat
        /// <summary>
        /// Zajistí přidání textu do tohoto segmentu.
        /// Pokud na poslední pozici v řadě this.Values je objekt typu Text, předá daný text do něj na konec.
        /// Pokud je na poslední pozici nějaký jiný typ, založí nový objekt do this.Values s typem Text, a text vloží do něj.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(int pointerBegin, string text, int length)
        {
            if (this._Values.Count == 0 || this._Values[this._Values.Count - 1].ValueType != ParserSegmentValueType.Text)
                this._Values.Add(new ParserSegmentValue(this, pointerBegin, ParserSegmentValueType.Text));
            this._Values[this._Values.Count - 1].AddContent(text, length);
        }
        /// <summary>
        /// Zajistí přidání prázdného textu do tohoto segmentu.
        /// Pokud na poslední pozici v řadě this.Values je objekt typu Blank, předá daný text do něj na konec.
        /// Pokud je na poslední pozici nějaký jiný typ, založí nový objekt do this.Values s typem Blank, a text vloží do něj.
        /// </summary>
        /// <param name="text"></param>
        public void AddBlank(int pointerBegin, string text, int length)
        {
            if (this._Values.Count == 0 || this._Values[this._Values.Count - 1].ValueType != ParserSegmentValueType.Blank)
                this._Values.Add(new ParserSegmentValue(this, pointerBegin, ParserSegmentValueType.Blank));
            this._Values[this._Values.Count - 1].AddContent(text, length);
        }
        /// <summary>
        /// Do tohoto segmentu přidá nový objekt typu Delimiter a předaný text do něj uloží.
        /// Obsah delimiteru se ukládá vždy samostatně, nikdy se nesloučí dva delimitery do jednoho objektu.
        /// </summary>
        /// <param name="text"></param>
        public void AddDelimiter(int pointerBegin, string text, int length)
        {
            this._Values.Add(new ParserSegmentValue(this, pointerBegin, ParserSegmentValueType.Delimiter, text, length));
        }
        /// <summary>
        /// Do tohoto segmentu přidá dodaný objekt Value.
        /// </summary>
        /// <param name="value"></param>
        public void AddValue(ParserSegmentValue value)
        {
            value.Parent = this;             // Změním parenta. Value může být doma jen v jednom segmentu.
            this._Values.Add(value);
        }
        /// <summary>
        /// Do tohoto segmentu přidá řadu dodaných objektů Value.
        /// </summary>
        /// <param name="values"></param>
        public void AddValues(IEnumerable<ParserSegmentValue> values)
        {
            foreach (ParserSegmentValue value in values)
            {
                value.Parent = this;         // Změním parenta. Value může být doma jen v jednom segmentu.
                this._Values.Add(value);
            }
        }
        /// <summary>
        /// Do tohoto segmentu do jeho seznamu Values přidá nový objekt typu ParserSegmentValue (ValueType = InnerSegment).
        /// Do tohoto objektu Value vytvoří nový segment a vloží jej do value.InnerSegment.
        /// Vytvořený InnerSegment vrátí.
        /// Pro vytvoření nového segmentu použije dodanou metodu (parametr segmentCreator).
        /// </summary>
        /// <param name="segmentCreator"></param>
        /// <returns></returns>
        internal ParserSegment AddInnerSegment(int pointerBegin, Func<ParserSegmentValue, ParserSegment> segmentCreator)
        {
            ParserSegmentValue value = new ParserSegmentValue(this, pointerBegin, ParserSegmentValueType.InnerSegment);
            this._Values.Add(value);

            ParserSegment innerSegment = segmentCreator(value);
            value.InnerSegment = innerSegment;
            return innerSegment;
        }
        #endregion
        #region Změna charakteru segmentu
        /// <summary>
        /// Změní charakter tohoto segmentu na nově zadaný.
        /// Dohledá odpovídající setting. Uloží nově zadaný Begin a End, ale nemění pozice BeginAt a EndBefore (ty se vztahují k původnímu textu).
        /// Umožní založit novou value daného typu, jména a obsahu.
        /// </summary>
        /// <param name="segmentName"></param>
        /// <param name="begin">Text, kterým bude segment začínat (uvozovky, apostrof, závorka)</param>
        /// <param name="end">Text, kterým bude segment končit (uvozovky, apostrof, závorka)</param>
        /// <param name="valueType"></param>
        /// <param name="valueName"></param>
        /// <param name="content"></param>
        public void ChangeTo(string segmentName, string begin, string end, ParserSegmentValueType valueType, string valueName, string content)
        {
            this.Begin = (begin == null ? "" : begin);
            this.End = (end == null ? "" : end);
            ParserSegmentSetting segmentSetting = this.SegmentSetting.Parent.Segments.FirstOrDefault(s => s.SegmentName == segmentName);
            if (segmentSetting != null)
                this.SegmentSetting = segmentSetting;

            int beginAt = this.BeginAt;
            int endBefore = this.EndBefore;
            this._Values.Clear();
            if (valueType != ParserSegmentValueType.None)
            {
                ParserSegmentValue value = new ParserSegmentValue(this, this.BeginAt, valueType, content);
                value.ValueName = valueName;
                value.ValueEndBefore = endBefore;
                this._Values.Add(value);
            }
        }
        #endregion
        #region Vyhledání a filtrování parsovaných dat
        /// <summary>
        /// Najde a vrátí seznam segmentů, které vyhovují dané podmínce (predicate).
        /// Může rekurzivně prohledávat i vnořené segmenty, podle parametru recursive.
        /// </summary>
        /// <param name="predicate">Podmínka</param>
        /// <param name="recursive">Prohledávat i vnořené segmenty?</param>
        /// <returns></returns>
        public List<ParserSegment> FindAllSegments(Func<ParserSegment, bool> predicate, bool recursive)
        {
            List<ParserSegment> result = new List<ParserSegment>();
            this.AddAllSegments(result, predicate, recursive);
            return result;
        }
        /// <summary>
        /// Do daného seznamu přidá sebe pokud vyhovuje dané podmínce, a případně i rekurzivně pro svoje InnerSegments.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        /// <param name="recursive"></param>
        protected void AddAllSegments(List<ParserSegment> list, Func<ParserSegment, bool> predicate, bool recursive)
        {
            if (predicate(this))
                list.Add(this);
            if (recursive)
            {
                foreach (ParserSegmentValue value in this.Values.Where(v => v.HasInnerSegment))
                    value.InnerSegment.AddAllSegments(list, predicate, recursive);
            }
        }
        /// <summary>
        /// Najde a vrátí seznam hodnot, které vyhovují dané podmínce (predicate).
        /// Může rekurzivně prohledávat i vnořené segmenty, podle parametru recursive.
        /// </summary>
        /// <param name="predicate">Podmínka</param>
        /// <param name="recursive">Prohledávat i vnořené segmenty?</param>
        /// <returns></returns>
        public List<ParserSegmentValue> FindAllValues(Func<ParserSegmentValue, bool> predicate, bool recursive)
        {
            List<ParserSegmentValue> result = new List<ParserSegmentValue>();
            this.AddAllValues(result, predicate, recursive);
            return result;
        }
        /// <summary>
        /// Do daného seznamu přidá svoje Values, které vyhovují podmínce.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        /// <param name="recursive"></param>
        protected void AddAllValues(List<ParserSegmentValue> list, Func<ParserSegmentValue, bool> predicate, bool recursive)
        {
            foreach (ParserSegmentValue value in this.Values)
            {
                if (predicate(value))
                    list.Add(value);
                if (recursive && value.HasInnerSegment)
                    value.InnerSegment.AddAllValues(list, predicate, recursive);
            }
        }
        #endregion
        #region Generování formátovaného kódu pro zobrazení a editaci
        /// <summary>
        /// Metoda vrátí formátovaný text z dodaných segmentů.
        /// Pro formátování použije dodaný assembler. Assembler na počátku vyprázdní (dá Clear()), 
        /// ale na konci v něm nechá kompletní obsah (nedává Clear()), takže volající s naplněným assemblerem může provést i jiné věci.
        /// </summary>
        /// <param name="segments">Souhrn segmentů</param>
        /// <param name="assembler">Assembler = objekt, který z formátovacích prvků sestaví konkrétní formátovaný text.</param>
        /// <returns></returns>
        public static string GetFormattedText(IEnumerable<ParserSegment> segments, IFormattedTextAssembler assembler)
        {
            assembler.Clear();
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    if (segment != null)
                        assembler.AddRange(segment.FormattedItems);
                }
            }
            return assembler.Text;
        }
        /// <summary>
        /// Seznam všech formátovacích položek tohoto segmentu.
        /// Výstup lze vložit do RTF/HTML coderu.
        /// </summary>
        public IEnumerable<FormatCode> FormattedItems { get { return this.GetFormattedItems(); } }
        /// <summary>
        /// Vrátí seznam všech formátovacích položek vytvořený ze všech předaných segmentů.
        /// Výstup lze vložit do RTF/HTML coderu.
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static IEnumerable<FormatCode> GetFormattedItems(IEnumerable<ParserSegment> segments)
        {
            List<FormatCode> formatItems = new List<FormatCode>();
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    if (segment != null)
                        segment.AddFormattedItemsTo(formatItems);
                }
            }
            return formatItems;
        }
        /// <summary>
        /// Vrátí formátovací položky, které je možno vkládat do RTF/HTML coderu.
        /// </summary>
        /// <returns></returns>
        protected List<FormatCode> GetFormattedItems()
        {
            List<FormatCode> formatItems = new List<FormatCode>();
            this.AddFormattedItemsTo(formatItems);
            return formatItems;
        }
        /// <summary>
        /// Do seznamu formátovaných položek přidá data tohoto segmentu.
        /// Data jsou: 
        /// 1. Zahájení (nastavení fontu, dané definicí segmentu)
        /// 2. Tělo (obsah this.Values):
        ///  a) Content
        ///   aa) zahájení contentu (nastavení fontu platné pro tento segment)
        ///   ab) vlastní text contentu
        ///  b) Vnořené segmenty, rekurzivně od bodu 1)
        /// 3. Ukončení segmentu
        /// </summary>
        /// <param name="formatItems"></param>
        protected void AddFormattedItemsTo(List<FormatCode> formatItems)
        {
            // 1. Zahájení (nastavení fontu, dané definicí segmentu + výpis textu Begin, typicky uvozovky nebo jiná úvodní sekvence segmentu:
            if (this.HasBegin)
            {
                this.SegmentSetting.FormatCodesAddSet(formatItems, this, ParserSegmentValueType.Delimiter);
                formatItems.Add(FormatCode.NewText(this.Begin));
                this.SegmentSetting.FormatCodesAddReset(formatItems, this, ParserSegmentValueType.Delimiter);
            }

            // 2. Tělo (obsah this.Values):
            foreach (ParserSegmentValue value in this.Values)
            {
                switch (value.ValueType)
                {
                    case ParserSegmentValueType.Blank:
                    case ParserSegmentValueType.Text:
                    case ParserSegmentValueType.Delimiter:
                        // a) Content
                        this.AddFormattedContentTo(formatItems, value);
                        break;
                    case ParserSegmentValueType.InnerSegment:
                        // b) Vnořené segmenty, rekurzivně od bodu 1)
                        value.InnerSegment.AddFormattedItemsTo(formatItems);
                        break;
                }
            }

            // 3. Ukončení segmentu
            if (this.HasEnd)
            {
                this.SegmentSetting.FormatCodesAddSet(formatItems, this, ParserSegmentValueType.Delimiter);
                formatItems.Add(FormatCode.NewText(this.End));
                this.SegmentSetting.FormatCodesAddReset(formatItems, this, ParserSegmentValueType.Delimiter);
            }
        }
        /// <summary>
        /// Do seznamu formátovaných položek vloží text odpovídající textu tohoto segmentu (který má být IsContent).
        /// Pozor: v textu se musí reverzně nahradit speciální znaky!
        /// </summary>
        /// <param name="formatItems"></param>
        /// <param name="value"></param>
        protected void AddFormattedContentTo(List<FormatCode> formatItems, ParserSegmentValue value)
        {
            if (!value.HasContent) return;

            // aa) zahájení contentu (nastavení fontu platné pro tento segment a aktuální typ Value)
            this.SegmentSetting.FormatCodesAddSet(formatItems, value);

            // ab) vlastní text contentu
            string content = value.Content;
            string rtfContent = ReverseReplaceSpecialTexts(this.SegmentSetting, content);
            formatItems.Add(FormatCode.NewText(rtfContent));

            // ac) ukončení contentu (zrušení fontu a dalších nastavení pro tento segment a aktuální typ Value)
            this.SegmentSetting.FormatCodesAddReset(formatItems, value);
        }
        /// <summary>
        /// Provede reverzní náhradu speciálních textů: z čistých (Output) do vstupních (Input)
        /// </summary>
        /// <param name="segmentSetting"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected static string ReverseReplaceSpecialTexts(ParserSegmentSetting segmentSetting, string content)
        {
            string result = null;
            if (content != null)
            {
                result = content;
                if (segmentSetting.SpecialTexts != null && segmentSetting.SpecialTexts.Length > 0)
                {
                    foreach (ParserSegmentSpecialTexts specialText in segmentSetting.SpecialTexts)
                    {
                        if (result.Contains(specialText.OutputText))
                            result = result.Replace(specialText.OutputText, specialText.InputText);
                    }
                }
            }
            return result;
        }
        #endregion
    }
    #endregion
    #region class ParserSegmentValue : Jednotlivá hodnota v segmentu (text nebo segment).
    /// <summary>
    /// Jednotlivá hodnota v segmentu.
    /// Může to být nijak nespecifikovaný text, nebo to může být vnitřní segment.
    /// Obsahuje odkaz na svůj Parent segment.
    /// </summary>
    public class ParserSegmentValue
    {
        #region Konstrukce
        public ParserSegmentValue(ParserSegment parent, int beginAt, ParserSegmentValueType valueType)
        {
            this.Parent = parent;
            this.BeginAt = beginAt;
            this.ValueEndBefore = beginAt;
            this.ValueType = valueType;
            this.ContentBuilder = new StringBuilder();
        }
        public ParserSegmentValue(ParserSegment parent, int beginAt, ParserSegmentValueType valueType, string text)
        {
            this.Parent = parent;
            this.BeginAt = beginAt;
            this.ValueEndBefore = beginAt + (text == null ? 0 : text.Length);
            this.ValueType = valueType;
            this.ContentBuilder = new StringBuilder(text);
        }
        public ParserSegmentValue(ParserSegment parent, int beginAt, ParserSegmentValueType valueType, string text, int length)
        {
            this.Parent = parent;
            this.BeginAt = beginAt;
            this.ValueEndBefore = beginAt + length;
            this.ValueType = valueType;
            this.ContentBuilder = new StringBuilder(text);
        }
        public ParserSegmentValue(ParserSegment parent, int beginAt, ParserSegmentValueType valueType, ParserSegment innerSegment)
        {
            this.Parent = parent;
            this.BeginAt = beginAt;
            this.ValueEndBefore = beginAt;
            this.ValueType = valueType;
            this.InnerSegment = innerSegment;
            this.ContentBuilder = null;
        }
        protected string ContentCache;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Text;
        }
        #endregion
        #region Public property
        /// <summary>
        /// Parent segment, v němž je tato hodnota umístěna.
        /// Je nastaveno vždy.
        /// </summary>
        public ParserSegment Parent { get; internal set; }
        /// <summary>
        /// Root segment = nejvyšší Parent segment, v němž je tato hodnota umístěna.
        /// Je nastaveno vždy.
        /// </summary>
        public ParserSegment Root { get { return this.Parent.Root; } }
        /// <summary>
        /// Pozice ve vstupním textu (bufferu), na které hodnota začíná
        /// </summary>
        public int BeginAt { get; set; }
        /// <summary>
        /// Pozice ve vstupním textu (bufferu), před kterou tato hodnota končí.
        /// Rozdíl (EndBefore - BeginAt) = počet znaků tohoto segmentu včetně Inner.
        /// Na pozici EndBefore je první znak dalšího segmentu.
        /// </summary>
        public int EndBefore { get { return (this.HasInnerSegment ? this.InnerSegment.EndBefore : this.ValueEndBefore); } }
        /// <summary>
        /// EndBefore pokud this value nemá vnitřní segment, obsahuje jen text. (Pokud by this měla InnerSegment, pak EndBefore se čte z tohoto InnerSegment).
        /// Pozice ve vstupním textu (bufferu), před kterou tato hodnota končí.
        /// Rozdíl (EndBefore - BeginAt) = počet znaků tohoto segmentu včetně Inner.
        /// Na pozici EndBefore je první znak dalšího segmentu.
        /// </summary>
        public int ValueEndBefore { get; internal set; }
        /// <summary>
        /// Název hodnoty v tomto segmentu.
        /// </summary>
        public string ValueName { get; set; }
        /// <summary>
        /// Druh hodnoty v tomto segmentu
        /// </summary>
        public ParserSegmentValueType ValueType { get; private set; }
        /// <summary>
        /// Text, který je v této části obsažen (pokud je zadán = není null, pak musí být null vnořený segment this.Segment).
        /// Text je obsažen v prvcích typu Blank, Text, Delimiter.
        /// </summary>
        public virtual string Content
        {
            get
            {
                if (!this.HasContent) return null;
                if (this.ContentCache == null)
                    this.ContentCache = this.ContentBuilder.ToString();
                return this.ContentCache;
            }
        }
        /// <summary>
        /// true, pokud tento prvek má textový obsah (Content).
        /// To jest pro prvek typu Blank nebo Text nebo Delimiter, a když v Content je text dělší než 0.
        /// </summary>
        public virtual bool HasContent
        {
            get
            {
                return ((this.ValueType == ParserSegmentValueType.Blank || this.ValueType == ParserSegmentValueType.Text || this.ValueType == ParserSegmentValueType.Delimiter)
                        && this.ContentBuilder != null && this.ContentBuilder.Length > 0);
            }
        }
        /// <summary>
        /// true, pokud tento prvek má vnitřní segment (InnerSegment).
        /// To jest pro prvek typu InnerSegment, a když v InnerSegment není null.
        /// </summary>
        public virtual bool HasInnerSegment
        {
            get
            {
                return (this.ValueType == ParserSegmentValueType.InnerSegment && this.InnerSegment != null);
            }
        }
        /// <summary>
        /// Zde se skládá text postupným přidáváním
        /// </summary>
        protected StringBuilder ContentBuilder;
        /// <summary>
        /// Vnitřní vnořený segment, může obsahovat řadu dalších hodnot. Pokud není null, pak musí být null Content.
        /// </summary>
        public ParserSegment InnerSegment
        {
            get { return this._InnerSegment; }
            set
            {
                if (this.ValueType == ParserSegmentValueType.InnerSegment)
                {
                    if (this._InnerSegment != null)
                        throw new ParserModifyException("You can not insert a new object into the property ParserSegmentValue.InnerSegment, if there is already an object exists.", this.Parent.SegmentName);
                    this._InnerSegment = value;
                }
                else
                {
                    if (value == null)
                        this._InnerSegment = value;
                    else
                        throw new ParserModifyException("Unable to insert an object into the property ParserSegmentValue.InnerSegment, if ValueType is not InnerSegment.", this.Parent.SegmentName);
                }
            }
        }
        private ParserSegment _InnerSegment;
        /// <summary>
        /// Plný text hodnoty, včetně úvodního a koncového textu (uvozovky, závorky) a včetně vnořených hodnot (rozvinutých do textu).
        /// I vnitřní vnořené hodnoty mají uvedeny i svoje úvodní a koncové texty.
        /// Příklad: text v závorce: (abc * def * (x + y)) je tedy vrácen shodně: (abc * def * (x + y))
        /// Text v oddělovačích «{proměnná}» je vrácen včetně oddělovačů: «{proměnná}»
        /// Na rozdíl od property InnerText, která vrací text bez vnějších oddělovačů.
        /// </summary>
        public virtual string Text
        {
            get
            {
                if (this.HasInnerSegment) return this.InnerSegment.Text;
                if (this.HasContent) return this.Content;
                return "";
            }
        }
        /// <summary>
        /// Text segmentu, bez úvodního a koncového textu (uvozovky, závorky), ale obsahuje vnořené vnitřní hodnoty.
        /// Pozor: vnitřní vnořené hodnoty ale mají uvedeny i svoje úvodní a koncové texty.
        /// Příklad: text v závorce: (abc * def * (x + y)) je tedy vrácen bez vnějších závorek, ale s vnitřními závorkami: abc * def * (x + y)
        /// Text v oddělovačích «{proměnná}» je vrácen bez oddělovačů: proměnná
        /// Na rozdíl od property Text, která vrací text včetně vnějších oddělovačů.
        /// </summary>
        public virtual string InnerText
        {
            get
            {
                if (this.HasInnerSegment) return this.InnerSegment.InnerText;
                if (this.HasContent) return this.Content;
                return "";
            }
        }
        /// <summary>
        /// Text segmentu do SimpleTextu.
        /// Pokud jde o Value, která obsahuje InnerSegment, pak se z InnerSegmentu čte 
        /// jeho InnerSimpleText (tři tečky ohraničené originálním oddělovačem).
        /// </summary>
        public virtual string SimpleText
        {
            get
            {
                if (this.HasInnerSegment) return this.InnerSegment.SimplifiedText;
                if (this.HasContent) return this.Content;
                return "";
            }
        }
        /// <summary>
        /// Index, na němž se tato Value nachází v segmentu this.Parent v jeho seznamu ValueList.
        /// Hodnota indexu není uložena ve Value, ale pokaždé se nově dohledává.
        /// U dlouhých segmentů to může chvilku trvat.
        /// </summary>
        /// <returns></returns>
        public int CurrentIndex
        {
            get { return this.Parent.ValueList.FindIndex(v => Object.ReferenceEquals(v, this)); }
        }
        #endregion
        #region Public metody
        /// <summary>
        /// Nastaví do sebe daný text.
        /// Texty je povoleno vkládat jen pro prvky obsahující Text nebo Blank nebo Delimiter.
        /// Do prvků typu Segment nelze text vkládat, pro ně tato metoda vyvolá chybu.
        /// </summary>
        /// <param name="text"></param>
        public void SetContent(string text, int length)
        {
            this._SetContent(text, length, true, false);
        }
        /// <summary>
        /// Nastaví do sebe daný text.
        /// Texty je povoleno vkládat jen pro prvky obsahující Text nebo Blank nebo Delimiter.
        /// Do prvků typu Segment nelze text vkládat, pro ně tato metoda vyvolá chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="forceConvertToText">true = V případě potřeby převést this prvek na Text = do this.Content uložit this.Text, a změnit typ prvku.</param>
        public void SetContent(string text, int length, bool forceConvertToText)
        {
            this._SetContent(text, length, true, forceConvertToText);
        }
        /// <summary>
        /// Přidá do sebe další kousek textu do prvku Content.
        /// Texty je povoleno přidávat (tj. slučovat) jen pro prvky obsahující Text nebo Blank.
        /// Prvky obsahující Delimiter se již zakládají se zadaným textovým obsahem, a nelze je doplňovat.
        /// Prvky obsahující Segment vůbec nemají text.
        /// Pro prvky ostatních typů nelze tuto metodu použít, vyvolá chybu.
        /// </summary>
        /// <param name="text"></param>
        public void AddContent(string text, int length)
        {
            this._SetContent(text, length, false, false);
        }
        /// <summary>
        /// Přidá do sebe další kousek textu do prvku Content.
        /// Texty je povoleno přidávat (tj. slučovat) jen pro prvky obsahující Text nebo Blank.
        /// Prvky obsahující Delimiter se již zakládají se zadaným textovým obsahem, a nelze je doplňovat.
        /// Prvky obsahující Segment vůbec nemají text.
        /// Pro prvky ostatních typů nelze tuto metodu použít, vyvolá chybu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="forceConvertToText">true = V případě potřeby převést this prvek na Text = do this.Content uložit this.Text, a změnit typ prvku.</param>
        public void AddContent(string text, int length, bool forceConvertToText)
        {
            this._SetContent(text, length, false, forceConvertToText);
        }
        /// <summary>
        /// Vloží / přidá do sebe text do Content.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clearCurrentContent"></param>
        /// <param name="forceConvertToText"></param>
        private void _SetContent(string text, int length, bool clearCurrentContent, bool forceConvertToText)
        {
            switch (this.ValueType)
            {
                case ParserSegmentValueType.Blank:
                case ParserSegmentValueType.Text:
                case ParserSegmentValueType.Delimiter:
                    if (clearCurrentContent)
                        this._ClearContent();

                    if (text != null)
                    {
                        this.ContentBuilder.Append(text);
                        this.ValueEndBefore += length;
                        this.ContentCache = null;
                    }
                    break;
                default:
                    if (forceConvertToText)
                    {
                        this.ConvertToText();
                        if (clearCurrentContent)
                            this._ClearContent();
                        if (text != null)
                        {
                            this.ContentBuilder.Append(text);
                            this.ValueEndBefore += length;
                            this.ContentCache = null;
                        }
                    }
                    else
                        throw new ParserModifyException("You can not add text to an item ParserSegmentValue, which includes data type: " + this.ValueType.ToString() + ".");
                    break;
            }
        }
        private void _ClearContent()
        {
            this.ContentCache = null;
            this.ContentBuilder = new StringBuilder();
            this.ValueEndBefore = this.BeginAt;
        }
        /// <summary>
        /// Konvertuje sebe sama na typ Text
        /// </summary>
        public void ConvertToText()
        {
            string text = this.Text;
            int endBefore = (this.HasInnerSegment ? this.InnerSegment.EndBefore : this.ValueEndBefore);
            this.ValueType = ParserSegmentValueType.Text;
            this.InnerSegment = null;
            this.ValueName = null;
            this.ContentBuilder = new StringBuilder(text);
            this.ValueEndBefore = endBefore;
            this.ContentCache = null;
        }
        /// <summary>
        /// Vrátí informaci o tom, zda this je prvek typu Text, a zda obsahuje daný text.
        /// Slouží při zpracování textu.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool ContainText(string text)
        {
            return this._ContainText(text, StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Vrátí informaci o tom, zda this je prvek typu Text, a zda obsahuje daný text.
        /// Slouží při zpracování textu.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ignoreCase">true = ignorovat velikost písmen</param>
        /// <returns></returns>
        public bool ContainText(string text, bool ignoreCase)
        {
            return this._ContainText(text, (ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.Ordinal));
        }
        /// <summary>
        /// Vrátí informaci o tom, zda this je prvek typu Text, a zda obsahuje daný text.
        /// Slouží při zpracování textu.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private bool _ContainText(string text, StringComparison comparison)
        {
            if (this.ValueType != ParserSegmentValueType.Text || this.HasInnerSegment || !this.HasContent) return false;
            string content = this.Content;
            return String.Equals(content, text, comparison);
        }
        #endregion
    }
    /// <summary>
    /// Druh obsahu v této položce
    /// </summary>
    public enum ParserSegmentValueType
    {
        None = 0,
        Blank,
        Text,
        Delimiter,
        InnerSegment
    }
    #endregion
    #region class ParserSetting : informace pro textové parsování.
    /// <summary>
    /// ParserSetting : informace pro textové parsování.
    /// Obsahuje sadu definic segmentů.
    /// Segment je například string, závorka, komentář, text.
    /// Definice segmentů viz třída ParserSegmentSetting.
    /// </summary>
    public class ParserSetting
    {
        #region Konstrukce a základní property
        /// <summary>
        /// Konstruktor pro settings
        /// </summary>
        /// <param name="name">Jméno settingu, informativní text</param>
        /// <param name="initialSegmentName">Název segmentu, který očekáváme jako první. Bývá to segment typu "standardní text" (neměl by to být text typu například Řetězec, který je uzavřený do uvozovek).</param>
        public ParserSetting(string name, string initialSegmentName)
        {
            this.Name = name;
            this.InitialSegmentName = initialSegmentName;
            this._Segments = new List<ParserSegmentSetting>();
        }
        /// <summary>
        /// Jméno settingu, informativní text
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Název výchozího segmentu, kterým text začíná.
        /// Jeho pravidla platí tehdy, když začínáme, a pak vždy když skončí jeho vnořené segmenty.
        /// Tento segment nemusí mít delimitery (ale může).
        /// Tento segment by měl definovat, které vnořené bloky může obsahovat.
        /// </summary>
        public string InitialSegmentName { get; private set; }
        /// <summary>
        /// Definované vnitřní segmenty textu (čím začínají, jak se jmenují, čím končí, co mohou obsahovat, atd)
        /// </summary>
        public IEnumerable<ParserSegmentSetting> Segments { get { return this._Segments; } }
        private List<ParserSegmentSetting> _Segments;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = this.Name;
            for (int i = 0; i < this._Segments.Count; i++)
                text += (i == 0 ? "" : ",") + this._Segments[i].SegmentName;

            return text;
        }
        #endregion
        #region Přidávání, hledání
        /// <summary>
        /// Do nastavení segmentů přidá další položku
        /// </summary>
        /// <param name="segmentSetting"></param>
        public void SegmentSettingAdd(ParserSegmentSetting segmentSetting)
        {
            segmentSetting.Parent = this;
            this._Segments.Add(segmentSetting);
        }
        /// <summary>
        /// Vrátí segment daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ParserSegmentSetting GetSegment(string name)
        {
            return this.Segments.FirstOrDefault(s => s.SegmentName == name);
        }
        #endregion
        #region Služby
        /// <summary>
        /// Vrátí daný text rozdělený na pole stringů, kde oddělovače jsou určeny. Pole se dělí s options: RemoveEmptyEntries.
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static List<string> SplitTextToRows(string keywords, params char[] separators)
        {
            string[] keys = keywords.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            foreach (string key in keys)
                result.Add(key.Trim(' ', '\t', '\n', '\r'));
            return result;
        }
        /// <summary>
        /// Rozdělí daný text na jednotlivé znaky, ty znaky převede na stringy a vrátí pole těchto stringů.
        /// Použije se typicky pro snadnější zadání textů ParserSegmentSetting.Permitteds, kdy chceme vyjmenovat sadu povolených znaků.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static string[] SplitTextToOneCharStringArray(string text)
        {
            if (text == null) return null;
            char[] chars = text.ToCharArray();
            string[] result = chars.Select(c => c.ToString()).ToArray();
            return result;
        }
        #endregion
    }
    #endregion
    #region class ParserSegmentSetting : Popisek jednoho segmentu (část parsovaného kódu).
    /// <summary>
    /// ParserSegmentSetting : Popisek jednoho segmentu (část parsovaného kódu).
    /// Zde se definuje: jak se segment jmenuje, čím začíná, čím končí, co vnořeného může obsahovat...
    /// </summary>
    public class ParserSegmentSetting
    {
        #region Konstrukce a základní property
        /// <summary>
        /// Konstruktor definice segmentu daného jména
        /// </summary>
        /// <param name="segmentName">Jméno segmentu</param>
        public ParserSegmentSetting(string segmentName)
        {
            this._Parent = null;
            this.SegmentName = segmentName;
            this.FormatCodePairDict = new Dictionary<FormatCodeKey, FormatCodePair>();
        }
        /// <summary>
        /// Konstruktor definice segmentu daného jména.
        /// Opíše do sebe veškerá data (kromě SegmentName) z dodaného vzoru. Lze tak snadno vytvořit setting podobný settingu jež existujícímu.
        /// Pole obsažená v originále klonuje, neukládá referenci na identický objekt z originálu.
        /// </summary>
        /// <param name="segmentName">Jméno segmentu</param>
        /// <param name="original">Vzorový setting</param>
        public ParserSegmentSetting(string segmentName, ParserSegmentSetting original)
        {
            this._Parent = null;
            this.SegmentName = segmentName;
            this.BeginWith = original.BeginWith;
            if (original.Blanks != null) this.Blanks = original.Blanks.ToArray();
            if (original.Delimiters != null) this.Delimiters = original.Delimiters.ToArray();
            if (original.EndWith != null) this.EndWith = original.EndWith.ToArray();
            if (original.StopWith != null) this.StopWith = original.StopWith.ToArray();
            if (original.Illegal != null) this.Illegal = original.Illegal.ToArray();
            this.IsComment = original.IsComment;
            if (original.InnerSegmentsNames != null) this.InnerSegmentsNames = original.InnerSegmentsNames.ToArray();
            if (original.SpecialTexts != null) this.SpecialTexts = original.SpecialTexts.ToArray();
            this.FormatCodePairDict = new Dictionary<FormatCodeKey, FormatCodePair>(original.FormatCodePairDict);
        }
        /// <summary>
        /// Nastavení pro parsování, hlavička. Do uvedeného settingu patří nastavení segmentu (objekt this).
        /// </summary>
        public ParserSetting Parent
        {
            get { return this._Parent; }
            set
            {
                if (this._Parent != null)
                    throw new ParserSettingException("You can not change the value ParserSegmentSetting.Parent after he was once assigned.", this.SegmentName);
                this._Parent = value;
            }
        }
        private ParserSetting _Parent;
        /// <summary>
        /// Název segmentu, klíčové slovo
        /// </summary>
        public string SegmentName { get; private set; }
        /// <summary>
        /// Oddělovač, kterým tento segment začíná.
        /// </summary>
        public string BeginWith { get; set; }
        /// <summary>
        /// Texty, které jsou chápány jako blank (oddělovače bloků bez vlastního významu).
        /// Typicky jde o mezery, tabulátory, Cr, Lf.
        /// Text takového oddělovače je po parsování uložen do samostatného bloku, typu Blank.
        /// </summary>
        public string[] Blanks { get; set; }
        /// <summary>
        /// Texty, které jsou chápány jako významové oddělovače (mající vlastní význam).
        /// Typicky může jít o čárku, středník, operátory (+ - * /), znaky rovnosti a nerovnosti atd.
        /// Může jít i o řetězec delší než 1 znak (typicky "==", "!=", atd).
        /// Pak je při parsování detekován nejdelší odpovídající blok: 
        /// pokud je deklarován delimiter "=" i "==", pak při výskytu dvou rovnítek v textu je správně detekován a uložen delimiter "==".
        /// Text takového oddělovače je po parsování uložen do samostatného bloku, typu Delimiter.
        /// Každý detekovaný oddělovač je uložen do samostatného bloku. 
        /// Pokud po sobě následuje více oddělovačů, každý má svůj blok.
        /// </summary>
        public string[] Delimiters { get; set; }
        /// <summary>
        /// Explicitně daná délka segmentu = počet znaků.
        /// Standardně = null, délka je proměnná.
        /// V určitých případech lze nastavit na kladnou hodnotu, pak se do segmentu vezme jako obsah daný počet znaků ze vstupu - bez jakéhokoliv hledání konce (End nebo Stop, ty v tomto případě nemají význam).
        /// </summary>
        public Int32? ContentLength { get; set; }
        /// <summary>
        /// Oddělovače, kterými tento segment může skončit.
        /// Znaky, které jsou zde definovány, jsou ze vstupního textu odebrány a zařazeny do End znaků segmentu (=jsou "sežrané").
        /// Pokud segment může končit tím způsobem, že po něm následují znaky které do něj už nepatří, ale ty už mají být chápány jako následující text, pak jsou uvedeny v this.StopWith.
        /// </summary>
        public string[] EndWith { get; set; }
        /// <summary>
        /// Znaky, které - pokud budou nalezeny - budou chápány již jako obsah následujícího bloku = this segment bude ukončen, ale bez znaků konce. 
        /// Tyto nalezené znaky se vyhodnotí jako nové znaky v parent segmentu. Mohou začít další vnořený segment, nebo představují začátek textu.
        /// </summary>
        public string[] StopWith { get; set; }
        /// <summary>
        /// <para>
        /// Texty, které - pokud budou nalezeny - budou vyhodnoceny jako nepřípustné v daném segmentu. 
        /// Při parsování takového textu dojde k chybě typu ParserProcessException, kde bude uveden segment, chybný text a jeho pozice ve vstupním textu.
        /// Typicky se zde uvádějí ukončovací závorky povolených vnitřních segmentů, které ale nebyly zahájeny.
        /// </para>
        /// <para>
        /// Příklad chybného textu: "...(aaa(bb])..", kde "]" je nepovolený text. Pokud by text byl: "...(aaa[bb])..", byl by v pořádku protože znak ] by ukončoval segment [bb].
        /// </para>
        /// </summary>
        public string[] Illegal { get; set; }
        /// <summary>
        /// Tento segment je komentářem, nenese hodnotné informace
        /// </summary>
        public bool IsComment { get; set; }
        /// <summary>
        /// Speciální bloky textu a jejich konverze.
        /// <para>
        /// Speciální blok je typicky nějaká escape sekvence. Jde o posloupnost znaků, kdy některé znaky uvnitř posloupnosti se nemají chápat jako například oddělovače, ale jako znak.
        /// Příklad: pokud znak uvozovka ohraničuje řetězec (v EndWith je uvedena uvozovka), ale přitom je povoleno použít dvojici znaků (zpětné lomítko)(uvozovka) která reprezentuje uvozovku uvnitř řetězce, pak jde o speciální blok.
        /// Druhý znak z této dvojice tedy neukončuje řetězec. Celý dvojznak má význam jedné uvozovky uvnitř řetězce.
        /// </para>
        /// <para>
        /// Speciální text se tedy zadává jako dvojice řetězců: vstupní (jak je uveden v parsovaném textu) a výstupní (jak je text vepisován do výstupního segmentu).
        /// </para>
        /// </summary>
        public ParserSegmentSpecialTexts[] SpecialTexts { get; set; }
        /// <summary>
        /// Jediné povolené znaky/sekvence znaků. 
        /// Pokud je zadáno, pak znaky/sekvence uvnitř tohoto segmentu mohou být pouze z tohoto seznamu. Lze tak například definovat segment, který bude obsahovat výhradně číslice, nebo hexadecimální sekvenci 0xabcdABCD1234...
        /// Pokud zde není nic zadáno, pak uvnitř segmentu je povoleno vše (kromě Delimiterů, Blank, EndWith).
        /// </summary>
        public string[] Permitteds { get; set; }
        /// <summary>
        /// Seznam segmentů, které se mohou vyskytovat uvnitř tohoto elementu.
        /// Typicky v závorce může být string nebo další závorky.
        /// </summary>
        public string[] InnerSegmentsNames { get; set; }
        /// <summary>
        /// Seznam klíčových slov.
        /// Prvky tohoto segmentu, které jsou typu Text, se porovnávají s těmito klíčovými slovy, 
        /// a odpovídající texty dostanou ValueName = Parser.KEYWORD.
        /// </summary>
        public string[] Keywords { get; set; }
        /// <summary>
        /// Specifikuje, zda klíčová slova se mají vyhledávat case-sensitive.
        /// </summary>
        public bool KeywordsCaseSensitive { get; set; }
        /// <summary>
        /// Příznak, který povoluje spojování textů do jednoho prvku (Value) typu Text, pokud jde o následující kombinaci:
        /// {Text1} {Blank} {Text "."} {Blank} {Text2}
        /// Jinými slovy: při zahájení hodnoty Text2 se otestuje, zda předchozí hodnoty nejsou uvedené konfigurace
        /// (přičemž Blank jsou nepovinné, a teček může být více za sebou).
        /// Pokud ano, tak se předcházející Blank odeberou, texty "." se shrnou a vše se přidá k předchozímu Text1.
        /// Zjednodušeně: SQL jazyk dovoluje psát názvy objektů ve formě "lcs . tabulka . sloupec", což je následně nedetekovatelné, 
        /// protože texty jsou uloženy v oddělených Values.
        /// Tento EnableJoinDotSpaceText zajistí, že se tato specialita vyřeší již při parsování textu.
        /// </summary>
        internal bool EnableMergeDotSpaceText { get; set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = this.SegmentName;
            if (!String.IsNullOrEmpty(this.BeginWith))
                text += " " + this.BeginWith;
            if (this.InnerSegmentsNames != null && this.InnerSegmentsNames.Length > 0)
            {
                for (int i = 0; i < this.InnerSegmentsNames.Length; i++)
                    text += (i == 0 ? " {" : ", ") + this.InnerSegmentsNames[i];
                text += "} ";
            }
            if (this.EndWith != null && this.EndWith.Length > 0)
            {
                text += this.EndWith[0];
            }
            return text;
        }
        #endregion
        #region FormatCodes : deklarace formátovacích kódů pro různé druhy Values, jejich vkládání, hledání a podpora modifikace
        /// <summary>
        /// Přidá do segmentu deklaraci formátovacích kódů použitých na začátku určité hodnoty, dané typem.
        /// </summary>
        /// <param name="valueType">Typ hodnoty.
        /// Správné je předat typ: Blank, Delimiter, Text.
        /// Podle typu Text (bez názvu) se budou zobrazovat naše okraje (Begin a End, typicky závorky).
        /// Název je pro tuto položku nastaven na null.
        /// </param>
        /// <param name="codeSet">Formátovací kódy na začátku</param>
        /// <param name="codeReset">Formátovací kódy na konci</param>
        public void AddFormatCodes(ParserSegmentValueType valueType, FormatCode[] codeSet, FormatCode[] codeReset)
        {
            this.AddFormatCodes(new FormatCodePair(valueType, codeSet, codeReset));
        }
        /// <summary>
        /// Přidá do segmentu deklaraci formátovacích kódů použitých na začátku určité hodnoty, dané názvem.
        /// </summary>
        /// <param name="valueName">Název páru.
        /// Typ hodnoty (ValueType) je pro tuto položku nastaven na ParserSegmentValueType.Text.</param>
        /// <param name="codeSet">Formátovací kódy na začátku</param>
        /// <param name="codeReset">Formátovací kódy na konci</param>
        public void AddFormatCodes(string valueName, FormatCode[] codeSet, FormatCode[] codeReset)
        {
            this.AddFormatCodes(new FormatCodePair(valueName, codeSet, codeReset));
        }
        /// <summary>
        /// Přidá deklaraci formátovacích kódů.
        /// Kontroluje duplicitu.
        /// </summary>
        /// <param name="FormatCodePair"></param>
        protected void AddFormatCodes(FormatCodePair formatCodePair)
        {
            if (this.FormatCodePairDict.ContainsKey(formatCodePair))
                throw new ParserSettingException("Definice formátovacích kódů pro klíč " + formatCodePair.ToString() + " již v definici segmentu " + this.SegmentName + " existuje. Nelze přidat duplicitní položku.");
            this.FormatCodePairDict.Add(formatCodePair, formatCodePair);
        }
        /// <summary>
        /// Obsahuje (vrací) sadu všech formátovacích kódů, vrací celé páry.
        /// Používá se typicky při modifikaci obsažených formátovacích kódů (CodeSet, CodeReset).
        /// Touto cestou nelze přidat/odebrat celé definice ani změnit jejich klíče (Type, Name).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FormatCodePair> FormatCodePairs
        {
            get { return this.FormatCodePairDict.Values; }
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Set pro daný segment.
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="segment"></param>
        internal void FormatCodesAddSet(List<FormatCode> formatCodes, ParserSegment segment)
        {
            FormatCodePair codePair = this.FindFormatCodePair(new FormatCodeKey(ParserSegmentValueType.Text, (string)null));
            if (codePair == null || codePair.CodeSet == null || codePair.CodeSet.Length == 0) return;
            formatCodes.AddRange(codePair.CodeSet);
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Set pro daný segment.
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="segment"></param>
        internal void FormatCodesAddSet(List<FormatCode> formatCodes, ParserSegment segment, ParserSegmentValueType part)
        {
            FormatCodePair codePair = this.FindFormatCodePair(new FormatCodeKey(part, (string)null));
            if (codePair == null && part != ParserSegmentValueType.Text)
                codePair = this.FindFormatCodePair(new FormatCodeKey(ParserSegmentValueType.Text, (string)null));
            if (codePair == null || codePair.CodeSet == null || codePair.CodeSet.Length == 0) return;
            formatCodes.AddRange(codePair.CodeSet);
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Set pro danou položku (value).
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="value"></param>
        internal void FormatCodesAddSet(List<FormatCode> formatCodes, ParserSegmentValue value)
        {
            FormatCodePair codePair = this.FindFormatCodePair(value);
            if (codePair == null || codePair.CodeSet == null || codePair.CodeSet.Length == 0) return;
            formatCodes.AddRange(codePair.CodeSet);
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Reset pro daný segment.
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="segment"></param>
        internal void FormatCodesAddReset(List<FormatCode> formatCodes, ParserSegment segment)
        {
            FormatCodePair codePair = this.FindFormatCodePair(new FormatCodeKey(ParserSegmentValueType.Text, null));
            if (codePair == null || codePair.CodeReset == null || codePair.CodeReset.Length == 0) return;
            formatCodes.AddRange(codePair.CodeReset);
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Reset pro daný segment.
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="segment"></param>
        internal void FormatCodesAddReset(List<FormatCode> formatCodes, ParserSegment segment, ParserSegmentValueType part)
        {
            FormatCodePair codePair = this.FindFormatCodePair(new FormatCodeKey(part, (string)null));
            if (codePair == null && part != ParserSegmentValueType.Text)
                codePair = this.FindFormatCodePair(new FormatCodeKey(ParserSegmentValueType.Text, (string)null));
            if (codePair == null || codePair.CodeReset == null || codePair.CodeReset.Length == 0) return;
            formatCodes.AddRange(codePair.CodeReset);
        }
        /// <summary>
        /// Do seznamu formátovacích kódů vloží kódy typu Reset pro danou položku (value).
        /// </summary>
        /// <param name="formatCodes"></param>
        /// <param name="value"></param>
        internal void FormatCodesAddReset(List<FormatCode> formatCodes, ParserSegmentValue value)
        {
            FormatCodePair codePair = this.FindFormatCodePair(value);
            if (codePair == null || codePair.CodeReset == null || codePair.CodeReset.Length == 0) return;
            formatCodes.AddRange(codePair.CodeReset);
        }
        /// <summary>
        /// Vrátí definici formátovacích kódů (pár Set + Reset) pro danou položku (value).
        /// Pokud neexistuje, vrací null.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FormatCodePair FindFormatCodePair(ParserSegmentValue value)
        {
            if (value == null) return null;
            FormatCodeKey key = new FormatCodeKey(value.ValueType, (value.ValueType == ParserSegmentValueType.Text ? value.ValueName : (string)null));
            return FindFormatCodePair(key);
        }
        /// <summary>
        /// Vrátí definici formátovacích kódů (pár Set + Reset) pro daný klíč (key).
        /// Pokud neexistuje, vrací null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public FormatCodePair FindFormatCodePair(FormatCodeKey key)
        {
            if (key == null) return null;
            FormatCodePair pair;
            // Hledaný prvek:
            if (this.FormatCodePairDict.TryGetValue(key, out pair))
                return pair;
            // Náhradní prvek:
            if (this.FormatCodePairDict.TryGetValue(new FormatCodeKey(ParserSegmentValueType.Text, null), out pair))
                return pair;
            return null;
        }
        /// <summary>
        /// Dictionary obsahující FormatCodePair. Obsahuje je jak v Key (hledá se podle nich) tak ve Values = reálně uložené hodnoty.
        /// </summary>
        protected Dictionary<FormatCodeKey, FormatCodePair> FormatCodePairDict { get; private set; }
        #endregion
    }
    #endregion
    #region class ParserSegmentSpecialTexts : Speciální bloky textu a jejich konverze
    /// <summary>
    /// ParserSegmentSpecialTexts : Speciální bloky textu a jejich konverze.
    /// <para>
    /// Speciální blok je typicky nějaká escape sekvence. Jde o posloupnost znaků, kdy některé znaky uvnitř posloupnosti se nemají chápat jako například oddělovače, ale jako znak.
    /// Příklad: pokud znak uvozovka ohraničuje řetězec (v EndWith je uvedena uvozovka), ale přitom je povoleno použít dvojici znaků (zpětné lomítko)(uvozovka) která reprezentuje uvozovku uvnitř řetězce, pak jde o speciální blok.
    /// Druhý znak z této dvojice tedy neukončuje řetězec. Celý dvojznak má význam jedné uvozovky uvnitř řetězce.
    /// </para>
    /// <para>
    /// Speciální text se tedy zadává jako dvojice řetězců: vstupní (jak je uveden v parsovaném textu) a výstupní (jak je text vepisován do výstupního segmentu).
    /// </para>
    /// </summary>
    public class ParserSegmentSpecialTexts
    {
        public ParserSegmentSpecialTexts(string inputText, string outputText)
        {
            this.InputText = inputText;
            this.OutputText = outputText;
        }
        /// <summary>
        /// Text, který pokud bude nalezen, značí speciální text.
        /// V C# například \" značí jednu uvozovku, \r značí CR.
        /// V SQL například '' značí '
        /// </summary>
        public string InputText { get; private set; }
        /// <summary>
        /// Text, který se zapisuje do textu segmentu namísto vstupujícího textu.
        /// </summary>
        public string OutputText { get; private set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.InputText + " => " + this.OutputText;
        }
    }
    #endregion
    #region region ParserException
    /// <summary>
    /// Výjimka vyvolaná kdekoliv v procesu parsování. Bázová třída.
    /// </summary>
    public class ParserException : Exception
    {
        public ParserException()
            : base()
        { }
        public ParserException(string message)
            : base(message)
        { }
        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
    /// <summary>
    /// Výjimka vyvolaná v procesu analýzy settingu pro parsování. Typicky chybějící deklarace segmentu nebo duplicitní segment.
    /// </summary>
    public class ParserSettingException : ParserException
    {
        public ParserSettingException()
            : base()
        { }
        public ParserSettingException(string message)
            : base(message)
        { }
        public ParserSettingException(string message, Exception innerException)
            : base(message, innerException)
        { }
        public ParserSettingException(string message, string segmentName)
            : base(message)
        {
            this.SegmentName = segmentName;
        }
        /// <summary>
        /// Název segmentu, který je chybný (duplicitní nebo chybějící)
        /// </summary>
        public string SegmentName { get; private set; }
    }
    /// <summary>
    /// Výjimka vyvolaná v procesu provádění parsování textu. Typicky výskyt nepovolených znaků v segmentu, nebo logická chyba v parsování.
    /// </summary>
    public class ParserProcessException : ParserException
    {
        public ParserProcessException()
            : base()
        { }
        public ParserProcessException(string message)
            : base(message)
        { }
        public ParserProcessException(string message, Exception innerException)
            : base(message, innerException)
        { }
        public ParserProcessException(string message, int position, string text, string segmentName)
            : base(message)
        {
            this.Position = position;
            this.Text = text;
            this.SegmentName = segmentName;
        }
        /// <summary>
        /// Pozice ve vstupním textu, kde došlo k chybě
        /// </summary>
        public int? Position { get; private set; }
        /// <summary>
        /// Text, který byl na dané pozici nalezen
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Název segmentu, který se na dané pozici nachází
        /// </summary>
        public string SegmentName { get; private set; }
    }
        /// <summary>
    /// Výjimka vyvolaná v procesu modifikace segmentu po parsování. Typicky nepovolená operace v daném segmentu při jeho aktuálním stavu.
    /// </summary>
    public class ParserModifyException : ParserException
    {
        public ParserModifyException()
            : base()
        { }
        public ParserModifyException(string message)
            : base(message)
        { }
        public ParserModifyException(string message, Exception innerException)
            : base(message, innerException)
        { }
        public ParserModifyException(string message, string segmentName)
            : base(message)
        {
            this.SegmentName = segmentName;
        }
        /// <summary>
        /// Název segmentu, který je chybný (duplicitní nebo chybějící)
        /// </summary>
        public string SegmentName { get; private set; }
    }

    
    #endregion
    #region class FormatCodeKey a FormatCodePair : podpora pro formátování obsahu (Values) segmentu.
    /// <summary>
    /// FormatCodeKey : klíč pro pár formátovacích kódů.
    /// Pracuje s typem hodnoty a s jejím názvem (ValueType a ValueName).
    /// Slouží jako klíč, nenese konkrétní formátovací data.
    /// Funguje jako předek třídy FormatCodePair.
    /// </summary>
    public class FormatCodeKey
    {
        #region Konstrukce, overrides
        /// <summary>
        /// Vytvoří FormatCodeKey pro daný klíč a název, pro vyhledávání v Dictionary.
        /// </summary>
        /// <param name="valueType">Typ hodnoty.</param>
        /// <param name="valueName">Název hodnoty</param>
        internal FormatCodeKey(ParserSegmentValue value)
        {
            this.ValueType = value.ValueType;
            this.ValueName = value.ValueName;
            this._SetHashcode();
        }
        /// <summary>
        /// Vytvoří FormatCodeKey pro daný klíč a název, pro vyhledávání v Dictionary.
        /// </summary>
        /// <param name="valueType">Typ hodnoty.</param>
        /// <param name="valueName">Název hodnoty</param>
        internal FormatCodeKey(ParserSegmentValueType valueType, string valueName)
        {
            this.ValueType = valueType;
            this.ValueName = valueName;
            this._SetHashcode();
        }
        public override bool Equals(object obj)
        {
            FormatCodeKey other = obj as FormatCodeKey;
            if ((object)other == (object)null) return false;
            if (this.ValueType != other.ValueType) return false;
            return String.Equals(this.ValueName, other.ValueName);
        }
        public override int GetHashCode()
        {
            return this._Hashcode;
        }
        private void _SetHashcode()
        {
            if (!String.IsNullOrEmpty(this.ValueName))
                _Hashcode = this.ValueType.GetHashCode() ^ this.ValueName.GetHashCode();
            else
                _Hashcode = this.ValueType.GetHashCode() ^ 0x7654321;
        }
        private int _Hashcode;
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = this.ValueType.ToString();
            if (!String.IsNullOrEmpty(this.ValueName))
                text += ": " + this.ValueType;
            return text;
        }
        public static bool operator ==(FormatCodeKey a, FormatCodeKey b)
        {
            bool aIsNull = (object)a == (object)null;
            bool bIsNull = (object)b == (object)null;
            if (aIsNull && bIsNull) return true;           // Pokud jsou oba objekty null, pak == true
            if (aIsNull || bIsNull) return false;          // Pokud je jeden objekt null, a druhý ne, pak je == false
            return a.Equals(b);
        }
        public static bool operator !=(FormatCodeKey a, FormatCodeKey b)
        {
            bool aIsNull = (object)a == (object)null;
            bool bIsNull = (object)b == (object)null;
            if (aIsNull && bIsNull) return false;          // Pokud jsou oba objekty null, pak != false
            if (aIsNull || bIsNull) return true;           // Pokud je jeden objekt null, a druhý ne, pak je != true
            return !a.Equals(b);
        }
        #endregion
        #region Property
        /// <summary>
        /// Typ, pro který se tento RTF kód využije
        /// </summary>
        public ParserSegmentValueType ValueType { get; private set; }
        /// <summary>
        /// Název hodnoty, pro který se tento RTF kód využije
        /// </summary>
        public string ValueName { get; private set; }
        #endregion
    }
    /// <summary>
    /// FormatCodePair : podpora pro formátování obsahu (Values) segmentu.
    /// Obsahuje typ (ValueType), název (PairName) a pak dvě pole formátovacích kódů: jedno definuje formátovací kódy vkládané před určitou část (část je daná jménem) a druhé definuje formátovací kódy vložené za tuto část.
    /// Objekt této třídy (FormatCodePair) může hrát roli klíče v Dictionary.
    /// </summary>
    public class FormatCodePair : FormatCodeKey
    {
        #region Konstrukce
        /// <summary>
        /// Vytvoří FormatCodePair pro daný typ hodnoty.
        /// Název je pro tuto položku nastaven na null.
        /// </summary>
        /// <param name="valueType">Typ hodnoty.
        /// Správné je předat typ: Blank, Delimiter, Text.
        /// Podle typu Text (bez názvu) se budou zobrazovat naše okraje (Begin a End, typicky závorky).
        /// Název je pro tuto položku nastaven na null.
        /// </param>
        /// <param name="codeSet">RTF kódy na začátku</param>
        /// <param name="codeReset">RTF kódy na konci</param>
        public FormatCodePair(ParserSegmentValueType valueType, FormatCode[] codeSet, FormatCode[] codeReset)
            : base(valueType, null)
        {
            this.CodeSet = codeSet;
            this.CodeReset = codeReset;
        }
        /// <summary>
        /// Vytvoří FormatCodePair pro typ hodnoty Text pro daný název. 
        /// "Název" je uživatelská definice, umožňuje barevně odlišovat různé texty (Keyword, Numbers, Functions, atd).
        /// Z hlediska Parseru jsou to všechno Texty, ale z hlediska Lexeru mají různý význam.
        /// Typ hodnoty (ValueType) je pro tuto položku nastaven na ParserSegmentValueType.Text.
        /// </summary>
        /// <param name="valueName">Název páru.
        /// Typ hodnoty (ValueType) je pro tuto položku nastaven na ParserSegmentValueType.Text.</param>
        /// <param name="codeSet">Formátovací kódy na začátku</param>
        /// <param name="codeReset">Formátovací kódy na konci</param>
        public FormatCodePair(string valueName, FormatCode[] codeSet, FormatCode[] codeReset)
            : base(ParserSegmentValueType.Text, valueName)
        {
            this.CodeSet = codeSet;
            this.CodeReset = codeReset;
        }
        #endregion
        #region Property
        /// <summary>
        /// Formátovací kódy na začátku textu
        /// </summary>
        public FormatCode[] CodeSet { get; set; }
        /// <summary>
        /// Formátovací kódy na konci textu
        /// </summary>
        public FormatCode[] CodeReset { get; set; }
        #endregion
    }
    #endregion
    #region class FormatItem, enum FormatItemType
    /// <summary>
    /// Jeden prvek výstupního formátovaného textu. Vytváří se statickým konstruktorem: FormatItem item = FormatItem.New???(data);
    /// </summary>
    public class FormatCode
    {
        #region Private konstruktory (ItemType, Value)
        private FormatCode(FormatItemType itemType, string value)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.Text:
                    this.Text = value;
                    break;
                case FormatItemType.Code:
                    this.Code = value;
                    break;
                case FormatItemType.FontName:
                    this.FontName = value;
                    break;

            }
        }
        private FormatCode(FormatItemType itemType, int value)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.FontSize:
                    this.FontSize = value;
                    break;
            }
        }
        private FormatCode(FormatItemType itemType, Color value)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.BackColor:
                case FormatItemType.ForeColor:
                case FormatItemType.Highlight:
                    this.Color = value;
                    break;
            }
        }
        private FormatCode(FormatItemType itemType, FormatHighlightColor highlightColor)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.Highlight:
                    this.HighlightColor = highlightColor;
                    break;
            }
        }
        private FormatCode(FormatItemType itemType, FormatFontStyle fontStyle)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.FontStyle:
                    this.FontStyle = fontStyle;
                    break;
            }
        }
        private FormatCode(FormatItemType itemType, FormatAlignment alignment)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.ParagraphAlignment:
                    this.Alignment = alignment;
                    break;
            }
        }
        private FormatCode(FormatItemType itemType, bool value)
        {
            this._Reset(itemType);
            switch (itemType)
            {
                case FormatItemType.ProtectState:
                    this.IsProtected = value;
                    break;
            }
        }
        private void _Reset(FormatItemType itemType)
        {
            this.ItemType = itemType;
            this.Text = null;
            this.FontName = null;
            this.FontSize = 0;
            this.Color = Color.Empty;
            this.IsProtected = false;
        }
        public override string ToString()
        {
            if (this.ItemType == FormatItemType.Text)
                return this.Text;
            return "{" + this.ItemType.ToString() + "}";
        }
        #endregion
        #region Property
        public bool IsEmpty { get { return this.ItemType == FormatItemType.None; } }
        public FormatItemType ItemType { get; private set; }
        /// <summary>
        /// Čitelný text, pokud v tomto prvku má co pohledávat
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Explicitně zadaný vnitřní kód
        /// </summary>
        public string Code { get; private set; }
        public string FontName { get; private set; }
        public int FontSize { get; private set; }
        public FormatFontStyle FontStyle { get; private set; }
        public Color Color { get; private set; }
        public FormatHighlightColor HighlightColor { get; private set; }
        public bool IsProtected { get; private set; }
        public FormatAlignment Alignment { get; private set; }
        #endregion
        #region Static konstruktory
        public static FormatCode NewText(string text)
        {
            return new FormatCode(FormatItemType.Text, text);
        }
        public static FormatCode NewText(string text, bool addNewLine)
        {
            return new FormatCode(FormatItemType.Text, text + (addNewLine ? "\r" : ""));
        }
        /// <summary>
        /// Obsahuje znak pro nový řádek
        /// </summary>
        public static FormatCode NewLine { get { return new FormatCode(FormatItemType.Text, "\r"); } }
        /// <summary>
        /// Obsahuje znak pro defaultní odstavec
        /// </summary>
        public static FormatCode DefaultParagraph { get { return new FormatCode(FormatItemType.Code, @"\pard"); } }
        public static FormatCode NewFont(string fontName)
        {
            return new FormatCode(FormatItemType.FontName, fontName);
        }
        public static FormatCode NewSize(int fontSize)
        {
            return new FormatCode(FormatItemType.FontName, fontSize);
        }
        public static FormatCode NewFontStyle(FormatFontStyle fontStyle)
        {
            return new FormatCode(FormatItemType.FontStyle, fontStyle);
        }
        public static FormatCode NewParAlignment(FormatAlignment alignment)
        {
            return new FormatCode(FormatItemType.ParagraphAlignment, alignment);
        }
        public static FormatCode NewBackColor(Color color)
        {
            return new FormatCode(FormatItemType.BackColor, color);
        }
        public static FormatCode NewForeColor(Color color)
        {
            return new FormatCode(FormatItemType.ForeColor, color);
        }
        public static FormatCode NewHighlight(FormatHighlightColor highlight)
        {
            Color backColor = GetHighlightColor(highlight);
            return new FormatCode(FormatItemType.Highlight, backColor);
        }
        public static FormatCode NewHighlight(Color highlightColor)
        {
            return new FormatCode(FormatItemType.Highlight, highlightColor);
        }
        /// <summary>
        /// Vrátí konkrétní barvu pro typ barvy.
        /// </summary>
        /// <param name="highlight"></param>
        /// <returns></returns>
        private static Color GetHighlightColor(FormatHighlightColor highlight)
        {
            switch (highlight)
            {
                case FormatHighlightColor.None: return Color.Empty;
                case FormatHighlightColor.Black: return Color.Black;
                case FormatHighlightColor.Blue: return Color.Blue;
                case FormatHighlightColor.Cyan: return Color.Cyan;
                case FormatHighlightColor.Green: return Color.Green;
                case FormatHighlightColor.Magenta: return Color.Magenta;
                case FormatHighlightColor.Red: return Color.Red;
                case FormatHighlightColor.Yellow: return Color.Yellow;
                case FormatHighlightColor.DarkBlue: return Color.DarkBlue;
                case FormatHighlightColor.DarkCyan: return Color.DarkCyan;
                case FormatHighlightColor.DarkGreen: return Color.DarkGreen;
                case FormatHighlightColor.DarkMagenta: return Color.DarkMagenta;
                case FormatHighlightColor.DarkRed: return Color.DarkRed;
                case FormatHighlightColor.DarkYellow: return Color.Gold;
                case FormatHighlightColor.DarkGray: return Color.DarkGray;
                case FormatHighlightColor.LightGray: return Color.LightGray;
            }
            return Color.Empty;
        }
        public static FormatCode NewProtect(bool isProtected)
        {
            return new FormatCode(FormatItemType.ProtectState, isProtected);
        }
        #endregion
    }
    /// <summary>
    /// Formátování - Druh formátovacího prvku
    /// </summary>
    public enum FormatItemType
    {
        None,
        Text,
        Code,
        FontName,
        FontSize,
        FontStyle,
        ForeColor,
        BackColor,
        Highlight,
        ProtectState,
        ParagraphAlignment
    }
    /// <summary>
    /// Formátování - druh zarovnání
    /// </summary>
    public enum FormatAlignment
    {
        None,
        Left,
        Center,
        Right,
        Justify
    }
    /// <summary>
    /// Formátování - styl písma
    /// </summary>
    [Flags]
    public enum FormatFontStyle
    {
        None = 0,
        Bold = 0x01,
        BoldEnd = Bold << 1,
        Italic = BoldEnd << 1,
        ItalicEnd = Italic << 1,
        Underline = ItalicEnd << 1,
        UnderlineEnd = Underline << 1,
        StrikeOut = UnderlineEnd << 1,
        StrikeOutEnd = StrikeOut << 1,

        Regular = BoldEnd | ItalicEnd | UnderlineEnd | StrikeOutEnd,
        BoldOnly = Bold | ItalicEnd | UnderlineEnd | StrikeOutEnd,
        ItalicOnly = BoldEnd | Italic | UnderlineEnd | StrikeOutEnd,
        UnderlineOnly = BoldEnd | ItalicEnd | Underline | StrikeOutEnd,
        StrikeOutOnly = BoldEnd | ItalicEnd | UnderlineEnd | StrikeOut,
        BoldItalicOnly = Bold | Italic | UnderlineEnd | StrikeOutEnd
    }
    /// <summary>
    /// Formátování - barva pozadí v případě používání prastarých základních barev 1-16
    /// </summary>
    public enum FormatHighlightColor
    {
        None = 0,
        Black = 1,
        Blue = 2,
        Cyan = 3,
        Green = 4,
        Magenta = 5,
        Red = 6,
        Yellow = 7,
        DarkBlue = 9,
        DarkCyan = 10,
        DarkGreen = 11,
        DarkMagenta = 12,
        DarkRed = 13,
        DarkYellow = 14,
        DarkGray = 15,
        LightGray = 16
    }
    #endregion
    #region interface IFormattedTextAssembler
    /// <summary>
    /// Předpis pro objekt, který dokáže z předaných formátových prvků (značky, text) sestavit výstupní text v konkrétním formátu 
    /// (rtf, html, a další krásné formáty na sebe nenechají jistě dlouho čekat).
    /// </summary>
    public interface IFormattedTextAssembler
    {
        /// <summary>
        /// Vyprázdní vnitřní seznam prvků. Začíná se od nuly = od prázdného listu papíru.
        /// </summary>
        void Clear();
        /// <summary>
        /// Vloží do objektu jeden prvek
        /// </summary>
        /// <param name="item"></param>
        void Add(FormatCode item);
        /// <summary>
        /// Vloží do objektu řadu prvků
        /// </summary>
        /// <param name="items"></param>
        void AddRange(IEnumerable<FormatCode> items);
        /// <summary>
        /// Obsahuje aktuální zformátovaný text
        /// </summary>
        string Text { get; }
    }
    #endregion
}
