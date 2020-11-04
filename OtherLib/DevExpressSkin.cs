using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WF = System.Windows.Forms;

using WSC = Asol.Tools.WorkScheduler.Components;

using DXL = DevExpress.LookAndFeel;
using DXE = DevExpress.XtraEditors;
using DXS = DevExpress.Skins;
using System.Drawing;

namespace Asol.Tools.WorkScheduler.DevExpressTest
{
    public class DevExpressSkinSupport
    {
        #region Inicializace
        /// <summary>
        /// Inicializace
        /// </summary>
        public static void Initialise()
        {
            DXE.WindowsFormsSettings.EnableFormSkins();
            DXE.WindowsFormsSettings.SetPerMonitorDpiAware();

            DevExpress.UserSkins.BonusSkins.Register();
            DXL.UserLookAndFeel.Default.StyleChanged += UserLookStyleChanged;
            
        }
        public static event EventHandler SkinChanged;
        private static void UserLookStyleChanged(object sender, EventArgs e)
        {
            ApplyCurrentSkin();
        }
        #endregion
        #region Skin Combo
        /// <summary>
        /// Vytvoří a vrátí Combo pro nabídku Skinů
        /// </summary>
        /// <returns></returns>
        public static WF.Control CreateSkinsCombo()
        {
            var activeSkinName = ActiveSkinName;
            var combo = new WF.ComboBox() { DropDownStyle = WF.ComboBoxStyle.DropDownList };

            List<SkinItem> skinItems = new List<SkinItem>();
            foreach (var skin in DXS.SkinManager.Default.GetRuntimeSkins())
                skinItems.Add(new SkinItem(skin));
            skinItems.Sort((a, b) => String.Compare(a.Name, b.Name));

            foreach (var skinItem in skinItems)
            {
                combo.Items.Add(skinItem);
                if (skinItem.Name == activeSkinName)
                    combo.SelectedItem = skinItem;
            }

            combo.SelectedIndexChanged += Combo_SelectedIndexChanged;

            return combo;
        }
        private static void Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var combo = sender as WF.ComboBox;
            if (combo == null) return;
            var skinItem = combo.SelectedItem as SkinItem;
            if (skinItem == null) return;
            DXL.UserLookAndFeel.Default.SetSkinStyle(skinItem.Name);
        }
        private class SkinItem
        {
            public SkinItem(DXS.SkinContainer skinContainer)
            {
                SkinContainer = skinContainer;
            }
            public DXS.SkinContainer SkinContainer { get; private set; }
            public string Name { get { return SkinContainer.SkinName; } }
            public override string ToString()
            {
                return Name;
            }
        }
        #endregion
        #region Aplikování skinu DevExpress => ASOL
        /// <summary>
        /// Aplikuje aktuální skin DevExpress do skinu ASOL
        /// </summary>
        public static void ApplyCurrentSkin()
        {
            var activeLookAndFeel = ActiveLookAndFeel;
            // var painter = activeLookAndFeel.Painter;
            var currentSkin = DXS.CommonSkins.GetSkin(activeLookAndFeel);

            DXS.SkinElement element;

            if (SearchSkinElement(currentSkin, out element, DXS.CommonSkins.SkinTextBorder))
            {
                WSC.Skin.TextBox.BorderColor = element.Border.All;
            }

            if (SearchSkinElement(currentSkin, out element, DXS.CommonSkins.SkinTextControl, DXS.CommonSkins.SkinLabel))
            {
                if (!element.Color.BackColor.IsEmpty) WSC.Skin.TextBox.BackColorEnabled = element.Color.BackColor;
                if (!element.Color.ForeColor.IsEmpty) WSC.Skin.TextBox.TextColorFocused = element.Color.ForeColor;
            }





            // Dáme vědět existujícím controlům:
            SkinChanged?.Invoke(activeLookAndFeel, EventArgs.Empty);

            /*
            var skinSet = DXS.SkinManager.Default.GetRuntimeSkins().FirstOrDefault(s => s.SkinName == activeLookAndFeel.SkinName); // GetRuntimeSkins( .GetSkin(DXS.SkinProductId.Form);
            var skin = skinSet.GetSkin(DXS.SkinProductId.Tab);
            WSC.Skin.Reset();

            WSC.Skin.Control.ControlBackColor = skin.BaseColor;          // activeLookAndFeel.Painter.
            WSC.Skin.Control.ControlFocusBackColor = skin.BaseColor;          // activeLookAndFeel.Painter.

            WSC.Skin.Control.ControlBackColor = Color.LightBlue;
            WSC.Skin.Control.ControlFocusBackColor = Color.LightYellow;
            */
        }
        private static bool SearchSkinElement(DXS.Skin skin, out DXS.SkinElement element, params string[] keys)
        {
            foreach (string key in keys)
            {
                element = skin[key];
                if (element != null) return true;
            }
            element = null;
            return false;
        }
        public static DXL.UserLookAndFeel ActiveLookAndFeel { get { return DXL.UserLookAndFeel.Default.ActiveLookAndFeel; } }
        public static string ActiveSkinName {  get { return DXL.UserLookAndFeel.Default.ActiveSkinName; } }
        public static string ActiveSvgPaletteName { get { return DXL.UserLookAndFeel.Default.ActiveSvgPaletteName; } }


