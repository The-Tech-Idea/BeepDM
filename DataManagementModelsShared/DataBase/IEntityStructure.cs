﻿using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IEntityStructure
    {
        bool Created { get; set; }
        string CustomBuildQuery { get; set; }
        DataSourceType DatabaseType { get; set; }
        string DataSourceID { get; set; }
        bool Drawn { get; set; }
        bool Editable { get; set; }
        string EntityName { get; set; }
        List<EntityField> Fields { get; set; }
        List<ReportFilter> Filters { get; set; }
        int Id { get; set; }
        int ParentId { get; set; }
        List<EntityField> PrimaryKeys { get; set; }
        List<RelationShipKeys> Relations { get; set; }
        List<EntityParameters> Paramenters { get; set; }
        string KeyToken { get; set; }
        string Category { get; set; }
        string SchemaOrOwnerOrDatabase { get; set; }
        bool Show { get; set; }
        string StatusDescription { get; set; }
        int ViewID { get; set; }
        ViewType Viewtype { get; set; }
    }
}