using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
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
}
