using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Json
{
    public class JsonDataSource : IDataSource, IDisposable
    {
        public JsonDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = DataSourceType.Json;
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName.Equals(datasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            FileName = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(FileName))
            {
                ReadJson(FileName);
            }
        }
        #region "Properties"
        private bool disposedValue;
        private JObject _rootJsonObject = null;
        private string  jsonContent=null;
        private string lastentityname;

        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Json;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }=new List<string>();
        public List<EntityStructure> Entities { get; set; }=new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }= ConnectionState.Closed;
        public string FileName { get; private set; }
        public bool ObjectsCreated { get; private set; }
        public EntityStructure DataStruct { get; private set; }

        private Type enttype;

        public event EventHandler<PassedArgs> PassEvent;
        #endregion


        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            return Entities.Any(e => e.EntityName == EntityName);
        }

        public ConnectionState Closeconnection()
        {
            SaveJson(FileName);
            return ConnectionState.Closed;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
               foreach (EntityStructure entity in entities)
                {
                    if (!CreateEntityAs(entity))
                    {
                        return new ErrorsInfo { Message = $"Failed to create entity: {entity.EntityName}", Flag = Errors.Failed };
                    }
                }
                return new ErrorsInfo { Message = "Entities created successfully.", Flag = Errors.Ok };
            }
            catch (Exception ex)
            {

                return new ErrorsInfo { Message = $"Failed to create entity: {ex.Message}", Flag = Errors.Failed };
            }
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            // Access the root JSON object.
            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                // Root JSON object not available, can't create the entity.
                return false;
            }

            // Check if the entity already exists.
            if (rootJson.SelectToken(entity.EntityName) != null)
            {
                // Entity already exists, can't create a new one with the same name.
                return false;
            }

            // Create a new JArray or JObject based on your needs.
            JArray newArray = new JArray();
            // JObject newObject = new JObject(); // Uncomment if you want to create a JObject instead.

            // Add the new array or object to the root.
            rootJson.Add(new JProperty(entity.EntityName, newArray));

            // Optionally, update the EntityStructure to reflect the new entity's details.
            entity.EntityPath = $"$.{entity.EntityName}";
            Entities.Add(entity);

           

            return true;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object criteria)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            // Access the root JSON object.
            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                return new ErrorsInfo { Message = "Root JSON object is not available.", Flag = Errors.Failed };
            }

            // Use the path to select the specific part of the JSON.
            JToken entityToken = rootJson.SelectToken(entityStructure.EntityPath);
            if (entityToken == null)
            {
                return new ErrorsInfo { Message = "Entity path not found.", Flag = Errors.Failed };
            }

            try
            {
                if (entityToken is JArray entityArray)
                {
                    // For arrays, remove the item that matches the criteria.
                    // This assumes the criteria is an object with properties that should match the item to be removed.
                    var itemToRemove = entityArray.FirstOrDefault(item => item.MatchesCriteria(criteria));
                    if (itemToRemove != null)
                    {
                        itemToRemove.Remove();
                        return new ErrorsInfo { Message = "Item removed successfully.", Flag = Errors.Ok };
                    }

                }
                else
                {
                    // For a single object, you can simply remove it or set it to null.
                    // The exact behavior depends on your application's needs.
                    entityToken.Remove();
                    return new ErrorsInfo { Message = "Entity removed successfully.", Flag = Errors.Ok };
                }
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Message = $"Error deleting entity: {ex.Message}", Flag = Errors.Failed };
            }

            return new ErrorsInfo { Message = "No matching item found to remove.", Flag = Errors.Failed };
        }

        // Helper method to check if a JSON token matches the deletion criteria.
        public IErrorsInfo EndTransaction(PassedArgs args)
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
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }
        public List<string> GetEntitesList()
        {
            if (Entities.Count > 0)
            {
                return Entities.Select(e => e.EntityName).ToList();
            }else
            {
                return null;
            }
            
        }

        public object GetEntity(string entityName, List<AppFilter> filter)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == entityName);
            if (entityStructure == null)
            {
                return new List<object>();  // Or throw an appropriate exception
            }

            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                return new List<object>();  // Or throw an appropriate exception if the root JSON object is not set.
            }

            JToken token = rootJson.SelectToken(entityStructure.EntityPath);
            List<object> resultList = new List<object>();

            if (token is JArray arrayToken)
            {
                foreach (var item in arrayToken.Children<JObject>())
                {
                    dynamic record = new ExpandoObject();
                    var recordDictionary = (IDictionary<string, object>)record;
                    foreach (var property in item.Properties())
                    {
                        recordDictionary[property.Name] = property.Value.ToObject<object>();
                    }
                    // Apply any filters to the record here if necessary
                    UpdateEntityStructureWithMissingFields(item, entityStructure);
                    resultList.Add(record);
                }
            }
            else if (token is JObject objectToken)
            {
                dynamic record = new ExpandoObject();
                var recordDictionary = (IDictionary<string, object>)record;
                foreach (var property in objectToken.Properties())
                {
                    recordDictionary[property.Name] = property.Value.ToObject<object>();
                }
                UpdateEntityStructureWithMissingFields((JObject)token, entityStructure);
                // Apply any filters to the record here if necessary
                resultList.Add(record);
            }

            return resultList;
        }
        public object GetEntity(string entityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == entityName);
            if (entityStructure == null)
            {
                return new List<object>();  // Or throw an appropriate exception
            }

            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                return new List<object>();  // Or throw an appropriate exception if the root JSON object is not set.
            }

            JToken token = rootJson.SelectToken(entityStructure.EntityPath);
            List<object> resultList = new List<object>();

            if (token is JArray arrayToken)
            {
                foreach (var item in arrayToken.Children<JObject>())
                {
                    dynamic record = new ExpandoObject();
                    var recordDictionary = (IDictionary<string, object>)record;
                    foreach (var property in item.Properties())
                    {
                        recordDictionary[property.Name] = property.Value.ToObject<object>();
                    }
                    // Apply any filters to the record here if necessary
                    UpdateEntityStructureWithMissingFields(item, entityStructure);
                    resultList.Add(record);
                }
            }
            else if (token is JObject objectToken)
            {
                dynamic record = new ExpandoObject();
                var recordDictionary = (IDictionary<string, object>)record;
                foreach (var property in objectToken.Properties())
                {
                    recordDictionary[property.Name] = property.Value.ToObject<object>();
                }
                UpdateEntityStructureWithMissingFields((JObject)token, entityStructure);
                // Apply any filters to the record here if necessary
                resultList.Add(record);
            }

            return resultList;
        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
           return Task.FromResult(GetEntity(EntityName, Filter));
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }
        public int GetEntityIdx(string entityName)
        {
            return Entities.FindIndex(e => e.EntityName == entityName);
        }
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            return Entities.FirstOrDefault(e => e.EntityName == EntityName);
        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return Entities.FirstOrDefault(e => e.EntityName == fnd.EntityName);
        }

        public Type GetEntityType(string EntityName)
        {

            string filenamenoext = EntityName;
            DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }




        public ConnectionState Openconnection()
        {
            // Check if the file exists
            if (!File.Exists(FileName))
            {
                // If the file doesn't exist, you might want to create it or return an error.
                // Here, we'll choose to create an empty JSON file.
                File.WriteAllText(FileName, "{}");
            }

            try
            {
                // Attempt to read and parse the file to verify it contains valid JSON.
                ReadJson(FileName);
                ConnectionStatus = ConnectionState.Open;
                // If this point is reached without an exception, the file is considered 'open' and accessible.
                return ConnectionState.Open;
            }
            catch (JsonReaderException)
            {
                ConnectionStatus = ConnectionState.Broken;
                // The file content is not a valid JSON.
                return ConnectionState.Broken;
            }
            catch (Exception)
            {
                ConnectionStatus = ConnectionState.Closed;
                // Some other error occurred (e.g., file access permissions).
                return ConnectionState.Closed;
            }
        }

        //var titles = RunQuery("$.books[*].title");
        //var booksAfter1900 = RunQuery("$.books[?(@.year > 1900)]");
        //var authorOf1984 = RunQuery("$.books[?(@.title == '1984')].author");

        public object RunQuery(string qrystr)
        {
            // Ensure the JSON is loaded.
            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                // Handle the error appropriately.
                return new List<object>();
            }

            try
            {
                // Use JSONPath to query the JSON structure.
                var queryResult = rootJson.SelectTokens(qrystr);
                List<object> resultList = new List<object>();
                EntityStructure entityStructure = Entities[GetEntityIdx(JsonExtensions.ExtractEntityNameFromQuery(qrystr))];
                foreach (var result in queryResult)
                {
                    if (result is JObject)
                    {
                        dynamic record = new ExpandoObject();
                        var recordDictionary = (IDictionary<string, object>)record;
                        foreach (var property in ((JObject)result).Properties())
                        {
                            recordDictionary[property.Name] = property.Value.ToObject<object>();
                            if(entityStructure != null)
                            {
                                // Update the EntityStructure with any missing fields from the query result
                                UpdateEntityStructureWithMissingFields((JObject)result,entityStructure );
                            }
                            

                        }
                        resultList.Add(record);
                    }
                    //else if (result is JValue)
                    //{
                    //    resultList.Add(((JValue)result).Value);
                    //}
                    //else
                    //{
                    //    // For JArrays or other types, you can decide how to handle them.
                    //    // For simplicity, add them directly to the result list.
                    //    resultList.Add(result.ToObject<object>());
                    //}
                }

                return resultList;
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed.
                Console.WriteLine($"An error occurred while running the query: {ex.Message}");
                return new List<object>();
            }
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // Find the EntityStructure with the given name.
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            // Retrieve the corresponding part of the JSON.
            JToken entityToken = _rootJsonObject.SelectToken(entityStructure.EntityPath);
            if (entityToken == null || !(entityToken is JArray))
            {
                return new ErrorsInfo { Message = "Target entity is not an array or collection.", Flag = Errors.Failed };
            }

            JArray targetArray = (JArray)entityToken;
            if (UploadData is JArray newDataArray)
            {
                try
                {
                    // Example: Replace the entire array
                    targetArray.Replace(newDataArray);

                    // Optionally, report progress
                    progress?.Report(new PassedArgs { Messege = "Update completed", ParameterInt1 = 100 });

                    return new ErrorsInfo { Message = "Entities updated successfully.", Flag = Errors.Ok };
                }
                catch (Exception ex)
                {
                    return new ErrorsInfo { Message = $"Failed to update entities: {ex.Message}", Flag = Errors.Failed };
                }
            }
            else
            {
                return new ErrorsInfo { Message = "Invalid data for update.", Flag = Errors.Failed };
            }
        }
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            // Find the EntityStructure with the given name.
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                // Return an error if the entity does not exist.
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            // Retrieve the corresponding part of the JSON.
            JToken entityToken = _rootJsonObject.SelectToken(entityStructure.EntityPath);
            if (entityToken == null)
            {
                // Return an error if the entity path does not exist.
                return new ErrorsInfo { Message = "Entity path not found.", Flag = Errors.Failed };
            }

            try
            {
                // Update the entity with the new data.
                if (entityToken.Type == JTokenType.Object && UploadDataRow is JObject newData)
                {
                    // For an object, replace each existing property with the new data.
                    foreach (var prop in newData.Properties())
                    {
                        ((JObject)entityToken)[prop.Name] = prop.Value;
                    }
                }
                else if (entityToken.Type == JTokenType.Array && UploadDataRow is JArray newArrayData)
                {
                    // For an array, replace the entire array.
                    ((JArray)entityToken).Replace(newArrayData);
                }
                else
                {
                    // Return an error if the data types do not match or are not supported.
                    return new ErrorsInfo { Message = "Incompatible data type for update.", Flag = Errors.Failed };
                }

                return new ErrorsInfo { Message = "Entity updated successfully.", Flag = Errors.Ok };
            }
            catch (Exception ex)
            {
                // Return an error if the update operation fails.
                return new ErrorsInfo { Message = $"Failed to update entity: {ex.Message}", Flag = Errors.Failed };
            }
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            // Find the EntityStructure with the given name.
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                // Return an error if the entity does not exist.
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            // Retrieve the corresponding part of the JSON.
            JToken entityToken = _rootJsonObject.SelectToken(entityStructure.EntityPath);
            if (entityToken == null || !(entityToken is JArray))
            {
                // Return an error if the path does not point to an array.
                return new ErrorsInfo { Message = "Target entity is not an array.", Flag = Errors.Failed };
            }

            JArray entityArray = (JArray)entityToken;

            try
            {
                // Insert the data into the array.
                entityArray.Add(JToken.FromObject(InsertedData));
                return new ErrorsInfo { Message = "Data inserted successfully.", Flag = Errors.Ok };
            }
            catch (Exception ex)
            {
                // Return an error if the insertion fails.
                return new ErrorsInfo { Message = $"Failed to insert data: {ex.Message}", Flag = Errors.Failed };
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Entities = null;
                    EntitiesNames = null;
                    _rootJsonObject = null;
                    DataStruct = null;
                    Dataconnection = null;
                    ErrorObject = null;
                    

                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JsonDataSource2()
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
        #region "Json File"
        public void SaveJson(string jsonFilePath)
        {
            // Ensure the file path is valid.
            if (string.IsNullOrWhiteSpace(jsonFilePath))
            {
                // Handle the error or log as needed.
                Console.WriteLine("Invalid file path.");
                return;
            }

            // Access the root JSON object.
            JObject rootJson = GetRootJsonObject();
            if (rootJson == null)
            {
                // Root JSON object not available, can't save the file.
                Console.WriteLine("Root JSON object is not available.");
                return;
            }

            try
            {
                // Write the JSON content to the specified file with indentation for readability.
                File.WriteAllText(jsonFilePath, rootJson.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                // Handle exceptions, such as issues with file write access.
                Console.WriteLine($"An error occurred while saving the JSON file: {ex.Message}");
            }
        }

        public void ReadJson(string jsonFilePath)
        {
            // Update the file name only if it's different from the current file or if _rootJsonObject is not initialized.
            if (FileName != jsonFilePath || _rootJsonObject == null)
            {
                FileName = jsonFilePath;
                InitializeRootJsonObject();
            }

            // Use the initialized _rootJsonObject for further processing.
            JToken json = _rootJsonObject;

            // Proceed to work with the json object.
            if (json.Type == JTokenType.Array)
            {
                ParseJsonArray(json as JArray, null, "RootArray", "$");
            }
            else if (json.Type == JTokenType.Object)
            {
                ParseJsonObject(json as JObject, null, "RootObject", "$");
            }
            // If _rootJsonObject is empty, this part will be skipped.
        }

        private void ParseJsonObject(JObject jObject, EntityStructure parentStructure, string entityName, string currentPath)
        {
            if (entityName != null) // Skip creating EntityStructure for root
            {
                var currentStructure = CreateEntityStructure(parentStructure, entityName, currentPath);
                Entities.Add(currentStructure);
                parentStructure = currentStructure;
            }

            foreach (var property in jObject.Properties())
            {
                if (parentStructure != null)
                {
                    var field = CreateEntityField(property);
                    parentStructure.Fields.Add(field);
                }

                string propertyPath = $"{currentPath}.{property.Name}";

                if (property.Value is JObject)
                {
                    ParseJsonObject(property.Value as JObject, parentStructure, property.Name, propertyPath);
                }
                else if (property.Value is JArray)
                {
                    ParseJsonArray(property.Value as JArray, parentStructure, property.Name, propertyPath);
                }
            }
        }

        private void ParseJsonArray(JArray jArray, EntityStructure parentStructure, string entityName, string currentPath)
        {
            int index = 0;
            foreach (var item in jArray)
            {
                string itemPath = $"{currentPath}[{index}]";

                if (item is JObject)
                {
                    // Entity name for nested objects in the array is derived from the index
                    ParseJsonObject(item as JObject, parentStructure, $"{entityName}_{index}", itemPath);
                }
                else if (item is JArray)
                {
                    // Entity name for nested arrays in the array is derived from the index
                    ParseJsonArray(item as JArray, parentStructure, $"{entityName}_{index}", itemPath);
                }
                index++;
            }
        }

        private EntityStructure CreateEntityStructure(EntityStructure parent, string name, string path)
        {
            return new EntityStructure
            {
                EntityName = name,
                ParentId = parent?.Id ?? 0,
                EntityPath = path,
                Fields = new List<EntityField>(),
            };
        }

        private EntityField CreateEntityField(JProperty property)
        {
            return new EntityField
            {
                fieldname = property.Name,
                fieldtype = JsonExtensions.DetermineFieldType(property.Value),
            };
        }


        private void InitializeRootJsonObject()
        {
            // Check if _rootJsonObject is already initialized
            if (_rootJsonObject != null) return;

            // If the file exists and is not empty, read and parse it.
            if (File.Exists(FileName) && new FileInfo(FileName).Length > 0)
            {
                 jsonContent = File.ReadAllText(FileName);
                try
                {
                    _rootJsonObject = JObject.Parse(jsonContent);
                }
                catch (JsonReaderException)
                {
                    // If the file content is not valid JSON, initialize an empty JObject.
                    _rootJsonObject = new JObject();
                }
            }
            else
            {
                // If the file does not exist or is empty, initialize an empty JObject.
                _rootJsonObject = new JObject();
            }
        }


        private JObject GetRootJsonObject()
        {
            return _rootJsonObject;
        }

        public void UpdateEntityStructureWithMissingFields(JObject record, EntityStructure entityStructure)
        {
            // Iterate over each property in the incoming JSON record
            foreach (var property in record.Properties())
            {
                // Check if this property is already represented in the EntityStructure
                bool fieldExists = entityStructure.Fields.Any(f => f.fieldname == property.Name);

                // If the field doesn't exist in the EntityStructure, add it
                if (!fieldExists)
                {
                    entityStructure.Fields.Add(new EntityField
                    {
                        fieldname = property.Name,
                        // Determining the type can be simplistic (as shown) or more complex based on your needs
                        fieldtype = property.Value.Type.ToString(),
                        // Add additional field details as necessary
                    });
                }
            }
        }

        #endregion
        #region "Utility"
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
                // command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        #endregion
    }
}
