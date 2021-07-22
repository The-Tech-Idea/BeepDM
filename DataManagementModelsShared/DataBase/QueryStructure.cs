using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public class QueryStructure : IQueryStructure
    {
        public QueryStructure()
        {

        }
        public string QueryName { get; set; }
        public string QueryDescription { get; set; }
        public string Querystring { get; set; }
        public List<EntityField> SelectedColumns { get; set; }
        public List<EntityStructure> FromEntities { get; set; }
        public List<QueryFieldsandValues> FieldsandValues { get; set; }
        public int ParentViewID { get; set; }
        public int ParentTableID { get; set; }



    }

    public class QueryFieldsandValues
    {
        public QueryFieldsandValues()
        {

        }
        public EntityField CompareField1 { get; set; }
        public string Fieldvalue { get; set; }
        public string Comparison { get; set; }

    }

    public class FkListforSQLlite
    {
        public FkListforSQLlite()
        {

        }
        public int id { get; set; } // Integer Foreign key ID number
        public int seq { get; set; }// Integer Column sequence number for this key
        public string table { get; set; }//   Text Name of foreign table
        public string from { get; set; } //  Text Local column name
        public string to { get; set; }// Text    Foreign column name
        public string on_update { get; set; } // Text ON UPDATE action
        public string on_delete { get; set; } // Text    ON DELETE action
        public string match { get; set; } // Text Always NONE
    }
}
