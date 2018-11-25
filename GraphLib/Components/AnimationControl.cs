using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Asol.Tools.WorkScheduler.Data;

namespace Asol.Tools.WorkScheduler.Components
{
    /// <summary>
    /// Řídící prvek jakékoli animace
    /// </summary>
    public class AnimationControl<T>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public AnimationControl()
        {
            this._AnimationList = new List<Tuple<int, T>>();
        }
        private List<Tuple<int, T>> _AnimationList;
        private int _NextIndex;
        private int? _WaitSteps;
        /// <summary>
        /// Metoda vymaže všechna data animátoru, a dá Rewind
        /// </summary>
        public void Clear()
        {
            lock (this._AnimationList)
            {
                this._AnimationList.Clear();
            }
            this.Rewind();
        }
        /// <summary>
        /// Metoda přetočí animaci na začátek
        /// </summary>
        public void Rewind()
        {
            this._NextIndex = 0;
            this._WaitSteps = 0;
        }
        /// <summary>
        /// Metoda přidá do animátoru pauzu v daném počtu cyklů.
        /// Standardní animace má 25 cyklů za sekundu.
        /// </summary>
        /// <param name="cycles"></param>
        public void AddPause(int cycles)
        {
            if (cycles <= 0) return;
            lock (this._AnimationList)
            {
                this._AnimationList.Add(new Tuple<int, T>(cycles, default(T)));
            }
        }
        /// <summary>
        /// Metoda přidá do animátoru pauzu v dané délce.
        /// Standardní animace má 25 cyklů za sekundu, s tím počítá přepočet v této metodě.
        /// </summary>
        /// <param name="time"></param>
        public void AddPause(TimeSpan time)
        {
            if (time.TotalMilliseconds < 40d) return;
            int cycles = (int)(Math.Ceiling((time.TotalMilliseconds / 40d)));
            this.AddPause(cycles);
        }
        /// <summary>
        /// Metoda přidá do animátoru jeden krok animace
        /// </summary>
        /// <param name="animationStep"></param>
        public void AddStep(T animationStep)
        {
            lock (this._AnimationList)
            {
                this._AnimationList.Add(new Tuple<int, T>(0, animationStep));
            }
        }
        /// <summary>
        /// Metoda přidá do animátoru několik kroků animace
        /// </summary>
        /// <param name="animationSteps"></param>
        public void AddSteps(IEnumerable<T> animationSteps)
        {
            if (animationSteps == null) return;
            lock (this._AnimationList)
            {
                foreach (T step in animationSteps)
                    this._AnimationList.Add(new Tuple<int, T>(0, step));
            }
        }
        /// <summary>
        /// Metoda vynuluje animátor, a přidá do animátoru několik kroků animace
        /// </summary>
        /// <param name="animationSteps"></param>
        public void StoreSteps(IEnumerable<T> animationSteps)
        {
            if (animationSteps == null) return;
            lock (this._AnimationList)
            {
                this.Clear();
                foreach (T step in animationSteps)
                    this._AnimationList.Add(new Tuple<int, T>(0, step));
            }
        }
        /// <summary>
        /// Obsahuje true, když animace doběhla do svého konce.
        /// Řídící vrstva může v této situaci zavolat metodu <see cref="Rewind()"/>, pak se animace nastaví na začátek, 
        /// a pokud existují nějaké kroky animace, pak v <see cref="IsEnd"/> bude false.
        /// </summary>
        public bool IsEnd
        {
            get { return (this._WaitSteps == 0 && this._NextIndex >= this._AnimationList.Count); }
        }
        /// <summary>
        /// Metoda provede další krok animace.
        /// Vrátí true = animátor vydal další data (generického typu) pro provedení animace, jsou v out parametru.
        /// Vrátí false = animátor provádí čekání mezi kroky animace.
        /// Vrátí false i v případě, že animátor došel do konce, a nemá žádný další krok.
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool Tick(out T step)
        {
            lock (this._AnimationList)
            {
                return this._Tick(out step);
            }
        }
        private bool _Tick(out T step)
        {
            if (this._WaitSteps > 0)
            {   // Provádíme čekání:
                this._WaitSteps = this._WaitSteps - 1;
                step = default(T);
                return false;
            }
            if (this._NextIndex >= this._AnimationList.Count)
            {   // Nemáme žádná data:
                step = default(T);
                return false;
            }

            // Nečekáme, a máme data:
            Tuple<int, T> data = this._AnimationList[this._NextIndex];
            this._NextIndex++;
            if (data.Item1 > 0)
            {   // Zahajujeme čekání:
                this._WaitSteps = data.Item1;
                step = default(T);
                return false;
            }

            // Vyzvedli jsme krok animace, dáme jej do out parametru a vrátíme true:
            step = data.Item2;
            return true;
        }
    }
}
