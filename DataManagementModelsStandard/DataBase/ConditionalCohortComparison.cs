using System;
namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Bounded selector for a count-only reference-integrity check; no row materialization or authority grant.</summary>
    public sealed class ConditionalCohortSelector
    {
        /// <summary>Declared reference entity.</summary>
        public string EntityName { get; }
        /// <summary>Copied immutable predicates. Paging, ordering and computed getters are not part of a count.</summary>
        public BoundedPageRequest Request { get; }
        /// <summary>Require explicit predicates and no unused paging/order metadata.</summary>
        public ConditionalCohortSelector(string entityName,BoundedPageRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if(string.IsNullOrWhiteSpace(entityName)||entityName.Length>256||entityName!=entityName.Trim()||
                request.CopyFilters().Count==0||request.PageNumber!=1||request.PageSize!=1||request.Order.Count!=0||request.ValidateReadOnlyScalars)
                throw new ArgumentException("An exact declared entity and explicit count-only selector are required.");
            EntityName=entityName;Request=request;
        }
    }
    /// <summary>Require equal complete counts in the same transaction. Counts may exceed interactive row limits.</summary>
    public sealed record ConditionalCohortComparison(ConditionalCohortSelector Left,ConditionalCohortSelector Right);
    /// <summary>Optional capability; producers without it must refuse comparisons rather than ignore them.</summary>
    public interface IComparedCohortMutationBatchDataSource:IProjectionGuardedMutationBatchDataSource
    {
        /// <summary>Whether complete cohort equality is enforced before writes and commit.</summary>
        bool SupportsCohortComparisons { get; }
    }
}
