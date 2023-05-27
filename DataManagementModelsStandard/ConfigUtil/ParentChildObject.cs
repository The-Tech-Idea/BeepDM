using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Util
{
    [Serializable]
    public class ParentChildObject

    {
        public string id { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ParentID { get; set; }
        public string ObjType { get; set; }
        public string AddinName { get; set; }
       
        public string Description { get; set; }
        public bool Mapped { get; set; } = false;
        public bool Show { get; set; } = true;
        public string ObjectName { get; set; }

        public ParentChildObject()
        {

        }
    }
}
