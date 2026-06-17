using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Protected Helper Methods (For Partial Classes)

        /// <summary>
        /// Validates the minimum inputs required to register a block.
        /// </summary>
        /// <param name="blockName">Logical block name.</param>
        /// <param name="unitOfWork">Unit of work backing the block.</param>
        protected void ValidateBlockRegistrationParameters(string blockName, IUnitofWork unitOfWork)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Block name cannot be null or empty", nameof(blockName));
            
            if (unitOfWork == null)
                throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Resolves entity metadata from the explicit registration argument, the unit of work itself,
        /// or the backing datasource when only the entity name is known.
        /// </summary>
        protected IEntityStructure ResolveBlockEntityStructure(
            IUnitofWork unitOfWork,
            IEntityStructure entityStructure,
            string dataSourceName = null)
        {
            if (entityStructure != null)
            {
                if (unitOfWork?.EntityStructure == null && entityStructure is EntityStructure concreteStructure)
                    unitOfWork.EntityStructure = concreteStructure;

                return entityStructure;
            }

            if (unitOfWork?.EntityStructure != null)
                return unitOfWork.EntityStructure;

            var entityName = unitOfWork?.EntityName;
            if (string.IsNullOrWhiteSpace(entityName))
                return null;

            var dataSource = unitOfWork.DataSource;
            if (dataSource == null)
            {
                var resolvedDataSourceName = !string.IsNullOrWhiteSpace(dataSourceName)
                    ? dataSourceName
                    : unitOfWork.DatasourceName;

                if (!string.IsNullOrWhiteSpace(resolvedDataSourceName))
                    dataSource = _dmeEditor?.GetDataSource(resolvedDataSourceName);
            }

            if (dataSource == null)
                return null;

            try
            {
                var resolvedStructure = dataSource.GetEntityStructure(entityName, false);
                if (resolvedStructure != null)
                {
                    unitOfWork.EntityStructure = resolvedStructure;
                    if (unitOfWork.DataSource == null)
                        unitOfWork.DataSource = dataSource;
                }

                return resolvedStructure;
            }
            catch (Exception ex)
            {
                LogError($"Error resolving entity structure for '{entityName}'", ex);
                return null;
            }
        }

        /// <summary>
        /// Builds the <see cref="DataBlockInfo"/> snapshot stored for a registered block.
        /// </summary>
        /// <param name="blockName">Logical block name.</param>
        /// <param name="unitOfWork">Unit of work backing the block.</param>
        /// <param name="entityStructure">Entity metadata used by the block.</param>
        /// <param name="dataSourceName">Optional datasource alias for later lookups.</param>
        /// <param name="isMasterBlock">Whether the block is initially treated as a master block.</param>
        /// <returns>The initialized block metadata instance.</returns>
        protected DataBlockInfo CreateBlockInfo(string blockName, IUnitofWork unitOfWork, 
            IEntityStructure entityStructure, string dataSourceName, bool isMasterBlock)
        {
            return new DataBlockInfo
            {
                BlockName = blockName,
                UnitOfWork = unitOfWork,
                EntityStructure = entityStructure,
                DataSourceName = dataSourceName ?? "Unknown",
                IsMasterBlock = isMasterBlock,
                EntityType = ResolveBlockEntityType(unitOfWork, entityStructure, dataSourceName),
                Mode = DataBlockMode.Query,
                IsRegistered = true,
                RegisteredAt = DateTime.Now,
                Configuration = Configuration?.GetBlockConfiguration(blockName) ?? new BlockConfiguration()
            };
        }

        /// <summary>
        /// Resolves the CLR entity type for a block from the unit of work, loaded items, entity metadata, or datasource metadata.
        /// </summary>
        /// <param name="unitOfWork">Unit of work backing the block.</param>
        /// <param name="entityStructure">Entity metadata used by the block.</param>
        /// <param name="dataSourceName">Optional datasource alias used for editor lookups.</param>
        /// <returns>The resolved CLR type, or <c>null</c> when the type cannot be determined.</returns>
        protected Type ResolveBlockEntityType(IUnitofWork unitOfWork, IEntityStructure entityStructure, string dataSourceName = null)
        {
            if (unitOfWork?.EntityType != null)
                return unitOfWork.EntityType;

            if (unitOfWork?.CurrentItem != null)
                return unitOfWork.CurrentItem.GetType();

            if (TryResolveEntityTypeFromUnits(unitOfWork?.Units, out Type unitsEntityType))
                return unitsEntityType;

            var entityName = entityStructure?.EntityName;
            if (string.IsNullOrWhiteSpace(entityName))
                entityName = unitOfWork?.EntityName;

            if (unitOfWork?.DataSource != null && !string.IsNullOrWhiteSpace(entityName))
            {
                try
                {
                    var dataSourceEntityType = unitOfWork.DataSource.GetEntityType(entityName);
                    if (dataSourceEntityType != null)
                        return dataSourceEntityType;
                }
                catch (Exception ex)
                {
                    LogError($"Error resolving entity type from data source for '{entityName}'", ex);
                }
            }

            if (!string.IsNullOrWhiteSpace(dataSourceName) &&
                !string.Equals(dataSourceName, "Unknown", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(entityName))
            {
                try
                {
                    var dataSource = _dmeEditor?.GetDataSource(dataSourceName);
                    var dataSourceEntityType = dataSource?.GetEntityType(entityName);
                    if (dataSourceEntityType != null)
                        return dataSourceEntityType;
                }
                catch (Exception ex)
                {
                    LogError($"Error resolving entity type from editor data source '{dataSourceName}' for '{entityName}'", ex);
                }
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                return Type.GetType(entityName, throwOnError: false, ignoreCase: true);
            }

            return null;
        }

        /// <summary>
        /// Attempts to infer the entity type from a units collection by inspecting its generic argument or first record.
        /// </summary>
        /// <param name="units">Units collection to inspect.</param>
        /// <param name="entityType">Resolved entity type when successful.</param>
        /// <returns><c>true</c> when a non-<see cref="object"/> entity type can be inferred; otherwise <c>false</c>.</returns>
        protected static bool TryResolveEntityTypeFromUnits(object units, out Type entityType)
        {
            entityType = null;
            if (units == null)
                return false;

            var unitsType = units.GetType();
            if (unitsType.IsGenericType)
            {
                var genericArgument = unitsType.GetGenericArguments().FirstOrDefault();
                if (genericArgument != null && genericArgument != typeof(object))
                {
                    entityType = genericArgument;
                    return true;
                }
            }

            if (units is not IEnumerable enumerable)
                return false;

            var enumerator = enumerable.GetEnumerator();
            try
            {
                if (!enumerator.MoveNext() || enumerator.Current == null)
                    return false;

                entityType = enumerator.Current.GetType();
                return true;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Applies persisted block configuration to a freshly created block metadata instance.
        /// </summary>
        /// <param name="blockInfo">Block metadata to update.</param>
        protected void ApplyBlockConfiguration(DataBlockInfo blockInfo)
        {
            var config = Configuration?.GetBlockConfiguration(blockInfo.BlockName);
            if (config != null)
            {
                blockInfo.Configuration = config;
                // Apply any specific configuration settings
            }
        }

        /// <summary>
        /// Writes a successful runtime operation entry to the editor log.
        /// </summary>
        /// <param name="message">Message to record.</param>
        /// <param name="blockName">Optional block context for the message.</param>
        protected void LogOperation(string message, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor?.AddLogMessage("UnitofWorksManager", fullMessage, DateTime.Now, 0, null, Errors.Ok);
            LogOperationStructured(message, blockName);
        }

        /// <summary>
        /// Writes a failure entry to the editor log and mirrors the error into the per-block error log when a block context is provided.
        /// </summary>
        /// <param name="message">High-level error message.</param>
        /// <param name="ex">Optional exception that caused the failure.</param>
        /// <param name="blockName">Optional block context for the error.</param>
        protected void LogError(string message, Exception ex = null, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor?.AddLogMessage("UnitofWorksManager", fullMessage, DateTime.Now, -1, null, Errors.Failed);

            if (!string.IsNullOrEmpty(blockName))
                _errorLog?.LogError(blockName, ex ?? new InvalidOperationException(message), message);

            LogErrorStructured(message, ex, blockName);
        }

        private void SuppressSync(string blockName) =>
            _syncSuppressCount.AddOrUpdate(blockName, 1, (_, v) => v + 1);
        private void ResumeSync(string blockName) =>
            _syncSuppressCount.AddOrUpdate(blockName, 0, (_, v) => Math.Max(0, v - 1));
        private bool IsSyncSuppressed(string blockName) =>
            _syncSuppressCount.TryGetValue(blockName, out var cnt) && cnt > 0;

        private void ValidateRelationshipParameters(string masterBlockName, string detailBlockName)
        {
            if (string.IsNullOrWhiteSpace(masterBlockName))
                throw new ArgumentException("Master block name cannot be null or empty", nameof(masterBlockName));
            if (string.IsNullOrWhiteSpace(detailBlockName))
                throw new ArgumentException("Detail block name cannot be null or empty", nameof(detailBlockName));
        }

        internal List<DataBlockRelationship> GetActiveRelationships(string masterBlockName)
        {
            lock (_lockObject)
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                    return new List<DataBlockRelationship>();

                return relationships.Where(r => r.IsActive).ToList();
            }
        }

        private async Task SynchronizeDetailHierarchyAsync(string masterBlockName, HashSet<string> visited, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!visited.Add(masterBlockName))
                return;

            var relationships = GetActiveRelationships(masterBlockName);
            if (!relationships.Any())
                return;

            var masterBlock = GetBlock(masterBlockName);
            var currentItem = masterBlock?.UnitOfWork?.CurrentItem;
            if (currentItem == null)
            {
                foreach (var relationship in relationships)
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited, ct);
                return;
            }

            foreach (var relationship in relationships)
            {
                ct.ThrowIfCancellationRequested();

                var fieldMappings = GetRelationshipFieldMappings(relationship);
                if (fieldMappings.Count == 0)
                {
                    // B8 (audit pass 3, 2026-06): silent-fail fix.
                    // The previous version treated an empty
                    // mapping list (returned when
                    // MasterDetailKeyResolver.TryParseMappings
                    // fails — e.g. malformed composite key like
                    // "OrderId;LineNumber" with the wrong
                    // separator) as "no filter, clear the
                    // detail". The clear still happened, but
                    // the user had no signal that their
                    // relationship config was wrong. Log it
                    // once before the clear so the misconfig
                    // is visible.
                    LogError(
                        $"SynchronizeDetailHierarchyAsync: failed to parse master/detail key mapping for relationship " +
                        $"({relationship.MasterBlockName}.{relationship.MasterKeyField} -> " +
                        $"{relationship.DetailBlockName}.{relationship.DetailForeignKeyField}). " +
                        $"Falling back to clearing the detail block.",
                        null, relationship.DetailBlockName);
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited, ct);
                    continue;
                }

                var filters = new List<AppFilter>();
                var hasMissingMasterValue = false;

                foreach (var mapping in fieldMappings)
                {
                    var masterValue = GetPropertyValue(currentItem, mapping.MasterField);
                    if (IsNullOrEmpty(masterValue))
                    {
                        hasMissingMasterValue = true;
                        break;
                    }

                    filters.Add(new AppFilter
                    {
                        FieldName = mapping.DetailField,
                        Operator = "=",
                        // B3 (audit pass 3, 2026-06): culture-invariant
                        // ToString. The previous version used the
                        // current culture's default, which
                        // produced different strings for
                        // DateTime / float / decimal in
                        // different locales. The filter value
                        // round-trips through the UoW's Get
                        // path, which parses the string back to
                        // the target type — a culture-mismatched
                        // string would silently fail to parse
                        // and the detail query would return
                        // zero rows. Using InvariantCulture makes
                        // the round-trip deterministic.
                        FilterValue = masterValue is IFormattable fmt
                            ? fmt.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                            : masterValue.ToString()
                    });
                }

                if (hasMissingMasterValue)
                {
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited, ct);
                    continue;
                }

                var detailBlock = GetBlock(relationship.DetailBlockName);
                if (detailBlock?.UnitOfWork == null)
                    continue;

                SuppressSync(relationship.DetailBlockName);
                try
                {
                    await detailBlock.UnitOfWork.Get(filters);
                }
                // B4 (audit pass 3, 2026-06): the previous
                // version had no catch — an exception from
                // UnitOfWork.Get (e.g. SQL error, disposed
                // connection) would propagate up and abort
                // the whole hierarchy sync, leaving the
                // remaining relationships' detail blocks in
                // an un-synced state. Now: log, continue to
                // the next relationship.
                catch (Exception ex)
                {
                    LogError(
                        $"SynchronizeDetailHierarchyAsync: failed to query detail block '{relationship.DetailBlockName}' for " +
                        $"master '{relationship.MasterBlockName}' key '{relationship.MasterKeyField}'. " +
                        $"Detail block left in its previous state.",
                        ex, relationship.DetailBlockName);
                }
                finally
                {
                    ResumeSync(relationship.DetailBlockName);
                }

                await SynchronizeDetailHierarchyAsync(relationship.DetailBlockName, visited, ct);
            }
        }

        private async Task ClearDetailHierarchyAsync(string blockName, HashSet<string> visited, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!visited.Add(blockName))
                return;

            var blockInfo = GetBlock(blockName);
            if (blockInfo?.UnitOfWork != null)
            {
                SuppressSync(blockName);
                try
                {
                    blockInfo.UnitOfWork.Clear();
                }
                // B5 (audit pass 3, 2026-06): same pattern as
                // B4. UnitOfWork.Clear() on a disposed or
                // otherwise unhappy UoW throws — the
                // exception previously aborted the whole
                // hierarchy clear, leaving downstream
                // detail blocks uncleared. Now: log,
                // continue clearing the rest of the
                // hierarchy.
                catch (Exception ex)
                {
                    LogError(
                        $"ClearDetailHierarchyAsync: failed to clear block '{blockName}'",
                        ex, blockName);
                }
                finally
                {
                    ResumeSync(blockName);
                }
            }

            foreach (var detailBlockName in GetDetailBlocks(blockName))
            {
                await ClearDetailHierarchyAsync(detailBlockName, visited, ct);
            }
        }

        private object GetPropertyValue(object target, string propertyName)
        {
            // Delegates to the process-wide, cached RecordPropertyAccessor.
            // Replaces 5-line reflection block that repeated
            // target.GetType().GetProperty(...) on every call and silently
            // returned null on a misconfigured FieldName. The accessor
            // emits a single throttled warning on miss per (Type, name).
            // Promoted from `static` to instance so it can pass
            // `_dmeEditor` to the accessor for diagnostic logging.
            return RecordPropertyAccessor.GetValue(target, propertyName, _dmeEditor);
        }

        private bool TrySetPropertyValue(object target, string propertyName, object value)
        {
            // Same rationale as GetPropertyValue above: centralized cache
            // + loud diagnostic on missing/read-only fields. Existing
            // call sites (Navigation.SetCurrentIndexByKey, etc.) are
            // unchanged in behavior. Promoted from `static` to instance
            // so it can pass `_dmeEditor` to the accessor.
            return RecordPropertyAccessor.TrySetValue(target, propertyName, value, _dmeEditor);
        }

        private static bool IsNullOrEmpty(object value) =>
            value == null || value == DBNull.Value || (value is string text && string.IsNullOrWhiteSpace(text));

        private static List<DataBlockFieldMapping> GetRelationshipFieldMappings(DataBlockRelationship relationship)
        {
            if (relationship == null)
            {
                return new List<DataBlockFieldMapping>();
            }

            if (!MasterDetailKeyResolver.TryParseMappings(
                relationship.MasterKeyField,
                relationship.DetailForeignKeyField,
                out var mappings,
                out _))
            {
                return new List<DataBlockFieldMapping>();
            }

            return mappings;
        }

        #endregion
    }
}
