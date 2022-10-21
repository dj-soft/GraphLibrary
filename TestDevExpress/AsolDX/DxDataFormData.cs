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

namespace Noris.Clients.Win.Components.AsolDX.DataForm
{
    #region DataForm + interface
    /// <summary>
    /// Data definující jeden celý Dataform
    /// </summary>
    public class DataForm : IDataForm
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataForm()
        {
            Pages = new List<IDataFormPage>();
        }
        /// <summary>
        /// ID dataformu
        /// </summary>
        public virtual string FormId { get; set; }
        /// <summary>
        /// Styl pozadí tohoto dataformu
        /// </summary>
        public virtual IDataFormBackgroundAppearance BackgroundAppearance { get; set; }
        /// <summary>
        /// Jednotlivé stránky dataformu
        /// </summary>
        public virtual List<IDataFormPage> Pages { get; set; }
        /// <summary>prvek interface</summary>
        IEnumerable<IDataFormPage> IDataForm.Pages { get { return this.Pages; } }
    }
    /// <summary>
    /// Data definující jeden celý Dataform
    /// </summary>
    public interface IDataForm
    {
        /// <summary>
        /// ID dataformu
        /// </summary>
        string FormId { get; }
        /// <summary>
        /// Styl pozadí tohoto dataformu
        /// </summary>
        IDataFormBackgroundAppearance BackgroundAppearance { get; }
        /// <summary>
        /// Jednotlivé stránky dataformu
        /// </summary>
        IEnumerable<IDataFormPage> Pages { get; }
    }
    #endregion
    #region DataFormPage + interface
    /// <summary>
    /// Data definující jednu stránku (záložku) v DataFormu
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
        /// Styl pozadí této stránky
        /// </summary>
        public virtual IDataFormBackgroundAppearance BackgroundAppearance { get; set; }
        /// <summary>
        /// Jednotlivé grupy na záložce
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
        /// <summary>prvek interface</summary>
        IEnumerable<IDataFormGroup> IDataFormPage.Groups { get { return Groups; } }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jednu stránku (záložku) v rámci DataFormu
    /// </summary>
    public interface IDataFormPage : IToolTipItem
    {
        /// <summary>
        /// ID stránky (záložky), jednoznačné v celém DataFormu
        /// </summary>
        string PageId { get; }
        /// <summary>
        /// Název obrázku stránky. Zobrazuje se jako ikona v oušku stránky před textem.
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
        /// Styl pozadí této stránky
        /// </summary>
        IDataFormBackgroundAppearance BackgroundAppearance { get; }
        /// <summary>
        /// Jednotlivé grupy na záložce
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
            Items = new List<IDataFormColumn>();
        }
        /// <summary>
        /// ID grupy, jednoznačné v celém DataFormu
        /// </summary>
        public virtual string GroupId { get; set; }
        /// <summary>
        /// Titulek grupy, pokud bude null pak grupa nemá titulek
        /// </summary>
        public virtual IDataFormGroupTitle GroupTitle { get; set; }
        /// <summary>
        /// Explicitně definovaná šířka grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou šířku jako součet rozměru obsahu plus vodorovné okraje <see cref="DesignPadding"/> + border <see cref="DesignBorderRange"/>.
        /// </summary>
        public virtual int? DesignWidth { get; set; }
        /// <summary>
        /// Explicitně definovaná výška grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/> + border <see cref="DesignBorderRange"/>.
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
        /// Grupa má své vnější souřadnice, dané velikostí <see cref="DesignWidth"/> a <see cref="DesignHeight"/>.
        /// Uvnitř tohoto prostoru se nachází Border, a uvnitř Borderu je potom Padding. Uvnitř Paddingu je prostor pro prvky <see cref="Items"/>, v tomto prostoru jsou zadávané souřadnice prvků.
        /// <para/>
        /// <u>Konkrétněji o souřadném systému:</u><br/>
        /// Mějme grupu s celkovou designovou šířkou 500px.<br/>
        /// Mějme deklaraci <see cref="DesignBorderRange"/> : { Begin = 4, Size = 2 };<br/>
        /// Mějme hodnotu <see cref="DesignPadding"/> : { All = 3 };<br/>
        /// Pak tedy zleva v prostoru grupy budou nejdříve 4px prázdné (<see cref="DesignBorderRange"/>.Begin);<br/>
        /// Následovat budou 2px vykresleného Borderu (<see cref="DesignBorderRange"/>.Size);<br/>
        /// Pak budou 3px volného prostoru (<see cref="DesignPadding"/>.Left);<br/>
        /// Celkem tedy levý okraj = (4 + 2 + 3) = 9px;<br/>
        /// A pokud některý Item má souřadnici X = 0, pak bude reálně umístěn na souřadnici v grupě X = 9.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. 
        /// Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// <para/>
        /// Titulkový prostor grupy <see cref="GroupTitle"/> se nachází uvnitř Borderu, jeho text je odsazen o <see cref="DesignPadding"/> od Borderu.
        /// <para/>
        /// Pokud <see cref="DesignBorderRange"/> je null, bere se jako { 0, 0 }.
        /// </summary>
        public virtual Int32Range DesignBorderRange { get; set; }
        /// <summary>
        /// Okraje uvnitř grupy.
        /// Hodnota <see cref="SWF.Padding.Left"/> a <see cref="SWF.Padding.Top"/> určují posun souřadného systému prvků <see cref="Items"/> oproti počátku grupy.
        /// Hodnoty <see cref="SWF.Padding.Right"/> a <see cref="SWF.Padding.Bottom"/> se použijí tehdy, když velikost grupy není dána explicitně 
        /// a bude se dopočítávat podle souhrnu rozměrů <see cref="Items"/>, pak se k nejkrajnější souřadnici prvku přičte pravý a dolní Padding.
        /// <para/>
        /// <u>Konkrétněji o souřadném systému:</u><br/>
        /// Mějme grupu s celkovou designovou šířkou 500px.<br/>
        /// Mějme deklaraci <see cref="DesignBorderRange"/> : { Begin = 4, Size = 2 };<br/>
        /// Mějme hodnotu <see cref="DesignPadding"/> : { All = 3 };<br/>
        /// Pak tedy zleva v prostoru grupy budou nejdříve 4px prázdné (<see cref="DesignBorderRange"/>.Begin);<br/>
        /// Následovat budou 2px vykresleného Borderu (<see cref="DesignBorderRange"/>.Size);<br/>
        /// Pak budou 3px volného prostoru (<see cref="DesignPadding"/>.Left);<br/>
        /// Celkem tedy levý okraj = (4 + 2 + 3) = 9px;<br/>
        /// A pokud některý Item má souřadnici X = 0, pak bude reálně umístěn na souřadnici v grupě X = 9.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. 
        /// Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// <para/>
        /// Titulkový prostor grupy <see cref="GroupTitle"/> se nachází uvnitř Borderu, jeho text je odsazen o <see cref="DesignPadding"/> od Borderu.
        /// </summary>
        public virtual IDataFormBackgroundAppearance BorderAppearance { get; set; }
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
        /// Styl pozadí této grupy
        /// </summary>
        public virtual IDataFormBackgroundAppearance BackgroundAppearance { get; set; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        public virtual List<IDataFormColumn> Items { get; set; }
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

        /// <summary>prvek interface</summary>
        IEnumerable<IDataFormColumn> IDataFormGroup.Items { get { return Items; } }
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
        /// Titulek grupy, pokud bude null pak grupa nemá titulek
        /// </summary>
        IDataFormGroupTitle GroupTitle { get; }
        /// <summary>
        /// Explicitně definovaná šířka grupy (designová hodnota: Zoom 100% a 96DPI).
        /// Může být null, pak se určí podle souhrnu rozměrů <see cref="Items"/> plus <see cref="DesignPadding"/>.
        /// <para/>
        /// Designer tedy může určit explicitně jen šířku grupy (nastaví hodnotu do <see cref="DesignWidth"/>), 
        /// a ponechá výšku grupy <see cref="DesignHeight"/> = null, 
        /// pak systém určí designovou výšku jako součet rozměru obsahu plus svislé okraje <see cref="DesignPadding"/>.
        /// <para/>
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormColumnType.Label"/>, včetně zadané velikosti a vzhledu.
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
        /// Pokud grupa má implementovat titulek, pak titulek bude jednou z položek grupy, typu <see cref="DataFormColumnType.Label"/>, včetně zadané velikosti a vzhledu.
        /// Pokud součástí grupy má být podtitulek a/nebo linka, musí být i to uvedeno v Items.
        /// </summary>
        int? DesignHeight { get; }
        /// <summary>
        /// Rozsah orámování grupy.
        /// Grupa má své vnější souřadnice, dané velikostí <see cref="DesignWidth"/> a <see cref="DesignHeight"/>.
        /// Uvnitř tohoto prostoru se nachází Border, a uvnitř Borderu je potom Padding. Uvnitř Paddingu je prostor pro prvky <see cref="Items"/>, v tomto prostoru jsou zadávané souřadnice prvků.
        /// <para/>
        /// <u>Konkrétněji o souřadném systému:</u><br/>
        /// Mějme grupu s celkovou designovou šířkou 500px.<br/>
        /// Mějme deklaraci <see cref="DesignBorderRange"/> : { Begin = 4, Size = 2 };<br/>
        /// Mějme hodnotu <see cref="DesignPadding"/> : { All = 3 };<br/>
        /// Pak tedy zleva v prostoru grupy budou nejdříve 4px prázdné (<see cref="DesignBorderRange"/>.Begin);<br/>
        /// Následovat budou 2px vykresleného Borderu (<see cref="DesignBorderRange"/>.Size);<br/>
        /// Pak budou 3px volného prostoru (<see cref="DesignPadding"/>.Left);<br/>
        /// Celkem tedy levý okraj = (4 + 2 + 3) = 9px;<br/>
        /// A pokud některý Item má souřadnici X = 0, pak bude reálně umístěn na souřadnici v grupě X = 9.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. 
        /// Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// <para/>
        /// Titulkový prostor grupy <see cref="GroupTitle"/> se nachází uvnitř Borderu, jeho text je odsazen o <see cref="DesignPadding"/> od Borderu.
        /// <para/>
        /// Pokud <see cref="DesignBorderRange"/> je null, bere se jako { 0, 0 }.
        /// </summary>
        Int32Range DesignBorderRange { get; }
        /// <summary>
        /// Okraje uvnitř grupy.
        /// Hodnota <see cref="SWF.Padding.Left"/> a <see cref="SWF.Padding.Top"/> určují posun souřadného systému prvků <see cref="Items"/> oproti počátku grupy.
        /// Hodnoty <see cref="SWF.Padding.Right"/> a <see cref="SWF.Padding.Bottom"/> se použijí tehdy, když velikost grupy není dána explicitně 
        /// a bude se dopočítávat podle souhrnu rozměrů <see cref="Items"/>, pak se k nejkrajnější souřadnici prvku přičte pravý a dolní Padding.
        /// <para/>
        /// <u>Konkrétněji o souřadném systému:</u><br/>
        /// Mějme grupu s celkovou designovou šířkou 500px.<br/>
        /// Mějme deklaraci <see cref="DesignBorderRange"/> : { Begin = 4, Size = 2 };<br/>
        /// Mějme hodnotu <see cref="DesignPadding"/> : { All = 3 };<br/>
        /// Pak tedy zleva v prostoru grupy budou nejdříve 4px prázdné (<see cref="DesignBorderRange"/>.Begin);<br/>
        /// Následovat budou 2px vykresleného Borderu (<see cref="DesignBorderRange"/>.Size);<br/>
        /// Pak budou 3px volného prostoru (<see cref="DesignPadding"/>.Left);<br/>
        /// Celkem tedy levý okraj = (4 + 2 + 3) = 9px;<br/>
        /// A pokud některý Item má souřadnici X = 0, pak bude reálně umístěn na souřadnici v grupě X = 9.
        /// <para/>
        /// Border je vykreslen jednoduchou barvou <see cref="BorderAppearance"/>. 
        /// Ta může pracovat s Alpha kanálem (průhlednost). Pokud <see cref="BorderAppearance"/> je null, nebude se kreslit.
        /// <para/>
        /// Titulkový prostor grupy <see cref="GroupTitle"/> se nachází uvnitř Borderu, jeho text je odsazen o <see cref="DesignPadding"/> od Borderu.
        /// </summary>
        SWF.Padding DesignPadding { get; }
        /// <summary>
        /// Způsob barev a stylu orámování okraje
        /// </summary>
        IDataFormBackgroundAppearance BorderAppearance { get; }
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
        /// Styl pozadí této grupy
        /// </summary>
        IDataFormBackgroundAppearance BackgroundAppearance { get; }
        /// <summary>
        /// Jednotlivé prvky grupy
        /// </summary>
        IEnumerable<IDataFormColumn> Items { get; }
    }
    /// <summary>
    /// Deklarace titulku grupy
    /// </summary>
    public class DataFormGroupTitle : IDataFormGroupTitle
    {
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public virtual string TitleText { get; set; }
        /// <summary>
        /// Okraje použité pro zarovnání textu titulku uvnitř grupy.
        /// </summary>
        public virtual SWF.Padding DesignTitlePadding { get; set; }
        /// <summary>
        /// Umístění textu <see cref="TitleText"/> v prostoru záhlaví. 
        /// Prostor je standardně zmenšen o Padding deklarovaný v <see cref="IDataFormGroup.DesignPadding"/>, tak je možno zajistit, že text titulku se nebude dotýkat okraje grupy.
        /// </summary>
        public virtual ContentAlignment TitleAlignment { get; set; }
        /// <summary>
        /// Výška záhlaví (v designových pixelech). Záhlaví je umístěno na vnitřním horním okraji grupy po celé její šířce.
        /// Uvnitř této výšky může být zobrazena linka, její pozice je definována v 
        /// Teprve pod touto výškou začíná prostor pro Columny.
        /// </summary>
        public virtual int? DesignTitleHeight { get; set; }
        /// <summary>
        /// Souřadnice prostoru barevné linky na ose Y, která reprezentuje podtržení nebo podbarvení titulku.
        /// Hodnota <b>Begin</b> reprezentuje souřadnici Y, kde linka začíná, měřeno od počátku titulku nahoře, 
        /// hodnota <b>Size</b> reprezentuje výšku linky.
        /// Pokud je zde null, pak se linka nevykresluje.
        /// Může zde být rozsah 0 až <see cref="DesignTitleHeight"/>, pak je podkreslen celý prostor titulku.
        /// </summary>
        public virtual Int32Range DesignLineRange { get; set; }
        /// <summary>
        /// Řídí viditelnost titulku grupy
        /// </summary>
        public virtual bool IsVisible { get; set; }
        /// <summary>
        /// Vzhled písma titulku - kalíšek, barvy, modifikace fontu. 
        /// Určuje vzhled titulkového textu.
        /// </summary>
        public virtual IDataFormColumnAppearance TitleAppearance { get; set; }
        /// <summary>
        /// Způsob barev a stylu pozadí titulkového řádku. Pozadí pokrývá celou výšku <see cref="DesignTitleHeight"/>.
        /// Teprve na toto pozadí je vykreslena linka v souřadnicích <see cref="DesignLineRange"/> se vzhledem <see cref="LineAppearance"/>.
        /// </summary>
        public virtual IDataFormBackgroundAppearance BackgroundAppearance { get; set; }
        /// <summary>
        /// Způsob barev a stylu linky titulkového řádku (prostor je umístěn ve svislých souřadnicích <see cref="DesignLineRange"/>.
        /// </summary>
        public virtual IDataFormBackgroundAppearance LineAppearance { get; set; }
    }
    /// <summary>
    /// Deklarace titulku grupy
    /// </summary>
    public interface IDataFormGroupTitle
    {
        /// <summary>
        /// Titulek grupy
        /// </summary>
        string TitleText { get; }
        /// <summary>
        /// Okraje použité pro zarovnání textu titulku uvnitř grupy.
        /// </summary>
        SWF.Padding DesignTitlePadding { get; }
        /// <summary>
        /// Umístění textu <see cref="TitleText"/> v prostoru záhlaví. 
        /// Prostor je standardně zmenšen o Padding deklarovaný v <see cref="IDataFormGroup.DesignPadding"/>, tak je možno zajistit, že text titulku se nebude dotýkat okraje grupy.
        /// </summary>
        ContentAlignment TitleAlignment { get; }
        /// <summary>
        /// Výška záhlaví (v designových pixelech). Záhlaví je umístěno na vnitřním horním okraji grupy po celé její šířce.
        /// Uvnitř této výšky může být zobrazena linka, její pozice je definována v 
        /// Teprve pod touto výškou začíná prostor pro Columny.
        /// </summary>
        int? DesignTitleHeight { get; }
        /// <summary>
        /// Souřadnice prostoru barevné linky na ose Y, která reprezentuje podtržení nebo podbarvení titulku.
        /// Hodnota <b>Begin</b> reprezentuje souřadnici Y, kde linka začíná, měřeno od počátku titulku nahoře, 
        /// hodnota <b>Size</b> reprezentuje výšku linky.
        /// Pokud je zde null, pak se linka nevykresluje.
        /// Může zde být rozsah 0 až <see cref="DesignTitleHeight"/>, pak je podkreslen celý prostor titulku.
        /// </summary>
        Int32Range DesignLineRange { get; }
        /// <summary>
        /// Řídí viditelnost titulku grupy
        /// </summary>
        bool IsVisible { get; }
        /// <summary>
        /// Vzhled písma titulku - kalíšek, barvy, modifikace fontu. 
        /// Určuje vzhled titulkového textu.
        /// </summary>
        IDataFormColumnAppearance TitleAppearance { get; }
        /// <summary>
        /// Způsob barev a stylu pozadí titulkového řádku. Pozadí pokrývá celou výšku <see cref="DesignTitleHeight"/>.
        /// Teprve na toto pozadí je vykreslena linka v souřadnicích <see cref="DesignLineRange"/> se vzhledem <see cref="LineAppearance"/>.
        /// </summary>
        IDataFormBackgroundAppearance BackgroundAppearance { get; }
        /// <summary>
        /// Způsob barev a stylu linky titulkového řádku (prostor je umístěn ve svislých souřadnicích <see cref="DesignLineRange"/>.
        /// </summary>
        IDataFormBackgroundAppearance LineAppearance { get; }
    }
    #endregion
    #region DataFormColumn + interface (různé třídy pro různé typy)
    /// <summary>
    /// Data definující jeden prvek v DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public class DataFormColumnMenuText : DataFormColumnImageText, IDataFormColumnMenuText
    {
        /// <summary>
        /// Soupis položek v nabídce
        /// </summary>
        public virtual IEnumerable<IMenuItem> MenuItems { get; set; }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public interface IDataFormColumnMenuText : IDataFormColumnImageText
    {
        /// <summary>
        /// Soupis položek v nabídce
        /// </summary>
        IEnumerable<IMenuItem> MenuItems { get; }
    }
    /// <summary>
    /// Data definující jeden prvek v DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public class DataFormColumnCheckBox : DataFormColumnImageText, IDataFormColumnCheckBox
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        public virtual bool Checked { get; set; }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public interface IDataFormColumnCheckBox : IDataFormColumnImageText
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        bool Checked { get; }
    }
    /// <summary>
    /// Definice jednoho buttonu
    /// </summary>
    public class DataFormColumnTextBoxButton : DataFormColumnImageText, IDataFormColumnTextBoxButton
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
    /// <summary>
    /// Definice jednoho buttonu
    /// </summary>
    public interface IDataFormColumnTextBoxButton : IDataFormColumnImageText
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
    public class DataFormColumnImageText : DataFormColumn, IDataFormColumnImageText
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
    public interface IDataFormColumnImageText : IDataFormColumn
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
    public class DataFormColumn : IDataFormColumn
    {
        #region Static factory
        /// <summary>
        /// Vrací konkrétního potomka <see cref="DataFormColumn"/> pro požadovaný typ prvku
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static DataFormColumn CreateItem(DataFormColumnType itemType)
        {
            switch (itemType)
            {
                case DataFormColumnType.None: return null;
                case DataFormColumnType.Label: return new DataFormColumnImageText() { ColumnType = itemType };
                case DataFormColumnType.TextBox: return null;
                case DataFormColumnType.TextBoxButton: return new DataFormColumnTextBoxButton() { ColumnType = itemType };
                case DataFormColumnType.EditBox: return null;
                case DataFormColumnType.SpinnerBox: return null;
                case DataFormColumnType.CheckBox: return new DataFormColumnCheckBox() { ColumnType = itemType };
                case DataFormColumnType.BreadCrumb: return null;
                case DataFormColumnType.ComboBoxList: return null;
                case DataFormColumnType.ComboBoxEdit: return null;
                case DataFormColumnType.TokenEdit: return null;
                case DataFormColumnType.ListView: return null;
                case DataFormColumnType.TreeView: return null;
                case DataFormColumnType.RadioButtonBox: return null;
                case DataFormColumnType.Button: return new DataFormColumnImageText() { ColumnType = itemType };
                case DataFormColumnType.CheckButton: return new DataFormColumnCheckBox() { ColumnType = itemType };
                case DataFormColumnType.DropDownButton: return null;
                case DataFormColumnType.BarCode: return null;
                case DataFormColumnType.Image: return null;
            }
            return null;
        }
        #endregion
        #region Základní prvky
        /// <summary>
        /// Konstruktor
        /// </summary>
        public DataFormColumn()
        {
            IsVisible = true;
            Indicators = DataFormColumnIndicatorType.None;
        }
        /// <summary>
        /// ID prvku, jednoznačné v celém DataFormu
        /// </summary>
        public virtual string ColumnId { get; set; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        public virtual DataFormColumnType ColumnType { get; set; }
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
        public virtual DataFormColumnIndicatorType Indicators { get; set; }
        /// <summary>
        /// Explicitní barva indikátoru (podsvícení prvku)
        /// </summary>
        public virtual Color? IndicatorColor { get; set; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        public virtual IDataFormColumnAppearance Appearance { get; set; }
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
    public interface IDataFormColumn : IToolTipItem
    {
        /// <summary>
        /// ID prvku, jednoznačné v celém DataFormu
        /// </summary>
        string ColumnId { get; }
        /// <summary>
        /// Typ prvku
        /// </summary>
        DataFormColumnType ColumnType { get; }
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
        DataFormColumnIndicatorType Indicators { get; }
        /// <summary>
        /// Explicitní barva indikátoru (podsvícení prvku).
        /// Její podrobnější řízení je dáno v <see cref="Indicators"/>, hodnoty např. <see cref="DataFormColumnIndicatorType.IndicatorColorAllwaysBold"/> a další.
        /// <para/>
        /// Pokud bude zadána barva <see cref="IndicatorColor"/>, ale nebude zadána patřičná předvolba v <see cref="Indicators"/>, pak se zadaná barva neuplatní,
        /// ale neuplatní se ani ostatní předvolby v <see cref="Indicators"/>!
        /// </summary>
        Color? IndicatorColor { get; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        IDataFormColumnAppearance Appearance { get; }
    }
    #endregion
    #region Podpůrné třídy a interface : DataFormColumnAppearance, DataFormBackgroundAppearance
    /// <summary>
    /// Modifikace vzhledu prvku
    /// </summary>
    public class DataFormColumnAppearance : IDataFormColumnAppearance
    {
        /// <summary>
        /// Název stylu (=kalíšek ve smyslu Nephrite).
        /// Pokud je zadán, pak je použit jako druhý zdroj hodnot (prvním zdrojem jsou jednotlivé property tohoto objektu).
        /// </summary>
        public virtual string StyleName { get; set; }
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
    public interface IDataFormColumnAppearance
    {
        /// <summary>
        /// Název stylu (=kalíšek ve smyslu Nephrite).
        /// Pokud je zadán, pak je použit jako druhý zdroj hodnot (prvním zdrojem jsou jednotlivé property tohoto objektu).
        /// </summary>
        string StyleName { get; }
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
        /// Název stylu (=kalíšek ve smyslu Nephrite).
        /// Pokud je zadán, pak je použit jako druhý zdroj hodnot (prvním zdrojem jsou jednotlivé property tohoto objektu).
        /// </summary>
        public virtual string StyleName { get; set; }
        /// <summary>
        /// Název obrázku použitého na pozadí
        /// </summary>
        public virtual string BackImageName { get; set; }
        /// <summary>
        /// Umístění obrázku použitého na pozadí
        /// </summary>
        public virtual BackImageAlignmentMode BackImageAlignment { get; set; }
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
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        public virtual string BackColorName { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        public virtual Color? BackColorEnd { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku). 
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        public virtual string BackColorEndName { get; set; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual Color? OnMouseBackColor { get; set; }
        /// <summary>
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual string OnMouseBackColorName { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual Color? OnMouseBackColorEnd { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku).
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        public virtual string OnMouseBackColorEndName { get; set; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual Color? FocusedBackColor { get; set; }
        /// <summary>
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual string FocusedBackColorName { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual Color? FocusedBackColorEnd { get; set; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku).
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        public virtual string FocusedBackColorEndName { get; set; }

    }
    /// <summary>
    /// Definice vzhledu pozadí
    /// </summary>
    public interface IDataFormBackgroundAppearance
    {
        /// <summary>
        /// Název stylu (=kalíšek ve smyslu Nephrite).
        /// Pokud je zadán, pak je použit jako druhý zdroj hodnot (prvním zdrojem jsou jednotlivé property tohoto objektu).
        /// </summary>
        string StyleName { get; }
        /// <summary>
        /// Název obrázku použitého na pozadí
        /// </summary>
        string BackImageName { get; }
        /// <summary>
        /// Umístění obrázku použitého na pozadí
        /// </summary>
        BackImageAlignmentMode BackImageAlignment { get; }
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
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        string BackColorName { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        Color? BackColorEnd { get; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku). 
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v neaktivním stavu.
        /// </summary>
        string BackColorEndName { get; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        Color? OnMouseBackColor { get; }
        /// <summary>
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        string OnMouseBackColorName { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        Color? OnMouseBackColorEnd { get; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku).
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy na prvku je myš.
        /// </summary>
        string OnMouseBackColorEndName { get; }
        /// <summary>
        /// Barva pozadí plná, nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        Color? FocusedBackColor { get; }
        /// <summary>
        /// Barva pozadí plná (její název kalíšku), nebo (pokud je definovaná párová barva End) barva počáteční v Gradientu.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        string FocusedBackColorName { get; }
        /// <summary>
        /// Barva koncová v Gradientu. Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        Color? FocusedBackColorEnd { get; }
        /// <summary>
        /// Barva koncová v Gradientu (její název kalíšku).
        /// Pokud není zadaná barva počáteční (bez suffixu End) pak se barva End ignoruje.
        /// <para/>
        /// Barva v situaci, kdy v prvku je focus.
        /// </summary>
        string FocusedBackColorEndName { get; }
    }
    /// <summary>
    /// Kam vykreslit obrázek pozadí
    /// </summary>
    public enum BackImageAlignmentMode
    {
        /// <summary>
        /// Nebude kreslen i když bude zadán
        /// </summary>
        None = 0,
        /// <summary>
        /// Celé pozadí bude obrázkem vydlážděno, pozor na výkonové problémy
        /// </summary>
        Tile,
        /// <summary>
        /// Celé pozadí bude obrázkem vyplněno bez ohledu na AspectRatio (bude zkreslen poměr stran), pozor na malý obrázek ve velké ploše
        /// </summary>
        Fill,
        /// <summary>
        /// Pozadí bude obrázkem vyplněno s ohledem na AspectRatio (bez zkreslení poměru stran), pozor na malý obrázek ve velké ploše
        /// </summary>
        Enlarge,
        /// <summary>
        /// Nahoře vlevo
        /// </summary>
        TopLeft,
        /// <summary>
        /// Nahoře, vodorovně uprostřed
        /// </summary>
        TopCenter,
        /// <summary>
        /// Nahoře vpravo
        /// </summary>
        TopRight,
        /// <summary>
        /// Levý okraj, svisle uprostřed
        /// </summary>
        LeftCenter,
        /// <summary>
        /// Uprostřed beze změny měřítka
        /// </summary>
        Center,
        /// <summary>
        /// Pravý okraj, svisle uprostřed
        /// </summary>
        RightCenter,
        /// <summary>
        /// Dole vlevo
        /// </summary>
        BottomLeft,
        /// <summary>
        /// Dole, vodorovně uprostřed
        /// </summary>
        BottomCenter,
        /// <summary>
        /// Dole vpravo
        /// </summary>
        BottomRight
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
    public enum DataFormColumnIndicatorType
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
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, orámování tenké, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true.
        /// </summary>
        CorrectOnDemandThin = 0x0010,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, orámování silné, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        CorrectOnDemandBold = 0x0020,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, orámování tenké, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>.
        /// </summary>
        CorrectAllwaysThin = 0x0040,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.CorrectIndicatorColor"/>, orámování silné, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        CorrectAllwaysBold = 0x0080,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, orámování tenké, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        WarningOnDemandThin = 0x0100,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, orámování silné, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        WarningOnDemandBold = 0x0200,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, orámování tenké, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        WarningAllwaysThin = 0x0400,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.WarningIndicatorColor"/>, orámování silné, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        WarningAllwaysBold = 0x0800,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, orámování tenké, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        ErrorOnDemandThin = 0x1000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, orámování silné, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        ErrorOnDemandBold = 0x2000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, orámování tenké, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        ErrorAllwaysThin = 0x4000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="DxDataFormAppearance.ErrorIndicatorColor"/>, orámování silné, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        ErrorAllwaysBold = 0x8000,

        /// <summary>
        /// Prvek bude orámován barvou <see cref="IDataFormColumn.IndicatorColor"/>, orámování tenké, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        IndicatorColorOnDemandThin = 0x10000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="IDataFormColumn.IndicatorColor"/>, orámování silné, 
        /// pouze pokud bude hodnota <see cref="DxDataForm.ItemIndicatorsVisible"/> = true
        /// </summary>
        IndicatorColorOnDemandBold = 0x20000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="IDataFormColumn.IndicatorColor"/>, orámování tenké, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        IndicatorColorAllwaysThin = 0x40000,
        /// <summary>
        /// Prvek bude orámován barvou <see cref="IDataFormColumn.IndicatorColor"/>, orámování silné, 
        /// bez ohledu na hodnotu <see cref="DxDataForm.ItemIndicatorsVisible"/>
        /// </summary>
        IndicatorColorAllwaysBold = 0x80000
    }
    /// <summary>
    /// Druh prvku v DataFormu
    /// </summary>
    public enum DataFormColumnType
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
