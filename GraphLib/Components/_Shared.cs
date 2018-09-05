using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asol.Tools.WorkScheduler.Components
{
    #region Enumy, které se sdílí mezi WorkScheduler a GraphLibrary
    // VAROVÁNÍ : Změna názvu jednotlivých enumů je zásadní změnou, která se musí promítnout i do konstant ve WorkSchedulerSupport a to jak zde, tak v Greenu.
    //            Hodnoty se z Greenu předávají v textové formě, a tady v GUI se z textu získávají parsováním (Enum.TryParse()) !

    /// <summary>
    /// Režim, jak osa reaguje na změnu velikosti.
    /// Pokud osa obsahuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
    /// pak po změně velikosti osy na 100 pixelů může dojít k jedné ze dvou akcí: změna rozsahu, nebo změna měřítka.
    /// a) změní se zobrazený rozsah, a zachová se měřítko (to je defaultní chování), pak 
    /// </summary>
    public enum AxisResizeContentMode
    {
        /// <summary>
        /// Neurčeno, v případě nutnosti se použije ChangeValue
        /// </summary>
        None,
        /// <summary>
        /// Změna hodnoty End:
        /// Pokud osa ve výchozím stavu zobrazuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
        /// pak po změně velikosti osy na 100 pixelů se zachová měřítko (1:1), a zvětší se rozsah zobrazených dat tak, 
        /// že osa bude nově zobrazovat data pro rozsah { 100 ÷ 200 }.
        /// </summary>
        ChangeValueEnd,
        /// <summary>
        /// Změna měřítka:
        /// Pokud osa ve výchozím stavu zobrazuje data pro rozsah { 100 ÷ 150 } a má velikost 50 pixelů, 
        /// pak po změně velikosti osy na 100 pixelů se ponechá rozsah zobrazených hodnot (stále bude zobrazen rozsah dat { 100 ÷ 150 }),
        /// ale upraví se měřítko tak, že osa bude zobrazovat více detailů (z měřítka 1:1 bude 2:1).
        /// </summary>
        ChangeScale
    }
    /// <summary>
    /// Režim, jak může uživatel interaktivně (myší) měnit hodnotu na ose.
    /// </summary>
    [Flags]
    public enum AxisInteractiveChangeMode
    {
        /// <summary>
        /// Uživatel interaktivně (myší) NESMÍ měnit hodnotu na ose ani posunutím, ani změnou měřítka.
        /// </summary>
        None = 0,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose posunutím.
        /// </summary>
        Shift = 1,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose změnou měřítka.
        /// </summary>
        Zoom = 2,
        /// <summary>
        /// Uživatel interaktivně (myší) SMÍ měnit hodnotu na ose jak posunutím, tak i změnou měřítka.
        /// </summary>
        All = Shift | Zoom
    }
    /// <summary>
    /// Režimy chování položky grafu. Zahrnují možnosti editace a možnosti zobrazování textu, tooltipu a vztahů.
    /// Editovatelnost položky grafu.
    /// </summary>
    [Flags]
    public enum GraphItemBehaviorMode : int
    {
        /// <summary>
        /// Bez zadání
        /// </summary>
        None = 0,
        /// <summary>
        /// Lze změnit délku času (roztáhnout šířku pomocí přesunutí začátku nebo konce)
        /// </summary>
        ResizeTime = 0x01,
        /// <summary>
        /// Lze změnit výšku = obsazený prostor v grafu (roztáhnout výšku)
        /// </summary>
        ResizeHeight = 0x02,
        /// <summary>
        /// Lze přesunout položku grafu na ose X = čas doleva / doprava
        /// </summary>
        MoveToAnotherTime = 0x10,
        /// <summary>
        /// Lze přesunout položku grafu na ose Y = na jiný řádek tabulky
        /// </summary>
        MoveToAnotherRow = 0x20,
        /// <summary>
        /// Lze přesunout položku grafu do jiné tabulky
        /// </summary>
        MoveToAnotherTable = 0x40,
        /// <summary>
        /// Nezobrazovat text v prvku nikdy.
        /// Toto je explicitní hodnota; ale shodné chování bude použito i když nebude specifikována žádná jiná hodnota ShowCaption*.
        /// </summary>
        ShowCaptionNone = 0x1000,
        /// <summary>
        /// Zobrazit text v prvku při stavu MouseOver.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionInMouseOver = 0x2000,
        /// <summary>
        /// Zobrazit text v prvku při stavu Selected.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionInSelected = 0x4000,
        /// <summary>
        /// Zobrazit text v prvku vždy.
        /// Pokud nebude specifikována hodnota <see cref="ShowCaptionInMouseOver"/> ani <see cref="ShowCaptionInSelected"/> ani <see cref="ShowCaptionAllways"/>, nebude se zobrazovat text v prvku vůbec.
        /// </summary>
        ShowCaptionAllways = 0x8000,
        /// <summary>
        /// Nezobrazovat ToolTip nikdy.
        /// Toto je explicitní hodnota; ale shodné chování bude použito i když nebude specifikována žádná jiná hodnota ShowToolTip*.
        /// </summary>
        ShowToolTipNone = 0x10000,
        /// <summary>
        /// Zobrazit ToolTip až nějaký čas po najetí myší, a po přiměřeném čase (vzhledem k délce zobrazeného textu) zhasnout.
        /// Pokud nebude specifikována hodnota <see cref="ShowToolTipImmediatelly"/> ani <see cref="ShowToolTipFadeIn"/>, nebude se zobrazovat ToolTip vůbec.
        /// </summary>
        ShowToolTipFadeIn = 0x20000,
        /// <summary>
        /// Zobrazit ToolTip okamžitě po najetí myší na prvek (trochu brutus) a nechat svítit "skoro pořád".
        /// Pokud nebude specifikována hodnota <see cref="ShowToolTipImmediatelly"/> ani <see cref="ShowToolTipFadeIn"/>, nebude se zobrazovat ToolTip vůbec.
        /// </summary>
        ShowToolTipImmediatelly = 0x40000,
        /// <summary>
        /// Default pro pracovní čas = <see cref="ResizeTime"/> | <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/>
        /// </summary>
        DefaultWorkTime = ResizeTime | MoveToAnotherTime | MoveToAnotherRow,
        /// <summary>
        /// Default pro text = <see cref="ShowCaptionInMouseOver"/> | <see cref="ShowCaptionInSelected"/> | <see cref="ShowToolTipFadeIn"/>
        /// </summary>
        DefaultText = ShowCaptionInMouseOver | ShowCaptionInSelected | ShowToolTipFadeIn,
        /// <summary>
        /// Souhrn příznaků, povolujících Drag and Drop prvku = <see cref="MoveToAnotherTime"/> | <see cref="MoveToAnotherRow"/> | <see cref="MoveToAnotherTable"/>
        /// </summary>
        AnyMove = MoveToAnotherTime | MoveToAnotherRow | MoveToAnotherTable
    }
    /// <summary>
    /// Režim přepočtu DateTime na osu X.
    /// </summary>
    public enum TimeGraphTimeAxisMode
    {
        /// <summary>
        /// Výchozí = podle vlastníka (sloupce, nebo tabulky).
        /// </summary>
        Default = 0,
        /// <summary>
        /// Standardní režim, kdy graf má osu X rovnou 1:1 k prvku TimeAxis.
        /// Využívá se v situaci, kdy prvky grafu jsou kresleny přímo pod TimeAxis.
        /// </summary>
        Standard,
        /// <summary>
        /// Proporcionální režim, kdy graf vykresluje ve své ploše stejný časový úsek jako TimeAxis,
        /// ale graf má jinou šířku v pixelech než časová osa (a tedy může mít i jiný počátek = souřadnici Bounds.X.
        /// Pak se pro přepočet hodnoty DateTime na hodnotu pixelu na ose X nepoužívá přímo TimeConverter, ale prostý přepočet vzdáleností.
        /// </summary>
        ProportionalScale,
        /// <summary>
        /// Logaritmický režim, kdy graf dovolí vykreslit všechny prvky grafu bez ohledu na to, že jejich pozice X (datum) je mimo rozsah TimeAxis.
        /// Vykreslování probíhá tak, že střední část grafu (typicky 60%) zobrazuje prvky proporcionálně (tj. lineárně) k časovému oknu,
        /// a okraje (vlevo a vpravo) zobrazují prvky ležící mimo časové okno, jejichž souřadnice X je určena logaritmicky.
        /// Na souřadnici X = 0 (úplně vlevo v grafu) se zobrazují prvky, jejichž Begin = mínus nekonečno,
        /// a na X = Right (úplně vpravo v grafu) se zobrazují prvky, jejichž End = plus nekonečno.
        /// </summary>
        LogarithmicScale
    }



    #endregion
}
