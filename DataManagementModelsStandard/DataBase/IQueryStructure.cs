using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.DataBase
{
    public interface IQueryStructure
    {
         int ID { get; set; }
         string GuidID { get; set; }
        List<QueryFieldsandValues> FieldsandValues { get; set; }
        List<EntityStructure> FromEntities { get; set; }
        int ParentTableID { get; set; }
        int ParentViewID { get; set; }
        string QueryDescription { get; set; }
        string QueryName { get; set; }
        string Querystring { get; set; }
        List<EntityField> SelectedColumns { get; set; }
    }
}