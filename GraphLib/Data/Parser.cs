using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Data.Parsing
{
    #region class Parser : parser textu na jednotlivé prvky včetně hierarchické struktury, podle předepsaných pravidel pro čtení konkrétního textu (pravidla jazyka).
    /// <summary>
    /// Parser : parser textu na jednotlivé prvky včetně hierarchické struktury, podle předepsaných pravidel pro čtení konkrétního textu (pravidla jazyka).
    /// </summary>
    internal class Parser : IDisposable
    {
        #region Konstrukce a proměnné
        /// <summary>
        /// Vygeneruje a vrátí new parser pro jazyk SQL.
        /// </summary>
        public static Parser CreateSqlParser() { return new Parser(DefaultSettings.MsSql); }
        /// <summary>
        /// Vytvoří pracovní instanci a vloží do ní settings + provede jeho analýzu.
        /// Pokud je settings nevyhovující, dojde k chybě.
        /// </summary>
        /// <param name="setting"></param>
        private Parser(Setting setting)
        {
            this._Setting = setting;
            this._SegmentSettings = new Dictionary<string, SettingSegmentCompiled>();
            this._CurrentSettingStack = new Stack<SettingSegmentCompiled>();
            this._AnalyseSetting();
        }
        /// <summary>
        /// Aktuální nastavení všech pravidel pro čtení
        /// </summary>
        private Setting _Setting;
        /// <summary>
        /// Sada předkompilovaných pravidel pro čtení jednotlivých typů segmentů
        /// </summary>
        private Dictionary<string, SettingSegmentCompiled> _SegmentSettings;
        /// <summary>
        /// Aktuálně zpracovávaný segment.
        /// Je to ten, který se nachází na vrcholu zásobníku _CurrentSettingStack.
        /// Pokud by byl zásobník prázdný, vrátí referenci na this._InitialSegmentSetting.
        /// Hodnota na vrchu zásobníku se nemění, jen se na ni koukneme a vrátíme (Peek, nikoli Pop).
        /// </summary>
        private SettingSegmentCompiled _CurrentSetting
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
        private Stack<SettingSegmentCompiled> _CurrentSettingStack;
        /// <summary>
        /// Výchozí segment, jeho deklarace. To je ten segment, který podle pravidel je na začátku vstupního textu.
        /// </summary>
        private SettingSegmentCompiled _InitialSegmentSetting;
        void IDisposable.Dispose()
        {
            this._SegmentSettings = null;
            this._Setting = null;
        }
        #endregion
        #region Analýza Settings = předkompilace (volá se z privátního konstruktoru třídy StringParser): vytvoří si pracovní Dictionary a další indexy pro rychlé parsování
        /// <summary>
        /// Provede analýzu aktuálního Settings.
        /// Volá se z privátního konstruktoru třídy Parser.
        /// </summary>
        private void _AnalyseSetting()
        {
            this._SegmentSettings.Clear();
            this._CurrentSettingStack.Clear();

            // Do dictionary nasypu vstupující segmentSettingy:
            foreach (SettingSegment segmentSetting in this._Setting.Segments)
                this._SegmentSettings.Add(segmentSetting.SegmentName, new SettingSegmentCompiled(segmentSetting));

            // Nyní do každého analyzovaného settingu vložím jeho inner segmenty:
            foreach (SettingSegmentCompiled settingAnalysed in this._SegmentSettings.Values)
            {
                if (settingAnalysed.ContainInnerSegments)
                {
                    for (int i = 0; i < settingAnalysed.SegmentSetting.InnerSegmentsNames.Length; i++)
                        settingAnalysed.InnerSegments[i] = this._SegmentSettings[settingAnalysed.SegmentSetting.InnerSegmentsNames[i]];
                }
            }

            // Nastavím si výchozí i aktuální segment = podle jména uvedeného v InitialSegmentName:
            this._InitialSegmentSetting = this._SegmentSettings[this._Setting.InitialSegmentName];
            this._CurrentSettingStack.Push(this._InitialSegmentSetting);
        }
        #endregion
        #region class SettingSegmentCompiled : Pravidla parsování pro segment, v "předkompilované" podobě
        /// <summary>
        /// SegmentSettingAnalysed : Třída obsahující "předkompilovanou" podobu pravidel pro jeden segment.
        /// Datově vychází z pravidel pro jeden segment, třída <see cref="SettingSegment"/>.
        /// Obsahuje jeho data převedená do polí a do indexů pro rychlou detekci znaků, a metody a funkce pro jejich zpracování.
        /// Obsahuje i reference na pravidla dalších segmentů.
        /// </summary>
        internal class SettingSegmentCompiled
        {
            #region Konstrukce
            internal SettingSegmentCompiled(SettingSegment segmentSetting)
            {
                this.SegmentSetting = segmentSetting;
                int length;

                length = segmentSetting.InnerSegmentsNames == null ? 0 : segmentSetting.InnerSegmentsNames.Length;
                this.InnerSegments = new SettingSegmentCompiled[length];      // jen připravím prostor, ale obsah do tohoto pole se bude vkládat až v druhé fázi analýzy celého settingu.

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
            /// Převede pole SpecialTexts na pole polí znaků (tzv. zubaté pole), 
            /// převádí texty z property SpecialTexts.InputText
            /// Na vstupu může být null nebo pole.
            /// Vytvoří se výstupní pole, jehož počet prvků odpovídá počtu prvků vstupujícího pole stringů.
            /// Do každého prvku výstupního pole se vloží další pole, které obsahuje hodnotu korespondující 
            /// SpecialTexts.InputText rozložené do pole char.
            /// </summary>
            /// <param name="specialTexts">Pole speciálních textů</param>
            /// <param name="containAny">out: Obsahuje pole nějaká data?</param>
            /// <returns></returns>
            private char[][] AnalyseSpecialTexts(SpecialTexts[] specialTexts, out bool containAny)
            {
                int length = (specialTexts == null ? 0 : specialTexts.Length);
                char[][] result = new char[length][];
                for (int i = 0; i < length; i++)
                {
                    SpecialTexts input = specialTexts[i];
                    if (input != null && input.EditorText != null)
                        result[i] = input.EditorText.ToCharArray();
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
            internal SettingSegment SegmentSetting { get; private set; }
            /// <summary>
            /// Pole vnořených definic segmentů (již analyzovné definice)
            /// </summary>
            internal SettingSegmentCompiled[] InnerSegments { get; private set; }
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
            /// Deklarace možných zakončení tohoto segmentu
            /// </summary>
            internal char[][] EndWithChar { get; private set; }
            /// <summary>
            /// true: Oddělovače na konci tohoto segmentu (this.EndWith) se nevkládají do konce segmentu (Segment.End), 
            /// ale zpracují se jako znaky následující za segmentem.
            /// Typicky jde o konec řádkového komentáře (Cr, Lf), které nejsou součástí komentáře, ale jde o standardní znak konce řádku v parent segmentu.
            /// </summary>
            internal bool EndOutside { get { return this.SegmentSetting.EndOutside; } }
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
            internal bool EnableMergeTextBlankDotText { get { return this.SegmentSetting.EnableMergeTextBlankDotText; } }
            /// <summary>
            /// true, pokud tento segment může obsahovat speciální bloky textu.
            /// </summary>
            public bool ContainSpecialTexts { get { return (this.SpecialTexts != null && this.SpecialTexts.Length > 0); } }
            /// <summary>
            /// Deklarace speciálních textů, pole deklarací párů (přímý přístup do this.SegmentSetting.SpecialTexts)
            /// </summary>
            internal SpecialTexts[] SpecialTexts { get { return this.SegmentSetting.SpecialTexts; } }
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
            /// Název segmentu, klíčové slovo
            /// </summary>
            public string SegmentName { get { return this.SegmentSetting.SegmentName; } }
            /// <summary>
            /// true, pokud tento segment může obsahovat nějaké vnořené vnitřní segmenty. Typicky v závorce může být string nebo další závorky.
            /// </summary>
            public bool ContainInnerSegments { get { return (this.InnerSegmentsNames != null && this.InnerSegmentsNames.Length > 0); } }
            /// <summary>
            /// Seznam segmentů, které se mohou vyskytovat uvnitř tohoto elementu.
            /// Typicky v závorce může být string nebo další závorky.
            /// </summary>
            public string[] InnerSegmentsNames { get { return this.SegmentSetting.InnerSegmentsNames; } }
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
            internal SegmentCurrentType DetectChange(char[] input, ref int pointer, out string text, out SettingSegmentCompiled nextSetting)
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
                {   // Našli jsme odpovídající vstupní text, ale vracet budeme text výstupní (převod Input => Output)
                    text = this.SpecialTexts[index].ValueText;
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
            protected bool IsBeginOfInnerSegment(char[] input, ref int pointer, out string text, out SettingSegmentCompiled nextSetting)
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
                        SettingSegmentCompiled settingAnalysed = this.InnerSegments[i];
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
        #region Řízení parsování textu = vlastní parser je zde
        /// <summary>
        /// Metoda provede parsování textu na jednotlivé prvky.
        /// Výstupem metody je prvek ParsedItem, který je vždy typu Array, a je Root. 
        /// Jeho Segment je ten, který je v pravidlech (<see cref="Setting"/>) označen jako <see cref="Setting.InitialSegmentName"/>.
        /// Z principu věci je tento Root prvek vždy Array, i když by obsahoval jen jedno písmeno = pak je toto obsaženo v prvním a jediném prvku v Items[0].
        /// Pokud pravidla (<see cref="Setting"/>) u Initial segmentu definují znaky typu End (tzn. tento segment může nějak skončit), 
        /// pak výsledný Root prvek ve svých Items obsahuje vždy sadu těchto segmentů, a teprve tyto segmenty obsahují parsovaný text.
        /// <para/>
        /// Například pravidla pro SQL určují, že <see cref="Setting.InitialSegmentName"/> = "SqlCode", 
        /// a definice segmentu "SqlCode" obsahuje (v <see cref="SettingSegment.EndWith"/>), že tento segment může být ukončen znakem ";".
        /// Proto parsování tohoto textu "DECLARE @cislo int;SET @cislo = 25;" vygeneruje následující strukturu:
        /// a) Výstupní <see cref="ParsedItem"/> je <see cref="ParsedItem.IsRoot"/>, a obshauje 2 prvky v poli <see cref="ParsedItem.Items"/>;
        /// b) Prvek [0] obsahuje pole prvků, které dávají dohromady první příkaz: "DECLARE @cislo int;"
        /// b1) Obsahuje tedy jednotlivé prvky: Text: "DECLARE"; Blank: " "; Text: "@cislo"; Blank: " "; Text: "int"; a hodnotu EndText: ";"
        /// b) Prvek [1] obsahuje pole prvků, které dávají dohromady první příkaz: "SET @cislo = 25;", obdobně poskládané.
        /// <para/>
        /// Pokud by <see cref="Setting.InitialSegmentName"/> v předaných pravidlech neobsahoval nic, co by ukončovalo daný segment, pak je situace jiná:
        /// Pak výsledný prvek (Root) obsahuje přímo jednotlivé prvky vstupního textu, protože není nic, čím by měly být strukturované na samostatné "bloky".
        /// </summary>
        /// <param name="code"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static ParsedItem ParseString(string code, Setting setting)
        {
            using (Parser parser = new Parser(setting))
            {
                ParsedItem result = parser.RunParse(code);
                return result;
            }
        }
        /// <summary>
        /// Metoda vrátí daný text rozdělený do jednotlivých syntaktických prvků.
        /// Položky odpovídají pravidlům, pro které byl tento parser vytvořen.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public ParsedItem ParseText(string text)
        {
            return this.RunParse(text);
        }
        /// <summary>
        /// Výkonný kód parsování, instanční prostředí s analyzovaným settingem.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        protected ParsedItem RunParse(string code)
        {
            string input = code;

            // Výstupní prvek = obsahuje pole prvků dle InitialSegmentSetting:
            SettingSegmentCompiled initialSetting = this._InitialSegmentSetting;
            ParsedItem rootParsedItem = ParsedItem.CreateRoot(code, initialSetting.SegmentSetting, initialSetting);
            ParsedItem currParsedItem = rootParsedItem;
            bool rootIsHost = initialSetting.HasEndWith;
            if (rootIsHost)
                // Pokud (Initial Setting) definuje nějaké znaky, které končí segment, pak Root prvek bude hostitelem pro jednotlivé Initial segmenty:
                //  viz následující kód pro case SegmentCurrentType.EndCurrentSegment:
                currParsedItem = ((IParsedItemCreate)rootParsedItem).AddArray(0, "", initialSetting.SegmentSetting, initialSetting);

            char[] codeChar = code.ToCharArray();
            int currPointer = 0;
            int codeLength = codeChar.Length;
            while (true)
            {
                SettingSegmentCompiled currentSetting = this._CurrentSetting;  // _CurrentSetting není jen tak nějaká proměnná, ale property vracející definici aktuálního segmentu!

                int prevPointer = currPointer;
                string currText;
                SettingSegmentCompiled nextSetting;
                SegmentCurrentType change = currentSetting.DetectChange(codeChar, ref currPointer, out currText, out nextSetting);

                // Nějaká forma úplného konce vstupního textu => musíme korektně dokončit všechny rozběhnuté segmenty a skončit:
                if (change == SegmentCurrentType.None)
                {   // Segmenty se musí všechny korektně ukončit, jsou na to stavěné:
                    while (currParsedItem != null && !((IParsedItemExtended)currParsedItem).IsRoot)
                        currParsedItem = ((IParsedItemCreate)currParsedItem).EndArray(currPointer, "");
                    break;
                }

                // Musíme prověřit, zda došlo k nějakému pokroku v parsování (pokud ne, ohlásíme chybu):
                this._CheckParseProgress(prevPointer, currPointer, change);

                // Nějaká forma pokroku:
                switch (change)
                {
                    case SegmentCurrentType.BeginNewSegment:
                        currParsedItem = ((IParsedItemCreate)currParsedItem).AddArray(prevPointer, currText, nextSetting.SegmentSetting, nextSetting);
                        this._CurrentSettingStack.Push(nextSetting);
                        break;

                    case SegmentCurrentType.Text:
                        ((IParsedItemCreate)currParsedItem).AddText(prevPointer, currText);
                        break;

                    case SegmentCurrentType.TextEnd:
                        // Nalezený text vepíšeme do aktuálního prvku jako každý jiný text:
                        ((IParsedItemCreate)currParsedItem).AddText(prevPointer, currText);

                        // Ale nalezený text současně ukončuje aktuální segment, protože ten má fixní délku.
                        // Takže na aktuální (lastPointer) pozici ukončíme Array, s textem End = "",
                        //  a zachováme se jako při standardním konci segmentu:
                        currParsedItem = ((IParsedItemCreate)currParsedItem).EndArray(prevPointer, "");
                        if (currParsedItem == null || (((IParsedItemExtended)currParsedItem).IsRoot && rootIsHost))
                            // Pokud (Initial Setting) definuje nějaké znaky, které končí segment, pak Root prvek bude hostitelem pro jednotlivé Initial segmenty:
                            //  viz následující kód pro case SegmentCurrentType.EndCurrentSegment:
                            currParsedItem = ((IParsedItemCreate)rootParsedItem).AddArray(currPointer, "", initialSetting.SegmentSetting, initialSetting);

                        if (this._CurrentSettingStack.Count > 0)
                            this._CurrentSettingStack.Pop();
                        break;

                    case SegmentCurrentType.Blank:
                        ((IParsedItemCreate)currParsedItem).AddBlank(prevPointer, currText);
                        break;

                    case SegmentCurrentType.Delimiter:
                        ((IParsedItemCreate)currParsedItem).AddDelimiter(prevPointer, currText);
                        break;

                    case SegmentCurrentType.EndCurrentSegment:
                    case SegmentCurrentType.StopCurrentSegment:
                        if (change == SegmentCurrentType.StopCurrentSegment || currentSetting.EndOutside)
                        {   // Pokud změna je typu StopCurrentSegment (=na aktuální pozici už začíná jiný segment);
                            // Anebo v aktuálním segmentu platí, že jeho ukončující znak[y] NEJSOU součástí segmentu, ale jen deklarují jeho konec,
                            //  pak načtený text zrušíme a pointer vrátíme na původní pozici, ukončíme aktuální segment,
                            //  a zde nalezený text se následně zpracuje znovu - jako součást nadřízeného segmentu v dalším cyklu, s jiným settingem:
                            currText = "";
                            currPointer = prevPointer;
                        }

                        currParsedItem = ((IParsedItemCreate)currParsedItem).EndArray(currPointer, currText);
                        if (currParsedItem == null || (((IParsedItemExtended)currParsedItem).IsRoot && rootIsHost))
                            // Pokud (Initial Setting) definuje nějaké znaky, které končí segment, pak Root prvek bude hostitelem pro jednotlivé Initial segmenty:
                            //  viz následující kód pro case SegmentCurrentType.EndCurrentSegment:
                            currParsedItem = ((IParsedItemCreate)rootParsedItem).AddArray(currPointer, "", initialSetting.SegmentSetting, initialSetting);

                        if (this._CurrentSettingStack.Count > 0)
                            this._CurrentSettingStack.Pop();
                        break;

                }
            }

            return rootParsedItem;
        }
        /// <summary>
        /// Metoda zkontroluje, zda došlo k nějakému pokroku v parsování.
        /// Pokud ne, oznámí chybu.
        /// </summary>
        /// <param name="lastPointer">Předešlá pozice</param>
        /// <param name="pointer">Aktuální pozice</param>
        /// <param name="change">Druh změny</param>
        private void _CheckParseProgress(int lastPointer, int pointer, SegmentCurrentType change)
        {
            if (pointer > lastPointer) return;
            if (change == SegmentCurrentType.BeginNewSegment || change == SegmentCurrentType.EndCurrentSegment) return;
            throw new InvalidOperationException("Metoda SegmentSettingAnalysed.DetectChange() nijak nepokročila.");
        }
        /// <summary>
        /// SegmentCurrentType : druh textu v aktuální pozici bufferu, řídí zpracování textu do segmentů.
        /// </summary>
        internal enum SegmentCurrentType
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
        #region Statické služby Parseru
        /// <summary>
        /// Metoda v daném textu výrazu vyhledá všechny prvky typu Delimiter, Text, Keyword, a umožní je zpracovat externě dodanou funkcí, a případně nahradit jejich obsah novým textem.
        /// Pro vyhledání se provede parsování daného textu, pak průchod jeho prvky, 
        /// při nalezení prvku typu {Delimiter, Text, Keyword} se tento prvek odešle do dané funkce (parametr textModifier), a podle jejího výsledku se provede náhrada textu.
        /// Předaná funkce tedy dostává celý prvek, a jednak jej sama může mírně upravit, a nebo může vracet:
        /// a) null = neprovede se žádná náhrada, prvek zůstane nezměněn;
        /// b) prázdný string = prvek typu Text bude nahrazen prvkem typu Blank s obsahem "";
        /// c) neprázdný string = ten bude vložen do textu na dané pozici.
        /// </summary>
        /// <param name="expression">Výraz k parsování</param>
        /// <param name="setting">Předpis pro parsování</param>
        /// <param name="childFilter">Funkce, která se rozhodne, zda scanovat do hloubky i vnořené prvky. 
        /// Například: obsah závorek v daném textu scanovat chceme, ale obsah komentářů a jiných scanovat nechceme.
        /// Tato funkce dostává každý Child prvek (=prvek typu <see cref="ItemType.Array"/>), 
        /// a podle jeho definice (<see cref="ParsedItem.SegmentName"/>) se rozhoduje, zda máme procházet i jeho prvky. 
        /// Metoda vrátí true = scanovat / false = nikoli.
        /// Pokud metoda vrátí false, pak se nebudou scanovat ani texty v tomto segmentu, ale ani jeho další vnořené prvky.
        /// Pokud je jako filtr předán null, pak se scanuje vše.
        /// </param>
        /// <param name="textModifier">Převáděč jmen tabulek na jejich aliasy. Odkaz na funkci. 
        /// Na vstupu dostává hodnotu <see cref="ParsedItem.Text"/> z prvků typu Text.
        /// Vrací null (=žádná změna) / prázdný string (=převést na typ Blank) / text (=změna obsahu prvku).</param>
        /// <returns></returns>
        public static string ScanChangeItems(string expression, Setting setting, Func<ParsedItem, bool> childFilter, Func<ParsedItem, string> textModifier)
        {
            if (expression == null || setting == null || textModifier == null) return expression;

            ParsedItem rootItem = ParseString(expression, setting);  // Parsování vrací VŽDY typ prvku = Array
            rootItem.ScanItems(null, childFilter, textModifier);
            return rootItem.Text;
        }
        #endregion
    }
    #endregion
    #region class ParsedItem : Výsledek parsování (typicky kolekce dalších položek typu ParsedItem, anebo již přímo text, nebo delimiter, nebo blank)
    /// <summary>
    /// ParsedItem : Výsledek parsování (typicky kolekce dalších položek typu ParsedItem, anebo již přímo text, nebo delimiter, nebo blank).
    /// Položka má svůj typ : ItemType = Blank, Delimiter, Text, Keyword, Array.
    /// <para/>
    /// Třída ParsedItem nabízí jen základní data. Pokud aplikace potřebuje více informací, nechť instanci ParsedItem přetypuje na interface <see cref="IParsedItemExtended"/>, 
    /// kde jsou připraveny další údaje pro rychlejší práci.
    /// <para/>
    /// Příklad: mějme na vstupu SQL text: "SELECT * FROM owner.table /* ukázka */;".
    /// Pak výstupem parsování bude prvek "A" třídy ParsedItem, jehož ItemType = Array = kolekce všech SQL příkazů, oddělených středníkem.
    /// Obsahem pole A.Items pak bude jeden prvek "B" ParsedItem, jehož ItemType = Array = kolekce prvků prvního SQL příkazu.
    /// Obsahem pole A.Items[0].Items pak budou jednotlivé komponenty SQL příkazu, a to v pořadí:
    /// [0] = "SELECT" = Keyword; 
    /// [1] = " " = Blank; 
    /// [2] = "*" = Delimiter; 
    /// [3] = " " = Blank; 
    /// [4] = "FROM" = Keyword;
    /// [5] = " " = Blank; 
    /// [6] = "owner.table" = Text; 
    /// [7] = " " = Blank; 
    /// [8] = "/* ukázka */" = Array, a jeho SegmentName = "BlockComment"; 
    /// </summary>
    internal class ParsedItem : IParsedItemCreate, IParsedItemExtended
    {
        #region Konstruktory
        /// <summary>
        /// Metoda vrátí nový objekt reprezentující Root prvek pro daný text.
        /// </summary>
        /// <param name="sourceText">Zdrojový text</param>
        /// <param name="setting">Pravidla segmentu veřejná</param>
        /// <param name="settingCompiled">Pravidla segmentu předkompilovaná</param>
        /// <returns></returns>
        public static ParsedItem CreateRoot(string sourceText, SettingSegment setting, Parser.SettingSegmentCompiled settingCompiled)
        {
            ParsedItem rootItem = new ParsedItem(null, setting, settingCompiled, ItemType.Array);
            rootItem._SourceText = sourceText;
            return rootItem;
        }
        /// <summary>
        /// Konstruktor nested prvku
        /// </summary>
        /// <param name="parent">Parent tohoto prvku</param>
        /// <param name="setting">Pravidla segmentu veřejná</param>
        /// <param name="settingCompiled">Pravidla segmentu předkompilovaná</param>
        /// <param name="itemType">Typ prvku</param>
        protected ParsedItem(ParsedItem parent, SettingSegment setting, Parser.SettingSegmentCompiled settingCompiled, ItemType itemType)
        {
            this.Parent = parent;
            this.Setting = setting;
            this.SettingCompiled = settingCompiled;
            this.ItemType = itemType;
            this.ExpressionState = ItemExpressionState.None;
            if (itemType == ItemType.Array)
                this._Items = new List<ParsedItem>();
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.Text; }
        #endregion
        #region Standardní public properties
        /// <summary>
        /// Typ obsahu v tomto prvku ParsedItem
        /// </summary>
        public ItemType ItemType { get; private set; }
        /// <summary>
        /// Význam obsahu tohoto prvku z hlediska Výrazu.
        /// Výchozí hodnota je None. Konkrétní hodnotu do této property doplňuje zpracovatelský kód.
        /// </summary>
        public ItemExpressionState ExpressionState { get; set; }
        /// <summary>
        /// Parent položka, do které this item patří. Root item zde má null.
        /// </summary>
        public ParsedItem Parent { get; private set; }
        /// <summary>
        /// Počet prvků v poli Items
        /// </summary>
        public int ItemCount { get { return (this._Items != null ? this._Items.Count : 0); } }
        /// <summary>
        /// Obsahuje true, pokud <see cref="Items"/> není null a obsahuje alespoň jeden záznam
        /// </summary>
        public bool HasItems { get { return (this._Items != null && this._Items.Count > 0); } }
        /// <summary>
        /// Jednotlivé vnořené prvky tohoto pole, pouze pokud this prvek je typu Array (<see cref="ItemType"/> == <see cref="ItemType.Array"/>).
        /// <para/>
        /// Obsahuje všechny prvky bez filtrování.
        /// </summary>
        public ParsedItem[] Items { get { return (this._Items != null ? this._Items.ToArray() : null); } }
        /// <summary>
        /// Název segmentu - vychází z jeho definice
        /// </summary>
        public string SegmentName { get { return this.Setting.SegmentName; } }
        /// <summary>
        /// Text prvku, obsahový (=speciální znaky ze vstupního textu jsou nahrazeny jejich významovým obsahem, viz níže).
        /// U prvků typu: Blank, Delimiter, Text, Keyword jde o jejich vlastní text.
        /// U prvku typu Array je v této property čten kompletní souhrn textů: BeginText + Suma(Items.Text = rekurzivně) + EndText.
        /// <para/>
        /// Speciální znaky v této property jsou nahrazeny jejich významovým obsahem.
        /// Proto se tento <see cref="Text"/> liší od hodnoty v <see cref="IParsedItemExtended.TextEdit"/> (tam jsou speciální znaky uvedeny).
        /// Příklad: Mějme SQL jazyk, v němž se stringy zadávají v apostrofech: 'text'. 
        /// Pokud je uvnitř stringu obsažen apostrof (jako znak v textu), pak se zapisuje jako dva apostrofy: 'text'' s apostrofem'. Takový text vstupuje do parseru.
        /// V property <see cref="IParsedItemExtended.TextEdit"/> prvku typu (<see cref="ItemType"/> == <see cref="ItemType.Text"/>) 
        /// uvnitř segmentu typu "Literal" (<see cref="Parent"/>.<see cref="Setting"/>) je ale daný řetězec uveden bez vnějších apostrofů, 
        /// a s jedním vnitřním apostrofem: {text' s apostrofem}.
        /// Naproti tomu v property <see cref="IParsedItemExtended.TextEdit"/> je uvnitř textu apostrof zdvojený: {text'' s apostrofem}.
        /// </summary>
        public string Text { get { return this.GetText(TextMode.CurrentText | TextMode.AddBeginEnd); } }
        /// <summary>
        /// Text prvku, vnitřní text tohoto pole.
        /// Pokud this je běžný textový prvek (Delimiter, Text, Keyword), pak <see cref="TextInner"/> == <see cref="Text"/>.
        /// Pokud this je Array, a má naplněné texty <see cref="BeginText"/> a <see cref="EndText"/>, pak <see cref="TextInner"/> neobsahuje tyto úvodní a koncové texty, obsahuje jen texty vnitřních prvků.
        /// Vnitřní prvky ale svoje úvodní a koncové texty mají.
        /// Například pokud this obsahuje blokový komentář, pak <see cref="Text"/> = "/*..obsah komentáře..*/", 
        /// ale <see cref="TextInner"/> obsahuje jen "..obsah komentáře.." (tj. bez znaků /* a */).
        /// Podobně pole, které obsahuje řetězec, v property <see cref="Text"/> obsahuje krajní uvozovky, ale <see cref="TextInner"/> je nemá.
        /// </summary>
        public string TextInner { get { return this.GetText(TextMode.CurrentText); } }
        /// <summary>
        /// Prostor pro libovolná data uživatele
        /// </summary>
        public object UserData { get; set; }
        #endregion
        #region Generování RTF kódu pro editaci
        /// <summary>
        /// Obsahuje kompletní RTF text tohoto segmentu (včetně hlavičky).
        /// String, který je získán z této property, může být uložen jako soubor.rtf, nebo zobrazen v RtfTextBoxu.
        /// Upozornění: RTF text je určen k zobrazení a k editaci, tedy i k následnému opakovanému parsování.
        /// Proto RTF text musí obsahovat speciální znaky psané tak, jak jsou psány na vstupu do parsování, nikoliv tak, jak jsou v čistém textu (<see cref="Text"/>).
        /// Příklad: v C# máme string "Aaaa\"Bbbb", což značí že uprostřed je uvozovka. Čistá hodnota (Text) je Aaaa"Bbbb .
        /// Do RTF textu se ale dostává hodnota včetně oddělovačů (uvozovky na okrajích), a vnitřní uvozovka se do RTF textu vepisuje shodně jako na vstupu (tj. se zpětným lomítkem).
        /// Obdobně v SQL stringu mějme string = 'Aaaa''Bbbb', čistý Text = Aaaa'Bbbb a Rtf text musí opět nést uprostřed apostrofy dva.
        /// </summary>
        protected string RtfText { get { return this.GetRtfText(); } }
        /// <summary>
        /// Vrátí kompletní RTF text za tento segment jako string.
        /// </summary>
        /// <returns></returns>
        protected string GetRtfText()
        {
            using (RtfCoder rtfCoder = new RtfCoder())
            {
                List<FormatCode> rtfItems = this.GetRtfItems();
                rtfCoder.AddRange(rtfItems);
                return rtfCoder.RtfText;
            }
        }
        /// <summary>
        /// Vrátí položky RTF items, které je možno vkládat do RTF coderu.
        /// </summary>
        /// <returns></returns>
        protected List<FormatCode> GetRtfItems()
        {
            List<FormatCode> rtfItems = new List<FormatCode>();
            this.AddRtfItems(rtfItems);
            return rtfItems;
        }
        /// <summary>
        /// Do předaného seznamu RTF prvků přidá prvky tohoto prvku a všch jeho podřízených prvků.
        /// Data jsou:
        /// 1. Zahájení (nastavení fontu, dané definicí segmentu)
        /// 2. Tělo (obsah this.Values):
        ///  a) Content
        ///   aa) zahájení contentu (nastavení fontu platné pro tento segment)
        ///   ab) vlastní text contentu
        ///  b) Vnořené segmenty, rekurzivně od bodu 1)
        /// 3. Ukončení segmentu
        /// </summary>
        /// <param name="rtfItems"></param>
        protected void AddRtfItems(List<FormatCode> rtfItems)
        {
            switch (this.ItemType)
            {
                case ItemType.None:
                case ItemType.Blank:
                case ItemType.Delimiter:
                case ItemType.Text:
                case ItemType.Keyword:
                    this.AddRtfText(rtfItems);
                    break;
                case ItemType.Array:
                    // RTF kódy pro this segment (jde o kódy pro Begin a End text, nikoliv o kódy pro vnitřní prvky):
                    FormatCodePair rtfCodes = ((ISettingSegmentInternal)this.Setting).GetRtfCode(this.ItemType);

                    if (this.BeginText != null && this.BeginText.Length > 0)
                    {   // Úvodní znaky: levá závorka, uvozovky, atd:
                        AddRtfCodeSet(rtfItems, rtfCodes);
                        rtfItems.Add(FormatCode.NewText(this.BeginText));
                        AddRtfCodeReset(rtfItems, rtfCodes);
                    }
                    foreach (ParsedItem childItem in this._Items)
                    {   // Vnitřní obsah: výrazy v závorce, text v uvozovkách, atd:
                        childItem.AddRtfItems(rtfItems);
                    }
                    if (this.EndText != null && this.EndText.Length > 0)
                    {   // Koncové znaky: pravá závorka, uvozovky, atd:
                        AddRtfCodeSet(rtfItems, rtfCodes);
                        rtfItems.Add(FormatCode.NewText(this.EndText));
                        AddRtfCodeReset(rtfItems, rtfCodes);
                    }
                    break;
            }
        }
        /// <summary>
        /// Do seznamu RTF položek přidá položky odpovídající zdejšímu textovému obsahu.
        /// Používá se pouze na jednoduchém typu (Blank, Delimiter, Text, Keyword), nikoli na Array.
        /// Pozor: v textu se musí reverzně nahradit speciální znaky!
        /// </summary>
        /// <param name="rtfItems"></param>
        protected void AddRtfText(List<FormatCode> rtfItems)
        {
            string text = this.Text;
            if (text.Length == 0) return;

            // Setting (zdejší nebo z parenta), a z něj získané kódy pro this typ prvku:
            SettingSegment setting = this.Setting ?? this.Parent.Setting;
            FormatCodePair rtfCodes = ((ISettingSegmentInternal)setting).GetRtfCode(this.ItemType);

            // aa) zahájení contentu (nastavení fontu platné pro tento segment a aktuální typ Value)
            AddRtfCodeSet(rtfItems, rtfCodes);

            // ab) vlastní text contentu
            string rtfText = ReverseReplaceSpecialTexts(this.Setting, text);
            rtfItems.Add(FormatCode.NewText(rtfText));

            // ac) ukončení contentu (zrušení fontu a dalších nastavení pro tento segment a aktuální typ Value)
            AddRtfCodeReset(rtfItems, rtfCodes);
        }
        /// <summary>
        /// Metoda přidá do pole RTF prvků (rtfItems) další prvky z rtfCodes.CodeSet (pokud není null a něco obsahuje).
        /// </summary>
        /// <param name="rtfItems"></param>
        /// <param name="rtfCodes"></param>
        protected static void AddRtfCodeSet(List<FormatCode> rtfItems, FormatCodePair rtfCodes)
        {
            if (rtfCodes != null && rtfCodes.CodeSet != null && rtfCodes.CodeSet.Length > 0)
                rtfItems.AddRange(rtfCodes.CodeSet);
        }
        /// <summary>
        /// Metoda přidá do pole RTF prvků (rtfItems) další prvky z rtfCodes.CodeReset (pokud není null a něco obsahuje).
        /// </summary>
        /// <param name="rtfItems"></param>
        /// <param name="rtfCodes"></param>
        protected static void AddRtfCodeReset(List<FormatCode> rtfItems, FormatCodePair rtfCodes)
        {
            if (rtfCodes != null && rtfCodes.CodeReset != null && rtfCodes.CodeReset.Length > 0)
                rtfItems.AddRange(rtfCodes.CodeReset);
        }
        /// <summary>
        /// Provede reverzní náhradu speciálních textů: z čistých (Output) do vstupních (Input)
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected static string ReverseReplaceSpecialTexts(SettingSegment setting, string content)
        {
            string result = null;
            if (content != null)
            {
                result = content;
                if (setting.SpecialTexts != null && setting.SpecialTexts.Length > 0)
                {
                    foreach (SpecialTexts specialText in setting.SpecialTexts)
                    {
                        if (result.Contains(specialText.ValueText))
                            result = result.Replace(specialText.ValueText, specialText.EditorText);
                    }
                }
            }
            return result;
        }
        #endregion
        #region Scanování this prvku a jeho Items
        /// <summary>
        /// Metoda v this prvku vyhledá všechny prvky typu { Delimiter, Text, Keyword }, a umožní je zpracovat externě dodanou funkcí (parametr textModifier),
        /// a případně nahradit jejich obsah novým textem (na základě výstupní hodnoty funkce).
        /// Pro vyhledání se provede průchod přes prvky this prvku, při nalezení prvku typu { Delimiter, Text, Keyword } se tento prvek odešle do dané funkce (parametr textModifier), 
        /// a podle jejího výsledku se provede náhrada textu. Předaná funkce tedy dostává celý prvek, a jednak jej sama může mírně upravit, a nebo může vracet:
        /// a) null = neprovede se žádná náhrada, prvek zůstane nezměněn;
        /// b) prázdný string = prvek typu Text bude nahrazen prvkem typu Blank s obsahem "";
        /// c) neprázdný string = ten bude vložen do textu na dané pozici.
        /// </summary>
        /// <param name="processFilter">Funkce, která dostává úplně všechny prvky, a rozhoduje se zda provádět jejich další analýzu.
        /// Účelem této funkce je například přeskočit komentáře a blank prvky.
        /// Pokud je null, pak se analyzují všechny prvky.
        /// </param>
        /// <param name="recurseFilter">Filtr, který je volán pro každý prvek typu Array, a rozhoduje o tom, zda se má provádět scanování vnitřních prvků tohoto prvku.
        /// Účelem filtru je odlišit od sebe prvky Array, které mají vnitřní strukturu (typicky vnořený kód) 
        /// od prvků, které jsou sice z formálního hlediska Array, ale významově jde o standardní jednoduchý prvek (typicky string).
        /// Například: obsah závorek v daném textu scanovat chceme, ale obsah komentářů a jiných segmentů scanovat nechceme.
        /// Tato funkce dostává každý Child prvek (=prvek typu <see cref="ItemType.Array"/>), 
        /// a podle jeho definice (název segmentu = <see cref="ParsedItem.SegmentName"/>) se rozhoduje, zda máme procházet i jeho prvky.
        /// Metoda vrátí true = scanovat / false = nikoli.
        /// Pokud metoda vrátí false, pak se nebudou scanovat ani texty v tomto segmentu, ale ani jeho další vnořené prvky.
        /// Pokud je jako filtr předán null, pak se scanuje vše.
        /// </param>
        /// <param name="textModifier">Odkaz na funkci, která pro daný prvek ParsedItem rozhodne, zda se má změnit jeho textový obsah.
        /// Na vstupu dostává hodnotu <see cref="ParsedItem.Text"/> z prvků typu Text.
        /// Vrací null (=žádná změna) / prázdný string (=převést na typ Blank) / text (=změna obsahu prvku).
        /// Upozornění: pokud bude vrácen neprázdný string, pak bude vložen do property <see cref="Text"/>, ale neprovede se změna <see cref="ItemType"/>!
        /// Pokud tedy bude nahrazeno klíčové slovo (např. FROM) za string ",", pak se prvek bude i nadále tvářit jako Keyword.
        /// Komplexní změnu prvku zajistí metoda <see cref="IParsedItemCreate.ChangeTo(ItemType, string)"/>.
        /// </param>
        public void ScanItems(Func<ParsedItem, bool> processFilter, Func<ParsedItem, bool> recurseFilter, Func<ParsedItem, string> textModifier)
        {
            bool hasProcess = (processFilter != null);
            bool hasRecurse = (recurseFilter != null);
            bool hasTextModifier = (textModifier != null);
            if (this.IsSimpleText)
            {
                bool process = (hasProcess ? processFilter(this) : true);
                if (process && hasTextModifier)
                    this.ScanItem(textModifier);
                return;
            }
            if (!this.IsArray) return;

            Stack<ScanItemInfo> stack = new Stack<ScanItemInfo>();
            stack.Push(new ScanItemInfo(this));
            while (stack.Count > 0)
            {
                ScanItemInfo scanItem = stack.Pop();

                while (!scanItem.EndOfChilds)                        // Dokud máme nějaké nezpracované Child prvky
                {
                    ParsedItem childItem = scanItem.GetNextChild();  // Vrátí prvek z aktuální pozice, posune index aktuální pozice na +1;
                    bool process = (hasProcess ? processFilter(childItem) : true);
                    if (!process) continue;

                    if (childItem.IsArray)
                    {   // Aktuální Child je Array:
                        bool recurse = (hasRecurse ? recurseFilter(childItem) : true);
                        if (recurse)
                        {   // Pokud budeme zpracovávat toto pole rekurzí, zajistíme to nyní:
                            stack.Push(scanItem);                    // Do zásobníku vložím aktuální scanItem, vrátíme se k němu až poté, co projdeme nový childItem
                            stack.Push(new ScanItemInfo(childItem)); // Do zásobníku přidám nový prvek pro childItem, ten budu scanovat v příštím cyklu od jeho indexu [0]
                            break;                                   // Opustím smyčku scanování while (scanItem..), abych mohl ze zásobníku (stack) vyjmout nový prvek child...
                        }
                    }
                    else if (childItem.IsSimpleText)
                    {   // Běžný textový prvek:
                        if (hasTextModifier)
                            childItem.ScanItem(textModifier);
                    }
                }
            }
        }
        /// <summary>
        /// Zajistí provedení modifikace pro this prvek
        /// </summary>
        /// <param name="textModifier"></param>
        protected void ScanItem(Func<ParsedItem, string> textModifier)
        {
            string modifiedText = textModifier(this);
            if (modifiedText != null)
            {   // Pokud výstup není null, pak se bude dělat modifikace:
                if (String.IsNullOrWhiteSpace(modifiedText))
                {   // Z textu uděláme Blank:
                    ((IParsedItemCreate)this).ChangeTo(ItemType.Blank, modifiedText);
                }
                else
                {   // Do prvku typu Text vložíme nový obsah:
                    ((IParsedItemCreate)this).ChangeTo(ItemType.Text, modifiedText);
                }
            }
        }
        /// <summary>
        /// Třída umožňující provádět sekvenční scanování v nativním pořadí bez použití rekurzivního volání scanovací metody, jen s použitím Stack zásobníku
        /// </summary>
        protected class ScanItemInfo
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="arrayItem"></param>
            public ScanItemInfo(ParsedItem arrayItem)
            {
                this.ArrayItem = arrayItem;
                this.ChildIndex = 0;
            }
            /// <summary>
            /// Prvek, jehož jednotlivé <see cref="ParsedItem.Items"/> se prohlíží
            /// </summary>
            public ParsedItem ArrayItem { get; private set; }
            /// <summary>
            /// Index prvku v poli <see cref="ArrayItem"/> : <see cref="ParsedItem.Items"/>, který se bude prohlížet v dalším kroku. Výchozí hodnota = 0.
            /// Jakmile bude <see cref="ChildIndex"/> mimo rozsah počtu prvků pole, bude <see cref="EndOfChilds"/> == true.
            /// </summary>
            public int ChildIndex { get; private set; }
            /// <summary>
            /// true pokud jsme na konci s položkami, v tomto případě metoda <see cref="GetNextChild()"/> vrátí null.
            /// false pokud máme položky, a metoda <see cref="GetNextChild()"/> vrátí vhodnou další položku.
            /// </summary>
            public bool EndOfChilds { get { return (this.ArrayItem == null || !this.ArrayItem.IsArray || this.ChildIndex < 0 || this.ChildIndex >= this.ArrayItem.ItemCount); } }
            /// <summary>
            /// Metoda vrátí prvek <see cref="ArrayItem"/>[<see cref="ChildIndex"/>], a poté navýší <see cref="ChildIndex"/> o +1.
            /// Pokud ale na začátku je <see cref="EndOfChilds"/> = true, pak vrací null.
            /// </summary>
            /// <returns></returns>
            public ParsedItem GetNextChild()
            {
                if (this.EndOfChilds) return null;
                ParsedItem childItem = this.ArrayItem._Items[this.ChildIndex];
                this.ChildIndex = this.ChildIndex + 1;
                return childItem;
            }
        }
        #endregion
        #region Něco málo soukromého
        /// <summary>
        /// Root položka
        /// </summary>
        protected ParsedItem Root
        {
            get
            {
                ParsedItem root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }
        /// <summary>
        /// true pokud this prvek je Root.
        /// Root prvek nemá Parenta.
        /// </summary>
        protected bool IsRoot { get { return (this.Parent == null); } }
        /// <summary>
        /// true pokud this prvek má (nebo alespoň může mít) vnořené prvky v <see cref="Items"/> (tj. pokud <see cref="ItemType"/> == <see cref="ItemType.Array"/>).
        /// Neověřuje se jejich počet, tedy HasItems vrací true, pokud je typu Array, i když <see cref="ItemCount"/> == 0.
        /// </summary>
        protected bool IsArray { get { return (this.ItemType == ItemType.Array); } }
        /// <summary>
        /// true pokud this prvek je komentářem. Tato informace pochází z pravidel tohoto prvku (this.Setting.IsComment).
        /// </summary>
        protected bool IsComment { get { return this.Setting.IsComment; } }
        /// <summary>
        /// Obsahuje true na prvku, který není komentář (<see cref="IsComment"/> == false) 
        /// a jehož typ (<see cref="ItemType"/>) je některý z typů: { Delimiter || Text || Keyword || Array }.
        /// </summary>
        protected bool IsRelevant { get { return (!this.IsComment && (this.ItemType == ItemType.Delimiter || this.ItemType == ItemType.Text || this.ItemType == ItemType.Keyword || this.ItemType == ItemType.Array)); } }
        /// <summary>
        /// Obsahuje true na prvku, který je některý z typů: { Delimiter || Text || Keyword }.
        /// </summary>
        protected bool IsSimpleText { get { ItemType it = this.ItemType; return (it == ItemType.Delimiter || ItemType == ItemType.Text || it == ItemType.Keyword); } }
        /// <summary>
        /// Jednotlivé vnořené prvky tohoto pole, pouze pokud this prvek je typu Array (<see cref="ItemType"/> == <see cref="ItemType.Array"/>).
        /// <para/>
        /// Obsahuje pouze ty prvky, které nejsou komentářové, a které nejsou Blank.
        /// </summary>
        protected ParsedItem[] RelevantItems { get { return (this._Items != null ? this._Items.Where(i => i.IsRelevant).ToArray() : null); } }
        /// <summary>
        /// Setting pro this Array, pokud this je pole.
        /// Pokud this je jednoduchý prvek, pak je zde reference na Setting jeho parenta = segment, do něhož this prvek patří.
        /// </summary>
        protected SettingSegment Setting { get; private set; }
        /// <summary>
        /// Vstupní text, který byl parsován. 
        /// Fyzicky je uložen pouze jedenkrát, v Root prvku.
        /// </summary>
        protected string SourceText { get { return this.Root._SourceText; } }
        private string _SourceText;
        /// <summary>
        /// Pozice znaku ve vstupním textu (bufferu), na které tento prvek začíná.
        /// Jde o pozici, na které začíná uvozující text prvku, pokud je prvek typu Array, pak je to pozice textu BeginText.
        /// </summary>
        protected int BeginPointer { get; private set; }
        /// <summary>
        /// Text, kterým začíná tento segment.
        /// Prvky typu Array mohou mít na začátku a na konci svůj text, který je uvozuje a uzavírá, například závorky, uvozovky, znaky blokového komentáře, atd.
        /// </summary>
        protected string BeginText { get; private set; }
        /// <summary>
        /// Text prvku, editační (=speciální znaky ze vstupního textu jsou zde uvedeny ve formě vhodné pro editaci, viz níže).
        /// U prvků typu: Blank, Delimiter, Text, Keyword jde o jejich vlastní text.
        /// U prvku typu Array je v této property čten kompletní souhrn textů: BeginText + Suma(Items.Text = rekurzivně) + EndText.
        /// <para/>
        /// Speciální znaky v této property jsou nahrazeny jejich editačním obsahem.
        /// Proto se tento <see cref="Text"/> liší od hodnoty v <see cref="TextEdit"/> (tam jsou speciální znaky uvedeny).
        /// Příklad: Mějme SQL jazyk, v němž se stringy zadávají v apostrofech: 'text'. 
        /// Pokud je uvnitř stringu obsažen apostrof, pak v se zapisuje jako dva apostrofy: 'text'' s apostrofem', takový text vstupuje do parseru.
        /// V property <see cref="TextEdit"/> prvku typu (<see cref="ItemType"/> == <see cref="ItemType.Text"/>) 
        /// uvnitř segmentu typu "Literal" (<see cref="Parent"/>.<see cref="Setting"/>) je ale daný řetězec uveden bez vnějších apostrofů, 
        /// a s jedním vnitřním apostrofem: {text' s apostrofem}.
        /// Naproti tomu v property <see cref="TextEdit"/> je uvnitř textu apostrof zdvojený: {text'' s apostrofem}.
        /// </summary>
        protected string TextEdit { get { return this.GetText(TextMode.CurrentText | TextMode.EditText | TextMode.AddBeginEnd); } }
        /// <summary>
        /// Text, kterým končí tento segment.
        /// Prvky typu Array mohou mít na začátku a na konci svůj text, který je uvozuje a uzavírá, například závorky, uvozovky, znaky blokového komentáře, atd.
        /// </summary>
        protected string EndText { get; private set; }
        /// <summary>
        /// Pozice znaku ve vstupním textu, který je prvním znakem za tímto segmentem.
        /// </summary>
        protected int EndPointer { get; private set; }
        /// <summary>
        /// Předkompilovaný setting pro this prvek.
        /// Je zde uložen pouze v segmentu typu Array, a pouze v době, kdy se do prvku přidávají další prvky, 
        /// tj. počínaje metodou <see cref="IParsedItemCreate.AddArray( int, string,SettingSegment, Parser.SettingSegmentCompiled)"/> 
        /// a konče <see cref="IParsedItemCreate.EndArray(int, string)"/>.
        /// Po proběhnutí metody <see cref="IParsedItemCreate.EndArray(int, string)"/> je <see cref="SettingCompiled"/> = null.
        /// </summary>
        protected Parser.SettingSegmentCompiled SettingCompiled { get; private set; }
        /// <summary>
        /// Vrátí text tohoto prvku, čistý, do property <see cref="Text"/>.
        /// </summary>
        /// <param name="textMode">Recept na režim výstupu textu (co do něj vzít, jak to upravit a jak to obalit)</param>
        /// <returns></returns>
        protected string GetText(TextMode textMode)
        {
            StringBuilder sb = new StringBuilder();
            this.GetText(sb, textMode);
            return sb.ToString();
        }
        /// <summary>
        /// Do daného StringBuilderu vloží text za this prvek, čistý, do property <see cref="Text"/>, plus za všechny Items
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="textMode">Recept na režim výstupu textu (co do něj vzít, jak to upravit a jak to obalit)</param>
        protected void GetText(StringBuilder sb, TextMode textMode)
        {
            if (!this.IsArray)
            {   // Prostý text:
                string text = ((textMode.HasFlag(TextMode.CurrentText) && this.ItemTextModified != null) ? this.ItemTextModified : this.ItemTextOriginal);
                if (textMode.HasFlag(TextMode.EditText))
                    text = ReverseReplaceSpecialTexts(this.Setting ?? this.Parent.Setting, text);
                sb.Append(text);
            }
            else
            {   // Begin + Suma(Items) + End:
                bool addBeginEnd = textMode.HasFlag(TextMode.AddBeginEnd);
                if (addBeginEnd && this.BeginText != null) sb.Append(this.BeginText);
                TextMode innerMode = textMode | TextMode.AddBeginEnd;          // Vnořené prvky musí mít Begin a End vždy, bez ohledu na nastavení this prvku
                foreach (ParsedItem item in this._Items)
                    item.GetText(sb, innerMode);
                if (addBeginEnd && this.EndText != null) sb.Append(this.EndText);
            }
        }
        /// <summary>
        /// Text prvku.
        /// U prvků typu: Blank, Delimiter, Text, Keyword je vkládán přímo do této proměnné a je zde pamatován.
        /// U prvku typu Array je v této property null.
        /// </summary>
        private string ItemTextOriginal;
        /// <summary>
        /// Text prvku modifikovaný. Pokud je null, pak nedošlo k modifikaci textu.
        /// U prvků typu: Blank, Delimiter, Text, Keyword je vkládán přímo do této proměnné a je zde pamatován.
        /// U prvku typu Array je v této property null.
        /// </summary>
        private string ItemTextModified;
        /// <summary>
        /// Fyzické pole prvků, které jsou v this prvku obsaženy.
        /// Pole je připraveno pouze pro prvky typu <see cref="ItemType.Array"/>, pro ostatní typy je null.
        /// </summary>
        private List<ParsedItem> _Items;
        /// <summary>
        /// Řízení výstupu textu
        /// </summary>
        [Flags]
        protected enum TextMode
        {
            /// <summary>
            /// Nezadáno, nic
            /// </summary>
            None = 0,
            /// <summary>
            /// Aktuální varianta textu = <see cref="ParsedItem.ItemTextModified"/> (tzn. text po případné editaci obsahu).
            /// Pokud text nebyl modifikován, pak tato varianta vrací originální text = <see cref="ParsedItem.ItemTextOriginal"/>.
            /// </summary>
            CurrentText = 0x01,
            /// <summary>
            /// Originální varianta textu (tzn. tak jak bylo parsováno, bez případné editace obsahu)
            /// </summary>
            OriginalText = 0x02,
            /// <summary>
            /// Formulace textu pro editaci: tzn. obsahuje speciální znaky v "editační" formě.
            /// Tedy například pro SQL jazyk: pokud je uvnitř stringu jeden apostrof, pak v editační formě jsou uvedeny dva.
            /// </summary>
            EditText = 0x10,
            /// <summary>
            /// Přidávat texty Begin a End u segmentů
            /// </summary>
            AddBeginEnd = 0x100
        }
        #endregion
        #region Protected podpora pro parsování
        /// <summary>
        /// Pokud (this.ItemType == ItemType.Array) pak vrátí řízení.
        /// Jinak vyhodí chybu.
        /// </summary>
        /// <param name="method"></param>
        protected void CheckArray(string method)
        {
            if (this.ItemType == ItemType.Array) return;
            throw new InvalidOperationException("Na třídě ParsedItem nelze provést metodu " + method + ", to lze pouze pro ItemType = Array, ale aktuální ItemType =" + this.ItemType.ToString() + "!");
        }
        /// <summary>
        /// Metoda zajistí, že pokud aktuální prvek (poslední v this._Items = <see cref="LastItem"/>) je jiného typu než bude prvek následující (parametr "nextItemType"),
        /// tak se tento aktuální prvek <see cref="LastItem"/> uzavře.
        /// Metoda se volá ze všech metod typu IParsedItemInternal.Add*() a z metody IParsedItemInternal.EndArray(), pak je itemType = <see cref="ItemType.None"/>.
        /// Metoda se volá výhradně uvnitř this prvku typu Array.
        /// Prvek <see cref="LastItem"/> může být jak jednoduchý (Text, Blank), tak složený (Array).
        /// </summary>
        /// <param name="nextItemType">Typ prvku, který bude následovat. Pokud je nextItemType == <see cref="ItemType.None"/>, pak se už nebude zadávat nic (končí segment).</param>
        protected void CloseLastItem(ItemType nextItemType)
        {
            ParsedItem lastItem = this.LastItem;
            if (lastItem != null)
                lastItem.Close(nextItemType);
        }
        /// <summary>
        /// Tato metoda má zajistit ukončení prvku this, který se až dosud zadával, 
        /// v situaci kdy se do Parent pole bude nově zadávat prvek daného typu (nextItemType).
        /// Účelem je zejména: v případě změny typu dat (this.ItemType != nextItemType) ukončit současný prvek:
        /// pro prvky typu Text detekovat, zda nejsou Keywords;
        /// pro prvky typu Array detekovat, zda se nemají konvertovat na typ Text, a zda mají mergovat Text+Dot+Space.
        /// </summary>
        /// <param name="nextItemType"></param>
        protected void Close(ItemType nextItemType)
        {
            switch (this.ItemType)
            {
                case ItemType.Blank:
                    if (nextItemType != ItemType.Blank)
                        this.CloseBlank();
                    break;

                case ItemType.Text:
                    if (nextItemType != ItemType.Text)
                        this.CloseText();
                    break;

                case ItemType.Array:
                    this.CloseArray();
                    break;
            }
        }
        /// <summary>
        /// Zajistí ukončení tohoto prvku v případě, že this.ItemType je Blank v situaci, 
        /// kdy se do Parenta bude buď vkládat nový prvek jiného typu, anebo se Parent právě ukončuje.
        /// </summary>
        protected void CloseBlank()
        {
        }
        /// <summary>
        /// Zajistí ukončení tohoto prvku v případě, že this.ItemType je Text v situaci, 
        /// kdy se do Parenta bude buď vkládat nový prvek jiného typu, anebo se Parent právě ukončuje.
        /// Tato metoda má vyhodnotit tento prvek, a pokud jeho ItemText podle aktuálního settingu odpovídá nějakému Keyword, pak přepne typ prvku z Text na Keyword.
        /// </summary>
        protected void CloseText()
        {
            if (this.Parent.SettingCompiled.IsKeyword(this.ItemTextOriginal))
                this.ItemType = ItemType.Keyword;
        }
        /// <summary>
        /// Zajistí ukončení tohoto prvku v případě, že this.ItemType je Array v situaci, 
        /// kdy se do Parenta bude buď vkládat nový prvek, anebo se Parent právě ukončuje.
        /// Tato metoda má vyhodnotit aktuální prvek s ohledem na jeho pravidla...
        /// </summary>
        protected void CloseArray()
        {
            // Při ukončení prvku typu Array se může:
            //  a) provést sloučení prvků Text Blank Dot:
            if (this.Setting.EnableMergeTextBlankDotText)
                this.MergeTextBlankDotText();
            //  b) jeho obsah konvertovat na Text:
            if (this.Setting.EnableConvertArrayToText)
                this.ConvertArrayToText();


            /*
            // Pokud this segment má nastaveno, že se mají spojovat prvky Text a Space a Dot, pak to může být i náš případ:
            if (this.Setting.EnableMergeTextSpaceDotText)
            {   // Pokud daný CurrentItem prvek (dříve segment, nyní Text) má před sebou Blank, a před ním Text, a texty lze spojit tečkou, provedeme to nyní:
                int currentIndex = currentItem.ItemIndex;
                if (this.CanMergeTextDotDot(currentIndex - 1, text))
                {   // Prvek currentItem (ještě před chvílí segment, nyní Text) může být mergován s prvkem na indexu [currentIndex - 1]:
                    MergeItems(this._Items[currentIndex - 1], currentItem);
                    this._Items.RemoveAt(currentIndex);              // Na tomto indexu je dosavadní currentItem, jehož obsah byl připojen k prvku na předešlém indexu
                }
                else if (this.CanMergeTextDotBlankDot(currentIndex - 2, text))
                {   // Prvek currentItem (ještě před chvílí segment, nyní Text) může být mergován s prvkem na indexu [currentIndex - 2], a prvek na indexu [currentIndex - 1] bude vypuštěn (je Blank):
                    MergeItems(this._Items[currentIndex - 2], currentItem);
                    this._Items.RemoveAt(currentIndex - 1);          // Na tomto indexu je prvek Blank
                    this._Items.RemoveAt(currentIndex - 1);          // Na tento index se přisunul prvek currentItem, jehož obsah byl připojen k prvku na předešlém indexu
                }
            }
            */
        }
        /// <summary>
        /// Metoda projde všechny prvky v this array, najde vhodné kandidáty na Merge (pro prvky typu Text, Blank, Dot) a spojí je do jednoho prvku Text.
        /// </summary>
        protected void MergeTextBlankDotText()
        {
            bool mergeAsterix = this.Setting.EnableMergeTextSpaceDotAsterix;

            ParsedItem firstItem = null;
            int firstIndex = -1;
            bool firstEndDot = false;
            for (int testIndex = 0; testIndex < this.ItemCount; testIndex++)
            {
                ParsedItem testItem = this._Items[testIndex];
                string testText = testItem.ItemTextOriginal;       // Pozor, někdy je null (u Array)

                if (firstItem != null)
                {   // Máme určený první prvek k mergování ?
                    bool? canMerge = null;                 // canMerge : null = přeskočíme prvek, uvidíme. false = nelze napojovat, konec mergování.  true = připojit.
                    switch (testItem.ItemType)
                    {
                        case ItemType.Blank:
                            // Ten by šel přeskočit, necháme canMerge = null...
                            break;
                        case ItemType.Delimiter:
                            // Nějaké oddělovače:
                            if (testText == ".")
                                // Tečku dovolím napojit:
                                canMerge = true;
                            else if (testText == "*" && firstEndDot && mergeAsterix)
                                // Hvězdičku napojím jen tehdy, když předešlý text končí tečkou, a je to povoleno v pravidlech:
                                canMerge = true;
                            else
                                // Jiné oddělovače zruší mergování:
                                canMerge = false;
                            break;
                        case ItemType.Text:
                            // Text napojím jen přes tečku, ale pokud to nepůjde (spojení by neproběhlo přes tečku), tak mergování přeruším:
                            bool testBeginDot = testText.StartsWith(".");
                            canMerge = (firstEndDot || testBeginDot);
                            break;
                        case ItemType.Keyword:
                            // Klíčová slova nelze mergovat:
                            canMerge = false;
                            break;
                        case ItemType.Array:
                            // Vnořená pole nelze mergovat:
                            canMerge = false;
                            break;
                    }

                    // Tak jak jsme se rozhodli?
                    if (canMerge.HasValue)
                    {   // Nějaké rozhodnutí padlo:
                        if (canMerge.Value)
                        {   // Spojit prvky firstItem a testItem, a poté mezilehlé prvky vypustit (jsou to Blank):
                            MergeItems(firstItem, testItem);
                            int removeItemCount = (testIndex - firstIndex);
                            for (int d = 0; d < removeItemCount; d++)
                                this._Items.RemoveAt(firstIndex + 1);
                            testIndex = firstIndex;                  // Ukazatel cyklu nastavím na first prvek, protože v příštím cyklu půjdeme na prvek +1.
                            // Proměnné firstItem a firstIndex neměním (správně ukazují na první prvek), ale provedu aktualizaci firstEndDot (zjistím aktuální stav tečky na konci):
                            firstEndDot = (firstItem.ItemTextOriginal.EndsWith("."));
                            // Mohlo dojít k mergování do prvního prvku typu Delimiter = ".", pak změním typ na Text:
                            if (firstItem.ItemType == ItemType.Delimiter)
                                firstItem.ItemType = ItemType.Text;
                        }
                        else
                        {   // Nespojit:
                            firstItem = null;
                            firstIndex = -1;
                            firstEndDot = false;
                            // Následně se provede další větev, která sama otestuje, zda prvek (item) na indexu [i] může být First v dalším Merge...
                        }
                    }

                }

                if (firstItem == null)
                {   // V tuto chvíli nemáme prvek, do kterého bychom napojovali => pokoušíme se jej najít:
                    if (testItem.ItemType == ItemType.Text || (testItem.ItemType == ItemType.Delimiter && testText == "."))
                    {   // Smí to být běžný Text, anebo Delimiter s obsahem "."
                        firstItem = testItem;
                        firstIndex = testIndex;
                        firstEndDot = (testText.EndsWith("."));
                    }
                }
            }
        }
        /// <summary>
        /// Převede this prvek typu Array na prvek typu Text. Ponechá mu jeho kompletní obsah (včetně BeginText a EndText).
        /// Ponechá mu i Setting, který měl v době založení jako Array.
        /// </summary>
        protected void ConvertArrayToText()
        {
            // Celý obsah prvku this máme převést na Text, a v tomto stavu ponechat v jeho Parentovi:
            // Prvek se převede na Text, ale musíme mu ponechat Setting, přinejmenším kvůli RTF kódům. 
            //  (Nicméně, pokud bude prvek Mergován to předchozího Textu, pak se jeho Setting nejspíš ztratí...)
            string text = this.Text;
            this.ItemType = ItemType.Text;
            this.ItemTextOriginal = text;
            this.ItemTextModified = text;
            this._Items = null;
        }
        /// <summary>
        /// Do this prvku (který musí být Array) přidá další prvek do pole <see cref="_Items"/>.
        /// Do prvku přenese referenci na this.Setting i this.SettingCompiled.
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="pointer"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected ParsedItem AddItem(ItemType itemType, int pointer, string text)
        {
            ParsedItem item = new ParsedItem(this, this.Setting, this.SettingCompiled, itemType);
            item.BeginPointer = pointer;
            item.EndPointer = pointer;
            item.ItemTextOriginal = text;
            item.EndPointer = item.EndPointer + text.Length;
            this._Items.Add(item);
            return item;
        }
        /// <summary>
        /// Do this prvku (který musí být Blank nebo Text) přidá daný text do <see cref="ItemTextOriginal"/>.
        /// Používá se tehdy, když např. přidáváme Blank text do pole, kde jako <see cref="LastItem"/> figuruje rovněž prvek typu Blank, a podobně pro typ Text.
        /// Další typy: Delimitery se nesčítají, Keywordy se detekují pro prvky typu Text, po jejich ukončení, a prvky Array se přidávají jinak.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="text"></param>
        protected void AddText(int pointer, string text)
        {
            this.ItemTextOriginal = this.ItemTextOriginal + text;
            this.EndPointer = this.EndPointer + text.Length;
        }
        /// <summary>
        /// Do prvního prvku z daného seznamu postupně připojí obsah ze všech dalších prvků.
        /// Střádá jejich Text a ukládá Max(EndPointer). Nekontroluje se shoda obsahu, pouze to že první prvek musí být typu Text.
        /// </summary>
        /// <param name="items"></param>
        protected void MergeItems(params ParsedItem[] items)
        {
            if (items.Length == 0) return;
            ParsedItem targetItem = items[0];
            if (targetItem.ItemType != ItemType.Text)
                throw new InvalidOperationException("Nelze provést ParsedItem.MergeItems() do cílového prvku, který není typu Text; aktuální typ je " + targetItem.ItemType.ToString());

            for (int i = 1; i < items.Length; i++)
            {   // Do prvku targetItem postupně přidám data z ostatních prvků:
                ParsedItem mergeItem = items[i];
                targetItem.ItemTextOriginal += mergeItem.Text;
                if (targetItem.EndPointer < mergeItem.EndPointer)
                    targetItem.EndPointer = mergeItem.EndPointer;
            }
        }
        /// <summary>
        /// Pokud this je typu Array, pak CurrentItem = poslední prvek v poli _Items (nebo null, když pole _Items je prázdné).
        /// Pokud this je jiného typu než Array, pak CurrentItem = null.
        /// </summary>
        protected ParsedItem LastItem { get { return (this.ItemCount > 0 ? this._Items[this._Items.Count - 1] : null); } }
        /// <summary>
        /// Obsahuje index tohoto prvku = index v poli Parent.Items[], na kterém se this prvek nyní nachází.
        /// Index je zjištěn dynamicky, vyhledáním ReferenceEquals v poli Parent._Items.
        /// Pokud this je Root, vrací se -1.
        /// </summary>
        protected int ItemIndex
        {
            get
            {
                if (this.IsRoot) return -1;
                return this.Parent._Items.FindIndex(i => Object.ReferenceEquals(i, this));
            }
        }
        #endregion
        #region Static služby
        /// <summary>
        /// Metoda vrátí textový souhrn dodaných itemů. Výsledek má formu: item[0].Text + delimiter + item[1].Text + delimiter + item[2].Text + delimiter + ... + item[n].Text.
        /// Hodnota "delimiter" = jedna mezera. Lze využít variantu s parametrem delimiterGenerator.
        /// </summary>
        /// <param name="items">Souhrn prvků ke spojení</param>
        /// <returns></returns>
        public static string Join(IEnumerable<ParsedItem> items)
        {
            return Join(items, null, TextMode.AddBeginEnd | TextMode.CurrentText);
        }
        /// <summary>
        /// Metoda vrátí textový souhrn dodaných itemů. Výsledek má formu: item[0].Text + delimiter + item[1].Text + delimiter + item[2].Text + delimiter + ... + item[n].Text.
        /// Hodnotu "delimiter" může určit funkce dodaná jako parametr delimiterGenerator, která dostává dva parametry: item před delimiterem a item za ním.
        /// </summary>
        /// <param name="items">Souhrn prvků ke spojení</param>
        /// <param name="delimiterGenerator">Generátor oddělovacího textu (mezery) mezi dvěma prvky. Pokud je null, bude se vždy vkládat jedna mezera.</param>
        /// <returns></returns>
        public static string Join(IEnumerable<ParsedItem> items, Func<ParsedItem, ParsedItem, string> delimiterGenerator)
        {
            return Join(items, delimiterGenerator, TextMode.AddBeginEnd | TextMode.CurrentText);
        }
        /// <summary>
        /// Metoda vrátí textový souhrn dodaných itemů. Výsledek má formu: item[0].Text + delimiter + item[1].Text + delimiter + item[2].Text + delimiter + ... + item[n].Text.
        /// Hodnotu "delimiter" může určit funkce dodaná jako parametr delimiterGenerator, která dostává dva parametry: item před delimiterem a item za ním.
        /// </summary>
        /// <param name="items">Souhrn prvků ke spojení</param>
        /// <param name="delimiterGenerator">Generátor oddělovacího textu (mezery) mezi dvěma prvky. Pokud je null, bude se vždy vkládat jedna mezera.</param>
        /// <param name="editFormat">true = do výstupu vložit formát "Edit" : speciální znaky budou v "editační" notaci, false = speciální znaky budou v "hodnotové" notaci</param>
        /// <returns></returns>
        public static string Join(IEnumerable<ParsedItem> items, Func<ParsedItem, ParsedItem, string> delimiterGenerator, bool editFormat)
        {
            TextMode textMode = TextMode.AddBeginEnd | TextMode.CurrentText;
            if (editFormat) textMode |= TextMode.EditText;
            return Join(items, delimiterGenerator, textMode);
        }
        /// <summary>
        /// Metoda vrátí textový souhrn dodaných itemů. Výsledek má formu: item[0].Text + delimiter + item[1].Text + delimiter + item[2].Text + delimiter + ... + item[n].Text.
        /// Hodnotu "delimiter" může určit funkce dodaná jako parametr delimiterGenerator, která dostává dva parametry: item před delimiterem a item za ním.
        /// </summary>
        /// <param name="items">Souhrn prvků ke spojení</param>
        /// <param name="delimiterGenerator">Generátor oddělovacího textu (mezery) mezi dvěma prvky. Pokud je null, bude se vždy vkládat jedna mezera.</param>
        /// <param name="textMode">Režim textu</param>
        /// <returns></returns>
        protected static string Join(IEnumerable<ParsedItem> items, Func<ParsedItem, ParsedItem, string> delimiterGenerator, TextMode textMode)
        {
            StringBuilder sb = new StringBuilder();
            if (items != null)
            {
                ParsedItem prevItem = null;
                foreach (ParsedItem item in items)
                {
                    if (prevItem != null) sb.Append(delimiterGenerator != null ? delimiterGenerator(prevItem, item) : " ");
                    item.GetText(sb, textMode);
                    prevItem = item;
                }
            }
            return sb.ToString();
        }
        #endregion
        #region IParsedItemExtended
        ParsedItem IParsedItemExtended.Root { get { return this.Root; } }
        bool IParsedItemExtended.IsRoot { get { return this.IsRoot; } }
        string IParsedItemExtended.SourceText { get { return this.SourceText; } }
        bool IParsedItemExtended.IsSimpleText { get { return this.IsSimpleText; } }
        bool IParsedItemExtended.IsArray { get { return this.IsArray; } }
        bool IParsedItemExtended.IsRelevant { get { return this.IsRelevant; } }
        bool IParsedItemExtended.IsComment { get { return this.IsComment; } }
        ParsedItem[] IParsedItemExtended.RelevantItems { get { return this.RelevantItems; } }
        SettingSegment IParsedItemExtended.Setting { get { return this.Setting; } }
        string IParsedItemExtended.TextModified { get { return this.ItemTextModified; } set { this.ItemTextModified = value; } }
        int IParsedItemExtended.BeginPointer { get { return this.BeginPointer; } }
        string IParsedItemExtended.BeginText { get { return this.BeginText; } }
        string IParsedItemExtended.TextEdit { get { return this.TextEdit; } }
        string IParsedItemExtended.EndText { get { return this.EndText; } }
        int IParsedItemExtended.EndPointer { get { return this.EndPointer; } }
        int IParsedItemExtended.ItemIndex { get { return this.ItemIndex; } }
        string IParsedItemExtended.RtfText { get { return this.RtfText; } }
        #endregion
        #region IParsedItemCreate
        ParsedItem IParsedItemCreate.AddArray(int beginPointer, string beginText, SettingSegment setting, Parser.SettingSegmentCompiled settingCompiled)
        {
            this.CheckArray("AddArray()");
            this.CloseLastItem(ItemType.Array);

            ParsedItem childItem = new ParsedItem(this, setting, settingCompiled, ItemType.Array);
            childItem.BeginPointer = beginPointer;
            childItem.BeginText = beginText;
            this._Items.Add(childItem);
            return childItem;
        }
        ParsedItem IParsedItemCreate.EndArray(int endPointer, string endText)
        {
            this.CheckArray("EndArray()");
            this.CloseLastItem(ItemType.None);                    // Zavírám poslední prvek v this.Items, což může být Text (který se tímto může přepnout na Keyword)

            this.EndPointer = endPointer;
            this.EndText = endText;

            ParsedItem parentItem = this.Parent;
            if (parentItem != null)
                parentItem.CloseLastItem(ItemType.None);          // Zavírám poslední prvek v this.Parent, což je this pole. Ale zavírá se jako celkové pole, ne jen jeho poslední prvek.

            // Vynulujeme referenci na SettingCompiled z this i z mých Items:
            //  moje Items mohou být klidně Array, ale když se ukončuji já, tak moje Childs již musí být dávno ukončeny, a nullování jejich SettingCompiled nezpůsobí problém.
            this.SettingCompiled = null;
            if (this._Items != null)
            {   // Tenhle test tu není omylem, protože metoda CloseCurrentItem pro prvek typu Array mohla převést typ prvku (tj. this) z Array na Text,
                //  kde součástí tohoto převodu je i zrušení pole Items!
                foreach (ParsedItem childItem in this._Items)
                    childItem.SettingCompiled = null;
            }

            return parentItem;
        }
        void IParsedItemCreate.AddBlank(int pointer, string text)
        {
            this.CheckArray("AddBlank()");
            this.CloseLastItem(ItemType.Blank);

            ParsedItem lastItem = this.LastItem;
            if (lastItem == null || lastItem.ItemType != ItemType.Blank)
                this.AddItem(ItemType.Blank, pointer, text);
            else
                lastItem.AddText(pointer, text);
        }
        void IParsedItemCreate.AddDelimiter(int pointer, string text)
        {
            this.CheckArray("AddDelimiter()");
            this.CloseLastItem(ItemType.Delimiter);

            this.AddItem(ItemType.Delimiter, pointer, text);
        }
        void IParsedItemCreate.AddText(int pointer, string text)
        {
            this.CheckArray("AddText()");
            this.CloseLastItem(ItemType.Text);
            // this.MergeDotSpaceText(text);

            ParsedItem lastItem = this.LastItem;
            if (lastItem == null || lastItem.ItemType != ItemType.Text)
                this.AddItem(ItemType.Text, pointer, text);
            else
                lastItem.AddText(pointer, text);
        }
        void IParsedItemCreate.ChangeTo(ItemType itemType, string text)
        {
            string textOriginal = this.Text;
            this.ItemType = itemType;
            if (text == null) text = "";
            if (this.IsArray)
            {   // Proběhla změna na Array: vytvoříme nové pole prvků (staré zahodíme), a vložíme jeden prvek typu text nebo Blank:
                this._Items = new List<ParsedItem>();
                ItemType subItemType = (!String.IsNullOrWhiteSpace(text) ? ItemType.Text : ItemType.Blank);
                this.AddItem(subItemType, this.BeginPointer, text);
            }
            else
            {   // Proběhla změna na prvek: do this prvku vepíšeme text, a zahodíme Items:
                this.ItemTextOriginal = textOriginal;
                this.ItemTextModified = text;
                this._Items = null;
            }
        }
        #endregion
    }
    /// <summary>
    /// Interface pro přístup k rozšířeným členům třídy ParsedItem
    /// </summary>
    internal interface IParsedItemExtended
    {
        /// <summary>
        /// Root položka
        /// </summary>
        ParsedItem Root { get; }
        /// <summary>
        /// true pokud this prvek je Root.
        /// Root prvek nemá Parenta.
        /// </summary>
        bool IsRoot { get; }
        /// <summary>
        /// true pokud this prvek má (nebo alespoň může mít) vnořené prvky v <see cref="ParsedItem.Items"/> (tj. pokud <see cref="ItemType"/> == <see cref="ItemType.Array"/>).
        /// Neověřuje se jejich počet, tedy HasItems vrací true, pokud je typu Array, i když <see cref="ParsedItem.ItemCount"/> == 0.
        /// </summary>
        bool IsArray { get; }
        /// <summary>
        /// true pokud this prvek je komentářem. Tato informace pochází z pravidel tohoto prvku (this.Setting.IsComment).
        /// </summary>
        bool IsComment { get; }
        /// <summary>
        /// Obsahuje true na prvku, který není komentář (<see cref="IsComment"/> == false) 
        /// a jehož typ (<see cref="ItemType"/>) je některý z typů: { Delimiter || Text || Keyword || Array }.
        /// </summary>
        bool IsRelevant { get; }
        /// <summary>
        /// Obsahuje true na prvku, který je některý z typů: { Delimiter || Text || Keyword }.
        /// </summary>
        bool IsSimpleText { get; }
        /// <summary>
        /// Jednotlivé vnořené prvky tohoto pole, pouze pokud this prvek je typu Array (<see cref="ItemType"/> == <see cref="ItemType.Array"/>).
        /// <para/>
        /// Obsahuje pouze ty prvky, které nejsou komentářové, a které nejsou Blank.
        /// </summary>
        ParsedItem[] RelevantItems { get; }
        /// <summary>
        /// Setting pro this Array, pokud this je pole.
        /// Pokud this je jednoduchý prvek, pak je zde reference na Setting jeho parenta = segment, do něhož this prvek patří.
        /// </summary>
        SettingSegment Setting { get; }
        /// <summary>
        /// Text prvku modifikovaný.
        /// Aplikace může někdy vyžadovat provedení změny v textu, v tom případě tedy najde patřičný prvek a pomocí <see cref="IParsedItemExtended.TextModified"/>
        /// do tohoto prvku vepíše modifikovaný text. Ten se poté projeví v <see cref="ParsedItem.Text"/>.
        /// Aplikace může editaci odvolat, vložením null do <see cref="IParsedItemExtended.TextModified"/>. Pak se v prvku objeví původní originální text.
        /// </summary>
        string TextModified { get; set; }
        /// <summary>
        /// Kompletní vstupní text, který byl parsován. 
        /// Fyzicky je uložen pouze jedenkrát, v Root prvku.
        /// </summary>
        string SourceText { get; }
        /// <summary>
        /// Pozice znaku ve vstupním textu (bufferu), na které tento prvek začíná.
        /// Jde o pozici, na které začíná uvozující text prvku, pokud je prvek typu Array, pak je to pozice textu BeginText.
        /// </summary>
        int BeginPointer { get; }
        /// <summary>
        /// Text, kterým začíná tento segment.
        /// Prvky typu Array mohou mít na začátku a na konci svůj text, který je uvozuje a uzavírá, například závorky, uvozovky, znaky blokového komentáře, atd.
        /// </summary>
        string BeginText { get; }
        /// <summary>
        /// Text prvku, editační (=speciální znaky ze vstupního textu jsou zde uvedeny ve formě vhodné pro editaci, viz níže).
        /// U prvků typu: Blank, Delimiter, Text, Keyword jde o jejich vlastní text.
        /// U prvku typu Array je v této property čten kompletní souhrn textů: BeginText + Suma(Items.Text = rekurzivně) + EndText.
        /// <para/>
        /// Speciální znaky v této property jsou nahrazeny jejich editačním obsahem.
        /// Proto se tento <see cref="ParsedItem.Text"/> liší od hodnoty v <see cref="TextEdit"/> (tam jsou speciální znaky uvedeny).
        /// Příklad: Mějme SQL jazyk, v němž se stringy zadávají v apostrofech: 'text'. 
        /// Pokud je uvnitř stringu obsažen apostrof, pak v se zapisuje jako dva apostrofy: 'text'' s apostrofem', takový text vstupuje do parseru.
        /// V property <see cref="TextEdit"/> prvku typu (<see cref="ItemType"/> == <see cref="ItemType.Text"/>) 
        /// uvnitř segmentu typu "Literal" (<see cref="ParsedItem.Parent"/>.<see cref="Setting"/>) je ale daný řetězec uveden bez vnějších apostrofů, 
        /// a s jedním vnitřním apostrofem: {text' s apostrofem}.
        /// Naproti tomu v property <see cref="TextEdit"/> je uvnitř textu apostrof zdvojený: {text'' s apostrofem}.
        /// </summary>
        string TextEdit { get; }
        /// <summary>
        /// Text, kterým končí tento segment.
        /// Prvky typu Array mohou mít na začátku a na konci svůj text, který je uvozuje a uzavírá, například závorky, uvozovky, znaky blokového komentáře, atd.
        /// </summary>
        string EndText { get; }
        /// <summary>
        /// Pozice znaku ve vstupním textu, který je prvním znakem za tímto segmentem.
        /// </summary>
        int EndPointer { get; }
        /// <summary>
        /// Index prvku v jeho parentovi
        /// </summary>
        int ItemIndex { get; }
        /// <summary>
        /// Obsahuje kompletní RTF text tohoto segmentu (včetně hlavičky).
        /// String, který je získán z této property, může být uložen jako soubor.rtf, nebo zobrazen v RtfTextBoxu.
        /// Upozornění: RTF text je určen k zobrazení a k editaci, tedy i k následnému opakovanému parsování.
        /// Proto RTF text musí obsahovat speciální znaky psané tak, jak jsou psány na vstupu do parsování, nikoliv tak, jak jsou v čistém textu (<see cref="ParsedItem.Text"/>).
        /// Příklad: v C# máme string "Aaaa\"Bbbb", což značí že uprostřed je uvozovka. Čistá hodnota (Text) je Aaaa"Bbbb .
        /// Do RTF textu se ale dostává hodnota včetně oddělovačů (uvozovky na okrajích), a vnitřní uvozovka se do RTF textu vepisuje shodně jako na vstupu (tj. se zpětným lomítkem).
        /// Obdobně v SQL stringu mějme string = 'Aaaa''Bbbb', čistý Text = Aaaa'Bbbb a Rtf text musí opět nést uprostřed apostrofy dva.
        /// </summary>
        string RtfText { get; }
    }
    /// <summary>
    /// Interface pro přístup k vnitřním metodám třídy ParsedItem, potřebným v době jejího vytváření
    /// </summary>
    internal interface IParsedItemCreate
    {
        /// <summary>
        /// Otevře výstup pro další prvky do nového Child prvku, který vytvoří, vloží jej do this.Items, a prvek (tedy ten vytvořený Child array) vrátí.
        /// </summary>
        /// <param name="beginPointer">Index prvního znaku, kterým tento segment začíná</param>
        /// <param name="beginText">Text, kterým tento segment začíná. Může být prázdný u automatických segmentů.</param>
        /// <param name="setting">Pravidla tohoto segmentu</param>
        /// <param name="settingCompiled">Zkompilovaná pravidla segmentu</param>
        /// <returns></returns>
        ParsedItem AddArray(int beginPointer, string beginText, SettingSegment setting, Parser.SettingSegmentCompiled settingCompiled);
        /// <summary>
        /// Ukončí výstup do aktuálního segmentu, tím jej uzavře. Další výstupy už půjdou do jeho parenta, kterého tato metoda vrací.
        /// </summary>
        /// <param name="endPointer">Index znaku, který je prvním za tímto segmentem</param>
        /// <param name="endText">Text, kterým tento segment končí</param>
        /// <returns></returns>
        ParsedItem EndArray(int endPointer, string endText);
        /// <summary>
        /// Do this pole Array, anebo do this prvku (pokud je text) přidá další text, případně sloučí texty typu "text" "blank" "text=tečka", pokud je povoleno parametrem.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="text"></param>
        void AddText(int pointer, string text);
        /// <summary>
        /// Do this pole Array, anebo do this prvku (pokud je Blank) přidá další text.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="text"></param>
        void AddBlank(int pointer, string text);
        /// <summary>
        /// Do this pole Array přidá daný Delimiter. Neslučuje texty delimiterů.
        /// Pokud některý delimiter je víceznakový, musí být tak definován, a pak je víceznakově uložen (jedním chodem).
        /// Tedy pokud v definici jsou dva delimitery ;  :  a následně je v textu nalezeno :;  tak do parsovaného pole jsou zadány dva delimitery, a ne jeden dvojznakový.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="text"></param>
        void AddDelimiter(int pointer, string text);
        /// <summary>
        /// Změní typ this prvku a jeho obsah.
        /// Ponechává index počátku i konce.
        /// Pokud dochází ke změně z Array na typ jednoduchý, pak zahazuje celý obsah pole Items.
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="text"></param>
        void ChangeTo(ItemType itemType, string text);
    }
    /// <summary>
    /// Typ obsahu v prvku ParsedItem
    /// </summary>
    internal enum ItemType
    {
        /// <summary>
        /// Neurčen
        /// </summary>
        None,
        /// <summary>
        /// Prázdné znaky.
        /// Tento prvek je členem segmentu ParsedItem.Parent, a řeší se podle jeho pravidel.
        /// Tato jeho pravidla jsou opsaná i zde v ParsedItem.Setting.
        /// </summary>
        Blank,
        /// <summary>
        /// Oddělovač.
        /// Tento prvek je členem segmentu ParsedItem.Parent, a řeší se podle jeho pravidel.
        /// Tato jeho pravidla jsou opsaná i zde v ParsedItem.Setting.
        /// </summary>
        Delimiter,
        /// <summary>
        /// Nespecifický text.
        /// Tento prvek je členem segmentu ParsedItem.Parent, a řeší se podle jeho pravidel.
        /// Tato jeho pravidla jsou opsaná i zde v ParsedItem.Setting.
        /// </summary>
        Text,
        /// <summary>
        /// Klíčové slovo.
        /// Tento prvek je členem segmentu ParsedItem.Parent, a řeší se podle jeho pravidel.
        /// Tato jeho pravidla jsou opsaná i zde v ParsedItem.Setting.
        /// </summary>
        Keyword,
        /// <summary>
        /// V tomto prvku jsou vnořeny další prvky.
        /// Tento prvek je členem segmentu ParsedItem.Parent, ale řeší se podle svých vlastních pravidel.
        /// Tato pravidla tohoto vnřeného segmentu jsou uvedena zde v ParsedItem.Setting, a mohou se lišit od pravidel Parent segmentu.
        /// </summary>
        Array
    }
    /// <summary>
    /// Význam obsahu v prvku ParsedItem z hlediska výrazu
    /// </summary>
    internal enum ItemExpressionState
    {
        /// <summary>
        /// Neurčeno, na začátku
        /// </summary>
        None = 0,
        /// <summary>
        /// Hodnota (název sloupce, tabulky, název proměnné, číselná konstanta, string, unicode string...), např. "lcs.subjekty.cislo_poradace", "@record", "'Zadaný text'", "N'Unicode text'"
        /// Podrobnější rozbor obsahu si provádí jiná metoda.
        /// </summary>
        Value,
        /// <summary>
        /// Název funkce = text, za nímž následuje závorka, např.: "COUNT(xxxxx)"
        /// </summary>
        Function,
        /// <summary>
        /// Vnořená závorka (kulatá, s libovolným obsahem, který zde neřešíme).
        /// Analýzu obsahu závorky lze provést vytvořením Expression z jejích Items.
        /// </summary>
        Parentheses,
        /// <summary>
        /// Spojítko dvou textů nebo textu a závorky nebo dvou závorek nebo spojítka a něčeho:
        /// + - * / = menší větší
        /// and or not like
        /// </summary>
        Joiner,
        /// <summary>
        /// Konec dat, nebo čárka, nebo stopword
        /// </summary>
        End
    }
    #endregion
    #region class ParsedItemBuffer : Buffer postavený nad jednotlivými prvky příkazu
    /// <summary>
    /// ParsedItemBuffer : Buffer postavený nad jednotlivými prvky příkazu.
    /// Obsahuje pole prvků <see cref="IParsedItemExtended.RelevantItems"/> = tedy těch prvků příkazu, které jsou významné z hlediska příkazu (jsou vynechány prvky Blank a Comment).
    /// <para/>
    /// Obsahuje pouze prvky <u>z hlavní linie příkazu</u>, případné vnořené příkazy jsou uzavřeny v závorkách a v hlavní linii se vyskytují jako složený objekt, nikoli jako Keywords.
    /// Buffer obsahuje i pointer <see cref="Index"/>, který ukazuje na položku, která "je nyní aktuální", jde o položku <see cref="CurrentItem"/>.
    /// <para/>
    /// Pohyb v bufferu je možno realizovat třemi způsoby:
    /// <list type="bullet">
    /// <item> a) metodou <see cref="Skip()"/> = krok o jeden prvek;</item>
    /// <item> b) nebo pomocí testu: if (<see cref="IsKeywordSkip(string[])"/>) (a podobně) = pokud na aktuální pozici je některé klíčové slovo, pak vrátí true a slovo přeskočí;</item>
    /// <item> c) nebo pomocí hledání <see cref="SearchForKeyword(string[])"/> (a podobně) = najde první slovo s daným textem a na něm zastaví;</item>
    /// <item> d) anebo nastavením hodnoty <see cref="Index"/> na konkrétní prvek.</item>
    /// </list>
    /// Buffer může být již na svém konci, to když <see cref="Index"/> ukazuje mimo pole <see cref="Items"/>: pak je (<see cref="IsEnd"/> == true).
    /// <para/>
    /// Buffer nabízí sadu metod pro hledání textů a klíčových slov: SearchFor*(), IsKeyword*(), IsSimpleText*(), atd.
    /// Nabízí i statické metody pro tvorbu indexu ze sady textu: <see cref="GetKeyDict(string[])"/>.
    /// <para/>
    /// Buffer nabízí i metody pro vyhození chyby včetně kontextu pozice v bufferu (<see cref="CheckNotEnd(string)"/>, <see cref="ExceptionWithContext(string)"/>).
    /// </summary>
    internal class ParsedItemBuffer
    {
        #region Konstrukce a základní data
        /// <summary>
        /// Konstruktor pro buffer
        /// </summary>
        /// <param name="parent"></param>
        public ParsedItemBuffer(ParsedItem parent)
        {
            this.Parent = parent;
            this.Items = ((IParsedItemExtended)parent).RelevantItems;
            this.Index = 0;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.Text; }
        /// <summary>
        /// Textové vyjádření prvku
        /// </summary>
        public string Text
        {
            get
            {
                string text = "";
                if (this.IsEnd)
                    text = "<END>";
                else
                {
                    ParsedItem currentItem = this.CurrentItem;
                    text = $"CurrentItem: \"{currentItem.Text}\" at Index: [{this.Index}]; Content: ";

                    int i = ((IParsedItemExtended)currentItem).ItemIndex;
                    ParsedItem[] items = currentItem.Parent.Items;
                    int last = items.Length - 1;

                    string textPrev = "";
                    for (int p = (i - 1); p >= 0; p--)
                    {
                        textPrev = items[p].Text + textPrev;
                        if (textPrev.Length > 40)
                        {
                            textPrev = "..." + textPrev.Substring(textPrev.Length - 36, 36);
                            break;
                        }
                    }

                    string textAfter = "";
                    for (int p = (i + 1); p <= last; p++)
                    {
                        textAfter = textAfter + items[p].Text;
                        if (textAfter.Length > 40)
                        {
                            textAfter = textAfter.Substring(0, 36) + "...";
                            break;
                        }
                    }

                    text = text + textPrev + this.CurrentItem.Text + textAfter;

                    return text;
                }
                return text;
            }
        }
        /// <summary>
        /// Parent prvek, pro všechny případy je uložen zde
        /// </summary>
        protected ParsedItem Parent { get; private set; }
        /// <summary>
        /// Pole prvků z jednoho parenta, které jsou <see cref="IParsedItemExtended.IsRelevant"/> = jsou významné z hlediska příkazu (jsou vynechány prvky Blank a Comment).
        /// </summary>
        public ParsedItem[] Items { get; private set; }
        /// <summary>
        /// Index prvku pole <see cref="Items"/>, s nímž se pracuje. Lze setovat libovolnou hodnotu.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Aktuální prvek = <see cref="Items"/>[<see cref="Index"/>].
        /// </summary>
        public ParsedItem CurrentItem { get { return (!this.IsEnd ? this.Items[this.Index] : null); } }
        /// <summary>
        /// Předchozí prvek před prvkem <see cref="CurrentItem"/>. Může být null.
        /// </summary>
        public ParsedItem PrevItem { get { return (this.HasPrevItem ? this.Items[this.Index + 1] : null); } }
        /// <summary>
        /// Obsahuje true, pokud existuje předchozí prvek před prvkem <see cref="CurrentItem"/>. Může být null.
        /// </summary>
        public bool HasPrevItem { get { return this.IsIndexExisting(this.Index - 1); } }
        /// <summary>
        /// Příští prvek za prvkem <see cref="CurrentItem"/>. Může být null.
        /// </summary>
        public ParsedItem NextItem { get { return (this.HasNextItem ? this.Items[this.Index + 1] : null); } }
        /// <summary>
        /// Obsahuje true, pokud existuje i příští prvek za prvkem <see cref="CurrentItem"/>. Může být null.
        /// </summary>
        public bool HasNextItem { get { return this.IsIndexExisting(this.Index + 1); } }
        /// <summary>
        /// Přejde na další prvek v poli <see cref="Items"/> (navýší hodnotu <see cref="Index"/> o +1).
        /// Vrací true, pokud i tento další prvek je existující = pokud po přechodu na další prvek je (<see cref="IsEnd"/> == false).
        /// Pokud to nešlo nebo pokud nyní jsme na konci, vrací false.
        /// </summary>
        /// <returns></returns>
        public bool Skip()
        {
            if (!this.IsEnd)
                this.Index = this.Index + 1;
            return !this.IsEnd;
        }
        /// <summary>
        /// Metoda posune ukazatel <see cref="Index"/> na konec dat. 
        /// Následně nelze číst data: <see cref="IsEnd"/> je true a <see cref="CurrentItem"/> je null.
        /// </summary>
        public void GoEnd()
        {
            this.Index = this.Items.Length;
        }
        /// <summary>
        /// true pokud <see cref="Index"/> NEUKAZUJE na nějaký existující prvek pole <see cref="Items"/>, false pokud dosud nejsme na konci.
        /// Tj. pokud <see cref="IsEnd"/> je false, pak <see cref="CurrentItem"/> není null.
        /// </summary>
        public bool IsEnd { get { return !this.IsIndexExisting(this.Index); } }
        /// <summary>
        /// Vrátí true, pokud daný index ukazuje na existující prvek
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected bool IsIndexExisting(int index)
        {
            return (index >= 0 && index < this.Items.Length);
        }
        #endregion
        #region Metody pro čtení dat (SearchFor..., Is...)
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v parsedItem.Items, a vrátí první pole, které obsahuje některé zadané klíčové slovo.
        /// Pokud tedy zadaný prvek (parsedItem) sám obsahuje hledané klíčové slovo ve svých Items, pak je tento vstupní prvek vrácen jako výsledek metody.
        /// Pokud zadaný prvek sám toto klíčové slovo neobsahuje, ale obsahuje nějaké vnořené pole typu SQL_PARENTHESIS (závorky), pak jsou postupně prohledávána i tato vnořená pole.
        /// Pokud počet zadaných klíčových slov == 0, pak vyhovuje kterýkoli text Keyword!
        /// </summary>
        /// <param name="parsedItem">Parsovanný prvek typu Array. Může to být root prvek vzniklý parsováním, může to být jednotlivý příkaz (první úroveň Items v Root prvku), může to být prvek typu závorka...</param>
        /// <param name="keywords">Texty klíčového slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public static ParsedItem SearchFirstArrayWithKeyword(ParsedItem parsedItem, params string[] keywords)
        {
            Dictionary<string, string> keyDict = GetKeyDict(keywords);
            return SearchFirstArrayWithKeyword(parsedItem, keyDict);
        }
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v parsedItem.Items, a vrátí první pole, které obsahuje některé zadané klíčové slovo.
        /// Pokud počet zadaných klíčových slov == 0, pak vyhovuje kterýkoli text Keyword!
        /// </summary>
        /// <param name="parsedItem">Parsovanný prvek typu Array. Může to být root prvek vzniklý parsováním, může to být jednotlivý příkaz (první úroveň Items v Root prvku), může to být prvek typu závorka...</param>
        /// <param name="keyDict">Texty klíčového slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public static ParsedItem SearchFirstArrayWithKeyword(ParsedItem parsedItem, Dictionary<string, string> keyDict)
        {
            if (parsedItem == null || parsedItem.ItemCount <= 0) return null;

            bool anyKeyword = (keyDict.Count == 0);
            string searchName1 = DefaultSettings.SQL_CODE;
            string searchName2 = DefaultSettings.SQL_PARENTHESIS;
            Stack<ParsedItem> stack = new Stack<ParsedItem>();
            stack.Push(parsedItem);
            while (stack.Count > 0)
            {
                ParsedItem item = stack.Pop();
                if (item.ItemType == ItemType.Array)
                {
                    // a) Pokud některý vnořený prvek v poli item.Items sám je hledaným klíčovým slovem, pak vrátím aktuální prvek:
                    if (item.Items.Any(i => i.ItemType == ItemType.Keyword && (anyKeyword || IsKey(i.Text, keyDict))))
                        return item;

                    // b) Tento prvek neobsahuje hledané klíčové slovo, můžeme tedy postupně prohledat jeho vnořené prvky typu Závorka:
                    List<ParsedItem> subItems = item.Items.Where(i => i.ItemType == ItemType.Array && (i.SegmentName == searchName1 || i.SegmentName == searchName2)).ToList();
                    if (subItems.Count > 0)
                    {
                        if (subItems.Count > 1)
                            subItems.Reverse();
                        foreach (ParsedItem subItem in subItems)
                            stack.Push(subItem);
                    }
                }
            }

            // Nenašli nic:
            return null;
        }
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v this bufferu, a najde první prvek, které obsahuje dané klíčové slovo.
        /// Hledá se od slova <see cref="CurrentItem"/> dopředně. Pokud se najde, pak se nalezené slovo stane prvek <see cref="CurrentItem"/> a vrací se true.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud se nenajde, pak se <see cref="Index"/> nezmění a vrací se false.
        /// </summary>
        /// <param name="keywords">Texty klíčového slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool SearchForKeyword(params string[] keywords)
        {
            Dictionary<string, string> keyDict = GetKeyDict(keywords);
            return this.SearchForKeyword(keyDict);
        }
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v this bufferu, a najde první prvek, které obsahuje dané klíčové slovo.
        /// Hledá se od slova <see cref="CurrentItem"/> dopředně. Pokud se najde, pak se nalezené slovo stane prvek <see cref="CurrentItem"/> a vrací se true.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud se nenajde, pak se <see cref="Index"/> nezmění a vrací se false.
        /// </summary>
        /// <param name="keyDict">Texty klíčového slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool SearchForKeyword(Dictionary<string, string> keyDict)
        {
            return this.ScanItems(p => (p.ItemType == ItemType.Keyword && (keyDict.Count == 0 || IsKey(p.Text, keyDict))));
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Keyword s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="keywords">Texty klíčového slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsKeyword(params string[] keywords)
        {
            Dictionary<string, string> keyDict = GetKeyDict(keywords);
            return this.IsKeyword(keyDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Keyword s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="keyDict">Texty klíčového slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsKeyword(Dictionary<string, string> keyDict)
        {
            return this.TestItem(p => (p.ItemType == ItemType.Keyword && (keyDict.Count == 0 || IsKey(p.Text, keyDict))), false);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Keyword s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="keywords">Texty klíčového slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsKeywordSkip(params string[] keywords)
        {
            Dictionary<string, string> keyDict = GetKeyDict(keywords);
            return this.IsKeywordSkip(keyDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Keyword s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Keyword!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="keyDict">Texty klíčového slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsKeywordSkip(Dictionary<string, string> keyDict)
        {
            return this.TestItem(p => (p.ItemType == ItemType.Keyword && (keyDict.Count == 0 || IsKey(p.Text, keyDict))), true);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Delimiter s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Delimiter!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="delimiters">Texty oddělovačů. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsDelimiter(params string[] delimiters)
        {
            Dictionary<string, string> delimiterDict = GetKeyDict(delimiters);
            return this.IsDelimiter(delimiterDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Delimiter s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Delimiter!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="delimiterDict">Texty oddělovačů. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsDelimiter(Dictionary<string, string> delimiterDict)
        {
            return this.TestItem(p => (p.ItemType == ItemType.Delimiter && (delimiterDict.Count == 0 || IsKey(p.Text, delimiterDict))), false);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Delimiter s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Delimiter!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="delimiters">Texty oddělovačů. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsDelimiterSkip(params string[] delimiters)
        {
            Dictionary<string, string> delimiterDict = GetKeyDict(delimiters);
            return this.IsDelimiterSkip(delimiterDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je Delimiter s některým z daných textů.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli text Delimiter!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="delimiterDict">Texty oddělovačů. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsDelimiterSkip(Dictionary<string, string> delimiterDict)
        {
            return this.TestItem(p => (p.ItemType == ItemType.Delimiter && (delimiterDict.Count == 0 || IsKey(p.Text, delimiterDict))), true);
        }
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v this bufferu, a najde první prvek, který je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Hledá se od slova <see cref="CurrentItem"/> dopředně. Pokud se najde, pak se nalezené slovo stane prvek <see cref="CurrentItem"/> a vrací se true.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud se nenajde, pak se <see cref="Index"/> nezmění a vrací se false.
        /// </summary>
        /// <param name="texts">Texty klíčového slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool SearchForSimpleText(params string[] texts)
        {
            Dictionary<string, string> textDict = GetKeyDict(texts);
            return this.SearchForSimpleText(textDict);
        }
        /// <summary>
        /// Metoda prohledá jednotlivé SQL příkazy v this bufferu, a najde první prvek, který je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Hledá se od slova <see cref="CurrentItem"/> dopředně. Pokud se najde, pak se nalezené slovo stane prvek <see cref="CurrentItem"/> a vrací se true.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud se nenajde, pak se <see cref="Index"/> nezmění a vrací se false.
        /// </summary>
        /// <param name="textDict">Texty hledaného slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool SearchForSimpleText(Dictionary<string, string> textDict)
        {
            return this.ScanItems(p => (((IParsedItemExtended)p).IsSimpleText && (textDict.Count == 0 || IsKey(p.Text, textDict))));
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="texts">Texty hledaného slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsSimpleText(params string[] texts)
        {
            Dictionary<string, string> textDict = GetKeyDict(texts);
            return this.IsSimpleText(textDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta nemění hodnotu <see cref="Index"/>.
        /// </summary>
        /// <param name="textDict">Texty hledaného slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsSimpleText(Dictionary<string, string> textDict)
        {
            return this.TestItem(p => (((IParsedItemExtended)p).IsSimpleText && (textDict.Count == 0 || IsKey(p.Text, textDict))), false);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="texts">Texty hledaného slova. Pro rychlejší opakovanou práci je vhodné používat Dictionary, viz statická metoda <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        public bool IsSimpleTextSkip(params string[] texts)
        {
            Dictionary<string, string> textDict = GetKeyDict(texts);
            return this.IsSimpleTextSkip(textDict);
        }
        /// <summary>
        /// Metoda zjistí, zda prvek <see cref="CurrentItem"/> je jednoduchý text ( Delimiter, Text, Keyword ) a obsahuje daný text.
        /// Pokud počet zadaných prvků = 0, pak vyhovuje kterýkoli jednoduchý text!
        /// Pokud ano, pak vrací true, jinak vrací false.
        /// Tato varianta navýší hodnotu <see cref="Index"/> o 1, pokud aktuální prvek podmínce vyhovuje.
        /// </summary>
        /// <param name="textDict">Texty hledaného slova. Dictionary je možno získat statickou metodou <see cref="ParsedItemBuffer.GetKeyDict(string[])"/>.</param>
        /// <returns></returns>
        public bool IsSimpleTextSkip(Dictionary<string, string> textDict)
        {
            return this.TestItem(p => (((IParsedItemExtended)p).IsSimpleText && (textDict.Count == 0 || IsKey(p.Text, textDict))), true);
        }
        /// <summary>
        /// Metoda vyhledává první prvek počínaje prvkem na aktuálním indexu <see cref="Index"/>, který vyhovuje daném podmínce.
        /// Pokud najde, nastaví jeho index do <see cref="Index"/> a vrátí true. Pokud nenajde, vrátí false a <see cref="Index"/> se nemění.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool ScanItems(Func<ParsedItem, bool> predicate)
        {
            if (this.IsEnd) return false;
            int? index = null;
            int count = this.Items.Length;
            for (int i = this.Index; i < count; i++)
            {
                if (predicate(this.Items[i]))
                {
                    index = i;
                    break;
                }
            }
            if (!index.HasValue) return false;
            this.Index = index.Value;
            return true;
        }
        /// <summary>
        /// Metoda vyhodnotí aktuální prvek (<see cref="CurrentItem"/>, na indexu indexu <see cref="Index"/>), zda vyhovuje daném podmínce.
        /// Pokud vyhovuje, a je předám parametr skip == true, pak zvýší <see cref="Index"/> o 1. 
        /// Vrací true pokud prvek podmínce vyhověl, false pokud ne.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public bool TestItem(Func<ParsedItem, bool> predicate, bool skip)
        {
            if (this.IsEnd) return false;
            bool result = predicate(this.CurrentItem);
            if (result && skip)
                this.Skip();
            return result;
        }
        /// <summary>
        /// Pokud this buffer není na konci (<see cref="IsEnd"/> == false), pak metoda nic nedělá.
        /// Pokud ale buffer je na konci, pak ohlásí chybu (Message.SysError) s textem v parametru message.
        /// </summary>
        /// <param name="message"></param>
        public void CheckNotEnd(string message)
        {
            if (!this.IsEnd) return;

        }
        /// <summary>
        /// Metoda bez podmínek vyhodí chybu (Message.SysError), vloží do ní text v délce zhruba 40 znaků počínaje aktuálním prvkem.
        /// Text bude vložen do hlášky: "Bad SQL format at position Nnn, near ...FROM sys.table as... : %2."
        /// </summary>
        /// <param name="message"></param>
        public void ExceptionWithContext(string message)
        {
            this.CheckNotEnd(message);

            ParsedItem parent = this.Parent;
            IParsedItemExtended iCurrent = this.CurrentItemExtended;
            int parentIndex = iCurrent.ItemIndex;
            ParsedItem[] items = parent.Items;
            string text = (parentIndex > 0 ? "..." : "");
            for (int i = parentIndex; i < items.Length; i++)
            {
                text += items[i].Text;
                if (text.Length > 45)
                {
                    text += "...";
                    break;
                }
            }

            throw new Application.GraphLibCodeException("Bad SQL format at position " + iCurrent.BeginPointer + ", near " + text + ": " + message + ".");
        }
        /// <summary>
        /// Obsahuje <seealso cref="CurrentItem"/> přetypovaný na <see cref="IParsedItemExtended"/>.
        /// </summary>
        protected IParsedItemExtended CurrentItemExtended { get { return this.CurrentItem as IParsedItemExtended; } }
        #endregion
        #region Metody pro čtení výrazu (Expression)
        /// <summary>
        /// Metoda načte a vrátí prvky, které počínaje aktuální pozicí představují jeden ucelený SQL výraz.
        /// Výrazem může být: konstanta, název proměnné, název sloupce, funkce se závorkou a parametry, závorka obsahující subselect, 
        /// závorka obsahující kombinaci výše uvedených, nebo i kombinace výše uvedených bez závorky.
        /// Pokud narazí na prvek (typu Delimiter, nebo Text nebo Keyword), jehož text je zadán v soupisu v paramertru stopWords, pak skončí (a <see cref="CurrentItem"/> bude ukazovat na tento prvek).
        /// Metoda může vrátit null, pokud nenajde nic.
        /// </summary>
        /// <param name="stopWords"></param>
        /// <returns></returns>
        public ParsedItem[] GetExpression(params string[] stopWords)
        {
            Dictionary<string, string> stopDict = GetKeyDict(stopWords);
            return this.GetExpression(null, stopDict);
        }
        /// <summary>
        /// Metoda načte a vrátí prvky, které počínaje aktuální pozicí představují jeden ucelený SQL výraz.
        /// Výrazem může být: konstanta, název proměnné, název sloupce, funkce se závorkou a parametry, závorka obsahující subselect, 
        /// závorka obsahující kombinaci výše uvedených, nebo i kombinace výše uvedených bez závorky.
        /// Pokud narazí na prvek (typu Delimiter, nebo Text nebo Keyword), jehož text je zadán v soupisu v paramertru stopWords, pak skončí (a <see cref="CurrentItem"/> bude ukazovat na tento prvek).
        /// Metoda může vrátit null, pokud nenajde nic.
        /// </summary>
        /// <param name="joinerDict">Texty, které reprezentují spojovací prvky mezi textovými výrazy. Defaultní obsah je DefaultExpressionJoinerDict { "+", "-", "*", "/", "=", "&lt;", "&gt;", "&lt;=", "&gt;=", "&lt;&gt;" }</param>
        /// <param name="stopDict"></param>
        /// <returns></returns>
        public ParsedItem[] GetExpression(Dictionary<string, string> joinerDict, Dictionary<string, string> stopDict)
        {
            if (joinerDict == null) joinerDict = DefaultExpressionJoinerDict;
            List<ParsedItem> result = new List<ParsedItem>();
            ItemExpressionState currentState = ItemExpressionState.None;
            while (true)
            {
                if (this.IsEnd) break;
                if (!this.IsAllowedAddCurrentItemToExpression(ref currentState, joinerDict, stopDict)) break;
                this.CurrentItem.ExpressionState = currentState;
                result.Add(this.CurrentItem);
                this.Skip();
            }

            return (result.Count > 0 ? result.ToArray() : null);
        }
        /// <summary>
        /// Metoda vrátí oddělovač, který je vhodné vložit mezi dva výrazy, 
        /// při jejich Joinování (<see cref="ParsedItem.Join( IEnumerable{ParsedItem},Func{ParsedItem, ParsedItem, string})"/>).
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <param name="joinerDict">Texty, které reprezentují spojovací prvky mezi textovými výrazy. 
        /// Pokud je joinerDict == null, pak se v textu nerozpoznají spojovací slova. 
        /// Lze použít defaultní soupis spojovacích slov, viz <see cref="DefaultExpressionJoinerDict"/>.</param>
        /// <returns></returns>
        public static string GetExpressionDelimiter(ParsedItem item1, ParsedItem item2, Dictionary<string, string> joinerDict)
        {
            string delimiter = " ";
            // Budu řešit jen situaci, kdy mezeru NECHCI vložit:
            if ((item1.ItemType == ItemType.Text || item1.ItemType == ItemType.Keyword) &&
                (joinerDict == null || !IsKey(item1.Text, joinerDict)) &&
                (item2.ItemType == ItemType.Array && item2.SegmentName == DefaultSettings.SQL_PARENTHESIS))
                // Pokud mám kombinaci: "Text nebo Keyword" (ale ne Joiner) a "Vložená závorka", pak nedám mezeru = vypadá to na funkci a její parametry:
                delimiter = "";
            return delimiter;
        }
        /// <summary>
        /// Implicitní slovník textů, které představují spojovací prvky mezi textovými výrazy. 
        /// Obsahuje texty: { "+", "-", "*", "/", "=", "&lt;", "&gt;", "&lt;=", "&gt;=", "&lt;&gt;" }
        /// </summary>
        public static Dictionary<string, string> DefaultExpressionJoinerDict { get { return GetKeyDict("+", "-", "*", "/", "=", "<", ">", "<=", ">=", "<>"); } }
        /// <summary>
        /// Vrátí true, pokud daná položka se může přidat ke vznikajícímu výrazu
        /// </summary>
        /// <param name="outerState">Vnější stav: na vstupu ten předchozí, na výstupu stav platný po aktuálním prvku</param>
        /// <param name="joinerDict">Slovník joinerů</param>
        /// <param name="stopDict">Slovník klíčových slov, lze jej vytvořit metodou <see cref="GetKeyDict(IEnumerable{string})"/></param>
        /// <returns></returns>
        private bool IsAllowedAddCurrentItemToExpression(ref ItemExpressionState outerState, Dictionary<string, string> joinerDict, Dictionary<string, string> stopDict)
        {
            ItemExpressionState prevState = outerState;
            if (this.CurrentItem == null) return false;

            // Zjistíme stav aktuálního prvku
            ItemExpressionState currState = GetItemState(this.CurrentItem, this.NextItem, joinerDict, stopDict);
            if (currState != ItemExpressionState.End)
            {   // Jednotlivé kombinace prvků po sobě jdoucích a jejich přípustnost z hlediska složení do jednoho výrazu:
                switch (prevState)
                {   // Stav před aktuálním prvkem:
                    case ItemExpressionState.None:
                        // Před aktuálním prvkem jsme byli na začátku => a na začátku je přípustné vše:
                        outerState = currState;
                        return true;
                    case ItemExpressionState.Value:
                        // Před aktuálním prvkem jsme měli prvek typu Value (nikoli funkce, to znamená že nyní NEBUDE závorka) => povolené pokračování výrazu je pouze JOINER:
                        if (currState == ItemExpressionState.Joiner)
                        {
                            outerState = currState;
                            return true;
                        }
                        break;
                    case ItemExpressionState.Function:
                        // Před aktuálním prvkem jsme měli prvek typu Function => povolené pokračování výrazu je pouze ZÁVORKA:
                        if (currState == ItemExpressionState.Parentheses)
                        {
                            outerState = currState;
                            return true;
                        }
                        break;
                    case ItemExpressionState.Parentheses:
                        // Před aktuálním prvkem jsme měli prvek typu Parentheses => povolené pokračování výrazu je pouze JOINER:
                        if (currState == ItemExpressionState.Joiner)
                        {
                            outerState = currState;
                            return true;
                        }
                        break;
                    case ItemExpressionState.Joiner:
                        // Před aktuálním prvkem jsme měli prvek typu Joiner => povolené pokračování výrazu za ním může být jakékoli (Value, Function, Závorka, další Joiner):
                        outerState = currState;
                        return true;
                }
            }
            // Když jsme došli až sem, pak aktuální prvek NENÍ POVOLENO napojit:
            outerState = ItemExpressionState.End;
            return false;
        }
        /// <summary>
        /// Vrátí charakter daného prvku (currItem) z hlediska SQL výrazu.
        /// </summary>
        /// <param name="currItem">Daný aktuální prvek, může být null (to je konec!)</param>
        /// <param name="nextItem">Prvek následující za currItem, může být null (to ale není konec z hlediska currItem)</param>
        /// <param name="joinerDict">Slovník joinerů</param>
        /// <param name="stopDict">Slovník klíčových slov: pokud daný item má dané klíčové slovo, pak se vrací stav End. Slovník lze vytvořit metodou <see cref="GetKeyDict(IEnumerable{string})"/></param>
        /// <returns></returns>
        private static ItemExpressionState GetItemState(ParsedItem currItem, ParsedItem nextItem, Dictionary<string, string> joinerDict, Dictionary<string, string> stopDict)
        {
            if (currItem == null) return ItemExpressionState.End;

            switch (currItem.ItemType)
            {
                case ItemType.Delimiter:
                case ItemType.Text:
                case ItemType.Keyword:
                    string text = currItem.Text.ToLower();
                    if (currItem.ItemType == ItemType.Keyword && IsKey(text, stopDict)) return ItemExpressionState.End;       // Stop KeyWord
                    if (IsKey(text, joinerDict)) return ItemExpressionState.Joiner;
                    // Není to ani StopWord, ani Joiner: může to být Value nebo Function.
                    // Function je to tehdy, když existuje následující prvek (nextItem), a je to Array jménem SQL_PARENTHESIS:
                    if (nextItem != null && nextItem.ItemType == ItemType.Array && nextItem.SegmentName == DefaultSettings.SQL_PARENTHESIS) return ItemExpressionState.Function;
                    // Musí to tedy být nějaká hodnota (konstanta, proměnná, název sloupce, atd):
                    return ItemExpressionState.Value;

                case ItemType.Array:
                    if (currItem.SegmentName == DefaultSettings.SQL_PARENTHESIS) return ItemExpressionState.Parentheses;
                    if (currItem.SegmentName == DefaultSettings.SQL_STRING) return ItemExpressionState.Value;
                    if (currItem.SegmentName == DefaultSettings.SQL_STRING_UNICODE) return ItemExpressionState.Value;
                    return ItemExpressionState.End;

            }
            return ItemExpressionState.End;
        }
        #endregion
        #region Metody pro dohledání komentářů (komentáře nejsou sice přítomny v bufferu, ale buffer je přesto dokáže najít)
        /// <summary>
        /// Voláním této metody si buffer zapamatuje aktuální pozici, od které bude následně vyhledávat konentáře v metodě <see cref="GetComments()"/>.
        /// Před prvním voláním se hledá od první pozice.
        /// </summary>
        public void SetCommentsBegin()
        {
            this.FirstItemToComment = this.CurrentItem;
        }
        /// <summary>
        /// Metoda najde a vrátí pole všech komentářů, které se nacházejí mezi prvkem aktivním v době volání <see cref="SetCommentsBegin()"/> a prvkem aktuálním <see cref="CurrentItem"/>.
        /// Poznámka: pole "items" komentáře neobsahuje, protože toto pole obsahuje <see cref="ParsedItem.RelevantItems"/> a v něm komentáře nejsou.
        /// Komentáře se vyhledají v poli Parent.Items.
        /// Pokud se žádné komentáře nenajdou, je výstupem této metody null.
        /// Pokud se najdou, jsou uloženy odděleně ve vráceném poli, ale bez krajních textů (/* a */ nebo --).
        /// </summary>
        /// <returns></returns>
        public string[] GetComments()
        {
            return this.GetComments(true);
        }
        /// <summary>
        /// Metoda najde a vrátí pole všech komentářů, které se nacházejí mezi prvkem aktivním v době volání <see cref="SetCommentsBegin()"/>
        /// a prvkem aktuálním <see cref="CurrentItem"/>.
        /// Poznámka: pole <see cref="Items"/> komentáře neobsahuje, protože toto pole obsahuje <see cref="ParsedItem.RelevantItems"/> a v něm komentáře nejsou.
        /// Komentáře se vyhledají v poli Parent.Items.
        /// Pokud se žádné komentáře nenajdou, je výstupem této metody null.
        /// Pokud se najdou, jsou uloženy jako stringy, jednotlivé, odděleně ve vráceném poli, ale bez krajních textů komentářů = načtené z <see cref="ParsedItem.TextInner"/>.
        /// </summary>
        /// <param name="setCurrentBegin">true = nastavit aktuální pozici jako výchozí pro další sadu komentářů (jako by se na konci volala metoda <see cref="SetCommentsBegin"/>)</param>
        /// <returns></returns>
        public string[] GetComments(bool setCurrentBegin)
        {
            ParsedItem[] items = this.Parent.Items;

            // Odkud, Kam:
            ParsedItem relevantItem1 = this.FirstItemToComment;
            int i1 = (relevantItem1 != null ? ((IParsedItemExtended)relevantItem1).ItemIndex : 0);
            ParsedItem relevantItem2 = this.CurrentItem;
            int i2 = (relevantItem2 != null ? ((IParsedItemExtended)relevantItem2).ItemIndex : items.Length);

            // Najdeme komentáře:
            List<string> result = new List<string>();
            for (int i = i1; i < i2; i++)
            {
                ParsedItem item = items[i];
                if (((IParsedItemExtended)item).IsComment)
                {
                    string comment = item.TextInner;
                    if (!String.IsNullOrEmpty(comment))
                        result.Add(comment);
                }
            }

            if (setCurrentBegin)
                this.SetCommentsBegin();

            // Vrátíme komentáře (nebo nic):
            return (result.Count > 0 ? result.ToArray() : null);
        }
        /// <summary>
        /// První prvek, který je IsRelevant, za kterým se začnou hledat komentáře
        /// </summary>
        protected ParsedItem FirstItemToComment { get; set; }
        #endregion
        #region Podpora pro Dictionary
        /// <summary>
        /// Vrátí Dictionary z dodaných klíčových slov
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetKeyDict(params string[] keys)
        {
            return (keys == null ? null : keys.ToDictionary(t => t.Trim().ToLower()));
        }
        /// <summary>
        /// Vrátí Dictionary z dodaných klíčových slov
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetKeyDict(IEnumerable<string> keys)
        {
            return keys.ToDictionary(t => t.Trim().ToLower());
        }
        /// <summary>
        /// Vrátí true pokud daný text je přítomen ve slovníku slov.
        /// Pokud je slovo prázdné, nebo slovník je null nebo prázdný, pak vrací false = slovo není ve slovníku.
        /// </summary>
        /// <param name="text">Hledaný text</param>
        /// <param name="keyDict">Slovník klíčových slov, lze jej vytvořit metodou <see cref="GetKeyDict(IEnumerable{string})"/></param>
        /// <returns></returns>
        protected static bool IsKey(string text, Dictionary<string, string> keyDict)
        {
            return (!String.IsNullOrEmpty(text) && keyDict != null && keyDict.Count > 0 && keyDict.ContainsKey(text.Trim().ToLower()));
        }
        #endregion
    }
    #endregion
    #region class ParsedItemScanArray : Třída umožňující provádět sekvenční scanování v nativním pořadí bez použití rekurzivního volání scanovací metody, jen s použitím Stack zásobníku
    /// <summary>
    /// ParsedItemScanArray : Třída umožňující provádět sekvenční scanování v nativním pořadí bez použití rekurzivního volání scanovací metody, jen s použitím Stack zásobníku
    /// </summary>
    internal class ParsedItemScanArray
    {
        #region Scanner
        /// <summary>
        /// Metoda projde všechny prvky daného pole, a každý prvek předá do nalezené scanovací metody.
        /// </summary>
        /// <param name="relevantItems"></param>
        /// <param name="scanAction"></param>
        public static void ScanRelevantItems(ParsedItem[] relevantItems, Action<ParsedItemScanArray> scanAction)
        {
            Stack<ParsedItemScanArray> stack = new Stack<ParsedItemScanArray>();
            stack.Push(new ParsedItemScanArray(relevantItems, 0));
            while (stack.Count > 0)
            {
                ParsedItemScanArray scanArray = stack.Pop();
                while (!scanArray.IsEnd)                             // Dokud máme nějaké nezpracované Child prvky
                {
                    bool exit = false;
                    ParsedItem currentItem = scanArray.CurrentItem;  // Obsahuje prvek z aktuální pozice, neposouvá index aktuální pozice
                    IParsedItemExtended iCurrentItem = currentItem as IParsedItemExtended;
                    if (iCurrentItem.IsArray)
                    {   // Aktuální Child je nějaké Array:
                        switch (currentItem.SegmentName)
                        {
                            case DefaultSettings.SQL_PARENTHESIS:
                                // Do hloubky procházím jen Závorky:
                                stack.Push(scanArray);               // Do zásobníku vložím aktuální scanItem, vrátíme se k němu až poté, co projdeme Child prvky aktuálního prvku
                                stack.Push(new ParsedItemScanArray(iCurrentItem.RelevantItems, scanArray.Level + 1));     // Do zásobníku přidám nový prvek pro Relevant pole prvků z childItem, toto pole budu scanovat v příštím cyklu od jeho indexu [0]
                                exit = true;                         // Opustím smyčku scanování while (scanArray.IsEnd), abych mohl ze zásobníku (stack) vyjmout nový prvek child...
                                break;
                            case DefaultSettings.SQL_STRING:
                            case DefaultSettings.SQL_STRING_UNICODE:
                                // String je z tohoto pohledu prvek typu Simple:
                                scanAction(scanArray);
                                break;
                                // Ostatní typy Array (což jsou komentáře) neprocházíme.
                        }
                    }
                    else
                    {
                        if (iCurrentItem.IsSimpleText)
                            // Běžný textový prvek předáme k vyhodnocení:
                            scanAction(scanArray);
                    }
                    scanArray.Skip();                                // Přejdeme na další prvek v našem scanovaném poli, i když toto pole možná už neprocházíme (je ve Stacku a je nastaven exit => break)...
                    if (exit) break;
                }
            }
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="items"></param>
        /// <param name="level">Hladina vnoření, počínaje 0</param>
        protected ParsedItemScanArray(ParsedItem[] items, int level)
        {
            this.Items = items;
            this.Index = 0;
            this.Level = level;
        }
        #endregion
        #region Data jednoho lineárního pole
        /// <summary>
        /// Pole prvků k scanování
        /// </summary>
        public ParsedItem[] Items { get; private set; }
        /// <summary>
        /// Hladina vnoření, počínaje 0
        /// </summary>
        public int Level { get; private set; }
        /// <summary>
        /// Počet prvků v poli Items
        /// </summary>
        public int Count { get { return (this.Items != null ? this.Items.Length : 0); } }
        /// <summary>
        /// Index prvku v poli <see cref="Items"/>, který se bude prohlížet v dalším kroku. Výchozí hodnota = 0.
        /// Jakmile bude <see cref="Index"/> mimo rozsah počtu prvků pole, bude <see cref="IsEnd"/> == true.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Vrátí prvek v dané relativní vzdálenosti od aktuálního prvku.
        /// Pokud na daném offsetu prvek není, pak vrátí null.
        /// Prvky zde obsažené jsou pouze takové, které mají význam z hlediska analýzy (tzn. jde o prvky <see cref="IParsedItemExtended.IsRelevant"/>).
        /// Pole obsahuje prvky typu Delimiter, Text, Keyword, Array (typu Závorka a String).
        /// Pole neobsahuje prvky typu Blank a Komentáře.
        /// Je tedy jistota, že při požadavku na prvek na offsetu -1 se vrátí skutečně předchozí prvek s významovým obsahem, a ne komentář nebo mezera.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public ParsedItem GetItemRelative(int offset)
        {
            int i = this.Index + offset;
            return ((i >= 0 && i < this.Count) ? this.Items[i] : null);
        }
        /// <summary>
        /// Aktuální prvek
        /// </summary>
        public ParsedItem CurrentItem { get { return (!this.IsEnd ? this.Items[this.Index] : null); } }
        /// <summary>
        /// true pokud jsme na konci s položkami, v tomto případě prvek <see cref="CurrentItem"/> obsahuje null.
        /// false pokud máme položky, a prvek <see cref="CurrentItem"/> obsahuje aktuální položku.
        /// </summary>
        public bool IsEnd { get { return (this.Index < 0 || this.Items == null || this.Index >= this.Items.Length); } }
        /// <summary>
        /// Metoda přejde na index + 1.
        /// </summary>
        /// <returns></returns>
        public void Skip()
        {
            if (this.Items != null && this.Index < this.Items.Length)
                this.Index = this.Index + 1;
        }
        #endregion
    }
    #endregion
    #region class Setting a SettingSegment a SpecialTexts : Předpis pro parsování jednoho konkrétního formátu (Setting = jeden jazyk a SettingSegment = všechna jeho pravidla)
    /// <summary>
    /// Setting : Předpis pro parsování jednoho konkrétního formátu (jeden jazyk a všechna jeho pravidla).
    /// Obsahuje sadu definic segmentů.
    /// Segment je například string, závorka, komentář, text.
    /// Definice segmentů viz třída <see cref="SettingSegment"/>.
    /// </summary>
    internal class Setting
    {
        #region Konstrukce a základní property
        /// <summary>
        /// Konstruktor pro settings
        /// </summary>
        /// <param name="name">Jméno settingu, informativní text</param>
        /// <param name="initialSegmentName">Název segmentu, který očekáváme jako první. Bývá to segment typu "standardní text" (neměl by to být text typu například Řetězec, který je uzavřený do uvozovek).</param>
        public Setting(string name, string initialSegmentName)
        {
            this.Name = name;
            this.InitialSegmentName = initialSegmentName;
            this._Segments = new List<SettingSegment>();
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
        public IEnumerable<SettingSegment> Segments { get { return this._Segments; } }
        private List<SettingSegment> _Segments;
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
        #region Přidávání a vyhledání
        /// <summary>
        /// Do nastavení segmentů přidá další položku
        /// </summary>
        /// <param name="segmentSetting"></param>
        public void SegmentSettingAdd(SettingSegment segmentSetting)
        {
            segmentSetting.Parent = this;
            this._Segments.Add(segmentSetting);
        }
        /// <summary>
        /// Vrátí segment daného jména
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SettingSegment GetSegment(string name)
        {
            return this.Segments.FirstOrDefault(s => s.SegmentName == name);
        }
        #endregion
    }
    /// <summary>
    /// SettingSegment : Popisek jednoho segmentu (jedna část parsovaného kódu, pro kterou platí jedno pravidlo).
    /// Jeden segment je například: string, komentář, závorka, atd
    /// Zde se definuje: jak se segment jmenuje, čím začíná, čím končí, co vnořeného může obsahovat...
    /// </summary>
    internal class SettingSegment : ISettingSegmentInternal
    {
        #region Konstrukce a základní property
        /// <summary>
        /// Konstruktor definice segmentu daného jména
        /// </summary>
        /// <param name="segmentName">Jméno segmentu</param>
        public SettingSegment(string segmentName)
        {
            this._Parent = null;
            this.SegmentName = segmentName;
            this.FormatCodePairDict = new Dictionary<FormatCodeKey, FormatCodePair>();
        }
        /// <summary>
        /// Konstruktor definice segmentu daného jména.
        /// Opíše do sebe veškerá data (kromě SegmentName) z dodaného vzoru. Lze tak snadno vytvořit setting podobný settingu jež existujícímu.
        /// </summary>
        /// <param name="segmentName">Jméno segmentu</param>
        /// <param name="original">Vzorový setting</param>
        public SettingSegment(string segmentName, SettingSegment original)
        {
            this._Parent = null;
            this.SegmentName = segmentName;
            this.BeginWith = original.BeginWith;
            if (original.Blanks != null) this.Blanks = original.Blanks.ToArray();
            if (original.Delimiters != null) this.Delimiters = original.Delimiters.ToArray();
            this.EndOutside = original.EndOutside;
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
        public Setting Parent
        {
            get { return this._Parent; }
            set
            {
                if (this._Parent != null)
                    throw new InvalidOperationException("Nelze změnit hodnotu SettingSegment.Parent poté, kdy byla jednou přiřazena.");
                this._Parent = value;
            }
        }
        private Setting _Parent;
        /// <summary>
        /// Název segmentu, klíčové slovo
        /// </summary>
        public string SegmentName { get; private set; }
        /// <summary>
        /// true pokud this segment je komentářem.
        /// </summary>
        public bool IsComment { get; set; }
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
        /// </summary>
        public string[] EndWith { get; set; }
        /// <summary>
        /// true: Oddělovače na konci tohoto segmentu (this.EndWith) se nevkládají do konce segmentu (Segment.End), 
        /// ale zpracují se jako znaky následující za segmentem.
        /// Typicky jde o konec řádkového komentáře (Cr, Lf), které nejsou součástí komentáře, ale jde o standardní znak konce řádku v parent segmentu.
        /// </summary>
        public bool EndOutside { get; set; }
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
        public SpecialTexts[] SpecialTexts { get; set; }
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
        /// {Text1} {Blank} {Text = "."} {Blank} {Text2}
        /// Jinými slovy: při zahájení hodnoty Text2 se otestuje, zda předchozí hodnoty nejsou uvedené konfigurace
        /// (přičemž Blank jsou nepovinné, a teček může být více za sebou).
        /// Pokud ano, tak se předcházející Blank odeberou, texty "." se shrnou a vše se přidá k předchozímu Text1.
        /// Zjednodušeně: SQL jazyk dovoluje psát názvy objektů ve formě "lcs . tabulka . sloupec", což je následně nedetekovatelné, 
        /// protože texty jsou uloženy v oddělených Values.
        /// Tento EnableMergeTextSpaceDotText zajistí, že se tato specialita vyřeší již při parsování textu.
        /// </summary>
        public bool EnableMergeTextBlankDotText { get; set; }
        /// <summary>
        /// Příznak, který povoluje připojování hvězdičky za text, pokud text končí tečkou.
        /// Opět se řeší SQL formát, kdy je povoleno zadat tabulka . *, ale pro analýzu textu je vhodné mít text vcelku: "tabulka.*"
        /// [ Já vím, že "hvězdička" se překládá jako "asterisk" a ne "Asterix", ale to by Obelix ani Panoramix nepochopili :-) ]
        /// </summary>
        public bool EnableMergeTextSpaceDotAsterix { get; set; }
        /// <summary>
        /// true, pokud tento segment po svém ukončení má být převeden na nestrukturovaný Text.
        /// <para/>
        /// Význam to má u segmentu pro jazyk SQL, typ segmentu SQL_SYSNAME, kde tento segment začíná [ a končí ], uvnitř obsahuje libovolný text a žádné vnořené segmenty.
        /// Jeho obsahem je text, který nesmí být dělen delimitery ani blank prvky, a z hlediska kódu má být chápán jako prostý text.
        /// Příklad: ve vstupním textu " SELECT * FROM lcs.[main table]" je text "[main table]" formálně načítán jako segment typu SQL_SYSNAME,
        /// protože bez tohoto ošetření by byl načten jako text "main", potom Blank " " a poté další text "table". Jako SQL_SYSNAME je načten vcelku.
        /// Ale z hlediska kódu se na tento text máme dívat jako na prostý text "[main table]", nikoli jako na vnořený segment.
        /// Proto pro tento typ segmentu nastavíme StoreSegmentAsText = true.
        /// <para/>
        /// Poznámka: detekce Keywords v tomto segmentu proběhne podle definice Keywords ještě před převodem na Text, tedy podle definice segmentu typicky SQL_SYSNAME, 
        /// nikoli podle definice segmentu Parent. Pokud tedy zdejší segment neobsahuje definice Keywords, pak zdejší texty nebudou detekovány jako Keywords. 
        /// A naopak, pokud parent segment obsahuje definice Keywords, pak i kdyby zdejší text odpovídal některému klíčovému slovu z parent segmentu, 
        /// pak zdejší text NEBUDE označen jako Keywords.
        /// <para/>
        /// <see cref="EnableConvertArrayToText"/> a RTF formátování: přestože zdejší text bude při ukončení svého segmentu (this) převeden na prvek typu Text v rámci svého Parenta,
        /// tak i přesto si v sobě ponechává referenci na zdejší <see cref="SettingSegment"/>. 
        /// Při tvorbě RTF textu pak prvek ParsedItem vkládá RTF formátovací znaky ze svého <see cref="SettingSegment"/>, ale jen pro prvek typu Text.
        /// Má tedy význam nastavit RTF kódy jen pro Text, nemá smysl řešit RTF kódy pro jiné typy prvků.
        /// A pokud navíc bude v parent prvku nastaveno <see cref="EnableMergeTextBlankDotText"/> = true, pak se Setting tohoto prvku Mergováním ztratí...
        /// </summary>
        public bool EnableConvertArrayToText { get; set; }
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
        public void AddFormatCodes(ItemType valueType, FormatCode[] codeSet, FormatCode[] codeReset)
        {
            this.AddFormatCodes(new FormatCodePair(valueType, codeSet, codeReset));
        }
        /// <summary>
        /// Přidá do segmentu deklaraci formátovacích kódů použitých na začátku určité hodnoty, dané názvem.
        /// </summary>
        /// <param name="valueName">Název páru. Typ hodnoty (ValueType) je pro tuto položku nastaven na ItemType.Text.</param>
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
        /// <param name="formatCodePair"></param>
        protected void AddFormatCodes(FormatCodePair formatCodePair)
        {
            if (this.FormatCodePairDict.ContainsKey(formatCodePair))
                throw new System.ArgumentException("Definice formátovacích kódů pro klíč " + formatCodePair.ToString() + " již v definici segmentu " + this.SegmentName + " existuje. Nelze přidat duplicitní položku.");
            this.FormatCodePairDict.Add(formatCodePair, formatCodePair);
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
            if (this.FormatCodePairDict.TryGetValue(new FormatCodeKey(ItemType.Text, null), out pair))
                return pair;
            return null;
        }
        /// <summary>
        /// Dictionary obsahující FormatCodePair. Obsahuje je jak v Key (hledá se podle nich) tak ve Values = reálně uložené hodnoty.
        /// </summary>
        protected Dictionary<FormatCodeKey, FormatCodePair> FormatCodePairDict { get; private set; }
        #endregion
        #region ISettingSegmentInternal
        FormatCodePair[] ISettingSegmentInternal.RtfCodes { get { return this.FormatCodePairDict.Values.ToArray(); } }
        FormatCodePair ISettingSegmentInternal.GetRtfCode(ItemType itemType)
        {
            FormatCodePair pair;
            FormatCodeKey key = new FormatCodeKey(itemType, (string)null);
            if (this.FormatCodePairDict.TryGetValue(key, out pair))
                return pair;
            return null;
        }
        #endregion
    }
    /// <summary>
    /// Interface pro vnější přístup k vnitřním členům třídy SettingSegment
    /// </summary>
    internal interface ISettingSegmentInternal
    {
        /// <summary>
        /// Pole všech RTF kódů v this segmentu.
        /// Používá se typicky při modifikaci obsažených RTF kódů (CodeSet, CodeReset).
        /// Touto cestou nelze přidat/odebrat celé definice ani změnit jejich klíče (Type, Name).
        /// </summary>
        FormatCodePair[] RtfCodes { get; }
        /// <summary>
        /// Metoda vrátí RtfCodePair (obsahuje RTF kódy pro začátek bloku: CodeSet a pro konec bloku: CodeReset), pro daný typ prvku (itemType) v rámci tohoto segmentu.
        /// Může vrátit null.
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        FormatCodePair GetRtfCode(ItemType itemType);
    }
    /// <summary>
    /// SpecialTexts : Speciální bloky textu a jejich konverze.
    /// <para>
    /// Speciální blok je typicky nějaká escape sekvence. Jde o posloupnost znaků, kdy některé znaky uvnitř posloupnosti se nemají chápat jako například oddělovače, ale jako znak.
    /// Příklad: pokud znak uvozovka ohraničuje řetězec (v EndWith je uvedena uvozovka), ale přitom je povoleno použít dvojici znaků (zpětné lomítko)(uvozovka) která reprezentuje uvozovku uvnitř řetězce, pak jde o speciální blok.
    /// Druhý znak z této dvojice tedy neukončuje řetězec. Celý dvojznak má význam jedné uvozovky uvnitř řetězce.
    /// </para>
    /// <para>
    /// Speciální text se tedy zadává jako dvojice řetězců: vstupní (jak je uveden v parsovaném textu) a výstupní (jak je text vepisován do výstupního segmentu).
    /// </para>
    /// </summary>
    internal class SpecialTexts
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="editorText">Text v té formě, v níž se nachází v editoru</param>
        /// <param name="valueText">Text v té formě, ve které je chápán kódem</param>
        public SpecialTexts(string editorText, string valueText)
        {
            this.EditorText = editorText;
            this.ValueText = valueText;
        }
        /// <summary>
        /// Text v té formě, v níž se nachází v editoru.
        /// Pokud bude nalezen tento text, značí to speciální text.
        /// V C# například \" značí jednu uvozovku, \r značí CR.
        /// V SQL například '' značí '
        /// </summary>
        public string EditorText { get; private set; }
        /// <summary>
        /// Text v té formě, ve které je chápán kódem.
        /// Text, který se zapisuje do textu segmentu namísto vstupujícího textu.
        /// </summary>
        public string ValueText { get; private set; }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.EditorText + " => " + this.ValueText;
        }
    }
    #endregion
    #region class DefaultSettings : Generátor settingů pro obecný parser pro různé konkrétní jazyky.
    /// <summary>
    /// DefaultSettings : Generátor settingů pro různé konkrétní jazyky.
    /// </summary>
    internal static class DefaultSettings
    {
        #region Generátor settings pro MsSql
        /// <summary>
        /// Setting pro Microsoft T-SQL.
        /// Zajistí sloučení názvů databázových objektů v případě, kdy okolo vnitřní tečky jsou mezery: "lcs . tabulka . sloupec".
        /// </summary>
        public static Setting MsSql { get { return _GetSettingMsSql(true); } }
        #region Konstanty SQL
        public const string SQL_ = "Sql";
        public const string SQL_CODE = "SqlCode";
        public const string SQL_STRING = "SqlString";
        public const string SQL_STRING_UNICODE = "SqlStringUnicode";
        public const string SQL_SYSNAME = "SqlSysName";
        public const string SQL_PARENTHESIS = "SqlParenthesis";
        public const string SQL_COMMENTLINE = "SqlCommentLine";
        public const string SQL_COMMENTBLOCK = "SqlCommentBlock";
        #endregion
        private static Setting _GetSettingMsSql(bool enableMergeDotSpaceText)
        {
            Setting setting = new Setting("MsSql", SQL_CODE);
            AddSettingMsSql(setting, enableMergeDotSpaceText);
            return setting;
        }
        /// <summary>
        /// Do daného settingu vloží položky (definice segmentů) pro jazyk T-SQL.
        /// Vrací root segment této definice.
        /// </summary>
        /// <param name="setting"></param>
        public static SettingSegment AddSettingMsSql(Setting setting)
        {
            return AddSettingMsSql(setting, false);
        }
        /// <summary>
        /// Do daného settingu vloží položky (definice segmentů) pro jazyk T-SQL.
        /// Vrací root segment této definice.
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="enableMergeDotSpaceText"></param>
        public static SettingSegment AddSettingMsSql(Setting setting, bool enableMergeDotSpaceText)
        {
            SettingSegment segment;
            SettingSegment result;

            // Společná data pro SQL_CODE a SQL_PARENTHESIS, které obě mohou obsahovat SQL kód:
            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", "+", "-", "*", "/", "<", "=", ">", "<=", ">=", "<>", "%" };

            // SQL Kód:
            segment = new SettingSegment(SQL_CODE);
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.EndWith = new string[] { ";" };
            segment.InnerSegmentsNames = new string[] { SQL_STRING, SQL_STRING_UNICODE, SQL_SYSNAME, SQL_PARENTHESIS, SQL_COMMENTLINE, SQL_COMMENTBLOCK };
            segment.Keywords = GetKeywordsSqlLanguage().ToArray();
            segment.KeywordsCaseSensitive = false;
            segment.EnableMergeTextBlankDotText = enableMergeDotSpaceText;
            segment.EnableMergeTextSpaceDotAsterix = enableMergeDotSpaceText;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment, SchemeCodeColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);
            result = segment;                         // Výstup této metody

            // Stringy normální:
            segment = new SettingSegment(SQL_STRING);
            segment.BeginWith = "'";
            segment.EndWith = new string[] { "'" };
            segment.SpecialTexts = new SpecialTexts[] { new SpecialTexts("''", "'") };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Stringy Unicode:
            segment = new SettingSegment(SQL_STRING_UNICODE);
            segment.BeginWith = "N'";
            segment.EndWith = new string[] { "'" };
            segment.SpecialTexts = new SpecialTexts[] { new SpecialTexts("''", "'") };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Hranaté závorky = systémové jméno:
            segment = new SettingSegment(SQL_SYSNAME);
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            segment.EnableConvertArrayToText = true;       // Obsah tohoto segmentu bude při parsování na konci tohoto segmentu převeden na Text.
            ApplySchemeSysObject(segment);
            setting.SegmentSettingAdd(segment);

            // Kulaté závorky = funkce, výrazy, a pozor => i vnořené selecty, takže má charakter i code:
            segment = new SettingSegment(SQL_PARENTHESIS);
            segment.BeginWith = "(";
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.EndWith = new string[] { ")" };
            segment.InnerSegmentsNames = new string[] { SQL_STRING, SQL_STRING_UNICODE, SQL_SYSNAME, SQL_PARENTHESIS, SQL_COMMENTLINE, SQL_COMMENTBLOCK };
            segment.Keywords = GetKeywordsSqlLanguage().ToArray();
            segment.KeywordsCaseSensitive = false;
            segment.EnableMergeTextBlankDotText = enableMergeDotSpaceText;
            segment.EnableMergeTextSpaceDotAsterix = enableMergeDotSpaceText;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment, SchemeCodeColorText);
            ApplySchemeDelimiter(segment, SchemeCodeColorText);
            setting.SegmentSettingAdd(segment);

            // Komentář řádkový (začíná --, končí koncem řádku, přičemž konec není ukládán do segmentu, ale zpracuje se jako další text):
            segment = new SettingSegment(SQL_COMMENTLINE);
            segment.IsComment = true;
            segment.BeginWith = "--";
            segment.StopWith = new string[] { "\r\n", "\r", "\n" };
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // Komentář blokový (začíná /*, končí */), otázka je zda může obsahovat vnořené blokové komentáře => rozhodnutí zní ANO:
            segment = new SettingSegment(SQL_COMMENTBLOCK);
            segment.IsComment = true;
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.InnerSegmentsNames = new string[] { SQL_COMMENTBLOCK };
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            // Vrátíme segment, který je rootem tohoto settingu
            return result;
        }
        /// <summary>
        /// Vrátí pole klíčových slov jazyka SQL.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetKeywordsSqlLanguage()
        {
            #region Text of keywords : seznam klíčových slov pochází z článku SQL Helpu, a lze jej odtamtud snadno aktualizovat
            string keywords = @"
ADD
 EXISTS
 PRECISION
 
ALL
 EXIT
 PRIMARY
 
ALTER
 EXTERNAL
 PRINT
 
AND
 FETCH
 PROC
 
ANY
 FILE
 PROCEDURE
 
AS
 FILLFACTOR
 PUBLIC
 
ASC
 FOR
 RAISERROR
 
AUTHORIZATION
 FOREIGN
 READ
 
BACKUP
 FREETEXT
 READTEXT
 
BEGIN
 FREETEXTTABLE
 RECONFIGURE
 
BETWEEN
 FROM
 REFERENCES
 
BREAK
 FULL
 REPLICATION
 
BROWSE
 FUNCTION
 RESTORE
 
BULK
 GOTO
 RESTRICT
 
BY
 GRANT
 RETURN
 
CASCADE
 GROUP
 REVERT
 
CASE
 HAVING
 REVOKE
 
CHECK
 HOLDLOCK
 RIGHT
 
CHECKPOINT
 IDENTITY
 ROLLBACK
 
CLOSE
 IDENTITY_INSERT
 ROWCOUNT
 
CLUSTERED
 IDENTITYCOL
 ROWGUIDCOL
 
COALESCE
 IF
 RULE
 
COLLATE
 IN
 SAVE
 
COLUMN
 INDEX
 SCHEMA
 
COMMIT
 INNER
 SECURITYAUDIT
 
COMPUTE
 INSERT
 SELECT
 
CONSTRAINT
 INTERSECT
 SESSION_USER
 
CONTAINS
 INTO
 SET
 
CONTAINSTABLE
 IS
 SETUSER
 
CONTINUE
 JOIN
 SHUTDOWN
 
CONVERT
 KEY
 SOME
 
CREATE
 KILL
 STATISTICS
 
CROSS
 LEFT
 SYSTEM_USER
 
CURRENT
 LIKE
 TABLE
 
CURRENT_DATE
 LINENO
 TABLESAMPLE
 
CURRENT_TIME
 LOAD
 TEXTSIZE
 
CURRENT_TIMESTAMP
 MERGE
 THEN
 
CURRENT_USER
 NATIONAL
 TO
 
CURSOR
 NOCHECK 
 TOP
 
DATABASE
 NONCLUSTERED
 TRAN
 
DBCC
 NOT
 TRANSACTION
 
DEALLOCATE
 NULL
 TRIGGER
 
DECLARE
 NULLIF
 TRUNCATE
 
DEFAULT
 OF
 TSEQUAL
 
DELETE
 OFF
 UNION
 
DENY
 OFFSETS
 UNIQUE
 
DESC
 ON
 UNPIVOT
 
DISK
 OPEN
 UPDATE
 
DISTINCT
 OPENDATASOURCE
 UPDATETEXT
 
DISTRIBUTED
 OPENQUERY
 USE
 
DOUBLE
 OPENROWSET
 USER
 
DROP
 OPENXML
 VALUES
 
DUMP
 OPTION
 VARYING
 
ELSE
 OR
 VIEW
 
END
 ORDER
 WAITFOR
 
ERRLVL
 OUTER
 WHEN
 
ESCAPE
 OVER
 WHERE
 
EXCEPT
 PERCENT
 WHILE
 
EXEC
 PIVOT
 WITH
 
EXECUTE
 PLAN
 WRITETEXT
 

";
            #endregion
            return SplitTextToRows(keywords, ' ', '\t', '\r', '\n');
        }
        #endregion
        #region Generátor settings pro Rtf
        /// <summary>
        /// Setting pro RTF formát
        /// </summary>
        public static Setting Rtf { get { return _GetSettingRtf(); } }
        private static Setting _GetSettingRtf()
        {
            Setting setting = new Setting("Rtf", RTF_NONE);

            SettingSegment segment;

            string[] blanks = new string[] { "\t", "\r", "\n" };   // MEZERU nechápeme jak Blank znak, protože v textu je významná. Kdežto Cr a Lf (v RTF kódu) se v textu neprojevují.    původně:  new string[] { " ", "\t", "\r", "\n" };
            SpecialTexts[] specs = new SpecialTexts[] { new SpecialTexts(@"\\", @"\") };

            string nums = "0123456789";
            string hexs = "abcdef";
            string lows = "abcdefghijklmnopqrstuvwxyz";
            string upps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // RTF text začíná "ve vakuu", vlastní obsah povinně musí začínat znakem { a končit }, v tomto bloku se nachází vlastní dokument i celá jeho vnitřní stavba:
            segment = new SettingSegment(RTF_NONE);
            segment.InnerSegmentsNames = new string[] { RTF_DOCUMENT };
            setting.SegmentSettingAdd(segment);

            // RTF dokument začíná { a končí }, obsahuje vnitřní bloky ({...}), entity (\fxxx) a znaky (\'a8) i texty:
            segment = new SettingSegment(RTF_DOCUMENT);
            segment.BeginWith = "{";
            segment.Blanks = blanks.ToArray();
            segment.EndWith = new string[] { "}" };
            segment.SpecialTexts = specs.ToArray();
            segment.InnerSegmentsNames = new string[] { RTF_BLOCK, RTF_ENTITY, RTF_CHAR2 /*, RTF_CHARUNICODE */ };
            ApplySchemePlainText(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF vnořený blok začíná { a končí }, obsahuje další vnitřní bloky ({...}), entity (\fxxx) a texty:
            segment = new SettingSegment(RTF_BLOCK);
            segment.BeginWith = "{";
            segment.Blanks = blanks.ToArray();
            segment.EndWith = new string[] { "}" };
            segment.SpecialTexts = specs.ToArray();
            segment.InnerSegmentsNames = new string[] { RTF_BLOCK, RTF_ENTITY, RTF_CHAR2 };
            ApplySchemePlainText(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF entita začíná \ a končí mezerou (mezeru "sežere" = nestane se součástí dalšího textu), nebo končí \ což je stop znak = není součástí End bloku entity, ale představuje další entitu nebo text. Nemá vnitřní segmenty.
            segment = new SettingSegment(RTF_ENTITY);
            segment.BeginWith = @"\";
            segment.EndWith = new string[] { " " };                  // Pokud je za entitou mezera, tak entita končí, a mezeru sežeru (=nevstupuje do následujícího textu). EndWith se vyhodnocuje dříve než Permitteds, takže mezeru chápu jako End a sežeru ji, kdežto něco jiného chápu jako Non-Permitteds, nesežeru to a vyhodnotím to do dalšího segmentu.
            segment.StopWith = new string[] { @"\", "{", "}" };      // Pokud je za entitou backslash nebo { nebo }, tak entita končí, ale backslash (a další znaky) tam nechám a vyhodnotím jako součást další entity nebo char: \f0\fnil\fcharset238 Calibri;
            segment.Permitteds = SplitTextToOneCharStringArray(nums + lows + upps + "*");      // Entita smí obsahovat jen číslice a malá a velká základní písmena a *
            ApplySchemeText(segment, SchemeKeywordColorText, null, FormatFontStyle.Regular, null);           // Dávám barvu Keyword do hodnoty Text, ale ne do Keyword. Pro toto nemám specifickou metodu, použiju obecnější variantu.
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF znak Char začíná \' a má dva znaky z množiny 0123456789abcdef:
            segment = new SettingSegment(RTF_CHAR2);
            segment.BeginWith = @"\'";
            segment.Permitteds = SplitTextToOneCharStringArray(nums + hexs);
            segment.ContentLength = 2;
            ApplySchemeChar(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region Konstanty RTF
        public const string RTF_ = "Rtf";
        public const string RTF_NONE = "RtfNone";
        public const string RTF_DOCUMENT = "RtfDocument";
        public const string RTF_BLOCK = "RtfBlock";
        public const string RTF_ENTITY = "RtfEntity";
        public const string RTF_CHAR2 = "RtfChar2";
        public const string RTF_CHARUNICODE = "RtfCharUnicode";
        #endregion
        #endregion
        #region Generátor settings pro CSharp
        /// <summary>
        /// Setting pro C# formát
        /// </summary>
        public static Setting CSharp { get { return _GetSettingCSharp(); } }
        private static Setting _GetSettingCSharp()
        {
            Setting setting = new Setting("Cs", CS_DOCUMENT);

            SettingSegment segment;

            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", ";", "+", "-", "*", "/", "<", "=", ">", "<=", ">=", "%", "!", "!=" };
            SpecialTexts[] specs = new SpecialTexts[] {
                new SpecialTexts(@"\\", @"\"),
                new SpecialTexts("\\\"", "\""),
                new SpecialTexts(@"\r", "\r"),
                new SpecialTexts(@"\n", "\n"),
                new SpecialTexts(@"\t", "\t")
            };
            List<string> keywordList = GetKeywordsCSharpLanguage();

            // CS dokument začíná standardním kódem
            segment = new SettingSegment(CS_DOCUMENT);
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { CS_BLOCK, CS_COMMENT_BLOCK, CS_COMMENT_ROW, CS_XML_COMMENT, CS_REGION };
            segment.Keywords = keywordList.ToArray();
            segment.KeywordsCaseSensitive = true;
            segment.EnableMergeTextBlankDotText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment);
            setting.SegmentSettingAdd(segment);

            // CS blok začíná { a končí }, obsahuje vnitřní bloky ({...}), stringy, závorky, komentáře. Je vzorem i pro CS_PARENTHESIS a CS_INDEX.
            segment = new SettingSegment(CS_BLOCK);
            segment.BeginWith = "{";
            segment.EndWith = new string[] { "}" };
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { CS_BLOCK, CS_STRING, CS_STRING_RAW, CS_CHAR, CS_PARENTHESIS, CS_INDEX, CS_COMMENT_BLOCK, CS_COMMENT_ROW, CS_XML_COMMENT, CS_REGION };
            segment.Keywords = keywordList.ToArray();
            segment.KeywordsCaseSensitive = true;
            segment.EnableMergeTextBlankDotText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment);
            setting.SegmentSettingAdd(segment);

            // String blok začíná " a končí ", obsahuje speciální znaky ale žádné segmenty:
            segment = new SettingSegment(CS_STRING);
            segment.BeginWith = "\"";
            segment.EndWith = new string[] { "\"", "\r\n", "\r", "\n" };
            segment.SpecialTexts = specs.ToArray();
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // RAW string začíná @" a končí ", neobsahuje žádné speciální znaky ani žádné segmenty:
            segment = new SettingSegment(CS_STRING_RAW);
            segment.BeginWith = "@\"";
            segment.EndWith = new string[] { "\"" };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Char začíná \' a končí \':
            segment = new SettingSegment(CS_CHAR);
            segment.BeginWith = @"\'";
            segment.EndWith = new string[] { "\'" };
            segment.SpecialTexts = specs.ToArray();
            ApplySchemeChar(segment);
            setting.SegmentSettingAdd(segment);

            // Závorky kulaté = skoro jako blok, jen jiný začátek a konec:
            segment = new SettingSegment(CS_PARENTHESIS, setting.GetSegment(CS_BLOCK));
            segment.BeginWith = "(";
            segment.EndWith = new string[] { ")" };
            setting.SegmentSettingAdd(segment);

            // Závorky hranaté (index) = skoro jako blok, jen jiný začátek a konec:
            segment = new SettingSegment(CS_INDEX, setting.GetSegment(CS_BLOCK));
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            setting.SegmentSettingAdd(segment);

            // Blokový komentář
            segment = new SettingSegment(CS_COMMENT_BLOCK);
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            // Řádkový komentář
            segment = new SettingSegment(CS_COMMENT_ROW);
            segment.BeginWith = "//";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // XML komentář
            segment = new SettingSegment(CS_XML_COMMENT);
            segment.BeginWith = "///";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentXml(segment);
            setting.SegmentSettingAdd(segment);

            // Záhlaví regionu
            segment = new SettingSegment(CS_REGION);
            segment.BeginWith = "#region ";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            ApplySchemeCommentXml(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        /// <summary>
        /// Vrátí pole klíčových slov jazyka CSharp.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetKeywordsCSharpLanguage()
        {
            #region Text of keywords : seznam klíčových slov pochází z článku SQL Helpu, a lze jej odtamtud snadno aktualizovat
            string keywords = @"
abstract 	as 	base 	bool 	break
byte 	case 	catch 	char 	checked
class 	const 	continue 	decimal 	default
delegate 	do 	double 	else 	enum
event 	explicit 	extern 	false 	finally
fixed 	float 	for 	foreach 	goto
if 	implicit 	in 	int 	interface
internal 	is 	lock 	long 	namespace
new 	null 	object 	operator 	out
override 	params 	private 	protected 	public
readonly 	ref 	return 	sbyte 	sealed
short 	sizeof 	stackalloc 	static 	string
struct 	switch 	this 	throw 	true
try 	typeof 	uint 	ulong 	unchecked
unsafe 	ushort 	using 	virtual 	void
volatile 	while 	  	  	 

add 	alias 	ascending 	by 	descending
equals 	from 	get 	global 	group
into 	join 	let 	on 	orderby
partial 	remove 	select 	set 	value
var 	where 	yield
";
            #endregion
            return SplitTextToRows(keywords, ' ', '\t', '\r', '\n');
        }
        #region Konstanty C#
        public const string CS_ = "Cs";
        public const string CS_DOCUMENT = "CsDocument";
        public const string CS_BLOCK = "CsBlock";
        public const string CS_PARENTHESIS = "CsParenthesis";
        public const string CS_INDEX = "CsIndex";
        public const string CS_COMMENT_BLOCK = "CsCommentBlock";
        public const string CS_COMMENT_ROW = "CsCommentRow";
        public const string CS_REGION = "CsRegion";
        public const string CS_XML_COMMENT = "CsXmlComment";
        public const string CS_CHAR = "CsChar";
        public const string CS_STRING = "CsString";
        public const string CS_STRING_RAW = "CsStringRaw";
        #endregion
        #endregion
        #region Generátor settings pro XML+HTML
        /// <summary>
        /// Setting pro XML + HTML formát
        /// </summary>
        public static Setting Xml { get { return _GetSettingXml(); } }
        private static Setting _GetSettingXml()
        {
            Setting setting = new Setting("Xml", XML_DOCUMENT);

            SettingSegment segment;
            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", "+", "-", "*", "=", "(", ")", "[", "]", "!", "?" };

            // Text dokumentu = hodnota uvedená v těle elementů, například : <a href="odkaz">Text dokumentu</a>
            segment = new SettingSegment(XML_DOCUMENT);
            segment.InnerSegmentsNames = new string[] { XML_HEADER, XML_ELEMENT, XML_COMMENTBLOCK, XML_ENTITY, XML_END_ELEMENT };
            ApplySchemePlainText(segment);
            setting.SegmentSettingAdd(segment);

            // Komentář blokový, začíná <!--, končí -->, nemůže obsahovat vnořené blokové komentáře ani nic dalšího:
            segment = new SettingSegment(XML_COMMENTBLOCK);
            segment.BeginWith = "<!--";
            segment.EndWith = new string[] { "-->" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            // Element, začíná < a končí >, barevně jde jen o název segmentu, obsahuje volitelně vnořený segment ATTRIBUTE (který začíná mezerou).
            segment = new SettingSegment(XML_ELEMENT);
            segment.BeginWith = "<";
            segment.EndWith = new string[] { "/>", ">" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // EndElement, začíná </ a končí >, barevně jde jen o název segmentu, obsahuje volitelně vnořený segment ATTRIBUTE (který začíná mezerou) - i když to není přípustné, tak se to tak koloruje.
            segment = new SettingSegment(XML_END_ELEMENT);
            segment.BeginWith = "</";
            segment.EndWith = new string[] { ">" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Attribute se nachází v Element, začíná první mezerou která se najde v Element, má StopWith / nebo >, (tím končí atributy, a tento znak se zpracuje už v rámci Elementu), obsahuje delimitery a blank znaky.
            segment = new SettingSegment(XML_ATTRIBUTE);
            segment.BeginWith = " ";
            segment.StopWith = new string[] { "/", "?", ">" };
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_VALUE };
            ApplySchemeVariable(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Value, začíná " a končí ", může obsahovat entity
            segment = new SettingSegment(XML_VALUE);
            segment.BeginWith = "\"";
            segment.EndWith = new string[] { "\"" };
            segment.InnerSegmentsNames = new string[] { XML_ENTITY };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Entita: začíná & a končí ; například &nbsp; 
            segment = new SettingSegment(XML_ENTITY);
            segment.BeginWith = "&";
            segment.StopWith = new string[] { ";" };
            ApplySchemeEntity(segment);
            setting.SegmentSettingAdd(segment);

            // Hlavička, začíná <?  a končí ?>, pravidla má shodná jako element:
            segment = new SettingSegment(XML_HEADER);
            segment.BeginWith = "<?";
            segment.EndWith = new string[] { "?>" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText, null, FormatFontStyle.Italic, FormatFontStyle.ItalicEnd);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region Konstanty XML
        public const string XML_ = "Xml";
        public const string XML_DOCUMENT = "XmlDocument";
        public const string XML_HEADER = "XmlHeader";
        public const string XML_ELEMENT = "XmlElement";
        public const string XML_END_ELEMENT = "XmlEndElement";
        public const string XML_ATTRIBUTE = "XmlAttribute";
        public const string XML_VALUE = "XmlValue";
        public const string XML_ENTITY = "XmlEntity";
        public const string XML_COMMENTBLOCK = "XmlCommentBlock";
        #endregion
        #endregion
        #region Generátor settings pro MSDN formát
        /// <summary>
        /// Setting pro MSDN formát
        /// </summary>
        public static Setting Msdn { get { return _GetSettingMsdn(); } }
        /// <summary>
        /// <para>
        /// Vytvoří setting pro parsování formálního zápisu syntaxe libovolného jazyka.
        /// Formální zápis vychází ze stránek MSDN a má tvar:
        /// </para>
        /// <para>
        /// "&lt;SELECT statement&gt; ::=  SELECT [ ALL | DISTINCT ] [ TOP ( expression ) [ PERCENT ] [ WITH TIES ] ] &lt;select_list&gt;  [ INTO new_table ] 
        /// [ FROM { &lt;table_source&gt; } [ ,...n ] ] [ WHERE &lt;search_condition&gt; ] [ &lt;GROUP BY&gt; ]  [ HAVING &lt; search_condition &gt; ]
        /// &lt;select_list&gt; ::= { * | { table_name | view_name | table_alias }.* | { [ { table_name | view_name | table_alias }. ] { column_name | $IDENTITY | $ROWGUID } 
        /// | udt_column_name [ { . | :: } { { property_name | field_name } | method_name ( argument [ ,...n] ) } ] | expression [ [ AS ] column_alias ] }
        /// | column_alias = expression } [ ,...n ] "
        /// </para>
        /// </summary>
        /// <returns></returns>
        private static Setting _GetSettingMsdn()
        {
            Setting setting = new Setting("MSDN", MSDN_TEXT);

            SettingSegment segment;

            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string varset = "::=";
            string repeater = "...n";

            // MSDN dokument začíná standardním textem, obsahuje cokoliv
            segment = new SettingSegment(MSDN_TEXT);
            SettingSegment segmentText = segment;        // Slouží jako vzor pro některé další podobné segmenty
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = new string[] { varset, "(", ")", ",", repeater };
            segment.InnerSegmentsNames = new string[] { MSDN_VARIABLE, MSDN_OPTIONAL, MSDN_SELECTION, MSDN_COMMENT_ROW, MSDN_COMMENT_BLOCK };
            ApplySchemeCode(segment);
            setting.SegmentSettingAdd(segment);

            // MSDN název proměnné začíná < a končí >, neobsahuje nic jiného
            segment = new SettingSegment(MSDN_VARIABLE);
            segment.BeginWith = "<";
            segment.EndWith = new string[] { ">" };
            segment.Blanks = blanks.ToArray();
            ApplySchemeVariable(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Optional blok začíná [ a končí ], obsahuje vše:
            segment = new SettingSegment(MSDN_OPTIONAL, segmentText);
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            // segment.Illegal = new string[] { ">", "}" };
            segment.Delimiters = new string[] { "(", ")", ",", repeater };
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Selection blok začíná { a končí }, jako oddělovač může mít |, obsahuje všechny další segmenty:
            segment = new SettingSegment(MSDN_SELECTION, segmentText);
            segment.BeginWith = "{";
            segment.EndWith = new string[] { "}" };
            // segment.Illegal = new string[] { ">", "]" };
            segment.Delimiters = new string[] { "|", "(", ")", ",", repeater };
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Řádkový komentář
            segment = new SettingSegment(MSDN_COMMENT_ROW);
            segment.BeginWith = "//";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // Blokový komentář
            segment = new SettingSegment(MSDN_COMMENT_BLOCK);
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region Konstanty MSDN
        public const string MSDN_SETTING = "MsdnSetting";
        public const string MSDN_TEXT = "MsdnText";
        public const string MSDN_VARIABLE = "MsdnVariable";
        public const string MSDN_OPTIONAL = "MsdnOptional";
        public const string MSDN_SELECTION = "MsdnSelection";
        public const string MSDN_COMMENT_ROW = "MsdnCommentRow";
        public const string MSDN_COMMENT_BLOCK = "MsdnCommentBlock";
        #endregion
        #endregion
        #region Obecné barevné schema
        /// <summary>Barva pro text: Obecný text</summary>
        internal static Color SchemePlainColorText { get { return Color.Black; } }
        /// <summary>Barva pro text: Text kódu</summary>
        internal static Color SchemeCodeColorText { get { return Color.DarkSlateGray; } }
        /// <summary>Barva pro text: Klíčové slovo</summary>
        internal static Color SchemeKeywordColorText { get { return Color.MediumBlue; } }
        /// <summary>Barva pro text: Název systémového objektu</summary>
        internal static Color SchemeSysObjectColorText { get { return Color.DarkSlateGray; } }

        /// <summary>Barva pro text: Oddělovače</summary>
        internal static Color SchemeDelimiterColorText { get { return Color.Magenta; } }

        /// <summary>Barva pro text: Proměnná</summary>
        internal static Color SchemeVariableColorText { get { return Color.DarkCyan; } }
        /// <summary>Styl písma pro : Proměnná, počátek</summary>
        internal static FormatFontStyle SchemeVariableStyleBegin { get { return FormatFontStyle.None; } }
        /// <summary>Styl písma pro : Proměnná, konec</summary>
        internal static FormatFontStyle SchemeVariableStyleEnd { get { return FormatFontStyle.None; } }

        /// <summary>Barva pro text: String</summary>
        internal static Color SchemeStringColorText { get { return Color.DarkRed; } }
        /// <summary>Styl písma pro : String, počátek</summary>
        internal static FormatFontStyle SchemeStringStyleBegin { get { return FormatFontStyle.Bold; } }
        /// <summary>Styl písma pro : String, konec</summary>
        internal static FormatFontStyle SchemeStringStyleEnd { get { return FormatFontStyle.BoldEnd; } }

        /// <summary>Barva pro text: Char</summary>
        internal static Color SchemeCharColorText { get { return Color.DarkViolet; } }
        /// <summary>Styl písma pro : Char, počátek</summary>
        internal static FormatFontStyle SchemeCharStyleBegin { get { return FormatFontStyle.Bold; } }
        /// <summary>Styl písma pro : Char, konec</summary>
        internal static FormatFontStyle SchemeCharStyleEnd { get { return FormatFontStyle.BoldEnd; } }

        /// <summary>Barva pro text: Entity (speciální znaky)</summary>
        internal static Color SchemeEntityColorText { get { return Color.DarkBlue; } }
        /// <summary>Barva pro pozadí: Entity (speciální znaky)</summary>
        internal static Color SchemeEntityColorBack { get { return Color.AliceBlue; } }
        /// <summary>Styl písma pro : Entity (speciální znaky), počátek</summary>
        internal static FormatFontStyle SchemeEntityStyleBegin { get { return FormatFontStyle.None; } }
        /// <summary>Styl písma pro : Entity (speciální znaky), konec</summary>
        internal static FormatFontStyle SchemeEntityStyleEnd { get { return FormatFontStyle.None; } }

        /// <summary>Barva pro text: Komentář (barva písma)</summary>
        internal static Color SchemeCommentColorText { get { return Color.DarkSlateGray; } }
        /// <summary>Barva pro text: Komentář obecně (barva pozadí)</summary>
        internal static Color SchemeCommentColorBack { get { return Color.FromArgb(226, 226, 234); } }
        /// <summary>Barva pro text: Komentář blokový (barva pozadí)</summary>
        internal static Color SchemeCommentBlockColorBack { get { return SchemeCommentColorBack; } }
        /// <summary>Barva pro text: Komentář řádkový (barva pozadí)</summary>
        internal static Color SchemeCommentRowColorBack { get { return SchemeCommentColorBack; } }
        /// <summary>Barva pro text: Komentář XML (barva pozadí)</summary>
        internal static Color SchemeCommentXmlColorBack { get { return Color.AliceBlue; } }
        /// <summary>Styl písma pro : Komentář, počátek</summary>
        internal static FormatFontStyle SchemeCommentStyleBegin { get { return FormatFontStyle.None; } }
        /// <summary>Styl písma pro : Komentář, konec</summary>
        internal static FormatFontStyle SchemeCommentStyleEnd { get { return FormatFontStyle.None; } }

        #region Aplikace standardních formátovacích schemat do settingu
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro ValueType = Text, vloží barvy popředí a pozadí, a formátovací styly Begin a End (pokud jsou předány hodnoty).
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="colorText"></param>
        private static void ApplySchemeText(SettingSegment segment, Color colorText)
        {
            segment.AddFormatCodes(ItemType.Text,
                new FormatCode[] { FormatCode.NewForeColor(colorText), FormatCode.NewFont("Courier New CE") },
                new FormatCode[] { });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro ValueType = Text, vloží barvy popředí a pozadí, a formátovací styly Begin a End (pokud jsou předány hodnoty).
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="colorText"></param>
        /// <param name="colorBack"></param>
        /// <param name="styleBegin"></param>
        /// <param name="styleEnd"></param>
        private static void ApplySchemeText(SettingSegment segment, Color? colorText, Color? colorBack, FormatFontStyle? styleBegin, FormatFontStyle? styleEnd)
        {
            segment.AddFormatCodes(ItemType.Text,
                new FormatCode[] { FormatCode.NewForeColor(colorText.HasValue ? colorText.Value : Color.Empty), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(colorBack.HasValue ? colorBack.Value : Color.Empty), FormatCode.NewFontStyle(styleBegin.HasValue ? styleBegin.Value : FormatFontStyle.None) },
                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(styleEnd.HasValue ? styleEnd.Value : FormatFontStyle.None) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Plain
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemePlainText(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemePlainColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Code
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCode(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCodeColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Keyword
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeKeyword(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Keyword,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeKeywordColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Keyword
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="nextForeColor"></param>
        private static void ApplySchemeKeyword(SettingSegment segment, Color? nextForeColor)
        {
            segment.AddFormatCodes(ItemType.Keyword,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeKeywordColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor((nextForeColor.HasValue ? nextForeColor.Value : Color.Empty)) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Delimiter
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeDelimiter(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Delimiter,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeDelimiterColorText) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Delimiter
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="nextForeColor"></param>
        private static void ApplySchemeDelimiter(SettingSegment segment, Color? nextForeColor)
        {
            segment.AddFormatCodes(ItemType.Delimiter,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeDelimiterColorText) },
                                new FormatCode[] { FormatCode.NewForeColor((nextForeColor.HasValue ? nextForeColor.Value : Color.Empty)) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = String
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeString(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeStringColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeStringStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeStringStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Char
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeChar(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCharColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeCharStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeCharStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Variable
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeVariable(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeVariableColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeVariableStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeVariableStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = SysObject
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeSysObject(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeSysObjectColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Entity
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeEntity(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeEntityColorText), FormatCode.NewHighlight(SchemeEntityColorBack), FormatCode.NewFontStyle(SchemeEntityStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeEntityStyleEnd) });
        }





        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentRow
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentBlock(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentBlockColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentBlock
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentRow(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentRowColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentXml
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentXml(SettingSegment segment)
        {
            segment.AddFormatCodes(ItemType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentXmlColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }

        #endregion
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
    #region Podpora pro obecné formátování textu : IFormattedTextAssembler, FormatCodeKey, FormatCodePair, FormatCode, enumy Format*
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
    #region class FormatCodeKey a FormatCodePair : podpora pro formátování obsahu (Values) segmentu.
    /// <summary>
    /// FormatCodeKey : klíč pro pár formátovacích kódů.
    /// Pracuje s typem hodnoty a s jejím názvem (ValueType a ValueName).
    /// Slouží jako klíč, nenese konkrétní formátovací data.
    /// Funguje jako předek třídy FormatCodePair.
    /// </summary>
    internal class FormatCodeKey
    {
        #region Konstrukce, overrides
        /// <summary>
        /// Vytvoří ParserSegmentRtfCodeKey pro daný klíč a název, pro vyhledávání v Dictionary.
        /// </summary>
        /// <param name="value">Parser hodnota, obsahuje Type i Name.</param>
        internal FormatCodeKey(ParsedItem value)
        {
            this.ValueType = value.ItemType;
            this.ValueName = value.SegmentName;
            this._SetHashcode();
        }
        /// <summary>
        /// Vytvoří ParserSegmentRtfCodeKey pro daný klíč a název, pro vyhledávání v Dictionary.
        /// </summary>
        /// <param name="itemType">Typ hodnoty.</param>
        /// <param name="valueName">Název hodnoty</param>
        internal FormatCodeKey(ItemType itemType, string valueName)
        {
            this.ValueType = itemType;
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
        public ItemType ValueType { get; private set; }
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
    internal class FormatCodePair : FormatCodeKey
    {
        #region Konstrukce
        /// <summary>
        /// Vytvoří RtfCodePair pro daný typ hodnoty.
        /// Název je pro tuto položku nastaven na null.
        /// </summary>
        /// <param name="itemType">Typ hodnoty.
        /// Správné je předat typ: Blank, Delimiter, Text.
        /// Podle typu Text (bez názvu) se budou zobrazovat naše okraje (Begin a End, typicky závorky).
        /// Název je pro tuto položku nastaven na null.
        /// </param>
        /// <param name="codeSet">RTF kódy na začátku</param>
        /// <param name="codeReset">RTF kódy na konci</param>
        public FormatCodePair(ItemType itemType, FormatCode[] codeSet, FormatCode[] codeReset)
            : base(itemType, null)
        {
            this.CodeSet = codeSet;
            this.CodeReset = codeReset;
        }
        /// <summary>
        /// Vytvoří RtfCodePair pro typ hodnoty Text pro daný název. 
        /// "Název" je uživatelská definice, umožňuje barevně odlišovat různé texty (Keyword, Numbers, Functions, atd).
        /// Z hlediska Parseru jsou to všechno Texty, ale z hlediska Lexeru mají různý význam.
        /// Typ hodnoty (ValueType) je pro tuto položku nastaven na ItemType.Text.
        /// </summary>
        /// <param name="valueName">Název páru.
        /// Typ hodnoty (ValueType) je pro tuto položku nastaven na ItemType.Text.</param>
        /// <param name="codeSet">RTF kódy na začátku</param>
        /// <param name="codeReset">RTF kódy na konci</param>
        public FormatCodePair(string valueName, FormatCode[] codeSet, FormatCode[] codeReset)
            : base(ItemType.Text, valueName)
        {
            this.CodeSet = codeSet;
            this.CodeReset = codeReset;
        }
        #endregion
        #region Property
        /// <summary>
        /// RTF kódy na začátku textu
        /// </summary>
        public FormatCode[] CodeSet { get; set; }
        /// <summary>
        /// RTF kódy na konci textu
        /// </summary>
        public FormatCode[] CodeReset { get; set; }
        #endregion
    }
    #endregion
    #region class FormatItem, enum FormatItemType, FormatAlignment, FormatFontStyle, FormatHighlightColor
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
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.ItemType == FormatItemType.Text)
                return this.Text;
            return "{" + this.ItemType.ToString() + "}";
        }
        #endregion
        #region Property
        /// <summary>
        /// Obsahuje true, když je prázdný (<see cref="ItemType"/> == <see cref="FormatItemType.None"/>)
        /// </summary>
        public bool IsEmpty { get { return this.ItemType == FormatItemType.None; } }
        /// <summary>
        /// Typ položky
        /// </summary>
        public FormatItemType ItemType { get; private set; }
        /// <summary>
        /// Čitelný text, pokud v tomto prvku má co pohledávat
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Explicitně zadaný vnitřní kód
        /// </summary>
        public string Code { get; private set; }
        /// <summary>
        /// Název písma
        /// </summary>
        public string FontName { get; private set; }
        /// <summary>
        /// Velikost písma
        /// </summary>
        public int FontSize { get; private set; }
        /// <summary>
        /// Styl písma
        /// </summary>
        public FormatFontStyle FontStyle { get; private set; }
        /// <summary>
        /// Barva písma
        /// </summary>
        public Color Color { get; private set; }
        /// <summary>
        /// Barva pozadí
        /// </summary>
        public FormatHighlightColor HighlightColor { get; private set; }
        /// <summary>
        /// true = protekovaný blok
        /// </summary>
        public bool IsProtected { get; private set; }
        /// <summary>
        /// Styl zarovnání
        /// </summary>
        public FormatAlignment Alignment { get; private set; }
        #endregion
        #region Static konstruktory
        /// <summary>
        /// Vrátí nový prvek typu <see cref="FormatItemType.Text"/> s daným textem
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static FormatCode NewText(string text)
        {
            return new FormatCode(FormatItemType.Text, text);
        }
        /// <summary>
        /// Vrátí nový prvek typu <see cref="FormatItemType.Text"/> s daným textem, a volitelně koncem řádku
        /// </summary>
        /// <param name="text"></param>
        /// <param name="addNewLine"></param>
        /// <returns></returns>
        public static FormatCode NewText(string text, bool addNewLine)
        {
            return new FormatCode(FormatItemType.Text, text + (addNewLine ? "\r" : ""));
        }
        /// <summary>
        /// Obsahuje kód pro nový řádek
        /// </summary>
        public static FormatCode NewLine { get { return new FormatCode(FormatItemType.Text, "\r"); } }
        /// <summary>
        /// Obsahuje znak pro defaultní odstavec
        /// </summary>
        public static FormatCode DefaultParagraph { get { return new FormatCode(FormatItemType.Code, @"\pard"); } }
        /// <summary>
        /// Obsahuje kód pro pro nový font
        /// </summary>
        /// <param name="fontName"></param>
        /// <returns></returns>
        public static FormatCode NewFont(string fontName)
        {
            return new FormatCode(FormatItemType.FontName, fontName);
        }
        /// <summary>
        /// Obsahuje kód pro pro velikost textu
        /// </summary>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        public static FormatCode NewSize(int fontSize)
        {
            return new FormatCode(FormatItemType.FontName, fontSize);
        }
        /// <summary>
        /// Obsahuje kód pro pro styl textu
        /// </summary>
        /// <param name="fontStyle"></param>
        /// <returns></returns>
        public static FormatCode NewFontStyle(FormatFontStyle fontStyle)
        {
            return new FormatCode(FormatItemType.FontStyle, fontStyle);
        }
        /// <summary>
        /// Obsahuje kód pro pro zarovnání
        /// </summary>
        /// <param name="alignment"></param>
        /// <returns></returns>
        public static FormatCode NewParAlignment(FormatAlignment alignment)
        {
            return new FormatCode(FormatItemType.ParagraphAlignment, alignment);
        }
        /// <summary>
        /// Obsahuje kód pro pro barvu pozadí
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static FormatCode NewBackColor(Color color)
        {
            return new FormatCode(FormatItemType.BackColor, color);
        }
        /// <summary>
        /// Obsahuje kód pro pro barvu popředí
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static FormatCode NewForeColor(Color color)
        {
            return new FormatCode(FormatItemType.ForeColor, color);
        }
        /// <summary>
        /// Obsahuje kód pro pro druh zvýraznění
        /// </summary>
        /// <param name="highlight"></param>
        /// <returns></returns>
        public static FormatCode NewHighlight(FormatHighlightColor highlight)
        {
            Color backColor = GetHighlightColor(highlight);
            return new FormatCode(FormatItemType.Highlight, backColor);
        }
        /// <summary>
        /// Obsahuje kód pro pro barvu zvýraznění
        /// </summary>
        /// <param name="highlightColor"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Obsahuje kód pro pro protekovaný blok
        /// </summary>
        /// <param name="isProtected"></param>
        /// <returns></returns>
        public static FormatCode NewProtect(bool isProtected)
        {
            return new FormatCode(FormatItemType.ProtectState, isProtected);
        }
        #endregion
    }
    /// <summary>
    /// Druh RTF prvku
    /// </summary>
    public enum FormatItemType
    {
        /// <summary>
        /// Nezadáno
        /// </summary>
        None,
        /// <summary>
        /// Text
        /// </summary>
        Text,
        /// <summary>
        /// Code
        /// </summary>
        Code,
        /// <summary>
        /// FontName
        /// </summary>
        FontName,
        /// <summary>
        /// FontSize
        /// </summary>
        FontSize,
        /// <summary>
        /// FontStyle
        /// </summary>
        FontStyle,
        /// <summary>
        /// ForeColor
        /// </summary>
        ForeColor,
        /// <summary>
        /// BackColor
        /// </summary>
        BackColor,
        /// <summary>
        /// Highlight
        /// </summary>
        Highlight,
        /// <summary>
        /// ProtectState
        /// </summary>
        ProtectState,
        /// <summary>
        /// ParagraphAlignment
        /// </summary>
        ParagraphAlignment
    }
    /// <summary>
    /// Zarovnání textu
    /// </summary>
    public enum FormatAlignment
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Left
        /// </summary>
        Left,
        /// <summary>
        /// Center
        /// </summary>
        Center,
        /// <summary>
        /// Right
        /// </summary>
        Right,
        /// <summary>
        /// Justify
        /// </summary>
        Justify
    }
    /// <summary>
    /// Styl písma
    /// </summary>
    [Flags]
    public enum FormatFontStyle
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Bold
        /// </summary>
        Bold = 0x01,
        /// <summary>
        /// Konec Bold
        /// </summary>
        BoldEnd = 0x02,
        /// <summary>
        /// Italic
        /// </summary>
        Italic = 0x04,
        /// <summary>
        /// Konec Italic
        /// </summary>
        ItalicEnd = 0x08,
        /// <summary>
        /// Underline
        /// </summary>
        Underline = 0x10,
        /// <summary>
        /// Konec Underline
        /// </summary>
        UnderlineEnd = 0x20,
        /// <summary>
        /// StrikeOut
        /// </summary>
        StrikeOut = 0x40,
        /// <summary>
        /// Konec StrikeOut
        /// </summary>
        StrikeOutEnd = 0x80,
        /// <summary>
        /// Regular = konec všech ostatních (= BoldEnd | ItalicEnd | UnderlineEnd | StrikeOutEnd)
        /// </summary>
        Regular = BoldEnd | ItalicEnd | UnderlineEnd | StrikeOutEnd,
        /// <summary>
        /// BoldOnly = pouze Bold, ostatní konec (= Bold | ItalicEnd | UnderlineEnd | StrikeOutEnd)
        /// </summary>
        BoldOnly = Bold | ItalicEnd | UnderlineEnd | StrikeOutEnd,
        /// <summary>
        /// BoldOnly = pouze Italic, ostatní konec (= BoldEnd | Italic | UnderlineEnd | StrikeOutEnd)
        /// </summary>
        ItalicOnly = BoldEnd | Italic | UnderlineEnd | StrikeOutEnd,
        /// <summary>
        /// BoldOnly = pouze Underline, ostatní konec (= BoldEnd | ItalicEnd | Underline | StrikeOutEnd)
        /// </summary>
        UnderlineOnly = BoldEnd | ItalicEnd | Underline | StrikeOutEnd,
        /// <summary>
        /// StrikeOutOnly = pouze StrikeOut, ostatní konec (= BoldEnd | ItalicEnd | UnderlineEnd | StrikeOut)
        /// </summary>
        StrikeOutOnly = BoldEnd | ItalicEnd | UnderlineEnd | StrikeOut,
        /// <summary>
        /// BoldOnly = pouze Bold a Italic, ostatní konec (= Bold | Italic | UnderlineEnd | StrikeOutEnd)
        /// </summary>
        BoldItalicOnly = Bold | Italic | UnderlineEnd | StrikeOutEnd
    }
    /// <summary>
    /// Styl zvýraznězní
    /// </summary>
    public enum FormatHighlightColor
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Black
        /// </summary>
        Black = 1,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 2,
        /// <summary>
        /// Cyan
        /// </summary>
        Cyan = 3,
        /// <summary>
        /// Green
        /// </summary>
        Green = 4,
        /// <summary>
        /// Magenta
        /// </summary>
        Magenta = 5,
        /// <summary>
        /// Red
        /// </summary>
        Red = 6,
        /// <summary>
        /// Yellow
        /// </summary>
        Yellow = 7,
        /// <summary>
        /// DarkBlue
        /// </summary>
        DarkBlue = 9,
        /// <summary>
        /// DarkCyan
        /// </summary>
        DarkCyan = 10,
        /// <summary>
        /// DarkGreen
        /// </summary>
        DarkGreen = 11,
        /// <summary>
        /// DarkMagenta
        /// </summary>
        DarkMagenta = 12,
        /// <summary>
        /// DarkRed
        /// </summary>
        DarkRed = 13,
        /// <summary>
        /// DarkYellow
        /// </summary>
        DarkYellow = 14,
        /// <summary>
        /// DarkGray
        /// </summary>
        DarkGray = 15,
        /// <summary>
        /// LightGray
        /// </summary>
        LightGray = 16
    }
    #endregion
    #endregion
    #region class RtfCoder
    #region class RtfCoder : třída pro sestavování RTF textu z jednotlivých složek, a následnou kompletaci do standardního RTF kódu
    /// <summary>
    /// RtfCoder : třída pro sestavování RTF textu z jednotlivých složek, a následnou kompletaci do standardního RTF kódu
    /// </summary>
    public class RtfCoder : IFormattedTextAssembler, IDisposable
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor
        /// </summary>
        public RtfCoder()
        {
            this.RtfItems = new List<FormatCode>();
            this.ReadOnly = true;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="readOnly"></param>
        public RtfCoder(bool readOnly)
        {
            this.RtfItems = new List<FormatCode>();
            this.ReadOnly = readOnly;
        }
        void IDisposable.Dispose()
        {
            this.RtfItems = null;
        }
        #endregion
        #region IFormattedTextAssembler implementace
        void IFormattedTextAssembler.Clear() { this.Clear(); }
        void IFormattedTextAssembler.Add(FormatCode item) { this.Add(item); }
        void IFormattedTextAssembler.AddRange(IEnumerable<FormatCode> items) { this.AddRange(items); }
        string IFormattedTextAssembler.Text { get { return this.RtfText; } }
        #endregion
        #region Public property
        /// <summary>
        /// Standardní RTF hlavička
        /// </summary>
        public static string Header
        {
            get
            {
                return @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset238{\*\fname Courier New;}Courier New CE;}}
{\*\generator Msftedit 5.41.15.1515;}\viewkind4\uc1\pard\lang1029\f0\fs20 ";
            }
        }
        /// <summary>
        /// Seznam RTF položek
        /// </summary>
        public List<FormatCode> RtfItems { get; private set; }
        /// <summary>
        /// Příznak, zda RTF text má být read-only
        /// </summary>
        public bool ReadOnly { get; set; }
        /// <summary>
        /// Obsahuje vždy platný aktuální RTF text
        /// </summary>
        public string RtfText
        {
            get { return this._CreateRtfText(); }
        }
        #endregion
        #region Přidávání a mazání položek RTF textu
        /// <summary>
        /// Do RTF dokumentu přidá další segmenty.
        /// Segmenty se vytvářejí pomocí statických konstruktorů třídy RtfItem, typicky: .Add(RtfItem.NewText("text do RTF dokumentu."))
        /// </summary>
		/// <param name="items"></param>
        public void Add(params FormatCode[] items)
        {
            if (items != null)
                this.AddRange((IEnumerable<FormatCode>)items);
        }
        /// <summary>
        /// Do RTF dokumentu přidá další segmenty.
        /// Segmenty se vytvářejí pomocí statických konstruktorů třídy RtfItem, typicky: .Add(RtfItem.NewText("text do RTF dokumentu."))
        /// </summary>
		/// <param name="items"></param>
        public void AddRange(IEnumerable<FormatCode> items)
        {
            if (items != null)
            {
                foreach (FormatCode item in items)
                {
                    if (!item.IsEmpty)
                        this.RtfItems.Add(item);
                }
            }
        }
        /// <summary>
        /// Vymaže svůj obsah
        /// </summary>
        public void Clear()
        {
            this.RtfItems.Clear();
        }
        #endregion
        #region Kompletace RTF textu
        /// <summary>
        /// Zkompletuje RTF text
        /// </summary>
        /// <returns></returns>
        private string _CreateRtfText()
        {
            // Nejdřív musím projít položky RtfItems, protože tím se mi vytvoří tabulka fontů a tabulka barev, kteréžto patří do záhlaví před vlastní text:
            StringBuilder txt = new StringBuilder();

            List<RtfFont> fontTable = new List<RtfFont>();
            fontTable.Add(new RtfFont(0, 238, "nil", "Courier New", "Courier New CE"));

            List<RtfColor> colorTable = new List<RtfColor>();
            colorTable.Add(new RtfColor(0, Color.Empty));            // Takhle je zajištěna default barva: v RTF kódu má číslo "0", a do color tabulky se explicitně nevepisuje.

            Dictionary<int, string> codeTable = _CreateRtfCodeTable();
            bool inCode = true;
            foreach (FormatCode item in this.RtfItems)
            {
                string fragment = null;
                switch (item.ItemType)
                {
                    case FormatItemType.None:
                        break;
                    case FormatItemType.Text:
                        fragment = _ItemCreateRtfText(item, ref inCode, codeTable);
                        break;
                    case FormatItemType.Code:
                        fragment = _ItemCreateRtfCode(item, ref inCode);
                        break;
                    case FormatItemType.FontName:
                        fragment = _ItemCreateRtfFont(item, ref inCode, fontTable);
                        break;
                    case FormatItemType.FontSize:
                        fragment = _ItemCreateRtfSize(item, ref inCode);
                        break;
                    case FormatItemType.FontStyle:
                        fragment = _ItemCreateRtfStyle(item, ref inCode);
                        break;
                    case FormatItemType.ForeColor:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.BackColor:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.Highlight:
                        fragment = _ItemCreateRtfColor(item, ref inCode, colorTable);
                        break;
                    case FormatItemType.ProtectState:
                        break;
                    case FormatItemType.ParagraphAlignment:
                        fragment = _ItemCreateRtfAlign(item, ref inCode);
                        break;
                }
                if (fragment != null)
                    txt.Append(fragment);
            }

            // Kompletace RTF: hlavička
            StringBuilder rtf = new StringBuilder();
            rtf.Append(@"{\rtf1\ansi\ansicpg1250\deff0");

            // fonty
            if (fontTable.Count > 0)
            {
                rtf.Append(@"{\fonttbl");
                foreach (RtfFont font in fontTable)
                    rtf.Append(font.RtfTable);
                rtf.Append(@"}");
            }
            // barvy
            if (colorTable.Count > 1)
            {
                rtf.AppendLine();
                rtf.Append(@"{\colortbl ");
                foreach (RtfColor color in colorTable)
                    rtf.Append(color.RtfTable);
                rtf.Append(@"}");
            }
            // záhlaví před prvním textem:
            rtf.AppendLine();
            rtf.Append(@"{\*\generator Msftedit 5.41.21.2510;}\viewkind4" + (this.ReadOnly ? @"\allprot" : "") + @"\uc1\pard\lang1029\f0\fs20");

            // text:
            rtf.Append(txt.ToString());

            // zápatí:
            rtf.AppendLine("\\par");
            rtf.AppendLine("}");
            rtf.Append(" ");

            return rtf.ToString();
        }
        private Dictionary<int, string> _CreateRtfCodeTable()
        {
            Dictionary<int, string> codeTable = new Dictionary<int, string>();

            codeTable.Add(09, @"\tab");           // TAB
            codeTable.Add(10, "");                // LF = nic, ale musím zajistit že ve vstupním textu bude vždy CR (viz metoda ConvertCrLf())
            codeTable.Add(13, @"\par" + "\r\n");  // CR = text "\par" + CrLf (vizuální oddělení textu)
            codeTable.Add(92, @"\\");             // Jedno zpětné lomítko => nahradit dvěma
            codeTable.Add((int)'{', @"\{");
            codeTable.Add((int)'}', @"\}");

            return codeTable;
        }
        private string _ItemCreateRtfText(FormatCode item, ref bool inCode, Dictionary<int, string> codeTable)
        {
            StringBuilder sb = new StringBuilder();
            if (item.Text != null)
            {
                string text = ConvertCrLf(item.Text);       // Zajistí korektní přítomnost CR
                foreach (char c in text)
                {
                    int i = (int)c;
                    string code;
                    // Znaky 128 a vyšší:
                    if (i >= 128 && !codeTable.ContainsKey(i))
                    {
                        if (i <= 255)
                        {   // Unicode znaku se vejde do jednoho byte:
                            code = @"\'" + i.ToString("X2").ToLower();
                        }
                        else
                        {   // Unicode je větší než 1 byte, vypíšu jej jinak:
                            code = @"\u" + i.ToString() + "?";
                        }

                        //Encoding ec = Encoding.GetEncoding(852);
                        //byte[] dc = ec.GetBytes(new char[] { c });

                        //Encoding ec2 = Encoding.GetEncoding(1250);
                        //byte[] dc2 = ec2.GetBytes(new char[] { c });


                        //code = @"\'";
                        //int h = dc[0];
                        //if (dc.Length == 1)
                        //    code += (dc[0]).ToString("X2");
                        //else if (dc.Length == 2)
                        //    code += (dc[0] + dc[1] * 0x100).ToString("X4");

                        codeTable.Add(i, code);
                    }

                    // Pokud znak je obsažen v codeTable, vypíšu jej jako kód:
                    if (codeTable.TryGetValue(i, out code))
                    {
                        sb.Append(code);
                        inCode = (i < 32 && i != 13);    // Řídící kód je to jen pro znaky menší než 128. Znaky s vyšším ASCII kódem jsou písmena a za nimi se mezera nepřidává...  A taky ne za \par (tam je fyzicky CrLf)
                    }
                    else
                    {
                        if (inCode)
                        {	// Po kódu, před textem se vkládá mezera:
                            sb.Append(" ");
                            inCode = false;
                        }
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Metoda upraví vstupující text tak, aby obsahoval pouze znaky CR.
        /// Namísto znaků CrLf dá Cr, a pak namísto Lf dá Cr.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ConvertCrLf(string text)
        {
            if (text == null) return null;
            string result = text.Replace("\r\n", "\r").Replace("\n", "\r");
            return result;
        }
        private string _ItemCreateRtfCode(FormatCode item, ref bool inCode)
        {
            inCode = true;
            return item.Code;
        }
        private string _ItemCreateRtfFont(FormatCode item, ref bool inCode, List<RtfFont> fontTable)
        {
            RtfFont font = fontTable.FirstOrDefault(ft => ft.FontName == item.FontName);
            if (font == null)
            {
                font = new RtfFont(fontTable.Count, 238, "swiss", item.FontName, item.FontName);
                fontTable.Add(font);
            }
            inCode = true;
            return font.RtfMark;
        }
        private string _ItemCreateRtfSize(FormatCode item, ref bool inCode)
        {
            inCode = true;
            return @"\fs" + (2 * item.FontSize).ToString();
        }
        private string _ItemCreateRtfStyle(FormatCode item, ref bool inCode)
        {
            string result = "";
            if ((item.FontStyle & FormatFontStyle.Bold) > 0)
                result += @"\b";
            if ((item.FontStyle & FormatFontStyle.BoldEnd) > 0)
                result += @"\b0";
            if ((item.FontStyle & FormatFontStyle.Italic) > 0)
                result += @"\i";
            if ((item.FontStyle & FormatFontStyle.ItalicEnd) > 0)
                result += @"\i0";
            if ((item.FontStyle & FormatFontStyle.Underline) > 0)
                result += @"\ul";
            if ((item.FontStyle & FormatFontStyle.UnderlineEnd) > 0)
                result += @"\ulnone";
            if ((item.FontStyle & FormatFontStyle.StrikeOut) > 0)
                result += @"\strike";
            if ((item.FontStyle & FormatFontStyle.StrikeOutEnd) > 0)
                result += @"\strike0";
            if (result.Length > 0)
                inCode = true;
            return result;
        }
        private string _ItemCreateRtfColor(FormatCode item, ref bool inCode, List<RtfColor> colorTable)
        {
            RtfColor rtfColor = colorTable.FirstOrDefault(rc => rc.ColorValue.Equals(item.Color));   // Najdu položku RtfColor odpovídající zadané barvě
            if (rtfColor == null)
            {
                rtfColor = new RtfColor(colorTable.Count, item.Color);
                colorTable.Add(rtfColor);
            }
            switch (item.ItemType)
            {
                case FormatItemType.ForeColor:
                    inCode = true;
                    return rtfColor.RtfMarkF;
                case FormatItemType.BackColor:
                    inCode = true;
                    return rtfColor.RtfMarkB;
                case FormatItemType.Highlight:
                    inCode = true;
                    return rtfColor.RtfMarkH;
            }
            return "";
        }
        private string _ItemCreateRtfAlign(FormatCode item, ref bool inCode)
        {
            switch (item.Alignment)
            {
                case FormatAlignment.Left:
                    inCode = true;
                    return @"";
                case FormatAlignment.Center:
                    inCode = true;
                    return @"\qc";
                case FormatAlignment.Right:
                    inCode = true;
                    return @"\qr";
                case FormatAlignment.Justify:
                    inCode = true;
                    return @"\qj";
            }
            return @"";
        }
        #endregion
    }
    #endregion
    #region classes RtfFont, RtfColor
    /// <summary>
    /// Úložiště jednoho fontu v tabulce fontů RTF
    /// </summary>
    internal class RtfFont
    {
        /// <summary>
        /// Konstruktor pro font
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        public RtfFont(int index, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = "238";
            this.FontType = "nil";
            this.FontFamily = name;
            this.FontName = name;
        }
        /// <summary>
        /// Konstruktor pro font
        /// </summary>
        /// <param name="index"></param>
        /// <param name="family"></param>
        /// <param name="name"></param>
        public RtfFont(int index, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = "238";
            this.FontType = "nil";
            this.FontFamily = family;
            this.FontName = name;
        }
        /// <summary>
        /// Konstruktor pro font
        /// </summary>
        /// <param name="index"></param>
        /// <param name="charSet"></param>
        /// <param name="family"></param>
        /// <param name="name"></param>
        public RtfFont(int index, int charSet, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = charSet.ToString();
            this.FontType = "nil";
            this.FontFamily = family;
            this.FontName = name;
        }
        /// <summary>
        /// Konstruktor pro font
        /// </summary>
        /// <param name="index"></param>
        /// <param name="charSet"></param>
        /// <param name="type"></param>
        /// <param name="family"></param>
        /// <param name="name"></param>
        public RtfFont(int index, int charSet, string type, string family, string name)
        {
            this.FontCode = index.ToString();
            this.FontCharset = charSet.ToString();
            this.FontType = type;
            this.FontFamily = family;
            this.FontName = name;
        }
        /// <summary>
        /// Pořadové číslo fontu v tabulce, jako string, bez znaku "f"
        /// </summary>
        public string FontCode { get; private set; }
        /// <summary>
        /// Číslo charset jako string
        /// </summary>
        public string FontCharset { get; private set; }
        /// <summary>
        /// Typ fontu
        /// </summary>
        public string FontType { get; private set; }
        /// <summary>
        /// Family fontu
        /// </summary>
        public string FontFamily { get; private set; }
        /// <summary>
        /// Název fontu
        /// </summary>
        public string FontName { get; private set; }
        /// <summary>
        /// Značka fontu do flow textu: \f3   
        /// </summary>
        public string RtfMark { get { return @"\f" + this.FontCode; } }
        /// <summary>
        /// Plný text fontu do tabulky fontů: {\f3\fnil\fcharset238{\*\fname Arial;}Arial CE;}
        /// </summary>
        public string RtfTable
        {
            get
            {
                return @"{\f" + this.FontCode + @"\f" + this.FontType + @"\fcharset" + this.FontCharset + @"{\*\fname " + this.FontFamily + @";}" + this.FontName + ";}";
            }
        }
    }
    /// <summary>
    /// Úložiště jedné barvy v paletě barev
    /// </summary>
    internal class RtfColor
    {
        /// <summary>
        /// Konstruktor barvy
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        public RtfColor(int index, Color color)
        {
            this.ColorCode = index.ToString();
            this.ColorValue = color;
        }
        /// <summary>
        /// Obsahuje číslenou část kódu barvy = text "0" až "199"
        /// </summary>
        public string ColorCode { get; private set; }
        /// <summary>
        /// Obsahuje barvu. Pokud je IsEmpty, jde o default barvu která se do tabulky nevypisuje (její hodnoty), ale jen se z aní vkládá středník.
        /// </summary>
        public Color ColorValue { get; private set; }
        /// <summary>
        /// Značka fontu do flow textu: \cf1   (pro barvu ForeColor)
        /// </summary>
        public string RtfMarkF { get { return @"\cf" + this.ColorCode; } }
        /// <summary>
        /// Značka fontu do flow textu: \cb1   (pro barvu BackColor)
        /// </summary>
        public string RtfMarkB { get { return @"\cb" + this.ColorCode; } }
        /// <summary>
        /// Značka fontu do flow textu: \highlight1   (pro barvu Highlight = BackColor)
        /// </summary>
        public string RtfMarkH { get { return @"\highlight" + this.ColorCode; } }
        /// <summary>
        /// Text barvy v RTF kódu: "red0\green128\blue64;"
        /// </summary>
        public string RtfTable
        {
            get
            {
                if (this.ColorValue.IsEmpty)
                    return ";";
                return "\\red" + this.ColorValue.R.ToString() + "\\green" + this.ColorValue.G.ToString() + "\\blue" + this.ColorValue.B.ToString() + ";";
            }
        }
    }
    #endregion
    #endregion
}
