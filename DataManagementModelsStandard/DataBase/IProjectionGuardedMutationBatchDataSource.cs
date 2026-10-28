using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional producer: recheck complete projection count/content before mutations and before commit in the same physical transaction.</summary>
    public interface IProjectionGuardedMutationBatchDataSource : IConditionalMutationBatchDataSource
    {
        /// <summary>Whether this producer enforces every stored projection guard inside its transaction.</summary>
        bool SupportsProjectionGuardedBatches { get; }
        /// <summary>Await all source checks, mutations, final checks and the exact original batch acknowledgement.</summary>
        Task<ConditionalMutationBatchResponse> MutateProjectionGuardedBatchAsync(ProjectionGuardedMutationBatchRequest request, CancellationToken cancellationToken = default);
    }
    /// <summary>Additional source observations; the ordinary batch, its identity, authority guards and outcomes stay unchanged.</summary>
    public sealed class ProjectionGuardedMutationBatchRequest
    {
        /// <summary>Original batch identity, authority observations, operations and result semantics.</summary>
        public ConditionalMutationBatchRequest Batch { get; }
        /// <summary>Complete additional reference cohorts checked before writes and commit.</summary>
        public IReadOnlyList<ConditionalStoredProjectionGuard> ProjectionGuards { get; }
        /// <summary>Additional complete-count equalities; requires the explicit comparison producer capability.</summary>
        public IReadOnlyList<ConditionalCohortComparison> CohortComparisons { get; }
        /// <summary>Copy at most16 cohorts with a combined16 MiB canonical/source request budget.</summary>
        public ProjectionGuardedMutationBatchRequest(ConditionalMutationBatchRequest batch, IEnumerable<ConditionalStoredProjectionGuard> projectionGuards)
            :this(batch,projectionGuards,Array.Empty<ConditionalCohortComparison>()) { }
        /// <summary>Compose count equalities without weakening existing row/count or source-payload budgets.</summary>
        public ProjectionGuardedMutationBatchRequest(ConditionalMutationBatchRequest batch,IEnumerable<ConditionalStoredProjectionGuard> projectionGuards,
            IEnumerable<ConditionalCohortComparison> cohortComparisons)
        {
            ArgumentNullException.ThrowIfNull(batch);
            var guards = projectionGuards?.ToArray() ?? throw new ArgumentNullException(nameof(projectionGuards));
            if (guards.Length is < 1 or > 16 || guards.Any(guard => guard is null) || guards.Sum(guard => guard.CanonicalBytes) > BoundedPageRequest.MaximumPayloadBytes)
                throw new ArgumentException("One through16 complete bounded source cohorts are required.");
            Batch = batch; ProjectionGuards = Array.AsReadOnly(guards);
            var comparisons=cohortComparisons?.ToArray()??throw new ArgumentNullException(nameof(cohortComparisons));
            if(comparisons.Length>8||comparisons.Any(value=>value?.Left is null||value.Right is null))throw new ArgumentException("At most eight explicit complete cohort comparisons are permitted.");
            CohortComparisons=Array.AsReadOnly(comparisons);
            using var encoded = new BoundedMutationEncodingStream(BoundedPageRequest.MaximumPayloadBytes);
            object Filter(TheTechIdea.Beep.Report.AppFilter value)=>new{value.FieldName,value.Operator,value.FilterValue,value.FilterValue1,
                Type=value.FieldType?.AssemblyQualifiedName,value.valueType,value.ID,value.GuidID};
            object Related(PageRelatedFilter value)=>new{value.EntityName,value.Keys,value.Comparisons,
                Filters=value.CopyFilters().Select(Filter),Any=value.CopyAnyFilters().Select(Filter)};
            object Selector(ConditionalCohortSelector value)=>new{value.EntityName,Filters=value.Request.CopyFilters().Select(Filter),Any=value.Request.CopyAnyFilters().Select(Filter),
                Related=value.Request.AnyRelatedFilters.Select(Related),Groups=value.Request.RequiredRelatedGroups.Select(group=>group.Alternatives.Select(Related))};
            // BoundedPageRequest intentionally exposes copied predicates through methods, not
            // serialized properties. Count their actual values explicitly in this envelope.
            JsonSerializer.Serialize(encoded, new { Batch, CohortComparisons=comparisons.Select(value=>new{Left=Selector(value.Left),Right=Selector(value.Right)}),ProjectionGuards=guards.Select(guard=>new{guard.EntityName,guard.Fields,guard.KeyFields,
                guard.ExpectedCount,guard.ExpectedDigest,guard.CanonicalBytes,guard.Request.PageSize,guard.Request.MaxPayloadBytes,guard.Request.Order,
                Filters=guard.Request.CopyFilters().Select(Filter),Any=guard.Request.CopyAnyFilters().Select(Filter),
                Related=guard.Request.AnyRelatedFilters.Select(Related),Groups=guard.Request.RequiredRelatedGroups.Select(group=>group.Alternatives.Select(Related))}) });
        }
    }
}
