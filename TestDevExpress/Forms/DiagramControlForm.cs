using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Utils.Extensions;
using DevExpress.Utils.Menu;
using DevExpress.XtraPivotGrid.Data;
using DevExpress.XtraTreeList;
using Noris.Clients.Win.Components.AsolDX;

namespace TestDevExpress.Forms
{
    /// <summary>
    /// Okno pro testování DiagramControlu
    /// </summary>
    public class DiagramControlForm : DxRibbonForm
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DiagramControlForm() : base()
        {
            this.InitDiagramControl();
            this.InitData();
        }
        /// <summary>
        /// Inicializace Ribbonu a StatusBaru. Volá se v konstruktoru třídy <see cref="DxRibbonForm"/>!
        /// </summary>
        protected override void InitDxRibbonForm()
        {
            this.Text = "Ukázka možností controlu 'DevExpress.XtraDiagram.DiagramControl'...";
            base.InitDxRibbonForm();
        }
        /// <summary>
        /// Provede přípravu obsahu Ribbonu.
        /// Pozor: Bázová třída <see cref="DxRibbonForm"/> pouze nastaví <see cref="DxRibbonForm.DxRibbon"/>.Visible = false; nic jiného neprovádí !!!
        /// To proto, když by potomek nijak s Ribbonem nepracoval, pak nebude Ribbon zobrazen.
        /// U této metody tedy není vhodné volat base metodu, anebo je třeba po jejím volání nastavit viditelnost Ribbonu na true.
        /// </summary>
        protected override void DxRibbonPrepare()
        {
            // base.DxRibbonPrepare();

            List<DataRibbonPage> pages = new List<DataRibbonPage>();
            DataRibbonPage page;
            DataRibbonGroup group;

            page = new DataRibbonPage() { PageId = "DX", PageText = "ZÁKLADNÍ" };
            pages.Add(page);
            group = DxRibbonControl.CreateSkinIGroup("DESIGN", addUhdSupport: true) as DataRibbonGroup;
            group.Items.Add(ImagePickerForm.CreateRibbonButton());
            page.Groups.Add(group);

            this.DxRibbon.Clear();
            this.DxRibbon.AddPages(pages);

            _RibbonPages = pages;
        }
        protected override void DxStatusPrepare()
        {
            this.DxStatusBar.ItemLinks.Add(new DxBarStaticItem() { Caption = "DiagramControl", Border = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat });
        }
        List<DataRibbonPage> _RibbonPages;
        private void InitDiagramControl()
        {
            _DiagramControl = new DevExpress.XtraDiagram.DiagramControl();
            _DiagramControl.Dock = System.Windows.Forms.DockStyle.Fill;


            _DiagramControl.OptionsView.PropertiesPanelVisibility = DevExpress.Diagram.Core.PropertiesPanelVisibility.Visible;
            _DiagramControl.OptionsView.ShowPanAndZoomPanel = true;
            _DiagramControl.OptionsView.ToolboxVisibility = DevExpress.Diagram.Core.ToolboxVisibility.Compact;
            _DiagramControl.OptionsView.ShowRulers = true;
            _DiagramControl.OptionsView.ShowMeasureUnit = true;

            bool readOnly = false;
            if (readOnly)
            {
                _DiagramControl.OptionsProtection.AllowEditItems = false;
                _DiagramControl.OptionsProtection.AllowMoveItems = false;
                _DiagramControl.OptionsProtection.AllowResizeItems = false;
                _DiagramControl.OptionsProtection.AllowRotateItems = false;

                _DiagramControl.OptionsProtection.AllowAddRemoveItems = false;
                _DiagramControl.OptionsProtection.AllowApplyAutomaticLayout = false;

                _DiagramControl.OptionsProtection.IsReadOnly = true;
            }

            _DiagramControl.Toolbox = new DevExpress.XtraToolbox.ToolboxControl();
            _DiagramControl.OptionsView.ToolboxVisibility = DevExpress.Diagram.Core.ToolboxVisibility.Full;


            DxMainPanel.Controls.Add(_DiagramControl);
            _DiagramControl.InitializeRibbon(this.DxRibbon);
            _DiagramControl.CustomSaveDocument += _DiagramControl_CustomSaveDocument;
            _DiagramControl.AttachToForm();
        }

        private void _DiagramControl_CustomSaveDocument(object sender, DevExpress.XtraDiagram.DiagramCustomSaveDocumentEventArgs e)
        {
            string xml = null;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                _DiagramControl.SaveDocument(ms);
                var content = ms.ToArray();
                xml = System.Text.UTF8Encoding.UTF8.GetString(content);
                string code = "\"" + xml.Replace("\"", "\\\"").Replace("\r\n", "\\r\\n\" + \r\n  \"") + "\"";
            }

