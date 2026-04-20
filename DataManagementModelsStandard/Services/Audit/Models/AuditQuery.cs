using System;

namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Filter description used by <c>IBeepAudit.QueryAsync</c>.
    /// Phase 01 ships only the minimal fields needed for the contract;
    /// the fluent builder and engine-specific filters arrive in Phase 10.
    /// </summary>
    public partial class AuditQuery
    {
        /// <summary>Optional inclusive lower bound on <c>TimestampUtc</c>.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Optional inclusive upper bound on <c>TimestampUtc</c>.</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Optional <c>Source</c> filter.</summary>
        public string Source { get; set; }

        /// <summary>Optional <c>EntityName</c> filter.</summary>
        public string EntityName { get; set; }

        /// <summary>Optional <c>RecordKey</c> filter.</summary>
        public string RecordKey { get; set; }

        /// <summary>Optional <c>UserId</c> filter.</summary>
        public string UserId { get; set; }

        /// <summary>Optional <c>Tenant</c> filter.</summary>
        public string Tenant { get; set; }

        /// <summary>Maximum records to return; 0 = unbounded.</summary>
        public int Take { get; set; }
    }
}
