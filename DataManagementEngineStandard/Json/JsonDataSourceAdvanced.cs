using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Json.Helpers;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Json
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.Json)]
    public class JsonDataSourceAdvanced : IDataSource, IDisposable
    {
        #region Private Fields
        private bool disposedValue;
        private string _filePath;
        private JToken _rootJson;            // original root (object or array)
        private JArray _rootArray;            // normalized array root
        private JsonDataHelper _dataHelper;
        private JsonAsyncDataHelper _asyncHelper;
        private JsonCrudHelper _crudHelper;
        private JsonGraphHelper _graphHelper;
        private JsonSchemaPersistenceHelper _schemaPersistence = new();
        private readonly object _lock = new();
        private bool _dataDirty;
        private bool _rootWasObject;
        #endregion

        #region Ctor
        public JsonDataSourceAdvanced(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
            if (Dataconnection.ConnectionProp != null)
            {
                _filePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }
        }
        #endregion

        #region IDataSource Properties
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Json;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        #endregion

        #region Connection Handling
        public ConnectionState Openconnection()
        {
            try
            {
                LoadJson();
                BuildSchema();
                InitializeHelpers();
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ConnectionStatus = ConnectionState.Broken;
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            PersistDataIfDirty();
            PersistSchemaIfDirty();
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void EnsureOpen()
        {
            if (ConnectionStatus != ConnectionState.Open)
            {
                Openconnection();
            }
        }
        #endregion

        #region JSON / Schema
        private void LoadJson()
        {
            if (string.IsNullOrWhiteSpace(_filePath))
                return;
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json)) json = "[]";
            try
            {
                _rootJson = JToken.Parse(json);
            }
            catch
            {
                _rootJson = new JArray();
            }

            _rootWasObject = _rootJson.Type == JTokenType.Object;

            if (_rootJson.Type == JTokenType.Array)
            {
                _rootArray = (JArray)_rootJson;
            }
            else if (_rootJson.Type == JTokenType.Object)
            {
                // Normalize in-memory representation to an array so schema/paging/helpers work.
                _rootArray = new JArray(_rootJson);
                _rootJson = _rootArray;
            }
            else
            {
                _rootArray = new JArray();
                _rootJson = _rootArray;
            }
        }

        private void BuildSchema()
        {
            Entities.Clear();
            EntitiesNames.Clear();
            if (_rootArray == null)
            {
                _schemaPersistence.Initialize(Entities);
                return;
            }
            var (ents, _) = JsonSchemaHelper.BuildEntityStructures(
                _rootArray,
                Path.GetFileNameWithoutExtension(_filePath ?? DatasourceName) ?? "Root",
                DatasourceName,
                DatasourceType);
            Entities = ents;
            EntitiesNames = Entities.Select(e => e.EntityName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _schemaPersistence.Initialize(Entities);
        }

        private void InitializeHelpers()
        {
            if (_rootArray == null || Entities.Count == 0) return;
            var rootEntity = Entities.First();
            _dataHelper = new JsonDataHelper(_rootArray, rootEntity, Entities, GetEntityType);
            _asyncHelper = new JsonAsyncDataHelper(_rootJson, Entities, GetEntityType);
            _crudHelper = new JsonCrudHelper(_rootJson, Entities, GetEntityType);
            _graphHelper = new JsonGraphHelper(_rootJson, Entities, GetEntityType);
        }

        private void PersistSchemaIfDirty()
        {
            _schemaPersistence.FlushIfDirty(Entities, ents =>
            {
                try
                {
                    DMEEditor?.ConfigEditor?.SaveDataSourceEntitiesValues(new DatasourceEntities
                    {
                        datasourcename = DatasourceName,
                        Entities = ents.ToList()
                    });
                }
                catch { }
            });
        }

        private void PersistDataIfDirty()
        {
            if (!_dataDirty)
                return;

            if (string.IsNullOrWhiteSpace(_filePath))
                return;

            lock (_lock)
            {
                if (!_dataDirty)
                    return;

                try
                {
                    JToken toWrite = _rootArray ?? new JArray();

                    // Preserve original root shape when possible.
                    if (_rootWasObject && toWrite is JArray arr && arr.Count == 1 && arr[0] is JObject)
                    {
                        toWrite = arr[0];
                    }

                    var json = toWrite.ToString(Formatting.Indented);
                    var directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var tempPath = _filePath + ".tmp";
                    File.WriteAllText(tempPath, json);

                    // Replace is the safest atomic-ish write on Windows; fallback to move if needed.
                    if (File.Exists(_filePath))
                    {
                        var backupPath = _filePath + ".bak";
                        File.Replace(tempPath, _filePath, backupPath, ignoreMetadataErrors: true);
                        try { File.Delete(backupPath); } catch { }
                    }
                    else
                    {
                        File.Move(tempPath, _filePath);
                    }

                    _dataDirty = false;
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = ex.Message;
                    try
                    {
                        PassEvent?.Invoke(this, new PassedArgs
                        {
                            EventType = "Error",
                            Messege = $"Failed to persist JSON data: {ex.Message}",
                          
                        });
                    }
                    catch { }
                }
            }
        }
        #endregion

        #region Metadata
        public IEnumerable<string> GetEntitesList() => EntitiesNames;

        public bool CheckEntityExist(string EntityName) => EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));

        public int GetEntityIdx(string entityName) => Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (refresh) { LoadJson(); BuildSchema(); InitializeHelpers(); }
            return Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            var es = GetEntityStructure(entityname, false);
            return es?.Relations ?? Enumerable.Empty<RelationShipKeys>();
        }

        public IEnumerable<TheTechIdea.Beep.DataBase.ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            var parent = GetEntityStructure(tablename, false);
            if (parent == null) return Enumerable.Empty<TheTechIdea.Beep.DataBase.ChildRelation>();
            // ChildRelation actual properties unknown; return empty list until definition known
            return Enumerable.Empty<TheTechIdea.Beep.DataBase.ChildRelation>();
        }

        public Type GetEntityType(string EntityName)
        {
            // For advanced scenario use dynamic type builder; current returns dictionary type
            return typeof(Dictionary<string, object>);
        }
        #endregion

        #region Data Retrieval
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            EnsureOpen();
            return _dataHelper?.GetEntities(EntityName, filter) ?? Enumerable.Empty<object>();
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            EnsureOpen();
            return _dataHelper?.GetEntitiesPaged(EntityName, filter, pageNumber, pageSize) ?? new PagedResult();
        }

        public async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            EnsureOpen();
            if (_asyncHelper == null)
                return Enumerable.Empty<object>();

            var list = new List<object>();
            await foreach (var item in _asyncHelper.StreamAsync(EntityName, Filter ?? new List<AppFilter>(), CancellationToken.None))
            {
                list.Add(item);
            }

            return list;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Treat query as entity name for now
            if (string.IsNullOrWhiteSpace(qrystr)) return Enumerable.Empty<object>();
            return GetEntity(qrystr.Trim(), null);
        }
        #endregion

        #region Advanced Features (P2)
        /// <summary>
        /// Optional graph hydration: returns dictionaries that include nested child entity collections.
        /// This does not change the underlying stored JSON; it only shapes the returned results.
        /// </summary>
        public IEnumerable<object> GetEntityGraph(string rootEntityName, List<AppFilter> rootFilters, int depth = 1, bool includeParentReference = true, bool includeAncestorChain = false)
        {
            EnsureOpen();
            if (_graphHelper == null)
                return Enumerable.Empty<object>();

            var options = new GraphHydrationOptions
            {
                Depth = Math.Max(0, depth),
                IncludeParentReference = includeParentReference,
                IncludeAncestorChain = includeAncestorChain
            };

            return _graphHelper.MaterializeGraph(rootEntityName, rootFilters ?? new List<AppFilter>(), options);
        }

        /// <summary>
        /// Scans the current JSON data for an entity and adds any missing fields to its EntityStructure.
        /// Marks schema as dirty so it will be persisted on Commit/Close.
        /// </summary>
        public IErrorsInfo SyncSchemaFromData(string entityName)
        {
            EnsureOpen();
            if (string.IsNullOrWhiteSpace(entityName))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Entity name is required.";
                return ErrorObject;
            }

            var es = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            if (es == null)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Entity not found.";
                return ErrorObject;
            }

            var arr = JsonPathNavigator.ResolveArray(_rootJson, es);
            if (arr == null)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Entity data is not an array.";
                return ErrorObject;
            }

            bool changed = false;
            try
            {
                changed |= JsonSchemaSyncHelper.SyncFieldsFromData(arr, es);
                changed |= JsonSchemaSyncHelper.EnsurePrimaryKeyIntegrity(es);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }

            if (changed)
                _schemaPersistence.MarkDirty();

            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = changed ? "Schema synchronized from data." : "Schema already up to date.";
            return ErrorObject;
        }
        #endregion

        #region CRUD
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            EnsureOpen();
            bool ok = _crudHelper?.Insert(EntityName, InsertedData) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok)
            {
                _schemaPersistence.MarkDirty();
                _dataDirty = true;
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            EnsureOpen();
            bool ok = _crudHelper?.Update(EntityName, UploadDataRow) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok)
            {
                _schemaPersistence.MarkDirty();
                _dataDirty = true;
            }
            return ErrorObject;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            EnsureOpen();
            var es = GetEntityStructure(EntityName, false);
            var pk = es?.PrimaryKeys?.FirstOrDefault();
            if (pk == null)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Primary key not defined.";
                return ErrorObject;
            }
            var val = UploadDataRow?.GetType().GetProperty(pk.FieldName)?.GetValue(UploadDataRow)?.ToString();
            if (string.IsNullOrWhiteSpace(val))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Primary key value missing.";
                return ErrorObject;
            }
            bool ok = _crudHelper?.Delete(EntityName, new AppFilter {FieldName = pk.FieldName, Operator = "=", FilterValue = val }) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok)
            {
                _schemaPersistence.MarkDirty();
                _dataDirty = true;
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities != null)
            {
                foreach (var e in entities) CreateEntityAs(e);
                _schemaPersistence.MarkDirty();
            }
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            if (entity == null) return false;
            if (CheckEntityExist(entity.EntityName)) return false;
            Entities.Add(entity);
            EntitiesNames.Add(entity.EntityName);
            _schemaPersistence.MarkDirty();
            return true;
        }
        #endregion

        #region Not Supported Operations
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            // Minimal semantics for file-backed JSON: treated as "unit of work" barrier.
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Transaction started";
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Transaction ended";
            return ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            EnsureOpen();
            PersistDataIfDirty();
            PersistSchemaIfDirty();
            if (ErrorObject.Flag != Errors.Failed)
            {
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Committed";
            }
            return ErrorObject;
        }
        public IErrorsInfo ExecuteSql(string sql) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "ExecuteSql not supported"; return ErrorObject; }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "RunScript not supported"; return ErrorObject; }
        public double GetScalar(string query) => 0;
        public Task<double> GetScalarAsync(string query) => Task.FromResult(0.0);
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Bulk update not supported"; return ErrorObject; }
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            // JSON file source does not generate Ddl scripts; return empty collection
            return Enumerable.Empty<ETLScriptDet>();
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    PersistDataIfDirty();
                    PersistSchemaIfDirty();
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

  
}
