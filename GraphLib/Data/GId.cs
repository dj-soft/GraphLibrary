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
        /// Název třídy záznamu
        /// </summary>
        public string ClassName { get { return Scheduler.GreenClasses.GetClassName(this.ClassId); } }
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
            string name = Scheduler.GreenClasses.GetClassName(this.ClassId);
            return "C:" + this.ClassId + "; R:" + this.RecordId + (name == null ? "" : "; \"" + name + "\"");
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
            bool an = ((object)a) == null;
            bool bn = ((object)b) == null;
            if (an && bn) return true;           // null == null
            if (an || bn) return false;          // (any object) != null
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
    /// <summary>
    /// Strana vztahu, na které najdeme Mastera.
    /// </summary>
    public enum RelationMasterSide
    {
        /// <summary>
        /// Master je vlevo: u statického vztahu je Master ten záznam, v jehož databázovém sloupci je uloženo číslo záznamu vztaženého.
        /// Left tedy znamená, že v tomto vztahu je je vlevo Master, z něj načteme číslo vztaženého záznamu (Slave), který bude zobrazen vpravo.
        /// </summary>
        Left,
        /// <summary>
        /// Master je vpravo, vlevo je záznam Slave.
        /// </summary>
        Right
    }
}
