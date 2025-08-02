using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxFilter = DevExpress.Data.Filtering;

namespace TestDevExpress.AsolDX.News
{
    #region class DxToExpressionConvertor : public konvertor dat z filtru DX do formy MS SQL atd
    /// <summary>
    /// <see cref="DxToExpressionConvertor"/> : public konvertor dat z filtru DX do formy MS SQL atd
    /// </summary>
    internal class DxToExpressionConvertor
    {
        #region Public members
        public static string ConvertToString(string dxExpression, DxToExpressionLanguageType language)
        {
            var filter = DxFilter.CriteriaOperator.Parse(dxExpression);
            return ConvertToString(filter, language);
        }
        public static string ConvertToString(DxFilter.CriteriaOperator filter, DxToExpressionLanguageType language)
        {
            if (filter is null) return null;

            var visitor = new DxToExpressionVisitor(language);
            var result = filter.Accept(visitor);
            return result.ToText(language);
        }
        internal static string FormatPropertyName(string propertyName, DxToExpressionLanguageType language)
        {
            return $"[{propertyName}]";
        }
        internal static string FormatValue(object value, DxToExpressionLanguageType language)
        {
            return $"'{value}'";
        }
        #endregion
    }
    #endregion
    #region class DxToExpressionVisitor : Rekurzivní konverzní třída pro vlastní převod DxFilter.CriteriaOperator do výsledných částí
    internal class DxToExpressionVisitor : DxFilter.ICriteriaVisitor<DxToExpressionPart>, DxFilter.IClientCriteriaVisitor<DxToExpressionPart>
    {
        #region Konstruktor a jazyk
        internal DxToExpressionVisitor(DxToExpressionLanguageType language)
        {
            __Language = language;
        }
        private DxToExpressionLanguageType __Language;
        #endregion
        #region Visitors
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.GroupOperator groupOperator)
        {
            var operandItems = ConvertOperands(groupOperator.Operands, ConvertOperandsMode.RemoveEmptyItems);
            var operandsCount = operandItems?.Count ?? 0;
            if (operandsCount == 0) return null;                                                   // null reprezentuje stav, kdy daný fragment vynecháváme.

            // Jediný prvek nepotřebuje závorky ani delimiter:
            if (operandsCount == 1) return operandItems[0];

            // Výsledek: pokud je v něm více než 1 prvek, pak jej ozávorkujeme :              (a < 1 or b > 10)   
            //   aby navazující vyšší grupa (např. AND) byla napojena validně  :   c = 10 and (a < 1 or b > 10)
            //   kdežto bez závorek by to dopadlo špatně                       :   c = 10 and  a < 1 or b > 10
            //   ....  protože AND mívá přednost, takže význam by byl          :  (c = 10 and a < 1) or b > 10 
            string delimiter = (groupOperator.OperatorType == DxFilter.GroupOperatorType.And ? " and " : (groupOperator.OperatorType == DxFilter.GroupOperatorType.Or ? " or " : ", "));
            return DxToExpressionPart.CreateFrom("(", DxToExpressionPart.CreateDelimited(delimiter, operandItems), ")");
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.BetweenOperator betweenOperator)
        {
            var testItem = betweenOperator.TestExpression.Accept(this);                        // Testovaná hodnota, sloupec
            var beginItem = betweenOperator.BeginExpression.Accept(this);                      // Dolní hodnota
            var endItem = betweenOperator.EndExpression.Accept(this);                          // Horní hodnota, včetně
            if (testItem is null || beginItem is null || endItem is null) return null;         // null reprezentuje stav, kdy daný fragment vynecháváme.
            return DxToExpressionPart.CreateFrom(testItem, " between ", beginItem, " and ", endItem);
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.BinaryOperator binaryOperator)
        {
            var leftItem = binaryOperator.LeftOperand.Accept(this);
            var rightItem = binaryOperator.RightOperand.Accept(this);
            if (leftItem is null || rightItem is null) return null;                            // null reprezentuje stav, kdy daný fragment vynecháváme.

            switch (binaryOperator.OperatorType)
            {
                case DxFilter.BinaryOperatorType.Equal:
                    return DxToExpressionPart.CreateFrom(leftItem, " = ", rightItem);
                case DxFilter.BinaryOperatorType.NotEqual:
                    return DxToExpressionPart.CreateFrom(leftItem, " <> ", rightItem);
                case DxFilter.BinaryOperatorType.Greater:
                    return DxToExpressionPart.CreateFrom(leftItem, " > ", rightItem);
                case DxFilter.BinaryOperatorType.Less:
                    return DxToExpressionPart.CreateFrom(leftItem, " < ", rightItem);
                case DxFilter.BinaryOperatorType.LessOrEqual:
                    return DxToExpressionPart.CreateFrom(leftItem, " <= ", rightItem);
                case DxFilter.BinaryOperatorType.GreaterOrEqual:
                    return DxToExpressionPart.CreateFrom(leftItem, " >= ", rightItem);
                case DxFilter.BinaryOperatorType.Like:
                    return DxToExpressionPart.CreateFrom(leftItem, " like ", rightItem);
                case DxFilter.BinaryOperatorType.BitwiseAnd:
                    return DxToExpressionPart.CreateFrom(leftItem, " & ", rightItem);
                case DxFilter.BinaryOperatorType.BitwiseOr:
                    return DxToExpressionPart.CreateFrom(leftItem, " | ", rightItem);
                case DxFilter.BinaryOperatorType.BitwiseXor:
                    return DxToExpressionPart.CreateFrom(leftItem, " ~ ", rightItem);
                case DxFilter.BinaryOperatorType.Divide:
                    return DxToExpressionPart.CreateFrom(leftItem, " / ", rightItem);
                case DxFilter.BinaryOperatorType.Modulo:
                    return DxToExpressionPart.CreateFrom(leftItem, " % ", rightItem);
                case DxFilter.BinaryOperatorType.Multiply:
                    return DxToExpressionPart.CreateFrom(leftItem, " * ", rightItem);
                case DxFilter.BinaryOperatorType.Plus:
                    return DxToExpressionPart.CreateFrom(leftItem, " + ", rightItem);
                case DxFilter.BinaryOperatorType.Minus:
                    return DxToExpressionPart.CreateFrom(leftItem, " - ", rightItem);
            }
            throw new NotImplementedException($"DxToExpressionVisitor do not implement BinaryOperator: {binaryOperator.OperatorType}.");
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.UnaryOperator unaryOperator)
        {
            var operatorItem = unaryOperator.Operand.Accept(this);                                 // Sloupec nebo hodnota nebo výsledný výraz...   např. "lcs.subjekty.nazev_subjektu"  nebo  "polozka.pocet" 
            if (operatorItem is null) return null;                                                 // null reprezentuje stav, kdy daný fragment vynecháváme.

            switch (unaryOperator.OperatorType)
            {
                case DxFilter.UnaryOperatorType.BitwiseNot:
                    return DxToExpressionPart.CreateFrom(" ~", operatorItem);
                case DxFilter.UnaryOperatorType.Plus:
                    return operatorItem;
                case DxFilter.UnaryOperatorType.Minus:
                    return DxToExpressionPart.CreateFrom(" -", operatorItem);
                case DxFilter.UnaryOperatorType.Not:
                    return DxToExpressionPart.CreateFrom(" not(", operatorItem, ")");
                case DxFilter.UnaryOperatorType.IsNull:
                    return DxToExpressionPart.CreateFrom(operatorItem, " is null");
            }
            throw new NotImplementedException($"DxToExpressionVisitor do not implement UnaryOperator: {unaryOperator.OperatorType}.");
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.InOperator inOperator)
        {
            var leftItem = inOperator.LeftOperand.Accept(this);
            if (leftItem is null) return null;                                                 // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operandItems = ConvertOperands(inOperator.Operands, ConvertOperandsMode.RemoveEmptyItems);
            int operandCount = operandItems.Count;
            if (operandCount == 0) return null;                                                // null reprezentuje stav, kdy daný fragment vynecháváme.
            if (operandCount == 1) return DxToExpressionPart.CreateFrom(leftItem, " = ", operandItems[0]);                       // Pokud IN má jen jednu hodnotu, pak vrátím:     cislo_poradace = 1234
            return DxToExpressionPart.CreateFrom(leftItem, " in (", DxToExpressionPart.CreateDelimited(",", operandItems), ")"); // Více hodnot:                                   cislo_poradace in (1234,1235,1236,1238)
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.FunctionOperator functionOperator)
        {
            // Jednotlivé operandy funkce, a jejich počet:
            List<DxToExpressionPart> operandItems;
            int operandsCount;

            //  DevExpress : https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering.FunctionOperatorType
            //  MS SQL Math: https://learn.microsoft.com/en-us/sql/t-sql/functions/mathematical-functions-transact-sql?view=sql-server-ver17
            switch (functionOperator.OperatorType)
            {
                case DxFilter.FunctionOperatorType.None:
                    return null;
                case DxFilter.FunctionOperatorType.Custom:
                    break;
                case DxFilter.FunctionOperatorType.CustomNonDeterministic:
                    break;


                //   =================  LOGICKÉ FUNKCE  =================
                case DxFilter.FunctionOperatorType.Iif:
                    // Returns one of the specified values depending upon the values of logical expressions. The function can take 2N+1 arguments (where N is the number of specified logical expressions):
                    // Each odd argument specifies a logical expression.
                    // Each even argument specifies the value that is returned if the previous expression evaluates to True.
                    // The last argument specifies the value that is returned if the previously evaluated logical expressions yield False.
                    // If you pass only one argument, the passed argument is returned.
                    // If you compare a 0(zero) with a Null value, the expression evaluates to True.
                    // Examples: Iif(Name = 'Bob', 1, 0)
                    //           Iif(Name = 'Bob', 1, Name = 'Dan', 2, Name = 'Sam', 3, 0)
                    loadOperands();
                    if (operandsCount == 0) invalidOperandsCount("1 or more");                           // Bez argumentů = nevalidní
                    if ((operandsCount % 2) == 0) invalidOperandsCount("1, 3, 5, 7 .. (=odd count)");    // Musí jich být lichý počet
                    if (operandsCount == 1) return operandItems[0];                                      // If you pass only one argument, the passed argument is returned.
                    var iifPart = DxToExpressionPart.CreateFrom("(case ");                               // (case when ... 
                    for (int i = 0; i < operandsCount; i += 2)
                        iifPart.AddRange("when ", operandItems[i], " then ", operandItems[i + 1], " ");  //   ... [Name] = 'Bob' then 1 ...       přičemž operandItems[i] je logický: "[Name] = 'Bob'" a operandItems[i + 1] je hodnota: "1"
                    iifPart.AddRange("else ", operandItems[operandsCount - 1], " end)");                 //   ... else 0 end)
                    return iifPart;
                case DxFilter.FunctionOperatorType.IsNull:
                    // Compares the first operand with the NULL value. This function requires one or two operands of the CriteriaOperator class. The returned value depends on the number of arguments (one or two arguments).
                    //  True / False: If a single operand is passed, the function returns True if the operand is null; otherwise, False.
                    //  Value1 / Value2: If two operands are passed, the function returns the first operand if it is not set to NULL; otherwise, the second operand is returned.
                    loadOperands();
                    if (operandsCount == 1) return DxToExpressionPart.CreateFrom(operandItems[0], " is null");                             // [datum_akce] is null
                    if (operandsCount == 2) return DxToExpressionPart.CreateFrom("isnull(", operandItems[0], ", ", operandItems[1], ")");  // isnull([datum_akce], [datum_podani])
                    invalidOperandsCount("1 or 2");
                    break;
                case DxFilter.FunctionOperatorType.IsNullOrEmpty:
                    // Returns True if the specified value is null or an empty string. Otherwise, returns False.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("(", operandItems[0], " is null or trim(", operandItems[0], ") = '')");


                //   =================  STRINGOVÉ FUNKCE  =================
                case DxFilter.FunctionOperatorType.Trim:
                    //Returns a string that is a copy of the specified string with all white-space characters removed from the start and end of the specified string.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("trim(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Len:
                    // Returns the length of the string specified by an operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("len(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Substring:
                    // Returns a substring from the specified string. This function requires two or three operands.
                    // If two operands are passed, the substring starts from the beginning of the specified string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    // If three operands are passed, a substring starts from the specified position in the source string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    //   3 - an integer that specifies the length of the substring.
                    loadOperands();
                    if (operandsCount == 2) return DxToExpressionPart.CreateFrom("substring(", operandItems[0], ",(1+", operandItems[1], "),9999)");                     // substring([poznamka], (1+40), 9999) : DevExpress umožňuje 2 argumenty (string, begin), ale SQL server chce povinně 3, kde třetí = délka
                    if (operandsCount == 3) return DxToExpressionPart.CreateFrom("substring(", operandItems[0], ",(1+", operandItems[1], "),", operandItems[2], ")");    // substring([poznamka], (1+40), 25)   : DevExpress má počátek substringu zero-based ("integer that specifies the zero-based position at which the substring starts."), ale SQL server má base 1
                    invalidOperandsCount("2 or 3");
                    break;
                case DxFilter.FunctionOperatorType.Upper:
                    // Converts all characters in a string operand to uppercase in an invariant culture.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("upper(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Lower:
                    // Converts all characters in a string operand to lowercase in an invariant culture.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("lower(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Concat:
                    // Concatenates the specified strings.
                    loadOperands();
                    // SQL server pro funkci CONCAT vyžaduje nejméně dva parametry; proto pro méně operandů provádím konverze jinak:
                    if (operandsCount == 0) return DxToExpressionPart.CreateText("''");                                          // concat()                            vrátí ''
                    if (operandsCount == 1) return operandItems[0];                                                              // concat([nazev])                     vrátí [nazev]
                    return DxToExpressionPart.CreateFrom("concat(", DxToExpressionPart.CreateDelimited(",", operandItems), ")"); // concat([nazev1], ',', [nazev2])     vrátí concat([nazev1], ',', [nazev2])   = to je SQL validní
                case DxFilter.FunctionOperatorType.Ascii:
                    // Returns the ASCII code of the first character in a string operand.
                    // If the argument is an empty string, the null value is returned.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("ascii(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Char:
                    // Converts a numeric operand to a Unicode character.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("char(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.ToStr:
                case DxFilter.FunctionOperatorType.Replace:
                case DxFilter.FunctionOperatorType.Reverse:
                case DxFilter.FunctionOperatorType.Insert:
                case DxFilter.FunctionOperatorType.CharIndex:
                case DxFilter.FunctionOperatorType.Remove:

                //   =================  MATEMATICKÉ FUNKCE  =================
                case DxFilter.FunctionOperatorType.Abs:
                    // Returns the absolute value of a numeric operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("abs(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Sqr:
                    // Returns the square root of a specified numeric operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("sqrt(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Cos:
                    // Returns the cosine of the numeric operand, in radians.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cos(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Sin:
                    // Returns the sine of the numeric operand, in radians.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("sin(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Atn:
                    // Returns the arctangent (the inverse tangent function) of the numeric operand. The arctangent is the angle in the range -π/2 to π/2 radians, whose tangent is the numeric operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("atan(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Exp:
                    // Returns the number e raised to the power specified by a numeric operand.
                    //   If the specified operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Exp function reverses the FunctionOperatorType.Log function. Use the FunctionOperatorType.Power operand to calculate powers of other bases.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("exp(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Log:
                    // Returns the logarithm of the specified numeric operand. The return value depends upon the number of operands.
                    // If one operand is passed, the function returns the natural(base e) logarithm of a specified operand.
                    // If two operands are passed, the function returns the logarithm of the specified operand to the specified base.The operands are:
                    //   1 - a number whose logarithm is to be calculated.
                    //   2 - the base of the logarithm.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Log function reverses the FunctionOperatorType.Exp function. To calculate the base - 10 logarithm, use the FunctionOperatorType.Log10 function.
                    loadOperands();
                    if (operandsCount == 1) return DxToExpressionPart.CreateFrom("log(", operandItems[0], ")");
                    if (operandsCount == 2) return DxToExpressionPart.CreateFrom("log(", operandItems[0], ",", operandItems[1], ")");
                    invalidOperandsCount("1 or 2");
                    break;
                case DxFilter.FunctionOperatorType.Rnd:
                    // Returns a random number greater than or equal to 0.0, and less than 1.0.
                    return DxToExpressionPart.CreateText("rand()");
                case DxFilter.FunctionOperatorType.Tan:
                    // Returns the tangent of the specified numeric operand that is an angle in radians.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("tan(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Power:
                    // Returns a specified numeric operand raised to a specified power.
                    // The operands are:
                    //  1 - the base number.
                    //  2 - the exponent to which the base number is raised.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Power function reverses the FunctionOperatorType.Log or FunctionOperatorType.Log10 function. Use the FunctionOperatorType.Exp operand to calculate powers of the number e.
                    loadCheckOperands(2);
                    return DxToExpressionPart.CreateFrom("power(", operandItems[0], ",", operandItems[1], ")");
                case DxFilter.FunctionOperatorType.Sign:
                    // Returns an integer that indicates the sign of a number. The function returns 1 for positive numbers, -1 for negative numbers, and 0 (zero) if a number is equal to zero.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("sign(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Round:
                    // Rounds a specified numeric operand to the nearest integer or to a specified number of fractional digits.
                    // The operands are:
                    // 1 - a value to round.
                    // 2 - (optional)the number of decimal places to which to round. 0 indicates that the first operand is rounded to the nearest integer.
                    loadOperands();
                    if (operandsCount == 1) return DxToExpressionPart.CreateFrom("round(", operandItems[0], ", 0)");
                    if (operandsCount == 2) return DxToExpressionPart.CreateFrom("round(", operandItems[0], ",", operandItems[1], ")");
                    invalidOperandsCount("1 or 2");
                    break;
                case DxFilter.FunctionOperatorType.Ceiling:
                    // Returns the smallest integral value greater than or equal to the specified numeric operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("ceiling(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Floor:
                    // Returns the largest integral value less than or equal to the specified numeric operand.
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("floor(", operandItems[0], ")");
                case DxFilter.FunctionOperatorType.Max:
                    // Returns the larger of two numeric values.
                    loadCheckOperands(2);
                    return DxToExpressionPart.CreateFrom("(case when ", operandItems[0], " > ", operandItems[1], " then ", operandItems[0], " else ", operandItems[1], " end)");   // (case when pocet1 > pocet2 then pocet1 else pocet2 end)
                    // Pokud bychom chtěli řešit více než 2 vstupní čísla, pak SQL dovoluje použít inlinovanou tabulku:
                    // (select max(val) from (values (cislo_poradace), (cislo_subjektu), (pachatel)) AS t(val))
                case DxFilter.FunctionOperatorType.Min:
                    // Returns the smaller of two numeric values.
                    loadCheckOperands(2);
                    return DxToExpressionPart.CreateFrom("(case when ", operandItems[0], " < ", operandItems[1], " then ", operandItems[0], " else ", operandItems[1], " end)");   // (case when pocet1 < pocet2 then pocet1 else pocet2 end)
                case DxFilter.FunctionOperatorType.Acos:
                case DxFilter.FunctionOperatorType.Asin:
                case DxFilter.FunctionOperatorType.Atn2:
                case DxFilter.FunctionOperatorType.BigMul:
                case DxFilter.FunctionOperatorType.Cosh:
                case DxFilter.FunctionOperatorType.Log10:
                case DxFilter.FunctionOperatorType.Sinh:
                case DxFilter.FunctionOperatorType.Tanh:


                //   =================  STRINGOVÉ FUNKCE  =================
                case DxFilter.FunctionOperatorType.PadLeft:
                case DxFilter.FunctionOperatorType.PadRight:
                    break;
                case DxFilter.FunctionOperatorType.StartsWith:
                    loadCheckOperands(2);
                    return DxToExpressionPart.CreateFrom(operandItems[0], " like (", operandItems[1], " + '%')");                // [nazev] like (N'adr' + '%')
                case DxFilter.FunctionOperatorType.EndsWith:
                    loadCheckOperands(2);
                    return DxToExpressionPart.CreateFrom(operandItems[0], " like ('%' + ", operandItems[1], ")");                // [nazev] like ('%' + N'adr')
                case DxFilter.FunctionOperatorType.Contains:
                    loadCheckOperands(2);
                    if (operandItems[1].IsValueString)
                        // Pokud hodnota je zadána jako konstanta typu Text (=nejčastější situace), 
                        //  pak do výstupu dáme novou hodnotu typu String s upraveným vstupním obsahem:
                        return DxToExpressionPart.CreateFrom(operandItems[0], " like ", DxToExpressionPart.CreateValue("%" + operandItems[1].ValueString + "%"));   // [nazev] like '%adr%'
                    else
                        // Pokud hodnota není stringová konstanta, pak do výsledku musíme dát vzorec: '%' + ... + '%' :
                        return DxToExpressionPart.CreateFrom(operandItems[0], " like ('%' + ", operandItems[1], " + '%')");                                         // [nazev] like ('%' + N'adr' + '%')
                case DxFilter.FunctionOperatorType.ToInt:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cast(", operandItems[0], " as int)");
                case DxFilter.FunctionOperatorType.ToLong:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cast(", operandItems[0], " as bigint)");
                case DxFilter.FunctionOperatorType.ToFloat:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cast(", operandItems[0], " as decimal(19,6))");
                case DxFilter.FunctionOperatorType.ToDouble:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cast(", operandItems[0], " as decimal(19,6))");
                case DxFilter.FunctionOperatorType.ToDecimal:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("cast(", operandItems[0], " as decimal(19,6))");


                //   =================  DATETIME FUNKCE  =================
                case DxFilter.FunctionOperatorType.LocalDateTimeThisYear:
                case DxFilter.FunctionOperatorType.LocalDateTimeThisMonth:
                case DxFilter.FunctionOperatorType.LocalDateTimeLastWeek:
                case DxFilter.FunctionOperatorType.LocalDateTimeThisWeek:
                case DxFilter.FunctionOperatorType.LocalDateTimeYesterday:
                case DxFilter.FunctionOperatorType.LocalDateTimeToday:
                case DxFilter.FunctionOperatorType.LocalDateTimeNow:
                case DxFilter.FunctionOperatorType.LocalDateTimeTomorrow:
                case DxFilter.FunctionOperatorType.LocalDateTimeDayAfterTomorrow:
                case DxFilter.FunctionOperatorType.LocalDateTimeNextWeek:
                case DxFilter.FunctionOperatorType.LocalDateTimeTwoWeeksAway:
                case DxFilter.FunctionOperatorType.LocalDateTimeNextMonth:
                case DxFilter.FunctionOperatorType.LocalDateTimeNextYear:
                case DxFilter.FunctionOperatorType.LocalDateTimeTwoMonthsAway:
                case DxFilter.FunctionOperatorType.LocalDateTimeTwoYearsAway:
                case DxFilter.FunctionOperatorType.LocalDateTimeLastMonth:
                case DxFilter.FunctionOperatorType.LocalDateTimeLastYear:
                case DxFilter.FunctionOperatorType.LocalDateTimeYearBeforeToday:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalBeyondThisYear:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalLaterThisYear:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalLaterThisMonth:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalNextWeek:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalLaterThisWeek:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalTomorrow:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalToday:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalYesterday:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalEarlierThisWeek:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalLastWeek:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalEarlierThisMonth:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalEarlierThisYear:
                case DxFilter.FunctionOperatorType.IsOutlookIntervalPriorThisYear:
                case DxFilter.FunctionOperatorType.IsThisWeek:
                case DxFilter.FunctionOperatorType.IsThisMonth:
                case DxFilter.FunctionOperatorType.IsThisYear:
                case DxFilter.FunctionOperatorType.IsNextMonth:
                case DxFilter.FunctionOperatorType.IsNextYear:
                case DxFilter.FunctionOperatorType.IsLastMonth:
                case DxFilter.FunctionOperatorType.IsLastYear:
                case DxFilter.FunctionOperatorType.IsYearToDate:
                case DxFilter.FunctionOperatorType.IsSameDay:
                case DxFilter.FunctionOperatorType.InRange:
                case DxFilter.FunctionOperatorType.InDateRange:
                    break;

                case DxFilter.FunctionOperatorType.IsJanuary:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(1));
                case DxFilter.FunctionOperatorType.IsFebruary:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(2));
                case DxFilter.FunctionOperatorType.IsMarch:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(3));
                case DxFilter.FunctionOperatorType.IsApril:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(4));
                case DxFilter.FunctionOperatorType.IsMay:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(5));
                case DxFilter.FunctionOperatorType.IsJune:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(6));
                case DxFilter.FunctionOperatorType.IsJuly:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(7));
                case DxFilter.FunctionOperatorType.IsAugust:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(8));
                case DxFilter.FunctionOperatorType.IsSeptember:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(9));
                case DxFilter.FunctionOperatorType.IsOctober:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(10));
                case DxFilter.FunctionOperatorType.IsNovember:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(11));
                case DxFilter.FunctionOperatorType.IsDecember:
                    loadCheckOperands(1);
                    return DxToExpressionPart.CreateFrom("month(", operandItems[0], ") = ", DxToExpressionPart.CreateValue(12));

                case DxFilter.FunctionOperatorType.DateDiffTick:
                case DxFilter.FunctionOperatorType.DateDiffSecond:
                case DxFilter.FunctionOperatorType.DateDiffMilliSecond:
                case DxFilter.FunctionOperatorType.DateDiffMinute:
                case DxFilter.FunctionOperatorType.DateDiffHour:
                case DxFilter.FunctionOperatorType.DateDiffDay:
                case DxFilter.FunctionOperatorType.DateDiffMonth:
                case DxFilter.FunctionOperatorType.DateDiffYear:
                case DxFilter.FunctionOperatorType.GetDate:
                case DxFilter.FunctionOperatorType.GetMilliSecond:
                case DxFilter.FunctionOperatorType.GetSecond:
                case DxFilter.FunctionOperatorType.GetMinute:
                case DxFilter.FunctionOperatorType.GetHour:
                case DxFilter.FunctionOperatorType.GetDay:
                case DxFilter.FunctionOperatorType.GetMonth:
                case DxFilter.FunctionOperatorType.GetYear:
                case DxFilter.FunctionOperatorType.GetDayOfWeek:
                case DxFilter.FunctionOperatorType.GetDayOfYear:
                case DxFilter.FunctionOperatorType.GetTimeOfDay:
                    break;
                case DxFilter.FunctionOperatorType.Now:
                    // Returns the DateTime value that is the current date and time.
                    return DxToExpressionPart.CreateText("getdate()");
                case DxFilter.FunctionOperatorType.UtcNow:
                    // Returns a DateTime object that is the current date and time in Universal Coordinated Time (UTC).
                    return DxToExpressionPart.CreateText("getutcdate()");
                case DxFilter.FunctionOperatorType.Today:
                    // Returns a DateTime value that is the current date. The time part is set to 00:00:00.
                    return DxToExpressionPart.CreateText("datetrunc(d, getdate())");
                case DxFilter.FunctionOperatorType.TruncateToMinute:
                    return DxToExpressionPart.CreateText("datetrunc(mi, getdate())");
                case DxFilter.FunctionOperatorType.IsSameHour:
                case DxFilter.FunctionOperatorType.IsSameTime:
                case DxFilter.FunctionOperatorType.BeforeMidday:
                case DxFilter.FunctionOperatorType.AfterMidday:
                case DxFilter.FunctionOperatorType.IsNight:
                case DxFilter.FunctionOperatorType.IsMorning:
                case DxFilter.FunctionOperatorType.IsAfternoon:
                case DxFilter.FunctionOperatorType.IsEvening:
                case DxFilter.FunctionOperatorType.IsLastHour:
                case DxFilter.FunctionOperatorType.IsThisHour:
                case DxFilter.FunctionOperatorType.IsNextHour:
                case DxFilter.FunctionOperatorType.IsWorkTime:
                case DxFilter.FunctionOperatorType.IsFreeTime:
                case DxFilter.FunctionOperatorType.IsLunchTime:
                case DxFilter.FunctionOperatorType.AddTimeSpan:
                case DxFilter.FunctionOperatorType.AddTicks:
                case DxFilter.FunctionOperatorType.AddMilliSeconds:
                case DxFilter.FunctionOperatorType.AddSeconds:
                case DxFilter.FunctionOperatorType.AddMinutes:
                case DxFilter.FunctionOperatorType.AddHours:
                case DxFilter.FunctionOperatorType.AddDays:
                case DxFilter.FunctionOperatorType.AddMonths:
                case DxFilter.FunctionOperatorType.AddYears:
                case DxFilter.FunctionOperatorType.DateTimeFromParts:
                case DxFilter.FunctionOperatorType.DateOnlyFromParts:
                case DxFilter.FunctionOperatorType.TimeOnlyFromParts:
                    break;
            }

