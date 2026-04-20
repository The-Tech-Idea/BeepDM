using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Public contract for the Phase 05 shard router. Combines the
    /// Phase 03 placement resolver with the Phase 04 partition
    /// functions to turn an incoming
    /// <see cref="DataBase.IDataSource"/> call into a concrete
    /// <see cref="RoutingDecision"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All members are pure with respect to the active distribution
    /// plan: a router instance is constructed once per plan version
    /// and re-built atomically when the plan changes (Phase 11).
    /// Callers who want to override routing without touching the plan
    /// should install an <see cref="IShardRoutingHook"/>.
    /// </para>
    /// <para>
    /// Implementations MUST raise
    /// <see cref="ShardRoutingException"/> for routing-specific
    /// failures (e.g. scatter writes when scatter is disallowed) and
    /// MAY surface upstream <see cref="System.InvalidOperationException"/>
    /// for placement failures forwarded from the resolver.
    /// </para>
    /// </remarks>
    public interface IShardRouter
    {
        /// <summary>Active hook; never <c>null</c>.</summary>
        IShardRoutingHook Hook { get; }

        /// <summary>
        /// Routes a read using a list of <see cref="AppFilter"/>s
        /// (the standard Beep query-filter shape). The router scans
        /// the filter list for the entity's partition-key column and
        /// extracts a single value (<c>=</c>) or a value set
        /// (<c>IN</c>); if no filter matches the partition key, the
        /// resulting decision is a scatter read across every live
        /// shard listed in the placement.
        /// </summary>
        /// <param name="entityName">Logical entity being read; required.</param>
        /// <param name="filters">Filter list; may be <c>null</c> or empty.</param>
        /// <param name="structure">Optional entity structure; used by extractors that need PK metadata.</param>
        /// <param name="context">Optional execution context; one is auto-created when omitted.</param>
        RoutingDecision RouteRead(
            string                       entityName,
            List<AppFilter>              filters,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null);

        /// <summary>
        /// Routes a read using positional primary-key values (the
        /// shape expected by <c>GetEntity(string, object[])</c>).
        /// Positional values are matched against
        /// <see cref="EntityStructure.PrimaryKeys"/> in declaration
        /// order.
        /// </summary>
        /// <param name="entityName">Logical entity being read; required.</param>
        /// <param name="positionalKeys">Positional key array; may be <c>null</c>.</param>
        /// <param name="structure">Entity structure carrying PK metadata; required when positional keys are supplied.</param>
        /// <param name="context">Optional execution context.</param>
        RoutingDecision RouteRead(
            string                       entityName,
            object[]                     positionalKeys,
            EntityStructure              structure,
            DistributedExecutionContext  context = null);

        /// <summary>
        /// Routes a write using an entity instance (POCO,
        /// <see cref="IDictionary{TKey,TValue}"/>, or anonymous
        /// object). The router reads the partition-key column(s) from
        /// the instance via reflection (cached per type).
        /// </summary>
        /// <param name="entityName">Logical entity being written; required.</param>
        /// <param name="record">Instance carrying the partition-key values; required.</param>
        /// <param name="structure">Optional entity structure; falls back to placement metadata when omitted.</param>
        /// <param name="context">Optional execution context.</param>
        RoutingDecision RouteWrite(
            string                       entityName,
            object                       record,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null);

        /// <summary>
        /// Low-level catch-all that takes a fully-prepared key map.
        /// Useful for callers that have already extracted the
        /// partition-key values themselves (e.g. ETL pipelines).
        /// </summary>
        /// <param name="entityName">Logical entity; required.</param>
        /// <param name="keyValues">Partition-key column → value map; <c>null</c> means "no key supplied".</param>
        /// <param name="isWrite">Hint that the call is a write.</param>
        /// <param name="context">Optional execution context.</param>
        RoutingDecision Route(
            string                              entityName,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            DistributedExecutionContext         context = null);
    }
}
