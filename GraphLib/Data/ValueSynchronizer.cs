using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asol.Tools.WorkScheduler.Application;

namespace Asol.Tools.WorkScheduler.Data
{
    #region class ValueTimeRangeSynchronizer : potomek ValueSynchronizer pro typ TimeRange
    /// <summary>
    /// ValueTimeRangeSynchronizer : potomek třídy <see cref="ValueSynchronizer{T}"/> pro typ hodnoty <see cref="TimeRange"/>.
    /// </summary>
    public class ValueTimeRangeSynchronizer : ValueSynchronizer<TimeRange>
    {
        /// <summary>
        /// Limitní hodnota v synchronizeru.
        /// Hodnotu lze číst i zapisovat.
        /// Tato hodnota může mít jednu nebo obě strany (Begin i End¨) = null, pak v daném směru neplatí omezení.
        /// </summary>
        public TimeRange ValueLimit { get { return this._ValueLimit; } set { this._ValueLimit = value; } } private TimeRange _ValueLimit = null;
        /// <summary>
        /// Metoda má zajistit zarovnání hodnoty TimeRange do stanovených mezí, podle hodnoty <see cref="ValueLimit"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override TimeRange AlignValueToLimit(TimeRange value)
        {
            TimeRange limit = this.ValueLimit;
            if (limit == null) return value;
            DateTime begin = value.Begin.Value;
            TimeSpan size = value.Size.Value;
            DateTime end = value.End.Value;

            if (limit.End.HasValue && end > limit.End.Value)
            {
                end = limit.End.Value;
                begin = end - size;
            }
            if (limit.Begin.HasValue && begin < limit.Begin.Value)
            {
                begin = limit.Begin.Value;
                end = begin + size;
                if (limit.End.HasValue && end > limit.End.Value)
                    // Pouze v případě, kdy limit definuje menší prostor (size) než je prostor dané hodnoty, tak se daná hodnota musí zmenšit:
                    //  = upravil se begin i end podle limitu, a ztratila se původní velikost size.
                    end = limit.End.Value;
            }
            return new TimeRange(begin, end);
        }
    }
    #endregion
    #region class ValueSynchronizer : třída, která dokáže zajistit synchronní hodnotu ve vícero navázaných objektech.
    /// <summary>
    /// ValueSynchronizer : třída, která dokáže zajistit synchronní hodnotu ve vícero navázaných objektech.
    /// K jednomu synchronizeru se může napojit více 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueSynchronizer<T>
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ValueSynchronizer()
        {
            this.SynchronizerId = App.GetNextId(typeof(System.ValueType));
            if (AllSynchronizerList == null)
                AllSynchronizerList = new List<object>();
            AllSynchronizerList.Add(this);
        }
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ValueSynchronizer #" + this.SynchronizerId.ToString() + "; Value: " + (this.Value == null ? "NULL" : this.Value.ToString());
        }
        /// <summary>
        /// ID tohoto konkrétního synchronizeru
        /// </summary>
        protected int SynchronizerId { get; private set; }
        /// <summary>
        /// Pole všech synchronizerů
        /// </summary>
        protected static List<object> AllSynchronizerList;
        /// <summary>
        /// Aktuální hodnota v synchronizeru.
        /// Hodnotu lze číst i zapisovat.
        /// Zápis hodnoty (takto napřímo) vyvolá událost <see cref="ValueChanging"/>, kde ale do eventhandleru bude předán objekt sender == null.
        /// Pokud je potřeba identifikovat odesílatele události, je třeba novou hodnotu vkládat pomocí metody <see cref="SetValue(T, object)"/> nebo <see cref="SetValue(T, object, EventSourceType)"/>.
        /// </summary>
        public T Value { get { return this._Value; } set { this._SetValue(null, value, EventSourceType.ApplicationCode); } }
        /// <summary>
        /// Metoda vloží dodanou hodnotu do this instance synchronizeru. Do eventu <see cref="ValueChanging"/> se odesílá i daný sender.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="sender"></param>
        public void SetValue(T value, object sender) { this._SetValue(sender, value, EventSourceType.ApplicationCode); }
        /// <summary>
        /// Metoda vloží dodanou hodnotu do this instance synchronizeru. Do eventu <see cref="ValueChanging"/> se odesílá i daný sender a zdroj události.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="sender"></param>
        /// <param name="eventSource"></param>
        public void SetValue(T value, object sender, EventSourceType eventSource) { this._SetValue(sender, value, eventSource); }
        private T _Value;
        /// <summary>
        /// Obsahuje true v době, kdy se provádí eventhandler <see cref="ValueChanging"/> (prostřednictvím metody <see cref="CallValueChanging(object, GPropertyChangeArgs{T})"/>).
        /// Pokud je true, pak volání metody <see cref="_SetValue(object, T, EventSourceType)"/> nic neprovádí (ignoruje se); to je ochranou proti zacyklení.
        /// </summary>
        protected bool _ValueIsChanging;
        /// <summary>
        /// Provede vložení hodnoty, a vyvolání eventu <see cref="ValueChanging"/>.
        /// Pokud ale na začátku je <see cref="_ValueIsChanging"/> == true, pak nedělá nic; to je ochranou proti zacyklení.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        /// <param name="eventSource"></param>
        protected void _SetValue(object sender, T value, EventSourceType eventSource)
        {
            if (this._ValueIsChanging) return;
            try
            {
                this._ValueIsChanging = true;

                T oldValue = this._Value;
                T newValue = this.AlignValueToLimit(value);
                if (!this.IsEqual(oldValue, newValue))
                {
                    this._Value = newValue;

                    GPropertyChangeArgs<T> args = new GPropertyChangeArgs<T>(oldValue, newValue, eventSource);
                    this.CallValueChanging(sender, args);

                    this._Value = args.CorrectValue;
                }
            }
            finally
            {
                this._ValueIsChanging = false;
            }
        }
        /// <summary>
        /// Metoda má zajistit zarovnání hodnoty T do stanovených mezí.
        /// Bázová generická třída <see cref="ValueSynchronizer{T}"/> nemá implementované limity hodnot, proto vždy vrací vstupní hodnotu.
        /// Potomek může tuto metodu přepsat, a zajistit omezení hodnot sám.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual T AlignValueToLimit(T value) { return value; }
        /// <summary>
        /// Metoda vrátí true, pokud dané dvě hodnoty jsou stejné. 
        /// Pokud jsou stejné, tak nemá význam měnit hodnotu a volat eventy.
        /// Bázová třída využívá metodu Equals() na třídě T.
        /// Potomek může přepsat.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        protected virtual bool IsEqual(T oldValue, T newValue)
        {
            bool an = (((Object)oldValue) == null);
            bool bn = (((Object)newValue) == null);
            if (an && bn) return true;
            if (an || bn) return false;
            return oldValue.Equals(newValue);
        }
        /// <summary>
        /// Metoda vyvolá háček <see cref="OnValueChanging(object, GPropertyChangeArgs{T})"/> a event <see cref="ValueChanging"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected void CallValueChanging(object sender, GPropertyChangeArgs<T> args)
        {
            this.OnValueChanging(sender, args);
            if (this.ValueChanging != null)
                this.ValueChanging(sender, args);
        }
        /// <summary>
        /// Protected háček při změně hodnoty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void OnValueChanging(object sender, GPropertyChangeArgs<T> args)
        { }
        /// <summary>
        /// Událost, vyvolaná po změně hodnoty (odkudkoliv)
        /// </summary>
        public event GPropertyChangedHandler<T> ValueChanging;
    }
    #endregion
}
