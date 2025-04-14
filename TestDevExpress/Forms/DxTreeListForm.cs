using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Noris.Clients.Win.Components.AsolDX;
using System.Drawing;
using TestDevExpress.Components;
using Noris.Clients.Win.Components;
using DevExpress.XtraRichEdit.Layout;
using DevExpress.PivotGrid.OLAP.Mdx;
using DevExpress.Utils.DirectXPaint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DevExpress.XtraBars.Docking2010.DragEngine;

namespace TestDevExpress.Forms
{
    #region DxNativeTreeListForm : Formulář s nastavením vlastností nativní DevExpress = výchozí property
    [RunFormInfo(groupText: "Testovací okna", buttonText: "Native TreeList", buttonOrder: 60, buttonImage: "svgimages/dashboards/inserttreeview.svg", buttonToolTip: "Otevře okno TreeList s parametry", tabViewToolTip: "Okno zobrazující nový TreeList")]
    internal class DxNativeTreeListForm : DxTreeListForm
    {
        protected override void RefreshTitle()
        {
            this.Text = $"DevExpress TreeList   [{CurrentId}]";
        }
        protected override void OnParamsPrepareTreeListProperties(FlowLayout flowLayout)
        {
            // Vytvoří controly pro settings
            flowLayout.StartNewColumn(110, 220);
            CreateTitle(flowLayout, "Základní vlastnosti DevExpress TreeListu");
            CheckMultiSelect = CreateToggle(flowLayout, ControlActionType.SettingsApply, "MultiSelect", "MultiSelectEnabled", "MultiSelectEnabled = výběr více nodů", "Zaškrtnuto: lze vybrat více nodů (Ctrl, Shift). Sledujme pak události.");
            TextNodeIndent = CreateSpinner(flowLayout, ControlActionType.SettingsApply, "NodeIndent", "Node indent:", 0, 100, "Node indent = odstup jednotlivých úrovní stromu", "Počet pixelů mezi nody jedné úrovně a jejich podřízenými nody, doprava.");
            CheckShowTreeLines = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowTreeLines", "Guide Lines Visible", "Guide Lines Visible = vodicí linky jsou viditelné", "Zaškrtnuto: Strom obsahuje GuideLines mezi úrovněmi nodů.", false);
            CheckShowFirstLines = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowFirstLines", "Show First Lines", "Show First Lines = zobrazit vodicí linky v první úrovni", "Zaškrtnuto: Strom obsahuje GuideLines v levé úrovni.");
            CheckShowHorzLines = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowHorzLines", "Show Horizontal Lines", "", "");
            CheckShowVertLines = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowVertLines", "Show Vertical Lines", "", "");
            ComboTreeLineStyle = CreateCombo(flowLayout, ControlActionType.SettingsApply, "TreeLineStyle", "TreeLine Style:", typeof(DevExpress.XtraTreeList.LineStyle));
            CheckShowRoot = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowRoot", "Show Root", "", "");
            CheckShowHierarchyIndentationLines = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowHierarchyLines", "Show Hierarchy Indentation Lines", "", "", true);
            CheckShowIndentAsRowStyle = CreateToggle(flowLayout, ControlActionType.SettingsApply, "ShowIndentAsRow", "Show Indent As RowStyle", "", "");
            ComboRowFilterBoxMode = CreateCombo(flowLayout, ControlActionType.SettingsApply, "RowFilterMode", "Row Filter Mode:", typeof(RowFilterBoxMode));
            ComboCheckBoxStyle = CreateCombo(flowLayout, ControlActionType.SettingsApply, "CheckBxStyle", "CheckBox Style:", typeof(DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle));
            ComboFocusRectStyle = CreateCombo(flowLayout, ControlActionType.SettingsApply, "FocusRectangleStyle", "Focus Style:", typeof(DevExpress.XtraTreeList.DrawFocusRectStyle));
            CheckEditable = CreateToggle(flowLayout, ControlActionType.SettingsApply, "Editable", "Editable", "", "");
            ComboEditingMode = CreateCombo(flowLayout, ControlActionType.SettingsApply, "EditingMode", "Editing mode:", typeof(DevExpress.XtraTreeList.TreeListEditingMode));
            ComboEditorShowMode = CreateCombo(flowLayout, ControlActionType.SettingsApply, "EditorShowMode", "Editor Show Mode:", typeof(DevExpress.XtraTreeList.TreeListEditorShowMode));
            flowLayout.EndColumn();
        }
        protected override void OnSettingLoadTreeListProperties()
        {
            // Properties jsou uváděny v typech odpovídajících TreeListu.
            //  Konvertují se z/na string do Settings;
            //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;

            SetingsMultiSelect = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsMultiSelect", ""));
            SettingsNodeIndent = ConvertToInt32(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsNodeIndent", ""), 25);
            SetingsShowTreeLines = ConvertToDefaultBoolean(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowTreeLines", null));
            SetingsShowFirstLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowFirstLines", ""));
            SetingsShowHorzLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowHorzLines", ""));
            SetingsShowVertLines = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowVertLines", ""));
            SettingsTreeLineStyle = ConvertToLineStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsTreeLineStyle", ""));
            SetingsShowRoot = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowRoot", ""));
            SetingsShowHierarchyIndentationLines = ConvertToDefaultBoolean(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsShowHierarchyIndentationLines", ""));
            SettingsShowIndentAsRowStyle = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsShowIndentAsRowStyle", ""));
            SetingsCheckBoxStyle = ConvertToNodeCheckBoxStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsCheckBoxStyle", ""));
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ""));
            SetingsFocusRectStyle = ConvertToDrawFocusRectStyle(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsFocusRectStyle", ""));
            SettingsEditable = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditable", ""));
            SettingsEditingMode = ConvertToTreeListEditingMode(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditingMode", ""));
            SettingsEditorShowMode = ConvertToTreeListEditorShowMode(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditorShowMode", ""));
        }
        protected override void OnSettingSaveTreeListProperties()
        {
            // Do DxComponent.Settings   z Properties
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsMultiSelect", ConvertToString(SetingsMultiSelect));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsNodeIndent", ConvertToString(SettingsNodeIndent));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowTreeLines", ConvertToString(SetingsShowTreeLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowFirstLines", ConvertToString(SetingsShowFirstLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowHorzLines", ConvertToString(SetingsShowHorzLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowVertLines", ConvertToString(SetingsShowVertLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsTreeLineStyle", ConvertToString(SettingsTreeLineStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowRoot", ConvertToString(SetingsShowRoot));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsShowHierarchyIndentationLines", ConvertToString(SetingsShowHierarchyIndentationLines));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsShowIndentAsRowStyle", ConvertToString(SettingsShowIndentAsRowStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsCheckBoxStyle", ConvertToString(SetingsCheckBoxStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ConvertToString(SetingsRowFilterBoxMode));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsFocusRectStyle", ConvertToString(SetingsFocusRectStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditable", ConvertToString(SettingsEditable));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditingMode", ConvertToString(SettingsEditingMode));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditorShowMode", ConvertToString(SettingsEditorShowMode));
        }
        protected override void OnSettingShowTreeListProperties()
        {
            // Do vizuálních checkboxů   z Properties
            CheckMultiSelect.Checked = SetingsMultiSelect;
            TextNodeIndent.Value = SettingsNodeIndent;
            CheckShowTreeLines.CheckState = ConvertToCheckState(SetingsShowTreeLines);
            CheckShowFirstLines.Checked = SetingsShowFirstLines;
            CheckShowHorzLines.Checked = SetingsShowHorzLines;
            CheckShowVertLines.Checked = SetingsShowVertLines;
            SelectComboItem(ComboTreeLineStyle, SettingsTreeLineStyle);
            CheckShowRoot.Checked = SetingsShowRoot;
            CheckShowHierarchyIndentationLines.CheckState = ConvertToCheckState(SetingsShowHierarchyIndentationLines);
            CheckShowIndentAsRowStyle.Checked = SettingsShowIndentAsRowStyle;
            SelectComboItem(ComboCheckBoxStyle, SetingsCheckBoxStyle);
            SelectComboItem(ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            SelectComboItem(ComboFocusRectStyle, SetingsFocusRectStyle);
            CheckEditable.Checked = SettingsEditable;
            SelectComboItem(ComboEditingMode, SettingsEditingMode);
            SelectComboItem(ComboEditorShowMode, SettingsEditorShowMode);
        }
        protected override void OnSettingCollectTreeListProperties()
        {
            // Do datových Settings    z Controlů
            SetingsMultiSelect = CheckMultiSelect.Checked;
            SettingsNodeIndent = (int)TextNodeIndent.Value;
            SetingsShowTreeLines = ConvertToDefaultBoolean(CheckShowTreeLines.CheckState);
            SetingsShowFirstLines = CheckShowFirstLines.Checked;
            SetingsShowHorzLines = CheckShowHorzLines.Checked;
            SetingsShowVertLines = CheckShowVertLines.Checked;
            SettingsTreeLineStyle = ConvertToLineStyle(ComboTreeLineStyle, SettingsTreeLineStyle);
            SetingsShowRoot = CheckShowRoot.Checked;
            SetingsShowHierarchyIndentationLines = ConvertToDefaultBoolean(CheckShowHierarchyIndentationLines.CheckState);
            SettingsShowIndentAsRowStyle = CheckShowIndentAsRowStyle.Checked;
            SetingsCheckBoxStyle = ConvertToNodeCheckBoxStyle(ComboCheckBoxStyle, SetingsCheckBoxStyle);
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            SetingsFocusRectStyle = ConvertToDrawFocusRectStyle(ComboFocusRectStyle, SetingsFocusRectStyle);
            SettingsEditable = CheckEditable.Checked;
            SettingsEditingMode = ConvertToTreeListEditingMode(ComboEditingMode, SettingsEditingMode);
            SettingsEditorShowMode = ConvertToTreeListEditingMode(ComboEditorShowMode, SettingsEditorShowMode);
        }
        protected override void OnSettingApplyTreeListProperties()
        {
            // Do TreeListu     z Properties
            DxTreeList.MultiSelectEnabled = SetingsMultiSelect;
            DxTreeList.TreeListNative.TreeLevelWidth = SettingsNodeIndent;
            DxTreeList.TreeListNative.OptionsView.ShowTreeLines = SetingsShowTreeLines;
            DxTreeList.TreeListNative.OptionsView.ShowFirstLines = SetingsShowFirstLines;
            DxTreeList.TreeListNative.OptionsView.ShowHorzLines = SetingsShowHorzLines;
            DxTreeList.TreeListNative.OptionsView.ShowVertLines = SetingsShowVertLines;
            DxTreeList.TreeListNative.OptionsView.TreeLineStyle = SettingsTreeLineStyle;
            DxTreeList.TreeListNative.OptionsView.ShowRoot = SetingsShowRoot;
            DxTreeList.TreeListNative.OptionsView.ShowHierarchyIndentationLines = SetingsShowHierarchyIndentationLines;
            DxTreeList.TreeListNative.OptionsView.ShowIndentAsRowStyle = SettingsShowIndentAsRowStyle;
            DxTreeList.TreeListNative.OptionsView.CheckBoxStyle = SetingsCheckBoxStyle;
            DxTreeList.FilterBoxMode = SetingsRowFilterBoxMode;
            DxTreeList.TreeListNative.OptionsView.RootCheckBoxStyle = DevExpress.XtraTreeList.NodeCheckBoxStyle.Default;
            DxTreeList.TreeListNative.OptionsView.FocusRectStyle = SetingsFocusRectStyle;
            DxTreeList.TreeListNative.OptionsBehavior.Editable = SettingsEditable;
            DxTreeList.TreeListNative.OptionsBehavior.EditingMode = SettingsEditingMode;
            DxTreeList.TreeListNative.OptionsBehavior.EditorShowMode = SettingsEditorShowMode;
        }

        protected DxCheckEdit CheckMultiSelect;
        protected DxSpinEdit TextNodeIndent;
        protected DxCheckEdit CheckShowTreeLines;
        protected DxCheckEdit CheckShowFirstLines;
        protected DxCheckEdit CheckShowHorzLines;
        protected DxCheckEdit CheckShowVertLines;
        protected DxImageComboBoxEdit ComboTreeLineStyle;
        protected DxCheckEdit CheckShowRoot;
        protected DxCheckEdit CheckShowHierarchyIndentationLines;
        protected DxCheckEdit CheckShowIndentAsRowStyle;
        protected DxImageComboBoxEdit ComboRowFilterBoxMode;
        protected DxImageComboBoxEdit ComboCheckBoxStyle;
        protected DxImageComboBoxEdit ComboFocusRectStyle;
        protected DxCheckEdit CheckEditable;
        protected DxImageComboBoxEdit ComboEditingMode;
        protected DxImageComboBoxEdit ComboEditorShowMode;

        // Properties jsou uváděny v typech odpovídajících TreeListu.
        //  Konvertují se z/na string do Settings;
        //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;
        internal bool SetingsMultiSelect { get; set; }
        internal int SettingsNodeIndent { get; set; }
        internal DevExpress.Utils.DefaultBoolean SetingsShowTreeLines { get; set; }
        internal bool SetingsShowFirstLines { get; set; }
        internal bool SetingsShowHorzLines { get; set; }
        internal bool SetingsShowVertLines { get; set; }
        internal DevExpress.XtraTreeList.LineStyle SettingsTreeLineStyle { get; set; }
        internal bool SetingsShowRoot { get; set; }
        internal DevExpress.Utils.DefaultBoolean SetingsShowHierarchyIndentationLines { get; set; }
        internal bool SettingsShowIndentAsRowStyle { get; set; }
        internal DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle SetingsCheckBoxStyle { get; set; }
        internal RowFilterBoxMode SetingsRowFilterBoxMode { get; set; }
        internal DevExpress.XtraTreeList.DrawFocusRectStyle SetingsFocusRectStyle { get; set; }
        internal bool SettingsEditable { get; set; }
        internal DevExpress.XtraTreeList.TreeListEditingMode SettingsEditingMode { get; set; }
        internal DevExpress.XtraTreeList.TreeListEditorShowMode SettingsEditorShowMode { get; set; }
    }
    #endregion
    #region DxAsolDxTreeListForm : Formulář s nastavením vlastností AsolDX = agregátní property
    [RunFormInfo(groupText: "Testovací okna", buttonText: "AsolDX TreeList", buttonOrder: 61, buttonImage: "svgimages/dashboards/inserttreeview.svg", buttonToolTip: "Otevře okno TreeList s parametry", tabViewToolTip: "Okno zobrazující nový TreeList")]
    internal class DxAsolDxTreeListForm : DxTreeListForm
    {
        protected override void RefreshTitle()
        {
            this.Text = $"AsolDX TreeList   [{CurrentId}]";
        }
        protected override void OnParamsPrepareTreeListProperties(FlowLayout flowLayout)
        {
            // Vytvoří controly pro settings
            flowLayout.StartNewColumn(110, 220);
            CreateTitle(flowLayout, "Agregované vlastnosti AsolDX TreeListu");

            CheckVisibleHeaders = CreateToggle(flowLayout, ControlActionType.SettingsApply, "VisibleHeaders", "Visible Headers", "Viditelné záhlaví", "Pro jeden sloupec se běžně nepoužívá, pro více sloupců je vhodné. Je vhodné pro řešení TreeList s jedním sloupcem explicitně deklarovaným (např. kvůli zarovnání nebo HTML formátování).");
            ComboRowFilterBoxMode = CreateCombo(flowLayout, ControlActionType.SettingsApply, "RowFilterMode", "Row Filter Mode:", typeof(RowFilterBoxMode));
            CheckMultiSelect = CreateToggle(flowLayout, ControlActionType.SettingsApply, "MultiSelect", "MultiSelectEnabled", "MultiSelectEnabled = výběr více nodů", "Zaškrtnuto: lze vybrat více nodů (Ctrl, Shift). Sledujme pak události.");
            TextNodeIndent = CreateSpinner(flowLayout, ControlActionType.SettingsApply, "NodeIndent", "Node indent:", 0, 100, "Node indent = odstup jednotlivých úrovní stromu", "Počet pixelů mezi nody jedné úrovně a jejich podřízenými nody, doprava.");
            ComboLevelLineType = CreateCombo(flowLayout, ControlActionType.SettingsApply, "LevelLineType", "LevelLineType:", typeof(TreeLevelLineType));
            ComboCellLinesType = CreateCombo(flowLayout, ControlActionType.SettingsApply, "CellLinesType", "CellLinesType:", typeof(TreeCellLineType));
            CheckEditable = CreateToggle(flowLayout, ControlActionType.SettingsApply, "Editable", "Editable", "", "");
            ComboEditorStartMode = CreateCombo(flowLayout, ControlActionType.SettingsApply, "EditorStartMode", "EditorStartMode:", typeof(TreeEditorStartMode));
        }
        protected override void OnSettingLoadTreeListProperties()
        {
            // Properties jsou uváděny v typech odpovídajících TreeListu.
            //  Konvertují se z/na string do Settings;
            //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;

            SetingsVisibleHeaders = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsVisibleHeaders", ""));
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ""));
            SetingsMultiSelect = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsMultiSelect", ""));
            SettingsNodeIndent = ConvertToInt32(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsNodeIndent", ""), 25);
            SettingsLevelLineType = ConvertToLevelLineType(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsLevelLineType", ""));
            SettingsCellLinesType = ConvertToCellLinesType(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsCellLinesType", ""));
            SettingsEditable = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditable", ""));
            SettingsEditorStartMode = ConvertToEditorStartMode(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsEditorStartMode", ""));
        }
        protected override void OnSettingSaveTreeListProperties()
        {
            // Do DxComponent.Settings   z Properties
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsVisibleHeaders", ConvertToString(SetingsVisibleHeaders));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsRowFilterBoxMode", ConvertToString(SetingsRowFilterBoxMode));
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsMultiSelect", ConvertToString(SetingsMultiSelect));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsNodeIndent", ConvertToString(SettingsNodeIndent));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsLevelLineType", ConvertToString(SettingsLevelLineType));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsCellLinesType", ConvertToString(SettingsCellLinesType));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditable", ConvertToString(SettingsEditable));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsEditorStartMode", ConvertToString(SettingsEditorStartMode));
        }
        protected override void OnSettingShowTreeListProperties()
        {
            // Do vizuálních checkboxů   z Properties
            CheckVisibleHeaders.Checked = SetingsVisibleHeaders;
            SelectComboItem(ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            CheckMultiSelect.Checked = SetingsMultiSelect;
            TextNodeIndent.Value = SettingsNodeIndent;
            SelectComboItem(ComboLevelLineType, SettingsLevelLineType);
            SelectComboItem(ComboCellLinesType, SettingsCellLinesType);
            CheckEditable.Checked = SettingsEditable;
            SelectComboItem(ComboEditorStartMode, SettingsEditorStartMode);
        }
        protected override void OnSettingCollectTreeListProperties()
        {
            // Do datových Settings    z Controlů
            SetingsVisibleHeaders = CheckVisibleHeaders.Checked;
            SetingsRowFilterBoxMode = ConvertToRowFilterBoxMode(ComboRowFilterBoxMode, SetingsRowFilterBoxMode);
            SetingsMultiSelect = CheckMultiSelect.Checked;
            SettingsNodeIndent = (int)TextNodeIndent.Value;
            SettingsLevelLineType = ConvertToLevelLineType(ComboLevelLineType, SettingsLevelLineType);
            SettingsCellLinesType = ConvertToCellLinesType(ComboCellLinesType, SettingsCellLinesType);
            SettingsEditable = CheckEditable.Checked;
            SettingsEditorStartMode = ConvertToEditorStartMode(ComboEditorStartMode, SettingsEditorStartMode);
        }
        protected override void OnSettingApplyTreeListProperties()
        {
            // Do TreeListu     z Properties
            DxTreeList.ColumnHeadersVisible = SetingsVisibleHeaders;
            DxTreeList.FilterBoxMode = SetingsRowFilterBoxMode;
            DxTreeList.MultiSelectEnabled = SetingsMultiSelect;
            DxTreeList.TreeNodeIndent = SettingsNodeIndent;
            DxTreeList.LevelLineType = SettingsLevelLineType;
            DxTreeList.CellLinesType = SettingsCellLinesType;
            DxTreeList.IsEditable = SettingsEditable;
            DxTreeList.EditorStartMode = SettingsEditorStartMode;
        }

        protected DxCheckEdit CheckVisibleHeaders;
        protected DxImageComboBoxEdit ComboRowFilterBoxMode;
        protected DxCheckEdit CheckMultiSelect;
        protected DxSpinEdit TextNodeIndent;
        protected DxImageComboBoxEdit ComboLevelLineType;
        protected DxImageComboBoxEdit ComboCellLinesType;
        protected DxCheckEdit CheckEditable;
        protected DxImageComboBoxEdit ComboEditorStartMode;

        // Properties jsou uváděny v typech odpovídajících TreeListu.
        //  Konvertují se z/na string do Settings;
        //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;

        internal bool SetingsVisibleHeaders { get; set; }
        internal RowFilterBoxMode SetingsRowFilterBoxMode { get; set; }
        internal bool SetingsMultiSelect { get; set; }
        internal int SettingsNodeIndent { get; set; }
        internal TreeLevelLineType SettingsLevelLineType { get; set; }
        internal TreeCellLineType SettingsCellLinesType { get; set; }
        internal bool SettingsEditable { get; set; }
        internal TreeEditorStartMode SettingsEditorStartMode { get; set; }
    }
    #endregion
    /// <summary>
    /// Bázová třída - nezobrazuje se, obsahuje virtual podklady pro variabilní konfiguraci
    /// </summary>
    internal class DxTreeListForm : DxRibbonForm
    {
        #region Inicializace
        public DxTreeListForm()
        {
            // Páry barev mají: Item1 = Písmo, Item2 = Pozadí:
            var pairs = new Tuple<string, string>[] { Constants.ColorPairGreen, Constants.ColorPairRed, Constants.ColorPairYellow, Constants.ColorPairOrange, Constants.ColorPairBlue, Constants.ColorPairTurquoise, Constants.ColorPairPurple, Constants.ColorPairBrown, Constants.ColorPairBlack };
            var pair = pairs[Counter % (pairs.Length)];
            string znak = ((char)(65 + (Counter % 25))).ToString();

            Counter++;

            this.ImageName = "svgimages/dashboards/inserttreeview.svg";
            // Starý Nephrite:  this.ImageNameAdd = $"@text|{znak}|{pair.Item1}|tahoma|B|4|{pair.Item1}|{pair.Item2}";
            // Nový Nephrite:
            var iconData = new SvgImageTextIcon()
            {
                Text = znak,
                TextBold = true,
                TextFont = SvgImageTextIcon.TextFontType.Tahoma,
                TextColorName = pair.Item1,
                BackColorName = pair.Item2,
                BorderColorBW = false,
                Padding = 3,
                BorderWidth = 1,
                Rounding = 8
            };
            this.ImageNameAdd = iconData.SvgImageName;

            CurrentId = ++InstanceCounter;
            RefreshTitle();
        }
        protected virtual void RefreshTitle()
        {
            this.Text = $"TreeList   [{CurrentId}]";
        }
        protected static int Counter = 12;
        protected int CurrentId;
        protected static int InstanceCounter;
        #endregion
        #region Ribbon - obsah a rozcestník
        protected override void DxRibbonPrepare()
        {
            var ribbonContent = new DataRibbonContent();
            var homePage = DxRibbonControl.CreateStandardHomePage();
            var treePrepareGroup = new DataRibbonGroup() { GroupText = "Vytvoření TreeListu" };
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet50", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 50", RibbonStyle = RibbonItemStyles.Large });
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet500", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 500", RibbonStyle = RibbonItemStyles.Large });
            treePrepareGroup.Items.Add(new DataRibbonItem() { ItemId = "TreePrepareSet5000", ImageName = "svgimages/icon%20builder/actions_addcircled.svg", Text = "Vytvoř TreeList 5000", RibbonStyle = RibbonItemStyles.Large });
            homePage.Groups.Add(treePrepareGroup);
            ribbonContent.Pages.Add(homePage);

            this.DxRibbon.RibbonContent = ribbonContent;
            this.DxRibbon.RibbonItemClick += DxRibbonControl_RibbonItemClick;
        }
        protected void DxRibbonControl_RibbonItemClick(object sender, DxRibbonItemClickArgs e)
        {
            var itemId = e.Item.ItemId;
            switch (itemId)
            {
                case "TreePrepareSet50": PrepareTreeList(20, 1); break;
                case "TreePrepareSet500": PrepareTreeList(40, 2); break;
                case "TreePrepareSet5000": PrepareTreeList(80, 3); break;
            }
        }
        #endregion
        #region Hlavní controly - tvorba a převolání Initů
        protected override void DxMainContentPrepare()
        {
            base.DxMainContentPrepare();

            MainSplitContainer = new DxSplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 450, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterOrientation = Orientation.Horizontal, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True, Name = "MainSplitContainer" };
            this.DxMainPanel.Controls.Add(MainSplitContainer);

            DxTreeList = new DxTreeList() { Dock = DockStyle.Fill, Name = "DxTreeList" };
            TreeListInit();
            MainSplitContainer.Panel1.Controls.Add(DxTreeList);
            MainSplitContainer.Panel1.MinSize = 200;

            ParamSplitContainer = new DxSplitContainerControl() { Dock = DockStyle.Fill, SplitterPosition = 300, FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel1, SplitterOrientation = Orientation.Vertical, ShowSplitGlyph = DevExpress.Utils.DefaultBoolean.True, Name = "ParamSplitContainer" };
            MainSplitContainer.Panel2.Controls.Add(ParamSplitContainer);

            ParamsPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "ParamsPanel" };
            ParamsInit();
            ParamSplitContainer.Panel1.Controls.Add(ParamsPanel);

