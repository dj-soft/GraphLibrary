using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace DjSoft.Tools.SDCardTester
{
    internal class App
    {
        #region Singleton
        private static App Instance
        {
            get
            {
                if (__Instance is null)
                {
                    lock (__Locker)
                    {
                        if (__Instance is null)
                        {
                            __Instance = new App();
                            __Instance.Initialize();
                        }
                    }
                }
                return __Instance;
            }
        }
        private static App __Instance;
        private static Object __Locker = new Object();
        /// <summary>
        /// Konstruktor = první fáze inicializace. Zde se nesmí používat singleton <see cref="Instance"/>, protože dosud není uložen.
        /// </summary>
        private App()
        {
            __Settings = new AppSettings();
        }
        /// <summary>
        /// Druhá fáze inicializace, volá se až po uložení instance do <see cref="__Instance"/>. Zde se již smí používat singleton <see cref="Instance"/>.
        /// </summary>
        private void Initialize()
        {
            __Settings.Load();
        }
        #endregion
        #region Settings
        /// <summary>
        /// Konfigurace aplikace, persistovaná v souboru
        /// </summary>
        public static AppSettings Settings { get { return Instance.__Settings; } }
        private AppSettings __Settings;
        #endregion
        #region Obecný servis
        public static System.Windows.Forms.Form MainForm
        {
            get
            {
                var forms = System.Windows.Forms.Application.OpenForms
                    .OfType<System.Windows.Forms.Form>()
                    .Where(f => f != null && !f.IsDisposed && f.IsHandleCreated)
                    .ToArray();
                if (forms.Length == 0) return null;

                var visibleForm = forms.FirstOrDefault(f => f.Visible);
                if (visibleForm is null) visibleForm = forms[0];

                return visibleForm;
            }
        }
        public static void RunInGui(Action action)
        {
            var mainForm = MainForm;
            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(action);
            }
            else
            {
                action();
            }
        }
        #endregion
        #region MessageBoxy
        /// <summary>
        /// Aplikace se ukončuje (uživatel zavřel okno během běhu akce a čeká se na její zastavení, nebo se okno zavírá).
        /// V tomto stavu metody Show* nic nezobrazují: <see cref="ShowInfo"/>, <see cref="ShowWarning"/> a <see cref="ShowError"/> nic nedělají,
        /// <see cref="ShowQuestion"/> vrací false (= Ne, typicky „nepokračovat“).
        /// </summary>
        public static bool IsClosing
        {
            get { return Instance._IsClosing; } 
            set { Instance._IsClosing = value;  } 
        }
        /// <summary>
        /// Aplikace se ukončuje (uživatel zavřel okno během běhu akce a čeká se na její zastavení, nebo se okno zavírá).
        /// </summary>
        private bool _IsClosing
        {
            get { return __IsClosing; }
            set
            {
                bool isChanged = (__IsClosing != value);
                __IsClosing = value;
                if (isChanged)
                    __IsClosingChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Aplikace se ukončuje
        /// </summary>
        private static volatile bool __IsClosing;
        /// <summary>
        /// Událost vyvolaná po změně stavu <see cref="IsClosing"/>. Eventhandler tedy již čte aktuální stav.
        /// </summary>
        public static event EventHandler IsClosingChanged { add { Instance.__IsClosingChanged += value; } remove { Instance.__IsClosingChanged -= value; } }
        private event EventHandler __IsClosingChanged;
        /// <summary>
        /// Zobrazí informaci v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowInfo(string message, string caption = "Information")
        {
            if (IsClosing) return;
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information) );
        }
        /// <summary>
        /// Zobrazí varování v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowWarning(string message, string caption = "Warning")
        {
            if (IsClosing) return;
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning) );
        }
        /// <summary>
        /// Zobrazí chybovou hlášku v MessageBoxu. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        public static void ShowError(string message, string caption = "Error")
        {
            if (IsClosing) return;
            RunInGui(() => System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error) );
        }
        /// <summary>
        /// Zobrazí dotaz Ano / Ne v MessageBoxu a vrátí true = Ano. Automaticky se spustí v GUI vlákně, pokud je potřeba.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public static bool ShowQuestion(string message, string caption = "Question")
        {
            if (IsClosing) return false;
            var result = DialogResult.None;
            RunInGui(() => result = System.Windows.Forms.MessageBox.Show(MainForm, message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question));
            return (result == DialogResult.Yes);
        }
        #endregion
    }
    #region class AppSettings : konfigurace aplikace, ukládaná do XML souboru
    /// <summary>
    /// Konfigurace aplikace, ukládaná do XML souboru <see cref="FileName"/>.
    /// <para/>
    /// Hodnoty jsou uloženy jako stringy v <see cref="__Values"/>, klíčem je název property.
    /// Přidání nové property = jeden řádek: <c>public string Xxx { get { return _GetValue(); } set { _SetValue(value); } }</c>,
    /// metody <see cref="Load"/> a <see cref="Save"/> se nemění.
    /// <para/>
    /// Setter změněné hodnoty ihned ukládá soubor.
    /// </summary>
    internal class AppSettings
    {
        #region Konstrukce, umístění souboru
        /// <summary>
        /// Konstruktor. Nenačítá data, to provede až <see cref="Load"/>.
        /// </summary>
        public AppSettings()
        {
            __Values = new Dictionary<string, string>();
            __Lock = new object();
            __IsLoading = false;
        }
        /// <summary>
        /// Plné jméno souboru s konfigurací: LocalAppData\DjSoft\SDCardTester\Settings.xml
        /// </summary>
        public static string FileName
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AuthorName, ApplicationName, SettingsFileName); }
        }
        private const string AuthorName = "DjSoft";
        private const string ApplicationName = "SDCardTester";
        private const string SettingsFileName = "Settings.xml";
        #endregion
        #region Public properties
        /// <summary>
        /// Posledně zadané vstupní soubory pro FileRescue, každý soubor na jednom řádku.
        /// Getter vrací řádky oddělené CRLF; v úložišti a v XML jsou odděleny jen LF.
        /// </summary>
        public string LastFileRescueInputFileNames { get { return _ToCrLf(_GetValue()); } set { _SetValue(_ToLf(value)); } }
        /// <summary>
        /// Posledně zadaný výstupní adresář pro FileRescue
        /// </summary>
        public string LastFileRescueOutputFilesPath { get { return _GetValue(); } set { _SetValue(value); } }
        /// <summary>
        /// FileRescue: barvit OK bloky v mapě odstíny podle rychlosti čtení. Výchozí = true (uloženo "1" / "0").
        /// </summary>
        public bool FileRescueSpeedShading { get { return (_GetValue() != "0"); } set { _SetValue(value ? "1" : "0"); } }
        /// <summary>
        /// Umístění hlavního okna při posledním zavření: "Stav;X;Y;Šířka;Výška", kde Stav je Normal nebo Maximized
        /// a souřadnice jsou vždy Bounds okna ve stavu Normal (pro Maximized = RestoreBounds). Null = dosud neuloženo.
        /// Skládání a rozklad textu řeší MainForm.
        /// </summary>
        public string MainFormPlacement { get { return _GetValue(); } set { _SetValue(value); } }
        #endregion
        #region Úložiště hodnot
        /// <summary>
        /// Vrátí hodnotu pro danou property, nebo null když není uložena
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string _GetValue([CallerMemberName] string name = null)
        {
            lock (__Lock)
            {
                return (__Values.TryGetValue(name, out var value) ? value : null);
            }
        }
        /// <summary>
        /// Uloží hodnotu pro danou property. Pokud se hodnota změnila, uloží soubor.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        private void _SetValue(string value, [CallerMemberName] string name = null)
        {
            lock (__Lock)
            {
                var oldValue = (__Values.TryGetValue(name, out var v) ? v : null);
                if (String.Equals(oldValue, value, StringComparison.Ordinal)) return;
                if (value is null)
                    __Values.Remove(name);
                else
                    __Values[name] = value;
            }
            Save();
        }
        /// <summary>
        /// Vrátí text, kde konce řádků jsou pouze LF (odebere CR). Null zůstává null.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string _ToLf(string text)
        {
            return text?.Replace("\r", "");
        }
        /// <summary>
        /// Vrátí text, kde konce řádků jsou CRLF. Null zůstává null.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string _ToCrLf(string text)
        {
            return _ToLf(text)?.Replace("\n", "\r\n");
        }
        /// <summary>
        /// Hodnoty: Key = název property, Value = hodnota
        /// </summary>
        private Dictionary<string, string> __Values;
        /// <summary>
        /// Zámek pro přístup k <see cref="__Values"/> a k souboru
        /// </summary>
        private object __Lock;
        /// <summary>
        /// Právě probíhá <see cref="Load"/>, v tomto stavu se neukládá
        /// </summary>
        private volatile bool __IsLoading;
        #endregion
        #region Load a Save
        /// <summary>
        /// Načte hodnoty ze souboru <see cref="FileName"/>.
        /// Pokud soubor neexistuje nebo je poškozený, ponechá výchozí hodnoty (null). Nevyhazuje chyby.
        /// </summary>
        public void Load()
        {
            try
            {
                __IsLoading = true;
                lock (__Lock)
                {
                    __Values.Clear();
                    try
                    {
                        var fileName = FileName;
                        if (File.Exists(fileName))
                            _LoadFile(fileName);
                    }
                    catch
                    {   // Poškozený soubor: ponecháme výchozí hodnoty
                        __Values.Clear();
                    }
                }
            }
            finally
            {
                __IsLoading = false;
            }
        }
        /// <summary>
        /// Načte hodnoty z daného souboru přímo do <see cref="__Values"/> (ne přes settery, takže se neukládá)
        /// </summary>
        /// <param name="fileName"></param>
        private void _LoadFile(string fileName)
        {
            var document = XDocument.Load(fileName);
            var root = document.Root;
            if (root is null || root.Name.LocalName != XmlRootName) return;
            foreach (var element in root.Elements(XmlValueName))
            {
                var name = (string)element.Attribute(XmlNameAttribute);
                if (String.IsNullOrEmpty(name)) continue;
                __Values[name] = element.Value;
            }
        }
        /// <summary>
        /// Uloží hodnoty do souboru <see cref="FileName"/>.
        /// Nevyhazuje chyby: případnou chybu oznámí uživateli pomocí <see cref="App.ShowError(string, string)"/>, až mimo zámek.
        /// </summary>
        public void Save()
        {
            if (__IsLoading) return;

            Exception error = null;
            lock (__Lock)
            {
                try
                {
                    _SaveFile(FileName);
                }
                catch (Exception exc)
                {
                    error = exc;
                }
            }

            // Chybu hlásíme až po uvolnění zámku: ShowError může čekat na GUI thread, který může současně čekat na tento zámek.
            if (error != null)
                App.ShowError($"Nepodařilo se uložit nastavení do souboru:\r\n{FileName}\r\n\r\n{error.Message}", "Chyba při ukládání nastavení");
        }
        /// <summary>
        /// Uloží hodnoty do dočasného souboru, pak smaže původní soubor a dočasný přejmenuje na původní
        /// </summary>
        /// <param name="fileName"></param>
        private void _SaveFile(string fileName)
        {
            var path = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var root = new XElement(XmlRootName, new XAttribute(XmlVersionAttribute, XmlVersion));
            foreach (var pair in __Values.OrderBy(p => p.Key, StringComparer.Ordinal))
                root.Add(new XElement(XmlValueName, new XAttribute(XmlNameAttribute, pair.Key), pair.Value));
            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

            var tempFile = fileName + ".tmp";
            var writerSettings = new XmlWriterSettings() { Indent = true, Encoding = new UTF8Encoding(true) };
            using (var writer = XmlWriter.Create(tempFile, writerSettings))
                document.Save(writer);

            if (File.Exists(fileName))
                File.Delete(fileName);
            File.Move(tempFile, fileName);
        }
        private const string XmlRootName = "Settings";
        private const string XmlVersionAttribute = "Version";
        private const string XmlVersion = "1";
        private const string XmlValueName = "Value";
        private const string XmlNameAttribute = "Name";
        #endregion
    }
    #endregion
}
