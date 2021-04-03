using DataManagmentEngineShared.WebAPI;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Sparrow.Collections;
using Sparrow.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.NOSQL;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;
using TheTechIdea.Tools;

namespace DataManagmentEngineShared.NOSQL
{
    
    public class RavenDBReader : WebAPIReader
    {
        public IDocumentSession Session { get; set; }
        public IDocumentStore Store { get; set; }
        public List<DatabaseCollection> Databases { get; set; }
        public List<string> Collections { get; set; }
        public string CurrentDatabase { get; set; }
        public RavenDBReader( string datasourcename,string databasename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields = null) :base( datasourcename,  pDMEEditor,  pConn, pfields )
        {
            CurrentDatabase = databasename;
            if (string.IsNullOrWhiteSpace(CurrentDatabase) == false)
            {
                Store = OpenStore(ConnProp.Url, 10, true);
                
            }
          

            //if (Store != null)
            //{
              
            //    Databases = GetDatabaseNames();
            //}
        }
        public override IEnumerable<string> GetEntities()
        {

            try
            {
                return GetCollection();
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not retrieve entities in RavenDB " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        public EntityStructure GetEntityStructure(string DocName)
        {
            EntityStructure retval = new EntityStructure();
            try
            {

                Session=GetSession(CurrentDatabase);
                var command = new GetDocumentsCommand(DocName,  null, metadataOnly: true);
                Session.Advanced.RequestExecutor.Execute(command, Session.Advanced.Context);
                var result = (BlittableJsonReaderObject)command.Result.Results[0];
                var documentMetadata = (BlittableJsonReaderObject)result["@metadata"];

                // Print out all the metadata properties.
                EntityStructure entityData = new EntityStructure();

                string sheetname;
                sheetname = DocName;
                entityData.EntityName = DocName;
                entityData.DataSourceID = ConnProp.ConnectionName;
                entityData.SchemaOrOwnerOrDatabase = CurrentDatabase;
                List<EntityField> Fields = new List<EntityField>();
                int y = 0;
                foreach (var propertyName in documentMetadata.GetPropertyNames())
                {
                 documentMetadata.TryGet<object>(propertyName, out var metaPropValue);
                    
                 EntityField f = new EntityField();
                 f.fieldname = propertyName;
                 f.fieldtype = "System.String";
                 f.ValueRetrievedFromParent = false;
                 f.EntityName = sheetname;
                 f.FieldIndex = y;
                 Fields.Add(f);
                 y += 1;

                   

                   
                   
                }
                return retval;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not Create Entity structure for RavenDB Entity " + DocName, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        public List<string> GetCollection()
        {

            try
            {
                var op = new GetCollectionStatisticsOperation();
                CollectionStatistics collectionStats = Store.Maintenance.Send(op);
             Collections= collectionStats.Collections.Keys.ToList();
                return Collections;

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not get Collection from Database in RavenDB " + CurrentDatabase , DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        public List<DatabaseCollection> GetDatabaseNames()
        {

            try
            {
                var operation = new GetDatabaseNamesOperation(0, 25);
                string[] databaseNames = Store.Maintenance.Server.Send(operation);
                Databases = new List<DatabaseCollection>();
                foreach (string item in databaseNames)
                {
                    GetSession(item);
                    DatabaseCollection t = new DatabaseCollection();
                    t.DatabasName = item;
                    t.Collections=GetCollection();
                   
                        foreach (string col in t.Collections)
                        {

                        Entities.Add(GetEntityStructure(col.Remove(col.Length,1)));

                        }
                   
                }
              
                return Databases;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Could not Get Databases From RavenDB  {ex.Message}" + ConnProp.ConnectionName, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        private DataSet CreateDataset(string pDatabase)
        {
            ds = new DataSet();
            try
            {

                return ds;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not create dataset RavenDB " + mes, DateTime.Now, -1, mes, Errors.Failed);

                return null;
            };
        }
        public IDocumentSession GetSession(string Database)
        {
            try
            {
                if (State == ConnectionState.Open)
                {
                    if (string.IsNullOrWhiteSpace(Database)==false)
                    {
                      
                            Session = Store.OpenSession( Database);
                          
                      
                    }
                    else
                        Session = Store.OpenSession();
                    return Session;


                };
                return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Open Store Session in RavenDB " + ex.Message, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        public IDocumentSession CloseSession(string Database)
        {
            try
            {
                Session.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Close Store Session in RavenDB " + ex.Message, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        public IDocumentStore OpenStore(string pUrl,int pMaxNumberOfRequestsPerSession=10,bool pUseOptimisticConcurrency=true)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(ConnProp.CertificatePath)==false)
                {
                  
                        Store = new DocumentStore()
                        {

                            Urls = new[] { pUrl, /*some additional nodes of this cluster*/ },
                            // Set conventions as necessary (optional)
                            Conventions =
                            {
                                MaxNumberOfRequestsPerSession = pMaxNumberOfRequestsPerSession,
                                UseOptimisticConcurrency = pUseOptimisticConcurrency
                            },
                            Certificate = new X509Certificate2(ConnProp.CertificatePath),
                            Database = CurrentDatabase
                        }.Initialize();
                   
                }else
                {
                    Store = new DocumentStore()
                    {

                        Urls = new[] { pUrl, /*some additional nodes of this cluster*/ },
                        // Set conventions as necessary (optional)
                        Conventions =
                    {
                         MaxNumberOfRequestsPerSession = pMaxNumberOfRequestsPerSession,
                         UseOptimisticConcurrency = pUseOptimisticConcurrency
                    },
                         Database=CurrentDatabase

                    }.Initialize();
                }
                State = ConnectionState.Open;
                return Store;
            }
            catch (Exception ex)
            {
                State = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Error", "Could not Open Store in RavenDB " + ex.Message, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }

           
           
        }
        public async Task EnsureDatabaseExistsAsync(IDocumentStore store, string database = null, bool createDatabaseIfNotExists = true)
        {
            database = database ?? store.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

            try
            {
                await store.Maintenance.ForDatabase(database).SendAsync(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (createDatabaseIfNotExists == false)
                    throw;

                try
                {
                    await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(database)));
                }
                catch (ConcurrencyException)
                {
                }
            }
        }
        public bool WriteDocumentToStore<T>(string pDatabase,T MyObject)
        {
            try
            {
                   using (var session = Store.OpenAsyncSession(new SessionOptions
                    {
                        //default is:     TransactionMode.SingleNode
                        TransactionMode = TransactionMode.ClusterWide,
                         Database=pDatabase
 
                    }))
                    {
                        //var user = new Employee
                        //{
                        //    FirstName = "John",
                        //    LastName = "Doe"
                        //};
                        session.StoreAsync(MyObject);

                        // this transaction is now conditional on this being 
                        // successfully created (so, no other users with this name)
                        // it also creates an association to the new user's id
                        //session.Advanced.ClusterTransaction
                        //    .CreateCompareExchangeValue("usernames/John", user.Id);

                        session.SaveChangesAsync();
                    }
               
                return true;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Store Document Store in RavenDB " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
          }
    }
   

    
}
