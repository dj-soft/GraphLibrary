using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Djs.Common.TextParser
{
    /// <summary>
    /// ParserDefaultSetting : Generátor settingů pro různé konkrétní jazyky.
    /// </summary>
    public static class ParserDefaultSetting
    {
        #region Public static property, které vracejí settings
        /// <summary>
        /// ParserSetting pro Microsoft T-SQL.
        /// </summary>
        public static ParserSetting MsSql { get { return _GetSettingMsSql(); } }
        /// <summary>
        /// ParserSetting pro RTF formát
        /// </summary>
        public static ParserSetting Rtf { get { return _GetSettingRtf(); } }
        /// <summary>
        /// ParserSetting pro C# formát
        /// </summary>
        public static ParserSetting CSharp { get { return _GetSettingCSharp(); } }
        /// <summary>
        /// ParserSetting pro XML + HTML formát
        /// </summary>
        public static ParserSetting Xml { get { return _GetSettingXml(); } }
        /// <summary>
        /// ParserSetting pro MSDN formát
        /// </summary>
        public static ParserSetting Msdn { get { return _GetSettingMsdn(); } }
        /// <summary>
        /// Vrátí daný text rozdělený na pole stringů, kde oddělovače jsou určeny. Pole se dělí s options: RemoveEmptyEntries.
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static List<string> SplitTextToRows(string keywords, params char[] separators)
        {
            return ParserSetting.SplitTextToRows(keywords, separators);
        }
        public const string NAME_KEYWORD = "Keyword";
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
        /// <param name="colorBack"></param>
        /// <param name="styleBegin"></param>
        /// <param name="styleEnd"></param>
        private static void ApplySchemeText(ParserSegmentSetting segment, Color colorText)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
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
        private static void ApplySchemeText(ParserSegmentSetting segment, Color? colorText, Color? colorBack, FormatFontStyle? styleBegin, FormatFontStyle? styleEnd)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                new FormatCode[] { FormatCode.NewForeColor(colorText.HasValue ? colorText.Value : Color.Empty), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(colorBack.HasValue ? colorBack.Value : Color.Empty), FormatCode.NewFontStyle(styleBegin.HasValue ? styleBegin.Value : FormatFontStyle.None) },
                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(styleEnd.HasValue ? styleEnd.Value : FormatFontStyle.None) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Plain
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemePlainText(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemePlainColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Code
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCode(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCodeColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Keyword
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeKeyword(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(NAME_KEYWORD,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeKeywordColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Keyword
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeKeyword(ParserSegmentSetting segment, Color? nextForeColor)
        {
            segment.AddFormatCodes(NAME_KEYWORD,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeKeywordColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor((nextForeColor.HasValue ? nextForeColor.Value : Color.Empty)) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Delimiter
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeDelimiter(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Delimiter,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeDelimiterColorText) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Delimiter
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeDelimiter(ParserSegmentSetting segment, Color? nextForeColor)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Delimiter,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeDelimiterColorText) },
                                new FormatCode[] { FormatCode.NewForeColor((nextForeColor.HasValue ? nextForeColor.Value : Color.Empty)) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = String
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeString(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeStringColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeStringStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeStringStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Char
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeChar(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCharColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeCharStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeCharStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Variable
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeVariable(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeVariableColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(SchemeVariableStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewFontStyle(SchemeVariableStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = SysObject
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeSysObject(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeSysObjectColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewFontStyle(FormatFontStyle.Regular) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = Entity
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeEntity(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeEntityColorText), FormatCode.NewHighlight(SchemeEntityColorBack), FormatCode.NewFontStyle(SchemeEntityStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeEntityStyleEnd) });
        }


        
        

        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentRow
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentBlock(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentBlockColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentBlock
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentRow(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentRowColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }
        /// <summary>
        /// Do segmentu vloží kompletní formátovací příkazy pro Text = CommentXml
        /// </summary>
        /// <param name="segment"></param>
        private static void ApplySchemeCommentXml(ParserSegmentSetting segment)
        {
            segment.AddFormatCodes(ParserSegmentValueType.Text,
                                new FormatCode[] { FormatCode.NewForeColor(SchemeCommentColorText), FormatCode.NewFont("Courier New CE"), FormatCode.NewHighlight(SchemeCommentXmlColorBack), FormatCode.NewFontStyle(SchemeCommentStyleBegin) },
                                new FormatCode[] { FormatCode.NewForeColor(Color.Empty), FormatCode.NewHighlight(Color.Empty), FormatCode.NewFontStyle(SchemeCommentStyleEnd) });
        }

        #endregion
        #endregion
        #region Generátor settings pro MsSql
        private static ParserSetting _GetSettingMsSql()
        {
            ParserSetting setting = new ParserSetting("MsSql", SQL_CODE);

            ParserSegmentSetting segment;

            // Společná data pro SQL_CODE a SQL_PARENTHESIS, které obě mohou obsahovat SQL kód:
            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", "+", "-", "*", "/", "<", "=", ">", "<=", ">=", "<>", "%" };
            string nums = "0123456789";
            // string hexs = "abcdef";
            string lows = "abcdefghijklmnopqrstuvwxyz";
            string upps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // SQL Kód:
            segment = new ParserSegmentSetting(SQL_CODE);
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.EndWith = new string[] { ";" };
            segment.InnerSegmentsNames = new string[] { SQL_LITERAL, SQL_UNICODE_LITERAL, SQL_VARIABLE, SQL_SYSNAME, SQL_PARENTHESIS, SQL_COMMENTLINE, SQL_COMMENTBLOCK };
            segment.Keywords = GetKeywordsSqlLanguage().ToArray();
            segment.KeywordsCaseSensitive = false;
            segment.EnableMergeDotSpaceText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment, SchemeCodeColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // String:
            segment = new ParserSegmentSetting(SQL_LITERAL);
            segment.BeginWith = "'";
            segment.EndWith = new string[] { "'" };
            segment.SpecialTexts = new ParserSegmentSpecialTexts[] { new ParserSegmentSpecialTexts("''", "'") };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Unicode String:
            segment = new ParserSegmentSetting(SQL_UNICODE_LITERAL);
            segment.BeginWith = "N'";
            segment.EndWith = new string[] { "'" };
            segment.SpecialTexts = new ParserSegmentSpecialTexts[] { new ParserSegmentSpecialTexts("''", "'") };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Variable:
            segment = new ParserSegmentSetting(SQL_VARIABLE);
            segment.BeginWith = "@";
            segment.Permitteds = ParserSetting.SplitTextToOneCharStringArray(nums + upps + lows + "_");
            segment.SpecialTexts = new ParserSegmentSpecialTexts[] { new ParserSegmentSpecialTexts("''", "'") };
            ApplySchemeVariable(segment);
            setting.SegmentSettingAdd(segment);

            // Hranaté závorky = systémové jméno:
            segment = new ParserSegmentSetting(SQL_SYSNAME);
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            ApplySchemeSysObject(segment);
            setting.SegmentSettingAdd(segment);

            // Kulaté závorky = funkce, výrazy, a pozor => i vnořené selecty, takže má charakter i code:
            segment = new ParserSegmentSetting(SQL_PARENTHESIS);
            segment.BeginWith = "(";
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.EndWith = new string[] { ")" };
            segment.InnerSegmentsNames = new string[] { SQL_LITERAL, SQL_UNICODE_LITERAL, SQL_VARIABLE, SQL_SYSNAME, SQL_PARENTHESIS, SQL_COMMENTLINE, SQL_COMMENTBLOCK };
            segment.Keywords = GetKeywordsSqlLanguage().ToArray();
            segment.KeywordsCaseSensitive = false;
            segment.EnableMergeDotSpaceText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment, SchemeCodeColorText);
            ApplySchemeDelimiter(segment, SchemeCodeColorText);
            setting.SegmentSettingAdd(segment);

            // Komentář řádkový (začíná --, končí koncem řádku, přičemž konec není ukládán do segmentu, ale zpracuje se jako další text):
            segment = new ParserSegmentSetting(SQL_COMMENTLINE);
            segment.BeginWith = "--";
            segment.StopWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // Komentář blokový (začíná /*, končí */), otázka je zda může obsahovat vnořené blokové komentáře => rozhodnutí zní ANO:
            segment = new ParserSegmentSetting(SQL_COMMENTBLOCK);
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.IsComment = true;
            segment.InnerSegmentsNames = new string[] { SQL_COMMENTBLOCK };
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
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
        #region SQL Konstanty
        public const string SQL_ = "Sql";
        public const string SQL_CODE = "SqlCode";
        public const string SQL_LITERAL = "SqlLiteral";
        public const string SQL_UNICODE_LITERAL = "SqlUnicodeLiteral";
        public const string SQL_VARIABLE = "SqlVariable";
        public const string SQL_SYSNAME = "SqlSysName";
        public const string SQL_PARENTHESIS = "SqlParenthesis";
        public const string SQL_COMMENTLINE = "SqlCommentLine";
        public const string SQL_COMMENTBLOCK = "SqlCommentBlock";
        #endregion
        #endregion
        #region Generátor settings pro Rtf
        private static ParserSetting _GetSettingRtf()
        {
            ParserSetting setting = new ParserSetting("Rtf", RTF_NONE);

            ParserSegmentSetting segment;

            string[] blanks = new string[] { "\t", "\r", "\n" };   // MEZERU nechápeme jak Blank znak, protože v textu je významná. Kdežto Cr a Lf (v RTF kódu) se v textu neprojevují.    původně:  new string[] { " ", "\t", "\r", "\n" };
            ParserSegmentSpecialTexts[] specs = new ParserSegmentSpecialTexts[] { new ParserSegmentSpecialTexts(@"\\", @"\") };

            string nums = "0123456789";
            string hexs = "abcdef";
            string lows = "abcdefghijklmnopqrstuvwxyz";
            string upps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // RTF text začíná "ve vakuu", vlastní obsah povinně musí začínat znakem { a končit }, v tomto bloku se nachází vlastní dokument i celá jeho vnitřní stavba:
            segment = new ParserSegmentSetting(RTF_NONE);
            segment.InnerSegmentsNames = new string[] { RTF_DOCUMENT };
            setting.SegmentSettingAdd(segment);

            // RTF dokument začíná { a končí }, obsahuje vnitřní bloky ({...}), entity (\fxxx) a znaky (\'a8) i texty:
            segment = new ParserSegmentSetting(RTF_DOCUMENT);
            segment.BeginWith = "{";
            segment.Blanks = blanks.ToArray();
            segment.EndWith = new string[] { "}" };
            segment.SpecialTexts = specs.ToArray();
            segment.InnerSegmentsNames = new string[] { RTF_BLOCK, RTF_ENTITY, RTF_CHAR2 /*, RTF_CHARUNICODE */ };
            ApplySchemePlainText(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF vnořený blok začíná { a končí }, obsahuje další vnitřní bloky ({...}), entity (\fxxx) a texty:
            segment = new ParserSegmentSetting(RTF_BLOCK);
            segment.BeginWith = "{";
            segment.Blanks = blanks.ToArray();
            segment.EndWith = new string[] { "}" };
            segment.SpecialTexts = specs.ToArray();
            segment.InnerSegmentsNames = new string[] { RTF_BLOCK, RTF_ENTITY, RTF_CHAR2 };
            ApplySchemePlainText(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF entita začíná \ a končí mezerou (mezeru "sežere" = nestane se součástí dalšího textu), nebo končí \ což je stop znak = není součástí End bloku entity, ale představuje další entitu nebo text. Nemá vnitřní segmenty.
            segment = new ParserSegmentSetting(RTF_ENTITY);
            segment.BeginWith = @"\";
            segment.EndWith = new string[] { " " };                  // Pokud je za entitou mezera, tak entita končí, a mezeru sežeru (=nevstupuje do následujícího textu). EndWith se vyhodnocuje dříve než Permitteds, takže mezeru chápu jako End a sežeru ji, kdežto něco jiného chápu jako Non-Permitteds, nesežeru to a vyhodnotím to do dalšího segmentu.
            segment.StopWith = new string[] { @"\", "{", "}" };      // Pokud je za entitou backslash nebo { nebo }, tak entita končí, ale backslash (a další znaky) tam nechám a vyhodnotím jako součást další entity nebo char: \f0\fnil\fcharset238 Calibri;
            segment.Permitteds = ParserSetting.SplitTextToOneCharStringArray(nums + lows + upps + "*");      // Entita smí obsahovat jen číslice a malá a velká základní písmena a *
            ApplySchemeText(segment, SchemeKeywordColorText, null, FormatFontStyle.Regular, null);           // Dávám barvu Keyword do hodnoty Text, ale ne do Keyword. Pro toto nemám specifickou metodu, použiju obecnější variantu.
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // RTF znak Char začíná \' a má dva znaky z množiny 0123456789abcdef:
            segment = new ParserSegmentSetting(RTF_CHAR2);
            segment.BeginWith = @"\'";
            segment.Permitteds = ParserSetting.SplitTextToOneCharStringArray(nums + hexs);
            segment.ContentLength = 2;
            ApplySchemeChar(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region RTF Konstanty
        public const string RTF_ = "Rtf";
        public const string RTF_NONE = "RtfNone";
        public const string RTF_DOCUMENT = "RtfDocument";
        public const string RTF_BLOCK= "RtfBlock";
        public const string RTF_ENTITY = "RtfEntity";
        public const string RTF_CHAR2 = "RtfChar2";
        public const string RTF_CHARUNICODE = "RtfCharUnicode";
        #endregion
        #endregion
        #region Generátor settings pro CSharp
        private static ParserSetting _GetSettingCSharp()
        {
            ParserSetting setting = new ParserSetting("Cs", CS_DOCUMENT);

            ParserSegmentSetting segment;

            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", ";", "+", "-", "*", "/", "<", "=", ">", "<=", ">=", "%", "!", "!=" };
            ParserSegmentSpecialTexts[] specs = new ParserSegmentSpecialTexts[] { 
                new ParserSegmentSpecialTexts(@"\\", @"\"), 
                new ParserSegmentSpecialTexts("\\\"", "\""), 
                new ParserSegmentSpecialTexts(@"\r", "\r"), 
                new ParserSegmentSpecialTexts(@"\n", "\n"), 
                new ParserSegmentSpecialTexts(@"\t", "\t")
            };
            List<string> keywordList = GetKeywordsCSharpLanguage();

            // CS dokument začíná standardním kódem
            segment = new ParserSegmentSetting(CS_DOCUMENT);
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { CS_BLOCK, CS_COMMENT_BLOCK, CS_COMMENT_ROW, CS_XML_COMMENT, CS_REGION };
            segment.Keywords = keywordList.ToArray();
            segment.KeywordsCaseSensitive = true;
            segment.EnableMergeDotSpaceText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment);
            setting.SegmentSettingAdd(segment);

            // CS blok začíná { a končí }, obsahuje vnitřní bloky ({...}), stringy, závorky, komentáře. Je vzorem i pro CS_PARENTHESIS a CS_INDEX.
            segment = new ParserSegmentSetting(CS_BLOCK);
            segment.BeginWith = "{";
            segment.EndWith = new string[] { "}" };
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { CS_BLOCK, CS_STRING, CS_STRING_RAW, CS_CHAR, CS_PARENTHESIS, CS_INDEX, CS_COMMENT_BLOCK, CS_COMMENT_ROW, CS_XML_COMMENT, CS_REGION };
            segment.Keywords = keywordList.ToArray();
            segment.KeywordsCaseSensitive = true;
            segment.EnableMergeDotSpaceText = true;
            ApplySchemeCode(segment);
            ApplySchemeKeyword(segment);
            setting.SegmentSettingAdd(segment);

            // String blok začíná " a končí ", obsahuje speciální znaky ale žádné segmenty:
            segment = new ParserSegmentSetting(CS_STRING);
            segment.BeginWith = "\"";
            segment.EndWith = new string[] { "\"", "\r\n", "\r", "\n" };
            segment.SpecialTexts = specs.ToArray();
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // RAW string začíná @" a končí ", neobsahuje žádné speciální znaky ani žádné segmenty:
            segment = new ParserSegmentSetting(CS_STRING_RAW);
            segment.BeginWith = "@\"";
            segment.EndWith = new string[] { "\"" };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Char začíná \' a končí \':
            segment = new ParserSegmentSetting(CS_CHAR);
            segment.BeginWith = @"\'";
            segment.EndWith = new string[] { "\'" };
            segment.SpecialTexts = specs.ToArray();
            ApplySchemeChar(segment);
            setting.SegmentSettingAdd(segment);

            // Závorky kulaté = skoro jako blok, jen jiný začátek a konec:
            segment = new ParserSegmentSetting(CS_PARENTHESIS, setting.GetSegment(CS_BLOCK));
            segment.BeginWith = "(";
            segment.EndWith = new string[] { ")" };
            setting.SegmentSettingAdd(segment);

            // Závorky hranaté (index) = skoro jako blok, jen jiný začátek a konec:
            segment = new ParserSegmentSetting(CS_INDEX, setting.GetSegment(CS_BLOCK));
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            setting.SegmentSettingAdd(segment);

            // Blokový komentář
            segment = new ParserSegmentSetting(CS_COMMENT_BLOCK);
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            // Řádkový komentář
            segment = new ParserSegmentSetting(CS_COMMENT_ROW);
            segment.BeginWith = "//";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // XML komentář
            segment = new ParserSegmentSetting(CS_XML_COMMENT);
            segment.BeginWith = "///";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentXml(segment);
            setting.SegmentSettingAdd(segment);

            // Záhlaví regionu
            segment = new ParserSegmentSetting(CS_REGION);
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
        #region CS Konstanty
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
        private static ParserSetting _GetSettingXml()
        {
            ParserSetting setting = new ParserSetting("Xml", XML_DOCUMENT);

            ParserSegmentSetting segment;
            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string[] delimiters = new string[] { ",", "+", "-", "*", "=", "(", ")", "[", "]", "!", "?" };

            // Text dokumentu = hodnota uvedená v těle elementů, například : <a href="odkaz">Text dokumentu</a>
            segment = new ParserSegmentSetting(XML_DOCUMENT);
            segment.InnerSegmentsNames = new string[] { XML_HEADER, XML_ELEMENT, XML_COMMENTBLOCK, XML_ENTITY, XML_END_ELEMENT };
            ApplySchemePlainText(segment);
            setting.SegmentSettingAdd(segment);

            // Komentář blokový, začíná <!--, končí -->, nemůže obsahovat vnořené blokové komentáře ani nic dalšího:
            segment = new ParserSegmentSetting(XML_COMMENTBLOCK);
            segment.BeginWith = "<!--";
            segment.EndWith = new string[] { "-->" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            // Element, začíná < a končí >, barevně jde jen o název segmentu, obsahuje volitelně vnořený segment ATTRIBUTE (který začíná mezerou).
            segment = new ParserSegmentSetting(XML_ELEMENT);
            segment.BeginWith = "<";
            segment.EndWith = new string[] { "/>", ">" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // EndElement, začíná </ a končí >, barevně jde jen o název segmentu, obsahuje volitelně vnořený segment ATTRIBUTE (který začíná mezerou) - i když to není přípustné, tak se to tak koloruje.
            segment = new ParserSegmentSetting(XML_END_ELEMENT);
            segment.BeginWith = "</";
            segment.EndWith = new string[] { ">" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Attribute se nachází v Element, začíná první mezerou která se najde v Element, má StopWith / nebo >, (tím končí atributy, a tento znak se zpracuje už v rámci Elementu), obsahuje delimitery a blank znaky.
            segment = new ParserSegmentSetting(XML_ATTRIBUTE);
            segment.BeginWith = " ";
            segment.StopWith = new string[] { "/", "?", ">" };
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_VALUE };
            ApplySchemeVariable(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Value, začíná " a končí ", může obsahovat entity
            segment = new ParserSegmentSetting(XML_VALUE);
            segment.BeginWith = "\"";
            segment.EndWith = new string[] { "\"" };
            segment.InnerSegmentsNames = new string[] { XML_ENTITY };
            ApplySchemeString(segment);
            setting.SegmentSettingAdd(segment);

            // Entita: začíná & a končí ; například &nbsp; 
            segment = new ParserSegmentSetting(XML_ENTITY);
            segment.BeginWith = "&";
            segment.StopWith = new string[] { ";" };
            ApplySchemeEntity(segment);
            setting.SegmentSettingAdd(segment);

            // Hlavička, začíná <?  a končí ?>, pravidla má shodná jako element:
            segment = new ParserSegmentSetting(XML_HEADER);
            segment.BeginWith = "<?";
            segment.EndWith = new string[] { "?>" };
            segment.Delimiters = delimiters.ToArray();
            segment.InnerSegmentsNames = new string[] { XML_ATTRIBUTE };
            ApplySchemeText(segment, SchemeKeywordColorText, null, FormatFontStyle.Italic, FormatFontStyle.ItalicEnd);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region XML Konstanty
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
        private static ParserSetting _GetSettingMsdn()
        {
            ParserSetting setting = new ParserSetting("MSDN", MSDN_TEXT);

            ParserSegmentSetting segment;

            string[] blanks = new string[] { " ", "\t", "\r", "\n" };
            string varset = "::=";
            string repeater = "...n";

            // MSDN dokument začíná standardním textem, obsahuje cokoliv
            segment = new ParserSegmentSetting(MSDN_TEXT);
            ParserSegmentSetting segmentText = segment;        // Slouží jako vzor pro některé další podobné segmenty
            segment.Blanks = blanks.ToArray();
            segment.Delimiters = new string[] { varset, "(", ")", ",", repeater };
            segment.InnerSegmentsNames = new string[] { MSDN_VARIABLE, MSDN_OPTIONAL, MSDN_SELECTION, MSDN_COMMENT_ROW, MSDN_COMMENT_BLOCK };
            ApplySchemeCode(segment);
            setting.SegmentSettingAdd(segment);

            // MSDN název proměnné začíná < a končí >, neobsahuje nic jiného
            segment = new ParserSegmentSetting(MSDN_VARIABLE);
            segment.BeginWith = "<";
            segment.EndWith = new string[] { ">" };
            segment.Blanks = blanks.ToArray();
            ApplySchemeVariable(segment);
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Optional blok začíná [ a končí ], obsahuje vše:
            segment = new ParserSegmentSetting(MSDN_OPTIONAL, segmentText);
            segment.BeginWith = "[";
            segment.EndWith = new string[] { "]" };
            // segment.Illegal = new string[] { ">", "}" };
            segment.Delimiters = new string[] { "(", ")", ",", repeater };
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Selection blok začíná { a končí }, jako oddělovač může mít |, obsahuje všechny další segmenty:
            segment = new ParserSegmentSetting(MSDN_SELECTION, segmentText);
            segment.BeginWith = "{";
            segment.EndWith = new string[] { "}" };
            // segment.Illegal = new string[] { ">", "]" };
            segment.Delimiters = new string[] { "|", "(", ")", ",", repeater };
            ApplySchemeDelimiter(segment);
            setting.SegmentSettingAdd(segment);

            // Řádkový komentář
            segment = new ParserSegmentSetting(MSDN_COMMENT_ROW);
            segment.BeginWith = "//";
            segment.EndWith = new string[] { "\r\n", "\r", "\n" };
            segment.IsComment = true;
            ApplySchemeCommentRow(segment);
            setting.SegmentSettingAdd(segment);

            // Blokový komentář
            segment = new ParserSegmentSetting(MSDN_COMMENT_BLOCK);
            segment.BeginWith = "/*";
            segment.EndWith = new string[] { "*/" };
            segment.IsComment = true;
            ApplySchemeCommentBlock(segment);
            setting.SegmentSettingAdd(segment);

            return setting;
        }
        #region MSDN Konstanty
        public const string MSDN_SETTING = "MsdnSetting";
        public const string MSDN_TEXT = "MsdnText";
        public const string MSDN_VARIABLE = "MsdnVariable";
        public const string MSDN_OPTIONAL = "MsdnOptional";
        public const string MSDN_SELECTION = "MsdnSelection";
        public const string MSDN_COMMENT_ROW = "MsdnCommentRow";
        public const string MSDN_COMMENT_BLOCK = "MsdnCommentBlock";
        #endregion
        #endregion
    }
}
