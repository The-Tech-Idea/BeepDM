using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Workflow
{
    public class ObjectTypes
    {
        public ObjectTypes()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }

    }
}
