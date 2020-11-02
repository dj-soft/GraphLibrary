using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using RES = Noris.LCS.Base.WorkScheduler.Resources;
using ICS = Asol.Tools.WorkScheduler.Resources;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Standardized icons for use in GraphLibrary
    /// </summary>
    public class StandardIcons
    {
        /// <summary>
        /// Standardní ikona : Export - 64
        /// </summary>
        public static Image DocumentExport { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentExport2Png); } }
        /// <summary>
        /// Standardní ikona : Save - 64
        /// </summary>
        public static Image DocumentSave { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentSave5Png); } }
        /// <summary>
        /// Standardní ikona : Save As - 64
        /// </summary>
        public static Image DocumentSaveAs { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.DocumentSaveAs5Png); } }

        /// <summary>
        /// Standardní ikona : EditCopy
        /// </summary>
        public static Image EditCopy { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.EditCopy6Png); } }
        /// <summary>
        /// Standardní ikona : EditCut
        /// </summary>
        public static Image EditCut { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.EditCut6Png); } }
        /// <summary>
        /// Standardní ikona : EditPaste
        /// </summary>
        public static Image EditPaste { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.EditPaste6Png); } }
        /// <summary>
        /// Standardní ikona : EditUndo
        /// </summary>
        public static Image EditUndo { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.EditUndo5Png); } }
        /// <summary>
        /// Standardní ikona : EditRedo
        /// </summary>
        public static Image EditRedo { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.EditRedo5Png); } }

        /// <summary>
        /// Standardní ikona : GoTop
        /// </summary>
        public static Image GoTop { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoTop3Png); } }
        /// <summary>
        /// Standardní ikona : GoUp
        /// </summary>
        public static Image GoUp { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoUp4Png); } }
        /// <summary>
        /// Standardní ikona : GoDown
        /// </summary>
        public static Image GoDown { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoDown4Png); } }
        /// <summary>
        /// Standardní ikona : GoBottom
        /// </summary>
        public static Image GoBottom { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoBottom3Png); } }

        /// <summary>
        /// Standardní ikona : GoHome
        /// </summary>
        public static Image GoHome { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoFirst2Png); } }
        /// <summary>
        /// Standardní ikona : GoLeft
        /// </summary>
        public static Image GoLeft { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoPrevious4Png); } }
        /// <summary>
        /// Standardní ikona : GoRight
        /// </summary>
        public static Image GoRight { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoNext4Png); } }
        /// <summary>
        /// Standardní ikona : GoEnd
        /// </summary>
        public static Image GoEnd { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.GoLast2Png); } }

        /// <summary>
        /// Standardní ikona : RelationRecord
        /// </summary>
        public static Image RelationRecord { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.ArrowRightBluePng); } }
        /// <summary>
        /// Standardní ikona : RelationDocument
        /// </summary>
        public static Image RelationDocument { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.ArrowRightYellowPng); } }
        /// <summary>
        /// Standardní ikona : OpenFolder
        /// </summary>
        public static Image OpenFolder { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.FolderOrangePng); } }
        /// <summary>
        /// Standardní ikona : Kalkulačka
        /// </summary>
        public static Image Calculator { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.AccessoriesCalculator3Png); } }
        /// <summary>
        /// Standardní ikona : Kalendář
        /// </summary>
        public static Image Calendar { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.AccessoriesDatePng); } }
        /// <summary>
        /// Standardní ikona : DropDown
        /// </summary>
        public static Image DropDown { get { return App.ResourcesExe.GetImage(ICS.Images.AsolGraphlibIcs_Pic24.ComboDownPng); } }

        /// <summary>
        /// Standardní ikona : Refresh
        /// </summary>
        public static Image Refresh { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.ViewRefresh3Png); } }

        /// <summary>
        /// Standardní ikona : FlipHorizontal
        /// </summary>
        public static Image ObjectFlipHorizontal32 { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.ObjectFlipHorizontalPng); } }
        /// <summary>
        /// Standardní ikona : FlipVertical
        /// </summary>
        public static Image ObjectFlipVertical32 { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.ObjectFlipVerticalPng); } }
        /// <summary>
        /// Standardní ikona : Kalendář
        /// </summary>
        public static Image ViewPimCalendar32 { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.ViewPimCalendarPng); } }
        /// <summary>
        /// Standardní ikona : Zoom
        /// </summary>
        public static Image ZoomFitBest32 { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.ZoomFitBest3Png); } }

        /// <summary>
        /// Standardní ikona : Třídit ASC
        /// </summary>
        public static Image SortAsc { get { return App.ResourcesApp.GetImage(RES.Images.Small16.GoUp2Png); } }
        /// <summary>
        /// Standardní ikona : Třídit DESC
        /// </summary>
        public static Image SortDesc { get { return App.ResourcesApp.GetImage(RES.Images.Small16.GoDown2Png); } }
        /// <summary>
        /// Standardní ikona : Vybraný řádek
        /// </summary>
        public static Image RowSelected { get { return App.ResourcesApp.GetImage(RES.Images.Small16.DialogAccept2Png); } }

        /// <summary>
        /// Standardní ikona : Info
        /// </summary>
        public static Image IconInfo { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.HelpContentsPng); } }
        /// <summary>
        /// Standardní ikona : Help
        /// </summary>
        public static Image IconHelp { get { return App.ResourcesApp.GetImage(RES.Images.Actions24.HelpPng); } }
    }
}
