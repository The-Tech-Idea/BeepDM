using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T> : IConditionalUnitOfWorkMutation
    {
        /// <summary>Whether this clean owner supports guarded physical mutation.</summary>
        public bool SupportsConditionalMutation => !disposedValue && !IsInListMode &&
            GetType() == typeof(UnitofWork<T>) && DataSource is IConditionalMutationDataSource;

        /// <summary>Awaits the guarded mutation and validates its acknowledgement.</summary>
        public async Task<ConditionalMutationResponse> MutateConditionallyAsync(ConditionalMutationRequest request,
            CancellationToken cancellationToken = default)
        {
            var admitted = false;
            var dispatched = false;
            ConditionalMutationResponse acknowledged = null;
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                cancellationToken.ThrowIfCancellationRequested();
                if (Interlocked.CompareExchange(ref _commitAdmission, 1, 0) != 0)
                    throw new InvalidOperationException("Another commit or staged operation owns this unit of work.");
                admitted = true;
                if (!SupportsConditionalMutation || GetIsDirty())
                    throw new NotSupportedException("A clean unit of work and a qualified conditional provider are required.");
                if (request.Guards.SelectMany(group => group.Alternatives).Any(guard => guard.HasCalendarWindow) &&
                    DataSource is not ICalendarGuardedMutationDataSource { SupportsCalendarGuards: true })
                    throw new NotSupportedException("The provider cannot enforce inclusive calendar guards.");
                if (!string.Equals(EntityName, request.EntityName, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("The mutation must target this unit of work's entity.");
                if (IsTenantScoped)
                {
                    var scope = request.Kind == ConditionalMutationKind.Insert ? request.Values : request.Match;
                    if (!scope.TryGetValue(TenantFieldName, out var company) || company is not string value ||
                        !string.Equals(TenantId, value, StringComparison.OrdinalIgnoreCase) ||
                        request.Values.TryGetValue(TenantFieldName, out var replacement) &&
                        !string.Equals(TenantId, replacement as string, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The mutation must retain this unit of work's tenant boundary.");
                }
                dispatched = true;
                var response = await ((IConditionalMutationDataSource)DataSource).MutateConditionallyAsync(request, cancellationToken).ConfigureAwait(false);
                if (response == null || response.RequestId != request.RequestId)
                    throw new InvalidOperationException("The provider did not return the mutation's own acknowledgement.");
                acknowledged = response;
                if (response.Status == ConditionalMutationStatus.Applied)
                {
                    Interlocked.Increment(ref _readTargetRevision);
                    Interlocked.Increment(ref _recordTargetRevision);
                    InvalidateReadBufferIdentity();
                }
                return response;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(nameof(UnitofWork<T>), "Conditional mutation admission failed: " + ex,
                    DateTime.Now, -1, null, Errors.Failed);
                return new ConditionalMutationResponse(request?.RequestId ?? Guid.Empty,
                    acknowledged?.Status == ConditionalMutationStatus.Applied ? ConditionalMutationStatus.Applied :
                    dispatched ? ConditionalMutationStatus.Unconfirmed : ConditionalMutationStatus.NotApplied, ex);
            }
            finally
            {
                if (admitted) Volatile.Write(ref _commitAdmission, 0);
            }
        }
    }
}
