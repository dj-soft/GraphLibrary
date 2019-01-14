using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace Asol.Tools.WorkScheduler.TestGUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.App.AppCompanyName = "Asseco Solutions";
            Application.App.AppProductName = "WorkScheduler";
            Application.App.AppProductTitle = "GraphLibrary utilities";

            Application.App.TracePriority = Application.TracePriority.Priority5_Normal;

            Application.App.RunMainForm(typeof(SchedulerForm));            // interní testovací data
            // Application.App.RunMainForm(typeof(PluginForm));               // data z Greenu


            // Application.App.RunMainForm(typeof(Asol.Tools.WorkScheduler.TestGUI.Forms.TestSnapForm));
            // Tests.TestLinq(6);
            // Application.App.RunMainForm(typeof(TestOneComponent));
            // Application.App.RunMainForm(typeof(TestFormGrid));
            // Application.App.RunMainForm(typeof(TestFinalForm));
            // Application.App.RunMainForm(typeof(TestFormNew));
            // Application.App.RunMainForm(typeof(Forms.MainForm));
            // Application.App.RunMainForm(typeof(Asol.Tools.WorkScheduler.TestGUI.Forms.TestGraphSettingForm));
        }
        #region Nepoužívané metody
        /*
        internal static Form CreateNewestForm()
        {
            // Najde nejnovější soubor nějakého formuláře v adresáři zdrojů, vytvoří jej a vrátí.
            // Prostě otevře ten formulář, se kterým si právě hrajeme.
            string runFile = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;                     // c:\DavidPrac\VSProjects\GraphLib\bin\TestGUI.exe
            string frmPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(runFile)), @"TestGUI\Forms");  // c:\DavidPrac\VSProjects\GraphLib\TestGUI\Forms
            DirectoryInfo dirInfo = new DirectoryInfo(frmPath);
            if (dirInfo.Exists)
            {
                List<FileInfo> files = dirInfo.GetFiles("*.*").ToList();
                if (files.Count > 0)
                {
                    files.Sort((a,b) => (b.LastWriteTime.CompareTo(a.LastWriteTime)));
                    FileInfo file = null;
                    foreach (FileInfo f in files)
                    {
                        if (!String.Equals(f.Extension, ".resx", StringComparison.InvariantCultureIgnoreCase) && !f.Name.EndsWith(".Designer.cs", StringComparison.InvariantCultureIgnoreCase))
                        {
                            file = f;
                            break;
                        }
                    }
                    if (file != null)
                    {
                        Form form = TryCreateForm(Path.GetFileNameWithoutExtension(file.Name));
                        if (form != null)
                            return form;
                    }
                }
            }

            // Záchranná cesta:
            return new TestFormGrid();
        }
        private static Form TryCreateForm(string typeName)
        {
            try
            {
                string nameSpace = typeof(Program).Namespace;
                object instance = null;

                instance = TryCreateInstance(nameSpace + "." + typeName);
                if (instance != null && instance is Form) return instance as Form;

                instance = TryCreateInstance(nameSpace + ".Forms." + typeName);
                if (instance != null && instance is Form) return instance as Form;

                instance = TryCreateInstance(nameSpace + ".Form." + typeName);
                if (instance != null && instance is Form) return instance as Form;
            }
            catch { }
            return null;
        }
        private static object TryCreateInstance(string typeName)
        {
            try
            {
                object result = null;
                System.Reflection.Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
                Type type = ass.GetTypes().FirstOrDefault(t => String.Equals(t.Namespace + "." + t.Name, typeName, StringComparison.InvariantCultureIgnoreCase));
                if (type != null)
                {
                    result = System.Activator.CreateInstance(type);
                }
                else
                {
                    result = System.Activator.CreateInstance(null, typeName);
                }

                if (result is System.Runtime.Remoting.ObjectHandle)
                {
                    System.Runtime.Remoting.ObjectHandle handle = result as System.Runtime.Remoting.ObjectHandle;
                    result = handle.Unwrap();
                }
                if (result != null) return result;
            }
            catch (Exception exc)
            {
                string msg = exc.Message;
            }
            return null;
        }
        */
        #endregion
    }
}
