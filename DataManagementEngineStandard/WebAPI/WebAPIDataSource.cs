
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using System.Data;
using TheTechIdea.Beep.DataBase;
using System.Net.Http;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.WebAPI
{
    public class WebAPIDataSource :IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
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
        // public DataTable SourceEntityData { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        // IHttpClientFactory ClientFactory;
        public HttpClient client { get; set; } = new HttpClient();
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        WebAPIDataConnection cn;
      
        public WebAPIDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            client = new HttpClient();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            cn =(WebAPIDataConnection) Dataconnection;
          //  Reader = new WebAPIReader(Dataconnection.ConnectionProp.FileName, DMEEditor, cn, null);
            cn.OpenConnection();
            cn.ConnectionStatus = ConnectionStatus;
           // GetEntitesList();
        }
        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public bool CheckEntityExist(string EntityName)
        {
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase)).Any();
        }

        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public virtual DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public virtual  IDataReader GetDataReader(string querystring)
        {
            throw new NotImplementedException();
        }

        public virtual List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try

            {

                return Dataconnection.ConnectionProp.Entities.Select(x => x.EntityName).ToList();

            //    Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Unsuccessfully Retrieve Entites list for {DatasourceName} ({ex.Message})", DateTime.Now, 0,DatasourceName, Errors.Ok);


            }

            return EntitiesNames;
        }
       
        
       

        public virtual object GetEntity(string EntityName, List<AppFilter> filter)
        {
            EntityStructure ent = GetEntityStructure(EntityName,false);
            string str = ent.CustomBuildQuery;
            if (ent.Filters == null)
            {
                ent.Filters = new List<AppFilter>();
            }
            foreach (AppFilter item in ent.Filters)
            {
                if (string.IsNullOrEmpty(item.FilterValue) || string.IsNullOrWhiteSpace(item.FilterValue))
                {
                    
                }
            }
            foreach (EntityParameters item in ent.Parameters)
            {
                str = str.Replace("{" + item.parameterIndex + "}", ent.Filters.Where(u => u.FieldName == item.parameterName).Select(p => p.FilterValue).FirstOrDefault());
            }
            throw new NotImplementedException();
        }

      
        public virtual  object RunQuery( string qrystr)
        {

            throw new NotImplementedException();

        }

        public virtual List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName, false);
            DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, x.Fields);
            return DMTypeBuilder.myType;
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
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

        
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            var request = new HttpRequestMessage();
            client = new HttpClient();
            string filterstr="";
            client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);

            EntityStructure ent = Dataconnection.ConnectionProp.Entities.Where(o => o.EntityName == EntityName).FirstOrDefault();
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
            {
                Dataconnection.ConnectionProp.Url = Dataconnection.ConnectionProp.Url.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            if (!string.IsNullOrEmpty(filterstr))
            {
                filterstr = filterstr.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
           
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + "/" + filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }

            //string retval = SendAsync(request).Result;

            using (var response = await client.SendAsync(request))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                dynamic x = DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<dynamic>(body);
                return x;
            }


        }
        #region "dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}