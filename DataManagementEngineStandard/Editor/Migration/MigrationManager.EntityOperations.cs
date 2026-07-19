using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Helpers.FileandFolderHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (entity == null)
                return CreateErrorsInfo(Errors.Failed, "Entity structure cannot be null");

            if (string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name cannot be empty");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            // NOTE: Do NOT pre-map types here. The datasource's CreateEntityAs handles all
            // type conversion from .NET types (Fieldtype like "System.String") to its own
            // native type system internally. Pre-mapping corrupts the EntityStructure.

            bool entityExists;
            try
            {
                entityExists = MigrateDataSource.CheckEntityExist(entity.EntityName);

                // Check if there was an error during the existence check itself
                // (as opposed to just "entity doesn't exist")
                if (MigrateDataSource.ErrorObject != null && MigrateDataSource.ErrorObject.Flag == Errors.Failed)
                {
                    var checkError = CreateErrorsInfo(
                        Errors.Failed,
                        $"Error checking if entity '{entity.EntityName}' exists: {MigrateDataSource.ErrorObject.Message}",
                        MigrateDataSource.ErrorObject.Ex);
                    return checkError;
                }
            }
            catch (Exception checkEx)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception checking entity existence '{entity.EntityName}': {checkEx.Message}", checkEx);
            }

            if (!entityExists)
            {
                if (!createIfMissing)
                    return CreateErrorsInfo(Errors.Failed, $"Entity '{entity.EntityName}' does not exist");

                // Use IDataSource.CreateEntityAs for datasource-agnostic entity creation
                try
                {
                    var created = MigrateDataSource.CreateEntityAs(entity);
                    IErrorsInfo result;
                    if (created)
                    {
                        result = CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully");
                    }
                    else
                    {
                        result = CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'. Check ErrorObject for details.");
                    }

                    // Check ErrorObject for additional details
                    if (!created && MigrateDataSource.ErrorObject != null)
                    {
                        result.Message = MigrateDataSource.ErrorObject.Message ?? result.Message;
                        result.Ex = MigrateDataSource.ErrorObject.Ex;
                    }

                    TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, result);
                    if (!created)
                        return result;

                    // Entity was just created. Apply indexes and FKs if requested.
                    // The create path bypasses the addMissingColumns branch, so
                    // we must apply relational artifacts here to honor the
                    // applyForeignKeys / applyIndexes opt-in contract.
                    if (applyIndexes || applyForeignKeys)
                    {
                        var postCreateFailures = new List<string>();
                        if (applyIndexes)
                        {
                            var indexFailures = ApplyIndexesForEntity(entity);
                            if (indexFailures != null) postCreateFailures.AddRange(indexFailures);
                        }
                        if (applyForeignKeys)
                        {
                            var fkFailures = ApplyForeignKeysForEntity(entity);
                            if (fkFailures != null) postCreateFailures.AddRange(fkFailures);
                        }

                        if (postCreateFailures.Count > 0)
                        {
                            var msg = $"Entity '{entity.EntityName}' was created, but relational artifacts failed: {string.Join("; ", postCreateFailures)}";
                            var partial = CreateErrorsInfo(Errors.Failed, msg);
                            TrackMigration("EnsureEntity.PostCreate", entity.EntityName, null, string.Empty, partial);
                            return partial;
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    var errorResult = CreateErrorsInfo(Errors.Failed, $"Exception creating entity '{entity.EntityName}': {ex.Message}", ex);
                    TrackMigration("CreateEntityAs", entity.EntityName, null, string.Empty, errorResult);
                    return errorResult;
                }
            }

            if (!addMissingColumns)
            {
                // Even when not adding missing columns, the caller may still
                // want the relational artifacts applied. Honor that.
                if (applyForeignKeys || applyIndexes)
                {
                    var postExistFailures = new List<string>();
                    if (applyIndexes)
                    {
                        var indexFailures = ApplyIndexesForEntity(entity);
                        if (indexFailures != null) postExistFailures.AddRange(indexFailures);
                    }
                    if (applyForeignKeys)
                    {
                        var fkFailures = ApplyForeignKeysForEntity(entity);
                        if (fkFailures != null) postExistFailures.AddRange(fkFailures);
                    }
                    if (postExistFailures.Count > 0)
                    {
                        var msg = $"Entity '{entity.EntityName}' exists, but relational artifacts failed: {string.Join("; ", postExistFailures)}";
                        var partial = CreateErrorsInfo(Errors.Failed, msg);
                        TrackMigration("EnsureEntity.PostExist", entity.EntityName, null, string.Empty, partial);
                        return partial;
                    }
                }
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' already exists");
            }

            var current = MigrateDataSource.GetEntityStructure(entity.EntityName, true);
            if (current == null)
                return CreateErrorsInfo(Errors.Failed, $"Failed to load structure for '{entity.EntityName}'");

            var missingColumns = GetMissingColumns(current, entity);
            var failures = new List<string>();
            if (missingColumns.Count > 0)
            {
                foreach (var column in missingColumns)
                {
                    var addResult = AddColumn(entity, column);
                    if (addResult.Flag != Errors.Ok)
                        failures.Add($"{column.FieldName}: {addResult.Message}");
                }
            }

            // After schema is in sync, optionally apply relational artifacts.
            // These are opt-in because the caller (especially EF Core adapters)
            // may have already created them via the datasource's own provisioning.
            if (applyIndexes)
            {
                var indexFailures = ApplyIndexesForEntity(entity);
                if (indexFailures != null) failures.AddRange(indexFailures);
            }

            if (applyForeignKeys)
            {
                var fkFailures = ApplyForeignKeysForEntity(entity);
                if (fkFailures != null) failures.AddRange(fkFailures);
            }

            if (failures.Count > 0)
            {
                return CreateErrorsInfo(
                    Errors.Failed,
                    $"Failed schema sync for '{entity.EntityName}': {string.Join("; ", failures)}");
            }

            if (missingColumns.Count == 0 && !applyForeignKeys && !applyIndexes)
                return CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' is up to date");

            return CreateErrorsInfo(
                Errors.Ok,
                $"Entity '{entity.EntityName}' synchronized ({missingColumns.Count} new column(s)" +
                (applyIndexes ? ", indexes applied" : string.Empty) +
                (applyForeignKeys ? ", foreign keys applied" : string.Empty) + ")");
        }

        public IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false)
        {
            if (pocoType == null)
                return CreateErrorsInfo(Errors.Failed, "POCO type cannot be null");

            // Use TryGetEntityStructure (not classCreator directly) so the
            // model-interop cache populated by BuildMigrationPlanForModel is
            // honored. EF Core callers that populated MigrationModel then
            // BuildMigrationPlanForModel have already primed the cache with
            // ORM-shaped structures (with table name from GetTableName(), etc.).
            var entity = TryGetEntityStructure(pocoType);
            if (entity == null)
            {
                if (_editor?.classCreator == null)
                    return CreateErrorsInfo(Errors.Failed, "Class creator is not available");
                return CreateErrorsInfo(Errors.Failed, $"Failed to convert POCO '{pocoType.Name}' to EntityStructure");
            }

            return EnsureEntity(entity, createIfMissing, addMissingColumns, detectRelationships && applyForeignKeys, detectRelationships && applyIndexes);
        }

        public IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired)
        {
            if (current?.Fields == null || desired?.Fields == null)
                return new List<EntityField>();

            // Phase 7 (W2): compare by the EFFECTIVE datasource column name — a field's [Column("x")]
            // rename means its live column is "x", not the CLR property name. Comparing FieldName-only
            // made a [Column]-renamed property look perpetually "missing" and re-planned every run.
            var existing = new HashSet<string>(
                current.Fields.Select(EffectiveColumnName),
                StringComparer.OrdinalIgnoreCase);

            return desired.Fields.Where(f => !existing.Contains(EffectiveColumnName(f))).ToList();
        }

        /// <summary>The datasource column name a field maps to: its <c>[Column]</c> name when set, else its field name.</summary>
        private static string EffectiveColumnName(EntityField field)
            => field == null ? string.Empty
             : (!string.IsNullOrWhiteSpace(field.ColumnName) ? field.ColumnName : field.FieldName);

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            // NOTE: Do NOT pre-map types here. The datasource's CreateEntityAs handles all
            // type conversion internally. Passing .NET types (Fieldtype) is correct.

            // Validate entity structure before creation (if helper supports validation)
            var helper = _editor?.GetDataSourceHelper(MigrateDataSource.DatasourceType);
            if (helper != null)
            {
                try
                {
                    var (isValid, errors) = helper.ValidateEntity(entity);
                    if (!isValid && errors != null && errors.Count > 0)
                    {
                        var errorMsg = $"Entity validation failed: {string.Join("; ", errors)}";
                        var validationResult = CreateErrorsInfo(Errors.Failed, errorMsg);
                        TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, validationResult);
                        return validationResult;
                    }
                }
                catch
                {
                    // Validation not supported or failed, continue with creation
                }
            }

            // Use IDataSource.CreateEntityAs for datasource-agnostic entity creation
            // This allows each datasource to handle creation according to its own capabilities
            // Each datasource implementation maps .NET types to its own type system internally
            try
            {
                var created = MigrateDataSource.CreateEntityAs(entity);
                var result = created
                    ? CreateErrorsInfo(Errors.Ok, $"Entity '{entity.EntityName}' created successfully")
                    : CreateErrorsInfo(Errors.Failed, $"Failed to create entity '{entity.EntityName}'. Check ErrorObject for details.");

                // Check ErrorObject for additional details
                if (!created && MigrateDataSource.ErrorObject != null)
                {
                    result.Message = MigrateDataSource.ErrorObject.Message ?? result.Message;
                    result.Ex = MigrateDataSource.ErrorObject.Ex;
                }

                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, result);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = CreateErrorsInfo(Errors.Failed, $"Exception creating entity '{entity.EntityName}': {ex.Message}", ex);
                TrackMigration("CreateEntity", entity.EntityName, null, string.Empty, errorResult);
                return errorResult;
            }
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.DropEntity))
                return UnsupportedOperation("DropEntity", entityName, null, null, "DDL-UNSUPPORTED-PROVIDER");

            var dropResult = provider.DropEntity(entityName);
            TrackMigration("DropEntity", entityName, null, string.Empty, dropResult);
            EmitDdlEvidence("DropEntity", entityName, null, null, null,
                OutcomeFor(dropResult), DdlHelperSource.Direct,
                dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        public IErrorsInfo TruncateEntity(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.TruncateEntity))
                return UnsupportedOperation("TruncateEntity", entityName, null, null, "DDL-UNSUPPORTED-PROVIDER");

            var truncateResult = provider.TruncateEntity(entityName);
            TrackMigration("TruncateEntity", entityName, null, string.Empty, truncateResult);
            EmitDdlEvidence("TruncateEntity", entityName, null, null, null,
                OutcomeFor(truncateResult), DdlHelperSource.Direct,
                truncateResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return truncateResult;
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return CreateErrorsInfo(Errors.Failed, "Entity names are missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.RenameEntity))
                return UnsupportedOperation("RenameEntity", oldName, null, null, "DDL-UNSUPPORTED-PROVIDER");

            var renameResult = provider.RenameEntity(oldName, newName);
            TrackMigration("RenameEntity", $"{oldName}->{newName}", null, string.Empty, renameResult);
            EmitDdlEvidence("RenameEntity", oldName, null, null, null,
                OutcomeFor(renameResult), DdlHelperSource.Direct,
                renameResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return renameResult;
        }

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(columnName) || newColumn == null)
                return CreateErrorsInfo(Errors.Failed, "Entity or column information is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.AlterColumn))
                return UnsupportedOperation("AlterColumn", entityName, columnName, null, "DDL-UNSUPPORTED-PROVIDER");

            var alterResult = provider.AlterColumn(entityName, columnName, newColumn);
            TrackMigration("AlterColumn", entityName, columnName, string.Empty, alterResult);
            EmitDdlEvidence("AlterColumn", entityName, columnName, null, null,
                OutcomeFor(alterResult), DdlHelperSource.Direct,
                alterResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return alterResult;
        }

        public IErrorsInfo DropColumn(string entityName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(columnName))
                return CreateErrorsInfo(Errors.Failed, "Entity or column name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.DropColumn))
                return UnsupportedOperation("DropColumn", entityName, columnName, null, "DDL-UNSUPPORTED-PROVIDER");

            var dropResult = provider.DropColumn(entityName, columnName);
            TrackMigration("DropColumn", entityName, columnName, string.Empty, dropResult);
            EmitDdlEvidence("DropColumn", entityName, columnName, null, null,
                OutcomeFor(dropResult), DdlHelperSource.Direct,
                dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
        {
            if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(oldColumnName) || string.IsNullOrWhiteSpace(newColumnName))
                return CreateErrorsInfo(Errors.Failed, "Entity or column name is missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.RenameColumn))
                return UnsupportedOperation("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, "DDL-UNSUPPORTED-PROVIDER");

            var renameResult = provider.RenameColumn(entityName, oldColumnName, newColumnName);
            TrackMigration("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", string.Empty, renameResult);
            EmitDdlEvidence("RenameColumn", entityName, $"{oldColumnName}->{newColumnName}", null, null,
                OutcomeFor(renameResult), DdlHelperSource.Direct,
                renameResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return renameResult;
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            if (string.IsNullOrWhiteSpace(entityName) || columns == null || columns.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "Entity or columns are missing");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.CreateIndex))
                return UnsupportedOperation("CreateIndex", entityName, null, indexName, "DDL-UNSUPPORTED-PROVIDER");

            var indexResult = provider.CreateIndex(entityName, indexName, columns, options);
            TrackMigration("CreateIndex", entityName, indexName, string.Empty, indexResult);
            EmitDdlEvidence("CreateIndex", entityName, null, indexName, null,
                OutcomeFor(indexResult), DdlHelperSource.Direct,
                indexResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return indexResult;
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (string.IsNullOrWhiteSpace(indexName))
                return CreateErrorsInfo(Errors.Failed, "Index name is required");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.DropIndex))
                return UnsupportedOperation("DropIndex", entityName, null, indexName, "DDL-UNSUPPORTED-PROVIDER");

            var dropResult = provider.DropIndex(entityName, indexName);
            TrackMigration("DropIndex", entityName, indexName, string.Empty, dropResult);
            EmitDdlEvidence("DropIndex", entityName, null, indexName, null,
                OutcomeFor(dropResult), DdlHelperSource.Direct,
                dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        // Reflection-based fallbacks for custom IDataSourceHelper implementations were removed
        // in Phase 10 — index/FK generation now lives inside the ISchemaMigrationProvider
        // (RdbmsSqlMigrationProvider.InvokeHelperReturningSql handles helper-specific overloads).

        public IErrorsInfo AddForeignKey(
            string entityName,
            string[] columnNames,
            string referencedEntityName,
            string[] referencedColumnNames,
            string onDeleteBehavior = "Cascade",
            string onUpdateBehavior = "Cascade",
            string constraintName = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Dependent entity name is missing");

            if (columnNames == null || columnNames.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "At least one foreign-key column must be specified");

            if (string.IsNullOrWhiteSpace(referencedEntityName))
                return CreateErrorsInfo(Errors.Failed, "Referenced entity name is missing");

            if (referencedColumnNames == null || referencedColumnNames.Length == 0)
                return CreateErrorsInfo(Errors.Failed, "At least one referenced column must be specified");

            if (columnNames.Length != referencedColumnNames.Length)
                return CreateErrorsInfo(Errors.Failed, "Number of foreign-key columns must match referenced columns");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            constraintName = string.IsNullOrWhiteSpace(constraintName)
                ? BuildForeignKeyName(entityName, referencedEntityName, columnNames)
                : constraintName.Trim();

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.AddForeignKey))
                return UnsupportedOperation("AddForeignKey", entityName, null, constraintName, "DDL-UNSUPPORTED-PROVIDER");

            var fkResult = provider.AddForeignKey(entityName, columnNames, referencedEntityName, referencedColumnNames, onDeleteBehavior, onUpdateBehavior, constraintName);
            TrackMigration("AddForeignKey", entityName, constraintName ?? string.Join(",", columnNames), string.Empty, fkResult);
            EmitDdlEvidence("AddForeignKey", entityName, null, constraintName, null,
                OutcomeFor(fkResult), DdlHelperSource.Direct,
                fkResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return fkResult;
        }

        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return CreateErrorsInfo(Errors.Failed, "Entity name is missing");

            if (string.IsNullOrWhiteSpace(constraintName))
                return CreateErrorsInfo(Errors.Failed, "Constraint name is required");

            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.DropForeignKey))
                return UnsupportedOperation("DropForeignKey", entityName, null, constraintName, "DDL-UNSUPPORTED-PROVIDER");

            var dropResult = provider.DropForeignKey(entityName, constraintName);
            TrackMigration("DropForeignKey", entityName, constraintName, string.Empty, dropResult);
            EmitDdlEvidence("DropForeignKey", entityName, null, constraintName, null,
                OutcomeFor(dropResult), DdlHelperSource.Direct,
                dropResult.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return dropResult;
        }

        // (TryGenerateAddForeignKeyViaInterface / TryGenerateDropForeignKeyViaInterface removed —
        // FK generation now lives inside ISchemaMigrationProvider. See note after DropIndex.)

        private IErrorsInfo AddColumn(EntityStructure entity, EntityField column)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            var provider = ResolveProvider();
            if (!provider.Capabilities.Supports(SchemaMigrationOp.AddColumn))
            {
                EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null,
                    DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, "DDL-UNSUPPORTED-PROVIDER");
                var unsup = CreateErrorsInfo(Errors.Failed, $"'AddColumn' is not supported for {MigrateDataSource.DatasourceType}.");
                TrackMigration("AddColumn", entity.EntityName, column.FieldName, string.Empty, unsup);
                return unsup;
            }

            var result = provider.AddColumn(entity.EntityName, column);
            TrackMigration("AddColumn", entity.EntityName, column.FieldName, string.Empty, result);
            EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null,
                OutcomeFor(result), DdlHelperSource.Direct,
                result.Flag == Errors.Ok ? "DDL-EXECUTED" : "DDL-EXEC-FAILED");
            return result;
        }

        // ── Phase 10 provider dispatch ─────────────────────────────────────
        //
        // All schema operations are routed through ISchemaMigrationProvider (resolved 3-tier:
        // exact DataSourceType → DatasourceCategory fallback → NullMigrationProvider).
        // The provider executes against the data source's NATIVE API and returns an
        // IErrorsInfo outcome; the manager wraps it with the existing TrackMigration /
        // EmitDdlEvidence instrumentation. FILE/Connector/Queue/Stream/WebApi categories
        // are covered by their category-fallback providers; the IsFileDataSource branches
        // and the inlined file-mutation helpers were removed in Phase 10.3.

        private ISchemaMigrationProvider ResolveProvider()
            => _editor.GetMigrationProvider(MigrateDataSource);

        private static DdlOperationOutcome OutcomeFor(IErrorsInfo result)
        {
            if (result == null || result.Flag != Errors.Ok)
                return DdlOperationOutcome.Failed;
            return (result.Message?.IndexOf("No DDL", StringComparison.OrdinalIgnoreCase) >= 0)
                ? DdlOperationOutcome.NoOp
                : DdlOperationOutcome.Executed;
        }

        private IErrorsInfo UnsupportedOperation(string operationName, string entityName, string columnName, string indexName, string reasonCode)
        {
            var msg = $"'{operationName}' is not supported for {MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown}.";
            EmitDdlEvidence(operationName, entityName, columnName, indexName, null,
                DdlOperationOutcome.Unsupported, DdlHelperSource.Direct, reasonCode);
            return CreateErrorsInfo(Errors.Failed, msg);
        }

        /// <summary>
        /// Applies all indexes declared on an entity. Returns null when the entity
        /// has no indexes; otherwise returns a list of failure messages.
        /// </summary>
        private List<string> ApplyIndexesForEntity(EntityStructure entity)
        {
            var failures = new List<string>();
            if (entity?.Indexes == null || entity.Indexes.Count == 0)
                return null;

            // Field-level unique/indexed markers create implicit indexes that the
            // DDL layer may have already emitted. Track them here only when an
            // explicit EntityIndex row is present.
            foreach (var idx in entity.Indexes)
            {
                if (idx == null || idx.Columns == null || idx.Columns.Count == 0)
                    continue;

                var name = string.IsNullOrWhiteSpace(idx.Name)
                    ? $"IX_{entity.EntityName}_{string.Join("_", idx.Columns)}"
                    : idx.Name;

                var options = idx.Options != null && idx.Options.Count > 0
                    ? new Dictionary<string, object>(idx.Options)
                    : null;
                if (idx.IsUnique && (options == null || !options.ContainsKey("UNIQUE")))
                    options ??= new Dictionary<string, object>();
                if (idx.IsUnique && !options.ContainsKey("UNIQUE"))
                    options["UNIQUE"] = true;

                var result = CreateIndex(entity.EntityName, name, idx.Columns.ToArray(), options);
                if (result.Flag != Errors.Ok)
                    failures.Add($"index '{name}': {result.Message}");
            }

            return failures.Count == 0 ? null : failures;
        }

        /// <summary>
        /// Applies only the index whose canonical name matches
        /// <paramref name="targetIndexName"/>. The canonical name is
        /// <c>idx.Name</c> or the auto-generated fallback
        /// <c>IX_{EntityName}_{Column1}_{Column2}</c> — the same naming
        /// convention used by the plan builder to populate TargetName.
        /// </summary>
        private List<string> ApplyIndexesForEntity(EntityStructure entity, string targetIndexName)
        {
            var failures = new List<string>();
            if (entity?.Indexes == null || entity.Indexes.Count == 0)
                return null;
            if (string.IsNullOrWhiteSpace(targetIndexName))
                return ApplyIndexesForEntity(entity);

            var matched = false;
            foreach (var idx in entity.Indexes)
            {
                if (idx == null || idx.Columns == null || idx.Columns.Count == 0)
                    continue;

                var name = string.IsNullOrWhiteSpace(idx.Name)
                    ? $"IX_{entity.EntityName}_{string.Join("_", idx.Columns)}"
                    : idx.Name;

                if (!string.Equals(name, targetIndexName, StringComparison.OrdinalIgnoreCase))
                    continue;

                matched = true;
                var options = idx.Options != null && idx.Options.Count > 0
                    ? new Dictionary<string, object>(idx.Options)
                    : null;
                if (idx.IsUnique && (options == null || !options.ContainsKey("UNIQUE")))
                    options ??= new Dictionary<string, object>();
                if (idx.IsUnique && !options.ContainsKey("UNIQUE"))
                    options["UNIQUE"] = true;

                var result = CreateIndex(entity.EntityName, name, idx.Columns.ToArray(), options);
                if (result.Flag != Errors.Ok)
                    failures.Add($"index '{name}': {result.Message}");
            }

            if (!matched)
                failures.Add($"index '{targetIndexName}' was not found on entity '{entity.EntityName}'.");

            return failures.Count == 0 ? null : failures;
        }

        private sealed class ForeignKeyDefinition
        {
            public string ConstraintName { get; set; } = string.Empty;
            public string ReferencedEntityName { get; set; } = string.Empty;
            public List<string> ColumnNames { get; } = new List<string>();
            public List<string> ReferencedColumnNames { get; } = new List<string>();
            public string OnDeleteBehavior { get; set; } = "Cascade";
            public string OnUpdateBehavior { get; set; } = "Cascade";
        }

        private static List<ForeignKeyDefinition> BuildForeignKeyDefinitions(EntityStructure entity)
        {
            var definitions = new List<ForeignKeyDefinition>();
            if (entity?.Relations == null || entity.Relations.Count == 0)
                return definitions;

            var byKey = new Dictionary<string, ForeignKeyDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in entity.Relations)
            {
                if (rel == null || string.IsNullOrWhiteSpace(rel.EntityColumnID))
                    continue;

                var parentEntity = rel.RelatedEntityID;
                var parentColumn = rel.RelatedEntityColumnID;
                if (string.IsNullOrWhiteSpace(parentEntity) || string.IsNullOrWhiteSpace(parentColumn))
                    continue;

                var explicitName = rel.RalationName?.Trim() ?? string.Empty;
                var groupKey = !string.IsNullOrWhiteSpace(explicitName)
                    ? $"{entity.EntityName}|{parentEntity}|{explicitName}"
                    : $"{entity.EntityName}|{parentEntity}|{rel.EntityColumnID}|{parentColumn}";

                if (!byKey.TryGetValue(groupKey, out var definition))
                {
                    definition = new ForeignKeyDefinition
                    {
                        ConstraintName = explicitName,
                        ReferencedEntityName = parentEntity,
                        OnDeleteBehavior = rel.OnDeleteBehavior ?? string.Empty,
                        OnUpdateBehavior = rel.OnUpdateBehavior ?? string.Empty
                    };
                    byKey[groupKey] = definition;
                    definitions.Add(definition);
                }

                if (definition.ColumnNames.Any(column => string.Equals(column, rel.EntityColumnID, StringComparison.OrdinalIgnoreCase)))
                    continue;

                definition.ColumnNames.Add(rel.EntityColumnID);
                definition.ReferencedColumnNames.Add(parentColumn);
                if (string.IsNullOrWhiteSpace(definition.OnDeleteBehavior) && !string.IsNullOrWhiteSpace(rel.OnDeleteBehavior))
                    definition.OnDeleteBehavior = rel.OnDeleteBehavior;
                if (string.IsNullOrWhiteSpace(definition.OnUpdateBehavior) && !string.IsNullOrWhiteSpace(rel.OnUpdateBehavior))
                    definition.OnUpdateBehavior = rel.OnUpdateBehavior;
            }

            foreach (var definition in definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.ConstraintName))
                    definition.ConstraintName = BuildForeignKeyName(entity.EntityName, definition.ReferencedEntityName, definition.ColumnNames);
                if (string.IsNullOrWhiteSpace(definition.OnDeleteBehavior))
                    definition.OnDeleteBehavior = "Cascade";
                if (string.IsNullOrWhiteSpace(definition.OnUpdateBehavior))
                    definition.OnUpdateBehavior = "Cascade";
            }

            return definitions;
        }

        private static string BuildForeignKeyName(string entityName, string referencedEntityName, IEnumerable<string> columnNames)
        {
            var columns = columnNames?
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim())
                .ToList() ?? new List<string>();

            if (columns.Count == 0)
                columns.Add("Column");

            var entity = string.IsNullOrWhiteSpace(entityName) ? "Entity" : entityName.Trim();
            var referenced = string.IsNullOrWhiteSpace(referencedEntityName) ? "Referenced" : referencedEntityName.Trim();
            return $"FK_{entity}_{referenced}_{string.Join("_", columns)}";
        }

        /// <summary>
        /// Applies all foreign-key relations declared on an entity. Returns null
        /// when the entity has no relations; otherwise returns a list of failure
        /// messages. Relations sharing a constraint name are applied as one
        /// composite FK.
        /// </summary>
        private List<string> ApplyForeignKeysForEntity(EntityStructure entity)
        {
            var failures = new List<string>();
            var foreignKeys = BuildForeignKeyDefinitions(entity);
            if (foreignKeys.Count == 0)
                return null;

            foreach (var fk in foreignKeys)
            {
                var result = AddForeignKey(
                    entityName: entity.EntityName,
                    columnNames: fk.ColumnNames.ToArray(),
                    referencedEntityName: fk.ReferencedEntityName,
                    referencedColumnNames: fk.ReferencedColumnNames.ToArray(),
                    onDeleteBehavior: fk.OnDeleteBehavior,
                    onUpdateBehavior: fk.OnUpdateBehavior,
                    constraintName: fk.ConstraintName);

                if (result.Flag != Errors.Ok)
                    failures.Add($"FK '{fk.ConstraintName}': {result.Message}");
            }

            return failures.Count == 0 ? null : failures;
        }

        /// <summary>
        /// Applies only the foreign key whose canonical name matches
        /// <paramref name="targetForeignKeyName"/>. The canonical name is
        /// <c>rel.RalationName</c> or the auto-generated fallback
        /// <c>FK_{EntityName}_{RelatedEntityID}_{EntityColumnID}</c> — the
        /// same naming convention used by the plan builder.
        /// </summary>
        private List<string> ApplyForeignKeysForEntity(EntityStructure entity, string targetForeignKeyName)
        {
            var failures = new List<string>();
            var foreignKeys = BuildForeignKeyDefinitions(entity);
            if (foreignKeys.Count == 0)
                return null;
            if (string.IsNullOrWhiteSpace(targetForeignKeyName))
                return ApplyForeignKeysForEntity(entity);

            var matched = false;
            foreach (var fk in foreignKeys)
            {
                if (!string.Equals(fk.ConstraintName, targetForeignKeyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                matched = true;
                var result = AddForeignKey(
                    entityName: entity.EntityName,
                    columnNames: fk.ColumnNames.ToArray(),
                    referencedEntityName: fk.ReferencedEntityName,
                    referencedColumnNames: fk.ReferencedColumnNames.ToArray(),
                    onDeleteBehavior: fk.OnDeleteBehavior,
                    onUpdateBehavior: fk.OnUpdateBehavior,
                    constraintName: fk.ConstraintName);

                if (result.Flag != Errors.Ok)
                    failures.Add($"FK '{fk.ConstraintName}': {result.Message}");
            }

            if (!matched)
                failures.Add($"foreign key '{targetForeignKeyName}' was not found on entity '{entity.EntityName}'.");

            return failures.Count == 0 ? null : failures;
        }

        // NOTE: EnsureEntityStructureTypes, InferDataSourceTypeFromNetType, and
        // InferNetTypeFromDataSourceType have been intentionally removed.
        //
        // The MigrationManager must NOT do type mapping or SQL generation for entity creation.
        // Each IDataSource.CreateEntityAs() implementation handles .NET type → native type
        // mapping internally. The EntityStructure should contain .NET types as produced by
        // ConvertToEntityStructure  (e.g., "System.String", "System.Int32", "System.DateTime").
        // The datasource converts these to its own native types (TEXT/INTEGER for SQLite,
        // VARCHAR/INT for SQL Server, etc.).
    }
}
