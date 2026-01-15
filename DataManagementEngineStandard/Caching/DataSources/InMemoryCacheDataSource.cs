using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Reflection;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Caching.DataSources;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// In-Memory Cache Data Source - uses simple in-memory cache as a data storage backend.
    /// Provides fast CRUD operations on cached data with no persistence.
    /// Uses InMemory Provider (SimpleCacheProvider) for lightweight caching.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.INMEMORY, DatasourceType = DataSourceType.InMemoryCache)]
    public class InMemoryCacheDataSource : IDataSource
    {
        #region Private Fields
        private bool disposedValue;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _entityData;
        private readonly ConcurrentDictionary<string, DateTime> _entityTimestamps;
        private readonly object _schemaLock = new object();
        private ICacheProvider _cacheProvider;
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.InMemoryCache;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.INMEMORY;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        #endregion

        #region Constructor
        public InMemoryCacheDataSource()
        {
            _entityData = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
            _entityTimestamps = new ConcurrentDictionary<string, DateTime>();
            ErrorObject = new ConfigUtil.ErrorsInfo();
        }

        public InMemoryCacheDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
            : this()
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per ?? new ConfigUtil.ErrorsInfo();
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            
            // Initialize with InMemory cache provider specifically
            InitializeCacheProvider();
            InitializeConnection();
        }
        #endregion

        #region Cache Provider Initialization
        private void InitializeCacheProvider()
        {
            try
            {
                // Create a dedicated InMemory cache provider for this data source
                var config = new CacheConfiguration
                {
                    DefaultExpiry = TimeSpan.FromHours(24), // Long expiry for data source
                    MaxItems = 50000, // Higher limit for data storage
                    EnableStatistics = true,
                    KeyPrefix = $"inmemory:{DatasourceName}:"
                };

                _cacheProvider = new Providers.SimpleCacheProvider(config);
                Logger?.WriteLog($"InMemory cache provider initialized for '{DatasourceName}'.");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error initializing InMemory cache provider: {ex.Message}");
                // Create a basic fallback provider
                _cacheProvider = new Providers.SimpleCacheProvider();
            }
        }
        #endregion

        #region Connection Management
        private void InitializeConnection()
        {
            Dataconnection = new MemoryCacheConnection(DMEEditor)
            {
                Logger = Logger,
                ErrorObject = ErrorObject,
            };

            // Try to find existing connection properties or create default
            var existingConnection = DMEEditor?.ConfigEditor?.DataConnections?
                .FirstOrDefault(c => c.ConnectionName.Equals(DatasourceName, StringComparison.OrdinalIgnoreCase));
            
            if (existingConnection != null)
            {
                Dataconnection.ConnectionProp = existingConnection;
            }
            else
            {
                // Create default connection properties for in-memory cache
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = DatasourceName,
                    DatabaseType = DataSourceType.InMemoryCache,
                    Category = DatasourceCategory.INMEMORY,
                    DriverName = "InMemoryCacheDataSource",
                    DriverVersion = "1.0.0"
                };
            }
        }

        public ConnectionState Openconnection()
        {
            try
            {
                if (!_isInitialized)
                {
                    LoadExistingEntities();
                    _isInitialized = true;
                }
                
                ConnectionStatus = ConnectionState.Open;
                Logger?.WriteLog($"InMemory Cache Data Source '{DatasourceName}' opened successfully.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error opening InMemory Cache Data Source: {ex.Message}");
            }
            
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
            Logger?.WriteLog($"InMemory Cache Data Source '{DatasourceName}' closed.");
            return ConnectionStatus;
        }

        private void LoadExistingEntities()
        {
            try
            {
                // Try to load existing entity definitions from configuration
                var savedEntities = DMEEditor?.ConfigEditor?.LoadDataSourceEntitiesValues(DatasourceName);
                if (savedEntities != null && savedEntities.Entities != null)
                {
                    Entities = savedEntities.Entities;
                    EntitiesNames = Entities.Select(e => e.EntityName).ToList();
                    
                    // Initialize data containers for each entity
                    foreach (var entity in Entities)
                    {
                        if (!_entityData.ContainsKey(entity.EntityName))
                        {
                            _entityData[entity.EntityName] = new ConcurrentDictionary<string, object>();
                            _entityTimestamps[entity.EntityName] = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error loading existing entities: {ex.Message}");
            }
        }
        #endregion

        #region Entity Management
        public IEnumerable<string> GetEntitesList()
        {
            EnsureOpen();
            return EntitiesNames.ToList();
        }

        public bool CheckEntityExist(string EntityName)
        {
            if (string.IsNullOrWhiteSpace(EntityName)) return false;
            return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public int GetEntityIdx(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return -1;
            return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (string.IsNullOrWhiteSpace(EntityName)) return null;
            
            var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
            
            if (entity == null && !refresh)
            {
                // Try to auto-discover entity structure from cached data
                entity = AutoDiscoverEntityStructure(EntityName);
            }
            
            return entity;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            var entityStructure = GetEntityStructure(EntityName, false);
            if (entityStructure == null) return typeof(Dictionary<string, object>);
            
            // For now, return Dictionary type - could implement dynamic type creation later
            return typeof(Dictionary<string, object>);
        }

        private EntityStructure AutoDiscoverEntityStructure(string entityName)
        {
            if (!_entityData.TryGetValue(entityName, out var entityCache) || !entityCache.Any())
                return null;

            var entity = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                Caption = entityName,
                DatabaseType = DataSourceType.InMemoryCache,
                DataSourceID = DatasourceName,
                Fields = new List<EntityField>()
            };

            // Analyze first few items to determine field structure
            var sampleItems = entityCache.Take(10).ToList();
            var fieldMap = new Dictionary<string, EntityField>();

            foreach (var item in sampleItems)
            {
                if (item.Value is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        if (!fieldMap.ContainsKey(kvp.Key))
                        {
                            var Fieldtype = InferFieldtype(kvp.Value);
                            fieldMap[kvp.Key] = new EntityField
                            {
                               FieldName = kvp.Key,
                                Fieldtype = Fieldtype,
                                EntityName = entityName,
                                AllowDBNull = true,
                                Size1 = GetFieldSize(kvp.Value, Fieldtype)
                            };
                        }
                    }
                }
            }

            entity.Fields = fieldMap.Values.ToList();
            
            // Auto-assign first field as primary key if no explicit key exists
            if (entity.Fields.Any() && !entity.Fields.Any(f => f.IsKey))
            {
                entity.Fields[0].IsKey = true;
                entity.PrimaryKeys = new List<EntityField> { entity.Fields[0] };
            }

            return entity;
        }

        private string InferFieldtype(object value)
        {
            if (value == null) return "System.String";
            
            return value.GetType().FullName switch
            {
                var t when t == typeof(int).FullName => "System.Int32",
                var t when t == typeof(long).FullName => "System.Int64",
                var t when t == typeof(double).FullName => "System.Double",
                var t when t == typeof(decimal).FullName => "System.Decimal",
                var t when t == typeof(bool).FullName => "System.Boolean",
                var t when t == typeof(DateTime).FullName => "System.DateTime",
                var t when t == typeof(Guid).FullName => "System.Guid",
                _ => "System.String"
            };
        }

        private int GetFieldSize(object value, string Fieldtype)
        {
            if (Fieldtype == "System.String" && value != null)
            {
                return Math.Max(50, value.ToString().Length + 20); // Add some buffer
            }
            return 0;
        }
        #endregion

        #region Data Operations
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            EnsureOpen();
            
            if (string.IsNullOrWhiteSpace(EntityName) || !_entityData.TryGetValue(EntityName, out var entityCache))
            {
                return Enumerable.Empty<object>();
            }

            var results = entityCache.Values.AsEnumerable();

            // Apply filters if provided
            if (filter != null && filter.Any())
            {
                results = ApplyFilters(results, filter);
            }

            return results.ToList();
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 100;

            var allData = GetEntity(EntityName, filter).ToList();
            var totalRecords = allData.Count;
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            
            var skip = (pageNumber - 1) * pageSize;
            var pageData = allData.Skip(skip).Take(pageSize).ToList();

            return new PagedResult 
            { 
                Data = pageData,
                // Set additional properties if PagedResult supports them
            };
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // For cache data source, treat query string as entity name
            if (string.IsNullOrWhiteSpace(qrystr)) return Enumerable.Empty<object>();
            
            // Simple query parsing - look for entity name
            var entityName = qrystr.Trim();
            return GetEntity(entityName, null);
        }

        private IEnumerable<object> ApplyFilters(IEnumerable<object> data, List<AppFilter> filters)
        {
            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName)) continue;
                
                data = data.Where(item => EvaluateFilter(item, filter));
            }
            return data;
        }

        private bool EvaluateFilter(object item, AppFilter filter)
        {
            if (!(item is Dictionary<string, object> dict) || !dict.TryGetValue(filter.FieldName, out var fieldValue))
                return false;

            var filterValue = filter.FilterValue;
            var op = (filter.Operator ?? "=").ToLowerInvariant();

            if (fieldValue == null)
            {
                return op == "isnull" || (op == "=" && string.IsNullOrEmpty(filterValue));
            }

            var fieldStr = fieldValue.ToString();
            
            return op switch
            {
                "=" or "equals" => string.Equals(fieldStr, filterValue, StringComparison.OrdinalIgnoreCase),
                "!=" or "<>" => !string.Equals(fieldStr, filterValue, StringComparison.OrdinalIgnoreCase),
                "contains" => fieldStr.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                "startswith" => fieldStr.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                "endswith" => fieldStr.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                ">" => CompareValues(fieldValue, filterValue) > 0,
                ">=" => CompareValues(fieldValue, filterValue) >= 0,
                "<" => CompareValues(fieldValue, filterValue) < 0,
                "<=" => CompareValues(fieldValue, filterValue) <= 0,
                _ => false
            };
        }

        private int CompareValues(object fieldValue, string filterValue)
        {
            try
            {
                if (fieldValue is IComparable comparable)
                {
                    var convertedFilter = Convert.ChangeType(filterValue, fieldValue.GetType());
                    return comparable.CompareTo(convertedFilter);
                }
            }
            catch { }
            
            return string.Compare(fieldValue?.ToString(), filterValue, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region CRUD Operations
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            EnsureOpen();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (string.IsNullOrWhiteSpace(EntityName) || InsertedData == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Entity name or data is null.";
                    return ErrorObject;
                }

                // Ensure entity data container exists
                if (!_entityData.ContainsKey(EntityName))
                {
                    _entityData[EntityName] = new ConcurrentDictionary<string, object>();
                    _entityTimestamps[EntityName] = DateTime.UtcNow;
                    
                    // Add to entities list if not present
                    if (!EntitiesNames.Contains(EntityName))
                    {
                        EntitiesNames.Add(EntityName);
                        
                        // Auto-create entity structure
                        var autoEntity = CreateEntityStructureFromData(EntityName, InsertedData);
                        if (autoEntity != null)
                        {
                            Entities.Add(autoEntity);
                        }
                    }
                }

                var entityCache = _entityData[EntityName];
                
                // Generate key for the item
                var key = GenerateEntityKey(EntityName, InsertedData);
                
                // Convert to dictionary format for storage
                var dataDict = ConvertToDataDictionary(InsertedData);
                
                // Store in local memory cache
                entityCache[key] = dataDict;
                
                // Also store in dedicated cache provider
                if (_cacheProvider != null)
                {
                    var cacheKey = $"{EntityName}:{key}";
                    _ = _cacheProvider.SetAsync(cacheKey, dataDict, TimeSpan.FromDays(1));
                }

                _entityTimestamps[EntityName] = DateTime.UtcNow;
                Logger?.WriteLog($"Entity '{EntityName}' record inserted with key '{key}' in InMemory cache.");
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error inserting entity '{EntityName}': {ex.Message}");
            }

            return ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            EnsureOpen();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (!_entityData.TryGetValue(EntityName, out var entityCache))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity '{EntityName}' not found.";
                    return ErrorObject;
                }

                var key = GenerateEntityKey(EntityName, UploadDataRow);
                
                if (entityCache.ContainsKey(key))
                {
                    var dataDict = ConvertToDataDictionary(UploadDataRow);
                    entityCache[key] = dataDict;
                    
                    // Update in cache provider
                    if (_cacheProvider != null)
                    {
                        var cacheKey = $"{EntityName}:{key}";
                        _ = _cacheProvider.SetAsync(cacheKey, dataDict, TimeSpan.FromDays(1));
                    }
                    
                    _entityTimestamps[EntityName] = DateTime.UtcNow;
                    Logger?.WriteLog($"Entity '{EntityName}' record updated with key '{key}' in InMemory cache.");
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Record with key '{key}' not found in entity '{EntityName}'.";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error updating entity '{EntityName}': {ex.Message}");
            }

            return ErrorObject;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            EnsureOpen();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (!_entityData.TryGetValue(EntityName, out var entityCache))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity '{EntityName}' not found.";
                    return ErrorObject;
                }

                var key = GenerateEntityKey(EntityName, UploadDataRow);
                
                if (entityCache.TryRemove(key, out _))
                {
                    // Remove from cache provider
                    if (_cacheProvider != null)
                    {
                        var cacheKey = $"{EntityName}:{key}";
                        _ = _cacheProvider.RemoveAsync(cacheKey);
                    }
                    
                    _entityTimestamps[EntityName] = DateTime.UtcNow;
                    Logger?.WriteLog($"Entity '{EntityName}' record deleted with key '{key}' from InMemory cache.");
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Record with key '{key}' not found in entity '{EntityName}'.";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error deleting entity '{EntityName}': {ex.Message}");
            }

            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                foreach (var entity in entities ?? Enumerable.Empty<EntityStructure>())
                {
                    if (CreateEntityAs(entity))
                    {
                        Logger?.WriteLog($"Entity '{entity.EntityName}' created successfully.");
                    }
                }
                
                // Save entity structures
                SaveEntityStructures();
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.WriteLog($"Error creating entities: {ex.Message}");
            }

            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            if (entity == null || CheckEntityExist(entity.EntityName))
                return false;

            try
            {
                lock (_schemaLock)
                {
                    Entities.Add(entity);
                    EntitiesNames.Add(entity.EntityName);
                    
                    // Initialize data container
                    _entityData[entity.EntityName] = new ConcurrentDictionary<string, object>();
                    _entityTimestamps[entity.EntityName] = DateTime.UtcNow;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error creating entity '{entity.EntityName}': {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Helper Methods
        private void EnsureOpen()
        {
            if (ConnectionStatus != ConnectionState.Open)
            {
                Openconnection();
            }
        }

        private string GenerateEntityKey(string entityName, object data)
        {
            var entityStructure = GetEntityStructure(entityName, false);
            
            // Try to use primary key if defined
            if (entityStructure?.PrimaryKeys?.Any() == true)
            {
                var pkField = entityStructure.PrimaryKeys.First();
                var pkValue = GetFieldValue(data, pkField.FieldName);
                if (pkValue != null)
                {
                    return pkValue.ToString();
                }
            }
            
            // Fallback to hash-based key or GUID
            if (data is Dictionary<string, object> dict && dict.ContainsKey("Id"))
            {
                return dict["Id"]?.ToString() ?? Guid.NewGuid().ToString();
            }
            
            return Guid.NewGuid().ToString();
        }

        private object GetFieldValue(object data, string FieldName)
        {
            if (data is Dictionary<string, object> dict)
            {
                return dict.TryGetValue(FieldName, out var value) ? value : null;
            }
            
            // Use reflection for object properties
            var property = data.GetType().GetProperty(FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return property?.GetValue(data);
        }

        private Dictionary<string, object> ConvertToDataDictionary(object data)
        {
            if (data is Dictionary<string, object> dict)
                return new Dictionary<string, object>(dict);
            
            var result = new Dictionary<string, object>();
            var properties = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var prop in properties)
            {
                result[prop.Name] = prop.GetValue(data);
            }
            
            return result;
        }

        private EntityStructure CreateEntityStructureFromData(string entityName, object data)
        {
            var entity = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                Caption = entityName,
                DatabaseType = DataSourceType.InMemoryCache,
                DataSourceID = DatasourceName,
                Fields = new List<EntityField>()
            };

            var dataDict = ConvertToDataDictionary(data);
            var fieldIndex = 0;
            
            foreach (var kvp in dataDict)
            {
                var field = new EntityField
                {
                   FieldName = kvp.Key,
                    Fieldtype = InferFieldtype(kvp.Value),
                    EntityName = entityName,
                    FieldIndex = fieldIndex++,
                    AllowDBNull = true,
                    IsKey = kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase),
                    Size1 = GetFieldSize(kvp.Value, InferFieldtype(kvp.Value))
                };
                
                entity.Fields.Add(field);
            }

            // Set primary key
            var pkField = entity.Fields.FirstOrDefault(f => f.IsKey);
            if (pkField != null)
            {
                entity.PrimaryKeys = new List<EntityField> { pkField };
            }

            return entity;
        }

        private void SaveEntityStructures()
        {
            try
            {
                DMEEditor?.ConfigEditor?.SaveDataSourceEntitiesValues(new DatasourceEntities
                {
                    datasourcename = DatasourceName,
                    Entities = Entities
                });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error saving entity structures: {ex.Message}");
            }
        }
        #endregion

        #region Not Implemented / Unsupported Operations
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Transactions not supported in InMemory Cache Data Source.";
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            return BeginTransaction(args);
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            return BeginTransaction(args);
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported in InMemory Cache Data Source.";
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return Enumerable.Empty<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            return Enumerable.Empty<ETLScriptDet>();
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            var entity = GetEntityStructure(entityname, false);
            return entity?.Relations ?? Enumerable.Empty<RelationShipKeys>();
        }

        public double GetScalar(string query)
        {
            return 0.0;
        }

        public Task<double> GetScalarAsync(string query)
        {
            return Task.FromResult(0.0);
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Script execution not supported in InMemory Cache Data Source.";
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Bulk update not supported in InMemory Cache Data Source.";
            return ErrorObject;
        }
        #endregion

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SaveEntityStructures();
                    _entityData?.Clear();
                    _entityTimestamps?.Clear();
                    _cacheProvider?.Dispose();
                    _cacheProvider = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #region Helper Classes
    
    #endregion
}
