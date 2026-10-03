using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager;

public partial class FormsManager : IFormsLocalPaging
{
    private readonly SemaphoreSlim _localPageGate = new(1, 1);
    private readonly AsyncLocal<int> _localPageDepth = new();
    private sealed class SupersededLocalPageException : InvalidOperationException
    { internal SupersededLocalPageException() : base("Local page registration, buffer, configuration or request changed.") { } }

    public async Task<LocalPageResult> LoadLocalPageWithOutcomeAsync(string blockName, int pageNumber,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_localPageDepth.Value != 0 || _managedReadDepth.Value != 0)
            throw new InvalidOperationException("A paging/read callback cannot await nested local paging. Schedule it after the operation.");
        using var lifetime = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _operationLifetime.Token);
        var ct = linked.Token;
        var result = new LocalPageResult { BlockName = blockName, State = LocalPageState.Failed };
        if (string.IsNullOrWhiteSpace(blockName) || pageNumber <= 0)
        { result.State = LocalPageState.InvalidRequest; result.Message = "A block and positive page number are required."; return result; }
        var acquired = false;
        _localPageDepth.Value++;
        try
        {
            RegistrationLease registration;
            long request, queryRevision;
            lock (_registrationGate)
            {
                if (!_registrations.TryGetValue(blockName, out registration) || !CanDispatchRegistration(registration))
                    throw new SupersededLocalPageException();
                request = ++registration.LocalPageRevision; queryRevision = registration.QueryRevision;
            }
            result.BlockName = blockName = registration.Name;
            result.RegistrationId = registration.Identity; result.RequestRevision = request;
            if (_pagingManager is not ILocalPagingPublication paging || _securityManager is not IQuerySecurityPublication security)
                throw new NotSupportedException("Local paging requires publication-capable paging and security helpers.");
            var unit = registration.Source;
            if (unit.IsVirtualMode) throw new NotSupportedException("Virtual/provider pages require bounded provider paging, not local cursor paging.");
            object units = unit.Units;
            var count = unit.TotalItemCount;
            if (units == null || count < 0 || LocalCount(units) != count)
                throw new InvalidOperationException("Local paging requires a consistent loaded collection.");
            var cursor = count == 0 ? -1 : LocalCursor(units);
            var configuration = registration.Block.Configuration;
            var configuredSize = configuration?.PageSize;
            var mode = registration.Block.Mode;
            if (mode == Models.DataBlockMode.EnterQuery) throw new NotSupportedException("Query criteria are not local result pages.");
            var policyRevision = SecurityPolicyRevision;
            var plan = paging.PrepareLocalPage(blockName, pageNumber, count);
            var page = plan.Page;
            var target = checked((int)page.SkipLong);
            var attemptedMove = false;
            var revisions = unit as IUnitofWorkRecordRevision;
            var hasRevision = revisions?.SupportsRecordRevision == true;
            long initialRevision = 0;
            if (hasRevision && !revisions.TryGetRecordRevision(out initialRevision)) throw new SupersededLocalPageException();
            void CheckOwned()
            {
                ct.ThrowIfCancellationRequested();
                if (!CanDispatchRegistration(registration) || registration.LocalPageRevision != request ||
                    registration.QueryRevision != queryRevision || registration.Block.Mode != mode ||
                    !ReferenceEquals(registration.Block.Configuration, configuration) || configuration?.PageSize != configuredSize)
                    throw new SupersededLocalPageException();
            }
            void Check()
            {
                lock (_registrationGate) CheckOwned();
                if (!result.PageStatePublished && !paging.IsLocalPageCurrent(plan)) throw new SupersededLocalPageException();
                if (unit.IsVirtualMode || !ReferenceEquals((object)unit.Units, units) || unit.TotalItemCount != count || LocalCount(units) != count ||
                    SecurityPolicyRevision != policyRevision) throw new SupersededLocalPageException();
                if (!IsBufferAuthorized(registration)) throw new UnauthorizedAccessException("Local paging cannot authorize cached rows; execute an accepted managed read first.");
                if (!attemptedMove && count != 0 && LocalCursor(units) != cursor) throw new SupersededLocalPageException();
                if (result.PageStatePublished && count != 0 && LocalCursor(units) != target) throw new SupersededLocalPageException();
                if (!attemptedMove && hasRevision && (!revisions.TryGetRecordRevision(out var current) || current != initialRevision))
                    throw new SupersededLocalPageException();
                lock (_registrationGate) CheckOwned();
            }
            void Publish()
            {
                Check();
                if (count != 0 && LocalCursor(units) != target)
                    throw new InvalidOperationException("Cursor setter did not acknowledge the requested local index.");
                if (!paging.TryPublishLocalPage(plan, () =>
                {
                    if (!security.TryPublishQuery(policyRevision, () =>
                    {
                        lock (_registrationGate)
                        {
                            CheckOwned();
                            registration.Block.CurrentPage = page.PageNumber;
                            result.Page = page; result.PageStatePublished = true;
                            result.CursorNavigationAcknowledged = count != 0;
                        }
                    })) throw new SupersededLocalPageException();
                })) throw new SupersededLocalPageException();
            }
            await _localPageGate.WaitAsync(ct).ConfigureAwait(false);
            acquired = true; Check();
            if (count == 0 || cursor == target) Publish();
            else
            {
                var success = await NavigateToRecordInternalAsync(blockName, target, true, Check,
                    () => { attemptedMove = true; result.NavigationEffectsPossible = true; }, Publish, ct).ConfigureAwait(false);
                if (!success && !result.PageStatePublished)
                { result.State = LocalPageState.NavigationRejected; result.Message = "Local navigation was rejected; page state was not published."; return result; }
            }
            if (!result.PageStatePublished) throw new InvalidOperationException("Local navigation did not publish page state.");
            result.State = LocalPageState.Completed; result.Message = "Local cursor page acknowledged; no datasource page was fetched.";
            if (CanDispatchRegistration(registration)) Status = result.Message;
            return result;
        }
        catch (Exception ex)
        {
            if (result.PageStatePublished)
            {
                result.State = LocalPageState.Completed;
                result.NotificationFailures = result.NotificationFailures.Concat(new[] { ex }).ToArray();
                result.Message = "Local page acknowledged with notification failure; do not blindly replay.";
            }
            else
            {
                result.Error = ex; result.Message = ex.Message;
                result.State = ex is OperationCanceledException ? LocalPageState.Cancelled :
                    ex is SupersededLocalPageException ? LocalPageState.Superseded :
                    ex is NotSupportedException ? LocalPageState.Unsupported : LocalPageState.Failed;
            }
            return result;
        }
        finally { if (acquired) _localPageGate.Release(); _localPageDepth.Value--; }
    }

    private static int LocalCursor(object units) => (int)((dynamic)units).CurrentIndex;
    private static int LocalCount(object units) => (int)((dynamic)units).Count;
}
