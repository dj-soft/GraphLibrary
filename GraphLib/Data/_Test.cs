using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Asol.Tools.WorkScheduler.Data;
using Noris.LCS.Base.WorkScheduler;
using Asol.Tools.WorkScheduler.Data.Parsing;
using RES = Noris.LCS.Base.WorkScheduler.Resources;

namespace Asol.Tools.WorkScheduler.Data.Test
{
    /// <summary>
    /// Třída obsahující testy dat
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class TestData
    {
        /// <summary>
        /// Test dat
        /// </summary>
        [TestMethod]
        public void TestDataRun()
        {
          
        }

        #region Testy Parseru
        /// <summary>
        /// Test Parseru
        /// </summary>
        [TestMethod]
        public void TestParserSql()
        {
            string sql1 = @"SELECT (SELECT top 1 sklad FROM lcs.sk_hlavicka h WHERE h.cislo_subjektu = s.cislo_subjektu)
 ,s.*
FROM lcs. [subjekty] s with (updlock, holdlock)
   /* podmnožina hlaviček pro pořadač 15307 a sklady s referencí 6%, a doklady s referencí obsahující 3 */
inner join (select * from lcs.sk_hlavicka where cislo_poradace = 15307) x on x.cislo_subjektu = s.cislo_subjektu 
    and (x.sklad in (select sk.cislo_subjektu from lcs.sk_sklad sk join lcs.subjekty su on su.cislo_subjektu = sk.cislo_subjektu where su.reference_subjektu like '6%'))
where s.reference_subjektu like '%3%' or s.nazev_subjektu = 'Kontra''A';

DECLARE @cislo INT;
SET @cislo = 15307;
";
            ParsedItem result = Parser.ParseString(sql1, DefaultSettings.MsSql);
            string sql2 = result.Text;
            string sql3 = ((IParsedItemExtended)result).TextEdit;

            string rtf = ((IParsedItemExtended)result).RtfText;

            string segmentName = DefaultSettings.SQL_CODE;
            string tables = "";
            result.ScanItems(
                null,
                f => (f.SegmentName == segmentName),
                t =>
                {
                    if (t.ItemType == Data.Parsing.ItemType.Text)
                    {
                        tables += t.Text + "; ";
                    }
                    return null;
                });
        }
        #endregion
        #region Test Regex
        /// <summary>
        /// Test Regex
        /// </summary>
        [TestMethod]
        public void TestRegex()
        {
            System.Text.RegularExpressions.Regex regex = null;

            regex = RegexSupport.CreateWildcardsRegex(@"Data\Page\*\Grid?");
            //       Wildcards =          @"Data\Page\*\Grid?"
            bool isMatch11 = regex.IsMatch(@"Data\Page\Panel\Grid1");          // true, jinak chyba:
            if (!isMatch11) throw new AssertFailedException("Chyba Regex 11");

            bool isMatch12 = regex.IsMatch(@"Data\Page\Mini\Grid1");           // true, jinak chyba:
            if (!isMatch12) throw new AssertFailedException("Chyba Regex 12");

            bool isMatch13 = regex.IsMatch(@"Data\Page\Panel\Grid2");          // true, jinak chyba:
            if (!isMatch13) throw new AssertFailedException("Chyba Regex 13");

            bool isMatch14 = regex.IsMatch(@"Data\Pag1\Panel\Grid1");          // false, jinak chyba:
            if (isMatch14) throw new AssertFailedException("Chyba Regex 14");

            regex = RegexSupport.CreateWildcardsRegex(@"*\Page\*\G*");
            //       Wildcards =          @"*\Page\*\G*"
            bool isMatch21 = regex.IsMatch(@"Gui\Page\\Gcdef1");               // true, jinak chyba:
            if (!isMatch21) throw new AssertFailedException("Chyba Regex 21");

            bool isMatch22 = regex.IsMatch(@"Gui\Page12\Aaa\G12");             // false, jinak chyba:
            if (isMatch22) throw new AssertFailedException("Chyba Regex 22");

            bool isMatch23 = regex.IsMatch(@"D\Page\25\Gcdef1");               // true, jinak chyba:
            if (!isMatch23) throw new AssertFailedException("Chyba Regex 23");

            bool isMatch24 = regex.IsMatch(@"C\Page\\E21");                    // false, jinak chyba:
            if (isMatch24) throw new AssertFailedException("Chyba Regex 24");
        }
        #endregion
        #region Testy různých Data/Extension metod
        /// <summary>
        /// Testy různých Data/Extension metod
        /// </summary>
        [TestMethod]
        public void TestExtensions()
        {
            _TestTimeRound(TimeSpan.FromSeconds(782.45d), 
                           TimeSpan.FromSeconds(900d),
                           new DateTime(2018, 9, 18, 6, 38, 5, 405, DateTimeKind.Local),
                           new DateTime(2018, 9, 18, 6, 45, 0, 0, DateTimeKind.Local));

            _TestTimeRound(TimeSpan.FromSeconds(84.12d),
                           TimeSpan.FromSeconds(300d),
                           new DateTime(2018, 9, 18, 6, 38, 5, 405, DateTimeKind.Local),
                           new DateTime(2018, 9, 18, 6, 40, 0, 0, DateTimeKind.Local));

            _TestTimeRound(TimeSpan.FromMinutes(42.5d),
                           TimeSpan.FromMinutes(60d),
                           new DateTime(2018, 9, 18, 23, 38, 1, 105, DateTimeKind.Local),
                           new DateTime(2018, 9, 19, 0, 0, 0, 0, DateTimeKind.Local));

        }
        /// <summary>
        /// Test pro jednu sadu hodnot
        /// </summary>
        /// <param name="timeRaw"></param>
        /// <param name="timeExp"></param>
        /// <param name="dateRaw"></param>
        /// <param name="dateExp"></param>
        private static void _TestTimeRound(TimeSpan timeRaw, TimeSpan timeExp, DateTime dateRaw, DateTime dateExp)
        {
            TimeSpan timeRound = timeRaw.GetRoundTimeBase();
            if (timeRound != timeExp) throw new AssertFailedException("TimeSpan.GetRoundTimeBase() error: expected value: " + timeExp.ToString() + ", returned value " + timeRound.ToString() + ".");

            DateTime dateRound = dateRaw.RoundTime(timeRound);
            if (dateRound != dateExp) throw new AssertFailedException("TestExtensions.RoundTime() error: expected time: " + dateExp.ToString("hh:MM:ss.fff") + ", returned time " + dateRound.ToString("hh:MM:ss.fff") + ".");
        }
        #endregion
        #region Testy IntervalArray
        /// <summary>
        /// Testy IntervalArray
        /// </summary>
        [TestMethod]
        public void TestIntervalArray()
        {
            TimeRangeArray intervalArray = new TimeRangeArray();
            intervalArray.Merge(_IntervalCreate(12, 14));
            _IntervalAssert(intervalArray, "12-14");
            intervalArray.Merge(_IntervalCreate(18, 20));
            _IntervalAssert(intervalArray, "12-14,18-20");
            intervalArray.Merge(_IntervalCreate(6, 10));
            _IntervalAssert(intervalArray, "06-10,12-14,18-20");
            intervalArray.Merge(_IntervalCreate(11, 12));
            _IntervalAssert(intervalArray, "06-10,11-14,18-20");
            intervalArray.Merge(_IntervalCreate(08, 13));
            _IntervalAssert(intervalArray, "06-14,18-20");
            intervalArray.Merge(_IntervalCreate(15, 16));
            _IntervalAssert(intervalArray, "06-14,15-16,18-20");
            intervalArray.Merge(_IntervalCreate(20, 22));
            _IntervalAssert(intervalArray, "06-14,15-16,18-22");
            intervalArray.Merge(_IntervalCreate(19, 22));
            _IntervalAssert(intervalArray, "06-14,15-16,18-22");
            intervalArray.Merge(_IntervalCreate(04, 07));
            _IntervalAssert(intervalArray, "04-14,15-16,18-22");
            intervalArray.Merge(_IntervalCreate(14, 15));
            _IntervalAssert(intervalArray, "04-16,18-22");

            intervalArray.Clear();
            intervalArray.Merge(_IntervalCreate(06, 12));
            _IntervalAssert(intervalArray, "06-12");
            intervalArray.Merge(_IntervalCreate(08, 10));
            _IntervalAssert(intervalArray, "06-12");
            intervalArray.Merge(_IntervalCreate(06, 14));
            _IntervalAssert(intervalArray, "06-14");
            intervalArray.Merge(_IntervalCreate(14, 16));
            _IntervalAssert(intervalArray, "06-16");
            intervalArray.Merge(_IntervalCreate(18, 20));
            _IntervalAssert(intervalArray, "06-16,18-20");
            intervalArray.Merge(_IntervalCreate(21, 22));
            _IntervalAssert(intervalArray, "06-16,18-20,21-22");
            intervalArray.Merge(_IntervalCreate(02, 04));
            _IntervalAssert(intervalArray, "02-04,06-16,18-20,21-22");
            intervalArray.Merge(_IntervalCreate(02, 22));
            _IntervalAssert(intervalArray, "02-22");

            intervalArray.Clear();
            intervalArray.Merge(_IntervalCreate(08, 12));
            _IntervalAssert(intervalArray, "08-12");
            intervalArray.Merge(_IntervalCreate(12, 14));
            _IntervalAssert(intervalArray, "08-14");
            intervalArray.Merge(_IntervalCreate(14, 16));
            _IntervalAssert(intervalArray, "08-16");
            intervalArray.Merge(_IntervalCreate(06, 18));
            _IntervalAssert(intervalArray, "06-18");
            intervalArray.Merge(_IntervalCreate(04, 06));
            _IntervalAssert(intervalArray, "04-18");
            intervalArray.Merge(_IntervalCreate(18, 20));
            _IntervalAssert(intervalArray, "04-20");
        }
        private TimeRange _IntervalCreate(int h1, int h2)
        {
            return new TimeRange(new DateTime(2019, 06, 01, h1, 0, 0), new DateTime(2019, 06, 01, h2, 0, 0));
        }
        private void _IntervalAssert(TimeRangeArray intervalArray, string expected)
        {
            string current = _IntervalToString(intervalArray);
            if (current == expected) return;
            Assert.Fail("Liší se hodnota reálná: " + current + " od očekáváné: " + expected);
        }
        private string _IntervalToString(TimeRangeArray intervalArray)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in intervalArray.Items)
            {
                string time = item.Begin.Value.ToString("HH") + "-" + item.End.Value.ToString("HH");
                sb.Append((sb.Length > 0 ? "," : "") + time);
            }
            return sb.ToString();
        }
        #endregion
        #region Testy serializace
        /// <summary>
        /// Testy serializace
        /// </summary>
        [TestMethod]
        public void TestXmlPersistFromFile()
        {
            string file = @"d:\DavidPrac\Vývoje\0061146 Dílenské plánování D4\Zálohy\serial-Manufacturing.xml";
            string serial = System.IO.File.ReadAllText(file, Encoding.UTF8);
            object result = Persist.Deserialize(serial);
            GuiData guiData = result as GuiData;
        }

        /// <summary>
        /// Test XML Serializace a Deserializace
        /// </summary>
        [TestMethod]
        public void TestXmlPersistClass()
        {
            TestPersist orig = new TestPersist();
            GuiId guiId = new GuiId(1180, 123456);
            orig.GuiId = guiId;

            orig.Array = new object[6];
            orig.Array[0] = "Zkouška\r\nřádku";
            orig.Array[1] = new DateTime(2019, 01, 15, 12, 0, 0);
            orig.Array[2] = 16.02m;
            orig.Array[3] = new Rectangle(5, 10, 100, 50);
            orig.Array[4] = new List<int> { 10, 20, 30 };


            GuiId guiId0 = new GuiId(21, 1234);
            GuiId guiId5 = new GuiId(26, 6789);
            orig.GuiIdList = new List<GuiId>();
            orig.GuiIdList.Add(guiId0);
            orig.GuiIdList.Add(new GuiId(22, 2345));
            orig.GuiIdList.Add(null);                    // new GuiId(23, 3456));
            orig.GuiIdList.Add(new GuiId(24, 4567));
            orig.GuiIdList.Add(new GuiId(25, 5678));
            orig.GuiIdList.Add(guiId5);

            orig.Tabulka = new string[3, 2];
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 2; c++)
                    orig.Tabulka[r, c] = "Pozice(" + r + "," + c + ")";

            orig.Sachovnice = new TestDictionary();
            GuiId key1 = new GuiId(1, 101);
            GuiId key6 = new GuiId(1, 106);
            orig.Sachovnice.Add(key1, new Rectangle(1, 1, 1, 1));
            orig.Sachovnice.Add(new GuiId(1, 102), new Rectangle(2, 2, 2, 2));
            orig.Sachovnice.Add(new GuiId(1, 103), new Rectangle(3, 3, 3, 3));
            orig.Sachovnice.Add(new GuiId(1, 104), new Rectangle(4, 4, 4, 4));
            orig.Sachovnice.Add(new GuiId(1, 105), new Rectangle(5, 5, 5, 5));
            orig.Sachovnice.Add(key6, new Rectangle(6, 6, 6, 6));

            orig.Images = new List<GuiImage>();
            orig.Images.Add(RES.Images.Actions16.DialogNo3Png);
            orig.Images.Add(RES.Images.Actions16.DialogOk3Png);
            orig.Images.Add(RES.Images.Actions16.DialogOkApply3Png);

            string zip = Persist.Serialize(orig, PersistArgs.Compressed);
            string xml = Persist.Serialize(orig, PersistArgs.Default);

            TestPersist copy = Persist.Deserialize(zip) as TestPersist;

            // Test shodného obsahu:
            if (copy.GuiId == null) throw new AssertFailedException("TestPersist.GuiId is null");
            if (copy.GuiId != guiId) throw new AssertFailedException("TestPersist.GuiId is not equal to original");

            if (copy.GuiIdList == null) throw new AssertFailedException("TestPersist.GuiIdList is null");
            if (copy.GuiIdList.Count != 6) throw new AssertFailedException("TestPersist.GuiIdList has bad count");
            if (copy.GuiIdList[0] != guiId0) throw new AssertFailedException("TestPersist.GuiIdList[0] is not equal to original");
            if (copy.GuiIdList[2] != null) throw new AssertFailedException("TestPersist.GuiIdList[2] is not null");
            if (copy.GuiIdList[5] != guiId5) throw new AssertFailedException("TestPersist.GuiIdList[5] is not equal to original");

            if (copy.Tabulka == null) throw new AssertFailedException("TestPersist.Tabulka is null");
            if (copy.Tabulka.Rank != 2) throw new AssertFailedException("TestPersist.Tabulka has Rank != 2");
            if (copy.Tabulka.GetLength(0) != 3) throw new AssertFailedException("TestPersist.Tabulka has Length(0) != 3");
            if (copy.Tabulka.GetLength(1) != 2) throw new AssertFailedException("TestPersist.Tabulka has Length(1) != 2");
            if (copy.Tabulka[2,1] != "Pozice(2,1)") throw new AssertFailedException("TestPersist.Tabulka has bad value in cell [2,1]");

            if (copy.Sachovnice == null) throw new AssertFailedException("TestPersist.Sachovnice is null");
            if (copy.Sachovnice.Count != 6) throw new AssertFailedException("TestPersist.Sachovnice has bad count");
            if (!copy.Sachovnice.ContainsKey(key1)) throw new AssertFailedException("TestPersist.Sachovnice does not contains key0");
            if (copy.Sachovnice[key1].Top != 1) throw new AssertFailedException("TestPersist.Sachovnice in [key0] has bad value");
            if (!copy.Sachovnice.ContainsKey(key6)) throw new AssertFailedException("TestPersist.Sachovnice does not contains key5");
            if (copy.Sachovnice[key6].Top != 6) throw new AssertFailedException("TestPersist.Sachovnice in [key5] has bad value");
        }
        internal class TestPersist
        {
            [PropertyName("GuiID_položka")]
            public GuiId GuiId { get; set; }
            public object[] Array { get; set; }
            public List<GuiId> GuiIdList { get; set; }
            public string[,] Tabulka { get; set; }
            public TestDictionary Sachovnice { get; set; }
            public List<GuiImage> Images { get; set; }
        }
        internal class TestDictionary : Dictionary<GuiId, Rectangle>
        { }
        /// <summary>
        /// Test XML Serializace a Deserializace
        /// </summary>
        [TestMethod]
        public void TestXmlPersistSimple()
        {
            object[] source = new object[3];
            source[0] = 0;
            source[1] = "XXX";
            object[] values = new object[3];
            TestSimpleClass value0 = new TestSimpleClass() { Name = "Name0", Value = 0 };
            TestSimpleClass value1 = new TestSimpleClass() { Name = "Name1", Value = "Item1" };
            TestSimpleClass value2 = new TestSimpleClass() { Name = "Name2" };
            TestSimpleClass value2a = new TestSimpleClass() { Name = "Name2a", Value = "Item2a" };
            value2.Value = value2a;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;

            source[2] = values;

            string xml = Persist.Serialize(source, PersistArgs.Default);

            object result = Persist.Deserialize(xml);
            if (result == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized = null");

            object[] target = result as object[];
            if (target == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized is not array object[]");
            if (target.Length != source.Length) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong length");

            if (!(target[0] is int)) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong type of item[0]");
            if (((int)(target[0])) != ((int)(source[0]))) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong value of item[0]");

            if (!(target[1] is string)) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong type of item[1]");
            if (((string)(target[1])) != ((string)(source[1]))) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong value of item[1]");

            if (target[2] == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong item[2] == null");
            object[] clones = target[2] as object[];
            if (clones == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array has wrong type item[2], has be object[]");
            if (clones.Length != values.Length) throw new AssertFailedException("TestXmlPersistSimple: deserialized array in item[2] has wrong length");

            TestSimpleClass clone0 = clones[0] as TestSimpleClass;
            if (clone0 == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array in item[2] has wrong item[0]");
            if (clone0.Name != value0.Name) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 0 has wrong Name");
            if (clone0.Value.ToString() != value0.Value.ToString()) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 0 has wrong Value");

            TestSimpleClass clone1 = clones[1] as TestSimpleClass;
            if (clone1 == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array in item[2] has wrong item[1]");
            if (clone1.Name != value1.Name) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 1 has wrong Name");
            if (clone1.Value.ToString() != value1.Value.ToString()) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 1 has wrong Value");

            TestSimpleClass clone2 = clones[2] as TestSimpleClass;
            if (clone2 == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array in item[2] has wrong item[2]");
            if (clone2.Name != value2.Name) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 2 has wrong Name");

            TestSimpleClass clone2a = clone2.Value as TestSimpleClass;
            if (clone2a == null) throw new AssertFailedException("TestXmlPersistSimple: deserialized array in item[2] has wrong item[2].Value");
            if (clone2a.Name != value2a.Name) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 2a has wrong Name");
            if (clone2a.Value.ToString() != value2a.Value.ToString()) throw new AssertFailedException("TestXmlPersistSimple: deserialized clone 2a has wrong Value");

        }
        #endregion
        #region ServiceGate XML response testy
        /// <summary>
        /// Test zpracování XML response
        /// </summary>
        [TestMethod]
        public void TestXmlResponse()
        {
            XmlResponse response1 = XmlResponse.CreateFrom(Response1);
            XmlResponse response2 = XmlResponse.CreateFrom(Response2);
            XmlResponse response3 = XmlResponse.CreateFrom(Response3);
            XmlResponse response4 = XmlResponse.CreateFrom("Úplná kravina beze smyslu <což jest pravda< !");
            XmlResponse response5 = XmlResponse.CreateFrom("");
            XmlResponse response6 = XmlResponse.CreateFrom(null);
        }
        /// <summary>
        /// XmlResponse
        /// </summary>
        protected class XmlResponse
        {
            /// <summary>
            /// Vrátí XmlResponse z daného stringu
            /// </summary>
            /// <param name="xmlResponse"></param>
            /// <returns></returns>
            public static XmlResponse CreateFrom(string xmlResponse)
            {
                XmlResponse result = new XmlResponse(xmlResponse);
                return result;
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="xmlResponse"></param>
            private XmlResponse(string xmlResponse)
            {
                System.Xml.Linq.XDocument xDoc = _TryParseXml(xmlResponse);
                this._ReadXDocument(xDoc);
            }
            private XmlResponse(System.Xml.Linq.XDocument xDoc)
            {
                this._ReadXDocument(xDoc);
            }
            private static System.Xml.Linq.XDocument _TryParseXml(string xmlResponse)
            {
                System.Xml.Linq.XDocument xDoc = null;
                try
                {
                    xDoc = System.Xml.Linq.XDocument.Parse(xmlResponse, System.Xml.Linq.LoadOptions.PreserveWhitespace);
                }
                catch
                {
                    xDoc = null;
                }
                return xDoc;
            }
            private void _ReadXDocument(System.Xml.Linq.XDocument xDoc)
            {
                if (xDoc == null) return;
                System.Xml.Linq.XElement xResult = xDoc.Element("RUNRESULT");
                if (xResult == null) return;
                this.ResultState = _ReadAttributeString(xResult, "STATE");
                this.Start = _ReadAttributeDateTime(xResult, "START");
                this.Stop = _ReadAttributeDateTime(xResult, "STOP");
                this.DatabaseId = _ReadAttributeString(xResult, "DATABASENUMBER");

                System.Xml.Linq.XElement xDetail = _GetElement(xResult, "DETAIL");
                this.DetailLevel = _ReadAttributeString(xDetail, "LEVEL");
                this.DetailErrorMessage = _ReadAttributeString(xDetail, "errorMessage");
                this.DetailTime = _ReadAttributeDateTime(xDetail, "WHEN");

                System.Xml.Linq.XElement xText = _GetElement(xDetail, "TEXT");
                this.DetailText = _ReadElementValue(xText);

                System.Xml.Linq.XElement xUserData = _GetElement(xResult, "USERDATA");
                this.UserDataIsExists = (xUserData != null);
                this.UserDataIsBase64 = _ReadAttributeString(xUserData, "Base64Conversion");
                this.UserDataContent = _ReadElementValue(xUserData);

                System.Xml.Linq.XElement xAuditlog = _GetElement(xResult, "auditlog");
                this.AuditlogId = _ReadAttributeString(xAuditlog, "id");
                this.AuditlogState = _ReadAttributeString(xAuditlog, "state");

                this.IsValid = true;
            }
            private static string _ReadAttributeString(System.Xml.Linq.XElement xElement, string attributeName)
            {
                if (xElement == null) return "";
                System.Xml.Linq.XAttribute xAttribute = xElement.Attribute(attributeName);
                if (xAttribute == null) return "";
                return xAttribute.Value;
            }
            private static DateTime? _ReadAttributeDateTime(System.Xml.Linq.XElement xElement, string attributeName)
            {
                string text = _ReadAttributeString(xElement, attributeName);
                if (String.IsNullOrEmpty(text)) return null;
                DateTime dateTime = (DateTime)Noris.LCS.Base.WorkScheduler.Convertor.StringToDateTime(text);
                if (dateTime.Year < 1900) return null;
                return dateTime;
            }
            private static string _ReadElementValue(System.Xml.Linq.XElement xElement)
            {
                if (xElement == null) return "";
                return xElement.Value;
            }
            private static System.Xml.Linq.XElement _GetElement(System.Xml.Linq.XElement xElement, string elementName)
            {
                if (xElement == null) return null;
                return xElement.Element(elementName);
            }
            /// <summary>
            /// Je platný?
            /// </summary>
            public bool IsValid { get; private set; }
            /// <summary>
            /// ResultState
            /// </summary>
            public string ResultState { get; private set; }
            /// <summary>
            /// Čas Start
            /// </summary>
            public DateTime? Start { get; private set; }
            /// <summary>
            /// Čas Stop
            /// </summary>
            public DateTime? Stop { get; private set; }
            /// <summary>
            /// DatabaseId
            /// </summary>
            public string DatabaseId { get; private set; }
            /// <summary>
            /// DetailLevel
            /// </summary>
            public string DetailLevel { get; private set; }
            /// <summary>
            /// DetailErrorMessage
            /// </summary>
            public string DetailErrorMessage { get; private set; }
            /// <summary>
            /// DetailTime
            /// </summary>
            public DateTime? DetailTime { get; private set; }
            /// <summary>
            /// DetailText
            /// </summary>
            public string DetailText { get; private set; }
            /// <summary>
            /// UserDataIsExists
            /// </summary>
            public bool UserDataIsExists { get; private set; }
            /// <summary>
            /// UserDataIsBase64
            /// </summary>
            public string UserDataIsBase64 { get; private set; }
            /// <summary>
            /// UserDataContent
            /// </summary>
            public string UserDataContent { get; private set; }
            /// <summary>
            /// AuditlogId
            /// </summary>
            public string AuditlogId { get; private set; }
            /// <summary>
            /// AuditlogState
            /// </summary>
            public string AuditlogState { get; private set; }

            // <RUNRESULT STATE=\"SUCCESS\" START=\"2018-09-19 15:28:08\" STOP=\"2018-09-19 15:30:31\" DATABASENUMBER=\"999999001\"><DETAIL /><USERDATA Base64Conversion=\"No\">OK</USERDATA><auditlog id=\"198689\" state=\"success\" /></RUNRESULT>
        }
        /// <summary>
        /// Vzorek 1
        /// </summary>
        protected const string Response1 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"FAIL\" START=\"2018-09-19 15:20:23\" STOP=\"2018-09-19 15:24:35\" DATABASENUMBER=\"0\"><DETAIL LEVEL=\"SYSTEM\" errorMessage=\"Key «ClassNumber» value «9999» [System.Int32] was not found in the store «Noris.Repo.ClassStoreDefinition» !\" WHEN=\"2018-09-19 15:24:35\"><TEXT>System.Collections.Generic.KeyNotFoundException: Key «ClassNumber» value «9999» [System.Int32] was not... ...ServiceGate\\Processor.cs:line 398</TEXT></DETAIL></RUNRESULT>";
        /// <summary>
        /// Vzorek 2
        /// </summary>
        protected const string Response2 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"FAIL\" START=\"2018-09-19 15:26:46\" STOP=\"2018-09-19 15:27:08\" DATABASENUMBER=\"0\"><DETAIL LEVEL=\"APPLICATION\" errorMessage=\"Exception of type 'Noris.Srv.StopProcessException' was thrown.\" WHEN=\"2018-09-19 15:27:08\"><TEXT>Noris.Srv.StopProcessException: Exception of type 'Noris.Srv.StopProcessException' was thrown.   at Noris.Message.StopProcess() ....ServiceGate\\Processor.cs:line 398</TEXT></DETAIL></RUNRESULT>";
        /// <summary>
        /// Vzorek 3
        /// </summary>
        protected const string Response3 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"SUCCESS\" START=\"2018-09-19 15:28:08\" STOP=\"2018-09-19 15:30:31\" DATABASENUMBER=\"999999001\"><DETAIL /><USERDATA Base64Conversion=\"No\">OK</USERDATA><auditlog id=\"198689\" state=\"success\" /></RUNRESULT>";
        #endregion
    }
    /// <summary>
    /// Třída pro testy: Name + Value
    /// </summary>
    internal class TestSimpleClass
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Value
        /// </summary>
        public object Value { get; set; }
    }
}
