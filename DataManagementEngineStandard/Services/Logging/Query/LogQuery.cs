using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Lightweight filter for diagnostics-grade log queries. Logs are
    /// sampled, deduped, and rate-limited so the query API is intended
    /// for operator triage rather than reporting; the search surface is
    /// deliberately small to keep storage indexes cheap.
    /// </summary>
    public sealed class LogQuery
    {
        /// <summary>Optional inclusive lower bound on <c>TimestampUtc</c>.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Optional inclusive upper bound on <c>TimestampUtc</c>.</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Optional minimum level (records below are filtered out).</summary>
        public BeepLogLevel? MinLevel { get; set; }

        /// <summary>Optional case-insensitive category filter.</summary>
        public string Category { get; set; }

        /// <summary>Optional case-insensitive substring match on <c>Message</c>.</summary>
        public string MessageContains { get; set; }

        /// <summary>Optional <c>CorrelationId</c> filter.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Optional <c>TraceId</c> filter.</summary>
        public string TraceId { get; set; }

        /// <summary>Maximum records to return; 0 = unbounded.</summary>
        public int Take { get; set; }

        /// <summary>
        /// When <c>true</c> (the default) results are ordered by
        /// timestamp descending (newest first).
        /// </summary>
        public bool OrderDescending { get; set; } = true;

        /// <summary>
        /// In-memory match used by file-scan engines after parsing each
        /// candidate row.
        /// </summary>
        public bool Matches(LogRecord record)
        {
            if (record is null) { return false; }
            if (FromUtc.HasValue && record.TimestampUtc < FromUtc.Value) { return false; }
            if (ToUtc.HasValue && record.TimestampUtc > ToUtc.Value) { return false; }
            if (MinLevel.HasValue && record.Level < MinLevel.Value) { return false; }
            if (!string.IsNullOrEmpty(Category) &&
                !string.Equals(record.Category, Category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(MessageContains))
            {
                if (string.IsNullOrEmpty(record.Message)) { return false; }
                if (record.Message.IndexOf(MessageContains, StringComparison.OrdinalIgnoreCase) < 0) { return false; }
            }
            if (!string.IsNullOrEmpty(CorrelationId) &&
                !string.Equals(record.CorrelationId, CorrelationId, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(TraceId) &&
                !string.Equals(record.TraceId, TraceId, StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Materialized log row returned by <see cref="ILogQueryEngine"/>.
    /// Distinct from <c>TelemetryEnvelope</c> so callers do not couple
    /// to the internal pipeline shape.
    /// </summary>
    public sealed class LogRecord
    {
        /// <summary>UTC timestamp the record was produced.</summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>Severity level.</summary>
        public BeepLogLevel Level { get; set; }

        /// <summary>Source category (typically a fully-qualified type name).</summary>
        public string Category { get; set; }

        /// <summary>Free-form message body.</summary>
        public string Message { get; set; }

        /// <summary>Correlation identifier, if propagated.</summary>
        public string CorrelationId { get; set; }

        /// <summary>OpenTelemetry-compatible trace identifier, if available.</summary>
        public string TraceId { get; set; }

        /// <summary>Free-form structured properties.</summary>
        public IDictionary<string, object> Properties { get; set; }
    }
}
