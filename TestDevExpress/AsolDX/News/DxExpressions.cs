using DevExpress.Data.Filtering;
using Noris.Clients.Win.Components.AsolDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using Noris.Srv.NrsInternal;
using DxFilter = DevExpress.Data.Filtering;
using DevExpress.Pdf.Native.BouncyCastle.Asn1.X509;
using DevExpress.XtraEditors.Design;
using System.Reflection.Emit;

namespace Noris.Srv.NrsInternal.DxFiltering
{
    #region class DxFilterConvertor : public konvertor dat z filtru DX do formy MS SQL atd
    /// <summary>
    /// <see cref="DxFilterConvertor"/> : public konvertor dat z filtru DX do formy MS SQL atd
    /// </summary>
    internal class DxFilterConvertor
    {
        #region Public members: ConvertToString, ConvertToPart
        /// <summary>
        /// Konvertuje dodaný string (výraz DevExpress filtru) do jazyka MS SQL, bez specifických konverzí.
        /// </summary>
        /// <param name="dxExpression"></param>
        /// <returns></returns>
        public static string ConvertToString(string dxExpression)
        {
            var args = new ConvertArgs() { DxExpression = dxExpression };
            var part = _ConvertToPart(args);
            return part?.ResultText;
        }
        /// <summary>
        /// Konvertuje dodaný filtrační výraz (výraz DevExpress filtru) do tokenu v jazyce MS SQL, aplikuje specifickované parametry (sloupce, handler, atd)
        /// </summary>
        /// <param name="convertArgs"></param>
        /// <returns></returns>
        public static DxExpressionToken Convert(ConvertArgs convertArgs)
        {
            return _ConvertToPart(convertArgs);
        }
        /// <summary>
        /// Konvertuje dodaný filtrační výraz (výraz DevExpress filtru) do tokenu v jazyce MS SQL, aplikuje specifickované parametry (sloupce, handler, atd)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static DxExpressionToken _ConvertToPart(ConvertArgs args)
        {
            if (args is null || !args.IsValid) return null;

            var visitor = new DxCriteriaVisitor(args);
            var result = args.Filter.Accept(visitor);
            if (result != null) ((IDxExpressionTokenWorking)result).Language = args.Language;
            return result;
        }
        #endregion
        #region Public members: FormatPropertyName, FormatValue
        /// <summary>
        /// Formátuje název sloupce
        /// </summary>
        /// <param name="propertyToken"></param>
        /// <returns></returns>
        internal static string FormatPropertyName(DxExpressionToken propertyToken)
        {
            string resultName = null;

            if (!String.IsNullOrEmpty(propertyToken.PropertyResult))
                resultName = propertyToken.PropertyResult;                               // Explicitně dodaný text, popisující zdroj dat pro tento "sloupec"

            if (String.IsNullOrEmpty(resultName) && propertyToken.Column != null && !String.IsNullOrEmpty(propertyToken.Column.DisplayValueSource))
                resultName = propertyToken.Column.DisplayValueSource;                    // Standardně zobrazovaný výraz pro aktuální sloupec

            if (String.IsNullOrEmpty(resultName))
                resultName = propertyToken.PropertyName;                                 // Název sloupce (ColumnId) tak, jak byl nalezen v podmínce

            if (!String.IsNullOrEmpty(resultName) && propertyToken.Language == DxExpressionLanguageType.MsSqlDatabase &&
                !(resultName.Contains(".") || resultName.Contains(" ") || resultName.Contains(",") || resultName.Contains("(") || resultName.Contains(")") || resultName.Contains("+") || resultName.Contains("-")))
                resultName = $"[{resultName}]";

            return resultName;
        }
        /// <summary>
        /// Formátuje hodnotu
        /// </summary>
        /// <param name="value"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        internal static string FormatValue(object value, DxExpressionLanguageType language)
        {
            if (value is null) return "NULL";

            if (value is Byte) return ((Byte)value).ToString();
            if (value is SByte) return ((SByte)value).ToString();

            if (value is UInt16) return ((UInt16)value).ToString();
            if (value is UInt32) return ((UInt32)value).ToString();
            if (value is UInt64) return ((UInt64)value).ToString();
            if (value is Int16) return ((Int16)value).ToString();
            if (value is Int32) return ((Int32)value).ToString();
            if (value is Int64) return ((Int64)value).ToString();

            if (value is Single) return ((Single)value).ToString().Replace(",", ".");
            if (value is Double) return ((Double)value).ToString().Replace(",", ".");
            if (value is Decimal) return ((Decimal)value).ToString().Replace(",", ".");

            if (value is DateTime) { var dt = (DateTime)value; return $"convert(datetime, '{dt.Year:D4}-{dt.Month:D2}-{dt.Day:D2} {dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}.{dt.Millisecond:D3}', 121)"; }

            if (value is Boolean) { var bv = (Boolean)value; return bv ? "1" : "0"; }

            if (value is String) { var sv = (String)value; return "'" + sv.Replace("'", "''") + "'"; }

            return $"'{value}'";
        }
        #endregion
        #region Public members: ConvertRowFilterPartForColumns (konverze operátorů a hodnot řádkového filtru pro sloupce a jejich editační styl)
        /// <summary>
        /// Metoda vyřeší ty operátory řádkového filtru, které mohou spolupracovat se sloupcem s CodeTable, pokud se na ně řádkový filtr odkazuje.
        /// <para/>
        /// Tedy například, pokud uživatel vidí sloupec "Stav dokladu" s nabídkou hodnot (pocházející z CodeTable): 0="Pořízen";  1="Realizován"; 2=Stornován", 
        /// a nastaví řádkový filtr na "Stav" = "Realizován", pak zdejší metoda:<br/>
        /// - vyhodnotí operátor "=" jako řešitelný;<br/>
        /// - najde sloupec šablony ("Stav dokladu");<br/>
        /// - zjistí, že sloupec má CodeTable;<br/>
        /// - v CodeTable tohoto sloupce najde položku s DisplayValue = "Realizován" a tomu odpovídající CodeValue = 1;<br/>
        /// - ve sloupci dále najde název zdrojového sloupce v databázi, kde je uložena CodeValue, např. "dokl.status") a vloží jej do operandu Property jako jeho PropertyName;<br/>
        /// - odpovídajícím způsobem modifikuje operandy typu Value (vymění název sloupce v operandu Property, a vymění hodnotu v operandu Value);
        /// <para/>
        /// Pokud uživatel zadá do řádkového filtru operátor například "Obsahuje" nebo "Začíná" nebo "Končí":<br/>
        /// - pak metoda v použitém sloupci vyhledá v CodeTable <b><u>všechny hodnoty</u></b> odpovídající danému typu operátoru a danému textu;<br/>
        /// - zamění v operandu Property název sloupce v databázi (jako minule);<br/>
        /// - změní operátor na "IN";<br/>
        /// - vloží sadu operandů typu Value, které budou obsahovat jednotlivé CodeValue z nalezených položek;<br/>
        /// - například pro zadání: "Stav dokladu": Function_EndsWith: "án":;<br/>
        /// - budou v CodeTable nalezeny hodnoty: 1="Realizován"; 2=Stornován"; a výsledný filtr tedy bude: "<c>dokl.status in (1, 2)</c>"
        /// </summary>
        /// <param name="args"></param>
        internal static void ConvertRowFilterPartForColumns(DxConvertorCustomArgs args)
        {
            var convertType = _GetConvertedOperationFor(args, out var propertyToken, out var column, out var codeValues);
            if (convertType == DxCodeTableOperationType.NotCodeTable) return;

            switch (column.SourceType)
            {
                case FilterColumnSourceType.CodeTable:
                    _ConvertRowFilterPartForCodeTable(args, propertyToken, column, codeValues, convertType);
                    break;
                case FilterColumnSourceType.Virtual:


                    break;
            }
        }
        /// <summary>
        /// Vytvoří standardní Dictionary z dodaných Columns, kde Key = ColumnId, se zadaným CaseSensitive comparerem
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        internal static Dictionary<string, IFilterColumnInfo> CreateColumnsDictionary(IEnumerable<IFilterColumnInfo> columns, bool caseSensitive)
        {
            var dictComparer = (caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            var columnDict = new Dictionary<string, IFilterColumnInfo>(dictComparer);
            if (columns != null)
            {
                foreach (var column in columns)
                {
                    var colId = column?.ColumnId;
                    if (!String.IsNullOrEmpty(colId) && !columnDict.ContainsKey(colId))
                        columnDict.Add(colId, column);
                }
            }
            return columnDict;
        }
        /// <summary>
        /// Metoda určí, jak specificky konvertovat zadanou operaci, pokud se odvolává na sloupec, který obsahuje CodeTable.
        /// Určí sloupec <see cref="IFilterColumnInfo"/>, jehož hodnota se bude řešit.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="propertyToken">Out token reprezentující sloupec</param>
        /// <param name="column">Out Sloupec, jehož se výraz filtru týká</param>
        /// <param name="codeValues">Out Hodnoty typu CodeValue, nalezené v CodeTable daného sloupce, k stringovým hodnotám typu DisplayValue v operátorech dané operace</param>
        /// <returns></returns>
        private static DxCodeTableOperationType _GetConvertedOperationFor(DxConvertorCustomArgs args, out DxExpressionToken propertyToken, out IFilterColumnInfo column, out object[] codeValues)
        {
            // Default out:
            propertyToken = null;
            column = null;
            codeValues = null;

            // Obecně operandy:
            var operation = args.Operation;
            var operandsCount = args.Operands.Count;
            if (operandsCount < 2)
                return DxCodeTableOperationType.NotCodeTable;                            // S jedním anebo žádným operandem nemá cenu nic řešit

            // Řešíme tři varianty zadání (tři typy operací): Equals, Regex a InList:
            bool isEquals = (operation == DxFilterOperationType.Binary_Equal || operation == DxFilterOperationType.Binary_NotEqual);
            bool isPattern = (operation == DxFilterOperationType.Function_StartsWith || operation == DxFilterOperationType.Function_Contains || operation == DxFilterOperationType.Function_EndsWith || operation == DxFilterOperationType.Binary_Like || operation == DxFilterOperationType.Custom_Like);
            bool isInList = (operation == DxFilterOperationType.In);
            if (!(isEquals || isPattern || isInList)) 
                return DxCodeTableOperationType.NotCodeTable;                            // Ostatní typy operací nemá význam řešit

            // Najdeme sloupec (Property), který musí být jeden a musí být Specific (CodeTable nebo Virtual):
            var opProperties = args.Operands.Where(op => op.IsPropertyName).ToArray();
            if (opProperties.Length != 1 || !isColumnSpecific(opProperties[0])) 
                return DxCodeTableOperationType.NotCodeTable;                            // Tato akce se týká pouze Columnů, které jsou specifické

            // OK.   Máme vhodnou operaci, a máme sloupec se specifickým chováním.   
            propertyToken = opProperties[0];
            column = propertyToken.Column;

            //  Načteme datové operandy, které musí nést String. Nic jiného v operandech nemůže být:
            //  Operandy musí být pouze: 1 sloupec a všechny ostatní ValueString
            var opValues = args.Operands.Where(op => op.IsValueString).ToArray();        // Stringová hodnota, nese zadanou hodnotu DisplayValue (jiné operandy neberu, a pokud budou - pak skončíme)
            if ((opValues.Length + 1) != operandsCount)
                return DxCodeTableOperationType.NotCodeTable;                            // Mezi operandy bylo i něco jiného než String. To bychom nedokázali vyhodnotit, a to je chyba.
            if ((isEquals || isPattern) && opValues.Length != 1)
                return DxCodeTableOperationType.NotCodeTable;                            // Operace typu Equals anebo Pattern vyžadují právě jeden operand typu String ( Sloupec = 'Hodnota' nebo StartWith(Sloupec, 'Hodn') )

            if (isEquals || isInList)
            {   // Pro operace Binary_Equal a Binary_NotEqual, a In:
                codeValues = getValuesEquals(column.CodeTableItems, opValues);           // POZOR: tady je legální, když pro jeden operand opValues se vrátí více values[], pokud por jednu DisplayValue máme více CodeValue v tabulce CodeTable!!!
            }
            else if (isPattern)
            {   // Vyhledáme CodeValues do out pole values:
                var regex = getLikePattern(operation, opValues[0].ValueString);          // opValues obsahuje zaručeně právě jen jeden operand, typu ValueString = zadaný textový pattern => vytvořím z něj Regex:
                codeValues = getValuesRegex(column.CodeTableItems, regex);               // Pro jeden Regex mohu získat vícero CodeValue
            }
            // Nyní tedy víme, na který specifický sloupec filtrujeme, a jaké CodeValue ve filtru budou (anebo taky žádná, anebo více hodnot InList).

            // Z kombinace počtu hodnot a zadaného operátoru (NotEqual, In, Equal) určíme, jaký finální operátor (DxFilterOperationType) by měl být aplikován:
            var valuesCount = codeValues.Length;
            if (operation == DxFilterOperationType.Binary_NotEqual)
            {   // Tenhle operátor je jiný => ten říká "nesmí to být něco z tohoto" (values), a zvlášť pokud ve values nic není (zadaná DisplayValue neodpovídá žádné položce CodeTable):
                if (valuesCount == 0) return DxCodeTableOperationType.True;              // Podmínka zněla "Stav dokladu" <> "Ztracený" a přitom hodnota "Ztracený" v CodeTable není, tedy filtr na základě CodeValue nebude nic omezovat => vynecháme jej zcela:
                if (valuesCount == 1) return DxCodeTableOperationType.NotEqual;          // Podmínka zněla "Stav dokladu" <> "Aktivní" a hodnotu "Aktivní" v CodeTable máme jedenkrát, filtr bude "dokl.status <> 2"
                return DxCodeTableOperationType.NotInList;                               // Podmínka zněla "Stav dokladu" <> "Aktivní" a hodnotu "Aktivní" v CodeTable máme vícekrát pro různé CodeValue, filtr bude "dokl.status not in (2,3,4)"
            }
            // Ostatní operátory jsou "pozitivní", a říkají tedy že filtru vyhoví ty záíznamy, které v daném sloupci mají jednu nebo více hodnot, anebo žádná hodnotas = nevyhoví žádný záznam:
            if (valuesCount == 0) return DxCodeTableOperationType.False;                 // Podmínka zněla "Stav dokladu" = "Ztracený" a přitom hodnota "Ztracený" v CodeTable není, tedy filtr na základě CodeValue musí vyřadit všechny záznamy => bude tedy znít (1=0)
            if (valuesCount == 1) return DxCodeTableOperationType.Equal;                 // Podmínka zněla "Stav dokladu" = "Aktivní" a hodnotu "Aktivní" v CodeTable máme jedenkrát, filtr bude "dokl.status = 2"
            return DxCodeTableOperationType.InList;                                      // Podmínka zněla "Stav dokladu" like "Akt*" a hodnotu "Akt*" v CodeTable máme vícekrát pro různé CodeValue, filtr bude "dokl.status in (2,3,4)"


            // Vrátí true, pokud dodaná token reprezentuje Property, má dohledaný sloupec, a sloupec je typu CodeTable nebo Virtual:
            bool isColumnSpecific(DxExpressionToken dxToken)
            {
                return (dxToken != null && dxToken.IsPropertyName && dxToken.Column != null && (dxToken.Column.SourceType == FilterColumnSourceType.CodeTable || dxToken.Column.SourceType == FilterColumnSourceType.Virtual));
            }
            // Vrátí Regex odpovídající danému operátoru (typ podmínky) a zadanému textu podmínky
            System.Text.RegularExpressions.Regex getLikePattern(DxFilterOperationType operation, string filterText)
            {
                string pattern = "";
                switch (operation)
                {
                    // Tyto tři operace pro běžné hodnoty převádíme na LIKE '%text%', takže musíme i tady:
                    case DxFilterOperationType.Function_StartsWith:
                        pattern = $"^{filterText}%";
                        break;
                    case DxFilterOperationType.Function_Contains:
                        pattern = $"%{filterText}%";
                        break;
                    case DxFilterOperationType.Function_EndsWith:
                        pattern = $"%{filterText}$";
                        break;
                    case DxFilterOperationType.Binary_Like:
                    case DxFilterOperationType.Custom_Like:
                        // Like operátor očekává, že případný znak "%" na začátku nebo na konci už bude přítomno v zadaném textu, a nepřidáváme jej tedy:
                        pattern = $"^{filterText}$";
                        break;
                }

                // Konverze SQL to RegEx:
                // Escapování funkčních znaků:
                replace(ref pattern, @"\", @"\\");
                replace(ref pattern, @".", @"\.");
                replace(ref pattern, @"*", @"\*");
                replace(ref pattern, @"+", @"\+");
                replace(ref pattern, @"?", @"\?");
                replace(ref pattern, @"(", @"\(");
                replace(ref pattern, @")", @"\)");
                replace(ref pattern, @"{", @"\{");
                replace(ref pattern, @"}", @"\}");
                replace(ref pattern, @"/", @"\/");

                // Wildcards:
                replace(ref pattern, @"%", @".*");
                replace(ref pattern, @"_", @".");

                // Výčet znaků [abcd] se nekonvertuje, je v SQL i v RegEx shodný.

                // Hotovo, case - insensitive:
                System.Text.RegularExpressions.Regex regex = null;

                try
                {   // try-catch, abych ohlídal nevalidní pattern:
                    // Exact pattern:
                    regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {   // Chyba? Odeberu řídící znaky [výčtů] (ostatní jsem escapoval nahoře):
                    replace(ref pattern, @"[", @"\[");
                    replace(ref pattern, @"]", @"\]");
                    try
                    {   // Náhradní pattern:
                        regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                    catch
                    {   // Beru cokoliv:
                        regex = new System.Text.RegularExpressions.Regex(".*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                }
                return regex;
            }
            // Nahradí zadanou část textu jiným textem, pokud tam ten vstupní je
            void replace(ref string text, string search, string replacement)
            {
                if (text.Contains(search)) text = text.Replace(search, replacement);
            }
            // Z dodané CodeTable (která je definovaná položkami { Key=CodeValue, Value=DisplayValue } ) vybere ty položky, jejichž DisplayValue odpovídá kterékoli hodnotě, dodané v poli 'displayValues' jako ValueString.
            //  Výstupní pole bude tedy v pořadí dle codeTableItems, kde samozřejmě nebudou duplikátní položky - i kdyby byly duplikátní v displayValues.
            //  A naopak, pokud by více položek v codeTableItems mělo shodný DisplayValue a ten byl zadán v jedném prvku pole 'displayValues', 
            //     pak ve výstupu bude více CodeValues, které společně odpovídají shodnému DisplayValue, i kdyby byl zadán v jedném prvku v 'displayValues'.
            object[] getValuesEquals(KeyValuePair<object, string>[] codeTableItems, DxExpressionToken[] displayValues)
            {
                if (codeTableItems is null || codeTableItems.Length == 0 || displayValues is null || displayValues.Length == 0) return new object[0];

                return codeTableItems
                    .Where(cti => displayValues.Any(dv => String.Equals(cti.Value, dv.ValueString, StringComparison.CurrentCultureIgnoreCase)))
                    .Select(cti => cti.Key)
                    .ToArray();
            }
            // Z dodané CodeTable (která je definovaná položkami { CodeValue, DisplayValue } ) vybere ty položky, jejichž DisplayValue odpovídá dodanému Regex výrazu.
            object[] getValuesRegex(KeyValuePair<object, string>[] codeTableItems, System.Text.RegularExpressions.Regex regex)
            {
                if (codeTableItems is null || codeTableItems.Length == 0 || regex is null) return new object[0];

                return codeTableItems
                    .Where(cti => regex.IsMatch(cti.Value))
                    .Select(cti => cti.Key)
                    .ToArray();
            }
        }
        /// <summary>
        /// Metoda provede finální sestavení filtračního výrazu pro dodaná data (sloupec, hodnoty, styl výrazu).
        /// </summary>
        /// <param name="args"></param>
        /// <param name="propertyToken"></param>
        /// <param name="column"></param>
        /// <param name="codeValues"></param>
        /// <param name="convertType"></param>
        private static void _ConvertRowFilterPartForCodeTable(DxConvertorCustomArgs args, DxExpressionToken propertyToken, IFilterColumnInfo column, object[] codeValues, DxCodeTableOperationType convertType)
        {
            var tokenValues = codeValues?.Select(cv => DxExpressionToken.CreateValue(cv)).ToArray();
            switch (convertType)
            {
                case DxCodeTableOperationType.False:
                    args.CustomResult = DxExpressionToken.CreateText("(1=0)");
                    break;
                case DxCodeTableOperationType.True:
                    args.CustomResult = DxExpressionToken.CreateText("(1=1)");
                    break;
                case DxCodeTableOperationType.Equal:
                    propertyToken.PropertyResult = column.CodeValueSource;
                    args.CustomResult = DxExpressionToken.CreateFrom(propertyToken, " = ", tokenValues[0]);
                    break;
                case DxCodeTableOperationType.NotCodeTable:
                    propertyToken.PropertyResult = column.CodeValueSource;
                    args.CustomResult = DxExpressionToken.CreateFrom(propertyToken, " <=> ", tokenValues[0]);
                    break;
                case DxCodeTableOperationType.InList:
                    propertyToken.PropertyResult = column.CodeValueSource;
                    args.CustomResult = DxExpressionToken.CreateFrom(propertyToken, " in (", DxExpressionToken.CreateDelimited(",", tokenValues), ")");
                    break;
                case DxCodeTableOperationType.NotInList:
                    propertyToken.PropertyResult = column.CodeValueSource;
                    args.CustomResult = DxExpressionToken.CreateFrom(propertyToken, " in (", DxExpressionToken.CreateDelimited(",", tokenValues), ")");
                    break;
            }
        }
        #endregion
    }
    /// <summary>
    /// Data pro konverzi
    /// </summary>
    internal class ConvertArgs
    {
        public ConvertArgs()
        {
            Language = DxExpressionLanguageType.MsSqlDatabase;
        }
        /// <summary>
        /// Filtrační výraz jako instance <see cref="DxFilter.CriteriaOperator"/>
        /// </summary>
        public DxFilter.CriteriaOperator Filter { get { return __Filter; } set { __Filter = value; } } private DxFilter.CriteriaOperator __Filter;
        /// <summary>
        /// Filtrační výraz jako string; ukládá se / čte se z <see cref="Filter"/>
        /// </summary>
        public string DxExpression { get { return __Filter?.ToString(); } set { __Filter = (!String.IsNullOrEmpty(value) ? DxFilter.CriteriaOperator.Parse(value) : null); } }
        /// <summary>
        /// Jazyk výrazu
        /// </summary>
        public DxExpressionLanguageType Language { get { return __Language; } set { __Language = value; } } private DxExpressionLanguageType __Language;
        /// <summary>
        /// Kolekce sloupců, která slouží k určení zdrojového textu pro výraz, a pro zpracování editačních stylů
        /// </summary>
        public IEnumerable<IFilterColumnInfo> Columns { get { return __Columns; } set { __Columns = value?.ToArray(); } } private IFilterColumnInfo[] __Columns;
        /// <summary>
        /// Obsahuje true, pokud kolekce sloupců má alespoň jeden prvek = máme sloupce
        /// </summary>
        public bool HasColumns { get { return (__Columns != null && __Columns.Length > 0); } }
        /// <summary>
        /// Externí handler, který umožňuje řešit každou konverzi každého operátoru externě
        /// </summary>
        public DxConvertorCustomHandler CustomHandler { get { return __CustomHandler; } set { __CustomHandler = value; } } private DxConvertorCustomHandler __CustomHandler;
        /// <summary>
        /// Obsahuje true, pokud je dodán externí handler <see cref="CustomHandler"/>
        /// </summary>
        public bool HasCustomHandler { get { return (__CustomHandler != null); } }

        /// <summary>
        /// Obsahuje true, pokud argument je validní a použitelný
        /// </summary>
        public bool IsValid { get { return !(__Filter is null); } }
    }
    /// <summary>
    /// Obecná data o sloupci šablony, rozšířená verze
    /// </summary>
    internal interface IFilterColumnInfo
    {
        /// <summary>
        /// ID sloupce = jednoznačný alias v načítaných datech, aktuální. Pod tímto názvem se sloupec vyskytuje v zadaném filtračním výrazu.
        /// </summary>
        string ColumnId { get; }
        /// <summary>
        /// Zdroj pro tento sloupec, pro jeho zobrazovanou hodnotu.
        /// U sloupce s editačním stylem je zde typicky rozklad editačního stylu:<br/>
        /// <c>case tab.status when 0 then 'Pořízen' when 1 then 'Aktivován' when 2 then 'Stornován' else 'Jiný' end</c><br/>
        /// </summary>
        string DisplayValueSource { get; }
        /// <summary>
        /// Zdroj pro CodeValue v tomto sloupci. Je naplněn typicky pro sloupce s editačním stylem, pak zde je jeho podkladová datová hodnota, typicky:<br/>
        /// <c>tab.status</c>
        /// </summary>
        string CodeValueSource { get; }
        /// <summary>
        /// Typ zdroje dat v tomto sloupci
        /// </summary>
        FilterColumnSourceType SourceType { get; }
        /// <summary>
        /// Položky CodeTable (z Editačního stylu anebo z Valuace atributů), kde Key = Code a Value = DisplayText
        /// </summary>
        KeyValuePair<object, string>[] CodeTableItems { get; }
    }
    /// <summary>
    /// Typ zdroje dat ve sloupci <see cref="IFilterColumnInfo"/>. Řídí zpracování sloupců specifického typu pro konkrétní typy operátorů.
    /// </summary>
    internal enum FilterColumnSourceType
    {
        /// <summary>
        /// Defaultní = co je uvedeno ve filtru, to je řešeno v podmínce. Běžné sloupce, které jednoduše zobrazují datovou hodnotu.
        /// </summary>
        Default,
        /// <summary>
        /// Sloupec zobrazující DisplayValue z daného Editačního stylu, kde do textu podmínky filtru nebudeme dávat jeho <see cref="IFilterColumnInfo.DisplayValueSource"/>, 
        /// ale kódovu hodnotu <see cref="IFilterColumnInfo.CodeValueSource"/>.
        /// </summary>
        CodeTable,
        /// <summary>
        /// Virtuální sloupec, který zobrazuje hodnotu, získanou typicky subselectem, který není uložen v <see cref="IFilterColumnInfo.CodeValueSource"/>, 
        /// ale je třeba do textu podmínky jej sestavit.
        /// </summary>
        Virtual
    }
    /// <summary>
    /// Potřebný typ operace po náhradě podmínky filtru, která se týká operace s CodeTable hodnotami
    /// </summary>
    internal enum DxCodeTableOperationType
    {
        /// <summary>
        /// Aktuální podmínka filtru se netýká sloupce s CodeTable
        /// </summary>
        NotCodeTable,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu False: <c>(1=0)</c>, protože zadané filtrační podmínce nemůže vyhovět žádný záznam
        /// </summary>
        False,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu True: <c>(1=1)</c>, protože zadané filtrační podmínce vyhovuje každý záznam
        /// </summary>
        True,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu Equal: <c>CodeValueSource = value[0]</c>, protože zadaná podmínka je "pozitivní" a hodnotě sloupce odpovídá jediná CodeValue
        /// </summary>
        Equal,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu NotEqual: <c>CodeValueSource != value[0]</c>, protože zadaná podmínka je "negativní" a hodnotě sloupce neodpovídá jediná CodeValue
        /// </summary>
        NotEqual,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu In List: <c>CodeValueSource in (values...)</c>, protože zadaná podmínka je "pozitivní" a hodnotě sloupce odpovídá více CodeValues
        /// </summary>
        InList,
        /// <summary>
        /// Aktuální podmínka filtru má být nahrazena podmínkou typu Not In List: <c>CodeValueSource not in (values...)</c>, protože zadaná podmínka je "negativní " a hodnotě sloupce neodpovídá více CodeValues
        /// </summary>
        NotInList
    }
    #endregion
    #region class DxCriteriaVisitor : Vlastní konverzní třída pro (rekurzivní) převod DxFilter.CriteriaOperator do výsledných částí DxExpressionToken
    /// <summary>
    /// <see cref="DxCriteriaVisitor"/> : Rekurzivní konverzní třída pro vlastní převod <see cref="DxFilter.CriteriaOperator"/> do výsledných částí <see cref="DxExpressionToken"/>
    /// </summary>
    internal class DxCriteriaVisitor : DxFilter.ICriteriaVisitor<DxExpressionToken>, DxFilter.IClientCriteriaVisitor<DxExpressionToken>
    {
        #region Konstruktor a jazyk
        internal DxCriteriaVisitor(ConvertArgs args)
        {
            __Args = args;
            __ColumnDict = DxFilterConvertor.CreateColumnsDictionary(args.Columns, false);
        }
        private ConvertArgs __Args;
        private Dictionary<string, IFilterColumnInfo> __ColumnDict;

        /// <summary>
        /// Cílový jazyk konverze
        /// </summary>
        private DxExpressionLanguageType _Language { get { return __Args.Language; } }
        /// <summary>
        /// Sloupce
        /// </summary>
        private IEnumerable<IFilterColumnInfo> _Columns { get { return __Args.Columns; } }
        /// <summary>
        /// Obsahuje true, pokud máme dodané sloupce
        /// </summary>
        private bool _HasColumns { get { return __Args.HasColumns; } }
        /// <summary>
        /// Externí handler operátorů
        /// </summary>
        private DxConvertorCustomHandler _CustomHandler { get { return __Args.CustomHandler; } }
        /// <summary>
        /// Obsahuje true, pokud máme dodaný externí handler
        /// </summary>
        private bool _HasCustomHandler { get { return __Args.HasCustomHandler; } }
        #endregion
        #region Sloupce
        /// <summary>
        /// Zkusí najít sloupec pro dané ID
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool _TryGetColumn(string columnId, out IFilterColumnInfo column)
        {
            if (!String.IsNullOrEmpty(columnId) && __ColumnDict.TryGetValue(columnId, out column)) return true;

            column = null;
            return false;
        }
        #endregion
        #region Visitors : jednotlivé typy operací
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.GroupOperator groupOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.RemoveEmptyItems, groupOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Group, groupOperator.OperatorType);
            return ConvertToPart(groupOperator, operation, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.BetweenOperator betweenOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, betweenOperator.TestExpression, betweenOperator.BeginExpression, betweenOperator.EndExpression);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            return ConvertToPart(betweenOperator, DxFilterOperationType.Between, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.BinaryOperator binaryOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, binaryOperator.LeftOperand, binaryOperator.RightOperand);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Binary, binaryOperator.OperatorType);
            return ConvertToPart(binaryOperator, operation, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.UnaryOperator unaryOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, unaryOperator.Operand);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            var operation = ConvertOperation(FamilyType.Unary, unaryOperator.OperatorType);
            return ConvertToPart(unaryOperator, operation, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.InOperator inOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.RemoveEmptyItems, inOperator.LeftOperand, inOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            return ConvertToPart(inOperator, DxFilterOperationType.In, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.FunctionOperator functionOperator)
        {
            var dxOperands = ConvertOperands(ConvertOperandsMode.StrictlyAllItems, functionOperator.Operands);
            if (dxOperands is null) return null;                                         // null reprezentuje stav, kdy daný fragment vynecháváme.

            // Která funkce to má být?
            var operation = DxFilterOperationType.None;

            if (functionOperator.OperatorType == DxFilter.FunctionOperatorType.Custom && dxOperands.Count > 0 && dxOperands[0].IsValueString)
            {   // Funkce může být i Custom, pak její vlastní název je přítomen v prvním operandu typu String:
                operation = ConvertOperation(FamilyType.Custom, dxOperands[0].ValueString);
                // První operand odeberu, protože obsahoval název operace => a tu jsme převedli do "operation", a poté by tam překážel (mezi operandy typu Sloupec, Hodnota atd):
                dxOperands.RemoveAt(0);
            }
            else
            {   // Standardní funkce z rodiny DevExpress:
                operation = ConvertOperation(FamilyType.Function, functionOperator.OperatorType);
            }

            return ConvertToPart(functionOperator, operation, dxOperands);
        }
        DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.OperandValue operandValue)
        {
            return DxExpressionToken.CreateValue(operandValue.Value);
        }
        DxExpressionToken DxFilter.IClientCriteriaVisitor<DxExpressionToken>.Visit(DxFilter.OperandProperty operandProperty)
        {
            // OperandProperty = název property, a ve smyslu filtru = název sloupce.
            // Pokusím se dohledat sloupec podle jeho ColumnId = PropertyName:
            string columnId = operandProperty.PropertyName;
            _TryGetColumn(columnId, out var column);
            return DxExpressionToken.CreateProperty(columnId, column);
        }
        DxExpressionToken DxFilter.IClientCriteriaVisitor<DxExpressionToken>.Visit(DxFilter.AggregateOperand aggregateOperand)
        {
            return DxExpressionToken.CreateText("aggregateoperand ");
        }
        DxExpressionToken DxFilter.IClientCriteriaVisitor<DxExpressionToken>.Visit(DxFilter.JoinOperand joinOperand)
        {
            return DxExpressionToken.CreateText("joinoperand ");
        }
        #endregion
        #region TADY JE  ♥  KONVERTORU  :  Vlastní konvertor všech operací (funkce, operace, porovnání...), s využitím __CustomHandler
        /// <summary>
        /// Vlastní  ♥  celého convertoru !!
        /// <para/>
        /// dostává typ operace <paramref name="operation"/> a veškeré jeho operátory již zpracované do pole <paramref name="operands"/>.
        /// Dostává i výchozí operaci (<paramref name="filterOperator"/> - ale jeho data jsou již zpracovaná a operátor v podstatě není potřeba);
        /// <para/>
        /// V této metodě se nejprve řeší Custom handler = externí metoda, která může danou operaci řešit specificky. 
        /// Víceméně je Custom handler používán pro řešení sloupců s editačním stylem, pro které řeší zpětnou konverzi z DisplayValue (výraz v SQL dotazu a Display texty zobrazené uživateli) 
        /// na CodeValue (vstupní sloupec s daty v databázi a Code hodnoty = bez konverzí).
        /// Tato metoda je typicky umístěna v prostoru volajícího, protože tam ví, jaké sloupce a jaké CodeTable se používají.
        /// </summary>
        /// <param name="filterOperator">Vstupující operátor, data jsou ale už dost předzpracována a <paramref name="filterOperator"/> v podstatě není třeba</param>
        /// <param name="operation"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private DxExpressionToken ConvertToPart(DxFilter.CriteriaOperator filterOperator, DxFilterOperationType operation, List<DxExpressionToken> operands)
        {
            if (_HasCustomHandler || _HasColumns)
            {   // Custom handler nebo sloupce a jejich interní handler:
                var args = new DxConvertorCustomArgs(_Language, operation, operands);
                if (_HasColumns)
                    DxFilterConvertor.ConvertRowFilterPartForColumns(args);
                if (_HasCustomHandler)
                    _CustomHandler(filterOperator, args);
                if (args.Skip) return null;
                if (args.CustomResult != null) return args.CustomResult;
                operation = args.Operation;
                operands = args.Operands;
            }

            switch (this._Language)
            {
                case DxExpressionLanguageType.MsSqlDatabase:
                case DxExpressionLanguageType.Default:
                    return ConvertToPartMsSqlDatabase(filterOperator, operation, operands);
                case DxExpressionLanguageType.SystemDataFilter:
                    return ConvertToPartMsSqlDatabase(filterOperator, operation, operands);
            }

            throw new NotImplementedException($"DxFilterConvertor.ConvertToPart does not implement language: '{this._Language}'");
        }
        /// <summary>
        /// Fyzická konverze dodaného operátoru, konkrétní operace a operandů, do jazyka MS SQL DATABASE
        /// </summary>
        /// <param name="filterOperator"></param>
        /// <param name="operation"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private DxExpressionToken ConvertToPartMsSqlDatabase(DxFilter.CriteriaOperator filterOperator, DxFilterOperationType operation, List<DxExpressionToken> operands)
        {
            // Viz wiki:

            //  DxFilter:  https://docs.devexpress.com/CoreLibraries/DevExpress.Data.Filtering.FunctionOperatorType
            //  Numeric:   https://learn.microsoft.com/en-us/sql/t-sql/functions/mathematical-functions-transact-sql?view=sql-server-ver17
            //  String:    https://learn.microsoft.com/en-us/sql/t-sql/functions/string-functions-transact-sql?view=sql-server-ver17
            //  DateTime:  https://learn.microsoft.com/en-us/sql/t-sql/functions/date-and-time-data-types-and-functions-transact-sql?view=sql-server-ver17

            //  System.Data:  https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-data-datacolumn-expression

            // Řádkový filtr se bude opírat o aktuální čas na serveru, beztak to tak dělal i dříve...
            var now = DateTime.Now;
            DateTime dateBegin, dateEnd;
            int year;

            int count = operands?.Count ?? -1;
            switch (operation)
            {
                #region Group: And, Or
                case DxFilterOperationType.Group_And:
                case DxFilterOperationType.Group_Or:
                    // Žádný prvek: vynecháme;
                    // Jediný prvek: nepotřebuje závorky ani delimiter:
                    if (count <= 0) return null;
                    if (count == 1) return operands[0];

                    // Výsledek: pokud je v něm více než 1 prvek, pak jej ozávorkujeme :              (a < 1 or b > 10)   
                    //   aby navazující vyšší grupa (např. AND) byla napojena validně  :   c = 10 and (a < 1 or b > 10)
                    //   kdežto bez závorek by to dopadlo špatně                       :   c = 10 and  a < 1 or b > 10
                    //   ....  protože AND mívá přednost, takže význam by byl          :  (c = 10 and a < 1) or b > 10 
                    string delimiter = (operation == DxFilterOperationType.Group_And ? " and " : (operation == DxFilterOperationType.Group_Or ? " or " : ", "));
                    return DxExpressionToken.CreateFrom("(", DxExpressionToken.CreateDelimited(delimiter, operands), ")");

                #endregion
                #region Between
                case DxFilterOperationType.Between:
                    //    Operand.0  between  Operand.1  and  Operand.2
                    checkCount(3);
                    return DxExpressionToken.CreateFrom(operands[0], " between ", operands[1], " and ", operands[2]);
                #endregion
                #region Binary: Equal, Greater, Less, Modulo, Multiply...
                case DxFilterOperationType.Binary_Equal:
                case DxFilterOperationType.Binary_NotEqual:
                case DxFilterOperationType.Binary_Greater:
                case DxFilterOperationType.Binary_Less:
                case DxFilterOperationType.Binary_LessOrEqual:
                case DxFilterOperationType.Binary_GreaterOrEqual:
                case DxFilterOperationType.Binary_Like:
                case DxFilterOperationType.Binary_BitwiseAnd:
                case DxFilterOperationType.Binary_BitwiseOr:
                case DxFilterOperationType.Binary_BitwiseXor:
                case DxFilterOperationType.Binary_Divide:
                case DxFilterOperationType.Binary_Modulo:
                case DxFilterOperationType.Binary_Multiply:
                case DxFilterOperationType.Binary_Plus:
                case DxFilterOperationType.Binary_Minus:
                    // Všechny binární vyřešíme najednou:
                    checkCount(2);
                    return DxExpressionToken.CreateFrom(operands[0], getBinaryOperatorText(operation), operands[1]);
                #endregion
                #region Unary: Not, IsNull, ...
                case DxFilterOperationType.Unary_BitwiseNot:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom(" ~", operands[0]);
                case DxFilterOperationType.Unary_Plus:
                    checkCount(1);
                    return operands[0];
                case DxFilterOperationType.Unary_Minus:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom(" -(", operands[0], ")");
                case DxFilterOperationType.Unary_Not:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("not (", operands[0], ")");
                case DxFilterOperationType.Unary_IsNull:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom(operands[0], " is null");
                #endregion
                #region In list
                case DxFilterOperationType.In:
                    if (count < 2) return null;                                          // null reprezentuje stav, kdy daný fragment vynecháváme.
                    if (count == 2) return DxExpressionToken.CreateFrom(operands[0], " = ", operands[1]);                                                       // Sloupec in (123)   převedeme na   Sloupec = 123
                    if (count > 2) return DxExpressionToken.CreateFrom(operands[0], " in (", DxExpressionToken.CreateDelimited(",", operands.Skip(1)), ")");     // Sloupec in (operandy počínaje [1] oddělené , delimiterem)
                    failCount("1 or more");
                    break;
                #endregion
                #region Function - Custom: None, Custom, CustomNonDeterministic
                case DxFilterOperationType.Function_None:
                case DxFilterOperationType.Function_Custom:
                case DxFilterOperationType.Function_CustomNonDeterministic:
                    break;
                #endregion
                #region Function - Logical: Iif, IsNull
                case DxFilterOperationType.Function_Iif:
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
                    var iifPart = DxExpressionToken.CreateFrom("(case ");                           // (case when ... 
                    for (int i = 0; i < count; i += 2)
                        iifPart.AddRange("when ", operands[i], " then ", operands[i + 1], " ");    //   ... [Name] = 'Bob' then 1 ...       přičemž operandItems[i] je logický: "[Name] = 'Bob'" a operandItems[i + 1] je hodnota: "1"
                    iifPart.AddRange("else ", operands[count - 1], " end)");                       //   ... else 0 end)
                    return iifPart;
                case DxFilterOperationType.Function_IsNull:
                    // Compares the first operand with the NULL value. This function requires one or two operands of the CriteriaOperator class. 
                    // The returned value depends on the number of arguments (one or two arguments).
                    //  True / False: If a single operand is passed, the function returns True if the operand is null; otherwise, False.
                    //  Value1 / Value2: If two operands are passed, the function returns the first operand if it is not set to NULL; otherwise, the second operand is returned.
                    if (count == 1) return DxExpressionToken.CreateFrom(operands[0], " is null");                                 // [datum_akce] is null
                    if (count == 2) return DxExpressionToken.CreateFrom("isnull(", operands[0], ", ", operands[1], ")");          // isnull([datum_akce], [datum_podani])
                    failCount("1 or 2");
                    break;
                case DxFilterOperationType.Function_IsNullOrEmpty:
                    // Returns True if the specified value is null or an empty string. Otherwise, returns False.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(", operands[0], " is null or len(", operands[0], ") = 0)");
                #endregion
                #region Function - String 1: Trim. Len, Substring, Upper, Lower, Concat, ...
                case DxFilterOperationType.Function_Trim:
                    //Returns a string that is a copy of the specified string with all white-space characters removed from the start and end of the specified string.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("trim(", operands[0], ")");
                case DxFilterOperationType.Function_Len:
                    // Returns the length of the string specified by an operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("len(", operands[0], ")");
                case DxFilterOperationType.Function_Substring:
                    // Returns a substring from the specified string. This function requires two or three operands.
                    // If two operands are passed, the substring starts from the beginning of the specified string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    // If three operands are passed, a substring starts from the specified position in the source string.The operands are:
                    //   1 - the source string.
                    //   2 - an integer that specifies the zero - based position at which the substring starts.
                    //   3 - an integer that specifies the length of the substring.
                    if (count == 2)
                        return DxExpressionToken.CreateFrom("substring(", operands[0], ",", intAsText(operands[1], 1), ",9999)");                           // substring([poznamka], 41, 9999) : DevExpress umožňuje 2 argumenty (string, begin), ale SQL server chce povinně 3, kde třetí = délka
                    if (count == 3)
                        return DxExpressionToken.CreateFrom("substring(", operands[0], ",", intAsText(operands[1], 1), ",", intAsText(operands[2]), ")");   // substring([poznamka], (40+1), 25)   : DevExpress má počátek substringu zero-based ("integer that specifies the zero-based position at which the substring starts."), ale SQL server má base 1
                    failCount("2 or 3");
                    break;
                case DxFilterOperationType.Function_Upper:
                    // Converts all characters in a string operand to uppercase in an invariant culture.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("upper(", operands[0], ")");
                case DxFilterOperationType.Function_Lower:
                    // Converts all characters in a string operand to lowercase in an invariant culture.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("lower(", operands[0], ")");
                case DxFilterOperationType.Function_Concat:
                    // Concatenates the specified strings.
                    // SQL server pro funkci CONCAT vyžaduje nejméně dva parametry; proto pro méně operandů provádím konverze jinak:
                    if (count <= 0) return DxExpressionToken.CreateText("''");                                                    // concat()                            vrátí ''
                    if (count == 1) return operands[0];                                                                          // concat([nazev])                     vrátí [nazev]
                    return DxExpressionToken.CreateFrom("concat(", DxExpressionToken.CreateDelimited(",", operands), ")");         // concat([nazev1], ',', [nazev2])     vrátí concat([nazev1], ',', [nazev2])   = to je SQL validní
                case DxFilterOperationType.Function_Ascii:
                    // Returns the ASCII code of the first character in a string operand.
                    // If the argument is an empty string, the null value is returned.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("ascii(", operands[0], ")");
                case DxFilterOperationType.Function_Char:
                    // Converts a numeric operand to a Unicode character.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("char(", operands[0], ")");
                case DxFilterOperationType.Function_ToStr:
                    // Returns a string representation of the specified value or property.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as nvarchar(max))");
                case DxFilterOperationType.Function_Replace:
                    // Returns a new string in which all occurrences of one specified string (string1) in another string (string2) are replaced with the specified string (string3).
                    // The operands are:
                    //  1 - the string in which replacements are made.
                    //  2 - the string to be replaced.
                    //  3 - the string to replace all occurrences of the specified string.
                    //  ... Taky správně chápete, jaký význam má string1 a string2 ?
                    // MS SQL : REPLACE ( string_expression , string_pattern , string_replacement )  
                    checkCount(3);
                    return DxExpressionToken.CreateFrom("replace(", operands[0], ", ", operands[1], ", ", operands[2], ")");
                case DxFilterOperationType.Function_Reverse:
                    // Returns a string in which the character order of a specified string is reversed.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("reverse(", operands[0], ")");
                case DxFilterOperationType.Function_Insert:
                    // Returns a new string in which a specified string is inserted at a specified index position into another specified string.
                    // The operands are:
                    //  1 - the string into which another string should be inserted.              například   [nazev_subjektu]
                    //  2 - the zero-based index position of the insertion.                       například   3
                    //  3 - the string to insert.                                                 například   'ABCD'
                    // GithubCopilot mi navrhnul:
                    // MS SQL : (case when ([nazev_subjektu] is null) then null when (len([nazev_subjektu]) <= 3) then ([nazev_subjektu] + 'ABCD') else (left([nazev_subjektu], 3) + 'ABCD' + right([nazev_subjektu], len([nazev_subjektu]) - 3)) end)
                    //          return DxExpressionToken.CreateFrom("(case when (", operands[0], " is null) then null when (len(", operands[0], ") <= ", intAsText(operands[1]), ") then (", operands[0], " + ", operands[2], ") else (left(", operands[0], ", ", intAsText(operands[1]), ") + ", operands[2], " + right(", operands[0], ", len(", operands[0], ") - ", intAsText(operands[1]), ")) end)");
                    // MS SQL : STUFF ( character_expression , start , length , replace_with_expression )
                    // MS SQL : (case when ([nazev_subjektu] is null) then null when (len([nazev_subjektu]) <= 3) then ([nazev_subjektu] + 'ABCD') else stuff([nazev_subjektu], 4, 0, 'ABCD') end)
                    checkCount(3);
                    return DxExpressionToken.CreateFrom("(case when (", operands[0], " is null) then null when (len(", operands[0], ") <= ", intAsText(operands[1]), ") then (", operands[0], " + ", operands[2], ") else stuff(", operands[0], ", ", intAsText(operands[1], 1), ", 0, ", operands[2], ") end)");
                case DxFilterOperationType.Function_CharIndex:
                    // Returns the index of the first occurrence of a specified string within another string.
                    // The operands are:
                    //  1 - a string that you want to find in another string.
                    //  2 - a string that contains the string you are searching for.
                    //  3 - (optional) an integer that specifies the zero-based index at which the search starts. If this operand is not specified, the search begins from the start of the string.
                    //  4 - (optional) an integer that specifies the number of characters to examine, starting from the specified position. If this operand is not specified, the search continues until the end of the string.
                    //     This function performs a word search using the current culture. If a specified substring is found, the function returns its index. Otherwise, -1 is returned.
                    // MS SQL : CHARINDEX ( expressionToFind , expressionToSearch [ , start_location ] )  
                    if (count == 2) return DxExpressionToken.CreateFrom("charindex(", operands[0], ", ", operands[1], ")");
                    if (count == 3) return DxExpressionToken.CreateFrom("charindex(", operands[0], ", ", operands[1], ", ", operands[2], ")");
                    failCount("2 or 3");
                    break;
                case DxFilterOperationType.Function_Remove:
                    // Returns a new string with the specified number of characters in the specified string removed, starting at the specified position.
                    // The operands are:
                    //  1 - the string that needs to be shortened.
                    //  2 - the zero-based index at which character removal starts.
                    //  3 - (optional) an integer that specifies the number of characters to remove, starting at the specified position. If this operand is not specified, all characters between the starting position and the end of the string are removed.
                    // MS SQL : STUFF ( character_expression , start , length , replace_with_expression )
                    //        (case when ([nazev_subjektu] is null) then null when (len([nazev_subjektu]) <= 3) then ([nazev_subjektu]) else stuff([nazev_subjektu], 4, 9999, '')  end)
                    //        (case when ([nazev_subjektu] is null) then null when (len([nazev_subjektu]) <= 3) then ([nazev_subjektu]) else stuff([nazev_subjektu], 4, 2, '')  end)
                    if (count == 2) return DxExpressionToken.CreateFrom("(case when (", operands[0], " is null) then null when (len(", operands[0], ") <= ", intAsText(operands[1]), ") then (", operands[0], ") else stuff(", operands[0], ", ", intAsText(operands[1], 1), ", 9999, '') end)");
                    if (count == 3) return DxExpressionToken.CreateFrom("(case when (", operands[0], " is null) then null when (len(", operands[0], ") <= ", intAsText(operands[1]), ") then (", operands[0], ") else stuff(", operands[0], ", ", intAsText(operands[1], 1), ", ", intAsText(operands[2]), ", '') end)");
                    failCount("2 or 3");
                    break;
                #endregion
                #region Function - Mathematics: Abs, Sqrt, Sin, Exp, Log, Pow, Celinig, Round, ...
                case DxFilterOperationType.Function_Abs:
                    // Returns the absolute value of a numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("abs(", operands[0], ")");
                case DxFilterOperationType.Function_Sqr:
                    // Returns the square root of a specified numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("sqrt(", operands[0], ")");
                case DxFilterOperationType.Function_Cos:
                    // Returns the cosine of the numeric operand, in radians.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cos(", operands[0], ")");
                case DxFilterOperationType.Function_Sin:
                    // Returns the sine of the numeric operand, in radians.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("sin(", operands[0], ")");
                case DxFilterOperationType.Function_Atn:
                    // Returns the arctangent (the inverse tangent function) of the numeric operand. The arctangent is the angle in the range -π/2 to π/2 radians, whose tangent is the numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("atan(", operands[0], ")");
                case DxFilterOperationType.Function_Exp:
                    // Returns the number e raised to the power specified by a numeric operand.
                    //   If the specified operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Exp function reverses the FunctionOperatorType.Log function. Use the FunctionOperatorType.Power operand to calculate powers of other bases.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("exp(", operands[0], ")");
                case DxFilterOperationType.Function_Log:
                    // Returns the logarithm of the specified numeric operand. The return value depends upon the number of operands.
                    // If one operand is passed, the function returns the natural(base e) logarithm of a specified operand.
                    // If two operands are passed, the function returns the logarithm of the specified operand to the specified base.The operands are:
                    //   1 - a number whose logarithm is to be calculated.
                    //   2 - the base of the logarithm.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Log function reverses the FunctionOperatorType.Exp function. To calculate the base - 10 logarithm, use the FunctionOperatorType.Log10 function.
                    if (count == 1) return DxExpressionToken.CreateFrom("log(", operands[0], ")");
                    if (count == 2) return DxExpressionToken.CreateFrom("log(", operands[0], ",", operands[1], ")");
                    failCount("1 or 2");
                    break;
                case DxFilterOperationType.Function_Rnd:
                    // Returns a random number greater than or equal to 0.0, and less than 1.0.
                    return DxExpressionToken.CreateText("rand()");
                case DxFilterOperationType.Function_Tan:
                    // Returns the tangent of the specified numeric operand that is an angle in radians.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("tan(", operands[0], ")");
                case DxFilterOperationType.Function_Power:
                    // Returns a specified numeric operand raised to a specified power.
                    // The operands are:
                    //  1 - the base number.
                    //  2 - the exponent to which the base number is raised.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Power function reverses the FunctionOperatorType.Log or FunctionOperatorType.Log10 function. Use the FunctionOperatorType.Exp operand to calculate powers of the number e.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("power(", operands[0], ",", operands[1], ")");
                case DxFilterOperationType.Function_Sign:
                    // Returns an integer that indicates the sign of a number. The function returns 1 for positive numbers, -1 for negative numbers, and 0 (zero) if a number is equal to zero.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("sign(", operands[0], ")");
                case DxFilterOperationType.Function_Round:
                    // Rounds a specified numeric operand to the nearest integer or to a specified number of fractional digits.
                    // The operands are:
                    // 1 - a value to round.
                    // 2 - (optional)the number of decimal places to which to round. 0 indicates that the first operand is rounded to the nearest integer.
                    if (count == 1) return DxExpressionToken.CreateFrom("round(", operands[0], ", 0)");
                    if (count == 2) return DxExpressionToken.CreateFrom("round(", operands[0], ",", intAsText(operands[1]), ")");
                    failCount("1 or 2");
                    break;
                case DxFilterOperationType.Function_Ceiling:
                    // Returns the smallest integral value greater than or equal to the specified numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("ceiling(", operands[0], ")");
                case DxFilterOperationType.Function_Floor:
                    // Returns the largest integral value less than or equal to the specified numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("floor(", operands[0], ")");
                case DxFilterOperationType.Function_Max:
                    // Returns the larger of two numeric values.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(case when ", operands[0], " > ", operands[1], " then ", operands[0], " else ", operands[1], " end)");   // (case when pocet1 > pocet2 then pocet1 else pocet2 end)
                case DxFilterOperationType.Function_Min:
                    // Returns the smaller of two numeric values.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(case when ", operands[0], " < ", operands[1], " then ", operands[0], " else ", operands[1], " end)");   // (case when pocet1 < pocet2 then pocet1 else pocet2 end)
                case DxFilterOperationType.Function_Acos:
                    // Returns the arccosine of the numeric operand. The arccosine is the angle in the range 0 (zero) to π radians, whose cosine is the numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("acos(", operands[0], ")");
                case DxFilterOperationType.Function_Asin:
                    // Returns the arcsine of the numeric operand. The arcsine is the angle in the range -π/2 to π/2 radians, whose sine is the numeric operand.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("asin(", operands[0], ")");
                case DxFilterOperationType.Function_Atn2:
                    // Returns the arctangent (the inverse tangent function) of the quotient of the two specified numeric operands. The arctangent is the angle in the range -π/2 to π/2 radians.
                    // The operands are:
                    //  1 - the y coordinate of a point in Cartesian coordinates (x, y).
                    //  2 - the x coordinate of a point in Cartesian coordinates (x, y).
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("atn2(", operands[0], ",", operands[1], ")");
                case DxFilterOperationType.Function_BigMul:
                    // Calculates the full product of two integer operands.
                    // MS SQL : (cast(op1 as bigint) * cast(op2 as bigint)) 
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(cast(", operands[0], " as bigint) * cast(", operands[1], " as bigint))");
                case DxFilterOperationType.Function_Cosh:
                    // Returns the hyperbolic cosine of the numeric operand, in radians.

                    // MS SQL : asi nemá ???

                    break;
                case DxFilterOperationType.Function_Log10:
                    // Returns the base 10 logarithm of the specified numeric operand.
                    // If the operand cannot be converted to Double, the NotSupportedException is thrown.
                    // The Log10 function reverses the FunctionOperatorType.Power function. Use the FunctionOperatorType.Log 
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("log10(", operands[0], ")");
                case DxFilterOperationType.Function_Sinh:
                    // Returns the hyperbolic sine of the numeric operand, in radians.

                    // MS SQL : asi nemá ???

                    break;
                case DxFilterOperationType.Function_Tanh:
                    // Returns the hyperbolic tangent of a specified numeric operand that is an angle in radians.

                    // MS SQL : asi nemá ???

                    break;
                #endregion
                #region Function - String 2: PadLeft, StartsWith, Contains, ToInt, ToDecimal, ...
                case DxFilterOperationType.Function_PadLeft:
                    // Returns a new string that pads the character in the specified string on the left with a specified Unicode character, for a specified total length.
                    // The operands are:
                    //  1 - a string to be padded.
                    //  2 - the total number of characters in the resulting string, including padding characters.
                    //  3 - (optional) a Unicode padding character. If not specified, the space character is used for padding. If a string is passed as this operand, its first character is used for padding.
                    // MS SQL : right(replicate('=', 80) + nazev_subjektu, 80) as padleft     zachovává NULL hodnotu
                    if (count == 2) return DxExpressionToken.CreateFrom("right(replicate(' ', ", intAsText(operands[1]), ") + ", operands[0], ", ", intAsText(operands[1]), ")");
                    if (count == 3) return DxExpressionToken.CreateFrom("right(replicate(", operands[2], ", ", intAsText(operands[1]), ") + ", operands[0], ", ", intAsText(operands[1]), ")");
                    failCount("2 or 3");
                    break;
                case DxFilterOperationType.Function_PadRight:
                    // Returns a new string of a specified length in which the end of a specified string is padded with spaces or with a specified Unicode character.
                    // The operands are:
                    //  1 - a string to be padded.
                    //  2 - the total number of characters in the resulting string, including padding characters.
                    //  3 - (optional) a Unicode padding character. If not specified, the space character is used for padding. If a string is passed as this operand, its first character is used for padding.
                    // MS SQL : left(nazev_subjektu + replicate('=', 80), 80) as padright     zachovává NULL hodnotu
                    if (count == 2) return DxExpressionToken.CreateFrom("left(", operands[0], " + replicate(' ', ", intAsText(operands[1]), "), ", intAsText(operands[1]), ")");
                    if (count == 3) return DxExpressionToken.CreateFrom("left(", operands[0], " + replicate(", operands[2], ", ", intAsText(operands[1]), "), ", intAsText(operands[1]), ")");
                    failCount("2 or 3");
                    break;
                case DxFilterOperationType.Function_StartsWith:
                    checkCount(2);
                    return DxExpressionToken.CreateFrom(operands[0], " like ", stringAsValue(operands[1], null, "%"));            // [nazev] like 'adr%'     nebo    [nazev] like (Funkce(x,y) + '%')
                case DxFilterOperationType.Function_EndsWith:
                    checkCount(2);
                    return DxExpressionToken.CreateFrom(operands[0], " like ", stringAsValue(operands[1], "%", null));            // [nazev] like '%adr'     nebo    [nazev] like ('%' + Funkce(x,y))
                case DxFilterOperationType.Function_Contains:
                    checkCount(2);
                    return DxExpressionToken.CreateFrom(operands[0], " like ", stringAsValue(operands[1], "%", "%"));             // [nazev] like '%adr%'    nebo    [nazev] like ('%' + Funkce(x,y) + '%')
                case DxFilterOperationType.Function_ToInt:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as int)");
                case DxFilterOperationType.Function_ToLong:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as bigint)");
                case DxFilterOperationType.Function_ToFloat:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                case DxFilterOperationType.Function_ToDouble:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                case DxFilterOperationType.Function_ToDecimal:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as decimal(19,6))");
                #endregion
                #region Function - DateTime 1: LocalDateTime
                case DxFilterOperationType.Function_LocalDateTimeThisYear:
                    // Returns the DateTime value with the date part that is the first day of the current year, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year, 1, 1, 0, 0, 0);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-01-01 00:00:00.000', 121)
                case DxFilterOperationType.Function_LocalDateTimeThisMonth:
                    // Returns the DateTime value with the date part that is the first day of the current month, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-08-01 00:00:00.000', 121)
                case DxFilterOperationType.Function_LocalDateTimeLastWeek:
                    // Returns the DateTime value that has the date part that is 7 days before the start of the current week, and the time part of 00:00:00.
                    dateBegin = getWeekBegin(now).AddDays(-7);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-07-28 00:00:00.000', 121)        pokud dnes je 2025-08-05 (úterý), pak aktuální pondělí je 2025-08-04  a minulé pondělí je 2025-07-28 !
                case DxFilterOperationType.Function_LocalDateTimeThisWeek:
                    // Returns the DateTime value with the date part that is the first day of the current week, and the time part of 00:00:00.
                    dateBegin = getWeekBegin(now);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-08-04 00:00:00.000', 121)        pokud dnes je 2025-08-05 (úterý), pak aktuální pondělí je 2025-08-04
                case DxFilterOperationType.Function_LocalDateTimeYesterday:
                    // Returns the DateTime value with the date part that is the previous day, and the time part of 00:00:00.
                    dateBegin = now.Date.AddDays(-1);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-08-04 00:00:00.000', 121)        pokud dnes je 2025-08-05 (úterý), pak včerejšek je 2025-08-04
                case DxFilterOperationType.Function_LocalDateTimeToday:
                    // Returns the DateTime value with the date part that is the start of the current day, and the time part of 00:00:00.
                    dateBegin = now.Date;
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-08-05 00:00:00.000', 121)        pokud dnes je 2025-08-05 (úterý), pak to je dnešek (bez času)
                case DxFilterOperationType.Function_LocalDateTimeNow:
                    // Returns the DateTime value that is the current moment in time.
                    dateBegin = now;
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));                           // convert(datetime, '2025-08-05 09:59:26.453', 121)
                case DxFilterOperationType.Function_LocalDateTimeTomorrow:
                    // Returns the DateTime value with the date part that is the next day, and the time part of 00:00:00.
                    dateBegin = now.Date.AddDays(1);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeDayAfterTomorrow:
                    // Returns the DateTime value that has the date part that is two days after the current date, and the time part of 00:00:00.
                    dateBegin = now.Date.AddDays(2);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeNextWeek:
                    // Returns the DateTime value that has the date part that is 7 days after the start of the current week, and the time part of 00:00:00.
                    dateBegin = getWeekBegin(now).AddDays(7);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeTwoWeeksAway:
                    // Returns the DateTime value with the date part that is the first day of the week after the next week, and the time part of 00:00:00.
                    dateBegin = getWeekBegin(now).AddDays(14);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeNextMonth:
                    // Returns the DateTime value that has the date part that is the first day of the next month, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(1);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeNextYear:
                    // Returns the DateTime value with the date part that corresponds to the first day of the next year, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year + 1, 1, 1, 0, 0, 0);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeTwoMonthsAway:
                    // Returns the DateTime value with the date part that is the first day of the month after the next month, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year, now.Month, 1, 0, 0, 0).AddMonths(2);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeTwoYearsAway:
                    // Returns the DateTime value with the date part that is the first day of the year after the next year, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year + 2, 1, 1, 0, 0, 0);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeLastMonth:
                    // Returns the DateTime value that has the date part that is one month before the current date, and the time part of 00:00:00.
                    dateBegin = now.AddMonths(-1).Date;
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeLastYear:
                    // Returns the DateTime value that has the date part that is the first day of the previous year, and the time part of 00:00:00.
                    dateBegin = new DateTime(now.Year - 1, 1, 1, 0, 0, 0);
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                case DxFilterOperationType.Function_LocalDateTimeYearBeforeToday:
                    // Returns the DateTime value with the date part that is the date one year ago, and the time part of 00:00:00.
                    dateBegin = now.AddMonths(-12).Date;
                    return DxExpressionToken.CreateFrom(dateTimeAsText(dateBegin));
                #endregion
                #region Function - DateTime 2: IsOutlookInterval
                case DxFilterOperationType.Function_IsOutlookIntervalBeyondThisYear:
                    // The Boolean Is Beyond This Year operator for date/time values. Requires one argument.
                    // The operator is defined as follows: date >= First Day of Next Year
                    checkCount(1);
                    dateBegin = new DateTime(now.Year + 1, 1, 1);                        // Začátek příštího roku
                    return DxExpressionToken.CreateFrom(operands[0], " >= ", dateTimeAsText(dateBegin));            // [datum_akce] >= '2026-01-01 00:00:00'
                case DxFilterOperationType.Function_IsOutlookIntervalLaterThisYear:
                    // The Boolean Is Later This Year operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of Next Month <= date < First Day of Next Year
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, 1).AddMonths(1);       // 1. příštího měsíce
                    dateEnd = new DateTime(now.Year + 1, 1, 1);                          // 1.1. příštího roku
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalLaterThisMonth:
                    // The Boolean Is Later This Month operator for date/time values. Requires one argument.
                    // The operator is defined as follows: Last Day of Next Week < date < First Day of Next Month
                    //  ... tohle nechápu, ale budiž: ode dneška příští pondělí, od něj další pondělí ráno je Begin; a End je 1. dne dalšího měsíce:
                    checkCount(1);
                    dateBegin = getWeekBegin(now).AddDays(14);                           // Začátek přespříštího týdne
                    dateEnd = new DateTime(now.Year, now.Month, 1).AddMonths(1);         // 1. příštího měsíce
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalNextWeek:
                    // The Boolean Is Next Week operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of Next Week <= date <= Last Day of Next Week
                    checkCount(1);
                    dateBegin = getWeekBegin(now).AddDays(7);                            // Začátek příštího týdne
                    dateEnd = dateBegin.AddDays(7);                                      // Začátek přespříštího týdne
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalLaterThisWeek:
                    // The Boolean Is Later This Week operator for date/time values. Requires one argument.
                    // The operator is defined as follows: Day After Tomorrow <= date < First Day of Next Week
                    //  ... tedy Begin není zítra, ale pozítří ráno; konec je před příští pondělí:
                    checkCount(1);
                    dateBegin = now.AddDays(2).Date;                                     // Začátek pozítřka
                    dateEnd = getWeekBegin(now).AddDays(7);                              // Začátek příštího týdne
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalTomorrow:
                    // The Boolean Is Tomorrow operator for date/time values. Requires one argument.
                    checkCount(1);
                    dateBegin = now.Date.AddDays(1).Date;                                // Zítra ráno
                    dateEnd = dateBegin.AddDays(1).Date;                                 // Pozítří ráno
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalToday:
                    // The Boolean Is Today operator for date/time values. Requires one argument.
                    checkCount(1);
                    dateBegin = now.Date;                                                // Dnes ráno
                    dateEnd = dateBegin.AddDays(1).Date;                                 // Zítra
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalYesterday:
                    // The Boolean Is Yesterday operator for date/time values. Requires one argument.
                    checkCount(1);
                    dateBegin = now.Date.AddDays(-1).Date;                               // Včera ráno
                    dateEnd = dateBegin.AddDays(1).Date;                                 // Dnes ráno
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalEarlierThisWeek:
                    // The Boolean Is Earlier This Week operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of This Week <= date < Yesterday
                    //  ... tedy konec není dnes ráno, ale už včera ráno!
                    checkCount(1);
                    dateBegin = getWeekBegin(now);                                       // Začátek aktuálního týdne
                    dateEnd = now.Date.AddDays(-1).Date;                                 // Včera ráno
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalLastWeek:
                    // The Boolean Is Last Week operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of Last Week <= date < First Day of This Week
                    checkCount(1);
                    dateEnd = getWeekBegin(now);                                         // Začátek aktuálního týdne = konec intervalu
                    dateBegin = dateEnd.AddDays(-7).Date;                                // Začátek minulého týdne = začátek intervalu
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalEarlierThisMonth:
                    // The Boolean Is Earlier This Month operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of This Month <= date < First Day of Last Week
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, 1);                    // Začátek tohoto měsíce
                    dateEnd = getWeekBegin(now).AddDays(-7).Date;                        // Pondělí v minulém týdnu
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalEarlierThisYear:
                    // The Boolean Is Earlier This Year operator for date/time values. Requires one argument.
                    // The operator is defined as follows: First Day of This Year <= date < First Day of This Month
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, 1, 1);                            // Začátek tohoto roku
                    dateEnd = new DateTime(now.Year, now.Month, 1);                      // Začátek tohoto měsíce
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsOutlookIntervalPriorThisYear:
                    // The Boolean Is Prior This Year operator for date/time values. Requires one argument.
                    // The operator is defined as follows: date < First Day of This Year
                    checkCount(1);
                    dateEnd = new DateTime(now.Year, 1, 1);                              // Začátek tohoto roku
                    return DxExpressionToken.CreateFrom(operands[0], " < ", dateTimeAsText(dateEnd));            // [datum_akce] < '2025-01-01 00:00:00'
                #endregion
                #region Function - DateTime 3: Is... (IsThisWeek, IsLastYear, IsJanuary, ...)
                case DxFilterOperationType.Function_IsThisWeek:
                    // Tento týden:
                    checkCount(1);
                    dateBegin = getWeekBegin(now);                                       // Začátek aktuálního týdne
                    dateEnd = dateBegin.AddDays(7).Date;                                 // Začátek příštího týdne
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsThisMonth:
                    // Celý tento měsíc:
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, 1).Date;               // Začátek aktuálního měsíce
                    dateEnd = dateBegin.AddMonths(1).Date;                               // Začátek příštího měsíce
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsThisYear:
                    // Celý tento rok:
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, 1, 1).Date;                       // Začátek aktuálního roku
                    dateEnd = new DateTime(now.Year + 1, 1, 1).Date;                     // Začátek příštího roku
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsNextMonth:
                    // Celý příští měsíc:
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, 1).AddMonths(1).Date;  // Začátek příštího měsíce
                    dateEnd = dateBegin.AddMonths(1).Date;                               // Začátek přespříštího měsíce
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsNextYear:
                    // Celý budoucí rok:
                    checkCount(1);
                    year = now.Year + 1;                                                 // Příští rok: pokud letos je 2025, pak year = 2026
                    return DxExpressionToken.CreateFrom("year(", operands[0], ") = " + year.ToString());       // Rok(datum_akce) = 2026
                case DxFilterOperationType.Function_IsLastMonth:
                    // Celý minulý měsíc:
                    checkCount(1);
                    dateEnd = new DateTime(now.Year, now.Month, 1);                      // Začátek aktuálního měsíce
                    dateBegin = dateEnd.AddMonths(-1).Date;                              // Začátek minulého měsíce
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsLastYear:
                    // Celý minulý rok:
                    checkCount(1);
                    year = now.Year - 1;                                                 // Minulý rok: pokud letos je 2025, pak year = 2024
                    return DxExpressionToken.CreateFrom("year(", operands[0], ") = " + year.ToString());       // Rok(datum_akce) = 2024
                case DxFilterOperationType.Function_IsYearToDate:
                    // Od 1.1. tohoto roku do dnešního večera:
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, 1, 1);                            // Začátek aktuálního roku
                    dateEnd = now.AddDays(1).Date;                                       // Začátek zítřka
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsSameDay:
                    // Určitý den (ve sloupci [0]) je stejný den, jako je zadán v parametru [1]
                    checkCount(2);
                    // Prostě porovnám Date část obou výrazů:
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as date) = cast(", operands[1], " as date)");       // cast(datum0 as date) = cast(datum1 as date)
                case DxFilterOperationType.Function_InRange:
                    // Obecně v rozmezí [ od včetně ... do mimo ): jak datumy, tak čísla...
                    checkCount(3);
                    return DxExpressionToken.CreateFrom("(", operands[0], " >= ", operands[1], " and ", operands[0], " < ", operands[2], ")");
                case DxFilterOperationType.Function_InDateRange:
                    // Den v rozmezí od - do, ale zadaný v proměnných Od-Do:
                    checkCount(3);
                    return DxExpressionToken.CreateFrom("(", operands[0], " >= cast(", operands[1], " as date) and ", operands[0], " < dateadd(day, 1, cast(", operands[2], " as date)))");
                case DxFilterOperationType.Function_IsJanuary:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 1");
                case DxFilterOperationType.Function_IsFebruary:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 2");
                case DxFilterOperationType.Function_IsMarch:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 3");
                case DxFilterOperationType.Function_IsApril:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 4");
                case DxFilterOperationType.Function_IsMay:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 5");
                case DxFilterOperationType.Function_IsJune:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 6");
                case DxFilterOperationType.Function_IsJuly:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 7");
                case DxFilterOperationType.Function_IsAugust:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 8");
                case DxFilterOperationType.Function_IsSeptember:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 9");
                case DxFilterOperationType.Function_IsOctober:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 10");
                case DxFilterOperationType.Function_IsNovember:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 11");
                case DxFilterOperationType.Function_IsDecember:
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("month(", operands[0], ") = 12");
                #endregion
                #region Function - DateTime 4: DateDiff...
                case DxFilterOperationType.Function_DateDiffTick:
                    // Returns the number of tick boundaries between the specified dates.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(microsecond, ", operands[0], ",", operands[1], ")");  // DATEDIFF ( datepart , startdate , enddate );  microsecond
                case DxFilterOperationType.Function_DateDiffSecond:
                    // Returns the number of second boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(second, ", operands[0], ",", operands[1], ")");       // DATEDIFF ( datepart , startdate , enddate );  second
                case DxFilterOperationType.Function_DateDiffMilliSecond:
                    // Returns the number of millisecond boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(millisecond, ", operands[0], ",", operands[1], ")");  // DATEDIFF ( datepart , startdate , enddate );  millisecond
                case DxFilterOperationType.Function_DateDiffMinute:
                    // Returns the number of minute boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(minute, ", operands[0], ",", operands[1], ")");       // DATEDIFF ( datepart , startdate , enddate );  minute
                case DxFilterOperationType.Function_DateDiffHour:
                    // Returns the number of hour boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(hour, ", operands[0], ",", operands[1], ")");         // DATEDIFF ( datepart , startdate , enddate );  hour
                case DxFilterOperationType.Function_DateDiffDay:
                    // Returns the number of day boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(day, ", operands[0], ",", operands[1], ")");          // DATEDIFF ( datepart , startdate , enddate );  day
                case DxFilterOperationType.Function_DateDiffMonth:
                    // Returns the number of month boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(month, ", operands[0], ",", operands[1], ")");        // DATEDIFF ( datepart , startdate , enddate );  month
                case DxFilterOperationType.Function_DateDiffYear:
                    // Returns the number of year boundaries between the specified dates/ times.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the DateTime value that is the end date.                    
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datediff(year, ", operands[0], ",", operands[1], ")");         // DATEDIFF ( datepart , startdate , enddate );  year
                #endregion
                #region Function - DateTime 5: GetPart...
                case DxFilterOperationType.Function_GetDate:
                    // Returns the date part of the specified date.
                    // The operand must be of the DateTime / DateOnly type.
                    // The return value is a DateTime object with the same date part where the time part is 00:00:00, or DateOnly. The return value type depends on the operand type.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("cast(", operands[0], " as date)");                             // cast(datum_akce as date)
                case DxFilterOperationType.Function_GetMilliSecond:
                    // Returns the milliseconds value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 0 and 999.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(millisecond, ", operands[0], ")");                    // DATEPART ( datepart , date ) ;  millisecond
                case DxFilterOperationType.Function_GetSecond:
                    // Returns the seconds value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 0 and 59.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(seconds, ", operands[0], ")");                        // DATEPART ( datepart , date ) ;  seconds
                case DxFilterOperationType.Function_GetMinute:
                    // Returns the minute value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 0 and 59.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(minute, ", operands[0], ")");                         // DATEPART ( datepart , date ) ;  minute
                case DxFilterOperationType.Function_GetHour:
                    // Returns the hour value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 0 and 23.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(hour, ", operands[0], ")");                           // DATEPART ( datepart , date ) ;  hour
                case DxFilterOperationType.Function_GetDay:
                    // Returns the day value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 1 and 31.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(day, ", operands[0], ")");                            // DATEPART ( datepart , date ) ;  day
                case DxFilterOperationType.Function_GetMonth:
                    // Returns the month value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer and depends on the current calendar.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(month, ", operands[0], ")");                         // DATEPART ( datepart , date ) ;  month
                case DxFilterOperationType.Function_GetYear:
                    // Returns the year value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 0 and 9999.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(year, ", operands[0], ")");                          // DATEPART ( datepart , date ) ;  year
                case DxFilterOperationType.Function_GetDayOfWeek:
                    // Returns the day of the week value in the specified date/time.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer value of the DayOfWeek enumeration. It does not depend on the current culture.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(weekday, ", operands[0], ")");                        // DATEPART ( datepart , date ) ;  weekday
                case DxFilterOperationType.Function_GetDayOfYear:
                    // Gets the day of the year in the specified date.
                    // The operand must be of the DateTime / TimeOnly type.
                    // The return value is an integer in the range between 1 and 366.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(dayofyear, ", operands[0], ")");                      // DATEPART ( datepart , date ) ;  dayofyear
                case DxFilterOperationType.Function_GetTimeOfDay:
                    // Gets the time part of the specified date.
                    // The operand must be of the DateTime type.
                    // The return value is the Int64 object that is the number of 100 - nanosecond ticks that have elapsed since midnight.
                    checkCount(1);
                    break;
                #endregion
                #region Function - DateTime 6: Current
                case DxFilterOperationType.Function_Now:
                    // Returns the DateTime value that is the current date and time.
                    return DxExpressionToken.CreateText("getdate()");
                case DxFilterOperationType.Function_UtcNow:
                    // Returns a DateTime object that is the current date and time in Universal Coordinated Time (UTC).
                    return DxExpressionToken.CreateText("getutcdate()");
                case DxFilterOperationType.Function_Today:
                    // Returns a DateTime value that is the current date. The time part is set to 00:00:00.
                    return DxExpressionToken.CreateText("datetrunc(d, getdate())");
                case DxFilterOperationType.Function_TruncateToMinute:
                    // For internal use.
                    checkCount(1);
                    if (count == 0) DxExpressionToken.CreateText("datetrunc(mi, getdate())");
                    if (count == 1) DxExpressionToken.CreateFrom("datetrunc(mi, ", operands[0], ")");
                    failCount("1 or 2");
                    break;
                #endregion
                #region Function - DateTime 7: Time (Hour, BeforeMidday, Afternoon, IsLunchTime...)
                //    ========  TYTO FUNKCE NEUMÍM NAVODIT Z OKNA ŘÁDKOVÉHO FILTRU  ======== ,   tedy nemohu je otestovat...  :
                case DxFilterOperationType.Function_IsSameHour:
                    // Returns True if the specified time falls within the same hour.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("datepart(hour, ", operands[0], ") = datepart(hour, ", operands[1], ")");
                case DxFilterOperationType.Function_IsSameTime:
                    // Returns True if the specified time falls within the same time of day (hour and minute).
                    return DxExpressionToken.CreateFrom("(datepart(hour, ", operands[0], ") = datepart(hour, ", operands[1], ") and datepart(minute, ", operands[0], ") = datepart(minute, ", operands[1], ")");
                case DxFilterOperationType.Function_BeforeMidday:
                    // Returns True if the specified time is before 12:00 PM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(hour,", operands[0], ") < 12");
                case DxFilterOperationType.Function_AfterMidday:
                    // Returns True if the specified time is after 12:00 PM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("datepart(hour,", operands[0], ") >= 12");
                case DxFilterOperationType.Function_IsNight:
                    // Returns True if the specified time falls between 9:00 PM and 9:00 AM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(hour,", operands[0], ") < 9 or datepart(hour,", operands[0], ") >= 21)");
                case DxFilterOperationType.Function_IsMorning:
                    // Returns True if the specified time falls within between 6:00 AM and 12:00 PM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(hour,", operands[0], ") >= 6 and datepart(hour,", operands[0], ") < 12)");
                case DxFilterOperationType.Function_IsAfternoon:
                    // Returns True if the specified time falls between 12:00 PM and 6:00 PM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(hour,", operands[0], ") >= 12 and datepart(hour,", operands[0], ") < 18)");
                case DxFilterOperationType.Function_IsEvening:
                    // Returns True if the specified time falls between 6:00 PM and 9:00 PM.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(hour,", operands[0], ") >= 18 and datepart(hour,", operands[0], ") < 21)");
                case DxFilterOperationType.Function_IsLastHour:
                    // Returns True if the specified time falls within the last hour.
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-1);     // Začátek předešlé hodiny (pokud nyní je 15.8.2025 16:48:25, pak dateBegin = 15.8.2025 15:00:00
                    dateEnd = dateBegin.AddHours(1);                                                         // Konec hodinového intervalu (dateEnd = 15.8.2025 16:00:00)
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsThisHour:
                    // Returns True if the specified time falls within the hour.
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);                  // Začátek této hodiny (pokud nyní je 15.8.2025 16:48:25, pak dateBegin = 15.8.2025 16:00:00
                    dateEnd = dateBegin.AddHours(1);                                                         // Konec hodinového intervalu (dateEnd = 15.8.2025 17:00:00)
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsNextHour:
                    // Returns True if the specified time falls within the next hour.
                    checkCount(1);
                    dateBegin = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);      // Začátek příští hodiny (pokud nyní je 15.8.2025 16:48:25, pak dateBegin = 15.8.2025 17:00:00
                    dateEnd = dateBegin.AddHours(1);                                                         // Konec hodinového intervalu (dateEnd = 15.8.2025 18:00:00)
                    return createDateTimeInterval(operands[0], dateBegin, dateEnd);
                case DxFilterOperationType.Function_IsWorkTime:
                    // Returns True if the specified time falls within work time.
                    //   ... co já vím, kdy je pracovní doba ???
                    //   ... prostě bude se pracovat od pondělí do pátku od 7 do 16 hodin...   Poznámka: datepart(weekday, [datum]) vrací 2 = pondělí, 6 = pátek.   A svátky neslavíme.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(weekday,", operands[0], ") >= 2 and datepart(weekday,", operands[0], ") < 7 and datepart(hour,", operands[0], ") >= 7 and datepart(hour,", operands[0], ") < 16)");
                case DxFilterOperationType.Function_IsFreeTime:
                    // Returns True if the specified time falls within free time.
                    //   ... co já vím, kdy je pracovní doba ???
                    //   ... prostě bude se pracovat od pondělí do pátku od 7 do 16 hodin...   Poznámka: datepart(weekday, [datum]) vrací 2 = pondělí, 6 = pátek.   A svátky neslavíme.
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(not (datepart(weekday,", operands[0], ") >= 2 and datepart(weekday,", operands[0], ") < 7 and datepart(hour,", operands[0], ") >= 7 and datepart(hour,", operands[0], ") < 16))");
                case DxFilterOperationType.Function_IsLunchTime:
                    // Returns True if the specified time falls within the lunch time.
                    //   ... zase dobrý. Třeba hobiti mají LunchTime určitě delší, než Čech pracující v montovně.
                    //   ... Dáme tedy čas oběda mezi 11:00 až 12:59    ( < 13)
                    checkCount(1);
                    return DxExpressionToken.CreateFrom("(datepart(hour,", operands[0], ") > 11 and datepart(hour,", operands[0], ") < 13)");
                case DxFilterOperationType.Function_AddTimeSpan:
                    // Returns a DateTime value that differs by a specified amount of time from a specified date.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the TimeSpan object that is the time period before or after the start date.



                    break;
                case DxFilterOperationType.Function_AddTicks:
                    // Returns a DateTime value that is the specified number of ticks before or after a specified start date.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the integer number that is the number of 100-nanosecond ticks. This number can be negative or positive.
                    checkCount(2);
                    // MS SQL : DATEADD (datepart , number , date )
                    return DxExpressionToken.CreateFrom("(dateadd(nanosecond,", intAsText(operands[1], 0, 100), ",", operands[0], ")");
                case DxFilterOperationType.Function_AddMilliSeconds:
                    // Returns a DateTime/TimeOnly value that is the specified number of milliseconds before or after a specified start date/time.
                    // The operands are:
                    //  1 - the DateTime/TimeOnly value that is the start date.
                    //  2 - the Double value that is the number of milliseconds before or after the start date. This number can be negative or positive. Its decimal part is a fraction of a millisecond.
                    // Returns a DateTime value that is the specified number of ticks before or after a specified start date.
                    // The operands are:
                    //  1 - the DateTime value that is the start date.
                    //  2 - the integer number that is the number of 100-nanosecond ticks. This number can be negative or positive.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(millisecond,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddSeconds:
                    // Returns a DateTime/TimeOnly value that is the specified number of seconds before or after a specified start date/time.
                    // The operands are:
                    //  1 - the DateTime/TimeOnly value that is the start date.
                    //  2 - the Double value that is the number of seconds before or after the start date. This number can be negative or positive. Its decimal part is a fraction of a second.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(second,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddMinutes:
                    // Returns a DateTime/TimeOnly value that is the specified number of minutes before or after a specified start date/time.
                    // The operands are:
                    //  1 - the DateTime/TimeOnly value that is the start date.
                    //  2 - the Double value that is the number of minutes before or after the start date. This number can be negative or positive. Its decimal part is a fraction of a minute.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(minute,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddHours:
                    // Returns a DateTime/TimeOnly value that is the specified number of hours before or after a specified start date/time.
                    // The operands are:
                    //  1 - the DateTime/TimeOnly value that is the start date.
                    //  2 - the Double value that is the number of hours before or after the start date. This number can be negative or positive. Its decimal part is a fraction of an hour.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(hour,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddDays:
                    // Returns a DateTime/DateOnly value that is the specified number of days before or after a specified start date.
                    // The operands are:
                    //  1 - the DateTime/DateOnly value that is the start date.
                    //  2 - the Double value that is the number of days before or after the start date. This number can be negative or positive. Its decimal part is a fraction of a day.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(day,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddMonths:
                    // Returns a DateTime/DateOnly value that is the specified number of months before or after a specified start date.
                    // The operands are:
                    //  1 - the DateTime/DateOnly value that is the start date.
                    //  2 - the integer value that is the number of months before or after the start date. This number can be negative or positive.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(month,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_AddYears:
                    // Returns a DateTime/DateOnly value that is the specified number of years before or after a specified start date.
                    // The operands are:
                    //  1 - the DateTime/DateOnly value that is the start date.
                    //  2 - the integer value that is the number of years before or after the start date. This number can be negative or positive.
                    checkCount(2);
                    return DxExpressionToken.CreateFrom("(dateadd(year,", operands[1], ",", operands[0], ")");
                case DxFilterOperationType.Function_DateTimeFromParts:
                    // Returns a date value constructed from the specified Year, Month, Day, Hour, Minute, Second, and Millisecond.
                    // The operands are:
                    //  1 - (Required) - an integer value that is the full year value (four digits, century included).
                    //  2 - (Required) - an integer value that is the month number (1-12).
                    //  3 - (Required) - an integer value that is the day of the month (1-31).
                    //  4 - (Optional) - an hour value in 24-hour format (0-23).
                    //  5 - (Optional) - a minute value (0-59).
                    //  6 - (Optional) - a second value (0-59).
                    //  7 - (Optional) - a millisecond value.
                    // MS SQL : DATEFROMPARTS ( year, month, day ) 
                    // MS SQL : DATETIMEFROMPARTS ( year , month , day , hour , minute , seconds , milliseconds )
                    if (count == 3) return DxExpressionToken.CreateFrom("(datefromparts(", operands[0], ",", operands[1], ",", operands[2], ")");
                    if (count == 4) return DxExpressionToken.CreateFrom("(datetimefromparts(", operands[0], ",", operands[1], ",", operands[2], ",", operands[3], ", 0, 0, 0)");
                    if (count == 5) return DxExpressionToken.CreateFrom("(datetimefromparts(", operands[0], ",", operands[1], ",", operands[2], ",", operands[3], ",", operands[4], ", 0, 0)");
                    if (count == 6) return DxExpressionToken.CreateFrom("(datetimefromparts(", operands[0], ",", operands[1], ",", operands[2], ",", operands[3], ",", operands[4], ",", operands[5], ", 0)");
                    if (count == 7) return DxExpressionToken.CreateFrom("(datetimefromparts(", operands[0], ",", operands[1], ",", operands[2], ",", operands[3], ",", operands[4], ",", operands[5], ",", operands[6], ")");
                    failCount("3 to 7");
                    break;
                case DxFilterOperationType.Function_DateOnlyFromParts:
                    // Returns a DateOnly value constructed from the specified Year, Month, and Day.
                    // The operands are:
                    //  1 - an integer value that is the full year value (four digits, century included).
                    //  2 - an integer value that is the month number (1-12).
                    //  3 - an integer value that is the day of the month (1-31).
                    // MS SQL : DATEFROMPARTS ( year, month, day ) 
                    checkCount(3);
                    return DxExpressionToken.CreateFrom("(datefromparts(", operands[0], ",", operands[1], ",", operands[2], ")");
                case DxFilterOperationType.Function_TimeOnlyFromParts:
                    // Returns a TimeOnly value constructed from the specified hour, minute, seconds (optional), and milliseconds (optional).
                    // MS SQL : TIMEFROMPARTS ( hour, minute, seconds, fractions, precision ) 
                    if (count == 2) return DxExpressionToken.CreateFrom("(timefromparts(", operands[0], ",", operands[1], ", 0, 0, 0)");
                    if (count == 3) return DxExpressionToken.CreateFrom("(timefromparts(", operands[0], ",", operands[1], ",", operands[2], ", 0, 0)");
                    if (count == 4) return DxExpressionToken.CreateFrom("(timefromparts(", operands[0], ",", operands[1], ",", operands[2], ",", operands[3], ",", operands[4], ", 3)");
                    failCount("2 to 4");
                    break;
                #endregion
                #region Function - Custom: Like, ...
                // Custom funkce mají v nativním poli operandů na pozici [0] uložen název funkce, ale předchozí kód tento název funkce odebral (a z jeho hodnoty vygeneroval kód operace).
                // Proto v poli 'operands' je tento první operand (název funkce) odebrán, a toto pole tedy obsahuje pouze reálné datové operandy !!!
                case DxFilterOperationType.Custom_Like:
                    checkCount(2);
                    // v této operaci NEPŘIDÁVÁM prefix % ; ani suffix %   ten by měl být dodán v hodnotě od uživatele. Proto jsme LIKE, ne jako v operaci:  Function_Contains atd.
                    // Jsme obdoba Binary_Like, tam se taky prefix nepřidává.
                    return DxExpressionToken.CreateFrom(operands[0], " like ", stringAsValue(operands[1]));        // [nazev] like '%hodnota%';
                    #endregion
            }

            if (System.Diagnostics.Debugger.IsAttached) return DxExpressionToken.CreateFrom("/* NotConverted: ", operation.ToString(), "(", DxExpressionToken.CreateDelimited(",", operands), ") */");

            throw new NotImplementedException($"DxCriteriaVisitor: Operation '{operation}' is not implemented! ({(DxExpressionToken.CreateDelimited(",", operands))})");


            // Vrátí text binárního operátoru
            string getBinaryOperatorText(DxFilterOperationType binOp)
            {
                switch (binOp)
                {
                    case DxFilterOperationType.Binary_Equal: return " = ";
                    case DxFilterOperationType.Binary_NotEqual: return " <> ";
                    case DxFilterOperationType.Binary_Greater: return " > ";
                    case DxFilterOperationType.Binary_Less: return " < ";
                    case DxFilterOperationType.Binary_LessOrEqual: return " <= ";
                    case DxFilterOperationType.Binary_GreaterOrEqual: return " >= ";
                    case DxFilterOperationType.Binary_Like: return " like ";
                    case DxFilterOperationType.Binary_BitwiseAnd: return " & ";
                    case DxFilterOperationType.Binary_BitwiseOr: return " | ";
                    case DxFilterOperationType.Binary_BitwiseXor: return " ~ ";
                    case DxFilterOperationType.Binary_Divide: return " / ";
                    case DxFilterOperationType.Binary_Modulo: return " % ";
                    case DxFilterOperationType.Binary_Multiply: return " * ";
                    case DxFilterOperationType.Binary_Plus: return " + ";
                    case DxFilterOperationType.Binary_Minus: return " - ";
                }
                return " " + binOp.ToString() + " ";
            }
            // Pokud daná část obsahuje value, typu Int, pak výstupem je Text s touto hodnotou. Používá se u konstant, které NECHCEME řešit pomocí DB parametrů. Např. délka čísla atd.
            object intAsText(DxExpressionToken part, int addValue = 0, int mulValue = 1)
            {
                // Pokud 'part' je IsValueInt32, pak výstupem bude string obsahující zadanou hodnotu [s přičteným modifikátorem], 
                //   z toho pak bude obyčejný text = součást textu filtr, a nikoli Value (=DB parametr):
                if (part.IsValueInt32) return ((mulValue * part.ValueInt32) + addValue).ToString();

                // Pokud 'part' není IsValueInt32, a pokud není potřeba nic přičíst/odečíst (addValue == 0) ani násobit (mulValue == 1),
                //   pak výstupem bude vstupní částice a půjde do výstupního filtru sama za sebe, například podřízený vzorec nebo funkce:
                bool hasAdd = (addValue != 0);
                bool hasMul = (mulValue != 1);
                if (!hasAdd && !hasMul) return part;

                // Pokud ale 'part' není IsValueInt32, a přitom jsme k ní chtěli něco přičíst nebo pronásobit,
                //   pak vytvoříme new částici typu Container,
                //   kde bude: "(" a původní částice (= výraz) a k tomu text " +- addValue" a ")":
                // Varianty a odpovídající součásti textu:
                //                  part               
                //     (            part  +/- addValue)
                //      (mulValue * part)              
                //     ((mulValue * part) +/- addValue)
                // tx  1222222222222    344444444444444
                string tx1 = null;
                string tx2 = null;
                string tx3 = null;
                string tx4 = null;
                if (hasMul)
                {
                    tx2 = $"({mulValue} * ";
                    tx3 = ")";
                }
                if (addValue > 0)
                {
                    tx1 = "(";
                    tx4 = $" + {addValue})";
                }
                else if (addValue < 0)
                {
                    tx1 = "(";
                    tx4 = $" - {(-addValue)})";
                }
                // Složený výraz (pokud vkládám null, pak nebude vloženo):
                return DxExpressionToken.CreateFrom(tx1, tx2, part, tx3, tx4);
            }
            // Pokud daná část obsahuje value, typu String, pak výstupem je Value s touto hodnotou, s možností přidání textu před/po. Používá se u proměnných, které chceme modifikovat.
            object stringAsValue(DxExpressionToken part, string addBefore = null, string addAfter = null)
            {
                bool hasBefore = !String.IsNullOrEmpty(addBefore);
                bool hasAfter = !String.IsNullOrEmpty(addAfter);
                addBefore = addBefore ?? "";
                addAfter = addAfter ?? "";

                if (part.IsValue)
                {   // Obsahuje hodnotu => pouze upravíme jeho hodnotu (Value), a vrátíme týž objekt:
                    //   Objekt se vyskytuje jen v jednom místě, a proto jej můžeme modifikovat = nezměníme jinou část podmínky!
                    if (part.IsValueString)
                        part.Value = mergeText(part.ValueString, addBefore, addAfter);
                    else
                        part.Value = $"{addBefore}{part.Value}{addAfter}";
                    return part;
                }
                if (part.IsText)
                {   // Obsahuje Text => vytvoříme new instanci typu Value (obsahující Before + Text + After), a tu pak vrátíme:
                    var valuePart = DxExpressionToken.CreateValue(mergeText(part.Text, addBefore, addAfter));
                    return valuePart;
                }

                // Obsahuje něco jiného = například vzorec: pak musíme vrátit new část typu Container, obsahující texty Before a/nebo After:
                //  Pokud ale nemáme nic přidat, nemusíme nic řešit:
                if (!hasBefore && !hasAfter) return part;

                // Sestavíme container (přičemž vstupní objekty, které jsou null, nebudou do containeru vloženy):
                return DxExpressionToken.CreateFrom("(", (hasBefore ? $"'{addBefore}' + " : null), part, (hasAfter ? $" + '{addAfter}'" : null), ")");
            }
            // Dané datum vrátí jako string (text), který lze použít ve filtru a reprezentuje přesně zadaný čas, ve formě:  "convert(datetime, '2025-08-03 12:34:56.789', 121)"
            object dateTimeAsText(DateTime dateTime)
            {
                return $"convert(datetime, '{dateTime.Year:D4}-{dateTime.Month:D2}-{dateTime.Day:D2} {dateTime.Hour:D2}:{dateTime.Minute:D2}:{dateTime.Second:D2}.{dateTime.Millisecond:D3}', 121)";
            }
            // Vrátí dodaný text, opatřený prefixem (pokud je zadán, a text s ním dosud nezačíná) a suffixem (pokud je zadán, a text s ním dosud nekončí)
            string mergeText(string text, string prefix, string suffix)
            {
                string result = text ?? "";
                if (!String.IsNullOrEmpty(prefix) && !result.StartsWith(prefix))
                    result = prefix + result;
                if (!String.IsNullOrEmpty(suffix) && !result.EndsWith(suffix))
                    result = result + suffix;
                return result;
            }
            // Vrátí datum odpovídající prvnímu dni týdne, ve kterém je daný vstup, čas bude 00:00
            DateTime getWeekBegin(DateTime dateTime)
            {
                var fdow = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;              // Který den v týdnu v aktuální kultuře reprezentuje počátek týdne?
                int fnow = (int)fdow;                      // Číslo prvního dne v týdnu: neděle = 0, pondělí = 1, ..., sobota = 6
                int fnod = (int)dateTime.DayOfWeek;        // Číslo zadaného dne v týdnu: neděle = 0, pondělí = 1, ..., sobota = 6
                int subtract = fnod - fnow;                // Kolik dnů nazpátek musím přetočit od dateTime doleva, abych byl v prvním dnu v týdnu
                var dBegin = (subtract == 0 ? dateTime.Date : dateTime.AddDays(-subtract).Date);
                return dBegin;
            }
            // Vrátí DxExpressionToken obsahující výraz pro DateTime: "(value >= dBegin and value < dEnd)"
            DxExpressionToken createDateTimeInterval(DxExpressionToken value, DateTime dBegin, DateTime dEnd)
            {
                return DxExpressionToken.CreateFrom("(", value, " >= ", dateTimeAsText(dBegin), " and ", value, " < ", dateTimeAsText(dEnd), ")");
            }
            // Pokud počet operandů == daný počet, pak vrátí řízení. POkud není rovno, vyhodí chybu pomocí failCount().
            void checkCount(int validCount)
            {
                if (count == validCount) return;
                failCount(validCount.ToString());
            }
            // Ohlásí chybu v počtu parametrů s daným textem obsahujícím očekávaný počet parametrů
            void failCount(string countInfo)
            {
                if (count < 0)
                    throw new ArgumentException($"Filter condition '{operation}' requires {countInfo} operators, but any from the supplied operators is not valid.");
                else
                    throw new ArgumentException($"Filter condition '{operation}' requires {countInfo} operators, but {count} valid operators are passed.");
            }
        }
        /// <summary>
        /// Konvertuje DX operaci na zdejší operaci, na základě názvu grupy a názvu DX operace (string TryParse <see cref="DxFilterOperationType"/>).
        /// Neznámé vstupy vyhodí <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="familyType">Rodina operací</param>
        /// <param name="operationType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private DxFilterOperationType ConvertOperation(FamilyType familyType, object operationType = null)
        {
            string resultName = ((operationType is null) ? $"{familyType}" : $"{familyType}_{operationType}");
            if (Enum.TryParse<DxFilterOperationType>(resultName, true, out var resultValue)) return resultValue;
            throw new ArgumentException($"DxCriteriaVisitor: Operation '{resultName}' does not exists in 'DxFilterOperationType' enum.");
        }
        /// <summary>
        /// Typ rodiny operací, odpovídá visitorům jednotlivých typů operandů.
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
        #region Konverze operandu DxFilter.CriteriaOperator do DxExpressionToken, jednotlivě i kolekce
        /// <summary>
        /// Z dodaného pole operandů <paramref name="operands"/> konvertuje jejich obsah do stringů a vrátí. 
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operands">Neomezené pole operandů</param>
        /// <returns></returns>
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionToken>();
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operand <paramref name="operand0"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0)
        {
            var operandItems = new List<DxExpressionToken>();
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
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionToken>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operandy <paramref name="operand0"/> a <paramref name="operand1"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operand1">Druhý fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1)
        {
            var operandItems = new List<DxExpressionToken>();
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
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1, DxFilter.CriteriaOperatorCollection operands)
        {
            var operandItems = new List<DxExpressionToken>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand1)) return null;
            if (!ConvertAllOperand(operandItems, mode, operands)) return null;
            return operandItems;
        }
        /// <summary>
        /// Konvertuje fixní operandy <paramref name="operand0"/>, <paramref name="operand1"/> a <paramref name="operand2"/>.
        /// Nevalidní operandy buď vynechává, anebo při jejich výskytu vrátí null, podle <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="operand0">První fixní operand</param>
        /// <param name="operand1">Druhý fixní operand</param>
        /// <param name="operand2">Třetí fixní operand</param>
        /// <returns></returns>
        private List<DxExpressionToken> ConvertOperands(ConvertOperandsMode mode, DxFilter.CriteriaOperator operand0, DxFilter.CriteriaOperator operand1, DxFilter.CriteriaOperator operand2)
        {
            var operandItems = new List<DxExpressionToken>();
            if (!ConvertOneOperand(operandItems, mode, operand0)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand1)) return null;
            if (!ConvertOneOperand(operandItems, mode, operand2)) return null;
            return operandItems;
        }
        /// <summary>
        /// Do dodaného pole výsledných výrazů přidá konvertované operandy.
        /// </summary>
        /// <param name="operandItems"></param>
        /// <param name="mode"></param>
        /// <param name="operands"></param>
        /// <returns></returns>
        private bool ConvertAllOperand(List<DxExpressionToken> operandItems, ConvertOperandsMode mode, DxFilter.CriteriaOperatorCollection operands)
        {
            if (operands is null) return (mode == ConvertOperandsMode.RemoveEmptyItems);        // Pokud na vstupu je null, pak vracím true = OK jen tehdy, když režim je volnější (Remove empty items)
            foreach (var operand in operands)
            {
                bool isValid = ConvertOneOperand(operandItems, mode, operand);
                if (!isValid) return false;
            }
            return true;
        }
        /// <summary>
        /// Do dodaného pole výsledných výrazů přidá konvertovaný operand.
        /// </summary>
        /// <param name="operandItems"></param>
        /// <param name="mode"></param>
        /// <param name="operand"></param>
        /// <returns></returns>
        private bool ConvertOneOperand(List<DxExpressionToken> operandItems, ConvertOperandsMode mode, DxFilter.CriteriaOperator operand)
        {
            if (operand is null) return (mode == ConvertOperandsMode.RemoveEmptyItems);        // Pokud na vstupu je null, pak vracím true = OK jen tehdy, když režim je volnější (Remove empty items)

            // V tomto řádku se provede:
            //  Operand podle svého konkrétního typu vyvolá metodu Visit dodaného Visitoru (kterým jsme my),
            //  a tedy se vyvolá určitá konkrétní metoda nahoře v této třídě, např:
            //     DxExpressionToken DxFilter.ICriteriaVisitor<DxExpressionToken>.Visit(DxFilter.BinaryOperator binaryOperator)
            //  v té metodě se (rekurzivně) konvertují její operandy z třídy DxFilter.CriteriaOperator (z konkrétního potomka) do jednotlivých DxExpressionToken,
            //  a poté se z operátorů a z textu odpovídajícího dané operaci sestaví nový DxExpressionToken typu Container, který bude obsahovat operátory a texty.
            var operandItem = operand.Accept(this);

            // 
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
            /// Operandy jsou povinné, a pokud nějaký nebude možno konvertovat, pak vrátí null namísto celé kolekce
            /// </summary>
            StrictlyAllItems,
            /// <summary>
            /// Operandy jsou nepovinné, a pokud nějaký nebude možno konvertovat, pak jej do kolekce nepřidá, vrátí kolekci ostatních konvertovaných prvků.
            /// </summary>
            RemoveEmptyItems
        }
        #endregion
    }
    #endregion
    #region class DxExpressionToken  : Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// <summary>
    /// <see cref="DxExpressionToken"/> : Část výrazu v procesu skládání výsledku z filtru do cílového jazyka
    /// </summary>
    internal class DxExpressionToken : IDxExpressionTokenWorking
    {
        #region Public members : Create + Add
        /// <summary>
        /// Vytvoří prvek typu Container
        /// </summary>
        /// <returns></returns>
        internal static DxExpressionToken CreateContainer()
        {
            var part = new DxExpressionToken(PartType.Container);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static DxExpressionToken CreateText(string text)
        {
            var part = new DxExpressionToken(PartType.Text);
            part.__Text = text;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Property = sloupec databáze
        /// </summary>
        /// <param name="propertyName">Jméno sloupce = ColumnId</param>
        /// <param name="column">Nalezený sloupec</param>
        /// <returns></returns>
        internal static DxExpressionToken CreateProperty(string propertyName, IFilterColumnInfo column)
        {
            var part = new DxExpressionToken(PartType.PropertyName);
            part.__PropertyName = propertyName;
            part.__Column = column;
            part.__PropertyResult = column?.DisplayValueSource;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Value = hodnota
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static DxExpressionToken CreateValue(object value)
        {
            var part = new DxExpressionToken(PartType.Value);
            part.__Value = value;
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionToken CreateFrom(params object[] parts)
        {
            var part = new DxExpressionToken(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionToken CreateFrom(IEnumerable<object> parts)
        {
            var part = new DxExpressionToken(PartType.Container);
            part._AddRange(parts);
            return part;
        }
        /// <summary>
        /// Vytvoří prvek typu Container a naplní jej dodanými daty, kde mezi každý nenulový objekt vloží text s daným oddělovačem
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        internal static DxExpressionToken CreateDelimited(string delimiter, IEnumerable<object> parts)
        {
            var part = new DxExpressionToken(PartType.Container);
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
            return this.ResultText;
        }
        /// <summary>
        /// Konvertuje obsah this prvku (podle jeho typu, tedy i včetně subprvků v Containeru) do textu v jeho výstupním jazyce.
        /// Jazyk je dodán jako vstupní údaj do konvertoru.
        /// Jazyk ovlivní konverzi, formátování názvů sloupců a hodnot.
        /// </summary>
        internal string ResultText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                var language = this.Language;
                this._AddText(sb, language);
                return sb.ToString();
            }
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
                    sb.Append(DxFilterConvertor.FormatPropertyName(this));
                    break;
                case PartType.Value:
                    sb.Append(DxFilterConvertor.FormatValue(this.__Value, language));
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
        private DxExpressionToken(PartType partType)
        {
            __Language = null;
            __PartType = partType;
            if (partType == PartType.Container)
                __Items = new List<DxExpressionToken>();
        }
        private DxExpressionLanguageType? __Language;
        private PartType __PartType;
        private List<DxExpressionToken> __Items;
        private string __Text;
        private string __PropertyName;
        private string __PropertyResult;
        private IFilterColumnInfo __Column;
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
        /// <summary>
        /// Do daného pole částic přidá (detekuje, merguje) zadané prvky.
        /// </summary>
        /// <param name="targetItems"></param>
        /// <param name="addItems"></param>
        private static void _AddItemsTo(List<DxExpressionToken> targetItems, object[] addItems)
        {
            for (int i = 0; i < addItems.Length; i++)
            {
                // Na vstupu může být cokoliv - string, DxToExpressionPart (různého typu) i cokoliv jiného, což budeme chápat jako String. Jen null budeme přeskakovat.
                var addItem = addItems[i];
                if (addItem is null) continue;

                var targetCount = targetItems.Count;
                var targetLastItem = (targetCount > 0 ? targetItems[targetCount - 1] : null);
                bool canMergeText = (targetCount > 0 && targetLastItem.IsText);
                if (addItem is DxExpressionToken addPart)
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
        /// Pokud this obsahuje nějaká Simple data, pak z this (Simple) dat vytvoří klon, ze sebe vytvoří Container; a ten klon původních Simple dat vloží jako první prvek do this nového Containeru.
        /// </summary>
        private void _SwitchToContainer()
        {
            var isSimple = IsSimple;
            var isContainer = IsContainer;

            // Pokud this obsahuje jednoduchou hodnotu, pak jí zde zkopíruji do new objektu:
            DxExpressionToken simple = (isSimple ? this.MemberwiseClone() as DxExpressionToken : null);

            // Pokud this dosud NENÍ Container, pak this změním tak, aby byl Container:
            if (!isContainer)
            {
                this.__PartType = PartType.Container;
                this.__Items = new List<DxExpressionToken>();
                this.__Text = null;
                this.__PropertyName = null;
                this.__Value = null;
            }

            // Pokud this původně byl Simple, pak nyní do this Containeru vložím data z původního Simple objektu:
            if (isSimple)
                this.__Items.Add(simple);
        }
        #endregion
        #region Private : IDxExpressionTokenWorking
        DxExpressionLanguageType? IDxExpressionTokenWorking.Language 
        {
            get { return __Language; }
            set { _SetLanguage(value); } 
        }
        /// <summary>
        /// Nastaví daný jazyk do this a do mách Child items
        /// </summary>
        /// <param name="language"></param>
        private void _SetLanguage(DxExpressionLanguageType? language)
        {
            this.__Language = language;
            if (this.IsContainer && this.__Items != null)
            {
                foreach (var item in this.__Items)
                    item._SetLanguage(language);
            }
        }
        #endregion
        #region Public informace
        /// <summary>
        /// Jazyk výrazu
        /// </summary>
        public DxExpressionLanguageType Language { get { return __Language ?? DxExpressionLanguageType.Default; } }
        /// <summary>
        /// Tato částice reprezentuje fixní text? Typicky kód výrazu, závorky, název funkce, operátor...
        /// </summary>
        public bool IsText { get { return (this.__PartType == PartType.Text); } }
        /// <summary>
        /// Fixní text. Typicky kód výrazu, závorky, název funkce, operátor...
        /// </summary>
        public string Text { get { return (IsText ? __Text : null); } set { if (IsText) __Text = value; } }
        /// <summary>
        /// Tato částice reprezentuje sloupec? Na vstupu se mu říká PropertyName, reprezentuje databázový sloupec.
        /// </summary>
        public bool IsPropertyName { get { return (this.__PartType == PartType.PropertyName); } }
        /// <summary>
        /// Sloupec s daty z databáze, zde je uvedeno jeho = ColumnId, bez hranatých závorek. Na vstupu se mu říká PropertyName, reprezentuje databázový sloupec.
        /// </summary>
        public string PropertyName { get { return (IsPropertyName ? __PropertyName : null); } set { if (IsPropertyName) __PropertyName = value; } }
        /// <summary>
        /// Sloupec s daty z databáze, zde je dohledán objekt sloupce, pokud byly sloupce dodány v poli sloupců v <see cref="ConvertArgs.Columns"/>
        /// </summary>
        public IFilterColumnInfo Column { get { return (IsPropertyName ? __Column : null); } set { if (IsPropertyName) __Column = value; } }
        /// <summary>
        /// Sloupec s daty z databáze: zde je uveden výraz v cílovém jazyce, který bude umístěn do výsledného filtru.
        /// Pokud tedy filtr pracuje se sloupcem [nazev], což je alias (ColumnId) sloupce, a sloupec je dohledán v <see cref="Column"/>, 
        /// pak do této property je vložen odpovídající výraz, který ve filtru WHERE získá data očekávaná pro podmínku filtru.<br/>
        /// Povětšinou je zde uvedena základní hodnota <see cref="IFilterColumnInfo.DisplayValueSource"/>.<br/>
        /// Pokud sloupec reprezentuje CodeTable, pak zde je <see cref="IFilterColumnInfo.CodeValueSource"/>.<br/>
        /// Pokud sloupec je virtuální, pak zde je uveden odpovídající výraz (subselect), načítající data.
        /// </summary>
        public string PropertyResult { get { return (IsPropertyName ? __PropertyResult : null); } set { if (IsPropertyName) __PropertyResult = value; } }
        /// <summary>
        /// Tato částice reprezentuje hodnotu, typicky proměnnou, kterou je možno umístit do DB parametru?
        /// </summary>
        public bool IsValue { get { return (this.__PartType == PartType.Value); } }
        /// <summary>
        /// Tato částice reprezentuje hodnotu typu String, typicky proměnnou, kterou je možno umístit do DB parametru?
        /// </summary>
        public bool IsValueString { get { return (this.__PartType == PartType.Value && this.__Value is string); } }
        /// <summary>
        /// Tato částice reprezentuje hodnotu typu Int32, typicky proměnnou, kterou je možno umístit do DB parametru?
        /// </summary>
        public bool IsValueInt32 { get { return (this.__PartType == PartType.Value && this.__Value is int); } }
        /// <summary>
        /// Hodnota v této částici, typicky proměnná, kterou je možno umístit do DB parametru.
        /// </summary>
        public object Value { get { return (IsValue ? __Value : null); } set { if (IsValue) __Value = value; } }
        /// <summary>
        /// Hodnota v této částici typu String, typicky proměnná, kterou je možno umístit do DB parametru.
        /// </summary>
        public string ValueString { get { return (IsValueString ? __Value as string : null); } }
        /// <summary>
        /// Hodnota v této částici typu Int32, typicky proměnná, kterou je možno umístit do DB parametru.
        /// </summary>
        public int ValueInt32 { get { return (IsValueInt32 ? (int)__Value : 0); } }
        /// <summary>
        /// Tato částice reprezentuje jednomístný prvek (Text, Sloupec, Hodnota). Nikoli pole dalších hodnot?
        /// </summary>
        public bool IsSimple { get { return (this.__PartType == PartType.Text || this.__PartType == PartType.PropertyName || this.__PartType == PartType.Value); } }
        /// <summary>
        /// Tato částice reprezentuje pole dalších hodnot? Pokud ano, jsou v <see cref="Items"/>.
        /// </summary>
        public bool IsContainer { get { return (this.__PartType == PartType.Container); } }
        /// <summary>
        /// Pole dalších vnořených hodnot, pouze pokud this je Container.
        /// </summary>
        public DxExpressionToken[] Items { get { return (IsContainer ? __Items.ToArray() : null); } }
        /// <summary>
        /// Druh částice
        /// </summary>
        public enum PartType
        {
            /// <summary>
            /// Container: obsahuje další prvky, viz <see cref="DxExpressionToken.Items"/>
            /// </summary>
            Container,
            /// <summary>
            /// Prostý text: klíčová slova, oddělovače, závorky, atd, viz <see cref="DxExpressionToken.Text"/>
            /// </summary>
            Text,
            /// <summary>
            /// Název datového sloupce / property, viz <see cref="DxExpressionToken.PropertyName"/>
            /// </summary>
            PropertyName,
            /// <summary>
            /// Hodnota: obsahuje zadanou hodnotu / konstantu / proměnnou / parametr, viz <see cref="DxExpressionToken.Value"/>
            /// </summary>
            Value
        }
        #endregion
    }
    /// <summary>
    /// Pracovní rozhraní do <see cref="DxExpressionToken"/>
    /// </summary>
    internal interface IDxExpressionTokenWorking
    {
        /// <summary>
        /// Jazyk výrazu
        /// </summary>
        DxExpressionLanguageType? Language { get; set; }
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
    internal delegate void DxConvertorCustomHandler(object sender, DxConvertorCustomArgs args);
    /// <summary>
    /// Argumenty s daty pro custom handler
    /// </summary>
    internal class DxConvertorCustomArgs : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="language"></param>
        /// <param name="operation"></param>
        /// <param name="operands"></param>
        public DxConvertorCustomArgs(DxExpressionLanguageType language, DxFilterOperationType operation, List<DxExpressionToken> operands)
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
        public DxFilterOperationType Operation { get; set; }
        /// <summary>
        /// Jednotlivé operandy. Jejich význam a počet je dán typem operace.
        /// </summary>
        public List<DxExpressionToken> Operands { get; set; }
        /// <summary>
        /// Externí aplikace si přeje tuto funkci přeskočit, výsledkem bude null. Může to být v pořádku jen tehdy, když operace je členem vyšší operace, která nemá povinné operandy.
        /// </summary>
        public bool Skip { get; set; }
        /// <summary>
        /// Externí aplikace sama určila výsledný tvar výrazu
        /// </summary>
        public DxExpressionToken CustomResult { get; set; }
    }
    /// <summary>
    /// Typ operace.
    /// Obsahuje souhrn všech operací ze všech typů ve filtračním výrazu = <c>Family_Operation</c>.
    /// Konverze do tohoto enumu se provádí na základě zadané Family a "_" a textu názvu konkrétní operace!!!
    /// </summary>
    internal enum DxFilterOperationType
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
        Function_TimeOnlyFromParts,

        Custom_Like
    }
    #endregion
}
