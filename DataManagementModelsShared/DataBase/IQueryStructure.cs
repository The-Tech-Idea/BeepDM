using System.Collections.Generic;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IQueryStructure
    {
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