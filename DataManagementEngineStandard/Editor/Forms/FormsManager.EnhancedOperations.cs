using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Enhanced operations partial class for UnitofWorksManager
    /// Provides enhanced operations and error recovery with mode transition awareness
    /// </summary>
    public partial class FormsManager
    {
      
        #region Enhanced Data Operations
        /// <summary>
        /// Creates a new record for a block using proper type resolution
        /// Automatically handles mode transition if needed
        /// </summary>
        public object CreateNewRecord(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.EntityStructure == null)
                {
                    Status = $"Block '{blockName}' not found or has no entity structure";
                    return null;
                }

                // CRITICAL: Ensure block is in appropriate mode for new record creation
                if (blockInfo.Mode == DataBlockMode.Query)
                {
                    LogOperation($"Block '{blockName}' is in Query mode, transitioning to CRUD mode for new record", blockName);
                    // Transition to CRUD mode asynchronously - but since this method is sync, 
                    // we'll log and continue with a warning
                    Status = $"Warning: Block '{blockName}' should be in CRUD mode for new record creation";
                }

                // Use class creator to create new instance based on entity structure
                var entityType = blockInfo.EntityType ?? ResolveBlockEntityType(blockInfo.UnitOfWork, blockInfo.EntityStructure, blockInfo.DataSourceName);
                if (entityType != null && blockInfo.EntityType == null)
                    blockInfo.EntityType = entityType;

                if (entityType == null)
                {
                    Status = $"Cannot create new record for block '{blockName}' because no CLR entity type is registered or discoverable";
                    LogOperation(Status, blockName);
                    return null;
                }

                if (entityType.IsAbstract || entityType.IsInterface)
                {
                    Status = $"Cannot create new record for block '{blockName}' because CLR entity type '{entityType.FullName}' is not instantiable";
                    LogOperation(Status, blockName);
                    return null;
                }

                var newRecord = Activator.CreateInstance(entityType);
                _formsSimulationHelper.SetAuditDefaults(newRecord, Environment.UserName);

                // Populate item-authored values before the trigger fires, so
                // WHEN-CREATE-RECORD logic sees (and can still override) them —
                // matching Oracle Forms' own order. Three sources, applied
                // least- to most-specific so a later one can override an
                // earlier one for the same field:
                //   1. DEFAULT_VALUE (ItemInfo.DefaultValue) — static, authored
                //      per field (directly or via a Property Class).
                //   2. "Copy Value from Item" — seeds from another item's
                //      current value (e.g. a detail record picking up its
                //      master's key).
                //   3. Registered item-default factories (SetItemDefault) —
                //      programmatic/computed defaults (e.g. sequences).
                // All three existed with no caller before this: ItemInfo had
                // DefaultValue and ItemPropertyManager had ApplyDefaultValues,
                // but nothing invoked it on record creation; ApplyItemDefaults
                // was documented as "called internally from CreateNewRecord"
                // yet had zero callers anywhere. (2026-08-22)
                _itemPropertyManager.ApplyDefaultValues(blockName, newRecord);
                ApplyCopyValueFromItem(blockName, newRecord);
                ApplyItemDefaults(blockName, newRecord);

                // Fire WHEN-CREATE-RECORD trigger after the instance is created
                _triggerManager.FireBlockTrigger(
                    TriggerType.WhenCreateRecord, blockName,
                    TriggerContext.ForBlock(TriggerType.WhenCreateRecord, blockName, newRecord, _dmeEditor));

                LogOperation($"New record created for block '{blockName}' using '{entityType.FullName}'", blockName);
                return newRecord;
            }
            catch (Exception ex)
            {
                Status = $"Error creating new record for block '{blockName}': {ex.Message}";
                LogError($"Error creating new record for block '{blockName}'", ex, blockName);
                return null;
            }
        }

        /// <summary>
        /// Applies each item's authored "Copy Value from Item" — Oracle Forms'
        /// declarative counterpart to the COPY built-in. Reads the source
        /// item's CURRENT value from the item-property store (not the record
        /// it came from — the item store is where a host keeps it in sync as
        /// the user navigates), so a freshly-created record can pick up
        /// e.g. its master block's key without the caller wiring a trigger
        /// for it.
        /// </summary>
        private void ApplyCopyValueFromItem(string blockName, object record)
        {
            if (record == null) return;

            var items = _itemPropertyManager.GetAllItems(blockName);
            if (items.Count == 0) return;

            var recordType = record.GetType();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.CopyValueFromItem) || string.IsNullOrWhiteSpace(item.BoundProperty))
                    continue;

                var parts = item.CopyValueFromItem.Split('.');
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    LogOperation(
                        $"Item '{item.ItemName}' has an invalid CopyValueFromItem '{item.CopyValueFromItem}' (expected \"Block.Item\"); skipped.",
                        blockName);
                    continue;
                }

                var sourceValue = _itemPropertyManager.GetItemValue(parts[0], parts[1]);
                if (sourceValue == null) continue;

                var property = recordType.GetProperty(item.BoundProperty,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (property == null || !property.CanWrite) continue;

                try
                {
                    var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    property.SetValue(record, Convert.ChangeType(sourceValue, targetType));
                }
                catch (Exception ex)
                {
                    LogError($"Error applying CopyValueFromItem for item '{item.ItemName}'", ex, blockName);
                }
            }
        }

        /// <summary>
        /// Enhanced insert operation with mode transition validation and better type safety
        /// </summary>
        public async Task<IErrorsInfo> InsertRecordEnhancedAsync(string blockName, object record = null)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // Phase 6: security check. This runs BEFORE the CRUD flag
                // guard below, and the order matters: SetBlockSecurity calls
                // ApplyAllSecurityFlags, which writes the policy INTO those same
                // CRUD flags. With the guard first, a security denial always
                // exited here and EnforceBlockSecurity never ran, so the denial
                // was honoured but never recorded — GetSecurityViolations stayed
                // empty for every real denial and the security panel showed an
                // empty audit trail. (2026-08-03)
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Insert))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Security: insert not permitted on block '{blockName}'";
                    return result;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.InsertAllowed)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Insert not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, result.Message);
                    return result;
                }

                // CRITICAL: Validate mode transition before insert
                if (blockInfo.Mode != DataBlockMode.CRUD)
                {
                    LogOperation($"Block '{blockName}' not in CRUD mode, attempting transition", blockName);
                    
                    var modeTransitionResult = await EnterCrudModeForNewRecordAsync(blockName).ConfigureAwait(false);
                    if (modeTransitionResult.Flag != Errors.Ok)
                    {
                        result.Flag = modeTransitionResult.Flag;
                        result.Message = $"Cannot insert: Mode transition failed - {modeTransitionResult.Message}";
                        return result;
                    }
                }

                // Check for unsaved changes in current and related blocks
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Insert cancelled due to unsaved changes";
                    return result;
                }

                // Create new record if not provided
                if (record == null)
                {
                    record = CreateNewRecord(blockName);
                    if (record == null)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Cannot create new record for block '{blockName}'";
                        return result;
                    }
                }

                // Validate record before insert
                // NO record validation here.
                //
                // This is CREATE_RECORD, not INSERT. Oracle Forms creates an
                // empty record in the block for the user to fill and validates it
                // at COMMIT; validating at creation time makes a blank record
                // impossible, which is to say it makes creating a record
                // impossible on any table with a required column. Validation
                // still runs on the way to the datasource. (2026-08-02)

                // Fire WHEN-DATABASE-RECORD — unrestricted in Oracle Forms, so
                // a notification, not a cancellation gate. Oracle fires this
                // the moment a record transitions into being a database
                // record: on fetch for a queried row, or — for a new record —
                // right after it validates and BEFORE Forms performs the
                // physical INSERT, which puts it ahead of PRE-INSERT in the
                // firing order (WHEN-DATABASE-RECORD announces the
                // transition; PRE-INSERT is the "about to insert" gate that
                // follows it). This engine's own PRE-INSERT already fires
                // here, at add-to-block time, not at the true
                // physical-commit moment (the class-level comment above
                // explains why: CREATE_RECORD stages the record in the
                // block, the physical INSERT happens later via
                // SaveDirtyBlocksAsync/UnitOfWork.Commit()) — firing
                // WHEN-DATABASE-RECORD immediately before it is the closest
                // honest match this engine's pipeline supports without a
                // second, separate firing pass over the commit path.
                // TriggerType.WhenDatabaseRecord existed nowhere before this
                // (2026-08-24).
                await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.WhenDatabaseRecord, blockName,
                    TriggerContext.ForBlock(TriggerType.WhenDatabaseRecord, blockName, record, _dmeEditor))
                    .ConfigureAwait(false);

                // Fire PRE-INSERT trigger — abort if cancelled
                var preInsertResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreInsert, blockName,
                    TriggerContext.ForBlock(TriggerType.PreInsert, blockName, record, _dmeEditor)).ConfigureAwait(false);
                if (preInsertResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Insert cancelled by PRE-INSERT trigger in block '{blockName}'";
                    return result;
                }

                // Add the record to the BLOCK. Do not write it to the datasource.
                //
                // This called UnitOfWork.InsertAsync until 2026-08-02, which
                // writes immediately — so creating a record wrote a blank row to
                // the database on the spot, and on any table with a NOT NULL
                // column it simply failed ("constraint failed"). You could not
                // create a record at all against a real schema.
                //
                // Oracle Forms' CREATE_RECORD adds an empty record to the block;
                // the INSERT happens at COMMIT once the user has filled it in.
                // UnitOfWork.Add is exactly that: it applies insert defaults,
                // generates the primary-key sequence, and adds to Units marked
                // Added, which the commit path already picks up through
                // CommitAllAsync's insert branch.
                //
                // Add returns void and can decline silently (its own Validateall
                // guard), so the effect is checked rather than assumed.
                var countBefore = GetBlockCount(blockName);

                SuppressSync(blockName);
                try
                {
                    blockInfo.UnitOfWork.Add(record);
                }
                finally { ResumeSync(blockName); }

                var added = GetBlockCount(blockName) > countBefore;

                if (added)
                {
                    // Fire POST-INSERT trigger after successful insert
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.PostInsert, blockName,
                        TriggerContext.ForBlock(TriggerType.PostInsert, blockName, record, _dmeEditor)).ConfigureAwait(false);

                    await SynchronizeDetailBlocksAsync(blockName).ConfigureAwait(false);
                    result.Message = "Record created in block; it is written on commit";
                    Status = $"Record created in block '{blockName}'";
                    LogOperation($"Record created in block '{blockName}'", blockName);
                }
                else
                {
                    result.Flag = Errors.Failed;
                    result.Message =
                        $"The record was not added to block '{blockName}'. UnitOfWork.Add " +
                        "declined it — its Validateall guard returns without reporting.";
                    Status = result.Message;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error inserting record in block '{blockName}': {ex.Message}";
                LogError($"Error inserting record in block '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Enhanced update operation with mode validation for current record
        /// </summary>
        public async Task<IErrorsInfo> UpdateCurrentRecordAsync(string blockName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // Phase 6: security check. This runs BEFORE the CRUD flag
                // guard below, and the order matters: SetBlockSecurity calls
                // ApplyAllSecurityFlags, which writes the policy INTO those same
                // CRUD flags. With the guard first, a security denial always
                // exited here and EnforceBlockSecurity never ran, so the denial
                // was honoured but never recorded — GetSecurityViolations stayed
                // empty for every real denial and the security panel showed an
                // empty audit trail. (2026-08-03)
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Update))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Security: update not permitted on block '{blockName}'";
                    return result;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.UpdateAllowed)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, result.Message);
                    return result;
                }

                // CRITICAL: Must be in CRUD mode to update
                if (blockInfo.Mode != DataBlockMode.CRUD)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' must be in CRUD mode to update records. Current mode: {blockInfo.Mode}";
                    return result;
                }

                var currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"No current record to update in block '{blockName}'";
                    return result;
                }

                // Fire ON-LOCK. A registered handler replaces the default
                // client-side lock below. Added 2026-08-22 — closes the same
                // "enum member with no firing code" gap as ON-INSERT/UPDATE/DELETE.
                var onLockOutcome = await FireOnLockAsync(blockName, currentRecord).ConfigureAwait(false);
                if (onLockOutcome == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update cancelled by ON-LOCK trigger in block '{blockName}'";
                    return result;
                }
                if (onLockOutcome == false)
                {
                    // No ON-LOCK registered — default path (Phase 7).
                    await _lockManager.AutoLockIfNeededAsync(blockName).ConfigureAwait(false);
                }

                // Validate record before update
                if (!ValidateRecordForOperation(blockName, currentRecord, "UPDATE"))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Record validation failed for update in block '{blockName}'";
                    return result;
                }

                // Update audit fields through FormsSimulationHelper
                _formsSimulationHelper.SetFieldValue(currentRecord, "ModifiedDate", DateTime.Now);
                _formsSimulationHelper.SetFieldValue(currentRecord, "ModifiedBy", Environment.UserName);

                // Fire PRE-UPDATE trigger — abort if cancelled
                var preUpdateResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreUpdate, blockName,
                    TriggerContext.ForBlock(TriggerType.PreUpdate, blockName, currentRecord, _dmeEditor)).ConfigureAwait(false);
                if (preUpdateResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update cancelled by PRE-UPDATE trigger in block '{blockName}'";
                    return result;
                }

                // Fire ON-UPDATE. A registered handler replaces the default
                // UnitOfWork.UpdateAsync call below — Oracle Forms' ON-UPDATE is
                // exactly this: substitute logic for the physical UPDATE. Added
                // 2026-08-22 — FireOnUpdateAsync existed (Phase 4.2) with zero
                // callers anywhere, so a registered ON-UPDATE trigger could never
                // fire; this closes that gap.
                var onUpdateOutcome = await FireOnUpdateAsync(blockName, currentRecord).ConfigureAwait(false);
                if (onUpdateOutcome == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update cancelled by ON-UPDATE trigger in block '{blockName}'";
                    return result;
                }

                IErrorsInfo updateResult;
                if (onUpdateOutcome == true)
                {
                    // Handled by the registered ON-UPDATE trigger — the default
                    // update must not also run, or the record would be written twice.
                    updateResult = new ErrorsInfo { Flag = Errors.Ok, Message = "Handled by ON-UPDATE trigger" };
                }
                else
                {
                    // No ON-UPDATE registered — default path. IUnitofWork
                    // (non-generic) declares UpdateAsync(dynamic doc) directly.
                    // Direct dispatch — same rationale as the InsertAsync change above.
                    updateResult = await blockInfo.UnitOfWork.UpdateAsync(currentRecord).ConfigureAwait(false);
                }

                if (updateResult == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"UpdateAsync returned null on unit of work for block '{blockName}'";
                    return result;
                }

                if (updateResult.Flag == Errors.Ok)
                {
                    // Fire POST-UPDATE trigger after successful update
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.PostUpdate, blockName,
                        TriggerContext.ForBlock(TriggerType.PostUpdate, blockName, currentRecord, _dmeEditor)).ConfigureAwait(false);

                    await SynchronizeDetailBlocksAsync(blockName).ConfigureAwait(false);
                    result.Message = "Record updated successfully";
                    Status = $"Record updated successfully in block '{blockName}'";
                    LogOperation($"Record updated successfully in block '{blockName}'", blockName);
                }
                else
                {
                    result.Flag = updateResult.Flag;
                    result.Message = updateResult.Message;
                    result.Ex = updateResult.Ex;
                    Status = $"Error updating record: {updateResult.Message}";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error updating record in block '{blockName}': {ex.Message}";
                LogError($"Error updating record in block '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Enhanced query execution with proper mode transition handling
        /// This method now properly handles Query->Execute->CRUD mode transition
        /// </summary>
        public async Task<IErrorsInfo> ExecuteQueryEnhancedAsync(string blockName, List<AppFilter> filters = null)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // CRITICAL: This method should handle the Query->CRUD transition
                // If not in Query mode, first transition to Query mode
                if (blockInfo.Mode != DataBlockMode.Query)
                {
                    LogOperation($"Block '{blockName}' not in Query mode, entering Query mode first", blockName);
                    
                    var queryModeResult = await EnterQueryModeAsync(blockName).ConfigureAwait(false);
                    if (queryModeResult.Flag != Errors.Ok)
                    {
                        result.Flag = queryModeResult.Flag;
                        result.Message = $"Cannot execute query: Failed to enter Query mode - {queryModeResult.Message}";
                        return result;
                    }
                }

                // Fire PRE-QUERY trigger — abort if cancelled
                var preQueryResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.PreQuery, blockName, null, _dmeEditor)).ConfigureAwait(false);
                if (preQueryResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Query cancelled by PRE-QUERY trigger in block '{blockName}'";
                    return result;
                }

                // IUnitofWork (non-generic) declares Get(List<AppFilter>) and Get() directly.
                // Direct dynamic dispatch — the previous GetMethod("Get").Invoke(...) was
                // a silent-no-op trap if the method didn't exist.
                if (filters != null && filters.Any())
                {
                    await blockInfo.UnitOfWork.Get(filters).ConfigureAwait(false);
                }
                else
                {
                    await blockInfo.UnitOfWork.Get().ConfigureAwait(false);
                }

                // :SYSTEM.LAST_QUERY -- see G0.36 in gaps.md. DataSourceAppFilterExtensions
                // (DataManagementModelsStandard/Extensions) already builds a parameterized
                // SELECT statement from an AppFilter list -- no new serialization needed.
                // Best-effort: an unresolvable data source (bad DataSourceName, block not yet
                // wired to a real IDataSource in a test) leaves LAST_QUERY at its prior value
                // rather than failing the query that already succeeded above.
                var queryDataSource = string.IsNullOrWhiteSpace(blockInfo.DataSourceName)
                    ? null
                    : _dmeEditor?.GetDataSource(blockInfo.DataSourceName);
                if (queryDataSource != null)
                {
                    var entityNameForQuery = blockInfo.EntityStructure?.EntityName ?? blockName;
                    var queryDefinition = queryDataSource.BuildSelectQueryDefinition(entityNameForQuery, filters);
                    _systemVariablesManager?.SetLastQuery(queryDefinition.QueryText);
                }

                // :SYSTEM.BLOCK_STATUS / :SYSTEM.RECORD_STATUS -- see G0.36 in gaps.md.
                // A record just fetched by a query and not yet touched is Oracle Forms'
                // "QUERY" status. Set unconditionally on a successful Get (whether or not
                // it found rows), the same simplification SetMode already makes at this
                // site -- ItemChanged (the "CHANGED" transition) takes over the moment a
                // field on it is actually edited.
                _systemVariablesManager?.SetBlockStatus(blockName, "QUERY");
                _systemVariablesManager?.SetRecordStatus(blockName, "QUERY");

                // CRITICAL: After successful query execution, transition to CRUD mode
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;
                // :SYSTEM.MODE -- see G0.36 in gaps.md.
                _systemVariablesManager?.SetMode(ToSystemVariableMode(DataBlockMode.CRUD));

                // Fire POST-QUERY trigger (before returning to caller)
                await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PostQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.PostQuery, blockName, null, _dmeEditor)).ConfigureAwait(false);

                var recordCount = GetRecordCount(blockName);
                
                result.Message = $"Query executed successfully. {recordCount} records found.";
                Status = $"Query executed successfully for block '{blockName}'. {recordCount} records.";
                LogOperation($"Query executed successfully for block '{blockName}' with {recordCount} records", blockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error executing query for '{blockName}': {ex.Message}";
                LogError($"Error executing query for '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Gets the current record for a block
        /// </summary>
        public object GetCurrentRecord(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                return blockInfo?.UnitOfWork?.CurrentItem;
            }
            catch (Exception ex)
            {
                LogError($"Error getting current record for block '{blockName}'", ex, blockName);
                return null;
            }
        }

        /// <summary>
        /// Gets the record count for a block
        /// </summary>
        public int GetRecordCount(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork?.Units != null)
                {
                    // Units is `dynamic`. The runtime type is ObservableBindingList<T>
                    // which extends BindingList<T> : Collection<T> with a Count property.
                    // The previous GetProperty("Count") reflection was a silent-no-op
                    // trap on a custom Units implementation; dynamic dispatch is louder.
                    dynamic d = blockInfo.UnitOfWork.Units;
                    int? count = d.Count as int?;
                    return count ?? 0;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting record count for block '{blockName}'", ex, blockName);
                return 0;
            }
        }
        #endregion

        #region Enhanced Validation Operations

        /// <summary>
        /// Validates a record for a specific operation with mode awareness
        /// </summary>
        private bool ValidateRecordForOperation(string blockName, object record, string operation)
        {
            try
            {
                if (record == null)
                {
                    LogError($"Cannot perform {operation}: Record is null", null, blockName);
                    return false;
                }

                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    LogError($"Cannot perform {operation}: Block info not found", null, blockName);
                    return false;
                }

                // Check mode compatibility
                if (operation == "INSERT" || operation == "UPDATE" || operation == "DELETE")
                {
                    if (blockInfo.Mode != DataBlockMode.CRUD)
                    {
                        LogError($"Cannot perform {operation}: Block must be in CRUD mode", null, blockName);
                        return false;
                    }
                }

                // Fire WHEN-VALIDATE-RECORD before the rule pass.
                //
                // This trigger had no fire point anywhere in the engine until
                // 2026-08-02 — it existed in the enum and in TriggerLibrary's
                // catalogue and was never invoked, so a form could register it
                // and never have it run. This is where Oracle Forms validates a
                // record: on the way into INSERT, UPDATE and DELETE. Cancelling
                // fails the record, matching the PRE-QUERY / PRE-INSERT
                // convention used at the other fire sites.
                //
                // Fired synchronously because this method is synchronous. A sync
                // Handler is safe on any thread; a trigger supplying only an
                // AsyncHandler gets a clear exception from
                // TriggerDefinition.Execute rather than a deadlock.
                var recordTriggerResult = _triggerManager.FireBlockTrigger(
                    TriggerType.WhenValidateRecord, blockName,
                    TriggerContext.ForBlock(
                        TriggerType.WhenValidateRecord, blockName, record, _dmeEditor));

                if (recordTriggerResult == TriggerResult.Cancelled)
                {
                    LogError(
                        $"{operation} cancelled by WHEN-VALIDATE-RECORD trigger",
                        null, blockName);
                    return false;
                }

                // Use existing validation logic
                var isValid = ValidateBlock(blockName);

                if (!isValid)
                {
                    LogError($"Record validation failed for {operation} operation", null, blockName);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                LogError($"Error validating record for {operation} operation", ex, blockName);
                return false;
            }
        }

        #endregion

        #region Field Operations (Using FormsSimulationHelper)
        /// <summary>
        /// Copies field values between records
        /// </summary>
        public bool CopyFields(object sourceRecord, object targetRecord, params string[] FieldNames)
        {
            if (sourceRecord == null || targetRecord == null)
                return false;

            try
            {
                var success = true;
                foreach (var FieldName in FieldNames)
                {
                    var value = _formsSimulationHelper.GetFieldValue(sourceRecord, FieldName);
                    if (!_formsSimulationHelper.SetFieldValue(targetRecord, FieldName, value))
                    {
                        success = false;
                        LogError($"Failed to copy field '{FieldName}'", null);
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                LogError("Error copying fields between records", ex);
                return false;
            }
        }

        /// <summary>
        /// Applies audit defaults to a record (delegates to <see cref="SetAuditDefaults"/>).
        /// </summary>
        [Obsolete("Use SetAuditDefaults instead — this is a duplicate kept for backward compatibility.")]
        public void ApplyAuditDefaults(object record, string currentUser = null)
        {
            SetAuditDefaults(record, currentUser);
        }

        #endregion

        #region Private Helper Methods for Enhanced Operations
        // FindBestInsertMethod and FindBestUpdateMethod were removed: the engine now
        // calls IUnitofWork.InsertAsync / UpdateAsync directly via dynamic dispatch.
        // The reflection-based method-resolution helpers were a silent-no-op trap
        // (returning null when the typed overload didn't exist) and have been
        // superseded by the direct call.

        #endregion
    }
}