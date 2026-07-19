using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.Common.Retry;
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

        // Phase 7: cache the reflection Type→EntityStructure conversion so planning, CanSkip hashing,
        // drift, and each execution step don't re-reflect the same type. Keyed by Type; the resolved
        // structure is deterministic per type, and the model-interop cache still wins ahead of it.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, EntityStructure> _entityStructureCache
            = new System.Collections.Concurrent.ConcurrentDictionary<Type, EntityStructure>();

        // Phase 4: stores the evidence snapshot from the most recent DiscoverEntityTypes call
        private AssemblyDiscoveryEvidence _lastDiscoveryEvidence;

        // Phase 3: DDL operation evidence accumulated during this session
        private readonly List<DdlOperationEvidence> _ddlEvidence = new List<DdlOperationEvidence>();

        public IDMEEditor DMEEditor => _editor;
        public IDataSource MigrateDataSource { get; set; }

        private EntityReadOptions _readOptions = EntityReadOptions.Default;

        /// <summary>
        /// Phase 7 (C0): options controlling how .NET entity classes are read into structures during
        /// planning/drift (enum storage, nullable-reference-type nullability, relations/indexes, key
        /// policy). Defaults to <see cref="EntityReadOptions.Default"/>. Changing it clears the
        /// conversion cache so the next plan re-reads with the new options.
        /// </summary>
        public EntityReadOptions ReadOptions
        {
            get => _readOptions;
            set
            {
                _readOptions = value ?? EntityReadOptions.Default;
                ClearEntityStructureCache();
            }
        }

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
            MigrationRecordWriter.WriteOperation(
                _editor,
                operation,
                entityName,
                columnName,
                sql,
                result,
                MigrateDataSource?.DatasourceName ?? string.Empty,
                MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown);
        }
    }
}
