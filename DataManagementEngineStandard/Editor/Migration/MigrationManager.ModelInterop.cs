using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// ORM/framework-agnostic interop partial.
    /// <para>
    /// Bridges externally-supplied migration models (e.g. an EF Core <c>IModel</c>
    /// translated to a POCO at the call site, an NHibernate model, a hand-rolled
    /// dictionary, or a snapshot loaded from disk) into the existing migration
    /// plan / readiness / apply pipeline. BeepDM does not import any ORM package —
    /// the engine accepts a populated <see cref="MigrationModel"/> POCO and does the rest.
    /// </para>
    /// <para>
    /// For the canonical "EF Core" path, populate <see cref="MigrationModel"/> from
    /// <c>dbContext.Model</c> at the call site (typically a few dozen lines) and pass it
    /// to <see cref="BuildMigrationPlanForModel"/>. A future companion NuGet package
    /// (<c>TheTechIdea.Beep.DataManagementEngine.EFCore</c>) can ship that adapter as a
    /// reusable helper without forcing every BeepDM consumer to take a hard EF Core dep.
    /// </para>
    /// </summary>
    public partial class MigrationManager
    {
        // ── State captured during the most recent ORM-model plan ──
        private readonly object _modelInteropLock = new object();
        private MigrationModelEvidence _lastModelEvidence = new MigrationModelEvidence();
        // Cached EntityStructures keyed by CLR type full name. Populated by
        // BuildMigrationPlanForModel so that any downstream code that resolves
        // the same CLR type (via AppDomain reflection) picks up the ORM-shaped
        // EntityStructure instead of the classCreator's annotation-only view.
        private readonly Dictionary<string, EntityStructure> _modelInteropEntityStructures
            = new Dictionary<string, EntityStructure>(StringComparer.OrdinalIgnoreCase);

        // ───────────────────────────────────────────────────────────────────
        // IMigrationManager implementation
        // ───────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public MigrationPlanArtifact BuildMigrationPlanForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (model == null)
            {
                return CreatePlanArtifactWithError(
                    usesDiscovery: true,
                    code: "model-null",
                    message: "Migration model cannot be null.",
                    recommendation: "Populate a MigrationModel POCO from your ORM (e.g. DbContext.Model) before calling BuildMigrationPlanForModel.");
            }

            if (model.Entities == null || model.Entities.Count == 0)
            {
                return CreatePlanArtifactWithError(
                    usesDiscovery: true,
                    code: "model-entities-empty",
                    message: "Migration model contains no entities.",
                    recommendation: "Verify the DbContext.OnModelCreating flow or the source that produced the model is being executed.");
            }

            CaptureModelEvidence(model);

            // Convert the POCO model into EntityStructures. The resulting structures carry
            // the ORM-resolved table name, schema, columns (with nullability/length/precision
            // captured), keys, indexes, and foreign keys.
            var entityStructures = ConvertModelToEntityStructures(model, detectRelationships);

            // Try to resolve CLR type names back to live Type instances. If a Type resolves
            // it can be fed into the rich BuildMigrationPlanForTypes pipeline; if not we
            // fall back to a streamlined EntityStructure-driven plan that still benefits
            // from readiness, policy, dry-run, and preflight reports.
            var resolvedTypes = TryResolveClrTypes(model.Entities.Keys);

            if (resolvedTypes.Count == model.Entities.Count)
            {
                // All CLR types resolved — use the rich Type-driven pipeline but pre-populate
                // a cache so the existing TryGetEntityStructure picks up the ORM-shaped
                // EntityStructure (with table name from GetTableName(), etc.) instead of the
                // annotation-only EntityStructure the classCreator would otherwise build.
                CacheModelEntityStructures(entityStructures);

                try
                {
                    // Thread the apply flags through so the plan emits AddForeignKey /
                    // CreateIndex operations alongside the entity ops. This is the
                    // rich-Type-driven branch; the structures used by BuildPlanOperation
                    // come from the same model-interop cache, so the FK/Index metadata
                    // picked up here is the ORM-shaped version.
                    var plan = BuildMigrationPlanForTypes(resolvedTypes, detectRelationships, applyForeignKeys, applyIndexes);
                    AnnotatePlanWithModelProvenance(plan, model);
                    return plan;
                }
                finally
                {
                    // The cache is deliberately NOT cleared here.
                    //
                    // During execution the executor calls TryGetEntityStructure for
                    // each entity, and for FK/Index operations it reads Relations
                    // and Indexes from the returned structure.  If the cache were
                    // cleared, the executor would get the classCreator (annotation-
                    // only) view, which is missing the FKs and indexes the ORM
                    // model contributed — causing a silent no-op for every
                    // AddForeignKey / CreateIndex step.
                    //
                    // The cache keys are CLR type full names, and the ORM-shaped
                    // structure is a superset of the classCreator view (it carries
                    // the same fields and table name, plus Relations / Indexes).
                    // A subsequent explicit-type or discovery plan for the same type
                    // therefore sees a richer structure, not a corrupted one.
                }
            }

            // Fallback path: at least one CLR type was not resolvable. Build a streamlined
            // plan from the EntityStructures alone. This still produces a valid
            // MigrationPlanArtifact with operations, readiness, and downstream reports,
            // but skips the few features that require live Type access (e.g. assembly name).
            return BuildMigrationPlanFromEntityStructures(
                entityStructures,
                model,
                unresolvedTypeCount: model.Entities.Count - resolvedTypes.Count,
                detectRelationships: detectRelationships,
                applyForeignKeys: applyForeignKeys,
                applyIndexes: applyIndexes);
        }

        /// <inheritdoc/>
        public MigrationPlanArtifact BuildMigrationPlanForTypesAnnotated(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (entityTypes == null)
                return BuildMigrationPlanForTypes(Array.Empty<Type>(), detectRelationships, applyForeignKeys, applyIndexes);

            var materialized = entityTypes.Where(t => t != null).Distinct().ToList();
            return BuildMigrationPlanForTypes(materialized, detectRelationships, applyForeignKeys, applyIndexes);
        }

        /// <inheritdoc/>
        public IErrorsInfo EnsureDatabaseCreatedForModel(MigrationModel model, bool detectRelationships = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (model == null)
                return CreateErrorsInfo(Errors.Failed, "Migration model cannot be null.");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set.");

            var structures = ConvertModelToEntityStructures(model, detectRelationships);
            // Topologically order so principal tables exist before dependents.
            var ordered = TopologicallyOrderByForeignKeys(structures, out _);
            var aggregate = CreateErrorsInfo(Errors.Ok, $"EnsureDatabaseCreatedForModel: processed {ordered.Count} entity/entities from {model.Source} model.");
            var firstError = (IErrorsInfo)null;

            for (var i = 0; i < ordered.Count; i++)
            {
                var entity = ordered[i];
                progress?.Report(new PassedArgs { ParameterString1 = $"Ensuring '{entity.EntityName}'", Messege = $"EnsureDatabaseCreatedForModel: {i + 1}/{ordered.Count}" });
                var result = EnsureEntity(entity, createIfMissing: true, addMissingColumns: false, applyForeignKeys: detectRelationships && applyForeignKeys, applyIndexes: detectRelationships && applyIndexes);
                if (result.Flag == Errors.Failed)
                {
                    if (firstError == null)
                        firstError = result;
                    aggregate.Flag = Errors.Failed;
                    aggregate.Message = $"EnsureDatabaseCreatedForModel: first failure on '{entity.EntityName}' — {result.Message}";
                }
            }

            TrackMigration("EnsureDatabaseCreatedForModel", model.SourceId ?? model.Source, null, $"entities={ordered.Count}", aggregate);
            return firstError ?? aggregate;
        }

        /// <inheritdoc/>
        public IErrorsInfo ApplyMigrationsForModel(MigrationModel model, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (model == null)
                return CreateErrorsInfo(Errors.Failed, "Migration model cannot be null.");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set.");

            var structures = ConvertModelToEntityStructures(model, detectRelationships);

            // Topologically order the entities by FK dependency so that principal
            // tables exist before dependent tables. Cycles fall back to the source
            // order; a warning is recorded in the aggregate.
            var ordered = TopologicallyOrderByForeignKeys(structures, out var cycleWarning);
            if (!string.IsNullOrEmpty(cycleWarning))
            {
                progress?.Report(new PassedArgs { ParameterString1 = "FK cycle detected", Messege = cycleWarning });
            }

            var aggregate = CreateErrorsInfo(Errors.Ok, $"ApplyMigrationsForModel: processed {ordered.Count} entity/entities from {model.Source} model.");
            var firstError = (IErrorsInfo)null;

            for (var i = 0; i < ordered.Count; i++)
            {
                var entity = ordered[i];
                progress?.Report(new PassedArgs { ParameterString1 = $"Applying '{entity.EntityName}'", Messege = $"ApplyMigrationsForModel: {i + 1}/{ordered.Count}" });
                var result = EnsureEntity(entity, createIfMissing: true, addMissingColumns: addMissingColumns, applyForeignKeys: detectRelationships && applyForeignKeys, applyIndexes: detectRelationships && applyIndexes);
                if (result.Flag == Errors.Failed)
                {
                    if (firstError == null)
                        firstError = result;
                    aggregate.Flag = Errors.Failed;
                    aggregate.Message = $"ApplyMigrationsForModel: first failure on '{entity.EntityName}' — {result.Message}";
                }
            }

            TrackMigration("ApplyMigrationsForModel", model.SourceId ?? model.Source, null, $"entities={ordered.Count}", aggregate);
            return firstError ?? aggregate;
        }

        /// <inheritdoc/>
        public MigrationReadinessReport GetMigrationReadinessForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (model == null)
            {
                var empty = CreateBaseReadinessReport(usesDiscovery: true);
                empty.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "model-null",
                    Severity = MigrationIssueSeverity.Error,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Migration model cannot be null.",
                    Recommendation = "Populate a MigrationModel POCO from your ORM (e.g. DbContext.Model) before calling GetMigrationReadinessForModel."
                });
                ApplyIntentFlags(empty, applyForeignKeys, applyIndexes, detectRelationships);
                return empty;
            }

            var resolvedTypes = TryResolveClrTypes(model.Entities.Keys);
            MigrationReadinessReport report;
            if (resolvedTypes.Count == model.Entities.Count)
            {
                report = GetMigrationReadinessForTypes(resolvedTypes, detectRelationships, applyForeignKeys, applyIndexes);
            }
            else
            {
                // Fallback: build a synthetic readiness from the model directly.
                report = BuildReadinessReportFromEntityStructures(
                    ConvertModelToEntityStructures(model, detectRelationships),
                    model,
                    unresolvedTypeCount: model.Entities.Count - resolvedTypes.Count);
            }

            ApplyIntentFlags(report, applyForeignKeys, applyIndexes, detectRelationships);
            return report;
        }

        /// <inheritdoc/>
        public MigrationSummary GetMigrationSummaryForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (model == null)
            {
                var empty = new MigrationSummary
                {
                    DataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                    DataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE
                };
                empty.Diagnostics.Add($"applyForeignKeys={applyForeignKeys}, applyIndexes={applyIndexes}");
                return empty;
            }

            var structures = ConvertModelToEntityStructures(model, detectRelationships);
            var summary = new MigrationSummary
            {
                DataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                DataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE
            };
            if (applyForeignKeys) summary.Diagnostics.Add("applyForeignKeys=true");
            if (applyIndexes) summary.Diagnostics.Add("applyIndexes=true");

            foreach (var entity in structures)
            {
                var decision = new EntityDecisionRecord
                {
                    EntityName = entity.EntityName,
                    Source = EntityMigrationSource.DiscoveryEFCoreModel
                };

                if (MigrateDataSource == null || string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    decision.Decision = EntityMigrationDecision.Error;
                    decision.DecisionReasonCode = "ENTITY-NO-DATASOURCE";
                    summary.EntityDecisions.Add(decision);
                    summary.Errors.Add($"{entity.EntityName}: no migration datasource is configured.");
                    continue;
                }

                try
                {
                    var exists = MigrateDataSource.CheckEntityExist(entity.EntityName);
                    if (!exists)
                    {
                        decision.Decision = EntityMigrationDecision.Create;
                        decision.DecisionReasonCode = "ENTITY-CREATED";
                        summary.EntitiesToCreate.Add(entity.EntityName);
                    }
                    else
                    {
                        var current = MigrateDataSource.GetEntityStructure(entity.EntityName, true);
                        var missing = GetMissingColumns(current, entity);
                        if (missing.Count > 0)
                        {
                            decision.Decision = EntityMigrationDecision.Update;
                            decision.DecisionReasonCode = "COLUMNS-ADDED";
                            summary.EntitiesToUpdate.Add(entity.EntityName);
                        }
                        else
                        {
                            decision.Decision = EntityMigrationDecision.NoChange;
                            decision.DecisionReasonCode = "ENTITY-UP-TO-DATE";
                            summary.EntitiesUpToDate.Add(entity.EntityName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    decision.Decision = EntityMigrationDecision.Error;
                    decision.DecisionReasonCode = "ENTITY-ERROR";
                    summary.Errors.Add($"{entity.EntityName}: {ex.Message}");
                }

                summary.EntityDecisions.Add(decision);
            }

            return summary;
        }

        /// <inheritdoc/>
        public MigrationModelEvidence GetMigrationModelEvidence()
        {
            lock (_modelInteropLock)
            {
                // Defensive copy so callers cannot mutate the live state.
                return new MigrationModelEvidence
                {
                    EntityTypeCount = _lastModelEvidence.EntityTypeCount,
                    KeylessTypeCount = _lastModelEvidence.KeylessTypeCount,
                    ForeignKeyCount = _lastModelEvidence.ForeignKeyCount,
                    IndexCount = _lastModelEvidence.IndexCount,
                    Source = _lastModelEvidence.Source,
                    SourceVersion = _lastModelEvidence.SourceVersion,
                    SourceId = _lastModelEvidence.SourceId,
                    ModelHash = _lastModelEvidence.ModelHash,
                    Diagnostics = new List<string>(_lastModelEvidence.Diagnostics),
                    Entities = _lastModelEvidence.Entities.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // POCO → EntityStructure adapter
        // ───────────────────────────────────────────────────────────────────

        private List<EntityStructure> ConvertModelToEntityStructures(MigrationModel model, bool detectRelationships = true)
        {
            var result = new List<EntityStructure>(model.Entities.Count);
            foreach (var entity in model.Entities.Values)
            {
                result.Add(BuildEntityStructureFromModel(entity, detectRelationships));
            }
            CacheModelEntityStructures(result);
            return result;
        }

        private static EntityStructure BuildEntityStructureFromModel(MigrationModelEntity modelEntity, bool detectRelationships = true)
        {
            var structure = new EntityStructure
            {
                EntityName = string.IsNullOrWhiteSpace(modelEntity.TableName) ? modelEntity.ClrTypeFullName : modelEntity.TableName,
                DatasourceEntityName = modelEntity.TableName ?? string.Empty,
                SchemaOrOwnerOrDatabase = modelEntity.Schema ?? string.Empty,
                HasDataAnnotations = true,
                EntityType = modelEntity.IsKeyless ? EntityType.View : EntityType.Entity,
                Caption = modelEntity.ClrTypeFullName,
                Description = $"Supplied via MigrationModel (ClrType={modelEntity.ClrTypeFullName})"
            };

            structure.Fields = new List<EntityField>(modelEntity.Properties.Count);
            structure.PrimaryKeys = new List<EntityField>();
            structure.Relations = new List<RelationShipKeys>();
            // EntityStructure's indexer initializes Indexes in init(); defensively
            // re-init in case a caller has constructed a bare EntityStructure.
            structure.Indexes = new List<EntityIndex>();

            foreach (var property in modelEntity.Properties)
            {
                var field = new EntityField
                {
                    FieldName = string.IsNullOrWhiteSpace(property.ColumnName) ? property.PropertyName : property.ColumnName,
                    Originalfieldname = property.PropertyName,
                    Fieldtype = string.IsNullOrWhiteSpace(property.FieldType) ? typeof(string).FullName : property.FieldType,
                    Size = property.MaxLength ?? 0,
                    NumericPrecision = (short)(property.Precision ?? 0),
                    NumericScale = (short)(property.Scale ?? 0),
                    IsRequired = !property.IsNullable,
                    AllowDBNull = property.IsNullable,
                    IsKey = property.IsPrimaryKey,
                    IsAutoIncrement = property.IsIdentity,
                    IsIdentity = property.IsIdentity,
                    IsUnique = property.IsUnique,
                    IsIndexed = property.IsIndexed || property.IsUnique || property.IsPrimaryKey,
                    // Surface the new column-mapped fields so that downstream column
                    // generation (e.g. RdbmsHelper.GenerateAddColumnSql) can honor
                    // row-version, computed, and default-value expressions.
                    IsRowVersion = property.IsRowVersion,
                    DefaultValue = property.DefaultValueSql ?? string.Empty,
                    Expression = property.ComputedColumnSql ?? string.Empty,
                    FieldCategory = property.FieldCategoryHint == default
                        ? ResolveFieldCategory(property.FieldType)
                        : property.FieldCategoryHint
                };
                structure.Fields.Add(field);
                if (field.IsKey)
                    structure.PrimaryKeys.Add(field);
            }

            // Indexes: populate EntityStructure.Indexes so the migration manager
            // can apply them when callers opt into applyIndexes=true.
            if (detectRelationships && modelEntity.Indexes != null)
            {
                foreach (var idx in modelEntity.Indexes)
                {
                    if (idx == null || idx.Columns == null || idx.Columns.Count == 0)
                        continue;

                    var indexEntry = new EntityIndex
                    {
                        Name = string.IsNullOrWhiteSpace(idx.Name)
                            ? $"IX_{structure.EntityName}_{string.Join("_", idx.Columns)}"
                            : idx.Name,
                        EntityName = structure.EntityName,
                        Columns = new List<string>(idx.Columns),
                        IsUnique = idx.IsUnique
                    };

                    if (idx.IsUnique)
                        indexEntry.Options["UNIQUE"] = true;

                    structure.Indexes.Add(indexEntry);
                }
            }

            if (!detectRelationships)
                return structure;

            foreach (var fk in modelEntity.ForeignKeys)
            {
                // A multi-column FK generates one RelationShipKeys per column to
                // keep the existing single-column relation contract. The OnDelete
                // and OnUpdate behavior attach to the first column's relation
                // and to the constraint name on every column.
                for (var i = 0; i < fk.Columns.Count; i++)
                {
                    var column = fk.Columns[i];
                    var principalColumn = i < fk.PrincipalColumns.Count
                        ? fk.PrincipalColumns[i]
                        : fk.PrincipalColumns.FirstOrDefault() ?? string.Empty;

                    var rel = new RelationShipKeys(
                        pParentEntityID: string.IsNullOrWhiteSpace(fk.PrincipalSchema)
                            ? fk.PrincipalTable
                            : $"{fk.PrincipalSchema}.{fk.PrincipalTable}",
                        pParentEntityColumnID: principalColumn,
                        pEntityColumnID: column)
                    {
                        RalationName = fk.ConstraintName
                    };

                    // Only the first relation in a multi-column FK carries the
                    // behavior metadata; downstream DDL generation groups by
                    // constraint name to compose multi-column FKs.
                    if (i == 0)
                    {
                        rel.OnDeleteBehavior = fk.OnDeleteBehavior ?? string.Empty;
                        rel.OnUpdateBehavior = fk.OnUpdateBehavior ?? string.Empty;
                    }

                    structure.Relations.Add(rel);
                }
            }

            return structure;
        }

        private static DbFieldCategory ResolveFieldCategory(string fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
                return DbFieldCategory.String;
            switch (fieldType)
            {
                case "System.String": return DbFieldCategory.String;
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64": return DbFieldCategory.Numeric;
                case "System.Single":
                case "System.Double":
                case "System.Decimal": return DbFieldCategory.Decimal;
                case "System.Boolean": return DbFieldCategory.Boolean;
                case "System.DateTime":
                case "System.DateTimeOffset": return DbFieldCategory.DateTime;
                case "System.Guid": return DbFieldCategory.Guid;
                case "System.Byte[]": return DbFieldCategory.Binary;
                default: return DbFieldCategory.Complex;
            }
        }

        /// <summary>
        /// Topologically orders the supplied entity structures so that every
        /// principal table appears before any dependent table that references it
        /// via a foreign key. When a cycle is detected, falls back to source order
        /// and returns a non-empty warning describing the cycle.
        /// </summary>
        private static List<EntityStructure> TopologicallyOrderByForeignKeys(
            List<EntityStructure> structures,
            out string cycleWarning)
        {
            cycleWarning = string.Empty;
            if (structures == null || structures.Count <= 1)
                return structures != null ? new List<EntityStructure>(structures) : new List<EntityStructure>();

            // Build principal → dependents adjacency and in-degree map.
            var nameSet = new HashSet<string>(
                structures.Select(s => s.EntityName).Where(n => !string.IsNullOrWhiteSpace(n)),
                StringComparer.OrdinalIgnoreCase);

            // Map principal table name (case-insensitive) to its index in the
            // structure list, so we can resolve FKs that point at principal
            // tables outside the supplied set.
            var principalIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < structures.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(structures[i].EntityName))
                    principalIndex[structures[i].EntityName] = i;
            }

            // dependents[i] = list of structure indices that depend on i
            var dependents = new List<int>[structures.Count];
            for (var i = 0; i < structures.Count; i++) dependents[i] = new List<int>();

            var inDegree = new int[structures.Count];
            var edgeCount = 0;

            for (var i = 0; i < structures.Count; i++)
            {
                var s = structures[i];
                if (s?.Relations == null) continue;

                foreach (var rel in s.Relations)
                {
                    if (rel == null) continue;
                    var principal = rel.RelatedEntityID;
                    if (string.IsNullOrWhiteSpace(principal)) continue;
                    if (principalIndex.TryGetValue(principal, out var principalIdx) && principalIdx != i)
                    {
                        if (!dependents[principalIdx].Contains(i))
                        {
                            dependents[principalIdx].Add(i);
                            inDegree[i]++;
                            edgeCount++;
                        }
                    }
                    else if (!nameSet.Contains(principal))
                    {
                        // FK references a table outside the supplied set; nothing to
                        // do in topo sort, but it is not a cycle condition.
                    }
                }
            }

            // Kahn's algorithm.
            var ready = new Queue<int>();
            for (var i = 0; i < structures.Count; i++)
                if (inDegree[i] == 0) ready.Enqueue(i);

            var ordered = new List<EntityStructure>(structures.Count);
            while (ready.Count > 0)
            {
                var idx = ready.Dequeue();
                ordered.Add(structures[idx]);
                foreach (var dep in dependents[idx])
                {
                    if (--inDegree[dep] == 0) ready.Enqueue(dep);
                }
            }

            if (ordered.Count == structures.Count) return ordered;

            // Cycle detected. Fall back to source order and surface a warning.
            cycleWarning = $"Foreign-key cycle detected among {structures.Count - ordered.Count} entit(ies); " +
                           "applying in source order. Resolve the cycle by either removing circular FKs or " +
                           "creating the principal tables in a prior pass.";
            return new List<EntityStructure>(structures);
        }

        // ───────────────────────────────────────────────────────────────────
        // Type resolution across registered + AppDomain assemblies
        // ───────────────────────────────────────────────────────────────────

        private List<Type> TryResolveClrTypes(IEnumerable<string> clrTypeFullNames)
        {
            var resolved = new List<Type>();
            var considered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First pass: registered assemblies (manual + auto-discovered)
            foreach (var assembly in GetRegisteredAssemblies())
            {
                if (assembly == null) continue;
                Type[] exported;
                try { exported = assembly.GetExportedTypes(); }
                catch { continue; }
                foreach (var type in exported)
                {
                    if (type == null || string.IsNullOrWhiteSpace(type.FullName)) continue;
                    if (clrTypeFullNames.Contains(type.FullName) && considered.Add(type.FullName))
                        resolved.Add(type);
                }
                if (resolved.Count == clrTypeFullNames.Count())
                    return resolved;
            }

            // Second pass: AppDomain (catches everything else the user has loaded)
            if (clrTypeFullNames.Any())
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly == null) continue;
                    if (assembly.IsDynamic) continue;
                    Type[] exported;
                    try { exported = assembly.GetExportedTypes(); }
                    catch { continue; }
                    foreach (var type in exported)
                    {
                        if (type == null || string.IsNullOrWhiteSpace(type.FullName)) continue;
                        if (clrTypeFullNames.Contains(type.FullName) && considered.Add(type.FullName))
                            resolved.Add(type);
                    }
                    if (resolved.Count == clrTypeFullNames.Count())
                        return resolved;
                }
            }

            return resolved;
        }

        // ───────────────────────────────────────────────────────────────────
        // Evidence capture + provenance annotation
        // ───────────────────────────────────────────────────────────────────

        private void CaptureModelEvidence(MigrationModel model)
        {
            var evidence = new MigrationModelEvidence
            {
                Source = model.Source ?? "Manual",
                SourceVersion = model.SourceVersion ?? string.Empty,
                SourceId = model.SourceId ?? string.Empty
            };
            evidence.EntityTypeCount = model.Entities.Count;
            evidence.KeylessTypeCount = model.Entities.Count(kv => kv.Value.IsKeyless);
            evidence.ForeignKeyCount = model.Entities.Sum(kv => kv.Value.ForeignKeys?.Count ?? 0);
            evidence.IndexCount = model.Entities.Sum(kv => kv.Value.Indexes?.Count ?? 0);

            var hashInput = new StringBuilder();
            hashInput.Append(evidence.Source).Append('|')
                    .Append(evidence.SourceVersion).Append('|')
                    .Append(evidence.SourceId).Append('|');

            foreach (var kv in model.Entities.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                var entity = kv.Value;
                var record = new MigrationModelEntityRecord
                {
                    ClrTypeFullName = entity.ClrTypeFullName,
                    TableName = entity.TableName,
                    Schema = entity.Schema,
                    PropertyNames = entity.Properties.Select(p => p.PropertyName).ToList(),
                    PrimaryKey = entity.Properties.Where(p => p.IsPrimaryKey).Select(p => p.PropertyName).ToList(),
                    Indexes = new List<MigrationModelIndex>(entity.Indexes ?? new List<MigrationModelIndex>()),
                    ForeignKeys = new List<MigrationModelForeignKey>(entity.ForeignKeys ?? new List<MigrationModelForeignKey>())
                };
                record.RecordHash = ComputeEntityRecordHash(record);
                if (record.PrimaryKey.Count == 0 && !entity.IsKeyless)
                    record.Warnings.Add("No primary key declared in supplied model.");
                if (string.IsNullOrWhiteSpace(entity.TableName))
                    record.Warnings.Add("No table name supplied; migration will use the CLR type name as the table.");

                evidence.Entities[entity.ClrTypeFullName] = record;

                hashInput.Append("E:").Append(entity.ClrTypeFullName)
                         .Append('|').Append(entity.TableName)
                         .Append('|').Append(entity.Schema).Append('|');
                foreach (var prop in entity.Properties)
                {
                    hashInput.Append("P:").Append(prop.PropertyName)
                             .Append(':').Append(prop.ColumnName)
                             .Append(':').Append(prop.FieldType)
                             .Append(':').Append(prop.IsNullable)
                             .Append(':').Append(prop.MaxLength?.ToString() ?? string.Empty)
                             .Append(':').Append(prop.Precision?.ToString() ?? string.Empty)
                             .Append(':').Append(prop.Scale?.ToString() ?? string.Empty)
                             .Append(':').Append(prop.IsPrimaryKey)
                             .Append(':').Append(prop.IsIdentity)
                             .Append(':').Append(prop.IsRowVersion)
                             .Append(':').Append(prop.IsUnique)
                             .Append('|');
                }
            }

            if (evidence.ForeignKeyCount > 0)
                evidence.Diagnostics.Add($"Model exposes {evidence.ForeignKeyCount} foreign key(s).");
            if (evidence.KeylessTypeCount > 0)
                evidence.Diagnostics.Add($"Model contains {evidence.KeylessTypeCount} keyless/query type(s) that are typically not migrated.");
            if (string.IsNullOrEmpty(model.SourceVersion))
                evidence.Diagnostics.Add("Migration model did not declare a SourceVersion; downstream consumers cannot pin ORM features.");

            using var sha = SHA256.Create();
            evidence.ModelHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(hashInput.ToString()))).Replace("-", string.Empty);

            lock (_modelInteropLock)
            {
                _lastModelEvidence = evidence;
            }
        }

        private static string ComputeEntityRecordHash(MigrationModelEntityRecord record)
        {
            var input = new StringBuilder();
            input.Append(record.ClrTypeFullName).Append('|')
                 .Append(record.TableName).Append('|')
                 .Append(record.Schema).Append('|');
            foreach (var p in record.PropertyNames)
                input.Append("P:").Append(p).Append('|');
            foreach (var pk in record.PrimaryKey)
                input.Append("K:").Append(pk).Append('|');
            foreach (var idx in record.Indexes)
                input.Append("I:").Append(idx.Name).Append(':').Append(idx.IsUnique).Append(':').Append(string.Join(",", idx.Columns)).Append('|');
            foreach (var fk in record.ForeignKeys)
                input.Append("F:").Append(fk.ConstraintName).Append(':').Append(string.Join(",", fk.Columns)).Append("->")
                     .Append(fk.PrincipalSchema).Append('.').Append(fk.PrincipalTable)
                     .Append('(').Append(string.Join(",", fk.PrincipalColumns)).Append(')').Append('|');
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(input.ToString()))).Replace("-", string.Empty);
        }

        /// <summary>
        /// Returns true when the supplied type's <see cref="EntityStructure"/>
        /// representation was primed by <see cref="BuildMigrationPlanForModel"/>
        /// (i.e. it is an ORM-model-sourced entity, not a classCreator-only entity).
        /// </summary>
        internal bool IsModelInteropSourcedEntity(Type entityType)
        {
            if (entityType == null) return false;
            try
            {
                lock (_modelInteropLock)
                {
                    return _modelInteropEntityStructures.ContainsKey(entityType.FullName ?? entityType.Name);
                }
            }
            catch
            {
                return false;
            }
        }

        private void AnnotatePlanWithModelProvenance(MigrationPlanArtifact plan, MigrationModel model)
        {
            if (plan == null || model == null) return;

            // Patch the EntityMigrationSource tag on every operation that came from the
            // supplied model. Operations whose EntityName matches a supplied table are
            // re-tagged; everything else is left untouched.
            var suppliedTables = new HashSet<string>(
                model.Entities.Values.Select(e => string.IsNullOrWhiteSpace(e.TableName) ? e.ClrTypeFullName : e.TableName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var op in plan.Operations)
            {
                if (op == null) continue;
                if (suppliedTables.Contains(op.EntityName))
                    op.Note = AppendProvenanceNote(op.Note, $"source={model.Source}");
            }
        }

        private void CacheModelEntityStructures(IEnumerable<EntityStructure> structures)
        {
            if (structures == null)
                return;

            lock (_modelInteropLock)
            {
                foreach (var structure in structures)
                {
                    if (structure == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(structure.Caption))
                        _modelInteropEntityStructures[structure.Caption] = structure;

                    if (!string.IsNullOrWhiteSpace(structure.EntityName))
                        _modelInteropEntityStructures[structure.EntityName] = structure;
                }
            }
        }

        private EntityStructure ResolveCachedEntityStructure(string entityTypeName, string entityName)
        {
            lock (_modelInteropLock)
            {
                if (!string.IsNullOrWhiteSpace(entityTypeName) &&
                    _modelInteropEntityStructures.TryGetValue(entityTypeName, out var byType))
                {
                    return byType;
                }

                if (!string.IsNullOrWhiteSpace(entityName) &&
                    _modelInteropEntityStructures.TryGetValue(entityName, out var byEntity))
                {
                    return byEntity;
                }
            }

            return null;
        }

        private static string AppendProvenanceNote(string existing, string addition)
        {
            if (string.IsNullOrWhiteSpace(existing)) return addition;
            if (existing.Contains(addition, StringComparison.OrdinalIgnoreCase)) return existing;
            return existing + "; " + addition;
        }

        // ───────────────────────────────────────────────────────────────────
        // Fallback pipeline (no live CLR types available)
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Emits one <see cref="MigrationPlanOperationKind.AddForeignKey"/> op per
        /// <see cref="RelationShipKeys"/> on the supplied entity, plus one
        /// <see cref="MigrationPlanOperationKind.CreateIndex"/> op per
        /// <see cref="EntityIndex"/>. The emitted ops carry their own
        /// <see cref="MigrationPlanOperation.MissingColumns"/> slots to thread
        /// the relation columns / index columns through the execution pipeline
        /// without redefining the operation data shape, and a
        /// <see cref="MigrationPlanOperation.TargetName"/> carrying the constraint
        /// / index name so the execution step can run the matching drop or add DDL
        /// without re-synthesizing the name.
        /// </summary>
        private void EmitRelationalArtifactsForEntity(EntityStructure entity, List<MigrationPlanOperation> sink, bool applyForeignKeys, bool applyIndexes)
        {
            if (entity == null || sink == null) return;

            if (applyForeignKeys && entity.Relations != null && entity.Relations.Count > 0)
            {
                foreach (var fk in BuildForeignKeyDefinitions(entity))
                {
                    sink.Add(new MigrationPlanOperation
                    {
                        EntityName = entity.EntityName,
                        EntityTypeName = entity.Caption ?? entity.EntityName,
                        Kind = MigrationPlanOperationKind.AddForeignKey,
                        RiskLevel = MigrationPlanRiskLevel.Low,
                        Note = $"FK '{fk.ConstraintName}' {entity.EntityName}({string.Join(",", fk.ColumnNames)}) -> {fk.ReferencedEntityName}({string.Join(",", fk.ReferencedColumnNames)}) (OnDelete={fk.OnDeleteBehavior}, OnUpdate={fk.OnUpdateBehavior})",
                        MissingColumns = new List<string>(fk.ColumnNames),
                        TargetName = fk.ConstraintName,
                        IsDestructive = false
                    });
                }
            }

            if (applyIndexes && entity.Indexes != null && entity.Indexes.Count > 0)
            {
                foreach (var idx in entity.Indexes)
                {
                    if (idx == null || idx.Columns == null || idx.Columns.Count == 0) continue;

                    var name = string.IsNullOrWhiteSpace(idx.Name)
                        ? $"IX_{entity.EntityName}_{string.Join("_", idx.Columns)}"
                        : idx.Name;

                    sink.Add(new MigrationPlanOperation
                    {
                        EntityName = entity.EntityName,
                        EntityTypeName = entity.Caption ?? entity.EntityName,
                        Kind = MigrationPlanOperationKind.CreateIndex,
                        RiskLevel = MigrationPlanRiskLevel.Low,
                        Note = $"Index '{name}' on {entity.EntityName}({string.Join(",", idx.Columns)}) unique={idx.IsUnique}",
                        MissingColumns = new List<string>(idx.Columns),
                        TargetName = name,
                        IsDestructive = false
                    });
                }
            }
        }

        private MigrationPlanArtifact BuildMigrationPlanFromEntityStructures(
            List<EntityStructure> structures,
            MigrationModel model,
            int unresolvedTypeCount,
            bool detectRelationships = true,
            bool applyForeignKeys = false,
            bool applyIndexes = false)
        {
            var plan = CreateBasePlanArtifact(usesDiscovery: true);
            plan.EntityTypeCount = structures.Count;
            var effectiveApplyForeignKeys = detectRelationships && applyForeignKeys;
            var effectiveApplyIndexes = detectRelationships && applyIndexes;
            var readiness = BuildReadinessReportFromEntityStructures(structures, model, unresolvedTypeCount);
            plan.ReadinessIssues.AddRange(readiness.Issues);
            plan.ProviderAssumptions = readiness.MigrationBestPractices
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            plan.ProviderCapabilities = BuildProviderCapabilityProfile(plan.DataSourceType, plan.DataSourceCategory);
            if (!string.IsNullOrWhiteSpace(plan.ProviderCapabilities.PortabilityWarning))
            {
                plan.ReadinessIssues.Add(new MigrationReadinessIssue
                {
                    Code = "provider-portability-warning",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = plan.ProviderCapabilities.PortabilityWarning,
                    Recommendation = "Review provider-specific capability constraints and fallback tasks before execution."
                });
            }

            foreach (var entity in structures)
            {
                var op = new MigrationPlanOperation
                {
                    EntityTypeName = entity.Caption ?? entity.EntityName,
                    EntityName = entity.EntityName,
                    Kind = MigrationPlanOperationKind.Error,
                    RiskLevel = MigrationPlanRiskLevel.Medium,
                    Note = "Plan generated from MigrationModel (no live CLR Type resolved)."
                };

                try
                {
                    if (MigrateDataSource != null)
                    {
                        var exists = MigrateDataSource.CheckEntityExist(entity.EntityName);
                        if (!exists)
                        {
                            op.Kind = MigrationPlanOperationKind.CreateEntity;
                            op.RiskLevel = MigrationPlanRiskLevel.Medium;
                            op.Note = $"Entity '{entity.EntityName}' does not exist and will be created (from {model.Source} model).";
                        }
                        else
                        {
                            var current = MigrateDataSource.GetEntityStructure(entity.EntityName, true);
                            var missing = GetMissingColumns(current, entity);
                            if (missing.Count > 0)
                            {
                                op.Kind = MigrationPlanOperationKind.AddMissingColumns;
                                op.MissingColumns = missing.Select(m => m.FieldName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                op.RiskLevel = MigrationPlanRiskLevel.Medium;
                                op.Note = $"Entity '{entity.EntityName}' requires {op.MissingColumns.Count} missing column(s) (from {model.Source} model).";
                            }
                            else
                            {
                                op.Kind = MigrationPlanOperationKind.UpToDate;
                                op.RiskLevel = MigrationPlanRiskLevel.Low;
                                op.Note = $"Entity '{entity.EntityName}' is up to date (compared with {model.Source} model).";
                            }
                        }
                    }
                    else
                    {
                        op.Kind = MigrationPlanOperationKind.None;
                        op.Note = "Migration datasource is not set; cannot compare to current state.";
                    }
                }
                catch (Exception ex)
                {
                    op.Kind = MigrationPlanOperationKind.Error;
                    op.RiskLevel = MigrationPlanRiskLevel.Critical;
                    op.Note = $"Comparison failed for '{entity.EntityName}': {ex.Message}";
                }

                plan.Operations.Add(op);

                // Emit FK and Index plan operations alongside the entity op so
                // the dry-run previews them, the policy lints them, and the
                // execution orchestrator can apply them when applyForeignKeys /
                // applyIndexes is true. They are informational — the apply path
                // also re-derives the same artifacts from the structures.
                EmitRelationalArtifactsForEntity(entity, plan.Operations, effectiveApplyForeignKeys, effectiveApplyIndexes);
            }

            if (effectiveApplyForeignKeys)
                EnsureCreateEntityBeforeForeignKey(plan.Operations);

            plan.PolicyEvaluation = EvaluateMigrationPlanPolicy(plan, CreateDefaultPolicyOptions());
            plan.PlanHash = ComputePlanHash(plan);
            plan.ImpactReport = BuildImpactReport(plan);
            plan.DryRunReport = GenerateDryRunReport(plan);
            plan.PreflightReport = new MigrationPreflightReport
            {
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                CheckedOnUtc = DateTime.UtcNow,
                CanApply = false
            };
            plan.CompensationPlan = BuildCompensationPlan(plan);
            plan.RollbackReadinessReport = CheckRollbackReadiness(
                plan,
                backupConfirmed: false,
                restoreTestEvidenceProvided: false,
                restoreTestEvidence: "Not provided at planning time.");
            plan.PerformancePlan = BuildPerformancePlan(plan);
            plan.CiValidationReport = ValidatePlanForCi(plan);
            plan.RolloutGovernanceReport = EvaluateRolloutGovernance(plan);
            plan.ExecutionCheckpoint = CreateExecutionCheckpoint(plan);

            RecordPlanCreated(plan);
            TryTrackMigrationPlan(plan, nameof(BuildMigrationPlanForModel));
            return plan;
        }

        /// <summary>
        /// Stamps the apply-foreign-keys / apply-indexes intent on a readiness
        /// report so consumers can audit the caller's intent. Uses the Issues
        /// list at Info severity (which is informational, not a finding).
        /// </summary>
        internal static void ApplyIntentFlags(MigrationReadinessReport report, bool applyForeignKeys, bool applyIndexes, bool detectRelationships = true)
        {
            if (report == null) return;
            if (!detectRelationships && (applyForeignKeys || applyIndexes))
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "intent-relationships-disabled",
                    Severity = MigrationIssueSeverity.Info,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Caller disabled relationship detection; foreign-key and index apply flags will not emit relational DDL.",
                    Recommendation = "Set detectRelationships=true when FK/index planning or application is required."
                });
                return;
            }

            if (applyForeignKeys)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "intent-apply-foreign-keys",
                    Severity = MigrationIssueSeverity.Info,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Caller requested applyForeignKeys=true; FKs from the model will be applied during ApplyMigrations.",
                    Recommendation = "Verify the migration datasource supports the FK behavior vocabulary used by the model."
                });
            }
            if (applyIndexes)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "intent-apply-indexes",
                    Severity = MigrationIssueSeverity.Info,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Caller requested applyIndexes=true; indexes from the model will be applied during ApplyMigrations.",
                    Recommendation = "Confirm large-table index creation windows before applying in production."
                });
            }
        }

        private MigrationReadinessReport BuildReadinessReportFromEntityStructures(
            List<EntityStructure> structures,
            MigrationModel model,
            int unresolvedTypeCount)
        {
            var report = CreateBaseReadinessReport(usesDiscovery: true);
            report.EntityTypeCount = structures.Count;

            if (unresolvedTypeCount > 0)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "model-clrtype-unresolved",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = $"{unresolvedTypeCount} CLR type name(s) could not be resolved; readiness analysis is partial.",
                    Recommendation = "Register the relevant assemblies via MigrationManager.RegisterAssembly or call GetMigrationReadinessForTypes with the live Type objects."
                });
            }

            foreach (var entity in structures)
            {
                if (entity == null) continue;
                if (string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    report.Issues.Add(new MigrationReadinessIssue
                    {
                        Code = "entity-name-missing",
                        Severity = MigrationIssueSeverity.Warning,
                        Channel = MigrationReportChannel.ReadinessIssue,
                        Message = "Model entity has no resolvable table name; the CLR type name will be used.",
                        Recommendation = "Populate MigrationModelEntity.TableName when the ORM mapping differs from the CLR type name."
                    });
                }
                if (entity.PrimaryKeys == null || entity.PrimaryKeys.Count == 0)
                {
                    report.Issues.Add(new MigrationReadinessIssue
                    {
                        Code = "primary-key-missing",
                        Severity = MigrationIssueSeverity.Warning,
                        Channel = MigrationReportChannel.ReadinessIssue,
                        Message = $"Entity '{entity.EntityName}' has no primary key in the supplied model.",
                        Recommendation = "Mark at least one property with IsPrimaryKey=true in the MigrationModelEntity.",
                        EntityName = entity.EntityName
                    });
                }
            }

            report.ReportHash = ComputeReadinessReportHash(
                report.DataSourceName,
                report.DataSourceType,
                report.DataSourceCategory,
                usesDiscovery: true,
                report.EntityTypeCount,
                report.Issues);

            return report;
        }
    }
}
