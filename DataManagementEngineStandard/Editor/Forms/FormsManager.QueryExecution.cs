using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager : IFormsQueryOutcomes, IFormsProviderPaging
    {
        private sealed class SupersededQueryException : InvalidOperationException
        {
            internal SupersededQueryException() : base("Managed query request, registration or mode was superseded.") { }
        }

        public Task<FormQueryResult> ExecuteQueryWithOutcomeAsync(string blockName, List<AppFilter> filters = null,
            CancellationToken cancellationToken = default) => ExecuteManagedQueryAsync(blockName, filters, cancellationToken, false);

        public Task<FormQueryResult> FetchPageWithOutcomeAsync(string blockName, BoundedPageRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return ExecuteManagedQueryAsync(blockName, request.CopyFilters(), cancellationToken, false, request);
        }

        private void VerifyQueryRegistration(ManagedReadPlan plan)
        {
            lock (_registrationGate)
            {
                if (_disposed || !_registrations.TryGetValue(plan.QueryRegistration.Name, out var current) ||
                    !ReferenceEquals(current, plan.QueryRegistration) || current.Retired || !current.Published ||
                    current.QueryRevision != plan.QueryRevision || !ReferenceEquals(current.Block, plan.Block) ||
                    !ReferenceEquals(current.Source, plan.Unit) || !ReferenceEquals(plan.Block.UnitOfWork, plan.Unit) ||
                    (plan.QueryMode.HasValue && plan.Block.Mode != plan.QueryMode.Value))
                    throw new SupersededQueryException();
            }
        }

        private bool CanDeliverQueryNotification(ManagedReadPlan plan, CancellationToken token)
        {
            if (plan == null || token.IsCancellationRequested) return false;
            try { VerifyQueryRegistration(plan); return true; }
            catch (SupersededQueryException) { return false; }
        }

        private ManagedReadPlan CaptureQueryCompletionPlan(FormQueryResult result, DataBlockInfo block, IUnitofWork unit)
        {
            lock (_registrationGate)
            {
                if (_disposed || !_registrations.TryGetValue(result.BlockName, out var registration) ||
                    !ReferenceEquals(registration.Block, block) || !ReferenceEquals(registration.Source, unit)) return null;
                return new ManagedReadPlan { Block = block, Unit = unit, QueryRegistration = registration,
                    QueryRevision = result.RequestRevision, QueryMode = DataBlockMode.CRUD };
            }
        }

        private static void AddQueryNotificationFailure(FormQueryResult result, Exception error) =>
            result.NotificationFailures = result.NotificationFailures.Concat(new[] { error }).ToArray();

        private FormQueryResult ReportRejectedQuery(FormQueryResult result, ManagedReadPlan plan, CancellationToken token,
            bool basicNotifications)
        {
            ObserveQueryNotification(result, plan, token, () => Status = result.Message);
            if (basicNotifications)
                ObserveQueryNotification(result, plan, token, () =>
                {
                    if (result.State is FormQueryState.BlockedDirty or FormQueryState.Cancelled || IsQueryWarningOutcome(result))
                        _messageManager?.ShowWarningMessage(result.BlockName, result.Message);
                    else _messageManager?.ShowErrorMessage(result.BlockName, result.Message);
                });
            return result;
        }

        private static FormQueryResult CancelQuery(FormQueryResult result, Exception error)
        {
            result.State = result.RecordsPublished ? FormQueryState.Completed : FormQueryState.Cancelled;
            result.Flag = result.RecordsPublished ? Errors.Ok : Errors.Failed;
            if (result.RecordsPublished) AddQueryNotificationFailure(result, error);
            else { result.Message = "Managed query cancelled before acknowledged acceptance."; result.Ex = error; }
            return result;
        }

        private void ObserveQueryNotification(FormQueryResult result, ManagedReadPlan plan, CancellationToken token, Action action)
        {
            if (!CanDeliverQueryNotification(plan, token)) return;
            try { action(); }
            catch (Exception ex) { AddQueryNotificationFailure(result, ex); }
        }

        private async Task ObserveQueryTriggerAsync(FormQueryResult result, ManagedReadPlan plan,
            TriggerType trigger, CancellationToken token)
        {
            if (!CanDeliverQueryNotification(plan, token)) return;
            try
            {
                var response = await _triggerManager.FireBlockTriggerAsync(trigger, result.BlockName,
                    TriggerContext.ForBlock(trigger, result.BlockName, null, _dmeEditor), token).ConfigureAwait(false);
                if (response == TriggerResult.Cancelled)
                    AddQueryNotificationFailure(result, new InvalidOperationException($"{trigger} cannot undo an acknowledged query."));
            }
            catch (Exception ex) { AddQueryNotificationFailure(result, ex); }
        }

        private async Task<FormQueryResult> ExecuteManagedQueryAsync(string blockName, List<AppFilter> filters,
            CancellationToken callerToken, bool basicNotifications, BoundedPageRequest page = null)
        {
            callerToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("Block name is required.", nameof(blockName));
            if (_managedReadDepth.Value != 0)
                throw new InvalidOperationException("A managed read trigger/provider cannot await a nested query/detail read. Schedule it after the current operation.");
            using var lifetime = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken, _operationLifetime.Token);
            var ct = cancellation.Token;
            var result = new FormQueryResult { FormInstanceId = _commitFormInstanceId, BlockName = blockName, Flag = Errors.Failed };
            ManagedReadPlan plan = null;
            var acquired = false;
            _managedReadDepth.Value++;
            try
            {
                RegistrationLease registration;
                long revision;
                lock (_registrationGate)
                {
                    ct.ThrowIfCancellationRequested();
                    if (_disposed || !_registrations.TryGetValue(blockName, out registration) || registration.Retired || !registration.Published)
                        throw new InvalidOperationException($"Block '{blockName}' not found or has no unit of work.");
                    revision = ++registration.QueryRevision;
                }
                result.RequestRevision = revision;
                result.RegistrationId = registration.Identity;
                result.BlockName = blockName = registration.Name;
                plan = new ManagedReadPlan { Block = registration.Block, Unit = registration.Source,
                    QueryRegistration = registration, QueryRevision = revision };
                plan = BuildManagedReadPlan(blockName, filters);
                plan.QueryRegistration = registration;
                plan.QueryRevision = revision;
                if (page != null) ConfigureManagedPage(plan, page);
                var wasInEnterQueryMode = plan.Block.Mode == DataBlockMode.EnterQuery;
                var reader = plan.Unit as IStagedUnitofWorkRead;
                var pageReader = plan.Unit as IStagedUnitofWorkPageRead;
                if (page != null && pageReader?.SupportsStagedPageRead != true)
                    throw new NotSupportedException("Managed provider pages require a bounded staged UoW/provider; unbounded fallback is forbidden.");
                result.UsedStaging = page != null || reader?.SupportsStagedRead == true;
                if (result.UsedStaging) plan.QueryMode = plan.Block.Mode;
                VerifyManagedReadPlan(blockName, plan);
                if (result.UsedStaging && _securityManager is not IQuerySecurityPublication)
                    throw new NotSupportedException("Staged managed queries require a revision-gated security helper.");

                // Capture policy, filters, registration and dirty-protection targets before queueing.
                Dictionary<string, DetailNode> targets = null;
                DetailNode root = null;
                if (result.UsedStaging)
                {
                    targets = new(StringComparer.OrdinalIgnoreCase);
                    root = CaptureDetailGraph(blockName, null, targets, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
                await _managedReadGate.WaitAsync(ct).ConfigureAwait(false);
                acquired = true;
                ct.ThrowIfCancellationRequested();
                VerifyManagedReadPlan(blockName, plan);
                if (result.UsedStaging)
                {
                    if (HasDirtyDetailSubtree(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    {
                        result.State = FormQueryState.BlockedDirty;
                        result.Message = "Query target or captured detail has unsaved changes. Save or explicitly discard before querying.";
                        return ReportRejectedQuery(result, plan, ct, basicNotifications);
                    }
                    // Implicit execution is not ENTER_QUERY: keep records/mode until acceptance.
                    // Explicit EnterQueryModeAsync still clears criteria buffers as before.
                    if (plan.Block.Mode != DataBlockMode.Query && plan.Block.Mode != DataBlockMode.EnterQuery)
                    {
                        foreach (var target in targets.Values)
                        {
                            ct.ThrowIfCancellationRequested();
                            VerifyManagedReadPlan(blockName, plan);
                            VerifyDetailNode(target);
                            if (target.Unit.CurrentItem != null && !ValidateRecordForModeTransition(target.Name))
                                throw new InvalidOperationException($"Cannot execute query: record validation failed for '{target.Name}'.");
                        }
                    }
                }
                else if (plan.Block.Mode != DataBlockMode.Query && plan.Block.Mode != DataBlockMode.EnterQuery)
                {
                    var entered = await EnterQueryModeCoreAsync(blockName, () =>
                    { ct.ThrowIfCancellationRequested(); VerifyManagedReadPlan(blockName, plan); }, ct).ConfigureAwait(false);
                    if (entered.Flag != Errors.Ok)
                        throw new InvalidOperationException($"Cannot execute query: Failed to enter Query mode - {entered.Message}");
                }

                ct.ThrowIfCancellationRequested();
                VerifyManagedReadPlan(blockName, plan);
                var preQuery = await _triggerManager.FireBlockTriggerAsync(TriggerType.PreQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.PreQuery, blockName, null, _dmeEditor), ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                VerifyManagedReadPlan(blockName, plan);
                if (preQuery == TriggerResult.Cancelled)
                {
                    result.State = FormQueryState.Cancelled;
                    result.Message = $"Query cancelled by PRE-QUERY trigger in block '{blockName}'.";
                    return ReportRejectedQuery(result, plan, ct, basicNotifications);
                }

                if (result.UsedStaging)
                {
                    if (HasDirtyDetailSubtree(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    {
                        result.State = FormQueryState.BlockedDirty;
                        result.Message = "Captured query target became dirty before provider execution.";
                        return ReportRejectedQuery(result, plan, ct, basicNotifications);
                    }
                    using var stage = page == null ? await reader.PrepareReadAsync(plan.Filters, ct).ConfigureAwait(false) :
                        await pageReader.PreparePageReadAsync(plan.PageRequest, ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    VerifyManagedReadPlan(blockName, plan);
                    if (page != null)
                    {
                        var observation = ((IUnitofWorkPageReadStage)stage).Page ??
                            throw new InvalidOperationException("Page stage omitted validated page evidence.");
                        if (observation.RequestId != plan.PageRequest.RequestId ||
                            observation.PageNumber != plan.PageRequest.PageNumber ||
                            observation.PageSize != plan.PageRequest.PageSize ||
                            observation.PayloadBytes > plan.PageRequest.MaxPayloadBytes)
                            throw new InvalidOperationException("Page stage evidence does not match the captured request bounds.");
                        result.ProviderPage = observation;
                        if (result.ProviderPage.IsOutOfRange)
                        {
                            result.State = FormQueryState.PageOutOfRange;
                            result.Message = "Provider count changed or the requested page is out of range; prior records were retained. Request a valid page explicitly.";
                            return ReportRejectedQuery(result, plan, ct, basicNotifications);
                        }
                    }
                    if (HasDirtyDetailSubtree(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    {
                        result.State = FormQueryState.BlockedDirty;
                        result.Message = "Captured query target became dirty before publication.";
                        return ReportRejectedQuery(result, plan, ct, basicNotifications);
                    }
                    var changedAt = DateTime.Now;
                    PublishManagedRead(blockName, plan, stage, ct, () =>
                    {
                        plan.Block.Mode = DataBlockMode.CRUD;
                        plan.Block.LastModeChange = changedAt;
                        plan.QueryMode = DataBlockMode.CRUD;
                    });
                    result.RecordsPublished = stage.IsPublished;
                    result.NotificationFailures = stage.NotificationFailures;
                }
                else
                {
                    result.LegacyPublicationPossible = true;
                    object rows = plan.Filters.Count == 0 ? await plan.Unit.Get().ConfigureAwait(false) :
                        await plan.Unit.Get(plan.Filters).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    VerifyManagedReadPlan(blockName, plan);
                    if (rows == null) throw new InvalidOperationException("Legacy query Get returned no result; publication success is unknown.");
                    lock (_registrationGate)
                    {
                        ct.ThrowIfCancellationRequested();
                        VerifyQueryRegistration(plan);
                        plan.Block.Mode = DataBlockMode.CRUD;
                        plan.Block.LastModeChange = DateTime.Now;
                    }
                }

                result.State = FormQueryState.Completed;
                result.Flag = Errors.Ok;
                result.Message = $"Query acknowledged for block '{blockName}'.";
                // Observers are outside publication monitors, but remain inside read ordering/drain.
                // A failed/cancelled/stale notification cannot reclassify accepted rows as unpublished.
                ObserveQueryNotification(result, plan, ct, () =>
                {
                    if (page != null) return; // An unpaged SELECT is not evidence of the provider's executed page query.
                    var source = string.IsNullOrWhiteSpace(plan.DataSourceName) ? null : _dmeEditor?.GetDataSource(plan.DataSourceName);
                    if (source == null || !CanDeliverQueryNotification(plan, ct)) return;
                    var definition = source.BuildSelectQueryDefinition(plan.EntityName ?? blockName, plan.Filters);
                    if (CanDeliverQueryNotification(plan, ct)) _systemVariablesManager?.SetLastQuery(definition.QueryText);
                });
                ObserveQueryNotification(result, plan, ct, () => _systemVariablesManager?.SetBlockStatus(blockName, "QUERY"));
                ObserveQueryNotification(result, plan, ct, () => _systemVariablesManager?.SetRecordStatus(blockName, "QUERY"));
                ObserveQueryNotification(result, plan, ct, () => _systemVariablesManager?.SetMode(ToSystemVariableMode(DataBlockMode.CRUD)));
                await ObserveQueryTriggerAsync(result, plan, TriggerType.PostQuery, ct).ConfigureAwait(false);
                ObserveQueryNotification(result, plan, ct, () => Status = result.Message);
                ObserveQueryNotification(result, plan, ct, () => LogOperation(result.Message, blockName));
                if (basicNotifications)
                {
                    ObserveQueryNotification(result, plan, ct, () => _messageManager?.ShowInfoMessage(blockName, result.Message));
                    if (wasInEnterQueryMode) await ObserveQueryTriggerAsync(result, plan, TriggerType.ExitQuery, ct).ConfigureAwait(false);
                }
                if (!CanDeliverQueryNotification(plan, ct))
                    AddQueryNotificationFailure(result, ct.IsCancellationRequested ? new OperationCanceledException(ct) :
                        new SupersededQueryException());
                return result;
            }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested) { return CancelQuery(result, ex); }
            catch (Exception ex)
            {
                if (result.RecordsPublished)
                {
                    result.State = FormQueryState.Completed;
                    result.Flag = Errors.Ok;
                    AddQueryNotificationFailure(result, ex);
                }
                else
                {
                    if (ct.IsCancellationRequested) return CancelQuery(result, new OperationCanceledException(ct));
                    result.State = ex is SupersededQueryException ? FormQueryState.Superseded :
                        ex is UnauthorizedAccessException ? FormQueryState.Denied : FormQueryState.Failed;
                    result.Message = ex.Message;
                    result.Ex = ex;
                    ObserveQueryNotification(result, plan, ct, () => LogError($"Error executing query for '{blockName}'", ex, blockName));
                    return ReportRejectedQuery(result, plan, ct, basicNotifications);
                }
                return result;
            }
            finally
            {
                if (acquired) _managedReadGate.Release();
                _managedReadDepth.Value--;
            }
        }
    }
}
