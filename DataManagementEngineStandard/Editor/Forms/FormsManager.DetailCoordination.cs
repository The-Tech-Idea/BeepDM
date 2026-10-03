using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager : IFormsDetailSynchronization
    {
        private readonly SemaphoreSlim _managedReadGate = new(1, 1);
        private readonly AsyncLocal<int> _managedReadDepth = new();
        private readonly CancellationTokenSource _operationLifetime = new();
        private Task _readResourcesRetired = Task.CompletedTask;
        private readonly Dictionary<RegistrationLease, int> _detailSyncSuppressions = new();

        private IDisposable SuppressDetailSync(DetailNode node)
        {
            lock (_registrationGate)
            {
                VerifyDetailNode(node);
                var registration = _registrations[node.Name];
                _detailSyncSuppressions.TryGetValue(registration, out var count);
                _detailSyncSuppressions[registration] = count + 1;
                return new DetailSuppression(this, registration);
            }
        }

        private sealed class DetailSuppression(FormsManager owner, RegistrationLease registration) : IDisposable
        {
            private FormsManager _owner = owner;
            public void Dispose()
            {
                var manager = Interlocked.Exchange(ref _owner, null);
                if (manager == null) return;
                lock (manager._registrationGate)
                    if (manager._detailSyncSuppressions.TryGetValue(registration, out var count))
                    {
                        if (count == 1) manager._detailSyncSuppressions.Remove(registration);
                        else manager._detailSyncSuppressions[registration] = count - 1;
                    }
            }
        }

        private bool IsDetailSyncSuppressed(string name)
        {
            lock (_registrationGate)
                return _registrations.TryGetValue(name, out var registration) && _detailSyncSuppressions.ContainsKey(registration);
        }

        private void SetDetailPending(DetailNode node, bool pending)
        {
            lock (_registrationGate)
            {
                VerifyDetailNode(node);
                if (pending) _pendingDeferredSync[node.Name] = true;
                else _pendingDeferredSync.TryRemove(node.Name, out _);
            }
        }

        private Task RetireReadResourcesAfterDrain()
        {
            Task drained;
            lock (_callbackGate) drained = _pendingCallbacks == 0 ? Task.CompletedTask : _callbacksDrained.Task;
            return drained.ContinueWith(_ =>
            {
                CleanupAction("Managed operation lifetime source", _operationLifetime.Dispose);
                CleanupAction("Managed read gate", _managedReadGate.Dispose);
                CleanupAction("Local paging gate", _localPageGate.Dispose);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private sealed class DetailNode
        {
            internal string Name;
            internal DataBlockInfo Block;
            internal IUnitofWork Unit;
            internal DetailEdge[] Edges;
        }

        private sealed class DetailEdge
        {
            internal DetailNode Target;
            internal DetailCoordination Coordination;
            internal DataBlockFieldMapping[] Mappings;
        }

        private sealed class DetailRecord
        {
            internal object Item;
            internal List<AppFilter>[] Filters;
        }

        private sealed class SupersededDetailException : InvalidOperationException
        {
            internal SupersededDetailException() : base("Detail synchronization target or master record changed.") { }
        }

        public Task<DetailSynchronizationResult> SynchronizeDetailBlocksWithOutcomeAsync(string masterBlockName,
            CancellationToken cancellationToken = default) =>
            CoordinateDetailsAsync(masterBlockName, null, cancellationToken);

        public Task<DetailSynchronizationResult> SynchronizeDeferredDetailWithOutcomeAsync(string masterBlockName,
            string detailBlockName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(detailBlockName)) throw new ArgumentException("Detail name is required.", nameof(detailBlockName));
            return CoordinateDetailsAsync(masterBlockName, detailBlockName, cancellationToken);
        }

        private async Task<DetailSynchronizationResult> CoordinateDetailsAsync(string masterName, string forcedDetail,
            CancellationToken callerToken)
        {
            callerToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(masterName)) throw new ArgumentException("Master name is required.", nameof(masterName));
            if (_managedReadDepth.Value != 0)
                throw new InvalidOperationException("A managed read trigger/provider cannot await a nested query/detail read. Schedule it after the current operation.");
            using var lifetime = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken, _operationLifetime.Token);
            var ct = cancellation.Token;
            var result = new DetailSynchronizationResult { Flag = Errors.Ok };
            var acquired = false;
            _managedReadDepth.Value++;
            try
            {
                // Capture before the first await: later edits to public relationship objects cannot redirect this request.
                var nodes = new Dictionary<string, DetailNode>(StringComparer.OrdinalIgnoreCase);
                var root = CaptureDetailGraph(masterName, forcedDetail, nodes, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                var record = CaptureDetailRecord(root);
                var outcomes = nodes.Values.Where(n => !ReferenceEquals(n, root)).ToDictionary(n => n.Name,
                    n => new DetailBlockSynchronizationResult { FormInstanceId = _commitFormInstanceId, BlockName = n.Name },
                    StringComparer.OrdinalIgnoreCase);
                result.Details = outcomes.Values.ToList().AsReadOnly();
                ct.ThrowIfCancellationRequested();
                await _managedReadGate.WaitAsync(ct).ConfigureAwait(false);
                acquired = true;
                ct.ThrowIfCancellationRequested();
                VerifyDetailRecord(root, record);
                if (forcedDetail == null)
                {
                    var trigger = await _triggerManager.FireBlockTriggerAsync(TriggerType.OnPopulateDetails, root.Name,
                        TriggerContext.ForBlock(TriggerType.OnPopulateDetails, root.Name, record.Item, _dmeEditor), ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    VerifyDetailRecord(root, record);
                    if (trigger == TriggerResult.Cancelled)
                        throw new InvalidOperationException("Detail synchronization cancelled by ON-POPULATE-DETAILS.");
                }
                await ApplyDetailNodeAsync(root, record, outcomes, new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    forcedDetail != null, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                if (result.Details.Any(d => d.State is DetailSynchronizationState.Unattempted or
                    DetailSynchronizationState.BlockedDirty or DetailSynchronizationState.Superseded or
                    DetailSynchronizationState.Missing or DetailSynchronizationState.Failed))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "One or more captured details did not synchronize. Inspect Details before continuing navigation or deletion.";
                }
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                ct.ThrowIfCancellationRequested();
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                foreach (var detail in result.Details.Where(d => d.State == DetailSynchronizationState.Unattempted))
                {
                    detail.State = ex is SupersededDetailException ? DetailSynchronizationState.Superseded : DetailSynchronizationState.Failed;
                    detail.Message = ex.Message;
                }
                ReportDetailFailure(masterName, ex);
                return result;
            }
            finally
            {
                if (acquired) _managedReadGate.Release();
                _managedReadDepth.Value--;
            }
        }

        private DetailNode CaptureDetailGraph(string name, string forcedDetail, Dictionary<string, DetailNode> nodes,
            HashSet<string> path)
        {
            if (path.Contains(name)) throw new InvalidOperationException("Master-detail synchronization graph contains a cycle.");
            if (nodes.TryGetValue(name, out var existing)) return existing;
            if (path.Count >= 128 || nodes.Count >= 256)
                throw new InvalidOperationException("Detail synchronization supports at most 256 blocks and 128 hierarchy levels.");
            var block = GetBlock(name);
            var node = new DetailNode { Name = name, Block = block, Unit = block?.UnitOfWork };
            nodes[name] = node;
            path.Add(name);
            var edges = new List<DetailEdge>();
            foreach (var relation in GetActiveRelationships(name))
            {
                if (forcedDetail != null && (!string.Equals(relation.DetailBlockName, forcedDetail, StringComparison.OrdinalIgnoreCase) ||
                    relation.Coordination != DetailCoordination.Deferred)) continue;
                var targetName = relation.DetailBlockName;
                var coordination = relation.Coordination;
                var mappings = GetRelationshipFieldMappings(relation).Select(m => new DataBlockFieldMapping
                    { MasterField = m.MasterField, DetailField = m.DetailField }).ToArray();
                if (mappings.Length == 0 || mappings.Any(m => string.IsNullOrWhiteSpace(m.MasterField) || string.IsNullOrWhiteSpace(m.DetailField)))
                    throw new InvalidOperationException("Relationship has no valid master/detail field mappings.");
                edges.Add(new DetailEdge { Target = CaptureDetailGraph(targetName, null, nodes, path),
                    Coordination = coordination, Mappings = mappings });
            }
            path.Remove(name);
            node.Edges = edges.ToArray();
            return node;
        }

        private DetailRecord CaptureDetailRecord(DetailNode node)
        {
            VerifyDetailNode(node);
            object item = node.Unit.CurrentItem;
            var filters = node.Edges.Select(edge =>
            {
                if (item == null) return null;
                var values = new List<AppFilter>();
                foreach (var mapping in edge.Mappings)
                {
                    if (!RecordPropertyAccessor.TryGetValue(item, mapping.MasterField, out var value, _dmeEditor))
                        throw new InvalidOperationException($"Master key '{mapping.MasterField}' is unreadable; detail clearing is not authorized by an access failure.");
                    if (IsNullOrEmpty(value)) return null;
                    values.Add(new AppFilter { FieldName = mapping.DetailField, Operator = "=",
                        FilterValue = ManagedFilterCompiler.FormatInputValue(value) });
                }
                return values;
            }).ToArray();
            VerifyDetailNode(node);
            return new DetailRecord { Item = item, Filters = filters };
        }

        private void VerifyDetailNode(DetailNode node)
        {
            if (node.Unit == null || !IsCurrentRegistration(node.Name, node.Block, node.Unit))
                throw new SupersededDetailException();
        }

        private void VerifyDetailRecord(DetailNode node, DetailRecord record)
        {
            VerifyDetailNode(node);
            var now = CaptureDetailRecord(node);
            if (!ReferenceEquals(now.Item, record.Item) || now.Filters.Where((filters, i) =>
                FilterSignature(filters ?? new List<AppFilter>()) != FilterSignature(record.Filters[i] ?? new List<AppFilter>())).Any())
                throw new SupersededDetailException();
        }

        private async Task ApplyDetailNodeAsync(DetailNode master, DetailRecord record,
            Dictionary<string, DetailBlockSynchronizationResult> outcomes, HashSet<string> visited, bool force, CancellationToken ct,
            bool clearHierarchy = false)
        {
            if (!visited.Add(master.Name)) return;
            for (var i = 0; i < master.Edges.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                VerifyDetailRecord(master, record);
                var edge = master.Edges[i];
                var target = edge.Target;
                var outcome = outcomes[target.Name];
                if (!force && edge.Coordination == DetailCoordination.Deferred)
                {
                    SetDetailPending(target, true);
                    MarkDeferredSubtree(target, outcomes);
                    continue;
                }
                try
                {
                    VerifyDetailNode(target);
                    if (HasDirtyDetailSubtree(target, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    {
                        outcome.State = DetailSynchronizationState.BlockedDirty;
                        outcome.Message = "Detail or descendant has unsaved changes. Save or explicitly discard before synchronizing.";
                        continue;
                    }
                    using (SuppressDetailSync(target))
                    {
                        if (clearHierarchy || record.Filters[i] == null)
                        {
                            target.Unit.Clear();
                            ct.ThrowIfCancellationRequested();
                            VerifyDetailNode(target);
                            VerifyDetailRecord(master, record);
                            outcome.State = DetailSynchronizationState.Cleared;
                        }
                        else
                        {
                            var plan = BuildManagedReadPlan(target.Name, record.Filters[i]);
                            if (!ReferenceEquals(plan.Unit, target.Unit)) throw new SupersededDetailException();
                            ct.ThrowIfCancellationRequested();
                            VerifyManagedReadPlan(target.Name, plan);
                            VerifyDetailRecord(master, record);
                            if (target.Unit.IsDirty) throw new InvalidOperationException("Detail became dirty before its read.");
                            ct.ThrowIfCancellationRequested();
                            VerifyDetailNode(target);
                            VerifyDetailNode(master);
                            if (plan.Unit is IStagedUnitofWorkRead reader && reader.SupportsStagedRead)
                            {
                                if (_securityManager is not IQuerySecurityPublication)
                                    throw new NotSupportedException("Staged detail reads require a revision-gated security helper.");
                                using var stage = await reader.PrepareReadAsync(plan.Filters, ct).ConfigureAwait(false);
                                ct.ThrowIfCancellationRequested();
                                VerifyManagedReadPlan(target.Name, plan);
                                VerifyDetailRecord(master, record);
                                PublishManagedRead(target.Name, plan, stage, ct);
                                outcome.RecordsPublished = stage.IsPublished;
                                outcome.ProviderMayHavePublished = stage.IsPublished;
                                outcome.NotificationFailures = stage.NotificationFailures;
                            }
                            else
                            {
                                outcome.ProviderMayHavePublished = true;
                                object rows = await plan.Unit.Get(plan.Filters).ConfigureAwait(false);
                                ct.ThrowIfCancellationRequested();
                                VerifyManagedReadPlan(target.Name, plan);
                                VerifyDetailRecord(master, record);
                                if (rows == null) throw new InvalidOperationException("Legacy detail Get returned no result; publication success is unknown.");
                            }
                            outcome.State = DetailSynchronizationState.Refreshed;
                        }
                    }
                    SetDetailPending(target, false);
                    await ApplyDetailNodeAsync(target, CaptureDetailRecord(target), outcomes, visited, false, ct,
                        outcome.State == DetailSynchronizationState.Cleared).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    outcome.State = ex is SupersededDetailException ? DetailSynchronizationState.Superseded :
                        target.Unit == null ? DetailSynchronizationState.Missing : DetailSynchronizationState.Failed;
                    outcome.Message = ex.Message;
                    ReportDetailFailure(target.Name, ex);
                    // Descendants remain unattempted, never queried from the failed parent's old current item.
                }
            }
        }

        private bool HasDirtyDetailSubtree(DetailNode node, HashSet<string> visited)
        {
            if (!visited.Add(node.Name)) return false;
            VerifyDetailNode(node);
            return node.Unit.IsDirty || node.Edges.Any(edge => HasDirtyDetailSubtree(edge.Target, visited));
        }

        private static void MarkDeferredSubtree(DetailNode node, Dictionary<string, DetailBlockSynchronizationResult> outcomes)
        {
            if (outcomes[node.Name].State != DetailSynchronizationState.Unattempted) return;
            outcomes[node.Name].State = DetailSynchronizationState.Deferred;
            foreach (var edge in node.Edges) MarkDeferredSubtree(edge.Target, outcomes);
        }

        private void ReportDetailFailure(string name, Exception error)
        {
            try { LogError($"Detail synchronization failed for '{name}'; legacy reads may already have published.", error, name); }
            catch { /* Diagnostics cannot hide an independent detail's result. */ }
        }
    }
}
