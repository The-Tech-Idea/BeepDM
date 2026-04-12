using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
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

        private List<DataBlockRelationship> GetActiveRelationships(string masterBlockName)
        {
            lock (_lockObject)
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                    return new List<DataBlockRelationship>();

                return relationships.Where(r => r.IsActive).ToList();
            }
        }

        private async Task SynchronizeDetailHierarchyAsync(string masterBlockName, HashSet<string> visited)
        {
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
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited);
                return;
            }

            foreach (var relationship in relationships)
            {
                var fieldMappings = GetRelationshipFieldMappings(relationship);
                if (fieldMappings.Count == 0)
                {
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited);
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
                        FilterValue = masterValue.ToString()
                    });
                }

                if (hasMissingMasterValue)
                {
                    await ClearDetailHierarchyAsync(relationship.DetailBlockName, visited);
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
                finally
                {
                    ResumeSync(relationship.DetailBlockName);
                }

                await SynchronizeDetailHierarchyAsync(relationship.DetailBlockName, visited);
            }
        }

        private async Task ClearDetailHierarchyAsync(string blockName, HashSet<string> visited)
        {
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
                finally
                {
                    ResumeSync(blockName);
                }
            }

            foreach (var detailBlockName in GetDetailBlocks(blockName))
            {
                await ClearDetailHierarchyAsync(detailBlockName, visited);
            }
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = target.GetType().GetProperty(
                propertyName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            return property?.GetValue(target);
        }

        private static bool TrySetPropertyValue(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            var property = target.GetType().GetProperty(
                propertyName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite)
                return false;

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var converted = value;
            if (converted != null && !targetType.IsInstanceOfType(converted))
                converted = Convert.ChangeType(converted, targetType);

            property.SetValue(target, converted);
            return true;
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
