using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataView
{
    public interface IDataViewDataSource
    {
        
        string GuidID { get; set; }
        DatasourceCategory Category { get; set; }
        string ColumnDelimiter { get; set; }
        string CompositeLayerDataSourceID { get; set; }
        ConnectionState ConnectionStatus { get; set; }
        IDataConnection Dataconnection { get; set; }
        string DatasourceName { get; set; }
        DataSourceType DatasourceType { get; set; }
        IDMDataView DataView { get; set; }
        string DataViewDataSourceID { get; set; }
        IDMEEditor DMEEditor { get; set; }
        bool Editable { get; set; }
        List<EntityStructure> Entities { get; set; }
        List<string> EntitiesNames { get; set; }
        string EntityDataSourceID { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        bool FileLoaded { get; set; }
       
        IDMLogger Logger { get; set; }
        string ParameterDelimiter { get; set; }
        DataTable SourceEntityData { get; set; }
        string VID { get; set; }
        int ViewID { get; set; }
        string ViewName { get; set; }
        ViewType Viewtype { get; set; }

        event EventHandler<PassedArgs> PassEvent;

        int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex);
        int AddEntitytoDataView(EntityStructure maintab);
        int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        bool CheckEntityExist(string entityname);
        ConnectionState Closeconnection();
        IErrorsInfo CreateEntities(List<EntityStructure> entities);
        bool CreateEntityAs(EntityStructure entity);
        IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow);
        void Dispose();
        int EntityListIndex(int entityid);
        int EntityListIndex(string entityname);
        IErrorsInfo ExecuteSql(string sql);
        int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters);
        IDMDataView GenerateView(string ViewName, string ConnectionName);
        int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters);
        List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);
        List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null);
        List<DataSet> GetDataSetForView(string viewname);
        List<string> GetEntitesList();
        EntityStructure GetEntity(string entityname);
        object GetEntity(string EntityName, List<AppFilter> filter);
        Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter);
        List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);
        int GetEntityIdx(string entityName);
        EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
        EntityStructure GetEntityStructure(string EntityName, bool refresh = false);
        Type GetEntityType(string entityname);
        string GeticonForViewType(ViewType v);
        IErrorsInfo InsertEntity(string EntityName, object InsertedData);
        IErrorsInfo LoadView();
        int NextHearId();
        ConnectionState Openconnection();
        IDMDataView ReadDataViewFile(string pathandfilename);
        IErrorsInfo RemoveChildEntities(int EntityID);
        IErrorsInfo RemoveEntity(int EntityID);
        object RunQuery(string qrystr);
        IErrorsInfo RunScript(ETLScriptDet dDLScripts);
        IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress);
        IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow);
        void WriteDataViewFile(string filename);
        void WriteDataViewFile(string path, string filename);
    }
}