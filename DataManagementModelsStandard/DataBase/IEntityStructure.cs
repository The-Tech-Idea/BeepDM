using System.Collections.Generic;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public interface IEntityStructure
    {
        string EntityPath { get;set; }
        string GuidID { get; set; }
        EntityType EntityType { get; set; }
        string SourceDataSourceID { get; set; }
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
        string PrimaryKeyString { get; set; }
        List<EntityField> PrimaryKeys { get; set; }
        List<RelationShipKeys> Relations { get; set; }
        List<EntityParameters> Parameters { get; set; }
        string KeyToken { get; set; }
        string Category { get; set; }
        string SchemaOrOwnerOrDatabase { get; set; }
        bool Show { get; set; }
        string StatusDescription { get; set; }
        string Description { get; set; }
        int ViewID { get; set; }
        ViewType Viewtype { get; set; }
        int StartRow { get; set; }
        int EndRow { get; set; }
        bool IsLoaded { get; set; }
        bool IsSaved { get; set; }
        bool IsSynced { get; set; }
        bool IsCreated { get; set; }
        bool IsIdentity { get; set; }
    }
}