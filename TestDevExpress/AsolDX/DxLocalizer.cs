// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Localization;

namespace Noris.Clients.Win.Components.AsolDX
{
    /// <summary>
    /// Controller pro lokalizace
    /// </summary>
    public class DxLocalizer : IDxLocalizerInternal
    {
        #region Singleton, instanční proměnné
        /// <summary>
        /// Instance singletonu
        /// </summary>
        protected static DxLocalizer Instance
        {
            get
            {
                if (__Instance == null)
                {
                    lock (__Lock)
                    {
                        if (__Instance == null)
                            __Instance = new DxLocalizer();
                    }
                }
                return __Instance;
            }
        }
        /// <summary>Úložiště instance</summary>
        private static DxLocalizer __Instance = null;
        /// <summary>Zámek singletonu</summary>
        private static object __Lock = new object();
        /// <summary>Konstruktor</summary>
        private DxLocalizer()
        {
            __Language = "CZ";
            __Enabled = false;
            __Translates = new Dictionary<string, Dictionary<string, TranslateInfo>>();
        }
        private string __Language;
        private bool __Enabled;
        private bool __RegisterLocalizingStrings;
        private bool __HighlightNonTranslated;
        #endregion
        #region Public static property: Language, Enabled

        public static bool Enabled { get { return Instance._Enabled; } set { Instance._Enabled = value; } }
        public static bool RegisterLocalizingStrings { get { return Instance.__RegisterLocalizingStrings; } set { Instance.__RegisterLocalizingStrings = value; } }
        public static bool HighlightNonTranslated { get { return Instance.__HighlightNonTranslated; } set { Instance.__HighlightNonTranslated = value; } }
        public static string Language { get { return Instance.__Language; } set { Instance.__Language = value; } }
        #endregion
        #region Private: aktivace / deaktivace, tvorba typových lokalizerů
        /// <summary>
        /// Hodnota Enabled, setování provede reálné zapnutí / vypnutí patřičných lokalizerů
        /// </summary>
        protected bool _Enabled
        {
            get { return __Enabled; }
            set
            {
                bool oldValue = _Enabled;
                bool newValue = value;
                if (newValue && !oldValue) _LocalizationEnable();
                if (!newValue && oldValue) _LocalizationDisable();
            }
        }
        private void _LocalizationEnable()
        {
            IDxLocalizerInternal owner = this;

            try
            {
                // From assembly: DevExpress.Data.v20.1.dll
                DevExpress.Data.Localization.CommonLocalizer.Active = GetLocalizer(DevExpress.Data.Localization.CommonLocalizer.Active, current => new DxLocalizerOne<DevExpress.Data.Localization.CommonStringId>(owner, current));
                DevExpress.Utils.Filtering.Internal.FilteringLocalizer.Active = GetLocalizer(DevExpress.Utils.Filtering.Internal.FilteringLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.Filtering.Internal.FilteringLocalizerStringId>(owner, current));
                DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.Active = GetLocalizer(DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizerStringId>(owner, current));
                DevExpress.XtraPrinting.Localization.PreviewLocalizer.Active = GetLocalizer(DevExpress.XtraPrinting.Localization.PreviewLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraPrinting.Localization.PreviewStringId>(owner, current));

                // From assembly: DevExpress.Dialogs.v20.1.Core.dll
                DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active = GetLocalizer(DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active, current => new DxLocalizerOne<DevExpress.Dialogs.Core.Localization.DialogsStringId>(owner, current));

                // From assembly: DevExpress.PivotGrid.v20.1.Core.dll
                DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.Active = GetLocalizer(DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraPivotGrid.Localization.PivotGridStringId>(owner, current));

                // From assembly: DevExpress.Utils.v20.1.dll
                DevExpress.Accessibility.AccLocalizer.Active = GetLocalizer(DevExpress.Accessibility.AccLocalizer.Active, current => new DxLocalizerOne<DevExpress.Accessibility.AccStringId>(owner, current));
                DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.Active = GetLocalizer(DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.DragDrop.Internal.DragDropLocalizerStringId>(owner, current));

                // From assembly: DevExpress.Utils.v20.1.UI.dll
                DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active = GetLocalizer(DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.UI.Localization.UtilsUIStringId>(owner, current));

                // From assembly: DevExpress.XtraBars.v20.1.dll
                DevExpress.XtraBars.Docking.DockManagerLocalizer.Active = GetLocalizer(DevExpress.XtraBars.Docking.DockManagerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraBars.Docking.DockManagerStringId>(owner, current));
                DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.Active = GetLocalizer(DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraBars.Docking2010.DocumentManagerStringId>(owner, current));

                // From assembly: DevExpress.XtraEditors.v20.1.dll
                DevExpress.XtraEditors.Controls.Localizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.Localizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));
                DevExpress.XtraEditors.FilterPanelLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.FilterPanelLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.FilterPanelLocalizerStringId>(owner, current));
                DevExpress.XtraEditors.ImageEditorLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.ImageEditorLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.ImageEditorLocalizerStringId>(owner, current));

                // From assembly: DevExpress.XtraGrid.v20.1.dll
                DevExpress.XtraGrid.Localization.GridLocalizer.Active = GetLocalizer(DevExpress.XtraGrid.Localization.GridLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGrid.Localization.GridStringId>(owner, current));
                DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.Active = GetLocalizer(DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGrid.Localization.EnumStringID>(owner, current));

                // From assembly: DevExpress.XtraCharts.v20.1.dll
                DevExpress.XtraCharts.Localization.ChartLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Localization.ChartLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Localization.ChartStringId>(owner, current));

                // From assembly: DevExpress.XtraCharts.v20.1.Wizard.dll
                DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId>(owner, current));

                // From assembly: DevExpress.XtraLayout.v20.1.dll
                DevExpress.XtraLayout.Localization.LayoutLocalizer.Active = GetLocalizer(DevExpress.XtraLayout.Localization.LayoutLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraLayout.Localization.LayoutStringId>(owner, current));

                // From assembly: DevExpress.XtraNavBar.v20.1.dll
                DevExpress.XtraNavBar.NavBarLocalizer.Active = GetLocalizer(DevExpress.XtraNavBar.NavBarLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraNavBar.NavBarStringId>(owner, current));

                // From assembly: DevExpress.XtraScheduler.v20.1.Core.Reporting.dll
                DevExpress.Accessibility.AccLocalizer.Active = GetLocalizer(DevExpress.Accessibility.AccLocalizer.Active, current => new DxLocalizerOne<DevExpress.Accessibility.AccStringId>(owner, current));
                DevExpress.XtraEditors.Controls.Localizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.Localizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));

                // From assembly: DevExpress.XtraTreeList.v20.1.dll
                DevExpress.XtraTreeList.Localization.TreeListLocalizer.Active = GetLocalizer(DevExpress.XtraTreeList.Localization.TreeListLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraTreeList.Localization.TreeListStringId>(owner, current));

                __Enabled = true;
            }
            catch { }
        }
        private void _LocalizationDisable()
        {
            try
            {
                // From assembly: DevExpress.Data.v20.1.dll
                DevExpress.Data.Localization.CommonLocalizer.Active = DevExpress.Data.Localization.CommonLocalizer.CreateDefaultLocalizer();
                DevExpress.Utils.Filtering.Internal.FilteringLocalizer.Active = DevExpress.Utils.Filtering.Internal.FilteringLocalizer.CreateDefaultLocalizer();
                DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.Active = DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.CreateDefaultLocalizer();
                DevExpress.XtraPrinting.Localization.PreviewLocalizer.Active = DevExpress.XtraPrinting.Localization.PreviewLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.Dialogs.v20.1.Core.dll
                DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active = DevExpress.Dialogs.Core.Localization.DialogsLocalizer.GetActiveLocalizerProvider().GetActiveLocalizer();

                // From assembly: DevExpress.PivotGrid.v20.1.Core.dll
                DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.Active = DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.GetActiveLocalizerProvider().GetActiveLocalizer();

                // From assembly: DevExpress.Utils.v20.1.dll
                DevExpress.Accessibility.AccLocalizer.Active = DevExpress.Accessibility.AccLocalizer.CreateDefaultLocalizer();
                DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.Active = DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.Utils.v20.1.UI.dll
                DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active = DevExpress.Utils.UI.Localization.UtilsUILocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraBars.v20.1.dll
                DevExpress.XtraBars.Docking.DockManagerLocalizer.Active = DevExpress.XtraBars.Docking.DockManagerLocalizer.CreateDefaultLocalizer();
                DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.Active = DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraEditors.v20.1.dll
                DevExpress.XtraEditors.Controls.Localizer.Active = DevExpress.XtraEditors.Controls.Localizer.CreateDefaultLocalizer();
                DevExpress.XtraEditors.FilterPanelLocalizer.Active = DevExpress.XtraEditors.FilterPanelLocalizer.CreateDefaultLocalizer();
                DevExpress.XtraEditors.ImageEditorLocalizer.Active = DevExpress.XtraEditors.ImageEditorLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraGrid.v20.1.dll
                DevExpress.XtraGrid.Localization.GridLocalizer.Active = DevExpress.XtraGrid.Localization.GridLocalizer.CreateDefaultLocalizer();
                DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.Active = DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraCharts.v20.1.dll
                DevExpress.XtraCharts.Localization.ChartLocalizer.Active = DevExpress.XtraCharts.Localization.ChartLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraCharts.v20.1.Wizard.dll
                DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraLayout.v20.1.dll
                DevExpress.XtraLayout.Localization.LayoutLocalizer.Active = DevExpress.XtraLayout.Localization.LayoutLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraNavBar.v20.1.dll
                DevExpress.XtraNavBar.NavBarLocalizer.Active = DevExpress.XtraNavBar.NavBarLocalizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraScheduler.v20.1.Core.Reporting.dll
                DevExpress.Accessibility.AccLocalizer.Active = DevExpress.Accessibility.AccLocalizer.CreateDefaultLocalizer();
                DevExpress.XtraEditors.Controls.Localizer.Active = DevExpress.XtraEditors.Controls.Localizer.CreateDefaultLocalizer();

                // From assembly: DevExpress.XtraTreeList.v20.1.dll
                DevExpress.XtraTreeList.Localization.TreeListLocalizer.Active = DevExpress.XtraTreeList.Localization.TreeListLocalizer.CreateDefaultLocalizer();

                __Enabled = false;
            }
            catch { }
        }
        private XtraLocalizer<T> GetLocalizer<T>(XtraLocalizer<T> current, Func<XtraLocalizer<T>, XtraLocalizer<T>> creator) where T : struct
        {
            if (current == null) return creator(null);
            if (current is DxLocalizerOne<T>) return current;
            return creator(current);
        }
        #endregion
        #region Střádání a tvorba překladů

        protected string GetLocalizedString(string localizerType, string idCode, string defString)
        {
            string localized = null;

            if (__Translates.TryGetValue(localizerType, out var localizerItems))
                if (localizerItems.TryGetValue(idCode, out var translateInfo))
                {
                    localized = translateInfo.Localized;
                    translateInfo?.AddOneUsed();
                }

            if (localized == null)
            {
                if (__HighlightNonTranslated)
                    localized = $"{defString} [{idCode} | {localizerType}]";
                else
                    localized = defString;
            }

            return localized;
        }

        protected void AddLocalizingDefStrings(string localizerType, IEnumerable<Tuple<string, string>> items)
        {
            Dictionary<string, TranslateInfo> localizerItems;
            if (!__Translates.TryGetValue(localizerType, out localizerItems))
            {
                localizerItems = new Dictionary<string, TranslateInfo>();
                __Translates.Add(localizerType, localizerItems);
            }
            foreach (var item in items)
            {
                string idCode = item.Item1;
                string defString = item.Item2;
                if (!localizerItems.TryGetValue(idCode, out var translateInfo))
                {
                    translateInfo = new TranslateInfo() { IdCode = idCode, Localized = null, DefString = defString };
                    localizerItems.Add(idCode, translateInfo);
                }
            }
        }
        private Dictionary<string, Dictionary<string, TranslateInfo>> __Translates;
        private class TranslateInfo
        {
            public override string ToString()
            {
                string localized = Localized ?? "NULL";
                return $"Code: {IdCode}, DefString: {DefString}, Localized: {localized}";
            }
            public string IdCode { get; set; }
            public string Localized { get; set; }
            /// <summary>
            /// Default text
            /// </summary>
            public string DefString { get; set; }
            /// <summary>
            /// Kolikrát jej systém použil (maximum = 20000)
            /// </summary>
            public int UsedCount { get; set; }
            public void AddOneUsed()
            {
                if (UsedCount < 20000)
                    UsedCount++;
            }
        }
        #endregion
        #region Private: Podpora pro mapování DevExpress DLL a vyhledání systémových lokalizerů
        /// <summary>
        /// V daném adresáři projde všechny soubory DLL, najde v nich typy
        /// </summary>
        /// <param name="path"></param>
        private void MapDlls(string path)
        {
            _MapPath = path;
            AppDomain.CurrentDomain.AssemblyResolve += _TestAssemblyResolve;

            string actives = "";
            string line;
            var files = System.IO.Directory.GetFiles(path, "*.dll").ToList();
            files.Sort();
            foreach (var file in files)
            {
                bool addAsm = true;

                var types = _MapTypesIn(file);
                if (types == null)
                {
                    // line = "  ==>  " + file + "          LOADING ERROR ";
                    // actives += line + Environment.NewLine;
                    continue;
                }

                foreach (var type in types)
                {
                    bool isClass = (type.IsClass && !type.IsAbstract);
                    if (!isClass) continue;                                              // Nechci ani struct, ani interface, ani abstract class

                    var property = type.GetProperty("Active");
                    if (property == null) continue;                                      // Musí mít property Active
                    Type propertyType = property.PropertyType;

                    string propertyName = property.Name;
                    string propertyTypeName = propertyType.Name;
                    if (!propertyTypeName.StartsWith("XtraLocalizer")) continue;         //  ... typu odvozeného od XtraLocalizer

                    // Tohle už mě zajímá:
                    if (addAsm)
                    {
                        line = Environment.NewLine + " // From assembly: " + System.IO.Path.GetFileName(file);
                        actives += line + Environment.NewLine;
                        addAsm = false;
                    }

                    var typeName = type.FullName;
                    var propertyTypeGenericTypes = propertyType.GetGenericArguments();
                    if (propertyTypeGenericTypes.Length != 1)
                    {
                        line = $"// {typeName}.{propertyName} is type: {propertyTypeName}, it does not have exactly one generic argument.";
                        actives += line + Environment.NewLine;
                        continue;
                    }

                    string genericTypeName = propertyTypeGenericTypes[0].FullName;

                    line = $"{propertyTypeName}<{genericTypeName}> {typeName}.{propertyName};";
                    string code = $"{typeName}.{propertyName} = GetLocalizer({typeName}.{propertyName}, current => new DxLocalizer<{genericTypeName}>(owner, current));";
                    actives += code + Environment.NewLine;

                    continue;

                    object target = property.GetValue(null);  // Static property
                    System.Reflection.MethodInfo mi = target.GetType().GetMethod("CreateXmlDocument");
                    object result = mi.Invoke(target, null);
                    XtraLocalizer xtraLocalizer = target as XtraLocalizer;
                }
            }

            System.Windows.Forms.Clipboard.SetText(actives);

            AppDomain.CurrentDomain.AssemblyResolve -= _TestAssemblyResolve;
        }
        private List<Type> _MapTypesIn(string assemblyFile)
        {
            try
            {
                var assem = System.Reflection.Assembly.LoadFile(assemblyFile);
                var types = assem.GetTypes().ToList();
                types.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                return types;
            }
            catch
            {
                return null;
            }
        }
        private System.Reflection.Assembly _TestAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
            System.Reflection.Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Try to load by filename - split out the filename of the full assembly name
            // and append the base path of the original assembly (ie. look in the same dir)
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();

            string asmFile = System.IO.Path.Combine(_MapPath, filename);

            try
            {
                return System.Reflection.Assembly.LoadFrom(asmFile);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private string _MapPath;

        private void _LocalizationEnableFullSample()
        {
            return;
            //  MapDlls(@"c:\CSharp\DevExpressDll\DX20.1\");

            /*

            Tohle nechodí:

            DevExpress.XtraBars.Localization.BarLocalizer.QueryLocalizedString += BarLocalizer_QueryLocalizedString;

            DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedString += XtraLocalizer_QueryLocalizedString;
            DevExpress.Utils.Localization.XtraLocalizer<DevExpress.XtraEditors.Controls.StringId>.QueryLocalizedString += XtraLocalizerG_QueryLocalizedString;
            DevExpress.Utils.Localization.XtraLocalizer<DevExpress.XtraBars.Localization.BarString>.QueryLocalizedString += XtraLocalizerG_QueryLocalizedString;

            DevExpress.XtraBars.Localization.BarLocalizer.QueryLocalizedString += BarLocalizer_QueryLocalizedString;
            DevExpress.XtraBars.Localization.BarResLocalizer.QueryLocalizedString += BarResLocalizer_QueryLocalizedString;
            */



            // Tohle chodí:
            /*
            DevExpress.XtraBars.Localization.BarLocalizer.Active = LocalizerBars.Localizer;

            DevExpress.XtraEditors.Controls.Localizer.Active = LocalizerControls.Localizer;
            DevExpress.XtraBars.Localization.BarLocalizer.Active = LocalizerBars.Localizer;
            DevExpress.XtraCharts.Localization.ChartLocalizer.Active = LocalizerChart.Localizer;
            DevExpress.XtraCharts.Localization.ChartResLocalizer.Active = LocalizerChartRes.Localizer;
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = LocalizerChartDesigner.Localizer;
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer.Active = LocalizerChartDesignerRes.Localizer;
            */
            // Pokus:

            IDxLocalizerInternal owner = this;

            /*   OK :
            DevExpress.XtraEditors.Controls.Localizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.Localizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));
            DevExpress.Accessibility.AccLocalizer.Active = GetLocalizer(DevExpress.Accessibility.AccLocalizer.Active, current => new DxLocalizerOne<DevExpress.Accessibility.AccStringId>(owner, current));
            //   null  DevExpress.Data.Localization.CommonResLocalizer.Active = new DxLocalizerOne<DevExpress.Data.Localization.CommonStringId>();
            DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active = GetLocalizer(DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active, current => new DxLocalizerOne<DevExpress.Dialogs.Core.Localization.DialogsStringId>(owner, current));
            DevExpress.Dialogs.Core.Localization.DialogsResXLocalizer.Active = GetLocalizer(DevExpress.Dialogs.Core.Localization.DialogsResXLocalizer.Active, current => new DxLocalizerOne<DevExpress.Dialogs.Core.Localization.DialogsStringId>(owner, current));
            //   null  DevExpress.Charts.Designer.Native.ChartDesignerLocalizer.Active = new DxLocalizerOne<DevExpress.Charts.Designer.Native.ChartDesignerStringIDs>();


            DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId>(owner, current));
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId>(owner, current));
            DevExpress.XtraCharts.Localization.ChartLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Localization.ChartLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Localization.ChartStringId>(owner, current));
            DevExpress.XtraCharts.Localization.ChartResLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Localization.ChartResLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Localization.ChartStringId>(owner, current));
            DevExpress.XtraNavBar.NavBarLocalizer.Active = GetLocalizer(DevExpress.XtraNavBar.NavBarLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraNavBar.NavBarStringId>(owner, current));
            DevExpress.XtraNavBar.NavBarResLocalizer.Active = GetLocalizer(DevExpress.XtraNavBar.NavBarResLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraNavBar.NavBarStringId>(owner, current));

            DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active = GetLocalizer(DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.UI.Localization.UtilsUIStringId>(owner, current));

            DevExpress.XtraEditors.Controls.EditResLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.EditResLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));

            DevExpress.XtraEditors.ImageEditorLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.ImageEditorLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.ImageEditorLocalizerStringId>(owner, current));
            */



            // From assembly: DevExpress.Data.v20.1.dll
            DevExpress.Data.Localization.CommonLocalizer.Active = GetLocalizer(DevExpress.Data.Localization.CommonLocalizer.Active, current => new DxLocalizerOne<DevExpress.Data.Localization.CommonStringId>(owner, current));
            DevExpress.Data.Utils.ProcessStartConfirmationLocalizer.Active = GetLocalizer(DevExpress.Data.Utils.ProcessStartConfirmationLocalizer.Active, current => new DxLocalizerOne<DevExpress.Data.Utils.ProcessStartConfirmationStringId>(owner, current));
            DevExpress.Utils.Filtering.Internal.FilteringLocalizer.Active = GetLocalizer(DevExpress.Utils.Filtering.Internal.FilteringLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.Filtering.Internal.FilteringLocalizerStringId>(owner, current));
            DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.Active = GetLocalizer(DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.Filtering.Internal.FilterUIElementLocalizerStringId>(owner, current));
            DevExpress.XtraPrinting.Localization.PreviewLocalizer.Active = GetLocalizer(DevExpress.XtraPrinting.Localization.PreviewLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraPrinting.Localization.PreviewStringId>(owner, current));

            // From assembly: DevExpress.Diagram.v20.1.Core.dll
            //  DevExpress.Diagram.Core.Localization.DiagramControlLocalizer.Active = GetLocalizer(DevExpress.Diagram.Core.Localization.DiagramControlLocalizer.Active, current => new DxLocalizerOne<DevExpress.Diagram.Core.Localization.DiagramControlStringId>(owner, current));

            // From assembly: DevExpress.Dialogs.v20.1.Core.dll
            DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active = GetLocalizer(DevExpress.Dialogs.Core.Localization.DialogsLocalizer.Active, current => new DxLocalizerOne<DevExpress.Dialogs.Core.Localization.DialogsStringId>(owner, current));

            // From assembly: DevExpress.Map.v20.1.Core.dll
            //  DevExpress.Map.Localization.MapLocalizer.Active = GetLocalizer(DevExpress.Map.Localization.MapLocalizer.Active, current => new DxLocalizerOne<DevExpress.Map.Localization.MapStringId>(owner, current));

            // From assembly: DevExpress.Pdf.v20.1.Core.dll
            //  DevExpress.Pdf.Localization.PdfCoreLocalizer.Active = GetLocalizer(DevExpress.Pdf.Localization.PdfCoreLocalizer.Active, current => new DxLocalizerOne<DevExpress.Pdf.Localization.PdfCoreStringId>(owner, current));

            // From assembly: DevExpress.PivotGrid.v20.1.Core.dll
            //  DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.Active = GetLocalizer(DevExpress.XtraPivotGrid.Localization.PivotGridLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraPivotGrid.Localization.PivotGridStringId>(owner, current));

            // From assembly: DevExpress.Snap.v20.1.Core.dll
            //  DevExpress.Snap.Localization.SnapLocalizer.Active = GetLocalizer(DevExpress.Snap.Localization.SnapLocalizer.Active, current => new DxLocalizerOne<DevExpress.Snap.Localization.SnapStringId>(owner, current));

            // From assembly: DevExpress.Snap.v20.1.Extensions.dll
            //  DevExpress.Snap.Extensions.Localization.SnapExtensionsLocalizer.Active = GetLocalizer(DevExpress.Snap.Extensions.Localization.SnapExtensionsLocalizer.Active, current => new DxLocalizerOne<DevExpress.Snap.Extensions.Localization.SnapExtensionsStringId>(owner, current));

            // From assembly: DevExpress.Sparkline.v20.1.Core.dll
            //  DevExpress.Sparkline.Localization.SparklineLocalizer.Active = GetLocalizer(DevExpress.Sparkline.Localization.SparklineLocalizer.Active, current => new DxLocalizerOne<DevExpress.Sparkline.Localization.SparklineStringId>(owner, current));

            // From assembly: DevExpress.Utils.v20.1.dll
            DevExpress.Accessibility.AccLocalizer.Active = GetLocalizer(DevExpress.Accessibility.AccLocalizer.Active, current => new DxLocalizerOne<DevExpress.Accessibility.AccStringId>(owner, current));
            //            DevExpress.Utils.Controls.SvgImageBox.SvgImageBoxLocalizer.Active = GetLocalizer(DevExpress.Utils.Controls.SvgImageBox.SvgImageBoxLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.Controls.SvgImageBox.StringId>(owner, current));
            DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.Active = GetLocalizer(DevExpress.Utils.DragDrop.Internal.DragDropLocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.DragDrop.Internal.DragDropLocalizerStringId>(owner, current));

            // From assembly: DevExpress.Utils.v20.1.UI.dll
            DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active = GetLocalizer(DevExpress.Utils.UI.Localization.UtilsUILocalizer.Active, current => new DxLocalizerOne<DevExpress.Utils.UI.Localization.UtilsUIStringId>(owner, current));

            // From assembly: DevExpress.XtraBars.v20.1.dll
            DevExpress.XtraBars.Docking.DockManagerLocalizer.Active = GetLocalizer(DevExpress.XtraBars.Docking.DockManagerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraBars.Docking.DockManagerStringId>(owner, current));
            DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.Active = GetLocalizer(DevExpress.XtraBars.Docking2010.DocumentManagerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraBars.Docking2010.DocumentManagerStringId>(owner, current));

            // From assembly: DevExpress.XtraEditors.v20.1.dll
            DevExpress.XtraEditors.Controls.Localizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.Localizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));
            DevExpress.XtraEditors.FilterPanelLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.FilterPanelLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.FilterPanelLocalizerStringId>(owner, current));
            DevExpress.XtraEditors.ImageEditorLocalizer.Active = GetLocalizer(DevExpress.XtraEditors.ImageEditorLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.ImageEditorLocalizerStringId>(owner, current));

            // From assembly: DevExpress.XtraGantt.v20.1.dll
            //  DevExpress.XtraGantt.Localization.GanttLocalizer.Active = GetLocalizer(DevExpress.XtraGantt.Localization.GanttLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGantt.Localization.GanttStringId>(owner, current));

            // From assembly: DevExpress.XtraGauges.v20.1.Core.dll
            //  DevExpress.XtraGauges.Core.Localization.GaugesCoreLocalizer.Active = GetLocalizer(DevExpress.XtraGauges.Core.Localization.GaugesCoreLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGauges.Core.Localization.GaugesCoreStringId>(owner, current));

            // From assembly: DevExpress.XtraGauges.v20.1.Presets.dll
            //  DevExpress.XtraGauges.Presets.Localization.GaugesPresetsLocalizer.Active = GetLocalizer(DevExpress.XtraGauges.Presets.Localization.GaugesPresetsLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGauges.Presets.Localization.GaugesPresetsStringId>(owner, current));

            // From assembly: DevExpress.XtraGrid.v20.1.dll
            DevExpress.XtraGrid.Localization.GridLocalizer.Active = GetLocalizer(DevExpress.XtraGrid.Localization.GridLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGrid.Localization.GridStringId>(owner, current));
            DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.Active = GetLocalizer(DevExpress.XtraGrid.Localization.LayoutViewEnumLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraGrid.Localization.EnumStringID>(owner, current));

            // From assembly: DevExpress.XtraCharts.v20.1.dll
            DevExpress.XtraCharts.Localization.ChartLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Localization.ChartLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Localization.ChartStringId>(owner, current));

            // From assembly: DevExpress.XtraCharts.v20.1.Wizard.dll
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = GetLocalizer(DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId>(owner, current));

            // From assembly: DevExpress.XtraLayout.v20.1.dll
            DevExpress.XtraLayout.Localization.LayoutLocalizer.Active = GetLocalizer(DevExpress.XtraLayout.Localization.LayoutLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraLayout.Localization.LayoutStringId>(owner, current));

            // From assembly: DevExpress.XtraNavBar.v20.1.dll
            DevExpress.XtraNavBar.NavBarLocalizer.Active = GetLocalizer(DevExpress.XtraNavBar.NavBarLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraNavBar.NavBarStringId>(owner, current));

            // From assembly: DevExpress.XtraPdfViewer.v20.1.dll
            //  DevExpress.XtraPdfViewer.Localization.XtraPdfViewerLocalizer.Active = GetLocalizer(DevExpress.XtraPdfViewer.Localization.XtraPdfViewerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraPdfViewer.Localization.XtraPdfViewerStringId>(owner, current));

            // From assembly: DevExpress.XtraReports.v20.1.Extensions.dll
            //  DevExpress.XtraReports.ReportGeneration.Wizard.Localization.ReportGeneratorWizardLocalizer.Active = GetLocalizer(DevExpress.XtraReports.ReportGeneration.Wizard.Localization.ReportGeneratorWizardLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraReports.ReportGeneration.Wizard.Localization.ReportGeneratorWizardStringId>(owner, current));
            //  DevExpress.XtraReports.Wizards.Localization.ReportDesignerLocalizer.Active = GetLocalizer(DevExpress.XtraReports.Wizards.Localization.ReportDesignerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraReports.Wizards.Localization.ReportBoxDesignerStringId>(owner, current));

            // From assembly: DevExpress.XtraScheduler.v20.1.Core.dll
            //  DevExpress.XtraScheduler.Accessibility.AccSchedulerLocalizer.Active = GetLocalizer(DevExpress.XtraScheduler.Accessibility.AccSchedulerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraScheduler.Accessibility.AccSchedulerStringId>(owner, current));
            //  DevExpress.XtraScheduler.Localization.SchedulerLocalizer.Active = GetLocalizer(DevExpress.XtraScheduler.Localization.SchedulerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraScheduler.Localization.SchedulerStringId>(owner, current));

            // From assembly: DevExpress.XtraScheduler.v20.1.Core.Reporting.dll
            DevExpress.Accessibility.AccLocalizer.Active = GetLocalizer(DevExpress.Accessibility.AccLocalizer.Active, current => new DxLocalizerOne<DevExpress.Accessibility.AccStringId>(owner, current));
            DevExpress.XtraEditors.Controls.Localizer.Active = GetLocalizer(DevExpress.XtraEditors.Controls.Localizer.Active, current => new DxLocalizerOne<DevExpress.XtraEditors.Controls.StringId>(owner, current));

            // From assembly: DevExpress.XtraScheduler.v20.1.Extensions.dll
            //  DevExpress.XtraScheduler.Localization.SchedulerExtensionsLocalizer.Active = GetLocalizer(DevExpress.XtraScheduler.Localization.SchedulerExtensionsLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraScheduler.Localization.SchedulerExtensionsStringId>(owner, current));

            // From assembly: DevExpress.XtraSpellChecker.v20.1.dll
            //  DevExpress.XtraSpellChecker.Localization.SpellCheckerLocalizer.Active = GetLocalizer(DevExpress.XtraSpellChecker.Localization.SpellCheckerLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraSpellChecker.Localization.SpellCheckerStringId>(owner, current));

            // From assembly: DevExpress.XtraTreeList.v20.1.dll
            DevExpress.XtraTreeList.Localization.TreeListLocalizer.Active = GetLocalizer(DevExpress.XtraTreeList.Localization.TreeListLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraTreeList.Localization.TreeListStringId>(owner, current));

            // From assembly: DevExpress.XtraVerticalGrid.v20.1.dll
            //  DevExpress.XtraVerticalGrid.Localization.VGridLocalizer.Active = GetLocalizer(DevExpress.XtraVerticalGrid.Localization.VGridLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraVerticalGrid.Localization.VGridStringId>(owner, current));

            // From assembly: DevExpress.XtraWizard.v20.1.dll
            //  DevExpress.XtraWizard.Localization.WizardLocalizer.Active = GetLocalizer(DevExpress.XtraWizard.Localization.WizardLocalizer.Active, current => new DxLocalizerOne<DevExpress.XtraWizard.Localization.WizardStringId>(owner, current));


            __Enabled = true;
        }

        #endregion
        #region IDxLocalizerInternal implementace
        bool IDxLocalizerInternal.RegisterLocalizingStrings { get { return __RegisterLocalizingStrings; } }
        string IDxLocalizerInternal.GetLocalizedString(string localizerType, string idCode, string defString) { return GetLocalizedString(localizerType, idCode, defString); }
        void IDxLocalizerInternal.AddLocalizingDefStrings(string localizerType, IEnumerable<Tuple<string, string>> items) { AddLocalizingDefStrings(localizerType, items); }
        #endregion
    }
    #region interface IDxLocalizerInternal
    /// <summary>
    /// Interface pro interní přístup k instanci <see cref="DxLocalizer"/>
    /// </summary>
    internal interface IDxLocalizerInternal
    {
        bool RegisterLocalizingStrings { get; }
        string GetLocalizedString(string localizerType, string idCode, string defString);
        void AddLocalizingDefStrings(string localizerType, IEnumerable<Tuple<string, string>> items);
    }
    #endregion
    #region class DxLocalizerOne<T> : generická třída pro jeden lokalizer
    /// <summary>
    /// DxLocalizerOne : generická třída, která tvoří Lokalizer pro jednotlivé implementace
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DxLocalizerOne<T> : XtraLocalizer<T> where T : struct
    {
        /// <summary>
        /// Konstruktor, pro danou instanci 
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="sysLocalizer"></param>
        internal DxLocalizerOne(IDxLocalizerInternal owner, XtraLocalizer<T> sysLocalizer)
        {
            LocalizerType = typeof(T).FullName;
            _Owner = owner;
            _SysLocalizer = sysLocalizer;
            _ScanThisLocalization();
        }
        IDxLocalizerInternal _Owner;
        XtraLocalizer<T> _SysLocalizer;
        public override string Language { get { return DxLocalizer.Language; } }
        public string LocalizerType { get; private set; }
        public override string GetLocalizedString(T id)
        {
            string idCode = id.ToString();
            string defString = (_SysLocalizer != null ? _SysLocalizer.GetLocalizedString(id) : idCode);
            return _Owner.GetLocalizedString(LocalizerType, idCode, defString);
        }
        public override XtraLocalizer<T> CreateResXLocalizer()
        {
            return this;
        }
        protected override void PopulateStringTable()
        {
        }
        protected override void CreateStringTable()
        {
            if (_SysLocalizer != null)
            {
                var xml = _SysLocalizer.CreateXmlDocument();
            }
        }
        protected void _ScanThisLocalization()
        {
            if (_SysLocalizer == null || _Owner == null) return;
            if (!_Owner.RegisterLocalizingStrings) return;

            // var xml = _SysLocalizer.CreateXmlDocument();

            List<Tuple<string, string>> items = new List<Tuple<string, string>>();
            Array values = Enum.GetValues(typeof(T));
            foreach (T value in values)
            {
                var text = _SysLocalizer.GetLocalizedString(value);
                items.Add(new Tuple<string, string>(value.ToString(), text));
            }

            _Owner.AddLocalizingDefStrings(LocalizerType, items);
        }
    }
    #endregion
}
