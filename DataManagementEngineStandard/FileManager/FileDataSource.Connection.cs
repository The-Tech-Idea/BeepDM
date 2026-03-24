using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Connection lifecycle partial — open, close, transactions, query routing.
    /// All format-specific work is delegated to <see cref="_reader"/>.
    /// </summary>
    public partial class FileDataSource
    {
        // ── Connection ───────────────────────────────────────────────────────

        public ConnectionState Openconnection()
        {
            try
            {
                // Ensure connection object exists
                if (Dataconnection == null && DMEEditor != null)
                {
                    Dataconnection = new FileConnection(DMEEditor)
                    {
                        Logger      = Logger,
                        ErrorObject = ErrorObject
                    };
                }

                // Hydrate connection properties from config store
                LoadConnectionProperties();

                // Resolve and configure the format reader for this DatasourceType
                FileReaderFactory.RegisterDefaults();
                _reader = FileReaderFactory.GetReader(DatasourceType);
                _reader.Configure(Dataconnection?.ConnectionProp);

                // Open the underlying connection (validates path etc.)
                ConnectionStatus = Dataconnection?.OpenConnection() ?? ConnectionState.Closed;
                if (ConnectionStatus != ConnectionState.Open)
                    return ConnectionStatus;

                // Validate the target file exists (warn, but do not fail —
                // the user may be about to create it via CreateEntityAs).
                string filePath = ResolveEntityFilePath(DatasourceName);
                if (!File.Exists(filePath))
                {
                    DMEEditor?.AddLogMessage("Warn",
                        $"FileDataSource: target file not found: {filePath}. " +
                        $"Use CreateEntityAs to create it.",
                        DateTime.Now, 0, DatasourceName, Errors.Ok);
                }

                // Auto-discover entity structure on first open
                if (Entities.Count == 0 && File.Exists(filePath))
                {
                    string entityName  = Path.GetFileNameWithoutExtension(filePath);
                    // Resolve directly through infer path to avoid overload ambiguity
                    // when stale/generated duplicate signatures are present.
                    EntityStructure es = InferEntityStructure(entityName);
                    if (es != null)
                    {
                        Entities.Add(es);
                        EntitiesNames.Add(es.EntityName);
                    }
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Fail",
                    $"FileDataSource.Openconnection failed: {ex.Message}",
                    DateTime.Now, -1, DatasourceName, Errors.Failed);
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        // ── Transactions ─────────────────────────────────────────────────────

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            _inTransaction = true;
            _pendingOperations.Clear();
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            _inTransaction = false;
            _pendingOperations.Clear();
            return ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            if (!_inTransaction) return ErrorObject;

            foreach (Func<IErrorsInfo> op in _pendingOperations.ToList())
                op();

            _pendingOperations.Clear();
            _inTransaction = false;
            return ErrorObject;
        }

        // ── SQL / Query pass-through ─────────────────────────────────────────

        public IErrorsInfo ExecuteSql(string sql)
        {
            // Files have no SQL engine; log and ignore.
            DMEEditor?.AddLogMessage("Info",
                "FileDataSource: ExecuteSql is not applicable. Use GetEntity / CRUD.",
                DateTime.Now, 0, sql, Errors.Ok);
            return ErrorObject;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            if (string.IsNullOrWhiteSpace(qrystr))
                return Enumerable.Empty<object>();

            string query = qrystr.Trim();

            if (query.StartsWith("select count", StringComparison.OrdinalIgnoreCase))
                return new object[] { GetScalar(query) };

            string entity = EntitiesNames.FirstOrDefault() ?? DatasourceName;
            return GetEntity(entity, new List<AppFilter>());
        }
    }
}