            e.Handled = true;
        }

        private DevExpress.XtraDiagram.DiagramControl _DiagramControl;
        private void InitData()
        {
            int sampleId = 0;
            _Content = _GetData(_GetSample(sampleId));
            if (_Content != null)
            {
                _DiagramControl.DocumentSource = _Content;
            }
            else
            {
                List<DevExpress.XtraDiagram.DiagramItem> diagramItems = new List<DevExpress.XtraDiagram.DiagramItem>();
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_RoundedRectangle, 50f, 50f, "Fraktura FD24-01", backColor: Color.FromArgb(255, 220, 160), isTitle: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_Rectangle, 250f, 50f, "Fraktura FD24-02", backColor: Color.FromArgb(220, 220, 255), isComment: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Decision, 450f, 50f, "Fraktura FD24-03"));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Document, 50f, 125f, "Fraktura FD2-01", backColor: Color.FromArgb(220, 255, 160), isTitle: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_RightTriangle, 250f, 125f, "Fraktura FD2-02", isComment: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_Trapezoid, 450f, 125f, "Fraktura FD2-03", isComment: true));

                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Curved, diagramItems[0], diagramItems[1]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Curved, diagramItems[1], diagramItems[2]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.RightAngle, diagramItems[3], diagramItems[4]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.RightAngle, diagramItems[4], diagramItems[5]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Straight, diagramItems[1], diagramItems[5]));

                _DiagramControl.Items.AddRange(diagramItems.ToArray());

                string xml = null;
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    _DiagramControl.SaveDocument(ms);
                    var data = ms.ToArray();
                    xml = System.Text.UTF8Encoding.UTF8.GetString(data);
                }
            }

            // _DiagramControl.ShowEditor();
        }
        private byte[] _Content;
        private byte[] _GetData(string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            return System.Text.UTF8Encoding.UTF8.GetBytes(text);
        }
        private string _GetSample(int sampleId)
        {
            switch (sampleId)
            {
                case 1:
                    return "<XtraSerializer version=\"21.1.5.0\">" +
                        "  <Items>" +
                        "    <Item1 ItemKind=\"DiagramRoot\">" +
                        "      <Children>" +
                        "        <Item1 Position=\"50,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFFFDCA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-01\" ItemKind=\"DiagramShape\" />" +
                        "        <Item2 Position=\"250,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" Background=\"#FFDCDCFF\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-02\" ItemKind=\"DiagramShape\" />" +
                        "        <Item3 Position=\"450,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-03\" ItemKind=\"DiagramShape\" />" +
                        "        <Item4 Position=\"60,210\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFDCFFA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-01\" ItemKind=\"DiagramShape\" />" +
                        "        <Item5 Position=\"250,125\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-02\" ItemKind=\"DiagramShape\" />" +
                        "        <Item6 Position=\"450,125\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-03\" ItemKind=\"DiagramShape\" />" +
                        "      </Children>" +
                        "    </Item1>" +
                        "  </Items>" +
                        "</XtraSerializer>";

                case 2:
                    return "﻿<XtraSerializer version=\"21.1.5.0\">" +
                        "  <Items>" +
                        "    <Item1 ItemKind=\"DiagramRoot\">" +
                        "      <Children>" +
                        "        <Item1 Position=\"50,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFFFDCA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-01\" ItemKind=\"DiagramShape\" />" +
                        "        <Item2 Position=\"250,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" Background=\"#FFDCDCFF\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-02\" ItemKind=\"DiagramShape\" />" +
                        "        <Item3 Position=\"450,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-03\" ItemKind=\"DiagramShape\" />" +
                        "        <Item4 Position=\"60,210\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFDCFFA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-01\" ItemKind=\"DiagramShape\" />" +
                        "        <Item5 Position=\"250,210\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-02\" ItemKind=\"DiagramShape\" />" +
                        "        <Item6 Position=\"450,125\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-03\" ItemKind=\"DiagramShape\" />" +
                        "        <Item7 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"0\" EndItem=\"1\" BeginPoint=\"200,75\" EndPoint=\"250,75\" />" +
                        "        <Item8 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"3\" EndItem=\"4\" BeginPoint=\"210,235\" EndPoint=\"250,235\" />" +
                        "        <Item9 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"440,235 440,150\" ItemKind=\"DiagramConnector\" BeginItem=\"4\" EndItem=\"5\" BeginPoint=\"400,235\" EndPoint=\"450,150\" />" +
                        "        <Item10 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"1\" EndItem=\"2\" BeginPoint=\"400,75\" EndPoint=\"450,75\" />" +
                        "      </Children>" +
                        "    </Item1>" +
                        "  </Items>" +
                        "</XtraSerializer>";
                case 3:
                    return "﻿<XtraSerializer version=\"21.1.5.0\">\r\n" +
  "  <Items>\r\n" +
  "    <Item1 ItemKind=\"DiagramRoot\" Theme=\"Linear\">\r\n" +
  "      <Children>\r\n" +
  "        <Item1 Position=\"80,80\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFFFDCA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-01\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item2 Position=\"310,80\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" Background=\"#FFDCDCFF\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-02\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item3 Position=\"560,80\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD24-03\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item4 Position=\"70,270\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFDCFFA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-01\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item5 Position=\"310,270\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-02\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item6 Position=\"500,370\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" Angle=\"-90\" MoveWithSubordinates=\"true\" Content=\"Fraktura FD2-03\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item7 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"0\" EndItem=\"1\" BeginPoint=\"200,75\" EndPoint=\"250,75\" />\r\n" +
  "        <Item8 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"3\" EndItem=\"4\" BeginPoint=\"210,235\" EndPoint=\"250,235\" />\r\n" +
  "        <Item9 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"575,295\" ItemKind=\"DiagramConnector\" BeginItem=\"4\" EndItem=\"5\" BeginPoint=\"400,235\" EndPoint=\"450,150\" />\r\n" +
  "        <Item10 BeginItemPointIndex=\"1\" EndItemPointIndex=\"3\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"1\" EndItem=\"2\" BeginPoint=\"400,75\" EndPoint=\"450,75\" />\r\n" +
  "        <Item11 BeginItemPointIndex=\"2\" EndItemPointIndex=\"0\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"1\" EndItem=\"4\" BeginPoint=\"325,100\" EndPoint=\"325,210\" />\r\n" +
  "        <Item12 Position=\"670,360\" Size=\"230,70\" ThemeStyleId=\"Variant3\" Content=\"Dodací list ABCD\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item13 BeginItemPointIndex=\"0\" Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"5\" EndItem=\"11\" BeginPoint=\"600,395\" EndPoint=\"670,405\" />\r\n" +
  "      </Children>\r\n" +
  "    </Item1>\r\n" +
  "  </Items>\r\n" +
  "</XtraSerializer>";
                case 4:
                    return "﻿<XtraSerializer version=\"21.1.5.0\">\r\n" +
  "  <Items>\r\n" +
  "    <Item1 ItemKind=\"DiagramRoot\">\r\n" +
  "      <Children>\r\n" +
  "        <Item1 Position=\"50,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFFFDCA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.Document\" Content=\"Fraktura FD24-01\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item2 Position=\"250,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" Background=\"#FFDCDCFF\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.Document\" Content=\"Fraktura FD24-02\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item3 Position=\"450,50\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.OffPageReference\" Content=\"Fraktura FD24-03\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item4 Position=\"50,160\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Bold\" FontStyle=\"Normal\" FontSize=\"10.25\" TextDecorations=\"None\" Background=\"#FFDCFFA0\" Foreground=\"#FF000000\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.StartEnd\" Content=\"Fraktura FD2-01\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item5 Position=\"250,160\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.Subprocess\" Content=\"Fraktura FD2-02\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item6 Position=\"480,260\" Size=\"150,50\" MinSize=\"100,20\" Anchors=\"Left\" FontFamily=\"Tahoma\" FontWeight=\"Normal\" FontStyle=\"Italic\" FontSize=\"8.25\" TextDecorations=\"None\" MoveWithSubordinates=\"true\" Shape=\"BasicFlowchartShapes.Decision\" Content=\"Fraktura FD2-03\" ItemKind=\"DiagramShape\" />\r\n" +
  "        <Item7 Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"0\" EndItem=\"1\" BeginPoint=\"0,0\" EndPoint=\"0,0\" />\r\n" +
  "        <Item8 Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"1\" EndItem=\"2\" BeginPoint=\"0,0\" EndPoint=\"0,0\" />\r\n" +
  "        <Item9 Points=\"(Empty)\" ItemKind=\"DiagramConnector\" BeginItem=\"3\" EndItem=\"4\" BeginPoint=\"0,0\" EndPoint=\"0,0\" />\r\n" +
  "        <Item10 Points=\"463,185 463,285\" ItemKind=\"DiagramConnector\" BeginItem=\"4\" EndItem=\"5\" BeginPoint=\"0,0\" EndPoint=\"0,0\" />\r\n" +
  "        <Item11 Points=\"433,75 433,285\" ItemKind=\"DiagramConnector\" BeginItem=\"1\" EndItem=\"5\" BeginPoint=\"0,0\" EndPoint=\"0,0\" />\r\n" +
  "      </Children>\r\n" +
  "    </Item1>\r\n" +
  "  </Items>\r\n" +
  "</XtraSerializer>";
            }
            return null;
        }

        private DevExpress.XtraDiagram.DiagramShape _CreateShape(string shapeType, float x, float y, string text, Color? backColor = null, Color? borderColor = null, bool isTitle = false, bool isComment = false)
        {
            DevExpress.Diagram.Core.ShapeDescription shapeDescriptor = DxDiagrams.GetShapeDescriptor(shapeType);
            var shape = new DevExpress.XtraDiagram.DiagramShape(shapeDescriptor)
            {
                Bounds = new System.Drawing.RectangleF(x, y, 150f, 50f),
                Content = text,
                MinHeight = 20f, MinWidth = 100f,
                Anchors = DevExpress.Diagram.Core.Sides.Left,
                CollapseButtonVisibilityMode = DevExpress.Diagram.Core.CollapseButtonVisibilityMode.HasSubordinates,
                MoveWithSubordinates = true
            };
            if (backColor.HasValue)
            {
                shape.Appearance.BackColor = backColor.Value;
                shape.Appearance.Options.UseBackColor = true;
                shape.Appearance.ForeColor = backColor.Value.Contrast();
            }
            // shape.Appearance.BorderColor = Color.FromArgb(160, 160, 220);
            // shape.Appearance.ForeColor = Color.FromArgb(32, 0, 0);
            if (isTitle)
            {
                shape.Appearance.FontSizeDelta = 1;
                shape.Appearance.FontStyleDelta = FontStyle.Bold;
            }
            else if (isComment)
            {
                shape.Appearance.FontSizeDelta = 0;
                shape.Appearance.FontStyleDelta = FontStyle.Italic;
            }
            
            return shape;
        }
       
    }



}

