using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Selector : správce interaktivních objektů, které jsou selectované.
    /// Pomáhá v rámci <see cref="GInteractiveControl"/> provádět vyběr / rušení výběru u interaktivních prvků.
    /// Každý prvek se k instanci <see cref="Selector"/> dostane přes <see cref="IInteractiveParent.Host"/>.Selector
    /// Nejde o samostatnou grafickou komponentu.
    /// </summary>
    public class Selector
    {
        #region Konstruktor, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Selector()
        { }
        /// <summary>
        /// Příprava k používání: pokud některá Dictionary je null, vytvoří se new.
        /// </summary>
        private void _PrepareForUse()
        {
            if (this._Selected == null) this._Selected = new Dictionary<uint, IInteractiveItem>();
            if (this._Framed == null) this._Framed = new Dictionary<uint, IInteractiveItem>();
        }
        private Dictionary<uint, IInteractiveItem> _Selected;
        private Dictionary<uint, IInteractiveItem> _Framed;
        #endregion
        #region Selectování
        /// <summary>
        /// Metoda vrátí true, pokud daný prvek je aktuálně Selected; výhradně stabilní Select, nezohledňuje Framed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsSelected(IInteractiveItem item)
        {
            if (item == null) return false;
            this._PrepareForUse();
            return (this._Selected.ContainsKey(item.Id));
        }
        /// <summary>
        /// Metoda provede odselectování všech aktuálně selectovaných prvků
        /// </summary>
        public void ClearSelected()
        {
            this._PrepareForUse();
            // Pokud nemáme nechat ostatní prvky selectované, a nějaké existují, pak je musíme odselectovat:
            this._Selected.Values.ForEachItem(i => i.Repaint());     // Zajistím, že se aktuálně selectované prvky překreslí
            this._Selected.Clear();                                  // A všechny zruším = budou mít IInteractiveItem.IsSelected = false.
        }
        /// <summary>
        /// Metoda zajistí, že daný prvek změní svůj stav Selected (výhradně stabilní Select): pokud nyní je vybraný, pak nebude; a naopak.
        /// Druhý parametr říká, zda ostatní dosud selectované prvky mají být zapomenuty (leaveOther je false) anebo ponechány (leaveOther je true).
        /// Standardně se tato metoda volá po kliknutí levou myší na prvek, 
        /// a parametr leaveOther se odvozuje od stisknuté klávesy Ctrl: stisknutá klávesa =» leaveOther = true =» ponechat ostatní prvky beze změn.
        /// Tato metoda nijak nepracuje s příznaky Framed.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="leaveOther"></param>
        public void ChangeSelection(IInteractiveItem item, bool leaveOther)
        {
            if (item == null) return;
            this._PrepareForUse();
            if (!leaveOther && this._Selected.Count > 0)
                this.ClearSelected();

            uint id = item.Id;
            if (!this._Selected.ContainsKey(id))
                this._Selected.Add(id, item);
            else
                this._Selected.Remove(id);
            item.Repaint();
        }
        /// <summary>
        /// Metoda nastaví IsSelected pro daný prvek na danou hodnotu.
        /// Tato metoda nemění stav <see cref="IInteractiveItem.IsSelected"/> pro ostatní aktuálně selectované prvky.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isSelected"></param>
        public void SetSelected(IInteractiveItem item, bool isSelected)
        {
            if (item == null) return;
            this._PrepareForUse();
            uint id = item.Id;
            bool oldSelected = this._Selected.ContainsKey(id);

            if (isSelected && !oldSelected)
                this._Selected.Add(id, item);
            else if (!isSelected && oldSelected)
                this._Selected.Remove(id);

            if (isSelected != oldSelected)
                item.Repaint();
        }
        #endregion
        #region Framování
        /// <summary>
        /// Metoda vrátí true, pokud daný prvek je aktuálně Framed; výhradně Framed, nezohledňuje Select.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsFramed(IInteractiveItem item)
        {
            if (item == null) return false;
            this._PrepareForUse();
            return (this._Framed.ContainsKey(item.Id));
        }
        /// <summary>
        /// Metoda provede odebrání z Framed všech aktuálně framovaných prvků
        /// </summary>
        public void ClearFramed()
        {
            this._PrepareForUse();
            // Pokud nemáme nechat ostatní prvky selectované, a nějaké existují, pak je musíme odselectovat:
            this._Framed.Values.ForEachItem(i => i.Repaint());       // Zajistím, že se aktuálně selectované prvky překreslí
            this._Framed.Clear();                                    // A všechny zruším = budou mít IInteractiveItem.IsSelected = false.
        }
        /// <summary>
        /// Metoda nastaví IsFramed pro daný prvek na danou hodnotu.
        /// Tato metoda nemění stav <see cref="IInteractiveItem.IsFramed"/> pro ostatní aktuálně framované prvky.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isFramed"></param>
        public void SetFramed(IInteractiveItem item, bool isFramed)
        {
            if (item == null) return;
            this._PrepareForUse();
            uint id = item.Id;
            bool oldFramed = this._Framed.ContainsKey(id);

            if (isFramed && !oldFramed)
                this._Framed.Add(id, item);
            else if (!isFramed && oldFramed)
                this._Framed.Remove(id);

            if (isFramed != oldFramed)
                item.Repaint();
        }
        /// <summary>
        /// Metoda zajistí, že ve stavu Framed budou jen předané prvky.
        /// To znamená, že prvky, které nejsou předané v parametru, budou z pole Framed odebrány.
        /// Dále se zajistí, že prvky budou v poli Framed přidány v pořadí, v jakém jsou na vstupu.
        /// </summary>
        /// <param name="items"></param>
        public void SetFramedItems(IEnumerable<IInteractiveItem> items)
        {
            if (items == null) return;
            this._PrepareForUse();
            var itemDict = items.GetDictionary(i => i.Id, true);

            // a) odeberu prvky, které jsou ve this._Framed a nyní nejsou na vstupu:
            this._Framed.RemoveWhere((id, item) =>
            {   // Pokud vstupní pole (itemDict) NEOBSAHUJE klíč prvku z Dictionary this._Framed, pak bude (remove) = true:
                bool remove = !itemDict.ContainsKey(id);
                if (remove)
                    // Prvky, které z Dictionary this._Framed budou odebrány, musíme překreslit:
                    item.Repaint();
                return remove;
            });

            // b) přidám prvky, které jsou nyní na vstupu, ale ještě nejsou v Dictionary this._Framed:
            foreach (IInteractiveItem item in items)
            {
                if (!this._Framed.ContainsKey(item.Id))
                {
                    this._Framed.Add(item.Id, item);
                    item.Repaint();
                }
            }
        }
        /// <summary>
        /// Přenese prvky Framed do prvků Selected, seznam Framed vyprázdní.
        /// Zajistí Repaint pro prvky, jichž se přenos týká.
        /// </summary>
        public void MoveFramedToSelected()
        {
            this._PrepareForUse();
            foreach (IInteractiveItem item in this._Framed.Values)
            {
                if (!this._Selected.ContainsKey(item.Id))
                {
                    this._Selected.Add(item.Id, item);
                }
                item.Repaint();
            }
            this._Framed.Clear();
        }
        #endregion
    }
}
