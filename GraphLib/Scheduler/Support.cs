using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

using Asol.Tools.WorkScheduler.Data;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Scheduler
{
    #region class WorkSchedulerSupport : Třída obsahující konstanty a další podporu WorkScheduleru
    /// <summary>
    /// WorkSchedulerSupport : Třída obsahující konstanty a další podporu WorkScheduleru
    /// </summary>
    public class WorkSchedulerSupport
    {
        #region Konstanty
        /// <summary>
        /// "ASOL": Autor pluginu
        /// </summary>
        public const string MS_PLUGIN_AUTHOR = "ASOL";
        /// <summary>
        /// "GreenForMsPlugin": Název pluginu
        /// </summary>
        public const string MS_PLUGIN_NAME = "WorkScheduler";
        /// <summary>
        /// 0: číslo funkce "???"
        /// </summary>
        public const int MS_LICENSE_FUNC_NUMBER = 0;

        /// <summary>
        /// Command pro start pluginu
        /// </summary>
        public const string CMD_START_PLUGIN = "StartPlugin";
        /// <summary>
        /// Key v Request: "Data"
        /// </summary>
        public const string KEY_REQUEST_DATA = "Data";
        /// <summary>
        /// Key v Request: "DataDeclaration"
        /// </summary>
        public const string KEY_REQUEST_DATA_DECLARATION = "DataDeclaration";
        /// <summary>
        /// Název tabulky "DataDeclaration"
        /// </summary>
        public const string DATA_DECLARATION_NAME = "DataDeclaration";
        /// <summary>
        /// Struktura tabulky "DataDeclaration"
        /// </summary>
        public const string DATA_DECLARATION_STRUCTURE = "data_id int; target string; content string; name string; title string; tooltip string; image string; data string";
        /// <summary>
        /// Key v Request: "Table.{{Name}}.Row"
        /// </summary>
        public const string KEY_REQUEST_TABLE_ROW = "Table.{{Name}}.Row";
        /// <summary>
        /// Key v Request: "Table.{{Name}}.Graph"
        /// </summary>
        public const string KEY_REQUEST_TABLE_GRAPH = "Table.{{Name}}.Graph";
        /// <summary>
        /// Key v Request: "Table.{{Name}}.Rel"
        /// </summary>
        public const string KEY_REQUEST_TABLE_REL = "Table.{{Name}}.Rel";
        /// <summary>
        /// Key v Request: "Table.{{Name}}.Item"
        /// </summary>
        public const string KEY_REQUEST_TABLE_ITEM = "Table.{{Name}}.Item";
        /// <summary>
        /// Pattern v KEY_REQUEST_TABLE_???, na jehož místo se vloží název tabulky
        /// </summary>
        public const string KEY_REQUEST_PATTERN_TABLENAME = "{{Name}}";
        /// <summary>
        /// Struktura tabulky "Table.Graph"
        /// </summary>
        public const string DATA_TABLE_GRAPH_STRUCTURE = "parent_rec_id	int; parent_class_id int; item_rec_id int; item_class_id int; group_rec_id int; group_class_id int; data_rec_id int; data_class_id int; layer int; level int; is_user_fixed int; time_begin datetime; time_end datetime; height decimal; back_color string; join_back_color string; edit_mode string";

        /// <summary>
        /// Key v Response: Status
        /// </summary>
        public const string KEY_RESPONSE_RESULT_STATUS = "ResultStatus";
        /// <summary>
        /// Key v Response: Message
        /// </summary>
        public const string KEY_RESPONSE_RESULT_MESSAGE = "ResultMessage";

        /// <summary>
        /// Název GUI panelu: Main
        /// </summary>
        public const string GUI_TARGET_MAIN = "main";
        /// <summary>
        /// Název GUI panelu: Toolbar
        /// </summary>
        public const string GUI_TARGET_TOOLBAR = "toolbar";
        /// <summary>
        /// Název GUI panelu: Task
        /// </summary>
        public const string GUI_TARGET_TASK = "task";
        /// <summary>
        /// Název GUI panelu: Schedule
        /// </summary>
        public const string GUI_TARGET_SCHEDULE = "schedule";
        /// <summary>
        /// Název GUI panelu: Source
        /// </summary>
        public const string GUI_TARGET_SOURCE = "source";
        /// <summary>
        /// Název GUI panelu: Info
        /// </summary>
        public const string GUI_TARGET_INFO = "info";
        /// <summary>
        /// Název GUI obsahu: nic
        /// </summary>
        public const string GUI_CONTENT_NONE = "";
        /// <summary>
        /// Název GUI obsahu: Button
        /// </summary>
        public const string GUI_CONTENT_BUTTON = "button";
        /// <summary>
        /// Název GUI obsahu: Table
        /// </summary>
        public const string GUI_CONTENT_TABLE = "table";
        /// <summary>
        /// Název GUI obsahu: Function
        /// </summary>
        public const string GUI_CONTENT_FUNCTION = "function";

        #endregion
        #region Podpora tvorby tabulek a sloupců
        /// <summary>
        /// Vrátí DataTable daného jména a obsahující dané sloupce.
        /// Sloupce jsou zadány jedním stringem ve formě: "název typ, název typ, ...", kde typ je název datového typu dle níže uvedeného soupisu.
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static DataTable CreateTable(string tableName, string structure)
        {
            DataTable table = new DataTable();
            table.TableName = tableName;

            Tuple<string, Type>[] columns = ParseTableStructure(structure);
            foreach (Tuple<string, Type> column in columns)
                table.Columns.Add(column.Item1, column.Item2);

            return table;
        }
        /// <summary>
        /// Metoda vyhodí chybu s odpovídající zprávou, pokud daná tabulka není v pořádku (je null, anebo ne obsahuje všechny dané sloupce (ověřuje jejich název a typ)).
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static void CheckTable(DataTable table, string structure)
        {
            string message = _VerifyTable(table, structure);
            if (message == null) return;
            throw new InvalidDataException(message);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná tabulka je v pořádku = není null, a obsahuje všechny dané sloupce (ověřuje jejich název a typ).
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool VerifyTable(DataTable table, string structure)
        {
            string message = _VerifyTable(table, structure);
            return (message == null);
        }
        /// <summary>
        /// Metoda vrátí true, pokud daná tabulka je v pořádku = není null, a obsahuje všechny dané sloupce (ověřuje jejich název a typ).
        /// </summary>
        /// <param name="table"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        private static string _VerifyTable(DataTable table, string structure)
        {
            if (table == null) return "DataTable is null";

            Tuple<string, Type>[] columns = ParseTableStructure(structure);
            foreach (Tuple<string, Type> column in columns)
            {
                if (!table.Columns.Contains(column.Item1)) return "DataTable <" + table.TableName + "> does not contain column <" + column.Item1 + ">";
                if (table.Columns[column.Item1].DataType != column.Item2) return "Column <" + table.TableName + "> in DataTable <" + table.TableName + "> is not of type <" + column.Item2.Name + ">";
            }

            return null;
        }
        /// <summary>
        /// Metoda z textové podoby struktury vrací typově definované pole, které obsahuje zadanou strukturu.
        /// Sloupce jsou zadány jedním stringem ve formě: "název typ, název typ, ...", kde typ je název datového typu dle níže uvedeného soupisu.
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static Tuple<string, Type>[] ParseTableStructure(string structure)
        {
            List<Tuple<string, Type>> columnList = new List<Tuple<string, Type>>();
            if (!String.IsNullOrEmpty(structure))
            {
                char[] columnDelimiters = ",;".ToCharArray();
                char[] partDelimiters = " :".ToCharArray();
                string[] columns = structure.Split(columnDelimiters, StringSplitOptions.RemoveEmptyEntries);
                int count = columns.Length;
                for (int i = 0; i < count; i++)
                {
                    string[] column = columns[i].Trim().Split(partDelimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (column.Length != 2) continue;
                    string name = column[0];
                    if (String.IsNullOrEmpty(name)) continue;
                    Type type = GetTypeFromName(column[1]);
                    if (type == null) continue;
                    columnList.Add(new Tuple<string, Type>(name, type));
                }
            }
            return columnList.ToArray();
        }
        /// <summary>
        /// Vrátí Type pro daný název typu.
        /// Detekuje tyto typy:
        /// string = char = text = varchar = nvarchar; sbyte; short = int16; int = int32; long = int64; byte; ushort = uint16; uint = uint32; ulong = uint64;
        /// single = float; double; decimal = numeric; bool = boolean; datetime = date; binary = image = picture.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetTypeFromName(string typeName)
        {
            if (String.IsNullOrEmpty(typeName)) return null;
            typeName = typeName.Trim().ToLower();
            switch (typeName)
            {
                case "string":
                case "char":
                case "text":
                case "varchar":
                case "nvarchar":
                    return typeof(String);

                case "sbyte":
                    return typeof(SByte);

                case "short":
                case "int16":
                    return typeof(Int16);

                case "int":
                case "int32":
                    return typeof(Int32);

                case "long":
                case "int64":
                    return typeof(Int64);

                case "byte":
                    return typeof(Byte);

                case "ushort":
                case "uint16":
                    return typeof(UInt16);

                case "uint":
                case "uint32":
                    return typeof(UInt32);

                case "ulong":
                case "uint64":
                    return typeof(UInt64);

                case "single":
                case "float":
                    return typeof(Single);

                case "double":
                    return typeof(Double);

                case "decimal":
                case "numeric":
                    return typeof(Decimal);

                case "bool":
                case "boolean":
                    return typeof(Boolean);

                case "datetime":
                case "date":
                    return typeof(DateTime);

                case "binary":
                case "image":
                case "picture":
                    return typeof(Byte[]);

            }

            return null;
        }
        #endregion
        #region Serializace a Deserializace DataTable
        /// <summary>
        /// Serializuje tabulku. Z objektu DataTable vrátí text.
        /// Text lze převést na tabulku metodou TableDeserialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string TableSerialize(DataTable table)
        {
            if (table == null) return null;
            try
            {
                StringBuilder sb = new StringBuilder();
                using (System.IO.StringWriter writer = new System.IO.StringWriter(sb))
                {
                    table.WriteXml(writer, XmlWriteMode.WriteSchema);
                }
                return sb.ToString();
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Zadanou tabulku (table) není možno serializovat do stringu. Při serializaci je hlášena chyba " + exc.Message + ".");
            }
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vrátí objekt DataTable. 
        /// Text má být vytvořen metodou TableSerialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Tato metoda při chybě hodí chybu, jinak vrátí Table. Nikdy nevrací null.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable TableDeserialize(string data)
        {
            return _TableDeserialize(data, false);
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vytvoří objekt DataTable a vrátíé true.
        /// Text má být vytvořen metodou TableSerialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Tato metoda při chybě vrátí false a do out parametru table nechá null. Nikdy nehodí chybu.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool TryTableDeserialize(string data, out DataTable table)
        {
            table = _TableDeserialize(data, true);
            return (table != null);
        }
        /// <summary>
        /// Deserializuje tabulku. Z textu vrátí objekt DataTable. 
        /// Text má být vytvořen metodou TableSerialize().
        /// Používají se o párové metody na instanci třídy DataTable : ReadXml() a WriteXml().
        /// Při chybě se chová podle parametru ignoreErrors.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ignoreErrors"></param>
        /// <returns></returns>
        private static DataTable _TableDeserialize(string data, bool ignoreErrors)
        {
            DataTable table = null;
            string message = null;
            if (String.IsNullOrEmpty(data))
                message = "TableDeserialize: Zadaný řetězec (data) není možno převést do formátu DataTable, řetězec je prázdný.";
            else
            {
                try
                {
                    table = new DataTable();
                    using (System.IO.StringReader reader = new System.IO.StringReader(data))
                    {
                        table.ReadXml(reader);
                    }
                }
                catch (Exception exc)
                {
                    table = null;
                    message = "TableDeserialize: Zadaný řetězec (data) není možno převést do formátu DataTable. Při deserializaci je hlášena chyba " + exc.Message + ".";
                }
            }
            if (table == null && !ignoreErrors)
                throw new ArgumentException(message);
            return table;
        }
        #endregion
        #region Komprimace a dekomprimace stringu
        /// <summary>
        /// Metoda vrátí daný string KOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený do Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-5% původní délky stringu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Compress(string source)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Text.Encoding.UTF8.GetBytes(source);
            using (System.IO.MemoryStream inpStream = new MemoryStream(inpBuffer))
            using (System.IO.MemoryStream outStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
                {
                    inpStream.CopyTo(zipStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                target = System.Convert.ToBase64String(outBuffer, Base64FormattingOptions.InsertLineBreaks);
            }
            return target;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-8% původní délky stringu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Decompress(string source)
        {
            if (source == null || source.Length == 0) return source;

            string target = null;
            byte[] inpBuffer = System.Convert.FromBase64String(source);
            using (System.IO.MemoryStream inpStream = new MemoryStream(inpBuffer))
            using (System.IO.MemoryStream outStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream zipStream = new System.IO.Compression.GZipStream(inpStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    zipStream.CopyTo(outStream);
                }   // Obsah streamu outStream je použitelný až po Dispose streamu GZipStream !
                outStream.Flush();
                byte[] outBuffer = outStream.ToArray();
                target = System.Text.Encoding.UTF8.GetString(outBuffer);
            }
            return target;

        }
        #endregion
        #region Odesílání a příjem datového balíku
        /// <summary>
        /// Vytvoří a vrátí <see cref="DataBuffer"/> pro zápis dat.
        /// Data se do něj vkládají metodami <see cref="DataBuffer.WriteText(string, string)"/>atd, zapsaný text se získá v property <see cref="DataBuffer.WrittenContent"/>.
        /// </summary>
        /// <returns></returns>
        public static DataBuffer CreateDataBufferWriter()
        {
            return new DataBuffer();
        }
        /// <summary>
        /// Vytvoří a vrátí <see cref="DataBuffer"/> pro čtení dat.
        /// Vstupní text se předává do této metody.
        /// Data se čtou metodami 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static DataBuffer CreateDataBufferReader(string content)
        {
            return new DataBuffer(content);
        }
        /// <summary>
        /// Buffer pro zápis a čtení dat
        /// </summary>
        public class DataBuffer : IDisposable
        {
            #region Konstrukce, buffer, Dispose
            internal DataBuffer()
            {
                this._StringBuilder = new StringBuilder();
                this._Writer = new StringWriter(this._StringBuilder);
            }
            internal DataBuffer(string data)
            {
                this._Reader = new StringReader(data);
            }
            private System.Text.StringBuilder _StringBuilder;
            private System.IO.StringWriter _Writer;
            private System.IO.StringReader _Reader;
            void IDisposable.Dispose()
            {
                try
                {
                    if (this._Writer != null)
                    {
                        this._Writer.Close();
                        this._Writer.Dispose();
                        this._Writer = null;
                    }
                    if (this._StringBuilder != null)
                    {
                        this._StringBuilder = null;
                    }
                    if (this._Reader != null)
                    {
                        this._Reader.Close();
                        this._Reader.Dispose();
                        this._Reader = null;
                    }
                }
                catch { }
            }
            #endregion
            #region Write: Zápis dat
            /// <summary>
            /// true pokud this Buffer je v režimu Write = umožní zapisovat, ale ne číst
            /// </summary>
            public bool IsWritter { get { return (this._Writer != null); } }
            /// <summary>
            /// Do bufferu zapíše data, která získá komprimací daného textu.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            public void WriteText(string key, string text)
            {
                this.WriteText(key, text, false);
            }
            /// <summary>
            /// Do bufferu zapíše data, která získá komprimací daného textu.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            /// <param name="verify"></param>
            public void WriteText(string key, string text, bool verify)
            {
                this._CheckWritter("WriteText method");
                string data = WorkSchedulerSupport.Compress(text);
                if (verify)
                {
                    string test = WorkSchedulerSupport.Decompress(data);
                    if (test.Length != text.Length || test != text)
                        throw new InvalidOperationException("WorkSchedulerSupport.Compress() and Decompress() error.");
                }
                this.WriteData(key, data);
            }
            /// <summary>
            /// Do bufferu zapíše komprimovaná data
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data"></param>
            public void WriteData(string key, string data)
            {
                this._CheckWritter("WriteData method");
                key = key
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("<", "{")
                    .Replace(">", "}");
                this._Writer.WriteLine(KEY_BEGIN);
                this._Writer.WriteLine(key);
                this._Writer.WriteLine(KEY_END);
                this._Writer.WriteLine(DATA_BEGIN);
                this._Writer.WriteLine(data);
                this._Writer.WriteLine(DATA_END);
                this._Writer.WriteLine();
            }
            /// <summary>
            /// Obsahuje aktuálně zapsaná data, ale pouze v režimu <see cref="IsWritter"/>.
            /// </summary>
            public string WrittenContent
            {
                get
                {
                    this._CheckWritter("WrittenContent property");
                    this._Writer.Flush();
                    return this._StringBuilder.ToString();
                }
            }
            /// <summary>
            /// Ověří, že this Buffer je v režimu <see cref="IsWritter"/>.
            /// Pokud není, vyhodí chybu.
            /// </summary>
            /// <param name="usedMember"></param>
            private void _CheckWritter(string usedMember)
            {
                if (!this.IsWritter)
                    throw new InvalidOperationException($"Instance of DataBuffer is not in Writer mode. Using the {usedMember} is not possible.");
            }
            #endregion
            #region Read: čtení dat
            /// <summary>
            /// true pokud this Buffer je v režimu Read = umožní číst, ale ne zapisovat
            /// </summary>
            public bool IsReader { get { return (this._Reader != null); } }
            /// <summary>
            /// Metoda najde v textu nejbližší klíč a jeho obsah, načte je, data dekomrpimuje, a vepíše do out parametrů, pak vrací true.
            /// Pokud nic nemá (došla data), vrací false.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="text"></param>
            /// <returns></returns>
            public bool ReadNextText(out string key, out string text)
            {
                this._CheckReader("ReadNextText method");
                text = null;
                string data;
                if (!this._ReadNextBlock(out key, out data)) return false;

                text = WorkSchedulerSupport.Decompress(data);
                return true;
            }
            /// <summary>
            /// Metoda najde v textu nejbližší klíč a jeho obsah, načte je, načtený obsah nezmění (neprovede dekomrpimaci), a vepíše do out parametrů, pak vrací true.
            /// Pokud nic nemá (došla data), vrací false.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public bool ReadNextData(out string key, out string data)
            {
                this._CheckReader("ReadNextText method");
                data = null;
                if (!this._ReadNextBlock(out key, out data)) return false;
                return true;
            }
            /// <summary>
            /// Metoda z bloku dat načte key a value, kde value je prostý načtený blok dat (bez dekomprimace).
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private bool _ReadNextBlock(out string key, out string value)
            {
                key = null;
                value = null;
                if (this._ReaderIsEnd) return false;

                StringBuilder content = new StringBuilder();

                _ReadState currState = _ReadState.Before;            // state obsahuje stav, který byl před chvilkou, před řádkem který načteme do line.
                while (!(currState == _ReadState.DataEnd || currState == _ReadState.Incorrect))         // Pokud najdu stav Konec dat nebo Chyba, tak nebudu pokračovat.
                {
                    string line = this._Reader.ReadLine();
                    if (line == null)
                    {
                        this._ReaderIsEnd = true;                    // Úplný konec dat (fyzický)
                        break;
                    }
                    _ReadState lineState = _ReadDetectLine(line);    // Čemu odpovídá načtený řádek
                    if (lineState == _ReadState.Empty) continue;     // Prázdné přeskočím

                    switch (currState)                               // Stavový automat vychází ze stavu před řádkem line
                    {
                        case _ReadState.Before:
                            if (lineState == _ReadState.KeyBegin) currState = _ReadState.KeyBegin;
                            break;
                        case _ReadState.KeyBegin:
                            if (lineState == _ReadState.Other)
                            {
                                key = line;
                                currState = _ReadState.Key;
                            }
                            break;
                        case _ReadState.Key:
                            if (lineState == _ReadState.KeyEnd) currState = _ReadState.KeyEnd;
                            break;
                        case _ReadState.KeyEnd:
                            if (lineState == _ReadState.DataBegin) currState = _ReadState.DataBegin;
                            break;
                        case _ReadState.DataBegin:
                        case _ReadState.Data:
                            if (lineState == _ReadState.Other)
                            {
                                content.AppendLine(line);
                                currState = _ReadState.Data;
                            }
                            else if (lineState == _ReadState.DataEnd) currState = _ReadState.DataEnd;
                            else currState = _ReadState.Incorrect;             // Pokud uprostřed dat najdu nějaký jiný klíčový text, který nečekám, pak skončím s chybou
                            break;
                        case _ReadState.DataEnd:
                            break;
                    }
                }

                if (currState == _ReadState.DataEnd)
                {
                    value = content.ToString();
                    return true;
                }
                return false;
            }
            private static _ReadState _ReadDetectLine(string line)
            {
                if (String.IsNullOrEmpty(line)) return _ReadState.Empty;
                if (String.Equals(line, KEY_BEGIN, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.KeyBegin;
                if (String.Equals(line, KEY_END, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.KeyEnd;
                if (String.Equals(line, DATA_BEGIN, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.DataBegin;
                if (String.Equals(line, DATA_END, StringComparison.InvariantCultureIgnoreCase)) return _ReadState.DataEnd;
                return _ReadState.Other;
            }
            private enum _ReadState { Before, Empty, KeyBegin, Key, KeyEnd, DataBegin, Data, DataEnd, Other, Incorrect }
            /// <summary>
            /// true, pokud Reader došel na konec vstupních dat, a už nic dalšího nepřečte.
            /// </summary>
            public bool ReaderIsEnd
            {
                get
                {
                    this._CheckReader("ReaderIsEnd property");
                    return this._ReaderIsEnd;
                }
            }
            private bool _ReaderIsEnd;
            /// <summary>
            /// Ověří, že this Buffer je v režimu <see cref="IsWritter"/>.
            /// Pokud není, vyhodí chybu.
            /// </summary>
            /// <param name="usedMember"></param>
            private void _CheckReader(string usedMember)
            {
                if (!this.IsReader)
                    throw new InvalidOperationException($"Instance of DataBuffer is not in Reader mode. Using the {usedMember} is not possible.");
            }
            #endregion
            #region Konstanty
            protected const string KEY_BEGIN = "<Key>";
            protected const string KEY_END = "</Key>";
            protected const string DATA_BEGIN = "<Data>";
            protected const string DATA_END = "</Data>";
            #endregion
        }
        #endregion
    }
    #endregion
    #region class DataColumnExtendedInfo : Třída obsahující rozšířené informace o jednom sloupci tabulky
    /// <summary>
    /// DataColumnExtendedInfo : Třída obsahující rozšířené informace o jednom sloupci tabulky <see cref="DataColumn"/>),
    /// které do objektu <see cref="DataColumn"/>.ExtendedProperties přidává Helios Green po načtení dat z přehledové šablony.
    /// </summary>
    public class DataColumnExtendedInfo
    {
        #region Konstrukce a načtení dat
        /// <summary>
        /// Vrací informace o daném sloupci, které načte z Extended properties daného sloupce 
        /// (tam je uložit Helios Green v metodě <see cref="BrowseTemplateInfo.GetTemplateData(int, int?, int?, BigFilter, int?)"/>).
        /// Referenci na sloupec, předaný sem jako parametr, si this instance neukládá, data z něj v této metodě fyzicky načte do svých jednoduchých proměnných.
        /// Sloupec může být poté zahozen, jeho data budou opsána zde.
        /// </summary>
        /// <param name="dataColumn"></param>
        /// <returns></returns>
        public static DataColumnExtendedInfo CreateForColumn(DataColumn dataColumn)
        {
            DataColumnExtendedInfo info = new DataColumnExtendedInfo();
            info._LoadFromDataColumn(dataColumn);
            return info;
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        private DataColumnExtendedInfo() { }
        /// <summary>
        /// Načte data z ownera do properties
        /// </summary>
        private void _LoadFromDataColumn(DataColumn dataColumn)
        {
            this.ClassNumber = GetPropertyValue(dataColumn, "ClassNumber", (int?)null);
            this.TemplateNumber = GetPropertyValue(dataColumn, "TemplateNumber", (int?)null);
            this.Alias = GetPropertyValue(dataColumn, "Alias", "");
            this.AllowRowFilter = GetPropertyValue(dataColumn, "AllowRowFilter", true);
            this.AllowSort = GetPropertyValue(dataColumn, "AllowSort", true);
            this.BrowseColumnType = GetPropertyValue(dataColumn, "BrowseColumnType", "");
            this.CodeName_FromSelect = GetPropertyValue(dataColumn, "CodeName_FromSelect", "");
            this.CodeName_FromTemplate = GetPropertyValue(dataColumn, "CodeName_FromTemplate", "");
            this.ColRelNum = GetPropertyValue(dataColumn, "ColRelNum", 0);
            this.ColType = GetPropertyValue(dataColumn, "ColType", "");
            this.DataTypeRepo = GetPropertyValue(dataColumn, "DataTypeRepo", "");
            this.DataTypeSystem = GetPropertyValue(dataColumn, "DataTypeSystem", "");
            this.Format = GetPropertyValue(dataColumn, "Format", "");
            this.Index = GetPropertyValue(dataColumn, "Index", 0);
            this.IsVisible = GetPropertyValue(dataColumn, "IsVisible", true);
            this.Label = GetPropertyValue(dataColumn, "Label", "");
            this.SortIndex = GetPropertyValue(dataColumn, "SortIndex", (int?)null);
            this.Width = GetPropertyValue(dataColumn, "Width", 0);
            this.RelationClassNumber = GetPropertyValue(dataColumn, "RelationClassNumber", (int?)null);
            this.RelationNumber = GetPropertyValue(dataColumn, "RelationNumber", (int?)null);
            this.RelationSide = GetPropertyValue(dataColumn, "RelationSide", "");
            this.RelationVolumeType = GetPropertyValue(dataColumn, "RelationVolumeType", "");
            this.RelationTableAlias = GetPropertyValue(dataColumn, "RelationTableAlias", "");
        }
        protected int GetPropertyValue(DataColumn dataColumn, string propertyName, int defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        protected int? GetPropertyValue(DataColumn dataColumn, string propertyName, int? defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int32)value;
            int number;
            if (value is string && Int32.TryParse((string)value, out number)) return number;
            return defaultValue;
        }
        protected bool GetPropertyValue(DataColumn dataColumn, string propertyName, bool defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is Boolean) return (Boolean)value;
            if (!(value is String)) return defaultValue;
            string text = ((string)value).Trim();
            return String.Equals(text, "true", StringComparison.InvariantCultureIgnoreCase);
        }
        protected string GetPropertyValue(DataColumn dataColumn, string propertyName, string defaultValue)
        {
            object value;
            if (!this.TryGetPropertyValue(dataColumn, propertyName, out value)) return defaultValue;
            if (value == null) return defaultValue;
            if (value is String) return (String)value;
            return value.ToString();
        }
        protected bool TryGetPropertyValue(DataColumn dataColumn, string propertyName, out object value)
        {
            value = null;
            if (dataColumn == null || dataColumn.ExtendedProperties.Count == 0 || !dataColumn.ExtendedProperties.ContainsKey(propertyName)) return false;
            value = dataColumn.ExtendedProperties[propertyName];
            return true;
        }
        #endregion
        #region Public properties, obsahující hodnoty
        /// <summary>
        /// Číslo třídy, z níž pochází data šablony
        /// </summary>
        public int? ClassNumber { get; private set; }
        /// <summary>
        /// Číslo šablony
        /// </summary>
        public int? TemplateNumber { get; private set; }
        /// <summary>
        /// Alias sloupce = ColumnName
        /// </summary>
        public string Alias { get; private set; }
        /// <summary>
        /// Povolit řádkové filtrování
        /// </summary>
        public bool AllowRowFilter { get; private set; }
        /// <summary>
        /// Povolit třídění
        /// </summary>
        public bool AllowSort { get; private set; }
        /// <summary>
        /// Typ sloupce v přehledu: pomocný, datový, ... Zobrazují se vždy jen sloupce typu DataColumn, ostatní sloupce jsou pomocné.
        /// Aktuálně hodnoty: SubjectNumber, ObjectNumber, DataColumn, RelationHelpfulColumn, TotalCountHelpfulColumn
        /// </summary>
        public string BrowseColumnType { get; private set; }
        /// <summary>
        /// Název sloupce v SQL selectu
        /// </summary>
        public string CodeName_FromSelect { get; private set; }
        /// <summary>
        /// Název sloupce uvedený v definici šablony
        /// </summary>
        public string CodeName_FromTemplate { get; private set; }
        /// <summary>
        /// Číslo vztahu, z něhož pochází tento sloupec
        /// </summary>
        public int ColRelNum { get; private set; }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (může se lišit od Repozitory, upravuje se dle dat, která se načtou do přehledu - DDLB, ...)
        /// </summary>
        public string ColType { get; private set; }
        /// <summary>
        /// Datový typ sloupce - NrsTypes (definice dle Repozitory - to, co je vidět)
        /// </summary>
        public string DataTypeRepo { get; private set; }
        /// <summary>
        /// Datový typ sloupce - c# SystemTypes
        /// </summary>
        public string DataTypeSystem { get; private set; }
        /// <summary>
        /// Formát sloupce v přehledu
        /// </summary>
        public string Format { get; private set; }
        /// <summary>
        /// Vrátí index sloupce v seznamu sloupců. Pokud sloupec do žádného seznamu nepatří, vrátí -1.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Informace o viditelnosti sloupce (zda má být vidět v přehledu)
        /// </summary>
        public bool IsVisible { get; private set; }
        /// <summary>
        /// Nadpis sloupce v přehledu
        /// </summary>
        public string Label { get; private set; }
        /// <summary>
        /// Pořadí sloupce v přehledu - pořadí zobrazení
        /// </summary>
        public int? SortIndex { get; private set; }
        /// <summary>
        /// Šířka sloupce v přehledu
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        /// Číslo třídy vztaženého záznamu v tomto sloupci
        /// </summary>
        public int? RelationClassNumber { get; private set; }
        /// <summary>
        /// Číslo vztahu v tomto sloupci, je rovno <see cref="ColRelNum"/>
        /// </summary>
        public int? RelationNumber { get; private set; }
        /// <summary>
        /// Strana vztahu: Undefined, Left, Right
        /// </summary>
        public string RelationSide { get; private set; }
        /// <summary>
        /// Databáze, kde máme hledat vztah (Product, Archival)
        /// </summary>
        public string RelationVolumeType { get; private set; }
        /// <summary>
        /// Alias tabulky, která nese číslo záznamu ve vztahu pro jeho rozkliknutí.
        /// Typický obsah: "TabGS_1_4".
        /// Jednoduchý návod, kterak vyhledati název sloupce této tabulky, ve kterém jest uloženo číslo záznamu v tomto vztahu:
        /// $"H_RN_{RelationNumber}_{RelationTableAlias}_RN_H", tedy ve výsledku: "H_RN_102037_TabGS_1_4_RN_H".
        /// Zcela stačí načíst obsah property <see cref="RelationRecordColumnName"/>.
        /// </summary>
        public string RelationTableAlias { get; private set; }
        /// <summary>
        /// Název sloupce, který obsahuje číslo záznamu, jehož reference nebo název jsou v aktuálním sloupci zobrazeny.
        /// </summary>
        public string RelationRecordColumnName { get { return (this.RelationNumber != 0 && !String.IsNullOrEmpty(this.RelationTableAlias) ? $"H_RN_{RelationNumber}_{RelationTableAlias}_RN_H" : ""); } }
        /// <summary>
        /// true pokud tento sloupec má být k dispozici uživateli (jeho viditelnost se pak řídí pomocí <see cref="IsVisible"/>),
        /// false pro sloupce "systémové", které se nikdy nezobrazují.
        /// </summary>
        public bool ColumnIsForUser { get { return (!String.IsNullOrEmpty(this.BrowseColumnType) && this.BrowseColumnType == "DataColumn"); } }
        #endregion
    }
    #endregion
}
