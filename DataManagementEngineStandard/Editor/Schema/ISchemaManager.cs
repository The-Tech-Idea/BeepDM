using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Single-responsibility service for ALL schema concerns in BeepDM.
    /// Owns:
    /// <list type="bullet">
    ///   <item>Data-source resolution + entity-structure loading (used by everyone).</item>
    ///   <item>Cross-datasource preflight (will the destination accept the source?).</item>
    ///   <item>Sync-draft production (DataSyncSchema for downstream execution).</item>
    ///   <item>Entity existence + creation (thin wrapper over IDataSource.CreateEntityAs).</item>
    ///   <item>Schema snapshots (capture, persist, compare, drift report).</item>
    /// </list>
    /// Does NOT: move data (DataImportManager), execute DDL plans (MigrationManager),
    /// or run sync pipelines (BeepSyncManager). It only plans and validates.
    /// </summary>
    public interface ISchemaManager
    {
        // ── Resolution & loading ──────────────────────────────────────────
        Task<SchemaResolutionResult> ResolveDataSourceAsync(
            string dataSourceName, CancellationToken token = default);

        Task<EntityStructure?> LoadEntityStructureAsync(
            string dataSourceName, string entityName, bool refresh = false,
            CancellationToken token = default);

        Task<bool> EntityExistsAsync(
            string dataSourceName, string entityName,
            CancellationToken token = default);

        Task<EntityStructure?> TryGetEntityStructureAsync(
            Type type, CancellationToken token = default);

        // ── Preflight & draft ─────────────────────────────────────────────
        Task<SchemaPreflightResult> RunPreflightAsync(
            SchemaRequest request,
            Action<string>? log = null,
            CancellationToken token = default);

        Task<SchemaDraftResult> BuildSyncDraftAsync(
            SchemaRequest request,
            CancellationToken token = default);

        // ── Entity creation (thin wrapper over IDataSource.CreateEntityAs) ─
        Task<SchemaEntityResult> CreateEntityAsync(
            string dataSourceName, EntityStructure entity,
            CancellationToken token = default);

        // ── Snapshots & drift ─────────────────────────────────────────────
        Task<SchemaSnapshot> CaptureFromTypeAsync(
            Type type, string dataSourceName, CancellationToken token = default);

        Task<SchemaSnapshot> CaptureFromDataSourceAsync(
            string dataSourceName, string entityName, bool refresh = true,
            CancellationToken token = default);

        Task<SchemaDriftReport> InspectAsync(
            Type type, string dataSourceName, string entityName,
            CancellationToken token = default);

        Task<SchemaSnapshot> SaveBaselineAsync(
            Type type, string dataSourceName, string entityName,
            CancellationToken token = default);

        Task<SchemaDriftReport?> DiffAgainstBaselineAsync(
            Type type, string dataSourceName, string entityName,
            CancellationToken token = default);

        Task<SchemaSnapshot> SaveDatabaseBaselineAsync(
            string dataSourceName, string entityName,
            CancellationToken token = default);

        Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(
            IEnumerable<Type> types, string dataSourceName,
            CancellationToken token = default);

        /// <summary>Same as the string-name overload but accepts a pre-resolved <see cref="IDataSource"/>.</summary>
        Task<Dictionary<string, SchemaDriftReport>> InspectManyAsync(
            IEnumerable<Type> types, IDataSource dataSource,
            CancellationToken token = default);

        /// <summary>Generates a DDL change plan from a drift report without executing it.</summary>
        Task<SchemaChangePlan> GenerateChangePlanAsync(
            SchemaDriftReport driftReport,
            CancellationToken token = default);
    }

    /// <summary>Result of resolving a data source by name (and auto-opening it if not open).</summary>
    public sealed class SchemaResolutionResult
    {
        public IDataSource? DataSource { get; init; }
        public bool IsOpen { get; init; }
        public IErrorsInfo Status { get; init; } = new ErrorsInfo { Flag = Errors.Ok, Message = "Resolved." };
    }
}
