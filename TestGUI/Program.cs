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

            var formTypes = GetAvailableForms();
            int count = formTypes.Length;
            if (count == 0)
                Application.App.ShowError("V projektu 'TestGUI' nebyl nalezen žádný hlavní formulář [IsMainForm]");
            else if (count == 1)
                Application.App.RunMainForm(formTypes[0]);
            else
                Application.App.RunMainForm(typeof(Forms.SelectMainForm));
            


            /*
            if (Control.ModifierKeys == Keys.Shift)
            {   // Spuštění s klávesou Shift, pomocí ikonky  |> Start  :
                Application.App.RunMainForm(typeof(DataForm));                 // interní testovací data
            }
            else
            {
                Application.App.RunMainForm(typeof(SchedulerForm));            // interní testovací data
            }
            */

            // Application.App.RunMainForm(typeof(PluginForm));                // data z Greenu

            // Application.App.RunMainForm(typeof(Asol.Tools.WorkScheduler.TestGUI.Forms.TestOneComponent));
            // Application.App.RunMainForm(typeof(Asol.Tools.WorkScheduler.TestGUI.Forms.TestSnapForm));
            // Application.App.RunMainForm(typeof(TestOneComponent));
            // Application.App.RunMainForm(typeof(TestFormGrid));
            // Application.App.RunMainForm(typeof(TestFinalForm));
            // Application.App.RunMainForm(typeof(TestFormNew));
            // Application.App.RunMainForm(typeof(Forms.MainForm));
            // Application.App.RunMainForm(typeof(Asol.Tools.WorkScheduler.TestGUI.Forms.TestGraphSettingForm));
            // Tests.TestLinq(6);
        }
        /// <summary>
        /// Vrátí typy Main formulářů
        /// </summary>
        /// <returns></returns>
        public static Type[] GetAvailableForms()
        {
            bool isShiftKey = (Control.ModifierKeys == Keys.Shift);
            bool isDebugMode = System.Diagnostics.Debugger.IsAttached;
            var myType = typeof(Asol.Tools.WorkScheduler.TestGUI.Program);
            var myAssembly = myType.Assembly;
            var myAssemblyTypes = myAssembly.GetTypes();
            var formTypes = myAssemblyTypes.Where(t => IsMainForm(t, isShiftKey, isDebugMode)).ToList();                        // Vhodné formuláře
            var formSorts = formTypes.Select(t => new Tuple<IsMainFormAttribute, Type>(GetMainFormAttribute(t), t)).ToList();   // Přidat k nim metadata
            formSorts = AcceptOnlyAutoRun(formSorts);                // Pokud některé formuláře mají režim AutoRun, pak akceptovat pouze takové
            formSorts.Sort((a, b) => IsMainFormAttribute.CompareToList(a.Item1, b.Item1));                   // Setřídit dle metadat pomocí komparátoru
            return formSorts.Select(t => t.Item2).ToArray();         // Vrátit setříděné pole typů
        }
        /// <summary>
        /// Zjistí, zda daný typ je Main formulář
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isShiftKey"></param>
        /// <param name="isDebugMode"></param>
        /// <returns></returns>
        private static bool IsMainForm(Type type, bool isShiftKey, bool isDebugMode)
        {
            if (!type.IsClass || type.IsAbstract || type.IsArray) return false;
            if (!type.IsSubclassOf(typeof(Form))) return false;
            var imfa = GetMainFormAttribute(type);
            if (imfa == null) return false;
            if (imfa.FormMode == MainFormMode.OnlyDebug && !isDebugMode) return false;
            return true;
        }
        /// <summary>
        /// Vrací informace o daném typu z hlediska Main formuláře
        /// </summary>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static IsMainFormAttribute GetMainFormAttribute(Type formType)
        {
            var imfas = formType.GetCustomAttributes(typeof(IsMainFormAttribute), true);
            if (imfas.Length == 0) return null;
            var imfa = imfas[0] as IsMainFormAttribute;
            if (imfa == null) return null;
            return imfa;
        }
        /// <summary>
        /// Vrátí pole formulářů pro nabídku a spuštění
        /// </summary>
        /// <param name="formsAll"></param>
        /// <returns></returns>
        private static List<Tuple<IsMainFormAttribute, Type>> AcceptOnlyAutoRun(List<Tuple<IsMainFormAttribute, Type>> formsAll)
        {
            if (formsAll == null || formsAll.Count == 0) return formsAll;
            var formsAutoRun = formsAll.Where(t => t.Item1.FormMode == MainFormMode.AutoRun).ToList();
            return (formsAutoRun.Count > 0 ? formsAutoRun : formsAll);
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
    /// <summary>
    /// Atribut, který říká, že daný Form má být nabízen jako Main form při spouštění aplikace
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class IsMainFormAttribute : Attribute
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="formName">Uživatelský název formuláře</param>
        /// <param name="formMode">Režim aktivace formuláře</param>
        /// <param name="order">Pořadí v nabídce</param>
        public IsMainFormAttribute(string formName, MainFormMode formMode = MainFormMode.Default, int order = 0)
        {
            this._FormName = formName;
            this._Order = order;
            this._FormMode = formMode;
        }
        readonly string _FormName;
        readonly int _Order;
        readonly MainFormMode _FormMode;
        /// <summary>
        /// Uživatelské jméno formuláře
        /// </summary>
        public string FormName { get { return _FormName; } }
        /// <summary>
        /// Režim aktivace formuláře
        /// </summary>
        public MainFormMode FormMode { get { return _FormMode; } }
        /// <summary>
        /// Pořadí v nabídce
        /// </summary>
        public int Order { get { return _Order; } }
        /// <summary>
        /// Zajistí porovnání dvou instancí tak, aby podle výsledku byl setříděn List prvků dle <see cref="Order"/> ASC, <see cref="FormName"/> ASC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareToList(IsMainFormAttribute a, IsMainFormAttribute b)
        {
            int cmp = a.Order.CompareTo(b.Order);
            if (cmp == 0) cmp = String.Compare(a.FormName, b.FormName, StringComparison.CurrentCultureIgnoreCase);
            return cmp;
        }
    }
    /// <summary>
    /// Režim hlavního formuláře
    /// </summary>
    public enum MainFormMode
    {
        /// <summary>
        /// Defaultní
        /// </summary>
        Default = 0,
        /// <summary>
        /// Automaticky spouštěný = pokud bude nalezen takový Form, nebude se dávat nabídka ostatních a spustí se rovnou tento
        /// </summary>
        AutoRun,
        /// <summary>
        /// Akceptovat pouze v Debug režimu
        /// </summary>
        OnlyDebug
    }
}
