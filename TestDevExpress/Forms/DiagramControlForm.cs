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
            DataRibbonPage page = this.CreateRibbonHomePage(FormRibbonDesignGroupPart.None);
            pages.Add(page);
            this.DxRibbon.AddPages(pages, true);

            _RibbonPages = pages;
        }
        /// <summary>
        /// Připraví obsah do StatusBaru
        /// </summary>
        protected override void DxStatusPrepare()
        {
            this.DxStatusBar.ItemLinks.Add(new DxBarStaticItem() { Caption = "DiagramControl", Border = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat });
        }
        protected override void OnFirstShownAfter()
        {
            base.OnFirstShownAfter();

            _DiagramControl.Toolbox.RefreshGroups();
        }
        /// <summary>
        /// Stránky v Ribbonu
        /// </summary>
        private List<DataRibbonPage> _RibbonPages;
        /// <summary>
        /// Inicializace controlu
        /// </summary>
        private void InitDiagramControl()
        {
            _DiagramControl = new DxDiagramControl();
            _ToolboxControl = new DevExpress.XtraToolbox.ToolboxControl();
            _ToolboxControl.Dock = System.Windows.Forms.DockStyle.Left;
            _ToolboxControl.Width = 320;

            _DiagramControl.Dock = System.Windows.Forms.DockStyle.Fill;

            _DiagramControl.OptionsBehavior.ShowQuickShapes = true;
            _DiagramControl.OptionsView.AllowShapeShadows = true;
            _DiagramControl.ItemInitializing += _DiagramControl_ItemInitializing;

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

            _DiagramControl.Toolbox = _ToolboxControl;
            _DiagramControl.OptionsView.ToolboxVisibility = DevExpress.Diagram.Core.ToolboxVisibility.Full;
            _DiagramControl.Toolbox.State = DevExpress.XtraToolbox.ToolboxState.Normal;
            _DiagramControl.OptionsBehavior.SelectPartiallyCoveredItems = true;
            _DiagramControl.OptionsBehavior.ShowQuickShapes = true;
            _DiagramControl.OptionsBehavior.EnableProportionalResizing = false;
            _DiagramControl.OptionsBehavior.GlueToConnectionPointDistance = 12;
            var allStencils = new string[] { "BasicShapes", "BasicFlowchartShapes", "ArrowShapes", "DecorativeShapes", "SDLDiagramShapes" };
            _DiagramControl.OptionsBehavior.SelectedStencils = new DevExpress.Diagram.Core.StencilCollection(allStencils); // new string[] { "BasicShapes", "BasicFlowchartShapes" });
            

            DxMainPanel.Controls.Add(_ToolboxControl);
            DxMainPanel.Controls.Add(_DiagramControl);
            _DiagramControl.InitializeRibbon(this.DxRibbon);

            _DiagramControl.Toolbox.RefreshGroups();
            _DiagramControl.Toolbox.Groups.ForEach(g => g.Visible = true);
            _DiagramControl.Toolbox.InitializeMenu += Toolbox_InitializeMenu;
            _DiagramControl.Toolbox.ShowMenuButton = true;
            
            _DiagramControl.CustomSaveDocument += _DiagramControl_CustomSaveDocument;
            _DiagramControl.AttachToForm();
        }

        private void Toolbox_InitializeMenu(object sender, DevExpress.XtraToolbox.ToolboxInitializeMenuEventArgs e)
        {
            
        }

        private DxDiagramControl _DiagramControl;
        private DevExpress.XtraToolbox.ToolboxControl _ToolboxControl;

        private void _DiagramControl_ItemInitializing(object sender, DevExpress.XtraDiagram.DiagramItemInitializingEventArgs e)
        {
            
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
                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Decision, 450f, 70f, "Fraktura FD24-03"));

                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Document, 50f, 160f, "Fraktura FD2-01", backColor: Color.FromArgb(220, 255, 160), isTitle: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_Chevron, 250f, 160f, "Fraktura FD2-02", isComment: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Basic_Trapezoid, 450f, 140f, "Fraktura FD2-03", isComment: true));

                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Database, 50f, 260f, "Fraktura FDx-01", backColor: Color.FromArgb(240, 190, 160), isTitle: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Process, 250f, 260f, "Fraktura FDx-02", isComment: true));
                diagramItems.Add(_CreateShape(DxDiagramItemType.Flowchart_Data, 450f, 280f, "Fraktura FDx-03", isComment: true));

                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.RightAngle, diagramItems[0], diagramItems[1]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Curved, diagramItems[1], diagramItems[2]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.RightAngle, diagramItems[3], diagramItems[4]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Curved, diagramItems[4], diagramItems[5]));
                diagramItems.Add(DxDiagrams.CreateConnector(DxDiagramLineType.Straight, diagramItems[1], diagramItems[4]));

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