#warning   ===========   Pouze pro testy ... do ostré veze SMAZAT !!!   ===========

            loadOperands();
            return DxToExpressionPart.CreateFrom(functionOperator.OperatorType.ToString(), "(", DxToExpressionPart.CreateDelimited(",", operandItems), ")");


            throw new NotImplementedException($"DxToExpressionVisitor do not implement FunctionOperator: {functionOperator.OperatorType}.");

            // Načítání a kontroly operandů:
            void loadOperands()
            {
                operandItems = ConvertOperands(functionOperator.Operands, ConvertOperandsMode.StrictlyAllItems);
                operandsCount = operandItems?.Count ?? -1;
            }
            void loadCheckOperands(int? count)
            {
                loadOperands();

                // Kontrola počtu záznamů, a případná chyba:
                if (count.HasValue && operandsCount != count.Value)
                    invalidOperandsCount(count.Value.ToString());
            }
            void invalidOperandsCount(string countInfo)
            {
                if (operandsCount < 0)
                    throw new ArgumentException($"Function '{functionOperator.OperatorType}'() requires {countInfo} operators, but any from the supplied operators is not valid.");
                else
                    throw new ArgumentException($"Function '{functionOperator.OperatorType}'() requires {countInfo} operators, but {operandsCount} valid operators are passed.");
            }
        }
        DxToExpressionPart DxFilter.ICriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.OperandValue operandValue)
        {
            return DxToExpressionPart.CreateValue(operandValue.Value);
        }
        DxToExpressionPart DxFilter.IClientCriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.OperandProperty theOperand)
        {
            return DxToExpressionPart.CreateProperty(theOperand.PropertyName);
        }
        DxToExpressionPart DxFilter.IClientCriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.AggregateOperand theOperand)
        {
            return DxToExpressionPart.CreateText("aggregateoperand ");
        }
        DxToExpressionPart DxFilter.IClientCriteriaVisitor<DxToExpressionPart>.Visit(DxFilter.JoinOperand theOperand)
        {
            return DxToExpressionPart.CreateText("joinoperand ");
        }

        /// <summary>
        /// Z dodaného pole operandů <paramref name="operands"/> konvertuje jejich obsah do stringů a vrátí. 
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="operands"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private List<DxToExpressionPart> ConvertOperands(DxFilter.CriteriaOperatorCollection operands, ConvertOperandsMode mode)
        {
            var operandItems = new List<DxToExpressionPart>();
            foreach (var operand in operands)
            {
                var operandItem = operand.Accept(this);
                if (operandItem != null)                                                           // null reprezentuje stav, kdy daný fragment vynecháváme.
                    operandItems.Add(operandItem);                                                 // Toto je validní operátor
                else if (mode == ConvertOperandsMode.StrictlyAllItems)                             // Nevalidní operátory nemáme akceptovat:
                    return null;                                                                   // null reprezentuje stav, kdy daný fragment vynecháváme.
            }
            return operandItems;
        }
        /// <summary>
        /// Jak řešit operand, který není možno konvertovat?
        /// </summary>
        private enum ConvertOperandsMode
        {
            /// <summary>
            /// Nezadáno
            /// </summary>
            None,
            /// <summary>
            /// Operandy jsou povinné, a pokud nějaký nebude možno konvertovat, pak vrátí null
            /// </summary>
            StrictlyAllItems,
            /// <summary>
            /// Operandy jsou nepovinné, a pokud nějaký nebude možno konvertovat, pak vrátí null
            /// </summary>
            RemoveEmptyItems
        }
        #endregion
    }
    #endregion
    #region class DxToExpressionPart : Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// <summary>
    /// Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// </summary>
    internal class DxToExpressionPart
    {
        #region Public members : Create + Add
        /// <summary>
        /// Vytvoří prvek typu Container
        /// </summary>
        /// <returns></returns>
        internal static DxToExpressionPart CreateContainer()
        {
            var part = new DxToExpressionPart(PartType.Container);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateText(string text)
        {
            var part = new DxToExpressionPart(PartType.Text);
            part.__Text = text;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Property = sloupec databáze
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateProperty(string propertyName)
        {
            var part = new DxToExpressionPart(PartType.PropertyName);
            part.__PropertyName = propertyName;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Value = hodnota
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateValue(object value)
        {
            var part = new DxToExpressionPart(PartType.Value);
            part.__Value = value;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateFrom(params object[] parts)
        {
            var part = new DxToExpressionPart(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateFrom(IEnumerable<object> parts)
        {
            var part = new DxToExpressionPart(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty, kde mezi každý nenulový objekt vloží text s daným oddělovačem
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxToExpressionPart CreateDelimited(string delimiter, IEnumerable<object> parts)
        {
            var part = new DxToExpressionPart(PartType.Container);
            part._AddRange(parts, delimiter);
            return part;
        }
        /// <summary>
        /// Do aktuálního prvku přidá další dodané prvky
        /// </summary>
        /// <param name="parts"></param>
        internal void AddRange(IEnumerable<object> parts)
        {
            this._AddRange(parts);
        }
        /// <summary>
        /// Do aktuálního prvku přidá další dodané prvky
        /// </summary>
        /// <param name="parts"></param>
        internal void AddRange(params object[] parts)
        {
            this._AddRange(parts);
        }
        #endregion
        #region Public members : Results
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToText(DxToExpressionLanguageType.MsSqlDatabase);
        }
        /// <summary>
        /// Konvertuje obsah this prvku (podle jeho typu, tedy i včetně subprvků v Containeru) do textu v daném jazyce. Jazyk ovlivní formátování názvů sloupců a hodnot.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        internal string ToText(DxToExpressionLanguageType language)
        {
            StringBuilder sb = new StringBuilder();
            this._AddText(sb, language);
            return sb.ToString();
        }
        /// <summary>
        /// Přidá obsah this prvku do dodaného <see cref="StringBuilder"/>, v zadaném jazyce <paramref name="language"/>.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="language"></param>
        private void _AddText(StringBuilder sb, DxToExpressionLanguageType language)
        {
            switch (this.__PartType)
            {
                case PartType.Text:
                    sb.Append(this.__Text);
                    break;
                case PartType.PropertyName:
                    sb.Append(DxToExpressionConvertor.FormatPropertyName(this.__PropertyName, language));
                    break;
                case PartType.Value:
                    sb.Append(DxToExpressionConvertor.FormatValue(this.__Value, language));
                    break;
                case PartType.Container:
                    foreach (var item in __Items)
                        item._AddText(sb, language);
                    break;
            }
        }
        /// <summary>
        /// Přičte navazující text do this <see cref="__Text"/>
        /// </summary>
        /// <param name="addText"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void _MergeText(string addText)
        {
            if (this.IsText)
                this.__Text += addText;
            else
                throw new InvalidOperationException($"DxToExpressionPart can not Merge text into part of type '{this.__PartType}'.");
        }
        #endregion
        #region Private members : Konstruktor, proměné, přidávání, slučování, mergování...
        private DxToExpressionPart(PartType partType)
        {
            __PartType = partType;
            if (partType == PartType.Container)
                __Items = new List<DxToExpressionPart>();
        }
        private PartType __PartType;
        private List<DxToExpressionPart> __Items;
        private string __Text;
        private string __PropertyName;
        private object __Value;
        /// <summary>
        /// Do this prvku, který je/bude Container, přidá další prvky, volitelně s delimiterem
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="delimiter"></param>
        private void _AddRange(IEnumerable<object> parts, string delimiter = null)
        {
            // Pokud nejsou vstupy, nic nedělá:
            if (parts is null) return;
            var items = parts.Where(p => !(p is null)).ToArray();
            if (items.Length == 0) return;

            // Pokud máme delimiter, pak vytvoříme nové pole, kam bude delimiter vložen mezi prvky (které už nyní jsou pouze not-null):
            if (!String.IsNullOrEmpty(delimiter))
                items = _CreateDelimitedArray(items, delimiter);

            // Pokud this není Container (tedy jde o Text, nebo Property nebo Value), pak se konvertujeme na Container:
            if (!IsContainer)
                this._SwitchToContainer();

            // Nyní vezmu moje pole prvků, a budu do něj přidávat dodané prvky.
            // Pokud vstupní prvek bude string, nebo prvek typu Text, a současně poslední prvek mého pole bude Text, pak nový text připojím na konec stávajícího.
            _AddItemsTo(this.__Items, items);
        }
        private static void _AddItemsTo(List<DxToExpressionPart> targetItems, object[] addItems)
        {
            for (int i = 0; i < addItems.Length; i++)
            {
                // Na vstupu může být cokoliv - string, DxToExpressionPart (různého typu) i cokoliv jiného, což budeme chápat jako String. Jen null budeme přeskakovat.
                var addItem = addItems[i];
                if (addItem is null) continue;

                var targetCount = targetItems.Count;
                var targetLastItem = (targetCount > 0 ? targetItems[targetCount - 1] : null);
                bool canMergeText = (targetCount > 0 && targetLastItem.IsText);
                if (addItem is DxToExpressionPart addPart)
                {   // Přidáváme DxToExpressionPart:
                    // Pak postupujeme podle toho, co přidávaný prvek reálně obsahuje:
                    switch (addPart.__PartType)
                    {
                        case PartType.Text:
                            if (canMergeText)
                                targetLastItem._MergeText(addPart.__Text);
                            else
                                targetItems.Add(addPart);
                            break;
                        case PartType.PropertyName:
                        case PartType.Value:
                            targetItems.Add(addPart);
                            break;
                        case PartType.Container:
                            _AddItemsTo(targetItems, addPart.__Items.ToArray());
                            break;
                    }
                }
                else
                {   // Přidávám cokoliv jiného => přidám to jako Text:
                    string addText = addItem.ToString();
                    if (canMergeText)
                        targetLastItem._MergeText(addText);
                    else
                        targetItems.Add(CreateText(addText));
                }
            }
        }
        /// <summary>
        /// Vrátí new pole, kde mezi prvky vstupního pole <paramref name="items"/> budou vloženy texty delimiteru <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        private object[] _CreateDelimitedArray(object[] items, string delimiter)
        {
            var result = new List<object>();
            foreach (var item in items)
            {
                if (item is null) continue;                // Vstupní prvky null přeskočím
                if (result.Count > 0)
                    result.Add(delimiter);                 // Delimiter přidám až za první prvek, nikoli před první
                result.Add(item);
            }
            return result.ToArray();
        }
        /// <summary>
        /// Konvertuje this objekt na Container, který do sebe může pojmout sadu jiných částic.
        /// Z this dat typu Simple vytvoří klon, a ze sebe vytvoří Container; a ten klon původních dat vloží jako první prvek do this nového Containeru
        /// </summary>
        private void _SwitchToContainer()
        {
            var isSimple = IsSimple;
            var isContainer = IsContainer;

            // Pokud this obsahuje jednoduchou hodnotu, pak jí zde zkopíruji do new objektu:
            DxToExpressionPart simple = (isSimple ? this.MemberwiseClone() as DxToExpressionPart : null);

            // Pokud this dosud NENÍ Container, pak this změním tak, aby byl Container:
            if (!isContainer)
            {
                this.__PartType = PartType.Container;
                this.__Items = new List<DxToExpressionPart>();
                this.__Text = null;
                this.__PropertyName = null;
                this.__Value = null;
            }

            // Pokud this původně byl Simple, pak nyní do this Containeru vložím data z původního Simple objektu:
            if (isSimple)
                this.__Items.Add(simple);
        }
        #endregion
        #region Public informace
        public bool IsText { get { return (this.__PartType == PartType.Text); } }
        public bool IsPropertyName { get { return (this.__PartType == PartType.PropertyName); } }
        public bool IsValue { get { return (this.__PartType == PartType.Value); } }
        public bool IsValueString { get { return (this.__PartType == PartType.Value && this.__Value is string); } }
        public bool IsValueInt32 { get { return (this.__PartType == PartType.Value && this.__Value is int); } }
        public bool IsSimple { get { return (this.__PartType == PartType.Text || this.__PartType == PartType.PropertyName || this.__PartType == PartType.Value); } }
        public bool IsContainer { get { return (this.__PartType == PartType.Container); } }
        public string Text { get { return (IsText ? __Text : null); } }
        public string PropertyName { get { return (IsPropertyName ? __PropertyName : null); } }
        public object Value { get { return (IsValue ? __Value : null); } }
        public string ValueString { get { return (IsValueString ? __Value as string : null); } }
        public int ValueInt32 { get { return (IsValueInt32 ? (int)__Value : 0); } }
        public DxToExpressionPart[] Items { get { return (IsContainer ? __Items.ToArray() : null); } }
        /// <summary>
        /// Druh částice
        /// </summary>
        public enum PartType
        {
            /// <summary>
            /// Container: obsahuje další prvky, viz <see cref="DxToExpressionPart.Items"/>
            /// </summary>
            Container,
            /// <summary>
            /// Prostý text: klíčová slova, oddělovače, závorky, atd, viz <see cref="DxToExpressionPart.Text"/>
            /// </summary>
            Text,
            /// <summary>
            /// Název datového sloupce / property, viz <see cref="DxToExpressionPart.PropertyName"/>
            /// </summary>
            PropertyName,
            /// <summary>
            /// Hodnota: obsahuje zadanou hodnotu / konstantu / proměnnou / parametr, viz <see cref="DxToExpressionPart.Value"/>
            /// </summary>
            Value
        }
        #endregion
    }
    #endregion
    #region Enumy a další
    /// <summary>
    /// Cílový jazyk konverze
    /// </summary>
    internal enum DxToExpressionLanguageType
    {
        Default,
        MsSqlDatabase,
        SystemDataFilter
    }
    #endregion
}
