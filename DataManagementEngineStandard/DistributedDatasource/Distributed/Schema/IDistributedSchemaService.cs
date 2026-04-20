using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Phase 12 orchestrator for DDL operations across a
    /// <see cref="DistributedDataSource"/>. Coordinates create / alter /
    /// drop against every shard that the active
    /// <see cref="DistributionPlan"/> says owns the entity, and exposes
    /// drift detection so operators can repair heterogeneous schemas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service sits on top of the Phase 07 write-executor surface
    /// and per-shard <see cref="Proxy.IProxyCluster"/> DDL methods.
    /// Callers typically do not invoke it directly; the Phase 12
    /// <c>DistributedDataSource.Schema.cs</c> partial forwards
    /// <see cref="IDataSource.CreateEntityAs"/> /
    /// <see cref="IDataSource.CreateEntities"/> /
    /// <see cref="IDataSource.RunScript"/> into this surface.
    /// </para>
    /// <para>
    /// Every public method emits an audit-friendly
    /// <see cref="SchemaOperationOutcome"/> that carries the per-shard
    /// success / error state; drift detection returns a
    /// <see cref="SchemaDriftReport"/>.
    /// </para>
    /// </remarks>
    public interface IDistributedSchemaService
    {
        /// <summary>
        /// Creates the entity on every shard that the active plan
        /// assigns to it, honouring the distribution mode:
        /// <list type="bullet">
        ///   <item><c>Routed</c> → create on exactly one shard.</item>
        ///   <item><c>Sharded</c> → create on every member shard.</item>
        ///   <item><c>Replicated</c> / <c>Broadcast</c> → create on every listed shard.</item>
        /// </list>
        /// When the entity carries an identity column and the mode is
        /// <c>Sharded</c>, the
        /// <see cref="DistributedDataSourceOptions.IdentityColumnPolicy"/>
        /// option decides whether to proceed with a warning or reject.
        /// </summary>
        Task<SchemaOperationOutcome> CreateEntityAsync(
            EntityStructure   structure,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Broadcasts an <see cref="AlterEntityChange"/> to every shard
        /// that owns the target entity. Per-shard failures surface
        /// through <see cref="SchemaOperationOutcome.Errors"/>; the
        /// repair path is a subsequent
        /// <see cref="DetectSchemaDriftAsync"/> scan.
        /// </summary>
        Task<SchemaOperationOutcome> AlterEntityAsync(
            AlterEntityChange change,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops the entity on every owning shard.
        /// </summary>
        Task<SchemaOperationOutcome> DropEntityAsync(
            string            entityName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Samples <see cref="EntityStructure"/> from every shard listed
        /// in the active plan and returns a
        /// <see cref="SchemaDriftReport"/> listing every mismatch.
        /// </summary>
        Task<SchemaDriftReport> DetectSchemaDriftAsync(
            CancellationToken cancellationToken = default);
    }
}
