using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Classification;
using TheTechIdea.Beep.FileManager.Governance;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Governance partial — access policy enforcement, row-level security, field masking.
    /// Phase 7 implementation.
    /// </summary>
    public partial class FileDataSource
    {
        // ── Access-policy enforcement ─────────────────────────────────────────

        /// <summary>
        /// Calls <see cref="IFileAccessPolicy.Enforce"/> if an access policy is wired.
        /// Throws <see cref="UnauthorizedAccessException"/> if the policy denies the operation.
        /// No-ops when <see cref="AccessPolicy"/> or <see cref="TenantContext"/> is null.
        /// </summary>
        internal void EnforceAccessPolicy(string entityName, FileOperation operation)
        {
            if (AccessPolicy == null || TenantContext == null) return;
            AccessPolicy.Enforce(entityName, operation, TenantContext);
        }

        // ── Row-level-security ────────────────────────────────────────────────

        /// <summary>
        /// Merges RLS predicates into the existing filter list.
        /// Returns a new list that includes both caller-supplied and security-supplied filters.
        /// </summary>
        internal List<AppFilter> ApplyRowSecurity(string entityName, List<AppFilter> filters)
        {
            if (RowSecurityFilter == null || TenantContext == null) return filters;

            IReadOnlyList<AppFilter> secFilters =
                RowSecurityFilter.GetFilters(entityName, TenantContext);

            if (secFilters == null || secFilters.Count == 0) return filters;

            var merged = new List<AppFilter>(filters ?? new List<AppFilter>());
            merged.AddRange(secFilters);
            return merged;
        }

        // ── Field masking ─────────────────────────────────────────────────────

        /// <summary>
        /// Applies column-level masking to a projected row dictionary.
        /// Non-string values and columns without a registered policy are passed through unchanged.
        /// Returns the same dictionary reference with masked values applied in-place.
        /// </summary>
        internal Dictionary<string, object> ApplyFieldMasking(
            EntityStructure             entity,
            Dictionary<string, object>  row)
        {
            if (MaskingEngine == null || MaskingPolicies == null || row == null) return row;

            foreach (var key in row.Keys.ToList())
            {
                ColumnMaskingPolicy policy = MaskingPolicies.GetPolicy(key);
                if (policy == null) continue;
                if (row[key] is string strVal)
                    row[key] = MaskingEngine.Mask(strVal, key, policy);
            }

            return row;
        }
    }
}