            LogPanel = new DxPanelControl() { Dock = DockStyle.Fill, Name = "LogPanel" };
            LogInit();
            ParamSplitContainer.Panel2.Controls.Add(LogPanel);

            SettingLoad();
            SampleLoad();
        }
        protected static Keys[] CreateHotKeys()
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
        protected Noris.Clients.Win.Components.AsolDX.DxSplitContainerControl MainSplitContainer;
        protected Noris.Clients.Win.Components.AsolDX.DxTreeList DxTreeList;
        protected Noris.Clients.Win.Components.AsolDX.DxSplitContainerControl ParamSplitContainer;
        protected Noris.Clients.Win.Components.AsolDX.DxPanelControl ParamsPanel;
        protected Noris.Clients.Win.Components.AsolDX.DxPanelControl LogPanel;
        #endregion
        #region TreeList setting a events
        protected void TreeListInit()
        {
            DxTreeList.CheckBoxMode = TreeListCheckBoxMode.SpecifyByNode;
            DxTreeList.LazyLoadNodeText = "Copak to tu asi bude?";
            DxTreeList.LazyLoadNodeImageName = "hourglass_16";
            DxTreeList.LazyLoadFocusNode = TreeListLazyLoadFocusNodeType.ParentNode;
            DxTreeList.FilterBoxMode = RowFilterBoxMode.Server;
            DxTreeList.EditorShowMode = DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;
            DxTreeList.IncrementalSearchMode = TreeListIncrementalSearchMode.InAllNodes;
            DxTreeList.FilterBoxOperators = DxFilterBox.CreateDefaultOperatorItems(FilterBoxOperatorItems.DefaultText);
            DxTreeList.FilterBoxChangedSources = DxFilterBoxChangeEventSource.Default;
            DxTreeList.MultiSelectEnabled = true;
            DxTreeList.MainClickMode = NodeMainClickMode.AcceptNodeSetting;

            DxTreeList.NodeImageSize = ResourceImageSizeType.Large;        // Zkus různé...
            DxTreeList.NodeImageSize = ResourceImageSizeType.Medium;
            DxTreeList.NodeImageSize = ResourceImageSizeType.Small;

            DxTreeList.NodeAllowHtmlText = true;

            DxTreeList.HotKeys = CreateHotKeys();

            DxTreeList.FilterBoxChanged += TreeList_FilterBoxChanged;
            DxTreeList.FilterBoxKeyEnter += TreeList_FilterBoxKeyEnter;
            DxTreeList.NodeKeyDown += TreeList_NodeKeyDown;
            DxTreeList.NodeFocusedChanged += TreeList_AnyAction;
            DxTreeList.SelectedNodesChanged += TreeList_SelectedNodesChanged;
            DxTreeList.ShowContextMenu += TreeList_ShowContextMenu;
            DxTreeList.NodeIconClick += TreeList_IconClick;
            DxTreeList.NodeItemClick += TreeList_ItemClick;
            DxTreeList.NodeDoubleClick += _TreeList_DoubleClick;
            DxTreeList.NodeExpanded += TreeList_AnyAction;
            DxTreeList.NodeCollapsed += TreeList_AnyAction;
            DxTreeList.ActivatedEditor += TreeList_AnyAction;
            DxTreeList.EditorDoubleClick += _TreeList_DoubleClick;
            DxTreeList.NodeEdited += _TreeList_NodeEdited;
            DxTreeList.NodeCheckedChange += TreeList_AnyAction;
            DxTreeList.NodesDelete += _TreeList_NodesDelete;
            DxTreeList.LazyLoadChilds += _TreeList_LazyLoadChilds;
            DxTreeList.ToolTipChanged += _TreeList_ToolTipChanged;
            DxTreeList.MouseLeave += _TreeList_MouseLeave;
        }
        protected void TreeList_AnyAction(object sender, DxTreeListNodesArgs args)
        {
            AddToLog(args.Action.ToString(), args);
        }
        protected void TreeList_AnyAction(object sender, DxTreeListNodeArgs args)
        {
            AddToLog(args.Action.ToString(), args, (args.Action == TreeListActionType.NodeEdited || args.Action == TreeListActionType.EditorDoubleClick || args.Action == TreeListActionType.NodeCheckedChange));
        }
        protected void TreeList_FilterBoxChanged(object sender, DxFilterBoxChangeArgs args)
        {
            var filter = this.DxTreeList.FilterBoxValue;
            AddToLog($"RowFilter: Change: {args.EventSource}; Operator: {args.FilterValue.FilterOperator?.ItemId}, Text: \"{args.FilterValue.FilterText}\"");
        }
        protected void TreeList_FilterBoxKeyEnter(object sender, EventArgs e)
        {
            AddToLog($"RowFilter: 'Enter' pressed");
        }
        protected void TreeList_NodeKeyDown(object sender, DxTreeListNodeKeyArgs args)
        {
            AddToLog($"KeyUp: Node: {args.Node?.Text}; KeyCode: '{args.KeyArgs.KeyCode}'; KeyData: '{args.KeyArgs.KeyData}'; Modifiers: {args.KeyArgs.Modifiers}");
        }
        protected void TreeList_SelectedNodesChanged(object sender, DxTreeListNodeArgs args)
        {
            int count = 0;
            string selectedNodes = "";
            DxTreeList.SelectedNodes.ForEachExec(n => { count++; selectedNodes += "; '" + n.ToString() + "'"; });
            if (selectedNodes.Length > 0) selectedNodes = selectedNodes.Substring(2);
            AddToLog($"SelectedNodesChanged: Selected {count} Nodes: {selectedNodes}");
        }
        protected void TreeList_ShowContextMenu(object sender, DxTreeListNodeContextMenuArgs args)
        {
            AddToLog($"ShowContextMenu: Node: {args.Node} Part: {args.HitInfo.PartType}");
            if (args.Node != null)
                ShowDXPopupMenu(Control.MousePosition);
        }
        protected void TreeList_IconClick(object sender, DxTreeListNodeArgs args)
        {
            TreeList_AnyAction(sender, args);
        }
        protected void TreeList_ItemClick(object sender, DxTreeListNodeArgs args)
        {
            TreeList_AnyAction(sender, args);
        }
        protected void _TreeList_DoubleClick(object sender, DxTreeListNodeArgs args)
        {
            TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => TreeNodeDoubleClickBgr(args));
        }
        protected void _TreeList_NodeEdited(object sender, DxTreeListNodeArgs args)
        {
            TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => TreeNodeEditedBgr(args));
        }
        protected void _TreeList_NodesDelete(object sender, DxTreeListNodesArgs args)
        {
            TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => TreeNodeDeleteBgr(args));
        }
        protected void _TreeList_LazyLoadChilds(object sender, DxTreeListNodeArgs args)
        {
            TreeList_AnyAction(sender, args);
            ThreadManager.AddAction(() => LoadChildNodesFromServerBgr(args));
        }
        protected void _TreeList_ToolTipChanged(object sender, DxToolTipArgs args)
        {
            if (SetingsLogToolTipChanges)
            {
                string line = "ToolTip: " + args.EventName;
                bool skipGUI = (line.Contains("IsFASTMotion"));             // ToolTip obsahující IsFASTMotion nebudu dávat do GUI Textu - to jsou rychlé eventy:
                AddToLog(line, skipGUI);
            }
        }
        protected void _TreeList_MouseLeave(object sender, EventArgs e)
        {
            if (TreeListPending)
                AddToLog("TreeList.MouseLeave");
        }
        #endregion
        #region Kontextové menu
        protected void ShowDXPopupMenu(Point mousePosition)
        {

        }
        #endregion
        #region TreeList a BackgroundRun
        protected void TreeNodeDoubleClickBgr(DxTreeListNodeArgs args)
        {
            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            if (args.Node.NodeType == NodeItemType.OnDoubleClickLoadNext)
            {
                DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    DxTreeList.RemoveNode(node.ItemId);            // Odeberu OnDoubleClickLoadNext node, to kvůli pořadí: nový OnDoubleClickLoadNext přidám (možná) nakonec

                    var newNodes = CreateNodes(node, false, true);
                    DxTreeList.AddNodes(newNodes);

                    // Aktivuji první přidaný node:
                    if (newNodes.Length > 0)
                        DxTreeList.SetFocusToNode(newNodes[0]);
                }
               ), args.Node);
            }
        }
        protected void TreeNodeEditedBgr(DxTreeListNodeArgs args)
        {
            var nodeInfo = args.Node;
            string nodeId = nodeInfo.ItemId;
            string column = (args.ColumnIndex.HasValue ? "; Column:" + args.ColumnIndex.Value.ToString() : "");
            string parentNodeId = nodeInfo.ParentNodeFullId;

            string textInfo = "";
            string newValue = "";
            if (args.ColumnIndex.HasValue && nodeInfo.Cells != null && args.ColumnIndex.Value >= 0 && args.ColumnIndex.Value < nodeInfo.Cells.Length)
            {
                newValue = nodeInfo.Cells[args.ColumnIndex.Value] as string;
                textInfo = $"Nová hodnota: '{newValue}'";
            }
            else
            {
                newValue = nodeInfo.TextEdited;
                textInfo = $"Výchozí hodnota: '{nodeInfo.Text}' => Nová hodnota: '{newValue}'";
            }

            AddToLog($"Změna textu pro node '{nodeId}'{column}: {textInfo}");

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            /*
            var newNodePosition = __NewNodePosition;
            bool isBlankNode = (oldValue == "" && (newNodePosition == NewNodePositionType.First || newNodePosition == NewNodePositionType.Last));
            if (String.IsNullOrEmpty(newValue))
            {   // Delete node:
                if (nodeInfo.CanDelete)
                    __DxTreeList.RemoveNode(nodeId);
            }
            else if (nodeInfo.NodeType == NodeItemType.BlankAtFirstPosition) // isBlankNode && newPosition == NewNodePositionType.First)
            {   // Insert new node, a NewPosition je First = je první (jako Green):
                __DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    node.Text = "";                                 // Z prvního node odeberu jeho text, aby zase vypadal jako nový node
                    node.Refresh();

                    // Přidám nový node pro konkrétní text = jakoby nově zadaný záznam:
                    DataTreeListNode newNode = _CreateNode(node.ParentNodeFullId, NodeItemType.DefaultText);
                    if (newNode != null)
                    {
                        newNode.Text = newValue;
                        __DxTreeList.AddNode(newNode, 1);
                    }
                }
                ), nodeInfo);
            }
            else if (isBlankNode && newNodePosition == NewNodePositionType.Last)
            {   // Insert new node, a NewPosition je Last = na konci:
                __DxTreeList.RunInLock(new Action<DataTreeListNode>(node =>
                {   // V jednom vizuálním zámku:
                    __DxTreeList.RemoveNode(node.ItemId);              // Odeberu blank node, to kvůli pořadí: nový blank přidám nakonec

                    // Přidám nový node pro konkrétní text = jakoby záznam:
                    DataTreeListNode newNode = _CreateNode(node.ParentNodeFullId, NodeItemType.DefaultText);
                    if (newNode != null)
                    {
                        newNode.Text = newValue;
                        __DxTreeList.AddNode(newNode);
                    }

                    // Přidám Blank node, ten bude opět na konci Childs:
                    DataTreeListNode blankNode = _CreateNode(node.ParentNodeFullId, NodeItemType.BlankAtLastPosition);
                    if (blankNode != null)
                    {
                        __DxTreeList.AddNode(blankNode);
                    }

                    // Aktivuji editovaný node:
                    if (newNode != null)
                    {
                        __DxTreeList.SetFocusToNode(newNode);
                    }
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

            */
        }
        protected void TreeNodeDeleteBgr(DxTreeListNodesArgs args)
        {
            var removeNodeKeys = args.Nodes.Select(n => n.ItemId).ToArray();

            System.Threading.Thread.Sleep(720);                      // Něco jako uděláme...

            DxTreeList.RemoveNodes(removeNodeKeys);
        }
        protected void LoadChildNodesFromServerBgr(DxTreeListNodeArgs args)
        {
            var parentNode = args.Node;
            var parentNodeId = parentNode.ItemId;
            AddToLog($"Načítám data pro node '{parentNodeId}'...");

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
            var nodes = CreateNodes(parentNode);                          // A pak vyrobíme Child nody
            AddToLog($"Načtena data: {nodes.Length} prvků.");
            DxTreeList.AddLazyLoadNodes(parentNodeId, nodes);            //  a pošleme je do TreeView.
        }
        #endregion
        #region Vytváření sloupců a nodů, smazání a plnění dat do TreeListu
        /// <summary>
        /// Naplní nějaká výchozí data po otevření okna
        /// </summary>
        protected void SampleLoad()
        {
            PrepareTreeList(25, 1);
        }
        /// <summary>
        /// Vymaže obsah TreeListu
        /// </summary>
        protected void ClearTreeList()
        {
            PrepareTreeList(0, 0);
        }
        /// <summary>
        /// Smaže definice sloupců
        /// </summary>
        protected void ClearTreeColumns()
        {
            CheckColumns(true);
        }
        /// <summary>
        /// Naplní data do TreeListu pro daný požadavek na cca počet nodů a počet sub-úrovní
        /// </summary>
        /// <param name="sampleCountBase"></param>
        /// <param name="sampleLevelsCount"></param>
        protected void PrepareTreeList(int sampleCountBase, int sampleLevelsCount)
        {
            LogClear();

            CheckColumns();

            DxComponent.LogActive = true;

            TotalNodesCount = 0;
            SampleCountBase = sampleCountBase;
            SampleLevelsCount = sampleLevelsCount;
            if (sampleCountBase == 0)
            {
                var time0 = DxComponent.LogTimeCurrent;
                DxTreeList.ClearNodes();
                var time1 = DxComponent.LogTimeCurrent;
                this.AddToLog($"Smazání nodů z TreeListu; čas: {DxComponent.LogGetTimeElapsed(time0, time1, DxComponent.LogTokenTimeMilisec)} ms");
            }
            else
            {
                var time0 = DxComponent.LogTimeCurrent;
                var nodes = CreateNodes(null);
                var time1 = DxComponent.LogTimeCurrent;
                DxTreeList.AddNodes(nodes, true, PreservePropertiesMode.None);
                var time2 = DxComponent.LogTimeCurrent;

                this.AddToLog($"Tvorba nodů; počet: {nodes.Length}; čas: {DxComponent.LogGetTimeElapsed(time0, time1, DxComponent.LogTokenTimeMilisec)} ms");
                this.AddToLog($"Plnění nodů do TreeList; počet: {nodes.Length}; čas: {DxComponent.LogGetTimeElapsed(time1, time2, DxComponent.LogTokenTimeMilisec)} ms");
            }

            DxTreeList.TreeListNative.Refresh();

            string text = this.GetControlStructure();
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné sloupce podle předvolby <see cref="SettingsUseMultiColumns"/>
        /// </summary>
        protected void CheckColumns(bool force = false)
        {
            bool useMultiColumns = SettingsUseMultiColumns;
            var dxColumns = DxTreeList.TreeListNative.DxColumns;
            bool isHtmlFormatted = SettingsUseHtmlFormat;
            bool isChangeHtmlColumns = (CurrentColumnHtmlFormat != isHtmlFormatted);
            if (useMultiColumns && (force || isChangeHtmlColumns || (dxColumns is null || dxColumns.Length < 3)))
                CreateMultiColumns(isHtmlFormatted);
            else if (!useMultiColumns && (force || isChangeHtmlColumns || (dxColumns != null && dxColumns.Length >= 3)))
                CreateSingleColumns(isHtmlFormatted);
            CurrentColumnHtmlFormat = isHtmlFormatted;
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné Multi sloupce
        /// </summary>
        protected void CreateMultiColumns(bool isHtmlFormatted)
        {
            List<DataTreeListColumn> dxColumns = new List<DataTreeListColumn>();
            dxColumns.Add(new DataTreeListColumn() { Caption = "Text", Width = 220, MinWidth = 150, IsEditable = true });
            dxColumns.Add(new DataTreeListColumn() { Caption = "Informace", Width = 120, MinWidth = 80, HeaderContentAlignment = DevExpress.Utils.HorzAlignment.Center, CellContentAlignment = DevExpress.Utils.HorzAlignment.Far, IsEditable = false });
            dxColumns.Add(new DataTreeListColumn() { Caption = "Popisek", Width = 160, MinWidth = 100, IsEditable = true, IsHtmlFormatted = isHtmlFormatted });
            DxTreeList.DxColumns = dxColumns.ToArray();
        }
        /// <summary>
        /// Metoda zajistí, že TreeList bude mít připravené správné Single sloupce
        /// </summary>
        protected void CreateSingleColumns(bool isHtmlFormatted)
        {
            if (isHtmlFormatted)
                // SingleColumn, používající HTML => musím jej explicitně vytvořit, abych do něj mohl vepsat EnableHtmlFormat = true :
                DxTreeList.DxColumns = new DataTreeListColumn[] { new DataTreeListColumn() { Caption = "   ", Width = 4000, IsEditable = false, IsHtmlFormatted = isHtmlFormatted } };
            else
                // Default nám vyhovuje null, komponenta se vygeneruje prázdný sloupec:
                DxTreeList.DxColumns = null;
        }
        /// <summary>
        /// Vytvoří nody
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="canAddEditable"></param>
        /// <param name="canAddShowNext"></param>
        /// <returns></returns>
        protected DataTreeListNode[] CreateNodes(ITreeListNode parentNode, bool canAddEditable = true, bool canAddShowNext = true)
        {
            List<DataTreeListNode> nodes = new List<DataTreeListNode>();
            AddNodesToList(parentNode, canAddEditable, canAddShowNext, nodes);
            return nodes.ToArray();
        }
        /// <summary>
        /// Vrací počet prvků reálně přidaných
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="canAddEditable"></param>
        /// <param name="canAddShowNext"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        protected int AddNodesToList(ITreeListNode parentNode, bool canAddEditable, bool canAddShowNext, List<DataTreeListNode> nodes)
        {
            int result = 0;
            int currentLevel = getNodeLevel(parentNode);
            int count = getCount(currentLevel);
            for (int i = 0; i < count; i++)
            {
                var child = CreateNode(parentNode?.ItemId, NodeItemType.DefaultText);
                if (child is null) break;

                child.ParentItem = parentNode;
                nodes.Add(child);
                result++;
                bool hasChild = false;
                if (canAddChilds(currentLevel))
                {
                    var childCount = AddNodesToList(child, canAddEditable, canAddShowNext, nodes);
                    hasChild = (childCount > 0);
                    if (hasChild && Randomizer.IsTrue(25))
                    {
                        child.IsExpanded = true;
                    }
                }
                // Tento konkrétní node mohu editovat tehdy, když node nemá SubNodes:
                //   Pokud by nebyla povolena editace celého TreeListu, tak nelze editovat ani takový Node!
                child.IsEditable = !hasChild;
                child.CanCheck = !hasChild && this.SettingsUseCheckBoxes && Randomizer.IsTrue(40);
            }
            return result;

            // Určí level pro daný node. Pokud je null, pak výstup je 0.
            int getNodeLevel(IMenuItem node)
            {
                int level = 0;
                while (node != null)
                {
                    level++;
                    node = node.ParentItem;
                }
                return level;
            }
            // Určí, zda je vhodné přidat subnody do dané úrovně
            bool canAddChilds(int level)
            {
                if (level >= SampleLevelsCount) return false;
                int probability = (level == 0 ? 50 : (level == 1 ? 25 : (level == 2 ? 10 : 0)));
                return Randomizer.IsTrue(probability);
            }
            // Určí počet prvků do daného levelu
            int getCount(int level)
            {
                if (level > SampleLevelsCount) return 0;

                int baseCount = SampleCountBase;
                if (level > 0)
                    baseCount = baseCount / (level + 1);

                return Randomizer.GetValueInRange(baseCount * 60 / 100, baseCount * 175 / 100);
            }
        }
        /// <summary>
        /// Vytvoří a vrátí jeden Node
        /// </summary>
        /// <param name="parentKey"></param>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        protected DataTreeListNode CreateNode(string parentKey, NodeItemType nodeType)
        {
            if (TotalNodesCount >= MaxNodesCount) return null;

            string childKey = "C." + (++InternalNodeId).ToString();
            string text = "";
            DataTreeListNode childNode = null;
            switch (nodeType)
            {
                case NodeItemType.BlankAtFirstPosition:
                case NodeItemType.BlankAtLastPosition:
                    text = "";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, isEditable: true, canDelete: false);          // Node pro přidání nového prvku (Blank) nelze odstranit
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Zadejte referenci nového prvku";
                    childNode.SuffixImageName = GetSuffixImageName();
                    TotalNodesCount++;
                    break;
                case NodeItemType.OnDoubleClickLoadNext:
                    text = "Načíst další záznamy";
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, isEditable: false, canDelete: false);        // Node pro zobrazení dalších nodů nelze editovat ani odstranit
                    childNode.FontStyle = FontStyle.Italic;
                    childNode.AddVoidCheckSpace = true;
                    childNode.ToolTipText = "Umožní načíst další sadu záznamů...";
                    childNode.SuffixImageName = GetSuffixImageName();
                    TotalNodesCount++;
                    break;
                case NodeItemType.DefaultText:
                    text = Randomizer.GetSentence(2, 5);
                    childNode = new DataTreeListNode(childKey, parentKey, text, nodeType: nodeType, isEditable: true, canDelete: true);
                    childNode.CanCheck = true;
                    childNode.Checked = (Randomizer.Rand.Next(20) > 16);

                    // Jeden sloupec a HTML:
                    if (!SettingsUseMultiColumns && SettingsUseHtmlFormat)
                    {
                        childNode.Text = applyHtmlFormat(childNode.Text);
                    }

                    // Více sloupců?
                    if (SettingsUseMultiColumns)
                    {
                        childNode.Cells = new string[]
                        {
                            text,
                            Randomizer.GetSentence(1, 3),
                            createCell2()
                        };
                    }

                    FillNode(childNode);
                    TotalNodesCount++;
                    break;
            }
            return childNode;

            string createCell2()
            {
                if (!this.SettingsUseHtmlFormat) return Randomizer.GetSentence(1, 3);

                string cellText = Randomizer.GetSentence(3, 7);
                return applyHtmlFormat(cellText);
            }
            string applyHtmlFormat(string text)
            {
                if (Randomizer.IsTrue(50)) return text;                        // Polovina nodů NEBUDE mít HTML formátování

                var words = text.Split(' ');
                int changeIndex = Randomizer.Rand.Next(words.Length);

                string word = words[changeIndex];
                string code = Randomizer.GetItem("b", "u", "i", "backcolor", "color", "b", "u", "i", "b", "u", "i", "a");       // Opakování prvků není "Matka moudrosti", ale navýšení pravděpodobnosti jejich výskytu :-)
                string style = "";
                if (code == "backcolor")
                    style = "=" + Randomizer.GetColorHex(160, 250);
                if (code == "color")
                    style = "=" + Randomizer.GetColorHex(16, 80);

                if (code == "a")
                {
                    string url = Randomizer.GetItem("seznam.cz", "idnes.cz", "google.com", "yahoo.com", "ceskatelevize.cz", "assecosolutions.cz");
                    style = $" href=\"https://www.{url}\"";                     // <a href="https://www.assecosolutions.cz">assecosolutions.cz</a>
                    word = url;
                }
                words[changeIndex] = $"<{code}{style}>{word}</{code}>";        // <color=#152e10">Slovo</color>
                return words.ToOneString(" ");
            }
        }
        /// <summary>
        /// Naplní data do nodu, vyjma textu. Plní ToolTip, ikony, styl, kalíšek - podle Settings.
        /// </summary>
        /// <param name="node"></param>
        protected void FillNode(DataTreeListNode node)
        {
            if (Randomizer.IsTrue(7))
                node.SuffixImageName = GetSuffixImageName();

            node.ImageName = GetMainImageName(SettingsNodeImageSet);
            node.ToolTipTitle = null;
            node.ToolTipText = Randomizer.GetSentence(10, 50);

            if (SettingsUseExactStyle && Randomizer.IsTrue(33))
            {
                node.FontStyle = getRandomFontStyle();
                node.FontSizeRatio = getRandomSizeRatio();
                node.BackColor = getRandomBackColor();
                node.ForeColor = getRandomForeColor();
            }
            if (SettingsUseStyleName && Randomizer.IsTrue(33))
            {
                node.StyleName = getRandomStyleName();
            }

            FontStyle? getRandomFontStyle()
            {
                FontStyle? result = null;
                if (Randomizer.IsTrue(60))
                {
                    bool isBold = Randomizer.IsTrue(50);
                    bool isItalic = Randomizer.IsTrue(20);
                    result = (isBold ?  FontStyle.Bold : FontStyle.Regular) 
                           | (isItalic ? FontStyle.Italic : FontStyle.Regular);
                }
                return result;
            }
            float? getRandomSizeRatio()
            {
                float? result = null;
                if (Randomizer.IsTrue(60))
                {
                    int delta = Randomizer.GetValueInRange(6, 14);
                    result = ((float)delta) / 10f;
                }
                return result;
            }
            Color? getRandomBackColor()
            {
                Color? result = null;
                if (Randomizer.IsTrue(20))
                {
                    result = GetRandomBackColor();
                }
                return result;
            }
            Color? getRandomForeColor()
            {
                Color? result = null;
                if (Randomizer.IsTrue(35))
                {
                    result = GetRandomForeColor();
                }
                return result;
            }
            string getRandomStyleName()
            {
                string result = null;
                if (Randomizer.IsTrue(35))
                {
                    result = GetRandomStyleName();
                }
                return result;
            }
        }
        /// <summary>
        /// ID posledně přiděleného nodu
        /// </summary>
        protected int InternalNodeId;
        /// <summary>
        /// Celkový počet vygenerovaných nodes
        /// </summary>
        protected int TotalNodesCount;
        /// <summary>
        /// Maximální počet nodů
        /// </summary>
        protected static int MaxNodesCount { get { return 10000; } }
        /// <summary>
        /// Základní typický počet nodů v Root úrovni; pro subnody je poloviční.
        /// </summary>
        protected int SampleCountBase;
        /// <summary>
        /// Maximální počet úrovní
        /// </summary>
        protected int SampleLevelsCount;
        /// <summary>
        /// Stav HTML format aplikovaný do sloupců
        /// </summary>
        protected bool CurrentColumnHtmlFormat;
        #endregion
        #region Ikony: druhy ikon, seznam názvů podle druhů, generátor ikony, barvy, stylu
        /// <summary>
        /// Vrátí náhodný Main obrázek z dané sady <paramref name="imageSet"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetMainImageName(NodeImageSetType imageSet)
        {
            var images = _GetMainImageNames(imageSet);
            if (images != null && images.Length > 0)
                return Randomizer.GetItem(images);
            return null;
        }
        /// <summary>
        /// Vrať náhodný Suffix image name
        /// </summary>
        /// <returns></returns>
        protected virtual string GetSuffixImageName()
        {
            if (__ImagesSuffix is null)
            {
                __ImagesSuffix = new string[]
                {
                    "svgimages/xaf/action_navigation_history_back.svg",
                    "svgimages/xaf/action_navigation_history_forward.svg",
                    "svgimages/xaf/action_navigation_next_object.svg",
                    "svgimages/xaf/action_navigation_previous_object.svg"
                };
            }
            return Randomizer.GetItem(__ImagesSuffix);
        }
        /// <summary>
        /// Vrať náhodnou světlou barvu pro BackColor
        /// </summary>
        /// <returns></returns>
        protected virtual Color GetRandomBackColor()
        {
            if (__BackColors is null)
            {
                int h = 240;
                int l = 210;
                __BackColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__BackColors);
        }
        /// <summary>
        /// Vrať náhodnou tmavou barvu pro ForeColor
        /// </summary>
        /// <returns></returns>
        protected virtual Color GetRandomForeColor()
        {
            if (__ForeColors is null)
            {
                int h = 80;
                int l = 24;
                __ForeColors = new Color[]
                {
                    Color.FromArgb(h,h,h),
                    Color.FromArgb(h,h,l),
                    Color.FromArgb(h,l,h),
                    Color.FromArgb(l,h,h),
                    Color.FromArgb(l,h,l),
                    Color.FromArgb(l,l,h),
                    Color.FromArgb(h,l,l),
                    Color.FromArgb(l,l,l)
                };
            }
            return Randomizer.GetItem(__ForeColors);
        }
        protected virtual string GetRandomStyleName()
        {
            if (__StyleNames is null)
                __StyleNames = new string[]
                {   // Nebudu dávat všechny styly, jen vybrané:
                    AdapterSupport.StyleDefault,
                    AdapterSupport.StyleOK,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleWarning,
                    AdapterSupport.StyleImportant,
                    AdapterSupport.StyleNote,
                    AdapterSupport.StyleHeader1

                };
            return Randomizer.GetItem(__StyleNames);
        }
        /// <summary>
        /// Vrátí set požadovaných obrázků. Autoinicializační.
        /// </summary>
        /// <param name="imageSet"></param>
        /// <returns></returns>
        private string[] _GetMainImageNames(NodeImageSetType imageSet)
        {
            switch (imageSet)
            {
                case NodeImageSetType.Documents:
                    if (__ImagesDocuments is null)
                        __ImagesDocuments = new string[]
{
    "svgimages/reports/alignmentbottomcenter.svg",
    "svgimages/reports/alignmentbottomleft.svg",
    "svgimages/reports/alignmentbottomright.svg",
    "svgimages/reports/alignmentcentercenter.svg",
    "svgimages/reports/alignmentcenterleft.svg",
    "svgimages/reports/alignmentcenterright.svg",
    "svgimages/reports/alignmenttopcenter.svg",
    "svgimages/reports/alignmenttopleft.svg",
    "svgimages/reports/alignmenttopright.svg",
    "svgimages/richedit/alignbottomcenter.svg",
    "svgimages/richedit/alignbottomcenterrotated.svg",
    "svgimages/richedit/alignbottomleft.svg",
    "svgimages/richedit/alignbottomleftrotated.svg",
    "svgimages/richedit/alignbottomright.svg",
    "svgimages/richedit/alignbottomrightrotated.svg",
    "svgimages/richedit/alignfloatingobjectbottomcenter.svg",
    "svgimages/richedit/alignfloatingobjectbottomleft.svg",
    "svgimages/richedit/alignfloatingobjectbottomright.svg",
    "svgimages/richedit/alignfloatingobjectmiddlecenter.svg",
    "svgimages/richedit/alignfloatingobjectmiddleleft.svg",
    "svgimages/richedit/alignfloatingobjectmiddleright.svg",
    "svgimages/richedit/alignfloatingobjecttopcenter.svg",
    "svgimages/richedit/alignfloatingobjecttopleft.svg",
    "svgimages/richedit/alignfloatingobjecttopright.svg",
    "svgimages/richedit/alignmiddlecenter.svg",
    "svgimages/richedit/alignmiddlecenterrotated.svg",
    "svgimages/richedit/alignmiddleleft.svg",
    "svgimages/richedit/alignmiddleleftrotated.svg",
    "svgimages/richedit/alignmiddleright.svg",
    "svgimages/richedit/alignmiddlerightrotated.svg",
    "svgimages/richedit/alignright.svg",
    "svgimages/richedit/aligntopcenter.svg",
    "svgimages/richedit/aligntopcenterrotated.svg",
    "svgimages/richedit/aligntopleft.svg",
    "svgimages/richedit/aligntopleftrotated.svg",
    "svgimages/richedit/aligntopright.svg",
    "svgimages/richedit/aligntoprightrotated.svg",
    "svgimages/richedit/borderbottom.svg",
    "svgimages/richedit/borderinsidehorizontal.svg",
    "svgimages/richedit/borderinsidevertical.svg",
    "svgimages/richedit/borderleft.svg",
    "svgimages/richedit/bordernone.svg",
    "svgimages/richedit/borderright.svg",
    "svgimages/richedit/bordersall.svg",
    "svgimages/richedit/bordersandshading.svg",
    "svgimages/richedit/bordersbox.svg",
    "svgimages/richedit/borderscustom.svg",
    "svgimages/richedit/bordersgrid.svg",
    "svgimages/richedit/bordersinside.svg",
    "svgimages/richedit/bordersoutside.svg",
    "svgimages/richedit/bordertop.svg"
};
                    return __ImagesDocuments;
                case NodeImageSetType.Actions:
                    if (__ImagesActions is null)
                        __ImagesActions = new string[]
{
    "svgimages/icon%20builder/actions_add.svg",
    "svgimages/icon%20builder/actions_addcircled.svg",
    "svgimages/icon%20builder/actions_arrow1down.svg",
    "svgimages/icon%20builder/actions_arrow1left.svg",
    "svgimages/icon%20builder/actions_arrow1leftdown.svg",
    "svgimages/icon%20builder/actions_arrow1leftup.svg",
    "svgimages/icon%20builder/actions_arrow1right.svg",
    "svgimages/icon%20builder/actions_arrow1rightdown.svg",
    "svgimages/icon%20builder/actions_arrow1rightup.svg",
    "svgimages/icon%20builder/actions_arrow1up.svg",
    "svgimages/icon%20builder/actions_arrow2down.svg",
    "svgimages/icon%20builder/actions_arrow2left.svg",
    "svgimages/icon%20builder/actions_arrow2leftdown.svg",
    "svgimages/icon%20builder/actions_arrow2leftup.svg",
    "svgimages/icon%20builder/actions_arrow2right.svg",
    "svgimages/icon%20builder/actions_arrow2rightdown.svg",
    "svgimages/icon%20builder/actions_arrow2rightup.svg",
    "svgimages/icon%20builder/actions_arrow2up.svg",
    "svgimages/icon%20builder/actions_arrow3down.svg",
    "svgimages/icon%20builder/actions_arrow3left.svg",
    "svgimages/icon%20builder/actions_arrow3right.svg",
    "svgimages/icon%20builder/actions_arrow3up.svg",
    "svgimages/icon%20builder/actions_arrow4down.svg",
    "svgimages/icon%20builder/actions_arrow4left.svg",
    "svgimages/icon%20builder/actions_arrow4leftdown.svg",
    "svgimages/icon%20builder/actions_arrow4leftup.svg",
    "svgimages/icon%20builder/actions_arrow4right.svg",
    "svgimages/icon%20builder/actions_arrow4rightdown.svg",
    "svgimages/icon%20builder/actions_arrow4rightup.svg",
    "svgimages/icon%20builder/actions_arrow4up.svg",
    "svgimages/icon%20builder/actions_arrow5downleft.svg",
    "svgimages/icon%20builder/actions_arrow5downright.svg",
    "svgimages/icon%20builder/actions_arrow5leftdown.svg",
    "svgimages/icon%20builder/actions_arrow5leftup.svg",
    "svgimages/icon%20builder/actions_arrow5rightdown.svg",
    "svgimages/icon%20builder/actions_arrow5rightup.svg",
    "svgimages/icon%20builder/actions_arrow5upleft.svg",
    "svgimages/icon%20builder/actions_arrow5upright.svg"
};
                    return __ImagesActions;
                case NodeImageSetType.Formats:
                    if (__ImagesFormats is null)
                        __ImagesFormats = new string[]
{
    "svgimages/export/exporttocsv.svg",
    "svgimages/export/exporttodoc.svg",
    "svgimages/export/exporttodocx.svg",
    "svgimages/export/exporttoepub.svg",
    "svgimages/export/exporttohtml.svg",
    "svgimages/export/exporttoimg.svg",
    "svgimages/export/exporttomht.svg",
    "svgimages/export/exporttoodt.svg",
    "svgimages/export/exporttopdf.svg",
    "svgimages/export/exporttortf.svg",
    "svgimages/export/exporttotxt.svg",
    "svgimages/export/exporttoxls.svg",
    "svgimages/export/exporttoxlsx.svg",
    "svgimages/export/exporttoxml.svg",
    "svgimages/export/exporttoxps.svg"
};
                    return __ImagesFormats;
                case NodeImageSetType.Charts:
                    if (__ImagesCharts is null)
                        __ImagesCharts = new string[]
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
    "svgimages/chart/charttype_boxplot.svg",
    "svgimages/chart/charttype_bubble.svg",
    "svgimages/chart/charttype_bubble3d.svg",
    "svgimages/chart/charttype_candlestick.svg",
    "svgimages/chart/charttype_doughnut.svg",
    "svgimages/chart/charttype_doughnut3d.svg",
    "svgimages/chart/charttype_funnel.svg",
    "svgimages/chart/charttype_funnel3d.svg",
    "svgimages/chart/charttype_gantt.svg",
    "svgimages/chart/charttype_histogram.svg",
    "svgimages/chart/charttype_line.svg",
    "svgimages/chart/charttype_line3d.svg",
    "svgimages/chart/charttype_line3dstacked.svg",
    "svgimages/chart/charttype_line3dstacked100.svg",
    "svgimages/chart/charttype_linestacked.svg",
    "svgimages/chart/charttype_linestacked100.svg",
    "svgimages/chart/charttype_manhattanbar.svg",
    "svgimages/chart/charttype_nesteddoughnut.svg",
    "svgimages/chart/charttype_pareto.svg",
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
    "svgimages/chart/charttype_sunburst.svg",
    "svgimages/chart/charttype_swiftplot.svg",
    "svgimages/chart/charttype_waterfall.svg",
    "svgimages/chart/sankey.svg",
    "svgimages/chart/treemap.svg"
};
                    return __ImagesCharts;
                case NodeImageSetType.Spreadsheet:
                    if (__ImagesSpreadsheet is null)
                        __ImagesSpreadsheet = new string[]
                    {
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
    "svgimages/spreadsheet/createfullstackedarea3dchart.svg",
    "svgimages/spreadsheet/createfullstackedareachart.svg",
    "svgimages/spreadsheet/createfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createfullstackedbarchart.svg",
    "svgimages/spreadsheet/createfullstackedlinechart.svg",
    "svgimages/spreadsheet/createline3dchart.svg",
    "svgimages/spreadsheet/createlinechart.svg",
    "svgimages/spreadsheet/createmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpie3dchart.svg",
    "svgimages/spreadsheet/createpiechart.svg",
    "svgimages/spreadsheet/createpyramidbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidfullstackedbar3dchart.svg",
    "svgimages/spreadsheet/createpyramidmanhattanbarchart.svg",
    "svgimages/spreadsheet/createpyramidstackedbar3dchart.svg",
    "svgimages/spreadsheet/createradarlinechart.svg",
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
    "svgimages/spreadsheet/createstackedarea3dchart.svg",
    "svgimages/spreadsheet/createstackedareachart.svg",
    "svgimages/spreadsheet/createstackedbar3dchart.svg",
    "svgimages/spreadsheet/createstackedbarchart.svg",
    "svgimages/spreadsheet/createstackedlinechart.svg"
};
                    return __ImagesSpreadsheet;
            }
            return null;
        }
        private string[] __ImagesDocuments;
        private string[] __ImagesActions;
        private string[] __ImagesFormats;
        private string[] __ImagesCharts;
        private string[] __ImagesSpreadsheet;
        private string[] __ImagesSuffix;
        private Color[] __BackColors;
        private Color[] __ForeColors;
        private string[] __StyleNames;
        internal enum NodeImageSetType
        {
            None,
            Documents,
            Actions,
            Formats,
            Charts,
            Spreadsheet
        }
        protected NewNodePositionType __NewNodePosition = NewNodePositionType.None;
        protected enum NewNodePositionType { None, First, Last }
        #endregion
        #region Parametry v okně
        /// <summary>
        /// Vytvoří obsah panelu s parametry
        /// </summary>
        protected void ParamsInit()
        {
            FlowLayout flowLayout = new FlowLayout();
            OnParamsPrepare(flowLayout);
            ParamSplitContainer.Panel1.MinSize = (flowLayout.MaxY + 8);
        }
        protected virtual void OnParamsPrepare(FlowLayout flowLayout)
        {
            OnParamsPrepareTreeListProperties(flowLayout);
            OnParamsPrepareNodeProperties(flowLayout);
            OnParamsPrepareLog(flowLayout);
        }
        protected virtual void OnParamsPrepareTreeListProperties(FlowLayout flowLayout)
        {
            flowLayout.EndColumn(true);
        }
        protected virtual void OnParamsPrepareNodeProperties(FlowLayout flowLayout)
        {
            flowLayout.StartNewColumn(100, 200);
            CreateTitle(flowLayout, "Vlastnosti jednotlivých prvků");
            ComboNodeImageSet = CreateCombo(flowLayout, ControlActionType.ClearNodes, "NodeImageType", "Node images:", typeof(NodeImageSetType));
            ComboImagePosition = CreateCombo(flowLayout, ControlActionType.ClearNodes | ControlActionType.SettingsApply, "ImagePosition", "Image Position:", typeof(TreeImagePositionType));
            CheckUseExactStyle = CreateToggle(flowLayout, ControlActionType.ClearNodes, "UseExactStyle", "Use explicit styles", "Použít exaktně dané nastavení stylu", "Budou vepsány hodnoty jako FontStyle, FontSizeDelta, BackColor, ForeColor");
            CheckUseStyleName = CreateToggle(flowLayout, ControlActionType.ClearNodes, "UseStyleName", "Use Style Cup", "Použít styl daný kalíškem", "Bude vepsán StyleName, ten bude dohledán a aplikován");
            CheckUseCheckBoxes = CreateToggle(flowLayout, ControlActionType.ClearNodes, "UseCheckBoxes", "Use Check Boxes", "Použít pro některé koncové nody CheckBoxy", "Některé nody, které nemají podřízenou úroveň, budou zobrazeny jako CheckBox");
            CheckUseMultiColumns = CreateToggle(flowLayout, ControlActionType.ClearColumns, "UseMultiColumns", "Use Multi Columns", "Zobrazit více sloupců v TreeListu", "TreeList pak může připomínat BrowseGrid se stromem");
            CheckUseHtmlFormat = CreateToggle(flowLayout, ControlActionType.ClearColumns, "UseHtmlFormat", "Use HTML Format", "Použít HTMl formát pro formátování obsahu nodů", "Některé nody ve sloupci 3 ('Popisek') budou obsahovat HTML tagy a budou tak zobrazovány");
            CheckUseWordWrap = CreateToggle(flowLayout, ControlActionType.SettingsApply, "UseWordWrap", "Use Word Wrap", "Zalamovat text na více řádků", "Zaškrtnuto = dlouhé texty budou zobrazeny ve více řádcích pod sebou");

            flowLayout.CurrentY += 25;

            CreateTitle(flowLayout, "Vytvoření prvků stromu");
            CreateButton(flowLayout, 0, 120, 30, 0, "Vytvoř 15:1", NodeCreateClick151);
            CreateButton(flowLayout, 130, 120, 30, 38, "Vytvoř 25:2", NodeCreateClick252);
            CreateButton(flowLayout, 0, 120, 30, 0, "Vytvoř 40:3", NodeCreateClick403);
            CreateButton(flowLayout, 130, 120, 30, 38, "Vytvoř 60:4", NodeCreateClick604);
            CreateButton(flowLayout, 0, 250, 30, 38, "Smaž všechny prvky", NodeCreateClick000);
            flowLayout.EndColumn();
        }
        protected virtual void OnParamsPrepareLog(FlowLayout flowLayout)
        {
            flowLayout.StartNewColumn(100, 150);
            CreateTitle(flowLayout, "Logování");
            CheckLogToolTipChanges = CreateToggle(flowLayout, ControlActionType.None, "", "Log: ToolTipChange", "Logovat události ToolTipChange", "Zaškrtnuto: při pohybu myši se plní Log událostí.");
            CreateButton(flowLayout, 0, 200, 30, 38, "Smazat Log", LogClearBtnClick);

            flowLayout.CurrentY += 20;
            CreateButton(flowLayout, 0, 200, 30, 38, "Otisk nastavení do CLipboardu", OptionsDumpToClipbard);
            flowLayout.EndColumn();
        }
        protected DxTitleLabelControl CreateTitle(FlowLayout flowLayout, string text)
        {
            return DxComponent.CreateDxTitleLabel(flowLayout.CurrentX, ref flowLayout.CurrentY, flowLayout.TitleWidth, ParamsPanel, text, shiftY: true);
        }
        protected DxCheckEdit CreateToggle(FlowLayout flowLayout, ControlActionType actions, string controlInfo, string text, string toolTipTitle, string toolTipText, bool? allowGrayed = null)
        {
            var toggle = DxComponent.CreateDxCheckEdit(flowLayout.CurrentX, ref flowLayout.CurrentY, flowLayout.TitleWidth, ParamsPanel, text, ParamsChanged,
                DevExpress.XtraEditors.Controls.CheckBoxStyle.SvgToggle1, DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, null,
                toolTipTitle, toolTipText, allowGrayed: allowGrayed, shiftY: true);
            toggle.Tag = PackTag(actions, controlInfo);
            return toggle;
        }
        protected DxSpinEdit CreateSpinner(FlowLayout flowLayout, ControlActionType actions, string controlInfo, string label, int minValue, int maxValue, string toolTipTitle, string toolTipText)
        {
            DxComponent.CreateDxLabel(flowLayout.LabelLeft, flowLayout.CurrentY + 3, flowLayout.LabelWidth, ParamsPanel, label, LabelStyleType.Default, hAlignment: DevExpress.Utils.HorzAlignment.Far);

            var spinner = DxComponent.CreateDxSpinEdit(flowLayout.ComboLeft, ref flowLayout.CurrentY, 50, ParamsPanel, ParamsChanged,
                minValue, maxValue, mask: "####", spinStyles: DevExpress.XtraEditors.Controls.SpinStyles.Vertical,
                toolTipTitle: toolTipTitle, toolTipText: toolTipText, shiftY: true);
            spinner.Tag = PackTag(actions, controlInfo);
            return spinner;
        }
        protected DxImageComboBoxEdit CreateCombo(FlowLayout flowLayout, ControlActionType actions, string controlInfo, string label, Type enumType)
        {
            DxComponent.CreateDxLabel(flowLayout.LabelLeft, flowLayout.CurrentY + 3, flowLayout.LabelWidth, ParamsPanel, label, LabelStyleType.Default, hAlignment: DevExpress.Utils.HorzAlignment.Far);

            var combo = DxComponent.CreateDxImageComboBox(flowLayout.ComboLeft, ref flowLayout.CurrentY, flowLayout.ComboWidth, ParamsPanel, ParamsChanged, shiftY: true);
            combo.Tag = PackTag(actions, controlInfo);

            var enumName = enumType.Name + ".";
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);
            for (int n = 0; n < names.Length; n++)
            {
                var item = new DevExpress.XtraEditors.Controls.ImageComboBoxItem() { Description = enumName + names[n], Value = values.GetValue(n) };
                combo.Properties.Items.Add(item);
            }
            return combo;
        }
        protected DxSimpleButton CreateButton(FlowLayout flowLayout, int offsetX, int width, int height, int shiftY, string text, EventHandler click)
        {
            var button = DxComponent.CreateDxSimpleButton(flowLayout.CurrentX + flowLayout.LabelOffset + offsetX, flowLayout.CurrentY, width, height, ParamsPanel, text, click);
            if (shiftY > 0) flowLayout.CurrentY += shiftY;
            return button;
        }
        protected class FlowLayout
        {
            public FlowLayout()
            {
                TopY = 8;
                MaxY = 0;
                LabelOffset = 16;
                ComboSpace = 5;
                ColumnSpace = 20;
                CurrentY = TopY;
                CurrentX = 25;
            }
            /// <summary>
            /// Ukončí aktuální sloupec; příští začne nahoře a vpravo;
            /// Zahájí nový sloupec, s danou šířkou labelu a controlu
            /// </summary>
            /// <param name="labelWidth"></param>
            /// <param name="comboWidth"></param>
            public void StartNewColumn(int labelWidth = 110, int comboWidth = 220)
            {
                EndColumn(false);

                LabelWidth = labelWidth;
                ComboWidth = comboWidth;
            }
            /// <summary>
            /// Ukončí aktuální sloupec; příští začne nahoře a vpravo
            /// </summary>
            public void EndColumn(bool force = false)
            {
                if (CurrentY > MaxY) MaxY = CurrentY;

                bool isEmpty = (CurrentY <= TopY);
                if (!isEmpty || force)
                    CurrentX = NextX;
                CurrentY = TopY;
            }
            public int TopY;
            public int MaxY;
            public int CurrentX;
            public int CurrentY;
            public int ColumnSpace;
            public int LabelOffset;
            public int LabelWidth;
            public int ComboSpace;
            public int ComboWidth;

            public int TitleWidth { get { return LabelOffset + LabelWidth + ComboSpace + ComboWidth; } }
            public int LabelLeft { get { return CurrentX + LabelOffset; } }
            public int ComboLeft { get { return CurrentX + LabelOffset + LabelWidth + ComboSpace; } }
            public int NextX { get { return CurrentX + TitleWidth + ColumnSpace; } }
        }
        /// <summary>
        /// Co dělat?
        /// </summary>
        [Flags]
        protected enum ControlActionType
        {
            /// <summary>
            /// Netřeba ničehož
            /// </summary>
            None = 0,
            /// <summary>
            /// Odstranit nody
            /// </summary>
            ClearNodes = 0x0001,
            /// <summary>
            /// Odstranit sloupce
            /// </summary>
            ClearColumns = 0x0002,
            /// <summary>
            /// Aplikovat Settings do TreeListu
            /// </summary>
            SettingsApply = 0x0004
        }
        /// <summary>
        /// Zabalí data do Tagu
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="controlInfo"></param>
        /// <returns></returns>
        protected virtual object PackTag(ControlActionType actions, string controlInfo)
        {
            return new Tuple<ControlActionType, string>(actions, controlInfo);
        }
        /// <summary>
        /// Rozbalí data vložená do Tagu
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="actions"></param>
        /// <param name="controlInfo"></param>
        protected virtual void UnPackTag(object tag, out ControlActionType actions, out string controlInfo)
        {
            actions = ControlActionType.None;
            controlInfo = null;
            if (tag is Tuple<ControlActionType, string> tuple)
            {
                actions = tuple.Item1;
                controlInfo = tuple.Item2;
            }
        }
        /// <summary>
        /// Událost po změně parametru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ParamsChanged(object sender, EventArgs e)
        {
            if (!SettingsLoaded) return;
            var control = sender as Control;

            // Hodnoty z parametrů přenesu do properties Settings* :
            //  Protože na ně reaguje např. _ClearTreeList() => _CheckColumns() :
            SettingCollect();

            // Návaznosti z konkrétního controlu = logovat hodnotu parametru; nulovat obsah => nastavit sloupce:
            UnPackTag(control.Tag, out ControlActionType actions, out string controlInfo);
            if (!String.IsNullOrEmpty(controlInfo))
                AddToLogParamChange(control, controlInfo);
            ParamsRunActions(actions);
        }
        /// <summary>
        /// Provede akce specifikované v <paramref name="actions"/>
        /// </summary>
        /// <param name="actions"></param>
        protected virtual void ParamsRunActions(ControlActionType actions)
        {
            if (actions.HasFlag(ControlActionType.ClearNodes))
                ClearTreeList();
            if (actions.HasFlag(ControlActionType.ClearColumns))
                ClearTreeColumns();

            if (SettingsLoaded)
            {
                if (actions.HasFlag(ControlActionType.SettingsApply))
                {
                    SettingApply();
                }
                SettingSave();
            }
        }
        /// <summary>
        /// Do logu vepíše informaci o změně parametru
        /// </summary>
        /// <param name="control"></param>
        /// <param name="controlInfo"></param>
        protected virtual void AddToLogParamChange(Control control, string controlInfo)
        {
            string text = "";
            if (control is DxCheckEdit checkEdit)
            {
                if (checkEdit.Properties.AllowGrayed)
                    text = "; CheckState: " + checkEdit.CheckState.ToString();
                else
                    text = "; Checked: " + checkEdit.Checked.ToString();
            }
            else if (control is DxSpinEdit spinEdit)
            {
                text = "; Value: " + spinEdit.Value.ToString("###0");
            }
            else if (control is DxImageComboBoxEdit comboBox)
            {
                text = "; Selected: " + (comboBox.SelectedItem != null ? comboBox.SelectedItem?.ToString() : "NULL");
            }
            AddToLog($"Change Setting: {controlInfo}{text}");
        }
        /// <summary>
        /// Po kliknutí na tlačítko 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NodeCreateClick151(object sender, EventArgs e)
        {
            PrepareTreeList(15, 1);
        }
        /// <summary>
        /// Po kliknutí na tlačítko 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NodeCreateClick252(object sender, EventArgs e)
        {
            PrepareTreeList(25, 2);
        }
        /// <summary>
        /// Po kliknutí na tlačítko 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NodeCreateClick403(object sender, EventArgs e)
        {
            PrepareTreeList(40, 3);
        }
        /// <summary>
        /// Po kliknutí na tlačítko 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NodeCreateClick604(object sender, EventArgs e)
        {
            PrepareTreeList(60, 4);
        }
        /// <summary>
        /// Po kliknutí na tlačítko 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void NodeCreateClick000(object sender, EventArgs e)
        {
            PrepareTreeList(0, 0);
        }
        /// <summary>
        /// Po kliknutí na tlačítko Clear Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void LogClearBtnClick(object sender, EventArgs e)
        {
            LogClear();
        }
        protected virtual void OptionsDumpToClipbard(object sender, EventArgs e)
        {
            string text = this.DxTreeList.CreateOptionsDump();
            System.Windows.Forms.Clipboard.SetText(text);
        }
        protected DxImageComboBoxEdit ComboNodeImageSet;
        protected DxImageComboBoxEdit ComboImagePosition;
        protected DxCheckEdit CheckUseExactStyle;
        protected DxCheckEdit CheckUseStyleName;
        protected DxCheckEdit CheckUseCheckBoxes;
        protected DxCheckEdit CheckUseMultiColumns;
        protected DxCheckEdit CheckUseHtmlFormat;
        protected DxCheckEdit CheckUseWordWrap;

        protected DxCheckEdit CheckLogToolTipChanges;
        #endregion
        #region Settings: načtení/uložení do konfigurace; zobrazení/sesbírání z Checkboxů Params; aplikování do TreeListu
        /// <summary>
        /// Načte konfiguraci z Settings do properties, do TreeListu i do Parametrů
        /// </summary>
        protected void SettingLoad()
        {
            OnSettingLoad();

            SettingShow();
            SettingApply();

            // Teprve od tohoto místa se změny ukládají do Settings:
            SettingsLoaded = true;
        }
        protected virtual void OnSettingLoad()
        {
            // Do Properties   z DxComponent.Settings
            OnSettingLoadTreeListProperties();
            OnSettingLoadNodeProperties();
            OnSettingLoadLog();
        }
        protected virtual void OnSettingLoadTreeListProperties()
        {
        }
        protected virtual void OnSettingLoadNodeProperties()
        {
            SettingsNodeImageSet = ConvertToNodeImageSetType(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsNodeImageSet ", ""), NodeImageSetType.Documents);
            SettingsImagePosition = ConvertToImagePositionType(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsImagePosition", ""), TreeImagePositionType.None);
            SettingsUseExactStyle = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseExactStyle", ""));
            SettingsUseStyleName = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseStyleName", ""));
            SettingsUseCheckBoxes = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseCheckBoxes", ""));
            SettingsUseMultiColumns = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseMultiColumns", ""));
            SettingsUseHtmlFormat = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseHtmlFormat", ""));
            SettingsUseWordWrap = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SettingsUseWordWrap", ""));
        }
        protected virtual void OnSettingLoadLog()
        {
            SetingsLogToolTipChanges = ConvertToBool(DxComponent.Settings.GetRawValue(SettingsKey, "SetingsLogToolTipChanges", "N"));
        }
        /// <summary>
        /// Uloží konfiguraci z Properties do Settings
        /// </summary>
        protected virtual void SettingSave()
        {
            if (!SettingsLoaded) return;
            OnSettingSave();
        }
        protected virtual void OnSettingSave()
        {
            // Do DxComponent.Settings   z Properties
            OnSettingSaveTreeListProperties();
            OnSettingSaveNodeProperties();
            OnSettingSaveLog();
        }
        protected virtual void OnSettingSaveTreeListProperties()
        {
        }
        protected virtual void OnSettingSaveNodeProperties()
        {
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsNodeImageSet", ConvertToString(SettingsNodeImageSet));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsImagePosition", ConvertToString(SettingsImagePosition));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseExactStyle", ConvertToString(SettingsUseExactStyle));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseStyleName", ConvertToString(SettingsUseStyleName));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseCheckBoxes", ConvertToString(SettingsUseCheckBoxes));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseMultiColumns", ConvertToString(SettingsUseMultiColumns));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseHtmlFormat", ConvertToString(SettingsUseHtmlFormat));
            DxComponent.Settings.SetRawValue(SettingsKey, "SettingsUseWordWrap", ConvertToString(SettingsUseWordWrap));
        }
        protected virtual void OnSettingSaveLog()
        {
            DxComponent.Settings.SetRawValue(SettingsKey, "SetingsLogToolTipChanges", ConvertToString(SetingsLogToolTipChanges));
        }
        /// <summary>
        /// Main klíč v Settings pro zdejší proměnné
        /// </summary>
        protected const string SettingsKey = "DxTreeListValues";
        /// <summary>
        /// Hodnoty z properties vepíše do vizuálních parametrů (checkboxy), přitom potlačí eventy o změnách
        /// </summary>
        protected void SettingShow()
        {
            OnSettingShow();
        }
        protected virtual void OnSettingShow()
        {
            // Do vizuálních checkboxů   z Properties
            OnSettingShowTreeListProperties();
            OnSettingShowNodeProperties();
            OnSettingShowLog();
        }
        protected virtual void OnSettingShowTreeListProperties()
        {
        }
        protected virtual void OnSettingShowNodeProperties()
        {
            SelectComboItem(ComboNodeImageSet, SettingsNodeImageSet);
            SelectComboItem(ComboImagePosition, SettingsImagePosition);
            CheckUseExactStyle.Checked = SettingsUseExactStyle;
            CheckUseStyleName.Checked = SettingsUseStyleName;
            CheckUseCheckBoxes.Checked = SettingsUseCheckBoxes;
            CheckUseMultiColumns.Checked = SettingsUseMultiColumns;
            CheckUseHtmlFormat.Checked = SettingsUseHtmlFormat;
            CheckUseWordWrap.Checked = SettingsUseWordWrap;
        }
        protected virtual void OnSettingShowLog()
        {
            CheckLogToolTipChanges.Checked = SetingsLogToolTipChanges;
        }
        /// <summary>
        /// Hodnoty z vizuálních parametrů (checkboxy) opíše do properties, nic dalšího nedělá
        /// </summary>
        protected virtual void SettingCollect()
        {
            if (!SettingsLoaded) return;
            OnSettingCollect();
        }
        protected virtual void OnSettingCollect()
        {
            // Do datových Settings    z Controlů
            OnSettingCollectTreeListProperties();
            OnSettingCollectNodeProperties();
            OnSettingCollectLog();
        }
        protected virtual void OnSettingCollectTreeListProperties()
        {
        }
        protected virtual void OnSettingCollectNodeProperties()
        {
            SettingsNodeImageSet = ConvertToNodeImageSetType(ComboNodeImageSet, SettingsNodeImageSet);
            SettingsImagePosition = ConvertToImagePositionType(ComboImagePosition, SettingsImagePosition);
            SettingsUseExactStyle = CheckUseExactStyle.Checked;
            SettingsUseStyleName = CheckUseStyleName.Checked;
            SettingsUseCheckBoxes = CheckUseCheckBoxes.Checked;
            SettingsUseMultiColumns = CheckUseMultiColumns.Checked;
            SettingsUseHtmlFormat = CheckUseHtmlFormat.Checked;
            SettingsUseWordWrap = CheckUseWordWrap.Checked;
        }
        protected virtual void OnSettingCollectLog()
        {
            SetingsLogToolTipChanges = CheckLogToolTipChanges.Checked;
        }
        /// <summary>
        /// Hodnoty z konfigurace vepíše do TreeListu
        /// </summary>
        protected void SettingApply()
        {
            OnSettingApply();
        }
        protected virtual void OnSettingApply()
        {
            // Do TreeListu     z Properties
            OnSettingApplyTreeListProperties();
            OnSettingApplyNodeProperties();
            OnSettingApplyLog();
        }
        protected virtual void OnSettingApplyTreeListProperties()
        {
        }
        protected virtual void OnSettingApplyNodeProperties()
        {
            this.DxTreeList.ImagePositionType = SettingsImagePosition;
            this.DxTreeList.WordWrap = SettingsUseWordWrap;
        }
        protected virtual void OnSettingApplyLog()
        {
        }

        // Properties jsou uváděny v typech odpovídajících TreeListu.
        //  Konvertují se z/na string do Settings;
        //  Konvertují se z/na konkrétní typ do ovládacích Checkboxů a comboboxů atd do Params;


        internal NodeImageSetType SettingsNodeImageSet { get; set; }
        internal TreeImagePositionType SettingsImagePosition { get; set; }
        internal bool SettingsUseExactStyle { get; set; }
        internal bool SettingsUseStyleName { get; set; }
        internal bool SettingsUseCheckBoxes { get; set; }
        internal bool SettingsUseMultiColumns { get; set; }
        internal bool SettingsUseHtmlFormat { get; set; }
        internal bool SettingsUseWordWrap { get; set; }
        internal bool SetingsLogToolTipChanges { get; set; }

        protected bool SettingsLoaded;
        #endregion
        #region Konverze typů
        internal static bool ConvertToBool(string value)
        {
            return (String.Equals(value, "Y", StringComparison.InvariantCultureIgnoreCase));
        }
        internal static string ConvertToString(bool value)
        {
            return value ? "Y" : "N";
        }
        internal static bool ConvertToBool(bool? value)
        {
            return value ?? false;
        }
        internal static string ConvertToString(bool? value)
        {
            return (value.HasValue ? (value.Value ? "Y" : "N") : "");
        }
        internal static bool? ConvertToBoolN(CheckState checkState)
        {
            switch (checkState)
            {
                case CheckState.Checked: return true;
                case CheckState.Unchecked: return false;
                case CheckState.Indeterminate: return null;
            }
            return null;
        }
        internal static bool? ConvertToBoolN(string value)
        {
            if (String.Equals(value, "Y", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (String.Equals(value, "N", StringComparison.InvariantCultureIgnoreCase)) return false;
            return null;
        }

        internal static int ConvertToInt32(string value, int defValue = 0)
        {
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out var number)) return number;
            return defValue;
        }
        internal static string ConvertToString(int value)
        {
            return value.ToString();
        }

        internal static CheckState ConvertToCheckState(bool? value)
        {
            if (value.HasValue) return (value.Value ? CheckState.Checked : CheckState.Unchecked);
            return CheckState.Indeterminate;
        }
        internal static CheckState ConvertToCheckState(DevExpress.Utils.DefaultBoolean value)
        {
            switch (value)
            {
                case DevExpress.Utils.DefaultBoolean.Default: return CheckState.Indeterminate;
                case DevExpress.Utils.DefaultBoolean.False: return CheckState.Unchecked;
                case DevExpress.Utils.DefaultBoolean.True: return CheckState.Checked;
            }
            return CheckState.Indeterminate;
        }

        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(string value, DevExpress.Utils.DefaultBoolean defValue = DevExpress.Utils.DefaultBoolean.Default)
        {
            if (!String.IsNullOrEmpty(value)) 
            {
                switch (value)
                {
                    case "D": return DevExpress.Utils.DefaultBoolean.Default;
                    case "N": return DevExpress.Utils.DefaultBoolean.False;
                    case "Y": return DevExpress.Utils.DefaultBoolean.True;
                }
            }
            return defValue;
        }
        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(CheckState value)
        {
            switch (value)
            {
                case CheckState.Indeterminate: return DevExpress.Utils.DefaultBoolean.Default;
                case CheckState.Unchecked: return DevExpress.Utils.DefaultBoolean.False;
                case CheckState.Checked: return DevExpress.Utils.DefaultBoolean.True;
            }
            return DevExpress.Utils.DefaultBoolean.Default;
        }
        internal static DevExpress.Utils.DefaultBoolean ConvertToDefaultBoolean(bool? value)
        {
            if (value.HasValue) return (value.Value ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False);
            return DevExpress.Utils.DefaultBoolean.Default;
        }
        internal static string ConvertToString(DevExpress.Utils.DefaultBoolean value)
        {
            switch (value)
            {
                case DevExpress.Utils.DefaultBoolean.Default: return "D";
                case DevExpress.Utils.DefaultBoolean.False: return "N";
                case DevExpress.Utils.DefaultBoolean.True: return "Y";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.LineStyle ConvertToLineStyle(string value, DevExpress.XtraTreeList.LineStyle defValue = DevExpress.XtraTreeList.LineStyle.Percent50)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.LineStyle.None;
                    case "P": return DevExpress.XtraTreeList.LineStyle.Percent50;
                    case "D": return DevExpress.XtraTreeList.LineStyle.Dark;
                    case "L": return DevExpress.XtraTreeList.LineStyle.Light;
                    case "W": return DevExpress.XtraTreeList.LineStyle.Wide;
                    case "G": return DevExpress.XtraTreeList.LineStyle.Large;
                    case "S": return DevExpress.XtraTreeList.LineStyle.Solid;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.LineStyle ConvertToLineStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.LineStyle defValue = DevExpress.XtraTreeList.LineStyle.Percent50)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.LineStyle)
                        return (DevExpress.XtraTreeList.LineStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.LineStyle)
                {
                    return (DevExpress.XtraTreeList.LineStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.LineStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.LineStyle.None: return "N";
                case DevExpress.XtraTreeList.LineStyle.Percent50: return "P";
                case DevExpress.XtraTreeList.LineStyle.Dark: return "D";
                case DevExpress.XtraTreeList.LineStyle.Light: return "L";
                case DevExpress.XtraTreeList.LineStyle.Wide: return "W";
                case DevExpress.XtraTreeList.LineStyle.Large: return "G";
                case DevExpress.XtraTreeList.LineStyle.Solid: return "S";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle ConvertToNodeCheckBoxStyle(string value, DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle defValue = DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "D": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default;
                    case "C": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Check;
                    case "R": return DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Radio;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle ConvertToNodeCheckBoxStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle defValue = DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)
                        return (DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)
                {
                    return (DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Default: return "D";
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Check: return "C";
                case DevExpress.XtraTreeList.DefaultNodeCheckBoxStyle.Radio: return "R";
            }
            return "";
        }

        internal static RowFilterBoxMode ConvertToRowFilterBoxMode(string value, RowFilterBoxMode defValue = RowFilterBoxMode.None)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return RowFilterBoxMode.None;
                    case "C": return RowFilterBoxMode.Client;
                    case "S": return RowFilterBoxMode.Server;
                }
            }
            return defValue;
        }
        internal static RowFilterBoxMode ConvertToRowFilterBoxMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, RowFilterBoxMode defValue = RowFilterBoxMode.None)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is RowFilterBoxMode)
                        return (RowFilterBoxMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is RowFilterBoxMode)
                {
                    return (RowFilterBoxMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(RowFilterBoxMode value)
        {
            switch (value)
            {
                case RowFilterBoxMode.None: return "N";
                case RowFilterBoxMode.Client: return "C";
                case RowFilterBoxMode.Server: return "S";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.DrawFocusRectStyle ConvertToDrawFocusRectStyle(string value, DevExpress.XtraTreeList.DrawFocusRectStyle defValue = DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.DrawFocusRectStyle.None;
                    case "R": return DevExpress.XtraTreeList.DrawFocusRectStyle.RowFocus;
                    case "C": return DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus;
                    case "F": return DevExpress.XtraTreeList.DrawFocusRectStyle.RowFullFocus;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.DrawFocusRectStyle ConvertToDrawFocusRectStyle(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.DrawFocusRectStyle defValue = DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.DrawFocusRectStyle)
                        return (DevExpress.XtraTreeList.DrawFocusRectStyle)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.DrawFocusRectStyle)
                {
                    return (DevExpress.XtraTreeList.DrawFocusRectStyle)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.DrawFocusRectStyle value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.DrawFocusRectStyle.None: return "N";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.RowFocus: return "R";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.CellFocus: return "C";
                case DevExpress.XtraTreeList.DrawFocusRectStyle.RowFullFocus: return "F";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.TreeListEditingMode ConvertToTreeListEditingMode(string value, DevExpress.XtraTreeList.TreeListEditingMode defValue = DevExpress.XtraTreeList.TreeListEditingMode.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "D": return DevExpress.XtraTreeList.TreeListEditingMode.Default;
                    case "I": return DevExpress.XtraTreeList.TreeListEditingMode.Inplace;
                    case "F": return DevExpress.XtraTreeList.TreeListEditingMode.EditForm;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.TreeListEditingMode ConvertToTreeListEditingMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.TreeListEditingMode defValue = DevExpress.XtraTreeList.TreeListEditingMode.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.TreeListEditingMode)
                        return (DevExpress.XtraTreeList.TreeListEditingMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.TreeListEditingMode)
                {
                    return (DevExpress.XtraTreeList.TreeListEditingMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.TreeListEditingMode value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.TreeListEditingMode.Default: return "D";
                case DevExpress.XtraTreeList.TreeListEditingMode.Inplace: return "I";
                case DevExpress.XtraTreeList.TreeListEditingMode.EditForm: return "F";
            }
            return "";
        }

        internal static DevExpress.XtraTreeList.TreeListEditorShowMode ConvertToTreeListEditorShowMode(string value, DevExpress.XtraTreeList.TreeListEditorShowMode defValue = DevExpress.XtraTreeList.TreeListEditorShowMode.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return DevExpress.XtraTreeList.TreeListEditorShowMode.Default;
                    case "D": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDown;
                    case "U": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp;
                    case "C": return DevExpress.XtraTreeList.TreeListEditorShowMode.Click;
                    case "F": return DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDownFocused;
                    case "2": return DevExpress.XtraTreeList.TreeListEditorShowMode.DoubleClick;
                }
            }
            return defValue;
        }
        internal static DevExpress.XtraTreeList.TreeListEditorShowMode ConvertToTreeListEditingMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraTreeList.TreeListEditorShowMode defValue = DevExpress.XtraTreeList.TreeListEditorShowMode.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is DevExpress.XtraTreeList.TreeListEditorShowMode)
                        return (DevExpress.XtraTreeList.TreeListEditorShowMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is DevExpress.XtraTreeList.TreeListEditorShowMode)
                {
                    return (DevExpress.XtraTreeList.TreeListEditorShowMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(DevExpress.XtraTreeList.TreeListEditorShowMode value)
        {
            switch (value)
            {
                case DevExpress.XtraTreeList.TreeListEditorShowMode.Default: return "N";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDown: return "D";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseUp: return "U";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.Click: return "C";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.MouseDownFocused: return "F";
                case DevExpress.XtraTreeList.TreeListEditorShowMode.DoubleClick: return "2";
            }
            return "";
        }

        internal static TreeLevelLineType ConvertToLevelLineType(string value, TreeLevelLineType defValue = TreeLevelLineType.None)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return TreeLevelLineType.None;
                    case "P": return TreeLevelLineType.Percent50;
                    case "D": return TreeLevelLineType.Dark;
                    case "S": return TreeLevelLineType.Solid;
                }
            }
            return defValue;
        }
        internal static TreeLevelLineType ConvertToLevelLineType(DevExpress.XtraEditors.ComboBoxEdit comboBox, TreeLevelLineType defValue = TreeLevelLineType.None)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is TreeLevelLineType)
                        return (TreeLevelLineType)comboItem.Value;
                }
                if (comboBox.SelectedItem is TreeLevelLineType)
                {
                    return (TreeLevelLineType)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(TreeLevelLineType value)
        {
            switch (value)
            {
                case TreeLevelLineType.None: return "N";
                case TreeLevelLineType.Percent50: return "P";
                case TreeLevelLineType.Dark: return "D";
                case TreeLevelLineType.Solid: return "S";
            }
            return "";
        }

        internal static TreeCellLineType ConvertToCellLinesType(string value, TreeCellLineType defValue = TreeCellLineType.None)
        {   // Flags:
            if (value != null)
            {
                TreeCellLineType result = TreeCellLineType.None;
                if (value.Contains("H")) result |= TreeCellLineType.Horizontal;
                if (value.Contains("I")) result |= TreeCellLineType.VerticalInner;
                if (value.Contains("F")) result |= TreeCellLineType.VerticalFirst;
                return result;
            }
            return defValue;
        }
        internal static TreeCellLineType ConvertToCellLinesType(DevExpress.XtraEditors.ComboBoxEdit comboBox, TreeCellLineType defValue = TreeCellLineType.None)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is TreeCellLineType)
                        return (TreeCellLineType)comboItem.Value;
                }
                if (comboBox.SelectedItem is TreeLevelLineType)
                {
                    return (TreeCellLineType)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(TreeCellLineType value)
        {   // Flags:
            string result = "";
            if (value.HasFlag(TreeCellLineType.Horizontal)) result += "H";
            if (value.HasFlag(TreeCellLineType.VerticalInner)) result += "I";
            if (value.HasFlag(TreeCellLineType.VerticalFirst)) result += "F";
            return result;
        }

        internal static TreeEditorStartMode ConvertToEditorStartMode(string value, TreeEditorStartMode defValue = TreeEditorStartMode.Default)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return TreeEditorStartMode.Default;
                    case "D": return TreeEditorStartMode.MouseDown;
                    case "U": return TreeEditorStartMode.MouseUp;
                    case "C": return TreeEditorStartMode.Click;
                    case "F": return TreeEditorStartMode.MouseDownFocused;
                    case "B": return TreeEditorStartMode.DoubleClick;
                }
            }
            return defValue;
        }
        internal static TreeEditorStartMode ConvertToEditorStartMode(DevExpress.XtraEditors.ComboBoxEdit comboBox, TreeEditorStartMode defValue = TreeEditorStartMode.Default)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is TreeEditorStartMode)
                        return (TreeEditorStartMode)comboItem.Value;
                }
                if (comboBox.SelectedItem is TreeEditorStartMode)
                {
                    return (TreeEditorStartMode)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(TreeEditorStartMode value)
        {
            switch (value)
            {
                case TreeEditorStartMode.Default: return "N";
                case TreeEditorStartMode.MouseDown: return "D";
                case TreeEditorStartMode.MouseUp: return "U";
                case TreeEditorStartMode.Click: return "C";
                case TreeEditorStartMode.MouseDownFocused: return "F";
                case TreeEditorStartMode.DoubleClick: return "B";
            }
            return "";
        }

        internal static NodeImageSetType ConvertToNodeImageSetType(string value, NodeImageSetType defValue = NodeImageSetType.Documents)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return NodeImageSetType.None;
                    case "D": return NodeImageSetType.Documents;
                    case "A": return NodeImageSetType.Actions;
                    case "F": return NodeImageSetType.Formats;
                    case "C": return NodeImageSetType.Charts;
                    case "S": return NodeImageSetType.Spreadsheet;
                }
            }
            return defValue;
        }
        internal static NodeImageSetType ConvertToNodeImageSetType(DevExpress.XtraEditors.ComboBoxEdit comboBox, NodeImageSetType defValue = NodeImageSetType.Documents)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is NodeImageSetType)
                        return (NodeImageSetType)comboItem.Value;
                }
                if (comboBox.SelectedItem is NodeImageSetType)
                {
                    return (NodeImageSetType)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(NodeImageSetType value)
        {
            switch (value)
            {
                case NodeImageSetType.None: return "N";
                case NodeImageSetType.Documents: return "D";
                case NodeImageSetType.Actions: return "A";
                case NodeImageSetType.Formats: return "F";
                case NodeImageSetType.Charts: return "C";
                case NodeImageSetType.Spreadsheet: return "S";
            }
            return "";
        }

        internal static TreeImagePositionType ConvertToImagePositionType(string value, TreeImagePositionType defValue = TreeImagePositionType.None)
        {
            if (value != null)
            {
                switch (value)
                {
                    case "N": return TreeImagePositionType.None;
                    case "M": return TreeImagePositionType.MainIconOnly;
                    case "S": return TreeImagePositionType.SuffixAndMainIcon;
                    case "R": return TreeImagePositionType.MainAndSuffixIcon;
                }
            }
            return defValue;
        }
        internal static TreeImagePositionType ConvertToImagePositionType(DevExpress.XtraEditors.ComboBoxEdit comboBox, TreeImagePositionType defValue = TreeImagePositionType.None)
        {
            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (comboBox.SelectedItem is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem)
                {
                    if (comboItem.Value is TreeImagePositionType)
                        return (TreeImagePositionType)comboItem.Value;
                }
                if (comboBox.SelectedItem is TreeImagePositionType)
                {
                    return (TreeImagePositionType)comboBox.SelectedItem;
                }
            }
            return defValue;
        }
        internal static string ConvertToString(TreeImagePositionType value)
        {
            switch (value)
            {
                case TreeImagePositionType.None: return "N";
                case TreeImagePositionType.MainIconOnly: return "M";
                case TreeImagePositionType.SuffixAndMainIcon: return "S";
                case TreeImagePositionType.MainAndSuffixIcon: return "R";
            }
            return "";
        }

        internal static void SelectComboItem(DevExpress.XtraEditors.ImageComboBoxEdit comboBox, object value)
        {
            comboBox.SelectedItem = comboBox.Properties.Items.FirstOrDefault(i => isValidItem(i));

            bool isValidItem(object item)
            {
                if (item is DevExpress.XtraEditors.Controls.ImageComboBoxItem comboItem && Object.Equals(comboItem.Value, value)) return true;
                if (Object.Equals(item, value)) return true;
                return false;
            }
        }

        #endregion
        #region Log událostí
        protected void LogInit()
        {
            TreeListMemoEdit = DxComponent.CreateDxMemoEdit(0, 0, 100, 100, LogPanel, readOnly: true);
            TreeListMemoEdit.Dock = DockStyle.Fill;
            TreeListMemoEdit.MouseEnter += TreeListMemoEdit_MouseEnter;
        }
        protected void LogClear()
        {
            TreeListLogId = 0;
            TreeListLog = "";
            TreeListShowLogText();
        }

        protected void AddToLog(string actionName, DxTreeListNodeArgs args, bool showValue = false)
        {
            string value = (showValue ? $", Value change: '{args.EditedValueOld}' => '{args.EditedValueNew}'" : "");
            string column = (args.ColumnIndex.HasValue ? "; Column:" + args.ColumnIndex.Value.ToString() : "");
            AddToLog($"{actionName}: Node: {args.Node}{column}{value}");
        }
        protected void AddToLog(string actionName, DxTreeListNodesArgs args)
        {
            string nodes = args.Nodes.ToOneString("; ");
            AddToLog($"{actionName}: Nodes: {nodes}");
        }
        protected void AddToLog(string line, bool skipGUI = false)
        {
            int id = ++TreeListLogId;
            var now = DateTime.Now;
            bool isLong = (TreeListLogTime.HasValue && ((TimeSpan)(now - TreeListLogTime.Value)).TotalMilliseconds > 750d);
            string log = id.ToString() + ". " + line + Environment.NewLine + (isLong ? Environment.NewLine : "") + TreeListLog;
            TreeListLog = log;
            TreeListLogTime = now;
            if (skipGUI) TreeListPending = true;
            else TreeListShowLogText();
        }
        protected void TreeListShowLogText()
        {
            if (TreeListMemoEdit != null)
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(TreeListShowLogText));
                else
                {
                    TreeListMemoEdit.Text = TreeListLog;
                    TreeListPending = false;
                }
            }
        }
        protected void TreeListMemoEdit_MouseEnter(object sender, EventArgs e)
        {
            if (TreeListPending)
                TreeListShowLogText();
        }

        protected int TreeListLogId;
        protected string TreeListLog;
        protected DateTime? TreeListLogTime;
        protected bool TreeListPending;
        protected private DxMemoEdit TreeListMemoEdit;
        #endregion
    }
}
