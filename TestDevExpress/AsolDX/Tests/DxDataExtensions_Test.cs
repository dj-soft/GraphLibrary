using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noris.Clients.Win.Components.Tests
{
    using Noris.Clients.Win.Components.AsolDX;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Testy
    /// </summary>
    [TestClass]
    public class DataExtensionsTest
    {
        /// <summary>
        /// Test Align()
        /// </summary>
        [TestMethod]
        public void TestAlign()
        {
            // Int32
            TestAlignType(10, 20, new int[] { 5, 10, 15, 20, 25 }, new int[] { 10, 10, 15, 20, 20 });

            // Decimal
            TestAlignType(10m, 20m, new decimal[] { 5m, 9.999m, 10m, 10.001m, 15m, 19.999m, 20m, 20.001m, 25m }, new decimal[] { 10m, 10m, 10m, 10.001m, 15m, 19.999m, 20m, 20m, 20m });

            // DateTime
            TestAlignType(new DateTime(2021, 7, 1), new DateTime(2021, 7, 31),
                new DateTime[] { new DateTime(2021, 6, 1), new DateTime(2021, 7, 1), new DateTime(2021, 7, 15), new DateTime(2021, 7, 31), new DateTime(2021, 8, 10) },
                new DateTime[] { new DateTime(2021, 7, 1), new DateTime(2021, 7, 1), new DateTime(2021, 7, 15), new DateTime(2021, 7, 31), new DateTime(2021, 7, 31) });

            // String
            TestAlignType("E", "I", new string[] { "A", "Dyson", "E", "Epsilon", "G", "I", "Idea", "K" }, new string[] { "E", "E", "E", "Epsilon", "G", "I", "I", "I" });
        }
        /// <summary>
        /// Provede test Align() pro dané rozmezí Min-Max, pro dané hodnoty a pro očekávané výsledky
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="values"></param>
        /// <param name="results"></param>
        private void TestAlignType<T>(T min, T max, T[] values, T[] results) where T : IComparable<T>
        {
            if (values.Length != results.Length) Assert.Fail("values.Length != results.Length");

            for (int i = 0; i < values.Length; i++)
            {
                T value = values[i];
                T result = results[i];
                T align;

                align = value.Align(min, max);
                Assert.AreEqual(align, result);

                align = value.Align(min, min);
                Assert.AreEqual(align, min);

                align = value.Align(max, min);
                Assert.AreEqual(align, max);
            }
        }
    }

    /// <summary>
    /// Testy
    /// </summary>
    [TestClass]
    public class CompressorTest
    {
        /// <summary>
        /// Test Align()
        /// </summary>
        [TestMethod]
        public void TestCompress()
        {
            string text = @"SQL Server APPLY operator has two variants; CROSS APPLY and OUTER APPLY

    The CROSS APPLY operator returns only those rows from the left table expression (in its final output) if it matches with the right table expression. In other words, the right table expression returns rows for the left table expression match only.
    The OUTER APPLY operator returns all the rows from the left table expression irrespective of its match with the right table expression. For those rows for which there are no corresponding matches in the right table expression, it contains NULL values in columns of the right table expression.
    So you might conclude, the CROSS APPLY is equivalent to an INNER JOIN (or to be more precise its like a CROSS JOIN with a correlated sub-query) with an implicit join condition of 1=1 whereas the OUTER APPLY is equivalent to a LEFT OUTER JOIN.

You might be wondering if the same can be achieved with a regular JOIN clause, so why and when do you use the APPLY operator? Although the same can be achieved with a normal JOIN, the need of APPLY arises if you have a table-valued expression on the right part and in some cases the use of the APPLY operator boosts performance of your query. Let me explain with some examples.";
            byte[] data = Encoding.UTF8.GetBytes(text);
            var zip = Noris.Clients.Win.Components.AsolDX.Compressor.Compress(data);

            var unzip = Noris.Clients.Win.Components.AsolDX.Compressor.DeCompress(zip);
            string result = Encoding.UTF8.GetString(unzip);

            if (text != result)
                Assert.Fail("Unzip != Zip");
        }
    }

}
