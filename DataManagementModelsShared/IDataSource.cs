using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea
{
    public interface IDataSource
    {
        event EventHandler<PassedArgs> PassEvent;
        DataSourceType DatasourceType { get; set; }
        DatasourceCategory Category { get; set; }
        IDataConnection Dataconnection { get; set; }
        string DatasourceName { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        string Id { get; set; }
        IDMLogger Logger { get; set; }
        List<string> EntitiesNames { get; set; }
        List<EntityStructure> Entities { get; set; }
        IDMEEditor DMEEditor { get; set; }
        ConnectionState ConnectionStatus { get; set; }
        List<string> GetEntitesList();
        IErrorsInfo ExecuteSql(string sql);
        bool CreateEntityAs(EntityStructure entity);
        Type GetEntityType(string EntityName);
        bool CheckEntityExist(string EntityName);
        List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);
        List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);
        EntityStructure GetEntityStructure(string EntityName, bool refresh );
        EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
        Task<object> GetEntityDataAsync(string EntityName, string filterstr);
        DataTable GetEntity(string EntityName, string filterstr);
        DataTable RunQuery(string qrystr);
        LScript RunScript( LScript dDLScripts);
        List<LScript> GetCreateEntityScript(List<EntityStructure> entities=null);
        IErrorsInfo CreateEntities(List<EntityStructure> entities);
        IErrorsInfo UpdateEntities(string EntityName,object UploadData, IMapping_rep Mapping=null);
        IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping=null);
        IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null);
       
       

    }
}
