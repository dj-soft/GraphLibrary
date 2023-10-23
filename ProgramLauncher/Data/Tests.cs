using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DjSoft.Tools.ProgramLauncher
{
    internal class TestManager
    {
        public static void RunTests()
        {
            __Messages = new List<MessageInfo>();

            var testTypes = typeof(TestManager).Assembly.GetTypes().Where(t => t.IsClass).ToList();
            testTypes.Sort((a, b) => a.FullName.CompareTo(b.FullName));
            foreach (var testType in testTypes)
            {
                if (testType.CustomAttributes.Any(ca => ca.AttributeType == typeof(TestClassAttribute)))
                {
                    try
                    {
                        __CurrentClass = testType.FullName;
                        var testMethods = testType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).ToList();
                        testMethods.Sort((a, b) => a.Name.CompareTo(b.Name));
                        foreach (var testMethod in testMethods)
                        {
                            if (testMethod.CustomAttributes.Any(ca => ca.AttributeType == typeof(TestMethodAttribute)))
                            {
                                try
                                {
                                    __CurrentMethod = testMethod.Name;
                                    testMethod.Invoke(null, null);
                                }
                                catch (Exception exc)
                                {
                                    AddError(exc);
                                }
                                __CurrentMethod = null;
                            }
                        }
                        __CurrentClass = null;
                    }
                    catch (Exception exc)
                    {
                        AddError(exc);
                    }
                }
            }

            if (__Messages.Count > 0)
            {

            }
        }
        public static void AddError(string text)
        {
            __Messages.Add(new MessageInfo(text));
        }
        public static void AddError(Exception exc)
        {
            if (exc.GetType() == typeof(TargetInvocationException) && exc.InnerException != null)
                exc = exc.InnerException;

            __Messages.Add(new MessageInfo(exc.Message));
        }
        private static List<MessageInfo> __Messages;
        private static string __CurrentClass;
        private static string __CurrentMethod;
        private class MessageInfo
        {
            public MessageInfo(string text)
            {
                Class = TestManager.__CurrentClass; 
                Method = TestManager.__CurrentMethod;
                Text = text;
            }
            /// <summary>
            /// Vizualizace
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{Class}:{Method} => {Text}";
            }
            public string Class { get; private set; }
            public string Method { get; private set; }
            public string Text { get; private set; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute 
    { }
}
