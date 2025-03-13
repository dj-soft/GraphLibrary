using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.App.iCollect.Collect
{
    public class Definition
    {
        public Definition() { }

    }
    public class DefinitionItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public SchemaItemType Type { get; set; }
    }
    public enum SchemaItemType
    {
        /// <summary>
        /// Interní ID, jednoznačné, autoinkrement, nezobrazuje se, poslední hodnota se udržuje v hlavičce a i po smazání záznamu posledně vloženého dostane příští záznam vyšší číslo.
        /// </summary>
        Id,
        /// <summary>
        /// Automatické počitadlo, nový záznam dostane číslo o 1 vyšší než dosud nejvyšší číslo
        /// </summary>
        AutoCounter,
        GroupName,
        SubGroupName,

        Text,
        Int,
        Decimal,
        Date,
        DateTime,

    }
}
