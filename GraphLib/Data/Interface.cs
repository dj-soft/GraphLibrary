using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    #region interface IOwnerProperty<T>
    /// <summary>
    /// Interface, který dovoluje vložit referenci na libovolného vlastníka
    /// </summary>
    public interface IOwnerProperty<T>
    {
        /// <summary>
        /// Vlastník tohoto objektu
        /// </summary>
        T Owner { get; set; }
    }
    #endregion
    #region delegate GPropertyChangedHandler, class GPropertyChangeArgs, enum EventSourceType
    /// <summary>
    /// Delegát pro handlery události, kdy došlo ke změně hodnoty na GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GPropertyChangedHandler<T>(object sender, GPropertyChangeArgs<T> e);
    /// <summary>
    /// Data pro eventhandler navázaný na změnu nějaké hodnoty v GInteractiveControl
    /// </summary>
    public class GPropertyChangeArgs<T> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="oldvalue"></param>
        /// <param name="newValue"></param>
        /// <param name="eventSource"></param>
        public GPropertyChangeArgs(T oldvalue, T newValue, EventSourceType eventSource)
        {
            this.OldValue = oldvalue;
            this.NewValue = newValue;
            this.EventSource = eventSource;
            this.CorrectValue = newValue;
            this.Cancel = false;
        }
        /// <summary>
        /// Hodnota platná před změnou
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Hodnota platná po změně
        /// </summary>
        public T NewValue { get; private set; }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSourceType EventSource { get; private set; }
        /// <summary>
        /// Hodnota odpovídající aplikační logice, hodnotu nastavuje eventhandler.
        /// Výchozí hodnota je NewValue.
        /// Komponenta by na tuto korigovanou hodnotu měla reagovat.
        /// </summary>
        public T CorrectValue { get; set; }
        /// <summary>
        /// Požadavek aplikačního kódu na zrušení této změny = ponechat OldValue.
        /// Výchozí hodnota je false.
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// true pokud hodnota CorrectValue je odlišná od OldValue, false pokud jsou shodné.
        /// Pokud typ hodnoty není IComparable, pak se vrací true vždy.
        /// </summary>
        public bool IsChangeValue
        {
            get
            {
                IComparable a = this.OldValue as IComparable;
                IComparable b = this.CorrectValue as IComparable;
                if (a == null || b == null) return true;
                return (a.CompareTo(b) != 0);
            }
        }
        /// <summary>
        /// Výsledná hodnota (pokud je Cancel == true, pak OldValue, jinak CorrectValue).
        /// </summary>
        public T ResultValue { get { return (this.Cancel ? this.OldValue : this.CorrectValue); } }
    }
    /// <summary>
    /// Specifies the source that caused this change
    /// </summary>
    [Flags]
    public enum EventSourceType
    {
        /// <summary>
        /// Neurčeno
        /// </summary>
        None = 0,
        /// <summary>
        /// Change to Value property directly (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueChanging = 0x0001,
        /// <summary>
        /// Change to Value property directly (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueChange = 0x0002,
        /// <summary>
        /// Change to ValueRange property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueRangeChange = 0x0010,
        /// <summary>
        /// Change to Scale property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueScaleChange = 0x0020,
        /// <summary>
        /// Change to ScaleRange property (from code or from GUI, by flag ApplicationCode or InteractiveChange)
        /// </summary>
        ValueScaleRangeChange = 0x0040,
        /// <summary>
        /// Change of control visual size
        /// </summary>
        BoundsChange = 0x0100,
        /// <summary>
        /// Change of control Orientation
        /// </summary>
        OrientationChange = 0x0200,
        /// <summary>
        /// Application code is source of change (set new value to property)
        /// </summary>
        ApplicationCode = 0x1000,
        /// <summary>
        /// Interactive action is source of change (user was entered / dragged new value).
        /// Change of value is still in process (Drag). After DragEnd will be sent event with source = InteractiveChanged.
        /// </summary>
        InteractiveChanging = 0x2000,
        /// <summary>
        /// Interactive action is source of change (user was entered / dragged new value)
        /// </summary>
        InteractiveChanged = 0x4000
    }
    #endregion
    #region delegate GPropertyChangedHandler, class GPropertyChangeArgs, enum EventSourceType
    /// <summary>
    /// Delegát pro handlery události, kdy došlo ke změně hodnoty na GInteractiveControl
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void GObjectPropertyChangedHandler<TObject, TValue>(object sender, GObjectPropertyChangeArgs<TObject, TValue> e);
    /// <summary>
    /// Data pro eventhandler navázaný na změnu nějaké hodnoty v určitém objektu
    /// </summary>
    public class GObjectPropertyChangeArgs<TObject, TValue> : EventArgs
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="currentObject"></param>
        /// <param name="oldvalue"></param>
        /// <param name="newValue"></param>
        /// <param name="eventSource"></param>
        public GObjectPropertyChangeArgs(TObject currentObject, TValue oldvalue, TValue newValue, EventSourceType eventSource)
        {
            this.CurrentObject = currentObject;
            this.OldValue = oldvalue;
            this.NewValue = newValue;
            this.EventSource = eventSource;
            this.CorrectValue = newValue;
            this.Cancel = false;
        }
        /// <summary>
        /// Objekt, v němž došlo ke změně
        /// </summary>
        public TObject CurrentObject { get; private set; }
        /// <summary>
        /// Hodnota platná před změnou
        /// </summary>
        public TValue OldValue { get; private set; }
        /// <summary>
        /// Hodnota platná po změně
        /// </summary>
        public TValue NewValue { get; private set; }
        /// <summary>
        /// Zdroj události
        /// </summary>
        public EventSourceType EventSource { get; private set; }
        /// <summary>
        /// Hodnota odpovídající aplikační logice, hodnotu nastavuje eventhandler.
        /// Výchozí hodnota je NewValue.
        /// Komponenta by na tuto korigovanou hodnotu měla reagovat.
        /// </summary>
        public TValue CorrectValue { get; set; }
        /// <summary>
        /// Požadavek aplikačního kódu na zrušení této změny = ponechat OldValue.
        /// Výchozí hodnota je false.
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// true pokud hodnota CorrectValue je odlišná od OldValue, false pokud jsou shodné.
        /// Pokud typ hodnoty není IComparable, pak se vrací true vždy.
        /// </summary>
        public bool IsChangeValue
        {
            get
            {
                IComparable a = this.OldValue as IComparable;
                IComparable b = this.CorrectValue as IComparable;
                if (a == null || b == null) return true;
                return (a.CompareTo(b) != 0);
            }
        }
        /// <summary>
        /// Výsledná hodnota (pokud je Cancel == true, pak OldValue, jinak CorrectValue).
        /// </summary>
        public TValue ResultValue { get { return (this.Cancel ? this.OldValue : this.CorrectValue); } }
    }
    #endregion
}
