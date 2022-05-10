// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDevExpress.AsolDX.News
{
    #region FilterOperators
    /// <summary>
    /// Třída poskytující nástroje pro práci s výrazem v jazyce "DevExpress Criteria Language Syntax".
    /// <para/>
    /// https://docs.devexpress.com/CoreLibraries/4928/devexpress-data-library/criteria-language-syntax#
    /// https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering
    /// https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering.CriteriaOperator
    /// </summary>
    internal class DxClsTools
    {
        #region ConvertToMsSqlParts() : Převod DxCls výrazu do MS SQL částic
        /// <summary>
        /// Metoda analyzuje dodaný výraz v jazyce <b>DevExpress Criteria Language Syntax</b> (parametr <paramref name="clsExpression"/>),
        /// vyhledá v něm jména sloupců, ty odešle do dodané filtrační metody <paramref name="columnFilter"/>,
        /// a pro které sloupce metoda vrátí false, ty podmínky z výrazu odstraní.
        /// Výsledný výraz v jazyce <b>DevExpress Criteria Language Syntax</b> vrátí, aby mohl být reaplikován do šablony, anebo i použit pro tvorbu řádkového filtru.
        /// </summary>
        /// <param name="clsExpression">Text podmínky v jazyce "DevExpress : Criteria Language Syntax"</param>
        /// <param name="columnFilter">Filtrační metoda: dostává každý sloupec nalezený ve výrazu, vrací true = sloupec je v pořádku a může se na něj filtrovat / false = sloupec neexistuje nebo jinak nelze filtrovat</param>
        /// <returns></returns>
        internal static string GetValidDxClsExpression(string clsExpression, Func<string, bool> columnFilter)
        {
            WorkContext context = new WorkContext(WorkType.ValidateColumns, clsExpression, columnFilter, null);
            try
            {
                RunAction(context);
            }
            catch (Exception exc)
            {
                throw new ArgumentException($"GetValidDxClsExpression() failed: invalid CLS expression: «{clsExpression}».\r\n{exc.Message}");
            }
            return context.ResultFilter?.ToString() ?? "";
        }
        internal static string RenameColumnsInDxClsExpression(string clsExpression, Func<string, string> columnRename)
        {
            WorkContext context = new WorkContext(WorkType.RenameColumns, clsExpression, null, columnRename);
            try
            {
                RunAction(context);
            }
            catch (Exception exc)
            {
                throw new ArgumentException($"GetValidDxClsExpression() failed: invalid CLS expression: «{clsExpression}».\r\n{exc.Message}");
            }
            return context.ResultFilter?.ToString() ?? "";
        }
        /// <summary>
        /// Tato metoda analyzuje dopdaný DxCls výraz a převede jej do jednotlivých částic MS SQL jazyka:
        /// <list type="bullet">
        /// <item>Najde v něm sloupce, a ty přeevede na částice kategorie <u>Column</u>;</item>
        /// <item>Najde v něm konstantní hodnoty, a ty převede na částice kategorie <u>DbParameter</u>;</item>
        /// <item>Ostatní části textu převede na částice kategorie <u>Text</u>;</item>
        /// </list>
        /// <param name="clsExpression">Text podmínky v jazyce "DevExpress : Criteria Language Syntax"</param>
        /// <returns>Částice výrazu</returns>
        internal static MsSqlPart[] ConvertToMsSqlParts(string clsExpression)
        {
            WorkContext context = new WorkContext(WorkType.ConvertToMsSql, clsExpression, null, null);
            try
            {
                RunAction(context);
            }
            catch (Exception exc)
            {
                throw new ArgumentException($"ConvertToMsSqlParts() failed: invalid CLS expression: «{clsExpression}».\r\n{exc.Message}");
            }
            return context.Parts?.ToArray();
        }
        #region Analýza DxCls filtru, vyhledání sloupců a hodnot, jejich nahrazení tokeny nebo jmény, nahrazení konstantních hodnot za property (podle požadavku, příprava na DbParametry)
        /// <summary>
        /// Provede patřičné operace s filtrem
        /// </summary>
        /// <param name="context"></param>
        private static void RunAction(WorkContext context)
        {
            // Parsujeme filtr:
            var filter = DevExpress.Data.Filtering.CriteriaOperator.Parse(context.ClsExpression, out var operandValues);

            // Pokud je na vstupu jednoduchá podmínka typu "pocet = 10", pak parsovaná instance 'filter' není Grupa, ale konkrétní operátor (zde BinaryOperator).
            // Abych ale mohl správně validovat i tu jednu podmínku, chci ji zabalit do grupy,
            // ze které bych potom mohl tu neplatnou podmínku odebrat - pokud by byla neplatná:
            if (!(filter is DevExpress.Data.Filtering.GroupOperator))
                filter = new DevExpress.Data.Filtering.GroupOperator(filter);
            context.InputFilter = filter;

            context.ResultFilter = DoConvertOperator(filter, context);

            if (context.DoConvert)
                DoConvertMsSql(context.ResultFilter, context);
        }
        /// <summary>
        /// Prověří platnost daného operátoru a jeho sub-operátorů.
        /// </summary>
        /// <param name="operand"></param>
        /// <param name="context"></param>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertOperator(DevExpress.Data.Filtering.CriteriaOperator operand, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            if (operand is null) return operand;
            else if (operand is DevExpress.Data.Filtering.GroupOperator group) return DoConvertGroup(group, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.UnaryOperator opUnary) return DoConvertUnary(opUnary, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.BinaryOperator opBinary) return DoConvertBinary(opBinary, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.BetweenOperator opBetween) return DoConvertBetween(opBetween, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.InOperator opIn) return DoConvertIn(opIn, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.OperandProperty opProperty) return DoConvertProperty(opProperty, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.OperandParameter opParameter) return DoConvertParameter(opParameter, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.ConstantValue opConstant) return DoConvertConstant(opConstant, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.OperandValue opValue) return DoConvertValue(opValue, context, owner, index);
            else if (operand is DevExpress.Data.Filtering.FunctionOperator opFunction) return DoConvertFunction(opFunction, context, owner, index);
            return operand;
        }
        /// <summary>
        /// Provede konverzi prvků dodané grupy
        /// </summary>
        /// <param name="group"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertGroup(DevExpress.Data.Filtering.GroupOperator group, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            for (int i = 0; i < group.Operands.Count; i++)
            {
                group.Operands[i] = DoConvertOperator(group.Operands[i], context, group, i);
                if (IsNull(group.Operands[i]))
                {   // Z obecné grupy (závorka) mohu odstranit kterýkoli prvek grupy, a zbytek je formálně použitelný:
                    context.IsChanged = true;
                    group.Operands.RemoveAt(i);
                    i--;
                }
            }
            // Pokud grupa (závorka) neobsahuje žádný prvek (odstranili jsme všechny prvky), pak grupa jako prvek je neplatná:
            bool isValid = (group.Operands.Count > 0);
            return (isValid ? group : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu UnaryOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opUnary"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertUnary(DevExpress.Data.Filtering.UnaryOperator opUnary, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            opUnary.Operand = DoConvertOperator(opUnary.Operand, context, opUnary);
            bool isValid = !IsNull(opUnary.Operand);
            return (isValid ? opUnary : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu BinaryOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opBinary"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertBinary(DevExpress.Data.Filtering.BinaryOperator opBinary, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            opBinary.LeftOperand = DoConvertOperator(opBinary.LeftOperand, context, opBinary);
            opBinary.RightOperand = DoConvertOperator(opBinary.RightOperand, context, opBinary);

            bool isValid = !IsNull(opBinary.LeftOperand, opBinary.RightOperand);
            return (isValid ? opBinary : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu BetweenOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opBetween"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertBetween(DevExpress.Data.Filtering.BetweenOperator opBetween, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            opBetween.TestExpression = DoConvertOperator(opBetween.TestExpression, context, opBetween);
            opBetween.BeginExpression = DoConvertOperator(opBetween.BeginExpression, context, opBetween);
            opBetween.EndExpression = DoConvertOperator(opBetween.EndExpression, context, opBetween);

            bool isValid = !IsNull(opBetween.TestExpression, opBetween.BeginExpression, opBetween.EndExpression);
            return (isValid ? opBetween : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu InOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opIn"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertIn(DevExpress.Data.Filtering.InOperator opIn, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            opIn.LeftOperand = DoConvertOperator(opIn.LeftOperand, context, opIn);
            if (IsNull(opIn.LeftOperand)) return null;

            for (int i = 0; i < opIn.Operands.Count; i++)
            {
                opIn.Operands[i] = DoConvertOperator(opIn.Operands[i], context, opIn, i);
                if (IsNull(opIn.Operands[i]))
                {   // Ze seznamu výrazů v podmínce "[Column] IN (Operands...) mohu odstranit kterýkoli prvek grupy, a zbytek je formálně použitelný:
                    context.IsChanged = true;
                    opIn.Operands.RemoveAt(i);
                    i--;
                }
            }
            // Pokud seznam výrazů (závorka) neobsahuje žádný prvek (odstranili jsme všechny prvky), pak výraz IN jako prvek je neplatný:
            bool isValid = (opIn.Operands.Count > 0);
            return (isValid ? opIn : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu OperandProperty a jeho sub-operátorů.
        /// </summary>
        /// <param name="opProperty"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertProperty(DevExpress.Data.Filtering.OperandProperty opProperty, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            // OperandProperty je jedním z našich cílů. Obsahuje totiž název sloupce v tabulce, na který se filtruje.
            // Obsahuje jméno PropertyName, které v řádkovém filtru odpovídá ColumnId = Alias.

            bool isValid = true;
            if (context.DoValidate)
            {   // Pokud máme sloupec jen validovat:
                isValid = context.IsValidColumnName(opProperty.PropertyName);
            }
            else if (context.DoRename)
            {   // Přejmenovat sloupec:
                opProperty.PropertyName = context.GetColumnName(opProperty.PropertyName, out isValid);
                context.IsChanged = true;
            }
            else if (context.DoConvert)
            {   // Máme připravit sloupec pro konverzi do MS SQL:
                // Naším úkolem je toto jméno sloupce detekovat a vyčlenit jej později do samostatné částice ExpressionPart typu Column, podtyp Alias.
                // V první (přípravné) fázi si stávající alias uložíme do paměti a nahradíme jej unikátním stringem typickým pro Sloupec:
                opProperty.PropertyName = context.GetColumnToken(opProperty.PropertyName, out isValid);
                context.IsChanged = true;
            }
            return (isValid ? opProperty : null);
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu OperandParameter a jeho sub-operátorů.
        /// </summary>
        /// <param name="opProperty"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertParameter(DevExpress.Data.Filtering.OperandParameter opParameter, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            return opParameter;
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu ConstantValue a jeho sub-operátorů.
        /// </summary>
        /// <param name="opValue"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertConstant(DevExpress.Data.Filtering.ConstantValue opConstant, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            if (context.DoConvert)
            {   // Pouze když se má dělat konverze:
                // Konstantní hodnotu ConstantValue vždy uložím do contextu a nahradím ji objektem typu OperandProperty s textem unikátního tokenu.
                // Navazující logika pak tuto hodnotu skryje před MS SQL formátováním, skrz formátování hodnota projde jako by to byl sloupec dotazu,
                // a na závěr je token nahrazen částicí typu DB Parameter s uschovanou hodnotou:

                // Pozor, v určité situaci konstantu neměním ale nechám beze změny:
                if (IsFunctionConstant(owner, opConstant, index)) return opConstant;

                string token = context.GetConstantValueToken(opConstant.Value);
                context.IsChanged = true;
                return new DevExpress.Data.Filtering.OperandProperty(token);
            }
            return opConstant;
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu OperandValue a jeho sub-operátorů.
        /// </summary>
        /// <param name="opValue"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertValue(DevExpress.Data.Filtering.OperandValue opValue, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            return opValue;
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu FunctionOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opFunction"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertFunction(DevExpress.Data.Filtering.FunctionOperator opFunction, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            bool isValid = true;
            for (int i = 0; i < opFunction.Operands.Count; i++)
            {
                opFunction.Operands[i] = DoConvertOperator(opFunction.Operands[i], context, opFunction, i);
                if (IsNull(opFunction.Operands[i]))
                {   // Pokud kterýkoli z parametrů funkce je nevalidní, pak je nevalidní celá funkce. Tady nemohu (tak jako v grupě) odstranit jen nevalidní parametry:
                    isValid = false;
                    break;
                }
            }
            return (isValid ? opFunction : null);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná konstanta v daném vlastníku je nezměnitelná.
        /// To je tehdy, když vlastníkem je Funkce typu Custom, a konstanta je první v poli jejích operandů. V tom případě jde totiž o jméno Custom funkce, a ne o hodnotu.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="opConstant"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool IsFunctionConstant(DevExpress.Data.Filtering.CriteriaOperator owner, DevExpress.Data.Filtering.ConstantValue opConstant, int? index)
        {
            if (owner is DevExpress.Data.Filtering.FunctionOperator opFunction && index.HasValue)
            {   // Sem přijdu tehdy, když se vyhodnocuje konstanta 'opConstant', která pochází z operandů funkce 'opFunction' (v metodě DoConvertFunction()):
                // pokud funkce je typu Custom, pak v prvním parametru funkce Operands[0] je klíčové jméno funkce, a to se nesmí změnit z konstanty na Property:
                if (opFunction.OperatorType == DevExpress.Data.Filtering.FunctionOperatorType.Custom && index.Value == 0) return true;
            }
            return false;
        }
        /// <summary>
        /// Vrátí true, pokud dodaná kolekce je null, anebo pokud kterýkoli (byť i jeden) z dodaných operandů je null. Pak je celá kolekce nevalidní.
        /// </summary>
        /// <param name="operands"></param>
        /// <returns></returns>
        private static bool IsNull(IEnumerable<DevExpress.Data.Filtering.CriteriaOperator> operands)
        {
            return operands is null || operands.Any(o => ((object)o is null));
        }
        /// <summary>
        /// Vrátí true, pokud kterýkoli (byť i jeden) z dodaných operandů je null. Pak je celá kolekce nevalidní.
        /// </summary>
        /// <param name="operands"></param>
        /// <returns></returns>
        private static bool IsNull(params DevExpress.Data.Filtering.CriteriaOperator[] operands)
        {
            return operands.Any(o => ((object)o is null));
        }
        #endregion
        #region Konverze upraveného filtru do MS SQL a jeho rozdělení do částic (s pomocí vygenerovaných tokenů)


        private static void DoConvertMsSql(DevExpress.Data.Filtering.CriteriaOperator filter, WorkContext context)
        {
            // Vytvořím string obsahující SQL podmínku, z dodaného filtru. Ten má na místech sloupců a konstantních hodnot tokeny přítomné v contextu.
            string sql = DevExpress.Data.Filtering.CriteriaToWhereClauseHelper.GetMsSqlWhere(filter, c => c.PropertyName);

            // Pro vstupní text (clsExpression):
            //   "[pocetkusu] >= 48.2 && [nazev] like 'Nad%'"
            // Dostaneme výsledný text například:
            //   "((CL45Y732GNNSA >= VU4H2ARZ8W64V) And (COGQNO6ZYXN2B like VLPFW0OAWRDKX))"
            // Objekt context obsahuje tokeny:
            //   CL45Y732GNNSA = Column: pocetkusu
            //   VU4H2ARZ8W64V = Value: 48,2
            //   COGQNO6ZYXN2B = Column: nazev
            //   VLPFW0OAWRDKX = Value: Nad%

            // S pomocí kontextu budu v SQL textu vyhledávat tokeny a získávat jejich pozice, 
            // a s pomocí kontextu budu ukládat nalezené částice textu:
            int index = 0;
            while (true)
            {
                if (!context.TrySearchToken(sql, index, out int tokenIndex, out TokenInfo token)) break;
                index = context.AddParts(sql, index, tokenIndex, token);
                if (index < 0) break;
            }
        }

        #endregion
        #region class ConvertContext
        /// <summary>
        /// Kontext validace výrazu
        /// </summary>
        private class WorkContext
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="columnFormatter">Převodní funkce pro jména sloupců z výrazu (vyhledává aliasy atd).</param>
            public WorkContext(WorkType workType, string clsExpression, Func<string, bool> columnFilter, Func<string, string> columnRename)
            {
                WorkType = workType;
                ClsExpression = clsExpression;
                ColumnFilter = columnFilter;
                ColumnRename = columnRename;

                Parts = new List<MsSqlPart>();
                Columns = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                Tokens = new Dictionary<string, TokenInfo>();
                IsChanged = false;
                HasColumnFilter = (columnFilter != null);
                HasColumnRename = (columnRename != null);
                Rand = new Random();
                Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            }
            /// <summary>
            /// Typ aktivity
            /// </summary>
            public WorkType WorkType { get; private set; }
            /// <summary>
            /// Vstupní DxCls string
            /// </summary>
            public string ClsExpression { get; private set; }
            /// <summary>
            /// Filtr sloupců
            /// </summary>
            public Func<string, bool> ColumnFilter { get; private set; }
            /// <summary>
            /// Přejmenovávač sloupců
            /// </summary>
            public Func<string, string> ColumnRename { get; private set; }
            /// <summary>
            /// Prostor pro uložení vstupního parsovaného filtru
            /// </summary>
            public DevExpress.Data.Filtering.CriteriaOperator InputFilter { get; set; }
            /// <summary>
            /// Prostor pro uložení výsledného parsovaného filtru
            /// </summary>
            public DevExpress.Data.Filtering.CriteriaOperator ResultFilter { get; set; }
            /// <summary>
            /// Provádět pouze validaci sloupců
            /// </summary>
            public bool DoValidate { get { return this.WorkType == WorkType.ValidateColumns; } }
            /// <summary>
            /// Provádět pouze přejmenování sloupců
            /// </summary>
            public bool DoRename { get { return this.WorkType == WorkType.RenameColumns; } }
            /// <summary>
            /// Provádět konverzi do MS SQL
            /// </summary>
            public bool DoConvert { get { return this.WorkType == WorkType.ConvertToMsSql; } }
            /// <summary>
            /// Výstupní částice
            /// </summary>
            public List<MsSqlPart> Parts { get; private set; }
            /// <summary>
            /// Dictionary sloupců (Key) a jejich zástupných tokenů (Values).
            /// Jeden název sloupce má vždy shodný token.
            /// </summary>
            public Dictionary<string, string> Columns { get; private set; }
            /// <summary>
            /// Dictionary tokenů, tak aby byly přes celý výraz unikátní
            /// </summary>
            public Dictionary<string, TokenInfo> Tokens { get; private set; }
            /// <summary>
            /// Obsahujeme změny?
            /// </summary>
            public bool IsChanged { get; set; }
            /// <summary>
            /// true pokud existuje filtr sloupců
            /// </summary>
            private bool HasColumnFilter;
            /// <summary>
            /// true pokud existuje přejmenovávač sloupců
            /// </summary>
            private bool HasColumnRename;
            /// <summary>
            /// Random pro generátor tokenů
            /// </summary>
            private Random Rand;
            /// <summary>
            /// Pole znaků povolených v tokenech, na prvních 26 pozicích jsou znaky povolené na první pozici
            /// </summary>
            private char[] Chars;
            /// <summary>
            /// Vrací true, pokud daný sloupec je platný. Používá se jen při validacích.
            /// </summary>
            /// <param name="columnName"></param>
            /// <returns></returns>
            public bool IsValidColumnName(string columnName)
            {
                return (!this.HasColumnFilter || ColumnFilter(columnName));
            }
            /// <summary>
            /// Metoda vrátí nové jméno pro dané výchozí jméno sloupce.
            /// Může vrátit i prázdný string, pak bude out parametr <paramref name="isValid"/> = false.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="isValid"></param>
            /// <returns></returns>
            public string GetColumnName(string columnName, out bool isValid)
            {
                string newName = columnName;
                isValid = true;
                if (this.HasColumnRename)
                {
                    newName = this.ColumnRename(columnName);
                    isValid = !String.IsNullOrEmpty(newName);
                }
                return newName;
            }
            /// <summary>
            /// Pro daný název sloupce najde token buď dříve již přidělený (a bude použit opakovaně, což je pro Column v pořádku) anebo vygeneruje nový unikátní token (a zapamatuje si jej pro daný sloupec).
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="isValid"></param>
            /// <returns></returns>
            public string GetColumnToken(string columnName, out bool isValid)
            {
                if (this.HasColumnFilter && !ColumnFilter(columnName))
                {
                    isValid = false;
                    return null;
                }

                string key = columnName.Trim();
                if (!Columns.TryGetValue(key, out var token))
                {
                    token = CreateNewToken("C", ExpressionCategory.Column, columnName, null);
                    Columns.Add(key, token);
                }
                isValid = true;
                return token;
            }
            /// <summary>
            /// Získá nový unikátní token a vrátí jej, uloží si token a dodanou hodnotu do slovníku <see cref="Tokens"/>.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public string GetConstantValueToken(object value)
            {
                string token = CreateNewToken("V", ExpressionCategory.Value, null, value);
                return token;
            }
            /// <summary>
            /// Vytvoří a vrátí new token s daným prefixem.
            /// Je zajištěno, že takový token ještě neexistuje v <see cref="Tokens"/> a ani se nevyskytuje v <see cref="ClsExpression"/>.
            /// </summary>
            /// <param name="prefix"></param>
            /// <returns></returns>
            private string CreateNewToken(string prefix, ExpressionCategory target, string columnName, object value)
            {
                var expression = ClsExpression;
                var rand = Rand;
                var chars = Chars;
                var len = chars.Length;
                int count = 12;
                string token = null;
                StringBuilder sb = new StringBuilder();
                for (int t = 0; t < 1000; t++)
                {   // t slouží jen jako timeout
                    sb.Clear();
                    sb.Append(prefix);
                    for (int i = 0; i < count; i++)
                        sb.Append(chars[rand.Next(i == 0 ? 26 : len)]);
                    token = sb.ToString();
                    if (expression.IndexOf(token) < 0 && !Tokens.ContainsKey(token)) break;
                }
                if (!Tokens.ContainsKey(token))
                    Tokens.Add(token, new TokenInfo(token, target, columnName, value));

                return token;
            }
            /// <summary>
            /// Metoda najde v daném SQL příkazu další token počínaje indexem <paramref name="index"/>, vrátí jeho pozici a hodnoty.
            /// Pokud vstupní index je za koncem textu, vrací false.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="index"></param>
            /// <param name="tokenIndex"></param>
            /// <param name="token"></param>
            /// <returns></returns>
            public bool TrySearchToken(string text, int index, out int tokenIndex, out TokenInfo token)
            {
                tokenIndex = -1;
                token = null;

                // Pokud nemáme nic k prohledání, skončíme:
                int textLength = text?.Length ?? 0;
                if (textLength == 0 || index >= textLength) return false;

                // Máme ještě nějaký text nezpracovaný:
                foreach (var test in this.Tokens.Values)
                {
                    int i = text.IndexOf(test.Token, index);
                    if (i > 0 && i >= index && (tokenIndex < 0 || i < tokenIndex))
                    {
                        tokenIndex = i;
                        token = test;
                    }
                }
                // Výstup true značí, že máme co zpracovat, i kdyby tam už nebyl token (je třeba zpracovat i prostý text bez tokenů až do konce):
                return true;
            }
            /// <summary>
            /// Metoda přidá částice: částici za prostý text před tokenem (je-li), částici reprezentující token (a jeho obsah) (je-li nalezen), a vrátí index znaku za tokenem.
            /// </summary>
            /// <param name="text"></param>
            /// <param name="index"></param>
            /// <param name="tokenIndex"></param>
            /// <param name="token"></param>
            /// <returns></returns>
            public int AddParts(string text, int index, int tokenIndex, TokenInfo token)
            {
                // Pokud nemáme nic k prohledání, skončíme:
                int textLength = text?.Length ?? 0;
                if (textLength == 0 || index >= textLength) return -1;

                int nextIndex = -1;

                // 1. Zpracujeme prostý text od pozice index (kde se začal hledat token) až do pozice, kde se token našel, nebo do konce textu:
                int textEndIndex = (tokenIndex > 0 ? tokenIndex : textLength);
                if (textEndIndex > index)
                {
                    AddPart(ExpressionCategory.Text, text.Substring(index, textEndIndex - index), null, null);
                    nextIndex = textEndIndex;
                }

                // 2. Zpracujeme token, pokud byl nalezen:
                if (tokenIndex >= 0 && token != null)
                {
                    switch (token.Target)
                    {
                        case ExpressionCategory.Column:
                            AddPart(ExpressionCategory.Column, token.ColumnName, token.ColumnName, null);
                            break;
                        case ExpressionCategory.Value:
                            AddPart(ExpressionCategory.Value, token.Value.ToString(), null, token.Value);
                            break;
                    }
                    nextIndex = tokenIndex + token.Token.Length;
                    if (nextIndex >= textLength) nextIndex = -1;
                }

                return nextIndex;
            }
            private void AddPart(ExpressionCategory category, string text, string columnName, object value)
            {
                this.Parts.Add(new MsSqlPart(category, text, columnName, value));
            }
        }
        /// <summary>
        /// Typy práce
        /// </summary>
        private enum WorkType { None, ValidateColumns, RenameColumns, ConvertToMsSql }
        /// <summary>
        /// Info o jednom tokenu (text, účel, hodnoty)
        /// </summary>
        private class TokenInfo
        {
            /// <summary>
            /// Konstuktor
            /// </summary>
            /// <param name="token"></param>
            /// <param name="target"></param>
            /// <param name="columnName"></param>
            /// <param name="value"></param>
            public TokenInfo(string token, ExpressionCategory target, string columnName, object value)
            {
                this.Token = token;
                this.Target = target;
                this.ColumnName = columnName;
                this.Value = value;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Token + " = " + (this.Target == ExpressionCategory.Column ? "Column: " + ColumnName : (this.Target == ExpressionCategory.Value ? "Value: " + Value.ToString() : "???"));
            }
            public string Token { get; private set; }
            public ExpressionCategory Target { get; private set; }
            public string ColumnName { get; private set; }
            public object Value { get; private set; }
        }
        #endregion
        #endregion
    }
    /// <summary>
    /// Částice výrazu
    /// </summary>
    public class MsSqlPart
    {
        public MsSqlPart(ExpressionCategory category, string text, string columnName, object value)
        {
            this.Category = category;
            this.OriginalText = text;
            this.ColumnName = columnName;
            this.Value = value;
        }
        public override string ToString() { return this.ResultText; }
        public ExpressionCategory Category { get; set; }
        public string OriginalText { get; set; }
        public string ResultText
        {
            get
            {
                switch (this.Category)
                {
                    case ExpressionCategory.Text: return this.OriginalText;
                    case ExpressionCategory.Column: return this.ColumnName;
                    case ExpressionCategory.Value: return ToSql(this.Value);
                }
                return "{" + this.Category.ToString() + "}";
            }
        }
        public string ColumnName { get; set; }
        public object Value { get; set; }
        private static string ToSql(object value)
        {
            if (value is null) return "NULL";
            string type = value.GetType().Name;
            switch (type)
            {
                case "Boolean": return (((Boolean)value) ? "true" : "false");

                case "Char": return "N'" + ((Char)value).ToString() + "'";
                case "String": return "N'" + ((String)value) + "'";

                case "Int16": return ((Int16)value).ToString();
                case "Int32": return ((Int32)value).ToString();
                case "Int64": return ((Int64)value).ToString();
                case "UInt16": return ((UInt16)value).ToString();
                case "UInt32": return ((UInt32)value).ToString();
                case "UInt64": return ((UInt64)value).ToString();
                case "Byte": return ((Byte)value).ToString();
                case "SByte": return ((SByte)value).ToString();

                case "Single": return ((Single)value).ToString().Replace(" ", "").Replace(",", ".");
                case "Double": return ((Double)value).ToString().Replace(" ", "").Replace(",", ".");
                case "Decimal": return ((Decimal)value).ToString().Replace(" ", "").Replace(",", ".");

                case "DateTime": return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }

            return value.ToString();
        }
        /// <summary>
        /// Sloučí dodané prvky do jednoho HTML textu
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static string MergeToHtml(IEnumerable<MsSqlPart> parts)
        {
            string htmlBegin = "<html><body style=\"background-color:#EEEEFF;\"><p align=\"left\"><code>";
            string htmlEnd = "</code></p></body></html>";
            string textBegin = "<span style=\"color:#101020\">";
            string textEnd = "</span>";
            string columnBegin = "<span style=\"color:#1010C0\"><u>";
            string columnEnd = "</u></span>";
            string valueBegin = "<span style=\"color:#C01010\"><b>";
            string valueEnd = "</b></span>";

            StringBuilder sb = new StringBuilder();
            sb.Append(htmlBegin);
            foreach (var part in parts)
            {
                var type = part.Category;
                var text = part.ResultText;
                switch (type)
                {
                    case Djs.Test.DxCls.ExpressionCategory.Text:
                        sb.Append(textBegin + text + textEnd); ;
                        break;
                    case Djs.Test.DxCls.ExpressionCategory.Column:
                        sb.Append(columnBegin + text + columnEnd); ;
                        break;
                    case Djs.Test.DxCls.ExpressionCategory.Value:
                        sb.Append(valueBegin + text + valueEnd); ;
                        break;
                }
            }
            sb.Append(htmlEnd);
            return sb.ToString();
        }
    }
    public enum ExpressionCategory { None, Text, Column, Value }

    #endregion
}