namespace Noris.Clients.Win.Components.AsolDX
{
    public static class DxDiagrams
    {
        /// <summary>
        /// Vrátí descriptor tvaru daného jména.
        /// </summary>
        /// <param name="shapeType"></param>
        /// <returns></returns>
        public static DevExpress.Diagram.Core.ShapeDescription GetShapeDescriptor(string shapeType)
        {
            if (!String.IsNullOrEmpty(shapeType))
            {
                switch (shapeType)
                {
                    case DxDiagramItemType.Basic_Rectangle: return DevExpress.Diagram.Core.BasicShapes.Rectangle;
                    case DxDiagramItemType.Basic_RoundedRectangle: return DevExpress.Diagram.Core.BasicShapes.RoundedRectangle;
                    case DxDiagramItemType.Basic_RoundCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.RoundCornerRectangle;
                    case DxDiagramItemType.Basic_Ellipse: return DevExpress.Diagram.Core.BasicShapes.Ellipse;
                    case DxDiagramItemType.Basic_Triangle: return DevExpress.Diagram.Core.BasicShapes.Triangle;
                    case DxDiagramItemType.Basic_RightTriangle: return DevExpress.Diagram.Core.BasicShapes.RightTriangle;
                    case DxDiagramItemType.Basic_Frame: return DevExpress.Diagram.Core.BasicShapes.Frame;
                    case DxDiagramItemType.Basic_FrameCorner: return DevExpress.Diagram.Core.BasicShapes.FrameCorner;
                    case DxDiagramItemType.Basic_Cube: return DevExpress.Diagram.Core.BasicShapes.Cube;
                    case DxDiagramItemType.Basic_Can: return DevExpress.Diagram.Core.BasicShapes.Can;
                    case DxDiagramItemType.Basic_Star4: return DevExpress.Diagram.Core.BasicShapes.Star4;
                    case DxDiagramItemType.Basic_Star5: return DevExpress.Diagram.Core.BasicShapes.Star5;
                    case DxDiagramItemType.Basic_Star6: return DevExpress.Diagram.Core.BasicShapes.Star6;
                    case DxDiagramItemType.Basic_Star7: return DevExpress.Diagram.Core.BasicShapes.Star7;
                    case DxDiagramItemType.Basic_Star16: return DevExpress.Diagram.Core.BasicShapes.Star16;
                    case DxDiagramItemType.Basic_Star24: return DevExpress.Diagram.Core.BasicShapes.Star24;
                    case DxDiagramItemType.Basic_Star32: return DevExpress.Diagram.Core.BasicShapes.Star32;
                    case DxDiagramItemType.Basic_LeftBrace: return DevExpress.Diagram.Core.BasicShapes.LeftBrace;
                    case DxDiagramItemType.Basic_RightBrace: return DevExpress.Diagram.Core.BasicShapes.RightBrace;
                    case DxDiagramItemType.Basic_LeftParenthesis: return DevExpress.Diagram.Core.BasicShapes.LeftParenthesis;
                    case DxDiagramItemType.Basic_RightParenthesis: return DevExpress.Diagram.Core.BasicShapes.RightParenthesis;
                    case DxDiagramItemType.Basic_Pentagon: return DevExpress.Diagram.Core.BasicShapes.Pentagon;
                    case DxDiagramItemType.Basic_Hexagon: return DevExpress.Diagram.Core.BasicShapes.Hexagon;
                    case DxDiagramItemType.Basic_Heptagon: return DevExpress.Diagram.Core.BasicShapes.Heptagon;
                    case DxDiagramItemType.Basic_Octagon: return DevExpress.Diagram.Core.BasicShapes.Octagon;
                    case DxDiagramItemType.Basic_Decagon: return DevExpress.Diagram.Core.BasicShapes.Decagon;
                    case DxDiagramItemType.Basic_SingleSnipCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SingleSnipCornerRectangle;
                    case DxDiagramItemType.Basic_SnipSameSideCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SnipSameSideCornerRectangle;
                    case DxDiagramItemType.Basic_SnipDiagonalCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SnipDiagonalCornerRectangle;
                    case DxDiagramItemType.Basic_SingleRoundCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SingleRoundCornerRectangle;
                    case DxDiagramItemType.Basic_SnipAndRoundCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SnipAndRoundCornerRectangle;
                    case DxDiagramItemType.Basic_RoundSameSideCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.RoundSameSideCornerRectangle;
                    case DxDiagramItemType.Basic_RoundDiagonalCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.RoundDiagonalCornerRectangle;
                    case DxDiagramItemType.Basic_SnipAndRoundSingleCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SnipAndRoundSingleCornerRectangle;
                    case DxDiagramItemType.Basic_SnipCornerRectangle: return DevExpress.Diagram.Core.BasicShapes.SnipCornerRectangle;
                    case DxDiagramItemType.Basic_Plaque: return DevExpress.Diagram.Core.BasicShapes.Plaque;
                    case DxDiagramItemType.Basic_LShape: return DevExpress.Diagram.Core.BasicShapes.LShape;
                    case DxDiagramItemType.Basic_DiagonalStripe: return DevExpress.Diagram.Core.BasicShapes.DiagonalStripe;
                    case DxDiagramItemType.Basic_Donut: return DevExpress.Diagram.Core.BasicShapes.Donut;
                    case DxDiagramItemType.Basic_NoSymbol: return DevExpress.Diagram.Core.BasicShapes.NoSymbol;
                    case DxDiagramItemType.Basic_Parallelogram: return DevExpress.Diagram.Core.BasicShapes.Parallelogram;
                    case DxDiagramItemType.Basic_Trapezoid: return DevExpress.Diagram.Core.BasicShapes.Trapezoid;
                    case DxDiagramItemType.Basic_Diamond: return DevExpress.Diagram.Core.BasicShapes.Diamond;
                    case DxDiagramItemType.Basic_Cross: return DevExpress.Diagram.Core.BasicShapes.Cross;
                    case DxDiagramItemType.Basic_Chevron: return DevExpress.Diagram.Core.BasicShapes.Chevron;

                    case DxDiagramItemType.Flowchart_Process: return DevExpress.Diagram.Core.BasicFlowchartShapes.Process;
                    case DxDiagramItemType.Flowchart_Decision: return DevExpress.Diagram.Core.BasicFlowchartShapes.Decision;
                    case DxDiagramItemType.Flowchart_Subprocess: return DevExpress.Diagram.Core.BasicFlowchartShapes.Subprocess;
                    case DxDiagramItemType.Flowchart_StartEnd: return DevExpress.Diagram.Core.BasicFlowchartShapes.StartEnd;
                    case DxDiagramItemType.Flowchart_Document: return DevExpress.Diagram.Core.BasicFlowchartShapes.Document;
                    case DxDiagramItemType.Flowchart_Data: return DevExpress.Diagram.Core.BasicFlowchartShapes.Data;
                    case DxDiagramItemType.Flowchart_Database: return DevExpress.Diagram.Core.BasicFlowchartShapes.Database;
                    case DxDiagramItemType.Flowchart_ExternalData: return DevExpress.Diagram.Core.BasicFlowchartShapes.ExternalData;
                    case DxDiagramItemType.Flowchart_Custom1: return DevExpress.Diagram.Core.BasicFlowchartShapes.Custom1;
                    case DxDiagramItemType.Flowchart_Custom2: return DevExpress.Diagram.Core.BasicFlowchartShapes.Custom2;
                    case DxDiagramItemType.Flowchart_Custom3: return DevExpress.Diagram.Core.BasicFlowchartShapes.Custom3;
                    case DxDiagramItemType.Flowchart_Custom4: return DevExpress.Diagram.Core.BasicFlowchartShapes.Custom4;
                    case DxDiagramItemType.Flowchart_OnPageReference: return DevExpress.Diagram.Core.BasicFlowchartShapes.OnPageReference;
                    case DxDiagramItemType.Flowchart_OffPageReference: return DevExpress.Diagram.Core.BasicFlowchartShapes.OffPageReference;

                    case DxDiagramItemType.ArrowShapes_LeftRightUpArrow: return DevExpress.Diagram.Core.ArrowShapes.LeftRightUpArrow;
                    case DxDiagramItemType.ArrowShapes_QuadArrow: return DevExpress.Diagram.Core.ArrowShapes.QuadArrow;
                    case DxDiagramItemType.ArrowShapes_CircularArrow: return DevExpress.Diagram.Core.ArrowShapes.CircularArrow;
                    case DxDiagramItemType.ArrowShapes_BlockArrow: return DevExpress.Diagram.Core.ArrowShapes.BlockArrow;
                    case DxDiagramItemType.ArrowShapes_StripedArrow: return DevExpress.Diagram.Core.ArrowShapes.StripedArrow;
                    case DxDiagramItemType.ArrowShapes_NotchedArrow: return DevExpress.Diagram.Core.ArrowShapes.NotchedArrow;
                    case DxDiagramItemType.ArrowShapes_CurvedLeftArrow: return DevExpress.Diagram.Core.ArrowShapes.CurvedLeftArrow;
                    case DxDiagramItemType.ArrowShapes_LeftRightArrowBlock: return DevExpress.Diagram.Core.ArrowShapes.LeftRightArrowBlock;
                    case DxDiagramItemType.ArrowShapes_CurvedRightArrow: return DevExpress.Diagram.Core.ArrowShapes.CurvedRightArrow;
                    case DxDiagramItemType.ArrowShapes_UTurnArrow: return DevExpress.Diagram.Core.ArrowShapes.UTurnArrow;
                    case DxDiagramItemType.ArrowShapes_BentArrow: return DevExpress.Diagram.Core.ArrowShapes.BentArrow;
                    case DxDiagramItemType.ArrowShapes_FlexibleArrow: return DevExpress.Diagram.Core.ArrowShapes.FlexibleArrow;
                    case DxDiagramItemType.ArrowShapes_ModernArrow: return DevExpress.Diagram.Core.ArrowShapes.ModernArrow;
                    case DxDiagramItemType.ArrowShapes_SimpleDoubleArrow: return DevExpress.Diagram.Core.ArrowShapes.SimpleDoubleArrow;
                    case DxDiagramItemType.ArrowShapes_SimpleArrow: return DevExpress.Diagram.Core.ArrowShapes.SimpleArrow;
                    case DxDiagramItemType.ArrowShapes_SharpBentArrow: return DevExpress.Diagram.Core.ArrowShapes.SharpBentArrow;
                    case DxDiagramItemType.ArrowShapes_QuadArrowBlock: return DevExpress.Diagram.Core.ArrowShapes.QuadArrowBlock;


                }
                // a další jako: ... return DevExpress.Diagram.Core.DecorativeShapes.Cloud; ... 
            }

            // default:
            return DevExpress.Diagram.Core.BasicShapes.Rectangle;
        }

