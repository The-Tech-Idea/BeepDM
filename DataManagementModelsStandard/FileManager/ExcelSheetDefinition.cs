using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    public class ExcelSheetDefinition
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public List<EntityField> Fields { get; set; } = new List<EntityField>();
        public List<ColumnDef> ColumnValuesDef { get; set; } = new List<ColumnDef>();
        public object MyType { get; set; }
        public string SheetName { get; set; }
        public int SheetNo { get; set; }
        public ExcelSheetDefinition()
        {

        }
    }
}
