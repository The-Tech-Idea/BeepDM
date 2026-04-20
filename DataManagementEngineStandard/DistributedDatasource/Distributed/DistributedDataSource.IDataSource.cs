using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — placeholder
    /// implementations of the <see cref="IDataSource"/> data-access
    /// surface. Phase 01 deliberately throws
    /// <see cref="NotImplementedException"/> from every method, naming
    /// the phase that will fill it in. This guarantees a callable but
    /// honest skeleton: existing <see cref="IDataSource"/> consumers
    /// can compile against the new type immediately, and any premature
    /// runtime use surfaces a clear error rather than silently
    /// succeeding.
    /// </summary>
    /// <remarks>
    /// Phase mapping:
    /// <list type="bullet">
    ///   <item>Phase 02 — entity / metadata accessors back the catalog plumbing.</item>
    ///   <item>Phase 06 — read methods (<c>GetEntity*</c>, <c>RunQuery</c>, <c>GetScalar*</c>).</item>
    ///   <item>Phase 07 — write methods (<c>InsertEntity</c>, <c>UpdateEntity*</c>, <c>DeleteEntity</c>).</item>
    ///   <item>Phase 09 — transaction methods (<c>BeginTransaction</c>, <c>Commit</c>, <c>EndTransaction</c>).</item>
    ///   <item>Phase 12 — schema methods (<c>CreateEntityAs</c>, <c>CreateEntities</c>, <c>RunScript</c>, <c>GetCreateEntityScript</c>).</item>
    /// </list>
    /// </remarks>
    public partial class DistributedDataSource
    {
        // ── Entity catalog (filled in Phase 02) ───────────────────────────

        /// <inheritdoc/>
        public IEnumerable<string> GetEntitesList()
            => throw NotYet("GetEntitesList", phase: "02");

        /// <inheritdoc/>
        public bool CheckEntityExist(string EntityName)
            => throw NotYet("CheckEntityExist", phase: "02");

        /// <inheritdoc/>
        public int GetEntityIdx(string entityName)
            => throw NotYet("GetEntityIdx", phase: "02");

        /// <inheritdoc/>
        public Type GetEntityType(string EntityName)
            => throw NotYet("GetEntityType", phase: "02");

        /// <inheritdoc/>
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
            => throw NotYet("GetEntityStructure(string, bool)", phase: "02");

        /// <inheritdoc/>
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => throw NotYet("GetEntityStructure(EntityStructure, bool)", phase: "02");

        /// <inheritdoc/>
        public IEnumerable<ChildRelation> GetChildTablesList(
            string tablename, string SchemaName, string Filterparamters)
            => throw NotYet("GetChildTablesList", phase: "02");

        /// <inheritdoc/>
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
            => throw NotYet("GetEntityforeignkeys", phase: "02");

        // ── Schema / scripts (Phase 12) ───────────────────────────────────
        // DDL methods (CreateEntityAs, CreateEntities, RunScript,
        // GetCreateEntityScript) live in
        // DistributedDataSource.Schema.cs.

        // ── Reads (filled in Phase 06) ────────────────────────────────────
        // Read methods (RunQuery, GetEntity overloads, GetEntityAsync,
        // GetScalar/Async) live in DistributedDataSource.Reads.cs.

        // ── Writes (filled in Phase 07) ───────────────────────────────────
        // Write methods (InsertEntity, UpdateEntity, UpdateEntities,
        // DeleteEntity, ExecuteSql) live in DistributedDataSource.Writes.cs.

        // ── Transactions (Phase 09) ───────────────────────────────────────
        // Transaction methods (BeginTransaction, Commit, EndTransaction,
        // plus the explicit distributed-scope API) live in
        // DistributedDataSource.Transactions.cs.

        // ── Helpers ───────────────────────────────────────────────────────

        private static NotImplementedException NotYet(string member, string phase)
            => new NotImplementedException(
                $"DistributedDataSource.{member} is not implemented yet — see Phase {phase} " +
                "in DistributedDatasource/DistributedPlans/MASTER-TODO-TRACKER.md.");
    }
}
