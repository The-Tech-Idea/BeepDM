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

        /// <summary>
        /// Whether planning writes the full plan artifact into the migration history.
        /// </summary>
        /// <remarks>
        /// The history file is a single JSON document rewritten on every record, so a plan record that carries the
        /// whole artifact makes each later append rewrite it too. For a plan over thousands of entities that record
        /// is hundreds of megabytes, and appending per operation then costs O(plan size) each time - which is what
        /// made a 2716-entity schema apply take minutes per module. Callers that keep the plan in memory and execute
        /// it in the same session (the desktop's module setup) set this to false; execution and checkpoint records
        /// are still written, so the audit trail of what ran remains. Defaults to true, which is the existing
        /// behaviour for callers that reload a plan by id after a restart.
        /// </remarks>
        public bool PersistPlanArtifactsInHistory { get; set; } = true;

        /// <summary>
        /// Whether each executed operation (one record per created/altered entity) is appended to the migration
        /// history. The history is a whole-file JSON document, so one record per entity makes a large apply rewrite
        /// it thousands of times. A caller running a generated, thousands-of-entities setup in one session can turn
        /// this off; the throttled execution checkpoints still record progress and outcome. Defaults to true.
        /// </summary>
        public bool TrackOperationsInHistory { get; set; } = true;

        public IDataSource MigrateDataSource
        {
            get => _executionScope.Value?.Source ?? _migrationSource;
            set { lock (_bindingGate) { RejectBindingChange(); _migrationSource = value; } }
        }

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
            if (!TrackOperationsInHistory)
            {
                return;
            }

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
