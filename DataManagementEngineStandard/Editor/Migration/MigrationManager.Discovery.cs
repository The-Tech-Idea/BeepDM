using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Searches in the given assembly, registered assemblies, entry assembly and its references,
        /// AppDomain assemblies, and DMEEditor's assembly handler.
        /// </summary>
        public List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true)
        {
            var entityTypes = new List<Type>();
            IEnumerable<(Assembly Assembly, AssemblySourceKind Source)> assemblyWithSources;

            if (assembly != null)
            {
                var asmSet = new List<(Assembly, AssemblySourceKind)> { (assembly, AssemblySourceKind.ManualRegistered) };
                try
                {
                    foreach (var refName in assembly.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(refName);
                            if (refAsm != null && !refAsm.IsDynamic)
                                asmSet.Add((refAsm, AssemblySourceKind.EntryReference));
                        }
                        catch { }
                    }
                }
                catch { }
                assemblyWithSources = asmSet;
            }
            else
            {
                assemblyWithSources = GetSearchableAssembliesWithSource();
            }

            var asmList = assemblyWithSources.ToList();

            // Phase 4: build discovery evidence
            var evidence = new AssemblyDiscoveryEvidence
            {
                TotalAssembliesConsidered = asmList.Count
            };

            _editor?.AddLogMessage("Beep",
                $"MigrationManager.DiscoverEntityTypes: Scanning {asmList.Count} assembly(ies)" +
                (string.IsNullOrWhiteSpace(namespaceName) ? "" : $" in namespace '{namespaceName}'"),
                DateTime.Now, 0, null, Errors.Ok);

            int scannedCount = 0;
            foreach (var (asm, sourceKind) in asmList)
            {
                var record = new AssemblyDiscoveryRecord
                {
                    AssemblyFullName = asm?.FullName ?? string.Empty,
                    AssemblyName     = asm?.GetName().Name ?? string.Empty,
                    Source           = sourceKind
                };

                if (!ShouldScanAssembly(asm, assembly))
                {
                    record.Skipped = true;
                    record.SkipReason = "Framework or system assembly excluded from scan";
                    evidence.Records.Add(record);
                    evidence.Skipped++;
                    continue;
                }

                try
                {
                    var types = asm.GetTypes()
                        .Where(t => IsEntityType(t, namespaceName, includeSubNamespaces))
                        .ToList();

                    if (types.Count > 0)
                    {
                        _editor?.AddLogMessage("Beep",
                            $"  Found {types.Count} Entity type(s) in '{asm.GetName().Name}': {string.Join(", ", types.Select(t => t.Name))}",
                            DateTime.Now, 0, null, Errors.Ok);
                    }

                    entityTypes.AddRange(types);
                    scannedCount++;
                    record.Scanned = true;
                    record.FoundEntityTypes.AddRange(types.Select(t => t.FullName ?? t.Name));

                    // Populate entity origin map
                    foreach (var t in types)
                        evidence.EntityTypeOriginMap[t.FullName ?? t.Name] = asm.GetName().Name ?? string.Empty;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var loadedTypes = ex.Types
                        .Where(t => t != null && IsEntityType(t, namespaceName, includeSubNamespaces))
                        .ToList();
                    entityTypes.AddRange(loadedTypes);
                    scannedCount++;
                    record.Scanned = true;
                    record.FoundEntityTypes.AddRange(loadedTypes.Select(t => t.FullName ?? t.Name));
                    foreach (var t in loadedTypes)
                        evidence.EntityTypeOriginMap[t.FullName ?? t.Name] = asm.GetName().Name ?? string.Empty;

                    LogLoaderExceptions(asm, ex, record);

                    if (loadedTypes.Count > 0)
                    {
                        _editor?.AddLogMessage("Beep",
                            $"  Found {loadedTypes.Count} Entity type(s) in partially-loaded '{asm.GetName().Name}'",
                            DateTime.Now, 0, null, Errors.Warning);
                    }
                }
                catch (Exception ex)
                {
                    record.Skipped = true;
                    record.SkipReason = $"Scan failed: {ex.Message}";
                    _editor?.AddLogMessage("Beep",
                        $"  Warning: Could not scan assembly '{asm.GetName().Name}': {ex.Message}",
                        DateTime.Now, 0, null, Errors.Warning);
                }

                evidence.Records.Add(record);
                if (record.Scanned) evidence.Scanned++;
            }

            var result = entityTypes.Distinct().ToList();

            // Phase 4: namespace scope diagnostics when nothing found with a filter applied
            if (result.Count == 0 && !string.IsNullOrWhiteSpace(namespaceName))
            {
                var scannedNames = evidence.Records.Where(r => r.Scanned).Select(r => r.AssemblyName).ToList();
                evidence.NamespaceScopeDiagnostics.Add(
                    $"No entity types found in namespace '{namespaceName}'. Scanned {scannedNames.Count} assemblies: {string.Join(", ", scannedNames)}");
            }

            _lastDiscoveryEvidence = evidence;

            _editor?.AddLogMessage("Beep",
                $"MigrationManager.DiscoverEntityTypes: Scanned {scannedCount}/{asmList.Count} assemblies, found {result.Count} distinct Entity type(s)",
                DateTime.Now, 0, null, result.Count > 0 ? Errors.Ok : Errors.Warning);

            return result;
        }

        /// <summary>
        /// Discovers all types that inherit from Entity in all searchable assemblies.
        /// Scans registered assemblies, AppDomain, entry assembly references, and DMEEditor's assembly handler.
        /// </summary>
        public List<Type> DiscoverAllEntityTypes(bool includeSubNamespaces = true)
        {
            return DiscoverEntityTypes(null, null, includeSubNamespaces);
        }

        /// <summary>
        /// Ensures database is created with all discovered Entity types.
        /// Similar to EF Core's Database.EnsureCreated().
        /// Creates entities for all classes that inherit from Entity.
        /// </summary>
        public IErrorsInfo EnsureDatabaseCreated(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                var readiness = BuildReadinessReport(entityTypes, usesDiscovery: true);
                LogReadinessReport(nameof(EnsureDatabaseCreated), readiness, progress);
                if (readiness.HasBlockingIssues)
                {
                    var blocking = string.Join("; ", readiness.Issues
                        .Where(issue => issue.Severity == MigrationIssueSeverity.Error)
                        .Select(issue => issue.Message));
                    return CreateErrorsInfo(Errors.Failed, $"Migration readiness failed: {blocking}");
                }

                if (entityTypes.Count == 0)
                {
                    var msg = string.IsNullOrWhiteSpace(namespaceName)
                        ? "No Entity types found in loaded assemblies"
                        : $"No Entity types found in namespace '{namespaceName}'";
                    return CreateErrorsInfo(Errors.Warning, msg);
                }

                progress?.Report(new PassedArgs { Messege = $"Found {entityTypes.Count} Entity type(s) to migrate" });

                var errors = new List<string>();
                int created = 0;
                int skipped = 0;

                foreach (var entityType in entityTypes)
                {
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = $"Processing Entity type: {entityType.Name}" });

                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
                        if (entityStructure == null)
                        {
                            errors.Add($"Failed to convert {entityType.Name} to EntityStructure");
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        // Ensure entity exists (create if missing)
                        var result = EnsureEntity(entityStructure, createIfMissing: true, addMissingColumns: false);
                        if (result.Flag == Errors.Ok)
                        {
                            created++;
                        }
                        else if (result.Flag == Errors.Warning && result.Message.Contains("already exists"))
                        {
                            skipped++;
                        }
                        else
                        {
                            errors.Add($"{entityType.Name}: {result.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entityType.Name}: {ex.Message}");
                    }
                }

                var summary = $"Created {created} entity(ies), skipped {skipped} existing";
                if (errors.Count > 0)
                {
                    summary += $", {errors.Count} error(s)";
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", errors)}");
                }

                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during EnsureDatabaseCreated: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies migrations for all discovered Entity types.
        /// Compares Entity classes with database schema and applies changes.
        /// Similar to EF Core's Database.Migrate().
        /// </summary>
        public IErrorsInfo ApplyMigrations(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                var readiness = BuildReadinessReport(entityTypes, usesDiscovery: true);
                LogReadinessReport(nameof(ApplyMigrations), readiness, progress);
                if (readiness.HasBlockingIssues)
                {
                    var blocking = string.Join("; ", readiness.Issues
                        .Where(issue => issue.Severity == MigrationIssueSeverity.Error)
                        .Select(issue => issue.Message));
                    return CreateErrorsInfo(Errors.Failed, $"Migration readiness failed: {blocking}");
                }

                if (entityTypes.Count == 0)
                {
                    var msg = string.IsNullOrWhiteSpace(namespaceName)
                        ? "No Entity types found in loaded assemblies"
                        : $"No Entity types found in namespace '{namespaceName}'";
                    return CreateErrorsInfo(Errors.Warning, msg);
                }

                progress?.Report(new PassedArgs { Messege = $"Applying migrations for {entityTypes.Count} Entity type(s)" });

                var pipelineResult = ExecuteEntityMigrationPipeline(
                    entityTypes,
                    usesDiscovery: true,
                    source: EntityMigrationSource.DiscoveryAssembly,
                    addMissingColumns: addMissingColumns,
                    progress: progress);

                var summary = pipelineResult.ToSummaryString();
                if (pipelineResult.ErrorCount > 0)
                    return CreateErrorsInfo(Errors.Failed, $"{summary}. Errors: {string.Join("; ", pipelineResult.Entries.Where(e => !e.Success).Select(e => $"{e.EntityName}: {e.Message}"))}");

                return CreateErrorsInfo(Errors.Ok, summary);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during ApplyMigrations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets migration summary comparing Entity classes with current database state.
        /// Returns list of entities that need creation or updates.
        /// </summary>
        public MigrationSummary GetMigrationSummary(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true)
        {
            var summary = new MigrationSummary
            {
                EntitiesToCreate = new List<string>(),
                EntitiesToUpdate = new List<string>(),
                EntitiesUpToDate = new List<string>(),
                Errors = new List<string>()
            };

            if (MigrateDataSource == null)
            {
                summary.Errors.Add("Migration data source is not set");
                return summary;
            }

            summary.DataSourceType = MigrateDataSource.DatasourceType;
            summary.DataSourceCategory = MigrateDataSource.Category;

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                var capabilities = GetCapabilities(summary.DataSourceType, summary.DataSourceCategory);

                foreach (var entityType in entityTypes)
                {
                    try
                    {
                        // Convert Entity type to EntityStructure
                        var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
                        if (entityStructure == null)
                        {
                            summary.Errors.Add($"Failed to convert {entityType.Name} to EntityStructure");
                            continue;
                        }

                        // Use table name from type name or Table attribute if present
                        var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                        if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        {
                            entityStructure.EntityName = tableAttr.Name;
                        }

                        var exists = MigrateDataSource.CheckEntityExist(entityStructure.EntityName);

                        // Phase 2: Build per-entity decision record with capability context snapshot
                        var decisionRecord = new EntityDecisionRecord
                        {
                            EntityName = entityStructure.EntityName,
                            Source = EntityMigrationSource.DiscoveryAssembly,
                            AssemblyName = entityType.Assembly?.GetName().Name ?? string.Empty,
                            CapabilityContextSnapshot =
                            {
                                ["SupportsSchemaEvolution"] = capabilities.SupportsSchemaEvolution.ToString(),
                                ["IsSchemaEnforced"] = capabilities.IsSchemaEnforced.ToString(),
                                ["SupportsTransactions"] = capabilities.SupportsTransactions.ToString(),
                                ["HelperAvailable"] = TryHasHelper(summary.DataSourceType).ToString()
                            }
                        };

                        if (!exists)
                        {
                            summary.EntitiesToCreate.Add(entityStructure.EntityName);
                            decisionRecord.Decision = EntityMigrationDecision.Create;
                            decisionRecord.DecisionReasonCode = "ENTITY-MISSING";
                        }
                        else
                        {
                            // Check for missing columns
                            var current = MigrateDataSource.GetEntityStructure(entityStructure.EntityName, true);
                            if (current != null)
                            {
                                var missingColumns = GetMissingColumns(current, entityStructure);
                                if (missingColumns.Count > 0)
                                {
                                    summary.EntitiesToUpdate.Add($"{entityStructure.EntityName} ({missingColumns.Count} missing column(s))");
                                    decisionRecord.Decision = EntityMigrationDecision.Update;
                                    decisionRecord.DecisionReasonCode = "COLUMNS-MISSING";
                                }
                                else
                                {
                                    summary.EntitiesUpToDate.Add(entityStructure.EntityName);
                                    decisionRecord.Decision = EntityMigrationDecision.NoChange;
                                    decisionRecord.DecisionReasonCode = "ENTITY-UP-TO-DATE";
                                }
                            }
                            else
                            {
                                summary.EntitiesUpToDate.Add(entityStructure.EntityName);
                                decisionRecord.Decision = EntityMigrationDecision.NoChange;
                                decisionRecord.DecisionReasonCode = "ENTITY-UP-TO-DATE";
                            }
                        }

                        summary.EntityDecisions.Add(decisionRecord);
                    }
                    catch (Exception ex)
                    {
                        summary.Errors.Add($"{entityType.Name}: {ex.Message}");
                    }
                }

                // Phase 2: Compute stable report hash
                summary.ReportHash = ComputeReadinessReportHash(
                    MigrateDataSource.DatasourceName,
                    summary.DataSourceType,
                    summary.DataSourceCategory,
                    usesDiscovery: true,
                    entityTypeCount: summary.EntityDecisions.Count,
                    issues: summary.EntityDecisions.Select(d => new MigrationReadinessIssue
                    {
                        Code = d.DecisionReasonCode,
                        EntityName = d.EntityName,
                        Severity = d.Decision == EntityMigrationDecision.Error ? MigrationIssueSeverity.Error : MigrationIssueSeverity.Info
                    }));
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Exception during GetMigrationSummary: {ex.Message}");
            }

            return summary;
        }

        /// <summary>
        /// Builds a datasource-aware readiness report for discovery-based migrations.
        /// </summary>
        public MigrationReadinessReport GetMigrationReadiness(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true)
        {
            if (MigrateDataSource == null)
            {
                return CreateReadinessReportWithError("datasource-not-set", "Migration data source is not set", "Configure MigrateDataSource before running migration readiness checks.");
            }

            try
            {
                var entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
                return BuildReadinessReport(entityTypes, usesDiscovery: true);
            }
            catch (Exception ex)
            {
                var report = CreateBaseReadinessReport(usesDiscovery: true);
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "readiness-discovery-failed",
                    Severity = MigrationIssueSeverity.Error,
                    Message = $"Could not discover entity types: {ex.Message}",
                    Recommendation = "Register the required assemblies explicitly or use the explicit-type migration APIs."
                });
                return report;
            }
        }

        /// <summary>
        /// Builds a datasource-aware readiness report for explicit-type migrations.
        /// </summary>
        public MigrationReadinessReport GetMigrationReadinessForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true)
        {
            if (MigrateDataSource == null)
            {
                return CreateReadinessReportWithError("datasource-not-set", "Migration data source is not set", "Configure MigrateDataSource before running migration readiness checks.");
            }

            return BuildReadinessReport(entityTypes, usesDiscovery: false);
        }

        /// <summary>
        /// Returns datasource-aware migration best-practice guidance for the current or requested platform.
        /// </summary>
        public IReadOnlyList<string> GetMigrationBestPractices(DataSourceType? dataSourceType = null, DatasourceCategory? dataSourceCategory = null)
        {
            var effectiveType = dataSourceType ?? MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown;
            var effectiveCategory = dataSourceCategory ?? MigrateDataSource?.Category ?? MigrateDataSource?.Dataconnection?.ConnectionProp?.Category ?? DatasourceCategory.NONE;
            var capabilities = GetCapabilities(effectiveType, effectiveCategory);
            var practices = new List<string>();

            switch (effectiveCategory)
            {
                case DatasourceCategory.FILE:
                case DatasourceCategory.VIEWS:
                    practices.Add("Prefer additive and non-destructive changes; keep file backups and avoid assuming full ALTER support.");
                    practices.Add("Treat schema preparation and data bootstrap as separate steps so recovery is simple when file format changes fail.");
                    break;

                case DatasourceCategory.NOSQL:
                    practices.Add("Favor backward-compatible document or key-space evolution and validate whether schema enforcement is optional or application-driven.");
                    practices.Add("Avoid assuming joins, relational constraints, or in-place DDL parity with RDBMS platforms.");
                    break;

                case DatasourceCategory.WEBAPI:
                case DatasourceCategory.Connector:
                    practices.Add("Treat migration as contract evolution rather than physical DDL; validate endpoint capabilities, throttling, and version compatibility first.");
                    practices.Add("Do not assume remote APIs support transactional rollback or bulk schema operations.");
                    break;

                case DatasourceCategory.STREAM:
                case DatasourceCategory.QUEUE:
                    practices.Add("Treat entities as topics, streams, or queues; validate retention, partitioning, and consumer impact before structural changes.");
                    practices.Add("Prefer new-version topics/queues over destructive in-place changes when downstream consumers are already attached.");
                    break;

                case DatasourceCategory.INMEMORY:
                    practices.Add("Treat schema as volatile runtime metadata and separate durable migration concerns from cache or in-memory topology changes.");
                    practices.Add("Validate warm-up, replay, or persistence strategy before relying on in-memory schema preparation in production.");
                    break;

                case DatasourceCategory.VectorDB:
                    practices.Add("Review embedding dimension, index type, and similarity-metric compatibility before altering vector collections.");
                    practices.Add("Plan reindexing or backfill workflows explicitly because structural changes may require expensive rebuilds.");
                    break;

                case DatasourceCategory.Blockchain:
                    practices.Add("Treat migration as smart-contract or ledger-compatible evolution rather than mutable table DDL.");
                    practices.Add("Avoid assumptions about destructive rollback; validate versioning, signing, and deployment governance first.");
                    break;
            }

            switch (effectiveType)
            {
                case DataSourceType.Oracle:
                    practices.Add("Keep identifiers conservative and stable; avoid quoted-name dependence and review sequence/identity behavior explicitly.");
                    practices.Add("Prefer additive migrations and staged rewrites for type changes rather than assuming in-place ALTER operations are operationally cheap.");
                    break;

                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    practices.Add("Review lock impact, default constraints, and index rebuild implications before applying type or nullability changes on large tables.");
                    practices.Add("Validate helper-generated DDL against operational requirements such as online maintenance windows and rollback plans.");
                    break;

                case DataSourceType.SqlLite:
                case DataSourceType.DuckDB:
                case DataSourceType.FlatFile:
                case DataSourceType.CSV:
                case DataSourceType.TSV:
                case DataSourceType.Text:
                case DataSourceType.Json:
                case DataSourceType.XML:
                case DataSourceType.YAML:
                    practices.Add("Prefer create-if-missing and additive-only changes; destructive alters and complex column rewrites are limited or emulated.");
                    practices.Add("Separate schema preparation from seed/bootstrap flows and validate file-level backups before structural changes.");
                    break;

                case DataSourceType.Mysql:
                case DataSourceType.MariaDB:
                case DataSourceType.Postgre:
                case DataSourceType.DB2:
                case DataSourceType.FireBird:
                case DataSourceType.Hana:
                case DataSourceType.Cockroach:
                case DataSourceType.SnowFlake:
                case DataSourceType.AWSRDS:
                    practices.Add("Validate reserved words, casing, nullability transitions, and helper capability on the real target engine before rollout.");
                    practices.Add("Treat provider portability as opt-in; rerun migration summary and readiness checks per provider rather than reusing assumptions.");
                    break;

                default:
                    if (practices.Count == 0)
                    {
                        practices.Add("Prefer explicit-type migration for application-owned schemas and discovery only for dynamic or plugin-driven entity sets.");
                        practices.Add("Review migration summary and datasource capabilities before applying structural changes in production.");
                    }
                    break;
            }

            if (!capabilities.SupportsSchemaEvolution)
            {
                practices.Add("This datasource advertises limited schema evolution support; verify whether migration should create new entities/resources rather than alter existing ones.");
            }

            if (!capabilities.IsSchemaEnforced)
            {
                practices.Add("This datasource is not strongly schema-enforced; validate compatibility at the application layer rather than relying only on DDL.");
            }

            practices.Add("Resolve operations through IDMEEditor.GetDataSourceHelper when helper-backed validation or platform-specific DDL is required; avoid hardcoded SQL or file mutations in application code.");
            practices.Add("Normalize and persist ConnectionProperties before running migrations so datasource category, file settings, and remote endpoints stay consistent across discovery, creation, and upgrade steps.");
            practices.Add("Keep entity names deterministic and avoid platform-specific type assumptions in entity classes; let the datasource map .NET types.");
            return practices.AsReadOnly();
        }

        [Obsolete("Use GetMigrationBestPractices instead. This alias remains for backward compatibility.")]
        public IReadOnlyList<string> GetProviderBestPractices(DataSourceType? dataSourceType = null)
        {
            return GetMigrationBestPractices(dataSourceType);
        }

        /// <summary>
        /// Checks if a type inherits from Entity (or implements IEntity) and matches namespace filter.
        /// </summary>
        private bool IsEntityType(Type type, string namespaceFilter, bool includeSubNamespaces)
        {
            if (type == null) return false;
            if (!type.IsClass) return false;
            if (type.IsAbstract) return false;
            if (type.IsInterface) return false;
            if (type.IsGenericTypeDefinition) return false;
            if (type.IsNested && !type.IsNestedPublic) return false;

            // Check if type inherits from Entity class
            var baseType = type.BaseType;
            bool inheritsFromEntity = false;
            while (baseType != null)
            {
                if (baseType.Name == "Entity" || baseType.FullName == "TheTechIdea.Beep.Editor.Entity")
                {
                    inheritsFromEntity = true;
                    break;
                }
                baseType = baseType.BaseType;
            }

            // Also check if type implements IEntity interface (for cases where Entity base class might not be used)
            if (!inheritsFromEntity)
            {
                var interfaces = type.GetInterfaces();
                inheritsFromEntity = interfaces.Any(i =>
                    i.Name == "IEntity" ||
                    i.FullName == "TheTechIdea.Beep.Editor.IEntity");
            }

            if (!inheritsFromEntity) return false;

            // Check namespace filter
            if (string.IsNullOrWhiteSpace(namespaceFilter))
                return true;

            if (includeSubNamespaces)
            {
                return type.Namespace != null &&
                       (type.Namespace.Equals(namespaceFilter, StringComparison.OrdinalIgnoreCase) ||
                        type.Namespace.StartsWith(namespaceFilter + ".", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return type.Namespace != null &&
                       type.Namespace.Equals(namespaceFilter, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets all searchable assemblies from multiple sources:
        /// 1. Manually registered assemblies (RegisterAssembly / RegisterAssemblies)
        /// 2. Entry assembly and all its referenced assemblies (covers projects referenced by the exe)
        /// 3. All loaded assemblies from AppDomain.CurrentDomain
        /// 4. DMEEditor's assembly handler (plugin assemblies)
        /// This ensures entity classes from other projects loaded in the exe are always found.
        /// </summary>
        private IEnumerable<Assembly> GetSearchableAssemblies()
            => GetSearchableAssembliesWithSource().Select(pair => pair.Item1);

        /// <summary>
        /// Returns all searchable assemblies with their <see cref="AssemblySourceKind"/> provenance.
        /// Used by Phase 4 discovery evidence.
        /// </summary>
        private List<(Assembly Assembly, AssemblySourceKind Source)> GetSearchableAssembliesWithSource()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = new List<(Assembly, AssemblySourceKind)>();

            void TryAdd(Assembly asm, AssemblySourceKind kind)
            {
                if (asm == null || asm.IsDynamic) return;
                var name = asm.FullName;
                if (name != null && seen.Add(name))
                    assemblies.Add((asm, kind));
            }

            // 1. Manually registered assemblies — highest priority
            lock (_assemblyLock)
            {
                foreach (var asm in _registeredAssemblies)
                    TryAdd(asm, AssemblySourceKind.ManualRegistered);
            }

            // 2. Entry assembly + all its statically-referenced assemblies
            try
            {
                var entryAsm = Assembly.GetEntryAssembly();
                if (entryAsm != null)
                {
                    TryAdd(entryAsm, AssemblySourceKind.EntryReference);
                    foreach (var referencedName in entryAsm.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(referencedName);
                            TryAdd(refAsm, AssemblySourceKind.EntryReference);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 3. Calling assembly and its references
            try
            {
                var callingAsm = Assembly.GetCallingAssembly();
                if (callingAsm != null)
                {
                    TryAdd(callingAsm, AssemblySourceKind.CallingReference);
                    foreach (var referencedName in callingAsm.GetReferencedAssemblies())
                    {
                        try
                        {
                            var refAsm = Assembly.Load(referencedName);
                            TryAdd(refAsm, AssemblySourceKind.CallingReference);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 4. All currently loaded assemblies from AppDomain
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                    TryAdd(asm, AssemblySourceKind.AppDomainLoaded);
            }

            // 5. DMEEditor's assembly handler (plugins loaded at runtime)
            if (_editor?.assemblyHandler?.Assemblies != null)
            {
                foreach (var asmInfo in _editor.assemblyHandler.Assemblies)
                    TryAdd(asmInfo.DllLib, AssemblySourceKind.AssemblyHandlerPlugin);
            }

            return assemblies;
        }

        private static bool ShouldScanAssembly(Assembly assembly, Assembly explicitlyRequestedAssembly)
        {
            if (assembly == null || assembly.IsDynamic)
                return false;

            if (explicitlyRequestedAssembly != null)
                return true;

            var name = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(name))
                return true;

            return !IsFrameworkAssemblyName(name);
        }

        private static bool IsFrameworkAssemblyName(string assemblyName)
        {
            return assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("WindowsBase", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("Accessibility", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase);
        }

        private void LogLoaderExceptions(Assembly assembly, ReflectionTypeLoadException exception, AssemblyDiscoveryRecord record = null)
        {
            if (exception.LoaderExceptions == null || exception.LoaderExceptions.Length == 0)
                return;

            var distinctMessages = exception.LoaderExceptions
                .Where(loaderException => loaderException != null)
                .Select(loaderException => loaderException.Message)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToList();

            if (distinctMessages.Count == 0)
                return;

            // Phase 4: persist to discovery record
            record?.LoaderExceptions.AddRange(distinctMessages);

            _editor?.AddLogMessage(
                "Beep",
                $"  Loader issues while scanning '{assembly.GetName().Name}': {string.Join(" | ", distinctMessages)}",
                DateTime.Now,
                0,
                null,
                Errors.Warning);
        }
    }
}
