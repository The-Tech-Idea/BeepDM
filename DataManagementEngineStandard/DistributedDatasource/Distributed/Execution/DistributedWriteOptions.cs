namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Per-call write hints consumed by the Phase 07
    /// <see cref="IDistributedWriteExecutor"/>. Long-lived knobs live
    /// on <see cref="DistributedDataSourceOptions"/>; these override
    /// behaviour for a single call without mutating the shared
    /// options instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers create a fresh instance per write. All properties are
    /// nullable so an unset property means "inherit the datasource
    /// default." Implementations MUST treat <c>null</c> properties as
    /// "no override" — never fall back to <c>default(T)</c>.
    /// </para>
    /// <para>
    /// Use <see cref="Default"/> when you just want the plan's quorum
    /// and no scatter delete. The type is deliberately a class (not a
    /// struct) so callers can build and share a pre-configured
    /// instance across a loop of related writes.
    /// </para>
    /// </remarks>
    public sealed class DistributedWriteOptions
    {
        /// <summary>Shared "use defaults" singleton.</summary>
        public static readonly DistributedWriteOptions Default = new DistributedWriteOptions();

        /// <summary>
        /// When <c>true</c> a write against a
        /// <see cref="Plan.DistributionMode.Sharded"/> entity that is
        /// missing a partition key is allowed to scatter across every
        /// live shard. Typical use-case: "delete rows matching this
        /// filter everywhere." Opt-in because the default path would
        /// otherwise throw <see cref="Routing.ShardRoutingException"/>.
        /// Per-call variant of
        /// <see cref="DistributedDataSourceOptions.AllowScatterWrite"/>;
        /// <c>null</c> inherits from the datasource option.
        /// </summary>
        public bool? AllowScatterWrite { get; set; }

        /// <summary>
        /// Override the quorum policy for this call.
        /// <list type="bullet">
        ///   <item><c>null</c> — use the default derived from
        ///   <see cref="Plan.EntityPlacement.WriteQuorum"/>.</item>
        ///   <item><see cref="QuorumPolicy.All"/> — strict.</item>
        ///   <item><see cref="QuorumPolicy.Majority"/> — tolerate
        ///   one outage with RF=3.</item>
        ///   <item><see cref="QuorumPolicy.AtLeastN"/> — pair with
        ///   <see cref="AtLeastN"/>.</item>
        /// </list>
        /// </summary>
        public QuorumPolicy? QuorumOverride { get; set; }

        /// <summary>
        /// Required ack count when
        /// <see cref="QuorumOverride"/> is
        /// <see cref="QuorumPolicy.AtLeastN"/>. Ignored otherwise.
        /// Must be <c>&gt; 0</c> when set.
        /// </summary>
        public int? AtLeastN { get; set; }

        /// <summary>
        /// Optional caller-supplied correlation id. When set, the
        /// executor propagates it into
        /// <see cref="DistributedExecutionContext.CorrelationId"/> so
        /// downstream audit / telemetry can stitch the write to an
        /// upstream request. <c>null</c> lets the datasource generate
        /// a fresh id.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Optional entity-name hint for <c>ExecuteSql</c> or similar
        /// "bring your own SQL" write paths. When omitted the write
        /// executor errors out with a routing exception because it
        /// cannot decide which shard(s) own the statement. Phase 12
        /// extends this with a SQL parser that infers the entity from
        /// the statement body.
        /// </summary>
        public string EntityNameHint { get; set; }
    }
}
