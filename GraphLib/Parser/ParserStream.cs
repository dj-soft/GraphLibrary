using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.TextParser
{
    // tento soubor se může z definitivní verze smazat, zdejší třídy jsou jen testovací.

    #region class ParserStream : stream položek (ParserSegmentValue) nad parsovaným SQL textem. Řeší čtení položek, ignorování blank a komentářů, řeší Preview (čtení bez posunu).
    /// <summary>
    /// ParserStream : stream položek (ParserSegmentValue) nad parsovaným SQL textem.
    /// Řeší čtení položek, ignorování blank a komentářů, řeší Preview (čtení bez posunu).
    /// </summary>
    public class ParserStream
    {
        #region Konstrukce streamu, data
        /// <summary>
        /// Konstruktor z dodaného textu
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public static ParserStream ParseSqlCommand(string sqlCommand)
        {
            List<ParserSegment> segments = Parser.ParseString(sqlCommand, ParserDefaultSetting.MsSql);
            return new ParserStream(segments);
        }
        /// <summary>
        /// Konstruktor z dodaných segmentů
        /// </summary>
        /// <param name="segments"></param>
        public ParserStream(List<ParserSegment> segments)
        {
            this.Values = new List<ParserSegmentValue>();
            this.FillValues(segments);
            // this.Segment = segments[0];
            this.Pointer = 0;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = "";
            for (int i = this.Pointer; i < this.Pointer + 12; i++)
            {
                if (i >= this.Values.Count) break;
                text += this.Values[i].Text + " ";
            }
            return text.Trim();
        }
        /// <summary>
        /// Položky SQL streamu.
        /// Jde o hodnoty (Value) z dodaného segmentu = první úroveň SQL textu.
        /// Tzn. případné vnořené příkazy nebo výrazy (v závorkách, apostrofech, atd) jsou zde uloženy v jedné položce Values, která je typu Segment a obsahuje svůj segment a jeho Values (rekurzivně...).
        /// V tomto seznamu nejsou vloženy hodnoty typu Blank anebo Komentář.
        /// V tomto seznamu jsou v textových prvcích, které nejsou KEYWORD, v případě potřeby sloučené texty oddělené mezerami a tečkami 
        /// (řeší se tím speciální možnost SQL psát názvy prvků například: "lcs . [product_order] . [column] ")
        /// </summary>
        public List<ParserSegmentValue> Values { get; private set; }
        /// <summary>
        /// Ukazatel na aktuální segment. Úvodní hodnota = 0.
        /// </summary>
        public int Pointer { get; set; }
        #endregion
        #region Příprava streamu = načítání Values
        /// <summary>
        /// Načte do sebe všechny hodnoty z dodaných segmentů
        /// </summary>
        /// <param name="segments"></param>
        protected void FillValues(IEnumerable<ParserSegment> segments)
        {
            foreach (ParserSegment segment in segments)
                this.FillValues(segment);
        }
        /// <summary>
        /// Načte do sebe všechny hodnoty z dodaného segmentu
        /// </summary>
        /// <param name="segment"></param>
        protected void FillValues(ParserSegment segment)
        {
            ParserSegmentValue mergeText = null;                            // Value, do které mergujeme texty a tečky
            FillValueChainState endState = FillValueChainState.None;        // Aktuální (=předchozí) stav, může mít hodnoty pouze: None, IsText a IsDot. Jiné hodnoty nenabývá.  Hodnotu IsDot má i tehdy, když byl naposledy Text, který končil tečkou (například "lcs.")
            foreach (ParserSegmentValue value in segment.Values)
            {
                string content;
                FillValueChainState currState = DetectChainState(value);    // Čemu odpovídá aktuální prvek. Hodnotu IsDot má tehdy, když tečkou začíná (nebo to je tečka).
                FillValueChainState nextState;
                switch (currState)
                {
                    case FillValueChainState.None:
                    case FillValueChainState.Ignore:
                        break;

                    case FillValueChainState.IsText:
                        // Nyní je text, který nezačíná tečkou (pozor: může jít i o segment SYSNAME, který ale nemá Content!) :
                        content = value.Text.Trim();
                        nextState = (content.EndsWith(".") ? FillValueChainState.IsDot : FillValueChainState.IsText);
                        switch (endState)
                        {   // lastState může být pouze None, IsText a IsDot:
                            case FillValueChainState.None:
                                // Dosud nebylo nic, teď je text:
                                this.Values.Add(value);
                                mergeText = value;
                                break;
                            case FillValueChainState.IsText:
                                // Dosud byl text (nekončil tečkou), nyní je text (nezačíná tečkou) = dva oddělené texty:
                                this.Values.Add(value);
                                mergeText = value;
                                break;
                            case FillValueChainState.IsDot:
                                // Dosud byl text končící tečkou, nyní je text (nezačíná tečkou):
                                mergeText.AddContent(content, content.Length, true);
                                break;
                        }
                        endState = nextState;
                        break;

                    case FillValueChainState.IsDot:
                        // Nyní je to text, který začíná tečkou (tím pádem to nebude segment SYSNAME, ten tečkou nikdy nezačíná):
                        content = value.Content.Trim();
                        nextState = (content.EndsWith(".") ? FillValueChainState.IsDot : FillValueChainState.IsText);
                        switch (endState)
                        {   // lastState může být pouze None, IsText a IsDot:
                            case FillValueChainState.None:
                                // Dosud nebylo nic, teď je tečka [a text]:
                                this.Values.Add(value);
                                mergeText = value;
                                break;
                            case FillValueChainState.IsText:
                                // Dosud byl text (nekončil tečkou), teď je tečka [a text]:
                                mergeText.AddContent(content, content.Length, true);
                                break;
                            case FillValueChainState.IsDot:
                                // Dosud byl text končící tečkou, nyní je text (nezačíná tečkou):
                                mergeText.AddContent(content, content.Length, true);
                                break;
                        }
                        endState = nextState;
                        break;

                    case FillValueChainState.Other:
                        // Nyní je něco jiného:
                        this.Values.Add(value);
                        mergeText = null;
                        endState = FillValueChainState.None;
                        break;

                }
            }
        }
        /// <summary>
        /// Určí, zda daný prvek je možno řetězit, a případně jakou roli v řetězu má (nic / text / tečka).
        /// Hodnotu IsDot vrací pro SimpleText, který začíná tečkou (nebo je to pouze tečka).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static FillValueChainState DetectChainState(ParserSegmentValue value)
        {
            if (value == null)
                return FillValueChainState.None;

            if (value.ValueType == ParserSegmentValueType.Blank)
                return FillValueChainState.Ignore;
            if (value.ValueType == ParserSegmentValueType.InnerSegment && value.HasInnerSegment && (value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_COMMENTBLOCK || value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_COMMENTLINE))
                return FillValueChainState.Ignore;
            if (value.ValueType == ParserSegmentValueType.Text && value.ValueName != Parser.VALUENAME_KEYWORD && value.HasContent)
                return ((value.Content.Trim().StartsWith(".")) ? FillValueChainState.IsDot : FillValueChainState.IsText);
            else if (value.HasInnerSegment && value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_SYSNAME)
                return FillValueChainState.IsText;
            return FillValueChainState.Other;
        }
        /// <summary>
        /// Stav hodnoty z hlediska možného slučování textů a teček mezi texty
        /// </summary>
        protected enum FillValueChainState
        {
            /// <summary>Neurčeno</summary>
            None,
            /// <summary>Ignorovat</summary>
            Ignore,
            /// <summary>Je to text</summary>
            IsText,
            /// <summary>Je to tečka</summary>
            IsDot,
            /// <summary>Je to něco jiného</summary>
            Other
        }
        /// <summary>
        /// Vrací true, pokud daná hodnota (ParserSegmentValue) je Blank
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsBlank(ParserSegmentValue value)
        {
            if (value == null) return false;
            return (value.ValueType == ParserSegmentValueType.Blank);
        }
        /// <summary>
        /// Vrací true, pokud daná hodnota (ParserSegmentValue) je komentář
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsComment(ParserSegmentValue value)
        {
            if (value == null) return false;
            return (value.ValueType == ParserSegmentValueType.InnerSegment && value.HasInnerSegment &&
                      (value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_COMMENTBLOCK ||
                       value.InnerSegment.SegmentName == ParserDefaultSetting.SQL_COMMENTLINE));
        }
        /// <summary>
        /// Vrací true, pokud daná hodnota (ParserSegmentValue) je prostý text, nikoli KEYWORD
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsSimpleText(ParserSegmentValue value)
        {
            if (value == null) return false;
            return (value.ValueType == ParserSegmentValueType.Text && value.ValueName != Parser.VALUENAME_KEYWORD && value.HasContent);
        }
        #endregion
        #region Detekce slov ve streamu, přeskočení slov ve streamu
        /// <summary>
        /// Metoda zjistí, zda na aktuální pozici streamu se nachází některé z předaných klíčových slov/sousloví.
        /// Metoda neposouvá stream, používá Preview.
        /// Zadání klíčových slov: jeden string. Může obsahovat více hledaných slov/sousloví, odělených čárkou.
        /// Jedna položka může mít více slov, oddělené mezerou.
        /// Jedno slovo může být nahrazeno otazníkem = na jeho místě může být libovolná položka (i vnořený segment, v závorce).
        /// Typické zadání může být tedy: string found = stream.BeginWithKeyword("INNER ? JOIN, LEFT JOIN, RIGHT JOIN").
        /// Metoda vrací příznak nalezení slova (true / false).
        /// </summary>
        /// <param name="keyWords"></param>
        /// <returns></returns>
        public bool StartsWithKeyword(string keyWords)
        {
            string keyWord = FindInitialKeyword(keyWords);
            return (keyWord != null);
        }
        /// <summary>
        /// Metoda zjistí, zda na aktuální pozici streamu se nachází některé z předaných klíčových slov/sousloví.
        /// Metoda neposouvá stream, používá Preview.
        /// Zadání klíčových slov: jeden string. Může obsahovat více hledaných slov/sousloví, odělených čárkou.
        /// Jedna položka může mít více slov, oddělené mezerou.
        /// Jedno slovo může být nahrazeno otazníkem = na jeho místě může být libovolná položka (i vnořený segment, v závorce).
        /// Typické zadání může být tedy: string found = stream.BeginWithKeyword("INNER ? JOIN, LEFT JOIN, RIGHT JOIN").
        /// Metoda vrací příznak nalezení slova (true / false), a nalezené klíčové slovo ukládá do out parametru keyWord.
        /// </summary>
        /// <param name="keywords">Přípustná slova</param>
        /// <param name="keyword">Nalezené slovo nebo null</param>
        /// <returns></returns>
        public bool StartsWithKeyword(string keywords, out string keyword)
        {
            keyword = FindInitialKeyword(keywords);
            return (keyword != null);
        }
        /// <summary>
        /// Metoda zjistí, zda na aktuální pozici streamu se nachází některé z předaných klíčových slov/sousloví.
        /// Metoda neposouvá stream, používá Preview.
        /// Zadání klíčových slov: jeden string. Může obsahovat více hledaných slov/sousloví, odělených čárkou.
        /// Jedna položka může mít více slov, oddělené mezerou.
        /// Jedno slovo může být nahrazeno otazníkem = na jeho místě může být libovolná položka (i vnořený segment, v závorce).
        /// Typické zadání může být tedy: string found = stream.BeginWithKeyword("INNER ? JOIN, LEFT JOIN, RIGHT JOIN").
        /// Metoda vrací klíčové slovo, které nalezla, nebo vrací null když nenalezla nic.
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns></returns>
        public string FindInitialKeyword(string keywords)
        {
            if (keywords == null) return null;

            // Rozeberu zadaný text na prvky:
            List<KeywordPhrase> phrases = new List<KeywordPhrase>();      // Jedna položka Listu = jedno sousloví (obsahuje pole oddělených slov { "LEFT", "OUTER" }).
            string[] separator = new string[] { KEYWORDS_SEPARATOR };
            string[] keyws = keywords.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string phrase in keyws)
                phrases.Add(new KeywordPhrase(phrase));
            int phraseCount = phrases.Count;
            if (phraseCount == 0) return null;
            if (phraseCount > 1)
                phrases.Sort(KeywordPhrase.CompareByCountDesc);
            int maxWordCount = phrases[0].WordCount;                // Nejvyšší počet slov ze všech sousloví je v první položce (jsou setříděné podle WordCount DESC)

            // Načtu si potřebný počet Value, načítám metodou PeekValues() = v Preview modu:
            List<ParserSegmentValue> streamValues = this.PeekValues(maxWordCount);

            // Projdu si sousloví od nejdelšího, a prověřím zda aktuální stream obsahuje jeho požadovaná slova:
            foreach (KeywordPhrase phrase in phrases)
            {
                if (phrase.IsEquivalent(streamValues))
                    return phrase.Phrase;
            }
            return null;
        }
        /// <summary>
        /// Metoda přeskočí ve streamu přes zadané klíčové slovo (které může být víceslovné, například "ORDER BY") a nechá stream stát na první Value za tímto slovem. Vrací true.
        /// Pokud stream obsahuje něco jiného, nechá stream stát na prvním nesouhlasném prvku a vrátí false.
        /// </summary>
        /// <param name="keyword">Klíčové slovo, které se má přeskočit. Může být víceslovné, například "ORDER BY".</param>
        /// <returns></returns>
        public bool SkipKeyword(string keyword)
        {
            List<ParserSegmentValue> values;
            return this.SkipKeyword(keyword, out values);
        }
        /// <summary>
        /// Metoda přeskočí ve streamu přes zadané klíčové slovo (které může být víceslovné, například "ORDER BY") a nechá stream stát na první Value za tímto slovem. Vrací true.
        /// Pokud stream obsahuje něco jiného, nechá stream stát na prvním nesouhlasném prvku a vrátí false.
        /// </summary>
        /// <param name="keyword">Klíčové slovo, které se má přeskočit. Může být víceslovné, například "ORDER BY".</param>
        /// <param name="values">Výstup Values, které byly nalezeny a přeskočeny</param>
        /// <returns></returns>
        public bool SkipKeyword(string keyword, out List<ParserSegmentValue> values)
        {
            values = null;
            if (String.IsNullOrEmpty(keyword)) return false;
            values = new List<ParserSegmentValue>();
            if (keyword == "?") return true;
            KeywordPhrase phrase = new KeywordPhrase(keyword);
            for (int i = 0; i < phrase.WordCount; i++)
            {
                if (this.EndOfStream) return false;
                ParserSegmentValue value = this.PeekValue();
                if (!phrase.IsEquivalent(value, i)) return false;
                values.Add(value);
                this.SkipValue();
            }
            return true;
        }
        /// <summary>
        /// Oddělovač klíčových sousloví.
        /// Jedno klíčové slovo se může skládat z více slov, například "LEFT OUTER JOIN".
        /// Do vyhledávacích metod lze předat jejich sadu, kde jednotlivá sousloví jsou oddělena tímto separátorem (středník). 
        /// Například: "LEFT OUTER JOIN; RIGHT JOIN; LEFT ? JOIN" a pod.
        /// Středník (v této verzi). Používat tuto konstantu, prosím.
        /// </summary>
        public const string KEYWORDS_SEPARATOR = ";";
        #endregion
        #region Čtení ze streamu
        /// <summary>
        /// Metoda najde v aktuálním streamu nejbližší významný prvek a vrátí jej.
        /// Metoda vždy posouvá ukazatel na následující prvek (ale nijak neřeší, co je ten další prvek zač).
        /// Metoda nevrací prvky typu Blank, a nevrací ani segmenty typu Komentář.
        /// Vrací prvky (Value), které obsahují vnitřní segmenty = typicky vnitřní závorky, 
        /// anebo prvky čistého textu.
        /// Metoda může vrátit null, pokud dojde na konec streamu a nic nenajde.
        /// </summary>
        /// <returns></returns>
        public ParserSegmentValue ReadValue()
        {
            return (this._GetValue(true));
        }
        /// <summary>
        /// Metoda najde v aktuálním streamu nejbližší významný prvek a vrátí jej.
        /// Tato metoda NEPOSOUVÁ ukazatel, na rozdíl od metody ReadValue() (která ukazatel posune).
        /// Metoda nevrací prvky typu Blank, a nevrací ani segmenty typu Komentář.
        /// Vrací prvky (Value), které obsahují vnitřní segmenty = typicky vnitřní závorky, 
        /// anebo prvky čistého textu.
        /// Metoda může vrátit null, pokud dojde na konec streamu a nic nenajde.
        /// </summary>
        /// <returns></returns>
        public ParserSegmentValue PeekValue()
        {
            return (this._GetValue(false));
        }
        /// <summary>
        /// Přejde na další nejbližší významný prvek, nevrací jej, pouze posouvá pointer.
        /// Vrací true, pokud se další položka našla.
        /// </summary>
        /// <returns></returns>
        public bool SkipValue()
        { 
            ParserSegmentValue value = this._GetValue(true);
            return (value != null);
        }
        /// <summary>
        /// Přeskočí zadaný počet prvků a posune se na následující položku, pouze posouvá pointer.
        /// Vrací true, pokud se podařilo přejít na požadovanou položku, nebo false když žádná další není.
        /// </summary>
        /// <returns></returns>
        public bool SkipValues(int count)
        {
            for (int c = 0; c < count; c++)
            {
                ParserSegmentValue value = this._GetValue(true);
                if (value == null)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Metoda najde v aktuálním streamu nejbližší významné prvky v požadovaném počtu a vrátí je.
        /// Tato metoda POSOUVÁ ukazatel, na rozdíl od metody PeekValues() (která ukazatel neposune).
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<ParserSegmentValue> ReadValues(int count)
        {
            return this._GetValues(count);
        }
        /// <summary>
        /// Metoda najde v aktuálním streamu nejbližší významné prvky v požadovaném počtu a vrátí je.
        /// Tato metoda NEPOSOUVÁ ukazatel, na rozdíl od metody ReadValues() (která ukazatel posune).
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<ParserSegmentValue> PeekValues(int count)
        {
            using (this.Preview())
            {
                return this._GetValues(count);
            }
        }
        /// <summary>
        /// Metoda najde v aktuálním streamu nejbližší významné prvky v požadovaném počtu a vrátí je.
        /// Tato metoda POSOUVÁ ukazatel. Volající metoda může volání obalit do Preview modu a ukazatel tak po skončení metody vrátit.
        /// </summary>
        /// <param name="count">Požadovaný počet prvků. Může být vráceno méně, když nebudou nalezeny další.</param>
        /// <returns></returns>
        private List<ParserSegmentValue> _GetValues(int count)
        {
            List<ParserSegmentValue> result = new List<ParserSegmentValue>();
            for (int c = 0; c < count; c++)
            {
                ParserSegmentValue value = this._GetValue(true);
                if (value == null)
                    break;
                result.Add(value);
            }
            return result;
        }
        /// <summary>
        /// Metoda najde a vrátí nejbližší významný prvek ve streamu.
        /// Volitelně aktualizuje Pointer - podle parametru setPointer: true posouvá pointer, false neposouvá.
        /// </summary>
        /// <returns></returns>
        private ParserSegmentValue _GetValue(bool setPointer)
        {
            if (this.EndOfStream) return null;

            // Protože Stream obsahuje jen významově důležité prvky (neobsahuje Blank ani Comment), mohu zde vzít bezprostředně nejbližší prvek:
            ParserSegmentValue result = this.Values[this.Pointer];

            if (setPointer)
                this.Pointer++;

            return result;
        }
        /// <summary>
        /// Příznak, že tento stream už je za koncem vstupního textu.
        /// Porovnává aktuální pointer this.Pointer proti seznamu this.Values.
        /// </summary>
        public bool EndOfStream { get { return this.IsEndOfStream(this.Pointer); } }
        /// <summary>
        /// Příznak, že tento stream už je za koncem vstupního textu.
        /// Porovnává aktuální pointer this.Pointer proti seznamu this.Values.
        /// </summary>
        public bool IsEndOfStream(int pointer)
        {
            return (pointer < 0 || pointer >= this.Values.Count);
        }
        #endregion
        #region Preview only
        /// <summary>
        /// Zahajuje blok příkazů using() { ... }, kdy uvnitř bloku lze libovolně pohybovat streamem, a na konci bloku se stream vrátí do výchozí pozice.
        /// Typické využití je v metodách IsMyPart(stream).
        /// Optimální použití je: using (stream.Preview() { testy streamu; } return; .
        /// </summary>
        /// <returns></returns>
        public IDisposable Preview()
        {
            return new _SqlStreamPreview(this.Values, this.Pointer, this._PreviewEnd);
        }
        /// <summary>
        /// Na konci života objektu Preview: vrátí segment i pointer na hodnoty uložené při startu.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="pointer"></param>
        private void _PreviewEnd(List<ParserSegmentValue> values, int pointer)
        {
            this.Values = values;
            this.Pointer = pointer;
        }
        /// <summary>
        /// IDisposable třída ukládající návratové hodnoty pro Preview streamu.
        /// </summary>
        private class _SqlStreamPreview : IDisposable
        {
            public _SqlStreamPreview(List<ParserSegmentValue> values, int pointer, Action<List<ParserSegmentValue>, int> onDispose)
            {
                this._Values = values;
                this._Pointer = pointer;
                this._OnDispose = onDispose;
            }
            private List<ParserSegmentValue> _Values;
            private int _Pointer;
            private Action<List<ParserSegmentValue>, int> _OnDispose;
            void IDisposable.Dispose()
            {
                this._OnDispose(this._Values, this._Pointer);
                this._Values = null;
            }
        }
        #endregion
        #region SubStream
        /// <summary>
        /// Vrátí podmnožinu tohoto streamu, v daném intervalu (first, count)
        /// </summary>
        /// <param name="first"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public IEnumerable<ParserSegmentValue> SubPart(int first, int length)
        {
            return this.Values.GetRange(first, length);
        }
        /// <summary>
        /// Vrátí text z dodaných položek streamu
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string ToText(IEnumerable<ParserSegmentValue> values)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ParserSegmentValue value in values)
                sb.Append(value.Text + " ");
            return sb.ToString().Trim();
        }
        #endregion
    }
    #endregion
    #region class KeywordPhrase : sousloví a jeho slova a metody (CompareByCountDesc(), IsEquivalent())
    /// <summary>
    /// KeywordPhrase : sousloví a jeho slova a metody (CompareByCountDesc(), IsEquivalent())
    /// </summary>
    public class KeywordPhrase
    {
        /// <summary>
        /// Konstruktor. 
        /// Na vstupu se očekává text například "ORDER BY", "LEFT OUTER ? JOIN", "WHERE", atd.
        /// </summary>
        /// <param name="phrase"></param>
        public KeywordPhrase(string phrase)
        {
            this._Fill(phrase, new char[] { ' ' });
        }
        /// <summary>
        /// Konstruktor. 
        /// Na vstupu se očekává text například "ORDER BY", "LEFT OUTER ? JOIN", "WHERE", atd; a sada separátorů (defaultní je mezera).
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="separators"></param>
        public KeywordPhrase(string phrase, char[] separators)
        {
            this._Fill(phrase, separators);
        }
        private void _Fill(string phrase, char[] separators)
        {
            string p = phrase.Trim();
            this.Phrase = p;
            string[] words = p.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            this.WordCount = words.Length;
            this.Words = new string[this.WordCount];
            for (int w = 0; w < this.WordCount; w++)
                this.Words[w] = words[w].Trim();
        }
        /// <summary>
        /// Vstupní text fráze
        /// </summary>
        public string Phrase { get; private set; }
        /// <summary>
        /// Fráze separovaná na slova
        /// </summary>
        public string[] Words { get; private set; }
        /// <summary>
        /// Počet slov fráze
        /// </summary>
        public int WordCount { get; private set; }
        /// <summary>
        /// Normalizovaná fráze (velkými písmeny, mezi slovy jedna mezera)
        /// </summary>
        public string PhraseNormalised
        {
            get
            {
                string result = "";
                foreach (string w in this.Words)
                    result += (result.Length == 0 ? "" : " ") + w.Trim().ToUpper();
                return result;
            }
        }
        /// <summary>
        /// Porovná dvě hodnoty podle WordCount, sestupně (pro třídění).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareByCountDesc(KeywordPhrase a, KeywordPhrase b)
        {
            return b.WordCount.CompareTo(a.WordCount);
        }
        /// <summary>
        /// Zjistí, zda dodaný seznam hodnot ze streamu obsahuje zde definovaná slova.
        /// </summary>
        /// <param name="streamValues"></param>
        /// <returns></returns>
        public bool IsEquivalent(List<ParserSegmentValue> streamValues)
        {
            if (this.WordCount > streamValues.Count) return false;          // streamValues je sice přednačtený na potřebný počet slov, ale fyzicky ve streamu nemusí být dostatek textu na potřebný počet - takže může být kratší.
            for (int w = 0; w < this.WordCount; w++)
            {
                ParserSegmentValue value = streamValues[w];
                if (!this.IsEquivalent(value, w))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Vrátí true, pokud daná hodnota (value) odpovídá slovu na dané pozici
        /// </summary>
        /// <param name="value"></param>
        /// <param name="wordIndex"></param>
        /// <returns></returns>
        public bool IsEquivalent(ParserSegmentValue value, int wordIndex)
        {
            if (value == null) return false;
            string word = this.Words[wordIndex];
            if (word == "?") return true;
            if (value.ValueType == ParserSegmentValueType.Delimiter)
                return (value.Content == word);
            else
                return value.ContainText(word, true);
        }
    }
    #endregion

}
