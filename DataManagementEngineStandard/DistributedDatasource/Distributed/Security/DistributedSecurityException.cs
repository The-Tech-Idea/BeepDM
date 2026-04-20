using System;

namespace TheTechIdea.Beep.Distributed.Security
{
    /// <summary>
    /// Thrown by the distribution tier when an
    /// <see cref="IDistributedAccessPolicy"/> denies a caller.
    /// Carries the access kind, entity, and principal so audit
    /// handlers can log a clean record.
    /// </summary>
    [Serializable]
    public sealed class DistributedSecurityException : Exception
    {
        /// <summary>Creates a new exception.</summary>
        public DistributedSecurityException(
            string                 entityName,
            DistributedAccessKind  accessKind,
            string                 principal,
            string                 reason)
            : base(BuildMessage(entityName, accessKind, principal, reason))
        {
            EntityName = entityName ?? string.Empty;
            AccessKind = accessKind;
            Principal  = principal  ?? string.Empty;
            Reason     = reason     ?? string.Empty;
        }

        /// <summary>Entity the caller attempted to access.</summary>
        public string EntityName { get; }

        /// <summary>Access kind (Read / Write / Ddl).</summary>
        public DistributedAccessKind AccessKind { get; }

        /// <summary>Principal that was denied (empty when unknown).</summary>
        public string Principal { get; }

        /// <summary>Free-form remediation hint.</summary>
        public string Reason { get; }

        private static string BuildMessage(
            string entityName,
            DistributedAccessKind kind,
            string principal,
            string reason)
        {
            var who = string.IsNullOrEmpty(principal) ? "(anonymous)" : principal;
            var what = string.IsNullOrEmpty(entityName) ? "(unknown entity)" : entityName;
            var why  = string.IsNullOrEmpty(reason) ? "Access denied by distributed access policy." : reason;
            return $"Access denied: principal '{who}' cannot perform {kind} on '{what}'. {why}";
        }
    }
}
