using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Terminal outcome of a DDL broadcast operation performed by
    /// <see cref="IDistributedSchemaService"/>. Captures the entity,
    /// the set of shards targeted, and the per-shard success / error
    /// state so callers (and Phase 13 audit) can diagnose partial
    /// failures.
    /// </summary>
    /// <remarks>
    /// DDL cannot be rolled back atomically across heterogeneous
    /// shards (see Phase 12 risks). Operations therefore aim for
    /// "best-effort with detectable drift": success for every listed
    /// shard is reported as <see cref="IsFullySucceeded"/> = <c>true</c>;
    /// per-shard failures populate <see cref="Errors"/> and surface in
    /// the next drift scan.
    /// </remarks>
    public sealed class SchemaOperationOutcome
    {
        /// <summary>Initialises a new outcome.</summary>
        public SchemaOperationOutcome(
            string                                operation,
            string                                entityName,
            IReadOnlyList<string>                 targetedShardIds,
            IReadOnlyList<string>                 succeededShardIds,
            IReadOnlyDictionary<string, Exception> errors,
            Exception                             terminalError = null)
        {
            if (string.IsNullOrWhiteSpace(operation))
                throw new ArgumentException("Operation cannot be null or whitespace.", nameof(operation));
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            Operation         = operation;
            EntityName        = entityName;
            TargetedShardIds  = targetedShardIds  ?? Array.Empty<string>();
            SucceededShardIds = succeededShardIds ?? Array.Empty<string>();
            Errors            = errors            ?? new Dictionary<string, Exception>(0, StringComparer.OrdinalIgnoreCase);
            TerminalError     = terminalError;
            StartedUtc        = DateTime.UtcNow;
        }

        /// <summary>High-level operation name (CreateEntity, AlterEntity, DropEntity).</summary>
        public string Operation { get; }

        /// <summary>Entity the operation targeted.</summary>
        public string EntityName { get; }

        /// <summary>Shard ids the operation attempted to reach (order preserved).</summary>
        public IReadOnlyList<string> TargetedShardIds { get; }

        /// <summary>Shard ids that acknowledged the DDL successfully.</summary>
        public IReadOnlyList<string> SucceededShardIds { get; }

        /// <summary>Per-shard errors raised during DDL execution (never <c>null</c>).</summary>
        public IReadOnlyDictionary<string, Exception> Errors { get; }

        /// <summary>
        /// Set when the operation aborted before reaching individual
        /// shards (e.g. identity-column policy rejection, plan
        /// mismatch). When set, <see cref="Errors"/> may be empty.
        /// </summary>
        public Exception TerminalError { get; }

        /// <summary>UTC timestamp captured when the outcome was constructed.</summary>
        public DateTime StartedUtc { get; }

        /// <summary>True when every targeted shard succeeded and no terminal error fired.</summary>
        public bool IsFullySucceeded
            => TerminalError == null
               && Errors.Count == 0
               && SucceededShardIds.Count == TargetedShardIds.Count;

        /// <summary>True when at least one shard failed or the terminal error fired.</summary>
        public bool HasFailures
            => TerminalError != null || Errors.Count > 0;

        /// <inheritdoc/>
        public override string ToString()
            => $"{Operation}({EntityName}): targeted={TargetedShardIds.Count}, " +
               $"succeeded={SucceededShardIds.Count}, failed={Errors.Count}, " +
               $"terminal={TerminalError?.GetType().Name ?? "(none)"}";
    }
}