        /// <summary>
        /// Vygeneruje a vrátí spojovací linku, kterou lze vložit do prvků v diagramu
        /// </summary>
        /// <param name="lineType"></param>
        /// <param name="itemBegin"></param>
        /// <param name="itemEnd"></param>
        /// <returns></returns>
        public static DevExpress.XtraDiagram.DiagramConnector CreateConnector(string lineType, DevExpress.XtraDiagram.DiagramItem itemBegin, DevExpress.XtraDiagram.DiagramItem itemEnd)
        {
            var connectorType = GetConnectorType(lineType);
            var connector = new DevExpress.XtraDiagram.DiagramConnector(connectorType, itemBegin, itemEnd);
            connector.Appearance.BackColor = Color.Violet;
            connector.Appearance.BorderSize = 3;
            connector.Appearance.Options.UseBackColor = true;
            return connector;
        }
        /// <summary>
        /// Najde a vrátí typ konektoru pro jeho daný název typu.
        /// Pro název použijme hodnoty třídy <see cref="DxDiagramLineType"/>, lze ale zadat i jiné hodnoty, pokud jsou přítomny v poli <see cref="RegisteredConnectorTypes"/>
        /// </summary>
        /// <param name="lineType">Typ linie, použijme hodnoty třídy <see cref="DxDiagramLineType"/>, lze ale zadat i jiné hodnoty, pokud jsou přítomny v poli <see cref="RegisteredConnectorTypes"/></param>
        /// <returns></returns>
        public static DevExpress.Diagram.Core.ConnectorType GetConnectorType(string lineType)
        {
            if (!String.IsNullOrEmpty(lineType))
            {
                switch (lineType)
                {
                    case DxDiagramLineType.Curved: return DevExpress.Diagram.Core.ConnectorType.Curved;
                    case DxDiagramLineType.RightAngle: return DevExpress.Diagram.Core.ConnectorType.RightAngle;
                    case DxDiagramLineType.Straight: return DevExpress.Diagram.Core.ConnectorType.Straight;
                    case DxDiagramLineType.OrgChart: return DevExpress.Diagram.Core.ConnectorType.OrgChart;
                }
                foreach (var connectorType in DevExpress.Diagram.Core.ConnectorType.RegisteredTypes)
                {
                    if (connectorType != null && connectorType.TypeName == lineType.ToString())
                        return connectorType;
                }
            }
            return DevExpress.Diagram.Core.ConnectorType.RightAngle;
        }
        /// <summary>
        /// Dostupné typy konektorů = spojovacích linek
        /// </summary>
        public static string[] RegisteredConnectorTypes { get { return DevExpress.Diagram.Core.ConnectorType.RegisteredTypes.Select(t => t.TypeName).ToArray(); } }

    }
    /// <summary>
    /// Typy prvků v diagramech
    /// </summary>
    public static class DxDiagramItemType
    {
        public const string Basic_Rectangle = "Basic.Rectangle";
        public const string Basic_RoundedRectangle = "Basic.RoundedRectangle";
        public const string Basic_RoundCornerRectangle = "Basic.RoundCornerRectangle";
        public const string Basic_Ellipse = "Basic.Ellipse";
        public const string Basic_Triangle = "Basic.Triangle";
        public const string Basic_RightTriangle = "Basic.RightTriangle";
        public const string Basic_Frame = "Basic.Frame";
        public const string Basic_FrameCorner = "Basic.FrameCorner";
        public const string Basic_Cube = "Basic.Cube";
        public const string Basic_Can = "Basic.Can";
        public const string Basic_Star4 = "Basic.Star4";
        public const string Basic_Star5 = "Basic.Star5";
        public const string Basic_Star6 = "Basic.Star6";
        public const string Basic_Star7 = "Basic.Star7";
        public const string Basic_Star16 = "Basic.Star16";
        public const string Basic_Star24 = "Basic.Star24";
        public const string Basic_Star32 = "Basic.Star32";
        public const string Basic_LeftBrace = "Basic.LeftBrace";
        public const string Basic_RightBrace = "Basic.RightBrace";
        public const string Basic_LeftParenthesis = "Basic.LeftParenthesis";
        public const string Basic_RightParenthesis = "Basic.RightParenthesis";
        public const string Basic_Pentagon = "Basic.Pentagon";
        public const string Basic_Hexagon = "Basic.Hexagon";
        public const string Basic_Heptagon = "Basic.Heptagon";
        public const string Basic_Octagon = "Basic.Octagon";
        public const string Basic_Decagon = "Basic.Decagon";
        public const string Basic_SingleSnipCornerRectangle = "Basic.SingleSnipCornerRectangle";
        public const string Basic_SnipSameSideCornerRectangle = "Basic.SnipSameSideCornerRectangle";
        public const string Basic_SnipDiagonalCornerRectangle = "Basic.SnipDiagonalCornerRectangle";
        public const string Basic_SingleRoundCornerRectangle = "Basic.SingleRoundCornerRectangle";
        public const string Basic_SnipAndRoundCornerRectangle = "Basic.SnipAndRoundCornerRectangle";
        public const string Basic_RoundSameSideCornerRectangle = "Basic.RoundSameSideCornerRectangle";
        public const string Basic_RoundDiagonalCornerRectangle = "Basic.RoundDiagonalCornerRectangle";
        public const string Basic_SnipAndRoundSingleCornerRectangle = "Basic.SnipAndRoundSingleCornerRectangle";
        public const string Basic_SnipCornerRectangle = "Basic.SnipCornerRectangle";
        public const string Basic_Plaque = "Basic.Plaque";
        public const string Basic_LShape = "Basic.LShape";
        public const string Basic_DiagonalStripe = "Basic.DiagonalStripe";
        public const string Basic_Donut = "Basic.Donut";
        public const string Basic_NoSymbol = "Basic.NoSymbol";
        public const string Basic_Parallelogram = "Basic.Parallelogram";
        public const string Basic_Trapezoid = "Basic.Trapezoid";
        public const string Basic_Diamond = "Basic.Diamond";
        public const string Basic_Cross = "Basic.Cross";
        public const string Basic_Chevron = "Basic.Chevron";
  
