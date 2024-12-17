using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

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
        private JToken _rootJsonObject = null;
        private JArray _rootJsonArray = null;
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
            // Access the root JSON array.
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                // Root JSON array not available, initialize a new array.
                rootJsonArray = new JArray();
                _rootJsonObject["RootArray"] = rootJsonArray;
            }

            // Check if the entity already exists by checking if an entity with the same name is present.
            if (Entities.Any(e => e.EntityName == entity.EntityName))
            {
                // Entity already exists, can't create a new one with the same name.
                return false;
            }

            // Create a new JObject to represent the entity.
            JObject newEntityObject = new JObject();

            // Populate the new entity object with fields.
            foreach (var field in entity.Fields)
            {
                newEntityObject.Add(new JProperty(field.fieldname, JValue.CreateNull()));
            }

            // Add the new object to the root array.
            rootJsonArray.Add(newEntityObject);

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

            // Access the root JSON array.
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                // Find the entity in the array.
                JToken entityToken = rootJsonArray.FirstOrDefault(item => item[EntityName] != null);
                if (entityToken == null)
                {
                    return new ErrorsInfo { Message = "Entity path not found.", Flag = Errors.Failed };
                }

                // Remove the entity that matches the criteria.
                if (entityToken is JArray entityArray)
                {
                    var itemToRemove = entityArray.FirstOrDefault(item => item.MatchesCriteria(criteria));
                    if (itemToRemove != null)
                    {
                        itemToRemove.Remove();
                        return new ErrorsInfo { Message = "Item removed successfully.", Flag = Errors.Ok };
                    }
                }
                else
                {
                    // For a single object, remove it.
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

        // Helper method to get the root JSON array.
    

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

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new List<object>();  // Or throw an appropriate exception if the root JSON array is not set.
            }

            List<object> resultList = new List<object>();
            Type entityType = GetEntityType(entityName);

            foreach (var item in rootJsonArray.Children<JObject>())
            {
                var entity = Activator.CreateInstance(entityType);

                foreach (var property in item.Properties())
                {
                    var propInfo = entityType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        try
                        {
                            var fieldInfo = entityStructure.Fields.FirstOrDefault(p => p.fieldname == property.Name);
                            if (fieldInfo != null)
                            {
                                Type fieldType = Type.GetType(fieldInfo.fieldtype);
                                object value;

                                // Special handling for $oid
                                if (property.Value.Type == JTokenType.Object && property.Value["$oid"] != null)
                                {
                                    value = property.Value["$oid"].ToString();
                                }
                                else
                                {
                                    value = property.Value.ToObject(fieldType);
                                }

                                propInfo.SetValue(entity, value);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error setting property {property.Name}: {ex.Message}");
                        }

                    }
                }

                // Apply any filters to the record here if necessary
                UpdateEntityStructureWithMissingFields(item, entityStructure);
                resultList.Add(entity);
            }
            enttype = GetEntityType(entityName);
            Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
            // Prepare the arguments for the constructor
            object[] constructorArgs = new object[] { resultList };

            // Create an instance of UnitOfWork<T> with the specific constructor
            // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
            object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
            return uowInstance;
           // return resultList;
        }
        // Method to apply filters to a record
        private bool ApplyFilters(IDictionary<string, object> record, List<AppFilter> filters)
        {
            foreach (var filter in filters)
            {
                if (!record.ContainsKey(filter.FieldName) ||
                    !record[filter.FieldName].ToString().Contains(filter.FilterValue.ToString()))
                {
                    return false;
                }
            }
            return true;
        }
        public object GetEntity(string entityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == entityName);
            if (entityStructure == null)
            {
                return new List<object>();  // Or throw an appropriate exception
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new List<object>();  // Or throw an appropriate exception if the root JSON array is not set.
            }

            List<object> resultList = new List<object>();

            // Calculate the starting index for the desired page
            int startIndex = (pageNumber - 1) * pageSize;

            // Use LINQ to paginate the array
            var pagedItems = rootJsonArray.Skip(startIndex).Take(pageSize);

            foreach (var item in pagedItems.OfType<JObject>())
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
            DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Beep", EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.MyType;
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
        public object RunQuery(string qrystr)
        {
            // Ensure the JSON is loaded.
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                // Handle the error appropriately.
                return new List<object>();
            }

            try
            {
                // Use JSONPath to query the JSON structure.
                var queryResult = rootJsonArray.SelectTokens(qrystr);
                List<object> resultList = new List<object>();
                string entityName = JsonExtensions.ExtractEntityNameFromQuery(qrystr);
                EntityStructure entityStructure = Entities.FirstOrDefault(e => e.EntityName == entityName);
                Type entityType = GetEntityType(entityName);

                foreach (var result in queryResult)
                {
                    if (result is JObject jObject)
                    {
                        var entity = Activator.CreateInstance(entityType);

                        foreach (var property in jObject.Properties())
                        {
                            var propInfo = entityType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                            if (propInfo != null && propInfo.CanWrite)
                            {
                                try
                                {
                                    var fieldInfo = entityStructure.Fields.FirstOrDefault(p => p.fieldname == property.Name);
                                    if (fieldInfo != null)
                                    {
                                        Type fieldType = Type.GetType(fieldInfo.fieldtype);
                                        object value;

                                        // Special handling for $oid
                                        if (property.Value.Type == JTokenType.Object && property.Value["$oid"] != null)
                                        {
                                            value = property.Value["$oid"].ToString();
                                        }
                                        else
                                        {
                                            value = property.Value.ToObject(fieldType);
                                        }

                                        propInfo.SetValue(entity, value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error setting property {property.Name}: {ex.Message}");
                                }
                            }
                        }

                        // Apply any filters to the record here if necessary
                        UpdateEntityStructureWithMissingFields(jObject, entityStructure);
                        resultList.Add(entity);
                    }
                    else if (result is JValue jValue)
                    {
                        resultList.Add(jValue.Value);
                    }
                    else
                    {
                        // For JArrays or other types, you can decide how to handle them.
                        // For simplicity, add them directly to the result list.
                        resultList.Add(result.ToObject<object>());
                    }
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
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            if (UploadData is JArray newDataArray)
            {
                try
                {
                    // Replace the entire array
                    rootJsonArray.Replace(newDataArray);

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
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                if (UploadDataRow is JObject newData)
                {
                    // Find the object in the array to update based on some unique property, e.g., "_id".
                    var itemToUpdate = rootJsonArray.FirstOrDefault(item => item["_id"]?["$oid"]?.ToString() == newData["_id"]?["$oid"]?.ToString());
                    if (itemToUpdate != null)
                    {
                        foreach (var prop in newData.Properties())
                        {
                            ((JObject)itemToUpdate)[prop.Name] = prop.Value;
                        }
                        return new ErrorsInfo { Message = "Entity updated successfully.", Flag = Errors.Ok };
                    }
                    else
                    {
                        return new ErrorsInfo { Message = "Item to update not found.", Flag = Errors.Failed };
                    }
                }
                else
                {
                    return new ErrorsInfo { Message = "Invalid data type for update.", Flag = Errors.Failed };
                }
            }
            catch (Exception ex)
            {
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
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                // Return an error if the root JSON array is not available.
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                // Insert the data into the array.
                rootJsonArray.Add(JToken.FromObject(InsertedData));
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

            // Access the root JSON array.
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                // Root JSON array not available, can't save the file.
                Console.WriteLine("Root JSON array is not available.");
                return;
            }

            try
            {
                // Write the JSON content to the specified file with indentation for readability.
                File.WriteAllText(jsonFilePath, rootJsonArray.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                // Handle exceptions, such as issues with file write access.
                Console.WriteLine($"An error occurred while saving the JSON file: {ex.Message}");
            }
        }
        public void ReadJson(string jsonFilePath)
        {
            // Update the file name only if it's different from the current file or if _rootJsonArray is not initialized.
            if (FileName != jsonFilePath || _rootJsonObject == null)
            {
                FileName = jsonFilePath;
                InitializeRootJsonObject();
            }

            // Use the initialized _rootJsonArray for further processing.
            JToken json = _rootJsonObject;
            string entityName = Path.GetFileNameWithoutExtension(jsonFilePath);
            if (json.Type == JTokenType.Array)
            {
                // Process the JSON array as a single entity
                ParseJsonArray(json as JArray, null, entityName, "$");
            }else
            if (json.Type == JTokenType.Object)
            {
                // Process the JSON object as a single entity
                ParseJsonObject(json as JObject, null, entityName, "$");
            }
            else
            {
                Console.WriteLine("Unexpected JSON root type: " + json.Type);
            }
        }
        private void ParseJsonObject(JObject jObject, EntityStructure parentStructure, string entityName, string currentPath)
        {
           
          // use parsejsonarray to parse the first object
            ParseJsonArray(new JArray(jObject), parentStructure, entityName, currentPath);
        }
        private void ParseJsonArray(JArray jArray, EntityStructure parentStructure, string entityName, string currentPath)
        {
            
            // Create a single EntityStructure for the entire array
            var currentStructure = CreateEntityStructure(parentStructure, entityName, currentPath);
            Entities.Clear();
            Entities.Add(currentStructure);
            EntitiesNames.Clear();
            EntitiesNames.Add(entityName);

            // Assuming all objects in the array have the same structure, use the first object to define the fields
            if (jArray.Count > 0 && jArray[0] is JObject firstObject)
            {
                JObject firstrecord;
                if (firstObject.Property("RootArray") != null)
                {
                    _rootJsonArray = firstObject.Property("RootArray").Value as JArray;
                     firstrecord = _rootJsonArray[0] as JObject;

                }
                else
                {
                    _rootJsonArray = jArray;
                    firstrecord = firstObject;
                }
                foreach (var property in firstrecord.Properties())
                {
                    JObject keyValuePairs = new JObject();
                    var field = CreateEntityField(property);
                    if (field.fieldtype.Equals("object", StringComparison.InvariantCultureIgnoreCase))
                    {
                        field.fieldtype = "string";
                    }
                    if (!field.fieldtype.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentStructure.Fields.Add(field);
                    }
                    else
                        Console.WriteLine("Array field not supported");



                }

            }

            // Parse each object in the array to ensure all fields are captured
            foreach (var item in jArray)
            {
                if (item is JObject jObject)
                {
                    UpdateEntityStructureWithMissingFields(jObject, currentStructure);
                }
            }
        }
        private JArray GetRootJsonArray()
        {

            if (_rootJsonObject != null && _rootJsonArray!=null)
            {
                return _rootJsonArray;
            }
            return null;
        }
        private EntityStructure CreateEntityStructure(EntityStructure parent, string name, string path)
        {
            return new EntityStructure
            {
                EntityName = name,
                DatasourceEntityName = name,
                OriginalEntityName = name,
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
                BaseColumnName = property.Name,
                fieldtype = JsonExtensions.DetermineFieldType(property.Value),
            };
        }
        private void InitializeRootJsonObject()
        {
            // Check if _rootJsonArray is already initialized
            if (_rootJsonObject != null) return;

            // If the file exists and is not empty, read and parse it.
            if (File.Exists(FileName) && new FileInfo(FileName).Length > 0)
            {
                jsonContent = File.ReadAllText(FileName);
                try
                {
                    var jsonToken = JToken.Parse(jsonContent);
                    if (jsonToken is JArray jArray)
                    {
                        _rootJsonObject = jArray;
                    }else if(jsonToken is JObject jObject)
                    {
                        _rootJsonObject = (JObject)jsonToken;
                    }
                    else
                    {
                        Console.WriteLine("Unsupported JSON root type: " + jsonToken.Type);
                        _rootJsonObject = new JArray();
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine("Error parsing JSON: " + ex.Message);
                    _rootJsonObject = new JArray();
                }
            }
            else
            {
                _rootJsonObject = new JArray();
                Console.WriteLine("File does not exist or is empty. Initialized an empty JArray.");
            }
        }
        private JToken GetRootJsonObject()
        {
            return _rootJsonObject;
        }
        public void UpdateEntityStructureWithMissingFields(JObject record, EntityStructure entityStructure)
        {
            foreach (var property in record.Properties())
            {
                bool fieldExists = entityStructure.Fields.Any(f => f.fieldname == property.Name);

                if (!fieldExists)
                {
                    var field = CreateEntityField(property);
                    if (field.fieldtype.Equals("object", StringComparison.InvariantCultureIgnoreCase))
                    {
                        field.fieldtype = "string";
                    }
                    if (!field.fieldtype.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                    {
                        entityStructure.Fields.Add(field);
                    }
             
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
