using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IMapping_rep
    {
        int id { get; set; }
        string MappingName { get; set; }
        string Description { get; set; }
        string EntityName1 { get; set; }
        string EntityName2 { get; set; }
        string Entity1DataSource { get; set; }
        string Entity2DataSource { get; set; }
        List<EntityField> Entity1Fields { get; set; }
        List<EntityField> Entity2Fields { get; set; }
        List<Mapping_rep_fields> FldMapping { get; set; }
    }
}