using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Summary of migration status comparing Entity classes with database state.
    /// </summary>
    public class MigrationSummary
    {
        /// <summary>
        /// List of entity names that need to be created in the database.
        /// </summary>
        public List<string> EntitiesToCreate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that need updates (missing columns, etc.).
        /// </summary>
        public List<string> EntitiesToUpdate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that are up-to-date with their Entity classes.
        /// </summary>
        public List<string> EntitiesUpToDate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of errors encountered during migration summary generation.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Total count of entities that need migration.
        /// </summary>
        public int TotalPendingMigrations => EntitiesToCreate.Count + EntitiesToUpdate.Count;
        
        /// <summary>
        /// Indicates if there are any pending migrations.
        /// </summary>
        public bool HasPendingMigrations => TotalPendingMigrations > 0;
    }

    /// <summary>
    /// Severity levels used by migration readiness diagnostics.
    /// </summary>
    public enum MigrationIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Structured migration-readiness finding with datasource and operational guidance.
    /// </summary>
    public class MigrationReadinessIssue
    {
        public string Code { get; set; } = string.Empty;
        public MigrationIssueSeverity Severity { get; set; } = MigrationIssueSeverity.Info;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Datasource-aware readiness report for enterprise migration planning.
    /// </summary>
    public class MigrationReadinessReport
    {
        public string DataSourceName { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
        public bool UsesDiscovery { get; set; }
        public bool HelperAvailable { get; set; }
        public bool SupportsSchemaEvolution { get; set; }
        public bool IsSchemaEnforced { get; set; }
        public bool SupportsTransactions { get; set; }
        public string CapabilityNotes { get; set; } = string.Empty;
        public List<string> MigrationBestPractices { get; set; } = new List<string>();
        [Obsolete("Use MigrationBestPractices instead. This alias remains for backward compatibility.")]
        public List<string> ProviderBestPractices
        {
            get => MigrationBestPractices;
            set => MigrationBestPractices = value ?? new List<string>();
        }
        public List<MigrationReadinessIssue> Issues { get; set; } = new List<MigrationReadinessIssue>();
        public int EntityTypeCount { get; set; }
        public bool HasBlockingIssues => Issues.Exists(issue => issue.Severity == MigrationIssueSeverity.Error);
    }

    public interface IMigrationManager
    {
        IDMEEditor DMEEditor { get; }
        IDataSource MigrateDataSource { get; set; }

        // ── Entity-level operations ──

        IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true);
        IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true);
        IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired);

        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null);

        // ── Assembly registration for cross-project discovery ──

        /// <summary>
        /// Register additional assemblies for entity type discovery.
        /// Use this when entity classes live in separate projects/DLLs that may not be
        /// automatically found by AppDomain scanning (e.g., lazily-loaded assemblies).
        /// </summary>
        void RegisterAssembly(Assembly assembly);

        /// <summary>
        /// Register multiple assemblies for entity type discovery.
        /// </summary>
        void RegisterAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Gets all currently registered assemblies (manual + auto-discovered).
        /// Useful for diagnostics when entity types are not being found.
        /// </summary>
        IReadOnlyList<Assembly> GetRegisteredAssemblies();

        // ── Entity type discovery ──

        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Searches in the given assembly, registered assemblies, AppDomain assemblies,
        /// the entry assembly and its referenced assemblies, and DMEEditor's assembly handler.
        /// </summary>
        List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true);

        /// <summary>
        /// Discovers all types that inherit from Entity in all searchable assemblies.
        /// Scans registered assemblies, AppDomain, entry assembly references, and DMEEditor's assembly handler.
        /// </summary>
        List<Type> DiscoverAllEntityTypes(bool includeSubNamespaces = true);

        // ── Database-level migration (discovery-based) ──

        /// <summary>
        /// Ensures database is created with all discovered Entity types.
        /// Similar to EF Core's Database.EnsureCreated().
        /// Creates entities for all classes that inherit from Entity in the given namespace/assembly.
        /// </summary>
        IErrorsInfo EnsureDatabaseCreated(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Applies migrations for all discovered Entity types.
        /// Compares Entity classes with database schema and applies changes.
        /// Similar to EF Core's Database.Migrate().
        /// </summary>
        IErrorsInfo ApplyMigrations(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Gets migration summary comparing Entity classes with current database state.
        /// Returns list of entities that need creation or updates.
        /// </summary>
        MigrationSummary GetMigrationSummary(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true);

        /// <summary>
        /// Builds a datasource-aware readiness report for discovery-based migrations.
        /// Use this before running migrations in enterprise environments where platform behavior,
        /// naming standards, and operational risk need explicit review.
        /// </summary>
        MigrationReadinessReport GetMigrationReadiness(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true);

        // ── Database-level migration (explicit types — no discovery needed) ──

        /// <summary>
        /// Ensures database is created for the given entity types.
        /// Use this when you know exactly which types to create — bypasses assembly discovery entirely.
        /// This is the most reliable approach for cross-project scenarios.
        /// </summary>
        /// <example>
        /// migrationManager.EnsureDatabaseCreatedForTypes(
        ///     new[] { typeof(Customer), typeof(Product), typeof(Invoice) },
        ///     progress: progressReporter);
        /// </example>
        IErrorsInfo EnsureDatabaseCreatedForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Applies migrations for the given entity types.
        /// Use this when you know exactly which types to migrate — bypasses assembly discovery entirely.
        /// </summary>
        IErrorsInfo ApplyMigrationsForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Builds a datasource-aware readiness report for explicit-type migrations.
        /// </summary>
        MigrationReadinessReport GetMigrationReadinessForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true);

        /// <summary>
        /// Returns datasource-aware migration best-practice guidance for the current migration datasource
        /// or for an explicitly requested datasource type/category.
        /// </summary>
        IReadOnlyList<string> GetMigrationBestPractices(DataSourceType? dataSourceType = null, DatasourceCategory? dataSourceCategory = null);

        /// <summary>
        /// Backward-compatible alias for older provider-named guidance calls.
        /// </summary>
        [Obsolete("Use GetMigrationBestPractices instead. This alias remains for backward compatibility.")]
        IReadOnlyList<string> GetProviderBestPractices(DataSourceType? dataSourceType = null);
    }
}
