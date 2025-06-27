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
    public class UndoRedo<TData> : IDisposable
    {
        #region Konstruktor a privátní život
        /// <summary>
        /// Konstruktor
        /// </summary>
        public UndoRedo()
        {
            __Steps = new List<UndoRedoStep>();
            __MaxStepsCount = 250;
            _Clear();
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
        /// Pokud je ukládán nový stav metodou <see cref="Add(object)"/>, a za pointerem <see cref="__PointerUndo"/> by byly nějaké další kroky v poli <see cref="__Steps"/>, pak tyto objekty jsou zahozeny, už nebude možno je obnovit metodou Redo.
        /// Nový prvek je uložen za prvek, na který nyní ukazuje <see cref="__PointerUndo"/>.<br/>
        /// Toto chování repezentuje "Několik kroků zpátky a potom krok dopředu jiným směrem".
        /// </summary>
        private int __PointerUndo;
        /// <summary>
        /// Pointer na pozici kroku, který bych vrátil v nejbližším požadavku Redo.
        /// <para/>
        /// Pro prázdný kontejner obsahuje -1, stejně tak po přidání prvku metodou <see cref="Add(object)"/> = není možno provést akci Redo.<br/>
        /// Po provedení akce Undo je do tohoto ukazatele uložen index prvku, který by byl vrácen metodou Redo.
        /// Po provedení akce Redo je index posunut o +1 na další prvek v řadě akcí Redo.
        /// </summary>
        private int __PointerRedo;
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
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _Clear();
            __Steps = null;
        }
        #endregion
        #region Public
        /// <summary>
        /// Může container provést akci Undo?
        /// </summary>
        public bool CanUndo { get { return _CanUndo; } }
        /// <summary>
        /// Může container provést akci Redo?
        /// </summary>
        public bool CanRedo { get { return _CanRedo; } }
        /// <summary>
        /// Nejvyšší počet uložených kroků v tomto kontejneru. Při vložení dalšího kroku metodou <see cref="Add(object)"/>, který by byl nad tento počet, se nejstarší krok zahodí.
        /// </summary>
        public int MaxStepsCount { get { return __MaxStepsCount; } set { __MaxStepsCount = (value <= __MaxStepCountMinimum ? __MaxStepCountMinimum : (value >= __MaxStepCountMaximum ? __MaxStepCountMaximum : value)); _RemoveOlds(0); } }
        /// <summary>
        /// Aktuální stav containeru = jakou akci může provést.
        /// Při změně se volá event <see cref="CurrentStateChanged"/>.
        /// </summary>
        public UndoRedoState CurrentState { get { return _CurrentState; } }
        public event EventHandler CurrentStateChanged;
        /// <summary>
        /// Vloží dodaný stav do containeru
        /// </summary>
        /// <param name="data"></param>
        public void Add(TData data)
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
        public TData Undo()
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
        public TData Redo()
        {
            var oldState = _CurrentState;
            var data = _Redo();
            _RunChanged(oldState);
            return data;
        }
        /// <summary>
        /// Vymaže interní paměť všech kroků
        /// </summary>
        public void Clear()
        {
            var oldState = _CurrentState;
            _Clear();
            _RunChanged(oldState);
        }
        /// <summary>
        /// Událost je volána při provádění prvního Undo kroku, kdy aplikace může mít k dispozici aktuální stav dat, který není uložen v UndoRedo containeru.
        /// UndoRedo container si v této situaci data vyžádá a uloží do zásobníku jako "aktuální", a tím umožní po Undo provést Redo do aktuálního stavu dat.
        /// Pokud aplikace tento event nepoužije, anebo v něm nenaplní property <see cref="CatchCurrentRedoDataEventArgs.RedoData"/>, pak po prvním Undo nebude možno provést Redo.
        /// </summary>
        public event EventHandler<CatchCurrentRedoDataEventArgs> CatchCurrentRedoData;
        /// <summary>
        /// Data pro event <see cref="CatchCurrentRedoData"/>
        /// </summary>
        public class CatchCurrentRedoDataEventArgs : EventArgs
        {
            /// <summary>
            /// Konstruktor
            /// </summary>
            public CatchCurrentRedoDataEventArgs() 
            {
                __HasRedoData = false;
            }
            /// <summary>
            /// Data získaná z aktuálního dokumentu, určená pro nejbližší Redo krok
            /// </summary>
            public TData RedoData { get { return __RedoData; } set { __RedoData = value; __HasRedoData = true; } } private TData __RedoData;
            /// <summary>
            /// Obsahuje true poté, kdy do <see cref="RedoData"/> byl setován nějaký objekt.
            /// </summary>
            public bool HasRedoData { get { return __HasRedoData; } } private bool __HasRedoData;
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
        private void _AddStep(TData data)
        {
            UndoRedoStep step = new UndoRedoStep(data);
            __Steps.Add(step);
            __PointerUndo = __Steps.Count - 1;
            __PointerRedo = -1;
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Zpět
        /// </summary>
        /// <param name="data"></param>
        private TData _Undo()
        {
            if (!_CanUndo) return default;

            _AddCurrentDataAsNextRedo();

            int pointer = __PointerUndo;
            var data = __Steps[pointer].Data;

            __PointerUndo = pointer - 1;
            __PointerRedo = pointer + 1;

            return data;
        }
        /// <summary>
        /// Speciální operace, která zajistí uložení aktuálního stavu dat jako nejbližší krok Redo pro první Undo v řadě.
        /// Provádí se pouze tehdy, když je možno provést Undo, a přitom ukazatel <see cref="__PointerRedo"/> je záporný 
        /// = jsme ve stavu, kdy byl vložen nějaký datový stav, a možná jsou aktuální data změněna.
        /// Získání dat provede event , pokud je implmentován.
        /// </summary>
        private void _AddCurrentDataAsNextRedo()
        {
            // Za některé situace nebudeme event provádět:
            if (!_CanUndo || __PointerRedo >= 0 || CatchCurrentRedoData is null) return;

            CatchCurrentRedoDataEventArgs args = new CatchCurrentRedoDataEventArgs();
            CatchCurrentRedoData(null, args);
            if (!args.HasRedoData) return;

            // Máme data z aplikace => přidám je na konec pole __Steps, ale nebudu měnit indexy:
            __Steps.Add(new UndoRedoStep(args.RedoData));
        }
        /// <summary>
        /// Vrátí objekt uložený jako krok Vpřed
        /// </summary>
        /// <param name="data"></param>
        private TData _Redo()
        {
            if (!_CanRedo) return default;

            int pointer = __PointerRedo;
            var data = __Steps[pointer].Data;

            __PointerUndo = pointer - 1;
            __PointerRedo = pointer + 1;

            return data;
        }
        /// <summary>
        /// Pokud daný starý stav se liší od aktuálního, pak zavolá eventhandler <see cref="CurrentStateChanged"/>
        /// </summary>
        /// <param name="oldState"></param>
        private void _RunChanged(UndoRedoState oldState)
        {
            UndoRedoState newState = _CurrentState;
            if (newState != oldState)
                CurrentStateChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>
        /// Aktuální stav kontejneru, právě nyní vypočtený
        /// </summary>
        private UndoRedoState _CurrentState
        {
            get
            {
                if (__Steps is null || __Steps.Count == 0) return UndoRedoState.Empty;
                bool canUndo = _CanUndo;
                bool canRedo = _CanRedo;
                return ((canUndo && canRedo) ? UndoRedoState.CanUndoRedo :
                         canUndo ? UndoRedoState.CanUndo :
                         canRedo ? UndoRedoState.CanRedo : UndoRedoState.Empty);
            }
        }
        /// <summary>
        /// Vrátí true, pokud aktuálně lze provést Undo
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool _CanUndo
        {
            get
            {
                int count = __Steps.Count;
                if (count == 0) return false;
                int pointerUndo = __PointerUndo;
                return (pointerUndo >= 0 && pointerUndo <= (count - 1));
            }
        }
        /// <summary>
        /// Vrátí true, pokud aktuálně lze provést Redo
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool _CanRedo
        {
            get
            {
                int count = __Steps.Count;
                if (count == 0) return false;
                int pointerRedo = __PointerRedo;
                return (pointerRedo >= 0 && pointerRedo <= (count - 1));
            }
        }
        /// <summary>
        /// Vymaže obsah instance do výchozího stavu
        /// </summary>
        private void _Clear()
        {
            __Steps.Clear();
            __PointerUndo = -1;
            __PointerRedo = -1;
        }
        /// <summary>
        /// Jeden krok UndoRedo containeru
        /// </summary>
        private class UndoRedoStep
        {
            public UndoRedoStep(TData data)
            {
                __Data = data;
            }
            public override string ToString()
            {
                return this.Data?.ToString();
            }
            public TData Data { get { return __Data; } }
            private TData __Data;
        }
        #endregion
    }
    #region Enumy a testy
    /// <summary>
    /// Stav kontejneru UndoRedo
    /// </summary>
    public enum UndoRedoState
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
    /// Třída pro testování kontejneru UndoRedo
    /// </summary>
    [TestClass]
    public class UndoRedoTest
    {
        #region Testy
        /// <summary>
        /// Provede testy
        /// </summary>
        [TestMethod]
        public static void RunTest1()
        {
            string d0 = "A";
            string d1 = "B";
            using (var undoRedo = new UndoRedo<string>())
            {
                undoRedo.CatchCurrentRedoData += catchCurrentRedoData;

                undoRedo.Add(d0);
                var a1 = undoRedo.Undo();
                var a2 = undoRedo.Redo();

                if (a1 != d0) TestManager.AddError($"Undo != data: '{a1}' != {d0}");
                if (a2 != d1) TestManager.AddError($"Redo != data: '{a1}' != {d0}");
            }

            void catchCurrentRedoData(object sender, UndoRedo<string>.CatchCurrentRedoDataEventArgs e)
            {
                e.RedoData = d1;
            }
        }

        /// <summary>
        /// Provede testy
        /// </summary>
        [TestMethod]
        public static void RunTest2()
        {
            var da = "A";
            var db = "B";
            var dc = "C";
            var dd = "D";
            var de = "E";
            var df = "F";
            var dx = "X";
            using (var undoRedo = new UndoRedo<string>())
            {
                undoRedo.CatchCurrentRedoData += catchCurrentRedoData;

                undoRedo.Add(da);
                undoRedo.Add(db);
                undoRedo.Add(dc);
                undoRedo.Add(dd);                //          stav  A B C D

                var u1d = undoRedo.Undo();       // D        stav  A B C D X
                var u1c = undoRedo.Undo();       // C        stav  A B C D X
                undoRedo.Add(de);                //          stav  A B E
                undoRedo.Add(df);                //          stav  A B E F
                var u2f = undoRedo.Undo();       // F        stav  A B E F X
                var u2e = undoRedo.Undo();       // E        stav  A B E F X
                var u2b = undoRedo.Undo();       // B        stav  A B E F X
                var r3e = undoRedo.Redo();       // E        stav  A B E F X
                var r3f = undoRedo.Redo();       // F        stav  A B E F X
                var r3x = undoRedo.Redo();       // X        stav  A B E F X
                var u4f = undoRedo.Undo();
                var u4e = undoRedo.Undo();
                var u4b = undoRedo.Undo();
                var u4a = undoRedo.Undo();
                var u4n = undoRedo.Undo();        // null
            }

            void catchCurrentRedoData(object sender, UndoRedo<string>.CatchCurrentRedoDataEventArgs e)
            {
                e.RedoData = dx;
            }
        }
        #endregion
    }
    #endregion
}
