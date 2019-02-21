// Supervisor: DAJ
// Part of Helios Green, proprietary software, (c) LCS International, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with LCS International, a. s. 
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
using System.IO;

// Tento soubor obsahuje třídy, které provádějí serializaci a deserializaci libovolného objektu do XML formátu.
// Tento soubor se nachází jednak v Greenu: Noris\App\Lcs\Base\WorkSchedulerPersistor.cs, a zcela identický i v GraphLibrary: \GraphLib\Shared\WorkSchedulerPersistor.cs
namespace Noris.LCS.Base.WorkScheduler
{
    // public rozhraní:
    #region class Persist : Třída, která zajišťuje persistenci dat do / z XML formátu
    /// <summary>
    /// Třída, která zajišťuje persistenci dat do / z XML formátu
    /// </summary>
    public static class Persist
    {
        #region Statické public metody
        /// <summary>
        /// Zajistí persistenci (uložení = serializaci) datového objektu do stringu podle implicitních parametrů.
		/// Vrátí stringovou reprezentaci objektu s defaultním nastavením.
        /// Změnu nastavení lze specifikovat zadáním druhého parametru, třídy <see cref="PersistArgs"/>.
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
        /// Parametry lze snadno získat ze statických property <see cref="PersistArgs.Default"/>, <see cref="PersistArgs.Compressed"/>, <see cref="PersistArgs.MinimalXml"/>...
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
        /// Změnu nastavení lze specifikovat zadáním druhého parametru, třídy <see cref="PersistArgs"/>.
        /// Tato varianta může deserializovat data z čitelného stringu, ze zazipovaného stringu i ze souboru (metoda provádí autodetect zadaného stringu).
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static object Deserialize(string source)
        {
            return Deserialize(CreateArgs(source));
        }
        /// <summary>
		/// Vytvoří objekt ze serializovaného stavu, který je definován v argumentu (může to být string, nebo soubor).
        /// Parametry lze snadno získat ze statických property <see cref="PersistArgs.Default"/>, <see cref="PersistArgs.Compressed"/>, <see cref="PersistArgs.MinimalXml"/>...
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
        /// Parametry lze snadno získat ze statických property <see cref="PersistArgs.Default"/>, <see cref="PersistArgs.Compressed"/>, <see cref="PersistArgs.MinimalXml"/>...
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
            if (source.Trim().StartsWith(PersistArgs.XmlStringHeader)) parameters.XmlContent = source;  // XML obsah
            else if (source.Contains("\\")) parameters.XmlFile = source;       // Soubor (obsahuje adresáře)
            else parameters.DataContent = source;                              // ZIP obsah (ten neobsahuje lomítka)
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
        /// <summary>
        /// Konstruktor
        /// </summary>
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
            this.CompressMode = XmlCompressMode.None;
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
                args.WriterSettings.NewLineHandling = NewLineHandling.Entitize;
                args.WriterSettings.NewLineChars = Environment.NewLine;
                args.WriterSettings.NewLineOnAttributes = false;
                args.WriterSettings.OmitXmlDeclaration = false;
                return args;
            }
        }
        /// <summary>
        /// Parametry pro vytváření komprimovaného stingu, s minimální XML režií
        /// </summary>
        public static PersistArgs Compressed
        {
            get
            {
                PersistArgs args = new PersistArgs();
                args.WriterSettings.ConformanceLevel = ConformanceLevel.Document;
                args.WriterSettings.Encoding = Encoding.UTF8;
                args.WriterSettings.CheckCharacters = false;
                args.WriterSettings.Indent = false;
                args.WriterSettings.IndentChars = "";
                args.WriterSettings.NewLineHandling = NewLineHandling.Entitize;
                args.WriterSettings.NewLineChars = Environment.NewLine;
                args.WriterSettings.NewLineOnAttributes = false;
                args.WriterSettings.OmitXmlDeclaration = false;
                args.CompressMode = XmlCompressMode.Compress;
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
        /// Čitelný obsah XML dat.
        /// Pokud vstupní data <see cref="DataContent"/> jsou komprimovaná, pak zde v <see cref="XmlContent"/> jsou data přítomna v neserializované podobě.
        /// </summary>
        public string XmlContent { get; set; }
        /// <summary>
        /// Serializovaná data.
        /// Zde mohou být data v komprimované podobě.
        /// Do této property se při deserializaci mají ukládat data zvenku (tj. i ze souboru), zde proběhne jejich detekce a případná dekomprimace do property <see cref="XmlContent"/>.
        /// Při serializaci se mají výstupní data číst odsud, zde budou v případě požadavku na komprimaci <see cref="CompressMode"/> uložena komprimovaná.
        /// </summary>
        public string DataContent { get; set; }
        /// <summary>
        /// Verze, pod kterou chceme ukládat data. Defaultní = null = nejnovější.
        /// Možnosti jsou <see cref="Version100"/> nebo <see cref="Version200"/>.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Verze 1.00
        /// </summary>
        public const string Version100 = XmlPersist.XmlPersistV1.Version;
        /// <summary>
        /// Verze 2.00
        /// </summary>
        public const string Version200 = XmlPersist.XmlPersistV2.Version;
        /// <summary>
        /// Režim komprimace.
        /// Defaultní je <see cref="XmlCompressMode.None"/> = výstupní string není komprimovaný.
        /// </summary>
        public XmlCompressMode CompressMode { get; set; }
        /// <summary>
        /// Stav deserializace = obsahuje případné problémy
        /// </summary>
        public XmlDeserializeStatus DeserializeStatus { get; set; }
        /// <summary>
        /// Počet uložených primitivních hodnot
        /// </summary>
        public int SavePrimitivesCount { get; set; }
        /// <summary>
        /// Sumárné délka uložených primitivních hodnot
        /// </summary>
        public int SavePrimitivesLength { get; set; }
        /// <summary>
        /// Kompletní obsah všech uložených Primitives, oddělený Cr+Lf.
        /// Pokud je null, nevytváří se.
        /// </summary>
        public StringBuilder SavePrimitivesContent { get; set; }
        /// <summary>
        /// Začátek XML stringu pro jeho detekci: "&lt;?xml version="
        /// </summary>
        public static string XmlStringHeader { get { return "<?xml version="; } }
    }
    /// <summary>
    /// Stav serializace/deserializace
    /// </summary>
    public enum XmlDeserializeStatus
    {
        /// <summary>
        /// Neurčen
        /// </summary>
        None,
        /// <summary>
        /// Zpracovává se
        /// </summary>
        Processing,
        /// <summary>
        /// Nejsou vstupy
        /// </summary>
        NotInput,
        /// <summary>
        /// Chybný formát
        /// </summary>
        BadFormatPersistent,
        /// <summary>
        /// Chybný formát dat
        /// </summary>
        BadFormatData,
        /// <summary>
        /// Chybný formát hodnot
        /// </summary>
        BadFormatValue,
        /// <summary>
        /// Hotovo
        /// </summary>
        Done
    }
    /// <summary>
    /// Režim komprimace serializovaného stringu
    /// </summary>
    public enum XmlCompressMode
    {
        /// <summary>
        /// Bez komprimace, čitelný text.
        /// </summary>
        None = 0,
        /// <summary>
        /// Komprimovaný string.
        /// </summary>
        Compress,
        /// <summary>
        /// Automaticky: při serializaci se komprimace použije jen tehdy, pokud délka čitelného XML stringu přesáhne 2KB.
        /// </summary>
        Auto
    }
    #endregion

    // interfaces a atributy pro public použití:
    #region interface IXmlPersistNotify : dává objektu možnost být informován o procesech XmlPersist (Save / Load) pomocí property XmlPersistState
    /// <summary>
    /// interface IXmlPersistNotify : dává objektu možnost být informován o procesech XmlPersist (Save / Load) pomocí obsluhy set accessoru property XmlPersistState.
    /// Jakýkoli objekt, který chce být informován o tom, že je ukládán do XML / obnovován z XML, musí deklarovat interface IXmlPersistNotify.
    /// Pak v procesu serializace (ukládání dat z objektu to XML) bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.SaveBegin"/> a po dokončení ukládán ípak hodnota <see cref="XmlPersistState.SaveDone"/> a <see cref="XmlPersistState.None"/>.
    /// Obdobně při načítání dat z XML do objektu bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.LoadBegin"/> a po dokončení načítání pak hodnota <see cref="XmlPersistState.LoadDone"/> a <see cref="XmlPersistState.None"/>.
    /// </summary>
    public interface IXmlPersistNotify
    {
        /// <summary>
        /// Aktuální stav procesu XML persistence.
        /// Umožňuje persistovanému objektu reagovat na ukládání nebo na načítání dat.
        /// Do této property vkládá XmlPersistor hodnotu odpovídající aktuální situaci.
        /// Datová instance může v set accessoru zareagovat a například připravit data pro Save, 
        /// anebo dokončit proces Load (navázat si další data nebo provést přepočty a další reakce).
        /// V procesu serializace (ukládání dat z objektu to XML) bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.SaveBegin"/> a po dokončení ukládán ípak hodnota <see cref="XmlPersistState.SaveDone"/> a <see cref="XmlPersistState.None"/>.
        /// Obdobně při načítání dat z XML do objektu bude do property <see cref="IXmlPersistNotify.XmlPersistState"/> vložena hodnota <see cref="XmlPersistState.LoadBegin"/> a po dokončení načítání pak hodnota <see cref="XmlPersistState.LoadDone"/> a <see cref="XmlPersistState.None"/>.
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
    /// Pro konverzi jednoduchých typů lze použít statické metody třídy <see cref="Convertor"/>, které konvertují jakýkoli primární datový typ z/na string.
    /// </summary>
    public interface IXmlSerializer
    {
        /// <summary>
        /// Tato property má obsahovat (get vrací, set akceptuje) XML data z celého aktuálního objektu.
        /// </summary>
        string XmlSerialData { get; set; }
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
    public class PropertyNameAttribute : PersistAttribute
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
    /// Umožní potlačit ukládání této property.
    /// Pokud není specifikováno, property se uloží, pokud k ní existuje SET metoda (i kdyby byla privátní).
    /// V některých případech je vhodné hodnotu neukládat (a nenačítat), například pokud property při setování hodnotu ukládá do dalších property.
    /// Rozhodně je nutno potlačit persistenci property, které referencují globální (systémové) objekty, ty by se jinak persistovaly také!!!
    /// </summary>
    public class PersistingEnabledAttribute : PersistAttribute
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

    #region class Convertor : Knihovna statických konverzních metod mezi simple typy a stringem
    /// <summary>
    /// Convertor : Knihovna statických konverzních metod mezi simple typy a stringem
    /// </summary>
    public static class Convertor
    {
        #region Sada krátkých metod pro serializaci a deserializaci Simple typů (jsou vyjmenované v TypeLibrary._SimpleTypePrepare())
        #region System types
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BooleanToString(object value)
        {
            return ((Boolean)value ? "true" : "false");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="textTrue"></param>
        /// <param name="textFalse"></param>
        /// <returns></returns>
        public static string BooleanToString(object value, string textTrue, string textFalse)
        {
            return ((Boolean)value ? textTrue : textFalse);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToBoolean(string text)
        {
            return (!String.IsNullOrEmpty(text) && (text.ToLower() == "true" || text == "1" || text.ToLower() == "a" || text.ToLower() == "y"));
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ByteToString(object value)
        {
            return ((Byte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Byte)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Byte)0;
            Byte b = (Byte)(value & 0x00FF);
            return b;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToString(object value)
        {
            DateTime dateTime = (DateTime)value;
            if (dateTime.Millisecond == 0 && dateTime.Second == 0)
                return dateTime.ToString("D", _Dtfi);
            return dateTime.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            DateTime value;
            if (DateTime.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParseExact(text, "F", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;
            if (DateTime.TryParse(text, _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value))
                return value;

            return DateTime.MinValue;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeToSerial(object value)
        {
            DateTime dateTime = (DateTime)value;
            string u = (dateTime.Kind == DateTimeKind.Utc ? "U" : (dateTime.Kind == DateTimeKind.Local ? "L" : ""));
            bool h = (dateTime.TimeOfDay.Ticks > 0L);
            bool s = (h && dateTime.Second > 0);
            bool f = (h && dateTime.Millisecond > 0);
            string format = (f ? "yyyyMMddHHmmssfff" : (s ? "yyyyMMddHHmmss" : (h ? "yyyyMMddHHmm" : "yyyyMMdd")));
            return u + dateTime.ToString(format);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object SerialToDateTime(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTime.MinValue;
            string serial = text.Trim();
            if (serial.Length < 8) return DateTime.MinValue;
            DateTimeKind kind = _GetDateTimeKind(ref serial);
            int length = serial.Length;
            if (length < 8) return DateTime.MinValue;
            int dy = _GetInt(serial, 0, 4, 100);
            int dm = _GetInt(serial, 4, 2, 1);
            int dd = _GetInt(serial, 6, 2, 1);
            if (dm < 1 || dm > 12 || dd < 1 || dd > 31) return DateTime.MinValue;

            try
            {
                if (length == 8) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int th = _GetInt(serial, 8, 2, 0);
                int tm = _GetInt(serial, 10, 2, 0);
                int ts = _GetInt(serial, 12, 2, 0);
                if (th < 0 || th > 23 || tm < 0 || tm > 59 || ts < 0 || ts > 59) return new DateTime(dy, dm, dd, 0, 0, 0, kind);

                int tf = _GetInt(serial, 14, 3, 0);
                return new DateTime(dy, dm, dd, th, tm, ts, tf, kind);
            }
            catch { }
            return DateTime.MinValue;
        }
        private static DateTimeKind _GetDateTimeKind(ref string serial)
        {
            switch (serial[0])
            {
                case 'U':
                    serial = serial.Substring(1);
                    return DateTimeKind.Utc;
                case 'L':
                    serial = serial.Substring(1);
                    return DateTimeKind.Local;
            }
            return DateTimeKind.Unspecified;
        }
        /// <summary>
        /// Vrátí číslo ze substringu
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="begin"></param>
        /// <param name="length"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        private static int _GetInt(string serial, int begin, int length, int defValue)
        {
            if (serial.Length < (begin + length)) return defValue;
            int value;
            if (!Int32.TryParse(serial.Substring(begin, length), out value)) return defValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DateTimeOffsetToString(object value)
        {
            DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset.ToString("F", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDateTimeOffset(string text)
        {
            if (String.IsNullOrEmpty(text)) return DateTimeOffset.MinValue;
            DateTimeOffset value;
            if (!DateTimeOffset.TryParseExact(text, "D", _Dtfi, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault, out value)) return DateTimeOffset.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DecimalToString(object value)
        {
            return ((Decimal)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDecimal(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Decimal)0;
            Decimal value;
            if (!Decimal.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Decimal)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DoubleToString(object value)
        {
            return ((Double)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToDouble(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Double)0;
            Double value;
            if (!Double.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Double)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GuidToString(object value)
        {
            return ((Guid)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToGuid(string text)
        {
            if (String.IsNullOrEmpty(text)) return Guid.Empty;
            Guid value;
            if (!Guid.TryParse(text, out value)) return Guid.Empty;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CharToString(object value)
        {
            return ((Char)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToChar(string text)
        {
            if (String.IsNullOrEmpty(text)) return Char.MinValue;
            Char value;
            if (!Char.TryParse(text, out value)) return Char.MinValue;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int16ToString(object value)
        {
            return ((Int16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int16)0;
            Int16 value;
            if (!Int16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int32ToString(object value)
        {
            return ((Int32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Int64ToString(object value)
        {
            return ((Int64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int64)0;
            Int64 value;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string IntPtrToString(object value)
        {
            return ((IntPtr)value).ToInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (IntPtr)0;
            Int64 int64;
            if (!Int64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out int64)) return (IntPtr)0;
            return new IntPtr(int64);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SByteToString(object value)
        {
            return ((SByte)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSByte(string text)
        {
            if (String.IsNullOrEmpty(text)) return (SByte)0;
            SByte value;
            if (!SByte.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (SByte)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SingleToString(object value)
        {
            return ((Single)value).ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSingle(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Single)0;
            Single value;
            if (!Single.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Single)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string StringToString(object value)
        {
            if (value == null) return null;
            return value as string;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToString(string text)
        {
            if (text == null) return null;
            return text;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TimeSpanToString(object value)
        {
            return ((TimeSpan)value).ToString("G", _Dtfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToTimeSpan(string text)
        {
            if (String.IsNullOrEmpty(text)) return TimeSpan.Zero;
            TimeSpan value;
            if (!TimeSpan.TryParse(text, _Dtfi, out value)) return TimeSpan.Zero;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt16ToString(object value)
        {
            return ((UInt16)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt16(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt16)0;
            UInt16 value;
            if (!UInt16.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt16)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt32ToString(object value)
        {
            return ((UInt32)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt32)0;
            UInt32 value;
            if (!UInt32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UInt64ToString(object value)
        {
            return ((UInt64)value).ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUInt64(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UInt64)0;
            UInt64 value;
            if (!UInt64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (UInt64)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UIntPtrToString(object value)
        {
            return ((UIntPtr)value).ToUInt64().ToString("G");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToUIntPtr(string text)
        {
            if (String.IsNullOrEmpty(text)) return (UIntPtr)0;
            UInt64 uint64;
            if (!UInt64.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out uint64)) return (UIntPtr)0;
            return new UIntPtr(uint64);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        #endregion
        #region Nullable types
        public static string Int32NToString(object value)
        {
            Int32? v = (Int32?)value;
            return (v.HasValue ? v.Value.ToString() : "null");
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToColor(string text)
        {
            if (String.IsNullOrEmpty(text)) return Color.Empty;
            string t = text.Trim();                      // Jméno "Orchid", nebo hexa #806040 (RGB), nebo 0xD02000 (RGB), nebo hexa "#FF808040" (ARGB) nebo 0x40C0C0FF (ARGB).
            if (t.Length == 7 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 6)))
                return StringRgbToColor(t.Substring(1, 6));
            if (t.Length == 8 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 6)))
                return StringRgbToColor(t.Substring(2, 6));
            if (t.Length == 9 && t[0] == '#' && ContainOnlyHexadecimals(t.Substring(1, 8)))
                return StringARgbToColor(t.Substring(1, 8));
            if (t.Length == 10 && t.Substring(0, 2).ToLower() == "0x" && ContainOnlyHexadecimals(t.Substring(2, 8)))
                return StringARgbToColor(t.Substring(2, 8));
            return StringNameToColor(t);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor z hodnoty "RRGGBB" na Color, kde RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static object StringRgbToColor(string t)
        {
            int r = HexadecimalToInt32(t.Substring(0, 2));
            int g = HexadecimalToInt32(t.Substring(2, 2));
            int b = HexadecimalToInt32(t.Substring(4, 2));
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor z hodnoty "AARRGGBB" na Color, kde aa, RR, GG, BB je hexadecimální číslo
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static object StringARgbToColor(string t)
        {
            int a = HexadecimalToInt32(t.Substring(0, 2));
            int r = HexadecimalToInt32(t.Substring(2, 2));
            int g = HexadecimalToInt32(t.Substring(4, 2));
            int b = HexadecimalToInt32(t.Substring(6, 2));
            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string PointToString(object value)
        {
            Point data = (Point)value;
            return data.X.ToString() + ";" + data.Y.ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToPoint(string text)
        {
            if (String.IsNullOrEmpty(text)) return Point.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return Point.Empty;
            int x = StringInt32(items[0]);
            int y = StringInt32(items[1]);
            return new Point(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string PointFToString(object value)
        {
            PointF data = (PointF)value;
            return data.X.ToString("N", _Nmfi) + ";" + data.Y.ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToPointF(string text)
        {
            if (String.IsNullOrEmpty(text)) return PointF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return PointF.Empty;
            Single x = StringSingle(items[0]);
            Single y = StringSingle(items[1]);
            return new PointF(x, y);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string RectangleToString(object value)
        {
            Rectangle data = (Rectangle)value;
            return data.X.ToString() + ";" + data.Y.ToString() + ";" + data.Width.ToString() + ";" + data.Height.ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string RectangleFToString(object value)
        {
            RectangleF data = (RectangleF)value;
            return data.X.ToString("N", _Nmfi) + ";" + data.Y.ToString("N", _Nmfi) + ";" + data.Width.ToString("N", _Nmfi) + ";" + data.Height.ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SizeToString(object value)
        {
            Size data = (Size)value;
            return data.Width.ToString() + ";" + data.Height.ToString();
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSize(string text)
        {
            if (String.IsNullOrEmpty(text)) return Size.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return Size.Empty;
            int w = StringInt32(items[0]);
            int h = StringInt32(items[1]);
            return new Size(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SizeFToString(object value)
        {
            SizeF data = (SizeF)value;
            return data.Width.ToString("N", _Nmfi) + ";" + data.Height.ToString("N", _Nmfi);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToSizeF(string text)
        {
            if (String.IsNullOrEmpty(text)) return SizeF.Empty;
            string[] items = text.Split(';');
            if (items.Length != 2) return SizeF.Empty;
            Single w = StringSingle(items[0]);
            Single h = StringSingle(items[1]);
            return new SizeF(w, h);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static object StringToFontStyle(string text)
        {
            if (String.IsNullOrEmpty(text)) return FontStyle.Regular;
            FontStyle result = (text.Contains("B") ? FontStyle.Bold : FontStyle.Regular) |
                               (text.Contains("I") ? FontStyle.Italic : FontStyle.Regular) |
                               (text.Contains("S") ? FontStyle.Strikeout : FontStyle.Regular) |
                               (text.Contains("U") ? FontStyle.Underline : FontStyle.Regular);
            return result;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FontToString(object value)
        {
            if (value == null) return "";
            Font font = (Font)value;
            return font.Name + ";" + SingleToString(font.SizeInPoints) + ";" + FontStyleToString(font.Style) + ";" + ByteToString(font.GdiCharSet);
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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
        #region User types : je vhodnější persistovat je pomocí interface IXmlSerializer (pomocí property string IXmlSerializer.XmlSerialData { get; set; } )
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
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Int32 StringInt32(string text)
        {
            if (String.IsNullOrEmpty(text)) return (Int32)0;
            Int32 value;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.Any, _Nmfi, out value)) return (Int32)0;
            return value;
        }
        /// <summary>
        /// Konkrétní konvertor
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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
    #endregion

    // XmlPersist, TypeLibrary, XmDocument : výkonné soukromé kódy
    /// <summary>
    /// Internal výkonná třída pro XML persistenci. 
    /// Tuto třídu nevyužívat z aplikačního kódu, má se používat třída (Djs.Tools.XmlPersistor.)Persist !!!
    /// </summary>
    internal sealed class XmlPersist : IDisposable
    {
        #region Konstrukce
        /// <summary>
        /// Konstruktor, používá se ze statických metod této třídy
        /// </summary>
        private XmlPersist()
        {
            this._TypeLibrary = new TypeLibrary();
        }
        /// <summary>
        /// Provozní instance knihovny použitých typů
        /// </summary>
        private TypeLibrary _TypeLibrary;
        void IDisposable.Dispose()
        {
            ((IDisposable)this._TypeLibrary).Dispose();
            this._TypeLibrary = null;
        }
        #endregion
        #region Statické internal metody = jediný veřejný přístup
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
                _PrepareDataFromXml(parameters);
            }
            if (parameters.XmlFile != null)
                SaveXmlToFile(parameters.DataContent, parameters.XmlFile);

            return parameters.DataContent;
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
                _PrepareXmlFromData(parameters);
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
                _PrepareXmlFromData(parameters);
                object result = xmlPersist._Deserialize(parameters);
                xmlPersist._CloneProperties(result, data);
            }
        }
        #endregion
        #region Privátní výkonné metody: Save, Load, CloneProperties, komprimace a dekomprimace
        /// <summary>
        /// Serializuje daný objekt s danými parametry.
        /// Výstupem je XML string.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string _Serialize(object data, PersistArgs parameters)
        {
            this.PreparePersistAdapter(parameters.Version);

            XmlDocument xDoc = new System.Xml.XmlDocument();
            xDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            this.PersistAdapter.Save(data, parameters, xDoc);

            //parameters.WriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            //parameters.WriterSettings.ConformanceLevel = ConformanceLevel.Document;
            //parameters.WriterSettings.OmitXmlDeclaration = false;
            //parameters.WriterSettings.WriteEndDocumentOnClose = true;

            StringBuilder sb = new StringBuilder();
            XmlWriter xw = XmlWriter.Create(sb, parameters.WriterSettings);
            xDoc.WriteTo(xw);
            xw.Flush();

            return sb.ToString();
        }
        /// <summary>
        /// Z dodaného XML obsahu sestaví a vrátí odpovídající objekt.
        /// Vstupní string se serializovaným obsahem očekává v <see cref="PersistArgs.XmlContent"/>.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private object _Deserialize(PersistArgs parameters)
        {
            parameters.DeserializeStatus = XmlDeserializeStatus.Processing;

            XmDocument xmDoc = null;
            if (!String.IsNullOrEmpty(parameters.XmlContent))
                xmDoc = XmDocument.CreateFromString(parameters.XmlContent);
            if (xmDoc == null)
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.NotInput;
                return null;
            }

            if (!this.PreparePersistAdapter(xmDoc))
            {
                parameters.DeserializeStatus = XmlDeserializeStatus.BadFormatPersistent;
                return null;
            }

            parameters.DeserializeStatus = XmlDeserializeStatus.Done;
            return this.PersistAdapter.Load(parameters, xmDoc);
        }
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
                if (propTrg.CanWrite && TypeLibrary.PropInfo.IsPropertyCloneable(propTrg))
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
        /// <summary>
        /// Metoda zajistí provedení komprimace dodaných XML dat podle nastavení v parametru.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static void _PrepareDataFromXml(PersistArgs parameters)
        {
            string xmlContent = parameters.XmlContent;
            string dataContent = xmlContent;
            XmlCompressMode mode = parameters.CompressMode;
            if (xmlContent != null && (mode == XmlCompressMode.Compress || (mode == XmlCompressMode.Auto && xmlContent.Length >= 2048)))
                dataContent = Compress(xmlContent, false);
            parameters.DataContent = dataContent;
        }
        /// <summary>
        /// Metoda zajistí provedení dekomprimace dodaných XML dat.
        /// Zajistí načtení dat ze souboru <see cref="PersistArgs.XmlFile"/>, pokud je to zapotřebí.
        /// Zajistí dekomprimaci z <see cref="PersistArgs.DataContent"/>, pokud tam jsou data komprimovaná.
        /// Výsledkem je, že v property <see cref="PersistArgs.XmlContent"/> je čitelný serializovaný obsah.
        /// </summary>
        /// <param name="parameters"></param>
        private static void _PrepareXmlFromData(PersistArgs parameters)
        {
            // a) načíst ze souboru, pokud nemám jiná data zadaná:
            if (String.IsNullOrEmpty(parameters.XmlContent) && String.IsNullOrEmpty(parameters.DataContent) && !String.IsNullOrEmpty(parameters.XmlFile))
                parameters.DataContent = System.IO.File.ReadAllText(parameters.XmlFile, Encoding.UTF8);
            // b) zpracovat data do xml, včetně dekomprimace:
            if (String.IsNullOrEmpty(parameters.XmlContent) && !String.IsNullOrEmpty(parameters.DataContent))
            {
                string dataContent = parameters.DataContent;
                if (dataContent.Trim().StartsWith(PersistArgs.XmlStringHeader, StringComparison.InvariantCultureIgnoreCase))
                    parameters.XmlContent = dataContent;
                else
                    // Máme data, ale nezačínají tak, jak by měla (""), zkusíme provést dekomprimaci:
                    parameters.XmlContent = TryDecompress(dataContent);
            }
        }
        #endregion
        #region Adapter na konkrétní formátovací algoritmy
        /// <summary>
        /// Připraví adapter <see cref="PersistAdapter"/> podle zadané verze. Implicitní je V2.
        /// Používá se při Serializaci (=Save).
        /// </summary>
        /// <param name="version"></param>
        private void PreparePersistAdapter(string version = null)
        {
            if (!String.IsNullOrEmpty(version))
            {
                if (version == XmlPersistV1.Version)
                    this.PersistAdapter = new XmlPersistV1(this);
                else if (version == PersistArgs.Version200)
                    this.PersistAdapter = new XmlPersistV2(this);
            }
            if (this.PersistAdapter == null)
                this.PersistAdapter = new XmlPersistV2(this);
        }
        /// <summary>
        /// Najde a připraví adapter <see cref="PersistAdapter"/> pro daný dokument. Detekuje obsah dokumentu.
        /// Pokud nelze detekovat, vrací false (=BadFormat).
        /// </summary>
        /// <param name="xmDoc"></param>
        private bool PreparePersistAdapter(XmDocument xmDoc)
        {
            if (xmDoc == null) return false;
            IXmlPersistVersion persistAdapter = null;

            persistAdapter = new XmlPersistV2(this);
            if (persistAdapter.IsValidFormat(xmDoc))
            {
                this.PersistAdapter = persistAdapter;
                return true;
            }

            persistAdapter = new XmlPersistV1(this);
            if (persistAdapter.IsValidFormat(xmDoc))
            {
                this.PersistAdapter = persistAdapter;
                return true;
            }

            return false;
        }
        /// <summary>
        /// Adapter na konkrétní persistor dané verze
        /// </summary>
        private IXmlPersistVersion PersistAdapter;
        #endregion
        #region class ArrayIndices
        /// <summary>
        /// Třída pro analýzu rozměrů pole, serializaci a deserializaci rozměrů, a pro enumeraci přes všechny buňky pole
        /// </summary>
        internal class ArrayIndices
        {
            #region Konstruktory, základní data, serializace, deserializace
            /// <summary>
            /// Konstruktor pro konkrétní pole
            /// </summary>
            /// <param name="array"></param>
            public ArrayIndices(Array array)
            {
                if (array != null)
                {
                    int rank = array.Rank;
                    this.Rank = rank;
                    this.IndicesRange = new int[this.Rank, 3];
                    for (int r = 0; r < rank; r++)
                    {
                        this.IndicesRange[r, 0] = array.GetLowerBound(r);
                        this.IndicesRange[r, 1] = array.GetLength(r);
                        this.IndicesRange[r, 2] = array.GetUpperBound(r);
                    }
                }
            }
            /// <summary>
            /// Konstruktor pro serializovanou deklaraci pole
            /// </summary>
            /// <param name="serial"></param>
            public ArrayIndices(string serial)
            {
                string[] parts = DeSerialString(serial);             // Ze stringu "[0+10,0+20,0+30]" vrací části "0+10"; "0+20"; "0+30".
                if (parts != null)
                {
                    int rank = parts.Length;
                    int[,] indices = new int[rank, 3];
                    bool isValid = (rank > 0);
                    for (int r = 0; r < rank; r++)
                    {
                        string[] items = parts[r].Split('+');        // "0+10"  =>  "0"; "10"
                        int lowerBounds, length;
                        if (items.Length == 2 && Int32.TryParse(items[0].Trim(), out lowerBounds) && Int32.TryParse(items[1].Trim(), out length))
                        {
                            indices[r, 0] = lowerBounds;
                            indices[r, 1] = length;
                            indices[r, 2] = lowerBounds + length - 1;
                        }
                        else
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        this.Rank = rank;
                        this.IndicesRange = indices;
                    }
                }
            }
            /// <summary>
            /// Obsahuje serializovanou hodnotu rozměrů pole, například pole deklarované new int[10,20,30] zde obsahuje text: "[0+10,0+20,0+30]".
            /// Z této serializované formy lze následně vytvořit new instanci <see cref="ArrayIndices"/> konstruktorem <see cref="ArrayIndices(String)"/>.
            /// </summary>
            public string Serial
            {
                get
                {
                    string serial = "[";
                    for (int r = 0; r < this.Rank; r++)
                        serial += (r == 0 ? "" : ",") + this.IndicesRange[r, 0].ToString() + "+" + this.IndicesRange[r, 1].ToString();
                    serial += "]";
                    return serial;
                }
            }
            /// <summary>
            /// Počet dimenzí pole = počet číslic v adrese buňky, 
            /// například pole deklarované new int[10,20,30] má <see cref="Rank"/> = 3
            /// </summary>
            public int Rank { get; private set; }
            /// <summary>
            /// Rozsah jednotlivých dimenzí, 
            /// například pole deklarované new int[10,20,30] má <see cref="IndicesRange"/> obsahující tři řádky, což odpovídá <see cref="Rank"/> (první index pole <see cref="IndicesRange"/> má hodnoty 0,1,2).
            /// Každý řádek obsahuje tři čísla, kde první číslo = <see cref="IndicesRange"/>[r, 0] obsahuje dolní index pole = <see cref="Array.GetLowerBound(int)"/>, což je nejčastěji 0.
            /// Druhé číslo v řádku <see cref="IndicesRange"/>[r, 1] obsahuje počet prvků pole = <see cref="Array.GetLength(int)"/>.
            /// Třetí číslo v řádku <see cref="IndicesRange"/>[r, 2] obsahuje horní index pole = <see cref="Array.GetUpperBound(int)"/>.
            /// </summary>
            public int[,] IndicesRange { get; private set; }
            /// <summary>
            /// Vizualizace this instance: vrací <see cref="Serial"/>
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return this.Serial;
            }
            /// <summary>
            /// Vizualizace konkrétní pozice, vrací typicky "[0,12,2]" pro pole, obshaující hodnoty { 0, 12, 2 }
            /// </summary>
            /// <param name="indices"></param>
            /// <returns></returns>
            public static string ToString(int[] indices)
            {
                if (indices == null) return "Null";
                string text = "";
                for (int i = 0; i < indices.Length; i++)
                    text = (i == 0 ? "[" : text + ",") + indices[i].ToString();
                if (text.Length > 0) text = text + "]";
                return text;
            }
            #endregion
            #region Generátor pole pro dané rozměry
            /// <summary>
            /// Metoda vytvoří a vrátí pole daného typu prvku, s rozměry uloženými v this instanci.
            /// </summary>
            /// <param name="elementType"></param>
            /// <returns></returns>
            public Array CreateArray(Type elementType)
            {
                int rank = this.Rank;
                if (rank <= 0) return null;
                int[] lowerBounds = new int[rank];
                int[] lengths = new int[rank];
                for (int r = 0; r < rank; r++)
                {
                    lowerBounds[r] = this.IndicesRange[r, 0];
                    lengths[r] = this.IndicesRange[r, 1];
                }
                return Array.CreateInstance(elementType, lengths, lowerBounds);
            }
            #endregion
            #region Static Serializace indexu prvku
            /// <summary>
            /// Vrátí serializovaný index jednoduchého pole, např. pro pole int[] = { 0, 5, 60} vrátí string "[0,5,60]"
            /// </summary>
            /// <param name="indices"></param>
            /// <returns></returns>
            public static string SerialIndices(int[] indices)
            {
                if (indices == null) return "[]";
                int rank = indices.Length;
                string serial = "[";
                for (int r = 0; r < rank; r++)
                    serial += (r == 0 ? "" : ",") + indices[r].ToString();
                return serial + "]";
            }
            /// <summary>
            /// Vrátí deserializovaný index jednoduchého pole, např. pro string "[0,5,60]" vrátí pole int[] = { 0, 5, 60}
            /// </summary>
            /// <param name="serial"></param>
            /// <returns></returns>
            public static int[] DeSerialIndices(string serial)
            {
                int[] indices = null;
                string[] parts = DeSerialString(serial);            // Ze stringu "[0,5,60]" vrací části "0"; "5"; "60".
                if (parts != null)
                {
                    int rank = parts.Length;
                    indices = new int[rank];
                    for (int r = 0; r < rank; r++)
                    {
                        int index;
                        if (Int32.TryParse(parts[r].Trim(), out index))
                            indices[r] = index;
                    }
                }
                return indices;
            }
            /// <summary>
            /// Vrátí jednotlivé prvky ze serializovaného stringu.
            /// String musí mít formu "[x,y,z]", výstupem je pole { "x"; "y"; "z" }.
            /// </summary>
            /// <param name="serial"></param>
            /// <returns></returns>
            private static string[] DeSerialString(string serial)
            {
                string[] parts = null;
                if (!String.IsNullOrEmpty(serial))
                {
                    string text = serial.Trim();
                    if (text.Length > 2 && text.StartsWith("[") && text.EndsWith("]"))   // "[0+10,0+20,0+30]"
                    {
                        text = text.Substring(1, text.Length - 2).Trim();                // "0+10,0+20,0+30"
                        parts = text.Split(',');                                         // "0+10"; "0+20"; "0+30"
                    }
                }
                return parts;
            }
            #endregion
            #region Enumerace
            /// <summary>
            /// Metoda vygeneruje a vrátí první sadu idnexů, ukazující typicky na [0,0,...].
            /// Sadu si interně zapamatuje.
            /// <para/>
            /// Typické použití: for (int[] indices = <see cref="ArrayIndices.FirstIndices()"/>; 
            /// <see cref="ArrayIndices.IsValidIndices"/>(indices); 
            /// indices = <see cref="ArrayIndices.NextIndices"/>(indices)) { akce ... }
            /// </summary>
            /// <returns></returns>
            public int[] FirstIndices()
            {
                int[] indices = _FirstIndices;
                return indices;
            }
            /// <summary>
            /// Vrací true, pokud daná sada indexů je platná
            /// </summary>
            /// <param name="indices"></param>
            public bool IsValidIndices(int[] indices)
            {
                if (indices == null) return false;
                int rank = this.Rank;
                if (rank <= 0 || indices.Length != rank) return false;
                for (int r = 0; r < rank; r++)
                {
                    if ((indices[r] < this.IndicesRange[r, 0]) || (indices[r] > this.IndicesRange[r, 2])) return false;
                }
                return true;
            }
            /// <summary>
            /// Vrátí sadu indexu ukazující na další prvek pole. Může vrátit sadu, ukazující za poslední prvek; pak podmínka IsValidIndices(indices) vrací false.
            /// </summary>
            /// <param name="indices"></param>
            /// <returns></returns>
            public int[] NextIndices(int[] indices)
            {
                int rank = this.Rank;
                if (rank <= 0) return null;
                int[] next = new int[rank];
                indices.CopyTo(next, 0);

                // Nejprve zajistím Lower values:
                for (int r = 0; r < rank; r++)
                {
                    if (next[r] < this.IndicesRange[r, 0]) next[r] = this.IndicesRange[r, 0];
                }

                // Nyní provedu Increment, v pořadí od posledního prvku pole:
                for (int r = (rank - 1); r >= 0; r--)
                {
                    next[r] = next[r] + 1;
                    // Pokud navýšený index nepřesáhl UpperBounds, je to OK a končíme. 
                    // Pokud jej přesáhl, ale je to na pozici 0, končíme taky; výsledek (next) je sice nevalidní, ale to je řešeno v testu IsValidIndices(), tam se vrátí false.
                    if (next[r] <= this.IndicesRange[r, 2] || r == 0) break;
                    // Na dané pozici dáme index = LowerBounds, a jdeme na další pozici (doleva):
                    next[r] = this.IndicesRange[r, 0];
                }

                return next;
            }
            /// <summary>
            /// Obsahuje pole indexů prvního prvku, typicky [0,0,0].
            /// </summary>
            private int[] _FirstIndices
            {
                get
                {
                    int rank = this.Rank;
                    if (rank <= 0) return null;
                    int[] indices = new int[rank];
                    for (int r = 0; r < rank; r++)
                        indices[r] = this.IndicesRange[r, 0];
                    return indices;
                }
            }
            #endregion
        }
        #endregion
        #region Komprimace a dekomprimace stringu
        /// <summary>
        /// Metoda vrátí daný string KOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený do Base64 stringu.
        /// Standardní serializovanou DataTable tato komprimace zmenší na cca 3-5% původní délky stringu.
        /// </summary>
        /// <param name="source">Vstupní string</param>
        /// <param name="splitToRows">Rozdělit výstupní komprimát na řádky (použije se <see cref="Base64FormattingOptions.InsertLineBreaks"/>).</param>
        /// <returns></returns>
        public static string Compress(string source, bool splitToRows = false)
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
                Base64FormattingOptions options = (splitToRows ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None);
                target = System.Convert.ToBase64String(outBuffer, options);
            }
            return target;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený z Base64 stringu.
        /// Pokud někde dojde k chybě, vrátí null ale ne chybu.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string TryDecompress(string source)
        {
            try
            {
                return Decompress(source);
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Metoda vrátí daný string DEKOMPRIMOVANÝ pomocí <see cref="System.IO.Compression.GZipStream"/>, převedený z Base64 stringu.
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
        #region class XmlPersistSaveArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při serializaci objektu
        /// <summary>
        /// XmlPersistSaveArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při serializaci objektu
        /// </summary>
        internal class XmlPersistSaveArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="data"></param>
            /// <param name="objectName"></param>
            /// <param name="estimatedType"></param>
            /// <param name="xmlElement"></param>
            /// <param name="typeLibrary"></param>
            internal XmlPersistSaveArgs(object data, string objectName, Type estimatedType, XmlElement xmlElement, TypeLibrary typeLibrary)
            {
                this.Data = data;
                this.ObjectName = objectName;
                this.EstimatedType = estimatedType;
                this.TargetName = null;
                this.XmlElement = xmlElement;
                this.DataTypeInfo = (data == null ? null : typeLibrary.GetInfo(data.GetType()));
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="data"></param>
            /// <param name="objectName"></param>
            /// <param name="estimatedType"></param>
            /// <param name="targetName"></param>
            /// <param name="xmlElement"></param>
            /// <param name="typeLibrary"></param>
            internal XmlPersistSaveArgs(object data, string objectName, Type estimatedType, string targetName, XmlElement xmlElement, TypeLibrary typeLibrary)
            {
                this.Data = data;
                this.ObjectName = objectName;
                this.EstimatedType = estimatedType;
                this.TargetName = targetName;
                this.XmlElement = xmlElement;
                this.DataTypeInfo = (data == null ? null : typeLibrary.GetInfo(data.GetType()));
            }
            /// <summary>
            /// Vlastní objekt s daty
            /// </summary>
            internal object Data { get; private set; }
            /// <summary>
            /// true pokud objekt není null
            /// </summary>
            internal bool HasData { get { return (this.Data != null); } }
            /// <summary>
            /// true pokud je zadané jméno <see cref="ObjectName"/>
            /// </summary>
            internal bool HasObjectName { get { return !String.IsNullOrEmpty(this.ObjectName); } }
            /// <summary>
            /// Název objektu
            /// </summary>
            internal string ObjectName { get; private set; }
            /// <summary>
            /// Očekávaný datový typ
            /// </summary>
            internal Type EstimatedType { get; private set; }
            internal string TargetName { get; private set; }
            internal string TypeAttributeName { get; private set; }
            /// <summary>
            /// XML element, do kterého se budou ukládat aktuální data
            /// </summary>
            internal XmlElement XmlElement { get; private set; }
            /// <summary>
            /// Datový typ
            /// </summary>
            internal Type DataType { get { return (this.DataTypeInfo == null ? null : this.DataTypeInfo.DataType); } }
            /// <summary>
            /// Informace o typu, který se právě zpracovává
            /// </summary>
            internal TypeLibrary.TypeInfo DataTypeInfo { get; set; }
            /// <summary>
            /// Informace o aktuální operaci, pro případ chyby - do textu chybové hlášky
            /// </summary>
            internal string CurrentOperation { get; set; }
            /// <summary>
            /// Informace o aktuální operaci, pro případ chyby - do textu chybové hlášky
            /// </summary>
            internal string CurrentProperty { get; set; }
        }
        #endregion
        #region class XmlPersistLoadArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při deserializaci objektu
        /// <summary>
        /// XmlPersistLoadArgs : sada parametrů předávaných mezi vnitřními metodami XmlPersist při deserializaci objektu
        /// </summary>
        internal class XmlPersistLoadArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="parameters"></param>
            /// <param name="propInfo"></param>
            /// <param name="dataType"></param>
            /// <param name="dataTypeInfo"></param>
            /// <param name="xmElement"></param>
            /// <param name="xmAttribute"></param>
            internal XmlPersistLoadArgs(PersistArgs parameters, TypeLibrary.PropInfo propInfo, Type dataType, TypeLibrary.TypeInfo dataTypeInfo, XmElement xmElement, XmAttribute xmAttribute)
            {
                this.Parameters = parameters;
                this.PropInfo = propInfo;
                this.DataType = dataType;
                this.DataTypeInfo = dataTypeInfo;
                this.XmElement = xmElement;
                this.XmAttribute = xmAttribute;
            }
            internal PersistArgs Parameters { get; private set; }
            internal TypeLibrary.PropInfo PropInfo { get; private set; }
            internal Type DataType { get; private set; }
            /// <summary>
            /// Údaje o datovém typu
            /// </summary>
            internal TypeLibrary.TypeInfo DataTypeInfo { get; set; }
            internal XmElement XmElement { get; private set; }
            internal XmAttribute XmAttribute { get; private set; }
            /// <summary>
            /// Informace o aktuální operaci, pro případ chyby - do textu chybové hlášky
            /// </summary>
            internal string CurrentOperation { get; set; }
            /// <summary>
            /// Informace o aktuální operaci, pro případ chyby - do textu chybové hlášky
            /// </summary>
            internal string CurrentProperty { get; set; }

        }
        #endregion
        #region enum XmlPersistenceType
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
        #region Výměnné algoritmy konkrétní verze persistence - dovolují načítat XML dokumenty autodetectem z různých verzí
        #region Algoritmy verze 1.0
        internal class XmlPersistV1 : XmlPersistVBase, IXmlPersistVersion
        {
            #region Konstruktor, číslo verze
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            internal XmlPersistV1(XmlPersist owner)
                : base(owner)
            { }
            /// <summary>
            /// Číslo verze
            /// </summary>
            public const string Version = "1.00";
            #endregion
            #region Řízení serializace/deserializace
            void IXmlPersistVersion.Save(object data, PersistArgs parameters, XmlDocument xDoc)
            {
                XmlElement xRootElement = xDoc.CreateElement(_DocumentNamePersist);
                xDoc.AppendChild(xRootElement);
                CreateAttribute("Version", Version, xRootElement);
                CreateAttribute("Created", Convertor.DateTimeToString(DateTime.Now), xRootElement);
                CreateAttribute("Creator", System.Windows.Forms.SystemInformation.UserName, xRootElement);

                XmlElement xDataElement = CreateElement(_DocumentNameData, xRootElement);
                XmlPersistSaveArgs saveArgs = new XmlPersistSaveArgs(data, _DocumentNameValue, null, xDataElement, this.TypeLibrary);
                this.SaveObject(saveArgs);
            }
            bool IXmlPersistVersion.IsValidFormat(XmDocument xmDoc)
            {
                string version;
                XmAttribute atValue;
                XmElement elValue;
                XmElement elData = this.SearchRootDataElement(xmDoc, out version, out atValue, out elValue);
                return (elData != null && version != null && version == Version);
            }
            object IXmlPersistVersion.Load(PersistArgs parameters, XmDocument xmDoc)
            {
                string version;
                XmAttribute atValue;
                XmElement elValue;
                XmElement elData = this.SearchRootDataElement(xmDoc, out version, out atValue, out elValue);
                if (!(elData != null && version != null && version == Version)) return null;

                XmlPersistLoadArgs itemArgs = CreateLoadArgs(parameters, null, null, elValue, atValue);
                return this.LoadObject(itemArgs);
            }
            /// <summary>
            /// Uloží předaný objekt do XML.
            /// Tato metoda nezakládá nový element, pokud vstupují data typu Simple.
            /// Ostatní typy dat řeší specializované metody, ty si svoje elementy zakládají.
            /// </summary>
            /// <param name="args">Kompletní datový balík pro uložení dat</param>
            private void SaveObject(XmlPersistSaveArgs args)
            {
                // do XML budu persistovat pouze not null hodnoty!
                if (!args.HasData) return;

                try
                {
                    // Zjistíme, jaký objekt to sem přišel:
                    args.DataTypeInfo = this.TypeLibrary.GetInfo(args.Data.GetType());

                    // Podle reálného typu, podle jeho charakteru předám řízení do odpovídající metody:
                    switch (args.DataTypeInfo.PersistenceType)
                    {
                        case TypeLibrary.XmlPersistenceType.Simple:
                            this.SimpleTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Self:
                            this.SelfTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Enum:
                            this.EnumTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Array:
                            this.ArrayTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.IList:
                            this.IListTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.IDictionary:
                            this.IDictionaryTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Compound:
                            this.CompoundTypeSave(args);
                            break;
                    }
                }
                catch (InvalidOperationException)
                {   // Tuhle výjimku jsme hodili sami v některém vnořeném cyklu, proto ji nebudu nijak řešit a pošlu ji dál...
                    throw;
                }
                catch (Exception exc)
                {   // Chyby při serializaci okomentujeme, aby developer věděl, odkud vítr fouká:
                    string type = args.DataTypeInfo.DataType.Namespace + "." + args.DataTypeInfo.DataType.Name;
                    string oper = (args.CurrentOperation != null ? args.CurrentOperation : "Null");
                    string prop = (args.CurrentProperty != null ? args.CurrentProperty : "Null");
                    string eol = Environment.NewLine;
                    throw new InvalidOperationException("Error on serialize of object." + eol +
                        "Type: " + type + ";" + eol +
                        "Operation: " + oper + ";" + eol +
                        "Property: " + prop + ";" + eol +
                        "Error: " + exc.Message,
                        exc);
                }
            }
            private XmElement SearchRootDataElement(XmDocument xmDoc, out string version, out XmAttribute atValue, out XmElement elValue)
            {
                version = null;
                atValue = null;
                elValue = null;

                // Najdu element "persistent", z něj lze načíst číslo verze:
                XmElement xmPers = xmDoc.FindElement(_DocumentNamePersist);
                if (xmPers == null) return null;

                version = xmPers.FindAttributeValue("Version", "");
                if (!String.Equals(version, Version)) return null;

                // Najdu element "data", v něm bude uložen objekt:
                XmElement elData = xmDoc.FindElement(_DocumentNamePersist + "/" + _DocumentNameData);
                if (elData == null) return null;

                // Element Data bude obsahovat buď atribut nebo sub element Value:
                if (!this._FindAttrElementByName(elData, _DocumentNameValue, true, out elValue, out atValue)) return null;

                return elData;
            }
            /// <summary>
            /// Vytvoří objekt na základě dat v aktuálním elementu readeru.
            /// Na vstupu je předán reader, defaultní typ prvku, jméno nepovinného atributu, který nese explicitní typ, a jméno atributu který nese hodnotu (platí pouze pro Simple typy).
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object LoadObject(XmlPersistLoadArgs args)
            {
                if (args.DataType == null) return null;
                try
                {
                    switch (args.DataTypeInfo.PersistenceType)
                    {
                        case TypeLibrary.XmlPersistenceType.Simple:
                            return this.SimpleTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Self:
                            return this.SelfTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Enum:
                            return this.EnumTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Array:
                            return this.ArrayTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.IList:
                            return this.IListTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.IDictionary:
                            return this.IDictionaryTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Compound:
                            return this.CompoundTypeLoad(args);
                    }
                }
                catch (InvalidOperationException)
                {   // Tuhle výjimku jsme hodili sami v některém vnořeném cyklu, proto ji nebudu nijak řešit a pošlu ji dál...
                    throw;
                }
                catch (Exception exc)
                {   // Chyby při deserializaci okomentujeme, aby developer věděl, odkud vítr fouká:
                    string type = args.DataTypeInfo.DataType.Namespace + "." + args.DataTypeInfo.DataType.Name;
                    string oper = (args.CurrentOperation != null ? args.CurrentOperation : "Null");
                    string prop = (args.CurrentProperty != null ? args.CurrentProperty : "Null");
                    string eol = Environment.NewLine;
                    throw new InvalidOperationException("Error on deserialize of object." + eol +
                        "Type: " + type + ";" + eol +
                        "Operation: " + oper + ";" + eol +
                        "Property: " + prop + ";" + eol +
                        "Error: " + exc.Message,
                        exc);
                }
                return null;
            }
            #endregion
            #region Simple typy: Save, Create
            /// <summary>
            /// Daný objekt (data) konvertuje pomocí TypeConvertoru pro Simple typ, a výslednou hodnotu vloží do atributu daného jména.
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void SimpleTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "NotifyData.Begin";
                args.CurrentProperty = null;
                NotifyData(args.Data, XmlPersistState.SaveBegin);

                args.CurrentOperation = "SerializeValue";
                if (args.DataTypeInfo.TypeConvert == null)
                    throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() neobsahuje TypeConvertor.");
                if (args.DataTypeInfo.TypeConvert.Serializator == null)
                    throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() obsahuje TypeConvertor, který nemá serializátor.");
                string value = args.DataTypeInfo.TypeConvert.Serializator(args.Data);

                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args);

                args.CurrentOperation = "SaveValues";
                args.CurrentProperty = "Value";
                CreateAttribute(args.ObjectName, value, args.XmlElement);
                args.CurrentProperty = null;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(args.Data, XmlPersistState.SaveDone);
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Simple (tj. je uložený v jednom atributu jako jeden string)
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private object SimpleTypeLoad(XmlPersistLoadArgs args)
            {
                args.CurrentOperation = "DeserializeValue";
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
            /// <param name="args"></param>
            private void SelfTypeSave(XmlPersistSaveArgs args)
            {
                if (args.HasData)
                {
                    args.CurrentOperation = "NotifyData.Begin";
                    args.CurrentProperty = null;
                    NotifyData(args.Data, XmlPersistState.SaveBegin);

                    args.CurrentOperation = "SerializeValue";
                    if (!(args.Data is IXmlSerializer))
                        throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SelfTypeSave() neimplementuje interface IXmlSerializer.");
                    string value = ((IXmlSerializer)args.Data).XmlSerialData;

                    args.CurrentOperation = "SaveType";
                    SaveTypeAttribute(args);

                    args.CurrentOperation = "SaveValues";
                    args.CurrentProperty = "SelfValue";
                    CreateAttribute(args.ObjectName, value, args.XmlElement);
                    args.CurrentProperty = null;

                    args.CurrentOperation = "NotifyData.End";
                    NotifyData(args.Data, XmlPersistState.SaveDone);
                }
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Self (tj. deserializaci provádí on sám osobně)
            /// </summary>
            /// <returns></returns>
            private object SelfTypeLoad(XmlPersistLoadArgs args)
            {
                string xmlData = args.XmAttribute.ValueFirstOrDefault;
                if (xmlData == null)
                    return null;

                args.CurrentOperation = "CreateInstance";
                object data = _ObjectCreate(args.DataType);
                if (data == null)
                    return null;

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "Deserialize";
                ((IXmlSerializer)data).XmlSerialData = xmlData;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return data;
            }
            #endregion
            #region Enum typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Self (tj. serializaci provádí on sám osobně)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void EnumTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "SerializeValue";
                string value = Enum.Format(args.DataType, args.Data, "F");

                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args);

                args.CurrentOperation = "SaveValues";
                args.CurrentProperty = "EnumValue";
                CreateAttribute(args.ObjectName, value, args.XmlElement);
                args.CurrentProperty = null;
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Enum
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object EnumTypeLoad(XmlPersistLoadArgs args)
            {
                string xmlData = args.XmAttribute.ValueFirstOrDefault;
                if (xmlData == null)
                    return null;

                object data = null;
                try
                {
                    args.CurrentOperation = "ParseEnum";
                    data = Enum.Parse(args.DataType, xmlData);
                }
                catch
                {
                    args.CurrentOperation = "DefaultEnum";
                    data = _ObjectCreate(args.DataType);                  // Vytvoří objekt, prázdný (hodnota = 0)
                }
                return data;
            }
            #endregion
            #region Array typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Array
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void ArrayTypeSave(XmlPersistSaveArgs args)
            {
                // Zavedu element za celý List,například <Array ...>...</Array>:
                XmlElement xmlArrayElement = CreateElement(args.ObjectName, args.XmlElement);

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlArrayElement);
                Array array = args.Data as Array;
                Type itemType = args.DataTypeInfo.ItemDataType;

                // Správce indexů pole:
                args.CurrentOperation = "CreateIndices";
                ArrayIndices arrayIndices = new ArrayIndices(array);

                // Rozměry pole:  [0+2,0+45] :
                CreateAttribute(_ArrayNameArrayRange, arrayIndices.Serial, xmlArrayElement);

                // Výpis všech Items:
                args.CurrentOperation = "EnumerateArray";
                // string itemName = _ArrayNameItem;
                for (int[] indices = arrayIndices.FirstIndices(); arrayIndices.IsValidIndices(indices); indices = arrayIndices.NextIndices(indices))
                {
                    // Data:
                    object item = array.GetValue(indices);
                    if (item != null)
                    {   // Pokud data jsou null, můžu je vynechat, protože to je default stav pole Array po jeho vytvoření. Ukládám tedy jen nut null hodnoty:
                        args.CurrentOperation = "SaveValue";
                        args.CurrentProperty = "Item" + ArrayIndices.ToString(indices);

                        XmlElement xmlItemElement = CreateElement(_ArrayNameItem, xmlArrayElement);
                        // Vypíšu indices:
                        CreateAttribute(_ArrayNameItemIndices, ArrayIndices.SerialIndices(indices), xmlItemElement);

                        // Vypíšu obsah itemu:
                        XmlPersistSaveArgs nextArgs = new XmlPersistSaveArgs(item, _ElementNameValue, itemType, xmlItemElement, this.TypeLibrary);
                        this.SaveObject(nextArgs);
                    }
                }
                args.CurrentProperty = null;
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Array
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object ArrayTypeLoad(XmlPersistLoadArgs args)
            {
                XmAttribute xmAtt;
                if (!args.XmElement.TryGetAttribute(_ArrayName, out xmAtt)) return null;

                args.CurrentOperation = "ParseIndices";
                ArrayIndices arrayIndices = new ArrayIndices(xmAtt.Range);
                Type itemType = args.DataTypeInfo.ItemDataType;
                args.CurrentOperation = "CreateArray";
                Array array = arrayIndices.CreateArray(itemType);
                if (array == null) return null;

                args.CurrentOperation = "EnumerateXmlElements";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _ArrayNameItem)
                    {   // Element má odpovídající jméno. 
                        XmAttribute itemAttribute;
                        if (xmEle.TryGetAttribute(_ArrayNameItem, out itemAttribute) && itemAttribute.Indices != null)
                        {   // V elementu jsme našli atribut: Item.Indices="[0,5,20]" nesoucí indexy aktuálního prvku:
                            int[] indices = ArrayIndices.DeSerialIndices(itemAttribute.Indices);
                            if (indices.Length == arrayIndices.Rank)
                            {   // Deserializované indexy mají správný počet položek pro naše pole:
                                args.CurrentOperation = "ReadValue";
                                args.CurrentProperty = "Item" + ArrayIndices.ToString(indices);

                                object value = null;
                                // Element xmEle: buď obsahuje atribut, nebo podřízený element s názvem "Value":
                                XmAttribute atValue;
                                XmElement elValue;
                                if (_FindAttrElementByName(xmEle, _ElementNameValue, false, out elValue, out atValue))
                                {
                                    args.CurrentOperation = "CreateItem";
                                    XmlPersistLoadArgs nextArgs = this.CreateLoadArgs(args.Parameters, null, itemType, elValue, atValue);
                                    value = this.LoadObject(nextArgs);
                                }
                                array.SetValue(value, indices);
                            }
                        }
                    }
                }

                return array;
            }
            #endregion
            #region IList typy: Save, Create
            /// <summary>
            /// Uloží kolekci (List)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void IListTypeSave(XmlPersistSaveArgs args)
            {
                // Zavedu element za celý List,například <ItemList ...>...</ItemList>:
                XmlElement xmlListElement = CreateElement(args.ObjectName, args.XmlElement);

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlListElement);

                IList iList = args.Data as IList;
                if (iList == null) return;

                args.CurrentOperation = "Map.IList";
                Type itemType = args.DataTypeInfo.GetGenericType(0);

                args.CurrentOperation = "Enumerate.IList";
                int index = 0;
                foreach (object item in iList)
                {
                    args.CurrentOperation = "SaveValue";
                    args.CurrentProperty = "Item[" + (index++).ToString() + "]";

                    // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek (tj. ani null položky nelze vynechávat):
                    XmlElement xmlItemElement = CreateElement(_ArrayNameItem, xmlListElement);

                    // Obsah tohoto prvku vepíšeme, jen když obsah prvku pole není null:
                    if (item != null)
                    {
                        XmlPersistSaveArgs nextArgs = new XmlPersistSaveArgs(item, _ElementNameValue, itemType, xmlItemElement, this.TypeLibrary);
                        this.SaveObject(nextArgs);
                    }
                }
            }
            /// <summary>
            /// Vytvoří a vrátí datový objekt
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object IListTypeLoad(XmlPersistLoadArgs args)
            {
                // Vytvořím objekt s daty odpovídajícími datům persistovaným:
                args.CurrentOperation = "Create.IList";
                object data = _ObjectCreate(args.DataType);
                IList iList = data as IList;
                if (iList == null) return null;

                args.CurrentOperation = "GetGenerics";
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());           // Reálný Type + jeho property
                Type itemType = args.DataTypeInfo.ItemDataType;
                if (itemType == null)
                    itemType = args.DataTypeInfo.GetGenericType(0);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "EnumerateItems";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _ArrayNameItem)
                    {   // Element má odpovídající jméno. 
                        object value = null;
                        // Buď obsahuje atribut, nebo podřízený element s názvem Value:
                        XmAttribute atValue;
                        XmElement elValue;
                        if (_FindAttrElementByName(xmEle, _ElementNameValue, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateItem";
                            XmlPersistLoadArgs itemArgs = this.CreateLoadArgs(args.Parameters, null, itemType, elValue, atValue);
                            value = this.LoadObject(itemArgs);
                        }
                        iList.Add(value);
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return data;
            }
            #endregion
            #region IDictionary typy: Save, Create
            /// <summary>
            /// Uloží Dictionary
            /// </summary>
            private void IDictionaryTypeSave(XmlPersistSaveArgs args)
            {
                // string currentName, string itemName, TypeInfo typeInfo, object data, Type estimatedType, string typeAttributeName, XmlElement xElement)
                XmlElement xmlDictElement = CreateElement(args.ObjectName, args.XmlElement);  // Zavedu element za celý List,například <ItemList ...>...</ItemList>

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlDictElement);

                IDictionary iDict = args.Data as IDictionary;
                if (iDict == null) return;

                args.CurrentOperation = "Map.IDictionary";
                Type keyType = args.DataTypeInfo.GetGenericType(0);
                Type valueType = args.DataTypeInfo.GetGenericType(1);

                args.CurrentOperation = "Enumerate.IDictionary";
                foreach (DictionaryEntry entry in iDict)
                {
                    // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek (tj. ani null položky nelze vynechávat):
                    XmlElement xmlItemElement = CreateElement(_DictNamePair, xmlDictElement);

                    this.SaveObject(new XmlPersistSaveArgs(entry.Key, _DictNameKey, keyType, xmlItemElement, this.TypeLibrary));
                    this.SaveObject(new XmlPersistSaveArgs(entry.Value, _DictNameValue, valueType, xmlItemElement, this.TypeLibrary));
                }
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Dictionary
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object IDictionaryTypeLoad(XmlPersistLoadArgs args)
            {
                args.CurrentOperation = "Create.IDictionary";
                object data = _ObjectCreate(args.DataType);
                IDictionary iDict = data as IDictionary;
                if (iDict == null) return null;

                args.CurrentOperation = "GetGenerics";
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());           // Reálný Type + jeho property
                Type keyType = args.DataTypeInfo.GetGenericType(0);
                Type valueType = args.DataTypeInfo.GetGenericType(1);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "EnumerateItems";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _DictNamePair)
                    {   // Element má odpovídající jméno. 
                        object key = null;
                        object value = null;
                        // Buď obsahuje atribut, nebo podřízený element s názvem Value:
                        XmAttribute atValue;
                        XmElement elValue;
                        if (_FindAttrElementByName(xmEle, _DictNameKey, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateKey";
                            XmlPersistLoadArgs itemArgs = this.CreateLoadArgs(args.Parameters, null, keyType, elValue, atValue);
                            key = this.LoadObject(itemArgs);
                        }
                        if (_FindAttrElementByName(xmEle, _DictNameValue, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateValue";
                            XmlPersistLoadArgs itemArgs = this.CreateLoadArgs(args.Parameters, null, valueType, elValue, atValue);
                            value = this.LoadObject(itemArgs);
                        }
                        if (key != null)
                        {
                            args.CurrentOperation = "AddItem.IDictionary";
                            iDict.Add(key, value);
                        }
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return iDict;
            }
            #endregion
            #region Compound typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Compound (tj. má property)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void CompoundTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "NotifyData.Begin";
                args.CurrentProperty = null;
                NotifyData(args.Data, XmlPersistState.SaveBegin);

                args.CurrentOperation = "SaveType";
                XmlElement xmlCurrElement = CreateElement(args.ObjectName, args.XmlElement);
                SaveTypeAttribute(args, args.ObjectName, xmlCurrElement);

                args.CurrentOperation = "SaveValues";
                foreach (TypeLibrary.PropInfo propInfo in args.DataTypeInfo.Properties)
                {
                    args.CurrentProperty = propInfo.Name;
                    object value = propInfo.Property.GetValue(args.Data, null);
                    this.SaveObject(new XmlPersistSaveArgs(value, propInfo.XmlName, propInfo.PropertyType, propInfo.XmlName, xmlCurrElement, this.TypeLibrary));
                }
                args.CurrentProperty = null;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(args.Data, XmlPersistState.SaveDone);
            }
            /// <summary>
            /// Vytvoří a vrátí objekt typu Compound z dat XML
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private object CompoundTypeLoad(XmlPersistLoadArgs args)
            {
                // Vytvořím objekt s daty odpovídajícími datům persistovaným:
                args.CurrentOperation = "Create.Compound";
                object data = _ObjectCreate(args.DataType);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());             // Reálný Type + jeho property

                // 1. Projdeme atributy, ty obsahují jednoduché datové typy. Uložíme je do property našeho objektu:
                args.CurrentOperation = "ReadAttributes";
                foreach (XmAttribute xmAtt in args.XmElement.XmAttributes)
                {
                    TypeLibrary.PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(xmAtt.Name);
                    if (propInfo != null)
                    {
                        args.CurrentProperty = propInfo.Name;
                        XmlPersistLoadArgs propArgs = this.CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, args.XmElement, xmAtt);
                        object value = this.LoadObject(propArgs);
                        propInfo.Property.SetValue(data, value, null);
                    }
                }

                // 2. Projdeme si sub-elementy našeho elementu. pro každý z nich určím zda máme cílovou property, a pak načtu hodnotu:
                args.CurrentOperation = "ReadElements";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    TypeLibrary.PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(xmEle.Name);
                    if (propInfo != null)
                    {
                        args.CurrentProperty = propInfo.Name;
                        XmAttribute xmAte;
                        xmEle.TryGetAttribute(xmEle.Name, out xmAte);            // Pokud je v elementu uložen obraz objektu, který je jiného typu než je očekáván v property, pak je zde uložen i konkrétní Type
                        XmlPersistLoadArgs propArgs = this.CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, xmEle, xmAte);
                        object value = this.LoadObject(propArgs);
                        propInfo.Property.SetValue(data, value, null);
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);
                return data;
            }
            #endregion
            #region Konstanty
            private const string _DocumentNamePersist = "persistent";
            private const string _DocumentNameData = "data";
            private const string _DocumentNameValue = "Value";
            private const string _ElementNameValue = "Value";
            private const string _ArrayName = "Array";
            private const string _ArrayNameItem = "Item";
            private const string _DictNamePair = "Entry";
            private const string _DictNameKey = "Key";
            private const string _DictNameValue = "Value";
            private const string _ArrayNameArrayRange = _ArrayName + "." + XmAttribute.SuffixNameRange;
            private const string _ArrayNameItemIndices = _ArrayNameItem + "." + XmAttribute.SuffixNameIndices;
            #endregion
        }
        #endregion
        #region Algoritmy verze 2.0
        internal class XmlPersistV2 : XmlPersistVBase, IXmlPersistVersion
        {
            #region Konstruktor, číslo verze
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            internal XmlPersistV2(XmlPersist owner) 
                : base(owner)
            { }
            /// <summary>
            /// Číslo verze
            /// </summary>
            public const string Version = "2.00";
            #endregion
            #region Řízení serializace/deserializace
            void IXmlPersistVersion.Save(object data, PersistArgs parameters, XmlDocument xDoc)
            {
                this.SaveBegin(parameters);
                parameters.SavePrimitivesCount = 0;
                this.Parameters = parameters;

                XmlElement xRootElement = xDoc.CreateElement(_DocumentNamePersist);
                xDoc.AppendChild(xRootElement);
                CreateAttribute("Version", Version, xRootElement);
                CreateAttribute("Created", Convertor.DateTimeToString(DateTime.Now), xRootElement);
                CreateAttribute("Creator", System.Windows.Forms.SystemInformation.UserName, xRootElement);

                XmlElement xDataElement = CreateElement(_DocumentNameData, xRootElement);
                this.SaveObject(new XmlPersistSaveArgs(data, _DocumentNameValue, null, xDataElement, this.TypeLibrary));

                this.Parameters = null;
            }
            bool IXmlPersistVersion.IsValidFormat(XmDocument xmDoc)
            {
                string version;
                XmAttribute atValue;
                XmElement elValue;
                XmElement elData = this.SearchRootDataElement(xmDoc, out version, out atValue, out elValue);
                return (elData != null && version != null && version == Version);
            }
            object IXmlPersistVersion.Load(PersistArgs parameters, XmDocument xmDoc)
            {
                string version;
                XmAttribute atValue;
                XmElement elValue;
                XmElement elData = this.SearchRootDataElement(xmDoc, out version, out atValue, out elValue);
                if (!(elData != null && version != null && version == Version)) return null;

                XmlPersistLoadArgs itemArgs = CreateLoadArgs(parameters, null, null, elValue, atValue);
                return this.LoadObject(itemArgs);
            }
            /// <summary>
            /// Uloží předaný objekt do XML.
            /// Tato metoda nezakládá nový element, pokud vstupují data typu Simple.
            /// Ostatní typy dat řeší specializované metody, ty si svoje elementy zakládají.
            /// </summary>
            /// <param name="args">Kompletní datový balík pro uložení dat</param>
            private void SaveObject(XmlPersistSaveArgs args)
            {
                // do XML budu persistovat pouze not null hodnoty!
                if (!args.HasData) return;

                try
                {
                    // Zjistíme, jaký objekt to sem přišel:
                    args.DataTypeInfo = this.TypeLibrary.GetInfo(args.Data.GetType());

                    // Podle reálného typu, podle jeho charakteru předám řízení do odpovídající metody:
                    switch (args.DataTypeInfo.PersistenceType)
                    {
                        case TypeLibrary.XmlPersistenceType.Simple:
                            this.SimpleTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Self:
                            this.SelfTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Enum:
                            this.EnumTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Array:
                            this.ArrayTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.IList:
                            this.IListTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.IDictionary:
                            this.IDictionaryTypeSave(args);
                            break;
                        case TypeLibrary.XmlPersistenceType.Compound:
                            this.CompoundTypeSave(args);
                            break;
                    }
                }
                catch (InvalidOperationException)
                {   // Tuhle výjimku jsme hodili sami v některém vnořeném cyklu, proto ji nebudu nijak řešit a pošlu ji dál...
                    throw;
                }
                catch (Exception exc)
                {   // Chyby při serializaci okomentujeme, aby developer věděl, odkud vítr fouká:
                    string type = args.DataTypeInfo.DataType.Namespace + "." + args.DataTypeInfo.DataType.Name;
                    string oper = (args.CurrentOperation != null ? args.CurrentOperation : "Null");
                    string prop = (args.CurrentProperty != null ? args.CurrentProperty : "Null");
                    string eol = Environment.NewLine;
                    throw new InvalidOperationException("Error on serialize of object." + eol +
                        "Type: " + type + ";" + eol +
                        "Operation: " + oper + ";" + eol +
                        "Property: " + prop + ";" + eol +
                        "Error: " + exc.Message,
                        exc);
                }
            }
            private XmElement SearchRootDataElement(XmDocument xmDoc, out string version, out XmAttribute atValue, out XmElement elValue)
            {
                version = null;
                atValue = null;
                elValue = null;

                // Najdu element "persistent", z něj lze načíst číslo verze:
                XmElement xmPers = xmDoc.FindElement(_DocumentNamePersist);
                if (xmPers == null) return null;

                version = xmPers.FindAttributeValue("Version", "");
                if (!String.Equals(version, Version)) return null;

                // Najdu element "data", v něm bude uložen objekt:
                XmElement elData = xmDoc.FindElement(_DocumentNamePersist + "/" + _DocumentNameData);
                if (elData == null) return null;

                // Element Data bude obsahovat buď atribut nebo sub element Value:
                if (!this._FindAttrElementByName(elData, _DocumentNameValue, true, out elValue, out atValue)) return null;

                return elData;
            }
            /// <summary>
            /// Vytvoří objekt na základě dat v aktuálním elementu readeru.
            /// Na vstupu je předán reader, defaultní typ prvku, jméno nepovinného atributu, který nese explicitní typ, a jméno atributu který nese hodnotu (platí pouze pro Simple typy).
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object LoadObject(XmlPersistLoadArgs args)
            {
                if (args.DataType == null) return null;
                try
                {
                    switch (args.DataTypeInfo.PersistenceType)
                    {
                        case TypeLibrary.XmlPersistenceType.Simple:
                            return this.SimpleTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Self:
                            return this.SelfTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Enum:
                            return this.EnumTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Array:
                            return this.ArrayTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.IList:
                            return this.IListTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.IDictionary:
                            return this.IDictionaryTypeLoad(args);
                        case TypeLibrary.XmlPersistenceType.Compound:
                            return this.CompoundTypeLoad(args);
                    }
                }
                catch (InvalidOperationException)
                {   // Tuhle výjimku jsme hodili sami v některém vnořeném cyklu, proto ji nebudu nijak řešit a pošlu ji dál...
                    throw;
                }
                catch (Exception exc)
                {   // Chyby při deserializaci okomentujeme, aby developer věděl, odkud vítr fouká:
                    string type = args.DataTypeInfo.DataType.Namespace + "." + args.DataTypeInfo.DataType.Name;
                    string oper = (args.CurrentOperation != null ? args.CurrentOperation : "Null");
                    string prop = (args.CurrentProperty != null ? args.CurrentProperty : "Null");
                    string eol = Environment.NewLine;
                    throw new InvalidOperationException("Error on deserialize of object." + eol +
                        "Type: " + type + ";" + eol +
                        "Operation: " + oper + ";" + eol +
                        "Property: " + prop + ";" + eol +
                        "Error: " + exc.Message,
                        exc);
                }
                return null;
            }
            #endregion
            #region Simple typy: Save, Create
            /// <summary>
            /// Daný objekt (data) konvertuje pomocí TypeConvertoru pro Simple typ, a výslednou hodnotu vloží do atributu daného jména.
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void SimpleTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "NotifyData.Begin";
                args.CurrentProperty = null;
                NotifyData(args.Data, XmlPersistState.SaveBegin);

                args.CurrentOperation = "SerializeValue";
                if (args.DataTypeInfo.TypeConvert == null)
                    throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() neobsahuje TypeConvertor.");
                if (args.DataTypeInfo.TypeConvert.Serializator == null)
                    throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SimpleTypeSave() obsahuje TypeConvertor, který nemá serializátor.");
                string value = args.DataTypeInfo.TypeConvert.Serializator(args.Data);

                this.SavePrimitive(value);

                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args);

                args.CurrentOperation = "SaveValues";
                args.CurrentProperty = "Value";
                CreateAttribute(args.ObjectName, value, args.XmlElement);
                args.CurrentProperty = null;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(args.Data, XmlPersistState.SaveDone);
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Simple (tj. je uložený v jednom atributu jako jeden string)
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private object SimpleTypeLoad(XmlPersistLoadArgs args)
            {
                args.CurrentOperation = "DeserializeValue";
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
            /// <param name="args"></param>
            private void SelfTypeSave(XmlPersistSaveArgs args)
            {
                if (args.HasData)
                {
                    args.CurrentOperation = "NotifyData.Begin";
                    args.CurrentProperty = null;
                    NotifyData(args.Data, XmlPersistState.SaveBegin);

                    args.CurrentOperation = "SerializeValue";
                    if (!(args.Data is IXmlSerializer))
                        throw new InvalidOperationException("Nelze serializovat, typ předaný do metody SelfTypeSave() neimplementuje interface IXmlSerializer.");
                    string value = ((IXmlSerializer)args.Data).XmlSerialData;

                    this.SavePrimitive(value);

                    args.CurrentOperation = "SaveType";
                    SaveTypeAttribute(args);

                    args.CurrentOperation = "SaveValues";
                    args.CurrentProperty = "SelfValue";
                    CreateAttribute(args.ObjectName, value, args.XmlElement);
                    args.CurrentProperty = null;

                    args.CurrentOperation = "NotifyData.End";
                    NotifyData(args.Data, XmlPersistState.SaveDone);
                }
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Self (tj. deserializaci provádí on sám osobně)
            /// </summary>
            /// <returns></returns>
            private object SelfTypeLoad(XmlPersistLoadArgs args)
            {
                string xmlData = args.XmAttribute.ValueFirstOrDefault;
                if (xmlData == null)
                    return null;

                args.CurrentOperation = "CreateInstance";
                object data = this._ObjectCreate(args.DataType);
                if (data == null)
                    return null;

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "Deserialize";
                ((IXmlSerializer)data).XmlSerialData = xmlData;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return data;
            }
            #endregion
            #region Enum typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Self (tj. serializaci provádí on sám osobně)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void EnumTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "SerializeValue";
                string value = Enum.Format(args.DataType, args.Data, "F");

                this.SavePrimitive(value);

                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args);

                args.CurrentOperation = "SaveValues";
                args.CurrentProperty = "EnumValue";
                CreateAttribute(args.ObjectName, value, args.XmlElement);
                args.CurrentProperty = null;
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Enum
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object EnumTypeLoad(XmlPersistLoadArgs args)
            {
                string xmlData = args.XmAttribute.ValueFirstOrDefault;
                if (xmlData == null)
                    return null;

                object data = null;
                try
                {
                    args.CurrentOperation = "ParseEnum";
                    data = Enum.Parse(args.DataType, xmlData);
                }
                catch
                {
                    args.CurrentOperation = "DefaultEnum";
                    data = _ObjectCreate(args.DataType);                  // Vytvoří objekt, prázdný (hodnota = 0)
                }
                return data;
            }
            #endregion
            #region Array typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Array
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void ArrayTypeSave(XmlPersistSaveArgs args)
            {
                // Zavedu element pro celé pole Array, pod jménem cíle, kam toto pole je vloženo (nahoru):
                XmlElement xmlArrayElement = CreateElement(args.ObjectName, args.XmlElement);

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlArrayElement);
                Array array = args.Data as Array;
                if (array == null) return;

                Type itemType = args.DataTypeInfo.ItemDataType;

                // Správce indexů pole:
                args.CurrentOperation = "CreateIndices";
                ArrayIndices arrayIndices = new ArrayIndices(array);

                // Rozměry pole:  [0+2,0+45] :
                CreateAttribute(_ArrayNameArrayRange, arrayIndices.Serial, xmlArrayElement);

                // Výpis všech Items:
                args.CurrentOperation = "EnumerateArray";
                for (int[] indices = arrayIndices.FirstIndices(); arrayIndices.IsValidIndices(indices); indices = arrayIndices.NextIndices(indices))
                {
                    // Data:
                    object item = array.GetValue(indices);
                    if (item != null)
                    {   // Pokud data jsou null, můžu je vynechat, protože to je default stav pole Array po jeho vytvoření. Ukládám tedy jen nut null hodnoty:
                        args.CurrentOperation = "SaveValue";
                        args.CurrentProperty = "Item" + ArrayIndices.ToString(indices);

                        // Vytvořím element pro prvek pole, a vepíšu do něj atribut obsahující indexy:
                        XmlElement xmlItemElement = CreateElement(_ArrayNameItem, xmlArrayElement);
                        CreateAttribute(_ArrayNameItemIndices, ArrayIndices.SerialIndices(indices), xmlItemElement);

                        // Do XML elementu pro prvek pole vložím hodnoty z objektu:
                        XmlPersistSaveArgs nextArgs = new XmlPersistSaveArgs(item, _ElementNameValue, itemType, xmlItemElement, this.TypeLibrary);
                        this.SaveObject(nextArgs);
                    }
                }
                args.CurrentProperty = null;
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Array
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object ArrayTypeLoad(XmlPersistLoadArgs args)
            {
                XmAttribute xmAtt;
                if (!args.XmElement.TryGetAttribute(_ArrayName, out xmAtt)) return null;

                args.CurrentOperation = "ParseIndices";
                ArrayIndices arrayIndices = new ArrayIndices(xmAtt.Range);
                Type itemType = args.DataTypeInfo.ItemDataType;
                args.CurrentOperation = "CreateArray";
                Array array = arrayIndices.CreateArray(itemType);
                if (array == null) return null;

                args.CurrentOperation = "EnumerateXmlElements";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _ArrayNameItem)
                    {   // Element má odpovídající jméno. 
                        XmAttribute itemAttribute;
                        if (xmEle.TryGetAttribute(_ArrayNameItem, out itemAttribute) && itemAttribute.Indices != null)
                        {   // V elementu jsme našli atribut: Item.Indices="[0,5,20]" nesoucí indexy aktuálního prvku:
                            int[] indices = ArrayIndices.DeSerialIndices(itemAttribute.Indices);
                            if (indices.Length == arrayIndices.Rank)
                            {   // Deserializované indexy mají správný počet položek pro naše pole:
                                args.CurrentOperation = "ReadValue";
                                args.CurrentProperty = "Item" + ArrayIndices.ToString(indices);

                                object value = null;
                                // Element xmEle: buď obsahuje atribut, nebo podřízený element s názvem "Value":
                                XmAttribute atValue;
                                XmElement elValue;
                                if (_FindAttrElementByName(xmEle, _ElementNameValue, false, out elValue, out atValue))
                                {
                                    args.CurrentOperation = "CreateItem";
                                    XmlPersistLoadArgs nextArgs = this.CreateLoadArgs(args.Parameters, null, itemType, elValue, atValue);
                                    value = this.LoadObject(nextArgs);
                                }
                                array.SetValue(value, indices);
                            }
                        }
                    }
                }

                return array;
            }
            #endregion
            #region IList typy: Save, Create
            /// <summary>
            /// Uloží kolekci (List)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void IListTypeSave(XmlPersistSaveArgs args)
            {
                // Zavedu element pro celý List, pod jménem cíle, kam tento List pole je vložen (nahoru):
                XmlElement xmlListElement = CreateElement(args.ObjectName, args.XmlElement);

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlListElement);

                IList iList = args.Data as IList;
                if (iList == null) return;

                args.CurrentOperation = "Map.IList";
                Type itemType = args.DataTypeInfo.GetGenericType(0);

                args.CurrentOperation = "Enumerate.IList";
                int index = 0;
                foreach (object item in iList)
                {
                    args.CurrentOperation = "SaveValue";
                    args.CurrentProperty = "Item[" + (index++).ToString() + "]";

                    // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek (tj. ani null položky nelze vynechávat):
                    XmlElement xmlItemElement = CreateElement(_ArrayNameItem, xmlListElement);

                    // Obsah tohoto prvku vepíšeme, jen když obsah prvku pole není null:
                    if (item != null)
                    {
                        XmlPersistSaveArgs nextArgs = new XmlPersistSaveArgs(item, _ElementNameValue, itemType, xmlItemElement, this.TypeLibrary);
                        this.SaveObject(nextArgs);
                    }
                }
            }
            /// <summary>
            /// Vytvoří a vrátí datový objekt
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object IListTypeLoad(XmlPersistLoadArgs args)
            {
                // Vytvořím objekt s daty odpovídajícími datům persistovaným:
                args.CurrentOperation = "Create.IList";
                object data = _ObjectCreate(args.DataType);
                IList iList = data as IList;
                if (iList == null) return null;

                args.CurrentOperation = "GetGenerics";
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());           // Reálný Type + jeho property
                Type itemType = args.DataTypeInfo.ItemDataType;
                if (itemType == null)
                    itemType = args.DataTypeInfo.GetGenericType(0);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "EnumerateItems";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _ArrayNameItem)
                    {   // Element má odpovídající jméno. 
                        object value = null;
                        // Buď obsahuje atribut, nebo podřízený element s názvem Value:
                        XmAttribute atValue;
                        XmElement elValue;
                        if (_FindAttrElementByName(xmEle, _ElementNameValue, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateItem";
                            XmlPersistLoadArgs nextArgs = this.CreateLoadArgs(args.Parameters, null, itemType, elValue, atValue);
                            value = this.LoadObject(nextArgs);
                        }
                        iList.Add(value);
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return data;
            }
            #endregion
            #region IDictionary typy: Save, Create
            /// <summary>
            /// Uloží Dictionary
            /// </summary>
            private void IDictionaryTypeSave(XmlPersistSaveArgs args)
            {
                // Zavedu element pro celý Dictionary, pod jménem cíle, kam tento List pole je vložen (nahoru):
                XmlElement xmlDictElement = CreateElement(args.ObjectName, args.XmlElement);

                // Pokud se skutečný Type objektu s daty liší od očekávaného typu, vepíšu jeho Type jako atribut:
                args.CurrentOperation = "SaveType";
                SaveTypeAttribute(args, args.ObjectName, xmlDictElement);

                IDictionary iDict = args.Data as IDictionary;
                if (iDict == null) return;

                args.CurrentOperation = "Map.IDictionary";
                Type keyType = args.DataTypeInfo.GetGenericType(0);
                Type valueType = args.DataTypeInfo.GetGenericType(1);

                args.CurrentOperation = "Enumerate.IDictionary";
                foreach (DictionaryEntry entry in iDict)
                {
                    args.CurrentOperation = "SaveValue";
                    args.CurrentProperty = "Item[" + entry.Key.ToString() + "]";        // Key v Dictionary nemůže být null !!!

                    // Element za tento prvek seznamu založíme, protože je třeba zachovávat pořadí položek (tj. ani null položky nelze vynechávat):
                    XmlElement xmlItemElement = CreateElement(_DictNamePair, xmlDictElement);

                    this.SaveObject(new XmlPersistSaveArgs(entry.Key, _DictNameKey, keyType, xmlItemElement, this.TypeLibrary));
                    this.SaveObject(new XmlPersistSaveArgs(entry.Value, _DictNameValue, valueType, xmlItemElement, this.TypeLibrary));
                }
            }
            /// <summary>
            /// Z dodaného readeru načte a sestaví objekt uložený jako Dictionary
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            /// <returns></returns>
            private object IDictionaryTypeLoad(XmlPersistLoadArgs args)
            {
                args.CurrentOperation = "Create.IDictionary";
                object data = _ObjectCreate(args.DataType);
                IDictionary iDict = data as IDictionary;
                if (iDict == null) return null;

                args.CurrentOperation = "GetGenerics";
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());           // Reálný Type + jeho property
                Type keyType = args.DataTypeInfo.GetGenericType(0);
                Type valueType = args.DataTypeInfo.GetGenericType(1);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);

                args.CurrentOperation = "EnumerateItems";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    if (xmEle.Name == _DictNamePair)
                    {   // Element má odpovídající jméno. 
                        object key = null;
                        object value = null;
                        // Buď obsahuje atribut, nebo podřízený element s názvem Value:
                        XmAttribute atValue;
                        XmElement elValue;
                        if (_FindAttrElementByName(xmEle, _DictNameKey, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateKey";
                            XmlPersistLoadArgs itemArgs = this.CreateLoadArgs(args.Parameters, null, keyType, elValue, atValue);
                            key = this.LoadObject(itemArgs);
                        }
                        if (_FindAttrElementByName(xmEle, _DictNameValue, false, out elValue, out atValue))
                        {
                            args.CurrentOperation = "CreateValue";
                            XmlPersistLoadArgs itemArgs = this.CreateLoadArgs(args.Parameters, null, valueType, elValue, atValue);
                            value = this.LoadObject(itemArgs);
                        }
                        if (key != null)
                        {
                            args.CurrentOperation = "AddItem.IDictionary";
                            iDict.Add(key, value);
                        }
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);

                return iDict;
            }
            #endregion
            #region Compound typy: Save, Create
            /// <summary>
            /// Uloží do Xml dokumentu daný objekt, který je typu Compound (tj. má property)
            /// </summary>
            /// <param name="args">Kompletní datový balík</param>
            private void CompoundTypeSave(XmlPersistSaveArgs args)
            {
                args.CurrentOperation = "NotifyData.Begin";
                args.CurrentProperty = null;
                NotifyData(args.Data, XmlPersistState.SaveBegin);

                args.CurrentOperation = "SaveType";
                XmlElement xmlCurrElement = CreateElement(_ElementNameValue, args.XmlElement);
                SaveTypeAttribute(args, _ElementNameValue, xmlCurrElement);

                args.CurrentOperation = "SaveValues";
                if (args.HasObjectName)
                    CreateAttribute(_ElementNameValue + "." + XmAttribute.SuffixNameTarget, args.ObjectName, xmlCurrElement);
                foreach (TypeLibrary.PropInfo propInfo in args.DataTypeInfo.Properties)
                {   // V jednom cyklu zapisujeme data z jednoduhých i složitých propeties (tzn. do XmlAtributů i do XmlElementů),
                    //  to řeší až metoda SaveObject() podle typu persistence konkrétního objektu.
                    args.CurrentProperty = propInfo.Name;
                    object value = propInfo.Property.GetValue(args.Data, null);
                    this.SaveObject(new XmlPersistSaveArgs(value, propInfo.XmlName, propInfo.PropertyType, propInfo.XmlName, xmlCurrElement, this.TypeLibrary));
                }
                args.CurrentProperty = null;

                args.CurrentOperation = "NotifyData.End";
                NotifyData(args.Data, XmlPersistState.SaveDone);
            }
            /// <summary>
            /// Vytvoří a vrátí objekt typu Compound z dat XML
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private object CompoundTypeLoad(XmlPersistLoadArgs args)
            {
                // Vytvořím objekt s daty odpovídajícími datům persistovaným:
                args.CurrentOperation = "Create.Compound";
                object data = _ObjectCreate(args.DataType);

                args.CurrentOperation = "NotifyData.Begin";
                NotifyData(data, XmlPersistState.LoadBegin);
                args.DataTypeInfo = this.TypeLibrary.GetInfo(data.GetType());             // Reálný Type + jeho property

                // 1. Projdeme atributy, ty obsahují jednoduché datové typy. Uložíme je do property našeho objektu:
                args.CurrentOperation = "ReadAttributes";
                foreach (XmAttribute xmAtt in args.XmElement.XmAttributes)
                {
                    if (xmAtt.Values.Count == 0) continue;          // Atribut bez hodnoty = nese jen suffixy, například: Type, Indices, Assembly. Takové nelze zpracovávat zde.
                    TypeLibrary.PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(xmAtt.Name);
                    if (propInfo != null)
                    {
                        args.CurrentProperty = propInfo.Name;
                        XmlPersistLoadArgs nextArgs = this.CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, args.XmElement, xmAtt);
                        object value = this.LoadObject(nextArgs);
                        propInfo.SetValue(data, value);
                    }
                }

                // 2. Projdeme si sub-elementy našeho elementu. Pro každý z nich určíme, zda máme cílovou property, a pak načtu jejich hodnotu do této property:
                args.CurrentOperation = "ReadElements";
                foreach (XmElement xmEle in args.XmElement.XmElements)
                {
                    string name = xmEle.Name;
                    if (name == _ElementNameValue)
                    {
                        XmAttribute xmVal;
                        if (xmEle.TryGetAttribute(_ElementNameValue, out xmVal))
                            name = xmVal.Target;
                    }
                    TypeLibrary.PropInfo propInfo = args.DataTypeInfo.FindPropertyByXmlName(name);
                    if (propInfo != null)
                    {
                        args.CurrentProperty = propInfo.Name;
                        XmAttribute xmAte;
                        xmEle.TryGetAttribute(xmEle.Name, out xmAte);            // Pokud je v elementu uložen obraz objektu, který je jiného typu než je očekáván v property, pak je zde uložen i konkrétní Type
                        XmlPersistLoadArgs nextArgs = this.CreateLoadArgs(args.Parameters, propInfo, propInfo.PropertyType, xmEle, xmAte);
                        object value = this.LoadObject(nextArgs);
                        propInfo.SetValue(data, value);
                    }
                }

                args.CurrentOperation = "NotifyData.End";
                NotifyData(data, XmlPersistState.LoadDone);
                return data;
            }
            #endregion
            #region Konstanty
            private const string _DocumentNamePersist = "id-persistent";
            private const string _DocumentNameData = "id-data";
            private const string _DocumentNameValue = "id-value";
            private const string _ElementNameValue = "id-value";
            private const string _ArrayName = "id-array";
            private const string _ArrayNameItem = "id-item";
            private const string _DictNamePair = "id-pair";
            private const string _DictNameKey = "id-key";
            private const string _DictNameValue = "id-value";
            private const string _ArrayNameArrayRange = _ArrayName + "." + XmAttribute.SuffixNameRange;
            private const string _ArrayNameItemIndices = _ArrayNameItem + "." + XmAttribute.SuffixNameIndices;
            #endregion
        }
        #endregion
        #region Algoritmy verze - bázová třída
        internal class XmlPersistVBase
        {
            #region Konsrtuktor, Owner, TypeLibrary
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="owner"></param>
            protected XmlPersistVBase(XmlPersist owner)
            {
                this.Owner = owner;
                this.TypeLibrary = owner._TypeLibrary;
            }
            /// <summary>
            /// Majitel
            /// </summary>
            protected XmlPersist Owner { get; private set; }
            /// <summary>
            /// Knihovna typů
            /// </summary>
            protected TypeLibrary TypeLibrary { get; private set; }
            /// <summary>
            /// Začátek ukládání dat
            /// </summary>
            /// <param name="parameters"></param>
            protected void SaveBegin(PersistArgs parameters)
            {
                parameters.SavePrimitivesCount = 0;
                parameters.SavePrimitivesLength = 0;
                this.Parameters = parameters;
            }
            /// <summary>
            /// Konec ukládání dat
            /// </summary>
            protected void SaveEnd()
            {
                this.Parameters = null;
            }
            /// <summary>
            /// Uloží primitivní obsah do StringBuilderu a napočítá počet těchto primitivů
            /// </summary>
            /// <param name="content"></param>
            protected void SavePrimitive(string content)
            {
                if (this.Parameters != null)
                {
                    this.Parameters.SavePrimitivesCount++;
                    if (content != null)
                    {
                        this.Parameters.SavePrimitivesLength += content.Length;
                        if (this.Parameters.SavePrimitivesContent != null)
                            this.Parameters.SavePrimitivesContent.AppendLine(content);
                    }
                }
            }
            /// <summary>
            /// Parametry pro ukládání primitivů
            /// </summary>
            protected PersistArgs Parameters;
            #endregion
            #region Support pro práci s typy a argumenty
            /// <summary>
            /// Metoda zjistí, zda daný element (inElement) obsahuje atribut daného jména (name): pak ten atribut vloží do out findAttribute, a do out findElement vloží vstupující element a vrátí true.
            /// Pokud nenajde, podívá se do přímo podřízených elementů k elementu inElement, zda v něm najde element daného jména. Pokud najde, pak do out findElement vloží ten nalezený element,
            /// a do findAttribute zkusí najít atribut daného jména v tomto nalezeném elementu.
            /// Pokud nenajde, nechá všude null a vrátí false.
            /// Toto hledání odpovídá stylu ukládání Simple / Compound prvků.
            /// </summary>
            /// <param name="inElement"></param>
            /// <param name="name"></param>
            /// <param name="requiredAttInElement"></param>
            /// <param name="findElement"></param>
            /// <param name="findAttribute"></param>
            /// <returns></returns>
            protected bool _FindAttrElementByName(XmElement inElement, string name, bool requiredAttInElement, out XmElement findElement, out XmAttribute findAttribute)
            {
                // return this.Owner._FindAttrElementByName(inElement, name, requiredAttInElement, out findElement, out findAttribute);
                findElement = null;
                findAttribute = null;

                // Zkusím najít atribut s daným jménem:
                XmAttribute att;
                if (inElement.TryGetAttribute(name, out att))
                {   // Nalezen atribut (typický vzhled:  <element Value.Type="XmlPersistor.GID" Value="1234;5678" />)
                    findElement = inElement;
                    findAttribute = att;
                    return true;
                }

                // Zkusím hledat element s daným jménem:
                XmElement ele = inElement.TryFindElement(name);
                if (ele != null)
                {   // Uvnitř daného elementu <element> je element <name...> (například: <Value Value.Type="XmlPersistor.DataObject" BindFlags="Public, SetField" ... a další atributy = jednoduché property):
                    bool find = ele.TryGetAttribute(name, out att);
                    if (find || !requiredAttInElement)
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
            /// <param name="propInfo"></param>
            /// <param name="estimatedType"></param>
            /// <param name="xmElement"></param>
            /// <param name="xmAttribute"></param>
            /// <returns></returns>
            protected XmlPersistLoadArgs CreateLoadArgs(PersistArgs parameters, TypeLibrary.PropInfo propInfo, Type estimatedType, XmElement xmElement, XmAttribute xmAttribute)
            {
                Type dataType = null;
                if (xmAttribute != null && !String.IsNullOrEmpty(xmAttribute.Type))
                {
                    Type explicitDataType = TypeLibrary.GetTypeFromSerial(xmAttribute.Type, xmAttribute.Assembly);
                    if (explicitDataType != null)
                        dataType = explicitDataType;
                }
                if (dataType == null)
                {
                    dataType = estimatedType;
                    Type innerType = TypeLibrary.GetNullableInnerType(dataType);
                    if (innerType != null) dataType = innerType;
                }

                TypeLibrary.TypeInfo dataTypeInfo = null;
                if (dataType != null)
                    dataTypeInfo = this.TypeLibrary.GetInfo(dataType);

                return new XmlPersistLoadArgs(parameters, propInfo, dataType, dataTypeInfo, xmElement, xmAttribute);
            }
            /// <summary>
            /// Vytvoří a vrátí objekt daného typu
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            protected object _ObjectCreate(Type type)
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
            protected ConstructorInfo CheckConstructors(Type type)
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
            /// <param name="data"></param>
            /// <param name="xmlPersistState"></param>
            protected void NotifyData(object data, XmlPersistState xmlPersistState)
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
            #region Xml prvky - tvorba, zápisy, nalezení
            /// <summary>
            /// Založí nový element do elementu předaného. Nic do něj nevpisuje. Vrátí tento nově založený element.
            /// </summary>
            /// <param name="elementName">Název elementu (typicky jde o název property, nebo klíčové slovo pro element za položku listu, atd)</param>
            /// <param name="xParentElement"></param>
            /// <returns></returns>
            protected static XmlElement CreateElement(string elementName, XmlElement xParentElement)
            {
                string name = WorkScheduler.TypeLibrary.CreateXmlName(elementName);
                XmlElement xElement = xParentElement.OwnerDocument.CreateElement(name);
                xParentElement.AppendChild(xElement);
                return xElement;
            }
            /// <summary>
            /// Založí nový atribut a vloží jej do předaného elementu.
            /// </summary>
            /// <param name="name">Název atributu</param>
            /// <param name="value">Hodnota do atributu, string. Pokud bude null, bude atribut bez hodnoty.</param>
            /// <param name="xElement"></param>
            protected static XmlAttribute CreateAttribute(string name, string value, XmlElement xElement)
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
            protected static void SaveTypeAttribute(XmlPersistSaveArgs args)
            {
                SaveTypeAttribute(args, args.ObjectName, args.XmlElement);
            }
            /// <summary>
            /// V případě potřeby uloží do aktuálního elementu atribut, který ponese aktuální Type.
            /// Případ potřeby je tehdy, když se aktuální Type (currentType) liší od očekávaného (estimatedType).
            /// </summary>
            protected static void SaveTypeAttribute(XmlPersistSaveArgs args, string objectName, XmlElement xmlElement)
            {
                // Pokud se očekávaný typ a reálný typ neliší, pak do atributu nebudeme vepisovat explicitní typ pro aktuální objekt:
                if (TypeLibrary.IsEqualType(args.EstimatedType, args.DataType)) return;

                // Získám serializovatelné hodnoty o typu:
                string serialType, serialAssembly;
                TypeLibrary.GetSerialForType(args.DataType, out serialType, out serialAssembly);

                // Vytvořím atributy pro neprázdné serial hodnoty:
                if (!String.IsNullOrEmpty(serialType))
                    CreateAttribute(objectName + "." + XmAttribute.SuffixNameType, serialType, xmlElement);
                if (!String.IsNullOrEmpty(serialAssembly))
                    CreateAttribute(objectName + "." + XmAttribute.SuffixNameAssembly, serialAssembly, xmlElement);
            }
            /// <summary>
            /// Najde v daném readeru začátek daného elementu, 
            /// </summary>
            /// <param name="xmlReader"></param>
            /// <param name="elementName"></param>
            /// <param name="depthType"></param>
            protected static bool XmlReadFindElement(XmlTextReader xmlReader, string elementName, XmlElementDepthType depthType)
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
        }
        #endregion
        #region Interface adapteru
        /// <summary>
        /// Předpis pro persistor konkrétní verze
        /// </summary>
        internal interface IXmlPersistVersion
        {
            /// <summary>
            /// Uloží dodaná data podle daných parametrů do dodaného XML dokumentu
            /// </summary>
            /// <param name="data"></param>
            /// <param name="parameters"></param>
            /// <param name="xDoc"></param>
            void Save(object data, PersistArgs parameters, XmlDocument xDoc);
            /// <summary>
            /// Vrátí true, pokud XML dokument načtený v daném objektu odpovídá this adapteru pro čtení
            /// </summary>
            /// <param name="xmDoc"></param>
            /// <returns></returns>
            bool IsValidFormat(XmDocument xmDoc);
            /// <summary>
            /// Metoda vytvoří a vrátí objekt, který je persistován v daném XML dokumentu
            /// </summary>
            /// <param name="parameters"></param>
            /// <param name="xmDoc"></param>
            /// <returns></returns>
            object Load(PersistArgs parameters, XmDocument xmDoc);
        }
        #endregion
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
        /// <summary>
        /// Konstruktor
        /// </summary>
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
        /// <param name="type">Hledaný typ</param>
        /// <param name="typeConvertor">Nalezený konvertor typu</param>
        /// <returns></returns>
        internal XmlPersistenceType GetPersistenceType(Type type, out TypeConvertor typeConvertor)
        {
            // Pokud dodaný Type je Nullable, pak v dalším algoritmu budeme řešit jeho vnitřní typ, a nikoli typ Nullable:
            Type innerType = GetNullableInnerType(type);
            if (innerType != null) type = innerType;

            // Předdefinovaný Type?
            if (this._PresetTypes.TryGetValue(type, out typeConvertor))
                return typeConvertor.PersistenceType;

            if (ImplementInterface(type, typeof(IXmlSerializer))) return XmlPersistenceType.Self;
            if (type.IsEnum) return XmlPersistenceType.Enum;
            if (type.IsArray) return XmlPersistenceType.Array;
            if (type.IsInterface) return XmlPersistenceType.None;
            if (ImplementInterface(type, typeof(IList))) return XmlPersistenceType.IList;
            if (ImplementInterface(type, typeof(IDictionary))) return XmlPersistenceType.IDictionary;

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
        /// Metoda vrací true, pokud daný typ je nullable
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsNullableType(Type type)
        {
            Type innerType = GetNullableInnerType(type);
            return (innerType != null);
        }
        /// <summary>
        /// Metoda zjistí, zda daný Type je Nullable, a pokud ano pak vrací vnitřní datový typ.
        /// Jinak vrací null.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Type GetNullableInnerType(Type type)
        {
            if (type != null && type.IsValueType && type.IsGenericType &&
                type.Namespace == "System" && type.Name == "Nullable`1" &&
                type.GenericTypeArguments != null && type.GenericTypeArguments.Length == 1)
            {
                Type innerType = type.GenericTypeArguments[0];
                return innerType;
            }
            return null;
        }
        /// <summary>
        /// Metoda vrací true, pokud aktuální typ (currentType) je odpovídající očekávanému typu (estimatedType).
        /// </summary>
        /// <param name="estimatedType"></param>
        /// <param name="currentType"></param>
        /// <returns></returns>
        internal static bool IsEqualType(Type estimatedType, Type currentType)
        {
            // Pokud je některý typ null, pak nejsou shodné. To se vyskytne pouze u root typu, kdy estimated == null (a do serial dat se vepisuje full type root objektu).
            if (estimatedType == null || currentType == null) return false;

            // Neliší se typ reálný a očekávaný? OK, žádný atribut s typem se neuloží.
            if (Type.Equals(estimatedType, currentType)) return true;

            // Pokud očekávaný typ je nějaký Nullable, pak budeme testovat jeho interní typ.
            // Protože pokud máme někde použitou propetry typu: public Color? BackColor { get; set; }
            //  a ta má hodnotu Color.Black, pak v serializovaném tvaru nepotřebujeme mít přidaný atribut: BackColor.Type="System.Drawing.Color" ... BackColor="Black".
            Type innerType = GetNullableInnerType(estimatedType);
            if (innerType != null && Type.Equals(innerType, currentType)) return true;

            return false;
        }
        /// <summary>
        /// Určí text do atributu Type a Assembly pro daný Type.
        /// Pro některé typy není Assembly zapotřebí.
        /// Vrací takové informace, z nichž může reciproční metoda GetTypeFromSerial() vrátit Type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serialType"></param>
        /// <param name="serialAssembly"></param>
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
            string[] typeItems = typeName.Split('.');
            string result = "";
            for (int t = 0; t < (typeItems.Length - 1); t++)
                result = result + (t == 0 ? "" : ".") + typeItems[t];
            return result;
        }
        /// <summary>
        /// Vrátí true, pokud this type implementuje daný interface.
        /// Je obdobou testu "if (data is implementInterface)", ale pro případ, kdy nemáme objekt ale jeho Type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="implementInterface"></param>
        /// <returns></returns>
        internal static bool ImplementInterface(Type type, Type implementInterface)
        {
            return type.GetInterfaces().Any(i => Type.Equals(i, implementInterface));
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
            internal PropInfo[] Properties { get { return this._PropertyList.ToArray(); } }
            private List<PropInfo> _PropertyList;
            private Dictionary<string, PropInfo> _PropertyDict;
            #region Properties
            /// <summary>
            /// Detekuje PropertyInfo zdejšího typu a vytváří seznam this.PropertyList s prvky třídy PropInfo
            /// </summary>
            private void _DetectProperties()
            {
                this._PropertyList = new List<PropInfo>();
                this._PropertyDict = new Dictionary<string, PropInfo>();
                MemberInfo[] members = this.DataType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                Dictionary<string, MethodInfo> accessorsDict = members.Where(m => m.MemberType == MemberTypes.Method).Cast<MethodInfo>().Where(m => m.IsSpecialName).ToDictionary(m => m.Name);
                PropertyInfo[] properties = members.Where(m => m.MemberType == MemberTypes.Property).Cast<PropertyInfo>().ToArray();
                // PropertyInfo[] props = this.DataType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (PropertyInfo prop in properties)
                {
                    PropInfo propData = PropInfo.Create(this, prop, accessorsDict);
                    if (propData == null) continue;

                    if (this._PropertyDict.ContainsKey(propData.XmlName))
                        throw new ArgumentException("Chyba při persistenci typu " + this.DataType.Namespace + "." + this.DataType.Name + ": duplicitní název property: " + propData.XmlName + " (zkontrolujte atributy [PropertyName(string)])");

                    this._PropertyList.Add(propData);
                    this._PropertyDict.Add(propData.XmlName, propData);
                }
                // Seznam setřídit:
                if (this._PropertyList.Count > 1)
                    this._PropertyList.Sort(PropInfo.CompareByName);
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
            #region Vyhledání dle názvu
            internal PropInfo FindPropertyByXmlName(string propName)
            {
                if (String.IsNullOrEmpty(propName)) return null;
                PropInfo propData;
                if (!this._PropertyDict.TryGetValue(propName, out propData)) return null;
                return propData;
            }
            #endregion
        }
        #endregion
        #region class PropInfo : data o jedné property (jméno property, její typ, její TypeInfo
        /// <summary>
        /// PropInfo : data o jedné property
        /// </summary>
        internal class PropInfo
        {
            #region Vytvoření instance
            /// <summary>
            /// Zkusí vytvořit a vrátit instanci třídy <see cref="PropInfo"/> pro danou property.
            /// Pokud vrátí null, není možno s touto property pracovat.
            /// </summary>
            /// <param name="typeInfo"></param>
            /// <param name="property"></param>
            /// <param name="accessorsDict"></param>
            /// <returns></returns>
            public static PropInfo Create(TypeInfo typeInfo, PropertyInfo property, Dictionary<string, MethodInfo> accessorsDict)
            {
                if (property == null || accessorsDict == null) return null;

                if (property.Name == "SkinDict")
                {
                }



                // Pro některé property nebudu instanci vytvářet:
                if (!_IsPropertyAllowed(property)) return null;

                // Aby bylo možno property serializovat, musí mít správné GET a SET metody:
                MethodInfo getMethod, setMethod;
                bool getPrivate, setPrivate;
                if (!_IsPropertySerializable(property, accessorsDict, out getMethod, out getPrivate, out setMethod, out setPrivate)) return null;

                return new PropInfo(typeInfo, property, getMethod, getPrivate, setMethod, setPrivate);
            }
            /// <summary>
            /// Vrací true, pokud property je povoleno řešit.
            /// Tato metoda bere do úvahy: název property, její datový typ.
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            private static bool _IsPropertyAllowed(PropertyInfo property)
            {
                // Některé typy prostě serializovat nebudu:
                Type propertyType = property.PropertyType;
                string typeName = propertyType.Namespace + "." + propertyType.Name;
                if (typeName == "System.Type" || typeName == "System.RuntimeType") return false;

                // Některé property dělat nebudu podle jejich jména:
                Type ixpn = typeof(IXmlPersistNotify);
                string excl = ixpn.Namespace + "." + ixpn.Name + ".XmlPersistState";
                if (property.Name == excl) return false;

                return true;
            }
            /// <summary>
            /// Vrací true, pokud property je technicky možno serializovat.
            /// Tato metoda bere do úvahy: get a set metody k property.
            /// </summary>
            /// <param name="property"></param>
            /// <param name="accessorsDict"></param>
            /// <param name="getMethod"></param>
            /// <param name="getPrivate"></param>
            /// <param name="setMethod"></param>
            /// <param name="setPrivate"></param>
            /// <returns></returns>
            private static bool _IsPropertySerializable(PropertyInfo property, Dictionary<string, MethodInfo> accessorsDict, 
                out MethodInfo getMethod, out bool getPrivate, out MethodInfo setMethod, out bool setPrivate)
            {
                string name = property.Name;
                MethodInfo[] privateAccessors = null;

                // Musím najít obě metody = get i set:
                bool hasGet = accessorsDict.TryGetValue("get_" + name, out getMethod);
                getPrivate = false;
                if (!hasGet)
                {   // Pokud v "accessorsDict" není metoda get, může být důvodem to, že je deklarována jako "private" a přitom Reflected typ je potomkem.
                    // Pak privátní metody nejsou zahrnuty v sadě členů potomka.
                    // Ale persistor by je měl být schopen najít a použít.
                    // Taková situace nastane málokdy, proto může být řešena specifickým způsobem...
                    if (privateAccessors == null) privateAccessors = _GetPrivateAccessors(property);
                    getMethod = privateAccessors.FirstOrDefault(m => m.Name == "get_" + name);
                    hasGet = (getMethod != null);
                    getPrivate = hasGet;
                }

                bool hasSet = accessorsDict.TryGetValue("set_" + name, out setMethod);
                setPrivate = false;
                if (!hasSet)
                {
                    if (privateAccessors == null) privateAccessors = _GetPrivateAccessors(property);
                    setMethod = privateAccessors.FirstOrDefault(m => m.Name == "set_" + name);
                    hasSet = (setMethod != null);
                    setPrivate = hasSet;
                }
                if (!hasGet || !hasSet) return false;

                // Obě metody musí být non-indexer:
                ParameterInfo[] getParams = getMethod.GetParameters();
                if (getParams != null && getParams.Length > 0) return false;   // Má GET metodu s parametry (tj. indexer)

                ParameterInfo[] setParams = setMethod.GetParameters();
                if (setParams != null && setParams.Length > 1) return false;   // Má SET metodu s více parametry (tj. indexer)  (jeden parametr má vždy = setovaná hodnota)

                return true;
            }
            private static MethodInfo[] _GetPrivateAccessors(PropertyInfo property)
            {
                string name = property.Name;
                MethodInfo[] privateAccessors = property.DeclaringType
                    .GetMethods(BindFlags)
                    .Where(m => m.IsSpecialName && (m.Name == "get_" + name || m.Name == "set_" + name))
                    .ToArray();
                return privateAccessors;
            }
            /// <summary>
            /// Vrací true, pokud property je povoleno persistovat a klonovat.
            /// Tato metoda bere do úvahy: její atribut PersistingEnabled, hodnotu PersistEnable a CloneEnable
            /// </summary>
            /// <param name="property"></param>
            /// <param name="persistEnabled"></param>
            /// <param name="cloneEnabled"></param>
            /// <returns></returns>
            private static void _DetectPropertyPersistableCloneable(PropertyInfo property, out bool persistEnabled, out bool cloneEnabled)
            {
                persistEnabled = true;
                cloneEnabled = true;

                // Některé property to samy zakazují pomocí atributu: [PersistingEnabled(false)]:
                PersistingEnabledAttribute attEnabled = property.GetCustomAttributes(typeof(PersistingEnabledAttribute), true).Cast<PersistingEnabledAttribute>().FirstOrDefault();
                if (attEnabled != null)
                {
                    persistEnabled = attEnabled.PersistEnable;
                    cloneEnabled = attEnabled.CloneEnable;
                }
            }
            /// <summary>
            /// Metoda vrátí XmlName pro danou property
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            private static string _GetPropertyXmlName(PropertyInfo property)
            {
                string xmlName = property.Name;
                PropertyNameAttribute attName = property.GetCustomAttributes(typeof(PropertyNameAttribute), true).Cast<PropertyNameAttribute>().FirstOrDefault();
                if (attName != null && !String.IsNullOrEmpty(attName.PropertyName))
                    xmlName = attName.PropertyName;
                return WorkScheduler.TypeLibrary.CreateXmlName(xmlName);
            }
            /// <summary>
            /// Konstrutor
            /// </summary>
            /// <param name="typeInfo"></param>
            /// <param name="property"></param>
            /// <param name="getMethod"></param>
            /// <param name="setMethod"></param>
            private PropInfo(TypeInfo typeInfo, PropertyInfo property, MethodInfo getMethod, bool getPrivate, MethodInfo setMethod, bool setPrivate)
            {
                this.TypeInfo = typeInfo;
                this.Property = property;
                this.GetMethod = getMethod;
                this.GetMethodPrivate = getPrivate;
                this.SetMethod = setMethod;
                this.SetMethodPrivate = setPrivate;
                this._PropertyTypeInfo = null;

                bool persistEnabled, cloneEnabled;
                _DetectPropertyPersistableCloneable(property, out persistEnabled, out cloneEnabled);
                this.PersistEnabled = persistEnabled;
                this.CloneEnabled = cloneEnabled;

                this.XmlName = _GetPropertyXmlName(property);
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string text = this.Property.Name + ": ";
                if (this.PersistEnabled)
                    text += "XML name=" + this.XmlName + "; Type=" + this.PropertyType.Name;
                else
                    text += "Persist Disabled.";
                return text;
            }
            internal static BindingFlags BindFlags { get { return (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy); } }
            #endregion
            #region Properties
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

            public bool PersistEnabled { get; private set; }
            public bool CloneEnabled { get; private set; }
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
            /// Zajistí uložení hodnoty (value) do objektu (data) do jeho property this.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="value"></param>
            internal void SetValue(object data, object value)
            {
                if (this.SetMethodPrivate && this.SetMethod != null)
                    this.SetMethod.Invoke(data, new object[] { value });
                else
                    this.Property.SetValue(data, value);
            }
            private MethodInfo GetMethod { get; set; }
            private bool GetMethodPrivate { get; set; }
            private MethodInfo SetMethod { get; set; }
            private bool SetMethodPrivate { get; set; }
            #endregion
            #region Obecné static služby
            /// <summary>
            /// Určí, zda daná property se má persistovat (ukládat + načítat z XML)
            /// </summary>
            /// <param name="property"></param>
            /// <param name="atts"></param>
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
            /// <summary>
            /// Pro třídění dle Name
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            internal static int CompareByName(PropInfo a, PropInfo b)
            {
                return String.Compare(a.Name, b.Name);
            }
            #endregion
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
                // string lower = "abcdefghijklmnopqrstuvwxyz";
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
        #region enum XmlPersistenceType
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
        #endregion
    }
    #endregion
    #region class XmDocument: obálka nad objektem System.Xml.Linq.XDocument
    /// <summary>
    /// XmDocument: obálka nad objektem System.Xml.Linq.XDocument
    /// </summary>
    internal class XmDocument
    {
        #region Konstrukce - tvorba obálky XmDocument ze souboru nebo z textu
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
        #region Hledání elementu v rámci dokumentu
        /// <summary>
        /// Vyhledá a vrátí přesně zadaný element.
        /// Zadání se provádí uvedením všech elementů počínaje Root elementem, až k elementu který chci vrátit, oddělovačem elementů v cestě je lomítko / nebo \
        /// Například FindElement("root/data/first") vyhledá root element, v něm vyhledá element data a v něm pak first.
        /// </summary>
        /// <param name="elementPath"></param>
        /// <returns></returns>
        internal XmElement FindElement(string elementPath)
        {
            XmElement result = null;
            if (elementPath != null && elementPath.Length > 0)
            {
                XElement findIn = this._XDocument.Root;
                string[] elementNames = elementPath.Split('\\', '/');
                int len = elementNames.Length;
                if (findIn.Name.LocalName == elementNames[0])
                {   // Pokud se jméno (LocalName) skutečného Root elementu shoduje s požadovaným, tak jdeme hledat:
                    if (len == 1)
                        result = XmElement.Create(findIn);
                    else
                    {
                        for (int i = 1; i < len; i++)
                        {   // Hledáme další element, ne jen Root:
                            string elementName = elementNames[i];
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
    #region class XmElement: obálka nad objektem System.Xml.Linq.XElement
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
        /// <param name="xEle"></param>
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
        #endregion
        #region Atributy elementu
        /// <summary>
        /// Sada atributů tohoto elementu
        /// </summary>
        internal IEnumerable<XmAttribute> XmAttributes
        {
            get
            {
                if (this._XmAttributes == null)
                    this._ReadAttributes();
                return this._XmAttributes.Values;
            }
        }
        /// <summary>
        /// Sada atributů tohoto elementu, Dictionary
        /// </summary>
        private Dictionary<string, XmAttribute> XmAttributeDict
        {
            get
            {
                if (this._XmAttributes == null)
                    this._ReadAttributes();
                return this._XmAttributes;
            }
        }
        /// <summary>
        /// Najde ve zdejších atributech atribut daného jména (case sensitive) a vrátí jeho hodnotu Value. Pokud je jich víc, vrátí první.
        /// Pokud neexistuje atribut, nebo neobsahuje value, vrací defaultValue.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        internal bool TryGetAttribute(string attributeName, out XmAttribute attribute)
        {
            return (this.XmAttributeDict.TryGetValue(attributeName, out attribute) && attribute != null);
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
            XmAttribute attribute;
            if (!this.TryGetAttribute(attributeName, out attribute)) return defaultValue;
            string value = attribute.ValueFirstOrDefault;
            if (value == null) return defaultValue;
            return value;
        }
        /// <summary>
        /// Načte atributy tohoto elementu do <see cref="_XmAttributes"/>
        /// </summary>
        private void _ReadAttributes()
        {
            Dictionary<string, XmAttribute> attributes = new Dictionary<string, XmAttribute>();
            if (this._XElement.HasAttributes)
            {
                foreach (XAttribute xAtr in this._XElement.Attributes())
                {
                    string name, suffix;
                    XmAttribute.ParseSuffix(xAtr.Name.LocalName, out name, out suffix);

                    // Najdu / vytvořím atribut:
                    XmAttribute xmAtt;
                    if (!attributes.TryGetValue(name, out xmAtt))
                    {
                        xmAtt = new XmAttribute(name);
                        attributes.Add(name, xmAtt);
                    }

                    // Uložím suffix a hodnotu:
                    xmAtt.Add(suffix, xAtr.Value);
                }
            }
            this._XmAttributes = attributes;
        }
        /// <summary>
        /// Index atributů tohoto elementu, klíčem je název.
        /// Pokud má element více atributů shodného názvu, je zde uložen jen jedenkrát, 
        /// a všechny jeho hodnoty jsou uloženy v něm, v property <see cref="XmAttribute.Values"/>.
        /// </summary>
        private Dictionary<string, XmAttribute> _XmAttributes;
        #endregion
        #region Sub-elementy this elementu
        /// <summary>
        /// Sada vnořených elementů tohoto elementu
        /// </summary>
        internal IEnumerable<XmElement> XmElements
        {
            get
            {
                if (this._XmElements == null)
                    this._ReadElements();
                return this._XmElements;
            }
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
        /// <summary>
        /// Načte sub-elementy tohoto elementu do <see cref="_XmElements"/>
        /// </summary>
        private void _ReadElements()
        {
            List<XmElement> elements = new List<XmElement>();
            if (this._XElement.HasElements)
            {
                foreach (XElement subElement in this._XElement.Elements())
                    elements.Add(XmElement.Create(subElement));
            }
            this._XmElements = elements.ToArray();
        }
        /// <summary>
        /// Index atributů tohoto elementu, klíčem je název.
        /// Pokud má element více atributů shodného názvu, je zde uložen jen ten první!
        /// </summary>
        private XmElement[] _XmElements;
        #endregion
    }
    #endregion
    #region class XmAttribute : jeden atribut, slovník atributů
    /// <summary>
    /// XmAttribute: jeden atribut, logická jednotka (shrnutá z více záznamů shodného jména).
    /// Pokud jsou v elementu uvedeny atributy například: { Item = "hodnota", Item.Type = "String", Item.Indices = "[0,2,6]" };
    /// pak budou tyto atributy shrnuty v jednom objektu třídy <see cref="XmAttribute"/>, jehož <see cref="Name"/> = "Item", 
    /// kde obsah pole <see cref="Values"/> bude jeden prvek obsahující string: "hodnota", 
    /// a jehož Dictionary <see cref="_SuffixDict"/> bude obsahovat páry: { "Type" : "String" }; { "Indices" : "[0,2,6]" }.
    /// </summary>
    internal class XmAttribute
    {
        #region Konstrukce
        internal XmAttribute(string name)
        {
            this.Name = name;
            this.Values = new List<string>();
            this._SuffixDict = new Dictionary<string, string>();
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
        /// <summary>
        /// Název atributu, bez suffixu (tj. před první tečkou v názvu atributu)
        /// </summary>
        internal string Name { get; private set; }
        /// <summary>
        /// Hodnota atributu se suffixem .Target
        /// </summary>
        internal string Target { get { return GetSuffix(SuffixNameTarget); } }
        /// <summary>
        /// Hodnota atributu se suffixem .Type
        /// </summary>
        internal string Type { get { return GetSuffix(SuffixNameType); } }
        /// <summary>
        /// Hodnota atributu se suffixem .Assembly
        /// </summary>
        internal string Assembly { get { return GetSuffix(SuffixNameAssembly); } }
        /// <summary>
        /// Hodnota atributu se suffixem .Range
        /// </summary>
        internal string Range { get { return GetSuffix(SuffixNameRange); } }
        /// <summary>
        /// Hodnota atributu se suffixem .Indices
        /// </summary>
        internal string Indices { get { return GetSuffix(SuffixNameIndices); } }
        /// <summary>
        /// Hodnoty všech atributů daného jména, pokud je to jméno bez suffixu.
        /// Pokud jeden XML element obsahuje více atributů shodného názvu, hodnoty jsou zde, ze všech atributů tohoto názvu.
        /// </summary>
        internal List<string> Values { get; private set; }
        /// <summary>
        /// První hodnota z <see cref="Values"/>, nebo null.
        /// </summary>
        internal string ValueFirstOrDefault { get { return (this.Values.Count > 0 ? this.Values[0] : null); } }
        /// <summary>
        /// Všechny suffixy a jejich hodnoty
        /// </summary>
        internal IEnumerable<KeyValuePair<string, string>> Suffixes { get { return this._SuffixDict; } }
        /// <summary>
        /// Index suffixů
        /// </summary>
        private Dictionary<string, string> _SuffixDict;
        #endregion
        #region Metody Add, ContainSuffix, GetSuffix, ParseSuffix
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
                if (!this._SuffixDict.ContainsKey(suffix))
                    this._SuffixDict.Add(suffix, value);
                else
                    this._SuffixDict[suffix] = value;
            }
        }
        /// <summary>
        /// Vrací hodnotu uloženou za daným suffixem.
        /// Pokud nebyl nalezen atribut se jménem this.Name + "." + suffix, vrátí null.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private string GetSuffix(string suffix)
        {
            if (suffix == null) return null;
            if (!this._SuffixDict.ContainsKey(suffix)) return null;
            return this._SuffixDict[suffix];
        }
        /// <summary>
        /// Metoda rozdělí daný text v místě poslední tečky, a do out parametru name vloží obsah před tečkou a do suffix dá text za touto tečkou.
        /// </summary>
        /// <param name="attributeName">Typicky Attribute.Name.LocalName</param>
        /// <param name="name">Výstup jména = před první tečkou</param>
        /// <param name="suffix">Výstup suffixu = za první tečkou. Pokud attributeName nemá tečku, bude zde null. Pokud má tečku na konci textu, bude zde "".</param>
        internal static void ParseSuffix(string attributeName, out string name, out string suffix)
        {
            name = attributeName;
            suffix = null;
            if (!String.IsNullOrEmpty(name) && name.Contains("."))
            {
                int len = name.Length;
                int iDot = name.LastIndexOf('.');
                suffix = (iDot < (len - 1) ? name.Substring(iDot + 1) : "");   // Text za tečkou   = suffix (určuje property v XmAttribute, do které se uloží hodnota Value)
                name = (iDot > 0 ? name.Substring(0, iDot) : "");              // Text před tečkou = název logického atributu
            }
        }
        #endregion
        #region Konstanty
        /// <summary>
        /// Konstanta suffixu Type
        /// </summary>
        internal const string SuffixNameType = "Type";
        /// <summary>
        /// Konstanta suffixu Assembly
        /// </summary>
        internal const string SuffixNameAssembly = "Assembly";
        /// <summary>
        /// Konstanta suffixu Range
        /// </summary>
        internal const string SuffixNameRange = "Range";
        /// <summary>
        /// Konstanta suffixu Indices
        /// </summary>
        internal const string SuffixNameIndices = "Indices";
        /// <summary>
        /// Konstanta suffixu Target
        /// </summary>
        internal const string SuffixNameTarget = "Target";
        #endregion
    }
    #endregion
}
