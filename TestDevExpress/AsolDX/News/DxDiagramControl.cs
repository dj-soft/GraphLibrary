// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.Utils.Extensions;
using DevExpress.XtraEditors.ViewInfo;

using XS = Noris.WS.Parser.XmlSerializer;
using System.ComponentModel;

namespace Noris.Clients.Win.Components.AsolDX
{
    public class DxDiagramControl : DevExpress.XtraDiagram.DiagramControl
    {
    }

    public class DxDiagramLine
    {
        public int BeginItemId { get; set; }
        public int EndItemId { get; set; }
        public string LineType { get; set; }
        public Color? LineColor { get; set; }

    }
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
