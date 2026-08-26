using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Data Operations (Required by Interface - Basic Implementation)

        /// <summary>
        /// Inserts a new record in the specified block
        /// Basic implementation - use InsertRecordEnhancedAsync for better functionality
        /// </summary>
        public async Task<bool> InsertRecordAsync(string blockName, object record = null)
        {
            try
            {
                var result = await InsertRecordEnhancedAsync(blockName, record).ConfigureAwait(false);
                if (result.Flag == Errors.Ok)
                {
                    Status = $"Record inserted successfully in block '{blockName}'";
                    _messageManager?.ShowSuccessMessage(blockName, Status);
                }
                else
                {
                    Status = $"Error inserting record: {result.Message}";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                }
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Status = $"Error inserting record in block '{blockName}': {ex.Message}";
                LogError($"Error inserting record in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes the current record in the specified block
        /// </summary>
        public async Task<bool> DeleteCurrentRecordAsync(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                // Phase 6: security check. This runs BEFORE the CRUD flag
                // guard below, and the order matters: SetBlockSecurity calls
                // ApplyAllSecurityFlags, which writes the policy INTO those same
                // CRUD flags. With the guard first, a security denial always
                // exited here and EnforceBlockSecurity never ran, so the denial
                // was honoured but never recorded — GetSecurityViolations stayed
                // empty for every real denial and the security panel showed an
                // empty audit trail. (2026-08-03)
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Delete))
                {
                    Status = $"Security: delete not permitted on block '{blockName}'";
                    return false;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.DeleteAllowed)
                {
                    Status = $"Delete not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                    return false;
                }

                // Check for unsaved changes in detail blocks
                var detailBlocks = GetDetailBlocks(blockName);
                foreach (var detailBlockName in detailBlocks)
                {
                    if (!await CheckAndHandleUnsavedChangesAsync(detailBlockName))
                        return false;
                }

                // Get current record
                object currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord == null)
                {
                    Status = $"No current record to delete in block '{blockName}'";
                    return false;
                }

                // Master-detail delete-behavior check (Oracle: ON-CHECK-DELETE-MASTER,
                // isolated/non-isolated/cascading — DataBlockRelationship.DeleteBehavior).
                // Added 2026-08-22: neither the trigger nor the isolated/non-isolated/
                // cascading distinction existed anywhere in the engine before this —
                // deleting a master record never checked, blocked on, or cascaded to
                // its detail records at all.
                foreach (var relationship in GetActiveRelationships(blockName))
                {
                    // Deferred coordination means the detail block might not
                    // reflect the master's current record yet — force it
                    // current before counting. No-op for an Immediate
                    // relationship, which is already synced.
                    await SynchronizeDeferredDetailAsync(blockName, relationship.DetailBlockName).ConfigureAwait(false);

                    var checkOutcome = await FireOnCheckDeleteMasterAsync(
                        blockName, relationship.DetailBlockName, currentRecord).ConfigureAwait(false);
                    if (checkOutcome == null)
                    {
                        Status = $"Delete cancelled by ON-CHECK-DELETE-MASTER trigger in block '{blockName}' (detail '{relationship.DetailBlockName}')";
                        _messageManager?.ShowWarningMessage(blockName, Status);
                        return false;
                    }
                    if (checkOutcome == true)
                    {
                        // A registered handler decided — skip the default
                        // DeleteBehavior check for this relationship entirely.
                        continue;
                    }

                    var detailBlock = GetBlock(relationship.DetailBlockName);
                    var detailCount = detailBlock?.UnitOfWork?.TotalItemCount ?? 0;
                    if (detailCount == 0)
                        continue;

                    switch (relationship.DeleteBehavior)
                    {
                        case MasterDeleteBehavior.Isolated:
                            // Orphans allowed — nothing to check.
                            break;

                        case MasterDeleteBehavior.Cascading:
                            if (!await CascadeDeleteDetailRecordsAsync(relationship.DetailBlockName).ConfigureAwait(false))
                            {
                                Status = $"Cascading delete failed for detail block '{relationship.DetailBlockName}' — master delete in '{blockName}' aborted";
                                _messageManager?.ShowErrorMessage(blockName, Status);
                                return false;
                            }
                            await _triggerManager.FireBlockTriggerAsync(
                                TriggerType.OnClearDetails, relationship.DetailBlockName,
                                TriggerContext.ForBlock(TriggerType.OnClearDetails, relationship.DetailBlockName, null, _dmeEditor))
                                .ConfigureAwait(false);
                            break;

                        case MasterDeleteBehavior.NonIsolated:
                        default:
                            Status = $"Cannot delete: block '{blockName}' has {detailCount} detail record(s) in '{relationship.DetailBlockName}'";
                            _messageManager?.ShowErrorMessage(blockName, Status);
                            return false;
                    }
                }

                // Fire ON-LOCK. A registered handler replaces the default
                // client-side lock below. Added 2026-08-22 — closes the same
                // "enum member with no firing code" gap as ON-INSERT/UPDATE/DELETE.
                var onLockOutcome = await FireOnLockAsync(blockName, currentRecord).ConfigureAwait(false);
                if (onLockOutcome == null)
                {
                    Status = $"Delete cancelled by ON-LOCK trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }
                if (onLockOutcome == false)
                {
                    // No ON-LOCK registered — default path (Phase 7).
                    await _lockManager.AutoLockIfNeededAsync(blockName).ConfigureAwait(false);
                }

                // Fire WHEN-REMOVE-RECORD trigger (before the record is removed)
                var whenRemoveCtx = TriggerContext.ForBlock(TriggerType.WhenRemoveRecord, blockName, currentRecord, _dmeEditor);
                var whenRemoveResult = await _triggerManager.FireBlockTriggerAsync(TriggerType.WhenRemoveRecord, blockName, whenRemoveCtx).ConfigureAwait(false);
                if (whenRemoveResult == TriggerResult.Cancelled)
                {
                    Status = $"Delete cancelled by WHEN-REMOVE-RECORD trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }

                // Fire PRE-DELETE trigger
                var preDeleteCtx = TriggerContext.ForBlock(TriggerType.PreDelete, blockName, currentRecord, _dmeEditor);
                var preDeleteResult = await _triggerManager.FireBlockTriggerAsync(TriggerType.PreDelete, blockName, preDeleteCtx).ConfigureAwait(false);
                if (preDeleteResult == TriggerResult.Cancelled)
                {
                    Status = $"Delete cancelled by PRE-DELETE trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }

                // Fire ON-DELETE. A registered handler replaces the default
                // UnitOfWork.DeleteAsync call below — Oracle Forms' ON-DELETE is
                // exactly this: substitute logic for the physical DELETE. Added
                // 2026-08-22 — FireOnDeleteAsync existed (Phase 4.2) with zero
                // callers anywhere, so a registered ON-DELETE trigger could never
                // fire; this closes that gap.
                var onDeleteOutcome = await FireOnDeleteAsync(blockName, currentRecord).ConfigureAwait(false);
                if (onDeleteOutcome == null)
                {
                    Status = $"Delete cancelled by ON-DELETE trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }

                // Delete the current record. IUnitofWork (non-generic) declares
                // Task<IErrorsInfo> DeleteAsync(dynamic doc) directly — no reflection needed.
                // (The previous GetMethod("DeleteAsync").Invoke(...) was a silent-no-op trap
                //  if the method didn't exist: the whole delete path was skipped without a
                //  loud error. The direct call now either compiles or fails fast.)
                IErrorsInfo result;
                if (onDeleteOutcome == true)
                {
                    // Handled by the registered ON-DELETE trigger — the default
                    // delete must not also run, or the record would be deleted twice.
                    result = new ErrorsInfo { Flag = Errors.Ok, Message = "Handled by ON-DELETE trigger" };
                }
                else
                {
                    SuppressSync(blockName);
                    try
                    {
                        result = await blockInfo.UnitOfWork.DeleteAsync(currentRecord).ConfigureAwait(false);
                    }
                    finally { ResumeSync(blockName); }
                }

                if (result == null)
                {
                    Status = $"DeleteAsync returned null on unit of work for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                    return false;
                }

                if (result.Flag == Errors.Ok)
                {
                    Status = $"Record deleted successfully in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);

                    // Fire POST-DELETE trigger after successful delete
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.PostDelete, blockName,
                        TriggerContext.ForBlock(TriggerType.PostDelete, blockName, currentRecord, _dmeEditor)).ConfigureAwait(false);

                    await SynchronizeDetailBlocksAsync(blockName).ConfigureAwait(false);
                    return true;
                }
                else
                {
                    Status = $"Error deleting record: {result.Message}";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Status = $"Error deleting record in block '{blockName}': {ex.Message}";
                LogError($"Error deleting record in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes every record currently in a detail block, one at a time
        /// through its own full delete pipeline (<see cref="DeleteCurrentRecordAsync"/>)
        /// — so the detail's own triggers (WHEN-REMOVE-RECORD, PRE-DELETE,
        /// ON-LOCK, ON-DELETE, POST-DELETE), and any further Cascading
        /// relationship IT is a master of, all fire exactly as if a user had
        /// deleted each record by hand. Added 2026-08-22, for
        /// <see cref="MasterDeleteBehavior.Cascading"/>.
        /// </summary>
        /// <remarks>
        /// Bounded by the starting record count rather than looping on
        /// <c>TotalItemCount &gt; 0</c> unconditionally, and re-checks that the
        /// count actually decreased after each delete — a UoW whose current
        /// record does not advance after a delete, or that reports success
        /// without actually removing the record, fails this loudly instead of
        /// spinning forever.
        /// </remarks>
        private async Task<bool> CascadeDeleteDetailRecordsAsync(string detailBlockName)
        {
            var detailBlock = GetBlock(detailBlockName);
            if (detailBlock?.UnitOfWork == null) return true;

            var guard = detailBlock.UnitOfWork.TotalItemCount + 1;

            while (detailBlock.UnitOfWork.TotalItemCount > 0)
            {
                if (guard-- <= 0)
                {
                    LogError(
                        $"CascadeDeleteDetailRecordsAsync: '{detailBlockName}' still has " +
                        $"{detailBlock.UnitOfWork.TotalItemCount} record(s) after exhausting the " +
                        "expected delete count — the current-record pointer may not be advancing " +
                        "after delete. Aborting to avoid an infinite loop.",
                        null, detailBlockName);
                    return false;
                }

                var beforeCount = detailBlock.UnitOfWork.TotalItemCount;
                var deleted = await DeleteCurrentRecordAsync(detailBlockName).ConfigureAwait(false);
                if (!deleted)
                {
                    LogError(
                        $"CascadeDeleteDetailRecordsAsync: deleting a record in '{detailBlockName}' " +
                        $"failed or was cancelled; {beforeCount} record(s) remained",
                        null, detailBlockName);
                    return false;
                }

                if (detailBlock.UnitOfWork.TotalItemCount >= beforeCount)
                {
                    LogError(
                        $"CascadeDeleteDetailRecordsAsync: '{detailBlockName}' record count did not " +
                        $"decrease after a reported-successful delete ({beforeCount} -> " +
                        $"{detailBlock.UnitOfWork.TotalItemCount})",
                        null, detailBlockName);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Enters query mode for a block - equivalent to Oracle Forms ENTER_QUERY.
        /// </summary>
        /// <remarks>
        /// This is the bool-returning face of <see cref="EnterQueryModeAsync"/>, kept for
        /// callers that only need success/failure. Until 2026-08-01 it carried its own
        /// second implementation that just assigned <c>Mode = DataBlockMode.Query</c> —
        /// so whichever entry point a host happened to call decided whether unsaved-change
        /// validation, related-block validation, block clearing and the block-enter event
        /// ran at all. Hosts call this one, so in practice ENTER_QUERY did none of them.
        /// Keep the single implementation in ModeTransitions; do not reintroduce a
        /// mode assignment here.
        /// </remarks>
        public async Task<bool> EnterQueryAsync(string blockName)
        {
            try
            {
                var result = await EnterQueryModeAsync(blockName).ConfigureAwait(false);
                return result?.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Status = $"Error entering query mode for '{blockName}': {ex.Message}";
                LogError($"Error entering query mode for '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Builds the row-level security filter for a block
        /// (<see cref="BlockSecurity.RowFilterClause"/>/<c>.RowFilterValues</c>),
        /// parsed and ready to AND into a query's filter list the same way
        /// <c>DefaultWhereClause</c> already is.
        /// </summary>
        /// <remarks>
        /// <c>ISecurityManager.GetBlockRowFilter</c>/<c>GetBlockSecurity</c> existed
        /// with no caller anywhere in the engine — a block configured with a row
        /// filter (e.g. "TenantId = :TenantId", to restrict a user to their own
        /// tenant's rows) had that restriction stored and never enforced:
        /// <see cref="ExecuteQueryAsync"/> only ever checked the coarse
        /// query/insert/update/delete allow-flags via
        /// <see cref="EnforceBlockSecurity"/>, never the row filter, so a
        /// permitted user saw every row rather than only their own. (2026-08-22)
        /// </remarks>
        private List<AppFilter> BuildSecurityRowFilters(string blockName)
        {
            var security = _securityManager?.GetBlockSecurity(blockName);
            if (security == null || string.IsNullOrWhiteSpace(security.RowFilterClause))
                return null;

            var filters = _queryBuilderManager.ParseWhereClause(security.RowFilterClause);
            if (filters == null || filters.Count == 0) return null;

            if (security.RowFilterValues != null)
            {
                foreach (var filter in filters)
                {
                    // ParseWhereClause has no concept of a ":Name" bind
                    // placeholder — it parses "TenantId = :TenantId" as a
                    // literal FilterValue of ":TenantId". Resolve it against
                    // RowFilterValues here, the one place that dictionary is
                    // actually meant to be consumed per its own doc comment.
                    if (filter.FilterValue != null &&
                        filter.FilterValue.StartsWith(":", StringComparison.Ordinal) &&
                        security.RowFilterValues.TryGetValue(filter.FilterValue.Substring(1), out var value))
                    {
                        filter.FilterValue = value?.ToString() ?? string.Empty;
                    }
                }
            }

            return filters;
        }

        /// <summary>
        /// Executes query for a block - equivalent to Oracle Forms EXECUTE_QUERY.
        /// Merges block-level default WHERE clause with caller-supplied filters via QueryBuilder.
        /// </summary>
        public async Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null)
        {
            try
            {
                var block = GetBlock(blockName);

                // Phase 6: security check. This runs BEFORE the CRUD flag
                // guard below, and the order matters: SetBlockSecurity calls
                // ApplyAllSecurityFlags, which writes the policy INTO those same
                // CRUD flags. With the guard first, a security denial always
                // exited here and EnforceBlockSecurity never ran, so the denial
                // was honoured but never recorded — GetSecurityViolations stayed
                // empty for every real denial and the security panel showed an
                // empty audit trail. (2026-08-03)
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Query))
                {
                    Status = $"Security: query not permitted on block '{blockName}'";
                    return false;
                }

                if (block != null && !block.QueryAllowed)
                {
                    Status = $"Query not allowed for block '{blockName}'";
                    return false;
                }

                // Merge default WHERE clause from block metadata
                var finalFilters = filters;
                if (block != null && !string.IsNullOrWhiteSpace(block.DefaultWhereClause))
                {
                    var defaultFilters = _queryBuilderManager.ParseWhereClause(block.DefaultWhereClause);
                    finalFilters = _queryBuilderManager.CombineFiltersAnd(
                        finalFilters ?? new List<AppFilter>(), defaultFilters);
                }

                // Merge row-level security filter — see BuildSecurityRowFilters.
                var securityFilters = BuildSecurityRowFilters(blockName);
                if (securityFilters != null)
                {
                    finalFilters = _queryBuilderManager.CombineFiltersAnd(
                        finalFilters ?? new List<AppFilter>(), securityFilters);
                }

                var result = await ExecuteQueryEnhancedAsync(blockName, finalFilters).ConfigureAwait(false);
                if (result.Flag == Errors.Ok)
                {
                    Status = $"Query executed successfully for block '{blockName}'";
                    _messageManager?.ShowInfoMessage(blockName, Status);
                }
                else
                {
                    bool warningOutcome = IsQueryWarningOutcome(result);
                    Status = string.IsNullOrWhiteSpace(result.Message)
                        ? (warningOutcome
                            ? $"Query execution stopped for block '{blockName}'"
                            : $"Error executing query for block '{blockName}'")
                        : result.Message;

                    if (warningOutcome)
                    {
                        _messageManager?.ShowWarningMessage(blockName, Status);
                    }
                    else
                    {
                        _messageManager?.ShowErrorMessage(blockName, Status);
                    }
                }
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Status = $"Error executing query for '{blockName}': {ex.Message}";
                LogError($"Error executing query for '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Counts records matching the block's current query criteria —
        /// equivalent to Oracle Forms COUNT_QUERY. Merges the block's default
        /// WHERE clause with the supplied filters, the same way
        /// <see cref="ExecuteQueryAsync"/> does, and asks the datasource for a
        /// COUNT directly via <see cref="IDataSource.GetScalarAsync"/> — it
        /// does not fetch or otherwise disturb the block's currently loaded
        /// records, matching Oracle's own COUNT_QUERY contract.
        /// </summary>
        /// <returns>
        /// The matching record count, or -1 when the block/datasource/entity
        /// cannot be resolved or the datasource's GetScalarAsync throws (see
        /// <see cref="Status"/> and the log for why) — never a silent 0, which
        /// would read as "no matching records" instead of "could not count."
        /// </returns>
        public async Task<int> CountQueryAsync(string blockName, List<AppFilter> filters = null, CancellationToken ct = default)
        {
            try
            {
                var block = GetBlock(blockName);
                if (block == null)
                {
                    Status = $"Block '{blockName}' not found";
                    return -1;
                }

                var entityName = block.EntityStructure?.EntityName;
                if (string.IsNullOrWhiteSpace(entityName))
                {
                    Status = $"Block '{blockName}' has no entity name to count against";
                    return -1;
                }

                var ds = _dmeEditor.GetDataSource(block.DataSourceName);
                if (ds == null)
                {
                    Status = $"Block '{blockName}' has no open datasource '{block.DataSourceName}'";
                    return -1;
                }

                // Merge default WHERE clause with caller-supplied filters —
                // identical to ExecuteQueryAsync, so COUNT_QUERY and
                // EXECUTE_QUERY always agree on what "matches."
                var finalFilters = filters;
                if (!string.IsNullOrWhiteSpace(block.DefaultWhereClause))
                {
                    var defaultFilters = _queryBuilderManager.ParseWhereClause(block.DefaultWhereClause);
                    finalFilters = _queryBuilderManager.CombineFiltersAnd(
                        finalFilters ?? new List<AppFilter>(), defaultFilters);
                }

                // Merge row-level security filter — see BuildSecurityRowFilters.
                // Without this, COUNT_QUERY would report how many rows match
                // *ignoring* the same row-level restriction EXECUTE_QUERY
                // enforces, leaking the true row count to a user who isn't
                // permitted to see all of them.
                var securityFilters = BuildSecurityRowFilters(blockName);
                if (securityFilters != null)
                {
                    finalFilters = _queryBuilderManager.CombineFiltersAnd(
                        finalFilters ?? new List<AppFilter>(), securityFilters);
                }

                var whereClause = finalFilters != null && finalFilters.Count > 0
                    ? string.Join(" AND ", finalFilters.Select(TheTechIdea.Beep.Utils.Util.GenerateFilterExpression))
                    : null;

                var sql = string.IsNullOrWhiteSpace(whereClause)
                    ? $"SELECT COUNT(*) FROM {entityName}"
                    : $"SELECT COUNT(*) FROM {entityName} WHERE {whereClause}";

                var scalar = await ds.GetScalarAsync(sql).ConfigureAwait(false);
                var count = (int)scalar;
                Status = $"Query would return {count} record(s) for block '{blockName}'";
                return count;
            }
            catch (Exception ex)
            {
                Status = $"Error counting query for '{blockName}': {ex.Message}";
                LogError($"Error counting query for block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return -1;
            }
        }

        private static bool IsQueryWarningOutcome(IErrorsInfo? result)
        {
            if (result == null)
            {
                return false;
            }

            if (result.Flag == Errors.Warning || result.Flag == Errors.Information)
            {
                return true;
            }

            string message = result.Message ?? string.Empty;
            return message.IndexOf("cancelled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("validation failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("must be in Query mode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("already in Query mode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Post (validate and send to DB without committing). Oracle Forms POST equivalent.
        /// Validates and saves the current record to the underlying datasource but does NOT
        /// commit the transaction. In the current engine, this delegates to the UoW's Commit
        /// which performs a save + commit in one operation. Once IUnitofWork gains a dedicated
        /// PostAsync/ValidateAsync method, this will be updated to call it directly.
        /// </summary>
        public async Task<bool> PostBlockAsync(string blockName, CancellationToken ct = default)
        {
            var block = GetBlock(blockName);
            if (block == null) return false;
            var uow = block.UnitOfWork;
            if (uow == null) return false;

            try
            {
                var result = await uow.Commit(null, ct).ConfigureAwait(false);
                return result?.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                LogError($"PostBlockAsync failed for block '{blockName}'", ex, blockName);
                return false;
            }
        }

        #endregion
    }
}
