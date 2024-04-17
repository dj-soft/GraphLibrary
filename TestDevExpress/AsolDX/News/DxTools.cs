// Supervisor: David Janáček, od 01.01.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.
using DevExpress.XtraRichEdit.Import.Doc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestDevExpress.AsolDX.News
{
    #region class DxClsTools : Třída poskytující nástroje pro práci s výrazem v jazyce "DevExpress Criteria Language Syntax"
    /// <summary>
    /// Třída poskytující nástroje pro práci s výrazem v jazyce "DevExpress Criteria Language Syntax".
    /// <para/>
    /// https://docs.devexpress.com/CoreLibraries/4928/devexpress-data-library/criteria-language-syntax#
    /// https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering
    /// https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering.CriteriaOperator
    /// </summary>
    internal class DxClsTools
    {
        #region Public rozhraní
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
        /// <summary>
        /// Metoda najde sloupce v daném DxCls výrazu a nahradí jejich text jiným textem, který vygeneruje dodaný <paramref name="columnRename"/>.
        /// Je vhodné pro převod řádkového filtru mezi šablonami, kdy v prvním průchodu nahradíme aliasy sloupců plným názvem sloupce včetně FullEntityKey,
        /// a v druhém průchodu pak tyto FullColumnName vyhledáme v nové šabloně a do výrazu vložíme aliasy sloupců v této nové šabloně.
        /// </summary>
        /// <param name="clsExpression">Text podmínky v jazyce "DevExpress : Criteria Language Syntax"</param>
        /// <param name="columnRename">Metoda, která dostává názvy sloupců nalezené ve výrazu, a vrací nové názvy, které se do vytvářeného výrazu vkládají. Pokud vrátí null, část výrazu bude odebrána.</param>
        /// <returns></returns>
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
        /// </summary>
        /// <param name="clsExpression">Text podmínky v jazyce "DevExpress : Criteria Language Syntax"</param>
        /// <returns>Částice výrazu</returns>
        internal static ExpressionPart[] ConvertToMsSqlParts(string clsExpression)
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
        #endregion
        #region Analýza DxCls filtru, vyhledání sloupců a hodnot, jejich nahrazení tokeny nebo jmény, nahrazení konstantních hodnot za property (podle požadavku, příprava na DbParametry)
        /// <summary>
        /// Provede patřičné operace s filtrem
        /// </summary>
        /// <param name="context">Pracovní kontext</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="opParameter"></param>
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertParameter(DevExpress.Data.Filtering.OperandParameter opParameter, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            return opParameter;
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu ConstantValue a jeho sub-operátorů.
        /// </summary>
        /// <param name="opConstant"></param>
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
        /// <returns></returns>
        private static DevExpress.Data.Filtering.CriteriaOperator DoConvertValue(DevExpress.Data.Filtering.OperandValue opValue, WorkContext context, DevExpress.Data.Filtering.CriteriaOperator owner = null, int? index = null)
        {
            return opValue;
        }
        /// <summary>
        /// Provede konverzi prvků daného operátoru typu FunctionOperator a jeho sub-operátorů.
        /// </summary>
        /// <param name="opFunction"></param>
        /// <param name="context">Pracovní kontext</param>
        /// <param name="owner">Prvek, v němž je daný operátor uložen</param>
        /// <param name="index">Index prvku v poli Ownera, pokud je prvek uložen v poli (Grupy, operátor In, Funkce)</param>
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
            /// <param name="workType"></param>
            /// <param name="clsExpression"></param>
            /// <param name="columnFilter"></param>
            /// <param name="columnRename"></param>
            public WorkContext(WorkType workType, string clsExpression, Func<string, bool> columnFilter, Func<string, string> columnRename)
            {
                WorkType = workType;
                ClsExpression = clsExpression;
                ColumnFilter = columnFilter;
                ColumnRename = columnRename;

                Parts = new List<ExpressionPart>();
                Columns = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                Tokens = new Dictionary<string, TokenInfo>();
                IsChanged = false;
                HasColumnFilter = (columnFilter != null);
                HasColumnRename = (columnRename != null);
                Rand = new System.Random();
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
            public List<ExpressionPart> Parts { get; private set; }
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
            private System.Random Rand;
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
                    token = CreateNewToken("C", TextPartCategory.ColumnAlias, columnName, null);
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
                string token = CreateNewToken("V", TextPartCategory.ValueConstant, null, value);
                return token;
            }
            /// <summary>
            /// Vytvoří a vrátí new token s daným prefixem.
            /// Je zajištěno, že takový token ještě neexistuje v <see cref="Tokens"/> a ani se nevyskytuje v <see cref="ClsExpression"/>.
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="target"></param>
            /// <param name="columnName"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private string CreateNewToken(string prefix, TextPartCategory target, string columnName, object value)
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
                    AddPartText(text.Substring(index, textEndIndex - index));
                    nextIndex = textEndIndex;
                }

                // 2. Zpracujeme token, pokud byl nalezen:
                if (tokenIndex >= 0 && token != null)
                {
                    switch (token.Target)
                    {
                        case TextPartCategory.ColumnAlias:
                            AddPartColumnAlias(token.ColumnName);
                            break;
                        case TextPartCategory.ValueConstant:
                            AddPartValueConstant(token.Value.ToString(), token.Value);
                            break;
                    }
                    nextIndex = tokenIndex + token.Token.Length;
                    if (nextIndex >= textLength) nextIndex = -1;
                }

                return nextIndex;
            }
            private void AddPartText(string text)
            {
                this.Parts.Add(ExpressionPart.CreatePartText(text));
            }
            private void AddPartColumnAlias(string columnName)
            {
                this.Parts.Add(ExpressionPart.CreatePartColumn(TextPartCategory.ColumnAlias, columnName));
            }
            private void AddPartValueConstant(string name, object value)
            {
                this.Parts.Add(ExpressionPart.CreatePartValueConstant(name, value));
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
            public TokenInfo(string token, TextPartCategory target, string columnName, object value)
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
                return this.Token + " = " + (this.Target == TextPartCategory.ColumnAlias ? "Column: " + ColumnName : (this.Target == TextPartCategory.ValueConstant ? "Value: " + Value.ToString() : "???"));
            }
            public string Token { get; private set; }
            public TextPartCategory Target { get; private set; }
            public string ColumnName { get; private set; }
            public object Value { get; private set; }
        }
        #endregion
    }
    #endregion
    #region Třídy ExpressionPart a patřičné potomstvo
    #region class ExpressionPartArgument : třída obsahující proměnnou hodnotu zadávanou uživatelem, typicky položku v okně parametrů
    /// <summary>
    /// Částice výrazu, obsahující proměnnou hodnotu zadávanou uživatelem, typicky položku v okně parametrů.
    /// </summary>
    public class ExpressionPartArgument : ExpressionPartValue
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="argumentName"></param>
        /// <param name="parsedItem"></param>
        internal ExpressionPartArgument(string argumentName, ParsedItem parsedItem)
            : base(TextPartCategory.ValueArgument, argumentName, null)
        {
            ParsedItem = parsedItem;
        }
        #region ParsedItem a informace z něj vytěžené
        /// <summary>
        /// Vlastní definice argumentu. Obsahuje 
        /// </summary>
        public ParsedItem ParsedItem { get; private set; }
        #endregion
        #region Aktuální hodnota a související příznaky
        /// <summary>
        /// Aktuální hodnota. 
        /// Pokud není od uživatele vyplněna, nebo uživatel řekl 'Přeskočit', nebo aplikační kód vložil NULL, pak je čtena jako null.
        /// <para/>
        /// Pokud je typu <see cref="String"/>, pak výsledná hodnota je opatřena <see cref="ExpressionPartValue.StringValuePrefix"/> a <see cref="ExpressionPartValue.StringValuePostfix"/>.
        /// Pokud je dynamická, je vyhodnocena nyní.
        /// <para/>
        /// Hodnota by měla být čtena pokud možno co nejblíže k aplikaci výsledného SQL SELECTU, protože hodnota může obsahovat údaje platné v době čtení.
        /// Jinými slovy, hodnota může obsahovat aktuální datum a čas; a pokud vrácený údaj bude dlouhodobě skladován, bude ztrácet na aktuálnosti.
        /// </summary>
        protected override object CurrentValue
        {
            get
            {
                if (!IsDefined || IsSkipped || IsNull) return null;
                return base.CurrentValue;
            }
        }
        /// <summary>
        /// Resetuje hodnotu argumentu do výchozího stavu, včetně příznaků.
        /// Bude se tvářit jako čerstvě vytvořený argument.
        /// </summary>
        public void Reset()
        {
            Value = null;
            IsDefined = false;
            IsSkipped = false;
            IsNull = false;
        }
        /// <summary>
        /// Nastaví se na true po vyplnění hodnoty.
        /// </summary>
        public bool IsDefined { get; set; }
        /// <summary>
        /// Nastaví se na true po požadavku na přeskočení podmínky.
        /// </summary>
        public bool IsSkipped { get; set; }
        /// <summary>
        /// Nastaví se na true po požadavku na NULL hodnotu.
        /// </summary>
        public bool IsNull { get; set; }
        #endregion
    }
    #endregion
    #region class ExpressionPartVariable : třída obsahující proměnnou hodnotu: klíčové slovo
    /// <summary>
    /// Částice výrazu, obsahující proměnnou hodnotu, typicky klíčové slovo.
    /// </summary>
    public class ExpressionPartVariable : ExpressionPartValue
    {
        #region Základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="parsedItem"></param>
        internal ExpressionPartVariable(string variableName, ParsedItem parsedItem)
            : base(TextPartCategory.ValueVariable, variableName, null)
        {
            ParsedItem = parsedItem;
        }
        /// <summary>
        /// Vlastní hodnota
        /// </summary>
        public ParsedItem ParsedItem { get; private set; }
        /// <summary>
        /// Aktuální hodnota.
        /// Pokud je typu <see cref="String"/>, pak výsledná hodnota je opatřena <see cref="ExpressionPartValue.StringValuePrefix"/> a <see cref="ExpressionPartValue.StringValuePostfix"/>.
        /// Pokud je dynamická, je vyhodnocena nyní.
        /// </summary>
        protected override object CurrentValue
        {
            get
            {
                var value = GetVariableValue(ValueName, ParsedItem);
                return value;
            }
        }
        /// <summary>
        /// Úvodní HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> před hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string BeginTagHtml { get { return "<span style=\"color:#C01080\"><b>"; } }
        /// <summary>
        /// Závěrečné HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> za hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string EndTagHtml { get { return "</b></span>"; } }
        #endregion
        #region Proměnná hodnota - klíčové slovo a jeho vyhodnocení
        /// <summary>
        /// Metoda vrátí aktuálně platnou hodnotu dané proměnné
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="parsedItem"></param>
        /// <returns></returns>
        protected object GetVariableValue(string variableName, ParsedItem parsedItem)
        {
            return null;
        }
        #endregion
    }
    #endregion
    #region class ExpressionPartValue : třída obsahující hodnotu
    /// <summary>
    /// Částice výrazu, obsahující hodnotu (ve třídě <see cref="ExpressionPartValue"/> jde o hodnotu fixně danou).
    /// Pro proměnnou anebo uživatelský argument je třeba vytvořit odpovídajícího potomka této třídy.
    /// <para/>
    /// Zdejší třída <see cref="ExpressionPartValue"/> podporuje předání hodnoty do Db parametru.
    /// </summary>
    public class ExpressionPartValue : ExpressionPart
    {
        #region Základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="category"></param>
        /// <param name="valueName"></param>
        /// <param name="value"></param>
        internal ExpressionPartValue(TextPartCategory category, string valueName, object value)
            : base(category)
        {
            ValueName = valueName;
            Value = value;
            ParamName = null;
        }
        /// <summary>
        /// Textové označení hodnoty, typicky její textové vyjádření.
        /// </summary>
        public string ValueName { get; private set; }
        /// <summary>
        /// Vlastní hodnota
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Aktuální hodnota.
        /// Pokud je typu <see cref="String"/>, pak výsledná hodnota je opatřena <see cref="StringValuePrefix"/> a <see cref="StringValuePostfix"/>.
        /// Pokud je dynamická, je vyhodnocena nyní.
        /// <para/>
        /// Hodnota by měla být čtena pokud možno co nejblíže k aplikaci výsledného SQL SELECTU, protože hodnota může obsahovat údaje platné v době čtení.
        /// Jinými slovy, hodnota může obsahovat aktuální datum a čas; a pokud vrácený údaj bude dlouhodobě skladován, bude ztrácet na aktuálnosti.
        /// </summary>
        protected virtual object CurrentValue
        {
            get
            {
                var value = Value;
                if (IsDynamic)
                    value = GetDynamicValue(value);
                if (value is string text)
                    value = GetPrefixedStringValue(text);
                return value;
            }
        }
        /// <summary>
        /// Výsledný text výrazu, v notaci SQL.
        /// Výstupní text = jméno parametru <see cref="ParamName"/> (pokud je přiděleno: <see cref="HasParameter"/>),
        /// anebo přítomná hodnota <see cref="Value"/>, formátovaná do SQL notace (bude v SQL citována jako konstanta).
        /// <para/>
        /// Hodnota by měla být čtena pokud možno co nejblíže k aplikaci výsledného SQL SELECTU, protože hodnota může obsahovat údaje platné v době čtení.
        /// Jinými slovy, hodnota může obsahovat aktuální datum a čas; a pokud vrácený údaj bude dlouhodobě skladován, bude ztrácet na aktuálnosti.
        /// <br/>
        /// To platí jak pro variantu s DB parametrem, tak i pro přímou citaci do výsledného textu.
        /// </summary>
        public override string ResultText { get { return (!HasParameter ? ToSql(CurrentValue) : "@" + ParamName); } }
        /// <summary>
        /// Úvodní HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> před hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string BeginTagHtml { get { return "<span style=\"color:#C01010\"><b>"; } }
        /// <summary>
        /// Závěrečné HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> za hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string EndTagHtml { get { return "</b></span>"; } }
        #endregion
        #region String prefix a postfix "%...%" - pro vyhledávání s pomocí LIKE, a to i v potmkovi typu Variable a Argument
        /// <summary>
        /// Prefix textu: použije se pouze pokud <see cref="Value"/> je typu String, předsadí se před text.
        /// Umožní tak požít prefix i v případě, kdy hodnota je určená jako Argument = Value je zadaná bez "%", ale do SQL výrazu se předsadí "%"
        /// </summary>
        public virtual string StringValuePrefix { get { return _StringValuePrefix; } set { _StringValuePrefix = value; } }
        private string _StringValuePrefix;
        /// <summary>
        /// Postix textu: použije se pouze pokud <see cref="Value"/> je typu String, přidá se za text.
        /// Umožní tak požít prefix i v případě, kdy hodnota je určená jako Argument = Value je zadaná bez "%", ale do SQL výrazu se připojí "%"
        /// </summary>
        public virtual string StringValuePostfix { get { return _StringValuePostfix; } set { _StringValuePostfix = value; } }
        private string _StringValuePostfix;
        /// <summary>
        /// Vrací zadanou hodnotu opatřenou prefixem <see cref="StringValuePrefix"/> a postfixem <see cref="StringValuePostfix"/>
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected string GetPrefixedStringValue(string text)
        {
            if (StringValuePrefix != null || StringValuePostfix != null)
            {
                string prefix = StringValuePrefix ?? "";
                string postfix = StringValuePostfix ?? "";
                text = prefix + (text ?? "") + postfix;
            }
            return text;
        }
        #endregion
        #region DbParameter
        /// <summary>
        /// Obsahuje true, pokud tato instance umožňuje pracovat s DB parametrem.
        /// </summary>
        public bool EnableDbParameters { get { return true; } set { } }
        /// <summary>
        /// Název parametru, bez úvodního znaku @.
        /// Pokud je vložen neplatný text, obsahuje null.
        /// <para/>
        /// <u>Práce s parametry:</u><br/>
        /// 1. Hodnota <see cref="ExpressionPartValue"/> může být vytvořena bez názvu parametru; 
        ///  v takovém případě je do SQL výrazu (tj. do <see cref="ResultText"/>) vkládána napřímo, formátovaná do SQL notace<br/>
        /// 2. Hodnotu lze vytvořit se jménem parametru, nebo do <see cref="ParamName"/> jméno parametru vložit;<br/>
        /// a. Pak do výstupního SQL výrazu (tj. do <see cref="ResultText"/>) je vloženo jméno parametru s @;<br/>
        /// b. V tom případě je nutno vyzvednout instanci <see cref="DbParameter"/> a přidat ji do pole ostatních parametrů a použít v rámci SQL SELECTU
        /// </summary>
        public virtual string ParamName { get { return _ParamName; } set { _ParamName = GetParamName(value); } }
        private string _ParamName;
        /// <summary>
        /// Obsahuje true, pokud this hodnota má parametr = má je povolené (<see cref="EnableDbParameters"/> je true), 
        /// a aktuálně je zadané jméno parametru (<see cref="ParamName"/> není null)
        /// </summary>
        protected bool HasParameter { get { return (EnableDbParameters && (ParamName != null)); } }
        /// <summary>
        /// Db parametr.
        /// <para/>
        /// Hodnota by měla být čtena pokud možno co nejblíže k aplikaci výsledného SQL SELECTU, protože hodnota může obsahovat údaje platné v době čtení.
        /// Jinými slovy, hodnota může obsahovat aktuální datum a čas; a pokud vrácený údaj bude dlouhodobě skladován, bude ztrácet na aktuálnosti.
        /// <br/>
        /// To platí jak pro variantu s DB parametrem, tak i pro přímou citaci do výsledného textu.
        /// </summary>
        protected virtual DbParameter DbParameter { get { return (HasParameter ? new DbParameter(ParamName, CurrentValue) : null); } }
        /// <summary>
        /// Resetuje parametr = zahodí jej, následně bude SQL výraz obsahovat explicitní hodnotu, a nikoli parametr.
        /// </summary>
        internal void ResetDbParameters()
        {
            if (EnableDbParameters)
                _ParamName = null;
        }
        /// <summary>
        /// Přidá svůj parametr(y) do daného pole.
        /// </summary>
        /// <param name="dbParameters"></param>
        internal void AddDbParameters(List<DbParameter> dbParameters)
        {
            if (!EnableDbParameters) return;

            // Bylo by vhodné, aby parametry se stejnou hodnotou nebyly duplikované.
            // Typicky pokud stavím filtr na číslo pořadače int 5926, pak je zbytečné dodávat dva či více parametrů s tímto číslem.
            // Konec konců pokud by filtr potřeboval docela jiné atributy, ale na shodnou hodnotu
            //   (např. v jedné podmínce číslo pořadače int 5926 a současně v jiné podmínce na číslo vztahu int 5926),
            //   tak je SQL serveru srdečně jedno, kdyby pro obě podmínky měl použít shodný parametr @Par1, který má hodnotu int 5926).
            // Takže než založíme nový parametr, podíváme se v poli dbParameters, zda tam už není stejná hodnota:
            object value = this.Value;
            DbParameter dbParameter;
            if (dbParameters.TryFindFirst(out dbParameter, dbp => IsEqualValue(dbp.Value, value)))
            {   // Pro naši hodnotu Value už v poli parametrů 'dbParameters' existuje parametr zadaný někde jinde:
                // Použijeme tedy jeho jméno i pro nás, další nový parametr do pole nepřidáváme, a je hotovo:
                ParamName = dbParameter.Name;
                return;
            }

            // Naše hodnota ještě v poli parametrů není, přidáme ji tam jako nový parametr:
            ParamName = DbParameterNamePrefix + (dbParameters.Count + 1).ToString();
            dbParameter = this.DbParameter;

            if (dbParameter != null)
                dbParameters.Add(dbParameter);
        }
        /// <summary>
        /// Prefix jména parametru
        /// </summary>
        protected virtual string DbParameterNamePrefix { get { return "filtpar"; } }
        /// <summary>
        /// Konverze hodnoty do SQL notace
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string ToSql(object value)
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
        /// Vrátí platné jméno proměnné z daného textu, nebo null (odebere mezery, tečky a zavináče a uzenáče)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected string GetParamName(string name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (name.Contains(" ")) name = name.Replace(" ", "");
            if (name.Contains(".")) name = name.Replace(".", "");
            if (name.Contains("@")) name = name.Replace("@", "");
            if (String.IsNullOrEmpty(name)) return null;
            return name;
        }
        /// <summary>
        /// Vrátí true, pokud dva objekty obsahují shodnou hodnotu.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static bool IsEqualValue(object a, object b)
        {
            bool an = (a is null);
            bool bn = (b is null);
            if (an && bn) return true;
            if (an || bn) return false;
            // Oba objekty nejsou null:
            return (Type.Equals(a, b));
        }
        #endregion
        #region Dynamic Value (klíčové slovo, název časového období, ...)
        /// <summary>
        /// Typ dat
        /// </summary>
        protected ExpressionValueDataType DataType { get; set; }
        /// <summary>
        /// Metoda vrací dynamicky určenou hodnotu
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual object GetDynamicValue(object value)
        {
            //switch (this.DataType)
            //{
            //    case ExpressionValueDataType.Integer:
            //    case ExpressionValueDataType.Decimal:
            //    case ExpressionValueDataType.Relation:
            //        if (this._DynamicKey is string)
            //        {
            //            string keyword = (string)this._DynamicKey;
            //            var numeric = NrsFilterSupport.ConvertKeyWordToNumeric(keyword);
            //            value = numeric;
            //        }
            //        break;
            //    case ExpressionValueDataType.Date:
            //        if (this._DynamicKey is DateFilterKeyWord)
            //        {
            //            DateFilterKeyWord dateKey = (DateFilterKeyWord)this._DynamicKey;
            //            TimeRange timeRange = NrsFilterSupport.ConvertKeyWordToDateTimeRange(dateKey);
            //            value = timeRange;
            //        }
            //        break;
            //}
            return value;
        }
        /// <summary>
        /// Hodnota je dynamická = je nutno ji vyhodnotit až v okamžiku potřeby
        /// </summary>
        public bool IsDynamic { get { return _IsDynamic; } }
        private bool _IsDynamic;
        #endregion



    }
    #endregion
    #region class ExpressionPartColumn : třída obsahující sloupec
    /// <summary>
    /// Částice výrazu, obsahující sloupec (buď alias sloupce, nebo přímo sloupec tabulky, nebo i včetně klíče entity)
    /// </summary>
    public class ExpressionPartColumn : ExpressionPart
    {
        #region Základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="category"></param>
        /// <param name="column"></param>
        internal ExpressionPartColumn(TextPartCategory category, string column)
            : base(category)
        {
            Column = column;
            ColumnSource = null;
        }
        /// <summary>
        /// Jméno sloupce původní, nalezené v textu
        /// </summary>
        public string Column { get; private set; }
        /// <summary>
        /// Zdrojový výraz pro sloupec
        /// </summary>
        public string ColumnSource { get; set; }
        /// <summary>
        /// Výsledný text výrazu, v notaci SQL.
        /// Výstupní text = <see cref="ColumnSource"/> (pokud není null), nebo <see cref="Column"/> (výchozí stav).
        /// </summary>
        public override string ResultText { get { return ColumnSource ?? Column; } }
        /// <summary>
        /// Úvodní HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> před hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string BeginTagHtml { get { return "<span style=\"color:#1010C0\"><u>"; } }
        /// <summary>
        /// Závěrečné HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> za hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string EndTagHtml { get { return "</u></span>"; } }
        #endregion
        #region Práce se sloupcem
        #endregion
    }
    #endregion
    #region class ExpressionPartText : třída obsahující prostý text
    /// <summary>
    /// Částice výrazu, obsahující prostý text
    /// </summary>
    public class ExpressionPartText : ExpressionPart
    {
        #region Základní data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="text"></param>
        internal ExpressionPartText(string text)
            : base(TextPartCategory.Text)
        {
            this.SourceText = text;
            this.CurrentText = null;
        }
        /// <summary>
        /// Výchozí text.
        /// </summary>
        public string SourceText { get; private set; }
        /// <summary>
        /// Zadaný aktuální text. 
        /// Pokud bude null (výchozí stav), pak jak <see cref="ResultText"/> bude obsažen výchozí <see cref="SourceText "/>.
        /// </summary>
        public string CurrentText { get; set; }
        /// <summary>
        /// Výsledný text výrazu, v notaci SQL.
        /// Výstupní text = <see cref="CurrentText"/> (pokud není null), nebo <see cref="SourceText "/> (výchozí stav).
        /// </summary>
        public override string ResultText { get { return CurrentText ?? SourceText; } }
        /// <summary>
        /// Úvodní HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> před hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string BeginTagHtml { get { return "<span style=\"color:#101020\">"; } }
        /// <summary>
        /// Závěrečné HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> za hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected override string EndTagHtml { get { return "</span>"; } }
        #endregion
    }
    #endregion
    #region class ExpressionPart : bázová abstraktní třída pro všechny konkrétní potomky
    /// <summary>
    /// Částice výrazu
    /// </summary>
    public abstract class ExpressionPart
    {
        #region Factory metody
        /// <summary>
        /// Vrátí část textu obsahující prostý text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ExpressionPart CreatePartText(string text)
        {
            return new ExpressionPartText(text);
        }
        /// <summary>
        /// Vrátí část textu obsahující sloupec (buď alias sloupce, nebo přímo sloupec tabulky, nebo i včetně klíče entity)
        /// </summary>
        /// <param name="category"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public static ExpressionPart CreatePartColumn(TextPartCategory category, string column)
        {
            return new ExpressionPartColumn(category, column);
        }
        /// <summary>
        /// Vrátí část textu obsahující konstantní hodnotu
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ExpressionPart CreatePartValueConstant(string name, object value)
        {
            return new ExpressionPartValue(TextPartCategory.ValueConstant, name, value);
        }
        /// <summary>
        /// Vrátí část textu obsahující proměnnou hodnotu (klíčové slovo)
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="parsedItem"></param>
        /// <returns></returns>
        public static ExpressionPart CreatePartValueVariable(string variableName, ParsedItem parsedItem)
        {
            return new ExpressionPartVariable(variableName, parsedItem);
        }
        /// <summary>
        /// Protected konstruktor
        /// </summary>
        /// <param name="category"></param>
        protected ExpressionPart(TextPartCategory category)
        {
            Category = category;
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return this.ResultText; }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.Text"/>, 
        /// tedy typu <see cref="ExpressionPartText"/>.
        /// </summary>
        public bool IsText { get { return IsAnyCategory(TextPartCategory.Text); } }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.ColumnAlias"/> nebo <see cref="TextPartCategory.ColumnFull"/>, 
        /// tedy typu <see cref="ExpressionPartColumn"/>.
        /// </summary>
        public bool IsColumn { get { return IsAnyCategory(TextPartCategory.ColumnAlias, TextPartCategory.ColumnFull); } }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.ValueConstant"/> nebo <see cref="TextPartCategory.ValueVariable"/> nebo <see cref="TextPartCategory.ValueArgument"/>,
        /// tedy typu <see cref="ExpressionPartValue"/> nebo potomek.
        /// Tento typ může mít DbParametr.
        /// </summary>
        public bool IsValue { get { return IsAnyCategory(TextPartCategory.ValueConstant, TextPartCategory.ValueVariable, TextPartCategory.ValueArgument); } }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.ValueConstant"/>,
        /// tedy typu <see cref="ExpressionPartValue"/>.
        /// </summary>
        public bool IsConstant { get { return IsAnyCategory(TextPartCategory.ValueConstant); } }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.ValueVariable"/>,
        /// tedy typu <see cref="ExpressionPartVariable"/>.
        /// </summary>
        public bool IsVariable { get { return IsAnyCategory(TextPartCategory.ValueVariable); } }
        /// <summary>
        /// Obsahuje true, pokud this instance je kategorie <see cref="TextPartCategory.ValueArgument"/>,
        /// tedy typu <see cref="ExpressionPartArgument"/>.
        /// </summary>
        public bool IsArgument { get { return IsAnyCategory(TextPartCategory.ValueArgument); } }
        /// <summary>
        /// Vrátí true, pokud zdejší <see cref="Category"/> je některá z daných kategorií
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        protected bool IsAnyCategory(params TextPartCategory[] categories)
        {
            return categories.Any(c => c == this.Category);
        }
        #endregion
        #region Vlastní data
        /// <summary>
        /// Kategorie částice
        /// </summary>
        public TextPartCategory Category { get; protected set; }
        /// <summary>
        /// Výsledný text výrazu, v notaci SQL.
        /// <para/>
        /// Hodnota by měla být čtena pokud možno co nejblíže k aplikaci výsledného SQL SELECTU, protože hodnota může obsahovat údaje platné v době čtení.
        /// Jinými slovy, hodnota může obsahovat aktuální datum a čas; a pokud vrácený údaj bude dlouhodobě skladován, bude ztrácet na aktuálnosti.
        /// </summary>
        public abstract string ResultText { get; }
        #endregion
        #region Podpora pro tvorbu sumárního textu SQL a HTML
        /// <summary>
        /// Sloučí dodané prvky do jednoho prostého textu
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static string MergeToText(IEnumerable<ExpressionPart> parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in parts)
                sb.Append(part.ResultText);
            return sb.ToString();
        }
        /// <summary>
        /// Sloučí dodané prvky do jednoho HTML textu
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static string MergeToHtml(IEnumerable<ExpressionPart> parts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DocBeginTagHtml);
            foreach (var part in parts)
                sb.Append(part.ResultTextHtml);
            sb.Append(DocEndTagHtml);
            return sb.ToString();
        }
        /// <summary>
        /// Výsledný text výrazu, v notaci HTML, pro zvýraznění obsahu / syntaxe
        /// </summary>
        protected virtual string ResultTextHtml { get { return BeginTagHtml + ResultText + EndTagHtml; } }
        /// <summary>
        /// Úvodní HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> před hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected virtual string BeginTagHtml { get { return ""; } }
        /// <summary>
        /// Závěrečné HTML TAGy vkládané do hodnoty <see cref="ExpressionPart.ResultTextHtml"/> za hodnotu <see cref="ExpressionPart.ResultText"/>
        /// </summary>
        protected virtual string EndTagHtml { get { return ""; } }
        /// <summary>
        /// Úvodní HTML tagy na začátek dokumentu, před sloučený text prvků
        /// </summary>
        protected static string DocBeginTagHtml { get { return "<html><body style=\"background-color:#EEEEFF;\"><p align=\"left\"><code>"; } }
        /// <summary>
        /// Závěrečné HTML tagy na konec dokumentu, za sloučený text prvků
        /// </summary>
        protected static string DocEndTagHtml { get { return "</code></p></body></html>"; } }
        #endregion
    }
    #endregion
    #region enum TextPartCategory, ExpressionValueDataType
    /// <summary>
    /// Kategorie textu
    /// </summary>
    public enum TextPartCategory
    {
        /// <summary>
        /// Nezadáno. Pro účely parametrování. Žádná existující částice by neměla mít tuto hodnotu.
        /// </summary>
        None,
        /// <summary>
        /// Prostý text
        /// </summary>
        Text,
        /// <summary>
        /// Alias sloupce
        /// </summary>
        ColumnAlias,
        /// <summary>
        /// Název zdrojového sloupce
        /// </summary>
        ColumnFull,
        /// <summary>
        /// Konstantní hodnota
        /// </summary>
        ValueConstant,
        /// <summary>
        /// Proměnná hodnota = klíčové slovo atd.
        /// </summary>
        ValueVariable,
        /// <summary>
        /// Uživatelem zadávaná hodnota v okně parametrů
        /// </summary>
        ValueArgument
    }
    #endregion
    #endregion

    // náhražky pro překlad
    /// <summary>
    /// Typ hodnoty
    /// </summary>
    public enum ExpressionValueDataType
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Boolean
        /// </summary>
        Boolean,
        /// <summary>
        /// Integer
        /// </summary>
        Integer,
        /// <summary>
        /// Decimal
        /// </summary>
        Decimal,
        /// <summary>
        /// Relation
        /// </summary>
        Relation,
        /// <summary>
        /// Date
        /// </summary>
        Date,

    }
    /// <summary>
    /// ParsedItem
    /// </summary>
    public class ParsedItem
    { }
    /// <summary>
    /// DbParameter
    /// </summary>
    public class DbParameter
    {
        /// <summary>
        /// DbParameter
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        public DbParameter(string paramName, object value)
        {
            this.Name = paramName;
            this.Value = value;
        }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Value
        /// </summary>
        public object Value { get; set; }
    }
    /// <summary>
    /// NrsExtensions
    /// </summary>
    public static class NrsExtensions
    {
        /// <summary>
        /// Najdi první
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="found"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool TryFindFirst<T>(this IEnumerable<T> items, out T found, Func<T, bool> filter) where T : class
        {
            found = items.FirstOrDefault(filter);
            return (found != null);
        }
    }
}
