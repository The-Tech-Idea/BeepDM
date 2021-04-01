using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TheTechIdea.Tools
{
    public class AssemblyItem
    {
        public string Assemblyname { get; set; }
        public string Typename { get; set; }
        public List<AssemblyItemFieldDataTypes> MyFields { get; set; } = new List<AssemblyItemFieldDataTypes>();
        public AssemblyItem()
        {

        }
    }
    public class AssemblyItemFieldDataTypes
    {

        public string fieldName { get; set; }
        public string fieldType { get; set; }
        public AssemblyItemFieldDataTypes()
        {
        }


    }

}
