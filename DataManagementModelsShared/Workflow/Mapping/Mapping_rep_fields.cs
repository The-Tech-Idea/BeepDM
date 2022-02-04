using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public class Mapping_rep_fields : IMapping_rep_fields
    {
        public string ToEntityName { get; set; }
        public string ToFieldName { get; set; }
        public string ToFieldType { get; set; }
        public int ToFieldIndex { get; set; }
        public string FromEntityName { get; set; }
        public string FromFieldName { get; set; }
        public string FromFieldType { get; set; }
        public int FromFieldIndex { get; set; }
        public string Rules { get; set; }
        public Mapping_rep_fields()
        {


        }
    }
}
