using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Canonical audit-event envelope. This Phase 01 declaration carries only
    /// the fields needed for the public surface to compile; the full schema
    /// (chain fields, categories, outcomes, field changes) is layered in by
    /// Phase 08 via additional partials.
    /// </summary>
    /// <remarks>
    /// Declared as <c>partial</c> so subsequent phases can add members
    /// (notably <c>ChainId</c>, <c>Sequence</c>, <c>PrevHash</c>, <c>Hash</c>,
    /// <c>Category</c>, <c>Operation</c>, <c>Outcome</c>, <c>FieldChanges</c>)
    /// without forcing a breaking change here.
    /// </remarks>
    public partial class AuditEvent
    {
        /// <summary>Stable identifier for this event.</summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>UTC timestamp when the event was produced.</summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Originating subsystem (for example <c>Forms.Block.HR_EMP</c>).</summary>
        public string Source { get; set; }

        /// <summary>Logical entity name (table, block, resource).</summary>
        public string EntityName { get; set; }

        /// <summary>Stable record identifier within the entity, if applicable.</summary>
        public string RecordKey { get; set; }

        /// <summary>Authenticated user identifier, if known.</summary>
        public string UserId { get; set; }

        /// <summary>Friendly user name, if known.</summary>
        public string UserName { get; set; }

        /// <summary>Tenant or container scope, if known.</summary>
        public string Tenant { get; set; }

        /// <summary>Correlation identifier shared with related log entries.</summary>
        public string CorrelationId { get; set; }

        /// <summary>OpenTelemetry-compatible trace identifier, if available.</summary>
        public string TraceId { get; set; }

        /// <summary>Free-form structured properties.</summary>
        public IDictionary<string, object> Properties { get; set; }
    }
}
