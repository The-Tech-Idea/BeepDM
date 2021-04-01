using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class ColumnDef
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public bool FoundValue    {get;set;}


        public ColumnDef()
        {

        }
    }
}
