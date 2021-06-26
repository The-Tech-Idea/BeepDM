using DataManagmentEngineShared.WebAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.WebAPI.EIAWebApi
{
    [ClassProperties(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.WebService)]
    public class EIAWebApiDatasource :IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public HttpClient client { get; set; } = new HttpClient();

        WebAPIDataConnection cn;
        public EIAWebApiDatasource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) 
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
            cn = (WebAPIDataConnection)Dataconnection;
            cn.DMEEditor = DMEEditor;
         

        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public ConnectionState Openconnection()
        {
             return   ConnectionStatus= cn.OpenConnection();
         
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }

        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            EntityStructure ent=Entities.Where(p=>p.EntityName.Equals(tablename,StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            List<EntityStructure> ents=  GetSeriesList(ent);
            Entities.AddRange(ents);
            return null;
        }

        public List<string> GetEntitesList()
        {
            try
            {
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
                {
                    if (Entities.Count == 0)

                    {
                        DatasourceEntities f = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName);
                        if (f != null)
                        {
                            Entities = f.Entities;
                        }


                    }
                    DMEEditor.AddLogMessage("Log Time", $"started International Energy Data {DateTime.Now}", DateTime.Now, 2134384, "International Energy Data", Errors.Ok);

                    if (!Entities.Where(o => o.EntityName.Equals("International Energy Data", StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        CreateCategories(2134384, "International Energy Data", 0);
                    }
                    DMEEditor.AddLogMessage("Log Time", $"started Petroleum {DateTime.Now}", DateTime.Now, 2134384, "Petroleum", Errors.Ok);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    if (!Entities.Where(o => o.EntityName.Equals("Petroleum", StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        CreateCategories(714755, "Petroleum", 0);
                    }
                    DMEEditor.AddLogMessage("Log Time", $"started  Crude Oil Imports {DateTime.Now}", DateTime.Now, 2134384, "Crude Oil Imports", Errors.Ok);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    if (!Entities.Where(o => o.EntityName.Equals("Crude Oil Imports", StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        CreateCategories(1292190, "Crude Oil Imports", 0);
                    }
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });





                    //string retval = GetCategoriesDataAsync("GetCategories", $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id=371").Result;
                    //    var data = JObject.Parse(retval);
                    //    var categorytk = data.SelectToken("category");


                    //    Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

                    //    int i = 1;
                    //    foreach (Childcategory item in categorys.childcategories)
                    //    {
                    //        if (!Entities.Where(o => o.EntityName.Equals(item.name, StringComparison.OrdinalIgnoreCase)).Any())
                    //        {
                    //            EntityStructure x = new EntityStructure();
                    //            x.ParentId = 0;
                    //            x.Id = i;
                    //            i += 1;
                    //            x.EntityName = item.name;
                    //            x.Viewtype = ViewType.Url;

                    //            x.DataSourceID = DatasourceName;
                    //            x.Category = "Category";
                    //            x.CustomBuildQuery = $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id={item.category_id}";
                    //            Entities.Add(x);
                    //            var ou = GetCategoriesDataAsync("GetCategories", x.CustomBuildQuery);
                    //            ou.Wait();
                    //            data = JObject.Parse(ou.Result);
                    //            categorytk = data.SelectToken("category");
                    //            Category ct = JsonConvert.DeserializeObject<Category>(categorytk.ToString());
                    //            foreach (Childcategory cc in ct.childcategories)
                    //            {
                    //                CreateChildCategories(cc, x);
                    //            }

                    //            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    //        }

                    //    }

                    //}
                    EntitiesNames.Clear();
                    EntitiesNames = Entities.Select(p => p.EntityName).ToList();

                    
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });

                }
                //---------------Get Categories---------------- -
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
                {

                }
                 return EntitiesNames;
               
            }
            catch (Exception )
            {
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                return null;
            }
        }
        #region "EIA Methods"
        private void CreateCategories(int categoryid,string categoryname, int entityid)
        {
            EntityStructure catent = new EntityStructure();
            if (Entities.Count == 0)
            {
                catent.Id =  1;
            }
            else
            {
                catent.Id = Entities.Max(u => u.Id) + 1;
            }
           
            catent.ParentId = entityid;
            catent.EntityName = categoryname;
            catent.Viewtype = ViewType.Url;
            catent.Category = "Category";
            catent.DataSourceID = DatasourceName;
            catent.CustomBuildQuery = $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id={categoryid}";
            Entities.Add(catent);
            var retval = GetCategoriesDataAsync("GetCategories", catent.CustomBuildQuery);
            retval.Wait();

            var data = JObject.Parse(retval.Result);
            var categorytk = data.SelectToken("category");
            Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

            //if (categorys.childseries.Count() > 0)
            //{
            //    foreach (Childsery childs in categorys.childseries)
            //    {
            //        EntityStructure serent = new EntityStructure();
            //        serent = new EntityStructure();
            //        serent.Id = Entities.Max(u => u.Id) + 1;
            //        serent.ParentId = catent.Id;
            //        serent.EntityName = childs.name;
            //        serent.Viewtype = ViewType.Url;
            //        serent.Category = "Series";
            //        serent.CustomBuildQuery = $"/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={childs.series_id}";
            //        Entities.Add(serent);
            //    }
            //  ;
            //}
            if (categorys.childcategories.Count() > 0)
            {
                foreach (Childcategory ct in categorys.childcategories)
                {
                    CreateChildCategories(ct, catent.Id);
                }


            }
        }
        private void CreateChildCategories(Childcategory item, int entityid)
        {
            EntityStructure catent = new EntityStructure();
            if (Entities.Count == 0)
            {
                catent.Id = 1;
            }
            else
            {
                catent.Id = Entities.Max(u => u.Id) + 1;
            }
            catent.ParentId = entityid;
            catent.EntityName = item.name;
            catent.Viewtype = ViewType.Url;
            catent.Category = "Category";
            catent.DataSourceID = DatasourceName;
            catent.CustomBuildQuery = $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id={item.category_id}";
            Entities.Add(catent);
            var retval = GetCategoriesDataAsync("GetCategories", catent.CustomBuildQuery);
            retval.Wait();

            var data = JObject.Parse(retval.Result);
            var categorytk = data.SelectToken("category");
            Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

            //if (categorys.childseries.Count() > 0)
            //{
            //    foreach (Childsery childs in categorys.childseries)
            //    {
            //        EntityStructure serent = new EntityStructure();
            //        serent = new EntityStructure();
            //        serent.Id = Entities.Max(u => u.Id) + 1;
            //        serent.ParentId = catent.Id;
            //        serent.EntityName = childs.name;
            //        serent.Viewtype = ViewType.Url;
            //        serent.Category = "Series";
            //        serent.CustomBuildQuery = $"/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={childs.series_id}";
            //        Entities.Add(serent);
            //    }
            //  ;
            //}
            if (categorys.childcategories.Count() > 0)
            {
                foreach (Childcategory ct in categorys.childcategories)
                {
                    CreateChildCategories(ct, catent.Id);
                }


            }
        }
        public List<EntityStructure> GetSeriesList(string CategoryID,int entityid)
        {

            List<EntityStructure> entities = new List<EntityStructure>();
            try
            {
              
                var retval = GetCategoriesDataAsync("GetCategories", $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id={CategoryID}");
                retval.Wait();

                var data = JObject.Parse(retval.Result);
                var categorytk = data.SelectToken("category");
                Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

                if (categorys.childseries.Count() > 0)
                {
                    foreach (Childsery childs in categorys.childseries)
                    {
                        EntityStructure serent = new EntityStructure();
                        serent = new EntityStructure();
                        if (Entities.Count == 0)
                        {
                            serent.Id = 1;
                        }
                        else
                        {
                            serent.Id = Entities.Max(u => u.Id) + 1;
                        }
                        serent.ParentId = entityid;
                        serent.EntityName = childs.name;
                        serent.Viewtype = ViewType.Url;
                        serent.Category = "Series";
                        serent.CustomBuildQuery = $"/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={childs.series_id}";
                        Entities.Add(serent);
                        entities.Add(serent);
                    }
                  ;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return entities;
        }
        public List<EntityStructure> GetSeriesList(EntityStructure ent)
        {
            List<EntityStructure> entities = new List<EntityStructure>();
            try
            {

                var retval = GetCategoriesDataAsync("GetCategories", ent.CustomBuildQuery);
                retval.Wait();

                var data = JObject.Parse(retval.Result);
                var categorytk = data.SelectToken("category");
                Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

                if (categorys.childseries.Count() > 0)
                {
                    foreach (Childsery childs in categorys.childseries)
                    {
                        EntityStructure serent = new EntityStructure();
                        serent = new EntityStructure();
                        if (Entities.Count == 0)
                        {
                            serent.Id = 1;
                        }
                        else
                        {
                            serent.Id = Entities.Max(u => u.Id) + 1;
                        }
                        serent.ParentId = ent.Id;
                        serent.EntityName = childs.name;
                        serent.Viewtype = ViewType.Url;
                        serent.Category = "Series";
                        serent.CustomBuildQuery = $"/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={childs.series_id}";
                        Entities.Add(serent);
                        entities.Add(serent);
                    }
                  ;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return entities;
        }
        public List<EntityStructure> GetSeriesList(string CategoryID)
        {
            List<EntityStructure> entities = new List<EntityStructure>();
            try
            {

                var retval = GetCategoriesDataAsync("GetCategories", $"/category/?api_key={Dataconnection.ConnectionProp.ApiKey}&category_id={CategoryID}");
                retval.Wait();

                var data = JObject.Parse(retval.Result);
                var categorytk = data.SelectToken("category");
                Category categorys = JsonConvert.DeserializeObject<Category>(categorytk.ToString());

                if (categorys.childseries.Count() > 0)
                {
                    foreach (Childsery childs in categorys.childseries)
                    {
                        EntityStructure serent = new EntityStructure();
                        serent = new EntityStructure();
                        if (Entities.Count == 0)
                        {
                            serent.Id = 1;
                        }
                        else
                        {
                            serent.Id = Entities.Max(u => u.Id) + 1;
                        }
                       // serent.ParentId = entityid;
                        serent.EntityName = childs.name;
                        serent.Viewtype = ViewType.Url;
                        serent.Category = "Series";
                        serent.CustomBuildQuery = $"/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={childs.series_id}";
                        Entities.Add(serent);
                        entities.Add(serent);
                    }
                  ;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return entities;
        }
        public  async Task<string> GetSeries(string seriesId)
        {
           
            using (var httpClient = new HttpClient())
            {

                var response = await httpClient.GetAsync($"http://api.eia.gov/series/?api_key={Dataconnection.ConnectionProp.ApiKey}&series_id={seriesId}");

                return await response.Content.ReadAsStringAsync();
            }
        }
        private async Task<string> GetCategoriesDataAsync(string EntityName, string filterstr)
        {
            var request = new HttpRequestMessage();
          

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
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }

            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string retval = body;
                //if (!string.IsNullOrEmpty(ent.KeyToken) || !string.IsNullOrWhiteSpace(ent.KeyToken))
                //{
                //    var data = JObject.Parse(body);
                //    var location = data.SelectToken("category");
                //    var categories = location.SelectToken(ent.KeyToken);
                //    retval = categories.ToString();
                //}

                return retval;
            }
        }

        #endregion

        public async Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {

            EntityStructure ent = Entities.Where(o => o.EntityName == EntityName).FirstOrDefault();
            string filterstr = ent.CustomBuildQuery;
            foreach (EntityParameters item in ent.Paramenters)
            {
                filterstr = filterstr.Replace("{" + item.parameterIndex + "}", ent.Filters.Where(u => u.FieldName == item.parameterName).Select(p => p.FilterValue).FirstOrDefault());
            }
            var request = new HttpRequestMessage();
            client = new HttpClient();
            client.BaseAddress = new Uri(Dataconnection.ConnectionProp.Url);
          
           
            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.ApiKey))
            {
                Dataconnection.ConnectionProp.Url = Dataconnection.ConnectionProp.Url.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            if (!string.IsNullOrEmpty(filterstr))
            {
                filterstr = filterstr.Replace("@apikey", Dataconnection.ConnectionProp.ApiKey);
            }
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + filterstr);
            foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
            {
                request.Headers.Add(item.headername, item.headervalue);
            }
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string retval = body;
                if (ent.Category=="Series")
                {
                    var data = JObject.Parse(body);
                    var location = data.SelectToken("series");
                    //    var categories = location.SelectToken(ent.KeyToken);
                    if (location != null)
                    {
                        retval = location.ToString();
                    }
             
                }
                var y = DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<List<Series>>(retval);
                return y;
            }
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (Entities.Count > 0)
            {
                return Entities[Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase))];
            }
            else
                return null;

        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (Entities.Count > 0)
            {
                return Entities[Entities.FindIndex(p => p.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))];
            }
            else
                return null;
        }
        public Type GetEntityType(string EntityName)
        {
            return typeof(Series);
        }
        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IMapping_rep Mapping = null)
        {
            throw new NotImplementedException();
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow, IMapping_rep Mapping = null)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo RunScript(SyncEntity dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<SyncEntity> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
          return  GetEntityAsync(EntityName, filter);
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
