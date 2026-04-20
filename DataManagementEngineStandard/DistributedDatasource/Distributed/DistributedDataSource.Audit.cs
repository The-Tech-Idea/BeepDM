using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Audit;
using TheTechIdea.Beep.Distributed.Security;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 13
    /// audit + access policy surface. Exposes an
    /// <see cref="IDistributedAuditSink"/> accessor, convenience
    /// <c>RaiseAuditEvent</c> emitters used by sibling partials,
    /// and an <see cref="EnsureAccess"/> helper that throws
    /// <see cref="DistributedSecurityException"/> when the
    /// configured policy denies a caller.
    /// </summary>
    public partial class DistributedDataSource
    {
        private IDistributedAuditSink _auditSink;
        private IDistributedAccessPolicy _accessPolicy;

        /// <summary>
        /// Returns the active audit sink. Never <c>null</c>;
        /// falls back to <see cref="NullDistributedAuditSink.Instance"/>
        /// when none is configured.
        /// </summary>
        public IDistributedAuditSink AuditSink
        {
            get
            {
                ThrowIfDisposed();
                var configured = _options?.AuditSink;
                if (configured != null) return configured;

                if (_auditSink == null) _auditSink = NullDistributedAuditSink.Instance;
                return _auditSink;
            }
        }

        /// <summary>
        /// Returns the active access policy. Never <c>null</c>;
        /// falls back to <see cref="AllowAllAccessPolicy.Instance"/>.
        /// </summary>
        public IDistributedAccessPolicy AccessPolicy
        {
            get
            {
                ThrowIfDisposed();
                var configured = _options?.AccessPolicy;
                if (configured != null) return configured;

                if (_accessPolicy == null) _accessPolicy = AllowAllAccessPolicy.Instance;
                return _accessPolicy;
            }
        }

        /// <summary>Replaces the active audit sink at runtime.</summary>
        public void ConfigureAuditSink(IDistributedAuditSink sink)
        {
            ThrowIfDisposed();
            _options.AuditSink = sink;
            _auditSink = sink;
        }

        /// <summary>Replaces the active access policy at runtime.</summary>
        public void ConfigureAccessPolicy(IDistributedAccessPolicy policy)
        {
            ThrowIfDisposed();
            _options.AccessPolicy = policy;
            _accessPolicy = policy;
        }

        /// <summary>
        /// Conventional tag key callers can set on a
        /// <see cref="DistributedExecutionContext"/> to forward an
        /// authenticated principal through routing/audit/access
        /// checks without leaking it into signatures.
        /// </summary>
        public const string PrincipalTagKey = "beep.principal";

        /// <summary>
        /// Extracts the principal from a context's tag bag. Returns
        /// <c>null</c> when no principal is present; access policies
        /// treat <c>null</c> as an anonymous caller.
        /// </summary>
        internal static string ResolvePrincipal(DistributedExecutionContext ctx)
        {
            if (ctx == null || ctx.Tags == null) return null;
            ctx.Tags.TryGetValue(PrincipalTagKey, out var principal);
            return string.IsNullOrWhiteSpace(principal) ? null : principal;
        }

        /// <summary>
        /// Throws <see cref="DistributedSecurityException"/> when
        /// <see cref="AccessPolicy"/> denies the caller. Call at
        /// the boundary of every executor hop so denied requests
        /// never reach a shard.
        /// </summary>
        internal void EnsureAccess(
            string                 entityName,
            DistributedAccessKind  kind,
            string                 principal)
        {
            var policy = AccessPolicy;
            if (policy == null) return;
            if (policy.IsAllowed(entityName, kind, principal)) return;

            RaiseAuditEvent(
                kind:       DistributedAuditEventKind.AccessDenied,
                operation:  kind.ToString(),
                entityName: entityName,
                principal:  principal,
                message:    $"Denied by {policy.GetType().Name}");

            throw new DistributedSecurityException(
                entityName: entityName,
                accessKind: kind,
                principal:  principal,
                reason:     $"Blocked by {policy.GetType().Name}.");
        }

        /// <summary>
        /// Emits one <see cref="DistributedAuditEvent"/> via the
        /// configured sink. Never throws.
        /// </summary>
        internal void RaiseAuditEvent(
            DistributedAuditEventKind      kind,
            string                         operation     = null,
            string                         entityName    = null,
            string                         mode          = null,
            IReadOnlyList<string>          shardIds      = null,
            string                         partitionKey  = null,
            string                         principal     = null,
            string                         correlationId = null,
            string                         message       = null,
            Exception                      error         = null,
            IReadOnlyDictionary<string, string> tags     = null)
        {
            try
            {
                var sink = AuditSink;
                if (sink == null || sink is NullDistributedAuditSink) return;

                var evt = DistributedAuditEvent.Now(
                    kind:          kind,
                    correlationId: correlationId,
                    entityName:    entityName,
                    mode:          mode,
                    operation:     operation,
                    shardIds:      shardIds,
                    partitionKey:  partitionKey,
                    principal:     principal,
                    message:       message,
                    error:         error,
                    tags:          tags);

                sink.Write(evt);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("Audit sink write failed: " + ex.Message);
            }
        }
    }
}
