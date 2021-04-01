using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class ExcelSheetDefinition
    {
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
