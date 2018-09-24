using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Noris.LCS.Base.WorkScheduler;

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
        #region Serializace testy
        /// <summary>
        /// Test XML Serializace a Deserializace
        /// </summary>
        [TestMethod]
        public void TestXmlPersist()
        {
            TestPersist orig = new TestPersist();
            GuiId guiId = new GuiId(1180, 123456);
            orig.GuiId = guiId;

            GuiId guiId0 = new GuiId(21, 1234);
            GuiId guiId5 = new GuiId(26, 6789);
            orig.GuiIdList = new List<GuiId>();
            orig.GuiIdList.Add(guiId0);
            orig.GuiIdList.Add(new GuiId(22, 2345));
            orig.GuiIdList.Add(null);                    // new GuiId(23, 3456));
            orig.GuiIdList.Add(new GuiId(24, 4567));
            orig.GuiIdList.Add(new GuiId(25, 5678));
            orig.GuiIdList.Add(guiId5);

            orig.Tabulka = new string[30, 6];
            for (int r = 0; r < 30; r++)
                for (int c = 0; c < 6; c++)
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
            if (copy.Tabulka.GetLength(0) != 30) throw new AssertFailedException("TestPersist.Tabulka has Length(0) != 30");
            if (copy.Tabulka.GetLength(1) != 6) throw new AssertFailedException("TestPersist.Tabulka has Length(1) != 6");
            if (copy.Tabulka[12,2] != "Pozice(12,2)") throw new AssertFailedException("TestPersist.Tabulka has bad value in cell [12,2]");

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
            public List<GuiId> GuiIdList { get; set; }
            public string[,] Tabulka { get; set; }
            public TestDictionary Sachovnice { get; set; }
        }
        internal class TestDictionary : Dictionary<GuiId, Rectangle>
        { }
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
        protected class XmlResponse
        {
            public static XmlResponse CreateFrom(string xmlResponse)
            {
                XmlResponse result = new XmlResponse(xmlResponse);
                return result;
            }
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
            public bool IsValid { get; private set; }
            public string ResultState { get; private set; }
            public DateTime? Start { get; private set; }
            public DateTime? Stop { get; private set; }
            public string DatabaseId { get; private set; }
            public string DetailLevel { get; private set; }
            public string DetailErrorMessage { get; private set; }
            public DateTime? DetailTime { get; private set; }
            public string DetailText { get; private set; }
            public bool UserDataIsExists { get; private set; }
            public string UserDataIsBase64 { get; private set; }
            public string UserDataContent { get; private set; }
            public string AuditlogId { get; private set; }
            public string AuditlogState { get; private set; }

            // <RUNRESULT STATE=\"SUCCESS\" START=\"2018-09-19 15:28:08\" STOP=\"2018-09-19 15:30:31\" DATABASENUMBER=\"999999001\"><DETAIL /><USERDATA Base64Conversion=\"No\">OK</USERDATA><auditlog id=\"198689\" state=\"success\" /></RUNRESULT>
        }
        protected const string Response1 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"FAIL\" START=\"2018-09-19 15:20:23\" STOP=\"2018-09-19 15:24:35\" DATABASENUMBER=\"0\"><DETAIL LEVEL=\"SYSTEM\" errorMessage=\"Key «ClassNumber» value «9999» [System.Int32] was not found in the store «Noris.Repo.ClassStoreDefinition» !\" WHEN=\"2018-09-19 15:24:35\"><TEXT>System.Collections.Generic.KeyNotFoundException: Key «ClassNumber» value «9999» [System.Int32] was not... ...ServiceGate\\Processor.cs:line 398</TEXT></DETAIL></RUNRESULT>";
        protected const string Response2 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"FAIL\" START=\"2018-09-19 15:26:46\" STOP=\"2018-09-19 15:27:08\" DATABASENUMBER=\"0\"><DETAIL LEVEL=\"APPLICATION\" errorMessage=\"Exception of type 'Noris.Srv.StopProcessException' was thrown.\" WHEN=\"2018-09-19 15:27:08\"><TEXT>Noris.Srv.StopProcessException: Exception of type 'Noris.Srv.StopProcessException' was thrown.   at Noris.Message.StopProcess() ....ServiceGate\\Processor.cs:line 398</TEXT></DETAIL></RUNRESULT>";
        protected const string Response3 = "<?xml version=\"1.0\" encoding=\"utf-8\"?><RUNRESULT STATE=\"SUCCESS\" START=\"2018-09-19 15:28:08\" STOP=\"2018-09-19 15:30:31\" DATABASENUMBER=\"999999001\"><DETAIL /><USERDATA Base64Conversion=\"No\">OK</USERDATA><auditlog id=\"198689\" state=\"success\" /></RUNRESULT>";
        #endregion
    }
}
