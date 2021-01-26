using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Djs.Tools.CovidGraphs.Data
{
    public class Localization
    {
        public static bool Enabled
        {
            get { return _Enabled; }
            set
            {
                bool oldValue = _Enabled;
                bool newValue = value;
                if (newValue && !oldValue) LocalizationEnable();
                if (!newValue && oldValue) LocalizationDisable();
            }
        }
        private static bool _Enabled;
        private static void LocalizationEnable()
        {
            return;

            /*
            DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedString += XtraLocalizer_QueryLocalizedString;
            DevExpress.Utils.Localization.XtraLocalizer<DevExpress.XtraEditors.Controls.StringId>.QueryLocalizedString += XtraLocalizerG_QueryLocalizedString;
            DevExpress.Utils.Localization.XtraLocalizer<DevExpress.XtraBars.Localization.BarString>.QueryLocalizedString += XtraLocalizerG_QueryLocalizedString;

            DevExpress.XtraBars.Localization.BarLocalizer.QueryLocalizedString += BarLocalizer_QueryLocalizedString;
            DevExpress.XtraBars.Localization.BarResLocalizer.QueryLocalizedString += BarResLocalizer_QueryLocalizedString;
            */



            // Tohle chodí:
            DevExpress.XtraBars.Localization.BarLocalizer.Active = LocalizerBars.Localizer;

            // Tohle nechodí:
            //   DevExpress.XtraBars.Localization.BarLocalizer.QueryLocalizedString += BarLocalizer_QueryLocalizedString;




            /*
            DevExpress.XtraEditors.Controls.Localizer.Active = LocalizerControls.Localizer;
            DevExpress.XtraBars.Localization.BarLocalizer.Active = LocalizerBars.Localizer;
            DevExpress.XtraCharts.Localization.ChartLocalizer.Active = LocalizerChart.Localizer;
            DevExpress.XtraCharts.Localization.ChartResLocalizer.Active = LocalizerChartRes.Localizer;
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = LocalizerChartDesigner.Localizer;
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer.Active = LocalizerChartDesignerRes.Localizer;
            */
            _Enabled = true;
        }

        private static void BarLocalizer_QueryLocalizedString(object sender, DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedStringEventArgs e)
        {
            e.Value = "[Lokalizováno BL]";
        }

        private static void BarResLocalizer_QueryLocalizedString(object sender, DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedStringEventArgs e)
        {
            e.Value = "[Lokalizováno BRL]";
        }

        private static void XtraLocalizerG_QueryLocalizedString(object sender, DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedStringEventArgs e)
        {
            e.Value = "[Lokalizováno S]";
        }

        private static void XtraLocalizer_QueryLocalizedString(object sender, DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedStringEventArgs e)
        {
            e.Value = "[Lokalizováno]";
        }

        private static void LocalizationDisable()
        {
            return;

            DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedString -= XtraLocalizer_QueryLocalizedString;
            /*
            DevExpress.XtraEditors.Controls.Localizer.Active = DevExpress.XtraEditors.Controls.Localizer.CreateDefaultLocalizer();
            DevExpress.XtraBars.Localization.BarLocalizer.Active = new DevExpress.XtraBars.Localization.BarLocalizer();
            DevExpress.XtraCharts.Localization.ChartLocalizer.Active = DevExpress.XtraCharts.Localization.ChartLocalizer.CreateDefaultLocalizer();
            DevExpress.XtraCharts.Localization.ChartResLocalizer.Active = new DevExpress.XtraCharts.Localization.ChartResLocalizer();
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer.Active = new DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer();
            DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer.Active = new DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer();
            */
            _Enabled = false;
        }

        public class LocalizerControls : DevExpress.XtraEditors.Controls.Localizer
        {
            public static LocalizerControls Localizer { get { return new LocalizerControls(); } }
            private LocalizerControls()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraEditors.Controls.StringId id)
            {
                string text = base.GetLocalizedString(id);
                text = "[Control : není přeloženo]";
                return text;
            }
        }
        public class LocalizerBars : DevExpress.XtraBars.Localization.BarLocalizer
        {
            public static LocalizerBars Localizer { get { return new LocalizerBars(); } }
            private LocalizerBars()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraBars.Localization.BarString id)
            {
                string text = base.GetLocalizedString(id);
                text = "[Bars : není přeloženo]";
                return text;
            }
        }
        public class LocalizerChart : DevExpress.XtraCharts.Localization.ChartLocalizer
        {
            public static LocalizerChart Localizer { get { return new LocalizerChart(); } }
            private LocalizerChart()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraCharts.Localization.ChartStringId id)
            {
                string text = base.GetLocalizedString(id);
                text = "[Chart : není přeloženo]";
                return text;
            }
        }
        public class LocalizerChartRes : DevExpress.XtraCharts.Localization.ChartResLocalizer
        {
            public static LocalizerChartRes Localizer { get { return new LocalizerChartRes(); } }
            private LocalizerChartRes()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraCharts.Localization.ChartStringId id)
            {
                string text = base.GetLocalizedString(id);
                text = "[ChartRes : není přeloženo]";
                return text;
            }
            protected override string GetLocalizedStringCore(DevExpress.XtraCharts.Localization.ChartStringId id)
            {
                string text = base.GetLocalizedStringCore(id);
                text = "[ChartRes.Core : není přeloženo]";
                return text;
            }
            protected override string GetEnumTypeName()
            {
                string text = base.GetEnumTypeName();
                text = "[ChartRes.Enum : není přeloženo]";
                return text;
            }
        }
        public class LocalizerChartDesigner : DevExpress.XtraCharts.Designer.Localization.ChartDesignerLocalizer
        {
            public static LocalizerChartDesigner Localizer { get { return new LocalizerChartDesigner(); } }
            private LocalizerChartDesigner()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId id)
            {
                string text = base.GetLocalizedString(id);
                text = "[ChartDesigner : není přeloženo]";
                return text;
            }
            protected override string GetEnumTypeName()
            {
                string text = base.GetEnumTypeName();
                text = "[ChartDesigner.Enum : není přeloženo]";
                return text;
            }
        }
        public class LocalizerChartDesignerRes : DevExpress.XtraCharts.Designer.Localization.ChartDesignerResLocalizer
        {
            public static LocalizerChartDesignerRes Localizer { get { return new LocalizerChartDesignerRes(); } }
            private LocalizerChartDesignerRes()
            {
                base.CreateStringTable();
            }
            public override string Language { get { return "CZ"; } }
            public override string GetLocalizedString(DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId id)
            {
                string text = base.GetLocalizedString(id);
                text = "[ChartDesignerRes : není přeloženo]";
                return text;
            }
            protected override string GetLocalizedStringCore(DevExpress.XtraCharts.Designer.Localization.ChartDesignerStringId id)
            {
                string text = base.GetLocalizedStringCore(id);
                text = "[ChartDesignerRes.Core : není přeloženo]";
                return text;
            }
            protected override string GetEnumTypeName()
            {
                string text = base.GetEnumTypeName();
                text = "[ChartDesignerRes.Enum : není přeloženo]";
                return text;
            }
        }

        /*   Nalezené lokalizační namespace:
         DevExpress.Data.Localization
         DevExpress.Dialogs.Core.Localization



        
        
        
        */

    }
}
