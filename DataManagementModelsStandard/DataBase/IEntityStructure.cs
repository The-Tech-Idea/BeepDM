using System.Collections.Generic;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    public interface IEntityStructure
    {
        
         string GuidID { get; set; }
        bool Created { get; set; }
        string CustomBuildQuery { get; set; }
        DataSourceType DatabaseType { get; set; }
        string DataSourceID { get; set; }
        bool Drawn { get; set; }
        bool Editable { get; set; }
        string EntityName { get; set; }
        string DatasourceEntityName { get; set; }
        string Caption { get; set; }
        List<EntityField> Fields { get; set; }
        List<AppFilter> Filters { get; set; }
        int Id { get; set; }
        string DefaultChartType { get; set; }
        int ParentId { get; set; }
        List<EntityField> PrimaryKeys { get; set; }
        List<RelationShipKeys> Relations { get; set; }
        List<EntityParameters> Parameters { get; set; }
        string KeyToken { get; set; }
        string Category { get; set; }
        string SchemaOrOwnerOrDatabase { get; set; }
        bool Show { get; set; }
        string StatusDescription { get; set; }
        int ViewID { get; set; }
        ViewType Viewtype { get; set; }
        int StartRow { get; set; }
        int EndRow { get; set; }
    }
}