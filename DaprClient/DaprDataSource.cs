using Dapr.Client;
using DataManagmentEngineShared.WebAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using YamlDotNet.Core.Tokens;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;

namespace DaprClient
{
    public class DaprDataSource : IDataSource
    {
        public Dapr.Client.DaprClient Client { get; set; }
        public DaprDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };

            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();

             Client = new DaprClientBuilder().Build();
            //var counter = await DaprClient.GetStateAsync<int>(StoreName, Key);
            Dataconnection.ConnectionStatus = ConnectionStatus;
            GetEntitesList();


        }

        public DataSourceType DatasourceType { get; set; } = DataSourceType.WebService;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public event EventHandler<PassedArgs> PassEvent;

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
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

        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
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

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
    }
}
