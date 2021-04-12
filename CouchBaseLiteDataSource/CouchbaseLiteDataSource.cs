using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.NOSQL
{
    public class CouchbaseLiteDataSource : IDataSource,ILocalDB

    {
        public event EventHandler<PassedArgs> PassEvent;

        public CouchbaseLiteDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) 
        {
            if (Dataconnection == null)
            {
                RDBDataConnection cn = new RDBDataConnection();
                Dataconnection = cn;
            }
            if (Dataconnection.ConnectionProp.ConnectionString != null)
            {
                ConnectionDriversConfig package = DMEEditor.ConfigEditor.DataDrivers.Where(x => x.classHandler == this.ToString()).FirstOrDefault();
                ConnectionProperties dataConnection = new ConnectionProperties();
                dataConnection.Category = DatasourceCategory.NOSQL;
                dataConnection.DatabaseType = DataSourceType.Couchbase;
                dataConnection.ConnectionName = datasourcename;
                dataConnection.DriverName = package.PackageName;
                dataConnection.DriverVersion = package.version;
                dataConnection.ID = DMEEditor.ConfigEditor.DataConnections.Max(y => y.ID) + 1;
                dataConnection.ConnectionString = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath, datasourcename);
                DMEEditor.ConfigEditor.DataConnections.Add(dataConnection);
                Dataconnection.ConnectionProp = dataConnection;
            }
            CreateDB();
        }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public List<object> Records { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public DataTable SourceEntityData { get; set; }
        public bool CanCreateLocal { get ; set ; }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            throw new NotImplementedException();
        }

        public bool CreateDB()
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public bool DeleteDB()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }


        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }

        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CloseConnection()
        {
            try

            {

                Dataconnection.DbConn.Close();


                DMEEditor.AddLogMessage("Success", $"Closing connection to SQL Compact Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to SQL Compact Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo DropEntity(string EntityName)
        {
            try

            {



                DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = $"Error Droping Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            throw new NotImplementedException();
        }
    }
}
