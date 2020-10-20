using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    #region class InteractiveFocusManager : Správce pro řešení předávání focusu mezi interaktivními prvky pomocí klávesnice
    /// <summary>
    /// Správce pro řešení předávání focusu mezi interaktivními prvky pomocí klávesnice
    /// </summary>
    internal class InteractiveFocusManager
    {
        /// <summary>
        /// Metoda zkusí najít první konkrétní prvek v rámci daného interaktivního parenta, do kterého lze umístit klávesový Focus.
        /// Pokud dodaný parent sám je interaktivní prvek <see cref="IInteractiveItem"/>, a pokud vyhovuje podmínkám Visible a TabStop, a podporuje KeyboardInput, pak je vrácen přímo tento prvek.
        /// Pokud není, nebo pokud nevyhovuje, pak se prověří jeho vlastní Childs prvky, a rekurzivně i jejich vnořené Childs prvky, a vyhledá se první vyhovující prvek.
        /// Pokud takový neexistuje, je vráceno false.
        /// </summary>
        /// <param name="parent">Vstupující prvek. On sám může být tím, kdo dostane focus, anebo některý z jeho Childs prvků (rekurzivně). 
        /// Vstupující prvek může být celý Control (=Host), nebo jakýkoli container nebo konkrétní prvek.</param>
        /// <param name="direction">Směr hledání: <see cref="Direction.Positive"/>: hledá první vhodný prvek (od začátku); <see cref="Direction.Negative"/>: hledá poslední vhodný prvek (od konce)</param>
        /// <param name="foundItem">Out: nalezený prvek (this nebo některý z this.Childs[.Childs[...]])</param>
        /// <param name="requirements">Požadavky na prohledávané prvky</param>
        /// <returns>true = nalezeno / false = nenalezeno</returns>
        internal static bool TryGetOuterFocusItem(IInteractiveParent parent, Direction direction, out IInteractiveItem foundItem, InteractiveFocusStateFlag requirements = InteractiveFocusStateFlag.Default)
        {
            foundItem = null;
            if (parent == null) return false;                                                             // Null nebrat

            // Pokud sám daný prvek je interaktivní, a pokud je vyhovující, pak jej akceptujeme:
            if (parent is IInteractiveItem)
            {
                IInteractiveItem item = parent as IInteractiveItem;
                if (IsItemValid(item, requirements) && item.Is.KeyboardInput)
                {
                    foundItem = item;
                    return true;
                }
            }

            // Získáme seznam Childs prvky daného prvku, ve správném pořadí, a najdeme první vhodný prvek = cyklem (skrz Childs) a rekurzí (do téže metody):
            List<IInteractiveItem> childList = GetChildsSorted(parent, direction, requirements, null);
            if (childList == null) return false;

            foreach (IInteractiveItem childItem in childList)
            {
                if (TryGetOuterFocusItem(childItem, direction, out foundItem, requirements)) return true;  // childItem je IInteractiveItem, a protože IInteractiveItem je potomkem IInteractiveParent, vyvoláme přímou rekurzi...
            }

            return false;
        }
        /// <summary>
        /// Metoda zkusí najít prvek následující za daným prvkem, do kterého má z něj přejít focus v daném směru
        /// </summary>
        /// <param name="currentItem">Výchozí prvek pro hledání. Tento prvek nedostane focus, dostane jej některý z jeho sousedů (=sousední prvky Childs od Parenta tohoto daného prvku).</param>
        /// <param name="direction">Směr kroku: <see cref="Direction.Positive"/> = "doprava" = na následující prvek (klávesou Tab); <see cref="Direction.Negative"/> = "doleva" = na předešlý prvek (klávesou Ctrl+Tab) </param>
        /// <param name="nextItem">Out nalezený sousední prvek</param>
        /// <param name="requirements">Požadavky na prohledávané prvky</param>
        /// <returns>true = nalezeno / false = nenalezeno</returns>
        internal static bool TryGetNextFocusItem(IInteractiveItem currentItem, Direction direction, out IInteractiveItem nextItem, InteractiveFocusStateFlag requirements = InteractiveFocusStateFlag.Default)
        {
            nextItem = null;
            if (currentItem == null) return false;                                                      // Nezadán prvek
            if (!(direction == Direction.Positive || direction == Direction.Negative)) return false;    // Nezadán platný směr

            List<IInteractiveItem> childList = GetChildsSorted(currentItem.Parent, direction, requirements, currentItem);
            if (childList == null) return false;                                                        // Jeho Parent neobsahuje žádné prvky

            int index = childList.FindIndex(i => Object.ReferenceEquals(currentItem, i));
            if (index < 0) return false;                                                                // Jeho Parent neobsahuje zadaný prvek (???)

            // Nyní projdu sousední prvky vstupního prvku, za ním/před ním ve správném pořadí (seznam je setříděn Positive = ASC / Negative = DESC):
            int count = childList.Count;
            for (int i = (index + 1); i < count; i++)
            {
                // Pokud daný sousední prvek je sám vhodný, anebo ve svých Childs obsahuje vhodný prvek, pak jej dáme do out nextItem a vrátíme true:
                if (TryGetOuterFocusItem(childList[i], direction, out nextItem, requirements)) return true;
            }

            // V mé úrovni ani v mých Child prvcích jsme nenašli vhodný prvek. Zpracujeme obdobně vyšší úroveň (=sousedy mého parenta):
            if (!(currentItem.Parent is IInteractiveItem)) return false;                                // Prvek nemá interaktivního Parenta = náš parent je fyzický Control.

            return TryGetNextFocusItem((currentItem.Parent as IInteractiveItem), direction, out nextItem, requirements);
        }
        /// <summary>
        /// Metoda získá a vrátí seznam Child prvků daného prvku <paramref name="parentItem"/>.
        /// Seznam setřídí podle <see cref="IInteractiveItem.TabOrder"/> vzestupně (pro <paramref name="direction"/> == <see cref="Direction.Positive"/>) 
        /// nebo sestupně (pro <paramref name="direction"/> == <see cref="Direction.Negative"/>).
        /// <para/>
        /// Seznam obsahuje pouze ty prvky, které jsou viditelné, které mají Enabled = true a které mají TabStop = true, vše podle požadavků v parametru <paramref name="requirements"/>.
        /// Seznam může volitelně obsahovat i explicitně zadaný Child prvek <paramref name="includeItem"/> bez ohledu na jeho Visible a TabStop (tedy pokud tento prvek je obsažen v seznamu Childs).
        /// Tato vlastnost slouží k zařazení určitého prvku pro hledání jeho Next prvků.
        /// <para/>
        /// Seznam obsahuje prvky bez ohledu na jejich vlastnost KeyboardInput, protože tuto vlastnost mohou mít až jejich vnořené Child prvky, a my chceme najít i nadřízené Containery.
        /// <para/>
        /// Pokud je na vstupu objekt <paramref name="parentItem"/>= null, nebo jeho <see cref="IInteractiveParent.Childs"/> je null, pak výstupem metody je null (nebudeme generovat new empty List).
        /// Dále pak proto, že vstupní objekt i jeho Childs připouštíme, že smí být null, ale nechceme nutit volajícího aby si to sám testoval, 
        /// protože získání Childs může být výkonnostně náročnější = získáme to jen jednou a vyhodnotíme zde, volající ať si otestuje vrácený objekt, to je nenáročné.
        /// </summary>
        /// <param name="parentItem">Parent, jehož <see cref="IInteractiveParent.Childs"/> prvky budeme zpracovávat</param>
        /// <param name="direction">Směr třídění: <see cref="Direction.Positive"/> = podle TabOrder vzestupně; <see cref="Direction.Negative"/> = podle TabOrder sestupně</param>
        /// <param name="requirements">Požadavky na prohledávané prvky</param>
        /// <param name="includeItem">Prvek, který chceme do seznamu přidat bezpodmínečně (pokud v <see cref="IInteractiveParent.Childs"/> bude přítomen)</param>
        /// <returns></returns>
        private static List<IInteractiveItem> GetChildsSorted(IInteractiveParent parentItem, Direction direction, InteractiveFocusStateFlag requirements = InteractiveFocusStateFlag.Default, IInteractiveItem includeItem = null)
        {
            if (parentItem == null) return null;
            var childs = parentItem.Childs;
            if (childs == null) return null;

            List<IInteractiveItem> childList = childs.Where(i => IsItemValid(i, requirements, includeItem)).ToList();
            if (childList.Count > 1)
            {
                switch (direction)
                {
                    case Direction.Positive:
                        childList.Sort(InteractiveObject.CompareByTabOrderAsc);
                        break;
                    case Direction.Negative:
                        childList.Sort(InteractiveObject.CompareByTabOrderDesc);
                        break;
                }
            }
            return childList;
        }
        /// <summary>
        /// Vrátí true pro testovaný prvek Item, který vyhovuje filtraci:
        /// a) Pokud Item je null, vrátí false (null nebrat);
        /// b) Pokud includeItem není null, a Item je identický s includeItem, vrátí true (includeItem bereme vždy);
        /// c) Pokud chceme pouze viditelné, a prvek Item není viditelný, vrátí false;
        /// d) Pokud chceme pouze TabStop, a prvek Item není TabStop, vrátí false;
        /// Jinak vrátí true = prvek Item vyhovuje.
        /// </summary>
        /// <param name="item">Testovaný prvek</param>
        /// <param name="requirements">Požadavky na prohledávané prvky</param>
        /// <param name="includeItem">Prvek, který chceme do seznamu přidat bezpodmínečně (pokud v <see cref="IInteractiveParent.Childs"/> bude přítomen)</param>
        /// <returns></returns>
        private static bool IsItemValid(IInteractiveItem item, InteractiveFocusStateFlag requirements = InteractiveFocusStateFlag.Default, IInteractiveItem includeItem = null)
        {
            if (item == null) return false;
            if (includeItem != null && Object.ReferenceEquals(item, includeItem)) return true;
            if (requirements.HasFlag(InteractiveFocusStateFlag.Visible) && !item.Is.Visible) return false;        // Pokud prvek musí být Visible, a tento není, pak vrátím false
            if (requirements.HasFlag(InteractiveFocusStateFlag.Enabled) && !item.Is.Enabled) return false;        // Pokud prvek musí být Enabled, a tento není, pak vrátím false
            if (requirements.HasFlag(InteractiveFocusStateFlag.TabStop) && !item.Is.TabStop) return false;        // Pokud prvek musí být TabStop, a tento není, pak vrátím false
            if (!Settings.TabStopOnReadOnlyItems && item.Is.ReadOnly) return false;                               // Pokud Setting říká "TabStopOnReadOnlyItems = false", a tento prvek je ReadOnly, pak vrátím false
            return true;
        }
    }
    #endregion
    #region enum InteractiveFocusStateFlag : Požadavky na vyhledání prvku podle jeho vlastností pro předávání focusu
    /// <summary>
    /// Požadavky na vyhledání prvku podle jeho vlastností pro předávání focusu
    /// </summary>
    [Flags]
    public enum InteractiveFocusStateFlag
    {
        /// <summary>
        /// Nemáme požadavky, akceptujeme jakýkoli prvek
        /// </summary>
        None = 0,
        /// <summary>
        /// Prvek musí být Visible
        /// </summary>
        Visible = 0x01,
        /// <summary>
        /// Prvek musí být Enabled
        /// </summary>
        Enabled = 0x02,
        /// <summary>
        /// Prvek musí být TabStop
        /// </summary>
        TabStop = 0x04,
        /// <summary>
        /// Default = všechny standardní požadavky = <see cref="Visible"/> a <see cref="Enabled"/> a <see cref="TabStop"/>
        /// </summary>
        Default = Visible | Enabled | TabStop
    }
    #endregion
}
