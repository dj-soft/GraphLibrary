// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

using XB = DevExpress.XtraBars;
using XR = DevExpress.XtraBars.Ribbon;
using DM = DevExpress.Utils.Menu;
using DS = DevExpress.Skins;
using DXN = DevExpress.XtraBars.Navigation;
using DXT = DevExpress.XtraTabbedMdi;

using NWC = Noris.Clients.Win.Components;
using TestDevExpress.Components;
using Noris.Clients.Win.Components.AsolDX;
using DevExpress.XtraRichEdit.Import.OpenXml;
using DevExpress.XtraRichEdit.Layout;

using Noris.Clients.Win.Components;
using Noris.WS.DataContracts.Desktop.Data;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Hlavní okno testovací aplikace
    /// </summary>
    public partial class MainForm : DxRibbonForm
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainForm()
        {
            ApplicationState.DesktopForm = this;              // ApplicationState si ukládá WeakReferenci, a sám si hlídá eventy o změně stavu okna.

            DxComponent.SplashUpdate(rightFooter: "Příprava...");

            InitializeComponent();
            InitData();

            DxLocalizer.Enabled = false;
            DxLocalizer.HighlightNonTranslated = true;

            InitBarManager();

            InitTabPages();

            InitPopupPage();           // 0
            InitTabHeaders();          // 1
            InitSplitters();           // 2
            InitAnimation();           // 3
            InitResize();              // 4
            InitChart();               // 5
            InitMsgBox();              // 6
            InitStepProgress();        // 7
            InitEditors();             // 8
            InitSvgIcons();            // 9
            InitTreeView();            // 10
            InitDragDrop();            // 11
            InitSyntaxEditor();        // 12

            // TestResources();

            DxComponent.SplashUpdate(subTitle: "A je to!");

            this.Disposed += MainForm_Disposed;
            DxComponent.LogTextChanged += DxComponent_LogTextChanged;

            this.ApplyStyle();

            _NotifyMessageInit();

            ActivatePage(7, true);
            // ActivatePage(10, true);

        }
        private void MainForm_Disposed(object sender, EventArgs e)
        {
            DxComponent.LogTextChanged -= DxComponent_LogTextChanged;

            // DAJ 0070458: tohle běžně nastavuje eventhandler WDesktop.Closed, odchycený v ApplicationState (tam je WDesktop zaháčkový už z jeho konstruktoru).
            // Ale při Automatickém ukončení klienta se vyvolá rovnou WDesktop.Dispose() - aby se obešla celá logika řádného ukončení klienta (ukládání změn v oknech).
            //  Viz: WinForms.Host\Windows\WUserSessionTimeout.cs  =>  private void _FinalCountDown()
            // Aby byl stav aplikace řádně evidován, nastavíme nyní stav Closed, a v base.Dispose() proběhne event Disposed, který v ApplicationState ukončí evidenci WDesktopu:
            //  Pokud probíhá normální konec klienta, pak následující řádek nic nezkazí, protože aplikační stav už nyní je Closed.
            Noris.Clients.Win.Components.ApplicationState.DesktopFormState = ApplicationFormStateType.Closed;
        }
        protected override void OnFirstShownAfter()
        {
            base.OnFirstShownAfter();
            ApplyRibbonSvgImagesResult(false);
        }
        protected override void OnStyleChanged()
        {
            base.OnStyleChanged();
            this.ApplyStyle();
        }
        private void ApplyStyle()
        {
            /*
            var color1 = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_WindowText);
            var color2 = DxComponent.GetSkinColor(SkinElementColor.CommonSkins_Control);

            Rectangle bounds = new Rectangle(850, 500, 1900, 600);
            Rectangle bounds2 = bounds.FitIntoMonitors();
            */
        }
        private void InitData()
        {
            DxComponent.ClipboardApplicationId = "TestDevExpress";
            this.Text = $"TestDevExpress :: {DxComponent.FrameworkName}";
            this._SysSvgImages = new string[]
              {
                "svgimages/spreadsheet/100percent.svg",
                "svgimages/spreadsheet/3arrowscolored.svg",
                "svgimages/spreadsheet/3arrowsgray.svg",
                "svgimages/spreadsheet/3flags.svg",
                "svgimages/spreadsheet/3signs.svg",
                "svgimages/spreadsheet/3stars.svg",
                "svgimages/spreadsheet/3symbolscircled.svg",
                "svgimages/spreadsheet/3symbolsuncircled.svg",
                "svgimages/spreadsheet/3trafficlights.svg",
                "svgimages/spreadsheet/3trafficlightsrimmed.svg",
                "svgimages/spreadsheet/4arrowscolored.svg",
                "svgimages/spreadsheet/4arrowsgray.svg",
                "svgimages/spreadsheet/4ratings.svg",
                "svgimages/spreadsheet/4trafficlights.svg",
                "svgimages/spreadsheet/5arrowscolored.svg",
                "svgimages/spreadsheet/5arrowsgray.svg",
                "svgimages/spreadsheet/5boxes.svg",
                "svgimages/spreadsheet/5quarters.svg",
                "svgimages/spreadsheet/5ratings.svg",
                "svgimages/spreadsheet/above%20average.svg",
                "svgimages/spreadsheet/accounting.svg",
                "svgimages/spreadsheet/accountingnumberformat.svg",
                "svgimages/spreadsheet/adateoccuring.svg",
                "svgimages/spreadsheet/adddatasource.svg",
                "svgimages/spreadsheet/alignright.svg",
                "svgimages/spreadsheet/allborders.svg",
                "svgimages/spreadsheet/allowuserstoeditranges.svg",
                "svgimages/spreadsheet/area.svg",
                "svgimages/spreadsheet/autosum.svg",
                "svgimages/spreadsheet/bar.svg",
                "svgimages/spreadsheet/belowaverage.svg",
                "svgimages/spreadsheet/between.svg",
                "svgimages/spreadsheet/bluedatabargradient.svg",
                "svgimages/spreadsheet/bluedatabarsolid.svg",
                "svgimages/spreadsheet/bluewhiteredcolorscale.svg",
                "svgimages/spreadsheet/bold.svg",
                "svgimages/spreadsheet/bottom10items.svg",
                "svgimages/spreadsheet/bottom10percent.svg",
                "svgimages/spreadsheet/bottomalign.svg",
                "svgimages/spreadsheet/bottomborder.svg",
                "svgimages/spreadsheet/bottomdoubleborder.svg",
                "svgimages/spreadsheet/bringforward.svg",
                "svgimages/spreadsheet/bringtofront.svg",
                "svgimages/spreadsheet/calculatenow.svg",
                "svgimages/spreadsheet/calculatesheet.svg",
                "svgimages/spreadsheet/calculationoptions.svg",
                "svgimages/spreadsheet/circleinvaliddata.svg",
                "svgimages/spreadsheet/clearall.svg",
                "svgimages/spreadsheet/clearfilter.svg",
                "svgimages/spreadsheet/clearformats.svg",
                "svgimages/spreadsheet/clearhyperlinks.svg",
                "svgimages/spreadsheet/clearpivottable.svg",
                "svgimages/spreadsheet/clearrules.svg",
                "svgimages/spreadsheet/clearvalidationcircles.svg",
                "svgimages/spreadsheet/collapsefieldpivottable.svg",
                "svgimages/spreadsheet/collated.svg",
                "svgimages/spreadsheet/column.svg",
                "svgimages/spreadsheet/commastyle.svg",
                "svgimages/spreadsheet/conditionalformatting.svg",
                "svgimages/spreadsheet/copy.svg",
                "svgimages/spreadsheet/create%20rotated%20bar%20chart.svg",
                "svgimages/spreadsheet/createarea3dchart.svg",
                "svgimages/spreadsheet/createareachart.svg",
                "svgimages/spreadsheet/createbar3dchart.svg",
                "svgimages/spreadsheet/createbarchart.svg",
                "svgimages/spreadsheet/createbubble3dchart.svg",
                "svgimages/spreadsheet/createbubblechart.svg",
                "svgimages/spreadsheet/createconebar3dchart.svg",
                "svgimages/spreadsheet/createconefullstackedbar3dchart.svg",
                "svgimages/spreadsheet/createconemanhattanbarchart.svg",
                "svgimages/spreadsheet/createconestackedbar3dchart.svg",
                "svgimages/spreadsheet/createcylinderbar3dchart.svg",
                "svgimages/spreadsheet/createcylinderfullstackedbar3dchart.svg",
                "svgimages/spreadsheet/createcylindermanhattanbarchart.svg",
                "svgimages/spreadsheet/createcylinderstackedbar3dchart.svg",
                "svgimages/spreadsheet/createdoughnutchart.svg",
                "svgimages/spreadsheet/createexplodeddoughnutchart.svg",
                "svgimages/spreadsheet/createexplodedpie3dchart.svg",
                "svgimages/spreadsheet/createexplodedpiechart.svg",
                "svgimages/spreadsheet/createfromselection.svg",
                "svgimages/spreadsheet/createfullstackedarea3dchart.svg",
                "svgimages/spreadsheet/createfullstackedareachart.svg",
                "svgimages/spreadsheet/createfullstackedbar3dchart.svg",
                "svgimages/spreadsheet/createfullstackedbarchart.svg",
                "svgimages/spreadsheet/createfullstackedlinechart.svg",
                "svgimages/spreadsheet/createfullstackedlinechartnomarkers.svg",
                "svgimages/spreadsheet/createline3dchart.svg",
                "svgimages/spreadsheet/createlinechart.svg",
                "svgimages/spreadsheet/createlinechartnomarkers.svg",
                "svgimages/spreadsheet/createmanhattanbarchart.svg",
                "svgimages/spreadsheet/createpie3dchart.svg",
                "svgimages/spreadsheet/createpiechart.svg",
                "svgimages/spreadsheet/createpyramidbar3dchart.svg",
                "svgimages/spreadsheet/createpyramidfullstackedbar3dchart.svg",
                "svgimages/spreadsheet/createpyramidmanhattanbarchart.svg",
                "svgimages/spreadsheet/createpyramidstackedbar3dchart.svg",
                "svgimages/spreadsheet/createradarlinechart.svg",
                "svgimages/spreadsheet/createradarlinechartfilled.svg",
                "svgimages/spreadsheet/createradarlinechartnomarkers.svg",
                "svgimages/spreadsheet/createrotatedbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedconebar3dchart.svg",
                "svgimages/spreadsheet/createrotatedcylinderbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedfullstackedbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedfullstackedbarchart.svg",
                "svgimages/spreadsheet/createrotatedfullstackedconebar3dchart.svg",
                "svgimages/spreadsheet/createrotatedfullstackedcylinderbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedfullstackedpyramidbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedpyramidbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedstackedbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedstackedbarchart.svg",
                "svgimages/spreadsheet/createrotatedstackedconebar3dchart.svg",
                "svgimages/spreadsheet/createrotatedstackedcylinderbar3dchart.svg",
                "svgimages/spreadsheet/createrotatedstackedpyramidbar3dchart.svg",
                "svgimages/spreadsheet/createscatterchartlines.svg",
                "svgimages/spreadsheet/createscatterchartlinesandmarkers.svg",
                "svgimages/spreadsheet/createscatterchartsmoothlines.svg",
                "svgimages/spreadsheet/createscatterchartsmoothlinesandmarkers.svg",
                "svgimages/spreadsheet/createstackedarea3dchart.svg",
                "svgimages/spreadsheet/createstackedareachart.svg",
                "svgimages/spreadsheet/createstackedbar3dchart.svg",
                "svgimages/spreadsheet/createstackedbarchart.svg",
                "svgimages/spreadsheet/createstackedlinechart.svg",
                "svgimages/spreadsheet/createstackedlinechartnomarkers.svg",
                "svgimages/spreadsheet/createstockcharthighlowclose.svg",
                "svgimages/spreadsheet/createstockchartopenhighlowclose.svg",
                "svgimages/spreadsheet/createstockchartvolumehighlowclose.svg",
                "svgimages/spreadsheet/createstockchartvolumeopenhighlowclose.svg",
                "svgimages/spreadsheet/custommargins.svg",
                "svgimages/spreadsheet/custompapersize.svg",
                "svgimages/spreadsheet/customscaling.svg",
                "svgimages/spreadsheet/cut.svg",
                "svgimages/spreadsheet/datavalidation.svg",
                "svgimages/spreadsheet/date&time.svg",
                "svgimages/spreadsheet/decreasedecimal.svg",
                "svgimages/spreadsheet/definednameuseinformula.svg",
                "svgimages/spreadsheet/definename.svg",
                "svgimages/spreadsheet/deletecomment.svg",
                "svgimages/spreadsheet/document%20properties.svg",
                "svgimages/spreadsheet/documentorientation.svg",
                "svgimages/spreadsheet/donotrepeatitemlabelspivottable.svg",
                "svgimages/spreadsheet/donotshowsubtotalspivottable.svg",
                "svgimages/spreadsheet/dropandhighlowlines.svg",
                "svgimages/spreadsheet/droplines.svg",
                "svgimages/spreadsheet/droplinesnone.svg",
                "svgimages/spreadsheet/duplicatevalues.svg",
                "svgimages/spreadsheet/editcomment.svg",
                "svgimages/spreadsheet/editfilter.svg",
                "svgimages/spreadsheet/encrypt.svg",
                "svgimages/spreadsheet/equalto.svg",
                "svgimages/spreadsheet/errorbars.svg",
                "svgimages/spreadsheet/errorbarsnone.svg",
                "svgimages/spreadsheet/errorbarswithpercentage.svg",
                "svgimages/spreadsheet/errorbarswithstandarddeviation.svg",
                "svgimages/spreadsheet/expandcollapsebuttonpivottable.svg",
                "svgimages/spreadsheet/expandfieldpivottable.svg",
                "svgimages/spreadsheet/fieldheaderspivottable.svg",
                "svgimages/spreadsheet/fieldlistpanelpivottable.svg",
                "svgimages/spreadsheet/fieldsettingspivottable.svg",
                "svgimages/spreadsheet/fillbackground.svg",
                "svgimages/spreadsheet/filldown.svg",
                "svgimages/spreadsheet/fillleft.svg",
                "svgimages/spreadsheet/fillright.svg",
                "svgimages/spreadsheet/fillup.svg",
                "svgimages/spreadsheet/financial.svg",
                "svgimages/spreadsheet/fitallcolumnsonepage.svg",
                "svgimages/spreadsheet/fitallrowsonepage.svg",
                "svgimages/spreadsheet/fitsheetonepage.svg",
                "svgimages/spreadsheet/fontcolor.svg",
                "svgimages/spreadsheet/format.svg",
                "svgimages/spreadsheet/formatastable.svg",
                "svgimages/spreadsheet/formatcells.svg",
                "svgimages/spreadsheet/fraction.svg",
                "svgimages/spreadsheet/freezefirstcolumn.svg",
                "svgimages/spreadsheet/freezepanes.svg",
                "svgimages/spreadsheet/freezetoprow.svg",
                "svgimages/spreadsheet/functionscompatibility.svg",
                "svgimages/spreadsheet/functionsengineering.svg",
                "svgimages/spreadsheet/functionsinformation.svg",
                "svgimages/spreadsheet/functionsstatistical.svg",
                "svgimages/spreadsheet/functionsweb.svg",
                "svgimages/spreadsheet/general.svg",
                "svgimages/spreadsheet/grandtotalsoffrowscolumnspivottable.svg",
                "svgimages/spreadsheet/grandtotalsoncolumnsonlypivottable.svg",
                "svgimages/spreadsheet/grandtotalsonrowscolumnspivottable.svg",
                "svgimages/spreadsheet/grandtotalsonrowsonlypivottable.svg",
                "svgimages/spreadsheet/grandtotalspivottable.svg",
                "svgimages/spreadsheet/greaterthan.svg",
                "svgimages/spreadsheet/greendatabargradient.svg",
                "svgimages/spreadsheet/greendatabarsolid.svg",
                "svgimages/spreadsheet/greenwhitecolorscale.svg",
                "svgimages/spreadsheet/greenwhiteredcolorscale.svg",
                "svgimages/spreadsheet/greenyellowcolorscale.svg",
                "svgimages/spreadsheet/greenyellowredcolorscale.svg",
                "svgimages/spreadsheet/group.svg",
                "svgimages/spreadsheet/groupfooter.svg",
                "svgimages/spreadsheet/groupheader.svg",
                "svgimages/spreadsheet/growfont.svg",
                "svgimages/spreadsheet/hidedetail.svg",
                "svgimages/spreadsheet/highlightcellsrules.svg",
                "svgimages/spreadsheet/highlowlines.svg",
                "svgimages/spreadsheet/horizontalmode.svg",
                "svgimages/spreadsheet/hyperlink.svg",
                "svgimages/spreadsheet/changedatasourcepivottable.svg",
                "svgimages/spreadsheet/chartaxesgroup.svg",
                "svgimages/spreadsheet/chartaxistitlegroup.svg",
                "svgimages/spreadsheet/chartaxistitlehorizontal.svg",
                "svgimages/spreadsheet/chartaxistitlehorizontal_none.svg",
                "svgimages/spreadsheet/chartaxistitlevertical.svg",
                "svgimages/spreadsheet/chartaxistitlevertical_horizontaltitle.svg",
                "svgimages/spreadsheet/chartaxistitlevertical_none.svg",
                "svgimages/spreadsheet/chartaxistitlevertical_rotatedtitle.svg",
                "svgimages/spreadsheet/chartaxistitlevertical_verticaltitle.svg",
                "svgimages/spreadsheet/chartdatalabels_above.svg",
                "svgimages/spreadsheet/chartdatalabels_below.svg",
                "svgimages/spreadsheet/chartdatalabels_bestfit.svg",
                "svgimages/spreadsheet/chartdatalabels_center.svg",
                "svgimages/spreadsheet/chartdatalabels_insidebase.svg",
                "svgimages/spreadsheet/chartdatalabels_insideend.svg",
                "svgimages/spreadsheet/chartdatalabels_left.svg",
                "svgimages/spreadsheet/chartdatalabels_linecenter.svg",
                "svgimages/spreadsheet/chartdatalabels_linenone.svg",
                "svgimages/spreadsheet/chartdatalabels_none.svg",
                "svgimages/spreadsheet/chartdatalabels_right.svg",
                "svgimages/spreadsheet/chartdatalabelsgroup.svg",
                "svgimages/spreadsheet/chartgridlines.svg",
                "svgimages/spreadsheet/chartgridlineshorizontal_major.svg",
                "svgimages/spreadsheet/chartgridlineshorizontal_majorminor.svg",
                "svgimages/spreadsheet/chartgridlineshorizontal_minor.svg",
                "svgimages/spreadsheet/chartgridlineshorizontal_none.svg",
                "svgimages/spreadsheet/chartgridlinesvertical_major.svg",
                "svgimages/spreadsheet/chartgridlinesvertical_majorminor.svg",
                "svgimages/spreadsheet/chartgridlinesvertical_minor.svg",
                "svgimages/spreadsheet/chartgridlinesvertical_none.svg",
                "svgimages/spreadsheet/chartgroupscatter.svg",
                "svgimages/spreadsheet/charthorizontalaxis_billions.svg",
                "svgimages/spreadsheet/charthorizontalaxis_default.svg",
                "svgimages/spreadsheet/charthorizontalaxis_lefttoright.svg",
                "svgimages/spreadsheet/charthorizontalaxis_logscale.svg",
                "svgimages/spreadsheet/charthorizontalaxis_millions.svg",
                "svgimages/spreadsheet/charthorizontalaxis_none.svg",
                "svgimages/spreadsheet/charthorizontalaxis_righttoleft.svg",
                "svgimages/spreadsheet/charthorizontalaxis_thousands.svg",
                "svgimages/spreadsheet/charthorizontalaxis_withoutlabeling.svg",
                "svgimages/spreadsheet/chartlegend_none.svg",
                "svgimages/spreadsheet/chartlegend_overlaylegendatleft.svg",
                "svgimages/spreadsheet/chartlegend_overlaylegendatright.svg",
                "svgimages/spreadsheet/chartlegend_showlegendatbottom.svg",
                "svgimages/spreadsheet/chartlegend_showlegendatleft.svg",
                "svgimages/spreadsheet/chartlegend_showlegendatright.svg",
                "svgimages/spreadsheet/chartlegend_showlegendattop.svg",
                "svgimages/spreadsheet/chartlegendgroup.svg",
                "svgimages/spreadsheet/charttitleabove.svg",
                "svgimages/spreadsheet/charttitlecenteredoverlay.svg",
                "svgimages/spreadsheet/charttitlenone.svg",
                "svgimages/spreadsheet/chartverticalaxis_billions.svg",
                "svgimages/spreadsheet/chartverticalaxis_bottomtoup.svg",
                "svgimages/spreadsheet/chartverticalaxis_default.svg",
                "svgimages/spreadsheet/chartverticalaxis_logscale.svg",
                "svgimages/spreadsheet/chartverticalaxis_millions.svg",
                "svgimages/spreadsheet/chartverticalaxis_none.svg",
                "svgimages/spreadsheet/chartverticalaxis_thousands.svg",
                "svgimages/spreadsheet/chartverticalaxis_toptodown.svg",
                "svgimages/spreadsheet/chartverticalaxis_withoutlabeling.svg",
                "svgimages/spreadsheet/iconsets.svg",
                "svgimages/spreadsheet/increasedecimal.svg",
                "svgimages/spreadsheet/indent%20increase.svg",
                "svgimages/spreadsheet/insertblanklinepivottable.svg",
                "svgimages/spreadsheet/insertcellscommandgroup.svg",
                "svgimages/spreadsheet/insertsheet.svg",
                "svgimages/spreadsheet/insertsheetcolumns.svg",
                "svgimages/spreadsheet/insertsheetrows.svg",
                "svgimages/spreadsheet/inserttablecolumnstotheleft.svg",
                "svgimages/spreadsheet/inserttablecolumnstotheright.svg",
                "svgimages/spreadsheet/inserttablerowsabove.svg",
                "svgimages/spreadsheet/inserttablerowsbelow.svg",
                "svgimages/spreadsheet/landscape.svg",
                "svgimages/spreadsheet/leftborder.svg",
                "svgimages/spreadsheet/lessthan.svg",
                "svgimages/spreadsheet/lightbluedatabargradient.svg",
                "svgimages/spreadsheet/lightbluedatabarsolid.svg",
                "svgimages/spreadsheet/line.svg",
                "svgimages/spreadsheet/linecolor.svg",
                "svgimages/spreadsheet/linestyle.svg",
                "svgimages/spreadsheet/link.svg",
                "svgimages/spreadsheet/logical.svg",
                "svgimages/spreadsheet/longdate.svg",
                "svgimages/spreadsheet/lookup&reference.svg",
                "svgimages/spreadsheet/mailmergepreview.svg",
                "svgimages/spreadsheet/managedatasource.svg",
                "svgimages/spreadsheet/managequeries.svg",
                "svgimages/spreadsheet/managerelations.svg",
                "svgimages/spreadsheet/managerules.svg",
                "svgimages/spreadsheet/math&trig.svg",
                "svgimages/spreadsheet/merge&center.svg",
                "svgimages/spreadsheet/mergeacross.svg",
                "svgimages/spreadsheet/mergecells.svg",
                "svgimages/spreadsheet/middlealign.svg",
                "svgimages/spreadsheet/more.svg",
                "svgimages/spreadsheet/movechart.svg",
                "svgimages/spreadsheet/movechartnewsheet.svg",
                "svgimages/spreadsheet/movechartobjectin.svg",
                "svgimages/spreadsheet/movepivottable.svg",
                "svgimages/spreadsheet/multipledocuments.svg",
                "svgimages/spreadsheet/multiplesheet.svg",
                "svgimages/spreadsheet/namemanager.svg",
                "svgimages/spreadsheet/narrowmargins.svg",
                "svgimages/spreadsheet/new.svg",
                "svgimages/spreadsheet/newcomment.svg",
                "svgimages/spreadsheet/newrule.svg",
                "svgimages/spreadsheet/noborder.svg",
                "svgimages/spreadsheet/normalmargins.svg",
                "svgimages/spreadsheet/normalview.svg",
                "svgimages/spreadsheet/noscaling.svg",
                "svgimages/spreadsheet/number.svg",
                "svgimages/spreadsheet/open.svg",
                "svgimages/spreadsheet/orientation.svg",
                "svgimages/spreadsheet/other%20charts.svg",
                "svgimages/spreadsheet/outsideborder.svg",
                "svgimages/spreadsheet/pagebreakpreview.svg",
                "svgimages/spreadsheet/papersize.svg",
                "svgimages/spreadsheet/pastespecial.svg",
                "svgimages/spreadsheet/percentstyle.svg",
                "svgimages/spreadsheet/picture.svg",
                "svgimages/spreadsheet/pie.svg",
                "svgimages/spreadsheet/pivottable.svg",
                "svgimages/spreadsheet/pivottablecalculationsfieldsitemssetsgroup.svg",
                "svgimages/spreadsheet/pivottablegroupfield.svg",
                "svgimages/spreadsheet/pivottablegroupselection.svg",
                "svgimages/spreadsheet/pivottableoptions.svg",
                "svgimages/spreadsheet/pivottableungroup.svg",
                "svgimages/spreadsheet/portrait.svg",
                "svgimages/spreadsheet/print.svg",
                "svgimages/spreadsheet/printactivesheets.svg",
                "svgimages/spreadsheet/printarea.svg",
                "svgimages/spreadsheet/printentireworkbook.svg",
                "svgimages/spreadsheet/printpreview.svg",
                "svgimages/spreadsheet/printsheetselection.svg",
                "svgimages/spreadsheet/printtitles.svg",
                "svgimages/spreadsheet/protectsheet.svg",
                "svgimages/spreadsheet/protectworkbook.svg",
                "svgimages/spreadsheet/purpledatabargradient.svg",
                "svgimages/spreadsheet/purpledatabarsolid.svg",
                "svgimages/spreadsheet/quickprint.svg",
                "svgimages/spreadsheet/reapply.svg",
                "svgimages/spreadsheet/reddatabargradient.svg",
                "svgimages/spreadsheet/reddatabarsolid.svg",
                "svgimages/spreadsheet/redo.svg",
                "svgimages/spreadsheet/redtoblack.svg",
                "svgimages/spreadsheet/redwhitebluecolorscale.svg",
                "svgimages/spreadsheet/redwhitecolorscale.svg",
                "svgimages/spreadsheet/redwhitegreencolorscale.svg",
                "svgimages/spreadsheet/redyellowgreencolorscale.svg",
                "svgimages/spreadsheet/refreshallpivottable.svg",
                "svgimages/spreadsheet/refreshpivottable.svg",
                "svgimages/spreadsheet/removeblanklinepivottable.svg",
                "svgimages/spreadsheet/removecellscommandgroup.svg",
                "svgimages/spreadsheet/removesheet.svg",
                "svgimages/spreadsheet/removesheetcolumns.svg",
                "svgimages/spreadsheet/removesheetrows.svg",
                "svgimages/spreadsheet/removetablecolumns.svg",
                "svgimages/spreadsheet/removetablerows.svg",
                "svgimages/spreadsheet/repeatallitemlabelspivottable.svg",
                "svgimages/spreadsheet/replace.svg",
                "svgimages/spreadsheet/reportlayoutpivottable.svg",
                "svgimages/spreadsheet/resetrange.svg",
                "svgimages/spreadsheet/rightborder.svg",
                "svgimages/spreadsheet/scatter.svg",
                "svgimages/spreadsheet/scientific.svg",
                "svgimages/spreadsheet/selectdata.svg",
                "svgimages/spreadsheet/selectdatamember.svg",
                "svgimages/spreadsheet/selectdatasource.svg",
                "svgimages/spreadsheet/selectlabelspivottable.svg",
                "svgimages/spreadsheet/selectpivottable.svg",
                "svgimages/spreadsheet/selectvaluespivottable.svg",
                "svgimages/spreadsheet/sendbackward.svg",
                "svgimages/spreadsheet/sendtoback.svg",
                "svgimages/spreadsheet/serieslines.svg",
                "svgimages/spreadsheet/setdetaildatamember.svg",
                "svgimages/spreadsheet/setdetaillevel.svg",
                "svgimages/spreadsheet/setdetailrange.svg",
                "svgimages/spreadsheet/setfooterrange.svg",
                "svgimages/spreadsheet/setheaderrange.svg",
                "svgimages/spreadsheet/shortdate.svg",
                "svgimages/spreadsheet/showallsubtotalsatbottompivottable.svg",
                "svgimages/spreadsheet/showallsubtotalsattoppivottable.svg",
                "svgimages/spreadsheet/showcompactformpivottable.svg",
                "svgimages/spreadsheet/showdetail.svg",
                "svgimages/spreadsheet/showformulas.svg",
                "svgimages/spreadsheet/showhidecomments.svg",
                "svgimages/spreadsheet/showoutlineformpivottable.svg",
                "svgimages/spreadsheet/showranges.svg",
                "svgimages/spreadsheet/showtabularformpivottable.svg",
                "svgimages/spreadsheet/shrinkfont.svg",
                "svgimages/spreadsheet/singlesheet.svg",
                "svgimages/spreadsheet/size.svg",
                "svgimages/spreadsheet/sort&filter.svg",
                "svgimages/spreadsheet/sortatoz.svg",
                "svgimages/spreadsheet/sortfields.svg",
                "svgimages/spreadsheet/sortztoa.svg",
                "svgimages/spreadsheet/strikeout.svg",
                "svgimages/spreadsheet/strikeoutdouble.svg",
                "svgimages/spreadsheet/subtotal.svg",
                "svgimages/spreadsheet/subtotalspivottable.svg",
                "svgimages/spreadsheet/switchrowcolumns.svg",
                "svgimages/spreadsheet/symbol.svg",
                "svgimages/spreadsheet/table.svg",
                "svgimages/spreadsheet/tableconverttorange.svg",
                "svgimages/spreadsheet/text.svg",
                "svgimages/spreadsheet/text2.svg",
                "svgimages/spreadsheet/textthatcontains.svg",
                "svgimages/spreadsheet/thickbottomborder.svg",
                "svgimages/spreadsheet/thickboxborder.svg",
                "svgimages/spreadsheet/time.svg",
                "svgimages/spreadsheet/top10items.svg",
                "svgimages/spreadsheet/top10percent.svg",
                "svgimages/spreadsheet/topalign.svg",
                "svgimages/spreadsheet/topandbottomborder.svg",
                "svgimages/spreadsheet/topanddoublebottomborder.svg",
                "svgimages/spreadsheet/topandthickbottomborder.svg",
                "svgimages/spreadsheet/topborder.svg",
                "svgimages/spreadsheet/topbottomrules.svg",
                "svgimages/spreadsheet/uncollated.svg",
                "svgimages/spreadsheet/underlinedouble.svg",
                "svgimages/spreadsheet/undo.svg",
                "svgimages/spreadsheet/unfreezepanes.svg",
                "svgimages/spreadsheet/ungroup.svg",
                "svgimages/spreadsheet/unlink.svg",
                "svgimages/spreadsheet/unmergecells.svg",
                "svgimages/spreadsheet/updownbars.svg",
                "svgimages/spreadsheet/updownbarsnone.svg",
                "svgimages/spreadsheet/verticalmode.svg",
                "svgimages/spreadsheet/whitegreencolorscale.svg",
                "svgimages/spreadsheet/whiteredcolorscale.svg",
                "svgimages/spreadsheet/widemargins.svg",
                "svgimages/spreadsheet/wraptext.svg",
                "svgimages/spreadsheet/yellowdatabargradient.svg",
                "svgimages/spreadsheet/yellowdatabarsolid.svg",
                "svgimages/spreadsheet/yellowgreencolorscale.svg",
                "svgimages/spreadsheet/zoom.svg",
                "svgimages/spreadsheet/zoomout.svg"
              };

            this._SysHotKeys = new Keys[]
            {
                Keys.Control | Keys.A,
                Keys.Control | Keys.B,
                Keys.Control | Keys.C,
                Keys.Control | Keys.D,
                Keys.Control | Keys.E,
                Keys.Control | Keys.F,
                Keys.Control | Keys.G,
                Keys.Control | Keys.H,
                Keys.Control | Keys.I,
                Keys.Control | Keys.J,
                Keys.Control | Keys.K,
                Keys.Control | Keys.L,
                Keys.Control | Keys.M,
                Keys.Control | Keys.N,
                Keys.Control | Keys.O,
                Keys.Control | Keys.P,
                Keys.Control | Keys.Q,
                Keys.Control | Keys.R,
                Keys.Control | Keys.S,
                Keys.Control | Keys.T,
                Keys.Control | Keys.U,
                Keys.Control | Keys.V,
                Keys.Control | Keys.W,
                Keys.Control | Keys.X,
                Keys.Control | Keys.Y,
                Keys.Control | Keys.Z,
                Keys.Control | Keys.F1,
                Keys.Control | Keys.F2,
                Keys.Control | Keys.F3,
                Keys.Control | Keys.F4,
                Keys.Control | Keys.F5,
                Keys.Control | Keys.F6,
                Keys.Control | Keys.F7,
                Keys.Control | Keys.F8,
                Keys.Control | Keys.F9,
                Keys.Control | Keys.Home,
                Keys.Control | Keys.End,
                Keys.Control | Keys.PageUp,
                Keys.Control | Keys.PageDown
            };
        }
        /// <summary>
        /// Pole obrázků typu SVG pro obecné použití
        /// </summary>
        private string[] _SysSvgImages;
        /// <summary>
        /// Pole kláves Ctrl+Něco pro HotKey
        /// </summary>
        private Keys[] _SysHotKeys;

        private void TestResources()
        {
            int cnt = DxApplicationResourceLibrary.Count;

            DxApplicationResourceLibrary.ResourceItem item;

            if (DxApplicationResourceLibrary.TryGetResource(@"Images\Actions24\align-horizontal-right-out(24).png", false, out item))
            {
                var sizeType = item.SizeType;
            }

            string caption = null;
            if (DxApplicationResourceLibrary.TryGetResource(@"Images\Svg\amazon-chime-svgrepo-com.svg", false, out item))
            {
                var sizeType = item.SizeType;
            }
            //  else if (ResourceLibrary.TryGetResource(@"Images\SvgAsol\building-factory-4-filled-large.svg", out item))
            else if (DxApplicationResourceLibrary.TryGetResource(@"Images\SvgAsol\vyhledavani-large.svg", false, out item))
            {
                var sizeType = item.SizeType;
                caption = "Vybrat pořadač";
            }

            if (item != null)
            {
                var funcRibbonForm = _RibbonPages[0].Groups[1].Items.FirstOrDefault(i => i.ItemId == "RibbonForm");
                var ribbonItem = funcRibbonForm?.RibbonItem;
                if (ribbonItem != null)
                {
                    ribbonItem.ImageOptions.SvgImage = item.CreateSvgImage(DxComponent.GetImagePalette(DxComponent.IsDarkTheme, false));
                    if (caption != null) ribbonItem.Caption = caption;
                }
            }
        }
        #region Ukládání a obnova pozice okna
        /// <summary>
        /// Pokusí se z konfigurace najít a načíst string popisující pozici okna.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Za toto jméno přidá suffix (začíná podtržítkem a obsahuje XML validní znaky) a vyhledá konfiguraci se suffixem.<br/>
        /// 3. Pokud nenajde konfiguraci se suffixem, vyhledá konfiguraci bez suffixu = obecná, posledně použití (viz <see cref="PositionSaveToConfig(string, string)"/>).<br/>
        /// 4. Nalezený string je ten, který byl uložen v metodě <see cref="PositionSaveToConfig(string, string)"/> a je roven parametru 'positionData'. Pokud položku v konfiguraci nenajde, vrátí null (nebo prázdný string).
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// <para/>
        /// Konverze formátů: Pokud v konfiguraci budou uložena stringová data ve starším formátu, než dokáže obsloužit zpracující třída <see cref="FormStatusInfo"/>, pak konverzi do jejího formátu musí zajistit aplikační kód (protože on ví, jak zpracovat starý formát).<br/>
        /// <b><u>Postup:</u></b><br/>
        /// 1. Po načtení konfigurace se lze dotázat metodou <see cref="FormStatusInfo.IsPositionDataValid(string)"/>, zda načtená data jsou validní.<br/>
        /// 2. Pokud nejsou validní, pak je volající aplikace zkusí analyzovat svým starším (legacy) postupem na prvočinitele;<br/>
        /// 3. A pokud je úspěšně rozpoznala, pak ze základních dat sestaví validní konfirurační string s pomocí metody <see cref="FormStatusInfo.CreatePositionData(bool?, FormWindowState?, Rectangle?, Rectangle?)"/>.<br/>
        /// </summary>
        /// <param name="nameSuffix">Suffix ke jménu konfigurace, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory</param>
        /// <returns></returns>
        protected override string PositionLoadFromConfig(string nameSuffix)
        {
            string positionData = DxComponent.Settings.GetRawValue("FormPosition", PositionConfigName + nameSuffix);
            if (String.IsNullOrEmpty(positionData))
                positionData = DxComponent.Settings.GetRawValue("FormPosition", PositionConfigName);
            return positionData;
        }
        /// <summary>
        /// Do konfigurace uloží dodaná data o pozici okna '<paramref name="positionData"/>'.
        /// Dostává k dispozici nameSuffix, který identifikuje aktuální rozložení monitorů, aby bylo možno načíst konfiguraci pro aktuální monitory.
        /// <para/>
        /// <b><u>Aplikační kód tedy:</u></b><br/>
        /// 1. Získá vlastní jméno položky konfigurace pro svoje konkrétní okno (např. typ okna).<br/>
        /// 2. Jednak uloží data <paramref name="positionData"/> přímo do položky konfigurace pod svým vlastním jménem bez suffixu = data obecná pro libovolnou konfiguraci monitorů.<br/>
        /// 3. A dále uloží tato data do položky konfigurace, kde za svoje jméno přidá dodaný suffix <paramref name="nameSuffix"/> = tato hodnota se použije po restore na shodné konfiguraci monitorů.<br/>
        /// <para/>
        /// Tato technika zajistí, že pro různé konfigurace monitorů (např. při práci na více monitorech a poté přechodu na RDP s jedním monitorem, atd) budou uchovány konfigurace odděleně.
        /// </summary>
        /// <param name="positionData"></param>
        /// <param name="nameSuffix"></param>
        protected override void PositionSaveToConfig(string positionData, string nameSuffix)
        {
            DxComponent.Settings.SetRawValue("FormPosition", PositionConfigName, positionData);
            DxComponent.Settings.SetRawValue("FormPosition", PositionConfigName + nameSuffix, positionData);
        }
        /// <summary>
        /// Jméno okna v uložené konfiguraci
        /// </summary>
        protected static string PositionConfigName { get { return "MainForm"; } }
        #endregion
        #region Log

        private void DxComponent_LogTextChanged(object sender, EventArgs e)
        {
            _LogContainChanges = true;
        }
        protected override void OnApplicationIdle()
        {
            if (this._StatusStartLabel.Tag == null)
                RefreshStartTime();
            if (_LogContainChanges)
                RefreshLog();
        }
        bool _LogContainChanges;
        private void RefreshStartTime()
        {
            TimeSpan? startTime = DxComponent.ApplicationStartUpTime;
            if (!startTime.HasValue) return;
            this._StatusStartLabel.Caption = "Start time: " + startTime.Value.ToString();
            this._StatusStartLabel.Tag = startTime.Value;
        }
        /// <summary>
        /// Provede RefreshLog = načte text z <see cref="DxComponent.LogText"/> a vloží jej do textu v controlu <see cref="CurrentLogControl"/> (pokud tam není null).
        /// Zajistí scrollování na konec textu.
        /// </summary>
        protected void RefreshLog()
        {
            var control = CurrentLogControl;
            if (control != null)
            {
                string logText = DxComponent.LogText ?? "";
                control.Text = logText;
                control.SelectionStart = logText.Length;
                control.SelectionLength = 0;
                control.ScrollToCaret();
            }
            _LogContainChanges = false;
        }
        /// <summary>
        /// Aktuálně aktivní control pro zobrazení dat logu, aktivuje konkrétní stránka
        /// </summary>
        protected DxMemoEdit CurrentLogControl;
        #endregion
        #region WinApi Messages - zpřístupnění
        private void _NotifyMessageInit()
        {
            this.MessageTrackActive = WinApiMessageSourceType.None;
        }
        protected override void OnNotifyMessage(Message m)
        {
            _TrackMessage(WinApiMessageSourceType.OnNotifyMessage, m);
            base.OnNotifyMessage(m);
        }
        protected override void WndProc(ref Message msg)
        {
            _TrackMessage(WinApiMessageSourceType.WndProc, msg);
            base.WndProc(ref msg);
        }
        public override bool PreProcessMessage(ref Message msg)
        {
            _TrackMessage(WinApiMessageSourceType.PreProcessMessage, msg);
            return base.PreProcessMessage(ref msg);
        }
        protected override bool ProcessKeyMessage(ref Message m)
        {
            _TrackMessage(WinApiMessageSourceType.ProcessKeyMessage, m);
            return base.ProcessKeyMessage(ref m);
        }
        private void _TrackMessage(WinApiMessageSourceType source, Message message)
        {
            if (MessageTrackActive != WinApiMessageSourceType.None && MessageTrackActive.HasFlag(source) && WinApiMessageReceivedHandler != null)
                WinApiMessageReceivedHandler(this, new WinApiMessageArgs(source, message));
        }
        public WinApiMessageSourceType MessageTrackActive { get; set; }
        public event WinApiMessageHandler WinApiMessageReceivedHandler;
        public delegate void WinApiMessageHandler(object sender, WinApiMessageArgs args);
        public class WinApiMessageArgs : EventArgs
        {
            public WinApiMessageArgs(WinApiMessageSourceType source, Message message)
            {
                Source = source;
                Message = message;
            }
            public WinApiMessageSourceType Source { get; private set; }
            public Message Message { get; private set; }
        }
        [Flags]
        public enum WinApiMessageSourceType
        {
            None = 0,
            OnNotifyMessage = 0x01,
            PreProcessMessage = 0x02,
            ProcessKeyMessage = 0x04,
            WndProc = 0x08,

            All = OnNotifyMessage | PreProcessMessage | ProcessKeyMessage | WndProc
        }
        #endregion
        #region BarManager a Ribbon
        private void InitBarManager()
        {
            this._BarManager = new XB.BarManager();
            this._BarManager.Form = this;

            this._BarManager.ToolTipController = DxComponent.CreateNewToolTipController();
            this._BarManager.ToolTipController.AddClientControl(this);

            //this._BarManager = new XB.BarManager(this.Container);
            //this._BarManager.ForceInitialize();

            //this._BarManager.AllowCustomization = true;
            //this._BarManager.AllowQuickCustomization = true;
            //this._BarManager.MenuAnimationType = XB.AnimationType.Fade;

            //this._BarManager.AllowItemAnimatedHighlighting = true;

            this._BarManager.ItemClick += _BarManager_ItemClick;
            //this._BarManager.CloseButtonClick += _BarManager_CloseButtonClick;
            //this._BarManager

        }
        XB.BarManager _BarManager;
        protected override void DxRibbonPrepare()
        {
            DxQuickAccessToolbar.ConfigValue = "Location:Below	_SYS__DevExpress_SkinSetDropDown	_SYS__DevExpress_SkinPaletteDropDown	";
            DxQuickAccessToolbar.ConfigValueChanged += DxQuickAccessToolbar_ConfigValueChanged;

            List<DataRibbonPage> pages = new List<DataRibbonPage>();

            DataRibbonPage page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.Default);
            page.MergeOrder = 10;
            AddFunctionsGroup(page);
            pages.Add(page);

            page = CreateRibbonSamplePage();
            page.MergeOrder = 20;
            if (page != null) pages.Add(page);

            page = CreateRibbonSvgImagesPage();
            page.MergeOrder = 30;
            if (page != null) pages.Add(page);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            _RibbonPages = pages;
        }

        private void DxQuickAccessToolbar_ConfigValueChanged(object sender, EventArgs e)
        {

        }

        private List<DataRibbonPage> _RibbonPages;
        protected override void DxStatusPrepare()
        {
            this._StatusStartLabel = DxComponent.CreateDxStatusLabel(this.DxStatusBar, "Start time: ...", XB.BarStaticItemSize.Content);
        }
        DxBarStaticItem _StatusStartLabel;

        private void _BarManager_CloseButtonClick(object sender, EventArgs e)
        {

        }

        private void _BarManager_ItemClick(object sender, XB.ItemClickEventArgs e)
        {
            if (e.Item.Name == "CheckItem")
                CheckItemChecked = (e.Item as XB.BarCheckItem).Checked;
        }
        #endregion
        #region Ribbon Functions
        private void AddFunctionsGroup(DataRibbonPage page)
        {
            DataRibbonGroup group;

            group = new DataRibbonGroup() { GroupText = "FUNKCE" };
            group.Items.Add(CreateRibbonFunction("GraphForm", "Graph Form", "svgimages/chart/chart.svg", "Ukázky grafů DevExpress", _OpenGraphFormButton_Click));
            group.Items.Add(CreateRibbonFunction("LayoutForm", "Layout Form", "devav/layout/pages.svg", "Otevře okno pro testování layoutu (pod-okna)", _OpenLayoutFormButton_Click));
            group.Items.Add(CreateRibbonFunction("DataForm1", "Data Form1", "svgimages/spreadsheet/showtabularformpivottable.svg", "Otevře okno pro testování DataFormu", _TestDataForm1ModalButton_Click));
            group.Items.Add(CreateRibbonFunction("DataForm2", "Data Form2", "svgimages/spreadsheet/showtabularformpivottable.svg", "Otevře okno pro testování DataFormu 2", _TestDataForm2ModalButton_Click));
            group.Items.Add(CreateRibbonFunction("RibbonForm", "Ribbon Form", "svgimages/reports/distributerowsevenly.svg", "Otevře okno pro testování Ribbonu", _TestDxRibbonFormModalButton_Click));
            group.Items.Add(CreateRibbonFunction("DisableSvgForm", "Disabled SVG", "svgimages/icon%20builder/actions_calendar.svg", "Otevře okno s Ribbonem s Enabled/Disabled tlačítky", _TestDxDisabledSvgRibbonFormModalButton_Click));

            // group.Items.Add(CreateRibbonFunction("DevExpressRibbon", "Native Ribbon", "svgimages/reports/gaugestylelinearhorizontal.svg", "Otevře okno s nativním Ribbonem", _TestDxDevExpressRibbon_Click, null, true));
            // // group.Items.Add(CreateRibbonFunction("RibbonFormClasses", "Classes Ribbon", "svgimages/reports/gaugestylelinearhorizontal.svg", "Otevře okno s ASOL Ribbonem, vytvořeným jen s použitím tříd DxRibbon", _TestDxRibbonFormClassesModalButton_Click));
            // group.Items.Add(CreateRibbonFunction("TestRibbon", "Test Ribbon", "svgimages/reports/gaugestylelinearhorizontal.svg", "Otevře okno s ASOL Ribbonem, vytvořeným s využitím všech metod DxRibbon a rozhraní IRibbon", _TestDxTestRibbon_Click));
            // group.Items.Add(CreateRibbonFunction("AsolRibbon", "ASOL Ribbon", "svgimages/reports/gaugestylelinearhorizontal.svg", "Otevře okno s ASOL Ribbonem, vytvořeným s použitím definičních dat IRibbon", _TestDxAsolRibbon_Click));
            // // group.Items.Add(CreateRibbonFunction("RibbonFormData3", "IData3 Ribbon", "svgimages/reports/gaugestylelinearhorizontal.svg", "Otevře okno s ASOL Ribbonem, vytvořeným s použitím definičních dat IRibbon a s třístupňovým mergováním (Slave => Void => Desktop)", _TestDxRibbonFormData3ModalButton_Click));

            group.Items.Add(CreateRibbonFunction("TreeBgrForm", "TreeBgr Form", "svgimages/diagramicons/tipovertree_right.svg", "Otevře okno pro testování TreeListu s akcemi na pozadí", _TestDxTreeBgrFormButton_Click));
            group.Items.Add(CreateRibbonFunction("DiagramControlForm", "Diagram Control", "svgimages/diagramicons/relayoutparts.svg", "Otevře okno pro Diagram Control", _TestDxDiagramControlFormButton_Click));
            group.Items.Add(CreateRibbonFunction("HandleScan", "Handle Scan", "svgimages/spreadsheet/createline3dchart.svg", "Otevře okno pro měření systémových handle běžících procesů", _TestHandleScanFormButton_Click));
            group.Items.Add(CreateRibbonFunction("FormWithTitleButton", "Title Button", "devav/ui/window/window.svg", "Otevře okno, které má v titulkovém řádku přidaný Button", _TestFormWithTitleButton_Click));
            group.Items.Add(CreateRibbonFunction("CabCabRead", "CabCab Read", "svgimages/actions/up.svg", "Otevře soubor dvojitého CABU a načítá jeho obsah", _TestCabCabReadButton_Click));

            AddRibbonSoundsMenu(group);
            AddRibbonWavFilesMenu(group);

            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupText = "BROWSE" };
            group.Items.Add(CreateRibbonFunction("BrowseStd", "Browse Standard", "svgimages/richedit/selecttablerow.svg", "Otevře okno pro testování standardního Browse", _TestDxBrowseStandardForm_Click));
            // group.Items.Add(CreateRibbonFunction("BrowseVirt", "Browse Virtual", "svgimages/richedit/selecttablerow.svg", "Otevře okno pro testování virtuálního Browse", _TestDxBrowseVirtualForm_Click));
            group.Items.Add(CreateRibbonFunction("BrowseNeph", "Browse Nephrite", "svgimages/dashboards/grid.svg", "Otevře okno pro testování Browse Nephrite", _TestDxBrowseForm_Click));
            page.Groups.Add(group);

            group = new DataRibbonGroup() { GroupText = "STYLY" };
            string resourceL = "svgimages/dashboards/editcolors.svg";
            string resourceS = "svgimages/dashboards/editrules.svg";
            group.Items.Add(CreateRibbonFunction("StylesAll", "All", resourceL, "Button ve stylu All", null, RibbonItemStyles.All, true));
            group.Items.Add(CreateRibbonFunction("StylesDefault", "Default", resourceL, "Button ve stylu Default", null, RibbonItemStyles.Default));
            group.Items.Add(CreateRibbonFunction("StylesLarge", "Large", resourceL, "Button ve stylu Large", null, RibbonItemStyles.Large, true));
            // group.Items.Add(CreateRibbonFunction("StylesLarge", "Large", resourceL, "Button ve stylu Large", null, RibbonItemStyles.Large));
            group.Items.Add(CreateRibbonFunction("StylesSmallWithText", "SmallWithText", resourceS, "Button ve stylu SmallWithText", null, RibbonItemStyles.SmallWithText));
            group.Items.Add(CreateRibbonFunction("StylesSmallWithoutText", "SmallWithoutText", resourceS, "Button ve stylu SmallWithoutText", null, RibbonItemStyles.SmallWithoutText));
            group.Items.Add(CreateRibbonFunction("StylesLargeSmallWithText", "Large WithText", resourceL, "Button ve stylu Large + SmallWithText", null, RibbonItemStyles.Large | RibbonItemStyles.SmallWithText, true));
            group.Items.Add(CreateRibbonFunction("StylesLargeSmallWithoutText", "Large WithoutText", resourceL, "Button ve stylu Large + SmallWithoutText", null, RibbonItemStyles.Large | RibbonItemStyles.SmallWithoutText));
            page.Groups.Add(group);

        }
        protected void AddRibbonSoundsMenu(DataRibbonGroup group)
        {
            DataRibbonItem menu1 = new DataRibbonItem()
            {
                ItemId = "RibbonSoundMenuOn",
                Text = "Zvuky",
                ImageName = "images/media/audiocontent_32x32.png",
                ToolTipText = "Nabídka AKTUÁLNĚ POUŽITELNÝCH systémových zvuků.\r\nKliknutí na zvuk jej přehraje, a do clipboardu vloží jeho EventName.",
                ItemType = RibbonItemType.Menu,
                RibbonStyle = RibbonItemStyles.Large,
                SubItems = new ListExt<IRibbonItem>(),
                ItemIsFirstInGroup = true,
                ClickAction = null
            };
            group.Items.Add(menu1);

            DataRibbonItem menu2 = new DataRibbonItem()
            {
                ItemId = "RibbonSoundMenuOff",
                Text = "Ne-Zvuky",
                ImageName = "images/media/audiocontent_16x16.png",
                ToolTipText = "Nabídka systémových zvuků, které NEMAJÍ definovaný konkrétní zvuk.\r\nKliknutí na zvuk jej zkusí přehrát, a do clipboardu vloží jeho EventName.",
                ItemType = RibbonItemType.Menu,
                RibbonStyle = RibbonItemStyles.Large,
                SubItems = new ListExt<IRibbonItem>(),
                ItemIsFirstInGroup = false,
                ClickAction = null
            };
            group.Items.Add(menu2);

            var sounds = DxComponent.SystemEventSounds;
            foreach (var sound in sounds)
            {
                if (sound.ExistsCurrentSource)
                {
                    string image = "images/arrows/play_16x16.png";
                    DataRibbonItem item = new DataRibbonItem()
                    {
                        ItemId = "RibbonSoundItem_" + sound.EventName,
                        Text = sound.Description + "  [" + sound.EventName + "]",
                        ImageName = image,
                        ToolTipTitle = sound.Description,
                        ToolTipText = sound.EventName + "\r\n" + sound.SoundSource + "\r\n",
                        ItemType = RibbonItemType.Button,
                        RibbonStyle = RibbonItemStyles.SmallWithText,
                        ItemIsFirstInGroup = false,
                        ClickAction = _PlaySystemSound,
                        Tag = sound
                    };
                    menu1.SubItems.Add(item);
                }
                else
                {
                    string image = "";
                    DataRibbonItem item = new DataRibbonItem()
                    {
                        ItemId = "RibbonSoundItem_" + sound.EventName,
                        Text = sound.Description + "  [" + sound.EventName + "]",
                        ImageName = image,
                        ToolTipTitle = sound.Description,
                        ToolTipText = sound.EventName + "\r\n" + sound.SoundSource + "\r\n",
                        ItemType = RibbonItemType.Button,
                        RibbonStyle = RibbonItemStyles.SmallWithText,
                        ItemIsFirstInGroup = false,
                        ClickAction = _PlaySystemSound,
                        Tag = sound
                    };
                    menu2.SubItems.Add(item);
                }
            }
        }
        protected void AddRibbonWavFilesMenu(DataRibbonGroup group)
        {
            string path = System.IO.Path.Combine(DxComponent.ApplicationPath, "media");
            DataRibbonItem menuW = new DataRibbonItem()
            {
                ItemId = "RibbonWavFilesMenu",
                Text = "Nahrávky",
                ImageName = "images/xaf/bo_folder_32x32.png",
                ToolTipText = $"Nabídka zvuků z adresáře '{path}', které lze přehrát.\r\nKliknutí na soubor jej zkusí přehrát, a do clipboardu vloží jeho FullName.",
                ItemType = RibbonItemType.Menu,
                RibbonStyle = RibbonItemStyles.Large,
                SubItems = new ListExt<IRibbonItem>(),
                ItemIsFirstInGroup = false,
                ClickAction = null
            };
            group.Items.Add(menuW);

            List<string> files = null;
            if (System.IO.Directory.Exists(path))
                files = System.IO.Directory.GetFiles(path, "*.wav").ToList();

            if (files is null || files.Count == 0)
            {
                DataRibbonItem item = new DataRibbonItem()
                {
                    ItemId = "RibbonWavFile_None",
                    Text = "Adresář Media neexistuje nebo neobsahuje žádný WAV soubor",
                    ImageName = "",
                    ItemType = RibbonItemType.Button,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    ItemIsFirstInGroup = false
                };
                menuW.SubItems.Add(item);
                return;
            }

            files.Sort();
            foreach (var fullName in files)
            {
                string image = "images/arrows/play_16x16.png";
                string fileName = System.IO.Path.GetFileName(fullName);
                string name = System.IO.Path.GetFileNameWithoutExtension(fullName);
                DataRibbonItem item = new DataRibbonItem()
                {
                    ItemId = "RibbonWavFile_" + name,
                    Text = fileName,
                    ImageName = image,
                    ToolTipTitle = name,
                    ToolTipText = fullName,
                    ItemType = RibbonItemType.Button,
                    RibbonStyle = RibbonItemStyles.SmallWithText,
                    ItemIsFirstInGroup = false,
                    ClickAction = _PlayWavFile,
                    Tag = fullName
                };
                menuW.SubItems.Add(item);
            }
        }
        protected DataRibbonItem CreateRibbonFunction(string itemId, string text, string image, string toolTipText, Action<IMenuItem> clickHandler = null, RibbonItemStyles? styles = null, bool firstInGroup = false)
        {
            DataRibbonItem iRibbonItem = new DataRibbonItem()
            {
                ItemId = itemId,
                Text = text,
                ImageName = image,
                ToolTipText = toolTipText,
                ItemType = RibbonItemType.Button,
                RibbonStyle = styles ?? RibbonItemStyles.All,
                ItemIsFirstInGroup = firstInGroup,
                ClickAction = clickHandler
            };
            return iRibbonItem;
        }
        private void _PlaySystemSound(IMenuItem menuItem)
        {
            if (menuItem?.Tag is SystemEventSound sound)
            {
                sound.Play();
                string code = $"            string soundEventName = \"{sound.EventName}\";";
                if (code.Length < 80) code = code.PadRight(80);
                code += $"// {sound.Description}\r\n";
                DxComponent.ClipboardInsert(code);
            }
        }
        private void _PlayWavFile(IMenuItem menuItem)
        {
            if (menuItem?.Tag is string fullName)
            {
                DxComponent.AudioSoundWavPlay(fullName);
                string code = $"            string fileName = @\"{fullName}\";";
                DxComponent.ClipboardInsert(code);
            }
        }
        private void _OpenGraphFormButton_Click(IMenuItem menuItem)
        {
            DxComponent.TryRun(() =>
            {
                using (GraphForm form = new GraphForm())
                {
                    form.ShowDialog(this);
                }
            });
        }
        private void _OpenLayoutFormButton_Click(IMenuItem menuItem)
        {
            LayoutForm form = new LayoutForm(true);
            form.Show();
        }
        private void _TestDataForm1ModalButton_Click(IMenuItem menuItem)
        {
            DxComponent.WinProcessInfo winProcessInfo = DxComponent.WinProcessInfo.GetCurent();
            using (var dataForm = new DataFormV1())
            {
                dataForm.WinProcessInfoBeforeForm = winProcessInfo;
                dataForm.WindowState = FormWindowState.Maximized;
                dataForm.ShowDialog();
            }
        }
        private void _TestDataForm1NormalButton_Click(IMenuItem menuItem)
        {
            DxComponent.WinProcessInfo winProcessInfo = DxComponent.WinProcessInfo.GetCurent();
            var dataForm = new DataFormV1();
            dataForm.WinProcessInfoBeforeForm = winProcessInfo;
            dataForm.WindowState = FormWindowState.Normal;
            dataForm.Size = new Size(1400, 900);
            dataForm.StartPosition = FormStartPosition.WindowsDefaultLocation;
            dataForm.Show();
        }
        private void _TestDataForm2ModalButton_Click(IMenuItem menuItem)
        {
            DxComponent.WinProcessInfo winProcessInfo = DxComponent.WinProcessInfo.GetCurent();
            using (var dataForm = new DataFormV2())
            {
                dataForm.WindowState = FormWindowState.Maximized;
                dataForm.ShowDialog();
            }
        }
        private void _TestDxRibbonFormModalButton_Click(IMenuItem menuItem)
        {
            using (var ribbonForm = new RibbonForm())
            {
                ribbonForm.WindowState = FormWindowState.Maximized;
                ribbonForm.ShowDialog();
            }
        }
        private void _TestDxDisabledSvgRibbonFormModalButton_Click(IMenuItem menuItem)
        {
            using (var ribbonForm = new TestSvgForm())
            {
                ribbonForm.WindowState = FormWindowState.Maximized;
                ribbonForm.ShowDialog();
            }
        }
        private void _TestDxTreeBgrFormButton_Click(IMenuItem menuItem)
        {
            using (var form = new TreeBgrForm())
            {
                form.WindowState = FormWindowState.Maximized;
                form.ShowDialog();
            }
        }
        private void _TestDxDiagramControlFormButton_Click(IMenuItem menuItem)
        {
            using (var form = new DiagramControlForm())
            {
                form.WindowState = FormWindowState.Maximized;
                form.ShowDialog();
            }
        }
        private void _TestHandleScanFormButton_Click(IMenuItem menuItem)
        {
            if (_HandleScanForm is null || _HandleScanForm.ActivityState == WindowActivityState.Closing || _HandleScanForm.ActivityState == WindowActivityState.Closed || _HandleScanForm.ActivityState == WindowActivityState.Disposed || _HandleScanForm.ActivityState == WindowActivityState.Disposed)
            {
                _HandleScanForm = new HandleScanForm();
                _HandleScanForm.WindowState = FormWindowState.Maximized;
                _HandleScanForm.Show();
            }
            else
            {
                _HandleScanForm.Show();
                _HandleScanForm.Activate();
            }
        }
        private void _TestFormWithTitleButton_Click(IMenuItem menuItem)
        {
            using (var form = new DxRibbonForm())
            {
                form.Text = "Ukázka okna s tlačítkem navíc...";
                form.WindowState = FormWindowState.Normal;
                form.FormRibbonVisibility = FormRibbonVisibilityMode.FormTitleRow;          // Nothing  FormTitleRow  Standard
                form.Size = new Size(800, 600);
                form.StartPosition = FormStartPosition.CenterParent;
                form.ActivityStateChanged += MainForm_ActivityStateChanged;
                form.DxMainPanel.Controls.Add(new System.Windows.Forms.Panel() { Bounds = new Rectangle(0, 0, 64, 64), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightSkyBlue });
                var titleBarItems = _CreateTitleBarItems();
                form.DxRibbon.TitleBarItems = titleBarItems;
                form.DxRibbon.RibbonItemClick += TitleBarDxRibbon_RibbonItemClick;
                form.ShowDialog();
            }
        }
        private void _TestCabCabReadButton_Click(IMenuItem menuItem)
        {

        }
        private void MainForm_ActivityStateChanged(object sender, TEventValueChangedArgs<WindowActivityState> e)
        {
            var dxRibbonForm = sender as DxRibbonForm;
            var ribbonSize = dxRibbonForm?.Ribbon.Size;

            if (e.NewValue == WindowActivityState.ShowAfter)
            {
            
            }
            else if (e.NewValue == WindowActivityState.Visible)
            {

            }
            else if (e.NewValue == WindowActivityState.Active)
            {

            }

        }

        private void TitleBarDxRibbon_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            if (e.Item is null) return;
            var itemId = e.Item.ItemId;
            if (itemId == "SysMenu1")
                TitleBarDxRibbonChangeRefresh(e.Item);
            if (itemId != "SysSkin1" && itemId != "SysSkin2" && itemId != "SysMenu3" && itemId != "SysMenu3_xx")
                DxComponent.ShowMessageInfo($"Uživatel si přeje provést akci: '{e.Item.ItemId}' = '{e.Item.Text}' '{e.Item.ToolTipText}'");
        }

        private IRibbonItem[] _CreateTitleBarItems()
        {
            string resourceStruct = "svgimages/dashboards/treemap.svg";
            string resourceTree = "svgimages/dashboards/inserttreeview.svg";
            string resourceMenu1 = "svgimages/hybriddemoicons/bottompanel/hybriddemo_settings.svg";
            string resourceSysMenu = "images/setup/properties_16x16.png";

            string resourceSave = "svgimages/xaf/action_save_new.svg";
            string resourceReset = "svgimages/xaf/action_save_close.svg";
            string resourcePreview = "svgimages/print/preview.svg";
            string resourceCancel = "svgimages/hybriddemoicons/bottompanel/hybriddemo_cancel.svg";

            var sysMenu3SubItems = new ListExt<IRibbonItem>();
            sysMenu3SubItems.Add(new DataRibbonItem() { ItemId = "SysMenu3_01", Text = "Uložit pozici okna", ImageName = resourceSave });
            sysMenu3SubItems.Add(new DataRibbonItem() { ItemId = "SysMenu3_02", Text = "Resetovat pozici okna", ImageName = resourceReset });
            sysMenu3SubItems.Add(new DataRibbonItem() { ItemId = "SysMenu3_03", Text = "Zobrazit pozici okna", ImageName = resourcePreview });
            sysMenu3SubItems.Add(new DataRibbonItem() { ItemId = "SysMenu3_xx", Text = "Zavřít", ItemIsFirstInGroup = true, ImageName = resourceCancel });

            List<DataRibbonItem> sysMenuItems = new List<DataRibbonItem>();
            sysMenuItems.Add(new DataRibbonItem() { ItemId = "SysSkin1", ItemType = RibbonItemType.SkinSetDropDown });
            sysMenuItems.Add(new DataRibbonItem() { ItemId = "SysSkin2", ItemType = RibbonItemType.SkinPaletteDropDown });
            // sysMenuItems.Add(new DataRibbonItem() { ItemId = "SysMenu1", Text = "", ToolTipTitle = "Systémová akce 1", ToolTipText = "Zobrazí strukturu controlů v okně.\r\nKaždým kliknutím se tento button promění !", ImageName = resourceStruct });
            // sysMenuItems.Add(new DataRibbonItem() { ItemId = "SysMenu2", Text = "", ToolTipTitle = "Systémová akce 2", ToolTipText = "Zobrazí strukturu controllerů", ImageName = resourceTree });
            sysMenuItems.Add(new DataRibbonItem() { ItemId = "SysMenu3", Text = "", ToolTipTitle = "Systémové menu", ToolTipText = "Nabídne funkce pro uložení pozice okna", ImageName = resourceSysMenu, ItemType = RibbonItemType.Menu, SubItems = sysMenu3SubItems });

            return sysMenuItems.ToArray();
        }

        private void TitleBarDxRibbonChangeRefresh(IRibbonItem iRibbonItem)
        {
            DataRibbonItem ribbonItem = iRibbonItem as DataRibbonItem;
            if (ribbonItem is null) return;

            string resource0 = "svgimages/dashboards/treemap.svg";
            string resource1 = "svgimages/dashboards/sliceanddice.svg";
            string resource2 = "svgimages/dashboards/squarified.svg";
            string resource3 = "svgimages/dashboards/striped.svg";

            string toolTipText = "Zobrazí strukturu controlů v okně.\r\nKaždým kliknutím se tento button promění !\r\n\r\n";
            int version = ((ribbonItem.Tag is int number) ? number : 0);
            switch (version)
            {
                case 0:
                    ribbonItem.ImageName = resource1;
                    ribbonItem.ToolTipText = toolTipText + "Nyní je aktivní varianta 1";
                    ribbonItem.Tag = 1;
                    break;
                case 1:
                    ribbonItem.ImageName = resource2;
                    ribbonItem.ToolTipText = toolTipText + "Nyní je aktivní varianta 2";
                    ribbonItem.Tag = 2;
                    break;
                case 2:
                    ribbonItem.ImageName = resource3;
                    ribbonItem.ToolTipText = toolTipText + "Nyní je aktivní varianta 3";
                    ribbonItem.Tag = 3;
                    break;
                case 3:
                    ribbonItem.ImageName = resource0;
                    ribbonItem.ToolTipText = toolTipText + "Nyní je aktivní varianta 0";
                    ribbonItem.Tag = 0;
                    break;
            }
            ribbonItem.Refresh();
        }
        /// <summary>
        /// Formulář ScanForm
        /// </summary>
        private HandleScanForm _HandleScanForm;
        private void _TestDxDevExpressRibbon_Click(IMenuItem menuItem)
        {
            _TestDxRibbonFormNative(NativeRibbonForm.CreateMode.DevExpress, false);
        }
        //private void _TestDxRibbonFormClassesModalButton_Click(IMenuItem menuItem)
        //{
        //    _TestDxRibbonFormnNative(NativeRibbonForm.CreateMode.UseClasses, false);
        //}
        private void _TestDxTestRibbon_Click(IMenuItem menuItem)
        {
            _TestDxRibbonFormNative(NativeRibbonForm.CreateMode.Tests, false);
        }
        private void _TestDxAsolRibbon_Click(IMenuItem menuItem)
        {
            _TestDxRibbonFormNative(NativeRibbonForm.CreateMode.Asol, false);
        }
        //private void _TestDxRibbonFormData3ModalButton_Click(IMenuItem menuItem)
        //{
        //    _TestDxRibbonFormnNative(NativeRibbonForm.CreateMode.Asol, true);
        //}
        private void _TestDxRibbonFormNative(NativeRibbonForm.CreateMode createMode, bool useVoidSlave)
        {
            using (var ribbonForm = new NativeRibbonForm(createMode, useVoidSlave))
            {
                ribbonForm.ShowDialog();
            }
        }
        private void _TestDxBrowseStandardForm_Click(IMenuItem menuItem)
        {
            using (var dxBrowseForm = new DxBrowseStandardForm())
            {
                dxBrowseForm.WindowState = FormWindowState.Maximized;
                dxBrowseForm.ShowDialog();
            }
        }
        private void _TestDxBrowseVirtualForm_Click(IMenuItem menuItem)
        {
            using (var dxBrowseForm = new DxBrowseVirtualForm())
            {
                dxBrowseForm.WindowState = FormWindowState.Maximized;
                dxBrowseForm.ShowDialog();
            }
        }
        private void _TestDxBrowseForm_Click(IMenuItem menuItem)
        {
            using (var dxBrowseForm = new DxBrowseForm())
            {
                dxBrowseForm.WindowState = FormWindowState.Maximized;
                dxBrowseForm.ShowDialog();
            }
        }
        #endregion
        #region Ribbon a SvgImage kombinace
        private DataRibbonPage CreateRibbonSvgImagesPage()
        {
            var commonSkin = DS.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default);
            var ribbonSkin = DxComponent.GetSkinInfo(SkinElementColor.RibbonSkins);

            var palette = new DevExpress.Utils.Svg.SvgPalette();
            palette.Colors.Add(new DevExpress.Utils.Svg.SvgColor("modrá", Color.FromArgb(44, 55, 88)));

            ribbonSkin.CustomSvgPalettes.Add(new DevExpress.Utils.Svg.SvgPaletteKey(0, "key0"), palette);

            // commonSkin.CustomSvgPalettes.Add(new DevExpress.Utils.Svg.SvgPaletteKey(0, "key0"), palette);
            DevExpress.LookAndFeel.LookAndFeelHelper.ForceDefaultLookAndFeelChanged();

            AddTestDxResources();

            _SvgCombineData = new object[4];

            DataRibbonPage page = new DataRibbonPage() { PageText = "SVG IKONY" };
            _SvgCombineRibbonGroup = new DataRibbonGroup() { GroupText = "Kombinace více ikon do jedné" };
            _SvgCombineRibbonGroup.Items.Add(new DataRibbonItem() { Text = "Základ", ItemType = RibbonItemType.Menu, SubItems = CreateRibbonSvgMenu0(), RibbonStyle = RibbonItemStyles.Large });
            _SvgCombineRibbonGroup.Items.Add(new DataRibbonItem() { Text = "Umístění", ItemType = RibbonItemType.Menu, SubItems = CreateRibbonSvgMenu1(), RibbonStyle = RibbonItemStyles.Large });
            _SvgCombineRibbonGroup.Items.Add(new DataRibbonItem() { Text = "Velikost", ImageName = "svgimages/dashboards/zoom2.svg", ItemType = RibbonItemType.Menu, SubItems = CreateRibbonSvgMenu2(), RibbonStyle = RibbonItemStyles.Large });
            _SvgCombineRibbonGroup.Items.Add(new DataRibbonItem() { Text = "Přídavek", ItemType = RibbonItemType.Menu, SubItems = CreateRibbonSvgMenu3(), RibbonStyle = RibbonItemStyles.Large });
            _SvgCombineRibbonGroup.Items.Add(new DataRibbonItem() { Text = "Výsledek", ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large, ClickAction = ClickRibbonSvgReadOnly, ItemIsFirstInGroup = true });
            page.Groups.Add(_SvgCombineRibbonGroup);

            string toolTipText = "Kliknutím na ikonu bude zobrazen obsah SVG ikony; kliknutím s klávesou CTRL bude do ikony načten editovaný text.";
            _SvgDjColorRibbonGroup = new DataRibbonGroup() { GroupText = "Ukázky ikon Dj-Colorized" };
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/robot-arm-filled", Text = "robot", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/safe", Text = "safe", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/salary", Text = "salary", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/sale-blue-filled", Text = "sale-blue", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/store-closed", Text = "store", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/symbol-forbidden-script", Text = "forbidden", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/symbol-refresh", Text = "refresh", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/symbol-remove", Text = "remove", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/symbol-update", Text = "update", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/symbol-upload", Text = "upload", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });

            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/table", Text = "table" });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/text", Text = "text" });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/time", Text = "time" });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/toolbox", Text = "toolbox" });
            // _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/DjColorized/user", Text = "user" });

            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/insertlistbox.svg", Text = "insertlistbox", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable, ItemIsFirstInGroup = true });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/new.svg", Text = "new", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/parameters.svg", Text = "parameters", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });
            _SvgDjColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/showlegendinsidehorizontalbottomright.svg", Text = "bottomright", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgEditable });


            /*
            string[] resources = new string[]
            {
    "svgimages/dashboards/insertlistbox.svg",
    "svgimages/dashboards/new.svg",
    "svgimages/dashboards/parameters.svg",
    "svgimages/dashboards/showlegendinsidehorizontalbottomleft.svg",
    "svgimages/dashboards/showlegendinsidehorizontalbottomright.svg",
    "svgimages/dashboards/showlegendinsidehorizontaltopcenter.svg",
    "svgimages/dashboards/showlegendinsidehorizontaltopleft.svg",
    "svgimages/dashboards/showlegendinsidehorizontaltopright.svg"
            };
            */

            page.Groups.Add(_SvgDjColorRibbonGroup);

            _SvgOrigColorRibbonGroup = new DataRibbonGroup() { GroupText = "Ukázky ikon ASOL original + DarkConvertor" };
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/robot-arm-filled", Text = "robot", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/safe", Text = "safe", ClickAction = ClickRibbonSvgReadOnly });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/salary", Text = "salary", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/sale-blue-filled", Text = "sale-blue", ClickAction = ClickRibbonSvgReadOnly });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/store-closed", Text = "store", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/symbol-forbidden-script", Text = "forbidden", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/symbol-refresh", Text = "refresh", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/symbol-remove", Text = "remove", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/symbol-update", Text = "update", ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/symbol-upload", Text = "upload", ClickAction = ClickRibbonSvgReadOnly });

            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/table", Text = "table" });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/text", Text = "text" });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/time", Text = "time" });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/toolbox", Text = "toolbox" });
            // _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "SvgTest/AsolOriginal/user", Text = "user" });

            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/insertlistbox.svg", Text = "insertlistbox", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgReadOnly, ItemIsFirstInGroup = true });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/new.svg", Text = "new", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/parameters.svg", Text = "parameters", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgReadOnly });
            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/showlegendinsidehorizontalbottomright.svg", Text = "bottomright", ToolTipText = toolTipText, ClickAction = ClickRibbonSvgReadOnly });


            _SvgOrigColorRibbonGroup.Items.Add(new DataRibbonItem() { ImageName = "pic_0/Win/AppIcons/Nephrite_15.ico", Text = "Nephrite_15.ico", ItemIsFirstInGroup = true });

            page.Groups.Add(_SvgOrigColorRibbonGroup);

            ClickRibbonSvgMenuAny(0, 0, false);
            ClickRibbonSvgMenuAny(1, 8, false);
            ClickRibbonSvgMenuAny(2, 3, false);
            ClickRibbonSvgMenuAny(3, 0, false);

            this.DxRibbon.UseLazyContentCreate = DxRibbonControl.LazyContentMode.CreateAllItems;           // Potřebuji, aby v době FirstShown existovaly všechny prvky Ribbonu (i na druhé Page), protože do Result buttonu chci vyrenderovat kombinovanou ikonu v metodě ApplyRibbonSvgImagesResult()

            return page;
        }
        private void AddTestDxResources()
        {
            string directory = SearchDirectory(1, "Images\\SvgTest");
            if (directory == null)
                directory = SearchDirectory(0, "Images\\SvgTest");
            if (directory != null)
                DxApplicationResourceLibrary.AddResources(DataResources.GetResourcesFromDirectory(directory));
        }
        private string SearchDirectory(int upDirs, string subDir)
        {
            string path = DxComponent.ApplicationPath;
            for (int i = 0; i < upDirs && !String.IsNullOrEmpty(path); i++)
                path = System.IO.Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(path)) return null;
            if (!String.IsNullOrEmpty(subDir))
                path = System.IO.Path.Combine(path, subDir);
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(path);
            if (!dirInfo.Exists) return null;
            return path;
        }
        private void ApplyRibbonSvgImagesResult(bool showStatus)
        {
            _SvgCombineRunAction(showStatus);
        }
        private void ClickRibbonSvgMenu0(IMenuItem item) { ClickRibbonSvgMenuAny(0, item as DataRibbonItem, true); }
        private void ClickRibbonSvgMenu1(IMenuItem item) { ClickRibbonSvgMenuAny(1, item as DataRibbonItem, true); }
        private void ClickRibbonSvgMenu2(IMenuItem item) { ClickRibbonSvgMenuAny(2, item as DataRibbonItem, true); }
        private void ClickRibbonSvgMenu3(IMenuItem item) { ClickRibbonSvgMenuAny(3, item as DataRibbonItem, true); }
        /// <summary>
        /// Kliknutí na button v Ribbonu, která dovoluje Read a Write
        /// </summary>
        /// <param name="item"></param>
        private void ClickRibbonSvgEditable(IMenuItem item)
        {
            ClickRibbonSvgGetSet(item, true);
        }
        /// <summary>
        /// Kliknutí na button v Ribbonu, která dovoluje Read only
        /// </summary>
        /// <param name="item"></param>
        private void ClickRibbonSvgReadOnly(IMenuItem item)
        {
            ClickRibbonSvgGetSet(item, false);
        }
        /// <summary>
        /// Kliknutí na button v Ribbonu, která dovoluje Read a volitelně Write (<paramref name="enableSet"/> je true)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="enableSet"></param>
        private void ClickRibbonSvgGetSet(IMenuItem item, bool enableSet)
        {
            bool isCtrl = (Control.ModifierKeys == Keys.Control);
            var barItem = (item as IRibbonItem)?.RibbonItem;
            if (barItem is null) return;

            ActivatePage(8, true);

            if (!isCtrl)
                _RunDjColorizeGetImage(barItem, enableSet);
            else if (enableSet)
                _RunDjColorizeSetImage(barItem);
        }
        /// <summary>
        /// Z dodaného BarItem načte jeho SVG ikonu a zobrazí ji v textovém editoru a ve velkém Image
        /// </summary>
        /// <param name="barItem"></param>
        /// <param name="enableSet">Pokud je true, pak tento BarItem uloží jako případný cíl pro Set metodu. Pokud je false, ponechá tam dosavadní cíl.</param>
        private void _RunDjColorizeGetImage(XB.BarItem barItem, bool enableSet)
        {
            DxSvgImage dxSvgImage = null;
            if (barItem.ImageOptions.SvgImage != null)
                dxSvgImage = DxSvgImage.Create(barItem.ImageOptions.SvgImage);
            else if (barItem.ImageOptions.LargeImageIndex >= 0 && barItem.LargeImages is DxSvgImageCollection svgImages)
                dxSvgImage = DxSvgImage.Create(svgImages[barItem.ImageOptions.LargeImageIndex]);

            if (dxSvgImage != null)
            {
                string xmlContent = dxSvgImage.XmlContent;
                _SvgIconXmlText.Text = xmlContent;
                EditorImageName = xmlContent;
                if (enableSet)
                    _DjColorizedBarItem = barItem;
            }
        }
        /// <summary>
        /// Editovanou ikonu uloží do aktivního BarItemu (posledně zobrazený)
        /// </summary>
        private void _RunDjColorizeSetImage()
        {
            if (_DjColorizedBarItem != null)
                _RunDjColorizeSetImage(_DjColorizedBarItem);
        }
        /// <summary>
        /// Editovanou ikonu uloží do dodaného BarItemu
        /// </summary>
        /// <param name="barItem"></param>
        private void _RunDjColorizeSetImage(XB.BarItem barItem)
        {
            if (barItem is null) return;
            string xmlContent = _SvgIconXmlText.Text;
            if (String.IsNullOrEmpty(xmlContent)) return;
            try
            {
                DxComponent.ApplyImage(barItem.ImageOptions, xmlContent, null, ResourceImageSizeType.Large);
                //  barItem.ImageOptions.Reset();
                //  barItem.ImageOptions.SvgImage = DxSvgImage.Create(xmlContent);
            }
            catch { }
        }
        /// <summary>
        /// Button, do kterého se provede setování obrázku (z něj byl naposledy načten obrázek pro režim Read a Write)
        /// </summary>
        private XB.BarItem _DjColorizedBarItem;
        private void ClickRibbonSvgMenuAny(int mainItemIndex, int subItemIndex, bool showStatus)
        {
            DataRibbonItem mainItem = _SvgCombineRibbonGroup.Items[mainItemIndex] as DataRibbonItem;
            if (mainItem == null || mainItem.SubItems == null || subItemIndex < 0 || subItemIndex >= mainItem.SubItems.Count) return;
            DataRibbonItem subItem = mainItem.SubItems[subItemIndex] as DataRibbonItem;
            ClickRibbonSvgMenuAny(mainItemIndex, subItem, showStatus);
        }
        private void ClickRibbonSvgMenuAny(int mainItemIndex, DataRibbonItem subItem, bool showStatus)
        {
            _SvgCombineData[mainItemIndex] = subItem.Tag ?? subItem.ImageName;

            DataRibbonItem mainItem = _SvgCombineRibbonGroup.Items[mainItemIndex] as DataRibbonItem;
            switch (mainItemIndex)
            {
                case 0:
                    mainItem.ImageName = subItem.ImageName;
                    this.EditorImageName = subItem.ImageName;
                    break;
                case 1:
                    mainItem.ImageName = subItem.ImageName;
                    break;
                case 2:
                    mainItem.Text = "Velikost " + _SvgCombineData[2].ToString() + "%";
                    break;
                case 3:
                    mainItem.ImageName = subItem.ImageName;
                    this.EditorImageName = subItem.ImageName;
                    break;
            }
            mainItem.ToolTipText = subItem.Text;

            if (mainItem.RibbonItem != null)
                this.DxRibbon.RefreshItem(mainItem, true);

            _SvgCombineRunAction(showStatus);

            if (mainItemIndex == 3)
                this.EditorImageName = _SvgCombineResultName;
        }
        private DataRibbonGroup _SvgCombineRibbonGroup;
        private DataRibbonGroup _SvgDjColorRibbonGroup;
        private DataRibbonGroup _SvgOrigColorRibbonGroup;
        /// <summary>
        /// Obsahuje prvky: ImageName základ; ContentAlignment; Size; ImageName přídavek
        /// </summary>
        private object[] _SvgCombineData;
        private ListExt<IRibbonItem> CreateRibbonSvgMenu0()
        {
            ListExt<IRibbonItem> subItems = new ListExt<IRibbonItem>();
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/printexcludeevaluations.svg", Text = "printexcludeevaluations", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/printincludeevaluations.svg", Text = "printincludeevaluations", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/save.svg", Text = "save", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/select%20all.svg", Text = "select all", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/select%20all2.svg", Text = "select all2", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/contacts/mail.svg", Text = "mail", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/print/tasklist.svg", Text = "tasklist", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/ui/window/window.svg", Text = "window", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/diagramicons/orientation/album.svg", Text = "album", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/diagramicons/orientation/portrait.svg", Text = "portrait", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/richedit/columns.svg", Text = "columns", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/richedit/columnsone.svg", Text = "columnsone", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/richedit/columnstwo.svg", Text = "columnstwo", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/richedit/columnsthree.svg", Text = "columnsthree", ClickAction = ClickRibbonSvgMenu0 });

            subItems.Add(new DataRibbonItem() { ImageName = "pic_0/Menu/frmnew", Text = "frmnew", ClickAction = ClickRibbonSvgMenu0, ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ImageName = "pic_0/Menu/frmdel", Text = "frmdel", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic_0/Menu/frmcopy", Text = "frmcopy", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/address-book-large.svg", Text = "address-book-large", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/alert-filled-large.svg", Text = "alert-filled-large", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/anchor-large.svg", Text = "anchor-large", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/archive-box-large.svg", Text = "archive-box-large", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic_0/Bar_s/vyhledavani-large.svg", Text = "vyhledavani-large", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/waste-bin", Text = "waste-bin", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/window", Text = "window", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "pic/turbine-2-large.svg", Text = "turbine-2-large", ClickAction = ClickRibbonSvgMenu0 });

            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/poznamkovy_blok", Text = "poznamkovy_blok", ClickAction = ClickRibbonSvgMenu0, ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/kalendar", Text = "kalendar", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/asset", Text = "asset", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/asset-cancel-2", Text = "asset-cancel-2", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/asset-filled-ok-2", Text = "asset-filled-ok-2", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/attach-1", Text = "attach-1", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/attach-1-add-2", Text = "attach-1-add-2", ClickAction = ClickRibbonSvgMenu0 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/backspace", Text = "backspace", ClickAction = ClickRibbonSvgMenu0 });

            return subItems;
        }
        private ListExt<IRibbonItem> CreateRibbonSvgMenu1()
        {
            ListExt<IRibbonItem> subItems = new ListExt<IRibbonItem>();
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmenttopleft.svg", Text = "Nahoře vlevo", Tag = (int)ContentAlignment.TopLeft, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmenttopcenter.svg", Text = "Nahoře uprostřed", Tag = (int)ContentAlignment.TopCenter, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmenttopright.svg", Text = "Nahoře vpravo", Tag = (int)ContentAlignment.TopRight, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentcenterleft.svg", Text = "Uprostřed vlevo", Tag = (int)ContentAlignment.MiddleLeft, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentcentercenter.svg", Text = "Uprostřed", Tag = (int)ContentAlignment.MiddleCenter, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentcenterright.svg", Text = "Uprostřed vpravo", Tag = (int)ContentAlignment.MiddleRight, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentbottomleft.svg", Text = "Dole vlevo", Tag = (int)ContentAlignment.BottomLeft, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentbottomcenter.svg", Text = "Dole uprostřed", Tag = (int)ContentAlignment.BottomCenter, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/dashboards/alignmentbottomright.svg", Text = "Dole vpravo", Tag = (int)ContentAlignment.BottomRight, ClickAction = ClickRibbonSvgMenu1 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/icon%20builder/actions_arrow2left.svg", Text = "Pouze ZÁKLAD", Tag = (int)2048, ClickAction = ClickRibbonSvgMenu1, ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/icon%20builder/actions_arrow2right.svg", Text = "Pouze PŘÍDAVEK", Tag = (int)4096, ClickAction = ClickRibbonSvgMenu1 });
            return subItems;
        }
        private ListExt<IRibbonItem> CreateRibbonSvgMenu2()
        {
            ListExt<IRibbonItem> subItems = new ListExt<IRibbonItem>();
            subItems.Add(new DataRibbonItem() { Text = "Velikost 25%", Tag = (int)25, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 33%", Tag = (int)33, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 40%", Tag = (int)40, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 50%", Tag = (int)50, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 60%", Tag = (int)60, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 75%", Tag = (int)75, ClickAction = ClickRibbonSvgMenu2 });
            subItems.Add(new DataRibbonItem() { Text = "Velikost 100%", Tag = (int)100, ClickAction = ClickRibbonSvgMenu2 });
            return subItems;
        }
        private ListExt<IRibbonItem> CreateRibbonSvgMenu3()
        {
            ListExt<IRibbonItem> subItems = new ListExt<IRibbonItem>();
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/about.svg", Text = "about", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/add.svg", Text = "add", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/apply.svg", Text = "apply", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/close.svg", Text = "close", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/refresh.svg", Text = "refresh", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/remove.svg", Text = "remove", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/reset2.svg", Text = "reset2", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/search.svg", Text = "search", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/other/a.svg", Text = "a", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/other/b.svg", Text = "b", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/other/brand.svg", Text = "brand", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/cut.svg", Text = "cut", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/actions/delete.svg", Text = "delete", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/contacts/mail.svg", Text = "mail", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/contacts/mail1.svg", Text = "mail1", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/contacts/mail2.svg", Text = "mail2", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "devav/contacts/mail3.svg", Text = "mail3", ClickAction = ClickRibbonSvgMenu3 });

            subItems.Add(new DataRibbonItem() { ImageName = "svgimages/icon%20builder/actions_add.svg", Text = "actions_add", ClickAction = ClickRibbonSvgMenu3, ItemIsFirstInGroup = true });

            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/suffix-add", Text = "suffix-add", ClickAction = ClickRibbonSvgMenu3, ItemIsFirstInGroup = true });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/suffix-cancel", Text = "suffix-cancel", ClickAction = ClickRibbonSvgMenu3 });
            subItems.Add(new DataRibbonItem() { ImageName = "svgtest/suffix-ok", Text = "suffix-ok", ClickAction = ClickRibbonSvgMenu3 });

            return subItems;
        }
        private void _SvgCombineRunAction(bool showStatus)
        {
            string svgImageName0 = _SvgCombineData[0] as string;
            int svgCombine = (_SvgCombineData[1] is int ? (int)_SvgCombineData[1] : -1);
            int svgSize = (_SvgCombineData[2] is int ? (int)_SvgCombineData[2] : 40);
            string svgImageName1 = _SvgCombineData[3] as string;
            if (String.IsNullOrEmpty(svgImageName0) || String.IsNullOrEmpty(svgImageName1)) return;

            DataRibbonItem resultItem = _SvgCombineRibbonGroup.Items[4] as DataRibbonItem;
            var barItem = resultItem.RibbonItem;
            if (barItem is null) return;

            if (svgCombine == 1 || svgCombine == 2 || svgCombine == 4 || svgCombine == 16 || svgCombine == 32 || svgCombine == 64 || svgCombine == 256 || svgCombine == 512 || svgCombine == 1024)
                _SvgCombineRunActionCombine(svgImageName0, (ContentAlignment)svgCombine, svgSize, svgImageName1, barItem, showStatus);
            else if (svgCombine == 2048)
                _SvgCombineRunActionSet(svgImageName0, barItem);
            else if (svgCombine == 4096)
                _SvgCombineRunActionSet(svgImageName1, barItem);
        }
        private void _SvgCombineRunActionCombine(string svgImageName0, ContentAlignment contentAlignment, int percent, string svgImageName1, XB.BarItem barItem, bool showStatus)
        {
            // Uložíme obrázky do souboru:
            _SaveSvgImage(svgImageName0);
            _SaveSvgImage(svgImageName1);

            // Vstupní obrázky:
            SvgImageArrayInfo svgImageArray = new SvgImageArrayInfo(svgImageName0);
            svgImageArray.Add(svgImageName1, contentAlignment, percent);
            // Výsledný string:
            _SvgCombineResultName = svgImageArray.Key;

            bool ok = SvgImageArrayInfo.TryDeserialize(_SvgCombineResultName, out var imgCopy);
            string copyName = imgCopy?.Key;

            _SaveSvgImage(copyName);

            string info = "";

            var startTime = DxComponent.LogTimeCurrent;
            bool isStandard = true;
            if (isStandard)
            {   // Standardní cesta:
                DataRibbonItem resultItem = _SvgCombineRibbonGroup.Items[4] as DataRibbonItem;
                resultItem.ImageName = _SvgCombineResultName;
                this.DxRibbon.RefreshItem(resultItem, true);

                // DxComponent.ApplyImage(barItem.ImageOptions, imageName);

                info = "Standard; ";
            }
            else
            {   // Přímo voláme SvgImageArraySupport.CreateSvgImage() :
                // Zpracování:
                var svgImageOut = SvgImageSupport.CreateSvgImage(svgImageArray);

                // Výstup:
                barItem.ImageOptions.SvgImage = svgImageOut;

                info = "DirectCall; ";
            }
            decimal microsecs = DxComponent.LogGetTimeElapsed(startTime);
            if (showStatus)
                this._StatusStartLabel.Caption = $"Key: {svgImageArray.Key}; {info}Time: {microsecs} microsecs";
        }
        private string _SvgCombineResultName;
        private void _SaveSvgImage(string svgImageName)
        {
            DevExpress.Utils.Svg.SvgImage svgImage = DxComponent.CreateVectorImage(svgImageName);
            if (svgImage == null) return;
            string path = @"C:\CSharp\TestDevExpress\SvgImages";
            if (!System.IO.Directory.Exists(path))
                path = @"C:\DavidPrac\VsProjects\SvgSamples";
            if (!System.IO.Directory.Exists(path))
                return;
            string name = svgImageName
                .Replace("/", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("«", "_")
                .Replace("»", "_")
                .Replace("\\", "_");
            string fullName = System.IO.Path.Combine(path, name);
            if (!fullName.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
                fullName += ".svg";
            try { svgImage.Save(fullName); }
            catch { }
        }
        private void _SvgCombineRunActionSet(string svgImageName, XB.BarItem barItem)
        {
            DevExpress.Utils.Svg.SvgImage svgImage = DxComponent.CreateVectorImage(svgImageName);
            barItem.ImageOptions.SvgImage = svgImage;
        }
        #endregion
        #region Hlavní záložkovník + přepínání testovacích stránek
        private void InitTabPages()
        {
            _MainTabs = new DxTabPane() { Dock = DockStyle.Fill };
            _MainTabs.SelectedPageChanging += _MainTabs_SelectedPageChanging;
            _MainTabs.SelectedPageChanged += _MainTabs_SelectedPageChanged;
            DxMainPanel.Controls.Add(_MainTabs);

            _PreparedPages = new Dictionary<int, PageInfo>();
        }
        private void AddNewPage(string pageText,
            Action<DxPanelControl> prepareMethod, Action activateMethod = null, Action deactivateMethod = null,
            string pageToolTip = null, string pageImageName = null)
        {
            int index = _MainTabs.Pages.Count;
            DataPageItem dataPageItem = new DataPageItem() { ItemId = pageText, Text = pageText, ToolTipText = pageToolTip };
            _MainTabs.AddPage(dataPageItem);
            PageInfo pageInfo = new PageInfo() { Index = index, Page = dataPageItem.PageControl, PrepareMethod = prepareMethod, ActivateMethod = activateMethod, DeactivateMethod = deactivateMethod };

            _PreparedPages.Add(index, pageInfo);
        }
        private void _MainTabs_SelectedPageChanging(object sender, DXN.SelectedPageChangingEventArgs e)
        {
            int pageIndex = _MainTabs.Pages.IndexOf(e.Page);
            _MainTabsCheckPrepared(pageIndex);
        }
        private void _MainTabs_SelectedPageChanged(object sender, DXN.SelectedPageChangedEventArgs e)
        {
            OnActivatePage(CurrentPageIndex);
        }
        /// <summary>
        /// Index aktuální stránky
        /// </summary>
        protected int CurrentPageIndex { get { return _MainTabs.SelectedPageIndex; } set { _MainTabs.SelectedPageIndex = value; } }
        private void ActivatePage(int pageIndex, bool forceEvent)
        {
            CurrentPageIndex = pageIndex;
            if (forceEvent)
                OnActivatePage(pageIndex);
        }
        /// <summary>
        /// Provede se po aktivaci stránky daného indexu. Volá události DeActivate***Page a Activate***Page konkrétní stránky.
        /// </summary>
        /// <param name="pageIndex"></param>
        private void OnActivatePage(int pageIndex)
        {
            OnDeactivatePage(_LastActivatedPage);

            _MainTabsCheckPrepared(pageIndex);
            if (pageIndex >= 0 && this._PreparedPages.TryGetValue(pageIndex, out PageInfo pageInfo))
                pageInfo.ActivateMethod?.Invoke();

            _LastActivatedPage = pageIndex;
            RefreshLog();
        }
        /// <summary>
        /// Provede se při deaktivaci stránky daného indexu. Volá události DeActivate***Page
        /// </summary>
        /// <param name="pageIndex"></param>
        private void OnDeactivatePage(int pageIndex)
        {
            if (pageIndex >= 0 && this._PreparedPages.TryGetValue(pageIndex, out PageInfo pageInfo))
                pageInfo.DeactivateMethod?.Invoke();

            CurrentLogControl = null;              // Konkrétní stránka ať si to nastaví v následující metodě...
            _LastActivatedPage = -1;
        }
        /// <summary>
        /// Zajistí provedení přípravy obsahu dané stránky
        /// </summary>
        /// <param name="pageIndex"></param>
        private void _MainTabsCheckPrepared(int pageIndex)
        {
            if (pageIndex >= 0 && this._PreparedPages.TryGetValue(pageIndex, out PageInfo pageInfo))
            {
                if (pageInfo.Panel == null)
                {
                    pageInfo.Panel = DxComponent.CreateDxPanel(pageInfo.Page, DockStyle.Fill, DevExpress.XtraEditors.Controls.BorderStyles.NoBorder);
                    pageInfo.PrepareMethod?.Invoke(pageInfo.Panel);
                }
            }
        }

        private Dictionary<int, PageInfo> _PreparedPages;
        private int _LastActivatedPage = -1;
        private DxTabPane _MainTabs;
        private class PageInfo
        {
            public int Index;
            public Control Page;
            public DxPanelControl Panel;
            public Action<DxPanelControl> PrepareMethod;
            public Action ActivateMethod;
            public Action DeactivateMethod;
        }
        #endregion
        #region Kontextové menu
        private void InitPopupPage()
        {
            AddNewPage("Kontextové menu", PreparePopupPage);
        }
        private DxPanelControl _PanelPopupPage;
        private void PreparePopupPage(DxPanelControl panel)
        {
            _PanelPopupPage = panel;
            System.Windows.Forms.Panel panel1 = new Panel() { Dock = DockStyle.None, Bounds = new Rectangle(20, 20, 250, 280), BorderStyle = BorderStyle.Fixed3D };
            panel1.BackColor = Color.FromArgb(0, 0, 0, 0);
            panel.Controls.Add(panel1);
            DxComponent.CreateDxSimpleButton(3, 3, 200, 45, panel1, "XB.PopupMenu", PopupPageXBPopupClick);
            DxComponent.CreateDxSimpleButton(3, 54, 200, 45, panel1, "XB.RadialMenu", PopupPageXBRadialClick);
            DxComponent.CreateDxSimpleButton(3, 105, 200, 45, panel1, "DM.Menu", PopupPageDXPopupMenuClick);
            DxComponent.CreateDxSimpleButton(3, 156, 200, 45, panel1, "Win.ToolStripMenu", PopupPageWinMenuClick);
            DxComponent.CreateDxSimpleButton(3, 227, 200, 45, panel1, "AsolDx PopupMenu", PopupPageAsolDxPopupMenuClick);

            // TextBox s tlačítky:
            var editText = new DxButtonEdit() { Bounds = new Rectangle(300, 20, 250, 20), ButtonImage = @"\pic_0\UI\DynRel\StaticRel" };

            /*
            // Tlačítko:
            editText.Properties.Buttons.Clear();
            var relationButton = new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            bool isDocument = false;
            string imageName = isDocument ? @"\pic_0\UI\DynRel\StaticRelExtDoc" : @"\pic_0\UI\DynRel\StaticRel";         // Jméno ikony
            DxComponent.ApplyImage(relationButton.ImageOptions, imageName, sizeType: ResourceImageSizeType.Small);
            editText.Properties.Buttons.Add(relationButton);
            */

            // Eventy:
            editText.DoubleClick += _EditText_DoubleClick;
            editText.ButtonClick += _EditText_ButtonClick;
            
            editText.Text = "Krabička plechová";
            panel.Controls.Add(editText);
        }

        private void _EditText_DoubleClick(object sender, EventArgs e)
        {
            DxComponent.ShowMessageInfo("Relation text DoubleClick");
        }
        private void _EditText_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            DxComponent.ShowMessageInfo("Relation text Button Click");
        }

        #region XB.PopupMenu
        private void PopupPageXBPopupClick(object sender, EventArgs e)
        {
            XB.PopupMenu popup = new XB.PopupMenu(_BarManager);
            // pm.MenuCaption = "Kontextové menu";    Používám BarHeaderItem !
            // pm.ShowCaption = true;
            //pm.ShowNavigationHeader = DevExpress.Utils.DefaultBoolean.True;
            //pm.DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True;
            //pm.MenuAppearance.AppearanceMenu.Normal.BackColor = Color.Violet;
            //pm.MenuAppearance.HeaderItemAppearance.BackColor = Color.LightBlue;
            _BarManager.ShowScreenTipsInMenus = true;

            popup.DrawMenuSideStrip = DevExpress.Utils.DefaultBoolean.True;
            popup.DrawMenuRightIndent = DevExpress.Utils.DefaultBoolean.True;
            popup.MenuDrawMode = XB.MenuDrawMode.SmallImagesText;
            popup.Name = "menu";
            
            AddXbPopupBigItems(popup);

            XB.BarHeaderItem bh1 = new XB.BarHeaderItem() { Caption = "Základní" };
            bh1.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
            bh1.OptionsMultiColumn.ItemDisplayMode = DM.MultiColumnItemDisplayMode.Image;
            bh1.OptionsMultiColumn.ColumnCount = 6;
            bh1.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Left;
            bh1.OptionsMultiColumn.ImageVerticalAlignment = DevExpress.Utils.Drawing.ItemVerticalAlignment.Top;

            popup.AddItem(bh1);
            popup.AddItem(new XB.BarButtonItem(_BarManager, "První") { Hint = "Hint k položce", Glyph = DxComponent.CreateBitmapImage("Images/Actions24/db_add(24).png") });
            popup.AddItem(new XB.BarButtonItem(_BarManager, "Druhý") { ButtonStyle = XB.BarButtonStyle.Check, Glyph = DxComponent.CreateBitmapImage("Images/Actions24/dialog-close(24).png"), PaintStyle = XB.BarItemPaintStyle.Caption });

            XB.BarButtonItem bi3 = new XB.BarButtonItem(_BarManager, "Třetí&nbsp;<b>zvýrazněný</b> a <i>kurzivový</i> <u>text</u>");
            bi3.Glyph = DxComponent.CreateBitmapImage("Images/Actions24/arrow-right-double-2(24).png");
            bi3.ShortcutKeyDisplayString = "Ctrl+F3";
            bi3.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            popup.AddItem(bi3);
            bi3.Links[0].BeginGroup = true;

            XB.BarHeaderItem bh2 = new XB.BarHeaderItem() { Caption = "Rozšiřující" };
            popup.AddItem(bh2);
            XB.BarCheckItem bbs = new XB.BarCheckItem(_BarManager) { Caption = "CheckItem zkouška", Name = "CheckItem", CheckBoxVisibility = XB.CheckBoxVisibility.BeforeText, CheckStyle = XB.BarCheckStyles.Standard };
            bbs.Checked = CheckItemChecked;
            popup.AddItem(bbs);

            XB.BarButtonItem bei = new XB.BarButtonItem(_BarManager, "BarButtonItem with Tip");
            bei.SuperTip = new DevExpress.Utils.SuperToolTip();
            bei.SuperTip.Items.AddTitle("NÁPOVĚDA");
            bei.SuperTip.Items.AddSeparator();
            var superItem = bei.SuperTip.Items.Add("BarButtonItem SuperTip");
            superItem.ImageOptions.Image = DxComponent.CreateBitmapImage("Images/Actions24/call-start(24).png");

            bei.ItemAppearance.Normal.BackColor = Color.PaleVioletRed;
            popup.AddItem(bei);

            XB.BarButtonGroup bbg = new XB.BarButtonGroup(_BarManager)
            {
                Caption = "BarButtonGroup",
                ButtonGroupsLayout = XB.ButtonGroupsLayout.Default,
                Border = DevExpress.XtraEditors.Controls.BorderStyles.Style3D,
                MenuCaption = "Caption BarButtonGroup",
            };
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "1/4 in container") { Glyph = DxComponent.CreateBitmapImage("Images/Actions24/distribute-horizontal-x(24).png"), SuperTip = DxComponent.CreateDxSuperTip("1/4 in container", "první ze čtyř v kontejneru") });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "2/4 in container") { Glyph = DxComponent.CreateBitmapImage("Images/Actions24/distribute-horizontal-left(24).png"), SuperTip = DxComponent.CreateDxSuperTip("2/4 in container", "druhý ze čtyř v kontejneru") });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "3/4 in container") { Glyph = DxComponent.CreateBitmapImage("Images/Actions24/distribute-horizontal-right(24).png"), SuperTip = DxComponent.CreateDxSuperTip("3/4 in container", "třetí ze čtyř v kontejneru") });
            bbg.AddItem(new XB.BarButtonItem(_BarManager, "4/4 in container") { Glyph = DxComponent.CreateBitmapImage("Images/Actions24/distribute-horozontal-page(24).png"), SuperTip = DxComponent.CreateDxSuperTip("4/4 in container", "poslední ze čtyř v kontejneru") });
            popup.AddItem(bbg);


            XB.BarHeaderItem bh3 = new XB.BarHeaderItem() { Caption = "Podřízené funkce on demand..." };
            popup.AddItem(bh3);
            XB.BarSubItem bsi = new XB.BarSubItem(_BarManager, "BarButtonGroup");
            // bbg.GetItemData += Bbg_GetItemData;                           // Tudy to chodí při každém rozsvícení MainMenu
            bsi.Popup += Bbg_Popup;                                       // Tudy to chodí při každém rozbalení SubMenu
            bsi.ItemAppearance.Normal.ForeColor = Color.Violet;
            bsi.Tag = "RELOAD";
            popup.AddItem(bsi);

            XB.BarHeaderItem bh4 = new XB.BarHeaderItem() { Caption = "Funkce se načítají...", Tag = "Funkce:" };
            bsi.AddItem(bh4);
            // XB.BarButtonItem bf0 = new XB.BarButtonItem(_BarManager, "funkce se načítají...");
            // bbg.AddItem(bf0);

            var links = popup.ItemLinks;
            links[2].Item.Enabled = false;
            links[7].Item.Caption = "Hlavička se změněným textem";


            _BarManager.SetPopupContextMenu(this, popup);
            popup.ShowPopup(_BarManager, Control.MousePosition);

        }
        private void AddXbPopupBigItems(XB.PopupMenu popup)
        {
            var header = new XB.BarHeaderItem();
            header.Caption = "Základní funkce";
            header.RibbonStyle = XR.RibbonItemStyles.Large;
            header.PaintStyle = XB.BarItemPaintStyle.CaptionInMenu;

            header.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
            header.OptionsMultiColumn.ColumnCount = 6;
            header.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Left;
            header.OptionsMultiColumn.ItemDisplayMode = DM.MultiColumnItemDisplayMode.Image;
            header.OptionsMultiColumn.UseMaxItemWidth = DevExpress.Utils.DefaultBoolean.False;
            popup.AddItem(header);


            string[] resources = new string[]
            {
    "images/chart/bubble3d_32x32.png",
    "images/chart/clusteredbar_32x32.png",
    "images/chart/clusteredbar3d_32x32.png",
    "images/chart/clusteredcolumn_32x32.png",
    "images/chart/clusteredcone_32x32.png",
    "images/chart/clusteredcylinder_32x32.png",
    "images/chart/clusteredhorizontalcone_32x32.png"
            };

            var b1a = new XB.BarButtonItem(_BarManager, "B1a");
            b1a.Glyph = DxComponent.GetBitmapImage(resources[0], ResourceImageSizeType.Large);
            b1a.ButtonStyle = XB.BarButtonStyle.Default;
            b1a.PaintStyle = XB.BarItemPaintStyle.CaptionInMenu;
            b1a.RibbonStyle = XR.RibbonItemStyles.Large;
            b1a.SuperTip = DxComponent.CreateDxSuperTip("Nový záznam", "Kliknutím se otevře nový prázdný formulář");
            popup.AddItem(b1a);

            var b1b = new XB.BarButtonItem(_BarManager, "B1b");
            b1b.Glyph = DxComponent.GetBitmapImage(resources[1], ResourceImageSizeType.Large);
            b1b.ButtonStyle = XB.BarButtonStyle.Default;
            b1b.PaintStyle = XB.BarItemPaintStyle.CaptionInMenu;
            b1b.RibbonStyle = XR.RibbonItemStyles.Large;
            b1b.SuperTip = DxComponent.CreateDxSuperTip("Otevřít záznam", "Kliknutím se otevře formulář s vybraným záznamem");
            popup.AddItem(b1b);

            var b1c = new XB.BarButtonItem(_BarManager, "B1c");
            b1c.Glyph = DxComponent.GetBitmapImage(resources[2], ResourceImageSizeType.Large);
            b1c.ButtonStyle = XB.BarButtonStyle.Default;
            b1c.PaintStyle = XB.BarItemPaintStyle.CaptionInMenu;
            b1c.RibbonStyle = XR.RibbonItemStyles.Large;
            b1c.SuperTip = DxComponent.CreateDxSuperTip("Otevřít dokument", "Kliknutím se načte dokument a zobrazí ve Wordu");
            popup.AddItem(b1c);

        }
        private XB.BarItem CreateContainerItem1()
        {
            var container = new XB.BarLinkContainerExItem();

            container.ItemLinks.Add(new XB.BarButtonItem(_BarManager, "B1a"));
            container.ItemLinks.Add(new XB.BarButtonItem(_BarManager, "B1b"));
            container.RibbonStyle = XR.RibbonItemStyles.SmallWithText;


            return container;
        }
        private XB.RibbonGalleryBarItem CreateGalleryBarItem()
        {
            var gallery = new XB.RibbonGalleryBarItem();
            gallery.Gallery.Images = DxComponent.GetVectorImageList(ResourceImageSizeType.Large);
            gallery.Gallery.ShowItemImage = true;
            gallery.ImageIndex = DxComponent.GetVectorImageIndex("svgimages/dashboards/editcolors.svg", ResourceImageSizeType.Large);

            var galGroup = new XR.GalleryItemGroup();
            galGroup.Caption = "Gallery Group";
            galGroup.Items.Add(new XR.GalleryItem(DxComponent.GetBitmapImage("images/richedit/differentoddevenpages_32x32.png", ResourceImageSizeType.Large), "IT1", "Descr"));
            gallery.Gallery.Groups.Add(galGroup);
            return gallery;
        }
        /// <summary>
        /// Tudy to chodí při každém rozbalení SubMenu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bbg_Popup(object sender, EventArgs e)
        {   // Někdo rozbaluje SubItems...
            XB.BarSubItem bbg = sender as XB.BarSubItem;
            if (bbg == null) return;

            var tag = bbg.Tag;
            if (tag is string && ((string)tag) == "RELOAD")
            {
                bbg.Tag = "reloading...";
                StartReload(bbg);
            }
        }

        private void StartReload(XB.BarSubItem bbg)
        {
            this._WorkThread = new System.Threading.Thread(RunThread);
            this._WorkThread.IsBackground = true;
            this._WorkThread.Name = "ReloadThread";
            this._WorkThread.Start(bbg);
        }
        private void RunThread(object sender)
        {
            System.Threading.Thread.Sleep(500);
            this.Invoke(new Action<object>(DoneReload), sender);
            this._WorkThread = null;
        }
        private void DoneReload(object sender)
        {
            XB.BarSubItem bbg = sender as XB.BarSubItem;
            bbg.Tag = "loaded.";
            try
            {
                bbg.BeginUpdate();
                bbg.ItemLinks[0].Item.Caption = bbg.ItemLinks[0].Item.Tag as string;
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "1. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "2. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "4. podpoložka") { ShortcutKeyDisplayString = "Alt+F4", ShowItemShortcut = DevExpress.Utils.DefaultBoolean.True });

                XB.BarButtonItem bbi3 = new XB.BarButtonItem(_BarManager, "5. podpoložka");
                bbg.AddItem(bbi3);
                bbi3.Links[0].BeginGroup = true;

                bbg.AddItem(new XB.BarButtonItem(_BarManager, "3. podpoložka"));
                bbg.AddItem(new XB.BarButtonItem(_BarManager, "6. podpoložka"));
            }
            finally
            {
                bbg.EndUpdate();
            }
        }
        System.Threading.Thread _WorkThread;


        private void Bbg_GetItemData(object sender, EventArgs e)
        {
        }

        private bool CheckItemChecked = true;
        #endregion
        #region RadialMenu
        private void PopupPageXBRadialClick(object sender, EventArgs e)
        {
            var barManager = _BarManager;
            var rm = new XR.RadialMenu(barManager);
            rm.AutoExpand = true;                   // Menu je po vytvoření otevřené
            rm.ButtonRadius = 25;                   // Prostřední button
            rm.InnerRadius = 25;
            rm.MenuRadius = 140;                    // Celkem menu
            rm.MenuColor = Color.DarkCyan;          // Barva aktivních segmentů
            rm.BackColor = Color.LightBlue;         // Barva pozadí
            rm.Glyph = DxComponent.CreateBitmapImage("Images/Actions24/dialog-close(24).png");      // Ikona uprostřed menu
            rm.PaintStyle = XR.PaintStyle.Skin;


            // Create bar items to display in Radial Menu 
            XB.BarItem btnCopy = new XB.BarButtonItem(barManager, "Copy");
            btnCopy.ImageOptions.ImageUri.Uri = "Copy;Size16x16";

            XB.BarItem btnCut = new XB.BarButtonItem(barManager, "Cut");
            btnCut.ImageOptions.ImageUri.Uri = "Cut;Size16x16";

            XB.BarItem btnDelete = new XB.BarButtonItem(barManager, "Delete");
            btnDelete.ImageOptions.ImageUri.Uri = "Delete;Size16x16";

            XB.BarItem btnPaste = new XB.BarButtonItem(barManager, "Paste");
            btnPaste.ImageOptions.ImageUri.Uri = "Paste;Size16x16";

            // Sub-menu with 3 check buttons 
            XB.BarSubItem btnMenuFormat = new XB.BarSubItem(barManager, "Format");
            XB.BarCheckItem btnCheckBold = new XB.BarCheckItem(barManager, false);
            btnCheckBold.Caption = "Bold";
            btnCheckBold.Checked = true;
            btnCheckBold.ImageOptions.ImageUri.Uri = "Bold;Size16x16";

            XB.BarCheckItem btnCheckItalic = new XB.BarCheckItem(barManager, true);
            btnCheckItalic.Caption = "Italic";
            btnCheckItalic.Checked = true;
            btnCheckItalic.ImageOptions.ImageUri.Uri = "Italic;Size16x16";

            XB.BarCheckItem btnCheckUnderline = new XB.BarCheckItem(barManager, false);
            btnCheckUnderline.Caption = "Underline";
            btnCheckUnderline.ImageOptions.ImageUri.Uri = "Underline;Size16x16";

            XB.BarItem[] subMenuItems = new XB.BarItem[] { btnCheckBold, btnCheckItalic, btnCheckUnderline };
            btnMenuFormat.AddItems(subMenuItems);

            XB.BarItem btnFind = new XB.BarButtonItem(barManager, "Find");
            btnFind.ImageOptions.ImageUri.Uri = "Find;Size16x16";

            XB.BarItem btnUndo = new XB.BarButtonItem(barManager, "Undo");
            btnUndo.ImageOptions.ImageUri.Uri = "Undo;Size16x16";

            XB.BarItem btnRedo = new XB.BarButtonItem(barManager, "Redo");
            btnRedo.ImageOptions.ImageUri.Uri = "Redo;Size16x16";

            var items = new XB.BarItem[] { btnCopy, btnCut, btnDelete, btnPaste, btnMenuFormat, btnFind, btnUndo, btnRedo };
            rm.AddItems(items);

            rm.ShowPopup(Control.MousePosition);
        }
        #endregion
        #region DXMenu
        private void PopupPageDXPopupMenuClick(object sender, EventArgs e)
        {
            _ShowDXPopupMenu(MousePosition);
        }
        private void _ShowDXPopupMenu(Point mousePosition)
        {
            DM.DXPopupMenu popup = new DM.DXPopupMenu(this);
            popup.MenuViewType = DM.MenuViewType.Menu;

            /*
            popup.MenuViewType = DM.MenuViewType.Menu;
            popup.OptionsMultiColumn.ColumnCount = 4;
            popup.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Left;
            popup.OptionsMultiColumn.ImageVerticalAlignment = DevExpress.Utils.Drawing.ItemVerticalAlignment.Top;
            */

            /*

            https://docs.devexpress.com/WindowsForms/DevExpress.Utils.Menu.DXPopupMenu.MultiColumn
            https://docs.devexpress.com/WindowsForms/DevExpress.XtraBars.BarHeaderItem.MultiColumn

            */

            var header1 = new DM.DXMenuHeaderItem() { Caption = "", Visible = true };
            header1.MultiColumn = DevExpress.Utils.DefaultBoolean.True;
            header1.OptionsMultiColumn.ColumnCount = 8;
            header1.OptionsMultiColumn.ImageHorizontalAlignment = DevExpress.Utils.Drawing.ItemHorizontalAlignment.Center;
            header1.OptionsMultiColumn.ImageVerticalAlignment = DevExpress.Utils.Drawing.ItemVerticalAlignment.Top;
            header1.OptionsMultiColumn.ItemDisplayMode = DM.MultiColumnItemDisplayMode.Image;

            popup.Items.Add(header1);

            for (int c = 0; c < 8; c++)
                popup.Items.Add(createDxMenuItem(true, false, ResourceImageSizeType.Large));


            popup.Items.Add(new DM.DXMenuHeaderItem() { Caption = "Další sada operací" });

            int count = Randomizer.Rand.Next(3, 12);
            for (int c = 0; c < count; c++)
                popup.Items.Add(createDxMenuItem(true, true, ResourceImageSizeType.Small));

            popup.Items.Add(new DM.DXMenuCheckItem("CheckItem 5") { Checked = true });

            var mc6 = new DM.DXSubMenuItem("Sub menu připravené...");
            int count6 = Randomizer.Rand.Next(3, 12);
            for (int c = 0; c < count6; c++)
                mc6.Items.Add(createDxMenuItem(true, true, ResourceImageSizeType.Small));
            popup.Items.Add(mc6);

            Point point = this.PointToClient(mousePosition);
            popup.ShowPopup(this, point);


            DM.DXMenuItem createDxMenuItem(bool withImage, bool withCaption, ResourceImageSizeType imageSize, Action<DM.DXMenuItem> modifier = null)
            {
                string caption = (withCaption ? Randomizer.GetSentence(1, 4) : "");

                Image image = null;
                if (withImage)
                {
                    string resource = Randomizer.GetItem(this._SysSvgImages);
                    if (imageSize == ResourceImageSizeType.Small)
                        resource = resource.Replace("32x32", "16x16");
                    image = DxComponent.GetBitmapImage(resource, imageSize);
                }

                var menuItem = new DM.DXMenuItem(caption, null, image);

                string tipTitle = Randomizer.GetSentence(3, 6);
                string tipText = Randomizer.GetSentences(3, 6, 1, 6);
                menuItem.SuperTip = DxComponent.CreateDxSuperTip(tipTitle, tipText);

                modifier?.Invoke(menuItem);

                return menuItem;
            }
        }
        #endregion
        #region WinForm menu
        private void PopupPageWinMenuClick(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripDropDownMenu ddm = new ToolStripDropDownMenu();
            ddm.RenderMode = ToolStripRenderMode.Professional;
            ddm.AllowTransparency = true;
            ddm.Opacity = 0.90d;
            ddm.AutoClose = true;
            ddm.DefaultDropDownDirection = ToolStripDropDownDirection.BelowRight;
            ddm.DropShadowEnabled = true;
            ddm.ShowCheckMargin = true;
            ddm.ShowImageMargin = true;
            ddm.ShowItemToolTips = true;
            // ddm.BackColor = Color.FromArgb(0, 0, 32);
            // ddm.ForeColor = Color.FromArgb(255, 255, 255);
            ddm.Margin = new Padding(6);

            // Title
            ToolStripLabel titleItem = new ToolStripLabel("TITULEK");
            titleItem.ToolTipText = "Popisek titulku";
            titleItem.Size = new Size(100, 28 + 4);
            // titleItem.Font = new Font(titleItem.Font, FontStyle.Bold);
            titleItem.TextAlign = ContentAlignment.MiddleCenter;
            ddm.Items.Add(titleItem);
            ddm.Items.Add(new ToolStripSeparator());

            // Položky
            ddm.Items.Add(new ToolStripMenuItem("První") { ToolTipText = "Tooltip k položce", Image = DxComponent.CreateBitmapImage("Images/Actions24/arrow-right-double-2(24).png") });
            ddm.Items.Add(new ToolStripMenuItem("Druhý") { CheckOnClick = true, CheckState = CheckState.Checked, Image = DxComponent.CreateBitmapImage("Images/Actions24/arrow-left-double-2(24)") });
            ddm.Items.Add(new ToolStripMenuItem("Třetí"));

            // SubPoložka
            ToolStripMenuItem ddb = new ToolStripMenuItem() { Text = "DropDown &w", ToolTipText = "Submenu...", DropDownDirection = ToolStripDropDownDirection.Right };
            ddb.DropDown.BackColor = ddm.BackColor;
            ddb.DropDown.ForeColor = ddm.ForeColor;
            ddb.ShortcutKeys = Keys.Control | Keys.W;
            // její položky
            ddb.DropDownItems.Add("1. podpoložka");
            ddb.DropDownItems.Add("2. podpoložka");
            ddb.DropDownItems.Add("3. podpoložka");
            ddb.DropDownItems.Add("4. podpoložka");
            ddb.DropDownItems.Add("5. podpoložka");
            ddm.Items.Add(ddb);

            ddm.ItemClicked += Ddm_ItemClicked;
            ddm.LayoutStyle = ToolStripLayoutStyle.Table;
            ddm.Renderer.RenderItemText += Renderer_RenderItemText;

            ddm.Show(this, this.PointToClient(Control.MousePosition));
        }

        private void Renderer_RenderItemText(object sender, ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.YellowGreen;
            e.Text = e.Text + " *";
        }

        private void Ddm_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var ic = e.ClickedItem;
        }
        #endregion
        #region AsolDx PopupMenu
        /// <summary>
        /// Systémové AsolDx PopupMenu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopupPageAsolDxPopupMenuClick(object sender, EventArgs e)
        {
            var point = Control.MousePosition;

            // Definice Popup menu:
            List<IMenuItem> menuItems = new List<IMenuItem>();
            int cnt;

            // Horní grupa
            addHeader("Rychlý přístup", true, 8);
            for (int m = 0; m < 8; m++)
                addBigItem(false);

            // Běžné grupy a položky:
            cnt = Randomizer.Rand.Next(3, 6);
            addHeader("Práce se záznamem");
            for (int m = 0; m < cnt; m++)
                addItem();
            addItemSubMenu();

            addHeader("Práce s dokumentem");
            cnt = Randomizer.Rand.Next(3, 6);
            for (int m = 0; m < cnt; m++)
                addItem();
            addItemSubMenu();

            addHeader("Submenu načítané OnDemand");
            addOnDemandItem("Nabídka funkcí", "Funkce se načítají...");
            addOnDemandItem("Úkoly Workflow", "Úkoly se načítají...");
            addOnDemandItem("Navázané dokumenty: 0", null);

            // Vytvoření a zobrazení fyzického menu:
            var popupMenu = DxComponent.CreateXBPopupMenu(menuItems, _DxPopupMenuClick, _DxPopupMenuOnDemandLoad, this);
            popupMenu.ShowPopup(point);


            // Přidá do 'menuItems' deklaraci Header
            void addHeader(string text, bool isMultiColumn = false, int? columnCount = null, MenuItemDisplayMode itemDisplayMode = MenuItemDisplayMode.Default)
            {
                DataMenuHeaderItem headerItem = new DataMenuHeaderItem();
                headerItem.Text = text;
                headerItem.IsMultiColumn = isMultiColumn;
                headerItem.UseLargeImages = isMultiColumn;
                headerItem.ColumnCount = columnCount;
                headerItem.ItemDisplayMode = itemDisplayMode;
                menuItems.Add(headerItem);
            }
            // Přidá do 'menuItems' deklaraci Big buttonu (volitelně bez textu)
            void addBigItem(bool withText)
            {
                DataMenuItem item = new DataMenuItem();
                item.Text = (withText ? Randomizer.GetWord(true) : "");
                item.ImageName = Randomizer.GetItem(this._SysSvgImages);
                item.ToolTipTitle = Randomizer.GetSentence(2, 5, false);
                item.ToolTipText = Randomizer.GetSentences(2, 7, 1, 4);
                menuItems.Add(item);
            }
            // Přidá do 'menuItems' obyčejný prvek
            void addItem()
            {
                menuItems.Add(getItem());
            }
            // Přidá do 'menuItems' prvek obsahující Static SubMenu
            void addItemSubMenu()
            {
                var item = getItem();
                item.SubItems = getSubMenu(item.Text);
                menuItems.Add(item);
            }
            // Vrátí prvky do SubMenu
            List<IMenuItem> getSubMenu(string headerText)
            {
                var subItems = new List<IMenuItem>();

                if (headerText != null)
                {   // Titulek SubMenu
                    DataMenuHeaderItem headerItem = new DataMenuHeaderItem();
                    headerItem.Text = headerText;
                    subItems.Add(headerItem);
                }

                // Prvky SubMenu:
                int cnt = Randomizer.Rand.Next(5, 8);
                for (int i = 0; i < cnt; i++)
                    subItems.Add(getItem());
                return subItems;
            }
            // Přidá do 'menuItems' prvek obsahující OnDemand SubMenu, volitelně obsahující jeden SubItem s daným textem (subItemHeaderText), null = bez tohoto prvku
            void addOnDemandItem(string text, string subItemHeaderText)
            {
                // OnDemand prvek v menu:
                DataMenuItem item = new DataMenuItem();
                item.Text = text;
                item.ToolTipTitle = item.Text;
                item.ToolTipText = Randomizer.GetSentences(2, 7, 1, 4);
                item.SubItemsIsOnDemand = true;
                menuItems.Add(item);

                // První zástupný prvek do OnDemand submenu:
                if (subItemHeaderText != null)
                {
                    DataMenuHeaderItem headerItem = new DataMenuHeaderItem();
                    headerItem.Text = subItemHeaderText;
                    item.SubItems = new List<IMenuItem>();
                    item.SubItems.Add(headerItem);
                }

                // Reálné menu - ve zdejší metodě mám všechny metody pro tvorbu menu (jsou to lokální metody),
                // kdežto v eventhandleru _DxPopupMenuOnDemandLoad => _DxPopupMenuOnDemandFill je mít nebudu!!!
                item.Tag = getSubMenu(item.Text);
            }
            // Vygeneruje a vrátí definici prvku menu
            DataMenuItem getItem()
            {
                DataMenuItem item = new DataMenuItem();
                item.Text = Randomizer.GetSentence(2, 5, false);
                item.ImageName = Randomizer.GetItem(this._SysSvgImages);
                item.ToolTipTitle = item.Text;
                item.ToolTipText = Randomizer.GetSentences(2, 7, 1, 4);
                if (Randomizer.IsTrue(33))
                    item.HotKeys = Randomizer.GetItem(this._SysHotKeys);

                if (Randomizer.IsTrue(20))
                    item.FontStyle = FontStyle.Italic;
                else if (Randomizer.IsTrue(20))
                    item.FontStyle = FontStyle.Bold;

                if (Randomizer.IsTrue(20))
                {
                    item.ItemType = MenuItemType.DownButton;
                    item.Checked = true;
                    item.Text += " [!]";
                }

                return item;
            }
        }
        void _DxPopupMenuOnDemandLoad(object sender, DxPopupMenu.OnDemandLoadArgs args)
        {
            // Zavoláme metodu _DxPopupMenuOnDemandFill v threadu na pozadí:
            Noris.Clients.Win.Components.AsolDX.ThreadManager.AddAction(() => _DxPopupMenuOnDemandFill(args));
        }
        private void _DxPopupMenuOnDemandFill(DxPopupMenu.OnDemandLoadArgs args)
        {
            int timeout = Randomizer.Rand.Next(100, 1000);
            System.Threading.Thread.Sleep(timeout);

            if (args.MenuItem is DataMenuItem menuItem)
            {
                menuItem.SubItemsIsOnDemand = false;
                menuItem.SubItems = menuItem.Tag as List<IMenuItem>;         // Do Tagu jsem si je připravil v době tvorby...
            }

            args.FillSubMenu();
        }
        void _DxPopupMenuClick(object sender, MenuItemClickArgs args)
        {
            DxComponent.ShowMessageInfo($"Uživatel vybral prvek {args.Item}", "DxPopupMenuClick");
        }

        #endregion
        #endregion
        #region TabHeaders
        private void InitTabHeaders()
        {
            AddNewPage("TabHeaderStrip", PrepareTabHeaders);
        }
        DxPanelControl _PanelTabHeader;
        private void PrepareTabHeaders(DxPanelControl panel)
        {
            _PanelTabHeader = panel;
            _SplitTabHeader = DxComponent.CreateDxSplitContainer(_PanelTabHeader, dock: DockStyle.Fill, splitLineOrientation: Orientation.Horizontal, fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.None, showSplitGlyph: true);
            _SplitTabHeader.FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1;
            //  _SplitTabHeader.Panel2.BackColor = Color.FromArgb(200, 225, 250);

            int x = 10;
            int w1 = 180;
            int w2 = 210;
            int w3 = 180;
            int y1 = 9;
            int y2 = 52;
            int y3 = 60;
            int y4 = 100;
            int h1 = 36;
            int h2 = 36;
            int h3 = 28;
            DxComponent.CreateDxSimpleButton(x, y1, w1, h1, _SplitTabHeader.Panel1, "Smaž vše", _TabHeadersClear_Click); x += (w1 + 10);
            DxComponent.CreateDxSimpleButton(x, y1, w1, h1, _SplitTabHeader.Panel1, "Přidej 4 stránky", _TabHeadersAdd4_Click); x += (w1 + 10);
            DxComponent.CreateDxSimpleButton(x, y1, w1, h1, _SplitTabHeader.Panel1, "Smaž a přidej 4", _TabHeadersClearAdd4_Click); x += (w1 + 10);
            DxComponent.CreateDxSimpleButton(x, y1, w1, h1, _SplitTabHeader.Panel1, "ReFill beze změny", _TabHeadersReFill_Click); x += (w1 + 10);
            DxComponent.CreateDxSimpleButton(x, y1, w1, h1, _SplitTabHeader.Panel1, "Test TryFindTabHeader()", _TabHeadersTryFindHeader_Click); x += (w1 + 10);
            int r1 = x;

            x = 10;
            _TabHeaderPosition1 = DxComponent.CreateDxSimpleButton(x, y3, w3, h3, _SplitTabHeader.Panel1, "Nahoře", _TabHeadersPositionTop_Click); x += (w3 + 10);
            _TabHeaderPosition2 = DxComponent.CreateDxSimpleButton(x, y3, w3, h3, _SplitTabHeader.Panel1, "Dole", _TabHeadersPositionBottom_Click); x += (w3 + 10);
            _TabHeaderPosition3 = DxComponent.CreateDxSimpleButton(x, y3, w3, h3, _SplitTabHeader.Panel1, "Vlevo s textem", _TabHeadersPositionLeftText_Click); x += (w3 + 10);
            _TabHeaderPosition4 = DxComponent.CreateDxSimpleButton(x, y3, w3, h3, _SplitTabHeader.Panel1, "Vlevo jen ikona", _TabHeadersPositionLeftIcon_Click); x += (w3 + 10);
            _TabHeaderPosition5 = DxComponent.CreateDxSimpleButton(x, y3, w3, h3, _SplitTabHeader.Panel1, "Vpravo svislý text", _TabHeadersPositionRightVerticalText_Click); x += (w3 + 10);
            int r2 = x;

            x = (r1 > r2 ? r1 : r2) + 50;
            _TabHeaderControlXtra = DxComponent.CreateDxSimpleButton(x, y1, w2, h2, _SplitTabHeader.Panel1, "DxXtraTab control", _TabHeadersDxXtraTabControl_Click);
            _TabHeaderControlPane = DxComponent.CreateDxSimpleButton(x, y2, w2, h2, _SplitTabHeader.Panel1, "DxTabPane control", _TabHeadersDxTabPaneControl_Click);

            _SplitTabHeader.SplitterPosition = y4;

            _TabHeaderTextEdit = DxComponent.CreateDxMemoEdit(20, 20, 400, 200, _SplitTabHeader.Panel2, readOnly: true, tabStop: false);
            _TabHeaderTextEdit.Font = new Font(FontFamily.GenericMonospace, 11f, FontStyle.Bold);
            _TabHeaderTextEdit.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            _TabHeaderTextReset();

            _XtraTabImages = new string[]
            {
    "svgimages/icon%20builder/weather_fog.svg",
    "svgimages/icon%20builder/weather_hail.svg",
    "svgimages/icon%20builder/weather_humidity.svg",
    "svgimages/icon%20builder/weather_lightning.svg",
    "svgimages/icon%20builder/weather_moon.svg",
    "svgimages/icon%20builder/weather_partlycloudyday.svg",
    "svgimages/icon%20builder/weather_partlycloudynight.svg",
    "svgimages/icon%20builder/weather_rain.svg",
    "svgimages/icon%20builder/weather_rainandhail.svg",
    "svgimages/icon%20builder/weather_rainheavy.svg",
    "svgimages/icon%20builder/weather_rainlight.svg",
    "svgimages/icon%20builder/weather_snow.svg",
    "svgimages/icon%20builder/weather_snowfall.svg",
    "svgimages/icon%20builder/weather_snowfallheavy.svg",
    "svgimages/icon%20builder/weather_snowfalllight.svg",
    "svgimages/icon%20builder/weather_storm.svg",
    "svgimages/icon%20builder/weather_sunny.svg",
    "svgimages/icon%20builder/weather_temperature.svg",
    "svgimages/icon%20builder/weather_umbrella.svg",
    "svgimages/icon%20builder/weather_water.svg",
    "svgimages/icon%20builder/weather_wind.svg",
    "svgimages/icon%20builder/weather_winddirection.svg"
            };
            _SplitTabHeader.Panel2.SizeChanged += _SplitTabHeaderPanel2_SizeChanged;
            _TabHeaderActivateDxXtraTabControl();
        }
        DxSimpleButton _TabHeaderPosition1;
        DxSimpleButton _TabHeaderPosition2;
        DxSimpleButton _TabHeaderPosition3;
        DxSimpleButton _TabHeaderPosition4;
        DxSimpleButton _TabHeaderPosition5;
        DxSimpleButton _TabHeaderControlXtra;
        DxSimpleButton _TabHeaderControlPane;
        ITabHeaderControl _TabHeaderControl;
        DxSplitContainerControl _SplitTabHeader;
        string[] _XtraTabImages;
        private void _TabHeaderActivateDxXtraTabControl()
        {
            _TabHeaderPositionButtonBold(1);
            _TabHeaderTextReset("Create component DxXtraTab");
            _TabHeaderActivate(FactoryControlType.DxXtraTabControl);
            _TabHeaderControlXtra.Appearance.FontStyleDelta = FontStyle.Bold;
            _TabHeaderControlPane.Appearance.FontStyleDelta = FontStyle.Regular;
        }
        private void _TabHeaderActivateDxTabPaneControl()
        {
            _TabHeaderPositionButtonBold(1);
            _TabHeaderTextReset("Create component DxTabPane");
            _TabHeaderActivate(FactoryControlType.DxTabPane);
            _TabHeaderControlPane.Appearance.FontStyleDelta = FontStyle.Bold;
            _TabHeaderControlXtra.Appearance.FontStyleDelta = FontStyle.Regular;
        }
        private void _TabHeaderActivate(FactoryControlType controlType)
        {
            _TabHeaderDeActivate();
            var control = ControlFactory.CreateControl(controlType);
            if (control is ITabHeaderControl tabHeader)
            {
                tabHeader.PageHeaderPosition = DxPageHeaderPosition.Default;
                tabHeader.HeaderSizeChanged += _TabHeaderPane_HeaderSizeChanged;
                tabHeader.SelectedIPageChanging += _TabHeaders_SelectedIPageChanging;
                tabHeader.SelectedIPageChanged += _TabHeaders_SelectedIPageChanged;
                tabHeader.IPageClosing += TabHeader_IPageClosing;
                tabHeader.IPageRemoved += TabHeader_IPageRemoved;

                // tabHeader.PageHeaderMultiLine = true;
                _TabHeaderControl = tabHeader;
                _SplitTabHeader.Panel2.Controls.Add(control);

                _XtraTabAddPages(5, true);
            }
        }
        private void _TabHeaderDeActivate()
        {
            if (_TabHeaderControl != null && _TabHeaderControl is Control control)
            {
                var tabHeader = _TabHeaderControl;
                tabHeader.HeaderSizeChanged -= _TabHeaderPane_HeaderSizeChanged;
                tabHeader.SelectedIPageChanged -= _TabHeaders_SelectedIPageChanged;
                tabHeader.IPageClosing -= TabHeader_IPageClosing;
                tabHeader.IPageRemoved -= TabHeader_IPageRemoved;

                _SplitTabHeader.Panel2.Controls.Remove(control);

                control.Dispose();
            }
        }
        private void _TabHeaderClear()
        {
            _TabHeaderTextReset("Run ClearPages()");
            if (_TabHeaderControl is null) return;

            _TabHeaderControl.ClearPages();
        }
        private void _XtraTabAddPages(int count, bool setPages = false)
        {
            _TabHeaderTextAddLine($"Run AddPages({count})");
            if (_TabHeaderControl is null) return;

            if (setPages)
                _TabHeaderControl.SetPages(_XtraTabGetPages(count, 0), null, true, true);
            else
                _TabHeaderControl.AddPages(_XtraTabGetPages(count));
        }
        private void _TabHeaderReFill()
        {
            _TabHeaderTextAddLine($"Run SetPages()");
            if (_TabHeaderControl is null) return;

            int count = _TabHeaderControl.IPageCount;
            _TabHeaderControl.SetPages(_XtraTabGetPages(count, 0), null, true, true);
        }
        private void _TabHeadersTryFindHeader()
        {
            var iTabHeader = _TabHeaderControl;
            if (iTabHeader is null) return;
            var mousePoint = Control.MousePosition;
            if (!(iTabHeader is Control tabControl)) return;
            var relativePoint = tabControl.PointToClient(mousePoint);
            if (!tabControl.ClientRectangle.Contains(relativePoint))
            {
                _TabHeaderTextAddLine($"Umístěte myš nad záhlaví záložek, neklikejte, a pak klávesnicí (mezerníkem) aktivujte tlačítko s focusem.");
                return;
            }
            bool found = iTabHeader.TryFindTabHeader(relativePoint, out var iPage);
            if (found)
                _TabHeaderTextAddLine($"Pro souřadnici {relativePoint} byla nalezena stránka: '{iPage}'.");
            else
                _TabHeaderTextAddLine($"Pro souřadnici {relativePoint} nebyla nalezena stránka IPageItem.");
        }
        private List<IPageItem> _XtraTabGetPages(int count, int? firstIndex = null)
        {
            List<IPageItem> pages = new List<IPageItem>();
            int index = (firstIndex.HasValue ? firstIndex.Value : _TabHeaderControl.IPageCount);
            for (int i = 0; i < count; i++)
            {
                bool closeButtonVisible = ((index % 3) == 1);
                pages.Add(new DataPageItem()
                {
                    ItemId = "Id" + index.ToString(),
                    Text = (index + 1).ToString() + ". " + Randomizer.GetWord(true) + (closeButtonVisible ? " ×" : ""),
                    ToolTipText = Randomizer.GetSentences(1, 8, 1, 6),
                    ImageName = Randomizer.GetItem(_XtraTabImages),
                    CloseButtonVisible = closeButtonVisible
                });
                index++;
            }
            return pages;
        }
        private void _TabHeadersClear_Click(object sender, EventArgs e) { _TabHeaderClear(); }
        private void _TabHeadersAdd4_Click(object sender, EventArgs e) { _XtraTabAddPages(4); }
        private void _TabHeadersClearAdd4_Click(object sender, EventArgs e) { _TabHeaderClear(); _XtraTabAddPages(4); }
        private void _TabHeadersReFill_Click(object sender, EventArgs e) { _TabHeaderReFill(); }
        private void _TabHeadersTryFindHeader_Click(object sender, EventArgs e) { _TabHeadersTryFindHeader(); }
        private void _TabHeadersDxXtraTabControl_Click(object sender, EventArgs e) { _TabHeaderActivateDxXtraTabControl(); }
        private void _TabHeadersDxTabPaneControl_Click(object sender, EventArgs e) { _TabHeaderActivateDxTabPaneControl(); }
        private void _TabHeadersPositionTop_Click(object sender, EventArgs e) { _TabHeaderSetPosition(DxPageHeaderPosition.Top); _TabHeaderPositionButtonBold(1); }
        private void _TabHeadersPositionBottom_Click(object sender, EventArgs e) { _TabHeaderSetPosition(DxPageHeaderPosition.Bottom); _TabHeaderPositionButtonBold(2); }
        private void _TabHeadersPositionLeftText_Click(object sender, EventArgs e) { _TabHeaderSetPosition(DxPageHeaderPosition.PositionLeft | DxPageHeaderPosition.IconText); _TabHeaderPositionButtonBold(3); }
        private void _TabHeadersPositionLeftIcon_Click(object sender, EventArgs e) { _TabHeaderSetPosition(DxPageHeaderPosition.PositionLeft | DxPageHeaderPosition.IconOnly); _TabHeaderPositionButtonBold(4); }
        private void _TabHeadersPositionRightVerticalText_Click(object sender, EventArgs e) { _TabHeaderSetPosition(DxPageHeaderPosition.PositionRight | DxPageHeaderPosition.TextOnly | DxPageHeaderPosition.VerticalText); _TabHeaderPositionButtonBold(5); }
        private void _SplitTabHeaderPanel2_SizeChanged(object sender, EventArgs e)
        {
            _TabHeaderTextAddLine($"Event SizeChanged()");
            _TabHeaderDoLayout();
        }
        private void _TabHeaderPane_HeaderSizeChanged(object sender, EventArgs e)
        {
            _TabHeaderTextAddLine($"Event HeaderSizeChanged()");
            _TabHeaderDoLayout();
        }
        private void _TabHeaders_SelectedIPageChanging(object sender, TEventCancelArgs<IPageItem> e)
        {
            string suffix = "";
            if (Randomizer.IsTrue(60))
            {
                if (Randomizer.IsTrue(30))
                {
                    suffix = ";   Cancelled!";
                }
                else
                {
                    suffix = ";   Postponed...";
                    Task.Run(() => _TabHeaders_SelectPageDelay(e.Item.ItemId));
                }
                e.Cancel = true;
            }
            _TabHeaderTextAddLine($"Event SelectedIPageChanging, current page: {_TabHeaderControl.SelectedIPage}, new page: {e.Item}" + suffix);
        }
        private void _TabHeaders_SelectPageDelay(string itemId)
        {
            System.Threading.Thread.Sleep(1000);

            var tabHeaders = _TabHeaderControl;
            if (tabHeaders != null)
                tabHeaders.SelectedIPageIdForce = itemId;
        }
        private void _TabHeaders_SelectedIPageChanged(object sender, TEventArgs<IPageItem> e)
        {
            _TabHeaderTextAddLine($"Event SelectedIPageChanged, current page: {_TabHeaderControl.SelectedIPage}, new page: {e.Item}");
        }
        private void TabHeader_IPageClosing(object sender, TEventCancelArgs<IPageItem> e)
        {
            string suffix = "";
            if (Randomizer.IsTrue(60))
            {
                suffix = ";   Cancelled";
                e.Cancel = true;
            }
            _TabHeaderTextAddLine($"Event IPageClosing({e.Item})" + suffix);
        }
        private void TabHeader_IPageRemoved(object sender, TEventArgs<IPageItem> e)
        {
            _TabHeaderTextAddLine($"Event IPageRemoved({e.Item})");
        }
        private void _TabHeaderSetPosition(DxPageHeaderPosition headerPosition)
        {
            _TabHeaderTextAddLine($"Run PageHeaderPosition({headerPosition})");
            if (_TabHeaderControl is null) return;

            _TabHeaderControl.PageHeaderPosition = headerPosition;
            _TabHeaderDoLayout();
        }
        private void _TabHeaderPositionButtonBold(int buttonBold)
        {
            _TabHeaderPosition1.Appearance.FontStyleDelta = (buttonBold == 1 ? FontStyle.Bold : FontStyle.Regular);
            _TabHeaderPosition2.Appearance.FontStyleDelta = (buttonBold == 2 ? FontStyle.Bold : FontStyle.Regular);
            _TabHeaderPosition3.Appearance.FontStyleDelta = (buttonBold == 3 ? FontStyle.Bold : FontStyle.Regular);
            _TabHeaderPosition4.Appearance.FontStyleDelta = (buttonBold == 4 ? FontStyle.Bold : FontStyle.Regular);
            _TabHeaderPosition5.Appearance.FontStyleDelta = (buttonBold == 5 ? FontStyle.Bold : FontStyle.Regular);
        }
        private void _TabHeaderDoLayout()
        {
            var innerBounds = _SplitTabHeader.Panel2.GetInnerBounds();
            if (innerBounds.Width < 100 || innerBounds.Height < 15) return;

            Rectangle textBounds = innerBounds;
            Rectangle headerBounds = Rectangle.Empty;

            if (_TabHeaderControl != null)
            {
                var headerPosition = _TabHeaderControl.PageHeaderPosition & DxPageHeaderPosition.PositionSummary;
                int headerHeight, headerWidth;
                switch (headerPosition)
                {
                    case DxPageHeaderPosition.PositionTop:
                        headerHeight = _TabHeaderControl.HeaderHeight;
                        headerBounds = new Rectangle(innerBounds.X, innerBounds.Y + 10, innerBounds.Width, headerHeight);
                        textBounds = new Rectangle(innerBounds.X, headerBounds.Bottom, innerBounds.Width, innerBounds.Bottom - headerBounds.Bottom);
                        break;
                    case DxPageHeaderPosition.PositionBottom:
                        headerHeight = _TabHeaderControl.HeaderHeight;
                        headerBounds = new Rectangle(innerBounds.X, innerBounds.Bottom - 10 - headerHeight, innerBounds.Width, headerHeight);
                        textBounds = new Rectangle(innerBounds.X, innerBounds.Y, innerBounds.Width, headerBounds.Y - innerBounds.Y);
                        break;
                    case DxPageHeaderPosition.PositionLeft:
                        headerWidth = _TabHeaderControl.HeaderWidth;
                        headerBounds = new Rectangle(innerBounds.X + 10, innerBounds.Y, headerWidth, innerBounds.Height);
                        textBounds = new Rectangle(headerBounds.Right, innerBounds.Y, innerBounds.Right - headerBounds.Right, innerBounds.Height);
                        break;
                    case DxPageHeaderPosition.PositionRight:
                        headerWidth = _TabHeaderControl.HeaderWidth;
                        headerBounds = new Rectangle(innerBounds.Right - 10 - headerWidth, innerBounds.Y, headerWidth, innerBounds.Height);
                        textBounds = new Rectangle(innerBounds.X, innerBounds.Y, headerBounds.X - innerBounds.X, innerBounds.Height);
                        break;
                    default:
                        headerBounds = new Rectangle(innerBounds.X, innerBounds.Y + 10, innerBounds.Width, 50);

                        break;
                }
                _TabHeaderControl.Bounds = headerBounds;
            }
            _TabHeaderTextEdit.Bounds = textBounds;
        }
        private void _TabHeaderTextReset(string line = null)
        {
            _TabHeaderTextNumber = 0;
            _TabHeaderTextContent = "";
            _TabHeaderTextEdit.Text = _TabHeaderTextContent;
            if (line != null)
                _TabHeaderTextAddLine(line);
        }
        private void _TabHeaderTextAddLine(string line)
        {
            _TabHeaderTextNumber += 1;
            _TabHeaderTextContent += _TabHeaderTextNumber.ToString().PadLeft(4, ' ') + ". " + line + Environment.NewLine;
            _TabHeaderTextEdit.Text = _TabHeaderTextContent;
        }
        DxMemoEdit _TabHeaderTextEdit;
        private int _TabHeaderTextNumber;
        private string _TabHeaderTextContent;

        #region staré a opuštěné
        /*


        DxTabPane _TabHeaderTabPane;
        AsolSamplePanel _TabHeaderSamplePanel;

        private void _SplitTabHeaderPanel2_SizeChanged(object sender, EventArgs e)
        {
            _SplitTabHeaderDoLayout();
        }

        private void _TabHeaderPane_HeaderHeightChanged(object sender, EventArgs e)
        {
            _SplitTabHeaderDoLayout();
        }
        private void _SplitTabHeaderDoLayout()
        {
            if (_TabHeaderTabPane is null) return;

            var clientBounds = _SplitTabHeader.Panel2.GetInnerBounds();
            int tabHeaderHeight = _TabHeaderTabPane.HeaderHeight;
            _TabHeaderTabPane.Bounds = new Rectangle(clientBounds.X, clientBounds.Y, clientBounds.Width, tabHeaderHeight);

            int samplePanelY = _TabHeaderTabPane.Bounds.Bottom;
            _TabHeaderSamplePanel.Bounds = new Rectangle(clientBounds.X, samplePanelY, clientBounds.Width, clientBounds.Bottom - samplePanelY);
        }

        private void _TabHeaderClear()
        {
            if (_TabHeaderTabPane is null) return;
            _TabHeaderTabPane.ClearPages();
        }
        private void _TabHeaderAdd(int count)
        {
            if (_TabHeaderTabPane is null) return;
            string id = (Guid.NewGuid()).ToString();
            _TabHeaderTabPane.AddNewPage(id, id);
        }

        private void InitTabHeaders1()
        {
            _TabHeaderStrip1 = TabHeaderStrip.Create(TabHeaderStrip.HeaderType.DevExpressTop);
            _TabHeaderControl1 = _TabHeaderStrip1.Control;
            _TabHeaderControl1.Dock = DockStyle.Fill;

            using (_TabHeaderStrip1.SilentScope())
            {
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
            }

            _SplitTabHeader.Panel1.Controls.Add(_TabHeaderControl1);

            _TabHeaderStrip1.SelectedTabChanging += _TabHeaderStrip1_SelectedTabChanging;
            _TabHeaderStrip1.SelectedTabChanged += _TabHeaderStrip1_SelectedTabChanged;
            _TabHeaderStrip1.HeaderSizeChanged += _TabHeaderStrip1_HeaderSizeChanged;
        }
        /// <summary>
        /// Přidá novou záložku
        /// </summary>
        private void _TabStrip1AddItem()
        {
            int i = ++_TabStrip1BtnItem;
            TabHeaderItem tabHeaderItem = TabHeaderItem.CreateItem("Key" + i, "Záhlaví " + i, "Titulek stránky " + i, "Nápověda k záložce číslo " + i, null);
            _TabStrip1AddItem(tabHeaderItem);
        }
        private void _TabStrip1AddItem(TabHeaderItem tabHeaderItem)
        {
            bool nativeAdd = _NativeAddCheck.Checked;
            if (nativeAdd)
            {
                TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
                DXN.TabPane tabs = control as DXN.TabPane;
                // control.PageProperties.ShowMode = DXN.ItemShowMode.ImageAndText;

                // DXN.NavigationPageBounds
                // DXN.NavigationPageBase npb = new DXN.NavigationPageBase();

                // OK : DXN.NavigationPage page = new DXN.NavigationPage();
                var page = control.CreateNewPage() as DXN.TabNavigationPage;
                page.Name = tabHeaderItem.Key;
                page.Caption = tabHeaderItem.Label;
                page.PageText = tabHeaderItem.Label;
                page.ToolTip = tabHeaderItem.ToolTip;
                page.ImageOptions.Image = tabHeaderItem.Image;
                page.Properties.ShowMode = DXN.ItemShowMode.ImageAndText; // tabHeaderItem.ImageTextMode;

                tabs.Pages.Add(page);

                control.SelectedPage = page;
            }
            else
            {
                _TabHeaderStrip1.AddItem(tabHeaderItem);
            }
            _TestStacks();
        }
        private void _TabStrip1Clear()
        {
            bool nativeAdd = _NativeAddCheck.Checked;
            if (nativeAdd)
            {
                TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
                DXN.TabPane tabs = control as DXN.TabPane;
                tabs.Pages.Clear();
            }
            else
            {
                _TabHeaderStrip1.Clear();
            }
            _TestStacks();
        }
        private void _TestStacks()
        {
            StringBuilder sb = new StringBuilder();
            var sfis = StackFrameInfo.CreateStackTrace(1);
            StackFrameInfo.AddTo(sfis, sb, true);
            string result = sb.ToString();
        }
        /// <summary>
        /// Informace o jedné položce stacku
        /// </summary>
        private class StackFrameInfo
        {
            #region Konstruktor a data
            /// <summary>
            /// Vytvoří a vrátí pole položek stacktrace.
            /// </summary>
            /// <returns></returns>
            public static StackFrameInfo[] CreateStackTrace(int ignoreLevels, bool reverseOrder = false)
            {
                List<StackFrameInfo> result = new List<StackFrameInfo>();
                System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace(true).GetFrames();
                int count = frames.Length;
                int begin = ignoreLevels + 1;    // Na pozici [0] je this metoda, tu budu skrývat vždy; a přidám počet ignorovaných pozic z volající metody...
                if (begin < 0) begin = 0;
                for (int f = begin; f < count; f++)  
                    result.Add(new StackFrameInfo(frames[f]));
                if (reverseOrder) result.Reverse();
                return result.ToArray();
            }
            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="stackFrame"></param>
            public StackFrameInfo(System.Diagnostics.StackFrame stackFrame)
            {
                var method = stackFrame.GetMethod();
                var pars = method.GetParameters();

                FileName = stackFrame.GetFileName();
                LineNumber = stackFrame.GetFileLineNumber();
                DeclaringType = method.DeclaringType.FullName;
                MethodModifiers = (method.IsPublic ? "public " : "") +
                                  (method.IsPrivate ? "private " : "") +
                                  (!method.IsPrivate && !method.IsPublic ? "protected " : "") +
                                  (method.IsVirtual ? "virtual " : "") +
                                  (method.IsAbstract ? "abstract " : "") +
                                  (method.IsStatic ? "static " : "");
                MethodName = method.Name;
                string parameters = "(";
                string pard = "";
                foreach (var par in pars)
                {
                    var type = par.ParameterType;
                    parameters += (pard + (par.IsOut ? "out " : "") + type.FullName + " " + par.Name);
                    pard = ", ";
                }
                parameters += ")";
                Parameters = parameters;
                IsExternal = (DeclaringType.StartsWith("System.") || DeclaringType.StartsWith("Infragistics.") || DeclaringType.StartsWith("DevExpress."));
            }
            public override string ToString()
            {
                return $"{DeclaringType} : {MethodName}{Parameters}";
            }
            public readonly string FileName;
            public readonly int LineNumber;
            public readonly string DeclaringType;
            public readonly string MethodModifiers;
            public readonly string MethodName;
            public readonly string Parameters;
            public readonly bool IsExternal;
            #endregion
            /// <summary>
            /// Vloží data všech objektů do daného textového výstupu
            /// </summary>
            /// <param name="frames"></param>
            /// <param name="sb"></param>
            /// <param name="collapseExternals"></param>
            public static void AddTo(IEnumerable<StackFrameInfo> frames, StringBuilder sb, bool collapseExternals = false)
            {
                if (frames == null) return;
                bool inExternals = false;
                foreach (var frame in frames)
                {
                    if (collapseExternals && frame.IsExternal)
                    {
                        if (!inExternals)
                        {
                            frame.AddTo(sb, true);
                            inExternals = true;

                        }
                    }
                    else
                    {
                        inExternals = false;
                        frame.AddTo(sb);
                    }
                }
            }
            /// <summary>
            /// Vloží data this objektu do daného textového výstupu
            /// </summary>
            /// <param name="sb"></param>
            /// <param name="asExternal"></param>
            public void AddTo(StringBuilder sb, bool asExternal = false)
            {
                string tab = "\t";
                if (asExternal)
                {
                    sb.Append("[External code]" + tab);
                    sb.Append(tab);
                }
                else
                {
                    sb.Append(FileName + tab);
                    sb.Append(LineNumber + tab);
                }
                sb.Append(DeclaringType + tab);
                sb.Append(MethodModifiers + tab);
                sb.Append(MethodName + tab);
                sb.Append(Parameters);
                sb.AppendLine();
            }
        }
        private void _TabStrip1Refresh()
        {
            TabHeaderStripDXTop control = _TabHeaderStrip1.Control as TabHeaderStripDXTop;
        }
        private int _TabStrip1BtnItem = 0;
        /// <summary>
        /// Přidej 2 záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn1_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1AddItem();
                    _TabStrip1AddItem();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1Refresh();
            }
        }
        /// <summary>
        /// Smaž záložky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn2_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1Clear();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1Clear();
                _TabStrip1Refresh();
            }
        }
        /// <summary>
        /// Smaž a přidej
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TabTopAddBtn3_Click(object sender, EventArgs e)
        {
            bool silent = _TabTopAddSilentCheck.Checked;
            if (silent)
            {
                using (_TabHeaderStrip1.SilentScope())
                {
                    _TabStrip1Clear();
                    _TabStrip1AddItem();
                    _TabStrip1AddItem();
                    _TabStrip1Refresh();
                }
            }
            else
            {
                _TabStrip1Clear();
                _TabStrip1AddItem();
                _TabStrip1AddItem();
                _TabStrip1Refresh();
            }
        }

        private void _TabHeaderStrip1_SelectedTabChanging(object sender, ValueChangingArgs<string> e)
        {
            // S pomocí tohoto handleru realizuji požadavek: "Na novou záložku přepni až po druhém kliknutí na ni:"
            if (!String.Equals(e.ValueNew, _TabHeaderStrip1_AttemptKey))
            {   // Pokud jsem klikl na záložku, jejíž klíč není v _TabHeaderStrip1_AttemptKey, tak si klíč uložím (pro příště) a nyní to zakážu:
                _TabHeaderStrip1_AttemptKey = e.ValueNew;
                e.Cancel = true;
            }
        }
        private string _TabHeaderStrip1_AttemptKey = null;

        private void _TabHeaderStrip1_SelectedTabChanged(object sender, ValueChangedArgs<string> e)
        {
            // this._PanelHeaders1.Height = _TabHeaderStrip1.OptimalSize;
            this.Text = e.ValueNew;
        }
        private void _TabHeaderStrip1_HeaderSizeChanged(object sender, ValueChangedArgs<Size> e)
        {
            // this._PanelHeaders1.Height = _TabHeaderStrip1.OptimalSize;
        }

        private TabHeaderStrip _TabHeaderStrip1;
        private Control _TabHeaderControl1;

        private void InitTabHeaders2()
        {
            _TabHeaderStrip2 = TabHeaderStrip.Create(TabHeaderStrip.HeaderType.DevExpressLeft);
            _TabHeaderControl2 = _TabHeaderStrip2.Control;
            _TabHeaderControl2.Dock = DockStyle.Fill;

            using (_TabHeaderStrip2.SilentScope())
            {
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key1", "Záhlaví první 1", "Titulek stránky 1, poměrně velký prostor na šířku", "Nápověda 1", null, DxComponent.CreateBitmapImage("Images/Actions24/arrow-right(24).png")));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key2", "Záhlaví druhé 2", "Titulek stránky 2, obsahuje doplňkové informace", "Nápověda 2", null, DxComponent.CreateBitmapImage("Images/Actions24/arrow-right-2(24).png")));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key3", "Záhlaví třetí 3", "Titulek stránky 3, například: Uživatelem definované atributy", "Nápověda 3", null, DxComponent.CreateBitmapImage("Images/Actions24/arrow-right-3(24).png")));
                _TabHeaderStrip2.AddItem(TabHeaderItem.CreateItem("key4", "Záhlaví čtvrté 4", "Titulek stránky 4", "Nápověda 4", null, DxComponent.CreateBitmapImage("Images/Actions24/arrow-right-3(24).png")));
            }

            _SplitTabHeader.Panel2.Controls.Add(_TabHeaderControl2);

            _TabHeaderStrip2.SelectedTabChanged += _TabHeaderStrip2_SelectedTabChanged;
            _TabHeaderStrip2.HeaderSizeChanged += _TabHeaderStrip2_HeaderSizeChanged;
        }
        private void _TabHeaderStrip2_SelectedTabChanged(object sender, ValueChangedArgs<string> e)
        {
            // this._PanelHeaders2.Width = _TabHeaderStrip2.OptimalSize;
            this.Text = e.ValueNew;
        }
        private void _TabHeaderStrip2_HeaderSizeChanged(object sender, ValueChangedArgs<Size> e)
        {
            // this._PanelHeaders2.Width = _TabHeaderStrip2.OptimalSize;
        }
        private TabHeaderStrip _TabHeaderStrip2;
        private Control _TabHeaderControl2;

        private void InitTabHeaders8()
        {
            // Standardní horní navigační lišta, pouze buttony
            var tabPane = new TestTabPane()                // XB.Navigation.TabPane()
            {
                Name = "InnerTabControl",
                Dock = DockStyle.Fill,
                TabAlignment = DevExpress.XtraEditors.Alignment.Near,
                AllowCollapse = DevExpress.Utils.DefaultBoolean.True,       // Dovolí uživateli skrýt headery
                OverlayResizeZoneThickness = 25,
                ItemOrientation = Orientation.Horizontal
            };
            tabPane.PageProperties.ShowMode = XB.Navigation.ItemShowMode.ImageAndText;
            tabPane.PageProperties.AppearanceCaption.FontSizeDelta = 2;
            tabPane.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Style3D;
            tabPane.LookAndFeel.UseWindowsXPTheme = true;

            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            tabPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;  // ???
            tabPane.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            tabPane.TransitionAnimationProperties.FrameCount = 20;                  // Celkový čas = interval * count
            tabPane.TransitionType = DevExpress.Utils.Animation.Transitions.Push;

            AddTabPages(tabPane);

            #region fungující algoritmy
            / *
             * 
            tabPane.AddPage("TabPane.TabNavigationPage 1", "page1");
            tabPane.AddPage("Titulek 2", "page2");
            tabPane.AddPage("Titulek 3", "page3");
            XB.Navigation.TabNavigationPage px = new XB.Navigation.TabNavigationPage()
            {
                Name = "p3",
                ControlName = "p3",
                Caption = "Titulek 5",
                PageText = "Titulek dalších dat",
                Image = Properties.Resources.address_book_new_4
            };
            tabPane.AddPage(px);

            ListView listView = new ListView();
            listView.Items.Add("Položka 1");
            listView.Items.Add("Položka 2");
            listView.Items.Add("Položka 3");
            listView.Items.Add("Položka 4");
            listView.Items.Add("Položka 5");
            listView.View = View.LargeIcon;
            var pl = tabPane.AddPage(listView);
            listView.Dock = DockStyle.Fill;
            pl.PageText = "List";
            pl.Caption = "Caption";

            var p0 = tabPane.Pages[0] as XB.Navigation.TabNavigationPage;
            p0.ImageOptions.Image = Properties.Resources.distribute_vertical_equal;
            p0.ToolTip = "Záhlaví dokladu / záznamu";
            p0.Caption = "Záznam";
            p0.PageText = "Data záznamu";

            var p1 = tabPane.Pages[1] as XB.Navigation.TabNavigationPage;
            p1.ImageOptions.Image = Properties.Resources.address_book_new_4;

            var type = tabPane.Pages[0].GetType();


            * /
            #endregion

            _SplitTabHeader.Panel2.Controls.Add(tabPane);
            // this._PanelHeaders2.BackColor = Color.FromArgb(180, 196, 180);

            tabPane.SelectedPageChanged += TabPane_SelectedPageChanged;
        }
        private void AddTabPageXX0(TestTabPane tabPane, string key, string caption, string pageText = null, Image image = null, Action<XB.Navigation.TabNavigationPage> fillAction = null)
        {
            XB.Navigation.TabNavigationPage page = new XB.Navigation.TabNavigationPage()
            {
                Name = key,
                ControlName = key,
                Caption = caption,
                PageText = pageText ?? caption,
                Image = image
            };
            fillAction?.Invoke(page);
            tabPane.AddPage(page);

            page.PageText = pageText ?? caption;
        }
        private void AddTabPages(XB.Navigation.TabPane tabPane)
        {
            AddTabPage(tabPane, "page1", "Titulek 1", image: DxComponent.CreateBitmapImage("Images/Actions24/arrow-right(24).png"));
            AddTabPage(tabPane, "page2", "Titulek 2", image: DxComponent.CreateBitmapImage("Images/Actions24/arrow-right(24).png"));
            AddTabPage(tabPane, "page3", "Titulek 3", image: DxComponent.CreateBitmapImage("Images/Actions24/arrow-right(24).png"));
            AddTabPage(tabPane, "page4", "Titulek 4", image: DxComponent.CreateBitmapImage("Images/Actions24/arrow-right(24).png"));
        }
        private void AddTabPage(XB.Navigation.TabPane tabPane, string key, string caption, string pageText = null, Image image = null, Action<XB.Navigation.TabNavigationPage> fillAction = null)
        {
            tabPane.AddPage(caption, key);
            XB.Navigation.TabNavigationPage page = tabPane.Pages[tabPane.Pages.Count - 1] as XB.Navigation.TabNavigationPage;
            page.Name = key;
            page.ControlName = key;
            page.Caption = caption;
            page.PageText = pageText ?? caption;
            page.Image = image;

            fillAction?.Invoke(page);
        }
        private void TabPane_SelectedPageChanged(object sender, XB.Navigation.SelectedPageChangedEventArgs e)
        {
            Type newType = e.Page.GetType();
            XB.Navigation.TabNavigationPage np = e.Page as XB.Navigation.TabNavigationPage;
            var name = np.Name;
            this.Text = np.PageText;
        }
        internal class TestTabPane : XB.Navigation.TabPane
        { }
        private void InitTabHeaders9()
        {
            DevExpress.XtraEditors.WindowsFormsSettings.AnimationMode = DevExpress.XtraEditors.AnimationMode.EnableAll;
            DevExpress.XtraEditors.WindowsFormsSettings.AllowHoverAnimation = DevExpress.Utils.DefaultBoolean.True;

            // DevExpress.Utils.Animation.TransitionManager tm; tm.

            // Boční lišta záhlaví + objekt
            XB.Navigation.NavigationPane navPane = new XB.Navigation.NavigationPane()
            {
                Name = "InnerTabControl",
                Dock = DockStyle.Fill
                // TabAlignment = DevExpress.XtraEditors.Alignment.Near
            };
            navPane.PageProperties.ShowMode = XB.Navigation.ItemShowMode.ImageAndText;
            navPane.PageProperties.AppearanceCaption.FontSizeDelta = 2;
            navPane.ItemOrientation = Orientation.Horizontal;             // Otáčí obsah buttonu

            navPane.AllowTransitionAnimation = DevExpress.Utils.DefaultBoolean.True;
            navPane.TransitionAnimationProperties.FrameInterval = 5 * 10000;        // 10000 je jedna jednotka, která je rovna 1 milisekundě
            navPane.TransitionAnimationProperties.FrameCount = 20;                  // Celkový čas = interval * count
            navPane.TransitionType = DevExpress.Utils.Animation.Transitions.Push;

            navPane.PageProperties.ShowCollapseButton = false;
            navPane.PageProperties.ShowExpandButton = false;
            navPane.PageProperties.AllowBorderColorBlending = true;
            navPane.ShowToolTips = DevExpress.Utils.DefaultBoolean.True;
            navPane.State = XB.Navigation.NavigationPaneState.Expanded;

            navPane.AddPage("Titulek 1", "page1");

            navPane.AddPage(new System.Windows.Forms.TextBox() { Text = "Obsah textboxu", Name = "TextBox" });

            ListView listView = new ListView();
            listView.Items.Add("Položka 1");
            listView.Items.Add("Položka 2");
            listView.Items.Add("Položka 3");
            listView.Items.Add("Položka 4");
            listView.Items.Add("Položka 5");
            listView.View = View.LargeIcon;
            navPane.AddPage(listView);
            listView.Dock = DockStyle.Fill;

            var type = navPane.Pages[0].GetType();

            var p0 = navPane.Pages[0] as XB.Navigation.NavigationPage;
            p0.PageText = "Hlavička";
            p0.Caption = "Hlavička záznamu";
            p0.CustomHeaderButtons.Add(new XB.Docking2010.WindowsUIButton("Tlačítko", XB.Docking2010.ButtonStyle.PushButton) { UseImage = true } );
            p0.PageVisible = true;
            p0.ToolTip = "Hlavička záznamu";
            p0.ImageOptions.Image = DxComponent.CreateBitmapImage("Images/Actions24/align-horizontal-left(24).png");

            var p1 = navPane.Pages[1] as XB.Navigation.NavigationPage;
            p1.ImageOptions.Image = DxComponent.CreateBitmapImage("Images/Actions24/align-horizontal-right-2(24).png");
            p1.PageText = "UDA";
            p1.Caption = "Uživatelem definované atributy";

            var p2 = navPane.Pages[2] as XB.Navigation.NavigationPage;
            p2.ImageOptions.Image = DxComponent.CreateBitmapImage("Images/Actions24/align-vertical-bottom-2(24)");
            p2.Caption = "Titulkový text = Seznam položek";
            p2.PageText = "Položky";

            _SplitTabHeader.Panel2.Controls.Add(navPane);
        }
        */
        #endregion

        #endregion
        #region Splittery
        private void InitSplitters()
        {
            AddNewPage("Splittery", PrepareSplitters);
        }
        private DxPanelControl _PanelSplitters;
        private void PrepareSplitters(DxPanelControl panel)
        {
            _PanelSplitters = panel;
            InitAsolSplitters();
        }
        private void InitAsolSplitters()
        {
            _AsolPanel = new AsolPanel();
            _AsolPanel.Dock = DockStyle.None;
            _AsolPanel.AutoScroll = true;
            _PanelSplitters.Controls.Add(_AsolPanel);
            _PanelSplitters.SizeChanged += _PanelSplitter_SizeChanged;

            int x = 50;
            int y = 50;
            int w = 350;
            int h = 250;
            var gp1 = new AsolSamplePanel() { Name = "Rhombus", Bounds = new Rectangle(x, y, w, h), Shape = AsolSamplePanel.ShapeType.Rhombus, CenterColor = Color.DarkGreen };
            _AsolPanel.Controls.Add(gp1);
            var gp2 = new AsolSamplePanel() { Name = "Star4", Bounds = new Rectangle(x, y + h, w, h), Shape = AsolSamplePanel.ShapeType.Star4, CenterColor = Color.BlueViolet };
            _AsolPanel.Controls.Add(gp2);
            var gp3 = new AsolSamplePanel() { Name = "Star8AcuteAngles", Bounds = new Rectangle(x + w, y, w, h), Shape = AsolSamplePanel.ShapeType.Star8AcuteAngles, CenterColor = Color.DarkOrchid };
            _AsolPanel.Controls.Add(gp3);
            var gp4 = new AsolSamplePanel() { Name = "Star8ObtuseAngles", Bounds = new Rectangle(x + w, y + h, w, h), Shape = AsolSamplePanel.ShapeType.Star8ObtuseAngles, CenterColor = Color.Cyan };
            _AsolPanel.Controls.Add(gp4);

            var sp1 = new NWC.SplitterManager() { Name = "SplitterVertical1", SplitPosition = 245, Orientation = Orientation.Vertical, OnTopMode = NWC.SplitterBar.SplitterOnTopMode.OnMouseEnter };
            sp1.ControlsBefore.Add(gp1);
            sp1.ControlsBefore.Add(gp2);
            sp1.ControlsAfter.Add(gp3);
            sp1.ControlsAfter.Add(gp4);
            sp1.SplitterColorByParent = false;
            sp1.DevExpressSkinEnabled = true;
            sp1.ActivityMode = NWC.SplitterBar.SplitterActivityMode.ResizeAfterMove;
            sp1.ApplySplitterToControls();
            sp1.SplitPositionChanging += _SplitterValueChanging;
            _AsolPanel.Controls.Add(sp1);

            sp1.AcceptBoundsToSplitter = true;
            sp1.Bounds = new Rectangle(100, 15, 14, 650);

            var sp2 = new NWC.SplitterManager() { Name = "SplitterHorizontal1", SplitPosition = 105, Orientation = Orientation.Horizontal, OnTopMode = NWC.SplitterBar.SplitterOnTopMode.None };
            sp2.ControlsBefore.Add(gp1);
            sp2.ControlsBefore.Add(gp3);
            sp2.ControlsAfter.Add(gp2);
            sp2.ControlsAfter.Add(gp4);
            sp2.ApplySplitterToControls();
            sp2.SplitPositionChanging += _SplitterValueChanging;
            _AsolPanel.Controls.Add(sp2);

            _Splitter3Panel = new Panel()
            {
                Name = "SpliterPanel",
                BackColor = Color.DarkGoldenrod
            };
            _AsolPanel.Controls.Add(_Splitter3Panel);

            _Splitter3 = new NWC.SplitterBar()
            {
                Name = _SplitterTransferName,
                SplitPosition = 240,
                Orientation = Orientation.Horizontal,
                DevExpressSkinEnabled = false,
                SplitterColorByParent = false,
                SplitterColor = Color.LightCoral,
                AnchorType = NWC.SplitterAnchorType.Relative,
                OnTopMode = NWC.SplitterBar.SplitterOnTopMode.OnMouseEnter,
                TransferToParentSelector = _SpliterPanelSearch,
                TransferToParentEnabled = true
            };
            _Splitter3.SplitPositionChanging += _Splitter3_SplitPositionChanging;
            _Splitter3.SplitPositionChanging += _SplitterValueChanging;
            _Splitter3.SplitInactiveRange = new Range<int>(16, -16);
            _Splitter3Panel.Controls.Add(_Splitter3);
            SetWorkingBounds(_AsolPanel);

            _SplitLabel = new Label() { AutoSize = false, Dock = DockStyle.Left, Width = _SplitterLabelWidth - 5, Text = "Splittery", Font = SystemFonts.StatusFont };
            _PanelSplitters.Controls.Add(_SplitLabel);

            _AsolPanel.Resize += X_Resize;
            _SetAsolPanelBounds();
        }

        private void _PanelSplitter_SizeChanged(object sender, EventArgs e)
        {
            _SetAsolPanelBounds();
        }
        private void _SetAsolPanelBounds()
        {
            int dx = _SplitterLabelWidth;
            Size totalSize = _PanelSplitters.ClientSize;
            _AsolPanel.Bounds = new Rectangle(dx, 0, totalSize.Width - dx, totalSize.Height);
        }
        private const int _SplitterLabelWidth = 140;
        private const string _SplitterTransferName = "SplitterTransfer";
        private void X_Resize(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                SetWorkingBounds(control);
            }
        }
        private void _SplitterValueChanging(object sender, TEventValueChangeArgs<double> e)
        {
            if (sender is NWC.SplitterBar splitter)
            {
                string eol = Environment.NewLine;
                var bounds = splitter.Bounds;
                if (splitter.Name == _SplitterTransferName) bounds = splitter.Parent.Bounds;
                var offset = _AsolPanel.AutoScrollPosition;
                string text = splitter.Name + eol +
                              "Position: " + splitter.SplitPosition + eol +
                              "OldValue: " + e.OldValue + eol +
                              "NewValue: " + e.NewValue + eol +
                              "X: " + bounds.X + eol +
                              "Y: " + bounds.Y + eol +
                              "Scroll.X: " + offset.X + eol +
                              "Scroll.Y: " + offset.Y + eol;
                _SplitLabel.Text = text;
            }
        }
        private void _Splitter3_SplitPositionChanging(object sender, TEventValueChangeArgs<double> e)
        {
            int newValue = (int)e.NewValue;
            int step = 1;
            int changedValue = (newValue < 50 ? 50 : (newValue > 420 ? 420 : newValue));
            changedValue = step * (changedValue / step);
            if (changedValue != newValue)
                e.NewValue = changedValue;
            var spr = _Splitter3.SplitPositionRange;
        }

        /// <summary>
        /// Tato metoda je volaná v situaci, kdy splitter má určit svého Parenta, kterým bude pohybovat.
        /// Tato metoda dostává tedy různé parenty našeho splitteru a určuje, který je ten správný.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        private NWC.SplitterBar.SplitterParentSelectorMode _SpliterPanelSearch(Control control)
        {
            return ((control.Name == "SpliterPanel") ? NWC.SplitterBar.SplitterParentSelectorMode.Accept : NWC.SplitterBar.SplitterParentSelectorMode.SearchAnother);
        }
        private AsolPanel _AsolPanel;
        private Panel _Splitter3Panel;
        private NWC.SplitterBar _Splitter3;
        private Label _SplitLabel;
        private void SetWorkingBounds(Control control)
        {
            var clientSize = control.ClientSize;
            Rectangle workingBounds = new Rectangle(16, 10, clientSize.Width - 32, clientSize.Height - 20);
            // _Splitter3.WorkingBounds = workingBounds;
        }
        #endregion
        #region Animace
        private void InitAnimation()
        {
            AddNewPage("Animace", PrepareAnimation, ActivateAnimation);
        }
        private DxPanelControl _PanelAnimation;
        private DxSplitContainerControl _SplitAnimation;
        private void PrepareAnimation(DxPanelControl panel)
        {
            _PanelAnimation = panel;
            _PanelAnimation.ClientSizeChanged += _PanelAnimation_AnySizeChanged;

            PointOfAnimation = new List<AnimationPoint>();

            _SplitAnimation = DxComponent.CreateDxSplitContainer(_PanelAnimation, splitterPositionChanged: _PanelAnimation_AnySizeChanged, dock: DockStyle.Fill, splitLineOrientation: Orientation.Vertical, fixedPanel: DevExpress.XtraEditors.SplitFixedPanel.Panel2, showSplitGlyph: true);
            _SplitAnimation.SizeChanged += _PanelAnimation_AnySizeChanged;
            _SplitAnimation.Panel1.SizeChanged += _PanelAnimation_AnySizeChanged;
            _SplitAnimation.ClientSizeChanged += _PanelAnimation_AnySizeChanged;

            _PanelAnimationGraphic = new DxPanelBufferedGraphic() { LogActive = true };
            _PanelAnimationGraphic.Layers = new DxBufferedLayer[] { DxBufferedLayer.AppBackground, DxBufferedLayer.MainLayer };
            _PanelAnimationGraphic.PaintLayer += _PanelAnimationGraphic_PaintLayer;
            _PanelAnimationGraphic.MouseDown += _PanelAnimationGraphic_MouseDown;
            _PanelAnimationGraphic.MouseMove += _PanelAnimationGraphic_MouseMove;
            _SplitAnimation.Panel1.Controls.Add(_PanelAnimationGraphic);

            _LogTextAnimation = DxComponent.CreateDxMemoEdit(_SplitAnimation.Panel2, System.Windows.Forms.DockStyle.Fill, readOnly: true, tabStop: false);


            //string imgFile = @"D:\Asol\Práce\Tools\TestDevExpress\TestDevExpress\Images\Animated kitty.gif";
            //if (System.IO.File.Exists(imgFile))
            //{
            //    System.Windows.Forms.PictureBox pcb = new PictureBox();
            //    pcb.Image = System.Drawing.Bitmap.FromFile(imgFile);
            //    var size = pcb.Image.Size;
            //    pcb.SizeMode = PictureBoxSizeMode.Zoom;
            //    pcb.Bounds = new Rectangle(new Point(20, 20), new Size(size.Width / 2, size.Height / 2));
            //    pcb.BackColor = Color.Transparent;
            //    _PanelAnimation.Controls.Add(pcb);
            //}

            _PanelAnimation_DoLayout();
        }

        private void _PanelAnimationGraphic_MouseDown(object sender, MouseEventArgs e)
        {
            Point point = e.Location;
            _AnimationLastTime = DateTime.Now;
            _AnimationLastPoint = point;
            PointOfAnimation.Add(new AnimationPoint(_PanelAnimationGraphic.ClientRectangle, point));
            _PanelAnimationGraphic.InvalidateLayers(DxBufferedLayer.MainLayer);
        }
        private void _PanelAnimationGraphic_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None) return;

            var now = DateTime.Now;
            var time = now - _AnimationLastTime;
            if (time.TotalMilliseconds < 70) return;

            var point = e.Location;
            int dx = point.X - _AnimationLastPoint.X;
            bool nox = (dx > -12 && dx < 12);
            int dy = point.Y - _AnimationLastPoint.Y;
            bool noy = (dy > -12 && dy < 12);
            if (nox && noy) return;

            _AnimationLastTime = now;
            _AnimationLastPoint = point;
            PointOfAnimation.Add(new AnimationPoint(_PanelAnimationGraphic.ClientRectangle, point));
            _PanelAnimationGraphic.InvalidateLayers(DxBufferedLayer.MainLayer);
        }
        private DateTime _AnimationLastTime;
        private Point _AnimationLastPoint;

        private void _PanelAnimationGraphic_PaintLayer(object sender, DxBufferedGraphicPaintArgs args)
        {
            var size = args.Size;
            switch (args.LayerId)
            {
                case DxBufferedLayer.AppBackground:
                    args.Graphics.DrawRectangle(System.Drawing.Pens.Violet, new System.Drawing.Rectangle(1, 1, size.Width - 3, size.Height - 3));
                    break;
                case DxBufferedLayer.MainLayer:
                    if (PointOfAnimation == null || PointOfAnimation.Count == 0)
                        PaintAnimationCircle(args, null);
                    else
                    {
                        args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        PointOfAnimation.ForEach(p => PaintAnimationCircle(args, p));
                    }
                    //args.Graphics.FillRectangle(System.Drawing.Brushes.LightBlue, new System.Drawing.Rectangle(40, 16, 120, 20));
                    //args.Graphics.FillRectangle(System.Drawing.Brushes.LightCyan, new System.Drawing.Rectangle(80, 42, 40, 60));
                    break;
            }
        }
        private void PaintAnimationCircle(DxBufferedGraphicPaintArgs args, AnimationPoint animationPoint)
        {
            if (animationPoint == null) return;
            using (SolidBrush b = new SolidBrush(animationPoint.Color))
                args.Graphics.FillEllipse(b, animationPoint.Bounds);
        }
        private List<AnimationPoint> PointOfAnimation;
        private class AnimationPoint
        {
            public AnimationPoint(Rectangle bounds)
            {
                int w = Randomizer.Rand.Next(16, 64);
                int h = Randomizer.Rand.Next(16, 64);
                int x = Randomizer.Rand.Next(8, bounds.Width - w - 8);
                int y = Randomizer.Rand.Next(8, bounds.Height - h - 8);
                Bounds = new Rectangle(x, y, w, h);

                int r = Randomizer.Rand.Next(32, 224);
                int g = Randomizer.Rand.Next(32, 224);
                int b = Randomizer.Rand.Next(32, 224);
                Color = Color.FromArgb(r, g, b);
            }
            public AnimationPoint(Rectangle bounds, Point center)
            {
                int w = Randomizer.Rand.Next(16, 64);
                int h = Randomizer.Rand.Next(16, 64);
                int x = center.X - (w / 2);
                int y = center.Y - (h / 2);
                Bounds = new Rectangle(x, y, w, h);

                int r = Randomizer.Rand.Next(32, 224);
                int g = Randomizer.Rand.Next(32, 224);
                int b = Randomizer.Rand.Next(32, 224);
                Color = Color.FromArgb(r, g, b);
            }
            public Rectangle Bounds;
            public Color Color;
        }
        private void ActivateAnimation()
        {
            DxComponent.LogClear();
            CurrentLogControl = _LogTextAnimation;

            PointOfAnimation = new List<AnimationPoint>();
            _PanelAnimationGraphic.InvalidateLayers(DxBufferedLayer.MainLayer);
        }
        private void _PanelAnimation_AnySizeChanged(object sender, EventArgs e)
        {
            _PanelAnimation_DoLayout();
        }
        private void _PanelAnimation_DoLayout()
        {
            var size = _SplitAnimation.Panel1.ClientSize;
            if (size.Width > 24 && size.Height > 24)
                _PanelAnimationGraphic.Bounds = new Rectangle(12, 12, size.Width - 24, size.Height - 24);
        }
        private DxPanelBufferedGraphic _PanelAnimationGraphic;
        private DxMemoEdit _LogTextAnimation;
        #endregion
        #region Resize
        private void InitResize()
        {
            AddNewPage("Resize", PrepareResize);
        }
        private DxPanelControl _PanelResize;
        private void PrepareResize(DxPanelControl panel)
        {
            _PanelResize = panel;

            _ChildResize = new PanelResize()
            {
                Bounds = new Rectangle(40, 40, 200, 100),
                BackColor = Color.FromArgb(255, 230, 240, 255),
                BorderStyle = BorderStyle.Fixed3D
            };

            _PanelResize.Controls.Add(_ChildResize);
            _PanelChildResizeSetPosition();
            _PanelResize.SizeChanged += _PanelResize_SizeChanged;
        }
        private void _PanelResize_SizeChanged(object sender, EventArgs e)
        {
            _PanelChildResizeSetPosition();
        }
        private void _PanelChildResizeSetPosition()
        {
            Rectangle clientBounds = _PanelResize.ClientRectangle;
            int d1 = 60;
            int d2 = 120;
            Rectangle childBounds = new Rectangle(clientBounds.X + d1, clientBounds.Y + d1, clientBounds.Width - d2, clientBounds.Height - d2);
            if (childBounds.Width < 10) childBounds.Width = 10;
            if (childBounds.Height < 10) childBounds.Height = 10;
            _ChildResize.Bounds = childBounds;
        }
        private PanelResize _ChildResize;
        #endregion
        #region Chart
        private void InitChart()
        {
            AddNewPage("Grafy", PrepareChart);
        }
        private DxPanelControl _PanelChart;
        private void PrepareChart(DxPanelControl panel)
        {
            _PanelChart = panel;

            NWC.ChartPanel chart = new NWC.ChartPanel() { Dock = DockStyle.Fill };
            chart.DataSource = NWC.ChartPanel.CreateSampleData();
            chart.ChartSettings = NWC.ChartPanel.CreateSampleSettings();
            _PanelChart.Controls.Add(chart);
        }
        #endregion
        #region MsgBox
        private void InitMsgBox()
        {
            AddNewPage("Dialog Box", PrepareMsgBox, ActivateMsgBoxPage, DeActivateMsgBoxPage);
        }
        private DxPanelControl _MsgBoxPanel;
        private void PrepareMsgBox(DxPanelControl panel)
        {
            _MsgBoxPanel = panel;
            _MsgBoxPanel.AutoScroll = true;
            int x0 = 15;
            int y0 = 38;
            int xs = 20;
            int ys = 6;
            int w = 320;
            int h = 35;
            int x = x0;
            int y = y0;

            DxComponent.CreateDxLabel(x0, 9, 250, _MsgBoxPanel, "Working Thread invoke to GUI thread:");
            _MsgBoxInvokedLabel = DxComponent.CreateDxLabel(x0 + 260, 6, 100, _MsgBoxPanel, "...");

            _MsgBoxInvokedLabel.Appearance.FontSizeDelta = 2;
            _MsgBoxInvokedLabel.Appearance.FontStyleDelta = FontStyle.Regular;

            _CreateOneButton("Dialog [ OK ]", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKClick); y += (h + ys);
            _CreateOneButton("Dialog [ OK ] / Center", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKCenterClick); y += (h + ys);

            // _CreateOneButton("Dialog [ OK ] / AutoCenter", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKAutoCenterClick); y += (h + ys);
            _CreateOneButton("Show Exception", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogException); y += (h + ys);

            _CreateOneButton("Show Extended Error", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogExtendedError); y += (h + ys);

            _CreateOneButton("Dialog Yes/No", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogYesNoClick); y += (h + ys);
            _CreateOneButton("Dialog Yes/No / Right", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogYesNoRightClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore / Right", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreRightRightClick); y += (h + ys);
            _CreateOneButton("Dialog Abort/Retry/Ignore / TopRight", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogAbortRetryIgnoreTopRightClick); y += (h + ys);
            _CreateOneButton("Dialog OK / HTML ", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKHtmlButtonClick); y += (h + ys);
            _CreateOneButton("Dialog Extra dlouhý text", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogOKExtraLongButtonClick); y += (h + ys);
            _CreateOneButton("Dialog Vícetlačítkový", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogMultiButtonButtonClick); y += (h + ys);

            x = x0 + w + xs;
            y = y0;

            _CreateOneButton("Otevři obyčejné okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenStandardFormClick); y += (h + ys);
            _CreateOneButton("Otevři TopMost okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenTopMostFormClick); y += (h + ys);
            _CreateOneButton("Otevři Modal okno", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgOpenModalFormClick); y += (h + ys);
           
            y += (h + ys);
            _CreateOneButton("Dialog InputTextLine", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogInputTextLineClick); y += (h + ys);
            _CreateOneButton("Dialog InputMemoLine", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogInputTextMemoClick); y += (h + ys);
            _CreateOneButton("Dialog NonModal", new Rectangle(x, y, w, h), _MsgBoxPanel, _MsgShowDialogNonModalClick); y += (h + ys);

            _MsgBoxResultLabel = DxComponent.CreateDxLabel(x, y, w + 60, _MsgBoxPanel, "Výsledek: ");
            _MsgBoxResultLabel.Appearance.FontSizeDelta = 2;
            _MsgBoxResultLabel.Appearance.FontStyleDelta = FontStyle.Regular;
        }
        private DxLabelControl _MsgBoxInvokedLabel;
        private DxLabelControl _MsgBoxResultLabel;
        private DateTime _MsgBoxActivateTime;
        private Guid? _MsgBoxTimerGuid;
        private void ActivateMsgBoxPage()
        {
            _MsgBoxInvokedLabel.Text = "";
            _MsgBoxActivateTime = DateTime.Now;
            _MsgBoxTimerGuid = WatchTimer.CallMeEvery(_MsgBoxRefreshGui, 50, false, _MsgBoxTimerGuid);
        }
        private void DeActivateMsgBoxPage()
        {
            _MsgBoxInvokedLabel.Text = "";
            if (_MsgBoxTimerGuid.HasValue)
                WatchTimer.Remove(_MsgBoxTimerGuid.Value);
        }
        private void _MsgBoxRefreshGui()
        {
            if (this.InvokeRequired)
                //  Fungují obě varianty:
                this.BeginInvoke(new Action(_MsgBoxRefreshGui));
                // this.Invoke(new Action(_MsgBoxRefreshGui));
            else
            {
                TimeSpan time = DateTime.Now - _MsgBoxActivateTime;
                string text = time.ToString(@"hh\.mm\.ss\.fff");
                _MsgBoxInvokedLabel.Text = text;
            }
        }
        private void _MsgShowDialogOKClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK]; " + Randomizer.GetSentences(3, 7, 4, 6);
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = Randomizer.GetSentences(4, 8, 3, 12);

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKCenterClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] Center";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Jistě, pane ministře.";
            dialogArgs.AutoCenterSmallText = true;
            //  dialogArgs.MessageHorizontalAlignment = AlignContentToSide.Center;
            //  dialogArgs.MessageVerticalAlignment = AlignContentToSide.Center;
            dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
            dialogArgs.StatusBarVisible = false;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKAutoCenterClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] AutoCenter";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Tento text má být automaticky vystředěn, pokud je v jednom řádku a má dostatek místa.";
            dialogArgs.AutoCenterSmallText = true;
            //  dialogArgs.MessageHorizontalAlignment = AlignContentToSide.Center;
            //  dialogArgs.MessageVerticalAlignment = AlignContentToSide.Center;
            dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
            dialogArgs.StatusBarVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogException(object sender, EventArgs args)
        {
            try { _DoExceptionGui(); }
            catch (Exception exc)
            {
                NWC.DialogArgs dialogArgs = NWC.DialogArgs.CreateForException(exc);
                DialogForm(dialogArgs);
            }
        }
        private void _MsgShowDialogExtendedError(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Helios Nephrite";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Warning;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Server nereaguje. Zkuste to znovu nebo kontaktujte správce.";
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.AltMessageText = @"Nepodařilo se přihlásit k serveru na adrese <b>«c:\inetpub\wwwroot\noris46\noris\bin\noris.dll»</b> jako uživatel <b>«daj»</b> k databázovému profilu <b>«local2017.NV46_source»</b>.
Aplikační server neodpověděl v časovém limitu 130 sekund. Opakujte pokus o přihlášení, nebo kontaktujte správce systému.";
            dialogArgs.AltMessageTextContainsHtml = true;
            DialogForm(dialogArgs);
        }
        private void _DoExceptionGui()
        {
            try { _DoExceptionMain(); }
            catch (Exception exc) { throw new InvalidOperationException("Chyba v GUI vrstvě [A]", exc); }
        }
        private void _DoExceptionMain()
        {
            try { _DoExceptionInner(); }
            catch (Exception exc) { throw new InvalidOperationException("Chyba v řídící vrstvě [B]", exc); }
        }
        private void _DoExceptionInner()
        {
            throw new ArgumentException("Chyba ve výkonné vrstvě [C], mnohořádková, dlouhá, nemá konce.\r\nJeště jeden řádek chyby, velice dlouhý, naprosto zbytečně rovláčný - stejně toho tolik nikdo nebude číst, ale něky potřebuje programátor otestovat text, který se snaží vecpat do titulku - a bude tam jen překážet...");
        }
        private void _MsgShowDialogYesNoClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog Yes/No";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Question;
            dialogArgs.MessageText = "Přejete si další chod k obědu?" + Environment.NewLine + Randomizer.GetSentences(4, 8, 3, 12);
            dialogArgs.PrepareButtons(DialogResult.Yes, DialogResult.No);
            dialogArgs.IconFile = "Quest";
            dialogArgs.Buttons[0].ImageFile = "Yes";
            dialogArgs.Buttons[1].ImageFile = "No";
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogYesNoRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog Yes/No / Right";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Question;
            dialogArgs.MessageText = "Přejete si další chod k obědu?" + Environment.NewLine + Randomizer.GetSentences(4, 8, 3, 12);
            dialogArgs.MessageHorizontalAlignment = AlignContentToSide.End;
            dialogArgs.MessageVerticalAlignment = AlignContentToSide.Center;
            dialogArgs.PrepareButtons(DialogResult.Yes, DialogResult.No);
            dialogArgs.Buttons[1].IsInitialButton = true;
            dialogArgs.ButtonPanelDock = DockStyle.Bottom;
            dialogArgs.ButtonsAlignment = AlignContentToSide.End;
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = Zkopíruj";
            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog Abort/Retry/Ignore";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Error;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + Randomizer.GetSentences(4, 8, 3, 12);
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.Buttons[2].IsInitialButton = true;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreRightRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog Abort/Retry/Ignore / RightRight";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Exclamation;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + Randomizer.GetSentences(4, 9, 8, 20);
            dialogArgs.MessageHorizontalAlignment = AlignContentToSide.End;
            dialogArgs.MessageVerticalAlignment = AlignContentToSide.End;
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.ButtonPanelDock = DockStyle.Right;
            dialogArgs.ButtonsAlignment = AlignContentToSide.End;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogAbortRetryIgnoreTopRightClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog Abort/Retry/Ignore / TopRight";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Hand;
            dialogArgs.MessageText = "Došlo k chybě. Můžete zrušit celou akci, nebo zopakovat pokus, anebo tuto chybu ignorovat a pokračovat dál..." + Environment.NewLine + Randomizer.GetSentences(4, 9, 8, 20);
            dialogArgs.PrepareButtons(DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore);
            dialogArgs.ButtonPanelDock = DockStyle.Right;
            dialogArgs.ButtonsAlignment = AlignContentToSide.Begin;
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKHtmlButtonClick(object sender, EventArgs args)
        {
            string html = @"<size=14><b><color=255,96,96><backcolor=0,0,0>Doklad není uložen</backcolor></color></b><size=11>
Změny provedené do tohoto dokladu nejsou dosud uloženy do databáze.
<b>Uložit</b> - změny budou uloženy a okno bude zavřeno
<b>Neukládat</b> - změny se neuloží, okno bude zavřeno
<b>Storno</b> - změny se neuloží, a okno zůstane otevřené
<size=14><b>Co si přejete provést?</b><size=11>
 ";

            html = html.Replace("'", "\"");

            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] / HTML";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Shield;
            dialogArgs.MessageText = html;
            dialogArgs.MessageTextContainsHtml = true;
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Uložit", ResultValue = "SAVE", StatusBarText = "Aktuální stav uloží do databáze", Image = DxComponent.CreateBitmapImage("Images/Actions24/document-save(24).png") });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Neukládat", ResultValue = "DISCARD", StatusBarText = "Aktuální změny se zahodí", Image = DxComponent.CreateBitmapImage("Images/Actions24/document-revert(24).png") });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Storno", ResultValue = "CANCEL", StatusBarText = "Nezavírat okno, neukládat změny", Image = DxComponent.CreateBitmapImage("Images/Actions24/edit-delete-9(24).png") });
            dialogArgs.StatusBarCtrlCVisible = true;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogOKExtraLongButtonClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] ExtraLong";
            dialogArgs.StatusBarVisible = true;
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.MessageText = Randomizer.Text_TaborSvatych;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.No);
            dialogArgs.ButtonsAlignment = AlignContentToSide.Center;
            dialogArgs.ButtonHeight = 26;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogMultiButtonButtonClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] ExtraLong";
            dialogArgs.Icon = DxComponent.CreateBitmapImage("Images/Actions48/help-hint(48).png");
            dialogArgs.MessageText = "Více tlačítek";
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = AlignContentToSide.End;
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Zkopíruj do schránky", ResultValue = "COPY", StatusBarText = "Zobrazený text zkopíruje do schránky Windows, pak můžete Ctrl+V text vložit jinam.", Image = DxComponent.CreateBitmapImage("Images/Actions24/edit-copy-3(24).png") });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Odešli mailem", ResultValue = "MAIL", StatusBarText = "Otevře novou mailovou zprávu, a do ní vloží tuto hlášku.", Image = DxComponent.CreateBitmapImage("Images/Actions24/document-import(24).png") });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Otevři v prohlížeči", ResultValue = "VIEW", StatusBarText = "Otevře hlášku v internetovém prohlížeči. Netuším, jak.", Image = DxComponent.CreateBitmapImage("Images/Actions24/go-home-9(24)") });
            dialogArgs.AddButton(new NWC.DialogArgs.ButtonInfo() { Text = "Zavřít", IsEscapeButton = true, ResultValue = "EXIT", StatusBarText = "Zavře okno, zavře i okenice a zhasne v kamnech.", Image = DxComponent.CreateBitmapImage("Images/Actions24/edit-delete-6(24).png") });
            // dialogArgs.StatusBarVisible = true;     nastaví se autodetekcí automaticky
            dialogArgs.ButtonHeight = 32;
            dialogArgs.UserZoomRatio = 1.15f;
            dialogArgs.DefaultResultValue = "CLOSE";

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogInputTextLineClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [InputTextLine]";
            dialogArgs.MessageText = "Zadejte prosím jedno svoje přání:";
            dialogArgs.InputTextType = NWC.ShowInputTextType.TextBox;
            dialogArgs.InputTextValue = "Moje přání je...";
            dialogArgs.InputTextStatusInfo = "Bez obav zadejte svoje přání, ale pozor - může se splnit!";
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = AlignContentToSide.Begin;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.Cancel);
            dialogArgs.DefaultResultValue = DialogResult.Cancel;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogInputTextMemoClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [InputTextMemo]";
            dialogArgs.MessageText = "Zadejte prosím několik svých přání, jedno na jeden řádek:";
            dialogArgs.InputTextType = NWC.ShowInputTextType.MemoEdit;
            dialogArgs.InputTextValue = "Nemám přání...";
            dialogArgs.InputTextStatusInfo = "Bez obav zadejte svoje přání, ale pozor - může se splnit!";
            dialogArgs.InputTextSize = new Size(300, 70);
            dialogArgs.StatusBarCtrlCText = "Ctrl+C = COPY";
            dialogArgs.ButtonsAlignment = AlignContentToSide.Begin;
            dialogArgs.PrepareButtons(DialogResult.OK, DialogResult.Cancel);
            dialogArgs.DefaultResultValue = DialogResult.Cancel;

            DialogForm(dialogArgs);
        }
        private void _MsgShowDialogNonModalClick(object sender, EventArgs args)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs();
            dialogArgs.Title = "Dialog [OK] NonModal";
            dialogArgs.SystemIcon = NWC.DialogSystemIcon.Information;
            dialogArgs.PrepareButtons(DialogResult.OK);
            dialogArgs.MessageText = "Jistě, pane premiére.";
            dialogArgs.ButtonsAlignment = AlignContentToSide.Center;

            DialogFormNonModal(dialogArgs);
        }
        private Image IconGenerator(string iconName)
        {
            string key = iconName.ToLower();
            switch (key)
            {
                case "quest": return DxComponent.CreateBitmapImage("Images/Actions24/help-3(24).png");
                case "yes": return DxComponent.CreateBitmapImage("Images/Actions24/dialog-ok-apply-2(24).png");
                case "no": return DxComponent.CreateBitmapImage("Images/Actions24/dialog-no-2(24).png");
            }
            return null;
        }
        private string LocalizerCZ(string code)
        {
            string key = code.ToLower();
            switch (key)
            {
                case "formtitleerror": return "Chyba";
                case "formtitleprefix": return "Došlo k chybě";

                case "ctrlctext": return "Ctrl+C";
                case "ctrlctooltip": return "Zkopíruje do schránky Windows celý text tohoto okna (titulek, informaci i texty tlačítek).\r\nPak je možno otevřít nový mail a klávesou Ctrl + V doň opsat obsah tohoto okna.";
                case "ctrlcinfo": return "Zkopírováno do schránky";

                case "altmsgbuttontext": return "Zobraz detaily";
                case "altmsgbuttontooltip": return "Zobrazí detailní informace";
                case "stdmsgbuttontext": return "Skryj detaily";
                case "stdmsgbuttontooltip": return "Zobrazí výchozí informace";

                case "dialogresult_ok": return "&OK";
                case "dialogresult_cancel": return "&Zrušit";
                case "dialogresult_abort": return "&Storno";
                case "dialogresult_retry": return "&Opakovat";
                case "dialogresult_ignore": return "&Ignorovat";
                case "dialogresult_yes": return "Ano";
                case "dialogresult_no": return "&Ne";
            }
            return null;
        }
        private string LocalizerEN(string code)
        {
            string key = code.ToLower();
            switch (key)
            {
                case "formtitleerror": return "Error";
                case "formtitleprefix": return "An error occured";

                case "ctrlctext": return "Ctrl+C";
                case "ctrlctooltip": return "Copies the entire text of this window (title, information and button texts) to the Windows clipboard.\r\nThen you can open a new mail and press Ctrl+V to copy the contents of this window.";
                case "ctrlcinfo": return "Copied to clipboard";

                case "dialogresult_ok": return "OK";
                case "dialogresult_cancel": return "Cancel";
                case "dialogresult_abort": return "Abort";
                case "dialogresult_retry": return "Retry";
                case "dialogresult_ignore": return "Ignore";
                case "dialogresult_yes": return "Yes";
                case "dialogresult_no": return "Oh, no";
            }
            return null;
        }
        private string LocalizerSK(string code)
        {
            string key = code.ToLower();
            switch (key)
            {
                case "formtitleerror": return "Chyba";
                case "formtitleprefix": return "Došlo k dákej chybe";

                case "ctrlctext": return "Ctrl+C";
                case "ctrlctooltip": return "Skopíruje do schránky Windows celý text tohto okna (titulok, informáciu i texty tlačidiel).\r\nPak je možné otvoriť nový mail a klávesom Ctrl + V doň opísať obsah tohto okna.";
                case "ctrlcinfo": return "Skopírované do schránky";

                case "dialogresult_ok": return "&Inu dobre";
                case "dialogresult_cancel": return "&Nekonaj";
                case "dialogresult_abort": return "&Zahoď";
                case "dialogresult_retry": return "&Ešte raz";
                case "dialogresult_ignore": return "&Nechaj tak";
                case "dialogresult_yes": return "Áno";
                case "dialogresult_no": return "&Nie";
            }
            return null;
        }
        private void DialogForm(NWC.DialogArgs dialogArgs)
        {
            _MsgBoxResultLabel.Text = "...";
            dialogArgs.Owner = OwnerWindow;
            var result = NWC.DialogForm.ShowDialog(dialogArgs);
            
            string text = $"Výsledek dialogu je: [{result}]";
            if (dialogArgs.InputTextType != NWC.ShowInputTextType.None)
                text += $"; Text: [{dialogArgs.InputTextValue}]";
            _MsgBoxResultLabel.Text = text;
        }
        private void ShowMessageInfo(string text)
        {
            NWC.DialogArgs dialogArgs = new NWC.DialogArgs()
            {
                Title = "Info",
                MessageText = text
            };
            dialogArgs.PrepareButtons(System.Windows.Forms.DialogResult.OK);
            NWC.DialogForm.ShowDialog(dialogArgs);
        }
        private void DialogFormNonModal(NWC.DialogArgs dialogArgs)
        {
            dialogArgs.Owner = OwnerWindow;
            NWC.DialogForm.Show(dialogArgs, DialogCallback);
        }
        private void DialogCallback(object sender, NWC.DialogFormClosingArgs args)
        {
            var dialogArgs = args.DialogArgs;
            object result = dialogArgs.ResultValue;
            string text = $"Výsledek dialogu je: [{result}]";
            if (dialogArgs.InputTextType != NWC.ShowInputTextType.None)
                text += $"; Text: [{dialogArgs.InputTextValue}]";
            _MsgBoxResultLabel.Text = text;
        }
        private Form OwnerWindow
        {
            get
            {
                var form = _LastWindow;
                if (form != null && form.TryGetTarget(out Form lastWindow)) return lastWindow;
                _LastWindow = null;
                return this;
            }
        }
        private WeakReference<Form> _LastWindow;
        private void _MsgOpenStandardFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [STANDARD]", false, false);
        }
        private void _MsgOpenTopMostFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [TOPMOST]", true, false);
        }
        private void _MsgOpenModalFormClick(object sender, EventArgs args)
        {
            _MsgOpenShowForm(" [MODAL]", true, true);
        }
        private void _MsgOpenShowForm(string subTitle, bool topMost, bool asModal)
        {
            Rectangle bounds = GetRandomRectangle();
            string caption = Randomizer.GetSentence(2, 5) + subTitle;
            string text = Randomizer.GetSentences(4, 8, 3, 12);
            DevExpress.XtraEditors.XtraForm form = new DevExpress.XtraEditors.XtraForm() { Bounds = bounds, Text = caption, TopMost = topMost, ShowInTaskbar = topMost };
            Label label = new Label() { Text = text, Name = "Label", AutoSize = false, Bounds = new Rectangle(12, 9, bounds.Width - 28, bounds.Height - 30), Font = SystemFonts.DialogFont };
            form.Controls.Add(label);
            SampleForm_SetLabelBounds(form, label);
            form.ClientSizeChanged += SampleForm_ClientSizeChanged;
            _LastWindow = new WeakReference<Form>(form);
            if (asModal)
                form.ShowDialog(this);
            else
                form.Show(this);
        }

        private void SampleForm_ClientSizeChanged(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form is null) return;
            int index = form.Controls.IndexOfKey("Label");
            if (index < 0) return;
            Label label = form.Controls[index] as Label;
            if (label is null) return;
            SampleForm_SetLabelBounds(form, label);
        }

        private void SampleForm_SetLabelBounds(Form form, Label label)
        {
            Size clientSize = form.ClientSize;
            Rectangle labelBounds = new Rectangle(12, 9, clientSize.Width - 24, clientSize.Height - 18);
            label.Bounds = labelBounds;
        }

        /// <summary>
        /// Vytvoří a vrátí Button
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="parent"></param>
        /// <param name="clickHandler"></param>
        /// <returns></returns>
        private Button _CreateOneButton(string text, Rectangle bounds, Control parent, EventHandler clickHandler)
        {
            Button button = new Button() { Bounds = bounds, Text = text };
            if (parent != null) parent.Controls.Add(button);
            if (clickHandler != null) button.Click += clickHandler;
            return button;
        }
        #endregion
        #region StepProgress
        private void InitStepProgress()
        {
            AddNewPage("StepProgress", PrepareStepProgress, ActivateStepProgress, DeactivateStepProgress);
        }
        private DxPanelControl _PanelStepProgress;
        private DevExpress.XtraEditors.StepProgressBar _StepProgressBar;
        private void PrepareStepProgress(DxPanelControl panel)
        {
            _PanelStepProgress = panel;
            _PanelStepProgress.AutoScroll = true;

            _StepProgressBar = new DevExpress.XtraEditors.StepProgressBar() { Bounds = new Rectangle(12, 12, 780, 150) };
            _StepProgressBar.CustomDrawItemIndicator += _StepProgressBar_CustomDrawItemIndicator;
            _StepProgressBar.CustomDrawConnector += _StepProgressBar_CustomDrawConnector;
            _PanelStepProgress.Controls.Add(_StepProgressBar);

            RefillStepProgress();
        }

        private void _StepProgressBar_CustomDrawConnector(object sender, DevExpress.XtraEditors.Drawing.StepProgressBarConnectorCustomDrawEventArgs e)
        {
            e.DefaultDraw();
        }

        private void _StepProgressBar_CustomDrawItemIndicator(object sender, DevExpress.XtraEditors.Drawing.StepProgressBarItemIndicatorCustomDrawEventArgs e)
        {
            bool isActive = (e.Item.Tag is bool) ? (bool)e.Item.Tag : false;
            if (isActive)
            {
                e.DefaultDrawIndicatorShadow();
                e.DefaultDrawImage();

                var bounds = e.IndicatorBounds;
                bounds.Width -= 1;
                bounds.Height -= 1;

                var color = DxComponent.SkinColorSet.NamedHighlightAlternate ?? DxComponent.SkinColorSet.NamedHighlight ?? Color.FromArgb(16, 110, 190);
                var pen = DxComponent.PaintGetPen(color, 220);
                pen.Width = 3;

                using (var path = DxComponent.CreateGraphicsPathOval(bounds, 1f, 0.450f))
                {
                    DxComponent.GraphicsSetForSmooth(e.Graphics);
                    e.Cache.DrawPath(pen, path);
                }

                // Nic dalšího mi nekresli:
                e.Handled = true;
            }
            else
            {   // Default vše:
                //   e.DefaultDrawImage();
                e.DefaultDrawImage();
                e.Handled = true;
            }
        }

        private void ActivateStepProgress()
        {
            RefillStepProgress();
        }
        private void DeactivateStepProgress()
        { }
        private void RefillStepProgress()
        {
            _StepProgressBar.ItemOptions.ConnectorOffset = 8;
            _StepProgressBar.ItemOptions.Indicator.Width = 64;
            _StepProgressBar.ProgressMode = DevExpress.XtraEditors.Controls.StepProgressBar.ProgressMode.SingleStep;
            _StepProgressBar.DrawConnectors = true;
            _StepProgressBar.ConnectorLineThickness = 4;
            _StepProgressBar.IndentBetweenItems = 24;
            _StepProgressBar.ScrollMode = DevExpress.XtraEditors.Controls.StepProgressBar.ScrollMode.Auto;


            string[] resources = new string[]
{
    "svgimages/diagramicons/exportdiagram_bmp.svg",
    "svgimages/diagramicons/exportdiagram_gif.svg",
    "svgimages/diagramicons/exportdiagram_jpeg.svg",
    "svgimages/diagramicons/exportdiagram_png.svg",
    "svgimages/diagramicons/exporttopdf.svg",
    "svgimages/diagramicons/exporttosvg.svg"
};

            _StepProgressBar.Items.Clear();
            _StepProgressBar.Items.Add(_CreateProgressBarItem(0, "s0", false, "Krok -2", "Předevčírem", resources[0]));
            _StepProgressBar.Items.Add(_CreateProgressBarItem(0, "s1", false, "Krok -1", "Včera", resources[1]));
            _StepProgressBar.Items.Add(_CreateProgressBarItem(0, "s2", true, "Krok 0", "Dnes", resources[2]));
            _StepProgressBar.Items.Add(_CreateProgressBarItem(0, "s3", false, "Krok 1", "Zítra", resources[3]));
            _StepProgressBar.Items.Add(_CreateProgressBarItem(0, "s4", false, "Krok 2", "Pozítří", resources[4]));

            _StepProgressBar.ScrollToItem(_StepProgressBar.Items[2]);
        }
        private DevExpress.XtraEditors.StepProgressBarItem _CreateProgressBarItem(int progress, string name, bool isActive, string text1, string text2, string imageName)
        {
            var item = new DevExpress.XtraEditors.StepProgressBarItem()
            {
                Name = name,
                Progress = progress,
                Tag = isActive,
                State = DevExpress.XtraEditors.StepProgressBarItemState.Inactive      // (isActive ? DevExpress.XtraEditors.StepProgressBarItemState.Active : DevExpress.XtraEditors.StepProgressBarItemState.Inactive)
            };
            item.ContentBlock1.Caption = text1;
            item.ContentBlock1.Description = "Popisek údaje nahoře ...";
            item.ContentBlock2.Caption = text2;
            item.ContentBlock2.Description = "Popisek údaje dole ...";
            item.Options.Indicator.InactiveStateDrawMode = (isActive ? DevExpress.XtraEditors.IndicatorDrawMode.Outline : DevExpress.XtraEditors.IndicatorDrawMode.None);
            item.Options.Indicator.Width = 48;
            DxComponent.ApplyImage(item.Options.Indicator.InactiveStateImageOptions, imageName);
            item.Options.Indicator.AutoCropImage = DevExpress.Utils.DefaultBoolean.True;
            if (isActive && false)
            {
                item.Appearance.InactiveIndicatorColor = Color.FromArgb(16, 110, 190);
                item.Appearance.ContentBlockAppearance.CaptionInactive.BackColor = Color.FromArgb(0, 0, 48);
            }
            item.Appearance.ContentBlockAppearance.CaptionInactive.FontStyleDelta = (isActive ? FontStyle.Bold : FontStyle.Regular);
            item.Appearance.ContentBlockAppearance.CaptionInactive.Options.UseFont = true;

            return item;
        }
        #endregion
        #region Editory
        private void InitEditors()
        {
            AddNewPage("Editory", PrepareEditors);
        }
        private DxPanelControl _PanelEditors;
        private void PrepareEditors(DxPanelControl panel)
        {
            _PanelEditors = panel;

            PrepareEditorToken();
            PrepareEditorButtonEdit();
        }
        #region Editor - Token
        private void PrepareEditorToken()
        { 
            _TokenLabel = new DxLabelControl() { Bounds = new Rectangle(25, 12, 250, 20), Text = "Zvolte počet prvků k přidání a stiskněte 'Generuj'" };
            _PanelEditors.Controls.Add(_TokenLabel);

            _TokenCountSpin = new DxSpinEdit() { Bounds = new Rectangle(20, 40, 90, 20), Value = 5000m };
            _TokenCountSpin.Properties.MinValue = 500m;
            _TokenCountSpin.Properties.MaxValue = 1000000m;
            _TokenCountSpin.Properties.EditMask = "### ### ##0";
            _TokenCountSpin.Properties.SpinStyle = DevExpress.XtraEditors.Controls.SpinStyles.Horizontal;
            _TokenCountSpin.Properties.Increment = 500m;

            _PanelEditors.Controls.Add(_TokenCountSpin);

            _TokenAddButtonGreen = new DxSimpleButton() { Bounds = new Rectangle(130, 37, 120, 28), Text = "Generuj GREEN" };
            _TokenAddButtonGreen.Click += _TokenAddButtonGreen_Click;
            _PanelEditors.Controls.Add(_TokenAddButtonGreen);

            _TokenAddButtonDaj = new DxSimpleButton() { Bounds = new Rectangle(260, 37, 120, 28), Text = "Generuj DAJ" };
            _TokenAddButtonDaj.Click += _TokenAddButtonDaj_Click;
            _PanelEditors.Controls.Add(_TokenAddButtonDaj);
           
            _TokenEdit = new DxTokenEdit() { Bounds = new Rectangle(20, 68, 360, 25) };
            _PanelEditors.Controls.Add(_TokenEdit);

            _TokenInfoLabel = new DxLabelControl { Bounds = new Rectangle(25, 100, 350, 20), Text = "" };
            _PanelEditors.Controls.Add(_TokenInfoLabel);

            _PanelEditors.SizeChanged += _EditorsPanel_SizeChanged;
            EditorPanelDoLayout();
        }
        /// <summary>
        /// Po změně velikosti <see cref="_PanelEditors"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _EditorsPanel_SizeChanged(object sender, EventArgs e)
        {
            EditorPanelDoLayout();
        }
        private void EditorPanelDoLayout()
        {
            var size = _PanelEditors.ClientSize;

            // if (_DxImagePicker != null) _DxImagePicker.Bounds = new Rectangle(20, 100, 640, size.Height - 106);
        }
        private void _TokenAddButtonGreen_Click(object sender, EventArgs e)
        {
            _TokenInfoLabel.Text = "probíhá příprava dat...";
            _TokenInfoLabel.Refresh();

            DateTime time0 = DateTime.Now;
            int count = (int)_TokenCountSpin.Value;
            var tokens = CreateTokenTuples(count);

            DateTime time1 = DateTime.Now;
            this._TokenEdit.Properties.Tokens.Clear();

            DateTime time2 = DateTime.Now;
            this._TokenEdit.Properties.BeginUpdate();
            foreach (var token in tokens)
            {
                this._TokenEdit.Properties.Tokens.AddToken(token.Item1, token.Item2);
            }
            this._TokenEdit.Properties.EndUpdate();
            DateTime time3 = DateTime.Now;

            string diff1 = ((TimeSpan)(time1 - time0)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff2 = ((TimeSpan)(time2 - time1)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff3 = ((TimeSpan)(time3 - time2)).TotalMilliseconds.ToString("### ### ##0").Trim();
            string message = $"Počet: {count}, Generátor: {diff1} ms, Clear: {diff2} ms, AddTokens: {diff3} ms";
            _TokenInfoLabel.Text = message;

            string tab = "\t";
            string clip = count.ToString() + tab + diff1.Replace(" ", "") + tab + diff2.Replace(" ", "") + tab + diff3.Replace(" ", "");
            Clipboard.Clear();
            Clipboard.SetText(clip);

        }
        private List<Tuple<string, int>> CreateTokenTuples(int count)
        {
            List<Tuple<string, int>> tokens = new List<Tuple<string, int>>();
            for (int n = 0; n < count; n++)
            {
                string text = Randomizer.GetSentence(1, 4, false);
                tokens.Add(new Tuple<string, int>(text, n));
            }
            return tokens;
        }
        private void _TokenAddButtonDaj_Click(object sender, EventArgs e)
        {
            _TokenInfoLabel.Text = "probíhá příprava dat...";
            _TokenInfoLabel.Refresh();

            DateTime time0 = DateTime.Now;
            int count = (int)_TokenCountSpin.Value;
            var tokens = CreateTokens(count);

            DateTime time1 = DateTime.Now;
            this._TokenEdit.Properties.Tokens.Clear();

            DateTime time2 = DateTime.Now;
            this._TokenEdit.Properties.BeginUpdate();
            this._TokenEdit.Properties.Tokens.AddRange(tokens);
            this._TokenEdit.Properties.EndUpdate();
            DateTime time3 = DateTime.Now;

            string diff1 = ((TimeSpan)(time1 - time0)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff2 = ((TimeSpan)(time2 - time1)).TotalMilliseconds.ToString("### ##0").Trim();
            string diff3 = ((TimeSpan)(time3 - time2)).TotalMilliseconds.ToString("### ### ##0").Trim();
            string message = $"Počet: {count}, Generátor: {diff1} ms, Clear: {diff2} ms, AddTokens: {diff3} ms";
            _TokenInfoLabel.Text = message;

            string tab = "\t";
            string clip = count.ToString() + tab + diff1.Replace(" ", "") + tab + diff2.Replace(" ", "") + tab + diff3.Replace(" ", "");
            Clipboard.Clear();
            Clipboard.SetText(clip);

        }
        private List<DevExpress.XtraEditors.TokenEditToken> CreateTokens(int count)
        {
            List<DevExpress.XtraEditors.TokenEditToken> tokens = new List<DevExpress.XtraEditors.TokenEditToken>();
            for (int n = 0; n < count; n++)
            {
                string text = Randomizer.GetSentence(1, 4, false);
                tokens.Add(new DevExpress.XtraEditors.TokenEditToken(text, n));
            }
            return tokens;
        }
        private DxLabelControl _TokenLabel;
        private DxSpinEdit _TokenCountSpin;
        private DxSimpleButton _TokenAddButtonGreen;
        private DxSimpleButton _TokenAddButtonDaj;
        private DxTokenEdit _TokenEdit;
        private DxLabelControl _TokenInfoLabel;
        #endregion
        #region Editor - ButtonEdit
        private void PrepareEditorButtonEdit()
        {
            _EditorText1 = new DxTextEdit() { Bounds = new Rectangle(20, 100, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple };
            _EditorTextButton1 = new DxButtonEdit() { Bounds = new Rectangle(400, 100, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple };
            _EditorTextButton1.ButtonClick += _EditorButtonClick;
            _EditorText2 = new DxTextEdit() { Bounds = new Rectangle(20, 130, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D };
            _EditorTextButton2 = new DxButtonEdit() { Bounds = new Rectangle(400, 130, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D };
            _EditorTextButton2.ButtonClick += _EditorButtonClick;
            _EditorText3 = new DxTextEdit() { Bounds = new Rectangle(20, 160, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003 };
            _EditorTextButton3 = new DxButtonEdit() { Bounds = new Rectangle(400, 160, 360, 20), BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003 };
            _EditorTextButton3.ButtonClick += _EditorButtonClick;
            _PanelEditors.Controls.Add(_EditorText1);
            _PanelEditors.Controls.Add(_EditorTextButton1);
            _PanelEditors.Controls.Add(_EditorText2);
            _PanelEditors.Controls.Add(_EditorTextButton2);
            _PanelEditors.Controls.Add(_EditorText3);
            _PanelEditors.Controls.Add(_EditorTextButton3);

            string resource1 = "devav/other/map.svg";
            string resource2 = "images/actions/apply_16x16.png";
            string resource3 = "images/actions/cancel_16x16.png";

            _EditorTextButton1.Properties.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            _EditorTextButton1.Properties.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
            _EditorTextButton1.ButtonsVisibility = DxChildControlVisibility.Allways;
            DxComponent.ApplyImage(_EditorTextButton1.Properties.Buttons[0].ImageOptions, resource1, imageSize: new Size(14, 14));

            _EditorTextButton2.Properties.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.UltraFlat;
            _EditorTextButton2.ButtonsVisibility = DxChildControlVisibility.OnActiveControl;
            _EditorTextButton2.Properties.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.OK;

            _EditorTextButton3.Properties.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            _EditorTextButton3.ButtonsVisibility = DxChildControlVisibility.OnFocus;
            _EditorTextButton3.Properties.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis;
        }
        private void _EditorButtonClick(object sender, EventArgs args)
        {
            if (sender is DxButtonEdit dxButtonEdit)
            {
                var kind = dxButtonEdit.Properties.Buttons[0].Kind;
                if (kind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph)
                    ShowMessageInfo("Tuhle ikonu nezměníme");
                else
                {
                    int value = (int)kind;
                    value = (value == 11 ? -10 : value + 1);

                    DevExpress.XtraEditors.Controls.ButtonPredefines newKind = (DevExpress.XtraEditors.Controls.ButtonPredefines)value;
                    if (newKind == DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph) newKind = (DevExpress.XtraEditors.Controls.ButtonPredefines)(value + 1);
                    dxButtonEdit.Properties.Buttons[0].Kind = newKind;
                    dxButtonEdit.Text = newKind.ToString();
                }
            }
            else
                ShowMessageInfo("Někdo kliknul na moje tlačítko?");
        }
        private DxTextEdit _EditorText1;
        private DxButtonEdit _EditorTextButton1;
        private DxTextEdit _EditorText2;
        private DxButtonEdit _EditorTextButton2;
        private DxTextEdit _EditorText3;
        private DxButtonEdit _EditorTextButton3;
        #endregion
        #endregion
        #region Svg ikony
        private void InitSvgIcons()
        {
            AddNewPage("SVG ikony", PrepareSvgIcons);
        }
        private DxPanelControl _PanelSvgIcons;
        private void PrepareSvgIcons(DxPanelControl panel)
        {
            _PanelSvgIcons = panel;

            PrepareSvgIconsContent();
        }
        private void PrepareSvgIconsContent()
        {
            _PanelSvgIcons.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;

            _PanelSvgIcons.ClientSizeChanged += _PanelSvgIcons_ClientSizeChanged;
            _SvgIconXmlText = new DxMemoEdit();
            _SvgIconXmlText.Font = new Font(FontFamily.GenericMonospace, 11f, FontStyle.Bold);
            _PanelSvgIcons.Controls.Add(_SvgIconXmlText);
            _SvgIconReloadButton = DxComponent.CreateDxSimpleButton(766, 70, 160, 26, _PanelSvgIcons, "F5: Reload SvgImage", _SvgIconReload);
            _SvgIconReloadButton.HotKey = Keys.F5;
            _SvgIconImage1 = new DxImageArea()
            {
                ImageName = _EditorImageName,
                UseCustomPalette = true,
                // BackColor = Color.FromArgb(60, Color.Wheat), 
                BorderColor = Color.FromArgb(120, Color.Black),
                EdgeColor = Color.FromArgb(120, Color.Violet),
                DotColor = Color.FromArgb(100, Color.DarkMagenta)
            };
            _SvgIconImage2 = new DxImageArea()
            {
                ImageName = _EditorImageName,
                UseCustomPalette = false,
                // BackColor = Color.FromArgb(60, Color.Wheat),
                BorderColor = Color.FromArgb(120, Color.Black),
                EdgeColor = Color.FromArgb(120, Color.Violet),
                DotColor = Color.FromArgb(100, Color.DarkMagenta)
            };
            DevExpress.XtraBars.BarItemLink bl;
            _PanelSvgIcons.PaintedItems.Add(_SvgIconImage1);
            _PanelSvgIcons.PaintedItems.Add(_SvgIconImage2);
            _PanelSvgDoLayout();
        }
        private void _PanelSvgIcons_ClientSizeChanged(object sender, EventArgs e)
        {
            _PanelSvgDoLayout();
        }
        private void _PanelSvgDoLayout()
        {
            var clientSize = _PanelSvgIcons.ClientSize;

            int contentY = 36;

            int minEditWidth = 300;
            int minImgWidth = 16;
            int maxImgWidth = 388;
            int minImgHeight = 16;
            int maxImgHeight = 320;

            int imageX = clientSize.Width - 6 - maxImgWidth;
            int image1Y = contentY;
            int imageRight = clientSize.Width - 6;
            int imageBottom = clientSize.Height - 6;
            int imageWidth = imageRight - imageX;
            int imageHeight = (imageBottom - image1Y - 6) / 2;
            if (imageWidth < minImgWidth)
            {
                imageWidth = minImgWidth;
                imageX = imageRight - imageWidth;
            }
            if (imageWidth > maxImgWidth)
            {
                imageWidth = maxImgWidth;
                imageX = imageRight - imageWidth;
            }
            if (imageHeight < minImgHeight)
            {
                imageHeight = minImgHeight;
                image1Y = imageBottom - imageHeight;
            }
            if (imageHeight > maxImgHeight)
            {
                imageHeight = maxImgHeight;
                imageBottom = image1Y + imageHeight;
            }
            int image2Y = image1Y + imageHeight + 6;

            int editX = 6;
            int editY = contentY;
            int editRight = imageX - 6;
            int editWidth = editRight - editX;
            if (editWidth < minEditWidth)
            {
                editWidth = minEditWidth;
                editRight = editX + editWidth;
                imageX = editRight + 6;
                imageWidth = imageRight - imageX;
                if (imageWidth < minImgWidth)
                {
                    imageWidth = minImgWidth;
                    imageRight = imageX + imageWidth;
                }
            }
            int editBottom = clientSize.Height - 6;
            int editHeight = editBottom - editY;

            int buttonRight = imageRight;
            int buttonWidth = imageWidth;
            if (buttonWidth < 100) buttonWidth = 100;
            int buttonX = buttonRight - buttonWidth;
            int buttonBottom = image1Y - 6;
            int buttonHeight = 26;
            int buttonY = buttonBottom - buttonHeight;

            _SvgIconXmlText.Bounds = new Rectangle(editX, editY, editWidth, editHeight);
            _SvgIconImage1.Bounds = new Rectangle(imageX, image1Y, imageWidth, imageHeight);
            _SvgIconImage2.Bounds = new Rectangle(imageX, image2Y, imageWidth, imageHeight);
            _SvgIconReloadButton.Bounds = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);
        }
        private void _SvgIconReload(object sender, EventArgs e)
        {
            string xmlContent = _SvgIconXmlText.Text;
            EditorImageName = xmlContent;
            _RunDjColorizeSetImage();
            _SvgIconXmlText.Focus();
        }
        /// <summary>
        /// Obrázek na záložce Editor 
        /// </summary>
        protected string EditorImageName
        {
            get { return _EditorImageName; }
            set
            {
                _EditorImageName = value;
                if (_SvgIconImage1 != null)
                    _SvgIconImage1.ImageName = value;
                if (_SvgIconImage2 != null)
                    _SvgIconImage2.ImageName = value;
                _PanelSvgIcons?.Invalidate();
            }
        }
        private string _EditorImageName = "pic_0/Menu/frmcopy";
        private DxSimpleButton _SvgIconReloadButton;
        private DxImageArea _SvgIconImage1;
        private DxImageArea _SvgIconImage2;
        private DxMemoEdit _SvgIconXmlText;
        #endregion
        #region TreeView
        private void InitTreeView()
        {
            AddNewPage("TreeList", PrepareTreeView, ActivateTreeView, pageToolTip: "Tato záložka zobrazí <b>TreeView</b>,\r\na demonstruje tak <br>ToolTipy a celé chování TreeListu - <u>včetně událostí</u>.");
            _TreeListCreateNodesData();
        }
        private DxPanelControl _PanelTreeView;
        private void PrepareTreeView(DxPanelControl panel)
        {
            _PanelTreeView = panel;
            CreateTreeViewComponents();
        }
        private void ActivateTreeView()
        {
            _TreeListLog = _TreeListLogInit;
            _TreeListLogId = _TreeListLogIdInit;
            _TreeListShowLogText();
        }
        private void CreateTreeViewComponents()
        {
            CreateImageList();
            CreateTreeView();
        }
        private void CreateImageList() { }
        //{
        //    _Images16 = new ImageList();
        //    _Images16.Images.Add("Ball01_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball01_16.png"));
        //    _Images16.Images.Add("Ball02_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball02_16.png"));
        //    _Images16.Images.Add("Ball03_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball03_16.png"));
        //    _Images16.Images.Add("Ball04_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball04_16.png"));
        //    _Images16.Images.Add("Ball05_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball05_16.png"));
        //    _Images16.Images.Add("Ball06_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball06_16.png"));
        //    _Images16.Images.Add("Ball07_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball07_16.png"));
        //    _Images16.Images.Add("Ball08_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball08_16.png"));
        //    _Images16.Images.Add("Ball09_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball09_16.png"));
        //    _Images16.Images.Add("Ball10_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball10_16.png"));
        //    _Images16.Images.Add("Ball11_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball11_16.png"));
        //    _Images16.Images.Add("Ball12_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball12_16.png"));
        //    _Images16.Images.Add("Ball13_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball13_16.png"));
        //    _Images16.Images.Add("Ball14_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball14_16.png"));
        //    _Images16.Images.Add("Ball15_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball15_16.png"));
        //    _Images16.Images.Add("Ball16_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball16_16.png"));
        //    _Images16.Images.Add("Ball17_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball17_16.png"));
        //    _Images16.Images.Add("Ball18_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball18_16.png"));
        //    _Images16.Images.Add("Ball19_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball19_16.png"));
        //    _Images16.Images.Add("Ball20_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball20_16.png"));
        //    _Images16.Images.Add("Ball21_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball21_16.png"));
        //    _Images16.Images.Add("Ball22_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball22_16.png"));
        //    _Images16.Images.Add("Ball23_16", DxComponent.CreateBitmapImage("Images/Icons16/Ball23_16.png"));

        //    _Images16.Images.Add("edit_add_4_16", DxComponent.CreateBitmapImage("Images/Icons16/edit-add-4_16.png"));
        //    _Images16.Images.Add("list_add_3_16", DxComponent.CreateBitmapImage("Images/Icons16/list-add-3_16.png"));
        //    _Images16.Images.Add("lock_5_16", DxComponent.CreateBitmapImage("Images/Icons16/lock-5_16.png"));
        //    _Images16.Images.Add("object_locked_2_16", DxComponent.CreateBitmapImage("Images/Icons16/object-locked-2_16.png"));
        //    _Images16.Images.Add("object_unlocked_2_16", DxComponent.CreateBitmapImage("Images/Icons16/object-unlocked-2_16.png"));
        //    _Images16.Images.Add("msn_blocked_16", DxComponent.CreateBitmapImage("Images/Icons16/msn-blocked_16.png"));
        //    _Images16.Images.Add("hourglass_16", DxComponent.CreateBitmapImage("Images/Icons16/hourglass_16.png"));
        //    _Images16.Images.Add("move_task_down_16", DxComponent.CreateBitmapImage("Images/Icons16/move_task_down_16.png"));
        //}
        //private int GetImageIndex(string imageName)
        //{
        //    return (_Images16.Images.ContainsKey(imageName) ? _Images16.Images.IndexOfKey(imageName) : -1);
        //}
        //ImageList _Images16;
        private void CreateTreeView()
        {
            _SplitContainer = DxComponent.CreateDxSplitContainer(this._PanelTreeView, null, DockStyle.Fill, Orientation.Vertical, DevExpress.XtraEditors.SplitFixedPanel.Panel1, 280, showSplitGlyph: true);

            _TreeMultiCheckBox = DxComponent.CreateDxCheckEdit(0,0,200,null, "MultiSelectEnabled", _TreeMultiCheckBoxChanged, DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, 
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                "MultiSelectEnabled = výběr více nodů", "Zaškrtnuto: lze vybrat více nodů (Ctrl, Shift). Sledujme pak události.");
            _TreeMultiCheckBox.Dock = DockStyle.Top;
            _TreeMultiCheckBox.Checked = true;

            _TreeList = new DxTreeList() { Dock = DockStyle.Fill };
            _TreeList.CheckBoxMode = TreeListCheckBoxMode.SpecifyByNode;
            _TreeList.ImageMode = TreeListImageMode.ImageStatic;
            _TreeList.LazyLoadNodeText = "Copak to tu asi bude?";
            _TreeList.LazyLoadNodeImageName = "hourglass_16";
            _TreeList.LazyLoadFocusNode = TreeListLazyLoadFocusNodeType.ParentNode;
            _TreeList.FilterBoxVisible = true;
            _TreeList.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;
            _TreeList.IncrementalSearchMode = TreeListIncrementalSearchMode.InAllNodes;
            _TreeList.FilterBoxOperators = DxFilterBox.CreateDefaultOperatorItems(FilterBoxOperatorItems.DefaultText);
            _TreeList.FilterBoxChangedSources = DxFilterBoxChangeEventSource.Default;
            _TreeList.MultiSelectEnabled = true;
            _TreeList.MainClickMode = NodeMainClickMode.AcceptNodeSetting;
            _TreeList.NodeImageSize = ResourceImageSizeType.Large;        // Zkus různé...
            _TreeList.NodeImageSize = ResourceImageSizeType.Medium;
            _TreeList.NodeAllowHtmlText = true;
            _TreeList.Parent = this;
            _SplitContainer.Panel1.Controls.Add(_TreeList);               // Musí být dřív než se začne pracovat s daty!!!
            _SplitContainer.Panel1.Controls.Add(_TreeMultiCheckBox);      // 

            var nodes = _TreeListGetPreparedNodeData();                   // Nody by měly být připraveny na pozadí, viz metoda _TreeListCreateNodesData() => _TreeListCreateNodesDataBgr()
            DateTime t1 = DateTime.Now;
            _TreeList.AddNodes(nodes);
            DateTime t2 = DateTime.Now;

            _TreeList.FilterBoxChanged += _TreeList_FilterBoxChanged;
            _TreeList.FilterBoxKeyEnter += _TreeList_FilterBoxKeyEnter;
            _TreeList.HotKeys = _CreateHotKeys();
            _TreeList.NodeKeyDown += _TreeList_NodeKeyDown;
            _TreeList.NodeFocusedChanged += _TreeList_AnyAction;
            _TreeList.SelectedNodesChanged += _TreeList_SelectedNodesChanged;
            _TreeList.ShowContextMenu += _TreeList_ShowContextMenu;
            _TreeList.NodeIconClick += _TreeList_IconClick;
            _TreeList.NodeDoubleClick += _TreeList_DoubleClick;
            _TreeList.NodeExpanded += _TreeList_AnyAction;
            _TreeList.NodeCollapsed += _TreeList_AnyAction;
            _TreeList.ActivatedEditor += _TreeList_AnyAction;
            _TreeList.EditorDoubleClick += _TreeList_DoubleClick;
            _TreeList.NodeEdited += _TreeList_NodeEdited;
            _TreeList.NodeCheckedChange += _TreeList_AnyAction;
            _TreeList.NodesDelete += _TreeList_NodesDelete;
            _TreeList.LazyLoadChilds += _TreeList_LazyLoadChilds;
            _TreeList.ToolTipChanged += _TreeList_ToolTipChanged;
            _TreeList.MouseLeave += _TreeList_MouseLeave;

            int y = 0;
            _TreeListMemoEdit = DxComponent.CreateDxMemoEdit(0, ref y, 100, 100, this._SplitContainer.Panel2, readOnly: true);
            _TreeListMemoEdit.Dock = DockStyle.Fill;
            _TreeListMemoEdit.MouseEnter += _TreeListMemoEdit_MouseEnter;
            _TreeListLogId = 0;
            _TreeListLog = "";

            string line = "Počet nodů: " + nodes.Count.ToString();
            _TreeListAddLogLine(line);
            line = "Tvorba nodů: " + _TreeListCreateNodeTime.Value.TotalMilliseconds.ToString("##0.000") + " ms";
            _TreeListAddLogLine(line);
            line = "Plnění do TreeView: " + ((TimeSpan)(t2 - t1)).TotalMilliseconds.ToString("##0.000") + " ms";
            _TreeListAddLogLine(line);

            _TreeListLogInit = _TreeListLog;
            _TreeListLogIdInit = _TreeListLogId;
        }
        private static Keys[] _CreateHotKeys()
        {
            Keys[] keys = new Keys[]
            {
                Keys.Delete,
                Keys.Control | Keys.N,
                Keys.Control | Keys.Delete,
                Keys.Enter,
                Keys.Control | Keys.Enter,
                Keys.Control | Keys.Shift | Keys.Enter,
                Keys.Control | Keys.Home,
                Keys.Control | Keys.End,
                Keys.F1,
                Keys.F2,
                Keys.Control | Keys.Space
            };
            return keys;
        }
        private void _TreeList_FilterBoxKeyEnter(object sender, EventArgs e)
        {
            _TreeListAddLogLine($"RowFilter: 'Enter' pressed");
        }
        private void _TreeList_FilterBoxChanged(object sender, DxFilterBoxChangeArgs args)
        {
            var filter = this._TreeList.FilterBoxValue;
            _TreeListAddLogLine($"RowFilter: Change: {args.EventSource}; Operator: {args.FilterValue.FilterOperator?.ItemId}, Text: \"{args.FilterValue.FilterText}\"");
        }
        private void _TreeMultiCheckBoxChanged(object sender, EventArgs e)
        {
            if (_TreeList == null) return;
            bool multiSelectEnabled = _TreeMultiCheckBox.Checked;
            _TreeList.MultiSelectEnabled = multiSelectEnabled;
            _TreeListAddLogLine($"MultiSelectEnabled: {multiSelectEnabled}");
        }
        private void _TreeList_NodeKeyDown(object sender, DxTreeListNodeKeyArgs args)
        {
            _TreeListAddLogLine($"KeyUp: Node: {args.Node?.Text}; KeyCode: '{args.KeyArgs.KeyCode}'; KeyData: '{args.KeyArgs.KeyData}'; Modifiers: {args.KeyArgs.Modifiers}");
        }
        private void _TreeList_AnyAction(object sender, DxTreeListNodeArgs args)
        {
            _AddTreeNodeLog(args.Action.ToString(), args, (args.Action == TreeListActionType.NodeEdited || args.Action == TreeListActionType.EditorDoubleClick || args.Action == TreeListActionType.NodeCheckedChange));
        }
        private void _TreeList_AnyAction(object sender, DxTreeListNodesArgs args)
        {
            _AddTreeNodeLog(args.Action.ToString(), args);
        }
        private void _TreeList_SelectedNodesChanged(object sender, DxTreeListNodeArgs args)
        {
            int count = 0;
            string selectedNodes = "";
            _TreeList.SelectedNodes.ForEachExec(n => { count++; selectedNodes += "; '" + n.ToString() + "'"; });
            if (selectedNodes.Length > 0) selectedNodes = selectedNodes.Substring(2);
            _TreeListAddLogLine($"SelectedNodesChanged: Selected {count} Nodes: {selectedNodes}");
        }
        private void _TreeList_ShowContextMenu(object sender, DxTreeListNodeContextMenuArgs args)
        {
            _TreeListAddLogLine($"ShowContextMenu: Node: {args.Node} Part: {args.HitInfo.PartType}");
            if (args.Node != null)
                _ShowDXPopupMenu(Control.MousePosition);
        }
        private void _TreeList_LazyLoadChilds(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _LoadChildNodesFromServerBgr(args));
        }
        private void _LoadChildNodesFromServerBgr(DxTreeListNodeArgs args)
        {
            string parentNodeId = args.Node.ItemId;
            _TreeListAddLogLine($"Načítám data pro node '{parentNodeId}'...");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            // Upravíme hodnoty v otevřeném nodu:
            string text = args.Node.Text;
            if (text.EndsWith(" ..."))
            {
                if (args.Node is DataTreeListNode node)
                {
                    node.Text = text.Substring(0, text.Length - 4);
                    node.MainClickAction = NodeMainClickActionType.ExpandCollapse;
                    node.Refresh();
                }
            }

            // Vytvoříme ChildNodes a zobrazíme je:
            bool empty = (Randomizer.Rand.Next(10) > 7);
            int totalCount = 0;
            var nodes = _CreateSampleChilds(parentNodeId, ref totalCount, 99, ItemCountType.Standard);       // A pak vyrobíme Child nody
            _TreeListAddLogLine($"Načtena data: {nodes.Count} prvků.");
            _TreeList.AddLazyLoadNodes(parentNodeId, nodes);            //  a pošleme je do TreeView.
        }
        private void _TreeList_NodeEdited(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeEditedBgr(args));
        }
        private void _TreeNodeEditedBgr(DxTreeListNodeArgs args)
        {
            var nodeInfo = args.Node;
            string nodeId = nodeInfo.ItemId;
            string parentNodeId = nodeInfo.ParentNodeFullId;
            string oldValue = nodeInfo.Text;
            string newValue = (args.EditedValue is string text ? text : "");
            _TreeListAddLogLine($"Změna textu pro node '{nodeId}': '{oldValue}' => '{newValue}'");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            var newPosition = _NewNodePosition;
            bool isBlankNode = (oldValue == "" && (newPosition == NewNodePositionType.First || newPosition == NewNodePositionType.Last));
            int totalCount = 0;
            if (String.IsNullOrEmpty(newValue))
            {   // Delete node:
                if (nodeInfo.CanDelete)
                    _TreeList.RemoveNode(nodeId);
            }
            else if (nodeInfo.NodeType == NodeItemType.BlankAtFirstPosition) // isBlankNode && newPosition == NewNodePositionType.First)
            {   // Insert new node, a NewPosition je First = je první (jako Green):
                _TreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    node.Text = "";                                 // Z prvního node odeberu jeho text, aby zase vypadal jako nový node
                    node.Refresh();

                    // Přidám nový node pro konkrétní text = jakoby záznam:
                    DataTreeListNode newNode = _CreateChildNode(node.ParentNodeFullId, NodeItemType.DefaultText, ref totalCount);
                    newNode.Text = newValue;
                    _TreeList.AddNode(newNode, 1);
                }
                ), nodeInfo);
            }
            else if (isBlankNode && newPosition == NewNodePositionType.Last)
            {   // Insert new node, a NewPosition je Last = na konci:
                _TreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    _TreeList.RemoveNode(node.ItemId);              // Odeberu blank node, to kvůli pořadí: nový blank přidám nakonec

                    // Přidám nový node pro konkrétní text = jakoby záznam:
                    DataTreeListNode newNode = _CreateChildNode(node.ParentNodeFullId, NodeItemType.DefaultText, ref totalCount);
                    newNode.Text = newValue;
                    _TreeList.AddNode(newNode);

                    // Přidám Blank node, ten bude opět na konci Childs:
                    DataTreeListNode blankNode = _CreateChildNode(node.ParentNodeFullId, NodeItemType.BlankAtLastPosition, ref totalCount);
                    _TreeList.AddNode(blankNode);

                    // Aktivuji editovaný node:
                    _TreeList.SetFocusToNode(newNode);
                }
                ), nodeInfo);
            }
            else
            {   // Edited node:
                if (args.Node is DataTreeListNode node)
                {
                    node.Text = newValue + " [OK]";
                    node.Refresh();
                }
            }
        }
        private void _TreeList_IconClick(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
        }
        private void _TreeList_DoubleClick(object sender, DxTreeListNodeArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeDoubleClickBgr(args));
        }
        private void _TreeNodeDoubleClickBgr(DxTreeListNodeArgs args)
        {
            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            if (args.Node.NodeType == NodeItemType.OnDoubleClickLoadNext)
            {
                _TreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    _TreeList.RemoveNode(node.ItemId);              // Odeberu OnDoubleClickLoadNext node, to kvůli pořadí: nový OnDoubleClickLoadNext přidám (možná) nakonec

                    int totalCount = 0;
                    var newNodes = _CreateSampleChilds(node.ParentNodeFullId, ref totalCount, 99, ItemCountType.Standard, false, true);
                    _TreeList.AddNodes(newNodes);

                    // Aktivuji první přidaný node:
                    if (newNodes.Count > 0)
                        _TreeList.SetFocusToNode(newNodes[0]);
                }
               ), args.Node);
            }
        }
        private void _TreeList_NodesDelete(object sender, DxTreeListNodesArgs args)
        {
            _TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => _TreeNodeDeleteBgr(args));
        }
        private void _TreeNodeDeleteBgr(DxTreeListNodesArgs args)
        {
            var removeNodeKeys = args.Nodes.Select(n => n.ItemId).ToArray();

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            _TreeList.RemoveNodes(removeNodeKeys);
        }
        private void _TreeList_MouseLeave(object sender, EventArgs e)
        {
            if (_TreeListPending)
                _TreeListAddLogLine("TreeList.MouseLeave");
        }
        private void _TreeListMemoEdit_MouseEnter(object sender, EventArgs e)
        {
            if (_TreeListPending)
                _TreeListShowLogText();
        }
        private void _TreeList_ToolTipChanged(object sender, DxToolTipArgs args)
        {
            string line = "ToolTip: " + args.EventName;
            bool skipGUI = (line.Contains("IsFASTMotion"));             // ToolTip obsahující IsFASTMotion nebudu dávat do GUI Textu - to jsou rychlé eventy:
            _TreeListAddLogLine(line, skipGUI);
        }
        private void _AddTreeNodeLog(string actionName, DxTreeListNodeArgs args, bool showValue = false)
        {
            string value = (showValue ? ", Value: " + (args.EditedValue == null ? "NULL" : "'" + args.EditedValue.ToString() + "'") : "");
            _TreeListAddLogLine($"{actionName}: Node: {args.Node}{value}");
        }
        private void _AddTreeNodeLog(string actionName, DxTreeListNodesArgs args)
        {
            string nodes = args.Nodes.ToOneString("; ");
            _TreeListAddLogLine($"{actionName}: Nodes: {nodes}");
        }
        private void _TreeListAddLogLine(string line, bool skipGUI = false)
        {
            int id = ++_TreeListLogId;
            var now = DateTime.Now;
            bool isLong = (_TreeListLogTime.HasValue && ((TimeSpan)(now - _TreeListLogTime.Value)).TotalMilliseconds > 750d);
            string log = id.ToString() + ". " + line + Environment.NewLine + (isLong ? Environment.NewLine : "") + _TreeListLog;
            _TreeListLog = log;
            _TreeListLogTime = now;
            if (skipGUI) _TreeListPending = true;
            else _TreeListShowLogText();
        }
        private void _TreeListShowLogText()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(_TreeListShowLogText));
            else
            {
                _TreeListMemoEdit.Text = _TreeListLog;
                _TreeListPending = false;
            }
        }
        DateTime? _TreeListLogTime;
        int _InternalNodeId;


        DxSplitContainerControl _SplitContainer;
        DxCheckEdit _TreeMultiCheckBox;
        DxTreeList _TreeList;
        DxMemoEdit _TreeListMemoEdit;
        string _TreeListLog;
        int _TreeListLogId;
        string _TreeListLogInit;
        int _TreeListLogIdInit;
        bool _TreeListPending;
        NewNodePositionType _NewNodePosition;
        private enum NewNodePositionType { None, First, Last }
        private ResourceContentType _TreeListImageType;
        #region Předpříprava nodů do TreeListu
        private void _TreeListCreateNodesData()
        {
            bool useSvg = (Randomizer.IsTrue(40));
            _TreeListImageType = (useSvg ? ResourceContentType.Vector : ResourceContentType.Bitmap);
            _TreeListImageType = ResourceContentType.Vector;
            _NewNodePosition = NewNodePositionType.First;

            _TreeListNodeData = null;
            _TreeListCreateNodeTime = null;

            ThreadManager.AddAction(_TreeListCreateNodesDataBgr);
        }
        private void _TreeListCreateNodesDataBgr()
        {
            DateTime t0 = DateTime.Now;
            int totalCount = 0;
            var nodes = _CreateSampleTreeNodes(ItemCountType.Big, out totalCount);    // ItemCountType.Big);
            DateTime t1 = DateTime.Now;

            _TreeListNodeData = nodes;
            _TreeListNodeDataCount = totalCount;
            _TreeListCreateNodeTime = t1 - t0;
        }
        /// <summary>
        /// GUI chce dostat vygenerovaný soupis Nodes. Ten se měl vygenerovat v threadu na pozadí = v mezičase od inicializace Formu do přepnutí na záložku "TreeList".
        /// Pokud by tam někdo přepnul tak rychle že ještě není vygenerováno, tato metoda na to počká.
        /// </summary>
        /// <returns></returns>
        private List<DataTreeListNode> _TreeListGetPreparedNodeData()
        {
            DateTime waitEnd = DateTime.Now.AddSeconds(30d);         // I kdyby sem někdo přišel hned, tak počkáme do 30 sekund. Tvorba nodů trvá nejvýše 4 sekundy.
            while (!_TreeListCreateNodeTime.HasValue && (DateTime.Now <= waitEnd))      // Po vygenerování se nejprve setuje _TreeListNodeData, a poté _TreeListCreateNodeTime.
                System.Threading.Thread.Sleep(50);
            return _TreeListNodeData;
        }
        private List<DataTreeListNode> _TreeListNodeData;
        private int _TreeListNodeDataCount;
        private TimeSpan? _TreeListCreateNodeTime;
        private List<DataTreeListNode> _CreateSampleTreeNodes(ItemCountType countType, out int totalCount)
        {
            if (_TreeListImageType == ResourceContentType.None)
                _TreeListImageType = ResourceContentType.Vector;

            List<DataTreeListNode> list = new List<DataTreeListNode>();

            totalCount = 0;
            int rootCount = GetItemCount(countType, false);
            for (int r = 0; r < rootCount; r++)
            {   // Root nodes:
                bool isLazy = (Randomizer.Rand.Next(10) >= 5);
                bool addChilds = !isLazy && (Randomizer.Rand.Next(10) >= 3);
                bool isExpanded = (addChilds && (Randomizer.Rand.Next(10) >= 2));

                string rootKey = "R." + (++_InternalNodeId).ToString();
                string text = Randomizer.GetSentence(2, 5) + (isLazy ? " ..." : "");
                FontStyle fontStyleDelta = FontStyle.Bold;
                DataTreeListNode rootNode = new DataTreeListNode(rootKey, null, text, nodeType: NodeItemType.DefaultText, expanded: isExpanded, lazyExpandable: isLazy, fontStyleDelta: fontStyleDelta);
                totalCount++;
                // Node v první úrovni: LazyLoad má MainClick = RunEvent, a naplněný node má MainClick = Expand/Collapse:
                rootNode.MainClickAction = (isLazy ? NodeMainClickActionType.RunEvent : NodeMainClickActionType.ExpandCollapse);
                _FillNode(rootNode);
                list.Add(rootNode);

                if (addChilds)
                    list.AddRange(_CreateSampleChilds(rootKey, ref totalCount, 0, countType));
            }
            return list;
        }
        private List<DataTreeListNode> _CreateSampleChilds(string parentKey, ref int totalCount, int parentLevel, ItemCountType countType, bool canAddEditable = true, bool canAddShowNext = true)
        {
            List<DataTreeListNode> list = new List<DataTreeListNode>();

            var newPosition = _NewNodePosition;
            int childCount = GetItemCount(countType, true);
            int lastIndex = childCount - 1;
            bool addEditable = canAddEditable && (Randomizer.Rand.Next(20) >= 8);
            bool addShowNext = canAddShowNext && (childCount < 25 && (Randomizer.Rand.Next(20) >= 4));
            if (addEditable) childCount++;
            for (int c = 0; c < childCount; c++)
            {
                NodeItemType nodeType = ((addEditable && newPosition == NewNodePositionType.First && c == 0) ? NodeItemType.BlankAtFirstPosition :
                                        ((addEditable && newPosition == NewNodePositionType.Last && c == lastIndex) ? NodeItemType.BlankAtLastPosition : NodeItemType.DefaultText));
                var childNode = _CreateChildNode(parentKey, nodeType, ref totalCount);
                if (childNode != null)
                {
                    list.Add(childNode);
                    bool addChilds = canAddChildNodes();
                    if (addChilds)
                        list.AddRange(_CreateSampleChilds(childNode.ItemId, ref totalCount, parentLevel + 1, countType));
                }
            }
            if (addShowNext)
            {
                list.Add(_CreateChildNode(parentKey, NodeItemType.OnDoubleClickLoadNext, ref totalCount));
            }

            return list;


            bool canAddChildNodes()
            {
                int currentLevel = parentLevel + 1;
                bool isEnabled = (countType == ItemCountType.Standard ? (currentLevel <= 0) :
                                 (countType == ItemCountType.Big ? (currentLevel <= 1) :
                                 (countType == ItemCountType.SuperBig ? (currentLevel <= 2) : false)));
                if (!isEnabled) return false;
                return (Randomizer.Rand.Next(100) < 10);
            }
        }
        private DataTreeListNode _CreateChildNode(string parentKey, NodeItemType nodeType, ref int totalCount)
        {
            if (totalCount > 25000) return null;

            string childKey = "C." + (++_InternalNodeId).ToString();
            string text = "";
            DataTreeListNode childNode = null;
            switch (nodeType)
            {
                case NodeItemType.BlankAtFirstPosition:
                case NodeItemType.BlankAtLastPosition:
                    text = "";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: true, canDelete: false);          // Node pro přidání nového prvku (Blank) nelze odstranit
                    totalCount++;
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Zadejte referenci nového prvku";
                    childNode.ImageDynamicDefault = "list_add_3_16";
                    break;
                case NodeItemType.OnDoubleClickLoadNext:
                    text = "Načíst další záznamy";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: false, canDelete: false);        // Node pro zobrazení dalších nodů nelze editovat ani odstranit
                    totalCount++;
                    childNode.FontStyle = FontStyle.Italic;
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Umožní načíst další sadu záznamů...";
                    childNode.ImageDynamicDefault = "move_task_down_16";
                    break;
                case NodeItemType.DefaultText:
                    text = Randomizer.GetSentence(2, 5);
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, canEdit: true, canDelete: true);
                    totalCount++;
                    childNode.CanCheck = true;
                    childNode.Checked = (Randomizer.Rand.Next(20) > 16);
                    _FillNode(childNode);
                    break;
            }
            return childNode;
        }
        private void _FillNode(DataTreeListNode node)
        {
            if (GetRandomTrue(25))
                node.ImageDynamicDefault = "object_locked_2_16";

            // Ikony vektorové / bitmapové:
            if (_TreeListImageType == ResourceContentType.Vector)
                // node.ImageName = this.GetRandomSysSvgName(false, true);
                node.ImageName = this.GetRandomApplicationSvgName();
            else
                node.ImageName = this.GetRandomBallImageName();

            node.ToolTipTitle = null; // RandomText.GetRandomSentence(2, 5);
            node.ToolTipText = Randomizer.GetSentence(10, 50);
        }
        private int GetItemCount(ItemCountType countType, bool forChilds)
        {
            switch (countType)
            {
                case ItemCountType.Empty: return 0;
                case ItemCountType.Standard: return (forChilds ? Randomizer.Rand.Next(1, 12) : Randomizer.Rand.Next(5, 15));
                case ItemCountType.Big: return (forChilds ? Randomizer.Rand.Next(20, 40) : Randomizer.Rand.Next(60, 120));
                case ItemCountType.SuperBig: return (forChilds ? Randomizer.Rand.Next(40, 80) : Randomizer.Rand.Next(100, 200));
            }
            return 0;
        }
        private enum ItemCountType { Empty, Standard, Big, SuperBig }
        #endregion
        #endregion
        #region DragDrop
        private void InitDragDrop()
        {
            AddNewPage("Drag and Drop", PrepareDragDrop, ActivateDragDropPage);
        }
        private DxPanelControl _PanelDragDrop;
        private void PrepareDragDrop(DxPanelControl panel)
        {
            _PanelDragDrop = panel;

            ControlKeyActionType sourceKeyActions = ControlKeyActionType.SelectAll | ControlKeyActionType.ClipCopy;
            DxDragDropActionType sourceDDActions = DxDragDropActionType.CopyItemsFrom;
            _DragDropAList = new DxListBoxPanel() { SelectionMode = SelectionMode.MultiExtended, DragDropActions = sourceDDActions, EnabledKeyActions = sourceKeyActions };
            _DragDropAList.Name = "AList";
            _DragDropAList.ListItems = _CreateSampleListItems(100, false, true);
            _DragDropAList.MouseDown += _DragDrop_MouseDown;
            _DragDropAList.DataExchangeCrossType = DataExchangeCrossType.None;
            _DragDropAList.RowFilterMode = DxListBoxPanel.FilterRowMode.Server;
            _PanelDragDrop.Controls.Add(_DragDropAList);

            ControlKeyActionType targetKeyActions = ControlKeyActionType.All;
            DxDragDropActionType targetDDActions = DxDragDropActionType.ReorderItems | DxDragDropActionType.ImportItemsInto | DxDragDropActionType.CopyItemsFrom | DxDragDropActionType.MoveItemsFrom;
            _DragDropBList = new DxListBoxPanel() { SelectionMode = SelectionMode.MultiExtended, DragDropActions = targetDDActions, EnabledKeyActions = targetKeyActions };
            _DragDropBList.Name = "BList";
            _DragDropBList.DuplicityEnabled = false;
            _DragDropBList.ListItems = _CreateSampleListItems(18, true, false);
            _DragDropBList.MouseDown += _DragDrop_MouseDown;
            _DragDropBList.DataExchangeCrossType = DataExchangeCrossType.AllControlsInCurrentApplication | DataExchangeCrossType.AnyOtherApplications;
            _DragDropBList.RowFilterMode = DxListBoxPanel.FilterRowMode.Server;
            _DragDropBList.ButtonsPosition = ToolbarPosition.BottomSideCenter;
            _DragDropBList.ButtonsTypes = ControlKeyActionType.MoveAll;
            _PanelDragDrop.Controls.Add(_DragDropBList);

            _DragDropCTree = new DxTreeList() { FilterBoxVisible = true, DragDropActions = targetDDActions, EnabledKeyActions = sourceKeyActions };
            _DragDropCTree.Name = "CTree";
            _DragDropCTree.MultiSelectEnabled = true;
            _DragDropCTree.SelectNodeBeforeShowContextMenu = false;
            _DragDropCTree.TransparentBackground = true;

            int totalCount;
            var nodes = _CreateSampleTreeNodes(ItemCountType.Standard, out totalCount);
            nodes.ForEachExec(n => { if (Randomizer.IsTrue(5)) n.Selected = true; });
            _DragDropCTree.AddNodes(nodes);
            
            _DragDropCTree.ShowContextMenu += _DragDropATree_ShowContextMenu;
            _DragDropCTree.MouseDown += _DragDrop_MouseDown;
            _PanelDragDrop.Controls.Add(_DragDropCTree);

            _DragDropLogText = DxComponent.CreateDxMemoEdit(_PanelDragDrop, System.Windows.Forms.DockStyle.None, readOnly: true, tabStop: false);

            _PanelDragDrop.SizeChanged += _DragDropPanel_SizeChanged;
            _PanelDragDrop.Dock = DockStyle.Fill;
            DragDropDoLayout();
        }
        private void _DragDropATree_ShowContextMenu(object sender, DxTreeListNodeContextMenuArgs args)
        {
            DxTreeList dxTreeList = sender as DxTreeList;
            var nodes = new List<IMenuItem>(dxTreeList.SelectedNodes);
            var clickNode = args.Node;
            if (clickNode != null && !nodes.Any(n => Object.ReferenceEquals(n, clickNode)))
                nodes.Add(DataMenuItem.CreateClone(clickNode, c => { c.ItemIsFirstInGroup = true; c.Checked = true; }));

            var menu = DxComponent.CreateDXPopupMenu(nodes, "SelectedNodes:");
            Point localPoint = dxTreeList.PointToClient(args.MousePosition);
            menu.ShowPopup(dxTreeList, localPoint);
        }
        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("První nabídka");
            contextMenu.Items.Add("Druhá nabídka");
            contextMenu.Items.Add("Poslední nabídka");
            contextMenu.Opening += ContextMenu_Opening;
            return contextMenu;
        }
        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (sender is ContextMenuStrip contextMenu)
            {
                int newPos = contextMenu.Items.Count + 1;
                string text = $"[{newPos}]: nabídka OnOpening";
                contextMenu.Items.Add(text);
            }
        }
        private void _DragDrop_MouseDown(object sender, MouseEventArgs e)
        {
            DxComponent.LogClear();
        }
        private void ActivateDragDropPage()
        {
            CurrentLogControl = _DragDropLogText;
            DxComponent.LogClear();
        }
        /// <summary>
        /// Po změně velikosti <see cref="_DragDropPanel"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DragDropPanel_SizeChanged(object sender, EventArgs e)
        {
            DragDropDoLayout();
        }
        /// <summary>
        /// Vygeneruje prvky menu v daném počtu a s daným typem Images
        /// </summary>
        /// <param name="count"></param>
        /// <param name="fileTypes"></param>
        /// <param name="chartTypes"></param>
        /// <returns></returns>
        private IMenuItem[] _CreateSampleListItems(int count, bool fileTypes = true, bool chartTypes = true)
        {
            List<IMenuItem> items = new List<IMenuItem>();
            for (int i = 0; i < count; i++)
            {
                string text = Randomizer.GetSentence(3, 6, false);
                string toolTip = Randomizer.GetSentences(2, 8, 1, 5);
                string image = this.GetRandomSysSvgName(fileTypes, chartTypes);
                DataMenuItem item = new DataMenuItem() { Text = $"[{i}]. {text}", ToolTipTitle = text, ToolTipText = toolTip, ImageName = image };
                items.Add(item);
            }
            return items.ToArray();
        }
        private void DragDropDoLayout()
        {
            var size = _PanelDragDrop.ClientSize;
            int xm = 6;
            int ym = 6;
            int xs = 12;
            int cnt = 4;

            int w = (size.Width - (2 * xm + (cnt - 1) * xs)) / 4;
            int ws = w + xs;
            int h = size.Height - 20;
            if (_DragDropAList != null) _DragDropAList.Bounds = new Rectangle(xm, ym, w, h);
            if (_DragDropBList != null) _DragDropBList.Bounds = new Rectangle(xm + 1 * ws, ym, w, h);
            if (_DragDropCTree != null) _DragDropCTree.Bounds = new Rectangle(xm + 2 * ws, ym, w, h);
            if (_DragDropLogText != null) _DragDropLogText.Bounds = new Rectangle(xm + 3 * ws, ym, w, h);
        }
        private DxListBoxPanel _DragDropAList;
        private DxListBoxPanel _DragDropBList;
        private DxTreeList _DragDropCTree;
        private DxMemoEdit _DragDropLogText;
        #endregion
        #region SyntaxEditor
        protected void InitSyntaxEditor()
        {
            AddNewPage("Syntax Editor", PrepareSyntaxEditor);
        }
        private DxPanelControl _PanelSyntaxEditor;
        private void PrepareSyntaxEditor(DxPanelControl panel)
        {
            _PanelSyntaxEditor = panel;
            _PanelSyntaxEditor.ClientSizeChanged += _PanelSyntaxEditor_AnySizeChanged;

            bool withRibbon = false;

            _SyntaxRtfEdit = new DxSyntaxEditControl();
            _PanelSyntaxEditor.Controls.Add(_SyntaxRtfEdit);

            if (withRibbon)
            {
                _SyntaxRtfRibbon = _SyntaxRtfEdit.CreateRibbon(DevExpress.XtraRichEdit.RichEditToolbarType.All);
                _SyntaxRtfRibbon.CommandLayout = XR.CommandLayout.Simplified;
                _SyntaxRtfRibbon.SizeChanged += _PanelSyntaxEditor_AnySizeChanged;
                _PanelSyntaxEditor.Controls.Add(_SyntaxRtfRibbon);
            }

            _SyntaxRtfEdit.SyntaxRules = Noris.WS.Parser.NetParser.DefaultSettings.MsSqlColor;

            _PanelSyntaxEditorDoLayout();
        }
        private void _PanelSyntaxEditor_AnySizeChanged(object sender, EventArgs e)
        {
            _PanelSyntaxEditorDoLayout();
        }
        private void _PanelSyntaxEditorDoLayout()
        {
            if (_SyntaxRtfEdit == null) return;

            var clientSize = _PanelSyntaxEditor.ClientSize;

            int y = (_SyntaxRtfRibbon != null ? _SyntaxRtfRibbon.Bottom : 0) + 6;
            _SyntaxRtfEdit.Bounds = new Rectangle(6, y, clientSize.Width - 12, clientSize.Height - 6 - y);

            /*
            int contentY = 36;

            int minEditWidth = 300;
            int minImgWidth = 16;
            int maxImgWidth = 388;
            int minImgHeight = 16;
            int maxImgHeight = 320;

            int imageX = clientSize.Width - 6 - maxImgWidth;
            int image1Y = contentY;
            int imageRight = clientSize.Width - 6;
            int imageBottom = clientSize.Height - 6;
            int imageWidth = imageRight - imageX;
            int imageHeight = (imageBottom - image1Y - 6) / 2;
            if (imageWidth < minImgWidth)
            {
                imageWidth = minImgWidth;
                imageX = imageRight - imageWidth;
            }
            if (imageWidth > maxImgWidth)
            {
                imageWidth = maxImgWidth;
                imageX = imageRight - imageWidth;
            }
            if (imageHeight < minImgHeight)
            {
                imageHeight = minImgHeight;
                image1Y = imageBottom - imageHeight;
            }
            if (imageHeight > maxImgHeight)
            {
                imageHeight = maxImgHeight;
                imageBottom = image1Y + imageHeight;
            }
            int image2Y = image1Y + imageHeight + 6;

            int editX = 6;
            int editY = contentY;
            int editRight = imageX - 6;
            int editWidth = editRight - editX;
            if (editWidth < minEditWidth)
            {
                editWidth = minEditWidth;
                editRight = editX + editWidth;
                imageX = editRight + 6;
                imageWidth = imageRight - imageX;
                if (imageWidth < minImgWidth)
                {
                    imageWidth = minImgWidth;
                    imageRight = imageX + imageWidth;
                }
            }
            int editBottom = clientSize.Height - 6;
            int editHeight = editBottom - editY;

            int buttonRight = imageRight;
            int buttonWidth = imageWidth;
            if (buttonWidth < 100) buttonWidth = 100;
            int buttonX = buttonRight - buttonWidth;
            int buttonBottom = image1Y - 6;
            int buttonHeight = 26;
            int buttonY = buttonBottom - buttonHeight;

            _SvgIconXmlText.Bounds = new Rectangle(editX, editY, editWidth, editHeight);
            _SvgIconImage1.Bounds = new Rectangle(imageX, image1Y, imageWidth, imageHeight);
            _SvgIconImage2.Bounds = new Rectangle(imageX, image2Y, imageWidth, imageHeight);
            _SvgIconReloadButton.Bounds = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);
            */
        }
        private DxCheckEdit _SyntaxCheckCSharp;
        private DxCheckEdit _SyntaxCheckSQL;
        private DxCheckEdit _SyntaxCheckXML;
        private DxSyntaxEditControl _SyntaxRtfEdit;
        private XR.RibbonControl _SyntaxRtfRibbon;

        #endregion
        #region RibbonSample
        private DataRibbonPage CreateRibbonSamplePage()
        {
            DataRibbonPage page = new DataRibbonPage() { PageText = "RŮZNÉ" };
            DataRibbonGroup group = new DataRibbonGroup() { GroupText = "Jednotlivé prvky", GroupButtonVisible = true };
            page.Groups.Add(group);
            DataRibbonItem item;

            string[] resources = new string[]
{
    "devav/actions/filter.svg",
    "devav/actions/new.svg",
    "devav/actions/printincludeevaluations.svg",
    "devav/actions/printpreview.svg",
    "devav/print/summary.svg",
    "svgimages/dashboards/chartstackedline.svg"
};

            item = new DataRibbonItem() { ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large, Text = "Filtr", ImageName = "devav/actions/filter.svg" };
            group.Items.Add(item);


            DataRibbonComboItem comboItem;
            comboItem = new DataRibbonComboItem() { ItemType = RibbonItemType.ComboListBox };
            comboItem.RibbonStyle = RibbonItemStyles.SmallWithText;
            comboItem.Text = "";
            comboItem.ImageFromCaptionMode = ImageFromCaptionType.Disabled;
            comboItem.ImageName = "devav/print/summary.svg";
            comboItem.ToolTipTitle = "ComboBox";
            comboItem.ToolTipText = "Měl by nabízet sadu prvků";
            comboItem.SubButtons = "DropDown;Ellipsis;Delete";
            comboItem.ComboBorderStyle = DxBorderStyle.None;
            comboItem.SubButtonsBorderStyle = DxBorderStyle.HotFlat;
            comboItem.Width = 280;

            comboItem.SubItems = new ListExt<IRibbonItem>();
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "První šablona" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Druhá šablona" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Třetí šablona" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Poslední šablona", ItemIsFirstInGroup = true });
            comboItem.SubItems[0].Checked = true;
            group.Items.Add(comboItem);



            comboItem = new DataRibbonComboItem() { ItemType = RibbonItemType.ComboListBox };
            comboItem.RibbonStyle = RibbonItemStyles.SmallWithText;
            comboItem.Text = "";
            comboItem.ImageFromCaptionMode = ImageFromCaptionType.Disabled;
            comboItem.ImageName = "devav/actions/filter.svg";
            comboItem.ToolTipTitle = "ComboBox";
            comboItem.ToolTipText = "Měl by nabízet sadu prvků";
            comboItem.SubButtons = "DropDown;Ellipsis;OK";
            comboItem.ComboBorderStyle = DxBorderStyle.HotFlat;
            comboItem.SubButtonsBorderStyle = DxBorderStyle.HotFlat;
            comboItem.Width = 280;

            comboItem.SubItems = new ListExt<IRibbonItem>();
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "První filtr" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Druhý filtr" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Třetí filtr" });
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Poslední filtr", ItemIsFirstInGroup = true });
            comboItem.SubItems[1].Checked = true;
            group.Items.Add(comboItem);



            comboItem = new DataRibbonComboItem() { ItemType = RibbonItemType.ComboListBox };
            comboItem.RibbonStyle = RibbonItemStyles.SmallWithText;
            comboItem.Text = "";
            comboItem.ImageFromCaptionMode = ImageFromCaptionType.Disabled;
            comboItem.ImageName = "svgimages/dashboards/chartstackedline.svg";
            comboItem.ToolTipTitle = "ComboBox";
            comboItem.ToolTipText = "Měl by nabízet sadu prvků";
            comboItem.SubButtons = "DropDown;Ellipsis;Plus";
            comboItem.ComboBorderStyle = DxBorderStyle.Style3D;
            comboItem.SubButtonsBorderStyle = DxBorderStyle.Style3D;
            comboItem.Width = 280;

            comboItem.SubItems = new ListExt<IRibbonItem>();
            comboItem.SubItems.Add(new DataRibbonItem() { Text = "Načte se po otevření..." });
            comboItem.SubItems[0].Checked = true;
            comboItem.SubItemsContentMode = RibbonContentMode.OnDemandLoadOnce;
            group.Items.Add(comboItem);


            item = new DataRibbonItem() { ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large, Text = "Clear Log", ImageName = "svgimages/spreadsheet/deletecomment.svg" };
            item.ClickAction = _ClearLog;
            group.Items.Add(item);



            item = new DataRibbonItem() { ItemType = RibbonItemType.Button, RibbonStyle = RibbonItemStyles.Large, Text = "Šablona", ImageName = "devav/actions/printpreview.svg" };
            group.Items.Add(item);


            return page;
        }
        private void _ClearLog(IMenuItem item)
        {
            DxComponent.LogClear();
            DxComponent.LogActive = true;
        }
        #endregion
        #region Random
        /// <summary>
        /// Vrací true v daném procentu volání (např. percent = 10: vrátí true 10 x za 100 volání)
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        private bool GetRandomTrue(int percent)
        {
            return (GetRandomInt(0, 100) < percent);
        }
        private int GetRandomInt(int min, int max)
        {
            return Randomizer.Rand.Next(min, max);
        }
        private Size GetRandomSize()
        {
            int w = 25 * GetRandomInt(14, 35);
            int h = 25 * GetRandomInt(6, 21);
            return new Size(w, h);
        }
        private Point GetRandomPoint()
        {
            int x = 25 * GetRandomInt(2, 16);
            int y = 25 * GetRandomInt(2, 14);
            return new Point(x, y);
        }
        private Rectangle GetRandomRectangle()
        {
            return new Rectangle(GetRandomPoint(), GetRandomSize());
        }
        private string GetRandomBallImageName()
        {
            string imageNumb = GetRandomInt(1, 24).ToString("00");
            return $"Ball{imageNumb}_16";
        }
        /// <summary>
        /// Vrátí náhodné jméno SVG obrázku 
        /// </summary>
        /// <returns></returns>
        private string GetRandomSvgImageName()
        {
            return Randomizer.GetItem(_SysSvgImages);
        }
        private string GetRandomSysSvgName(bool fileTypes = true, bool chartTypes = true)
        {
            List<string> names = new List<string>();
            if (fileTypes)
            {
                names.AddRange(new string[]
                {
                    "images/xaf/templatesv2images/action_export_tocsv.svg",
                    "images/xaf/templatesv2images/action_export_todocx.svg",
                    "images/xaf/templatesv2images/action_export_toexcel.svg",
                    "images/xaf/templatesv2images/action_export_tohtml.svg",
                    "images/xaf/templatesv2images/action_export_toimage.svg",
                    "images/xaf/templatesv2images/action_export_tomht.svg",
                    "images/xaf/templatesv2images/action_export_topdf.svg",
                    "images/xaf/templatesv2images/action_export_tortf.svg",
                    "images/xaf/templatesv2images/action_export_totext.svg",
                    "images/xaf/templatesv2images/action_export_toxls.svg",
                    "images/xaf/templatesv2images/action_export_toxlsx.svg",
                    "images/xaf/templatesv2images/action_export_toxml.svg"
                });
            }

            if (chartTypes)
            {
                names.AddRange(new string[]
                {
                    "svgimages/chart/chart.svg",
                    "svgimages/chart/charttype_area.svg",
                    "svgimages/chart/charttype_area3d.svg",
                    "svgimages/chart/charttype_area3dstacked.svg",
                    "svgimages/chart/charttype_area3dstacked100.svg",
                    "svgimages/chart/charttype_areastacked.svg",
                    "svgimages/chart/charttype_areastacked100.svg",
                    "svgimages/chart/charttype_areastepstacked.svg",
                    "svgimages/chart/charttype_areastepstacked100.svg",
                    "svgimages/chart/charttype_bar.svg",
                    "svgimages/chart/charttype_bar3d.svg",
                    "svgimages/chart/charttype_bar3dstacked.svg",
                    "svgimages/chart/charttype_bar3dstacked100.svg",
                    "svgimages/chart/charttype_barstacked.svg",
                    "svgimages/chart/charttype_barstacked100.svg",
                    "svgimages/chart/charttype_bubble.svg",
                    "svgimages/chart/charttype_bubble3d.svg",
                    "svgimages/chart/charttype_candlestick.svg",
                    "svgimages/chart/charttype_doughnut.svg",
                    "svgimages/chart/charttype_doughnut3d.svg",
                    "svgimages/chart/charttype_funnel.svg",
                    "svgimages/chart/charttype_funnel3d.svg",
                    "svgimages/chart/charttype_gantt.svg",
                    "svgimages/chart/charttype_line.svg",
                    "svgimages/chart/charttype_line3d.svg",
                    "svgimages/chart/charttype_line3dstacked.svg",
                    "svgimages/chart/charttype_line3dstacked100.svg",
                    "svgimages/chart/charttype_linestacked.svg",
                    "svgimages/chart/charttype_linestacked100.svg",
                    "svgimages/chart/charttype_manhattanbar.svg",
                    "svgimages/chart/charttype_nesteddoughnut.svg",
                    "svgimages/chart/charttype_pie.svg",
                    "svgimages/chart/charttype_pie3d.svg",
                    "svgimages/chart/charttype_point.svg",
                    "svgimages/chart/charttype_point3d.svg",
                    "svgimages/chart/charttype_polararea.svg",
                    "svgimages/chart/charttype_polarline.svg",
                    "svgimages/chart/charttype_polarpoint.svg",
                    "svgimages/chart/charttype_polarrangearea.svg",
                    "svgimages/chart/charttype_radararea.svg",
                    "svgimages/chart/charttype_radarline.svg",
                    "svgimages/chart/charttype_radarpoint.svg",
                    "svgimages/chart/charttype_radarrangearea.svg",
                    "svgimages/chart/charttype_rangearea.svg",
                    "svgimages/chart/charttype_rangearea3d.svg",
                    "svgimages/chart/charttype_rangebar.svg",
                    "svgimages/chart/charttype_scatterline.svg",
                    "svgimages/chart/charttype_scatterpolarline.svg",
                    "svgimages/chart/charttype_scatterradarline.svg",
                    "svgimages/chart/charttype_sidebysidebar3dstacked.svg",
                    "svgimages/chart/charttype_sidebysidebar3dstacked100.svg",
                    "svgimages/chart/charttype_sidebysidebarstacked.svg",
                    "svgimages/chart/charttype_sidebysidebarstacked100.svg",
                    "svgimages/chart/charttype_sidebysidegantt.svg",
                    "svgimages/chart/charttype_sidebysiderangebar.svg",
                    "svgimages/chart/charttype_spline.svg",
                    "svgimages/chart/charttype_spline3d.svg",
                    "svgimages/chart/charttype_splinearea.svg",
                    "svgimages/chart/charttype_splinearea3d.svg",
                    "svgimages/chart/charttype_splinearea3dstacked.svg",
                    "svgimages/chart/charttype_splinearea3dstacked100.svg",
                    "svgimages/chart/charttype_splineareastacked.svg",
                    "svgimages/chart/charttype_splineareastacked100.svg",
                    "svgimages/chart/charttype_steparea.svg",
                    "svgimages/chart/charttype_steparea3d.svg",
                    "svgimages/chart/charttype_stepline.svg",
                    "svgimages/chart/charttype_stepline3d.svg",
                    "svgimages/chart/charttype_stock.svg",
                    "svgimages/chart/charttype_swiftplot.svg"
                });
            }

            return (names.Count > 0 ? Randomizer.GetItem<string>(names) : null);
        }
        private string GetRandomApplicationSvgName()
        {
            if (_ApplicationSvgNames == null)
            {
                var names = DxComponent.GetResourceNames(".svg", true, false);
                _ApplicationSvgNames = Randomizer.GetItems(200, names);
            }
            return Randomizer.GetItem(_ApplicationSvgNames);
        }
        string[] _ApplicationSvgNames = null;
        #endregion
    }
}
