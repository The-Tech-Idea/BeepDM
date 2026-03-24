using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Core MigrationManager shell.
    /// Feature groups are implemented in dedicated partial files.
    /// </summary>
    public partial class MigrationManager : IMigrationManager
    {
        private readonly IDMEEditor _editor;
        private readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();
        private readonly object _assemblyLock = new object();

        // Phase 4: stores the evidence snapshot from the most recent DiscoverEntityTypes call
        private AssemblyDiscoveryEvidence _lastDiscoveryEvidence;

        // Phase 3: DDL operation evidence accumulated during this session
        private readonly List<DdlOperationEvidence> _ddlEvidence = new List<DdlOperationEvidence>();

        public IDMEEditor DMEEditor => _editor;
        public IDataSource MigrateDataSource { get; set; }

        public MigrationManager(IDMEEditor editor, IDataSource dataSource = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            MigrateDataSource = dataSource;
        }

        private static IErrorsInfo CreateErrorsInfo(Errors flag, string message, Exception ex = null)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message,
                Ex = ex
            };
        }

        /// <summary>
        /// Returns the assembly discovery evidence snapshot from the most recent DiscoverEntityTypes call.
        /// Returns null if no discovery has run yet in this session.
        /// </summary>
        public AssemblyDiscoveryEvidence GetDiscoveryEvidence() => _lastDiscoveryEvidence;

        /// <summary>Returns all DDL operation evidence accumulated in this session.</summary>
        public IReadOnlyList<DdlOperationEvidence> GetDdlEvidence() => _ddlEvidence.AsReadOnly();

        private void EmitDdlEvidence(
            string operationName,
            string entityName,
            string columnName,
            string indexName,
            string sql,
            DdlOperationOutcome outcome,
            DdlHelperSource helperSource,
            string reasonCode = "")
        {
            string sqlHash = string.Empty;
            if (!string.IsNullOrWhiteSpace(sql))
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sql));
                sqlHash = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16).ToLowerInvariant();
            }
            _ddlEvidence.Add(new DdlOperationEvidence
            {
                OperationName   = operationName,
                EntityName      = entityName ?? string.Empty,
                ColumnName      = columnName ?? string.Empty,
                IndexName       = indexName  ?? string.Empty,
                Outcome         = outcome,
                HelperSource    = helperSource,
                SqlHash         = sqlHash,
                ReasonCode      = reasonCode,
                TimestampUtc    = DateTime.UtcNow
            });
        }

        private void TrackMigration(string operation, string entityName, string columnName, string sql, IErrorsInfo result)
        {
            try
            {
                var configEditor = _editor?.ConfigEditor as ConfigEditor;
                if (configEditor == null)
                    return;

                var dataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty;
                var dataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown;

                var record = new MigrationRecord
                {
                    MigrationId = Guid.NewGuid().ToString(),
                    Name = operation,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = result?.Flag == Errors.Ok,
                    Steps = new List<MigrationStep>
                    {
                        new MigrationStep
                        {
                            Operation = operation,
                            EntityName = entityName,
                            ColumnName = columnName,
                            Sql = sql ?? string.Empty,
                            Success = result?.Flag == Errors.Ok,
                            Message = result?.Message
                        }
                    }
                };

                configEditor.AppendMigrationRecord(dataSourceName, dataSourceType, record);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Beep", $"Failed to track migration operation '{operation}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
    }
}
