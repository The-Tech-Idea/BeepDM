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
using System.ComponentModel;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Json
{
    /// <summary>
    /// JsonDataSource provides CRUD operations on a JSON file acting as a data source.
    /// It implements IDataSource and manages entities derived from JSON objects/arrays.
    /// This class supports reading, writing, updating, and deleting JSON objects from a file,
    /// and includes the ability to generate EntityStructures dynamically.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.Json)]
    public class JsonDataSource : IDataSource, IDisposable
    {
      
        /// <summary>
        /// Constructor initializing JsonDataSource with a specified data source name, logger, editor, and datasource type.
        /// It attempts to load the JSON file associated with the connection specified in the DMEEditor.ConfigEditor.
        /// </summary>
        /// <param name="datasourcename">Name of the data source</param>
        /// <param name="logger">Logger implementation</param>
        /// <param name="pDMEEditor">Editor reference</param>
        /// <param name="databasetype">Type of the data source</param>
        /// <param name="per">Error object to accumulate errors</param>
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
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections
                .Where(c => c.ConnectionName.Equals(datasourcename, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

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
        private string jsonContent = null;
        private string lastentityname;
        private Type enttype;

        /// <summary>
        /// Delimiter used to separate columns if needed.
        /// </summary>
        public string ColumnDelimiter { get; set; }

        /// <summary>
        /// Delimiter used to separate parameters if needed.
        /// </summary>
        public string ParameterDelimiter { get; set; }

        /// <summary>
        /// GUID of the data source instance.
        /// </summary>
        public string GuidID { get; set; }

        /// <summary>
        /// Type of the data source (e.g., Json).
        /// </summary>
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Json;

        /// <summary>
        /// Category of the data source (e.g., FILE).
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;

        /// <summary>
        /// Data connection used to manage file paths and properties.
        /// </summary>
        public IDataConnection Dataconnection { get; set; }

        /// <summary>
        /// Name of the data source.
        /// </summary>
        public string DatasourceName { get; set; }

        /// <summary>
        /// Error object instance used to store and report errors.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Identifier for this data source instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Logger instance for logging operations.
        /// </summary>
        public IDMLogger Logger { get; set; }

        /// <summary>
        /// List of entity names recognized by this data source.
        /// </summary>
        public List<string> EntitiesNames { get; set; } = new List<string>();

        /// <summary>
        /// List of entity structures representing schema-like information for each entity.
        /// </summary>
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        /// <summary>
        /// Reference to the main editor instance.
        /// </summary>
        public IDMEEditor DMEEditor { get; set; }

        /// <summary>
        /// Current connection state of the data source.
        /// </summary>
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        /// <summary>
        /// Path to the JSON file managed by this data source.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Indicates whether objects (structures, types) have been created.
        /// </summary>
        public bool ObjectsCreated { get; private set; }

        /// <summary>
        /// Holds the current entity structure being operated on.
        /// </summary>
        public EntityStructure DataStruct { get; private set; }

        /// <summary>
        /// Event triggered to pass arguments between components.
        /// </summary>
        public event EventHandler<PassedArgs> PassEvent;
        #endregion

        #region "Transaction and Database Operations (Not Implemented)"

        /// <summary>
        /// Begin a database transaction. Not implemented for JSON.
        /// </summary>
        /// <param name="args">Passed arguments</param>
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Commit a database transaction. Not implemented for JSON.
        /// </summary>
        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends a database transaction. Not implemented for JSON.
        /// </summary>
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a SQL query against the data source. Not applicable for JSON.
        /// </summary>
        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a provided script (DML/DDL). Not implemented for JSON.
        /// </summary>
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region "Connection Management"
        /// <summary>
        /// Opens a connection to the JSON file by reading and parsing it.
        /// Returns the resulting state.
        /// </summary>
        public ConnectionState Openconnection()
        {
            if (!File.Exists(FileName))
            {
                // If file doesn't exist, create an empty JSON file.
                File.WriteAllText(FileName, "{}");
            }

            try
            {
                // Verify file is valid JSON.
                ReadJson(FileName);
                ConnectionStatus = ConnectionState.Open;
                return ConnectionState.Open;
            }
            catch (JsonReaderException)
            {
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
            catch (Exception)
            {
                ConnectionStatus = ConnectionState.Closed;
                return ConnectionState.Closed;
            }
        }

        /// <summary>
        /// Closes the connection to the JSON file and ensures that any changes are saved.
        /// </summary>
        public ConnectionState Closeconnection()
        {
            SaveJson(FileName);
            return ConnectionState.Closed;
        }
        #endregion

        #region "Entity Management"
        /// <summary>
        /// Checks if an entity with the specified name exists in the data source.
        /// </summary>
        public bool CheckEntityExist(string EntityName)
        {
            return Entities.Any(e => e.EntityName == EntityName);
        }

        /// <summary>
        /// Creates multiple entities as specified by a list of EntityStructure objects.
        /// </summary>
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

        /// <summary>
        /// Creates a single entity based on the provided EntityStructure.
        /// Note: This implementation assumes a root JSON array and adds a new JSON object for the entity schema.
        /// </summary>
        public bool CreateEntityAs(EntityStructure entity)
        {
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                // Initialize a new array if not present
                if (_rootJsonObject == null || !(_rootJsonObject is JObject rootObj))
                {
                    // If root object is null or not an object, make a new one
                    _rootJsonObject = new JObject();
                }

                rootJsonArray = new JArray();
                ((JObject)_rootJsonObject)["RootArray"] = rootJsonArray;
                _rootJsonArray = rootJsonArray;
            }

            if (Entities.Any(e => e.EntityName == entity.EntityName))
            {
                // Entity with this name already exists
                return false;
            }

            // Create a new JObject representing the entity
            JObject newEntityObject = new JObject();
            foreach (var field in entity.Fields)
            {
                newEntityObject.Add(new JProperty(field.FieldName, JValue.CreateNull()));
            }

            // Add to root array
            rootJsonArray.Add(newEntityObject);

            entity.EntityPath = $"$.{entity.EntityName}";
            Entities.Add(entity);

            return true;
        }

        /// <summary>
        /// Deletes an entity (or an item within it) matching certain criteria.
        /// Currently, the method is a placeholder. You need to provide a 'MatchesCriteria' extension or logic.
        /// </summary>
        public IErrorsInfo DeleteEntity(string EntityName, object criteria)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                // This assumes a certain structure and a 'MatchesCriteria' method which is not provided.
                // In practice, implement a logic to identify which item to remove based on 'criteria'.
                JToken entityToken = rootJsonArray.FirstOrDefault(item => item[EntityName] != null);
                if (entityToken == null)
                {
                    return new ErrorsInfo { Message = "Entity path not found.", Flag = Errors.Failed };
                }

                if (entityToken is JArray entityArray)
                {
                    // 'MatchesCriteria' is a placeholder for your filtering logic
                    var itemToRemove = entityArray.FirstOrDefault(item => item.MatchesCriteria(criteria));
                    if (itemToRemove != null)
                    {
                        itemToRemove.Remove();
                        return new ErrorsInfo { Message = "Item removed successfully.", Flag = Errors.Ok };
                    }
                }
                else
                {
                    // If it's a single object, remove it directly
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

      
        /// <summary>
        /// Returns a list of scripts to create entities, if applicable. Not implemented for JSON.
        /// </summary>
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a list of entity names managed by this data source.
        /// </summary>
        public IEnumerable<string> GetEntitesList()
        {
            if (Entities.Count > 0)
            {
                return Entities.Select(e => e.EntityName).ToList();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves entity data as objects. Each object is constructed dynamically from the JSON array.
        /// </summary>
        /// <param name="entityName">Name of the entity to retrieve</param>
        /// <param name="filter">Filter criteria (not fully implemented)</param>
        /// <returns>Collection of objects representing the entity data</returns>
        public IEnumerable<object> GetEntity(string entityName, List<AppFilter> filter)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(entityName)) return Enumerable.Empty<object>();
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (entityStructure == null) return Enumerable.Empty<object>();
            var rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null) return Enumerable.Empty<object>();

            // Prepare simple filters (equality only). If AppFilter has an Operator property elsewhere it will be ignored here.
            var activeFilters = (filter ?? new List<AppFilter>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrEmpty(f.FilterValue))
                .ToList();

            var results = new List<object>();

            foreach (var item in rootJsonArray.OfType<JObject>())
            {
                // Apply filters
                bool include = true;
                foreach (var f in activeFilters)
                {
                    var token = item.Property(f.FieldName, StringComparison.OrdinalIgnoreCase)?.Value;
                    if (token == null)
                    {
                        include = false; break;
                    }
                    // Simple string comparison (case-insensitive)
                    if (!string.Equals(token.Type == JTokenType.Object && token["$oid"] != null ? token["$oid"].ToString() : token.ToString(), f.FilterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        include = false; break;
                    }
                }
                if (!include) continue;

                // Build a dictionary for the row
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in item.Properties())
                {
                    object value;
                    if (prop.Value.Type == JTokenType.Object && prop.Value["$oid"] != null)
                        value = prop.Value["$oid"].ToString();
                    else
                        value = prop.Value.Type == JTokenType.Null ? null : prop.Value.ToObject<object>();
                    dict[prop.Name] = value;
                }

                // Update entity structure with any new fields discovered
                UpdateEntityStructureWithMissingFields(item, entityStructure);
                results.Add(dict);
            }

            return results;
        }

        /// <summary>
        /// Retrieves a paginated subset of the entity data.
        /// This version returns dynamic objects (ExpandoObjects).
        /// </summary>
        public PagedResult GetEntity(string entityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == entityName);
            if (entityStructure == null)
            {
                return new PagedResult();
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new PagedResult();
            }

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = int.MaxValue;

            // Prepare simple filters (equality only). If AppFilter has an Operator property elsewhere it will be ignored here.
            var activeFilters = (filter ?? new List<AppFilter>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrEmpty(f.FilterValue))
                .ToList();

            var resultList = new List<object>();
            int skip = (pageNumber - 1) * pageSize;
            int total = 0;

            foreach (var item in rootJsonArray.OfType<JObject>())
            {
                // Apply filters
                bool include = true;
                foreach (var f in activeFilters)
                {
                    var token = item.Property(f.FieldName, StringComparison.OrdinalIgnoreCase)?.Value;
                    if (token == null)
                    {
                        include = false; break;
                    }

                    if (!string.Equals(token.Type == JTokenType.Object && token["$oid"] != null ? token["$oid"].ToString() : token.ToString(), f.FilterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        include = false; break;
                    }
                }
                if (!include) continue;

                if (total >= skip && resultList.Count < pageSize)
                {
                    dynamic record = new ExpandoObject();
                    var recordDictionary = (IDictionary<string, object>)record;

                    foreach (var property in item.Properties())
                    {
                        recordDictionary[property.Name] = property.Value.ToObject<object>();
                    }

                    UpdateEntityStructureWithMissingFields(item, entityStructure);
                    resultList.Add(record);
                }

                total++;
            }

            return new PagedResult(resultList, pageNumber, pageSize, total);
        }

        /// <summary>
        /// Asynchronously retrieves entity data. Uses GetEntity internally.
        /// </summary>
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsyncImpl(EntityName, Filter);
        }

        private async Task<IEnumerable<object>> GetEntityAsyncImpl(string entityName, List<AppFilter> filter)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(entityName)) return Enumerable.Empty<object>();
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (entityStructure == null) return Enumerable.Empty<object>();
            var rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null) return Enumerable.Empty<object>();

            // Prepare simple filters (equality only). If AppFilter has an Operator property elsewhere it will be ignored here.
            var activeFilters = (filter ?? new List<AppFilter>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrEmpty(f.FilterValue))
                .ToList();

            var results = new List<object>();
            int yielded = 0;

            foreach (var item in rootJsonArray.OfType<JObject>())
            {
                // Apply filters
                bool include = true;
                foreach (var f in activeFilters)
                {
                    var token = item.Property(f.FieldName, StringComparison.OrdinalIgnoreCase)?.Value;
                    if (token == null)
                    {
                        include = false; break;
                    }
                    if (!string.Equals(token.Type == JTokenType.Object && token["$oid"] != null ? token["$oid"].ToString() : token.ToString(), f.FilterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        include = false; break;
                    }
                }
                if (!include) continue;

                // Build a dictionary for the row
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in item.Properties())
                {
                    object value;
                    if (prop.Value.Type == JTokenType.Object && prop.Value["$oid"] != null)
                        value = prop.Value["$oid"].ToString();
                    else
                        value = prop.Value.Type == JTokenType.Null ? null : prop.Value.ToObject<object>();
                    dict[prop.Name] = value;
                }

                UpdateEntityStructureWithMissingFields(item, entityStructure);
                results.Add(dict);

                // Keep large reads responsive.
                if (++yielded % 128 == 0)
                    await Task.Yield();
            }

            return results;
        }

        /// <summary>
        /// Returns foreign keys for an entity. Not implemented for JSON.
        /// </summary>
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the index of a given entity in the Entities list.
        /// </summary>
        public int GetEntityIdx(string entityName)
        {
            return Entities.FindIndex(e => e.EntityName == entityName);
        }

        /// <summary>
        /// Retrieves the EntityStructure for a given entity name.
        /// If refresh is true, it may re-parse the entity structure.
        /// </summary>
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            return Entities.FirstOrDefault(e => e.EntityName == EntityName);
        }

        /// <summary>
        /// Overload that retrieves the EntityStructure from an existing structure object.
        /// </summary>
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return Entities.FirstOrDefault(e => e.EntityName == fnd.EntityName);
        }

        /// <summary>
        /// Dynamically constructs a .NET Type for the given entity based on its fields.
        /// Uses DMTypeBuilder utility.
        /// </summary>
        public Type GetEntityType(string EntityName)
        {
            DMTypeBuilder.CreateNewObject(DMEEditor, "TheTechIdea.Beep", EntityName,
                Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.MyType;
        }

        /// <summary>
        /// Retrieves a scalar value from the data source. Not implemented for JSON.
        /// </summary>
        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously retrieves a scalar value. Not implemented for JSON.
        /// </summary>
        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a given query against the JSON data using JSONPath-like syntax.
        /// Attempts to match entity structure and return a list of objects.
        /// </summary>
        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Ensure JSON is loaded
            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return Enumerable.Empty<object>();
            }

            try
            {
                var queryResult = rootJsonArray.SelectTokens(qrystr);
                BindingList<object> resultList = new BindingList<object>();
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
                                    var fieldInfo = entityStructure.Fields.FirstOrDefault(p => p.FieldName == property.Name);
                                    if (fieldInfo != null)
                                    {
                                        Type Fieldtype = Type.GetType(fieldInfo.Fieldtype);
                                        object value;

                                        // Handle $oid
                                        if (property.Value.Type == JTokenType.Object && property.Value["$oid"] != null)
                                        {
                                            value = property.Value["$oid"].ToString();
                                        }
                                        else
                                        {
                                            value = property.Value.ToObject(Fieldtype);
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

                        UpdateEntityStructureWithMissingFields(jObject, entityStructure);
                        resultList.Add(entity);
                    }
                    else if (result is JValue jValue)
                    {
                        resultList.Add(jValue.Value);
                    }
                    else
                    {
                        // If it's JArray or other JTokens directly add them
                        resultList.Add(result.ToObject<object>());
                    }
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while running the query: {ex.Message}");
                return new BindingList<object>();
            }
        }
        #endregion

        #region "Data Modification"
        /// <summary>
        /// Updates multiple entities at once by replacing the root JSON array content.
        /// Expects UploadData to be a JArray that replaces the entire root array.
        /// </summary>
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            if (UploadData is JArray newDataArray)
            {
                try
                {
                    // Replace entire array
                    rootJsonArray.Replace(newDataArray);

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

        /// <summary>
        /// Updates a single entity by locating it (e.g., via an '_id') and replacing fields from UploadDataRow.
        /// Expects UploadDataRow to be a JObject containing updated fields.
        /// </summary>
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                if (UploadDataRow is JObject newData)
                {
                    // Update logic assumes unique '_id' field is present
                    var itemToUpdate = rootJsonArray.FirstOrDefault(item =>
                        item["_id"]?["$oid"]?.ToString() == newData["_id"]?["$oid"]?.ToString());

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

        /// <summary>
        /// Inserts a new entity/object into the JSON array.
        /// Expects InsertedData to be an object (e.g., JObject) that can be converted to JToken.
        /// </summary>
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName == EntityName);
            if (entityStructure == null)
            {
                return new ErrorsInfo { Message = "Entity not found.", Flag = Errors.Failed };
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                return new ErrorsInfo { Message = "Root JSON array is not available.", Flag = Errors.Failed };
            }

            try
            {
                rootJsonArray.Add(JToken.FromObject(InsertedData));
                return new ErrorsInfo { Message = "Data inserted successfully.", Flag = Errors.Ok };
            }
            catch (Exception ex)
            {
                return new ErrorsInfo { Message = $"Failed to insert data: {ex.Message}", Flag = Errors.Failed };
            }
        }
        #endregion

        #region "Dispose Pattern"
        /// <summary>
        /// Dispose pattern implementation to clean up resources.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed.</param>
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
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Public Dispose method to clean up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region "JSON File Management"
        /// <summary>
        /// Writes the current _rootJsonArray to the file with indentation.
        /// </summary>
        public void SaveJson(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
            {
                Console.WriteLine("Invalid file path.");
                return;
            }

            JArray rootJsonArray = GetRootJsonArray();
            if (rootJsonArray == null)
            {
                Console.WriteLine("Root JSON array is not available.");
                return;
            }

            try
            {
                File.WriteAllText(jsonFilePath, rootJsonArray.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving the JSON file: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the JSON content from the specified file and initializes internal structures.
        /// </summary>
        public void ReadJson(string jsonFilePath)
        {
            // Only re-parse if it's a new file or the object is not initialized
            if (FileName != jsonFilePath || _rootJsonObject == null)
            {
                FileName = jsonFilePath;
                InitializeRootJsonObject();
            }

            JToken json = _rootJsonObject;
            string entityName = Path.GetFileNameWithoutExtension(jsonFilePath);
            if (json.Type == JTokenType.Array)
            {
                // Entire JSON is an array
                ParseJsonArray(json as JArray, null, entityName, "$");
            }
            else if (json.Type == JTokenType.Object)
            {
                // Entire JSON is an object
                ParseJsonObject(json as JObject, null, entityName, "$");
            }
            else
            {
                Console.WriteLine("Unexpected JSON root type: " + json.Type);
            }
        }

        /// <summary>
        /// Parses a JSON object as if it's the first record in an array.
        /// </summary>
        private void ParseJsonObject(JObject jObject, EntityStructure parentStructure, string entityName, string currentPath)
        {
            // Treat the object as if it's a single record in a JArray
            ParseJsonArray(new JArray(jObject), parentStructure, entityName, currentPath);
        }

        /// <summary>
        /// Parses a JSON array to create an EntityStructure and discover fields.
        /// Expects uniform objects or at least compatible structures.
        /// </summary>
        private void ParseJsonArray(JArray jArray, EntityStructure parentStructure, string entityName, string currentPath)
        {
            var currentStructure = CreateEntityStructure(parentStructure, entityName, currentPath);

            // Clear and rebuild Entities and EntitiesNames based on the new entity
            Entities.Clear();
            EntitiesNames.Clear();
            Entities.Add(currentStructure);
            EntitiesNames.Add(entityName);

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

                // Use the first record to define initial fields
                foreach (var property in firstrecord.Properties())
                {
                    var field = CreateEntityField(property);

                    // Convert "object" Fieldtype to "string" if necessary
                    if (field.Fieldtype.Equals("object", StringComparison.InvariantCultureIgnoreCase))
                    {
                        field.Fieldtype = "string";
                    }

                    // Arrays currently not fully supported as fields
                    if (!field.Fieldtype.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                    {
                        currentStructure.Fields.Add(field);
                    }
                    else
                    {
                        Console.WriteLine("Array field not fully supported");
                    }
                }
            }

            // Ensure that all records' fields are captured
            foreach (var item in jArray)
            {
                if (item is JObject jObject)
                {
                    UpdateEntityStructureWithMissingFields(jObject, currentStructure);
                }
            }
        }

        /// <summary>
        /// Initializes _rootJsonObject by reading and parsing the file at FileName.
        /// If file does not exist or is empty, initializes an empty JArray.
        /// </summary>
        private void InitializeRootJsonObject()
        {
            if (_rootJsonObject != null) return;

            if (File.Exists(FileName) && new FileInfo(FileName).Length > 0)
            {
                jsonContent = File.ReadAllText(FileName);
                try
                {
                    var jsonToken = JToken.Parse(jsonContent);
                    if (jsonToken is JArray jArray)
                    {
                        _rootJsonObject = jArray;
                    }
                    else if (jsonToken is JObject jObject)
                    {
                        _rootJsonObject = jObject;
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

        /// <summary>
        /// Returns the root JSON array if available. If not, returns null.
        /// </summary>
        private JArray GetRootJsonArray()
        {
            if (_rootJsonObject != null && _rootJsonArray != null)
            {
                return _rootJsonArray;
            }
            return _rootJsonArray;
        }

        /// <summary>
        /// Constructs an EntityStructure with given name and path.
        /// </summary>
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

        /// <summary>
        /// Creates an EntityField from a JSON property, determining field type dynamically.
        /// </summary>
        private EntityField CreateEntityField(JProperty property)
        {
            return new EntityField
            {
               FieldName = property.Name,
                BaseColumnName = property.Name,
                Fieldtype = JsonExtensions.DetermineFieldtype(property.Value),
            };
        }

        /// <summary>
        /// Ensures that any fields present in the given JObject but not defined in the EntityStructure are added.
        /// This keeps the entity schema consistent with the data.
        /// </summary>
        public void UpdateEntityStructureWithMissingFields(JObject record, EntityStructure entityStructure)
        {
            foreach (var property in record.Properties())
            {
                bool fieldExists = entityStructure.Fields.Any(f => f.FieldName == property.Name);
                if (!fieldExists)
                {
                    var field = CreateEntityField(property);

                    if (field.Fieldtype.Equals("object", StringComparison.InvariantCultureIgnoreCase))
                    {
                        field.Fieldtype = "string";
                    }

                    if (!field.Fieldtype.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                    {
                        entityStructure.Fields.Add(field);
                    }
                }
            }
        }
        #endregion

        #region "Utility Methods"
        /// <summary>
        /// Prepares objects for operations on a specified entity if not done or if a different entity is requested.
        /// </summary>
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        #endregion
        #region "New Enhancements"

        /// <summary>
        /// Synchronizes the JSON data with the current EntityStructure.
        /// Adds missing fields and removes fields not present in the EntityStructure.
        /// </summary>
        public void SynchronizeEntityStructure()
        {
            if (_rootJsonArray == null) return;

            var currentFields = DataStruct.Fields.Select(f => f.FieldName).ToList();
            var jsonFields = _rootJsonArray.FirstOrDefault() is JObject firstRecord
                ? firstRecord.Properties().Select(p => p.Name).ToList()
                : new List<string>();

            // Add missing fields to JSON
            foreach (var field in currentFields.Except(jsonFields))
            {
                foreach (JObject record in _rootJsonArray)
                {
                    record[field] = JValue.CreateNull(); // Add missing fields
                }
            }

            // Remove extra fields from JSON
            foreach (var field in jsonFields.Except(currentFields))
            {
                foreach (JObject record in _rootJsonArray)
                {
                    record.Remove(field); // Remove fields not in EntityStructure
                }
            }

            SaveJson(FileName); // Persist changes
        }

        /// <summary>
        /// Validates that the JSON schema matches the current EntityStructure.
        /// Logs discrepancies for missing or extra fields.
        /// </summary>
        public bool ValidateSchema()
        {
            if (_rootJsonArray == null || !_rootJsonArray.Any()) return true;

            var jsonFields = _rootJsonArray.FirstOrDefault() is JObject firstRecord
                ? firstRecord.Properties().Select(p => p.Name).ToList()
                : new List<string>();

            var missingFields = DataStruct.Fields.Where(f => !jsonFields.Contains(f.FieldName)).ToList();
            var extraFields = jsonFields.Except(DataStruct.Fields.Select(f => f.FieldName)).ToList();

            if (missingFields.Any() || extraFields.Any())
            {
                Logger?.LogWarning($"Schema mismatch: Missing fields - {string.Join(", ", missingFields.Select(f => f.FieldName))}; Extra fields - {string.Join(", ", extraFields)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles schema changes dynamically, including adding and removing fields.
        /// </summary>
        public void HandleSchemaChanges()
        {
            if (!ValidateSchema())
            {
                SynchronizeEntityStructure();
                Logger?.LogInfo("Schema synchronized successfully.");
            }
        }
        /// <summary>
        /// Returns a list of child relations for a given table. Not implemented for JSON.
        /// </summary>
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // JSON datasource has no native child relation discovery implemented yet
            return Enumerable.Empty<ChildRelation>();
        }

        #endregion
    }
}