        #endregion
    }
}

// https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins
// https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins#apply-themes-to-entire-application
// https://docs.devexpress.com/WindowsForms/2397/common-features/application-appearance-and-skin-colors/look-and-feel-and-skinning?v=20.1
// https://docs.devexpress.com/WindowsForms/DevExpress.XtraEditors.WindowsFormsSettings.EnableFormSkins

/*  https://supportcenter.devexpress.com/ticket/details/a2967/how-to-obtain-the-color-of-a-particular-control-s-element-when-skins-are-used
 
    
How to obtain the color of a particular control's element when skins are used

Description:
I'm utilizing your Skins technology in my application. I want to adjust the colors of the standard controls used in my application to improve its apperance. How can I determine the colors you use to paint your editors? For example, how to get an editor's border color?

Answer:
A skinable control paints itself via a set of skin elements. Skin colors and images can be obtained from a given DevExpress.Skins.SkinElement. Here is some sample code:

DevExpress.Skins.Skin currentSkin;  
DevExpress.Skins.SkinElement element;  
string elementName;  
currentSkin = DevExpress.Skins.CommonSkins.GetSkin(defaultLookAndFeel1.LookAndFeel);  
elementName = DevExpress.Skins.CommonSkins.SkinTextBorder;  
element = currentSkin[elementName];  
Color skinBorderColor = element.Border.All;  

Ensure that a skin element name is accessed from the same class as a corresponding skin.

Skins are separated into different classes defined in the DevExpress.Skins assembly. The code above uses the CommonSkins class, containing skins for common elements used by different products. Below is a list of additional classes:
DockingSkins: contains elements and colors used by a docking library (docked windows, floating windows, tab pages, etc.).
EditorsSkins: contains elements and colors used by editors.
FormSkins: contains elements and colors used by Forms (such as control box buttons, Form caption and borders, etc.).
GridSkins: contains elements and colors used by XtraGrid.
NavBarSkins: contains common elements and colors used by XtraNavBar.
NavPaneSkins: contains elements and colors used by XtraNavBar when the "Navigation Pane" paint style is used.
PrintingSkins: contains some elements and colors specific to printing dialogs.
ReportsSkins: contains some elements and colors specific to XtraReports.
RibbonSkins: contains elements and colors used by RibbonControl.
RichEditSkins: contains some elements and colors specific to XtraRichEdit.
SchedulerSkins: contains elements and colors used by XtraScheduler.
TabSkins: contains some elements and colors specific to XtraTabControl.
VGridSkins: contains some elements and colors specific to controls from the XtraVerticalGrid library.
     
     */
