using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// Tamper-evident manifest produced alongside every audit export.
    /// Carries the SHA-256 of the export payload, the count of events,
    /// the time window covered, the chains touched, and an HMAC of the
    /// canonical manifest payload signed with the same key material as
    /// the audit hash chain (Phase 08). Verifying an export means
    /// re-hashing the payload, recomputing the HMAC, and comparing.
    /// </summary>
    public sealed class ExportManifest
    {
        /// <summary>Manifest schema version. Bumped on breaking changes.</summary>
        public int Version { get; set; } = 1;

        /// <summary>UTC timestamp the export was produced.</summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Operator that requested the export.</summary>
        public string OperatorId { get; set; }

        /// <summary>Format of the payload file (<c>"ndjson"</c>, <c>"csv"</c>, <c>"json"</c>).</summary>
        public string Format { get; set; }

        /// <summary>Total event count contained in the payload.</summary>
        public long EventCount { get; set; }

        /// <summary>Inclusive lower bound on <c>TimestampUtc</c> for the events.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Inclusive upper bound on <c>TimestampUtc</c> for the events.</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Distinct chain ids represented in the payload.</summary>
        public IList<string> ChainIds { get; set; } = new List<string>();

        /// <summary>SHA-256 (hex) of the payload bytes.</summary>
        public string PayloadSha256 { get; set; }

        /// <summary>HMAC-SHA256 (hex) of the canonical manifest payload, sans the HMAC field itself.</summary>
        public string ManifestHmac { get; set; }

        /// <summary>Free-form notes recorded by the operator (e.g. ticket number).</summary>
        public string Notes { get; set; }
    }
}
