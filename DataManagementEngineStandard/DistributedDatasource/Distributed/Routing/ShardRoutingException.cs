using System;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Raised when the <see cref="ShardRouter"/> cannot turn an
    /// incoming request into a workable
    /// <see cref="RoutingDecision"/>: scatter writes when scatter is
    /// disallowed, partition functions resolving to zero live shards,
    /// or a hook returning an empty / contradictory override.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is intentionally distinct from the
    /// <see cref="InvalidOperationException"/> raised by
    /// <see cref="DistributedDataSource.ResolvePlacement(string, bool, DistributedExecutionContext)"/>
    /// for unmapped placements: callers and observability tooling can
    /// treat routing failures (key extraction / scatter rejection /
    /// hook errors) separately from the upstream placement failures.
    /// </para>
    /// <para>
    /// The <see cref="EntityName"/> and <see cref="Reason"/>
    /// properties are populated for every throw so structured logging
    /// can correlate failures back to a specific entity without
    /// regex-parsing the message.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class ShardRoutingException : InvalidOperationException
    {
        /// <summary>Initialises a new exception.</summary>
        /// <param name="entityName">Logical entity that failed to route. Required for diagnostics.</param>
        /// <param name="reason">Machine-friendly category (e.g. <c>"ScatterWriteRejected"</c>, <c>"NoLiveShard"</c>, <c>"HookOverrideEmpty"</c>).</param>
        /// <param name="message">Human-readable explanation. Used as the base <see cref="Exception.Message"/>.</param>
        /// <param name="inner">Optional inner exception.</param>
        public ShardRoutingException(
            string    entityName,
            string    reason,
            string    message,
            Exception inner = null)
            : base(message, inner)
        {
            EntityName = entityName ?? string.Empty;
            Reason     = reason     ?? string.Empty;
        }

        /// <summary>Logical entity that failed to route; never <c>null</c>.</summary>
        public string EntityName { get; }

        /// <summary>Short, machine-friendly failure category; never <c>null</c>.</summary>
        public string Reason { get; }
    }
}
