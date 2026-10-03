using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

/// <summary>
/// Opt-in adapter binding over existing host/view/presenter contracts. Owns only its
/// subscriptions and view binding, never the host, manager, registration or dispatcher.
/// </summary>
public sealed class FormsViewBinding : IDisposable, IAsyncDisposable
{
    private static readonly object OwnersGate = new();
    private static readonly ConditionalWeakTable<IBlockView, FormsViewBinding> Owners = new();
    private readonly object _gate = new();
    private readonly IBeepFormsHost _host;
    private readonly IBlockView _view;
    private readonly IFormsDispatcher _dispatcher;
    private readonly IUnitofWorksManager _manager;
    private readonly IFormsBindingTargets _targets;
    private readonly IFormsPolicyNotifications _policies;
    private readonly FormBindingTarget _registration;
    private readonly IUnitofWork _source;
    private readonly IFormsNotificationService _notifications;
    private readonly BeepViewState _viewState;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly AsyncLocal<int> _deliveryDepth = new();
    private readonly AsyncLocal<CancellationToken> _deliveryCancellation = new();
    private readonly List<Action> _unsubscribe = new();
    private readonly Dictionary<string, IFieldPresenter> _presenters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<IFieldPresenter, int> _presentationOrigins = new(ReferenceEqualityComparer.Instance);
    private readonly Guid _bindingOrigin = Guid.NewGuid();
    private long _presentationRevision;
    private readonly ConcurrentQueue<FormViewDeliveryResult> _outcomes = new();
    private readonly ConcurrentQueue<Exception> _cleanupFailures = new();
    private bool _closed, _attached, _cleaned, _bindAttempted;
    private int _pending;
    private TaskCompletionSource _drained;
    private bool _policyPumpRunning;
    private long _requestedPolicyRevision = -1;

    private FormsViewBinding(IBeepFormsHost host, IBlockView view, IFormsDispatcher dispatcher,
        IFormsNotificationService notifications)
    {
        _host = host; _view = view; _dispatcher = dispatcher; _notifications = notifications;
        _manager = host.FormsManager ?? throw new InvalidOperationException("Host has no forms manager.");
        _targets = _manager as IFormsBindingTargets ?? throw new NotSupportedException("Binding requires captured manager targets.");
        _policies = _manager as IFormsPolicyNotifications;
        if (!_targets.TryCaptureBindingTarget(view.BlockName, out _registration))
            throw new InvalidOperationException("View has no live registered block target.");
        _source = _manager.GetBlock(_registration.BlockName)?.UnitOfWork ?? throw new InvalidOperationException("Block has no source.");
        _viewState = view.ViewState as BeepViewState;
        if (notifications != null && _viewState == null)
            throw new NotSupportedException("Notifications require a BeepViewState.");
    }

    public static FormsViewBinding Attach(IBeepFormsHost host, IBlockView view, IFormsDispatcher dispatcher,
        IFormsNotificationService notifications = null)
    {
        ArgumentNullException.ThrowIfNull(host); ArgumentNullException.ThrowIfNull(view); ArgumentNullException.ThrowIfNull(dispatcher);
        if (!dispatcher.CheckAccess()) throw new InvalidOperationException("Attach must run on the adapter UI context.");
        if (view.IsBound) throw new InvalidOperationException("View is already bound; detach its owner first.");
        var binding = new FormsViewBinding(host, view, dispatcher, notifications);
        lock (OwnersGate)
        {
            if (Owners.TryGetValue(view, out _)) throw new InvalidOperationException("View already has a binding owner.");
            Owners.Add(view, binding);
        }
        try
        {
            binding.Install();
            return binding;
        }
        catch { binding.Dispose(); throw; }
    }

    public IReadOnlyList<FormViewDeliveryResult> Outcomes => _outcomes.ToArray();
    public IReadOnlyList<Exception> CleanupFailures => _cleanupFailures.ToArray();
    public int PendingDeliveries { get { lock (_gate) return _pending; } }

