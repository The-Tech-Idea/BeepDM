using DataManagmentEngineShared.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.WebAPI;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Data;

using TheTechIdea.Util;


namespace TheTechIdea.DataManagment_Engine.NOSQL.MongoDB
{
    public class MongoDBReader : WebAPIReader
    {

        //mongodb://[username:password@]host1[:port1][,...hostN[:portN]][/[defaultauthdb][?options]]

  // {
  //"mapreduce" : "collectionName",
  //"map" : function()
  //      {
  //  for (var key in this) { emit(key, null); }
  //      },
  //"reduce" : function(key, stuff) { return null; },
  //"out": "collectionName" + "_keys"}

    public MongoClient Client { get; set; }

        public IMongoDatabase Database { get; set; }
        public List<DatabaseCollection> Databases { get; set; }
        public List<string> Collections { get; set; }
        public string CurrentDatabase { get; set; }
      

        public MongoDBReader(string datasourcename, string databasename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields = null) : base(datasourcename, pDMEEditor, pConn, pfields)
        {
            CurrentDatabase = databasename;
            if (string.IsNullOrWhiteSpace(CurrentDatabase) == false)
            {
                OpenStore(ConnProp.Url );

            }

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

              
             
                
                // Print out all the metadata properties.
                EntityStructure entityData = new EntityStructure();

                string sheetname;
                sheetname = DocName;
                entityData.EntityName = DocName;
                entityData.DataSourceID = ConnProp.ConnectionName;
                entityData.SchemaOrOwnerOrDatabase = CurrentDatabase;
                var col = Database.GetCollection<BsonDocument>(DocName);
                var filter = new BsonDocument();
                var myDocument = col.Find<BsonDocument>(filter).FirstOrDefault() ;

                var mydocjson = myDocument.ToJson();
                 ds = JsonConvert.DeserializeObject<DataSet>(mydocjson);
                DataTable tb = ds.Tables[0];
                List<EntityField> Fields = new List<EntityField>();
                int y = 0;
                foreach (DataColumn propertyName in tb.Columns)
                {
                   EntityField f = new EntityField();
                    f.fieldname = propertyName.ColumnName;
                    f.fieldtype = propertyName.DataType.ToString();
                    f.FoundValue = false;
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
                return Collections;

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage(ex.Message, "Could not get Collection from Database in RavenDB " + CurrentDatabase, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }
        }
        
        public BsonDocument RunCommandOnDB(string pdatabasename,string cmd)
        {

            try
            {
                IMongoDatabase db = Client.GetDatabase(pdatabasename);
                var command = new JsonCommand<BsonDocument>(cmd);

                BsonDocument res =db.RunCommand(command);
               
                return res;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Run DB Command " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
     
        public List<DatabaseCollection> GetDatabaseNames()
        {

            try
            {
                var dbList = Client.ListDatabases().ToList();
                Databases = new List<DatabaseCollection>();
                foreach (string item in dbList)
                {
                    IMongoDatabase db = Client.GetDatabase(item);
                    DatabaseCollection t = new DatabaseCollection();
                    t.DatabasName = item;
                    t.Collections = db.ListCollectionNames().ToList();
                   
                    foreach (string col in t.Collections)
                    {

                        Entities.Add(GetEntityStructure(col));

                    }

                }

                return Databases;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", "Could not Get Databases From RavenDB " + ConnProp.ConnectionName, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
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
        public MongoClient OpenStore(string pUrl)
        {

            try
            { //MongoClientSettings settings = new MongoClientSettings();

                     Client = new MongoClient(pUrl);
                     Database = Client.GetDatabase(CurrentDatabase);
                    var t = Database.ListCollectionNames();
                     Collections = t.ToList();
                    State = ConnectionState.Open;
               
                return Client;
            }
            catch (Exception ex)
            {
                State = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Error", "Could not Open MongoDb Connection " + ex.Message, DateTime.Now, -1, ConnProp.Url, Errors.Failed);
                return null;
            }



        }
     

    }
}
