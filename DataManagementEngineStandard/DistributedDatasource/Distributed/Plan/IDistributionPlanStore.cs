using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Plan
{
    /// <summary>
    /// Persistence contract for <see cref="DistributionPlan"/>. Phase 02
    /// ships a single implementation backed by
    /// <see cref="TheTechIdea.Beep.ConfigUtil.IConfigEditor"/>; later
    /// phases (or hosts) may layer file-per-plan or remote stores
    /// behind the same interface.
    /// </summary>
    /// <remarks>
    /// All methods are synchronous to match the existing
    /// <c>ProxyCluster.LoadNodesFromConfig</c> pattern. Implementations
    /// SHOULD be safe to call from multiple threads concurrently for
    /// read operations; write operations may serialise internally.
    /// </remarks>
    public interface IDistributionPlanStore
    {
        /// <summary>
        /// Loads the plan with <paramref name="distributionName"/>;
        /// returns <see cref="DistributionPlan.Empty"/> when no records
        /// exist for that name.
        /// </summary>
        DistributionPlan Load(string distributionName);

        /// <summary>
        /// Persists <paramref name="plan"/> under
        /// <paramref name="distributionName"/>. Existing records for the
        /// same plan are overwritten in place; entities removed from the
        /// plan are deleted from the store.
        /// </summary>
        void Save(DistributionPlan plan, string distributionName);

        /// <summary>Deletes every persisted record for <paramref name="distributionName"/>; returns the number removed.</summary>
        int Delete(string distributionName);

        /// <summary>Returns the distinct plan names currently persisted in the underlying store.</summary>
        IReadOnlyList<string> ListPlanNames();
    }
}
