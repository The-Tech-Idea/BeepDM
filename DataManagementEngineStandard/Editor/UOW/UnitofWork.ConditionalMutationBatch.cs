using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T> : IConditionalUnitOfWorkBatchMutation, IUnitofWorkProjectionGuardedBatch
    {
        /// <summary>Whether this owner can dispatch an isolated atomic batch.</summary>
        public bool SupportsConditionalMutationBatch => !disposedValue && !IsInListMode && GetType() == typeof(UnitofWork<T>) && DataSource is IConditionalMutationBatchDataSource;
        /// <summary>Awaits a scoped clean batch and validates its physical acknowledgement.</summary>
        public async Task<ConditionalMutationBatchResponse> MutateBatchConditionallyAsync(ConditionalMutationBatchRequest request, CancellationToken cancellationToken = default)
            => await MutateBatchCoreAsync(request,null,cancellationToken).ConfigureAwait(false);

        public bool SupportsProjectionGuardedBatch => SupportsConditionalMutationBatch &&
            DataSource is IProjectionGuardedMutationBatchDataSource { SupportsProjectionGuardedBatches:true };
        public async Task<ConditionalMutationBatchResponse> MutateProjectionGuardedBatchAsync(ProjectionGuardedMutationBatchRequest request,CancellationToken cancellationToken=default)
            => await MutateBatchCoreAsync(request?.Batch,request,cancellationToken).ConfigureAwait(false);

        private async Task<ConditionalMutationBatchResponse> MutateBatchCoreAsync(ConditionalMutationBatchRequest request,
            ProjectionGuardedMutationBatchRequest projected,CancellationToken cancellationToken)
        {
            var admitted = false; var dispatched = false; ConditionalMutationBatchResponse acknowledged = null;
            try
            {
                ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested();
                if (Interlocked.CompareExchange(ref _commitAdmission, 1, 0) != 0) throw new InvalidOperationException("Another operation owns this unit of work.");
                admitted = true;
                if (!SupportsConditionalMutationBatch || GetIsDirty()) throw new NotSupportedException("A clean owner and an atomic batch provider are required.");
                if(projected is not null)
                {
                    if(!SupportsProjectionGuardedBatch)throw new NotSupportedException("The provider cannot enforce stored projection transaction guards.");
                    foreach(var guard in projected.ProjectionGuards)RequireStoredProjectionCapabilities(guard.Request);
                    if(projected.CohortComparisons.Count>0&&DataSource is not IComparedCohortMutationBatchDataSource{SupportsCohortComparisons:true})
                        throw new NotSupportedException("The provider cannot enforce complete cohort comparisons.");
                    foreach(var comparison in projected.CohortComparisons)
                    {RequireStoredProjectionCapabilities(comparison.Left.Request);RequireStoredProjectionCapabilities(comparison.Right.Request);}
                }
                if (request.Sets.Any(set => set.DateRanges.Count != 0) &&
                    DataSource is not IDateRangeGuardedMutationBatchDataSource { SupportsDateRangeGuards: true })
                    throw new NotSupportedException("The provider cannot enforce physical date-range cohort guards.");
                if (request.Sets.Any(set => set.TextEqualities.Count != 0) &&
                    DataSource is not IOrdinalIgnoreCaseTextDataSource { SupportsOrdinalIgnoreCaseText: true })
                    throw new NotSupportedException("The provider cannot enforce ordinal case-insensitive text cohorts.");
                if (request.Guards.Concat(request.CommitGuards).Concat(request.Mutations.SelectMany(op => op.Guards))
                    .SelectMany(group => group.Alternatives).Any(guard => guard.HasCalendarWindow) &&
                    DataSource is not ICalendarGuardedMutationDataSource { SupportsCalendarGuards: true })
                    throw new NotSupportedException("The provider cannot enforce inclusive calendar guards.");
                if (!string.Equals(request.Mutations[0].EntityName, EntityName, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("The first operation must anchor this owner.");
                foreach (var mutation in request.Mutations)
                {
                    if (!IsTenantScoped) continue;
                    var scope = mutation.Kind == ConditionalMutationKind.Insert ? mutation.Values : mutation.Match;
                    if (!scope.TryGetValue(TenantFieldName, out var company) || company is not string value || !string.Equals(TenantId, value, StringComparison.OrdinalIgnoreCase) ||
                        mutation.Values.TryGetValue(TenantFieldName, out var replacement) && !string.Equals(TenantId, replacement as string, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Every batch target must retain this owner's tenant boundary.");
                }
                dispatched = true;
                var response = projected is null
                    ? await ((IConditionalMutationBatchDataSource)DataSource).MutateBatchConditionallyAsync(request, cancellationToken).ConfigureAwait(false)
                    : await ((IProjectionGuardedMutationBatchDataSource)DataSource).MutateProjectionGuardedBatchAsync(projected,cancellationToken).ConfigureAwait(false);
                if (response == null || response.RequestId != request.RequestId || response.Status == ConditionalMutationBatchStatus.Applied && response.ChangedRows != request.Mutations.Count)
                    throw new InvalidOperationException("The provider did not acknowledge this exact batch.");
                acknowledged = response;
                if (response.Status == ConditionalMutationBatchStatus.Applied)
                {
                    Interlocked.Increment(ref _readTargetRevision); Interlocked.Increment(ref _recordTargetRevision); InvalidateReadBufferIdentity();
                }
                return response;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage(nameof(UnitofWork<T>), "Conditional batch failed: " + ex, DateTime.Now, -1, null, Errors.Failed);
                var status = acknowledged?.Status == ConditionalMutationBatchStatus.Applied ? ConditionalMutationBatchStatus.Applied : dispatched ? ConditionalMutationBatchStatus.Unconfirmed : ConditionalMutationBatchStatus.NotApplied;
                return new ConditionalMutationBatchResponse(request?.RequestId ?? Guid.Empty, status, status == ConditionalMutationBatchStatus.Applied ? acknowledged.ChangedRows : dispatched ? null : 0, ex);
            }
            finally { if (admitted) Volatile.Write(ref _commitAdmission, 0); }
        }
    }
}
