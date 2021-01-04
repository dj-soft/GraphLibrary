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
    /// Pomáhá v rámci <see cref="InteractiveControl"/> provádět vyběr / rušení výběru u interaktivních prvků.
    /// Každý prvek se k instanci <see cref="Selector"/> dostane přes <see cref="IInteractiveParent.Host"/>.Selector
    /// Nejde o samostatnou grafickou komponentu.
    /// </summary>
    public class Selector : ISelectorInternal
    {
        #region Konstruktor, proměnné
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Selector()
        {
            this._PrepareForUse();
        }
        /// <summary>
        /// Příprava k používání: pokud některá Dictionary je null, vytvoří se new.
        /// </summary>
        private void _PrepareForUse()
        {
            if (this._Selected == null) this._Selected = new Dictionary<uint, IInteractiveItem>();
            if (this._Framed == null) this._Framed = new Dictionary<uint, IInteractiveItem>();
            if (this._Activated == null) this._Activated = new Dictionary<uint, IInteractiveItem>();
        }
        private Dictionary<uint, IInteractiveItem> _Selected;
        private Dictionary<uint, IInteractiveItem> _Framed;
        private Dictionary<uint, IInteractiveItem> _Activated;
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
            return (this._Selected.ContainsKey(item.Id));
        }
        /// <summary>
        /// Metoda provede odselectování všech aktuálně selectovaných prvků
        /// </summary>
        public void ClearSelected()
        {
            this._Selected.Values.ToArray().ForEachItem(i => i.IsSelected = false); // Položka sama ve své property IsSelected by měla zavolat: ((ISelectorInternal)host.Selector).SetSelectedValue(this, value);
            this._Selected.Clear();              // jen pro jistotu
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
            uint id = item.Id;
            bool isSelected = this._Selected.ContainsKey(id);        // Aktuální hodnota daného prvku; na konci metody do něj vložíme hodnotu opačnou (Changed)
            
            // Pokud nemáme nechat ostatní prvky selectované, a nějaké existují, pak musíme všechny prvky odselectovat:
            if (!leaveOther && this._Selected.Count > 0)
                this.ClearSelected();

            item.IsSelected = !isSelected;       // Položka sama ve své property IsSelected by měla zavolat: ((ISelectorInternal)host.Selector).SetSelectedValue(this, value);
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
            uint id = item.Id;
            bool oldSelected = this._Selected.ContainsKey(id);
            if (isSelected != oldSelected)
                item.IsSelected = isSelected;    // Položka sama ve své property IsSelected by měla zavolat: ((ISelectorInternal)host.Selector).SetSelectedValue(this, value);
        }
        /// <summary>
        /// Vlastní výkonná metoda, volá se výhradně z <see cref="IInteractiveItem.IsSelected"/>.set{}
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isSelected"></param>
        void ISelectorInternal.SetSelectedValue(IInteractiveItem item, bool isSelected)
        {
            if (item == null) return;
            uint id = item.Id;
            bool oldSelected = this._Selected.ContainsKey(id);
            if (isSelected != oldSelected)
            {
                if (isSelected && !oldSelected)
                    this._Selected.Add(id, item);
                else if (!isSelected && oldSelected)
                    this._Selected.Remove(id);
            }
        }
        /// <summary>
        /// Obsahuje souhrn všech aktuálně selectovaných prvků
        /// </summary>
        public IInteractiveItem[] SelectedItems { get { return this._Selected.Values.ToArray(); } }
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
            return (this._Framed.ContainsKey(item.Id));
        }
        /// <summary>
        /// Metoda provede odebrání z Framed všech aktuálně framovaných prvků
        /// </summary>
        public void ClearFramed()
        {
            this._Framed.Values.ToArray().ForEachItem(i => i.IsFramed = false);     // Položka sama ve své property IsSelected by měla zavolat: ((ISelectorInternal)host.Selector).SetFramedValue(this, value);
            this._Framed.Clear();                // jen pro jistotu
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
            uint id = item.Id;
            bool oldFramed = this._Framed.ContainsKey(id);
            if (isFramed != oldFramed)
                item.IsFramed = isFramed;    // Položka sama ve své property IsFramed by měla zavolat: ((ISelectorInternal)host.Selector).SetFramedValue(this, value);
        }
        /// <summary>
        /// Vlastní výkonná metoda, volá se výhradně z <see cref="IInteractiveItem.IsFramed"/>.set{}
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isFramed"></param>
        void ISelectorInternal.SetFramedValue(IInteractiveItem item, bool isFramed)
        {
            if (item == null) return;
            uint id = item.Id;
            bool oldFramed = this._Framed.ContainsKey(id);
            if (isFramed != oldFramed)
            {
                if (isFramed && !oldFramed)
                    this._Framed.Add(id, item);
                else if (!isFramed && oldFramed)
                    this._Framed.Remove(id);
            }
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

            var itemDict = items.GetDictionary(i => i.Id, true);

            // a) Vyberu si prvky, které nyní jsou ve this._Framed a aktuálně nejsou na vstupu => ty budeme odebírat:
            IInteractiveItem[] remove = this._Framed.Values.Where(i => !itemDict.ContainsKey(i.Id)).ToArray();

            // b) Na vybraných prvcích nastavím IsFramed = false, tím se ty prvky samy odeberou z this._Framed:
            remove.ForEachItem(i => i.IsFramed = false);

            // c) A pro jistotu z this._Framed odeberu dané prvky (to kdyby to prvek IInteractiveItem v property IsFramed neudělal sám):
            this._Framed.RemoveWhere((id, item) => !itemDict.ContainsKey(id));

            // d) A poté přidám ty prvky, které jsou nyní na vstupu, ale ještě nejsou v Dictionary this._Framed:
            foreach (IInteractiveItem item in items)
            {
                if (!this._Framed.ContainsKey(item.Id))
                    item.IsFramed = true;                  // Prvek sám si zařídí přidání do dictionary IsFramed, a zařídí si i Repaint().
            }
        }
        /// <summary>
        /// Přenese prvky Framed do prvků Selected, seznam Framed vyprázdní.
        /// Zajistí Repaint pro prvky, jichž se přenos týká.
        /// </summary>
        public void MoveFramedToSelected()
        {
            IInteractiveItem[] items = this.FramedItems;   // Musíme zhmotnit pole this._Framed.Values, protože z něj budeme průběžně odebírat prvky...
            foreach (IInteractiveItem item in items)
            {
                item.IsFramed = false;                     // Prvek sám se odebere z dictionary this._Framed, proto jsme to pole museli zhmotnit
                if (!this._Selected.ContainsKey(item.Id))
                    item.IsSelected = true;                // Prvek sám se přidá do dictionary this._Selected
            }
            this._Framed.Clear();      // jen pro jistotu
        }
        /// <summary>
        /// Obsahuje souhrn všech aktuálně framovaných prvků
        /// </summary>
        public IInteractiveItem[] FramedItems { get { return this._Framed.Values.ToArray(); } }
        /// <summary>
        /// Obsahuje souhrn všech aktuálně framovaných prvků, které dosud NEJSOU SELECTOVANÉ.
        /// </summary>
        public IEnumerable<IInteractiveItem> FramedOnlyItems { get { return this._GetFramedOnlyItems(); } }
        /// <summary>
        /// Vrací souhrn všech aktuálně framovaných prvků, které dosud NEJSOU SELECTOVANÉ.
        /// </summary>
        /// <returns></returns>
        private IInteractiveItem[] _GetFramedOnlyItems()
        {
            if (this._Framed.Count == 0) return new IInteractiveItem[0];
            if (this._Selected.Count == 0) return this._Framed.Values.ToArray();
            return this._Framed.Values
                .Where(i => !this._Selected.ContainsKey(i.Id))
                .ToArray();
        }
        #endregion
        #region Aktivování
        /// <summary>
        /// Metoda vrátí true, pokud daný prvek je aktuálně Activated.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsActivated(IInteractiveItem item)
        {
            if (item == null) return false;
            return (this._Activated.ContainsKey(item.Id));
        }
        /// <summary>
        /// Metoda provede deaktivaci všech aktuálně aktivních prvků
        /// </summary>
        public void ClearActivated()
        {
            this._Activated.Values.ToArray().ForEachItem(i => i.IsActivated = false);     // Položka sama ve své property IsActivated by měla zavolat: ((ISelectorInternal)host.Selector).SetActivatedValue(this, value);
            this._Activated.Clear();             // jen pro jistotu
        }
        /// <summary>
        /// Metoda nastaví IsActivated pro daný prvek na danou hodnotu.
        /// Tato metoda nemění stav <see cref="IInteractiveItem.IsActivated"/> pro ostatní aktuálně aktivní prvky.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isActivated"></param>
        public void SetActivated(IInteractiveItem item, bool isActivated)
        {
            if (item == null) return;
            uint id = item.Id;
            bool oldActivated = this._Activated.ContainsKey(id);
            if (isActivated != oldActivated)
                item.IsActivated = isActivated;    // Položka sama ve své property IsActivated by měla zavolat: ((ISelectorInternal)host.Selector).SetActivatedValue(this, value);
        }
        /// <summary>
        /// Vlastní výkonná metoda, volá se výhradně z <see cref="IInteractiveItem.IsActivated"/>.set{}
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isActivated"></param>
        void ISelectorInternal.SetActivatedValue(IInteractiveItem item, bool isActivated)
        {
            if (item == null) return;
            uint id = item.Id;
            bool oldActivated = this._Activated.ContainsKey(id);
            if (isActivated != oldActivated)
            {
                if (isActivated && !oldActivated)
                    this._Activated.Add(id, item);
                else if (!isActivated && oldActivated)
                    this._Activated.Remove(id);
            }
        }
        /// <summary>
        /// Metoda zajistí, že ve stavu Activated budou předané prvky.
        /// Stávající prvky může ponechat aktivní, nebo je může deaktivovat (podle parametru leaveOthers).
        /// </summary>
        /// <param name="items">Prvky, které mají být aktivní</param>
        /// <param name="leaveOthers">true = ponechat dosavadní aktivní prvky / false = deaktivovat ty z dosavadních prvků, které nejsou zadané v novém seznamu</param>
        public void SetActivatedItems(IEnumerable<IInteractiveItem> items, bool leaveOthers = false)
        {
            if (items == null) return;

            var itemDict = items.GetDictionary(i => i.Id, true);

            if (!leaveOthers)
            {
                // a) Vyberu si prvky, které nyní jsou ve this._Activated a aktuálně nejsou na vstupu => ty budeme odebírat:
                IInteractiveItem[] remove = this._Activated.Values.Where(i => !itemDict.ContainsKey(i.Id)).ToArray();

                // b) Na vybraných prvcích nastavím IsActivated = false, tím se ty prvky samy odeberou z this._Activated:
                remove.ForEachItem(i => i.IsActivated = false);

                // c) A pro jistotu z this._Activated odeberu dané prvky (to kdyby to prvek IInteractiveItem v property IsActivated neudělal sám):
                this._Activated.RemoveWhere((id, item) => !itemDict.ContainsKey(id));
            }
            
            // d) A poté přidám ty prvky, které jsou nyní na vstupu, ale ještě nejsou v Dictionary this._Activated:
            foreach (IInteractiveItem item in items)
            {
                if (!this._Activated.ContainsKey(item.Id))
                    item.IsActivated = true;                  // Prvek sám si zařídí přidání do dictionary _Activated, a zařídí si i Repaint().
            }
        }
        /// <summary>
        /// Obsahuje souhrn všech aktuálně aktivních prvků
        /// </summary>
        public IInteractiveItem[] ActivatedItems { get { return this._Activated.Values.ToArray(); } }
        #endregion
    }
    #region interface ISelectorInternal : pro přístup k výkonným metodám třídy Selector, které reálně změní hodnoty
    /// <summary>
    /// ISelectorInternal : Interface pro přístup k výkonným metodám třídy <see cref="Selector"/>, které reálně změní hodnoty
    /// </summary>
    public interface ISelectorInternal
    {
        /// <summary>
        /// Nastaví požadovanou hodnotu IsSelected
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isSelected"></param>
        void SetSelectedValue(IInteractiveItem item, bool isSelected);
        /// <summary>
        /// Nastaví požadovanou hodnotu IsFramed
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isFramed"></param>
        void SetFramedValue(IInteractiveItem item, bool isFramed);
        /// <summary>
        /// Nastaví požadovanou hodnotu IsActivated
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isActivated"></param>
        void SetActivatedValue(IInteractiveItem item, bool isActivated);
    }
    #endregion
}