        public const string Flowchart_Process = "Flowchart.Process";
        public const string Flowchart_Decision = "Flowchart.Decision";
        public const string Flowchart_Subprocess = "Flowchart.Subprocess";
        public const string Flowchart_StartEnd = "Flowchart.StartEnd";
        public const string Flowchart_Document = "Flowchart.Document";
        public const string Flowchart_Data = "Flowchart.Data";
        public const string Flowchart_Database = "Flowchart.Database";
        public const string Flowchart_ExternalData = "Flowchart.ExternalData";
        public const string Flowchart_Custom1 = "Flowchart.Custom1";
        public const string Flowchart_Custom2 = "Flowchart.Custom2";
        public const string Flowchart_Custom3 = "Flowchart.Custom3";
        public const string Flowchart_Custom4 = "Flowchart.Custom4";
        public const string Flowchart_OnPageReference = "Flowchart.OnPageReference";
        public const string Flowchart_OffPageReference = "Flowchart.OffPageReference";


        public const string ArrowShapes_LeftRightUpArrow = "ArrowShapes_LeftRightUpArrow";
        public const string ArrowShapes_QuadArrow = "ArrowShapes_QuadArrow";
        public const string ArrowShapes_CircularArrow = "ArrowShapes_CircularArrow";
        public const string ArrowShapes_BlockArrow = "ArrowShapes_BlockArrow";
        public const string ArrowShapes_StripedArrow = "ArrowShapes_StripedArrow";
        public const string ArrowShapes_NotchedArrow = "ArrowShapes_NotchedArrow";
        public const string ArrowShapes_CurvedLeftArrow = "ArrowShapes_CurvedLeftArrow";
        public const string ArrowShapes_LeftRightArrowBlock = "ArrowShapes_LeftRightArrowBlock";
        public const string ArrowShapes_CurvedRightArrow = "ArrowShapes_CurvedRightArrow";
        public const string ArrowShapes_UTurnArrow = "ArrowShapes_UTurnArrow";
        public const string ArrowShapes_BentArrow = "ArrowShapes_BentArrow";
        public const string ArrowShapes_FlexibleArrow = "ArrowShapes_FlexibleArrow";
        public const string ArrowShapes_ModernArrow = "ArrowShapes_ModernArrow";
        public const string ArrowShapes_SimpleDoubleArrow = "ArrowShapes_SimpleDoubleArrow";
        public const string ArrowShapes_SimpleArrow = "ArrowShapes_SimpleArrow";
        public const string ArrowShapes_SharpBentArrow = "ArrowShapes_SharpBentArrow";
        public const string ArrowShapes_QuadArrowBlock = "ArrowShapes_QuadArrowBlock";


    }
    public class DxDiagramLine
    { 
        public int BeginItemId { get; set; }
        public int EndItemId { get; set; }
        public string LineType { get; set; }
        public Color? LineColor { get; set; }

    }
    /// <summary>
    /// Typy spojovacích linií v diagramech
    /// </summary>
    public static class DxDiagramLineType
    {
        public const string None = "None";
        public const string Curved = "Curved";
        public const string RightAngle = "RightAngle";
        public const string Straight = "Straight";
        public const string OrgChart = "OrgChart";
    }
}