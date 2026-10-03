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
        #region Block Registration and Management

        /// <summary>
        /// Registers a data block with the manager using schema metadata already carried by the unit of work.
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork,
            string dataSourceName = null, bool isMasterBlock = false)
        {
            RegisterBlock(blockName, unitOfWork, null, dataSourceName, isMasterBlock);
        }

        /// <summary>
        /// Registers a data block with the manager
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure,
            string dataSourceName = null, bool isMasterBlock = false)
        {
            ValidateBlockRegistrationParameters(blockName, unitOfWork);
            using var preparation = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            RegistrationLease registration = null, previous = null;
            try
            {
                registration = ReserveRegistration(blockName, unitOfWork, out previous);
                blockName = registration.Name;
                if (previous != null && (_eventManager is not IGatedUnitOfWorkEventSubscriptions ||
                    _itemPropertyManager is not IPreparedBlockItems))
                    throw new NotSupportedException("Atomic replacement requires gated owned event subscriptions and prepared item registration.");

                var originalStructure = unitOfWork.EntityStructure;
                var originalSource = unitOfWork.DataSource;
                var resolved = ResolveRegistrationStructure(unitOfWork, entityStructure, dataSourceName, out var resolvedSource);
                if (resolved == null)
                    throw new ArgumentNullException(nameof(entityStructure), $"Block '{blockName}' requires entity metadata.");
                registration.Check();
                if (originalStructure == null && resolved is EntityStructure concrete)
                {
                    registration.Own($"Prepared UoW metadata: {blockName}", () =>
                    {
                        if (!registration.EverPublished && ReferenceEquals(unitOfWork.EntityStructure, concrete))
                            unitOfWork.EntityStructure = originalStructure;
                    });
                    unitOfWork.EntityStructure = concrete;
                }
                if (originalSource == null && resolvedSource != null)
                {
                    registration.Own($"Prepared UoW source: {blockName}", () =>
                    {
                        if (!registration.EverPublished && ReferenceEquals(unitOfWork.DataSource, resolvedSource))
                            unitOfWork.DataSource = originalSource;
                    });
                    unitOfWork.DataSource = resolvedSource;
                }
                registration.Check();

                var blockInfo = CreateBlockInfo(blockName, unitOfWork, resolved, dataSourceName, isMasterBlock);
                ApplyBlockConfiguration(blockInfo);
                blockInfo.IsRegistered = false;
                registration.Block = blockInfo;
                registration.Check();

                IBlockItemsRegistration preparedItems = null;
                if (_itemPropertyManager is IPreparedBlockItems prepared)
                {
                    preparedItems = prepared.PrepareBlockItems(_commitFormInstanceId, blockName, resolved)
                        ?? throw new InvalidOperationException("Item helper returned no prepared registration.");
                    registration.Own($"Block items: {blockName}", preparedItems.Dispose);
                }
                else
                {
                    registration.Own($"Legacy block items: {blockName}", () => _itemPropertyManager.ClearBlockItems(blockName));
                    _itemPropertyManager.RegisterItemsFromEntityStructure(blockName, resolved);
                }
                registration.Check();

                if (_eventManager is IOwnedUnitOfWorkEventSubscriptions owned)
                {
                    var lease = _eventManager is IGatedUnitOfWorkEventSubscriptions gated
                        ? gated.SubscribeOwned(unitOfWork, blockName, () => CanDispatchRegistration(registration))
                        : owned.SubscribeOwned(unitOfWork, blockName);
                    if (lease == null) throw new InvalidOperationException("Event helper returned no subscription lease.");
                    registration.Own($"Event helper lease: {blockName}", lease.Dispose);
                }
                else
                {
                    registration.Own($"Event helper: {blockName}", () => _eventManager.UnsubscribeFromUnitOfWorkEvents(unitOfWork, blockName));
                    _eventManager.SubscribeToUnitOfWorkEvents(unitOfWork, blockName);
                }
                registration.Check();

                EventHandler<ItemChangedEventArgs<Entity>> handler = async (s, e) =>
                {
                    using var callback = TryEnterCallback();
                    if (callback == null) return;
                    if (e == null) return;
                    if (!IsCurrentRegistration(blockName, blockInfo, unitOfWork)) return;
                    ObserveRecordChange(registration, e.Item, e.PropertyName);
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_operationLifetime.Token);
                    var ct = cancellation.Token;
                    RecordTarget target = null;
                    // Async so the LOV validation below can be properly
                    // awaited instead of fire-and-forgotten (see that
                    // block's comment). Wrapped in try/catch for the same
                    // reason mdHandler below is: an unhandled exception
                    // from an async-void event handler is unobservable —
                    // it never reaches the property setter that raised
                    // ItemChanged, so nothing here can be allowed to
                    // throw uncaught. (2026-08-22)
                    try
                    {
                    if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
                    target = CaptureRecordTarget(blockName, e.PropertyName, "Validation");
                    var isCurrentRecord = ReferenceEquals(target.Record, e.Item);
                    // Find the record's position without a dynamic call.
                    //
                    // This read `unitOfWork.Units.IndexOf(e.Item)` until
                    // 2026-08-01. `Units` is dynamic, so that was a runtime
                    // dispatch — and a dynamic call binds its *statically
                    // typed* arguments by their compile-time type, not their
                    // runtime one. `e.Item` is declared `Entity`, while the
                    // list is `ObservableBindingList<orders>` (or whatever
                    // the block's generated entity is), so the binder looked
                    // for `IndexOf(Entity)` on `Collection<orders>`, found
                    // only `IndexOf(orders)`, and threw RuntimeBinderException
                    // — every time, for every block, on every field change.
                    //
                    // The throw escaped through the property setter, so
                    // SetFieldValue reported failure and no edit ever reached
                    // a record. Non-generic IList.IndexOf(object) binds
                    // statically and takes the base type happily.
                    object units = unitOfWork.Units;
                    var idx = units is System.Collections.IList list
                        ? list.IndexOf(e.Item)
                        : -1;
                    // Read the new field value off the record. Note: the
                    // previous code used `typeof(Entity).GetProperty(...)`
                    // which assumed every record is an `Entity` and
                    // silently returned null for any non-Entity record
                    // (e.g. anonymous projections, EF Core entities,
                    // POCOs). RecordPropertyAccessor reads from the
                    // actual runtime type of e.Item, with a cached
                    // PropertyInfo lookup and a throttled warning on
                    // miss.
                    var newVal = RecordPropertyAccessor.GetValue(
                        e.Item,
                        e.PropertyName,
                        _dmeEditor);
                    VerifyRecordTarget(target, ct);

                    OnBlockFieldChanged?.Invoke(this, new BlockFieldChangedEventArgs
                    {
                        BlockName   = blockName,
                        FieldName   = e.PropertyName,
                        NewValue    = newVal,
                        RecordIndex = idx
                    });
                    VerifyRecordTarget(target, ct);

                    // Keep the block change feed for noncurrent edits, but never annotate the current item's store with them.
                    if (!isCurrentRecord) return;

                    if (!string.IsNullOrWhiteSpace(e.PropertyName))
                    {
                        // Mark the item dirty.
                        //
                        // IItemPropertyManager.MarkItemDirty existed and had
                        // no caller anywhere in the engine, so item-level
                        // dirty state was never set: GetDirtyItems always
                        // returned empty and anything showing "which fields
                        // changed" — WinFormDirtyStatePanel among them — was
                        // permanently blank. Record-level dirty tracking (the
                        // unit of work) was unaffected, which is why this went
                        // unnoticed. ItemChangedEventArgs carries no previous
                        // value, so the old value is recorded as unknown
                        // rather than invented. (2026-08-02)
                        _itemPropertyManager?.MarkItemDirty(blockName, e.PropertyName, null);
                        VerifyRecordTarget(target, ct);
                        _systemVariablesManager?.SetBlockStatus(blockName, "CHANGED");
                        VerifyRecordTarget(target, ct);
                        _systemVariablesManager?.SetRecordStatus(blockName, "CHANGED");
                        VerifyRecordTarget(target, ct);

                        PrepareValidationContext(blockName);
                        VerifyRecordTarget(target, ct);
                        var itemValidation = _validationManager.ValidateItem(
                            blockName, e.PropertyName, newVal, ValidationTiming.OnChange);
                        VerifyRecordTarget(target, ct);

                        // SetItemError/ClearItemError existed with no caller
                        // anywhere in the engine for the field/record
                        // rule-based path (G0.25 wired it for LOV only) —
                        // HasItemError/GetItemErrorMessage/GetItemsWithErrors
                        // could never report true no matter what a
                        // registered ValidationRule found wrong.
                        // ValidationFailed/ValidationCompleted still fired
                        // correctly as .NET events the whole time (a host
                        // subscribed directly was unaffected); this closes
                        // the gap for the per-item error *store* those
                        // events were never wired into. Not yet cleared here
                        // if invalid — the LOV check below composes with
                        // this result rather than overwriting it blindly.
                        // (2026-08-22)
                        if (!itemValidation.IsValid)
                        {
                            _itemPropertyManager?.SetItemError(
                                blockName, e.PropertyName, itemValidation.FirstError ?? "Validation failed");
                        }

                        VerifyRecordTarget(target, ct);
                        var hasLov = _lovManager.HasLOV(blockName, e.PropertyName);
                        VerifyRecordTarget(target, ct);
                        if (hasLov)
                        {
                            VerifyRecordTarget(target, ct);
                            var capturedLov = _lovManager.GetLOV(blockName, e.PropertyName);
                            // WHEN-LOV-VALIDATION never fired here before
                            // this fix — only ShowLOVAsync (explicit LOV
                            // invocation) fired it, so a form author
                            // relying on the trigger for its far more
                            // common use (validating a *typed* value
                            // against the LOV) was silently unserved.
                            // Fire it first so a registered handler can
                            // reject the value outright; otherwise fall
                            // through to the default list-membership
                            // check. Both outcomes now also update the
                            // item's error state — SetItemError/
                            // ClearItemError existed with no caller
                            // anywhere in the engine, so HasItemError/
                            // GetItemErrorMessage/GetItemsWithErrors could
                            // never report true no matter what failed.
                            // The result was previously discarded
                            // (`_ = ValidateLOVValueAsync(...)`) — the
                            // LOVValidationFailed .NET event still fired
                            // as a side effect, but nothing awaited the
                            // call, so an exception inside it (e.g. the
                            // LOV's datasource erroring) was an unobserved
                            // task exception, silently dropped. (2026-08-22)
                            var lovCtx = TriggerContext.ForItem(
                                TriggerType.WhenLOVValidation, blockName, e.PropertyName, null, newVal, _dmeEditor);
                            var lovTriggerResult = await _triggerManager
                                .FireBlockTriggerAsync(TriggerType.WhenLOVValidation, blockName, lovCtx, ct)
                                .ConfigureAwait(false);
                            VerifyRecordTarget(target, ct);
                            if (!ReferenceEquals(_lovManager.GetLOV(blockName, e.PropertyName), capturedLov)) return;
                            VerifyRecordTarget(target, ct);

                            if (lovTriggerResult != TriggerResult.Success && lovTriggerResult != TriggerResult.Skipped)
                            {
                                _itemPropertyManager?.SetItemError(
                                    blockName, e.PropertyName, $"Value rejected by WHEN-LOV-VALIDATION trigger: {lovTriggerResult}");
                            }
                            else
                            {
                                var lovValidation = await _lovManager
                                    .ValidateLOVValueAsync(blockName, e.PropertyName, newVal)
                                    .ConfigureAwait(false);
                                VerifyRecordTarget(target, ct);
                                if (!ReferenceEquals(_lovManager.GetLOV(blockName, e.PropertyName), capturedLov)) return;
                                VerifyRecordTarget(target, ct);

                                if (lovValidation.IsValid)
                                {
                                    // LOV passing must not mask a genuine
                                    // rule failure this same change already
                                    // recorded above — only clear when
                                    // BOTH checks agree the value is good.
                                    if (itemValidation.IsValid)
                                        _itemPropertyManager?.ClearItemError(blockName, e.PropertyName);
                                }
                                else
                                {
                                    _itemPropertyManager?.SetItemError(
                                        blockName, e.PropertyName,
                                        lovValidation.ErrorMessage ?? "Value not found in List of Values");
                                }
                            }
                        }
                        else if (itemValidation.IsValid)
                        {
                            // No LOV attached — the rule-based result above
                            // is the whole story for this item.
                            _itemPropertyManager?.ClearItemError(blockName, e.PropertyName);
                        }
                    }
                    }
                    catch (SupersededRecordOperationException) { }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                    catch (Exception ex)
                    {
                        if (RecordTargetCurrentSafely(target, ct))
                            CleanupAction($"Item callback error delivery: {blockName}", () => _eventManager.TriggerError(blockName, ex));
                        else CleanupAction($"Retired item callback: {blockName}", () => throw ex);
                    }
                };
                registration.Own($"ItemChanged: {blockName}", () => unitOfWork.ItemChanged -= handler);
                unitOfWork.ItemChanged += handler;
                registration.Check();

                EventHandler mdHandler = async (s, e) =>
                {
                    using var callback = TryEnterCallback();
                    if (callback == null) return;
                    if (!IsCurrentRegistration(blockName, blockInfo, unitOfWork)) return;
                    ObserveRecordChange(registration);
                    // B7 (audit pass 3, 2026-06): the
                    // previous version was an async-void
                    // event handler with no try/catch. A
                    // throw from SynchronizeDetailBlocksAsync
                    // would be unobserved (the await is in a
                    // fire-and-forget void-returning lambda),
                    // and the event subscriber pipeline would
                    // never see it. Now: try/catch and route
                    // to the error event so the host gets a
                    // diagnostic.
                    try
                    {
                        if (!IsSyncSuppressed(blockName) && GetDetailBlocks(blockName).Any())
                            await SynchronizeDetailBlocksAsync(blockName).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (IsCurrentRegistration(blockName, blockInfo, unitOfWork))
                            CleanupAction($"Detail callback error delivery: {blockName}", () => _eventManager.TriggerError(blockName, ex));
                    }
                };
                registration.Own($"CurrentChanged: {blockName}", () => unitOfWork.CurrentChanged -= mdHandler);
                unitOfWork.CurrentChanged += mdHandler;
                registration.Check();

                registration.Own($"Block cache: {blockName}", () => RetireRegistrationCache(registration, previous));
                _performanceManager.CacheBlockInfo(blockName, blockInfo);
                registration.Check();
                PrepareInitialBufferAuthorization(registration);
                if (preparedItems != null) preparedItems.Commit(() => PublishRegistration(registration, previous));
                else PublishRegistration(registration, previous);

                // Published registration is not undone by an observer/cleanup failure.
                try { ApplyRegistrationSecurityFlags(registration); }
                catch (Exception ex) { RecordPolicyNotificationFailure(ex); }
                previous?.CleanupIfRetired();
                if (registration.Published && !_disposed)
                {
                    lock (_registrationGate)
                        if (registration.Published && !_disposed) Status = $"Block '{blockName}' registered successfully";
                    if (previous != null)
                        CleanupAction($"Replacement leave: {blockName}", () => _eventManager.TriggerBlockLeave(blockName));
                    if (registration.Published && !_disposed)
                        CleanupAction($"Current block variables: {blockName}", () => _systemVariablesManager.UpdateForBlockChange(_currentBlockName));
                    if (registration.Published && !_disposed)
                        CleanupAction($"Block enter: {blockName}", () => _eventManager.TriggerBlockEnter(blockName));
                    if (registration.Published && !_disposed)
                        CleanupAction($"Registration log: {blockName}", () => LogOperation($"Block '{blockName}' registered", blockName));
                }
            }
            catch (Exception ex)
            {
                var alreadyRetired = registration?.Retired == true;
                if (registration != null && !registration.EverPublished) registration.MarkRetired();
                if (registration != null && !alreadyRetired && !_disposed)
                {
                    Status = $"Error registering block '{blockName}': {ex.Message}";
                    CleanupAction($"Registration failure log: {blockName}", () => LogError($"Error registering block '{blockName}'", ex, blockName));
                    CleanupAction($"Registration failure notification: {blockName}", () => _eventManager.TriggerError(blockName, ex));
                }
                throw;
            }
            finally
            {
                if (registration != null)
                {
                    registration.FinishPreparation();
                    EndRegistration(registration);
                    previous = null;
                    CleanupAction("Registration current-block variables", ClearOwnedCurrentBlockVariables);
                }
            }
        }

        /// <summary>
        /// Registers a block by resolving UoW + EntityStructure from connection name and entity name.
        /// Uses BlockFactory to create the block, then delegates to RegisterBlock.
        /// </summary>
        public async Task<bool> RegisterBlockFromSourceAsync(
            string blockName, string connectionName, string entityName,
            bool isMasterBlock = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockName) ||
                string.IsNullOrWhiteSpace(connectionName) ||
                string.IsNullOrWhiteSpace(entityName))
                return false;

            try
            {
                var (uow, structure) = await _blockFactory
                    .CreateBlockAsync(connectionName, entityName, ct)
                    .ConfigureAwait(false);

                if (uow == null || structure == null)
                {
                    Status = $"Block '{blockName}': could not resolve '{connectionName}.{entityName}'";
                    return false;
                }

                RegisterBlock(blockName, uow, structure, connectionName, isMasterBlock);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"RegisterBlockFromSourceAsync failed for '{blockName}'", ex, blockName);
                return false;
            }
        }

        /// <summary>
        /// Creates a named savepoint for the specified block capturing current record index.
        /// Convenience wrapper around Savepoints.CreateSavepoint (Phase 6).
        /// </summary>
        public string CreateBlockSavepoint(string blockName, string savepointName = null)
        {
            var block = GetBlock(blockName);
            if (block?.UnitOfWork == null)
                return null;

            bool isDirty = block.UnitOfWork.IsDirty;
            int recordCount = block.UnitOfWork.TotalItemCount;
            int recordIndex = 0;
            try
            {
                var current = block.UnitOfWork.CurrentItem;
                if (current != null && block.UnitOfWork.Units != null)
                    recordIndex = block.UnitOfWork.Units.IndexOf(current);
            }
            catch (Exception ex)
            {
                // Units may be null before first query, or the indexer
                // may throw if the backing collection is in an
                // inconsistent state. Either way, the record-index
                // field falls back to 0 (the start of the block).
                LogError($"CreateBlockSavepoint: failed to resolve record index for block '{blockName}'", ex, blockName);
            }

            // Capture the record snapshot. Per-property failures inside
            // RecordPropertyAccessor are caught and logged individually;
            // the catch here is for failures in the dictionary
            // construction itself (e.g. non-generic IDictionary with
            // exotic key types) that the accessor can't handle.
            IDictionary<string, object> snapshot;
            try
            {
                snapshot = CaptureCurrentRecordSnapshot(block.UnitOfWork.CurrentItem);
            }
            catch (Exception ex)
            {
                LogError($"CreateBlockSavepoint: failed to capture record snapshot for block '{blockName}'", ex, blockName);
                snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            return _savepointManager.CreateSavepoint(
                blockName, savepointName, recordIndex, recordCount, isDirty, snapshot);
        }

        /// <summary>
        /// Rolls back a block to a previously created savepoint (Phase 6).
        /// </summary>
        /// <remarks>
        /// Ordering matters here. The savepoint store is mutated LAST,
        /// after the data rollback succeeds, so a partial failure in
        /// the data rollback leaves the savepoint store intact and
        /// the user can retry. If the data rollback fails after the
        /// store was mutated, the user would lose their other
        /// savepoints with no way to recover.
        ///
        /// Flow:
        /// <list type="number">
        ///   <item>Look up the savepoint (read-only).</item>
        ///   <item>Roll back the unit of work (data side).</item>
        ///   <item>Move to the saved record index.</item>
        ///   <item>Restore the record snapshot (best-effort).</item>
        ///   <item>Tell the manager to delete later savepoints (store side).</item>
        /// </list>
        /// </remarks>
        public async Task<bool> RollbackToSavepointAsync(string blockName, string savepointName,
            CancellationToken ct = default)
        {
            // 1. Look up the savepoint. Read-only — no state mutation.
            var savepoint = _savepointManager.ListSavepoints(blockName)
                .FirstOrDefault(sp => string.Equals(sp.Name, savepointName, StringComparison.OrdinalIgnoreCase));

            if (savepoint == null)
                return false;

            var block = GetBlock(blockName);
            var unitOfWork = block?.UnitOfWork;

            if (unitOfWork == null)
            {
                LogError(
                    $"RollbackToSavepointAsync: block '{blockName}' has no unit of work; cannot restore data. " +
                    "Returning true (savepoint will be removed from the store below) is misleading — this is a no-op rollback.",
                    null, blockName);
                // Still proceed to clean the store so the user isn't left
                // with a savepoint they can never roll back to. The
                // alternative (returning false and leaving the store
                // intact) is also defensible; we choose to clean up
                // because a savepoint with no data to roll back to is
                // strictly worse than a silent no-op.
                _ = await _savepointManager.RollbackToSavepointAsync(blockName, savepointName, ct).ConfigureAwait(false);
                return true;
            }

            // 2. Roll back the unit of work. If this fails, we DO NOT
            // touch the savepoint store — the user can retry the
            // rollback.
            var rollbackResult = await unitOfWork.Rollback().ConfigureAwait(false);
            if (rollbackResult?.Flag == Errors.Failed)
            {
                LogError(
                    $"RollbackToSavepointAsync: unit-of-work rollback failed for block '{blockName}' savepoint '{savepointName}'",
                    null, blockName);
                return false;
            }

            // 3. Move to the saved record index. We bound-check first
            // because the index may be out of range if records were
            // deleted between savepoint and rollback. The bound-check
            // is logged but not fatal — a record-count mismatch is a
            // legitimate "the data shape changed" scenario.
            if (savepoint.RecordIndex >= 0 &&
                unitOfWork.TotalItemCount > 0 &&
                savepoint.RecordIndex < unitOfWork.TotalItemCount)
            {
                unitOfWork.MoveTo(savepoint.RecordIndex);
            }
            else if (savepoint.RecordIndex >= 0)
            {
                LogError(
                    $"RollbackToSavepointAsync: saved record index {savepoint.RecordIndex} is out of range for current TotalItemCount {unitOfWork.TotalItemCount} in block '{blockName}'",
                    null, blockName);
            }

            // 4. Restore the record snapshot (best-effort; per-property
            // failures are logged via RecordPropertyAccessor.LogRestoreFailure).
            RestoreCurrentRecordSnapshot(unitOfWork.CurrentItem, savepoint.RecordSnapshot);
            TryUpdateSavepointSystemVariables(blockName, savepoint.RecordIndex, unitOfWork.TotalItemCount);

            // 5. Data rollback succeeded; only NOW delete the later
            // savepoints. If this step throws, the data is in the
            // rolled-back state and the user can manually call
            // ReleaseSavepoint to clean up.
            var rolledBack = await _savepointManager.RollbackToSavepointAsync(blockName, savepointName, ct)
                .ConfigureAwait(false);

            return rolledBack;
        }

        private IDictionary<string, object> CaptureCurrentRecordSnapshot(object currentRecord)
        {
            if (currentRecord == null)
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (currentRecord is IDictionary<string, object> typedDictionary)
                return new Dictionary<string, object>(typedDictionary, StringComparer.OrdinalIgnoreCase);

            if (currentRecord is IDictionary dictionary)
            {
                var snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key == null)
                        continue;

                    snapshot[entry.Key.ToString()] = entry.Value;
                }

                return snapshot;
            }

            // Use RecordPropertyAccessor so the snapshot is built from the
            // same cached PropertyInfo catalog as the rest of FormsManager.
            // The catalog is seeded on first use, so the first snapshot of
            // a record type is a dict walk (not a reflection scan).
            // Promoted from `static` to instance so the accessor can
            // receive `_dmeEditor` for diagnostic logging on read failure.
            // Return type loosened from Dictionary<,> to IDictionary<,>
            // because the accessor returns IDictionary<,> (the catalog
            // produces case-insensitive Dictionary<,> instances, but the
            // caller signature only requires IDictionary).
            return RecordPropertyAccessor.GetAllReadable(currentRecord, _dmeEditor);
        }

        private void RestoreCurrentRecordSnapshot(object currentRecord, IReadOnlyDictionary<string, object> snapshot)
        {
            if (currentRecord == null || snapshot == null || snapshot.Count == 0)
                return;

            if (currentRecord is IDictionary<string, object> typedDictionary)
            {
                foreach (var entry in snapshot)
                    typedDictionary[entry.Key] = entry.Value;

                return;
            }

            if (currentRecord is IDictionary dictionary)
            {
                foreach (var entry in snapshot)
                    dictionary[entry.Key] = entry.Value;

                return;
            }

            // Walk the writable surface of currentRecord through
            // RecordPropertyAccessor so the PropertyInfo cache is shared
            // with get/snapshot. The accessor already filters to
            // non-indexed, publicly-writable properties. The value
            // conversion is delegated to ConvertSnapshotValue (it has
            // special enum/DateTime handling that RecordPropertyAccessor's
            // generic path doesn't replicate).
            //
            // Note: we call property.SetValue directly instead of
            // RecordPropertyAccessor.TrySetValue because TrySetValue uses
            // the generic Convert.ChangeType path, which doesn't have
            // the enum/DateTime special cases. The trade-off is that
            // SetValue failures are caught here and logged via the
            // accessor's diagnostic rather than swallowed silently
            // (audit pass 2026-06: previous version caught silently).
            foreach (var property in RecordPropertyAccessor.EnumerateWritableProperties(currentRecord))
            {
                if (!snapshot.TryGetValue(property.Name, out var value))
                    continue;

                try
                {
                    if (value == null)
                    {
                        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                            continue;

                        property.SetValue(currentRecord, null);
                        continue;
                    }

                    property.SetValue(currentRecord, ConvertSnapshotValue(value, property.PropertyType));
                }
                catch (Exception ex)
                {
                    // Best-effort restore only. Some projected/read-only
                    // properties may not be writable, or the value in
                    // the snapshot may be incompatible with the property
                    // type after ConvertSnapshotValue's best-effort
                    // conversion. Log via the accessor's diagnostic so
                    // the failure is visible (throttled). Pass the
                    // PropertyInfo so the diagnostic can surface the
                    // target type (otherwise it would print "type ?").
                    RecordPropertyAccessor.LogRestoreFailure(_dmeEditor, currentRecord, property.Name, property, ex);
                }
            }
        }

        private static object ConvertSnapshotValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effectiveType.IsInstanceOfType(value))
                return value;

            if (effectiveType.IsEnum)
            {
                if (value is string enumName)
                    return Enum.Parse(effectiveType, enumName, ignoreCase: true);

                return Enum.ToObject(effectiveType, value);
            }

            return Convert.ChangeType(value, effectiveType);
        }

        private void TryUpdateSavepointSystemVariables(string blockName, int recordIndex, int totalRecords)
        {
            try
            {
                _systemVariablesManager?.UpdateForRecordChange(blockName, recordIndex, totalRecords);
                // Same choke-point reasoning as the two calls in
                // FormsManager.Navigation.cs (G0.60 in gaps.md): a savepoint
                // rollback changes the current record just as much as an
                // ordinary navigation does, and LockManager's own index
                // tracking needs to follow it the same way.
                _lockManager.SetCurrentRecordIndex(blockName, recordIndex);
            }
            catch
            {
                // Savepoint rollback should not fail because system-variable refresh is unavailable.
            }
        }

        /// <summary>
        /// Opens the named datasource if needed, fetches EntityStructure, creates a UnitOfWork,
        /// and registers the block. This is the single-call bootstrap entry point for UI layers
        /// (BeepForms, BeepBlock) that must never touch IDataSource directly.
        /// Delegates to <see cref="RegisterBlockFromSourceAsync"/>.
        /// </summary>
        public Task<bool> SetupBlockAsync(
            string blockName,
            string connectionName,
            string entityName,
            bool isMasterBlock = false,
            CancellationToken cancellationToken = default)
            => RegisterBlockFromSourceAsync(blockName, connectionName, entityName, isMasterBlock, cancellationToken);

        /// <summary>
        /// Unregisters a data block from the manager
        /// </summary>
        public bool UnregisterBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;
            RegistrationLease live, pending;
            var reservedHere = false;
            lock (_registrationGate)
            {
                if (_disposed) return false;
                _pendingRegistrations.TryGetValue(blockName, out pending);
                if (pending?.Retired == true && !_registrations.ContainsKey(blockName)) return false;
                _registrations.Remove(blockName, out live);
                if (live == null && pending == null) return false;
                pending?.MarkRetired();
                live?.MarkRetired();
                _blocks.TryRemove(blockName, out _);
                if (string.Equals(_currentBlockName, blockName, StringComparison.OrdinalIgnoreCase)) _currentBlockName = null;
                if (pending == null)
                {
                    _pendingRegistrations[blockName] = live;
                    reservedHere = true;
                }
            }
            try
            {
                var success = pending?.CleanupIfRetired() ?? true;
                if (live != null)
                {
                    success = CleanupAction($"Block leave: {live.Name}", () => _eventManager.TriggerBlockLeave(live.Name)) && success;
                    success = live.CleanupIfRetired() && success;
                }
                success = CleanupAction($"Relationships: {blockName}", () => RemoveBlockRelationships(blockName)) && success;
                success = CleanupAction($"Navigation history: {blockName}", () => _navHistoryManager.RemoveBlock(blockName)) && success;
                _syncSuppressCount.TryRemove(blockName, out _);
                _pendingDeferredSync.TryRemove(blockName, out _);
                if (!_disposed) Status = success ? $"Block '{blockName}' unregistered successfully" : $"Block '{blockName}' removed with cleanup failures";
                success = CleanupAction("Retired current-block variables", ClearOwnedCurrentBlockVariables) && success;
                CleanupAction($"Unregister log: {blockName}", () => LogOperation(Status, blockName));
                return success;
            }
            finally { if (reservedHere) EndRegistration(live); }
        }

        /// <summary>
        /// Gets a registered block with performance caching
        /// </summary>
        public DataBlockInfo GetBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return null;
            DataBlockInfo block;
            lock (_registrationGate)
                if (_disposed || !_blocks.TryGetValue(blockName, out block)) return null;

            // Try cache first
            var cachedBlock = _performanceManager.GetCachedBlockInfo(blockName);
            if (ReferenceEquals(cachedBlock, block))
                return IsCurrentRegistration(blockName, block, block.UnitOfWork) ? cachedBlock : null;
            
            // Cache for future access
            if (block != null)
            {
                _performanceManager.CacheBlockInfo(blockName, block);
            }
            
            return IsCurrentRegistration(blockName, block, block.UnitOfWork) ? block : null;
        }

        /// <summary>
        /// Gets the unit of work for a specific block
        /// </summary>
        public IUnitofWork GetUnitOfWork(string blockName)
        {
            return GetBlock(blockName)?.UnitOfWork;
        }

        /// <summary>
        /// Checks if a block exists
        /// </summary>
        public bool BlockExists(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;
            lock (_registrationGate) return !_disposed && _blocks.ContainsKey(blockName);
        }

        #endregion
    }
}
