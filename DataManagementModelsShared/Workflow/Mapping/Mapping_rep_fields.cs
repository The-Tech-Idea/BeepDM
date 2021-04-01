using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class Mapping_rep_fields : IMapping_rep_fields
    {
        public string EntityName1 { get; set; }
        public string FieldName1 { get; set; }
        public string FieldType1 { get; set; }
        public int FieldIndex1 { get; set; }
        public string EntityName2 { get; set; }
        public string FieldName2 { get; set; }
        public string FieldType2 { get; set; }
        public int FieldIndex2 { get; set; }
        public string Rules { get; set; }
        public Mapping_rep_fields()
        {


        }
    }
}
