// Supervisor: David Janáček, od 01.02.2021
// Part of Helios Nephrite, proprietary software, (c) Asseco Solutions, a. s.
// Redistribution and use in source and binary forms, with or without modification, 
// is not permitted without valid contract with Asseco Solutions, a. s.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using SWF = System.Windows.Forms;

namespace Noris.Clients.Win.Components.AsolDX
{
    #region DataFormPage + interface
    /// <summary>
    /// Data definující jednu stránku v DataFormu
    /// </summary>
    public class DataFormPage : IDataFormPage
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormPage()
        {
            Groups = new List<IDataFormGroup>();
        }
        /// <summary>
        /// ID stránky (záložky), jednoznačné v celém DataFormu
        /// </summary>
        public virtual string PageId { get; set; }
        /// <summary>
        /// Název obrázku stránky
        /// </summary>
        public virtual string PageImageName { get; set; }
        /// <summary>
        /// Titulek stránky
        /// </summary>
        public virtual string PageText { get; set; }
        /// <summary>
        /// Vnitřní okraje mezi okrajem stránky a souřadnicí grupy uvnitř
        /// </summary>
        public virtual SWF.Padding DesignPadding { get; set; }
        /// <summary>
        /// Stránka je aktivní? 
        /// Po iniciaci se přebírá do GUI, následně udržuje GUI.
        /// V jeden okamžik může být aktivních více stránek najednou, pokud je více stránek <see cref="IDataFormPage"/> mergováno do jedné záložky.
        /// </summary>
        public virtual bool Active { get; set; }
        /// <summary>
        /// Obsahuje true, pokud obsah této stránky je povoleno mergovat do předchozí stránky, pokud je dostatek prostoru.
        /// Stránky budou mergovány do vedle sebe stojících sloupců, každý bude mít nadpis své původní stránky.
        /// <para/>
        /// Aby byly stránky mergovány, musí mít tento příznak obě (nebo všechny).
        /// </summary>
        public virtual bool AllowMerge { get; set; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        public virtual List<IDataFormGroup> Groups { get; set; }

        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }
        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? PageText; } }
        IEnumerable<IDataFormGroup> IDataFormPage.Groups { get { return Groups; } }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jednu stránku v rámci DataFormu
    /// </summary>
    public interface IDataFormPage : IToolTipItem
    {
        /// <summary>
        /// ID stránky (záložky), jednoznačné v celém DataFormu
        /// </summary>
        string PageId { get; }
        /// <summary>
        /// Název obrázku stránky
        /// </summary>
        string PageImageName { get; }
        /// <summary>
        /// Titulek stránky
        /// </summary>
        string PageText { get; }
        /// <summary>
        /// Vnitřní okraje mezi okrajem stránky a souřadnicí grupy uvnitř
        /// </summary>
        SWF.Padding DesignPadding { get; }
        /// <summary>
        /// Stránka je aktivní? 
        /// Po iniciaci se přebírá do GUI, následně udržuje GUI.
        /// V jeden okamžik může být aktivních více stránek najednou, pokud je více stránek <see cref="IDataFormPage"/> mergováno do jedné záložky.
        /// </summary>
        bool Active { get; set; }
        /// <summary>
        /// Obsahuje true, pokud obsah této stránky je povoleno mergovat do předchozí stránky, pokud je dostatek prostoru.
        /// Stránky budou mergovány do vedle sebe stojících sloupců, každý bude mít nadpis své původní stránky.
        /// <para/>
        /// Aby byly stránky mergovány, musí mít tento příznak obě (nebo všechny).
        /// </summary>
        bool AllowMerge { get; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        IEnumerable<IDataFormGroup> Groups { get; }
    }
    #endregion
    #region DataFormGroup + interface
    /// <summary>
    /// Data definující jednu grupu v DataFormu
    /// </summary>
    public class DataFormGroup : IDataFormGroup
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormGroup()
        {
            IsVisible = true;
            CollapseMode = DataFormGroupCollapseMode.None;
            LayoutMode = DatFormGroupLayoutMode.None;
            Items = new List<IDataFormItem>();
        }
        /// <summary>
        /// ID grupy, jednoznačné v celém DataFormu
        /// </summary>
        public virtual string GroupId { get; set; }
        /// <summary>
        /// Název obrázku grupy
        /// </summary>
        public virtual string GroupImageName { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public virtual string GroupText { get; set; }
        /// <summary>
        /// Explicitně definovaná šířka grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/>.
        /// <para/>
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormItemType.Label"/>, včetně zadané velikosti a vzhledu.
        /// Pokud součástí grupy má být podtitulek a/nebo linka, musí být i to uvedeno v Items.
        /// </summary>
        public virtual int? DesignWidth { get; set; }
        /// <summary>
        /// Explicitně definovaná výška grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/>.
        /// <para/>
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormItemType.Label"/>, včetně zadané velikosti a vzhledu.
        /// Pokud součástí grupy má být podtitulek a/nebo linka, musí být i to uvedeno v Items.
        /// </summary>
        public virtual int? DesignHeight { get; set; }
        /// <summary>
        /// Okraje uvnitř grupy.
        /// Hodnota <see cref="SWF.Padding.Left"/> a <see cref="SWF.Padding.Top"/> určují posun souřadného systému prvků <see cref="Items"/> oproti počátku grupy.
        /// Hodnoty <see cref="SWF.Padding.Right"/> a <see cref="SWF.Padding.Bottom"/> se použijí tehdy, když velikost grupy není dána explicitně 
        /// a bude se dopočítávat podle souhrnu rozměrů <see cref="Items"/>, pak se k nejkrajnější souřadnici prvku přičte pravý a dolní Padding.
        /// </summary>
        public virtual SWF.Padding DesignPadding { get; set; }
        /// <summary>
        /// Rozsah orámování grupy.
        /// Grupa má své vnější souřadnice, dané <see cref="DesignWidth"/> a <see cref="DesignHeight"/>.
        /// Vnitřní prvky mohou být odsazené o <see cref="DesignPadding"/>. V rámci tohoto Paddingu může být vykreslen Border grupy.
        /// Border pak začíná např. zleva (tj. svislá linie) na souřadnici X = <see cref="DesignBorderRange"/>.Begin a končí na souřadnici X = <see cref="DesignBorderRange"/>.End.
        /// Jinými slovy, Border se nachází uvnitř grupy, od vnějšího okraje grupy je odsazen vždy o <see cref="DesignBorderRange"/>.Begin, a síla linky je <see cref="DesignBorderRange"/>.Size.
        /// Při použití tohoto Border dbejme o to, aby <see cref="DesignPadding"/> byl větší než Border, jinak by prvky <see cref="Items"/> mohly překrývat Border.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// Border pak může sloužit pro určení odsazení titulkového prostoru.
        /// <para/>
        /// Titulkový prostor grupy se nachází uvnitř Borderu.
        /// <para/>
        /// Pokud <see cref="DesignBorderRange"/> je null, bere se jako { 0, 0 }.
        /// </summary>
        public virtual Int32Range DesignBorderRange { get; set; }
        /// <summary>
        /// Způsob barev a stylu orámování okraje
        /// </summary>
        public virtual IDataFormBackgroundAppearance BorderAppearance { get; set; }
        /// <summary>
        /// Výška záhlaví (v designových pixelech)
        /// </summary>
        public virtual int? DesignHeaderHeight { get; set; }
        /// <summary>
        /// Způsob barev a stylu záhlaví (prostor nahoře uvnitř borderu, s výškou <see cref="DesignHeaderHeight"/>
        /// </summary>
        public virtual IDataFormBackgroundAppearance HeaderAppearance { get; set; }
        /// <summary>
        /// Řídí viditelnost grupy
        /// </summary>
        public virtual bool IsVisible { get; set; }
        /// <summary>
        /// Pravidla pro sbalení/rozbalení grupy
        /// </summary>
        public virtual DataFormGroupCollapseMode CollapseMode { get; set; }
        /// <summary>
        /// Grupa je sbalena? GUI bude tuto hodnotu nastavovat
        /// </summary>
        public virtual bool Collapsed { get; set; }
        /// <summary>
        /// Obsahuje příznaky pro skládání dynamického layoutu stránky pro tuto grupu.
        /// Grupa může definovat standardní chování = povinný layout v jednom sloupci, nebo může deklarovat povinné nebo volitelné zalomení před nebo za touto grupou.
        /// Algoritmus pak zalomí obsah stránky (=grupy) tak, aby optimálně využil dostupnou šířku prostoru, a do něj vložil všechny grupy tak, aby byly rozloženy rovnoměrně.
        /// </summary>
        public virtual DatFormGroupLayoutMode LayoutMode { get; set; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        public virtual IDataFormItemAppearance Appearance { get; set; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        public virtual List<IDataFormItem> Items { get; set; }

        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }

        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? GroupText; } }
        IEnumerable<IDataFormItem> IDataFormGroup.Items { get { return Items; } }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jednu grupu v rámci DataFormu
    /// </summary>
    public interface IDataFormGroup : IToolTipItem
    {
        /// <summary>
        /// ID grupy, jednoznačné v celém DataFormu
        /// </summary>
        string GroupId { get; }
        /// <summary>
        /// Název obrázku grupy
        /// </summary>
        string GroupImageName { get; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        string GroupText { get; }
        /// <summary>
        /// Explicitně definovaná šířka grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/>.
        /// <para/>
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormItemType.Label"/>, včetně zadané velikosti a vzhledu.
        /// Pokud součástí grupy má být podtitulek a/nebo linka, musí být i to uvedeno v Items.
        /// </summary>
        int? DesignWidth { get; }
        /// <summary>
        /// Explicitně definovaná výška grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/>.
        /// <para/>
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormItemType.Label"/>, včetně zadané velikosti a vzhledu.
        /// Pokud součástí grupy má být podtitulek a/nebo linka, musí být i to uvedeno v Items.
        /// </summary>
        int? DesignHeight { get; }
        /// <summary>
        /// Okraje uvnitř grupy.
        /// Hodnota <see cref="SWF.Padding.Left"/> a <see cref="SWF.Padding.Top"/> určují posun souřadného systému prvků <see cref="Items"/> oproti počátku grupy.
        /// Hodnoty <see cref="SWF.Padding.Right"/> a <see cref="SWF.Padding.Bottom"/> se použijí tehdy, když velikost grupy není dána explicitně 
        /// a bude se dopočítávat podle souhrnu rozměrů <see cref="Items"/>, pak se k nejkrajnější souřadnici prvku přičte pravý a dolní Padding.
        /// </summary>
        SWF.Padding DesignPadding { get; }
        /// <summary>
        /// Rozsah orámování grupy.
        /// Grupa má své vnější souřadnice, dané <see cref="DesignWidth"/> a <see cref="DesignHeight"/>.
        /// Vnitřní prvky mohou být odsazené o <see cref="DesignPadding"/>. V rámci tohoto Paddingu může být vykreslen Border grupy.
        /// Border pak začíná např. zleva (tj. svislá linie) na souřadnici X = <see cref="DesignBorderRange"/>.Begin a končí na souřadnici X = <see cref="DesignBorderRange"/>.End.
        /// Jinými slovy, Border se nachází uvnitř grupy, od vnějšího okraje grupy je odsazen vždy o <see cref="DesignBorderRange"/>.Begin, a síla linky je <see cref="DesignBorderRange"/>.Size.
        /// Při použití tohoto Border dbejme o to, aby <see cref="DesignPadding"/> byl větší než Border, jinak by prvky <see cref="Items"/> mohly překrývat Border.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// Border pak může sloužit pro určení odsazení titulkového prostoru.
        /// <para/>
        /// Titulkový prostor grupy se nachází uvnitř Borderu.
        /// <para/>
        /// Pokud <see cref="DesignBorderRange"/> je null, bere se jako { 0, 0 }.
        /// </summary>
        Int32Range DesignBorderRange { get; }
        /// <summary>
        /// Způsob barev a stylu orámování okraje
        /// </summary>
        IDataFormBackgroundAppearance BorderAppearance { get; }
        /// <summary>
        /// Výška záhlaví (v designových pixelech)
        /// </summary>
        int? DesignHeaderHeight { get; }
        /// <summary>
        /// Způsob barev a stylu záhlaví (prostor nahoře uvnitř borderu, s výškou <see cref="DesignHeaderHeight"/>
        /// </summary>
        IDataFormBackgroundAppearance HeaderAppearance { get; }
        /// <summary>
        /// Řídí viditelnost grupy
        /// </summary>
        bool IsVisible { get; }
        /// <summary>
        /// Pravidla pro sbalení/rozbalení grupy
        /// </summary>
        DataFormGroupCollapseMode CollapseMode { get; }
        /// <summary>
        /// Grupa je sbalena? GUI bude tuto hodnotu nastavovat
        /// </summary>
        bool Collapsed { get; set; }
        /// <summary>
        /// Obsahuje příznaky pro skládání dynamického layoutu stránky pro tuto grupu.
        /// Grupa může definovat standardní chování = povinný layout v jednom sloupci, nebo může deklarovat povinné nebo volitelné zalomení před nebo za touto grupou.
        /// Algoritmus pak zalomí obsah stránky (=grupy) tak, aby optimálně využil dostupnou šířku prostoru, a do něj vložil všechny grupy tak, aby byly rozloženy rovnoměrně.
        /// </summary>
        DatFormGroupLayoutMode LayoutMode { get; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        IDataFormItemAppearance Appearance { get; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        IEnumerable<IDataFormItem> Items { get; }
    }
    #endregion
    #region DataFormItem + interface
    /// <summary>
    /// Data definující jeden prvek v DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public class DataFormItemMenuText : DataFormItemImageText, IDataFormItemMenuText
    {
        /// <summary>
        /// Soupis položek v nabídce
        /// </summary>
        public virtual IEnumerable<IMenuItem> MenuItems { get; set; }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public interface IDataFormItemMenuText : IDataFormItemImageText
    {
        /// <summary>
        /// Soupis položek v nabídce
        /// </summary>
        IEnumerable<IMenuItem> MenuItems { get; }
    }
    /// <summary>
    /// Data definující jeden prvek v DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public class DataFormItemCheckBox : DataFormItemImageText, IDataFormItemCheckBox
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        public virtual bool Checked { get; set; }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public interface IDataFormItemCheckBox : IDataFormItemImageText
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        bool Checked { get; }
    }

    public class DataFormItemTextBoxButton : DataFormItemImageText, IDataFormItemTextBoxButton
    {
        /// <summary>
        /// Pokud je true, pak buttony jsou vidět stále. Pokud je false, pak jsou vidět jen pod myší anebo s focusem (default).
        /// </summary>
        public virtual bool ButtonsVisibleAllways { get; set; }
        /// <summary>
        /// Vzhled buttonu je (true) = 3D / (false) = plochý (default)
        /// </summary>
        public virtual bool ButtonAs3D { get; set; }
        /// <summary>
        /// Druh buttonu
        /// </summary>
        public virtual DataFormButtonKind ButtonKind { get; set; }
        /// <summary>
        /// Ikona do obrázku typu <see cref="DataFormButtonKind.Glyph"/>
        /// </summary>
        public virtual string ButtonGlyphImageName { get; set; }
    }
    public interface IDataFormItemTextBoxButton : IDataFormItemImageText
    {
        /// <summary>
        /// Pokud je true, pak buttony jsou vidět stále. Pokud je false, pak jsou vidět jen pod myší anebo s focusem (default).
        /// </summary>
        bool ButtonsVisibleAllways { get; }
        /// <summary>
        /// Vzhled buttonu je (true) = 3D / (false) = plochý (default)
        /// </summary>
        bool ButtonAs3D { get; }
        /// <summary>
        /// Druh buttonu
        /// </summary>
        DataFormButtonKind ButtonKind { get; }
        /// <summary>
        /// Ikona do obrázku typu <see cref="DataFormButtonKind.Glyph"/>
        /// </summary>
        string ButtonGlyphImageName { get; }
    }

    /// <summary>
    /// Data definující jeden prvek v DataFormu, který má Text a Ikonu
    /// </summary>
    public class DataFormItemImageText : DataFormItem, IDataFormItemImageText
    {
        /// <summary>
        /// Jméno ikony
        /// </summary>
        public virtual string ImageName { get; set; }
        /// <summary>
        /// Text
        /// </summary>
        public virtual string Text { get; set; }
        string IToolTipItem.ToolTipTitle { get { return ToolTipTitle ?? Text; } }

    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu
    /// </summary>
    public interface IDataFormItemImageText : IDataFormItem
    {
        /// <summary>
        /// Jméno ikony
        /// </summary>
        string ImageName { get; }
        /// <summary>
        /// Text
        /// </summary>
        string Text { get; }
    }
    /// <summary>
    /// Data definující jeden prvek v DataFormu
    /// </summary>
    public class DataFormItem : IDataFormItem
    {
        #region Static factory
        /// <summary>
        /// Vrací konkrétního potomka <see cref="DataFormItem"/> pro požadovaný typ prvku
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static DataFormItem CreateItem(DataFormItemType itemType)
        {
            switch (itemType)
            {
                case DataFormItemType.None: return null;
                case DataFormItemType.Label: return new DataFormItemImageText() { ItemType = itemType };
                case DataFormItemType.TextBox: return null;
                case DataFormItemType.TextBoxButton: return new DataFormItemTextBoxButton() { ItemType = itemType };
                case DataFormItemType.EditBox: return null;
                case DataFormItemType.SpinnerBox: return null;
                case DataFormItemType.CheckBox: return new DataFormItemCheckBox() { ItemType = itemType };
                case DataFormItemType.BreadCrumb: return null;
                case DataFormItemType.ComboBoxList: return null;
                case DataFormItemType.ComboBoxEdit: return null;
                case DataFormItemType.TokenEdit: return null;
                case DataFormItemType.ListView: return null;
                case DataFormItemType.TreeView: return null;
                case DataFormItemType.RadioButtonBox: return null;
                case DataFormItemType.Button: return new DataFormItemImageText() { ItemType = itemType };
                case DataFormItemType.CheckButton: return new DataFormItemCheckBox() { ItemType = itemType };
                case DataFormItemType.DropDownButton: return null;
                case DataFormItemType.BarCode: return null;
                case DataFormItemType.Image: return null;
            }
            return null;
        }
        #endregion
        #region Základní prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormItem()
        {
            IsVisible = true;
            Indicators = DataFormItemIndicatorType.None;
        }
        /// <summary>
        /// ID prvku, jednoznačné v celém DataFormu
        /// </summary>
        public virtual string ItemId { get; set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public virtual DataFormItemType ItemType { get; set; }
        /// <summary>
        /// Umístění prvku v rámci grupy = relativně v grupě, ne na stránce!
        /// V designových pixelech 96DPI, kde výška standardního textboxu je 20 px a výška labelu je 18 px
        /// </summary>
        public virtual Rectangle DesignBounds { get; set; }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        public virtual bool IsVisible { get; set; }
        /// <summary>
        /// Řízení barevných indikátorů u prvku
        /// </summary>
        public virtual DataFormItemIndicatorType Indicators { get; set; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        public virtual IDataFormItemAppearance Appearance { get; set; }

        /// <summary>
        /// Text ToolTipu
        /// </summary>
        public virtual string ToolTipText { get; set; }
        /// <summary>
        /// Titulek ToolTipu. Pokud nebude naplněn, vezme se text prvku.
        /// </summary>
        public virtual string ToolTipTitle { get; set; }
        /// <summary>
        /// Ikona ToolTipu
        /// </summary>
        public virtual string ToolTipIcon { get; set; }
        #endregion
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu
    /// </summary>
    public interface IDataFormItem : IToolTipItem
    {
        /// <summary>
        /// ID prvku, jednoznačné v celém DataFormu
        /// </summary>
        string ItemId { get; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        DataFormItemType ItemType { get; }
        /// <summary>
        /// Umístění prvku v rámci grupy = relativně v grupě, ne na stránce!
        /// V designových pixelech 96DPI, kde výška standardního textboxu je 20 px a výška labelu je 18 px
        /// </summary>
        Rectangle DesignBounds { get; }
        /// <summary>
        /// Prvek je viditelný?
        /// </summary>
        bool IsVisible { get; }
        /// <summary>
        /// Řízení barevných indikátorů u prvku
        /// </summary>
        DataFormItemIndicatorType Indicators { get; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        IDataFormItemAppearance Appearance { get; }
    }
    #endregion
    #region Podpůrné třídy a interface : DataFormAppearance, DataFormBackgroundAppearance
    /// <summary>
    /// Modifikace vzhledu prvku
    /// </summary>
    public class DataFormItemAppearance : IDataFormItemAppearance
    {
        /// <summary>
        /// Změna velikosti písma
        /// </summary>
        public virtual int? FontSizeDelta { get; set; }
        /// <summary>
        /// Změna stylu písma - Bold
        /// </summary>
        public virtual bool? FontStyleBold { get; set; }
        /// <summary>
        /// Změna stylu písma - Italic
        /// </summary>
        public virtual bool? FontStyleItalic { get; set; }
        /// <summary>
        /// Změna stylu písma - Underline
        /// </summary>
        public virtual bool? FontStyleUnderline { get; set; }
        /// <summary>
        /// Změna stylu písma - StrikeOut
        /// </summary>
        public virtual bool? FontStyleStrikeOut { get; set; }
        /// <summary>
        /// Zarovnání obsahu
        /// </summary>
        public virtual ContentAlignment? ContentAlignment { get; set; }
        /// <summary>
        /// Změna barvy pozadí
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Změna barvy popředí = písma
        /// </summary>
        public virtual Color? ForeColor { get; set; }
    }
    /// <summary>
    /// Modifikace vzhledu prvku
    /// </summary>
    public interface IDataFormItemAppearance
    {
        /// <summary>
        /// Změna velikosti písma
        /// </summary>
        int? FontSizeDelta { get; }
        /// <summary>
        /// Změna stylu písma - Bold
        /// </summary>
        bool? FontStyleBold { get; }
        /// <summary>
        /// Změna stylu písma - Italic
        /// </summary>
        bool? FontStyleItalic { get; }
        /// <summary>
        /// Změna stylu písma - Underline
        /// </summary>
        bool? FontStyleUnderline { get; }
        /// <summary>
        /// Změna stylu písma - StrikeOut
        /// </summary>
        bool? FontStyleStrikeOut { get; }
        /// <summary>
        /// Zarovnání obsahu
        /// </summary>
        ContentAlignment? ContentAlignment { get; }
        /// <summary>
        /// Změna barvy pozadí
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Změna barvy popředí = písma
        /// </summary>
        Color? ForeColor { get; }
    }
    /// <summary>
    /// Definice vzhledu pozadí
    /// </summary>
    public class DataFormBackgroundAppearance : IDataFormBackgroundAppearance
    {
        /// <summary>
        /// Směr gradientu
        /// </summary>
        public virtual GradientStyleType? GradientStyle { get; set; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        public virtual Color? BackColor { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        public virtual Color? BackColorEnd { get; set; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual Color? OnMouseBackColor { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual Color? OnMouseBackColorEnd { get; set; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual Color? FocusedBackColor { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual Color? FocusedBackColorEnd { get; set; }
    }
    /// <summary>
    /// Definice vzhledu pozadí
    /// </summary>
    public interface IDataFormBackgroundAppearance
    {
        /// <summary>
        /// Směr gradientu
        /// </summary>
        GradientStyleType? GradientStyle { get; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        Color? BackColor { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        Color? BackColorEnd { get; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        Color? OnMouseBackColor { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        Color? OnMouseBackColorEnd { get; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        Color? FocusedBackColor { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        Color? FocusedBackColorEnd { get; }
    }
    #endregion
    #region Enumy
    /// <summary>
    /// Jak má být grupa rozbalovací
    /// </summary>
    [Flags]
    public enum DataFormGroupCollapseMode
    {
        /// <summary>
        /// Grupa je vždy plně viditelná a není možno ji sbalit
        /// </summary>
        None = 0,
        /// <summary>
        /// Grupa umožňuje sbalení bez dalších podmínek
        /// </summary>
        AllowCollapseAllways = 0x0010
    }
    /// <summary>
    /// Jak se má grupa chovat při tvorbě layoutu stránky
    /// </summary>
    [Flags]
    public enum DatFormGroupLayoutMode
    {
        /// <summary>
        /// Tato grupa musí být umístěna pod grupu předchozí.
        /// </summary>
        None = 0,
        /// <summary>
        /// Tato grupa smí být umístěna doprava do nového sloupce, pokud to bude layoutu vyhovovat
        /// </summary>
        AllowBreakToNewColumn = 0x0001,
        /// <summary>
        /// Tato grupa musí být umístěna doprava do nového sloupce, i když by se nevešla do prostoru (zobrazí se pak vodorovný scrollbar)
        /// </summary>
        ForceBreakToNewColumn = 0x0002
    }
    /// <summary>
    /// Řízení barevných indikátorů u prvku
    /// </summary>
    [Flags]
    public enum DataFormItemIndicatorType
    {
        /// <summary>
        /// Prvek nemá orámování nikdy
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.OnMouseIndicatorColor"/>, v tenkém provedení
        /// </summary>
        MouseOverThin = 0x0001,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.OnMouseIndicatorColor"/>, v silném provedení
        /// </summary>
        MouseOverBold = 0x0002,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WithFocusIndicatorColor"/>, v tenkém provedení
        /// </summary>
        WithFocusThin = 0x0004,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WithFocusIndicatorColor"/>, v silném provedení
        /// </summary>
        WithFocusBold = 0x0008,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        CorrectOnDemand = 0x0010,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        CorrectAllways = 0x0020,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        WarningOnDemand = 0x0100,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        WarningAllways = 0x0200,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        ErrorOnDemand = 0x1000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        ErrorAllways = 0x2000
    }
    /// <summary>
    /// Druh prvku v DataFormu
    /// </summary>
    public enum DataFormItemType
    {
        /// <summary>
        /// Žádný prvek
        /// </summary>
        None = 0,
        /// <summary>
        /// Label
        /// </summary>
        Label,
        /// <summary>
        /// TextBox = jednořádkový text s různým formátováním vstupu
        /// </summary>
        TextBox,
        /// <summary>
        /// TextBox s buttonem vpravo = jednořádkový text s různým formátováním vstupu a s tlačítkem
        /// </summary>
        TextBoxButton,
        /// <summary>
        /// EditBox = víceřádkový text s volitelným formátováním (WordPad, Html, Syntax color)
        /// </summary>
        EditBox,
        /// <summary>
        /// SpinnerBox
        /// </summary>
        SpinnerBox,
        /// <summary>
        /// CheckBox
        /// </summary>
        CheckBox,
        /// <summary>
        /// BreadCrumb
        /// </summary>
        BreadCrumb,
        /// <summary>
        /// ComboBoxList
        /// </summary>
        ComboBoxList,
        /// <summary>
        /// ComboBoxEdit
        /// </summary>
        ComboBoxEdit,
        /// <summary>
        /// TokenEdit = jednořádkový text s možností výběru více položek z předvoleb (typicky pro mailové adresy s výběrem z adresáře)
        /// </summary>
        TokenEdit,
        /// <summary>
        /// ListView
        /// </summary>
        ListView,
        /// <summary>
        /// TreeView
        /// </summary>
        TreeView,
        /// <summary>
        /// RadioButtonBox
        /// </summary>
        RadioButtonBox,
        /// <summary>
        /// Button = běžný button
        /// </summary>
        Button,
        /// <summary>
        /// CheckButton = button s možností stavu Checked
        /// </summary>
        CheckButton,
        /// <summary>
        /// DropDownButton = button s možností nabídek podfunkcí
        /// </summary>
        DropDownButton,
        /// <summary>
        /// Jakýkoli 1D i 2D kód (EAN, UPC, QR, DataMatrix)
        /// </summary>
        BarCode,
        /// <summary>
        /// Image
        /// </summary>
        Image
    }
    /// <summary>
    /// Druh předdefinovaného buttonu
    /// </summary>
    public enum DataFormButtonKind : int
    {
        /// <summary>
        /// Žádný button
        /// </summary>
        None = 0,
        /// <summary>
        /// A Close symbol is displayed on the button's surface.
        /// </summary>
        Close = -10,
        /// <summary>
        /// A right-arrow for a spin editor is displayed on the button's surface.
        /// </summary>
        SpinRight = -9,
        /// <summary>
        /// A left-arrow for a spin editor is displayed on the button's surface.
        /// </summary>
        SpinLeft = -8,
        /// <summary>
        /// A down-arrow for a spin editor is displayed on the button's surface.
        /// </summary>
        SpinDown = -7,
        /// <summary>
        /// An up-arrow for a spin editor is displayed on the button's surface.
        /// </summary>
        SpinUp = -6,
        /// <summary>
        /// A Down-arrow for a combo box is drawn on the button's surface.
        /// </summary>
        Combo = -5,
        /// <summary>
        /// A Right-arrow is drawn the button's surface.
        /// </summary>
        Right = -4,
        /// <summary>
        /// A Left-arrow symbol is drawn on the button's surface.
        /// </summary>
        Left = -3,
        /// <summary>
        /// An Up-arrow is drawn on the button's surface.
        /// </summary>
        Up = -2,
        /// <summary>
        /// A Down-arrow is drawn on the button's surface.
        /// </summary>
        Down = -1,
        /// <summary>
        /// An Ellipsis symbol is drawn on the button's surface.
        /// </summary>
        Ellipsis = 1,
        /// <summary>
        /// A Delete symbol is drawn on the button's surface.
        /// </summary>
        Delete = 2,
        /// <summary>
        /// An OK sign is drawn on the button's surface.
        /// </summary>
        OK = 3,
        /// <summary>
        /// A Plus sign is drawn on the button's surface.
        /// </summary>
        Plus = 4,
        /// <summary>
        /// A Minus sign is drawn on the button's surface.
        /// </summary>
        Minus = 5,
        /// <summary>
        /// A Redo symbol is drawn on the button's surface.
        /// </summary>
        Redo = 6,
        /// <summary>
        /// An Undo symbol is drawn on the button's surface.
        /// </summary>
        Undo = 7,
        /// <summary>
        /// A Down-arrow is drawn on the button's surface. Unlike, the Down button, this kind of button allows text to be displayed next to the down-arrow.
        /// </summary>
        DropDown = 8,
        /// <summary>
        /// A Search symbol is drawn on the button's surface.
        /// </summary>
        Search = 9,
        /// <summary>
        /// A Clear symbol is drawn on the button's surface.
        /// </summary>
        Clear = 10,
        /// <summary>
        /// A Separator.
        /// </summary>
        Separator = 11,
        /// <summary>
        /// Uživatelem dodaná bitmapa, v 
        /// A custom bitmap is drawn on the button's surface.
        /// </summary>
        Glyph = 99

    }
    #endregion
}
