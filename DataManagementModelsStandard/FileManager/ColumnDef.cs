using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager
{
    public class ColumnDef
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public bool FoundValue    {get;set;}
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        public ColumnDef()
        {

        }
    }
}
