using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Djs.Common.Components
{
    /// <summary>
    /// Parent of BitStorage classes
    /// </summary>
    public abstract class BitStorage
    {
        /// <summary>
        /// Constructor. 
        /// Initialise this value to this.DefaultValue
        /// </summary>
        public BitStorage()
        {
            this._Value = this.DefaultValue;
        }
        /// <summary>
        /// Constructor. 
        /// Initialise this value to explicitly defaultValue
        /// </summary>
        /// <param name="defaultValue"></param>
        public BitStorage(UInt32 defaultValue)
        {
            this._Value = defaultValue;
        }
        private UInt32 _Value;
        /// <summary>
        /// Default value for new instance
        /// </summary>
        protected abstract UInt32 DefaultValue { get; }
        /// <summary>
        /// Return true / false from bit mask (one bit set).
        /// </summary>
        /// <param name="mask">Bit mask (for example: 0x02000 = bit 13)</param>
        /// <returns></returns>
        public bool GetBitValue(UInt32 mask)
        {
            return ((this._Value & mask) != 0);
        }
        /// <summary>
        /// Return true / false from bit mask (one bit set).
        /// </summary>
        /// <param name="mask">Bit mask (for example: 0x02000 = bit 13)</param>
        /// <param name="value">New value: true = set bit, false = reset bit</param>
        /// <returns></returns>
        public void SetBitValue(UInt32 mask, bool value)
        {
            this._Value = SetBitValue(this._Value, mask, value);
        }
        /// <summary>
        /// Return a new value for storage, where bit[s] by mask are set/cleared by value.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="mask"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static UInt32 SetBitValue(UInt32 storage, UInt32 mask, bool value)
        {
            if (value)
                return storage | mask;
            else
                return storage & (UInt32.MaxValue ^ mask);
        }
        /// <summary>
        /// Return a new value for storage, where bit[s] by mask are set/cleared by value.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="mask"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Int32 SetBitValue(Int32 storage, Int32 mask, bool value)
        {
            if (value)
                return storage | mask;
            else
                return storage & (Int32.MaxValue ^ mask);
        }
    }
}
