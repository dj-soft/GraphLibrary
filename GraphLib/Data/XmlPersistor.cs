using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Collections;

namespace Asol.Tools.WorkScheduler.Data
{
    #region class Persist : Třída, která zajišťuje persistenci dat do / z XML formátu
    /// <summary>
    /// Třída, která zajišťuje persistenci dat do / z XML formátu
    /// </summary>
    public static class Persist
    {
        #region Statické public metody
        /// <summary>
        /// Zajistí persistenci (uložení = serializaci) datového objektu do stringu podle implicitních parametrů.
		/// Vrátí stringovou reprezentaci objektu.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Serialize(object data)
        {
            return Serialize(data, PersistArgs.Default);
        }
        /// <summary>
        /// Zajistí persistenci (uložení = serializaci) datového objektu do stringu podle daných parametrů.
		/// Pokud je v parametru vyplněn soubor (XmlFile), pak je XML uložen do daného souboru, v korektním kódování UTF-8 (včetně záhlaví XML!)
		/// Vrátí stringovou reprezentaci objektu.
		/// </summary>
        /// <param name="data"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string Serialize(object data, PersistArgs parameters)
        {
            return XmlPersist.Serialize(data, parameters);
        }
        /// <summary>
        /// Vytvoří objekt ze serializovaného stavu, který je dodán jako string do této metody.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static object Deserialize(string source)
        {
            return Deserialize(CreateArgs(source));
        }
        /// <summary>
		/// Vytvoří objekt ze serializovaného stavu, který je definován v argumentu (může to být string, nebo soubor).
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object Deserialize(PersistArgs parameters)
        {
            return XmlPersist.Deserialize(parameters);
        }
        /// <summary>
        /// Určená data rekonstruuje a naplní je do předaného objektu.
        /// Pokud objekt je null, vytvoří jej.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="data"></param>
        public static void LoadTo(string source, object data)
        {
            LoadTo(CreateArgs(source), data);
        }
        /// <summary>
        /// Určená data rekonstruuje a naplní je do předaného objektu.
        /// Předaný objekt nemůže být null, protože se nepředává jako reference (ref).
		/// Pokud by měl být objekt null, je třeba využít metodu Persist.Deserialize().
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="data"></param>
        public static void LoadTo(PersistArgs parameters, object data)
        {
            XmlPersist.LoadTo(parameters, data);
        }
        /// <summary>
        /// Hodnoty ze vstupního objektu přenese do cílového objektu.
        /// Používá XML persistenci, takže přenáší jen ty hodnoty, které jsou serializovatelné.
        /// Provádí tedy hluboké klonování.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void CloneTo(object source, object target)
        {
            if (source == null || target == null) return;

            PersistArgs args = PersistArgs.MinimalXml;
            string xmlSource = XmlPersist.Serialize(source, args);
            LoadTo(xmlSource, target);
        }
        /// <summary>
        /// Vrátí defaultní parametry, do nichž naplní source do XmlContent nebo do XmlFile
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static PersistArgs CreateArgs(string source)
        {
            if (source == null) return null;
            PersistArgs parameters = PersistArgs.Default;
            if (source.Trim().StartsWith("<"))
                parameters.XmlContent = source;
            else
                parameters.XmlFile = source;
            return parameters;
        }
        #endregion
    }
    #endregion
    #region class PersistArgs : parametry pro persistenci objektu
    /// <summary>
    /// class PersistArgs : parametry pro persistenci objektu
    /// </summary>
    public class PersistArgs
    {
        public PersistArgs()
        {
            XmlWriterSettings xs = new XmlWriterSettings();
            xs.ConformanceLevel = ConformanceLevel.Document;
            xs.Encoding = Encoding.UTF8;
            xs.CheckCharacters = false;
            xs.Indent = true;
            xs.IndentChars = "  ";
            xs.NewLineHandling = NewLineHandling.Entitize;
            xs.NewLineChars = Environment.NewLine;
            xs.NewLineOnAttributes = false;
            xs.OmitXmlDeclaration = false;
            this.WriterSettings = xs;
        }
        /// <summary>
        /// Defaultní parametry
        /// </summary>
        public static PersistArgs Default
        {
            get { return new PersistArgs(); }
        }
        /// <summary>
        /// Parametry pro vytváření minimálního XML textu
        /// </summary>
        public static PersistArgs MinimalXml
        {
            get
            {
                PersistArgs args = new PersistArgs();
                args.WriterSettings.ConformanceLevel = ConformanceLevel.Document;
                args.WriterSettings.Encoding = Encoding.UTF8;
                args.WriterSettings.CheckCharacters = false;
                args.WriterSettings.Indent = false;
                args.WriterSettings.IndentChars = "";
                args.WriterSettings.NewLineHandling = NewLineHandling.None;
                args.WriterSettings.NewLineChars = Environment.NewLine;
                args.WriterSettings.NewLineOnAttributes = false;
                args.WriterSettings.OmitXmlDeclaration = false;
                return args;
            }
        }
        /// <summary>
        /// Nastavení pro zápis XML
        /// </summary>
        public XmlWriterSettings WriterSettings { get; set; }
        /// <summary>
        /// Soubor pro načtení/uložení XML. Pokud bude null, nebude se načítat/ukládat ze souboru, ale použije se string XmlContent.
        /// </summary>
        public string XmlFile { get; set; }
        /// <summary>
        /// Obsah XML dat.
        /// Při načítání (Load), pokud je určen soubor XmlFile, je obsah uložen sem (obsah souboru se načte a vloží do XmlContent). 
        /// Pokud při Load není soubor XmlFile určen, přebírá se XML text odsud.
        /// Při ukládání (Save) je obsah XML vždy uložen i sem.
        /// </summary>
        public string XmlContent { get; set; }
        /// <summary>
        /// Stav deserializace = obsahuje případné problémy
        /// </summary>
        public XmlDeserializeStatus DeserializeStatus { get; set; }
    }
    public enum XmlDeserializeStatus
    {
        None,
        Processing,
        NotInput,
        BadFormatPersistent,
        BadFormatData,
        BadFormatValue
    }
    #endregion

