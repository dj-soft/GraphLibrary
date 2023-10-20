using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{
    /// <summary>
    /// UndoRedo container. Dovoluje vkládat libovolné prvky, reprezentující "Krok zpět", 
    /// následně pak zjišťovat, zda nějaký existuje a dovoluje provést Undo i reverzní Redo.
    /// Obsahuje event o změně stavu, na který typicky reaguje Toolbar.
    /// </summary>
    public class UndoRedo : IDisposable
    {
        #region Konstruktor a privátní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public UndoRedo()
        {
            __Steps = new List<UndoRedoStep>();
            __PointerUndo = -1;
            __MaxStepsCount = 250;
            __LastAction = LastAction.None;
        }
        /// <summary>
        /// Jednotlivé kroky UndoRedo
        /// </summary>
        private List<UndoRedoStep> __Steps;
        /// <summary>
        /// Pointer na pozici kroku, který bych vrátil v nejbližším požadavku Undo.
        /// <para/>
        /// Pro prázdný kontejner obsahuje -1.<br/>
        /// Po vložení prvního prvku <see cref="Add(object)"/> obsahuje 0, po vložení dalšího prvku obsahuje 1, atd.<br/>
        /// Při požadavku <see cref="Undo()"/> se vrátí prvek na tomto indexu a tento index se o 1 sníží.<br/>
        /// Při stavu indexu menším než 0 se hlásí stav <see cref="CanUndo"/> = false, v tomto stavu požadavek <see cref="Undo()"/> vrátí null.<br/>
        /// Pokud hodnota indexu <see cref="__PointerUndo"/> ukazuje na prvek, za kterým je ještě nějaký další prvek, pak je možno provést <see cref="Redo()"/>, hlásí se <see cref="CanRedo"/> = true.<br/>
        /// Požadavek <see cref="Redo()"/> posune ukazatel <see cref="__PointerUndo"/> o 1 nahoru a vrátí prvek z daného indexu.<br/>
        /// Pokud je ukláádn nový stav metodou <see cref="Add(object)"/>, a za pointerem jsou další kroky v poli <see cref="__Steps"/>, pak tyto objekty jsou zahozeny.
        /// Nový prvek je uložen za prvek, na který nyní ukazuje <see cref="__PointerUndo"/>.<br/>
        /// Toto chování repezentuje "Několik kroků zpátky a potom krok dopředu jiným směrem".
        /// </summary>
        private int __PointerUndo;
        /// <summary>
        /// Maximální počet uložených kroků. Při přidání dalšího kroku nad tento počet jsou odebrány nejstarší kroky (od indexu 0).
        /// </summary>
        private int __MaxStepsCount;
        /// <summary>
        /// Nejmenší počet kroků <see cref="__MaxStepsCount"/>
        /// </summary>
        private const int __MaxStepCountMinimum = 12;
        /// <summary>
        /// Nejvyšší počet kroků <see cref="__MaxStepsCount"/>
        /// </summary>
        private const int __MaxStepCountMaximum = 1000;
        /// <summary>
        /// Posledně provedená akce. Slouží ke správné synchronizaci indexu při změně Undo = Redo.
        /// </summary>
        private LastAction __LastAction;
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            __Steps.Clear();
            __Steps = null;
            __PointerUndo = -1;
            __LastAction = LastAction.Dispose;
        }
        #endregion
        #region Public
        /// <summary>
        /// Může container provést akci Undo?
        /// </summary>
        public bool CanUndo { get { return _CanUndo(_CurrentState); } }
        /// <summary>
        /// Může container provést akci Redo?
        /// </summary>
        public bool CanRedo { get { return _CanRedo(_CurrentState); } }
        /// <summary>
        /// Nejvyšší počet uložených kroků v tomto kontejneru. Při vložení dalšího kroku metodou <see cref="Add(object)"/>, který by byl nad tento počet, se nejstarší krok zahodí.
        /// </summary>
        public int MaxStepsCount { get { return __MaxStepsCount; } set { __MaxStepsCount = (value <= __MaxStepCountMinimum ? __MaxStepCountMinimum : (value >= __MaxStepCountMaximum ? __MaxStepCountMaximum : value)); _RemoveOlds(0); } }
        /// <summary>
        /// Aktuální stav containeru = jakou akci může provést.
        /// Při změně se volá event <see cref="CurrentStateChanged"/>.
        /// </summary>
        public State CurrentState { get { return _CurrentState; } }
        public event EventHandler CurrentStateChanged;
        /// <summary>
        /// Vloží dodaný stav do containeru
        /// </summary>
        /// <param name="data"></param>
        public void Add(object data)
        {
            var oldState = _CurrentState;
            _RemoveRedo();
            _RemoveOlds(1);
            _AddStep(data);
            _RunChanged(oldState);
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Zpět
        /// </summary>
        /// <param name="data"></param>
        public object Undo()
        {
            var oldState = _CurrentState;
            var data = _Undo();
            _RunChanged(oldState);
            return data;
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Vpřed
        /// </summary>
        /// <param name="data"></param>
        public object Redo()
        {
            var oldState = _CurrentState;
            var data = _Redo();
            _RunChanged(oldState);
            return data;
        }
        #endregion
        #region Private
        /// <summary>
        /// Pokud existují prvky, které bych mohl vrátit jako Redo, pak je odeberu - protože se budou vkládat nové prvky.
        /// </summary>
        private void _RemoveRedo()
        {
            int count = __Steps.Count;
            int pointer = __PointerUndo;
            if (pointer >= (count - 1)) return;            // Pokud pointer ukazuje na poslední prvek (nebo za něj?), pak neexistují prvky ZA pointerem a nebudeme nic odebírat...

            int removeIndex = pointer + 1;
            if (removeIndex < 0) removeIndex = 0;
            int removeCount = count - removeIndex;
            __Steps.RemoveRange(removeIndex, removeCount);
        }
        /// <summary>
        /// Pokud by container obsahoval více prvků (nebo stejně) než je povoleno, pak nejstarší prvky odebere.
        /// Následovat bude přidání jednoho prvku.
        /// </summary>
        private void _RemoveOlds(int addCount)
        {
            int currentCount = __Steps.Count;
            int maximumCount = __MaxStepsCount;
            int removeCount = (currentCount + addCount) - maximumCount;

            if (removeCount <= 0) return;                  // Zatím jsme nepřesáhli MAX počet (s přídavkem addCount), nemusíme nic zahazovat...
            if (removeCount >= currentCount)
            {   // Z nějakého důvodu máme odbrat všechno:
                __Steps.Clear();
                __PointerUndo = -1;
            }
            else
            {   // Odebereme několik prvků, a další poté zůstanou:
                __Steps.RemoveRange(0, removeCount);
                __PointerUndo -= removeCount;
                if (__PointerUndo < -1) __PointerUndo = -1;
            }
        }
        /// <summary>
        /// Vloží daný objekt jako nový Undo Step, a nastaví Pointer na tento prvek
        /// </summary>
        /// <param name="data"></param>
        private void _AddStep(object data)
        {
            UndoRedoStep step = new UndoRedoStep(data);
            __Steps.Add(step);
            __PointerUndo = __Steps.Count - 1;
            __LastAction = LastAction.Add;
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Zpět
        /// </summary>
        /// <param name="data"></param>
        private object _Undo()
        {
            int count = __Steps.Count;
            if (count == 0) return null;

            int pointer = __PointerUndo;
            if (__LastAction == LastAction.Redo) pointer--;          // Pokud jsem poslední akci měl Redo, pak ukazuji na prvek posledně vrácený, takže pro Undo se musím vrátit o 1 dolů
            if (pointer < 0) return null;

            if (pointer >= count) pointer = count - 1;
            var data = __Steps[pointer].Data;
            __PointerUndo = pointer - 1;
            __LastAction = LastAction.Undo;

            return data;
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Vpřed
        /// </summary>
        /// <param name="data"></param>
        private object _Redo()
        {
            int count = __Steps.Count;
            if (count == 0) return null;

            int pointer = __PointerUndo;
            if (__LastAction == LastAction.Undo) pointer++;          // Pokud jsem poslední akci dal Undo, pak ukazuji na prvek před prvkem posledně vráceným, a obyčejná +1 mě vrátí na týž prvek, který jsem posledně vrátil...  Přidám tedy dvakrát +1 !
            if (pointer >= (count - 1)) return null;

            pointer++;
            if (pointer < 0) pointer = 0;
            var data = __Steps[pointer].Data;
            __PointerUndo = pointer;
            __LastAction = LastAction.Redo;

            return data;
        }
        /// <summary>
        /// Posledně prováděná akce
        /// </summary>
        private enum LastAction
        {
            None,
            Add,
            Undo,
            Redo,
            Dispose
        }
        /// <summary>
        /// Pokud daný starý stav se liší od aktuálního, pak zavolá eventhandler <see cref="CurrentStateChanged"/>
        /// </summary>
        /// <param name="oldState"></param>
        private void _RunChanged(State oldState)
        {
            State newState = _CurrentState;
            if (newState != oldState)
                CurrentStateChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>
        /// Aktuální stav kontejneru, právě nyní vypočtený
        /// </summary>
        private State _CurrentState
        {
            get
            {
                if (__Steps is null || __Steps.Count == 0) return State.Empty;
                int count = __Steps.Count;
                int pointer = __PointerUndo;
                bool canUndo = (pointer >= 0);             // Undo mohu provést, když Pointer ukazuje na konkrétní prvek (i když Pointer = 0, mohu vrátit prvek na indexu [0]
                bool canRedo = (pointer < (count - 1));    // Redo mohu provést, když za prvkem na který ukazuje Pointer je ještě další prvek
                return ((canUndo && canRedo) ? State.CanUndoRedo :
                         canUndo ? State.CanUndo :
                         canRedo ? State.CanRedo : State.Empty);
            }
        }
        /// <summary>
        /// Vrátí true, pokud za daného stavu lze provést Undo
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool _CanUndo(State state) { return (state == State.CanUndo || state == State.CanUndoRedo); }
        /// <summary>
        /// Vrátí true, pokud za daného stavu lze provést Redo
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool _CanRedo(State state) { return (state == State.CanRedo || state == State.CanUndoRedo); }
        /// <summary>
        /// Stav kontejneru
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Kontejner je prázdný
            /// </summary>
            Empty,
            /// <summary>
            /// Mohu provést Undo
            /// </summary>
            CanUndo,
            /// <summary>
            /// Mohu provést Redo
            /// </summary>
            CanRedo,
            /// <summary>
            /// Mohu provést Undo i Redo
            /// </summary>
            CanUndoRedo
        }
        /// <summary>
        /// Jeden krok UndoRedo containeru
        /// </summary>
        private class UndoRedoStep
        {
            public UndoRedoStep(object data)
            {
                __Data = data;
            }
            public override string ToString()
            {
                return this.Data?.ToString();
            }
            public object Data { get { return __Data; } }
            private object __Data;
        }
        #endregion
    }
}
