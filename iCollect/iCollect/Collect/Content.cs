using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmlSerial = DjSoft.App.iCollect.Data.XmlSerializer;

namespace DjSoft.App.iCollect.Collect
{
    public class Content
    {
        [XmlSerial.PersistingEnabled(false)]
        public Collection Owner { get; set; }
    }
}
