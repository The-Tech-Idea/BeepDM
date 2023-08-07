using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea
{
    public interface IDataSource:IDisposable
    {
         string GuidID { get; set; }
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
        object RunQuery(string qrystr);
        IErrorsInfo ExecuteSql(string sql);
        bool CreateEntityAs(EntityStructure entity);
        Type GetEntityType(string EntityName);
        bool CheckEntityExist(string EntityName);
        int GetEntityIdx(string entityName);
        List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);
        List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);
        EntityStructure GetEntityStructure(string EntityName, bool refresh );
        EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
        IErrorsInfo RunScript(ETLScriptDet dDLScripts);
        List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities=null);
        IErrorsInfo CreateEntities(List<EntityStructure> entities);
        IErrorsInfo UpdateEntities(string EntityName,object UploadData, IProgress<PassedArgs> progress);
        IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow);
        IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow);
        IErrorsInfo InsertEntity(string EntityName, object InsertedData);
        IErrorsInfo BeginTransaction(PassedArgs args);
        IErrorsInfo EndTransaction(PassedArgs args);
        IErrorsInfo Commit(PassedArgs args);
        object GetEntity(string EntityName, List<AppFilter> filter);
        Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter);
        string ColumnDelimiter { get; set; }
        string ParameterDelimiter { get; set; }
        ConnectionState Openconnection();
        ConnectionState Closeconnection();

    }
}
