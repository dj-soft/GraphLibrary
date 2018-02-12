using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Djs.Common.TextParser
{
    #region SyntacticSetting
    /// <summary>
    /// Slovník všech sekvencí které jsou pro aktuální jazyk k dispozici
    /// </summary>
    public class SyntacticSettingLibrary
    {
        public SyntacticSettingLibrary()
        {
            this._SequenceDict = new Dictionary<string, SyntacticSettingSequence>();
        }
        private Dictionary<string, SyntacticSettingSequence> _SequenceDict;
        #region Načtení stringu definujícího syntaxi konkrétního jazyka, syntaxe je definovaná ve formátu MSDN
        /// <summary>
        /// Načte string definující syntaxi konkrétního jazyka. 
        /// Syntaxe je definovaná ve formátu MSDN: 
        /// "&lt;SELECT statement&gt; ::=  SELECT [ ALL | DISTINCT ] [ TOP ( expression ) [ PERCENT ] [ WITH TIES ] ] <select_list>  [ INTO new_table ] 
        /// [ FROM { <table_source> } [ ,...n ] ] [ WHERE <search_condition> ] [ <GROUP BY> ]  [ HAVING < search_condition > ] "
        /// </summary>
        /// <param name="syntax"></param>
        public void LoadMsdnSyntax(string syntax, bool showExceptions)
        {
            if (!showExceptions)
            {
                this.LoadMsdnSyntax(syntax);
            }
            else
            {
                try
                {
                    this.LoadMsdnSyntax(syntax);
                }
                catch (ParserProcessException ppe)
                {
                    string info = ppe.Message;
                    if (ppe.Position.HasValue)
                    {
                        int ix = ppe.Position.Value - 60;
                        if (ix < 0) ix = 0;
                        int len = 120;
                        if (ix + len > syntax.Length)
                            len = syntax.Length - ix;
                        string sample = syntax.Substring(ix, len);

                        info += Environment.NewLine + sample;
                    }

                    System.Windows.Forms.MessageBox.Show(info, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
                catch (Exception exc)
                {
                    System.Windows.Forms.MessageBox.Show(exc.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
        }
        /// <summary>
        /// Načte string definující syntaxi konkrétního jazyka. 
        /// Syntaxe je definovaná ve formátu MSDN: 
        /// "&lt;SELECT statement&gt; ::=  SELECT [ ALL | DISTINCT ] [ TOP ( expression ) [ PERCENT ] [ WITH TIES ] ] <select_list>  [ INTO new_table ] 
        /// [ FROM { <table_source> } [ ,...n ] ] [ WHERE <search_condition> ] [ <GROUP BY> ]  [ HAVING < search_condition > ] "
        /// </summary>
        /// <param name="syntax"></param>
        public void LoadMsdnSyntax(string syntax)
        {
            this._SequenceDict.Clear();

            ParserSetting setting = ParserDefaultSetting.Msdn;
            ParserSegment segment = Parser.ParseString(syntax, setting).FirstOrDefault();
            if (segment != null && segment.ValueList != null)
            {
                List<Tuple<ParserSegmentValue, List<ParserSegmentValue>>> commands = this.SplitToCommands(segment.ValueList);
                foreach (Tuple<ParserSegmentValue, List<ParserSegmentValue>> command in commands)
                    this.ProcessCommand(command);
            }
        }
        /// <summary>
        /// Rozdělí prvky ParserSegmentValue do jednotlivých příkazů, kde příkaz má jméno před oddělovačem "::=" a seznam prvků je za tímto oddělovačem.
        /// Vynechává Blank a komentáře.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private List<Tuple<ParserSegmentValue, List<ParserSegmentValue>>> SplitToCommands(List<ParserSegmentValue> values)
        {
            List<Tuple<ParserSegmentValue, List<ParserSegmentValue>>> commands = new List<Tuple<ParserSegmentValue, List<ParserSegmentValue>>>();
            int count = values.Count;
            int from = 0;
            int nextAssign = -1;
            int nextVariable = -1;
            do
            {
                // 1. Najdu nejbližší pozici ::= buď ji mám jako kladnou hodnotu uchovanou od minula v nextAssign, nebo ji najdu počínaje od from:
                int currAssign = (nextAssign > 0 ? nextAssign : values.FindIndex(from, v => v.ValueType == ParserSegmentValueType.Delimiter && v.Text == "::="));
                if (currAssign < 0) break;

                // 2. Najdu nejbližší segment se jménem Variable před :== (je o jméno commandu), může být uchovaná od minula nebo ji najdu nyní (jde jen o poprvé):
                int currVariable = (nextVariable >= 0 ? nextVariable : values.FindLastIndex(currAssign - 1, v => v.ValueType == ParserSegmentValueType.InnerSegment && v.InnerSegment.SegmentName == ParserDefaultSetting.MSDN_VARIABLE));
                if (currVariable < 0) break;

                // 3. Najdu pozici nejbližšího následujího ::=, což je přiřazení dalšího commandu, počínaje za nalezeným:
                nextAssign = values.FindIndex(currAssign + 1, v => v.ValueType == ParserSegmentValueType.Delimiter && v.Text == "::=");

                // 4. Najdu pozici názvu následující proměnné = těsně před následujícím přiřazením:
                nextVariable = (nextAssign < 0 ? -1 : values.FindLastIndex(nextAssign - 1, v => v.ValueType == ParserSegmentValueType.InnerSegment && v.InnerSegment.SegmentName == ParserDefaultSetting.MSDN_VARIABLE));
                if (nextVariable > 0 && nextVariable < currAssign) nextVariable = -1;              // Pokud bych (pozpátku) našel položku <variable> až před naším přiřazením, tak ji neberu a tvářím se jako že jsem nenašel nic.

                // 5. Sestavím aktuální command (našli jsme jméno proměnné, máme pozici přiřazení v currAssign, pozici názvu příští proměnné máme v nextVariable, takže mezi tím se nachází prvky, které patří do příkazu:
                ParserSegmentValue variable = values[currVariable];
                List<ParserSegmentValue> items = new List<ParserSegmentValue>();
                int toItem = (nextVariable > 0 ? nextVariable : count);                            // Projdu prvky do (příštího názvu proměnné nebo do konce) - 1:
                for (int i = currAssign + 1; i < toItem; i++)
                {
                    ParserSegmentValue item = values[i];
                    // Vynechávám Blank a Comment:
                    if (item.ValueType == ParserSegmentValueType.Blank || (item.ValueType == ParserSegmentValueType.InnerSegment && item.InnerSegment.IsComment)) continue;
                    items.Add(item);
                }
                Tuple<ParserSegmentValue, List<ParserSegmentValue>> command = new Tuple<ParserSegmentValue, List<ParserSegmentValue>>(variable, items);
                commands.Add(command);

                // 6. Pokud jsme NEnašli následující proměnnou, tak skončíme:
                if (nextVariable <= 0) break;
            } while (true);
            return commands;
        }
        /// <summary>
        /// Zpracuje jeden dodaný příkaz: zpracuje jej do SyntacticSettingSequence a uloží jej do slovníku this._SequenceDict.
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(Tuple<ParserSegmentValue, List<ParserSegmentValue>> command)
        {
            string commandName = command.Item1.Text;
            List<ParserSegmentValue> commandItems = command.Item2;
            this._SequenceDict.Add(commandName, null);
        }
        #endregion



        #region Sample pro testování: Deklarace jazyka T-SQL ve formátu MSDN
        public const string T_SQL_SETTING = @"
<SELECT statement> ::=  
    [WITH <common_table_expression> [,...n]]
    <query_expression> 
    [ ORDER BY { order_by_expression | column_position [ ASC | DESC ] } 
  [ ,...n ] ] 
    [ <FOR Clause>] 
    [ OPTION ( <query_hint> [ ,...n ] ) ] 

<query_expression> ::= 
    { <query_specification> | ( <query_expression> ) } 
    [  { UNION [ ALL ] | EXCEPT | INTERSECT }
        <query_specification> | ( <query_expression> ) [...n ] ] 

<query_specification> ::= 
SELECT [ ALL | DISTINCT ] 
    [TOP ( expression ) [PERCENT] [ WITH TIES ] ] 
    < select_list > 
    [ INTO new_table ] 
    [ FROM { <table_source> } [ ,...n ] ] 
    [ WHERE <search_condition> ] 
    [ <GROUP BY> ] 
    [ HAVING < search_condition > ] 

<select_list> ::= 
    { 
      * 
      | { table_name | view_name | table_alias }.* 
      | {
          [ { table_name | view_name | table_alias }. ]
               { column_name | $IDENTITY | $ROWGUID } 
          | udt_column_name [ { . | :: } { { property_name | field_name } 
            | method_name ( argument [ ,...n] ) } ]
          | expression
          [ [ AS ] column_alias ] 
         }
      | column_alias = expression 
    } [ ,...n ] 

<table_source> ::= 
{
    table_or_view_name [ [ AS ] table_alias ] [ <tablesample_clause> ] 
        [ WITH ( < table_hint > [ [ , ]...n ] ) ] 
    | rowset_function [ [ AS ] table_alias ] 
        [ ( bulk_column_alias [ ,...n ] ) ] 
    | user_defined_function [ [ AS ] table_alias ] ]
    | OPENXML <openxml_clause> 
    | derived_table [ AS ] table_alias [ ( column_alias [ ,...n ] ) ] 
    | <joined_table> 
    | <pivoted_table> 
    | <unpivoted_table>
      | @variable [ [ AS ] table_alias ]
        | @variable.function_call ( expression [ ,...n ] ) [ [ AS ] table_alias ] [ (column_alias [ ,...n ] ) ]
}
<tablesample_clause> ::=
    TABLESAMPLE [SYSTEM] ( sample_number [ PERCENT | ROWS ] ) 
        [ REPEATABLE ( repeat_seed ) ] 

<joined_table> ::= 
{
    <table_source> <join_type> <table_source> ON <search_condition> 
    | <table_source> CROSS JOIN <table_source> 
    | left_table_source { CROSS | OUTER } APPLY right_table_source 
    | [ ( ] <joined_table> [ ) ] 
}
<join_type> ::= 
    [ { INNER | { { LEFT | RIGHT | FULL } [ OUTER ] } } [ <join_hint> ] ]
    JOIN

<pivoted_table> ::=
    table_source PIVOT <pivot_clause> [ AS ] table_alias

<pivot_clause> ::=
        ( aggregate_function ( value_column [ [ , ]...n ]) 
        FOR pivot_column 
        IN ( <column_list> ) 
    ) 

<unpivoted_table> ::=
    table_source UNPIVOT <unpivot_clause> [ AS ] table_alias

<unpivot_clause> ::=
        ( value_column FOR pivot_column IN ( <column_list> ) ) 

<column_list> ::=
          column_name [ ,...n ]


<group by spec> ::=
    <group by item> [ ,...n ]

<group by item> ::=
    <simple group by item>
    | <rollup spec>
    | <cube spec>
    | <grouping sets spec>
    | <grand total>

<simple group by item> ::=
    <column_expression>

<rollup spec> ::=
    ROLLUP ( <composite element list> ) 

<cube spec> ::=
    CUBE ( <composite element list> ) 

<composite element list> ::=
    <composite element> [ ,...n ]

<composite element> ::=
    <simple group by item>
    | ( <simple group by item list> ) 

<simple group by item list> ::=
    <simple group by item> [ ,...n ]

<grouping sets spec> ::=
    GROUPING SETS ( <grouping set list> ) 

<grouping set list> ::=
    <grouping set> [ ,...n ]

<grouping set> ::=
    <grand total>
    | <grouping set item>
    | ( <grouping set item list> ) 

<empty group> ::= 
        ( ) 

<grouping set item> ::=
    <simple group by item>
    | <rollup spec>
    | <cube spec>

<grouping set item list> ::=
    <grouping set item> [ ,...n ]


ORDER BY order_by_expression
    [ COLLATE collation_name ] 
    [ ASC | DESC ] 
    [ ,...n ] 
[ <offset_fetch> ]


<offset_fetch> ::=
{ 
    OFFSET { integer_constant | offset_row_count_expression } { ROW | ROWS }
    [
      FETCH { FIRST | NEXT } {integer_constant | fetch_row_count_expression } { ROW | ROWS } ONLY
    ]
}

";
        #endregion
    }
    /// <summary>
    /// Deklarace jedné sekvence příkazů. 
    /// Sekvence představuje časovou posloupnost více frází (vizuálně = na ose X).
    /// </summary>
    public class SyntacticSettingSequence : SyntacticSettingItem
    {
        #region Konstruktor, public property
        public SyntacticSettingSequence()
            : base()
        { }
        public SyntacticSettingSequence(SyntacticSettingLibrary library, SyntacticSettingItem parentItem)
            : base(library, parentItem)
        { }
        public SyntacticSettingSequence(SyntacticSettingItem parentItem, string name, string keyword, bool isCaseSensitive)
            : base(parentItem, keyword, isCaseSensitive)
        {
            this.Name = name;
        }
        /// <summary>
        /// Protected metoda volaná z konstruktoru bázové třídy. Silně nedoporučená technika, protože ještě neběžel konstruktor this třídy.
        /// </summary>
        protected override void Init()
        {
            this._Items = new List<SyntacticSettingItem>();
        }
        /// <summary>
        /// Typ této položky = Sequence
        /// </summary>
        public override SyntacticSettingItemType ItemType { get { return SyntacticSettingItemType.Sequence; } }
        /// <summary>
        /// Název prvku, pochází z jehodefinice. 
        /// Ve standardním definičním formátu je uveden v hranatých závorkách: &lt;SELECT statement&gt;.
        /// Například: SELECT statement, select_list, table_source, search_condition, joined_table, join_type, ...
        /// Typicky: SELECT, FROM, JOIN, INSERT, UPDATE, WHERE...
        /// </summary>
        public string Name { get; protected set; }
        /// <summary>
        /// Souhrn po sobě jdoucích frází. Jedna fráze může mít více variant.
        /// </summary>
        public IEnumerable<SyntacticSettingItem> Items { get { return this._Items; } } private List<SyntacticSettingItem> _Items;
        /// <summary>
        /// Počet položek
        /// </summary>
        public int ItemsCount { get { return this._Items.Count; } }
        /// <summary>
        /// Zkrácený text: "[ FROM { <table_source> } [ ,...n ] ]"
        /// </summary>
        public override string ShortText
        {
            get
            {
                return this.Name;
            }
        }
        /// <summary>
        /// Plný text: "[ FROM { <table_source> } [ ,...n ] ]", obsahuje 
        /// </summary>
        public override string LongText
        {
            get
            {
                string text = "";
                string delimiter = "";
                foreach (SyntacticSettingPhrase phrase in this._Items)
                {
                    text += delimiter + phrase.ShortText;
                    delimiter = " ";
                }
                return text;
            }
        }
        #endregion
        #region SearchWord
        /// <summary>
        /// Vyhledá a vrátí nejbližší položku, která by mohla být daným klíčovým slovem.
        /// Do out parametru searchNext uloží true, pokud sice hledané slovo nebylo nalezeno, 
        /// ale nadřízený algoritmus může hledat dané slovo v dalších strukturách, protože žádná ze zdejších položek nebyla povinná (mohou se přeskočit).
        /// </summary>
        /// <param name="word"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="foundList"></param>
        public override SyntacticSettingItem SearchKeyword(string word, out bool searchNext)
        {
            searchNext = true;
            for (int i = 0; i < this.ItemsCount; i++)
            {
                SyntacticSettingItem item = this._Items[i].SearchKeyword(word, out searchNext);
                if (item != null || !searchNext) return item;
            }
            return null;
        }
        /// <summary>
        /// Vyhledá klíčové slovo odpovídající danému slovu, ale následující v našem seznamu za daným klíčovým slovem (originItem), nikoli od první položky.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="searchNext"></param>
        /// <returns></returns>
        public override SyntacticSettingItem SearchNextKeyword(SyntacticSettingItem originItem, string word, out bool searchNext)
        {
            
            return base.SearchNextKeyword(word, out searchNext);
        }
        #endregion
    }
    /// <summary>
    /// Deklarace jedné fráze příkazu.
    /// Fráze je část sekvence, která může mít (nula - jednu - více) variant (vizuálně = na ose Y), kde každá z těchto variant je představována jednou sekvencí (vizuálně = na ose X).
    /// </summary>
    public class SyntacticSettingPhrase : SyntacticSettingItem
    {
        #region Konstruktor, public property
        public SyntacticSettingPhrase()
            : base()
        { }
        public SyntacticSettingPhrase(SyntacticSettingLibrary library, SyntacticSettingItem parentItem)
            : base(library, parentItem)
        { }
        public SyntacticSettingPhrase(SyntacticSettingItem parentItem, string name, string keyword, bool isCaseSensitive)
            : base(parentItem, keyword, isCaseSensitive)
        {
            this.Name = name;
        }
        /// <summary>
        /// Protected metoda volaná z konstruktoru bázové třídy. Silně nedoporučená technika, protože ještě neběžel konstruktor this třídy.
        /// </summary>
        protected override void Init()
        {
            this._Variations = new List<SyntacticSettingSequence>();
        }
        /// <summary>
        /// Typ této položky = Phrase
        /// </summary>
        public override SyntacticSettingItemType ItemType { get { return SyntacticSettingItemType.Phrase; } }
        /// <summary>
        /// Název prvku, pochází z jeho definice. 
        /// Ve standardním definičním formátu je uveden v hranatých závorkách: &lt;SELECT statement&gt;.
        /// Například: SELECT statement, select_list, table_source, search_condition, joined_table, join_type, ...
        /// Typicky: SELECT, FROM, JOIN, INSERT, UPDATE, WHERE...
        /// </summary>
        public string Name { get; protected set; }
        /// <summary>
        /// Souhrn variant, které tato fráze může mít.
        /// Pokud zde není žádná varianta, pak jde o frázi IsArbitrary = na místě této fráze může být cokoliv.
        /// V takovém případě typicky v textu hledáme začátek následující fráze, a dokud nenajdeme tak vše je součástí této fráze.
        /// </summary>
        public IEnumerable<SyntacticSettingSequence> Variations { get { return this._Variations; } } private List<SyntacticSettingSequence> _Variations;
        /// <summary>
        /// Počet variant
        /// </summary>
        public int VariationCount { get { return this._Variations.Count; } }
        /// <summary>
        /// true pokud tato fráze je libovolná (neobsahuje žádnou definici variant), na jejím místě může být cokoliv, dokud se v textu příkazu nenajde další slovo, které patří až do některé příští fráze.
        /// </summary>
        public bool IsArbitrary { get { return (this.VariationCount == 0); } }
        /// <summary>
        /// Zkrácený text: "[ FROM { <table_source> } [ ,...n ] ]"
        /// </summary>
        public override string ShortText
        {
            get
            {
                bool isMandatory = this.IsMandatory;
                string text = "";
                if (!isMandatory)
                    text += "[ ";
                text += this.Name;
                if (!isMandatory)
                    text += " ]";
                return text;
            }
        }
        /// <summary>
        /// Plný text: "[ FROM { <table_source> } [ ,...n ] ]", obsahuje 
        /// </summary>
        public override string LongText
        {
            get
            {
                bool isMandatory = this.IsMandatory;
                
                string text = "";
                if (!isMandatory)
                    text += "[ ";
                switch (this.VariationCount)
                {
                    case 0:
                        text += "*";
                        break;
                    case 1:
                        text += "{ " + this._Variations[0].ShortText + " }";
                        break;
                    default:
                        text += "{ ";
                        string delimiter = "";
                        foreach (SyntacticSettingSequence variation in this._Variations)
                        {
                            text += delimiter + variation.ShortText;
                            delimiter = " | ";
                        }
                        text += " }";
                        break;
                }
                if (!isMandatory)
                    text += " ]";
                return text;
            }
        }
        #endregion
        #region SearchWord
        /// <summary>
        /// Vyhledá a vrátí nejbližší položku, která by mohla být daným klíčovým slovem.
        /// Do out parametru searchNext uloží true, pokud sice hledané slovo nebylo nalezeno, 
        /// ale nadřízený algoritmus může hledat dané slovo v dalších strukturách, protože žádná ze zdejších položek nebyla povinná (mohou se přeskočit).
        /// </summary>
        /// <param name="word"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="foundList"></param>
        public override SyntacticSettingItem SearchKeyword(string word, out bool searchNext)
        {
            searchNext = true;
            if (this.IsArbitrary)
            {   // Fráze bez sekvence = je libovolná, tj. může zde být cokoliv:
                // budeme hledat dál:
                return null;
            }

            for (int i = 0; i < this.VariationCount; i++)
            {
                SyntacticSettingItem item = this._Variations[i].SearchKeyword(word, out searchNext);
                if (item != null) return item;           // Pokud jsme nalezli, je to OK.
                // Daná varianta (this._Variations[i]) nevyhovuje => může vyhovovat jiná, jdeme dál:
            }

            // Žádná z našich variant nevyhovuje => pak můžeme hledat dál, jen když celá tato položka není povinná:
            searchNext = !this.IsMandatory;
            return null;
        }
        #endregion
    }
    /// <summary>
    /// Obecný předek pro setting syntaxe jazyka
    /// </summary>
    public class SyntacticSettingItem
    {
        #region Konstruktor, public property
        protected SyntacticSettingItem()
        {
            this.ParentItem = null;
            this.IsCaseSensitive = false;
            this.Keyword = null;
            this.Init();
        }
        protected SyntacticSettingItem(SyntacticSettingLibrary library, SyntacticSettingItem parentItem)
        {
            this.Library = library;
            this.ParentItem = parentItem;
            this.IsCaseSensitive = false;
            this.Keyword = null;
            this.Init();
        }
        protected SyntacticSettingItem(SyntacticSettingItem parentItem, string keyword, bool isCaseSensitive)
        {
            this.ParentItem = parentItem;
            this.Keyword = (keyword == null ? null : keyword.Trim());
            this.IsCaseSensitive = isCaseSensitive;
            this.Init();
        }
        /// <summary>
        /// Zde potomek inicializuje svoje základní prvky.
        /// </summary>
        protected virtual void Init() { }
        /// <summary>
        /// Reference na kompletní knihovnu, která obsahuje všechny sekvence
        /// </summary>
        public SyntacticSettingLibrary Library { get; protected set; }
        /// <summary>
        /// Parent, v němž se nacházíme.
        /// Může být null, pak jsme top entitou dle definice.
        /// </summary>
        public SyntacticSettingItem ParentItem { get; protected set; }
        /// <summary>
        /// Typ této položky přepsaný na konkrétním potomkovi, bázová třída obsahuje typ = Word.
        /// </summary>
        public virtual SyntacticSettingItemType ItemType { get { return SyntacticSettingItemType.Word; } }
        /// <summary>
        /// Klíčové slovo jazyka. Může být null (pak jde o sekvenci), nebo je uvedeno (pak jde o konkrétní slovo).
        /// Pokud není null, pak jsou z něj odstraněny blank znaky na začátku i na konci.
        /// </summary>
        public string Keyword { get; protected set; }
        /// <summary>
        /// Kewords je case-sensitive?
        /// </summary>
        public bool IsCaseSensitive { get; protected set; }
        /// <summary>
        /// true pokud tato položka je povinná = musí se vyskytovat / false = může a nemusí být přítomna
        /// </summary>
        public bool IsMandatory { get; protected set; }
        /// <summary>
        /// Zkrácený text, obsahuje v zásadě jen název tohoto prvku, nikoli text prvků podřízených.
        /// </summary>
        public virtual string ShortText
        {
            get
            {
                bool isMandatory = this.IsMandatory;
                string text = "";
                if (!isMandatory)
                    text += "[ ";
                text += this.Keyword;
                if (!isMandatory)
                    text += " ]";
                return text;
            }
        }
        /// <summary>
        /// Plný text, obsahuje název tohoto prvku a pak zkrácené texty prvků podřízených. Tato property se používá k vizualizaci.
        /// </summary>
        public virtual string LongText { get { return this.ShortText; } }
        /// <summary>
        /// Vizualizace. Vrací LongText.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.LongText;
        }
        #endregion
        #region SearchWord
        /// <summary>
        /// Metoda zjistí, zda dané slovo je zdejší Keyword.
        /// Pokud slovo odpovídá, vrací this = nalezený objekt, a out searchNext nastaví na false.
        /// Pokud slovo neodpovídá zdejšímu Keyword, pak nastaví out searchNext podle toho, zda this je nepovinné: 
        /// nepovinné = nastaví true (hledat dál je možno) / povinné = nastaví false (dál nehledat). Vrací null.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="searchNext"></param>
        /// <returns></returns>
        public virtual SyntacticSettingItem SearchKeyword(string word, out bool searchNext)
        {
            searchNext = false;
            if (this.IsKeyword(word))
                return this;
            searchNext = !this.IsMandatory;
            return null;
        }
        /// <summary>
        /// Vyhledá klíčové slovo odpovídající danému slovu, ale následující za this klíčovým slovem, nikoli od první položky.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="searchNext"></param>
        /// <returns></returns>
        public virtual SyntacticSettingItem SearchNextKeyword(string word, out bool searchNext)
        {
            searchNext = false;
            if (this.ParentItem == null)
                return null;
            return this.ParentItem.SearchNextKeyword(this, word, out searchNext);
        }
        /// <summary>
        /// Vyhledá klíčové slovo odpovídající danému slovu, ale následující v našem seznamu za daným klíčovým slovem (originItem), nikoli od první položky.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="searchNext"></param>
        /// <returns></returns>
        public virtual SyntacticSettingItem SearchNextKeyword(SyntacticSettingItem originItem, string word, out bool searchNext)
        {
            searchNext = false;
            return null;
        }
        /// <summary>
        /// Vrátí true pokud dané slovo odpovídá zdejšímu Keywordu.
        /// Vrací false, pokud this.Keyword je prázdné (nebo pokud je prázdné slovo word).
        /// Reaguje na this.IsCaseSensitive
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        protected bool IsKeyword(string word)
        {
            if (String.IsNullOrEmpty(this.Keyword) || String.IsNullOrEmpty(word)) return false;
            return String.Equals(this.Keyword, word.Trim(), this.KeywordComparison);
        }
        /// <summary>
        /// Obsahuje StringComparison odpovídající this.IsCaseSensitive
        /// </summary>
        protected StringComparison KeywordComparison { get { return (this.IsCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase); } }
        #endregion
    }
    /// <summary>
    /// Druh prvku, usnadňuje práci - není nutno pracovat s GetType().
    /// </summary>
    public enum SyntacticSettingItemType
    {
        None = 0,
        Word,
        Phrase,
        Sequence
    }
    #endregion
}
