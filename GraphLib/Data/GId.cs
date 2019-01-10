using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Noris.LCS.Base.WorkScheduler;

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
        }
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="classId">Číslo třídy</param>
        /// <param name="recordId">Číslo záznamu (MasterId)</param>
        /// <param name="entryId">Číslo položky (EntryId)</param>
        public GId(int classId, int recordId, int entryId)
        {
            this.ClassId = classId;
            this.RecordId = recordId;
            this.EntryId = entryId;
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
        /// Číslo položky = Entry (objekt).
        /// Pokud je null (=nemá hodnotu), pak jde o ID Master záznamu.
        /// </summary>
        public int? EntryId { get; private set; }
        /// <summary>
        /// true pro prázdný ID (kdy <see cref="ClassId"/> i <see cref="RecordId"/> == 0 a <see cref="EntryId"/> nemá hodnotu)
        /// </summary>
        public bool IsEmpty { get { return (this.ClassId == 0 && this.RecordId == 0 && !this.EntryId.HasValue); } }
        /// <summary>
        /// Název třídy záznamu
        /// </summary>
        public string ClassName { get { return Scheduler.GreenClasses.GetClassName(this.ClassId); } }
        #endregion
        #region Overrides, equals, ==
        /// <summary>
        /// Vizualizace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string name = Scheduler.GreenClasses.GetClassName(this.ClassId);
            return "C:" + this.ClassId + "; R:" + this.RecordId + 
                (this.EntryId.HasValue ? "; E:" + this.EntryId.Value.ToString() : "") + 
                (name == null ? "" : "; \"" + name + "\"");
        }
        /// <summary>
        /// Obsahuje textové vyjádření zdejších dat
        /// </summary>
        public string Name
        {
            get { return this.ClassId.ToString() + ":" + this.RecordId.ToString() + (this.EntryId.HasValue ? ":" + this.EntryId.Value.ToString() : ""); }
            private set
            {
                int classId = 0;
                int recordId = 0;
                int entryId = 0;
                bool isValid = false;
                bool isEntry = false;
                if (!String.IsNullOrEmpty(value) && value.Contains(":"))
                {
                    string[] items = value.Split(':');
                    isValid = (Int32.TryParse(items[0], out classId) && Int32.TryParse(items[1], out recordId));
                    if (isValid && items.Length > 2)
                        isEntry = Int32.TryParse(items[2], out entryId);
                }
                this.ClassId = (isValid ? classId : 0);
                this.RecordId = (isValid ? recordId : 0);
                this.EntryId = (isEntry ? (int?)entryId : (int?)null);
            }
        }
        /// <summary>
        /// Vrátí HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (!this._HashCode.HasValue)
                this._HashCode = this.ClassId.GetHashCode() ^ this.RecordId.GetHashCode() ^ (this.EntryId.HasValue ? this.EntryId.Value.GetHashCode() : 0);
            return this._HashCode.Value;
        }
        /// <summary>
        /// Lazy initialized HasCode
        /// </summary>
        private int? _HashCode;
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
            return (a.Name == b.Name);
        }
        /// <summary>
        /// Operátor "je rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(GId a, GId b) { return GId._IsEqual(a, b); }
        /// <summary>
        /// Operátor "není rovno"
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(GId a, GId b) { return (!GId._IsEqual(a, b)); }
        #endregion
        #region Implicitní konverze z/na GuiId
        /// <summary>
        /// Implicitní konverze z <see cref="GuiId"/> na <see cref="GId"/>.
        /// Pokud je na vstupu <see cref="GuiId"/> = null, pak na výstupu je <see cref="GId"/> == null.
        /// </summary>
        /// <param name="guiId"></param>
        public static implicit operator GId(GuiId guiId)
        {
            if (guiId == null) return null;
            if (!guiId.EntryId.HasValue) return new GId(guiId.ClassId, guiId.RecordId);
            return new GId(guiId.ClassId, guiId.RecordId, guiId.EntryId.Value);
        }
        /// <summary>
        /// Implicitní konverze z <see cref="GId"/> na <see cref="GuiId"/>.
        /// Pokud je na vstupu <see cref="GId"/> = null, pak na výstupu je <see cref="GuiId"/> == null.
        /// </summary>
        /// <param name="gId"></param>
        public static implicit operator GuiId(GId gId)
        {
            if (gId == null) return null;
            if (!gId.EntryId.HasValue) return new GuiId(gId.ClassId, gId.RecordId);
            return new GuiId(gId.ClassId, gId.RecordId, gId.EntryId.Value);
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
