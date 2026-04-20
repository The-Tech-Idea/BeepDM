using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Read-only context handed to an <see cref="IShardRoutingHook"/>
    /// alongside the baseline <see cref="RoutingDecision"/>. Hooks
    /// inspect the context to decide whether to override (e.g. force
    /// a specific shard for a debug session, pin a tenant, or block
    /// a request to a draining shard).
    /// </summary>
    public sealed class ShardRoutingHookContext
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyKeyValues
            = new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        /// <summary>Initialises a new context.</summary>
        /// <param name="entityName">Logical entity being routed; required.</param>
        /// <param name="isWrite"><c>true</c> when the call is a write.</param>
        /// <param name="keyValues">Partition-key columns/values consulted; <c>null</c> normalises to empty.</param>
        /// <param name="execution">Distributed execution context (correlation id etc.); may be <c>null</c>.</param>
        public ShardRoutingHookContext(
            string                              entityName,
            bool                                isWrite,
            IReadOnlyDictionary<string, object> keyValues  = null,
            DistributedExecutionContext         execution  = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            EntityName = entityName;
            IsWrite    = isWrite;
            KeyValues  = keyValues ?? EmptyKeyValues;
            Execution  = execution;
        }

        /// <summary>Logical entity being routed.</summary>
        public string EntityName { get; }

        /// <summary><c>true</c> when the call is a write.</summary>
        public bool IsWrite { get; }

        /// <summary>Partition-key columns and values that produced the baseline decision.</summary>
        public IReadOnlyDictionary<string, object> KeyValues { get; }

        /// <summary>Optional execution context for correlation / tags.</summary>
        public DistributedExecutionContext Execution { get; }
    }

    /// <summary>
    /// Pluggable override surface for <see cref="ShardRouter"/>. The
    /// router invokes <see cref="OnRouteResolved"/> after producing
    /// its baseline <see cref="RoutingDecision"/>; the hook may
    /// return the baseline unchanged, return a modified decision, or
    /// return <c>null</c> to indicate "use the baseline".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hooks MUST be deterministic and non-blocking — they run on
    /// the request hot path. Use them to pin debug sessions, force a
    /// shard for a specific tenant, or implement custom canary
    /// routing. Anything heavier (rate limiting, RBAC) belongs in a
    /// dedicated middleware layer.
    /// </para>
    /// <para>
    /// Returning a decision whose
    /// <see cref="RoutingDecision.HookOverridden"/> is <c>false</c>
    /// is allowed but discouraged: callers cannot tell the override
    /// happened. The convention is to construct any override decision
    /// with <c>hookOverridden: true</c> so observability tooling can
    /// surface the customisation.
    /// </para>
    /// </remarks>
    public interface IShardRoutingHook
    {
        /// <summary>
        /// Invoked once per routing call. Return the baseline
        /// (or a copy of it) when no override is needed; return a
        /// new decision to override.
        /// </summary>
        /// <param name="baseline">Decision produced by the router; never <c>null</c>.</param>
        /// <param name="context">Context describing the call; never <c>null</c>.</param>
        /// <returns>The decision the executor will act on. Never return <c>null</c>.</returns>
        RoutingDecision OnRouteResolved(RoutingDecision baseline, ShardRoutingHookContext context);
    }
}
