using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            if (_rootJson.Type == JTokenType.Array)
                _rootArray = (JArray)_rootJson;
            else if (_rootJson.Type == JTokenType.Object)
                _rootArray = new JArray(_rootJson);
            else
                _rootArray = new JArray();
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
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = int.MaxValue;
            var all = GetEntity(EntityName, filter).ToList();
            int skip = (pageNumber - 1) * pageSize;
            var page = skip >= all.Count ? new List<object>() : all.Skip(skip).Take(pageSize).ToList();
            return new PagedResult { Data = page };
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            // Treat query as entity name for now
            if (string.IsNullOrWhiteSpace(qrystr)) return Enumerable.Empty<object>();
            return GetEntity(qrystr.Trim(), null);
        }
        #endregion

        #region CRUD
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            EnsureOpen();
            bool ok = _crudHelper?.Insert(EntityName, InsertedData) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok) _schemaPersistence.MarkDirty();
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            EnsureOpen();
            bool ok = _crudHelper?.Update(EntityName, UploadDataRow) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok) _schemaPersistence.MarkDirty();
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
            var val = UploadDataRow?.GetType().GetProperty(pk.fieldname)?.GetValue(UploadDataRow)?.ToString();
            if (string.IsNullOrWhiteSpace(val))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Primary key value missing.";
                return ErrorObject;
            }
            bool ok = _crudHelper?.Delete(EntityName, new AppFilter { FieldName = pk.fieldname, Operator = "=", FilterValue = val }) ?? false;
            ErrorObject.Flag = ok ? Errors.Ok : Errors.Failed;
            if (ok) _schemaPersistence.MarkDirty();
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
            ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Transactions not supported"; return ErrorObject;
        }
        public IErrorsInfo EndTransaction(PassedArgs args) => BeginTransaction(args);
        public IErrorsInfo Commit(PassedArgs args) => BeginTransaction(args);
        public IErrorsInfo ExecuteSql(string sql) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "ExecuteSql not supported"; return ErrorObject; }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "RunScript not supported"; return ErrorObject; }
        public double GetScalar(string query) => 0;
        public Task<double> GetScalarAsync(string query) => Task.FromResult(0.0);
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Bulk update not supported"; return ErrorObject; }
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            // JSON file source does not generate DDL scripts; return empty collection
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
