using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Application
{
    #region Test engine
    /// <summary>
    /// TestEngine
    /// </summary>
    internal class TestEngine
    {
        /// <summary>
        /// Run all test with any from specified type 
        /// </summary>
        /// <param name="testType"></param>
        internal static void RunTests(TestType testType)
        {
            List<TestResultItem> resultList = new List<TestResultItem>();
            var testProviders = App.GetPlugins(typeof(ITest));
            foreach (var instance in testProviders)
            {
                ITest testProvider = instance as ITest;
                if (testProvider != null)
                {
                    if ((testType & testProvider.TestType) != 0)
                    {
                        RunTestOne(testProvider, testType, resultList);
                    }
                }
            }
        }
        /// <summary>
        /// Run one test
        /// </summary>
        /// <param name="testProvider"></param>
        /// <param name="testType"></param>
        /// <param name="resultList"></param>
        private static void RunTestOne(ITest testProvider, TestType testType, List<TestResultItem> resultList)
        {
            TestArgs testArgs = new TestArgs(testProvider, testType, resultList);
            try
            {
                testProvider.RunTest(testArgs);
            }
            catch (Exception exc)
            {
                resultList.Add(new TestResultItem(testProvider, TestResultType.TestError, "Exception in test: " + exc.Message));
            }
        }
    }
    #endregion
    #region Data for test: class TestArgs, TestResultItem; enum TestResultType
    /// <summary>
    /// Arguments for one test
    /// </summary>
    public class TestArgs
    {
        public TestArgs(ITest testProvider, TestType testType, List<TestResultItem> resultList)
        {
            this._TestProvider = testProvider;
            this._TestType = testType;
            this._ResultList = resultList;
        }
        private ITest _TestProvider;
        private List<TestResultItem> _ResultList;
        /// <summary>
        /// Type of running tests.
        /// Test provider can select subtest set by this type.
        /// </summary>
        public TestType TestType { get { return this._TestType; } } private TestType _TestType;
        /// <summary>
        /// Add any result (info/warning/error) from test
        /// </summary>
        /// <param name="resultType"></param>
        /// <param name="message"></param>
        public void AddResult(TestResultType resultType, string message)
        {
            _ResultList.Add(new TestResultItem(this._TestProvider, resultType, message));
        }
    }
    /// <summary>
    /// One result item from test
    /// </summary>
    public class TestResultItem
    {
        public TestResultItem(ITest testProvider, TestResultType resultType, string resultMessage)
        {
            this.TestProvider = testProvider;
            this.ResultType = resultType;
            this.ResultMessage = resultMessage;
        }
        /// <summary>
        /// Test Provider for this info
        /// </summary>
        public ITest TestProvider { get; private set; }
        /// <summary>
        /// Type of info
        /// </summary>
        public TestResultType ResultType { get; private set; }
        /// <summary>
        /// String message
        /// </summary>
        public string ResultMessage { get; private set; }
    }
    /// <summary>
    /// Type of result info from test
    /// </summary>
    public enum TestResultType
    {
        /// <summary>
        /// Info
        /// </summary>
        Info,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Error in test, with which application can run
        /// </summary>
        TestError,
        /// <summary>
        /// Error in test, with which application can not run
        /// </summary>
        ApplicationError
    }
    
    #endregion
    #region interface ITest, enum TestType
    /// <summary>
    /// Interface for provider of test
    /// </summary>
    public interface ITest : IPlugin
    {
        /// <summary>
        /// Type of test
        /// </summary>
        TestType TestType { get; }
        /// <summary>
        /// Entry for test process in this class
        /// </summary>
        void RunTest(TestArgs testArgs);
    }
    [Flags]
    public enum TestType
    {
        None = 0,
        Essential = 0x0001,
        AtStartup = 0x0002,

        HardWorkLoad = 0x8000,
        AllStandard = 0x00FF,
        All = 0xFFFF
    }
    
    #endregion

}