    private void Install()
    {
        EventHandler<BlockFieldChangedEventArgs> changed = (_, e) =>
        {
            if (string.Equals(e?.BlockName, _registration.BlockName, StringComparison.OrdinalIgnoreCase)) _ = RequestRefreshAsync();
        };
        EventHandler active = (_, _) => { _ = RequestRefreshAsync(); };
        EventHandler<BeepUnitOfWorkEventArgs> activity = (_, e) =>
        {
            if (e != null && (e.UnitOfWork == null || ReferenceEquals(e.UnitOfWork, _source)) &&
                string.Equals(e.BlockName, _registration.BlockName, StringComparison.OrdinalIgnoreCase) &&
                e.EventKind is BeepUnitOfWorkEventKind.CurrentChanged or BeepUnitOfWorkEventKind.PostQuery or
                    BeepUnitOfWorkEventKind.PostCommit or BeepUnitOfWorkEventKind.PostCreate or BeepUnitOfWorkEventKind.PostDelete)
                _ = RequestRefreshAsync();
        };
        EventHandler<FormsHostMessageEventArgs> raised = (_, e) => { _ = DeliverMessageAsync(e, clear: false); };
        EventHandler<FormsHostMessageEventArgs> cleared = (_, e) => { _ = DeliverMessageAsync(e, clear: true); };
        _unsubscribe.Add(() => _manager.OnBlockFieldChanged -= changed); _manager.OnBlockFieldChanged += changed;
        _unsubscribe.Add(() => _host.ActiveBlockChanged -= active); _host.ActiveBlockChanged += active;
        _unsubscribe.Add(() => _host.MessageRaised -= raised); _host.MessageRaised += raised;
        _unsubscribe.Add(() => _host.MessageCleared -= cleared); _host.MessageCleared += cleared;
        _unsubscribe.Add(() => _view.UnitOfWorkActivity -= activity); _view.UnitOfWorkActivity += activity;
        if (_policies != null)
        {
            EventHandler<SecurityPolicyChangedEventArgs> policyChanged = (_, e) => RequestPolicyReconciliation(e?.Revision ?? -1);
            _unsubscribe.Add(() => _policies.SecurityPolicyChanged -= policyChanged);
            _policies.SecurityPolicyChanged += policyChanged;
        }
        var presenters = _view.FieldPresenters.ToArray();
        if (presenters.Select(p => p.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != presenters.Length)
            throw new InvalidOperationException("Duplicate presenter fields require separate bindings.");
        foreach (var presenter in presenters)
        {
            var field = presenter.FieldName;
            _presenters.Add(field, presenter);
            if (presenter is IOriginAwareFieldPresenter stamped)
            {
                EventHandler<FormFieldValueChangedEventArgs> originHandler = (_, e) =>
                {
                    if (e == null || e.Origin != Guid.Empty) return;
                    _ = DeliverEditAsync(presenter, field, e.Value);
                };
                _unsubscribe.Add(() => stamped.ValueChangedWithOrigin -= originHandler);
                stamped.ValueChangedWithOrigin += originHandler;
                continue;
            }
            EventHandler<object> handler = (_, value) =>
            {
                if (_dispatcher.CheckAccess() && _presentationOrigins.ContainsKey(presenter)) return;
                _ = DeliverEditAsync(presenter, field, value);
            };
            _unsubscribe.Add(() => presenter.ValueChanged -= handler); presenter.ValueChanged += handler;
        }
        _bindAttempted = true;
        _view.Bind(_host);
        _attached = true;
        if (_targets.TryCaptureBindingTarget(_registration.BlockName, out _)) _ = RequestRefreshAsync();
        else if (_policies != null) RequestPolicyReconciliation(_policies.SecurityPolicyRevision);
    }

    private void RequestPolicyReconciliation(long revision)
    {
        if (revision < 0) return;
        lock (_gate)
        {
            if (_closed) return;
            _requestedPolicyRevision = Math.Max(_requestedPolicyRevision, revision);
            if (_policyPumpRunning) return;
            _policyPumpRunning = true;
            if (_pending++ == 0) _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        _ = ReconcilePoliciesAsync();
    }

    private async Task ReconcilePoliciesAsync()
    {
        // Defer inline dispatchers too: a presenter may publish another policy while
        // rendering. One owned pump coalesces those publications without recursive UI delivery.
        await Task.Yield();
        while (true)
        {
            long revision;
            TaskCompletionSource drained = null;
            lock (_gate)
            {
                if (_closed || _requestedPolicyRevision < 0)
                {
                    _policyPumpRunning = false;
                    if (--_pending == 0) drained = _drained;
                    revision = -1;
                }
                else { revision = _requestedPolicyRevision; _requestedPolicyRevision = -1; }
            }
            if (revision < 0) { drained?.TrySetResult(); return; }
            try
            {
                await QueuePolicyReconciliation(revision, default).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RetainOutcome(new FormViewDeliveryResult { State = FormViewDeliveryState.Failed, Exception = ex });
            }
        }
    }

    private bool CurrentPolicy(long revision) => Current() && _policies.SecurityPolicyRevision == revision;

    public Task<FormViewDeliveryResult> RequestPolicyReconciliationAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate) if (_closed) return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
        var revision = _policies?.SecurityPolicyRevision ?? -1;
        if (revision < 0) return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Rejected,
            Exception = new NotSupportedException("Policy reconciliation requires manager revision notifications.") });
        return QueuePolicyReconciliation(revision, cancellationToken);
    }

    private Task<FormViewDeliveryResult> QueuePolicyReconciliation(long revision, CancellationToken cancellationToken) => Submit(result =>
    {
        if (!CurrentPolicy(revision)) return;
        // A policy invalidation is not a delayed edit/focus result. Reconcile the
        // currently authorized buffer, including a query accepted while queued.
        var captured = _targets.TryCaptureBindingTarget(_registration.BlockName, out var target);
        if (!CurrentPolicy(revision)) return;
        if (captured) Refresh(target, result);
        else ClearRevokedPresentation(revision, result);
    }, cancellationToken);

    private void ClearRevokedPresentation(long revision, FormViewDeliveryResult result)
    {
        var failures = new List<Exception>();
        bool ClearStep(Action action)
        {
            if (!CurrentPolicy(revision)) return false;
            result.EffectsPossible = true;
            try { action(); }
            catch (Exception ex)
            {
                failures.Add(ex);
                result.Exception = new AggregateException("Privacy clearing was only partially applied.", failures);
            }
            return CurrentPolicy(revision);
        }
        if (!CurrentPolicy(revision)) return;
        foreach (var presenter in _presenters.Values)
        {
            if (!ClearStep(() => presenter.IsVisible = false)) return;
            if (!ClearStep(() => presenter.IsEnabled = false)) return;
            if (!ClearStep(() => presenter.IsReadOnly = true)) return;
            if (!ClearStep(() => Present(presenter, null))) return;
            if (!ClearStep(() => presenter.ValidationError = null)) return;
        }
        if (!CurrentPolicy(revision)) return;
        if (_viewState != null)
        {
            result.EffectsPossible = true;
            _viewState.StatusText = _viewState.CoordinationText = _viewState.WorkflowText = _viewState.SavepointText =
                _viewState.AlertText = _viewState.CurrentMessage = _viewState.RecordPositionText = _viewState.ConnectionName = _viewState.AggregateText = string.Empty;
            _viewState.CoordinationSeverity = _viewState.WorkflowSeverity = _viewState.SavepointSeverity = _viewState.AlertSeverity =
                _viewState.MessageSeverity = BeepMessageSeverity.None;
            _viewState.ActiveItemName = _viewState.FirstErrorBlockName = _viewState.FirstErrorFieldName = null;
            _viewState.ErrorCount = 0;
            _viewState.WorkflowHistoryItems.Clear();
            if (_notifications != null && !ClearStep(() => _notifications.Clear(_viewState))) return;
        }
        if (!CurrentPolicy(revision)) return;
        result.Acknowledged = failures.Count == 0;
        result.State = failures.Count == 0 ? FormViewDeliveryState.Delivered : FormViewDeliveryState.Failed;
    }

    private bool Current(FormBindingTarget target = null)
    {
        lock (_gate) if (_closed || !_attached || _deliveryCancellation.Value.IsCancellationRequested) return false;
        if (!_dispatcher.CheckAccess()) throw new InvalidOperationException("Dispatcher executed outside its UI context.");
        var current = ReferenceEquals(_host.FormsManager, _manager) && ReferenceEquals(_view.FormsHost, _host) && _view.IsBound &&
            (_viewState == null || ReferenceEquals(_view.ViewState, _viewState)) &&
            string.Equals(_view.ManagerBlockName, _registration.BlockName, StringComparison.OrdinalIgnoreCase) &&
            _targets.IsBindingTargetCurrent(_registration, includeRecord: false) &&
            (target == null || target.RegistrationId == _registration.RegistrationId && _targets.IsBindingTargetCurrent(target));
        if (current)
        {
            var presenters = _view.FieldPresenters.ToArray();
            current = presenters.Length == _presenters.Count && presenters.All(p =>
                _presenters.TryGetValue(p.FieldName, out var expected) && ReferenceEquals(expected, p));
        }
        lock (_gate) return current && !_closed && _attached && !_deliveryCancellation.Value.IsCancellationRequested;
    }

    public Task<FormViewDeliveryResult> RequestRefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate) if (_closed) return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
        if (!_targets.TryCaptureBindingTarget(_registration.BlockName, out var target))
            return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
        return Submit(result => Refresh(target, result), cancellationToken);
    }

    public Task<FormViewDeliveryResult> FocusAsync(FormBindingTarget acceptedTarget, string fieldName,
        CancellationToken cancellationToken = default) => Submit(result =>
    {
        if (!Current(acceptedTarget) || acceptedTarget == null ||
            !string.Equals(_host.ActiveBlockName, _registration.BlockName, StringComparison.OrdinalIgnoreCase)) return;
        var presenter = _view.FindFieldPresenter(fieldName);
        if (presenter == null || !presenter.IsEnabled || !presenter.IsVisible) { result.State = FormViewDeliveryState.Rejected; return; }
        if (!Current(acceptedTarget)) return;
        result.EffectsPossible = true;
        result.Acknowledged = _view.FocusField(fieldName);
        result.State = !result.Acknowledged ? FormViewDeliveryState.Rejected :
            Current(acceptedTarget) ? FormViewDeliveryState.Delivered : FormViewDeliveryState.Superseded;
    }, cancellationToken);

    private Task<FormViewDeliveryResult> DeliverEditAsync(IFieldPresenter presenter, string field, object value)
    {
        lock (_gate) if (_closed) return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
        value = value is byte[] bytes ? bytes.ToArray() : value;
        if (!_targets.TryCaptureBindingTarget(_registration.BlockName, out var target))
            return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
        return Submit(result =>
        {
            if (!Current(target) || !ReferenceEquals(_view.FindFieldPresenter(field), presenter))
                return;
            if (!presenter.IsEnabled || !presenter.IsVisible || presenter.IsReadOnly)
                { result.State = FormViewDeliveryState.Rejected; return; }
            var applied = _targets.ApplyBindingValue(target, field, value);
            result.State = applied.State; result.EffectsPossible = applied.EffectsPossible;
            result.Acknowledged = applied.Acknowledged; result.Exception = applied.Exception;
        }, default);
    }

    private void Refresh(FormBindingTarget target, FormViewDeliveryResult result)
    {
        if (!Current(target)) return;
        result.EffectsPossible = true;
        _view.SyncFromManager();
        if (!Current(target)) return;
        foreach (var presenter in _view.FieldPresenters.ToArray())
        {
            if (!Current(target)) return;
            var field = presenter.FieldName;
            var item = _host.GetItemInfo(_registration.BlockName, field);
            var block = _host.GetBlockInfo(_registration.BlockName) ?? throw new InvalidOperationException("Host returned no block metadata.");
            var security = _host.GetFieldSecurity(_registration.BlockName, field);
            var admin = _host.GetSecurityContext()?.IsAdmin == true;
            var tracking = target.Record == null ? null : block.UnitOfWork?.GetTrackingItem(target.Record);
            var inserting = block.Mode == DataBlockMode.Insert || tracking?.IsNew == true || tracking?.EntityState == EntityState.Added;
            var visible = (item?.Visible ?? true) && (security == null || admin || security.Visible) &&
                _host.IsBlockAllowed(_registration.BlockName, SecurityPermission.Query);
            var enabled = visible && (item?.Enabled ?? true) && (security == null || admin || security.Editable);
            var readOnly = !enabled || security?.Masked == true || block.Mode == DataBlockMode.EnterQuery || block.Mode == DataBlockMode.ReadOnly ||
                !(inserting ? block.InsertAllowed && (item?.InsertAllowed ?? true) : block.UpdateAllowed && (item?.UpdateAllowed ?? true)) ||
                !_host.IsBlockAllowed(_registration.BlockName, inserting ? SecurityPermission.Insert : SecurityPermission.Update);
            var value = visible ? CopyValue(_host.GetFieldValue(_registration.BlockName, field)) : null;
            try { value = _host.GetMaskedFieldValue(_registration.BlockName, field, value); }
            catch
            {
                if (Current(target)) Present(presenter, null);
                throw;
            }
            if (!Current(target)) return;
            presenter.IsVisible = visible;
            if (!Current(target)) return;
            presenter.IsEnabled = enabled;
            if (!Current(target)) return;
            presenter.IsReadOnly = readOnly;
            if (!Current(target)) return;
            Present(presenter, visible ? CopyValue(value) : null);
            if (!Current(target)) return;
            presenter.ValidationError = item?.ErrorMessage;
            if (!Current(target)) return;
        }
        result.Acknowledged = true;
        result.State = FormViewDeliveryState.Delivered;
    }

    private void Present(IFieldPresenter presenter, object value)
    {
        _presentationOrigins.TryGetValue(presenter, out var depth);
        _presentationOrigins[presenter] = depth + 1;
        try
        {
            if (presenter is IOriginAwareFieldPresenter stamped)
                stamped.SetValue(value, _bindingOrigin, ++_presentationRevision);
            else presenter.SetValue(value);
        }
        finally
        {
            if (depth == 0) _presentationOrigins.Remove(presenter);
            else _presentationOrigins[presenter] = depth;
        }
    }

    private static object CopyValue(object value) => value is byte[] bytes ? bytes.ToArray() : value;

    private Task<FormViewDeliveryResult> DeliverMessageAsync(FormsHostMessageEventArgs args, bool clear)
    {
        if (args == null || !string.IsNullOrEmpty(args.BlockName) &&
            !string.Equals(args.BlockName, _registration.BlockName, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Rejected });
        return Submit(result =>
        {
            if (!Current()) return;
            if (_notifications == null) { result.State = FormViewDeliveryState.Rejected; return; }
            result.EffectsPossible = true;
            if (clear) _notifications.Clear(_viewState);
            else _notifications.Publish(_viewState, args.Message, args.Level switch
            { MessageLevel.Error => BeepMessageSeverity.Error, MessageLevel.Warning => BeepMessageSeverity.Warning, _ => BeepMessageSeverity.Info });
            result.Acknowledged = true;
            result.State = Current() ? FormViewDeliveryState.Delivered : FormViewDeliveryState.Superseded;
        }, default);
    }

    private Task<FormViewDeliveryResult> Submit(Action<FormViewDeliveryResult> action, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_closed) return Task.FromResult(new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded });
            if (_pending++ == 0) _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        return DeliverAsync(action, ct);
    }

    private async Task<FormViewDeliveryResult> DeliverAsync(Action<FormViewDeliveryResult> action, CancellationToken ct)
    {
        var result = new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        try
        {
            await DispatchPhysicalAsync(() =>
            {
                var prior = _deliveryCancellation.Value;
                _deliveryCancellation.Value = cancellation.Token;
                try { action(result); }
                finally { _deliveryCancellation.Value = prior; }
            }, cancellation.Token).ConfigureAwait(false);
            if (cancellation.IsCancellationRequested) result.State = FormViewDeliveryState.Cancelled;
        }
        catch (OperationCanceledException ex) { result.State = FormViewDeliveryState.Cancelled; result.Exception = ex; }
        catch (Exception ex) { result.State = FormViewDeliveryState.Failed; result.Exception = ex; }
        finally
        {
            RetainOutcome(result);
            TaskCompletionSource drained = null;
            lock (_gate) if (--_pending == 0) drained = _drained;
            drained?.TrySetResult();
        }
        return result;
    }

    private void RetainOutcome(FormViewDeliveryResult result)
    {
        _outcomes.Enqueue(result);
        while (_outcomes.Count > 128) _outcomes.TryDequeue(out _);
    }

    private async Task DispatchPhysicalAsync(Action action, CancellationToken ct = default)
    {
        var actionFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var phase = 0;
        try
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (Interlocked.CompareExchange(ref phase, 1, 0) != 0)
                    throw new InvalidOperationException("Dispatcher invoked a delivery twice or after acknowledgement.");
                _deliveryDepth.Value++;
                try
                {
                    ct.ThrowIfCancellationRequested();
                    if (!_dispatcher.CheckAccess()) throw new InvalidOperationException("Dispatcher executed outside its UI context.");
                    action();
                }
                finally
                {
                    _deliveryDepth.Value--;
                    Interlocked.Exchange(ref phase, 2); actionFinished.TrySetResult();
                }
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            // Fence late work, but drain a running action even when its adapter task acknowledges early.
            if (Interlocked.CompareExchange(ref phase, 3, 0) == 1)
                await actionFinished.Task.ConfigureAwait(false);
        }
        if (Volatile.Read(ref phase) == 3 && !ct.IsCancellationRequested)
            throw new InvalidOperationException("Dispatcher acknowledged without executing the delivery.");
    }

    public Task WaitForPendingDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        if (_deliveryDepth.Value > 0) throw new InvalidOperationException("A binding delivery cannot await its own drain.");
        lock (_gate) return _pending == 0 ? Task.CompletedTask : _drained.Task.WaitAsync(cancellationToken);
    }

    private void Close()
    {
        lock (_gate) { if (_closed) return; _closed = true; }
        try { _lifetime.Cancel(); } catch (Exception ex) { _cleanupFailures.Enqueue(ex); }
    }

    public void Dispose()
    {
        if (!_dispatcher.CheckAccess()) throw new InvalidOperationException("Use DisposeAsync off the UI context.");
        Close(); Cleanup();
    }

    public ValueTask DisposeAsync()
    {
        if (_deliveryDepth.Value > 0) throw new InvalidOperationException("Dispose synchronously in a delivery; drain from the adapter later.");
        Close(); return new ValueTask(FinishDisposalAsync());
    }

    private async Task FinishDisposalAsync()
    {
        try { await DispatchPhysicalAsync(Cleanup).ConfigureAwait(false); }
        catch (Exception ex) { _cleanupFailures.Enqueue(ex); }
        await WaitForPendingDeliveriesAsync().ConfigureAwait(false);
    }

    private void Cleanup()
    {
        if (!_dispatcher.CheckAccess()) throw new InvalidOperationException("Detach must run on the adapter UI context.");
        if (_cleaned) return;
        _cleaned = true; _attached = false;
        foreach (var remove in _unsubscribe.ToArray())
            try { remove(); } catch (Exception ex) { _cleanupFailures.Enqueue(ex); }
        _unsubscribe.Clear();
        try { if (_bindAttempted && ReferenceEquals(_host.FormsManager, _manager) && ReferenceEquals(_view.FormsHost, _host)) _view.Unbind(); }
        catch (Exception ex) { _cleanupFailures.Enqueue(ex); }
        lock (OwnersGate) if (Owners.TryGetValue(_view, out var owner) && ReferenceEquals(owner, this)) Owners.Remove(_view);
    }
}