    // type library:
    #region class TypeLibrary : knihovna informací o datových typech a o postupech jejich konverze. Instancuje se 1x pro save/load, obsahuje položky XmlPersistTypeInfo pro každý nalezený Type.
    /// <summary>
    /// TypeLibrary : knihovna informací o datových typech a o postupech jejich konverze.
    /// Instancuje se 1x pro save/load, obsahuje položky TypeInfo pro každý nalezený Type.
    /// Každý nalezený TypeInfo popisuje datový typ a způsob jeho persistence.
    /// Pokud je typ složený z dalších property, pak TypeInfo obsahuje seznam PropInfo.
    /// </summary>
    internal class TypeLibrary : IDisposable
    {
        #region Konstrukce
        internal TypeLibrary()
        {
            this.TypeInfos = new Dictionary<Type, TypeInfo>();
            this._PrepareTypes();
        }
        private Dictionary<Type, TypeInfo> TypeInfos;
        void IDisposable.Dispose()
        {
            this.TypeInfos.Clear();
            this.TypeInfos = null;

            this._DisposeTypes();
        }
        #endregion
        #region Hledání / vytvoření informací o typu
        /// <summary>
        /// Vrátí informaci o daném typu včetně informací sloužících k řízení jeho persistence.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal TypeInfo GetInfo(Type type)
        {
            TypeInfo info;
            if (this.TypeInfos.TryGetValue(type, out info))
                return info;

            // TypeInfo musím vytvořit, NEmapovat! a ihned vložit do this (Library):
            info = new TypeInfo(this, type);
            this.TypeInfos.Add(type, info);

            // Teprve po vložení do Library mohu typ zmapovat:
            // Proč? Protože existují rekurzivní typy (i vzdáleně rekurzivní), 
            //   a ty by se v opačném případě (nejprve komplet zmapování typu, a pak teprve jeho vložení do Library) zamotaly.
            // Jak? Příklad: Type1 obsahuje property typu Type2, a Type2 obsahuje referenci na svého parenta = Type1.
            // Co by se stalo? Type1 se vytváří, najde property typu Type2, začal by ji mapovat ještě před uložením Type1,
            //   takže by se v tu chvíli začal mapovat opět Type1, atd...
            // Toto řešení (uložit nezmapovaný Type1 do Library a teprve potom jej zmapovat) tomu zabrání:
            info.Fill();
            return info;
        }
        /// <summary>
        /// Pokusí se najít daný Type ve své paměti předdefinovaných typů, anebo detekovat typ jako jeden ze známých, a vrátí jeho XmlPersistenceType.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal XmlPersistenceType GetPersistenceType(Type type, out TypeConvertor typeConvertor)
        {
            if (type.Name == "Boolean?")
            { }
            // Předdefinovaný Type?
            if (this._PresetTypes.TryGetValue(type, out typeConvertor))
                return typeConvertor.PersistenceType;

            if (type.ImplementInterface(typeof(IXmlSerializer))) return XmlPersistenceType.Self;
            if (type.IsEnum) return XmlPersistenceType.Enum;
            if (type.IsArray) return XmlPersistenceType.Array;
            if (type.IsInterface) return XmlPersistenceType.None;
            if (type.ImplementInterface(typeof(IList))) return XmlPersistenceType.IList;
            if (type.ImplementInterface(typeof(IDictionary))) return XmlPersistenceType.IDictionary;

            return XmlPersistenceType.Compound;
        }
        #endregion
        #region Simple types: příprava seznamu _PresetTypes, metoda AddTypeConvertor()
        /// <summary>
        /// Připraví seznam datových typů, které se ukládají Simple
        /// </summary>
        private void _SimpleTypePrepare()
        {
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Boolean), XmlPersistenceType.Simple, Convertor.BooleanToString, Convertor.StringToBoolean));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Byte), XmlPersistenceType.Simple, Convertor.ByteToString, Convertor.StringToByte));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.DateTime), XmlPersistenceType.Simple, Convertor.DateTimeToString, Convertor.StringToDateTime));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.DateTimeOffset), XmlPersistenceType.Simple, Convertor.DateTimeOffsetToString, Convertor.StringToDateTimeOffset));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Decimal), XmlPersistenceType.Simple, Convertor.DecimalToString, Convertor.StringToDecimal));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Double), XmlPersistenceType.Simple, Convertor.DoubleToString, Convertor.StringToDouble));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Guid), XmlPersistenceType.Simple, Convertor.GuidToString, Convertor.StringToGuid));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Char), XmlPersistenceType.Simple, Convertor.CharToString, Convertor.StringToChar));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Int16), XmlPersistenceType.Simple, Convertor.Int16ToString, Convertor.StringToInt16));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Int32), XmlPersistenceType.Simple, Convertor.Int32ToString, Convertor.StringToInt32));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Int64), XmlPersistenceType.Simple, Convertor.Int64ToString, Convertor.StringToInt64));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.IntPtr), XmlPersistenceType.Simple, Convertor.IntPtrToString, Convertor.StringToIntPtr));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.SByte), XmlPersistenceType.Simple, Convertor.SByteToString, Convertor.StringToSByte));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Single), XmlPersistenceType.Simple, Convertor.SingleToString, Convertor.StringToSingle));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.String), XmlPersistenceType.Simple, Convertor.StringToString, Convertor.StringToString));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.TimeSpan), XmlPersistenceType.Simple, Convertor.TimeSpanToString, Convertor.StringToTimeSpan));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.UInt16), XmlPersistenceType.Simple, Convertor.UInt16ToString, Convertor.StringToUInt16));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.UInt32), XmlPersistenceType.Simple, Convertor.UInt32ToString, Convertor.StringToUInt32));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.UInt64), XmlPersistenceType.Simple, Convertor.UInt64ToString, Convertor.StringToUInt64));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.UIntPtr), XmlPersistenceType.Simple, Convertor.UIntPtrToString, Convertor.StringToUIntPtr));

            /*
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlBinary), XmlPersistenceType.Simple, Convertor.SqlBinaryToString, Convertor.StringToSqlBinary));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlBoolean), XmlPersistenceType.Simple, Convertor.SqlBooleanToString, Convertor.StringToSqlBoolean));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlByte), XmlPersistenceType.Simple, Convertor.SqlByteToString, Convertor.StringToSqlByte));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlDateTime), XmlPersistenceType.Simple, Convertor.SqlDateTimeToString, Convertor.StringToSqlDateTime));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlDecimal), XmlPersistenceType.Simple, Convertor.SqlDecimalToString, Convertor.StringToSqlDecimal));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlDouble), XmlPersistenceType.Simple, Convertor.SqlDoubleToString, Convertor.StringToSqlDouble));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlGuid), XmlPersistenceType.Simple, Convertor.SqlGuidToString, Convertor.StringToSqlGuid));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlInt16), XmlPersistenceType.Simple, Convertor.SqlInt16ToString, Convertor.StringToSqlInt16));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlInt32), XmlPersistenceType.Simple, Convertor.SqlInt32ToString, Convertor.StringToSqlInt32));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlInt64), XmlPersistenceType.Simple, Convertor.SqlInt64ToString, Convertor.StringToSqlInt64));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlMoney), XmlPersistenceType.Simple, Convertor.SqlMoneyToString, Convertor.StringToSqlMoney));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlSingle), XmlPersistenceType.Simple, Convertor.SqlSingleToString, Convertor.StringToSqlSingle));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Data.SqlTypes.SqlString), XmlPersistenceType.Simple, Convertor.SqlStringToString, Convertor.StringToSqlString));
            */

            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.Color), XmlPersistenceType.Simple, Convertor.ColorToString, Convertor.StringToColor));
            // this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.CharacterRange), XmlPersistenceType.Simple, null, null));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.Point), XmlPersistenceType.Simple, Convertor.PointToString, Convertor.StringToPoint));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.PointF), XmlPersistenceType.Simple, Convertor.PointFToString, Convertor.StringToPointF));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.Rectangle), XmlPersistenceType.Simple, Convertor.RectangleToString, Convertor.StringToRectangle));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.RectangleF), XmlPersistenceType.Simple, Convertor.RectangleFToString, Convertor.StringToRectangleF));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.Size), XmlPersistenceType.Simple, Convertor.SizeToString, Convertor.StringToSize));
            this.AddTypeConvertor(new TypeConvertor(typeof(System.Drawing.SizeF), XmlPersistenceType.Simple, Convertor.SizeFToString, Convertor.StringToSizeF));

            // Pokud zařadím další typy, musím k nim přidat jejich konvertory.
        }
        /// <summary>
        /// Několik vybraných typů, které se ukládají Simple
        /// </summary>
        private Dictionary<Type, TypeConvertor> _PresetTypes;
        /// <summary>
        /// Připraví seznam datových typů, které se ukládají Simple
        /// </summary>
        private void _PrepareTypes()
        {
            this._PresetTypes = new Dictionary<Type, TypeConvertor>();
            this._SimpleTypePrepare();
        }
        /// <summary>
        /// Přidá/modifikuje daný TypeConvertor
        /// </summary>
        /// <param name="typeConvertor"></param>
        internal void AddTypeConvertor(TypeConvertor typeConvertor)
        {
            if (!this._PresetTypes.ContainsKey(typeConvertor.DataType))
                this._PresetTypes.Add(typeConvertor.DataType, typeConvertor);
            else
                this._PresetTypes[typeConvertor.DataType] = typeConvertor;
        }
        private void _DisposeTypes()
        {
            this._PresetTypes.Clear();
            this._PresetTypes = null;
        }
        #endregion
        #region InstanceCreator

        /// <summary>
        /// Vygeneruje Compound objekt z aktuálního nodu XmlReaderu.
        /// Compound prvek obsahuje pouze jeden atribut : Net.Type="Namespace.TypeName",
        /// hodnota jednotlivých property je ve vnořeném elementu s názvem valueAttributeName.
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="type"></param>
        /// <param name="valueAttributeName"></param>
        /// <returns></returns>
        //private object _CompoundTypeCreate(XmlTextReader xmlReader, Type type, string valueAttributeName)
        //{
        //    object compound = _ObjectCreate(type);             // Vytvoří objekt
        //    if (compound == null) return null;

        //    // Soupis property, které se ukládaly (nebo by se ukládaly za aktuálního stavu objektu):
        //    TypeInfo typeInfo = this.GetInfo(type);
        //    int pc = typeInfo.PropertyList.Count;

        //    // Najdeme vnořený element "Value":
        //    xmlReader.MoveToContent();
        //    using (XmlElementReader eRead = XmlElementReader.ReadCurrentElement(xmlReader))
        //    {
        //        while (eRead.GoNext(true))
        //        {
        //            string el = eRead.CurrentNameLower;
        //            XmlNodeType nt = eRead.XmlReader.NodeType;
        //            if (eRead.XmlReader.NodeType == XmlNodeType.Element && eRead.CurrentNameLower == valueAttributeName.ToLower())
        //            {
        //                // 1. V atributech jsou hodnoty do Simple property:
        //                bool hasAtt = xmlReader.MoveToFirstAttribute();
        //                while (hasAtt)
        //                {
        //                    string attName = xmlReader.LocalName;
        //                    string attValue = xmlReader.ReadContentAsString();

        //                    hasAtt = xmlReader.MoveToNextAttribute();
        //                }

        //                // 2. Zpracujeme elementy, obsahují hodnoty do složených Property:


        //                // Další objekt nemohu hledat a vrátit, skončíme:
        //                eRead.GoEnd();
        //                break;
        //            }
        //        }
        //    }


        //    //if (xmlReader.HasAttributes)
        //    //{
        //    //    for (int i = 0; i < xmlReader.AttributeCount; i++)
        //    //    {
        //    //        xmlReader.MoveToAttribute(i);
        //    //        string attName = xmlReader.LocalName;

        //    //        string attTName = xmlReader.Name;
        //    //        string attValue = xmlReader.ReadContentAsString();
        //    //        xmlReader.GetAttribute(i);
        //    //    }
        //    //}

        //    return compound;


        //}
        /// <summary>
        /// Vytvoří a vrátí objekt daného typu
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private object _ObjectCreate(Type type)
        {
            object result = null;
            ConstructorInfo constructor = this.CheckConstructors(type);     // Ověří, zda Type má bezparametrický konstruktor. Vrátí jej.
            if (constructor != null)
                result = constructor.Invoke(null);
            else
                // Například struktury nemají bezparametrický konstruktor definovaný, proto vrací null. Přesto je lze standardně vytvořit:
                result = System.Activator.CreateInstance(type);
            return result;
        }
        /// <summary>
        /// Metoda ověří, zda typ má bezparametrický konstruktor.
        /// Pokud jej nemá, vyhodí chybu.
        /// Pokud jej má, vrátí jej.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal ConstructorInfo CheckConstructors(Type type)
        {
            if (type.IsClass)
            {
                ConstructorInfo[] typeConsts = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);   // Najdu konstruktory daného Type
                ConstructorInfo[] typeConstNps = typeConsts.Where(c => c.GetParameters().Length == 0).ToArray();     // Najdu jen ty bezparametrické...
                if (typeConstNps.Length == 0)
                    throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " can not be persisted, must be a type with parameterless constructor!");
                return typeConstNps[0];
            }
            if (type.IsInterface)
            {
                throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " is interface. Object can not be created.!");
            }
            if (type.IsValueType || type.IsEnum || type.IsPrimitive)
            {
                return null;
            }
            throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " is unknown type. Object can not be created.!");
        }
        #endregion
        #region Static Type.Assembly podpora
        /// <summary>
        /// Určí text do atributu Type a Assembly pro daný Type.
        /// Pro některé typy není Assembly zapotřebí.
        /// Vrací takové informace, z nichž může reciproční metoda GetTypeFromSerial() vrátit Type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static void GetSerialForType(Type type, out string serialType, out string serialAssembly)
        {
            serialType = type.FullName;
            serialAssembly = null;

            // Někdy si chci uložit i assembly - to v případě, že v type.FullName není explicitně určena, a pro konkrétní datový typ ji budu potřebovat explicitně loadovat (v metodě GetTypeFromSerial()):

            // Pokud už v Type je obsažena explicitní informace o Assembly, pak končím:
            if (serialType.Contains("[") && serialType.Contains(" Version=")) return;

            // Pokud dokážu vytvořit Type jen na základě jeho názvu, pak nemusím ukládat explicitní jméno Assembly:
            Type test = Type.GetType(serialType);
            if (test != null) return;

            // Pokud dokážu vrátit assembly jen na základě názvu typu, pak nemusím ukládat její explicitní jméno:
            Assembly assm = _GetAssemblyFromTypeName(serialType);
            if (assm != null) return;

            // Protože z typu nedokážu určit assembly, musím si ji explicitně uložit teď, když mám typ:
            serialAssembly = type.Assembly.FullName;
        }
        /// <summary>
        /// Ze serializovaných údajů Type a Assembly vrátí Type daného objektu.
        /// </summary>
        /// <param name="serialType"></param>
        /// <param name="serialAssembly"></param>
        /// <returns></returns>
        internal static Type GetTypeFromSerial(string serialType, string serialAssembly)
        {
            if (String.IsNullOrEmpty(serialType)) return null;

            if (String.IsNullOrEmpty(serialAssembly))
            {
                Type result = Type.GetType(serialType);
                if (result != null) return result;

                Assembly assm = _GetAssemblyFromTypeName(serialType);
                if (assm == null) return null;

                return assm.GetType(serialType);
            }

            Assembly assLoad;
            Dictionary<string, Assembly> assDict = _LoadedAssemblyDict;
            if (!assDict.TryGetValue(serialAssembly, out assLoad))
            {
                AssemblyName assName = new AssemblyName(serialAssembly);
                assLoad = Assembly.Load(assName);
                assDict.Add(serialAssembly, assLoad);
            }
            return assLoad.GetType(serialType);
        }
        private static Dictionary<string, Assembly> _LoadedAssemblyDict
        {
            get
            {
                if (__LoadedAssemblyDict == null)
                    __LoadedAssemblyDict = new Dictionary<string, Assembly>();
                return __LoadedAssemblyDict;
            }
        }
        private static Dictionary<string, Assembly> __LoadedAssemblyDict;
        /// <summary>
        /// Zkusí určit Assembly pro daný název typu.
        /// </summary>
        /// <param name="serialType"></param>
        /// <returns></returns>
        private static Assembly _GetAssemblyFromTypeName(string serialType)
        {
            string nmsp = _GetNamespaceFromType(serialType);
            if (nmsp == "System") return typeof(System.Decimal).Assembly;
            if (nmsp == "System.Data") return typeof(System.Data.DataTable).Assembly;
            if (nmsp == "System.Drawing") return typeof(System.Drawing.Font).Assembly;
            if (nmsp == "System.Drawing.Design") return typeof(System.Drawing.Design.ToolboxItem).Assembly;
            if (nmsp == "System.Drawing.Drawing2D") return typeof(System.Drawing.Drawing2D.PathData).Assembly;
            if (nmsp == "System.Drawing.Imaging") return typeof(System.Drawing.Imaging.ColorMap).Assembly;
            if (nmsp == "System.Drawing.Printing") return typeof(System.Drawing.Printing.PageSettings).Assembly;
            if (nmsp == "System.Drawing.Text") return typeof(System.Drawing.Text.GenericFontFamilies).Assembly;
            if (nmsp == "System.Globalization") return typeof(System.Globalization.Calendar).Assembly;
            if (nmsp == "System.IO") return typeof(System.IO.DirectoryInfo).Assembly;

            return null;
        }
        /// <summary>
        /// Vrátí namespace typu, nebo jeho owner class pokud je to vnořená třída.
        /// </summary>
        /// <param name="serialType"></param>
        /// <returns></returns>
        private static string _GetNamespaceFromType(string serialType)
        {
            // Oddělit název typu před [ 
            //   "System.Collections.Generic.List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]" => "System.Collections.Generic.List`1"
            int ptr = serialType.IndexOf('[');
            string typeName = (ptr < 0 ? serialType : (ptr == 0 ? "" : serialType.Substring(0, ptr)));

            // Rozdělit název typu na položky: 
            //   "System.Drawing.Drawing2D.PathGradientBrush" => "System"; "Drawing"; "Drawing2D"; "PathGradientBrush"
            string[] typeItems = serialType.Split('.');
            string result = "";
            for (int t = 0; t < (typeItems.Length - 1); t++)
                result = result + (t == 0 ? "" : ".") + typeItems[t];
            return result;
        }
        #endregion
    }
    #endregion
    #region class TypeInfo : obecné informace o persistování jednoho datového typu. TypeInfo může obsahovat seznam PropInfo, anebo
    /// <summary>
    /// TypeInfo : obecné informace o persistování jednoho datového typu. TypeInfo může obsahovat seznam PropInfo, anebo 
    /// </summary>
    internal class TypeInfo
    {
        internal TypeInfo(TypeLibrary typeLibrary, Type dataType)
        {
            this.TypeLibrary = typeLibrary;
            this.DataType = dataType;
        }
        public override string ToString()
        {
            return this.DataType.Name + ": " + this.PersistenceType.ToString();
        }
        internal void Fill()
        {
            // Základní režim persistence (primitiv / datový objekt / soupis), plus najdu TypeConvertor:
            TypeConvertor typeConvert;
            this.PersistenceType = this.TypeLibrary.GetPersistenceType(this.DataType, out typeConvert);
            this.TypeConvert = typeConvert;

            // Detekce property do seznamu properties:
            switch (this.PersistenceType)
            {
                case XmlPersistenceType.Array:
                    this._DetectArrayItemType();
                    break;
                case XmlPersistenceType.IList:
                case XmlPersistenceType.IDictionary:
                    break;
                case XmlPersistenceType.Compound:
                    this._DetectProperties();
                    break;
            }

            // Detekce generických typů proběhne vždy:
            this._DetectGenericArgs();
        }
        /// <summary>
        /// Reference na knihovnu typů.
        /// V ní jsem uložen já, v ní dohledávám informace pro svoje typy.
        /// </summary>
        internal TypeLibrary TypeLibrary { get; private set; }
        /// <summary>
        /// Můj Type.
        /// </summary>
        internal Type DataType { get; private set; }
        /// <summary>
        /// Typ persistence zdejšího typu (this.DataType).
        /// </summary>
        internal XmlPersistenceType PersistenceType { get; private set; }
        /// <summary>
        /// Type convertor, pokud pro tento typ existuje.
        /// </summary>
        internal TypeConvertor TypeConvert { get; private set; }
        /// <summary>
        /// Soupis property zdejšího typu.
        /// Je naplněn pouze při PersistenceType = XmlPersistenceType.InnerObject.
        /// </summary>
        internal List<PropInfo> PropertyList { get; private set; }
        #region Properties
        /// <summary>
        /// Detekuje PropertyInfo zdejšího typu a vytváří seznam this.PropertyList s prvky třídy PropInfo
        /// </summary>
        private void _DetectProperties()
        {
            this.PropertyList = new List<PropInfo>();
            PropertyInfo[] props = this.DataType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo prop in props)
            {
                PropInfo propData = new PropInfo(this, prop);
                if (!propData.Enabled) continue;

                this.PropertyList.Add(propData);
            }
            // Seznam setřídit:
            this.PropertyList.Sort(PropInfo.CompareByName);
        }
        #endregion
        #region Generika
        /// <summary>
        /// Počet generických argumentů
        /// </summary>
        internal int GenericCount
        {
            get
            {
                Type[] gps = this.DataType.GetGenericArguments();
                if (gps == null || gps.Length == 0) return 0;
                return gps.Length;
            }
        }
        /// <summary>
        /// Vrátí generický typ z dané pozice. Pokud daná pozice není obsazená, vrátí null.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal Type GetGenericType(int index)
        {
            if (this.GenericList != null && index >= 0 && index < this.GenericList.Count)
                return this.GenericList[index];
            return null;
        }
        /// <summary>
        /// Detekuje typy generických parametrů do this.GenericParamList s prvky třídy TypeInfo.
        /// </summary>
        private void _DetectGenericArgs()
        {
            Type[] gps = this.DataType.GetGenericArguments();
            if (gps == null || gps.Length == 0) return;
            this.GenericList = new List<Type>();
            foreach (Type generic in gps)
                this.GenericList.Add(generic);
        }
        /// <summary>
        /// Soupis generických typů zdejšího typu.
        /// Je naplněn pouze při PersistenceType = XmlPersistenceType.InnerObject.
        /// </summary>
        internal List<Type> GenericList { get; private set; }
        #endregion
        #region Array
        /// <summary>
        /// Type prvků v tomto Array, pokud this popisuje standardní Array.
        /// Array má jeden typ, deklaruje se například: Int32[,] x = new Int32[5,12];
        /// Pak typ pole (this.DataType) = typeof(Int32[,]); a typ prvku this.ItemDataType = typeof(Int32);
        /// </summary>
        internal Type ItemDataType { get; private set; }
        /// <summary>
        /// Detekuje typ prvklů standardního Array, do this.ItemDataType
        /// </summary>
        private void _DetectArrayItemType()
        {
            Type arrayType = this.DataType;              // Typ pole, například = typeof(System.Int32[,])
            if (arrayType.HasElementType)
                this.ItemDataType = arrayType.GetElementType();      // Vrátí Type elementu pole, tedy typeof(System.Int32) !!! 
            else if (this.GenericCount > 0)
                this.ItemDataType = this.GetGenericType(0);
            else
                this.ItemDataType = null;
        }
        #endregion
        internal PropInfo FindPropertyByXmlName(string propName)
        {
            return this.PropertyList.FirstOrDefault(p => String.Equals(p.XmlName, propName));
        }
    }
    #endregion
    #region class PropInfo : data o jedné property (jméno property, její typ, její TypeInfo
    /// <summary>
    /// PropInfo : data o jedné property
    /// </summary>
    internal class PropInfo
    {
        internal PropInfo(TypeInfo typeInfo, PropertyInfo property)
        {
            this.TypeInfo = typeInfo;
            this.Property = property;
            this._PropertyTypeInfo = null;

            // Property lze ukládat, pokud má set metodu:
            this.Enabled = (property.GetSetMethod(true) != null);
            this.XmlName = null;

            // Zjistím, zda nejde o property "Djs.Tools.XmlPersistor.IXmlPersistNotify.XmlPersistState" (takovou nebudu persistovat zcela automaticky):
            object[] atts = null;
            // Anebo, zda property má atribut PersistingEnabledAttribute s hodnotou PersistEnable = false, pak by se neukládala:
            if (!this.Enabled || property.Name == "Djs.Tools.XmlPersistor.IXmlPersistNotify.XmlPersistState" || !IsPropertyPersistable(property, out atts))
            {
                this.Enabled = false;
                return;
            }
            if (atts == null)
                atts = property.GetCustomAttributes(typeof(PersistAttribute), true);

            // XmlName (Custom / default):
            object attName = atts.FirstOrDefault(at => at is PropertyNameAttribute);
            if (attName != null)
                this.XmlName = (attName as PropertyNameAttribute).PropertyName;
            if (String.IsNullOrEmpty(this.XmlName))
                this.XmlName = this.Name;
            this.XmlName = XmlPersist.CreateXmlName(this.XmlName);

            // XmlItemName
            object attItem = atts.FirstOrDefault(at => at is CollectionItemNameAttribute);
            if (attItem != null)
                this.XmlItemName = (attItem as CollectionItemNameAttribute).ItemName;
            if (String.IsNullOrEmpty(this.XmlItemName))
                this.XmlItemName = "Item";
            this.XmlItemName = XmlPersist.CreateXmlName(this.XmlItemName);
        }
        /// <summary>
        /// Určí, zda daná property se má persistovat (ukládat + načítat z XML)
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        internal static bool IsPropertyPersistable(PropertyInfo property)
        {
            object[] atts;
            return IsPropertyPersistable(property, out atts);
        }
        /// <summary>
        /// Určí, zda daná property se má persistovat (ukládat + načítat z XML)
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        internal static bool IsPropertyPersistable(PropertyInfo property, out object[] atts)
        {
            // Custom atributy:
            atts = property.GetCustomAttributes(typeof(PersistAttribute), true);
            object attEnabled = atts.FirstOrDefault(at => at is PersistingEnabledAttribute);

            // Pokud existuje atribut PersistingEnabledAttribute, pak vrátím jeho hodnotu PersistEnable:
            if (attEnabled != null)
                return (attEnabled as PersistingEnabledAttribute).PersistEnable;

            // Pokud neexistuje, vrátím true = implicitně persistuji vše:
            return true;
        }
        /// <summary>
        /// Určí, zda daná property se má persistovat (ukládat + načítat z XML)
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        internal static bool IsPropertyCloneable(PropertyInfo property)
        {
            // Custom atributy:
            object[] atts = property.GetCustomAttributes(typeof(PersistAttribute), true);
            object attEnabled = atts.FirstOrDefault(at => at is PersistingEnabledAttribute);

            // Pokud existuje atribut PersistingEnabledAttribute, pak vrátím jeho hodnotu CloneEnable:
            if (attEnabled != null)
                return (attEnabled as PersistingEnabledAttribute).CloneEnable;

            // Pokud neexistuje, vrátím true = implicitně persistuji vše:
            return true;

        }
        public override string ToString()
        {
            string text = this.Property.Name + ": ";
            if (this.Enabled)
                text += "XML name=" + this.XmlName + "; Type=" + this.PropertyType.Name;
            else
                text += "disabled.";
            return text;
        }
        internal static int CompareByName(PropInfo a, PropInfo b)
        {
            return String.Compare(a.Name, b.Name);
        }
        /// <summary>
        /// Vztah na typ, jehož je tato property členem
        /// </summary>
        internal TypeInfo TypeInfo { get; private set; }
        /// <summary>
        /// Reference na knihovnu typů.
        /// V ní je uložen můj TypeInfo, a přes něj se do knihovny dostanu i já.
        /// </summary>
        internal TypeLibrary TypeLibrary { get { return this.TypeInfo.TypeLibrary; } }
        /// <summary>
        /// Info o této property
        /// </summary>
        internal PropertyInfo Property { get; private set; }
        /// <summary>
        /// Název této property = exaktně z this.Property.Name
        /// </summary>
        internal string Name { get { return this.Property.Name; } }
        /// <summary>
        /// .NET Type této property = this.Property.PropertyType.
        /// Jde o typ, jak je property deklarována, nikoli Type, který je v ní aktuálně uložen (to se zjistí až podle objektu s daty).
        /// Může to být potomek zdejšího typu, anebo implementace interface...
        /// </summary>
        internal Type PropertyType { get { return this.Property.PropertyType; } }
        /// <summary>
        /// true, pokud se tato property má persistovat
        /// </summary>
        internal bool Enabled { get; private set; }
        /// <summary>
        /// Rozšířené vlastnosti typu této property - data, načtená z TypeLibrary.
        /// Data se načítají až on demand, z TypeLibrary.
        /// </summary>
        internal TypeInfo PropertyTypeInfo
        {
            get
            {
                if (this._PropertyTypeInfo == null)
                    this._PropertyTypeInfo = this.TypeLibrary.GetInfo(this.PropertyType);
                return this._PropertyTypeInfo;
            }
        }
        private TypeInfo _PropertyTypeInfo;
        /// <summary>
        /// Jméno pro persistenci hodnoty této property
        /// </summary>
        internal string XmlName { get; private set; }
        /// <summary>
        /// Jméno pro persistenci jednotlivých položek kolekce (Item)
        /// </summary>
        internal string XmlItemName { get; private set; }
        #region Vytváření jména elementu - asi zbytečné
        /// <summary>
        /// Ze jména property vytvoří jméno XML elementu/atributu
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        private string _XmlCreateName(string objectName)
        {
            string xmlName = "";
            string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lower = "abcdefghijklmnopqrstuvwxyz";
            string number = "0123456789";
            string diacriticUpp = "ÁĆČĎÉĚËÍĹĽŇÓŔŘŠŤÚŮÝŽ";
            string noDiacritUpp = "ACCDEEEILLNORRSTUUYZ";
            string diacriticLow = "áćčďéěëíĺľňóŕřśšťúůýž";
            string noDiacritLow = "accdeeeillnorrsstuuyz";
            string diacritic = diacriticUpp + diacriticLow;
            string noDiacrit = noDiacritUpp + noDiacritLow;
            _XmlCreateSequence state = _XmlCreateSequence.Begin;
            for (int i = 0; i < objectName.Length; i++)
            {
                string c = objectName.Substring(i, 1);

                // Zrušit diakritiku
                int diacr = diacritic.IndexOf(c);
                if (diacr > 0)
                    c = noDiacrit.Substring(diacr, 0);

                // Pokud mám ve vstupujícím textu podtržítko:
                if (c == "_")
                {
                    if (state != _XmlCreateSequence.AfterUnders)
                    {   // Pokud nejsem po explicitním podtržítku, tak tam první dát můžu (ale další už ne):
                        xmlName += c;
                        state = _XmlCreateSequence.AfterUnders;
                    }
                }
                // Předsadit _ před číslice:
                else if (number.Contains(c))
                {   // Máme číslici:
                    if (state == _XmlCreateSequence.AfterLower || state == _XmlCreateSequence.AfterUpper)
                        // po jakémkoli písmenu => předsadím _, po _ a po číslici nepředsadím:
                        xmlName += "_";
                    xmlName += c.ToLower();
                    state = _XmlCreateSequence.AfterNumber;
                }
                // Řešit konverzi CamelCase => camel_case
                else if (upper.Contains(c))
                {   // Máme velké písmeno:
                    if (state == _XmlCreateSequence.AfterLower || state == _XmlCreateSequence.AfterNumber)
                        // po malém písmenu nebo po číslici => předsadím _, po velkém písmenu a po _ ne:
                        xmlName += "_";
                    xmlName += c.ToLower();
                    state = _XmlCreateSequence.AfterUpper;
                }
                else
                {
                    xmlName += c.ToLower();
                    state = _XmlCreateSequence.AfterLower;
                }
            }
            return xmlName;
        }
        private enum _XmlCreateSequence { Begin, AfterMultiUpper, AfterUpper, AfterLower, AfterNumber, AfterUnders }
        #endregion
    }
    #endregion
    #region Extensions
    internal static class Extensions
    {
        /// <summary>
        /// Vrátí true, pokud this type implementuje daný interface.
        /// Je obdobou testu "if (data is implementInterface)", ale pro případ, kdy nemáme objekt ale jeho Type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="implementInterface"></param>
        /// <returns></returns>
        internal static bool ImplementInterface(this Type type, Type implementInterface)
        {
            return type.GetInterfaces().Any(i => Type.Equals(i, implementInterface));
        }
    }
    #endregion

    // convertor:
    // internal class for conversion string - object
    /// <summary>
    /// Knihovna statických konverzních metod mezi simple typy a stringem
    /// </summary>
    public static class Convertor
    {
        #region Sada krátkých metod pro serializaci a deserializaci Simple typů (jsou vyjmenované v TypeLibrary._SimpleTypePrepare())
        #region System types
        public static string BooleanToString(object value)
        {
            return ((Boolean)value ? "true" : "false");
        }
        public static string BooleanToString(object value, string textTrue, string textFalse)
        {
            return ((Boolean)value ? textTrue : textFalse);
        }
        public static object StringToBoolean(string text)
        {
            return (!String.IsNullOrEmpty(text) && (text.ToLower() == "true" || text == "1" || text.ToLower() == "a" || text.ToLower() == "y"));
        }
        public static string ByteToString(object value)
        {
            return ((Byte)value).ToString();
        }
        public static object StringToByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Byte)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Byte)0;
            Byte b = (Byte)(value & 0x00FF);
            return b;
        }
        public static string DateTimeToString(object value)
        {
            DateTime dateTime = (DateTime)value;
            if (dateTime.Millisecond == 0 && dateTime.Second == 0)
                return dateTime.ToString("D", _Dtfi);
            return dateTime.ToString("F", _Dtfi);
        }
        public static object StringToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            DateTime value;
            if (DateTime.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParseExact(text, "F", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;

            return DateTime.MinValue;
        }
        public static string DateTimeOffsetToString(object value)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.ToString("F", _Dtfi);
        }
        public static object StringToDateTimeOffset(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTimeOffset.MinValue;
            DateTimeOffset value;
            if (!DateTimeOffset.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value)) return DateTimeOffset.MinValue;
            return value;
        }
        public static string DecimalToString(object value)
        {
            return ((Decimal)value).ToString("N", _Nmfi);
        }
        public static object StringToDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Decimal)0;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Decimal)0;
            return value;
        }
        public static string DoubleToString(object value)
        {
            return ((Double)value).ToString("N", _Nmfi);
        }
        public static object StringToDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Double)0;
            Double value;
            if (!Double.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Double)0;
            return value;
        }
        public static string GuidToString(object value)
        {
            return ((Guid)value).ToString("N", _Nmfi);
        }
        public static object StringToGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return Guid.Empty;
            Guid value;
            if (!Guid.TryParse(text, out value)) return Guid.Empty;
            return value;
        }
        public static string CharToString(object value)
        {
            return ((Char)value).ToString();
        }
        public static object StringToChar(string text)
        {
            if (String.IsNullOrEmpty(text)) return Char.MinValue;
            Char value;
            if (!Char.TryParse(text, out value)) return Char.MinValue;
            return value;
        }
        public static string Int16ToString(object value)
        {
            return ((Int16)value).ToString();
        }
        public static object StringToInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int16)0;
            Int16 value;
            if (!Int16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int16)0;
            return value;
        }
        public static string Int32ToString(object value)
        {
            return ((Int32)value).ToString();
        }
        public static object StringToInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        public static string Int64ToString(object value)
        {
            return ((Int64)value).ToString();
        }
        public static object StringToInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int64)0;
            Int64 value;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int64)0;
            return value;
        }
        public static string IntPtrToString(object value)
        {
            return ((IntPtr)value).ToInt64().ToString("G");
        }
        public static object StringToIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (IntPtr)0;
            Int64 int64;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out int64)) return (IntPtr)0;
            return new IntPtr(int64);
        }
        public static string SByteToString(object value)
        {
            return ((SByte)value).ToString();
        }
        public static object StringToSByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (SByte)0;
            SByte value;
            if (!SByte.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (SByte)0;
            return value;
        }
        public static string SingleToString(object value)
        {
            return ((Single)value).ToString("N", _Nmfi);
        }
        public static object StringToSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        public static string StringToString(object value)
        {
            if (value == null) return null;
            return (string)value;
        }
        public static object StringToString(string text)
        {
            if (text == null) return null;
            return text;
        }
        public static string TimeSpanToString(object value)
        {
            return ((TimeSpan)value).ToString("N", _Dtfi);
        }
        public static object StringToTimeSpan(string text)
        {
            if (String.IsNullOrEmpty(text)) return TimeSpan.Zero;
            TimeSpan value;
            if (!TimeSpan.TryParse(text, _Dtfi, out value)) return TimeSpan.Zero;
            return value;
        }
        public static string UInt16ToString(object value)
        {
            return ((UInt16)value).ToString();
        }
        public static object StringToUInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt16)0;
            UInt16 value;
            if (!UInt16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt16)0;
            return value;
        }
        public static string UInt32ToString(object value)
        {
            return ((UInt32)value).ToString();
        }
        public static object StringToUInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt32)0;
            UInt32 value;
            if (!UInt32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt32)0;
            return value;
        }
        public static string UInt64ToString(object value)
        {
            return ((UInt64)value).ToString();
        }
        public static object StringToUInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt64)0;
            UInt64 value;
            if (!UInt64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt64)0;
            return value;
        }
        public static string UIntPtrToString(object value)
        {
            return ((UIntPtr)value).ToUInt64().ToString("G");
        }
        public static object StringToUIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UIntPtr)0;
            UInt64 uint64;
            if (!UInt64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out uint64)) return (UIntPtr)0;
            return new UIntPtr(uint64);
        }
        #endregion
        #region Nullable types
        public static string Int32NToString(object value)
        {
            Int32? v = (Int32?)value;
            return (v.HasValue ? v.Value.ToString() : "null");
        }
        public static object StringToInt32N(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32?)null;
            if (text.ToLower().Trim() == "null") return (Int32?)null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32?)null;
            return (Int32?)value;
        }
        #endregion
        #region Sql Types (removed)
        /*
        public static string SqlBinaryToString(object value)
        {
            if (value == null) return null;
            SqlBinary data = (SqlBinary)value;
            if (data.IsNull) return null;
            return Convert.ToBase64String(data.Value);
        }
        public static object StringToSqlBinary(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlBinary.Null;
            return new SqlBinary(Convert.FromBase64String(text));
        }
        public static string SqlBooleanToString(object value)
        {
            if (value == null) return null;
            SqlBoolean data = (SqlBoolean)value;
            if (data.IsNull) return null;
            return (data.Value ? "true" : "false");
        }
        public static object StringToSqlBoolean(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlBoolean.Null;
            string data = text.Trim().ToLower();
            return (text == "true" ? SqlBoolean.True :
                   (text == "false" ? SqlBoolean.False : SqlBoolean.Null));
        }
        public static string SqlByteToString(object value)
        {
            if (value == null) return null;
            SqlByte data = (SqlByte)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlByte.Null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlByte.Null;
            SqlByte b = new SqlByte((Byte)(value & 0x00FF));
            return b;
        }
        public static string SqlDateTimeToString(object value)
        {
            if (value == null) return null;
            SqlDateTime data = (SqlDateTime)value;
            if (data.IsNull) return null;
            return data.Value.ToString("F", _Dtfi);
        }
        public static object StringToSqlDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDateTime.Null;
            DateTime value;
            if (!DateTime.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value)) return SqlDateTime.Null;
            return new SqlDateTime(value);
        }
        public static string SqlDecimalToString(object value)
        {
            if (value == null) return null;
            SqlDecimal data = (SqlDecimal)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDecimal.Null;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlDecimal.Null;
            return new SqlDecimal(value);
        }
        public static string SqlDoubleToString(object value)
        {
            if (value == null) return null;
            SqlDouble data = (SqlDouble)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlDouble.Null;
            Double value;
            if (!Double.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlDouble.Null;
            return new SqlDouble(value);
        }
        public static string SqlGuidToString(object value)
        {
            if (value == null) return null;
            SqlGuid data = (SqlGuid)value;
            if (data.IsNull) return null;
            return data.Value.ToString("N", _Nmfi);
        }
        public static object StringToSqlGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlGuid.Null;
            Guid value;
            if (!Guid.TryParse(text, out value)) return SqlGuid.Null;
            return new SqlGuid(value);
        }
        public static string SqlInt16ToString(object value)
        {
            if (value == null) return null;
            SqlInt16 data = (SqlInt16)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt16.Null;
            Int16 value;
            if (!Int16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt16.Null;
            return new SqlInt16(value);
        }
        public static string SqlInt32ToString(object value)
        {
            if (value == null) return null;
            SqlInt32 data = (SqlInt32)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt32.Null;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt32.Null;
            return new SqlInt32(value);
        }
        public static string SqlInt64ToString(object value)
        {
            if (value == null) return null;
            SqlInt64 data = (SqlInt64)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlInt64.Null;
            Int64 value;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlInt64.Null;
            return new SqlInt64(value);
        }
        public static string SqlMoneyToString(object value)
        {
            if (value == null) return null;
            SqlMoney data = (SqlMoney)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlMoney(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlMoney.Null;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlMoney.Null;
            return new SqlMoney(value);
        }
        public static string SqlSingleToString(object value)
        {
            if (value == null) return null;
            SqlSingle data = (SqlSingle)value;
            if (data.IsNull) return null;
            return data.Value.ToString();
        }
        public static object StringToSqlSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlSingle.Null;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return SqlSingle.Null;
            return new SqlSingle(value);
        }
        public static string SqlStringToString(object value)
        {
            if (value == null) return null;
            SqlString data = (SqlString)value;
            if (data.IsNull) return null;
            return data.Value;
        }
        public static object StringToSqlString(string text)
        {
            if (String.IsNullOrEmpty(text)) return SqlString.Null;
            return text;
        }
        */
        #endregion
        #region Drawing Types
        public static string ColorToString(object value)
        {
            if (value is KnownColor)
            {
                KnownColor knownColor = (KnownColor)value;
                return System.Enum.GetName(typeof(KnownColor), knownColor);
            }
            if (!(value is Color))
                return "";
            Color data = (Color)value;
            if (data.IsKnownColor)
                return System.Enum.GetName(typeof(KnownColor), data.ToKnownColor());
            if (data.IsNamedColor)
                return data.Name;
            if (data.IsSystemColor)
                return "System." + data.ToString();
            if (data.A < 255)
                return ("#" + data.A.ToString("X2") + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
            return ("#" + data.R.ToString("X2") + data.G.ToString("X2") + data.B.ToString("X2")).ToUpper();
        }
        public static object StringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo hexa "#FF808040" (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t);
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t);
            return StringNameToColor(t);
        }
        private static object StringNameToColor(string name)
        {
            KnownColor known;
            if (System.Enum.TryParse<KnownColor>(name, out known))
                return Color.FromKnownColor(known);

            try
            {
                return Color.FromName(name);
            }
            catch
            { }
            return Color.Empty;
        }
        private static object StringARgbToColor(string t)
        {
            int a = HexadecimalToInt32(t.Substring(1, 2));
            int r = HexadecimalToInt32(t.Substring(3, 2));
            int g = HexadecimalToInt32(t.Substring(5, 2));
            int b = HexadecimalToInt32(t.Substring(7, 2));
            return Color.FromArgb(a, r, g, b);
        }
        private static object StringRgbToColor(string t)
        {
            int r = HexadecimalToInt32(t.Substring(1, 2));
            int g = HexadecimalToInt32(t.Substring(3, 2));
            int b = HexadecimalToInt32(t.Substring(5, 2));
            return Color.FromArgb(r, g, b);
        }
        public static string PointToString(object value)
        {
            Point data = (Point)value;
            return data.X.ToString() + ";" + data.Y.ToString();
        }
        public static object StringToPoint(string text)
        {
            if (String.IsNullOrEmpty(text)) return Point.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return Point.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            return new Point(x, y);
        }
        public static string PointFToString(object value)
        {
            PointF data = (PointF)value;
            return data.X.ToString("N", _Nmfi) + ";" + data.Y.ToString("N", _Nmfi);
        }
        public static object StringToPointF(string text)
        {
            if (String.IsNullOrEmpty(text)) return PointF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return PointF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            return new PointF(x, y);
        }
        public static string RectangleToString(object value)
        {
            Rectangle data = (Rectangle)value;
            return data.X.ToString() + ";" + data.Y.ToString() + ";" + data.Width.ToString() + ";" + data.Height.ToString();
        }
        public static object StringToRectangle(string text)
        {
            if (String.IsNullOrEmpty(text)) return Rectangle.Empty;
            string[] items = text.Split(';');
            if (items.Length != 4) return Rectangle.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            int w = StringInt32(items[2]);
            int h = StringInt32(items[3]);
            return new Rectangle(x, y, w, h);
        }
        public static string RectangleFToString(object value)
        {
            RectangleF data = (RectangleF)value;
            return data.X.ToString("N", _Nmfi) + ";" + data.Y.ToString("N", _Nmfi) + ";" + data.Width.ToString("N", _Nmfi) + ";" + data.Height.ToString("N", _Nmfi);
        }
        public static object StringToRectangleF(string text)
        {
            if (String.IsNullOrEmpty(text)) return RectangleF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 4) return RectangleF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            Single w = StringSingle(items[2]);
            Single h = StringSingle(items[3]);
            return new RectangleF(x, y, w, h);
        }
        public static string SizeToString(object value)
        {
            Size data = (Size)value;
            return data.Width.ToString() + ";" + data.Height.ToString();
        }
        public static object StringToSize(string text)
        {
            if (String.IsNullOrEmpty(text)) return Size.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return Size.Empty;
            int w = StringInt32(items[0]);
            int h = StringInt32(items[1]);
            return new Size(w, h);
        }
        public static string SizeFToString(object value)
        {
            SizeF data = (SizeF)value;
            return data.Width.ToString("N", _Nmfi) + ";" + data.Height.ToString("N", _Nmfi);
        }
        public static object StringToSizeF(string text)
        {
            if (String.IsNullOrEmpty(text)) return SizeF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return SizeF.Empty;
            Single w = StringSingle(items[0]);
            Single h = StringSingle(items[1]);
            return new SizeF(w, h);
        }
        public static string FontStyleToString(object value)
        {
            FontStyle fontStyle = (FontStyle)value;
            bool b = ((fontStyle & FontStyle.Bold) != 0);
            bool i = ((fontStyle & FontStyle.Italic) != 0);
            bool s = ((fontStyle & FontStyle.Strikeout) != 0);
            bool u = ((fontStyle & FontStyle.Underline) != 0);
            string result = (b ? "B" : "") + (i ? "I" : "") + (s ? "S" : "") + (u ? "U" : "");
            if (result.Length > 0) return result;
            return "R";
        }
        public static object StringToFontStyle(string text)
        {
            if (String.IsNullOrEmpty(text)) return FontStyle.Regular;
            FontStyle result = (text.Contains("B") ? FontStyle.Bold : FontStyle.Regular) |
                               (text.Contains("I") ? FontStyle.Italic : FontStyle.Regular) |
                               (text.Contains("S") ? FontStyle.Strikeout : FontStyle.Regular) |
                               (text.Contains("U") ? FontStyle.Underline : FontStyle.Regular);
            return result;
        }
        public static string FontToString(object value)
        {
            if (value == null) return "";
            Font font = (Font)value;
            return font.Name + ";" + SingleToString(font.SizeInPoints) + ";" + FontStyleToString(font.Style) + ";" + ByteToString(font.GdiCharSet);
        }
        public static object StringToFont(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            string[] items = text.Split(';');
            if (items.Length != 4) return null;
            float emSize = (float)StringToSingle(items[1]);
            FontStyle fontStyle = (FontStyle)StringToFontStyle(items[2]);
            byte gdiCharSet = (byte)StringToByte(items[3]);
            Font result = new Font(items[0], emSize, fontStyle, GraphicsUnit.Point, gdiCharSet);
            return result;
        }
        #endregion
        #region User types
        public static string TimeRangeToString(object value)
        {
            if (!(value is TimeRange))
                return "";
            TimeRange data = (TimeRange)value;
            return DateTimeToString(data.Begin) + "÷" + DateTimeToString(data.End);
        }
        public static object StringToTimeRange(string text)
        {
            if (String.IsNullOrEmpty(text)) return TimeRange.Empty;
            string[] items = text.Split('÷');
            if (items.Length != 2) return TimeRange.Empty;
            DateTime begin = (DateTime)StringToDateTime(items[0]);
            DateTime end = (DateTime)StringToDateTime(items[1]);
            return new TimeRange(begin, end);
        }
        #endregion
        #region Enum types
        /// <summary>
        /// Vrátí název dané hodnoty enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EnumToString<T>(T value)
        {
            return Enum.GetName(typeof(T), value);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return default(T);
        }
        /// <summary>
        /// Vrátí hodnotu enumu daného typu z daného stringu
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="defaultValue">Defaultní hodnota</param>
        /// <returns></returns>
        public static T StringToEnum<T>(string text, T defaultValue) where T : struct
        {
            T value;
            if (Enum.TryParse<T>(text, out value))
                return value;
            return defaultValue;
        }
        #endregion
        #region Helpers
        public static Int32 StringInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        public static Single StringSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Vrátí Int32 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int32, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int32 HexadecimalToInt32(string hexa)
        {
            Int64 value = HexadecimalToInt64(hexa);
            if (value > (Int64)(Int32.MaxValue) || value < (Int64)(Int32.MinValue))
                throw new OverflowException("Hexadecimal value " + hexa + " exceeding range for Int32 number.");
            return (Int32)value;
        }
        /// <summary>
        /// Vrátí Int64 ekvivalent daného hexadecimálního čísla.
        /// Hexadecimální číslo nesmí obsahovat prefix ani mezery, pouze hexadecimální znaky ("0123456789abcdefABCDEF").
        /// Délka textu je relativně libovolná (v rozsahu Int64, jinak dojde k přetečení).
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Int64 HexadecimalToInt64(string hexa)
        {
            int result = 0;
            if (hexa == null || hexa.Length == 0 || !ContainOnlyHexadecimals(hexa)) return result;
            int len = hexa.Length;
            int cfc = 1;
            for (int u = (len - 1); u >= 0; u--)
            {
                char c = hexa[u];
                switch (c)
                {
                    case '0':
                        break;
                    case '1':
                        result += cfc;
                        break;
                    case '2':
                        result += 2 * cfc;
                        break;
                    case '3':
                        result += 3 * cfc;
                        break;
                    case '4':
                        result += 4 * cfc;
                        break;
                    case '5':
                        result += 5 * cfc;
                        break;
                    case '6':
                        result += 6 * cfc;
                        break;
                    case '7':
                        result += 7 * cfc;
                        break;
                    case '8':
                        result += 8 * cfc;
                        break;
                    case '9':
                        result += 9 * cfc;
                        break;
                    case 'a':
                    case 'A':
                        result += 10 * cfc;
                        break;
                    case 'b':
                    case 'B':
                        result += 11 * cfc;
                        break;
                    case 'c':
                    case 'C':
                        result += 12 * cfc;
                        break;
                    case 'd':
                    case 'D':
                        result += 13 * cfc;
                        break;
                    case 'e':
                    case 'E':
                        result += 14 * cfc;
                        break;
                    case 'f':
                    case 'F':
                        result += 15 * cfc;
                        break;
                }
                cfc = cfc * 16;
            }
            return result;
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze hexadecimální znaky
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ContainOnlyHexadecimals(string text)
        {
            return ContainOnlyChars(text, "0123456789abcdefABCDEF");
        }
        /// <summary>
        /// Vrací true, když text obsahuje pouze povolené znaky ze seznamu (chars)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static bool ContainOnlyChars(string text, string chars)
        {
            if (text == null) return false;
            foreach (char c in text)
            {
                // Pokud písmeno c (ze vstupního textu) není obsaženo v seznamu povolených písmen, pak vrátíme false (text obsahuje jiné znaky než dané):
                if (!chars.Contains(c)) return false;
            }
            return true;
        }
        /// <summary>
        /// Z daného řetězce (text) odkrojí a vrátí část, která se nachází před delimiterem.
        /// Dany text (ref) zkrátí, bude obsahovat část za delimiterem.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string StringCutOff(ref string text, string delimiter)
        {
            if (text == null) return null;
            if (text.Length == 0) return "";
            string result;
            if (String.IsNullOrEmpty(delimiter))
                throw new ArgumentNullException("delimiter", "Parametr metody Convertor.StringCutOff(«delimiter») nemůže být prázdný.");
            int len = delimiter.Length;
            int at = text.IndexOf(delimiter);
            if (at < 0)
            {
                result = text;
                text = "";
            }
            else if (at == 0)
            {
                result = "";
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            else
            {
                result = text.Substring(0, at);
                text = (at + len >= text.Length ? "" : text.Substring(at + len));
            }
            return result;
        }
        #endregion
        #endregion
        #region Static konstruktor
        static Convertor()
        { _PrepareFormats(); }
        #endregion
        #region FormatInfo
        static void _PrepareFormats()
        {
            _Dtfi = new System.Globalization.DateTimeFormatInfo();
            _Dtfi.LongDatePattern = "yyyy-MM-dd HH:mm";                   // Pattern pro formátování písmenem D, musí být nastaveno před nastavením patternu FullDateTimePattern
            _Dtfi.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss.fff";        // Pattern pro formátování písmenem F

            _Nmfi = new System.Globalization.NumberFormatInfo();
            _Nmfi.NumberDecimalDigits = 4;
            _Nmfi.NumberDecimalSeparator = ".";
            _Nmfi.NumberGroupSeparator = "";
        }
        static System.Globalization.DateTimeFormatInfo _Dtfi;
        static System.Globalization.NumberFormatInfo _Nmfi;
        #endregion
    }


    // interfaces:
    #region interface IXmlPersistNotify : dává objektu možnost být informován o procesech XmlPersist (Save / Load) pomocí property XmlPersistState
    /// <summary>
    /// interface IXmlPersistNotify : dává objektu možnost být informován o procesech XmlPersist (Save / Load) pomocí obsluhy set accessoru property XmlPersistState.
    /// Jakýkoli objekt, který chce být informován o tom, že je ukládán do XML / obnovován z XML, musí deklarovat interface IXmlPersistNotify.
    /// Pak do jeho property IXmlPersistNotify.XmlPersistState bude vkládána hodnota: při ukládání do XML hodnota Save (a po dokončení None), při načítání z XML hodnota Load a None.
    /// </summary>
    public interface IXmlPersistNotify
    {
        /// <summary>
        /// Aktuální stav procesu XML persistence.
        /// Umožňuje persistovanému objektu reagovat na ukládání nebo na načítání dat.
        /// Do této property vkládá XmlPersistor hodnotu odpovídající aktuální situaci.
        /// Datová instance může v set accessoru zareagovat a například připravit data pro Save, 
        /// anebo dokončit proces Load (navázat si další data nebo provést přepočty a další reakce).
        /// </summary>
		XmlPersistState XmlPersistState { get; set; }
    }
    /// <summary>
    /// Informuje objekt s daty, v jakém stavu je proces XmlPersist.
    /// Při zahájení Load (kdy jsou do objektu vkládána data načtená z XML) se do objektu, který má interface IXmlPersistNotify, 
    /// do property XmlPersistState vloží hodnota Load, a při zahájení Save hodnota Save. Po dokončení metody se vrátí hodnota None.
    /// Objekt na to může reagovat potlačením logiky navázané na Setování hodnot...
    /// </summary>
    public enum XmlPersistState
    {
        /// <summary>
        /// Nyní je objekt v klidu.
        /// Může přejít do stavu LoadBegin nebo SaveBegin.
        /// </summary>
		None = 0,
        /// <summary>
        /// Nyní začalo načítání dat do objektu.
        /// Po načtení bude stav změněn na LoadDone a pak None.
        /// </summary>
		LoadBegin,
        /// <summary>
        /// Nyní skončilo načítání dat do objektu.
        /// Okamžitě poté bude následovat vepsání stavu None.
        /// </summary>
        LoadDone,
        /// <summary>
        /// Nyní začalo ukládání dat z objektu.
        /// Po uložení bude stav změněn na SaveDone a pak None.
        /// </summary>
		SaveBegin,
        /// <summary>
        /// Nyní skončilo ukládání dat z objektu.
        /// Okamžitě poté bude následovat vepsání stavu None.
        /// </summary>
        SaveDone
    }
    #endregion
    #region interface IXmlSerializer : dává objektu možnost serializovat / deserializovat se vlastními silami.
    /// <summary>
    /// IXmlSerializer : dává objektu možnost serializovat / deserializovat se vlastními silami.
    /// Interface předepisuje jednu property XmlSerialData { get; set; }, 
    /// která v GET accessoru vrátí svoji vlastní serializaci,
    /// a která v SET accessoru převezme serializovaný text a naplní se z něj.
    /// Pro konverzi jednoduchých typů lze použít statické metody třídy Djs.Tools.XmlPersistor.Convertor, které konvertují datový typ z/na string.
    /// </summary>
    public interface IXmlSerializer
    {
        string XmlSerialData { get; set; }
    }
    #endregion
    #region class TypeConvertor : definuje pravidla pro persistenci specifického datového typu, obsahuje delegáty pro serializaci / deserializaci typu.
    /// <summary>
    /// TypeConvertor : Předpis pro konverzi jednoho typu.
    /// Popisuje jeden konkrétní Type, a uvádí delegáty na metody, které provedou serializaci / deserializaci tohoto typu.
    /// </summary>
    internal class TypeConvertor
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="dataType">Datový typ, pro který platá tato deklarace</param>
        /// <param name="persistenceType">Druh uložení. Pokud se bude používat serializátor</param>
        /// <param name="serializator">Metoda, která provede serializaci (z objektu na string). Musí být zadán pro persistenceType = XmlPersistenceType.Simple!</param>
        /// <param name="deserializator">Metoda, která provede deserializaci (ze stringu na objekt). Musí být zadán pro persistenceType = XmlPersistenceType.Simple!</param>
        public TypeConvertor(Type dataType, Func<object, string> serializator, Func<string, object> deserializator)
        {
            this.DataType = dataType;
            this.PersistenceType = XmlPersistenceType.Self;
            this.Serializator = serializator;
            this.Deserializator = deserializator;
        }
        /// <summary>
        /// Konstruktor interní
        /// </summary>
        /// <param name="dataType">Datový typ, pro který platá tato deklarace</param>
        /// <param name="persistenceType">Druh uložení. Pokud se bude používat serializátor</param>
        /// <param name="serializator">Metoda, která provede serializaci (z objektu na string). Musí být zadán pro persistenceType = XmlPersistenceType.Simple!</param>
        /// <param name="deserializator">Metoda, která provede deserializaci (ze stringu na objekt). Musí být zadán pro persistenceType = XmlPersistenceType.Simple!</param>
        internal TypeConvertor(Type dataType, XmlPersistenceType persistenceType, Func<object, string> serializator, Func<string, object> deserializator)
        {
            this.DataType = dataType;
            this.PersistenceType = persistenceType;
            this.Serializator = serializator;
            this.Deserializator = deserializator;
        }
        /// <summary>
        /// Datový typ, pro který platá tato deklarace
        /// </summary>
        public Type DataType { get; private set; }
        /// <summary>
        /// Druh uložení. Pokud bude zadána hodnota XmlPersistenceType.Simple, musí se vyplnit i Serializator a Deserializator.
        /// Pokud aplikace chce některé typy serializovat vlastními silami, uvede XmlPersistenceType.Simple a naplní svoje metody Serializator a Deserializator.
        /// </summary>
        internal XmlPersistenceType PersistenceType { get; private set; }
        /// <summary>
        /// Metoda, která provede serializaci (z objektu na string)
        /// </summary>
        public Func<object, string> Serializator { get; private set; }
        /// <summary>
        /// Metoda, která provede deserializaci (ze stringu na objekt)
        /// </summary>
        public Func<string, object> Deserializator { get; private set; }
    }
    #endregion
    #region Attribute classes: PropertyNameAttribute, CollectionItemNameAttribute, PersistingEnabledAttribute
    /// <summary>
    /// Abstraktní předek atributů XmlPersistoru
    /// </summary>
    public abstract class PersistAttribute : Attribute
    { }
    /// <summary>
    /// Definuje jméno elementu, do něhož se ukládá hodnota této property.
    /// Pokud nebude specifikováno, jméno bude odvozeno ze jména property dle těchto příkladů:
    /// "PropertyName" = "property_name"; "_Ukazatel" = "_ukazatel"; "GID" = "gid", atd
    /// </summary>
    internal class PropertyNameAttribute : PersistAttribute
    {
        /// <summary>
        /// Definice jména elementu.
        /// Pokud nebude specifikováno, jméno bude odvozeno ze jména property.
        /// </summary>
        /// <param name="propertyName">
        /// Definuje jméno elementu, do něhož se ukládá hodnota této property.
        /// Pokud nebude specifikováno, jméno bude odvozeno ze jména property dle těchto příkladů:
        /// "PropertyName" = "property_name"; "_Ukazatel" = "_ukazatel"; "GID" = "gid", atd
        /// </param>
        public PropertyNameAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }
        /// <summary>
        /// Název elemetnu v persistovaném XML dokumentu, do něhož se ukládá tato property
        /// </summary>
        public string PropertyName { get; private set; }
    }
    /// <summary>
    /// Definuje jméno elementu, do něhož se ukládají jednotlivé položky kolekce tohoto seznamu.
    /// Implicitní název je Item, ale tímto atributem jej lze předefinovat.
    /// </summary>
    internal class CollectionItemNameAttribute : PersistAttribute
    {
        /// <summary>
        /// Definice jména elementu.
        /// Pokud nebude specifikováno, jméno bude odvozeno ze jména property.
        /// </summary>
        /// <param name="itemName">
        /// Definuje jméno elementu, do něhož se ukládá hodnota této property.
        /// Pokud nebude specifikováno, jméno bude odvozeno ze jména property dle těchto příkladů:
        /// "PropertyName" = "property_name"; "_Ukazatel" = "_ukazatel"; "GID" = "gid", atd
        /// </param>
        public CollectionItemNameAttribute(string itemName)
        {
            this.ItemName = itemName;
        }
        public string ItemName { get; private set; }
    }
    /// <summary>
    /// Umožní potlačit ukládání této property.
    /// Pokud není specifikováno, property se uloží, pokud k ní existuje SET metoda (i kdyby byla privátní).
    /// V některých případech je vhodné hodnotu neukládat (a nenačítat), například pokud property při setování hodnotu ukládá do dalších property.
    /// Rozhodně je nutno potlačit persistenci property, které referencují globální (systémové) objekty, ty by se jinak persistovaly také!!!
    /// </summary>
    internal class PersistingEnabledAttribute : PersistAttribute
    {
        /// <summary>
        /// Tato property se má persistovat? true = ano, false = ne.
        /// Pokud tento atribut není přítomen, chápe se hodnota jako Ano.
        /// </summary>
		/// <param name="persistAndCloneEnable">Property lze persistovat i klonovat</param>
		public PersistingEnabledAttribute(bool persistAndCloneEnable)
        {
            this.PersistEnable = persistAndCloneEnable;
            this.CloneEnable = persistAndCloneEnable;
        }
        /// <summary>
        /// Tato property se má persistovat? true = ano, false = ne.
        /// Pokud tento atribut není přítomen, chápe se hodnota jako Ano.
        /// </summary>
        /// <param name="persistEnable">Property lze persistovat</param>
		/// <param name="cloneEnable">Property lze klonovat</param>
        public PersistingEnabledAttribute(bool persistEnable, bool cloneEnable)
        {
            this.PersistEnable = persistEnable;
            this.CloneEnable = cloneEnable;
        }
        /// <summary>
        /// Tato property se má persistovat? true = ano, false = ne.
        /// Pokud tento atribut není přítomen, chápe se hodnota PersistEnable jako Ano.
        /// </summary>
		public bool PersistEnable { get; private set; }
        /// <summary>
        /// Tato property se má klonovat? true = ano, false = ne.
        /// Pokud tento atribut není přítomen, chápe se hodnota CloneEnable jako Ano.
        /// Pokud je atribut vytvořen s pouze jednou hodnotou (persistAndCloneEnable), pak platí shodná pravidla pro persistování i klonování.
        /// </summary>
        public bool CloneEnable { get; private set; }
    }
    #endregion


    // all internal:
    /// <summary>
    /// Internal výkonná třída pro XML persistenci. 
    /// Tuto třídu nevyužívat z aplikačního kódu, má se používat třída (Djs.Tools.XmlPersistor.)Persist !!!
    /// </summary>
    internal class XmlPersist : IDisposable
    {
        #region Konstrukce
        protected XmlPersist()
        {
            this._TypeLibrary = new TypeLibrary();
        }
        private TypeLibrary _TypeLibrary;
        void IDisposable.Dispose()
        {
            ((IDisposable)this._TypeLibrary).Dispose();
            this._TypeLibrary = null;
        }
        /// <summary>
        /// Označení verze dat při jejich načítání.
        /// Je tak umožněno aktuálním loaderem načíst data uložená v dřívější verzi XmlPersist.
        /// </summary>
        protected string XmlPersistedVersion { get; set; }
        #endregion
        #region Statické internal metody
        /// <summary>
        /// Zajistí persistenci (uložení = serializaci) datového objektu do stringu podle daných parametrů.
        /// Pokud je v parametru uveden soubor (XmlFile), pak XML text uloží do něj (případně vytvoří adresář).
        /// </summary>
        /// <param name="data"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal static string Serialize(object data, PersistArgs parameters)
        {
            using (XmlPersist xmlPersist = new XmlPersist())
            {
                parameters.XmlContent = xmlPersist._Serialize(data, parameters);
            }
            if (parameters.XmlFile != null)
                SaveXmlToFile(parameters.XmlContent, parameters.XmlFile);

            return parameters.XmlContent;
        }
        /// <summary>
        /// Uloží daný XML obsah do daného souboru.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        internal static void SaveXmlToFile(string content, string fileName)
        {
            string eol = Environment.NewLine;
            string text = content.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            System.IO.File.WriteAllText(fileName, text, Encoding.UTF8);
        }
        /// <summary>
        /// Vytvoří objekt ze serializovaného stavu
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal static object Deserialize(PersistArgs parameters)
        {
            using (XmlPersist xmlPersist = new XmlPersist())
            {
                return xmlPersist._Deserialize(parameters);
            }
        }
        /// <summary>
        /// Určená data rekonstruuje a naplní je do předaného objektu.
        /// Pokud objekt je null, vytvoří jej.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="data"></param>
        internal static void LoadTo(PersistArgs parameters, object data)
        {
            using (XmlPersist xmlPersist = new XmlPersist())
            {
                object result = xmlPersist._Deserialize(parameters);
                xmlPersist._CloneProperties(result, data);
            }
        }
        #endregion
        #region Privátní výkonné metody: Save
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string _Serialize(object data, PersistArgs parameters)
        {
            XmlDocument xDoc = new System.Xml.XmlDocument();
            XmlSchema ts = new XmlSchema();
            ts.TargetNamespace = "http";
            ts.Id = "id";
            xDoc.Schemas.Add(ts);

            XmlElement xRootElement = xDoc.CreateElement("persistent");
            xDoc.AppendChild(xRootElement);
            CreateAttribute("Version", "1.00", xRootElement);
            CreateAttribute("Created", Convertor.DateTimeToString(DateTime.Now), xRootElement);
            CreateAttribute("Creator", System.Windows.Forms.SystemInformation.UserName, xRootElement);

            XmlElement xDataElement = CreateElement("data", xRootElement);
            this.SaveObject(new XmlPersistSaveArgs(data, "Value", null, xDataElement, this._TypeLibrary));

            StringBuilder sb = new StringBuilder();
            XmlWriter xw = XmlWriter.Create(sb, parameters.WriterSettings);
            xDoc.WriteTo(xw);
            xw.Flush();

            return sb.ToString();
        }
        /// <summary>
        /// Uloží předaný objekt do XML.
        /// Tato metoda nezakládá nový element, pokud vstupují data typu Simple.
        /// Ostatní typy dat řeší specializované metody, ty si svoje elementy zakládají.
        /// </summary>
        /// <param name="data">Objekt nesoucí data, reálný</param>
        /// <param name="objectName">Jméno objektu = název property, anebo fixní jméno pokud jde o prvek listu/array/dictionary</param>
        /// <param name="estimatedType">Typ objektu očekávaný (tj. co podle definice má být objekt zač). Může být null.
        /// Pokud se typ reálného objektu liší, bude typ vepsán do atributu.</param>
        /// <param name="typeAttributeName">Název atributu, do kteréhu bude uveden reálný Type datového objektu, pokud se liší od očekávaného (estimatedType).
        /// Pokud nebude určeno, použije se název "Net.Type"</param>
		/// <param name="xElement">Aktuální element, do něhož se budou vkládat hodnoty z objektu</param>
        internal void SaveObject(XmlPersistSaveArgs args)
        {
            // do XML budu persistovat pouze not null hodnoty!
            if (!args.HasData) return;

            // Zjistíme, jaký objekt to sem přišel:
            args.DataTypeInfo = this._TypeLibrary.GetInfo(args.Data.GetType());

            // Podle reálného typu, podle jeho charakteru předám řízení do odpovídající metody:
            switch (args.DataTypeInfo.PersistenceType)
            {
                case XmlPersistenceType.Simple:
                    this.SimpleTypeSave(args);
                    break;
                case XmlPersistenceType.Self:
                    this.SelfTypeSave(args);
                    break;
                case XmlPersistenceType.Enum:
                    this.EnumTypeSave(args);
                    break;
                case XmlPersistenceType.Array:
                    this.ArrayTypeSave(args);
                    break;
                case XmlPersistenceType.IList:
                    this.IListTypeSave(args);
                    break;
                case XmlPersistenceType.IDictionary:
                    this.IDictionaryTypeSave(args);
                    break;
                case XmlPersistenceType.Compound:
                    this.CompoundTypeSave(args);
                    break;
            }
        }
        #endregion
        #region Privátní výkonné metody: Load
        /// <summary>
        /// Z dodaného XML obsahu sestaví a vrátí odpovídající objekt.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private object _Deserialize(PersistArgs parameters)
        {
            parameters.DeserializeStatus = XmlDeserializeStatus.Processing;

            XmDocument xmDoc = null;
            if (!String.IsNullOrEmpty(parameters.XmlContent))
                xmDoc = XmDocument.CreateFromString(parameters.XmlContent);
            else if (!String.IsNullOrEmpty(parameters.XmlFile))
                xmDoc = XmDocument.CreateFromFile(parameters.XmlFile);
            if (xmDoc == null)
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.NotInput;
                return null;
            }

            // Najdu element "persistent", z něj lze načíst číslo verze:
            XmElement xmPers = xmDoc.FindElement("persistent");          // Element "persistent" je Root
            if (xmPers == null)
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.BadFormatPersistent;
                return null;
            }
            this.XmlPersistedVersion = xmPers.FindAttributeValue("Version", "");

            // Najdu element "data", v něm bude uložen objekt:
            XmElement elData = xmDoc.FindElement("persistent", "data");
            if (elData == null)
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.BadFormatData;
                return null;
            }

            // Element Data bude obsahovat buď atribut nebo sub element Value:
            XmAttribute atValue;
            XmElement elValue;
            if (!_FindAttrElementByName(elData, "Value", true, out elValue, out atValue))
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.BadFormatValue;
                return null;
            }

            return _CreateObjectOfType(_CreateLoadArgs(parameters, null, null, elValue, atValue));
        }
        /// <summary>
        /// Metoda zjistí, zda daný element (inElement) obsahuje atribut daného jména (name): pak ten atribut vloží do out findAttribute, a do out findElement vloží vstupující element a vrátí true.
        /// Pokud nenajde, podívá se do přímo podřízených elementů k elementu inElement, zda v něm najde element daného jména. Pokud najde, pak do out findElement vloží ten nalezený element,
        /// a do findAttribute zkusí najít atribut daného jména v tomto nalezeném elementu.
        /// Pokud nenajde, nechá všude null a vrátí false.
        /// Toto hledání odpovídá stylu ukládání Simple / Compound prvků.
        /// </summary>
        /// <param name="inElement"></param>
        /// <param name="name"></param>
        /// <param name="findElement"></param>
        /// <param name="findAttribute"></param>
        /// <returns></returns>
        private bool _FindAttrElementByName(XmElement inElement, string name, bool requiredAttInElement, out XmElement findElement, out XmAttribute findAttribute)
        {
            findElement = null;
            findAttribute = null;

            // Zkusím najít atribut s daným jménem:
            XmAttribute att = inElement.TryFindAttribute(name);
            if (att != null)
            {   // Nalezen atribut (typický vzhled:  <element Value.Type="XmlPersistor.GID" Value="1234;5678" />)
                findElement = inElement;
                findAttribute = att;
                return true;
            }

            // Zkusím hledat element s daným jménem:
            XmElement ele = inElement.TryFindElement(name);
            if (ele != null)
            {   // Uvnitř daného elementu <element> je element <name...> (například: <Value Value.Type="XmlPersistor.DataObject" BindFlags="Public, SetField" ... a další atributy = jednoduché property):
                att = ele.TryFindAttribute(name);
                if (att != null || !requiredAttInElement)
                {   // Nalezen atribut (typický vzhled:  <element Value.Type="XmlPersistor.GID" Value="1234;5678" />)
                    findElement = ele;
                    findAttribute = att;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Vytvoří argument pro načítání dat.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="estimatedType"></param>
        /// <param name="xmElement"></param>
        /// <param name="xmAttribute"></param>
        /// <returns></returns>
        private XmlPersistLoadArgs _CreateLoadArgs(PersistArgs parameters, PropInfo propInfo, Type estimatedType, XmElement xmElement, XmAttribute xmAttribute)
        {
            Type dataType = estimatedType;
            if (xmAttribute != null && !String.IsNullOrEmpty(xmAttribute.Type))
            {
                Type explicitDataType = TypeLibrary.GetTypeFromSerial(xmAttribute.Type, xmAttribute.Assembly);
                if (explicitDataType != null)
                    dataType = explicitDataType;
            }

            TypeInfo dataTypeInfo = null;
            if (dataType != null)
                dataTypeInfo = this._TypeLibrary.GetInfo(dataType);

            return new XmlPersistLoadArgs(parameters, propInfo, dataType, dataTypeInfo, xmElement, xmAttribute);
        }
        /// <summary>
		/// Vytvoří objekt na základě dat v aktuálním elementu readeru.
		/// Na vstupu je předán reader, defaultní typ prvku, jméno nepovinného atributu, který nese explicitní typ, a jméno atributu který nese hodnotu (platí pouze pro Simple typy).
		/// </summary>
		/// <param name="xmlReader"></param>
		/// <param name="estimatedType"></param>
		/// <param name="typeAttributeName"></param>
		/// <param name="valueAttributeName"></param>
		/// <returns></returns>
		internal object _CreateObjectOfType(XmlPersistLoadArgs args)
        {
            if (args.DataType == null) return null;
            try
            {
                switch (args.DataTypeInfo.PersistenceType)
                {
                    case XmlPersistenceType.Simple:
                        return this.SimpleTypeCreate(args);
                    case XmlPersistenceType.Self:
                        return this.SelfTypeCreate(args);
                    case XmlPersistenceType.Enum:
                        return this.EnumTypeCreate(args);
                    case XmlPersistenceType.Array:
                        break;
                    case XmlPersistenceType.IList:
                        return this.IListTypeCreate(args);
                    case XmlPersistenceType.IDictionary:
                        return null; // this._DictionaryTypeCreate(args);
                    case XmlPersistenceType.Compound:
                        return this.CompoundTypeCreate(args);
                }
            }
            catch (Exception exc)
            {
                string diag = exc.StackTrace;
            }
            return null;
        }
        #endregion
        #region Privátní výkonné metody: CloneProperties
        /// <summary>
        /// Přenese hodnoty (objekty z properties) ze source do target, provede Shallow Copy (MemberwiseClone)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private void _CloneProperties(object source, object target)
        {
            // Pokud nejsou vytvořena žádná data, nic neděláme.
            if (source == null || target == null)
                return;

            System.Reflection.PropertyInfo[] propSrcs = source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            System.Reflection.PropertyInfo[] propTrgs = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (System.Reflection.PropertyInfo propTrg in propTrgs)
            {   // Pro každou target property:
                // Pokud je property zapisovatelná a klonovatelná:
                if (propTrg.CanWrite && PropInfo.IsPropertyCloneable(propTrg))
                {
                    System.Reflection.PropertyInfo propSrc = propSrcs.FirstOrDefault(p => p.Name == propTrg.Name);         // Najdu property ve zdrojovém objektu?
                    if (propSrc != null)
                    {
                        try
                        {
                            object value = propSrc.GetValue(source, null);
                            propTrg.SetValue(target, value, null);
                        }
                        catch
                        { }
                    }
                }
            }
        }
        #endregion
        #region Simple typy: Save, Create
        /// <summary>
		/// Daný objekt (data) konvertuje pomocí TypeConvertoru pro Simple typ, a výslednou hodnotu vloží do atributu daného jména.
		/// </summary>
		/// <param name="attributeName">Jméno atributu pro uložení hodnoty</param>
		/// <param name="typeInfo">Infromace o typu</param>
		/// <param name="data">Objekt k zápisu. Musí jít o Simple objekt, zápis bude řešit metoda this._TypeLibrary.SimpleTypeSave()</param>
		/// <param name="xDocument"></param>
		/// <param name="xElement"></param>
		private void SimpleTypeSave(XmlPersistSaveArgs args)
        {
            NotifyData(args.Data, XmlPersistState.SaveBegin);

            if (args.DataTypeInfo.TypeConvert == null)
                throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() neobsahuje TypeConvertor.");
            if (args.DataTypeInfo.TypeConvert.Serializator == null)
                throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() obsahuje TypeConvertor, který nemá serializátor.");
            string value = args.DataTypeInfo.TypeConvert.Serializator(args.Data);

            SaveTypeAttribute(args);

            CreateAttribute(args.ObjectName, value, args.XmlElement);

            NotifyData(args.Data, XmlPersistState.SaveDone);
        }
        /// <summary>
        /// Z dodaného readeru načte a sestaví objekt uložený jako Simple (tj. je uložený v jednom atributu jako jeden string)
        /// </summary>
        /// <returns></returns>
        private object SimpleTypeCreate(XmlPersistLoadArgs args)
        {
            if (args.DataTypeInfo.TypeConvert == null)
                throw new InvalidOperationException("Nelze deserializovat, typ předaný do metody SimpleTypeCreate() neobsahuje TypeConvertor.");
            if (args.DataTypeInfo.TypeConvert.Deserializator == null)
                throw new InvalidOperationException("Nelze deserializovat, typ předaný do metody SimpleTypeCreate() obsahuje TypeConvertor, který nemá deserializátor.");

            return args.DataTypeInfo.TypeConvert.Deserializator(args.XmAttribute.ValueFirstOrDefault);
        }
        #endregion
        #region Self typy: Save, Create
        /// <summary>
        /// Uloží do Xml dokumentu daný objekt, který je typu Self (tj. serializaci provádí on sám osobně)
        /// </summary>
        private void SelfTypeSave(XmlPersistSaveArgs args)
        {
            if (args.HasData)
            {
                NotifyData(args.Data, XmlPersistState.SaveBegin);

                if (!(args.Data is IXmlSerializer))
                    throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SelfTypeSave() neimplementuje interface IXmlSerializer.");
                string value = ((IXmlSerializer)args.Data).XmlSerialData;

                SaveTypeAttribute(args);

                CreateAttribute(args.ObjectName, value, args.XmlElement);

                NotifyData(args.Data, XmlPersistState.SaveDone);
            }
        }
        /// <summary>
        /// Z dodaného readeru načte a sestaví objekt uložený jako Self (tj. deserializaci provádí on sám osobně)
        /// </summary>
        /// <returns></returns>
        private object SelfTypeCreate(XmlPersistLoadArgs args)
        {
            string xmlData = args.XmAttribute.ValueFirstOrDefault;
            if (xmlData == null)
                return null;

            object data = _ObjectCreate(args.DataType);
            if (data == null)
                return null;

            NotifyData(data, XmlPersistState.LoadBegin);
            ((IXmlSerializer)data).XmlSerialData = xmlData;
            NotifyData(data, XmlPersistState.LoadDone);

            return data;
        }
        #endregion
        #region Enum typy: Save, Create
        /// <summary>
        /// Uloží do Xml dokumentu daný objekt, který je typu Self (tj. serializaci provádí on sám osobně)
        /// </summary>
        /// <param name="attributeName">Jméno atributu pro uložení hodnoty</param>
        /// <param name="typeInfo">Infromace o typu</param>
        /// <param name="data">Objekt k zápisu. Musí jít o Enum objekt</param>
        /// <param name="xDocument"></param>
        /// <param name="xElement"></param>
        private void EnumTypeSave(XmlPersistSaveArgs args)
        {
            string value = Enum.Format(args.DataType, args.Data, "F");

            SaveTypeAttribute(args);

            CreateAttribute(args.ObjectName, value, args.XmlElement);
        }
        /// <summary>
        /// Z dodaného readeru načte a sestaví objekt uložený jako Enum
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="type"></param>
        /// <param name="valueAttributeName"></param>
        /// <returns></returns>
        private object EnumTypeCreate(XmlPersistLoadArgs args)
        {
            string xmlData = args.XmAttribute.ValueFirstOrDefault;
            if (xmlData == null)
                return null;

            object data = null;
            try
            {
                data = Enum.Parse(args.DataType, xmlData);
            }
            catch
            {
                data = _ObjectCreate(args.DataType);                  // Vytvoří objekt, prázdný (hodnota = 0)
            }
            return data;
        }
        #endregion
        #region Array typy: Save, Create
        /// <summary>
        /// Uloží do Xml dokumentu daný objekt, který je typu Array
        /// </summary>
        /// <param name="attributeName">Jméno atributu pro uložení hodnoty</param>
        /// <param name="typeInfo">Infromace o typu</param>
        /// <param name="data">Objekt k zápisu. Musí jít o Enum objekt</param>
        /// <param name="xDocument"></param>
        /// <param name="xElement"></param>
        private void ArrayTypeSave(XmlPersistSaveArgs args)
        {
            // Zavedu element za celý List,například <Array ...>...</Array>:
            XmlElement xmlArrayElement = CreateElement(args.ObjectName, args.XmlElement);

            // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
            SaveTypeAttribute(args, xmlArrayElement);
            Array a = args.Data as Array;
            Type itemType = args.DataTypeInfo.ItemDataType;

            // Rozměry pole:  [0÷2,0÷45] :
            string range = "";
            for (int r = 0; r < a.Rank; r++)
                range += (range.Length == 0 ? "" : ",") + a.GetLowerBound(r).ToString() + "+" + a.GetLength(r).ToString();
            // range += (range.Length == 0 ? "" : ",") + a.GetLowerBound(r).ToString() + "÷" + a.GetUpperBound(r).ToString();
            range = "[" + range + "]";
            CreateAttribute("Array.Range", range, xmlArrayElement);

            // Low a High Bounds:
            int[] indicesLow = new int[a.Rank];
            int[] indicesHigh = new int[a.Rank];
            for (int r = 0; r < a.Rank; r++)
            {
                indicesLow[r] = a.GetLowerBound(r);
                indicesHigh[r] = a.GetUpperBound(r);
            }

            // Výpis všech Items:
            int[] indices = new int[a.Rank];               // Aktuální mapa dimenzí (průběžně se bude měnit)
            indicesLow.CopyTo(indices, 0);
            string itemName = args.GetItemName("Item");
            while (true)
            {
                // Načtu hodnotu prvku adresovaného pointerem indices, a vepíšu jej do výstupu:
                bool valid = true;
                for (int r = 0; r < a.Rank; r++)
                {
                    if (indices[r] < 0 || indices[r] > indicesHigh[r])
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    object item = a.GetValue(indices);
                    XmlElement xmlItemElement = CreateElement(itemName, xmlArrayElement);
                    // Vypíšu indices:
                    string ptr = "";
                    for (int r = 0; r < a.Rank; r++)
                        ptr += (ptr.Length == 0 ? "" : ",") + indices[r].ToString();
                    CreateAttribute(itemName + ".Indices", "[" + ptr + "]", xmlItemElement);

                    // Vypíšu obsah itemu:
                    if (item != null)
                        this.SaveObject(new XmlPersistSaveArgs(item, "Value", itemType, xmlItemElement, this._TypeLibrary));
                }

                // Pokročím na další indices nebo skončím:
                bool end = true;
                for (int r = a.Rank - 1; r >= 0; r--)     // Dimenze: 2,1,0 (pořadí indexu v adresaci prvku pole N-té dimenze)
                {
                    indices[r]++;                         // Postupně zvyšuji index v dané dimenzi (zprava): [0,0,0], [0,0,1], [0,0,2], [0,1,0], [0,1,1], ...
                    if (indices[r] <= indicesHigh[r])     // Pokud tato dimenze není dokončena, vyběhnu s příznakem !end:
                    {
                        end = false;
                        break;
                    }
                    indices[r] = indicesLow[r];           // Tuto dimenzi nastavím na Low, a buď jdu dál (zvýším další dimenzi) nebo skončím (vše je hotovo).
                }
                if (end) break;
            }
        }
        /// <summary>
        /// Z dodaného readeru načte a sestaví objekt uložený jako Array
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="type"></param>
        /// <param name="valueAttributeName"></param>
        /// <returns></returns>
        private object ArrayTypeCreate(XmlPersistLoadArgs args)
        {
            return null;
        }
        #endregion
        #region IList typy: Save, Create
        /// <summary>
        /// Uloží kolekci (List)
        /// </summary>
        /// <param name="iCollection"></param>
        /// <param name="elementName"></param>
        /// <param name="xDocument"></param>
        /// <param name="xParentElement"></param>
        internal void IListTypeSave(XmlPersistSaveArgs args)
        {
            // Zavedu element za celý List,například <ItemList ...>...</ItemList>:
            XmlElement xmlListElement = CreateElement(args.ObjectName, args.XmlElement);

            // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
            SaveTypeAttribute(args, xmlListElement);

            IList iList = args.Data as IList;
            if (iList == null) return;

            Type itemType = args.DataTypeInfo.GetGenericType(0);

            string itemName = args.GetItemName("Item");
            foreach (object item in iList)
            {
                // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek bez vynechávání null položek:
                XmlElement xmlItemElement = CreateElement(itemName, xmlListElement);
                if (args.HasData)
                    this.SaveObject(new XmlPersistSaveArgs(item, "Value", itemType, xmlItemElement, this._TypeLibrary));
            }
        }
        internal object IListTypeCreate(XmlPersistLoadArgs args)
        {
            // Vytvořím objekt s daty odpovídajícími datům persistovaným:
            object data = _ObjectCreate(args.DataType);
            IList iList = data as IList;
            if (iList == null) return null;

            NotifyData(data, XmlPersistState.LoadBegin);

            args.DataTypeInfo = this._TypeLibrary.GetInfo(data.GetType());           // Reálný Type + jeho property
            Type itemType = args.DataTypeInfo.ItemDataType;
            if (itemType == null)
                itemType = args.DataTypeInfo.GetGenericType(0);

            string itemName = (args.PropInfo != null && !String.IsNullOrEmpty(args.PropInfo.XmlItemName) ? args.PropInfo.XmlItemName : "Item");

            foreach (XmElement xmEle in args.XmElement.XmElements)
            {
                if (xmEle.Name == itemName)
                {   // Element má odpovídající jméno. 
                    object value = null;
                    // Buď obsahuje atribut, nebo podřízený element s názvem Value:
                    XmAttribute atValue;
                    XmElement elValue;
                    if (_FindAttrElementByName(xmEle, "Value", false, out elValue, out atValue))
                    {
                        XmlPersistLoadArgs itemArgs = this._CreateLoadArgs(args.Parameters, null, itemType, elValue, atValue);
                        value = this._CreateObjectOfType(itemArgs);
                    }
                    iList.Add(value);
                }
            }

            NotifyData(data, XmlPersistState.LoadDone);

            return data;
        }
        #endregion
        #region IDictionary typy: Save, Create
        /// <summary>
        /// Uloží Dictionary
        /// </summary>
        internal void IDictionaryTypeSave(XmlPersistSaveArgs args)
        {
            // string currentName, string itemName, TypeInfo typeInfo, object data, Type estimatedType, string typeAttributeName, XmlElement xElement)
            XmlElement xmlDictElement = CreateElement(args.ObjectName, args.XmlElement);  // Zavedu element za celý List,například <ItemList ...>...</ItemList>

            // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
            SaveTypeAttribute(args, xmlDictElement);


            IDictionary iDict = args.Data as IDictionary;
            if (iDict == null) return;

            Type keyType = args.DataTypeInfo.GetGenericType(0);
            Type valueType = args.DataTypeInfo.GetGenericType(1);

            string itemName = args.GetItemName("Entry");
            foreach (DictionaryEntry entry in iDict)
            {
                // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek bez vynechávání null položek:
                XmlElement xmlItemElement = CreateElement(itemName, xmlDictElement);

                this.SaveObject(new XmlPersistSaveArgs(entry.Key, "Key", keyType, xmlItemElement, this._TypeLibrary));
                this.SaveObject(new XmlPersistSaveArgs(entry.Value, "Value", valueType, xmlItemElement, this._TypeLibrary));
            }
        }
        #endregion
        #region Compound typy: Save, Create
        /// <summary>
        /// Uloží do Xml dokumentu daný objekt, který je typu Compound (tj. má property)
        /// </summary>
        /// <param name="currentName">Jméno atributu pro uložení hodnoty</param>
        /// <param name="typeInfo">Infromace o typu</param>
        /// <param name="data">Objekt k zápisu. Musí jít o Enum objekt</param>
        /// <param name="xDocument"></param>
        /// <param name="xElement"></param>
        internal void CompoundTypeSave(XmlPersistSaveArgs args)
        {
            NotifyData(args.Data, XmlPersistState.SaveBegin);

            XmlElement xmlCurrElement = CreateElement(args.ObjectName, args.XmlElement);
            SaveTypeAttribute(args, xmlCurrElement);

            foreach (PropInfo propInfo in args.DataTypeInfo.PropertyList)
            {
                object value = propInfo.Property.GetValue(args.Data, null);
                this.SaveObject(new XmlPersistSaveArgs(value, propInfo.XmlName, propInfo.PropertyType, propInfo.XmlItemName, xmlCurrElement, this._TypeLibrary));
            }

            NotifyData(args.Data, XmlPersistState.SaveDone);
        }
        /// <summary>
        /// Vytvoří a vrátí objekt typu Compound z dat XML
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
		internal object CompoundTypeCreate(XmlPersistLoadArgs args)
        {
            // Vytvořím objekt s daty odpovídajícími datům persistovaným:
            object data = _ObjectCreate(args.DataType);
            NotifyData(data, XmlPersistState.LoadBegin);
            args.DataTypeInfo = this._TypeLibrary.GetInfo(data.GetType());             // Reálný Type + jeho property

            // 1. Projdeme atributy, ty obsahují jednoduché datové typy. Uložíme je do property našeho objektu:
            foreach (XmAttribute xmAtt in args.XmElement.XmAttributes)
            {
                PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(xmAtt.Name);
                if (propInfo != null)
                {
                    XmlPersistLoadArgs propArgs = this._CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, args.XmElement, xmAtt);
                    object value = this._CreateObjectOfType(propArgs);
                    propInfo.Property.SetValue(data, value, null);
                }
            }

            // 2. Projdeme si sub-elementy našeho elementu. pro každý z nich určím zda máme cílovou property, a pak načtu hodnotu:
            foreach (XmElement xmEle in args.XmElement.XmElements)
            {
                PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(xmEle.Name);
                if (propInfo != null)
                {
                    XmAttribute xmAte = xmEle.TryFindAttribute(xmEle.Name);            // Pokud je v elementu uložen obraz objektu, který je jiného typu než je očekáván v property, pak je zde uložen i konkrétní Type
                    XmlPersistLoadArgs propArgs = this._CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, xmEle, xmAte);
                    object value = this._CreateObjectOfType(propArgs);
                    propInfo.Property.SetValue(data, value, null);
                }
            }

            NotifyData(data, XmlPersistState.LoadDone);
            return data;
        }
        #endregion
        #region Xml prvky - tvorba, zápisy, nalezení
        /// <summary>
        /// Založí nový element do elementu předaného. Nic do něj nevpisuje. Vrátí tento nově založený element.
        /// </summary>
        /// <param name="elementName">Název elementu (typicky jde o název property, nebo klíčové slovo pro element za položku listu, atd)</param>
        /// <param name="xParentElement"></param>
        /// <returns></returns>
        internal static XmlElement CreateElement(string elementName, XmlElement xParentElement)
        {
            string name = CreateXmlName(elementName);
            XmlElement xElement = xParentElement.OwnerDocument.CreateElement(name);
            xParentElement.AppendChild(xElement);
            return xElement;
        }
        /// <summary>
        /// Z daného textu vyloučí nevhodné znaky a výsledek vrátí. Nemění ToLower.
        /// Odstraňuje ampersand, čárku, mezeru, hranaté závorky.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static string CreateXmlName(string name)
        {
            string excludeChars = "`, []";
            string xmlName = name;
            foreach (char c in excludeChars)
            {
                if (xmlName.Contains(c))
                    xmlName = xmlName.Replace(c.ToString(), "");
            }
            return xmlName;
        }
        /// <summary>
        /// Založí nový atribut a vloží jej do předaného elementu.
        /// </summary>
        /// <param name="name">Název atributu</param>
        /// <param name="value">Hodnota do atributu, string. Pokud bude null, bude atribut bez hodnoty.</param>
        /// <param name="xElement"></param>
        internal static XmlAttribute CreateAttribute(string name, string value, XmlElement xElement)
        {
            XmlAttribute xAttribute = xElement.OwnerDocument.CreateAttribute(name);
            if (value != null)
                xAttribute.Value = value;
            xElement.Attributes.Append(xAttribute);
            return xAttribute;
        }
        /// <summary>
        /// V případě potřeby uloží do aktuálního elementu atribut, který ponese aktuální Type.
        /// Případ potřeby je tehdy, když se aktuální Type (currentType) liší od očekávaného (estimatedType).
        /// </summary>
        internal static void SaveTypeAttribute(XmlPersistSaveArgs args)
        {
            SaveTypeAttribute(args, args.XmlElement);
        }
        /// <summary>
        /// V případě potřeby uloží do aktuálního elementu atribut, který ponese aktuální Type.
        /// Případ potřeby je tehdy, když se aktuální Type (currentType) liší od očekávaného (estimatedType).
        /// </summary>
        internal static void SaveTypeAttribute(XmlPersistSaveArgs args, XmlElement xmlElement)
        {
            // Neliší se typ reálný a očekávaný? OK, žádný atribut s typem se neuloží.
            if (Type.Equals(args.DataType, args.EstimatedType)) return;

            // Získám serializovatelné hodnoty o typu:
            string serialType, serialAssembly;
            TypeLibrary.GetSerialForType(args.DataType, out serialType, out serialAssembly);

            // Vytvořím atributy pro neprázdné serial hodnoty:
            if (!String.IsNullOrEmpty(serialType))
                CreateAttribute(args.ObjectName + ".Type", serialType, xmlElement);
            if (!String.IsNullOrEmpty(serialAssembly))
                CreateAttribute(args.ObjectName + ".Assembly", serialAssembly, xmlElement);
        }
        /// <summary>
        /// Najde v daném readeru začátek daného elementu, 
        /// </summary>
        /// <param name="elementName"></param>
        internal static bool XmlReadFindElement(XmlTextReader xmlReader, string elementName, XmlElementDepthType depthType)
        {
            int depth = xmlReader.Depth;
            while (!xmlReader.EOF)
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == elementName)
                {
                    switch (depthType)
                    {
                        case XmlElementDepthType.None:
                            break;
                        case XmlElementDepthType.Anywhere:
                            return true;
                        case XmlElementDepthType.InCurrentDepth:
                            if (xmlReader.Depth == depth)
                                return true;
                            break;
                        case XmlElementDepthType.CurrentAndAnyChilds:
                            if (xmlReader.Depth >= depth)
                                return true;
                            break;
                        case XmlElementDepthType.OnlyInChilds:
                            if (xmlReader.Depth > depth)
                                return true;
                            break;
                    }
                    return true;
                }
                xmlReader.Read();
            }
            return false;
        }
        #endregion
        #region Privátní výkonné metody: obecné
        /// <summary>
        /// Vytvoří a vrátí objekt daného typu
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private object _ObjectCreate(Type type)
        {
            object result = null;
            ConstructorInfo constructor = this.CheckConstructors(type);     // Ověří, zda Type má bezparametrický konstruktor. Vrátí jej.
            if (constructor != null)
                result = constructor.Invoke(null);
            else
                // Například struktury nemají bezparametrický konstruktor definovaný, proto vrací null. Přesto je lze standardně vytvořit:
                result = System.Activator.CreateInstance(type);
            return result;
        }
        /// <summary>
        /// Metoda ověří, zda typ má bezparametrický konstruktor.
        /// Pokud jej nemá, vyhodí chybu.
        /// Pokud jej má, vrátí jej.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal ConstructorInfo CheckConstructors(Type type)
        {
            if (type.IsClass)
            {
                ConstructorInfo[] typeConsts = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);   // Najdu konstruktory daného Type
                ConstructorInfo[] typeConstNps = typeConsts.Where(c => c.GetParameters().Length == 0).ToArray();     // Najdu jen ty bezparametrické...
                if (typeConstNps.Length == 0)
                    throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " can not be persisted, must be a type with parameterless constructor!");
                return typeConstNps[0];
            }
            if (type.IsInterface)
            {
                throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " is interface. Object can not be created.!");
            }
            if (type.IsValueType || type.IsEnum || type.IsPrimitive)
            {
                return null;
            }
            throw new InvalidOperationException("Type " + type.Namespace + "." + type.Name + " is unknown type. Object can not be created.!");
        }
        /// <summary>
        /// Oznámí datovému objektu změnu stavu na danou hodnotu.
        /// Datový objekt se tak může připravit na nadcházející události (vkládání dat do property v režimu Load() nemusí vyvolávat množství logiky, a nemělo by vyvolávat mnoho chyb).
        /// Ukončení tohoto stavu (Load / Save) bude oznámeno vložením hodnoty LoadDone nebo SaveDone, a pak ihned None.
        /// Pokud daný objekt nepodporuje IXmlPersistNotify, pak tato metoda nic nevkládá.
        /// </summary>
        /// <param name="xmlPersistState"></param>
        internal static void NotifyData(object data, XmlPersistState xmlPersistState)
        {
            if (data != null && data is IXmlPersistNotify)
            {
                IXmlPersistNotify xmlPers = data as IXmlPersistNotify;
                xmlPers.XmlPersistState = xmlPersistState;

                // Ze stavu LoadDone a SaveDone automaticky přejdu do stavu None:
                if (xmlPersistState == XmlPersistState.LoadDone || xmlPersistState == XmlPersistState.SaveDone)
                    xmlPers.XmlPersistState = XmlPersistState.None;
            }
        }
        #endregion
        #region Ukázky XML
        /*    XML sample

	Příklad persistence objektu s následující strukturou:
class Sample
{
	int Id { get; private set; }                              // Jednoduchá property
	string Name { get; set; }                                 // Jednoduchá property
	List<string> Lines { get; set; }                          // Seznam s položkami, jejichž typ jednoduchý
	List<SampleItem> Items { get; private set; }              // Seznam s položkami, jejichž typ je složený
	Dictionary<int, SampleItem> Cached { get; private set; }  // Dictionary s položkami
	bool HasItems { get { return (this.Items.Count > 0); }    // Nemá set = nebude se ukládat
}
class SampleItem
{
	int Id { get; private set; }                              // Jednoduchá property
	string Name { get; set; }                                 // Jednoduchá property
}

XML reprezentace:
<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data>
        <Value Value.Type="XmlPersistor.DataObject" BindFlags="Default" Count="0" ObjectId="0" InternalDecimal="0.00" NextGId="0;0" Operation="0" PrevGId="0;0" Product_Order_Structure="" Product_Order26="" VstupníStrana="Vychod">
            <Days>
                <Day Value="1" />
                <Day Value="2" />
                <Day Value="3" />
            </Days>
            <ItemDict />
            <ItemList />
            <MatrixFloat Array.Range="[0+1,0+1]">
                <Item Item.Indices="[0,0]" Value="0.00" />
            </MatrixFloat>
            <MatrixObj Array.Range="[0+0,0+0]" />
            <QItem0 Id="0" />
        </Value>
    </data>
</persistent>


anebo neprázdný objekt:
<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data>
        <Value Value.Type="XmlPersistor.DataObject" BindFlags="Public, SetField" ComparableKey.Type="System.Int32" ComparableKey="654" Count="0" Description="Zde je &lt;menší&gt; nebo větší == objekt, % s blbými znaky &lt;!-- ! --&gt;" ObjectId="0" InternalDecimal="0.00" NextGId="25;2100" Operation="0" PrevGId="25;1655" Product_Order_Structure="25614" Product_Order26="126" VstupníStrana="16">
            <Auditor Auditor.Type="System.Collections.Generic.List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]">
                <Item Value="Informace" />
                <Item Value="Varování" />
                <Item Value="Chyba" />
            </Auditor>
            <Days>
                <Day Value="1" />
                <Day Value="2" />
                <Day Value="3" />
                <Day Value="123" />
                <Day Value="234" />
                <Day Value="345" />
                <Day Value="321654" />
                <Day Value="2541" />
                <Day Value="3321" />
                <Day Value="52" />
                <Day Value="12385" />
                <Day Value="3366" />
                <Day Value="3368" />
                <Day Value="3658" />
            </Days>
            <Item1 Id="0" Key="It1" Text="Položka 1A" />
            <Item3 Id="0" Key="It3" Text="Položka 3C" />
            <ItemDict>
                <Item Key="1">
                    <Value Id="0" Text="ukázka1" />
                </Item>
            </ItemDict>
            <ItemList>
                <Item>
                    <Value Id="0" Text="text1" />
                </Item>
            </ItemList>
            <MatrixFloat Array.Range="[0+2,0+3]">
                <Item Item.Indices="[0,0]" Value="1.00" />
                <Item Item.Indices="[0,1]" Value="2.00" />
                <Item Item.Indices="[0,2]" Value="3.00" />
                <Item Item.Indices="[1,0]" Value="10.00" />
                <Item Item.Indices="[1,1]" Value="11.00" />
                <Item Item.Indices="[1,2]" Value="12.00" />
            </MatrixFloat>
            <MatrixObj Array.Range="[0+2,0+4]">
                <Item Item.Indices="[0,0]" Value.Type="System.Single" Value="0.01" />
                <Item Item.Indices="[0,1]" Value.Type="System.Single" Value="0.02" />
                <Item Item.Indices="[0,2]" Value.Type="System.Single" Value="0.03" />
                <Item Item.Indices="[0,3]" Value.Type="System.Single" Value="0.04" />
                <Item Item.Indices="[1,0]" Value.Type="System.Single" Value="1.01" />
                <Item Item.Indices="[1,1]" Value.Type="System.Single" Value="1.02" />
                <Item Item.Indices="[1,2]" Value.Type="System.Single" Value="1.03" />
                <Item Item.Indices="[1,3]" Value.Type="System.Single" Value="1.04" />
            </MatrixObj>
            <Object3 Object3.Type="XmlPersistor.Item" Id="0" Key="ItObj3" Text="Položka jakoby objekt" />
            <QItem0 Id="0" Key="Klíč" Text="Název" />
        </Value>
    </data>
</persistent>
    
    
   GID se ukládá pomocí jeho vlastní property string IXmlSerializer.XmlSerialData:

<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data Value.Type="XmlPersistor.GID" Value="1234;5678" />
</persistent>

   System.Decimal:

<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data Value.Type="System.Decimal" Value="12.45" />
</persistent>


   Array:

<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data>
        <Value Value.Type="System.Object[]" Array.Range="[0+7]">
            <Item Item.Indices="[0]" Value.Type="System.Int32" Value="12" />
            <Item Item.Indices="[1]" Value.Type="System.String" Value="Pokusník" />
            <Item Item.Indices="[2]" Value.Type="XmlPersistor.GID" Value="2;500" />
            <Item Item.Indices="[3]" Value.Type="System.DateTime" Value="2011-11-08 10:04 10:04:01" />
            <Item Item.Indices="[4]" Value.Type="System.Drawing.Rectangle" Value="10;50;60;30" />
            <Item Item.Indices="[5]" Value.Type="System.Data.SqlTypes.SqlInt32" Value="60" />
            <Item Item.Indices="[6]" Value.Type="System.Data.SqlTypes.SqlInt32" Value="" />
        </Value>
    </data>
</persistent>

   Hashtable:

<?xml version="1.0" encoding="utf-16"?>
<persistent Version="1.00">
    <data>
        <Value Value.Type="System.Collections.Hashtable">
            <Item Key.Type="XmlPersistor.GID" Key="16;16" Value.Type="System.Drawing.Point" Value="400;300" />
            <Item Key.Type="System.DateTime" Key="2011-11-08 10:04 10:04:23">
                <Value Value.Type="XmlPersistor.DataObject" BindFlags="Default" Count="0" ObjectId="0" InternalDecimal="0.00" NextGId="0;0" Operation="0" PrevGId="0;0" Product_Order_Structure="" Product_Order26="" VstupníStrana="Vychod">
                    <Days>
                        <Day Value="1" />
                        <Day Value="2" />
                        <Day Value="3" />
                    </Days>
                    <ItemDict />
                    <ItemList />
                    <MatrixFloat Array.Range="[0+1,0+1]">
                        <Item Item.Indices="[0,0]" Value="0.00" />
                    </MatrixFloat>
                    <MatrixObj Array.Range="[0+1,0+1]">
                        <Item Item.Indices="[0,0]" />
                    </MatrixObj>
                </Value>
            </Item>
            <Item Key.Type="System.Decimal" Key="26.20" Value.Type="System.Char" Value="a" />
            <Item Key.Type="System.Int32" Key="16" Value.Type="System.String" Value="Pokus" />
        </Value>
    </data>
</persistent>



*/
        #endregion
    }
    #region class XmlPersistSaveArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při serializaci objektu
    /// <summary>
    /// XmlPersistSaveArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při serializaci objektu
    /// </summary>
    internal class XmlPersistSaveArgs
    {
        internal XmlPersistSaveArgs(object data, string objectName, Type estimatedType, XmlElement xmlElement, TypeLibrary typeLibrary)
        {
            this.Data = data;
            this.ObjectName = objectName;
            this.EstimatedType = estimatedType;
            this.ItemName = null;
            this.XmlElement = xmlElement;
            this.DataTypeInfo = (data == null ? null : typeLibrary.GetInfo(data.GetType()));
        }
        internal XmlPersistSaveArgs(object data, string objectName, Type estimatedType, string itemName, XmlElement xmlElement, TypeLibrary typeLibrary)
        {
            this.Data = data;
            this.ObjectName = objectName;
            this.EstimatedType = estimatedType;
            this.ItemName = itemName;
            this.XmlElement = xmlElement;
            this.DataTypeInfo = (data == null ? null : typeLibrary.GetInfo(data.GetType()));
        }
        internal object Data;
        internal bool HasData { get { return (this.Data != null); } }
        internal string ObjectName;
        internal Type EstimatedType;
        internal string ItemName;
        internal string TypeAttributeName;
        internal XmlElement XmlElement;
        internal Type DataType { get { return (this.DataTypeInfo == null ? null : this.DataTypeInfo.DataType); } }
        internal TypeInfo DataTypeInfo;
        /// <summary>
        /// Vrátí jméno elementu, který obsahuje jednu položku (listu, dictionary, atd)
        /// </summary>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        internal string GetItemName(string defaultName)
        {
            return (String.IsNullOrEmpty(this.ItemName) ? "Item" : this.ItemName);
        }
    }
    #endregion
    #region class XmlPersistLoadArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při deserializaci objektu
    /// <summary>
    /// XmlPersistLoadArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při deserializaci objektu
    /// </summary>
    internal class XmlPersistLoadArgs
    {
        internal XmlPersistLoadArgs(PersistArgs parameters, PropInfo propInfo, Type dataType, TypeInfo dataTypeInfo, XmElement xmElement, XmAttribute xmAttribute)
        {
            this.Parameters = parameters;
            this.PropInfo = propInfo;
            this.DataType = dataType;
            this.DataTypeInfo = dataTypeInfo;
            this.XmElement = xmElement;
            this.XmAttribute = xmAttribute;
        }
        internal PersistArgs Parameters;
        internal PropInfo PropInfo;
        internal Type DataType;
        internal TypeInfo DataTypeInfo;
        internal XmElement XmElement;
        internal XmAttribute XmAttribute;
    }
    #endregion
    #region enum XmlPersistenceType
    /// <summary>
    /// Typ objektu z hlediska persistence
    /// </summary>
    internal enum XmlPersistenceType
    {
        /// <summary>
        /// Tento druh dat se neukládá
        /// </summary>
        None = 0,
        /// <summary>
        /// Jde o jednoduchou, jednorozměrnou hodnotu, kterou je možno uložit do XML atributu (Name = "value")
        /// </summary>
        Simple,
        /// <summary>
        /// Tato property je typu, který deklaruje, že sám provádí XmlPersistenci (z/do stringu). Do XML se formátuje jako Simple (do atributu).
        /// </summary>
        Self,
        /// <summary>
        /// Enumy
        /// </summary>
        Enum,
        /// <summary>
        /// Složený objekt, budeme hledat sadu jeho property. Pro představu: Jeden záznam (do šířky).
        /// </summary>
        Compound,
        /// <summary>
        /// Standardní pole prvků
        /// </summary>
        Array,
        /// <summary>
        /// IList, seznam, má více položek (řádků), přičemž v jednom řádku je uložen jeden objekt. Typicky List.
        /// </summary>
        IList,
        /// <summary>
        /// IDictionary, má více položek (řádků), přičemž v jednom řádku je uložena hodnota Key+Value.
        /// </summary>
        IDictionary
    }
    /// <summary>
    /// Určení úrovně, v níž se má hledat XML element
    /// </summary>
    internal enum XmlElementDepthType
    {
        None = 0,
        /// <summary>
        /// Kdekoliv (ve zdejší, v nižší i ve vyšší úrovni)
        /// </summary>
        Anywhere,
        /// <summary>
        /// Výhradně ve zdejší úrovni
        /// </summary>
        InCurrentDepth,
        /// <summary>
        /// Ve zdejší úrovni a v kterékoli z podřízených úrovní
        /// </summary>
        CurrentAndAnyChilds,
        /// <summary>
        /// Ve zdejší úrovni a v nejbližší podřízené úrovni
        /// </summary>
        CurrentAndMyChild,
        /// <summary>
        /// Pouze v podřízených úrovních
        /// </summary>
        OnlyInChilds,
        /// <summary>
        /// Pouze přímo podřízené úrovně (current + 1)
        /// </summary>
        OnlyMyOwnChild
    }
    #endregion

    #region XmDocument: obálka nad objektem System.Xml.Linq.XDocument
    /// <summary>
    /// XmDocument: obálka nad objektem System.Xml.Linq.XDocument
    /// </summary>
    internal class XmDocument
    {
        #region Konstrukce
        private XmDocument(System.Xml.Linq.XDocument xDoc)
        {
            this._XDocument = xDoc;
            this._CurrentElement = null;
        }
        /// <summary>
        /// Vrátí XML dokument z daného souboru
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static XmDocument CreateFromFile(string fileName)
        {
            if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
            {
                ResolveUtfMarker(fileName);     // Starší verze XmlPersist ukládaly XML soubor v kódování UTF-8, ale psaly do něj UTF-16. To se musí vyřešit nyní.
                XmDocument doc = new XmDocument(System.Xml.Linq.XDocument.Load(fileName));
                return doc;
            }
            return null;
        }
        /// <summary>
        /// Starší verze XmlPersist ukládaly XML soubor v kódování UTF-8, ale psaly do něj UTF-16. To se musí vyřešit nyní.
        /// </summary>
        /// <param name="fileName"></param>
        internal static void ResolveUtfMarker(string fileName)
        {
            if (String.IsNullOrEmpty(fileName) || !System.IO.File.Exists(fileName))
                return;
            string content = System.IO.File.ReadAllText(fileName);
            if (content.Contains(_MARKER_BAD))
            {
                content = content.Replace(_MARKER_BAD, _MARKER_CORRECT);
                System.IO.File.WriteAllText(fileName, content, Encoding.UTF8);
            }
        }
        private const string _MARKER_BAD = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
        private const string _MARKER_CORRECT = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

        /// <summary>
        /// Vrátí XML dokument z daného textu
        /// </summary>
        /// <param name="xmlContent"></param>
        /// <returns></returns>
        internal static XmDocument CreateFromString(string xmlContent)
        {
            if (!String.IsNullOrEmpty(xmlContent))
            {
                XmDocument doc = new XmDocument(System.Xml.Linq.XDocument.Parse(xmlContent));
                return doc;
            }
            return null;
        }
        public override string ToString()
        {
            if (this._XDocument == null) return "null";
            if (this._XDocument.Root == null) return "Empty";
            return "XmlDocument: " + this._XDocument.Root.Name.LocalName;
        }
        private System.Xml.Linq.XDocument _XDocument;
        private XmElement _CurrentElement;
        #endregion
        #region Hledání
        /// <summary>
        /// Vyhledá a vrátí přesně zadaný element.
        /// Zadání se provádí uvedením všech elementů počínaje Root elementem, až k elementu který chci vrátit.
        /// </summary>
        /// <param name="elementNames"></param>
        /// <returns></returns>
        internal XmElement FindElement(params string[] elementNames)
        {
            XElement findIn = this._XDocument.Root;
            XmElement result = null;
            if (elementNames != null && elementNames.Length > 0)
            {
                int len = elementNames.Length;
                if (findIn.Name.LocalName == elementNames[0])
                {
                    if (len == 1)
                        result = XmElement.Create(findIn);
                    else
                    {
                        for (int i = 1; i < len; i++)
                        {
                            string elementName = elementNames[i];
                            // XElement find = findIn.Element(elementName);
                            XElement find = findIn.Elements().FirstOrDefault(xe => xe.Name.LocalName == elementName);

                            if (find == null) break;
                            if (i < (len - 1))
                            {
                                findIn = find;
                            }
                            else
                            {
                                result = XmElement.Create(find);
                                break;
                            }
                        }
                    }
                }
            }
            this._CurrentElement = result;
            return result;
        }
        #endregion
    }
    #endregion
    #region XmElement: obálka nad objektem System.Xml.Linq.XElement
    /// <summary>
    /// XmElement: obálka nad objektem System.Xml.Linq.XElement
    /// </summary>
    internal class XmElement
    {
        #region Konstrukce
        private XmElement(System.Xml.Linq.XElement xEle)
        {
            this._XElement = xEle;
            this._XmAttributes = null;
        }
        /// <summary>
        /// Vrátí obálku XML elementu pro daný objekt
        /// </summary>
        /// <param name="findIn"></param>
        /// <returns></returns>
        internal static XmElement Create(XElement xEle)
        {
            XmElement xmEle = null;
            if (xEle != null)
                xmEle = new XmElement(xEle);
            return xmEle;
        }
        public override string ToString()
        {
            if (this._XElement == null) return "null";
            if (this._XElement.Name == null) return "Empty";
            return "XmlElement: " + this._XElement.Name.LocalName;
        }
        private System.Xml.Linq.XElement _XElement;
        private XmAttributes _XmAttributes;
        #endregion
        #region Property
        /// <summary>
        /// Jméno elementu, lokální (bez namespace)
        /// </summary>
        internal string Name { get { return this._XElement.Name.LocalName; } }
        /// <summary>
        /// Aktuální XElement = přímý přístup k objektu System.Xml.Linq.XElement, pro který je tento XmElement vytvořen
        /// </summary>
        internal XElement XElement { get { return this._XElement; } }
        /// <summary>
        /// Přístup k vnitřním elementům v tomto elementu.
        /// Vnitřní elementy jsou elementy právě v úrovni this.Childs, nikoli hlouběji zanořené.
        /// Vnitřní elementy jsou opět třídy shodné jako this (tj. XmElement), nejde o elementy System.Xml.Linq.XElement.
        /// </summary>
        internal IEnumerable<XmElement> XmElements { get { return new XmElementList(this); } }
        /// <summary>
		/// Zmapovaná sada atributů
		/// </summary>
		internal XmAttributes XmAttributes
        {
            get
            {
                if (this._XmAttributes == null)
                    this._ReadAttributes();
                return this._XmAttributes;
            }
        }
        #endregion
        #region Zmapování atributů
        private void _ReadAttributes()
        {
            this._XmAttributes = new XmAttributes(this);
            if (!this._XElement.HasAttributes) return;
            foreach (XAttribute xAtr in this._XElement.Attributes())
                this._XmAttributes.Add(xAtr);
        }
        #endregion
        #region Enumerace vnitřních elementů (enumerace elementů XmElement, nikoli XElement)
        protected class XmElementList : IEnumerable<XmElement>
        {
            internal XmElementList(XmElement parent)
            {
                this.Parent = parent;
            }
            protected XmElement Parent;
            IEnumerator<XmElement> IEnumerable<XmElement>.GetEnumerator()
            {
                return _GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _GetEnumerator();
            }
            /// <summary>
            /// Tvorba enumerátoru: z dat třídy XElement (systémová) z _XElement.Elements() 
            /// vytvořím položky třídy XmElement (zdejší) do lokálního seznamu, z nějž vrátím jeho enumerátor.
            /// </summary>
            /// <returns></returns>
            private IEnumerator<XmElement> _GetEnumerator()
            {
                if (this.Parent == null) return null;
                List<XmElement> list = new List<XmElement>();
                foreach (XElement xEle in this.Parent._XElement.Elements())
                    list.Add(XmElement.Create(xEle));
                return list.GetEnumerator();
            }
        }
        #endregion
        #region Vyhledání atributu a elementu
        /// <summary>
        /// Najde ve zdejších atributech atribut daného jména (case sensitive) a vrátí jeho hodnotu Value. Pokud je jich víc, vrátí první.
        /// Pokud neexistuje atribut, nebo neobsahuje value, vrací defaultValue.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        internal XmAttribute TryFindAttribute(string attributeName)
        {
            return this.XmAttributes.TryFind(attributeName);
        }
        /// <summary>
        /// Najde ve zdejších atributech atribut daného jména (case sensitive) a vrátí jeho hodnotu Value. Pokud je jich víc, vrátí první.
        /// Pokud neexistuje atribut, nebo neobsahuje value, vrací defaultValue.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        internal string FindAttributeValue(string attributeName, string defaultValue)
        {
            XmAttribute attr = this.XmAttributes.TryFind(attributeName);
            if (attr == null) return defaultValue;
            string value = attr.ValueFirstOrDefault;
            if (value == null) return defaultValue;
            return value;
        }
        /// <summary>
        /// Najde ve svých elementech element s daným jménem a vrátí jej.
        /// Pokud nenajde, vrátí null.
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        internal XmElement TryFindElement(string elementName)
        {
            if (String.IsNullOrEmpty(elementName))
                return null;
            return this.XmElements.FirstOrDefault(e => e.Name == elementName);
        }
        #endregion
    }
    #endregion
    #region XmAttributes a XmAttribute: slovník atributů
    /// <summary>
    /// Soupis atributů jednoho elementu
    /// </summary>
    internal class XmAttributes : IEnumerable<XmAttribute>
    {
        #region Konstrukce
        internal XmAttributes(XmElement xmEle)
        {
            this._XmElement = xmEle;
            this._AttributeDict = new Dictionary<string, XmAttribute>();
        }
        private XmElement _XmElement;
        private Dictionary<string, XmAttribute> _AttributeDict;
        /// <summary>
        /// Do sady atributů přidá hodnotu načtenou z dalšího atributu.
        /// Detekuje přitom lokální jména s tečkou, separuje název před tečkou.
        /// </summary>
        /// <param name="xAtr"></param>
        internal void Add(XAttribute xAtr)
        {
            // Rozdělím název atributu v místě tečky:
            string aNam = xAtr.Name.LocalName;          // Jméno, například "Value", nebo "Value.Type", nebo "Array.Range"
            string aSfx = null;                         // Zůstane null pro atributy, které ve jménu nemají tečku
            if (!String.IsNullOrEmpty(aNam) && aNam.Contains('.'))
            {
                int len = aNam.Length;
                int iDot = aNam.IndexOf('.');
                aSfx = (iDot < (len - 1) ? aNam.Substring(iDot + 1) : "");      // Text před tečkou = název logického atributu
                aNam = (iDot > 0 ? aNam.Substring(0, iDot) : "");               // Text za tečkou   = určuje property v XmAttribute, do které se uloží hodnota Value
            }

            // Najdu / vytvořím atribut:
            XmAttribute xmAtt;
            if (!this._AttributeDict.TryGetValue(aNam, out xmAtt))
            {
                xmAtt = new XmAttribute(aNam);
                this._AttributeDict.Add(aNam, xmAtt);
            }

            // Uložím suffix a hodnotu:
            xmAtt.Add(aSfx, xAtr.Value);
        }
        /// <summary>
        /// Najde a vrátí data pro daný název atributu, nebo vrátí null.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        internal XmAttribute TryFind(string attributeName)
        {
            XmAttribute result;
            if (this._AttributeDict.TryGetValue(attributeName, out result))
                return result;
            return null;
        }
        #endregion
        #region IEnumerable<XmAttribute> Members
        IEnumerator<XmAttribute> IEnumerable<XmAttribute>.GetEnumerator() { return this._AttributeDict.Values.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this._AttributeDict.Values.GetEnumerator(); }
        #endregion
    }
    /// <summary>
    /// XmAttribute: jeden atribut, logická jednotka (shrnutá z více záznamů shodného jména).
    /// Pokud jsou v elementu uvedeny atributy například Item = "hodnota", Item.Type = "String", Item.Indices = "[0,2,6]", 
    /// budou tyto atributy shrnuty v jednom objektu třídy XmAttribute
    /// </summary>
    internal class XmAttribute
    {
        #region Konstrukce
        internal XmAttribute(string name)
        {
            this.Name = name;
            this.Values = new List<string>();
            this.Suffixes = new Dictionary<string, string>();
        }
        public override string ToString()
        {
            string result = this.Name;
            string del = " = ";
            foreach (string value in this.Values)
            {
                result += del + value;
                del = ", ";
            }
            return result;
        }
        #endregion
        #region Property
        internal string Name { get; private set; }
        internal string Type { get { return GetSuffix("Type"); } }
        internal string Assembly { get { return GetSuffix("Assembly"); } }
        internal string Range { get { return GetSuffix("Range"); } }
        internal string Indices { get { return GetSuffix("Indices"); } }
        internal List<string> Values { get; private set; }
        internal string ValueFirstOrDefault { get { return (this.Values.Count > 0 ? this.Values[0] : null); } }
        internal Dictionary<string, string> Suffixes { get; private set; }
        #endregion
        #region Metody Add, ContainSuffix, GetSuffix
        /// <summary>
        /// Uloží do sebe hodnotu podle uvedeného suffixu.
        /// Příklad pro atribut: Value.Type = "Int32"
        /// Název atributu (this.Name) = "Value"
        /// Zde doddaný suffix = "Type", určuje že hodnota value se bude ukládat do property this.Type.
        /// Zde dodaná hodnota value nese vlastní data.
        /// </summary>
        /// <param name="suffix"></param>
        /// <param name="value"></param>
        internal void Add(string suffix, string value)
        {
            if (suffix == null)
                this.Values.Add(value);
            else
            {
                if (!this.Suffixes.ContainsKey(suffix))
                    this.Suffixes.Add(suffix, value);
                else
                    this.Suffixes[suffix] = value;
            }
        }
        /// <summary>
        /// Vrací příznak, zda tento atribut obsahuje daný suffix.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        internal bool ContainSuffix(string suffix)
        {
            if (suffix == null) return false;
            return this.Suffixes.ContainsKey(suffix);
        }
        /// <summary>
        /// Vrací hodnotu uloženou za daným suffixem.
        /// Pokud nebyl nalezen atribut se jménem this.Name + "." + suffix, vrátí null.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        internal string GetSuffix(string suffix)
        {
            if (suffix == null) return null;
            if (!this.Suffixes.ContainsKey(suffix)) return null;
            return this.Suffixes[suffix];
        }
        #endregion
    }
    #endregion

}
