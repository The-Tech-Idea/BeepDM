using System;

namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Phase 10 partial that exposes filter-evaluation helpers reused by
    /// every storage engine. Centralizing the predicates here keeps the
    /// SQLite, file-scan, and in-memory engines consistent and avoids
    /// subtle drift between engines as new filter fields are added.
    /// </summary>
    /// <remarks>
    /// The helpers operate on a single <see cref="AuditEvent"/> and return
    /// <c>true</c> when the event is a match. Engines that can push a
    /// filter into the storage layer (SQLite WHERE clauses) still use
    /// <see cref="Matches"/> for any predicate that cannot be expressed
    /// natively (typed property values, for example).
    /// </remarks>
    public partial class AuditQuery
    {
        /// <summary>
        /// Evaluates every configured filter against
        /// <paramref name="auditEvent"/> and returns <c>true</c> when the
        /// event satisfies all of them. <c>null</c> filters are treated
        /// as wildcards.
        /// </summary>
        public bool Matches(AuditEvent auditEvent)
        {
            if (auditEvent is null)
            {
                return false;
            }

            if (FromUtc.HasValue && auditEvent.TimestampUtc < FromUtc.Value)
            {
                return false;
            }
            if (ToUtc.HasValue && auditEvent.TimestampUtc > ToUtc.Value)
            {
                return false;
            }
            if (!IsEqual(Source, auditEvent.Source))
            {
                return false;
            }
            if (!IsEqual(EntityName, auditEvent.EntityName))
            {
                return false;
            }
            if (!IsEqual(RecordKey, auditEvent.RecordKey))
            {
                return false;
            }
            if (!IsEqual(UserId, auditEvent.UserId))
            {
                return false;
            }
            if (!IsEqual(Tenant, auditEvent.Tenant))
            {
                return false;
            }
            if (!IsEqual(ChainIdFilter, auditEvent.ChainId))
            {
                return false;
            }
            if (CategoryFilter.HasValue && CategoryFilter.Value != auditEvent.Category)
            {
                return false;
            }
            if (OutcomeFilter.HasValue && OutcomeFilter.Value != auditEvent.Outcome)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(OperationFilter)
                && !string.Equals(OperationFilter, auditEvent.Operation, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (PropertyFilters is { Count: > 0 })
            {
                if (auditEvent.Properties is null || auditEvent.Properties.Count == 0)
                {
                    return false;
                }
                foreach (var kvp in PropertyFilters)
                {
                    if (!auditEvent.Properties.TryGetValue(kvp.Key, out object actual))
                    {
                        return false;
                    }
                    if (!ScalarEquals(kvp.Value, actual))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsEqual(string filter, string actual)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }
            return string.Equals(filter, actual, StringComparison.Ordinal);
        }

        private static bool ScalarEquals(object expected, object actual)
        {
            if (expected is null)
            {
                return actual is null;
            }
            if (actual is null)
            {
                return false;
            }
            if (expected is string sExp && actual is string sAct)
            {
                return string.Equals(sExp, sAct, StringComparison.Ordinal);
            }
            return string.Equals(
                Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }
    }
}
