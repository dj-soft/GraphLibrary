using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Data
{
    /// <summary>
    /// Identifikátor záznamu: číslo třídy a číslo záznamu
    /// </summary>
    public class GId
    {
        #region Konstrukce, data
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="recordId"></param>
        public GId(int classId, int recordId)
        {
            this.ClassId = classId;
            this.RecordId = recordId;
            this.HashCode = (classId << 16) | recordId;
        }
        /// <summary>
        /// Číslo třídy
        /// </summary>
        public int ClassId { get; private set; }
        /// <summary>
        /// Číslo záznamu
        /// </summary>
        public int RecordId { get; private set; }
        /// <summary>
        /// Privátní uložený hashcode
        /// </summary>
        private int HashCode { get; set; }
        #endregion
        #region Overrides, equals, ==
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ClassId: {this.ClassId}; RecordId: {this.RecordId}";
        }
       
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode;
        }
        /// <summary>
        /// Equals() - pro použití GID v Hashtabulkách
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is GId)) return false;
            return (GId._IsEqual(this, (GId)obj));
        }
        /// <summary>
        /// Porovnání dvou instancí této struktury, zda obsahují shodná data
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool _IsEqual(GId a, GId b)
        {
            return (a.RecordId == b.RecordId && a.ClassId == b.ClassId);
        }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(GId a, GId b)
        {
            return GId._IsEqual(a, b);
        }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(GId a, GId b)
        {
            return (!GId._IsEqual(a, b));
        }
        #endregion
    }
}
