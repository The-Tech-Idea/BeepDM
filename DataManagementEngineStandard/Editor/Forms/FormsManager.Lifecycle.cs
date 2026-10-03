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
        #region IDisposable Implementation

        /// <summary>
        /// Releases helper subscriptions, cached block state, and timer resources held by the FormsManager instance.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
            try
            {
                RegistrationLease[] registrations;
                lock (_registrationGate)
                {
                    _disposed = true;
                    registrations = _registrations.Values.Concat(_pendingRegistrations.Values).Distinct().ToArray();
                    foreach (var registration in registrations) registration.MarkRetired();
                    _blocks.Clear();
                    _registrations.Clear();
                    _pendingRegistrations.Clear();
                    _detailSyncSuppressions.Clear();
                    _currentBlockName = null;
                }
                CleanupAction("Managed operation lifetime cancellation", _operationLifetime.Cancel);
                foreach (var registration in registrations) registration.CleanupIfRetired();
                CleanupAction("Closed current-block variables", ClearOwnedCurrentBlockVariables);
                CleanupAction("Dirty notifications", () => _dirtyStateManager.OnUnsavedChanges -= OnUnsavedChangesHandler);
                CleanupAction("Security notifications", () => _securityManager.OnSecurityViolation -= OnSecurityViolationHandler);
                if (_observedSecurityPolicy != null)
                    CleanupAction("Security policy notifications", () => _observedSecurityPolicy.SecurityPolicyChanged -= OnSecurityPolicyChanged);
                CleanupAction("Audit notifications", () => OnBlockFieldChanged -= HandleBlockFieldChangedForAudit);
                CleanupAction("Trigger notifications", DisposeTriggerChaining);
                CleanupAction("Bus notifications", () => _messageBus.OnFormMessage -= OnMessageBusFormMessage);
                // UnsubscribeAll(formName) can remove another owner's same-name subscriptions.
                DetachOwnedMessages();
                CleanupAction("Timer notifications", () => _timerManager.TimerFired -= OnTimerManagerFired);
                if (_ownsTimerManager) CleanupAction("Owned timers", _timerManager.Dispose);
                if (_ownsPerformanceManager) CleanupAction("Owned performance helper", _performanceManager.Dispose);
                if (_ownsItemPropertyManager && _itemPropertyManager is IDisposable items)
                    CleanupAction("Owned item helper", items.Dispose);
                lock (_lockObject) _relationships.Clear();
                _pendingDeferredSync.Clear();
                _syncSuppressCount.Clear();
                CleanupAction("Disposal log", () => LogOperation("UnitofWorksManager disposed"));
            }
            finally
            {
                try { _readResourcesRetired = RetireReadResourcesAfterDrain(); }
                finally { _disposeCompleted.TrySetResult(); }
            }
        }

        /// <summary>Recent cleanup failures (at most 128); cleanup never stops at the first failure.</summary>
        public IReadOnlyList<FormsCleanupFailure> CleanupFailures => _cleanupFailures.ToArray();

        private bool CleanupAction(string resource, Action cleanup)
        {
            try { cleanup(); return true; }
            catch (Exception ex)
            {
                _cleanupFailures.Enqueue(new FormsCleanupFailure(resource, ex));
                while (_cleanupFailures.Count > 128) _cleanupFailures.TryDequeue(out _);
                try { LogError($"Cleanup failed: {resource}", ex); } catch { }
                return false;
            }
        }

        private bool IsCurrentRegistration(string name, DataBlockInfo block, IUnitofWork source)
        {
            lock (_registrationGate)
                return !_disposed && _registrations.TryGetValue(name, out var registration) && registration.Published &&
                    !registration.Retired && ReferenceEquals(registration.Block, block) &&
                    ReferenceEquals(registration.Source, source) && ReferenceEquals(block.UnitOfWork, source);
        }

        #endregion

        #region Protected Helper Methods (For Partial Classes)

        /// <summary>
        /// Loads configuration and wires manager-level event handlers required after construction.
        /// </summary>
        protected void InitializeManager()
        {
            try
            {
                // Load configuration
                _configurationManager.LoadConfiguration();
                
                // Subscribe to dirty state events
                _dirtyStateManager.OnUnsavedChanges += OnUnsavedChangesHandler;
                
                LogOperation("UnitofWorksManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError("Error initializing UnitofWorksManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Handles timer fire events from TimerManager by firing the
        /// WHEN-TIMER-EXPIRED trigger on the current form.
        /// </summary>
        private async void OnTimerManagerFired(object sender, TimerFiredEventArgs e)
        {
            using var callback = TryEnterCallback();
            if (callback == null) return;
            if (_disposed || e == null) return;
            // Was `_ = _triggerManager.FireFormTriggerAsync(...)` — fire-and-forget
            // on an async Task, inside a synchronous event handler. The try/catch
            // only ever observed a *synchronous* throw from building the call; an
            // exception from the trigger's own execution (e.g. a registered
            // WHEN-TIMER-EXPIRED handler throwing) became an unobserved task
            // exception, never reaching LogError. Same defect shape as the
            // ItemChanged handler's LOV validation fix (2026-08-22) — awaiting it
            // here, in an async-void handler with the same try/catch, is the
            // established fix for exactly this pattern in this class.
            try
            {
                var ctx = Forms.Models.TriggerContext.ForForm(
                    Forms.Models.TriggerType.WhenTimerExpired, _currentFormName ?? string.Empty, _dmeEditor);
                ctx.Parameters["TimerName"] = e.TimerName;
                ctx.Parameters["FireCount"] = e.FireCount;
                await _triggerManager.FireFormTriggerAsync(
                    Forms.Models.TriggerType.WhenTimerExpired, _currentFormName ?? string.Empty, ctx)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CleanupAction("Timer callback diagnostic", () => LogError($"Error handling timer fired for '{e.TimerName}'", ex));
            }
        }

        /// <summary>
        /// Relays shared-bus messages addressed to this form through the per-form event surface.
        /// </summary>
        /// <param name="sender">Shared message bus instance.</param>
        /// <param name="e">Delivered message payload.</param>
        private async void OnMessageBusFormMessage(object sender, FormMessageEventArgs e)
        {
            using var callback = TryEnterCallback();
            if (callback == null) return;
            if (_disposed) return;
            // async void + try/catch: the WHEN-FORM-NOTIFICATION fire below is
            // genuinely async, and this is a plain event handler subscribed via
            // += — same hazard, same established fix as OnTimerManagerFired
            // (G0.28) and the ItemChanged handler (G0.25). An unhandled
            // exception from an async-void handler is unobservable otherwise.
            try
            {
                var message = e?.Message;
                var currentFormName = _currentFormName;

                if (message == null || string.IsNullOrWhiteSpace(currentFormName))
                    return;

                if (string.Equals(message.TargetForm, currentFormName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(message.TargetForm, "*", StringComparison.OrdinalIgnoreCase))
                {
                    OnFormMessage?.Invoke(this, e);
                    if (_disposed) return;

                    // Fire WHEN-FORM-NOTIFICATION — the trigger counterpart to
                    // the plain .NET event just above, for a form author using
                    // the Oracle-named trigger the IDE's own Add Trigger picker
                    // already offered with no matching engine member until now
                    // (2026-08-24).
                    var ctx = TriggerContext.ForForm(TriggerType.WhenFormNotification, currentFormName, _dmeEditor);
                    ctx.Parameters["SenderForm"] = message.SenderForm;
                    ctx.Parameters["MessageType"] = message.MessageType;
                    ctx.Parameters["Payload"] = message.Payload;
                    await _triggerManager.FireFormTriggerAsync(
                        TriggerType.WhenFormNotification, currentFormName, ctx).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                CleanupAction("Bus callback diagnostic", () => LogError("Error handling form-bus notification", ex, _currentFormName));
            }
        }

        /// <summary>
        /// Default handler for unsaved-change notifications raised by the dirty-state manager.
        /// </summary>
        /// <param name="sender">Dirty-state manager instance.</param>
        /// <param name="e">Unsaved-change event payload.</param>
        protected void OnUnsavedChangesHandler(object sender, UnsavedChangesEventArgs e)
        {
            if (_disposed) return;
            // This can be overridden by derived classes or handled by event subscribers
            // Default behavior could be to show a dialog or log the event
            LogOperation($"Unsaved changes detected in block '{e.BlockName}' with {e.DirtyBlocks.Count} affected blocks");
        }

        #endregion
    }
}
