using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Json
{
    /// <summary>
    /// Multi-file JSON data source that treats each .json file in a directory as a separate entity.
    /// Like tables in an RDBMS, each file is an independent entity with its own schema, data, and CRUD operations.
    ///
    /// Directory structure:
    ///   /datasource/
    ///     customers.json    → entity "customers"
    ///     orders.json       → entity "orders"
    ///     products.json     → entity "products"
    ///
    /// Each file can contain either a JSON array of records or a single JSON object.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.FILES, DatasourceType = DataSourceType.JsonMultiFile)]
    public class JsonMultiFileDataSource : IDataSource, IDisposable
    {
        #region "Constructor"

        public JsonMultiFileDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = DataSourceType.Json;
            Category = DatasourceCategory.FILE;
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };

            var conn = DMEEditor.ConfigEditor.DataConnections
                .FirstOrDefault(c => c.ConnectionName.Equals(datasourcename, StringComparison.InvariantCultureIgnoreCase));
            if (conn != null)
            {
                Dataconnection.ConnectionProp = conn;
                // Support both directory-only and file+directory path patterns
                if (!string.IsNullOrEmpty(conn.FileName))
                    DataDirectory = Path.Combine(conn.FilePath ?? "", conn.FileName);
                else
                    DataDirectory = conn.FilePath ?? "";
            }
        }

        #endregion

        #region "Properties"

        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Json;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public event EventHandler<PassedArgs> PassEvent;

        /// <summary>Directory path containing the .json entity files.</summary>
        public string DataDirectory { get; private set; }

        // Per-entity caches: loaded JSON data keyed by entity name (lowercase)
        private readonly Dictionary<string, JArray> _entityData = new(StringComparer.OrdinalIgnoreCase);
        // Per-entity file paths for custom files not following the standard naming convention
        private readonly Dictionary<string, string> _entityFilePaths = new(StringComparer.OrdinalIgnoreCase);
        // Tracks which entities have been modified since last save
        private readonly HashSet<string> _dirtyEntities = new(StringComparer.OrdinalIgnoreCase);
        // Thread safety for entity data access
        private readonly object _lock = new();

        // Legacy single-file fields for compatibility
        private bool _objectsCreated;
        private string _lastEntityName;
        public bool ObjectsCreated => _objectsCreated;
        public EntityStructure? DataStruct { get; private set; }

        private bool _disposedValue;

        #endregion

        #region "Connection Management"

        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(DataDirectory) || !Directory.Exists(DataDirectory))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Data directory not found: {DataDirectory}";
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }

                // Discover entities from .json files
                DiscoverEntities();

                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to open connection: {ex.Message}";
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            SaveAllDirtyEntities();
            _entityData.Clear();
            _dirtyEntities.Clear();
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        #endregion

        #region "Entity Discovery & Schema"

        /// <summary>
        /// Scans the data directory for .json files and builds entity structures from them.
        /// </summary>
        private void DiscoverEntities()
        {
            Entities.Clear();
            EntitiesNames.Clear();
            _entityData.Clear();

            var jsonFiles = Directory.GetFiles(DataDirectory, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var filePath in jsonFiles)
            {
                var entityName = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrEmpty(entityName)) continue;

                var entity = LoadEntityFromFile(entityName, filePath);
                if (entity != null)
                {
                    Entities.Add(entity);
                    EntitiesNames.Add(entityName);
                }
            }
        }

        /// <summary>
        /// Loads a single JSON file as an entity, parsing its schema from the data.
        /// </summary>
        private EntityStructure? LoadEntityFromFile(string entityName, string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var jsonText = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    _entityData[entityName] = new JArray();
                }
                else
                {
                    var token = JToken.Parse(jsonText);
                    _entityData[entityName] = token switch
                    {
                        JArray arr => arr,
                        JObject obj => new JArray(obj),
                        _ => new JArray()
                    };
                }

                var entity = new EntityStructure
                {
                    EntityName = entityName,
                    DatasourceEntityName = entityName,
                    OriginalEntityName = entityName,
                    EntityPath = filePath,
                    Viewtype = ViewType.Table,
                    Fields = new List<EntityField>(),
                    PrimaryKeys = new List<EntityField>()
                };

                // Infer schema from first record
                var data = _entityData[entityName];
                if (data.Count > 0 && data[0] is JObject firstRecord)
                {
                    foreach (var prop in firstRecord.Properties())
                    {
                        var field = new EntityField
                        {
                            FieldName = prop.Name,
                            BaseColumnName = prop.Name,
                            Fieldtype = JsonExtensions.DetermineFieldtype(prop.Value)
                        };
                        entity.Fields.Add(field);
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Warning", $"Failed to load entity '{entityName}': {ex.Message}",
                    DateTime.Now, 0, null, Errors.Warning);
                return null;
            }
        }

        /// <summary>
        /// Reloads a specific entity from its backing file, discarding in-memory changes.
        /// </summary>
        public void RefreshEntity(string entityName)
        {
            lock (_lock)
            {
                var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                var filePath = entity?.EntityPath ?? GetEntityFilePath(entityName);
                if (File.Exists(filePath))
                {
                    LoadEntityFromFile(entityName, filePath);
                    _dirtyEntities.Remove(entityName);
                }
            }
        }

        /// <summary>
        /// Manually registers a specific JSON file as an entity, overriding the standard naming convention.
        /// </summary>
        public EntityStructure? AddEntityFile(string entityName, string filePath)
        {
            if (!File.Exists(filePath)) return null;

            lock (_lock)
            {
                _entityFilePaths[entityName] = filePath;
                var entity = LoadEntityFromFile(entityName, filePath);
                if (entity != null)
                {
                    entity.EntityPath = filePath;
                    var existing = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                        Entities.Remove(existing);
                    Entities.Add(entity);
                    if (!EntitiesNames.Contains(entityName, StringComparer.OrdinalIgnoreCase))
                        EntitiesNames.Add(entityName);
                }
                return entity;
            }
        }

        /// <summary>
        /// Gets the file path for an entity, checking custom paths first, then the standard convention.
        /// </summary>
        private string GetEntityFilePath(string entityName)
        {
            if (_entityFilePaths.TryGetValue(entityName, out var customPath) && File.Exists(customPath))
                return customPath;
            return Path.Combine(DataDirectory, entityName + ".json");
        }

        /// <summary>
        /// Reloads a specific entity from its backing file (internal use).
        /// </summary>
        private void ReloadEntity(string entityName)
        {
            var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            var filePath = entity?.EntityPath ?? GetEntityFilePath(entityName);
            LoadEntityFromFile(entityName, filePath);
            _dirtyEntities.Remove(entityName);
        }

        /// <summary>
        /// Ensures any new fields discovered in JSON records are added to the entity schema.
        /// </summary>
        private void UpdateEntityStructureFromRecord(JObject record, EntityStructure entity)
        {
            foreach (var prop in record.Properties())
            {
                if (!entity.Fields.Any(f => f.FieldName == prop.Name))
                {
                    entity.Fields.Add(new EntityField
                    {
                        FieldName = prop.Name,
                        BaseColumnName = prop.Name,
                        Fieldtype = JsonExtensions.DetermineFieldtype(prop.Value)
                    });
                }
            }
        }

        #endregion

        #region "Entity Management"

        public IEnumerable<string> GetEntitesList()
        {
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            return Entities.Any(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public int GetEntityIdx(string entityName)
        {
            return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (refresh)
                ReloadEntity(EntityName);
            return Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        /// <summary>
        /// The runtime row type for an entity — an <c>Entity</c>-derived class
        /// generated from that entity's fields, one class per entity.
        /// </summary>
        /// <remarks>
        /// Delegates to the shared
        /// <see cref="TheTechIdea.Beep.Tools.EntityTypeFactory"/> rather than
        /// building a type here. This previously used <c>DMTypeBuilder</c>,
        /// which emits types deriving from <c>object</c>, while
        /// <c>UnitofWork&lt;T&gt;</c> is constrained to <c>T : Entity</c> — so a
        /// block over this datasource registered and then held no records:
        /// nothing to query, navigate or synchronise.
        /// </remarks>
        public Type GetEntityType(string EntityName)
        {
            var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
            if (entity?.Fields == null || !entity.Fields.Any())
                return typeof(ExpandoObject);

            return TheTechIdea.Beep.Tools.EntityTypeFactory.GetOrCreate(DMEEditor, entity)
                   ?? typeof(ExpandoObject);
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            foreach (var entity in entities)
            {
                if (!CreateEntityAs(entity))
                    return new ErrorsInfo { Message = $"Failed to create entity '{entity.EntityName}'", Flag = Errors.Failed };
            }
            return new ErrorsInfo { Message = "Entities created successfully.", Flag = Errors.Ok };
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            if (string.IsNullOrEmpty(entity?.EntityName)) return false;
            if (Entities.Any(e => e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)))
                return false;

            var filePath = Path.Combine(DataDirectory, entity.EntityName + ".json");
            entity.EntityPath = filePath;
            entity.DatasourceEntityName = entity.EntityName;

            // Initialize with empty array
            _entityData[entity.EntityName] = new JArray();
            Entities.Add(entity);
            EntitiesNames.Add(entity.EntityName);

            // Persist empty file
            SaveEntityToFile(entity.EntityName);
            return true;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return Enumerable.Empty<ChildRelation>();
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            return Enumerable.Empty<RelationShipKeys>();
        }

        #endregion

        #region "Data Retrieval"

        public IEnumerable<object> GetEntity(string entityName, List<AppFilter>? filter)
        {
            if (!TryGetEntityData(entityName, out var data))
                return Enumerable.Empty<object>();

            var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            var activeFilters = NormalizeFilters(filter);

            var results = new List<object>();
            foreach (var item in data.OfType<JObject>())
            {
                if (!MatchesFilters(item, activeFilters)) continue;
                if (entity != null) UpdateEntityStructureFromRecord(item, entity);
                results.Add(JObjectToDictionary(item));
            }
            return results;
        }

        public PagedResult GetEntity(string entityName, List<AppFilter>? filter, int pageNumber, int pageSize)
        {
            if (!TryGetEntityData(entityName, out var data))
                return new PagedResult();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = int.MaxValue;

            var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            var activeFilters = NormalizeFilters(filter);

            var paged = new List<object>();
            int skip = (pageNumber - 1) * pageSize;
            int total = 0;

            foreach (var item in data.OfType<JObject>())
            {
                if (!MatchesFilters(item, activeFilters)) continue;

                if (total >= skip && paged.Count < pageSize)
                {
                    if (entity != null) UpdateEntityStructureFromRecord(item, entity);
                    dynamic record = new ExpandoObject();
                    var dict = (IDictionary<string, object>)record;
                    foreach (var prop in item.Properties())
                        dict[prop.Name] = ConvertTokenValue(prop.Value);
                    paged.Add(record);
                }
                total++;
            }

            return new PagedResult(paged, pageNumber, pageSize, total);
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            if (string.IsNullOrWhiteSpace(qrystr)) return Enumerable.Empty<object>();

            // Support simple "SELECT * FROM entityName" syntax
            var entityName = JsonExtensions.ExtractEntityNameFromQuery(qrystr);
            if (!string.IsNullOrEmpty(entityName) && CheckEntityExist(entityName))
            {
                return GetEntity(entityName, null);
            }

            // Fallback: try JSONPath across all entities
            var allResults = new List<object>();
            foreach (var entityName2 in EntitiesNames)
            {
                if (TryGetEntityData(entityName2, out var data))
                {
                    try
                    {
                        var tokens = data.SelectTokens(qrystr, errorWhenNoMatch: false);
                        foreach (var t in tokens)
                            allResults.Add(t.Type == JTokenType.Object ? JObjectToDictionary((JObject)t) : t.ToObject<object>());
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("JsonMultiFileDataSource", $"RunQuery: JSONPath evaluation failed for entity '{entityName2}': {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
                    }
                }
            }
            return allResults;
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region "Data Modification — CRUD"

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                if (!TryGetEntityData(EntityName, out var data))
                    return ErrorResult($"Entity '{EntityName}' not found.");

                JToken token = InsertedData switch
                {
                    JObject jo => jo,
                    JToken jt => jt,
                    _ => JToken.FromObject(InsertedData)
                };
                data.Add(token);
                MarkDirty(EntityName);
                return OkResult("Record inserted.");
            }
            catch (Exception ex)
            {
                return ErrorResult($"Insert failed: {ex.Message}");
            }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (!TryGetEntityData(EntityName, out var data))
                    return ErrorResult($"Entity '{EntityName}' not found.");

                var updates = UploadDataRow switch
                {
                    JObject jo => jo,
                    Dictionary<string, object> dict => JObject.FromObject(dict),
                    _ => JObject.FromObject(UploadDataRow)
                };

                // Match by first field that acts as an identifier (_id, id, or first key field)
                var idProp = updates.Properties().FirstOrDefault(p =>
                    p.Name.Equals("_id", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

                JObject? target = null;
                if (idProp != null)
                {
                    // Read the row's id case-insensitively. `item["id"]` is an
                    // ordinal, case-SENSITIVE lookup, so a file written with
                    // "Id" — the usual .NET casing, and what every entity
                    // generated from a C# property produces — never matched, and
                    // the identified-row branch silently found nothing.
                    var idValue = idProp.Value.ToString();
                    target = data.OfType<JObject>().FirstOrDefault(item =>
                    {
                        var itemId = PropertyValue(item, "_id")?["$oid"]?.ToString() ??
                                     PropertyValue(item, "_id")?.ToString() ??
                                     PropertyValue(item, "id")?.ToString();
                        return string.Equals(itemId, idValue, StringComparison.OrdinalIgnoreCase);
                    });

                    // An update that carries an identifier addresses exactly one
                    // row. If that row is absent, the update fails — it must not
                    // fall through to matching on a data value.
                    //
                    // It did until 2026-08-01, and combined with the
                    // case-sensitivity above the result was silent corruption
                    // rather than a failed write: updating {Id:1, Qty:50} found
                    // no id match, fell back to "first non-id field", located a
                    // *different* row that happened to have Qty 50, and copied
                    // every property onto it — including Id, so that row's
                    // identity was overwritten and two rows ended up sharing an
                    // Id. The intended row was never touched.
                    if (target == null)
                        return ErrorResult(
                            $"No record with id '{idValue}' found in '{EntityName}' to update.");
                }
                else
                {
                    // No identifier in the payload at all — fall back to matching
                    // on the first field, which is the best this source can do.
                    var firstField = updates.Properties().FirstOrDefault();
                    if (firstField == null)
                        return ErrorResult("No identifiable fields to match for update.");

                    var matchValue = firstField.Value.ToString();
                    target = data.OfType<JObject>().FirstOrDefault(item =>
                        string.Equals(PropertyValue(item, firstField.Name)?.ToString(), matchValue,
                            StringComparison.OrdinalIgnoreCase));
                }

                if (target == null)
                    return ErrorResult("No matching record found to update.");

                // Assign onto the row's existing property name where one matches
                // case-insensitively, so updating "Id" does not leave the row
                // carrying both "Id" and "id".
                foreach (var prop in updates.Properties())
                {
                    var existing = target.Properties().FirstOrDefault(p =>
                        string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase));
                    target[existing?.Name ?? prop.Name] = prop.Value;
                }

                MarkDirty(EntityName);
                return OkResult("Record updated.");
            }
            catch (Exception ex)
            {
                return ErrorResult($"Update failed: {ex.Message}");
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object criteria)
        {
            try
            {
                if (!TryGetEntityData(EntityName, out var data))
                    return ErrorResult($"Entity '{EntityName}' not found.");

                JObject? target = null;
                if (criteria is JObject criteriaObj)
                {
                    var idProp = criteriaObj.Properties().FirstOrDefault();
                    if (idProp != null)
                    {
                        var idValue = idProp.Value.ToString();
                        target = data.OfType<JObject>().FirstOrDefault(item =>
                            string.Equals(item[idProp.Name]?.ToString(), idValue, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (criteria is Dictionary<string, object> criteriaDict && criteriaDict.Any())
                {
                    var kv = criteriaDict.First();
                    var idValue = kv.Value?.ToString();
                    target = data.OfType<JObject>().FirstOrDefault(item =>
                        string.Equals(item[kv.Key]?.ToString(), idValue, StringComparison.OrdinalIgnoreCase));
                }
                else if (criteria is int index && index >= 0 && index < data.Count)
                {
                    data.RemoveAt(index);
                    MarkDirty(EntityName);
                    return OkResult("Record deleted by index.");
                }

                if (target != null)
                {
                    target.Remove();
                    MarkDirty(EntityName);
                    return OkResult("Record deleted.");
                }

                return ErrorResult("No matching record found to delete.");
            }
            catch (Exception ex)
            {
                return ErrorResult($"Delete failed: {ex.Message}");
            }
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs>? progress)
        {
            try
            {
                if (!TryGetEntityData(EntityName, out var data))
                    return ErrorResult($"Entity '{EntityName}' not found.");

                JArray newData = UploadData switch
                {
                    JArray arr => arr,
                    IEnumerable<object> list => new JArray(list),
                    _ => new JArray(UploadData)
                };

                data.ReplaceAll(newData);
                MarkDirty(EntityName);
                progress?.Report(new PassedArgs { Messege = "Bulk update completed", ParameterInt1 = 100 });
                return OkResult($"Entity '{EntityName}' replaced with {newData.Count} records.");
            }
            catch (Exception ex)
            {
                return ErrorResult($"Bulk update failed: {ex.Message}");
            }
        }

        #endregion

        #region "File Persistence"

        /// <summary>
        /// Reads a property off a row by name, ignoring case. JSON keys are
        /// case-sensitive and <c>JObject</c>'s indexer follows suit, but the
        /// casing in a file is whatever wrote it — so a lookup that hard-codes
        /// one spelling silently finds nothing.
        /// </summary>
        private static JToken? PropertyValue(JObject row, string name) =>
            row?.Properties()
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                ?.Value;

        /// <summary>
        /// Saves a single entity's data to its backing .json file.
        /// </summary>
        public void SaveEntityToFile(string entityName)
        {
            JArray? data;
            string? filePath;
            lock (_lock)
            {
                if (!_entityData.TryGetValue(entityName, out data)) return;
                var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                filePath = entity?.EntityPath ?? GetEntityFilePath(entityName);
            }

            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(filePath!, data!.ToString(Formatting.Indented));
                lock (_lock) { _dirtyEntities.Remove(entityName); }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Error", $"Failed to save '{entityName}' to {filePath}: {ex.Message}",
                    DateTime.Now, 0, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Saves all entities that have been modified.
        /// </summary>
        public void SaveAllDirtyEntities()
        {
            List<string> dirty;
            lock (_lock) { dirty = _dirtyEntities.ToList(); }
            foreach (var entityName in dirty)
                SaveEntityToFile(entityName);
        }

        /// <summary>
        /// Records that an entity's in-memory data has changed and writes it
        /// straight through to its backing file.
        /// </summary>
        /// <remarks>
        /// This only set the dirty flag until 2026-08-01, and the sole flush was
        /// in <c>Dispose</c> — <see cref="SaveAllDirtyEntities"/> had no caller
        /// anywhere in the solution. Insert/Update/Delete therefore reported
        /// success while leaving the file untouched, so a unit-of-work commit
        /// returned Ok and the edit existed only in memory until the process
        /// happened to dispose the datasource cleanly; a crash, or any host that
        /// simply never disposed it, dropped the write with nothing logged.
        /// <para>
        /// Write-through is what "no transactions" means for a file source:
        /// there is no later moment at which a batch would be committed, so
        /// there is nothing to defer to. <c>Dispose</c> keeps its flush as a
        /// backstop for anything that marks dirty without going through here.
        /// </para>
        /// </remarks>
        private void MarkDirty(string entityName)
        {
            lock (_lock) { _dirtyEntities.Add(entityName); }
            SaveEntityToFile(entityName);
        }

        #endregion

        #region "Transaction & Script (Not Implemented)"

        public IErrorsInfo BeginTransaction(PassedArgs args) =>
            ErrorResult("Transactions not supported for JSON file data sources.");

        public IErrorsInfo Commit(PassedArgs args) =>
            ErrorResult("Transactions not supported for JSON file data sources.");

        public IErrorsInfo EndTransaction(PassedArgs args) =>
            ErrorResult("Transactions not supported for JSON file data sources.");

        public IErrorsInfo ExecuteSql(string sql) =>
            ErrorResult("SQL execution not supported for JSON data sources.");

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts) =>
            ErrorResult("Script execution not supported for JSON data sources.");

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure>? entities = null)
        {
            return Enumerable.Empty<ETLScriptDet>();
        }

        #endregion

        #region "Helpers"

        private bool TryGetEntityData(string entityName, out JArray data)
        {
            data = null!;
            if (string.IsNullOrEmpty(entityName)) return false;

            lock (_lock)
            {
                if (_entityData.TryGetValue(entityName, out var cached))
                {
                    data = cached;
                    return true;
                }
            }

            // Try lazy-load from file
            var filePath = GetEntityFilePath(entityName);
            if (File.Exists(filePath))
            {
                var loaded = LoadEntityFromFile(entityName, filePath);
                if (loaded != null)
                {
                    lock (_lock)
                    {
                        var existing = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            Entities.Add(loaded);
                            EntitiesNames.Add(entityName);
                        }
                        return _entityData.TryGetValue(entityName, out data);
                    }
                }
            }
            return false;
        }

        private static List<AppFilter> NormalizeFilters(List<AppFilter>? filter) =>
            (filter ?? new List<AppFilter>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrEmpty(f.FilterValue))
                .ToList();

        /// <summary>
        /// Applies a filter set to one row.
        /// </summary>
        /// <remarks>
        /// Every filter was compared with string equality until 2026-08-01 —
        /// <c>AppFilter.Operator</c> was never read — so a query-by-example for
        /// <c>Qty &gt; 100</c> silently behaved as <c>Qty = 100</c> and returned
        /// the wrong rows with no error anywhere. Comparisons are numeric when
        /// both sides parse as numbers, then by date, then ordinal-insensitive
        /// string, so "9" &lt; "100" orders as a number rather than as text.
        /// </remarks>
        private static bool MatchesFilters(JObject item, List<AppFilter> filters)
        {
            foreach (var f in filters)
            {
                var token = item.Property(f.FieldName, StringComparison.OrdinalIgnoreCase)?.Value;

                var op = (f.Operator ?? "=").Trim().ToLowerInvariant();

                // Null tests are the only ones a missing/null field can satisfy.
                var isNull = token == null || token.Type == JTokenType.Null;
                if (op is "is null") { if (!isNull) return false; continue; }
                if (op is "is not null") { if (isNull) return false; continue; }
                if (isNull) return false;

                var tokenStr = token.Type == JTokenType.Object && token["$oid"] != null
                    ? token["$oid"].ToString()
                    : token.ToString();

                if (!MatchesOperator(tokenStr, op, f.FilterValue, f.FilterValue1))
                    return false;
            }
            return true;
        }

        private static bool MatchesOperator(string value, string op, string operand, string operand2)
        {
            switch (op)
            {
                case "=":
                case "==":
                    return Compare(value, operand) == 0;
                case "<>":
                case "!=":
                    return Compare(value, operand) != 0;
                case ">":
                    return Compare(value, operand) > 0;
                case ">=":
                    return Compare(value, operand) >= 0;
                case "<":
                    return Compare(value, operand) < 0;
                case "<=":
                    return Compare(value, operand) <= 0;

                case "like":
                    return LikeMatches(value, operand);
                case "not like":
                    return !LikeMatches(value, operand);

                case "in":
                case "not in":
                {
                    var members = (operand ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(m => m.Trim('\'', '"'));
                    var hit = members.Any(m => Compare(value, m) == 0);
                    return op == "in" ? hit : !hit;
                }

                case "between":
                    // Oracle Forms' BETWEEN is inclusive at both ends.
                    return Compare(value, operand) >= 0 && Compare(value, operand2) <= 0;

                default:
                    // An operator this source does not implement must not be
                    // silently treated as equality — that is the defect this
                    // method used to have. Reject the row so the mismatch is
                    // visible rather than plausible.
                    return false;
            }
        }

        /// <summary>
        /// Numeric where both sides are numbers, then date, then ordinal
        /// case-insensitive string.
        /// </summary>
        private static int Compare(string left, string right)
        {
            right ??= string.Empty;

            if (decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var ln) &&
                decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rn))
                return ln.CompareTo(rn);

            if (DateTime.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ld) &&
                DateTime.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rd))
                return ld.CompareTo(rd);

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>SQL LIKE with % and _ wildcards.</summary>
        private static bool LikeMatches(string value, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return string.IsNullOrEmpty(value);

            var regex = "^" + Regex.Escape(pattern.Trim('\'', '"'))
                .Replace("%", ".*")
                .Replace("_", ".") + "$";
            return Regex.IsMatch(value ?? string.Empty, regex, RegexOptions.IgnoreCase);
        }

        private static Dictionary<string, object> JObjectToDictionary(JObject obj)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in obj.Properties())
                dict[prop.Name] = ConvertTokenValue(prop.Value);
            return dict;
        }

        private static object? ConvertTokenValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Null => null,
                JTokenType.Object when token["$oid"] != null => token["$oid"]!.ToString(),
                JTokenType.Object => token.ToObject<object>(),
                JTokenType.Array => token.ToObject<List<object>>(),
                _ => ((JValue)token).Value
            };
        }

        private IErrorsInfo OkResult(string message)
        {
            return new ErrorsInfo { Message = message, Flag = Errors.Ok };
        }

        private IErrorsInfo ErrorResult(string message)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = message;
            return ErrorObject;
        }

        #endregion

        #region "Dispose"

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    SaveAllDirtyEntities();
                    _entityData.Clear();
                    _dirtyEntities.Clear();
                    Entities?.Clear();
                    EntitiesNames?.Clear();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
