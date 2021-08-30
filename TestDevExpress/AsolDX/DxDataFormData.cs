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
            Items = new List<IDataFormItem>();
        }
        /// <summary>
        /// ID grupy, jednoznačné v celém DataFormu
        /// </summary>
        public virtual string GroupId { get; set; }
        /// <summary>
        /// Název obrázku grupy
        /// </summary>
        public virtual string PageImageName { get; set; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        public virtual string GroupText { get; set; }
        /// <summary>
        /// Vnitřní okraje mezi souřadnicí grupy a souřadnicí prvku uvnitř
        /// </summary>
        public virtual SWF.Padding DesignPadding { get; set; }
        /// <summary>
        /// Pravidla pro sbalení/rozbalení grupy
        /// </summary>
        public virtual DataFormGroupCollapseMode CollapseMode { get; set; }
        /// <summary>
        /// Grupa je sbalena? GUI bude tuto hodnotu nastavovat
        /// </summary>
        public virtual bool Collapsed { get; set; }
        /// <summary>
        /// Obsahuje true, pokud tato grupa smí být zalomena = smí začínat na dalším sloupci layoutu v rámci jedné stránky.
        /// Algoritmus zalomí stránky tak, aby optimálně využil dostupnou šířku prostoru, a do něj vložil všechny grupy tak, aby byly rozloženy rovnoměrně.
        /// </summary>
        public virtual bool AllowPageBreak { get; set; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        public virtual DataFormAppearance Appearance { get; set; }
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

        IEnumerable<IDataFormItem> IDataFormGroup.Items { get { return Items; } }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jednu grupu v rámci DataFormu
    /// </summary>
    public interface IDataFormGroup
    {
        /// <summary>
        /// ID grupy, jednoznačné v celém DataFormu
        /// </summary>
        string GroupId { get; }
        /// <summary>
        /// Název obrázku grupy
        /// </summary>
        string PageImageName { get; }
        /// <summary>
        /// Titulek grupy
        /// </summary>
        string GroupText { get; }
        /// <summary>
        /// Vnitřní okraje mezi souřadnicí grupy a souřadnicí prvku uvnitř.
        /// Designová hodnota.
        /// </summary>
        SWF.Padding DesignPadding { get; }
        /// <summary>
        /// Pravidla pro sbalení/rozbalení grupy
        /// </summary>
        DataFormGroupCollapseMode CollapseMode { get; }
        /// <summary>
        /// Grupa je sbalena? GUI bude tuto hodnotu nastavovat
        /// </summary>
        bool Collapsed { get; set; }
        /// <summary>
        /// Obsahuje true, pokud tato grupa smí být zalomena = smí začínat na dalším sloupci layoutu v rámci jedné stránky.
        /// Algoritmus zalomí stránky tak, aby optimálně využil dostupnou šířku prostoru, a do něj vložil všechny grupy tak, aby byly rozloženy rovnoměrně.
        /// </summary>
        bool AllowPageBreak { get; }
        /// <summary>
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        DataFormAppearance Appearance { get; }
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
    public class DataFormItemCheckItem : DataFormItemImageText, IDataFormItemCheckItem
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        public virtual bool Checked { get; set; }
    }
    /// <summary>
    /// Předpis požadovaných vlastností pro jeden prvek v rámci DataFormu, který má Text a Ikonu a zaškrtávací hodnotu (CheckBox, CheckButton)
    /// </summary>
    public interface IDataFormItemCheckItem : IDataFormItemImageText
    {
        /// <summary>
        /// Je zaškrtnuto
        /// </summary>
        bool Checked { get; }
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
                case DataFormItemType.EditBox: return null;
                case DataFormItemType.SpinnerBox: return null;
                case DataFormItemType.CheckBox: return new DataFormItemCheckItem() { ItemType = itemType };
                case DataFormItemType.BreadCrumb: return null;
                case DataFormItemType.ComboBoxList: return null;
                case DataFormItemType.ComboBoxEdit: return null;
                case DataFormItemType.TokenEdit: return null;
                case DataFormItemType.ListView: return null;
                case DataFormItemType.TreeView: return null;
                case DataFormItemType.RadioButtonBox: return null;
                case DataFormItemType.Button: return new DataFormItemImageText() { ItemType = itemType };
                case DataFormItemType.CheckButton: return new DataFormItemCheckItem() { ItemType = itemType };
                case DataFormItemType.DropDownButton: return null;
                case DataFormItemType.BarCode: return null;
                case DataFormItemType.Image: return null;
            }
            return null;
        }
        #endregion
        #region Základní prvky
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
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        public virtual DataFormAppearance Appearance { get; set; }

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
        /// Vzhled prvku - kalíšek, barvy, modifikace fontu
        /// </summary>
        DataFormAppearance Appearance { get; }
    }
    /// <summary>
    /// Modifikace vzhledu prvku
    /// </summary>
    public class DataFormAppearance
    {
        /// <summary>
        /// Změna velikosti písma
        /// </summary>
        public int? FontSizeDelta { get; set; }
        /// <summary>
        /// Změna stylu písma - Bold
        /// </summary>
        public bool? FontStyleBold { get; set; }
        /// <summary>
        /// Změna stylu písma - Italic
        /// </summary>
        public bool? FontStyleItalic { get; set; }
        /// <summary>
        /// Změna stylu písma - Underline
        /// </summary>
        public bool? FontStyleUnderline { get; set; }
        /// <summary>
        /// Změna stylu písma - StrikeOut
        /// </summary>
        public bool? FontStyleStrikeOut { get; set; }
        /// <summary>
        /// Změna barvy pozadí
        /// </summary>
        public Color? BackColor { get; set; }
        /// <summary>
        /// Změna barvy popředí = písma
        /// </summary>
        public Color? ForeColor { get; set; }
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
    #endregion
}
