using DevExpress.Data.Filtering;
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
        public static string ConvertToString(string dxExpression, DxExpressionLanguageType language, DxExpressionCustomHandler customHandler = null)
        {
            if (String.IsNullOrEmpty(dxExpression)) return null;
            var part = _ConvertToPart(DxFilter.CriteriaOperator.Parse(dxExpression), language, customHandler);
            return part?.ToText(language);
        }

        public static string ConvertToString(DxFilter.CriteriaOperator filter, DxExpressionLanguageType language, DxExpressionCustomHandler customHandler = null)
        {
            if (filter is null) return null;
            var part = _ConvertToPart(filter, language, customHandler);
            return part?.ToText(language);
        }
        public static DxExpressionPart ConvertToPart(string dxExpression, DxExpressionLanguageType language, DxExpressionCustomHandler customHandler = null)
        {
            if (String.IsNullOrEmpty(dxExpression)) return null;
            var part = _ConvertToPart(DxFilter.CriteriaOperator.Parse(dxExpression), language, customHandler);
            return part;
        }
        public static DxExpressionPart ConvertToPart(DxFilter.CriteriaOperator filter, DxExpressionLanguageType language, DxExpressionCustomHandler customHandler = null)
        {
            if (filter is null) return null;
            var part = _ConvertToPart(filter, language, customHandler);
            return part;
        }

        private static DxExpressionPart _ConvertToPart(CriteriaOperator filter, DxExpressionLanguageType language, DxExpressionCustomHandler customHandler)
        {
            if (filter is null) return null;

            var visitor = new DxExpressionVisitor(language, customHandler);
            var result = filter.Accept(visitor);
            return result;
        }

        internal static string FormatPropertyName(string propertyName, DxExpressionLanguageType language)
        {
            return $"[{propertyName}]";
        }
        internal static string FormatValue(object value, DxExpressionLanguageType language)
        {
            return $"'{value}'";
        }
        #endregion
    }
    #endregion
    #region class DxExpressionVisitor : Rekurzivní konverzní třída pro vlastní převod DxFilter.CriteriaOperator do výsledných částí DxExpressionPart
    /// <summary>
    /// <see cref="DxExpressionVisitor"/> : Rekurzivní konverzní třída pro vlastní převod <see cref="DxFilter.CriteriaOperator"/> do výsledných částí <see cref="DxExpressionPart"/>
    /// </summary>
    internal class DxExpressionVisitor : DxFilter.ICriteriaVisitor<DxExpressionPart>, DxFilter.IClientCriteriaVisitor<DxExpressionPart>
    {
        #region Konstruktor a jazyk
        internal DxExpressionVisitor(DxExpressionLanguageType language, DxExpressionCustomHandler customHandler = null)
        {
            __Language = language;
            __CustomHandler = customHandler;
            __HasCustomHandler = (customHandler != null);
        }
        private DxExpressionLanguageType __Language;
        private DxExpressionCustomHandler __CustomHandler;
        private bool __HasCustomHandler;
        #endregion
        #region Visitors
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.GroupOperator groupOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.RemoveEmptyItems, groupOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Group, groupOperator.OperatorType);
            return ConvertToPart(groupOperator, operation, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.BetweenOperator betweenOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, betweenOperator.TestExpression, betweenOperator.BeginExpression, betweenOperator.EndExpression);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            return ConvertToPart(betweenOperator, DxExpressionOperationType.Between, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.BinaryOperator binaryOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, binaryOperator.LeftOperand, binaryOperator.RightOperand);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Binary, binaryOperator.OperatorType);
            return ConvertToPart(binaryOperator, operation, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.UnaryOperator unaryOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, unaryOperator.Operand);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Unary, unaryOperator.OperatorType);
            return ConvertToPart(unaryOperator, operation, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.InOperator inOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.RemoveEmptyItems, inOperator.LeftOperand, inOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            return ConvertToPart(inOperator, DxExpressionOperationType.In, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.FunctionOperator functionOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, functionOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            // Která funkce to má být?
            var operation = DxExpressionOperationType.None;
            
            if (functionOperator.OperatorType == DxFilter.FunctionOperatorType.Custom && dxOperands.Count > 0 && dxOperands[0].IsValueString)
            {   // Funkce může být i Custom, pak její vlastní název je přítomen v prvním operandu typu String:
                operation = ConvertOperation(FamilyType.Custom, dxOperands[0].ValueString);
            }
            else
            {   // Standardní funkce z rodiny DevExpress:
                operation = ConvertOperation(FamilyType.Function, functionOperator.OperatorType);
            }

            return ConvertToPart(functionOperator, operation, dxOperands);
        }
        DxExpressionPart DxFilter.ICriteriaVisitor<DxExpressionPart>.Visit(DxFilter.OperandValue operandValue)
        {
            return DxExpressionPart.CreateValue(operandValue.Value);
        }
        DxExpressionPart DxFilter.IClientCriteriaVisitor<DxExpressionPart>.Visit(DxFilter.OperandProperty operandProperty)
        {
            return DxExpressionPart.CreateProperty(operandProperty.PropertyName);
        }
        DxExpressionPart DxFilter.IClientCriteriaVisitor<DxExpressionPart>.Visit(DxFilter.AggregateOperand aggregateOperand)
        {
            return DxExpressionPart.CreateText("aggregateoperand ");
        }
        DxExpressionPart DxFilter.IClientCriteriaVisitor<DxExpressionPart>.Visit(DxFilter.JoinOperand joinOperand)
        {
            return DxExpressionPart.CreateText("joinoperand ");
        }
        #endregion
        #region Vlastní konvertor všech operací (funkce, operace, porovnání...), s využitím __CustomHandler
        private DxExpressionPart ConvertToPart(DxFilter.CriteriaOperator filterOperator, DxExpressionOperationType operation, List<DxExpressionPart> operands)
        {
            if (__HasCustomHandler)
            {   // Custom handler:
                var args = new DxExpressionCustomArgs(__Language, operation, operands);
                __CustomHandler(filterOperator, args);
                if (args.Skip) return null;
                if (args.CustomResult != null) return args.CustomResult;
                operation = args.Operation;
                operands = args.Operands;
            }
            int count = operands?.Count ?? -1;
            switch (operation)
            {
                #region Group: And, Or
                case DxExpressionOperationType.Group_And:
                case DxExpressionOperationType.Group_Or:
                    // Žádný prvek: vynecháme;
                    // Jediný prvek: nepotřebuje závorky ani delimiter:
                    if (count <= 0) return null;
                    if (count == 1) return operands[0];

                    // Výsledek: pokud je v něm více než 1 prvek, pak jej ozávorkujeme :              (a < 1 or b > 10)   
                    //   aby navazující vyšší grupa (např. AND) byla napojena validně  :   c = 10 and (a < 1 or b > 10)
                    //   kdežto bez závorek by to dopadlo špatně                       :   c = 10 and  a < 1 or b > 10
                    //   ....  protože AND mívá přednost, takže význam by byl          :  (c = 10 and a < 1) or b > 10 
                    string delimiter = (operation == DxExpressionOperationType.Group_And ? " and " : (operation == DxExpressionOperationType.Group_Or ? " or " : ", "));
                    return DxExpressionPart.CreateFrom("(", DxExpressionPart.CreateDelimited(delimiter, operands), ")");

                #endregion
                #region Between
                case DxExpressionOperationType.Between:
                    //    Operand.0  between  Operand.1  and  Operand.2
                    checkCount(3);
                    return DxExpressionPart.CreateFrom(operands[0], " between ", operands[1], " and ", operands[2]);
                #endregion
                #region Binary: Equal, Greater, Less, Modulo, Multiply...
                case DxExpressionOperationType.Binary_Equal:
                case DxExpressionOperationType.Binary_NotEqual:
                case DxExpressionOperationType.Binary_Greater:
                case DxExpressionOperationType.Binary_Less:
                case DxExpressionOperationType.Binary_LessOrEqual:
                case DxExpressionOperationType.Binary_GreaterOrEqual:
                case DxExpressionOperationType.Binary_Like:
                case DxExpressionOperationType.Binary_BitwiseAnd:
                case DxExpressionOperationType.Binary_BitwiseOr:
                case DxExpressionOperationType.Binary_BitwiseXor:
                case DxExpressionOperationType.Binary_Divide:
                case DxExpressionOperationType.Binary_Modulo:
                case DxExpressionOperationType.Binary_Multiply:
                case DxExpressionOperationType.Binary_Plus:
                case DxExpressionOperationType.Binary_Minus:
                    checkCount(2);
                    return DxExpressionPart.CreateFrom(operands[0], getBinaryOperatorText(operation), operands[1]);
                #endregion
                #region Unary: Not, IsNull, ...
                case DxExpressionOperationType.Unary_BitwiseNot:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom(" ~", operands[0]);
                case DxExpressionOperationType.Unary_Plus:
                    checkCount(1);
                    return operands[0];
                case DxExpressionOperationType.Unary_Minus:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom(" -(", operands[0], ")");
                case DxExpressionOperationType.Unary_Not:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("not (", operands[0], ")");
                case DxExpressionOperationType.Unary_IsNull:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom(operands[0], " is null");

                #endregion
                #region In list:
                case DxExpressionOperationType.In:
                    if (count < 2) return null;                                          // null reprezentuje stav, kdy daný fragment vynecháváme.
                    if (count == 2) return DxExpressionPart.CreateFrom(operands[0], " = ", operands[1]);               // Sloupec in (123)   převedeme na   Sloupec = 123
                    if (count > 2) return DxExpressionPart.CreateFrom(operands[0], " in (", DxExpressionPart.CreateDelimited(",", operands.Skip(1)), ")");   // Sloupec in (operandy počínaje [1] oddělené , delimiterem)
                    failCount("1 or more");
                    break;

                #endregion
                #region Function - Custom:
                case DxExpressionOperationType.Function_None:
                case DxExpressionOperationType.Function_Custom:
                case DxExpressionOperationType.Function_CustomNonDeterministic:
                #endregion
                #region Function - Logical: Iif, IsNull
                case DxExpressionOperationType.Function_Iif:
                    // Returns one of the specified values depending upon the values of logical expressions. The function can take 2N+1 arguments (where N is the number of specified logical expressions):
                    // Each odd argument specifies a logical expression.
                    // Each even argument specifies the value that is returned if the previous expression evaluates to True.
                    // The last argument specifies the value that is returned if the previously evaluated logical expressions yield False.
                    // If you pass only one argument, the passed argument is returned.
                    // If you compare a 0(zero) with a Null value, the expression evaluates to True.
                    // Examples: Iif(Name = 'Bob', 1, 0)
                    //           Iif(Name = 'Bob', 1, Name = 'Dan', 2, Name = 'Sam', 3, 0)
                    if (count == 0) failCount("1 or more");                                        // Bez argumentů = nevalidní
                    if ((count % 2) == 0) failCount("1, 3, 5, 7 .. (=odd count)");                 // Musí jich být lichý počet
                    if (count == 1) return operands[0];                                            // If you pass only one argument, the passed argument is returned.
                    var iifPart = DxExpressionPart.CreateFrom("(case ");                           // (case when ... 
                    for (int i = 0; i < count; i += 2)
                        iifPart.AddRange("when ", operands[i], " then ", operands[i + 1], " ");    //   ... [Name] = 'Bob' then 1 ...       přičemž operandItems[i] je logický: "[Name] = 'Bob'" a operandItems[i + 1] je hodnota: "1"
                    iifPart.AddRange("else ", operands[count - 1], " end)");                       //   ... else 0 end)
                    return iifPart;
                case DxExpressionOperationType.Function_IsNull:
                    // Compares the first operand with the NULL value. This function requires one or two operands of the CriteriaOperator class. 
                    // The returned value depends on the number of arguments (one or two arguments).
                    //  True / False: If a single operand is passed, the function returns True if the operand is null; otherwise, False.
                    //  Value1 / Value2: If two operands are passed, the function returns the first operand if it is not set to NULL; otherwise, the second operand is returned.
                    if (count == 1) return DxExpressionPart.CreateFrom(operands[0], " is null");                                 // [datum_akce] is null
                    if (count == 2) return DxExpressionPart.CreateFrom("isnull(", operands[0], ", ", operands[1], ")");          // isnull([datum_akce], [datum_podani])
                    failCount("1 or 2");
                    break;
                case DxExpressionOperationType.Function_IsNullOrEmpty:
                    // Returns True if the specified value is null or an empty string. Otherwise, returns False.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("(", operands[0], " is null or len(trim(", operands[0], ")) = 0)");
                #endregion
                #region Function - String 1: Trim. Len, Substring, Upper, Lower, Concat, ...
                case DxExpressionOperationType.Function_Trim:
                    //Returns a string that is a copy of the specified string with all white-space characters removed from the start and end of the specified string.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("trim(", operands[0], ")");
                case DxExpressionOperationType.Function_Len:
                    // Returns the length of the string specified by an operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("len(", operands[0], ")");
                case DxExpressionOperationType.Function_Substring:
                    // Returns a substring from the specified string. This function requires two or three operands.
                    // If two operands are passed, the substring starts from the beginning of the specified string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    // If three operands are passed, a substring starts from the specified position in the source string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    //   3 - an integer that specifies the length of the substring.
                    if (count == 2)
                    {
                        if (operands[1].IsValueInt32)
                            return DxExpressionPart.CreateFrom("substring(", operands[0], ",", intAsText(operands[1], 1), ",9999)");       // substring([poznamka], 41, 9999) : DevExpress umožňuje 2 argumenty (string, begin), ale SQL server chce povinně 3, kde třetí = délka
                        else
                            return DxExpressionPart.CreateFrom("substring(", operands[0], ",(1+", operands[1], "),9999)");                 // substring([poznamka], (1+40), 9999) : DevExpress umožňuje 2 argumenty (string, begin), ale SQL server chce povinně 3, kde třetí = délka
                    }
                    if (count == 3)
                    {
                        if (operands[1].IsValueInt32)
                            return DxExpressionPart.CreateFrom("substring(", operands[0], ",", intAsText(operands[1], 1), ",", intAsText(operands[2]), ")");   // substring([poznamka], (1+40), 25)   : DevExpress má počátek substringu zero-based ("integer that specifies the zero-based position at which the substring starts."), ale SQL server má base 1
                        else
                            return DxExpressionPart.CreateFrom("substring(", operands[0], ",(1+", operands[1], "),", intAsText(operands[2]), ")");             // substring([poznamka], (1+40), 25)   : DevExpress má počátek substringu zero-based ("integer that specifies the zero-based position at which the substring starts."), ale SQL server má base 1
                    }
                    failCount("2 or 3");
                    break;
                case DxExpressionOperationType.Function_Upper:
                    // Converts all characters in a string operand to uppercase in an invariant culture.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("upper(", operands[0], ")");
                case DxExpressionOperationType.Function_Lower:
                    // Converts all characters in a string operand to lowercase in an invariant culture.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("lower(", operands[0], ")");
                case DxExpressionOperationType.Function_Concat:
                    // Concatenates the specified strings.
                    // SQL server pro funkci CONCAT vyžaduje nejméně dva parametry; proto pro méně operandů provádím konverze jinak:
                    if (count <= 0) return DxExpressionPart.CreateText("''");                                                    // concat()                            vrátí ''
                    if (count == 1) return operands[0];                                                                          // concat([nazev])                     vrátí [nazev]
                    return DxExpressionPart.CreateFrom("concat(", DxExpressionPart.CreateDelimited(",", operands), ")");         // concat([nazev1], ',', [nazev2])     vrátí concat([nazev1], ',', [nazev2])   = to je SQL validní
                case DxExpressionOperationType.Function_Ascii:
                    // Returns the ASCII code of the first character in a string operand.
                    // If the argument is an empty string, the null value is returned.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("ascii(", operands[0], ")");
                case DxExpressionOperationType.Function_Char:
                    // Converts a numeric operand to a Unicode character.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("char(", operands[0], ")");
                case DxExpressionOperationType.Function_ToStr:
                case DxExpressionOperationType.Function_Replace:
                case DxExpressionOperationType.Function_Reverse:
                case DxExpressionOperationType.Function_Insert:
                case DxExpressionOperationType.Function_CharIndex:
                case DxExpressionOperationType.Function_Remove:
                    break;
                #endregion
                #region Function - Mathematics: Abs, Sqrt, Sin, Exp, Log, Pow, Celinig, Round, ...
                case DxExpressionOperationType.Function_Abs:
                    // Returns the absolute value of a numeric operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("abs(", operands[0], ")");
                case DxExpressionOperationType.Function_Sqr:
                    // Returns the square root of a specified numeric operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("sqrt(", operands[0], ")");
                case DxExpressionOperationType.Function_Cos:
                    // Returns the cosine of the numeric operand, in radians.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cos(", operands[0], ")");
                case DxExpressionOperationType.Function_Sin:
                    // Returns the sine of the numeric operand, in radians.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("sin(", operands[0], ")");
                case DxExpressionOperationType.Function_Atn:
                    // Returns the arctangent (the inverse tangent function) of the numeric operand. The arctangent is the angle in the range -π/2 to π/2 radians, whose tangent is the numeric operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("atan(", operands[0], ")");
                case DxExpressionOperationType.Function_Exp:
                    // Returns the number e raised to the power specified by a numeric operand.
                    //   If the specified operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Exp function reverses the FunctionOperatorType.Log function. Use the FunctionOperatorType.Power operand to calculate powers of other bases.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("exp(", operands[0], ")");
                case DxExpressionOperationType.Function_Log:
                    // Returns the logarithm of the specified numeric operand. The return value depends upon the number of operands.
                    // If one operand is passed, the function returns the natural(base e) logarithm of a specified operand.
                    // If two operands are passed, the function returns the logarithm of the specified operand to the specified base.The operands are:
                    //   1 - a number whose logarithm is to be calculated.
                    //   2 - the base of the logarithm.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Log function reverses the FunctionOperatorType.Exp function. To calculate the base - 10 logarithm, use the FunctionOperatorType.Log10 function.
                    if (count == 1) return DxExpressionPart.CreateFrom("log(", operands[0], ")");
                    if (count == 2) return DxExpressionPart.CreateFrom("log(", operands[0], ",", operands[1], ")");
                    failCount("1 or 2");
                    break;
                case DxExpressionOperationType.Function_Rnd:
                    // Returns a random number greater than or equal to 0.0, and less than 1.0.
                    return DxExpressionPart.CreateText("rand()");
                case DxExpressionOperationType.Function_Tan:
                    // Returns the tangent of the specified numeric operand that is an angle in radians.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("tan(", operands[0], ")");
                case DxExpressionOperationType.Function_Power:
                    // Returns a specified numeric operand raised to a specified power.
                    // The operands are:
                    //  1 - the base number.
                    //  2 - the exponent to which the base number is raised.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Power function reverses the FunctionOperatorType.Log or FunctionOperatorType.Log10 function. Use the FunctionOperatorType.Exp operand to calculate powers of the number e.
                    checkCount(2);
                    return DxExpressionPart.CreateFrom("power(", operands[0], ",", operands[1], ")");
                case DxExpressionOperationType.Function_Sign:
                    // Returns an integer that indicates the sign of a number. The function returns 1 for positive numbers, -1 for negative numbers, and 0 (zero) if a number is equal to zero.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("sign(", operands[0], ")");
                case DxExpressionOperationType.Function_Round:
                    // Rounds a specified numeric operand to the nearest integer or to a specified number of fractional digits.
                    // The operands are:
                    // 1 - a value to round.
                    // 2 - (optional)the number of decimal places to which to round. 0 indicates that the first operand is rounded to the nearest integer.
                    if (count == 1) return DxExpressionPart.CreateFrom("round(", operands[0], ", 0)");
                    if (count == 2) return DxExpressionPart.CreateFrom("round(", operands[0], ",", intAsText(operands[1]), ")");
                    failCount("1 or 2");
                    break;
                case DxExpressionOperationType.Function_Ceiling:
                    // Returns the smallest integral value greater than or equal to the specified numeric operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("ceiling(", operands[0], ")");
                case DxExpressionOperationType.Function_Floor:
                    // Returns the largest integral value less than or equal to the specified numeric operand.
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("floor(", operands[0], ")");
                case DxExpressionOperationType.Function_Max:
                    // Returns the larger of two numeric values.
                    checkCount(2);
                    return DxExpressionPart.CreateFrom("(case when ", operands[0], " > ", operands[1], " then ", operands[0], " else ", operands[1], " end)");   // (case when pocet1 > pocet2 then pocet1 else pocet2 end)
                case DxExpressionOperationType.Function_Min:
                    // Returns the smaller of two numeric values.
                    checkCount(2);
                    return DxExpressionPart.CreateFrom("(case when ", operands[0], " < ", operands[1], " then ", operands[0], " else ", operands[1], " end)");   // (case when pocet1 < pocet2 then pocet1 else pocet2 end)
                case DxExpressionOperationType.Function_Acos:
                case DxExpressionOperationType.Function_Asin:
                case DxExpressionOperationType.Function_Atn2:
                case DxExpressionOperationType.Function_BigMul:
                case DxExpressionOperationType.Function_Cosh:
                case DxExpressionOperationType.Function_Log10:
                case DxExpressionOperationType.Function_Sinh:
                case DxExpressionOperationType.Function_Tanh:
                    break;
                #endregion
                #region Function - String 2: PadLeft, StartsWith, Contains, ToInt, ToDecimal, ...
                case DxExpressionOperationType.Function_PadLeft:
                    break;
                case DxExpressionOperationType.Function_PadRight:
                    break;
                case DxExpressionOperationType.Function_StartsWith:
                    checkCount(2);
                    if (operands[1].IsValueString)
                        // Pokud hodnota je zadána jako konstanta typu Text (=nejčastější situace), 
                        //  pak do výstupu dáme novou hodnotu typu String s upraveným vstupním obsahem:
                        return DxExpressionPart.CreateFrom(operands[0], " like ", DxExpressionPart.CreateValue(operands[1].ValueString + "%"));      // [nazev] like 'adr%'
                    else
                        // Pokud hodnota není stringová konstanta, pak do výsledku musíme dát vzorec: '%' + ... + '%' :
                        return DxExpressionPart.CreateFrom(operands[0], " like (", operands[1], " + '%')");                                          // [nazev] like (N'adr' + '%')
                case DxExpressionOperationType.Function_EndsWith:
                    checkCount(2);
                    if (operands[1].IsValueString)
                        // Pokud hodnota je zadána jako konstanta typu Text (=nejčastější situace), 
                        //  pak do výstupu dáme novou hodnotu typu String s upraveným vstupním obsahem:
                        return DxExpressionPart.CreateFrom(operands[0], " like ", DxExpressionPart.CreateValue("%" + operands[1].ValueString));      // [nazev] like '%adr'
                    else
                        // Pokud hodnota není stringová konstanta, pak do výsledku musíme dát vzorec: '%' + ... + '%' :
                        return DxExpressionPart.CreateFrom(operands[0], " like ('%' + ", operands[1], ")");                                          // [nazev] like ('%' + N'adr')
                case DxExpressionOperationType.Function_Contains:
                    checkCount(2);
                    if (operands[1].IsValueString)
                        // Pokud hodnota je zadána jako konstanta typu Text (=nejčastější situace), 
                        //  pak do výstupu dáme novou hodnotu typu String s upraveným vstupním obsahem:
                        return DxExpressionPart.CreateFrom(operands[0], " like ", DxExpressionPart.CreateValue("%" + operands[1].ValueString + "%"));      // [nazev] like '%adr%'
                    else
                        // Pokud hodnota není stringová konstanta, pak do výsledku musíme dát vzorec: '%' + ... + '%' :
                        return DxExpressionPart.CreateFrom(operands[0], " like ('%' + ", operands[1], " + '%')");                                          // [nazev] like ('%' + N'adr' + '%')
                case DxExpressionOperationType.Function_ToInt:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cast(", operands[0], " as int)");
                case DxExpressionOperationType.Function_ToLong:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cast(", operands[0], " as bigint)");
                case DxExpressionOperationType.Function_ToFloat:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                case DxExpressionOperationType.Function_ToDouble:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                case DxExpressionOperationType.Function_ToDecimal:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                #endregion
                #region Function - DateTime 1: LocalDateTime
                case DxExpressionOperationType.Function_LocalDateTimeThisYear:
                case DxExpressionOperationType.Function_LocalDateTimeThisMonth:
                case DxExpressionOperationType.Function_LocalDateTimeLastWeek:
                case DxExpressionOperationType.Function_LocalDateTimeThisWeek:
                case DxExpressionOperationType.Function_LocalDateTimeYesterday:
                case DxExpressionOperationType.Function_LocalDateTimeToday:
                case DxExpressionOperationType.Function_LocalDateTimeNow:
                case DxExpressionOperationType.Function_LocalDateTimeTomorrow:
                case DxExpressionOperationType.Function_LocalDateTimeDayAfterTomorrow:
                case DxExpressionOperationType.Function_LocalDateTimeNextWeek:
                case DxExpressionOperationType.Function_LocalDateTimeTwoWeeksAway:
                case DxExpressionOperationType.Function_LocalDateTimeNextMonth:
                case DxExpressionOperationType.Function_LocalDateTimeNextYear:
                case DxExpressionOperationType.Function_LocalDateTimeTwoMonthsAway:
                case DxExpressionOperationType.Function_LocalDateTimeTwoYearsAway:
                case DxExpressionOperationType.Function_LocalDateTimeLastMonth:
                case DxExpressionOperationType.Function_LocalDateTimeLastYear:
                case DxExpressionOperationType.Function_LocalDateTimeYearBeforeToday:
                    break;
                #endregion
                #region Function - DateTime 2: IsOutlookInterval
                case DxExpressionOperationType.Function_IsOutlookIntervalBeyondThisYear:
                case DxExpressionOperationType.Function_IsOutlookIntervalLaterThisYear:
                case DxExpressionOperationType.Function_IsOutlookIntervalLaterThisMonth:
                case DxExpressionOperationType.Function_IsOutlookIntervalNextWeek:
                case DxExpressionOperationType.Function_IsOutlookIntervalLaterThisWeek:
                case DxExpressionOperationType.Function_IsOutlookIntervalTomorrow:
                case DxExpressionOperationType.Function_IsOutlookIntervalToday:
                case DxExpressionOperationType.Function_IsOutlookIntervalYesterday:
                case DxExpressionOperationType.Function_IsOutlookIntervalEarlierThisWeek:
                case DxExpressionOperationType.Function_IsOutlookIntervalLastWeek:
                case DxExpressionOperationType.Function_IsOutlookIntervalEarlierThisMonth:
                case DxExpressionOperationType.Function_IsOutlookIntervalEarlierThisYear:
                case DxExpressionOperationType.Function_IsOutlookIntervalPriorThisYear:
                    break;
                #endregion
                #region Function - DateTime 3: Is... (IsThisWeek, IsLastYear, IsJanuary, ...)
                case DxExpressionOperationType.Function_IsThisWeek:
                case DxExpressionOperationType.Function_IsThisMonth:
                case DxExpressionOperationType.Function_IsThisYear:
                case DxExpressionOperationType.Function_IsNextMonth:
                case DxExpressionOperationType.Function_IsNextYear:
                case DxExpressionOperationType.Function_IsLastMonth:
                case DxExpressionOperationType.Function_IsLastYear:
                case DxExpressionOperationType.Function_IsYearToDate:
                case DxExpressionOperationType.Function_IsSameDay:
                case DxExpressionOperationType.Function_InRange:
                case DxExpressionOperationType.Function_InDateRange:
                    break;
                case DxExpressionOperationType.Function_IsJanuary:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 1");
                case DxExpressionOperationType.Function_IsFebruary:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 2");
                case DxExpressionOperationType.Function_IsMarch:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 3");
                case DxExpressionOperationType.Function_IsApril:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 4");
                case DxExpressionOperationType.Function_IsMay:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 5");
                case DxExpressionOperationType.Function_IsJune:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 6");
                case DxExpressionOperationType.Function_IsJuly:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 7");
                case DxExpressionOperationType.Function_IsAugust:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 8");
                case DxExpressionOperationType.Function_IsSeptember:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 9");
                case DxExpressionOperationType.Function_IsOctober:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 10");
                case DxExpressionOperationType.Function_IsNovember:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 11");
                case DxExpressionOperationType.Function_IsDecember:
                    checkCount(1);
                    return DxExpressionPart.CreateFrom("month(", operands[0], ") = 12");
                #endregion
                #region Function - DateTime 4: DateDiff...
                case DxExpressionOperationType.Function_DateDiffTick:
                case DxExpressionOperationType.Function_DateDiffSecond:
                case DxExpressionOperationType.Function_DateDiffMilliSecond:
                case DxExpressionOperationType.Function_DateDiffMinute:
                case DxExpressionOperationType.Function_DateDiffHour:
                case DxExpressionOperationType.Function_DateDiffDay:
                case DxExpressionOperationType.Function_DateDiffMonth:
                case DxExpressionOperationType.Function_DateDiffYear:
                    break;
                #endregion
                #region Function - DateTime 5: GetPart...
                case DxExpressionOperationType.Function_GetDate:
                case DxExpressionOperationType.Function_GetMilliSecond:
                case DxExpressionOperationType.Function_GetSecond:
                case DxExpressionOperationType.Function_GetMinute:
                case DxExpressionOperationType.Function_GetHour:
                case DxExpressionOperationType.Function_GetDay:
                case DxExpressionOperationType.Function_GetMonth:
                case DxExpressionOperationType.Function_GetYear:
                case DxExpressionOperationType.Function_GetDayOfWeek:
                case DxExpressionOperationType.Function_GetDayOfYear:
                case DxExpressionOperationType.Function_GetTimeOfDay:
                    break;
                #endregion
                #region Function - DateTime 6: Current
                case DxExpressionOperationType.Function_Now:
                    // Returns the DateTime value that is the current date and time.
                    return DxExpressionPart.CreateText("getdate()");
                case DxExpressionOperationType.Function_UtcNow:
                    // Returns a DateTime object that is the current date and time in Universal Coordinated Time (UTC).
                    return DxExpressionPart.CreateText("getutcdate()");
                case DxExpressionOperationType.Function_Today:
                    // Returns a DateTime value that is the current date. The time part is set to 00:00:00.
                    return DxExpressionPart.CreateText("datetrunc(d, getdate())");
                case DxExpressionOperationType.Function_TruncateToMinute:
                    return DxExpressionPart.CreateText("datetrunc(mi, getdate())");
                #endregion
                #region Function - DateTime 7: Time (Hour, BeforeMidday, Afternoon, IsLunchTime...)
                case DxExpressionOperationType.Function_IsSameHour:
                case DxExpressionOperationType.Function_IsSameTime:
                case DxExpressionOperationType.Function_BeforeMidday:
                case DxExpressionOperationType.Function_AfterMidday:
                case DxExpressionOperationType.Function_IsNight:
                case DxExpressionOperationType.Function_IsMorning:
                case DxExpressionOperationType.Function_IsAfternoon:
                case DxExpressionOperationType.Function_IsEvening:
                case DxExpressionOperationType.Function_IsLastHour:
                case DxExpressionOperationType.Function_IsThisHour:
                case DxExpressionOperationType.Function_IsNextHour:
                case DxExpressionOperationType.Function_IsWorkTime:
                case DxExpressionOperationType.Function_IsFreeTime:
                case DxExpressionOperationType.Function_IsLunchTime:
                case DxExpressionOperationType.Function_AddTimeSpan:
                case DxExpressionOperationType.Function_AddTicks:
                case DxExpressionOperationType.Function_AddMilliSeconds:
                case DxExpressionOperationType.Function_AddSeconds:
                case DxExpressionOperationType.Function_AddMinutes:
                case DxExpressionOperationType.Function_AddHours:
                case DxExpressionOperationType.Function_AddDays:
                case DxExpressionOperationType.Function_AddMonths:
                case DxExpressionOperationType.Function_AddYears:
                case DxExpressionOperationType.Function_DateTimeFromParts:
                case DxExpressionOperationType.Function_DateOnlyFromParts:
                case DxExpressionOperationType.Function_TimeOnlyFromParts:
                    break;
                    #endregion

            }
            return null;

            string getBinaryOperatorText(DxExpressionOperationType binOp)
            {
                switch (binOp)
                {
                    case DxExpressionOperationType.Binary_Equal: return " = ";
                    case DxExpressionOperationType.Binary_NotEqual: return " <> ";
                    case DxExpressionOperationType.Binary_Greater: return " > ";
                    case DxExpressionOperationType.Binary_Less: return " < ";
                    case DxExpressionOperationType.Binary_LessOrEqual: return " <= ";
                    case DxExpressionOperationType.Binary_GreaterOrEqual: return " >= ";
                    case DxExpressionOperationType.Binary_Like: return " like ";
                    case DxExpressionOperationType.Binary_BitwiseAnd: return " & ";
                    case DxExpressionOperationType.Binary_BitwiseOr: return " | ";
                    case DxExpressionOperationType.Binary_BitwiseXor: return " ~ ";
                    case DxExpressionOperationType.Binary_Divide: return " / ";
                    case DxExpressionOperationType.Binary_Modulo: return " % ";
                    case DxExpressionOperationType.Binary_Multiply: return " * ";
                    case DxExpressionOperationType.Binary_Plus: return " + ";
                    case DxExpressionOperationType.Binary_Minus: return " - ";
                }
                return " " + binOp.ToString() + " ";
            }
            // Pokud daná část obsahuje value, typu Int, pak výstupem je string s touto hodnotou. Používá se u konstant, které NECHCEME řešit pomocí DB parametrů. Např. délka čísla atd.
            object intAsText(DxExpressionPart part, int addValue = 0)
            {
                if (part.IsValueInt32) return (part.ValueInt32 + addValue).ToString();
                return part;
            }
            void checkCount(int validCount)
            {
                if (count ==  validCount) return;
                failCount(validCount.ToString());
            }
            void failCount(string countInfo)
            {
                if (count < 0)
                    throw new ArgumentException($"Filter condition '{operation}' requires {countInfo} operators, but any from the supplied operators is not valid.");
                else
                    throw new ArgumentException($"Filter condition '{operation}' requires {countInfo} operators, but {count} valid operators are passed.");
            }
        }
        /// <summary>
        /// Konvertuje DX operaci na zdejší operaci, na základě názvu grupy a názvu DX operace (string TryParse <see cref="DxExpressionOperationType"/>).
        /// Neznámé vstupy vyhodí <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="familyType">Rodina operací</param>
        /// <param name="operationType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private DxExpressionOperationType ConvertOperation(FamilyType familyType, object operationType = null)
        {
            string resultName = ((operationType is null) ? $"{familyType}" : $"{familyType}_{operationType}");
            if (Enum.TryParse<DxExpressionOperationType>(resultName, true, out var resultValue)) return resultValue;
            throw new ArgumentException($"Invalid operation name: '{resultName}'; operation with this name does not exists.");
        }
        /// <summary>
        /// Typ rodiny operací
        /// </summary>
        private enum FamilyType
        {
            None,
            Group,
            Binary,
            Unary,
            Function,
            Custom
        }
        #endregion
        #region Konverze operandu DxFilter.CriteriaOperator do DxExpressionPart, jednotlivě i kolekce
        /// <summary>
        /// Z dodaného pole operandů <paramref name="operands"/> konvertuje jejich obsah do stringů a vrátí. 
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operands">Neomezené pole operandů</param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operand <paramref name="operand0"/>, a poté přidá operandy z <paramref name="operands"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operand <paramref name="operand0"/>, a poté přidá operandy z <paramref name="operands"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operands">Neomezené pole operandů, vkládají se až po: <paramref name="operand0"/></param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operandy <paramref name="operand0"/> a <paramref name="operand1"/>, a poté přidá operandy z <paramref name="operands"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operand1">Druhý fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand1)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operandy <paramref name="operand0"/> a <paramref name="operand1"/>, a poté přidá operandy z <paramref name="operands"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operand1">Druhý fixní operand</param>
        /// <param name="operands">Neomezené pole operandů, vkládají se až po: <paramref name="operand0"/>, <paramref name="operand1"/></param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand1)) return null;
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Z dodaného pole operandů <paramref name="operands"/> konvertuje jejich obsah do stringů a vrátí. 
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operand1">Druhý fixní operand</param>
        /// <param name="operand2">Třetí fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionPart> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1, DxFilter.CriteriaOperator operand2)
        {
            var operandItems = new List<DxExpressionPart>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand1)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand2)) return null;
            return operandItems;
        }
        private bool ConvertAllOperand(List<DxExpressionPart> operandItems, ConvertOperandsMode mode, DxFilter.CriteriaOperatorCollection operands)
        {
            if (operands is null) return (mode == ConvertOperandsMode.RemoveEmptyItems);        // Pokud na vstupu je null, pak vracím true = OK jen tehdy, když režim je volnější (Remove empty items)
            foreach (var operand in operands)
            {
                bool isValid = ConvertOneOperand(operandItems, mode, operand);
                if (!isValid) return false;
            }
            return true;
        }
        private bool ConvertOneOperand(List<DxExpressionPart> operandItems, ConvertOperandsMode mode, DxFilter.CriteriaOperator operand)
        {
            if (operand is null) return (mode == ConvertOperandsMode.RemoveEmptyItems);        // Pokud na vstupu je null, pak vracím true = OK jen tehdy, když režim je volnější (Remove empty items)
            var operandItem = operand.Accept(this);
            if (operandItem != null)
            {   // Toto je validní operand:
                operandItems.Add(operandItem);
                return true;
            }
            return (mode == ConvertOperandsMode.RemoveEmptyItems);
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
    #region class DxExpressionPart : Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// <summary>
    /// <see cref="DxExpressionPart"/> : Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// </summary>
    internal class DxExpressionPart
    {
        #region Public members : Create + Add
        /// <summary>
        /// Vytvoří prvek typu Container
        /// </summary>
        /// <returns></returns>
        internal static DxExpressionPart CreateContainer()
        {
            var part = new DxExpressionPart(PartType.Container);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateText(string text)
        {
            var part = new DxExpressionPart(PartType.Text);
            part.__Text = text;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Property = sloupec databáze
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateProperty(string propertyName)
        {
            var part = new DxExpressionPart(PartType.PropertyName);
            part.__PropertyName = propertyName;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Value = hodnota
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateValue(object value)
        {
            var part = new DxExpressionPart(PartType.Value);
            part.__Value = value;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateFrom(params object[] parts)
        {
            var part = new DxExpressionPart(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateFrom(IEnumerable<object> parts)
        {
            var part = new DxExpressionPart(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty, kde mezi každý nenulový objekt vloží text s daným oddělovačem
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionPart CreateDelimited(string delimiter, IEnumerable<object> parts)
        {
            var part = new DxExpressionPart(PartType.Container);
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
            return this.ToText(DxExpressionLanguageType.MsSqlDatabase);
        }
        /// <summary>
        /// Konvertuje obsah this prvku (podle jeho typu, tedy i včetně subprvků v Containeru) do textu v daném jazyce. Jazyk ovlivní formátování názvů sloupců a hodnot.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        internal string ToText(DxExpressionLanguageType language)
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
        private void _AddText(StringBuilder sb, DxExpressionLanguageType language)
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
        private DxExpressionPart(PartType partType)
        {
            __PartType = partType;
            if (partType == PartType.Container)
                __Items = new List<DxExpressionPart>();
        }
        private PartType __PartType;
        private List<DxExpressionPart> __Items;
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
        private static void _AddItemsTo(List<DxExpressionPart> targetItems, object[] addItems)
        {
            for (int i = 0; i < addItems.Length; i++)
            {
                // Na vstupu může být cokoliv - string, DxToExpressionPart (různého typu) i cokoliv jiného, což budeme chápat jako String. Jen null budeme přeskakovat.
                var addItem = addItems[i];
                if (addItem is null) continue;

                var targetCount = targetItems.Count;
                var targetLastItem = (targetCount > 0 ? targetItems[targetCount - 1] : null);
                bool canMergeText = (targetCount > 0 && targetLastItem.IsText);
                if (addItem is DxExpressionPart addPart)
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
            DxExpressionPart simple = (isSimple ? this.MemberwiseClone() as DxExpressionPart : null);

            // Pokud this dosud NENÍ Container, pak this změním tak, aby byl Container:
            if (!isContainer)
            {
                this.__PartType = PartType.Container;
                this.__Items = new List<DxExpressionPart>();
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
        public string Text { get { return (IsText ? __Text : null); } set { if (IsText) __Text = value; } }
        public string PropertyName { get { return (IsPropertyName ? __PropertyName : null); } set { if (IsPropertyName) __PropertyName = value; } }
        public object Value { get { return (IsValue ? __Value : null); } set { if (IsValue) __Value = value; } }
        public string ValueString { get { return (IsValueString ? __Value as string : null); } }
        public int ValueInt32 { get { return (IsValueInt32 ? (int)__Value : 0); } }
        public DxExpressionPart[] Items { get { return (IsContainer ? __Items.ToArray() : null); } }
        /// <summary>
        /// Druh částice
        /// </summary>
        public enum PartType
        {
            /// <summary>
            /// Container: obsahuje další prvky, viz <see cref="DxExpressionPart.Items"/>
            /// </summary>
            Container,
            /// <summary>
            /// Prostý text: klíčová slova, oddělovače, závorky, atd, viz <see cref="DxExpressionPart.Text"/>
            /// </summary>
            Text,
            /// <summary>
            /// Název datového sloupce / property, viz <see cref="DxExpressionPart.PropertyName"/>
            /// </summary>
            PropertyName,
            /// <summary>
            /// Hodnota: obsahuje zadanou hodnotu / konstantu / proměnnou / parametr, viz <see cref="DxExpressionPart.Value"/>
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
    internal enum DxExpressionLanguageType
    {
        /// <summary>
        /// Neurčeno, základní
        /// </summary>
        Default,
        /// <summary>
        /// Optimální MS SQL databáze
        /// </summary>
        MsSqlDatabase,
        /// <summary>
        /// System.Data.DataTable filter
        /// </summary>
        SystemDataFilter
    }
    internal delegate void DxExpressionCustomHandler(object sender, DxExpressionCustomArgs args);
    /// <summary>
    /// Argumenty s daty pro custom handler
    /// </summary>
    internal class DxExpressionCustomArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="language"></param>
        /// <param name="operation"></param>
        /// <param name="operands"></param>
        public DxExpressionCustomArgs(DxExpressionLanguageType language, DxExpressionOperationType operation, List<DxExpressionPart> operands)
        {
            this.Language = language;
            this.Operation = operation;
            this.Operands = operands;
            this.CustomResult = null;
        }
        /// <summary>
        /// Jazyk konverze
        /// </summary>
        public DxExpressionLanguageType Language { get; private set; }
        /// <summary>
        /// Druh konkrétní operace
        /// </summary>
        public DxExpressionOperationType Operation { get; set; }
        /// <summary>
        /// Jednotlivé operandy. Jejich význam a počet je dán typem operace.
        /// </summary>
        public List<DxExpressionPart> Operands { get; set; }
        /// <summary>
        /// Externí aplikace si přeje tuto funkci přeskočit, výsledkem bude null. Může to být v pořádku jen tehdy, když operace je členem vyšší operace, která nemá povinné operandy.
        /// </summary>
        public bool Skip { get; set; }
        /// <summary>
        /// Externí aplikace sama určila výsledný tvar výrazu
        /// </summary>
        public DxExpressionPart CustomResult { get; set; }
    }
    /// <summary>
    /// Typ operace
    /// </summary>
    internal enum DxExpressionOperationType
    {
        None,
        Group_And,
        Group_Or,

        Between,

        Binary_Equal,
        Binary_NotEqual,
        Binary_Greater,
        Binary_Less,
        Binary_LessOrEqual,
        Binary_GreaterOrEqual,
        Binary_Like,
        Binary_BitwiseAnd,
        Binary_BitwiseOr,
        Binary_BitwiseXor,
        Binary_Divide,
        Binary_Modulo,
        Binary_Multiply,
        Binary_Plus,
        Binary_Minus,

        Unary_BitwiseNot,
        Unary_Plus,
        Unary_Minus,
        Unary_Not,
        Unary_IsNull,

        In,

        Function_None,
        Function_Custom,
        Function_CustomNonDeterministic,
        Function_Iif,
        Function_IsNull,
        Function_IsNullOrEmpty,
        Function_Trim,
        Function_Len,
        Function_Substring,
        Function_Upper,
        Function_Lower,
        Function_Concat,
        Function_Ascii,
        Function_Char,
        Function_ToStr,
        Function_Replace,
        Function_Reverse,
        Function_Insert,
        Function_CharIndex,
        Function_Remove,
        Function_Abs,
        Function_Sqr,
        Function_Cos,
        Function_Sin,
        Function_Atn,
        Function_Exp,
        Function_Log,
        Function_Rnd,
        Function_Tan,
        Function_Power,
        Function_Sign,
        Function_Round,
        Function_Ceiling,
        Function_Floor,
        Function_Max,
        Function_Min,
        Function_Acos,
        Function_Asin,
        Function_Atn2,
        Function_BigMul,
        Function_Cosh,
        Function_Log10,
        Function_Sinh,
        Function_Tanh,
        Function_PadLeft,
        Function_PadRight,
        Function_StartsWith,
        Function_EndsWith,
        Function_Contains,
        Function_ToInt,
        Function_ToLong,
        Function_ToFloat,
        Function_ToDouble,
        Function_ToDecimal,
        Function_LocalDateTimeThisYear,
        Function_LocalDateTimeThisMonth,
        Function_LocalDateTimeLastWeek,
        Function_LocalDateTimeThisWeek,
        Function_LocalDateTimeYesterday,
        Function_LocalDateTimeToday,
        Function_LocalDateTimeNow,
        Function_LocalDateTimeTomorrow,
        Function_LocalDateTimeDayAfterTomorrow,
        Function_LocalDateTimeNextWeek,
        Function_LocalDateTimeTwoWeeksAway,
        Function_LocalDateTimeNextMonth,
        Function_LocalDateTimeNextYear,
        Function_LocalDateTimeTwoMonthsAway,
        Function_LocalDateTimeTwoYearsAway,
        Function_LocalDateTimeLastMonth,
        Function_LocalDateTimeLastYear,
        Function_LocalDateTimeYearBeforeToday,
        Function_IsOutlookIntervalBeyondThisYear,
        Function_IsOutlookIntervalLaterThisYear,
        Function_IsOutlookIntervalLaterThisMonth,
        Function_IsOutlookIntervalNextWeek,
        Function_IsOutlookIntervalLaterThisWeek,
        Function_IsOutlookIntervalTomorrow,
        Function_IsOutlookIntervalToday,
        Function_IsOutlookIntervalYesterday,
        Function_IsOutlookIntervalEarlierThisWeek,
        Function_IsOutlookIntervalLastWeek,
        Function_IsOutlookIntervalEarlierThisMonth,
        Function_IsOutlookIntervalEarlierThisYear,
        Function_IsOutlookIntervalPriorThisYear,
        Function_IsThisWeek,
        Function_IsThisMonth,
        Function_IsThisYear,
        Function_IsNextMonth,
        Function_IsNextYear,
        Function_IsLastMonth,
        Function_IsLastYear,
        Function_IsYearToDate,
        Function_IsSameDay,
        Function_InRange,
        Function_InDateRange,
        Function_IsJanuary,
        Function_IsFebruary,
        Function_IsMarch,
        Function_IsApril,
        Function_IsMay,
        Function_IsJune,
        Function_IsJuly,
        Function_IsAugust,
        Function_IsSeptember,
        Function_IsOctober,
        Function_IsNovember,
        Function_IsDecember,
        Function_DateDiffTick,
        Function_DateDiffSecond,
        Function_DateDiffMilliSecond,
        Function_DateDiffMinute,
        Function_DateDiffHour,
        Function_DateDiffDay,
        Function_DateDiffMonth,
        Function_DateDiffYear,
        Function_GetDate,
        Function_GetMilliSecond,
        Function_GetSecond,
        Function_GetMinute,
        Function_GetHour,
        Function_GetDay,
        Function_GetMonth,
        Function_GetYear,
        Function_GetDayOfWeek,
        Function_GetDayOfYear,
        Function_GetTimeOfDay,
        Function_Now,
        Function_UtcNow,
        Function_Today,
        Function_TruncateToMinute,
        Function_IsSameHour,
        Function_IsSameTime,
        Function_BeforeMidday,
        Function_AfterMidday,
        Function_IsNight,
        Function_IsMorning,
        Function_IsAfternoon,
        Function_IsEvening,
        Function_IsLastHour,
        Function_IsThisHour,
        Function_IsNextHour,
        Function_IsWorkTime,
        Function_IsFreeTime,
        Function_IsLunchTime,
        Function_AddTimeSpan,
        Function_AddTicks,
        Function_AddMilliSeconds,
        Function_AddSeconds,
        Function_AddMinutes,
        Function_AddHours,
        Function_AddDays,
        Function_AddMonths,
        Function_AddYears,
        Function_DateTimeFromParts,
        Function_DateOnlyFromParts,
        Function_TimeOnlyFromParts
    }
    #endregion
}
